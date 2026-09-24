using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Cryptography;
using Xunit.Abstractions;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using static TagEkyc.IntegrationTests.Tip88C1C6BA3ConsentRetentionTests;

namespace TagEkyc.IntegrationTests;

// Synthetic PostgreSQL transition proofs. Each proof is bounded to the durable
// transition it drives; production activation remains outside this assembly.
[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3R2TerminalProjectionTests(
    PostgresPersistenceFixture postgres, ITestOutputHelper output)
{
    [Theory]
    [InlineData("LiveSelfieImage")]
    [InlineData("ChipDg2Portrait")]
    public async Task ActivatedHttpRoute_UsesBrokerAndBodyPipelineToPublishBothRawClasses(string rawClass)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            "a3_activated_http_" + rawClass.ToLowerInvariant());
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true, rawClass: rawClass);
        await using var observer = isolated.CreateDbContext();
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var owners = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observer.Database.GetConnectionString()!);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var keys = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var bodyPipeline = new CaptureRuntimeRawIngressBodyPipeline(owners, keys, keys,
            preflight.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()), brokerOptions, CancellationToken.None);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 1, 1024, 1024), broker, bodyPipeline, 1024);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();

        var now = DateTimeOffset.UtcNow;
        var idempotencyKey = Guid.NewGuid().ToString("N");
        await using var body = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);
        var response = await app.GetTestServer().SendAsync(context =>
        {
            context.Request.Method = "POST";
            context.Request.Path = "/api/ekyc/raw-export/source-ingress";
            context.Request.ContentType = "image/jpeg";
            context.Request.ContentLength = body.Length;
            context.Request.Body = body;
            context.Request.Headers["X-TagEkyc-Agent-Configuration-Revision"] = "1";
            context.Request.Headers["X-TagEkyc-Verification-Session-Id"] = scope.Session.ToString("N");
            context.Request.Headers["X-TagEkyc-Capture-Artifact-Id"] = scope.Artifact.ToString("N");
            context.Request.Headers["X-TagEkyc-Capture-Revision"] = "1";
            context.Request.Headers["X-TagEkyc-Raw-Class"] = rawClass;
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;
            context.Request.Headers["X-TagEkyc-Plaintext-Sha256"] =
                Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant();
            context.Request.Headers["X-TagEkyc-Captured-At-Utc"] = now.AddSeconds(-5).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Started-At-Utc"] = now.AddSeconds(-4).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Expires-At-Utc"] = now.AddMinutes(5).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Budget-Seconds"] = "300";
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] =
                AcceptedRuntimeAuthenticator.CredentialId.ToString("N");
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = "1";
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] =
                now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = Base64Url(new byte[32]);
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(new byte[64]);
        });

        Assert.Equal(200, response.Response.StatusCode);
        Assert.Equal(body.Length, body.Position);
        using var json = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Equal(RawExportSourceIngressCodes.Available,
            json.RootElement.GetProperty("outcomeCode").GetString());
        var sourceId = json.RootElement.GetProperty("sourceArtifactId").GetGuid();
        Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
            .CountAsync(row => row.SourceArtifactId == sourceId && row.PublicationState == "Available"));

        // The same signed-caller binding re-enters after R5. The published
        // descriptor must be returned without reading or re-encrypting a body.
        var keyCount = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
        var objectCount = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
        await using var replayBody = new CountingReplayBodyStream("synthetic-retainedsource"u8.ToArray());
        var replay = await app.GetTestServer().SendAsync(context =>
        {
            context.Request.Method = "POST";
            context.Request.Path = "/api/ekyc/raw-export/source-ingress";
            context.Request.ContentType = "image/jpeg";
            context.Request.ContentLength = replayBody.Length;
            context.Request.Body = replayBody;
            context.Request.Headers["X-TagEkyc-Agent-Configuration-Revision"] = "1";
            context.Request.Headers["X-TagEkyc-Verification-Session-Id"] = scope.Session.ToString("N");
            context.Request.Headers["X-TagEkyc-Capture-Artifact-Id"] = scope.Artifact.ToString("N");
            context.Request.Headers["X-TagEkyc-Capture-Revision"] = "1";
            context.Request.Headers["X-TagEkyc-Raw-Class"] = rawClass;
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;
            context.Request.Headers["X-TagEkyc-Plaintext-Sha256"] =
                Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant();
            context.Request.Headers["X-TagEkyc-Captured-At-Utc"] = now.AddSeconds(-5).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Started-At-Utc"] = now.AddSeconds(-4).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Expires-At-Utc"] = now.AddMinutes(5).ToString("O", CultureInfo.InvariantCulture);
            context.Request.Headers["X-TagEkyc-Retention-Budget-Seconds"] = "300";
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] =
                AcceptedRuntimeAuthenticator.CredentialId.ToString("N");
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = "1";
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] =
                now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = Base64Url(RandomNumberGenerator.GetBytes(32));
            context.Request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(new byte[64]);
        });
        Assert.Equal(200, replay.Response.StatusCode);
        using var replayJson = await JsonDocument.ParseAsync(replay.Response.Body);
        Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable,
            replayJson.RootElement.GetProperty("outcomeCode").GetString());
        Assert.Equal(sourceId, replayJson.RootElement.GetProperty("sourceArtifactId").GetGuid());
        Assert.Equal(0, replayBody.ReadCalls);
        Assert.Equal(keyCount, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
        Assert.Equal(objectCount, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
    }

    [Theory]
    [InlineData("LiveSelfieImage")]
    [InlineData("ChipDg2Portrait")]
    public async Task PublicBodyPipeline_ReusesR2ThroughR5AndPublishesAvailable(string rawClass)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            "a3_public_body_pipeline_" + rawClass.ToLowerInvariant());
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, handoff) = await CommitR1(isolated, rawClass);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var keys = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var pipeline = new CaptureRuntimeRawIngressBodyPipeline(scopes, keys, keys,
            preflight.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()),
            RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration()),
            CancellationToken.None);
        await using var body = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);

        var result = await pipeline.ProcessAsync(
            Tip88C1C6BA3BrokerHttpTests.Metadata() with { RawClass = rawClass },
            handoff, body, CancellationToken.None);

        Assert.Equal(CaptureRuntimeRawIngressOutcome.Available, result.Outcome);
        Assert.Equal(handoff.SourceArtifactId, result.SourceArtifactId);
        Assert.Equal("Available", result.CurrentSourceState);
        Assert.Equal("Available", result.CurrentDisposition);
        Assert.Null(result.RetryNotBeforeUtc);
        Assert.Equal(body.Length, body.Position);
        Assert.Equal(1, await observer.RawExportSourcePublications
            .CountAsync(row => row.SourceArtifactId == handoff.SourceArtifactId));
    }

    [Fact]
    public async Task PublicBodyPipeline_DoesNotReturnMismatchBeforeDurableFinalization()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_public_body_mismatch");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, handoff) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var keys = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var pipeline = new CaptureRuntimeRawIngressBodyPipeline(scopes, keys, keys,
            preflight.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()),
            RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration()),
            CancellationToken.None);
        var changed = "synthetic-retainedsource"u8.ToArray();
        changed[0] ^= 1;
        await using var body = new MemoryStream(changed, writable: false);

        var result = await pipeline.ProcessAsync(
            Tip88C1C6BA3BrokerHttpTests.Metadata(), handoff, body, CancellationToken.None);

        Assert.Equal(CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable, result.Outcome);
        var attempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .SingleAsync(row => row.AttemptId == handoff.AttemptId);
        Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch, attempt.R2TerminalIntentCode);
        Assert.Null(attempt.R2TerminalOutcomeCode);
        Assert.Empty(await observer.RawExportSourcePublications.AsNoTracking()
            .Where(row => row.SourceArtifactId == handoff.SourceArtifactId).ToListAsync());
    }

    [Fact]
    public async Task R6_WorkerDeletesOnlyObsoleteObjectAndKeyThenFinalizesCleanup()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r6_worker_cleanup");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var writer = new S3CompatibleProvisionalObjectWriter(
            minio.Options(ProvisionalObjectCapability.Writer));
        var (_, h, written, keyProvider) = await WriteIncompleteObject(isolated, writer);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);

        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var verificationServices = new ServiceCollection()
            .AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            verificationServices.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));

        Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.Publication,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.Publication,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);

        var obsolete = await AddObsoleteVerifiedAttemptAsync(isolated, h, writer);
        var available = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.Publication, available.Step);
        Assert.Equal("Pending", available.Snapshot!.CleanupDisposition);

        var selected = new List<RetainedContinuationStep>();
        for (var pass = 0; pass < 4; pass++)
        {
            var progress = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
            selected.Add(progress.Step);
            if (progress.Snapshot!.CleanupDisposition == "Completed") break;
        }
        Assert.Equal(2, selected.Count(value => value == RetainedContinuationStep.SettleCleanupResource));
        Assert.Equal(RetainedContinuationStep.FinalizeCleanup, selected[^1]);

        var publication = await observer.RawExportSourcePublications.AsNoTracking()
            .SingleAsync(value => value.SourceArtifactId == h.SourceArtifactId);
        Assert.Equal("Available", publication.PublicationState);
        Assert.Equal("Completed", publication.CleanupDisposition);
        Assert.Equal(3, publication.PublicationRevision);
        Assert.Equal(32, publication.CleanupEvidenceDigest!.Length);
        Assert.Equal(2, await observer.RawExportSourceCleanupItems.AsNoTracking()
            .CountAsync(value => value.SourcePublicationId == publication.SourcePublicationId
                && value.CleanupState == "Completed"));

        Assert.Equal("Deleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == obsolete.ObjectCustodyId)).State);
        Assert.Equal("Revoked", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
            .SingleAsync(value => value.AttemptKeyReservationId == obsolete.AttemptKeyReservationId))
            .PreparationDisposition);
        Assert.Equal("VerifiedCompleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == written.ObjectCustodyId)).State);
        Assert.Equal("Active", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
            .SingleAsync(value => value.AttemptKeyReservationId == h.AttemptKeyReservationId))
            .PreparationDisposition);

        await using var reconcilerScope = await scopes.OpenReconcilerAsync(default);
        var winner = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == written.ObjectCustodyId);
        Assert.Equal(ExactObjectInspectionOutcome.Present,
            (await reconcilerScope.Services.GetRequiredService<IProvisionalObjectReconciler>()
                .InspectExactAsync(new(winner.ProvisionalObjectIdentity, winner.ObjectKey,
                    winner.ObjectBindingDigest), default)).Outcome);
        Assert.Equal(RetainedContinuationStep.None,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task R6_ProcessKillResumesFromDurableCleanupItemsWithoutTouchingWinner(int cut)
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        await RunR6ProcessKillCut(cut, minio, probe);
    }

    private async Task RunR6ProcessKillCut(int cut, DurableObjectMinioFixture minio,
        Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe probe)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r6_process_kill");
        var writer = new S3CompatibleProvisionalObjectWriter(
            minio.Options(ProvisionalObjectCapability.Writer));
        var (_, h, written, keyProvider) = await WriteIncompleteObject(isolated, writer);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var verificationServices = new ServiceCollection()
            .AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            verificationServices.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.Publication,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.Publication,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        var obsolete = await AddObsoleteVerifiedAttemptAsync(isolated, h, writer);
        var available = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal("Pending", available.Snapshot!.CleanupDisposition);
        var publicationId = available.Snapshot.SourcePublicationId!.Value;

        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(capability))
                    .AsEnumerable().Where(entry => entry.Value is not null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value)).ToArray();
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(
            logins.Connections, configs, h.SourceArtifactId, cut, false, Cleanup: true);
        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}",
                await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            var beforeKill = await CleanupRows(observer, publicationId);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(beforeKill, await CleanupRows(observer, publicationId));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                var output = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal("", await restartedErrors);
                Assert.Equal($"A3-CLEANUP:{restarted.Id}", output.Trim());
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }

            var publication = await observer.RawExportSourcePublications.AsNoTracking()
                .SingleAsync(value => value.SourcePublicationId == publicationId);
            Assert.Equal("Available", publication.PublicationState);
            Assert.Equal("Completed", publication.CleanupDisposition);
            Assert.Equal(2, await observer.RawExportSourceCleanupItems.AsNoTracking()
                .CountAsync(value => value.SourcePublicationId == publicationId
                    && value.CleanupState == "Completed"));
            Assert.Equal("Deleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
                .SingleAsync(value => value.ObjectCustodyId == obsolete.ObjectCustodyId)).State);
            Assert.Equal("Revoked", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
                .SingleAsync(value => value.AttemptKeyReservationId == obsolete.AttemptKeyReservationId))
                .PreparationDisposition);
            Assert.Equal("VerifiedCompleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
                .SingleAsync(value => value.ObjectCustodyId == written.ObjectCustodyId)).State);
            Assert.Equal("Active", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
                .SingleAsync(value => value.AttemptKeyReservationId == h.AttemptKeyReservationId))
                .PreparationDisposition);
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await errors);
    }

    [Fact]
    public async Task IncompleteR2_LostPutResponseRecoversExactObjectWithoutSecondPut()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_lost_put_response");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var lost = new LostPutResponseWriter(
            new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)));
        var (_, h, written, keyProvider) = await WriteIncompleteObject(isolated, lost);
        Assert.Equal(RawExportR2WriterDisposition.ReconciliationRequired, written.Disposition);
        Assert.Equal("PutOutcomeUnknown", written.ObjectState);
        Assert.Equal(1, lost.Calls);
        Assert.Equal(ConditionalPutOutcome.Created, lost.InnerResult!.Outcome);

        await using var observer = isolated.CreateDbContext();
        var objectId = written.ObjectCustodyId!.Value;
        var beforeEvents = await ObjectEventCount(observer, objectId);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);

        var advanced = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject, advanced.Step);
        Assert.Equal("ObjectPresentPendingVerification", advanced.Snapshot!.ObjectState);
        Assert.Equal(written.StateRevision + 1, advanced.Snapshot.ObjectStateRevision);
        Assert.Equal(beforeEvents + 1, await ObjectEventCount(observer, objectId));
        Assert.Equal(1, lost.Calls); // Recovery inspected and read; it never issued another PUT.

        var row = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        Assert.Equal("RecoveredPresent", row.PutOutcomeKind);
        Assert.True(row.CiphertextLength > 0);
        Assert.Equal(32, row.CiphertextDigest!.Length);
        Assert.Equal(32, row.ProviderReceiptDigest!.Length);

        await using (var reconcilerScope = await scopes.OpenReconcilerAsync(default))
        {
            var reconciler = reconcilerScope.Services.GetRequiredService<IProvisionalObjectReconciler>();
            await using var exact = await reconciler.OpenExactReadAsync(new(row.ProvisionalObjectIdentity,
                row.ObjectKey, row.ObjectBindingDigest), default);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[4096];
            long length = 0;
            while (true)
            {
                var read = await exact.Ciphertext.ReadAsync(buffer);
                if (read == 0) break;
                length += read;
                hash.AppendData(buffer, 0, read);
            }
            Assert.Equal(row.CiphertextLength, length);
            Assert.Equal(row.CiphertextDigest, hash.GetHashAndReset());
        }

        var verificationConfiguration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var verificationServices = new ServiceCollection()
            .AddTagEkycContentCommitment(verificationConfiguration).BuildServiceProvider();
        var complete = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            verificationServices.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var verified = await complete.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject, verified.Step);
        Assert.Equal("VerifiedCompleted", verified.Snapshot!.ObjectState);
        Assert.Equal(beforeEvents + 2, await ObjectEventCount(observer, objectId));
        Assert.Equal(1, lost.Calls);

        Assert.Equal(RetainedContinuationStep.Publication,
            (await complete.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.Publication,
            (await complete.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        var available = await complete.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.Publication, available.Step);
        Assert.Equal("Available", available.Snapshot!.PublicationState);
        Assert.Equal(1, lost.Calls);
    }

    [Fact]
    public async Task VerifierRunsOnDurableCiphertextNotRequestBuffer()
    {
        const string consumerProbe = "TAGEKYC_A3_BP16_PLAINTEXT_CONSUMER_OBSERVED";
        var priorProbe = Environment.GetEnvironmentVariable(consumerProbe);
        Environment.SetEnvironmentVariable(consumerProbe, null);
        try
        {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_durable_verifier");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        // WriteIncompleteObject disposes its only plaintext request stream before returning.
        var (_, handoff, written, keyProvider) = await WriteIncompleteObject(isolated,
            new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)));
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        await using var observer = isolated.CreateDbContext();
        var objectId = Assert.IsType<Guid>(written.ObjectCustodyId);
        var before = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(row => row.ObjectCustodyId == objectId);
        Assert.Equal("ObjectPresentPendingVerification", before.State);
        Assert.True(before.CiphertextLength > 0);
        Assert.Equal(32, before.CiphertextDigest!.Length);
        var priorEvents = await ObjectEventCount(observer, objectId);

        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var services = new ServiceCollection()
            .AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            services.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var result = await pipeline.AdvanceAsync(handoff.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject, result.Step);
        Assert.Equal("VerifiedCompleted", result.Snapshot!.ObjectState);
        Assert.Equal(objectId, result.Snapshot.ObjectCustodyId);
        Assert.Equal(priorEvents + 1, await ObjectEventCount(observer, objectId));
        Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking()
            .CountAsync(row => row.SourceArtifactId == handoff.SourceArtifactId));
        Assert.Null(Environment.GetEnvironmentVariable(consumerProbe));
        }
        finally
        {
            Environment.SetEnvironmentVariable(consumerProbe, priorProbe);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CiphertextFailureIsNotProducerMismatch(bool tamper)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            tamper ? "a3_r2_cipher_tampered" : "a3_r2_cipher_valid");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, handoff, written, keyProvider) = await WriteIncompleteObject(isolated,
            new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)));
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        await using var observer = isolated.CreateDbContext();
        var objectId = Assert.IsType<Guid>(written.ObjectCustodyId);
        var objectRow = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(row => row.ObjectCustodyId == objectId);
        Assert.Equal("ObjectPresentPendingVerification", objectRow.State);

        if (tamper)
        {
            using var reader = minio.CreateCapabilityClient(ProvisionalObjectCapability.Reconciler);
            using var objectResponse = await reader.GetObjectAsync(minio.BucketName, objectRow.ObjectKey);
            await using var ciphertext = new MemoryStream();
            await objectResponse.ResponseStream.CopyToAsync(ciphertext);
            var changed = ciphertext.ToArray();
            Assert.True(changed.Length > 0);
            changed[^1] ^= 1;
            await minio.PutRootObjectAsync(minio.BucketName, objectRow.ObjectKey, changed);
        }

        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var services = new ServiceCollection()
            .AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            services.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var first = await pipeline.AdvanceAsync(handoff.SourceArtifactId, 1000, default);
        if (!tamper)
        {
            Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject, first.Step);
            Assert.Equal("VerifiedCompleted", first.Snapshot!.ObjectState);
            Assert.Null(first.Snapshot.R2TerminalIntentCode);
            return;
        }

        Assert.Equal(RetainedContinuationStep.RecordVerificationFailureIntent, first.Step);
        Assert.Equal("RECAPTURE_REQUIRED", first.Snapshot!.R2TerminalIntentCode);
        Assert.NotEqual("CONTENT_COMMITMENT_MISMATCH", first.Snapshot.R2TerminalIntentCode);
        Assert.Equal("CleanupPending", first.Snapshot.ObjectState);
        Assert.Equal(RetainedContinuationStep.CleanupTerminalObject,
            (await pipeline.AdvanceAsync(handoff.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.SettleTerminalResources,
            (await pipeline.AdvanceAsync(handoff.SourceArtifactId, 1000, default)).Step);
        var finalized = await pipeline.AdvanceAsync(handoff.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.FinalizeIntent, finalized.Step);
        Assert.Equal("RECAPTURE_REQUIRED", finalized.Snapshot!.R2TerminalOutcomeCode);
        Assert.Equal("Deleted", finalized.Snapshot.ObjectState);
        Assert.Equal("Revoked", await KeyState(observer, handoff));
    }

    [Fact]
    public async Task IncompleteR2_UnknownWithoutObjectRequiresTwoPositiveAbsenceObservations()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_positive_absence");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var unknown = new ObservationPutAdapter(() => Task.CompletedTask);
        var (_, h, written, _) = await WriteIncompleteObject(isolated, unknown);
        Assert.Equal("PutOutcomeUnknown", written.ObjectState);
        Assert.Equal(1, unknown.Calls);

        await using var observer = isolated.CreateDbContext();
        var objectId = written.ObjectCustodyId!.Value;
        var beforeEvents = await ObjectEventCount(observer, objectId);
        var quiescence = 0;
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, null, _ =>
        {
            Interlocked.Increment(ref quiescence);
            return Task.CompletedTask;
        });

        var advanced = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject, advanced.Step);
        Assert.Equal("NoObjectEstablished", advanced.Snapshot!.ObjectState);
        Assert.Equal(1, quiescence);
        Assert.Equal(beforeEvents + 1, await ObjectEventCount(observer, objectId));
        var row = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        Assert.Equal("PositiveAbsence", row.PutOutcomeKind);
        Assert.Null(row.CiphertextLength);
        Assert.Null(row.CiphertextDigest);
        Assert.Null(row.ProviderReceiptDigest);
    }

    [Fact]
    public async Task IncompleteR2_ObjectAppearingDuringQuiescenceCannotBecomePositiveAbsence()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_absence_race");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var unknown = new ObservationPutAdapter(() => Task.CompletedTask);
        var (_, h, written, _) = await WriteIncompleteObject(isolated, unknown);
        await using var observer = isolated.CreateDbContext();
        var objectId = written.ObjectCustodyId!.Value;
        var before = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        var ciphertext = "synthetic-recovered-ciphertext"u8.ToArray();
        var writer = new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer));
        var recreations = 0;

        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var raced = new CaptureRuntimeSourcePipeline(scopes, null, async token =>
        {
            await using var body = new MemoryStream(ciphertext, writable: false);
            var put = await writer.PutIfAbsentAsync(new(new(before.ProvisionalObjectIdentity,
                before.ObjectKey, before.ObjectBindingDigest), before.PutOperationId!.Value,
                ciphertext.Length), body, token);
            Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
            Interlocked.Increment(ref recreations);
        });

        var deferred = await raced.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.None, deferred.Step);
        Assert.Equal("PutOutcomeUnknown", await ObjectState(observer, objectId));
        Assert.Equal(1, recreations);

        var recovered = await new CaptureRuntimeSourcePipeline(scopes)
            .AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject, recovered.Step);
        Assert.Equal("ObjectPresentPendingVerification", recovered.Snapshot!.ObjectState);
        var after = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        Assert.Equal(ciphertext.Length, after.CiphertextLength);
        Assert.Equal(SHA256.HashData(ciphertext), after.CiphertextDigest);
    }

    [Fact]
    public async Task IncompleteR2_PresentObjectWithWrongOperationBindingBecomesConflict()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_binding_conflict");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var unknown = new ObservationPutAdapter(() => Task.CompletedTask);
        var (_, h, written, _) = await WriteIncompleteObject(isolated, unknown);
        await using var observer = isolated.CreateDbContext();
        var objectId = written.ObjectCustodyId!.Value;
        var before = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        var ciphertext = "synthetic-wrong-operation"u8.ToArray();
        await using (var body = new MemoryStream(ciphertext, writable: false))
        {
            var put = await new S3CompatibleProvisionalObjectWriter(
                    minio.Options(ProvisionalObjectCapability.Writer))
                .PutIfAbsentAsync(new(new(before.ProvisionalObjectIdentity, before.ObjectKey,
                    before.ObjectBindingDigest), Guid.Parse("deadbeef-dead-4eef-8ead-deadbeef0001"),
                    ciphertext.Length), body, default);
            Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
        }

        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var advanced = await new CaptureRuntimeSourcePipeline(scopes)
            .AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject, advanced.Step);
        Assert.Equal("ObjectConflict", advanced.Snapshot!.ObjectState);
        var after = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        Assert.Equal("RecoveredMismatch", after.PutOutcomeKind);
        Assert.Null(after.CiphertextLength);
        Assert.Null(after.CiphertextDigest);
        Assert.Equal(32, after.ProviderReceiptDigest!.Length);
    }

    [Fact]
    public async Task IncompleteR2_LivePutInFlightLeaseDefersWithoutMutation()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_live_put");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),
            new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated, (await Provision(writer, h, kek)).Outcome);
        var repository = new RawExportR2Repository(writer);
        var begun = await repository.BeginAsync(h.AttemptId, h.ExpectedEncryptionAttemptRevision,
            h.ExpectedFence, default);
        Assert.Equal("Created", begun.Mutation.OutcomeCode);
        var operation = Guid.Parse("deadbeef-dead-4eef-8ead-deadbeef0002");
        var armed = await repository.ArmAsync(begun.Mutation.ObjectCustodyId,
            begun.Mutation.StateRevision, operation, default);
        Assert.Equal("Armed", armed.OutcomeCode);

        await using var observer = isolated.CreateDbContext();
        var before = await ObjectRow(observer, h);
        var beforeEvents = await ObjectEventCount(observer, begun.Mutation.ObjectCustodyId);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var advanced = await new CaptureRuntimeSourcePipeline(scopes, null, _ => Task.CompletedTask)
            .AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.None, advanced.Step);
        Assert.Equal(before, await ObjectRow(observer, h));
        Assert.Equal(beforeEvents, await ObjectEventCount(observer, begun.Mutation.ObjectCustodyId));
    }

    [Fact]
    public async Task IncompleteR2_RecoveredCorruptCiphertextRecordsO20AndSettlesExactResources()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_corrupt_recovery");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var unknown = new ObservationPutAdapter(() => Task.CompletedTask);
        var (_, h, written, keyProvider) = await WriteIncompleteObject(isolated, unknown);
        await using var observer = isolated.CreateDbContext();
        var objectId = written.ObjectCustodyId!.Value;
        var before = await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value => value.ObjectCustodyId == objectId);
        var corrupt = "not-a-valid-framed-ciphertext"u8.ToArray();
        await using (var body = new MemoryStream(corrupt, writable: false))
        {
            var put = await new S3CompatibleProvisionalObjectWriter(
                    minio.Options(ProvisionalObjectCapability.Writer))
                .PutIfAbsentAsync(new(new(before.ProvisionalObjectIdentity, before.ObjectKey,
                    before.ObjectBindingDigest), before.PutOperationId!.Value, corrupt.Length), body, default);
            Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
        }

        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject,
            (await new CaptureRuntimeSourcePipeline(scopes)
                .AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);

        var verificationConfiguration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var verificationServices = new ServiceCollection()
            .AddTagEkycContentCommitment(verificationConfiguration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            verificationServices.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var failed = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.RecordVerificationFailureIntent, failed.Step);
        Assert.Equal("CleanupPending", failed.Snapshot!.ObjectState);
        Assert.Equal("RECAPTURE_REQUIRED", failed.Snapshot.R2TerminalIntentCode);
        Assert.Equal("Terminated", failed.Snapshot.R2TerminalIntentDisposition);
        Assert.Null(failed.Snapshot.R2TerminalOutcomeCode);

        Assert.Equal(RetainedContinuationStep.CleanupTerminalObject,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(RetainedContinuationStep.SettleTerminalResources,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        var finalized = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.FinalizeIntent, finalized.Step);
        Assert.Equal("RECAPTURE_REQUIRED", finalized.Snapshot!.R2TerminalOutcomeCode);
        Assert.Equal("Deleted", finalized.Snapshot.ObjectState);
        Assert.Equal("Revoked", await KeyState(observer, h));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task IncompleteR2_ProcessKillRecoversDurablyToAvailableWithoutSecondPut(int cut)
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        await RunIncompleteR2ProcessKillCut(cut, minio, probe);
    }

    private async Task RunIncompleteR2ProcessKillCut(int cut, DurableObjectMinioFixture minio,
        Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe probe)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r2_full_kill");
        var lost = new LostPutResponseWriter(
            new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)));
        var (_, h, written, _) = await WriteIncompleteObject(isolated, lost);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(capability))
                    .AsEnumerable().Where(entry => entry.Value is not null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value)).ToArray();
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(
            logins.Connections, configs, h.SourceArtifactId, cut, false,
            KeyJournalConnection: observer.Database.GetConnectionString(), FullR2: true);

        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}",
                await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
            var reader = new RawSourceRetentionContinuationRepository(readSource);
            var beforeKill = await reader.ReadAsync(h.SourceArtifactId, default);
            Assert.NotNull(beforeKill);
            Assert.Equal(cut switch
            {
                0 => "PutOutcomeUnknown",
                1 => "ObjectPresentPendingVerification",
                _ => "VerifiedCompleted",
            }, beforeKill.ObjectState);
            Assert.Equal(cut switch
            {
                < 3 => "Reserved",
                < 5 => "Staged",
                _ => "Available",
            }, beforeKill.CustodyState);
            var killed = child.Id;
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(beforeKill, await reader.ReadAsync(h.SourceArtifactId, default));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(killed, restarted.Id);
                var output = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal("", await restartedErrors);
                Assert.Equal($"A3-AVAILABLE:{restarted.Id}", output.Trim());
                var available = await reader.ReadAsync(h.SourceArtifactId, default);
                Assert.Equal("Available", available!.CustodyState);
                Assert.Equal("Available", available.PublicationState);
                Assert.Equal(h.AttemptId, available.AttemptId);
                Assert.Equal(written.ObjectCustodyId, available.ObjectCustodyId);
                Assert.Equal(1, await ObjectCount(observer, h));
                Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
                    .CountAsync(value => value.SourceArtifactId == h.SourceArtifactId));
                Assert.Equal(1, lost.Calls);
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await errors);
    }

    [Fact]
    public async Task R2R6_ClusterProcessKillMatrixUsesSharedInfrastructureAndIsolatedSources()
    {
        // One MinIO server and one compiled child harness for the cluster. Each helper
        // creates its own disposable PostgreSQL database and source/object IDs.
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        for (var cut = 0; cut < 6; cut++)
        {
            try
            {
                await RunIncompleteR2ProcessKillCut(cut, minio, probe);
                output.WriteLine($"PASS full-R2 cut={cut} owner=Reconciler");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Full-R2 crash matrix failed at cut {cut}", error);
            }
        }
        for (var cut = 0; cut < 4; cut++)
        {
            try
            {
                await RunR6ProcessKillCut(cut, minio, probe);
                output.WriteLine($"PASS R6-cleanup cut={cut} owner=Lifecycle/Reconciler");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"R6 cleanup crash matrix failed at cut {cut}", error);
            }
        }
    }

    [Fact]
    public async Task TerminalIntent_PresentObjectIsDeletedBeforeKeyAndTi02()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_object_cleanup");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h, objectId) = await TerminalPendingObject(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var intent = await RecordIntent(writer, h, "Terminated", "CONTENT_COMMITMENT_MISMATCH");
        var initialObjectEvents = await ObjectEventCount(observer, objectId);
        var initialKeyEvents = await KeyEventCount(observer, h);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);

        Assert.Equal(RetainedContinuationStep.MarkTerminalObjectCleanup,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("CleanupPending", await ObjectState(observer, objectId));
        Assert.Equal(initialObjectEvents + 1, await ObjectEventCount(observer, objectId));
        Assert.Equal("Active", await KeyState(observer, h));

        Assert.Equal(RetainedContinuationStep.CleanupTerminalObject,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("Deleted", await ObjectState(observer, objectId));
        Assert.Equal(initialObjectEvents + 2, await ObjectEventCount(observer, objectId));
        Assert.Equal("Active", await KeyState(observer, h));

        Assert.Equal(RetainedContinuationStep.SettleTerminalResources,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("Revoked", await KeyState(observer, h));
        Assert.Equal(initialKeyEvents + 1, await KeyEventCount(observer, h));
        Assert.Equal("Deleted", await ObjectState(observer, objectId));

        var finalized = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.FinalizeIntent, finalized.Step);
        Assert.Equal(intent.TerminalIntentCode, finalized.Snapshot!.R2TerminalOutcomeCode);
        Assert.Equal("Terminated", finalized.Snapshot.R2TerminationDisposition);
        Assert.Equal(RetainedContinuationStep.None,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal(initialObjectEvents + 2, await ObjectEventCount(observer, objectId));
        Assert.Equal(initialKeyEvents + 1, await KeyEventCount(observer, h));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task TerminalIntent_PresentObjectProcessKillRecoversWithoutSecondPutOrDelete(int cut)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_object_cleanup_kill");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        var (_, h, objectId) = await TerminalPendingObject(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var intent = await RecordIntent(writer, h, "Terminated", "CONTENT_COMMITMENT_MISMATCH");
        var initialObjectEvents = await ObjectEventCount(observer, objectId);
        var initialKeyEvents = await KeyEventCount(observer, h);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(capability))
                    .AsEnumerable().Where(entry => entry.Value is not null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value)).ToArray();
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(
            logins.Connections, configs, h.SourceArtifactId, cut, false, Terminal: true,
            TerminalOutcomeCode: intent.TerminalIntentCode!);
        var expectedObjects = new[]
            { "ObjectPresentPendingVerification", "CleanupPending", "Deleted", "Deleted", "Deleted" };
        var expectedKeys = new[] { "Active", "Active", "Active", "Revoked", "Revoked" };

        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}",
                await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            Assert.Equal(expectedObjects[cut], await ObjectState(observer, objectId));
            Assert.Equal(expectedKeys[cut], await KeyState(observer, h));
            var durableBeforeKill = await Rows(observer);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(durableBeforeKill, await Rows(observer));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(child.Id, restarted.Id);
                var message = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal("", await restartedErrors);
                Assert.Equal($"A3-TERMINAL:{restarted.Id}", message.Trim());
                Assert.Equal("Deleted", await ObjectState(observer, objectId));
                Assert.Equal("Revoked", await KeyState(observer, h));
                Assert.Equal(initialObjectEvents + 2, await ObjectEventCount(observer, objectId));
                Assert.Equal(initialKeyEvents + 1, await KeyEventCount(observer, h));
                Assert.Equal(1, await observer.RawExportProvisionalObjects
                    .CountAsync(row => row.ObjectCustodyId == objectId));
                await using var continuationSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
                Assert.Equal(intent.TerminalIntentCode,
                    (await new RawSourceRetentionContinuationRepository(continuationSource)
                        .ReadAsync(h.SourceArtifactId, default))!.R2TerminalOutcomeCode);
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await errors);
    }

    [Fact]
    public async Task TerminalIntent_LostDeleteResponseUsesTwoAbsenceObservationsBeforeTi02()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_object_absence");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h, objectId) = await TerminalPendingObject(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var intent = await RecordIntent(writer, h, "Terminated", "CONTENT_COMMITMENT_MISMATCH");
        var initialObjectEvents = await ObjectEventCount(observer, objectId);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);

        Assert.Equal(RetainedContinuationStep.MarkTerminalObjectCleanup,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        await using (var lifecycleScope = await scopes.OpenLifecycleAsync(default))
        {
            var db = lifecycleScope.Services.GetRequiredService<TagEkycDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(Principal, default);
            var current = await repository.ReadObjectLifecycleContextAsync(objectId, default);
            await tx.CommitAsync();
            Assert.NotNull(current);
            var provider = lifecycleScope.Services.GetRequiredService<IProvisionalObjectLifecycle>();
            var lostAcknowledgement = await provider.DeleteExactAsync(new(current.ProvisionalObjectIdentity,
                current.ObjectKey, current.ObjectBindingDigest), default);
            Assert.Equal(ExactDeleteOutcome.DeletedAcknowledged, lostAcknowledgement.Outcome);
            // Deliberately discard the 204: database remains CleanupPending.
        }
        Assert.Equal("CleanupPending", await ObjectState(observer, objectId));

        Assert.Equal(RetainedContinuationStep.ConfirmTerminalObjectAbsence,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("Deleted", await ObjectState(observer, objectId));
        Assert.Equal("PositiveAbsenceConfirmed", await ObjectDeletionEvidenceKind(observer, objectId));
        Assert.Equal(initialObjectEvents + 2, await ObjectEventCount(observer, objectId));
        Assert.Equal(RetainedContinuationStep.SettleTerminalResources,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        var finalized = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.FinalizeIntent, finalized.Step);
        Assert.Equal(intent.TerminalIntentCode, finalized.Snapshot!.R2TerminalOutcomeCode);
    }

    [Fact]
    public async Task TerminalIntent_AbsenceMustRemainPositiveAcrossQuiescence()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_object_absence_race");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h, objectId) = await TerminalPendingObject(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        _ = await RecordIntent(writer, h, "Terminated", "CONTENT_COMMITMENT_MISMATCH");
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var normal = new CaptureRuntimeSourcePipeline(scopes);
        Assert.Equal(RetainedContinuationStep.MarkTerminalObjectCleanup,
            (await normal.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);

        SourceObjectLifecycleContext current;
        await using (var lifecycleScope = await scopes.OpenLifecycleAsync(default))
        {
            var db = lifecycleScope.Services.GetRequiredService<TagEkycDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(Principal, default);
            current = (await repository.ReadObjectLifecycleContextAsync(objectId, default))!;
            await tx.CommitAsync();
            var deleted = await lifecycleScope.Services.GetRequiredService<IProvisionalObjectLifecycle>()
                .DeleteExactAsync(new(current.ProvisionalObjectIdentity, current.ObjectKey,
                    current.ObjectBindingDigest), default);
            Assert.Equal(ExactDeleteOutcome.DeletedAcknowledged, deleted.Outcome);
        }

        var recreations = 0;
        var race = new CaptureRuntimeSourcePipeline(scopes, null, async token =>
        {
            await using var writerScope = await scopes.OpenWriterAsync(token);
            var bytes = "synthetic-reappeared-ciphertext"u8.ToArray();
            await using var body = new MemoryStream(bytes, writable: false);
            var put = await writerScope.Services.GetRequiredService<IProvisionalObjectWriter>()
                .PutIfAbsentAsync(new(new(current.ProvisionalObjectIdentity, current.ObjectKey,
                    current.ObjectBindingDigest), current.PutOperationId!.Value, bytes.Length), body, token);
            Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
            Interlocked.Increment(ref recreations);
        });
        var deferred = await race.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.None, deferred.Step);
        Assert.Equal(1, recreations);
        Assert.Equal("CleanupPending", await ObjectState(observer, objectId));
        Assert.Null(await ObjectDeletionEvidenceKindOrNull(observer, objectId));

        // Fresh CP08 now observes the object as present and uses Lifecycle
        // deletion; it never commits the stale first absence observation.
        Assert.Equal(RetainedContinuationStep.CleanupTerminalObject,
            (await normal.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("Deleted", await ObjectState(observer, objectId));
        Assert.Equal("DeleteAcknowledged", await ObjectDeletionEvidenceKind(observer, objectId));
    }

    [Fact]
    public async Task TerminalIntent_ActiveKeyIsRevokedByLifecycleBeforeTi02Finalization()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_active_key");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h) = await TerminalReadyObject(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var intent = await RecordIntent(writer, h, "TerminatedBeforeStart", "CONTENT_COMMITMENT_MISMATCH");
        var frozenAttempt = await FullAttempt(observer, h);
        var frozenObject = await ObjectRow(observer, h);
        var eventCount = await KeyEventCount(observer, h);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);

        var settled = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.SettleTerminalResources, settled.Step);
        Assert.Equal("Revoked", await KeyState(observer, h));
        Assert.Equal(eventCount + 1, await KeyEventCount(observer, h));
        Assert.True(await KeyRevocationEvidenceMatches(observer, h, intent.TerminalIntentCode!));
        Assert.Equal(frozenAttempt, await FullAttempt(observer, h));
        Assert.Equal(frozenObject, await ObjectRow(observer, h));
        Assert.Null(settled.Snapshot!.R2TerminalOutcomeCode);

        var finalized = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.FinalizeIntent, finalized.Step);
        Assert.Equal(intent.TerminalIntentCode, finalized.Snapshot!.R2TerminalOutcomeCode);
        Assert.Equal(eventCount + 1, await KeyEventCount(observer, h));
        Assert.Equal(frozenObject, await ObjectRow(observer, h));
        Assert.Equal(RetainedContinuationStep.None,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
    }

    [Fact]
    public async Task TerminalIntent_LostProviderResponseIsCleanedBeforeAbandonAndTi02()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_provider_cleanup");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var durable = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        var lostResponse = new LostWrapResponseProvider(durable);
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
            (await Provision(writer, h, lostResponse)).Outcome);
        Assert.Equal(1, lostResponse.WrapCalls);
        Assert.Equal("PreparingLive|Issued", await KeyRecoveryState(observer, h));

        var intent = await RecordIntent(writer, h, "TerminatedBeforeStart", "CONTENT_COMMITMENT_MISMATCH");
        Assert.Equal("Recorded", intent.Outcome);
        var initialEvents = await KeyEventCount(observer, h);
        Assert.Equal(0, await ObjectCount(observer, h));
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, durable);

        Assert.Equal(RetainedContinuationStep.RequestTerminalKeyAbandon,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("AbandonRequested|Issued", await KeyRecoveryState(observer, h));
        Assert.Equal(RetainedContinuationStep.ResolveTerminalKeyProvider,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("AbandonRequested|CleanupRequired", await KeyRecoveryState(observer, h));
        Assert.Equal(RetainedContinuationStep.CleanupTerminalKeyProvider,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("AbandonRequested|CleanedUp", await KeyRecoveryState(observer, h));
        Assert.Equal(RetainedContinuationStep.FinalizeTerminalKeyAbandon,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
        Assert.Equal("ReservationAbandoned|CleanedUp", await KeyRecoveryState(observer, h));
        Assert.Equal(RetainedContinuationStep.FinalizeIntent,
            (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);

        await using var continuationSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
        var final = (await new RawSourceRetentionContinuationRepository(continuationSource)
            .ReadAsync(h.SourceArtifactId, default))!;
        Assert.Equal(intent.TerminalIntentCode, final.R2TerminalOutcomeCode);
        Assert.Equal(intent.TerminalIntentDisposition, final.R2TerminationDisposition);
        Assert.Equal(initialEvents + 4, await KeyEventCount(observer, h));
        Assert.Equal(1, lostResponse.WrapCalls);
        Assert.Equal(0, await ObjectCount(observer, h));
        Assert.Contains("\"R2TerminalOutcomeCode\": \"CONTENT_COMMITMENT_MISMATCH\"",
            await FullAttempt(observer, h));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task TerminalIntent_LostProviderResponseProcessKillRecoversFromDurableCleanupState(int cut)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_provider_cleanup_kill");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        var (_, h) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var durable = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        var lostResponse = new LostWrapResponseProvider(durable);
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
            (await Provision(writer, h, lostResponse)).Outcome);
        var intent = await RecordIntent(writer, h, "TerminatedBeforeStart", "CONTENT_COMMITMENT_MISMATCH");
        var initialEvents = await KeyEventCount(observer, h);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(capability))
                    .AsEnumerable().Where(entry => entry.Value is not null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value)).ToArray();
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(
            logins.Connections, configs, h.SourceArtifactId, cut, false, Terminal: true,
            TerminalOutcomeCode: intent.TerminalIntentCode!,
            KeyJournalConnection: observer.Database.GetConnectionString()!);
        var expectedKeyState = new[]
        {
            "PreparingLive|Issued", "AbandonRequested|Issued",
            "AbandonRequested|CleanupRequired", "AbandonRequested|CleanedUp",
            "ReservationAbandoned|CleanedUp", "ReservationAbandoned|CleanedUp",
        };

        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}",
                await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            Assert.Equal(expectedKeyState[cut], await KeyRecoveryState(observer, h));
            await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
            var reader = new RawSourceRetentionContinuationRepository(readSource);
            var checkpoint = (await reader.ReadAsync(h.SourceArtifactId, default))!;
            Assert.Equal(intent.TerminalIntentCode, checkpoint.R2TerminalIntentCode);
            Assert.Equal(cut == 5 ? intent.TerminalIntentCode : null, checkpoint.R2TerminalOutcomeCode);
            var durableBeforeKill = await Rows(observer);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(durableBeforeKill, await Rows(observer));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(child.Id, restarted.Id);
                var message = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal("", await restartedErrors);
                Assert.Equal($"A3-TERMINAL:{restarted.Id}", message.Trim());
                Assert.Equal("ReservationAbandoned|CleanedUp", await KeyRecoveryState(observer, h));
                var final = (await reader.ReadAsync(h.SourceArtifactId, default))!;
                Assert.Equal(intent.TerminalIntentCode, final.R2TerminalOutcomeCode);
                Assert.Equal(intent.TerminalIntentDisposition, final.R2TerminationDisposition);
                Assert.Equal(initialEvents + 4, await KeyEventCount(observer, h));
                Assert.Equal(1, lostResponse.WrapCalls);
                Assert.Equal(0, await ObjectCount(observer, h));
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TerminalIntent_ActiveKeyProcessKillRecoversFromDurableResourceState(int cut)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_terminal_active_key_kill");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        var (_, h) = await TerminalReadyObject(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var intent = await RecordIntent(writer, h, "TerminatedBeforeStart", "CONTENT_COMMITMENT_MISMATCH");
        var frozenObject = await ObjectRow(observer, h);
        var frozenProvider = await ProviderOperationRows(observer, h);
        var eventCount = await KeyEventCount(observer, h);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(capability =>
                Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(capability))
                    .AsEnumerable().Where(entry => entry.Value is not null)
                    .ToDictionary(entry => entry.Key, entry => entry.Value)).ToArray();
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(
            logins.Connections, configs, h.SourceArtifactId, cut, false, Terminal: true,
            TerminalOutcomeCode: intent.TerminalIntentCode!);

        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}",
                await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            Assert.Equal(cut == 0 ? "Active" : "Revoked", await KeyState(observer, h));
            await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
            var checkpointReader = new RawSourceRetentionContinuationRepository(readSource);
            var checkpoint = (await checkpointReader.ReadAsync(h.SourceArtifactId, default))!;
            Assert.Equal(intent.TerminalIntentCode, checkpoint.R2TerminalIntentCode);
            Assert.Equal(intent.TerminalIntentDisposition, checkpoint.R2TerminalIntentDisposition);
            Assert.Equal(intent.TerminalIntentAtUtc, checkpoint.R2TerminalIntentAtUtc);
            Assert.Equal(cut == 2 ? intent.TerminalIntentCode : null, checkpoint.R2TerminalOutcomeCode);
            Assert.Equal(cut == 2 ? intent.TerminalIntentDisposition : null, checkpoint.R2TerminationDisposition);
            Assert.Equal(cut == 2, checkpoint.R2TerminatedAtUtc is not null);
            if (cut >= 1)
                Assert.True(await KeyRevocationEvidenceMatches(observer, h, intent.TerminalIntentCode!));
            var beforeKillAttempt = await FullAttempt(observer, h);
            var beforeKillRows = await Rows(observer);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(beforeKillAttempt, await FullAttempt(observer, h));
            Assert.Equal(beforeKillRows, await Rows(observer));

            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(child.Id, restarted.Id);
                var message = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal("", await restartedErrors);
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal($"A3-TERMINAL:{restarted.Id}", message.Trim());
                Assert.Equal("Revoked", await KeyState(observer, h));
                Assert.Equal(eventCount + 1, await KeyEventCount(observer, h));
                Assert.True(await KeyRevocationEvidenceMatches(observer, h, intent.TerminalIntentCode!));
                Assert.Equal(frozenObject, await ObjectRow(observer, h));
                Assert.Equal(frozenProvider, await ProviderOperationRows(observer, h));
                var final = await FullAttempt(observer, h);
                Assert.Contains($"\"R2TerminalIntentCode\": \"{intent.TerminalIntentCode}\"", final);
                Assert.Contains($"\"R2TerminalOutcomeCode\": \"{intent.TerminalIntentCode}\"", final);
            }
            finally
            {
                if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); }
            }
        }
        finally
        {
            if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
        }
        Assert.Equal("", await errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task NoProviderRecovery_ProcessKillUsesDurableNpsIntentAndFinalization(int cut)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_no_provider_kill");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var probe = await Tip88C1C6BA3ContinuationWorkerTests.ContinuationProbe.Compile();
        await Prepare(isolated);
        var (_, h) = await CommitPreparedR1(isolated, lifetimeSeconds: 40, attemptDeadlineSeconds: 5);
        await using var observer = isolated.CreateDbContext();
        var horizon = await OriginalHorizon(observer, h);
        var resources = await Rows(observer);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        var configs = new[] { ProvisionalObjectCapability.Writer, ProvisionalObjectCapability.Reconciler,
            ProvisionalObjectCapability.Lifecycle }.Select(x => Tip88C1C6BA3SyntheticComposition.ObjectConfiguration(minio.Options(x))
                .AsEnumerable().Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value)).ToArray();
        await PastLease(observer, h);
        if (cut >= 2) await WaitForHorizon(observer, horizon);
        var input = new Tip88C1C6BA3ContinuationWorkerTests.ContinuationChildInput(logins.Connections, configs,
            h.SourceArtifactId, cut, false, Terminal: true);
        await using var readSource = NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
        var reader = new RawSourceRetentionContinuationRepository(readSource);
        using var child = probe.Start(input);
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal($"A3-CUT:{child.Id}:{cut}", await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40)));
            Assert.NotEqual(Environment.ProcessId, child.Id);
            Assert.False(child.HasExited);
            var checkpoint = (await reader.ReadAsync(h.SourceArtifactId, default))!;
            Assert.Equal(cut >= 1 ? "TerminatedBeforeStart" : null, checkpoint.R2TerminationDisposition);
            Assert.Equal(cut >= 2 ? "RECAPTURE_REQUIRED" : null, checkpoint.R2TerminalIntentCode);
            Assert.Equal(cut >= 3 ? "RECAPTURE_REQUIRED" : null, checkpoint.R2TerminalOutcomeCode);
            var frozenAttempt = await FullAttempt(observer, h);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, child.ExitCode);
            Assert.Equal(frozenAttempt, await FullAttempt(observer, h));
            Assert.Equal(resources, await Rows(observer));
            await WaitForHorizon(observer, horizon);
            using var restarted = probe.Start(input with { Recover = true });
            var restartedErrors = restarted.StandardError.ReadToEndAsync();
            try
            {
                Assert.NotEqual(child.Id, restarted.Id);
                var message = await restarted.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(40));
                await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(0, restarted.ExitCode);
                Assert.Equal($"A3-TERMINAL:{restarted.Id}", message.Trim());
                Assert.Equal("", await restartedErrors);
                var after = (await reader.ReadAsync(h.SourceArtifactId, default))!;
                Assert.Equal("RECAPTURE_REQUIRED", after.R2TerminalIntentCode);
                Assert.Equal(after.R2TerminalIntentCode, after.R2TerminalOutcomeCode);
                Assert.Equal("TerminatedBeforeStart", after.R2TerminationDisposition);
                Assert.True(after.R2TerminalIntentAtUtc >= horizon);
                Assert.Equal(checkpoint.AttemptId, after.AttemptId);
                Assert.Equal(checkpoint.AttemptKeyReservationId, after.AttemptKeyReservationId);
                Assert.Equal(checkpoint.Fence, after.Fence);
                Assert.Equal(checkpoint.EncryptionAttemptRevision, after.EncryptionAttemptRevision);
                if (cut >= 1) Assert.Equal(checkpoint.R2TerminatedAtUtc, after.R2TerminatedAtUtc);
                if (cut >= 2) Assert.Equal(checkpoint.R2TerminalIntentAtUtc, after.R2TerminalIntentAtUtc);
                Assert.Null(after.ObjectCustodyId);
                Assert.Null(after.SourcePublicationId);
                Assert.Equal(horizon, await OriginalHorizon(observer, h));
                Assert.Equal(resources, await Rows(observer));
                await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
                var pipeline = new CaptureRuntimeSourcePipeline(scopes);
                var finalAttempt = await FullAttempt(observer, h);
                Assert.Equal(RetainedContinuationStep.None, (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Step);
                Assert.Equal(finalAttempt, await FullAttempt(observer, h));
                Assert.DoesNotContain(h.SourceArtifactId, await pipeline.ScanAsync(null, default));
            }
            finally { if (!restarted.HasExited) { restarted.Kill(true); await restarted.WaitForExitAsync(); } }
        }
        finally { if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } }
        Assert.Equal("", await errors);
    }

    [Fact]
    public async Task NoProviderRecovery_LiveLeaseAndLiveOriginalHorizonRemainNonterminal()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_no_provider_live");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await Prepare(isolated);
        var (_, h) = await CommitPreparedR1(isolated, lifetimeSeconds: 40, attemptDeadlineSeconds: 5);
        await using var observer = isolated.CreateDbContext();
        var horizon = await OriginalHorizon(observer, h);
        var original = await FullAttempt(observer, h);
        var resources = await Rows(observer);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);
        // Call the real typed NPS boundary while the original lease is live.
        await using var role = await scopes.OpenReconcilerAsync(default);
        var recorder = new RawExportR2TerminalIntentRecorder(role.Services.GetRequiredService<NpgsqlDataSource>(), Principal, 1000, default);
        var live = await recorder.TerminateBeforeProviderStartAsync(h.SourceArtifactId, h.AttemptId,
            h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default);
        Assert.Equal(RetainedNoStartOutcome.LeaseLive, live.Outcome);
        Assert.Equal(original, await FullAttempt(observer, h));
        await PastLease(observer, h);
        var nps = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ProveNoProviderStart, nps.Step);
        Assert.Equal("TerminatedBeforeStart", nps.Snapshot!.R2TerminationDisposition);
        var witnessed = await FullAttempt(observer, h);
        Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp() < {horizon} AS \"Value\"").SingleAsync());
        var denied = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.RecordExpiredIntent, denied.Step);
        Assert.Null(denied.Snapshot!.R2TerminalIntentCode);
        Assert.Equal(witnessed, await FullAttempt(observer, h));
        await WaitForHorizon(observer, horizon);
        Assert.Equal("RECAPTURE_REQUIRED", (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Snapshot!.R2TerminalIntentCode);
        Assert.Equal("RECAPTURE_REQUIRED", (await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default)).Snapshot!.R2TerminalOutcomeCode);
        Assert.Equal(resources, await Rows(observer));
        Assert.Equal(horizon, await OriginalHorizon(observer, h));
    }

    [Fact]
    public async Task NoProviderRecovery_UnknownProviderDoesNotBecomeAnAbsenceWitness()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_no_provider_unknown");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var provider = new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown, (await Provision(writer, h, provider)).Outcome);
        Assert.Equal(1, provider.Calls);
        await using var observer = isolated.CreateDbContext();
        var original = await FullAttempt(observer, h);
        var resources = await Rows(observer);
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var pipeline = new CaptureRuntimeSourcePipeline(scopes);
        await PastLease(observer, h);
        var result = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.ProveNoProviderStart, result.Step);
        Assert.Null(result.Snapshot!.R2TerminationDisposition);
        Assert.Null(result.Snapshot.R2TerminalIntentCode);
        Assert.Equal(original, await FullAttempt(observer, h));
        Assert.Equal(resources, await Rows(observer));
        Assert.Equal(1, provider.Calls);
    }

    private static async Task WaitForHorizon(TagEkycDbContext observer, DateTimeOffset horizon)
    {
        var remaining = await observer.Database.SqlQuery<double>($"SELECT greatest(0,extract(epoch FROM ({horizon}-clock_timestamp())))::double precision AS \"Value\"").SingleAsync();
        Assert.InRange(remaining, 0, 40.1);
        await Task.Delay(TimeSpan.FromSeconds(remaining + 0.1));
        Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp()>={horizon} AS \"Value\"").SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoProviderRecovery_ReaderRejectsMalformedSqlResults(bool finalize)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_recovery_shape");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (_, h) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var role = await scopes.OpenReconcilerAsync(default);
        var recorder = new RawExportR2TerminalIntentRecorder(role.Services.GetRequiredService<NpgsqlDataSource>(), Principal, 1000, default);
        await using var admin = new NpgsqlConnection(observer.Database.GetConnectionString());
        await admin.OpenAsync();
        await using var definition = new NpgsqlCommand("SELECT pg_get_functiondef(to_regprocedure($1))", admin);
        definition.Parameters.AddWithValue(finalize ? "tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint)"
            : "tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)");
        var original = (string)(await definition.ExecuteScalarAsync())!;
        var body = original.IndexOf("AS $function$", StringComparison.Ordinal);
        Assert.True(body > 0);
        var frozen = await FullAttempt(observer, h);
        string failure = finalize ? "'CleanupPending'::text,NULL::text" : "'StateConflict'::text,NULL::text,NULL::timestamptz";
        string success = finalize ? "'ExistingMatch'::text,'CONTENT_COMMITMENT_MISMATCH'::text"
            : "'ExistingMatch'::text,'TerminatedBeforeStart'::text,clock_timestamp()";
        try
        {
            // Fault injection is confined to the disposable DB. These controls
            // prove the production parser, not that fabricated success persists.
            await Install("RETURN QUERY SELECT " + failure + ";");
            await Invoke();
            await Install("RETURN QUERY SELECT " + success + ";");
            await Invoke();
            string[] invalid = finalize
                ? ["'CleanupPending'::text,'CONTENT_COMMITMENT_MISMATCH'::text", "'Finalized'::text,NULL::text",
                    "'Finalized'::text,'RECAPTURE_REQUIRED'::text", "'ExistingMatch'::text,'RECAPTURE_REQUIRED'::text",
                    "'Unknown'::text,NULL::text"]
                : ["'LeaseLive'::text,'TerminatedBeforeStart'::text,NULL::timestamptz",
                    "'ProviderEvidencePresent'::text,NULL::text,clock_timestamp()",
                    "'TerminatedBeforeStart'::text,NULL::text,clock_timestamp()",
                    "'ExistingMatch'::text,'TerminatedBeforeStart'::text,NULL::timestamptz",
                    "'ExistingMatch'::text,'Terminated'::text,clock_timestamp()", "'Unknown'::text,NULL::text,NULL::timestamptz"];
            foreach (var tuple in invalid)
            {
                await Install("RETURN QUERY SELECT " + tuple + ";");
                await InvalidResponse();
            }
            await Install("RETURN;");
            await InvalidResponse();
            await Install("RETURN QUERY SELECT " + failure + "; RETURN QUERY SELECT " + failure + ";");
            await InvalidResponse();
            Assert.Equal(frozen, await FullAttempt(observer, h));
        }
        finally
        {
            await using var restore = new NpgsqlCommand(original, admin);
            await restore.ExecuteNonQueryAsync();
        }
        // Real function restored and invoked again with the same qualified role.
        if (finalize)
            Assert.Equal(RetainedTerminalFinalizeOutcome.StateConflict, (await recorder.FinalizeAsync(h.SourceArtifactId,
                h.AttemptId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, "CONTENT_COMMITMENT_MISMATCH", default)).Outcome);
        else
        {
            await PastLease(observer, h);
            Assert.Equal(RetainedNoStartOutcome.TerminatedBeforeStart, (await recorder.TerminateBeforeProviderStartAsync(
                h.SourceArtifactId, h.AttemptId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default)).Outcome);
        }
        async Task Invoke()
        {
            if (finalize) await recorder.FinalizeAsync(h.SourceArtifactId, h.AttemptId, h.ExpectedEncryptionAttemptRevision,
                h.ExpectedFence, "CONTENT_COMMITMENT_MISMATCH", default);
            else await recorder.TerminateBeforeProviderStartAsync(h.SourceArtifactId, h.AttemptId,
                h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default);
        }
        async Task InvalidResponse()
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(Invoke);
            Assert.Equal("A3_R2_TERMINAL_INTENT_RESULT_INVALID", error.Message);
        }
        async Task Install(string statements)
        {
            await using var change = new NpgsqlCommand(original[..body] + "AS $function$ BEGIN " + statements + " END $function$;", admin);
            await change.ExecuteNonQueryAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoProviderRecovery_CommitFailureNeverAcknowledges(bool finalize)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_recovery_commit");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await Prepare(isolated);
        var (_, h) = await CommitPreparedR1(isolated, lifetimeSeconds: 40, attemptDeadlineSeconds: 5);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var role = await scopes.OpenReconcilerAsync(default);
        var source = role.Services.GetRequiredService<NpgsqlDataSource>();
        var recorder = new RawExportR2TerminalIntentRecorder(source, Principal, 3000, default);
        await PastLease(observer, h);
        if (finalize)
        {
            Assert.Equal(RetainedNoStartOutcome.TerminatedBeforeStart, (await recorder.TerminateBeforeProviderStartAsync(
                h.SourceArtifactId, h.AttemptId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default)).Outcome);
            await WaitForHorizon(observer, await OriginalHorizon(observer, h));
            Assert.Equal(RawExportR2TerminalIntentOutcome.Recorded, await recorder.RecordAsync(h.SourceArtifactId, h.AttemptId,
                h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, "TerminatedBeforeStart", "RECAPTURE_REQUIRED", default));
        }
        var before = await FullAttempt(observer, h);
        var resources = await Rows(observer);
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_recovery_commit_failure() RETURNS trigger LANGUAGE plpgsql AS $proof$
            BEGIN RAISE EXCEPTION 'A3_TEST_RECOVERY_COMMIT_FAILURE'; END $proof$;
            CREATE CONSTRAINT TRIGGER a3_test_recovery_commit_failure
             AFTER UPDATE ON tagekyc.raw_export_source_encryption_attempts DEFERRABLE INITIALLY DEFERRED
             FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_recovery_commit_failure();
            """);
        try
        {
            var error = await Assert.ThrowsAsync<PostgresException>(Invoke);
            Assert.Equal("A3_TEST_RECOVERY_COMMIT_FAILURE", error.MessageText);
            Assert.Equal(before, await FullAttempt(observer, h));
            Assert.Equal(resources, await Rows(observer));
        }
        finally
        {
            await observer.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER a3_test_recovery_commit_failure ON tagekyc.raw_export_source_encryption_attempts;
                DROP FUNCTION tagekyc.a3_test_recovery_commit_failure();
                """);
        }
        // An ambient caller rollback cannot roll back an acknowledged recovery.
        using (var ambient = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            await Invoke();
        Assert.NotEqual(before, await FullAttempt(observer, h));
        Assert.Equal(resources, await Rows(observer));
        var committed = await FullAttempt(observer, h);
        await Invoke();
        Assert.Equal(committed, await FullAttempt(observer, h));
        await using var checkout = await source.OpenConnectionAsync();
        await using var actor = new NpgsqlCommand("SELECT current_setting('tagekyc.actor_principal_id',true)", checkout);
        Assert.True(await actor.ExecuteScalarAsync() is null or DBNull or "");
        async Task Invoke()
        {
            if (finalize)
            {
                var result = await recorder.FinalizeAsync(h.SourceArtifactId, h.AttemptId,
                    h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, "RECAPTURE_REQUIRED", default);
                Assert.Contains(result.Outcome, new[] { RetainedTerminalFinalizeOutcome.Finalized, RetainedTerminalFinalizeOutcome.ExistingMatch });
            }
            else
            {
                var result = await recorder.TerminateBeforeProviderStartAsync(h.SourceArtifactId, h.AttemptId,
                    h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default);
                Assert.Contains(result.Outcome, new[] { RetainedNoStartOutcome.TerminatedBeforeStart, RetainedNoStartOutcome.ExistingMatch });
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoProviderRecovery_HostStopOrDeadlineRollsBackBlockedTransaction(bool stopHost)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_recovery_stop");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var (scope, h) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var role = await scopes.OpenReconcilerAsync(default);
        var source = role.Services.GetRequiredService<NpgsqlDataSource>();
        var before = await FullAttempt(observer, h);
        var resources = await Rows(observer);
        await using var blocker = isolated.CreateDbContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        using var host = new CancellationTokenSource();
        var recorder = new RawExportR2TerminalIntentRecorder(source, Principal, stopHost ? 30000 : 1500, host.Token);
        var pending = recorder.TerminateBeforeProviderStartAsync(h.SourceArtifactId, h.AttemptId,
            h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default);
        try
        {
            // This qualified fixture deliberately disables pooling. Find the
            // active recovery backend, not the PID of a closed warm-up socket.
            var pid = 0;
            for (var i = 0; i < 20 && pid == 0; i++)
            {
                var waiting = await observer.Database.SqlQuery<int>($"""
                    SELECT a.pid AS "Value" FROM pg_catalog.pg_stat_activity a
                    WHERE a.datname=current_database() AND a.usename={CaptureRuntimeCustodyProviderScopes.ReconcilerLogin}
                     AND a.wait_event_type='Lock'
                     AND a.query LIKE '%raw_export_terminate_retained_r2_before_provider_start%'
                    """).ToArrayAsync();
                if (waiting.Length > 0) pid = Assert.Single(waiting);
                else await Task.Delay(25);
            }
            Assert.True(pid > 0, "The real recovery transaction must be observed waiting.");
            await WaitAtSession(observer, pid, scope.Session);
            if (stopHost) host.Cancel();
            Assert.Same(pending, await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(5))));
            await Assert.ThrowsAnyAsync<Exception>(async () => await pending);
            Assert.Equal(before, await FullAttempt(observer, h));
            Assert.Equal(resources, await Rows(observer));
        }
        finally
        {
            host.Cancel();
            await transaction.RollbackAsync();
            try { await pending.WaitAsync(TimeSpan.FromSeconds(5)); } catch (Exception) { }
        }
        await PastLease(observer, h);
        var fresh = new RawExportR2TerminalIntentRecorder(source, Principal, 3000, default);
        Assert.Equal(RetainedNoStartOutcome.TerminatedBeforeStart, (await fresh.TerminateBeforeProviderStartAsync(
            h.SourceArtifactId, h.AttemptId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, default)).Outcome);
    }

    [Theory]
    [InlineData("declared")]
    [InlineData("class")]
    public Task ActualDeclaredAndClassOverflow_StopFirstExcess(string mode) => InputObservationProof(mode);

    [Fact]
    public void ActualCounter_Int64BoundaryCannotWrap()
    {
        Assert.False(RawExportR2FramedCiphertextStream.ExceedsPlaintextLimit(long.MaxValue - 1, 1, long.MaxValue));
        Assert.True(RawExportR2FramedCiphertextStream.ExceedsPlaintextLimit(long.MaxValue - 1, 2, long.MaxValue));
        Assert.True(RawExportR2FramedCiphertextStream.ExceedsPlaintextLimit(long.MaxValue, 1, long.MaxValue));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("interrupted")]
    [InlineData("cancelled")]
    public Task CleanShortEofDiffersFromInterruptedPrefix(string mode) => InputObservationProof(mode);

    [Theory]
    [InlineData("match")]
    [InlineData("changed")]
    public Task ExactLengthChangedBytes_RecordMismatch(string mode) => InputObservationProof(mode);

    [Theory]
    [InlineData("commitment-unavailable")]
    [InlineData("commitment-throws")]
    public Task CommitmentProviderUnavailableIsNotDifferentMac(string mode) => InputObservationProof(mode);

    [Fact]
    public Task InputEncryptionFailureCannotOverwriteInputCause() => InputObservationProof("encryption-fails");

    [Theory]
    [InlineData("recorder-denied")]
    [InlineData("recorder-commit-fails")]
    public Task InputIntentFailureNeverAcknowledgesOrResumes(string mode) => InputObservationProof(mode);

    [Fact]
    public Task InputReadCannotUnwindBeforeTerminalIntentAcknowledgement() => InputObservationProof("recorder-gated");

    [Theory]
    [InlineData("null-tag")]
    [InlineData("unknown-tag")]
    [InlineData("historical-shape-on-current-schema")]
    public async Task EncryptionContext_CurrentSchemaNeverFallsBackToUntaggedAuthority(string mutation)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_reader_authority");
        var (_, h) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated, (await Provision(writer, h, kek)).Outcome);
        var repository = new RawExportR2Repository(writer);
        Assert.Equal("SourceRetention", (await repository.ReadEncryptionContextAsync(h.AttemptId,
            h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, CancellationToken.None))!.AuthorityKind);
        await using var owner = isolated.CreateDbContext();
        var definition = await owner.Database.SqlQueryRaw<string>("""
            SELECT pg_get_functiondef('tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)'::regprocedure) AS "Value"
            """).SingleAsync();
        var altered = mutation == "historical-shape-on-current-schema"
            ? definition.Replace(", \"AuthorityKind\" text)", ")", StringComparison.Ordinal)
                .Replace(",snapshot.\"AuthorityKind\"::text", "", StringComparison.Ordinal)
            : definition.Replace("snapshot.\"AuthorityKind\"::text", mutation == "null-tag" ? "NULL::text" : "'Unknown'::text", StringComparison.Ordinal);
        Assert.NotEqual(definition, altered);
        await owner.Database.ExecuteSqlRawAsync("DROP FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)");
        await owner.Database.ExecuteSqlRawAsync(altered);
        await owner.Database.ExecuteSqlRawAsync("""
            ALTER FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) TO tagekyc_raw_export_custody_encryptor;
            """);
        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ReadEncryptionContextAsync(
            h.AttemptId,h.ExpectedEncryptionAttemptRevision,h.ExpectedFence,CancellationToken.None));
    }

    [Theory]
    [InlineData("match")]
    [InlineData("changed")]
    [InlineData("record-put-fails")]
    [InlineData("missing-recorder")]
    [InlineData("missing-bound")]
    public async Task Writer_PreservesObservationAcrossProviderCatchAndPersistenceFailure(string mode)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_writer_observation");
        var (_, h) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var observer = isolated.CreateDbContext();
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        await using var source = RecorderSource(isolated);
        var recorder = new RawExportR2TerminalIntentRecorder(source, Principal, 3000, CancellationToken.None);
        var provider = new ObservationPutAdapter(async () =>
        {
            // The actual writer catches the adapter's replacement exception.
            Assert.Equal("CONTENT_COMMITMENT_MISMATCH", await observer.Database.SqlQuery<string>($"""
                SELECT "R2TerminalIntentCode" AS "Value" FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptId"={h.AttemptId}
                """).SingleAsync());
            if (mode == "record-put-fails")
                await writer.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_runtime");
        });
        var bytes = "synthetic-retainedsource"u8.ToArray();
        if (mode is "changed" or "record-put-fails") bytes[0] ^= 1;
        await using var input = new ObservedInputStream(bytes, mode);
        var result = await new RawExportR2EncryptionOrchestrator(new RawExportR2Repository(writer),
            new PostgresAttemptKeyReservationProvider(writer, new PostgresKeyProviderOperationMap(writer), kek),
            new AttemptAeadEncryptionOperationService(writer, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            services.GetRequiredService<IContentCommitmentService>(), provider,
            mode == "missing-recorder" ? null : recorder, mode == "missing-bound" ? null : 24)
            .ExecuteAsync(new(Principal, h.AttemptKeyReservationId, h.AttemptId, h.SourceArtifactId,
                h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, input), CancellationToken.None);
        var missing = mode is "missing-recorder" or "missing-bound";
        Assert.Equal(missing ? RawExportR2WriterDisposition.PreCustodyRejected : RawExportR2WriterDisposition.ReconciliationRequired, result.Disposition);
        Assert.Equal(new RawExportR2InputObservation(missing ? RawExportR2InputCompletionKind.NotCompleted
            : mode == "match" ? RawExportR2InputCompletionKind.CompleteMatch : RawExportR2InputCompletionKind.ContentCommitmentMismatch,
            mode is "changed" or "record-put-fails"), result.InputObservation);
        Assert.Equal(missing ? 0 : 1, provider.Calls);
        Assert.Equal(missing ? 0 : 24, input.BytesRead);
        Assert.Equal(missing ? 0 : 1, await observer.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_provisional_objects WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        if (!missing)
            Assert.Equal(mode == "record-put-fails" ? "PutInFlight" : "PutOutcomeUnknown", result.ObjectState);
        Assert.True(input.CanRead); // Writer never owns caller plaintext.
    }

    // This is a provider catch boundary, not proof of durable object storage.
    private sealed class ObservationPutAdapter(Func<Task> afterInputFailure) : IProvisionalObjectWriter
    {
        internal int Calls { get; private set; }
        public async Task<ConditionalPutResult> PutIfAbsentAsync(ExactWriteRequest request, Stream ciphertext, CancellationToken token)
        {
            Calls++;
            var buffer = new byte[4096];
            long read = 0;
            try
            {
                while (read < request.CiphertextLength)
                {
                    var count = await ciphertext.ReadAsync(buffer.AsMemory(), token);
                    Assert.True(count > 0);
                    read += count;
                }
            }
            catch
            {
                await afterInputFailure();
                throw new IOException("synthetic-sdk-replacement-exception");
            }
            return new(ConditionalPutOutcome.OutcomeUnknown, null, 0, null, null);
        }
    }

    private sealed class LostPutResponseWriter(IProvisionalObjectWriter inner) : IProvisionalObjectWriter
    {
        internal int Calls { get; private set; }
        internal ConditionalPutResult? InnerResult { get; private set; }

        public async Task<ConditionalPutResult> PutIfAbsentAsync(
            ExactWriteRequest request, Stream ciphertext, CancellationToken token)
        {
            Calls++;
            InnerResult = await inner.PutIfAbsentAsync(request, ciphertext, token);
            Assert.Equal(ConditionalPutOutcome.Created, InnerResult.Outcome);
            // Simulate a response lost after the provider committed the exact object.
            return new(ConditionalPutOutcome.OutcomeUnknown, null,
                InnerResult.BytesConsumed, null, null);
        }
    }

    private async Task InputObservationProof(string mode)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_input_observation");
        var (_, h) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),
            new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated, (await Provision(writer, h, kek)).Outcome);
        var repository = new RawExportR2Repository(writer);
        var context = Assert.IsType<RawExportR2EncryptionContext>(await repository.ReadEncryptionContextAsync(
            h.AttemptId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, CancellationToken.None));
        Assert.Equal("SourceRetention", context.AuthorityKind);
        var begun = await repository.BeginAsync(h.AttemptId, h.ExpectedEncryptionAttemptRevision,
            h.ExpectedFence, CancellationToken.None);
        Assert.Equal("Created", begun.Mutation.OutcomeCode);
        Assert.Equal("Armed", (await repository.ArmAsync(begun.Mutation.ObjectCustodyId,
            begun.Mutation.StateRevision, Guid.NewGuid(), CancellationToken.None)).OutcomeCode);
        await using var observer = isolated.CreateDbContext();
        var before = await AttemptWithoutIntent(observer, h);
        var fullBeforeInput = await FullAttempt(observer, h);
        await using var recorderSource = RecorderSource(isolated);
        if (mode == "recorder-commit-fails")
            await observer.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION public.a3_input_commit_failure() RETURNS trigger LANGUAGE plpgsql AS $f$
                BEGIN RAISE EXCEPTION 'A3_INPUT_COMMIT_FAILED'; END $f$;
                CREATE CONSTRAINT TRIGGER a3_input_commit_failure AFTER UPDATE ON tagekyc.raw_export_source_encryption_attempts
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                WHEN (OLD."R2TerminalIntentCode" IS NULL AND NEW."R2TerminalIntentCode" IS NOT NULL)
                EXECUTE FUNCTION public.a3_input_commit_failure();
                """);
        var recorder = new CountingInputRecorder(new RawExportR2TerminalIntentRecorder(recorderSource,
            mode == "recorder-denied" ? RecorderBaselineActor : Principal, 3000, CancellationToken.None), mode == "recorder-gated");
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var commitments = new InputCommitmentProvider(services.GetRequiredService<IContentCommitmentService>(), mode);
        var encryption = new CountingInputEncryption(new AttemptAeadEncryptionOperationService(writer, kek,
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager())), mode == "encryption-fails");
        var bytes = "synthetic-retainedsource"u8.ToArray();
        if (mode == "declared") bytes = [..bytes, 17, 18, 19];
        if (mode is "short" or "interrupted" or "cancelled") bytes = bytes[..^1];
        if (mode is "changed" or "recorder-denied" or "recorder-commit-fails" or "recorder-gated") bytes[0] ^= 1;
        await using var input = new ObservedInputStream(bytes, mode);
        await using var framed = new RawExportR2FramedCiphertextStream(input, context, begun.ObjectBindingDigest,
            encryption, commitments, terminalIntentRecorder: recorder, classMaximumBytes: mode == "class" ? 8 : 24);
        var buffer = new byte[mode == "match" ? 1 : 4096];
        long delivered = 0;
        Exception? failure = null;
        async Task Consume()
        {
            try
            {
                // Deliberately stop after the final byte, without an extra EOF read.
                while (delivered < framed.ExpectedCiphertextLength)
                {
                    var count = await framed.ReadAsync(buffer.AsMemory());
                    Assert.True(count > 0);
                    delivered += count;
                    if (mode == "match" && delivered < framed.ExpectedCiphertextLength)
                        Assert.Equal(default, framed.InputObservation);
                }
            }
            catch (Exception ex) { failure = ex; } // Models a provider swallowing the exception.
        }
        var consuming = Consume();
        if (mode == "recorder-gated")
        {
            try
            {
                await recorder.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                // Hold the recorder gate across a bounded interval: merely checking immediately
                // after Entered can race a fire-and-forget recorder's unwind continuation.
                await Assert.ThrowsAsync<TimeoutException>(() => consuming.WaitAsync(TimeSpan.FromMilliseconds(250)));
                Assert.False(consuming.IsCompleted);
                Assert.Equal(new RawExportR2InputObservation(RawExportR2InputCompletionKind.ContentCommitmentMismatch, false), framed.InputObservation);
                Assert.Equal(fullBeforeInput, await FullAttempt(observer, h));
            }
            finally { recorder.Release.TrySetResult(); }
        }
        await consuming.WaitAsync(TimeSpan.FromSeconds(10));
        var expected = mode switch
        {
            "match" => RawExportR2InputCompletionKind.CompleteMatch,
            "declared" or "class" => RawExportR2InputCompletionKind.ActualLimitExceeded,
            "short" => RawExportR2InputCompletionKind.CleanShortEof,
            "interrupted" or "cancelled" => RawExportR2InputCompletionKind.TransportInterrupted,
            "commitment-unavailable" or "commitment-throws" => RawExportR2InputCompletionKind.CommitmentProviderUnavailable,
            "encryption-fails" => RawExportR2InputCompletionKind.EncryptionProviderFailure,
            _ => RawExportR2InputCompletionKind.ContentCommitmentMismatch,
        };
        var durable = mode is "declared" or "class" or "short" or "changed" or "recorder-gated";
        Assert.Equal(new RawExportR2InputObservation(expected, durable), framed.InputObservation);
        var code = mode is "declared" or "class" ? "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED" : "CONTENT_COMMITMENT_MISMATCH";
        var intent = await observer.Database.SqlQuery<string>($"""
            SELECT COALESCE("R2TerminalIntentCode",'none') AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        Assert.Equal(durable ? code : "none", intent);
        Assert.Equal(before, await AttemptWithoutIntent(observer, h));
        Assert.Equal(durable || mode.StartsWith("recorder-", StringComparison.Ordinal) ? 1 : 0, recorder.Calls);
        if (durable)
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT "R2TerminalIntentDisposition"='Terminated' AND "R2TerminalIntentAtUtc" IS NOT NULL
                 AND "R2TerminalOutcomeCode" IS NULL AND "R2TerminatedAtUtc" IS NULL AS "Value"
                FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
                """).SingleAsync());
        if (mode == "match")
        {
            Assert.Null(failure);
            Assert.Equal(framed.ExpectedCiphertextLength, delivered);
            Assert.False(framed.Completed); // Legacy EOF-based contract stays unchanged.
            Assert.Equal(2, encryption.Calls);
            Assert.Equal(0, await framed.ReadAsync(buffer.AsMemory()));
            Assert.True(framed.Completed);
        }
        else
        {
            Assert.NotNull(failure);
            Assert.True(delivered < framed.ExpectedCiphertextLength);
            Assert.False(framed.Completed);
            var observed = (input.ReadCalls, input.BytesRead, encryption.Calls, commitments.Calls, recorder.Calls);
            var frozen = await FullAttempt(observer, h);
            var repeatedFailure = await Xunit.Record.ExceptionAsync(() => framed.ReadAsync(buffer.AsMemory()).AsTask());
            Assert.Equal(observed, (input.ReadCalls, input.BytesRead, encryption.Calls, commitments.Calls, recorder.Calls));
            Assert.Equal(frozen, await FullAttempt(observer, h));
            Assert.Equal(new RawExportR2InputObservation(expected, durable), framed.InputObservation);
            Assert.IsType<IOException>(repeatedFailure);
        }
        Assert.Equal(mode switch { "declared" => 25, "class" => 9, "short" or "interrupted" or "cancelled" => 23, _ => 24 }, input.BytesRead);
        Assert.Equal(mode is "class" or "short" or "interrupted" or "cancelled" ? 0 : mode == "match" ? 2 : 1, encryption.Calls);
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT h."CustodyState"='Reserved' AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p
             WHERE p."SourceArtifactId"=h."SourceArtifactId") AS "Value"
            FROM tagekyc.raw_export_source_head h WHERE h."SourceArtifactId"={h.SourceArtifactId}
            """).SingleAsync());
    }

    private sealed class ObservedInputStream(byte[] bytes, string mode) : MemoryStream(bytes, writable: false)
    {
        internal int ReadCalls { get; private set; }
        internal int BytesRead { get; private set; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            if (Position == Length && mode == "interrupted")
                throw new IOException("RAW_EXPORT_R2_PLAINTEXT_LENGTH_MISMATCH"); // Not a clean EOF despite the message.
            if (Position == Length && mode == "cancelled") throw new OperationCanceledException();
            var count = Read(buffer.Span);
            BytesRead += count;
            return ValueTask.FromResult(count);
        }
    }

    private sealed class CountingInputRecorder(IRawExportR2TerminalIntentRecorder inner, bool gated = false) : IRawExportR2TerminalIntentRecorder
    {
        internal int Calls { get; private set; }
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<RawExportR2TerminalIntentOutcome> RecordAsync(Guid sourceArtifactId, Guid attemptId,
            long expectedRevision, long expectedFence, string disposition, string code, CancellationToken token)
        {
            Calls++;
            Entered.TrySetResult();
            if (gated) await Release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return await inner.RecordAsync(sourceArtifactId, attemptId, expectedRevision, expectedFence, disposition, code, token);
        }
    }

    private sealed class CountingInputEncryption(IAttemptAeadEncryptionOperation inner, bool fail) : IAttemptAeadEncryptionOperation
    {
        internal int Calls { get; private set; }
        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(AttemptAeadChunkRequest request, CancellationToken token)
        {
            Calls++;
            if (fail) throw new IOException("synthetic-aead-unavailable");
            return inner.EncryptBoundedChunkAsync(request, token);
        }
    }

    private sealed class InputCommitmentProvider(IContentCommitmentService inner, string mode) : IContentCommitmentService
    {
        internal int Calls { get; private set; }
        public ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector, ReadOnlyMemory<byte> payload,
            CancellationToken token)
        {
            Calls++;
            if (mode == "commitment-throws") throw new IOException("synthetic-commitment-unavailable");
            return mode == "commitment-unavailable"
                ? ValueTask.FromResult(ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure))
                : inner.ComputeAsync(selector, payload, token);
        }
    }

    [Fact]
    public async Task Nps01_LostR1WithoutProviderPersistsOnlyOperationalPair()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_no_start");
        var (scope, handoff) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        var before = await Rows(observer);
        var original = await Attempt(observer, handoff);
        Assert.Equal("LeaseLive", (await Nps(isolated, handoff)).Outcome);
        Assert.Equal(before, await Rows(observer));
        Assert.Equal("StateConflict", (await Nps(isolated, handoff with { ExpectedFence = 2 })).Outcome);
        Assert.Equal("StateConflict", (await Nps(isolated, handoff, Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"))).Outcome);
        Assert.Equal("StateConflict", (await Nps(isolated, handoff with { SourceArtifactId = Guid.Empty })).Outcome);
        Assert.Equal("NotFound", (await Nps(isolated, handoff with { AttemptId = Guid.NewGuid() })).Outcome);
        Assert.Equal(before, await Rows(observer));
        await PastLease(observer, handoff);
        // Current consent is not required merely to settle already-owned resources.
        await Grant(observer, "SubjectConsentWithdrawer");
        await using (var tx = await observer.Database.BeginTransactionAsync())
        {
            await Actor(observer, Principal);
            Assert.Equal("Withdrawn", await observer.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
                 {Principal},{Client},{scope.Reference},1,'source-v1','synthetic-nps',{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync());
            await tx.CommitAsync();
        }
        var result = await Nps(isolated, handoff);
        Assert.Equal("TerminatedBeforeStart", result.Outcome);
        Assert.Equal("TerminatedBeforeStart", result.R2TerminationDisposition);
        Assert.NotNull(result.R2TerminatedAtUtc);
        Assert.Equal(original, await Attempt(observer, handoff)); // excludes only the two permitted fields
        Assert.Equal(before, await Rows(observer)); // includes complete source/head/reservation/provider rows
        Assert.Equal(0, await ProviderCount(observer));
        var replay = await Nps(isolated, handoff);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(result.R2TerminatedAtUtc, replay.R2TerminatedAtUtc);
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT "R2TerminationDisposition"='TerminatedBeforeStart' AND "R2TerminatedAtUtc"={result.R2TerminatedAtUtc}
             AND "R2TerminalOutcomeCode" IS NULL AND "R2TerminalIntentCode" IS NULL
             AND "R2TerminalIntentDisposition" IS NULL AND "R2TerminalIntentAtUtc" IS NULL AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={handoff.AttemptId}
            """).SingleAsync());
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var provider = new UnknownProvider();
        var denied = await Provision(writer, handoff, provider);
        Assert.Equal(AttemptKeyProvisioningOutcome.HeadNotReserved, denied.Outcome);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(0, await ProviderCount(observer));
        Assert.Equal(before, await Rows(observer));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Nps01_PrepareAndNoStartSerialize(bool prepareFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_race");
        var (scope, handoff) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var blocker = isolated.CreateDbContext();
        await using var block = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        var provider = new UnknownProvider();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await writer.Database.OpenConnectionAsync();
        var writerPid = ((NpgsqlConnection)writer.Database.GetDbConnection()).ProcessID;
        await using var recovery = RoleDb(isolated, "tagekyc_raw_export_reconciler");
        await recovery.Database.OpenConnectionAsync();
        var recoveryPid = ((NpgsqlConnection)recovery.Database.GetDbConnection()).ProcessID;
        try
        {
        if (prepareFirst)
        {
            // Actual prepare inserts key/operation/event inside the transaction that owns
            // the prefix. Recovery must wait, then see that evidence even without Wrap success.
            await Actor(blocker, Principal);
            await blocker.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_custody_encryptor");
            Assert.Equal("PreparingLive", await blocker.Database.SqlQuery<string>($"""
                SELECT outcome AS "Value" FROM tagekyc.raw_export_prepare_attempt_key_reservation(
                 {handoff.AttemptKeyReservationId},{handoff.AttemptId},{handoff.SourceArtifactId})
                """).SingleAsync());
            var pending = NpsOn(recovery, handoff);
            await WaitAtSession(observer, recoveryPid, scope.Session);
            Assert.Equal(0, await ProviderCount(observer)); // preparation not yet committed
            await PastLease(observer, handoff);
            await block.CommitAsync();
            var result = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("ProviderEvidencePresent", result.Outcome);
            Assert.Null(result.R2TerminatedAtUtc);
            Assert.Equal(3, await ProviderCount(observer));
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT "R2TerminationDisposition" IS NULL AND "R2TerminalOutcomeCode" IS NULL AS "Value"
                FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={handoff.AttemptId}
                """).SingleAsync());
        }
        else
        {
            var pendingNps = NpsOn(recovery, handoff);
            await WaitAtSession(observer, recoveryPid, scope.Session);
            var pendingPrepare = Provision(writer, handoff, provider);
            await WaitAtSession(observer, writerPid, scope.Session);
            await PastLease(observer, handoff);
            await block.CommitAsync();
            Assert.Equal("TerminatedBeforeStart", (await pendingNps.WaitAsync(TimeSpan.FromSeconds(10))).Outcome);
            Assert.Equal(AttemptKeyProvisioningOutcome.HeadNotReserved,
                (await pendingPrepare.WaitAsync(TimeSpan.FromSeconds(10))).Outcome);
            Assert.Equal(0, provider.Calls);
            Assert.Equal(0, await ProviderCount(observer));
        }
        }
        finally { if(block.GetDbTransaction().Connection is not null) await block.RollbackAsync(); }
    }

    [Fact]
    public async Task Nps01_ProviderUnknownIsEvidenceNotAbsence()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_unknown");
        var (_, handoff) = await CommitR1(isolated);
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        var provider = new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown, (await Provision(writer, handoff, provider)).Outcome);
        Assert.Equal(1, provider.Calls);
        await using var observer = isolated.CreateDbContext();
        Assert.True(await ProviderCount(observer) > 0);
        var before = await Rows(observer);
        await PastLease(observer, handoff);
        Assert.Equal("ProviderEvidencePresent", (await Nps(isolated, handoff)).Outcome);
        Assert.Equal(before, await Rows(observer));
    }

    [Theory]
    [InlineData("preparation")]
    [InlineData("active-key")]
    [InlineData("provider-unknown")]
    [InlineData("object-custody")]
    public async Task ProviderEvidenceCannotMasqueradeAsNoStart(string evidence)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            "a3_nps_evidence_" + evidence.Replace('-', '_'));
        var (_, handoff) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));

        if (evidence == "preparation")
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            await Actor(writer, Principal);
            Assert.Equal("PreparingLive", await writer.Database.SqlQuery<string>($"""
                SELECT outcome AS "Value" FROM tagekyc.raw_export_prepare_attempt_key_reservation(
                 {handoff.AttemptKeyReservationId},{handoff.AttemptId},{handoff.SourceArtifactId})
                """).SingleAsync());
            await transaction.CommitAsync();
        }
        else if (evidence == "provider-unknown")
        {
            var unknown = new UnknownProvider();
            Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
                (await Provision(writer, handoff, unknown)).Outcome);
            Assert.Equal(1, unknown.Calls);
        }
        else
        {
            Assert.Equal(AttemptKeyProvisioningOutcome.Activated,
                (await Provision(writer, handoff, kek)).Outcome);
            if (evidence == "object-custody")
                Assert.Equal("Created", (await BeginObject(writer, handoff)).OutcomeCode);
            else
                Assert.Equal("active-key", evidence);
        }

        var attemptBefore = await FullAttempt(observer, handoff);
        var rowsBefore = await Rows(observer);
        Assert.True(await ProviderCount(observer) > 0);
        await PastLease(observer, handoff);
        var result = await Nps(isolated, handoff);
        Assert.Equal("ProviderEvidencePresent", result.Outcome);
        Assert.Null(result.R2TerminationDisposition);
        Assert.Null(result.R2TerminatedAtUtc);
        Assert.Equal(attemptBefore, await FullAttempt(observer, handoff));
        Assert.Equal(rowsBefore, await Rows(observer));
    }

    [Fact]
    public async Task Nps01_KeyPreparationUsesPostLockLeaseClock()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_clock");
        var (scope, handoff) = await CommitR1(isolated);
        await using var observer = isolated.CreateDbContext();
        await using var blocker = isolated.CreateDbContext();
        await using var block = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        await using var writer = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await writer.Database.OpenConnectionAsync();
        var provider = new UnknownProvider();
        var pending = Provision(writer, handoff, provider);
        try
        {
        await WaitAtSession(observer, ((NpgsqlConnection)writer.Database.GetDbConnection()).ProcessID, scope.Session);
        await PastLease(observer, handoff);
        await block.CommitAsync();
        Assert.Equal(AttemptKeyProvisioningOutcome.HeadNotReserved, (await pending.WaitAsync(TimeSpan.FromSeconds(10))).Outcome);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(0, await ProviderCount(observer));
        }
        finally { if(block.GetDbTransaction().Connection is not null) await block.RollbackAsync(); }
    }

    private static async Task<(Tip88C1C6BA3RetentionCheckpointTests.Scope, RawIngressBrokerHandoff)> CommitR1(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, string rawClass = "LiveSelfieImage")
    {
        await Prepare(isolated);
        return await CommitPreparedR1(isolated, rawClass: rawClass);
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class AcceptedRuntimeAuthenticator : ICaptureRuntimeRequestAuthenticator
    {
        internal static readonly Guid CredentialId = Guid.Parse("60000000-0000-4000-8000-000000000001");

        public Task<TagEkyc.Application.VerificationSessions.SessionOperationResult<AuthenticatedCaptureRuntimeContext>>
            AuthenticateAsync(CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(TagEkyc.Application.VerificationSessions.SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(
                new(Guid.Parse("40000000-0000-4000-8000-000000000001"),
                    Guid.Parse("50000000-0000-4000-8000-000000000001"), CredentialId, 1, new byte[32],
                    Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, 1, 1, 1,
                    request.SignedAtUtc, request.Nonce, SHA256.HashData(request.ExactSignedPreimage.Span))));
    }

    private sealed class CountingReplayBodyStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        internal int ReadCalls { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            ReadCalls++;
            return base.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(base.Read(buffer.Span));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            ReadCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(base.Read(buffer, offset, count));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Nps01_AdjacentObjectAndTerminatorReplayTakeSessionPrefix(bool terminatedReplay)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_adjacent");
        var (scope,handoff)=await CommitR1(isolated);
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var journalWrite=isolated.CreateDbContext();
        await using var journalRead=isolated.CreateDbContext();
        var kek=new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated,(await Provision(writer,handoff,kek)).Outcome);
        await using var actor=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await actor.Database.OpenConnectionAsync();
        if(terminatedReplay)
        {
            var begun=await BeginObject(writer,handoff);
            Assert.Equal("Created",begun.OutcomeCode);
            var digest=C1HashCanonical.Compute("tip-88c1-object-not-armed-evidence-v1",
                new C1HashCanonical.Scalar(Convert.ToHexString(begun.ObjectBindingDigest).ToLowerInvariant()),
                new C1HashCanonical.Scalar(begun.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar("NotArmed"));
            Assert.Equal("NotArmed",await writer.Database.SqlQuery<string>($"""
                SELECT "OutcomeCode" AS "Value" FROM tagekyc.raw_export_record_provisional_object_not_armed(
                 {begun.ObjectCustodyId},{begun.StateRevision},{digest})
                """).SingleAsync());
            Assert.Equal("Terminated",await Terminate(actor,handoff));
        }
        await using var observer=isolated.CreateDbContext();
        var before=await Rows(observer);
        var beforeAttempt=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE "AttemptId"={handoff.AttemptId}
            """).SingleAsync();
        await using var blocker=isolated.CreateDbContext();
        await using var block=await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        await writer.Database.OpenConnectionAsync();
        Task<string> pending=terminatedReplay?Terminate(actor,handoff):BeginAndCode();
        try
        {
        await WaitAtSession(observer,((NpgsqlConnection)(terminatedReplay?actor:writer).Database.GetDbConnection()).ProcessID,scope.Session);
        Assert.False(pending.IsCompleted);
        Assert.Equal(before,await Rows(observer));
        await block.CommitAsync();
        Assert.Equal(terminatedReplay?"ExistingMatch":"Created",await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(beforeAttempt,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={handoff.AttemptId}
            """).SingleAsync());
        if(terminatedReplay)Assert.Equal(before,await Rows(observer));
        else Assert.Equal("ExistingMatch",(await BeginObject(writer,handoff)).OutcomeCode);
        }
        finally { if(block.GetDbTransaction().Connection is not null) await block.RollbackAsync(); }
        async Task<string> BeginAndCode()=>(await BeginObject(writer,handoff)).OutcomeCode;
    }

    [Fact]
    public async Task Nps01_CatalogAndWriteGuardAreClosed()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_nps_guard");
        var (_,handoff)=await CommitR1(isolated);
        await using var db=isolated.CreateDbContext();
        var roleNames = new[]{"tagekyc_runtime","tagekyc_capture_runtime_application","tagekyc_capture_runtime_authenticator",
            "tagekyc_capture_runtime_operator","tagekyc_raw_export_claim_broker","tagekyc_raw_export_custody_encryptor",
            "tagekyc_raw_export_reconciler","tagekyc_raw_export_lifecycle"};
        Assert.Equal(roleNames.Order(), (await db.Database.SqlQuery<string>($"""
            SELECT rolname AS "Value" FROM pg_roles WHERE rolname=ANY({roleNames})
            """).ToArrayAsync()).Order());
        var allowed=await db.Database.SqlQueryRaw<string>("""
            SELECT r.rolname AS "Value" FROM pg_roles r WHERE r.rolname IN
             ('tagekyc_runtime','tagekyc_capture_runtime_application','tagekyc_capture_runtime_authenticator','tagekyc_capture_runtime_operator',
              'tagekyc_raw_export_claim_broker','tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle')
             AND has_function_privilege(r.oid,'tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)'::regprocedure,'EXECUTE')
            """).ToArrayAsync();
        Assert.Equal(new[]{"tagekyc_raw_export_reconciler"},allowed);
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
             AND pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer' AS "Value"
            FROM pg_proc p WHERE p.oid='tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)'::regprocedure
            """).SingleAsync());
        var before=await Attempt(db,handoff);
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            await Actor(db,Principal);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_source_core_write_context','tip88c1-a3-no-provider-start-v1',true)");
            var rejected=await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tagekyc.raw_export_source_encryption_attempts
                SET "R2TerminationDisposition"='TerminatedBeforeStart',"R2TerminatedAtUtc"=clock_timestamp()
                WHERE "AttemptId"={handoff.AttemptId}
                """));
            Assert.Equal("RAW_EXPORT_SOURCE_CORE_APPEND_ONLY",rejected.MessageText);
            await tx.RollbackAsync();
        }
        Assert.Equal(before,await Attempt(db,handoff));
        await using var legacy=RoleDb(isolated,"tagekyc_runtime");
        Assert.False(await legacy.Database.SqlQuery<bool>($"""
            SELECT tagekyc.raw_export_record_r2_terminal_outcome({handoff.SourceArtifactId},{handoff.AttemptId},
             {handoff.ExpectedEncryptionAttemptRevision},{handoff.ExpectedFence},'TerminatedBeforeStart','RECAPTURE_REQUIRED') AS "Value"
            """).SingleAsync());
        Assert.Equal(before,await Attempt(db,handoff));
    }

    private static Task<ObjectResult> BeginObject(TagEkycDbContext db,RawIngressBrokerHandoff h)=>db.Database.SqlQuery<ObjectResult>($"""
        SELECT "OutcomeCode","ObjectCustodyId","StateRevision","ObjectBindingDigest"
        FROM tagekyc.raw_export_begin_provisional_object_custody({h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence},1)
        """).SingleAsync();
    private static Task<string> Terminate(TagEkycDbContext db,RawIngressBrokerHandoff h)=>db.Database.SqlQuery<string>($"""
        SELECT tagekyc.raw_export_terminate_source_encryption_attempt({h.AttemptId},{h.ExpectedEncryptionAttemptRevision},
         {h.ExpectedFence},'TerminatedBeforeStart') AS "Value"
        """).SingleAsync();
    private sealed class ObjectResult
    {
        public string OutcomeCode {get;set;}="";
        public Guid ObjectCustodyId {get;set;}
        public long StateRevision {get;set;}
        public byte[] ObjectBindingDigest {get;set;}=[];
    }

    private static async Task<(Tip88C1C6BA3RetentionCheckpointTests.Scope, RawIngressBrokerHandoff)> CommitPreparedR1(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,int lifetimeSeconds=300,int? attemptDeadlineSeconds=null,
        string rawClass="LiveSelfieImage")
    {
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated,lifetimeSeconds:lifetimeSeconds,
            rawIngress:true,rawClass:rawClass);
        return (scope,await AdmitPreparedScope(isolated,scope,rawClass,attemptDeadlineSeconds));
    }

    private static async Task<RawIngressBrokerHandoff> AdmitPreparedScope(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Tip88C1C6BA3RetentionCheckpointTests.Scope scope,string rawClass,int? attemptDeadlineSeconds=null)
    {
        await using var db = isolated.CreateDbContext();
        var login = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(db.Database.GetConnectionString()!);
        await using var source = NpgsqlDataSource.Create(login);
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(source, services.GetRequiredService<IContentCommitmentService>(),
            services.GetRequiredService<ISubjectRefTokenService>());
        if(attemptDeadlineSeconds is { } deadline)
        {
            // Only the bounded expiry fixture overrides qualified timing input.
            // Its 40-second original horizon cannot admit the normal 60-second
            // attempt deadline. No stored clock/horizon is rewritten.
            broker=new RawIngressBrokerTransactionFacade(source,
                new RetainedSourceClaimPreflight(services.GetRequiredService<IContentCommitmentService>(),
                    services.GetRequiredService<ISubjectRefTokenService>()),
                new QualifiedBrokerProfiles(new FixtureSourceEncryptionProfileCatalog().GetActive(),
                    new FixtureKekReferenceCatalog().GetActive(),new(TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(120),TimeSpan.FromSeconds(deadline),TimeSpan.FromSeconds(30))),
                new(Guid.Parse("8bf2e6a5425a42689647465139fc46b0"),10,100,
                    new("fixture-content-commitment",1),new("fixture-subject-ref-token",1),1000));
        }
        var now = await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var request = new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"),Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"),1,Guid.Parse("10000000-0000-4000-8000-000000000001"),1,
            now,new byte[32],new byte[32],1,scope.Session,scope.Artifact,1,rawClass,Guid.NewGuid(),"image/jpeg",24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5),now.AddSeconds(-4),now.AddMinutes(5),300);
        return Assert.IsType<RawIngressBrokerResult.Handoff>(await broker.AdmitAsync(request,CancellationToken.None)).Value;
    }

    private static TagEkycDbContext RoleDb(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, string role, Guid? actor = null)
    {
        using var owner = isolated.CreateDbContext();
        var connection = new NpgsqlConnectionStringBuilder(owner.Database.GetConnectionString())
        {
            Options=$"-c role={role} -c tagekyc.actor_principal_id={(actor ?? Principal):D}", Pooling=false, CommandTimeout=60,
        };
        return new(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connection.ConnectionString).Options);
    }

    private static async Task<NpsResult> Nps(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, RawIngressBrokerHandoff h, Guid? actor = null)
    {
        await using var db = RoleDb(isolated,"tagekyc_raw_export_reconciler",actor);
        return await NpsOn(db,h);
    }

    private static Task<NpsResult> NpsOn(TagEkycDbContext db, RawIngressBrokerHandoff h) => db.Database.SqlQuery<NpsResult>($"""
        SELECT * FROM tagekyc.raw_export_terminate_retained_r2_before_provider_start(
         {h.SourceArtifactId},{h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence})
        """).SingleAsync();

    private static Task Actor(TagEkycDbContext db, Guid actor) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{actor.ToString("D")},true)");

    private static Task<AttemptKeyProvisioningResult> Provision(TagEkycDbContext db, RawIngressBrokerHandoff h, IKekOperationProvider provider) =>
        new PostgresAttemptKeyReservationProvider(db,new PostgresKeyProviderOperationMap(db),provider)
            .ProvisionAsync(new(h.AttemptKeyReservationId,h.AttemptId,h.SourceArtifactId),CancellationToken.None);

    private static Task<string> KeyRecoveryState(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<string>($"""
            SELECT r."PreparationDisposition"||'|'||o."ProviderOperationState" AS "Value"
            FROM tagekyc.raw_export_attempt_key_reservations r
            JOIN tagekyc.raw_export_key_provider_operations o
              ON o."AttemptKeyReservationId"=r."AttemptKeyReservationId"
             AND o."PreparationFence"=r."CurrentPreparationFence"
            WHERE r."AttemptKeyReservationId"={h.AttemptKeyReservationId}
            """).SingleAsync();

    private static async Task PastLease(TagEkycDbContext observer, RawIngressBrokerHandoff h)
    {
        var remaining = await observer.Database.SqlQuery<double>($"""
            SELECT greatest(0,extract(epoch FROM ("OwnershipLeaseExpiresAtUtc"-clock_timestamp())))::double precision AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        Assert.InRange(remaining,0,30.1);
        await Task.Delay(TimeSpan.FromSeconds(remaining+0.1));
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT clock_timestamp()>"OwnershipLeaseExpiresAtUtc" AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
    }

    private static async Task WaitAtSession(TagEkycDbContext observer, int pid, Guid session)
    {
        for(var i=0;i<100;i++)
        {
            var waiting=await observer.Database.SqlQuery<bool>($"""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l
                 WHERE l.pid={pid} AND NOT l.granted AND l.locktype IN ('transactionid','tuple'))
                 AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks l WHERE l.pid={pid}
                  AND l.relation='tagekyc.verification_sessions'::regclass AND l.granted)
                 AND EXISTS(SELECT 1 FROM tagekyc.verification_sessions WHERE "Id"={session}) AS "Value"
                """).SingleAsync();
            if(waiting)return;
            await Task.Delay(50);
        }
        Assert.Fail("Expected actual PostgreSQL wait at retained session prefix.");
    }

    private static Task<string> Attempt(TagEkycDbContext db, RawIngressBrokerHandoff h) => db.Database.SqlQuery<string>($"""
        SELECT (to_jsonb(a)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc'])::text AS "Value"
        FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
        """).SingleAsync();

    private static Task<int> ProviderCount(TagEkycDbContext db) => db.Database.SqlQueryRaw<int>("""
        SELECT ((SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations)
          +(SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events)
          +(SELECT count(*) FROM tagekyc.raw_export_key_provider_operations)
          +(SELECT count(*) FROM tagekyc.raw_export_provisional_objects))::integer AS "Value"
        """).SingleAsync();

    private static async Task<string[]> Rows(TagEkycDbContext db)
    {
        var rows=new List<string>();
        foreach(var table in new[]{"raw_export_source_reservations","raw_export_source_head","raw_export_source_publications",
            "raw_export_attempt_key_reservations","raw_export_attempt_key_preparation_events","raw_export_key_provider_operations",
            "raw_export_provisional_objects","raw_export_provisional_object_events"})
            rows.AddRange(await db.Database.SqlQueryRaw<string>($"SELECT '{table}:'||to_jsonb(r)::text AS \"Value\" FROM tagekyc.{table} r ORDER BY to_jsonb(r)::text").ToArrayAsync());
        return rows.ToArray();
    }


    [Fact]
    public async Task Re01_LostR1RetryAppendsOneFreshAttemptAndRenewsOnlyDerivedLease()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry");
        var setup=await ReentrySetup(isolated);
        await using var observer=isolated.CreateDbContext();
        var h=setup.Handoff;
        var reservation=await ReservationWithoutLease(observer,h);
        var beforeBusyRows=await Rows(observer);
        var originalAttemptBeforeBusy=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT "OwnershipLeaseExpiresAtUtc">clock_timestamp()
             AND "R2TerminationDisposition" IS NULL AND "R2TerminatedAtUtc" IS NULL AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        AssertReservationBusy(await RetryAdmission(setup));
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT "OwnershipLeaseExpiresAtUtc">clock_timestamp() AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(beforeBusyRows,await Rows(observer));
        Assert.Equal(originalAttemptBeforeBusy,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(1,await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(0,await ProviderCount(observer));
        await PastLease(observer,h);
        // B-C owns NPS01 then RE01; no test-local operational UPDATE.
        var retry=Assert.IsType<RawIngressBrokerResult.Handoff>(await RetryAdmission(setup)).Value;
        Assert.Equal(h.SourceArtifactId,retry.SourceArtifactId);
        Assert.Equal(h.BindingId,retry.BindingId);
        Assert.NotEqual(h.AttemptId,retry.AttemptId);
        Assert.NotEqual(h.AttemptKeyReservationId,retry.AttemptKeyReservationId);
        Assert.Equal(h.ExpectedEncryptionAttemptRevision+1,retry.ExpectedEncryptionAttemptRevision);
        Assert.Equal(h.ExpectedFence+1,retry.ExpectedFence);
        Assert.Equal(reservation,await ReservationWithoutLease(observer,h));
        await AssertReentryProjection(observer,h,retry);
        Assert.Equal(0,await ProviderCount(observer));
        var old=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        var frozen=await Rows(observer);
        await AssertEvaluationPending(observer,setup,await RetryAdmission(setup));
        Assert.Equal(frozen,await Rows(observer));
        Assert.Equal(0,await ProviderCount(observer));
        await PastEvaluation(observer,setup);
        AssertReservationBusy(await RetryAdmission(setup));
        Assert.Equal(2,await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(frozen,await Rows(observer));
        Assert.Equal(old,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        var provider=new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.HeadNotReserved,(await Provision(writer,h,provider)).Outcome);
        Assert.Equal(0,provider.Calls);
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,(await Provision(writer,retry,provider)).Outcome);
        Assert.Equal(1,provider.Calls);
        Assert.Equal(3,await ProviderCount(observer)); // only the new attempt started provider preparation
    }

    [Fact]
    public async Task Re01_ConcurrentSameOwnerRetryHasOneHandoff()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry_race");
        var setup=await ReentrySetup(isolated);
        await using var observer=isolated.CreateDbContext();
        await PastLease(observer,setup.Handoff);
        var results=await Task.WhenAll(RetryAdmission(setup),RetryAdmission(setup));
        var handoff=Assert.Single(results.OfType<RawIngressBrokerResult.Handoff>()).Value;
        // The same-alias loser reaches B-B's earlier evaluation gate, not B-C.
        await AssertEvaluationPending(observer,setup,Assert.Single(results.OfType<RawIngressBrokerResult.Final>()));
        await PastEvaluation(observer,setup);
        AssertReservationBusy(await RetryAdmission(setup));
        await AssertReentryProjection(observer,setup.Handoff,handoff);
        Assert.Equal(0,await ProviderCount(observer));
    }

    [Fact]
    public async Task Re01_UnknownProviderCannotReenterOrEraseEvidence()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry_unknown");
        var setup=await ReentrySetup(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        var provider=new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
            (await Provision(writer,setup.Handoff,provider)).Outcome);
        Assert.Equal(1,provider.Calls);
        await PastLease(observer,setup.Handoff);
        var before=await Rows(observer);
        AssertReservationBusy(await RetryAdmission(setup));
        Assert.Equal(before,await Rows(observer));
        Assert.Equal(1,await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(3,await ProviderCount(observer));
    }

    [Fact]
    public async Task Re01_InternalHelperRejectsWrongActorBindingTupleAndStageRoles()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry_guards");
        var setup=await ReentrySetup(isolated);
        await using var observer=isolated.CreateDbContext();
        await PastLease(observer,setup.Handoff);
        Assert.Equal("TerminatedBeforeStart",(await Nps(isolated,setup.Handoff)).Outcome);
        var before=await Rows(observer);
        foreach(var role in new[]{"tagekyc_runtime","tagekyc_capture_runtime_application","tagekyc_capture_runtime_authenticator",
            "tagekyc_capture_runtime_operator","tagekyc_raw_export_claim_broker","tagekyc_raw_export_custody_encryptor",
            "tagekyc_raw_export_reconciler","tagekyc_raw_export_lifecycle"})
        {
            await using var denied=RoleDb(isolated,role);
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>Reenter(denied,setup.Handoff,setup.Scope.RuntimeBinding));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,failure.SqlState);
        }
        await using var owner=RoleDb(isolated,"tagekyc_raw_export_deployer");
        Assert.Equal("StateConflict",(await Reenter(owner,setup.Handoff,Guid.NewGuid())).OutcomeCode);
        Assert.Equal("StateConflict",(await Reenter(owner,setup.Handoff with {ExpectedFence=2},setup.Scope.RuntimeBinding)).OutcomeCode);
        await using var wrongActor=RoleDb(isolated,"tagekyc_raw_export_deployer",Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"));
        Assert.Equal("StateConflict",(await Reenter(wrongActor,setup.Handoff,setup.Scope.RuntimeBinding)).OutcomeCode);
        Assert.Equal(before,await Rows(observer));
        var good=await Reenter(owner,setup.Handoff,setup.Scope.RuntimeBinding);
        Assert.Equal("SameOwnerReentry",good.OutcomeCode); // positive owner-only boundary control
        Assert.Equal(0,await ProviderCount(observer));
    }


    [Fact]
    public async Task Re01_TerminatedNoObjectStillRequiresSettledKeyAndPreservesOldRows()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry_settled");
        await using var minio=await DurableObjectMinioFixture.StartAsync();
        var setup=await ReentrySetup(isolated);
        var h=setup.Handoff;
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await using var journalWrite=isolated.CreateDbContext();
        await using var journalRead=isolated.CreateDbContext();
        var kek=new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated,(await Provision(writer,h,kek)).Outcome);
        var begun=await BeginObject(writer,h);
        Assert.Equal("Created",begun.OutcomeCode);
        var digest=C1HashCanonical.Compute("tip-88c1-object-not-armed-evidence-v1",
            new C1HashCanonical.Scalar(Convert.ToHexString(begun.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(begun.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("NotArmed"));
        Assert.Equal("NotArmed",await writer.Database.SqlQuery<string>($"""
            SELECT "OutcomeCode" AS "Value" FROM tagekyc.raw_export_record_provisional_object_not_armed(
             {begun.ObjectCustodyId},{begun.StateRevision},{digest})
            """).SingleAsync());
        Assert.Equal("Terminated",await Terminate(reconciler,h));
        // Operational termination can coexist with Active key: not safe reentry.
        await PastLease(observer,h);
        var activeRows=await Rows(observer);
        AssertReservationBusy(await RetryAdmission(setup));
        Assert.Equal(activeRows,await Rows(observer));
        Assert.Equal("Revoked",await reconciler.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_revoke_attempt_key_reservation({h.AttemptKeyReservationId},'synthetic-reentry-settlement') AS "Value"
            """).SingleAsync());
        var oldRows=(await Rows(observer)).Where(row=>!row.StartsWith("raw_export_source_head:",StringComparison.Ordinal)
            && !row.StartsWith("raw_export_source_reservations:",StringComparison.Ordinal)).ToArray();
        var oldAttempt=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        var reservation=await ReservationWithoutLease(observer,h);
        await AssertEvaluationPending(observer,setup,await RetryAdmission(setup));
        await PastEvaluation(observer,setup);
        var next=Assert.IsType<RawIngressBrokerResult.Handoff>(await RetryAdmission(setup)).Value;
        await AssertReentryProjection(observer,h,next);
        Assert.Equal(reservation,await ReservationWithoutLease(observer,h));
        Assert.Equal(oldAttempt,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(oldRows,(await Rows(observer)).Where(row=>!row.StartsWith("raw_export_source_head:",StringComparison.Ordinal)
            && !row.StartsWith("raw_export_source_reservations:",StringComparison.Ordinal)).ToArray());
        var oldKey=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(k)::text AS "Value" FROM tagekyc.raw_export_attempt_key_reservations k
            WHERE "AttemptKeyReservationId"={h.AttemptKeyReservationId}
            """).SingleAsync();
        var oldObject=await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(o)::text AS "Value" FROM tagekyc.raw_export_provisional_objects o
            WHERE "ObjectCustodyId"={begun.ObjectCustodyId}
            """).SingleAsync();
        await using var logins=await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes=Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections,minio);
        await using var preflight=Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var pipeline=new CaptureRuntimeRawIngressBodyPipeline(scopes,kek,kek,
            preflight.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()),
            RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration()),
            CancellationToken.None);
        await using var body=new MemoryStream("synthetic-retainedsource"u8.ToArray(),writable:false);
        var available=await pipeline.ProcessAsync(setup.Request,next,body,CancellationToken.None);
        Assert.Equal(CaptureRuntimeRawIngressOutcome.Available,available.Outcome);
        Assert.Equal(h.SourceArtifactId,available.SourceArtifactId);
        Assert.Equal(1,await observer.RawExportSourcePublications.AsNoTracking()
            .CountAsync(row=>row.SourceArtifactId==h.SourceArtifactId && row.PublicationState=="Available"));
        Assert.Equal(2,await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(oldAttempt,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(oldKey,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(k)::text AS "Value" FROM tagekyc.raw_export_attempt_key_reservations k
            WHERE "AttemptKeyReservationId"={h.AttemptKeyReservationId}
            """).SingleAsync());
        Assert.Equal(oldObject,await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(o)::text AS "Value" FROM tagekyc.raw_export_provisional_objects o
            WHERE "ObjectCustodyId"={begun.ObjectCustodyId}
            """).SingleAsync());
    }

    // SQL terminal-intent/settlement proofs only. These do not stand in for
    // framed-input observation or child-process crash/restart qualification.
    [Theory]
    [InlineData("RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED")]
    [InlineData("CONTENT_COMMITMENT_MISMATCH")]
    [InlineData("RECAPTURE_REQUIRED")]
    public async Task Ti01_ExactTupleAndCauseReplayPreservesOnlyIntent(string code)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_tuple");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var wrongActor=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor",
            Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"));
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        foreach(var changed in new[]{h with {SourceArtifactId=Guid.NewGuid()},
            h with {ExpectedEncryptionAttemptRevision=h.ExpectedEncryptionAttemptRevision+1},
            h with {ExpectedFence=h.ExpectedFence+1}})
            AssertIntentDenied("StateConflict",await RecordIntent(writer,changed,"TerminatedBeforeStart",code));
        AssertIntentDenied("NotFound",await RecordIntent(writer,h with {AttemptId=Guid.NewGuid()},"TerminatedBeforeStart",code));
        AssertIntentDenied("StateConflict",await RecordIntent(wrongActor,h,"TerminatedBeforeStart",code));
        Assert.Equal(before,await FullAttempt(observer,h));
        var nonIntent=await AttemptWithoutIntent(observer,h);
        var recorded=await RecordIntent(writer,h,"TerminatedBeforeStart",code);
        Assert.Equal("Recorded",recorded.Outcome);
        Assert.Equal(code,recorded.TerminalIntentCode);
        Assert.Equal("TerminatedBeforeStart",recorded.TerminalIntentDisposition);
        Assert.NotNull(recorded.TerminalIntentAtUtc);
        Assert.Equal(nonIntent,await AttemptWithoutIntent(observer,h));
        Assert.Equal(resources,await Rows(observer));
        var committed=await FullAttempt(observer,h);
        var replay=await RecordIntent(writer,h,"TerminatedBeforeStart",code);
        Assert.Equal("ExistingMatch",replay.Outcome);
        Assert.Equal(recorded.TerminalIntentCode,replay.TerminalIntentCode);
        Assert.Equal(recorded.TerminalIntentDisposition,replay.TerminalIntentDisposition);
        Assert.Equal(recorded.TerminalIntentAtUtc,replay.TerminalIntentAtUtc);
        var different=code=="CONTENT_COMMITMENT_MISMATCH"?"RECAPTURE_REQUIRED":"CONTENT_COMMITMENT_MISMATCH";
        AssertIntentDenied("StateConflict",await RecordIntent(writer,h,"TerminatedBeforeStart",different));
        AssertIntentDenied("StateConflict",await RecordIntent(writer,h,"Terminated",code));
        Assert.Equal(committed,await FullAttempt(observer,h));
        Assert.Equal(resources,await Rows(observer));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StageCannotOvertakeTerminalIntent(bool intentFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_r3_race");
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var realObjectWriter = new S3CompatibleProvisionalObjectWriter(
            minio.Options(ProvisionalObjectCapability.Writer));
        var (scope, h, written, keyProvider) = await WriteIncompleteObject(isolated, realObjectWriter);
        await using var observer = isolated.CreateDbContext();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var services = new ServiceCollection()
            .AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var pipeline = new CaptureRuntimeSourcePipeline(scopes, keyProvider, null, keyProvider,
            services.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var verified = await pipeline.AdvanceAsync(h.SourceArtifactId, 1000, default);
        Assert.Equal(RetainedContinuationStep.VerifyIncompleteObject, verified.Step);
        Assert.Equal("VerifiedCompleted", verified.Snapshot!.ObjectState);
        Assert.Equal(written.ObjectCustodyId, verified.Snapshot.ObjectCustodyId);
        var stageCommand = new RawExportR3StageCommand(Principal, h.AttemptId,
            written.ObjectCustodyId!.Value, verified.Snapshot.ReservationRevision,
            h.ExpectedEncryptionAttemptRevision, h.ExpectedFence,
            verified.Snapshot.ObjectStateRevision!.Value);

        await using var intentDb = RoleDb(isolated, "tagekyc_raw_export_custody_encryptor");
        await using var stageDb = RoleDb(isolated, "tagekyc_raw_export_reconciler");
        await intentDb.Database.OpenConnectionAsync();
        await stageDb.Database.OpenConnectionAsync();
        var intentPid = ((NpgsqlConnection)intentDb.Database.GetDbConnection()).ProcessID;
        var stagePid = ((NpgsqlConnection)stageDb.Database.GetDbConnection()).ProcessID;
        await using var blocker = isolated.CreateDbContext();
        await using var block = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        var prior = await AttemptWithoutIntent(observer, h);
        Task<TerminalIntentResult>? intentTask = null;
        Task<RawExportR3StageResult>? stageTask = null;
        try
        {
            if (intentFirst)
            {
                intentTask = RecordIntent(intentDb, h, "Terminated", "RECAPTURE_REQUIRED");
                await WaitAtSession(observer, intentPid, scope.Session);
                stageTask = new RawExportR3StagingService(stageDb).StageAsync(stageCommand);
                await WaitAtSession(observer, stagePid, scope.Session);
            }
            else
            {
                stageTask = new RawExportR3StagingService(stageDb).StageAsync(stageCommand);
                await WaitAtSession(observer, stagePid, scope.Session);
                intentTask = RecordIntent(intentDb, h, "Terminated", "RECAPTURE_REQUIRED");
                await WaitAtSession(observer, intentPid, scope.Session);
            }
            Assert.False(intentTask.IsCompleted);
            Assert.False(stageTask.IsCompleted);
            await block.CommitAsync();
            var intent = await intentTask.WaitAsync(TimeSpan.FromSeconds(20));
            var staged = await stageTask.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(intentFirst ? "Recorded" : "StateConflict", intent.Outcome);
            Assert.Equal(intentFirst ? RawExportR3StageDisposition.StateConflict
                : RawExportR3StageDisposition.Staged, staged.Disposition);
            if (intentFirst)
            {
                Assert.Equal(prior, await AttemptWithoutIntent(observer, h));
                Assert.True(await observer.Database.SqlQuery<bool>($"""
                    SELECT "R2TerminalIntentCode"='RECAPTURE_REQUIRED'
                     AND "R2TerminalIntentDisposition"='Terminated'
                     AND "R2TerminalIntentAtUtc" IS NOT NULL
                     AND "StagedAtUtc" IS NULL AND "StagedCiphertextFingerprint" IS NULL AS "Value"
                    FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
                    """).SingleAsync());
                Assert.Equal("Reserved", await observer.Database.SqlQuery<string>($"""
                    SELECT "CustodyState" AS "Value" FROM tagekyc.raw_export_source_head
                    WHERE "SourceArtifactId"={h.SourceArtifactId}
                    """).SingleAsync());
            }
            else
            {
                Assert.Null(intent.TerminalIntentCode);
                Assert.Null(intent.TerminalIntentAtUtc);
                Assert.Equal(h.SourceArtifactId, staged.SourceArtifactId);
                Assert.True(await observer.Database.SqlQuery<bool>($"""
                    SELECT "R2TerminalIntentCode" IS NULL AND "R2TerminalIntentDisposition" IS NULL
                     AND "R2TerminalIntentAtUtc" IS NULL AND "StagedAtUtc" IS NOT NULL
                     AND length("StagedCiphertextFingerprint")=32 AS "Value"
                    FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
                    """).SingleAsync());
                Assert.Equal("Staged", await observer.Database.SqlQuery<string>($"""
                    SELECT "CustodyState" AS "Value" FROM tagekyc.raw_export_source_head
                    WHERE "SourceArtifactId"={h.SourceArtifactId}
                    """).SingleAsync());
            }
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking()
                .CountAsync(row => row.SourceArtifactId == h.SourceArtifactId));
        }
        finally
        {
            if (block.GetDbTransaction().Connection is not null) await block.RollbackAsync();
            if (intentTask is not null) try { await intentTask.WaitAsync(TimeSpan.FromSeconds(20)); } catch { }
            if (stageTask is not null) try { await stageTask.WaitAsync(TimeSpan.FromSeconds(20)); } catch { }
        }
    }

    [Fact]
    public async Task Ti02_ActiveKeyWaitsThenRevocationFinalizesExactReplay()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_settlement");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        var intent=await RecordIntent(writer,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH");
        Assert.Equal("Recorded",intent.Outcome);
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        var pending=await FinalizeIntent(reconciler,h);
        Assert.Equal("CleanupPending",pending.Outcome);
        Assert.Null(pending.TerminalOutcomeCode);
        Assert.Equal(before,await FullAttempt(observer,h));
        Assert.Equal(resources,await Rows(observer));
        await RevokeTerminalKey(reconciler,h);
        resources=await Rows(observer);
        var result=await FinalizeIntent(reconciler,h);
        Assert.Equal("Finalized",result.Outcome);
        Assert.Equal(intent.TerminalIntentCode,result.TerminalOutcomeCode);
        await AssertFinalIntent(observer,h,intent);
        Assert.Equal(resources,await Rows(observer));
        var committed=await FullAttempt(observer,h);
        var replay=await FinalizeIntent(reconciler,h);
        Assert.Equal("ExistingMatch",replay.Outcome);
        Assert.Equal(result.TerminalOutcomeCode,replay.TerminalOutcomeCode);
        var intentReplay=await RecordIntent(writer,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH");
        Assert.Equal("ExistingMatch",intentReplay.Outcome);
        Assert.Equal(intent.TerminalIntentAtUtc,intentReplay.TerminalIntentAtUtc);
        Assert.Equal(committed,await FullAttempt(observer,h));
        Assert.Equal(resources,await Rows(observer));
        await using var deployer=RoleDb(isolated,"tagekyc_raw_export_deployer");
        var terminalRows=await Rows(observer);
        Assert.Equal("ReservationBusy",(await Reenter(deployer,h,h.BindingId)).OutcomeCode);
        Assert.Equal(terminalRows,await Rows(observer));
    }

    [Fact]
    public async Task Ti02_FailureAfterOperationalWriteRollsBackBothTerminalWrites()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_atomic");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        var intent=await RecordIntent(writer,h,"TerminatedBeforeStart","RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED");
        Assert.Equal("Recorded",intent.Outcome);
        await RevokeTerminalKey(reconciler,h);
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_fixture_fail_terminal_final() RETURNS trigger
            LANGUAGE plpgsql AS $fixture$
            BEGIN
              IF NEW."R2TerminalOutcomeCode" IS NOT NULL AND OLD."R2TerminalOutcomeCode" IS NULL THEN
                IF OLD."R2TerminationDisposition" IS NULL OR OLD."R2TerminatedAtUtc" IS NULL THEN
                  RAISE EXCEPTION 'A3_FIXTURE_FINAL_BEFORE_OPERATIONAL';
                END IF;
                RAISE EXCEPTION 'A3_FIXTURE_FAIL_AFTER_OPERATIONAL';
              END IF;
              RETURN NEW;
            END $fixture$;
            CREATE TRIGGER zz_a3_fixture_fail_terminal_final BEFORE UPDATE
            ON tagekyc.raw_export_source_encryption_attempts
            FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_fixture_fail_terminal_final();
            """);
        try
        {
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>FinalizeIntent(reconciler,h));
            Assert.Equal("A3_FIXTURE_FAIL_AFTER_OPERATIONAL",failure.MessageText);
            Assert.Equal(before,await FullAttempt(observer,h));
            Assert.Equal(resources,await Rows(observer));
        }
        finally
        {
            await observer.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER zz_a3_fixture_fail_terminal_final ON tagekyc.raw_export_source_encryption_attempts;
                DROP FUNCTION tagekyc.a3_fixture_fail_terminal_final();
                """);
        }
        Assert.Equal("Finalized",(await FinalizeIntent(reconciler,h)).Outcome);
        await AssertFinalIntent(observer,h,intent);
        Assert.Equal(resources,await Rows(observer));
    }

    [Fact]
    public async Task Ti02_SessionWaitUsesPostLockTerminationClock()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_clock");
        var (scope,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await RevokeTerminalKey(reconciler,h);
        await reconciler.Database.OpenConnectionAsync();
        await using var blocker=isolated.CreateDbContext();
        await using var block=await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        var pending=FinalizeIntent(reconciler,h);
        try
        {
            await WaitAtSession(observer,((NpgsqlConnection)reconciler.Database.GetDbConnection()).ProcessID,scope.Session);
            Assert.False(pending.IsCompleted);
            await Actor(blocker,Principal);
            await blocker.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_custody_encryptor");
            var intent=await RecordIntent(blocker,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH");
            Assert.Equal("Recorded",intent.Outcome);
            await block.CommitAsync();
            var result=await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("Finalized",result.Outcome);
            Assert.Equal(intent.TerminalIntentCode,result.TerminalOutcomeCode);
            await AssertFinalIntent(observer,h,intent);
        }
        finally
        {
            if(block.GetDbTransaction().Connection is not null)await block.RollbackAsync();
            try { await pending.WaitAsync(TimeSpan.FromSeconds(15)); } catch(PostgresException) { }
        }
    }

    [Fact]
    public async Task Ti01_NoKeyRequiresNpsAndOriginalExpiryBeforeO20Finalization()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_nps_expiry");
        await Prepare(isolated);
        var (_,h)=await CommitPreparedR1(isolated,lifetimeSeconds:40,attemptDeadlineSeconds:5);
        await using var observer=isolated.CreateDbContext();
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        var original=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        AssertIntentDenied("StateConflict",await RecordIntent(reconciler,h,"TerminatedBeforeStart","RECAPTURE_REQUIRED"));
        Assert.Equal(original,await FullAttempt(observer,h));
        Assert.Equal(0,await ProviderCount(observer));
        await PastLease(observer,h);
        var nps=await NpsOn(reconciler,h);
        Assert.Equal("TerminatedBeforeStart",nps.Outcome);
        Assert.NotNull(nps.R2TerminatedAtUtc);
        var witnessed=await FullAttempt(observer,h);
        var operationalOnly=await observer.Database.SqlQuery<string>($"""
            SELECT (to_jsonb(a)-ARRAY['R2TerminalIntentCode','R2TerminalIntentDisposition',
              'R2TerminalIntentAtUtc','R2TerminalOutcomeCode'])::text AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();
        var nonAttemptRows=await Rows(observer);
        var operationalReadback=Assert.Single(await Continuation(reconciler,h.SourceArtifactId));
        Assert.Null(operationalReadback[20]);Assert.Null(operationalReadback[21]);Assert.Null(operationalReadback[22]);
        Assert.Equal("TerminatedBeforeStart",operationalReadback[23]);
        Assert.Equal(nps.R2TerminatedAtUtc,Assert.IsType<DateTimeOffset>(operationalReadback[24]));
        Assert.Null(operationalReadback[25]);
        var horizon=await OriginalHorizon(observer,h);
        var now=await observer.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        Assert.True(now<horizon,"The pre-expiry control must run before the original horizon.");
        AssertIntentDenied("StateConflict",await RecordIntent(reconciler,h,"TerminatedBeforeStart","RECAPTURE_REQUIRED"));
        Assert.Equal(witnessed,await FullAttempt(observer,h));
        var remaining=await observer.Database.SqlQuery<double>($"SELECT greatest(0,extract(epoch FROM ({horizon}-clock_timestamp())))::double precision AS \"Value\"").SingleAsync();
        Assert.InRange(remaining,0,40.1);
        await Task.Delay(TimeSpan.FromSeconds(remaining+0.1));
        Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp()>={horizon} AS \"Value\"").SingleAsync());
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(reconciler,null,100));
        Assert.Equal(operationalReadback,Assert.Single(await Continuation(reconciler,h.SourceArtifactId)));
        var intent=await RecordIntent(reconciler,h,"TerminatedBeforeStart","RECAPTURE_REQUIRED");
        Assert.Equal("Recorded",intent.Outcome);
        Assert.Equal("RECAPTURE_REQUIRED",intent.TerminalIntentCode);
        Assert.Equal("TerminatedBeforeStart",intent.TerminalIntentDisposition);
        Assert.True(intent.TerminalIntentAtUtc>=horizon);
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(reconciler,null,100));
        var final=await FinalizeIntent(reconciler,h);
        Assert.Equal("Finalized",final.Outcome);
        Assert.Equal("RECAPTURE_REQUIRED",final.TerminalOutcomeCode);
        var finalizedReadback=Assert.Single(await Continuation(reconciler,h.SourceArtifactId));
        Assert.Equal("RECAPTURE_REQUIRED",finalizedReadback[25]);
        Assert.True(Assert.IsType<DateTimeOffset>(finalizedReadback[24])<Assert.IsType<DateTimeOffset>(finalizedReadback[22]));
        Assert.Empty(await Scan(reconciler,null,100));
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT "R2TerminationDisposition"='TerminatedBeforeStart' AND "R2TerminatedAtUtc"={nps.R2TerminatedAtUtc}
             AND "R2TerminalIntentCode"='RECAPTURE_REQUIRED' AND "R2TerminalIntentDisposition"='TerminatedBeforeStart'
             AND "R2TerminalIntentAtUtc"={intent.TerminalIntentAtUtc} AND "R2TerminalOutcomeCode"='RECAPTURE_REQUIRED'
             AND "R2TerminatedAtUtc"<"R2TerminalIntentAtUtc" AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(operationalOnly,await observer.Database.SqlQuery<string>($"""
            SELECT (to_jsonb(a)-ARRAY['R2TerminalIntentCode','R2TerminalIntentDisposition',
              'R2TerminalIntentAtUtc','R2TerminalOutcomeCode'])::text AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(nonAttemptRows,await Rows(observer));
        Assert.Equal(1,await observer.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(0,await ProviderCount(observer));
        var committed=await FullAttempt(observer,h);
        var replay=await FinalizeIntent(reconciler,h);
        Assert.Equal("ExistingMatch",replay.Outcome);
        Assert.Equal("RECAPTURE_REQUIRED",replay.TerminalOutcomeCode);
        var intentReplay=await RecordIntent(reconciler,h,"TerminatedBeforeStart","RECAPTURE_REQUIRED");
        Assert.Equal("ExistingMatch",intentReplay.Outcome);
        Assert.Equal(intent.TerminalIntentAtUtc,intentReplay.TerminalIntentAtUtc);
        Assert.Equal(committed,await FullAttempt(observer,h));
        await using var deployer=RoleDb(isolated,"tagekyc_raw_export_deployer");
        var terminalRows=await Rows(observer);
        Assert.Equal("ReservationBusy",(await Reenter(deployer,h,h.BindingId)).OutcomeCode);
        Assert.Equal(terminalRows,await Rows(observer));
        Assert.Equal(resources,await Rows(observer));
        Assert.Equal(0,await ProviderCount(observer));
    }

    [Fact]
    public async Task Ti01_Ti02_ExactRoleCatalogArgumentsAndDirectWritesAreClosed()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_acl");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        var roles=new[]{"tagekyc_runtime","tagekyc_capture_runtime_application","tagekyc_capture_runtime_authenticator",
            "tagekyc_capture_runtime_operator","tagekyc_raw_export_claim_broker","tagekyc_raw_export_custody_encryptor",
            "tagekyc_raw_export_reconciler","tagekyc_raw_export_lifecycle"};
        Assert.Equal(roles.Order(StringComparer.Ordinal),(await observer.Database.SqlQuery<string>($"""
            SELECT rolname AS "Value" FROM pg_roles WHERE rolname=ANY({roles})
            """).ToArrayAsync()).Order(StringComparer.Ordinal));
        const string ti01="tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text)";
        const string ti02="tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint)";
        foreach(var signature in new[]{ti01,ti02})
        {
            Assert.True(await observer.Database.SqlQuery<bool>($"""
                SELECT p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
                 AND pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
                 AND NOT EXISTS(SELECT 1 FROM aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee=0 AND a.privilege_type='EXECUTE') AS "Value"
                FROM pg_proc p WHERE p.oid=to_regprocedure({signature})
                """).SingleAsync());
            var allowed=await observer.Database.SqlQuery<string>($"""
                SELECT rolname AS "Value" FROM pg_roles
                WHERE rolname=ANY({roles}) AND has_function_privilege(oid,to_regprocedure({signature}),'EXECUTE')
                """).ToArrayAsync();
            var expected=signature==ti01?new[]{"tagekyc_raw_export_custody_encryptor","tagekyc_raw_export_reconciler"}
                :new[]{"tagekyc_raw_export_reconciler"};
            Assert.Equal(expected.Order(StringComparer.Ordinal),allowed.Order(StringComparer.Ordinal));
            foreach(var role in roles.Except(expected))
            {
                await using var denied=RoleDb(isolated,role);
                var failure=signature==ti01
                    ?await Assert.ThrowsAsync<PostgresException>(()=>RecordIntent(denied,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH"))
                    :await Assert.ThrowsAsync<PostgresException>(()=>FinalizeIntent(denied,h));
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,failure.SqlState);
            }
        }
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        var original=await FullAttempt(observer,h);
        foreach(var changed in new[]{h with {SourceArtifactId=Guid.Empty},h with {AttemptId=Guid.Empty},
            h with {ExpectedEncryptionAttemptRevision=0},h with {ExpectedFence=0}})
        {
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>RecordIntent(writer,changed,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH"));
            Assert.Equal("P0001",failure.SqlState);
            Assert.Equal("A3_R2_TERMINAL_INTENT_ARGUMENT_INVALID",failure.MessageText);
            var denied=await FinalizeIntent(reconciler,changed);
            Assert.Equal("StateConflict",denied.Outcome);
            Assert.Null(denied.TerminalOutcomeCode);
        }
        var nullFailure=await Assert.ThrowsAsync<PostgresException>(()=>writer.Database.SqlQuery<TerminalIntentResult>($"""
            SELECT * FROM tagekyc.raw_export_record_retained_r2_terminal_intent(
             NULL::uuid,{h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence},'TerminatedBeforeStart','CONTENT_COMMITMENT_MISMATCH')
            """).SingleAsync());
        Assert.Equal("P0001",nullFailure.SqlState);
        Assert.Equal("A3_R2_TERMINAL_INTENT_ARGUMENT_INVALID",nullFailure.MessageText);
        var nullFinal=await reconciler.Database.SqlQuery<TerminalFinalResult>($"""
            SELECT * FROM tagekyc.raw_export_finalize_retained_r2_terminal(NULL::uuid,{h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence})
            """).SingleAsync();
        Assert.Equal("StateConflict",nullFinal.Outcome);
        Assert.Null(nullFinal.TerminalOutcomeCode);
        Assert.Equal(original,await FullAttempt(observer,h));
        await AssertDirectIntentWriteRejected(observer,h,"CONTENT_COMMITMENT_MISMATCH",false);
        Assert.Equal(original,await FullAttempt(observer,h));
        Assert.Equal("Recorded",(await RecordIntent(writer,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH")).Outcome);
        var recorded=await FullAttempt(observer,h);
        await AssertDirectIntentWriteRejected(observer,h,"RECAPTURE_REQUIRED",true);
        Assert.Equal(recorded,await FullAttempt(observer,h));
    }

    [Fact]
    public async Task Ti02_UnknownProviderPreservesIntentAndReturnsCleanupPending()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_unknown");
        var (_,h)=await CommitR1(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        var provider=new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,(await Provision(writer,h,provider)).Outcome);
        Assert.Equal(1,provider.Calls);
        Assert.Equal("Recorded",(await RecordIntent(writer,h,"Terminated","CONTENT_COMMITMENT_MISMATCH")).Outcome);
        var attempt=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        var result=await FinalizeIntent(reconciler,h);
        Assert.Equal("CleanupPending",result.Outcome);
        Assert.Null(result.TerminalOutcomeCode);
        Assert.Equal(attempt,await FullAttempt(observer,h));
        Assert.Equal(resources,await Rows(observer));
        Assert.Equal(3,await ProviderCount(observer));
        Assert.Equal(1,provider.Calls);
    }

    [Fact]
    public async Task Ti02_NotArmedObjectCannotUseO19PositiveAbsenceException()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_o19_notarmed");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await using var lifecycle=RoleDb(isolated,"tagekyc_raw_export_lifecycle");
        var objectEvidence=await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value=>value.AttemptId==h.AttemptId);
        Assert.Equal("NoObjectEstablished",objectEvidence.State);
        Assert.Equal("NotArmed",objectEvidence.PutOutcomeKind);
        Assert.Null(objectEvidence.PutOperationId);
        Assert.Null(objectEvidence.PutArmedAtUtc);
        Assert.Equal("Recorded",(await RecordIntent(writer,h,"Terminated","CONTENT_COMMITMENT_MISMATCH")).Outcome);
        await RevokeTerminalKey(lifecycle,h);
        var attemptBefore=await FullAttempt(observer,h);
        var resourcesBefore=await Rows(observer);
        var result=await reconciler.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_terminate_source_encryption_attempt(
             {h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence},'Terminated') AS "Value"
            """).SingleAsync();
        Assert.Equal("StateConflict",result);
        Assert.Equal("CleanupPending",(await FinalizeIntent(reconciler,h)).Outcome);
        Assert.Equal(attemptBefore,await FullAttempt(observer,h));
        Assert.Equal(resourcesBefore,await Rows(observer));
    }

    [Fact]
    public async Task Ti02_PositiveObjectAbsenceCannotSettleDifferentTerminalCause()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_ti_absence_wrong_cause");
        await using var minio=await DurableObjectMinioFixture.StartAsync();
        var unknown=new ObservationPutAdapter(()=>Task.CompletedTask);
        var (_,h,written,_)=await WriteIncompleteObject(isolated,unknown);
        Assert.Equal("PutOutcomeUnknown",written.ObjectState);
        await using var observer=isolated.CreateDbContext();
        await using var logins=await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var scopes=Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections,minio);
        var pipeline=new CaptureRuntimeSourcePipeline(scopes,null,_=>Task.CompletedTask);
        var advanced=await pipeline.AdvanceAsync(h.SourceArtifactId,1000,default);
        Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject,advanced.Step);
        var objectEvidence=await observer.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(value=>value.AttemptId==h.AttemptId);
        Assert.Equal("NoObjectEstablished",objectEvidence.State);
        Assert.Equal("PositiveAbsence",objectEvidence.PutOutcomeKind);
        Assert.NotNull(objectEvidence.PutOperationId);
        Assert.NotNull(objectEvidence.PutArmedAtUtc);
        Assert.NotNull(objectEvidence.OutcomeObservedAtUtc);
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await using var lifecycle=RoleDb(isolated,"tagekyc_raw_export_lifecycle");
        Assert.Equal("Recorded",(await RecordIntent(writer,h,"Terminated","RECAPTURE_REQUIRED")).Outcome);
        await RevokeTerminalKey(lifecycle,h);
        var attemptBefore=await FullAttempt(observer,h);
        var resourcesBefore=await Rows(observer);
        var result=await reconciler.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_terminate_source_encryption_attempt(
             {h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence},'Terminated') AS "Value"
            """).SingleAsync();
        Assert.Equal("StateConflict",result);
        Assert.Equal("CleanupPending",(await FinalizeIntent(reconciler,h)).Outcome);
        Assert.Equal(attemptBefore,await FullAttempt(observer,h));
        Assert.Equal(resourcesBefore,await Rows(observer));
    }

    [Fact]
    public async Task Ti02_O19PositiveAbsenceNegativeFamilyKeepsUnprovenAndMismatchedAttemptsPending()
    {
        await using var minio=await DurableObjectMinioFixture.StartAsync();
        // One provider fixture, isolated durable namespace per case. The positive case proves
        // the family is not green merely because termination is always rejected.
        foreach(var scenario in new[]{"provider-unknown","observation-incomplete","object-present",
            "principal-binding","revision","fence","positive-control"})
        {
            await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_o19_family_"+scenario.Replace('-','_'));
            RawIngressBrokerHandoff h;
            await using var observer=isolated.CreateDbContext();
            if(scenario=="provider-unknown")
            {
                (_,h)=await CommitR1(isolated);
                await using var preparing=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
                var provider=new UnknownProvider();
                Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,(await Provision(preparing,h,provider)).Outcome);
                Assert.Equal(1,provider.Calls);
            }
            else if(scenario=="object-present")
            {
                (_,h,_)=await TerminalPendingObject(isolated,minio);
                var objectEvidence=await observer.RawExportProvisionalObjects.AsNoTracking()
                    .SingleAsync(value=>value.AttemptId==h.AttemptId);
                Assert.NotEqual("NoObjectEstablished",objectEvidence.State);
            }
            else
            {
                var unknown=new ObservationPutAdapter(()=>Task.CompletedTask);
                var (_,handoff,written,_)=await WriteIncompleteObject(isolated,unknown);
                h=handoff;
                Assert.Equal("PutOutcomeUnknown",written.ObjectState);
                if(scenario!="observation-incomplete")
                {
                    await using var logins=await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
                        observer.Database.GetConnectionString()!);
                    await using var scopes=Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections,minio);
                    var pipeline=new CaptureRuntimeSourcePipeline(scopes,null,_=>Task.CompletedTask);
                    Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject,
                        (await pipeline.AdvanceAsync(h.SourceArtifactId,1000,default)).Step);
                    var objectEvidence=await observer.RawExportProvisionalObjects.AsNoTracking()
                        .SingleAsync(value=>value.AttemptId==h.AttemptId);
                    Assert.Equal("NoObjectEstablished",objectEvidence.State);
                    Assert.Equal("PositiveAbsence",objectEvidence.PutOutcomeKind);
                    Assert.NotNull(objectEvidence.PutOperationId);
                    Assert.NotNull(objectEvidence.PutArmedAtUtc);
                    Assert.NotNull(objectEvidence.OutcomeObservedAtUtc);
                }
                else
                {
                    var objectEvidence=await observer.RawExportProvisionalObjects.AsNoTracking()
                        .SingleAsync(value=>value.AttemptId==h.AttemptId);
                    Assert.NotEqual("PositiveAbsence",objectEvidence.PutOutcomeKind);
                    // Observation of an unknown result has a timestamp; it is not
                    // an observation of positive absence or provider quiescence.
                    Assert.Equal("OutcomeUnknown",objectEvidence.PutOutcomeKind);
                }
            }

            await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
            await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler",
                scenario=="principal-binding"?Guid.NewGuid():null);
            await using var lifecycle=RoleDb(isolated,"tagekyc_raw_export_lifecycle");
            Assert.Equal("Recorded",(await RecordIntent(writer,h,"Terminated","CONTENT_COMMITMENT_MISMATCH")).Outcome);
            if(scenario!="provider-unknown") await RevokeTerminalKey(lifecycle,h);
            var beforeAttempt=await FullAttempt(observer,h);
            var beforeResources=await Rows(observer);
            var revision=h.ExpectedEncryptionAttemptRevision+(scenario=="revision"?1:0);
            var fence=h.ExpectedFence+(scenario=="fence"?1:0);
            var result=await reconciler.Database.SqlQuery<string>($"""
                SELECT tagekyc.raw_export_terminate_source_encryption_attempt(
                 {h.AttemptId},{revision},{fence},'Terminated') AS "Value"
                """).SingleAsync();
            if(scenario=="positive-control")
            {
                Assert.True(result=="Terminated",$"O19 {scenario}: expected Terminated, actual {result}");
                Assert.Equal("Finalized",(await FinalizeIntent(reconciler,h)).Outcome);
                Assert.Equal("CONTENT_COMMITMENT_MISMATCH",await observer.Database.SqlQuery<string>($"""
                    SELECT "R2TerminalOutcomeCode" AS "Value" FROM tagekyc.raw_export_source_encryption_attempts
                    WHERE "AttemptId"={h.AttemptId}
                    """).SingleAsync());
            }
            else
            {
                Assert.True(result=="StateConflict",$"O19 {scenario}: expected StateConflict, actual {result}");
                Assert.Equal(beforeAttempt,await FullAttempt(observer,h));
                Assert.Equal(beforeResources,await Rows(observer));
            }
        }
    }

    [Fact]
    public async Task MigrationDownGuardMatchesInstalledA3FunctionBodies()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_down_body_guard");
        await using var observer=isolated.CreateDbContext();
        var migration=typeof(TagEkyc.Infrastructure.Persistence.Migrations.Tip88C1C6BA3RetainedIngressComposition);
        var field=migration.GetField("CaptureCurrentGuard",
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        var guard=Assert.IsType<string>(field.GetRawConstantValue());
        var signatures=System.Text.RegularExpressions.Regex.Matches(guard,
            @"\('(?<signature>[^']+)','(?<sha>[0-9a-f]{64})'\)");
        Assert.True(signatures.Count>=20);
        foreach(System.Text.RegularExpressions.Match entry in signatures)
        {
            var signature=entry.Groups["signature"].Value;
            var expected=entry.Groups["sha"].Value;
            var actual=await observer.Database.SqlQueryRaw<string>("""
                SELECT encode(tagekyc_extensions.digest(
                  convert_to(replace(p.prosrc,chr(13)||chr(10),chr(10)),'UTF8'),
                  'sha256'),'hex') AS "Value"
                FROM pg_catalog.pg_proc p
                WHERE p.oid=pg_catalog.to_regprocedure(@signature)
                """,new NpgsqlParameter("signature",signature)).SingleAsync();
            Assert.True(expected==actual,$"{signature}: expected {expected}, installed {actual}");
        }
    }

    [Fact]
    public async Task Recorder_IndependentCommitIgnoresDeadRequestAndRestoresActor()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_recorder_commit");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        var before=await AttemptWithoutIntent(observer,h);
        var resources=await Rows(observer);
        await using var source=RecorderSource(isolated);
        using var request=new CancellationTokenSource();
        request.Cancel();
        var recorder=new RawExportR2TerminalIntentRecorder(source,Principal,3000,CancellationToken.None);
        using(var callerTransaction=new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.RequiresNew,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.Equal(RawExportR2TerminalIntentOutcome.Recorded,await Record(recorder,h,request.Token));
            // Do not complete the caller's ambient transaction.
        }
        Assert.True(await observer.Database.SqlQuery<bool>($"""
            SELECT "R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH'
             AND "R2TerminalIntentDisposition"='TerminatedBeforeStart'
             AND "R2TerminalIntentAtUtc" IS NOT NULL AND "R2TerminalOutcomeCode" IS NULL AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());
        Assert.Equal(before,await AttemptWithoutIntent(observer,h));
        Assert.Equal(resources,await Rows(observer));
        var persisted=await FullAttempt(observer,h);
        Assert.Equal(RawExportR2TerminalIntentOutcome.ExistingMatch,await Record(recorder,h,request.Token));
        Assert.Equal(persisted,await FullAttempt(observer,h));
        // Pool reset is deliberately disabled in this fixture: the LOCAL GUC,
        // not connection reset/disposal, must restore the prior actor.
        await using var reused=await source.OpenConnectionAsync();
        await using var actor=new NpgsqlCommand("SELECT current_setting('tagekyc.actor_principal_id')",reused);
        Assert.Equal(RecorderBaselineActor.ToString("D"),await actor.ExecuteScalarAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Recorder_HostStopOrDeadlineRollsBackBlockedIntent(bool stopHost)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_recorder_stop");
        var (scope,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        await using var source=RecorderSource(isolated);
        int pid;
        await using(var warm=await source.OpenConnectionAsync())pid=warm.ProcessID;
        await using var blocker=isolated.CreateDbContext();
        await using var transaction=await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        using var host=new CancellationTokenSource();
        var recorder=new RawExportR2TerminalIntentRecorder(source,Principal,stopHost?30000:1500,host.Token);
        var pending=Record(recorder,h,CancellationToken.None);
        try
        {
            await WaitAtSession(observer,pid,scope.Session);
            if(stopHost)host.Cancel();
            var failed=await Task.WhenAny(pending,Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(pending,failed);
            await Assert.ThrowsAnyAsync<Exception>(async()=>await pending);
            Assert.Equal(before,await FullAttempt(observer,h));
            Assert.Equal(resources,await Rows(observer));
        }
        finally
        {
            host.Cancel();
            await transaction.RollbackAsync();
            try{await pending.WaitAsync(TimeSpan.FromSeconds(5));}catch(Exception){}
        }
        var fresh=new RawExportR2TerminalIntentRecorder(source,Principal,3000,CancellationToken.None);
        Assert.Equal(RawExportR2TerminalIntentOutcome.Recorded,await Record(fresh,h,CancellationToken.None));
    }

    [Fact]
    public async Task Recorder_CommitFailureNeverAcknowledgesPersistence()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_recorder_commit_fail");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        var before=await FullAttempt(observer,h);
        await using var source=RecorderSource(isolated);
        var recorder=new RawExportR2TerminalIntentRecorder(source,Principal,3000,CancellationToken.None);
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_recorder_commit_failure() RETURNS trigger LANGUAGE plpgsql AS $proof$
            BEGIN RAISE EXCEPTION 'A3_TEST_RECORDER_COMMIT_FAILURE'; END $proof$;
            CREATE CONSTRAINT TRIGGER a3_test_recorder_commit_failure
             AFTER UPDATE ON tagekyc.raw_export_source_encryption_attempts DEFERRABLE INITIALLY DEFERRED
             FOR EACH ROW WHEN (OLD."R2TerminalIntentCode" IS NULL AND NEW."R2TerminalIntentCode" IS NOT NULL)
             EXECUTE FUNCTION tagekyc.a3_test_recorder_commit_failure();
            """);
        try
        {
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>Record(recorder,h,CancellationToken.None));
            Assert.Equal("A3_TEST_RECORDER_COMMIT_FAILURE",failure.MessageText);
            Assert.Equal(before,await FullAttempt(observer,h));
        }
        finally
        {
            await observer.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER a3_test_recorder_commit_failure ON tagekyc.raw_export_source_encryption_attempts;
                DROP FUNCTION tagekyc.a3_test_recorder_commit_failure();
                """);
        }
        Assert.Equal(RawExportR2TerminalIntentOutcome.Recorded,await Record(recorder,h,CancellationToken.None));
    }

    [Fact]
    public async Task Recorder_DeniedTupleActorAndRoleHaveNoResidue()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_recorder_denial");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        await using var source=RecorderSource(isolated);
        var recorder=new RawExportR2TerminalIntentRecorder(source,Principal,3000,CancellationToken.None);
        Assert.Equal(RawExportR2TerminalIntentOutcome.StateConflict,await Record(recorder,h with {ExpectedFence=2},CancellationToken.None));
        Assert.Equal(RawExportR2TerminalIntentOutcome.NotFound,await Record(recorder,h with {AttemptId=Guid.NewGuid()},CancellationToken.None));
        var wrong=new RawExportR2TerminalIntentRecorder(source,RecorderBaselineActor,3000,CancellationToken.None);
        Assert.Equal(RawExportR2TerminalIntentOutcome.StateConflict,await Record(wrong,h,CancellationToken.None));
        await using var forbidden=RecorderSource(isolated,"tagekyc_raw_export_claim_broker");
        var denied=new RawExportR2TerminalIntentRecorder(forbidden,Principal,3000,CancellationToken.None);
        var failure=await Assert.ThrowsAsync<PostgresException>(()=>Record(denied,h,CancellationToken.None));
        Assert.Equal("42501",failure.SqlState);
        Assert.Equal(before,await FullAttempt(observer,h));
        Assert.Equal(resources,await Rows(observer));
        Assert.Equal(RawExportR2TerminalIntentOutcome.Recorded,await Record(recorder,h,CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Continuation_PreKeyAndProviderUnknownAreDiscoverableWithoutFabricatedOutcome(bool populatedHistory)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_prekey");
        await Prepare(isolated);
        var scope=await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated,rawIngress:true);
        await using var observer=isolated.CreateDbContext();
        await using var reader=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        if(populatedHistory)
        {
            // Real second-class admission/provider preparation, not manufactured
            // orphan rows or test-order dependence on a populated shared fixture.
            var artifact=Guid.NewGuid();
            observer.Set<TagEkyc.Infrastructure.Persistence.Entities.CaptureArtifactRow>().Add(new() {
                Id=artifact,VerificationSessionId=scope.Session,ArtifactType="NfcReadArtifact",CaptureSource="MobileSdk",
                CaptureAgentId="40000000000040008000000000000001",DeviceId="50000000000040008000000000000001",
                ArtifactHash="sha256:"+new string('c',64),MetadataHash="sha256:"+new string('d',64),QualityState="Accepted",
                RequestId="synthetic-pollution",CorrelationId="synthetic-pollution",CreatedAt=DateTimeOffset.UtcNow,
                ExpiresAt=DateTimeOffset.UtcNow.AddHours(1) });
            await observer.SaveChangesAsync();
            await using var acceptanceTransaction=await observer.Database.BeginTransactionAsync();
            await Actor(observer,Principal);
            var acceptance=await observer.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_append_capture_acceptance({scope.Session},{Client},'ChipDg2Portrait',{artifact},1,
                 'synthetic-challenge','synthetic-evidence','synthetic-acceptance-policy',1) AS "Value"
                """).SingleAsync();
            await acceptanceTransaction.CommitAsync();
            var unrelated=await AdmitPreparedScope(isolated,scope with {Artifact=artifact,Acceptance=acceptance},"ChipDg2Portrait");
            await using var unrelatedWriter=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
            var unrelatedProvider=new UnknownProvider();
            Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
                (await Provision(unrelatedWriter,unrelated,unrelatedProvider)).Outcome);
            Assert.Equal(1,unrelatedProvider.Calls);
            Assert.Equal(3,(await ProviderRows(observer,unrelated,true)).Length);
        }
        var scanBaseline=await Scan(reader,null,100);
        if(populatedHistory)Assert.NotEmpty(scanBaseline);
        var h=await AdmitPreparedScope(isolated,scope,"LiveSelfieImage");
        var unrelatedBefore=await ProviderRows(observer,h,false);
        if(populatedHistory)Assert.True(unrelatedBefore.Length>=3);
        var expectedScan=scanBaseline.Append(h.SourceArtifactId)
            .OrderBy(id=>id.ToString("N"),StringComparer.Ordinal).ToArray();
        var before=await Rows(observer);
        var row=Assert.Single(await Continuation(reader,h.SourceArtifactId));
        Assert.Equal(h.SourceArtifactId,row[0]);
        Assert.Equal(Principal,row[1]);
        Assert.Equal(h.ClientApplicationId,row[2]);
        Assert.Equal("Reserved",row[7]);
        Assert.Equal(1L,row[8]); // The freshly committed R1 custody head starts at revision 1.
        Assert.Equal(h.ExpectedFence,row[9]);
        Assert.Equal(h.AttemptId,row[10]);
        Assert.Equal(h.ExpectedEncryptionAttemptRevision,row[11]);
        Assert.Equal(h.AttemptKeyReservationId,row[12]);
        Assert.All(row.Skip(13),value=>Assert.Null(value));
        Assert.Equal(expectedScan,await Scan(reader,null,100));
        Assert.Equal(expectedScan.Where(id=>StringComparer.Ordinal.Compare(id.ToString("N"),h.SourceArtifactId.ToString("N"))>0),
            await Scan(reader,h.SourceArtifactId,100));
        Assert.Empty(await Continuation(reader,Guid.NewGuid()));
        Assert.Equal(before,await Rows(observer));
        Assert.Empty(await ProviderRows(observer,h,true));
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        var unknown=new UnknownProvider();
        Assert.Equal(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,(await Provision(writer,h,unknown)).Outcome);
        Assert.Equal(1,unknown.Calls);
        Assert.Equal(3,(await ProviderRows(observer,h,true)).Length);
        Assert.Equal(unrelatedBefore,await ProviderRows(observer,h,false));
        Assert.Equal(row,Assert.Single(await Continuation(reader,h.SourceArtifactId)));
        Assert.Equal(expectedScan.Take(1),await Scan(reader,null,1));
    }

    private static async Task<string[]> ProviderRows(TagEkycDbContext db,RawIngressBrokerHandoff h,bool owned)
    {
        // Each table is partitioned by the actual allocated attempt/key/object
        // identity. The complementary rows are compared byte-text, not ignored.
        var rows=new List<string>();
        foreach(var (table,predicate) in new[] {
            ("raw_export_attempt_key_reservations","r.\"AttemptId\"=@attempt OR r.\"AttemptKeyReservationId\"=@key"),
            ("raw_export_attempt_key_preparation_events","r.\"AttemptKeyReservationId\"=@key"),
            ("raw_export_key_provider_operations","r.\"AttemptKeyReservationId\"=@key"),
            ("raw_export_provisional_objects","r.\"AttemptId\"=@attempt OR r.\"AttemptKeyReservationId\"=@key OR r.\"ProvisionalObjectIdentity\"=(SELECT a.\"ProvisionalObjectIdentity\" FROM tagekyc.raw_export_source_encryption_attempts a WHERE a.\"AttemptId\"=@attempt)") })
        {
            var sql=$"SELECT '{table}:'||to_jsonb(r)::text AS \"Value\" FROM tagekyc.{table} r WHERE ({predicate})=@owned ORDER BY to_jsonb(r)::text";
            rows.AddRange(await db.Database.SqlQueryRaw<string>(sql,
                new NpgsqlParameter("attempt",h.AttemptId),new NpgsqlParameter("key",h.AttemptKeyReservationId),
                new NpgsqlParameter("owned",owned)).ToArrayAsync());
        }
        return rows.ToArray();
    }

    [Fact]
    public async Task Continuation_PendingIntentRemainsUntilExactSettledFinalization()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_intent");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var reader=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        var intent=await RecordIntent(writer,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH");
        var pending=Assert.Single(await Continuation(reader,h.SourceArtifactId));
        Assert.Equal("CONTENT_COMMITMENT_MISMATCH",pending[20]);
        Assert.Equal("TerminatedBeforeStart",pending[21]);
        Assert.Equal(intent.TerminalIntentAtUtc,Assert.IsType<DateTimeOffset>(pending[22]));
        Assert.Null(pending[23]);Assert.Null(pending[24]);Assert.Null(pending[25]);
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(reader,null,100));
        Assert.Equal("CleanupPending",(await FinalizeIntent(reader,h)).Outcome);
        Assert.Equal(pending,Assert.Single(await Continuation(reader,h.SourceArtifactId)));
        await using var lifecycle=RoleDb(isolated,"tagekyc_raw_export_lifecycle");
        await RevokeTerminalKey(lifecycle,h);
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(reader,null,100));
        Assert.Equal("Finalized",(await FinalizeIntent(reader,h)).Outcome);
        var final=Assert.Single(await Continuation(reader,h.SourceArtifactId));
        Assert.Equal("CONTENT_COMMITMENT_MISMATCH",final[25]);
        Assert.Equal("TerminatedBeforeStart",final[23]);
        Assert.True(Assert.IsType<DateTimeOffset>(final[24])>=Assert.IsType<DateTimeOffset>(final[22]));
        Assert.Empty(await Scan(reader,null,100));
        var before=await FullAttempt(observer,h);
        Assert.Equal("ExistingMatch",(await FinalizeIntent(reader,h)).Outcome);
        Assert.Equal(final,Assert.Single(await Continuation(reader,h.SourceArtifactId)));
        Assert.Equal(before,await FullAttempt(observer,h));
    }

    [Fact]
    public async Task Continuation_ExactAclAndBoundedScanArguments()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_acl");
        var (_,h)=await CommitR1(isolated);
        await using var observer=isolated.CreateDbContext();
        var roles=new[]{"tagekyc_runtime","tagekyc_raw_export_claim_broker","tagekyc_raw_export_custody_encryptor",
            "tagekyc_raw_export_reconciler","tagekyc_raw_export_lifecycle","tagekyc_capture_runtime_application",
            "tagekyc_capture_runtime_authenticator","tagekyc_capture_runtime_operator"};
        foreach(var role in roles)
        {
            await using var connection=RoleDb(isolated,role);
            if(role is "tagekyc_raw_export_custody_encryptor" or "tagekyc_raw_export_reconciler" or "tagekyc_raw_export_lifecycle")
                Assert.Single(await Continuation(connection,h.SourceArtifactId));
            else Assert.Equal("42501",(await Assert.ThrowsAsync<PostgresException>(()=>Continuation(connection,h.SourceArtifactId))).SqlState);
            if(role is "tagekyc_raw_export_reconciler" or "tagekyc_raw_export_lifecycle")
                Assert.Equal(new[]{h.SourceArtifactId},await Scan(connection,null,100));
            else Assert.Equal("42501",(await Assert.ThrowsAsync<PostgresException>(()=>Scan(connection,null,100))).SqlState);
        }
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        foreach(var invalid in new int?[]{null,0,-1,101})
        {
            var failure=await Assert.ThrowsAsync<PostgresException>(()=>Scan(reconciler,null,invalid));
            Assert.Equal("P0001",failure.SqlState);
            Assert.Equal("A3_CONTINUATION_SCAN_ARGUMENT_INVALID",failure.MessageText);
        }
        Assert.Equal(2,await observer.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
             WHERE n.nspname='tagekyc' AND p.proname IN
              ('raw_export_read_retained_source_continuation','raw_export_list_retained_source_continuations')
             AND p.prosecdef AND pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
             AND p.proconfig=ARRAY['search_path=pg_catalog']
             AND NOT EXISTS(SELECT 1 FROM aclexplode(p.proacl) x WHERE x.grantee=0)
            """).SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Continuation_OperationalTerminationMustAgreeWithDurableIntent(bool matching)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_disposition");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var observer=isolated.CreateDbContext();
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var reconciler=RoleDb(isolated,"tagekyc_raw_export_reconciler");
        await using var lifecycle=RoleDb(isolated,"tagekyc_raw_export_lifecycle");
        var disposition=matching?"TerminatedBeforeStart":"Terminated";
        Assert.Equal("Recorded",(await RecordIntent(writer,h,disposition,"CONTENT_COMMITMENT_MISMATCH")).Outcome);
        await RevokeTerminalKey(lifecycle,h);
        var before=await FullAttempt(observer,h);
        var resources=await Rows(observer);
        Assert.Equal(matching?"Terminated":"StateConflict",await Terminate(reconciler,h));
        Assert.Equal(resources,await Rows(observer));
        if(matching)
        {
            Assert.Equal("Finalized",(await FinalizeIntent(reconciler,h)).Outcome);
            Assert.Equal("CONTENT_COMMITMENT_MISMATCH",Assert.Single(await Continuation(reconciler,h.SourceArtifactId))[25]);
            Assert.Empty(await Scan(reconciler,null,100));
        }
        else
        {
            Assert.Equal(before,await FullAttempt(observer,h));
            var stillPending=Assert.Single(await Continuation(reconciler,h.SourceArtifactId));
            Assert.Null(stillPending[23]);Assert.Null(stillPending[24]);Assert.Null(stillPending[25]);
            Assert.Equal(new[]{h.SourceArtifactId},await Scan(reconciler,null,100));
            await using(var tx=await observer.Database.BeginTransactionAsync())
            {
                await Actor(observer,Principal);
                await observer.Database.ExecuteSqlRawAsync("""
                    SET LOCAL ROLE tagekyc_raw_export_deployer;
                    SELECT set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r2-termination-v1',true)
                    """);
                var denied=await Assert.ThrowsAsync<PostgresException>(()=>observer.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE tagekyc.raw_export_source_encryption_attempts SET
                     "R2TerminationDisposition"='TerminatedBeforeStart',"R2TerminatedAtUtc"=clock_timestamp()
                     WHERE "AttemptId"={h.AttemptId}
                    """));
                Assert.Equal("RAW_EXPORT_SOURCE_CORE_APPEND_ONLY",denied.MessageText);
                await tx.RollbackAsync();
            }
            Assert.Equal(before,await FullAttempt(observer,h));
        }
    }

    [Fact]
    public async Task Continuation_TypedRepositoryReadsExactCommittedMetadata()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_typed");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var source=RecorderSource(isolated,"tagekyc_raw_export_reconciler");
        var repository=new RawSourceRetentionContinuationRepository(source);
        await using var observer=isolated.CreateDbContext();
        var raw=Assert.Single(await Continuation(observer,h.SourceArtifactId));
        var typed=Assert.IsType<RawSourceRetentionContinuation>(await repository.ReadAsync(h.SourceArtifactId,CancellationToken.None));
        var properties=new[]{"SourceArtifactId","CustodyPrincipalId","ClientApplicationId","VerificationSessionId",
            "RuntimeBindingId","RetentionAuthorityId","RetentionAuthorityRevision","CustodyState","ReservationRevision",
            "Fence","AttemptId","EncryptionAttemptRevision","AttemptKeyReservationId","ObjectCustodyId","ObjectState",
            "ObjectStateRevision","SourcePublicationId","PublicationRevision","PublicationState","CleanupDisposition",
            "R2TerminalIntentCode","R2TerminalIntentDisposition","R2TerminalIntentAtUtc","R2TerminationDisposition",
            "R2TerminatedAtUtc","R2TerminalOutcomeCode"};
        Assert.Equal(26,typeof(RawSourceRetentionContinuation).GetProperties().Length);
        Assert.Equal(raw,properties.Select(name=>typeof(RawSourceRetentionContinuation).GetProperty(name)!.GetValue(typed)).ToArray());
        Assert.Equal(new[]{h.SourceArtifactId},await repository.ScanAsync(null,100,CancellationToken.None));
        Assert.Empty(await repository.ScanAsync(h.SourceArtifactId,100,CancellationToken.None));
        Assert.Null(await repository.ReadAsync(Guid.NewGuid(),CancellationToken.None));
        var error=await Assert.ThrowsAsync<PostgresException>(()=>repository.ScanAsync(null,101,CancellationToken.None));
        Assert.Equal("A3_CONTINUATION_SCAN_ARGUMENT_INVALID",error.MessageText);
    }

    [Theory]
    [InlineData("key-attempt")]
    [InlineData("key-fingerprint")]
    [InlineData("object-fence")]
    [InlineData("object-key")]
    [InlineData("terminal-disposition")]
    public async Task Continuation_ContradictoryDurableTuplesAreNotExecutableWork(string mutation)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_cp_tuple");
        var (_,h)=await TerminalReadyObject(isolated);
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        Assert.Equal("Recorded",(await RecordIntent(writer,h,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH")).Outcome);
        await using var observer=isolated.CreateDbContext();
        var baseline=Assert.Single(await Continuation(observer,h.SourceArtifactId));
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(observer,null,100));
        await using(var tx=await observer.Database.BeginTransactionAsync())
        {
            // Deliberately corrupt only a disposable fixture transaction to
            // exercise the reader independently of the write-side defenses.
            await observer.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role=replica");
            var sql=mutation switch
            {
                "key-attempt"=>"UPDATE tagekyc.raw_export_attempt_key_reservations SET \"AttemptId\"=gen_random_uuid()",
                "key-fingerprint"=>"UPDATE tagekyc.raw_export_attempt_key_reservations SET \"EncryptionAttemptFingerprint\"=decode(repeat('00',32),'hex')",
                "object-fence"=>"UPDATE tagekyc.raw_export_provisional_objects SET \"AttemptFence\"=\"AttemptFence\"+1",
                "object-key"=>"UPDATE tagekyc.raw_export_provisional_objects SET \"AttemptKeyReservationId\"=gen_random_uuid()",
                "terminal-disposition"=>"UPDATE tagekyc.raw_export_source_encryption_attempts SET \"R2TerminationDisposition\"='Terminated',\"R2TerminatedAtUtc\"=clock_timestamp()",
                _=>throw new InvalidOperationException(),
            };
            var key=mutation.StartsWith("key-",StringComparison.Ordinal)?h.AttemptKeyReservationId:h.AttemptId;
            sql+=mutation.StartsWith("key-",StringComparison.Ordinal)?" WHERE \"AttemptKeyReservationId\"={0}":" WHERE \"AttemptId\"={0}";
            Assert.Equal(1,await observer.Database.ExecuteSqlRawAsync(sql,key));
            Assert.Empty(await Continuation(observer,h.SourceArtifactId));
            Assert.Empty(await Scan(observer,null,100));
            await tx.RollbackAsync();
        }
        Assert.Equal(baseline,Assert.Single(await Continuation(observer,h.SourceArtifactId)));
        Assert.Equal(new[]{h.SourceArtifactId},await Scan(observer,null,100));
    }

    private static async Task<object?[][]> Continuation(TagEkycDbContext db,Guid source)
    {
        await db.Database.OpenConnectionAsync();
        await using var sql=db.Database.GetDbConnection().CreateCommand();
        sql.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();
        sql.CommandText="SELECT * FROM tagekyc.raw_export_read_retained_source_continuation(@id)";
        sql.Parameters.Add(new NpgsqlParameter("id",source));
        await using var reader=await sql.ExecuteReaderAsync();
        Assert.Equal(new[]{"SourceArtifactId","CustodyPrincipalId","ClientApplicationId","VerificationSessionId",
            "RuntimeBindingId","RetentionAuthorityId","RetentionAuthorityRevision","CustodyState","ReservationRevision",
            "Fence","AttemptId","EncryptionAttemptRevision","AttemptKeyReservationId","ObjectCustodyId","ObjectState",
            "ObjectStateRevision","SourcePublicationId","PublicationRevision","PublicationState","CleanupDisposition",
            "R2TerminalIntentCode","R2TerminalIntentDisposition","R2TerminalIntentAtUtc","R2TerminationDisposition",
            "R2TerminatedAtUtc","R2TerminalOutcomeCode"},Enumerable.Range(0,reader.FieldCount).Select(reader.GetName));
        var rows=new List<object?[]>();
        while(await reader.ReadAsync())
        {
            var values=new object?[26];
            for(var i=0;i<26;i++)values[i]=reader.IsDBNull(i)?null:i is 22 or 24?reader.GetFieldValue<DateTimeOffset>(i):reader.GetValue(i);
            rows.Add(values);
        }
        return rows.ToArray();
    }

    private static Task<Guid[]> Scan(TagEkycDbContext db,Guid? after,int? limit)=>db.Database.SqlQuery<Guid>($"""
        SELECT "SourceArtifactId" AS "Value" FROM tagekyc.raw_export_list_retained_source_continuations({after},{limit})
        """).ToArrayAsync();

    private static readonly Guid RecorderBaselineActor=Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");
    private static NpgsqlDataSource RecorderSource(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        string role="tagekyc_raw_export_custody_encryptor")
    {
        using var owner=isolated.CreateDbContext();
        return NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(owner.Database.GetConnectionString())
        {
            Options=$"-c role={role} -c tagekyc.actor_principal_id={RecorderBaselineActor:D}",
            Pooling=true,MaxPoolSize=1,NoResetOnClose=true,
        }.ConnectionString);
    }
    private static Task<RawExportR2TerminalIntentOutcome> Record(IRawExportR2TerminalIntentRecorder recorder,
        RawIngressBrokerHandoff h,CancellationToken token)=>recorder.RecordAsync(h.SourceArtifactId,h.AttemptId,
            h.ExpectedEncryptionAttemptRevision,h.ExpectedFence,"TerminatedBeforeStart","CONTENT_COMMITMENT_MISMATCH",token);

    private static Task<DateTimeOffset> OriginalHorizon(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        db.Database.SqlQuery<DateTimeOffset>($"""
            SELECT LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",
             s."ValidUntilUtc",b."ExecutionExpiresAtUtc") AS "Value"
            FROM tagekyc.raw_export_source_reservations r
            JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
            JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
            WHERE r."SourceArtifactId"={h.SourceArtifactId}
            """).SingleAsync();

    private static async Task AssertDirectIntentWriteRejected(TagEkycDbContext db,RawIngressBrokerHandoff h,string code,bool causeOnly)
    {
        await using var transaction=await db.Database.BeginTransactionAsync();
        await Actor(db,Principal);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_source_core_write_context','',true)");
        var failure=causeOnly
            ?await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tagekyc.raw_export_source_encryption_attempts SET "R2TerminalIntentCode"={code} WHERE "AttemptId"={h.AttemptId}
                """))
            :await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tagekyc.raw_export_source_encryption_attempts SET "R2TerminalIntentCode"={code},
                 "R2TerminalIntentDisposition"='TerminatedBeforeStart',"R2TerminalIntentAtUtc"=clock_timestamp() WHERE "AttemptId"={h.AttemptId}
                """));
        Assert.Equal("P0001",failure.SqlState);
        Assert.Equal("A3_R2_TERMINAL_INTENT_IMMUTABLE",failure.MessageText);
        await transaction.RollbackAsync();
    }

    private static async Task<(Tip88C1C6BA3RetentionCheckpointTests.Scope,RawIngressBrokerHandoff)> TerminalReadyObject(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        var result=await CommitR1(isolated);
        var h=result.Item2;
        await using var writer=RoleDb(isolated,"tagekyc_raw_export_custody_encryptor");
        await using var journalWrite=isolated.CreateDbContext();
        await using var journalRead=isolated.CreateDbContext();
        var kek=new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(journalWrite),new PostgresFixtureKekJournal(journalRead));
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated,(await Provision(writer,h,kek)).Outcome);
        var begun=await BeginObject(writer,h);
        Assert.Equal("Created",begun.OutcomeCode);
        var digest=C1HashCanonical.Compute("tip-88c1-object-not-armed-evidence-v1",
            new C1HashCanonical.Scalar(Convert.ToHexString(begun.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(begun.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("NotArmed"));
        Assert.Equal("NotArmed",await writer.Database.SqlQuery<string>($"""
            SELECT "OutcomeCode" AS "Value" FROM tagekyc.raw_export_record_provisional_object_not_armed(
             {begun.ObjectCustodyId},{begun.StateRevision},{digest})
            """).SingleAsync());
        return result;
    }

    private static async Task<(Tip88C1C6BA3RetentionCheckpointTests.Scope,RawIngressBrokerHandoff,Guid)> TerminalPendingObject(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, DurableObjectMinioFixture minio)
    {
        var result = await CommitR1(isolated);
        var h = result.Item2;
        byte[] plaintext = "synthetic-retainedsource"u8.ToArray();
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var services = new ServiceCollection().AddTagEkycContentCommitment(configuration)
            .BuildServiceProvider();
        var commitments = services.GetRequiredService<IContentCommitmentService>();
        await using var writerDb = isolated.CreateDbContext();
        await using var writerLookup = isolated.CreateDbContext();
        await using var recorderSource = NpgsqlDataSource.Create(writerDb.Database.GetConnectionString()!);
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb), new PostgresFixtureKekJournal(writerLookup));
        var writer = new RawExportR2EncryptionOrchestrator(new RawExportR2Repository(writerDb),
            new PostgresAttemptKeyReservationProvider(writerDb, new PostgresKeyProviderOperationMap(writerDb), kek),
            new AttemptAeadEncryptionOperationService(writerDb, kek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            commitments, new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)),
            new RawExportR2TerminalIntentRecorder(recorderSource, Principal, 3000, CancellationToken.None),
            classMaximumBytes: 24);
        await using var body = new MemoryStream(plaintext, writable: false);
        var written = await writer.ExecuteAsync(new(Principal, h.AttemptKeyReservationId, h.AttemptId,
            h.SourceArtifactId, h.ExpectedEncryptionAttemptRevision, h.ExpectedFence, body), CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        Assert.NotNull(written.ObjectCustodyId);
        return (result.Item1, h, written.ObjectCustodyId.Value);
    }

    private static async Task<(Tip88C1C6BA3RetentionCheckpointTests.Scope Scope,
        RawIngressBrokerHandoff Handoff, RawExportR2WriterResult Written,
        FixtureDurableKekOperationProvider KeyProvider)> WriteIncompleteObject(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        IProvisionalObjectWriter provider)
    {
        var result = await CommitR1(isolated);
        var h = result.Item2;
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef"
        };
        await using var services = new ServiceCollection().AddTagEkycContentCommitment(configuration)
            .BuildServiceProvider();
        await using var writerDb = isolated.CreateDbContext();
        await using var writerLookup = isolated.CreateDbContext();
        await using var recorderSource = NpgsqlDataSource.Create(writerDb.Database.GetConnectionString()!);
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb), new PostgresFixtureKekJournal(writerLookup));
        var orchestrator = new RawExportR2EncryptionOrchestrator(new RawExportR2Repository(writerDb),
            new PostgresAttemptKeyReservationProvider(writerDb,
                new PostgresKeyProviderOperationMap(writerDb), kek),
            new AttemptAeadEncryptionOperationService(writerDb, kek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            services.GetRequiredService<IContentCommitmentService>(), provider,
            new RawExportR2TerminalIntentRecorder(recorderSource, Principal, 3000, CancellationToken.None),
            classMaximumBytes: 24);
        await using var body = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);
        var written = await orchestrator.ExecuteAsync(new(Principal, h.AttemptKeyReservationId,
            h.AttemptId, h.SourceArtifactId, h.ExpectedEncryptionAttemptRevision,
            h.ExpectedFence, body), CancellationToken.None);
        Assert.NotNull(written.ObjectCustodyId);
        return (result.Item1, h, written, kek);
    }

    private static Task<ObsoleteResidue> AddObsoleteVerifiedAttemptAsync(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        RawIngressBrokerHandoff winner,
        IProvisionalObjectWriter writer) => AddObsoleteVerifiedAttemptAsync(isolated,
            winner.SourceArtifactId, winner.AttemptId, winner.AttemptKeyReservationId, writer);

    internal static async Task<ObsoleteResidue> AddObsoleteVerifiedAttemptAsync(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Guid sourceArtifactId, Guid winnerAttemptId, Guid winnerKeyReservationId,
        IProvisionalObjectWriter writer)
    {
        var attemptId = Guid.ParseExact("00000000000000000000000000000003", "N");
        var resourceId = Guid.ParseExact("00000000000000000000000000000013", "N");
        var objectIdentity = Guid.NewGuid();
        var preparationId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var fingerprint = RandomNumberGenerator.GetBytes(32);
        var contextFingerprint = RandomNumberGenerator.GetBytes(32);
        var bindingDigest = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var objectKey = "raw-export/c1/v1/" + objectIdentity.ToString("N");

        await using var connectionSource = isolated.CreateDbContext();
        await using var connection = new NpgsqlConnection(connectionSource.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand("""
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','complete-r1',true);
            INSERT INTO tagekyc.raw_export_source_encryption_attempts(
              "AttemptId","SourceArtifactId","EncryptionAttemptRevision","Fence","ProvisionalObjectIdentity","AttemptKeyReservationId",
              "KeyProviderId","KekId","KekVersion","KekFingerprint","EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
              "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize","FramingParametersDigest",
              "EncryptionAttemptFingerprint","OwnershipLeaseExpiresAtUtc","R2TerminationDisposition","R2TerminatedAtUtc",
              "StagedCiphertextFingerprintSchemaVersion","StagedCiphertextFingerprint","StagedObjectCustodyId","StagedObjectStateRevision",
              "StagedFromReservationRevision","VerifiedPlaintextLength","StagedCiphertextLength","StagedCiphertextDigest",
              "StagedProviderReceiptDigest","StagedVerificationEvidenceDigest","StagedAtUtc","CreatedAtUtc","SchemaVersion")
            SELECT @attempt,"SourceArtifactId","EncryptionAttemptRevision"+1,"Fence"+1,@identity,@resource,
              "KeyProviderId","KekId","KekVersion","KekFingerprint","EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
              "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize","FramingParametersDigest",
              @fingerprint,pg_catalog.clock_timestamp()+interval '1 hour',NULL,NULL,
              NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,pg_catalog.clock_timestamp(),"SchemaVersion"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=@winner AND "SourceArtifactId"=@source;

            SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
            INSERT INTO tagekyc.raw_export_attempt_key_reservations(
              "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint","KeyProviderId","KekId","KekVersion","KekFingerprint",
              "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion","PreparationDisposition","CurrentPreparationId",
              "CurrentPreparationFence","CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken","ResolutionAttemptCount",
              "NextResolutionAttemptNotBeforeUtc","ResolutionDeadlineUtc","CleanupAttemptCount","NextCleanupAttemptNotBeforeUtc",
              "CleanupDeadlineUtc","CleanupOperatorInterventionRequired","WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
              "WrappedDekMetadataDigest","RowRevision","PreparedAtUtc","RevokedAtUtc","RevocationReasonCode","CreatedAtUtc","UpdatedAtUtc")
            SELECT @resource,@attempt,@fingerprint,"KeyProviderId","KekId","KekVersion","KekFingerprint",
              @context,"WrappingSuiteId","WrappingSuiteVersion",'Active',@preparation,"CurrentPreparationFence",
              "CurrentPreparationLeaseExpiresAtUtc",@token,"ResolutionAttemptCount","NextResolutionAttemptNotBeforeUtc","ResolutionDeadlineUtc",
              "CleanupAttemptCount","NextCleanupAttemptNotBeforeUtc","CleanupDeadlineUtc","CleanupOperatorInterventionRequired",
              "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag","WrappedDekMetadataDigest",1,"PreparedAtUtc",NULL,NULL,
              pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp()
            FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=@winnerKey;

            INSERT INTO tagekyc.raw_export_key_provider_operations(
              "ProviderOperationId","KeyProviderId","ProviderOperationToken","AttemptKeyReservationId","PreparationId","PreparationFence",
              "AttemptKeyContextFingerprint","ProviderOperationState","WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
              "WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","ProviderResourceReference","ProviderOperationReceipt",
              "ResultObservedAtUtc","ProviderCleanupReference","ProviderCleanupReceipt","ProviderAbsenceProofReceipt","IssuedAtUtc","UpdatedAtUtc")
            SELECT @operation,"KeyProviderId",@token,@resource,@preparation,"PreparationFence",@context,'ResultObserved',
              "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion",
              "ProviderResourceReference","ProviderOperationReceipt","ResultObservedAtUtc",NULL,NULL,NULL,
              pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp()
            FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=@winnerKey
            ORDER BY "UpdatedAtUtc" DESC LIMIT 1;

            SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
            INSERT INTO tagekyc.raw_export_provisional_objects(
              "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity","EncryptionAttemptRevision",
              "AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest","State","StateRevision","PutOperationId",
              "PutArmedAtUtc","PutOutcomeKind","OutcomeObservedAtUtc","CiphertextLength","CiphertextDigest","ProviderReceiptDigest",
              "VerificationEvidenceDigest","VerifiedAtUtc","CleanupReasonCode","CleanupEvidenceDigest","CleanupRequestedAtUtc",
              "DeletionEvidenceDigest","DeletionEvidenceKind","DeletedAtUtc","QuarantineReasonCode","QuarantineEvidenceDigest",
              "QuarantinedAtUtc","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
            SELECT @resource,@attempt,@resource,"SourceArtifactId",@identity,"EncryptionAttemptRevision"+1,"AttemptFence"+1,@fingerprint,
              @objectKey,@binding,'VerifiedCompleted',"StateRevision",@operation,"PutArmedAtUtc","PutOutcomeKind","OutcomeObservedAtUtc",
              "CiphertextLength","CiphertextDigest","ProviderReceiptDigest","VerificationEvidenceDigest","VerifiedAtUtc",
              NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp(),"SchemaVersion"
            FROM tagekyc.raw_export_provisional_objects WHERE "AttemptId"=@winner;
            """, connection, tx);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("resource", resourceId);
        command.Parameters.AddWithValue("identity", objectIdentity);
        command.Parameters.AddWithValue("preparation", preparationId);
        command.Parameters.AddWithValue("operation", operationId);
        command.Parameters.AddWithValue("fingerprint", fingerprint);
        command.Parameters.AddWithValue("context", contextFingerprint);
        command.Parameters.AddWithValue("binding", bindingDigest);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("objectKey", objectKey);
        command.Parameters.AddWithValue("winner", winnerAttemptId);
        command.Parameters.AddWithValue("winnerKey", winnerKeyReservationId);
        command.Parameters.AddWithValue("source", sourceArtifactId);
        await command.ExecuteNonQueryAsync();
        await tx.CommitAsync();

        var bytes = "synthetic-obsolete-ciphertext"u8.ToArray();
        await using var body = new MemoryStream(bytes, writable: false);
        var put = await writer.PutIfAbsentAsync(new(new(objectIdentity, objectKey, bindingDigest),
            operationId, bytes.Length), body, default);
        Assert.Equal(ConditionalPutOutcome.Created, put.Outcome);
        return new(attemptId, resourceId, resourceId);
    }

    internal sealed record ObsoleteResidue(Guid AttemptId, Guid AttemptKeyReservationId,
        Guid ObjectCustodyId);

    private static async Task RevokeTerminalKey(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        Assert.Equal("Revoked",await db.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_revoke_attempt_key_reservation({h.AttemptKeyReservationId},'synthetic-terminal-settlement') AS "Value"
            """).SingleAsync());

    private static Task<string> KeyState(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<string>($"""
            SELECT "PreparationDisposition" AS "Value" FROM tagekyc.raw_export_attempt_key_reservations
            WHERE "AttemptKeyReservationId"={h.AttemptKeyReservationId}
            """).SingleAsync();

    private static Task<int> KeyEventCount(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_attempt_key_preparation_events
            WHERE "AttemptKeyReservationId"={h.AttemptKeyReservationId}
            """).SingleAsync();

    private static Task<bool> KeyRevocationEvidenceMatches(TagEkycDbContext db, RawIngressBrokerHandoff h,
        string reason) => db.Database.SqlQuery<bool>($"""
            SELECT EXISTS(
              SELECT 1
              FROM tagekyc.raw_export_attempt_key_reservations k
              JOIN tagekyc.raw_export_attempt_key_preparation_events e
                ON e."AttemptKeyReservationId"=k."AttemptKeyReservationId"
               AND e."EventKind"='Revoked'
              WHERE k."AttemptKeyReservationId"={h.AttemptKeyReservationId}
                AND k."RevocationReasonCode"={reason}
                AND e."RevocationReasonCode"={reason}
                AND e."RevocationEvidenceDigest"=tagekyc.raw_export_c1_hash_canonical(
                  'tip-88c1-key-revocation-evidence-v1',
                  encode(k."AttemptKeyContextFingerprint",'hex'),
                  replace(k."CurrentPreparationId"::text,'-',''),
                  k."CurrentPreparationFence"::text,
                  {reason})
            ) AS "Value"
            """).SingleAsync();

    private static Task<string> ObjectRow(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<string>($"""
            SELECT to_jsonb(o)::text AS "Value" FROM tagekyc.raw_export_provisional_objects o
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();

    private static Task<int> ObjectCount(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_provisional_objects
            WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();

    private static Task<string> ObjectState(TagEkycDbContext db, Guid objectId) =>
        db.Database.SqlQuery<string>($"""
            SELECT "State" AS "Value" FROM tagekyc.raw_export_provisional_objects
            WHERE "ObjectCustodyId"={objectId}
            """).SingleAsync();

    private static Task<int> ObjectEventCount(TagEkycDbContext db, Guid objectId) =>
        db.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_provisional_object_events
            WHERE "ObjectCustodyId"={objectId}
            """).SingleAsync();

    private static async Task<string[]> CleanupRows(TagEkycDbContext db, Guid publicationId)
    {
        var publication = await db.RawExportSourcePublications.AsNoTracking()
            .SingleAsync(value => value.SourcePublicationId == publicationId);
        var items = await db.RawExportSourceCleanupItems.AsNoTracking()
            .Where(value => value.SourcePublicationId == publicationId)
            .OrderBy(value => value.CleanupItemId).ToListAsync();
        var resourceIds = items.Select(value => value.ResourceId).ToArray();
        var objects = await db.RawExportProvisionalObjects.AsNoTracking()
            .Where(value => resourceIds.Contains(value.ObjectCustodyId))
            .OrderBy(value => value.ObjectCustodyId).ToListAsync();
        var keys = await db.RawExportAttemptKeyReservations.AsNoTracking()
            .Where(value => resourceIds.Contains(value.AttemptKeyReservationId))
            .OrderBy(value => value.AttemptKeyReservationId).ToListAsync();
        var rows = new List<string>
        {
            $"publication:{publication.PublicationRevision}:{publication.CleanupDisposition}:{Convert.ToHexString(publication.CleanupEvidenceDigest ?? [])}",
        };
        rows.AddRange(items.Select(value => $"item:{value.CleanupItemId:N}:{value.ResourceKind}:{value.ResourceId:N}:{value.CleanupState}:{value.CompletionDisposition}:{value.RowRevision}:{Convert.ToHexString(value.CleanupEvidenceDigest ?? [])}"));
        rows.AddRange(objects.Select(value => $"object:{value.ObjectCustodyId:N}:{value.State}:{value.StateRevision}:{value.DeletionEvidenceKind}:{Convert.ToHexString(value.DeletionEvidenceDigest ?? [])}"));
        rows.AddRange(keys.Select(value => $"key:{value.AttemptKeyReservationId:N}:{value.PreparationDisposition}:{value.RowRevision}:{value.RevocationReasonCode}"));
        return rows.ToArray();
    }

    private static Task<string> ObjectDeletionEvidenceKind(TagEkycDbContext db, Guid objectId) =>
        db.Database.SqlQuery<string>($"""
            SELECT "DeletionEvidenceKind" AS "Value" FROM tagekyc.raw_export_provisional_objects
            WHERE "ObjectCustodyId"={objectId}
            """).SingleAsync();

    private static Task<string?> ObjectDeletionEvidenceKindOrNull(TagEkycDbContext db, Guid objectId) =>
        db.Database.SqlQuery<string?>($"""
            SELECT "DeletionEvidenceKind" AS "Value" FROM tagekyc.raw_export_provisional_objects
            WHERE "ObjectCustodyId"={objectId}
            """).SingleAsync();

    private static Task<string[]> ProviderOperationRows(TagEkycDbContext db, RawIngressBrokerHandoff h) =>
        db.Database.SqlQuery<string>($"""
            SELECT to_jsonb(o)::text AS "Value" FROM tagekyc.raw_export_key_provider_operations o
            WHERE "AttemptKeyReservationId"={h.AttemptKeyReservationId} ORDER BY "ProviderOperationId"
            """).ToArrayAsync();

    private static Task<string> FullAttempt(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        db.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();

    private static Task<string> AttemptWithoutIntent(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        db.Database.SqlQuery<string>($"""
            SELECT (to_jsonb(a)-ARRAY['R2TerminalIntentCode','R2TerminalIntentDisposition','R2TerminalIntentAtUtc'])::text AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts a WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync();

    private static void AssertIntentDenied(string outcome,TerminalIntentResult result)
    {
        Assert.Equal(outcome,result.Outcome);
        Assert.Null(result.TerminalIntentCode);
        Assert.Null(result.TerminalIntentDisposition);
        Assert.Null(result.TerminalIntentAtUtc);
    }

    private static async Task AssertFinalIntent(TagEkycDbContext db,RawIngressBrokerHandoff h,TerminalIntentResult intent)=>
        Assert.True(await db.Database.SqlQuery<bool>($"""
            SELECT "R2TerminalIntentCode"={intent.TerminalIntentCode} AND "R2TerminalIntentDisposition"={intent.TerminalIntentDisposition}
             AND "R2TerminalIntentAtUtc"={intent.TerminalIntentAtUtc} AND "R2TerminalOutcomeCode"={intent.TerminalIntentCode}
             AND "R2TerminationDisposition"={intent.TerminalIntentDisposition} AND "R2TerminatedAtUtc">="R2TerminalIntentAtUtc" AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={h.AttemptId}
            """).SingleAsync());

    private static Task<TerminalIntentResult> RecordIntent(TagEkycDbContext db,RawIngressBrokerHandoff h,string disposition,string code)=>
        db.Database.SqlQuery<TerminalIntentResult>($"""
            SELECT * FROM tagekyc.raw_export_record_retained_r2_terminal_intent(
             {h.SourceArtifactId},{h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence},{disposition},{code})
            """).SingleAsync();

    private static Task<TerminalFinalResult> FinalizeIntent(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        db.Database.SqlQuery<TerminalFinalResult>($"""
            SELECT * FROM tagekyc.raw_export_finalize_retained_r2_terminal(
             {h.SourceArtifactId},{h.AttemptId},{h.ExpectedEncryptionAttemptRevision},{h.ExpectedFence})
            """).SingleAsync();

    private sealed class TerminalIntentResult
    {
        public string Outcome {get;set;}="";
        public string? TerminalIntentCode {get;set;}
        public string? TerminalIntentDisposition {get;set;}
        public DateTimeOffset? TerminalIntentAtUtc {get;set;}
    }

    private sealed class TerminalFinalResult
    {
        public string Outcome {get;set;}="";
        public string? TerminalOutcomeCode {get;set;}
    }

    private static void AssertReservationBusy(RawIngressBrokerResult result) =>
        Assert.Equal(new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_RESERVATION_BUSY")),result);

    private static Task<DateTimeOffset> EvaluationExpiry(TagEkycDbContext db,ReentryContext setup)=>
        db.Database.SqlQuery<DateTimeOffset>($"""
            SELECT "CurrentTokenExpiresAtUtc" AS "Value" FROM tagekyc.raw_export_source_ingress_claim_aliases
            WHERE "ClientApplicationId"={setup.Handoff.ClientApplicationId}
             AND "ProducerId"={setup.Request.CaptureAgentId.ToString("N")}
             AND "CaptureAgentInstanceId"={setup.Request.DeviceInstallationId.ToString("N")}
             AND "IngressIdempotencyKey"={setup.Request.IngressIdempotencyKey}
             AND "CurrentClaimEvaluationDisposition"='Active'
            """).SingleAsync();

    private static async Task AssertEvaluationPending(TagEkycDbContext db,ReentryContext setup,RawIngressBrokerResult result)
    {
        var expires=await EvaluationExpiry(db,setup);
        Assert.Equal(new RawIngressBrokerResult.Final(new("RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS",
            RetryNotBeforeUtc:expires)),result);
    }

    private static async Task PastEvaluation(TagEkycDbContext db,ReentryContext setup)
    {
        var expires=await EvaluationExpiry(db,setup);
        var now=await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var remaining=Math.Max(0,(expires-now).TotalSeconds);
        Assert.InRange(remaining,0,10.1);
        await Task.Delay(TimeSpan.FromSeconds(remaining+0.1));
        Assert.True(await db.Database.SqlQuery<bool>($"SELECT clock_timestamp()>{expires} AS \"Value\"").SingleAsync());
    }

    private static Task<string> ReservationWithoutLease(TagEkycDbContext db,RawIngressBrokerHandoff h)=>
        db.Database.SqlQuery<string>($"""
            SELECT (to_jsonb(r)-'ReservationExpiresAtUtc')::text AS "Value"
            FROM tagekyc.raw_export_source_reservations r WHERE "SourceArtifactId"={h.SourceArtifactId}
            """).SingleAsync();

    private static async Task AssertReentryProjection(TagEkycDbContext db,RawIngressBrokerHandoff old,RawIngressBrokerHandoff next)
    {
        Assert.Equal(2,await db.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_encryption_attempts").SingleAsync());
        Assert.Equal(1,await db.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_source_reservations").SingleAsync());
        Assert.True(await db.Database.SqlQuery<bool>($"""
            SELECT a."AttemptId"={next.AttemptId} AND a."AttemptKeyReservationId"={next.AttemptKeyReservationId}
             AND a."Fence"={next.ExpectedFence} AND a."EncryptionAttemptRevision"={next.ExpectedEncryptionAttemptRevision}
             AND a."ProvisionalObjectIdentity"<>pre."ProvisionalObjectIdentity"
             AND a."AttemptId"<>a."ProvisionalObjectIdentity" AND a."AttemptId"<>a."AttemptKeyReservationId"
             AND a."ProvisionalObjectIdentity"<>a."AttemptKeyReservationId"
             AND pre."R2TerminationDisposition"='TerminatedBeforeStart' AND pre."R2TerminatedAtUtc" IS NOT NULL
             AND a."R2TerminationDisposition" IS NULL AND a."R2TerminatedAtUtc" IS NULL AND a."R2TerminalOutcomeCode" IS NULL
             AND a."R2TerminalIntentCode" IS NULL AND a."R2TerminalIntentDisposition" IS NULL AND a."R2TerminalIntentAtUtc" IS NULL
             AND a."StagedCiphertextFingerprintSchemaVersion" IS NULL AND a."StagedCiphertextFingerprint" IS NULL
             AND a."StagedObjectCustodyId" IS NULL AND a."StagedObjectStateRevision" IS NULL AND a."StagedFromReservationRevision" IS NULL
             AND a."VerifiedPlaintextLength" IS NULL AND a."StagedCiphertextLength" IS NULL AND a."StagedCiphertextDigest" IS NULL
             AND a."StagedProviderReceiptDigest" IS NULL AND a."StagedVerificationEvidenceDigest" IS NULL AND a."StagedAtUtc" IS NULL
             AND a."NonceDerivationSeedReferenceOrWrappedSeed"='none'
             AND ROW(a."KeyProviderId",a."KekId",a."KekVersion",a."KekFingerprint",a."EncryptionSuiteId",
                a."EncryptionFramingVersion",a."NonceStrategyId",a."NonceDerivationSeedCommitment",a."ChunkSize",a."FramingParametersDigest")
              IS NOT DISTINCT FROM ROW(pre."KeyProviderId",pre."KekId",pre."KekVersion",pre."KekFingerprint",pre."EncryptionSuiteId",
                pre."EncryptionFramingVersion",pre."NonceStrategyId",pre."NonceDerivationSeedCommitment",pre."ChunkSize",pre."FramingParametersDigest")
             AND a."OwnershipLeaseExpiresAtUtc"=r."ReservationExpiresAtUtc" AND r."ReservationExpiresAtUtc">clock_timestamp()
             AND r."ReservationExpiresAtUtc">pre."OwnershipLeaseExpiresAtUtc"
             AND r."ReservationExpiresAtUtc"=LEAST(a."CreatedAtUtc"+interval '30 seconds',r."EffectivePlaintextRetentionExpiresAtUtc",
                r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc")
             AND h."CurrentEncryptionAttemptId"=a."AttemptId" AND h."Fence"=a."Fence" AND h."ReservationRevision"=2
             AND a."EncryptionAttemptFingerprint"=tagekyc.raw_export_c1_hash_canonical(
                'tip-88c1-encryption-attempt-v1',encode(r."SourceReservationFingerprint",'hex'),a."EncryptionAttemptRevision"::text,a."Fence"::text,
                replace(a."ProvisionalObjectIdentity"::text,'-',''),replace(a."AttemptKeyReservationId"::text,'-',''),
                pre."EncryptionSuiteId",pre."EncryptionFramingVersion"::text,pre."KeyProviderId",pre."KekId",pre."KekVersion"::text,pre."KekFingerprint",
                pre."NonceStrategyId",encode(pre."NonceDerivationSeedCommitment",'hex'),pre."ChunkSize"::text,encode(pre."FramingParametersDigest",'hex')) AS "Value"
            FROM tagekyc.raw_export_source_encryption_attempts a
            JOIN tagekyc.raw_export_source_encryption_attempts pre ON pre."AttemptId"={old.AttemptId}
            JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
            JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
            JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
            JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
            WHERE a."AttemptId"={next.AttemptId}
            """).SingleAsync());
    }

    private sealed record ReentryContext(Tip88C1C6BA3RetentionCheckpointTests.Scope Scope,RawIngressBrokerHandoff Handoff,
        CaptureRuntimeRawIngressAdmissionContext Request,string Login);
    private static async Task<ReentryContext> ReentrySetup(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await Prepare(isolated);
        var scope=await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated,rawIngress:true);
        await using var db=isolated.CreateDbContext();
        var login=await Tip88C1C6BA3SyntheticComposition.BrokerLogin(db.Database.GetConnectionString()!);
        var now=await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var request=new CaptureRuntimeRawIngressAdmissionContext(
            Guid.Parse("40000000-0000-4000-8000-000000000001"),Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"),1,Guid.Parse("10000000-0000-4000-8000-000000000001"),1,
            now,new byte[32],new byte[32],1,scope.Session,scope.Artifact,1,"LiveSelfieImage",Guid.NewGuid(),"image/jpeg",24,
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant(),
            now.AddSeconds(-5),now.AddSeconds(-4),now.AddMinutes(5),300);
        var initial=new ReentryContext(scope,null!,request,login);
        return initial with {Handoff=Assert.IsType<RawIngressBrokerResult.Handoff>(await RetryAdmission(initial)).Value};
    }
    private static async Task<RawIngressBrokerResult> RetryAdmission(ReentryContext setup)
    {
        await using var source=NpgsqlDataSource.Create(setup.Login);
        await using var services=Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker=Tip88C1C6BA3SyntheticComposition.Broker(source,services.GetRequiredService<IContentCommitmentService>(),
            services.GetRequiredService<ISubjectRefTokenService>());
        return await broker.AdmitAsync(setup.Request,CancellationToken.None);
    }
    private static Task<ReentryResult> Reenter(TagEkycDbContext db,RawIngressBrokerHandoff h,Guid binding)=>
        db.Database.SqlQuery<ReentryResult>($"""
            SELECT * FROM tagekyc.raw_export_reenter_retained_source({h.SourceArtifactId},{h.AttemptId},
             {h.ExpectedEncryptionAttemptRevision},1::bigint,{h.ExpectedFence},{binding},60,2000,30)
            """).SingleAsync();
    private sealed class ReentryResult
    {
        public string OutcomeCode {get;set;}="";
        public Guid? SourceArtifactId {get;set;}
        public Guid? AttemptKeyReservationId {get;set;}
        public Guid? AttemptId {get;set;}
        public long? ExpectedEncryptionAttemptRevision {get;set;}
        public long? ExpectedFence {get;set;}
    }

    private sealed class NpsResult
    {
        public string Outcome {get;set;}="";
        public string? R2TerminationDisposition {get;set;}
        public DateTimeOffset? R2TerminatedAtUtc {get;set;}
    }
    private sealed class UnknownProvider : IKekOperationProvider
    {
        internal int Calls;
        public Task<KekWrapResult> WrapDekAsync(KekReference reference,ProviderOperationToken token,
            ReadOnlyMemory<byte> fingerprint,IAttemptDekCandidate candidate,CancellationToken cancellationToken)
        { Calls++; return Task.FromResult<KekWrapResult>(new KekWrapResult.OutcomeUnknown()); }
        public Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken token,ReadOnlyMemory<byte> fingerprint,CancellationToken cancellationToken)
            =>throw new InvalidOperationException("NPS must not inspect provider.");
        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference,KekWrappedMaterial wrapped,ReadOnlyMemory<byte> fingerprint,CancellationToken cancellationToken)
            =>throw new InvalidOperationException("NPS must not unwrap.");
    }

    private sealed class LostWrapResponseProvider(IKekOperationProvider inner) : IKekOperationProvider
    {
        internal int WrapCalls;

        public async Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken token,
            ReadOnlyMemory<byte> fingerprint, IAttemptDekCandidate candidate, CancellationToken cancellationToken)
        {
            WrapCalls++;
            var result = await inner.WrapDekAsync(reference, token, fingerprint, candidate, cancellationToken);
            return result is KekWrapResult.Wrapped
                ? new KekWrapResult.OutcomeUnknown()
                : result;
        }

        public Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken token,
            ReadOnlyMemory<byte> fingerprint, CancellationToken cancellationToken) =>
            inner.LookupByOperationTokenAsync(token, fingerprint, cancellationToken);

        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> fingerprint, CancellationToken cancellationToken) =>
            inner.UnwrapDekAsync(reference, wrapped, fingerprint, cancellationToken);
    }

    [Fact]
    public async Task Re01_SettledSameOwnerSuccessorRunsExistingR2R5WithoutOldAttemptRewrite()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_reentry_successor_r2r5");
        var setup = await ReentrySetup(isolated);
        await using var observer = isolated.CreateDbContext();
        var originalHorizon = await OriginalHorizon(observer, setup.Handoff);
        await PastLease(observer, setup.Handoff);
        var successor = Assert.IsType<RawIngressBrokerResult.Handoff>(await RetryAdmission(setup)).Value;
        await AssertReentryProjection(observer, setup.Handoff, successor);
        var oldAttempt = await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE a."AttemptId"={setup.Handoff.AttemptId}
            """).SingleAsync();
        Assert.Equal(0, await observer.RawExportAttemptKeyReservations.AsNoTracking()
            .CountAsync(row => row.AttemptKeyReservationId == setup.Handoff.AttemptKeyReservationId));

        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var logins = await Tip88C1C6BA3SyntheticComposition.CustodyLogins(
            observer.Database.GetConnectionString()!);
        await using var owners = Tip88C1C6BA3ContinuationWorkerTests.Owners(logins.Connections, minio);
        await using var journalWrite = isolated.CreateDbContext();
        await using var journalRead = isolated.CreateDbContext();
        var keys = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(journalWrite), new PostgresFixtureKekJournal(journalRead));
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var commitments = preflight.GetRequiredService<IContentCommitmentService>();
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var keyOptions = DurableKeyCustodyOptions.Resolve(new ConfigurationManager());
        var writer = new CaptureRuntimeRawIngressBodyPipeline(owners, keys, keys,
            commitments, keyOptions, options, CancellationToken.None);
        await using var plaintext = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);
        var result = await writer.ProcessAsync(setup.Request, successor, plaintext, CancellationToken.None);
        Assert.Contains(result.Outcome, new[] {
            CaptureRuntimeRawIngressOutcome.Available, CaptureRuntimeRawIngressOutcome.ResumePending });
        if (result.Outcome == CaptureRuntimeRawIngressOutcome.ResumePending)
        {
            var continuation = new CaptureRuntimeSourcePipeline(owners, keys, null, keys,
                commitments, keyOptions);
            for (var step = 0; step < 16; step++)
            {
                var progress = await continuation.AdvanceAsync(successor.SourceArtifactId,
                    options.RequestTimeoutMilliseconds, CancellationToken.None);
                if (progress.Snapshot?.CustodyState == "Available") break;
                Assert.NotEqual(RetainedContinuationStep.None, progress.Step);
            }
        }

        var publication = await observer.RawExportSourcePublications.AsNoTracking().SingleAsync();
        Assert.Equal("Available", publication.PublicationState);
        Assert.Equal(successor.SourceArtifactId, publication.SourceArtifactId);
        Assert.Equal(successor.AttemptId, publication.AttemptId);
        Assert.Equal(originalHorizon, await OriginalHorizon(observer, setup.Handoff));
        Assert.Equal(oldAttempt, await observer.Database.SqlQuery<string>($"""
            SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE a."AttemptId"={setup.Handoff.AttemptId}
            """).SingleAsync());
        Assert.Equal(0, await observer.RawExportAttemptKeyReservations.AsNoTracking()
            .CountAsync(row => row.AttemptKeyReservationId == setup.Handoff.AttemptKeyReservationId));
        Assert.Equal(2, await observer.RawExportSourceEncryptionAttempts.AsNoTracking().CountAsync());
    }
}
