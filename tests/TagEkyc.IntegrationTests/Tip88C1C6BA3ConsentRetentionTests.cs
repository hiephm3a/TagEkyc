using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Migrations;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3ConsentRetentionTests(PostgresPersistenceFixture postgres)
{
    internal static readonly Guid Client = LocalDevRuntimePolicySource.BusinessClientId;
    internal static readonly Guid Principal = Guid.Parse("a3000000-0000-4000-8000-000000000002");
    private static readonly Guid Admin = Guid.Parse("a3000000-0000-4000-8000-000000000003");
    private static readonly JsonSerializerOptions Wire = new() { Converters = { new UtcTimestampWriter() } };

    [Fact]
    public async Task E01_RecordSqlCommitsRealGraph()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_sql");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client);
        await Grant(db, "SubjectConsentRecorder");
        var record = Record();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            var code = await db.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_record_consent_reference(
                  {Principal},{Client},{session},{record.ExternalConsentArtifactRef},{record.SourceVersion},
                  {record.ExpectedReferenceRevision},{record.ConsentTextVersion},{record.ConsentTextContentHash},
                  {record.ValidFromUtc},{record.ValidUntilUtc},{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync();
            Assert.Equal("Bound", code);
            await tx.CommitAsync();
        }
        await AssertCounts(isolated, 1, 1, 1);
    }

    [Fact]
    public async Task ExistingConsentReference_IsBoundNotAssumed()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_owned");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var owned = await Session(db, Client);
        var foreign = await Session(db, Guid.NewGuid());
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        var request = Record();
        using (var denied = await Post(client, Route(owned), request, Guid.NewGuid()))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await AssertCounts(isolated, 0, 0, 0);
        await Grant(db, "SubjectConsentRecorder");
        using (var denied = await Post(client, Route(foreign), request, Guid.NewGuid()))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Contains("ACCESS_DENIED", await denied.Content.ReadAsStringAsync());
        }
        await AssertCounts(isolated, 0, 0, 0);
        using var granted = await Post(client, Route(owned), request, Guid.NewGuid());
        Assert.True(granted.IsSuccessStatusCode, (await granted.Content.ReadAsStringAsync()) + A1SyntheticDbFailureInterceptor.Recorded);
        var binding = await ReadBound(granted);
        Assert.NotEqual(Guid.Empty, binding.ConsentBindingId);
        await AssertCounts(isolated, 1, 1, 1);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(Client, await observer.Database.SqlQueryRaw<Guid>(
            "SELECT \"ClientApplicationId\" AS \"Value\" FROM tagekyc.raw_source_consent_references").SingleAsync());
        Assert.Equal(0, await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM pg_catalog.pg_constraint WHERE conname='FK_a3_consent_reference_client'").SingleAsync());
        Assert.Null(await observer.Database.SqlQueryRaw<string>(
            "SELECT to_regclass('tagekyc.client_applications')::text AS \"Value\"").SingleAsync());
    }

    [Theory]
    [InlineData("subject\nA")]
    [InlineData("subject\rA")]
    [InlineData("subject\r\nA")]
    public async Task E01_PreservesExactExistingSessionSubjectRef(string subjectRef)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_subject");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client, subjectRef);
        await Grant(db, "SubjectConsentRecorder");
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var response = await Post(client, Route(session), Record(), Guid.NewGuid());
        var bound = await ReadBound(response);
        await AssertCounts(isolated, 1, 1, 1);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(subjectRef, await observer.Database.SqlQuery<string>($"""
            SELECT "SubjectRef" AS "Value" FROM tagekyc.verification_sessions WHERE "Id"={session}
            """).SingleAsync());
        Assert.Equal(subjectRef, await observer.Database.SqlQuery<string>($"""
            SELECT "SubjectRef" AS "Value" FROM tagekyc.raw_source_consent_references
            WHERE "ConsentReferenceId"={bound.ConsentReferenceId}
            """).SingleAsync());
        Assert.Equal(subjectRef, await observer.Database.SqlQuery<string>($"""
            SELECT "SubjectRef" AS "Value" FROM tagekyc.raw_source_consent_bindings
            WHERE "ConsentBindingId"={bound.ConsentBindingId}
            """).SingleAsync());
    }

    [Theory]
    [InlineData("clientApplicationId")]
    [InlineData("ClientApplicationId")]
    [InlineData("principalId")]
    public async Task E01_RequestCannotInjectClientIdentity(string member)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_json");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client);
        await Grant(db, "SubjectConsentRecorder");
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        var body = JsonSerializer.Serialize(Record(), Wire);
        body = body[..^1] + ",\"" + member + "\":\"" + Guid.NewGuid().ToString("N") + "\"}";
        using var request = new HttpRequestMessage(HttpMethod.Post, Route(session));
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertCounts(isolated, 0, 0, 0);
    }

    [Fact]
    public async Task E01_ExactReplayDoesNotReauthorizeAfterWithdrawal()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_replay");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client);
        await Grant(db, "SubjectConsentRecorder");
        await Grant(db, "SubjectConsentWithdrawer");
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        var record = Record(); var key = Guid.NewGuid();
        using var first = await Post(client, Route(session), record, key);
        Assert.True(first.IsSuccessStatusCode, (await first.Content.ReadAsStringAsync()) + A1SyntheticDbFailureInterceptor.Recorded);
        var bound = await ReadBound(first);
        using var withdrawn = await Post(client, $"/api/ekyc/source-consent-references/{bound.ConsentReferenceId:N}/withdraw",
            new E01WithdrawRequest(1, record.SourceVersion, "synthetic-withdrawal"), Guid.NewGuid());
        Assert.True(withdrawn.IsSuccessStatusCode, await withdrawn.Content.ReadAsStringAsync());
        using var replay = await Post(client, Route(session), record, key);
        Assert.Equal(bound, await ReadBound(replay));
        using var conflict = await Post(client, Route(session), record with { SourceVersion = "different" }, key);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var fresh = await Post(client, Route(session), record with { ExpectedReferenceRevision = 2 }, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Conflict, fresh.StatusCode);
        await AssertCounts(isolated, 1, 2, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E01_Record_PostLockExpiryDeniesWithoutGraphMutation(bool recorderExpires)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_record_clock");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client);
        var expiry = RoundedUtc(DateTimeOffset.UtcNow.AddSeconds(5));
        await Grant(setup, "SubjectConsentRecorder", recorderExpires ? expiry : null);
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var first = await Post(client, Route(session), Record(), Guid.NewGuid());
        var bound = await ReadBound(first);
        var before = await ConsentGraph(isolated);
        var next = Record() with
        {
            SourceVersion = "source-v2",
            ExpectedReferenceRevision = bound.ConsentReferenceRevision,
            ValidUntilUtc = recorderExpires ? RoundedUtc(DateTimeOffset.UtcNow.AddMinutes(10)) : expiry
        };
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended(
             'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
            """);
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var running = Post(client, Route(session), next, Guid.NewGuid());
        await using var observer = isolated.CreateDbContext();
        try
        {
            await WaitForReferenceLock(observer, blockerPid, "raw_source_record_consent_reference", "ExclusiveLock");
            Assert.False(running.IsCompleted);
            Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp() < {expiry} AS \"Value\"").SingleAsync(),
                "The real Record contender must reach the reference lock before the tested expiry.");
            await WaitUntilDatabaseClock(observer, expiry);
            Assert.Equal(before, await ConsentGraph(isolated));
            await hold.CommitAsync();
            using var denied = await running.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Contains("ACCESS_DENIED", await denied.Content.ReadAsStringAsync());
            Assert.Equal(before, await ConsentGraph(isolated));
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            if (!running.IsCompleted)
            {
                using var completed = await running.WaitAsync(TimeSpan.FromSeconds(15));
            }
        }
    }

    [Fact]
    public async Task E01_Race_RecordCas_OneWinnerAndExactReceipt()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_record_cas");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var firstSession = await Session(setup, Client);
        var secondSession = await Session(setup, Client);
        await Grant(setup, "SubjectConsentRecorder");
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended(
             'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
            """);
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var firstBody = Record();
        var secondBody = firstBody with { SourceVersion = "source-v2" };
        var firstKey = Guid.NewGuid();
        var first = Post(client, Route(firstSession), firstBody, firstKey);
        await using var observer = isolated.CreateDbContext();
        await WaitForReferenceLock(observer, blockerPid, "raw_source_record_consent_reference", "ExclusiveLock");
        var second = Post(client, Route(secondSession), secondBody, Guid.NewGuid());
        try
        {
            await WaitForReferenceContenders(observer, blockerPid, "raw_source_record_consent_reference", "ExclusiveLock", 2);
            Assert.Empty(await ConsentGraphRows(isolated));
            await hold.CommitAsync();
            using var winner = await first.WaitAsync(TimeSpan.FromSeconds(15));
            var bound = await ReadBound(winner);
            using var loser = await second.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(HttpStatusCode.Conflict, loser.StatusCode);
            Assert.Equal(1L, bound.ConsentReferenceRevision);
            await AssertCounts(isolated, 1, 1, 1);
            var graph = await ConsentGraph(isolated);
            using var replay = await Post(client, Route(firstSession), firstBody, firstKey);
            Assert.Equal(bound, await ReadBound(replay));
            Assert.Equal(graph, await ConsentGraph(isolated));
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            if (!first.IsCompleted) using (var completed = await first.WaitAsync(TimeSpan.FromSeconds(15))) { }
            if (!second.IsCompleted) using (var completed = await second.WaitAsync(TimeSpan.FromSeconds(15))) { }
        }
    }

    [Fact]
    public async Task E01_RecorderRevocationRacesRecord_RevocationFirstDenies()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_revoke_record");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var positiveSession = await Session(setup, Client);
        var deniedSession = await Session(setup, Client);
        await Grant(setup, "SubjectConsentRecorder");
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var positive = await Post(client, Route(positiveSession), Record(), Guid.NewGuid());
        await ReadBound(positive);
        var before = await ConsentGraph(isolated);
        await using var revoker = isolated.CreateDbContext();
        await using var revokeTx = await revoker.Database.BeginTransactionAsync();
        await revoker.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Admin.ToString("D")},true)");
        Assert.Equal(2, await revoker.Database.SqlQuery<int>($"""
            SELECT tagekyc.raw_export_append_subject_consent_authority(
             {Principal},{Client},'SubjectConsentRecorder',1,'Revoked',1,'synthetic-revocation',NULL::timestamptz) AS "Value"
            """).SingleAsync());
        var revokerPid = await revoker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var deniedBody = Record() with { ExternalConsentArtifactRef = "synthetic-other-consent" };
        var running = Post(client, Route(deniedSession), deniedBody, Guid.NewGuid());
        await using var observer = isolated.CreateDbContext();
        try
        {
            var blocked = await WaitForLockOrCompletion(observer, revokerPid,
                "raw_source_record_consent_reference", "ShareLock", running);
            if (blocked) Assert.Equal(before, await ConsentGraph(isolated));
            await revokeTx.CommitAsync();
            using var denied = await running.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.True(blocked, "Record must have waited behind the real revocation transaction.");
            Assert.Equal(before, await ConsentGraph(isolated));
        }
        finally
        {
            if (revokeTx.GetDbTransaction().Connection is not null) await revokeTx.RollbackAsync();
            if (!running.IsCompleted) using (var completed = await running.WaitAsync(TimeSpan.FromSeconds(15))) { }
        }
    }

    [Fact]
    public async Task E01_UpdateDoesNotReviveBinding_ExactReplayIsOnlyReceipt()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_update_binding");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client, challengeBound: true);
        await Grant(setup, "SubjectConsentRecorder");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!, retentionProfile: Profile(policy));
        using var client = app.GetTestClient();
        var original = Record(); var originalKey = Guid.NewGuid();
        using var first = await Post(client, Route(session), original, originalKey);
        var oldBinding = await ReadBound(first);
        var updated = original with { SourceVersion = "source-v2", ExpectedReferenceRevision = 1 };
        using var updateResponse = await Post(client, Route(session), updated, Guid.NewGuid());
        var newBinding = await ReadBound(updateResponse);
        Assert.Equal(oldBinding.ConsentReferenceId, newBinding.ConsentReferenceId);
        Assert.Equal(2L, newBinding.ConsentReferenceRevision);
        Assert.NotEqual(oldBinding.ConsentBindingId, newBinding.ConsentBindingId);
        using var replay = await Post(client, Route(session), original, originalKey);
        Assert.Equal(oldBinding, await ReadBound(replay));
        await AssertCounts(isolated, 1, 2, 2);
        using var staleIssue = await PostIssue(client, session, oldBinding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, staleIssue.StatusCode);
        await AssertRetentionIssueCounts(isolated, 0, 0, 0);
        using var currentIssue = await PostIssue(client, session, newBinding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, currentIssue.StatusCode);
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(newBinding.ConsentBindingId, await observer.Database.SqlQueryRaw<Guid>(
            "SELECT \"ConsentBindingId\" AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E01_Issue_PostLockRecorderAndSourceExpiryDeniesWithoutNewAuthority(bool recorderExpires)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_issue_clock");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var firstSession = await Session(setup, Client, challengeBound: true);
        var secondSession = await Session(setup, Client, challengeBound: true);
        var policy = await ApprovedRetentionPolicy(setup);
        var expiry = RoundedUtc(DateTimeOffset.UtcNow.AddSeconds(6));
        await Grant(setup, "SubjectConsentRecorder", recorderExpires ? expiry : null);
        var source = Record() with
        {
            ValidUntilUtc = recorderExpires ? RoundedUtc(DateTimeOffset.UtcNow.AddMinutes(10)) : expiry
        };
        await using var app = await Start(setup.Database.GetConnectionString()!, retentionProfile: Profile(policy));
        using var client = app.GetTestClient();
        using var firstRecord = await Post(client, Route(firstSession), source, Guid.NewGuid());
        var firstBinding = await ReadBound(firstRecord);
        using var secondRecord = await Post(client, Route(secondSession),
            source with { ExpectedReferenceRevision = 1 }, Guid.NewGuid());
        var secondBinding = await ReadBound(secondRecord);
        Assert.Equal(firstBinding.ConsentReferenceId, secondBinding.ConsentReferenceId);
        using var positive = await PostIssue(client, firstSession, firstBinding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, positive.StatusCode);
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended(
             'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
            """);
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var running = PostIssue(client, secondSession, secondBinding.ConsentBindingId, Guid.NewGuid());
        await using var observer = isolated.CreateDbContext();
        try
        {
            await WaitForReferenceLock(observer, blockerPid, "capture_runtime_issue_or_replace_capability", "ShareLock");
            Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp() < {expiry} AS \"Value\"").SingleAsync(),
                "The real R20 Issue contender must reach the reference lock before expiry.");
            await WaitUntilDatabaseClock(observer, expiry);
            await AssertRetentionIssueCounts(isolated, 1, 2, 1);
            await hold.CommitAsync();
            using var denied = await running.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            await AssertRetentionIssueCounts(isolated, 1, 2, 1);
            await AssertCounts(isolated, 1, 1, 2);
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            if (!running.IsCompleted) using (var completed = await running.WaitAsync(TimeSpan.FromSeconds(15))) { }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task E01_Race_IssueWithdrawal_ReferenceLockOrdersPermitAndWithdrawal(bool withdrawalFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_issue_withdraw");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client, challengeBound: true);
        await Grant(setup, "SubjectConsentRecorder");
        await Grant(setup, "SubjectConsentWithdrawer");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!, retentionProfile: Profile(policy));
        using var client = app.GetTestClient();
        using var recorded = await Post(client, Route(session), Record(), Guid.NewGuid());
        var binding = await ReadBound(recorded);
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        if (withdrawalFirst)
            await blocker.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT pg_advisory_xact_lock(hashtextextended(
                 'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
                """);
        else
            await blocker.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT pg_advisory_xact_lock_shared(hashtextextended(
                 'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
                """);
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        await using var observer = isolated.CreateDbContext();
        Task<HttpResponseMessage>? withdrawal = null;
        Task<HttpResponseMessage>? issue = null;
        Func<Task<string>>? racePermitR1 = null;
        try
        {
            if (withdrawalFirst)
            {
                withdrawal = Post(client,
                    $"/api/ekyc/source-consent-references/{binding.ConsentReferenceId:N}/withdraw",
                    new E01WithdrawRequest(1, "source-v1", "synthetic-withdrawal"), Guid.NewGuid());
                await WaitForReferenceLock(observer, blockerPid, "raw_source_withdraw_consent_reference", "ExclusiveLock");
                issue = PostIssue(client, session, binding.ConsentBindingId, Guid.NewGuid());
            }
            else
            {
                issue = PostIssue(client, session, binding.ConsentBindingId, Guid.NewGuid());
                using (var issuedBeforeWithdrawal = await issue.WaitAsync(TimeSpan.FromSeconds(15)))
                    Assert.Equal(HttpStatusCode.Created, issuedBeforeWithdrawal.StatusCode);
                await AssertRetentionIssueCounts(isolated, 1, 2, 1);
                racePermitR1 = await Tip88C1C6BA3RetentionCheckpointTests.PrepareR1ForIssuedPermit(
                    isolated, session, policy, binding.ConsentReferenceId);
                withdrawal = Post(client,
                    $"/api/ekyc/source-consent-references/{binding.ConsentReferenceId:N}/withdraw",
                    new E01WithdrawRequest(1, "source-v1", "synthetic-withdrawal"), Guid.NewGuid());
            }
            var followerBlocked = withdrawalFirst
                ? await WaitForLockOrCompletion(observer, blockerPid,
                    "capture_runtime_issue_or_replace_capability", "ShareLock", issue!)
                : await WaitForLockOrCompletion(observer, blockerPid,
                    "raw_source_withdraw_consent_reference", "ExclusiveLock", withdrawal!);
            await AssertRetentionIssueCounts(isolated,
                withdrawalFirst ? 0 : 1, withdrawalFirst ? 0 : 2, withdrawalFirst ? 0 : 1);
            await hold.CommitAsync();
            using var withdrawn = await withdrawal!.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(HttpStatusCode.OK, withdrawn.StatusCode);
            if (withdrawalFirst)
            {
                using var deniedIssue = await issue!.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.Equal(HttpStatusCode.Forbidden, deniedIssue.StatusCode);
            }
            Assert.True(followerBlocked, "The follower must serialize on the same PostgreSQL reference key.");
            await AssertRetentionIssueCounts(isolated, withdrawalFirst ? 0 : 1,
                withdrawalFirst ? 0 : 2, withdrawalFirst ? 0 : 1);
            await AssertCounts(isolated, 1, 2, 1);
            Assert.Equal(withdrawalFirst ? 0 : 1, await observer.Database.SqlQueryRaw<int>(
                "SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_execution_bindings").SingleAsync());
            if (!withdrawalFirst)
            {
                var permit = await observer.Database.SqlQueryRaw<Guid>(
                    "SELECT \"RetentionAuthorityId\" AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync();
                await using var tx = await observer.Database.BeginTransactionAsync();
                await PrelockRetentionRead(observer, session, policy);
                Assert.Equal(0, await observer.Database.SqlQuery<int>($"""
                    SELECT count(*)::integer AS "Value" FROM tagekyc.raw_source_resolve_retention_authority(
                     {permit},1,{Principal},{Client},{session},'ChipDg2Portrait',clock_timestamp())
                    """).SingleAsync());
                await tx.CommitAsync();
                Assert.NotNull(racePermitR1);
                Assert.Equal(0, await observer.RawExportSourceReservations.CountAsync());
                Assert.Equal(0, await observer.RawExportSourceEncryptionAttempts.CountAsync());
                Assert.Equal("SOURCE_RETENTION_NOT_AUTHORIZED", await racePermitR1());
                Assert.Equal(0, await observer.RawExportSourceReservations.CountAsync());
                Assert.Equal(0, await observer.RawExportSourceEncryptionAttempts.CountAsync());
            }
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            if (withdrawal is { IsCompleted: false }) using (var completed = await withdrawal.WaitAsync(TimeSpan.FromSeconds(15))) { }
            if (issue is { IsCompleted: false }) using (var completed = await issue.WaitAsync(TimeSpan.FromSeconds(15))) { }
        }
    }

    [Theory]
    [InlineData("ChipDg2Portrait")]
    [InlineData("LiveSelfieImage")]
    public async Task E01_ExactBothClassesRequired_ProfileAndSqlRejectOneClass(string retainedClass)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_two_classes");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var firstSession = await Session(setup, Client, challengeBound: true);
        var secondSession = await Session(setup, Client, challengeBound: true);
        await Grant(setup, "SubjectConsentRecorder");
        var policy = await ApprovedRetentionPolicy(setup);
        var source = Record();
        await using var positiveApp = await Start(setup.Database.GetConnectionString()!, retentionProfile: Profile(policy));
        using var positiveClient = positiveApp.GetTestClient();
        using var firstRecord = await Post(positiveClient, Route(firstSession), source, Guid.NewGuid());
        var firstBinding = await ReadBound(firstRecord);
        using var secondRecord = await Post(positiveClient, Route(secondSession),
            source with { ExpectedReferenceRevision = 1 }, Guid.NewGuid());
        var secondBinding = await ReadBound(secondRecord);
        using var positive = await PostIssue(positiveClient, firstSession, firstBinding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, positive.StatusCode);
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);

        await using var invalidApp = await Start(setup.Database.GetConnectionString()!,
            retentionProfile: Profile(policy) with { RawClasses = [retainedClass] });
        using var invalidClient = invalidApp.GetTestClient();
        using var rejectedProfile = await PostIssue(invalidClient, secondSession,
            secondBinding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, rejectedProfile.StatusCode);
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);

        await using (var sql = isolated.CreateDbContext())
        await using (var transaction = await sql.Database.BeginTransactionAsync())
        {
            await sql.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            var code = await sql.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_issue_retention_authority(
                 {Principal},{Client},{secondSession},{secondBinding.ConsentBindingId},{policy},1,
                 ARRAY[{retainedClass}]::text[],'synthetic-controller','synthetic-scope',
                 'synthetic-retention',1,'synthetic-class','synthetic-revoke','synthetic-purge',
                 'synthetic-hold',60,{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync();
            Assert.Equal("Denied", code);
            await transaction.CommitAsync();
        }
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);
    }

    [Fact]
    public async Task E01_DdlGraphAndAcl_DirectWritesAndHeadWithoutEventRollBack()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_graph_acl");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client, challengeBound: true);
        await Grant(setup, "SubjectConsentRecorder");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!, retentionProfile: Profile(policy));
        using var client = app.GetTestClient();
        using var recorded = await Post(client, Route(session), Record(), Guid.NewGuid());
        var binding = await ReadBound(recorded);
        using var issued = await PostIssue(client, session, binding.ConsentBindingId, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);
        var before = await ConsentGraph(isolated);
        var tables = new[]
        {
            "raw_source_consent_references", "raw_source_consent_reference_events",
            "raw_source_consent_bindings", "raw_source_retention_permits",
            "raw_source_retention_permit_classes"
        };
        await using var observer = isolated.CreateDbContext();
        foreach (var table in tables)
        {
            Assert.Equal(1, await observer.Database.SqlQuery<int>($"""
                SELECT count(*)::integer AS "Value" FROM pg_catalog.pg_trigger t
                 JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
                 JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                 WHERE n.nspname='tagekyc' AND c.relname={table}
                   AND t.tgname={"tr_a3_" + (table switch
                   {
                       "raw_source_consent_references" => "reference",
                       "raw_source_consent_reference_events" => "event",
                       "raw_source_consent_bindings" => "binding",
                       "raw_source_retention_permits" => "permit",
                       _ => "class"
                   }) + "_write"}
                """).SingleAsync());
            await using var denied = isolated.CreateDbContext();
            await using var tx = await denied.Database.BeginTransactionAsync();
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_capture_runtime_application");
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.Database.ExecuteSqlRawAsync(
                $"DELETE FROM tagekyc.{table} WHERE false"));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.Database.ExecuteSqlRawAsync(
                "UPDATE tagekyc.raw_source_consent_references SET \"CurrentRevision\"=\"CurrentRevision\"+1"));
            Assert.Equal("A3_AUTHORITY_WRITE_FORBIDDEN", error.MessageText);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await denied.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','E01-R',true)");
            await denied.Database.ExecuteSqlRawAsync(
                "UPDATE tagekyc.raw_source_consent_references SET \"CurrentRevision\"=\"CurrentRevision\"+1");
            var error = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("FK_a3_consent_reference_current", error.ConstraintName);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await denied.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            await denied.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','E01-R',true)");
            await denied.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_source_consent_reference_events
                SELECT "ConsentReferenceId",2,'Updated','source-v2',"ConsentTextVersion",
                 "ConsentTextContentHash","ValidFromUtc","ValidUntilUtc","RecordedByPrincipalId",
                 clock_timestamp(),'E01-R',gen_random_uuid(),"RequestFingerprint",NULL
                FROM tagekyc.raw_source_consent_reference_events WHERE "Revision"=1
                """);
            await denied.Database.ExecuteSqlRawAsync(
                "UPDATE tagekyc.raw_source_consent_references SET \"CurrentRevision\"=2");
            var error = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("A3_AUTHORITY_RECORD_BINDING_MISSING", error.MessageText);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await denied.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','R20-Issue',true)");
            await denied.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            await denied.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_source_retention_permits
                SELECT (pg_catalog.jsonb_populate_record(NULL::tagekyc.raw_source_retention_permits,
                 pg_catalog.to_jsonb(p) || pg_catalog.jsonb_build_object(
                 'RetentionAuthorityId',pg_catalog.gen_random_uuid(),
                 'IssueOperationId',pg_catalog.gen_random_uuid()))).* 
                FROM tagekyc.raw_source_retention_permits p LIMIT 1
                """);
            var error = await Assert.ThrowsAsync<PostgresException>(() => tx.CommitAsync());
            Assert.Equal("A3_RETENTION_PERMIT_CLASSES_INVALID", error.MessageText);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await denied.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            await denied.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','E01-R',true)");
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_source_consent_bindings
                SELECT pg_catalog.gen_random_uuid(),"ConsentReferenceId","ConsentReferenceRevision",
                 pg_catalog.gen_random_uuid(),"ClientApplicationId","VerificationSessionId","SubjectRef",
                 pg_catalog.gen_random_uuid(),"RequestFingerprint",clock_timestamp()
                FROM tagekyc.raw_source_consent_bindings LIMIT 1
                """));
            Assert.Equal("A3_AUTHORITY_ACTOR_MISMATCH", error.MessageText);
        }
        await using (var denied = isolated.CreateDbContext())
        await using (var tx = await denied.Database.BeginTransactionAsync())
        {
            await denied.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await denied.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            await denied.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','R20-Issue',true)");
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_source_retention_permits
                SELECT (pg_catalog.jsonb_populate_record(NULL::tagekyc.raw_source_retention_permits,
                 pg_catalog.to_jsonb(p) || pg_catalog.jsonb_build_object(
                 'RetentionAuthorityId',pg_catalog.gen_random_uuid(),
                 'IssueOperationId',pg_catalog.gen_random_uuid(),
                 'ConsentBindingId',pg_catalog.gen_random_uuid()))).* 
                FROM tagekyc.raw_source_retention_permits p LIMIT 1
                """));
            Assert.Equal("FK_a3_retention_permit_binding", error.ConstraintName);
        }
        Assert.Equal(before, await ConsentGraph(isolated));
        await AssertRetentionIssueCounts(isolated, 1, 2, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E01_CrossClientBindingMutationRejects(bool changeSession)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_cross_binding");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client);
        var foreignClient = Guid.NewGuid();
        var foreignSession = await Session(db, foreignClient);
        await Grant(db, "SubjectConsentRecorder");
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var first = await Post(client, Route(session), Record(), Guid.NewGuid());
        var bound = await ReadBound(first);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            await db.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.a3_authority_write','E01-R',true)");
            var failure = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO tagekyc.raw_source_consent_bindings
                    SELECT gen_random_uuid(),"ConsentReferenceId","ConsentReferenceRevision","PrincipalId",
                      {(changeSession ? Client : foreignClient)},{(changeSession ? foreignSession : session)},
                      "SubjectRef",gen_random_uuid(),"RequestFingerprint",clock_timestamp()
                    FROM tagekyc.raw_source_consent_bindings WHERE "ConsentBindingId"={bound.ConsentBindingId}
                    """);
                await tx.CommitAsync();
            });
            if (changeSession) Assert.Equal("A3_CONSENT_BINDING_LINEAGE_MISMATCH", failure.MessageText);
            else Assert.Equal("FK_a3_consent_binding_client", failure.ConstraintName);
        }
        await AssertCounts(isolated, 1, 1, 1);
    }

    [Fact]
    public async Task E01_ReferenceSurvivesCredentialRevocationAndReplacement()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_credential_lifetime");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext();
        var session = await Session(db, Client);
        await Grant(db, "SubjectConsentRecorder");
        var pepper = new ApiKeyStorePepper(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        // This proof owns credential lifecycle, not random-key generation. Use
        // fixed synthetic material; provisioning/hash storage/authentication run unchanged.
        var provisioning = new ApiKeyProvisioningService(db, pepper, new LocalDevRuntimePolicySource(), new SyntheticKeys());
        var command = new ApiKeyProvisioningCommand(Client, AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create" }, PrincipalId: Principal);
        var original = await provisioning.ProvisionAsync(command);
        var replacement = await provisioning.ProvisionAsync(command);
        Assert.NotEqual(original.ApiKeyId, replacement.ApiKeyId);
        Assert.Equal(2, await db.ApiKeys.CountAsync(k => k.ClientApplicationId == Client));
        await using var app = await Start(db.Database.GetConnectionString()!, pepper);
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-TagEkyc-Api-Key", original.PresentedKey);
        var request = Record(); var operation = Guid.NewGuid();
        using var first = await Post(client, Route(session), request, operation);
        var bound = await ReadBound(first);
        var before = await db.Database.SqlQueryRaw<string>("SELECT row_to_json(r)::text AS \"Value\" FROM tagekyc.raw_source_consent_references r").SingleAsync();
        var oldKey = await db.ApiKeys.SingleAsync(k => k.ApiKeyId == original.ApiKeyId);
        oldKey.CredentialStatus = "Revoked";
        await db.SaveChangesAsync();
        using var denied = await Post(client, Route(session), request, operation);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        client.DefaultRequestHeaders.Remove("X-TagEkyc-Api-Key");
        client.DefaultRequestHeaders.Add("X-TagEkyc-Api-Key", replacement.PresentedKey);
        using var replay = await Post(client, Route(session), request, operation);
        Assert.Equal(bound, await ReadBound(replay));
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(before, await observer.Database.SqlQueryRaw<string>("SELECT row_to_json(r)::text AS \"Value\" FROM tagekyc.raw_source_consent_references r").SingleAsync());
        await AssertCounts(isolated, 1, 1, 1);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(pepper.Value);
    }

    // Use the registered EF chain; no direct UpOperations fallback can hide
    // missing migration registration or an ordinary fixture/schema mismatch.
    internal static async Task Prepare(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await using var db = isolated.CreateDbContext();
        Assert.Contains(Tip88C1C6BA3MigrationTests.MigrationId, db.Database.GetMigrations());
        await db.Database.MigrateAsync();
        Assert.Equal("20260925090000_RawExportAssemblyRetainedModeWorkSource",
            (await db.Database.GetAppliedMigrationsAsync()).Last());
    }

    [Fact]
    public async Task E01_ProfileHorizonFrozen_RealIssuerAndResolver()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_permit");
        await Prepare(isolated);
        await using var db = isolated.CreateDbContext(); var session = await Session(db, Client);
        await Grant(db, "SubjectConsentRecorder");
        var policy = await ApprovedRetentionPolicy(db);
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var client = app.GetTestClient(); var record = Record();
        using var recorded = await Post(client, Route(session), record, Guid.NewGuid());
        var binding = await ReadBound(recorded); var operation = Guid.NewGuid();
        Guid authority;
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            authority = await db.Database.SqlQuery<Guid>($"""
                SELECT retention_authority_id AS "Value" FROM tagekyc.raw_source_issue_retention_authority(
                {Principal},{Client},{session},{binding.ConsentBindingId},{policy},1,
                ARRAY['ChipDg2Portrait','LiveSelfieImage']::text[],'synthetic-controller','synthetic-scope',
                'synthetic-retention',1,'synthetic-class','synthetic-revoke','synthetic-purge','synthetic-hold',60,
                {operation},{new byte[32]}) WHERE result_code='Granted'
                """).SingleAsync();
            await transaction.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        Assert.NotEqual(Guid.Empty, authority);
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync());
        Assert.Equal(new[] { "ChipDg2Portrait", "LiveSelfieImage" }, await observer.Database.SqlQueryRaw<string>(
            "SELECT \"RawClass\" AS \"Value\" FROM tagekyc.raw_source_retention_permit_classes ORDER BY \"RawClass\"").ToArrayAsync());
        Assert.True(await observer.Database.SqlQueryRaw<bool>("""
            SELECT "ExpiresAtUtc" <= "IssuedAtUtc" + interval '60 seconds' AND "ExpiresAtUtc">"IssuedAtUtc" AS "Value"
            FROM tagekyc.raw_source_retention_permits
            """).SingleAsync());
        foreach (var rawClass in new[] { "ChipDg2Portrait", "LiveSelfieImage" })
        {
            await using var transaction = await observer.Database.BeginTransactionAsync();
            await PrelockRetentionRead(observer, session, policy);
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT is_effective AS "Value" FROM tagekyc.raw_source_resolve_retention_authority(
                {authority},1,{Principal},{Client},{session},{rawClass},clock_timestamp())
                """).SingleAsync());
            await transaction.CommitAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E01_B2WithoutMatchingReference_PreservesLandedWithdrawal(bool differentSubject)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_b2_no_reference");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        await Grant(setup, "SubjectConsentRecorder"); await Grant(setup, "SubjectConsentWithdrawer");
        var policy = await ApprovedRetentionPolicy(setup);
        if (differentSubject)
        {
            var other = await Session(setup, Client, "different-subject");
            await using var app = await Start(setup.Database.GetConnectionString()!);
            using var client = app.GetTestClient();
            using var recorded = await Post(client, Route(other), Record(), Guid.NewGuid());
            await ReadBound(recorded);
        }
        var session = await Session(setup, Client);
        await new EfVerificationSessionRepository(setup).SetStateAsync(session, VerificationSessionState.Completed);
        var repository = new EfRawExportSubjectConsentRepository(setup);
        await repository.RecordSubjectConsentGrantedAsync(new(Principal, session, policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null, DateTimeOffset.UtcNow.AddMinutes(5)));
        var result = await repository.RecordSubjectConsentWithdrawnAsync(new(Principal, session, policy, 1, 1, 1, null));
        Assert.Equal(RawExportSubjectConsentCause.Withdrawn, result.Cause);
        await AssertCounts(isolated, differentSubject ? 1 : 0, differentSubject ? 1 : 0, differentSubject ? 1 : 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task E01_B2WithdrawalAndDirectWithdrawal_SerializeWithoutDuplicateProjection(bool b2First)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_b2_withdraw_race");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client);
        await Grant(setup, "SubjectConsentRecorder"); await Grant(setup, "SubjectConsentWithdrawer");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var recorded = await Post(client, Route(session), Record(), Guid.NewGuid());
        var binding = await ReadBound(recorded);
        await new EfVerificationSessionRepository(setup).SetStateAsync(session, VerificationSessionState.Completed);
        // The disposable database clones current fixture history. Keep a positive
        // unrelated-history control even when this test is run on its own.
        var otherSession = await Session(setup, Client);
        await new EfVerificationSessionRepository(setup).SetStateAsync(otherSession, VerificationSessionState.Completed);
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(new(Principal, otherSession, policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null, DateTimeOffset.UtcNow.AddMinutes(5)));
        var beforeConsentEvents = await setup.Database.SqlQueryRaw<string>(
            "SELECT to_jsonb(e)::text AS \"Value\" FROM tagekyc.raw_export_subject_consent_events e").ToListAsync();
        Assert.NotEmpty(beforeConsentEvents);
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(new(Principal, session, policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null, DateTimeOffset.UtcNow.AddMinutes(5)));
        await using var winner = isolated.CreateDbContext();
        await using var winnerTx = await winner.Database.BeginTransactionAsync();
        await winner.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
        Assert.Equal("Withdrawn", await WithdrawOnConnection(winner, session, policy, binding.ConsentReferenceId, b2First));
        var winnerPid = await winner.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string> Contender()
        {
            await using var db = isolated.CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            pid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            var code = await WithdrawOnConnection(db, session, policy, binding.ConsentReferenceId, !b2First);
            await transaction.CommitAsync(); return code;
        }
        var running = Contender();
        await using var observer = isolated.CreateDbContext();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for (var i = 0; i < 100 && !waiting; i++)
            {
                waiting = await observer.Database.SqlQuery<bool>($"""
                    SELECT {winnerPid}=ANY(pg_catalog.pg_blocking_pids({backend}))
                     AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks WHERE pid={backend}
                       AND locktype='advisory' AND mode='ExclusiveLock' AND NOT granted) AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(25);
            }
            Assert.True(waiting, "Actual withdrawal contenders must serialize on the shared reference domain.");
            Assert.Equal(1L, await observer.Database.SqlQueryRaw<long>("SELECT \"CurrentRevision\" AS \"Value\" FROM tagekyc.raw_source_consent_references").SingleAsync());
            await winnerTx.CommitAsync();
            Assert.Equal(b2First ? "Conflict" : "Withdrawn", await running);
            Assert.Equal(2L, await observer.Database.SqlQueryRaw<long>("SELECT \"CurrentRevision\" AS \"Value\" FROM tagekyc.raw_source_consent_references").SingleAsync());
            Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_consent_reference_events").SingleAsync());
            var afterConsentEvents = await observer.Database.SqlQueryRaw<string>(
                "SELECT to_jsonb(e)::text AS \"Value\" FROM tagekyc.raw_export_subject_consent_events e").ToListAsync();
            Assert.Equal(beforeConsentEvents.Count + 2, afterConsentEvents.Count);
            Assert.All(beforeConsentEvents, row => Assert.Contains(row, afterConsentEvents));
            Assert.Equal(2, await observer.Database.SqlQuery<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_subject_consent_events WHERE \"VerificationSessionId\"={session}").SingleAsync());
            Assert.Equal(b2First ? "B2-Withdrawal" : "E01-W", await observer.Database.SqlQueryRaw<string>("SELECT \"OperationDomain\" AS \"Value\" FROM tagekyc.raw_source_consent_reference_events WHERE \"EventType\"='Withdrawn'").SingleAsync());
        }
        finally
        {
            if (winnerTx.GetDbTransaction().Connection is not null) await winnerTx.RollbackAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    private static async Task<string> WithdrawOnConnection(TagEkycDbContext db, Guid session, Guid policy, Guid reference, bool b2)
    {
        if (b2)
        {
            Assert.Equal(2, await db.Database.SqlQuery<int>($"""
                SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                 {session},{policy},1,1,1,'synthetic-b2-withdraw',NULL::text) AS "Value"
                """).SingleAsync());
            return "Withdrawn";
        }
        return await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
             {Principal},{Client},{reference},1,'source-v1','synthetic-e01-withdraw',{Guid.NewGuid()},{new byte[32]})
            """).SingleAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("decision\r\nverbatim")]
    [InlineData("wide-unicode")]
    public async Task E01_B2WithdrawalSynchronizes_PreservesOriginalDecisionAndInvalidatesPermit(string? decision)
    {
        if (decision == "wide-unicode") decision = new string('é', 256);
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_b2_projection");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var session = await Session(setup, Client);
        await Grant(setup, "SubjectConsentRecorder");
        await Grant(setup, "SubjectConsentWithdrawer");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var recorded = await Post(client, Route(session), Record(), Guid.NewGuid());
        var binding = await ReadBound(recorded);
        Guid permit;
        await using (var transaction = await setup.Database.BeginTransactionAsync())
        {
            await setup.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
            permit = await setup.Database.SqlQuery<Guid>($"""
                SELECT retention_authority_id AS "Value" FROM tagekyc.raw_source_issue_retention_authority(
                {Principal},{Client},{session},{binding.ConsentBindingId},{policy},1,
                ARRAY['ChipDg2Portrait','LiveSelfieImage']::text[],'synthetic-controller','synthetic-scope',
                'synthetic-retention',1,'synthetic-class','synthetic-revoke','synthetic-purge','synthetic-hold',60,
                {Guid.NewGuid()},{new byte[32]}) WHERE result_code='Granted'
                """).SingleAsync();
            await transaction.CommitAsync();
        }
        await new EfVerificationSessionRepository(setup).SetStateAsync(session, VerificationSessionState.Completed);
        var repository = new EfRawExportSubjectConsentRepository(setup);
        await repository.RecordSubjectConsentGrantedAsync(new(Principal, session, policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null, DateTimeOffset.UtcNow.AddMinutes(5)));
        var result = await repository.RecordSubjectConsentWithdrawnAsync(new(Principal, session, policy, 1, 1, 1,
            decision, "caller-observation-is-not-the-reference-source"));
        Assert.Equal(RawExportSubjectConsentCause.Withdrawn, result.Cause);
        Assert.Equal(decision, result.DecisionRef);
        await using var observer = isolated.CreateDbContext();
        var eventId = await observer.Database.SqlQueryRaw<Guid>("""
            SELECT "SubjectConsentRecordId" AS "Value" FROM tagekyc.raw_export_subject_consent_events WHERE "EventType"='Withdrawn'
            """).SingleAsync();
        Assert.Equal(2L, await observer.Database.SqlQueryRaw<long>("SELECT \"CurrentRevision\" AS \"Value\" FROM tagekyc.raw_source_consent_references").SingleAsync());
        Assert.Equal(eventId, await observer.Database.SqlQueryRaw<Guid>("SELECT \"IdempotencyKey\" AS \"Value\" FROM tagekyc.raw_source_consent_reference_events WHERE \"EventType\"='Withdrawn'").SingleAsync());
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(eventId.ToByteArray(bigEndian: true)),
            await observer.Database.SqlQueryRaw<byte[]>("SELECT \"RequestFingerprint\" AS \"Value\" FROM tagekyc.raw_source_consent_reference_events WHERE \"EventType\"='Withdrawn'").SingleAsync());
        Assert.Equal(decision, await observer.Database.SqlQueryRaw<string?>("SELECT \"DecisionRef\" AS \"Value\" FROM tagekyc.raw_source_consent_reference_events WHERE \"EventType\"='Withdrawn'").SingleAsync());
        Assert.Equal("B2-Withdrawal", await observer.Database.SqlQueryRaw<string>("SELECT \"OperationDomain\" AS \"Value\" FROM tagekyc.raw_source_consent_reference_events WHERE \"EventType\"='Withdrawn'").SingleAsync());
        await using var read = await observer.Database.BeginTransactionAsync();
        await PrelockRetentionRead(observer, session, policy);
        Assert.Equal(0, await observer.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_source_resolve_retention_authority(
             {permit},1,{Principal},{Client},{session},'ChipDg2Portrait',clock_timestamp())
            """).SingleAsync());
        await read.CommitAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_R20_RetainedReplace_RechecksExpiryAfterReferenceWait(bool expireDuringWait)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_replace_clock");
        await Prepare(isolated);
        await using var setup = isolated.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var value = VerificationSession.Create(Client, "synthetic-subject", VerificationProfile.ChallengeBoundEkycProfile,
            "synthetic-r20", [RequiredCheckType.DocumentNfc], now.AddHours(1), now, challenge: "synthetic-challenge");
        await new EfVerificationSessionRepository(setup).AddAsync(value);
        var session = value.Id;
        await Grant(setup, "SubjectConsentRecorder");
        var policy = await ApprovedRetentionPolicy(setup);
        await using var app = await Start(setup.Database.GetConnectionString()!);
        using var client = app.GetTestClient();
        using var recorded = await Post(client, Route(session), Record(), Guid.NewGuid());
        var binding = await ReadBound(recorded);
        var profile = JsonSerializer.Serialize(new {
            PolicyId = policy, PolicyVersion = 1, RawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
            ControllerIdentity = "synthetic-controller", StableDataScopeId = "synthetic-scope",
            RetentionPolicyId = "synthetic-retention", RetentionPolicyVersion = 1, RetentionClass = "synthetic-class",
            RevocationPolicyId = "synthetic-revoke", PurgePolicyId = "synthetic-purge",
            LegalHoldPolicyId = "synthetic-hold", MaximumRetentionSeconds = 60 });
        var original = Guid.NewGuid();
        Assert.Equal("CREATED", await RunR20(isolated, session, "Issue", null, original, Guid.NewGuid(),
            binding.ConsentBindingId, profile));
        // Synthetic fixture horizon only; keep real guards enabled and permit/session live.
        await setup.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tagekyc.capture_capabilities SET "ExpiresAtUtc"=clock_timestamp()+interval '3 seconds'
            WHERE "CaptureCapabilityId"={original}
            """);
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended(
              'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
            """);
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var successor = Guid.NewGuid(); var operation = Guid.NewGuid();
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = RunR20(isolated, session, "Replace", original, successor, operation, null, null, pid);
        await using var observer = isolated.CreateDbContext();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for (var i = 0; i < 100 && !waiting; i++)
            {
                waiting = await observer.Database.SqlQuery<bool>($"""
                    SELECT {blockerPid}=ANY(pg_catalog.pg_blocking_pids({backend}))
                     AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks WHERE pid={backend}
                       AND locktype='advisory' AND mode='ShareLock' AND NOT granted) AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(25);
            }
            Assert.True(waiting, "Actual R20 must reach and wait for the shared reference lock.");
            Assert.False(running.IsCompleted);
            if (expireDuringWait)
            {
                var expired = false;
                for (var i = 0; i < 200 && !expired; i++)
                {
                    expired = await observer.Database.SqlQuery<bool>($"""
                        SELECT "ExpiresAtUtc"<=clock_timestamp() AS "Value" FROM tagekyc.capture_capabilities
                        WHERE "CaptureCapabilityId"={original}
                        """).SingleAsync();
                    if (!expired) await Task.Delay(25);
                }
                Assert.True(expired);
            }
            Assert.True(await observer.Database.SqlQueryRaw<bool>("SELECT \"ExpiresAtUtc\">clock_timestamp() AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync());
            await hold.CommitAsync();
            Assert.Equal(expireDuringWait ? "TERMINALIZED_EXPIRED_AND_DENIED" : "CREATED", await running);
            Assert.Equal(expireDuringWait ? "Expired" : "Revoked", await observer.Database.SqlQuery<string>($"""
                SELECT "State" AS "Value" FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"={original}
                """).SingleAsync());
            Assert.Equal(expireDuringWait ? 1 : 2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
            Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_operations").SingleAsync());
            Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_events").SingleAsync());
            Assert.Equal(expireDuringWait ? "TERMINALIZED_EXPIRED_AND_DENIED" : "EXISTING_MATCH_SECRET_UNAVAILABLE",
                await RunR20(isolated, session, "Replace", original, Guid.NewGuid(), operation, null, null));
            Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_events").SingleAsync());
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }

    private static async Task<string> RunR20(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Guid session, string action, Guid? current, Guid next, Guid operation, Guid? binding, string? profile,
        TaskCompletionSource<int>? pid = null)
    {
        await using var db = isolated.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (pid is not null) pid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Principal.ToString("D")},true)");
        long? revision = current is null ? null : 1;
        var result = await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
             {Client},{session},{action},CAST({current} AS uuid),CAST({revision} AS bigint),{operation},{next},
             {next.ToString("N")[..12]},{new byte[32]},1,{new byte[32]},clock_timestamp(),{Principal},
             CAST({binding} AS uuid),CAST({profile} AS jsonb))
            """).SingleAsync();
        await transaction.CommitAsync(); return result;
    }

    internal static async Task PrelockRetentionRead(TagEkycDbContext db, Guid session, Guid policy)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM tagekyc.verification_sessions WHERE \"Id\"={session} FOR UPDATE");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock_shared(hashtextextended(
             'tip88c1:a3:consent-reference:'||{Client.ToString("D")}||':synthetic-existing-consent',0))
            """);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'))");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||{policy.ToString("D")}||':1'))");
        foreach (var requirement in await db.Database.SqlQuery<string>($"SELECT \"RequirementType\" AS \"Value\" FROM tagekyc.raw_export_policy_requirements WHERE \"PolicyId\"={policy} AND \"PolicyVersion\"=1 AND \"RequirementType\"<>'ConsentArtifact' ORDER BY \"RequirementType\" COLLATE \"C\"").ToArrayAsync())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||{policy.ToString("D")}||':1:'||{requirement}))");
    }

    internal static async Task<Guid> ApprovedRetentionPolicy(TagEkycDbContext db, bool crossBorder = false)
    {
        // Use the landed B1 catalogue seeding pattern with its real constraints;
        // grant, fulfillment and activation all use production control functions.
        var policy = Guid.NewGuid();
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionPurposeCode","ConsentRequirement","ControllerRole","ControllerEntityRef",
                 "ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RetentionProfileRef","RequirementRuleSetId","RequirementRuleSetVersion","PermitTtlSeconds","CreatedAt")
                VALUES ({policy},1,'EncryptedRawVaultRetained','synthetic-purpose','NO_RETAIN','Required',
                 'Processor','synthetic-controller','VN',{(crossBorder ? "US" : "VN")},'VN','synthetic-retention','RAW_EXPORT_REQUIREMENTS',1,300,transaction_timestamp())
                """);
            foreach (var rawClass in new[] { "ChipDg2Portrait", "LiveSelfieImage" })
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO tagekyc.raw_export_policy_allowed_classes VALUES({policy},1,{rawClass},transaction_timestamp())");
            foreach (var requirement in new[] { "LegalApproval", "RetentionSchedule", "Dpia", "ConsentArtifact" }.Concat(crossBorder ? new[] { "CrossBorderAssessment" } : Array.Empty<string>()))
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO tagekyc.raw_export_policy_requirements VALUES({policy},1,{requirement},transaction_timestamp())");
            await transaction.CommitAsync();
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_policy_closures
            ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
            VALUES({policy},1,'CatalogApproved',transaction_timestamp(),'synthetic-catalog-principal','synthetic-catalog-decision')
            """);
        foreach (var authority in new[] { "RecorderAuthorityAdmin", "ActivationAuthority" })
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT tagekyc.raw_export_bootstrap_global_authority({Admin},{authority},'synthetic-policy-root')");
        var repository = new EfRawExportControlPlaneRepository(db);
        foreach (var requirement in new[] { RawExportRequirementType.LegalApproval, RawExportRequirementType.RetentionSchedule, RawExportRequirementType.Dpia }.Concat(crossBorder ? new[] { RawExportRequirementType.CrossBorderAssessment } : Array.Empty<RawExportRequirementType>()))
        {
            await repository.GrantControlAuthorityAsync(new(Admin, Principal, RawExportAuthorityType.FulfillmentRecorder,
                RawExportAuthorityScopeType.Policy, policy, requirement, 0, "synthetic-policy-recorder"));
            await repository.AcceptFulfillmentAsync(new(Principal, policy, 1, requirement, 0, null,
                "synthetic-approved-artifact", "v1", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1), "synthetic-fulfillment"));
        }
        await repository.ActivatePolicyAsync(new(Admin, policy, 1, 0, "synthetic-policy-activate"));
        return policy;
    }

    private static async Task<Guid> Session(TagEkycDbContext db, Guid client,
        string subjectRef = "synthetic-subject", bool challengeBound = false)
    {
        var now = DateTimeOffset.UtcNow;
        var value = VerificationSession.Create(client, subjectRef,
            challengeBound ? VerificationProfile.ChallengeBoundEkycProfile : VerificationProfile.StandardEkycProfile,
            "synthetic-a3", [RequiredCheckType.DocumentNfc], now.AddHours(1), now,
            challenge: challengeBound ? "synthetic-challenge" : null);
        await new EfVerificationSessionRepository(db).AddAsync(value);
        return value.Id;
    }

    internal static async Task Grant(TagEkycDbContext db, string kind, DateTimeOffset? validUntilUtc = null,
        Guid? clientApplicationId = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT tagekyc.raw_export_bootstrap_global_authority({Admin},'RecorderAuthorityAdmin','synthetic-a3-root')");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Admin.ToString("D")},true)");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT tagekyc.raw_export_append_subject_consent_authority({Principal},{clientApplicationId ?? Client},{kind},0,'Granted',NULL,'synthetic-a3-authority',CAST({validUntilUtc} AS timestamptz))");
        await tx.CommitAsync();
    }

    private static E01RecordRequest Record()
    {
        var now = DateTimeOffset.UtcNow;
        now = new DateTimeOffset(now.Ticks - now.Ticks % 10, TimeSpan.Zero);
        return new("synthetic-existing-consent", "source-v1", 0, "existing-text-v1", "existing-source-hash",
            now.AddMinutes(-1), now.AddMinutes(10));
    }

    private static DateTimeOffset RoundedUtc(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);

    private static async Task WaitForReferenceLock(TagEkycDbContext observer, int blockerPid, string function, string mode)
        => await WaitForReferenceContenders(observer, blockerPid, function, mode, 1);

    private static async Task WaitForReferenceContenders(TagEkycDbContext observer, int blockerPid,
        string function, string mode, int expected)
    {
        for (var i = 0; i < 160; i++)
        {
            if (await CountReferenceContenders(observer, blockerPid, function, mode) >= expected) return;
            await Task.Delay(25);
        }
        Assert.Fail($"Expected {expected} real {function} contenders on the intended PostgreSQL advisory lock.");
    }

    private static async Task<bool> WaitForLockOrCompletion(TagEkycDbContext observer, int blockerPid,
        string function, string mode, Task<HttpResponseMessage> running)
    {
        for (var i = 0; i < 160; i++)
        {
            if (await CountReferenceContenders(observer, blockerPid, function, mode) > 0) return true;
            if (running.IsCompleted) return false;
            await Task.Delay(25);
        }
        return false;
    }

    private static Task<int> CountReferenceContenders(TagEkycDbContext observer, int blockerPid,
        string function, string mode) => observer.Database.SqlQuery<int>($"""
                SELECT count(*)::integer AS "Value" FROM pg_catalog.pg_stat_activity a
                 JOIN pg_catalog.pg_locks l ON l.pid=a.pid
                 WHERE a.datname=current_database() AND a.pid<>pg_backend_pid()
                   AND a.query LIKE {'%' + function + '%'}
                   AND {blockerPid}=ANY(pg_catalog.pg_blocking_pids(a.pid))
                   AND l.locktype='advisory' AND l.mode={mode} AND NOT l.granted
                """).SingleAsync();

    private static async Task WaitUntilDatabaseClock(TagEkycDbContext observer, DateTimeOffset expiry)
    {
        for (var i = 0; i < 300; i++)
        {
            if (await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp() >= {expiry} AS \"Value\"").SingleAsync()) return;
            await Task.Delay(25);
        }
        Assert.Fail("PostgreSQL clock did not cross the tested finite horizon.");
    }

    private static async Task<string[]> ConsentGraph(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await using var observer = isolated.CreateDbContext();
        var result = new List<string>();
        foreach (var (table, order) in new[]
        {
            ("raw_source_consent_references", "\"ConsentReferenceId\""),
            ("raw_source_consent_reference_events", "\"ConsentReferenceId\",\"Revision\""),
            ("raw_source_consent_bindings", "\"ConsentBindingId\"")
        })
        {
            result.Add(table);
            result.AddRange(await observer.Database.SqlQueryRaw<string>(
                $"SELECT to_jsonb(r)::text AS \"Value\" FROM tagekyc.{table} r ORDER BY {order}").ToArrayAsync());
        }
        return result.ToArray();
    }

    private static async Task<string[]> ConsentGraphRows(PostgresPersistenceFixture.DisposableCurrentDatabase isolated) =>
        (await ConsentGraph(isolated)).Where(x => x.StartsWith('{')).ToArray();

    private static string Route(Guid session) => $"/api/ekyc/verification-sessions/{session:N}/source-consent-reference";
    private static Task<HttpResponseMessage> Post<T>(HttpClient client, string route, T value, Guid key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Idempotency-Key", key.ToString("N"));
        request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(value, Wire));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return client.SendAsync(request);
    }
    private static Task<HttpResponseMessage> PostIssue(HttpClient client, Guid session, Guid binding, Guid key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/ekyc/verification-sessions/{session:N}/capture-capabilities");
        request.Headers.Add("Idempotency-Key", key.ToString("N"));
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(
            $"{{\"action\":\"Issue\",\"consentBindingId\":\"{binding:N}\"}}"));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return client.SendAsync(request);
    }
    private static async Task AssertRetentionIssueCounts(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        int permits, int classes, int capabilities)
    {
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(permits, await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_retention_permits").SingleAsync());
        Assert.Equal(classes, await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_retention_permit_classes").SingleAsync());
        Assert.Equal(capabilities, await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
    }
    private static async Task AssertCounts(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, int references, int events, int bindings)
    {
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(references, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_consent_references").SingleAsync());
        Assert.Equal(events, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_consent_reference_events").SingleAsync());
        Assert.Equal(bindings, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_source_consent_bindings").SingleAsync());
    }
    private static async Task<WebApplication> Start(string connection, ApiKeyStorePepper? realKeyPepper = null,
        RawSourceRetentionProfile? retentionProfile = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        A1SyntheticDbFailureInterceptor.Clear();
        builder.Services.AddSingleton<ICaptureRuntimeDbContextFactory>(new DiagnosticContexts(connection));
        builder.Services.AddScoped<IRawSourceRetentionGateway, RawSourceRetentionGateway>();
        builder.Services.AddScoped<IRawSourceConsentService, RawSourceConsentApplicationService>();
        if (realKeyPepper is null) builder.Services.AddSingleton<IApiKeyAuthenticator, Identity>();
        else
        {
            builder.Services.AddDbContext<TagEkycDbContext>(options => options.UseNpgsql(connection));
            builder.Services.AddSingleton(realKeyPepper);
            builder.Services.AddScoped<IApiKeyStore, PostgresHashedApiKeyStore>();
            builder.Services.AddSingleton<ILocalDevClientPolicyProvider, LocalDevRuntimePolicySource>();
            builder.Services.AddScoped<LocalDevApiKeyValidator>();
            builder.Services.AddScoped<IApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
        }
        if (retentionProfile is not null)
        {
            builder.Services.AddSingleton<IRawSourceRetentionProfileProvider>(new RawSourceRetentionProfileValidator(
                new ConfigurationBuilder().AddInMemoryCollection(ProfileConfiguration(retentionProfile)).Build()));
            builder.Services.AddSingleton<ICaptureRuntimeVerifierPepperSource, SyntheticVerifierPepper>();
            builder.Services.AddSingleton<ICaptureRuntimeAppendGateway, NoRuntimeAppend>();
            builder.Services.AddScoped<ICaptureRuntimeExecutionGateway, CaptureRuntimeExecutionPersistenceBoundary>();
            builder.Services.AddScoped<ICaptureRuntimeExecutionService, CaptureRuntimeExecutionApplicationService>();
        }
        var app = builder.Build();
        app.MapRawSourceConsentEndpoints();
        if (retentionProfile is not null) app.MapCaptureRuntimeExecutionEndpoints();
        await app.StartAsync();
        return app;
    }
    private static RawSourceRetentionProfile Profile(Guid policy) => new(Client, policy, 1,
        ["ChipDg2Portrait", "LiveSelfieImage"], "synthetic-controller", "synthetic-scope",
        "synthetic-retention", 1, "synthetic-class", "synthetic-revoke", "synthetic-purge",
        "synthetic-hold", 60);
    private static Dictionary<string, string?> ProfileConfiguration(RawSourceRetentionProfile profile)
    {
        const string root = "RawSourceRetentionProfiles:Entries:0:";
        var values = new Dictionary<string, string?>
        {
            [root + "ClientApplicationId"] = profile.ClientApplicationId.ToString("D"),
            [root + "PolicyId"] = profile.PolicyId.ToString("D"),
            [root + "PolicyVersion"] = profile.PolicyVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [root + "ControllerIdentity"] = profile.ControllerIdentity,
            [root + "StableDataScopeId"] = profile.StableDataScopeId,
            [root + "RetentionPolicyId"] = profile.RetentionPolicyId,
            [root + "RetentionPolicyVersion"] = profile.RetentionPolicyVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [root + "RetentionClass"] = profile.RetentionClass,
            [root + "RevocationPolicyId"] = profile.RevocationPolicyId,
            [root + "PurgePolicyId"] = profile.PurgePolicyId,
            [root + "LegalHoldPolicyId"] = profile.LegalHoldPolicyId,
            [root + "MaximumRetentionSeconds"] = profile.MaximumRetentionSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        for (var i = 0; i < profile.RawClasses.Length; i++)
            values[root + "RawClasses:" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = profile.RawClasses[i];
        return values;
    }
    private sealed class SyntheticVerifierPepper : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 1;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version,
            CaptureRuntimeVerifierPepperDomain domain, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(
                version == 1 ? new SyntheticVerifierPepperLease(domain) : null);
    }
    private sealed class SyntheticVerifierPepperLease(CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        public int Version => 1;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key { get; } = new byte[32];
        public void Dispose() { }
    }
    private sealed class NoRuntimeAppend : ICaptureRuntimeAppendGateway
    {
        public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
            AuthenticatedCaptureRuntimeContext actor, Guid bindingId, CaptureRuntimeCaptureArtifactRequest request,
            Guid idempotencyKey, CancellationToken cancellationToken) => throw new InvalidOperationException("R20_ONLY");
        public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
            AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId, CaptureRuntimeEvidenceResultRequest request,
            Guid idempotencyKey, CancellationToken cancellationToken) => throw new InvalidOperationException("R20_ONLY");
    }
    private sealed class SyntheticKeys : IManagedApiKeyGenerator
    {
        private int sequence;
        public ManagedApiKeyMaterial Generate()
        {
            var prefix = (++sequence).ToString("D16", System.Globalization.CultureInfo.InvariantCulture);
            var material = new ManagedApiKeyMaterial($"tek_{prefix}_{new string('A', 43)}", prefix);
            Assert.Equal(prefix, ManagedApiKeyParser.Parse(material.PresentedKey)?.Prefix);
            return material;
        }
    }

    private sealed class Identity : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext context,
            string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(Guid.NewGuid(), Client,
                "synthetic", AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string>(), PrincipalId: Principal)));
    }
    private sealed class DiagnosticContexts(string connection) : ICaptureRuntimeDbContextFactory
    {
        public ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>()
                .UseNpgsql(connection).AddInterceptors(new A1SyntheticDbFailureInterceptor(),
                    new A1SyntheticTransactionFailureInterceptor()).Options));
    }
    private sealed class UtcTimestampWriter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    }
    private static async Task<E01BoundResponse> ReadBound(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        var reference = root.GetProperty("consentReferenceId").GetString()!;
        var binding = root.GetProperty("consentBindingId").GetString()!;
        var referenceId = Guid.ParseExact(reference, "N");
        var bindingId = Guid.ParseExact(binding, "N");
        Assert.Equal(referenceId.ToString("N"), reference);
        Assert.Equal(bindingId.ToString("N"), binding);
        return new(referenceId, root.GetProperty("consentReferenceRevision").GetInt64(), bindingId);
    }
}
