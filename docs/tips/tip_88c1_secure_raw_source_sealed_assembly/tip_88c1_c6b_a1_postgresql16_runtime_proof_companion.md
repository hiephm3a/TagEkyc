# TIP-88C1-C6B-A1 — PostgreSQL 16 Runtime Proof Companion

**Status:** EXECUTABLE REVIEW EVIDENCE — NOT IMPLEMENTATION AUTHORITY

Exact bound inputs:

- OP `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`
- DDL `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`
- T1 `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102`
- T2 `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115`
- T3 `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F`
- T4 `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C`

Run after all bound Up catalogues as migration owner in an isolated database.

```sql
BEGIN;
INSERT INTO tagekyc.platform_operator_credentials VALUES
('00000000-0000-4000-8000-000000000001','abcdefghijkl',decode(repeat('11',32),'hex'),1,'00000000-0000-4000-8000-000000000002',ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 minute',now()+interval '1 day',NULL,NULL);
INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES ('10000000-0000-4000-8000-000000000001',1,ARRAY['Configuration'],now()-interval '1 hour','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES ('10000000-0000-4000-8000-000000000001',1,1,now());
INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES ('20000000-0000-4000-8000-000000000001',1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES ('20000000-0000-4000-8000-000000000001',1,1,now());
INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES ('30000000-0000-4000-8000-000000000001',1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,'00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES ('30000000-0000-4000-8000-000000000001',1,1,now());
INSERT INTO tagekyc.capture_runtime_registrations VALUES ('40000000-0000-4000-8000-000000000001','Managed','20000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_installations VALUES ('50000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001',NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_credential_generations VALUES ('60000000-0000-4000-8000-000000000001',1,'50000000-0000-4000-8000-000000000001','70000000-0000-4000-8000-000000000001',decode(repeat('04',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('22',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"='60000000-0000-4000-8000-000000000001',"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"='50000000-0000-4000-8000-000000000001';
UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"='60000000-0000-4000-8000-000000000001';

-- Publish newer heads without assigning them.
SELECT * FROM tagekyc.capture_runtime_publish_role_policy('00000000-0000-4000-8000-000000000001','81000000-0000-4000-8000-000000000001','10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',ARRAY['Bind'],decode(repeat('31',32),'hex'),now());
SELECT * FROM tagekyc.capture_runtime_publish_trust_profile('00000000-0000-4000-8000-000000000001','82000000-0000-4000-8000-000000000001','20000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '2 days','Managed',true,true,false,decode(repeat('32',32),'hex'),now());
SELECT * FROM tagekyc.capture_runtime_publish_configuration('00000000-0000-4000-8000-000000000001','83000000-0000-4000-8000-000000000001','30000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '2 days',true,30,100,30,500,500,5000,5000,5000,512,decode(repeat('33',32),'hex'),now());

DO $proof$ DECLARE c record; r record; a record; n record; d bigint; x record; y record; z record; denied record; BEGIN
 SELECT * INTO c FROM tagekyc.capture_runtime_resolve_configuration('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,now());
 SELECT * INTO r FROM tagekyc.capture_runtime_read_readiness('40000000-0000-4000-8000-000000000001',now());
 IF c.result_code<>'AVAILABLE' OR c.configuration_revision<>1 OR NOT r.database_ready OR r.role_policy_revision<>1 OR r.configuration_revision<>1 THEN RAISE EXCEPTION 'HEAD_WITHOUT_ASSIGNMENT_INVALIDATED_FROZEN_REVISION'; END IF;

 SELECT * INTO a FROM tagekyc.platform_operator_authenticate('abcdefghijkl',now());
 IF a.credential_id IS NULL OR EXISTS(SELECT 1 FROM tagekyc.platform_operator_authenticate('xxxxxxxxxxxx',now())) THEN RAISE EXCEPTION 'OPERATOR_AUTH_PROOF_FAILED'; END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_resolve_verifier('60000000-0000-4000-8000-000000000001',1,now())) OR EXISTS(SELECT 1 FROM tagekyc.capture_runtime_resolve_verifier('60000000-0000-4000-8000-000000000001',2,now())) THEN RAISE EXCEPTION 'RUNTIME_AUTH_PROOF_FAILED'; END IF;

 SELECT * INTO n FROM tagekyc.capture_runtime_claim_nonce('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,'Configuration',decode(repeat('41',32),'hex'),now(),now());
 IF n.result_code<>'ADMITTED' THEN RAISE EXCEPTION 'NONCE_FIRST_FAILED'; END IF;
 SELECT * INTO n FROM tagekyc.capture_runtime_claim_nonce('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,'Configuration',decode(repeat('41',32),'hex'),now(),now());
 IF n.result_code<>'REPLAY' THEN RAISE EXCEPTION 'NONCE_REPLAY_FAILED'; END IF;
 SELECT * INTO n FROM tagekyc.capture_runtime_claim_nonce('40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,'Bind',decode(repeat('42',32),'hex'),now(),now());
 IF n.result_code<>'ACCESS_DENIED' THEN RAISE EXCEPTION 'FROZEN_ROLE_COLLISION_WENT_GREEN'; END IF;

 INSERT INTO tagekyc.capture_runtime_request_nonces VALUES('60000000-0000-4000-8000-000000000001',1,decode(repeat('43',32),'hex'),now()-interval '301 seconds',now()-interval '301 seconds',now()-interval '151 seconds');
 SELECT deleted_count INTO d FROM tagekyc.capture_runtime_cleanup_nonces(now(),1);
 IF d<>1 OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_request_nonces WHERE "Nonce"=decode(repeat('41',32),'hex')) THEN RAISE EXCEPTION 'NONCE_CLEANUP_PROOF_FAILED'; END IF;

 SELECT * INTO x FROM tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,2,'CredentialCompromise',decode(repeat('51',32),'hex'),now());
 SELECT * INTO y FROM tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,2,'CredentialCompromise',decode(repeat('51',32),'hex'),now());
 SELECT * INTO z FROM tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,2,'CredentialCompromise',decode(repeat('52',32),'hex'),now());
 IF x.result_code<>'Revoked' OR y.result_code<>'Revoked' OR z.result_code<>'Conflict' THEN RAISE EXCEPTION 'R10_REPLAY_CONFLICT_PROOF_FAILED'; END IF;
 UPDATE tagekyc.platform_operator_credentials SET "State"='Revoked',"RevokedAtUtc"=now(),"RevocationReason"='ProofRevocation',"Revision"="Revision"+1 WHERE "CredentialId"='00000000-0000-4000-8000-000000000001';
 SELECT * INTO denied FROM tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,2,'CredentialCompromise',decode(repeat('51',32),'hex'),now());
 IF denied.result_code<>'Denied' THEN RAISE EXCEPTION 'REVOKED_OPERATOR_REPLAY_DISCLOSED_RESULT'; END IF;
END $proof$;
SET CONSTRAINTS ALL IMMEDIATE;

-- R26 SQL rejects the nullable wire form and accepts only the landed
-- Application-normalized form. This proves ownership of normalization.
SET CONSTRAINTS ALL DEFERRED;
INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'90000000-0000-4000-8000-000000000001','91000000-0000-4000-8000-000000000001','Pending',now()+interval '1 day','stored-r','stored-c');
INSERT INTO tagekyc.audit_events("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") VALUES ('93000000-0000-4000-8000-000000000000','91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','ClientApplication','adjacent-prefix','SESSION_CANCELLED','sha256:localdev-session-cancelled','AdjacentBaseline','stored-r','stored-c',now()-interval '1 second');
DO $proof$ DECLARE bad record; good record; BEGIN
 SELECT * INTO bad FROM tagekyc.capture_runtime_cancel_session_with_capability('91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001',NULL,NULL,NULL,now(),'clientprefix','93000000-0000-4000-8000-000000000001');
 IF bad.result_code<>'INVALID_INPUT' THEN RAISE EXCEPTION 'R26_NULL_WIRE_REACHED_SQL'; END IF;
 SELECT * INTO good FROM tagekyc.capture_runtime_cancel_session_with_capability('91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','ClientRequested','stored-r','stored-c',now(),'clientprefix','93000000-0000-4000-8000-000000000002');
 IF good.result_code<>'APPLIED' THEN RAISE EXCEPTION 'R26_NORMALIZED_FAILED'; END IF;
END $proof$;
SET CONSTRAINTS ALL IMMEDIATE;
ROLLBACK;
```

