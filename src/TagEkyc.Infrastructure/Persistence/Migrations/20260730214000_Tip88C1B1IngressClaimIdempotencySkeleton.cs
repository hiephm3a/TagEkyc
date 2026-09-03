using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B1IngressClaimIdempotencySkeleton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_source_ingress_claims",
                schema: "tagekyc",
                columns: table => new
                {
                    IngressClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureAcceptanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticatedPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaptureAgentInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaptureRevision = table.Column<int>(type: "integer", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionChallengeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AuthoritySnapshotId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IngressIdentityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ClaimState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CommitmentKeySelectorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CommitmentKeySelectorVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_ingress_claims", x => x.IngressClaimId);
                    table.UniqueConstraint("uq_raw_export_source_ingress_exact_artifact", x => new { x.ClientApplicationId, x.ProducerId, x.VerificationSessionId, x.CaptureArtifactId, x.CaptureRevision, x.RawClass });
                    table.CheckConstraint("ck_raw_export_source_ingress_state", "\"ClaimState\" = 'ClaimEvaluating'\nAND \"CaptureRevision\" >= 1\nAND \"CommitmentKeySelectorVersion\" >= 1\nAND octet_length(\"IngressIdentityFingerprint\") = 32");
                    table.ForeignKey(
                        name: "fk_raw_export_source_ingress_claims_acceptance",
                        column: x => x.CaptureAcceptanceId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_capture_acceptance_events",
                        principalColumn: "CaptureAcceptanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_ingress_claims_capture_artifact",
                        column: x => x.CaptureArtifactId,
                        principalSchema: "tagekyc",
                        principalTable: "capture_artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_ingress_claims_session",
                        column: x => x.VerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_source_ingress_claim_aliases",
                schema: "tagekyc",
                columns: table => new
                {
                    IngressClaimAliasId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CaptureAgentInstanceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IngressIdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptedIngressIdentityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProducerClaimEnvelopeFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    AliasState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IngressClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentClaimEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentClaimEvaluationOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentClaimEvaluationDisposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentTokenIssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentTokenSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CurrentTokenVariant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentTokenAudience = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CurrentTokenDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    CurrentClaimEvaluationRevision = table.Column<long>(type: "bigint", nullable: false),
                    CurrentClaimEvaluationFence = table.Column<long>(type: "bigint", nullable: false),
                    LatestIssuedTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_ingress_claim_aliases", x => x.IngressClaimAliasId);
                    table.UniqueConstraint("uq_raw_export_source_ingress_alias_key", x => new { x.ClientApplicationId, x.ProducerId, x.CaptureAgentInstanceId, x.IngressIdempotencyKey });
                    table.CheckConstraint("ck_raw_export_source_ingress_alias_state", "\"AliasState\" IN ('Evaluating','Bound','ConflictTombstone')\nAND \"CurrentClaimEvaluationDisposition\"\n    IN ('Active','Completed','Expired','Reclaimed','Conflict')\nAND \"CurrentClaimEvaluationRevision\" >= 1\nAND \"CurrentClaimEvaluationFence\" >= 1\nAND \"CurrentTokenSchemaVersion\" = 1\nAND \"CurrentTokenVariant\"\n    IN ('NewClaimEvaluationToken','ExistingClaimComparisonToken')\nAND \"CurrentTokenAudience\" =\n    'tagekyc.raw-export-source-ingress-claim-comparison'\nAND octet_length(\"AttemptedIngressIdentityFingerprint\") = 32\nAND octet_length(\"ProducerClaimEnvelopeFingerprint\") = 32\nAND octet_length(\"CurrentTokenDigest\") = 32\nAND \"CurrentTokenExpiresAtUtc\" > \"CurrentTokenIssuedAtUtc\"\nAND \"LatestIssuedTokenExpiresAtUtc\" >= \"CurrentTokenExpiresAtUtc\"");
                    table.ForeignKey(
                        name: "fk_raw_export_source_ingress_alias_claim",
                        column: x => x.IngressClaimId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_ingress_claims",
                        principalColumn: "IngressClaimId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_source_ingress_aliases_claim",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claim_aliases",
                column: "IngressClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_source_ingress_claims_acceptance",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims",
                column: "CaptureAcceptanceId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_source_ingress_claims_artifact",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims",
                column: "CaptureArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_source_ingress_claims_session",
                schema: "tagekyc",
                table: "raw_export_source_ingress_claims",
                column: "VerificationSessionId");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON
                    tagekyc.raw_export_source_ingress_claims,
                    tagekyc.raw_export_source_ingress_claim_aliases
                FROM PUBLIC, tagekyc_runtime;

                GRANT SELECT, INSERT, UPDATE ON
                    tagekyc.raw_export_source_ingress_claims,
                    tagekyc.raw_export_source_ingress_claim_aliases
                TO tagekyc_raw_export_deployer;

                GRANT SELECT ON
                    tagekyc.verification_sessions,
                    tagekyc.capture_artifacts,
                    tagekyc.raw_export_capture_acceptance_events
                TO tagekyc_raw_export_deployer;

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

                CREATE TRIGGER tr_raw_export_source_ingress_claims_write_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_source_ingress_claims
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_source_ingress_write('claim');

                CREATE TRIGGER tr_raw_export_source_ingress_aliases_write_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_source_ingress_claim_aliases
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_source_ingress_write('alias');

                CREATE OR REPLACE FUNCTION tagekyc.begin_raw_export_source_ingress_claim(
                    p_authenticated_principal_id uuid,
                    p_client_application_id uuid,
                    p_producer_id text,
                    p_capture_agent_instance_id text,
                    p_ingress_idempotency_key text,
                    p_verification_session_id uuid,
                    p_capture_acceptance_id uuid,
                    p_capture_artifact_id uuid,
                    p_capture_revision integer,
                    p_raw_class text,
                    p_session_challenge_hash text,
                    p_authority_snapshot_id text,
                    p_claimed_plaintext_length bigint,
                    p_media_type text,
                    p_captured_at_utc timestamp with time zone,
                    p_plaintext_retention_started_at_utc timestamp with time zone,
                    p_plaintext_retention_expires_at_utc timestamp with time zone,
                    p_plaintext_retention_budget_seconds integer,
                    p_commitment_key_selector_id text,
                    p_commitment_key_selector_version integer,
                    p_claim_evaluation_owner_id uuid,
                    p_claim_evaluation_token_ttl_seconds integer,
                    p_idempotency_lock_timeout_milliseconds integer)
                RETURNS TABLE(
                    outcome_code text,
                    claim_evaluation_token text,
                    token_variant text,
                    token_expires_at_utc timestamp with time zone,
                    claim_evaluation_id uuid,
                    claim_evaluation_revision bigint,
                    claim_evaluation_fence bigint,
                    retry_not_before_utc timestamp with time zone)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    ingress_key uuid;
                    producer_id_nfc text;
                    capture_agent_instance_id_nfc text;
                    raw_class_nfc text;
                    session_challenge_hash_nfc text;
                    authority_snapshot_id_nfc text;
                    media_type_nfc text;
                    key_selector_id_nfc text;
                    accepted_session_id uuid;
                    accepted_client_id uuid;
                    accepted_artifact_id uuid;
                    accepted_revision integer;
                    accepted_raw_class text;
                    accepted_challenge_hash text;
                    artifact_session_id uuid;
                    session_client_id uuid;
                    alias_lock bigint;
                    exact_lock bigint;
                    first_lock bigint;
                    second_lock bigint;
                    lock_deadline timestamp with time zone;
                    acquired boolean;
                    value_bytes bytea;
                    canonical_preimage bytea;
                    ingress_fingerprint bytea;
                    envelope_fingerprint bytea;
                    claim_row tagekyc.raw_export_source_ingress_claims%ROWTYPE;
                    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
                    new_claim boolean := false;
                    issued_at timestamp with time zone;
                    expires_at timestamp with time zone;
                    evaluation_id uuid;
                    evaluation_revision bigint;
                    evaluation_fence bigint;
                    evaluation_token_bytes bytea;
                    evaluation_token text;
                    evaluation_token_digest bytea;
                    issued_variant text;
                    previous_context text;
                    rows_changed integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF actor_id IS DISTINCT FROM p_authenticated_principal_id THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;

                    IF p_authenticated_principal_id IS NULL
                       OR p_client_application_id IS NULL
                       OR p_verification_session_id IS NULL
                       OR p_capture_acceptance_id IS NULL
                       OR p_capture_artifact_id IS NULL
                       OR p_claim_evaluation_owner_id IS NULL
                       OR p_capture_revision IS NULL
                       OR p_capture_revision < 1
                       OR p_claimed_plaintext_length IS NULL
                       OR p_claimed_plaintext_length < 0
                       OR p_plaintext_retention_budget_seconds IS NULL
                       OR p_plaintext_retention_budget_seconds < 1
                       OR p_commitment_key_selector_version IS NULL
                       OR p_commitment_key_selector_version < 1
                       OR p_claim_evaluation_token_ttl_seconds IS NULL
                       OR p_claim_evaluation_token_ttl_seconds < 1
                       OR p_claim_evaluation_token_ttl_seconds > 300
                       OR p_idempotency_lock_timeout_milliseconds IS NULL
                       OR p_idempotency_lock_timeout_milliseconds < 1
                       OR p_idempotency_lock_timeout_milliseconds > 5000
                       OR p_captured_at_utc IS NULL
                       OR p_plaintext_retention_started_at_utc IS NULL
                       OR p_plaintext_retention_expires_at_utc IS NULL
                       OR p_plaintext_retention_started_at_utc < p_captured_at_utc
                       OR p_plaintext_retention_expires_at_utc
                            <= p_plaintext_retention_started_at_utc THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;

                    producer_id_nfc := pg_catalog.normalize(p_producer_id, 'NFC');
                    capture_agent_instance_id_nfc :=
                        pg_catalog.normalize(p_capture_agent_instance_id, 'NFC');
                    raw_class_nfc := pg_catalog.normalize(p_raw_class, 'NFC');
                    session_challenge_hash_nfc :=
                        pg_catalog.normalize(p_session_challenge_hash, 'NFC');
                    authority_snapshot_id_nfc :=
                        pg_catalog.normalize(p_authority_snapshot_id, 'NFC');
                    media_type_nfc := pg_catalog.normalize(p_media_type, 'NFC');
                    key_selector_id_nfc :=
                        pg_catalog.normalize(p_commitment_key_selector_id, 'NFC');

                    IF producer_id_nfc IS NULL
                       OR producer_id_nfc = ''
                       OR producer_id_nfc IS DISTINCT FROM
                            pg_catalog.replace(actor_id::text, '-', '')
                       OR pg_catalog.octet_length(producer_id_nfc) > 128
                       OR capture_agent_instance_id_nfc IS NULL
                       OR capture_agent_instance_id_nfc = ''
                       OR pg_catalog.octet_length(capture_agent_instance_id_nfc) > 128
                       OR raw_class_nfc IS NULL
                       OR raw_class_nfc NOT IN (
                            'ChipDg1',
                            'ChipDg2Portrait',
                            'ChipDg13',
                            'ChipDg15',
                            'ChipSod',
                            'AaChallenge',
                            'AaResponse',
                            'LiveSelfieImage',
                            'LivenessMedia',
                            'HandSignatureImage')
                       OR session_challenge_hash_nfc IS NULL
                       OR session_challenge_hash_nfc = ''
                       OR pg_catalog.octet_length(session_challenge_hash_nfc) > 128
                       OR authority_snapshot_id_nfc IS NULL
                       OR authority_snapshot_id_nfc = ''
                       OR pg_catalog.octet_length(authority_snapshot_id_nfc) > 128
                       OR media_type_nfc IS NULL
                       OR media_type_nfc = ''
                       OR pg_catalog.octet_length(media_type_nfc) > 128
                       OR key_selector_id_nfc IS NULL
                       OR key_selector_id_nfc = ''
                       OR pg_catalog.octet_length(key_selector_id_nfc) > 128
                       OR p_ingress_idempotency_key IS NULL
                       OR p_ingress_idempotency_key
                            !~ '^[0-9a-f]{12}4[0-9a-f]{3}[89ab][0-9a-f]{15}$' THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;

                    ingress_key := p_ingress_idempotency_key::uuid;

                    SELECT
                        session."ClientApplicationId",
                        artifact."VerificationSessionId",
                        acceptance."VerificationSessionId",
                        acceptance."ClientApplicationId",
                        acceptance."CaptureArtifactId",
                        acceptance."CaptureRevision",
                        acceptance."RawClass",
                        acceptance."SessionChallengeHash"
                    INTO
                        session_client_id,
                        artifact_session_id,
                        accepted_session_id,
                        accepted_client_id,
                        accepted_artifact_id,
                        accepted_revision,
                        accepted_raw_class,
                        accepted_challenge_hash
                    FROM tagekyc.verification_sessions AS session
                    JOIN tagekyc.capture_artifacts AS artifact
                      ON artifact."Id" = p_capture_artifact_id
                    JOIN tagekyc.raw_export_capture_acceptance_events AS acceptance
                      ON acceptance."CaptureAcceptanceId" = p_capture_acceptance_id
                    WHERE session."Id" = p_verification_session_id;

                    IF NOT FOUND
                       OR session_client_id IS DISTINCT FROM p_client_application_id
                       OR artifact_session_id IS DISTINCT FROM p_verification_session_id
                       OR accepted_session_id IS DISTINCT FROM p_verification_session_id
                       OR accepted_client_id IS DISTINCT FROM p_client_application_id
                       OR accepted_artifact_id IS DISTINCT FROM p_capture_artifact_id
                       OR accepted_revision IS DISTINCT FROM p_capture_revision
                       OR accepted_raw_class IS DISTINCT FROM raw_class_nfc
                       OR accepted_challenge_hash IS DISTINCT FROM session_challenge_hash_nfc THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;

                    canonical_preimage := ''::bytea;
                    value_bytes := pg_catalog.convert_to(
                        'tip-88c1-ingress-identity-v1',
                        'UTF8');
                    canonical_preimage := canonical_preimage
                        || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
                        || value_bytes;
                    FOREACH value_bytes IN ARRAY ARRAY[
                        pg_catalog.convert_to(
                            pg_catalog.replace(p_client_application_id::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(producer_id_nfc, 'UTF8'),
                        pg_catalog.convert_to(capture_agent_instance_id_nfc, 'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.replace(ingress_key::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.replace(p_authenticated_principal_id::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.replace(p_verification_session_id::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.replace(p_capture_acceptance_id::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.replace(p_capture_artifact_id::text, '-', ''),
                            'UTF8'),
                        pg_catalog.convert_to(p_capture_revision::text, 'UTF8'),
                        pg_catalog.convert_to(raw_class_nfc, 'UTF8'),
                        pg_catalog.convert_to(session_challenge_hash_nfc, 'UTF8'),
                        pg_catalog.convert_to(authority_snapshot_id_nfc, 'UTF8')
                    ]
                    LOOP
                        canonical_preimage := canonical_preimage
                            || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
                            || value_bytes;
                    END LOOP;
                    ingress_fingerprint := pg_catalog.sha256(canonical_preimage);

                    canonical_preimage := ''::bytea;
                    value_bytes := pg_catalog.convert_to(
                        'tip-88c1-producer-claim-envelope-v2',
                        'UTF8');
                    canonical_preimage := canonical_preimage
                        || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
                        || value_bytes;
                    FOREACH value_bytes IN ARRAY ARRAY[
                        pg_catalog.convert_to(
                            pg_catalog.encode(ingress_fingerprint, 'hex'),
                            'UTF8'),
                        pg_catalog.convert_to(p_claimed_plaintext_length::text, 'UTF8'),
                        pg_catalog.convert_to(media_type_nfc, 'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.to_char(
                                pg_catalog.date_trunc(
                                    'microseconds',
                                    p_captured_at_utc AT TIME ZONE 'UTC'),
                                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.to_char(
                                pg_catalog.date_trunc(
                                    'microseconds',
                                    p_plaintext_retention_started_at_utc
                                        AT TIME ZONE 'UTC'),
                                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                            'UTF8'),
                        pg_catalog.convert_to(
                            pg_catalog.to_char(
                                pg_catalog.date_trunc(
                                    'microseconds',
                                    p_plaintext_retention_expires_at_utc
                                        AT TIME ZONE 'UTC'),
                                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                            'UTF8'),
                        pg_catalog.convert_to(
                            p_plaintext_retention_budget_seconds::text,
                            'UTF8')
                    ]
                    LOOP
                        canonical_preimage := canonical_preimage
                            || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
                            || value_bytes;
                    END LOOP;
                    envelope_fingerprint := pg_catalog.sha256(canonical_preimage);

                    alias_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:alias:' ||
                        p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' ||
                        capture_agent_instance_id_nfc || ':' ||
                        ingress_key::text,
                        0);
                    exact_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:exact:' ||
                        p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' ||
                        p_verification_session_id::text || ':' ||
                        p_capture_artifact_id::text || ':' ||
                        p_capture_revision::text || ':' ||
                        raw_class_nfc,
                        0);
                    first_lock := LEAST(alias_lock, exact_lock);
                    second_lock := GREATEST(alias_lock, exact_lock);
                    lock_deadline := pg_catalog.clock_timestamp()
                        + pg_catalog.make_interval(
                            secs => p_idempotency_lock_timeout_milliseconds / 1000.0);

                    LOOP
                        acquired := pg_catalog.pg_try_advisory_xact_lock(first_lock);
                        EXIT WHEN acquired;
                        IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                            RAISE EXCEPTION USING
                                ERRCODE = '55P03',
                                MESSAGE = 'RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                        END IF;
                        PERFORM pg_catalog.pg_sleep(0.005);
                    END LOOP;

                    IF second_lock <> first_lock THEN
                        LOOP
                            acquired := pg_catalog.pg_try_advisory_xact_lock(second_lock);
                            EXIT WHEN acquired;
                            IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                                RAISE EXCEPTION USING
                                    ERRCODE = '55P03',
                                    MESSAGE = 'RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                            END IF;
                            PERFORM pg_catalog.pg_sleep(0.005);
                        END LOOP;
                    END IF;

                    SELECT alias.*
                    INTO alias_row
                    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
                    WHERE alias."ClientApplicationId" = p_client_application_id
                      AND alias."ProducerId" = producer_id_nfc
                      AND alias."CaptureAgentInstanceId" = capture_agent_instance_id_nfc
                      AND alias."IngressIdempotencyKey" = ingress_key;

                    issued_at := pg_catalog.date_trunc(
                        'microseconds',
                        pg_catalog.statement_timestamp());

                    IF FOUND THEN
                        IF alias_row."AliasState" = 'ConflictTombstone'
                           OR alias_row."AttemptedIngressIdentityFingerprint"
                                IS DISTINCT FROM ingress_fingerprint THEN
                            RAISE EXCEPTION USING
                                ERRCODE = 'P0001',
                                MESSAGE = 'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
                        END IF;

                        IF alias_row."ProducerClaimEnvelopeFingerprint"
                                IS DISTINCT FROM envelope_fingerprint THEN
                            RAISE EXCEPTION USING
                                ERRCODE = 'P0001',
                                MESSAGE = 'RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID';
                        END IF;

                        IF alias_row."CurrentClaimEvaluationDisposition" = 'Active'
                           AND alias_row."CurrentTokenExpiresAtUtc" > issued_at THEN
                            outcome_code :=
                                'RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS';
                            claim_evaluation_token := NULL;
                            token_variant := NULL;
                            token_expires_at_utc := NULL;
                            claim_evaluation_id := NULL;
                            claim_evaluation_revision := NULL;
                            claim_evaluation_fence := NULL;
                            retry_not_before_utc :=
                                alias_row."CurrentTokenExpiresAtUtc";
                            RETURN NEXT;
                            RETURN;
                        END IF;

                        evaluation_id := pg_catalog.gen_random_uuid();
                        evaluation_revision :=
                            alias_row."CurrentClaimEvaluationRevision" + 1;
                        evaluation_fence :=
                            alias_row."CurrentClaimEvaluationFence" + 1;
                        issued_variant := alias_row."CurrentTokenVariant";
                        expires_at := issued_at
                            + pg_catalog.make_interval(
                                secs => p_claim_evaluation_token_ttl_seconds);
                        evaluation_token_bytes := pg_catalog.sha256(
                            pg_catalog.uuid_send(pg_catalog.gen_random_uuid())
                            || pg_catalog.uuid_send(pg_catalog.gen_random_uuid()));
                        evaluation_token := pg_catalog.rtrim(
                            pg_catalog.translate(
                                pg_catalog.encode(evaluation_token_bytes, 'base64'),
                                '+/',
                                '-_'),
                            '=');
                        evaluation_token_digest :=
                            pg_catalog.sha256(evaluation_token_bytes);
                        previous_context := pg_catalog.current_setting(
                            'tagekyc.raw_export_source_ingress_write_context',
                            true);
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            'alias:UPDATE',
                            true);

                        BEGIN
                            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                            SET
                                "CurrentClaimEvaluationId" = evaluation_id,
                                "CurrentClaimEvaluationOwnerId" =
                                    p_claim_evaluation_owner_id,
                                "CurrentClaimEvaluationDisposition" = 'Active',
                                "CurrentTokenIssuedAtUtc" = issued_at,
                                "CurrentTokenExpiresAtUtc" = expires_at,
                                "CurrentTokenSchemaVersion" = 1,
                                "CurrentTokenVariant" = issued_variant,
                                "CurrentTokenAudience" =
                                    'tagekyc.raw-export-source-ingress-claim-comparison',
                                "CurrentTokenDigest" = evaluation_token_digest,
                                "CurrentClaimEvaluationRevision" =
                                    evaluation_revision,
                                "CurrentClaimEvaluationFence" = evaluation_fence,
                                "LatestIssuedTokenExpiresAtUtc" =
                                    GREATEST(
                                        "LatestIssuedTokenExpiresAtUtc",
                                        expires_at)
                            WHERE "IngressClaimAliasId" =
                                    alias_row."IngressClaimAliasId"
                              AND "CurrentClaimEvaluationId" =
                                    alias_row."CurrentClaimEvaluationId"
                              AND "CurrentClaimEvaluationRevision" =
                                    alias_row."CurrentClaimEvaluationRevision"
                              AND "CurrentClaimEvaluationFence" =
                                    alias_row."CurrentClaimEvaluationFence"
                              AND (
                                    "CurrentClaimEvaluationDisposition" <> 'Active'
                                    OR "CurrentTokenExpiresAtUtc" <= issued_at);
                            GET DIAGNOSTICS rows_changed = ROW_COUNT;
                        EXCEPTION WHEN OTHERS THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_source_ingress_write_context',
                                COALESCE(previous_context, ''),
                                true);
                            RAISE;
                        END;

                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            COALESCE(previous_context, ''),
                            true);

                        IF rows_changed <> 1 THEN
                            RAISE EXCEPTION USING
                                ERRCODE = 'P0001',
                                MESSAGE = 'RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED';
                        END IF;
                    ELSE
                        SELECT claim.*
                        INTO claim_row
                        FROM tagekyc.raw_export_source_ingress_claims AS claim
                        WHERE claim."ClientApplicationId" = p_client_application_id
                          AND claim."ProducerId" = producer_id_nfc
                          AND claim."VerificationSessionId" =
                                p_verification_session_id
                          AND claim."CaptureArtifactId" = p_capture_artifact_id
                          AND claim."CaptureRevision" = p_capture_revision
                          AND claim."RawClass" = raw_class_nfc;

                        IF FOUND THEN
                            IF claim_row."IngressIdentityFingerprint"
                                    IS DISTINCT FROM ingress_fingerprint THEN
                                RAISE EXCEPTION USING
                                    ERRCODE = 'P0001',
                                    MESSAGE = 'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
                            END IF;
                            issued_variant := 'ExistingClaimComparisonToken';
                        ELSE
                            new_claim := true;
                            claim_row."IngressClaimId" :=
                                pg_catalog.gen_random_uuid();
                            claim_row."CommitmentKeySelectorId" :=
                                key_selector_id_nfc;
                            claim_row."CommitmentKeySelectorVersion" :=
                                p_commitment_key_selector_version;
                            previous_context := pg_catalog.current_setting(
                                'tagekyc.raw_export_source_ingress_write_context',
                                true);
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_source_ingress_write_context',
                                'claim:INSERT',
                                true);

                            BEGIN
                                INSERT INTO tagekyc.raw_export_source_ingress_claims
                                    ("IngressClaimId",
                                     "VerificationSessionId",
                                     "CaptureAcceptanceId",
                                     "CaptureArtifactId",
                                     "ClientApplicationId",
                                     "AuthenticatedPrincipalId",
                                     "ProducerId",
                                     "CaptureAgentInstanceId",
                                     "CaptureRevision",
                                     "RawClass",
                                     "SessionChallengeHash",
                                     "AuthoritySnapshotId",
                                     "IngressIdentityFingerprint",
                                     "ClaimState",
                                     "CommitmentKeySelectorId",
                                     "CommitmentKeySelectorVersion",
                                     "CreatedAtUtc")
                                VALUES
                                    (claim_row."IngressClaimId",
                                     p_verification_session_id,
                                     p_capture_acceptance_id,
                                     p_capture_artifact_id,
                                     p_client_application_id,
                                     p_authenticated_principal_id,
                                     producer_id_nfc,
                                     capture_agent_instance_id_nfc,
                                     p_capture_revision,
                                     raw_class_nfc,
                                     session_challenge_hash_nfc,
                                     authority_snapshot_id_nfc,
                                     ingress_fingerprint,
                                     'ClaimEvaluating',
                                     key_selector_id_nfc,
                                     p_commitment_key_selector_version,
                                     issued_at);
                            EXCEPTION WHEN OTHERS THEN
                                PERFORM pg_catalog.set_config(
                                    'tagekyc.raw_export_source_ingress_write_context',
                                    COALESCE(previous_context, ''),
                                    true);
                                RAISE;
                            END;

                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_source_ingress_write_context',
                                COALESCE(previous_context, ''),
                                true);
                            issued_variant := 'NewClaimEvaluationToken';
                        END IF;

                        evaluation_id := pg_catalog.gen_random_uuid();
                        evaluation_revision := 1;
                        evaluation_fence := 1;
                        expires_at := issued_at
                            + pg_catalog.make_interval(
                                secs => p_claim_evaluation_token_ttl_seconds);
                        evaluation_token_bytes := pg_catalog.sha256(
                            pg_catalog.uuid_send(pg_catalog.gen_random_uuid())
                            || pg_catalog.uuid_send(pg_catalog.gen_random_uuid()));
                        evaluation_token := pg_catalog.rtrim(
                            pg_catalog.translate(
                                pg_catalog.encode(evaluation_token_bytes, 'base64'),
                                '+/',
                                '-_'),
                            '=');
                        evaluation_token_digest :=
                            pg_catalog.sha256(evaluation_token_bytes);
                        previous_context := pg_catalog.current_setting(
                            'tagekyc.raw_export_source_ingress_write_context',
                            true);
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            'alias:INSERT',
                            true);

                        BEGIN
                            INSERT INTO tagekyc.raw_export_source_ingress_claim_aliases
                                ("IngressClaimAliasId",
                                 "ClientApplicationId",
                                 "ProducerId",
                                 "CaptureAgentInstanceId",
                                 "IngressIdempotencyKey",
                                 "AttemptedIngressIdentityFingerprint",
                                 "ProducerClaimEnvelopeFingerprint",
                                 "AliasState",
                                 "IngressClaimId",
                                 "CurrentClaimEvaluationId",
                                 "CurrentClaimEvaluationOwnerId",
                                 "CurrentClaimEvaluationDisposition",
                                 "CurrentTokenIssuedAtUtc",
                                 "CurrentTokenExpiresAtUtc",
                                 "CurrentTokenSchemaVersion",
                                 "CurrentTokenVariant",
                                 "CurrentTokenAudience",
                                 "CurrentTokenDigest",
                                 "CurrentClaimEvaluationRevision",
                                 "CurrentClaimEvaluationFence",
                                 "LatestIssuedTokenExpiresAtUtc",
                                 "CreatedAtUtc")
                            VALUES
                                (pg_catalog.gen_random_uuid(),
                                 p_client_application_id,
                                 producer_id_nfc,
                                 capture_agent_instance_id_nfc,
                                 ingress_key,
                                 ingress_fingerprint,
                                 envelope_fingerprint,
                                 'Evaluating',
                                 claim_row."IngressClaimId",
                                 evaluation_id,
                                 p_claim_evaluation_owner_id,
                                 'Active',
                                 issued_at,
                                 expires_at,
                                 1,
                                 issued_variant,
                                 'tagekyc.raw-export-source-ingress-claim-comparison',
                                 evaluation_token_digest,
                                 evaluation_revision,
                                 evaluation_fence,
                                 expires_at,
                                 issued_at);
                        EXCEPTION WHEN OTHERS THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_source_ingress_write_context',
                                COALESCE(previous_context, ''),
                                true);
                            RAISE;
                        END;

                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            COALESCE(previous_context, ''),
                            true);
                    END IF;

                    outcome_code := NULL;
                    claim_evaluation_token := evaluation_token;
                    token_variant := issued_variant;
                    token_expires_at_utc := expires_at;
                    claim_evaluation_id := evaluation_id;
                    claim_evaluation_revision := evaluation_revision;
                    claim_evaluation_fence := evaluation_fence;
                    retry_not_before_utc := NULL;
                    RETURN NEXT;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    tagekyc.validate_raw_export_claim_evaluation_token(
                        p_client_application_id uuid,
                        p_producer_id text,
                        p_capture_agent_instance_id text,
                        p_ingress_idempotency_key text,
                        p_claim_evaluation_id uuid,
                        p_claim_evaluation_revision bigint,
                        p_claim_evaluation_fence bigint,
                        p_token_variant text,
                        p_token_expires_at_utc timestamp with time zone,
                        p_claim_evaluation_token text)
                RETURNS boolean
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    ingress_key uuid;
                    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
                    claim_principal_id uuid;
                    token_bytes bytea;
                    presented_digest bytea;
                    difference integer := 0;
                    index integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();

                    IF p_ingress_idempotency_key IS NULL
                       OR p_ingress_idempotency_key
                            !~ '^[0-9a-f]{12}4[0-9a-f]{3}[89ab][0-9a-f]{15}$'
                       OR p_claim_evaluation_token IS NULL
                       OR p_claim_evaluation_token
                            !~ '^[A-Za-z0-9_-]{43}$' THEN
                        RETURN false;
                    END IF;

                    ingress_key := p_ingress_idempotency_key::uuid;

                    SELECT alias.*
                    INTO alias_row
                    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
                    WHERE alias."ClientApplicationId" = p_client_application_id
                      AND alias."ProducerId" =
                            pg_catalog.normalize(p_producer_id, 'NFC')
                      AND alias."CaptureAgentInstanceId" =
                            pg_catalog.normalize(p_capture_agent_instance_id, 'NFC')
                      AND alias."IngressIdempotencyKey" = ingress_key;

                    IF NOT FOUND THEN
                        RETURN false;
                    END IF;

                    SELECT claim."AuthenticatedPrincipalId"
                    INTO claim_principal_id
                    FROM tagekyc.raw_export_source_ingress_claims AS claim
                    WHERE claim."IngressClaimId" = alias_row."IngressClaimId";

                    IF NOT FOUND
                       OR actor_id IS DISTINCT FROM claim_principal_id
                       OR alias_row."AliasState" = 'ConflictTombstone'
                       OR alias_row."CurrentClaimEvaluationDisposition" <> 'Active'
                       OR alias_row."CurrentTokenSchemaVersion" <> 1
                       OR alias_row."CurrentTokenAudience" <>
                            'tagekyc.raw-export-source-ingress-claim-comparison'
                       OR alias_row."CurrentTokenVariant"
                            IS DISTINCT FROM p_token_variant
                       OR alias_row."CurrentClaimEvaluationId"
                            IS DISTINCT FROM p_claim_evaluation_id
                       OR alias_row."CurrentClaimEvaluationRevision"
                            IS DISTINCT FROM p_claim_evaluation_revision
                       OR alias_row."CurrentClaimEvaluationFence"
                            IS DISTINCT FROM p_claim_evaluation_fence
                       OR alias_row."CurrentTokenExpiresAtUtc"
                            IS DISTINCT FROM p_token_expires_at_utc
                       OR alias_row."CurrentTokenExpiresAtUtc"
                            <= pg_catalog.statement_timestamp() THEN
                        RETURN false;
                    END IF;

                    BEGIN
                        token_bytes := pg_catalog.decode(
                            pg_catalog.translate(
                                p_claim_evaluation_token,
                                '-_',
                                '+/') || '=',
                            'base64');
                    EXCEPTION WHEN OTHERS THEN
                        RETURN false;
                    END;

                    IF pg_catalog.octet_length(token_bytes) <> 32
                       OR pg_catalog.octet_length(
                            alias_row."CurrentTokenDigest") <> 32 THEN
                        RETURN false;
                    END IF;

                    presented_digest := pg_catalog.sha256(token_bytes);
                    FOR index IN 0..31 LOOP
                        difference := difference
                            | (
                                pg_catalog.get_byte(presented_digest, index)
                                # pg_catalog.get_byte(
                                    alias_row."CurrentTokenDigest",
                                    index));
                    END LOOP;

                    RETURN difference = 0;
                END;
                $$;

                ALTER FUNCTION tagekyc.enforce_raw_export_source_ingress_write()
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.begin_raw_export_source_ingress_claim(
                    uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,text,
                    bigint,text,timestamp with time zone,timestamp with time zone,
                    timestamp with time zone,integer,text,integer,uuid,integer,integer)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.validate_raw_export_claim_evaluation_token(
                    uuid,text,text,text,uuid,bigint,bigint,text,
                    timestamp with time zone,text)
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.enforce_raw_export_source_ingress_write(),
                    tagekyc.begin_raw_export_source_ingress_claim(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,text,
                        bigint,text,timestamp with time zone,timestamp with time zone,
                        timestamp with time zone,integer,text,integer,uuid,integer,integer),
                    tagekyc.validate_raw_export_claim_evaluation_token(
                        uuid,text,text,text,uuid,bigint,bigint,text,
                        timestamp with time zone,text)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.begin_raw_export_source_ingress_claim(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,text,
                        bigint,text,timestamp with time zone,timestamp with time zone,
                        timestamp with time zone,integer,text,integer,uuid,integer,integer),
                    tagekyc.validate_raw_export_claim_evaluation_token(
                        uuid,text,text,text,uuid,bigint,bigint,text,
                        timestamp with time zone,text)
                TO tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS
                    tagekyc.validate_raw_export_claim_evaluation_token(
                        uuid,text,text,text,uuid,bigint,bigint,text,
                        timestamp with time zone,text);
                DROP FUNCTION IF EXISTS
                    tagekyc.begin_raw_export_source_ingress_claim(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,text,
                        bigint,text,timestamp with time zone,timestamp with time zone,
                        timestamp with time zone,integer,text,integer,uuid,integer,integer);

                DROP TRIGGER IF EXISTS
                    tr_raw_export_source_ingress_aliases_write_guard
                    ON tagekyc.raw_export_source_ingress_claim_aliases;
                DROP TRIGGER IF EXISTS
                    tr_raw_export_source_ingress_claims_write_guard
                    ON tagekyc.raw_export_source_ingress_claims;

                DROP FUNCTION IF EXISTS
                    tagekyc.enforce_raw_export_source_ingress_write();
                """);

            migrationBuilder.DropTable(
                name: "raw_export_source_ingress_claim_aliases",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_source_ingress_claims",
                schema: "tagekyc");
        }
    }
}
