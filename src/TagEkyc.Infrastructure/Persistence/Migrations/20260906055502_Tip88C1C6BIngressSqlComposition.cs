using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    public partial class Tip88C1C6BIngressSqlComposition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "R2TerminalOutcomeCode",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_attempt_r2_terminal_outcome",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                sql: "\"R2TerminalOutcomeCode\" IS NULL OR (\"R2TerminationDisposition\" IN ('Terminated','TerminatedBeforeStart') AND \"R2TerminatedAtUtc\" IS NOT NULL AND \"R2TerminalOutcomeCode\" IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED'))");

            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_begin_source_ingress_with_authority_core(
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
                    p_claimed_plaintext_length bigint,
                    p_media_type text,
                    p_captured_at_utc timestamptz,
                    p_plaintext_retention_started_at_utc timestamptz,
                    p_plaintext_retention_expires_at_utc timestamptz,
                    p_plaintext_retention_budget_seconds integer,
                    p_commitment_key_selector_id text,
                    p_commitment_key_selector_version integer,
                    p_claim_evaluation_owner_id uuid,
                    p_claim_evaluation_token_ttl_seconds integer,
                    p_idempotency_lock_timeout_milliseconds integer,
                    p_required_policy_mode text)
                RETURNS TABLE(
                    outcome_code text, claim_evaluation_token text,
                    token_variant text, token_expires_at_utc timestamptz,
                    claim_evaluation_id uuid, claim_evaluation_revision bigint,
                    claim_evaluation_fence bigint,
                    retry_not_before_utc timestamptz)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    producer_id_nfc text := pg_catalog.normalize(p_producer_id, 'NFC');
                    agent_id_nfc text := pg_catalog.normalize(p_capture_agent_instance_id, 'NFC');
                    raw_class_nfc text := pg_catalog.normalize(p_raw_class, 'NFC');
                    ingress_key uuid;
                    alias_lock bigint;
                    exact_lock bigint;
                    first_lock bigint;
                    second_lock bigint;
                    lock_deadline timestamptz;
                    acquired boolean;
                    existing_snapshot_id text;
                    decision_row record;
                    policy_row tagekyc.raw_export_policy_versions%ROWTYPE;
                    snapshot_row record;
                    now_utc timestamptz := pg_catalog.transaction_timestamp();
                BEGIN
                    IF p_ingress_idempotency_key IS NULL OR
                       p_ingress_idempotency_key !~ '^[0-9a-f]{12}4[0-9a-f]{3}[89ab][0-9a-f]{15}$' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;
                    ingress_key := p_ingress_idempotency_key::uuid;
                    alias_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:alias:' || p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' || agent_id_nfc || ':' || ingress_key::text, 0);
                    exact_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:exact:' || p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' || p_verification_session_id::text || ':' ||
                        p_capture_artifact_id::text || ':' || p_capture_revision::text || ':' ||
                        raw_class_nfc, 0);
                    first_lock := LEAST(alias_lock, exact_lock);
                    second_lock := GREATEST(alias_lock, exact_lock);
                    lock_deadline := pg_catalog.clock_timestamp() +
                        pg_catalog.make_interval(
                            secs => p_idempotency_lock_timeout_milliseconds / 1000.0);
                    LOOP
                        acquired := pg_catalog.pg_try_advisory_xact_lock(first_lock);
                        EXIT WHEN acquired;
                        IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                            RAISE EXCEPTION USING ERRCODE='55P03',
                                MESSAGE='RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                        END IF;
                        PERFORM pg_catalog.pg_sleep(0.005);
                    END LOOP;
                    IF second_lock <> first_lock THEN
                        LOOP
                            acquired := pg_catalog.pg_try_advisory_xact_lock(second_lock);
                            EXIT WHEN acquired;
                            IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                                RAISE EXCEPTION USING ERRCODE='55P03',
                                    MESSAGE='RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                            END IF;
                            PERFORM pg_catalog.pg_sleep(0.005);
                        END LOOP;
                    END IF;

                    SELECT claim."AuthoritySnapshotId"
                    INTO existing_snapshot_id
                    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
                    JOIN tagekyc.raw_export_source_ingress_claims AS claim
                      ON claim."IngressClaimId" = alias."IngressClaimId"
                    WHERE alias."ClientApplicationId" = p_client_application_id
                      AND alias."ProducerId" = producer_id_nfc
                      AND alias."CaptureAgentInstanceId" = agent_id_nfc
                      AND alias."IngressIdempotencyKey" = ingress_key;
                    IF NOT FOUND THEN
                        SELECT claim."AuthoritySnapshotId"
                        INTO existing_snapshot_id
                        FROM tagekyc.raw_export_source_ingress_claims AS claim
                        WHERE claim."ClientApplicationId" = p_client_application_id
                          AND claim."ProducerId" = producer_id_nfc
                          AND claim."VerificationSessionId" = p_verification_session_id
                          AND claim."CaptureArtifactId" = p_capture_artifact_id
                          AND claim."CaptureRevision" = p_capture_revision
                          AND claim."RawClass" = raw_class_nfc;
                    END IF;

                    IF existing_snapshot_id IS NULL THEN
                        SELECT decision.*, permit."PermitId",
                               permit."DecisionExpiresAtUtc" AS permit_expires_at
                        INTO decision_row
                        FROM tagekyc.raw_export_authorization_decisions AS decision
                        JOIN tagekyc.raw_export_authorization_permits AS permit
                          ON permit."AuthorizationDecisionId" = decision."ExportDecisionId"
                        JOIN tagekyc.raw_export_permit_classes AS permit_class
                          ON permit_class."PermitId" = permit."PermitId"
                        WHERE decision."Outcome" = 'Authorized'
                          AND decision."PrincipalId" = p_authenticated_principal_id
                          AND decision."ClientApplicationId" = p_client_application_id
                          AND decision."ResolvedVerificationSessionId" = p_verification_session_id
                          AND permit_class."RawClass" = raw_class_nfc
                          AND permit."DecisionExpiresAtUtc" > now_utc
                        ORDER BY decision."DecidedAtUtc" DESC
                        LIMIT 1;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        SELECT * INTO policy_row
                        FROM tagekyc.raw_export_policy_versions AS policy
                        WHERE policy."PolicyId" = decision_row."PolicyId"
                          AND policy."PolicyVersion" = decision_row."PolicyVersion"
                          AND policy."Mode" = p_required_policy_mode
                          AND policy."RetentionProfileRef" IS NOT NULL;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        IF EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_read_authorization_eligibility_inputs(
                                p_authenticated_principal_id,
                                policy_row."PolicyId",
                                policy_row."PolicyVersion") AS eligibility
                            WHERE NOT eligibility."PolicyExists"
                               OR eligibility."ClosureType" <> 'CatalogApproved'
                               OR eligibility."BoundRuleSetVersion" <>
                                    eligibility."CurrentRuleSetVersion"
                               OR eligibility."GrantEventType" <> 'Granted'
                               OR eligibility."LifecycleEventType" <> 'Activated'
                               OR (eligibility."RequirementType" IS NOT NULL AND (
                                    eligibility."FulfillmentEventType" <> 'Accepted'
                                    OR eligibility."ValidFromUtc" > now_utc
                                    OR (eligibility."ValidUntilUtc" IS NOT NULL AND
                                        eligibility."ValidUntilUtc" <= now_utc)))) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        IF NOT EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
                                p_verification_session_id,
                                policy_row."PolicyId",
                                policy_row."PolicyVersion") AS consent
                            WHERE consent."RawClass" = raw_class_nfc
                              AND consent."State" = 'Effective'
                              AND consent."PurposeCode" = 'SubjectRawBiometricExport'
                              AND consent."RecipientClientApplicationId" =
                                    p_client_application_id
                              AND consent."ValidFromUtc" <= now_utc
                              AND (consent."ValidUntilUtc" IS NULL OR
                                   consent."ValidUntilUtc" > now_utc)) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        SELECT * INTO snapshot_row
                        FROM tagekyc.raw_export_append_authority_snapshot(
                            p_client_application_id,
                            p_verification_session_id,
                            p_capture_acceptance_id,
                            raw_class_nfc,
                            decision_row."ExportDecisionId",
                            decision_row."GrantRevision",
                            policy_row."ControllerEntityRef",
                            p_client_application_id::text || ':' ||
                                p_verification_session_id::text || ':' || raw_class_nfc,
                            policy_row."RetentionProfileRef",
                            policy_row."PolicyVersion",
                            policy_row."PolicyId",
                            policy_row."PolicyVersion",
                            policy_row."Mode",
                            'CaptureAccepted',
                            LEAST(p_plaintext_retention_expires_at_utc,
                                  decision_row.permit_expires_at),
                            'PolicyGrantLifecycle',
                            policy_row."RetentionProfileRef",
                            'PolicyLegalHold',
                            now_utc,
                            decision_row.permit_expires_at);
                        existing_snapshot_id := snapshot_row."AuthoritySnapshotId"::text;
                    END IF;

                    RETURN QUERY
                    SELECT * FROM tagekyc.begin_raw_export_source_ingress_claim(
                        p_authenticated_principal_id,p_client_application_id,
                        p_producer_id,p_capture_agent_instance_id,
                        p_ingress_idempotency_key,p_verification_session_id,
                        p_capture_acceptance_id,p_capture_artifact_id,
                        p_capture_revision,p_raw_class,p_session_challenge_hash,
                        existing_snapshot_id,p_claimed_plaintext_length,p_media_type,
                        p_captured_at_utc,p_plaintext_retention_started_at_utc,
                        p_plaintext_retention_expires_at_utc,
                        p_plaintext_retention_budget_seconds,
                        p_commitment_key_selector_id,
                        p_commitment_key_selector_version,
                        p_claim_evaluation_owner_id,
                        p_claim_evaluation_token_ttl_seconds,
                        p_idempotency_lock_timeout_milliseconds);
                END;
                $function$;
