# TIP-88C1-C6B-A1 — Transition SQL 03: Capability, Binding and Readiness

> R27 successor with bounded implementation corrections, 2026-09-12. Preserved predecessor `tip_88c1_c6b_a1_transition_sql_03_capability_binding_readiness.md` SHA `5A88AC9FE8F03D3F16B20CE12FA54B367EB1E83AA768301376C0497C6A97F741`. Consumes `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`. Current SQL incorporates the authorized R24/R25 server-derived session, R28 global pepper projection and R26 internal cancellation identity; this is not a claim that predecessor SQL is unchanged. Current bindings are reconciled after full regression; see parent evidence.

**Status:** REVIEW CANDIDATE — NOT IMPLEMENTATION AUTHORITY  
**Source:** literal operation master SHA-256 `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`  
**Foundation dependency:** literal DDL master SHA-256 `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`; all tables, constraints, roles and
append-only guards named below must exist before this companion is applied.

This companion owns R20a, R20b, R21, R22, R26 and R28. It does not own runtime
authentication transaction N, catalog publication, registration lifecycle, A3,
the Client cancellation domain update, or deployment activation. The R19 SQL
reports only DB facts and required pepper version IDs. The R23 SQL reports only
configuration content; Application owns canonical-content ETag computation.
Role, trust and configuration catalogue heads govern publication/CAS only.
Runtime evaluation follows the exact immutable revisions already frozen on the
credential generation or registration and their own effective windows; a new
unassigned head never disables an existing assignment.

## Up — literal SQL

