# TIP-88C1-C6B-A1 — Transition SQL 04: Authentication, Nonce and Credential Revoke

> Mechanical R27 SHA-binding successor, 2026-09-11. Preserved predecessor `tip_88c1_c6b_a1_transition_sql_04_auth_nonce_credential_revoke.md` SHA `8E53CF1F459F9C95E7A29C80693A4F82C1D359789AD12F0FDADD5CEBAD619F99`. Consumes `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`. SQL fenced blocks are unchanged; this is not new SQL authority.

**Status:** REVIEW CANDIDATE — NOT IMPLEMENTATION AUTHORITY  
**Scope:** required authentication helpers, transaction N, bounded nonce cleanup and R10.  
**Source:** literal operation master SHA-256 `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`  
**Foundation dependency:** literal DDL master SHA-256 `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`

The two resolver functions disclose verifier material only to the dedicated
authenticator role. Signature or keyed-secret comparison remains fixed-time
Application code. `capture_runtime_claim_nonce` is the transaction-N
linearization point: after cryptographic verification it re-locks and rechecks
the complete current lineage and route role before inserting the nonce.

## Up — literal SQL

```sql
CREATE FUNCTION tagekyc.platform_operator_authenticate(
    p_key_lookup_prefix text,p_now timestamptz)
RETURNS TABLE(credential_id uuid,principal_id uuid,secret_digest bytea,
    verifier_pepper_version integer,scopes text[],revision bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT c."CredentialId",c."PrincipalId",c."SecretDigest",
           c."VerifierPepperVersion",c."Scopes",c."Revision"
    FROM tagekyc.platform_operator_credentials c
    WHERE c."KeyLookupPrefix"=p_key_lookup_prefix
      AND c."State"='Active' AND c."ExpiresAtUtc">p_now;
$function$;
ALTER FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_resolve_verifier(
    p_credential_id uuid,p_generation bigint,p_now timestamptz)
RETURNS TABLE(capture_agent_id uuid,device_installation_id uuid,
    credential_id uuid,generation bigint,public_verifier_spki bytea,
    public_key_thumbprint bytea,role_policy_id uuid,role_policy_revision bigint,
    runtime_revision bigint,installation_revision bigint,credential_revision bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT r."CaptureAgentId",i."DeviceInstallationId",g."CredentialId",g."Generation",
           g."PublicVerifierSpki",g."PublicKeyThumbprint",g."RolePolicyId",g."RolePolicyRevision",
           r."Revision",i."Revision",g."Revision"
    FROM tagekyc.capture_runtime_credential_generations g
    JOIN tagekyc.capture_runtime_installations i
      ON i."DeviceInstallationId"=g."DeviceInstallationId"
     AND i."CurrentCredentialId"=g."CredentialId"
     AND i."CurrentCredentialGeneration"=g."Generation"
    JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=i."CaptureAgentId"
    WHERE g."CredentialId"=p_credential_id AND g."Generation"=p_generation
      AND r."LifecycleState"='Active' AND i."LifecycleState"='Active'
      AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_claim_nonce(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_generation bigint,p_required_role text,p_nonce bytea,
    p_signed_timestamp timestamptz,p_admitted_at timestamptz)
RETURNS TABLE(result_code text,runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,role_policy_id uuid,role_policy_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
BEGIN
  IF p_required_role NOT IN ('Bind','CaptureObservation','Configuration','CredentialRotation','RawIngress','TrustedEvidence')
     OR pg_catalog.octet_length(p_nonce)<>32
     OR p_signed_timestamp<p_admitted_at-interval '150 seconds'
     OR p_signed_timestamp>p_admitted_at+interval '150 seconds' THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('capture-runtime-nonce-capacity',1));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_admitted_at OR v_g."ValidUntilUtc"<=p_admitted_at
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                    WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                      AND rp."EffectiveAtUtc"<=p_admitted_at AND p_required_role=ANY(rp."Roles")) THEN
    RETURN QUERY SELECT 'ACCESS_DENIED',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END IF;
  BEGIN
    INSERT INTO tagekyc.capture_runtime_request_nonces VALUES(
      p_credential_id,p_generation,p_nonce,p_signed_timestamp,p_admitted_at,p_signed_timestamp+interval '150 seconds');
  EXCEPTION WHEN unique_violation THEN
    RETURN QUERY SELECT 'REPLAY',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END;
  RETURN QUERY SELECT 'ADMITTED',v_r."Revision",v_i."Revision",v_g."Revision",v_g."RolePolicyId",v_g."RolePolicyRevision";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_cleanup_nonces(p_now timestamptz,p_limit integer)
RETURNS TABLE(deleted_count bigint)
LANGUAGE sql VOLATILE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
  WITH doomed AS (
    SELECT n."CredentialId",n."CredentialGeneration",n."Nonce"
    FROM tagekyc.capture_runtime_request_nonces n
    WHERE n."PurgeAfterUtc"<p_now
    ORDER BY n."PurgeAfterUtc",n."CredentialId",n."CredentialGeneration",n."Nonce"
    LIMIT CASE WHEN p_limit BETWEEN 1 AND 10000 THEN p_limit ELSE 0 END
    FOR UPDATE SKIP LOCKED
  ), deleted AS (
    DELETE FROM tagekyc.capture_runtime_request_nonces n USING doomed d
    WHERE n."CredentialId"=d."CredentialId" AND n."CredentialGeneration"=d."CredentialGeneration" AND n."Nonce"=d."Nonce"
    RETURNING 1)
  SELECT pg_catalog.count(*) FROM deleted;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_revoke_credential(
    p_actor uuid,p_idempotency_key uuid,p_capture_agent_id uuid,
    p_device_installation_id uuid,p_credential_id uuid,p_generation bigint,
    p_expected_revision bigint,p_reason text,p_request_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,state text,
    revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_o tagekyc.capture_runtime_management_operations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
BEGIN
  IF p_generation<=0 OR p_expected_revision<=0 OR p_reason<>'CredentialCompromise'
     OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN
    RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_credentials c WHERE c."CredentialId"=p_actor AND c."State"='Active' AND c."ExpiresAtUtc">p_now AND c."Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN
    RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_idempotency_key::text,60));
  SELECT * INTO v_o FROM tagekyc.capture_runtime_management_operations
   WHERE "ActorCredentialId"=p_actor AND "OperationKind"='CredentialRevoke' AND "IdempotencyKey"=p_idempotency_key;
  IF FOUND THEN
    IF v_o."RequestFingerprint"<>p_request_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz;
    ELSE RETURN QUERY SELECT 'Revoked',g."CredentialId",g."Generation",g."State"::text,g."Revision",g."RevokedAtUtc" FROM tagekyc.capture_runtime_credential_generations g WHERE g."CredentialId"=p_credential_id AND g."Generation"=p_generation; END IF;
    RETURN;
  END IF;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  IF NOT FOUND OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation THEN
    RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  IF v_g."State"<>'Active' OR v_g."Revision"<>p_expected_revision OR v_i."LifecycleState"<>'Active' THEN
    RETURN QUERY SELECT 'Conflict',v_g."CredentialId",v_g."Generation",v_g."State"::text,v_g."Revision",v_g."RevokedAtUtc"; RETURN;
  END IF;
  UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"=p_reason WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation RETURNING * INTO v_g;
  UPDATE tagekyc.capture_runtime_installations SET "LifecycleState"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"LifecycleReason"=p_reason WHERE "DeviceInstallationId"=p_device_installation_id;
  INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'CredentialRevoke',p_idempotency_key,p_request_fingerprint,'Credential',p_credential_id,'Applied',v_g."Revision",p_credential_id,p_now,p_now);
  INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'CredentialRevoke',p_idempotency_key,'Applied','Credential',p_credential_id,p_expected_revision,v_g."Revision",p_reason,p_now);
  RETURN QUERY SELECT 'Revoked',v_g."CredentialId",v_g."Generation",v_g."State"::text,v_g."Revision",v_g."RevokedAtUtc";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
```

