using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2AuthoritySnapshotLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_authority_snapshots",
                schema: "tagekyc",
                columns: table => new
                {
                    AuthoritySnapshotEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    TargetRevision = table.Column<long>(type: "bigint", nullable: true),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CapturedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    WithdrawnByPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureAcceptanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthoritySnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: true),
                    AuthoritySnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorityArtifactId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorityArtifactVersion = table.Column<int>(type: "integer", nullable: true),
                    ControllerIdentity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ApprovedPurpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StableDataScopeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RetentionPolicyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RetentionPolicyVersion = table.Column<int>(type: "integer", nullable: true),
                    RetentionClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RetentionStartEvent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AbsoluteSourceExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReuseDisposition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExtensionDisposition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RevocationPolicyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PurgePolicyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LegalHoldPolicyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_authority_snapshots", x => x.AuthoritySnapshotEventId);
                    table.UniqueConstraint("uq_raw_export_authority_snapshot_scope_revision", x => new { x.ClientApplicationId, x.VerificationSessionId, x.CaptureAcceptanceId, x.RawClass, x.Revision });
                    table.CheckConstraint("ck_raw_export_authority_snapshot_event_shape", "(\n    \"EventType\" = 'Granted'\n    AND \"TargetRevision\" IS NULL\n    AND \"ValidFromUtc\" IS NOT NULL\n    AND (\"ValidUntilUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\")\n    AND \"AuthoritySnapshotSchemaVersion\" IS NOT NULL\n    AND \"AuthoritySnapshotId\" IS NOT NULL\n    AND \"AuthorityArtifactId\" IS NOT NULL\n    AND \"AuthorityArtifactVersion\" IS NOT NULL\n    AND \"ControllerIdentity\" IS NOT NULL\n    AND \"ApprovedPurpose\" IS NOT NULL\n    AND \"StableDataScopeId\" IS NOT NULL\n    AND \"RetentionPolicyId\" IS NOT NULL\n    AND \"RetentionPolicyVersion\" IS NOT NULL\n    AND \"RetentionClass\" IS NOT NULL\n    AND \"RetentionStartEvent\" IS NOT NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NOT NULL\n    AND \"ReuseDisposition\" IS NOT NULL\n    AND \"ExtensionDisposition\" IS NOT NULL\n    AND \"RevocationPolicyId\" IS NOT NULL\n    AND \"PurgePolicyId\" IS NOT NULL\n    AND \"LegalHoldPolicyId\" IS NOT NULL\n    AND \"EvaluatedAtUtc\" IS NOT NULL\n    AND \"CapturedByPrincipalId\" IS NOT NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Withdrawn'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NOT NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Revoked'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NOT NULL\n)");
                    table.CheckConstraint("ck_raw_export_authority_snapshot_event_type", "\"EventType\" IN ('Granted','Withdrawn','Revoked')");
                    table.CheckConstraint("ck_raw_export_authority_snapshot_values", "\"Revision\" >= 1\nAND (\"TargetRevision\" IS NULL OR \"TargetRevision\" >= 1)\nAND \"ClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"VerificationSessionId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"CaptureAcceptanceId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND btrim(\"RawClass\") <> ''\nAND (\"AuthoritySnapshotSchemaVersion\" IS NULL OR \"AuthoritySnapshotSchemaVersion\" = 1)\nAND (\"AuthorityArtifactVersion\" IS NULL OR \"AuthorityArtifactVersion\" >= 1)\nAND (\"RetentionPolicyVersion\" IS NULL OR \"RetentionPolicyVersion\" >= 1)\nAND (\"AuthoritySnapshotId\" IS NULL OR \"AuthoritySnapshotId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"AuthorityArtifactId\" IS NULL OR \"AuthorityArtifactId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"ControllerIdentity\" IS NULL OR btrim(\"ControllerIdentity\") <> '')\nAND (\"ApprovedPurpose\" IS NULL OR \"ApprovedPurpose\" = 'SubjectRawBiometricExport')\nAND (\"StableDataScopeId\" IS NULL OR btrim(\"StableDataScopeId\") <> '')\nAND (\"RetentionPolicyId\" IS NULL OR btrim(\"RetentionPolicyId\") <> '')\nAND (\"RetentionClass\" IS NULL OR btrim(\"RetentionClass\") <> '')\nAND (\"RetentionStartEvent\" IS NULL OR btrim(\"RetentionStartEvent\") <> '')\nAND (\"ReuseDisposition\" IS NULL OR \"ReuseDisposition\" = 'FreshAuthorityRequired')\nAND (\"ExtensionDisposition\" IS NULL OR \"ExtensionDisposition\" = 'Forbidden')\nAND (\"RevocationPolicyId\" IS NULL OR btrim(\"RevocationPolicyId\") <> '')\nAND (\"PurgePolicyId\" IS NULL OR btrim(\"PurgePolicyId\") <> '')\nAND (\"LegalHoldPolicyId\" IS NULL OR btrim(\"LegalHoldPolicyId\") <> '')\nAND (\n    \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    OR \"EvaluatedAtUtc\" IS NULL\n    OR \"AbsoluteSourceExpiresAtUtc\" > \"EvaluatedAtUtc\"\n)");
                    table.ForeignKey(
                        name: "fk_raw_export_authority_snapshot_acceptance",
                        column: x => x.CaptureAcceptanceId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_capture_acceptance_events",
                        principalColumn: "CaptureAcceptanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_authority_snapshot_session",
                        column: x => x.VerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_authority_snapshots_CaptureAcceptanceId",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                column: "CaptureAcceptanceId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_authority_snapshots_VerificationSessionId",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                column: "VerificationSessionId");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON tagekyc.raw_export_authority_snapshots
                FROM PUBLIC, tagekyc_runtime;

                GRANT SELECT, INSERT ON tagekyc.raw_export_authority_snapshots
                TO tagekyc_raw_export_deployer;

                CREATE OR REPLACE FUNCTION
                    tagekyc.enforce_raw_export_authority_snapshot_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    expected_context text;
                    append_context text;
                    actor_id uuid;
                    acceptance_client_application_id uuid;
                    acceptance_verification_session_id uuid;
                    acceptance_raw_class text;
                    current_revision bigint;
                    current_event_type text;
                    current_is_effective boolean;
                BEGIN
                    expected_context := CASE NEW."EventType"
                        WHEN 'Granted' THEN 'grant'
                        WHEN 'Withdrawn' THEN 'withdraw'
                        WHEN 'Revoked' THEN 'revoke'
                        ELSE NULL
                    END;
                    append_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_authority_snapshot_append_context',
                        true);
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR append_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_DIRECT_INSERT_UNSUPPORTED';
                    END IF;

                    actor_id := tagekyc.raw_export_current_actor();
                    SELECT
                        acceptance."ClientApplicationId",
                        acceptance."VerificationSessionId",
                        acceptance."RawClass"
                    INTO
                        acceptance_client_application_id,
                        acceptance_verification_session_id,
                        acceptance_raw_class
                    FROM tagekyc.raw_export_capture_acceptance_events AS acceptance
                    WHERE acceptance."CaptureAcceptanceId" =
                        NEW."CaptureAcceptanceId";
                    IF NOT FOUND
                       OR acceptance_client_application_id IS DISTINCT FROM
                            NEW."ClientApplicationId"
                       OR acceptance_verification_session_id IS DISTINCT FROM
                            NEW."VerificationSessionId"
                       OR acceptance_raw_class IS DISTINCT FROM NEW."RawClass" THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtext(
                            'tip88c1:b2-authority:' ||
                            NEW."ClientApplicationId"::text || ':' ||
                            NEW."VerificationSessionId"::text || ':' ||
                            NEW."CaptureAcceptanceId"::text || ':' ||
                            NEW."RawClass"));

                    SELECT
                        snapshot."Revision",
                        snapshot."EventType",
                        (
                            snapshot."EventType" = 'Granted'
                            AND snapshot."ValidFromUtc" <=
                                pg_catalog.transaction_timestamp()
                            AND (
                                snapshot."ValidUntilUtc" IS NULL
                                OR pg_catalog.transaction_timestamp() <
                                    snapshot."ValidUntilUtc")
                        )
                    INTO
                        current_revision,
                        current_event_type,
                        current_is_effective
                    FROM tagekyc.raw_export_authority_snapshots AS snapshot
                    WHERE snapshot."ClientApplicationId" =
                            NEW."ClientApplicationId"
                      AND snapshot."VerificationSessionId" =
                            NEW."VerificationSessionId"
                      AND snapshot."CaptureAcceptanceId" =
                            NEW."CaptureAcceptanceId"
                      AND snapshot."RawClass" = NEW."RawClass"
                    ORDER BY snapshot."Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);

                    IF NEW."Revision" <> current_revision + 1 THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_REVISION_CONFLICT';
                    END IF;

                    IF NEW."EventType" = 'Granted' THEN
                        IF NEW."CapturedByPrincipalId" IS DISTINCT FROM actor_id
                           OR NEW."TargetRevision" IS NOT NULL THEN
                            RAISE EXCEPTION
                                'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
                        END IF;
                    ELSIF NEW."EventType" = 'Withdrawn' THEN
                        IF NEW."WithdrawnByPrincipalId" IS DISTINCT FROM actor_id
                           OR current_event_type IS DISTINCT FROM 'Granted'
                           OR current_is_effective IS DISTINCT FROM true
                           OR NEW."TargetRevision" IS DISTINCT FROM
                                current_revision THEN
                            RAISE EXCEPTION
                                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
                        END IF;
                    ELSIF NEW."EventType" = 'Revoked' THEN
                        IF NEW."RevokedByPrincipalId" IS DISTINCT FROM actor_id
                           OR current_event_type IS DISTINCT FROM 'Granted'
                           OR current_is_effective IS DISTINCT FROM true
                           OR NEW."TargetRevision" IS DISTINCT FROM
                                current_revision THEN
                            RAISE EXCEPTION
                                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
                        END IF;
                    ELSE
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
                    END IF;

                    NEW."RecordedAtUtc" :=
                        pg_catalog.transaction_timestamp();
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.raw_export_append_authority_snapshot(
                        p_client_application_id uuid,
                        p_verification_session_id uuid,
                        p_capture_acceptance_id uuid,
                        p_raw_class text,
                        p_authority_artifact_id uuid,
                        p_authority_artifact_version integer,
                        p_controller_identity text,
                        p_stable_data_scope_id text,
                        p_retention_policy_id text,
                        p_retention_policy_version integer,
                        p_retention_class text,
                        p_retention_start_event text,
                        p_absolute_source_expires_at_utc timestamptz,
                        p_revocation_policy_id text,
                        p_purge_policy_id text,
                        p_legal_hold_policy_id text,
                        p_evaluated_at_utc timestamptz,
                        p_valid_until_utc timestamptz)
                RETURNS TABLE(
                    "Revision" bigint,
                    "AuthoritySnapshotId" uuid)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    next_revision bigint;
                    snapshot_id uuid;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF p_client_application_id IS NULL
                       OR p_client_application_id =
                            '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_verification_session_id IS NULL
                       OR p_verification_session_id =
                            '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_capture_acceptance_id IS NULL
                       OR p_capture_acceptance_id =
                            '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_authority_artifact_id IS NULL
                       OR p_authority_artifact_id =
                            '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_authority_artifact_version < 1
                       OR p_retention_policy_version < 1
                       OR p_raw_class IS NULL
                       OR pg_catalog.btrim(p_raw_class) = ''
                       OR p_controller_identity IS NULL
                       OR pg_catalog.btrim(p_controller_identity) = ''
                       OR p_stable_data_scope_id IS NULL
                       OR pg_catalog.btrim(p_stable_data_scope_id) = ''
                       OR p_retention_policy_id IS NULL
                       OR pg_catalog.btrim(p_retention_policy_id) = ''
                       OR p_retention_class IS NULL
                       OR pg_catalog.btrim(p_retention_class) = ''
                       OR p_retention_start_event IS NULL
                       OR pg_catalog.btrim(p_retention_start_event) = ''
                       OR p_revocation_policy_id IS NULL
                       OR pg_catalog.btrim(p_revocation_policy_id) = ''
                       OR p_purge_policy_id IS NULL
                       OR pg_catalog.btrim(p_purge_policy_id) = ''
                       OR p_legal_hold_policy_id IS NULL
                       OR pg_catalog.btrim(p_legal_hold_policy_id) = ''
                       OR p_absolute_source_expires_at_utc IS NULL
                       OR p_evaluated_at_utc IS NULL THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_VALUE_INVALID';
                    END IF;
                    IF p_absolute_source_expires_at_utc <= p_evaluated_at_utc
                       OR (
                            p_valid_until_utc IS NOT NULL
                            AND p_valid_until_utc <=
                                pg_catalog.transaction_timestamp()) THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_TIME_INVALID';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtext(
                            'tip88c1:b2-authority:' ||
                            p_client_application_id::text || ':' ||
                            p_verification_session_id::text || ':' ||
                            p_capture_acceptance_id::text || ':' ||
                            p_raw_class));
                    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
                    INTO next_revision
                    FROM tagekyc.raw_export_authority_snapshots AS snapshot
                    WHERE snapshot."ClientApplicationId" =
                            p_client_application_id
                      AND snapshot."VerificationSessionId" =
                            p_verification_session_id
                      AND snapshot."CaptureAcceptanceId" =
                            p_capture_acceptance_id
                      AND snapshot."RawClass" = p_raw_class;

                    snapshot_id := pg_catalog.gen_random_uuid();
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_authority_snapshot_append_context',
                        'grant',
                        true);
                    INSERT INTO tagekyc.raw_export_authority_snapshots
                        (
                            "AuthoritySnapshotEventId",
                            "EventType",
                            "Revision",
                            "ValidFromUtc",
                            "ValidUntilUtc",
                            "RecordedAtUtc",
                            "CapturedByPrincipalId",
                            "ClientApplicationId",
                            "VerificationSessionId",
                            "CaptureAcceptanceId",
                            "RawClass",
                            "AuthoritySnapshotSchemaVersion",
                            "AuthoritySnapshotId",
                            "AuthorityArtifactId",
                            "AuthorityArtifactVersion",
                            "ControllerIdentity",
                            "ApprovedPurpose",
                            "StableDataScopeId",
                            "RetentionPolicyId",
                            "RetentionPolicyVersion",
                            "RetentionClass",
                            "RetentionStartEvent",
                            "AbsoluteSourceExpiresAtUtc",
                            "ReuseDisposition",
                            "ExtensionDisposition",
                            "RevocationPolicyId",
                            "PurgePolicyId",
                            "LegalHoldPolicyId",
                            "EvaluatedAtUtc")
                    VALUES
                        (
                            pg_catalog.gen_random_uuid(),
                            'Granted',
                            next_revision,
                            pg_catalog.transaction_timestamp(),
                            p_valid_until_utc,
                            pg_catalog.transaction_timestamp(),
                            actor_id,
                            p_client_application_id,
                            p_verification_session_id,
                            p_capture_acceptance_id,
                            p_raw_class,
                            1,
                            snapshot_id,
                            p_authority_artifact_id,
                            p_authority_artifact_version,
                            p_controller_identity,
                            'SubjectRawBiometricExport',
                            p_stable_data_scope_id,
                            p_retention_policy_id,
                            p_retention_policy_version,
                            p_retention_class,
                            p_retention_start_event,
                            p_absolute_source_expires_at_utc,
                            'FreshAuthorityRequired',
                            'Forbidden',
                            p_revocation_policy_id,
                            p_purge_policy_id,
                            p_legal_hold_policy_id,
                            p_evaluated_at_utc);

                    RETURN QUERY SELECT next_revision, snapshot_id;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.raw_export_withdraw_authority_snapshot(
                        p_client_application_id uuid,
                        p_verification_session_id uuid,
                        p_capture_acceptance_id uuid,
                        p_raw_class text,
                        p_target_revision bigint,
                        p_withdrawn_by uuid)
                RETURNS bigint
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    next_revision bigint;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF p_withdrawn_by IS DISTINCT FROM actor_id THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtext(
                            'tip88c1:b2-authority:' ||
                            p_client_application_id::text || ':' ||
                            p_verification_session_id::text || ':' ||
                            p_capture_acceptance_id::text || ':' ||
                            p_raw_class));
                    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
                    INTO next_revision
                    FROM tagekyc.raw_export_authority_snapshots AS snapshot
                    WHERE snapshot."ClientApplicationId" =
                            p_client_application_id
                      AND snapshot."VerificationSessionId" =
                            p_verification_session_id
                      AND snapshot."CaptureAcceptanceId" =
                            p_capture_acceptance_id
                      AND snapshot."RawClass" = p_raw_class;
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_authority_snapshot_append_context',
                        'withdraw',
                        true);
                    INSERT INTO tagekyc.raw_export_authority_snapshots
                        (
                            "AuthoritySnapshotEventId","EventType","Revision",
                            "TargetRevision","RecordedAtUtc",
                            "WithdrawnByPrincipalId","ClientApplicationId",
                            "VerificationSessionId","CaptureAcceptanceId",
                            "RawClass")
                    VALUES
                        (
                            pg_catalog.gen_random_uuid(),'Withdrawn',
                            next_revision,p_target_revision,
                            pg_catalog.transaction_timestamp(),actor_id,
                            p_client_application_id,p_verification_session_id,
                            p_capture_acceptance_id,p_raw_class);
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.raw_export_revoke_authority_snapshot(
                        p_client_application_id uuid,
                        p_verification_session_id uuid,
                        p_capture_acceptance_id uuid,
                        p_raw_class text,
                        p_target_revision bigint,
                        p_revoked_by uuid)
                RETURNS bigint
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    next_revision bigint;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF p_revoked_by IS DISTINCT FROM actor_id THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtext(
                            'tip88c1:b2-authority:' ||
                            p_client_application_id::text || ':' ||
                            p_verification_session_id::text || ':' ||
                            p_capture_acceptance_id::text || ':' ||
                            p_raw_class));
                    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
                    INTO next_revision
                    FROM tagekyc.raw_export_authority_snapshots AS snapshot
                    WHERE snapshot."ClientApplicationId" =
                            p_client_application_id
                      AND snapshot."VerificationSessionId" =
                            p_verification_session_id
                      AND snapshot."CaptureAcceptanceId" =
                            p_capture_acceptance_id
                      AND snapshot."RawClass" = p_raw_class;
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_authority_snapshot_append_context',
                        'revoke',
                        true);
                    INSERT INTO tagekyc.raw_export_authority_snapshots
                        (
                            "AuthoritySnapshotEventId","EventType","Revision",
                            "TargetRevision","RecordedAtUtc",
                            "RevokedByPrincipalId","ClientApplicationId",
                            "VerificationSessionId","CaptureAcceptanceId",
                            "RawClass")
                    VALUES
                        (
                            pg_catalog.gen_random_uuid(),'Revoked',
                            next_revision,p_target_revision,
                            pg_catalog.transaction_timestamp(),actor_id,
                            p_client_application_id,p_verification_session_id,
                            p_capture_acceptance_id,p_raw_class);
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        p_client_application_id uuid,
                        p_verification_session_id uuid,
                        p_capture_acceptance_id uuid,
                        p_raw_class text,
                        p_evaluated_at_utc timestamptz)
                RETURNS TABLE(
                    "Revision" bigint,
                    "ValidFromUtc" timestamptz,
                    "ValidUntilUtc" timestamptz,
                    "AuthoritySnapshotSchemaVersion" integer,
                    "AuthoritySnapshotId" uuid,
                    "AuthorityArtifactId" uuid,
                    "AuthorityArtifactVersion" integer,
                    "ControllerIdentity" text,
                    "ApprovedPurpose" text,
                    "StableDataScopeId" text,
                    "RetentionPolicyId" text,
                    "RetentionPolicyVersion" integer,
                    "RetentionClass" text,
                    "RetentionStartEvent" text,
                    "AbsoluteSourceExpiresAtUtc" timestamptz,
                    "ReuseDisposition" text,
                    "ExtensionDisposition" text,
                    "RevocationPolicyId" text,
                    "PurgePolicyId" text,
                    "LegalHoldPolicyId" text,
                    "EvaluatedAtUtc" timestamptz)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    WITH latest AS (
                        SELECT snapshot.*
                        FROM tagekyc.raw_export_authority_snapshots AS snapshot
                        WHERE snapshot."ClientApplicationId" =
                                p_client_application_id
                          AND snapshot."VerificationSessionId" =
                                p_verification_session_id
                          AND snapshot."CaptureAcceptanceId" =
                                p_capture_acceptance_id
                          AND snapshot."RawClass" = p_raw_class
                        ORDER BY snapshot."Revision" DESC
                        LIMIT 1
                    )
                    SELECT
                        latest."Revision",latest."ValidFromUtc",
                        latest."ValidUntilUtc",
                        latest."AuthoritySnapshotSchemaVersion",
                        latest."AuthoritySnapshotId",
                        latest."AuthorityArtifactId",
                        latest."AuthorityArtifactVersion",
                        latest."ControllerIdentity"::text,
                        latest."ApprovedPurpose"::text,
                        latest."StableDataScopeId"::text,
                        latest."RetentionPolicyId"::text,
                        latest."RetentionPolicyVersion",
                        latest."RetentionClass"::text,
                        latest."RetentionStartEvent"::text,
                        latest."AbsoluteSourceExpiresAtUtc",
                        latest."ReuseDisposition"::text,
                        latest."ExtensionDisposition"::text,
                        latest."RevocationPolicyId"::text,
                        latest."PurgePolicyId"::text,
                        latest."LegalHoldPolicyId"::text,
                        latest."EvaluatedAtUtc"
                    FROM latest
                    WHERE latest."EventType" = 'Granted'
                      AND p_evaluated_at_utc >= latest."ValidFromUtc"
                      AND (
                            latest."ValidUntilUtc" IS NULL
                            OR p_evaluated_at_utc <
                                latest."ValidUntilUtc");
                $$;

                CREATE TRIGGER
                    tr_raw_export_authority_snapshots_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_authority_snapshots
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_authority_snapshot_insert();

                CREATE TRIGGER
                    tr_raw_export_authority_snapshots_append_only
                BEFORE UPDATE OR DELETE
                ON tagekyc.raw_export_authority_snapshots
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.deny_append_only_mutation();

                ALTER FUNCTION
                    tagekyc.enforce_raw_export_authority_snapshot_insert()
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,
                        integer,text,text,timestamptz,text,text,text,
                        timestamptz,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_withdraw_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_revoke_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.enforce_raw_export_authority_snapshot_insert(),
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,
                        integer,text,text,timestamptz,text,text,text,
                        timestamptz,timestamptz),
                    tagekyc.raw_export_withdraw_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid),
                    tagekyc.raw_export_revoke_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid),
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                FROM PUBLIC, tagekyc_runtime;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                TO tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS
                    tr_raw_export_authority_snapshots_append_only
                    ON tagekyc.raw_export_authority_snapshots;
                DROP TRIGGER IF EXISTS
                    tr_raw_export_authority_snapshots_insert_guard
                    ON tagekyc.raw_export_authority_snapshots;

                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_revoke_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_withdraw_authority_snapshot(
                        uuid,uuid,uuid,text,bigint,uuid);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,
                        integer,text,text,timestamptz,text,text,text,
                        timestamptz,timestamptz);
                DROP FUNCTION IF EXISTS
                    tagekyc.enforce_raw_export_authority_snapshot_insert();
                """);

            migrationBuilder.DropTable(
                name: "raw_export_authority_snapshots",
                schema: "tagekyc");
        }
    }
}
