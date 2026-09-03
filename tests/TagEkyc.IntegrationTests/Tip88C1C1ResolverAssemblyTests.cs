using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

internal sealed record C2RecipientPackageLineageFixture(
    C2AssemblyPreparationRequest Request,
    Guid JobId,
    Guid AttemptId,
    long FencingToken);

internal sealed record C1RealC2ExecutionFixture(
    RawExportAssemblyExecutionResult Result,
    Guid JobId,
    Guid AttemptId,
    long FencingToken,
    Guid RecipientClientApplicationId);

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C1ResolverAssemblyTests(PostgresPersistenceFixture postgres)
{
    internal async Task<C1RealC2ExecutionFixture> ExecuteWithRealC2ProviderAsync(
        Func<Guid, Guid, Task<IC2AssemblyPreparationProvider>> providerFactory)
    {
        await using var prepared = await PrepareAssemblyExecutionAsync(
            null,
            null,
            null,
            null,
            providerFactory);
        var result = await prepared.Orchestrator.ExecuteAsync(prepared.Request, CancellationToken.None);
        await using var db = postgres.CreateDbContext();
        var recipient = await db.RawExportJobIdentities.AsNoTracking()
            .Where(row => row.JobId == prepared.JobId)
            .Select(row => row.RecipientClientApplicationId)
            .SingleAsync();
        return new(
            result,
            prepared.JobId,
            prepared.Request.AttemptId,
            prepared.Request.ExpectedFence,
            recipient);
    }

    internal async Task<C2RecipientPackageLineageFixture> CreateC2RecipientPackageLineageAsync()
    {
        var fixture = await CreateSealedAssemblyFixtureAsync();
        await using var db = postgres.CreateDbContext();
        var identity = await db.RawExportAssemblyIdentities.SingleAsync(row => row.AssemblyId == fixture.Result.AssemblyId);
        return new(
            new(
                identity.C2PreparationId,
                identity.AssemblyId,
                identity.AssemblyFingerprint.ToArray(),
                identity.ManifestDigest.ToArray(),
                identity.AssemblyDigest.ToArray(),
                identity.AssemblyAuthenticationValue.ToArray(),
                identity.CompleteAssemblyLength,
                identity.RecipientClientApplicationId),
            identity.JobId,
            identity.AttemptId,
            identity.FencingToken);
    }

    [Fact]
    public async Task C101_exact_selection_and_ordered_class_freeze()
    {
        var fixture = await CreateSealedAssemblyFixtureAsync();
        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, fixture.Result.Outcome);
        Assert.Equal(1, fixture.C2.PrepareCount);
        Assert.Equal(2, fixture.ObjectReads.OpenReadCount);
        Assert.True(fixture.C2.AssemblyWasRetained);
        Assert.True(fixture.C2.AssemblyWasErasedAfterFinalize);
        Assert.True(fixture.C2.Finalized);
        await using var assertion = postgres.CreateDbContext();
        Assert.Equal("AssemblySealed", (await assertion.RawExportJobOperationalHeads.SingleAsync(row => row.JobId == fixture.JobId)).CurrentState);
        Assert.Equal(1, await assertion.RawExportAssemblyIdentities.CountAsync(row => row.JobId == fixture.JobId));
        Assert.Equal(1, await assertion.RawExportAssemblyItems.CountAsync(row => row.AssemblyId == fixture.Result.AssemblyId));
        Assert.Equal("Finalized", (await assertion.RawExportAssemblyPreparationDispositions.SingleAsync(row => row.C2PreparationId == fixture.Result.C2PreparationId)).Disposition);
    }

    private async Task<SealedAssemblyFixture> CreateSealedAssemblyFixtureAsync(
        BoundedFixtureC2Provider? c2Override = null,
        byte[]? plaintextOverride = null)
    {
        await using var prepared = await PrepareAssemblyExecutionAsync(
            c2Override,
            plaintextOverride,
            null);
        var result = await prepared.Orchestrator.ExecuteAsync(prepared.Request, CancellationToken.None);
        return new(
            prepared.JobId,
            prepared.Request,
            result,
            prepared.Orchestrator,
            prepared.C2,
            prepared.ObjectReads,
            prepared.Source);
    }

    private async Task<PreparedAssemblyFixture> PrepareAssemblyExecutionAsync(
        BoundedFixtureC2Provider? c2Override,
        byte[]? plaintextOverride,
        Func<Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture, Task>? beforeAuthenticate,
        RawExportJobLeaseState? leaseOverride = null,
        Func<Guid, Guid, Task<IC2AssemblyPreparationProvider>>? c2Factory = null)
    {
        var plaintext = plaintextOverride ?? Encoding.UTF8.GetBytes("c1-end-to-end-synthetic-selfie");
        var minio = await DurableObjectMinioFixture.StartAsync();
        await using var setup = postgres.CreateDbContext();
        var permit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage]);
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var r2 = new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(postgres);
        var source = await r2.CreateR3VerifiedSourceForExistingSessionAsync(
            plaintext,
            minio,
            actor.PrincipalId,
            actor.ClientApplicationId,
            permit.SessionId,
            permit.PolicyId);

        await SelectAcceptanceAsync(source);
        RawExportR3StageResult staged;
        await using (var stageDb = postgres.CreateDbContext())
            staged = await new RawExportR3StagingService(stageDb).StageAsync(new(
                source.ActorPrincipalId, source.AttemptId, source.ObjectCustodyId,
                source.ReservationRevision, source.EncryptionAttemptRevision,
                source.Fence, source.ObjectStateRevision));
        Assert.Equal(RawExportR3StageDisposition.Staged, staged.Disposition);

        SourceCommitResult committed;
        await using (var commitDb = postgres.CreateDbContext())
            committed = await new RawExportSourceFinalizationService(commitDb).CommitAsync(new(
                source.ActorPrincipalId, source.AttemptId, staged.ReservationRevision!.Value,
                source.EncryptionAttemptRevision, source.Fence, source.ObjectStateRevision));
        Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
        await using (var publishDb = postgres.CreateDbContext())
        {
            var published = await new RawExportSourceFinalizationService(publishDb).PublishAsync(new(
                source.ActorPrincipalId, committed.SourcePublicationId!.Value,
                staged.ReservationRevision.Value, source.Fence));
            Assert.Equal(SourcePublishDisposition.Available, published.Disposition);
        }

        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(setup, leaseOverride);
        var bound = await jobs.BindAsync(new(actor, permit.PermitId, $"c1-e2e-{Guid.NewGuid():N}"));
        Assert.Equal(RawExportJobBindStatus.NewJob, bound.Status);
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(
            actor, bound.JobId, 0, 0, Guid.NewGuid()));
        Assert.Equal(RawExportJobLeaseStatus.Acquired, acquired.Status);

        var verifyDb = postgres.CreateDbContext();
        var lookupDb = postgres.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(lookupDb));
        var contentProvider = CreateContentCommitmentProvider();
        var contentScope = contentProvider.CreateAsyncScope();
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));
        var objectReads = new CountingReconciler(
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)));
        var resolver = new RawExportAssemblySourceResolver(
            repository,
            objectReads,
            new RawExportFramedSourceVerificationService(
                new AttemptAeadVerificationOperationService(
                    verifyDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                contentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>()));
        var c2 = c2Override ?? new BoundedFixtureC2Provider();
        var recipient = await setup.RawExportJobIdentities.AsNoTracking()
            .Where(row => row.JobId == bound.JobId)
            .Select(row => row.RecipientClientApplicationId)
            .SingleAsync();
        var effectiveC2 = c2Factory is null
            ? c2
            : await c2Factory(bound.JobId, recipient);
        var orchestrator = new RawExportAssemblyOrchestrator(
            repository,
            resolver,
            new RawExportAssemblyAuthenticationService(new FixtureAssemblyAuthenticator(
                beforeAuthenticate is null ? null : () => beforeAuthenticate(source))),
            effectiveC2);
        var request = new RawExportAssemblyExecutionRequest(
            bound.JobId,
            acquired.AttemptId!.Value,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            actor.PrincipalId);
        return new(
            [verifyDb, lookupDb, contentScope, contentProvider, minio],
            minio,
            bound.JobId,
            request,
            acquired.LeaseExpiresAt!.Value,
            orchestrator,
            c2,
            objectReads,
            source);
    }

    private OwnedAssemblyOrchestrator CreateIndependentOrchestrator(
        DurableObjectMinioFixture minio,
        BoundedFixtureC2Provider c2)
    {
        var verifyDb = postgres.CreateDbContext();
        var lookupDb = postgres.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(lookupDb));
        var contentProvider = CreateContentCommitmentProvider();
        var contentScope = contentProvider.CreateAsyncScope();
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));
        var resolver = new RawExportAssemblySourceResolver(
            repository,
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)),
            new RawExportFramedSourceVerificationService(
                new AttemptAeadVerificationOperationService(
                    verifyDb,
                    kek,
                    DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                contentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>()));
        return new(
            new RawExportAssemblyOrchestrator(
                repository,
                resolver,
                new RawExportAssemblyAuthenticationService(new FixtureAssemblyAuthenticator()),
                c2),
            [verifyDb, lookupDb, contentScope, contentProvider]);
    }
    [Fact]
    public async Task C102_missing_duplicate_or_mismatched_source_has_zero_bindings()
    {
        await using var db = postgres.CreateDbContext();
        var permit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPermitAsync(
            db, [RawExportRawClass.LiveSelfieImage]);
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, permit.PermitId, $"c1-missing-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(
            actor, bound.JobId, 0, 0, Guid.NewGuid()));
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));

        var result = await repository.FreezeAsync(new(
            bound.JobId, acquired.AttemptId!.Value, acquired.Revision!.Value,
            acquired.FencingToken!.Value, actor.PrincipalId), default);

        Assert.Equal("SourceUnavailable", result.Outcome);
        Assert.Equal(0, result.RowRevision);
        Assert.Equal(0, await db.RawExportJobSourceBindings.CountAsync(row => row.JobId == bound.JobId));

        var mismatched = await repository.FreezeAsync(new(
            bound.JobId, acquired.AttemptId.Value, acquired.Revision.Value,
            acquired.FencingToken.Value + 1, actor.PrincipalId), default);
        Assert.Equal("LeaseLost", mismatched.Outcome);
        Assert.Equal(0, await db.RawExportJobSourceBindings.CountAsync(row => row.JobId == bound.JobId));
    }
    [Fact]
    public async Task C103_source_eligibility_is_closed_and_fail_closed()
    {
        var fixture = await CreateSealedAssemblyFixtureAsync();
        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, fixture.Result.Outcome);
        await AssertFunctionContains(
            "raw_export_freeze_job_source_bindings",
            "PublicationState",
            "Available",
            "VerifiedCompleted",
            "PreparationDisposition",
            "Active",
            "StagedAtUtc",
            "EffectivePlaintextRetentionExpiresAtUtc",
            "R2TerminationDisposition",
            "FreshAuthorityRequired",
            "Forbidden");
    }
    [Fact]
    public async Task C104_frozen_binding_is_insert_once_and_exact_replay_only()
    {
        await using var prepared = await PrepareAssemblyExecutionAsync(null, null, null);
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));

        var first = await repository.FreezeAsync(prepared.Request, default);
        Assert.Equal("Frozen", first.Outcome);
        Assert.Equal(1, first.RowRevision);
        var replay = await repository.FreezeAsync(prepared.Request, default);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(first.RowRevision, replay.RowRevision);

        await using var db = postgres.CreateDbContext();
        var bindings = await db.RawExportJobSourceBindings.AsNoTracking()
            .Where(row => row.JobId == prepared.JobId)
            .ToArrayAsync();
        Assert.Single(bindings);
        Assert.Equal(0, bindings[0].Ordinal);
        Assert.Equal(prepared.Source.SourceArtifactId, bindings[0].SourceArtifactId);
        Assert.Equal(32, bindings[0].BindingFingerprint.Length);

        var wrongFence = await repository.FreezeAsync(
            prepared.Request with { ExpectedFence = prepared.Request.ExpectedFence + 1 },
            default);
        Assert.Equal("LeaseLost", wrongFence.Outcome);
        Assert.Equal(1, await db.RawExportJobSourceBindings.CountAsync(row => row.JobId == prepared.JobId));
    }

    [Fact]
    public async Task C105_resolver_and_sealer_acl_manifests_are_separate()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pg_catalog.count(*)
            FROM information_schema.routine_privileges
            WHERE specific_schema='tagekyc'
              AND grantee IN ('tagekyc_raw_export_assembly_resolver','tagekyc_raw_export_assembly_sealer')
              AND privilege_type='EXECUTE'
            """;
        Assert.Equal(10L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        await AssertFunctionGrantAsync(connection, "raw_export_read_job_source_verification_context", "tagekyc_raw_export_assembly_resolver", true);
        await AssertFunctionGrantAsync(connection, "raw_export_read_job_source_verification_context", "tagekyc_raw_export_assembly_sealer", false);
        await AssertFunctionGrantAsync(connection, "raw_export_seal_authenticated_assembly", "tagekyc_raw_export_assembly_sealer", true);
        await AssertFunctionGrantAsync(connection, "raw_export_seal_authenticated_assembly", "tagekyc_raw_export_assembly_resolver", false);
        await AssertFunctionGrantAsync(connection, "raw_export_read_committed_assembly_recovery_context", "tagekyc_raw_export_assembly_sealer", true);
        await AssertFunctionGrantAsync(connection, "raw_export_read_committed_assembly_recovery_context", "tagekyc_raw_export_assembly_resolver", false);
    }

    [Fact]
    public async Task C106_exact_object_is_reopened_without_public_locator_surface()
    {
        var fixture = await CreateSealedAssemblyFixtureAsync();
        await using var db = postgres.CreateDbContext();
        var objectRow = await db.RawExportProvisionalObjects.AsNoTracking()
            .SingleAsync(row => row.ObjectCustodyId == fixture.Source.ObjectCustodyId);

        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, fixture.Result.Outcome);
        Assert.Equal(2, fixture.ObjectReads.Locators.Count);
        Assert.All(fixture.ObjectReads.Locators, locator =>
        {
            Assert.Equal(objectRow.ProvisionalObjectIdentity, locator.ProvisionalObjectIdentity);
            Assert.Equal(objectRow.ObjectBindingDigest, locator.ObjectBindingDigest);
            Assert.Equal(objectRow.ObjectKey, locator.ObjectKey);
        });
        Assert.Equal(fixture.ObjectReads.Locators[0].ObjectKey, fixture.ObjectReads.Locators[1].ObjectKey);
        Assert.DoesNotContain(typeof(ExactObjectLocator), typeof(RawExportAssemblyExecutionRequest).GetProperties().Select(p => p.PropertyType));
    }

    [Fact]
    public void C107_typed_source_failure_partition_is_exact()
    {
        Assert.Equal(new[] { "Verified", "KeyAccessIndeterminate", "ObjectReadIndeterminate", "DeterministicCiphertextInvalid", "HistoricCommitmentMismatch", "CallerCancelled" }, Enum.GetNames<RawExportAssemblySourceDisposition>());
        var unavailable = ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure);
        var providerFailure = Assert.Throws<RawExportContentCommitmentProviderUnavailableException>(
            () => RawExportFramedSourceVerificationService.EnsureContentCommitmentAvailable(unavailable));
        Assert.Equal(
            RawExportAssemblySourceDisposition.KeyAccessIndeterminate,
            RawExportAssemblySourceResolver.ClassifyVerificationFailure(providerFailure, false));
        Assert.Equal(
            RawExportAssemblySourceDisposition.ObjectReadIndeterminate,
            RawExportAssemblySourceResolver.ClassifyVerificationFailure(new IOException("object-read"), false));
        Assert.Equal(
            RawExportAssemblySourceDisposition.HistoricCommitmentMismatch,
            RawExportAssemblySourceResolver.ClassifyVerificationFailure(
                new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_CONTENT_COMMITMENT_MISMATCH"), false));
    }

    [Fact]
    public void C108_r2_and_c1_share_framed_verification_service()
    {
        Assert.Contains(typeof(RawExportR2CompletionVerifier).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => field.FieldType == typeof(RawExportFramedSourceVerificationService));
        Assert.Contains(typeof(RawExportFramedSourceVerificationService), PrimaryParameters(typeof(RawExportAssemblySourceResolver)));
    }

    [Fact]
    public async Task C109_pass_one_absolute_assembly_digest_vector()
    {
        var canonical = await ComputeCanonicalVectorAsync();
        AssertHex("901a70447c9fe9f7a2884cd473359afef12b2e5d5b17ac1f74b49a7535f25c6f", canonical.AssemblyDigest);
        var mutated = await ComputeCanonicalVectorAsync(mutateSecondPlaintextByte: true);
        Assert.NotEqual(canonical.AssemblyDigest, mutated.AssemblyDigest);
    }

    [Fact]
    public async Task C110_manifest_and_authentication_absolute_vectors()
    {
        var canonical = await ComputeCanonicalVectorAsync();
        AssertHex("eb3ad4b3ddc4b49e33c8e4eff34a5e50f8574a438a70e95fd79d4ec88e7fc41b", canonical.ManifestDigest);
        AssertHex("d73771a5d97f1cd152836d597a5c3ddfbc7ecda007bbd2bbdc41f27996f96be2", canonical.AuthenticationValue);

        var changedAssemblyDigest = canonical.AssemblyDigest.ToArray();
        changedAssemblyDigest[0] ^= 0x80;
        var digestTargetMutation = ComputeManifestDigest(changedAssemblyDigest, "fixture-assembly-authentication", 1);
        var keyIdMutation = ComputeManifestDigest(canonical.AssemblyDigest, "fixture-assembly-authentication-mutated", 1);
        var keyVersionMutation = ComputeManifestDigest(canonical.AssemblyDigest, "fixture-assembly-authentication", 2);
        Assert.NotEqual(canonical.ManifestDigest, digestTargetMutation);
        Assert.NotEqual(canonical.ManifestDigest, keyIdMutation);
        Assert.NotEqual(canonical.ManifestDigest, keyVersionMutation);
    }

    [Fact]
    public async Task C111_deterministic_identifiers_and_fingerprints_are_absolute()
    {
        var vector = await ComputeCanonicalVectorAsync();
        Assert.Equal(Guid.Parse("aea6e706-6f9d-58ff-b4c3-9a2dc40c43a6"), vector.AssemblyId);
        AssertHex("09dba4977031ec99b7fe89a7b942b13508185015ffe2d45820850a2fd591757f", vector.AssemblyFingerprint);
        Assert.Equal(Guid.Parse("8e22d1f0-079a-5804-9c7d-63c47190644d"), vector.PreparationId);
        AssertHex("3285f039e1fe9ec64e1ebca30e7c86e0b608cd5fd06bafbf76381ee4bd75dd4f", vector.PreparationFingerprint);
        var mutatedFingerprint = vector.AssemblyFingerprint.ToArray();
        mutatedFingerprint[0] ^= 0x01;
        Assert.NotEqual(vector.PreparationId, RawExportAssemblyCodec.C2PreparationId(vector.AssemblyId, mutatedFingerprint));
    }

    [Fact]
    public async Task C112_first_fresh_barrier_precedes_source_read()
    {
        await using var prepared = await PrepareAssemblyExecutionAsync(null, null, null);
        await WithdrawAuthorityAsync(prepared.Source);

        var result = await prepared.Orchestrator.ExecuteAsync(prepared.Request, default);

        Assert.Equal(RawExportAssemblyExecutionOutcome.AuthorityInvalid, result.Outcome);
        Assert.Equal(0, prepared.ObjectReads.OpenReadCount);
        Assert.Equal(0, prepared.C2.PrepareCount);
    }

    [Fact]
    public async Task C113_second_fresh_barrier_immediately_precedes_prepare()
    {
        var callbackCount = 0;
        await using var prepared = await PrepareAssemblyExecutionAsync(
            null,
            null,
            async source =>
            {
                callbackCount++;
                await WithdrawAuthorityAsync(source);
            });

        var result = await prepared.Orchestrator.ExecuteAsync(prepared.Request, default);

        Assert.Equal(1, callbackCount);
        Assert.Equal(RawExportAssemblyExecutionOutcome.AuthorityInvalid, result.Outcome);
        Assert.Equal(1, prepared.ObjectReads.OpenReadCount);
        Assert.Equal(0, prepared.C2.PrepareCount);
    }
    [Fact]
    public async Task C114_preparing_is_durable_before_provider_call_and_recoverable()
    {
        await AssertFunctionContains("raw_export_register_assembly_preparing", "ON CONFLICT DO NOTHING", "Preparing");
        var provider = new BoundedFixtureC2Provider(loseFirstPrepareResponse: true);
        var fingerprint = Enumerable.Repeat((byte)0x51, 32).ToArray();
        var request = new C2AssemblyPreparationRequest(
            Guid.NewGuid(), Guid.NewGuid(), fingerprint,
            Enumerable.Repeat((byte)0x52, 32).ToArray(),
            Enumerable.Repeat((byte)0x53, 32).ToArray(),
            Enumerable.Repeat((byte)0x54, 32).ToArray(), 3, Guid.NewGuid());
        static Task Write(Stream destination, CancellationToken token) =>
            destination.WriteAsync(new byte[] { 1, 2, 3 }, token).AsTask();

        var recovered = await RawExportAssemblyOrchestrator.PrepareOrRecoverAsync(
            provider, request, false, Write, default);
        Assert.Equal(C2AssemblyPrepareOutcome.ExistingMatch, recovered.Outcome);
        Assert.Equal(1, provider.PrepareCount);
        Assert.Equal(1, provider.InspectCount);

        var replay = await RawExportAssemblyOrchestrator.PrepareOrRecoverAsync(
            provider, request, true, Write, default);
        Assert.Equal(C2AssemblyPrepareOutcome.ExistingMatch, replay.Outcome);
        Assert.Equal(1, provider.PrepareCount);
        Assert.Equal(2, provider.InspectCount);
    }
    [Fact]
    public async Task C115_pending_requires_exact_receipt_and_revision()
    {
        await using var db = postgres.CreateDbContext();
        var permit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPermitAsync(
            db, [RawExportRawClass.LiveSelfieImage]);
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, permit.PermitId, $"c1-pending-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(
            actor, bound.JobId, 0, 0, Guid.NewGuid()));
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));
        var assemblyId = RawExportAssemblyCodec.AssemblyId(bound.JobId);
        var assemblyFingerprint = Enumerable.Repeat((byte)0x41, 32).ToArray();
        var preparationFingerprint = Enumerable.Repeat((byte)0x42, 32).ToArray();
        var preparationId = RawExportAssemblyCodec.C2PreparationId(assemblyId, assemblyFingerprint);
        var request = new RawExportAssemblyExecutionRequest(
            bound.JobId,
            acquired.AttemptId!.Value,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            actor.PrincipalId);
        var created = await repository.RegisterPreparingAsync(
            preparationId, assemblyId, request,
            assemblyFingerprint, preparationFingerprint, default);
        Assert.Equal("Created", created.Outcome);
        var receipt = Enumerable.Repeat((byte)0x43, 32).ToArray();

        var wrongRevision = await repository.RecordPendingAsync(
            preparationId, created.RowRevision!.Value + 1, receipt, default);
        Assert.Equal("PreparationConflict", wrongRevision.Outcome);
        Assert.Null(wrongRevision.RowRevision);

        var pending = await repository.RecordPendingAsync(
            preparationId, created.RowRevision.Value, receipt, default);
        Assert.Equal("Pending", pending.Outcome);
        Assert.Equal(created.RowRevision + 1, pending.RowRevision);

        var replay = await repository.RecordPendingAsync(
            preparationId, created.RowRevision.Value, receipt, default);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(pending.RowRevision, replay.RowRevision);

        var wrongReceipt = receipt.ToArray();
        wrongReceipt[0] ^= 0x01;
        var conflict = await repository.RecordPendingAsync(
            preparationId, pending.RowRevision!.Value, wrongReceipt, default);
        Assert.Equal("PreparationConflict", conflict.Outcome);
        Assert.Null(conflict.RowRevision);

        db.ChangeTracker.Clear();
        var persisted = await db.RawExportAssemblyPreparationDispositions.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == preparationId);
        Assert.Equal("Pending", persisted.Disposition);
        Assert.Equal(pending.RowRevision, persisted.RowRevision);
        Assert.Equal(receipt, persisted.ProviderReceiptDigest);
    }

    [Fact]
    public async Task C116_global_lock_subsequence_is_preserved()
    {
        await using var prepared = await PrepareAssemblyExecutionAsync(null, null, null);
        await using var independent = CreateIndependentOrchestrator(prepared.Minio, prepared.C2);
        var first = prepared.Orchestrator.ExecuteAsync(prepared.Request, default);
        var second = independent.Orchestrator.ExecuteAsync(prepared.Request, default);
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromMinutes(1));
        Assert.Contains(results, result => result.Outcome == RawExportAssemblyExecutionOutcome.Sealed);
        Assert.Contains(results, result => result.Outcome is
            RawExportAssemblyExecutionOutcome.ExistingMatch or
            RawExportAssemblyExecutionOutcome.LeaseLost);
        Assert.Equal(1, prepared.C2.PrepareCount);

        var definition = await FunctionDefinition("raw_export_seal_authenticated_assembly");
        AssertOrdered(definition, "raw_export_job_operational_heads", "raw_export_job_attempts", "raw_export_job_identities", "raw_export_source_publications", "raw_export_source_encryption_attempts", "raw_export_source_head", "raw_export_source_reservations", "raw_export_source_ingress_claims", "raw_export_attempt_key_reservations", "raw_export_provisional_objects", "raw_export_job_source_bindings", "raw_export_assembly_preparation_dispositions");
    }

    [Fact]
    public async Task C117_post_lock_admission_clock_is_single_and_fresh()
    {
        await AssertSealLockWaitCrossesExpiryAsync(authorityLock: true);
        await AssertSealLockWaitCrossesExpiryAsync(authorityLock: false);
    }

    [Fact]
    public async Task C118_exact_seal_replay_precedes_stale_revision()
    {
        var provider = new BoundedFixtureC2Provider(unavailableFirstFinalize: true);
        var fixture = await CreateSealedAssemblyFixtureAsync(provider);
        Assert.Equal(RawExportAssemblyExecutionOutcome.ProviderUnavailable, fixture.Result.Outcome);
        await using var db = postgres.CreateDbContext();
        var identity = await db.RawExportAssemblyIdentities.SingleAsync(row => row.JobId == fixture.JobId);
        var before = await db.RawExportAssemblyPreparationDispositions.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == identity.C2PreparationId);
        Assert.Equal("SealCommitted", before.Disposition);
        Assert.Equal(1, fixture.C2.PrepareCount);
        Assert.Equal(1, fixture.C2.FinalizeCount);
        Assert.Equal(2, fixture.ObjectReads.OpenReadCount);

        var replay = await fixture.Orchestrator.ExecuteAsync(fixture.Request, default);

        Assert.Equal(RawExportAssemblyExecutionOutcome.ExistingMatch, replay.Outcome);
        Assert.Equal(fixture.Request.ExpectedJobRevision + 1, replay.JobRevision);
        Assert.True(fixture.C2.Finalized);
        Assert.Equal(1, fixture.C2.PrepareCount);
        Assert.Equal(2, fixture.C2.FinalizeCount);
        Assert.Equal(2, fixture.ObjectReads.OpenReadCount);
        db.ChangeTracker.Clear();
        var after = await db.RawExportAssemblyPreparationDispositions.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == identity.C2PreparationId);
        Assert.Equal("Finalized", after.Disposition);
        Assert.Equal(before.RowRevision + 1, after.RowRevision);
        Assert.Equal(before.SealCommittedAtUtc, after.SealCommittedAtUtc);
        Assert.NotNull(after.FinalizedAtUtc);

        var mismatched = await fixture.Orchestrator.ExecuteAsync(
            fixture.Request with { ExpectedFence = fixture.Request.ExpectedFence + 1 },
            default);
        Assert.Equal(RawExportAssemblyExecutionOutcome.LeaseLost, mismatched.Outcome);
        Assert.Equal(1, fixture.C2.PrepareCount);
        Assert.Equal(2, fixture.C2.FinalizeCount);
        Assert.Equal(2, fixture.ObjectReads.OpenReadCount);
    }
    [Fact]
    public async Task C119_two_pass_streaming_has_no_complete_plaintext_buffer_or_temp_file()
    {
        var plaintext = Enumerable.Range(0, 3 * 1024 * 1024)
            .Select(index => (byte)(index % 251))
            .ToArray();

        var fixture = await CreateSealedAssemblyFixtureAsync(plaintextOverride: plaintext);

        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, fixture.Result.Outcome);
        Assert.Equal(2, fixture.ObjectReads.OpenReadCount);
        Assert.True(fixture.C2.MaximumWriteSize > 0);
        Assert.True(fixture.C2.MaximumWriteSize < plaintext.Length);
        Assert.True(fixture.C2.CompleteAssemblyLength > plaintext.Length);
        Assert.True(fixture.C2.AssemblyWasRetained);
        Assert.True(fixture.C2.AssemblyWasErasedAfterFinalize);
    }
    [Fact]
    public async Task C120_seal_commits_preparation_identity_items_head_and_event()
    {
        var fixture = await CreateSealedAssemblyFixtureAsync(
            new BoundedFixtureC2Provider(unavailableFirstFinalize: true));
        Assert.Equal(RawExportAssemblyExecutionOutcome.ProviderUnavailable, fixture.Result.Outcome);
        Assert.NotNull(fixture.Result.AssemblyId);
        Assert.NotNull(fixture.Result.C2PreparationId);

        await using var db = postgres.CreateDbContext();
        var head = await db.RawExportJobOperationalHeads.AsNoTracking()
            .SingleAsync(row => row.JobId == fixture.JobId);
        var identity = await db.RawExportAssemblyIdentities.AsNoTracking()
            .SingleAsync(row => row.JobId == fixture.JobId);
        var item = await db.RawExportAssemblyItems.AsNoTracking()
            .SingleAsync(row => row.AssemblyId == identity.AssemblyId);
        var preparation = await db.RawExportAssemblyPreparationDispositions.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == identity.C2PreparationId);
        var transition = await db.RawExportJobTransitions.AsNoTracking()
            .SingleAsync(row => row.JobId == fixture.JobId && row.EventType == "AssemblySealed");

        Assert.Equal("AssemblySealed", head.CurrentState);
        Assert.Equal(fixture.Request.ExpectedJobRevision + 1, head.Revision);
        Assert.Equal(fixture.Request.AttemptId, head.CurrentAttemptId);
        Assert.Equal(fixture.Request.ExpectedFence, head.FencingToken);
        Assert.Null(head.LeaseOwnerId);
        Assert.Null(head.LeaseExpiresAt);
        Assert.Equal(fixture.Result.AssemblyId, identity.AssemblyId);
        Assert.Equal(fixture.Result.C2PreparationId, identity.C2PreparationId);
        Assert.Equal(fixture.Request.AttemptId, identity.AttemptId);
        Assert.Equal(fixture.Request.ExpectedFence, identity.FencingToken);
        Assert.Equal(1, identity.ItemCount);
        Assert.Equal(fixture.Source.SourceArtifactId, item.SourceArtifactId);
        Assert.NotEqual(Guid.Empty, item.CaptureArtifactId);
        Assert.Equal(fixture.Source.RawClass, item.RawClass);
        Assert.Equal("SealCommitted", preparation.Disposition);
        Assert.Equal(identity.AssemblyId, preparation.AssemblyId);
        Assert.Equal(identity.AttemptId, preparation.AttemptId);
        Assert.Equal(identity.FencingToken, preparation.FencingToken);
        Assert.Equal(identity.AssemblyFingerprint, preparation.AssemblyFingerprint);
        Assert.NotNull(preparation.ProviderReceiptDigest);
        Assert.NotNull(preparation.SealCommittedAtUtc);
        Assert.Null(preparation.FinalizedAtUtc);
        Assert.Equal(head.Revision, transition.ResultingRevision);
        Assert.Equal("Assembling", transition.FromState);
        Assert.Equal("AssemblySealed", transition.ToState);
        Assert.Equal(identity.AttemptId, transition.AttemptId);
        Assert.Equal(identity.FencingToken, transition.FencingToken);
    }

    [Fact]
    public async Task C121_assembly_sealed_is_a_closed_job_state_and_event()
    {
        Assert.Contains(RawExportJobState.AssemblySealed, Enum.GetValues<RawExportJobState>());
        Assert.Contains(RawExportJobEventType.AssemblySealed, Enum.GetValues<RawExportJobEventType>());
        var fixture = await CreateSealedAssemblyFixtureAsync();
        await using var db = postgres.CreateDbContext();
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var rejected = await Assert.ThrowsAsync<RawExportJobException>(() =>
            jobs.AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                fixture.JobId,
                fixture.Result.JobRevision!.Value,
                fixture.Request.ExpectedFence,
                Guid.NewGuid())));
        Assert.Equal("RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE", rejected.Code);
        db.ChangeTracker.Clear();
        var head = await db.RawExportJobOperationalHeads.AsNoTracking()
            .SingleAsync(row => row.JobId == fixture.JobId);
        Assert.Equal("AssemblySealed", head.CurrentState);
        Assert.Null(head.LeaseOwnerId);
        Assert.Null(head.LeaseExpiresAt);
    }

    [Fact]
    public async Task C122_finalize_follows_committed_seal()
    {
        AssertSourceOrder("RawExportAssemblyOrchestrator.cs", "repository.SealAsync", "FinalizeOrRecoverAsync");
        var provider = new BoundedFixtureC2Provider(loseFirstFinalizeResponse: true);
        var fingerprint = Enumerable.Repeat((byte)0x61, 32).ToArray();
        var request = new C2AssemblyPreparationRequest(
            Guid.NewGuid(), Guid.NewGuid(), fingerprint,
            Enumerable.Repeat((byte)0x62, 32).ToArray(),
            Enumerable.Repeat((byte)0x63, 32).ToArray(),
            Enumerable.Repeat((byte)0x64, 32).ToArray(), 1, Guid.NewGuid());
        await RawExportAssemblyOrchestrator.PrepareOrRecoverAsync(
            provider, request, false,
            (destination, token) => destination.WriteAsync(new byte[] { 7 }, token).AsTask(), default);

        var wrongFingerprint = fingerprint.ToArray();
        wrongFingerprint[0] ^= 0x01;
        var rejected = await RawExportAssemblyOrchestrator.FinalizeOrRecoverAsync(
            provider, request.C2PreparationId, wrongFingerprint, default);
        Assert.Equal(C2AssemblyFinalizeOutcome.Conflict, rejected.Outcome);
        Assert.False(provider.Finalized);
        Assert.True(provider.AssemblyWasRetained);

        var result = await RawExportAssemblyOrchestrator.FinalizeOrRecoverAsync(
            provider, request.C2PreparationId, fingerprint, default);

        Assert.Equal(C2AssemblyFinalizeOutcome.ExistingMatch, result.Outcome);
        Assert.True(provider.Finalized);
        Assert.True(provider.AssemblyWasErasedAfterFinalize);
        Assert.Equal(1, provider.InspectCount);
    }
    [Fact]
    public async Task C123_abort_requires_durable_authorization()
    {
        await using var db = postgres.CreateDbContext();
        var permit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPermitAsync(
            db, [RawExportRawClass.LiveSelfieImage]);
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, permit.PermitId, $"c1-abort-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(actor, bound.JobId, 0, 0, Guid.NewGuid()));
        var repository = new RawExportAssemblyRepository(new RoleConnectionFactory(postgres.ConnectionString));
        var assemblyId = RawExportAssemblyCodec.AssemblyId(bound.JobId);
        var fingerprint = Enumerable.Repeat((byte)0x71, 32).ToArray();
        var preparationId = RawExportAssemblyCodec.C2PreparationId(assemblyId, fingerprint);
        var preparationFingerprint = Enumerable.Repeat((byte)0x72, 32).ToArray();
        var registered = await repository.RegisterPreparingAsync(
            preparationId, assemblyId,
            new(bound.JobId, acquired.AttemptId!.Value, acquired.Revision!.Value, acquired.FencingToken!.Value, actor.PrincipalId),
            fingerprint, preparationFingerprint, default);
        Assert.Equal("Created", registered.Outcome);
        var provider = new BoundedFixtureC2Provider();
        var providerRequest = new C2AssemblyPreparationRequest(
            preparationId, assemblyId, fingerprint,
            Enumerable.Repeat((byte)0x73, 32).ToArray(),
            Enumerable.Repeat((byte)0x74, 32).ToArray(),
            Enumerable.Repeat((byte)0x75, 32).ToArray(), 1, Guid.NewGuid());
        await RawExportAssemblyOrchestrator.PrepareOrRecoverAsync(
            provider, providerRequest, false,
            (destination, token) => destination.WriteAsync(new byte[] { 9 }, token).AsTask(), default);
        var authorization = Enumerable.Repeat((byte)0x76, 32).ToArray();

        var unauthorized = await RawExportAssemblyOrchestrator.CompleteAuthorizedAbortAsync(
            repository, provider, preparationId, authorization, default);
        Assert.Equal(C2AssemblyAbortOutcome.Conflict, unauthorized.Outcome);
        Assert.Equal(0, provider.AbortCount);

        var durable = await repository.AuthorizeAbortAsync(
            preparationId, registered.RowRevision!.Value, authorization, default);
        Assert.Equal("AbortAuthorized", durable.Outcome);
        var completed = await RawExportAssemblyOrchestrator.CompleteAuthorizedAbortAsync(
            repository, provider, preparationId, authorization, default);
        Assert.Equal(C2AssemblyAbortOutcome.Aborted, completed.Outcome);
        Assert.Equal(1, provider.AbortCount);
        Assert.Equal("Aborted", (await repository.ReadRecoveryContextAsync(preparationId, default))!.Disposition);
    }

    [Fact]
    public async Task C124_schema_function_owner_and_acl_shapes_are_exact()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pg_catalog.count(*) FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname IN
              ('raw_export_job_source_bindings','raw_export_assembly_preparation_dispositions','raw_export_assembly_identities','raw_export_assembly_items')
              AND pg_catalog.pg_get_userbyid(c.relowner)='tagekyc_raw_export_deployer'
            """;
        Assert.Equal(4L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        command.CommandText = """
            WITH expected(name,args,grantee) AS (VALUES
              ('raw_export_freeze_job_source_bindings','uuid, uuid, bigint, bigint, uuid','tagekyc_raw_export_assembly_resolver'),
              ('raw_export_read_job_source_verification_context','uuid, integer, uuid, bigint, bigint, uuid','tagekyc_raw_export_assembly_resolver'),
              ('raw_export_register_assembly_preparing','uuid, uuid, uuid, bigint, bigint, bytea, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_pending','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_seal_authenticated_assembly','uuid, uuid, bigint, bigint, bigint, uuid, bytea, bytea, bytea, text, integer, bytea, bigint, integer, jsonb','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_authorize_assembly_abort','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_finalized','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_record_assembly_aborted','uuid, bigint, bytea','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_read_assembly_recovery_context','uuid','tagekyc_raw_export_assembly_sealer'),
              ('raw_export_read_committed_assembly_recovery_context','uuid, uuid, bigint, bigint','tagekyc_raw_export_assembly_sealer'))
            SELECT pg_catalog.count(*)
            FROM expected e
            JOIN pg_catalog.pg_namespace n ON n.nspname='tagekyc'
            JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid AND p.proname=e.name
              AND pg_catalog.oidvectortypes(p.proargtypes)=e.args
            WHERE pg_catalog.pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
              AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
              AND EXISTS (
                SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                JOIN pg_catalog.pg_roles r ON r.oid=a.grantee
                WHERE a.privilege_type='EXECUTE' AND r.rolname=e.grantee
                  AND a.grantor=p.proowner AND NOT a.is_grantable)
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                WHERE a.privilege_type='EXECUTE'
                  AND (a.grantee NOT IN (p.proowner,(SELECT oid FROM pg_catalog.pg_roles WHERE rolname=e.grantee))
                    OR a.grantor<>p.proowner OR a.is_grantable))
            """;
        Assert.Equal(10L, Convert.ToInt64(await command.ExecuteScalarAsync()));

        var recoveryDefinition = await FunctionDefinition("raw_export_read_committed_assembly_recovery_context");
        await AssertReadinessMutationAsync(
            "DROP FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint)",
            recoveryDefinition + "; ALTER FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer; REVOKE ALL ON FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint) FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer; GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint) TO tagekyc_raw_export_assembly_sealer");
        await AssertReadinessMutationAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) TO tagekyc_raw_export_assembly_sealer WITH GRANT OPTION",
            "REVOKE GRANT OPTION FOR EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) FROM tagekyc_raw_export_assembly_sealer");
        await AssertReadinessMutationAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) TO tagekyc_runtime",
            "REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) FROM tagekyc_runtime");
        await AssertReadinessMutationAsync(
            """
            CREATE ROLE tagekyc_c1_acl_alternate_grantor NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            GRANT USAGE ON SCHEMA tagekyc TO tagekyc_c1_acl_alternate_grantor;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) TO tagekyc_c1_acl_alternate_grantor WITH GRANT OPTION;
            SET ROLE tagekyc_c1_acl_alternate_grantor;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) TO tagekyc_raw_export_assembly_sealer;
            RESET ROLE;
            """,
            """
            SET ROLE tagekyc_c1_acl_alternate_grantor;
            REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) FROM tagekyc_raw_export_assembly_sealer;
            RESET ROLE;
            REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) FROM tagekyc_c1_acl_alternate_grantor CASCADE;
            REVOKE USAGE ON SCHEMA tagekyc FROM tagekyc_c1_acl_alternate_grantor;
            DROP ROLE tagekyc_c1_acl_alternate_grantor;
            """);
        await AssertReadinessMutationAsync(
            """
            CREATE FUNCTION tagekyc.raw_export_read_assembly_recovery_context(text)
            RETURNS text LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS 'SELECT $1';
            ALTER FUNCTION tagekyc.raw_export_read_assembly_recovery_context(text) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_read_assembly_recovery_context(text) FROM PUBLIC;
            """,
            "DROP FUNCTION tagekyc.raw_export_read_assembly_recovery_context(text)");
        command.CommandText = """
            WITH expected(name) AS (VALUES
              ('raw_export_freeze_job_source_bindings'),('raw_export_read_job_source_verification_context'),
              ('raw_export_register_assembly_preparing'),('raw_export_record_assembly_pending'),
              ('raw_export_seal_authenticated_assembly'),('raw_export_authorize_assembly_abort'),
              ('raw_export_record_assembly_finalized'),('raw_export_record_assembly_aborted'),
              ('raw_export_read_assembly_recovery_context'),('raw_export_read_committed_assembly_recovery_context'))
            SELECT pg_catalog.count(*) FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname IN (SELECT name FROM expected)
            """;
        Assert.Equal(10L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task C125_migration_model_is_clean_and_b4_down_contract_is_present()
    {
        var migration = Source("src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.cs");
        Assert.Contains("DROP CONSTRAINT \"CK_b4_job_transition_event_shape\"", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_job_head_mutation", migration, StringComparison.Ordinal);
        await postgres.ResetDatabaseAsync();
        try
        {
            await using var db = postgres.CreateDbContext();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20260812120000_Tip88C1B2R4R6SourceFinalization");
            await using (var connection = await OpenAsync())
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles WHERE rolname IN ('tagekyc_raw_export_assembly_resolver','tagekyc_raw_export_assembly_sealer')";
                Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
                command.CommandText = "SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles WHERE rolname IN ('tagekyc_raw_export_assembly_resolver_login','tagekyc_raw_export_assembly_sealer_login')";
                Assert.Equal(2L, Convert.ToInt64(await command.ExecuteScalarAsync()));
            }
            await migrator.MigrateAsync("20260815120000_Tip88C1C1ResolverAssembly");
            await using var reapplied = postgres.CreateDbContext();
            Assert.False(reapplied.Database.HasPendingModelChanges());
        }
        finally
        {
            await using var restore = postgres.CreateDbContext();
            await restore.Database.GetService<IMigrator>().MigrateAsync();
            await postgres.AssertLatestMigrationAsync(nameof(C125_migration_model_is_clean_and_b4_down_contract_is_present));
        }
    }

    [Fact]
    public async Task C126_disabled_is_valid_and_fixtureproof_is_nonproduction_only()
    {
        var disabled = new ConfigurationBuilder().Build();
        Assert.Equal(RawExportAssemblyTopology.Disabled, RawExportAssemblyOptions.Resolve(disabled).Topology);
        await ValidateReadinessAsync(disabled, isProduction: true);

        var fixture = FixtureAssemblyConfiguration();
        await AssertReadinessCodeAsync(fixture, true, RawExportAssemblyOptions.ConfigInvalid);
        await AssertReadinessCodeAsync(fixture, false, RawExportAssemblyOptions.AuthenticatorUnavailable);
        await AssertReadinessCodeAsync(fixture, false, RawExportAssemblyOptions.C2Unavailable,
            services => services.AddSingleton<IRawExportAssemblyAuthenticationProvider>(new FixtureAssemblyAuthenticator()));
        await AssertReadinessCodeAsync(fixture, false, RawExportAssemblyOptions.RoleTopologyInvalid,
            services =>
            {
                services.AddSingleton<IRawExportAssemblyAuthenticationProvider>(new FixtureAssemblyAuthenticator());
                services.AddSingleton<IC2AssemblyPreparationProvider>(new BoundedFixtureC2Provider());
            });
        await ValidateReadinessAsync(fixture, false, services =>
        {
            services.AddSingleton<IRawExportAssemblyAuthenticationProvider>(new FixtureAssemblyAuthenticator());
            services.AddSingleton<IC2AssemblyPreparationProvider>(new BoundedFixtureC2Provider());
            services.AddSingleton<IRawExportAssemblyConnectionFactory>(new RoleConnectionFactory(postgres.ConnectionString));
        });

        // Regression bite: the two C1 login-to-capability memberships must not
        // be captured by the independent Durable Key role graph.
        await using var db = postgres.CreateDbContext();
        await new CustodyRoleReadinessValidator(db).ValidateAsync(default);
    }

    private async Task AssertReadinessCodeAsync(
        IConfiguration configuration,
        bool isProduction,
        string expected,
        Action<IServiceCollection>? configure = null)
    {
        var exception = await Assert.ThrowsAsync<RawExportAssemblyReadinessException>(
            () => ValidateReadinessAsync(configuration, isProduction, configure));
        Assert.Equal(expected, exception.Code);
    }

    private async Task ValidateReadinessAsync(
        IConfiguration configuration,
        bool isProduction,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => postgres.CreateDbContext());
        services.AddTagEkycRawExportAssembly(configuration, isProduction);
        configure?.Invoke(services);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RawExportAssemblyReadinessValidator>().ValidateAsync(default);
    }

    private static IConfiguration FixtureAssemblyConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RawExportAssemblyOptions.SectionName}:Topology"] = "FixtureProof",
        }).Build();

    private async Task AssertReadinessMutationAsync(string applySql, string restoreSql)
    {
        await ExecuteSqlAsync(applySql);
        try
        {
            await AssertReadinessCodeAsync(
                FixtureAssemblyConfiguration(),
                false,
                RawExportAssemblyOptions.RoleTopologyInvalid,
                services =>
                {
                    services.AddSingleton<IRawExportAssemblyAuthenticationProvider>(new FixtureAssemblyAuthenticator());
                    services.AddSingleton<IC2AssemblyPreparationProvider>(new BoundedFixtureC2Provider());
                    services.AddSingleton<IRawExportAssemblyConnectionFactory>(new RoleConnectionFactory(postgres.ConnectionString));
                });
        }
        finally
        {
            await ExecuteSqlAsync(restoreSql);
        }

        await ValidateReadinessAsync(
            FixtureAssemblyConfiguration(),
            false,
            services =>
            {
                services.AddSingleton<IRawExportAssemblyAuthenticationProvider>(new FixtureAssemblyAuthenticator());
                services.AddSingleton<IC2AssemblyPreparationProvider>(new BoundedFixtureC2Provider());
                services.AddSingleton<IRawExportAssemblyConnectionFactory>(new RoleConnectionFactory(postgres.ConnectionString));
            });
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task AssertFunctionContains(string function, params string[] expected)
    {
        var definition = await FunctionDefinition(function);
        foreach (var value in expected) Assert.Contains(value, definition, StringComparison.Ordinal);
    }

    private async Task AssertFunctionOrder(string function, string first, string second)
    {
        var definition = await FunctionDefinition(function);
        Assert.True(definition.IndexOf(first, StringComparison.Ordinal) < definition.IndexOf(second, StringComparison.Ordinal));
    }

    private async Task<string> FunctionDefinition(string function)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_catalog.pg_get_functiondef(p.oid) FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='tagekyc' AND p.proname=@name";
        command.Parameters.AddWithValue("name", function);
        return (string)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException(function));
    }

    private async Task<NpgsqlConnection> OpenAsync() { var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); return connection; }

    private static async Task AssertFunctionGrantAsync(NpgsqlConnection connection, string function, string role, bool expected)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_catalog.has_function_privilege(@role,p.oid,'EXECUTE') FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='tagekyc' AND p.proname=@function";
        command.Parameters.AddWithValue("role", role); command.Parameters.AddWithValue("function", function);
        Assert.Equal(expected, await command.ExecuteScalarAsync());
    }

    private static Type[] PrimaryParameters(Type type) => type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderByDescending(c => c.GetParameters().Length).First().GetParameters().Select(p => p.ParameterType).ToArray();
    private static void AssertOrdered(string source, params string[] values) { var offset = -1; foreach (var value in values) { var next = source.IndexOf(value, offset + 1, StringComparison.Ordinal); Assert.True(next > offset, value); offset = next; } }
    private static int Count(string source, string value) { var count = 0; for (var at = 0; (at = source.IndexOf(value, at, StringComparison.Ordinal)) >= 0; at += value.Length) count++; return count; }
    private static void AssertSourceOrder(string file, string first, string second) { var source = Source($"src/TagEkyc.Infrastructure/RawExport/{file}"); Assert.True(source.IndexOf(first, StringComparison.Ordinal) < source.IndexOf(second, StringComparison.Ordinal)); }
    private static void AssertSourceExcludes(string file, params string[] forbidden) { var source = Source($"src/TagEkyc.Infrastructure/RawExport/{file}"); foreach (var value in forbidden) Assert.DoesNotContain(value, source, StringComparison.OrdinalIgnoreCase); }
    private static string Source(string relative) { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) directory = directory.Parent; return File.ReadAllText(Path.Combine(directory?.FullName ?? throw new InvalidOperationException(), relative)); }

    private static async Task<CanonicalAssemblyVector> ComputeCanonicalVectorAsync(
        bool mutateSecondPlaintextByte = false)
    {
        var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var assemblyId = RawExportAssemblyCodec.AssemblyId(jobId);
        var items = CanonicalVectorItems();
        var header = CanonicalVectorHeader(assemblyId, items);
        var second = new byte[] { 4, mutateSecondPlaintextByte ? (byte)6 : (byte)5 };
        var sources = new[]
        {
            VectorSource(items[0], [1, 2, 3]),
            VectorSource(items[1], second),
        };
        var headerBytes = RawExportAssemblyCodec.SerializeHeader(header);
        var length = RawExportAssemblyCodec.CompleteLength(headerBytes.Length, items);
        var assemblyDigest = await RawExportAssemblyCodec.ComputeAssemblyDigestAsync(header, sources, default);
        var manifestDigest = ComputeManifestDigest(assemblyDigest, "fixture-assembly-authentication", 1);
        var authenticationPayload = RawExportAssemblyAuthenticationService.BuildAuthenticationPayload(manifestDigest);
        var authentication = HMACSHA256.HashData(
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
            authenticationPayload);
        var fingerprint = RawExportAssemblyCodec.AssemblyFingerprint(
            assemblyId,
            jobId,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            7,
            manifestDigest,
            "fixture-assembly-authentication",
            1,
            authentication);
        var preparationId = RawExportAssemblyCodec.C2PreparationId(assemblyId, fingerprint);
        var preparationFingerprint = RawExportAssemblyCodec.PreparationFingerprint(
            preparationId, assemblyId, fingerprint, manifestDigest, assemblyDigest, length);
        return new(
            assemblyId, assemblyDigest, manifestDigest, authentication,
            fingerprint, preparationId, preparationFingerprint);
    }

    private static byte[] ComputeManifestDigest(
        byte[] assemblyDigest,
        string authenticationKeyId,
        int authenticationKeyVersion) =>
        RawExportAssemblyCodec.ManifestDigest(
            CanonicalVectorHeader(RawExportAssemblyCodec.AssemblyId(Guid.Parse("11111111-1111-1111-1111-111111111111")), CanonicalVectorItems()),
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            7,
            DateTimeOffset.Parse("2026-08-15T15:30:00Z"),
            1,
            "fixture-subject-token",
            1,
            authenticationKeyId,
            authenticationKeyVersion,
            assemblyDigest);

    private static RawExportAssemblyItemDescriptor[] CanonicalVectorItems() =>
    [
        VectorItem(0, "ChipDg2Portrait", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "cccccccc-cccc-cccc-cccc-cccccccccccc", 1, 3, 0x11),
        VectorItem(1, "LiveSelfieImage", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "dddddddd-dddd-dddd-dddd-dddddddddddd", 2, 2, 0x22),
    ];

    private static RawExportAssemblyHeader CanonicalVectorHeader(
        Guid assemblyId,
        RawExportAssemblyItemDescriptor[] items) =>
        new(
            assemblyId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            DateTimeOffset.Parse("2026-08-15T15:00:00.123456Z"),
            "EncryptedRawVaultRetained",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            1,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            1,
            "SubjectRawBiometricExport",
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            items);

    private static RawExportAssemblyItemDescriptor VectorItem(
        int ordinal, string rawClass, string source, string capture,
        int revision, long length, byte commitment) =>
        new(ordinal, rawClass, Guid.Parse(source), Guid.Parse(capture), revision,
            "image/jpeg", length, 1, "fixture-content-commitment", 1,
            Enumerable.Repeat(commitment, 32).ToArray());

    private static RawExportAssemblyPlaintextItem VectorSource(
        RawExportAssemblyItemDescriptor descriptor,
        byte[] plaintext) =>
        new(descriptor, async (consume, token) =>
        {
            try
            {
                await consume(plaintext, token);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        });

    private static void AssertHex(string expected, byte[] actual) =>
        Assert.Equal(expected, Convert.ToHexString(actual).ToLowerInvariant());

    private sealed record CanonicalAssemblyVector(
        Guid AssemblyId,
        byte[] AssemblyDigest,
        byte[] ManifestDigest,
        byte[] AuthenticationValue,
        byte[] AssemblyFingerprint,
        Guid PreparationId,
        byte[] PreparationFingerprint);

    private async Task SelectAcceptanceAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var actor = connection.CreateCommand())
        {
            actor.Transaction = transaction;
            actor.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)";
            actor.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT tagekyc.raw_export_select_session_capture_acceptance(@session,@class,@acceptance)";
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            Assert.NotEqual(Guid.Empty, (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty));
        }
        await transaction.CommitAsync();
    }

    private ServiceProvider CreateContentCommitmentProvider()
    {
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["TagEkyc:RawExport:CustodyProfile:Profile"] = "Fixture",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        var services = new ServiceCollection();
        services.AddScoped(_ => postgres.CreateDbContext());
        services.AddTagEkycRawExportSourceClaimComparison(configuration);
        return services.BuildServiceProvider();
    }

    private async Task WithdrawAuthorityAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var actor = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)", connection, transaction))
        {
            actor.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using (var command = new NpgsqlCommand("""
            SELECT tagekyc.raw_export_withdraw_authority_snapshot(
              @client,@session,@acceptance,@class,@revision,@actor)
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("client", source.ClientApplicationId);
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("revision", source.AuthorityRevision);
            command.Parameters.AddWithValue("actor", source.ActorPrincipalId);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task AssertSealLockWaitCrossesExpiryAsync(bool authorityLock)
    {
        await using var blocker = new DeferredAdvisoryBlocker(postgres.ConnectionString);
        PreparedAssemblyFixture? prepared = null;
        Task? release = null;
        var lockAcquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new BoundedFixtureC2Provider(afterPrepare: async () =>
        {
            await blocker.AcquireAsync(prepared!.Source, authorityLock);
            lockAcquired.SetResult();
            release = blocker.ReleaseAfterAsync(
                prepared.LeaseExpiresAtUtc + TimeSpan.FromMilliseconds(250));
        });
        await using (prepared = await PrepareAssemblyExecutionAsync(
            provider,
            null,
            null,
            new RawExportJobLeaseState(10, true, null)))
        {
            var execution = prepared.Orchestrator.ExecuteAsync(prepared.Request, default);
            await lockAcquired.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await Task.Delay(250);
            Assert.False(execution.IsCompleted);
            var result = await execution.WaitAsync(TimeSpan.FromMinutes(2));
            if (release is not null) await release;
            Assert.Contains(
                result.Outcome,
                new[]
                {
                    RawExportAssemblyExecutionOutcome.AuthorityInvalid,
                    RawExportAssemblyExecutionOutcome.Expired,
                });
            Assert.False(provider.Finalized);
        }
    }

    private sealed record SealedAssemblyFixture(
        Guid JobId,
        RawExportAssemblyExecutionRequest Request,
        RawExportAssemblyExecutionResult Result,
        RawExportAssemblyOrchestrator Orchestrator,
        BoundedFixtureC2Provider C2,
        CountingReconciler ObjectReads,
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture Source);

    private sealed record PreparedAssemblyFixture(
        IReadOnlyList<IAsyncDisposable> OwnedResources,
        DurableObjectMinioFixture Minio,
        Guid JobId,
        RawExportAssemblyExecutionRequest Request,
        DateTimeOffset LeaseExpiresAtUtc,
        RawExportAssemblyOrchestrator Orchestrator,
        BoundedFixtureC2Provider C2,
        CountingReconciler ObjectReads,
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture Source) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach (var resource in OwnedResources)
                await resource.DisposeAsync();
        }
    }

    private sealed record OwnedAssemblyOrchestrator(
        RawExportAssemblyOrchestrator Orchestrator,
        IReadOnlyList<IAsyncDisposable> OwnedResources) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach (var resource in OwnedResources)
                await resource.DisposeAsync();
        }
    }

    private sealed class CountingReconciler(IProvisionalObjectReconciler inner) : IProvisionalObjectReconciler
    {
        internal int OpenReadCount { get; private set; }
        internal List<ExactObjectLocator> Locators { get; } = [];

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        public Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken)
        {
            OpenReadCount++;
            Locators.Add(locator with { ObjectBindingDigest = locator.ObjectBindingDigest.ToArray() });
            return inner.OpenExactReadAsync(locator, cancellationToken);
        }
    }

    private sealed class RoleConnectionFactory(string baseConnectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RawExportAssemblyDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RawExportAssemblyDatabaseCapability.Resolver => "tagekyc_raw_export_assembly_resolver",
                RawExportAssemblyDatabaseCapability.Sealer => "tagekyc_raw_export_assembly_sealer",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class FixtureAssemblyAuthenticator(Func<Task>? beforeAuthenticate = null) : IRawExportAssemblyAuthenticationProvider
    {
        private static readonly byte[] Key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        public string KeyId => "fixture-assembly-authentication";
        public int KeyVersion => 1;

        public async Task<byte[]> AuthenticateManifestAsync(
            ReadOnlyMemory<byte> authenticationPayload,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (beforeAuthenticate is not null)
                await beforeAuthenticate();
            return HMACSHA256.HashData(Key, authenticationPayload.Span);
        }
    }

    private sealed class DeferredAdvisoryBlocker(string connectionString) : IAsyncDisposable
    {
        private NpgsqlConnection? connection;
        private NpgsqlTransaction? transaction;

        internal async Task AcquireAsync(
            Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,
            bool authorityLock)
        {
            connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            transaction = await connection.BeginTransactionAsync();
            var sql = authorityLock
                ? """
                  SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
                    'tip88c1:b2-authority:'||@client::text||':'||@session::text||':'||@acceptance::text||':'||@class))
                  """
                : """
                  SELECT pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(
                    tagekyc.raw_export_consent_scope_hash(
                      @session,@subject,@policy,@policyVersion,'SubjectRawBiometricExport',@client)))
                  """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("client", source.ClientApplicationId);
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("subject", source.SubjectRef);
            command.Parameters.AddWithValue("policy", source.ConsentPolicyId);
            command.Parameters.AddWithValue("policyVersion", source.ConsentPolicyVersion);
            await command.ExecuteNonQueryAsync();
        }

        internal async Task ReleaseAfterAsync(DateTimeOffset releaseAtUtc)
        {
            var delay = releaseAtUtc - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay);
            if (transaction is not null)
            {
                await transaction.CommitAsync();
                await transaction.DisposeAsync();
                transaction = null;
            }
            if (connection is not null)
            {
                await connection.DisposeAsync();
                connection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (connection is not null) await connection.DisposeAsync();
        }
    }

    private sealed class BoundedFixtureC2Provider(
        bool loseFirstPrepareResponse = false,
        bool loseFirstFinalizeResponse = false,
        bool unavailableFirstFinalize = false,
        Func<Task>? afterPrepare = null) : IC2AssemblyPreparationProvider
    {
        private byte[]? assemblyFingerprint;
        private byte[]? receipt;
        private byte[]? retainedAssembly;
        private byte[]? abortAuthorizationDigest;
        private Guid preparationId;
        private State state;
        private readonly SemaphoreSlim prepareGate = new(1, 1);

        internal int PrepareCount { get; private set; }
        internal int InspectCount { get; private set; }
        internal int FinalizeCount { get; private set; }
        internal int AbortCount { get; private set; }
        internal bool AssemblyWasRetained { get; private set; }
        internal bool AssemblyWasErasedAfterFinalize { get; private set; }
        internal int MaximumWriteSize { get; private set; }
        internal long CompleteAssemblyLength { get; private set; }
        internal bool Finalized => state == State.Finalized;

        public async Task<C2AssemblyPrepareResult> PrepareAsync(
            C2AssemblyPreparationRequest request,
            Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
            CancellationToken cancellationToken)
        {
            await prepareGate.WaitAsync(cancellationToken);
            try
            {
                if (request.CompleteAssemblyLength is < 1 or > 32 * 1024 * 1024)
                    return new(C2AssemblyPrepareOutcome.Conflict, null);
                if (state != State.None)
                    return preparationId == request.C2PreparationId && Fixed(assemblyFingerprint, request.AssemblyFingerprint)
                        ? new(C2AssemblyPrepareOutcome.ExistingMatch, receipt?.ToArray())
                        : new(C2AssemblyPrepareOutcome.Conflict, null);
                PrepareCount++;
                await using var sink = new ExactBoundedBufferStream(request.CompleteAssemblyLength);
                await boundedAssemblyWriter(sink, cancellationToken);
                if (sink.Length != request.CompleteAssemblyLength)
                    return new(C2AssemblyPrepareOutcome.Conflict, null);
                MaximumWriteSize = sink.MaximumWriteSize;
                CompleteAssemblyLength = request.CompleteAssemblyLength;
                assemblyFingerprint = request.AssemblyFingerprint.ToArray();
                preparationId = request.C2PreparationId;
                retainedAssembly = sink.Detach();
                receipt = SHA256.HashData(retainedAssembly);
                AssemblyWasRetained = retainedAssembly.Length == request.CompleteAssemblyLength;
                state = State.Prepared;
                if (afterPrepare is not null) await afterPrepare();
                if (loseFirstPrepareResponse && PrepareCount == 1)
                    return new(C2AssemblyPrepareOutcome.OutcomeUnknown, null);
                return new(C2AssemblyPrepareOutcome.Prepared, receipt.ToArray());
            }
            finally
            {
                prepareGate.Release();
            }
        }

        public Task<C2AssemblyInspection> GetPreparationAsync(Guid c2PreparationId, CancellationToken cancellationToken)
        {
            InspectCount++;
            if (state != State.None && c2PreparationId != preparationId)
                return Task.FromResult(new C2AssemblyInspection(C2AssemblyInspectionOutcome.Conflict, null, null));
            return Task.FromResult(state switch
            {
                State.None => new C2AssemblyInspection(C2AssemblyInspectionOutcome.Missing, null, null),
                State.Prepared => new C2AssemblyInspection(C2AssemblyInspectionOutcome.Prepared, assemblyFingerprint?.ToArray(), receipt?.ToArray()),
                State.Finalized => new C2AssemblyInspection(C2AssemblyInspectionOutcome.Finalized, assemblyFingerprint?.ToArray(), receipt?.ToArray()),
                State.Aborted => new C2AssemblyInspection(C2AssemblyInspectionOutcome.Aborted, assemblyFingerprint?.ToArray(), receipt?.ToArray()),
                _ => new C2AssemblyInspection(C2AssemblyInspectionOutcome.Conflict, null, null),
            });
        }

        public Task<C2AssemblyFinalizeResult> FinalizeAsync(Guid c2PreparationId, byte[] fingerprint, CancellationToken cancellationToken)
        {
            FinalizeCount++;
            if (c2PreparationId != preparationId)
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.Conflict));
            if (unavailableFirstFinalize && FinalizeCount == 1)
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.Unavailable));
            if (state == State.Finalized && Fixed(assemblyFingerprint, fingerprint))
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.ExistingMatch));
            if (state != State.Prepared || !Fixed(assemblyFingerprint, fingerprint))
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.Conflict));
            if (retainedAssembly is not null)
            {
                CryptographicOperations.ZeroMemory(retainedAssembly);
                retainedAssembly = null;
                AssemblyWasErasedAfterFinalize = true;
            }
            state = State.Finalized;
            return Task.FromResult(new C2AssemblyFinalizeResult(
                loseFirstFinalizeResponse
                    ? C2AssemblyFinalizeOutcome.OutcomeUnknown
                    : C2AssemblyFinalizeOutcome.Finalized));
        }

        public Task<C2AssemblyAbortResult> AbortAsync(Guid c2PreparationId, byte[] authorizationDigest, CancellationToken cancellationToken)
        {
            AbortCount++;
            if (c2PreparationId != preparationId || authorizationDigest.Length != 32)
                return Task.FromResult(new C2AssemblyAbortResult(C2AssemblyAbortOutcome.Conflict));
            if (state == State.Aborted && Fixed(abortAuthorizationDigest, authorizationDigest))
                return Task.FromResult(new C2AssemblyAbortResult(C2AssemblyAbortOutcome.ExistingMatch));
            if (state != State.Prepared)
                return Task.FromResult(new C2AssemblyAbortResult(C2AssemblyAbortOutcome.Conflict));
            if (retainedAssembly is not null)
            {
                CryptographicOperations.ZeroMemory(retainedAssembly);
                retainedAssembly = null;
            }
            abortAuthorizationDigest = authorizationDigest.ToArray();
            state = State.Aborted;
            return Task.FromResult(new C2AssemblyAbortResult(C2AssemblyAbortOutcome.Aborted));
        }

        private static bool Fixed(byte[]? left, byte[] right) =>
            left is not null && left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);

        private enum State { None, Prepared, Finalized, Aborted }
    }

    private sealed class ExactBoundedBufferStream : Stream
    {
        private byte[]? buffer;
        private int length;

        internal ExactBoundedBufferStream(long maximumLength)
        {
            if (maximumLength is < 1 or > 32 * 1024 * 1024 || maximumLength > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maximumLength));
            buffer = GC.AllocateUninitializedArray<byte>((int)maximumLength);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => length;
        public override long Position { get => Length; set => throw new NotSupportedException(); }
        internal int MaximumWriteSize { get; private set; }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> source)
        {
            var destination = buffer ?? throw new ObjectDisposedException(nameof(ExactBoundedBufferStream));
            if (source.Length > destination.Length - length) throw new IOException("C2 fixture bound exceeded.");
            MaximumWriteSize = Math.Max(MaximumWriteSize, source.Length);
            source.CopyTo(destination.AsSpan(length));
            length += source.Length;
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
        internal byte[] Detach()
        {
            var result = buffer ?? throw new ObjectDisposedException(nameof(ExactBoundedBufferStream));
            if (length != result.Length) throw new InvalidOperationException("C2 fixture assembly is incomplete.");
            buffer = null;
            return result;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && buffer is not null)
            {
                CryptographicOperations.ZeroMemory(buffer);
                buffer = null;
            }
            base.Dispose(disposing);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