```sql
CREATE FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(
    p_client_application_id uuid,
    p_verification_session_id uuid,
    p_action text,
    p_current_capability_id uuid,
    p_expected_revision bigint,
    p_idempotency_key uuid,
    p_new_capability_id uuid,
    p_key_lookup_prefix text,
    p_secret_digest bytea,
    p_verifier_pepper_version integer,
    p_request_fingerprint bytea,
    p_now timestamptz)
RETURNS TABLE(result_code text, capture_capability_id uuid,
    secret_available boolean, expires_at_utc timestamptz, state text,
    revision bigint)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $function$
DECLARE
    v_session tagekyc.verification_sessions%ROWTYPE;
    v_current tagekyc.capture_capabilities%ROWTYPE;
    v_operation tagekyc.capture_capability_operations%ROWTYPE;
    v_expiry timestamptz := p_now + interval '5 minutes';
    v_capability_lock_a bigint;
    v_capability_lock_b bigint;
    v_expiry_operation_id uuid;
BEGIN
    IF p_action NOT IN ('Issue','Replace')
       OR p_client_application_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_verification_session_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_idempotency_key = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_new_capability_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_key_lookup_prefix !~ '^[A-Za-z0-9_-]{12}$'
       OR pg_catalog.octet_length(p_secret_digest) <> 32
       OR p_verifier_pepper_version <= 0
       OR pg_catalog.octet_length(p_request_fingerprint) <> 32
       OR (p_action='Issue' AND (p_current_capability_id IS NOT NULL OR p_expected_revision IS NOT NULL))
       OR (p_action='Replace' AND (p_current_capability_id IS NULL OR p_expected_revision IS NULL OR p_expected_revision <= 0)) THEN
        RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
    v_capability_lock_a:=pg_catalog.hashtextextended(COALESCE(p_current_capability_id,p_new_capability_id)::text,80);
    v_capability_lock_b:=pg_catalog.hashtextextended(p_new_capability_id::text,80);
    PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(v_capability_lock_a,v_capability_lock_b));
    IF v_capability_lock_a<>v_capability_lock_b THEN
      PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(v_capability_lock_a,v_capability_lock_b));
    END IF;

    SELECT * INTO v_operation
    FROM tagekyc.capture_capability_operations
    WHERE "ClientApplicationId"=p_client_application_id
      AND "VerificationSessionId"=p_verification_session_id
      AND "OperationKind"=p_action
      AND "IdempotencyKey"=p_idempotency_key;
    IF FOUND THEN
        IF v_operation."RequestFingerprint" <> p_request_fingerprint THEN
            RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        ELSE
            RETURN QUERY
            SELECT CASE WHEN v_operation."ResultCode"='Expired' THEN 'TERMINALIZED_EXPIRED_AND_DENIED' ELSE 'EXISTING_MATCH_SECRET_UNAVAILABLE' END,c."CaptureCapabilityId",false,
                   c."ExpiresAtUtc",CASE WHEN v_operation."ResultCode"='Expired' THEN 'Expired' ELSE 'ActiveUnbound' END,v_operation."ResultRevision"
            FROM tagekyc.capture_capabilities c
            WHERE c."CaptureCapabilityId"=v_operation."ResultCapabilityId";
        END IF;
        RETURN;
    END IF;

    SELECT * INTO v_session FROM tagekyc.verification_sessions
    WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id
    FOR UPDATE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;
    IF v_session."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal')
       OR v_session."ExpiresAt" <= p_now
       OR v_session."BindingNonceHash" IS NULL THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;
    IF EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings b
               WHERE b."VerificationSessionId"=p_verification_session_id) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    SELECT * INTO v_current FROM tagekyc.capture_capabilities
    WHERE "VerificationSessionId"=p_verification_session_id
      AND "ClientApplicationId"=p_client_application_id
      AND "State" IN ('ActiveUnbound','Bound')
    ORDER BY "CaptureCapabilityId" LIMIT 1 FOR UPDATE;
    IF FOUND AND v_current."ExpiresAtUtc"<=p_now THEN
      IF p_action='Replace' THEN
        IF v_current."CaptureCapabilityId"<>p_current_capability_id OR v_current."Revision"<>p_expected_revision THEN
          RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
        END IF;
        UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_current."CaptureCapabilityId";
        INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,p_request_fingerprint,'Expired',v_current."CaptureCapabilityId",v_current."Revision"+1,p_now,p_now);
        INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,v_current."CaptureCapabilityId",'Expired',v_current."Revision",v_current."Revision"+1,p_now);
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',v_current."CaptureCapabilityId",false,v_current."ExpiresAtUtc",'Expired',v_current."Revision"+1; RETURN;
      END IF;
      v_expiry_operation_id:=pg_catalog.gen_random_uuid();
      UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_current."CaptureCapabilityId";
      INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,tagekyc_extensions.digest(pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')||pg_catalog.uuid_send(p_verification_session_id)||pg_catalog.uuid_send(v_current."CaptureCapabilityId")||pg_catalog.timestamptz_send(v_current."ExpiresAtUtc")||pg_catalog.uuid_send(v_expiry_operation_id),'sha256'),'Applied',v_current."CaptureCapabilityId",v_current."Revision"+1,p_now,p_now);
      INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,v_current."CaptureCapabilityId",'Expired',v_current."Revision",v_current."Revision"+1,p_now);
    END IF;

    IF p_action='Replace' THEN
        SELECT * INTO v_current FROM tagekyc.capture_capabilities
        WHERE "CaptureCapabilityId"=p_current_capability_id
          AND "VerificationSessionId"=p_verification_session_id
          AND "ClientApplicationId"=p_client_application_id
        FOR UPDATE;
        IF NOT FOUND OR v_current."State"<>'ActiveUnbound'
           OR v_current."Revision"<>p_expected_revision THEN
            RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
            RETURN;
        END IF;
        UPDATE tagekyc.capture_capabilities
        SET "State"='Revoked',"Revision"="Revision"+1,
            "SuccessorCapabilityId"=p_new_capability_id,"RevokedAtUtc"=p_now,
            "TerminalReason"='ClientReplacement'
        WHERE "CaptureCapabilityId"=p_current_capability_id;
    ELSIF EXISTS (SELECT 1 FROM tagekyc.capture_capabilities c
                  WHERE c."VerificationSessionId"=p_verification_session_id
                    AND c."State" IN ('ActiveUnbound','Bound')) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    INSERT INTO tagekyc.capture_capabilities(
        "CaptureCapabilityId","VerificationSessionId","ClientApplicationId",
        "KeyLookupPrefix","SecretDigest","VerifierPepperVersion","Audience",
        "Challenge","IssuedAtUtc","ExpiresAtUtc","State","Revision",
        "PredecessorCapabilityId")
    VALUES (p_new_capability_id,p_verification_session_id,p_client_application_id,
        p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,
        'ManagedCaptureRuntime',v_session."BindingNonceHash",p_now,v_expiry,
        'ActiveUnbound',1,p_current_capability_id);

    INSERT INTO tagekyc.capture_capability_operations(
        "ClientApplicationId","VerificationSessionId","OperationKind",
        "IdempotencyKey","RequestFingerprint","ResultCode",
        "ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc")
    VALUES (p_client_application_id,p_verification_session_id,p_action,
        p_idempotency_key,p_request_fingerprint,'Applied',p_new_capability_id,1,p_now,p_now);
    INSERT INTO tagekyc.capture_capability_events(
        "EventId","ClientApplicationId","VerificationSessionId","OperationKind",
        "IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision",
        "AfterRevision","RecordedAtUtc")
    VALUES (pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,
        p_action,p_idempotency_key,p_new_capability_id,
        CASE WHEN p_action='Issue' THEN 'Issued' ELSE 'Replaced' END,
        CASE WHEN p_action='Issue' THEN NULL ELSE p_expected_revision END,1,p_now);
    RETURN QUERY SELECT 'CREATED',p_new_capability_id,true,v_expiry,'ActiveUnbound',1::bigint;
END
$function$;

ALTER FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_application,tagekyc_runtime;

CREATE FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(p_capture_capability_id uuid)
RETURNS TABLE(secret_digest bytea,verifier_pepper_version integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT c."SecretDigest",c."VerifierPepperVersion"
    FROM tagekyc.capture_capabilities c
    WHERE c."CaptureCapabilityId"=p_capture_capability_id;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_bind_capability(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_capture_capability_id uuid,
    p_capability_secret_verified boolean,p_bind_operation_id uuid,
    p_request_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,capability_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE
    v_r tagekyc.capture_runtime_registrations%ROWTYPE;
    v_i tagekyc.capture_runtime_installations%ROWTYPE;
    v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
    v_c tagekyc.capture_capabilities%ROWTYPE;
    v_s tagekyc.verification_sessions%ROWTYPE;
    v_o tagekyc.capture_capability_operations%ROWTYPE;
    v_binding_id uuid;
    v_horizon timestamptz;
BEGIN
    IF NOT COALESCE(p_capability_secret_verified,false)
       OR p_credential_generation<=0 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN
        RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;
        RETURN;
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text||':'||p_credential_generation::text,30));
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id;
    IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_c."VerificationSessionId"::text,70));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_capability_id::text,80));

    SELECT * INTO v_o FROM tagekyc.capture_capability_operations
    WHERE "ClientApplicationId"=v_c."ClientApplicationId" AND "VerificationSessionId"=v_c."VerificationSessionId"
      AND "OperationKind"='Bind' AND "IdempotencyKey"=p_bind_operation_id;
    IF FOUND THEN
      IF v_o."RequestFingerprint"<>p_request_fingerprint OR v_o."RuntimeCaptureAgentId"<>p_capture_agent_id
         OR v_o."RuntimeInstallationId"<>p_device_installation_id OR v_o."RuntimeCredentialId"<>p_credential_id
         OR v_o."RuntimeCredentialGeneration"<>p_credential_generation THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;
      ELSIF v_o."ResultCode"='Expired' THEN
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,v_o."ResultRevision";
      ELSE
        RETURN QUERY SELECT 'AVAILABLE',b."CaptureExecutionBindingId",b."ExecutionExpiresAtUtc",b."RuntimeRevision",b."InstallationRevision",b."CredentialRevision",v_o."ResultRevision"
        FROM tagekyc.capture_execution_bindings b
        WHERE b."CaptureExecutionBindingId"=v_o."ResultBindingId";
      END IF;
      RETURN;
    END IF;

    SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_credential_generation FOR UPDATE;
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id FOR UPDATE;
    SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=v_c."VerificationSessionId" AND "ClientApplicationId"=v_c."ClientApplicationId" FOR UPDATE;
    IF v_c."State" IN ('ActiveUnbound','Bound') AND v_c."ExpiresAtUtc"<=p_now THEN
        UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=p_capture_capability_id;
        INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","RuntimeCaptureAgentId","RuntimeInstallationId","RuntimeCredentialId","RuntimeCredentialGeneration","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_request_fingerprint,p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,'Expired',p_capture_capability_id,v_c."Revision"+1,p_now,p_now);
        INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","RuntimeCaptureAgentId","RuntimeInstallationId","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_capture_capability_id,'Expired',p_capture_agent_id,p_device_installation_id,v_c."Revision",v_c."Revision"+1,p_now);
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',NULL::uuid,NULL::timestamptz,v_r."Revision",v_i."Revision",v_g."Revision",v_c."Revision"+1; RETURN;
    END IF;
    IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
       OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_credential_generation
       OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
       OR v_c."State" IS DISTINCT FROM 'ActiveUnbound'
       OR v_s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now
       OR v_c."Challenge" IS DISTINCT FROM v_s."BindingNonceHash"
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision" AND rp."EffectiveAtUtc"<=p_now AND 'Bind'=ANY(rp."Roles"))
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp WHERE tp."CatalogId"=v_r."TrustProfileId" AND tp."Revision"=v_r."TrustProfileRevision" AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now)
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp WHERE cp."CatalogId"=v_r."ConfigurationId" AND cp."Revision"=v_r."ConfigurationRevision" AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now) THEN
        RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
    END IF;
    IF EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings b WHERE b."VerificationSessionId"=v_c."VerificationSessionId" OR b."CaptureCapabilityId"=p_capture_capability_id) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
    END IF;
    v_binding_id:=pg_catalog.gen_random_uuid();
    v_horizon:=LEAST(v_c."ExpiresAtUtc",v_s."ExpiresAt",p_now+interval '30 minutes');
    INSERT INTO tagekyc.capture_execution_bindings VALUES(v_binding_id,v_c."VerificationSessionId",p_capture_capability_id,v_c."ClientApplicationId",p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,v_g."PublicKeyThumbprint",v_r."Revision",v_i."Revision",v_g."Revision",v_r."TrustProfileId",v_r."TrustProfileRevision",v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."ConfigurationId",v_r."ConfigurationRevision",v_c."Challenge",p_bind_operation_id,p_now,v_horizon);
    UPDATE tagekyc.capture_capabilities SET "State"='Bound',"Revision"="Revision"+1,"BoundAtUtc"=p_now WHERE "CaptureCapabilityId"=p_capture_capability_id;
    INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","RuntimeCaptureAgentId","RuntimeInstallationId","RuntimeCredentialId","RuntimeCredentialGeneration","ResultCode","ResultCapabilityId","ResultBindingId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_request_fingerprint,p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,'Applied',p_capture_capability_id,v_binding_id,v_c."Revision"+1,p_now,p_now);
    INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","RuntimeCaptureAgentId","RuntimeInstallationId","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_capture_capability_id,'Bound',p_capture_agent_id,p_device_installation_id,v_c."Revision",v_c."Revision"+1,p_now);
    RETURN QUERY SELECT 'CREATED',v_binding_id,v_horizon,v_r."Revision",v_i."Revision",v_g."Revision",v_c."Revision"+1;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_reconcile_binding(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_capture_capability_id uuid,
    p_bind_operation_id uuid,p_now timestamptz)
RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,capability_revision bigint)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path=pg_catalog
AS $function$
    SELECT CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN 'AVAILABLE' ELSE 'RESOURCE_NOT_AVAILABLE' END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."CaptureExecutionBindingId" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."ExecutionExpiresAtUtc" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."RuntimeRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."InstallationRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."CredentialRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN c."Revision" END
    FROM tagekyc.capture_execution_bindings b
    JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
    JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=b."CaptureAgentId"
    JOIN tagekyc.capture_runtime_installations i ON i."DeviceInstallationId"=b."DeviceInstallationId" AND i."CaptureAgentId"=b."CaptureAgentId"
    JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=b."DeviceInstallationId" AND g."CredentialId"=b."CredentialId" AND g."Generation"=b."CredentialGeneration"
    WHERE b."CaptureAgentId"=p_capture_agent_id
      AND b."DeviceInstallationId"=p_device_installation_id
      AND b."CredentialId"=p_credential_id
      AND b."CredentialGeneration"=p_credential_generation
      AND b."CaptureCapabilityId"=p_capture_capability_id
      AND b."BindOperationId"=p_bind_operation_id
      AND r."LifecycleState"='Active' AND r."Revision"=b."RuntimeRevision"
      AND i."LifecycleState"='Active' AND i."Revision"=b."InstallationRevision"
      AND i."CurrentCredentialId"=b."CredentialId" AND i."CurrentCredentialGeneration"=b."CredentialGeneration"
      AND g."State"='Active' AND g."Revision"=b."CredentialRevision"
      AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
      AND c."State"='Bound'
    UNION ALL
    SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM tagekyc.capture_execution_bindings b
        JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
        JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=b."CaptureAgentId"
        JOIN tagekyc.capture_runtime_installations i ON i."DeviceInstallationId"=b."DeviceInstallationId" AND i."CaptureAgentId"=b."CaptureAgentId"
        JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=b."DeviceInstallationId" AND g."CredentialId"=b."CredentialId" AND g."Generation"=b."CredentialGeneration"
        WHERE b."CaptureAgentId"=p_capture_agent_id
          AND b."DeviceInstallationId"=p_device_installation_id
          AND b."CredentialId"=p_credential_id
          AND b."CredentialGeneration"=p_credential_generation
          AND b."CaptureCapabilityId"=p_capture_capability_id
          AND b."BindOperationId"=p_bind_operation_id
          AND r."LifecycleState"='Active' AND r."Revision"=b."RuntimeRevision"
          AND i."LifecycleState"='Active' AND i."Revision"=b."InstallationRevision"
          AND i."CurrentCredentialId"=b."CredentialId" AND i."CurrentCredentialGeneration"=b."CredentialGeneration"
          AND g."State"='Active' AND g."Revision"=b."CredentialRevision"
          AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
          AND c."State"='Bound');
$function$;

ALTER FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(
    p_verification_session_id uuid,p_capture_capability_id uuid,
    p_now timestamptz,p_deterministic_operation_id uuid)
RETURNS TABLE(result_code text,state text,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_c tagekyc.capture_capabilities%ROWTYPE; v_o tagekyc.capture_capability_operations%ROWTYPE; v_fingerprint bytea;
BEGIN
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_capability_id::text,80));
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id AND "VerificationSessionId"=p_verification_session_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::text,NULL::bigint; RETURN; END IF;
  v_fingerprint:=tagekyc_extensions.digest(
    pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')
    ||pg_catalog.uuid_send(p_verification_session_id)
    ||pg_catalog.uuid_send(p_capture_capability_id)
    ||pg_catalog.timestamptz_send(v_c."ExpiresAtUtc")
    ||pg_catalog.uuid_send(p_deterministic_operation_id),'sha256');
  SELECT * INTO v_o FROM tagekyc.capture_capability_operations WHERE "ClientApplicationId"=v_c."ClientApplicationId" AND "VerificationSessionId"=p_verification_session_id AND "OperationKind"='Expire' AND "IdempotencyKey"=p_deterministic_operation_id;
  IF FOUND THEN
    IF v_o."RequestFingerprint" IS DISTINCT FROM v_fingerprint OR v_o."ResultCapabilityId" IS DISTINCT FROM p_capture_capability_id THEN
      RETURN QUERY SELECT 'CONFLICT',NULL::text,NULL::bigint;
    ELSE
      RETURN QUERY SELECT 'AVAILABLE',v_c."State"::text,v_c."Revision";
    END IF;
    RETURN;
  END IF;
  IF v_c."State"<>'ActiveUnbound' OR v_c."ExpiresAtUtc">p_now THEN RETURN QUERY SELECT 'CONFLICT',v_c."State"::text,v_c."Revision"; RETURN; END IF;
  UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=p_capture_capability_id;
  INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",p_verification_session_id,'Expire',p_deterministic_operation_id,v_fingerprint,'Applied',p_capture_capability_id,v_c."Revision"+1,p_now,p_now);
  INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",p_verification_session_id,'Expire',p_deterministic_operation_id,p_capture_capability_id,'Expired',v_c."Revision",v_c."Revision"+1,p_now);
  RETURN QUERY SELECT 'EXPIRED','Expired',v_c."Revision"+1;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_resolve_configuration(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,
    configuration_revision bigint,effective_at_utc timestamptz,
    expires_at_utc timestamptz,raw_export_enabled boolean,
    plaintext_budget_seconds integer,
    raw_export_source_claim_safety_margin_milliseconds integer,
    capture_agent_configuration_polling_interval_seconds integer,
    raw_export_source_maximum_chip_dg2_portrait_bytes integer,
    raw_export_source_maximum_live_selfie_image_bytes integer,
    raw_export_capture_maximum_aggregate_plaintext_bytes_per_host bigint,
    raw_export_custody_maximum_plaintext_window_bytes_per_stream integer,
    raw_export_custody_max_aggregate_bytes_per_deployment bigint,
    raw_export_ingress_maximum_pre_admission_buffered_bytes integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT 'AVAILABLE',r."CaptureAgentId",c."CatalogId",c."Revision",
           c."EffectiveAtUtc",c."ExpiresAtUtc",
           COALESCE(o."RawExportEnabled",c."RawExportEnabled"),
           LEAST(c."PlaintextBudgetSeconds",COALESCE(o."PlaintextBudgetSeconds",c."PlaintextBudgetSeconds")),
           GREATEST(c."RawExportSourceClaimSafetyMarginMilliseconds",COALESCE(o."RawExportSourceClaimSafetyMarginMilliseconds",c."RawExportSourceClaimSafetyMarginMilliseconds")),
           LEAST(c."CaptureAgentConfigurationPollingIntervalSeconds",COALESCE(o."CaptureAgentConfigurationPollingIntervalSeconds",c."CaptureAgentConfigurationPollingIntervalSeconds")),
           LEAST(c."RawExportSourceMaximumChipDg2PortraitBytes",COALESCE(o."RawExportSourceMaximumChipDg2PortraitBytes",c."RawExportSourceMaximumChipDg2PortraitBytes")),
           LEAST(c."RawExportSourceMaximumLiveSelfieImageBytes",COALESCE(o."RawExportSourceMaximumLiveSelfieImageBytes",c."RawExportSourceMaximumLiveSelfieImageBytes")),
           LEAST(c."RawExportCaptureMaximumAggregatePlaintextBytesPerHost",COALESCE(o."RawExportCaptureMaximumAggregatePlaintextBytesPerHost",c."RawExportCaptureMaximumAggregatePlaintextBytesPerHost")),
           LEAST(c."RawExportCustodyMaximumPlaintextWindowBytesPerStream",COALESCE(o."RawExportCustodyMaximumPlaintextWindowBytesPerStream",c."RawExportCustodyMaximumPlaintextWindowBytesPerStream")),
           LEAST(c."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment",COALESCE(o."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment",c."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment")),
           LEAST(c."RawExportIngressMaximumPreAdmissionBufferedBytes",COALESCE(o."RawExportIngressMaximumPreAdmissionBufferedBytes",c."RawExportIngressMaximumPreAdmissionBufferedBytes"))
    FROM tagekyc.capture_runtime_registrations r
    JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId"
    JOIN tagekyc.capture_runtime_credential_generations g
      ON g."DeviceInstallationId"=i."DeviceInstallationId"
     AND g."CredentialId"=i."CurrentCredentialId"
     AND g."Generation"=i."CurrentCredentialGeneration"
    JOIN tagekyc.capture_runtime_configuration_revisions c
      ON c."CatalogId"=r."ConfigurationId" AND c."Revision"=r."ConfigurationRevision"
    LEFT JOIN tagekyc.capture_runtime_configuration_overrides o
      ON o."ConfigurationOverrideId"=r."ConfigurationOverrideId"
     AND o."CaptureAgentId"=r."CaptureAgentId"
     AND o."BaseConfigurationId"=c."CatalogId"
     AND o."BaseConfigurationRevision"=c."Revision"
    WHERE r."CaptureAgentId"=p_capture_agent_id AND r."LifecycleState"='Active'
      AND i."DeviceInstallationId"=p_device_installation_id AND i."LifecycleState"='Active'
      AND g."CredentialId"=p_credential_id AND g."Generation"=p_credential_generation
      AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
      AND c."EffectiveAtUtc"<=p_now AND c."ExpiresAtUtc">p_now
    UNION ALL SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL
    WHERE NOT EXISTS (
      SELECT 1 FROM tagekyc.capture_runtime_registrations r
      JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId"
      JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" AND g."CredentialId"=i."CurrentCredentialId" AND g."Generation"=i."CurrentCredentialGeneration"
      JOIN tagekyc.capture_runtime_configuration_revisions c ON c."CatalogId"=r."ConfigurationId" AND c."Revision"=r."ConfigurationRevision"
      WHERE r."CaptureAgentId"=p_capture_agent_id AND r."LifecycleState"='Active'
        AND i."DeviceInstallationId"=p_device_installation_id AND i."LifecycleState"='Active'
        AND g."CredentialId"=p_credential_id AND g."Generation"=p_credential_generation
        AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
        AND c."EffectiveAtUtc"<=p_now AND c."ExpiresAtUtc">p_now);
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_read_readiness(p_capture_agent_id uuid,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,runtime_state text,
    runtime_revision bigint,installation_state text,installation_revision bigint,
    credential_state text,credential_generation bigint,credential_revision bigint,
    trust_profile_revision bigint,role_policy_revision bigint,
    configuration_revision bigint,required_pepper_versions integer[],
    nonce_store_ready boolean,cutover_sentinel_ready boolean,database_ready boolean)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT 'AVAILABLE',r."CaptureAgentId",r."LifecycleState",r."Revision",
           i."LifecycleState",i."Revision",g."State",g."Generation",g."Revision",
           r."TrustProfileRevision",g."RolePolicyRevision",r."ConfigurationRevision",
           ARRAY(SELECT DISTINCT x.v FROM (
             SELECT p."VerifierPepperVersion" v FROM tagekyc.platform_operator_credentials p WHERE p."State"='Active' AND p."ExpiresAtUtc">p_now
             UNION SELECT b."VerifierPepperVersion" FROM tagekyc.capture_runtime_bootstrap_issuances b WHERE b."State"='Active' AND b."ExpiresAtUtc">p_now
             UNION SELECT c."VerifierPepperVersion" FROM tagekyc.capture_capabilities c WHERE c."State" IN ('ActiveUnbound','Bound') AND c."ExpiresAtUtc">p_now
           ) x ORDER BY x.v),
           (SELECT pg_catalog.count(*)<=1000000 AND COALESCE(pg_catalog.max(p_now-n."PurgeAfterUtc") FILTER (WHERE n."PurgeAfterUtc"<p_now),interval '0')<=interval '900 seconds' FROM tagekyc.capture_runtime_request_nonces n),
           EXISTS (SELECT 1 FROM tagekyc.capture_runtime_cutover_state s WHERE s."Profile"='Managed' AND s."State" IN ('Prepared','Activated')),
           r."LifecycleState"='Active' AND i."LifecycleState"='Active' AND g."State"='Active'
             AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp WHERE tp."CatalogId"=r."TrustProfileId" AND tp."Revision"=r."TrustProfileRevision" AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now)
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp WHERE rp."CatalogId"=g."RolePolicyId" AND rp."Revision"=g."RolePolicyRevision" AND rp."EffectiveAtUtc"<=p_now)
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp WHERE cp."CatalogId"=r."ConfigurationId" AND cp."Revision"=r."ConfigurationRevision" AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now)
    FROM tagekyc.capture_runtime_registrations r
    LEFT JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId" AND i."LifecycleState"='Active' AND i."CurrentCredentialId" IS NOT NULL
    LEFT JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" AND g."CredentialId"=i."CurrentCredentialId" AND g."Generation"=i."CurrentCredentialGeneration"
    WHERE r."CaptureAgentId"=p_capture_agent_id
    UNION ALL SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,ARRAY[]::integer[],false,false,false
    WHERE NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_registrations r WHERE r."CaptureAgentId"=p_capture_agent_id);
$function$;
ALTER FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) TO tagekyc_capture_runtime_operator;

GRANT INSERT ("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") ON tagekyc.audit_events TO tagekyc_raw_export_deployer;

CREATE FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(
    p_client_application_id uuid,p_verification_session_id uuid,p_reason text,
    p_request_id text,p_correlation_id text,p_now timestamptz,p_actor_key_prefix text,
    p_audit_event_id uuid)
RETURNS TABLE(result_code text,verification_session_id uuid,state text,
    request_id text,correlation_id text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_s tagekyc.verification_sessions%ROWTYPE; v_c tagekyc.capture_capabilities%ROWTYPE; v_o tagekyc.capture_capability_operations%ROWTYPE; v_expiry_operation_id uuid;
  v_cancel_operation_id uuid:=p_verification_session_id;
  v_request_fingerprint bytea:=tagekyc_extensions.digest(pg_catalog.convert_to('TAG-EKYC-A1-R26-CANCEL-FINGERPRINT-v1','UTF8')||pg_catalog.uuid_send(p_client_application_id)||pg_catalog.uuid_send(p_verification_session_id),'sha256');
BEGIN
  IF p_client_application_id IS NULL OR p_verification_session_id IS NULL THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
  SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN; END IF;
  SELECT * INTO v_o FROM tagekyc.capture_capability_operations WHERE "ClientApplicationId"=p_client_application_id AND "VerificationSessionId"=p_verification_session_id AND "OperationKind"='Cancel' AND "IdempotencyKey"=v_cancel_operation_id;
  IF FOUND THEN
    IF v_o."RequestFingerprint" IS DISTINCT FROM v_request_fingerprint THEN RETURN QUERY SELECT 'NOT_READY',NULL::uuid,NULL::text,NULL::text,NULL::text;
    ELSE RETURN QUERY SELECT 'AVAILABLE',v_s."Id",v_s."State"::text,v_s."RequestId"::text,v_s."CorrelationId"::text; END IF;
    RETURN;
  END IF;
  -- Candidate metadata belongs only to a first transition; exact replay ignores it.
  IF p_reason IS NULL OR p_reason='' OR pg_catalog.length(p_reason)>64 OR p_reason!~'^[A-Za-z0-9_.:-]+$'
     OR p_request_id IS NULL OR pg_catalog.length(p_request_id)>128 OR p_correlation_id IS NULL OR pg_catalog.length(p_correlation_id)>128
     OR p_actor_key_prefix IS NULL OR pg_catalog.length(p_actor_key_prefix)>128 THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN;
  END IF;
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "VerificationSessionId"=p_verification_session_id AND "State" IN ('ActiveUnbound','Bound') ORDER BY "CaptureCapabilityId" LIMIT 1;
  IF FOUND THEN
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_c."CaptureCapabilityId"::text,80));
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId" FOR UPDATE;
    IF v_c."ExpiresAtUtc"<=p_now THEN
      v_expiry_operation_id:=pg_catalog.gen_random_uuid();
      UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId";
      INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,tagekyc_extensions.digest(pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')||pg_catalog.uuid_send(p_verification_session_id)||pg_catalog.uuid_send(v_c."CaptureCapabilityId")||pg_catalog.timestamptz_send(v_c."ExpiresAtUtc")||pg_catalog.uuid_send(v_expiry_operation_id),'sha256'),'Applied',v_c."CaptureCapabilityId",v_c."Revision"+1,p_now,p_now);
      INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,v_c."CaptureCapabilityId",'Expired',v_c."Revision",v_c."Revision"+1,p_now);
      v_c:=NULL;
    END IF;
  END IF;
  IF v_s."State"='Cancelled' THEN RETURN QUERY SELECT 'AVAILABLE',v_s."Id",v_s."State"::text,v_s."RequestId"::text,v_s."CorrelationId"::text; RETURN; END IF;
  IF v_s."State" IN ('Completed','Expired','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now THEN RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN; END IF;
  UPDATE tagekyc.verification_sessions SET "State"='Cancelled',"RequestId"=p_request_id,"CorrelationId"=p_correlation_id WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id;
  INSERT INTO tagekyc.audit_events("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") VALUES(p_audit_event_id,p_client_application_id,p_verification_session_id,'ClientApplication',p_actor_key_prefix,'SESSION_CANCELLED','sha256:localdev-session-cancelled',p_reason,p_request_id,p_correlation_id,p_now);
  IF v_c."CaptureCapabilityId" IS NOT NULL THEN
    UPDATE tagekyc.capture_capabilities SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"='SessionCancelled' WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId";
  END IF;
  INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Cancel',v_cancel_operation_id,v_request_fingerprint,'Applied',v_c."CaptureCapabilityId",CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision"+1 END,p_now,p_now);
  INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Cancel',v_cancel_operation_id,v_c."CaptureCapabilityId",'Cancelled',CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision" END,CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision"+1 END,p_now);
  RETURN QUERY SELECT 'APPLIED',p_verification_session_id,'Cancelled',p_request_id,p_correlation_id;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) TO tagekyc_capture_runtime_application,tagekyc_runtime;

CREATE FUNCTION tagekyc.capture_runtime_read_cutover_state(p_profile text,p_now timestamptz)
RETURNS TABLE(profile text,state text,revision bigint,prepared_at_utc timestamptz,
    activated_at_utc timestamptz,activated_by_credential_id uuid,required_pepper_versions integer[])
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path=pg_catalog
AS $function$
    SELECT c."Profile",c."State",c."Revision",c."PreparedAtUtc",
           c."ActivatedAtUtc",c."ActivatedByCredentialId",
           ARRAY(SELECT DISTINCT x.v FROM (
             SELECT p."VerifierPepperVersion" v FROM tagekyc.platform_operator_credentials p WHERE p."State"='Active' AND p."ExpiresAtUtc">p_now
             UNION SELECT b."VerifierPepperVersion" FROM tagekyc.capture_runtime_bootstrap_issuances b WHERE b."State"='Active' AND b."ExpiresAtUtc">p_now
             UNION SELECT c."VerifierPepperVersion" FROM tagekyc.capture_capabilities c WHERE c."State" IN ('ActiveUnbound','Bound') AND c."ExpiresAtUtc">p_now
           ) x ORDER BY x.v)
    FROM tagekyc.capture_runtime_cutover_state c
    WHERE p_profile='Managed' AND c."Profile"=p_profile;
$function$;

ALTER FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) TO tagekyc_capture_runtime_application;
```

