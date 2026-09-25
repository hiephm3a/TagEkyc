using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Infrastructure.RawExport;
#if A3_ACCEPTANCE
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
#endif
using static TagEkyc.IntegrationTests.Tip88C1C6BA3ConsentRetentionTests;

namespace TagEkyc.IntegrationTests;

// The trigger below only schedules the R5 transaction; all business transitions,
// HTTP mapping, broker admission, object work and response construction are product code.
[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3R2R6ClusterHttpTests(PostgresPersistenceFixture postgres)
{
#if A3_ACCEPTANCE
    [Theory]
    [InlineData(false, false, false, false, false, false, false, false)]
    [InlineData(true, false, false, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false, false, false)]
    [InlineData(false, true, true, false, false, false, false, false)]
    [InlineData(false, false, false, true, false, false, false, false)]
    [InlineData(false, false, false, false, true, false, false, false)]
    [InlineData(false, false, false, false, false, true, false, false)]
    [InlineData(false, false, false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, false, false, true)]
    public async Task R2R6HttpsPublicationReplayAndMismatchScenarios(bool contentMismatch, bool dropResponse, bool withObsolete,
        bool encryptionFailure, bool cancelAtCommitted, bool tamperCiphertext, bool reenterAfterLoss,
        bool agentFirstAtR5)
#else
    [Fact]
    public async Task AvailableHttpResponseWaitsForR5PublicationCommit()
#endif
    {
#if !A3_ACCEPTANCE
        const bool contentMismatch = false;
        const bool dropResponse = false;
        const bool withObsolete = false;
        const bool encryptionFailure = false;
        const bool cancelAtCommitted = false;
        const bool tamperCiphertext = false;
        const bool reenterAfterLoss = false;
        const bool agentFirstAtR5 = false;
#endif
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_bp11_r5_http_gate");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
#if A3_ACCEPTANCE
        if (withObsolete) await InstallR5EntryGate(observer.Database.GetConnectionString()!);
#endif
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_pause_available_publish() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
              IF NEW."PublicationState" = 'Available' THEN
                PERFORM pg_catalog.pg_advisory_xact_lock(71496231);
              END IF;
              RETURN NEW;
            END $fn$;
            CREATE TRIGGER a3_test_pause_available_publish
              AFTER UPDATE OF "PublicationState" ON tagekyc.raw_export_source_publications
              FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_pause_available_publish();
            """);
#if A3_ACCEPTANCE
        if (tamperCiphertext)
            await observer.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION tagekyc.a3_test_pause_r2_object_record() RETURNS trigger
                LANGUAGE plpgsql AS $fn$
                BEGIN
                  IF NEW."State" = 'ObjectPresentPendingVerification' THEN
                    PERFORM pg_catalog.pg_advisory_xact_lock(71496233);
                  END IF;
                  RETURN NEW;
                END $fn$;
                CREATE TRIGGER a3_test_pause_r2_object_record
                  AFTER UPDATE OF "State" ON tagekyc.raw_export_provisional_objects
                  FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_pause_r2_object_record();
                """);
#endif

        await using var gate = new NpgsqlConnection(observer.Database.GetConnectionString());
        await gate.OpenAsync();
        await using (var acquire = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock(71496231)", gate))
            await acquire.ExecuteNonQueryAsync();
#if A3_ACCEPTANCE
        await using var objectRecordGate = tamperCiphertext
            ? new NpgsqlConnection(observer.Database.GetConnectionString()) : null;
        if (objectRecordGate is not null)
        {
            await objectRecordGate.OpenAsync();
            await using var acquireObject = new NpgsqlCommand(
                "SELECT pg_catalog.pg_advisory_lock(71496233)", objectRecordGate);
            await acquireObject.ExecuteNonQueryAsync();
        }
        await using var entryGate = withObsolete
            ? new NpgsqlConnection(observer.Database.GetConnectionString()) : null;
        if (entryGate is not null)
        {
            await entryGate.OpenAsync();
            await using var acquireEntry = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock(71496232)", entryGate);
            await acquireEntry.ExecuteNonQueryAsync();
        }
#endif

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
        var failingEncryption = new FailingUnwrapKekProvider(keys);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var brokerOptions = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var bodyPipeline = new CaptureRuntimeRawIngressBodyPipeline(owners,
            encryptionFailure ? failingEncryption : keys, keys,
            preflight.GetRequiredService<IContentCommitmentService>(),
            DurableKeyCustodyOptions.Resolve(new ConfigurationManager()), brokerOptions, CancellationToken.None);
        var firstHandoffGate = new FirstHandoffGate(bodyPipeline);
        var capacity = new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            capacity, broker, reenterAfterLoss ? firstHandoffGate : bodyPipeline,
            1_048_576);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
#if A3_ACCEPTANCE
        R2R6LoopbackHttpsHarness.Configure(builder);
#else
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
#endif
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        var replayServerReads = 0;
        var replayRequestsObserved = 0;
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.Expect.ToString().Contains("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref replayRequestsObserved);
                context.Request.Body = new CountingServerRequestStream(context.Request.Body,
                    () => Interlocked.Increment(ref replayServerReads));
            }
            await next();
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();

        var now = DateTimeOffset.UtcNow;
        var idempotencyKey = Guid.NewGuid().ToString("N");
#if A3_ACCEPTANCE
        using var agentKeys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        var agentPlaintext = "synthetic-retainedsource"u8.ToArray();
        using var agentFixture = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            new Uri(address), agentKeys, scope.Session, scope.Artifact,
            Guid.Parse(idempotencyKey), RawExportRawClass.LiveSelfieImage,
            agentPlaintext);
        var capturedAt = agentFixture.Metadata.CapturedAtUtc;
        var retentionStartedAt = agentFixture.Metadata.PlaintextRetentionStartedAtUtc;
        var retentionExpiresAt = agentFixture.Metadata.PlaintextRetentionExpiresAtUtc;
        now = DateTimeOffset.UtcNow;
#else
        var capturedAt = now.AddSeconds(-5);
        var retentionStartedAt = now.AddSeconds(-4);
        var retentionExpiresAt = now.AddMinutes(5);
#endif
#if A3_ACCEPTANCE
        if (reenterAfterLoss)
        {
            // The first real Agent request reaches durable R1 and then loses
            // its request before the production R2 pipeline starts.
            using var firstAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            using var abort = new CancellationTokenSource();
            var first = firstAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease, abort.Token);
            var firstHandoff = await firstHandoffGate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(first.IsCompleted);
            Assert.Equal(0, Volatile.Read(ref replayServerReads));
            Assert.Equal(0, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(0, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            abort.Cancel();
            var lost = await Record.ExceptionAsync(() => first.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.True(lost is OperationCanceledException or HttpRequestException,
                $"Expected first-request loss, got {lost?.GetType().Name ?? "none"}");
            var oldAttempt = await observer.Database.SqlQuery<string>($"""
                SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
                WHERE a."AttemptId"={firstHandoff.AttemptId}
                """).SingleAsync();
            var originalHorizon = await observer.Database.SqlQuery<DateTimeOffset>($"""
                SELECT LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",
                  s."ValidUntilUtc",b."ExecutionExpiresAtUtc") AS "Value"
                FROM tagekyc.raw_export_source_reservations r
                JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
                JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
                WHERE r."SourceArtifactId"={firstHandoff.SourceArtifactId}
                """).SingleAsync();
            var lease = await observer.Database.SqlQuery<DateTimeOffset>($"""
                SELECT "OwnershipLeaseExpiresAtUtc" AS "Value"
                FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"={firstHandoff.AttemptId}
                """).SingleAsync();
            var databaseNow = await observer.Database.SqlQueryRaw<DateTimeOffset>(
                "SELECT clock_timestamp() AS \"Value\"").SingleAsync();
            var delay = lease - databaseNow;
            Assert.InRange(delay.TotalSeconds, 0, 45);
            if (delay > TimeSpan.Zero) await Task.Delay(delay + TimeSpan.FromMilliseconds(100));
            await using (var unlock = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate))
                await unlock.ExecuteNonQueryAsync();
            using var secondAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var available = await secondAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(40));
            Assert.Equal(RawExportSourceIngressCodes.Available, available.OutcomeCode);
            Assert.Equal(firstHandoff.SourceArtifactId.ToString("D"), available.SourceArtifactId);
            Assert.Equal(2, Volatile.Read(ref firstHandoffGate.Calls));
            Assert.True(Volatile.Read(ref replayServerReads) > 0);
            var publication = await observer.RawExportSourcePublications.AsNoTracking().SingleAsync();
            Assert.Equal("Available", publication.PublicationState);
            Assert.Equal(firstHandoff.SourceArtifactId, publication.SourceArtifactId);
            Assert.NotEqual(firstHandoff.AttemptId, publication.AttemptId);
            Assert.Equal(2, await observer.RawExportSourceEncryptionAttempts.AsNoTracking().CountAsync());
            Assert.Equal(1, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(1, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            Assert.Equal(originalHorizon, await observer.Database.SqlQuery<DateTimeOffset>($"""
                SELECT LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",
                  s."ValidUntilUtc",b."ExecutionExpiresAtUtc") AS "Value"
                FROM tagekyc.raw_export_source_reservations r
                JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
                JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
                WHERE r."SourceArtifactId"={firstHandoff.SourceArtifactId}
                """).SingleAsync());
            Assert.Equal("TerminatedBeforeStart", (await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
                .SingleAsync(row => row.AttemptId == firstHandoff.AttemptId)).R2TerminationDisposition);
            Assert.DoesNotContain(firstHandoff.AttemptId, await observer.RawExportAttemptKeyReservations
                .AsNoTracking().Select(row => row.AttemptId).ToArrayAsync());
            Assert.NotEqual(oldAttempt, await observer.Database.SqlQuery<string>($"""
                SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
                WHERE a."AttemptId"={firstHandoff.AttemptId}
                """).SingleAsync());
            return;
        }
        if (tamperCiphertext)
        {
            Assert.NotNull(objectRecordGate);
            using var wire = new CapturingBusinessResponseHandler(R2R6LoopbackHttpsHarness.CreateHandler());
            using var tamperAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: wire);
            var send = tamperAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease);
            try
            {
                var waiting = false;
                for (var poll = 0; poll < 400; poll++)
                {
                    waiting = await observer.Database.SqlQueryRaw<bool>("""
                        SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                          WHERE locktype='advisory' AND granted=false AND objid::integer=71496233) AS "Value"
                        """).SingleAsync();
                    if (waiting) break;
                    await Task.Delay(25);
                }
                Assert.True(waiting);
                Assert.False(send.IsCompleted);
                var exact = await observer.RawExportProvisionalObjects.AsNoTracking().SingleAsync();
                Assert.Equal("PutInFlight", exact.State);
                await minio.PutRootObjectAsync(minio.BucketName, exact.ObjectKey,
                    "not-a-valid-framed-ciphertext"u8.ToArray());
            }
            finally
            {
                await using var releaseObject = new NpgsqlCommand(
                    "SELECT pg_catalog.pg_advisory_unlock(71496233)", objectRecordGate);
                await releaseObject.ExecuteNonQueryAsync();
                await using var releasePublish = new NpgsqlCommand(
                    "SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
                await releasePublish.ExecuteNonQueryAsync();
            }
            var terminal = await send.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.RecaptureRequired, terminal.OutcomeCode);
            Assert.Equal(HttpStatusCode.Conflict, wire.Status);
            Assert.NotNull(wire.Body);
            using (var capturedBusiness = JsonDocument.Parse(wire.Body))
            {
                Assert.Equal(new[] { "outcomeCode" }, capturedBusiness.RootElement.EnumerateObject()
                    .Select(field => field.Name).ToArray());
                Assert.Equal(RawExportSourceIngressCodes.RecaptureRequired,
                    capturedBusiness.RootElement.GetProperty("outcomeCode").GetString());
            }
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking().CountAsync());
            var failedAttempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
            Assert.Equal(RawExportSourceIngressCodes.RecaptureRequired, failedAttempt.R2TerminalOutcomeCode);
            Assert.Equal("Deleted", (await observer.RawExportProvisionalObjects.AsNoTracking().SingleAsync()).State);
            Assert.Equal("Revoked", (await observer.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync())
                .PreparationDisposition);
            var readsAfterTerminal = Volatile.Read(ref replayServerReads);
            Assert.True(readsAfterTerminal > 0);
            var keyCount = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
            var objectCount = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
            using var retryAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var replay = await retryAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.RecaptureRequired, replay.OutcomeCode);
            Assert.Equal(readsAfterTerminal, Volatile.Read(ref replayServerReads));
            Assert.Equal(keyCount, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectCount, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            return;
        }
        if (cancelAtCommitted)
        {
            // Cancel a real in-flight Agent HTTP request only after R4 has
            // durably committed and R5 is waiting on the test scheduling lock.
            // The response is absent; no business result is synthesized.
            using var firstAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            using var abort = new CancellationTokenSource();
            var first = firstAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease, abort.Token);
            try
            {
                var committed = false;
                for (var poll = 0; poll < 400; poll++)
                {
                    committed = await observer.RawExportSourcePublications.AsNoTracking()
                        .AnyAsync(row => row.PublicationState == "Committed");
                    if (committed) break;
                    await Task.Delay(25);
                }
                Assert.True(committed);
                Assert.False(first.IsCompleted);
                abort.Cancel();
                var lost = await Record.ExceptionAsync(() => first.WaitAsync(TimeSpan.FromSeconds(30)));
                Assert.True(lost is OperationCanceledException or HttpRequestException,
                    $"Expected transport cancellation, got {lost?.GetType().Name ?? "none"}");
            }
            finally
            {
                await using var unlock = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
                await unlock.ExecuteNonQueryAsync();
            }
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking()
                .CountAsync(row => row.PublicationState == "Available"));
            var readsAfterLoss = Volatile.Read(ref replayServerReads);
            Assert.True(readsAfterLoss > 0);
            var keysAfterLoss = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
            var objectsAfterLoss = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
            using var retryAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var pending = await retryAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.ResumePending, pending.OutcomeCode);
            Assert.Equal(readsAfterLoss, Volatile.Read(ref replayServerReads));
            Assert.Equal(keysAfterLoss, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectsAfterLoss, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            var source = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
                .Select(row => row.SourceArtifactId).SingleAsync();
            var continuation = new CaptureRuntimeSourcePipeline(owners, keys, null, keys,
                preflight.GetRequiredService<IContentCommitmentService>(),
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
            for (var step = 0; step < 16; step++)
            {
                var progress = await continuation.AdvanceAsync(source,
                    brokerOptions.RequestTimeoutMilliseconds, CancellationToken.None);
                if (progress.Snapshot?.CustodyState == "Available") break;
                Assert.NotEqual(RetainedContinuationStep.None, progress.Step);
            }
            Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
                .CountAsync(row => row.PublicationState == "Available"));
            using var replayAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var replay = await replayAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            if (replay.OutcomeCode == RawExportSourceIngressCodes.EvaluationInProgress)
            {
                Assert.NotNull(replay.RetryNotBeforeUtc);
                Assert.Null(replay.SourceArtifactId);
                Assert.Equal(readsAfterLoss, Volatile.Read(ref replayServerReads));
                var databaseNow = await observer.Database.SqlQueryRaw<DateTimeOffset>(
                    "SELECT clock_timestamp() AS \"Value\"").SingleAsync();
                var delay = replay.RetryNotBeforeUtc.Value - databaseNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay + TimeSpan.FromMilliseconds(100));
                replay = await replayAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                    .WaitAsync(TimeSpan.FromSeconds(30));
            }
            Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable, replay.OutcomeCode);
            Assert.Equal(source.ToString("D"), replay.SourceArtifactId);
            Assert.Equal(readsAfterLoss, Volatile.Read(ref replayServerReads));
            Assert.Equal(keysAfterLoss, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectsAfterLoss, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking().CountAsync());
            return;
        }
        if (encryptionFailure)
        {
            // The premise fault is only the KEK unwrap used by the production
            // bounded AEAD operation. Broker, R2 writer, object PUT, mapper,
            // HTTPS transport and the Agent client remain production code.
            using var wire = new CapturingBusinessResponseHandler(R2R6LoopbackHttpsHarness.CreateHandler());
            using var failureAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: wire);
            var result = await failureAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.TemporarilyUnavailable, result.OutcomeCode);
            Assert.Null(result.SourceArtifactId);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, wire.Status);
            Assert.NotNull(wire.Body);
            Assert.DoesNotContain("SOURCE_ENCRYPTION_FAILED", wire.Body, StringComparison.Ordinal);
            using (var capturedBusiness = JsonDocument.Parse(wire.Body))
            {
                Assert.Equal(new[] { "outcomeCode" }, capturedBusiness.RootElement.EnumerateObject()
                    .Select(field => field.Name).ToArray());
                Assert.Equal(RawExportSourceIngressCodes.TemporarilyUnavailable,
                    capturedBusiness.RootElement.GetProperty("outcomeCode").GetString());
            }
            Assert.True(failingEncryption.UnwrapCalls > 0);
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking().CountAsync());
            var attempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
            Assert.Null(attempt.R2TerminalIntentCode);
            Assert.Null(attempt.R2TerminalOutcomeCode);
            var objectRow = await observer.RawExportProvisionalObjects.AsNoTracking().SingleAsync();
            Assert.Equal("PutOutcomeUnknown", objectRow.State);
            var attemptBeforeReconciliation = await observer.Database.SqlQuery<string>($"""
                SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
                WHERE a."AttemptId"={attempt.AttemptId}
                """).SingleAsync();
            var continuation = new CaptureRuntimeSourcePipeline(owners, keys, null, keys,
                preflight.GetRequiredService<IContentCommitmentService>(),
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
            var reconciled = await continuation.AdvanceAsync(attempt.SourceArtifactId,
                brokerOptions.RequestTimeoutMilliseconds, CancellationToken.None);
            Assert.Equal(RetainedContinuationStep.ReconcileIncompleteObject, reconciled.Step);
            Assert.Equal("NoObjectEstablished", reconciled.Snapshot?.ObjectState);
            Assert.Null(reconciled.Snapshot?.R2TerminalIntentCode);
            Assert.Null(reconciled.Snapshot?.R2TerminalOutcomeCode);
            var absentObject = await observer.RawExportProvisionalObjects.AsNoTracking().SingleAsync();
            Assert.Equal("PositiveAbsence", absentObject.PutOutcomeKind);
            Assert.NotNull(absentObject.OutcomeObservedAtUtc);
            Assert.Equal(attemptBeforeReconciliation, await observer.Database.SqlQuery<string>($"""
                SELECT to_jsonb(a)::text AS "Value" FROM tagekyc.raw_export_source_encryption_attempts a
                WHERE a."AttemptId"={attempt.AttemptId}
                """).SingleAsync());
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking().CountAsync());
            var readsBeforeRetry = Volatile.Read(ref replayServerReads);
            var keysBeforeRetry = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
            var objectsBeforeRetry = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
            using var retryAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var retry = await retryAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            if (retry.OutcomeCode == RawExportSourceIngressCodes.EvaluationInProgress)
            {
                Assert.NotNull(retry.RetryNotBeforeUtc);
                var databaseNow = await observer.Database.SqlQueryRaw<DateTimeOffset>(
                    "SELECT clock_timestamp() AS \"Value\"").SingleAsync();
                var delay = retry.RetryNotBeforeUtc.Value - databaseNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay + TimeSpan.FromMilliseconds(100));
                retry = await retryAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                    .WaitAsync(TimeSpan.FromSeconds(30));
            }
            Assert.Equal(RawExportSourceIngressCodes.ReservationBusy, retry.OutcomeCode);
            Assert.Equal(readsBeforeRetry, Volatile.Read(ref replayServerReads));
            Assert.Equal(keysBeforeRetry, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectsBeforeRetry, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            Assert.Null((await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync())
                .R2TerminalOutcomeCode);
            await using var unlock = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
            await unlock.ExecuteNonQueryAsync();
            return;
        }
        if (contentMismatch)
        {
            var transitCorruption = new OneByteTransitCorruptionHandler(
                R2R6LoopbackHttpsHarness.CreateHandler());
            using var mismatchAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: transitCorruption);
            var mismatch = await mismatchAgent.SubmitRawExportSourceAsync(
                agentFixture.Metadata, agentFixture.Lease).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(1, transitCorruption.Calls);
            var terminalState = await observer.Database.SqlQueryRaw<string>("""
                SELECT jsonb_build_object(
                  'intent',a."R2TerminalIntentCode",
                  'outcome',a."R2TerminalOutcomeCode",
                  'termination',a."R2TerminationDisposition",
                  'object',o."State",
                  'key',k."PreparationDisposition")::text AS "Value"
                FROM tagekyc.raw_export_source_encryption_attempts a
                LEFT JOIN tagekyc.raw_export_provisional_objects o ON o."AttemptId"=a."AttemptId"
                LEFT JOIN tagekyc.raw_export_attempt_key_reservations k
                  ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId"
                """).ToArrayAsync();
            Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch,
                mismatch.OutcomeCode);
            Assert.Single(terminalState);
            using var pendingTerminal = JsonDocument.Parse(terminalState[0]);
            Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch,
                pendingTerminal.RootElement.GetProperty("intent").GetString());
            if (mismatch.OutcomeCode == RawExportSourceIngressCodes.TemporarilyUnavailable)
            {
                Assert.Equal(JsonValueKind.Null, pendingTerminal.RootElement.GetProperty("outcome").ValueKind);
                var terminalSource = await observer.Database.SqlQueryRaw<Guid>("""
                    SELECT "SourceArtifactId" AS "Value" FROM tagekyc.raw_export_source_encryption_attempts
                    """).SingleAsync();
                var continuation = new CaptureRuntimeSourcePipeline(owners, keys, null, keys,
                    preflight.GetRequiredService<IContentCommitmentService>(),
                    DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
                RetainedContinuationProgress? finalized = null;
                for (var step = 0; step < 32; step++)
                {
                    finalized = await continuation.AdvanceAsync(terminalSource,
                        brokerOptions.RequestTimeoutMilliseconds, CancellationToken.None);
                    if (finalized.Snapshot?.R2TerminalOutcomeCode ==
                        RawExportSourceIngressCodes.ContentCommitmentMismatch) break;
                    Assert.NotEqual(RetainedContinuationStep.None, finalized.Step);
                }
                Assert.NotNull(finalized);
                Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch,
                    finalized.Snapshot?.R2TerminalOutcomeCode);
            }
            else
            {
                Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch,
                    pendingTerminal.RootElement.GetProperty("outcome").GetString());
            }
            var finalAttempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
            Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch,
                finalAttempt.R2TerminalOutcomeCode);
            Assert.Equal("Terminated", finalAttempt.R2TerminationDisposition);
            var absent = await observer.RawExportProvisionalObjects.AsNoTracking().SingleAsync();
            Assert.Equal("NoObjectEstablished", absent.State);
            Assert.Equal("PositiveAbsence", absent.PutOutcomeKind);
            Assert.NotNull(absent.PutOperationId);
            Assert.NotNull(absent.PutArmedAtUtc);
            Assert.NotNull(absent.OutcomeObservedAtUtc);
            var key = await observer.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync();
            Assert.Equal("Revoked", key.PreparationDisposition);
            var readsBeforeReplay = Volatile.Read(ref replayServerReads);
            using var replayAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var terminalReplay = await replayAgent.SubmitRawExportSourceAsync(
                agentFixture.Metadata, agentFixture.Lease).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.ContentCommitmentMismatch, terminalReplay.OutcomeCode);
            Assert.Null(terminalReplay.SourceArtifactId);
            Assert.Equal(readsBeforeReplay, Volatile.Read(ref replayServerReads));
            Assert.Null(mismatch.SourceArtifactId);
            Assert.Null(mismatch.CurrentSourceState);
            Assert.Null(mismatch.CurrentDisposition);
            Assert.Equal(2, Volatile.Read(ref replayRequestsObserved));
            Assert.True(Volatile.Read(ref replayServerReads) > 0);
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking().CountAsync());
            Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("""
                SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH'
                  AND "R2TerminalOutcomeCode"='CONTENT_COMMITMENT_MISMATCH'
                """).SingleAsync());
            await using var unlock = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
            await unlock.ExecuteNonQueryAsync();
            return;
        }
        if (dropResponse)
        {
            // The server completes R5; only its already-produced HTTP response
            // is lost in transit. The second client observes durable state.
            Tip88C1C6BA3R2TerminalProjectionTests.ObsoleteResidue? obsolete = null;
            var entryReleased = false;
            using var lost = new LostResponseAfterCommitHandler(R2R6LoopbackHttpsHarness.CreateHandler());
            using (var firstAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: lost))
            {
                var first = firstAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease);
                try
                {
                    var pending = false;
                    for (var poll = 0; poll < 400; poll++)
                    {
                        pending = await observer.RawExportSourcePublications.AsNoTracking()
                            .AnyAsync(row => row.PublicationState == "Committed");
                        if (pending) break;
                        await Task.Delay(25);
                    }
                    Assert.True(pending);
                    if (withObsolete)
                    {
                        var winner = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
                        var obsoleteWriter = new S3CompatibleProvisionalObjectWriter(
                            minio.Options(ProvisionalObjectCapability.Writer));
                        obsolete = await Tip88C1C6BA3R2TerminalProjectionTests.AddObsoleteVerifiedAttemptAsync(
                            isolated, winner.SourceArtifactId, winner.AttemptId,
                            winner.AttemptKeyReservationId, obsoleteWriter);
                        Assert.NotNull(entryGate);
                        await using var releaseEntry = new NpgsqlCommand(
                            "SELECT pg_catalog.pg_advisory_unlock(71496232)", entryGate);
                        Assert.True((bool)(await releaseEntry.ExecuteScalarAsync())!);
                        entryReleased = true;
                    }
                    Assert.False(first.IsCompleted);
                }
                finally
                {
                    if (entryGate is not null && !entryReleased)
                    {
                        await using var releaseEntry = new NpgsqlCommand(
                            "SELECT pg_catalog.pg_advisory_unlock(71496232)", entryGate);
                        await releaseEntry.ExecuteScalarAsync();
                    }
                    await using var unlock = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
                    await unlock.ExecuteNonQueryAsync();
                }
                await Assert.ThrowsAsync<HttpRequestException>(() => first.WaitAsync(TimeSpan.FromSeconds(30)));
            }
            Assert.Equal(1, lost.Calls);
            Assert.Equal(HttpStatusCode.OK, lost.ObservedStatus);
            Assert.Equal(RawExportSourceIngressCodes.Available, lost.ObservedOutcomeCode);
            var publication = await observer.RawExportSourcePublications.AsNoTracking().SingleAsync();
            Assert.Equal("Available", publication.PublicationState);
            if (withObsolete)
            {
                Assert.NotNull(obsolete);
                Assert.Equal("Pending", publication.CleanupDisposition);
                Assert.Equal(2, await observer.RawExportSourceCleanupItems.AsNoTracking()
                    .CountAsync(row => row.SourcePublicationId == publication.SourcePublicationId));
                var continuation = new CaptureRuntimeSourcePipeline(owners, keys, null, keys,
                    preflight.GetRequiredService<IContentCommitmentService>(),
                    DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
                var worker = new CaptureRuntimeSourceContinuationWorker(continuation, brokerOptions,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<CaptureRuntimeSourceContinuationWorker>.Instance);
                using var stop = new CancellationTokenSource();
                var run = worker.RunAsync(stop.Token);
                try
                {
                    var completed = false;
                    for (var poll = 0; poll < 800; poll++)
                    {
                        completed = await observer.RawExportSourcePublications.AsNoTracking()
                            .AnyAsync(row => row.SourcePublicationId == publication.SourcePublicationId
                                && row.CleanupDisposition == "Completed");
                        if (completed) break;
                        await Task.Delay(25);
                    }
                    Assert.True(completed);
                }
                finally
                {
                    stop.Cancel();
                    await run;
                }
                Assert.Equal("Deleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
                    .SingleAsync(row => row.ObjectCustodyId == obsolete.ObjectCustodyId)).State);
                Assert.Equal("Revoked", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
                    .SingleAsync(row => row.AttemptKeyReservationId == obsolete.AttemptKeyReservationId))
                    .PreparationDisposition);
                var completedPublication = await observer.RawExportSourcePublications.AsNoTracking()
                    .SingleAsync(row => row.SourcePublicationId == publication.SourcePublicationId);
                Assert.Equal("Completed", completedPublication.CleanupDisposition);
                Assert.Equal(3, completedPublication.PublicationRevision);
                Assert.Equal(32, completedPublication.CleanupEvidenceDigest?.Length);
                Assert.Equal(2, await observer.RawExportSourceCleanupItems.AsNoTracking()
                    .CountAsync(row => row.SourcePublicationId == publication.SourcePublicationId
                        && row.CleanupState == "Completed"));
                Assert.Equal("Active", (await observer.RawExportAttemptKeyReservations.AsNoTracking()
                    .SingleAsync(row => row.AttemptKeyReservationId == publication.AttemptKeyReservationId))
                    .PreparationDisposition);
                Assert.Equal("VerifiedCompleted", (await observer.RawExportProvisionalObjects.AsNoTracking()
                    .SingleAsync(row => row.ObjectCustodyId == publication.ObjectCustodyId)).State);
                var obsoleteObject = await observer.RawExportProvisionalObjects.AsNoTracking()
                    .SingleAsync(row => row.ObjectCustodyId == obsolete.ObjectCustodyId);
                await using var reconciler = await owners.OpenReconcilerAsync(default);
                Assert.Equal(ExactObjectInspectionOutcome.PositivelyAbsent,
                    (await reconciler.Services.GetRequiredService<IProvisionalObjectReconciler>()
                        .InspectExactAsync(new(obsoleteObject.ProvisionalObjectIdentity,
                            obsoleteObject.ObjectKey, obsoleteObject.ObjectBindingDigest), default)).Outcome);
            }
            var keysAfterFirst = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
            var objectsAfterFirst = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
            var readsAfterFirst = Volatile.Read(ref replayServerReads);
            Assert.True(readsAfterFirst > 0);
            using var secondAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var replay = await secondAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable, replay.OutcomeCode);
            Assert.Equal(publication.SourceArtifactId.ToString("D"), replay.SourceArtifactId);
            Assert.Equal(readsAfterFirst, Volatile.Read(ref replayServerReads));
            Assert.Equal(keysAfterFirst, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectsAfterFirst, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            if (withObsolete)
            {
                Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
                    .CountAsync(row => row.SourceArtifactId == publication.SourceArtifactId));
                Assert.Equal(2, Volatile.Read(ref replayRequestsObserved));
            }
            return;
        }
        if (agentFirstAtR5)
        {
            using var firstAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var first = firstAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease);
            try
            {
                var committed = false;
                var waiter = false;
                for (var poll = 0; poll < 400; poll++)
                {
                    committed = await observer.RawExportSourcePublications.AsNoTracking()
                        .AnyAsync(row => row.PublicationState == "Committed");
                    waiter = await observer.Database.SqlQueryRaw<bool>("""
                        SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                          WHERE locktype='advisory' AND granted=false AND objid::integer=71496231) AS "Value"
                        """).SingleAsync();
                    if (committed && (waiter || first.IsCompleted)) break;
                    await Task.Delay(25);
                }
                Assert.True(committed);
                Assert.True(waiter);
                Assert.False(first.IsCompleted);
                Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking()
                    .CountAsync(row => row.PublicationState == "Available"));
                Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("""
                    SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_source_head
                    WHERE "CustodyState"='Available'
                    """).SingleAsync());
            }
            finally
            {
                await using var release = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
                await release.ExecuteNonQueryAsync();
            }
            var available = await first.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.Available, available.OutcomeCode);
            Assert.NotNull(available.SourceArtifactId);
            var agentSourceId = Guid.Parse(available.SourceArtifactId);
            Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
                .CountAsync(row => row.SourceArtifactId == agentSourceId && row.PublicationState == "Available"));
            var readsAfterFirst = Volatile.Read(ref replayServerReads);
            Assert.True(readsAfterFirst > 0);
            var keysAfterFirst = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
            var objectsAfterFirst = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
            using var replayAgent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
                agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
            var replay = await replayAgent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable, replay.OutcomeCode);
            Assert.Equal(agentSourceId.ToString("D"), replay.SourceArtifactId);
            Assert.Equal(readsAfterFirst, Volatile.Read(ref replayServerReads));
            Assert.Equal(keysAfterFirst, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
            Assert.Equal(objectsAfterFirst, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
            Assert.Equal(2, Volatile.Read(ref replayRequestsObserved));
            return;
        }
#endif
        await using var body = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);