CREATE OR REPLACE FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(
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
                    p_claimed_plaintext_length bigint,
                    p_media_type text,
                    p_captured_at_utc timestamptz,
                    p_plaintext_retention_started_at_utc timestamptz,
                    p_plaintext_retention_expires_at_utc timestamptz,
                    p_plaintext_retention_budget_seconds integer,
                    p_commitment_key_selector_id text,
                    p_commitment_key_selector_version integer,
                    p_claim_evaluation_owner_id uuid,
                    p_claim_evaluation_token_ttl_seconds integer,
                    p_idempotency_lock_timeout_milliseconds integer)
                RETURNS TABLE(outcome_code text, claim_evaluation_token text, token_variant text, token_expires_at_utc timestamptz, claim_evaluation_id uuid, claim_evaluation_revision bigint, claim_evaluation_fence bigint, retry_not_before_utc timestamptz)
                LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog AS $wrapper$
                    SELECT * FROM tagekyc.raw_export_begin_source_ingress_with_authority_core(p_authenticated_principal_id,p_client_application_id,p_producer_id,p_capture_agent_instance_id,p_ingress_idempotency_key,p_verification_session_id,p_capture_acceptance_id,p_capture_artifact_id,p_capture_revision,p_raw_class,p_session_challenge_hash,p_claimed_plaintext_length,p_media_type,p_captured_at_utc,p_plaintext_retention_started_at_utc,p_plaintext_retention_expires_at_utc,p_plaintext_retention_budget_seconds,p_commitment_key_selector_id,p_commitment_key_selector_version,p_claim_evaluation_owner_id,p_claim_evaluation_token_ttl_seconds,p_idempotency_lock_timeout_milliseconds, 'EncryptedExportPacket');
                $wrapper$;
