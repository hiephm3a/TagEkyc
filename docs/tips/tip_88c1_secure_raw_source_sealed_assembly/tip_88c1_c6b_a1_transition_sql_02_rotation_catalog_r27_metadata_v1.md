# TIP-88C1-C6B-A1 — Transition SQL Companion 02

> Mechanical R27 SHA-binding successor, 2026-09-11. Preserved predecessor `tip_88c1_c6b_a1_transition_sql_02_rotation_catalog.md` SHA `08A496EA890E8C026C13819DD53E82B7E5C41B03EE99B3A21A241B92C1CEBDB6`. Consumes `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`. The R11/R13 bigint return literals are corrected to 1::bigint to match the ratified return type and tested Foundation; no transition semantics are changed.

**Status:** COMPLETE REVIEW CANDIDATE — NOT IMPLEMENTATION AUTHORITY  
**Scope:** R11, R12, R13a, R13b and R14–R18.  
**Consumed operation master:** SHA-256 `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`  
**Consumed literal DDL master:** SHA-256 `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`  

`RotationId` on HTTP is exactly the durable `RotationAuthorizationId` below.

## R11–R13 literal PostgreSQL

```sql
CREATE FUNCTION tagekyc.c6ba_assert_current_operator(p_actor uuid,p_now timestamptz)
RETURNS void LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $f$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_actor AND "State"='Active' AND "IssuedAtUtc"<=p_now AND "ExpiresAtUtc">p_now AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RAISE EXCEPTION 'TIP88C1C6BA_OPERATOR_DENIED'; END IF;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_authorize_rotation(
 p_actor uuid,p_idem uuid,p_agent uuid,p_installation uuid,p_credential uuid,
 p_generation bigint,p_expected_revision bigint,p_expires timestamptz,
 p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,rotation_id uuid,capture_agent_id uuid,
 installation_id uuid,predecessor_credential_id uuid,current_generation bigint,
 state text,revision bigint,expires_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; op record; new_rotation_id uuid;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_installation::text,20));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential::text,30));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_idem::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations
  WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RotationAuthorize' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN
  IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=op."ResultId";
  RETURN QUERY SELECT 'Replay',a."RotationAuthorizationId",a."CaptureAgentId",a."DeviceInstallationId",a."CredentialId",a."CurrentGeneration",'Active',1::bigint,a."ExpiresAtUtc"; RETURN;
 END IF;
 IF p_expires<=p_now OR p_expires>p_now+interval '10 minutes' OR octet_length(p_fingerprint)<>32 THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_INPUT_INVALID'; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_registrations r JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId" JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" WHERE r."CaptureAgentId"=p_agent AND i."DeviceInstallationId"=p_installation AND g."CredentialId"=p_credential AND g."Generation"=p_generation AND r."LifecycleState"='Active' AND i."LifecycleState"='Active' AND g."State"='Active' AND g."Revision"=p_expected_revision FOR UPDATE OF r,i,g;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 new_rotation_id:=pg_catalog.gen_random_uuid();
 a."RotationAuthorizationId":=new_rotation_id;a."CaptureAgentId":=p_agent;a."DeviceInstallationId":=p_installation;a."CredentialId":=p_credential;a."CurrentGeneration":=p_generation;a."AuthorizeOperationId":=p_idem;a."RequestFingerprint":=p_fingerprint;a."AuthorizedByCredentialId":=p_actor;a."AuthorizedAtUtc":=p_now;a."ExpiresAtUtc":=p_expires;a."State":='Active';a."Revision":=1;
 INSERT INTO tagekyc.capture_runtime_rotation_authorizations SELECT a.*;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RotationAuthorize',p_idem,p_fingerprint,'Rotation',new_rotation_id,'Applied',1,new_rotation_id,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RotationAuthorize',p_idem,'Applied','Rotation',new_rotation_id,NULL,1,NULL,p_now);
 RETURN QUERY SELECT 'Applied',new_rotation_id,p_agent,p_installation,p_credential,p_generation,'Active',1::bigint,p_expires;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_revoke_rotation(
 p_actor uuid,p_idem uuid,p_rotation uuid,p_expected bigint,p_reason text,
 p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,rotation_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RotationRevoke' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF; SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation; RETURN QUERY SELECT 'Replay',p_rotation,a."State"::text,a."Revision",a."RevokedAtUtc"; RETURN; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation FOR UPDATE;
 IF NOT FOUND OR a."State"<>'Active' OR a."Revision"<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"=p_reason WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RotationRevoke',p_idem,p_fingerprint,'Rotation',p_rotation,'Applied',a."Revision",p_rotation,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RotationRevoke',p_idem,'Applied','Rotation',p_rotation,p_expected,a."Revision",p_reason,p_now);
 RETURN QUERY SELECT 'Applied',p_rotation,a."State"::text,a."Revision",a."RevokedAtUtc";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_complete_rotation(
 p_rotation uuid,p_credential uuid,p_predecessor_generation bigint,
 p_candidate_key uuid,p_business_idem uuid,p_spki bytea,p_thumbprint bytea,
 p_successor_proof bytea,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,
 candidate_key_id uuid,public_key_thumbprint bytea,credential_revision bigint,
 installation_revision bigint,rotation_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; i tagekyc.capture_runtime_installations%ROWTYPE; op record; next_generation bigint;
BEGIN
 IF octet_length(p_spki)<>91 OR octet_length(p_thumbprint)<>32 OR octet_length(p_successor_proof)<>64 OR octet_length(p_fingerprint)<>32 THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_INPUT_INVALID'; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_DENIED'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(a."CaptureAgentId"::text,10));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(a."DeviceInstallationId"::text,20));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential::text,30));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_rotation_completion_operations WHERE "RotationAuthorizationId"=p_rotation AND "BusinessIdempotencyKey"=p_business_idem;
 IF FOUND THEN
  IF op."RequestFingerprint"<>p_fingerprint OR op."CandidateKeyId"<>p_candidate_key THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation;
  IF op."ResultCode"='Expired' THEN RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; ELSE RETURN QUERY SELECT 'Replay',p_credential,op."SuccessorGeneration",p_candidate_key,a."SuccessorPublicKeyThumbprint",op."CredentialRevision",op."InstallationRevision",op."RotationRevision"; END IF; RETURN;
 END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation FOR UPDATE;
 IF FOUND AND a."State"='Active' AND a."ExpiresAtUtc"<=p_now THEN
  UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='RotationAuthorizationExpired' WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(p_rotation,p_business_idem,p_fingerprint,a."DeviceInstallationId",p_credential,p_predecessor_generation,NULL,p_candidate_key,'Expired',NULL,NULL,NULL,p_now,p_now);
  INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(pg_catalog.gen_random_uuid(),p_rotation,p_business_idem,'Expired',p_credential,p_predecessor_generation,NULL,p_now);
  RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF NOT FOUND OR a."State"<>'Active' OR a."CredentialId"<>p_credential OR a."CurrentGeneration"<>p_predecessor_generation THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_DENIED'; END IF;
 SELECT * INTO i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=a."DeviceInstallationId" AND "CurrentCredentialId"=p_credential AND "CurrentCredentialGeneration"=p_predecessor_generation AND "LifecycleState"='Active' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 next_generation:=p_predecessor_generation+1;
 UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Rotated',"Revision"="Revision"+1,"RotatedAtUtc"=p_now,"TerminalReason"='RotationCompleted' WHERE "CredentialId"=p_credential AND "Generation"=p_predecessor_generation AND "State"='Active';
 INSERT INTO tagekyc.capture_runtime_credential_generations("CredentialId","Generation","DeviceInstallationId","CandidateKeyId","PublicVerifierSpki","Algorithm","PublicKeyThumbprint","RolePolicyId","RolePolicyRevision","ValidFromUtc","ValidUntilUtc","State","Revision") SELECT p_credential,next_generation,a."DeviceInstallationId",p_candidate_key,p_spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',p_thumbprint,r."NextRolePolicyId",r."NextRolePolicyRevision",p_now,g."ValidUntilUtc",'Active',1 FROM tagekyc.capture_runtime_registrations r JOIN tagekyc.capture_runtime_credential_generations g ON g."CredentialId"=p_credential AND g."Generation"=p_predecessor_generation JOIN tagekyc.capture_runtime_role_policy_revisions rp ON rp."CatalogId"=r."NextRolePolicyId" AND rp."Revision"=r."NextRolePolicyRevision" WHERE r."CaptureAgentId"=a."CaptureAgentId" AND r."NextRolePolicyId" IS NOT NULL AND rp."EffectiveAtUtc"<=p_now;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_NEXT_ROLE_POLICY_REQUIRED'; END IF;
 UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialGeneration"=next_generation,"Revision"="Revision"+1 WHERE "DeviceInstallationId"=a."DeviceInstallationId" RETURNING * INTO i;
 UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Completed',"Revision"="Revision"+1,"CandidateKeyId"=p_candidate_key,"SuccessorPublicVerifierSpki"=p_spki,"SuccessorPublicKeyThumbprint"=p_thumbprint,"SuccessorGeneration"=next_generation,"CompletedAtUtc"=p_now WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
 INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(p_rotation,p_business_idem,p_fingerprint,a."DeviceInstallationId",p_credential,p_predecessor_generation,next_generation,p_candidate_key,'Applied',1,i."Revision",a."Revision",p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(pg_catalog.gen_random_uuid(),p_rotation,p_business_idem,'Completed',p_credential,p_predecessor_generation,next_generation,p_now);
 RETURN QUERY SELECT 'Applied',p_credential,next_generation,p_candidate_key,p_thumbprint,1::bigint,i."Revision",a."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_replay_completed_rotation(
 p_rotation uuid,p_credential uuid,p_successor_generation bigint,p_candidate_key uuid,
 p_business_idem uuid,p_spki bytea,p_thumbprint bytea,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,candidate_key_id uuid,
 public_key_thumbprint bytea,credential_revision bigint,installation_revision bigint,rotation_revision bigint)
LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $f$
BEGIN
 RETURN QUERY SELECT 'Replay',o."CredentialId",o."SuccessorGeneration",o."CandidateKeyId",a."SuccessorPublicKeyThumbprint",o."CredentialRevision",o."InstallationRevision",o."RotationRevision" FROM tagekyc.capture_runtime_rotation_completion_operations o JOIN tagekyc.capture_runtime_rotation_authorizations a ON a."RotationAuthorizationId"=o."RotationAuthorizationId" JOIN tagekyc.capture_runtime_credential_generations g ON g."CredentialId"=o."CredentialId" AND g."Generation"=o."SuccessorGeneration" WHERE o."RotationAuthorizationId"=p_rotation AND o."BusinessIdempotencyKey"=p_business_idem AND o."CredentialId"=p_credential AND o."SuccessorGeneration"=p_successor_generation AND o."CandidateKeyId"=p_candidate_key AND o."RequestFingerprint"=p_fingerprint AND a."SuccessorPublicVerifierSpki"=p_spki AND a."SuccessorPublicKeyThumbprint"=p_thumbprint AND a."State"='Completed' AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_trust_profile(
 p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,
 p_expires timestamptz,p_runtime_type text,p_retained boolean,p_evidence boolean,
 p_handoff boolean,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; op record; n bigint;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='TrustPublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_trust_profile_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0);
 IF h<>p_expected OR p_expires<=p_effective THEN RAISE EXCEPTION 'TIP88C1C6BA_TRUST_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES(p_catalog,n,p_runtime_type,p_retained,p_evidence,p_handoff,p_effective,p_expires,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'TrustPublish',p_idem,p_fingerprint,'TrustProfile',p_catalog,'Applied',n,p_catalog,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'TrustPublish',p_idem,'Applied','TrustProfile',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_role_policy(
 p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,
 p_roles text[],p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; op record; n bigint;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RolePublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_role_policy_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0); IF h<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_ROLE_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES(p_catalog,n,p_roles,p_effective,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RolePublish',p_idem,p_fingerprint,'RolePolicy',p_catalog,'Applied',n,p_catalog,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RolePublish',p_idem,'Applied','RolePolicy',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_assign_role_policy(p_actor uuid,p_idem uuid,p_agent uuid,p_catalog uuid,p_catalog_revision bigint,p_expected bigint,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,role_policy_id uuid,role_policy_revision bigint,runtime_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE r tagekyc.capture_runtime_registrations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10)); PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RoleAssign' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_agent,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_agent,p_catalog,p_catalog_revision,op."ResultRevision"; RETURN; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_role_policy_revisions WHERE "CatalogId"=p_catalog AND "Revision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROLE_NOT_FOUND'; END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "NextRolePolicyId"=p_catalog,"NextRolePolicyRevision"=p_catalog_revision,"Revision"="Revision"+1 WHERE "CaptureAgentId"=p_agent AND "Revision"=p_expected RETURNING * INTO r; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_RUNTIME_CONFLICT'; END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RoleAssign',p_idem,p_fingerprint,'Runtime',p_agent,'Applied',r."Revision",p_agent,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RoleAssign',p_idem,'Applied','Runtime',p_agent,p_expected,r."Revision",NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_agent,p_catalog,p_catalog_revision,r."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_assign_configuration(p_actor uuid,p_idem uuid,p_agent uuid,p_catalog uuid,p_catalog_revision bigint,p_expected bigint,p_override uuid,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,configuration_revision bigint,override_id uuid,runtime_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE r tagekyc.capture_runtime_registrations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10)); PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='ConfigurationAssign' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_agent,NULL::uuid,NULL::bigint,NULL::uuid,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_agent,p_catalog,p_catalog_revision,p_override,op."ResultRevision"; RETURN; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_configuration_revisions WHERE "CatalogId"=p_catalog AND "Revision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_CONFIGURATION_NOT_FOUND'; END IF;
 IF p_override IS NOT NULL THEN PERFORM 1 FROM tagekyc.capture_runtime_configuration_overrides WHERE "ConfigurationOverrideId"=p_override AND "CaptureAgentId"=p_agent AND "BaseConfigurationId"=p_catalog AND "BaseConfigurationRevision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_OVERRIDE_INVALID'; END IF; END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "ConfigurationId"=p_catalog,"ConfigurationRevision"=p_catalog_revision,"ConfigurationOverrideId"=p_override,"Revision"="Revision"+1 WHERE "CaptureAgentId"=p_agent AND "Revision"=p_expected RETURNING * INTO r; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_RUNTIME_CONFLICT'; END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'ConfigurationAssign',p_idem,p_fingerprint,'Runtime',p_agent,'Applied',r."Revision",p_agent,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'ConfigurationAssign',p_idem,'Applied','Runtime',p_agent,p_expected,r."Revision",NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_agent,p_catalog,p_catalog_revision,p_override,r."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_configuration(p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,p_expires timestamptz,p_enabled boolean,p_budget integer,p_margin integer,p_poll integer,p_dg2 integer,p_selfie integer,p_capture_host bigint,p_custody_stream integer,p_custody_deployment bigint,p_pre_admission integer,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; n bigint; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40)); SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='ConfigurationPublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_configuration_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0); IF h<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_CONFIGURATION_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES(p_catalog,n,p_effective,p_expires,p_enabled,p_budget,p_margin,p_poll,p_dg2,p_selfie,p_capture_host,p_custody_stream,p_custody_deployment,p_pre_admission,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'ConfigurationPublish',p_idem,p_fingerprint,'Configuration',p_catalog,'Applied',n,p_catalog,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'ConfigurationPublish',p_idem,'Applied','Configuration',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

ALTER FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_operator,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_runtime;
ALTER FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
```