## Stage-1 R24/R25 append-authority validator — literal Up

```sql
CREATE FUNCTION tagekyc.capture_runtime_validate_append_authority(
    p_capture_agent_id uuid,p_installation_id uuid,p_credential_id uuid,
    p_generation bigint,p_binding_id uuid,
    p_required_role text,p_now timestamptz)
RETURNS TABLE(role_policy_id uuid,role_policy_revision bigint,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,verification_session_id uuid,capability_id uuid,
    capability_revision bigint,binding_id uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_b tagekyc.capture_execution_bindings%ROWTYPE;
        v_c tagekyc.capture_capabilities%ROWTYPE;
        v_s tagekyc.verification_sessions%ROWTYPE;
        discovered_session_id uuid;
        discovered_capability_id uuid;
BEGIN
  IF p_generation<=0 OR p_required_role NOT IN ('CaptureObservation','TrustedEvidence') THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id;
  IF NOT FOUND THEN RETURN; END IF;
  discovered_session_id:=v_b."VerificationSessionId";
  discovered_capability_id:=v_b."CaptureCapabilityId";
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(discovered_session_id::text,70));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(discovered_capability_id::text,80));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_installation_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id FOR UPDATE;
  IF NOT FOUND OR v_b."VerificationSessionId" IS DISTINCT FROM discovered_session_id
     OR v_b."CaptureCapabilityId" IS DISTINCT FROM discovered_capability_id THEN RETURN; END IF;
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=v_b."CaptureCapabilityId" FOR UPDATE;
  SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=v_b."VerificationSessionId" FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."DeviceInstallationId" IS DISTINCT FROM p_installation_id OR v_g."State" IS DISTINCT FROM 'Active'
     OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
     OR v_b."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_b."DeviceInstallationId" IS DISTINCT FROM p_installation_id
     OR v_b."CredentialId" IS DISTINCT FROM p_credential_id OR v_b."CredentialGeneration" IS DISTINCT FROM p_generation
     OR v_b."ExecutionExpiresAtUtc"<=p_now
     OR v_b."RuntimeRevision" IS DISTINCT FROM v_r."Revision" OR v_b."InstallationRevision" IS DISTINCT FROM v_i."Revision"
     OR v_b."CredentialRevision" IS DISTINCT FROM v_g."Revision"
     OR v_b."TrustProfileId" IS DISTINCT FROM v_r."TrustProfileId" OR v_b."TrustProfileRevision" IS DISTINCT FROM v_r."TrustProfileRevision"
     OR v_b."ConfigurationId" IS DISTINCT FROM v_r."ConfigurationId" OR v_b."ConfigurationRevision" IS DISTINCT FROM v_r."ConfigurationRevision"
     OR v_b."RolePolicyId" IS DISTINCT FROM v_g."RolePolicyId" OR v_b."RolePolicyRevision" IS DISTINCT FROM v_g."RolePolicyRevision"
     OR v_c."VerificationSessionId" IS DISTINCT FROM v_b."VerificationSessionId" OR v_c."ClientApplicationId" IS DISTINCT FROM v_b."ClientApplicationId"
     OR v_c."State" IS DISTINCT FROM 'Bound' OR v_c."Revision"<=0 OR v_c."ExpiresAtUtc"<=p_now
     OR v_s."ClientApplicationId" IS DISTINCT FROM v_b."ClientApplicationId"
     OR v_b."Challenge" IS DISTINCT FROM v_s."BindingNonceHash"
     OR v_s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp
                    WHERE tp."CatalogId"=v_r."TrustProfileId" AND tp."Revision"=v_r."TrustProfileRevision"
                      AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now
                      AND (p_required_role<>'TrustedEvidence' OR tp."AllowTrustedEvidence"))
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp
                    WHERE cp."CatalogId"=v_r."ConfigurationId" AND cp."Revision"=v_r."ConfigurationRevision"
                      AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now)
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                    WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                      AND rp."EffectiveAtUtc"<=p_now AND p_required_role=ANY(rp."Roles")) THEN RETURN;
  END IF;
  RETURN QUERY SELECT v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."Revision",v_i."Revision",v_g."Revision",v_b."VerificationSessionId",v_c."CaptureCapabilityId",v_c."Revision",v_b."CaptureExecutionBindingId";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) TO tagekyc_runtime;
```