CREATE FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(
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
                    p_claimed_plaintext_length bigint,
                    p_media_type text,
                    p_captured_at_utc timestamptz,
                    p_plaintext_retention_started_at_utc timestamptz,
                    p_plaintext_retention_expires_at_utc timestamptz,
                    p_plaintext_retention_budget_seconds integer,
                    p_commitment_key_selector_id text,
                    p_commitment_key_selector_version integer,
                    p_claim_evaluation_owner_id uuid,
                    p_claim_evaluation_token_ttl_seconds integer,
                    p_idempotency_lock_timeout_milliseconds integer)
                RETURNS TABLE(outcome_code text, claim_evaluation_token text, token_variant text, token_expires_at_utc timestamptz, claim_evaluation_id uuid, claim_evaluation_revision bigint, claim_evaluation_fence bigint, retry_not_before_utc timestamptz)
                LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog AS $wrapper$
                    SELECT * FROM tagekyc.raw_export_begin_source_ingress_with_authority_core(p_authenticated_principal_id,p_client_application_id,p_producer_id,p_capture_agent_instance_id,p_ingress_idempotency_key,p_verification_session_id,p_capture_acceptance_id,p_capture_artifact_id,p_capture_revision,p_raw_class,p_session_challenge_hash,p_claimed_plaintext_length,p_media_type,p_captured_at_utc,p_plaintext_retention_started_at_utc,p_plaintext_retention_expires_at_utc,p_plaintext_retention_budget_seconds,p_commitment_key_selector_id,p_commitment_key_selector_version,p_claim_evaluation_owner_id,p_claim_evaluation_token_ttl_seconds,p_idempotency_lock_timeout_milliseconds, 'EncryptedRawVaultRetained');
                $wrapper$;
CREATE FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
 p_client_application_id uuid,p_producer_id text,p_capture_agent_instance_id text,p_ingress_idempotency_key text,
 p_claim_evaluation_id uuid,p_claim_evaluation_revision bigint,p_claim_evaluation_fence bigint,p_token_variant text,
 p_token_expires_at_utc timestamptz,p_claim_evaluation_token text,p_producer_envelope_fingerprint bytea,
 p_commitment_schema integer,p_commitment_key_id text,p_commitment_key_version integer,p_content_commitment bytea,
 p_subject_token_schema integer,p_subject_token_key_id text,p_subject_token_key_version integer,p_subject_token bytea,
 p_claimed_plaintext_length bigint,p_media_type text,p_captured_at_utc timestamptz,p_retention_started_at_utc timestamptz,
 p_retention_expires_at_utc timestamptz,p_retention_budget_seconds integer,p_storage_profile_id text,p_source_profile_id text,
 p_source_profile_version integer,p_encryption_suite_id text,p_encryption_framing_version integer,p_nonce_strategy_id text,
 p_nonce_seed_commitment bytea,p_chunk_size integer,p_framing_parameters_digest bytea,p_key_provider_id text,p_kek_id text,
 p_kek_version integer,p_kek_fingerprint text,p_max_continuation_seconds integer,p_attempt_deadline_seconds integer,
 p_safety_margin_milliseconds integer,p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text,"SourceArtifactId" uuid,"AttemptKeyReservationId" uuid,"AttemptId" uuid,
 "ExpectedEncryptionAttemptRevision" bigint,"ExpectedFence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $handoff$
