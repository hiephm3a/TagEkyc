using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2CoreNewCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_ingress_state",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_authority_snapshot_event_shape",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_authority_snapshot_values",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsentPolicyId",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsentPolicyVersion",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "raw_export_source_reservations",
                schema: "tagekyc",
                columns: table => new
                {
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngressClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoritySnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    AuthoritySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRefTokenSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefTokenKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubjectRefTokenKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    StorageProfileId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceEncryptionProfileId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceEncryptionProfileVersion = table.Column<int>(type: "integer", nullable: false),
                    AbsoluteSourceExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AdmissionFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    EffectivePlaintextRetentionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReservationExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceReservationFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentCommitmentSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitmentKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentCommitmentKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitment = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClaimedPlaintextLength = table.Column<long>(type: "bigint", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaintextRetentionStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaintextRetentionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaintextRetentionBudgetSeconds = table.Column<int>(type: "integer", nullable: false),
                    ControllerIdentity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StableDataScopeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsentPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentPolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_reservations", x => x.SourceArtifactId);
                    table.UniqueConstraint("uq_raw_export_source_ingress_source", x => x.IngressClaimId);
                    table.CheckConstraint("ck_raw_export_source_reservation_values", "\"SchemaVersion\" = 1\nAND \"AuthoritySnapshotSchemaVersion\" = 1\nAND \"SubjectRefTokenSchemaVersion\" = 1\nAND \"ContentCommitmentSchemaVersion\" = 1\nAND \"SubjectRefTokenKeyVersion\" >= 1\nAND \"ContentCommitmentKeyVersion\" >= 1\nAND \"SourceEncryptionProfileVersion\" >= 1\nAND \"ConsentPolicyVersion\" >= 1\nAND \"ClaimedPlaintextLength\" >= 0\nAND \"PlaintextRetentionBudgetSeconds\" >= 1\nAND octet_length(\"SubjectRefToken\") = 32\nAND octet_length(\"ContentCommitment\") = 32\nAND octet_length(\"AdmissionFingerprint\") = 32\nAND octet_length(\"SourceReservationFingerprint\") = 32\nAND \"PlaintextRetentionExpiresAtUtc\" > \"PlaintextRetentionStartedAtUtc\"\nAND \"EffectivePlaintextRetentionExpiresAtUtc\" <= \"PlaintextRetentionExpiresAtUtc\"\nAND \"ReservationExpiresAtUtc\" <= \"EffectivePlaintextRetentionExpiresAtUtc\"\nAND \"ReservationExpiresAtUtc\" <= \"AbsoluteSourceExpiresAtUtc\"");
                    table.ForeignKey(
                        name: "fk_raw_export_source_reservation_claim",
                        column: x => x.IngressClaimId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_ingress_claims",
                        principalColumn: "IngressClaimId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_reservation_consent_policy",
                        columns: x => new { x.ConsentPolicyId, x.ConsentPolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_source_encryption_attempts",
                schema: "tagekyc",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptionAttemptRevision = table.Column<long>(type: "bigint", nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    ProvisionalObjectIdentity = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyProviderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KekId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KekVersion = table.Column<int>(type: "integer", nullable: false),
                    KekFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EncryptionSuiteId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EncryptionFramingVersion = table.Column<int>(type: "integer", nullable: false),
                    NonceStrategyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NonceDerivationSeedReferenceOrWrappedSeed = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NonceDerivationSeedCommitment = table.Column<byte[]>(type: "bytea", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    FramingParametersDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptionAttemptFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    OwnershipLeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    R2TerminationDisposition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_encryption_attempts", x => x.AttemptId);
                    table.UniqueConstraint("uq_raw_export_source_attempt_fence", x => new { x.SourceArtifactId, x.AttemptId, x.Fence });
                    table.UniqueConstraint("uq_raw_export_source_attempt_revision", x => new { x.SourceArtifactId, x.EncryptionAttemptRevision });
                    table.CheckConstraint("ck_raw_export_source_attempt_values", "\"SchemaVersion\" = 1\nAND \"EncryptionAttemptRevision\" >= 1\nAND \"Fence\" >= 1\nAND \"KekVersion\" >= 1\nAND \"EncryptionFramingVersion\" >= 1\nAND \"ChunkSize\" >= 1\nAND octet_length(\"NonceDerivationSeedCommitment\") = 32\nAND octet_length(\"FramingParametersDigest\") = 32\nAND octet_length(\"EncryptionAttemptFingerprint\") = 32\nAND \"R2TerminationDisposition\" IS NULL");
                    table.ForeignKey(
                        name: "fk_raw_export_source_attempt_reservation",
                        column: x => x.SourceArtifactId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_reservations",
                        principalColumn: "SourceArtifactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_source_head",
                schema: "tagekyc",
                columns: table => new
                {
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyState = table.Column<string>(type: "text", nullable: false),
                    CurrentEncryptionAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationRevision = table.Column<long>(type: "bigint", nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_head", x => x.SourceArtifactId);
                    table.CheckConstraint("ck_raw_export_source_head_values", "\"CustodyState\" = 'Reserved' AND \"ReservationRevision\" >= 1 AND \"Fence\" >= 1");
                    table.ForeignKey(
                        name: "fk_raw_export_source_head_attempt",
                        columns: x => new { x.SourceArtifactId, x.CurrentEncryptionAttemptId, x.Fence },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumns: new[] { "SourceArtifactId", "AttemptId", "Fence" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_head_reservation",
                        column: x => x.SourceArtifactId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_reservations",
                        principalColumn: "SourceArtifactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_ingress_state",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims",
                sql: "\"ClaimState\" IN ('ClaimEvaluating','Reserved')\nAND \"CaptureRevision\" >= 1\nAND \"CommitmentKeySelectorVersion\" >= 1\nAND octet_length(\"IngressIdentityFingerprint\") = 32");

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_authority_consent_policy",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                columns: new[] { "ConsentPolicyId", "ConsentPolicyVersion" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_authority_snapshot_event_shape",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                sql: "(\n    \"EventType\" = 'Granted'\n    AND \"TargetRevision\" IS NULL\n    AND \"ValidFromUtc\" IS NOT NULL\n    AND (\"ValidUntilUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\")\n    AND \"AuthoritySnapshotSchemaVersion\" IS NOT NULL\n    AND \"AuthoritySnapshotId\" IS NOT NULL\n    AND \"AuthorityArtifactId\" IS NOT NULL\n    AND \"AuthorityArtifactVersion\" IS NOT NULL\n    AND \"ControllerIdentity\" IS NOT NULL\n    AND \"ApprovedPurpose\" IS NOT NULL\n    AND \"StableDataScopeId\" IS NOT NULL\n    AND \"RetentionPolicyId\" IS NOT NULL\n    AND \"RetentionPolicyVersion\" IS NOT NULL\n    AND \"ConsentPolicyId\" IS NOT NULL\n    AND \"ConsentPolicyVersion\" IS NOT NULL\n    AND \"RetentionClass\" IS NOT NULL\n    AND \"RetentionStartEvent\" IS NOT NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NOT NULL\n    AND \"ReuseDisposition\" IS NOT NULL\n    AND \"ExtensionDisposition\" IS NOT NULL\n    AND \"RevocationPolicyId\" IS NOT NULL\n    AND \"PurgePolicyId\" IS NOT NULL\n    AND \"LegalHoldPolicyId\" IS NOT NULL\n    AND \"EvaluatedAtUtc\" IS NOT NULL\n    AND \"CapturedByPrincipalId\" IS NOT NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Withdrawn'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"ConsentPolicyId\" IS NULL\n    AND \"ConsentPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NOT NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Revoked'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"ConsentPolicyId\" IS NULL\n    AND \"ConsentPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NOT NULL\n)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_authority_snapshot_values",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                sql: "\"Revision\" >= 1\nAND (\"TargetRevision\" IS NULL OR \"TargetRevision\" >= 1)\nAND \"ClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"VerificationSessionId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"CaptureAcceptanceId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND btrim(\"RawClass\") <> ''\nAND (\"AuthoritySnapshotSchemaVersion\" IS NULL OR \"AuthoritySnapshotSchemaVersion\" = 1)\nAND (\"AuthorityArtifactVersion\" IS NULL OR \"AuthorityArtifactVersion\" >= 1)\nAND (\"RetentionPolicyVersion\" IS NULL OR \"RetentionPolicyVersion\" >= 1)\nAND (\"ConsentPolicyVersion\" IS NULL OR \"ConsentPolicyVersion\" >= 1)\nAND (\"AuthoritySnapshotId\" IS NULL OR \"AuthoritySnapshotId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"AuthorityArtifactId\" IS NULL OR \"AuthorityArtifactId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"ControllerIdentity\" IS NULL OR btrim(\"ControllerIdentity\") <> '')\nAND (\"ApprovedPurpose\" IS NULL OR \"ApprovedPurpose\" = 'SubjectRawBiometricExport')\nAND (\"StableDataScopeId\" IS NULL OR btrim(\"StableDataScopeId\") <> '')\nAND (\"RetentionPolicyId\" IS NULL OR btrim(\"RetentionPolicyId\") <> '')\nAND (\"RetentionClass\" IS NULL OR btrim(\"RetentionClass\") <> '')\nAND (\"RetentionStartEvent\" IS NULL OR btrim(\"RetentionStartEvent\") <> '')\nAND (\"ReuseDisposition\" IS NULL OR \"ReuseDisposition\" = 'FreshAuthorityRequired')\nAND (\"ExtensionDisposition\" IS NULL OR \"ExtensionDisposition\" = 'Forbidden')\nAND (\"RevocationPolicyId\" IS NULL OR btrim(\"RevocationPolicyId\") <> '')\nAND (\"PurgePolicyId\" IS NULL OR btrim(\"PurgePolicyId\") <> '')\nAND (\"LegalHoldPolicyId\" IS NULL OR btrim(\"LegalHoldPolicyId\") <> '')\nAND (\n    \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    OR \"EvaluatedAtUtc\" IS NULL\n    OR \"AbsoluteSourceExpiresAtUtc\" > \"EvaluatedAtUtc\"\n)");

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_source_head_attempt",
                schema: "tagekyc",
                table: "raw_export_source_head",
                columns: new[] { "SourceArtifactId", "CurrentEncryptionAttemptId", "Fence" });

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_source_reservation_consent",
                schema: "tagekyc",
                table: "raw_export_source_reservations",
                columns: new[] { "ConsentPolicyId", "ConsentPolicyVersion" });

            migrationBuilder.AddForeignKey(
                name: "fk_raw_export_authority_snapshot_consent_policy",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                columns: new[] { "ConsentPolicyId", "ConsentPolicyVersion" },
                principalSchema: "tagekyc",
                principalTable: "raw_export_policy_versions",
                principalColumns: new[] { "PolicyId", "PolicyVersion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_roles
                        WHERE rolname = 'tagekyc_raw_export_claim_broker')
                    THEN
                        CREATE ROLE tagekyc_raw_export_claim_broker NOLOGIN;
                    END IF;
                END
                $$;

                GRANT USAGE ON SCHEMA tagekyc
                TO tagekyc_raw_export_claim_broker;

                ALTER TABLE tagekyc.raw_export_source_reservations
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_source_encryption_attempts
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_source_head
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON TABLE
                    tagekyc.raw_export_source_reservations,
                    tagekyc.raw_export_source_encryption_attempts,
                    tagekyc.raw_export_source_head
                FROM PUBLIC, tagekyc_runtime, tagekyc_raw_export_claim_broker;

                CREATE OR REPLACE FUNCTION
                    tagekyc.enforce_raw_export_source_core_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                BEGIN
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR pg_catalog.current_setting(
                            'tagekyc.raw_export_source_core_write_context',
                            true) <> 'complete-r1' THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
                    END IF;
                    IF TG_OP <> 'INSERT' THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.enforce_raw_export_source_head_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                BEGIN
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR pg_catalog.current_setting(
                            'tagekyc.raw_export_source_core_write_context',
                            true) <> 'complete-r1'
                       OR TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.enforce_raw_export_source_ingress_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    expected_context text := TG_ARGV[0] || ':' || TG_OP;
                    actual_context text;
                BEGIN
                    actual_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_source_ingress_write_context',
                        true);

                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR actual_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_INGRESS_DIRECT_DML_UNSUPPORTED';
                    END IF;

                    IF TG_ARGV[0] = 'claim'
                       AND TG_OP = 'UPDATE'
                       AND pg_catalog.to_jsonb(NEW)->>'ClaimState' = 'Reserved'
                       AND NOT EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_source_reservations reservation
                            WHERE reservation."IngressClaimId" =
                                NEW."IngressClaimId") THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_RESERVATION_REQUIRED';
                    END IF;

                    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
                END;
                $$;

                CREATE TRIGGER tr_raw_export_source_reservations_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_source_reservations
                FOR EACH ROW
                EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_source_core_write();

                CREATE TRIGGER tr_raw_export_source_attempts_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_source_encryption_attempts
                FOR EACH ROW
                EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_source_core_write();

                CREATE TRIGGER tr_raw_export_source_head_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_source_head
                FOR EACH ROW
                EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_source_head_write();

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_c1_hash_canonical(
                    p_domain text,
                    VARIADIC p_fields text[])
                RETURNS bytea
                LANGUAGE plpgsql
                IMMUTABLE
                STRICT
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    payload bytea := ''::bytea;
                    normalized text;
                    field text;
                BEGIN
                    normalized := pg_catalog.normalize(p_domain, 'NFC');
                    payload := payload
                        || pg_catalog.int4send(
                            pg_catalog.octet_length(
                                pg_catalog.convert_to(normalized, 'UTF8')))
                        || pg_catalog.convert_to(normalized, 'UTF8');
                    FOREACH field IN ARRAY p_fields LOOP
                        normalized := pg_catalog.normalize(field, 'NFC');
                        payload := payload
                            || pg_catalog.int4send(
                                pg_catalog.octet_length(
                                    pg_catalog.convert_to(normalized, 'UTF8')))
                            || pg_catalog.convert_to(normalized, 'UTF8');
                    END LOOP;
                    RETURN pg_catalog.sha256(payload);
                END;
                $$;

                DROP FUNCTION
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                        text,text,timestamptz,text,text,text,timestamptz,timestamptz);

                CREATE FUNCTION tagekyc.raw_export_append_authority_snapshot(
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
                    p_consent_policy_id uuid,
                    p_consent_policy_version integer,
                    p_retention_class text,
                    p_retention_start_event text,
                    p_absolute_source_expires_at_utc timestamptz,
                    p_revocation_policy_id text,
                    p_purge_policy_id text,
                    p_legal_hold_policy_id text,
                    p_evaluated_at_utc timestamptz,
                    p_valid_until_utc timestamptz)
                RETURNS TABLE("Revision" bigint, "AuthoritySnapshotId" uuid)
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
                       OR p_capture_acceptance_id IS NULL
                       OR p_authority_artifact_id IS NULL
                       OR p_authority_artifact_version < 1
                       OR p_retention_policy_version < 1
                       OR p_consent_policy_id IS NULL
                       OR p_consent_policy_id =
                            '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_consent_policy_version < 1
                       OR pg_catalog.btrim(p_raw_class) = ''
                       OR pg_catalog.btrim(p_controller_identity) = ''
                       OR pg_catalog.btrim(p_stable_data_scope_id) = ''
                       OR pg_catalog.btrim(p_retention_policy_id) = ''
                       OR pg_catalog.btrim(p_retention_class) = ''
                       OR pg_catalog.btrim(p_retention_start_event) = ''
                       OR pg_catalog.btrim(p_revocation_policy_id) = ''
                       OR pg_catalog.btrim(p_purge_policy_id) = ''
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
                            "AuthoritySnapshotEventId","EventType","Revision",
                            "ValidFromUtc","ValidUntilUtc","RecordedAtUtc",
                            "CapturedByPrincipalId","ClientApplicationId",
                            "VerificationSessionId","CaptureAcceptanceId",
                            "RawClass","AuthoritySnapshotSchemaVersion",
                            "AuthoritySnapshotId","AuthorityArtifactId",
                            "AuthorityArtifactVersion","ControllerIdentity",
                            "ApprovedPurpose","StableDataScopeId",
                            "RetentionPolicyId","RetentionPolicyVersion",
                            "ConsentPolicyId","ConsentPolicyVersion",
                            "RetentionClass","RetentionStartEvent",
                            "AbsoluteSourceExpiresAtUtc","ReuseDisposition",
                            "ExtensionDisposition","RevocationPolicyId",
                            "PurgePolicyId","LegalHoldPolicyId","EvaluatedAtUtc")
                    VALUES
                        (
                            pg_catalog.gen_random_uuid(),'Granted',next_revision,
                            pg_catalog.transaction_timestamp(),p_valid_until_utc,
                            pg_catalog.transaction_timestamp(),actor_id,
                            p_client_application_id,p_verification_session_id,
                            p_capture_acceptance_id,p_raw_class,1,snapshot_id,
                            p_authority_artifact_id,p_authority_artifact_version,
                            p_controller_identity,'SubjectRawBiometricExport',
                            p_stable_data_scope_id,p_retention_policy_id,
                            p_retention_policy_version,p_consent_policy_id,
                            p_consent_policy_version,p_retention_class,
                            p_retention_start_event,
                            p_absolute_source_expires_at_utc,
                            'FreshAuthorityRequired','Forbidden',
                            p_revocation_policy_id,p_purge_policy_id,
                            p_legal_hold_policy_id,p_evaluated_at_utc);
                    RETURN QUERY SELECT next_revision, snapshot_id;
                END;
                $$;

                DROP FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz);

                CREATE FUNCTION
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
                    "ConsentPolicyId" uuid,
                    "ConsentPolicyVersion" integer,
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
                        LIMIT 1)
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
                        latest."ConsentPolicyId",
                        latest."ConsentPolicyVersion",
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
                            OR p_evaluated_at_utc < latest."ValidUntilUtc");
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
                    tagekyc.complete_raw_export_source_ingress_claim(
                        p_client_application_id uuid,
                        p_producer_id text,
                        p_capture_agent_instance_id text,
                        p_ingress_idempotency_key text,
                        p_claim_evaluation_id uuid,
                        p_claim_evaluation_revision bigint,
                        p_claim_evaluation_fence bigint,
                        p_token_variant text,
                        p_token_expires_at_utc timestamptz,
                        p_claim_evaluation_token text,
                        p_producer_envelope_fingerprint bytea,
                        p_commitment_schema integer,
                        p_commitment_key_id text,
                        p_commitment_key_version integer,
                        p_content_commitment bytea,
                        p_subject_token_schema integer,
                        p_subject_token_key_id text,
                        p_subject_token_key_version integer,
                        p_subject_token bytea,
                        p_claimed_plaintext_length bigint,
                        p_media_type text,
                        p_captured_at_utc timestamptz,
                        p_retention_started_at_utc timestamptz,
                        p_retention_expires_at_utc timestamptz,
                        p_retention_budget_seconds integer,
                        p_storage_profile_id text,
                        p_source_profile_id text,
                        p_source_profile_version integer,
                        p_encryption_suite_id text,
                        p_encryption_framing_version integer,
                        p_nonce_strategy_id text,
                        p_nonce_seed_commitment bytea,
                        p_chunk_size integer,
                        p_framing_parameters_digest bytea,
                        p_key_provider_id text,
                        p_kek_id text,
                        p_kek_version integer,
                        p_kek_fingerprint text,
                        p_max_continuation_seconds integer,
                        p_attempt_deadline_seconds integer,
                        p_safety_margin_milliseconds integer,
                        p_ownership_lease_seconds integer)
                RETURNS TABLE("OutcomeCode" text, "SourceArtifactId" uuid)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
                    claim_row tagekyc.raw_export_source_ingress_claims%ROWTYPE;
                    authority record;
                    consent record;
                    now_utc timestamptz := pg_catalog.statement_timestamp();
                    effective_expires timestamptz;
                    reservation_expires timestamptz;
                    source_id uuid := pg_catalog.gen_random_uuid();
                    attempt_id uuid := pg_catalog.gen_random_uuid();
                    object_id uuid := pg_catalog.gen_random_uuid();
                    key_reservation_id uuid := pg_catalog.gen_random_uuid();
                    admission bytea;
                    reservation_fingerprint bytea;
                    attempt_fingerprint bytea;
                    canonical_timestamp text;
                BEGIN
                    IF NOT tagekyc.validate_raw_export_claim_evaluation_token(
                        p_client_application_id,p_producer_id,
                        p_capture_agent_instance_id,p_ingress_idempotency_key,
                        p_claim_evaluation_id,p_claim_evaluation_revision,
                        p_claim_evaluation_fence,p_token_variant,
                        p_token_expires_at_utc,p_claim_evaluation_token) THEN
                        RETURN QUERY SELECT 'CLAIM_TOKEN_INVALID'::text, NULL::uuid;
                        RETURN;
                    END IF;

                    SELECT alias.* INTO alias_row
                    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
                    WHERE alias."ClientApplicationId" = p_client_application_id
                      AND alias."ProducerId" =
                            pg_catalog.normalize(p_producer_id, 'NFC')
                      AND alias."CaptureAgentInstanceId" =
                            pg_catalog.normalize(
                                p_capture_agent_instance_id, 'NFC')
                      AND alias."IngressIdempotencyKey" =
                            p_ingress_idempotency_key::uuid
                    FOR UPDATE;
                    SELECT claim.* INTO claim_row
                    FROM tagekyc.raw_export_source_ingress_claims AS claim
                    WHERE claim."IngressClaimId" = alias_row."IngressClaimId"
                    FOR UPDATE;

                    IF alias_row."AliasState" <> 'Evaluating'
                       OR claim_row."ClaimState" <> 'ClaimEvaluating'
                       OR alias_row."ProducerClaimEnvelopeFingerprint"
                            IS DISTINCT FROM p_producer_envelope_fingerprint THEN
                        RETURN QUERY SELECT 'CLAIM_TOKEN_INVALID'::text, NULL::uuid;
                        RETURN;
                    END IF;

                    SELECT * INTO authority
                    FROM tagekyc.raw_export_resolve_current_authority_for_source(
                        p_client_application_id,
                        claim_row."VerificationSessionId",
                        claim_row."CaptureAcceptanceId",
                        claim_row."RawClass",
                        now_utc);
                    IF NOT FOUND
                       OR authority."ApprovedPurpose" <>
                            'SubjectRawBiometricExport'
                       OR pg_catalog.lower(claim_row."AuthoritySnapshotId")
                            <> authority."AuthoritySnapshotId"::text THEN
                        RETURN QUERY SELECT
                            'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
                        RETURN;
                    END IF;

                    SELECT * INTO consent
                    FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
                        claim_row."VerificationSessionId",
                        authority."ConsentPolicyId",
                        authority."ConsentPolicyVersion")
                    WHERE "RawClass" = claim_row."RawClass"
                    LIMIT 1;
                    IF NOT FOUND
                       OR consent."State" <> 'Effective'
                       OR consent."PurposeCode" <>
                            'SubjectRawBiometricExport' THEN
                        RETURN QUERY SELECT
                            'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
                        RETURN;
                    END IF;

                    effective_expires := LEAST(
                        p_retention_expires_at_utc,
                        now_utc +
                            pg_catalog.make_interval(
                                secs => p_max_continuation_seconds));
                    IF effective_expires - now_utc <
                        pg_catalog.make_interval(
                            secs => p_attempt_deadline_seconds)
                        + p_safety_margin_milliseconds
                            * interval '1 millisecond' THEN
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            'alias:UPDATE',
                            true);
                        UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                        SET "CurrentClaimEvaluationDisposition" = 'Completed'
                        WHERE "IngressClaimAliasId" =
                                alias_row."IngressClaimAliasId"
                          AND "CurrentClaimEvaluationDisposition" = 'Active'
                          AND "CurrentClaimEvaluationRevision" =
                                p_claim_evaluation_revision
                          AND "CurrentClaimEvaluationFence" =
                                p_claim_evaluation_fence;
                        RETURN QUERY SELECT
                            'RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID'::text,
                            NULL::uuid;
                        RETURN;
                    END IF;

                    admission := tagekyc.raw_export_c1_hash_canonical(
                        'tip-88c1-ingress-admission-v1',
                        pg_catalog.encode(
                            claim_row."IngressIdentityFingerprint",'hex'),
                        p_commitment_schema::text,p_commitment_key_id,
                        p_commitment_key_version::text,
                        pg_catalog.encode(p_content_commitment,'hex'),
                        p_media_type,
                        pg_catalog.to_char(
                            pg_catalog.date_trunc(
                                'microseconds',
                                p_captured_at_utc AT TIME ZONE 'UTC'),
                            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                        pg_catalog.to_char(
                            pg_catalog.date_trunc(
                                'microseconds',
                                p_retention_started_at_utc AT TIME ZONE 'UTC'),
                            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                        pg_catalog.to_char(
                            pg_catalog.date_trunc(
                                'microseconds',
                                p_retention_expires_at_utc AT TIME ZONE 'UTC'),
                            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                        p_retention_budget_seconds::text);
                    reservation_fingerprint :=
                        tagekyc.raw_export_c1_hash_canonical(
                            'tip-88c1-source-reservation-v2',
                            pg_catalog.replace(source_id::text,'-',''),
                            pg_catalog.encode(admission,'hex'),
                            p_subject_token_schema::text,
                            p_subject_token_key_id,
                            p_subject_token_key_version::text,
                            pg_catalog.encode(p_subject_token,'hex'),
                            authority."AuthoritySnapshotSchemaVersion"::text,
                            pg_catalog.replace(
                                authority."AuthoritySnapshotId"::text,'-',''),
                            pg_catalog.to_char(
                                pg_catalog.date_trunc(
                                    'microseconds',
                                    authority."AbsoluteSourceExpiresAtUtc"
                                        AT TIME ZONE 'UTC'),
                                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                            p_storage_profile_id,p_source_profile_id,
                            p_source_profile_version::text);
                    attempt_fingerprint :=
                        tagekyc.raw_export_c1_hash_canonical(
                            'tip-88c1-encryption-attempt-v1',
                            pg_catalog.encode(
                                reservation_fingerprint,'hex'),
                            '1','1',
                            pg_catalog.replace(object_id::text,'-',''),
                            pg_catalog.replace(
                                key_reservation_id::text,'-',''),
                            p_encryption_suite_id,
                            p_encryption_framing_version::text,
                            p_key_provider_id,p_kek_id,p_kek_version::text,
                            p_kek_fingerprint,p_nonce_strategy_id,
                            pg_catalog.encode(
                                p_nonce_seed_commitment,'hex'),
                            p_chunk_size::text,
                            pg_catalog.encode(
                                p_framing_parameters_digest,'hex'));
                    reservation_expires := LEAST(
                        now_utc + pg_catalog.make_interval(
                            secs => p_ownership_lease_seconds),
                        effective_expires,
                        authority."AbsoluteSourceExpiresAtUtc");

                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_source_core_write_context',
                        'complete-r1',
                        true);
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_source_ingress_write_context',
                        'alias:UPDATE',
                        true);

                    INSERT INTO tagekyc.raw_export_source_reservations
                        (
                            "SourceArtifactId","IngressClaimId",
                            "AuthoritySnapshotSchemaVersion",
                            "AuthoritySnapshotId",
                            "SubjectRefTokenSchemaVersion",
                            "SubjectRefTokenKeyId","SubjectRefTokenKeyVersion",
                            "SubjectRefToken","StorageProfileId",
                            "SourceEncryptionProfileId",
                            "SourceEncryptionProfileVersion",
                            "AbsoluteSourceExpiresAtUtc",
                            "AdmissionFingerprint",
                            "EffectivePlaintextRetentionExpiresAtUtc",
                            "ReservationExpiresAtUtc",
                            "SourceReservationFingerprint",
                            "ContentCommitmentSchemaVersion",
                            "ContentCommitmentKeyId",
                            "ContentCommitmentKeyVersion",
                            "ContentCommitment","ClaimedPlaintextLength",
                            "MediaType","CapturedAtUtc",
                            "PlaintextRetentionStartedAtUtc",
                            "PlaintextRetentionExpiresAtUtc",
                            "PlaintextRetentionBudgetSeconds",
                            "ControllerIdentity","StableDataScopeId",
                            "ConsentPolicyId","ConsentPolicyVersion",
                            "SchemaVersion","CreatedAtUtc")
                    VALUES
                        (
                            source_id,claim_row."IngressClaimId",
                            authority."AuthoritySnapshotSchemaVersion",
                            authority."AuthoritySnapshotId",
                            p_subject_token_schema,p_subject_token_key_id,
                            p_subject_token_key_version,p_subject_token,
                            p_storage_profile_id,p_source_profile_id,
                            p_source_profile_version,
                            authority."AbsoluteSourceExpiresAtUtc",admission,
                            effective_expires,reservation_expires,
                            reservation_fingerprint,p_commitment_schema,
                            p_commitment_key_id,p_commitment_key_version,
                            p_content_commitment,p_claimed_plaintext_length,
                            p_media_type,p_captured_at_utc,
                            p_retention_started_at_utc,
                            p_retention_expires_at_utc,
                            p_retention_budget_seconds,
                            authority."ControllerIdentity",
                            authority."StableDataScopeId",
                            authority."ConsentPolicyId",
                            authority."ConsentPolicyVersion",1,now_utc);
                    INSERT INTO tagekyc.raw_export_source_encryption_attempts
                        (
                            "AttemptId","SourceArtifactId",
                            "EncryptionAttemptRevision","Fence",
                            "ProvisionalObjectIdentity",
                            "AttemptKeyReservationId","KeyProviderId",
                            "KekId","KekVersion","KekFingerprint",
                            "EncryptionSuiteId","EncryptionFramingVersion",
                            "NonceStrategyId",
                            "NonceDerivationSeedReferenceOrWrappedSeed",
                            "NonceDerivationSeedCommitment","ChunkSize",
                            "FramingParametersDigest",
                            "EncryptionAttemptFingerprint",
                            "OwnershipLeaseExpiresAtUtc",
                            "R2TerminationDisposition","CreatedAtUtc",
                            "SchemaVersion")
                    VALUES
                        (
                            attempt_id,source_id,1,1,object_id,
                            key_reservation_id,p_key_provider_id,p_kek_id,
                            p_kek_version,p_kek_fingerprint,
                            p_encryption_suite_id,
                            p_encryption_framing_version,
                            p_nonce_strategy_id,'none',
                            p_nonce_seed_commitment,p_chunk_size,
                            p_framing_parameters_digest,attempt_fingerprint,
                            now_utc + pg_catalog.make_interval(
                                secs => p_ownership_lease_seconds),
                            NULL,now_utc,1);
                    INSERT INTO tagekyc.raw_export_source_head
                        (
                            "SourceArtifactId","CustodyState",
                            "CurrentEncryptionAttemptId",
                            "ReservationRevision","Fence")
                    VALUES (source_id,'Reserved',attempt_id,1,1);

                    UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                    SET "AliasState" = 'Bound',
                        "CurrentClaimEvaluationDisposition" = 'Completed'
                    WHERE "IngressClaimAliasId" =
                            alias_row."IngressClaimAliasId"
                      AND "CurrentClaimEvaluationDisposition" = 'Active'
                      AND "CurrentClaimEvaluationRevision" =
                            p_claim_evaluation_revision
                      AND "CurrentClaimEvaluationFence" =
                            p_claim_evaluation_fence;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                    END IF;
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_source_ingress_write_context',
                        'claim:UPDATE',
                        true);
                    UPDATE tagekyc.raw_export_source_ingress_claims
                    SET "ClaimState" = 'Reserved'
                    WHERE "IngressClaimId" = claim_row."IngressClaimId"
                      AND "ClaimState" = 'ClaimEvaluating';
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                    END IF;

                    RETURN QUERY SELECT 'NewReservation'::text,source_id;
                END;
                $$;

                ALTER FUNCTION tagekyc.enforce_raw_export_source_core_write()
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_source_head_write()
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_c1_hash_canonical(
                    text,text[]) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_authority_snapshot(
                    uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                    uuid,integer,text,text,timestamptz,text,text,text,
                    timestamptz,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.complete_raw_export_source_ingress_claim(
                        uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,
                        text,bytea,integer,text,integer,bytea,integer,text,
                        integer,bytea,bigint,text,timestamptz,timestamptz,
                        timestamptz,integer,text,text,integer,text,integer,text,
                        bytea,integer,bytea,text,text,integer,text,integer,
                        integer,integer,integer)
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.enforce_raw_export_source_core_write(),
                    tagekyc.enforce_raw_export_source_head_write(),
                    tagekyc.raw_export_c1_hash_canonical(text,text[]),
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                        uuid,integer,text,text,timestamptz,text,text,text,
                        timestamptz,timestamptz),
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz),
                    tagekyc.complete_raw_export_source_ingress_claim(
                        uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,
                        text,bytea,integer,text,integer,bytea,integer,text,
                        integer,bytea,bigint,text,timestamptz,timestamptz,
                        timestamptz,integer,text,text,integer,text,integer,text,
                        bytea,integer,bytea,text,text,integer,text,integer,
                        integer,integer,integer)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                TO tagekyc_runtime;
                GRANT EXECUTE ON FUNCTION
                    tagekyc.complete_raw_export_source_ingress_claim(
                        uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,
                        text,bytea,integer,text,integer,bytea,integer,text,
                        integer,bytea,bigint,text,timestamptz,timestamptz,
                        timestamptz,integer,text,text,integer,text,integer,text,
                        bytea,integer,bytea,text,text,integer,text,integer,
                        integer,integer,integer)
                TO tagekyc_raw_export_claim_broker;
                REVOKE EXECUTE ON FUNCTION
                    tagekyc.complete_raw_export_source_ingress_claim(
                        uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,
                        text,bytea,integer,text,integer,bytea,integer,text,
                        integer,bytea,bigint,text,timestamptz,timestamptz,
                        timestamptz,integer,text,text,integer,text,integer,text,
                        bytea,integer,bytea,text,text,integer,text,integer,
                        integer,integer,integer)
                FROM tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS
                    tagekyc.complete_raw_export_source_ingress_claim(
                        uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,
                        text,bytea,integer,text,integer,bytea,integer,text,
                        integer,bytea,bigint,text,timestamptz,timestamptz,
                        timestamptz,integer,text,text,integer,text,integer,text,
                        bytea,integer,bytea,text,text,integer,text,integer,
                        integer,integer,integer);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_c1_hash_canonical(text,text[]);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                        uuid,integer,text,text,timestamptz,text,text,text,
                        timestamptz,timestamptz);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz);
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_raw_export_authority_snapshot_consent_policy",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropTable(
                name: "raw_export_source_head",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_source_encryption_attempts",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_source_reservations",
                schema: "tagekyc");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_ingress_state",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims");

            migrationBuilder.DropIndex(
                name: "ix_raw_export_authority_consent_policy",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_authority_snapshot_event_shape",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_authority_snapshot_values",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropColumn(
                name: "ConsentPolicyId",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.DropColumn(
                name: "ConsentPolicyVersion",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_ingress_state",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims",
                sql: "\"ClaimState\" = 'ClaimEvaluating'\nAND \"CaptureRevision\" >= 1\nAND \"CommitmentKeySelectorVersion\" >= 1\nAND octet_length(\"IngressIdentityFingerprint\") = 32");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_authority_snapshot_event_shape",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                sql: "(\n    \"EventType\" = 'Granted'\n    AND \"TargetRevision\" IS NULL\n    AND \"ValidFromUtc\" IS NOT NULL\n    AND (\"ValidUntilUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\")\n    AND \"AuthoritySnapshotSchemaVersion\" IS NOT NULL\n    AND \"AuthoritySnapshotId\" IS NOT NULL\n    AND \"AuthorityArtifactId\" IS NOT NULL\n    AND \"AuthorityArtifactVersion\" IS NOT NULL\n    AND \"ControllerIdentity\" IS NOT NULL\n    AND \"ApprovedPurpose\" IS NOT NULL\n    AND \"StableDataScopeId\" IS NOT NULL\n    AND \"RetentionPolicyId\" IS NOT NULL\n    AND \"RetentionPolicyVersion\" IS NOT NULL\n    AND \"RetentionClass\" IS NOT NULL\n    AND \"RetentionStartEvent\" IS NOT NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NOT NULL\n    AND \"ReuseDisposition\" IS NOT NULL\n    AND \"ExtensionDisposition\" IS NOT NULL\n    AND \"RevocationPolicyId\" IS NOT NULL\n    AND \"PurgePolicyId\" IS NOT NULL\n    AND \"LegalHoldPolicyId\" IS NOT NULL\n    AND \"EvaluatedAtUtc\" IS NOT NULL\n    AND \"CapturedByPrincipalId\" IS NOT NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Withdrawn'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NOT NULL\n    AND \"RevokedByPrincipalId\" IS NULL\n) OR (\n    \"EventType\" = 'Revoked'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"AuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"AuthoritySnapshotId\" IS NULL\n    AND \"AuthorityArtifactId\" IS NULL\n    AND \"AuthorityArtifactVersion\" IS NULL\n    AND \"ControllerIdentity\" IS NULL\n    AND \"ApprovedPurpose\" IS NULL\n    AND \"StableDataScopeId\" IS NULL\n    AND \"RetentionPolicyId\" IS NULL\n    AND \"RetentionPolicyVersion\" IS NULL\n    AND \"RetentionClass\" IS NULL\n    AND \"RetentionStartEvent\" IS NULL\n    AND \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    AND \"ReuseDisposition\" IS NULL\n    AND \"ExtensionDisposition\" IS NULL\n    AND \"RevocationPolicyId\" IS NULL\n    AND \"PurgePolicyId\" IS NULL\n    AND \"LegalHoldPolicyId\" IS NULL\n    AND \"EvaluatedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND \"RevokedByPrincipalId\" IS NOT NULL\n)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_authority_snapshot_values",
                schema: "tagekyc",
                table: "raw_export_authority_snapshots",
                sql: "\"Revision\" >= 1\nAND (\"TargetRevision\" IS NULL OR \"TargetRevision\" >= 1)\nAND \"ClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"VerificationSessionId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"CaptureAcceptanceId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND btrim(\"RawClass\") <> ''\nAND (\"AuthoritySnapshotSchemaVersion\" IS NULL OR \"AuthoritySnapshotSchemaVersion\" = 1)\nAND (\"AuthorityArtifactVersion\" IS NULL OR \"AuthorityArtifactVersion\" >= 1)\nAND (\"RetentionPolicyVersion\" IS NULL OR \"RetentionPolicyVersion\" >= 1)\nAND (\"AuthoritySnapshotId\" IS NULL OR \"AuthoritySnapshotId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"AuthorityArtifactId\" IS NULL OR \"AuthorityArtifactId\" <> '00000000-0000-0000-0000-000000000000'::uuid)\nAND (\"ControllerIdentity\" IS NULL OR btrim(\"ControllerIdentity\") <> '')\nAND (\"ApprovedPurpose\" IS NULL OR \"ApprovedPurpose\" = 'SubjectRawBiometricExport')\nAND (\"StableDataScopeId\" IS NULL OR btrim(\"StableDataScopeId\") <> '')\nAND (\"RetentionPolicyId\" IS NULL OR btrim(\"RetentionPolicyId\") <> '')\nAND (\"RetentionClass\" IS NULL OR btrim(\"RetentionClass\") <> '')\nAND (\"RetentionStartEvent\" IS NULL OR btrim(\"RetentionStartEvent\") <> '')\nAND (\"ReuseDisposition\" IS NULL OR \"ReuseDisposition\" = 'FreshAuthorityRequired')\nAND (\"ExtensionDisposition\" IS NULL OR \"ExtensionDisposition\" = 'Forbidden')\nAND (\"RevocationPolicyId\" IS NULL OR btrim(\"RevocationPolicyId\") <> '')\nAND (\"PurgePolicyId\" IS NULL OR btrim(\"PurgePolicyId\") <> '')\nAND (\"LegalHoldPolicyId\" IS NULL OR btrim(\"LegalHoldPolicyId\") <> '')\nAND (\n    \"AbsoluteSourceExpiresAtUtc\" IS NULL\n    OR \"EvaluatedAtUtc\" IS NULL\n    OR \"AbsoluteSourceExpiresAtUtc\" > \"EvaluatedAtUtc\"\n)");

            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS
                    tagekyc.enforce_raw_export_source_core_write();
                DROP FUNCTION IF EXISTS
                    tagekyc.enforce_raw_export_source_head_write();

                CREATE OR REPLACE FUNCTION
                    tagekyc.enforce_raw_export_source_ingress_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    expected_context text := TG_ARGV[0] || ':' || TG_OP;
                    actual_context text;
                BEGIN
                    actual_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_source_ingress_write_context',
                        true);

                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR actual_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_INGRESS_DIRECT_DML_UNSUPPORTED';
                    END IF;

                    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
                END;
                $$;

                CREATE FUNCTION tagekyc.raw_export_append_authority_snapshot(
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
                RETURNS TABLE("Revision" bigint, "AuthoritySnapshotId" uuid)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid := tagekyc.raw_export_current_actor();
                    next_revision bigint;
                    snapshot_id uuid := pg_catalog.gen_random_uuid();
                BEGIN
                    IF p_authority_artifact_version < 1
                       OR p_retention_policy_version < 1
                       OR p_absolute_source_expires_at_utc <= p_evaluated_at_utc
                    THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_AUTHORITY_SNAPSHOT_VALUE_INVALID';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtext(
                            'tip88c1:b2-authority:' ||
                            p_client_application_id::text || ':' ||
                            p_verification_session_id::text || ':' ||
                            p_capture_acceptance_id::text || ':' ||
                            p_raw_class));
                    SELECT COALESCE(MAX(snapshot."Revision"),0) + 1
                    INTO next_revision
                    FROM tagekyc.raw_export_authority_snapshots snapshot
                    WHERE snapshot."ClientApplicationId" =
                            p_client_application_id
                      AND snapshot."VerificationSessionId" =
                            p_verification_session_id
                      AND snapshot."CaptureAcceptanceId" =
                            p_capture_acceptance_id
                      AND snapshot."RawClass" = p_raw_class;
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_authority_snapshot_append_context',
                        'grant',true);
                    INSERT INTO tagekyc.raw_export_authority_snapshots
                        (
                            "AuthoritySnapshotEventId","EventType","Revision",
                            "ValidFromUtc","ValidUntilUtc","RecordedAtUtc",
                            "CapturedByPrincipalId","ClientApplicationId",
                            "VerificationSessionId","CaptureAcceptanceId",
                            "RawClass","AuthoritySnapshotSchemaVersion",
                            "AuthoritySnapshotId","AuthorityArtifactId",
                            "AuthorityArtifactVersion","ControllerIdentity",
                            "ApprovedPurpose","StableDataScopeId",
                            "RetentionPolicyId","RetentionPolicyVersion",
                            "RetentionClass","RetentionStartEvent",
                            "AbsoluteSourceExpiresAtUtc","ReuseDisposition",
                            "ExtensionDisposition","RevocationPolicyId",
                            "PurgePolicyId","LegalHoldPolicyId","EvaluatedAtUtc")
                    VALUES
                        (
                            pg_catalog.gen_random_uuid(),'Granted',next_revision,
                            pg_catalog.transaction_timestamp(),p_valid_until_utc,
                            pg_catalog.transaction_timestamp(),actor_id,
                            p_client_application_id,p_verification_session_id,
                            p_capture_acceptance_id,p_raw_class,1,snapshot_id,
                            p_authority_artifact_id,p_authority_artifact_version,
                            p_controller_identity,'SubjectRawBiometricExport',
                            p_stable_data_scope_id,p_retention_policy_id,
                            p_retention_policy_version,p_retention_class,
                            p_retention_start_event,
                            p_absolute_source_expires_at_utc,
                            'FreshAuthorityRequired','Forbidden',
                            p_revocation_policy_id,p_purge_policy_id,
                            p_legal_hold_policy_id,p_evaluated_at_utc);
                    RETURN QUERY SELECT next_revision,snapshot_id;
                END;
                $$;

                CREATE FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        p_client_application_id uuid,
                        p_verification_session_id uuid,
                        p_capture_acceptance_id uuid,
                        p_raw_class text,
                        p_evaluated_at_utc timestamptz)
                RETURNS TABLE(
                    "Revision" bigint,"ValidFromUtc" timestamptz,
                    "ValidUntilUtc" timestamptz,
                    "AuthoritySnapshotSchemaVersion" integer,
                    "AuthoritySnapshotId" uuid,"AuthorityArtifactId" uuid,
                    "AuthorityArtifactVersion" integer,
                    "ControllerIdentity" text,"ApprovedPurpose" text,
                    "StableDataScopeId" text,"RetentionPolicyId" text,
                    "RetentionPolicyVersion" integer,"RetentionClass" text,
                    "RetentionStartEvent" text,
                    "AbsoluteSourceExpiresAtUtc" timestamptz,
                    "ReuseDisposition" text,"ExtensionDisposition" text,
                    "RevocationPolicyId" text,"PurgePolicyId" text,
                    "LegalHoldPolicyId" text,"EvaluatedAtUtc" timestamptz)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    WITH latest AS (
                        SELECT snapshot.*
                        FROM tagekyc.raw_export_authority_snapshots snapshot
                        WHERE snapshot."ClientApplicationId" =
                                p_client_application_id
                          AND snapshot."VerificationSessionId" =
                                p_verification_session_id
                          AND snapshot."CaptureAcceptanceId" =
                                p_capture_acceptance_id
                          AND snapshot."RawClass" = p_raw_class
                        ORDER BY snapshot."Revision" DESC LIMIT 1)
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
                            OR p_evaluated_at_utc < latest."ValidUntilUtc");
                $$;

                ALTER FUNCTION tagekyc.raw_export_append_authority_snapshot(
                    uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                    text,text,timestamptz,text,text,text,timestamptz,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_append_authority_snapshot(
                        uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,
                        text,text,timestamptz,text,text,text,timestamptz,timestamptz),
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_resolve_current_authority_for_source(
                        uuid,uuid,uuid,text,timestamptz)
                TO tagekyc_runtime;
                """);
        }
    }
}
