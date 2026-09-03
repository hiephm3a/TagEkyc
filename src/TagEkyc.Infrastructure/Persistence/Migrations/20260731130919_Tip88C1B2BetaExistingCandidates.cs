using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2BetaExistingCandidates : Migration
    {
        private const string LandedB1BeginFunction =
            """
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
            """;

        private static readonly string BeginFunctionWithImmutableIdentityComparison =
            LandedB1BeginFunction.Replace(
            """
                            IF claim_row."IngressIdentityFingerprint"
                                    IS DISTINCT FROM ingress_fingerprint THEN
                                RAISE EXCEPTION USING
                                    ERRCODE = 'P0001',
                                    MESSAGE = 'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
                            END IF;
            """,
            """
                            IF claim_row."ClientApplicationId"
                                    IS DISTINCT FROM p_client_application_id
                               OR claim_row."AuthenticatedPrincipalId"
                                    IS DISTINCT FROM p_authenticated_principal_id
                               OR claim_row."ProducerId"
                                    IS DISTINCT FROM producer_id_nfc
                               OR claim_row."CaptureAgentInstanceId"
                                    IS DISTINCT FROM capture_agent_instance_id_nfc
                               OR claim_row."VerificationSessionId"
                                    IS DISTINCT FROM p_verification_session_id
                               OR claim_row."CaptureAcceptanceId"
                                    IS DISTINCT FROM p_capture_acceptance_id
                               OR claim_row."CaptureArtifactId"
                                    IS DISTINCT FROM p_capture_artifact_id
                               OR claim_row."CaptureRevision"
                                    IS DISTINCT FROM p_capture_revision
                               OR claim_row."RawClass"
                                    IS DISTINCT FROM raw_class_nfc
                               OR claim_row."SessionChallengeHash"
                                    IS DISTINCT FROM session_challenge_hash_nfc
                               OR claim_row."AuthoritySnapshotId"
                                    IS DISTINCT FROM authority_snapshot_id_nfc THEN
                                RAISE EXCEPTION USING
                                    ERRCODE = 'P0001',
                                    MESSAGE = 'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
                            END IF;
            """,
                StringComparison.Ordinal);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BeginFunctionWithImmutableIdentityComparison);

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
                    canonical_reservation tagekyc.raw_export_source_reservations%ROWTYPE;
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

                    IF p_token_variant = 'NewClaimEvaluationToken' THEN
                        IF alias_row."AliasState" <> 'Evaluating'
                           OR claim_row."ClaimState" <> 'ClaimEvaluating'
                           OR alias_row."ProducerClaimEnvelopeFingerprint"
                                IS DISTINCT FROM p_producer_envelope_fingerprint
                           OR p_content_commitment IS NULL THEN
                            RETURN QUERY SELECT
                                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
                            RETURN;
                        END IF;
                    ELSIF p_token_variant = 'ExistingClaimComparisonToken' THEN
                        IF alias_row."AliasState" NOT IN ('Evaluating','Bound')
                           OR claim_row."ClaimState" <> 'Reserved'
                           OR alias_row."ProducerClaimEnvelopeFingerprint"
                                IS DISTINCT FROM p_producer_envelope_fingerprint THEN
                            RETURN QUERY SELECT
                                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
                            RETURN;
                        END IF;
                    ELSE
                        RETURN QUERY SELECT
                            'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
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

                    IF p_token_variant = 'ExistingClaimComparisonToken'
                       AND p_content_commitment IS NULL THEN
                        RETURN QUERY SELECT
                            'RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE'::text,
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
                    IF p_token_variant = 'ExistingClaimComparisonToken' THEN
                        SELECT reservation.* INTO canonical_reservation
                        FROM tagekyc.raw_export_source_reservations AS reservation
                        WHERE reservation."IngressClaimId" =
                                claim_row."IngressClaimId";

                        IF NOT FOUND THEN
                            RETURN QUERY SELECT
                                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
                            RETURN;
                        END IF;

                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_source_ingress_write_context',
                            'alias:UPDATE',
                            true);

                        IF canonical_reservation."AdmissionFingerprint"
                                IS NOT DISTINCT FROM admission THEN
                            IF alias_row."AliasState" = 'Evaluating' THEN
                                UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                                SET "AliasState" = 'Bound'
                                WHERE "IngressClaimAliasId" =
                                        alias_row."IngressClaimAliasId"
                                  AND "AliasState" = 'Evaluating'
                                  AND "CurrentClaimEvaluationDisposition" = 'Active'
                                  AND "CurrentClaimEvaluationRevision" =
                                        p_claim_evaluation_revision
                                  AND "CurrentClaimEvaluationFence" =
                                        p_claim_evaluation_fence;
                                IF NOT FOUND THEN
                                    RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                                END IF;
                            END IF;

                            RETURN QUERY SELECT
                                'ExistingMatch'::text,
                                canonical_reservation."SourceArtifactId";
                            RETURN;
                        END IF;

                        IF alias_row."AliasState" = 'Evaluating' THEN
                            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                            SET "AliasState" = 'ConflictTombstone',
                                "CurrentClaimEvaluationDisposition" = 'Conflict'
                            WHERE "IngressClaimAliasId" =
                                    alias_row."IngressClaimAliasId"
                              AND "AliasState" = 'Evaluating'
                              AND "CurrentClaimEvaluationDisposition" = 'Active'
                              AND "CurrentClaimEvaluationRevision" =
                                    p_claim_evaluation_revision
                              AND "CurrentClaimEvaluationFence" =
                                    p_claim_evaluation_fence;
                        ELSE
                            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                            SET "CurrentClaimEvaluationDisposition" = 'Conflict'
                            WHERE "IngressClaimAliasId" =
                                    alias_row."IngressClaimAliasId"
                              AND "AliasState" = 'Bound'
                              AND "CurrentClaimEvaluationDisposition" = 'Active'
                              AND "CurrentClaimEvaluationRevision" =
                                    p_claim_evaluation_revision
                              AND "CurrentClaimEvaluationFence" =
                                    p_claim_evaluation_fence;
                        END IF;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                        END IF;

                        RETURN QUERY SELECT
                            'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT'::text,
                            NULL::uuid;
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(LandedB1BeginFunction);

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
                """);
        }
    }
}