DECLARE completed record; head_row tagekyc.raw_export_source_head%ROWTYPE;
 attempt_row tagekyc.raw_export_source_encryption_attempts%ROWTYPE; publication_row tagekyc.raw_export_source_publications%ROWTYPE;
 provisional_row tagekyc.raw_export_provisional_objects%ROWTYPE;
BEGIN
 SELECT * INTO completed FROM tagekyc.complete_raw_export_source_ingress_claim(
  p_client_application_id,p_producer_id,p_capture_agent_instance_id,p_ingress_idempotency_key,
  p_claim_evaluation_id,p_claim_evaluation_revision,p_claim_evaluation_fence,p_token_variant,p_token_expires_at_utc,
  p_claim_evaluation_token,p_producer_envelope_fingerprint,p_commitment_schema,p_commitment_key_id,p_commitment_key_version,
  p_content_commitment,p_subject_token_schema,p_subject_token_key_id,p_subject_token_key_version,p_subject_token,
  p_claimed_plaintext_length,p_media_type,p_captured_at_utc,p_retention_started_at_utc,p_retention_expires_at_utc,
  p_retention_budget_seconds,p_storage_profile_id,p_source_profile_id,p_source_profile_version,p_encryption_suite_id,
  p_encryption_framing_version,p_nonce_strategy_id,p_nonce_seed_commitment,p_chunk_size,p_framing_parameters_digest,
  p_key_provider_id,p_kek_id,p_kek_version,p_kek_fingerprint,p_max_continuation_seconds,p_attempt_deadline_seconds,
  p_safety_margin_milliseconds,p_ownership_lease_seconds);
 IF completed."OutcomeCode" NOT IN ('NewReservation','ExistingMatch') THEN
  RETURN QUERY SELECT completed."OutcomeCode",completed."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF completed."SourceArtifactId" IS NULL THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
 SELECT * INTO STRICT head_row FROM tagekyc.raw_export_source_head h
  WHERE h."SourceArtifactId"=completed."SourceArtifactId" FOR UPDATE;
 SELECT * INTO STRICT attempt_row FROM tagekyc.raw_export_source_encryption_attempts a
  WHERE a."AttemptId"=head_row."CurrentEncryptionAttemptId" AND a."SourceArtifactId"=head_row."SourceArtifactId" FOR UPDATE;
 SELECT * INTO publication_row FROM tagekyc.raw_export_source_publications p
  WHERE p."SourceArtifactId"=head_row."SourceArtifactId" ORDER BY p."PublicationRevision" DESC LIMIT 1 FOR UPDATE;
 SELECT * INTO provisional_row FROM tagekyc.raw_export_provisional_objects o
  WHERE o."SourceArtifactId"=head_row."SourceArtifactId" AND o."AttemptId"=attempt_row."AttemptId"
  ORDER BY o."StateRevision" DESC LIMIT 1 FOR UPDATE;
 IF completed."OutcomeCode"='NewReservation' THEN
  IF head_row."CustodyState"<>'Reserved' OR attempt_row."R2TerminationDisposition" IS NOT NULL
   OR attempt_row."R2TerminalOutcomeCode" IS NOT NULL OR attempt_row."EncryptionAttemptRevision"<>head_row."ReservationRevision"
   OR attempt_row."Fence"<>head_row."Fence" THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
  RETURN QUERY SELECT 'NewReservation'::text,head_row."SourceArtifactId",attempt_row."AttemptKeyReservationId",
   attempt_row."AttemptId",attempt_row."EncryptionAttemptRevision",attempt_row."Fence"; RETURN;
 END IF;
 IF publication_row."PublicationState"='Available' THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_ALREADY_AVAILABLE'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF head_row."CustodyState"='Staged' OR publication_row."PublicationState"='Committed'
    OR attempt_row."StagedAtUtc" IS NOT NULL OR provisional_row."State" IN ('ObjectPresentPendingVerification','VerifiedCompleted') THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESUME_PENDING'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF attempt_row."R2TerminationDisposition" IS NULL OR attempt_row."R2TerminalOutcomeCode" IS NULL THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESERVATION_BUSY'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 RETURN QUERY SELECT attempt_row."R2TerminalOutcomeCode",head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint;
END;$handoff$;
CREATE FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,
 p_operational_disposition text,p_terminal_outcome_code text)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $terminal$
