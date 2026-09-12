# TIP-88C1-C6B-A1 — Identity and audit mutation proof

**Status:** POSTGRESQL 16 PASS — EXECUTABLE REVIEW EVIDENCE — NOT IMPLEMENTATION AUTHORITY  
**Fixture owner:** mutation suite SHA-256 `756E2420ED9E8C9480D4F469CC971BDFB847506B8733B602BA84A81663330E5B`  
**DDL:** `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1`  
**T1:** `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102`  
**T2:** `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115`  
**T3:** `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F`  
**T4:** `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C`  

Insert this block immediately before the bound fixture suite's terminal
`ROLLBACK`. Every inner block is an autonomous subtransaction. Success is one
outer `DO` with no `*_WENT_GREEN` exception.

```sql
DO $proof$
DECLARE
 a1 constant uuid:='40000000-0000-4000-8000-000000000001'; a2 constant uuid:='40000000-0000-4000-8000-000000000002';
 i1 constant uuid:='50000000-0000-4000-8000-000000000001'; c1 constant uuid:='60000000-0000-4000-8000-000000000001';
 actor constant uuid:='00000000-0000-4000-8000-000000000001'; sess constant uuid:='80000000-0000-4000-8000-000000000001'; client constant uuid:='90000000-0000-4000-8000-000000000001';
 redeemed uuid; violated_constraint text;
BEGIN
 SET LOCAL search_path=tagekyc,pg_catalog;
 SET CONSTRAINTS ALL DEFERRED;
 SELECT "BootstrapIssuanceId" INTO redeemed FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "State"='Redeemed' LIMIT 1;

 BEGIN
  INSERT INTO tagekyc.capture_runtime_bootstrap_issuances("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion","RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision","ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint","IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision","RedeemedAtUtc","CaptureAgentId","DeviceInstallationId","CredentialId","CredentialGeneration") VALUES('aa000000-0000-4000-8000-000000000001','lineage00001',decode(repeat('71',32),'hex'),1,'Managed','20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,decode(repeat('72',32),'hex'),decode(repeat('73',32),'hex'),'aa000000-0000-4000-8000-000000000002',actor,now()-interval '2 minutes',now()+interval '1 hour','Redeemed',2,now()-interval '1 minute',a2,i1,c1,1);
  SET CONSTRAINTS tagekyc."FK_bootstrap_result_installation_agent" IMMEDIATE; RAISE EXCEPTION 'BOOTSTRAP_LINEAGE_WENT_GREEN';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(redeemed,'ab000000-0000-4000-8000-000000000001',decode(repeat('74',32),'hex'),'ab000000-0000-4000-8000-000000000002','Applied',a2,i1,c1,1,now(),now());
  SET CONSTRAINTS tagekyc."FK_redemption_installation_agent" IMMEDIATE; RAISE EXCEPTION 'REDEMPTION_LINEAGE_WENT_GREEN';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_rotation_authorizations("RotationAuthorizationId","CaptureAgentId","DeviceInstallationId","CredentialId","CurrentGeneration","AuthorizeOperationId","RequestFingerprint","AuthorizedByCredentialId","AuthorizedAtUtc","ExpiresAtUtc","State","Revision") VALUES('ac000000-0000-4000-8000-000000000001',a2,i1,c1,1,'ac000000-0000-4000-8000-000000000002',decode(repeat('75',32),'hex'),actor,now(),now()+interval '5 minutes','Active',1);
  SET CONSTRAINTS tagekyc."FK_rotation_installation_agent" IMMEDIATE; RAISE EXCEPTION 'ROTATION_AUTH_LINEAGE_WENT_GREEN';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;

 INSERT INTO tagekyc.capture_runtime_rotation_authorizations("RotationAuthorizationId","CaptureAgentId","DeviceInstallationId","CredentialId","CurrentGeneration","AuthorizeOperationId","RequestFingerprint","AuthorizedByCredentialId","AuthorizedAtUtc","ExpiresAtUtc","State","Revision") VALUES('ad000000-0000-4000-8000-000000000001',a1,i1,c1,1,'ad000000-0000-4000-8000-000000000002',decode(repeat('76',32),'hex'),actor,now(),now()+interval '5 minutes','Active',1);
 BEGIN
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES('ad000000-0000-4000-8000-000000000001','ad000000-0000-4000-8000-000000000003',decode(repeat('77',32),'hex'),'05000000-0000-4000-8000-000000000001','06000000-0000-4000-8000-000000000001',1,2,'ad000000-0000-4000-8000-000000000004','Applied',1,1,1,now(),now());
  SET CONSTRAINTS tagekyc."FK_rotation_completion_authorization_lineage" IMMEDIATE; RAISE EXCEPTION 'ROTATION_COMPLETION_LINEAGE_WENT_GREEN';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;

 -- Exact field mismatch and EventType-only mismatch for each event family.
 BEGIN
  INSERT INTO tagekyc.capture_runtime_management_operations VALUES(actor,'RuntimeSuspend','b0000000-0000-4000-8000-000000000001',decode(repeat('81',32),'hex'),'Runtime',a1,'Applied',2,a1,now(),now()); INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),actor,'RuntimeSuspend','b0000000-0000-4000-8000-000000000001','Applied','Runtime',a2,1,2,'OperatorSuspension',now()); SET CONSTRAINTS management_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'MANAGEMENT_FIELD_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_MANAGEMENT_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_management_operations VALUES(actor,'RuntimeSuspend','b0000000-0000-4000-8000-000000000002',decode(repeat('82',32),'hex'),'Runtime',a1,'Applied',2,a1,now(),now()); INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),actor,'RuntimeSuspend','b0000000-0000-4000-8000-000000000002','Rejected','Runtime',a1,1,2,'OperatorSuspension',now()); SET CONSTRAINTS management_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'MANAGEMENT_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_management_event_type' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.platform_operator_root_operations VALUES('b1000000-0000-4000-8000-000000000001','Revoke',decode(repeat('83',32),'hex'),'00000000-0000-4000-8000-000000000002',actor,'Applied',2,now(),now()); INSERT INTO tagekyc.platform_operator_root_events VALUES(gen_random_uuid(),'b1000000-0000-4000-8000-000000000001','Revoked','00000000-0000-4000-8000-000000000003',actor,1,2,now()); SET CONSTRAINTS root_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'ROOT_FIELD_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_ROOT_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.platform_operator_root_operations VALUES('b1000000-0000-4000-8000-000000000002','Revoke',decode(repeat('84',32),'hex'),'00000000-0000-4000-8000-000000000002',actor,'Applied',2,now(),now()); INSERT INTO tagekyc.platform_operator_root_events VALUES(gen_random_uuid(),'b1000000-0000-4000-8000-000000000002','Replayed','00000000-0000-4000-8000-000000000002',actor,1,2,now()); SET CONSTRAINTS root_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'ROOT_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_root_event_type' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(redeemed,'b2000000-0000-4000-8000-000000000001',decode(repeat('85',32),'hex'),'b2000000-0000-4000-8000-000000000011','Applied',a1,i1,c1,1,now(),now()); INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),redeemed,'b2000000-0000-4000-8000-000000000001','Redeemed',a2,i1,c1,1,now()); SET CONSTRAINTS redemption_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'REDEMPTION_FIELD_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_REDEMPTION_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(redeemed,'b2000000-0000-4000-8000-000000000002',decode(repeat('86',32),'hex'),'b2000000-0000-4000-8000-000000000012','Applied',a1,i1,c1,1,now(),now()); INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),redeemed,'b2000000-0000-4000-8000-000000000002','Replayed',a1,i1,c1,1,now()); SET CONSTRAINTS redemption_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'REDEMPTION_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_bootstrap_redemption_event_type' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES('ad000000-0000-4000-8000-000000000001','b3000000-0000-4000-8000-000000000001',decode(repeat('87',32),'hex'),i1,c1,1,2,'b3000000-0000-4000-8000-000000000011','Applied',1,3,2,now(),now()); INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(gen_random_uuid(),'ad000000-0000-4000-8000-000000000001','b3000000-0000-4000-8000-000000000001','Completed','06000000-0000-4000-8000-000000000001',1,2,now()); SET CONSTRAINTS rotation_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'ROTATION_FIELD_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_ROTATION_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES('ad000000-0000-4000-8000-000000000001','b3000000-0000-4000-8000-000000000002',decode(repeat('88',32),'hex'),i1,c1,1,2,'b3000000-0000-4000-8000-000000000012','Applied',1,3,2,now(),now()); INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(gen_random_uuid(),'ad000000-0000-4000-8000-000000000001','b3000000-0000-4000-8000-000000000002','Replayed',c1,1,2,now()); SET CONSTRAINTS rotation_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'ROTATION_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_rotation_completion_event_type' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_capability_operations VALUES(client,sess,'Expire','b4000000-0000-4000-8000-000000000001',decode(repeat('89',32),'hex'),NULL,NULL,NULL,NULL,'Applied','a0000000-0000-4000-8000-000000000001',NULL,2,now(),now()); INSERT INTO tagekyc.capture_capability_events VALUES(gen_random_uuid(),client,sess,'Expire','b4000000-0000-4000-8000-000000000001','a0000000-0000-4000-8000-000000000001','Expired',a1,NULL,1,2,now()); SET CONSTRAINTS capability_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'CAPABILITY_FIELD_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_CAPABILITY_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.capture_capability_operations VALUES(client,sess,'Expire','b4000000-0000-4000-8000-000000000002',decode(repeat('90',32),'hex'),NULL,NULL,NULL,NULL,'Applied','a0000000-0000-4000-8000-000000000001',NULL,2,now(),now()); INSERT INTO tagekyc.capture_capability_events VALUES(gen_random_uuid(),client,sess,'Expire','b4000000-0000-4000-8000-000000000002','a0000000-0000-4000-8000-000000000001','Replayed',NULL,NULL,1,2,now()); SET CONSTRAINTS capability_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'CAPABILITY_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_capability_event_type' THEN RAISE; END IF; END;

 BEGIN
INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'b5000000-0000-4000-8000-000000000010',client,'Pending',now()+interval '1 day','bound-r','bound-c'); INSERT INTO tagekyc.capture_capabilities VALUES('b5000000-0000-4000-8000-000000000001','b5000000-0000-4000-8000-000000000010',client,'boundtest001',decode(repeat('91',32),'hex'),1,'ManagedCaptureRuntime','bound-challenge',now(),now()+interval '1 hour','Bound',1,NULL,NULL,now(),NULL,NULL,NULL); SET CONSTRAINTS capability_graph_guard IMMEDIATE; RAISE EXCEPTION 'BOUND_WITHOUT_BINDING_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_CAPABILITY_GRAPH_INCONSISTENT%' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO tagekyc.verification_sessions("SubjectRef","Profile","Purpose","RequiredChecksJson","Result","AssuranceLevel","PolicySnapshotId","RetentionClass","DeletionEligibility","LegalHoldStatus","PurgeBlockReason","AccessAuditRequired","CreatedAt","Id","ClientApplicationId","State","ExpiresAt","RequestId","CorrelationId") VALUES('synthetic-a1-proof','Standard','SyntheticProof','[]'::jsonb,'Pending','None','synthetic-policy','Standard','NotEligible','None','None',false,now(),'b6000000-0000-4000-8000-000000000001',client,'Pending',now()+interval '1 day','binding-r','binding-c'); INSERT INTO tagekyc.capture_capabilities VALUES('b6000000-0000-4000-8000-000000000002','b6000000-0000-4000-8000-000000000001',client,'unboundtst01',decode(repeat('92',32),'hex'),1,'ManagedCaptureRuntime','unbound-challenge',now(),now()+interval '1 hour','ActiveUnbound',1,NULL,NULL,NULL,NULL,NULL,NULL); INSERT INTO tagekyc.capture_execution_bindings VALUES('b6000000-0000-4000-8000-000000000003','b6000000-0000-4000-8000-000000000001','b6000000-0000-4000-8000-000000000002',client,a1,i1,c1,1,decode(repeat('22',32),'hex'),1,2,2,'20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,'30000000-0000-4000-8000-000000000001',1,'unbound-challenge','b6000000-0000-4000-8000-000000000004',now(),now()+interval '1 hour'); SET CONSTRAINTS binding_capability_graph_guard IMMEDIATE; RAISE EXCEPTION 'BINDING_TO_NONBOUND_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_CAPABILITY_GRAPH_INCONSISTENT%' THEN RAISE; END IF; END;
END $proof$;

DO $allowed_pairs$
DECLARE redeemed uuid; rotation_id uuid; cap tagekyc.capture_capabilities%ROWTYPE; violated_constraint text;
BEGIN
 SET LOCAL search_path=tagekyc,pg_catalog; SET CONSTRAINTS ALL DEFERRED;
 SELECT "BootstrapIssuanceId" INTO STRICT redeemed FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "State"='Redeemed' LIMIT 1;
 SELECT "RotationAuthorizationId" INTO STRICT rotation_id FROM tagekyc.capture_runtime_rotation_authorizations LIMIT 1;
 SELECT * INTO STRICT cap FROM tagekyc.capture_capabilities LIMIT 1;

 SET CONSTRAINTS redemption_operation_requires_event DEFERRED; INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(redeemed,'c1000000-0000-4000-8000-000000000001',decode(repeat('a1',32),'hex'),'c1000000-0000-4000-8000-000000000002','Expired',NULL,NULL,NULL,NULL,now(),now());
 INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),redeemed,'c1000000-0000-4000-8000-000000000001','Expired',NULL,NULL,NULL,NULL,now());
 SET CONSTRAINTS redemption_operation_requires_event IMMEDIATE;
 BEGIN
  SET CONSTRAINTS redemption_operation_requires_event DEFERRED;
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(redeemed,'c1000000-0000-4000-8000-000000000003',decode(repeat('a2',32),'hex'),'c1000000-0000-4000-8000-000000000004','Expired',NULL,NULL,NULL,NULL,now(),now());
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),redeemed,'c1000000-0000-4000-8000-000000000003','Rejected',NULL,NULL,NULL,NULL,now()); SET CONSTRAINTS redemption_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'REDEMPTION_EXPIRED_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_bootstrap_redemption_event_type' THEN RAISE; END IF; END;

 SET CONSTRAINTS rotation_operation_requires_event DEFERRED; INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(rotation_id,'c2000000-0000-4000-8000-000000000001',decode(repeat('a3',32),'hex'),'50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,NULL,'c2000000-0000-4000-8000-000000000002','Expired',NULL,NULL,NULL,now(),now());
 INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(gen_random_uuid(),rotation_id,'c2000000-0000-4000-8000-000000000001','Expired','60000000-0000-4000-8000-000000000001',1,NULL,now()); SET CONSTRAINTS rotation_operation_requires_event IMMEDIATE;
 BEGIN
  SET CONSTRAINTS rotation_operation_requires_event DEFERRED;
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(rotation_id,'c2000000-0000-4000-8000-000000000003',decode(repeat('a4',32),'hex'),'50000000-0000-4000-8000-000000000001','60000000-0000-4000-8000-000000000001',1,NULL,'c2000000-0000-4000-8000-000000000004','Expired',NULL,NULL,NULL,now(),now());
  INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(gen_random_uuid(),rotation_id,'c2000000-0000-4000-8000-000000000003','Rejected','60000000-0000-4000-8000-000000000001',1,NULL,now()); SET CONSTRAINTS rotation_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'ROTATION_EXPIRED_TYPE_WENT_GREEN';
 EXCEPTION WHEN check_violation THEN GET STACKED DIAGNOSTICS violated_constraint = CONSTRAINT_NAME; IF violated_constraint <> 'CK_rotation_completion_event_type' THEN RAISE; END IF; END;

 SET CONSTRAINTS capability_operation_requires_event DEFERRED; INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(cap."ClientApplicationId",cap."VerificationSessionId",'Replace','c3000000-0000-4000-8000-000000000001',decode(repeat('a5',32),'hex'),'Expired',cap."CaptureCapabilityId",cap."Revision",now(),now());
 INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(gen_random_uuid(),cap."ClientApplicationId",cap."VerificationSessionId",'Replace','c3000000-0000-4000-8000-000000000001',cap."CaptureCapabilityId",'Expired',cap."Revision",cap."Revision",now()); SET CONSTRAINTS capability_operation_requires_event IMMEDIATE;
 BEGIN
  SET CONSTRAINTS capability_operation_requires_event DEFERRED;
  INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(cap."ClientApplicationId",cap."VerificationSessionId",'Bind','c3000000-0000-4000-8000-000000000002',decode(repeat('a6',32),'hex'),'Expired',cap."CaptureCapabilityId",cap."Revision",now(),now());
  INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(gen_random_uuid(),cap."ClientApplicationId",cap."VerificationSessionId",'Bind','c3000000-0000-4000-8000-000000000002',cap."CaptureCapabilityId",'Bound',cap."Revision",cap."Revision",now()); SET CONSTRAINTS capability_operation_requires_event IMMEDIATE; RAISE EXCEPTION 'CAPABILITY_EXPIRED_TYPE_WENT_GREEN';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM NOT LIKE '%TIP88C1C6BA_CAPABILITY_EVENT_MISMATCH%' THEN RAISE; END IF; END;
 SET CONSTRAINTS capability_operation_requires_event DEFERRED; INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(cap."ClientApplicationId",cap."VerificationSessionId",'Expire','c3000000-0000-4000-8000-000000000003',decode(repeat('a7',32),'hex'),'Applied',cap."CaptureCapabilityId",cap."Revision",now(),now());
 INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(gen_random_uuid(),cap."ClientApplicationId",cap."VerificationSessionId",'Expire','c3000000-0000-4000-8000-000000000003',cap."CaptureCapabilityId",'Expired',cap."Revision",cap."Revision",now()); SET CONSTRAINTS capability_operation_requires_event IMMEDIATE;
END $allowed_pairs$;
```

