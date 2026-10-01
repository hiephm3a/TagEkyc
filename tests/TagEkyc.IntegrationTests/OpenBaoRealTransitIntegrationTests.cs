using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

// The lifecycle pattern is adapted from SignFlow's audited OpenBao small-site fixture.
// This remains TagEkyc-native: no runtime or source dependency crosses repositories.
[Collection(PostgresPersistenceCollection.Name)]
public sealed class OpenBaoRealTransitIntegrationTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task Real_TLS_AppRole_HMAC_claim_providers_are_exact_versioned_and_policy_isolated()
    {
        await using var bao = await OpenBaoRealHarness.StartAsync();
        var configuration = OpenBaoProductionClaimProviderTests.Configuration();
        ConfigureClaimProvider(configuration, "ContentCommitment", bao,
            bao.ContentRoleIdPath, bao.ContentSecretIdPath);
        ConfigureClaimProvider(configuration, "SubjectRefToken", bao,
            bao.SubjectRoleIdPath, bao.SubjectSecretIdPath);
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(configuration);
        using var provider = services.BuildServiceProvider();
        using (var metadata = await bao.RootKeyMetadataAsync("content-hmac"))
        {
            var data = metadata.RootElement.GetProperty("data");
            Assert.False(data.TryGetProperty("keys", out _));
            Assert.Equal(1, data.GetProperty("latest_version").GetInt32());
            Assert.Equal(0, data.GetProperty("min_available_version").GetInt32());
        }
        await provider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
            .ValidateAsync(CancellationToken.None);

        var payload = Encoding.UTF8.GetBytes("tagekyc-claim-provider-interop-v1");
        var content = await provider.GetRequiredService<IContentCommitmentService>()
            .ComputeAsync(new("content-v1", 1), payload, CancellationToken.None);
        var subject = await provider.GetRequiredService<ISubjectRefTokenService>()
            .ComputeAsync(new("subject-v1", 1), payload, CancellationToken.None);
        Assert.True(content.IsSuccess);
        Assert.True(subject.IsSuccess);
        Assert.Equal(32, content.Mac.Length);
        Assert.Equal(32, subject.Token.Length);
        Assert.NotEqual(content.Mac.ToArray(), subject.Token.ToArray());
        Assert.Equal(await bao.RootHmacAsync("content-hmac", payload, 1), content.Mac.ToArray());
        Assert.Equal(await bao.RootHmacAsync("subject-hmac", payload, 1), subject.Token.ToArray());
        Assert.True(await bao.RootVerifyHmacAsync(
            "content-hmac", payload, content.Mac.ToArray(), 1));
        Assert.True(await bao.RootVerifyHmacAsync(
            "subject-hmac", payload, subject.Token.ToArray(), 1));

        Assert.False(await bao.AppRoleCanHmacAsync(
            bao.ContentRoleIdPath, bao.ContentSecretIdPath, "subject-hmac", payload));
        Assert.False(await bao.AppRoleCanHmacAsync(
            bao.SubjectRoleIdPath, bao.SubjectSecretIdPath, "content-hmac", payload));

        await bao.SetContentClaimPolicyAsync(includeSubjectKey: true);
        try
        {
            var overbroadServices = new ServiceCollection();
            overbroadServices.AddTagEkycProductionRawExportClaimProviders(configuration);
            using var overbroadProvider = overbroadServices.BuildServiceProvider();
            var separation = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
                overbroadProvider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                    .ValidateAsync(CancellationToken.None));
            Assert.Equal(RawExportClaimProviderReadinessValidator.SeparationInvalid,
                separation.Code);
        }
        finally
        {
            await bao.SetContentClaimPolicyAsync(includeSubjectKey: false);
        }

        var wrongVersionConfiguration = OpenBaoProductionClaimProviderTests.Configuration();
        ConfigureClaimProvider(wrongVersionConfiguration, "ContentCommitment", bao,
            bao.ContentRoleIdPath, bao.ContentSecretIdPath);
        ConfigureClaimProvider(wrongVersionConfiguration, "SubjectRefToken", bao,
            bao.SubjectRoleIdPath, bao.SubjectSecretIdPath);
        wrongVersionConfiguration[$"{OpenBaoProductionClaimProviderTests.Root}:ContentCommitment:Keys:0:KeyVersion"] =
            "2";
        wrongVersionConfiguration[$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorVersion"] =
            "2";
        var wrongVersionServices = new ServiceCollection();
        wrongVersionServices.AddTagEkycProductionRawExportClaimProviders(
            wrongVersionConfiguration);
        using (var wrongVersionProvider = wrongVersionServices.BuildServiceProvider())
        {
            var wrongVersion = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
                wrongVersionProvider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                    .ValidateAsync(CancellationToken.None));
            Assert.Equal(RawExportClaimProviderReadinessValidator.ProviderInvalid,
                wrongVersion.Code);
            Assert.Contains("OPENBAO_HMAC_KEY_REFERENCE_INVALID:VERSION",
                wrongVersion.InnerException?.Message,
                StringComparison.Ordinal);
        }

        var deniedConfiguration = OpenBaoProductionClaimProviderTests.Configuration();
        ConfigureClaimProvider(deniedConfiguration, "ContentCommitment", bao,
            bao.ContentRoleIdPath, bao.ContentSecretIdPath);
        ConfigureClaimProvider(deniedConfiguration, "SubjectRefToken", bao,
            bao.SubjectRoleIdPath, bao.SubjectSecretIdPath);
        deniedConfiguration[$"{OpenBaoProductionClaimProviderTests.Root}:ContentCommitment:Keys:0:TransitKeyName"] =
            "content-forbidden";
        var deniedServices = new ServiceCollection();
        deniedServices.AddTagEkycProductionRawExportClaimProviders(deniedConfiguration);
        using var deniedProvider = deniedServices.BuildServiceProvider();
        var denied = await deniedProvider.GetRequiredService<IContentCommitmentService>()
            .ComputeAsync(new("content-v1", 1), payload, CancellationToken.None);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ContentCommitmentFailure.ProviderFailure, denied.Failure);

        await bao.StopAsync();
        var unavailable = await provider.GetRequiredService<IContentCommitmentService>()
            .ComputeAsync(new("content-v1", 1), payload, CancellationToken.None);
        Assert.False(unavailable.IsSuccess);
        Assert.Equal(ContentCommitmentFailure.ProviderFailure, unavailable.Failure);
    }

    [Fact]
    public async Task Real_TLS_Raft_AppRole_Transit_survives_restart_and_closes_the_Transit_to_journal_crash_window()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_real_transit");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        await using var bao = await OpenBaoRealHarness.StartAsync();
        var options = new OpenBaoKekOptions(
            bao.Address, null, $"file:{bao.RoleIdPath}", $"file:{bao.SecretIdPath}", bao.CaPath,
            "transit", "raw-export", 1, bao.KeyFingerprint, TimeSpan.FromSeconds(5));

        var primary = await PrepareOpaqueAsync(database, options.Reference);
        await using var db = database.CreateDbContext();
        var journal = new PostgresOpenBaoKekJournal(db);
        using var transport = new OpenBaoHttpTransport(options);
        var session = new OpenBaoTokenSession(transport, options);
        var provider = new OpenBaoTransitKekOperationProvider(options, transport, session, journal);
        await provider.ValidateAsync(CancellationToken.None);

        var plaintext = RandomNumberGenerator.GetBytes(32);
        using var candidate = AttemptDekLease.CreateOwned(plaintext.ToArray());
        var wrapOutcome = await provider.WrapDekAsync(
            options.Reference, primary.Token, primary.Context, candidate, CancellationToken.None);
        if (wrapOutcome is not KekWrapResult.Wrapped)
        {
            var diagnostic = await journal.ReadAsync(primary.Token, primary.Context, CancellationToken.None);
            throw new InvalidOperationException(
                $"REAL_OPENBAO_WRAP_NOT_DURABLE:{wrapOutcome.GetType().Name}:{diagnostic?.JournalState}:{diagnostic?.RowRevision}");
        }
        var wrappedResult = (KekWrapResult.Wrapped)wrapOutcome;
        var wrapped = Assert.IsType<OpaqueProviderWrappedMaterial>(wrappedResult.Material);
        Assert.StartsWith("vault:v", Encoding.UTF8.GetString(wrapped.OpaquePayload), StringComparison.Ordinal);
        using (var lease = await provider.UnwrapDekAsync(options.Reference, wrapped, primary.Context,
                   CancellationToken.None))
            Assert.Equal(plaintext, lease.Material.ToArray());

        var wrongContextFailure = await Assert.ThrowsAsync<OpenBaoTransportException>(async () =>
        {
            using var _ = await provider.UnwrapDekAsync(options.Reference, wrapped,
                RandomNumberGenerator.GetBytes(32), CancellationToken.None);
        });
        Assert.Equal(400, wrongContextFailure.StatusCode);
        Assert.False(wrongContextFailure.IsTransient);
        var tamperedBytes = wrapped.OpaquePayload.ToArray();
        tamperedBytes[^1] ^= 0x01;
        var tampered = wrapped with { OpaquePayload = tamperedBytes };
        var tamperedFailure = await Assert.ThrowsAsync<OpenBaoTransportException>(async () =>
        {
            using var _ = await provider.UnwrapDekAsync(options.Reference, tampered, primary.Context,
                CancellationToken.None);
        });
        Assert.Equal(400, tamperedFailure.StatusCode);
        Assert.False(tamperedFailure.IsTransient);
        await Assert.ThrowsAsync<CryptographicException>(async () =>
        {
            using var _ = await provider.UnwrapDekAsync(options.Reference with { KekVersion = 2 }, wrapped,
                primary.Context, CancellationToken.None);
        });

        // AppRole tokens are deliberately short. Waiting beyond the local usable window proves re-login.
        await Task.Delay(TimeSpan.FromSeconds(3));
        using (var renewed = await provider.UnwrapDekAsync(options.Reference, wrapped, primary.Context,
                   CancellationToken.None))
            Assert.Equal(plaintext, renewed.Material.ToArray());

        await bao.RestartAndUnsealAsync();
        using (var restartTransport = new OpenBaoHttpTransport(options))
        {
            var restartProvider = new OpenBaoTransitKekOperationProvider(options, restartTransport,
                new OpenBaoTokenSession(restartTransport, options), journal);
            await restartProvider.ValidateAsync(CancellationToken.None);
            using var afterRestart = await restartProvider.UnwrapDekAsync(options.Reference, wrapped,
                primary.Context, CancellationToken.None);
            Assert.Equal(plaintext, afterRestart.Material.ToArray());
        }

        // A completed reservation is an idempotent replay boundary. The second full provisioning call
        // must be satisfied by the durable journal even while Transit is unavailable.
        var replayRequest = await CreateProvisioningRequestAsync(database, options.Reference);
        var replayProvisioner = new PostgresAttemptKeyReservationProvider(
            db, new PostgresKeyProviderOperationMap(db), provider,
            new OpenBaoKekWrappedMaterialProfileSource());
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated,
            (await replayProvisioner.ProvisionAsync(replayRequest, CancellationToken.None)).Outcome);
        var beforeReplay = await ReadReplayStateAsync(database, replayRequest.AttemptKeyReservationId);
        await bao.StopAsync();
        var replayObserver = new LookupOnlyReplayKekProvider(provider);
        var observedReplayProvisioner = new PostgresAttemptKeyReservationProvider(
            db, new PostgresKeyProviderOperationMap(db), replayObserver,
            new OpenBaoKekWrappedMaterialProfileSource());
        Assert.Equal(AttemptKeyProvisioningOutcome.Activated,
            (await observedReplayProvisioner.ProvisionAsync(replayRequest, CancellationToken.None)).Outcome);
        Assert.Equal(1, replayObserver.LookupCalls);
        Assert.Equal(0, replayObserver.WrapCalls);
        var afterReplay = await ReadReplayStateAsync(database, replayRequest.AttemptKeyReservationId);
        Assert.Equal(beforeReplay, afterReplay);
        await bao.StartAndUnsealAsync();

        // Persistent authorization denial survives the provider's single re-login attempt and is
        // classified as unavailable, never as corruption.
        var denied = await PrepareOpaqueAsync(database, options.Reference);
        await bao.SetTransitPolicyAsync(enabled: false);
        session.Invalidate();
        using (var deniedCandidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32)))
            Assert.IsType<KekWrapResult.Unavailable>(await provider.WrapDekAsync(
                options.Reference, denied.Token, denied.Context, deniedCandidate, CancellationToken.None));
        await bao.SetTransitPolicyAsync(enabled: true);
        session.Invalidate();

        // Real Transit accepts a result, then the process/provider disappears before Wrapped is committed.
        var crash = await PrepareOpaqueAsync(database, options.Reference);
        var crashIssued = await journal.IssueAsync(crash.Token, crash.Context, options.Reference,
            OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None);
        Assert.NotNull(crashIssued);
        var crashPlaintext = RandomNumberGenerator.GetBytes(32);
        var aad = OpenBaoTransitKekOperationProvider.ComputeAad(options.Reference, crash.Context);
        var appToken = await session.GetTokenAsync(CancellationToken.None);
        using var accepted = await transport.PostAsync("/v1/transit/encrypt/raw-export", new
        {
            plaintext = Convert.ToBase64String(crashPlaintext),
            associated_data = Convert.ToBase64String(aad),
            key_version = 1,
        }, appToken, CancellationToken.None);
        var lateCiphertext = accepted.RootElement.GetProperty("data").GetProperty("ciphertext").GetString();
        Assert.StartsWith("vault:v", lateCiphertext, StringComparison.Ordinal);
        await bao.StopAsync();
        Assert.Equal("Issued", (await journal.ReadAsync(crash.Token, crash.Context,
            CancellationToken.None))?.JournalState);

        var unavailable = await PrepareOpaqueAsync(database, options.Reference);
        using (var unavailableCandidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32)))
            Assert.IsType<KekWrapResult.Unavailable>(await provider.WrapDekAsync(options.Reference,
                unavailable.Token, unavailable.Context, unavailableCandidate, CancellationToken.None));

        await bao.StartAndUnsealAsync();
        await ExpirePreparationAsync(database, crash);
        var absence = await journal.ProveAbsenceAsync(crash.Token, crash.Context, crashIssued.RowRevision,
            CancellationToken.None);
        Assert.Equal("AbsenceProven", absence?.JournalState);
        Assert.Null(await journal.RecordWrappedAsync(crash.Token, crash.Context, crashIssued.RowRevision,
            Encoding.UTF8.GetBytes(lateCiphertext!), "openbao-transit:raw-export:1", CancellationToken.None));

        await bao.DeleteTransitKeyAsync();
        var deletedKeyFailure = await Assert.ThrowsAsync<OpenBaoTransportException>(async () =>
        {
            using var _ = await provider.UnwrapDekAsync(options.Reference, wrapped, primary.Context,
                CancellationToken.None);
        });
        Assert.Equal(400, deletedKeyFailure.StatusCode);
        Assert.False(deletedKeyFailure.IsTransient);
    }

    private static async Task ExpirePreparationAsync(
        PostgresPersistenceFixture database,
        OpaquePreparation prepared)
    {
        await using var db = database.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_raw_export_deployer");
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tagekyc.raw_export_attempt_key_reservations
            SET "CurrentPreparationLeaseExpiresAtUtc"=clock_timestamp()-interval '1 hour',
                "UpdatedAtUtc"=clock_timestamp()
            WHERE "AttemptKeyReservationId"={prepared.ReservationId}
            """);
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',false)");
        await db.Database.ExecuteSqlRawAsync("RESET ROLE");
        Assert.Equal("Expired", await db.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_mark_attempt_key_preparation_expired(
              {prepared.ReservationId},{prepared.PreparationId},{prepared.Fence}) AS "Value"
            """).SingleAsync());
    }

    private static void ConfigureClaimProvider(
        ConfigurationManager configuration,
        string section,
        OpenBaoRealHarness bao,
        string roleIdPath,
        string secretIdPath)
    {
        var prefix = $"{OpenBaoProductionClaimProviderTests.Root}:{section}";
        configuration[$"{prefix}:Address"] = bao.Address.AbsoluteUri;
        configuration[$"{prefix}:RoleIdSecretRef"] = $"file:{roleIdPath}";
        configuration[$"{prefix}:SecretIdSecretRef"] = $"file:{secretIdPath}";
        configuration[$"{prefix}:CaCertificatePath"] = bao.CaPath;
        configuration[$"{prefix}:TransitMount"] = "transit";
        configuration[$"{prefix}:RequestTimeoutSeconds"] = "5";
    }

    private static async Task<OpaquePreparation> PrepareOpaqueAsync(
        PostgresPersistenceFixture database,
        KekReference reference)
    {
        var fixture = await new Tip88C1B2CoreTests(database).SeedCandidateAsync();
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        var services = new ServiceCollection();
        services.AddScoped(_ => database.CreateDbContext());
        services.AddSingleton<ICustodyProfileProvider>(new OpenBaoTestCustodyProfileProvider(reference));
        services.AddTagEkycRawExportSourceClaimComparison(configuration);
        await using (var provider = services.BuildServiceProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteNewCandidateAsync(fixture.Command, CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.NewReservation, result.Outcome);
        }
        await using var db = database.CreateDbContext();
        var source = await db.RawExportSourceEncryptionAttempts
            .OrderByDescending(row => row.CreatedAtUtc)
            .Select(row => new { row.AttemptKeyReservationId, row.AttemptId, row.SourceArtifactId })
            .FirstAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(
              @reservation,@attempt,@source,'OPAQUE_PROVIDER_CIPHERTEXT',1,
              'OPENBAO_TRANSIT_AES_GCM',1)
            """;
        command.Parameters.AddWithValue("reservation", source.AttemptKeyReservationId);
        command.Parameters.AddWithValue("attempt", source.AttemptId);
        command.Parameters.AddWithValue("source", source.SourceArtifactId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("PreparingLive", reader.GetString(0));
        return new(source.AttemptKeyReservationId, reader.GetGuid(2), reader.GetInt64(3),
            new ProviderOperationToken(reader.GetString(4)), (byte[])reader[6]);
    }

    private static async Task<AttemptKeyProvisioningRequest> CreateProvisioningRequestAsync(
        PostgresPersistenceFixture database,
        KekReference reference)
    {
        var fixture = await new Tip88C1B2CoreTests(database).SeedCandidateAsync();
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        var services = new ServiceCollection();
        services.AddScoped(_ => database.CreateDbContext());
        services.AddSingleton<ICustodyProfileProvider>(new OpenBaoTestCustodyProfileProvider(reference));
        services.AddTagEkycRawExportSourceClaimComparison(configuration);
        await using (var serviceProvider = services.BuildServiceProvider())
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteNewCandidateAsync(fixture.Command, CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.NewReservation, result.Outcome);
        }
        await using var db = database.CreateDbContext();
        return await db.RawExportSourceEncryptionAttempts
            .OrderByDescending(row => row.CreatedAtUtc)
            .Select(row => new AttemptKeyProvisioningRequest(
                row.AttemptKeyReservationId, row.AttemptId, row.SourceArtifactId))
            .FirstAsync();
    }

    private static async Task<ReplayState> ReadReplayStateAsync(
        PostgresPersistenceFixture database,
        Guid reservationId)
    {
        await using var db = database.CreateDbContext();
        var operation = await db.RawExportKeyProviderOperations
            .Where(row => row.AttemptKeyReservationId == reservationId)
            .Select(row => new { row.ProviderOperationId, row.ProviderOperationToken })
            .SingleAsync();
        var journal = await db.RawExportOpenBaoKekOperationJournal
            .Where(row => row.ProviderOperationId == operation.ProviderOperationId)
            .Select(row => new { row.RowRevision, row.OpaqueWrappedDekPayload })
            .SingleAsync();
        var reservationRevision = await db.RawExportAttemptKeyReservations
            .Where(row => row.AttemptKeyReservationId == reservationId)
            .Select(row => row.RowRevision)
            .SingleAsync();
        return new(operation.ProviderOperationId, operation.ProviderOperationToken,
            journal.RowRevision, Convert.ToHexString(journal.OpaqueWrappedDekPayload!), reservationRevision);
    }

    private sealed record OpaquePreparation(Guid ReservationId, Guid PreparationId, long Fence,
        ProviderOperationToken Token, byte[] Context);

    private sealed record ReplayState(
        Guid ProviderOperationId,
        string ProviderOperationToken,
        long JournalRevision,
        string OpaquePayloadHex,
        long ReservationRevision);

    private sealed class OpenBaoTestCustodyProfileProvider(KekReference reference) : ICustodyProfileProvider
    {
        public SourceEncryptionProfileBundle ActiveSourceEncryptionProfile { get; } =
            new FixtureSourceEncryptionProfileCatalog().GetActive();
        public KekReferenceBundle ActiveKekReference { get; } =
            new(reference.KeyProviderId, reference.KekId, reference.KekVersion, reference.KekFingerprint);
        public CustodyTimeBounds TimeBounds { get; } = new(
            TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5));
    }

    private sealed class LookupOnlyReplayKekProvider(IKekOperationProvider inner) : IKekOperationProvider
    {
        internal int WrapCalls { get; private set; }
        internal int LookupCalls { get; private set; }

        public Task<KekWrapResult> WrapDekAsync(KekReference reference,
            ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            IAttemptDekCandidate candidate,
            CancellationToken cancellationToken)
        {
            WrapCalls++;
            throw new InvalidOperationException("ACTIVE_REPLAY_MUST_NOT_WRAP_OR_CREATE_A_DEK_CANDIDATE");
        }

        public Task<KekOperationLookup> LookupByOperationTokenAsync(
            ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken)
        {
            LookupCalls++;
            return inner.LookupByOperationTokenAsync(providerOperationToken,
                attemptKeyContextFingerprint, cancellationToken);
        }

        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference,
            KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) =>
            inner.UnwrapDekAsync(reference, wrapped, attemptKeyContextFingerprint, cancellationToken);
    }

}

internal sealed class OpenBaoRealHarness : IAsyncDisposable
{
    private const string Image = "ghcr.io/openbao/openbao@sha256:15e90b578c970ae57b596ed51295380cd54f93860fe36758f05b455d71aae0e0";
    private readonly string root;
    private readonly string project;
    private readonly string container;
    private HttpClient rootClient;
    private string rootToken = string.Empty;
    private string unsealKey = string.Empty;

    private OpenBaoRealHarness(string root, string project, Uri address, string caPath,
        string roleIdPath, string secretIdPath, HttpClient rootClient)
    {
        this.root = root;
        this.project = project;
        container = $"{project}-openbao-1";
        Address = address;
        CaPath = caPath;
        RoleIdPath = roleIdPath;
        SecretIdPath = secretIdPath;
        this.rootClient = rootClient;
    }

    internal Uri Address { get; }
    internal string CaPath { get; }
    internal string RoleIdPath { get; }
    internal string SecretIdPath { get; }
    internal string ContentRoleIdPath => Path.Combine(root, "content-role-id");
    internal string ContentSecretIdPath => Path.Combine(root, "content-secret-id");
    internal string SubjectRoleIdPath => Path.Combine(root, "subject-role-id");
    internal string SubjectSecretIdPath => Path.Combine(root, "subject-secret-id");
    internal string KeyFingerprint { get; private set; } = string.Empty;

    internal static async Task<OpenBaoRealHarness> StartAsync()
    {
        await RunAsync(Environment.CurrentDirectory, "docker", ["info"]);
        await RunAsync(Environment.CurrentDirectory, "docker", ["image", "inspect", Image]);
        var root = Path.Combine(Path.GetTempPath(), $"tagekyc-openbao-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var tls = Path.Combine(root, "tls");
        Directory.CreateDirectory(tls);
        CreateTlsMaterials(tls);
        var hostPort = ReserveLoopbackPort();
        await File.WriteAllTextAsync(Path.Combine(root, "openbao.hcl"), """
            ui = false
            storage "raft" { path = "/openbao/file" node_id = "tagekyc-test-1" }
            listener "tcp" {
              address = "0.0.0.0:8200"
              cluster_address = "0.0.0.0:8201"
              tls_cert_file = "/openbao/tls/server.crt"
              tls_key_file = "/openbao/tls/server.key"
              tls_min_version = "tls12"
              tls_max_version = "tls13"
              tls_disable_client_certs = true
            }
            api_addr = "https://openbao:8200"
            cluster_addr = "https://openbao:8201"
            """);
        var normalized = root.Replace('\\', '/');
        await File.WriteAllTextAsync(Path.Combine(root, "compose.yml"), $$"""
            services:
              openbao:
                image: {{Image}}
                pull_policy: never
                entrypoint: ["/usr/bin/dumb-init", "--", "/usr/bin/bao"]
                command: ["server", "-config=/openbao/config/openbao.hcl"]
                user: "100:49000"
                read_only: true
                cap_drop: ["ALL"]
                security_opt: ["no-new-privileges:true"]
                tmpfs: ["/tmp", "/openbao/logs"]
                ports: ["127.0.0.1:{{hostPort}}:8200"]
                volumes:
                  - openbao_raft:/openbao/file
                  - "{{normalized}}/openbao.hcl:/openbao/config/openbao.hcl:ro"
                  - "{{normalized}}/tls:/openbao/tls:ro"
            volumes:
              openbao_raft: {}
            """);
        var project = $"tagekyc-kek-{Guid.NewGuid():N}"[..31];
        try
        {
            await ComposeAsync(root, project, ["up", "-d", "--no-build"]);
            var address = new Uri($"https://localhost:{hostPort}/");
            var caPath = Path.Combine(tls, "ca.crt");
            var client = CreateTlsClient(address, caPath);
            var harness = new OpenBaoRealHarness(root, project, address, caPath,
                Path.Combine(root, "role-id"), Path.Combine(root, "secret-id"), client);
            await harness.WaitForEndpointAsync();
            await harness.InitializeAsync();
            return harness;
        }
        catch
        {
            await ComposeAsync(root, project, ["down", "-v", "--remove-orphans"], true);
            try { Directory.Delete(root, true); } catch { }
            throw;
        }
    }

    internal async Task StopAsync() =>
        await RunAsync(root, "docker", ["stop", "--time", "5", container]);

    internal async Task StartAndUnsealAsync()
    {
        await RunAsync(root, "docker", ["start", container]);
        ResetRootClient();
        await WaitForEndpointAsync();
        await PostAsync("/v1/sys/unseal", new { key = unsealKey });
        await WaitForReadyAsync();
    }

    internal async Task RestartAndUnsealAsync()
    {
        await RunAsync(root, "docker", ["restart", container]);
        ResetRootClient();
        try
        {
            await WaitForEndpointAsync();
        }
        catch (Exception exception)
        {
            var state = await RunAsync(root, "docker",
                ["inspect", "--format", "{{.State.Status}} {{.State.ExitCode}} {{.State.Error}}", container], true);
            var logs = await RunAsync(root, "docker", ["logs", "--tail", "80", container], true);
            var port = await RunAsync(root, "docker", ["port", container, "8200/tcp"], true);
            var curl = await RunAsync(root, "curl.exe",
                ["--silent", "--show-error", "--max-time", "5", "--cacert", CaPath,
                    new Uri(Address, "/v1/sys/health").AbsoluteUri], true);
            var internalStatus = await RunAsync(root, "docker",
                ["exec", container, "/usr/bin/bao", "status", "-address=https://127.0.0.1:8200",
                    "-tls-skip-verify", "-format=json"], true);
            throw new InvalidOperationException(
                $"OPENBAO_RESTART_NOT_REACHABLE:{state}:PORT={port}:CURL={curl}:INTERNAL={internalStatus}:LOGS={logs}", exception);
        }
        await PostAsync("/v1/sys/unseal", new { key = unsealKey });
        await WaitForReadyAsync();
    }

    internal async Task DeleteTransitKeyAsync()
    {
        await RootPostAsync("/v1/transit/keys/raw-export/config", new { deletion_allowed = true });
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/v1/transit/keys/raw-export");
        request.Headers.TryAddWithoutValidation("X-Vault-Token", rootToken);
        using var response = await rootClient.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    internal Task SetTransitPolicyAsync(bool enabled) =>
        RootPostAsync("/v1/sys/policies/acl/tagekyc-raw-export", new
        {
            policy = enabled
                ? "path \"transit/encrypt/raw-export\" { capabilities=[\"update\"] }\n" +
                  "path \"transit/decrypt/raw-export\" { capabilities=[\"update\"] }\n" +
                  "path \"transit/keys/raw-export\" { capabilities=[\"read\"] }"
                : "path \"transit/*\" { capabilities=[\"deny\"] }",
        });

    internal Task SetContentClaimPolicyAsync(bool includeSubjectKey) =>
        RootPostAsync("/v1/sys/policies/acl/tagekyc-content-commitment", new
        {
            policy = "path \"transit/hmac/content-hmac/*\" { capabilities=[\"update\"] }\n" +
                     "path \"transit/keys/content-hmac\" { capabilities=[\"read\"] }" +
                     (includeSubjectKey
                         ? "\npath \"transit/hmac/subject-hmac/*\" { capabilities=[\"update\"] }"
                         : string.Empty),
        });

    internal async Task<byte[]> RootHmacAsync(
        string keyName,
        byte[] payload,
        int keyVersion)
    {
        using var response = await RootPostAsync(
            $"/v1/transit/hmac/{Uri.EscapeDataString(keyName)}/sha2-256",
            new { input = Convert.ToBase64String(payload), key_version = keyVersion });
        return OpenBaoTransitHmacClient.ParseHmac(
            response.RootElement.GetProperty("data").GetProperty("hmac").GetString(),
            keyVersion);
    }

    internal Task<JsonDocument> RootKeyMetadataAsync(string keyName) =>
        RootGetAsync($"/v1/transit/keys/{Uri.EscapeDataString(keyName)}");

    internal async Task<bool> RootVerifyHmacAsync(
        string keyName,
        byte[] payload,
        byte[] hmac,
        int keyVersion)
    {
        using var response = await RootPostAsync(
            $"/v1/transit/verify/{Uri.EscapeDataString(keyName)}/sha2-256",
            new
            {
                input = Convert.ToBase64String(payload),
                hmac = $"vault:v{keyVersion}:{Convert.ToBase64String(hmac)}",
            });
        return response.RootElement.GetProperty("data").GetProperty("valid").GetBoolean();
    }

    internal async Task<bool> AppRoleCanHmacAsync(
        string roleIdPath,
        string secretIdPath,
        string keyName,
        byte[] payload)
    {
        using var login = await PostAsync("/v1/auth/approle/login", new
        {
            role_id = (await File.ReadAllTextAsync(roleIdPath)).Trim(),
            secret_id = (await File.ReadAllTextAsync(secretIdPath)).Trim(),
        });
        var token = login.RootElement.GetProperty("auth").GetProperty("client_token").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/v1/transit/hmac/{Uri.EscapeDataString(keyName)}/sha2-256")
        {
            Content = JsonContent.Create(new
            {
                input = Convert.ToBase64String(payload),
                key_version = 1,
            }),
        };
        request.Headers.TryAddWithoutValidation("X-Vault-Token", token);
        using var response = await rootClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private async Task InitializeAsync()
    {
        using (var initialized = await PostAsync("/v1/sys/init", new { secret_shares = 1, secret_threshold = 1 }))
        {
            rootToken = initialized.RootElement.GetProperty("root_token").GetString()!;
            unsealKey = initialized.RootElement.GetProperty("keys_base64")[0].GetString()!;
        }
        await PostAsync("/v1/sys/unseal", new { key = unsealKey });
        await WaitForReadyAsync();
        await RootPostAsync("/v1/sys/mounts/transit", new { type = "transit" });
        await RootPostAsync("/v1/transit/keys/raw-export", new
        {
            type = "aes256-gcm96", derived = false, exportable = false, allow_plaintext_backup = false,
        });
        using (var metadata = await RootGetAsync("/v1/transit/keys/raw-export"))
            KeyFingerprint = OpenBaoTransitKekOperationProvider.ComputeKeyFingerprint(
                metadata.RootElement.GetProperty("data"), 1);
        await RootPostAsync("/v1/transit/keys/content-hmac", new
        {
            type = "hmac", derived = false, exportable = false,
            allow_plaintext_backup = false, key_size = 32,
        });
        await RootPostAsync("/v1/transit/keys/subject-hmac", new
        {
            type = "hmac", derived = false, exportable = false,
            allow_plaintext_backup = false, key_size = 32,
        });
        await RootPostAsync("/v1/sys/policies/acl/tagekyc-raw-export", new
        {
            policy = "path \"transit/encrypt/raw-export\" { capabilities=[\"update\"] }\n" +
                     "path \"transit/decrypt/raw-export\" { capabilities=[\"update\"] }\n" +
                     "path \"transit/keys/raw-export\" { capabilities=[\"read\"] }",
        });
        await RootPostAsync("/v1/sys/auth/approle", new { type = "approle" });
        await RootPostAsync("/v1/auth/approle/role/tagekyc-raw-export", new
        {
            token_policies = new[] { "tagekyc-raw-export" }, token_ttl = "4s", token_max_ttl = "4s",
            secret_id_num_uses = 0, secret_id_ttl = "20m",
        });
        using (var role = await RootGetAsync("/v1/auth/approle/role/tagekyc-raw-export/role-id"))
            await File.WriteAllTextAsync(RoleIdPath,
                role.RootElement.GetProperty("data").GetProperty("role_id").GetString()! + "\n");
        using (var secret = await RootPostAsync("/v1/auth/approle/role/tagekyc-raw-export/secret-id", new { }))
            await File.WriteAllTextAsync(SecretIdPath,
                secret.RootElement.GetProperty("data").GetProperty("secret_id").GetString()! + "\n");
        await CreateClaimRoleAsync(
            "tagekyc-content-commitment",
            "tagekyc-content-commitment",
            "content-hmac",
            ContentRoleIdPath,
            ContentSecretIdPath);
        await CreateClaimRoleAsync(
            "tagekyc-subject-ref-token",
            "tagekyc-subject-ref-token",
            "subject-hmac",
            SubjectRoleIdPath,
            SubjectSecretIdPath);
    }

    private async Task CreateClaimRoleAsync(
        string policyName,
        string roleName,
        string keyName,
        string roleIdPath,
        string secretIdPath)
    {
        await RootPostAsync($"/v1/sys/policies/acl/{policyName}", new
        {
            policy = $"path \"transit/hmac/{keyName}/*\" {{ capabilities=[\"update\"] }}\n" +
                     $"path \"transit/keys/{keyName}\" {{ capabilities=[\"read\"] }}",
        });
        await RootPostAsync($"/v1/auth/approle/role/{roleName}", new
        {
            token_policies = new[] { policyName }, token_ttl = "4s", token_max_ttl = "4s",
            secret_id_num_uses = 0, secret_id_ttl = "20m",
        });
        using (var role = await RootGetAsync($"/v1/auth/approle/role/{roleName}/role-id"))
            await File.WriteAllTextAsync(roleIdPath,
                role.RootElement.GetProperty("data").GetProperty("role_id").GetString()! + "\n");
        using (var secret = await RootPostAsync($"/v1/auth/approle/role/{roleName}/secret-id", new { }))
            await File.WriteAllTextAsync(secretIdPath,
                secret.RootElement.GetProperty("data").GetProperty("secret_id").GetString()! + "\n");
    }

    private async Task<JsonDocument> RootGetAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("X-Vault-Token", rootToken);
        using var response = await rootClient.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
    }

    private async Task<JsonDocument> RootPostAsync(string path, object value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(value) };
        request.Headers.TryAddWithoutValidation("X-Vault-Token", rootToken);
        using var response = await rootClient.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return bytes.Length == 0 ? JsonDocument.Parse("{}") : JsonDocument.Parse(bytes);
    }

    private async Task<JsonDocument> PostAsync(string path, object value)
    {
        using var response = await rootClient.PostAsJsonAsync(path, value);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
    }

    private async Task WaitForEndpointAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        while (true)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            attempt.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                using var response = await rootClient.GetAsync("/v1/sys/health", attempt.Token);
                if ((int)response.StatusCode is 200 or 429 or 472 or 473 or 501 or 503) return;
            }
            catch when (!timeout.IsCancellationRequested) { }
            await Task.Delay(250, timeout.Token);
        }
    }

    private async Task WaitForReadyAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            using var response = await rootClient.GetAsync("/v1/sys/health", timeout.Token);
            if (response.StatusCode == HttpStatusCode.OK) return;
            await Task.Delay(200, timeout.Token);
        }
    }

    private static HttpClient CreateTlsClient(Uri address, string caPath)
    {
        var root = X509Certificate2.CreateFromPem(File.ReadAllText(caPath));
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, chain, errors) =>
            {
                if (certificate is null || (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                    return false;
                using var custom = new X509Chain();
                custom.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                custom.ChainPolicy.CustomTrustStore.Add(root);
                custom.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return custom.Build(new X509Certificate2(certificate));
            },
        };
        return new HttpClient(handler) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(30) };
    }

    private static int ReserveLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private void ResetRootClient()
    {
        rootClient.Dispose();
        rootClient = CreateTlsClient(Address, CaPath);
    }

    private static void CreateTlsMaterials(string directory)
    {
        var now = DateTimeOffset.UtcNow;
        using var caKey = RSA.Create(3072);
        var caRequest = new CertificateRequest("CN=TagEkyc OpenBao Test CA", caKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using var ca = caRequest.CreateSelfSigned(now.AddHours(-1), now.AddDays(2));
        using var serverKey = RSA.Create(3072);
        var request = new CertificateRequest("CN=localhost", serverKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") }, true));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddDnsName("openbao");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        using var signed = request.Create(ca, now.AddHours(-1), now.AddDays(1),
            RandomNumberGenerator.GetBytes(16));
        using var server = signed.CopyWithPrivateKey(serverKey);
        File.WriteAllText(Path.Combine(directory, "ca.crt"), ca.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "server.crt"), server.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "server.key"), serverKey.ExportPkcs8PrivateKeyPem());
    }

    private static Task<string> ComposeAsync(string root, string project, string[] arguments,
        bool allowFailure = false) =>
        RunAsync(root, "docker", ["compose", "--project-name", project, "--file",
            Path.Combine(root, "compose.yml"), .. arguments], allowFailure);

    private static async Task<string> RunAsync(string workingDirectory, string fileName,
        string[] arguments, bool allowFailure = false)
    {
        var start = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("PROCESS_START_FAILED");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = (await stdout).Trim();
        var error = (await stderr).Trim();
        if (!allowFailure && process.ExitCode != 0)
            throw new InvalidOperationException($"COMMAND_FAILED:{fileName}:{process.ExitCode}:{error}");
        return allowFailure ? $"exit={process.ExitCode};stdout={output};stderr={error}" : output;
    }

    public async ValueTask DisposeAsync()
    {
        rootClient.Dispose();
        await ComposeAsync(root, project, ["down", "-v", "--remove-orphans"], true);
        try { Directory.Delete(root, true); } catch { }
    }
}