#if A3_ACCEPTANCE
        using var client = R2R6LoopbackHttpsHarness.CreateClient();
#else
        using var client = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            Expect100ContinueTimeout = TimeSpan.FromSeconds(30)
        });
#endif
        using var request = new HttpRequestMessage(HttpMethod.Post,
            new Uri(new Uri(address), "/api/ekyc/raw-export/source-ingress"));
        request.Content = new StreamContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        request.Content.Headers.ContentLength = body.Length;
        void Header(string name, string value) => request.Headers.TryAddWithoutValidation(name, value);
        Header("X-TagEkyc-Agent-Configuration-Revision", "1");
        Header("X-TagEkyc-Verification-Session-Id", scope.Session.ToString("N"));
        Header("X-TagEkyc-Capture-Artifact-Id", scope.Artifact.ToString("N"));
        Header("X-TagEkyc-Capture-Revision", "1");
        Header("X-TagEkyc-Raw-Class", "LiveSelfieImage");
        Header("Idempotency-Key", idempotencyKey);
        Header("X-TagEkyc-Plaintext-Sha256",
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant());
        Header("X-TagEkyc-Captured-At-Utc", capturedAt.ToString("O", CultureInfo.InvariantCulture));
        Header("X-TagEkyc-Retention-Started-At-Utc", retentionStartedAt.ToString("O", CultureInfo.InvariantCulture));
        Header("X-TagEkyc-Retention-Expires-At-Utc", retentionExpiresAt.ToString("O", CultureInfo.InvariantCulture));
        Header("X-TagEkyc-Retention-Budget-Seconds", "300");
        Header(CaptureRuntimeCrt1RequestParser.CredentialIdHeader,
            AcceptedRuntimeAuthenticator.CredentialId.ToString("N"));
        Header(CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader, "1");
        Header(CaptureRuntimeCrt1RequestParser.TimestampHeader,
            now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
        Header(CaptureRuntimeCrt1RequestParser.NonceHeader,
            Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        Header(CaptureRuntimeCrt1RequestParser.SignatureHeader,
            Convert.ToBase64String(new byte[64]).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        var responseTask = client.SendAsync(request);

        try
        {
            var committed = false;
            var waiter = false;
            for (var poll = 0; poll < 400; poll++)
            {
                committed = await observer.RawExportSourcePublications.AsNoTracking()
                    .AnyAsync(row => row.PublicationState == "Committed");
                waiter = await observer.Database.SqlQueryRaw<bool>("""
                    SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                      WHERE locktype='advisory' AND granted=false AND objid::integer=71496231) AS "Value"
                    """).SingleAsync();
                if (committed && (waiter || responseTask.IsCompleted)) break;
                await Task.Delay(25);
            }
            Assert.True(committed);
            Assert.False(responseTask.IsCompleted);
            Assert.True(waiter);
            Assert.Equal(0, await observer.RawExportSourcePublications.AsNoTracking()
                .CountAsync(row => row.PublicationState == "Available"));
            Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("""
                SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_source_head
                WHERE "CustodyState"='Available'
                """).SingleAsync());
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496231)", gate);
            await release.ExecuteNonQueryAsync();
        }

        var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var responseBody = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(responseBody);
        Assert.Equal(RawExportSourceIngressCodes.Available,
            json.RootElement.GetProperty("outcomeCode").GetString());
        var sourceId = json.RootElement.GetProperty("sourceArtifactId").GetGuid();
        Assert.Equal(1, await observer.RawExportSourcePublications.AsNoTracking()
            .CountAsync(row => row.SourceArtifactId == sourceId && row.PublicationState == "Available"));

        var keysBeforeReplay = await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync();
        var objectsBeforeReplay = await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync();
#if A3_ACCEPTANCE
        using var agent = new CaptureRuntimeHttpClient(new Uri(address), agentFixture.Journal,
            agentKeys, agentFixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
        var agentReplay = await agent.SubmitRawExportSourceAsync(agentFixture.Metadata, agentFixture.Lease)
            .WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable, agentReplay.OutcomeCode);
        Assert.Equal(sourceId.ToString("D"), agentReplay.SourceArtifactId);
        Assert.Equal("Available", agentReplay.CurrentSourceState);
        Assert.Equal("Available", agentReplay.CurrentDisposition);
#else
        await using var replayBody = new MemoryStream("synthetic-retainedsource"u8.ToArray(), writable: false);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post,
            new Uri(new Uri(address), "/api/ekyc/raw-export/source-ingress"));
        replayRequest.Headers.ExpectContinue = true;
        replayRequest.Content = new StreamContent(replayBody);
        replayRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        replayRequest.Content.Headers.ContentLength = replayBody.Length;
        void ReplayHeader(string name, string value) => replayRequest.Headers.TryAddWithoutValidation(name, value);
        ReplayHeader("X-TagEkyc-Agent-Configuration-Revision", "1");
        ReplayHeader("X-TagEkyc-Verification-Session-Id", scope.Session.ToString("N"));
        ReplayHeader("X-TagEkyc-Capture-Artifact-Id", scope.Artifact.ToString("N"));
        ReplayHeader("X-TagEkyc-Capture-Revision", "1");
        ReplayHeader("X-TagEkyc-Raw-Class", "LiveSelfieImage");
        ReplayHeader("Idempotency-Key", idempotencyKey);
        ReplayHeader("X-TagEkyc-Plaintext-Sha256",
            Convert.ToHexString(SHA256.HashData("synthetic-retainedsource"u8)).ToLowerInvariant());
        ReplayHeader("X-TagEkyc-Captured-At-Utc", capturedAt.ToString("O", CultureInfo.InvariantCulture));
        ReplayHeader("X-TagEkyc-Retention-Started-At-Utc", retentionStartedAt.ToString("O", CultureInfo.InvariantCulture));
        ReplayHeader("X-TagEkyc-Retention-Expires-At-Utc", retentionExpiresAt.ToString("O", CultureInfo.InvariantCulture));
        ReplayHeader("X-TagEkyc-Retention-Budget-Seconds", "300");
        ReplayHeader(CaptureRuntimeCrt1RequestParser.CredentialIdHeader,
            AcceptedRuntimeAuthenticator.CredentialId.ToString("N"));
        ReplayHeader(CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader, "1");
        ReplayHeader(CaptureRuntimeCrt1RequestParser.TimestampHeader,
            now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
        ReplayHeader(CaptureRuntimeCrt1RequestParser.NonceHeader,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        ReplayHeader(CaptureRuntimeCrt1RequestParser.SignatureHeader,
            Convert.ToBase64String(new byte[64]).TrimEnd('=').Replace('+', '-').Replace('/', '_'));

        using var replay = await client.SendAsync(replayRequest).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        await using var replayResponseBody = await replay.Content.ReadAsStreamAsync();
        using var replayJson = await JsonDocument.ParseAsync(replayResponseBody);
        Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable,
            replayJson.RootElement.GetProperty("outcomeCode").GetString());
        Assert.Equal(sourceId, replayJson.RootElement.GetProperty("sourceArtifactId").GetGuid());
#endif
        Assert.Equal(1, Volatile.Read(ref replayRequestsObserved));
        Assert.Equal(0, Volatile.Read(ref replayServerReads));
        Assert.Equal(keysBeforeReplay, await observer.RawExportAttemptKeyReservations.AsNoTracking().CountAsync());
        Assert.Equal(objectsBeforeReplay, await observer.RawExportProvisionalObjects.AsNoTracking().CountAsync());
        using var released = capacity.TryAcquire(
            "40000000000040008000000000000001", body.Length);
        Assert.NotNull(released);
    }

