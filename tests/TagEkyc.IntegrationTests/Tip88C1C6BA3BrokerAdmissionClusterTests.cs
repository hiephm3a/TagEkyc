#if A3_ACCEPTANCE
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using AgentIngressMetadata = TagEkyc.CaptureAgent.Core.RawExportSourceIngressMetadata;

namespace TagEkyc.IntegrationTests;

// Shared production arrangement for the reachable BROKER_ADMISSION rows. The
// synthetic pieces supply only an already-authenticated caller and input data;
// admission, broker SQL, HTTP mapping and Agent parsing are production code.
[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3BrokerAdmissionClusterTests(PostgresPersistenceFixture postgres)
{
    [Theory]
    [InlineData("P02-missing", RawExportSourceIngressCodes.BindingInvalid, 403)]
    [InlineData("P02-foreign", RawExportSourceIngressCodes.BindingInvalid, 403)]
    [InlineData("P03", RawExportSourceIngressCodes.NotFoundOrNotAllowed, 403)]
    [InlineData("P04", RawExportSourceIngressCodes.TransportProtocolInvalid, 400)]
    [InlineData("P05", RawExportSourceIngressCodes.CapabilityUnavailable, 503)]
    [InlineData("P07", RawExportSourceIngressCodes.PlaintextRetentionInvalid, 422)]
    public async Task ReachableAdmissionFinalsTraverseKestrelAgentWithoutReadingBody(
        string row, string expectedCode, int expectedStatus)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            "a3_broker_admission_" + row.Replace("-", "_", StringComparison.Ordinal).ToLowerInvariant());
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        Tip88C1C6BA3RetentionCheckpointTests.Scope? foreign = null;
        if (row == "P02-foreign")
            foreign = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true,
                externalConsentArtifactRef: "synthetic-foreign-consent", bootstrapRuntime: false,
                clientApplicationId: Guid.Parse("b3000000-0000-4000-8000-000000000001"));
        await using var observer = isolated.CreateDbContext();
        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
            observer.Database.GetConnectionString()!);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        using var keys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(
            brokerSource, preflight.GetRequiredService<IContentCommitmentService>(),
            preflight.GetRequiredService<ISubjectRefTokenService>());
        var bodyPipeline = new RejectBodyPipeline();
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 2_097_152, 1_048_576),
            broker, bodyPipeline, 1_048_576);
        var reads = 0;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.Use(async (http, next) =>
        {
            http.Request.Body = new CountingReadStream(http.Request.Body,
                () => Interlocked.Increment(ref reads));
            await next();
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var origin = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single());

        using var request = new AdmissionAgentFixture(origin, keys,
            row == "P03" ? Guid.NewGuid() : scope.Session, row switch
            {
                "P02-missing" => Guid.NewGuid(),
                "P02-foreign" => foreign!.Artifact,
                _ => scope.Artifact
            },
            retentionBudgetSeconds: row == "P07" ? 10 : 300);
        if (foreign is not null)
        {
            Assert.NotEqual(await ClientForSession(observer, scope.Session),
                await ClientForSession(observer, foreign.Session));
            Assert.Equal(1, await BoundRows(observer, foreign.Session, foreign.Artifact));
            Assert.Equal(0, await BoundRows(observer, scope.Session, foreign.Artifact));
        }
        var before = await DurableRows(observer);
        var beforeProviders = await ProviderRows(observer);
        using var transport = new RecordingTransport(R2R6LoopbackHttpsHarness.CreateHandler(), outgoing =>
        {
            if (row == "P04") outgoing.Content!.Headers.ContentEncoding.Add("gzip");
            if (row == "P05")
            {
                outgoing.Headers.Remove("X-TagEkyc-Raw-Class");
                outgoing.Headers.TryAddWithoutValidation("X-TagEkyc-Raw-Class", "UnsupportedPortrait");
            }
        });
        using var client = new CaptureRuntimeHttpClient(origin, request.Journal, keys, request.Clock,
            rawTransport: transport);

        var result = await client.SubmitRawExportSourceAsync(request.Metadata, request.Lease);

        Assert.Equal(expectedCode, result.OutcomeCode);
        Assert.Equal(expectedStatus, (int)transport.StatusCode!.Value);
        Assert.Equal($"{{\"outcomeCode\":\"{expectedCode}\"}}", transport.ResponseBody);
        Assert.Null(result.SourceArtifactId);
        Assert.Null(result.CurrentSourceState);
        Assert.Null(result.CurrentDisposition);
        Assert.Null(result.RetryNotBeforeUtc);
        Assert.Equal(0, Volatile.Read(ref reads));
        Assert.Equal(0, bodyPipeline.Calls);
        Assert.Equal(beforeProviders, await ProviderRows(observer));

        var after = await DurableRows(observer);
        if (row is not "P07")
        {
            Assert.Equal(before, after);
            if (foreign is not null)
            {
                Assert.DoesNotContain(foreign.Artifact.ToString("D"), transport.ResponseBody,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(foreign.RuntimeBinding.ToString("D"), transport.ResponseBody,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            var alias = await observer.RawExportSourceIngressClaimAliases.AsNoTracking()
                .SingleAsync(value => value.IngressIdempotencyKey == request.Metadata.IngressIdempotencyKey);
            Assert.Equal("Evaluating", alias.AliasState);
            Assert.Equal("Completed", alias.CurrentClaimEvaluationDisposition);
            Assert.NotNull(alias.IngressClaimId);
            var claim = await observer.RawExportSourceIngressClaims.AsNoTracking()
                .SingleAsync(value => value.IngressClaimId == alias.IngressClaimId);
            Assert.Equal("ClaimEvaluating", claim.ClaimState);
            Assert.False(await observer.RawExportSourceReservations.AsNoTracking()
                .AnyAsync(value => value.IngressClaimId == alias.IngressClaimId));
        }
    }

    [Fact]
    public async Task P05_ExistingEvaluatingShellSurvivesCapabilityLossAndSameLeaseRetry()
        => await P05ExistingShell(strictTls: false);

    [Tip88C1C6BA3R2R6ClusterHttpTests.IsolatedTlsFact]
    public async Task P05_ExistingEvaluatingShellStrictTlsKeepsOriginalLease()
        => await P05ExistingShell(strictTls: true);

    private async Task P05ExistingShell(bool strictTls)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_p05_existing_shell");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var login = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
            observer.Database.GetConnectionString()!);
        await using var source = NpgsqlDataSource.Create(login);
        var options = RawIngressBrokerOptions.Read(Tip88C1C6BA3SyntheticComposition.BrokerConfiguration());
        await using var unavailable = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(
            source, options, missingKey: true);
        await using var restored = Tip88C1C6BA3SyntheticComposition.QualifiedBrokerServices(source, options);
        var selected = new SelectableBroker(
            unavailable.GetRequiredService<IRawIngressMetadataBroker>(),
            restored.GetRequiredService<IRawIngressMetadataBroker>());
        var bodyPipeline = new RejectBodyPipeline();
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 2_097_152, 1_048_576),
            selected, bodyPipeline, 1_048_576);
        var reads = 0;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        using var certificate = strictTls ? new X509Certificate2(
            Environment.GetEnvironmentVariable("A3_TEST_TLS_IP_PFX")!,
            Environment.GetEnvironmentVariable("A3_TEST_TLS_PFX_PASSWORD"), X509KeyStorageFlags.UserKeySet) : null;
        if (strictTls)
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
                listen => listen.UseHttps(certificate!)));
        else R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.Use(async (http, next) =>
        {
            http.Request.Body = new CountingReadStream(http.Request.Body,
                () => Interlocked.Increment(ref reads));
            await next();
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var origin = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single());
        using var keys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        using var request = new AdmissionAgentFixture(origin, keys, scope.Session, scope.Artifact, 300);

        // Production B creates the shell and token before the tested retry. The
        // same signed metadata, idempotency key and retained lease are then used
        // by the public Agent/Kestrel path; no fabricated alias is inserted.
        await using (var seed = isolated.CreateDbContext())
        await using (var tx = await seed.Database.BeginTransactionAsync())
        {
            await seed.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('tagekyc.actor_principal_id',{Tip88C1C6BA3ConsentRetentionTests.Principal.ToString("D")},true)");
            await seed.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
            var m = request.Metadata;
            var token = await seed.Database.SqlQuery<string>($"""
                SELECT token_variant AS "Value"
                FROM tagekyc.raw_export_begin_retained_source_ingress_with_authority(
                 {Tip88C1C6BA3ConsentRetentionTests.Principal},
                 {Tip88C1C6BA3ConsentRetentionTests.Client},
                 {request.Journal.State.CaptureAgentId!.Value.ToString("N")},
                 {request.Journal.State.DeviceInstallationId!.Value.ToString("N")},
                 {m.IngressIdempotencyKey.ToString("N")},{scope.Session},{scope.Acceptance},{scope.Artifact},
                 1,'LiveSelfieImage','synthetic-challenge',{m.ClaimedPlaintextLength},
                 {m.MediaType},{m.CapturedAtUtc},{m.PlaintextRetentionStartedAtUtc},
                 {m.PlaintextRetentionExpiresAtUtc},{m.PlaintextRetentionBudgetSeconds},
                 'fixture-content-commitment',1,{Guid.NewGuid()},10,100,{scope.Permit},1::bigint)
                """).SingleAsync();
            Assert.Equal("NewClaimEvaluationToken", token);
            await tx.CommitAsync();
        }

        var before = await AliasAndClaim();
        Assert.Equal(2, before.Length);
        var beforeDurable = await DurableRows(observer);
        var originalBudget = request.Lease.PlaintextRetentionBudgetSeconds;
        var originalExpiry = request.Lease.PlaintextRetentionExpiresAtUtc;
        using var transport = strictTls ? null : new RecordingTransport(R2R6LoopbackHttpsHarness.CreateHandler());
        using var client = strictTls
            ? new CaptureRuntimeHttpClient(origin, request.Journal, keys, request.Clock)
            : new CaptureRuntimeHttpClient(origin, request.Journal, keys, request.Clock, rawTransport: transport!);
        var denied = await client.SubmitRawExportSourceAsync(request.Metadata, request.Lease);
        Assert.Equal(RawExportSourceIngressCodes.CapabilityUnavailable, denied.OutcomeCode);
        if (!strictTls) Assert.Equal(HttpStatusCode.ServiceUnavailable, transport!.StatusCode);
        Assert.Equal(before, await AliasAndClaim());
        Assert.Equal(beforeDurable, await DurableRows(observer));
        Assert.Equal(originalBudget, request.Lease.PlaintextRetentionBudgetSeconds);
        Assert.Equal(originalExpiry, request.Lease.PlaintextRetentionExpiresAtUtc);
        Assert.Equal(0, Volatile.Read(ref reads));
        Assert.Equal(0, bodyPipeline.Calls);

        selected.Restore();
        var retry = await client.SubmitRawExportSourceAsync(request.Metadata, request.Lease);
        Assert.Equal(RawExportSourceIngressCodes.EvaluationInProgress, retry.OutcomeCode);
        Assert.Equal(before, await AliasAndClaim());
        Assert.Equal(beforeDurable, await DurableRows(observer));
        Assert.Equal(originalBudget, request.Lease.PlaintextRetentionBudgetSeconds);
        Assert.Equal(originalExpiry, request.Lease.PlaintextRetentionExpiresAtUtc);
        Assert.Equal(0, Volatile.Read(ref reads));
        Assert.Equal(0, bodyPipeline.Calls);

        async Task<string[]> AliasAndClaim() => await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'alias' family,to_jsonb(r)::text value FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
            ) rows ORDER BY family,value
            """).ToArrayAsync();
    }

    [Fact]
    public async Task CapacityDenialTraversesKestrelAgentReleasesSlotAndCreatesNoCustody()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_w3_capacity");
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var before = await DurableRows(observer);
        var beforeProviders = await ProviderRows(observer);
        using var keys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        var broker = new BlockingFinalBroker();
        var bodyPipeline = new RejectBodyPipeline();
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 2_097_152, 1_048_576),
            broker, bodyPipeline, 1_048_576);
        var reads = 0;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.Use(async (http, next) =>
        {
            http.Request.Body = new CountingReadStream(http.Request.Body,
                () => Interlocked.Increment(ref reads));
            await next();
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var origin = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single());

        using var firstRequest = new AdmissionAgentFixture(origin, keys, scope.Session, scope.Artifact, 300);
        using var deniedRequest = new AdmissionAgentFixture(origin, keys, scope.Session, scope.Artifact, 300);
        using var firstClient = new CaptureRuntimeHttpClient(origin, firstRequest.Journal, keys,
            firstRequest.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
        using var deniedTransport = new RecordingTransport(R2R6LoopbackHttpsHarness.CreateHandler());
        using var deniedClient = new CaptureRuntimeHttpClient(origin, deniedRequest.Journal, keys,
            deniedRequest.Clock, rawTransport: deniedTransport);

        var first = firstClient.SubmitRawExportSourceAsync(firstRequest.Metadata, firstRequest.Lease);
        await broker.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(first.IsCompleted);
        try
        {
            var denied = await deniedClient.SubmitRawExportSourceAsync(
                deniedRequest.Metadata, deniedRequest.Lease);
            Assert.Equal(RawExportSourceIngressCodes.CapacityUnavailable, denied.OutcomeCode);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedTransport.StatusCode);
            Assert.Equal("{\"outcomeCode\":\"RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE\"}",
                deniedTransport.ResponseBody);
            Assert.Null(denied.SourceArtifactId);
            Assert.Null(denied.CurrentSourceState);
            Assert.Null(denied.CurrentDisposition);
            Assert.Null(denied.RetryNotBeforeUtc);
            Assert.Equal(1, broker.Calls);
            Assert.Equal(0, Volatile.Read(ref reads));
            Assert.Equal(0, bodyPipeline.Calls);
            Assert.Equal(before, await DurableRows(observer));
            Assert.Equal(beforeProviders, await ProviderRows(observer));
        }
        finally
        {
            broker.ReleaseFirst();
        }

        Assert.Equal(RawExportSourceIngressCodes.BindingInvalid, (await first).OutcomeCode);
        var retry = await deniedClient.SubmitRawExportSourceAsync(
            deniedRequest.Metadata, deniedRequest.Lease);
        Assert.Equal(RawExportSourceIngressCodes.BindingInvalid, retry.OutcomeCode);
        Assert.Equal(2, broker.Calls);
        Assert.Equal(0, Volatile.Read(ref reads));
        Assert.Equal(0, bodyPipeline.Calls);
        Assert.Equal(before, await DurableRows(observer));
        Assert.Equal(beforeProviders, await ProviderRows(observer));
    }

    private static Task<Guid> ClientForSession(TagEkycDbContext observer, Guid session) =>
        observer.Database.SqlQuery<Guid>($"""
            SELECT "ClientApplicationId" AS "Value" FROM tagekyc.verification_sessions WHERE "Id"={session}
            """).SingleAsync();

    private static async Task<int> BoundRows(TagEkycDbContext observer, Guid session, Guid artifact)
    {
        await using var transaction = await observer.Database.BeginTransactionAsync();
        await observer.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
        var count = await observer.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.capture_runtime_read_bound_raw_ingress(
             '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
             '60000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,
             {session},{artifact},1,'LiveSelfieImage',1,clock_timestamp())
            """).SingleAsync();
        await transaction.CommitAsync();
        return count;
    }

    private static async Task<string[]> DurableRows(TagEkycDbContext observer) =>
        await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'snapshot' family,to_jsonb(r)::text value FROM tagekyc.raw_export_authority_snapshots r
              UNION ALL SELECT 'alias',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'head',to_jsonb(r)::text FROM tagekyc.raw_export_source_head r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
              UNION ALL SELECT 'provider-key',to_jsonb(r)::text FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();

    private static async Task<string[]> ProviderRows(TagEkycDbContext observer) =>
        await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'provider-key' family,to_jsonb(r)::text value FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();

    private sealed class AcceptedRuntimeAuthenticator : ICaptureRuntimeRequestAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("40000000-0000-4000-8000-000000000001"),
                Guid.Parse("50000000-0000-4000-8000-000000000001"),
                Guid.Parse("60000000-0000-4000-8000-000000000001"), 1, new byte[32],
                Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, 1, 1, 1,
                request.SignedAtUtc, request.Nonce, SHA256.HashData(request.ExactSignedPreimage.Span))));
    }

    private sealed class RejectBodyPipeline : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int Calls;
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("BROKER_ADMISSION_FINAL_MUST_NOT_START_BODY_PIPELINE");
        }
    }

    private sealed class BlockingFinalBroker : IRawIngressMetadataBroker
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task FirstEntered => entered.Task;
        internal int Calls;

        public async Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref Calls) == 1)
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
            }
            return new RawIngressBrokerResult.Final(
                new(RawExportSourceIngressCodes.BindingInvalid));
        }

        internal void ReleaseFirst() => release.TrySetResult();
    }

    private sealed class SelectableBroker(IRawIngressMetadataBroker unavailable,
        IRawIngressMetadataBroker restored) : IRawIngressMetadataBroker
    {
        private bool useRestored;
        public Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken) =>
            (useRestored ? restored : unavailable).AdmitAsync(context, cancellationToken);
        internal void Restore() => useRestored = true;
    }

    private sealed class AdmissionAgentFixture : IDisposable
    {
        internal Tip88C1C6BA3R2R6TransportHarnessTests.FixtureClock Clock { get; } = new();
        internal Tip88C1C6BA3R2R6TransportHarnessTests.FixtureJournal Journal { get; }
        internal RetainedRawBufferLease Lease { get; }
        internal AgentIngressMetadata Metadata { get; }

        internal AdmissionAgentFixture(Uri origin,
            Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys keys,
            Guid sessionId, Guid artifactId, int retentionBudgetSeconds)
        {
            Journal = new(origin, keys, Clock, sessionId);
            var bytes = Enumerable.Range(1, 64).Select(value => (byte)value).ToArray();
            var configuration = new RawExportAgentConfigurationDocument(
                Journal.State.CaptureAgentId!.Value, Guid.NewGuid(), 1,
                Clock.UtcNow.AddMinutes(-1), Clock.UtcNow.AddMinutes(20), true,
                retentionBudgetSeconds, 0, 60, 1024, 1024, 4096, 1024, 4096, 1024);
            Lease = RetainedRawBufferLease.TakeOwnership(bytes, RawExportRawClass.LiveSelfieImage,
                new(new(1024, 1024, 2, 2048)), configuration, Clock.UtcNow, Clock);
            Metadata = new(1, Journal.SessionId, artifactId, 1, RawExportRawClass.LiveSelfieImage,
                Guid.NewGuid(), "image/jpeg", bytes.Length, CaptureRuntimeWireCodec.Digest(bytes),
                Clock.UtcNow.AddSeconds(-1), Lease.PlaintextRetentionStartedAtUtc,
                Lease.PlaintextRetentionExpiresAtUtc, Lease.PlaintextRetentionBudgetSeconds);
        }

        public void Dispose() => Lease.Dispose();
    }

    private sealed class CountingReadStream(Stream inner, Action read) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) { read(); return inner.Read(buffer, offset, count); }
        public override int Read(Span<byte> buffer) { read(); return inner.Read(buffer); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { read(); return inner.ReadAsync(buffer, cancellationToken); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        { read(); return inner.ReadAsync(buffer, offset, count, cancellationToken); }
        public override int ReadByte() { read(); return inner.ReadByte(); }
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }

    private sealed class RecordingTransport(
        HttpMessageHandler inner,
        Action<HttpRequestMessage>? beforeSend = null) : DelegatingHandler(inner)
    {
        internal HttpStatusCode? StatusCode;
        internal string ResponseBody = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            beforeSend?.Invoke(request);
            var response = await base.SendAsync(request, cancellationToken);
            StatusCode = response.StatusCode;
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            ResponseBody = Encoding.UTF8.GetString(bytes);
            var replacement = new ByteArrayContent(bytes);
            foreach (var header in response.Content.Headers)
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response.Content = replacement;
            return response;
        }
    }
}
#endif
