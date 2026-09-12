# TIP-88C1-C6B-A1 — Transition SQL 01: root, enrollment and lifecycle

> Mechanical R27 SHA-binding successor, 2026-09-11. Preserved predecessor `tip_88c1_c6b_a1_transition_sql_01_root_enrollment_lifecycle.md` SHA `F3E7176DFBDAD72E276F2962E5ED5BAC96483FB487017794DDAF7B99BA202DB6`. Consumes `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`. SQL fenced blocks are unchanged; this is not new SQL authority.

**Status:** REVIEW CANDIDATE — NOT AUTHORITY  
**Consumed operation master:** SHA-256 `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`  
**Consumed literal DDL master:** SHA-256 `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`

This companion contains the complete literal PostgreSQL catalogue for R01–R09.

## R01–R04 literal functions

```sql
CREATE FUNCTION tagekyc.c6ba_root_provision_platform_credential(
 p_operation_id uuid, p_principal_id uuid, p_expires_at_utc timestamptz,
 p_key_lookup_prefix text, p_secret_digest bytea, p_verifier_pepper_version integer,
 p_request_fingerprint bytea, p_now_utc timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,key_lookup_prefix text,
 secret_available boolean,expires_at_utc timestamptz,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE o tagekyc.platform_operator_root_operations%ROWTYPE; c_id uuid; c tagekyc.platform_operator_credentials%ROWTYPE;
BEGIN
 IF p_operation_id IS NULL OR p_principal_id IS NULL OR p_principal_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_expires_at_utc<=p_now_utc OR p_key_lookup_prefix!~'^[A-Za-z0-9_-]{12}$'
 OR pg_catalog.octet_length(p_secret_digest)<>32 OR p_verifier_pepper_version<=0
 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_key_lookup_prefix,1)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_key_lookup_prefix,1)));
 SELECT * INTO o FROM tagekyc.platform_operator_root_operations WHERE "OperationId"=p_operation_id;
 IF FOUND THEN
  IF o."OperationKind"<>'Provision' OR o."RequestFingerprint"<>p_request_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,false,NULL::timestamptz,NULL::bigint;
  ELSE SELECT * INTO c FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=o."TargetCredentialId"; RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',o."TargetCredentialId",c."KeyLookupPrefix"::text,false,c."ExpiresAtUtc",o."ResultRevision"; END IF; RETURN;
 END IF;
 c_id:=pg_catalog.gen_random_uuid();
 INSERT INTO tagekyc.platform_operator_credentials VALUES(c_id,p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,p_principal_id,ARRAY['operator.capture-runtime.manage']::text[],'Active',1,p_now_utc,p_expires_at_utc,NULL,NULL);
 INSERT INTO tagekyc.platform_operator_root_operations VALUES(p_operation_id,'Provision',p_request_fingerprint,p_principal_id,c_id,'Applied',1,p_now_utc,p_now_utc);
 INSERT INTO tagekyc.platform_operator_root_events VALUES(pg_catalog.gen_random_uuid(),p_operation_id,'Provisioned',p_principal_id,c_id,NULL,1,p_now_utc);
 RETURN QUERY SELECT 'Created',c_id,p_key_lookup_prefix,true,p_expires_at_utc,1::bigint;
END $function$;

CREATE FUNCTION tagekyc.c6ba_root_revoke_platform_credential(
 p_operation_id uuid,p_credential_id uuid,p_expected_revision bigint,p_reason text,
 p_request_fingerprint bytea,p_now_utc timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE o tagekyc.platform_operator_root_operations%ROWTYPE; c tagekyc.platform_operator_credentials%ROWTYPE;
BEGIN
 IF p_expected_revision<=0 OR p_reason<>'DeploymentRevocation' OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_credential_id::text,1)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_credential_id::text,1)));
 SELECT * INTO o FROM tagekyc.platform_operator_root_operations WHERE "OperationId"=p_operation_id;
 IF FOUND THEN IF o."OperationKind"='Revoke' AND o."RequestFingerprint"=p_request_fingerprint THEN RETURN QUERY SELECT 'Revoked',o."TargetCredentialId",'Revoked',o."ResultRevision",o."CompletedAtUtc"; ELSE RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; END IF; RETURN; END IF;
 SELECT * INTO c FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_credential_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 IF c."State"<>'Active' OR c."Revision"<>p_expected_revision THEN RETURN QUERY SELECT 'Conflict',c."CredentialId",c."State"::text,c."Revision",c."RevokedAtUtc"; RETURN; END IF;
 UPDATE tagekyc.platform_operator_credentials SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now_utc,"RevocationReason"=p_reason WHERE "CredentialId"=p_credential_id RETURNING * INTO c;
 INSERT INTO tagekyc.platform_operator_root_operations VALUES(p_operation_id,'Revoke',p_request_fingerprint,c."PrincipalId",p_credential_id,'Applied',c."Revision",p_now_utc,p_now_utc);
 INSERT INTO tagekyc.platform_operator_root_events VALUES(pg_catalog.gen_random_uuid(),p_operation_id,'Revoked',c."PrincipalId",p_credential_id,p_expected_revision,c."Revision",p_now_utc);
 RETURN QUERY SELECT 'Revoked',c."CredentialId",c."State"::text,c."Revision",c."RevokedAtUtc";
END $function$;

CREATE FUNCTION tagekyc.capture_runtime_issue_bootstrap(
 p_actor_credential_id uuid,p_idempotency_key uuid,p_runtime_type text,
 p_trust_profile_id uuid,p_trust_profile_revision bigint,p_role_policy_id uuid,
 p_role_policy_revision bigint,p_configuration_id uuid,p_configuration_revision bigint,
 p_expires_at_utc timestamptz,p_attestation_digest bytea,p_key_lookup_prefix text,
 p_secret_digest bytea,p_verifier_pepper_version integer,p_request_fingerprint bytea,p_now_utc timestamptz)
RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,secret_available boolean,expires_at_utc timestamptz,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE op tagekyc.capture_runtime_management_operations%ROWTYPE; b_id uuid; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;
BEGIN
 IF p_runtime_type<>'Managed' OR p_expires_at_utc<=p_now_utc OR p_key_lookup_prefix!~'^[A-Za-z0-9_-]{12}$' OR pg_catalog.octet_length(p_secret_digest)<>32 OR p_verifier_pepper_version<=0 OR pg_catalog.octet_length(p_attestation_digest)<>32 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_idempotency_key::text,5),pg_catalog.hashtextextended(p_key_lookup_prefix,5)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_idempotency_key::text,5),pg_catalog.hashtextextended(p_key_lookup_prefix,5)));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_actor_credential_id AND "State"='Active' AND "ExpiresAtUtc">p_now_utc AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor_credential_id AND "OperationKind"='BootstrapIssue' AND "IdempotencyKey"=p_idempotency_key;
 IF FOUND THEN IF op."RequestFingerprint"=p_request_fingerprint THEN SELECT * INTO STRICT b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=op."ResultId"; RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',op."ResultId",false,b."ExpiresAtUtc",op."ResultRevision"; ELSE RETURN QUERY SELECT 'Conflict',NULL::uuid,false,NULL::timestamptz,NULL::bigint; END IF; RETURN; END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions WHERE "CatalogId"=p_trust_profile_id AND "Revision"=p_trust_profile_revision) OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions WHERE "CatalogId"=p_role_policy_id AND "Revision"=p_role_policy_revision) OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions WHERE "CatalogId"=p_configuration_id AND "Revision"=p_configuration_revision) THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 b_id:=pg_catalog.gen_random_uuid();
 INSERT INTO tagekyc.capture_runtime_bootstrap_issuances("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion","RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision","ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint","IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision") VALUES(b_id,p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,p_runtime_type,p_trust_profile_id,p_trust_profile_revision,p_role_policy_id,p_role_policy_revision,p_configuration_id,p_configuration_revision,p_attestation_digest,p_request_fingerprint,p_idempotency_key,p_actor_credential_id,p_now_utc,p_expires_at_utc,'Active',1);
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor_credential_id,'BootstrapIssue',p_idempotency_key,p_request_fingerprint,'Bootstrap',b_id,'Applied',1,b_id,p_now_utc,p_now_utc);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor_credential_id,'BootstrapIssue',p_idempotency_key,'Applied','Bootstrap',b_id,NULL,1,NULL,p_now_utc);
 RETURN QUERY SELECT 'Created',b_id,true,p_expires_at_utc,1::bigint;
END $function$;

ALTER FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
```

