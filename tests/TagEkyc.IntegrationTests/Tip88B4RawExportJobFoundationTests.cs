using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B4RawExportJobFoundationTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private static readonly Guid AdminPrincipal =
        Guid.Parse("88b34000-0000-5000-8000-000000000001");
    private static readonly Guid RecorderPrincipal =
        Guid.Parse("88b34000-0000-5000-8000-000000000002");
    private static readonly Guid WithdrawerPrincipal =
        Guid.Parse("88b34000-0000-5000-8000-000000000006");
    private const string CurrentMigration = "20260815120000_Tip88C1C1ResolverAssembly";

    private static readonly string[] RepositoryMethods =
    [
        "Bind",
        "Read",
        "Acquire",
        "Renew",
        "Failure",
        "Terminalize",
    ];

    private static readonly string[] Tables =
    [
        "raw_export_job_attempts",
        "raw_export_job_classes",
        "raw_export_job_identities",
        "raw_export_job_operational_heads",
        "raw_export_job_source_bindings",
        "raw_export_job_transitions",
    ];

    private static readonly string[] B4OwnedTables =
    [
        "raw_export_job_attempts",
        "raw_export_job_classes",
        "raw_export_job_identities",
        "raw_export_job_operational_heads",
        "raw_export_job_transitions",
    ];

    private static readonly string[] Columns =
    [
        "raw_export_job_attempts|AcquiredAt",
        "raw_export_job_attempts|AttemptId",
        "raw_export_job_attempts|AttemptOrdinal",
        "raw_export_job_attempts|FencingToken",
        "raw_export_job_attempts|InitialLeaseExpiresAt",
        "raw_export_job_attempts|JobId",
        "raw_export_job_attempts|LeaseOwnerId",
        "raw_export_job_attempts|Phase",
        "raw_export_job_classes|JobId",
        "raw_export_job_classes|Ordinal",
        "raw_export_job_classes|RawClass",
        "raw_export_job_identities|AuthorizationDecisionId",
        "raw_export_job_identities|ClientApplicationId",
        "raw_export_job_identities|CreatedAt",
        "raw_export_job_identities|CreatedByApiKeyId",
        "raw_export_job_identities|ExportMode",
        "raw_export_job_identities|IdempotencyFingerprintHash",
        "raw_export_job_identities|IdempotencyKey",
        "raw_export_job_identities|JobExpiresAt",
        "raw_export_job_identities|JobId",
        "raw_export_job_identities|PermitExpiresAt",
        "raw_export_job_identities|PermitId",
        "raw_export_job_identities|PolicyId",
        "raw_export_job_identities|PolicyVersion",
        "raw_export_job_identities|PrincipalId",
        "raw_export_job_identities|PurposeCode",
        "raw_export_job_identities|RecipientClientApplicationId",
        "raw_export_job_identities|SchemaVersion",
        "raw_export_job_identities|SubjectRef",
        "raw_export_job_identities|VerificationSessionId",
        "raw_export_job_operational_heads|CurrentAttemptId",
        "raw_export_job_operational_heads|CurrentState",
        "raw_export_job_operational_heads|FencingToken",
        "raw_export_job_operational_heads|JobId",
        "raw_export_job_operational_heads|LeaseExpiresAt",
        "raw_export_job_operational_heads|LeaseOwnerId",
        "raw_export_job_operational_heads|Revision",
        "raw_export_job_operational_heads|UpdatedAt",
        "raw_export_job_source_bindings|AbsoluteSourceExpiresAtUtc",
        "raw_export_job_source_bindings|AttemptKeyReservationId",
        "raw_export_job_source_bindings|AuthorityRevision",
        "raw_export_job_source_bindings|AuthoritySnapshotId",
        "raw_export_job_source_bindings|AuthoritySnapshotSchemaVersion",
        "raw_export_job_source_bindings|BindingFingerprint",
        "raw_export_job_source_bindings|CaptureAcceptanceId",
        "raw_export_job_source_bindings|CaptureArtifactId",
        "raw_export_job_source_bindings|CaptureRevision",
        "raw_export_job_source_bindings|ConsentPolicyId",
        "raw_export_job_source_bindings|ConsentPolicyVersion",
        "raw_export_job_source_bindings|ContentCommitment",
        "raw_export_job_source_bindings|ContentCommitmentKeyId",
        "raw_export_job_source_bindings|ContentCommitmentKeyVersion",
        "raw_export_job_source_bindings|ContentCommitmentSchemaVersion",
        "raw_export_job_source_bindings|ControllerIdentity",
        "raw_export_job_source_bindings|CreatedAtUtc",
        "raw_export_job_source_bindings|EffectivePlaintextRetentionExpiresAtUtc",
        "raw_export_job_source_bindings|EncryptionAttemptFence",
        "raw_export_job_source_bindings|EncryptionAttemptId",
        "raw_export_job_source_bindings|EncryptionAttemptRevision",
        "raw_export_job_source_bindings|JobId",
        "raw_export_job_source_bindings|JobSourceBindingId",
        "raw_export_job_source_bindings|MediaType",
        "raw_export_job_source_bindings|ObjectCustodyId",
        "raw_export_job_source_bindings|ObjectStateRevision",
        "raw_export_job_source_bindings|Ordinal",
        "raw_export_job_source_bindings|PlaintextLength",
        "raw_export_job_source_bindings|RawClass",
        "raw_export_job_source_bindings|SchemaVersion",
        "raw_export_job_source_bindings|SessionCaptureSelectionId",
        "raw_export_job_source_bindings|SourceArtifactId",
        "raw_export_job_source_bindings|SourcePublicationId",
        "raw_export_job_source_bindings|SourcePublicationRevision",
        "raw_export_job_source_bindings|StableDataScopeId",
        "raw_export_job_source_bindings|SubjectRefToken",
        "raw_export_job_source_bindings|SubjectRefTokenKeyId",
        "raw_export_job_source_bindings|SubjectRefTokenKeyVersion",
        "raw_export_job_source_bindings|SubjectRefTokenSchemaVersion",
        "raw_export_job_source_bindings|VerificationSessionId",
        "raw_export_job_transitions|AttemptId",
        "raw_export_job_transitions|EventType",
        "raw_export_job_transitions|FailureCode",
        "raw_export_job_transitions|FencingToken",
        "raw_export_job_transitions|FromState",
        "raw_export_job_transitions|JobId",
        "raw_export_job_transitions|OccurredAt",
        "raw_export_job_transitions|ResultingLeaseExpiresAt",
        "raw_export_job_transitions|ResultingLeaseOwnerId",
        "raw_export_job_transitions|ResultingRevision",
        "raw_export_job_transitions|ToState",
        "raw_export_job_transitions|TransitionId",
    ];

    private static readonly string[] Constraints =
    [
        "CK_b4_job_class_ordinal",
        "CK_b4_job_class_raw_class",
        "CK_b4_job_head_shape",
        "CK_b4_job_identity_deadlines",
        "CK_b4_job_identity_export_mode",
        "CK_b4_job_identity_fingerprint_length",
        "CK_b4_job_identity_policy_version",
        "CK_b4_job_identity_schema_version",
        "CK_b4_job_transition_event_shape",
        "CK_b4_job_transition_revision",
        "CK_raw_export_job_attempts_AttemptOrdinal",
        "CK_raw_export_job_attempts_FencingToken",
        "CK_raw_export_job_attempts_LeaseTime",
        "CK_raw_export_job_attempts_Phase",
        "CK_raw_export_job_identities_IdempotencyKey",
        "ck_raw_export_job_source_binding_shape",
        "FK_b4_job_attempt_job",
        "FK_b4_job_class_job",
        "FK_b4_job_head_attempt",
        "FK_b4_job_head_job",
        "FK_b4_job_identity_decision",
        "FK_b4_job_identity_permit",
        "FK_b4_job_identity_session",
        "FK_b4_job_transition_attempt",
        "FK_b4_job_transition_job",
        "fk_raw_export_job_source_binding_attempt",
        "fk_raw_export_job_source_binding_job",
        "fk_raw_export_job_source_binding_key",
        "fk_raw_export_job_source_binding_object",
        "fk_raw_export_job_source_binding_publication",
        "fk_raw_export_job_source_binding_selection",
        "PK_b4_job_attempts",
        "PK_b4_job_classes",
        "PK_b4_job_identities",
        "PK_b4_job_operational_heads",
        "PK_b4_job_transitions",
        "pk_raw_export_job_source_bindings",
        "UQ_b4_job_attempt_fence",
        "UQ_b4_job_attempt_ordinal",
        "UQ_b4_job_class_ordinal",
        "UQ_b4_job_identity_permit",
        "UQ_b4_job_transition_revision",
        "uq_raw_export_job_source_binding_class",
        "uq_raw_export_job_source_binding_ordinal",
        "tr_b4_job_identity_has_classes",
    ];

    private static readonly string[] Indexes =
    [
        "IX_b4_job_head_attempt_fence",
        "IX_b4_job_identity_decision",
        "IX_b4_job_identity_session",
        "IX_b4_job_transition_attempt_fence",
        "IX_raw_export_job_source_bindings_AttemptKeyReservationId",
        "IX_raw_export_job_source_bindings_EncryptionAttemptId",
        "IX_raw_export_job_source_bindings_ObjectCustodyId",
        "IX_raw_export_job_source_bindings_SessionCaptureSelectionId",
        "IX_raw_export_job_source_bindings_SourcePublicationId",
        "PK_b4_job_attempts",
        "PK_b4_job_classes",
        "PK_b4_job_identities",
        "PK_b4_job_operational_heads",
        "PK_b4_job_transitions",
        "pk_raw_export_job_source_bindings",
        "UQ_b4_job_attempt_fence",
        "UQ_b4_job_attempt_ordinal",
        "UQ_b4_job_class_ordinal",
        "UQ_b4_job_identity_permit",
        "UQ_b4_job_transition_revision",
        "uq_raw_export_job_source_binding_class",
        "uq_raw_export_job_source_binding_ordinal",
    ];

    private static readonly string[] Triggers =
    [
        "tr_b4_job_attempt_append_only",
        "tr_b4_job_attempt_insert_guard",
        "tr_b4_job_class_append_only",
        "tr_b4_job_class_insert_guard",
        "tr_b4_job_head_mutation_guard",
        "tr_b4_job_identity_append_only",
        "tr_b4_job_identity_has_classes",
        "tr_b4_job_identity_insert_guard",
        "tr_b4_job_transition_append_only",
        "tr_b4_job_transition_insert_guard",
    ];

    private static readonly string[] Functions =
    [
        "enforce_raw_export_job_attempt_insert",
        "enforce_raw_export_job_class_insert",
        "enforce_raw_export_job_head_mutation",
        "enforce_raw_export_job_identity_has_classes",
        "enforce_raw_export_job_identity_insert",
        "enforce_raw_export_job_transition_insert",
        "raw_export_acquire_or_reclaim_job_lease",
        "raw_export_claim_or_read_job",
        "raw_export_freeze_job_source_bindings",
        "raw_export_lock_job_for_attempt",
        "raw_export_read_job",
        "raw_export_read_job_binding_inputs",
        "raw_export_read_job_source_verification_context",
        "raw_export_record_job_attempt_failure",
        "raw_export_renew_job_lease",
        "raw_export_terminalize_job",
    ];

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task M1_intended_b4_identifiers_fit_and_round_trip_exactly()
    {
        var names = Tables
            .Concat(Columns.Select(value => value[(value.IndexOf('|') + 1)..]))
            .Concat(Constraints)
            .Concat(Indexes)
            .Concat(Triggers)
            .Concat(Functions)
            .ToArray();
        Assert.All(
            names,
            name => Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(name), 1, 63));

        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        var actual = await ReadCatalogManifestAsync(db);

        Assert.Equal(Tables.Order(), actual["TABLE"].Order());
        Assert.Equal(Columns.Order(), actual["COLUMN"].Order());
        Assert.Equal(Constraints.Order(), actual["CONSTRAINT"].Order());
        Assert.Equal(Indexes.Order(), actual["INDEX"].Order());
        Assert.Equal(Triggers.Order(), actual["TRIGGER"].Order());
        Assert.Equal(Functions.Order(), actual["FUNCTION"].Order());
    }

    [Fact]
    public async Task M2_bind_freezes_exact_authoritative_identity_mode_and_classes()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage]);
        var permit = await db.RawExportAuthorizationPermits.SingleAsync(
            item => item.PermitId == fixture.PermitId);
        var decision = await db.RawExportAuthorizationDecisions.SingleAsync(
            item => item.ExportDecisionId == fixture.DecisionId);
        var policy = await db.RawExportPolicyVersions.SingleAsync(
            item => item.PolicyId == fixture.PolicyId && item.PolicyVersion == 1);
        var databaseBefore = await ReadDatabaseClockAsync(db);
        var repository = CreateJobRepository(db);

        var result = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m2-bind"));

        Assert.Equal(RawExportJobBindStatus.NewJob, result.Status);
        var read = await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            result.JobId));
        var databaseAfter = await ReadDatabaseClockAsync(db);
        Assert.Equal(RawExportJobReadStatus.Found, read.Status);
        Assert.NotNull(read.Job);
        Assert.Equal(fixture.PermitId, read.Job!.Identity.PermitId);
        Assert.Equal(fixture.DecisionId, read.Job.Identity.AuthorizationDecisionId);
        Assert.Equal(fixture.SessionId, read.Job.Identity.VerificationSessionId);
        Assert.Equal(fixture.PolicyId, read.Job.Identity.PolicyId);
        Assert.Equal(permit.PolicyVersion, read.Job.Identity.PolicyVersion);
        Assert.Equal(permit.SubjectRef, read.Job.Identity.SubjectRef);
        Assert.Equal(permit.PurposeCode, read.Job.Identity.PurposeCode);
        Assert.Equal(
            permit.RecipientClientApplicationId,
            read.Job.Identity.RecipientClientApplicationId);
        Assert.Equal(Enum.Parse<RawExportMode>(policy.Mode), read.Job.Identity.ExportMode);
        Assert.Equal(Tip88B34AuthorizationEngineTests.Actor.PrincipalId, read.Job.Identity.PrincipalId);
        Assert.Equal(Tip88B34AuthorizationEngineTests.Actor.ClientApplicationId, read.Job.Identity.ClientApplicationId);
        Assert.Equal(Tip88B34AuthorizationEngineTests.Actor.ApiKeyId, read.Job.Identity.CreatedByApiKeyId);
        Assert.Equal(decision.PrincipalId, read.Job.Identity.PrincipalId);
        Assert.Equal(decision.ClientApplicationId, read.Job.Identity.ClientApplicationId);
        Assert.Equal(permit.DecisionExpiresAtUtc, read.Job.Identity.PermitExpiresAt);
        Assert.Equal(read.Job.Identity.PermitExpiresAt, read.Job.Identity.JobExpiresAt);
        Assert.Equal(1, read.Job.Identity.SchemaVersion);
        Assert.InRange(read.Job.Identity.CreatedAt, databaseBefore, databaseAfter);
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage],
            read.Job.Classes.OrderBy(item => item.Ordinal).Select(item => item.RawClass));
        Assert.Equal(RawExportJobState.Claimed, read.Job.Head.CurrentState);
        Assert.Equal(0, read.Job.Head.Revision);
        Assert.Equal(0, read.Job.Head.FencingToken);
        Assert.Equal(RawExportJobEventType.JobBound, read.Job.LatestTransition.LatestEventType);

        Assert.Equal(1, await db.RawExportJobIdentities.CountAsync());
        Assert.Equal(2, await db.RawExportJobClasses.CountAsync());
        Assert.Equal(1, await db.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(1, await db.RawExportJobTransitions.CountAsync());
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync());
    }

    [Fact]
    public async Task M3_concurrent_bind_same_fingerprint_returns_one_job()
    {
        await using var seed = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(seed, [RawExportRawClass.LiveSelfieImage]);
        await using var db1 = postgres.CreateDbContext();
        await using var db2 = postgres.CreateDbContext();
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-same");

        var results = await Task.WhenAll(
            CreateJobRepository(db1).BindAsync(command),
            CreateJobRepository(db2).BindAsync(command));

        Assert.Equal(results[0].JobId, results[1].JobId);
        Assert.Equal(
            [RawExportJobBindStatus.NewJob, RawExportJobBindStatus.ExistingMatch],
            results.Select(item => item.Status).Order());
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(1, await verify.RawExportJobIdentities.CountAsync());
        Assert.Equal(1, await verify.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(1, await verify.RawExportJobTransitions.CountAsync());
        Assert.Equal(1, await verify.RawExportJobClasses.CountAsync());
    }

    [Fact]
    public async Task M3_concurrent_bind_different_fingerprint_fails_closed()
    {
        await using var seed = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(seed, [RawExportRawClass.LiveSelfieImage]);
        await using var db1 = postgres.CreateDbContext();
        await using var db2 = postgres.CreateDbContext();
        var first = CreateJobRepository(db1).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-a"));
        var second = CreateJobRepository(db2).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-b"));

        var outcomes = await Task.WhenAll(
            CaptureAsync(first),
            CaptureAsync(second));

        Assert.Single(outcomes, item => item.Result is not null);
        var failure = Assert.Single(outcomes, item => item.Exception is not null).Exception;
        Assert.Equal("RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT", Assert.IsType<RawExportJobException>(failure).Code);
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(1, await verify.RawExportJobIdentities.CountAsync());
        Assert.Equal(1, await verify.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(1, await verify.RawExportJobTransitions.CountAsync());
        Assert.Equal(1, await verify.RawExportJobClasses.CountAsync());
    }

    [Fact]
    public async Task M3_lost_response_replay_returns_existing_job_after_authority_changes()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-replay");
        var first = await CreateJobRepository(db).BindAsync(command);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tagekyc.verification_sessions
            SET "State"='Cancelled'
            WHERE "Id"={fixture.SessionId};
            """);

        var replay = await CreateJobRepository(db).BindAsync(command);

        Assert.Equal(RawExportJobBindStatus.ExistingMatch, replay.Status);
        Assert.Equal(first.JobId, replay.JobId);
        Assert.Equal(1, await db.RawExportJobIdentities.CountAsync());
    }

    [Fact]
    public async Task M4_api_key_rotation_preserves_fingerprint_and_original_creator()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var firstActor = Tip88B34AuthorizationEngineTests.Actor;
        var rotatedActor = firstActor with { ApiKeyId = Guid.NewGuid() };
        var repository = CreateJobRepository(db);
        var first = await repository.BindAsync(new(
            firstActor,
            fixture.PermitId,
            "b4-m4-api-key-rotation"));
        var replay = await repository.BindAsync(new(
            rotatedActor,
            fixture.PermitId,
            "b4-m4-api-key-rotation"));

        Assert.Equal(RawExportJobBindStatus.NewJob, first.Status);
        Assert.Equal(RawExportJobBindStatus.ExistingMatch, replay.Status);
        Assert.Equal(first.JobId, replay.JobId);
        var identity = await db.RawExportJobIdentities.SingleAsync();
        Assert.Equal(firstActor.ApiKeyId, identity.CreatedByApiKeyId);
        Assert.NotEqual(rotatedActor.ApiKeyId, identity.CreatedByApiKeyId);
    }

    [Fact]
    public async Task M3_committed_graph_invalid_claimed_terminalizes_and_returns_terminal()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-graph-claimed");
        var bind = await repository.BindAsync(command);
        var permitClass = await db.RawExportPermitClasses.SingleAsync(
            item => item.PermitId == fixture.PermitId);
        await SetPermitClassPresenceAsync(permitClass, present: false);
        try
        {
            var terminal = await repository.BindAsync(command);
            var replay = await repository.BindAsync(command);

            Assert.Equal(RawExportJobBindStatus.Terminal, terminal.Status);
            Assert.Equal(bind.JobId, terminal.JobId);
            Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.TerminalResult!.Status);
            Assert.Equal(RawExportJobState.TerminalFailed, terminal.TerminalResult.State);
            Assert.Equal(
                RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE,
                terminal.TerminalResult.TerminalReason);
            Assert.Equal(RawExportJobBindStatus.Terminal, replay.Status);
            Assert.Equal(RawExportJobTerminalizeStatus.AlreadyTerminal, replay.TerminalResult!.Status);
            Assert.Equal(terminal.TerminalResult.Revision, replay.TerminalResult.Revision);
        }
        finally
        {
            await SetPermitClassPresenceAsync(permitClass, present: true);
        }
    }

    [Fact]
    public async Task M3_committed_graph_invalid_active_lease_uses_head_first_terminalization()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-graph-active");
        var bind = await repository.BindAsync(command);
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));
        var permitClass = await db.RawExportPermitClasses.SingleAsync(
            item => item.PermitId == fixture.PermitId);
        await SetPermitClassPresenceAsync(permitClass, present: false);
        try
        {
            var terminal = await repository.BindAsync(command);

            Assert.Equal(RawExportJobBindStatus.Terminal, terminal.Status);
            Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.TerminalResult!.Status);
            Assert.Equal(RawExportJobState.TerminalFailed, terminal.TerminalResult.State);
            Assert.Equal(
                RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE,
                terminal.TerminalResult.TerminalReason);
            var head = await db.RawExportJobOperationalHeads.SingleAsync(
                item => item.JobId == bind.JobId);
            Assert.Equal("TerminalFailed", head.CurrentState);
            Assert.Equal(acquired.AttemptId, head.CurrentAttemptId);
            Assert.Null(head.LeaseOwnerId);
            Assert.Null(head.LeaseExpiresAt);
        }
        finally
        {
            await SetPermitClassPresenceAsync(permitClass, present: true);
        }
    }

    [Fact]
    public async Task M3_committed_graph_invalid_deadline_crossing_returns_expired()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 3);
        var repository = CreateJobRepository(db);
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-graph-expired");
        var bind = await repository.BindAsync(command);
        var read = await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId));
        var permitClass = await db.RawExportPermitClasses.SingleAsync(
            item => item.PermitId == fixture.PermitId);
        await SetPermitClassPresenceAsync(permitClass, present: false);
        try
        {
            await WaitUntilAfterAsync(read.Job!.Identity.JobExpiresAt);
            var terminal = await repository.BindAsync(command);

            Assert.Equal(RawExportJobBindStatus.Terminal, terminal.Status);
            Assert.Equal(RawExportJobTerminalizeStatus.Expired, terminal.TerminalResult!.Status);
            Assert.Equal(RawExportJobState.Expired, terminal.TerminalResult.State);
            Assert.Equal(
                RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED,
                terminal.TerminalResult.TerminalReason);
        }
        finally
        {
            await SetPermitClassPresenceAsync(permitClass, present: true);
        }
    }

    [Fact]
    public async Task M3_committed_graph_invalid_already_terminal_returns_exact_result()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var command = new BindRawExportJobCommand(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-graph-terminal");
        var bind = await repository.BindAsync(command);
        var permitClass = await db.RawExportPermitClasses.SingleAsync(
            item => item.PermitId == fixture.PermitId);
        await SetPermitClassPresenceAsync(permitClass, present: false);
        try
        {
            var first = await repository.BindAsync(command);
            var second = await repository.BindAsync(command);

            Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, first.TerminalResult!.Status);
            Assert.Equal(RawExportJobTerminalizeStatus.AlreadyTerminal, second.TerminalResult!.Status);
            Assert.Equal(first.JobId, second.JobId);
            Assert.Equal(first.TerminalResult.State, second.TerminalResult.State);
            Assert.Equal(first.TerminalResult.Revision, second.TerminalResult.Revision);
            Assert.Equal(first.TerminalResult.FencingToken, second.TerminalResult.FencingToken);
            Assert.Equal(first.TerminalResult.TerminalReason, second.TerminalResult.TerminalReason);
            Assert.Equal(first.TerminalResult.StableCode, second.TerminalResult.StableCode);
            Assert.Equal(bind.JobId, second.JobId);
        }
        finally
        {
            await SetPermitClassPresenceAsync(permitClass, present: true);
        }
    }

    [Fact]
    public async Task M3_committed_graph_invalid_stale_tuple_rolls_back_without_mutation()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-stale"));
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));
        var transitionsBefore = await db.RawExportJobTransitions.CountAsync();

        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.TerminalizeAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                AttemptId: null,
                LeaseOwnerId: null,
                RawExportJobState.TerminalFailed,
                RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE)));

        Assert.Equal("RAW_EXPORT_JOB_CONCURRENCY_CONFLICT", exception.Code);
        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        Assert.Equal(acquired.Revision, head.Revision);
        Assert.Equal(acquired.FencingToken, head.FencingToken);
        Assert.Equal(transitionsBefore, await db.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M3_bind_rejects_caller_owned_or_ambient_transaction()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                fixture.PermitId,
                "b4-m3-ambient")));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.Code);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        Assert.Equal(0, await CountWithSeparateContextAsync(context => context.RawExportJobIdentities.CountAsync()));
    }

    [Fact]
    public async Task M3_bind_rejects_matching_read_committed_ambient_before_database()
    {
        await using var db = postgres.CreateDbContext();
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            new System.Transactions.TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
            },
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                Guid.NewGuid(),
                "b4-m3-ambient-rc")));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.Code);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        Assert.Equal(0, await CountWithSeparateContextAsync(context => context.RawExportJobIdentities.CountAsync()));
    }

    [Fact]
    public async Task M3_bind_preflight_precedes_transaction_ownership_failure()
    {
        await using var db = postgres.CreateDbContext();
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                Guid.NewGuid(),
                "invalid key")));

        Assert.Equal("RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED", exception.Code);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task M3_bind_rejects_active_read_committed_connection_transaction()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                fixture.PermitId,
                "b4-m3-active")));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.Code);
        Assert.Equal(0, await db.RawExportJobIdentities.CountAsync());
    }

    [Theory]
    [InlineData("Bind")]
    [InlineData("Read")]
    [InlineData("Acquire")]
    [InlineData("Renew")]
    [InlineData("Failure")]
    [InlineData("Terminalize")]
    public async Task B4_all_six_methods_reject_ambient_transaction_before_database(
        string method)
    {
        await using var db = postgres.CreateDbContext();
        var repository = CreateJobRepository(db);
        using (var scope = new System.Transactions.TransactionScope(
                   System.Transactions.TransactionScopeOption.Required,
                   System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
        {
            var ambient = await Assert.ThrowsAsync<RawExportJobException>(
                () => InvokeRepositoryMethodAsync(repository, method));
            Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", ambient.Code);
            Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        }
    }

    [Theory]
    [InlineData("Bind")]
    [InlineData("Read")]
    [InlineData("Acquire")]
    [InlineData("Renew")]
    [InlineData("Failure")]
    [InlineData("Terminalize")]
    public async Task B4_all_six_methods_reject_existing_ef_transaction_before_database(
        string method)
    {
        Assert.Contains(
            "db.Database.CurrentTransaction is not null",
            ReadTransactionAdmissionSource(),
            StringComparison.Ordinal);
        await using var db = postgres.CreateDbContext();
        var repository = CreateJobRepository(db);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted);
        var active = await Assert.ThrowsAsync<RawExportJobException>(
            () => InvokeRepositoryMethodAsync(repository, method));
        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", active.Code);
        Assert.Same(transaction, db.Database.CurrentTransaction);
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData("Bind")]
    [InlineData("Read")]
    [InlineData("Acquire")]
    [InlineData("Renew")]
    [InlineData("Failure")]
    [InlineData("Terminalize")]
    public async Task B4_all_six_methods_reject_existing_provider_transaction_before_database(
        string method)
    {
        Assert.Contains(
            "connection.State != ConnectionState.Closed",
            ReadTransactionAdmissionSource(),
            StringComparison.Ordinal);
        await using var db = postgres.CreateDbContext();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted);

        var active = await Assert.ThrowsAsync<RawExportJobException>(
            () => InvokeRepositoryMethodAsync(CreateJobRepository(db), method));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", active.Code);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        await transaction.RollbackAsync();
        await connection.CloseAsync();
    }

    [Theory]
    [InlineData("Bind")]
    [InlineData("Read")]
    [InlineData("Acquire")]
    [InlineData("Renew")]
    [InlineData("Failure")]
    [InlineData("Terminalize")]
    public async Task B4_all_six_methods_reject_open_connection_before_database(
        string method)
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();

        var active = await Assert.ThrowsAsync<RawExportJobException>(
            () => InvokeRepositoryMethodAsync(CreateJobRepository(db), method));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", active.Code);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(
            System.Data.ConnectionState.Open,
            db.Database.GetDbConnection().State);
        await db.Database.CloseConnectionAsync();
    }

    public static IEnumerable<object[]> AllSixPreflightAdmissionCases()
    {
        foreach (var method in RepositoryMethods)
        {
            foreach (var topology in new[] { "Ambient", "EfTransaction", "ProviderTransaction", "OpenConnection" })
            {
                yield return [method, topology];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllSixPreflightAdmissionCases))]
    public async Task B4_all_six_methods_preflight_precedes_transaction_admission(
        string method,
        string topology)
    {
        await using var db = postgres.CreateDbContext();
        var repository = CreateJobRepository(db);

        switch (topology)
        {
            case "Ambient":
                using (var scope = new System.Transactions.TransactionScope(
                           System.Transactions.TransactionScopeOption.Required,
                           System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
                {
                    await AssertRequestValidationAsync(repository, method);
                }
                break;
            case "EfTransaction":
                await using (var transaction = await db.Database.BeginTransactionAsync(
                                 System.Data.IsolationLevel.ReadCommitted))
                {
                    await AssertRequestValidationAsync(repository, method);
                    await transaction.RollbackAsync();
                }
                break;
            case "ProviderTransaction":
            {
                var connection = (NpgsqlConnection)db.Database.GetDbConnection();
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted);
                await AssertRequestValidationAsync(repository, method);
                await transaction.RollbackAsync();
                await connection.CloseAsync();
                break;
            }
            case "OpenConnection":
                await db.Database.OpenConnectionAsync();
                await AssertRequestValidationAsync(repository, method);
                await db.Database.CloseConnectionAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(topology), topology, null);
        }
    }

    [Theory]
    [InlineData(System.Data.IsolationLevel.RepeatableRead)]
    [InlineData(System.Data.IsolationLevel.Serializable)]
    public async Task M3_direct_claim_rejects_repeatable_read_and_serializable(
        System.Data.IsolationLevel isolation)
    {
        var exception = await InvokeDirectClaimExpectingFailureAsync(
            "valid-key",
            isolation);

        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.MessageText);
    }

    [Fact]
    public async Task M3_claim_function_permit_lock_wait_rechecks_physical_time()
    {
        AuthorizedPermitFixture fixture;
        DateTimeOffset deadline;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 3);
            deadline = (await setup.RawExportAuthorizationPermits.SingleAsync(
                item => item.PermitId == fixture.PermitId)).DecisionExpiresAtUtc;
        }
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockPermitAsync(blocker, blockerTransaction, fixture.PermitId);

        await using var caller = new NpgsqlConnection(postgres.ConnectionString);
        await caller.OpenAsync();
        await using var callerTransaction = await caller.BeginTransactionAsync();
        await SetActorAsync(
            caller,
            callerTransaction,
            Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());
        var claim = DirectClaimAsync(
            caller,
            callerTransaction,
            fixture.PermitId,
            "b4-m3-claim-wait");
        await Task.Delay(200);
        Assert.False(claim.IsCompleted);
        await WaitUntilAfterAsync(deadline);
        await blockerTransaction.RollbackAsync();
        var result = await claim;
        await callerTransaction.CommitAsync();

        Assert.Equal("AuthorityNotEffective", result.Outcome);
        Assert.Null(result.JobId);
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(0, await verify.RawExportJobIdentities.CountAsync());
    }

    [Fact]
    public async Task M3_blocked_bind_rechecks_physical_time_after_winner_rollback()
    {
        AuthorizedPermitFixture fixture;
        DateTimeOffset deadline;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 3);
            deadline = (await setup.RawExportAuthorizationPermits.SingleAsync(
                item => item.PermitId == fixture.PermitId)).DecisionExpiresAtUtc;
        }
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockPermitAsync(blocker, blockerTransaction, fixture.PermitId);
        await using var bindDb = postgres.CreateDbContext();
        var bind = CreateJobRepository(bindDb).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-blocked"));
        await Task.Delay(200);
        Assert.False(bind.IsCompleted);
        await WaitUntilAfterAsync(deadline);
        await blockerTransaction.RollbackAsync();

        var exception = await Assert.ThrowsAsync<RawExportJobException>(() => bind);
        Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", exception.Code);
        Assert.Equal(0, await bindDb.RawExportJobIdentities.CountAsync());
    }

    [Fact]
    public async Task M3_new_bind_permit_lock_wait_crossing_finite_authority_rolls_back()
    {
        AuthorizedPermitFixture fixture;
        var consentDeadline = DateTimeOffset.UtcNow.AddSeconds(3);
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 30,
                consentValidUntilUtc: consentDeadline);
        }
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockPermitAsync(blocker, blockerTransaction, fixture.PermitId);
        await using var bindDb = postgres.CreateDbContext();
        var bind = CreateJobRepository(bindDb).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m3-finite"));
        await Task.Delay(200);
        Assert.False(bind.IsCompleted);
        await WaitUntilAfterAsync(consentDeadline);
        await blockerTransaction.RollbackAsync();

        var exception = await Assert.ThrowsAsync<RawExportJobException>(() => bind);
        Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", exception.Code);
        Assert.Equal(0, await bindDb.RawExportJobIdentities.CountAsync());
        Assert.Equal(0, await bindDb.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(0, await bindDb.RawExportJobTransitions.CountAsync());
    }

    public static IEnumerable<object[]> AllSixCleanupExitCases()
    {
        foreach (var method in RepositoryMethods)
        {
            foreach (var exit in new[] { "Success", "TypedFailure", "ProviderException", "Cancellation" })
            {
                yield return [method, exit];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllSixCleanupExitCases))]
    public async Task B4_connection_is_closed_and_transaction_free_after_every_exit(
        string method,
        string exit)
    {
        await AssertConnectionLifecycleCellAsync(method, exit);
    }

    [Theory]
    [MemberData(nameof(AllSixCleanupExitCases))]
    public async Task B4_same_scope_landed_repository_can_reopen_after_B4(
        string method,
        string exit)
    {
        await AssertConnectionLifecycleCellAsync(method, exit);
    }

    [Fact]
    public async Task M7_lease_tokens_are_monotonic_and_attempts_are_unique()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-lease"));
        var owner = Guid.NewGuid();

        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            ExpectedRevision: 0,
            ExpectedFencingToken: 0,
            owner));

        Assert.Equal(RawExportJobLeaseStatus.Acquired, acquired.Status);
        Assert.NotNull(acquired.AttemptId);
        Assert.Equal(1, acquired.Revision);
        Assert.Equal(1, acquired.FencingToken);
        Assert.NotNull(acquired.LeaseExpiresAt);
        var attempt = await db.RawExportJobAttempts.SingleAsync();
        Assert.Equal(acquired.AttemptId, attempt.AttemptId);
        Assert.Equal(0, attempt.AttemptOrdinal);
        Assert.Equal(1, attempt.FencingToken);
        Assert.Equal("Assembling", attempt.Phase);
        Assert.True(attempt.InitialLeaseExpiresAt > attempt.AcquiredAt);
    }

    [Fact]
    public async Task M7_renewal_keeps_fence_and_increments_revision()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-renew"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));

        var renewed = await repository.RenewLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId!.Value,
            owner));

        Assert.Equal(RawExportJobRenewStatus.Renewed, renewed.Status);
        Assert.Equal(acquired.Revision + 1, renewed.Revision);
        Assert.Equal(acquired.FencingToken, renewed.FencingToken);
        Assert.True(renewed.LeaseExpiresAt >= acquired.LeaseExpiresAt);
        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        Assert.Equal(renewed.Revision, head.Revision);
        Assert.Equal(renewed.FencingToken, head.FencingToken);
        Assert.Equal(renewed.LeaseExpiresAt, head.LeaseExpiresAt);
        var transition = await db.RawExportJobTransitions
            .SingleAsync(item => item.ResultingRevision == renewed.Revision);
        Assert.Equal("LeaseRenewed", transition.EventType);
        Assert.Equal(renewed.LeaseExpiresAt, transition.ResultingLeaseExpiresAt);
    }

    [Fact]
    public async Task M7_mode_specific_retry_and_reclaim_fail_closed()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-no-retain"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));

        var failed = await repository.RecordAttemptFailureAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId!.Value,
            owner,
            RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE));

        Assert.Equal(RawExportJobAttemptFailureStatus.TerminalFailed, failed.Status);
        Assert.Equal(RawExportJobState.TerminalFailed, failed.State);
        Assert.Equal(RawExportJobTerminalReasonCode.MODE_RETRY_NOT_AUTHORIZED, failed.TerminalReason);
        Assert.Equal("RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED", failed.StableCode);
        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        Assert.Equal("TerminalFailed", head.CurrentState);
        Assert.Null(head.LeaseOwnerId);
        Assert.Null(head.LeaseExpiresAt);
        Assert.Equal(acquired.AttemptId, head.CurrentAttemptId);
        Assert.Contains(
            await db.RawExportJobTransitions.ToListAsync(),
            item => item.EventType == "JobTerminalFailed" &&
                    item.FailureCode == "MODE_RETRY_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task M7_retryable_release_and_reclaim_create_new_attempt_and_fence()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db, new RawExportJobLeaseState(10, true, null));
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-packet-retry"));
        var firstOwner = Guid.NewGuid();
        var first = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            firstOwner));
        var released = await repository.RecordAttemptFailureAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            first.Revision!.Value,
            first.FencingToken!.Value,
            first.AttemptId!.Value,
            firstOwner,
            RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE));
        var secondOwner = Guid.NewGuid();
        var second = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            released.Revision,
            released.FencingToken,
            secondOwner));

        Assert.Equal(RawExportJobAttemptFailureStatus.Recorded, released.Status);
        Assert.Equal(RawExportJobLeaseStatus.AcquiredAfterRetryableFailure, second.Status);
        Assert.NotEqual(first.AttemptId, second.AttemptId);
        Assert.Equal(first.FencingToken + 1, second.FencingToken);
        Assert.Equal(first.Revision + 2, second.Revision);
        var attempts = await db.RawExportJobAttempts
            .Where(item => item.JobId == bind.JobId)
            .OrderBy(item => item.AttemptOrdinal)
            .ToArrayAsync();
        Assert.Equal(2, attempts.Length);
        Assert.Equal([0, 1], attempts.Select(item => item.AttemptOrdinal));
        Assert.Equal([1L, 2L], attempts.Select(item => item.FencingToken));
    }

    [Fact]
    public async Task M7_public_terminalize_accepts_released_assembling_head()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-terminalize-released"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        var released = await repository.RecordAttemptFailureAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId!.Value,
            owner,
            RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE));

        var terminal = await repository.TerminalizeAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            released.Revision,
            released.FencingToken,
            acquired.AttemptId,
            LeaseOwnerId: null,
            RawExportJobState.Cancelled,
            RawExportJobTerminalReasonCode.REQUEST_CANCELLED));

        Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.Status);
        Assert.Equal(RawExportJobState.Cancelled, terminal.State);
        Assert.Equal(released.Revision + 1, terminal.Revision);
        Assert.Equal(released.FencingToken, terminal.FencingToken);
        var head = await db.RawExportJobOperationalHeads.SingleAsync(
            item => item.JobId == bind.JobId);
        Assert.Equal("Cancelled", head.CurrentState);
        Assert.Equal(acquired.AttemptId, head.CurrentAttemptId);
        Assert.Null(head.LeaseOwnerId);
        Assert.Null(head.LeaseExpiresAt);
    }

    [Fact]
    public async Task M7_acquire_and_reclaim_cap_lease_at_frozen_deadlines()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 5);
        var repository = CreateJobRepository(db, new RawExportJobLeaseState(300, true, null));
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-deadline-cap"));
        var read = await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId));
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));

        Assert.Equal(read.Job!.Identity.JobExpiresAt, read.Job.Identity.PermitExpiresAt);
        Assert.Equal(read.Job.Identity.JobExpiresAt, acquired.LeaseExpiresAt);
        Assert.True(acquired.LeaseExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task M7_direct_acquire_invalid_lease_bound_precedes_job_lookup()
    {
        foreach (var seconds in new int?[] { null, 9, 301 })
        {
            var exception = await InvokeDirectLeaseWithInvalidBoundAsync(
                "raw_export_acquire_or_reclaim_job_lease",
                passedPrincipal: Tip88B34AuthorizationEngineTests.Actor.PrincipalId,
                seconds);

            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_JOB_LEASE_CONFIG_INVALID", exception.MessageText);
        }
    }

    [Fact]
    public async Task M7_direct_renew_invalid_lease_bound_precedes_job_lookup()
    {
        foreach (var seconds in new int?[] { null, 9, 301 })
        {
            var exception = await InvokeDirectLeaseWithInvalidBoundAsync(
                "raw_export_renew_job_lease",
                passedPrincipal: Tip88B34AuthorizationEngineTests.Actor.PrincipalId,
                seconds);

            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_JOB_LEASE_CONFIG_INVALID", exception.MessageText);
        }
    }

    [Theory]
    [InlineData("raw_export_acquire_or_reclaim_job_lease")]
    [InlineData("raw_export_renew_job_lease")]
    public async Task M7_invalid_actor_precedes_invalid_lease_bound(string function)
    {
        var exception = await InvokeDirectLeaseWithInvalidBoundAsync(
            function,
            passedPrincipal: Guid.NewGuid());

        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_ACTOR_MISMATCH", exception.MessageText);
    }

    [Fact]
    public async Task M7_direct_acquire_folds_absent_stale_null_and_terminal_into_concurrency_conflict()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-direct-acquire-outcomes"));

        await AssertAcquireConcurrencyConflictAsync(Guid.NewGuid(), 0, 0);
        await AssertAcquireConcurrencyConflictAsync(bind.JobId, 1, 0);
        await AssertAcquireConcurrencyConflictAsync(bind.JobId, 0, 1);
        await AssertAcquireConcurrencyConflictAsync(bind.JobId, null, 0);
        await AssertAcquireConcurrencyConflictAsync(bind.JobId, 0, null);

        var terminal = await repository.TerminalizeAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            null,
            null,
            RawExportJobState.Cancelled,
            RawExportJobTerminalReasonCode.REQUEST_CANCELLED));
        Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.Status);
        await AssertAcquireConcurrencyConflictAsync(
            bind.JobId,
            terminal.Revision,
            terminal.FencingToken);
    }

    [Fact]
    public async Task M7_direct_entries_fail_closed_for_null_cas_and_transition_tokens()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-null-direct-arguments"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        Assert.NotNull(acquired.AttemptId);
        Assert.NotNull(acquired.Revision);
        Assert.NotNull(acquired.FencingToken);

        await AssertDirectOutcomeAsync(
            "lock", bind.JobId, null, acquired.FencingToken, null, null, null, null,
            "ConcurrencyConflict");
        await AssertDirectOutcomeAsync(
            "lock", bind.JobId, acquired.Revision, null, null, null, null, null,
            "FenceStale");
        foreach (var function in new[] { "renew", "failure", "terminalize" })
        {
            await AssertDirectOutcomeAsync(
                function,
                bind.JobId,
                null,
                acquired.FencingToken,
                acquired.AttemptId,
                owner,
                function == "renew" ? 60 : null,
                function == "failure"
                    ? "ATTEMPT_EXECUTION_FAILED_RETRYABLE"
                    : "TerminalFailed",
                "ConcurrencyConflict",
                function == "terminalize"
                    ? "ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE"
                    : null);
            await AssertDirectOutcomeAsync(
                function,
                bind.JobId,
                acquired.Revision,
                null,
                acquired.AttemptId,
                owner,
                function == "renew" ? 60 : null,
                function == "failure"
                    ? "ATTEMPT_EXECUTION_FAILED_RETRYABLE"
                    : "TerminalFailed",
                "FenceStale",
                function == "terminalize"
                    ? "ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE"
                    : null);
        }

        await AssertDirectOutcomeAsync(
            "failure",
            bind.JobId,
            acquired.Revision,
            acquired.FencingToken,
            acquired.AttemptId,
            owner,
            null,
            null,
            "TransitionInvalid");
        await AssertDirectOutcomeAsync(
            "terminalize",
            bind.JobId,
            acquired.Revision,
            acquired.FencingToken,
            acquired.AttemptId,
            owner,
            null,
            null,
            "TransitionInvalid",
            "ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE");
        await AssertDirectOutcomeAsync(
            "terminalize",
            bind.JobId,
            acquired.Revision,
            acquired.FencingToken,
            acquired.AttemptId,
            owner,
            null,
            "TerminalFailed",
            "TransitionInvalid",
            null);

        var head = await db.RawExportJobOperationalHeads.SingleAsync(
            item => item.JobId == bind.JobId);
        Assert.Equal(acquired.Revision, head.Revision);
        Assert.Equal(acquired.FencingToken, head.FencingToken);
    }

    [Fact]
    public async Task M7_blocked_acquire_rechecks_physical_time_after_head_lock()
    {
        await using var setup = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 3);
        var repository = CreateJobRepository(setup);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m7-blocked"));
        var read = await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId));
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockHeadAsync(blocker, blockerTransaction, bind.JobId);
        await using var attemptDb = postgres.CreateDbContext();
        var acquire = CreateJobRepository(attemptDb).AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));
        await Task.Delay(200);
        Assert.False(acquire.IsCompleted);
        await WaitUntilAfterAsync(read.Job!.Identity.JobExpiresAt);
        await blockerTransaction.RollbackAsync();
        var result = await acquire;

        Assert.Equal(RawExportJobLeaseStatus.Expired, result.Status);
        Assert.Equal(0, await attemptDb.RawExportJobAttempts.CountAsync());
        var transition = await attemptDb.RawExportJobTransitions.SingleAsync(
            item => item.JobId == bind.JobId && item.ResultingRevision == 1);
        Assert.Equal("JobExpired", transition.EventType);
        Assert.Equal("PERMIT_OR_JOB_EXPIRED", transition.FailureCode);
    }

    [Fact]
    public async Task M11_terminal_jobs_cannot_reenter_or_reacquire()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m11-terminal"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));

        var terminal = await repository.TerminalizeAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId,
            owner,
            RawExportJobState.Cancelled,
            RawExportJobTerminalReasonCode.REQUEST_CANCELLED));
        var reacquire = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            terminal.Revision,
            terminal.FencingToken,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.Status);
        Assert.Equal(RawExportJobState.Cancelled, terminal.State);
        Assert.Equal(RawExportJobLeaseStatus.AlreadyTerminal, reacquire.Status);
        Assert.Equal(RawExportJobState.Cancelled, reacquire.State);
        Assert.Null(reacquire.AttemptId);
        Assert.Null(reacquire.LeaseExpiresAt);
        Assert.Equal(terminal.Revision, reacquire.Revision);
        Assert.Equal(terminal.FencingToken, reacquire.FencingToken);
        Assert.Equal(1, await db.RawExportJobAttempts.CountAsync());
        Assert.Equal(3, await db.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M11_expiry_uses_database_time_and_never_extends_deadlines()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 3);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m11-expiry"));
        var before = await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId));
        await WaitUntilAfterAsync(before.Job!.Identity.JobExpiresAt);

        var expired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobLeaseStatus.Expired, expired.Status);
        Assert.Equal(RawExportJobState.Expired, expired.State);
        Assert.Equal(RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED, expired.TerminalReason);
        Assert.Null(expired.AttemptId);
        Assert.Null(expired.LeaseExpiresAt);
        Assert.Equal(0, expired.FencingToken);
        Assert.Equal(1, expired.Revision);
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync());
        var transition = await db.RawExportJobTransitions.SingleAsync(
            item => item.JobId == bind.JobId && item.ResultingRevision == 1);
        Assert.Equal("JobExpired", transition.EventType);
        Assert.Equal("PERMIT_OR_JOB_EXPIRED", transition.FailureCode);
    }

    [Fact]
    public async Task M11_new_bind_graph_invalid_rolls_back_without_job()
    {
        AuthorizedPermitFixture fixture;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage]);
        }
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(
            connection,
            transaction,
            Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => DirectClaimAsync(
                connection,
                transaction,
                fixture.PermitId,
                "b4-m11-graph",
                RawExportMode.EncryptedExportPacket));

        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE", exception.MessageText);
        await transaction.RollbackAsync();
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(0, await verify.RawExportJobIdentities.CountAsync());
        Assert.Equal(0, await verify.RawExportJobClasses.CountAsync());
        Assert.Equal(0, await verify.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(0, await verify.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M11_bound_revalidation_and_safe_graph_failures_terminalize_with_exact_evidence()
    {
        await using var db = postgres.CreateDbContext();
        var authorityFixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var authorityRepository = CreateJobRepository(db);
        var authorityBind = await authorityRepository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            authorityFixture.PermitId,
            "b4-m11-bound-authority"));
        await ApplyAuthorityMutationAsync(db, authorityFixture, "session");

        var authorityFailure = await authorityRepository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            authorityBind.JobId,
            ExpectedRevision: 0,
            ExpectedFencingToken: 0,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobLeaseStatus.TerminalFailed, authorityFailure.Status);
        Assert.Equal(RawExportJobState.TerminalFailed, authorityFailure.State);
        Assert.Equal(1, authorityFailure.Revision);
        Assert.Equal(0, authorityFailure.FencingToken);
        Assert.Null(authorityFailure.AttemptId);
        Assert.Null(authorityFailure.LeaseExpiresAt);
        Assert.Equal(
            RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED,
            authorityFailure.TerminalReason);
        Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", authorityFailure.StableCode);
        await AssertExactTerminalEvidenceAsync(
            db,
            authorityBind.JobId,
            "AUTHORITY_REVALIDATION_FAILED");

        var graphFixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.HandSignatureImage]);
        var graphBind = await CreateJobRepository(db).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            graphFixture.PermitId,
            "b4-m11-bound-graph"));
        var landedProjection = new EfRawExportAuthorizationProjectionReader(db);
        var graphRepository = new EfRawExportJobRepository(
            db,
            new EfRawExportControlPlaneRepository(db, landedProjection),
            new EvaluatedAtDriftProjectionReader(landedProjection),
            new EfRawExportSubjectConsentRepository(db),
            new RawExportJobLeaseState(60, true, null));

        var graphFailure = await graphRepository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            graphBind.JobId,
            ExpectedRevision: 0,
            ExpectedFencingToken: 0,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobLeaseStatus.TerminalFailed, graphFailure.Status);
        Assert.Equal(RawExportJobState.TerminalFailed, graphFailure.State);
        Assert.Equal(1, graphFailure.Revision);
        Assert.Equal(0, graphFailure.FencingToken);
        Assert.Null(graphFailure.AttemptId);
        Assert.Null(graphFailure.LeaseExpiresAt);
        Assert.Equal(
            RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE,
            graphFailure.TerminalReason);
        Assert.Equal("RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE", graphFailure.StableCode);
        await AssertExactTerminalEvidenceAsync(
            db,
            graphBind.JobId,
            "JOB_GRAPH_INVARIANT_FAILURE");
    }

    [Fact]
    public async Task M11_each_command_obeys_expiry_and_authority_precedence()
    {
        var finiteAuthorityDeadline = DateTimeOffset.UtcNow.AddSeconds(5);
        AuthorizedPermitFixture claimFixture;
        AuthorizedPermitFixture acquireFixture;
        AuthorizedPermitFixture workerFixture;
        DateTimeOffset absoluteDeadline;
        Guid acquireJob;
        Guid workerJob;
        long workerRevision;
        long workerFence;
        Guid workerAttempt;
        var workerOwner = Guid.NewGuid();

        await using (var setup = postgres.CreateDbContext())
        {
            claimFixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 5,
                consentValidUntilUtc: finiteAuthorityDeadline);
            acquireFixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.HandSignatureImage],
                permitTtlSeconds: 5,
                consentValidUntilUtc: finiteAuthorityDeadline);
            workerFixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.ChipDg1],
                permitTtlSeconds: 5,
                consentValidUntilUtc: finiteAuthorityDeadline);
            var repository = CreateJobRepository(setup);
            acquireJob = (await repository.BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                acquireFixture.PermitId,
                "b4-m11-precedence-acquire"))).JobId;
            workerJob = (await repository.BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                workerFixture.PermitId,
                "b4-m11-precedence-worker"))).JobId;
            var acquiredWorker = await repository.AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                workerJob,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                workerOwner));
            Assert.Equal(RawExportJobLeaseStatus.Acquired, acquiredWorker.Status);
            workerRevision = acquiredWorker.Revision ??
                throw new InvalidOperationException("Acquired worker revision was absent.");
            workerFence = acquiredWorker.FencingToken ??
                throw new InvalidOperationException("Acquired worker fence was absent.");
            workerAttempt = acquiredWorker.AttemptId ??
                throw new InvalidOperationException("Acquired worker attempt was absent.");
            absoluteDeadline = (await setup.RawExportJobIdentities
                .Where(item => item.JobId == workerJob)
                .Select(item => item.JobExpiresAt)
                .SingleAsync());
            var acquireDeadline = await setup.RawExportJobIdentities
                .Where(item => item.JobId == acquireJob)
                .Select(item => item.JobExpiresAt)
                .SingleAsync();
            if (acquireDeadline > absoluteDeadline)
            {
                absoluteDeadline = acquireDeadline;
            }
            var claimDeadline = await setup.RawExportAuthorizationPermits
                .Where(item => item.PermitId == claimFixture.PermitId)
                .Select(item => item.DecisionExpiresAtUtc)
                .SingleAsync();
            if (claimDeadline > absoluteDeadline)
            {
                absoluteDeadline = claimDeadline;
            }
            await ApplyAuthorityMutationAsync(setup, claimFixture, "session");
        }

        await WaitUntilAfterAsync(
            finiteAuthorityDeadline > absoluteDeadline
                ? finiteAuthorityDeadline
                : absoluteDeadline);

        await using (var claimConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await claimConnection.OpenAsync();
            await using var claimTransaction = await claimConnection.BeginTransactionAsync();
            await SetActorAsync(
                claimConnection,
                claimTransaction,
                Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());
            var claim = await DirectClaimAsync(
                claimConnection,
                claimTransaction,
                claimFixture.PermitId,
                "b4-m11-precedence-claim");
            Assert.Equal("AuthorityNotEffective", claim.Outcome);
            Assert.Null(claim.JobId);
            await claimTransaction.CommitAsync();
        }

        await using var db = postgres.CreateDbContext();
        var repositoryAfterExpiry = CreateJobRepository(db);
        var acquire = await repositoryAfterExpiry.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            acquireJob,
            ExpectedRevision: 0,
            ExpectedFencingToken: 0,
            Guid.NewGuid()));
        Assert.Equal(RawExportJobLeaseStatus.Expired, acquire.Status);
        Assert.Equal(RawExportJobState.Expired, acquire.State);
        Assert.Equal(
            RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED,
            acquire.TerminalReason);
        Assert.Null(acquire.AttemptId);
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync(
            item => item.JobId == acquireJob));

        var transitionsBeforeWorkerCommands = await db.RawExportJobTransitions.CountAsync(
            item => item.JobId == workerJob);
        var renew = await Assert.ThrowsAsync<RawExportJobException>(
            () => repositoryAfterExpiry.RenewLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                workerJob,
                workerRevision,
                workerFence,
                workerAttempt,
                workerOwner)));
        Assert.Equal("RAW_EXPORT_JOB_LEASE_NOT_HELD", renew.Code);
        var failure = await Assert.ThrowsAsync<RawExportJobException>(
            () => repositoryAfterExpiry.RecordAttemptFailureAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                workerJob,
                workerRevision,
                workerFence,
                workerAttempt,
                workerOwner,
                RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)));
        Assert.Equal("RAW_EXPORT_JOB_LEASE_NOT_HELD", failure.Code);
        Assert.Equal(
            transitionsBeforeWorkerCommands,
            await db.RawExportJobTransitions.CountAsync(item => item.JobId == workerJob));

        var terminal = await repositoryAfterExpiry.TerminalizeAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            workerJob,
            workerRevision,
            workerFence,
            workerAttempt,
            workerOwner,
            RawExportJobState.Cancelled,
            RawExportJobTerminalReasonCode.REQUEST_CANCELLED));
        Assert.Equal(RawExportJobTerminalizeStatus.Expired, terminal.Status);
        Assert.Equal(RawExportJobState.Expired, terminal.State);
        Assert.Equal(
            RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED,
            terminal.TerminalReason);
        var terminalHead = await db.RawExportJobOperationalHeads.SingleAsync(
            item => item.JobId == workerJob);
        Assert.Equal("Expired", terminalHead.CurrentState);
        Assert.Equal(workerRevision + 1, terminalHead.Revision);
        Assert.Equal(workerFence, terminalHead.FencingToken);
        Assert.Equal(workerAttempt, terminalHead.CurrentAttemptId);
        Assert.Null(terminalHead.LeaseOwnerId);
        Assert.Null(terminalHead.LeaseExpiresAt);
        var terminalTransition = await db.RawExportJobTransitions.SingleAsync(
            item => item.JobId == workerJob &&
                    item.ResultingRevision == workerRevision + 1);
        Assert.Equal("JobExpired", terminalTransition.EventType);
        Assert.Equal("PERMIT_OR_JOB_EXPIRED", terminalTransition.FailureCode);
        Assert.Equal(1, await db.RawExportJobAttempts.CountAsync(
            item => item.JobId == workerJob));
        Assert.Equal(0, await db.RawExportJobIdentities.CountAsync(
            item => item.PermitId == claimFixture.PermitId));
    }

    [Fact]
    public async Task M9_every_head_mutation_has_same_transaction_transition()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m9-ledger"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        var renewed = await repository.RenewLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId!.Value,
            owner));

        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        var revisions = await db.RawExportJobTransitions
            .OrderBy(item => item.ResultingRevision)
            .Select(item => item.ResultingRevision)
            .ToArrayAsync();
        Assert.Equal(renewed.Revision, head.Revision);
        Assert.Equal(Enumerable.Range(0, checked((int)head.Revision + 1)).Select(value => (long)value), revisions);
        Assert.Equal(head.Revision + 1, revisions.LongLength);
    }

    [Fact]
    public async Task M9_crash_after_lease_expiry_reclaims_same_job_without_duplicate_consumption()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db, new RawExportJobLeaseState(10, true, null));
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m9-crash"));
        var first = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));
        await WaitUntilAfterAsync(first.LeaseExpiresAt!.Value);
        var reclaimed = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            first.Revision!.Value,
            first.FencingToken!.Value,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobLeaseStatus.Reclaimed, reclaimed.Status);
        Assert.Equal(bind.JobId, (await repository.ReadAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId))).Job!.Identity.JobId);
        Assert.Equal(1, await db.RawExportJobIdentities.CountAsync());
        Assert.Equal(1, await db.RawExportJobClasses.CountAsync());
        Assert.Equal(2, await db.RawExportJobAttempts.CountAsync());
        Assert.Equal(3, await db.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M8_expired_unreclaimed_worker_cannot_renew_or_record_failure()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db, new RawExportJobLeaseState(10, true, null));
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m8-expired"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        await WaitUntilAfterAsync(acquired.LeaseExpiresAt!.Value);

        var renew = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.RenewLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                acquired.Revision!.Value,
                acquired.FencingToken!.Value,
                acquired.AttemptId!.Value,
                owner)));
        var failure = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.RecordAttemptFailureAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                acquired.Revision!.Value,
                acquired.FencingToken!.Value,
                acquired.AttemptId!.Value,
                owner,
                RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)));

        Assert.Equal("RAW_EXPORT_JOB_LEASE_NOT_HELD", renew.Code);
        Assert.Equal("RAW_EXPORT_JOB_LEASE_NOT_HELD", failure.Code);
        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        Assert.Equal(acquired.Revision, head.Revision);
        Assert.Equal(2, await db.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M8_stale_worker_cannot_mutate_after_reclaim()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db, new RawExportJobLeaseState(10, true, null));
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m8-reclaim"));
        var staleOwner = Guid.NewGuid();
        var first = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            staleOwner));
        await WaitUntilAfterAsync(first.LeaseExpiresAt!.Value);
        var reclaimed = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            first.Revision!.Value,
            first.FencingToken!.Value,
            Guid.NewGuid()));

        var renew = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.RenewLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                reclaimed.Revision!.Value,
                first.FencingToken.Value,
                first.AttemptId!.Value,
                staleOwner)));
        var failure = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.RecordAttemptFailureAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                reclaimed.Revision!.Value,
                first.FencingToken!.Value,
                first.AttemptId!.Value,
                staleOwner,
                RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)));

        Assert.Equal(RawExportJobLeaseStatus.Reclaimed, reclaimed.Status);
        Assert.Equal("RAW_EXPORT_JOB_FENCE_STALE", renew.Code);
        Assert.Equal("RAW_EXPORT_JOB_FENCE_STALE", failure.Code);
        var head = await db.RawExportJobOperationalHeads.SingleAsync();
        Assert.Equal(reclaimed.Revision, head.Revision);
        Assert.Equal(reclaimed.FencingToken, head.FencingToken);
    }

    [Fact]
    public async Task M8_head_first_terminalization_does_not_grant_stale_worker_authority()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPacketPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m8-terminal"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        var terminal = await repository.TerminalizeAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            acquired.Revision!.Value,
            acquired.FencingToken!.Value,
            acquired.AttemptId,
            owner,
            RawExportJobState.Cancelled,
            RawExportJobTerminalReasonCode.REQUEST_CANCELLED));

        var renew = await Assert.ThrowsAsync<RawExportJobException>(
            () => repository.RenewLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                terminal.Revision,
                terminal.FencingToken,
                acquired.AttemptId!.Value,
                owner)));

        Assert.Equal(RawExportJobTerminalizeStatus.Terminalized, terminal.Status);
        Assert.Equal("RAW_EXPORT_JOB_LEASE_NOT_HELD", renew.Code);
        Assert.Equal(1, await db.RawExportJobAttempts.CountAsync());
        Assert.Equal(3, await db.RawExportJobTransitions.CountAsync());
    }

    [Fact]
    public async Task M10_attempt_phase_check_rejects_non_assembling()
    {
        await AssertAttemptCheckAsync(
            phase: "Delivery",
            ordinal: 0,
            fence: 1,
            acquiredAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(1),
            "CK_raw_export_job_attempts_Phase");
    }

    [Fact]
    public async Task M10_attempt_ordinal_check_rejects_negative()
    {
        await AssertAttemptCheckAsync(
            phase: "Assembling",
            ordinal: -1,
            fence: 1,
            acquiredAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(1),
            "CK_raw_export_job_attempts_AttemptOrdinal");
    }

    [Fact]
    public async Task M10_attempt_fence_check_rejects_nonpositive()
    {
        await AssertAttemptCheckAsync(
            phase: "Assembling",
            ordinal: 0,
            fence: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(1),
            "CK_raw_export_job_attempts_FencingToken");
    }

    [Fact]
    public async Task M10_attempt_lease_time_check_rejects_nonincreasing()
    {
        var now = DateTimeOffset.UtcNow;
        await AssertAttemptCheckAsync(
            phase: "Assembling",
            ordinal: 0,
            fence: 1,
            acquiredAt: now,
            expiresAt: now,
            "CK_raw_export_job_attempts_LeaseTime");
    }

    [Fact]
    public async Task M10_identity_attempt_transition_and_head_guards_bite()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m10-guards"));
        var owner = Guid.NewGuid();
        var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));

        await AssertGuardFailureAsync(
            $"""UPDATE tagekyc.raw_export_job_identities SET "SubjectRef"="SubjectRef" WHERE "JobId"='{bind.JobId}'""",
            "RAW_EXPORT_JOB_IDENTITY_MUTATION_UNSUPPORTED");
        await AssertGuardFailureAsync(
            $"""UPDATE tagekyc.raw_export_job_attempts SET "Phase"="Phase" WHERE "AttemptId"='{acquired.AttemptId}'""",
            "RAW_EXPORT_JOB_ATTEMPT_MUTATION_UNSUPPORTED");
        await AssertGuardFailureAsync(
            $"""DELETE FROM tagekyc.raw_export_job_transitions WHERE "JobId"='{bind.JobId}' AND "ResultingRevision"=0""",
            "RAW_EXPORT_JOB_TRANSITION_MUTATION_UNSUPPORTED");
        await AssertGuardFailureAsync(
            $"""UPDATE tagekyc.raw_export_job_operational_heads SET "Revision"="Revision" WHERE "JobId"='{bind.JobId}'""",
            "RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED");
    }

    [Fact]
    public async Task M10_owner_missing_head_mutation_context_is_rejected()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var bind = await CreateJobRepository(db).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m10-head-context"));
        await db.Database.OpenConnectionAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer;");

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tagekyc.raw_export_job_operational_heads
                SET "Revision"="Revision"+1,
                    "UpdatedAt"=clock_timestamp()
                WHERE "JobId"={bind.JobId};
                """));

        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED", exception.MessageText);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task M10_runtime_bind_forces_named_class_constraint_and_restores_deferred()
    {
        AuthorizedPermitFixture fixture;
        await using (var db = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        }
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(
            connection,
            transaction,
            Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());
        await using (var immediate = new NpgsqlCommand(
                         "SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes IMMEDIATE;",
                         connection,
                         transaction))
        {
            await immediate.ExecuteNonQueryAsync();
        }

        var result = await DirectClaimAsync(
            connection,
            transaction,
            fixture.PermitId,
            "b4-m10-immediate");
        await transaction.CommitAsync();

        Assert.Equal("NewJob", result.Outcome);
        await using var verify = postgres.CreateDbContext();
        Assert.True(await verify.RawExportJobClasses.AnyAsync(item => item.JobId == result.JobId));
    }

    [Fact]
    public async Task M10_two_valid_binds_in_one_transaction_preserve_constraint_mode()
    {
        AuthorizedPermitFixture first;
        AuthorizedPermitFixture second;
        await using (var db = postgres.CreateDbContext())
        {
            first = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
            second = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.HandSignatureImage]);
        }
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(
            connection,
            transaction,
            Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());

        var firstResult = await DirectClaimAsync(
            connection,
            transaction,
            first.PermitId,
            "b4-m10-first");
        var secondResult = await DirectClaimAsync(
            connection,
            transaction,
            second.PermitId,
            "b4-m10-second");
        await transaction.CommitAsync();

        Assert.Equal("NewJob", firstResult.Outcome);
        Assert.Equal("NewJob", secondResult.Outcome);
        Assert.NotEqual(firstResult.JobId, secondResult.JobId);
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(2, await verify.RawExportJobIdentities.CountAsync());
        Assert.Equal(2, await verify.RawExportJobClasses.CountAsync());
    }

    [Fact]
    public async Task M10_named_class_constraint_body_mutations_go_red()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT pg_catalog.pg_get_functiondef(
              'tagekyc.raw_export_claim_or_read_job(
                uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,
                text,timestamptz,timestamptz,text,bytea,text[])'::regprocedure);
            """,
            connection);
        var body = (string)(await command.ExecuteScalarAsync())!;
        var entryDeferred = body.IndexOf(
            "SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;",
            StringComparison.Ordinal);
        var identityInsert = body.IndexOf(
            "INSERT INTO tagekyc.raw_export_job_identities",
            StringComparison.Ordinal);
        var immediate = body.IndexOf(
            "SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes IMMEDIATE;",
            StringComparison.Ordinal);
        var finalDeferred = immediate < 0
            ? -1
            : body.IndexOf(
                "SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;",
                immediate,
                StringComparison.Ordinal);
        var newJobReturn = body.IndexOf(
            "RETURN QUERY SELECT 'NewJob'::text,prospective_job_id;",
            StringComparison.Ordinal);

        Assert.True(entryDeferred >= 0 && entryDeferred < identityInsert);
        Assert.True(identityInsert < immediate);
        Assert.True(immediate < finalDeferred);
        Assert.True(finalDeferred < newJobReturn);
        Assert.DoesNotContain("SET CONSTRAINTS ALL", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task M10_idempotency_key_function_and_table_guards_bite()
    {
        var functionFailure = await InvokeDirectClaimExpectingFailureAsync("bad/key");
        Assert.Equal("P0001", functionFailure.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED", functionFailure.MessageText);

        AuthorizedPermitFixture fixture;
        await using (var db = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        }
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var replica = new NpgsqlCommand(
                         "SET LOCAL session_replication_role=replica;",
                         connection,
                         transaction))
        {
            await replica.ExecuteNonQueryAsync();
        }
        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_job_identities
                ("JobId","PermitId","AuthorizationDecisionId","PrincipalId",
                 "ClientApplicationId","CreatedByApiKeyId","VerificationSessionId",
                 "SubjectRef","PolicyId","PolicyVersion","PurposeCode",
                 "RecipientClientApplicationId","ExportMode","PermitExpiresAt",
                 "JobExpiresAt","IdempotencyKey","IdempotencyFingerprintHash",
                 "SchemaVersion","CreatedAt")
            SELECT @job,p."PermitId",p."AuthorizationDecisionId",d."PrincipalId",
                   d."ClientApplicationId",@api,p."ResolvedVerificationSessionId",
                   p."SubjectRef",p."PolicyId",p."PolicyVersion",p."PurposeCode",
                   p."RecipientClientApplicationId",v."Mode",p."DecisionExpiresAtUtc",
                   p."DecisionExpiresAtUtc",'bad/key',decode(repeat('00',32),'hex'),
                   1,clock_timestamp()
            FROM tagekyc.raw_export_authorization_permits p
            JOIN tagekyc.raw_export_authorization_decisions d
              ON d."ExportDecisionId"=p."AuthorizationDecisionId"
            JOIN tagekyc.raw_export_policy_versions v
              ON v."PolicyId"=p."PolicyId" AND v."PolicyVersion"=p."PolicyVersion"
            WHERE p."PermitId"=@permit;
            """,
            connection,
            transaction);
        insert.Parameters.AddWithValue("job", Guid.NewGuid());
        insert.Parameters.AddWithValue("api", Tip88B34AuthorizationEngineTests.Actor.ApiKeyId);
        insert.Parameters.AddWithValue("permit", fixture.PermitId);
        var tableFailure = await Assert.ThrowsAsync<PostgresException>(
            () => insert.ExecuteNonQueryAsync());
        Assert.Equal("23514", tableFailure.SqlState);
        Assert.Equal(
            "CK_raw_export_job_identities_IdempotencyKey",
            tableFailure.ConstraintName);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("grant")]
    [InlineData("lifecycle")]
    [InlineData("fulfillment")]
    [InlineData("consent")]
    public async Task M5_bind_and_attempt_revalidate_all_authority_gates(string mutation)
    {
        await using (var bindDb = postgres.CreateDbContext())
        {
            var fixture = await CreateAuthorizedPermitAsync(
                bindDb,
                [RawExportRawClass.LiveSelfieImage]);
            await ApplyAuthorityMutationAsync(bindDb, fixture, mutation);
            var exception = await Assert.ThrowsAsync<RawExportJobException>(
                () => CreateJobRepository(bindDb).BindAsync(new(
                    Tip88B34AuthorizationEngineTests.Actor,
                    fixture.PermitId,
                    $"b4-m5-bind-{mutation}")));
            Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", exception.Code);
            Assert.False(await bindDb.RawExportJobIdentities.AnyAsync(
                item => item.PermitId == fixture.PermitId));
        }

        await using var attemptDb = postgres.CreateDbContext();
        var attemptFixture = await CreateAuthorizedPermitAsync(
            attemptDb,
            [RawExportRawClass.LiveSelfieImage]);
        var bind = await CreateJobRepository(attemptDb).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            attemptFixture.PermitId,
            $"b4-m5-attempt-{mutation}"));
        await ApplyAuthorityMutationAsync(attemptDb, attemptFixture, mutation);

        var result = await CreateJobRepository(attemptDb).AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));

        Assert.Equal(RawExportJobLeaseStatus.TerminalFailed, result.Status);
        Assert.Equal(RawExportJobState.TerminalFailed, result.State);
        Assert.Equal(
            RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED,
            result.TerminalReason);
        Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", result.StableCode);
        Assert.Equal(0, await attemptDb.RawExportJobAttempts.CountAsync(
            item => item.JobId == bind.JobId));
        var transition = await attemptDb.RawExportJobTransitions
            .SingleAsync(item => item.JobId == bind.JobId && item.ResultingRevision == 1);
        Assert.Equal("JobTerminalFailed", transition.EventType);
        Assert.Equal("AUTHORITY_REVALIDATION_FAILED", transition.FailureCode);
    }

    [Fact]
    public async Task M5_head_wait_then_revocation_is_visible_to_revalidation()
    {
        await using var setup = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(setup);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m5-head-wait"));
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockHeadAsync(blocker, blockerTransaction, bind.JobId);
        await using var attemptDb = postgres.CreateDbContext();
        var acquire = CreateJobRepository(attemptDb).AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            Guid.NewGuid()));
        await Task.Delay(200);
        Assert.False(acquire.IsCompleted);
        await using (var mutationDb = postgres.CreateDbContext())
        {
            await ApplyAuthorityMutationAsync(mutationDb, fixture, "grant");
        }
        await blockerTransaction.RollbackAsync();
        var result = await acquire;

        Assert.Equal(RawExportJobLeaseStatus.TerminalFailed, result.Status);
        Assert.Equal(
            RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED,
            result.TerminalReason);
        Assert.Equal(0, await attemptDb.RawExportJobAttempts.CountAsync());
    }

    [Fact]
    public async Task M5_grant_revoke_blocks_behind_attempt_revalidation()
    {
        await AssertB1MutationBlocksAsync("grant", bindPath: false);
    }

    [Fact]
    public async Task M5_lifecycle_suspend_or_revoke_blocks_behind_bind_and_attempt_revalidation()
    {
        await AssertB1MutationBlocksAsync("lifecycle", bindPath: true);
        await postgres.ResetDatabaseAsync();
        await AssertB1MutationBlocksAsync("lifecycle", bindPath: false);
    }

    [Fact]
    public async Task M5_rule_set_publish_blocks_behind_bind_and_attempt_revalidation()
    {
        await AssertB1MutationBlocksAsync("ruleset", bindPath: true);
        await postgres.ResetDatabaseAsync();
        await AssertB1MutationBlocksAsync("ruleset", bindPath: false);
    }

    [Fact]
    public async Task M5_each_fulfillment_withdraw_blocks_behind_bind_and_attempt_revalidation()
    {
        foreach (var requirement in new[]
                 {
                     RawExportRequirementType.LegalApproval,
                     RawExportRequirementType.RetentionSchedule,
                 })
        {
            await AssertFulfillmentMutationBlocksAsync(requirement, bindPath: true);
            await postgres.ResetDatabaseAsync();
            await AssertFulfillmentMutationBlocksAsync(requirement, bindPath: false);
            await postgres.ResetDatabaseAsync();
        }
    }

    [Fact]
    public async Task M5_consent_withdraw_blocks_behind_attempt_revalidation()
    {
        await using var setup = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage]);
        await EnsureWithdrawerAuthorityAsync(setup);
        var bind = await CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m5-consent-lock"));

        await using var attemptDb = postgres.CreateDbContext();
        var projections = new EfRawExportAuthorizationProjectionReader(attemptDb);
        var pause = new PausingSubjectConsentRepository(
            new EfRawExportSubjectConsentRepository(attemptDb));
        var attempt = new EfRawExportJobRepository(
                attemptDb,
                new EfRawExportControlPlaneRepository(attemptDb, projections),
                projections,
                pause,
                new RawExportJobLeaseState(60, true, null))
            .AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                Guid.NewGuid()));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var withdraw = new EfRawExportSubjectConsentRepository(mutationDb)
            .RecordSubjectConsentWithdrawnAsync(new(
                WithdrawerPrincipal,
                fixture.SessionId,
                fixture.PolicyId,
                1,
                ExpectedRevision: 1,
                TargetRevision: 1,
                $"decision:b4-m5-consent-lock:{Guid.NewGuid():N}"));
        await Task.Delay(250);
        var withdrawBlocked = !withdraw.IsCompleted;
        pause.Release();
        RawExportJobLeaseResult? acquired = null;
        Exception? attemptFailure = null;
        Exception? withdrawFailure = null;
        try
        {
            acquired = await attempt.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            attemptFailure = exception;
        }
        try
        {
            await withdraw.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            withdrawFailure = exception;
        }
        Assert.True(withdrawBlocked);
        Assert.Null(attemptFailure);
        Assert.Null(withdrawFailure);
        Assert.NotNull(acquired);
        Assert.Equal(RawExportJobLeaseStatus.Acquired, acquired.Status);
    }

    [Fact]
    public async Task M5_session_transition_blocks_behind_attempt_revalidation()
    {
        await using var setup = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage]);
        var bind = await CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m5-session-lock"));

        await using var attemptDb = postgres.CreateDbContext();
        var projections = new EfRawExportAuthorizationProjectionReader(attemptDb);
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(attemptDb, projections));
        var attempt = new EfRawExportJobRepository(
                attemptDb,
                pause,
                projections,
                new EfRawExportSubjectConsentRepository(attemptDb),
                new RawExportJobLeaseState(60, true, null))
            .AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                bind.JobId,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                Guid.NewGuid()));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var transition = new EfVerificationSessionRepository(mutationDb)
            .SetStateAsync(fixture.SessionId, VerificationSessionState.Expired);
        await Task.Delay(250);
        var transitionBlocked = !transition.IsCompleted;
        pause.Release();
        RawExportJobLeaseResult? acquired = null;
        Exception? attemptFailure = null;
        Exception? transitionFailure = null;
        try
        {
            acquired = await attempt.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            attemptFailure = exception;
        }
        try
        {
            await transition.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            transitionFailure = exception;
        }
        Assert.True(transitionBlocked);
        Assert.Null(attemptFailure);
        Assert.Null(transitionFailure);
        Assert.NotNull(acquired);
        Assert.Equal(RawExportJobLeaseStatus.Acquired, acquired.Status);
    }

    [Theory]
    [InlineData("consent")]
    [InlineData("fulfillment")]
    public async Task M5_head_wait_crossing_consent_or_fulfillment_expiry_fails_authority(
        string finiteAuthority)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(3);
        await using var setup = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            setup,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 30);
        var bind = await CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            $"b4-m5-head-finite-{finiteAuthority}"));
        if (finiteAuthority == "consent")
        {
            await new EfRawExportSubjectConsentRepository(setup)
                .RecordSubjectConsentGrantedAsync(new(
                    RecorderPrincipal,
                    fixture.SessionId,
                    fixture.PolicyId,
                    1,
                    new HashSet<RawExportRawClass>
                    {
                        RawExportRawClass.LiveSelfieImage,
                    },
                    "consent-text:v2",
                    "sha256:b4-m5-finite",
                    "external:b4-m5-finite",
                    $"decision:b4-m5-finite-consent:{Guid.NewGuid():N}",
                    deadline));
        }
        else
        {
            await new EfRawExportControlPlaneRepository(setup)
                .AcceptFulfillmentAsync(new(
                    RecorderPrincipal,
                    fixture.PolicyId,
                    1,
                    RawExportRequirementType.LegalApproval,
                    ExpectedRevision: 1,
                    SupersedesRevision: 1,
                    $"artifact:b4-m5-finite:{Guid.NewGuid():N}",
                    "v2",
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    deadline,
                    $"decision:b4-m5-finite-fulfillment:{Guid.NewGuid():N}"));
        }

        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockHeadAsync(blocker, blockerTransaction, bind.JobId);
        await using var attemptDb = postgres.CreateDbContext();
        var attempt = CreateJobRepository(attemptDb).AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            ExpectedRevision: 0,
            ExpectedFencingToken: 0,
            Guid.NewGuid()));
        await Task.Delay(200);
        Assert.False(attempt.IsCompleted);
        await WaitUntilAfterAsync(deadline);
        await blockerTransaction.RollbackAsync();
        var result = await attempt;

        Assert.Equal(RawExportJobLeaseStatus.TerminalFailed, result.Status);
        Assert.Equal(
            RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED,
            result.TerminalReason);
        Assert.Equal("RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE", result.StableCode);
        Assert.Equal(0, await attemptDb.RawExportJobAttempts.CountAsync(
            item => item.JobId == bind.JobId));
    }

    [Theory]
    [InlineData(System.Data.IsolationLevel.RepeatableRead)]
    [InlineData(System.Data.IsolationLevel.Serializable)]
    public async Task M5_attempt_lock_rejects_non_read_committed_isolation(
        System.Data.IsolationLevel isolation)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(isolation);
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_lock_job_for_attempt(
              @job,@principal,@client,0,0);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        command.Parameters.AddWithValue("principal", actor.PrincipalId);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.MessageText);
    }

    [Fact]
    public async Task M5_attempt_rejects_matching_read_committed_ambient_before_database()
    {
        await using var db = postgres.CreateDbContext();
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            new System.Transactions.TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
            },
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                Guid.NewGuid(),
                0,
                0,
                Guid.NewGuid())));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.Code);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task M5_attempt_rejects_active_read_committed_connection_transaction()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted);
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => CreateJobRepository(db).AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                Guid.NewGuid(),
                0,
                0,
                Guid.NewGuid())));

        Assert.Equal("RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID", exception.Code);
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync());
    }

    [Fact]
    public async Task B4_invalid_config_registration_is_not_replaced_by_default_and_readiness_fails_closed()
    {
        var configured = RawExportJobLeaseOptions.Resolve("not-an-integer");
        var services = new ServiceCollection();
        services.AddSingleton(configured);
        services.AddTagEkycPostgresPersistence(postgres.ConnectionString);
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<RawExportJobLeaseState>();

        Assert.Same(configured, resolved);
        Assert.False(resolved.IsValid);
        Assert.Equal(RawExportJobLeaseOptions.InvalidCode, resolved.InvalidCode);

        var check = new RawExportJobReadinessCheck(
            scope.ServiceProvider.GetRequiredService<RawExportJobReadinessValidator>(),
            resolved);
        var issues = await check.CheckAsync(CancellationToken.None);
        var issue = Assert.Single(issues);
        Assert.Equal(RawExportJobLeaseOptions.InvalidCode, issue.Code);
    }

    [Fact]
    public async Task M12_readiness_returns_exact_code_for_each_manifest_drift()
    {
        await using var db = postgres.CreateDbContext();
        var validator = new RawExportJobReadinessValidator(
            db,
            new RawExportJobLeaseState(60, true, null));

        await validator.ValidateAsync(CancellationToken.None);

        var lease = new RawExportJobReadinessValidator(
            db,
            new RawExportJobLeaseState(0, false, RawExportJobLeaseOptions.InvalidCode));
        var leaseFailure = await Assert.ThrowsAsync<RawExportJobReadinessException>(
            () => lease.ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportJobLeaseOptions.InvalidCode, leaseFailure.Code);

        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE tagekyc.raw_export_job_identities
            RENAME CONSTRAINT "CK_b4_job_identity_schema_version"
            TO "CK_b4_job_identity_schema_version_drift";
            """);
        try
        {
            var failure = await Assert.ThrowsAsync<RawExportJobReadinessException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Equal(RawExportJobReadinessValidator.SchemaInvalid, failure.Code);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE tagekyc.raw_export_job_identities
                RENAME CONSTRAINT "CK_b4_job_identity_schema_version_drift"
                TO "CK_b4_job_identity_schema_version";
                """);
        }

        await db.Database.ExecuteSqlRawAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.enforce_raw_export_job_identity_insert() TO PUBLIC;");
        try
        {
            var failure = await Assert.ThrowsAsync<RawExportJobReadinessException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Equal(RawExportJobReadinessValidator.FunctionAclInvalid, failure.Code);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "REVOKE EXECUTE ON FUNCTION tagekyc.enforce_raw_export_job_identity_insert() FROM PUBLIC;");
        }

        await db.Database.ExecuteSqlRawAsync(
            "GRANT SELECT ON tagekyc.raw_export_job_identities TO PUBLIC;");
        try
        {
            var failure = await Assert.ThrowsAsync<RawExportJobReadinessException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Equal(RawExportJobReadinessValidator.TablePrivilegeInvalid, failure.Code);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "REVOKE SELECT ON tagekyc.raw_export_job_identities FROM PUBLIC;");
        }

        await validator.ValidateAsync(CancellationToken.None);
    }

    [Fact]
    public async Task M12_apply_rollback_reapply_preserves_pre_b4_catalog_and_acls()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260724015546_Tip88B1E3ResolverReadBoundary");
        var before = await ReadPreB4CatalogSnapshotAsync(db);

        await migrator.MigrateAsync("20260726145547_Tip88B4RawExportJobFoundation");
        var firstB4Manifest = await ReadCatalogManifestAsync(db);
        await migrator.MigrateAsync("20260724015546_Tip88B1E3ResolverReadBoundary");
        var after = await ReadPreB4CatalogSnapshotAsync(db);

        Assert.Equal(before, after);
        await migrator.MigrateAsync("20260726145547_Tip88B4RawExportJobFoundation");
        var secondB4Manifest = await ReadCatalogManifestAsync(db);
        foreach (var kind in firstB4Manifest.Keys.OrderBy(value => value, StringComparer.Ordinal))
        {
            Assert.Equal(
                firstB4Manifest[kind].OrderBy(value => value, StringComparer.Ordinal),
                secondB4Manifest[kind].OrderBy(value => value, StringComparer.Ordinal));
        }

        await migrator.MigrateAsync(CurrentMigration);
        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_function_acl_manifest_is_exact_and_grantor_aware()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            WITH b4 AS (
              SELECT p.oid,p.proowner,p.proname,r.rolname AS owner_name
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_catalog.pg_roles r ON r.oid=p.proowner
              WHERE n.nspname='tagekyc'
                AND p.proname IN (
                    'enforce_raw_export_job_attempt_insert',
                    'enforce_raw_export_job_class_insert',
                    'enforce_raw_export_job_head_mutation',
                    'enforce_raw_export_job_identity_has_classes',
                    'enforce_raw_export_job_identity_insert',
                    'enforce_raw_export_job_transition_insert',
                    'raw_export_acquire_or_reclaim_job_lease',
                    'raw_export_claim_or_read_job',
                    'raw_export_lock_job_for_attempt',
                    'raw_export_read_job',
                    'raw_export_read_job_binding_inputs',
                    'raw_export_record_job_attempt_failure',
                    'raw_export_renew_job_lease',
                    'raw_export_terminalize_job')
            ),
            nonowner AS (
              SELECT b.proname,grantor.rolname AS grantor_name,grantee.rolname AS grantee_name,
                     x.privilege_type,x.is_grantable
              FROM b4 b
              JOIN pg_catalog.pg_proc p ON p.oid=b.oid
              CROSS JOIN LATERAL pg_catalog.aclexplode(
                COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) x
              LEFT JOIN pg_catalog.pg_roles grantor ON grantor.oid=x.grantor
              LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=x.grantee
              WHERE x.grantee<>p.proowner
            )
            SELECT
              (SELECT count(*) FROM b4)=14
              AND NOT EXISTS (SELECT 1 FROM b4 WHERE owner_name<>'tagekyc_raw_export_deployer')
              AND (SELECT count(*) FROM nonowner)=8
              AND NOT EXISTS (
                SELECT 1 FROM nonowner
                WHERE proname NOT IN (
                    'raw_export_read_job_binding_inputs',
                    'raw_export_claim_or_read_job',
                    'raw_export_read_job',
                    'raw_export_lock_job_for_attempt',
                    'raw_export_acquire_or_reclaim_job_lease',
                    'raw_export_renew_job_lease',
                    'raw_export_record_job_attempt_failure',
                    'raw_export_terminalize_job')
                   OR grantor_name<>'tagekyc_raw_export_deployer'
                   OR grantee_name<>'tagekyc_runtime'
                   OR privilege_type<>'EXECUTE'
                   OR is_grantable);
            """;

        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task M6_alternate_function_acl_grantor_aborts_apply()
    {
        const string previousMigration = "20260724015546_Tip88B1E3ResolverReadBoundary";
        const string b4Migration = "20260726145547_Tip88B4RawExportJobFoundation";
        var role = $"b4_acl_grantor_{Guid.NewGuid():N}";
        var quotedRole = $"\"{role}\"";
        await using var db = postgres.CreateDbContext();
        var migrator = db.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(previousMigration);
        var script = migrator.GenerateScript(previousMigration, b4Migration);
        const string marker =
            """
            RESET ROLE;
            DO $$
            """;
        var injection =
            $"""
             RESET ROLE;
             SET LOCAL ROLE tagekyc_raw_export_deployer;
             GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_job(uuid,uuid,uuid)
                 TO {quotedRole} WITH GRANT OPTION;
             RESET ROLE;
             SET LOCAL ROLE {quotedRole};
             GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_job(uuid,uuid,uuid)
                 TO tagekyc_runtime;
             RESET ROLE;
             DO $$
             """;
        var mutatedScript = script.Replace(
            marker,
            injection,
            StringComparison.Ordinal);
        Assert.NotEqual(script, mutatedScript);

        await using var admin = new NpgsqlConnection(postgres.ConnectionString);
        await admin.OpenAsync();
        await using (var setup = new NpgsqlCommand(
                         $"""
                          CREATE ROLE {quotedRole} NOLOGIN;
                          GRANT USAGE ON SCHEMA tagekyc TO {quotedRole};
                          """,
                         admin))
        {
            await setup.ExecuteNonQueryAsync();
        }
        try
        {
            await using var apply = new NpgsqlCommand(mutatedScript, admin);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => apply.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Equal("TIP88B4_FUNCTION_ACL_INVALID", exception.MessageText);
            await using (var rollback = new NpgsqlCommand("ROLLBACK;", admin))
            {
                await rollback.ExecuteNonQueryAsync();
            }

            await using var verify = postgres.CreateDbContext();
            var applied = await verify.Database.SqlQueryRaw<string>(
                    """SELECT "MigrationId" AS "Value" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1""")
                .SingleAsync();
            Assert.Equal(previousMigration, applied);
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand(
                $"""
                 REVOKE USAGE ON SCHEMA tagekyc FROM {quotedRole};
                 DROP ROLE {quotedRole};
                 """,
                admin);
            await cleanup.ExecuteNonQueryAsync();
            await migrator.MigrateAsync(CurrentMigration);
        }

        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_runtime_only_login_can_use_all_b4_entries()
    {
        AuthorizedPermitFixture fixture;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage]);
        }

        var role = $"b4_runtime_{Guid.NewGuid():N}";
        var password = $"B4_{Guid.NewGuid():N}!";
        var quotedRole = $"\"{role}\"";
        await using var admin = new NpgsqlConnection(postgres.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand(
                         $"""
                          CREATE ROLE {quotedRole} LOGIN INHERIT PASSWORD '{password}'
                              NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                          GRANT tagekyc_runtime TO {quotedRole}
                              WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                          """,
                         admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
            {
                Username = role,
                Password = password,
                Pooling = false,
            };
            await using (var runtime = new NpgsqlConnection(builder.ConnectionString))
            {
                await runtime.OpenAsync();
                await using (var topology = new NpgsqlCommand(
                                 """
                                 SELECT session_user=current_user,
                                        pg_has_role(current_user,'tagekyc_runtime','USAGE');
                                 """,
                                 runtime))
                await using (var reader = await topology.ExecuteReaderAsync())
                {
                    Assert.True(await reader.ReadAsync());
                    Assert.True(reader.GetBoolean(0));
                    Assert.True(reader.GetBoolean(1));
                }

                DirectClaimResult claim;
                await using (var transaction = await runtime.BeginTransactionAsync())
                {
                    await SetActorAsync(
                        runtime,
                        transaction,
                        Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString());
                    claim = await DirectClaimAsync(
                        runtime,
                        transaction,
                        fixture.PermitId,
                        "b4-m6-runtime");
                    Assert.Equal("NewJob", claim.Outcome);
                    Assert.NotNull(claim.JobId);
                    await AssertRuntimeEntryCallsAsync(runtime, transaction, claim.JobId.Value);
                    await transaction.CommitAsync();
                }

                await using var forbidden = new NpgsqlCommand(
                    "SELECT count(*) FROM tagekyc.raw_export_job_identities;",
                    runtime);
                var exception = await Assert.ThrowsAsync<PostgresException>(
                    () => forbidden.ExecuteScalarAsync());
                Assert.Equal("42501", exception.SqlState);
            }

            await using var membership = new NpgsqlCommand(
                """
                SELECT m.admin_option,m.inherit_option,m.set_option
                FROM pg_catalog.pg_auth_members m
                JOIN pg_catalog.pg_roles granted ON granted.oid=m.roleid
                JOIN pg_catalog.pg_roles member ON member.oid=m.member
                WHERE granted.rolname='tagekyc_runtime' AND member.rolname=@role;
                """,
                admin);
            membership.Parameters.AddWithValue("role", role);
            await using var membershipReader = await membership.ExecuteReaderAsync();
            Assert.True(await membershipReader.ReadAsync());
            Assert.False(membershipReader.GetBoolean(0));
            Assert.True(membershipReader.GetBoolean(1));
            Assert.False(membershipReader.GetBoolean(2));
            Assert.False(await membershipReader.ReadAsync());
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"""
                 REVOKE tagekyc_runtime FROM {quotedRole};
                 DROP ROLE {quotedRole};
                 """,
                admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task M6_table_nonowner_acl_manifest_is_empty()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT count(*)
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(
              COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) x
            WHERE n.nspname='tagekyc'
              AND c.relname=ANY(@tables)
              AND x.grantee<>c.relowner;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tables";
        parameter.Value = B4OwnedTables;
        command.Parameters.Add(parameter);

        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task M6_column_nonowner_acl_manifest_is_empty()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT count(*)
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a
              ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            CROSS JOIN LATERAL pg_catalog.aclexplode(a.attacl) x
            WHERE n.nspname='tagekyc'
              AND c.relname=ANY(@tables)
              AND a.attacl IS NOT NULL
              AND x.grantee<>c.relowner;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tables";
        parameter.Value = B4OwnedTables;
        command.Parameters.Add(parameter);

        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task M6_actor_context_missing_invalid_mismatch_and_valid_are_distinct()
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await AssertActorContextFailureAsync(null, actor.PrincipalId, "RAW_EXPORT_ACTOR_CONTEXT_MISSING");
        await AssertActorContextFailureAsync(string.Empty, actor.PrincipalId, "RAW_EXPORT_ACTOR_CONTEXT_MISSING");
        await AssertActorContextFailureAsync("not-a-uuid", actor.PrincipalId, "RAW_EXPORT_ACTOR_CONTEXT_INVALID");
        await AssertActorContextFailureAsync(Guid.Empty.ToString(), actor.PrincipalId, "RAW_EXPORT_ACTOR_CONTEXT_INVALID");
        await AssertActorContextFailureAsync(Guid.NewGuid().ToString(), actor.PrincipalId, "RAW_EXPORT_JOB_ACTOR_MISMATCH");

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_job(@principal,@client,@job);",
            connection,
            transaction);
        command.Parameters.AddWithValue("principal", actor.PrincipalId);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }

    [Fact]
    public async Task M6_direct_claim_rejects_invalid_idempotency_grammar()
    {
        var invalid = new[]
        {
            string.Empty,
            " ",
            "\n",
            "khóa",
            "bad/key",
            new string('a', 257),
        };
        foreach (var key in invalid)
        {
            var exception = await InvokeDirectClaimExpectingFailureAsync(key);
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED", exception.MessageText);
        }

        foreach (var key in new[] { "a", new string('z', 256) })
        {
            var exception = await InvokeDirectClaimExpectingFailureAsync(key);
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE", exception.MessageText);
        }

        await using var db = postgres.CreateDbContext();
        Assert.Equal(0, await db.RawExportJobIdentities.CountAsync());
        Assert.Equal(0, await db.RawExportJobClasses.CountAsync());
        Assert.Equal(0, await db.RawExportJobOperationalHeads.CountAsync());
        Assert.Equal(0, await db.RawExportJobTransitions.CountAsync());
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync());
    }

    [Fact]
    public async Task M6_foreign_direct_claim_cannot_use_existing_job_as_existence_oracle()
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            "b4-m6-owned-job"));
        var identity = await db.RawExportJobIdentities.SingleAsync(
            item => item.JobId == bind.JobId);
        var classes = await db.RawExportJobClasses
            .Where(item => item.JobId == bind.JobId)
            .OrderBy(item => item.Ordinal)
            .Select(item => item.RawClass)
            .ToArrayAsync();
        var foreignPrincipal = Guid.NewGuid();
        var foreignClient = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, foreignPrincipal.ToString());
        await using var command = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_claim_or_read_job(
              @job,@permit,@decision,@principal,@client,@api,@session,@subject,
              @policy,@version,@purpose,@recipient,@mode,@permit_expiry,@job_expiry,
              @key,@fingerprint,@classes);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        command.Parameters.AddWithValue("permit", identity.PermitId);
        command.Parameters.AddWithValue("decision", identity.AuthorizationDecisionId);
        command.Parameters.AddWithValue("principal", foreignPrincipal);
        command.Parameters.AddWithValue("client", foreignClient);
        command.Parameters.AddWithValue("api", Guid.NewGuid());
        command.Parameters.AddWithValue("session", identity.VerificationSessionId);
        command.Parameters.AddWithValue("subject", identity.SubjectRef);
        command.Parameters.AddWithValue("policy", identity.PolicyId);
        command.Parameters.AddWithValue("version", identity.PolicyVersion);
        command.Parameters.AddWithValue("purpose", identity.PurposeCode);
        command.Parameters.AddWithValue("recipient", identity.RecipientClientApplicationId);
        command.Parameters.AddWithValue("mode", identity.ExportMode);
        command.Parameters.AddWithValue("permit_expiry", identity.PermitExpiresAt);
        command.Parameters.AddWithValue("job_expiry", identity.JobExpiresAt);
        command.Parameters.AddWithValue("key", "b4-m6-foreign-oracle");
        command.Parameters.AddWithValue("fingerprint", new byte[32]);
        command.Parameters.AddWithValue("classes", classes);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Equal("RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE", exception.MessageText);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task M6_valid_c1_job_functions_do_not_pollute_b4_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT pg_catalog.count(*) = 2
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname = 'tagekyc'
              AND p.proname IN (
                  'raw_export_freeze_job_source_bindings',
                  'raw_export_read_job_source_verification_context');
            """;
        Assert.True(await command.ExecuteScalarAsync() is true);

        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_unrelated_table_grantee_fails_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "GRANT SELECT ON tagekyc.raw_export_job_identities TO PUBLIC;");
        try
        {
            await AssertReadinessFailureAsync(
                db,
                RawExportJobReadinessValidator.TablePrivilegeInvalid);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "REVOKE SELECT ON tagekyc.raw_export_job_identities FROM PUBLIC;");
        }
        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_unrelated_column_grants_fail_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """GRANT SELECT("SubjectRef") ON tagekyc.raw_export_job_identities TO PUBLIC;""");
        try
        {
            await AssertReadinessFailureAsync(
                db,
                RawExportJobReadinessValidator.TablePrivilegeInvalid);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                """REVOKE SELECT("SubjectRef") ON tagekyc.raw_export_job_identities FROM PUBLIC;""");
        }
        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_column_alternate_grantor_or_grant_option_fails_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("""
            SET ROLE tagekyc_raw_export_deployer;
            GRANT SELECT("JobId") ON tagekyc.raw_export_job_identities
                TO tagekyc_runtime WITH GRANT OPTION;
            RESET ROLE;
            """);
        try
        {
            await AssertReadinessFailureAsync(
                db,
                RawExportJobReadinessValidator.TablePrivilegeInvalid);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("""
                SET ROLE tagekyc_raw_export_deployer;
                REVOKE SELECT("JobId") ON tagekyc.raw_export_job_identities
                    FROM tagekyc_runtime;
                RESET ROLE;
                """);
        }

        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_default_function_acl_grantee_aborts_apply()
    {
        const string previousMigration = "20260724015546_Tip88B1E3ResolverReadBoundary";
        const string b4Migration = "20260726145547_Tip88B4RawExportJobFoundation";
        var role = $"b4_default_acl_{Guid.NewGuid():N}";
        var quotedRole = $"\"{role}\"";

        await using var db = postgres.CreateDbContext();
        var migrator = db.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(previousMigration);
        await using var admin = new NpgsqlConnection(postgres.ConnectionString);
        await admin.OpenAsync();
        await using (var setup = new NpgsqlCommand(
                         $"""
                          CREATE ROLE {quotedRole} NOLOGIN;
                          ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc IN SCHEMA tagekyc
                              GRANT EXECUTE ON FUNCTIONS TO {quotedRole};
                          """,
                         admin))
        {
            await setup.ExecuteNonQueryAsync();
        }
        try
        {
            var apply = await Assert.ThrowsAsync<PostgresException>(
                () => migrator.MigrateAsync(b4Migration));
            Assert.Equal(PostgresErrorCodes.RaiseException, apply.SqlState);
            Assert.Equal("TIP88B4_FUNCTION_ACL_INVALID", apply.MessageText);

            var applied = await db.Database.SqlQueryRaw<string>(
                    """SELECT "MigrationId" AS "Value" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1""")
                .SingleAsync();
            Assert.Equal(previousMigration, applied);
        }
        finally
        {
            await using var cleanup = new NpgsqlCommand(
                $"""
                 ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc IN SCHEMA tagekyc
                     REVOKE EXECUTE ON FUNCTIONS FROM {quotedRole};
                 DROP ROLE {quotedRole};
                 """,
                admin);
            await cleanup.ExecuteNonQueryAsync();
            await migrator.MigrateAsync(CurrentMigration);
        }

        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_internal_helper_public_execute_fails_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.enforce_raw_export_job_head_mutation() TO PUBLIC;");
        try
        {
            await AssertReadinessFailureAsync(
                db,
                RawExportJobReadinessValidator.FunctionAclInvalid);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "REVOKE EXECUTE ON FUNCTION tagekyc.enforce_raw_export_job_head_mutation() FROM PUBLIC;");
        }
        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M6_deployer_owner_attributes_fail_readiness()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER ROLE tagekyc_raw_export_deployer NOINHERIT;");
        try
        {
            await AssertReadinessFailureAsync(
                db,
                RawExportJobReadinessValidator.FunctionAclInvalid);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER ROLE tagekyc_raw_export_deployer INHERIT;");
        }

        await HealthyReadinessAsync(db);
    }

    [Fact]
    public async Task M13_b4_database_contains_no_raw_byte_package_or_delivery_surface()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT count(*)
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a
              ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            WHERE n.nspname='tagekyc'
              AND c.relname=ANY(@tables)
              AND (
                a.attname ~* '(payload|bytes|blob|content|artifact|package|delivery)'
                OR a.atttypid='oid'::regtype
                OR (a.atttypid='bytea'::regtype AND a.attname<>'IdempotencyFingerprintHash'));
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tables";
        parameter.Value = B4OwnedTables;
        command.Parameters.Add(parameter);

        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    internal static EfRawExportJobRepository CreateJobRepository(
        TagEkycDbContext db,
        RawExportJobLeaseState? lease = null)
    {
        var projections = new EfRawExportAuthorizationProjectionReader(db);
        return new(
            db,
            new EfRawExportControlPlaneRepository(db, projections),
            projections,
            new EfRawExportSubjectConsentRepository(db),
            lease ?? new RawExportJobLeaseState(60, true, null));
    }

    internal static async Task<AuthorizedPermitFixture> CreateAuthorizedPermitAsync(
        TagEkycDbContext db,
        IReadOnlyList<RawExportRawClass> classes,
        int permitTtlSeconds = 300,
        DateTimeOffset? consentValidUntilUtc = null,
        DateTimeOffset? fulfillmentValidUntilUtc = null)
    {
        var policyId = Guid.NewGuid();
        var sessionId = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
            db,
            Tip88B34AuthorizationEngineTests.ClientApplicationId,
            $"subject:b4:{Guid.NewGuid():N}");
        await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
            db,
            policyId,
            classes,
            permitTtlSeconds,
            fulfillmentExpiry:
                fulfillmentValidUntilUtc ?? DateTimeOffset.UtcNow.AddMinutes(4));
        await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            classes,
            consentValidUntilUtc ?? DateTimeOffset.UtcNow.AddMinutes(3));
        var projections = new EfRawExportAuthorizationProjectionReader(db);
        var authorization = await new EfRawExportAuthorizationRepository(
                db,
                projections,
                new EfRawExportControlPlaneRepository(db),
                new EfRawExportSubjectConsentRepository(db),
                RawExportPermitTtlBoundsState.Valid(1, 900))
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                sessionId,
                policyId,
                $"b4-auth-{Guid.NewGuid():N}",
                classes));
        Assert.NotNull(authorization.Permit);
        return new(
            authorization.Permit!.PermitId,
            authorization.Decision.ExportDecisionId,
            sessionId,
            policyId);
    }

    internal static async Task<AuthorizedPermitFixture> CreateAuthorizedPacketPermitAsync(
        TagEkycDbContext db,
        IReadOnlyList<RawExportRawClass> classes)
    {
        var policyId = Guid.NewGuid();
        var sessionId = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
            db,
            Tip88B34AuthorizationEngineTests.ClientApplicationId,
            $"subject:b4-packet:{Guid.NewGuid():N}");
        foreach (var authority in new[] { "GrantAdmin", "RecorderAuthorityAdmin", "ActivationAuthority" })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT tagekyc.raw_export_bootstrap_global_authority(
                    {AdminPrincipal},{authority},{"decision:b4-bootstrap:" + authority});
                """);
        }

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_versions
                    ("PolicyId","PolicyVersion","Mode","Purpose","RetentionProfileRef",
                     "RetentionPurposeCode","ConsentRequirement","ControllerRole",
                     "ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction",
                     "ProcessingInfrastructureJurisdiction","RequirementRuleSetId",
                     "RequirementRuleSetVersion","PermitTtlSeconds","CreatedAt")
                VALUES
                    ({policyId},1,'EncryptedExportPacket','purpose','packet-ttl',
                     'PACKET_TTL','Required','Processor','controller','VN','VN','VN',
                     'RAW_EXPORT_REQUIREMENTS',1,300,transaction_timestamp());
                """);
            foreach (var rawClass in classes)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO tagekyc.raw_export_policy_allowed_classes
                        ("PolicyId","PolicyVersion","RawClass","CreatedAt")
                    VALUES ({policyId},1,{rawClass.ToString()},transaction_timestamp());
                    """);
            }
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_requirements
                    ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
                VALUES
                    ({policyId},1,'LegalApproval',transaction_timestamp()),
                    ({policyId},1,'ConsentArtifact',transaction_timestamp()),
                    ({policyId},1,'RetentionSchedule',transaction_timestamp());
                """);
            await transaction.CommitAsync();
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_policy_closures
                ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
            VALUES ({policyId},1,'CatalogApproved',transaction_timestamp(),
                    'principal:b4-catalog','decision:b4-catalog');
            """);
        var control = new EfRawExportControlPlaneRepository(db);
        await control.GrantExportPolicyAsync(new(
            AdminPrincipal,
            Tip88B34AuthorizationEngineTests.ConsumerPrincipal,
            policyId,
            1,
            ExpectedRevision: 0,
            ClientApplicationId: null,
            "decision:b4-packet-grant"));
        foreach (var requirement in new[]
                 {
                     RawExportRequirementType.LegalApproval,
                     RawExportRequirementType.RetentionSchedule,
                 })
        {
            await control.GrantControlAuthorityAsync(new(
                AdminPrincipal,
                RecorderPrincipal,
                RawExportAuthorityType.FulfillmentRecorder,
                RawExportAuthorityScopeType.Policy,
                policyId,
                requirement,
                ExpectedRevision: 0,
                $"decision:b4-packet-recorder:{requirement}"));
            await control.AcceptFulfillmentAsync(new(
                RecorderPrincipal,
                policyId,
                1,
                requirement,
                ExpectedRevision: 0,
                SupersedesRevision: null,
                $"artifact:b4-packet:{requirement}",
                "v1",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddMinutes(4),
                $"decision:b4-packet-fulfillment:{requirement}"));
        }
        await control.ActivatePolicyAsync(new(
            AdminPrincipal,
            policyId,
            1,
            ExpectedRevision: 0,
            "decision:b4-packet-activate"));
        await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            classes,
            DateTimeOffset.UtcNow.AddMinutes(3));
        var authorization = await Tip88B34AuthorizationEngineTests.CreateRepository(db)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                sessionId,
                policyId,
                $"b4-packet-auth-{Guid.NewGuid():N}",
                classes));
        Assert.NotNull(authorization.Permit);
        return new(
            authorization.Permit!.PermitId,
            authorization.Decision.ExportDecisionId,
            sessionId,
            policyId);
    }

    private async Task AssertB1MutationBlocksAsync(string mutationKind, bool bindPath)
    {
        AuthorizedPermitFixture fixture;
        Guid? boundJob = null;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage]);
            if (!bindPath)
            {
                boundJob = (await CreateJobRepository(setup).BindAsync(new(
                    Tip88B34AuthorizationEngineTests.Actor,
                    fixture.PermitId,
                    $"b4-m5-{mutationKind}-prebind"))).JobId;
            }
        }

        await using var operationDb = postgres.CreateDbContext();
        var projections = new EfRawExportAuthorizationProjectionReader(operationDb);
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(operationDb, projections));
        var repository = new EfRawExportJobRepository(
            operationDb,
            pause,
            projections,
            new EfRawExportSubjectConsentRepository(operationDb),
            new RawExportJobLeaseState(60, true, null));
        Task operation = bindPath
            ? repository.BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                fixture.PermitId,
                $"b4-m5-{mutationKind}-bind"))
            : repository.AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                boundJob!.Value,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                Guid.NewGuid()));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var control = new EfRawExportControlPlaneRepository(mutationDb);
        Task mutation = mutationKind switch
        {
            "grant" => control.RevokeExportPolicyGrantAsync(new(
                AdminPrincipal,
                Tip88B34AuthorizationEngineTests.ConsumerPrincipal,
                fixture.PolicyId,
                1,
                ExpectedRevision: 1,
                ClientApplicationId: null,
                $"decision:b4-m5-lock-revoke:{Guid.NewGuid():N}")),
            "lifecycle" => control.SuspendPolicyAsync(new(
                AdminPrincipal,
                fixture.PolicyId,
                1,
                ExpectedRevision: 1,
                $"decision:b4-m5-lock-suspend:{Guid.NewGuid():N}")),
            "ruleset" => PublishNextRuleSetAsync(postgres.ConnectionString),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutationKind),
                mutationKind,
                null),
        };
        await Task.Delay(250);
        var mutationBlocked = !mutation.IsCompleted;
        pause.Release();
        Exception? operationFailure = null;
        Exception? mutationFailure = null;
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }
        try
        {
            await mutation.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            mutationFailure = exception;
        }
        Assert.True(mutationBlocked, $"{mutationKind} did not block on its B1 lock.");
        Assert.Null(operationFailure);
        Assert.Null(mutationFailure);
        if (bindPath)
        {
            Assert.Equal(1, await operationDb.RawExportJobIdentities.CountAsync(
                item => item.PermitId == fixture.PermitId));
        }
        else
        {
            Assert.Equal(1, await operationDb.RawExportJobAttempts.CountAsync(
                item => item.JobId == boundJob));
        }
    }

    private async Task AssertFulfillmentMutationBlocksAsync(
        RawExportRequirementType requirement,
        bool bindPath)
    {
        AuthorizedPermitFixture fixture;
        Guid? boundJob = null;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await CreateAuthorizedPacketPermitAsync(
                setup,
                [RawExportRawClass.LiveSelfieImage]);
            if (!bindPath)
            {
                boundJob = (await CreateJobRepository(setup).BindAsync(new(
                    Tip88B34AuthorizationEngineTests.Actor,
                    fixture.PermitId,
                    $"b4-m5-{requirement}-prebind"))).JobId;
            }
        }

        await using var operationDb = postgres.CreateDbContext();
        var projections = new EfRawExportAuthorizationProjectionReader(operationDb);
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(operationDb, projections));
        var repository = new EfRawExportJobRepository(
            operationDb,
            pause,
            projections,
            new EfRawExportSubjectConsentRepository(operationDb),
            new RawExportJobLeaseState(60, true, null));
        Task operation = bindPath
            ? repository.BindAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                fixture.PermitId,
                $"b4-m5-{requirement}-bind"))
            : repository.AcquireOrReclaimLeaseAsync(new(
                Tip88B34AuthorizationEngineTests.Actor,
                boundJob!.Value,
                ExpectedRevision: 0,
                ExpectedFencingToken: 0,
                Guid.NewGuid()));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var withdraw = new EfRawExportControlPlaneRepository(mutationDb)
            .WithdrawFulfillmentAsync(new(
                RecorderPrincipal,
                fixture.PolicyId,
                1,
                requirement,
                ExpectedRevision: 1,
                TargetRevision: 1,
                $"decision:b4-m5-lock-withdraw:{requirement}:{Guid.NewGuid():N}"));
        await Task.Delay(250);
        var withdrawBlocked = !withdraw.IsCompleted;
        pause.Release();
        Exception? operationFailure = null;
        Exception? withdrawFailure = null;
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }
        try
        {
            await withdraw.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception)
        {
            withdrawFailure = exception;
        }
        Assert.True(
            withdrawBlocked,
            $"{requirement} withdrawal did not block on its B1 lock.");
        Assert.Null(operationFailure);
        Assert.Null(withdrawFailure);
        if (bindPath)
        {
            Assert.Equal(1, await operationDb.RawExportJobIdentities.CountAsync(
                item => item.PermitId == fixture.PermitId));
        }
        else
        {
            Assert.Equal(1, await operationDb.RawExportJobAttempts.CountAsync(
                item => item.JobId == boundJob));
        }
    }

    private static async Task PublishNextRuleSetAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand(
                         """
                         SELECT pg_catalog.pg_advisory_xact_lock(
                           pg_catalog.hashtext(
                             'tip88b1:raw_export_requirement_rule_set_publish'));
                         """,
                         connection,
                         transaction))
        {
            await lockCommand.ExecuteNonQueryAsync();
        }
        int version;
        await using (var versionCommand = new NpgsqlCommand(
                         """
                         SELECT COALESCE(MAX("RuleSetVersion"),0)+1
                         FROM tagekyc.raw_export_requirement_rule_sets
                         WHERE "RuleSetId"='RAW_EXPORT_REQUIREMENTS';
                         """,
                         connection,
                         transaction))
        {
            version = Convert.ToInt32(
                await versionCommand.ExecuteScalarAsync(),
                System.Globalization.CultureInfo.InvariantCulture);
        }
        await using (var replica = new NpgsqlCommand(
                         "SET LOCAL session_replication_role=replica;",
                         connection,
                         transaction))
        {
            await replica.ExecuteNonQueryAsync();
        }
        await using (var insert = new NpgsqlCommand(
                         """
                         INSERT INTO tagekyc.raw_export_requirement_rule_sets
                             ("RuleSetId","RuleSetVersion","MigrationRef",
                              "HomeJurisdictionCode","CreatedAt")
                         VALUES
                             ('RAW_EXPORT_REQUIREMENTS',@version,@migration,'VN',
                              transaction_timestamp());
                         INSERT INTO tagekyc.raw_export_requirement_rules
                             ("Id","RuleSetId","RuleSetVersion","RuleSelector",
                              "SelectorOperand","RequirementType","CreatedAt")
                         VALUES
                             (@legal,'RAW_EXPORT_REQUIREMENTS',@version,'Always',
                              NULL,'LegalApproval',transaction_timestamp()),
                             (@consent,'RAW_EXPORT_REQUIREMENTS',@version,
                              'ConsentRequired',NULL,'ConsentArtifact',
                              transaction_timestamp());
                         """,
                         connection,
                         transaction))
        {
            insert.Parameters.AddWithValue("version", version);
            insert.Parameters.AddWithValue("migration", $"tip88b4-m5-v{version}");
            insert.Parameters.AddWithValue("legal", Guid.NewGuid());
            insert.Parameters.AddWithValue("consent", Guid.NewGuid());
            await insert.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private static async Task EnsureWithdrawerAuthorityAsync(TagEkycDbContext db)
    {
        if (await db.RawExportSubjectConsentAuthorities.AnyAsync(item =>
                item.AuthorityPrincipalId == WithdrawerPrincipal &&
                item.ClientApplicationId ==
                Tip88B34AuthorizationEngineTests.ClientApplicationId &&
                item.AuthorityType ==
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer.ToString()))
        {
            return;
        }

        await new EfRawExportSubjectConsentRepository(db)
            .GrantConsentAuthorityAsync(new(
                AdminPrincipal,
                WithdrawerPrincipal,
                Tip88B34AuthorizationEngineTests.ClientApplicationId,
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                ExpectedRevision: 0,
                $"decision:b4-m5-withdrawer:{Guid.NewGuid():N}"));
    }

    private static async Task ApplyAuthorityMutationAsync(
        TagEkycDbContext db,
        AuthorizedPermitFixture fixture,
        string mutation)
    {
        switch (mutation)
        {
            case "session":
                await new EfVerificationSessionRepository(db)
                    .SetStateAsync(fixture.SessionId, VerificationSessionState.Expired);
                break;
            case "grant":
                await new EfRawExportControlPlaneRepository(db)
                    .RevokeExportPolicyGrantAsync(new(
                        AdminPrincipal,
                        Tip88B34AuthorizationEngineTests.ConsumerPrincipal,
                        fixture.PolicyId,
                        1,
                        ExpectedRevision: 1,
                        ClientApplicationId: null,
                        $"decision:b4-revoke:{Guid.NewGuid():N}"));
                break;
            case "lifecycle":
                await new EfRawExportControlPlaneRepository(db)
                    .SuspendPolicyAsync(new(
                        AdminPrincipal,
                        fixture.PolicyId,
                        1,
                        ExpectedRevision: 1,
                        $"decision:b4-suspend:{Guid.NewGuid():N}"));
                break;
            case "fulfillment":
                await new EfRawExportControlPlaneRepository(db)
                    .WithdrawFulfillmentAsync(new(
                        RecorderPrincipal,
                        fixture.PolicyId,
                        1,
                        RawExportRequirementType.LegalApproval,
                        ExpectedRevision: 1,
                        TargetRevision: 1,
                        $"decision:b4-withdraw-fulfillment:{Guid.NewGuid():N}"));
                break;
            case "consent":
                var consent = new EfRawExportSubjectConsentRepository(db);
                if (!await db.RawExportSubjectConsentAuthorities.AnyAsync(item =>
                        item.AuthorityPrincipalId == WithdrawerPrincipal &&
                        item.ClientApplicationId == Tip88B34AuthorizationEngineTests.ClientApplicationId &&
                        item.AuthorityType == RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer.ToString()))
                {
                    await consent.GrantConsentAuthorityAsync(new(
                        AdminPrincipal,
                        WithdrawerPrincipal,
                        Tip88B34AuthorizationEngineTests.ClientApplicationId,
                        RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                        ExpectedRevision: 0,
                        $"decision:b4-consent-authority:{Guid.NewGuid():N}"));
                }
                await consent.RecordSubjectConsentWithdrawnAsync(new(
                    WithdrawerPrincipal,
                    fixture.SessionId,
                    fixture.PolicyId,
                    1,
                    ExpectedRevision: 1,
                    TargetRevision: 1,
                    $"decision:b4-consent-withdraw:{Guid.NewGuid():N}"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null);
        }
    }

    internal sealed record AuthorizedPermitFixture(
        Guid PermitId,
        Guid DecisionId,
        Guid SessionId,
        Guid PolicyId);

    private static async Task<Dictionary<string, List<string>>> ReadCatalogManifestAsync(
        TagEkycDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT 'TABLE',c.relname::text
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relkind='r' AND c.relname LIKE 'raw_export_job_%'
            UNION ALL
            SELECT 'COLUMN',c.relname::text||'|'||a.attname::text
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            WHERE n.nspname='tagekyc' AND c.relkind='r' AND c.relname LIKE 'raw_export_job_%'
            UNION ALL
            SELECT 'CONSTRAINT',con.conname::text
            FROM pg_catalog.pg_constraint con
            JOIN pg_catalog.pg_class c ON c.oid=con.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname LIKE 'raw_export_job_%'
            UNION ALL
            SELECT 'INDEX',i.relname::text
            FROM pg_catalog.pg_index x
            JOIN pg_catalog.pg_class t ON t.oid=x.indrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            JOIN pg_catalog.pg_class i ON i.oid=x.indexrelid
            WHERE n.nspname='tagekyc' AND t.relname LIKE 'raw_export_job_%'
            UNION ALL
            SELECT 'TRIGGER',t.tgname::text
            FROM pg_catalog.pg_trigger t
            JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname LIKE 'raw_export_job_%' AND NOT t.tgisinternal
            UNION ALL
            SELECT 'FUNCTION',p.proname::text
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc'
              AND (p.proname LIKE 'raw_export_%job%' OR p.proname LIKE 'enforce_raw_export_job_%');
            """;

        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            ["TABLE"] = [],
            ["COLUMN"] = [],
            ["CONSTRAINT"] = [],
            ["INDEX"] = [],
            ["TRIGGER"] = [],
            ["FUNCTION"] = [],
        };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(0)].Add(reader.GetString(1));
        }

        return result;
    }

    private static async Task<string> ReadPreB4CatalogSnapshotAsync(TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            WITH objects AS (
              SELECT 'F|'||p.oid::regprocedure::text||'|'||r.rolname||'|'||
                     COALESCE(p.proacl::text,'')||'|'||
                     md5(pg_catalog.pg_get_functiondef(p.oid)) AS value
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_catalog.pg_roles r ON r.oid=p.proowner
              WHERE n.nspname='tagekyc'
                AND NOT (p.proname LIKE 'raw_export_%job%'
                         OR p.proname LIKE 'enforce_raw_export_job_%')
              UNION ALL
              SELECT 'T|'||c.relname::text||'|'||c.relkind::text||'|'||
                     r.rolname||'|'||COALESCE(c.relacl::text,'')
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_catalog.pg_roles r ON r.oid=c.relowner
              WHERE n.nspname='tagekyc'
                AND c.relname NOT LIKE 'raw_export_job_%'
              UNION ALL
              SELECT 'C|'||c.relname::text||'|'||a.attnum::text||'|'||
                     a.attname::text||'|'||
                     pg_catalog.format_type(a.atttypid,a.atttypmod)||'|'||
                     a.attnotnull::text||'|'||
                     COALESCE(pg_catalog.pg_get_expr(d.adbin,d.adrelid),'')||'|'||
                     COALESCE(a.attacl::text,'')
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_catalog.pg_attribute a
                ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
              LEFT JOIN pg_catalog.pg_attrdef d
                ON d.adrelid=a.attrelid AND d.adnum=a.attnum
              WHERE n.nspname='tagekyc'
                AND c.relname NOT LIKE 'raw_export_job_%'
              UNION ALL
              SELECT 'K|'||c.relname::text||'|'||k.conname::text||'|'||
                     k.contype::text||'|'||k.condeferrable::text||'|'||
                     k.condeferred::text||'|'||k.convalidated::text||'|'||
                     pg_catalog.pg_get_constraintdef(k.oid,true)
              FROM pg_catalog.pg_constraint k
              JOIN pg_catalog.pg_class c ON c.oid=k.conrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc'
                AND c.relname NOT LIKE 'raw_export_job_%'
              UNION ALL
              SELECT 'G|'||c.relname::text||'|'||t.tgname::text||'|'||
                     t.tgenabled::text||'|'||
                     pg_catalog.pg_get_triggerdef(t.oid,true)
              FROM pg_catalog.pg_trigger t
              JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc'
                AND c.relname NOT LIKE 'raw_export_job_%'
              UNION ALL
              SELECT 'I|'||tc.relname::text||'|'||ic.relname::text||'|'||
                     i.indisunique::text||'|'||i.indisprimary::text||'|'||
                     i.indisvalid::text||'|'||i.indisready::text||'|'||
                     pg_catalog.pg_get_indexdef(i.indexrelid)
              FROM pg_catalog.pg_index i
              JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid
              JOIN pg_catalog.pg_class tc ON tc.oid=i.indrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=tc.relnamespace
              WHERE n.nspname='tagekyc'
                AND tc.relname NOT LIKE 'raw_export_job_%'
              UNION ALL
              SELECT 'R|'||rolname||'|'||rolcanlogin::text||'|'||rolinherit::text||'|'||
                     rolsuper::text||'|'||rolcreatedb::text||'|'||rolcreaterole::text||'|'||
                     rolreplication::text||'|'||rolbypassrls::text
              FROM pg_catalog.pg_roles
              WHERE rolname LIKE 'tagekyc%'
              UNION ALL
              SELECT 'M|'||granted.rolname||'|'||member.rolname||'|'||
                     m.admin_option::text||'|'||m.inherit_option::text||'|'||m.set_option::text
              FROM pg_catalog.pg_auth_members m
              JOIN pg_catalog.pg_roles granted ON granted.oid=m.roleid
              JOIN pg_catalog.pg_roles member ON member.oid=m.member
              WHERE granted.rolname LIKE 'tagekyc%' OR member.rolname LIKE 'tagekyc%'
            )
            SELECT count(*)::text||'|'||md5(string_agg(value,E'\n' ORDER BY value))
            FROM objects;
            """;
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task AssertActorContextFailureAsync(
        string? context,
        Guid passedPrincipal,
        string expectedMessage)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (context is not null)
        {
            await SetActorAsync(connection, transaction, context);
        }

        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_job(@principal,@client,@job);",
            connection,
            transaction);
        command.Parameters.AddWithValue("principal", passedPrincipal);
        command.Parameters.AddWithValue("client", Guid.NewGuid());
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal(expectedMessage, exception.MessageText);
    }

    private static async Task AssertExactTerminalEvidenceAsync(
        TagEkycDbContext db,
        Guid jobId,
        string failureCode)
    {
        var head = await db.RawExportJobOperationalHeads.SingleAsync(
            item => item.JobId == jobId);
        Assert.Equal("TerminalFailed", head.CurrentState);
        Assert.Equal(1, head.Revision);
        Assert.Equal(0, head.FencingToken);
        Assert.Null(head.CurrentAttemptId);
        Assert.Null(head.LeaseOwnerId);
        Assert.Null(head.LeaseExpiresAt);
        Assert.Equal(0, await db.RawExportJobAttempts.CountAsync(
            item => item.JobId == jobId));

        var transitions = await db.RawExportJobTransitions
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.ResultingRevision)
            .ToArrayAsync();
        Assert.Equal(2, transitions.Length);
        Assert.Equal("JobBound", transitions[0].EventType);
        Assert.Equal("JobTerminalFailed", transitions[1].EventType);
        Assert.Equal("Claimed", transitions[1].FromState);
        Assert.Equal("TerminalFailed", transitions[1].ToState);
        Assert.Equal(failureCode, transitions[1].FailureCode);
        Assert.Equal(1, transitions[1].ResultingRevision);
        Assert.Equal(0, transitions[1].FencingToken);
        Assert.Null(transitions[1].AttemptId);
        Assert.Null(transitions[1].ResultingLeaseOwnerId);
        Assert.Null(transitions[1].ResultingLeaseExpiresAt);
    }

    private static async Task AssertReadinessFailureAsync(
        TagEkycDbContext db,
        string expectedCode)
    {
        var exception = await Assert.ThrowsAsync<RawExportJobReadinessException>(
            () => new RawExportJobReadinessValidator(
                    db,
                    new RawExportJobLeaseState(60, true, null))
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private static Task HealthyReadinessAsync(TagEkycDbContext db) =>
        new RawExportJobReadinessValidator(
                db,
                new RawExportJobLeaseState(60, true, null))
            .ValidateAsync(CancellationToken.None);

    private async Task<PostgresException> InvokeDirectClaimExpectingFailureAsync(
        string idempotencyKey,
        System.Data.IsolationLevel isolation = System.Data.IsolationLevel.ReadCommitted)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(isolation);
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_claim_or_read_job(
              @job,@permit,@decision,@principal,@client,@api,@session,@subject,
              @policy,@version,@purpose,@recipient,@mode,@permit_expiry,@job_expiry,
              @key,@fingerprint,@classes);
            """,
            connection,
            transaction);
        var now = DateTimeOffset.UtcNow.AddMinutes(5);
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        command.Parameters.AddWithValue("permit", Guid.NewGuid());
        command.Parameters.AddWithValue("decision", Guid.NewGuid());
        command.Parameters.AddWithValue("principal", actor.PrincipalId);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        command.Parameters.AddWithValue("api", actor.ApiKeyId);
        command.Parameters.AddWithValue("session", Guid.NewGuid());
        command.Parameters.AddWithValue("subject", "subject");
        command.Parameters.AddWithValue("policy", Guid.NewGuid());
        command.Parameters.AddWithValue("version", 1);
        command.Parameters.AddWithValue("purpose", "purpose");
        command.Parameters.AddWithValue("recipient", actor.ClientApplicationId);
        command.Parameters.AddWithValue("mode", RawExportMode.ExternalExportOnlyNoRetain.ToString());
        command.Parameters.AddWithValue("permit_expiry", now);
        command.Parameters.AddWithValue("job_expiry", now);
        command.Parameters.AddWithValue("key", idempotencyKey);
        command.Parameters.AddWithValue("fingerprint", new byte[32]);
        command.Parameters.AddWithValue("classes", new[] { RawExportRawClass.LiveSelfieImage.ToString() });
        return await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
    }

    private async Task<PostgresException> InvokeDirectLeaseWithInvalidBoundAsync(
        string function,
        Guid passedPrincipal,
        int? leaseSeconds = 9)
    {
        Assert.Contains(
            function,
            new[]
            {
                "raw_export_acquire_or_reclaim_job_lease",
                "raw_export_renew_job_lease",
            });
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(
            $"""
             SELECT * FROM tagekyc.{function}(
               @job,@principal,@client,0,0,@attempt,@owner,@seconds);
             """,
            connection,
            transaction);
        command.Parameters.AddWithValue("job", Guid.NewGuid());
        command.Parameters.AddWithValue("principal", passedPrincipal);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        command.Parameters.AddWithValue("attempt", Guid.NewGuid());
        command.Parameters.AddWithValue("owner", Guid.NewGuid());
        command.Parameters.Add(
            new NpgsqlParameter("seconds", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = leaseSeconds is null ? DBNull.Value : leaseSeconds.Value,
            });
        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
    }

    private async Task AssertAcquireConcurrencyConflictAsync(
        Guid jobId,
        long? expectedRevision,
        long? expectedFencingToken)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.raw_export_acquire_or_reclaim_job_lease(
              @job,@principal,@client,@revision,@fence,@attempt,@owner,60);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("job", jobId);
        command.Parameters.AddWithValue("principal", actor.PrincipalId);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        command.Parameters.Add(
            new NpgsqlParameter("revision", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = expectedRevision is null ? DBNull.Value : expectedRevision.Value,
            });
        command.Parameters.Add(
            new NpgsqlParameter("fence", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = expectedFencingToken is null ? DBNull.Value : expectedFencingToken.Value,
            });
        command.Parameters.AddWithValue("attempt", Guid.NewGuid());
        command.Parameters.AddWithValue("owner", Guid.NewGuid());
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("ConcurrencyConflict", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));
        Assert.True(reader.IsDBNull(3));
        Assert.False(await reader.ReadAsync());
        await reader.DisposeAsync();
        await transaction.RollbackAsync();
    }

    private async Task AssertDirectOutcomeAsync(
        string function,
        Guid jobId,
        long? expectedRevision,
        long? expectedFencingToken,
        Guid? attemptId,
        Guid? leaseOwnerId,
        int? leaseSeconds,
        string? value,
        string expectedOutcome,
        string? reason = null)
    {
        var sql = function switch
        {
            "lock" =>
                """
                SELECT * FROM tagekyc.raw_export_lock_job_for_attempt(
                  @job,@principal,@client,@revision,@fence);
                """,
            "renew" =>
                """
                SELECT * FROM tagekyc.raw_export_renew_job_lease(
                  @job,@principal,@client,@revision,@fence,@attempt,@owner,@seconds);
                """,
            "failure" =>
                """
                SELECT * FROM tagekyc.raw_export_record_job_attempt_failure(
                  @job,@principal,@client,@revision,@fence,@attempt,@owner,@value);
                """,
            "terminalize" =>
                """
                SELECT * FROM tagekyc.raw_export_terminalize_job(
                  @job,@principal,@client,@revision,@fence,@attempt,@owner,@value,@reason);
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(function)),
        };
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor.PrincipalId.ToString());
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("job", jobId);
        command.Parameters.AddWithValue("principal", actor.PrincipalId);
        command.Parameters.AddWithValue("client", actor.ClientApplicationId);
        command.Parameters.Add(
            new NpgsqlParameter("revision", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = expectedRevision is null ? DBNull.Value : expectedRevision.Value,
            });
        command.Parameters.Add(
            new NpgsqlParameter("fence", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = expectedFencingToken is null ? DBNull.Value : expectedFencingToken.Value,
            });
        if (function != "lock")
        {
            command.Parameters.Add(
                new NpgsqlParameter("attempt", NpgsqlTypes.NpgsqlDbType.Uuid)
                {
                    Value = attemptId is null ? DBNull.Value : attemptId.Value,
                });
            command.Parameters.Add(
                new NpgsqlParameter("owner", NpgsqlTypes.NpgsqlDbType.Uuid)
                {
                    Value = leaseOwnerId is null ? DBNull.Value : leaseOwnerId.Value,
                });
        }
        if (function == "renew")
        {
            command.Parameters.Add(
                new NpgsqlParameter("seconds", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = leaseSeconds is null ? DBNull.Value : leaseSeconds.Value,
                });
        }
        else if (function is "failure" or "terminalize")
        {
            command.Parameters.Add(
                new NpgsqlParameter("value", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = value is null ? DBNull.Value : value,
                });
        }
        if (function == "terminalize")
        {
            command.Parameters.Add(
                new NpgsqlParameter("reason", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = reason is null ? DBNull.Value : reason,
                });
        }

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(expectedOutcome, reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
        Assert.True(reader.IsDBNull(2));
        Assert.True(reader.IsDBNull(3));
        Assert.False(await reader.ReadAsync());
        await reader.DisposeAsync();
        await transaction.RollbackAsync();
    }

    private async Task<int> CountWithSeparateContextAsync(
        Func<TagEkycDbContext, Task<int>> count)
    {
        await using var db = postgres.CreateDbContext();
        return await count(db);
    }

    private async Task AssertConnectionLifecycleCellAsync(
        string method,
        string exit)
    {
        LifecycleFixture fixture;
        await using (var setup = postgres.CreateDbContext())
        {
            fixture = await PrepareLifecycleFixtureAsync(setup, method);
        }

        await using var db = postgres.CreateDbContext();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await AssertLandedEligibilityReadAsync(db, fixture.Permit);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        Assert.Null(db.Database.CurrentTransaction);

        var repository = CreateJobRepository(db);
        switch (exit)
        {
            case "Success":
                await InvokePreparedRepositoryMethodAsync(
                    repository,
                    method,
                    fixture,
                    CancellationToken.None);
                break;
            case "TypedFailure":
                await InvokeTypedFailureAsync(repository, method);
                break;
            case "ProviderException":
                await InvokeBlockedAndTerminateBackendAsync(
                    db,
                    repository,
                    method,
                    fixture);
                break;
            case "Cancellation":
                await InvokeBlockedAndCancelAsync(
                    db,
                    repository,
                    method,
                    fixture);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(exit), exit, null);
        }

        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        await AssertLandedEligibilityReadAsync(db, fixture.Permit);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    private async Task<LifecycleFixture> PrepareLifecycleFixtureAsync(
        TagEkycDbContext db,
        string method)
    {
        var permit = await CreateAuthorizedPermitAsync(
            db,
            [RawExportRawClass.LiveSelfieImage]);
        if (method == "Bind")
        {
            return new(permit, null, null, Guid.Empty, 0, 0);
        }

        var repository = CreateJobRepository(db);
        var bind = await repository.BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            permit.PermitId,
            $"b4-lifecycle-setup-{method}-{Guid.NewGuid():N}"));
        if (method is not ("Renew" or "Failure"))
        {
            return new(permit, bind.JobId, null, Guid.Empty, 0, 0);
        }

        var owner = Guid.NewGuid();
        var leaseResult = await repository.AcquireOrReclaimLeaseAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            bind.JobId,
            0,
            0,
            owner));
        Assert.NotNull(leaseResult.AttemptId);
        Assert.NotNull(leaseResult.Revision);
        Assert.NotNull(leaseResult.FencingToken);
        return new(
            permit,
            bind.JobId,
            leaseResult.AttemptId,
            owner,
            leaseResult.Revision.Value,
            leaseResult.FencingToken.Value);
    }

    private static async Task AssertLandedEligibilityReadAsync(
        TagEkycDbContext db,
        AuthorizedPermitFixture fixture)
    {
        await using (var transaction = await db.Database.BeginTransactionAsync(
                         System.Data.IsolationLevel.ReadCommitted))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT set_config(
                    'tagekyc.actor_principal_id',
                    {Tip88B34AuthorizationEngineTests.Actor.PrincipalId.ToString()},
                    true);
                """);
            var projections = new EfRawExportAuthorizationProjectionReader(db);
            var snapshot = await new EfRawExportControlPlaneRepository(db, projections)
                .ResolveExportEligibilityForAuthorizationAsync(
                    Tip88B34AuthorizationEngineTests.Actor.PrincipalId,
                    fixture.PolicyId,
                    1);
            Assert.Equal(RawExportEligibilityState.Active, snapshot.State);
            await transaction.RollbackAsync();
        }
    }

    private static async Task InvokePreparedRepositoryMethodAsync(
        IRawExportJobRepository repository,
        string method,
        LifecycleFixture fixture,
        CancellationToken cancellationToken)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        switch (method)
        {
            case "Bind":
                await repository.BindAsync(new(
                    actor,
                    fixture.Permit.PermitId,
                    $"b4-lifecycle-bind-{Guid.NewGuid():N}"),
                    cancellationToken);
                break;
            case "Read":
                var read = await repository.ReadAsync(
                    new(actor, fixture.JobId!.Value),
                    cancellationToken);
                Assert.Equal(RawExportJobReadStatus.Found, read.Status);
                break;
            case "Acquire":
                var acquired = await repository.AcquireOrReclaimLeaseAsync(new(
                    actor,
                    fixture.JobId!.Value,
                    0,
                    0,
                    Guid.NewGuid()),
                    cancellationToken);
                Assert.Equal(RawExportJobLeaseStatus.Acquired, acquired.Status);
                break;
            case "Renew":
                await repository.RenewLeaseAsync(new(
                    actor,
                    fixture.JobId!.Value,
                    fixture.Revision,
                    fixture.FencingToken,
                    fixture.AttemptId!.Value,
                    fixture.LeaseOwnerId),
                    cancellationToken);
                break;
            case "Failure":
                await repository.RecordAttemptFailureAsync(new(
                    actor,
                    fixture.JobId!.Value,
                    fixture.Revision,
                    fixture.FencingToken,
                    fixture.AttemptId!.Value,
                    fixture.LeaseOwnerId,
                    RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE),
                    cancellationToken);
                break;
            case "Terminalize":
                await repository.TerminalizeAsync(new(
                    actor,
                    fixture.JobId!.Value,
                    0,
                    0,
                    null,
                    null,
                    RawExportJobState.Cancelled,
                    RawExportJobTerminalReasonCode.REQUEST_CANCELLED),
                    cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
    }

    private static async Task InvokeTypedFailureAsync(
        IRawExportJobRepository repository,
        string method)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var jobId = Guid.NewGuid();
        switch (method)
        {
            case "Bind":
                var bindException = await Assert.ThrowsAsync<RawExportJobException>(
                    () => repository.BindAsync(new(
                        actor,
                        Guid.NewGuid(),
                        $"b4-lifecycle-missing-{Guid.NewGuid():N}")));
                Assert.Equal("RAW_EXPORT_JOB_PERMIT_UNAVAILABLE", bindException.Code);
                break;
            case "Read":
                var read = await repository.ReadAsync(new(actor, jobId));
                Assert.Equal(RawExportJobReadStatus.NotFound, read.Status);
                break;
            case "Acquire":
                var acquire = await repository.AcquireOrReclaimLeaseAsync(new(
                    actor,
                    jobId,
                    0,
                    0,
                    Guid.NewGuid()));
                Assert.Equal(RawExportJobLeaseStatus.NotFound, acquire.Status);
                break;
            case "Renew":
                var renewException = await Assert.ThrowsAsync<RawExportJobException>(
                    () => repository.RenewLeaseAsync(new(
                        actor,
                        jobId,
                        0,
                        0,
                        Guid.NewGuid(),
                        Guid.NewGuid())));
                Assert.Equal("RAW_EXPORT_JOB_CONCURRENCY_CONFLICT", renewException.Code);
                break;
            case "Failure":
                var failureException = await Assert.ThrowsAsync<RawExportJobException>(
                    () => repository.RecordAttemptFailureAsync(new(
                        actor,
                        jobId,
                        0,
                        0,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)));
                Assert.Equal("RAW_EXPORT_JOB_CONCURRENCY_CONFLICT", failureException.Code);
                break;
            case "Terminalize":
                var terminalizeException = await Assert.ThrowsAsync<RawExportJobException>(
                    () => repository.TerminalizeAsync(new(
                        actor,
                        jobId,
                        0,
                        0,
                        null,
                        null,
                        RawExportJobState.Cancelled,
                        RawExportJobTerminalReasonCode.REQUEST_CANCELLED)));
                Assert.Equal("RAW_EXPORT_JOB_CONCURRENCY_CONFLICT", terminalizeException.Code);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
    }

    private async Task InvokeBlockedAndCancelAsync(
        TagEkycDbContext db,
        IRawExportJobRepository repository,
        string method,
        LifecycleFixture fixture)
    {
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockLifecycleTableAsync(blocker, blockerTransaction, method);
        using var cancellation = new CancellationTokenSource();
        var invocation = InvokePreparedRepositoryMethodAsync(
            repository,
            method,
            fixture,
            cancellation.Token);
        await WaitForAsync(
            () => db.Database.GetDbConnection().State == System.Data.ConnectionState.Open,
            TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation);
        }
        finally
        {
            await blockerTransaction.RollbackAsync();
        }

        if (method == "Bind")
        {
            Assert.Equal(
                0,
                await CountWithSeparateContextAsync(
                    context => context.RawExportJobOperationalHeads.CountAsync()));
            Assert.Equal(
                0,
                await CountWithSeparateContextAsync(
                    context => context.RawExportJobTransitions.CountAsync()));
        }
    }

    private async Task InvokeBlockedAndTerminateBackendAsync(
        TagEkycDbContext db,
        IRawExportJobRepository repository,
        string method,
        LifecycleFixture fixture)
    {
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await LockLifecycleTableAsync(blocker, blockerTransaction, method);
        var invocation = InvokePreparedRepositoryMethodAsync(
            repository,
            method,
            fixture,
            CancellationToken.None);
        await WaitForAsync(
            () => db.Database.GetDbConnection().State == System.Data.ConnectionState.Open,
            TimeSpan.FromSeconds(5));
        var processId = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;

        await using (var admin = new NpgsqlConnection(postgres.ConnectionString))
        {
            await admin.OpenAsync();
            await using var terminate = new NpgsqlCommand(
                "SELECT pg_terminate_backend(@pid);",
                admin);
            terminate.Parameters.AddWithValue("pid", processId);
            Assert.True((bool)(await terminate.ExecuteScalarAsync())!);
        }

        try
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => invocation);
            Assert.True(
                exception is NpgsqlException or ObjectDisposedException,
                $"Unexpected provider-disconnect exception: {exception.GetType().FullName}");
        }
        finally
        {
            await blockerTransaction.RollbackAsync();
        }
    }

    private static async Task LockLifecycleTableAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string method)
    {
        var table = method == "Bind"
            ? "tagekyc.raw_export_authorization_permits"
            : "tagekyc.raw_export_job_identities";
        await using var command = new NpgsqlCommand(
            $"LOCK TABLE {table} IN ACCESS EXCLUSIVE MODE;",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertRequestValidationAsync(
        IRawExportJobRepository repository,
        string method)
    {
        var exception = await Assert.ThrowsAsync<RawExportJobException>(
            () => InvokeInvalidRepositoryMethodAsync(repository, method));
        Assert.Equal("RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED", exception.Code);
    }

    private static Task InvokeInvalidRepositoryMethodAsync(
        IRawExportJobRepository repository,
        string method)
    {
        var invalidActor = new AuthenticatedRawExportActor(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid());
        var jobId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        return method switch
        {
            "Bind" => repository.BindAsync(new(invalidActor, Guid.NewGuid(), "b4-invalid-bind")),
            "Read" => repository.ReadAsync(new(invalidActor, jobId)),
            "Acquire" => repository.AcquireOrReclaimLeaseAsync(new(invalidActor, jobId, 0, 0, owner)),
            "Renew" => repository.RenewLeaseAsync(new(invalidActor, jobId, 0, 0, attemptId, owner)),
            "Failure" => repository.RecordAttemptFailureAsync(new(
                invalidActor,
                jobId,
                0,
                0,
                attemptId,
                owner,
                RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)),
            "Terminalize" => repository.TerminalizeAsync(new(
                invalidActor,
                jobId,
                0,
                0,
                null,
                null,
                RawExportJobState.Cancelled,
                RawExportJobTerminalReasonCode.REQUEST_CANCELLED)),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };
    }

    private static Task InvokeRepositoryMethodAsync(
        IRawExportJobRepository repository,
        string method)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        var jobId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        return method switch
        {
            "Bind" => repository.BindAsync(new(actor, Guid.NewGuid(), "b4-six-bind")),
            "Read" => repository.ReadAsync(new(actor, jobId)),
            "Acquire" => repository.AcquireOrReclaimLeaseAsync(new(actor, jobId, 0, 0, owner)),
            "Renew" => repository.RenewLeaseAsync(new(actor, jobId, 0, 0, attemptId, owner)),
            "Failure" => repository.RecordAttemptFailureAsync(new(
                actor,
                jobId,
                0,
                0,
                attemptId,
                owner,
                RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE)),
            "Terminalize" => repository.TerminalizeAsync(new(
                actor,
                jobId,
                0,
                0,
                null,
                null,
                RawExportJobState.Cancelled,
                RawExportJobTerminalReasonCode.REQUEST_CANCELLED)),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };
    }

    private static async Task<CapturedBind> CaptureAsync(Task<RawExportJobBindResult> task)
    {
        try
        {
            return new(await task, null);
        }
        catch (Exception exception)
        {
            return new(null, exception);
        }
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string value)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('tagekyc.actor_principal_id',@value,true);",
            connection,
            transaction);
        command.Parameters.AddWithValue("value", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task LockPermitAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid permitId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT "PermitId"
            FROM tagekyc.raw_export_authorization_permits
            WHERE "PermitId"=@permit
            FOR UPDATE;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("permit", permitId);
        Assert.Equal(permitId, (Guid)(await command.ExecuteScalarAsync())!);
    }

    private static async Task LockHeadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid jobId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT "JobId"
            FROM tagekyc.raw_export_job_operational_heads
            WHERE "JobId"=@job
            FOR UPDATE;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("job", jobId);
        Assert.Equal(jobId, (Guid)(await command.ExecuteScalarAsync())!);
    }

    private static Task WaitUntilAfterAsync(DateTimeOffset deadline)
    {
        var delay = deadline - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(350);
        return delay > TimeSpan.Zero ? Task.Delay(delay) : Task.CompletedTask;
    }

    private static async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Timed out waiting for the expected test state.");
            }

            await Task.Delay(10);
        }
    }

    private static string ReadB4RepositorySource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ??
                   throw new InvalidOperationException("Repository root was not found.");
        return File.ReadAllText(Path.Combine(
            root,
            "src",
            "TagEkyc.Infrastructure",
            "Persistence",
            "EfRawExportJobRepository.cs"));
    }

    private static string ReadTransactionAdmissionSource()
    {
        var source = ReadB4RepositorySource();
        var start = source.IndexOf(
            "private void EnsureTransactionAdmission(",
            StringComparison.Ordinal);
        var end = source.IndexOf(
            "private static async Task SetActorAsync(",
            start,
            StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return source[start..end];
    }

    private async Task AssertAttemptCheckAsync(
        string phase,
        int ordinal,
        long fence,
        DateTimeOffset acquiredAt,
        DateTimeOffset expiresAt,
        string expectedConstraint)
    {
        await using var db = postgres.CreateDbContext();
        var fixture = await CreateAuthorizedPermitAsync(db, [RawExportRawClass.LiveSelfieImage]);
        var bind = await CreateJobRepository(db).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            fixture.PermitId,
            $"b4-m10-check-{Guid.NewGuid():N}"));

        await InsertAttemptWithTriggersDisabledAsync(
            bind.JobId,
            "Assembling",
            0,
            1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(1),
            expectFailure: false);
        var exception = await InsertAttemptWithTriggersDisabledAsync(
            bind.JobId,
            phase,
            ordinal,
            fence,
            acquiredAt,
            expiresAt,
            expectFailure: true);

        Assert.Equal("23514", exception!.SqlState);
        Assert.Equal(expectedConstraint, exception.ConstraintName);
    }

    private async Task<PostgresException?> InsertAttemptWithTriggersDisabledAsync(
        Guid jobId,
        string phase,
        int ordinal,
        long fence,
        DateTimeOffset acquiredAt,
        DateTimeOffset expiresAt,
        bool expectFailure)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var replication = new NpgsqlCommand(
                         "SET LOCAL session_replication_role=replica;",
                         connection,
                         transaction))
        {
            await replication.ExecuteNonQueryAsync();
        }
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_job_attempts
                ("AttemptId","JobId","AttemptOrdinal","Phase","LeaseOwnerId",
                 "FencingToken","AcquiredAt","InitialLeaseExpiresAt")
            VALUES
                (@attempt,@job,@ordinal,@phase,@owner,@fence,@acquired,@expires);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("attempt", Guid.NewGuid());
        command.Parameters.AddWithValue("job", jobId);
        command.Parameters.AddWithValue("ordinal", ordinal);
        command.Parameters.AddWithValue("phase", phase);
        command.Parameters.AddWithValue("owner", Guid.NewGuid());
        command.Parameters.AddWithValue("fence", fence);
        command.Parameters.AddWithValue("acquired", acquiredAt);
        command.Parameters.AddWithValue("expires", expiresAt);
        if (expectFailure)
        {
            return await Assert.ThrowsAsync<PostgresException>(
                () => command.ExecuteNonQueryAsync());
        }

        Assert.Equal(1, await command.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
        return null;
    }

    private async Task AssertGuardFailureAsync(string sql, string expectedMessage)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal(expectedMessage, exception.MessageText);
    }

    private async Task SetPermitClassPresenceAsync(
        RawExportPermitClassRow permitClass,
        bool present)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var replica = new NpgsqlCommand(
                         "SET LOCAL session_replication_role=replica;",
                         connection,
                         transaction))
        {
            await replica.ExecuteNonQueryAsync();
        }
        await using var command = new NpgsqlCommand(
            present
                ? """
                  INSERT INTO tagekyc.raw_export_permit_classes
                      ("PermitId","RawClass","Ordinal")
                  VALUES (@permit,@class,@ordinal);
                  """
                : """
                  DELETE FROM tagekyc.raw_export_permit_classes
                  WHERE "PermitId"=@permit AND "Ordinal"=@ordinal;
                  """,
            connection,
            transaction);
        command.Parameters.AddWithValue("permit", permitClass.PermitId);
        command.Parameters.AddWithValue("ordinal", permitClass.Ordinal);
        if (present)
        {
            command.Parameters.AddWithValue("class", permitClass.RawClass);
        }
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
        await transaction.CommitAsync();
    }

    private static async Task<DirectClaimResult> DirectClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid permitId,
        string idempotencyKey,
        RawExportMode? modeOverride = null)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        DirectProjection? projection = null;
        var classes = new List<RawExportRawClass>();
        await using (var read = new NpgsqlCommand(
                         "SELECT * FROM tagekyc.raw_export_read_job_binding_inputs(@principal,@client,@permit);",
                         connection,
                         transaction))
        {
            read.Parameters.AddWithValue("principal", actor.PrincipalId);
            read.Parameters.AddWithValue("client", actor.ClientApplicationId);
            read.Parameters.AddWithValue("permit", permitId);
            await using var reader = await read.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.Equal("Valid", reader.GetString(0));
                projection ??= new(
                    reader.GetGuid(3),
                    reader.GetGuid(6),
                    reader.GetString(7),
                    reader.GetGuid(8),
                    reader.GetInt32(9),
                    reader.GetString(10),
                    reader.GetGuid(12),
                    ReadTimestamp(reader, 13),
                    Enum.Parse<RawExportMode>(reader.GetString(15), false));
                classes.Add(Enum.Parse<RawExportRawClass>(reader.GetString(18), false));
            }
        }
        Assert.NotNull(projection);
        var requestedMode = modeOverride ?? projection!.ExportMode;
        var jobId = Guid.NewGuid();
        var fingerprint = RawExportJobFingerprintCodec.Compute(
            actor.PrincipalId,
            actor.ClientApplicationId,
            permitId,
            projection!.AuthorizationDecisionId,
            projection.VerificationSessionId,
            projection.SubjectRef,
            projection.PolicyId,
            projection.PolicyVersion,
            projection.PurposeCode,
            projection.RecipientClientApplicationId,
            requestedMode,
            projection.PermitExpiresAt,
            projection.PermitExpiresAt,
            idempotencyKey,
            classes);
        await using var claim = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_claim_or_read_job(
              @job,@permit,@decision,@principal,@client,@api,@session,@subject,
              @policy,@version,@purpose,@recipient,@mode,@permit_expiry,@job_expiry,
              @key,@fingerprint,@classes);
            """,
            connection,
            transaction);
        claim.Parameters.AddWithValue("job", jobId);
        claim.Parameters.AddWithValue("permit", permitId);
        claim.Parameters.AddWithValue("decision", projection.AuthorizationDecisionId);
        claim.Parameters.AddWithValue("principal", actor.PrincipalId);
        claim.Parameters.AddWithValue("client", actor.ClientApplicationId);
        claim.Parameters.AddWithValue("api", actor.ApiKeyId);
        claim.Parameters.AddWithValue("session", projection.VerificationSessionId);
        claim.Parameters.AddWithValue("subject", projection.SubjectRef);
        claim.Parameters.AddWithValue("policy", projection.PolicyId);
        claim.Parameters.AddWithValue("version", projection.PolicyVersion);
        claim.Parameters.AddWithValue("purpose", projection.PurposeCode);
        claim.Parameters.AddWithValue("recipient", projection.RecipientClientApplicationId);
        claim.Parameters.AddWithValue("mode", requestedMode.ToString());
        claim.Parameters.AddWithValue("permit_expiry", projection.PermitExpiresAt);
        claim.Parameters.AddWithValue("job_expiry", projection.PermitExpiresAt);
        claim.Parameters.AddWithValue("key", idempotencyKey);
        claim.Parameters.AddWithValue("fingerprint", fingerprint);
        claim.Parameters.AddWithValue("classes", classes.Select(item => item.ToString()).ToArray());
        await using var result = await claim.ExecuteReaderAsync();
        Assert.True(await result.ReadAsync());
        var outcome = result.GetString(0);
        var returnedJob = result.IsDBNull(1) ? (Guid?)null : result.GetGuid(1);
        Assert.False(await result.ReadAsync());
        return new(outcome, returnedJob);
    }

    private static async Task AssertRuntimeEntryCallsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid jobId)
    {
        var actor = Tip88B34AuthorizationEngineTests.Actor;
        await using (var read = new NpgsqlCommand(
                         "SELECT count(*) FROM tagekyc.raw_export_read_job(@principal,@client,@job);",
                         connection,
                         transaction))
        {
            read.Parameters.AddWithValue("principal", actor.PrincipalId);
            read.Parameters.AddWithValue("client", actor.ClientApplicationId);
            read.Parameters.AddWithValue("job", jobId);
            Assert.Equal(1L, (long)(await read.ExecuteScalarAsync())!);
        }
        await using (var lockJob = new NpgsqlCommand(
                         """
                         SELECT outcome FROM tagekyc.raw_export_lock_job_for_attempt(
                           @job,@principal,@client,0,0);
                         """,
                         connection,
                         transaction))
        {
            lockJob.Parameters.AddWithValue("job", jobId);
            lockJob.Parameters.AddWithValue("principal", actor.PrincipalId);
            lockJob.Parameters.AddWithValue("client", actor.ClientApplicationId);
            Assert.Equal("Locked", (string)(await lockJob.ExecuteScalarAsync())!);
        }

        var attempt = Guid.NewGuid();
        var owner = Guid.NewGuid();
        await using (var acquire = new NpgsqlCommand(
                         """
                         SELECT outcome FROM tagekyc.raw_export_acquire_or_reclaim_job_lease(
                           @job,@principal,@client,0,0,@attempt,@owner,60);
                         """,
                         connection,
                         transaction))
        {
            acquire.Parameters.AddWithValue("job", jobId);
            acquire.Parameters.AddWithValue("principal", actor.PrincipalId);
            acquire.Parameters.AddWithValue("client", actor.ClientApplicationId);
            acquire.Parameters.AddWithValue("attempt", attempt);
            acquire.Parameters.AddWithValue("owner", owner);
            Assert.Equal("Acquired", (string)(await acquire.ExecuteScalarAsync())!);
        }
        await using (var renew = new NpgsqlCommand(
                         """
                         SELECT outcome FROM tagekyc.raw_export_renew_job_lease(
                           @job,@principal,@client,1,1,@attempt,@owner,60);
                         """,
                         connection,
                         transaction))
        {
            renew.Parameters.AddWithValue("job", jobId);
            renew.Parameters.AddWithValue("principal", actor.PrincipalId);
            renew.Parameters.AddWithValue("client", actor.ClientApplicationId);
            renew.Parameters.AddWithValue("attempt", attempt);
            renew.Parameters.AddWithValue("owner", owner);
            Assert.Equal("Renewed", (string)(await renew.ExecuteScalarAsync())!);
        }
        await using (var failure = new NpgsqlCommand(
                         """
                         SELECT outcome FROM tagekyc.raw_export_record_job_attempt_failure(
                           @job,@principal,@client,2,1,@attempt,@owner,
                           'ATTEMPT_EXECUTION_FAILED_RETRYABLE');
                         """,
                         connection,
                         transaction))
        {
            failure.Parameters.AddWithValue("job", jobId);
            failure.Parameters.AddWithValue("principal", actor.PrincipalId);
            failure.Parameters.AddWithValue("client", actor.ClientApplicationId);
            failure.Parameters.AddWithValue("attempt", attempt);
            failure.Parameters.AddWithValue("owner", owner);
            Assert.Equal(
                "TerminalizedModeRetryForbidden",
                (string)(await failure.ExecuteScalarAsync())!);
        }
        await using (var terminal = new NpgsqlCommand(
                         """
                         SELECT outcome FROM tagekyc.raw_export_terminalize_job(
                           @job,@principal,@client,3,1,@attempt,@owner,
                           'Cancelled','REQUEST_CANCELLED');
                         """,
                         connection,
                         transaction))
        {
            terminal.Parameters.AddWithValue("job", jobId);
            terminal.Parameters.AddWithValue("principal", actor.PrincipalId);
            terminal.Parameters.AddWithValue("client", actor.ClientApplicationId);
            terminal.Parameters.AddWithValue("attempt", attempt);
            terminal.Parameters.AddWithValue("owner", owner);
            Assert.Equal("AlreadyTerminal", (string)(await terminal.ExecuteScalarAsync())!);
        }
    }

    private static DateTimeOffset ReadTimestamp(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException($"Unexpected timestamp type {value.GetType().FullName}."),
        };
    }

    private static async Task<DateTimeOffset> ReadDatabaseClockAsync(TagEkycDbContext db)
    {
        await using var connection = new NpgsqlConnection(
            db.Database.GetConnectionString() ??
            throw new InvalidOperationException("Connection string is unavailable."));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT clock_timestamp();", connection);
        var value = await command.ExecuteScalarAsync();
        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("Database clock returned an unexpected value."),
        };
    }

    private sealed record CapturedBind(
        RawExportJobBindResult? Result,
        Exception? Exception);

    private sealed record LifecycleFixture(
        AuthorizedPermitFixture Permit,
        Guid? JobId,
        Guid? AttemptId,
        Guid LeaseOwnerId,
        long Revision,
        long FencingToken);

    private sealed record DirectProjection(
        Guid AuthorizationDecisionId,
        Guid VerificationSessionId,
        string SubjectRef,
        Guid PolicyId,
        int PolicyVersion,
        string PurposeCode,
        Guid RecipientClientApplicationId,
        DateTimeOffset PermitExpiresAt,
        RawExportMode ExportMode);

    private sealed record DirectClaimResult(string Outcome, Guid? JobId);

    private sealed class EvaluatedAtDriftProjectionReader(
        IRawExportAuthorizationProjectionReader inner)
        : IRawExportAuthorizationProjectionReader
    {
        public Task<RawExportAuthorizationEligibilityProjection> ReadEligibilityInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default) =>
            inner.ReadEligibilityInputsAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);

        public async Task<RawExportAuthorizationPolicyProjection> ReadPolicyInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default)
        {
            var projection = await inner.ReadPolicyInputsAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            return projection with
            {
                EvaluatedAtUtc = projection.EvaluatedAtUtc.AddTicks(1),
            };
        }
    }

    private sealed class PausingControlPlaneRepository(
        IRawExportControlPlaneRepository inner)
        : IRawExportControlPlaneRepository
    {
        private readonly TaskCompletionSource reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => reached.Task;

        public void Release() => released.TrySetResult();

        public Task<int> GrantExportPolicyAsync(
            RawExportGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantExportPolicyAsync(command, cancellationToken);

        public Task<int> RevokeExportPolicyGrantAsync(
            RawExportGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeExportPolicyGrantAsync(command, cancellationToken);

        public Task<int> GrantControlAuthorityAsync(
            RawExportAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantControlAuthorityAsync(command, cancellationToken);

        public Task<int> RevokeControlAuthorityAsync(
            RawExportAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeControlAuthorityAsync(command, cancellationToken);

        public Task<int> AcceptFulfillmentAsync(
            RawExportFulfillmentAcceptCommand command,
            CancellationToken cancellationToken = default) =>
            inner.AcceptFulfillmentAsync(command, cancellationToken);

        public Task<int> WithdrawFulfillmentAsync(
            RawExportFulfillmentWithdrawCommand command,
            CancellationToken cancellationToken = default) =>
            inner.WithdrawFulfillmentAsync(command, cancellationToken);

        public Task<int> ActivatePolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.ActivatePolicyAsync(command, cancellationToken);

        public Task<int> SuspendPolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.SuspendPolicyAsync(command, cancellationToken);

        public Task<int> RevokePolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokePolicyAsync(command, cancellationToken);

        public async Task<RawExportEligibilitySnapshot>
            ResolveExportEligibilityForAuthorizationAsync(
                Guid principalId,
                Guid policyId,
                int policyVersion,
                CancellationToken cancellationToken = default)
        {
            var snapshot = await inner.ResolveExportEligibilityForAuthorizationAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            reached.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return snapshot;
        }
    }

    private sealed class PausingSubjectConsentRepository(
        IRawExportSubjectConsentRepository inner)
        : IRawExportSubjectConsentRepository
    {
        private readonly TaskCompletionSource reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => reached.Task;

        public void Release() => released.TrySetResult();

        public Task<int> GrantConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantConsentAuthorityAsync(command, cancellationToken);

        public Task<int> RevokeConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeConsentAuthorityAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentGrantedAsync(
            RawExportSubjectConsentGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentGrantedAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentWithdrawnAsync(
            RawExportSubjectConsentWithdrawCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentWithdrawnAsync(command, cancellationToken);

        public async Task<RawExportSubjectConsentSnapshot>
            ResolveSubjectExportConsentForAuthorizationAsync(
                Guid verificationSessionId,
                Guid policyId,
                int policyVersion,
                CancellationToken cancellationToken = default)
        {
            var snapshot = await inner.ResolveSubjectExportConsentForAuthorizationAsync(
                verificationSessionId,
                policyId,
                policyVersion,
                cancellationToken);
            reached.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return snapshot;
        }
    }

}