## Required result

The script runs under `ON_ERROR_STOP=1`, reaches `ROLLBACK`, and leaves no
fixture residue. Removing frozen-revision evaluation, verifier denial, nonce
uniqueness/strict cleanup, R10 fingerprint comparison, or R26 normalization
ownership turns its named assertion red.

## Reproducible two-session R10 race

Run the following second SQL block in the same isolated PostgreSQL 16 database
after the first block. It uses `dblink` only as a bounded two-session proof
driver. Both calls use the same idempotency key and distinct targets and
fingerprints. Session A holds rank 60 for two seconds; Session B has a five
second statement timeout and must observe A's committed operation.

```sql
CREATE EXTENSION IF NOT EXISTS dblink;
BEGIN;
SET CONSTRAINTS ALL DEFERRED;
INSERT INTO tagekyc.platform_operator_credentials VALUES
('00000000-0000-4000-8000-000000000001','abcdefghijkl',decode(repeat('11',32),'hex'),1,'00000000-0000-4000-8000-000000000002',ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 minute',now()+interval '1 day',NULL,NULL);
INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES ('10000000-0000-4000-8000-000000000001',1,ARRAY['Configuration'],now()-interval '1 hour','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES ('20000000-0000-4000-8000-000000000001',1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES ('30000000-0000-4000-8000-000000000001',1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,'00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_registrations VALUES
('40000000-0000-4000-8000-000000000001','Managed','20000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL),
('40000000-0000-4000-8000-000000000002','Managed','20000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_installations VALUES
('50000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001',NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL),
('50000000-0000-4000-8000-000000000002','40000000-0000-4000-8000-000000000002',NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_credential_generations VALUES
('60000000-0000-4000-8000-000000000001',1,'50000000-0000-4000-8000-000000000001','70000000-0000-4000-8000-000000000001',decode(repeat('04',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('21',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL),
('60000000-0000-4000-8000-000000000002',1,'50000000-0000-4000-8000-000000000002','70000000-0000-4000-8000-000000000002',decode(repeat('05',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('22',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"=CASE "DeviceInstallationId" WHEN '50000000-0000-4000-8000-000000000001' THEN '60000000-0000-4000-8000-000000000001'::uuid ELSE '60000000-0000-4000-8000-000000000002'::uuid END,"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2;
UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2;
SET CONSTRAINTS ALL IMMEDIATE;
COMMIT;

SELECT dblink_connect('r10a',pg_catalog.format('dbname=%L user=%L options=%L',current_database(),session_user,'-cstatement_timeout=5000'));
SELECT dblink_connect('r10b',pg_catalog.format('dbname=%L user=%L options=%L',current_database(),session_user,'-cstatement_timeout=5000'));
SELECT dblink_send_query('r10a',$q$WITH held AS MATERIALIZED (SELECT pg_advisory_xact_lock(hashtextextended('84000000-0000-4000-8000-000000000001',60)),pg_sleep(2)) SELECT f.result_code FROM held CROSS JOIN LATERAL tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,2,'CredentialCompromise',decode(repeat('51',32),'hex'),now()) f;$q$);
SELECT pg_sleep(0.2);
SELECT dblink_send_query('r10b',$q$SELECT result_code FROM tagekyc.capture_runtime_revoke_credential('00000000-0000-4000-8000-000000000001','84000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000002','50000000-0000-4000-8000-000000000002','60000000-0000-4000-8000-000000000002',1,2,'CredentialCompromise',decode(repeat('52',32),'hex'),now());$q$);
CREATE TEMP TABLE r10a_result(value text);
CREATE TEMP TABLE r10b_result(value text);
INSERT INTO r10a_result SELECT result_code FROM dblink_get_result('r10a') AS t(result_code text);
INSERT INTO r10b_result SELECT result_code FROM dblink_get_result('r10b') AS t(result_code text);
DO $proof$ BEGIN
 IF (SELECT value FROM r10a_result)<>'Revoked' OR (SELECT value FROM r10b_result)<>'Conflict' THEN RAISE EXCEPTION 'R10_CONCURRENT_OUTCOME_FAILED'; END IF;
 IF (SELECT count(*) FROM tagekyc.capture_runtime_management_operations WHERE "OperationKind"='CredentialRevoke')<>1 THEN RAISE EXCEPTION 'R10_CONCURRENT_OPERATION_COUNT_FAILED'; END IF;
 IF (SELECT count(*) FROM tagekyc.capture_runtime_management_events WHERE "OperationKind"='CredentialRevoke')<>1 THEN RAISE EXCEPTION 'R10_CONCURRENT_EVENT_COUNT_FAILED'; END IF;
 IF (SELECT count(*) FROM tagekyc.capture_runtime_credential_generations WHERE "State"='Revoked')<>1 THEN RAISE EXCEPTION 'R10_CONCURRENT_WINNER_COUNT_FAILED'; END IF;
END $proof$;
SELECT dblink_disconnect('r10a');
SELECT dblink_disconnect('r10b');
```

The isolated proof database is discarded after success. Removing the rank-60
advisory acquisition permits a unique-constraint exception or two target
mutations and turns at least one named assertion red.
