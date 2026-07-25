using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B1E3ResolverReadBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT SELECT ON tagekyc.raw_export_policy_allowed_classes
                TO tagekyc_raw_export_deployer;

                DO $migration$
                DECLARE
                    actual_body_hash text;
                BEGIN
                    SELECT pg_catalog.md5(p.prosrc)
                    INTO actual_body_hash
                    FROM pg_catalog.pg_proc AS p
                    WHERE p.oid =
                        'tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz)'::regprocedure;
                    IF actual_body_hash IS DISTINCT FROM '5084283e3d63b43b24595cbdce920ebc' THEN
                        RAISE EXCEPTION 'TIP88B1E3_B2_CONSENT_PRE_BODY_MISMATCH';
                    END IF;
                END;
                $migration$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_granted(
                    verification_session_id uuid,
                    policy_id uuid,
                    policy_version integer,
                    raw_classes text[],
                    consent_text_version text,
                    consent_text_content_hash text,
                    external_consent_artifact_ref text,
                    decision_ref text,
                    valid_until_utc timestamptz)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    actor_id uuid;
                    session_subject text;
                    session_owner uuid;
                    session_state text;
                    scope_hash bytea;
                    current_revision integer;
                    next_revision integer;
                    record_id uuid;
                    raw_class text;
                BEGIN
                    SET CONSTRAINTS
                        tagekyc.tr_raw_export_subject_consent_granted_classes_required
                        DEFERRED;

                    actor_id := tagekyc.raw_export_current_actor();
                    SELECT "SubjectRef", "ClientApplicationId", "State"
                    INTO session_subject, session_owner, session_state
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;
                    IF session_state <> 'Completed' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SESSION_NOT_COMPLETED';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentRecorder'));
                    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentRecorder') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF raw_classes IS NULL OR pg_catalog.cardinality(raw_classes) = 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED';
                    END IF;
                    IF (SELECT pg_catalog.count(DISTINCT item) FROM pg_catalog.unnest(raw_classes) AS item) <> pg_catalog.cardinality(raw_classes) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_DUPLICATE';
                    END IF;
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.unnest(raw_classes) AS item
                        WHERE item NOT IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_RAW_CLASS_INVALID';
                    END IF;
                    IF consent_text_version IS NULL OR pg_catalog.btrim(consent_text_version) = ''
                       OR consent_text_content_hash IS NULL OR pg_catalog.btrim(consent_text_content_hash) = ''
                       OR external_consent_artifact_ref IS NULL OR pg_catalog.btrim(external_consent_artifact_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_PROVENANCE_REQUIRED';
                    END IF;
                    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;

                    scope_hash := tagekyc.raw_export_consent_scope_hash(
                        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
                    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
                    SELECT COALESCE(MAX("Revision"), 0)
                    INTO current_revision
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "VerificationSessionId" = verification_session_id
                      AND "SubjectRef" = session_subject
                      AND "PolicyId" = policy_id
                      AND "PolicyVersion" = policy_version
                      AND "PurposeCode" = 'SubjectRawBiometricExport'
                      AND "RecipientClientApplicationId" = session_owner;
                    next_revision := current_revision + 1;
                    record_id := pg_catalog.gen_random_uuid();
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
                    INSERT INTO tagekyc.raw_export_subject_consent_events
                        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                         "Revision","EventType","TargetRevision","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef",
                         "ValidFromUtc","ValidUntilUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (record_id, scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
                         next_revision, 'Granted', NULL, consent_text_version, consent_text_content_hash, external_consent_artifact_ref, decision_ref,
                         pg_catalog.transaction_timestamp(), valid_until_utc, pg_catalog.transaction_timestamp(), actor_id, pg_catalog.transaction_timestamp());
                    FOREACH raw_class IN ARRAY raw_classes LOOP
                        INSERT INTO tagekyc.raw_export_subject_consent_classes
                            ("SubjectConsentRecordId","RawClass","CreatedAt")
                        VALUES
                            (record_id, raw_class, pg_catalog.transaction_timestamp());
                    END LOOP;
                    SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required IMMEDIATE;
                    SET CONSTRAINTS tagekyc.tr_raw_export_subject_consent_granted_classes_required DEFERRED;
                    RETURN next_revision;
                END;
                $function$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_authorization_eligibility_inputs(
                    principal_id uuid,
                    policy_id uuid,
                    policy_version integer)
                RETURNS TABLE (
                    "PolicyId" uuid,
                    "PolicyVersion" integer,
                    "EvaluatedAtUtc" timestamp with time zone,
                    "PolicyExists" boolean,
                    "BoundRuleSetVersion" integer,
                    "CurrentRuleSetVersion" integer,
                    "ClosureType" text,
                    "GrantPrincipalId" uuid,
                    "GrantPolicyId" uuid,
                    "GrantPolicyVersion" integer,
                    "GrantRevision" integer,
                    "GrantEventType" text,
                    "LifecyclePolicyId" uuid,
                    "LifecyclePolicyVersion" integer,
                    "LifecycleRevision" integer,
                    "LifecycleEventType" text,
                    "RequirementOrdinal" integer,
                    "RequirementType" text,
                    "FulfillmentEventId" uuid,
                    "FulfillmentRevision" integer,
                    "FulfillmentEventType" text,
                    "ArtifactRef" text,
                    "ArtifactVersion" text,
                    "ValidFromUtc" timestamp with time zone,
                    "ValidUntilUtc" timestamp with time zone)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    actor_id uuid;
                    lock_requirement text;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF actor_id IS DISTINCT FROM principal_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext(
                            'tip88b1:grant:' || principal_id::text || ':' ||
                            policy_id::text || ':' || policy_version::text));
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                        pg_catalog.hashtext(
                            'tip88b1:lifecycle:' || policy_id::text || ':' ||
                            policy_version::text));

                    FOREACH lock_requirement IN ARRAY ARRAY[
                        'CrossBorderAssessment',
                        'Dpia',
                        'LegalApproval',
                        'RetentionSchedule'
                    ]
                    LOOP
                        PERFORM pg_catalog.pg_advisory_xact_lock_shared(
                            pg_catalog.hashtext(
                                'tip88b1:fulfillment:' || policy_id::text || ':' ||
                                policy_version::text || ':' || lock_requirement));
                    END LOOP;

                    RETURN QUERY
                    WITH policy AS (
                        SELECT p."RequirementRuleSetVersion"
                        FROM tagekyc.raw_export_policy_versions AS p
                        WHERE p."PolicyId" = policy_id
                          AND p."PolicyVersion" = policy_version
                    ),
                    current_rule_set AS (
                        SELECT COALESCE(pg_catalog.max(r."RuleSetVersion"), 0)::integer AS version
                        FROM tagekyc.raw_export_requirement_rule_sets AS r
                        WHERE r."RuleSetId" = 'RAW_EXPORT_REQUIREMENTS'
                    ),
                    closure AS (
                        SELECT c."ClosureType"
                        FROM tagekyc.raw_export_policy_closures AS c
                        WHERE c."PolicyId" = policy_id
                          AND c."PolicyVersion" = policy_version
                    ),
                    grant_event AS (
                        SELECT g."PrincipalId", g."PolicyId", g."PolicyVersion",
                               g."Revision", g."EventType"
                        FROM tagekyc.raw_export_grants AS g
                        WHERE g."PrincipalId" = principal_id
                          AND g."PolicyId" = policy_id
                          AND g."PolicyVersion" = policy_version
                        ORDER BY g."Revision" DESC
                        LIMIT 1
                    ),
                    lifecycle_event AS (
                        SELECT l."PolicyId", l."PolicyVersion", l."Revision", l."EventType"
                        FROM tagekyc.raw_export_policy_lifecycle AS l
                        WHERE l."PolicyId" = policy_id
                          AND l."PolicyVersion" = policy_version
                        ORDER BY l."Revision" DESC
                        LIMIT 1
                    ),
                    requirements AS (
                        SELECT
                            (pg_catalog.row_number() OVER (
                                ORDER BY r."RequirementType") - 1)::integer AS ordinal,
                            r."RequirementType"
                        FROM tagekyc.raw_export_policy_requirements AS r
                        WHERE r."PolicyId" = policy_id
                          AND r."PolicyVersion" = policy_version
                          AND r."RequirementType" <> 'ConsentArtifact'
                    )
                    SELECT
                        policy_id,
                        policy_version,
                        pg_catalog.transaction_timestamp(),
                        p."RequirementRuleSetVersion" IS NOT NULL,
                        p."RequirementRuleSetVersion",
                        cr.version,
                        c."ClosureType"::text,
                        g."PrincipalId",
                        g."PolicyId",
                        g."PolicyVersion",
                        g."Revision",
                        g."EventType"::text,
                        l."PolicyId",
                        l."PolicyVersion",
                        l."Revision",
                        l."EventType"::text,
                        r.ordinal,
                        r."RequirementType"::text,
                        f."FulfillmentEventId",
                        f."Revision",
                        f."EventType"::text,
                        f."ArtifactRef"::text,
                        f."ArtifactVersion"::text,
                        f."ValidFromUtc",
                        f."ValidUntilUtc"
                    FROM (SELECT 1) AS seed
                    CROSS JOIN current_rule_set AS cr
                    LEFT JOIN policy AS p ON TRUE
                    LEFT JOIN closure AS c ON TRUE
                    LEFT JOIN grant_event AS g ON TRUE
                    LEFT JOIN lifecycle_event AS l ON TRUE
                    LEFT JOIN requirements AS r ON TRUE
                    LEFT JOIN LATERAL (
                        SELECT
                            x."FulfillmentEventId",
                            x."Revision",
                            x."EventType",
                            x."ArtifactRef",
                            x."ArtifactVersion",
                            x."ValidFromUtc",
                            x."ValidUntilUtc"
                        FROM tagekyc.raw_export_fulfillments AS x
                        WHERE x."PolicyId" = policy_id
                          AND x."PolicyVersion" = policy_version
                          AND x."RequirementType" = r."RequirementType"
                        ORDER BY x."Revision" DESC
                        LIMIT 1
                    ) AS f ON r."RequirementType" IS NOT NULL
                    ORDER BY r.ordinal NULLS FIRST;
                END;
                $function$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_authorization_policy_inputs(
                    principal_id uuid,
                    policy_id uuid,
                    policy_version integer)
                RETURNS TABLE (
                    "PolicyId" uuid,
                    "PolicyVersion" integer,
                    "EvaluatedAtUtc" timestamp with time zone,
                    "PolicyExists" boolean,
                    "PermitTtlSeconds" integer,
                    "ClosureType" text,
                    "ClassOrdinal" integer,
                    "RawClass" text)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    actor_id uuid;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF actor_id IS DISTINCT FROM principal_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH';
                    END IF;

                    RETURN QUERY
                    WITH policy AS (
                        SELECT p."PolicyId", p."PermitTtlSeconds"
                        FROM tagekyc.raw_export_policy_versions AS p
                        WHERE p."PolicyId" = policy_id
                          AND p."PolicyVersion" = policy_version
                    ),
                    closure AS (
                        SELECT c."ClosureType"
                        FROM tagekyc.raw_export_policy_closures AS c
                        WHERE c."PolicyId" = policy_id
                          AND c."PolicyVersion" = policy_version
                    ),
                    classes AS (
                        SELECT
                            (pg_catalog.row_number() OVER (
                                ORDER BY c."RawClass") - 1)::integer AS ordinal,
                            c."RawClass"
                        FROM tagekyc.raw_export_policy_allowed_classes AS c
                        WHERE c."PolicyId" = policy_id
                          AND c."PolicyVersion" = policy_version
                    )
                    SELECT
                        policy_id,
                        policy_version,
                        pg_catalog.transaction_timestamp(),
                        p."PolicyId" IS NOT NULL,
                        p."PermitTtlSeconds",
                        c."ClosureType"::text,
                        a.ordinal,
                        a."RawClass"::text
                    FROM (SELECT 1) AS seed
                    LEFT JOIN policy AS p ON TRUE
                    LEFT JOIN closure AS c ON TRUE
                    LEFT JOIN classes AS a ON TRUE
                    ORDER BY a.ordinal NULLS FIRST;
                END;
                $function$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_control_plane_root_health()
                RETURNS TABLE (
                    "IsHealthy" boolean,
                    "StatusCode" text)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    required_authority text;
                    active_count integer;
                    has_dev_default boolean;
                BEGIN
                    FOREACH required_authority IN ARRAY ARRAY[
                        'GrantAdmin',
                        'RecorderAuthorityAdmin',
                        'ActivationAuthority'
                    ]
                    LOOP
                        WITH latest AS (
                            SELECT DISTINCT ON (a."PrincipalId")
                                a."PrincipalId",
                                a."EventType"
                            FROM tagekyc.raw_export_control_authorities AS a
                            WHERE a."AuthorityType" = required_authority
                              AND a."ScopeType" = 'Global'
                              AND a."ScopeId" IS NULL
                              AND a."RequirementType" IS NULL
                            ORDER BY a."PrincipalId", a."Revision" DESC
                        )
                        SELECT
                            pg_catalog.count(*)::integer,
                            COALESCE(pg_catalog.bool_or(
                                l."PrincipalId" = '00000000-0000-0000-0000-000000000000'::uuid
                                OR l."PrincipalId" = '00000000-0000-5000-8000-000000088b10'::uuid
                            ), FALSE)
                        INTO active_count, has_dev_default
                        FROM latest AS l
                        WHERE l."EventType" = 'Granted';

                        IF active_count = 0 THEN
                            RETURN QUERY SELECT FALSE, 'PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING'::text;
                            RETURN;
                        END IF;

                        IF has_dev_default THEN
                            RETURN QUERY SELECT FALSE, 'PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT'::text;
                            RETURN;
                        END IF;
                    END LOOP;

                    RETURN QUERY SELECT TRUE, 'OK'::text;
                END;
                $function$;

                DO $migration$
                DECLARE
                    invalid_count integer;
                BEGIN
                    WITH expected(function_oid) AS (
                        VALUES
                            ('tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer)'::regprocedure),
                            ('tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer)'::regprocedure),
                            ('tagekyc.raw_export_control_plane_root_health()'::regprocedure)
                    ),
                    invalid AS (
                        SELECT expected.function_oid
                        FROM expected
                        JOIN pg_catalog.pg_proc AS function
                          ON function.oid = expected.function_oid
                        CROSS JOIN LATERAL (
                            SELECT
                                pg_catalog.count(*) AS row_count,
                                pg_catalog.count(*) FILTER (
                                    WHERE acl.grantee = 0
                                      AND acl.privilege_type = 'EXECUTE'
                                      AND NOT acl.is_grantable) AS expected_count
                            FROM pg_catalog.aclexplode(
                                COALESCE(
                                    function.proacl,
                                    pg_catalog.acldefault(
                                        'f',
                                        function.proowner))) AS acl
                            WHERE acl.grantee <> function.proowner
                        ) AS acl_manifest
                        WHERE acl_manifest.row_count <> 1
                           OR acl_manifest.expected_count <> 1
                    )
                    SELECT pg_catalog.count(*)
                    INTO invalid_count
                    FROM invalid;

                    IF invalid_count <> 0 THEN
                        RAISE EXCEPTION
                            'TIP88B1E3_FUNCTION_ACL_INVALID';
                    END IF;
                END;
                $migration$;

                ALTER FUNCTION tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_control_plane_root_health()
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_control_plane_root_health()
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_control_plane_root_health()
                TO tagekyc_runtime;

                DO $migration$
                DECLARE
                    invalid_count integer;
                BEGIN
                    WITH expected(function_oid) AS (
                        VALUES
                            ('tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer)'::regprocedure),
                            ('tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer)'::regprocedure),
                            ('tagekyc.raw_export_control_plane_root_health()'::regprocedure)
                    ),
                    invalid AS (
                        SELECT expected.function_oid
                        FROM expected
                        JOIN pg_catalog.pg_proc AS function
                          ON function.oid = expected.function_oid
                        JOIN pg_catalog.pg_roles AS owner
                          ON owner.oid = function.proowner
                        CROSS JOIN LATERAL (
                            SELECT
                                pg_catalog.count(*) AS row_count,
                                pg_catalog.count(*) FILTER (
                                    WHERE acl.grantor = owner.oid
                                      AND acl.grantee =
                                          'tagekyc_runtime'::regrole::oid
                                      AND acl.privilege_type = 'EXECUTE'
                                      AND NOT acl.is_grantable) AS expected_count
                            FROM pg_catalog.aclexplode(
                                COALESCE(
                                    function.proacl,
                                    pg_catalog.acldefault(
                                        'f',
                                        function.proowner))) AS acl
                            WHERE acl.grantee <> function.proowner
                        ) AS acl_manifest
                        WHERE owner.rolname <>
                                  'tagekyc_raw_export_deployer'
                           OR owner.rolcanlogin
                           OR NOT owner.rolinherit
                           OR owner.rolsuper
                           OR owner.rolcreatedb
                           OR owner.rolcreaterole
                           OR owner.rolreplication
                           OR owner.rolbypassrls
                           OR acl_manifest.row_count <> 1
                           OR acl_manifest.expected_count <> 1
                    )
                    SELECT pg_catalog.count(*)
                    INTO invalid_count
                    FROM invalid;

                    IF invalid_count <> 0 THEN
                        RAISE EXCEPTION
                            'TIP88B1E3_FUNCTION_ACL_INVALID';
                    END IF;
                END;
                $migration$;

                REVOKE SELECT ON
                    tagekyc.verification_sessions,
                    tagekyc.raw_export_subject_consent_authorities,
                    tagekyc.raw_export_subject_consent_events,
                    tagekyc.raw_export_subject_consent_classes
                FROM tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    actual_body_hash text;
                BEGIN
                    SELECT pg_catalog.md5(p.prosrc)
                    INTO actual_body_hash
                    FROM pg_catalog.pg_proc AS p
                    WHERE p.oid =
                        'tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz)'::regprocedure;
                    IF actual_body_hash IS DISTINCT FROM '9359b6f264931b77dc3194136fbc5cf2' THEN
                        RAISE EXCEPTION 'TIP88B1E3_B2_CONSENT_HARDENED_BODY_MISMATCH';
                    END IF;
                END;
                $migration$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_granted(
                    verification_session_id uuid,
                    policy_id uuid,
                    policy_version integer,
                    raw_classes text[],
                    consent_text_version text,
                    consent_text_content_hash text,
                    external_consent_artifact_ref text,
                    decision_ref text,
                    valid_until_utc timestamptz)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $function$
                DECLARE
                    actor_id uuid;
                    session_subject text;
                    session_owner uuid;
                    session_state text;
                    scope_hash bytea;
                    current_revision integer;
                    next_revision integer;
                    record_id uuid;
                    raw_class text;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    SELECT "SubjectRef", "ClientApplicationId", "State"
                    INTO session_subject, session_owner, session_state
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;
                    IF session_state <> 'Completed' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SESSION_NOT_COMPLETED';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentRecorder'));
                    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentRecorder') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF raw_classes IS NULL OR pg_catalog.cardinality(raw_classes) = 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED';
                    END IF;
                    IF (SELECT pg_catalog.count(DISTINCT item) FROM pg_catalog.unnest(raw_classes) AS item) <> pg_catalog.cardinality(raw_classes) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_DUPLICATE';
                    END IF;
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.unnest(raw_classes) AS item
                        WHERE item NOT IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_RAW_CLASS_INVALID';
                    END IF;
                    IF consent_text_version IS NULL OR pg_catalog.btrim(consent_text_version) = ''
                       OR consent_text_content_hash IS NULL OR pg_catalog.btrim(consent_text_content_hash) = ''
                       OR external_consent_artifact_ref IS NULL OR pg_catalog.btrim(external_consent_artifact_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_PROVENANCE_REQUIRED';
                    END IF;
                    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;

                    scope_hash := tagekyc.raw_export_consent_scope_hash(
                        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
                    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
                    SELECT COALESCE(MAX("Revision"), 0)
                    INTO current_revision
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "VerificationSessionId" = verification_session_id
                      AND "SubjectRef" = session_subject
                      AND "PolicyId" = policy_id
                      AND "PolicyVersion" = policy_version
                      AND "PurposeCode" = 'SubjectRawBiometricExport'
                      AND "RecipientClientApplicationId" = session_owner;
                    next_revision := current_revision + 1;
                    record_id := pg_catalog.gen_random_uuid();
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
                    INSERT INTO tagekyc.raw_export_subject_consent_events
                        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                         "Revision","EventType","TargetRevision","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef",
                         "ValidFromUtc","ValidUntilUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (record_id, scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
                         next_revision, 'Granted', NULL, consent_text_version, consent_text_content_hash, external_consent_artifact_ref, decision_ref,
                         pg_catalog.transaction_timestamp(), valid_until_utc, pg_catalog.transaction_timestamp(), actor_id, pg_catalog.transaction_timestamp());
                    FOREACH raw_class IN ARRAY raw_classes LOOP
                        INSERT INTO tagekyc.raw_export_subject_consent_classes
                            ("SubjectConsentRecordId","RawClass","CreatedAt")
                        VALUES
                            (record_id, raw_class, pg_catalog.transaction_timestamp());
                    END LOOP;
                    RETURN next_revision;
                END;
                $function$;

                REVOKE EXECUTE ON FUNCTION
                    tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer),
                    tagekyc.raw_export_control_plane_root_health()
                FROM tagekyc_runtime;

                DROP FUNCTION IF EXISTS tagekyc.raw_export_control_plane_root_health();
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer);

                REVOKE SELECT ON tagekyc.raw_export_policy_allowed_classes
                FROM tagekyc_raw_export_deployer;

                GRANT SELECT ON
                    tagekyc.verification_sessions,
                    tagekyc.raw_export_subject_consent_authorities,
                    tagekyc.raw_export_subject_consent_events,
                    tagekyc.raw_export_subject_consent_classes
                TO tagekyc_runtime;
                """);
        }
    }
}