BEGIN
 IF p_operational_disposition NOT IN ('Terminated','TerminatedBeforeStart') OR
    p_terminal_outcome_code NOT IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
 THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_TERMINAL_OUTCOME_INVALID'; END IF;
 UPDATE tagekyc.raw_export_source_encryption_attempts a SET
  "R2TerminationDisposition"=p_operational_disposition,"R2TerminatedAtUtc"=pg_catalog.statement_timestamp(),
  "R2TerminalOutcomeCode"=p_terminal_outcome_code
 FROM tagekyc.raw_export_source_head h
 WHERE a."AttemptId"=p_attempt_id AND a."SourceArtifactId"=p_source_artifact_id
  AND a."EncryptionAttemptRevision"=p_expected_revision AND a."Fence"=p_expected_fence
  AND a."R2TerminationDisposition" IS NULL AND a."R2TerminalOutcomeCode" IS NULL
  AND h."SourceArtifactId"=a."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=a."AttemptId"
  AND h."ReservationRevision"=a."EncryptionAttemptRevision" AND h."Fence"=a."Fence" AND h."CustodyState"='Reserved';
 RETURN FOUND;
END;$terminal$;
CREATE FUNCTION tagekyc.raw_export_accept_capture_for_ingress(
 p_verification_session_id uuid,p_client_application_id uuid,p_capture_artifact_id uuid,
 p_evidence_result_id uuid,p_session_challenge_hash text,p_acceptance_policy_id text,p_acceptance_policy_version integer)
RETURNS TABLE("CaptureAcceptanceId" uuid,"CaptureRevision" integer,"RawClass" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $accept$
DECLARE evidence_type text; raw_class text; existing tagekyc.raw_export_capture_acceptance_events%ROWTYPE;
 next_revision integer; acceptance_id uuid;
BEGIN
 PERFORM tagekyc.raw_export_current_actor();
 SELECT e."ResultType" INTO evidence_type FROM tagekyc.evidence_results e
 JOIN tagekyc.capture_artifacts a ON a."Id"=p_capture_artifact_id
 WHERE e."Id"=p_evidence_result_id AND e."VerificationSessionId"=p_verification_session_id
 AND a."VerificationSessionId"=p_verification_session_id AND e."Result"='Passed'
 AND e."InputCaptureArtifactIdsJson" ? pg_catalog.replace(p_capture_artifact_id::text,'-','');
 IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 raw_class:=CASE evidence_type WHEN 'NfcValidation' THEN 'ChipDg2Portrait' WHEN 'FaceMatch' THEN 'LiveSelfieImage' END;
 IF raw_class IS NULL THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INELIGIBLE'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('tip88c1a:capture-acceptance:'||p_verification_session_id::text||':'||raw_class,0));
 SELECT * INTO existing FROM tagekyc.raw_export_capture_acceptance_events e
 WHERE e."VerificationSessionId"=p_verification_session_id AND e."RawClass"=raw_class
 AND (e."CaptureArtifactId"=p_capture_artifact_id OR e."AcceptedEvidenceRef"=p_evidence_result_id::text)
 ORDER BY e."CaptureRevision" DESC LIMIT 1;
 IF FOUND THEN
  IF existing."CaptureArtifactId"<>p_capture_artifact_id OR existing."AcceptedEvidenceRef"<>p_evidence_result_id::text
   OR existing."ClientApplicationId"<>p_client_application_id OR existing."SessionChallengeHash"<>p_session_challenge_hash
   OR existing."AcceptancePolicyId"<>p_acceptance_policy_id OR existing."AcceptancePolicyVersion"<>p_acceptance_policy_version
  THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT'; END IF;
  RETURN QUERY SELECT existing."CaptureAcceptanceId",existing."CaptureRevision",raw_class; RETURN;
 END IF;
 SELECT COALESCE(MAX(e."CaptureRevision"),0)+1 INTO next_revision FROM tagekyc.raw_export_capture_acceptance_events e
 WHERE e."VerificationSessionId"=p_verification_session_id AND e."RawClass"=raw_class;
 acceptance_id:=tagekyc.raw_export_append_capture_acceptance(p_verification_session_id,p_client_application_id,raw_class,
 p_capture_artifact_id,next_revision,p_session_challenge_hash,p_evidence_result_id::text,p_acceptance_policy_id,p_acceptance_policy_version);
 RETURN QUERY SELECT acceptance_id,next_revision,raw_class;
END;$accept$;
ALTER FUNCTION tagekyc.raw_export_accept_capture_for_ingress(uuid,uuid,uuid,uuid,text,text,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_accept_capture_for_ingress(uuid,uuid,uuid,uuid,text,text,integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_accept_capture_for_ingress(uuid,uuid,uuid,uuid,text,text,integer) TO tagekyc_runtime;
CREATE FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(
 p_verification_session_id uuid,p_client_application_id uuid)
RETURNS TABLE("RawClass" text,"CaptureAcceptanceId" uuid,"CaptureRevision" integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $selection$
DECLARE class_name text; candidate record; existing_id uuid; lock_keys bigint[];
BEGIN
 PERFORM tagekyc.raw_export_current_actor();
 lock_keys:=ARRAY(SELECT pg_catalog.hashtextextended('tip88c1a:capture-acceptance:'||p_verification_session_id::text||':'||c,0)
  FROM unnest(ARRAY['ChipDg2Portrait','LiveSelfieImage']) c ORDER BY 1);
 PERFORM pg_catalog.pg_advisory_xact_lock(k) FROM unnest(lock_keys) k;
 FOREACH class_name IN ARRAY ARRAY['ChipDg2Portrait','LiveSelfieImage'] LOOP
  SELECT a."CaptureAcceptanceId",a."CaptureRevision" INTO candidate
  FROM tagekyc.raw_export_capture_acceptance_events a
  JOIN tagekyc.raw_export_source_ingress_claims c ON c."CaptureAcceptanceId"=a."CaptureAcceptanceId"
  JOIN tagekyc.raw_export_source_reservations r ON r."IngressClaimId"=c."IngressClaimId"
  JOIN tagekyc.raw_export_source_publications p ON p."SourceArtifactId"=r."SourceArtifactId" AND p."PublicationState"='Available'
  WHERE a."VerificationSessionId"=p_verification_session_id AND a."ClientApplicationId"=p_client_application_id
   AND a."RawClass"=class_name AND c."CaptureArtifactId"=a."CaptureArtifactId" AND c."CaptureRevision"=a."CaptureRevision";
  IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_SESSION_CAPTURE_SELECTION_MISSING'; END IF;
  IF (SELECT count(*) FROM tagekyc.raw_export_capture_acceptance_events a
   JOIN tagekyc.raw_export_source_ingress_claims c ON c."CaptureAcceptanceId"=a."CaptureAcceptanceId"
   JOIN tagekyc.raw_export_source_reservations r ON r."IngressClaimId"=c."IngressClaimId"
   JOIN tagekyc.raw_export_source_publications p ON p."SourceArtifactId"=r."SourceArtifactId" AND p."PublicationState"='Available'
   WHERE a."VerificationSessionId"=p_verification_session_id AND a."ClientApplicationId"=p_client_application_id AND a."RawClass"=class_name)<>1
   THEN RAISE EXCEPTION 'RAW_EXPORT_SESSION_CAPTURE_SELECTION_AMBIGUOUS'; END IF;
  SELECT s."CaptureAcceptanceId" INTO existing_id FROM tagekyc.raw_export_session_capture_selections s
   WHERE s."VerificationSessionId"=p_verification_session_id AND s."RawClass"=class_name;
  IF FOUND AND existing_id<>candidate."CaptureAcceptanceId" THEN RAISE EXCEPTION 'RAW_EXPORT_SESSION_CAPTURE_SELECTION_CONFLICT'; END IF;
  IF NOT FOUND THEN PERFORM tagekyc.raw_export_select_session_capture_acceptance(p_verification_session_id,class_name,candidate."CaptureAcceptanceId"); END IF;
  "RawClass":=class_name;"CaptureAcceptanceId":=candidate."CaptureAcceptanceId";"CaptureRevision":=candidate."CaptureRevision";RETURN NEXT;
 END LOOP;
END;$selection$;
ALTER FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(uuid,uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(uuid,uuid) TO tagekyc_runtime;
ALTER FUNCTION tagekyc.raw_export_begin_source_ingress_with_authority_core(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,text) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_source_ingress_with_authority_core(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,text) FROM PUBLIC, tagekyc_runtime, tagekyc_raw_export_claim_broker;
ALTER FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) FROM PUBLIC, tagekyc_runtime;
REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) FROM PUBLIC, tagekyc_runtime;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) TO tagekyc_raw_export_claim_broker;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) TO tagekyc_raw_export_claim_broker;
ALTER FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer) FROM PUBLIC,tagekyc_runtime;
GRANT EXECUTE ON FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer) TO tagekyc_raw_export_claim_broker;
ALTER FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text) TO tagekyc_runtime;

