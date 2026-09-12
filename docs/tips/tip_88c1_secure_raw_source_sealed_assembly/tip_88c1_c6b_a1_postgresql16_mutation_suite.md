# TIP-88C1-C6B-A1 — PostgreSQL 16 mutation suite

**Status:** EXECUTABLE REVIEW EVIDENCE — NOT IMPLEMENTATION AUTHORITY

Bound SHA-256:

- OP: `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`
- DDL: `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`
- T1: `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102`
- T2: `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115`
- T3: `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F`
- T4: `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C`

Run after the bound Up catalogues, as migration owner, in an isolated database.

```sql
BEGIN;
INSERT INTO tagekyc.platform_operator_credentials VALUES
('00000000-0000-4000-8000-000000000001','abcdefghijkl',decode(repeat('11',32),'hex'),1,'00000000-0000-4000-8000-000000000002',ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 minute',now()+interval '1 day',NULL,NULL);
INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES ('10000000-0000-4000-8000-000000000001',1,ARRAY['Configuration'],now()-interval '1 hour','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES ('20000000-0000-4000-8000-000000000001',1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day','00000000-0000-4000-8000-000000000001',now());
INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES ('30000000-0000-4000-8000-000000000001',1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,'00000000-0000-4000-8000-000000000001',now());

DO $smoke$ DECLARE n timestamptz:=now(); p record; q record; b record; br record; e record; er record; BEGIN
 SELECT * INTO p FROM tagekyc.c6ba_root_provision_platform_credential('01000000-0000-4000-8000-000000000001','01000000-0000-4000-8000-000000000002',n+interval '1 day','rootSmoke001',decode(repeat('51',32),'hex'),1,decode(repeat('52',32),'hex'),n);
 SELECT * INTO q FROM tagekyc.c6ba_root_provision_platform_credential('01000000-0000-4000-8000-000000000001','01000000-0000-4000-8000-000000000002',n+interval '1 day','rootSmoke001',decode(repeat('51',32),'hex'),1,decode(repeat('52',32),'hex'),n);
 IF p.result_code<>'Created' OR q.result_code<>'ExistingMatchSecretUnavailable' OR q.credential_id<>p.credential_id OR q.key_lookup_prefix<>'rootSmoke001' OR q.secret_available THEN RAISE EXCEPTION 'T1_R01_FIRST_REPLAY_SMOKE_FAILED'; END IF;
 SELECT * INTO p FROM tagekyc.c6ba_root_revoke_platform_credential('01000000-0000-4000-8000-000000000003',p.credential_id,1,'DeploymentRevocation',decode(repeat('53',32),'hex'),n);
 SELECT * INTO q FROM tagekyc.c6ba_root_revoke_platform_credential('01000000-0000-4000-8000-000000000003',p.credential_id,1,'DeploymentRevocation',decode(repeat('53',32),'hex'),n);
 IF p.result_code<>'Revoked' OR q.result_code<>'Revoked' OR q.revision<>p.revision THEN RAISE EXCEPTION 'T1_R02_FIRST_REPLAY_SMOKE_FAILED'; END IF;

 SELECT * INTO b FROM tagekyc.capture_runtime_issue_bootstrap('00000000-0000-4000-8000-000000000001','02000000-0000-4000-8000-000000000001','Managed','20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,n+interval '1 hour',decode(repeat('54',32),'hex'),'bootSmoke001',decode(repeat('55',32),'hex'),1,decode(repeat('56',32),'hex'),n);
 SELECT * INTO br FROM tagekyc.capture_runtime_issue_bootstrap('00000000-0000-4000-8000-000000000001','02000000-0000-4000-8000-000000000001','Managed','20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,n+interval '1 hour',decode(repeat('54',32),'hex'),'bootSmoke001',decode(repeat('55',32),'hex'),1,decode(repeat('56',32),'hex'),n);
 IF b.result_code<>'Created' OR br.result_code<>'ExistingMatchSecretUnavailable' OR br.bootstrap_issuance_id<>b.bootstrap_issuance_id OR br.secret_available THEN RAISE EXCEPTION 'T1_R03_FIRST_REPLAY_SMOKE_FAILED'; END IF;
 SELECT * INTO p FROM tagekyc.capture_runtime_revoke_bootstrap('00000000-0000-4000-8000-000000000001','02000000-0000-4000-8000-000000000002',b.bootstrap_issuance_id,1,'OperatorRevocation',decode(repeat('57',32),'hex'),n);
 SELECT * INTO q FROM tagekyc.capture_runtime_revoke_bootstrap('00000000-0000-4000-8000-000000000001','02000000-0000-4000-8000-000000000002',b.bootstrap_issuance_id,1,'OperatorRevocation',decode(repeat('57',32),'hex'),n);
 IF p.result_code<>'Revoked' OR q.result_code<>'Revoked' OR q.revision<>p.revision THEN RAISE EXCEPTION 'T1_R04_FIRST_REPLAY_SMOKE_FAILED'; END IF;

 SELECT * INTO b FROM tagekyc.capture_runtime_issue_bootstrap('00000000-0000-4000-8000-000000000001','02000000-0000-4000-8000-000000000003','Managed','20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,n+interval '1 hour',decode(repeat('58',32),'hex'),'bootSmoke002',decode(repeat('59',32),'hex'),1,decode(repeat('60',32),'hex'),n);
 SELECT * INTO e FROM tagekyc.capture_runtime_redeem_bootstrap(b.bootstrap_issuance_id,'02000000-0000-4000-8000-000000000004',decode(repeat('61',32),'hex'),'02000000-0000-4000-8000-000000000005',decode(repeat('62',91),'hex'),decode(repeat('63',32),'hex'),n,decode(repeat('64',32),'hex'),decode(repeat('65',64),'hex'),decode(repeat('59',32),'hex'),n);
 SELECT * INTO er FROM tagekyc.capture_runtime_redeem_bootstrap(b.bootstrap_issuance_id,'02000000-0000-4000-8000-000000000004',decode(repeat('61',32),'hex'),'02000000-0000-4000-8000-000000000005',decode(repeat('62',91),'hex'),decode(repeat('63',32),'hex'),n,decode(repeat('64',32),'hex'),decode(repeat('65',64),'hex'),decode(repeat('59',32),'hex'),n);
 IF e.result_code<>'Created' OR er.result_code<>'Replay' OR er.capture_agent_id<>e.capture_agent_id OR er.device_installation_id<>e.device_installation_id OR er.credential_id<>e.credential_id THEN RAISE EXCEPTION 'T1_R05_FIRST_REPLAY_SMOKE_FAILED'; END IF;
END $smoke$;

INSERT INTO tagekyc.capture_runtime_registrations VALUES ('40000000-0000-4000-8000-000000000001','Managed','20000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_registrations VALUES ('40000000-0000-4000-8000-000000000002','Managed','20000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_installations VALUES ('50000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001',NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
INSERT INTO tagekyc.capture_runtime_credential_generations VALUES ('60000000-0000-4000-8000-000000000001',1,'50000000-0000-4000-8000-000000000001','70000000-0000-4000-8000-000000000001',decode(repeat('04',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('22',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"='60000000-0000-4000-8000-000000000001',"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"='50000000-0000-4000-8000-000000000001';
UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"='60000000-0000-4000-8000-000000000001';

INSERT INTO tagekyc.capture_runtime_installations("DeviceInstallationId","CaptureAgentId","CurrentCredentialId","CurrentCredentialGeneration","LifecycleState","Revision","EnrolledAtUtc","RevokedAtUtc","LifecycleReason") VALUES ('05000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000001','06000000-0000-4000-8000-000000000001',1,'Revoked',2,now()-interval '2 days',now()-interval '1 day','OperatorRevocation');
INSERT INTO tagekyc.capture_runtime_credential_generations("CredentialId","Generation","DeviceInstallationId","CandidateKeyId","PublicVerifierSpki","Algorithm","PublicKeyThumbprint","RolePolicyId","RolePolicyRevision","ValidFromUtc","ValidUntilUtc","State","Revision","RevokedAtUtc","TerminalReason") VALUES ('06000000-0000-4000-8000-000000000001',1,'05000000-0000-4000-8000-000000000001','07000000-0000-4000-8000-000000000001',decode(repeat('05',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('23',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '2 days',now()+interval '1 day','Revoked',2,now()-interval '1 day','OperatorRevocation');

INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId") VALUES ('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'80000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','Pending',now()+interval '1 day','r','c');
INSERT INTO tagekyc.capture_capabilities VALUES ('a0000000-0000-4000-8000-000000000001','80000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','mnopqrstuvwx',decode(repeat('33',32),'hex'),1,'ManagedCaptureRuntime','challenge',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);

DO $test$
BEGIN
 BEGIN
  INSERT INTO tagekyc.capture_execution_bindings VALUES ('b0000000-0000-4000-8000-000000000001','80000000-0000-4000-8000-000000000001','a0000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','40000000-0000-4000-8000-000000000002','50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,decode(repeat('22',32),'hex'),1,2,2,'20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,'challenge','c0000000-0000-4000-8000-000000000001',now(),now()+interval '1 hour');
  RAISE EXCEPTION 'MUTATION_CROSS_LINEAGE_WENT_GREEN';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;

 BEGIN
  INSERT INTO tagekyc.capture_runtime_management_operations VALUES ('00000000-0000-4000-8000-000000000001','RuntimeSuspend','d0000000-0000-4000-8000-000000000001',decode(repeat('41',32),'hex'),'Runtime','40000000-0000-4000-8000-000000000001','Applied',2,'40000000-0000-4000-8000-000000000001',now(),now());
  INSERT INTO tagekyc.capture_runtime_management_events VALUES (gen_random_uuid(),'00000000-0000-4000-8000-000000000001','RuntimeSuspend','d0000000-0000-4000-8000-000000000001','Applied','Runtime','40000000-0000-4000-8000-000000000002',1,2,'OperatorSuspension',now());
  SET CONSTRAINTS ALL IMMEDIATE;
  RAISE EXCEPTION 'MUTATION_AUDIT_TARGET_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_MANAGEMENT_EVENT_MISMATCH%' THEN RAISE; END IF; END;
END $test$;

DO $test$ DECLARE x record;y record;z record;rv record;rt record;n timestamptz:=now(); BEGIN
 SELECT * INTO x FROM tagekyc.capture_runtime_suspend('00000000-0000-4000-8000-000000000001','d0000000-0000-4000-8000-000000000002','40000000-0000-4000-8000-000000000001',1,'OperatorSuspension',decode(repeat('42',32),'hex'),now());
 SELECT * INTO y FROM tagekyc.capture_runtime_reactivate('00000000-0000-4000-8000-000000000001','d0000000-0000-4000-8000-000000000003','40000000-0000-4000-8000-000000000001',2,'OperatorReactivation',decode(repeat('43',32),'hex'),now());
 SELECT * INTO z FROM tagekyc.capture_runtime_suspend('00000000-0000-4000-8000-000000000001','d0000000-0000-4000-8000-000000000002','40000000-0000-4000-8000-000000000001',1,'OperatorSuspension',decode(repeat('42',32),'hex'),now());
 IF x.state<>'Suspended' OR x.revision<>2 OR z.state IS DISTINCT FROM x.state OR z.revision IS DISTINCT FROM x.revision OR z.transitioned_at_utc IS DISTINCT FROM x.transitioned_at_utc OR y.state<>'Active' THEN RAISE EXCEPTION 'MUTATION_LIFECYCLE_REPLAY_FAILED'; END IF;
 SELECT * INTO rv FROM tagekyc.capture_runtime_revoke('00000000-0000-4000-8000-000000000001','d0000000-0000-4000-8000-000000000004','40000000-0000-4000-8000-000000000001',3,'OperatorRevocation',decode(repeat('44',32),'hex'),n);
 SELECT * INTO rt FROM tagekyc.capture_runtime_retire('00000000-0000-4000-8000-000000000001','d0000000-0000-4000-8000-000000000005','40000000-0000-4000-8000-000000000001',4,'OperatorRetirement',decode(repeat('45',32),'hex'),n+interval '1 second');
 IF rv.state<>'Revoked' OR rt.state<>'Retired' OR (SELECT "LifecycleState" FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"='50000000-0000-4000-8000-000000000001')<>'Retired' OR (SELECT "LifecycleState" FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"='05000000-0000-4000-8000-000000000001')<>'Revoked' THEN RAISE EXCEPTION 'MUTATION_MULTI_HISTORY_LIFECYCLE_FAILED'; END IF;
END $test$;

SELECT * FROM tagekyc.capture_runtime_materialize_capability_expiry('80000000-0000-4000-8000-000000000001','a0000000-0000-4000-8000-000000000001',now(),'e0000000-0000-4000-8000-000000000001');
INSERT INTO tagekyc.capture_capabilities VALUES ('a0000000-0000-4000-8000-000000000002','80000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','yzABCDEFGHIJ',decode(repeat('34',32),'hex'),1,'ManagedCaptureRuntime','challenge2',now()-interval '10 minutes',now()-interval '1 minute','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
DO $test$ DECLARE x record; BEGIN SELECT * INTO x FROM tagekyc.capture_runtime_materialize_capability_expiry('80000000-0000-4000-8000-000000000001','a0000000-0000-4000-8000-000000000002',now(),'e0000000-0000-4000-8000-000000000001'); IF x.result_code<>'CONFLICT' THEN RAISE EXCEPTION 'MUTATION_EXPIRY_IDENTITY_WENT_GREEN'; END IF; END $test$;
ROLLBACK;
```