## Down — literal SQL

```sql
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz);
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
DROP FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz);
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer);
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz);
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz);
REVOKE EXECUTE ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz);
```

## Stage-1 atomic R13 nonce classifier — literal Up

```sql
CREATE FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(
    p_credential_id uuid,p_generation bigint,p_rotation_id uuid,
    p_candidate_key_id uuid,p_successor_thumbprint bytea,
    p_request_fingerprint_as_predecessor bytea,
    p_request_fingerprint_as_successor bytea,p_nonce bytea,
    p_signed_at timestamptz,p_now timestamptz)
RETURNS TABLE(branch text,selected_request_fingerprint bytea,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,role_policy_id uuid,role_policy_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE;
        v_o tagekyc.capture_runtime_rotation_completion_operations%ROWTYPE;
        v_branch text;
        v_selected bytea;
BEGIN
  IF p_generation<=0 OR pg_catalog.octet_length(p_successor_thumbprint)<>32
     OR (p_request_fingerprint_as_predecessor IS NOT NULL AND pg_catalog.octet_length(p_request_fingerprint_as_predecessor)<>32)
     OR (p_request_fingerprint_as_successor IS NOT NULL AND pg_catalog.octet_length(p_request_fingerprint_as_successor)<>32)
     OR (p_request_fingerprint_as_predecessor IS NULL AND p_request_fingerprint_as_successor IS NULL)
     OR pg_catalog.octet_length(p_nonce)<>32
     OR p_signed_at<p_now-interval '150 seconds' OR p_signed_at>p_now+interval '150 seconds' THEN
    RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('capture-runtime-nonce-capacity',1));
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations
   WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation;
  IF NOT FOUND THEN RETURN; END IF;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations
   WHERE "DeviceInstallationId"=v_g."DeviceInstallationId";
  IF NOT FOUND THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_i."CaptureAgentId"::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_i."DeviceInstallationId"::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation_id::text,60));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=v_i."CaptureAgentId" FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=v_i."DeviceInstallationId" FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  SELECT * INTO v_a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation_id FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
     OR v_a."DeviceInstallationId" IS DISTINCT FROM v_i."DeviceInstallationId"
     OR v_a."CredentialId" IS DISTINCT FROM p_credential_id THEN RETURN;
  END IF;
  IF v_a."State"='Active' AND v_a."CurrentGeneration"=p_generation
     AND p_request_fingerprint_as_predecessor IS NOT NULL
     AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                 WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                   AND rp."EffectiveAtUtc"<=p_now AND 'CredentialRotation'=ANY(rp."Roles")) THEN
    v_branch:='Predecessor'; v_selected:=p_request_fingerprint_as_predecessor;
  ELSIF v_a."State"='Completed' AND v_a."SuccessorGeneration"=p_generation
     AND v_a."CandidateKeyId" IS NOT DISTINCT FROM p_candidate_key_id
     AND v_a."SuccessorPublicKeyThumbprint" IS NOT DISTINCT FROM p_successor_thumbprint
     AND p_request_fingerprint_as_successor IS NOT NULL THEN
    SELECT * INTO v_o FROM tagekyc.capture_runtime_rotation_completion_operations
     WHERE "RotationAuthorizationId"=p_rotation_id AND "CredentialId"=p_credential_id
       AND "SuccessorGeneration"=p_generation AND "CandidateKeyId"=p_candidate_key_id
       AND "ResultCode"='Applied' AND "RequestFingerprint" IS NOT DISTINCT FROM p_request_fingerprint_as_successor;
    IF NOT FOUND THEN RETURN; END IF;
    v_branch:='Successor'; v_selected:=p_request_fingerprint_as_successor;
  ELSE RETURN;
  END IF;
  BEGIN
    INSERT INTO tagekyc.capture_runtime_request_nonces VALUES(
      p_credential_id,p_generation,p_nonce,p_signed_at,p_now,p_signed_at+interval '150 seconds');
  EXCEPTION WHEN unique_violation THEN RETURN;
  END;
  RETURN QUERY SELECT v_branch,v_selected,v_r."Revision",v_i."Revision",v_g."Revision",v_g."RolePolicyId",v_g."RolePolicyRevision";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) TO tagekyc_capture_runtime_authenticator;
```

