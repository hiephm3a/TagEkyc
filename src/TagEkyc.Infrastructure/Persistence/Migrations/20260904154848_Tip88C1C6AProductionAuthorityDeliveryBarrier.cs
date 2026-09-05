using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C6AProductionAuthorityDeliveryBarrier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_c3_current_authority_eligible(
                    p_delivery_id uuid,
                    p_recipient_client_application_id uuid,
                    p_principal_id uuid,
                    p_evaluated_at_utc timestamptz)
                RETURNS boolean
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    identity_row tagekyc.raw_export_job_identities%ROWTYPE;
                    decision_row tagekyc.raw_export_authorization_decisions%ROWTYPE;
                    source_row record;
                    authority_row record;
                    consent_row record;
                    eligibility_row record;
                    requirement_count integer;
                    eligible_requirement_count integer;
                BEGIN
                    SELECT job.*
                    INTO identity_row
                    FROM tagekyc.raw_export_recipient_package_deliveries AS delivery
                    JOIN tagekyc.raw_export_recipient_package_preparations AS package
                      ON package."PackageId" = delivery."PackageId"
                    JOIN tagekyc.raw_export_job_identities AS job
                      ON job."JobId" = package."JobId"
                    WHERE delivery."DeliveryId" = p_delivery_id
                      AND delivery."RecipientClientApplicationId" =
                            p_recipient_client_application_id;
                    IF NOT FOUND
                       OR identity_row."PrincipalId" <> p_principal_id
                       OR identity_row."RecipientClientApplicationId" <>
                            p_recipient_client_application_id
                       OR identity_row."PermitExpiresAt" <= p_evaluated_at_utc
                       OR identity_row."JobExpiresAt" <= p_evaluated_at_utc THEN
                        RETURN FALSE;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1
                        FROM tagekyc.verification_sessions AS session
                        WHERE session."Id" = identity_row."VerificationSessionId"
                          AND session."State" = 'Completed'
                          AND session."ClientApplicationId" =
                                identity_row."ClientApplicationId"
                          AND session."SubjectRef" = identity_row."SubjectRef") THEN
                        RETURN FALSE;
                    END IF;

                    SELECT decision.*
                    INTO decision_row
                    FROM tagekyc.raw_export_authorization_decisions AS decision
                    WHERE decision."ExportDecisionId" =
                            identity_row."AuthorizationDecisionId";
                    IF NOT FOUND
                       OR decision_row."Outcome" <> 'Authorized'
                       OR decision_row."PrincipalId" <> identity_row."PrincipalId"
                       OR decision_row."ClientApplicationId" <>
                            identity_row."ClientApplicationId"
                       OR decision_row."ResolvedVerificationSessionId" <>
                            identity_row."VerificationSessionId"
                       OR decision_row."PolicyId" <> identity_row."PolicyId"
                       OR decision_row."PolicyVersion" <> identity_row."PolicyVersion"
                       OR decision_row."RecipientClientApplicationId" <>
                            identity_row."RecipientClientApplicationId"
                       OR decision_row."BoundRuleSetVersion" IS NULL
                       OR decision_row."DecisionExpiresAtUtc" <= p_evaluated_at_utc THEN
                        RETURN FALSE;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_authorization_permits AS permit
                        WHERE permit."PermitId" = identity_row."PermitId"
                          AND permit."AuthorizationDecisionId" =
                                identity_row."AuthorizationDecisionId"
                          AND permit."ResolvedVerificationSessionId" =
                                identity_row."VerificationSessionId"
                          AND permit."SubjectRef" = identity_row."SubjectRef"
                          AND permit."PolicyId" = identity_row."PolicyId"
                          AND permit."PolicyVersion" = identity_row."PolicyVersion"
                          AND permit."PurposeCode" = identity_row."PurposeCode"
                          AND permit."RecipientClientApplicationId" =
                                identity_row."RecipientClientApplicationId"
                          AND permit."DecisionExpiresAtUtc" =
                                identity_row."PermitExpiresAt") THEN
                        RETURN FALSE;
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext('tip88b1:grant:' ||
                            identity_row."PrincipalId"::text || ':' ||
                            identity_row."PolicyId"::text || ':' ||
                            identity_row."PolicyVersion"::text));
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext('tip88b1:lifecycle:' ||
                            identity_row."PolicyId"::text || ':' ||
                            identity_row."PolicyVersion"::text));
                    IF NOT EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_policy_versions AS policy
                        JOIN tagekyc.raw_export_policy_closures AS closure
                          ON closure."PolicyId" = policy."PolicyId"
                         AND closure."PolicyVersion" = policy."PolicyVersion"
                        WHERE policy."PolicyId" = identity_row."PolicyId"
                          AND policy."PolicyVersion" = identity_row."PolicyVersion"
                          AND closure."ClosureType" = 'CatalogApproved'
                          AND policy."RequirementRuleSetVersion" = (
                              SELECT pg_catalog.max(rule_set."RuleSetVersion")
                              FROM tagekyc.raw_export_requirement_rule_sets AS rule_set
                              WHERE rule_set."RuleSetId" = 'RAW_EXPORT_REQUIREMENTS')
                          AND decision_row."BoundRuleSetVersion" =
                                policy."RequirementRuleSetVersion"
                          AND (SELECT grant_event."EventType"
                               FROM tagekyc.raw_export_grants AS grant_event
                               WHERE grant_event."PrincipalId" = identity_row."PrincipalId"
                                 AND grant_event."PolicyId" = identity_row."PolicyId"
                                 AND grant_event."PolicyVersion" = identity_row."PolicyVersion"
                               ORDER BY grant_event."Revision" DESC LIMIT 1) = 'Granted'
                          AND (SELECT lifecycle."EventType"
                               FROM tagekyc.raw_export_policy_lifecycle AS lifecycle
                               WHERE lifecycle."PolicyId" = identity_row."PolicyId"
                                 AND lifecycle."PolicyVersion" = identity_row."PolicyVersion"
                               ORDER BY lifecycle."Revision" DESC LIMIT 1) = 'Activated') THEN
                        RETURN FALSE;
                    END IF;

                    SELECT pg_catalog.count(*)
                    INTO requirement_count
                    FROM tagekyc.raw_export_policy_requirements AS requirement
                    WHERE requirement."PolicyId" = identity_row."PolicyId"
                      AND requirement."PolicyVersion" = identity_row."PolicyVersion"
                      AND requirement."RequirementType" <> 'ConsentArtifact';
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext('tip88b1:fulfillment:' ||
                            identity_row."PolicyId"::text || ':' ||
                            identity_row."PolicyVersion"::text || ':' ||
                            requirement."RequirementType"))
                    FROM tagekyc.raw_export_policy_requirements AS requirement
                    WHERE requirement."PolicyId" = identity_row."PolicyId"
                      AND requirement."PolicyVersion" = identity_row."PolicyVersion"
                      AND requirement."RequirementType" <> 'ConsentArtifact'
                    ORDER BY requirement."RequirementType";
                    SELECT pg_catalog.count(*)
                    INTO eligible_requirement_count
                    FROM tagekyc.raw_export_policy_requirements AS requirement
                    WHERE requirement."PolicyId" = identity_row."PolicyId"
                      AND requirement."PolicyVersion" = identity_row."PolicyVersion"
                      AND requirement."RequirementType" <> 'ConsentArtifact'
                      AND EXISTS (
                          SELECT 1
                          FROM LATERAL (
                              SELECT fulfillment."EventType",
                                     fulfillment."ValidFromUtc",
                                     fulfillment."ValidUntilUtc"
                              FROM tagekyc.raw_export_fulfillments AS fulfillment
                              WHERE fulfillment."PolicyId" = requirement."PolicyId"
                                AND fulfillment."PolicyVersion" = requirement."PolicyVersion"
                                AND fulfillment."RequirementType" = requirement."RequirementType"
                              ORDER BY fulfillment."Revision" DESC
                              LIMIT 1) AS latest
                          WHERE latest."EventType" = 'Accepted'
                            AND latest."ValidFromUtc" <= p_evaluated_at_utc
                            AND (latest."ValidUntilUtc" IS NULL OR
                                 latest."ValidUntilUtc" > p_evaluated_at_utc));
                    IF eligible_requirement_count <> requirement_count THEN
                        RETURN FALSE;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_job_classes AS job_class
                        FULL JOIN tagekyc.raw_export_permit_classes AS permit_class
                          ON permit_class."PermitId" = identity_row."PermitId"
                         AND permit_class."Ordinal" = job_class."Ordinal"
                         AND permit_class."RawClass" = job_class."RawClass"
                        WHERE (job_class."JobId" = identity_row."JobId" OR
                               permit_class."PermitId" = identity_row."PermitId")
                          AND (job_class."JobId" IS NULL OR
                               permit_class."PermitId" IS NULL)) THEN
                        RETURN FALSE;
                    END IF;
                    IF (SELECT pg_catalog.count(*)
                        FROM tagekyc.raw_export_job_source_bindings AS binding
                        WHERE binding."JobId" = identity_row."JobId") <>
                       (SELECT pg_catalog.count(*)
                        FROM tagekyc.raw_export_job_classes AS job_class
                        WHERE job_class."JobId" = identity_row."JobId") THEN
                        RETURN FALSE;
                    END IF;

                    FOR source_row IN
                        SELECT binding.*
                        FROM tagekyc.raw_export_job_source_bindings AS binding
                        WHERE binding."JobId" = identity_row."JobId"
                        ORDER BY binding."Ordinal"
                    LOOP
                        PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                            pg_catalog.hashtext(
                                'tip88c1:b2-authority:' ||
                                identity_row."ClientApplicationId"::text || ':' ||
                                source_row."VerificationSessionId"::text || ':' ||
                                source_row."CaptureAcceptanceId"::text || ':' ||
                                source_row."RawClass"));
                        SELECT * INTO authority_row
                        FROM tagekyc.raw_export_resolve_current_authority_for_source(
                            identity_row."ClientApplicationId",
                            source_row."VerificationSessionId",
                            source_row."CaptureAcceptanceId",
                            source_row."RawClass",
                            p_evaluated_at_utc);
                        IF NOT FOUND
                           OR authority_row."AuthoritySnapshotId" <>
                                source_row."AuthoritySnapshotId"
                           OR authority_row."Revision" <>
                                source_row."AuthorityRevision"
                           OR authority_row."ApprovedPurpose" <>
                                identity_row."PurposeCode"
                           OR authority_row."ReuseDisposition" <>
                                'FreshAuthorityRequired'
                           OR authority_row."ExtensionDisposition" <> 'Forbidden'
                           OR authority_row."ConsentPolicyId" <>
                                source_row."ConsentPolicyId"
                           OR authority_row."ConsentPolicyVersion" <>
                                source_row."ConsentPolicyVersion"
                           OR authority_row."AbsoluteSourceExpiresAtUtc" <=
                                p_evaluated_at_utc
                           OR source_row."EffectivePlaintextRetentionExpiresAtUtc" <=
                                p_evaluated_at_utc THEN
                            RETURN FALSE;
                        END IF;

                        SELECT * INTO consent_row
                        FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
                            source_row."VerificationSessionId",
                            source_row."ConsentPolicyId",
                            source_row."ConsentPolicyVersion")
                        WHERE "RawClass" = source_row."RawClass"
                        LIMIT 1;
                        IF NOT FOUND
                           OR consent_row."State" <> 'Effective'
                           OR consent_row."PurposeCode" <> identity_row."PurposeCode"
                           OR consent_row."RecipientClientApplicationId" <>
                                identity_row."RecipientClientApplicationId"
                           OR consent_row."ValidFromUtc" > p_evaluated_at_utc
                           OR (consent_row."ValidUntilUtc" IS NOT NULL AND
                               consent_row."ValidUntilUtc" <= p_evaluated_at_utc) THEN
                            RETURN FALSE;
                        END IF;
                    END LOOP;
                    RETURN TRUE;
                END;
                $function$;

                ALTER FUNCTION tagekyc.raw_export_c3_current_authority_eligible(
                    uuid,uuid,uuid,timestamptz)
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_c3_current_authority_eligible(
                        uuid,uuid,uuid,timestamptz)
                    FROM PUBLIC, tagekyc_runtime,
                         tagekyc_raw_export_claim_broker;

                DO $migration$
                DECLARE
                    function_definition text;
                    old_fragment text := 'OR p."State"<>''Finalized'' OR p."Revision"<>d."PackageRevisionAtAuthorization" THEN outcome:=''Ineligible'';';
                    new_fragment text := 'OR p."State"<>''Finalized'' OR p."Revision"<>d."PackageRevisionAtAuthorization" OR NOT tagekyc.raw_export_c3_current_authority_eligible(p_delivery,p_recipient,p_principal,now_utc) THEN outcome:=''Ineligible'';';
                BEGIN
                    SELECT pg_catalog.pg_get_functiondef(
                        'tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)'::regprocedure)
                    INTO function_definition;
                    IF pg_catalog.strpos(function_definition, old_fragment) = 0 THEN
                        RAISE EXCEPTION 'TIP88C1C6A_C3_BEGIN_BODY_DRIFT';
                    END IF;
                    EXECUTE pg_catalog.replace(
                        function_definition, old_fragment, new_fragment);
                END;
                $migration$;

                CREATE FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(
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

                ALTER FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(
                    uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,
                    bigint,text,timestamptz,timestamptz,timestamptz,integer,
                    text,integer,uuid,integer,integer)
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_begin_production_source_ingress_with_authority(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,
                        bigint,text,timestamptz,timestamptz,timestamptz,integer,
                        text,integer,uuid,integer,integer)
                    FROM PUBLIC, tagekyc_runtime;
                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_begin_production_source_ingress_with_authority(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,
                        bigint,text,timestamptz,timestamptz,timestamptz,integer,
                        text,integer,uuid,integer,integer)
                    TO tagekyc_raw_export_claim_broker;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE EXECUTE ON FUNCTION
                    tagekyc.raw_export_begin_production_source_ingress_with_authority(
                        uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,
                        bigint,text,timestamptz,timestamptz,timestamptz,integer,
                        text,integer,uuid,integer,integer)
                    FROM tagekyc_raw_export_claim_broker;
                DROP FUNCTION tagekyc.raw_export_begin_production_source_ingress_with_authority(
                    uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,
                    bigint,text,timestamptz,timestamptz,timestamptz,integer,
                    text,integer,uuid,integer,integer);

                DO $migration$
                DECLARE
                    function_definition text;
                    old_fragment text := 'OR p."State"<>''Finalized'' OR p."Revision"<>d."PackageRevisionAtAuthorization" OR NOT tagekyc.raw_export_c3_current_authority_eligible(p_delivery,p_recipient,p_principal,now_utc) THEN outcome:=''Ineligible'';';
                    new_fragment text := 'OR p."State"<>''Finalized'' OR p."Revision"<>d."PackageRevisionAtAuthorization" THEN outcome:=''Ineligible'';';
                BEGIN
                    SELECT pg_catalog.pg_get_functiondef(
                        'tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)'::regprocedure)
                    INTO function_definition;
                    IF pg_catalog.strpos(function_definition, old_fragment) = 0 THEN
                        RAISE EXCEPTION 'TIP88C1C6A_C3_BEGIN_BODY_DRIFT';
                    END IF;
                    EXECUTE pg_catalog.replace(
                        function_definition, old_fragment, new_fragment);
                END;
                $migration$;
                DROP FUNCTION tagekyc.raw_export_c3_current_authority_eligible(
                    uuid,uuid,uuid,timestamptz);
                """);
        }
    }
}
