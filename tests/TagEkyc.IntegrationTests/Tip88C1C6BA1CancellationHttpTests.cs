using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1CancellationHttpTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid Session = Guid.Parse("90000000-0000-4000-8000-000000000001");
    private static readonly Guid Client = Guid.Parse("91000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task R26_SqlBoundary_CommitsBoundCancellation()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_sql_commit");
        await using var db = isolated.CreateDbContext();
        await Prepare(db);
        await using var tx = await db.Database.BeginTransactionAsync();
        var result = await db.Database.SqlQueryRaw<string>("""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_cancel_session_with_capability(
            '91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001',
            'ClientRequested','sql-request','sql-correlation',now(),'clientprefix',gen_random_uuid())
            """).SingleAsync();
        Assert.Equal("APPLIED", result);
        await db.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE");
        await tx.CommitAsync();
        await AssertHistory(isolated, Session, "ClientRequested", "sql-request", "sql-correlation");
    }

    [Fact]
    public async Task R26_NoHeader_InternalFingerprint_ExactReplay_ChangedMetadataAndHeadersKeepFirstWinner()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_replay");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        var first = await Cancel(http, Session, new("first_reason", "request-a", "correlation-a"));
        await AssertHistory(isolated, Session, "first_reason", "request-a", "correlation-a");
        var exact = await Cancel(http, Session, new("first_reason", "request-a", "correlation-a"));
        Assert.Equal(first, exact);
        await AssertHistory(isolated, Session, "first_reason", "request-a", "correlation-a");
        var changed = await Cancel(http, Session, new("second_reason", "request-b", "correlation-b"), "not-a-uuid");
        Assert.Equal(first, changed);
        var changedHeader = await Cancel(http, Session, new("third_reason", "request-c", "correlation-c"), Guid.NewGuid().ToString("N"));
        Assert.Equal(first, changedHeader);
        var oversizedIgnoredMetadata = await Cancel(http, Session,
            new("still_valid_reason", new string('r', 129), new string('c', 129)));
        Assert.Equal(first, oversizedIgnoredMetadata);
        await AssertHistory(isolated, Session, "first_reason", "request-a", "correlation-a");
        await using var observer = isolated.CreateDbContext();
        var key = await observer.Database.SqlQueryRaw<Guid>($"""
            SELECT "IdempotencyKey" AS "Value" FROM tagekyc.capture_capability_operations
            WHERE "VerificationSessionId"='{Session}' AND "OperationKind"='Cancel'
            """).SingleAsync();
        Assert.Equal(Session, key);
        var actual = await observer.Database.SqlQueryRaw<byte[]>($"""
            SELECT "RequestFingerprint" AS "Value" FROM tagekyc.capture_capability_operations
            WHERE "VerificationSessionId"='{Session}' AND "OperationKind"='Cancel'
            """).SingleAsync();
        // Independent .NET recomputation uses RFC/network-order UUID bytes, not
        // Guid.ToByteArray's little-endian layout and not the SQL digest helper.
        var preimage = Encoding.UTF8.GetBytes("TAG-EKYC-A1-R26-CANCEL-FINGERPRINT-v1")
            .Concat(Convert.FromHexString(Client.ToString("N")))
            .Concat(Convert.FromHexString(Session.ToString("N"))).ToArray();
        Assert.Equal(32, actual.Length);
        Assert.Equal("492D8F9903C79E08146F0DF85ACBE28B03D46DFFBEBDC3CAE364985C2AFAAD5B", Convert.ToHexString(SHA256.HashData(preimage)));
        Assert.Equal(SHA256.HashData(preimage), actual);
    }

    [Fact]
    public async Task R26_ConcurrentDifferentMetadata_ConvergesWithoutOverwritingWinner()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_concurrent");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        var a = Cancel(http, Session, new("reason_a", "request-a", "correlation-a"));
        var b = Cancel(http, Session, new("reason_b", "request-b", "correlation-b"));
        var results = await Task.WhenAll(a, b);
        Assert.Equal(results[0], results[1]);
        var winnerA = results[0].RequestId == "request-a";
        Assert.Contains(results[0].RequestId, new[] { "request-a", "request-b" });
        await AssertHistory(isolated, Session, winnerA ? "reason_a" : "reason_b",
            winnerA ? "request-a" : "request-b", winnerA ? "correlation-a" : "correlation-b");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task R26_PreA1CancelledSessionWithoutA1CancelOperation_PreservesHistoricalResult(bool expiredLiveCapability)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_historical_cancel");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        var sessionId = Guid.NewGuid();
        var historical = await seed.Sessions.AsNoTracking().SingleAsync(x => x.Id == Session);
        historical.Id = sessionId;
        historical.RequestId = "historical-request";
        historical.CorrelationId = "historical-correlation";
        seed.Sessions.Add(historical);
        await seed.SaveChangesAsync();
        if (expiredLiveCapability)
        {
            Assert.Equal("CREATED", await seed.Database.SqlQueryRaw<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
                '{Client}','{sessionId}','Issue',NULL,NULL,'{Guid.NewGuid()}','{Guid.NewGuid()}','r26history01',
                decode(repeat('11',32),'hex'),1,decode(repeat('22',32),'hex'),now())
                """).SingleAsync());
            await seed.Database.ExecuteSqlRawAsync($"""
                UPDATE tagekyc.capture_capabilities SET "ExpiresAtUtc"=clock_timestamp()+interval '50 milliseconds'
                WHERE "VerificationSessionId"='{sessionId}'
                """);
            await Task.Delay(100);
        }
        // Model pre-A1 persisted state directly, not by calling the A1 cancel
        // function and deleting its immutable history. The landed session has no
        // Reason column; absent Cancel operation/audit means no reason history
        // exists to manufacture. Every existing session/history byte stays frozen.
        historical.State = nameof(VerificationSessionState.Cancelled);
        await seed.SaveChangesAsync();
        seed.ChangeTracker.Clear();
        async Task<List<string>> Rows(TagEkycDbContext db, string table, string key) =>
            await db.Database.SqlQueryRaw<string>($"""
                SELECT pg_catalog.to_jsonb(r)::text AS "Value" FROM tagekyc.{table} r
                WHERE r."{key}"='{sessionId}' ORDER BY pg_catalog.to_jsonb(r)::text
                """).ToListAsync();
        var beforeSession = await Rows(seed, "verification_sessions", "Id");
        var beforeCapabilities = await Rows(seed, "capture_capabilities", "VerificationSessionId");
        var beforeOperations = await Rows(seed, "capture_capability_operations", "VerificationSessionId");
        var beforeEvents = await Rows(seed, "capture_capability_events", "VerificationSessionId");
        var beforeAudit = await Rows(seed, "audit_events", "VerificationSessionId");
        Assert.Single(beforeSession);
        Assert.Equal(expiredLiveCapability ? 1 : 0, beforeCapabilities.Count);
        Assert.Equal(expiredLiveCapability ? 1 : 0, beforeOperations.Count);
        Assert.Equal(expiredLiveCapability ? 1 : 0, beforeEvents.Count);
        Assert.Empty(beforeAudit);
        Assert.Equal(0, await Count(seed, sessionId, "capture_capability_operations", "\"OperationKind\" IN ('Cancel','Expire')"));
        Assert.Equal(0, await Count(seed, sessionId, "capture_capability_events", "\"EventType\" IN ('Cancelled','Expired')"));
        if (expiredLiveCapability)
            Assert.Equal(1, await Count(seed, sessionId, "capture_capabilities", "\"State\"='ActiveUnbound' AND \"ExpiresAtUtc\"<=clock_timestamp()"));

        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        List<string>? firstCapabilities = null, firstOperations = null, firstEvents = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = Request(sessionId, new($"caller_reason_{attempt}", $"caller-request-{attempt}", $"caller-correlation-{attempt}"));
            if (attempt == 1) request.Headers.Add("Idempotency-Key", "ignored-not-a-uuid");
            using var response = await http.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}\n{A1SyntheticDbFailureInterceptor.Recorded}");
            var result = (await response.Content.ReadFromJsonAsync<CancelVerificationSessionResponseDto>())!;
            Assert.Equal(sessionId, Guid.Parse(result.VerificationSessionId));
            Assert.Equal(VerificationSessionStateDto.Cancelled, result.State);
            Assert.Equal("historical-request", result.RequestId);
            Assert.Equal("historical-correlation", result.CorrelationId);
            await using var observer = isolated.CreateDbContext();
            Assert.Equal(beforeSession, await Rows(observer, "verification_sessions", "Id"));
            Assert.Equal(beforeAudit, await Rows(observer, "audit_events", "VerificationSessionId"));
            Assert.Equal(0, await Count(observer, sessionId, "capture_capability_operations", "\"OperationKind\"='Cancel'"));
            Assert.Equal(0, await Count(observer, sessionId, "capture_capability_events", "\"EventType\"='Cancelled'"));
            Assert.Equal(0, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == sessionId && x.EventType == "SESSION_CANCELLED"));
            Assert.Equal(expiredLiveCapability ? 1 : 0, await Count(observer, sessionId, "capture_capability_operations", "\"OperationKind\"='Expire'"));
            Assert.Equal(expiredLiveCapability ? 1 : 0, await Count(observer, sessionId, "capture_capability_events", "\"EventType\"='Expired'"));
            var capabilities = await Rows(observer, "capture_capabilities", "VerificationSessionId");
            var operations = await Rows(observer, "capture_capability_operations", "VerificationSessionId");
            var events = await Rows(observer, "capture_capability_events", "VerificationSessionId");
            if (expiredLiveCapability)
            {
                Assert.Single(capabilities);
                Assert.Equal(1, await Count(observer, sessionId, "capture_capabilities", "\"State\"='Expired' AND \"Revision\"=2 AND \"ExpiredAtUtc\" IS NOT NULL AND \"TerminalReason\"='CapabilityExpiry'"));
                Assert.Equal(beforeOperations.Count + 1, operations.Count);
                Assert.Equal(beforeEvents.Count + 1, events.Count);
                Assert.All(beforeOperations, row => Assert.Contains(row, operations));
                Assert.All(beforeEvents, row => Assert.Contains(row, events));
            }
            else
            {
                Assert.Equal(beforeCapabilities, capabilities);
                Assert.Equal(beforeOperations, operations);
                Assert.Equal(beforeEvents, events);
            }
            if (attempt == 0)
            {
                firstCapabilities = capabilities; firstOperations = operations; firstEvents = events;
            }
            else
            {
                Assert.Equal(firstCapabilities, capabilities);
                Assert.Equal(firstOperations, operations);
                Assert.Equal(firstEvents, events);
            }
        }
    }

    [Fact]
    public async Task R26_InternalFingerprintCorruption_IsNotCallerConflictAndCreatesNoReplayRows()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_invariant");
        await using var db = isolated.CreateDbContext();
        await Prepare(db);
        await using var app = await Start(db.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        await Cancel(http, Session, new("winner", "winner-request", "winner-correlation"));
        const string corruption = """
            UPDATE tagekyc.capture_capability_operations SET "RequestFingerprint"=decode(repeat('aa',32),'hex')
            WHERE "VerificationSessionId"='90000000-0000-4000-8000-000000000001' AND "OperationKind"='Cancel'
            """;
        var guard = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(corruption));
        Assert.Equal("TIP88C1C6BA_APPEND_ONLY", guard.MessageText);
        // Isolated fixture owner injects impossible-at-runtime corruption. Restore
        // the exact immutable trigger before the HTTP request; no online grant or
        // product transition is weakened to manufacture this negative control.
        await using (var injection = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.capture_capability_operations DISABLE TRIGGER capture_capability_operations_immutable");
            await db.Database.ExecuteSqlRawAsync(corruption);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.capture_capability_operations ENABLE TRIGGER capture_capability_operations_immutable");
            await injection.CommitAsync();
        }
        using var request = Request(Session, new("retry", "changed", "changed"));
        using var response = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("NOT_READY", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Fingerprint", body, StringComparison.OrdinalIgnoreCase);
        await AssertHistory(isolated, Session, "winner", "winner-request", "winner-correlation");
    }

    [Fact]
    public async Task R26_CrossClientCannotCancelOrReplay_NoCancelHistoryFromDeniedActor()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_cross_client");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        foreach (var afterCommit in new[] { false, true })
        {
            if (afterCommit) await Cancel(http, Session, new());
            using var request = Request(Session, new());
            request.Headers.Add("X-Test-Other-Client", "true");
            using var response = await http.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await using var observer = isolated.CreateDbContext();
            Assert.Equal(afterCommit ? 1 : 0, await Count(observer, Session, "capture_capability_operations", "\"OperationKind\"='Cancel'"));
            Assert.Equal(afterCommit ? 1 : 0, await Count(observer, Session, "capture_capability_events", "\"EventType\"='Cancelled'"));
            Assert.Equal(afterCommit ? 1 : 0, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == Session && x.EventType == "SESSION_CANCELLED"));
            Assert.Equal(0, await observer.Database.SqlQueryRaw<int>($"""
                SELECT count(*)::integer AS "Value" FROM tagekyc.capture_capability_operations
                WHERE "VerificationSessionId"='{Session}' AND "ClientApplicationId"<>'{Client}' AND "OperationKind"='Cancel'
                """).SingleAsync());
        }
    }

    [Fact]
    public async Task R26_BindCancelRace_SessionAndCapabilityTerminalizeAtomically()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_bind_race");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        var sessionId = Guid.NewGuid(); var capability = Guid.NewGuid();
        var template = await seed.Sessions.AsNoTracking().SingleAsync(x => x.Id == Session);
        template.Id = sessionId; seed.Sessions.Add(template); await seed.SaveChangesAsync();
        Assert.Equal("CREATED", await seed.Database.SqlQueryRaw<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
            '{Client}','{sessionId}','Issue',NULL,NULL,'{Guid.NewGuid()}','{capability}','r26racesecrt',
            decode(repeat('11',32),'hex'),1,decode(repeat('22',32),'hex'),now())
            """).SingleAsync());
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        async Task<string> Bind()
        {
            await using var db = isolated.CreateDbContext();
            return await db.Database.SqlQueryRaw<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.capture_runtime_bind_capability(
                '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
                '60000000-0000-4000-8000-000000000001',1,'{capability}',true,
                '{Guid.NewGuid()}',decode(repeat('44',32),'hex'),now())
                """).SingleAsync();
        }
        var bind = Bind();
        var cancel = Cancel(http, sessionId, new("race_cancel", "race-request", "race-correlation"));
        await Task.WhenAll(bind, cancel);
        Assert.Contains(await bind, new[] { "CREATED", "ACCESS_DENIED" });
        await AssertHistory(isolated, sessionId, "race_cancel", "race-request", "race-correlation");
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(0, await observer.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.capture_capabilities
            WHERE "VerificationSessionId"='{sessionId}' AND "State" IN ('Bound','ActiveUnbound')
            """).SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task R26_PreviouslyBoundTerminalCapability_PreservesImmutableBindingAndDeniesAppend(bool expireFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_bound_terminal");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        var binding = await seed.Database.SqlQueryRaw<Guid>($"""
            SELECT "CaptureExecutionBindingId" AS "Value" FROM tagekyc.capture_execution_bindings
            WHERE "VerificationSessionId"='{Session}'
            """).SingleAsync();
        async Task<string> FrozenBinding(TagEkycDbContext db) => await db.Database.SqlQueryRaw<string>($"""
            SELECT pg_catalog.to_jsonb(b)::text AS "Value" FROM tagekyc.capture_execution_bindings b
            WHERE b."CaptureExecutionBindingId"='{binding}'
            """).SingleAsync();
        async Task<int> AppendAdmission(TagEkycDbContext db) => await db.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.capture_runtime_validate_append_authority(
            '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
            '60000000-0000-4000-8000-000000000001',1,'{binding}','CaptureObservation',now())
            """).SingleAsync();
        var before = await FrozenBinding(seed);
        Assert.Equal(1, await AppendAdmission(seed));
        if (expireFirst)
        {
            // Synthetic horizon mutation preserves issued/bound ordering and every
            // frozen binding byte. Let the near-future horizon genuinely elapse.
            await seed.Database.ExecuteSqlRawAsync($"""
                UPDATE tagekyc.capture_capabilities SET "ExpiresAtUtc"=clock_timestamp()+interval '50 milliseconds'
                WHERE "VerificationSessionId"='{Session}' AND "State"='Bound'
                """);
            await Task.Delay(100);
        }
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        var first = await Cancel(http, Session, new("terminal_cancel", "terminal-request", "terminal-correlation"));
        Assert.Equal(first, await Cancel(http, Session, new("different", "changed", "changed")));
        await AssertHistory(isolated, Session, "terminal_cancel", "terminal-request", "terminal-correlation");
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(before, await FrozenBinding(observer));
        Assert.Equal(expireFirst ? "Expired" : "Revoked", await observer.Database.SqlQueryRaw<string>($"""
            SELECT "State"::text AS "Value" FROM tagekyc.capture_capabilities WHERE "VerificationSessionId"='{Session}'
            """).SingleAsync());
        Assert.Equal(expireFirst ? 1 : 0, await Count(observer, Session, "capture_capability_operations", "\"OperationKind\"='Expire'"));
        Assert.Equal(expireFirst ? 1 : 0, await Count(observer, Session, "capture_capability_events", "\"EventType\"='Expired'"));
        Assert.Equal(0, await AppendAdmission(observer));
        Assert.Equal(0, await observer.CaptureArtifacts.CountAsync(x => x.VerificationSessionId == Session));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task R26_ElapsedSessionAndCapability_CommitsExpiryOnDeniedAndReplayDoesNotDuplicate(bool previouslyBound)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_expiry_denied");
        await using var seed = isolated.CreateDbContext();
        await Prepare(seed);
        var sessionId = Session;
        if (!previouslyBound)
        {
            sessionId = Guid.NewGuid();
            var template = await seed.Sessions.AsNoTracking().SingleAsync(x => x.Id == Session);
            template.Id = sessionId; seed.Sessions.Add(template); await seed.SaveChangesAsync();
            Assert.Equal("CREATED", await seed.Database.SqlQueryRaw<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
                '{Client}','{sessionId}','Issue',NULL,NULL,'{Guid.NewGuid()}','{Guid.NewGuid()}','r26expired01',
                decode(repeat('11',32),'hex'),1,decode(repeat('22',32),'hex'),now())
                """).SingleAsync());
        }
        var frozenBindings = await seed.Database.SqlQueryRaw<string>($"""
            SELECT pg_catalog.to_jsonb(b)::text AS "Value" FROM tagekyc.capture_execution_bindings b
            WHERE b."VerificationSessionId"='{sessionId}'
            """).ToListAsync();
        Assert.Equal(previouslyBound ? 1 : 0, frozenBindings.Count);
        await seed.Database.ExecuteSqlRawAsync($"""
            UPDATE tagekyc.capture_capabilities SET "ExpiresAtUtc"=clock_timestamp()+interval '50 milliseconds'
            WHERE "VerificationSessionId"='{sessionId}';
            UPDATE tagekyc.verification_sessions SET "ExpiresAt"=clock_timestamp()+interval '50 milliseconds'
            WHERE "Id"='{sessionId}';
            """);
        await Task.Delay(100);
        await using var app = await Start(seed.Database.GetConnectionString()!);
        using var http = app.GetTestClient();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = Request(sessionId, new("expired_cancel", "expired-request", "expired-correlation"));
            using var response = await http.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.Conflict,
                $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}\n{A1SyntheticDbFailureInterceptor.Recorded}");
            await using var observer = isolated.CreateDbContext();
            Assert.Equal(1, await Count(observer, sessionId, "capture_capability_operations", "\"OperationKind\"='Expire'"));
            Assert.Equal(1, await Count(observer, sessionId, "capture_capability_events", "\"EventType\"='Expired'"));
            Assert.Equal(0, await Count(observer, sessionId, "capture_capability_operations", "\"OperationKind\"='Cancel'"));
            Assert.Equal(0, await Count(observer, sessionId, "capture_capability_events", "\"EventType\"='Cancelled'"));
            Assert.Equal(0, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == sessionId && x.EventType == "SESSION_CANCELLED"));
            Assert.Equal("Expired", await observer.Database.SqlQueryRaw<string>($"""
                SELECT "State"::text AS "Value" FROM tagekyc.capture_capabilities WHERE "VerificationSessionId"='{sessionId}'
                """).SingleAsync());
            Assert.Equal(frozenBindings, await observer.Database.SqlQueryRaw<string>($"""
                SELECT pg_catalog.to_jsonb(b)::text AS "Value" FROM tagekyc.capture_execution_bindings b
                WHERE b."VerificationSessionId"='{sessionId}'
                """).ToListAsync());
            Assert.NotEqual("Cancelled", (await observer.Sessions.AsNoTracking().SingleAsync(x => x.Id == sessionId)).State);
        }
    }

    [Theory]
    [InlineData("Revoked", true)]
    [InlineData("Revoked", false)]
    [InlineData("Expired", true)]
    [InlineData("Expired", false)]
    public async Task R26_TerminalHistoricalGraph_RejectsMissingBindingOrMissingBoundAt(string state, bool removeBinding)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("r26_graph_negative");
        await using var db = isolated.CreateDbContext();
        await Prepare(db);
        // Positive control: the same terminal tuple, with its full frozen
        // binding and BoundAt retained, must commit before applying one mutation.
        await db.Database.ExecuteSqlRawAsync($"""
            UPDATE tagekyc.capture_capabilities SET "State"='{state}',
            "RevokedAtUtc"={(state == "Revoked" ? "now()" : "NULL")},
            "ExpiredAtUtc"={(state == "Expired" ? "now()" : "NULL")},"TerminalReason"='SyntheticTerminal'
            WHERE "VerificationSessionId"='{Session}'
            """);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(removeBinding
            ? $"DELETE FROM tagekyc.capture_execution_bindings WHERE \"VerificationSessionId\"='{Session}'"
            : $"UPDATE tagekyc.capture_capabilities SET \"BoundAtUtc\"=NULL WHERE \"VerificationSessionId\"='{Session}'");
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE"));
        Assert.Equal("P0001", error.SqlState);
        Assert.Equal("TIP88C1C6BA_CAPABILITY_GRAPH_INCONSISTENT", error.MessageText);
        await transaction.RollbackAsync();
        Assert.Equal(1, await Count(db, Session, "capture_execution_bindings", "true"));
        Assert.Equal(1, await Count(db, Session, "capture_capabilities", "\"BoundAtUtc\" IS NOT NULL"));
    }

    [Fact]
    public async Task R26_AuditWriter_HasOnlyExactColumnInsertGrant_OnlineA1RolesHaveNone()
    {
        await using var db = postgres.CreateDbContext();
        string[] columns = ["Id", "ClientApplicationId", "VerificationSessionId", "ActorType", "ActorId", "EventType",
            "EventPayloadHash", "EventPayloadRef", "RequestId", "CorrelationId", "OccurredAt"];
        var actual = await db.Database.SqlQueryRaw<string>("""
            SELECT a.attname::text AS "Value" FROM pg_catalog.pg_attribute a
            WHERE a.attrelid='tagekyc.audit_events'::regclass AND a.attnum>0 AND NOT a.attisdropped
            AND pg_catalog.has_column_privilege('tagekyc_raw_export_deployer',a.attrelid,a.attnum,'INSERT')
            """).ToListAsync();
        Assert.Equal(columns.OrderBy(x => x, StringComparer.Ordinal), actual.OrderBy(x => x, StringComparer.Ordinal));
        Assert.False(await db.Database.SqlQueryRaw<bool>("""
            SELECT pg_catalog.has_table_privilege('tagekyc_raw_export_deployer','tagekyc.audit_events','INSERT') AS "Value"
            """).SingleAsync());
        foreach (var role in new[] { "tagekyc_capture_runtime_application", "tagekyc_capture_runtime_operator", "tagekyc_capture_runtime_authenticator" })
            Assert.False(await db.Database.SqlQueryRaw<bool>($"""
                SELECT pg_catalog.has_any_column_privilege('{role}','tagekyc.audit_events','INSERT') AS "Value"
                """).SingleAsync());
        // tagekyc_runtime is deliberately not asserted denied: its existing
        // Client audit privileges predate A1 and are not expanded by this grant.
    }

    private static async Task Prepare(TagEkycDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await Tip88C1C6BA1AppendAuthorityTests.SeedBindingAsync(db);
        var row = await db.Sessions.SingleAsync(x => x.Id == Session);
        row.Profile = nameof(VerificationProfile.StandardEkycProfile);
        row.State = nameof(VerificationSessionState.Created);
        row.Result = nameof(VerificationResult.NotAvailable);
        row.AssuranceLevel = nameof(AssuranceLevel.None);
        row.RequiredChecksJson = "[\"CaptureQuality\"]";
        row.PolicySnapshotId = PolicySnapshotId.LocalDevS1.Value;
        row.RetentionClass = nameof(RetentionClass.LocalDevEphemeral);
        row.DeletionEligibility = nameof(DeletionEligibility.NotEvaluated);
        row.LegalHoldStatus = nameof(LegalHoldStatus.None);
        row.PurgeBlockReason = nameof(PurgeBlockReason.None);
        await db.SaveChangesAsync(); await tx.CommitAsync(); db.ChangeTracker.Clear();
    }

    private static async Task AssertHistory(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Guid sessionId, string reason, string request, string correlation)
    {
        await using var db = isolated.CreateDbContext();
        Assert.Equal(1, await Count(db, sessionId, "capture_capability_operations", "\"OperationKind\"='Cancel'"));
        Assert.Equal(1, await Count(db, sessionId, "capture_capability_events", "\"EventType\"='Cancelled'"));
        var audit = Assert.Single(await db.AuditEvents.Where(x => x.VerificationSessionId == sessionId && x.EventType == "SESSION_CANCELLED").ToListAsync());
        Assert.Equal(reason, audit.EventPayloadRef); Assert.Equal(request, audit.RequestId); Assert.Equal(correlation, audit.CorrelationId);
        var session = await db.Sessions.SingleAsync(x => x.Id == sessionId);
        Assert.Equal("Cancelled", session.State); Assert.Equal(request, session.RequestId); Assert.Equal(correlation, session.CorrelationId);
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.capture_capabilities
            WHERE "VerificationSessionId"='{sessionId}' AND "State" IN ('Bound','ActiveUnbound')
            """).SingleAsync());
    }

    private static Task<int> Count(TagEkycDbContext db, Guid session, string table, string predicate) =>
        db.Database.SqlQueryRaw<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.{table} WHERE \"VerificationSessionId\"='{session}' AND {predicate}").SingleAsync();

    private static HttpRequestMessage Request(Guid session, CancelVerificationSessionRequestDto body) =>
        new(HttpMethod.Post, $"/api/ekyc/verification-sessions/{session:N}/cancel") { Content = JsonContent.Create(body) };

    private static async Task<CancelVerificationSessionResponseDto> Cancel(HttpClient client, Guid session,
        CancelVerificationSessionRequestDto body, string? header = null)
    {
        using var request = Request(session, body);
        if (header is not null) request.Headers.Add("Idempotency-Key", header);
        using var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}\nSynthetic DB evidence: {A1SyntheticDbFailureInterceptor.Recorded}");
        return (await response.Content.ReadFromJsonAsync<CancelVerificationSessionResponseDto>())!;
    }

    private static async Task<WebApplication> Start(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ILocalDevClientPolicyProvider, LocalDevRuntimePolicySource>();
        builder.Services.AddTagEkycPostgresPersistence(connectionString);
        A1SyntheticDbFailureInterceptor.Clear();
        builder.Services.AddDbContext<TagEkycDbContext>(options => options.AddInterceptors(
            new A1SyntheticDbFailureInterceptor(), new A1SyntheticTransactionFailureInterceptor()));
        builder.Services.AddSingleton<IEvidenceSigner, LocalDevEs256JwsEvidenceSigner>();
        builder.Services.AddScoped<VerificationSessionApplicationService>();
        builder.Services.AddScoped<IVerificationSessionCommands>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
        builder.Services.AddScoped<IVerificationSessionQueries>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
        builder.Services.AddScoped<VerificationCompletionApplicationService>();
        builder.Services.AddScoped<IVerificationSessionCompletionCommands>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
        builder.Services.AddScoped<IEvidencePackageQueries>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
        builder.Services.AddSingleton<IApiKeyAuthenticator, FixtureAuthentication>();
        var app = builder.Build();
        app.MapVerificationSessionEndpoints(includeLegacyProducerRoutes: false);
        await app.StartAsync(); return app;
    }

    // Only the authenticated identity seam is controlled. The HTTP handler,
    // application cancellation, finalization transaction and SQL are production.
    private sealed class FixtureAuthentication : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext context,
            string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(Guid.NewGuid(),
                context.Request.Headers.ContainsKey("X-Test-Other-Client") ? Guid.Parse("92000000-0000-4000-8000-000000000002") : Client,
                "r26-client", AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string> { "session.cancel" })));
    }
}
