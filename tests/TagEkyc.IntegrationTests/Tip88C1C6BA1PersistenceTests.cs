using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1PersistenceTests(PostgresPersistenceFixture postgres, Xunit.Abstractions.ITestOutputHelper output)
{
    private const string PreviousMigration = "20260906055502_Tip88C1C6BIngressSqlComposition";
    private const string FoundationMigration = "20260908120000_Tip88C1C6BA1Foundation";

    [Fact]
    public async Task R24_BindingDerivedSession_HasNoCallerSessionInputOrDirectBindingRead()
    {
        // Homeowner correction: session is verified helper output, never caller input.
        Assert.DoesNotContain(typeof(TagEkyc.Contracts.CaptureRuntime.CaptureRuntimeCaptureArtifactRequest)
            .GetProperties(), p => p.Name == "VerificationSessionId");
        Assert.DoesNotContain(typeof(TagEkyc.Contracts.CaptureRuntime.CaptureRuntimeCaptureArtifactPayload)
            .GetProperties(), p => p.Name == "VerificationSessionId");
        Assert.DoesNotContain(typeof(TagEkyc.Contracts.CaptureRuntime.AuthenticatedCaptureRuntimeContext)
            .GetProperties(), p => p.Name == "VerificationSessionId");
        await using var db = postgres.CreateDbContext();
        var shape = await db.Database.SqlQueryRaw<string>("""
            SELECT pg_catalog.pg_get_function_arguments(p.oid) || ' RETURNS ' ||
                   pg_catalog.pg_get_function_result(p.oid) AS "Value"
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname='capture_runtime_validate_append_authority'
            """).SingleAsync();
        Assert.DoesNotContain("p_verification_session_id", shape, StringComparison.Ordinal);
        Assert.Contains("verification_session_id uuid", shape.Split(" RETURNS ")[1], StringComparison.Ordinal);
        Assert.False(await db.Database.SqlQueryRaw<bool>("""
            SELECT pg_catalog.has_table_privilege('tagekyc_runtime',
              'tagekyc.capture_execution_bindings','SELECT') AS "Value"
            """).SingleAsync());
        output.WriteLine("MEASURED: R24 session is internal helper output; no caller session field; default role binding SELECT denied.");
    }

    [Fact]
    public async Task FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        Assert.Equal(FoundationMigration, (await db.Database.GetAppliedMigrationsAsync()).Last());
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
        var before = await A1CatalogueProof.ReadAsync(db);
        await A1CatalogueProof.WriteMeasuredEvidenceAsync(db, output);
        foreach (var injectFailureAfterDown in new[] { false, true })
        {
        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.Empty(await A1CatalogueProof.ReadAsync(db));
            var roles = await db.Database.SqlQueryRaw<string>($"SELECT rolname::text AS \"Value\" FROM pg_catalog.pg_roles WHERE rolname IN ({A1CatalogueProof.Literals(A1CatalogueProof.Roles)})").ToListAsync();
            Assert.Empty(roles);
            if (injectFailureAfterDown)
                throw new InvalidOperationException("A1_PROOF_INJECTED_FAILURE_AFTER_DOWN");
            await migrator.MigrateAsync(FoundationMigration);
            await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
            await A1CatalogueProof.AssertGrantsAsync(db);
            Assert.Equal(before, await A1CatalogueProof.ReadAsync(db));
        }
        catch (InvalidOperationException error) when (injectFailureAfterDown && error.Message == "A1_PROOF_INJECTED_FAILURE_AFTER_DOWN")
        {
            // Exercise the real shared-fixture failure path, then assert recovery below.
        }
        finally
        {
            // Preserve the original assertion/migration failure while always attempting recovery.
            if (!(await db.Database.GetAppliedMigrationsAsync()).Contains(FoundationMigration))
            {
                try { await migrator.MigrateAsync(FoundationMigration); }
                catch (Exception restoreError) { System.Diagnostics.Trace.TraceError("A1 fixture restore failed: {0}", restoreError); }
            }
        }
        Assert.Equal(FoundationMigration, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Equal(before, await A1CatalogueProof.ReadAsync(db));
        }
    }

    [Fact]
    public async Task Stage1_internal_authority_helpers_have_closed_shapes_and_null_safe_rotation_candidates()
    {
        await using var db = postgres.CreateDbContext();
        var bodies = await db.Database.SqlQueryRaw<string>("""
            SELECT p.proname || ':' || pg_catalog.pg_get_functiondef(p.oid) AS "Value"
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace ns ON ns.oid = p.pronamespace
            WHERE ns.nspname = 'tagekyc'
              AND p.proname IN (
                'capture_runtime_resolve_bootstrap_verifier',
                'capture_runtime_claim_rotation_completion_nonce',
                'capture_runtime_validate_append_authority')
            ORDER BY p.proname
            """).ToListAsync();

        Assert.Equal(3, bodies.Count);
        var bootstrap = Assert.Single(bodies, body => body.StartsWith("capture_runtime_resolve_bootstrap_verifier:", StringComparison.Ordinal));
        Assert.Contains("VerifierPepperVersion", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("SecretDigest", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("LifecycleState", bootstrap, StringComparison.Ordinal);

        var rotation = Assert.Single(bodies, body => body.StartsWith("capture_runtime_claim_rotation_completion_nonce:", StringComparison.Ordinal));
        Assert.Contains("p_request_fingerprint_as_predecessor IS NOT NULL", rotation, StringComparison.Ordinal);
        Assert.Contains("p_request_fingerprint_as_successor IS NOT NULL", rotation, StringComparison.Ordinal);
        Assert.Contains("IS NOT DISTINCT FROM p_request_fingerprint_as_successor", rotation, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO tagekyc.capture_runtime_request_nonces", rotation, StringComparison.Ordinal);

        var append = Assert.Single(bodies, body => body.StartsWith("capture_runtime_validate_append_authority:", StringComparison.Ordinal));
        Assert.Contains("p_required_role NOT IN ('CaptureObservation','TrustedEvidence')", append, StringComparison.Ordinal);
        Assert.Contains("ExecutionExpiresAtUtc", append, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", append, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Foundation_catalogue_contains_the_lineage_event_and_expiry_guards()
    {
        await using var db = postgres.CreateDbContext();
        var constraints = await db.Database.SqlQueryRaw<string>($"""
            SELECT con.conname AS "Value"
            FROM pg_catalog.pg_constraint con
            JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid
            JOIN pg_catalog.pg_namespace ns ON ns.oid = rel.relnamespace
            WHERE ns.nspname = 'tagekyc'
              AND rel.relname IN ({A1CatalogueProof.Literals(A1CatalogueProof.Tables)})
            ORDER BY con.conname
            """).ToListAsync();

        Assert.Contains("FK_bootstrap_result_installation_agent", constraints);
        Assert.Contains("FK_redemption_installation_agent", constraints);
        Assert.Contains("FK_rotation_completion_authorization_lineage", constraints);
        Assert.Contains("FK_rotation_completion_predecessor", constraints);
        Assert.Contains("FK_rotation_completion_successor", constraints);
        Assert.Contains("CK_bootstrap_redemption_event_type", constraints);
        Assert.Contains("CK_rotation_completion_event_type", constraints);
        Assert.Contains("CK_capability_event_type", constraints);

        var eventDomains = await db.Database.SqlQueryRaw<string>("""
            SELECT rel.relname || ':' || con.conname || ':' || pg_catalog.pg_get_constraintdef(con.oid) AS "Value"
            FROM pg_catalog.pg_constraint con
            JOIN pg_catalog.pg_class rel ON rel.oid = con.conrelid
            JOIN pg_catalog.pg_namespace ns ON ns.oid = rel.relnamespace
            WHERE ns.nspname = 'tagekyc'
              AND con.conname IN (
                'CK_bootstrap_redemption_event_type',
                'CK_rotation_completion_event_type',
                'CK_capability_event_type')
            ORDER BY rel.relname
            """).ToListAsync();

        Assert.Equal(3, eventDomains.Count);
        Assert.All(eventDomains, definition =>
        {
            Assert.DoesNotContain("Rejected", definition, StringComparison.Ordinal);
            Assert.DoesNotContain("Replayed", definition, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task R01R02Replay_UsesDurableOperationFingerprintAndFrozenResult()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Positional SQL INSERT does not contain the column token "ResultCode".
        // Execute the transitions and inspect durable outcomes instead of scanning that token.
        await db.Database.ExecuteSqlRawAsync("""
            DO $proof$
            DECLARE
              provision_id uuid:=pg_catalog.gen_random_uuid();
              revoke_id uuid:=pg_catalog.gen_random_uuid();
              principal uuid:=pg_catalog.gen_random_uuid();
              prefix text:=left(replace(pg_catalog.gen_random_uuid()::text,'-',''),12);
              first_result record; replay_result record; conflict_result record;
              revoke_result record; revoke_replay record;
              now_utc timestamptz:='2030-01-01T00:00:00Z';
              fingerprint bytea:=decode(repeat('11',32),'hex');
              revoke_fingerprint bytea:=decode(repeat('22',32),'hex');
              changed_fingerprint bytea:=decode(repeat('33',32),'hex');
            BEGIN
              SELECT * INTO first_result FROM tagekyc.c6ba_root_provision_platform_credential(
                provision_id,principal,now_utc+interval '1 day',prefix,
                decode(repeat('44',32),'hex'),1,fingerprint,now_utc);
              IF first_result.result_code IS DISTINCT FROM 'Created'
                 OR first_result.secret_available IS DISTINCT FROM true
                 OR first_result.credential_id IS NULL OR first_result.revision IS DISTINCT FROM 1::bigint
              THEN RAISE EXCEPTION 'R01_POSITIVE_CONTROL_FAILED'; END IF;

              SELECT * INTO replay_result FROM tagekyc.c6ba_root_provision_platform_credential(
                provision_id,principal,now_utc+interval '1 day',prefix,
                decode(repeat('55',32),'hex'),1,fingerprint,now_utc+interval '1 minute');
              IF replay_result.result_code IS DISTINCT FROM 'ExistingMatchSecretUnavailable'
                 OR replay_result.secret_available IS DISTINCT FROM false
                 OR (to_jsonb(replay_result)-'result_code'-'secret_available') IS DISTINCT FROM
                    (to_jsonb(first_result)-'result_code'-'secret_available')
              THEN RAISE EXCEPTION 'R01_FROZEN_REPLAY_FAILED'; END IF;
              SELECT * INTO conflict_result FROM tagekyc.c6ba_root_provision_platform_credential(
                provision_id,principal,now_utc+interval '1 day',prefix,
                decode(repeat('44',32),'hex'),1,changed_fingerprint,now_utc);
              IF conflict_result.result_code IS DISTINCT FROM 'Conflict'
                 OR conflict_result.credential_id IS NOT NULL
                 OR conflict_result.secret_available IS DISTINCT FROM false
              THEN RAISE EXCEPTION 'R01_FINGERPRINT_CONFLICT_FAILED'; END IF;
              IF (SELECT count(*) FROM tagekyc.platform_operator_credentials WHERE "PrincipalId"=principal)<>1
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_credentials
                   WHERE "CredentialId"=first_result.credential_id AND "SecretDigest"=decode(repeat('44',32),'hex'))
              THEN RAISE EXCEPTION 'R01_REPLAY_MUTATED_CREDENTIAL'; END IF;

              SELECT * INTO revoke_result FROM tagekyc.c6ba_root_revoke_platform_credential(
                revoke_id,first_result.credential_id,1,'DeploymentRevocation',revoke_fingerprint,now_utc+interval '2 minutes');
              IF revoke_result.result_code IS DISTINCT FROM 'Revoked'
                 OR revoke_result.state IS DISTINCT FROM 'Revoked'
                 OR revoke_result.revision IS DISTINCT FROM 2::bigint
              THEN RAISE EXCEPTION 'R02_POSITIVE_CONTROL_FAILED'; END IF;
              SELECT * INTO revoke_replay FROM tagekyc.c6ba_root_revoke_platform_credential(
                revoke_id,first_result.credential_id,1,'DeploymentRevocation',revoke_fingerprint,now_utc+interval '3 minutes');
              IF to_jsonb(revoke_replay) IS DISTINCT FROM to_jsonb(revoke_result)
              THEN RAISE EXCEPTION 'R02_FROZEN_REPLAY_FAILED'; END IF;
              SELECT * INTO conflict_result FROM tagekyc.c6ba_root_revoke_platform_credential(
                revoke_id,first_result.credential_id,1,'DeploymentRevocation',changed_fingerprint,now_utc+interval '4 minutes');
              IF conflict_result.result_code IS DISTINCT FROM 'Conflict' OR conflict_result.credential_id IS NOT NULL
              THEN RAISE EXCEPTION 'R02_FINGERPRINT_CONFLICT_FAILED'; END IF;
              IF (SELECT count(*) FROM tagekyc.platform_operator_root_operations WHERE "OperationId" IN (provision_id,revoke_id))<>2
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_root_operations
                   WHERE "OperationId"=provision_id AND "OperationKind"='Provision' AND "ResultCode"='Applied'
                     AND "RequestFingerprint"=fingerprint AND "TargetCredentialId"=first_result.credential_id AND "ResultRevision"=1)
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_root_operations
                   WHERE "OperationId"=revoke_id AND "OperationKind"='Revoke' AND "ResultCode"='Applied'
                     AND "RequestFingerprint"=revoke_fingerprint AND "TargetCredentialId"=first_result.credential_id AND "ResultRevision"=2)
              THEN RAISE EXCEPTION 'ROOT_DURABLE_OUTCOME_FAILED'; END IF;
              IF (SELECT count(*) FROM tagekyc.platform_operator_root_events WHERE "OperationId"=provision_id)<>1
                 OR (SELECT count(*) FROM tagekyc.platform_operator_root_events WHERE "OperationId"=revoke_id)<>1
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_root_events WHERE "OperationId"=provision_id AND "EventType"='Provisioned')
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_root_events WHERE "OperationId"=revoke_id AND "EventType"='Revoked')
              THEN RAISE EXCEPTION 'ROOT_EVENT_CARDINALITY_FAILED'; END IF;
            END $proof$;
            SET CONSTRAINTS ALL IMMEDIATE;
            """);
        await transaction.RollbackAsync();
    }

}

internal static class A1CatalogueProof
{
    internal const string Owner = "tagekyc_raw_export_deployer";
    internal static readonly string[] Tables =
    [
        "platform_operator_credentials",
        "capture_runtime_role_policy_revisions",
        "capture_runtime_role_policy_heads",
        "capture_runtime_trust_profile_revisions",
        "capture_runtime_trust_profile_heads",
        "capture_runtime_configuration_revisions",
        "capture_runtime_configuration_heads",
        "capture_runtime_registrations",
        "capture_runtime_installations",
        "capture_runtime_credential_generations",
        "capture_runtime_request_nonces",
        "capture_runtime_bootstrap_issuances",
        "capture_capabilities",
        "capture_execution_bindings",
        "capture_runtime_configuration_overrides",
        "capture_runtime_rotation_authorizations",
        "capture_runtime_management_operations",
        "capture_runtime_management_events",
        "capture_capability_operations",
        "capture_capability_events",
        "capture_runtime_bootstrap_redemption_operations",
        "capture_runtime_rotation_completion_operations",
        "capture_runtime_bootstrap_redemption_events",
        "capture_runtime_rotation_completion_events",
        "platform_operator_root_operations",
        "platform_operator_root_events",
        "capture_runtime_cutover_state",
    ];
    internal static readonly string[] Functions =
    [
        "tagekyc.c6ba_roles_are_canonical(text[])",
        "tagekyc.c6ba_reject_row_mutation()",
        "tagekyc.c6ba_validate_current_generation()",
        "tagekyc.c6ba_validate_capability_graph()",
        "tagekyc.c6ba_validate_configuration_override()",
        "tagekyc.c6ba_require_management_event()",
        "tagekyc.c6ba_require_capability_event()",
        "tagekyc.c6ba_require_redemption_event()",
        "tagekyc.c6ba_require_rotation_completion_event()",
        "tagekyc.c6ba_require_root_event()",
        "tagekyc.c6ba_root_provision_platform_credential(uuid, uuid, timestamp with time zone, text, bytea, integer, bytea, timestamp with time zone)",
        "tagekyc.c6ba_root_revoke_platform_credential(uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_issue_bootstrap(uuid, uuid, text, uuid, bigint, uuid, bigint, uuid, bigint, timestamp with time zone, bytea, text, bytea, integer, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_revoke_bootstrap(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_redeem_bootstrap(uuid, uuid, bytea, uuid, bytea, bytea, timestamp with time zone, bytea, bytea, bytea, timestamp with time zone)",
        "tagekyc.c6ba_transition_runtime_lifecycle(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, text, text, text, boolean)",
        "tagekyc.capture_runtime_suspend(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_reactivate(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_revoke(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_retire(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_authorize_rotation(uuid, uuid, uuid, uuid, uuid, bigint, bigint, timestamp with time zone, bytea, timestamp with time zone)",
        "tagekyc.c6ba_assert_current_operator(uuid, timestamp with time zone)",
        "tagekyc.capture_runtime_revoke_rotation(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_complete_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_replay_completed_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_publish_trust_profile(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, text, boolean, boolean, boolean, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_publish_role_policy(uuid, uuid, uuid, bigint, timestamp with time zone, text[], bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_assign_role_policy(uuid, uuid, uuid, uuid, bigint, bigint, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_assign_configuration(uuid, uuid, uuid, uuid, bigint, bigint, uuid, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_publish_configuration(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, boolean, integer, integer, integer, integer, integer, bigint, integer, bigint, integer, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_issue_or_replace_capability(uuid, uuid, text, uuid, bigint, uuid, uuid, text, bytea, integer, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_resolve_capability_verifier(uuid)",
        "tagekyc.capture_runtime_bind_capability(uuid, uuid, uuid, bigint, uuid, boolean, uuid, bytea, timestamp with time zone)",
        "tagekyc.capture_runtime_reconcile_binding(uuid, uuid, uuid, bigint, uuid, uuid, timestamp with time zone)",
        "tagekyc.capture_runtime_materialize_capability_expiry(uuid, uuid, timestamp with time zone, uuid)",
        "tagekyc.capture_runtime_resolve_configuration(uuid, uuid, uuid, bigint, timestamp with time zone)",
        "tagekyc.capture_runtime_read_readiness(uuid, timestamp with time zone)",
        "tagekyc.capture_runtime_cancel_session_with_capability(uuid, uuid, text, text, text, timestamp with time zone, text, uuid)",
        "tagekyc.capture_runtime_validate_append_authority(uuid, uuid, uuid, bigint, uuid, text, timestamp with time zone)",
        "tagekyc.capture_runtime_read_cutover_state(text, timestamp with time zone)",
        "tagekyc.platform_operator_authenticate(text, timestamp with time zone)",
        "tagekyc.capture_runtime_resolve_verifier(uuid, bigint, timestamp with time zone)",
        "tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid)",
        "tagekyc.capture_runtime_claim_nonce(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, timestamp with time zone)",
        "tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone, timestamp with time zone)",
        "tagekyc.capture_runtime_cleanup_nonces(timestamp with time zone, integer)",
        "tagekyc.capture_runtime_revoke_credential(uuid, uuid, uuid, uuid, uuid, bigint, bigint, text, bytea, timestamp with time zone)",
    ];
    internal static readonly string[] Grants =
    [
        "tagekyc.c6ba_roles_are_canonical(text[])|",
        "tagekyc.c6ba_reject_row_mutation()|",
        "tagekyc.c6ba_validate_current_generation()|",
        "tagekyc.c6ba_validate_capability_graph()|",
        "tagekyc.c6ba_validate_configuration_override()|",
        "tagekyc.c6ba_require_management_event()|",
        "tagekyc.c6ba_require_capability_event()|",
        "tagekyc.c6ba_require_redemption_event()|",
        "tagekyc.c6ba_require_rotation_completion_event()|",
        "tagekyc.c6ba_require_root_event()|",
        "tagekyc.c6ba_root_provision_platform_credential(uuid, uuid, timestamp with time zone, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.c6ba_root_revoke_platform_credential(uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_issue_bootstrap(uuid, uuid, text, uuid, bigint, uuid, bigint, uuid, bigint, timestamp with time zone, bytea, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_revoke_bootstrap(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_redeem_bootstrap(uuid, uuid, bytea, uuid, bytea, bytea, timestamp with time zone, bytea, bytea, bytea, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.c6ba_transition_runtime_lifecycle(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, text, text, text, boolean)|",
        "tagekyc.capture_runtime_suspend(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_reactivate(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_revoke(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_retire(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_authorize_rotation(uuid, uuid, uuid, uuid, uuid, bigint, bigint, timestamp with time zone, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.c6ba_assert_current_operator(uuid, timestamp with time zone)|",
        "tagekyc.capture_runtime_revoke_rotation(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_complete_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_replay_completed_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_publish_trust_profile(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, text, boolean, boolean, boolean, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_publish_role_policy(uuid, uuid, uuid, bigint, timestamp with time zone, text[], bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_assign_role_policy(uuid, uuid, uuid, uuid, bigint, bigint, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_assign_configuration(uuid, uuid, uuid, uuid, bigint, bigint, uuid, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_publish_configuration(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, boolean, integer, integer, integer, integer, integer, bigint, integer, bigint, integer, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_issue_or_replace_capability(uuid, uuid, text, uuid, bigint, uuid, uuid, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_capture_runtime_application,tagekyc_runtime",
        "tagekyc.capture_runtime_resolve_capability_verifier(uuid)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_bind_capability(uuid, uuid, uuid, bigint, uuid, boolean, uuid, bytea, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_reconcile_binding(uuid, uuid, uuid, bigint, uuid, uuid, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_materialize_capability_expiry(uuid, uuid, timestamp with time zone, uuid)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_resolve_configuration(uuid, uuid, uuid, bigint, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_read_readiness(uuid, timestamp with time zone)|tagekyc_capture_runtime_operator",
        "tagekyc.capture_runtime_cancel_session_with_capability(uuid, uuid, text, text, text, timestamp with time zone, text, uuid)|tagekyc_capture_runtime_application,tagekyc_runtime",
        "tagekyc.capture_runtime_validate_append_authority(uuid, uuid, uuid, bigint, uuid, text, timestamp with time zone)|tagekyc_runtime",
        "tagekyc.capture_runtime_read_cutover_state(text, timestamp with time zone)|tagekyc_capture_runtime_application",
        "tagekyc.platform_operator_authenticate(text, timestamp with time zone)|tagekyc_capture_runtime_authenticator",
        "tagekyc.capture_runtime_resolve_verifier(uuid, bigint, timestamp with time zone)|tagekyc_capture_runtime_authenticator",
        "tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid)|tagekyc_capture_runtime_application",
        "tagekyc.capture_runtime_claim_nonce(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, timestamp with time zone)|tagekyc_capture_runtime_authenticator",
        "tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone, timestamp with time zone)|tagekyc_capture_runtime_authenticator",
        "tagekyc.capture_runtime_cleanup_nonces(timestamp with time zone, integer)|tagekyc_capture_runtime_authenticator",
        "tagekyc.capture_runtime_revoke_credential(uuid, uuid, uuid, uuid, uuid, bigint, bigint, text, bytea, timestamp with time zone)|tagekyc_capture_runtime_operator",
    ];
    internal static readonly string[] Roles =
    [
        "tagekyc_capture_runtime_operator",
        "tagekyc_capture_runtime_authenticator",
        "tagekyc_capture_runtime_application"
    ];
    internal static string Literals(IEnumerable<string> values) =>
        string.Join(",", values.Select(value => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'"));
    internal static string TablePredicate => $"n.nspname='tagekyc' AND c.relname IN ({Literals(Tables)})";
    // Select the whole identity family, including wrong-signature/extra overloads.
    // Comparing the complete canonical key below, not filtering to expected signatures,
    // makes an extra overload visible.
    internal static string FunctionCte => $"""
        WITH functions AS (
          SELECT p.*, n.nspname,
            pg_catalog.format('%I.%I(%s)',n.nspname,p.proname,pg_catalog.oidvectortypes(p.proargtypes)) AS identity
          FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
        ), a1 AS (
          SELECT * FROM functions
          WHERE split_part(identity,'(',1) IN ({Literals(Functions.Select(value => value.Split('(')[0]).Distinct())})
        )
        """;
    internal static void Exact(IEnumerable<string> expected, IEnumerable<string> actual) =>
        Assert.Equal(expected.Order(StringComparer.Ordinal).ToArray(), actual.Order(StringComparer.Ordinal).ToArray());

    internal static async Task AssertOwnersAndRolesAsync(DbContext db)
    {
        Assert.Equal(27, Tables.Length);
        Assert.Equal(47, Functions.Length);
        Assert.Equal(47, Functions.Distinct().Count());
        var executor = await db.Database.SqlQueryRaw<string>("SELECT current_user::text AS \"Value\"").SingleAsync();
        Assert.NotEqual(Owner, executor);
        var tables = await db.Database.SqlQueryRaw<string>($"""
            SELECT c.relname || '|' || pg_catalog.pg_get_userbyid(c.relowner) AS "Value"
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {TablePredicate} AND c.relkind='r'
            """).ToListAsync();
        Exact(Tables.Select(table => table + "|" + Owner), tables);
        var functions = await db.Database.SqlQueryRaw<string>(FunctionCte + """
            SELECT identity || '|' || pg_catalog.pg_get_userbyid(proowner) AS "Value" FROM a1
            """).ToListAsync();
        Exact(Functions.Select(function => function + "|" + Owner), functions);
        var badRoles = await db.Database.SqlQueryRaw<string>($"""
            SELECT rolname::text AS "Value" FROM pg_catalog.pg_authid r
            WHERE rolname IN ({Literals(Roles.Append(Owner))}) AND (
              rolcanlogin OR rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication
              OR rolbypassrls OR NOT rolinherit OR rolpassword IS NOT NULL
              OR EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members m WHERE m.member=r.oid
                OR (m.roleid=r.oid AND r.rolname<>'tagekyc_raw_export_deployer')))
            """).ToListAsync();
        Assert.Empty(badRoles);
        var roles = await db.Database.SqlQueryRaw<string>($"""
            SELECT rolname::text AS "Value" FROM pg_catalog.pg_roles WHERE rolname IN ({Literals(Roles)})
            """).ToListAsync();
        Exact(Roles, roles);
    }

    internal static async Task AssertGrantsAsync(DbContext db)
    {
        var actual = await db.Database.SqlQueryRaw<string>(FunctionCte + """
            SELECT identity || '|' || COALESCE((
              SELECT string_agg(CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END,',' ORDER BY CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END)
              FROM pg_catalog.aclexplode(COALESCE(proacl,pg_catalog.acldefault('f',proowner))) a
              WHERE a.privilege_type='EXECUTE' AND a.grantee<>proowner
            ),'') AS "Value" FROM a1
            """).ToListAsync();
        Assert.Equal(47, actual.Count);
        Assert.Equal(12, Grants.Count(value => value.EndsWith('|')));
        Exact(Grants, actual);
    }

    internal static async Task<List<string>> ReadAsync(DbContext db)
    {
        var result = await db.Database.SqlQueryRaw<string>($"""
            SELECT 'table:' || c.relname || ':' || pg_catalog.pg_get_userbyid(c.relowner) || ':' || COALESCE(c.relacl::text,'') AS "Value"
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {TablePredicate} AND c.relkind='r'
            UNION ALL
            SELECT 'column:' || c.relname || ':' || a.attnum || ':' || a.attname || ':' ||
              pg_catalog.format_type(a.atttypid,a.atttypmod) || ':' || a.attnotnull || ':' ||
              COALESCE(pg_catalog.pg_get_expr(d.adbin,d.adrelid),'')
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
            WHERE {TablePredicate}
            UNION ALL
            SELECT 'constraint:' || c.relname || ':' || x.conname || ':' || pg_catalog.pg_get_constraintdef(x.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_constraint x ON x.conrelid=c.oid WHERE {TablePredicate}
            UNION ALL
            SELECT 'index:' || c.relname || ':' || pg_catalog.pg_get_indexdef(i.indexrelid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_index i ON i.indrelid=c.oid WHERE {TablePredicate}
            UNION ALL
            SELECT 'trigger:' || c.relname || ':' || pg_catalog.pg_get_triggerdef(t.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_trigger t ON t.tgrelid=c.oid AND NOT t.tgisinternal WHERE {TablePredicate}
            """).ToListAsync();
        result.AddRange(await db.Database.SqlQueryRaw<string>(FunctionCte + """
            SELECT 'function:' || identity || ':' ||
              pg_catalog.pg_get_userbyid(proowner) || ':' || COALESCE(proacl::text,'') || ':' ||
              pg_catalog.pg_get_functiondef(oid) AS "Value" FROM a1
            """).ToListAsync());
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal static async Task WriteMeasuredEvidenceAsync(DbContext db, Xunit.Abstractions.ITestOutputHelper output)
    {
        var rows = await db.Database.SqlQueryRaw<string>($"""
            SELECT c.relname || '|' || pg_catalog.pg_get_userbyid(c.relowner) AS "Value"
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {TablePredicate} AND c.relkind='r' ORDER BY c.relname
            """).ToListAsync();
        output.WriteLine("TABLE OWNERS {0}/27", rows.Count);
        foreach (var row in rows) output.WriteLine(row);
        var functions = await db.Database.SqlQueryRaw<string>(FunctionCte + """
            SELECT identity || '|' || pg_catalog.pg_get_userbyid(proowner) || '|EXECUTE=[' || COALESCE((
              SELECT string_agg(CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END,',' ORDER BY CASE WHEN a.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(a.grantee) END)
              FROM pg_catalog.aclexplode(COALESCE(proacl,pg_catalog.acldefault('f',proowner))) a
              WHERE a.privilege_type='EXECUTE' AND a.grantee<>proowner
            ),'') || ']' AS "Value" FROM a1 ORDER BY identity
            """).ToListAsync();
        output.WriteLine("FUNCTION OWNERS + EXECUTE {0}/47", functions.Count);
        foreach (var row in functions) output.WriteLine(row);
        // Supplemental signature rendering is evidence only, not the canonical key.
        var declaredSignatures = await db.Database.SqlQueryRaw<string>(FunctionCte + """
            SELECT identity || '|declared=' || pg_catalog.pg_get_function_identity_arguments(oid) AS "Value"
            FROM a1 ORDER BY identity
            """).ToListAsync();
        foreach (var row in declaredSignatures) output.WriteLine(row);
    }
}