#if A3_ACCEPTANCE
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IsolatedTlsFactAttribute : FactAttribute
    {
        public IsolatedTlsFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("A3_ISOLATED_TLS_RUNNER") != "1")
                Skip = "Requires an isolated CA/CRL runner and the production SslStream trust path.";
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IsolatedTlsTheoryAttribute : TheoryAttribute
    {
        public IsolatedTlsTheoryAttribute()
        {
            if (Environment.GetEnvironmentVariable("A3_ISOLATED_TLS_RUNNER") != "1")
                Skip = "Requires an isolated CA/CRL runner and the production SslStream trust path.";
        }
    }

    // A different PostgreSQL connection observes the real B/R1 commit while
    // the public Agent constructor uses StrictRawIngressTransport over TLS.
    [IsolatedTlsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpectFence_StrictTlsKestrelDurableR1PrecedesFirstBodyRead(bool loseFinalResponse)
    {
        var certificatePath = Environment.GetEnvironmentVariable("A3_TEST_TLS_IP_PFX")!;
        using var certificate = new X509Certificate2(certificatePath,
            Environment.GetEnvironmentVariable("A3_TEST_TLS_PFX_PASSWORD"), X509KeyStorageFlags.UserKeySet);
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_expect_strict_tls");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var observerConnection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
            { Pooling = false }.ConnectionString;
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_hold_strict_r1_commit() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
              PERFORM pg_catalog.pg_advisory_xact_lock(71496241);
              RETURN NEW;
            END $fn$;
            CREATE TRIGGER a3_test_hold_strict_r1_commit
              AFTER INSERT ON tagekyc.raw_export_source_reservations
              FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_hold_strict_r1_commit();
            """);
        await using var gate = new NpgsqlConnection(observerConnection);
        await gate.OpenAsync();
        await using (var acquire = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock(71496241)", gate))
            await acquire.ExecuteNonQueryAsync();

        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observerConnection);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var firstRead = new ExpectFenceFirstRead(observerConnection);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152), broker, firstRead, 1_048_576);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listen => listen.UseHttps(certificate)));
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        var rawPosts = 0;
        var bodyProbe = new ExpectFenceServerBodyProbe(observerConnection);
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "POST" &&
                context.Request.Path == "/api/ekyc/raw-export/source-ingress")
            {
                Interlocked.Increment(ref rawPosts);
                context.Request.Body = bodyProbe.Wrap(context.Request.Body);
                if (loseFinalResponse)
                    context.Response.OnStarting(() =>
                    {
                        context.Abort();
                        return Task.CompletedTask;
                    });
            }
            await next(context);
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var agentKeys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        using var fixture = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            new Uri(address), agentKeys, scope.Session, scope.Artifact,
            rawClass: RawExportRawClass.LiveSelfieImage, plaintext: [0x5A]);
        using var agent = new CaptureRuntimeHttpClient(new Uri(address), fixture.Journal,
            agentKeys, fixture.Clock); // Public production constructor; no handler or trust override.
        var agentId = fixture.Journal.State.CaptureAgentId!.Value;
        var now = fixture.Clock.UtcNow;
        var cache = new RawExportAgentConfigurationCache(agentId);
        var limits = new RawExportAgentConfigurationLimits(600);
        cache.Apply(new(false, new(agentId, Guid.NewGuid(), 1, now.AddMinutes(-1), now.AddMinutes(20), true,
            300, 100, 60, 1024, 1024, 4096, 1024, 4096, 1024), "\"strict-joined\""), now, limits);
        using var owner = new RawExportRetainedSubmissionOwner(agent, cache, limits,
            new(new(1024, 1024, 2, 2048)));
        using var source = owner.Adopt(new SensitiveByteBuffer([0x5A]),
            RawExportRawClass.LiveSelfieImage, now.AddSeconds(-1));
        var acceptance = new RawCaptureAcceptanceDto(scope.Artifact, Guid.NewGuid(), 1, "LiveSelfieImage");
        var send = owner.SubmitAsync(source, scope.Session.ToString("N"), acceptance, scope.Artifact);
        try
        {
            var waiting = false;
            for (var poll = 0; poll < 400; poll++)
            {
                waiting = await observer.Database.SqlQueryRaw<bool>("""
                    SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                      WHERE locktype='advisory' AND granted=false AND objid::integer=71496241) AS "Value"
                    """).SingleAsync();
                if (waiting) break;
                await Task.Delay(25);
            }
            if (!waiting && send.IsCompleted)
            {
                try
                {
                    var early = await send;
                    throw new Xunit.Sdk.XunitException($"B/R1 gate not reached; early outcome={early.Result.OutcomeCode}");
                }
                catch (RawIngressTransportException error)
                {
                    throw new Xunit.Sdk.XunitException(
                        $"B/R1 gate not reached; transport={error.ReasonCode}; stage={error.Stage}; copied={error.ContentBytesCopied}; inner={error.InnerException?.GetType().Name}: {error.InnerException?.Message}");
                }
                catch (CaptureAgentFlowException error)
                {
                    throw new Xunit.Sdk.XunitException(
                        $"B/R1 gate not reached; owner={error.ReasonCode}; inner={error.InnerException?.GetType().Name}: {error.InnerException?.Message}");
                }
            }
            Assert.True(waiting, "B/R1 did not reach the held pre-commit boundary.");
            Assert.Equal(1, Volatile.Read(ref rawPosts));
            Assert.False(send.IsCompleted);
            Assert.Equal(0, bodyProbe.FirstReadCalls);
            Assert.Equal(0, firstRead.ReadCalls);
            Assert.False(await ExpectFenceCommittedR1(observerConnection));
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496241)", gate);
            await release.ExecuteNonQueryAsync();
        }
        if (loseFinalResponse)
        {
            var unknown = await Assert.ThrowsAsync<CaptureAgentFlowException>(() => send.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.Equal("SUBMISSION_STATE_UNKNOWN", unknown.ReasonCode);
            var retryError = await Record.ExceptionAsync(() =>
                owner.SubmitAsync(source, scope.Session.ToString("N"), acceptance, scope.Artifact));
            // Count at Kestrel before inspecting the exception: a transport
            // failure on an illicit second POST must not mask that second send.
            Assert.Equal(1, Volatile.Read(ref rawPosts));
            var retry = Assert.IsType<CaptureAgentFlowException>(retryError);
            Assert.Equal("SUBMISSION_STATE_UNKNOWN", retry.ReasonCode);
        }
        else
        {
            var result = await send.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(RawExportSourceIngressCodes.TemporarilyUnavailable, result.Result.OutcomeCode);
        }
        Assert.Equal(1, Volatile.Read(ref rawPosts));
        Assert.Equal(1, bodyProbe.FirstReadCalls);
        Assert.True(bodyProbe.CommittedAtFirstRead);
        Assert.Equal(1, firstRead.ReadCalls);
        Assert.True(firstRead.CommittedAtRead);
        Assert.True(await ExpectFenceCommittedR1(observerConnection));
    }

    // F3 is a topology mutation, not a product-source mutation. A transparent
    // intermediary forwards the headers to the real Kestrel/B transaction.
    // The mutation makes that intermediary synthesize 100 before Kestrel can
    // authorize body release. Both paths use a platform-trusted TLS certificate.
    [IsolatedTlsFact]
    public async Task ExpectFence_IntermediaryCannotQualifyBySendingItsOwnContinue()
    {
        var prematureContinue = Environment.GetEnvironmentVariable("A3_F3_EARLY_CONTINUE") == "1";
        using var certificate = new X509Certificate2(
            Environment.GetEnvironmentVariable("A3_TEST_TLS_IP_PFX")!,
            Environment.GetEnvironmentVariable("A3_TEST_TLS_PFX_PASSWORD"), X509KeyStorageFlags.UserKeySet);
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_expect_f3");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var observerConnection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
            { Pooling = false }.ConnectionString;
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_hold_f3_r1() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
              PERFORM pg_catalog.pg_advisory_xact_lock(71496242);
              RETURN NEW;
            END $fn$;
            CREATE TRIGGER a3_test_hold_f3_r1
              AFTER INSERT ON tagekyc.raw_export_source_reservations
              FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_hold_f3_r1();
            """);
        await using var gate = new NpgsqlConnection(observerConnection);
        await gate.OpenAsync();
        await using (var acquire = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock(71496242)", gate))
            await acquire.ExecuteNonQueryAsync();

        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observerConnection);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var firstRead = new ExpectFenceFirstRead(observerConnection);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152), broker, firstRead, 1_048_576);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listen => listen.UseHttps(certificate)));
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        await using var app = builder.Build();
        var rawPosts = 0;
        var serverBodyProbe = new ExpectFenceServerBodyProbe(observerConnection);
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "POST" &&
                context.Request.Path == "/api/ekyc/raw-export/source-ingress")
            {
                Interlocked.Increment(ref rawPosts);
                context.Request.Body = serverBodyProbe.Wrap(context.Request.Body);
            }
            await next(context);
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single());
        await using var intermediary = new ExpectFenceForwardingIntermediary(address, certificate,
            observerConnection, prematureContinue);
        using var agentKeys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        using var fixture = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            intermediary.Origin, agentKeys, scope.Session, scope.Artifact,
            rawClass: RawExportRawClass.LiveSelfieImage, plaintext: [0x5A]);
        using var agent = new CaptureRuntimeHttpClient(intermediary.Origin, fixture.Journal,
            agentKeys, fixture.Clock);
        var send = agent.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease);
        try
        {
            var waiting = false;
            for (var poll = 0; poll < 400; poll++)
            {
                waiting = await observer.Database.SqlQueryRaw<bool>("""
                    SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                      WHERE locktype='advisory' AND granted=false AND objid::integer=71496242) AS "Value"
                    """).SingleAsync();
                if (waiting) break;
                await Task.Delay(25);
            }
            Assert.True(waiting, "Forwarded headers did not reach the held B/R1 transaction.");
            Assert.False(await ExpectFenceCommittedR1(observerConnection));
            Assert.Equal(1, Volatile.Read(ref rawPosts));
            Assert.Equal(0, serverBodyProbe.FirstReadCalls);
            Assert.Equal(0, firstRead.ReadCalls);
            if (prematureContinue)
            {
                var committedAtProxyFirstBody = await intermediary.FirstBodyRead.WaitAsync(TimeSpan.FromSeconds(10));
                // The intended F3 RED: the Agent trusted the TLS peer, but
                // that peer was not the Kestrel workload that committed B/R1.
                Assert.True(committedAtProxyFirstBody,
                    "An intermediary-generated 100 released raw body before durable B/R1.");
            }
            else Assert.False(intermediary.FirstBodyRead.IsCompleted);
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496242)", gate);
            await release.ExecuteNonQueryAsync();
        }
        var result = await send.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(RawExportSourceIngressCodes.TemporarilyUnavailable, result.OutcomeCode);
        Assert.True(await intermediary.FirstBodyRead.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(1, Volatile.Read(ref rawPosts));
        Assert.Equal(1, serverBodyProbe.FirstReadCalls);
        Assert.True(serverBodyProbe.CommittedAtFirstRead);
        Assert.True(firstRead.CommittedAtRead);
        if (!prematureContinue &&
            Environment.GetEnvironmentVariable("A3_SITE_QUALIFICATION_RECORD_PATH") is { Length: > 0 } recordPath)
        {
            var observed = DateTimeOffset.UtcNow;
            var record = JsonSerializer.SerializeToUtf8Bytes(new
            {
                formatVersion = 1,
                qualificationId = $"a3-site-{Guid.NewGuid():N}",
                siteId = Environment.GetEnvironmentVariable("A3_SITE_QUALIFICATION_SITE_ID") ?? "development-site",
                endpointOrigin = intermediary.Origin.AbsoluteUri.TrimEnd('/'),
                deploymentRevision = Environment.GetEnvironmentVariable("A3_SITE_QUALIFICATION_DEPLOYMENT_REVISION") ?? "development-1",
                status = "PASS",
                observedAtUtc = observed,
                validUntilUtc = observed.AddHours(8),
                agentBodySendsWhileBOrR1Held = 0,
                serverApplicationBodyReadsWhileBOrR1Held = 0,
                rawPostCount = Volatile.Read(ref rawPosts),
                kestrelContinueRelayedAfterCommit = true,
                earlyOrIntermediaryContinueObserved = false,
                applicationPrebufferObserved = false,
                hiddenRetryObserved = false
            });
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath)!);
            await File.WriteAllBytesAsync(recordPath, record);
        }
    }

    // One shared positive control for the Agent, broker and end-to-end fence.
    // The advisory lock holds the real B transaction before commit; both
    // observers query PostgreSQL using a different connection.
    [Fact]
    public async Task ExpectFence_DurableR1PrecedesAgentSerializationAndFirstServerRead()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_expect_fence");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var observerConnection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
            { Pooling = false }.ConnectionString;
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a3_test_hold_r1_commit() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
              PERFORM pg_catalog.pg_advisory_xact_lock(71496235);
              RETURN NEW;
            END $fn$;
            CREATE TRIGGER a3_test_hold_r1_commit
              AFTER INSERT ON tagekyc.raw_export_source_reservations
              FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_hold_r1_commit();
            """);
        await using var gate = new NpgsqlConnection(observerConnection);
        await gate.OpenAsync();
        await using (var acquire = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_lock(71496235)", gate))
            await acquire.ExecuteNonQueryAsync();

        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observerConnection);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var firstRead = new ExpectFenceFirstRead(observerConnection);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152), broker, firstRead, 1_048_576);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var agentKeys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        using var fixture = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            new Uri(address), agentKeys, scope.Session, scope.Artifact, rawClass: RawExportRawClass.LiveSelfieImage,
            plaintext: "synthetic-retainedsource"u8.ToArray());
        // Use the production raw handler, overriding only local development
        // certificate trust. No finite test-only Expect timeout is installed.
        var rawHandler = CaptureRuntimeHttpClient.CreateRawIngressHandler();
        rawHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        using var sendObserver = new ExpectFenceContentObserver(observerConnection, rawHandler);
        using var agent = new CaptureRuntimeHttpClient(new Uri(address), fixture.Journal,
            agentKeys, fixture.Clock, rawTransport: sendObserver);
        var send = agent.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease);
        try
        {
            var waiting = false;
            for (var poll = 0; poll < 400; poll++)
            {
                waiting = await observer.Database.SqlQueryRaw<bool>("""
                    SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks
                      WHERE locktype='advisory' AND granted=false AND objid::integer=71496235) AS "Value"
                    """).SingleAsync();
                if (waiting) break;
                await Task.Delay(25);
            }
            Assert.True(waiting, "Real B transaction did not reach the pre-commit gate.");
            Assert.False(send.IsCompleted);
            Assert.Equal(0, sendObserver.SerializeCalls);
            Assert.Equal(0, firstRead.ReadCalls);
            Assert.False(await ExpectFenceCommittedR1(observerConnection));
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_catalog.pg_advisory_unlock(71496235)", gate);
            await release.ExecuteNonQueryAsync();
        }
        var result = await send.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(RawExportSourceIngressCodes.TemporarilyUnavailable, result.OutcomeCode);
        Assert.True(sendObserver.ObservedExpect);
        Assert.Equal(1, sendObserver.SerializeCalls);
        Assert.Equal(1, sendObserver.CompletedCopyCalls);
        Assert.True(sendObserver.CommittedAtSerialization);
        Assert.Equal(1, firstRead.ReadCalls);
        Assert.True(firstRead.CommittedAtRead);
        Assert.True(await ExpectFenceCommittedR1(observerConnection));
    }

    // A non-transactional sequence proves the real B/R1 insert trigger ran;
    // its subsequent exception rolls the entire broker transaction back.
    [IsolatedTlsTheory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(1025)]
    public async Task ExpectFence_RolledBackR1NeverReleasesAgentBody(int bodyLength)
    {
        using var certificate = new X509Certificate2(
            Environment.GetEnvironmentVariable("A3_TEST_TLS_IP_PFX")!,
            Environment.GetEnvironmentVariable("A3_TEST_TLS_PFX_PASSWORD"), X509KeyStorageFlags.UserKeySet);
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_expect_rollback");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var observerConnection = new NpgsqlConnectionStringBuilder(observer.Database.GetConnectionString())
            { Pooling = false }.ConnectionString;
        await observer.Database.ExecuteSqlRawAsync("""
            CREATE SEQUENCE tagekyc.a3_test_rollback_probe;
            CREATE FUNCTION tagekyc.a3_test_rollback_r1() RETURNS trigger
            LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog AS $fn$
            BEGIN
              PERFORM pg_catalog.nextval('tagekyc.a3_test_rollback_probe');
              RAISE EXCEPTION 'A3_TEST_ROLLBACK_BEFORE_R1_COMMIT';
            END $fn$;
            CREATE TRIGGER a3_test_rollback_r1
              AFTER INSERT ON tagekyc.raw_export_source_reservations
              FOR EACH ROW EXECUTE FUNCTION tagekyc.a3_test_rollback_r1();
            """);

        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(observerConnection);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(brokerSource,
            preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var firstRead = new ExpectFenceFirstRead(observerConnection);
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152), broker, firstRead, 1_048_576);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listen => listen.UseHttps(certificate)));
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        var rawPosts = 0;
        var bodyProbe = new ExpectFenceServerBodyProbe(observerConnection);
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "POST" &&
                context.Request.Path == "/api/ekyc/raw-export/source-ingress")
            {
                Interlocked.Increment(ref rawPosts);
                context.Request.Body = bodyProbe.Wrap(context.Request.Body);
            }
            await next(context);
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var agentKeys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        using var fixture = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            new Uri(address), agentKeys, scope.Session, scope.Artifact, rawClass: RawExportRawClass.LiveSelfieImage,
            plaintext: Enumerable.Repeat((byte)0x5A, bodyLength).ToArray(), maximumRawBytes: 2048);
        using var agent = new CaptureRuntimeHttpClient(new Uri(address), fixture.Journal,
            agentKeys, fixture.Clock);

        var error = await Assert.ThrowsAsync<CaptureAgentFlowException>(async () =>
            await agent.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease));
        Assert.Contains("NOT_READY", error.ReasonCode, StringComparison.Ordinal);
        Assert.True(await observer.Database.SqlQueryRaw<bool>(
            "SELECT is_called AS \"Value\" FROM tagekyc.a3_test_rollback_probe").SingleAsync());
        Assert.False(await ExpectFenceCommittedR1(observerConnection));
        Assert.Equal(1, Volatile.Read(ref rawPosts));
        Assert.Equal(0, bodyProbe.FirstReadCalls);
        Assert.Equal(0, firstRead.ReadCalls);
    }

    private static async Task<bool> ExpectFenceCommittedR1(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM tagekyc.raw_export_source_reservations)
              AND EXISTS (SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts)
            """, connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private sealed class ExpectFenceForwardingIntermediary : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource lifetime = new(TimeSpan.FromSeconds(40));
        private readonly Task completion;
        private TcpClient? downstream;
        private TcpClient? upstream;
        private readonly TaskCompletionSource<bool> firstBodyRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Uri Origin { get; }
        public Task<bool> FirstBodyRead => firstBodyRead.Task;

        public ExpectFenceForwardingIntermediary(Uri kestrelOrigin, X509Certificate2 certificate,
            string observerConnection, bool prematureContinue)
        {
            listener.Start();
            Origin = new Uri($"https://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}");
            completion = Task.Run(async () =>
            {
                var token = lifetime.Token;
                downstream = await listener.AcceptTcpClientAsync(token);
                using var agentTls = new SslStream(downstream.GetStream());
                await agentTls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    ApplicationProtocols = [SslApplicationProtocol.Http11]
                }, token);
                var headers = await ReadHeaders(agentTls, token);

                upstream = new TcpClient();
                await upstream.ConnectAsync(kestrelOrigin.IdnHost, kestrelOrigin.Port, token);
                using var kestrelTls = new SslStream(upstream.GetStream());
                await kestrelTls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = kestrelOrigin.IdnHost,
                    ApplicationProtocols = [SslApplicationProtocol.Http11],
                    CertificateRevocationCheckMode = X509RevocationMode.Online
                }, token);
                await kestrelTls.WriteAsync(headers, token);
                await kestrelTls.FlushAsync(token);
                if (prematureContinue)
                {
                    await agentTls.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n"), token);
                    await agentTls.FlushAsync(token);
                }

                await Task.WhenAll(ForwardAgentBody(agentTls, kestrelTls, observerConnection, token),
                    ForwardKestrelResponse(kestrelTls, agentTls, token));
            });
        }

        private async Task ForwardAgentBody(SslStream agentTls, SslStream kestrelTls,
            string observerConnection, CancellationToken token)
        {
            var buffer = new byte[4096];
            var first = true;
            while (true)
            {
                var read = await agentTls.ReadAsync(buffer, token);
                if (read == 0) break;
                if (first)
                {
                    first = false;
                    firstBodyRead.TrySetResult(await ExpectFenceCommittedR1(observerConnection));
                }
                await kestrelTls.WriteAsync(buffer.AsMemory(0, read), token);
                await kestrelTls.FlushAsync(token);
            }
        }

        private static async Task ForwardKestrelResponse(SslStream kestrelTls, SslStream agentTls,
            CancellationToken token)
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = await kestrelTls.ReadAsync(buffer, token);
                if (read == 0) break;
                await agentTls.WriteAsync(buffer.AsMemory(0, read), token);
                await agentTls.FlushAsync(token);
            }
        }

        private static async Task<byte[]> ReadHeaders(SslStream stream, CancellationToken token)
        {
            using var bytes = new MemoryStream();
            var one = new byte[1];
            while (bytes.Length < 8192)
            {
                if (await stream.ReadAsync(one, token) != 1) throw new EndOfStreamException();
                bytes.WriteByte(one[0]);
                var length = (int)bytes.Length;
                if (length >= 4)
                {
                    var current = bytes.GetBuffer();
                    if (current[length - 4] == '\r' && current[length - 3] == '\n' &&
                        current[length - 2] == '\r' && current[length - 1] == '\n')
                        return bytes.ToArray();
                }
            }
            throw new InvalidDataException("Intermediary request headers exceed test bound.");
        }

        public async ValueTask DisposeAsync()
        {
            await lifetime.CancelAsync();
            listener.Stop();
            downstream?.Dispose();
            upstream?.Dispose();
            try { await completion; }
            catch (Exception error) when (error is OperationCanceledException or SocketException or IOException
                or System.Security.Authentication.AuthenticationException or ObjectDisposedException) { }
            lifetime.Dispose();
        }
    }

    private sealed class ExpectFenceServerBodyProbe(string connectionString)
    {
        internal int FirstReadCalls;
        internal bool CommittedAtFirstRead;
        internal Stream Wrap(Stream inner) => new ObservedStream(inner, this, connectionString);

        private sealed class ObservedStream(Stream inner, ExpectFenceServerBodyProbe owner,
            string connectionString) : Stream
        {
            private bool first = true;
            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => inner.CanSeek;
            public override bool CanWrite => inner.CanWrite;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => inner.Position = value; }
            public override void Flush() => inner.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) => inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
            public override Task WriteAsync(byte[] buffer, int offset, int count,
                CancellationToken cancellationToken) => inner.WriteAsync(buffer, offset, count, cancellationToken);
            public override int Read(byte[] buffer, int offset, int count) =>
                ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
                CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (first)
                {
                    first = false;
                    owner.CommittedAtFirstRead = await ExpectFenceCommittedR1(connectionString);
                    Interlocked.Increment(ref owner.FirstReadCalls);
                }
                return await inner.ReadAsync(buffer, cancellationToken);
            }
        }
    }

    // R2 itself is outside this pre-body claim. This double is installed only
    // after the production broker has committed and performs one real body read.
    private sealed class ExpectFenceFirstRead(string connectionString) : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int ReadCalls;
        internal bool CommittedAtRead;
        public async Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            CommittedAtRead = await ExpectFenceCommittedR1(connectionString);
            var first = new byte[1];
            Assert.Equal(1, await body.ReadAsync(first, cancellationToken));
            Interlocked.Increment(ref ReadCalls);
            return new(CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable, null, null, null, null);
        }
    }

    private sealed class ExpectFenceContentObserver(string connectionString, HttpMessageHandler inner)
        : DelegatingHandler(inner)
    {
        internal int SerializeCalls;
        internal int CompletedCopyCalls;
        internal bool CommittedAtSerialization;
        internal bool ObservedExpect;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ObservedExpect = request.Headers.ExpectContinue == true;
            Assert.NotNull(request.Content);
            request.Content = new ObservedContent(request.Content, async () =>
            {
                CommittedAtSerialization = await ExpectFenceCommittedR1(connectionString);
                Interlocked.Increment(ref SerializeCalls);
            }, () => Interlocked.Increment(ref CompletedCopyCalls));
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class ObservedContent : HttpContent
    {
        private readonly HttpContent inner;
        private readonly Func<Task> beforeSerialize;
        private readonly Action afterCopy;
        internal ObservedContent(HttpContent inner, Func<Task> beforeSerialize, Action afterCopy)
        {
            this.inner = inner;
            this.beforeSerialize = beforeSerialize;
            this.afterCopy = afterCopy;
            foreach (var header in inner.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await beforeSerialize();
            await inner.CopyToAsync(stream);
            afterCopy();
        }
        protected override bool TryComputeLength(out long length)
        {
            length = inner.Headers.ContentLength ?? 0;
            return inner.Headers.ContentLength.HasValue;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
#endif

    private sealed class CountingServerRequestStream(Stream inner, Action onRead) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override int Read(byte[] buffer, int offset, int count)
        {
            onRead();
            return inner.Read(buffer, offset, count);
        }
        public override int Read(Span<byte> buffer)
        {
            onRead();
            return inner.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            onRead();
            return inner.ReadAsync(buffer, cancellationToken);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            onRead();
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }

    private sealed class FailingUnwrapKekProvider(IKekOperationProvider inner) : IKekOperationProvider
    {
        internal int UnwrapCalls;

        public Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken token,
            ReadOnlyMemory<byte> fingerprint, IAttemptDekCandidate candidate, CancellationToken cancellationToken) =>
            inner.WrapDekAsync(reference, token, fingerprint, candidate, cancellationToken);

        public Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken token,
            ReadOnlyMemory<byte> fingerprint, CancellationToken cancellationToken) =>
            inner.LookupByOperationTokenAsync(token, fingerprint, cancellationToken);

        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> fingerprint, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref UnwrapCalls);
            throw new IOException("TEST_ONLY_ENCRYPTION_UNWRAP_FAILURE");
        }
    }

    private sealed class CapturingBusinessResponseHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        internal HttpStatusCode? Status;
        internal string? Body;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            Status = response.StatusCode;
            Body = await response.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }

    // Test-only scheduling at the broker handoff. The first call never invokes
    // R2 and is canceled by the client; every later call delegates unchanged.
    private sealed class FirstHandoffGate(ICaptureRuntimeRawIngressBodyPipeline inner)
        : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int Calls;
        internal TaskCompletionSource<RawIngressBrokerHandoff> Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref Calls) == 1)
            {
                Entered.TrySetResult(handoff);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("TEST_ONLY_FIRST_HANDOFF_MUST_BE_CANCELED");
            }
            return await inner.ProcessAsync(context, handoff, body, cancellationToken);
        }
    }