-- C6B closes the two direct acceptance mutation capabilities.  Runtime reaches
-- these primitives only through the server-owned acceptance coordinators.
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_append_capture_acceptance(uuid,uuid,text,uuid,integer,text,text,text,integer)
    FROM tagekyc_runtime;
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_select_session_capture_acceptance(uuid,text,uuid)
    FROM tagekyc_runtime;
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text) FROM tagekyc_runtime;
DROP FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text);
REVOKE EXECUTE ON FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer) FROM tagekyc_raw_export_claim_broker;
DROP FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer);
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) FROM tagekyc_raw_export_claim_broker;
DROP FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer);
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_accept_capture_for_ingress(uuid,uuid,uuid,uuid,text,text,integer) FROM tagekyc_runtime;
DROP FUNCTION tagekyc.raw_export_accept_capture_for_ingress(uuid,uuid,uuid,uuid,text,text,integer);
REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(uuid,uuid) FROM tagekyc_runtime;
DROP FUNCTION tagekyc.raw_export_select_capture_acceptances_on_session_completion(uuid,uuid);
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_append_capture_acceptance(uuid,uuid,text,uuid,integer,text,text,text,integer)
    TO tagekyc_runtime;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_select_session_capture_acceptance(uuid,text,uuid)
    TO tagekyc_runtime;
                CREATE OR REPLACE FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(
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
                    p_claimed_plaintext_length bigint,
                    p_media_type text,
                    p_captured_at_utc timestamptz,
                    p_plaintext_retention_started_at_utc timestamptz,
                    p_plaintext_retention_expires_at_utc timestamptz,
                    p_plaintext_retention_budget_seconds integer,
                    p_commitment_key_selector_id text,
                    p_commitment_key_selector_version integer,
                    p_claim_evaluation_owner_id uuid,
                    p_claim_evaluation_token_ttl_seconds integer,
                    p_idempotency_lock_timeout_milliseconds integer)
                RETURNS TABLE(
                    outcome_code text, claim_evaluation_token text,
                    token_variant text, token_expires_at_utc timestamptz,
                    claim_evaluation_id uuid, claim_evaluation_revision bigint,
                    claim_evaluation_fence bigint,
                    retry_not_before_utc timestamptz)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    producer_id_nfc text := pg_catalog.normalize(p_producer_id, 'NFC');
                    agent_id_nfc text := pg_catalog.normalize(p_capture_agent_instance_id, 'NFC');
                    raw_class_nfc text := pg_catalog.normalize(p_raw_class, 'NFC');
                    ingress_key uuid;
                    alias_lock bigint;
                    exact_lock bigint;
                    first_lock bigint;
                    second_lock bigint;
                    lock_deadline timestamptz;
                    acquired boolean;
                    existing_snapshot_id text;
                    decision_row record;
                    policy_row tagekyc.raw_export_policy_versions%ROWTYPE;
                    snapshot_row record;
                    now_utc timestamptz := pg_catalog.transaction_timestamp();
                BEGIN
                    IF p_ingress_idempotency_key IS NULL OR
                       p_ingress_idempotency_key !~ '^[0-9a-f]{12}4[0-9a-f]{3}[89ab][0-9a-f]{15}$' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SOURCE_BINDING_INVALID';
                    END IF;
                    ingress_key := p_ingress_idempotency_key::uuid;
                    alias_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:alias:' || p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' || agent_id_nfc || ':' || ingress_key::text, 0);
                    exact_lock := pg_catalog.hashtextextended(
                        'tip88c1b1:exact:' || p_client_application_id::text || ':' ||
                        producer_id_nfc || ':' || p_verification_session_id::text || ':' ||
                        p_capture_artifact_id::text || ':' || p_capture_revision::text || ':' ||
                        raw_class_nfc, 0);
                    first_lock := LEAST(alias_lock, exact_lock);
                    second_lock := GREATEST(alias_lock, exact_lock);
                    lock_deadline := pg_catalog.clock_timestamp() +
                        pg_catalog.make_interval(
                            secs => p_idempotency_lock_timeout_milliseconds / 1000.0);
                    LOOP
                        acquired := pg_catalog.pg_try_advisory_xact_lock(first_lock);
                        EXIT WHEN acquired;
                        IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                            RAISE EXCEPTION USING ERRCODE='55P03',
                                MESSAGE='RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                        END IF;
                        PERFORM pg_catalog.pg_sleep(0.005);
                    END LOOP;
                    IF second_lock <> first_lock THEN
                        LOOP
                            acquired := pg_catalog.pg_try_advisory_xact_lock(second_lock);
                            EXIT WHEN acquired;
                            IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                                RAISE EXCEPTION USING ERRCODE='55P03',
                                    MESSAGE='RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
                            END IF;
                            PERFORM pg_catalog.pg_sleep(0.005);
                        END LOOP;
                    END IF;

                    SELECT claim."AuthoritySnapshotId"
                    INTO existing_snapshot_id
                    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
                    JOIN tagekyc.raw_export_source_ingress_claims AS claim
                      ON claim."IngressClaimId" = alias."IngressClaimId"
                    WHERE alias."ClientApplicationId" = p_client_application_id
                      AND alias."ProducerId" = producer_id_nfc
                      AND alias."CaptureAgentInstanceId" = agent_id_nfc
                      AND alias."IngressIdempotencyKey" = ingress_key;
                    IF NOT FOUND THEN
                        SELECT claim."AuthoritySnapshotId"
                        INTO existing_snapshot_id
                        FROM tagekyc.raw_export_source_ingress_claims AS claim
                        WHERE claim."ClientApplicationId" = p_client_application_id
                          AND claim."ProducerId" = producer_id_nfc
                          AND claim."VerificationSessionId" = p_verification_session_id
                          AND claim."CaptureArtifactId" = p_capture_artifact_id
                          AND claim."CaptureRevision" = p_capture_revision
                          AND claim."RawClass" = raw_class_nfc;
                    END IF;

                    IF existing_snapshot_id IS NULL THEN
                        SELECT decision.*, permit."PermitId",
                               permit."DecisionExpiresAtUtc" AS permit_expires_at
                        INTO decision_row
                        FROM tagekyc.raw_export_authorization_decisions AS decision
                        JOIN tagekyc.raw_export_authorization_permits AS permit
                          ON permit."AuthorizationDecisionId" = decision."ExportDecisionId"
                        JOIN tagekyc.raw_export_permit_classes AS permit_class
                          ON permit_class."PermitId" = permit."PermitId"
                        WHERE decision."Outcome" = 'Authorized'
                          AND decision."PrincipalId" = p_authenticated_principal_id
                          AND decision."ClientApplicationId" = p_client_application_id
                          AND decision."ResolvedVerificationSessionId" = p_verification_session_id
                          AND permit_class."RawClass" = raw_class_nfc
                          AND permit."DecisionExpiresAtUtc" > now_utc
                        ORDER BY decision."DecidedAtUtc" DESC
                        LIMIT 1;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        SELECT * INTO policy_row
                        FROM tagekyc.raw_export_policy_versions AS policy
                        WHERE policy."PolicyId" = decision_row."PolicyId"
                          AND policy."PolicyVersion" = decision_row."PolicyVersion"
                          AND policy."Mode" = 'EncryptedExportPacket'
                          AND policy."RetentionProfileRef" IS NOT NULL;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        IF EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_read_authorization_eligibility_inputs(
                                p_authenticated_principal_id,
                                policy_row."PolicyId",
                                policy_row."PolicyVersion") AS eligibility
                            WHERE NOT eligibility."PolicyExists"
                               OR eligibility."ClosureType" <> 'CatalogApproved'
                               OR eligibility."BoundRuleSetVersion" <>
                                    eligibility."CurrentRuleSetVersion"
                               OR eligibility."GrantEventType" <> 'Granted'
                               OR eligibility."LifecycleEventType" <> 'Activated'
                               OR (eligibility."RequirementType" IS NOT NULL AND (
                                    eligibility."FulfillmentEventType" <> 'Accepted'
                                    OR eligibility."ValidFromUtc" > now_utc
                                    OR (eligibility."ValidUntilUtc" IS NOT NULL AND
                                        eligibility."ValidUntilUtc" <= now_utc)))) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        IF NOT EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
                                p_verification_session_id,
                                policy_row."PolicyId",
                                policy_row."PolicyVersion") AS consent
                            WHERE consent."RawClass" = raw_class_nfc
                              AND consent."State" = 'Effective'
                              AND consent."PurposeCode" = 'SubjectRawBiometricExport'
                              AND consent."RecipientClientApplicationId" =
                                    p_client_application_id
                              AND consent."ValidFromUtc" <= now_utc
                              AND (consent."ValidUntilUtc" IS NULL OR
                                   consent."ValidUntilUtc" > now_utc)) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND';
                        END IF;
                        SELECT * INTO snapshot_row
                        FROM tagekyc.raw_export_append_authority_snapshot(
                            p_client_application_id,
                            p_verification_session_id,
                            p_capture_acceptance_id,
                            raw_class_nfc,
                            decision_row."ExportDecisionId",
                            decision_row."GrantRevision",
                            policy_row."ControllerEntityRef",
                            p_client_application_id::text || ':' ||
                                p_verification_session_id::text || ':' || raw_class_nfc,
                            policy_row."RetentionProfileRef",
                            policy_row."PolicyVersion",
                            policy_row."PolicyId",
                            policy_row."PolicyVersion",
                            policy_row."Mode",
                            'CaptureAccepted',
                            LEAST(p_plaintext_retention_expires_at_utc,
                                  decision_row.permit_expires_at),
                            'PolicyGrantLifecycle',
                            policy_row."RetentionProfileRef",
                            'PolicyLegalHold',
                            now_utc,
                            decision_row.permit_expires_at);
                        existing_snapshot_id := snapshot_row."AuthoritySnapshotId"::text;
                    END IF;

                    RETURN QUERY
                    SELECT * FROM tagekyc.begin_raw_export_source_ingress_claim(
                        p_authenticated_principal_id,p_client_application_id,
                        p_producer_id,p_capture_agent_instance_id,
                        p_ingress_idempotency_key,p_verification_session_id,
                        p_capture_acceptance_id,p_capture_artifact_id,
                        p_capture_revision,p_raw_class,p_session_challenge_hash,
                        existing_snapshot_id,p_claimed_plaintext_length,p_media_type,
                        p_captured_at_utc,p_plaintext_retention_started_at_utc,
                        p_plaintext_retention_expires_at_utc,
                        p_plaintext_retention_budget_seconds,
                        p_commitment_key_selector_id,
                        p_commitment_key_selector_version,
                        p_claim_evaluation_owner_id,
                        p_claim_evaluation_token_ttl_seconds,
                        p_idempotency_lock_timeout_milliseconds);
                END;
                $function$;
ALTER FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) FROM PUBLIC, tagekyc_runtime;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer) TO tagekyc_raw_export_claim_broker;
DROP FUNCTION tagekyc.raw_export_begin_source_ingress_with_authority_core(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,text);
""");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_attempt_r2_terminal_outcome",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");

            migrationBuilder.DropColumn(
                name: "R2TerminalOutcomeCode",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");
        }
    }
}