## Required proofs

- `PlatformCredentialAuthentication_HasNoClientPartition`: inactive, expired or
  unknown prefix yields zero verifier rows; only the authenticator can execute.
- `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate`: claim rechecks the
  exact current lineage/role, first insert returns `ADMITTED`, duplicate returns
  `REPLAY`, and transaction B rollback does not remove N.
- `NonceCleanup_IsBoundedAndCannotDeleteTimeValidEnvelope`: invalid limits delete
  zero; at most 10,000 rows with `PurgeAfterUtc < now` are deleted; equality is retained.
- `CredentialRevoke_RacesRotationWithoutSplitGeneration`: revoke and completion
  use 10→20→30; one wins, and deferred current-generation guards observe either
  wholly Active or wholly revoked installation/generation state.
- `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown`: all five full
  signatures have the exact owner/grantee above and disappear after Down.

## Mechanical inventory acceptance

The dispatch callable-name census, OP full SQL-signature census and the union of
T1–T4 `CREATE FUNCTION`, exact full-signature `ALTER FUNCTION` owner statements, `GRANT EXECUTE`, Down
`REVOKE EXECUTE` and `DROP FUNCTION` statements are parsed as sets. Acceptance
requires all of the following, with literal names rather than counts alone:

1. every dispatch callable has exactly one full signature and one owning
   operation/helper paragraph in OP;
2. every OP full SQL signature has exactly one `CREATE`, owner, closed grantee
   set, Down revoke and Down drop in exactly one companion;
3. internal helpers are limited to `c6ba_transition_runtime_lifecycle`,
   `capture_runtime_resolve_capability_verifier`,
   `capture_runtime_materialize_capability_expiry` and
   `capture_runtime_read_cutover_state`, each joined to an OP owner and proof;
4. every mutator joins to a named replay/concurrency/audit proof and every
   reader joins to an ACL/non-enumeration proof;
5. set comparison currently MUST remain RED if the parent callable list still
   names `capture_runtime_apply_capture_artifact` or
   `capture_runtime_apply_evidence_result`: OP R24/R25 instead authorize reuse of
   the existing authority-neutral Application append boundary and explicitly
   authorize no new public SQL function. This parent/OP contradiction must be
   reconciled; the Builder may not create either function merely to make counts
   equal.