## PostgreSQL 16 execution result

Executed against the five exact bound artifacts above on `postgres:16-alpine`.
The complete bound Up catalogues compiled before this mutation suite ran.

| Mutation | Result | Executed evidence |
| --- | --- | --- |
| Cross-lineage binding | PASS | The attempted Agent-2/Agent-1-installation binding raised `foreign_key_violation`; the mutation did not go green. |
| Mismatched audit target | PASS | Deferred validation raised `TIP88C1C6BA_MANAGEMENT_EVENT_MISMATCH`; the mutation did not go green. |
| T1 first-call and replay smoke | PASS | R01 provision/replay, R02 root revoke/replay, R03 bootstrap issue/replay, R04 bootstrap revoke/replay and R05 redeem/replay returned their exact first/replay dispositions and durable identifiers. |
| Lifecycle replay after a later transition | PASS | Suspend returned `Suspended / 2`; reactivation later returned `Active / 3`; replay of the original Suspend operation returned its frozen `Suspended / 2` result and original `transitioned_at_utc`, rather than the mutable current registration state. |
| Multi-history lifecycle selection | PASS | A lower-UUID historical revoked installation was seeded beside the active installation. R06/R07 used the active lineage; R08/R09 terminalized that exact lineage and left the historical installation unchanged. |
| Expiry operation identity/fingerprint mismatch | PASS | First materialization returned `EXPIRED / Expired / 2`; reuse of the same deterministic operation id for a different capability returned `CONFLICT`; the mutation did not go green. |
| Down/reapply symmetry | PASS | T3, T2, T1 and DDL Down catalogues completed under `ON_ERROR_STOP=1`; immediate reapplication of DDL, T1, T2 and T3 completed without a surviving-relation conflict. Mechanical table census is 27 CREATE / 27 DROP. |

Overall result: **PASS**. All mutations and T1 first/replay smoke checks completed
under `ON_ERROR_STOP=1` in one PostgreSQL 16 execution and the fixture transaction
rolled back; the separate Down/reapply cycle also completed. T1 explicitly
casts persisted varchar state/prefix columns when returning declared `text` result
columns. No operation, DDL, T2, or T3 master/companion was modified by this suite run.
