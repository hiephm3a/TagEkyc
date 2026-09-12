# TIP-88C1-C6B-A1 — expiry-on-denied PostgreSQL 16 proof

**Status:** EXECUTABLE REVIEW EVIDENCE — NOT IMPLEMENTATION AUTHORITY

**OP:** `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`  
**DDL:** `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`  
**T1:** `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102`  
**T2:** `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115`  
**T3:** `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F`  
**T4:** `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C`  

Run after the current DDL and T1-T4 Up catalogues as
`tagekyc_raw_export_deployer` in an isolated PostgreSQL 16 database.

```sql
BEGIN;
SET LOCAL search_path=tagekyc,pg_catalog;
INSERT INTO tagekyc.platform_operator_credentials VALUES
('90000000-0000-4000-8000-000000000001'::uuid,'expiryprf001',decode(repeat('11',32),'hex'),1,'90000000-0000-4000-8000-000000000002',ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 day',now()+interval '1 day',NULL,NULL);
INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES
('90000000-0000-4000-8000-000000000003'::uuid,1,ARRAY['Configuration'],now()-interval '1 day','90000000-0000-4000-8000-000000000001'::uuid,now());
INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
('90000000-0000-4000-8000-000000000004'::uuid,1,'Managed',true,true,false,now()-interval '1 day',now()+interval '1 day','90000000-0000-4000-8000-000000000001'::uuid,now());
INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
('90000000-0000-4000-8000-000000000005'::uuid,1,now()-interval '1 day',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,'90000000-0000-4000-8000-000000000001'::uuid,now());

DO $proof$
DECLARE n timestamptz:=clock_timestamp(); b record; first_result record; replay_result record;
        live_bootstrap uuid; agent uuid; installation uuid; credential uuid;
BEGIN
 INSERT INTO tagekyc.capture_runtime_bootstrap_issuances
 VALUES('91000000-0000-4000-8000-000000000001'::uuid,'expiredboot1',decode(repeat('21',32),'hex'),1,'Managed',
 '90000000-0000-4000-8000-000000000004'::uuid,1,'90000000-0000-4000-8000-000000000003'::uuid,1,
 '90000000-0000-4000-8000-000000000005'::uuid,1,decode(repeat('22',32),'hex'),decode(repeat('23',32),'hex'),
 '91000000-0000-4000-8000-000000000002','90000000-0000-4000-8000-000000000001'::uuid,n-interval '2 hours',n-interval '1 hour','Active',1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_redeem_bootstrap(
 '91000000-0000-4000-8000-000000000001'::uuid,'91000000-0000-4000-8000-000000000003'::uuid,decode(repeat('24',32),'hex'),
 '91000000-0000-4000-8000-000000000004'::uuid,decode(repeat('25',91),'hex'),decode(repeat('26',32),'hex'),n,
 decode(repeat('27',32),'hex'),decode(repeat('28',64),'hex'),decode(repeat('21',32),'hex'),n);
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_redeem_bootstrap(
 '91000000-0000-4000-8000-000000000001'::uuid,'91000000-0000-4000-8000-000000000003'::uuid,decode(repeat('24',32),'hex'),
 '91000000-0000-4000-8000-000000000004'::uuid,decode(repeat('25',91),'hex'),decode(repeat('26',32),'hex'),n,
 decode(repeat('27',32),'hex'),decode(repeat('28',64),'hex'),decode(repeat('21',32),'hex'),n+interval '1 minute');
 IF first_result.result_code<>'TerminalizedExpiredAndDenied' OR replay_result.result_code<>'TerminalizedExpiredAndDenied'
 OR (SELECT "State" FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"='91000000-0000-4000-8000-000000000001'::uuid)<>'Expired'
 OR (SELECT count(*) FROM tagekyc.capture_runtime_bootstrap_redemption_operations WHERE "BootstrapIssuanceId"='91000000-0000-4000-8000-000000000001'::uuid)<>1
 OR (SELECT count(*) FROM tagekyc.capture_runtime_bootstrap_redemption_events WHERE "BootstrapIssuanceId"='91000000-0000-4000-8000-000000000001'::uuid AND "EventType"='Expired')<>1 THEN RAISE EXCEPTION 'BOOTSTRAP_EXPIRY_ON_DENIED_FAILED'; END IF;

 SELECT * INTO b FROM tagekyc.capture_runtime_issue_bootstrap('90000000-0000-4000-8000-000000000001'::uuid,'92000000-0000-4000-8000-000000000001'::uuid,'Managed','90000000-0000-4000-8000-000000000004'::uuid,1,'90000000-0000-4000-8000-000000000003'::uuid,1,'90000000-0000-4000-8000-000000000005'::uuid,1,n+interval '1 hour',decode(repeat('31',32),'hex'),'liveboot0001',decode(repeat('32',32),'hex'),1,decode(repeat('33',32),'hex'),n);
 live_bootstrap:=b.bootstrap_issuance_id;
 SELECT * INTO b FROM tagekyc.capture_runtime_redeem_bootstrap(live_bootstrap,'92000000-0000-4000-8000-000000000002'::uuid,decode(repeat('34',32),'hex'),'92000000-0000-4000-8000-000000000003'::uuid,decode(repeat('35',91),'hex'),decode(repeat('36',32),'hex'),n,decode(repeat('37',32),'hex'),decode(repeat('38',64),'hex'),decode(repeat('32',32),'hex'),n);
 agent:=b.capture_agent_id; installation:=b.device_installation_id; credential:=b.credential_id;
 INSERT INTO tagekyc.capture_runtime_rotation_authorizations VALUES
 ('93000000-0000-4000-8000-000000000001'::uuid,agent,installation,credential,1,'93000000-0000-4000-8000-000000000002',decode(repeat('41',32),'hex'),'90000000-0000-4000-8000-000000000001'::uuid,n-interval '9 minutes',n-interval '1 minute','Active',1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_complete_rotation('93000000-0000-4000-8000-000000000001'::uuid,credential,1,'93000000-0000-4000-8000-000000000003'::uuid,'93000000-0000-4000-8000-000000000004'::uuid,decode(repeat('42',91),'hex'),decode(repeat('43',32),'hex'),decode(repeat('44',64),'hex'),decode(repeat('45',32),'hex'),n);
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_complete_rotation('93000000-0000-4000-8000-000000000001'::uuid,credential,1,'93000000-0000-4000-8000-000000000003'::uuid,'93000000-0000-4000-8000-000000000004'::uuid,decode(repeat('42',91),'hex'),decode(repeat('43',32),'hex'),decode(repeat('44',64),'hex'),decode(repeat('45',32),'hex'),n+interval '1 minute');
 IF first_result.result_code<>'TerminalizedExpiredAndDenied' OR replay_result.result_code<>'TerminalizedExpiredAndDenied'
 OR (SELECT "State" FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"='93000000-0000-4000-8000-000000000001'::uuid)<>'Expired'
 OR (SELECT count(*) FROM tagekyc.capture_runtime_rotation_completion_operations WHERE "RotationAuthorizationId"='93000000-0000-4000-8000-000000000001'::uuid)<>1
 OR (SELECT count(*) FROM tagekyc.capture_runtime_rotation_completion_events WHERE "RotationAuthorizationId"='93000000-0000-4000-8000-000000000001'::uuid AND "EventType"='Expired')<>1 THEN RAISE EXCEPTION 'ROTATION_EXPIRY_ON_DENIED_FAILED'; END IF;
END $proof$;

DO $capability_proof$
DECLARE n timestamptz:=clock_timestamp(); client uuid:='94000000-0000-4000-8000-000000000001';
 agent uuid; installation uuid; credential uuid; first_result record; replay_result record;
BEGIN
 SELECT r."CaptureAgentId",i."DeviceInstallationId",g."CredentialId" INTO STRICT agent,installation,credential
 FROM tagekyc.capture_runtime_registrations r JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId"
 JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" AND g."CredentialId"=i."CurrentCredentialId" AND g."Generation"=i."CurrentCredentialGeneration"
 WHERE r."LifecycleState"='Active' AND i."LifecycleState"='Active' AND g."State"='Active' LIMIT 1;

 -- R20a: Issue materializes the prior expiry and alone may create one successor.
INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId","BindingNonceHash") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'94100000-0000-4000-8000-000000000001',client,'Pending',n+interval '1 hour','issue-r','issue-c',decode(repeat('51',32),'hex'));
 INSERT INTO tagekyc.capture_capabilities VALUES('94100000-0000-4000-8000-000000000002','94100000-0000-4000-8000-000000000001',client,'issueold0001',decode(repeat('52',32),'hex'),1,'ManagedCaptureRuntime',decode(repeat('51',32),'hex'),n-interval '10 minutes',n-interval '5 minutes','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_issue_or_replace_capability(client,'94100000-0000-4000-8000-000000000001','Issue',NULL,NULL,'94100000-0000-4000-8000-000000000003','94100000-0000-4000-8000-000000000004','issuenew0001',decode(repeat('53',32),'hex'),1,decode(repeat('54',32),'hex'),n);
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_issue_or_replace_capability(client,'94100000-0000-4000-8000-000000000001','Issue',NULL,NULL,'94100000-0000-4000-8000-000000000003','94100000-0000-4000-8000-000000000004','issuenew0001',decode(repeat('53',32),'hex'),1,decode(repeat('54',32),'hex'),n+interval '1 minute');
 IF first_result.result_code<>'CREATED' OR replay_result.result_code<>'EXISTING_MATCH_SECRET_UNAVAILABLE'
 OR (SELECT count(*) FROM tagekyc.capture_capabilities WHERE "VerificationSessionId"='94100000-0000-4000-8000-000000000001')<>2
 OR (SELECT count(*) FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"='94100000-0000-4000-8000-000000000002' AND "State"='Expired')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94100000-0000-4000-8000-000000000001' AND "OperationKind"='Expire')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94100000-0000-4000-8000-000000000001' AND "OperationKind"='Issue')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_events WHERE "VerificationSessionId"='94100000-0000-4000-8000-000000000001' AND "EventType"='Expired')<>1 THEN RAISE EXCEPTION 'R20A_ISSUE_AFTER_EXPIRY_FAILED'; END IF;

 -- R20b: Replace terminalizes the expired predecessor and creates no successor.
 INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId","BindingNonceHash") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'94200000-0000-4000-8000-000000000001',client,'Pending',n+interval '1 hour','replace-r','replace-c',decode(repeat('61',32),'hex'));
 INSERT INTO tagekyc.capture_capabilities VALUES('94200000-0000-4000-8000-000000000002','94200000-0000-4000-8000-000000000001',client,'replaceold01',decode(repeat('62',32),'hex'),1,'ManagedCaptureRuntime',decode(repeat('61',32),'hex'),n-interval '10 minutes',n-interval '5 minutes','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_issue_or_replace_capability(client,'94200000-0000-4000-8000-000000000001','Replace','94200000-0000-4000-8000-000000000002',1,'94200000-0000-4000-8000-000000000003','94200000-0000-4000-8000-000000000004','replacenew01',decode(repeat('63',32),'hex'),1,decode(repeat('64',32),'hex'),n);
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_issue_or_replace_capability(client,'94200000-0000-4000-8000-000000000001','Replace','94200000-0000-4000-8000-000000000002',1,'94200000-0000-4000-8000-000000000003','94200000-0000-4000-8000-000000000004','replacenew01',decode(repeat('63',32),'hex'),1,decode(repeat('64',32),'hex'),n+interval '1 minute');
 IF first_result.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED' OR replay_result.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED'
 OR EXISTS(SELECT 1 FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"='94200000-0000-4000-8000-000000000004')
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94200000-0000-4000-8000-000000000001' AND "OperationKind"='Replace' AND "ResultCode"='Expired')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_events WHERE "VerificationSessionId"='94200000-0000-4000-8000-000000000001' AND "EventType"='Expired')<>1 THEN RAISE EXCEPTION 'R20B_REPLACE_EXPIRY_FAILED'; END IF;

 -- R21: Bind terminalizes but never creates a binding.
 INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId","BindingNonceHash") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'94300000-0000-4000-8000-000000000001',client,'Pending',n+interval '1 hour','bind-r','bind-c',decode(repeat('71',32),'hex'));
 INSERT INTO tagekyc.capture_capabilities VALUES('94300000-0000-4000-8000-000000000002','94300000-0000-4000-8000-000000000001',client,'bindexpired1',decode(repeat('72',32),'hex'),1,'ManagedCaptureRuntime',decode(repeat('71',32),'hex'),n-interval '10 minutes',n-interval '5 minutes','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_bind_capability(agent,installation,credential,1,'94300000-0000-4000-8000-000000000002',true,'94300000-0000-4000-8000-000000000003',decode(repeat('73',32),'hex'),n);
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_bind_capability(agent,installation,credential,1,'94300000-0000-4000-8000-000000000002',true,'94300000-0000-4000-8000-000000000003',decode(repeat('73',32),'hex'),n+interval '1 minute');
 IF first_result.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED' OR replay_result.result_code<>'TERMINALIZED_EXPIRED_AND_DENIED'
 OR EXISTS(SELECT 1 FROM tagekyc.capture_execution_bindings WHERE "CaptureCapabilityId"='94300000-0000-4000-8000-000000000002')
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94300000-0000-4000-8000-000000000001' AND "OperationKind"='Bind' AND "ResultCode"='Expired')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_events WHERE "VerificationSessionId"='94300000-0000-4000-8000-000000000001' AND "EventType"='Expired')<>1 THEN RAISE EXCEPTION 'R21_BIND_EXPIRY_FAILED'; END IF;

 -- R26: expiry commits, cancellation commits, and the capability stays Expired.
 INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId","BindingNonceHash") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'94400000-0000-4000-8000-000000000001',client,'Pending',n+interval '1 hour','cancel-old-r','cancel-old-c',decode(repeat('81',32),'hex'));
 INSERT INTO tagekyc.capture_capabilities VALUES('94400000-0000-4000-8000-000000000002','94400000-0000-4000-8000-000000000001',client,'cancelexp001',decode(repeat('82',32),'hex'),1,'ManagedCaptureRuntime',decode(repeat('81',32),'hex'),n-interval '10 minutes',n-interval '5 minutes','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL);
 SELECT * INTO first_result FROM tagekyc.capture_runtime_cancel_session_with_capability(client,'94400000-0000-4000-8000-000000000001','ClientRequested','cancel-r','cancel-c',n,'clientproof','94400000-0000-4000-8000-000000000004');
 SELECT * INTO replay_result FROM tagekyc.capture_runtime_cancel_session_with_capability(client,'94400000-0000-4000-8000-000000000001','ClientRequested','cancel-r','cancel-c',n+interval '1 minute','clientproof','94400000-0000-4000-8000-000000000004');
 IF first_result.result_code<>'APPLIED' OR replay_result.result_code<>'AVAILABLE'
 OR (SELECT "State" FROM tagekyc.verification_sessions WHERE "Id"='94400000-0000-4000-8000-000000000001')<>'Cancelled'
 OR (SELECT count(*) FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"='94400000-0000-4000-8000-000000000002' AND "State"='Expired')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94400000-0000-4000-8000-000000000001' AND "OperationKind"='Expire')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_operations WHERE "VerificationSessionId"='94400000-0000-4000-8000-000000000001' AND "OperationKind"='Cancel')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_events WHERE "VerificationSessionId"='94400000-0000-4000-8000-000000000001' AND "EventType"='Expired')<>1
 OR (SELECT count(*) FROM tagekyc.capture_capability_events WHERE "VerificationSessionId"='94400000-0000-4000-8000-000000000001' AND "EventType"='Cancelled')<>1 THEN RAISE EXCEPTION 'R26_CANCEL_EXPIRY_FAILED'; END IF;
END $capability_proof$;
ROLLBACK;
```

## PostgreSQL 16 execution result

```text
PASS bootstrap redemption first expired touch returns TerminalizedExpiredAndDenied
PASS bootstrap redemption exact replay returns the frozen terminal result
PASS bootstrap target is Expired with exactly one Expired operation/event pair
PASS rotation completion first expired touch returns TerminalizedExpiredAndDenied
PASS rotation completion exact replay returns the frozen terminal result
PASS rotation authorization is Expired with exactly one Expired operation/event pair
PASS terminal ROLLBACK leaves zero proof residue
PASS R20a Issue materializes one expiry, creates one successor, exact replay creates no duplicate
PASS R20b Replace materializes one expiry, creates no successor, exact replay is frozen
PASS R21 Bind materializes one expiry, creates no binding, exact replay is frozen
PASS R26 Cancel commits session cancellation while capability remains Expired; replay creates no duplicate
```

Executed with `ON_ERROR_STOP=1` against a fresh PostgreSQL 16 database containing
the current DDL and complete T1-T4 Up catalogues. The proof emitted one successful
`DO` and terminal `ROLLBACK`.