## Closed exception boundary

Every explicit `RAISE EXCEPTION` in this companion has PostgreSQL SQLSTATE
`P0001`. The API/application boundary MUST compare both `SqlState` and the exact
primary message below. Before producing the public result it MUST roll back the
entire business transaction; no catalog head, revision, authorization,
credential generation, pointer, operation or event row may remain. Detail,
hint, schema, table, constraint and server message text are never returned.

| Exact primary message | Public HTTP / code |
| --- | --- |
| `TIP88C1C6BA_OPERATOR_DENIED` | `403 ACCESS_DENIED` |
| `TIP88C1C6BA_ROTATION_INPUT_INVALID` | `400 INVALID_INPUT` |
| `TIP88C1C6BA_ROTATION_CONFLICT` | `409 CONFLICT` |
| `TIP88C1C6BA_ROTATION_DENIED` | `403 ACCESS_DENIED` |
| `TIP88C1C6BA_NEXT_ROLE_POLICY_REQUIRED` | `409 CONFLICT` |
| `TIP88C1C6BA_TRUST_HEAD_CONFLICT` | `409 CONFLICT` |
| `TIP88C1C6BA_ROLE_HEAD_CONFLICT` | `409 CONFLICT` |
| `TIP88C1C6BA_ROLE_NOT_FOUND` | `404 RESOURCE_NOT_AVAILABLE` |
| `TIP88C1C6BA_RUNTIME_CONFLICT` | `409 CONFLICT` |
| `TIP88C1C6BA_CONFIGURATION_NOT_FOUND` | `404 RESOURCE_NOT_AVAILABLE` |
| `TIP88C1C6BA_OVERRIDE_INVALID` | `409 CONFLICT` |
| `TIP88C1C6BA_CONFIGURATION_HEAD_CONFLICT` | `409 CONFLICT` |

Any other SQLSTATE/message pair, including a constraint, trigger, serialization,
connection or timeout failure, maps only to `503 NOT_READY` after rollback.
There is no substring, prefix or fallback-to-closest-code matching.

Named proof:
`TransitionSql02_ExceptionMapping_IsClosedAndRollsBack`. It invokes every row
above independently, asserts exact SQLSTATE/message, public status/code, and
zero delta in all T2 target/operation/event tables. Its mutation changes one
message or mapping and MUST turn RED. A separate unknown constraint violation
must prove the `503 NOT_READY` catch-all and the same zero-residue condition.

## Stage-1 R13 authentication seam

R13 uses the sole atomic classifier
`capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz)`
projected by Transition SQL 04. The predecessor branch selects only the
non-null predecessor candidate and requires `CredentialRotation`; the successor
branch selects only the non-null successor candidate and compares it to the
durable completion fingerprint with `IS NOT DISTINCT FROM`. Both branches
recheck the exact rotation tuple under ranks 10→20→30→60 before one nonce
insert. R13a/R13b retain their own exact tuple recheck and receive the returned
selected fingerprint unchanged.

## Down (current tranche)

```sql
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
DROP FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz);
```