## R04–R05 literal functions

```sql
CREATE FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a ALIAS FOR $1; k ALIAS FOR $2; id ALIAS FOR $3; er ALIAS FOR $4; why ALIAS FOR $5; fp ALIAS FOR $6; n ALIAS FOR $7; o tagekyc.capture_runtime_management_operations%ROWTYPE; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;
BEGIN
 IF er<=0 OR why<>'OperatorRevocation' OR octet_length(fp)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(LEAST(hashtextextended(id::text,5),hashtextextended(k::text,5)));
 PERFORM pg_advisory_xact_lock(GREATEST(hashtextextended(id::text,5),hashtextextended(k::text,5)));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=a AND "State"='Active' AND "ExpiresAtUtc">n AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT * INTO o FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=a AND "OperationKind"='BootstrapRevoke' AND "IdempotencyKey"=k;
 IF FOUND THEN IF o."RequestFingerprint"<>fp THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;ELSE SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=o."TargetId";RETURN QUERY SELECT 'Revoked',b."BootstrapIssuanceId",b."State"::text,b."Revision",b."RevokedAtUtc";END IF;RETURN;END IF;
 SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 IF b."State"<>'Active' OR b."Revision"<>er THEN RETURN QUERY SELECT 'Conflict',b."BootstrapIssuanceId",b."State"::text,b."Revision",b."RevokedAtUtc";RETURN;END IF;
 UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=n,"TerminalReason"=why WHERE "BootstrapIssuanceId"=id RETURNING * INTO b;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(a,'BootstrapRevoke',k,fp,'Bootstrap',id,'Applied',b."Revision",id,n,n);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),a,'BootstrapRevoke',k,'Applied','Bootstrap',id,er,b."Revision",why,n);
 RETURN QUERY SELECT 'Revoked',id,b."State"::text,b."Revision",b."RevokedAtUtc";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,device_installation_id uuid,credential_id uuid,generation bigint,runtime_revision bigint,installation_revision bigint,credential_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE bid ALIAS FOR $1; opid ALIAS FOR $2; fp ALIAS FOR $3; kid ALIAS FOR $4; spki ALIAS FOR $5; thumb ALIAS FOR $6; sat ALIAS FOR $7; nonce ALIAS FOR $8; proof ALIAS FOR $9; digest ALIAS FOR $10; n ALIAS FOR $11; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;o tagekyc.capture_runtime_bootstrap_redemption_operations%ROWTYPE;a uuid;i uuid;c uuid;
BEGIN
 IF octet_length(fp)<>32 OR octet_length(spki)<>91 OR octet_length(thumb)<>32 OR octet_length(nonce)<>32 OR octet_length(proof)<>64 OR octet_length(digest)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(bid::text,5));
 PERFORM pg_advisory_xact_lock(hashtextextended(bid::text,10));
 PERFORM pg_advisory_xact_lock(hashtextextended(kid::text,20));
 PERFORM pg_advisory_xact_lock(hashtextextended(kid::text,30));
 SELECT * INTO o FROM tagekyc.capture_runtime_bootstrap_redemption_operations WHERE "BootstrapIssuanceId"=bid AND "RedeemOperationId"=opid;
 IF FOUND THEN IF o."RequestFingerprint"<>fp OR o."CandidateKeyId"<>kid THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; ELSIF o."ResultCode"='Expired' THEN RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; ELSE RETURN QUERY SELECT 'Replay',o."CaptureAgentId",o."DeviceInstallationId",o."CredentialId",o."Generation",1::bigint,2::bigint,2::bigint; END IF;RETURN;END IF;
 SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=bid FOR UPDATE;
 IF FOUND AND b."State"='Active' AND b."ExpiresAtUtc"<=n THEN
  UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=n,"TerminalReason"='BootstrapExpired' WHERE "BootstrapIssuanceId"=bid;
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(bid,opid,fp,kid,'Expired',NULL,NULL,NULL,NULL,n,n);
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),bid,opid,'Expired',NULL,NULL,NULL,NULL,n);
  RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF NOT FOUND OR b."State"<>'Active' OR b."SecretDigest"<>digest OR sat<n-interval '150 seconds' OR sat>n+interval '150 seconds' THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;RETURN;END IF;
 a:=gen_random_uuid();i:=gen_random_uuid();c:=gen_random_uuid();
 INSERT INTO tagekyc.capture_runtime_registrations("CaptureAgentId","RuntimeType","TrustProfileId","TrustProfileRevision","ConfigurationId","ConfigurationRevision","LifecycleState","Revision","CreatedAtUtc") VALUES(a,'Managed',b."TrustProfileId",b."TrustProfileRevision",b."ConfigurationId",b."ConfigurationRevision",'Active',1,n);
 INSERT INTO tagekyc.capture_runtime_installations("DeviceInstallationId","CaptureAgentId","LifecycleState","Revision","EnrolledAtUtc") VALUES(i,a,'Pending',1,n);
 INSERT INTO tagekyc.capture_runtime_credential_generations("CredentialId","Generation","DeviceInstallationId","CandidateKeyId","PublicVerifierSpki","Algorithm","PublicKeyThumbprint","RolePolicyId","RolePolicyRevision","ValidFromUtc","ValidUntilUtc","State","Revision") VALUES(c,1,i,kid,spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',thumb,b."RolePolicyId",b."RolePolicyRevision",n,b."ExpiresAtUtc",'Pending',1);
 UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"=c,"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"=i;UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"=c;UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Redeemed',"Revision"="Revision"+1,"RedeemedAtUtc"=n,"CaptureAgentId"=a,"DeviceInstallationId"=i,"CredentialId"=c,"CredentialGeneration"=1 WHERE "BootstrapIssuanceId"=bid;
 INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(bid,opid,fp,kid,'Applied',a,i,c,1,n,n);INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),bid,opid,'Redeemed',a,i,c,1,n);RETURN QUERY SELECT 'Created',a,i,c,1::bigint,1::bigint,2::bigint,2::bigint;
END $f$;
ALTER FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
```