Covered mutations: four composite lineage FKs; field and EventType-only mismatch
for management, root, redemption, rotation-completion and capability events; and
both directions of the Bound/binding invariant. The outer fixture rollback leaves
zero residue.

## PostgreSQL 16 execution result

```text
PASS  bootstrap issuance composite lineage FK
PASS  bootstrap redemption composite lineage FK
PASS  rotation authorization composite lineage FK
PASS  rotation completion composite lineage FK
PASS  management event exact-field mismatch
PASS  management event EventType-only mismatch
PASS  platform-root event exact-field mismatch
PASS  platform-root event EventType-only mismatch
PASS  redemption event exact-field mismatch
PASS  redemption event EventType-only mismatch
PASS  rotation-completion event exact-field mismatch
PASS  rotation-completion event EventType-only mismatch
PASS  capability event exact-field mismatch
PASS  capability event EventType-only mismatch
PASS  Bound capability without binding
PASS  binding referencing non-Bound capability
PASS  redemption Expired operation -> Expired event
PASS  redemption Expired operation -> Rejected event is RED
PASS  rotation-completion Expired operation -> Expired event
PASS  rotation-completion Expired operation -> Rejected event is RED
PASS  capability Replace Expired operation -> Expired event
PASS  capability Bind Expired operation -> Bound event is RED
PASS  standalone capability Expire Applied operation -> Expired event
PASS  outer transaction ROLLBACK; zero fixture/proof residue
```

The executable result was two successful proof `DO` blocks followed by terminal
`ROLLBACK`. Any mutation going GREEN raises its named `*_WENT_GREEN` exception;
any unexpected database error is re-raised, so `ON_ERROR_STOP=1` fails the run.