#if A3_ACCEPTANCE
    // Test-only scheduling: add one advisory wait before the installed R5 body
    // acquires row locks. Every other statement remains the installed function.
    private static async Task InstallR5EntryGate(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var read = new NpgsqlCommand("""
            SELECT pg_catalog.pg_get_functiondef(
              'tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)'::pg_catalog.regprocedure)
            """, connection);
        var installed = (string?)await read.ExecuteScalarAsync();
        Assert.NotNull(installed);
        var begin = System.Text.RegularExpressions.Regex.Match(installed,
            @"\bBEGIN\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        Assert.True(begin.Success);
        var scheduled = installed.Insert(begin.Index + begin.Length,
            "\n  PERFORM pg_catalog.pg_advisory_xact_lock(71496232);");
        await using var install = new NpgsqlCommand(scheduled, connection);
        await install.ExecuteNonQueryAsync();
    }

    // Premise injection only: the production Agent signs and sends its exact
    // metadata; this transport shim corrupts one body byte before real TLS.
    // It does not fabricate the Server's mismatch result or bypass R2.
    private sealed class OneByteTransitCorruptionHandler(HttpMessageHandler inner)
        : DelegatingHandler(inner)
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.NotNull(request.Content);
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            Assert.NotEmpty(bytes);
            bytes[0] ^= 1;
            var replacement = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
            request.Content = replacement;
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class LostResponseAfterCommitHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        internal int Calls;
        internal HttpStatusCode? ObservedStatus;
        internal string? ObservedOutcomeCode;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var response = await base.SendAsync(request, cancellationToken);
            Calls++;
            ObservedStatus = response.StatusCode;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var body = JsonDocument.Parse(json);
            if (!body.RootElement.TryGetProperty("outcomeCode", out var outcome))
                throw new InvalidOperationException(
                    $"TEST_ONLY_EXPECTED_BUSINESS_RESPONSE status={(int)response.StatusCode} body={json}");
            ObservedOutcomeCode = outcome.GetString();
            throw new HttpRequestException("TEST_ONLY_RESPONSE_LOSS_AFTER_SERVER_COMMIT");
        }
    }
#endif

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
}

internal static class R2R6LoopbackHttpsHarness
{
    // This shared test harness requires a local .NET development certificate.
    // TLS is real; certificate provisioning/trust is not a production claim.
    internal static void Configure(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listen => listen.UseHttps()));
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
    }

    internal static SocketsHttpHandler CreateHandler() => new()
    {
        UseProxy = false,
        Expect100ContinueTimeout = TimeSpan.FromSeconds(30),
        SslOptions = new() { RemoteCertificateValidationCallback = (_, _, _, _) => true }
    };

    internal static HttpClient CreateClient() => new(CreateHandler());
}
