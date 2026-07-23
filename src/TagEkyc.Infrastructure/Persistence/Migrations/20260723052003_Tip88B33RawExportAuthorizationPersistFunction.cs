using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B33RawExportAuthorizationPersistFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_persist_authorization_decision(payload jsonb)
                RETURNS uuid
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    identity_json jsonb;
                    decision_json jsonb;
                    permit_json jsonb;
                    item jsonb;
                    field_name text;
                    top_allowed text[] := ARRAY[
                        'PayloadSchemaVersion','ExportDecisionId','IdempotencyIdentity','Decision',
                        'EligibilityCauses','FulfillmentRefs','Classes','Permit','PermitClasses'];
                    top_required text[] := ARRAY[
                        'PayloadSchemaVersion','ExportDecisionId','IdempotencyIdentity','Decision',
                        'EligibilityCauses','FulfillmentRefs','Classes'];
                    identity_keys text[] := ARRAY[
                        'PrincipalId','ClientApplicationId','RequestedVerificationSessionId','IdempotencyKey'];
                    decision_keys text[] := ARRAY[
                        'PrincipalId','ClientApplicationId','ApiKeyId','RequestedVerificationSessionId',
                        'PolicyId','PolicyVersion','FingerprintHash','RawClassSelectionMode','Outcome',
                        'PrimaryCause','ResolvedVerificationSessionId','SessionOwnerClientApplicationId',
                        'SessionSubjectRef','SessionState','BoundRuleSetVersion','CurrentRuleSetVersion',
                        'EligibilityPrimaryCause','EligibilityEvaluatedAtUtc','GrantPrincipalId','GrantPolicyId',
                        'GrantPolicyVersion','GrantRevision','LifecyclePolicyId','LifecyclePolicyVersion',
                        'LifecycleRevision','PurposeCode','RecipientClientApplicationId','SubjectConsentCause',
                        'ConsentScopeHash','SubjectConsentRecordId','ConsentRevision','ConsentValidFromUtc',
                        'ConsentValidUntilUtc','ConsentEvaluatedAtUtc','PolicyPermitTtlSeconds',
                        'DecisionExpiresAtUtc'];
                    permit_keys text[] := ARRAY[
                        'PermitId','ResolvedVerificationSessionId','SubjectRef','PolicyId','PolicyVersion',
                        'PurposeCode','RecipientClientApplicationId','DecisionExpiresAtUtc','SchemaVersion'];
                    uuid_pattern constant text :=
                        '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
                    integer_pattern constant text := '^-?(0|[1-9][0-9]*)$';
                    timestamp_pattern constant text :=
                        '^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}([.][0-9]{1,6})?(Z|[+]00:00)$';
                    decision_id uuid;
                    identity_principal_id uuid;
                    identity_client_id uuid;
                    identity_session_id uuid;
                    decision_principal_id uuid;
                    decision_client_id uuid;
                    decision_session_id uuid;
                    fingerprint_hash bytea;
                    stored_fingerprint bytea;
                    stored_decision_id uuid;
                    actor_id uuid;
                    eligibility_count integer;
                    fulfillment_count integer;
                    policy_count integer;
                    requested_count integer;
                    effective_count integer;
                    consented_count integer;
                    authorized_count integer;
                    permit_class_count integer;
                    authorized_sequence jsonb;
                    permit_sequence jsonb;
                    session_none boolean;
                    session_owned_without_subject boolean;
                    session_required boolean;
                    b1_core_none boolean;
                    b1_core_required boolean;
                    active_refs_none boolean;
                    active_refs_required boolean;
                    b2_scope_none boolean;
                    b2_scope_required boolean;
                    b2_ref_none boolean;
                    b2_ref_required boolean;
                    b2_all_none boolean;
                    requested_shape_valid boolean;
                BEGIN
                    -- Phase 1: exact schema, JSON kinds, array bounds, and lexical forms.
                    IF pg_catalog.jsonb_typeof(payload) <> 'object'
                       OR NOT payload ?& top_required
                       OR EXISTS (
                           SELECT 1 FROM pg_catalog.jsonb_object_keys(payload) AS key_name
                           WHERE key_name <> ALL(top_allowed))
                       OR pg_catalog.jsonb_typeof(payload->'PayloadSchemaVersion') <> 'number'
                       OR payload->>'PayloadSchemaVersion' !~ integer_pattern
                       OR payload->>'PayloadSchemaVersion' <> '1'
                       OR pg_catalog.jsonb_typeof(payload->'ExportDecisionId') <> 'string'
                       OR payload->>'ExportDecisionId' !~ uuid_pattern
                       OR pg_catalog.jsonb_typeof(payload->'IdempotencyIdentity') <> 'object'
                       OR pg_catalog.jsonb_typeof(payload->'Decision') <> 'object'
                       OR pg_catalog.jsonb_typeof(payload->'EligibilityCauses') <> 'array'
                       OR pg_catalog.jsonb_typeof(payload->'FulfillmentRefs') <> 'array'
                       OR pg_catalog.jsonb_typeof(payload->'Classes') <> 'array'
                       OR pg_catalog.jsonb_array_length(payload->'EligibilityCauses') > 64
                       OR pg_catalog.jsonb_array_length(payload->'FulfillmentRefs') > 64
                       OR pg_catalog.jsonb_array_length(payload->'Classes') > 64
                       OR (payload ? 'PermitClasses' AND (
                           pg_catalog.jsonb_typeof(payload->'PermitClasses') <> 'array'
                           OR pg_catalog.jsonb_array_length(payload->'PermitClasses') > 64))
                    THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END IF;

                    identity_json := payload->'IdempotencyIdentity';
                    decision_json := payload->'Decision';
                    IF NOT identity_json ?& identity_keys
                       OR EXISTS (
                           SELECT 1 FROM pg_catalog.jsonb_object_keys(identity_json) AS key_name
                           WHERE key_name <> ALL(identity_keys))
                       OR NOT decision_json ?& decision_keys
                       OR EXISTS (
                           SELECT 1 FROM pg_catalog.jsonb_object_keys(decision_json) AS key_name
                           WHERE key_name <> ALL(decision_keys))
                    THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END IF;

                    FOREACH field_name IN ARRAY ARRAY[
                        'PrincipalId','ClientApplicationId','RequestedVerificationSessionId']
                    LOOP
                        IF pg_catalog.jsonb_typeof(identity_json->field_name) <> 'string'
                           OR identity_json->>field_name !~ uuid_pattern THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    IF pg_catalog.jsonb_typeof(identity_json->'IdempotencyKey') <> 'string' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END IF;

                    FOREACH field_name IN ARRAY ARRAY[
                        'PrincipalId','ClientApplicationId','ApiKeyId',
                        'RequestedVerificationSessionId','PolicyId']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) <> 'string'
                           OR decision_json->>field_name !~ uuid_pattern THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOREACH field_name IN ARRAY ARRAY[
                        'ResolvedVerificationSessionId','SessionOwnerClientApplicationId',
                        'GrantPrincipalId','GrantPolicyId','LifecyclePolicyId',
                        'RecipientClientApplicationId','SubjectConsentRecordId']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) NOT IN ('string','null')
                           OR (pg_catalog.jsonb_typeof(decision_json->field_name) = 'string'
                               AND decision_json->>field_name !~ uuid_pattern) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    IF pg_catalog.jsonb_typeof(decision_json->'PolicyVersion') <> 'number'
                       OR decision_json->>'PolicyVersion' !~ integer_pattern THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END IF;
                    FOREACH field_name IN ARRAY ARRAY[
                        'BoundRuleSetVersion','CurrentRuleSetVersion','GrantPolicyVersion','GrantRevision',
                        'LifecyclePolicyVersion','LifecycleRevision','ConsentRevision','PolicyPermitTtlSeconds']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) NOT IN ('number','null')
                           OR (pg_catalog.jsonb_typeof(decision_json->field_name) = 'number'
                               AND decision_json->>field_name !~ integer_pattern) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOREACH field_name IN ARRAY ARRAY[
                        'RawClassSelectionMode','Outcome']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) <> 'string' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOREACH field_name IN ARRAY ARRAY[
                        'PrimaryCause','SessionSubjectRef','SessionState','EligibilityPrimaryCause',
                        'PurposeCode','SubjectConsentCause']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) NOT IN ('string','null') THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOREACH field_name IN ARRAY ARRAY[
                        'EligibilityEvaluatedAtUtc','ConsentValidFromUtc','ConsentValidUntilUtc',
                        'ConsentEvaluatedAtUtc','DecisionExpiresAtUtc']
                    LOOP
                        IF pg_catalog.jsonb_typeof(decision_json->field_name) NOT IN ('string','null')
                           OR (pg_catalog.jsonb_typeof(decision_json->field_name) = 'string'
                               AND decision_json->>field_name !~ timestamp_pattern) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    IF pg_catalog.jsonb_typeof(decision_json->'FingerprintHash') <> 'string'
                       OR decision_json->>'FingerprintHash' !~ '^[0-9a-f]{64}$'
                       OR pg_catalog.jsonb_typeof(decision_json->'ConsentScopeHash') NOT IN ('string','null')
                       OR (pg_catalog.jsonb_typeof(decision_json->'ConsentScopeHash') = 'string'
                           AND decision_json->>'ConsentScopeHash' !~ '^[0-9a-f]{64}$')
                    THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END IF;

                    FOR item IN SELECT value FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses')
                    LOOP
                        IF pg_catalog.jsonb_typeof(item) <> 'object'
                           OR NOT item ?& ARRAY['Ordinal','Cause']
                           OR EXISTS (
                               SELECT 1 FROM pg_catalog.jsonb_object_keys(item) AS key_name
                               WHERE key_name <> ALL(ARRAY['Ordinal','Cause']))
                           OR pg_catalog.jsonb_typeof(item->'Ordinal') <> 'number'
                           OR item->>'Ordinal' !~ integer_pattern
                           OR pg_catalog.jsonb_typeof(item->'Cause') <> 'string'
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOR item IN SELECT value FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs')
                    LOOP
                        IF pg_catalog.jsonb_typeof(item) <> 'object'
                           OR NOT item ?& ARRAY[
                               'Ordinal','RequirementType','FulfillmentEventId','Revision',
                               'ArtifactRef','ArtifactVersion','ValidUntilUtc']
                           OR EXISTS (
                               SELECT 1 FROM pg_catalog.jsonb_object_keys(item) AS key_name
                               WHERE key_name <> ALL(ARRAY[
                                   'Ordinal','RequirementType','FulfillmentEventId','Revision',
                                   'ArtifactRef','ArtifactVersion','ValidUntilUtc']))
                           OR pg_catalog.jsonb_typeof(item->'Ordinal') <> 'number'
                           OR item->>'Ordinal' !~ integer_pattern
                           OR pg_catalog.jsonb_typeof(item->'RequirementType') <> 'string'
                           OR pg_catalog.jsonb_typeof(item->'FulfillmentEventId') <> 'string'
                           OR item->>'FulfillmentEventId' !~ uuid_pattern
                           OR pg_catalog.jsonb_typeof(item->'Revision') <> 'number'
                           OR item->>'Revision' !~ integer_pattern
                           OR pg_catalog.jsonb_typeof(item->'ArtifactRef') <> 'string'
                           OR pg_catalog.jsonb_typeof(item->'ArtifactVersion') <> 'string'
                           OR pg_catalog.jsonb_typeof(item->'ValidUntilUtc') NOT IN ('string','null')
                           OR (pg_catalog.jsonb_typeof(item->'ValidUntilUtc') = 'string'
                               AND item->>'ValidUntilUtc' !~ timestamp_pattern)
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;
                    FOR item IN SELECT value FROM pg_catalog.jsonb_array_elements(payload->'Classes')
                    LOOP
                        IF pg_catalog.jsonb_typeof(item) <> 'object'
                           OR NOT item ?& ARRAY['ClassKind','RawClass','Ordinal']
                           OR EXISTS (
                               SELECT 1 FROM pg_catalog.jsonb_object_keys(item) AS key_name
                               WHERE key_name <> ALL(ARRAY['ClassKind','RawClass','Ordinal']))
                           OR pg_catalog.jsonb_typeof(item->'ClassKind') <> 'string'
                           OR pg_catalog.jsonb_typeof(item->'RawClass') <> 'string'
                           OR pg_catalog.jsonb_typeof(item->'Ordinal') <> 'number'
                           OR item->>'Ordinal' !~ integer_pattern
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END LOOP;

                    IF payload ? 'Permit' THEN
                        IF pg_catalog.jsonb_typeof(payload->'Permit') <> 'object' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                        permit_json := payload->'Permit';
                        IF NOT permit_json ?& permit_keys
                           OR EXISTS (
                               SELECT 1 FROM pg_catalog.jsonb_object_keys(permit_json) AS key_name
                               WHERE key_name <> ALL(permit_keys))
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                        FOREACH field_name IN ARRAY ARRAY[
                            'PermitId','ResolvedVerificationSessionId','PolicyId',
                            'RecipientClientApplicationId']
                        LOOP
                            IF pg_catalog.jsonb_typeof(permit_json->field_name) <> 'string'
                               OR permit_json->>field_name !~ uuid_pattern THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                            END IF;
                        END LOOP;
                        IF pg_catalog.jsonb_typeof(permit_json->'SubjectRef') <> 'string'
                           OR pg_catalog.jsonb_typeof(permit_json->'PurposeCode') <> 'string'
                           OR pg_catalog.jsonb_typeof(permit_json->'PolicyVersion') <> 'number'
                           OR permit_json->>'PolicyVersion' !~ integer_pattern
                           OR pg_catalog.jsonb_typeof(permit_json->'SchemaVersion') <> 'number'
                           OR permit_json->>'SchemaVersion' !~ integer_pattern
                           OR pg_catalog.jsonb_typeof(permit_json->'DecisionExpiresAtUtc') <> 'string'
                           OR permit_json->>'DecisionExpiresAtUtc' !~ timestamp_pattern
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                    END IF;
                    IF payload ? 'PermitClasses' THEN
                        FOR item IN SELECT value FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses')
                        LOOP
                            IF pg_catalog.jsonb_typeof(item) <> 'object'
                               OR NOT item ?& ARRAY['RawClass','Ordinal']
                               OR EXISTS (
                                   SELECT 1 FROM pg_catalog.jsonb_object_keys(item) AS key_name
                                   WHERE key_name <> ALL(ARRAY['RawClass','Ordinal']))
                               OR pg_catalog.jsonb_typeof(item->'RawClass') <> 'string'
                               OR pg_catalog.jsonb_typeof(item->'Ordinal') <> 'number'
                               OR item->>'Ordinal' !~ integer_pattern
                            THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                            END IF;
                        END LOOP;
                    END IF;

                    -- Every cast is below the lexical gate and inside a controlled exception block.
                    BEGIN
                        decision_id := (payload->>'ExportDecisionId')::uuid;
                        identity_principal_id := (identity_json->>'PrincipalId')::uuid;
                        identity_client_id := (identity_json->>'ClientApplicationId')::uuid;
                        identity_session_id := (identity_json->>'RequestedVerificationSessionId')::uuid;
                        decision_principal_id := (decision_json->>'PrincipalId')::uuid;
                        decision_client_id := (decision_json->>'ClientApplicationId')::uuid;
                        decision_session_id := (decision_json->>'RequestedVerificationSessionId')::uuid;
                        fingerprint_hash := pg_catalog.decode(decision_json->>'FingerprintHash','hex');
                        IF pg_catalog.octet_length(fingerprint_hash) <> 32 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                        IF decision_json->>'ConsentScopeHash' IS NOT NULL
                           AND pg_catalog.octet_length(
                               pg_catalog.decode(decision_json->>'ConsentScopeHash','hex')) <> 32 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                        END IF;
                        FOREACH field_name IN ARRAY ARRAY[
                            'ApiKeyId','PolicyId','ResolvedVerificationSessionId',
                            'SessionOwnerClientApplicationId','GrantPrincipalId','GrantPolicyId',
                            'LifecyclePolicyId','RecipientClientApplicationId','SubjectConsentRecordId']
                        LOOP
                            IF decision_json->>field_name IS NOT NULL THEN
                                PERFORM (decision_json->>field_name)::uuid;
                            END IF;
                        END LOOP;
                        FOREACH field_name IN ARRAY ARRAY[
                            'PolicyVersion','BoundRuleSetVersion','CurrentRuleSetVersion',
                            'GrantPolicyVersion','GrantRevision','LifecyclePolicyVersion',
                            'LifecycleRevision','ConsentRevision','PolicyPermitTtlSeconds']
                        LOOP
                            IF decision_json->>field_name IS NOT NULL THEN
                                PERFORM (decision_json->>field_name)::integer;
                            END IF;
                        END LOOP;
                        FOREACH field_name IN ARRAY ARRAY[
                            'EligibilityEvaluatedAtUtc','ConsentValidFromUtc','ConsentValidUntilUtc',
                            'ConsentEvaluatedAtUtc','DecisionExpiresAtUtc']
                        LOOP
                            IF decision_json->>field_name IS NOT NULL THEN
                                PERFORM (decision_json->>field_name)::timestamptz;
                            END IF;
                        END LOOP;
                        FOR item IN SELECT value
                            FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses')
                        LOOP
                            PERFORM (item->>'Ordinal')::integer;
                        END LOOP;
                        FOR item IN SELECT value
                            FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs')
                        LOOP
                            PERFORM (item->>'Ordinal')::integer;
                            PERFORM (item->>'FulfillmentEventId')::uuid;
                            PERFORM (item->>'Revision')::integer;
                            IF item->>'ValidUntilUtc' IS NOT NULL THEN
                                PERFORM (item->>'ValidUntilUtc')::timestamptz;
                            END IF;
                        END LOOP;
                        FOR item IN SELECT value
                            FROM pg_catalog.jsonb_array_elements(payload->'Classes')
                        LOOP
                            PERFORM (item->>'Ordinal')::integer;
                        END LOOP;
                        IF permit_json IS NOT NULL THEN
                            FOREACH field_name IN ARRAY ARRAY[
                                'PermitId','ResolvedVerificationSessionId','PolicyId',
                                'RecipientClientApplicationId']
                            LOOP
                                PERFORM (permit_json->>field_name)::uuid;
                            END LOOP;
                            PERFORM (permit_json->>'PolicyVersion')::integer;
                            PERFORM (permit_json->>'SchemaVersion')::integer;
                            PERFORM (permit_json->>'DecisionExpiresAtUtc')::timestamptz;
                        END IF;
                        IF payload ? 'PermitClasses' THEN
                            FOR item IN SELECT value
                                FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses')
                            LOOP
                                PERFORM (item->>'Ordinal')::integer;
                            END LOOP;
                        END IF;
                    EXCEPTION
                        WHEN invalid_text_representation
                          OR invalid_datetime_format
                          OR datetime_field_overflow
                          OR invalid_parameter_value
                          OR numeric_value_out_of_range THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID';
                    END;

                    -- Phase 2: idempotency, payload identity copies, actor, then existing decision.
                        SELECT claim."FingerprintHash", claim."ExportDecisionId"
                        INTO stored_fingerprint, stored_decision_id
                        FROM tagekyc.raw_export_authorization_idempotency AS claim
                        WHERE claim."PrincipalId" = identity_principal_id
                          AND claim."ClientApplicationId" = identity_client_id
                          AND claim."RequestedVerificationSessionId" = identity_session_id
                          AND claim."IdempotencyKey" = identity_json->>'IdempotencyKey';
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISSING';
                        END IF;
                        IF stored_fingerprint IS DISTINCT FROM fingerprint_hash
                           OR stored_decision_id IS DISTINCT FROM decision_id
                           OR decision_principal_id IS DISTINCT FROM identity_principal_id
                           OR decision_client_id IS DISTINCT FROM identity_client_id
                           OR decision_session_id IS DISTINCT FROM identity_session_id
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISMATCH';
                        END IF;

                        actor_id := tagekyc.raw_export_current_actor();
                        IF actor_id IS DISTINCT FROM identity_principal_id
                           OR actor_id IS DISTINCT FROM decision_principal_id THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH';
                        END IF;
                        IF EXISTS (
                            SELECT 1 FROM tagekyc.raw_export_authorization_decisions
                            WHERE "ExportDecisionId" = decision_id) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_DECISION_EXISTS';
                        END IF;

                        -- Phase 3: closed vocabularies, uniqueness, contiguous ordinals, and oracle.
                        IF decision_json->>'RawClassSelectionMode' NOT IN ('DefaultPolicySet','ExplicitSubset')
                           OR decision_json->>'Outcome' NOT IN ('Authorized','Denied')
                           OR (decision_json->>'PrimaryCause' IS NOT NULL AND decision_json->>'PrimaryCause' NOT IN (
                               'SESSION_NOT_FOUND','SESSION_NOT_OWNED','SESSION_NOT_COMPLETED',
                               'EXPORT_ELIGIBILITY_INACTIVE','POLICY_PERMIT_TTL_INVALID',
                               'REQUESTED_RAW_CLASSES_NOT_ALLOWED','SUBJECT_CONSENT_NOT_EFFECTIVE',
                               'SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT'))
                           OR (decision_json->>'SessionState' IS NOT NULL AND decision_json->>'SessionState' NOT IN (
                               'Created','InProgress','ReadyToComplete','Completed','Expired','Cancelled','TechnicalTerminal'))
                           OR (decision_json->>'EligibilityPrimaryCause' IS NOT NULL
                               AND decision_json->>'EligibilityPrimaryCause' NOT IN (
                                   'GrantMissing','GrantRevoked','PolicyNotActive','PolicyRevoked',
                                   'PolicySuspended','NotCatalogApproved','StaleRuleSet','MissingOrInvalidFulfillment'))
                           OR (decision_json->>'SubjectConsentCause' IS NOT NULL
                               AND decision_json->>'SubjectConsentCause' NOT IN ('Missing','Withdrawn','Expired'))
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses') AS entry
                            WHERE entry->>'Cause' NOT IN (
                                'GrantMissing','GrantRevoked','PolicyNotActive','PolicyRevoked',
                                'PolicySuspended','NotCatalogApproved','StaleRuleSet','MissingOrInvalidFulfillment'))
                           OR EXISTS (
                            SELECT 1 FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs') AS entry
                            WHERE entry->>'RequirementType' NOT IN (
                                'LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule'))
                           OR EXISTS (
                            SELECT 1 FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry
                            WHERE entry->>'ClassKind' NOT IN (
                                      'PolicyAllowed','Requested','Effective','Consented','Authorized')
                               OR entry->>'RawClass' NOT IN (
                                      'ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod',
                                      'AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage'))
                           OR (payload ? 'PermitClasses' AND EXISTS (
                            SELECT 1 FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses') AS entry
                            WHERE entry->>'RawClass' NOT IN (
                                'ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod',
                                'AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')))
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses') AS entry
                            GROUP BY entry->>'Cause' HAVING count(*) > 1)
                           OR EXISTS (
                            SELECT 1
                            FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs') AS entry
                            GROUP BY entry->>'RequirementType' HAVING count(*) > 1)
                           OR EXISTS (
                            SELECT 1
                            FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry
                            GROUP BY entry->>'ClassKind', entry->>'RawClass' HAVING count(*) > 1)
                           OR (payload ? 'PermitClasses' AND EXISTS (
                            SELECT 1
                            FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses') AS entry
                            GROUP BY entry->>'RawClass' HAVING count(*) > 1))
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM (
                                SELECT count(*) AS item_count,
                                       count(DISTINCT (entry->>'Ordinal')::integer) AS ordinal_count,
                                       min((entry->>'Ordinal')::integer) AS min_ordinal,
                                       max((entry->>'Ordinal')::integer) AS max_ordinal
                                FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses') AS entry
                            ) AS shape
                            WHERE item_count > 0 AND (
                                ordinal_count <> item_count OR min_ordinal <> 0 OR max_ordinal <> item_count - 1))
                           OR EXISTS (
                            SELECT 1 FROM (
                                SELECT count(*) AS item_count,
                                       count(DISTINCT (entry->>'Ordinal')::integer) AS ordinal_count,
                                       min((entry->>'Ordinal')::integer) AS min_ordinal,
                                       max((entry->>'Ordinal')::integer) AS max_ordinal
                                FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs') AS entry
                            ) AS shape
                            WHERE item_count > 0 AND (
                                ordinal_count <> item_count OR min_ordinal <> 0 OR max_ordinal <> item_count - 1))
                           OR EXISTS (
                            SELECT 1 FROM (
                                SELECT entry->>'ClassKind' AS class_kind,
                                       count(*) AS item_count,
                                       count(DISTINCT (entry->>'Ordinal')::integer) AS ordinal_count,
                                       min((entry->>'Ordinal')::integer) AS min_ordinal,
                                       max((entry->>'Ordinal')::integer) AS max_ordinal
                                FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry
                                GROUP BY entry->>'ClassKind'
                            ) AS shape
                            WHERE ordinal_count <> item_count OR min_ordinal <> 0 OR max_ordinal <> item_count - 1)
                           OR (payload ? 'PermitClasses' AND EXISTS (
                            SELECT 1 FROM (
                                SELECT count(*) AS item_count,
                                       count(DISTINCT (entry->>'Ordinal')::integer) AS ordinal_count,
                                       min((entry->>'Ordinal')::integer) AS min_ordinal,
                                       max((entry->>'Ordinal')::integer) AS max_ordinal
                                FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses') AS entry
                            ) AS shape
                            WHERE item_count > 0 AND (
                                ordinal_count <> item_count OR min_ordinal <> 0 OR max_ordinal <> item_count - 1)))
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        SELECT pg_catalog.jsonb_array_length(payload->'EligibilityCauses'),
                               pg_catalog.jsonb_array_length(payload->'FulfillmentRefs')
                        INTO eligibility_count, fulfillment_count;
                        SELECT count(*) FILTER (WHERE entry->>'ClassKind' = 'PolicyAllowed')::integer,
                               count(*) FILTER (WHERE entry->>'ClassKind' = 'Requested')::integer,
                               count(*) FILTER (WHERE entry->>'ClassKind' = 'Effective')::integer,
                               count(*) FILTER (WHERE entry->>'ClassKind' = 'Consented')::integer,
                               count(*) FILTER (WHERE entry->>'ClassKind' = 'Authorized')::integer
                        INTO policy_count, requested_count, effective_count, consented_count, authorized_count
                        FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry;
                        permit_class_count := CASE WHEN payload ? 'PermitClasses'
                            THEN pg_catalog.jsonb_array_length(payload->'PermitClasses') ELSE 0 END;

                        requested_shape_valid :=
                            (decision_json->>'RawClassSelectionMode' = 'DefaultPolicySet' AND requested_count = 0)
                            OR (decision_json->>'RawClassSelectionMode' = 'ExplicitSubset' AND requested_count > 0);
                        IF NOT requested_shape_valid THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        IF decision_json->>'EligibilityPrimaryCause' IS NOT NULL
                           AND decision_json->>'EligibilityPrimaryCause' IS DISTINCT FROM (
                               SELECT entry->>'Cause'
                               FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses') AS entry
                               WHERE (entry->>'Ordinal')::integer = 0)
                        THEN
                            RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                        END IF;

                        session_none :=
                            decision_json->>'ResolvedVerificationSessionId' IS NULL
                            AND decision_json->>'SessionOwnerClientApplicationId' IS NULL
                            AND decision_json->>'SessionState' IS NULL
                            AND decision_json->>'SessionSubjectRef' IS NULL;
                        session_owned_without_subject :=
                            decision_json->>'ResolvedVerificationSessionId' IS NOT NULL
                            AND decision_json->>'SessionOwnerClientApplicationId' IS NOT NULL
                            AND decision_json->>'SessionState' IS NOT NULL
                            AND decision_json->>'SessionSubjectRef' IS NULL;
                        session_required :=
                            decision_json->>'ResolvedVerificationSessionId' IS NOT NULL
                            AND decision_json->>'SessionOwnerClientApplicationId' IS NOT NULL
                            AND decision_json->>'SessionState' IS NOT NULL
                            AND decision_json->>'SessionSubjectRef' IS NOT NULL;
                        b1_core_none :=
                            decision_json->>'BoundRuleSetVersion' IS NULL
                            AND decision_json->>'CurrentRuleSetVersion' IS NULL
                            AND decision_json->>'EligibilityEvaluatedAtUtc' IS NULL;
                        b1_core_required :=
                            decision_json->>'BoundRuleSetVersion' IS NOT NULL
                            AND decision_json->>'CurrentRuleSetVersion' IS NOT NULL
                            AND decision_json->>'EligibilityEvaluatedAtUtc' IS NOT NULL;
                        active_refs_none :=
                            decision_json->>'GrantPrincipalId' IS NULL
                            AND decision_json->>'GrantPolicyId' IS NULL
                            AND decision_json->>'GrantPolicyVersion' IS NULL
                            AND decision_json->>'GrantRevision' IS NULL
                            AND decision_json->>'LifecyclePolicyId' IS NULL
                            AND decision_json->>'LifecyclePolicyVersion' IS NULL
                            AND decision_json->>'LifecycleRevision' IS NULL;
                        active_refs_required :=
                            decision_json->>'GrantPrincipalId' IS NOT NULL
                            AND decision_json->>'GrantPolicyId' IS NOT NULL
                            AND decision_json->>'GrantPolicyVersion' IS NOT NULL
                            AND decision_json->>'GrantRevision' IS NOT NULL
                            AND decision_json->>'LifecyclePolicyId' IS NOT NULL
                            AND decision_json->>'LifecyclePolicyVersion' IS NOT NULL
                            AND decision_json->>'LifecycleRevision' IS NOT NULL;
                        b2_scope_none :=
                            decision_json->>'PurposeCode' IS NULL
                            AND decision_json->>'RecipientClientApplicationId' IS NULL
                            AND decision_json->>'ConsentEvaluatedAtUtc' IS NULL;
                        b2_scope_required :=
                            decision_json->>'PurposeCode' IS NOT NULL
                            AND decision_json->>'RecipientClientApplicationId' IS NOT NULL
                            AND decision_json->>'ConsentEvaluatedAtUtc' IS NOT NULL;
                        b2_ref_none :=
                            decision_json->>'ConsentScopeHash' IS NULL
                            AND decision_json->>'SubjectConsentRecordId' IS NULL
                            AND decision_json->>'ConsentRevision' IS NULL
                            AND decision_json->>'ConsentValidFromUtc' IS NULL;
                        b2_ref_required :=
                            decision_json->>'ConsentScopeHash' IS NOT NULL
                            AND decision_json->>'SubjectConsentRecordId' IS NOT NULL
                            AND decision_json->>'ConsentRevision' IS NOT NULL
                            AND decision_json->>'ConsentValidFromUtc' IS NOT NULL;
                        b2_all_none := b2_scope_none AND b2_ref_none
                            AND decision_json->>'SubjectConsentCause' IS NULL
                            AND decision_json->>'ConsentValidUntilUtc' IS NULL;

                        IF decision_json->>'Outcome' = 'Denied' THEN
                            IF payload ? 'Permit' OR payload ? 'PermitClasses'
                               OR authorized_count <> 0
                               OR decision_json->>'DecisionExpiresAtUtc' IS NOT NULL
                            THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                            END IF;

                            CASE decision_json->>'PrimaryCause'
                                WHEN 'SESSION_NOT_FOUND' THEN
                                    IF NOT (session_none AND b1_core_none
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_none AND fulfillment_count = 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NULL AND b2_all_none
                                        AND policy_count = 0 AND effective_count = 0 AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'SESSION_NOT_OWNED' THEN
                                    IF NOT (session_owned_without_subject AND b1_core_none
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_none AND fulfillment_count = 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NULL AND b2_all_none
                                        AND policy_count = 0 AND effective_count = 0 AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'SESSION_NOT_COMPLETED' THEN
                                    IF NOT (session_required AND b1_core_none
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_none AND fulfillment_count = 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NULL AND b2_all_none
                                        AND policy_count = 0 AND effective_count = 0 AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'EXPORT_ELIGIBILITY_INACTIVE' THEN
                                    IF NOT (session_required AND b1_core_required
                                        AND decision_json->>'EligibilityPrimaryCause' IS NOT NULL
                                        AND eligibility_count > 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NULL AND b2_all_none
                                        AND policy_count = 0 AND effective_count = 0 AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'POLICY_PERMIT_TTL_INVALID' THEN
                                    IF NOT (session_required AND b1_core_required
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_required AND fulfillment_count > 0
                                        AND b2_all_none AND policy_count > 0
                                        AND effective_count = 0 AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'REQUESTED_RAW_CLASSES_NOT_ALLOWED' THEN
                                    IF NOT (session_required AND b1_core_required
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_required AND fulfillment_count > 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NOT NULL
                                        AND b2_all_none AND policy_count > 0 AND effective_count > 0
                                        AND consented_count = 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'SUBJECT_CONSENT_NOT_EFFECTIVE' THEN
                                    IF NOT (session_required AND b1_core_required
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_required AND fulfillment_count > 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NOT NULL
                                        AND b2_scope_required
                                        AND decision_json->>'SubjectConsentCause' IS NOT NULL
                                        AND policy_count > 0 AND effective_count > 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                WHEN 'SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT' THEN
                                    IF NOT (session_required AND b1_core_required
                                        AND decision_json->>'EligibilityPrimaryCause' IS NULL
                                        AND eligibility_count = 0 AND active_refs_required AND fulfillment_count > 0
                                        AND decision_json->>'PolicyPermitTtlSeconds' IS NOT NULL
                                        AND b2_scope_required AND b2_ref_required
                                        AND decision_json->>'SubjectConsentCause' IS NULL
                                        AND policy_count > 0 AND effective_count > 0 AND consented_count > 0)
                                    THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID'; END IF;
                                ELSE
                                    RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                            END CASE;
                        ELSE
                            -- Authorized over-population is EVIDENCE_SHAPE_INVALID.
                            IF decision_json->>'PrimaryCause' IS NOT NULL
                               OR decision_json->>'EligibilityPrimaryCause' IS NOT NULL
                               OR eligibility_count <> 0
                               OR decision_json->>'SubjectConsentCause' IS NOT NULL
                            THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                            END IF;
                            -- Authorized under-population, including absent/empty PermitClasses, has its own code.
                            IF NOT (session_required AND b1_core_required AND active_refs_required
                                AND fulfillment_count > 0
                                AND decision_json->>'PolicyPermitTtlSeconds' IS NOT NULL
                                AND b2_scope_required AND b2_ref_required
                                AND policy_count > 0 AND effective_count > 0
                                AND consented_count > 0 AND authorized_count > 0
                                AND decision_json->>'DecisionExpiresAtUtc' IS NOT NULL
                                AND payload ? 'Permit' AND payload ? 'PermitClasses'
                                AND permit_class_count > 0)
                            THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_AUTHORIZED_EVIDENCE_INCOMPLETE';
                            END IF;

                            IF (permit_json->>'ResolvedVerificationSessionId')::uuid
                                   IS DISTINCT FROM (decision_json->>'ResolvedVerificationSessionId')::uuid
                               OR permit_json->>'SubjectRef'
                                   IS DISTINCT FROM decision_json->>'SessionSubjectRef'
                               OR (permit_json->>'PolicyId')::uuid
                                   IS DISTINCT FROM (decision_json->>'PolicyId')::uuid
                               OR (permit_json->>'PolicyVersion')::integer
                                   IS DISTINCT FROM (decision_json->>'PolicyVersion')::integer
                               OR permit_json->>'PurposeCode'
                                   IS DISTINCT FROM decision_json->>'PurposeCode'
                               OR (permit_json->>'RecipientClientApplicationId')::uuid
                                   IS DISTINCT FROM (decision_json->>'RecipientClientApplicationId')::uuid
                               OR (permit_json->>'DecisionExpiresAtUtc')::timestamptz
                                   IS DISTINCT FROM (decision_json->>'DecisionExpiresAtUtc')::timestamptz
                               OR (permit_json->>'SchemaVersion')::integer <> 1
                            THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                            END IF;

                            SELECT pg_catalog.jsonb_agg(
                                       pg_catalog.jsonb_build_array(
                                           entry->>'RawClass',(entry->>'Ordinal')::integer)
                                       ORDER BY (entry->>'Ordinal')::integer)
                            INTO authorized_sequence
                            FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry
                            WHERE entry->>'ClassKind' = 'Authorized';
                            SELECT pg_catalog.jsonb_agg(
                                       pg_catalog.jsonb_build_array(
                                           entry->>'RawClass',(entry->>'Ordinal')::integer)
                                       ORDER BY (entry->>'Ordinal')::integer)
                            INTO permit_sequence
                            FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses') AS entry;
                            IF authorized_sequence IS DISTINCT FROM permit_sequence THEN
                                RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID';
                            END IF;
                        END IF;

                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_authorization_insert_context','decision',true);
                        INSERT INTO tagekyc.raw_export_authorization_decisions
                        ("ExportDecisionId","PrincipalId","ClientApplicationId","ApiKeyId",
                         "RequestedVerificationSessionId","PolicyId","PolicyVersion","FingerprintHash",
                         "RawClassSelectionMode","Outcome","PrimaryCause","ResolvedVerificationSessionId",
                         "SessionOwnerClientApplicationId","SessionSubjectRef","SessionState",
                         "BoundRuleSetVersion","CurrentRuleSetVersion","EligibilityPrimaryCause",
                         "EligibilityEvaluatedAtUtc","GrantPrincipalId","GrantPolicyId","GrantPolicyVersion",
                         "GrantRevision","LifecyclePolicyId","LifecyclePolicyVersion","LifecycleRevision",
                         "PurposeCode","RecipientClientApplicationId","SubjectConsentCause","ConsentScopeHash",
                         "SubjectConsentRecordId","ConsentRevision","ConsentValidFromUtc","ConsentValidUntilUtc",
                         "ConsentEvaluatedAtUtc","PolicyPermitTtlSeconds","DecisionExpiresAtUtc","DecidedAtUtc")
                        VALUES
                        (decision_id,decision_principal_id,decision_client_id,
                         (decision_json->>'ApiKeyId')::uuid,decision_session_id,
                         (decision_json->>'PolicyId')::uuid,(decision_json->>'PolicyVersion')::integer,
                         fingerprint_hash,decision_json->>'RawClassSelectionMode',decision_json->>'Outcome',
                         decision_json->>'PrimaryCause',(decision_json->>'ResolvedVerificationSessionId')::uuid,
                         (decision_json->>'SessionOwnerClientApplicationId')::uuid,
                         decision_json->>'SessionSubjectRef',decision_json->>'SessionState',
                         (decision_json->>'BoundRuleSetVersion')::integer,
                         (decision_json->>'CurrentRuleSetVersion')::integer,
                         decision_json->>'EligibilityPrimaryCause',
                         (decision_json->>'EligibilityEvaluatedAtUtc')::timestamptz,
                         (decision_json->>'GrantPrincipalId')::uuid,
                         (decision_json->>'GrantPolicyId')::uuid,
                         (decision_json->>'GrantPolicyVersion')::integer,
                         (decision_json->>'GrantRevision')::integer,
                         (decision_json->>'LifecyclePolicyId')::uuid,
                         (decision_json->>'LifecyclePolicyVersion')::integer,
                         (decision_json->>'LifecycleRevision')::integer,
                         decision_json->>'PurposeCode',
                         (decision_json->>'RecipientClientApplicationId')::uuid,
                         decision_json->>'SubjectConsentCause',
                         CASE WHEN decision_json->>'ConsentScopeHash' IS NULL THEN NULL
                              ELSE pg_catalog.decode(decision_json->>'ConsentScopeHash','hex') END,
                         (decision_json->>'SubjectConsentRecordId')::uuid,
                         (decision_json->>'ConsentRevision')::integer,
                         (decision_json->>'ConsentValidFromUtc')::timestamptz,
                         (decision_json->>'ConsentValidUntilUtc')::timestamptz,
                         (decision_json->>'ConsentEvaluatedAtUtc')::timestamptz,
                         (decision_json->>'PolicyPermitTtlSeconds')::integer,
                         (decision_json->>'DecisionExpiresAtUtc')::timestamptz,
                         pg_catalog.transaction_timestamp());

                        IF eligibility_count > 0 THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_authorization_insert_context','eligibility_cause',true);
                            INSERT INTO tagekyc.raw_export_decision_eligibility_causes
                                ("ExportDecisionId","Ordinal","Cause")
                            SELECT decision_id,(entry->>'Ordinal')::integer,entry->>'Cause'
                            FROM pg_catalog.jsonb_array_elements(payload->'EligibilityCauses') AS entry
                            ORDER BY (entry->>'Ordinal')::integer;
                        END IF;
                        IF fulfillment_count > 0 THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_authorization_insert_context','fulfillment_ref',true);
                            INSERT INTO tagekyc.raw_export_decision_fulfillment_refs
                                ("ExportDecisionId","RequirementType","Ordinal","FulfillmentEventId",
                                 "Revision","ArtifactRef","ArtifactVersion","ValidUntilUtc")
                            SELECT decision_id,entry->>'RequirementType',(entry->>'Ordinal')::integer,
                                   (entry->>'FulfillmentEventId')::uuid,(entry->>'Revision')::integer,
                                   entry->>'ArtifactRef',entry->>'ArtifactVersion',
                                   (entry->>'ValidUntilUtc')::timestamptz
                            FROM pg_catalog.jsonb_array_elements(payload->'FulfillmentRefs') AS entry
                            ORDER BY (entry->>'Ordinal')::integer;
                        END IF;
                        IF pg_catalog.jsonb_array_length(payload->'Classes') > 0 THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_authorization_insert_context','decision_class',true);
                            INSERT INTO tagekyc.raw_export_decision_classes
                                ("ExportDecisionId","ClassKind","RawClass","Ordinal")
                            SELECT decision_id,entry->>'ClassKind',entry->>'RawClass',
                                   (entry->>'Ordinal')::integer
                            FROM pg_catalog.jsonb_array_elements(payload->'Classes') AS entry
                            ORDER BY entry->>'ClassKind',(entry->>'Ordinal')::integer;
                        END IF;
                        IF decision_json->>'Outcome' = 'Authorized' THEN
                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_authorization_insert_context','permit',true);
                            INSERT INTO tagekyc.raw_export_authorization_permits
                                ("PermitId","AuthorizationDecisionId","ResolvedVerificationSessionId",
                                 "SubjectRef","PolicyId","PolicyVersion","PurposeCode",
                                 "RecipientClientApplicationId","DecisionExpiresAtUtc","SchemaVersion","CreatedAt")
                            VALUES
                                ((permit_json->>'PermitId')::uuid,decision_id,
                                 (permit_json->>'ResolvedVerificationSessionId')::uuid,
                                 permit_json->>'SubjectRef',(permit_json->>'PolicyId')::uuid,
                                 (permit_json->>'PolicyVersion')::integer,permit_json->>'PurposeCode',
                                 (permit_json->>'RecipientClientApplicationId')::uuid,
                                 (permit_json->>'DecisionExpiresAtUtc')::timestamptz,
                                 (permit_json->>'SchemaVersion')::integer,
                                 pg_catalog.transaction_timestamp());

                            PERFORM pg_catalog.set_config(
                                'tagekyc.raw_export_authorization_insert_context','permit_class',true);
                            INSERT INTO tagekyc.raw_export_permit_classes
                                ("PermitId","RawClass","Ordinal")
                            SELECT (permit_json->>'PermitId')::uuid,entry->>'RawClass',
                                   (entry->>'Ordinal')::integer
                            FROM pg_catalog.jsonb_array_elements(payload->'PermitClasses') AS entry
                            ORDER BY (entry->>'Ordinal')::integer;
                        END IF;
                        RETURN decision_id;
                END;
                $$;

                ALTER FUNCTION tagekyc.raw_export_persist_authorization_decision(jsonb)
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_persist_authorization_decision(jsonb)
                    FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_persist_authorization_decision(jsonb)
                    TO tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE EXECUTE ON FUNCTION
                    tagekyc.raw_export_persist_authorization_decision(jsonb)
                FROM tagekyc_runtime;
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_persist_authorization_decision(jsonb);
                """);
        }
    }
}
