using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class PostgresPersistenceSliceTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task S1_flow_persists_idempotently_and_survives_service_restart()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-happy");
        var artifact = await AppendArtifactAsync(provider, session.VerificationSessionId);
        await AppendEvidenceAsync(provider, session.VerificationSessionId, artifact.CaptureArtifactId);

        var completed = await CompleteAsync(provider, session.VerificationSessionId);
        var second = await CompleteAsync(provider, session.VerificationSessionId);

        Assert.Equal(completed.EvidencePackageId, second.EvidencePackageId);
        Assert.Equal(completed.EvidencePackageHash, second.EvidencePackageHash);
        await AssertPersistedCountsAsync(decisions: 1, packages: 1, manifests: 1, completionAudits: 1);

        await using var restarted = BuildProvider();
        await using var scope = restarted.CreateAsyncScope();
        var summary = await scope.ServiceProvider.GetRequiredService<VerificationSessionApplicationService>()
            .GetSummaryAsync(BusinessCaller(), session.VerificationSessionId, CancellationToken.None);
        var package = await scope.ServiceProvider.GetRequiredService<VerificationCompletionApplicationService>()
            .GetEvidencePackageAsync(BusinessCaller(), completed.EvidencePackageId, CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.True(package.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Completed, summary.Value?.State);
        Assert.Equal(completed.EvidencePackageId, package.Value?.EvidencePackageId);
        Assert.Equal(completed.EvidencePackageHash, package.Value?.PackageHash);

        await using var db = postgres.CreateDbContext();
        var packageRow = await db.EvidencePackages.AsNoTracking().SingleAsync();
        var manifestRow = await db.EvidenceManifests.AsNoTracking().SingleAsync();
        Assert.Equal(EvidenceCanonicalization.PackageVersion, packageRow.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.CanonicalizationScheme, packageRow.CanonicalizationScheme);
        Assert.Equal(EvidenceCanonicalization.HashAlgorithm, packageRow.HashAlgorithm);
        Assert.Equal(EvidenceCanonicalization.PackageVersion, manifestRow.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.CanonicalizationScheme, manifestRow.CanonicalizationScheme);
        Assert.Equal(EvidenceCanonicalization.HashAlgorithm, manifestRow.HashAlgorithm);
        Assert.Equal("Signed", packageRow.EvidencePackageSignatureStatus);
        Assert.Equal("Signed", manifestRow.EvidencePackageSignatureStatus);
        Assert.Equal(EvidenceSignatureDefaults.FormatJws, manifestRow.SignatureFormat);
        Assert.Equal(EvidenceSignatureDefaults.SchemeJwsEs256V1, manifestRow.SignatureScheme);
        Assert.Equal(EvidenceSignatureDefaults.AlgorithmEs256, manifestRow.SignatureAlgorithm);
        Assert.False(string.IsNullOrWhiteSpace(manifestRow.KeyId));
        Assert.NotNull(manifestRow.SignedAt);
        Assert.False(string.IsNullOrWhiteSpace(manifestRow.SignatureValue));
        var signer = Assert.IsType<LocalDevEs256JwsEvidenceSigner>(provider.GetRequiredService<IEvidenceSigner>());
        using var publicKey = signer.ExportPublicKey();
        Assert.True(Tip66EvidenceSignatureTestVerifier.Verify(
            new EvidenceSignatureEnvelope(
                SignaturePlaceholderStatus.Signed,
                manifestRow.SignatureFormat!,
                manifestRow.SignatureScheme!,
                manifestRow.SignatureAlgorithm!,
                manifestRow.KeyId!,
                manifestRow.SignedAt!.Value,
                manifestRow.SignatureValue!),
            new EvidenceSignatureRequest(
                packageRow.Id.ToString("N"),
                manifestRow.ManifestHash,
                manifestRow.PackageVersion,
                manifestRow.CanonicalizationScheme,
                manifestRow.HashAlgorithm,
                EvidenceSignatureDefaults.PurposeEvidencePackageManifest),
            publicKey));
    }

    [Fact]
    public async Task Evidence_package_and_manifest_reads_fail_closed_for_unknown_hash_metadata()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-hash-metadata");
        var sessionId = Guid.Parse(session.VerificationSessionId);
        var now = DateTimeOffset.UtcNow;
        var currentPackageId = await InsertPackageAndManifestRowsAsync(
            sessionId,
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            now);
        var legacyPackageId = await InsertPackageAndManifestRowsAsync(
            sessionId,
            EvidenceCanonicalization.LegacyPackageVersion,
            EvidenceCanonicalization.LegacyCanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            now.AddSeconds(1));
        var unknownPackageId = await InsertPackageAndManifestRowsAsync(
            sessionId,
            EvidenceCanonicalization.PackageVersion,
            "bogus-scheme",
            EvidenceCanonicalization.HashAlgorithm,
            now.AddSeconds(2));

        await using var scope = provider.CreateAsyncScope();
        var packageRepository = scope.ServiceProvider.GetRequiredService<IEvidencePackageRepository>();
        var manifestRepository = scope.ServiceProvider.GetRequiredService<IInternalEvidenceManifestRepository>();

        var currentPackage = await packageRepository.GetAsync(currentPackageId, CancellationToken.None);
        var currentManifest = await manifestRepository.GetByPackageAsync(currentPackageId, CancellationToken.None);
        var legacyPackage = await packageRepository.GetAsync(legacyPackageId, CancellationToken.None);
        var legacyManifest = await manifestRepository.GetByPackageAsync(legacyPackageId, CancellationToken.None);

        Assert.NotNull(currentPackage);
        Assert.NotNull(currentManifest);
        Assert.Equal(EvidenceCanonicalization.PackageVersion, currentPackage.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.CanonicalizationScheme, currentManifest.CanonicalizationScheme);
        Assert.NotNull(legacyPackage);
        Assert.NotNull(legacyManifest);
        Assert.Equal(EvidenceCanonicalization.LegacyPackageVersion, legacyPackage.PackageVersion);
        Assert.Equal(EvidenceCanonicalization.LegacyCanonicalizationScheme, legacyManifest.CanonicalizationScheme);
        await Assert.ThrowsAsync<EvidenceHashMetadataException>(() =>
            packageRepository.GetAsync(unknownPackageId, CancellationToken.None));
        await Assert.ThrowsAsync<EvidenceHashMetadataException>(() =>
            packageRepository.GetBySessionAsync(sessionId, CancellationToken.None));
        await Assert.ThrowsAsync<EvidenceHashMetadataException>(() =>
            manifestRepository.GetByPackageAsync(unknownPackageId, CancellationToken.None));
    }

    [Fact]
    public async Task Legacy_placeholder_manifest_has_no_envelope_and_signed_without_envelope_fails_closed()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-signature-legacy");
        var sessionId = Guid.Parse(session.VerificationSessionId);
        var legacyPackageId = await InsertPackageAndManifestRowsAsync(
            sessionId,
            EvidenceCanonicalization.LegacyPackageVersion,
            EvidenceCanonicalization.LegacyCanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            DateTimeOffset.UtcNow);
        var invalidSignedPackageId = await InsertPackageAndManifestRowsAsync(
            sessionId,
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            DateTimeOffset.UtcNow.AddSeconds(1),
            signatureStatus: "Signed");

        await using var scope = provider.CreateAsyncScope();
        var manifestRepository = scope.ServiceProvider.GetRequiredService<IInternalEvidenceManifestRepository>();

        var legacyManifest = await manifestRepository.GetByPackageAsync(legacyPackageId, CancellationToken.None);

        Assert.NotNull(legacyManifest);
        Assert.Equal(SignaturePlaceholderStatusDto.PlaceholderUnverified, legacyManifest.EvidencePackageSignatureStatus);
        Assert.Null(legacyManifest.SignatureFormat);
        Assert.Null(legacyManifest.SignatureValue);
        await Assert.ThrowsAsync<EvidenceSignatureMetadataException>(() =>
            manifestRepository.GetByPackageAsync(invalidSignedPackageId, CancellationToken.None));
    }

    [Fact]
    public void Production_postgres_composition_does_not_register_fault_injector()
    {
        var services = new ServiceCollection();

        services.AddTagEkycPostgresPersistence(postgres.ConnectionString);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<EfPersistenceFaultInjector>());
    }

    [Fact]
    public async Task Verification_session_uses_xmin_as_concurrency_token()
    {
        await using var db = postgres.CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(VerificationSessionRow))
            ?? throw new InvalidOperationException("VerificationSessionRow model missing.");
        var token = entity.FindProperty("xmin");

        Assert.NotNull(token);
        Assert.True(token!.IsConcurrencyToken);
        Assert.True(token.IsShadowProperty());
        Assert.Equal(ValueGenerated.OnAddOrUpdate, token.ValueGenerated);
    }

    [Fact]
    public async Task Child_tables_reject_orphan_verification_session_ids()
    {
        await using var db = postgres.CreateDbContext();
        var missingSessionId = Guid.Parse("99999999-9999-5999-8999-999999999999");
        var now = DateTimeOffset.UtcNow;

        db.CaptureArtifacts.Add(new CaptureArtifactRow
        {
            Id = Guid.Parse("10000000-0000-5000-8000-000000000001"),
            VerificationSessionId = missingSessionId,
            ArtifactType = "DeviceCaptureMetadata",
            CaptureSource = "MobileSdk",
            QualityState = "Accepted",
            RequestId = "req-orphan-capture",
            CorrelationId = "corr-orphan-capture",
            CreatedAt = now,
        });
        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.EvidenceResults.Add(new EvidenceResultRow
        {
            Id = Guid.Parse("10000000-0000-5000-8000-000000000002"),
            VerificationSessionId = missingSessionId,
            ResultType = "CaptureQuality",
            InputCaptureArtifactIdsJson = "[]",
            Result = "Passed",
            ReasonCodesJson = "[]",
            PayloadSignatureStatus = "PlaceholderUnverified",
            EngineName = "localdev-quality",
            EngineVersion = "s1",
            RequestId = "req-orphan-evidence",
            CorrelationId = "corr-orphan-evidence",
            CreatedAt = now,
        });
        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.VerificationDecisions.Add(new VerificationDecisionRow
        {
            Id = Guid.Parse("10000000-0000-5000-8000-000000000003"),
            VerificationSessionId = missingSessionId,
            Result = "Passed",
            AssuranceLevel = "Medium",
            FailedChecksJson = "[]",
            CompletedChecksJson = """["CaptureQuality"]""",
            DecisionReasonCodesJson = "[]",
            RetryReasonCodesJson = "[]",
            CreatedAt = now,
        });
        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.EvidencePackages.Add(new EvidencePackageRow
        {
            Id = Guid.Parse("10000000-0000-5000-8000-000000000004"),
            VerificationSessionId = missingSessionId,
            PackageVersion = "test",
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            EvidenceRefsJson = "[]",
            AuditEventRefsJson = "[]",
            ResultRef = Guid.Parse("10000000-0000-5000-8000-000000000003"),
            PackageHash = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            EvidencePackageSignatureStatus = "PlaceholderUnverified",
            CreatedAt = now,
        });
        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.AuditEvents.Add(new AuditEventRow
        {
            Id = Guid.Parse("10000000-0000-5000-8000-000000000005"),
            ClientApplicationId = LocalDevRuntimePolicySource.BusinessClientId,
            VerificationSessionId = missingSessionId,
            ActorType = "BusinessConsumer",
            EventType = "VERIFICATION_COMPLETED",
            EventPayloadHash = "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            RequestId = "req-orphan-audit",
            CorrelationId = "corr-orphan-audit",
            OccurredAt = now,
        });
        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Evidence_manifest_rejects_orphan_evidence_package_id()
    {
        await using var db = postgres.CreateDbContext();

        db.EvidenceManifests.Add(new EvidenceManifestRow
        {
            EvidencePackageId = Guid.Parse("10000000-0000-5000-8000-000000000006"),
            SessionGuid = Guid.Parse("99999999-9999-5999-8999-999999999999"),
            VerificationSessionId = "99999999999959998999999999999999",
            PackageVersion = "test",
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            PackageHash = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            EvidenceRefsJson = "[]",
            AuditEventRefsJson = "[]",
            ResultRef = Guid.Parse("10000000-0000-5000-8000-000000000003"),
            EvidencePackageSignatureStatus = "PlaceholderUnverified",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await AssertForeignKeyViolationAsync(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Append_only_tables_reject_update_and_delete()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-append-only");
        var artifact = await AppendArtifactAsync(provider, session.VerificationSessionId);
        await AppendEvidenceAsync(provider, session.VerificationSessionId, artifact.CaptureArtifactId);

        await using var db = postgres.CreateDbContext();
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""UPDATE tagekyc.evidence_results SET "Result" = "Result";"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""DELETE FROM tagekyc.audit_events;"""));
    }

    [Fact]
    public async Task Concurrent_finalization_uses_xmin_conflict_without_partial_rows_or_pk_collision()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-concurrent-finalize");
        var artifact = await AppendArtifactAsync(provider, session.VerificationSessionId);
        await AppendEvidenceAsync(provider, session.VerificationSessionId, artifact.CaptureArtifactId);

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();
        var expected1 = await scope1.ServiceProvider.GetRequiredService<IVerificationSessionRepository>()
            .GetAsync(Guid.Parse(session.VerificationSessionId), CancellationToken.None)
            ?? throw new InvalidOperationException("First expected session missing.");
        var expected2 = await scope2.ServiceProvider.GetRequiredService<IVerificationSessionRepository>()
            .GetAsync(Guid.Parse(session.VerificationSessionId), CancellationToken.None)
            ?? throw new InvalidOperationException("Second expected session missing.");
        var gate = new TwoPartyAsyncGate();
        scope1.ServiceProvider.GetRequiredService<EfPersistenceFaultInjector>().BeforeFinalizationSaveAsync = gate.SignalAndWaitAsync;
        scope2.ServiceProvider.GetRequiredService<EfPersistenceFaultInjector>().BeforeFinalizationSaveAsync = gate.SignalAndWaitAsync;

        var task1 = scope1.ServiceProvider.GetRequiredService<IVerificationFinalizationBoundary>()
            .TryFinalizeAsync(CreateFinalizationWrite(expected1, "11111111-1111-5111-8111-111111111111"), CancellationToken.None);
        var task2 = scope2.ServiceProvider.GetRequiredService<IVerificationFinalizationBoundary>()
            .TryFinalizeAsync(CreateFinalizationWrite(expected2, "22222222-2222-5222-8222-222222222222"), CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);

        Assert.Single(results, result => result.Status == VerificationFinalizationWriteStatus.Applied);
        Assert.Single(results, result => result.Status == VerificationFinalizationWriteStatus.StateMismatch);
        await AssertPersistedCountsAsync(decisions: 1, packages: 1, manifests: 1, completionAudits: 1);
    }

    [Fact]
    public async Task Finalization_rollback_keeps_session_and_derived_records_consistent()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-rollback");
        var artifact = await AppendArtifactAsync(provider, session.VerificationSessionId);
        await AppendEvidenceAsync(provider, session.VerificationSessionId, artifact.CaptureArtifactId);

        await using (var scope = provider.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<EfPersistenceFaultInjector>().ThrowAfterSessionUpdateInFinalization = true;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scope.ServiceProvider.GetRequiredService<VerificationCompletionApplicationService>()
                    .CompleteAsync(BusinessCaller(), session.VerificationSessionId, new CompleteVerificationSessionRequestDto(), CancellationToken.None));
        }

        await using var checkScope = provider.CreateAsyncScope();
        var summary = await checkScope.ServiceProvider.GetRequiredService<VerificationSessionApplicationService>()
            .GetSummaryAsync(BusinessCaller(), session.VerificationSessionId, CancellationToken.None);

        Assert.Equal(VerificationSessionStateDto.ReadyToComplete, summary.Value?.State);
        Assert.Null(summary.Value?.EvidencePackageId);
        await AssertPersistedCountsAsync(decisions: 0, packages: 0, manifests: 0, completionAudits: 0);
    }

    [Fact]
    public async Task Stale_finalization_write_returns_state_mismatch()
    {
        await using var provider = BuildProvider();
        var session = await CreateCaptureQualitySessionAsync(provider, "external-pg-conflict");

        await using var scope = provider.CreateAsyncScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<IVerificationSessionRepository>();
        var boundary = scope.ServiceProvider.GetRequiredService<IVerificationFinalizationBoundary>();
        var expected = await sessionRepo.GetAsync(Guid.Parse(session.VerificationSessionId), CancellationToken.None)
            ?? throw new InvalidOperationException("Session missing.");
        var completed = expected.WithCompletion(
            VerificationResult.Passed,
            AssuranceLevel.Medium,
            Guid.Parse("aaaaaaaa-aaaa-5aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-5bbb-8bbb-bbbbbbbbbbbb"),
            new HashRef("sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            new HashRef("sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            "req-conflict",
            "corr-conflict",
            DateTimeOffset.UtcNow);

        await sessionRepo.SetStateAsync(expected.Id, VerificationSessionState.TechnicalTerminal, CancellationToken.None);

        var result = await boundary.TryFinalizeAsync(
            new VerificationFinalizationWrite(
                expected,
                completed,
                new VerificationDecision(
                    Guid.Parse("aaaaaaaa-aaaa-5aaa-8aaa-aaaaaaaaaaaa"),
                    expected.Id,
                    VerificationResult.Passed,
                    AssuranceLevel.Medium,
                    RiskScore: null,
                    FailedChecks: [],
                    CompletedChecks: [RequiredCheckType.CaptureQuality],
                    DecisionReasonCodes: ["ALL_REQUIRED_CHECKS_PASSED"],
                    RetryReasonCodes: [],
                    DateTimeOffset.UtcNow),
                new EvidencePackage(
                    Guid.Parse("bbbbbbbb-bbbb-5bbb-8bbb-bbbbbbbbbbbb"),
                    expected.Id,
                    "test",
                    EvidenceCanonicalization.CanonicalizationScheme,
                    EvidenceCanonicalization.HashAlgorithm,
                    new HashRef("sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                    [],
                    [],
                    Guid.Parse("aaaaaaaa-aaaa-5aaa-8aaa-aaaaaaaaaaaa"),
                    new HashRef("sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                    SignaturePlaceholderStatus.PlaceholderUnverified,
                    DateTimeOffset.UtcNow),
                new EvidenceManifestDto(
                    "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb",
                    expected.Id.ToString("N"),
                    "test",
                    EvidenceCanonicalization.CanonicalizationScheme,
                    EvidenceCanonicalization.HashAlgorithm,
                    "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    [],
                    [],
                    "aaaaaaaaaaaa5aaa8aaaaaaaaaaaaaaaa",
                    SignaturePlaceholderStatusDto.PlaceholderUnverified,
                    DateTimeOffset.UtcNow),
                new AuditEvent(
                    Guid.Parse("cccccccc-cccc-5ccc-8ccc-cccccccccccc"),
                    expected.ClientApplicationId,
                    expected.Id,
                    "BusinessConsumer",
                    "ldev_biz",
                    "VERIFICATION_COMPLETED",
                    new HashRef("sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
                    EventPayloadRef: null,
                    "req-conflict",
                    "corr-conflict",
                    DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.Equal(VerificationFinalizationWriteStatus.StateMismatch, result.Status);
    }

    [Fact]
    public void Production_inmemory_configuration_refuses_to_start()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Production");
                builder.UseSetting("TagEkyc:Persistence:Provider", "InMemory");
            });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Production requires TagEkyc:Persistence:Provider=Postgres", exception.ToString(), StringComparison.Ordinal);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<LocalDevRuntimePolicySource>();
        services.AddSingleton<ILocalDevClientPolicyProvider>(sp => sp.GetRequiredService<LocalDevRuntimePolicySource>());
        services.AddTagEkycPostgresPersistence(postgres.ConnectionString);
        services.AddSingleton<IEvidenceSigner, LocalDevEs256JwsEvidenceSigner>();
        services.AddScoped<EfPersistenceFaultInjector>();
        services.AddScoped<VerificationSessionApplicationService>();
        services.AddScoped<VerificationEvidenceApplicationService>();
        services.AddScoped<VerificationCompletionApplicationService>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }

    private static async Task<CreateVerificationSessionResponseDto> CreateCaptureQualitySessionAsync(
        IServiceProvider provider,
        string externalSessionId)
    {
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<VerificationSessionApplicationService>()
            .CreateAsync(
                BusinessCaller(),
                new CreateVerificationSessionRequestDto(
                    externalSessionId,
                    "subject-ref",
                    "PATIENT_REGISTRATION",
                    VerificationProfileDto.StandardEkycProfile,
                    [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, Required: true, MinimumConfidence: null)],
                    DateTimeOffset.UtcNow.AddMinutes(30),
                    RequestId: "req-create",
                    CorrelationId: "corr-create"),
                cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task<CaptureArtifactSubmissionResponseDto> AppendArtifactAsync(IServiceProvider provider, string sessionId)
    {
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<VerificationEvidenceApplicationService>()
            .AppendCaptureArtifactAsync(
                CaptureCaller(),
                sessionId,
                new CaptureArtifactSubmissionRequestDto(
                    CaptureArtifactTypeDto.DeviceCaptureMetadata,
                    CaptureSourceDto.MobileSdk,
                    "ldev_capture",
                    "device-1",
                    ArtifactHash: null,
                    MetadataHash: "sha256:metadata",
                    RequestId: "req-artifact",
                    CorrelationId: "corr-artifact"),
                CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private static async Task AppendEvidenceAsync(IServiceProvider provider, string sessionId, string artifactId)
    {
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<VerificationEvidenceApplicationService>()
            .AppendEvidenceResultAsync(
                TrustedCaller(),
                sessionId,
                new EvidenceResultSubmissionRequestDto(
                    EvidenceResultTypeDto.CaptureQuality,
                    [artifactId],
                    VerificationResultDto.Passed,
                    Confidence: 0.9m,
                    ReasonCodes: [],
                    RetryReasonCode: null,
                    SanitizedSummaryRef: "summary:capture-quality",
                    PayloadHash: "sha256:payload",
                    SignaturePlaceholderStatusDto.PlaceholderUnverified,
                    "localdev-quality",
                    "s1",
                    RequestId: "req-evidence",
                    CorrelationId: "corr-evidence"),
                CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    private static async Task<CompleteVerificationSessionResponseDto> CompleteAsync(IServiceProvider provider, string sessionId)
    {
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<VerificationCompletionApplicationService>()
            .CompleteAsync(
                BusinessCaller(),
                sessionId,
                new CompleteVerificationSessionRequestDto(
                    RequestId: "req-complete",
                    CorrelationId: "corr-complete"),
                CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value!;
    }

    private async Task AssertPersistedCountsAsync(int decisions, int packages, int manifests, int completionAudits)
    {
        await using var db = postgres.CreateDbContext();
        Assert.Equal(decisions, await db.VerificationDecisions.CountAsync());
        Assert.Equal(packages, await db.EvidencePackages.CountAsync());
        Assert.Equal(manifests, await db.EvidenceManifests.CountAsync());
        Assert.Equal(completionAudits, await db.AuditEvents.CountAsync(audit => audit.EventType == "VERIFICATION_COMPLETED"));
    }

    private async Task<Guid> InsertPackageAndManifestRowsAsync(
        Guid sessionId,
        string packageVersion,
        string canonicalizationScheme,
        string hashAlgorithm,
        DateTimeOffset createdAt,
        string signatureStatus = "PlaceholderUnverified")
    {
        var packageId = Guid.NewGuid();
        var resultRef = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        db.EvidencePackages.Add(new EvidencePackageRow
        {
            Id = packageId,
            VerificationSessionId = sessionId,
            PackageVersion = packageVersion,
            CanonicalizationScheme = canonicalizationScheme,
            HashAlgorithm = hashAlgorithm,
            ManifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            EvidenceRefsJson = "[]",
            AuditEventRefsJson = "[]",
            ResultRef = resultRef,
            PackageHash = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            EvidencePackageSignatureStatus = signatureStatus,
            CreatedAt = createdAt,
        });
        db.EvidenceManifests.Add(new EvidenceManifestRow
        {
            EvidencePackageId = packageId,
            SessionGuid = sessionId,
            VerificationSessionId = sessionId.ToString("N"),
            PackageVersion = packageVersion,
            CanonicalizationScheme = canonicalizationScheme,
            HashAlgorithm = hashAlgorithm,
            ManifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            PackageHash = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            EvidenceRefsJson = "[]",
            AuditEventRefsJson = "[]",
            ResultRef = resultRef,
            EvidencePackageSignatureStatus = signatureStatus,
            CreatedAt = createdAt,
        });
        await db.SaveChangesAsync();

        return packageId;
    }

    private static async Task AssertForeignKeyViolationAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(action);
        var postgresException = Assert.IsType<PostgresException>(exception.GetBaseException());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
    }

    private static VerificationFinalizationWrite CreateFinalizationWrite(VerificationSession expected, string idSeed)
    {
        var decisionId = Guid.Parse(idSeed);
        var packageId = Guid.Parse(idSeed.Replace('1', '3').Replace('2', '4'));
        var auditId = Guid.Parse(idSeed.Replace('1', '5').Replace('2', '6'));
        var completedAt = DateTimeOffset.UtcNow;
        var packageHash = new HashRef($"sha256:{new string(idSeed[0], 64)}");
        var manifestHash = new HashRef($"sha256:{new string(idSeed[1], 64)}");
        var completed = expected.WithCompletion(
            VerificationResult.Passed,
            AssuranceLevel.Medium,
            decisionId,
            packageId,
            packageHash,
            manifestHash,
            $"req-{idSeed[0]}",
            $"corr-{idSeed[0]}",
            completedAt);

        return new VerificationFinalizationWrite(
            expected,
            completed,
            new VerificationDecision(
                decisionId,
                expected.Id,
                VerificationResult.Passed,
                AssuranceLevel.Medium,
                RiskScore: null,
                FailedChecks: [],
                CompletedChecks: [RequiredCheckType.CaptureQuality],
                DecisionReasonCodes: ["ALL_REQUIRED_CHECKS_PASSED"],
                RetryReasonCodes: [],
                completedAt),
            new EvidencePackage(
                packageId,
                expected.Id,
                "test",
                EvidenceCanonicalization.CanonicalizationScheme,
                EvidenceCanonicalization.HashAlgorithm,
                manifestHash,
                [],
                [],
                decisionId,
                packageHash,
                SignaturePlaceholderStatus.PlaceholderUnverified,
                completedAt),
            new EvidenceManifestDto(
                packageId.ToString("N"),
                expected.Id.ToString("N"),
                "test",
                EvidenceCanonicalization.CanonicalizationScheme,
                EvidenceCanonicalization.HashAlgorithm,
                manifestHash.ToString(),
                packageHash.ToString(),
                [],
                [],
                decisionId.ToString("N"),
                SignaturePlaceholderStatusDto.PlaceholderUnverified,
                completedAt),
            new AuditEvent(
                auditId,
                expected.ClientApplicationId,
                expected.Id,
                "BusinessConsumer",
                "ldev_biz",
                "VERIFICATION_COMPLETED",
                new HashRef($"sha256:{new string(idSeed[2], 64)}"),
                EventPayloadRef: null,
                $"req-{idSeed[0]}",
                $"corr-{idSeed[0]}",
                completedAt));
    }

    private sealed class TwoPartyAsyncGate
    {
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public async Task SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                release.TrySetResult();
            }

            await release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read", "session.complete" });

    private static AuthenticatedClientContext CaptureCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000007"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_capture",
            AuthenticatedCallerCategory.CaptureAgent,
            new HashSet<string> { "capture.artifact.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
            new HashSet<string> { "ldev_capture" });

    private static AuthenticatedClientContext TrustedCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000008"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_adapter",
            AuthenticatedCallerCategory.TrustedAdapter,
            new HashSet<string> { "trusted.evidence.append" },
            new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId });
}