## R06–R09 literal functions

```sql
CREATE FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean)
RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE actor ALIAS FOR $1; idem ALIAS FOR $2; aid ALIAS FOR $3; expected ALIAS FOR $4; reason ALIAS FOR $5; fp ALIAS FOR $6; n ALIAS FOR $7; kind ALIAS FOR $8; from_state ALIAS FOR $9; to_state ALIAS FOR $10; terminalize ALIAS FOR $11; op tagekyc.capture_runtime_management_operations%ROWTYPE; r tagekyc.capture_runtime_registrations%ROWTYPE; i tagekyc.capture_runtime_installations%ROWTYPE; g tagekyc.capture_runtime_credential_generations%ROWTYPE; lineage_count bigint;
BEGIN
 IF expected<=0 OR octet_length(fp)<>32 OR NOT ((kind='RuntimeSuspend' AND reason='OperatorSuspension') OR (kind='RuntimeReactivate' AND reason='OperatorReactivation') OR (kind='RuntimeRevoke' AND reason='OperatorRevocation') OR (kind='RuntimeRetire' AND reason='OperatorRetirement')) THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(aid::text,10));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=actor AND "State"='Active' AND "ExpiresAtUtc">n AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=actor AND "OperationKind"=kind AND "IdempotencyKey"=idem;
 IF FOUND THEN IF op."RequestFingerprint"<>fp THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;ELSE RETURN QUERY SELECT 'Applied',op."TargetId",CASE op."OperationKind" WHEN 'RuntimeSuspend' THEN 'Suspended' WHEN 'RuntimeReactivate' THEN 'Active' WHEN 'RuntimeRevoke' THEN 'Revoked' WHEN 'RuntimeRetire' THEN 'Retired' END,op."ResultRevision",op."CompletedAtUtc";END IF;RETURN;END IF;
 SELECT * INTO r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=aid;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT pg_catalog.count(*) INTO lineage_count FROM tagekyc.capture_runtime_installations
 WHERE "CaptureAgentId"=aid
   AND ((kind='RuntimeRetire' AND "LifecycleState"='Revoked' AND "RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND "LifecycleState"='Active'));
 IF lineage_count<>1 THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 SELECT * INTO STRICT i FROM tagekyc.capture_runtime_installations
 WHERE "CaptureAgentId"=aid
   AND ((kind='RuntimeRetire' AND "LifecycleState"='Revoked' AND "RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND "LifecycleState"='Active'));
 PERFORM pg_advisory_xact_lock(hashtextextended(i."DeviceInstallationId"::text,20));
 PERFORM pg_advisory_xact_lock(hashtextextended(i."CurrentCredentialId"::text||':'||i."CurrentCredentialGeneration"::text,30));
 SELECT * INTO r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=aid FOR UPDATE;
 SELECT ix.* INTO i FROM tagekyc.capture_runtime_installations ix
 WHERE ix."DeviceInstallationId"=i."DeviceInstallationId"
   AND ((kind='RuntimeRetire' AND ix."LifecycleState"='Revoked' AND ix."RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND ix."LifecycleState"='Active')) FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 SELECT * INTO g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=i."DeviceInstallationId" AND "CredentialId"=i."CurrentCredentialId" AND "Generation"=i."CurrentCredentialGeneration" AND ((kind='RuntimeRetire' AND "State"='Revoked') OR (kind<>'RuntimeRetire' AND "State"='Active')) FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 IF r."Revision"<>expected OR (kind IN ('RuntimeSuspend','RuntimeReactivate','RuntimeRetire') AND r."LifecycleState"<>from_state) OR (kind='RuntimeRevoke' AND r."LifecycleState" NOT IN ('Active','Suspended')) THEN RETURN QUERY SELECT 'Conflict',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "LifecycleState"=to_state,"Revision"="Revision"+1,"SuspendedAtUtc"=CASE WHEN to_state='Suspended' THEN n WHEN to_state='Active' THEN NULL ELSE "SuspendedAtUtc" END,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"LifecycleReason"=CASE WHEN to_state='Active' THEN NULL ELSE reason END WHERE "CaptureAgentId"=aid RETURNING * INTO r;
 IF terminalize AND FOUND THEN UPDATE tagekyc.capture_runtime_installations SET "LifecycleState"=to_state,"Revision"="Revision"+1,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"LifecycleReason"=reason WHERE "DeviceInstallationId"=i."DeviceInstallationId"; IF g."CredentialId" IS NOT NULL THEN UPDATE tagekyc.capture_runtime_credential_generations SET "State"=to_state,"Revision"="Revision"+1,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"TerminalReason"=reason WHERE "CredentialId"=g."CredentialId" AND "Generation"=g."Generation";END IF;END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(actor,kind,idem,fp,'Runtime',aid,'Applied',r."Revision",aid,n,n);INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),actor,kind,idem,'Applied','Runtime',aid,expected,r."Revision",reason,n);RETURN QUERY SELECT 'Applied',aid,r."LifecycleState"::text,r."Revision",n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeSuspend','Active','Suspended',false) $f$;
CREATE FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeReactivate','Suspended','Active',false) $f$;
CREATE FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeRevoke','Active','Revoked',true) $f$;
CREATE FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeRetire','Revoked','Retired',true) $f$;
ALTER FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean) FROM PUBLIC;
ALTER FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
```

## Stage-1 R05 persisted-version resolver

```sql
CREATE FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(
    p_bootstrap_issuance_id uuid)
RETURNS TABLE(verifier_pepper_version integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT b."VerifierPepperVersion"
    FROM tagekyc.capture_runtime_bootstrap_issuances b
    WHERE b."BootstrapIssuanceId"=p_bootstrap_issuance_id;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) TO tagekyc_capture_runtime_application;
```

## Status

R01–R09 are present. PostgreSQL execution status is recorded after the Down
catalogue below.

## Executable Down

```sql
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid);
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
DROP FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean);
DROP FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz);
```

Proof owners: `PlatformCredentialAuthentication_HasNoClientPartition`,
`PlatformRootRevoke_IsIdempotentAndAudited`,
`BootstrapIssue_SecretOnceAndExactReplay`,
`BootstrapIssueRevoke_OneTerminalWinner`,
`Enrollment_AtomicActivationAndLostResponseReplay`,
`RuntimeSuspendReactivate_ExpectedRevisionSerializes`, and
`RuntimeRevokeRetire_TerminalWins`.

PostgreSQL 16 feasibility: exact base DDL Up plus every SQL block in this
companion, including Down, completed with `ON_ERROR_STOP=1` and exit code `0`.