There are no unresolved joins in this companion. Every Client cancellation
writes the landed session audit plus one `Cancel` operation/`Cancelled` event;
the event has a null capability/revisions before issue, or references the
atomically revoked live capability when one exists.

## Down — literal SQL

```sql
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) FROM tagekyc_runtime;
DROP FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) FROM tagekyc_capture_runtime_application,tagekyc_runtime;
DROP FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid);
REVOKE INSERT ("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") ON tagekyc.audit_events FROM tagekyc_raw_export_deployer;

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) FROM tagekyc_capture_runtime_operator;
DROP FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid);

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_application,tagekyc_runtime;
DROP FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz);
```

## Focused PostgreSQL 16 expiry-on-denied proof

Run inside the runtime-proof transaction after its active runtime/install/current
generation fixture is inserted. Its role-policy fixture must contain both
`Bind` and `Configuration`.

```sql
GRANT USAGE ON SCHEMA tagekyc_extensions TO tagekyc_raw_export_deployer;
GRANT EXECUTE ON FUNCTION tagekyc_extensions.digest(bytea,text) TO tagekyc_raw_export_deployer;
INSERT INTO tagekyc.verification_sessions("Id","ClientApplicationId","BindingNonceHash","State","ExpiresAt","RequestId","CorrelationId") VALUES
('94000000-0000-4000-8000-000000000001','95000000-0000-4000-8000-000000000001','issue-challenge','Pending',now()+interval '1 day','r1','c1'),
('94000000-0000-4000-8000-000000000002','95000000-0000-4000-8000-000000000001','replace-challenge','Pending',now()+interval '1 day','r2','c2'),
('94000000-0000-4000-8000-000000000003','95000000-0000-4000-8000-000000000001','bind-challenge','Pending',now()+interval '1 day','r3','c3'),
('94000000-0000-4000-8000-000000000004','95000000-0000-4000-8000-000000000001','cancel-challenge','Pending',now()+interval '1 day','r4','c4');
INSERT INTO tagekyc.capture_capabilities VALUES
('96000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000001','95000000-0000-4000-8000-000000000001','issueOld001X',decode(repeat('71',32),'hex'),1,'ManagedCaptureRuntime','issue-challenge',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL),
('96000000-0000-4000-8000-000000000002','94000000-0000-4000-8000-000000000002','95000000-0000-4000-8000-000000000001','replaceOld1X',decode(repeat('72',32),'hex'),1,'ManagedCaptureRuntime','replace-challenge',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL),
('96000000-0000-4000-8000-000000000003','94000000-0000-4000-8000-000000000003','95000000-0000-4000-8000-000000000001','bindOld0001X',decode(repeat('73',32),'hex'),1,'ManagedCaptureRuntime','bind-challenge',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL),
('96000000-0000-4000-8000-000000000004','94000000-0000-4000-8000-000000000004','95000000-0000-4000-8000-000000000001','cancelOld01X',decode(repeat('74',32),'hex'),1,'ManagedCaptureRuntime','cancel-challenge',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
DO $proof$ DECLARE a record; b record; BEGIN
 SELECT * INTO a FROM tagekyc.capture_runtime_issue_or_replace_capability('95000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000001','Issue',NULL,NULL,'97000000-0000-4000-8000-000000000001','96000000-0000-4000-8000-000000000011','issueNew001X',decode(repeat('81',32),'hex'),1,decode(repeat('91',32),'hex'),now());
 SELECT * INTO b FROM tagekyc.capture_runtime_issue_or_replace_capability('95000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000001','Issue',NULL,NULL,'97000000-0000-4000-8000-000000000001','96000000-0000-4000-8000-000000000011','issueNew001X',decode(repeat('81',32),'hex'),1,decode(repeat('91',32),'hex'),now());
 IF a.result_code<>'CREATED' OR b.result_code<>'EXISTING_MATCH_SECRET_UNAVAILABLE' OR (SELECT "State" FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"='96000000-0000-4000-8000-000000000001')<>'Expired' THEN RAISE EXCEPTION 'ISSUE_EXPIRY_PROOF_FAILED'; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_issue_or_replace_capability('95000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000002','Replace','96000000-0000-4000-8000-000000000002',1,'97000000-0000-4000-8000-000000000002','96000000-0000-4000-8000-000000000012','replaceNew1X',decode(repeat('82',32),'hex'),1,decode(repeat('92',32),'hex'),now());
 SELECT * INTO b FROM tagekyc.capture_runtime_issue_or_replace_capability('95000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000002','Replace','96000000-0000-4000-8000-000000000002',1,'97000000-0000-4000-8000-000000000002','96000000-0000-4000-8000-000000000012','replaceNew1X',decode(repeat('82',32),'hex'),1,decode(repeat('92',32),'hex'),now());
 IF a.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED' OR b.result_code<>a.result_code OR b.revision<>2 THEN RAISE EXCEPTION 'REPLACE_EXPIRY_PROOF_FAILED'; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_bind_capability('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,'96000000-0000-4000-8000-000000000003',true,'97000000-0000-4000-8000-000000000003',decode(repeat('93',32),'hex'),now());
 SELECT * INTO b FROM tagekyc.capture_runtime_bind_capability('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,'96000000-0000-4000-8000-000000000003',true,'97000000-0000-4000-8000-000000000003',decode(repeat('93',32),'hex'),now());
 IF a.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED' OR b.result_code<>a.result_code OR b.capability_revision<>2 THEN RAISE EXCEPTION 'BIND_EXPIRY_PROOF_FAILED'; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_cancel_session_with_capability('95000000-0000-4000-8000-000000000001','94000000-0000-4000-8000-000000000004','ClientRequested','r4','c4',now(),'clientprefix','98000000-0000-4000-8000-000000000004');
 IF a.result_code<>'APPLIED' OR (SELECT "State" FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"='96000000-0000-4000-8000-000000000004')<>'Expired' OR (SELECT "State" FROM tagekyc.verification_sessions WHERE "Id"='94000000-0000-4000-8000-000000000004')<>'Cancelled' THEN RAISE EXCEPTION 'CANCEL_EXPIRY_PROOF_FAILED'; END IF;
END $proof$;
SET CONSTRAINTS ALL IMMEDIATE;
```

## PostgreSQL 16 acceptance

The executable block is accepted only after the foundation and companion 01/02
apply cleanly to a disposable PostgreSQL 16 database, these functions compile,
R20 issue/replay/replace and R22 exact/mismatch behavior execute, ACL catalogue
queries prove the exact grants above, Down succeeds, and reapply succeeds.
Compilation against hand-written substitute tables is not acceptance.
