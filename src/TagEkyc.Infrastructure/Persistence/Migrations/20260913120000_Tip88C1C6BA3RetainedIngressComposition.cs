using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class Tip88C1C6BA3RetainedIngressComposition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Execute(migrationBuilder, CaptureMigrationLocks);
        Execute(migrationBuilder, CapturePredecessorGuard);
        Execute(migrationBuilder, ConsentSchema);
        Execute(migrationBuilder, ConsentGuards);
        Execute(migrationBuilder, ConsentOperations);
        Execute(migrationBuilder, RetentionOperations);
        Execute(migrationBuilder, CaptureLineageSchema);
        Execute(migrationBuilder, CapabilityIssueOperation);
        Execute(migrationBuilder, CapabilityBindOperation);
        Execute(migrationBuilder, B2WithdrawalOperation);
        Execute(migrationBuilder, SnapshotSchema);
        Execute(migrationBuilder, RetainedEncryptionReader);
        Execute(migrationBuilder, SnapshotOperations);
        Execute(migrationBuilder, ExportCheckpointOperations);
        Execute(migrationBuilder, TerminalIntentSchema);
        Execute(migrationBuilder, NoProviderStartOperations);
        Execute(migrationBuilder, RetainedStageOperations);
        Execute(migrationBuilder, RetainedCompletionOperation);
        Execute(migrationBuilder, RetainedBeginOperation);
        Execute(migrationBuilder, RawIngressCallerAuthorization);
        Execute(migrationBuilder, BoundRawIngressReader);
        Execute(migrationBuilder, SameOwnerReentryOperations);
        Execute(migrationBuilder, TerminalIntentOperations);
        Execute(migrationBuilder, ContinuationOperations);
        Execute(migrationBuilder, AcceptanceCompletionOperations);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        Execute(migrationBuilder, CaptureMigrationLocks);
        Execute(migrationBuilder, """
LOCK TABLE tagekyc.raw_source_consent_references,
 tagekyc.raw_source_consent_reference_events, tagekyc.raw_source_consent_bindings,
 tagekyc.raw_source_retention_permits, tagekyc.raw_source_retention_permit_classes
 IN ACCESS EXCLUSIVE MODE;
""");
        Execute(migrationBuilder, CaptureCurrentGuard);
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid); DROP FUNCTION tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer); REVOKE SELECT ON tagekyc.evidence_results FROM tagekyc_raw_export_deployer;");
        Execute(migrationBuilder, """
DO $guard$
BEGIN
    IF EXISTS (SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
        WHERE "R2TerminalIntentCode" IS NOT NULL OR "R2TerminalIntentDisposition" IS NOT NULL
         OR "R2TerminalIntentAtUtc" IS NOT NULL) THEN
        RAISE EXCEPTION 'A3_R2_TERMINAL_INTENT_DOWN_POPULATED';
    END IF;
    IF EXISTS (SELECT 1 FROM tagekyc.capture_capabilities WHERE "AuthorityMode"='SourceRetention')
        OR EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings WHERE "AuthorityMode"='SourceRetention')
        OR EXISTS (SELECT 1 FROM tagekyc.raw_source_consent_references)
        OR EXISTS (SELECT 1 FROM tagekyc.raw_source_consent_reference_events)
        OR EXISTS (SELECT 1 FROM tagekyc.raw_source_consent_bindings)
        OR EXISTS (SELECT 1 FROM tagekyc.raw_source_retention_permits)
        OR EXISTS (SELECT 1 FROM tagekyc.raw_source_retention_permit_classes)
        OR EXISTS (SELECT 1 FROM tagekyc.raw_export_authority_snapshots WHERE "AuthorityKind"='SourceRetention') THEN
        RAISE EXCEPTION 'A3_RETENTION_DOWN_POPULATED';
    END IF;
END $guard$;
""");
        Execute(migrationBuilder, SameOwnerReentryOperationsRestore);
        Execute(migrationBuilder, ExportCheckpointOperationsRestore);
        Execute(migrationBuilder, SnapshotOperationsRestore);
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz);");
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.capture_runtime_raw_ingress_caller_owns_session(uuid,uuid,uuid,bigint,uuid,bigint,uuid,bigint,text,timestamptz);");
        Execute(migrationBuilder, RetainedStageOperationsRestore);
        Execute(migrationBuilder, RetainedCompletionRestore);
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint);");
        Execute(migrationBuilder, NoProviderStartOperationsRestore);
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint); DROP FUNCTION tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text);");
        Execute(migrationBuilder, "DROP FUNCTION tagekyc.raw_export_list_retained_source_continuations(uuid,integer); DROP FUNCTION tagekyc.raw_export_read_retained_source_continuation(uuid);");
        Execute(migrationBuilder, TerminalIntentSchemaRestore);
        Execute(migrationBuilder, RetainedEncryptionReaderRestore);
        Execute(migrationBuilder, SnapshotSchemaRestore);
        Execute(migrationBuilder, CaptureLineageRestore);
        Execute(migrationBuilder, B2WithdrawalRestore);
        Execute(migrationBuilder, """
DROP FUNCTION tagekyc.raw_source_resolve_retention_authority(uuid,bigint,uuid,uuid,uuid,text,timestamptz);
DROP FUNCTION tagekyc.raw_source_issue_retention_authority(uuid,uuid,uuid,uuid,uuid,integer,text[],text,text,text,integer,text,text,text,text,integer,uuid,bytea);
DROP FUNCTION tagekyc.raw_source_withdraw_consent_reference(uuid,uuid,uuid,bigint,text,text,uuid,bytea);
DROP FUNCTION tagekyc.raw_source_record_consent_reference(uuid,uuid,uuid,text,text,bigint,text,text,timestamptz,timestamptz,uuid,bytea);
DROP TABLE tagekyc.raw_source_retention_permit_classes;
DROP TABLE tagekyc.raw_source_retention_permits;
DROP TABLE tagekyc.raw_source_consent_bindings;
ALTER TABLE tagekyc.raw_source_consent_references DROP CONSTRAINT "FK_a3_consent_reference_current";
DROP TABLE tagekyc.raw_source_consent_reference_events;
DROP TABLE tagekyc.raw_source_consent_references;
DROP FUNCTION tagekyc.raw_source_require_authority_graph();
DROP FUNCTION tagekyc.raw_source_enforce_authority_write();
""");
    }

    private static void Execute(MigrationBuilder migrationBuilder, string sql) =>
        migrationBuilder.Sql(sql.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal));

    private const string AcceptanceCompletionOperations = """
GRANT SELECT ON tagekyc.evidence_results TO tagekyc_raw_export_deployer;

CREATE FUNCTION tagekyc.raw_export_accept_runtime_evidence(
 p_binding_id uuid,p_session_id uuid,p_evidence_result_id uuid,
 p_acceptance_policy_id text,p_acceptance_policy_version integer)
RETURNS TABLE("CaptureArtifactId" uuid,"CaptureAcceptanceId" uuid,
 "CaptureRevision" integer,"RawClass" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $a3_accept$
DECLARE
 b tagekyc.capture_execution_bindings%ROWTYPE;
 e tagekyc.evidence_results%ROWTYPE;
 artifact_id uuid; raw_class text; candidate_count integer; conflicting_count integer;
 prior tagekyc.raw_export_capture_acceptance_events%ROWTYPE;
 selected_policy_id text; selected_policy_version integer; previous_actor text; accepted record;
BEGIN
 IF p_binding_id IS NULL OR p_binding_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_session_id IS NULL OR p_session_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_evidence_result_id IS NULL OR p_evidence_result_id='00000000-0000-0000-0000-000000000000'::uuid
  OR (p_acceptance_policy_id IS NULL)<>(p_acceptance_policy_version IS NULL)
 THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_session_id::text,70));
 PERFORM 1 FROM tagekyc.verification_sessions s WHERE s."Id"=p_session_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 SELECT * INTO b FROM tagekyc.capture_execution_bindings x
  WHERE x."CaptureExecutionBindingId"=p_binding_id AND x."VerificationSessionId"=p_session_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 IF b."AuthorityMode"<>'SourceRetention' THEN RETURN; END IF;
 IF b."PrincipalId" IS NULL OR b."PrincipalId"='00000000-0000-0000-0000-000000000000'::uuid
  OR b."ClientApplicationId"='00000000-0000-0000-0000-000000000000'::uuid
 THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 SELECT * INTO e FROM tagekyc.evidence_results x
  WHERE x."Id"=p_evidence_result_id AND x."VerificationSessionId"=p_session_id;
 IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 IF e."Result"<>'Passed' OR e."ResultType" NOT IN ('NfcValidation','FaceMatch') THEN RETURN; END IF;
 raw_class:=CASE e."ResultType" WHEN 'NfcValidation' THEN 'ChipDg2Portrait' ELSE 'LiveSelfieImage' END;
 SELECT pg_catalog.count(*),(pg_catalog.array_agg(a."Id"))[1] INTO candidate_count,artifact_id
 FROM tagekyc.capture_artifacts a
 WHERE a."VerificationSessionId"=p_session_id
  AND a."ArtifactType"=CASE e."ResultType" WHEN 'NfcValidation' THEN 'NfcReadArtifact' ELSE 'SelfieImage' END
  AND e."InputCaptureArtifactIdsJson" ? pg_catalog.replace(a."Id"::text,'-','');
 IF candidate_count<>1 THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(
  'tip88c1a:capture-acceptance:'||p_session_id::text||':'||raw_class,0));
 SELECT pg_catalog.count(*) INTO conflicting_count FROM tagekyc.raw_export_capture_acceptance_events a
 WHERE a."VerificationSessionId"=p_session_id AND a."RawClass"=raw_class
  AND (a."CaptureArtifactId"=artifact_id OR a."AcceptedEvidenceRef"=p_evidence_result_id::text);
 IF conflicting_count>1 THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT'; END IF;
 SELECT * INTO prior FROM tagekyc.raw_export_capture_acceptance_events a
 WHERE a."VerificationSessionId"=p_session_id AND a."RawClass"=raw_class
  AND (a."CaptureArtifactId"=artifact_id OR a."AcceptedEvidenceRef"=p_evidence_result_id::text);
 IF FOUND THEN
  IF prior."CaptureArtifactId"<>artifact_id OR prior."AcceptedEvidenceRef"<>p_evidence_result_id::text
   OR prior."ClientApplicationId"<>b."ClientApplicationId" OR prior."SessionChallengeHash"<>b."Challenge"
  THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT'; END IF;
  selected_policy_id:=prior."AcceptancePolicyId"; selected_policy_version:=prior."AcceptancePolicyVersion";
 ELSE
  IF p_acceptance_policy_id IS NULL OR p_acceptance_policy_id !~ '^[A-Za-z0-9._-]{1,128}$'
   OR p_acceptance_policy_version IS NULL OR p_acceptance_policy_version<1
  THEN RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_POLICY_NOT_READY'; END IF;
  selected_policy_id:=p_acceptance_policy_id; selected_policy_version:=p_acceptance_policy_version;
 END IF;
 previous_actor:=pg_catalog.current_setting('tagekyc.actor_principal_id',true);
 PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',b."PrincipalId"::text,true);
 BEGIN
  SELECT * INTO STRICT accepted FROM tagekyc.raw_export_accept_capture_for_ingress(
   p_session_id,b."ClientApplicationId",artifact_id,p_evidence_result_id,b."Challenge",
   selected_policy_id,selected_policy_version);
 EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',COALESCE(previous_actor,''),true);
  RAISE;
 END;
 PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',COALESCE(previous_actor,''),true);
 RETURN QUERY SELECT artifact_id,accepted."CaptureAcceptanceId",accepted."CaptureRevision",accepted."RawClass";
END $a3_accept$;

CREATE FUNCTION tagekyc.raw_export_select_runtime_sources_on_completion(
 p_session_id uuid,p_client_id uuid,p_completion_principal_id uuid)
RETURNS TABLE("Required" boolean,"RawClass" text,
 "CaptureAcceptanceId" uuid,"CaptureRevision" integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $a3_complete$
DECLARE b tagekyc.capture_execution_bindings%ROWTYPE; previous_actor text; selected record;
 rows_seen integer:=0;
BEGIN
 IF p_session_id IS NULL OR p_client_id IS NULL OR p_session_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_client_id='00000000-0000-0000-0000-000000000000'::uuid
 THEN RAISE EXCEPTION 'RAW_EXPORT_COMPLETION_ACCESS_DENIED'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_session_id::text,70));
 PERFORM 1 FROM tagekyc.verification_sessions s
  WHERE s."Id"=p_session_id AND s."ClientApplicationId"=p_client_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_COMPLETION_ACCESS_DENIED'; END IF;
 SELECT * INTO b FROM tagekyc.capture_execution_bindings x
  WHERE x."VerificationSessionId"=p_session_id;
 IF NOT FOUND OR b."AuthorityMode" IN ('HistoricalNonRetained','NonRetained') THEN
  RETURN QUERY SELECT false,NULL::text,NULL::uuid,NULL::integer; RETURN;
 END IF;
 IF b."AuthorityMode"<>'SourceRetention' OR b."ClientApplicationId"<>p_client_id
  OR p_completion_principal_id IS NULL
  OR p_completion_principal_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_completion_principal_id<>b."PrincipalId"
 THEN RAISE EXCEPTION 'RAW_EXPORT_COMPLETION_ACCESS_DENIED'; END IF;
 previous_actor:=pg_catalog.current_setting('tagekyc.actor_principal_id',true);
 PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',p_completion_principal_id::text,true);
 BEGIN
  FOR selected IN SELECT * FROM tagekyc.raw_export_select_capture_acceptances_on_session_completion(
   p_session_id,p_client_id)
  LOOP
   rows_seen:=rows_seen+1;
   "Required":=true;"RawClass":=selected."RawClass";
   "CaptureAcceptanceId":=selected."CaptureAcceptanceId";"CaptureRevision":=selected."CaptureRevision";
   RETURN NEXT;
  END LOOP;
 EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',COALESCE(previous_actor,''),true);
  RAISE;
 END;
 PERFORM pg_catalog.set_config('tagekyc.actor_principal_id',COALESCE(previous_actor,''),true);
 IF rows_seen<>2 THEN RAISE EXCEPTION 'RAW_EXPORT_SESSION_CAPTURE_SELECTION_MISSING'; END IF;
END $a3_complete$;

ALTER FUNCTION tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer),
 tagekyc.raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid) FROM PUBLIC,
 tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator,
 tagekyc_raw_export_claim_broker;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer),
 tagekyc.raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid) TO tagekyc_runtime;
""";

    // C1/C3 continue to require the existing post-Completed export decision,
    // permit and B2 subject-consent predicates.  A retained snapshot is only
    // an independently-current source-eligibility proof; it is never accepted
    // as the export authority itself.  Patch the landed functions in place so
    // the historical migrations remain byte-identical.
    private const string ExportCheckpointOperations = """
DO $a3_c1_c3$
DECLARE
  fn text;
  old_ text;
  new_ text;
BEGIN
  fn:=pg_catalog.pg_get_functiondef(
    'tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  old_:='class_row record; source_row record; authority record; consent record;';
  new_:='class_row record; source_row record; authority record; consent record; authority_kind text; prefix_session uuid; prefix_client uuid; prefix_reference text; prefix_key bigint;';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_FREEZE_DECLARATION_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(job."ClientApplicationId",job."VerificationSessionId",source_row."CaptureAcceptanceId",class_row."RawClass",now_);';
  new_:=old_||pg_catalog.chr(10)||
    '                    SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=source_row."AuthoritySnapshotId";';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_FREEZE_AUTHORITY_READ_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='IF authority."AuthoritySnapshotId" IS NULL OR consent."State" IS DISTINCT FROM ''Effective''';
  new_:='IF authority."AuthoritySnapshotId" IS NULL'||pg_catalog.chr(10)||
    '                       OR authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||
    '                       OR (authority_kind=''SourceRetention'' AND (authority."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(source_row."AuthoritySnapshotId",now_)))'||pg_catalog.chr(10)||
    '                       OR consent."State" IS DISTINCT FROM ''Effective''';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_FREEZE_AUTHORITY_GUARD_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='SELECT * INTO head FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;';
  new_:='SELECT "VerificationSessionId","ClientApplicationId" INTO prefix_session,prefix_client FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id;'||pg_catalog.chr(10)||
    '  IF NOT FOUND THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,0; RETURN; END IF;'||pg_catalog.chr(10)||
    '  PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE;'||pg_catalog.chr(10)||
    '  IF NOT FOUND OR NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id AND "VerificationSessionId"=prefix_session AND "ClientApplicationId"=prefix_client) THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,0; RETURN; END IF;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||prefix_client::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_classes class JOIN tagekyc.raw_export_session_capture_selections selection ON selection."VerificationSessionId"=prefix_session AND selection."RawClass"=class."RawClass" JOIN tagekyc.raw_export_capture_acceptance_events acceptance ON acceptance."CaptureAcceptanceId"=selection."CaptureAcceptanceId" JOIN tagekyc.raw_export_source_ingress_claims claim ON claim."VerificationSessionId"=prefix_session AND claim."CaptureAcceptanceId"=selection."CaptureAcceptanceId" AND claim."CaptureArtifactId"=acceptance."CaptureArtifactId" AND claim."CaptureRevision"=acceptance."CaptureRevision" AND claim."RawClass"=class."RawClass" JOIN tagekyc.raw_export_source_reservations reservation ON reservation."IngressClaimId"=claim."IngressClaimId" JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=reservation."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE class."JobId"=p_job_id AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  '||old_;
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_FREEZE_PREFIX_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  EXECUTE fn;

  fn:=pg_catalog.pg_get_functiondef(
    'tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  old_:='j tagekyc.raw_export_job_identities%ROWTYPE; b record; authority record; consent record; now_ timestamptz;';
  new_:='j tagekyc.raw_export_job_identities%ROWTYPE; b record; authority record; consent record; authority_kind text; prefix_session uuid; prefix_reference text; prefix_key bigint; now_ timestamptz;';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_SEAL_DECLARATION_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(b."ClientApplicationId",b."VerificationSessionId",b."CaptureAcceptanceId",b."RawClass",now_);';
  new_:=old_||pg_catalog.chr(10)||
    '                    SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=b."AuthoritySnapshotId";';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_SEAL_AUTHORITY_READ_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='IF consent."State" IS DISTINCT FROM ''Effective''';
  new_:='IF authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||
    '                       OR (authority_kind=''SourceRetention'' AND (authority."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(b."AuthoritySnapshotId",now_)))'||pg_catalog.chr(10)||
    '                       OR consent."State" IS DISTINCT FROM ''Effective''';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_SEAL_AUTHORITY_GUARD_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;';
  new_:='FOR prefix_session IN SELECT DISTINCT "VerificationSessionId" FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id ORDER BY "VerificationSessionId" LOOP PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE; IF NOT FOUND THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF; END LOOP;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||snapshot."ClientApplicationId"::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_source_bindings source JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=source."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE source."JobId"=p_job_id AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  '||old_;
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C1_SEAL_PREFIX_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  EXECUTE fn;

  fn:=pg_catalog.pg_get_functiondef(
    'tagekyc.raw_export_c3_current_authority_eligible(uuid,uuid,uuid,timestamptz)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  old_:='authority_row record;';
  new_:='authority_row record; authority_kind text;';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C3_DECLARATION_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='p_evaluated_at_utc);'||pg_catalog.chr(10)||'        IF NOT FOUND';
  new_:='p_evaluated_at_utc);'||pg_catalog.chr(10)||
    '        IF NOT FOUND THEN RETURN FALSE; END IF;'||pg_catalog.chr(10)||
    '        SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=source_row."AuthoritySnapshotId";'||pg_catalog.chr(10)||
    '        IF NOT FOUND';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C3_AUTHORITY_READ_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='OR authority_row."ApprovedPurpose" <>'||pg_catalog.chr(10)||
    '                identity_row."PurposeCode"';
  new_:='OR authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||
    '                           OR (authority_kind=''LegacyExport'' AND authority_row."ApprovedPurpose" <> identity_row."PurposeCode")'||pg_catalog.chr(10)||
    '                           OR (authority_kind=''SourceRetention'' AND (authority_row."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(source_row."AuthoritySnapshotId",p_evaluated_at_utc)))';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C3_AUTHORITY_GUARD_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  EXECUTE fn;

  fn:=pg_catalog.pg_get_functiondef(
    'tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  old_:='actor_api uuid;actor_principal uuid;actor_correlation bytea;';
  new_:='actor_api uuid;actor_principal uuid;actor_correlation bytea;prefix_session uuid;prefix_reference text;prefix_key bigint;prefix_job uuid;';
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C3_OUTER_DECLARATION_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  old_:='SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x WHERE x."RecipientClientApplicationId"=p_recipient';
  new_:='SELECT identity."JobId" INTO prefix_job FROM tagekyc.raw_export_recipient_package_deliveries delivery JOIN tagekyc.raw_export_recipient_package_preparations package ON package."PackageId"=delivery."PackageId" JOIN tagekyc.raw_export_assembly_identities identity ON identity."AssemblyId"=package."AssemblyId" WHERE delivery."DeliveryId"=p_delivery AND delivery."RecipientClientApplicationId"=p_recipient;'||pg_catalog.chr(10)||
    '  IF NOT FOUND THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF;'||pg_catalog.chr(10)||
    '  FOR prefix_session IN SELECT DISTINCT "VerificationSessionId" FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=prefix_job ORDER BY "VerificationSessionId" LOOP PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE; IF NOT FOUND THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF; END LOOP;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||snapshot."ClientApplicationId"::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_source_bindings source JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=source."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE source."JobId"=prefix_job AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_package_deliveries delivery JOIN tagekyc.raw_export_recipient_package_preparations package ON package."PackageId"=delivery."PackageId" JOIN tagekyc.raw_export_assembly_identities identity ON identity."AssemblyId"=package."AssemblyId" WHERE delivery."DeliveryId"=p_delivery AND delivery."RecipientClientApplicationId"=p_recipient AND identity."JobId"=prefix_job) THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF;'||pg_catalog.chr(10)||
    '  '||old_;
  IF pg_catalog.strpos(fn,old_)=0 THEN RAISE EXCEPTION 'A3_C3_OUTER_PREFIX_DRIFT'; END IF;
  fn:=pg_catalog.replace(fn,old_,new_);
  EXECUTE fn;
END $a3_c1_c3$;
""";

    private const string ExportCheckpointOperationsRestore = """
DO $a3_c1_c3_restore$
DECLARE
  fn text;
  old_ text;
  new_ text;
BEGIN
  fn:=pg_catalog.pg_get_functiondef('tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  fn:=pg_catalog.replace(fn,'class_row record; source_row record; authority record; consent record; authority_kind text; prefix_session uuid; prefix_client uuid; prefix_reference text; prefix_key bigint;','class_row record; source_row record; authority record; consent record;');
  fn:=pg_catalog.replace(fn,pg_catalog.chr(10)||'                    SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=source_row."AuthoritySnapshotId";','');
  fn:=pg_catalog.replace(fn,'IF authority."AuthoritySnapshotId" IS NULL'||pg_catalog.chr(10)||'                       OR authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||'                       OR (authority_kind=''SourceRetention'' AND (authority."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(source_row."AuthoritySnapshotId",now_)))'||pg_catalog.chr(10)||'                       OR consent."State" IS DISTINCT FROM ''Effective''','IF authority."AuthoritySnapshotId" IS NULL OR consent."State" IS DISTINCT FROM ''Effective''');
  old_:='SELECT * INTO head FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;';
  new_:='SELECT "VerificationSessionId","ClientApplicationId" INTO prefix_session,prefix_client FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id;'||pg_catalog.chr(10)||
    '  IF NOT FOUND THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,0; RETURN; END IF;'||pg_catalog.chr(10)||
    '  PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE;'||pg_catalog.chr(10)||
    '  IF NOT FOUND OR NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id AND "VerificationSessionId"=prefix_session AND "ClientApplicationId"=prefix_client) THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,0; RETURN; END IF;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||prefix_client::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_classes class JOIN tagekyc.raw_export_session_capture_selections selection ON selection."VerificationSessionId"=prefix_session AND selection."RawClass"=class."RawClass" JOIN tagekyc.raw_export_capture_acceptance_events acceptance ON acceptance."CaptureAcceptanceId"=selection."CaptureAcceptanceId" JOIN tagekyc.raw_export_source_ingress_claims claim ON claim."VerificationSessionId"=prefix_session AND claim."CaptureAcceptanceId"=selection."CaptureAcceptanceId" AND claim."CaptureArtifactId"=acceptance."CaptureArtifactId" AND claim."CaptureRevision"=acceptance."CaptureRevision" AND claim."RawClass"=class."RawClass" JOIN tagekyc.raw_export_source_reservations reservation ON reservation."IngressClaimId"=claim."IngressClaimId" JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=reservation."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE class."JobId"=p_job_id AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  '||old_;
  fn:=pg_catalog.replace(fn,new_,old_);
  EXECUTE fn;
  fn:=pg_catalog.pg_get_functiondef('tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  fn:=pg_catalog.replace(fn,'j tagekyc.raw_export_job_identities%ROWTYPE; b record; authority record; consent record; authority_kind text; prefix_session uuid; prefix_reference text; prefix_key bigint; now_ timestamptz;','j tagekyc.raw_export_job_identities%ROWTYPE; b record; authority record; consent record; now_ timestamptz;');
  fn:=pg_catalog.replace(fn,pg_catalog.chr(10)||'                    SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=b."AuthoritySnapshotId";','');
  fn:=pg_catalog.replace(fn,'IF authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||'                       OR (authority_kind=''SourceRetention'' AND (authority."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(b."AuthoritySnapshotId",now_)))'||pg_catalog.chr(10)||'                       OR consent."State" IS DISTINCT FROM ''Effective''','IF consent."State" IS DISTINCT FROM ''Effective''');
  old_:='SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;';
  new_:='FOR prefix_session IN SELECT DISTINCT "VerificationSessionId" FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id ORDER BY "VerificationSessionId" LOOP PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE; IF NOT FOUND THEN RETURN QUERY SELECT ''NotFoundOrNotAllowed''::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF; END LOOP;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||snapshot."ClientApplicationId"::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_source_bindings source JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=source."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE source."JobId"=p_job_id AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  '||old_;
  fn:=pg_catalog.replace(fn,new_,old_);
  EXECUTE fn;
  fn:=pg_catalog.pg_get_functiondef('tagekyc.raw_export_c3_current_authority_eligible(uuid,uuid,uuid,timestamptz)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  fn:=pg_catalog.replace(fn,'authority_row record; authority_kind text;','authority_row record;');
  fn:=pg_catalog.replace(fn,pg_catalog.chr(10)||'        IF NOT FOUND THEN RETURN FALSE; END IF;'||pg_catalog.chr(10)||'        SELECT "AuthorityKind" INTO authority_kind FROM tagekyc.raw_export_authority_snapshots WHERE "AuthoritySnapshotId"=source_row."AuthoritySnapshotId";','');
  fn:=pg_catalog.replace(fn,'OR authority_kind NOT IN (''LegacyExport'',''SourceRetention'')'||pg_catalog.chr(10)||'                           OR (authority_kind=''LegacyExport'' AND authority_row."ApprovedPurpose" <> identity_row."PurposeCode")'||pg_catalog.chr(10)||'                           OR (authority_kind=''SourceRetention'' AND (authority_row."ApprovedPurpose"<>''SourceRetention'' OR NOT tagekyc.raw_export_retained_snapshot_is_current(source_row."AuthoritySnapshotId",p_evaluated_at_utc)))','OR authority_row."ApprovedPurpose" <>'||pg_catalog.chr(10)||'                identity_row."PurposeCode"');
  EXECUTE fn;
  fn:=pg_catalog.pg_get_functiondef('tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)'::regprocedure);
  fn:=pg_catalog.replace(fn,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10));
  fn:=pg_catalog.replace(fn,'actor_api uuid;actor_principal uuid;actor_correlation bytea;prefix_session uuid;prefix_reference text;prefix_key bigint;prefix_job uuid;','actor_api uuid;actor_principal uuid;actor_correlation bytea;');
  old_:='SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x WHERE x."RecipientClientApplicationId"=p_recipient';
  new_:='SELECT identity."JobId" INTO prefix_job FROM tagekyc.raw_export_recipient_package_deliveries delivery JOIN tagekyc.raw_export_recipient_package_preparations package ON package."PackageId"=delivery."PackageId" JOIN tagekyc.raw_export_assembly_identities identity ON identity."AssemblyId"=package."AssemblyId" WHERE delivery."DeliveryId"=p_delivery AND delivery."RecipientClientApplicationId"=p_recipient;'||pg_catalog.chr(10)||
    '  IF NOT FOUND THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF;'||pg_catalog.chr(10)||
    '  FOR prefix_session IN SELECT DISTINCT "VerificationSessionId" FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=prefix_job ORDER BY "VerificationSessionId" LOOP PERFORM 1 FROM tagekyc.verification_sessions WHERE "Id"=prefix_session FOR UPDATE; IF NOT FOUND THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF; END LOOP;'||pg_catalog.chr(10)||
    '  FOR prefix_reference,prefix_key IN SELECT DISTINCT reference."ExternalConsentArtifactRef",pg_catalog.hashtextextended(''tip88c1:a3:consent-reference:''||snapshot."ClientApplicationId"::text||'':''||reference."ExternalConsentArtifactRef",0) FROM tagekyc.raw_export_job_source_bindings source JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=source."AuthoritySnapshotId" JOIN tagekyc.raw_source_consent_bindings binding ON binding."ConsentBindingId"=snapshot."ConsentBindingId" JOIN tagekyc.raw_source_consent_references reference ON reference."ConsentReferenceId"=binding."ConsentReferenceId" WHERE source."JobId"=prefix_job AND snapshot."AuthorityKind"=''SourceRetention'' ORDER BY 2 LOOP PERFORM pg_catalog.pg_advisory_xact_lock_shared(prefix_key); END LOOP;'||pg_catalog.chr(10)||
    '  IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_package_deliveries delivery JOIN tagekyc.raw_export_recipient_package_preparations package ON package."PackageId"=delivery."PackageId" JOIN tagekyc.raw_export_assembly_identities identity ON identity."AssemblyId"=package."AssemblyId" WHERE delivery."DeliveryId"=p_delivery AND delivery."RecipientClientApplicationId"=p_recipient AND identity."JobId"=prefix_job) THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery); RETURN; END IF;'||pg_catalog.chr(10)||
    '  '||old_;
  fn:=pg_catalog.replace(fn,new_,old_);
  EXECUTE fn;
END $a3_c1_c3_restore$;
""";

    // Quiesce writers before either population census, using the real session
    // prefix before capability/binding and then E01 evidence. Locks live until
    // the migration transaction commits or rolls back; no check/drop window.
    private const string CaptureMigrationLocks = """
LOCK TABLE tagekyc.verification_sessions, tagekyc.capture_capabilities,
 tagekyc.capture_execution_bindings, tagekyc.raw_export_authority_snapshots,
 tagekyc.raw_export_source_encryption_attempts IN ACCESS EXCLUSIVE MODE;
""";

    private const string RetainedEncryptionReader = """
DROP FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint);
CREATE FUNCTION tagekyc.raw_export_read_source_encryption_context(
  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
RETURNS TABLE(
  "AttemptId" uuid,"SourceArtifactId" uuid,"EncryptionAttemptRevision" bigint,"Fence" bigint,
  "AttemptKeyReservationId" uuid,"ProvisionalObjectIdentity" uuid,"EncryptionAttemptFingerprint" bytea,
  "KeyProviderId" text,"KekId" text,"KekVersion" integer,"KekFingerprint" text,
  "EncryptionSuiteId" text,"EncryptionFramingVersion" integer,"ChunkSize" integer,"NonceStrategyId" text,
  "NonceDerivationSeedReferenceOrWrappedSeed" text,"NonceDerivationSeedCommitment" bytea,
  "FramingParametersDigest" bytea,"WrappedDekMetadataDigest" bytea,
  "VerificationSessionId" uuid,"CaptureArtifactId" uuid,"CaptureRevision" integer,"RawClass" text,
  "StableDataScopeId" text,"ControllerIdentity" text,"ClaimedPlaintextLength" bigint,"MediaType" text,
  "ContentCommitmentSchemaVersion" integer,"ContentCommitmentKeyId" text,
  "ContentCommitmentKeyVersion" integer,"ContentCommitment" bytea,
  "OwnershipLeaseExpiresAtUtc" timestamptz,"EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
  "ReservationExpiresAtUtc" timestamptz,"AuthorityKind" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
BEGIN
  actor_id:=tagekyc.raw_export_current_actor();
  RETURN QUERY
  SELECT a."AttemptId",a."SourceArtifactId",a."EncryptionAttemptRevision",a."Fence",
    a."AttemptKeyReservationId",a."ProvisionalObjectIdentity",a."EncryptionAttemptFingerprint",
    a."KeyProviderId"::text,a."KekId"::text,a."KekVersion",a."KekFingerprint"::text,
    a."EncryptionSuiteId"::text,a."EncryptionFramingVersion",a."ChunkSize",a."NonceStrategyId"::text,
    a."NonceDerivationSeedReferenceOrWrappedSeed"::text,a."NonceDerivationSeedCommitment",
    a."FramingParametersDigest",k."WrappedDekMetadataDigest",
    c."VerificationSessionId",c."CaptureArtifactId",c."CaptureRevision",c."RawClass"::text,
    r."StableDataScopeId"::text,r."ControllerIdentity"::text,r."ClaimedPlaintextLength",r."MediaType"::text,
    r."ContentCommitmentSchemaVersion",r."ContentCommitmentKeyId"::text,
    r."ContentCommitmentKeyVersion",r."ContentCommitment",
    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",snapshot."AuthorityKind"::text
  FROM tagekyc.raw_export_source_encryption_attempts a
  JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
    AND h."CurrentEncryptionAttemptId"=a."AttemptId" AND h."Fence"=a."Fence"
  JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
  JOIN tagekyc.raw_export_authority_snapshots snapshot ON snapshot."AuthoritySnapshotId"=r."AuthoritySnapshotId"
  JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
  JOIN tagekyc.raw_export_attempt_key_reservations k
    ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
  WHERE a."AttemptId"=p_attempt_id AND a."EncryptionAttemptRevision"=p_expected_revision
    AND a."Fence"=p_expected_fence AND a."R2TerminationDisposition" IS NULL
    AND a."R2TerminatedAtUtc" IS NULL AND k."PreparationDisposition"='Active'
    AND pg_catalog.octet_length(k."WrappedDekMetadataDigest")=32
    AND a."OwnershipLeaseExpiresAtUtc">now_utc
    AND r."EffectivePlaintextRetentionExpiresAtUtc">now_utc AND r."ReservationExpiresAtUtc">now_utc;
END $fn$;
ALTER FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)
 FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_reconciler,
 tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) TO tagekyc_raw_export_custody_encryptor;
""";

    private const string RetainedEncryptionReaderRestore = """
DROP FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint);
CREATE FUNCTION tagekyc.raw_export_read_source_encryption_context(
  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
RETURNS TABLE(
  "AttemptId" uuid,"SourceArtifactId" uuid,"EncryptionAttemptRevision" bigint,"Fence" bigint,
  "AttemptKeyReservationId" uuid,"ProvisionalObjectIdentity" uuid,"EncryptionAttemptFingerprint" bytea,
  "KeyProviderId" text,"KekId" text,"KekVersion" integer,"KekFingerprint" text,
  "EncryptionSuiteId" text,"EncryptionFramingVersion" integer,"ChunkSize" integer,"NonceStrategyId" text,
  "NonceDerivationSeedReferenceOrWrappedSeed" text,"NonceDerivationSeedCommitment" bytea,
  "FramingParametersDigest" bytea,"WrappedDekMetadataDigest" bytea,
  "VerificationSessionId" uuid,"CaptureArtifactId" uuid,"CaptureRevision" integer,"RawClass" text,
  "StableDataScopeId" text,"ControllerIdentity" text,"ClaimedPlaintextLength" bigint,"MediaType" text,
  "ContentCommitmentSchemaVersion" integer,"ContentCommitmentKeyId" text,
  "ContentCommitmentKeyVersion" integer,"ContentCommitment" bytea,
  "OwnershipLeaseExpiresAtUtc" timestamptz,"EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
  "ReservationExpiresAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
BEGIN
  actor_id:=tagekyc.raw_export_current_actor();
  RETURN QUERY
  SELECT a."AttemptId",a."SourceArtifactId",a."EncryptionAttemptRevision",a."Fence",
    a."AttemptKeyReservationId",a."ProvisionalObjectIdentity",a."EncryptionAttemptFingerprint",
    a."KeyProviderId"::text,a."KekId"::text,a."KekVersion",a."KekFingerprint"::text,
    a."EncryptionSuiteId"::text,a."EncryptionFramingVersion",a."ChunkSize",a."NonceStrategyId"::text,
    a."NonceDerivationSeedReferenceOrWrappedSeed"::text,a."NonceDerivationSeedCommitment",
    a."FramingParametersDigest",k."WrappedDekMetadataDigest",
    c."VerificationSessionId",c."CaptureArtifactId",c."CaptureRevision",c."RawClass"::text,
    r."StableDataScopeId"::text,r."ControllerIdentity"::text,r."ClaimedPlaintextLength",r."MediaType"::text,
    r."ContentCommitmentSchemaVersion",r."ContentCommitmentKeyId"::text,
    r."ContentCommitmentKeyVersion",r."ContentCommitment",
    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc"
  FROM tagekyc.raw_export_source_encryption_attempts a
  JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
    AND h."CurrentEncryptionAttemptId"=a."AttemptId" AND h."Fence"=a."Fence"
  JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
  JOIN tagekyc.raw_export_attempt_key_reservations k
    ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
  WHERE a."AttemptId"=p_attempt_id AND a."EncryptionAttemptRevision"=p_expected_revision
    AND a."Fence"=p_expected_fence AND a."R2TerminationDisposition" IS NULL
    AND a."R2TerminatedAtUtc" IS NULL AND k."PreparationDisposition"='Active'
    AND pg_catalog.octet_length(k."WrappedDekMetadataDigest")=32
    AND a."OwnershipLeaseExpiresAtUtc">now_utc
    AND r."EffectivePlaintextRetentionExpiresAtUtc">now_utc AND r."ReservationExpiresAtUtc">now_utc;
END $fn$;
ALTER FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)
 FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_reconciler,
 tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint) TO tagekyc_raw_export_custody_encryptor;
""";

    private const string RawIngressCallerAuthorization = """
CREATE FUNCTION tagekyc.capture_runtime_raw_ingress_caller_owns_session(
 p_capture_agent_id uuid,p_installation_id uuid,p_credential_id uuid,p_generation bigint,
 p_role_policy_id uuid,p_role_policy_revision bigint,p_session_id uuid,
 p_configuration_revision bigint,p_raw_class text,p_now timestamptz)
RETURNS boolean
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $caller$
 SELECT EXISTS(
  SELECT 1
  FROM tagekyc.capture_execution_bindings b
  JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=b."CaptureAgentId"
  JOIN tagekyc.capture_runtime_installations i ON i."DeviceInstallationId"=b."DeviceInstallationId"
  JOIN tagekyc.capture_runtime_credential_generations g
    ON g."CredentialId"=b."CredentialId" AND g."Generation"=b."CredentialGeneration"
  JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
  JOIN tagekyc.verification_sessions s ON s."Id"=b."VerificationSessionId"
  JOIN tagekyc.raw_source_retention_permits p
    ON p."RetentionAuthorityId"=b."RetentionAuthorityId" AND p."Revision"=b."RetentionAuthorityRevision"
  JOIN tagekyc.raw_source_consent_bindings cb ON cb."ConsentBindingId"=p."ConsentBindingId"
  JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=cb."ConsentReferenceId"
  WHERE b."VerificationSessionId"=p_session_id
   AND b."CaptureAgentId"=p_capture_agent_id AND b."DeviceInstallationId"=p_installation_id
   AND b."CredentialId"=p_credential_id AND b."CredentialGeneration"=p_generation
   AND b."RolePolicyId"=p_role_policy_id AND b."RolePolicyRevision"=p_role_policy_revision
   AND b."ConfigurationRevision"=p_configuration_revision
   AND b."AuthorityMode"='SourceRetention' AND c."AuthorityMode"='SourceRetention'
   AND c."State"='Bound' AND c."VerificationSessionId"=p_session_id
   AND c."ClientApplicationId"=b."ClientApplicationId" AND s."ClientApplicationId"=b."ClientApplicationId"
   AND c."PrincipalId"=b."PrincipalId" AND p."PrincipalId"=b."PrincipalId"
   AND p."ClientApplicationId"=b."ClientApplicationId" AND p."VerificationSessionId"=p_session_id
   AND cb."ClientApplicationId"=b."ClientApplicationId" AND cb."VerificationSessionId"=p_session_id
   AND h."ClientApplicationId"=b."ClientApplicationId" AND h."SubjectRef"=s."SubjectRef"
   AND c."Challenge"=b."Challenge" AND b."Challenge"=s."BindingNonceHash"
   AND EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permit_classes pc
     WHERE pc."RetentionAuthorityId"=p."RetentionAuthorityId" AND pc."Revision"=p."Revision"
      AND pc."RawClass"=p_raw_class)
   AND r."LifecycleState"='Active' AND r."RuntimeType"='Managed'
   AND i."LifecycleState"='Active' AND i."CaptureAgentId"=p_capture_agent_id
   AND i."CurrentCredentialId"=p_credential_id AND i."CurrentCredentialGeneration"=p_generation
   AND g."State"='Active' AND g."DeviceInstallationId"=p_installation_id
   AND g."RolePolicyId"=p_role_policy_id AND g."RolePolicyRevision"=p_role_policy_revision
   AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
   AND b."ExecutionExpiresAtUtc">p_now AND c."ExpiresAtUtc">p_now AND s."ExpiresAt">p_now
 );
$caller$;
ALTER FUNCTION tagekyc.capture_runtime_raw_ingress_caller_owns_session(uuid,uuid,uuid,bigint,uuid,bigint,uuid,bigint,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_raw_ingress_caller_owns_session(uuid,uuid,uuid,bigint,uuid,bigint,uuid,bigint,text,timestamptz)
 FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_raw_ingress_caller_owns_session(uuid,uuid,uuid,bigint,uuid,bigint,uuid,bigint,text,timestamptz)
 TO tagekyc_raw_export_claim_broker;
""";

    private const string BoundRawIngressReader = """
CREATE FUNCTION tagekyc.capture_runtime_read_bound_raw_ingress(
 p_capture_agent_id uuid,p_installation_id uuid,p_credential_id uuid,p_generation bigint,
 p_role_policy_id uuid,p_role_policy_revision bigint,p_session_id uuid,p_artifact_id uuid,
 p_capture_revision integer,p_raw_class text,p_configuration_revision bigint,p_now timestamptz)
RETURNS TABLE(binding_id uuid,capability_id uuid,capability_revision bigint,principal_id uuid,
 client_application_id uuid,verification_session_id uuid,capture_acceptance_id uuid,
 capture_acceptance_revision bigint,session_challenge_hash text,source_retention_authority_id uuid,
 source_retention_authority_revision bigint,execution_expires_at_utc timestamptz,
 stable_data_scope_id text,controller_identity text,subject_ref text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $bound$
DECLARE
 r tagekyc.capture_runtime_registrations%ROWTYPE;
 i tagekyc.capture_runtime_installations%ROWTYPE;
 g tagekyc.capture_runtime_credential_generations%ROWTYPE;
 b tagekyc.capture_execution_bindings%ROWTYPE;
 c tagekyc.capture_capabilities%ROWTYPE;
 s tagekyc.verification_sessions%ROWTYPE;
 p tagekyc.raw_source_retention_permits%ROWTYPE;
 cb tagekyc.raw_source_consent_bindings%ROWTYPE;
 h tagekyc.raw_source_consent_references%ROWTYPE;
 a tagekyc.raw_export_capture_acceptance_events%ROWTYPE;
 configuration record; discovered_binding uuid; discovered_capability uuid;
 requirement text; locked_now timestamptz;
BEGIN
 IF p_capture_agent_id IS NULL OR p_installation_id IS NULL OR p_credential_id IS NULL
 OR p_role_policy_id IS NULL OR p_session_id IS NULL OR p_artifact_id IS NULL
 OR '00000000-0000-0000-0000-000000000000'::uuid=ANY(ARRAY[
  p_capture_agent_id,p_installation_id,p_credential_id,p_role_policy_id,p_session_id,p_artifact_id])
 OR p_generation IS NULL OR p_generation<1 OR p_role_policy_revision IS NULL OR p_role_policy_revision<1
 OR p_capture_revision IS NULL OR p_capture_revision<1 OR p_configuration_revision IS NULL OR p_configuration_revision<1
 OR p_raw_class IS NULL OR p_raw_class NOT IN ('ChipDg2Portrait','LiveSelfieImage')
 OR p_now IS NULL OR NOT isfinite(p_now) THEN RETURN; END IF;
 -- A1 runtime domains first. These observations do not disclose or select a principal.
 PERFORM pg_advisory_xact_lock(hashtextextended(p_capture_agent_id::text,10));
 PERFORM pg_advisory_xact_lock(hashtextextended(p_installation_id::text,20));
 PERFORM pg_advisory_xact_lock(hashtextextended(p_credential_id::text||':'||p_generation::text,30));
 BEGIN
  SELECT x."CaptureExecutionBindingId",x."CaptureCapabilityId" INTO STRICT discovered_binding,discovered_capability
  FROM tagekyc.capture_execution_bindings x WHERE x."VerificationSessionId"=p_session_id
   AND x."CaptureAgentId"=p_capture_agent_id AND x."DeviceInstallationId"=p_installation_id
   AND x."CredentialId"=p_credential_id AND x."CredentialGeneration"=p_generation;
 EXCEPTION WHEN no_data_found OR too_many_rows THEN RETURN;
 END;
 PERFORM pg_advisory_xact_lock(hashtextextended(p_session_id::text,70));
 PERFORM pg_advisory_xact_lock(hashtextextended(discovered_capability::text,80));
 SELECT * INTO r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
 SELECT * INTO i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_installation_id FOR UPDATE;
 SELECT * INTO g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
 IF r."LifecycleState" IS DISTINCT FROM 'Active' OR r."RuntimeType" IS DISTINCT FROM 'Managed'
 OR i."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR i."LifecycleState" IS DISTINCT FROM 'Active'
 OR i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
 OR g."DeviceInstallationId" IS DISTINCT FROM p_installation_id OR g."State" IS DISTINCT FROM 'Active'
 OR g."RolePolicyId" IS DISTINCT FROM p_role_policy_id OR g."RolePolicyRevision" IS DISTINCT FROM p_role_policy_revision
 OR r."ConfigurationRevision" IS DISTINCT FROM p_configuration_revision THEN RETURN; END IF;
 -- Actual session row precedes the reference and all subsequent source locks.
 SELECT * INTO s FROM tagekyc.verification_sessions WHERE "Id"=p_session_id FOR UPDATE;
 SELECT * INTO b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=discovered_binding FOR UPDATE;
 SELECT * INTO c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=discovered_capability FOR UPDATE;
 IF b."VerificationSessionId" IS DISTINCT FROM p_session_id OR b."CaptureCapabilityId" IS DISTINCT FROM discovered_capability
 OR b."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR b."DeviceInstallationId" IS DISTINCT FROM p_installation_id
 OR b."CredentialId" IS DISTINCT FROM p_credential_id OR b."CredentialGeneration" IS DISTINCT FROM p_generation
 OR b."RuntimeRevision" IS DISTINCT FROM r."Revision" OR b."InstallationRevision" IS DISTINCT FROM i."Revision"
 OR b."CredentialRevision" IS DISTINCT FROM g."Revision" OR b."PublicKeyThumbprint" IS DISTINCT FROM g."PublicKeyThumbprint"
 OR b."RolePolicyId" IS DISTINCT FROM p_role_policy_id OR b."RolePolicyRevision" IS DISTINCT FROM p_role_policy_revision
 OR b."TrustProfileId" IS DISTINCT FROM r."TrustProfileId" OR b."TrustProfileRevision" IS DISTINCT FROM r."TrustProfileRevision"
 OR b."ConfigurationId" IS DISTINCT FROM r."ConfigurationId" OR b."ConfigurationRevision" IS DISTINCT FROM p_configuration_revision
 OR b."AuthorityMode" IS DISTINCT FROM 'SourceRetention' OR c."AuthorityMode" IS DISTINCT FROM 'SourceRetention'
 OR c."State" IS DISTINCT FROM 'Bound' OR c."ClientApplicationId" IS DISTINCT FROM b."ClientApplicationId"
 OR c."VerificationSessionId" IS DISTINCT FROM p_session_id OR s."ClientApplicationId" IS DISTINCT FROM b."ClientApplicationId"
 OR b."Challenge" IS DISTINCT FROM s."BindingNonceHash" OR c."Challenge" IS DISTINCT FROM b."Challenge"
 OR c."PrincipalId" IS DISTINCT FROM b."PrincipalId" OR b."PrincipalId" IS NULL
 OR b."PrincipalId"='00000000-0000-0000-0000-000000000000'::uuid
 OR c."ConsentBindingId" IS DISTINCT FROM b."ConsentBindingId"
 OR c."RetentionAuthorityId" IS DISTINCT FROM b."RetentionAuthorityId"
 OR c."RetentionAuthorityRevision" IS DISTINCT FROM b."RetentionAuthorityRevision"
 OR s."State" IN ('Expired','Cancelled','TechnicalTerminal') THEN RETURN; END IF;
 SELECT * INTO p FROM tagekyc.raw_source_retention_permits x
 WHERE x."RetentionAuthorityId"=b."RetentionAuthorityId" AND x."Revision"=b."RetentionAuthorityRevision"
 AND x."ConsentBindingId"=b."ConsentBindingId" AND x."PrincipalId"=b."PrincipalId"
 AND x."ClientApplicationId"=b."ClientApplicationId" AND x."VerificationSessionId"=p_session_id;
 IF NOT FOUND THEN RETURN; END IF;
 SELECT * INTO cb FROM tagekyc.raw_source_consent_bindings x WHERE x."ConsentBindingId"=p."ConsentBindingId";
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x
 WHERE x."ConsentReferenceId"=cb."ConsentReferenceId" AND x."ClientApplicationId"=b."ClientApplicationId";
 IF NOT FOUND THEN RETURN; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtextextended(
  'tip88c1:a3:consent-reference:'||b."ClientApplicationId"::text||':'||h."ExternalConsentArtifactRef",0));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||p."PolicyId"::text||':'||p."PolicyVersion"::text));
 FOR requirement IN SELECT x."RequirementType" FROM tagekyc.raw_export_policy_requirements x
 WHERE x."PolicyId"=p."PolicyId" AND x."PolicyVersion"=p."PolicyVersion"
 AND x."RequirementType"<>'ConsentArtifact' ORDER BY x."RequirementType" COLLATE "C"
 LOOP PERFORM pg_advisory_xact_lock_shared(hashtext(
  'tip88b1:fulfillment:'||p."PolicyId"::text||':'||p."PolicyVersion"::text||':'||requirement)); END LOOP;
 BEGIN
  SELECT x.* INTO STRICT a FROM tagekyc.raw_export_capture_acceptance_events x
  JOIN tagekyc.capture_artifacts art ON art."Id"=x."CaptureArtifactId"
  WHERE x."VerificationSessionId"=p_session_id AND x."ClientApplicationId"=b."ClientApplicationId"
   AND x."CaptureArtifactId"=p_artifact_id AND x."CaptureRevision"=p_capture_revision AND x."RawClass"=p_raw_class
   AND x."SessionChallengeHash"=b."Challenge" AND art."VerificationSessionId"=p_session_id
   AND art."CaptureAgentId"=replace(p_capture_agent_id::text,'-','')
   AND art."DeviceId"=replace(p_installation_id::text,'-','');
 EXCEPTION WHEN no_data_found OR too_many_rows THEN RETURN;
 END;
 locked_now:=clock_timestamp();
 IF g."ValidFromUtc">locked_now OR g."ValidUntilUtc"<=locked_now
 OR b."ExecutionExpiresAtUtc"<=locked_now OR c."ExpiresAtUtc"<=locked_now OR s."ExpiresAt"<=locked_now
 OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
  WHERE rp."CatalogId"=p_role_policy_id AND rp."Revision"=p_role_policy_revision
  AND rp."EffectiveAtUtc"<=locked_now AND 'RawIngress'=ANY(rp."Roles"))
 OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp
  WHERE tp."CatalogId"=r."TrustProfileId" AND tp."Revision"=r."TrustProfileRevision"
  AND tp."RuntimeType"='Managed' AND tp."RetainedRawEnabled"
  AND tp."EffectiveAtUtc"<=locked_now AND tp."ExpiresAtUtc">locked_now) THEN RETURN; END IF;
 SELECT * INTO configuration FROM tagekyc.capture_runtime_resolve_configuration(
  p_capture_agent_id,p_installation_id,p_credential_id,p_generation,locked_now);
 IF configuration.result_code IS DISTINCT FROM 'AVAILABLE' OR configuration.raw_export_enabled IS NOT TRUE
 OR configuration.configuration_revision IS DISTINCT FROM p_configuration_revision THEN RETURN; END IF;
 RETURN QUERY SELECT b."CaptureExecutionBindingId",c."CaptureCapabilityId",c."Revision",b."PrincipalId",
  b."ClientApplicationId",p_session_id,a."CaptureAcceptanceId",a."CaptureRevision"::bigint,
  a."SessionChallengeHash"::text,p."RetentionAuthorityId",p."Revision",b."ExecutionExpiresAtUtc",
  p."StableDataScopeId"::text,p."ControllerIdentity"::text,s."SubjectRef"::text;
END $bound$;
ALTER FUNCTION tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz) TO tagekyc_raw_export_claim_broker;

""";

    private const string RetainedBeginOperation = """
CREATE FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(
    p_authenticated_principal_id uuid,
    p_client_application_id uuid,
    p_producer_id text,
    p_capture_agent_instance_id text,
    p_ingress_idempotency_key text,
    p_verification_session_id uuid,
    p_capture_acceptance_id uuid,
    p_capture_artifact_id uuid,
    p_capture_revision integer,
    p_raw_class text,
    p_session_challenge_hash text,
    p_claimed_plaintext_length bigint,
    p_media_type text,
    p_captured_at_utc timestamp with time zone,
    p_plaintext_retention_started_at_utc timestamp with time zone,
    p_plaintext_retention_expires_at_utc timestamp with time zone,
    p_plaintext_retention_budget_seconds integer,
    p_commitment_key_selector_id text,
    p_commitment_key_selector_version integer,
    p_claim_evaluation_owner_id uuid,
    p_claim_evaluation_token_ttl_seconds integer,
    p_idempotency_lock_timeout_milliseconds integer,
    p_retention_authority_id uuid,
    p_retention_authority_revision bigint)
RETURNS TABLE(
    outcome_code text,
    claim_evaluation_token text,
    token_variant text,
    token_expires_at_utc timestamp with time zone,
    claim_evaluation_id uuid,
    claim_evaluation_revision bigint,
    claim_evaluation_fence bigint,
    retry_not_before_utc timestamp with time zone,
    attempted_identity_fingerprint bytea, producer_envelope_fingerprint bytea,
    canonical_ingress_identity_fingerprint bytea,
    comparison_commitment_selector_id text, comparison_commitment_selector_version integer)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    ingress_key uuid;
    producer_id_nfc text;
    capture_agent_instance_id_nfc text;
    raw_class_nfc text;
    session_challenge_hash_nfc text;
    authority_snapshot_id_nfc text;
    media_type_nfc text;
    key_selector_id_nfc text;
    accepted_session_id uuid;
    accepted_client_id uuid;
    accepted_artifact_id uuid;
    accepted_revision integer;
    accepted_raw_class text;
    accepted_challenge_hash text;
    artifact_session_id uuid;
    session_client_id uuid;
    alias_lock bigint;
    exact_lock bigint;
    first_lock bigint;
    second_lock bigint;
    lock_deadline timestamp with time zone;
    acquired boolean;
    value_bytes bytea;
    canonical_preimage bytea;
    ingress_fingerprint bytea;
    envelope_fingerprint bytea;
    claim_row tagekyc.raw_export_source_ingress_claims%ROWTYPE;
    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
    new_claim boolean := false;
    issued_at timestamp with time zone;
    expires_at timestamp with time zone;
    evaluation_id uuid;
    evaluation_revision bigint;
    evaluation_fence bigint;
    evaluation_token_bytes bytea;
    evaluation_token text;
    evaluation_token_digest bytea;
    issued_variant text;
    previous_context text;
    rows_changed integer;
    p_authority_snapshot_id text;
    retained_binding tagekyc.capture_execution_bindings%ROWTYPE;
    retained_reference record;
    retained_policy record;
    retained_requirement text;
    retained_now timestamptz;
    alias_found boolean;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF actor_id IS DISTINCT FROM p_authenticated_principal_id THEN
        RAISE EXCEPTION USING
            ERRCODE = 'P0001',
            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
    END IF;

    IF p_authenticated_principal_id IS NULL
       OR p_client_application_id IS NULL
       OR p_verification_session_id IS NULL
       OR p_capture_acceptance_id IS NULL
       OR p_capture_artifact_id IS NULL
       OR p_claim_evaluation_owner_id IS NULL
       OR p_capture_revision IS NULL
       OR p_capture_revision < 1
       OR p_claimed_plaintext_length IS NULL
       OR p_claimed_plaintext_length < 0
       OR p_plaintext_retention_budget_seconds IS NULL
       OR p_plaintext_retention_budget_seconds < 1
       OR p_commitment_key_selector_version IS NULL
       OR p_commitment_key_selector_version < 1
       OR p_claim_evaluation_token_ttl_seconds IS NULL
       OR p_claim_evaluation_token_ttl_seconds < 1
       OR p_claim_evaluation_token_ttl_seconds > 3600
       OR p_idempotency_lock_timeout_milliseconds IS NULL
       OR p_idempotency_lock_timeout_milliseconds < 1
       OR p_idempotency_lock_timeout_milliseconds > 30000
       OR p_captured_at_utc IS NULL
       OR p_plaintext_retention_started_at_utc IS NULL
       OR p_plaintext_retention_expires_at_utc IS NULL
       OR p_plaintext_retention_started_at_utc < p_captured_at_utc
       OR p_plaintext_retention_expires_at_utc
            <= p_plaintext_retention_started_at_utc THEN
        RAISE EXCEPTION USING
            ERRCODE = 'P0001',
            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
    END IF;


    -- B-R supplies these identities. B-B still checks their exact immutable
    -- binding/permit/session lineage; it never substitutes actor for producer.
    BEGIN
        SELECT b.* INTO STRICT retained_binding
        FROM tagekyc.capture_execution_bindings b
        JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
        WHERE b."ClientApplicationId"=p_client_application_id AND b."PrincipalId"=actor_id
         AND b."VerificationSessionId"=p_verification_session_id AND b."AuthorityMode"='SourceRetention'
         AND b."RetentionAuthorityId"=p_retention_authority_id
         AND b."RetentionAuthorityRevision"=p_retention_authority_revision
         AND pg_catalog.replace(b."CaptureAgentId"::text,'-','')=p_producer_id
         AND pg_catalog.replace(b."DeviceInstallationId"::text,'-','')=p_capture_agent_instance_id
         AND c."State"='Bound' AND c."AuthorityMode"='SourceRetention'
         AND c."PrincipalId"=b."PrincipalId" AND c."ClientApplicationId"=b."ClientApplicationId"
         AND c."VerificationSessionId"=b."VerificationSessionId"
         AND c."ConsentBindingId"=b."ConsentBindingId"
         AND c."RetentionAuthorityId"=b."RetentionAuthorityId"
         AND c."RetentionAuthorityRevision"=b."RetentionAuthorityRevision";
    EXCEPTION WHEN NO_DATA_FOUND OR TOO_MANY_ROWS THEN
        outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN;
    END;
    PERFORM 1 FROM tagekyc.verification_sessions s
     WHERE s."Id"=retained_binding."VerificationSessionId" AND s."ClientApplicationId"=retained_binding."ClientApplicationId" FOR UPDATE;
    IF NOT FOUND THEN outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN; END IF;
    SELECT h."ConsentReferenceId",h."ExternalConsentArtifactRef" INTO retained_reference
    FROM tagekyc.raw_source_consent_bindings b JOIN tagekyc.raw_source_consent_references h USING("ConsentReferenceId")
    WHERE b."ConsentBindingId"=retained_binding."ConsentBindingId" AND b."ClientApplicationId"=p_client_application_id
     AND b."VerificationSessionId"=p_verification_session_id;
    IF NOT FOUND THEN outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
      'tip88c1:a3:consent-reference:'||p_client_application_id::text||':'||retained_reference."ExternalConsentArtifactRef",0));
    SELECT p."PolicyId",p."PolicyVersion" INTO retained_policy FROM tagekyc.raw_source_retention_permits p
     WHERE p."RetentionAuthorityId"=p_retention_authority_id AND p."Revision"=p_retention_authority_revision;
    IF NOT FOUND THEN outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
      'tip88b1:lifecycle:'||retained_policy."PolicyId"::text||':'||retained_policy."PolicyVersion"::text));
    FOR retained_requirement IN SELECT r."RequirementType" FROM tagekyc.raw_export_policy_requirements r
     WHERE r."PolicyId"=retained_policy."PolicyId" AND r."PolicyVersion"=retained_policy."PolicyVersion"
      AND r."RequirementType"<>'ConsentArtifact' ORDER BY r."RequirementType" COLLATE "C" LOOP
        PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
          'tip88b1:fulfillment:'||retained_policy."PolicyId"::text||':'||retained_policy."PolicyVersion"::text||':'||retained_requirement));
    END LOOP;
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
      'tip88c1:b2-authority:'||p_client_application_id::text||':'||p_verification_session_id::text||':'||p_capture_acceptance_id::text||':'||p_raw_class));
    retained_now:=pg_catalog.clock_timestamp();
    IF retained_binding."ExecutionExpiresAtUtc"<=retained_now
       OR NOT EXISTS(SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(
         p_retention_authority_id,p_retention_authority_revision,actor_id,p_client_application_id,p_verification_session_id,p_raw_class,retained_now))
       OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_artifacts a WHERE a."Id"=p_capture_artifact_id
          AND a."VerificationSessionId"=p_verification_session_id
          AND a."CaptureAgentId"=p_producer_id AND a."DeviceId"=p_capture_agent_instance_id) THEN
        outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN;
    END IF;
    SELECT s."AuthoritySnapshotId"::text INTO p_authority_snapshot_id
    FROM tagekyc.raw_export_append_retained_authority_snapshot(
      retained_binding."CaptureExecutionBindingId",p_capture_acceptance_id,p_raw_class,p_retention_authority_id,p_retention_authority_revision) s;

    producer_id_nfc := pg_catalog.normalize(p_producer_id, 'NFC');
    capture_agent_instance_id_nfc :=
        pg_catalog.normalize(p_capture_agent_instance_id, 'NFC');
    raw_class_nfc := pg_catalog.normalize(p_raw_class, 'NFC');
    session_challenge_hash_nfc :=
        pg_catalog.normalize(p_session_challenge_hash, 'NFC');
    authority_snapshot_id_nfc :=
        pg_catalog.normalize(p_authority_snapshot_id, 'NFC');
    media_type_nfc := pg_catalog.normalize(p_media_type, 'NFC');
    key_selector_id_nfc :=
        pg_catalog.normalize(p_commitment_key_selector_id, 'NFC');

    IF producer_id_nfc IS NULL
       OR producer_id_nfc = ''
       OR pg_catalog.octet_length(producer_id_nfc) > 128
       OR capture_agent_instance_id_nfc IS NULL
       OR capture_agent_instance_id_nfc = ''
       OR pg_catalog.octet_length(capture_agent_instance_id_nfc) > 128
       OR raw_class_nfc IS NULL
       OR raw_class_nfc NOT IN (
            'ChipDg1',
            'ChipDg2Portrait',
            'ChipDg13',
            'ChipDg15',
            'ChipSod',
            'AaChallenge',
            'AaResponse',
            'LiveSelfieImage',
            'LivenessMedia',
            'HandSignatureImage')
       OR session_challenge_hash_nfc IS NULL
       OR session_challenge_hash_nfc = ''
       OR pg_catalog.octet_length(session_challenge_hash_nfc) > 128
       OR authority_snapshot_id_nfc IS NULL
       OR authority_snapshot_id_nfc = ''
       OR pg_catalog.octet_length(authority_snapshot_id_nfc) > 128
       OR media_type_nfc IS NULL
       OR media_type_nfc = ''
       OR pg_catalog.octet_length(media_type_nfc) > 128
       OR key_selector_id_nfc IS NULL
       OR key_selector_id_nfc = ''
       OR pg_catalog.octet_length(key_selector_id_nfc) > 128
       OR p_ingress_idempotency_key IS NULL
       OR p_ingress_idempotency_key
            !~ '^[0-9a-f]{12}4[0-9a-f]{3}[89ab][0-9a-f]{15}$' THEN
        RAISE EXCEPTION USING
            ERRCODE = 'P0001',
            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
    END IF;

    ingress_key := p_ingress_idempotency_key::uuid;

    SELECT
        session."ClientApplicationId",
        artifact."VerificationSessionId",
        acceptance."VerificationSessionId",
        acceptance."ClientApplicationId",
        acceptance."CaptureArtifactId",
        acceptance."CaptureRevision",
        acceptance."RawClass",
        acceptance."SessionChallengeHash"
    INTO
        session_client_id,
        artifact_session_id,
        accepted_session_id,
        accepted_client_id,
        accepted_artifact_id,
        accepted_revision,
        accepted_raw_class,
        accepted_challenge_hash
    FROM tagekyc.verification_sessions AS session
    JOIN tagekyc.capture_artifacts AS artifact
      ON artifact."Id" = p_capture_artifact_id
    JOIN tagekyc.raw_export_capture_acceptance_events AS acceptance
      ON acceptance."CaptureAcceptanceId" = p_capture_acceptance_id
    WHERE session."Id" = p_verification_session_id;

    IF NOT FOUND
       OR session_client_id IS DISTINCT FROM p_client_application_id
       OR artifact_session_id IS DISTINCT FROM p_verification_session_id
       OR accepted_session_id IS DISTINCT FROM p_verification_session_id
       OR accepted_client_id IS DISTINCT FROM p_client_application_id
       OR accepted_artifact_id IS DISTINCT FROM p_capture_artifact_id
       OR accepted_revision IS DISTINCT FROM p_capture_revision
       OR accepted_raw_class IS DISTINCT FROM raw_class_nfc
       OR accepted_challenge_hash IS DISTINCT FROM session_challenge_hash_nfc THEN
        RAISE EXCEPTION USING
            ERRCODE = 'P0001',
            MESSAGE = 'RAW_EXPORT_SOURCE_BINDING_INVALID';
    END IF;

    canonical_preimage := ''::bytea;
    value_bytes := pg_catalog.convert_to(
        'tip-88c1-ingress-identity-v1',
        'UTF8');
    canonical_preimage := canonical_preimage
        || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
        || value_bytes;
    FOREACH value_bytes IN ARRAY ARRAY[
        pg_catalog.convert_to(
            pg_catalog.replace(p_client_application_id::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(producer_id_nfc, 'UTF8'),
        pg_catalog.convert_to(capture_agent_instance_id_nfc, 'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.replace(ingress_key::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.replace(p_authenticated_principal_id::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.replace(p_verification_session_id::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.replace(p_capture_acceptance_id::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.replace(p_capture_artifact_id::text, '-', ''),
            'UTF8'),
        pg_catalog.convert_to(p_capture_revision::text, 'UTF8'),
        pg_catalog.convert_to(raw_class_nfc, 'UTF8'),
        pg_catalog.convert_to(session_challenge_hash_nfc, 'UTF8'),
        pg_catalog.convert_to(authority_snapshot_id_nfc, 'UTF8')
    ]
    LOOP
        canonical_preimage := canonical_preimage
            || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
            || value_bytes;
    END LOOP;
    ingress_fingerprint := pg_catalog.sha256(canonical_preimage);

    canonical_preimage := ''::bytea;
    value_bytes := pg_catalog.convert_to(
        'tip-88c1-producer-claim-envelope-v2',
        'UTF8');
    canonical_preimage := canonical_preimage
        || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
        || value_bytes;
    FOREACH value_bytes IN ARRAY ARRAY[
        pg_catalog.convert_to(
            pg_catalog.encode(ingress_fingerprint, 'hex'),
            'UTF8'),
        pg_catalog.convert_to(p_claimed_plaintext_length::text, 'UTF8'),
        pg_catalog.convert_to(media_type_nfc, 'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.to_char(
                pg_catalog.date_trunc(
                    'microseconds',
                    p_captured_at_utc AT TIME ZONE 'UTC'),
                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.to_char(
                pg_catalog.date_trunc(
                    'microseconds',
                    p_plaintext_retention_started_at_utc
                        AT TIME ZONE 'UTC'),
                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
            'UTF8'),
        pg_catalog.convert_to(
            pg_catalog.to_char(
                pg_catalog.date_trunc(
                    'microseconds',
                    p_plaintext_retention_expires_at_utc
                        AT TIME ZONE 'UTC'),
                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
            'UTF8'),
        pg_catalog.convert_to(
            p_plaintext_retention_budget_seconds::text,
            'UTF8')
    ]
    LOOP
        canonical_preimage := canonical_preimage
            || pg_catalog.int4send(pg_catalog.octet_length(value_bytes))
            || value_bytes;
    END LOOP;
    envelope_fingerprint := pg_catalog.sha256(canonical_preimage);

    alias_lock := pg_catalog.hashtextextended(
        'tip88c1b1:alias:' ||
        p_client_application_id::text || ':' ||
        producer_id_nfc || ':' ||
        capture_agent_instance_id_nfc || ':' ||
        ingress_key::text,
        0);
    exact_lock := pg_catalog.hashtextextended(
        'tip88c1b1:exact:' ||
        p_client_application_id::text || ':' ||
        producer_id_nfc || ':' ||
        p_verification_session_id::text || ':' ||
        p_capture_artifact_id::text || ':' ||
        p_capture_revision::text || ':' ||
        raw_class_nfc,
        0);
    first_lock := LEAST(alias_lock, exact_lock);
    second_lock := GREATEST(alias_lock, exact_lock);
    lock_deadline := pg_catalog.clock_timestamp()
        + pg_catalog.make_interval(
            secs => p_idempotency_lock_timeout_milliseconds / 1000.0);

    LOOP
        acquired := pg_catalog.pg_try_advisory_xact_lock(first_lock);
        EXIT WHEN acquired;
        IF pg_catalog.clock_timestamp() >= lock_deadline THEN
            RAISE EXCEPTION USING
                ERRCODE = '55P03',
                MESSAGE = 'RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
        END IF;
        PERFORM pg_catalog.pg_sleep(0.005);
    END LOOP;

    IF second_lock <> first_lock THEN
        LOOP
            acquired := pg_catalog.pg_try_advisory_xact_lock(second_lock);
            EXIT WHEN acquired;
            IF pg_catalog.clock_timestamp() >= lock_deadline THEN
                RAISE EXCEPTION USING
                    ERRCODE = '55P03',
                    MESSAGE = 'RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY';
            END IF;
            PERFORM pg_catalog.pg_sleep(0.005);
        END LOOP;
    END IF;

    SELECT alias.*
    INTO alias_row
    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
    WHERE alias."ClientApplicationId" = p_client_application_id
      AND alias."ProducerId" = producer_id_nfc
      AND alias."CaptureAgentInstanceId" = capture_agent_instance_id_nfc
      AND alias."IngressIdempotencyKey" = ingress_key;

    alias_found:=FOUND;
    issued_at := pg_catalog.date_trunc(
        'microseconds',
        pg_catalog.clock_timestamp());

    IF retained_binding."ExecutionExpiresAtUtc"<=issued_at
       OR NOT tagekyc.raw_export_retained_snapshot_is_current(p_authority_snapshot_id::uuid,issued_at) THEN
        outcome_code:='SOURCE_RETENTION_NOT_AUTHORIZED'; RETURN NEXT; RETURN;
    END IF;

    IF alias_found THEN
        IF alias_row."AliasState" = 'ConflictTombstone'
           OR alias_row."AttemptedIngressIdentityFingerprint"
                IS DISTINCT FROM ingress_fingerprint THEN
            RAISE EXCEPTION USING
                ERRCODE = 'P0001',
                MESSAGE = 'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
        END IF;

        IF alias_row."ProducerClaimEnvelopeFingerprint"
                IS DISTINCT FROM envelope_fingerprint THEN
            RAISE EXCEPTION USING
                ERRCODE = 'P0001',
                MESSAGE = 'RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID';
        END IF;

        IF alias_row."CurrentClaimEvaluationDisposition" = 'Active'
           AND alias_row."CurrentTokenExpiresAtUtc" > issued_at THEN
            outcome_code :=
                'RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS';
            claim_evaluation_token := NULL;
            token_variant := NULL;
            token_expires_at_utc := NULL;
            claim_evaluation_id := NULL;
            claim_evaluation_revision := NULL;
            claim_evaluation_fence := NULL;
            retry_not_before_utc :=
                alias_row."CurrentTokenExpiresAtUtc";
            RETURN NEXT;
            RETURN;
        END IF;

        evaluation_id := pg_catalog.gen_random_uuid();
        evaluation_revision :=
            alias_row."CurrentClaimEvaluationRevision" + 1;
        evaluation_fence :=
            alias_row."CurrentClaimEvaluationFence" + 1;
        SELECT c.* INTO claim_row FROM tagekyc.raw_export_source_ingress_claims c
        WHERE c."IngressClaimId"=alias_row."IngressClaimId" FOR UPDATE;
        IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID'; END IF;
        IF claim_row."ClaimState"='Reserved' AND alias_row."AliasState" IN ('Evaluating','Bound') THEN
            issued_variant:='ExistingClaimComparisonToken';
        ELSIF claim_row."ClaimState"='ClaimEvaluating' AND alias_row."AliasState"='Evaluating' THEN
            issued_variant:='NewClaimEvaluationToken';
        ELSE
            RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID';
        END IF;
        expires_at := issued_at
            + pg_catalog.make_interval(
                secs => p_claim_evaluation_token_ttl_seconds);
        evaluation_token_bytes := pg_catalog.sha256(
            pg_catalog.uuid_send(pg_catalog.gen_random_uuid())
            || pg_catalog.uuid_send(pg_catalog.gen_random_uuid()));
        evaluation_token := pg_catalog.rtrim(
            pg_catalog.translate(
                pg_catalog.encode(evaluation_token_bytes, 'base64'),
                '+/',
                '-_'),
            '=');
        evaluation_token_digest :=
            pg_catalog.sha256(evaluation_token_bytes);
        previous_context := pg_catalog.current_setting(
            'tagekyc.raw_export_source_ingress_write_context',
            true);
        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:UPDATE',
            true);

        BEGIN
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET
                "CurrentClaimEvaluationId" = evaluation_id,
                "CurrentClaimEvaluationOwnerId" =
                    p_claim_evaluation_owner_id,
                "CurrentClaimEvaluationDisposition" = 'Active',
                "CurrentTokenIssuedAtUtc" = issued_at,
                "CurrentTokenExpiresAtUtc" = expires_at,
                "CurrentTokenSchemaVersion" = 1,
                "CurrentTokenVariant" = issued_variant,
                "CurrentTokenAudience" =
                    'tagekyc.raw-export-source-ingress-claim-comparison',
                "CurrentTokenDigest" = evaluation_token_digest,
                "CurrentClaimEvaluationRevision" =
                    evaluation_revision,
                "CurrentClaimEvaluationFence" = evaluation_fence,
                "LatestIssuedTokenExpiresAtUtc" =
                    GREATEST(
                        "LatestIssuedTokenExpiresAtUtc",
                        expires_at)
            WHERE "IngressClaimAliasId" =
                    alias_row."IngressClaimAliasId"
              AND "CurrentClaimEvaluationId" =
                    alias_row."CurrentClaimEvaluationId"
              AND "CurrentClaimEvaluationRevision" =
                    alias_row."CurrentClaimEvaluationRevision"
              AND "CurrentClaimEvaluationFence" =
                    alias_row."CurrentClaimEvaluationFence"
              AND (
                    "CurrentClaimEvaluationDisposition" <> 'Active'
                    OR "CurrentTokenExpiresAtUtc" <= issued_at);
            GET DIAGNOSTICS rows_changed = ROW_COUNT;
        EXCEPTION WHEN OTHERS THEN
            PERFORM pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                COALESCE(previous_context, ''),
                true);
            RAISE;
        END;

        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            COALESCE(previous_context, ''),
            true);

        IF rows_changed <> 1 THEN
            RAISE EXCEPTION USING
                ERRCODE = 'P0001',
                MESSAGE = 'RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED';
        END IF;
    ELSE
        SELECT claim.*
        INTO claim_row
        FROM tagekyc.raw_export_source_ingress_claims AS claim
        WHERE claim."ClientApplicationId" = p_client_application_id
          AND claim."ProducerId" = producer_id_nfc
          AND claim."VerificationSessionId" =
                p_verification_session_id
          AND claim."CaptureArtifactId" = p_capture_artifact_id
          AND claim."CaptureRevision" = p_capture_revision
          AND claim."RawClass" = raw_class_nfc;

        IF FOUND THEN
            IF claim_row."ClientApplicationId" IS DISTINCT FROM p_client_application_id
               OR claim_row."AuthenticatedPrincipalId" IS DISTINCT FROM p_authenticated_principal_id
               OR claim_row."ProducerId" IS DISTINCT FROM producer_id_nfc
               OR claim_row."CaptureAgentInstanceId" IS DISTINCT FROM capture_agent_instance_id_nfc
               OR claim_row."VerificationSessionId" IS DISTINCT FROM p_verification_session_id
               OR claim_row."CaptureAcceptanceId" IS DISTINCT FROM p_capture_acceptance_id
               OR claim_row."CaptureArtifactId" IS DISTINCT FROM p_capture_artifact_id
               OR claim_row."CaptureRevision" IS DISTINCT FROM p_capture_revision
               OR claim_row."RawClass" IS DISTINCT FROM raw_class_nfc
               OR claim_row."SessionChallengeHash" IS DISTINCT FROM session_challenge_hash_nfc
               OR claim_row."AuthoritySnapshotId" IS DISTINCT FROM authority_snapshot_id_nfc THEN
                RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT';
            END IF;
            issued_variant := 'ExistingClaimComparisonToken';
        ELSE
            new_claim := true;
            claim_row."IngressClaimId" :=
                pg_catalog.gen_random_uuid();
            claim_row."CommitmentKeySelectorId" :=
                key_selector_id_nfc;
            claim_row."CommitmentKeySelectorVersion" :=
                p_commitment_key_selector_version;
            previous_context := pg_catalog.current_setting(
                'tagekyc.raw_export_source_ingress_write_context',
                true);
            PERFORM pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                'claim:INSERT',
                true);

            BEGIN
                INSERT INTO tagekyc.raw_export_source_ingress_claims
                    ("IngressClaimId",
                     "VerificationSessionId",
                     "CaptureAcceptanceId",
                     "CaptureArtifactId",
                     "ClientApplicationId",
                     "AuthenticatedPrincipalId",
                     "ProducerId",
                     "CaptureAgentInstanceId",
                     "CaptureRevision",
                     "RawClass",
                     "SessionChallengeHash",
                     "AuthoritySnapshotId",
                     "IngressIdentityFingerprint",
                     "ClaimState",
                     "CommitmentKeySelectorId",
                     "CommitmentKeySelectorVersion",
                     "CreatedAtUtc")
                VALUES
                    (claim_row."IngressClaimId",
                     p_verification_session_id,
                     p_capture_acceptance_id,
                     p_capture_artifact_id,
                     p_client_application_id,
                     p_authenticated_principal_id,
                     producer_id_nfc,
                     capture_agent_instance_id_nfc,
                     p_capture_revision,
                     raw_class_nfc,
                     session_challenge_hash_nfc,
                     authority_snapshot_id_nfc,
                     ingress_fingerprint,
                     'ClaimEvaluating',
                     key_selector_id_nfc,
                     p_commitment_key_selector_version,
                     issued_at);
            EXCEPTION WHEN OTHERS THEN
                PERFORM pg_catalog.set_config(
                    'tagekyc.raw_export_source_ingress_write_context',
                    COALESCE(previous_context, ''),
                    true);
                RAISE;
            END;

            PERFORM pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                COALESCE(previous_context, ''),
                true);
            issued_variant := 'NewClaimEvaluationToken';
        END IF;

        evaluation_id := pg_catalog.gen_random_uuid();
        evaluation_revision := 1;
        evaluation_fence := 1;
        expires_at := issued_at
            + pg_catalog.make_interval(
                secs => p_claim_evaluation_token_ttl_seconds);
        evaluation_token_bytes := pg_catalog.sha256(
            pg_catalog.uuid_send(pg_catalog.gen_random_uuid())
            || pg_catalog.uuid_send(pg_catalog.gen_random_uuid()));
        evaluation_token := pg_catalog.rtrim(
            pg_catalog.translate(
                pg_catalog.encode(evaluation_token_bytes, 'base64'),
                '+/',
                '-_'),
            '=');
        evaluation_token_digest :=
            pg_catalog.sha256(evaluation_token_bytes);
        previous_context := pg_catalog.current_setting(
            'tagekyc.raw_export_source_ingress_write_context',
            true);
        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:INSERT',
            true);

        BEGIN
            INSERT INTO tagekyc.raw_export_source_ingress_claim_aliases
                ("IngressClaimAliasId",
                 "ClientApplicationId",
                 "ProducerId",
                 "CaptureAgentInstanceId",
                 "IngressIdempotencyKey",
                 "AttemptedIngressIdentityFingerprint",
                 "ProducerClaimEnvelopeFingerprint",
                 "AliasState",
                 "IngressClaimId",
                 "CurrentClaimEvaluationId",
                 "CurrentClaimEvaluationOwnerId",
                 "CurrentClaimEvaluationDisposition",
                 "CurrentTokenIssuedAtUtc",
                 "CurrentTokenExpiresAtUtc",
                 "CurrentTokenSchemaVersion",
                 "CurrentTokenVariant",
                 "CurrentTokenAudience",
                 "CurrentTokenDigest",
                 "CurrentClaimEvaluationRevision",
                 "CurrentClaimEvaluationFence",
                 "LatestIssuedTokenExpiresAtUtc",
                 "CreatedAtUtc")
            VALUES
                (pg_catalog.gen_random_uuid(),
                 p_client_application_id,
                 producer_id_nfc,
                 capture_agent_instance_id_nfc,
                 ingress_key,
                 ingress_fingerprint,
                 envelope_fingerprint,
                 'Evaluating',
                 claim_row."IngressClaimId",
                 evaluation_id,
                 p_claim_evaluation_owner_id,
                 'Active',
                 issued_at,
                 expires_at,
                 1,
                 issued_variant,
                 'tagekyc.raw-export-source-ingress-claim-comparison',
                 evaluation_token_digest,
                 evaluation_revision,
                 evaluation_fence,
                 expires_at,
                 issued_at);
        EXCEPTION WHEN OTHERS THEN
            PERFORM pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                COALESCE(previous_context, ''),
                true);
            RAISE;
        END;

        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            COALESCE(previous_context, ''),
            true);
    END IF;

    SELECT a."AttemptedIngressIdentityFingerprint",a."ProducerClaimEnvelopeFingerprint",
      c."IngressIdentityFingerprint",c."CommitmentKeySelectorId",c."CommitmentKeySelectorVersion"
    INTO attempted_identity_fingerprint,producer_envelope_fingerprint,canonical_ingress_identity_fingerprint,
      comparison_commitment_selector_id,comparison_commitment_selector_version
    FROM tagekyc.raw_export_source_ingress_claim_aliases a
    JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=a."IngressClaimId"
    WHERE a."ClientApplicationId"=p_client_application_id AND a."ProducerId"=producer_id_nfc
     AND a."CaptureAgentInstanceId"=capture_agent_instance_id_nfc AND a."IngressIdempotencyKey"=ingress_key;
    IF NOT FOUND OR octet_length(attempted_identity_fingerprint)<>32 OR octet_length(producer_envelope_fingerprint)<>32
       OR octet_length(canonical_ingress_identity_fingerprint)<>32 OR comparison_commitment_selector_id IS NULL
       OR comparison_commitment_selector_id='' OR comparison_commitment_selector_version IS NULL OR comparison_commitment_selector_version<1 THEN
        RAISE EXCEPTION 'RAW_EXPORT_SOURCE_BINDING_INVALID';
    END IF;
    outcome_code := NULL;
    claim_evaluation_token := evaluation_token;
    token_variant := issued_variant;
    token_expires_at_utc := expires_at;
    claim_evaluation_id := evaluation_id;
    claim_evaluation_revision := evaluation_revision;
    claim_evaluation_fence := evaluation_fence;
    retry_not_before_utc := NULL;
    RETURN NEXT;
END;
$$;
ALTER FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint) TO tagekyc_raw_export_claim_broker;
""";

    private const string RetainedCompletionOperation = """
CREATE OR REPLACE FUNCTION
    tagekyc.complete_raw_export_source_ingress_claim(
        p_client_application_id uuid,
        p_producer_id text,
        p_capture_agent_instance_id text,
        p_ingress_idempotency_key text,
        p_claim_evaluation_id uuid,
        p_claim_evaluation_revision bigint,
        p_claim_evaluation_fence bigint,
        p_token_variant text,
        p_token_expires_at_utc timestamptz,
        p_claim_evaluation_token text,
        p_producer_envelope_fingerprint bytea,
        p_commitment_schema integer,
        p_commitment_key_id text,
        p_commitment_key_version integer,
        p_content_commitment bytea,
        p_subject_token_schema integer,
        p_subject_token_key_id text,
        p_subject_token_key_version integer,
        p_subject_token bytea,
        p_claimed_plaintext_length bigint,
        p_media_type text,
        p_captured_at_utc timestamptz,
        p_retention_started_at_utc timestamptz,
        p_retention_expires_at_utc timestamptz,
        p_retention_budget_seconds integer,
        p_storage_profile_id text,
        p_source_profile_id text,
        p_source_profile_version integer,
        p_encryption_suite_id text,
        p_encryption_framing_version integer,
        p_nonce_strategy_id text,
        p_nonce_seed_commitment bytea,
        p_chunk_size integer,
        p_framing_parameters_digest bytea,
        p_key_provider_id text,
        p_kek_id text,
        p_kek_version integer,
        p_kek_fingerprint text,
        p_max_continuation_seconds integer,
        p_attempt_deadline_seconds integer,
        p_safety_margin_milliseconds integer,
        p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text, "SourceArtifactId" uuid)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
    claim_row tagekyc.raw_export_source_ingress_claims%ROWTYPE;
    canonical_reservation tagekyc.raw_export_source_reservations%ROWTYPE;
    authority record;
    consent record;
    now_utc timestamptz := pg_catalog.statement_timestamp();
    effective_expires timestamptz;
    reservation_expires timestamptz;
    source_id uuid := pg_catalog.gen_random_uuid();
    attempt_id uuid := pg_catalog.gen_random_uuid();
    object_id uuid := pg_catalog.gen_random_uuid();
    key_reservation_id uuid := pg_catalog.gen_random_uuid();
    admission bytea;
    reservation_fingerprint bytea;
    attempt_fingerprint bytea;
    canonical_timestamp text;
    retention_probe record; retention_locked record; retention_requirement text;
BEGIN
    SELECT c."IngressClaimId", c."ClientApplicationId", c."VerificationSessionId",
      c."CaptureAcceptanceId", c."RawClass", c."AuthenticatedPrincipalId", c."AuthoritySnapshotId",
      s."AuthorityKind", s."CustodyPrincipalId", s."RuntimeBindingId", s."ConsentBindingId",
      s."ConsentPolicyId", s."ConsentPolicyVersion", b."ExecutionExpiresAtUtc",
      h."ConsentReferenceId", h."ExternalConsentArtifactRef"
    INTO retention_probe
    FROM tagekyc.raw_export_source_ingress_claim_aliases a
    JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=a."IngressClaimId"
    JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"::text=pg_catalog.lower(c."AuthoritySnapshotId")
    LEFT JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
    LEFT JOIN tagekyc.raw_source_consent_bindings cb ON cb."ConsentBindingId"=s."ConsentBindingId"
    LEFT JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=cb."ConsentReferenceId"
    WHERE a."ClientApplicationId"=p_client_application_id
     AND a."ProducerId"=pg_catalog.normalize(p_producer_id,'NFC')
     AND a."CaptureAgentInstanceId"=pg_catalog.normalize(p_capture_agent_instance_id,'NFC')
     AND pg_catalog.replace(a."IngressIdempotencyKey"::text,'-','')=p_ingress_idempotency_key;
    IF retention_probe."AuthorityKind"='SourceRetention' THEN
        IF retention_probe."ConsentReferenceId" IS NULL
           OR retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
           OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM retention_probe."AuthenticatedPrincipalId" THEN
            RETURN QUERY SELECT 'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid; RETURN;
        END IF;
        PERFORM 1 FROM tagekyc.verification_sessions vs
        WHERE vs."Id"=retention_probe."VerificationSessionId"
         AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
        IF NOT FOUND THEN RETURN QUERY SELECT 'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid; RETURN; END IF;
        PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
          'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
        PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
        PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
          'tip88b1:lifecycle:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text));
        FOR retention_requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
          WHERE req."PolicyId"=retention_probe."ConsentPolicyId" AND req."PolicyVersion"=retention_probe."ConsentPolicyVersion"
           AND req."RequirementType"<>'ConsentArtifact' ORDER BY req."RequirementType" COLLATE "C" LOOP
            PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
              'tip88b1:fulfillment:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text||':'||retention_requirement));
        END LOOP;
        PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
          'tip88c1:b2-authority:'||retention_probe."ClientApplicationId"::text||':'||
          retention_probe."VerificationSessionId"::text||':'||retention_probe."CaptureAcceptanceId"::text||':'||retention_probe."RawClass"));
    END IF;
    IF NOT tagekyc.validate_raw_export_claim_evaluation_token(
        p_client_application_id,p_producer_id,
        p_capture_agent_instance_id,p_ingress_idempotency_key,
        p_claim_evaluation_id,p_claim_evaluation_revision,
        p_claim_evaluation_fence,p_token_variant,
        p_token_expires_at_utc,p_claim_evaluation_token) THEN
        RETURN QUERY SELECT 'CLAIM_TOKEN_INVALID'::text, NULL::uuid;
        RETURN;
    END IF;

    SELECT alias.* INTO alias_row
    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
    WHERE alias."ClientApplicationId" = p_client_application_id
      AND alias."ProducerId" =
            pg_catalog.normalize(p_producer_id, 'NFC')
      AND alias."CaptureAgentInstanceId" =
            pg_catalog.normalize(
                p_capture_agent_instance_id, 'NFC')
      AND alias."IngressIdempotencyKey" =
            p_ingress_idempotency_key::uuid
    FOR UPDATE;
    SELECT claim.* INTO claim_row
    FROM tagekyc.raw_export_source_ingress_claims AS claim
    WHERE claim."IngressClaimId" = alias_row."IngressClaimId"
    FOR UPDATE;

    IF p_token_variant = 'NewClaimEvaluationToken' THEN
        IF alias_row."AliasState" <> 'Evaluating'
           OR claim_row."ClaimState" <> 'ClaimEvaluating'
           OR alias_row."ProducerClaimEnvelopeFingerprint"
                IS DISTINCT FROM p_producer_envelope_fingerprint
           OR p_content_commitment IS NULL THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;
    ELSIF p_token_variant = 'ExistingClaimComparisonToken' THEN
        IF alias_row."AliasState" NOT IN ('Evaluating','Bound')
           OR claim_row."ClaimState" <> 'Reserved'
           OR alias_row."ProducerClaimEnvelopeFingerprint"
                IS DISTINCT FROM p_producer_envelope_fingerprint THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;
    ELSE
        RETURN QUERY SELECT
            'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
        RETURN;
    END IF;

    SELECT c."IngressClaimId", c."ClientApplicationId", c."VerificationSessionId",
      c."CaptureAcceptanceId", c."RawClass", c."AuthenticatedPrincipalId", c."AuthoritySnapshotId",
      s."AuthorityKind", s."CustodyPrincipalId", s."RuntimeBindingId", s."ConsentBindingId",
      s."ConsentPolicyId", s."ConsentPolicyVersion", b."ExecutionExpiresAtUtc",
      h."ConsentReferenceId", h."ExternalConsentArtifactRef"
    INTO retention_locked
    FROM tagekyc.raw_export_source_ingress_claim_aliases a
    JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=a."IngressClaimId"
    JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"::text=pg_catalog.lower(c."AuthoritySnapshotId")
    LEFT JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
    LEFT JOIN tagekyc.raw_source_consent_bindings cb ON cb."ConsentBindingId"=s."ConsentBindingId"
    LEFT JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=cb."ConsentReferenceId"
    WHERE a."ClientApplicationId"=p_client_application_id
     AND a."ProducerId"=pg_catalog.normalize(p_producer_id,'NFC')
     AND a."CaptureAgentInstanceId"=pg_catalog.normalize(p_capture_agent_instance_id,'NFC')
     AND pg_catalog.replace(a."IngressIdempotencyKey"::text,'-','')=p_ingress_idempotency_key;
    IF retention_probe."AuthorityKind"='SourceRetention' OR retention_locked."AuthorityKind"='SourceRetention' THEN
        IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked) THEN
            RETURN QUERY SELECT 'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid; RETURN;
        END IF;
        now_utc:=pg_catalog.clock_timestamp();
        IF alias_row."CurrentTokenExpiresAtUtc"<=now_utc THEN
            RETURN QUERY SELECT 'CLAIM_TOKEN_INVALID'::text,NULL::uuid; RETURN;
        END IF;
    END IF;

    SELECT * INTO authority
    FROM tagekyc.raw_export_resolve_current_authority_for_source(
        p_client_application_id,
        claim_row."VerificationSessionId",
        claim_row."CaptureAcceptanceId",
        claim_row."RawClass",
        now_utc);
    IF NOT FOUND
       OR authority."ApprovedPurpose" <>
            (CASE WHEN retention_locked."AuthorityKind"='SourceRetention' THEN 'SourceRetention' ELSE 'SubjectRawBiometricExport' END)
       OR pg_catalog.lower(claim_row."AuthoritySnapshotId")
            <> authority."AuthoritySnapshotId"::text THEN
        RETURN QUERY SELECT
            'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
        RETURN;
    END IF;

    IF retention_locked."AuthorityKind"='SourceRetention' THEN
        IF NOT tagekyc.raw_export_retained_snapshot_is_current(authority."AuthoritySnapshotId",now_utc)
           OR retention_locked."ExecutionExpiresAtUtc"<=now_utc THEN
            RETURN QUERY SELECT 'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid; RETURN;
        END IF;
    ELSE
        SELECT * INTO consent
        FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
            claim_row."VerificationSessionId",
            authority."ConsentPolicyId",
            authority."ConsentPolicyVersion")
        WHERE "RawClass" = claim_row."RawClass"
        LIMIT 1;
        IF NOT FOUND
           OR consent."State" <> 'Effective'
           OR consent."PurposeCode" <>
                'SubjectRawBiometricExport' THEN
            RETURN QUERY SELECT
                'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
            RETURN;
        END IF;
    
    END IF;

    IF p_token_variant = 'ExistingClaimComparisonToken'
       AND p_content_commitment IS NULL THEN
        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE'::text,
            NULL::uuid;
        RETURN;
    END IF;

    admission := tagekyc.raw_export_c1_hash_canonical(
        'tip-88c1-ingress-admission-v1',
        pg_catalog.encode(
            claim_row."IngressIdentityFingerprint",'hex'),
        p_commitment_schema::text,p_commitment_key_id,
        p_commitment_key_version::text,
        pg_catalog.encode(p_content_commitment,'hex'),
        p_media_type,
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_captured_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_retention_started_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_retention_expires_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        p_retention_budget_seconds::text);
    IF p_token_variant = 'ExistingClaimComparisonToken' THEN
        SELECT reservation.* INTO canonical_reservation
        FROM tagekyc.raw_export_source_reservations AS reservation
        WHERE reservation."IngressClaimId" =
                claim_row."IngressClaimId";

        IF NOT FOUND THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;

        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:UPDATE',
            true);

        IF canonical_reservation."AdmissionFingerprint"
                IS NOT DISTINCT FROM admission THEN
            IF alias_row."AliasState" = 'Evaluating' THEN
                UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                SET "AliasState" = 'Bound'
                WHERE "IngressClaimAliasId" =
                        alias_row."IngressClaimAliasId"
                  AND "AliasState" = 'Evaluating'
                  AND "CurrentClaimEvaluationDisposition" = 'Active'
                  AND "CurrentClaimEvaluationRevision" =
                        p_claim_evaluation_revision
                  AND "CurrentClaimEvaluationFence" =
                        p_claim_evaluation_fence;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                END IF;
            END IF;

            RETURN QUERY SELECT
                'ExistingMatch'::text,
                canonical_reservation."SourceArtifactId";
            RETURN;
        END IF;

        IF alias_row."AliasState" = 'Evaluating' THEN
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "AliasState" = 'ConflictTombstone',
                "CurrentClaimEvaluationDisposition" = 'Conflict'
            WHERE "IngressClaimAliasId" =
                    alias_row."IngressClaimAliasId"
              AND "AliasState" = 'Evaluating'
              AND "CurrentClaimEvaluationDisposition" = 'Active'
              AND "CurrentClaimEvaluationRevision" =
                    p_claim_evaluation_revision
              AND "CurrentClaimEvaluationFence" =
                    p_claim_evaluation_fence;
        ELSE
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "CurrentClaimEvaluationDisposition" = 'Conflict'
            WHERE "IngressClaimAliasId" =
                    alias_row."IngressClaimAliasId"
              AND "AliasState" = 'Bound'
              AND "CurrentClaimEvaluationDisposition" = 'Active'
              AND "CurrentClaimEvaluationRevision" =
                    p_claim_evaluation_revision
              AND "CurrentClaimEvaluationFence" =
                    p_claim_evaluation_fence;
        END IF;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
        END IF;

        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT'::text,
            NULL::uuid;
        RETURN;
    END IF;

    effective_expires := LEAST(
        p_retention_expires_at_utc,
        now_utc +
            pg_catalog.make_interval(
                secs => p_max_continuation_seconds));
    IF retention_locked."AuthorityKind"='SourceRetention' THEN
        effective_expires:=LEAST(effective_expires,authority."ValidUntilUtc",
            authority."AbsoluteSourceExpiresAtUtc",retention_locked."ExecutionExpiresAtUtc");
    END IF;
    IF effective_expires - now_utc <=
        pg_catalog.make_interval(
            secs => p_attempt_deadline_seconds)
        + p_safety_margin_milliseconds
            * interval '1 millisecond' THEN
        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:UPDATE',
            true);
        UPDATE tagekyc.raw_export_source_ingress_claim_aliases
        SET "CurrentClaimEvaluationDisposition" = 'Completed'
        WHERE "IngressClaimAliasId" =
                alias_row."IngressClaimAliasId"
          AND "CurrentClaimEvaluationDisposition" = 'Active'
          AND "CurrentClaimEvaluationRevision" =
                p_claim_evaluation_revision
          AND "CurrentClaimEvaluationFence" =
                p_claim_evaluation_fence;
        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID'::text,
            NULL::uuid;
        RETURN;
    END IF;

    reservation_fingerprint :=
        tagekyc.raw_export_c1_hash_canonical(
            'tip-88c1-source-reservation-v2',
            pg_catalog.replace(source_id::text,'-',''),
            pg_catalog.encode(admission,'hex'),
            p_subject_token_schema::text,
            p_subject_token_key_id,
            p_subject_token_key_version::text,
            pg_catalog.encode(p_subject_token,'hex'),
            authority."AuthoritySnapshotSchemaVersion"::text,
            pg_catalog.replace(
                authority."AuthoritySnapshotId"::text,'-',''),
            pg_catalog.to_char(
                pg_catalog.date_trunc(
                    'microseconds',
                    authority."AbsoluteSourceExpiresAtUtc"
                        AT TIME ZONE 'UTC'),
                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
            p_storage_profile_id,p_source_profile_id,
            p_source_profile_version::text);
    attempt_fingerprint :=
        tagekyc.raw_export_c1_hash_canonical(
            'tip-88c1-encryption-attempt-v1',
            pg_catalog.encode(
                reservation_fingerprint,'hex'),
            '1','1',
            pg_catalog.replace(object_id::text,'-',''),
            pg_catalog.replace(
                key_reservation_id::text,'-',''),
            p_encryption_suite_id,
            p_encryption_framing_version::text,
            p_key_provider_id,p_kek_id,p_kek_version::text,
            p_kek_fingerprint,p_nonce_strategy_id,
            pg_catalog.encode(
                p_nonce_seed_commitment,'hex'),
            p_chunk_size::text,
            pg_catalog.encode(
                p_framing_parameters_digest,'hex'));
    reservation_expires := LEAST(
        now_utc + pg_catalog.make_interval(
            secs => p_ownership_lease_seconds),
        effective_expires,
        authority."AbsoluteSourceExpiresAtUtc");

    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_core_write_context',
        'complete-r1',
        true);
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_ingress_write_context',
        'alias:UPDATE',
        true);

    INSERT INTO tagekyc.raw_export_source_reservations
        (
            "SourceArtifactId","IngressClaimId",
            "AuthoritySnapshotSchemaVersion",
            "AuthoritySnapshotId",
            "SubjectRefTokenSchemaVersion",
            "SubjectRefTokenKeyId","SubjectRefTokenKeyVersion",
            "SubjectRefToken","StorageProfileId",
            "SourceEncryptionProfileId",
            "SourceEncryptionProfileVersion",
            "AbsoluteSourceExpiresAtUtc",
            "AdmissionFingerprint",
            "EffectivePlaintextRetentionExpiresAtUtc",
            "ReservationExpiresAtUtc",
            "SourceReservationFingerprint",
            "ContentCommitmentSchemaVersion",
            "ContentCommitmentKeyId",
            "ContentCommitmentKeyVersion",
            "ContentCommitment","ClaimedPlaintextLength",
            "MediaType","CapturedAtUtc",
            "PlaintextRetentionStartedAtUtc",
            "PlaintextRetentionExpiresAtUtc",
            "PlaintextRetentionBudgetSeconds",
            "ControllerIdentity","StableDataScopeId",
            "ConsentPolicyId","ConsentPolicyVersion",
            "SchemaVersion","CreatedAtUtc")
    VALUES
        (
            source_id,claim_row."IngressClaimId",
            authority."AuthoritySnapshotSchemaVersion",
            authority."AuthoritySnapshotId",
            p_subject_token_schema,p_subject_token_key_id,
            p_subject_token_key_version,p_subject_token,
            p_storage_profile_id,p_source_profile_id,
            p_source_profile_version,
            authority."AbsoluteSourceExpiresAtUtc",admission,
            effective_expires,reservation_expires,
            reservation_fingerprint,p_commitment_schema,
            p_commitment_key_id,p_commitment_key_version,
            p_content_commitment,p_claimed_plaintext_length,
            p_media_type,p_captured_at_utc,
            p_retention_started_at_utc,
            p_retention_expires_at_utc,
            p_retention_budget_seconds,
            authority."ControllerIdentity",
            authority."StableDataScopeId",
            authority."ConsentPolicyId",
            authority."ConsentPolicyVersion",1,now_utc);
    INSERT INTO tagekyc.raw_export_source_encryption_attempts
        (
            "AttemptId","SourceArtifactId",
            "EncryptionAttemptRevision","Fence",
            "ProvisionalObjectIdentity",
            "AttemptKeyReservationId","KeyProviderId",
            "KekId","KekVersion","KekFingerprint",
            "EncryptionSuiteId","EncryptionFramingVersion",
            "NonceStrategyId",
            "NonceDerivationSeedReferenceOrWrappedSeed",
            "NonceDerivationSeedCommitment","ChunkSize",
            "FramingParametersDigest",
            "EncryptionAttemptFingerprint",
            "OwnershipLeaseExpiresAtUtc",
            "R2TerminationDisposition","CreatedAtUtc",
            "SchemaVersion")
    VALUES
        (
            attempt_id,source_id,1,1,object_id,
            key_reservation_id,p_key_provider_id,p_kek_id,
            p_kek_version,p_kek_fingerprint,
            p_encryption_suite_id,
            p_encryption_framing_version,
            p_nonce_strategy_id,'none',
            p_nonce_seed_commitment,p_chunk_size,
            p_framing_parameters_digest,attempt_fingerprint,
            CASE WHEN retention_locked."AuthorityKind"='SourceRetention' THEN reservation_expires
                 ELSE now_utc + pg_catalog.make_interval(secs => p_ownership_lease_seconds) END,
            NULL,now_utc,1);
    INSERT INTO tagekyc.raw_export_source_head
        (
            "SourceArtifactId","CustodyState",
            "CurrentEncryptionAttemptId",
            "ReservationRevision","Fence")
    VALUES (source_id,'Reserved',attempt_id,1,1);

    UPDATE tagekyc.raw_export_source_ingress_claim_aliases
    SET "AliasState" = 'Bound',
        "CurrentClaimEvaluationDisposition" = 'Completed'
    WHERE "IngressClaimAliasId" =
            alias_row."IngressClaimAliasId"
      AND "CurrentClaimEvaluationDisposition" = 'Active'
      AND "CurrentClaimEvaluationRevision" =
            p_claim_evaluation_revision
      AND "CurrentClaimEvaluationFence" =
            p_claim_evaluation_fence;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
    END IF;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_ingress_write_context',
        'claim:UPDATE',
        true);
    UPDATE tagekyc.raw_export_source_ingress_claims
    SET "ClaimState" = 'Reserved'
    WHERE "IngressClaimId" = claim_row."IngressClaimId"
      AND "ClaimState" = 'ClaimEvaluating';
    IF NOT FOUND THEN
        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
    END IF;

    RETURN QUERY SELECT 'NewReservation'::text,source_id;
END;
$$;
""";

    private const string RetainedCompletionRestore = """
CREATE OR REPLACE FUNCTION
    tagekyc.complete_raw_export_source_ingress_claim(
        p_client_application_id uuid,
        p_producer_id text,
        p_capture_agent_instance_id text,
        p_ingress_idempotency_key text,
        p_claim_evaluation_id uuid,
        p_claim_evaluation_revision bigint,
        p_claim_evaluation_fence bigint,
        p_token_variant text,
        p_token_expires_at_utc timestamptz,
        p_claim_evaluation_token text,
        p_producer_envelope_fingerprint bytea,
        p_commitment_schema integer,
        p_commitment_key_id text,
        p_commitment_key_version integer,
        p_content_commitment bytea,
        p_subject_token_schema integer,
        p_subject_token_key_id text,
        p_subject_token_key_version integer,
        p_subject_token bytea,
        p_claimed_plaintext_length bigint,
        p_media_type text,
        p_captured_at_utc timestamptz,
        p_retention_started_at_utc timestamptz,
        p_retention_expires_at_utc timestamptz,
        p_retention_budget_seconds integer,
        p_storage_profile_id text,
        p_source_profile_id text,
        p_source_profile_version integer,
        p_encryption_suite_id text,
        p_encryption_framing_version integer,
        p_nonce_strategy_id text,
        p_nonce_seed_commitment bytea,
        p_chunk_size integer,
        p_framing_parameters_digest bytea,
        p_key_provider_id text,
        p_kek_id text,
        p_kek_version integer,
        p_kek_fingerprint text,
        p_max_continuation_seconds integer,
        p_attempt_deadline_seconds integer,
        p_safety_margin_milliseconds integer,
        p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text, "SourceArtifactId" uuid)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    alias_row tagekyc.raw_export_source_ingress_claim_aliases%ROWTYPE;
    claim_row tagekyc.raw_export_source_ingress_claims%ROWTYPE;
    canonical_reservation tagekyc.raw_export_source_reservations%ROWTYPE;
    authority record;
    consent record;
    now_utc timestamptz := pg_catalog.statement_timestamp();
    effective_expires timestamptz;
    reservation_expires timestamptz;
    source_id uuid := pg_catalog.gen_random_uuid();
    attempt_id uuid := pg_catalog.gen_random_uuid();
    object_id uuid := pg_catalog.gen_random_uuid();
    key_reservation_id uuid := pg_catalog.gen_random_uuid();
    admission bytea;
    reservation_fingerprint bytea;
    attempt_fingerprint bytea;
    canonical_timestamp text;
BEGIN
    IF NOT tagekyc.validate_raw_export_claim_evaluation_token(
        p_client_application_id,p_producer_id,
        p_capture_agent_instance_id,p_ingress_idempotency_key,
        p_claim_evaluation_id,p_claim_evaluation_revision,
        p_claim_evaluation_fence,p_token_variant,
        p_token_expires_at_utc,p_claim_evaluation_token) THEN
        RETURN QUERY SELECT 'CLAIM_TOKEN_INVALID'::text, NULL::uuid;
        RETURN;
    END IF;

    SELECT alias.* INTO alias_row
    FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
    WHERE alias."ClientApplicationId" = p_client_application_id
      AND alias."ProducerId" =
            pg_catalog.normalize(p_producer_id, 'NFC')
      AND alias."CaptureAgentInstanceId" =
            pg_catalog.normalize(
                p_capture_agent_instance_id, 'NFC')
      AND alias."IngressIdempotencyKey" =
            p_ingress_idempotency_key::uuid
    FOR UPDATE;
    SELECT claim.* INTO claim_row
    FROM tagekyc.raw_export_source_ingress_claims AS claim
    WHERE claim."IngressClaimId" = alias_row."IngressClaimId"
    FOR UPDATE;

    IF p_token_variant = 'NewClaimEvaluationToken' THEN
        IF alias_row."AliasState" <> 'Evaluating'
           OR claim_row."ClaimState" <> 'ClaimEvaluating'
           OR alias_row."ProducerClaimEnvelopeFingerprint"
                IS DISTINCT FROM p_producer_envelope_fingerprint
           OR p_content_commitment IS NULL THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;
    ELSIF p_token_variant = 'ExistingClaimComparisonToken' THEN
        IF alias_row."AliasState" NOT IN ('Evaluating','Bound')
           OR claim_row."ClaimState" <> 'Reserved'
           OR alias_row."ProducerClaimEnvelopeFingerprint"
                IS DISTINCT FROM p_producer_envelope_fingerprint THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;
    ELSE
        RETURN QUERY SELECT
            'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
        RETURN;
    END IF;

    SELECT * INTO authority
    FROM tagekyc.raw_export_resolve_current_authority_for_source(
        p_client_application_id,
        claim_row."VerificationSessionId",
        claim_row."CaptureAcceptanceId",
        claim_row."RawClass",
        now_utc);
    IF NOT FOUND
       OR authority."ApprovedPurpose" <>
            'SubjectRawBiometricExport'
       OR pg_catalog.lower(claim_row."AuthoritySnapshotId")
            <> authority."AuthoritySnapshotId"::text THEN
        RETURN QUERY SELECT
            'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
        RETURN;
    END IF;

    SELECT * INTO consent
    FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
        claim_row."VerificationSessionId",
        authority."ConsentPolicyId",
        authority."ConsentPolicyVersion")
    WHERE "RawClass" = claim_row."RawClass"
    LIMIT 1;
    IF NOT FOUND
       OR consent."State" <> 'Effective'
       OR consent."PurposeCode" <>
            'SubjectRawBiometricExport' THEN
        RETURN QUERY SELECT
            'SOURCE_RETENTION_NOT_AUTHORIZED'::text,NULL::uuid;
        RETURN;
    END IF;

    IF p_token_variant = 'ExistingClaimComparisonToken'
       AND p_content_commitment IS NULL THEN
        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE'::text,
            NULL::uuid;
        RETURN;
    END IF;

    admission := tagekyc.raw_export_c1_hash_canonical(
        'tip-88c1-ingress-admission-v1',
        pg_catalog.encode(
            claim_row."IngressIdentityFingerprint",'hex'),
        p_commitment_schema::text,p_commitment_key_id,
        p_commitment_key_version::text,
        pg_catalog.encode(p_content_commitment,'hex'),
        p_media_type,
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_captured_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_retention_started_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        pg_catalog.to_char(
            pg_catalog.date_trunc(
                'microseconds',
                p_retention_expires_at_utc AT TIME ZONE 'UTC'),
            'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
        p_retention_budget_seconds::text);
    IF p_token_variant = 'ExistingClaimComparisonToken' THEN
        SELECT reservation.* INTO canonical_reservation
        FROM tagekyc.raw_export_source_reservations AS reservation
        WHERE reservation."IngressClaimId" =
                claim_row."IngressClaimId";

        IF NOT FOUND THEN
            RETURN QUERY SELECT
                'CLAIM_TOKEN_INVALID'::text,NULL::uuid;
            RETURN;
        END IF;

        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:UPDATE',
            true);

        IF canonical_reservation."AdmissionFingerprint"
                IS NOT DISTINCT FROM admission THEN
            IF alias_row."AliasState" = 'Evaluating' THEN
                UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                SET "AliasState" = 'Bound'
                WHERE "IngressClaimAliasId" =
                        alias_row."IngressClaimAliasId"
                  AND "AliasState" = 'Evaluating'
                  AND "CurrentClaimEvaluationDisposition" = 'Active'
                  AND "CurrentClaimEvaluationRevision" =
                        p_claim_evaluation_revision
                  AND "CurrentClaimEvaluationFence" =
                        p_claim_evaluation_fence;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
                END IF;
            END IF;

            RETURN QUERY SELECT
                'ExistingMatch'::text,
                canonical_reservation."SourceArtifactId";
            RETURN;
        END IF;

        IF alias_row."AliasState" = 'Evaluating' THEN
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "AliasState" = 'ConflictTombstone',
                "CurrentClaimEvaluationDisposition" = 'Conflict'
            WHERE "IngressClaimAliasId" =
                    alias_row."IngressClaimAliasId"
              AND "AliasState" = 'Evaluating'
              AND "CurrentClaimEvaluationDisposition" = 'Active'
              AND "CurrentClaimEvaluationRevision" =
                    p_claim_evaluation_revision
              AND "CurrentClaimEvaluationFence" =
                    p_claim_evaluation_fence;
        ELSE
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "CurrentClaimEvaluationDisposition" = 'Conflict'
            WHERE "IngressClaimAliasId" =
                    alias_row."IngressClaimAliasId"
              AND "AliasState" = 'Bound'
              AND "CurrentClaimEvaluationDisposition" = 'Active'
              AND "CurrentClaimEvaluationRevision" =
                    p_claim_evaluation_revision
              AND "CurrentClaimEvaluationFence" =
                    p_claim_evaluation_fence;
        END IF;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
        END IF;

        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT'::text,
            NULL::uuid;
        RETURN;
    END IF;

    effective_expires := LEAST(
        p_retention_expires_at_utc,
        now_utc +
            pg_catalog.make_interval(
                secs => p_max_continuation_seconds));
    IF effective_expires - now_utc <
        pg_catalog.make_interval(
            secs => p_attempt_deadline_seconds)
        + p_safety_margin_milliseconds
            * interval '1 millisecond' THEN
        PERFORM pg_catalog.set_config(
            'tagekyc.raw_export_source_ingress_write_context',
            'alias:UPDATE',
            true);
        UPDATE tagekyc.raw_export_source_ingress_claim_aliases
        SET "CurrentClaimEvaluationDisposition" = 'Completed'
        WHERE "IngressClaimAliasId" =
                alias_row."IngressClaimAliasId"
          AND "CurrentClaimEvaluationDisposition" = 'Active'
          AND "CurrentClaimEvaluationRevision" =
                p_claim_evaluation_revision
          AND "CurrentClaimEvaluationFence" =
                p_claim_evaluation_fence;
        RETURN QUERY SELECT
            'RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID'::text,
            NULL::uuid;
        RETURN;
    END IF;

    reservation_fingerprint :=
        tagekyc.raw_export_c1_hash_canonical(
            'tip-88c1-source-reservation-v2',
            pg_catalog.replace(source_id::text,'-',''),
            pg_catalog.encode(admission,'hex'),
            p_subject_token_schema::text,
            p_subject_token_key_id,
            p_subject_token_key_version::text,
            pg_catalog.encode(p_subject_token,'hex'),
            authority."AuthoritySnapshotSchemaVersion"::text,
            pg_catalog.replace(
                authority."AuthoritySnapshotId"::text,'-',''),
            pg_catalog.to_char(
                pg_catalog.date_trunc(
                    'microseconds',
                    authority."AbsoluteSourceExpiresAtUtc"
                        AT TIME ZONE 'UTC'),
                'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
            p_storage_profile_id,p_source_profile_id,
            p_source_profile_version::text);
    attempt_fingerprint :=
        tagekyc.raw_export_c1_hash_canonical(
            'tip-88c1-encryption-attempt-v1',
            pg_catalog.encode(
                reservation_fingerprint,'hex'),
            '1','1',
            pg_catalog.replace(object_id::text,'-',''),
            pg_catalog.replace(
                key_reservation_id::text,'-',''),
            p_encryption_suite_id,
            p_encryption_framing_version::text,
            p_key_provider_id,p_kek_id,p_kek_version::text,
            p_kek_fingerprint,p_nonce_strategy_id,
            pg_catalog.encode(
                p_nonce_seed_commitment,'hex'),
            p_chunk_size::text,
            pg_catalog.encode(
                p_framing_parameters_digest,'hex'));
    reservation_expires := LEAST(
        now_utc + pg_catalog.make_interval(
            secs => p_ownership_lease_seconds),
        effective_expires,
        authority."AbsoluteSourceExpiresAtUtc");

    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_core_write_context',
        'complete-r1',
        true);
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_ingress_write_context',
        'alias:UPDATE',
        true);

    INSERT INTO tagekyc.raw_export_source_reservations
        (
            "SourceArtifactId","IngressClaimId",
            "AuthoritySnapshotSchemaVersion",
            "AuthoritySnapshotId",
            "SubjectRefTokenSchemaVersion",
            "SubjectRefTokenKeyId","SubjectRefTokenKeyVersion",
            "SubjectRefToken","StorageProfileId",
            "SourceEncryptionProfileId",
            "SourceEncryptionProfileVersion",
            "AbsoluteSourceExpiresAtUtc",
            "AdmissionFingerprint",
            "EffectivePlaintextRetentionExpiresAtUtc",
            "ReservationExpiresAtUtc",
            "SourceReservationFingerprint",
            "ContentCommitmentSchemaVersion",
            "ContentCommitmentKeyId",
            "ContentCommitmentKeyVersion",
            "ContentCommitment","ClaimedPlaintextLength",
            "MediaType","CapturedAtUtc",
            "PlaintextRetentionStartedAtUtc",
            "PlaintextRetentionExpiresAtUtc",
            "PlaintextRetentionBudgetSeconds",
            "ControllerIdentity","StableDataScopeId",
            "ConsentPolicyId","ConsentPolicyVersion",
            "SchemaVersion","CreatedAtUtc")
    VALUES
        (
            source_id,claim_row."IngressClaimId",
            authority."AuthoritySnapshotSchemaVersion",
            authority."AuthoritySnapshotId",
            p_subject_token_schema,p_subject_token_key_id,
            p_subject_token_key_version,p_subject_token,
            p_storage_profile_id,p_source_profile_id,
            p_source_profile_version,
            authority."AbsoluteSourceExpiresAtUtc",admission,
            effective_expires,reservation_expires,
            reservation_fingerprint,p_commitment_schema,
            p_commitment_key_id,p_commitment_key_version,
            p_content_commitment,p_claimed_plaintext_length,
            p_media_type,p_captured_at_utc,
            p_retention_started_at_utc,
            p_retention_expires_at_utc,
            p_retention_budget_seconds,
            authority."ControllerIdentity",
            authority."StableDataScopeId",
            authority."ConsentPolicyId",
            authority."ConsentPolicyVersion",1,now_utc);
    INSERT INTO tagekyc.raw_export_source_encryption_attempts
        (
            "AttemptId","SourceArtifactId",
            "EncryptionAttemptRevision","Fence",
            "ProvisionalObjectIdentity",
            "AttemptKeyReservationId","KeyProviderId",
            "KekId","KekVersion","KekFingerprint",
            "EncryptionSuiteId","EncryptionFramingVersion",
            "NonceStrategyId",
            "NonceDerivationSeedReferenceOrWrappedSeed",
            "NonceDerivationSeedCommitment","ChunkSize",
            "FramingParametersDigest",
            "EncryptionAttemptFingerprint",
            "OwnershipLeaseExpiresAtUtc",
            "R2TerminationDisposition","CreatedAtUtc",
            "SchemaVersion")
    VALUES
        (
            attempt_id,source_id,1,1,object_id,
            key_reservation_id,p_key_provider_id,p_kek_id,
            p_kek_version,p_kek_fingerprint,
            p_encryption_suite_id,
            p_encryption_framing_version,
            p_nonce_strategy_id,'none',
            p_nonce_seed_commitment,p_chunk_size,
            p_framing_parameters_digest,attempt_fingerprint,
            now_utc + pg_catalog.make_interval(
                secs => p_ownership_lease_seconds),
            NULL,now_utc,1);
    INSERT INTO tagekyc.raw_export_source_head
        (
            "SourceArtifactId","CustodyState",
            "CurrentEncryptionAttemptId",
            "ReservationRevision","Fence")
    VALUES (source_id,'Reserved',attempt_id,1,1);

    UPDATE tagekyc.raw_export_source_ingress_claim_aliases
    SET "AliasState" = 'Bound',
        "CurrentClaimEvaluationDisposition" = 'Completed'
    WHERE "IngressClaimAliasId" =
            alias_row."IngressClaimAliasId"
      AND "CurrentClaimEvaluationDisposition" = 'Active'
      AND "CurrentClaimEvaluationRevision" =
            p_claim_evaluation_revision
      AND "CurrentClaimEvaluationFence" =
            p_claim_evaluation_fence;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
    END IF;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_source_ingress_write_context',
        'claim:UPDATE',
        true);
    UPDATE tagekyc.raw_export_source_ingress_claims
    SET "ClaimState" = 'Reserved'
    WHERE "IngressClaimId" = claim_row."IngressClaimId"
      AND "ClaimState" = 'ClaimEvaluating';
    IF NOT FOUND THEN
        RAISE EXCEPTION 'CLAIM_TOKEN_INVALID';
    END IF;

    RETURN QUERY SELECT 'NewReservation'::text,source_id;
END;
$$;
""";

    private const string RetainedStageOperations = """
CREATE OR REPLACE FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(
  p_attempt_id uuid,p_object_custody_id uuid,p_expected_reservation_revision bigint,
  p_expected_encryption_attempt_revision bigint,p_expected_fence bigint,p_expected_object_state_revision bigint)
RETURNS TABLE(
  "Outcome" text,"SourceArtifactId" uuid,"AttemptId" uuid,"ObjectCustodyId" uuid,
  "StagedCiphertextFingerprintSchemaVersion" integer,"StagedCiphertextFingerprint" bytea,
  "ReservationRevision" bigint,"Fence" bigint,"StagedAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $stage$
DECLARE
  actor_id uuid; a record; h record; r record; c record; k record; o record; consent record; authority record;
  stage_time timestamptz; fingerprint bytea;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
  any_staged boolean;
  retention_probe record; retention_locked record; retention_requirement text; legacy_consent_invalid boolean:=false;
BEGIN
  IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_object_custody_id IS NULL OR p_object_custody_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_encryption_attempt_revision IS NULL OR p_expected_encryption_attempt_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1
     OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_probe
  FROM tagekyc.raw_export_source_encryption_attempts aa
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE aa."AttemptId"=p_attempt_id;
  IF retention_probe."AuthorityKind"='SourceRetention' THEN
    IF retention_probe."ConsentReferenceId" IS NULL
       OR retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id
       OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM actor_id THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
    PERFORM 1 FROM tagekyc.verification_sessions vs
      WHERE vs."Id"=retention_probe."VerificationSessionId"
       AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
    IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
      'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
      'tip88b1:lifecycle:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text));
    FOR retention_requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
      WHERE req."PolicyId"=retention_probe."ConsentPolicyId" AND req."PolicyVersion"=retention_probe."ConsentPolicyVersion"
       AND req."RequirementType"<>'ConsentArtifact' ORDER BY req."RequirementType" COLLATE "C" LOOP
      PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
        'tip88b1:fulfillment:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text||':'||retention_requirement));
    END LOOP;
  END IF;
  SELECT attempt.* INTO a FROM tagekyc.raw_export_source_encryption_attempts AS attempt
    WHERE attempt."AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT head.* INTO h FROM tagekyc.raw_export_source_head AS head
    WHERE head."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT reservation.* INTO r FROM tagekyc.raw_export_source_reservations AS reservation
    WHERE reservation."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT claim.* INTO c FROM tagekyc.raw_export_source_ingress_claims AS claim
    WHERE claim."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT key_reservation.* INTO k FROM tagekyc.raw_export_attempt_key_reservations AS key_reservation
    WHERE key_reservation."AttemptId"=a."AttemptId"
      AND key_reservation."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT provisional_object.* INTO o FROM tagekyc.raw_export_provisional_objects AS provisional_object
    WHERE provisional_object."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_locked
  FROM tagekyc.raw_export_source_encryption_attempts aa
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE aa."AttemptId"=p_attempt_id;
  IF retention_probe."AuthorityKind"='SourceRetention' OR retention_locked."AuthorityKind"='SourceRetention' THEN
    IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked) THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  END IF;
  IF h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  any_staged:=a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
    OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
    OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
    OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
    OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
    OR a."StagedAtUtc" IS NOT NULL;
  IF any_staged OR h."CustodyState"='Staged' THEN
    IF o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL OR o."ProviderReceiptDigest" IS NULL
       OR o."VerificationEvidenceDigest" IS NULL OR r."ClaimedPlaintextLength" IS NULL
       OR r."ContentCommitment" IS NULL
       OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728
       OR pg_catalog.octet_length(o."CiphertextDigest")<>32
       OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32
       OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
       OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
      RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
    END IF;
    fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
      pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
      pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
      o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
      pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
    IF a."StagedCiphertextFingerprintSchemaVersion"=2
       AND a."StagedCiphertextFingerprint"=fingerprint AND a."StagedObjectCustodyId"=p_object_custody_id
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision
       AND a."StagedFromReservationRevision"=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_encryption_attempt_revision AND a."Fence"=p_expected_fence
       AND a."VerifiedPlaintextLength"=r."ClaimedPlaintextLength" AND a."StagedCiphertextLength"=o."CiphertextLength"
       AND a."StagedCiphertextDigest"=o."CiphertextDigest"
       AND a."StagedProviderReceiptDigest"=o."ProviderReceiptDigest"
       AND a."StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest"
       AND h."CustodyState"='Staged' AND h."ReservationRevision"=a."StagedFromReservationRevision"+1 THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,a."SourceArtifactId",a."AttemptId",a."StagedObjectCustodyId",2,a."StagedCiphertextFingerprint",h."ReservationRevision",a."Fence",a."StagedAtUtc"; RETURN;
    END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  IF retention_locked."AuthorityKind"='SourceRetention' AND a."R2TerminalIntentCode" IS NOT NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  IF h."CustodyState"<>'Reserved' OR h."ReservationRevision"<>p_expected_reservation_revision
     OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision OR a."Fence"<>p_expected_fence
     OR o."StateRevision"<>p_expected_object_state_revision
     OR a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL
   OR k."PreparationDisposition"<>'Active' OR o."State"<>'VerifiedCompleted'
     OR o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL
     OR o."ProviderReceiptDigest" IS NULL OR o."VerificationEvidenceDigest" IS NULL
     OR r."ClaimedPlaintextLength" IS NULL OR r."ContentCommitment" IS NULL
     OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728 OR pg_catalog.octet_length(o."CiphertextDigest")<>32
     OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32 OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
     OR o."VerifiedAtUtc" IS NULL OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
    c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  END IF;
  stage_time:=pg_catalog.clock_timestamp();
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
    legacy_consent_invalid := consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">stage_time
     OR (consent."ValidUntilUtc" IS NOT NULL AND stage_time>=consent."ValidUntilUtc");
  END IF;
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(
    c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",stage_time);
  IF NOT FOUND
     OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
     OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity"
     OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>(CASE retention_locked."AuthorityKind"
       WHEN 'SourceRetention' THEN 'SourceRetention' ELSE 'SubjectRawBiometricExport' END)
     OR (retention_locked."AuthorityKind"='SourceRetention'
       AND tagekyc.raw_export_retained_snapshot_is_current(r."AuthoritySnapshotId",stage_time) IS NOT TRUE)
     OR legacy_consent_invalid
     OR stage_time>=r."ReservationExpiresAtUtc" OR stage_time>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
    pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
    pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
    o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
    pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r3-stage-v1',true);
  UPDATE tagekyc.raw_export_source_encryption_attempts AS attempt SET
    "StagedCiphertextFingerprintSchemaVersion"=2,"StagedCiphertextFingerprint"=fingerprint,
    "StagedObjectCustodyId"=o."ObjectCustodyId","StagedObjectStateRevision"=o."StateRevision",
    "StagedFromReservationRevision"=h."ReservationRevision","VerifiedPlaintextLength"=r."ClaimedPlaintextLength",
    "StagedCiphertextLength"=o."CiphertextLength","StagedCiphertextDigest"=o."CiphertextDigest",
    "StagedProviderReceiptDigest"=o."ProviderReceiptDigest",
    "StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest","StagedAtUtc"=stage_time
  WHERE attempt."AttemptId"=a."AttemptId";
  UPDATE tagekyc.raw_export_source_head AS head
  SET "CustodyState"='Staged',"ReservationRevision"=head."ReservationRevision"+1
  WHERE head."SourceArtifactId"=h."SourceArtifactId" AND head."CustodyState"='Reserved'
    AND head."ReservationRevision"=p_expected_reservation_revision
    AND head."CurrentEncryptionAttemptId"=a."AttemptId" AND head."Fence"=a."Fence";
  IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R3_STAGE_CAS_FAILED'; END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RETURN QUERY SELECT 'Staged'::text,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",2,fingerprint,h."ReservationRevision"+1,a."Fence",stage_time;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $stage$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_commit_staged_source(
  p_attempt_id uuid,p_expected_reservation_revision bigint,p_expected_attempt_revision bigint,
  p_expected_fence bigint,p_expected_object_state_revision bigint)
RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"AttemptId" uuid,
  "ObjectCustodyId" uuid,"CommitEvidenceDigest" bytea,"CommittedAtUtc" timestamptz,"ReservationRevision" bigint,"Fence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; probe record; p record; a record; h record; r record; c record; k record; o record;
  consent record; authority record; now_ timestamptz; evidence bytea; publication_id uuid:=pg_catalog.gen_random_uuid();
  prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
  retention_probe record; retention_locked record; retention_requirement text; legacy_consent_invalid boolean:=false;
BEGIN
  IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_attempt_revision IS NULL OR p_expected_attempt_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1
     OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R4_ARGUMENT_INVALID'; END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_probe
  FROM tagekyc.raw_export_source_encryption_attempts aa
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE aa."AttemptId"=p_attempt_id;
  IF retention_probe."AuthorityKind"='SourceRetention' THEN
    IF retention_probe."ConsentReferenceId" IS NULL
       OR retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id
       OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM actor_id THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
    PERFORM 1 FROM tagekyc.verification_sessions vs
      WHERE vs."Id"=retention_probe."VerificationSessionId"
       AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
    IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
      'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
      'tip88b1:lifecycle:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text));
    FOR retention_requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
      WHERE req."PolicyId"=retention_probe."ConsentPolicyId" AND req."PolicyVersion"=retention_probe."ConsentPolicyVersion"
       AND req."RequirementType"<>'ConsentArtifact' ORDER BY req."RequirementType" COLLATE "C" LOOP
      PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
        'tip88b1:fulfillment:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text||':'||retention_requirement));
    END LOOP;
  END IF;
  SELECT x.* INTO probe FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND AND (probe."StagedCiphertextFingerprintSchemaVersion"<>2 OR probe."StagedCiphertextFingerprint" IS NULL
     OR probe."StagedObjectCustodyId" IS NULL OR probe."StagedObjectStateRevision" IS NULL OR probe."StagedAtUtc" IS NULL) THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
  SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
  SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=a."StagedObjectCustodyId" FOR UPDATE;
  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_locked
  FROM tagekyc.raw_export_source_encryption_attempts aa
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE aa."AttemptId"=p_attempt_id;
  IF retention_probe."AuthorityKind"='SourceRetention' OR retention_locked."AuthorityKind"='SourceRetention' THEN
    IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked) THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  END IF;
  IF a IS NULL OR h IS NULL OR r IS NULL OR c IS NULL OR k IS NULL OR o IS NULL
     OR h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
     OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
     OR a."StagedCiphertextFingerprintSchemaVersion"<>2 OR a."StagedCiphertextFingerprint" IS NULL
     OR a."StagedObjectCustodyId"<>o."ObjectCustodyId" OR a."StagedAtUtc" IS NULL
     OR a."VerifiedPlaintextLength"<>r."ClaimedPlaintextLength"
     OR a."StagedCiphertextLength"<>o."CiphertextLength"
     OR a."StagedCiphertextDigest" IS DISTINCT FROM o."CiphertextDigest"
     OR a."StagedProviderReceiptDigest" IS DISTINCT FROM o."ProviderReceiptDigest"
     OR a."StagedVerificationEvidenceDigest" IS DISTINCT FROM o."VerificationEvidenceDigest" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  IF p."SourcePublicationId" IS NOT NULL THEN
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
      p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
       AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
       AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
       AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
       AND p."CommitEvidenceDigest"=evidence
       AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
    END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
  END IF;
  IF retention_locked."AuthorityKind"='SourceRetention' AND a."R2TerminalIntentCode" IS NOT NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  IF h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision
     OR a."EncryptionAttemptRevision"<>p_expected_attempt_revision OR a."Fence"<>p_expected_fence
     OR a."StagedObjectStateRevision"<>p_expected_object_state_revision OR k."PreparationDisposition"<>'Active'
     OR a."StagedFromReservationRevision"+1<>h."ReservationRevision" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  END IF;
  now_:=pg_catalog.clock_timestamp();
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
    legacy_consent_invalid := consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc");
  END IF;
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
  IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion" OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>(CASE retention_locked."AuthorityKind"
       WHEN 'SourceRetention' THEN 'SourceRetention' ELSE 'SubjectRawBiometricExport' END)
     OR (retention_locked."AuthorityKind"='SourceRetention'
       AND tagekyc.raw_export_retained_snapshot_is_current(r."AuthoritySnapshotId",now_) IS NOT TRUE)
     OR legacy_consent_invalid
     OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(a."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(a."AttemptId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(k."AttemptKeyReservationId"::text),'-',''),
    a."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(a."StagedCiphertextFingerprint",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
    pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
    pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r4-commit-v1',true);
  INSERT INTO tagekyc.raw_export_source_publications VALUES(publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",k."AttemptKeyReservationId",
    2,a."StagedCiphertextFingerprint",authority."AuthoritySnapshotSchemaVersion",authority."AuthoritySnapshotId",authority."Revision",r."ConsentPolicyId",r."ConsentPolicyVersion",
    evidence,now_,'Committed',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'NotPlanned',NULL,NULL,1,1)
  ON CONFLICT DO NOTHING;
  IF NOT FOUND THEN
    SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id;
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
      p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
       AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
       AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
       AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
       AND p."CommitEvidenceDigest"=evidence
       AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
      PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
    END IF;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
  END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
  RETURN QUERY SELECT 'Committed'::text,publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",evidence,now_,h."ReservationRevision",h."Fence";
EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
END $fn$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_publish_available_source(p_source_publication_id uuid,p_expected_reservation_revision bigint,p_expected_fence bigint)
RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"OpaqueCommittedLocatorId" uuid,
  "AvailableEvidenceDigest" bytea,"ReservationRevision" bigint,"Fence" bigint,"AvailableAtUtc" timestamptz,"CleanupDisposition" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; p record; a record; h record; r record; c record; k record; o record; consent record; authority record;
  now_ timestamptz; locator uuid:=pg_catalog.gen_random_uuid(); evidence bytea; commit_evidence bytea; cleanup_digest bytea; item_count bigint;
  prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
  core_prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
  retention_probe record; retention_locked record; retention_requirement text; legacy_consent_invalid boolean:=false;
BEGIN
  IF p_source_publication_id IS NULL OR p_source_publication_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R5_ARGUMENT_INVALID'; END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_probe
  FROM tagekyc.raw_export_source_publications pp
  JOIN tagekyc.raw_export_source_encryption_attempts aa ON aa."AttemptId"=pp."AttemptId" AND aa."SourceArtifactId"=pp."SourceArtifactId"
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE pp."SourcePublicationId"=p_source_publication_id;
  IF retention_probe."AuthorityKind"='SourceRetention' THEN
    IF retention_probe."ConsentReferenceId" IS NULL
       OR retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id
       OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM actor_id THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
    PERFORM 1 FROM tagekyc.verification_sessions vs
      WHERE vs."Id"=retention_probe."VerificationSessionId"
       AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
    IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
      'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
      'tip88b1:lifecycle:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text));
    FOR retention_requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
      WHERE req."PolicyId"=retention_probe."ConsentPolicyId" AND req."PolicyVersion"=retention_probe."ConsentPolicyVersion"
       AND req."RequirementType"<>'ConsentArtifact' ORDER BY req."RequirementType" COLLATE "C" LOOP
      PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
        'tip88b1:fulfillment:'||retention_probe."ConsentPolicyId"::text||':'||retention_probe."ConsentPolicyVersion"::text||':'||retention_requirement));
    END LOOP;
  END IF;
  SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=p_source_publication_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  PERFORM 1 FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptId" FOR UPDATE;
  SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p."AttemptId";
  SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId" WHERE y."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptKeyReservationId" FOR UPDATE OF x;
  SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=p."AttemptKeyReservationId";
  PERFORM 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."ObjectCustodyId" FOR UPDATE;
  SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=p."ObjectCustodyId";
  SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."IngressClaimId", rr."AuthoritySnapshotId",
    cc."ClientApplicationId", cc."VerificationSessionId", cc."AuthenticatedPrincipalId",
    ss."CustodyPrincipalId", ss."RuntimeBindingId", ss."ConsentBindingId",
    rr."ConsentPolicyId", rr."ConsentPolicyVersion", hh."ConsentReferenceId", hh."ExternalConsentArtifactRef"
  INTO retention_locked
  FROM tagekyc.raw_export_source_publications pp
  JOIN tagekyc.raw_export_source_encryption_attempts aa ON aa."AttemptId"=pp."AttemptId" AND aa."SourceArtifactId"=pp."SourceArtifactId"
  JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
  JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
  JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
  LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
  LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
  WHERE pp."SourcePublicationId"=p_source_publication_id;
  IF retention_probe."AuthorityKind"='SourceRetention' OR retention_locked."AuthorityKind"='SourceRetention' THEN
    IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked) THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  END IF;
  IF p."PublicationState"='Available' THEN
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."OpaqueCommittedLocatorId"::text),'-',''),
      pg_catalog.encode(p."CommitEvidenceDigest",'hex'),p."PublishedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."PublishedAuthoritySnapshotId"::text),'-',''),p."PublishedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."PublishedConsentPolicyId"::text),'-',''),p."PublishedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."AvailableAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF h."CustodyState"='Available' AND h."CurrentEncryptionAttemptId"=p."AttemptId"
       AND h."ReservationRevision"=p_expected_reservation_revision+1 AND h."Fence"=p_expected_fence
       AND a."SourceArtifactId"=p."SourceArtifactId" AND a."AttemptKeyReservationId"=p."AttemptKeyReservationId"
       AND a."StagedObjectCustodyId"=p."ObjectCustodyId"
       AND a."StagedCiphertextFingerprintSchemaVersion"=p."StagedCiphertextFingerprintSchemaVersion"
       AND a."StagedCiphertextFingerprint"=p."StagedCiphertextFingerprint"
       AND k."AttemptId"=p."AttemptId" AND o."AttemptId"=p."AttemptId"
       AND p."AvailableEvidenceDigest"=evidence THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."OpaqueCommittedLocatorId",p."AvailableEvidenceDigest",h."ReservationRevision",h."Fence",p."AvailableAtUtc",p."CleanupDisposition"::text; RETURN; END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN;
  END IF;
  IF retention_locked."AuthorityKind"='SourceRetention' AND a."R2TerminalIntentCode" IS NOT NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  commit_evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
    p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
    pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
    pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  IF p."PublicationState"<>'Committed' OR p."PublicationRevision"<>1 OR p."CleanupDisposition"<>'NotPlanned'
     OR h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision OR h."Fence"<>p_expected_fence
     OR h."CurrentEncryptionAttemptId"<>p."AttemptId" OR a."SourceArtifactId"<>p."SourceArtifactId"
     OR a."AttemptKeyReservationId"<>p."AttemptKeyReservationId" OR a."StagedObjectCustodyId"<>p."ObjectCustodyId"
     OR a."StagedCiphertextFingerprintSchemaVersion"<>p."StagedCiphertextFingerprintSchemaVersion"
     OR a."StagedCiphertextFingerprint" IS DISTINCT FROM p."StagedCiphertextFingerprint"
     OR a."StagedFromReservationRevision"+1<>h."ReservationRevision"
     OR k."AttemptId"<>a."AttemptId" OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
     OR o."VerificationEvidenceDigest" IS DISTINCT FROM a."StagedVerificationEvidenceDigest"
     OR p."CommitEvidenceDigest"<>commit_evidence OR k."PreparationDisposition"<>'Active' THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  END IF;
  now_:=pg_catalog.clock_timestamp();
  IF retention_locked."AuthorityKind"='LegacyExport' THEN
    legacy_consent_invalid := consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc");
  END IF;
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
  IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
     OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>(CASE retention_locked."AuthorityKind"
       WHEN 'SourceRetention' THEN 'SourceRetention' ELSE 'SubjectRawBiometricExport' END)
     OR (retention_locked."AuthorityKind"='SourceRetention'
       AND tagekyc.raw_export_retained_snapshot_is_current(r."AuthoritySnapshotId",now_) IS NOT TRUE)
     OR legacy_consent_invalid
     OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(locator::text),'-',''),
    pg_catalog.encode(p."CommitEvidenceDigest",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
    pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
    pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',true);
  INSERT INTO tagekyc.raw_export_source_cleanup_items
  SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'ProvisionalObject',x."ObjectCustodyId",x."AttemptId",x."StateRevision",'Pending',NULL,NULL,NULL,1,1
  FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."ObjectCustodyId"<>p."ObjectCustodyId"
    AND x."State" IN ('PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending','Deleted','Quarantined');
  INSERT INTO tagekyc.raw_export_source_cleanup_items
  SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'AttemptKeyReservation',x."AttemptKeyReservationId",x."AttemptId",x."RowRevision",'Pending',NULL,NULL,NULL,1,1
  FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId"
  WHERE y."SourceArtifactId"=p."SourceArtifactId" AND x."AttemptKeyReservationId"<>p."AttemptKeyReservationId"
    AND NOT (x."PreparationDisposition"='ReadyForFreshPreparation' AND x."WrappedDekCiphertext" IS NULL AND x."CurrentProviderOperationToken" IS NULL);
  SELECT pg_catalog.count(*) INTO item_count FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId";
  IF item_count=0 THEN cleanup_digest:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),'NoObsoleteResidue','0',
      pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]); END IF;
  UPDATE tagekyc.raw_export_source_publications x SET "PublicationState"='Available',"OpaqueCommittedLocatorId"=locator,
    "PublishedAuthoritySnapshotSchemaVersion"=authority."AuthoritySnapshotSchemaVersion","PublishedAuthoritySnapshotId"=authority."AuthoritySnapshotId",
    "PublishedAuthorityRevision"=authority."Revision","PublishedConsentPolicyId"=r."ConsentPolicyId","PublishedConsentPolicyVersion"=r."ConsentPolicyVersion",
    "AvailableEvidenceDigest"=evidence,"AvailableAtUtc"=now_,"CleanupDisposition"=CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END,
    "CleanupEvidenceDigest"=cleanup_digest,"FinalizedAtUtc"=CASE WHEN item_count=0 THEN now_ ELSE NULL END,"PublicationRevision"=2
    WHERE x."SourcePublicationId"=p."SourcePublicationId";
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r5-publish-v1',true);
  UPDATE tagekyc.raw_export_source_head x SET "CustodyState"='Available',"ReservationRevision"=x."ReservationRevision"+1
    WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."CustodyState"='Staged' AND x."ReservationRevision"=p_expected_reservation_revision AND x."Fence"=p_expected_fence;
  IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R5_HEAD_CAS_FAILED'; END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
  RETURN QUERY SELECT 'Available'::text,p."SourcePublicationId",p."SourceArtifactId",locator,evidence,p_expected_reservation_revision+1,p_expected_fence,now_,CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
END $fn$;
""";

    private const string RetainedStageOperationsRestore = """
CREATE OR REPLACE FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(
  p_attempt_id uuid,p_object_custody_id uuid,p_expected_reservation_revision bigint,
  p_expected_encryption_attempt_revision bigint,p_expected_fence bigint,p_expected_object_state_revision bigint)
RETURNS TABLE(
  "Outcome" text,"SourceArtifactId" uuid,"AttemptId" uuid,"ObjectCustodyId" uuid,
  "StagedCiphertextFingerprintSchemaVersion" integer,"StagedCiphertextFingerprint" bytea,
  "ReservationRevision" bigint,"Fence" bigint,"StagedAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $stage$
DECLARE
  actor_id uuid; a record; h record; r record; c record; k record; o record; consent record; authority record;
  stage_time timestamptz; fingerprint bytea;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
  any_staged boolean;
BEGIN
  IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_object_custody_id IS NULL OR p_object_custody_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_encryption_attempt_revision IS NULL OR p_expected_encryption_attempt_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1
     OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT attempt.* INTO a FROM tagekyc.raw_export_source_encryption_attempts AS attempt
    WHERE attempt."AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT head.* INTO h FROM tagekyc.raw_export_source_head AS head
    WHERE head."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT reservation.* INTO r FROM tagekyc.raw_export_source_reservations AS reservation
    WHERE reservation."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT claim.* INTO c FROM tagekyc.raw_export_source_ingress_claims AS claim
    WHERE claim."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT key_reservation.* INTO k FROM tagekyc.raw_export_attempt_key_reservations AS key_reservation
    WHERE key_reservation."AttemptId"=a."AttemptId"
      AND key_reservation."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT provisional_object.* INTO o FROM tagekyc.raw_export_provisional_objects AS provisional_object
    WHERE provisional_object."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

  IF h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  any_staged:=a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
    OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
    OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
    OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
    OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
    OR a."StagedAtUtc" IS NOT NULL;
  IF any_staged OR h."CustodyState"='Staged' THEN
    IF o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL OR o."ProviderReceiptDigest" IS NULL
       OR o."VerificationEvidenceDigest" IS NULL OR r."ClaimedPlaintextLength" IS NULL
       OR r."ContentCommitment" IS NULL
       OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728
       OR pg_catalog.octet_length(o."CiphertextDigest")<>32
       OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32
       OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
       OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
      RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
    END IF;
    fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
      pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
      pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
      o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
      pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
    IF a."StagedCiphertextFingerprintSchemaVersion"=2
       AND a."StagedCiphertextFingerprint"=fingerprint AND a."StagedObjectCustodyId"=p_object_custody_id
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision
       AND a."StagedFromReservationRevision"=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_encryption_attempt_revision AND a."Fence"=p_expected_fence
       AND a."VerifiedPlaintextLength"=r."ClaimedPlaintextLength" AND a."StagedCiphertextLength"=o."CiphertextLength"
       AND a."StagedCiphertextDigest"=o."CiphertextDigest"
       AND a."StagedProviderReceiptDigest"=o."ProviderReceiptDigest"
       AND a."StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest"
       AND h."CustodyState"='Staged' AND h."ReservationRevision"=a."StagedFromReservationRevision"+1 THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,a."SourceArtifactId",a."AttemptId",a."StagedObjectCustodyId",2,a."StagedCiphertextFingerprint",h."ReservationRevision",a."Fence",a."StagedAtUtc"; RETURN;
    END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  IF h."CustodyState"<>'Reserved' OR h."ReservationRevision"<>p_expected_reservation_revision
     OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision OR a."Fence"<>p_expected_fence
     OR o."StateRevision"<>p_expected_object_state_revision
     OR a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL
   OR k."PreparationDisposition"<>'Active' OR o."State"<>'VerifiedCompleted'
     OR o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL
     OR o."ProviderReceiptDigest" IS NULL OR o."VerificationEvidenceDigest" IS NULL
     OR r."ClaimedPlaintextLength" IS NULL OR r."ContentCommitment" IS NULL
     OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728 OR pg_catalog.octet_length(o."CiphertextDigest")<>32
     OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32 OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
     OR o."VerifiedAtUtc" IS NULL OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
    c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  stage_time:=pg_catalog.clock_timestamp();
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(
    c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",stage_time);
  IF NOT FOUND
     OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
     OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity"
     OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
     OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">stage_time
     OR (consent."ValidUntilUtc" IS NOT NULL AND stage_time>=consent."ValidUntilUtc")
     OR stage_time>=r."ReservationExpiresAtUtc" OR stage_time>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;

  fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
    pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
    pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
    o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
    pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r3-stage-v1',true);
  UPDATE tagekyc.raw_export_source_encryption_attempts AS attempt SET
    "StagedCiphertextFingerprintSchemaVersion"=2,"StagedCiphertextFingerprint"=fingerprint,
    "StagedObjectCustodyId"=o."ObjectCustodyId","StagedObjectStateRevision"=o."StateRevision",
    "StagedFromReservationRevision"=h."ReservationRevision","VerifiedPlaintextLength"=r."ClaimedPlaintextLength",
    "StagedCiphertextLength"=o."CiphertextLength","StagedCiphertextDigest"=o."CiphertextDigest",
    "StagedProviderReceiptDigest"=o."ProviderReceiptDigest",
    "StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest","StagedAtUtc"=stage_time
  WHERE attempt."AttemptId"=a."AttemptId";
  UPDATE tagekyc.raw_export_source_head AS head
  SET "CustodyState"='Staged',"ReservationRevision"=head."ReservationRevision"+1
  WHERE head."SourceArtifactId"=h."SourceArtifactId" AND head."CustodyState"='Reserved'
    AND head."ReservationRevision"=p_expected_reservation_revision
    AND head."CurrentEncryptionAttemptId"=a."AttemptId" AND head."Fence"=a."Fence";
  IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R3_STAGE_CAS_FAILED'; END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RETURN QUERY SELECT 'Staged'::text,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",2,fingerprint,h."ReservationRevision"+1,a."Fence",stage_time;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $stage$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_commit_staged_source(
  p_attempt_id uuid,p_expected_reservation_revision bigint,p_expected_attempt_revision bigint,
  p_expected_fence bigint,p_expected_object_state_revision bigint)
RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"AttemptId" uuid,
  "ObjectCustodyId" uuid,"CommitEvidenceDigest" bytea,"CommittedAtUtc" timestamptz,"ReservationRevision" bigint,"Fence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; probe record; p record; a record; h record; r record; c record; k record; o record;
  consent record; authority record; now_ timestamptz; evidence bytea; publication_id uuid:=pg_catalog.gen_random_uuid();
  prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
BEGIN
  IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_attempt_revision IS NULL OR p_expected_attempt_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1
     OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R4_ARGUMENT_INVALID'; END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT x.* INTO probe FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND AND (probe."StagedCiphertextFingerprintSchemaVersion"<>2 OR probe."StagedCiphertextFingerprint" IS NULL
     OR probe."StagedObjectCustodyId" IS NULL OR probe."StagedObjectStateRevision" IS NULL OR probe."StagedAtUtc" IS NULL) THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
  SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
  SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=a."StagedObjectCustodyId" FOR UPDATE;
  IF a IS NULL OR h IS NULL OR r IS NULL OR c IS NULL OR k IS NULL OR o IS NULL
     OR h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
     OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
     OR a."StagedCiphertextFingerprintSchemaVersion"<>2 OR a."StagedCiphertextFingerprint" IS NULL
     OR a."StagedObjectCustodyId"<>o."ObjectCustodyId" OR a."StagedAtUtc" IS NULL
     OR a."VerifiedPlaintextLength"<>r."ClaimedPlaintextLength"
     OR a."StagedCiphertextLength"<>o."CiphertextLength"
     OR a."StagedCiphertextDigest" IS DISTINCT FROM o."CiphertextDigest"
     OR a."StagedProviderReceiptDigest" IS DISTINCT FROM o."ProviderReceiptDigest"
     OR a."StagedVerificationEvidenceDigest" IS DISTINCT FROM o."VerificationEvidenceDigest" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  IF p."SourcePublicationId" IS NOT NULL THEN
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
      p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
       AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
       AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
       AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
       AND p."CommitEvidenceDigest"=evidence
       AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
    END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
  END IF;
  IF h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision
     OR a."EncryptionAttemptRevision"<>p_expected_attempt_revision OR a."Fence"<>p_expected_fence
     OR a."StagedObjectStateRevision"<>p_expected_object_state_revision OR k."PreparationDisposition"<>'Active'
     OR a."StagedFromReservationRevision"+1<>h."ReservationRevision" THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  now_:=pg_catalog.clock_timestamp();
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
  IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion" OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
     OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc")
     OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
  evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(a."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(a."AttemptId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(k."AttemptKeyReservationId"::text),'-',''),
    a."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(a."StagedCiphertextFingerprint",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
    pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
    pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r4-commit-v1',true);
  INSERT INTO tagekyc.raw_export_source_publications VALUES(publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",k."AttemptKeyReservationId",
    2,a."StagedCiphertextFingerprint",authority."AuthoritySnapshotSchemaVersion",authority."AuthoritySnapshotId",authority."Revision",r."ConsentPolicyId",r."ConsentPolicyVersion",
    evidence,now_,'Committed',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'NotPlanned',NULL,NULL,1,1)
  ON CONFLICT DO NOTHING;
  IF NOT FOUND THEN
    SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id;
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
      p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
       AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
       AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
       AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
       AND p."CommitEvidenceDigest"=evidence
       AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
       AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
       AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
      PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
    END IF;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
  END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
  RETURN QUERY SELECT 'Committed'::text,publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",evidence,now_,h."ReservationRevision",h."Fence";
EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
END $fn$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_publish_available_source(p_source_publication_id uuid,p_expected_reservation_revision bigint,p_expected_fence bigint)
RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"OpaqueCommittedLocatorId" uuid,
  "AvailableEvidenceDigest" bytea,"ReservationRevision" bigint,"Fence" bigint,"AvailableAtUtc" timestamptz,"CleanupDisposition" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE actor_id uuid; p record; a record; h record; r record; c record; k record; o record; consent record; authority record;
  now_ timestamptz; locator uuid:=pg_catalog.gen_random_uuid(); evidence bytea; commit_evidence bytea; cleanup_digest bytea; item_count bigint;
  prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
  core_prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
  IF p_source_publication_id IS NULL OR p_source_publication_id='00000000-0000-0000-0000-000000000000'::uuid
     OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
     OR p_expected_fence IS NULL OR p_expected_fence<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R5_ARGUMENT_INVALID'; END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=p_source_publication_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  PERFORM 1 FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptId" FOR UPDATE;
  SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p."AttemptId";
  SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
  SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
  PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId" WHERE y."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptKeyReservationId" FOR UPDATE OF x;
  SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=p."AttemptKeyReservationId";
  PERFORM 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."ObjectCustodyId" FOR UPDATE;
  SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=p."ObjectCustodyId";
  IF p."PublicationState"='Available' THEN
    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
      pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."OpaqueCommittedLocatorId"::text),'-',''),
      pg_catalog.encode(p."CommitEvidenceDigest",'hex'),p."PublishedAuthoritySnapshotSchemaVersion"::text,
      pg_catalog.replace(pg_catalog.lower(p."PublishedAuthoritySnapshotId"::text),'-',''),p."PublishedAuthorityRevision"::text,
      pg_catalog.replace(pg_catalog.lower(p."PublishedConsentPolicyId"::text),'-',''),p."PublishedConsentPolicyVersion"::text,
      pg_catalog.to_char(p."AvailableAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
    IF h."CustodyState"='Available' AND h."CurrentEncryptionAttemptId"=p."AttemptId"
       AND h."ReservationRevision"=p_expected_reservation_revision+1 AND h."Fence"=p_expected_fence
       AND a."SourceArtifactId"=p."SourceArtifactId" AND a."AttemptKeyReservationId"=p."AttemptKeyReservationId"
       AND a."StagedObjectCustodyId"=p."ObjectCustodyId"
       AND a."StagedCiphertextFingerprintSchemaVersion"=p."StagedCiphertextFingerprintSchemaVersion"
       AND a."StagedCiphertextFingerprint"=p."StagedCiphertextFingerprint"
       AND k."AttemptId"=p."AttemptId" AND o."AttemptId"=p."AttemptId"
       AND p."AvailableEvidenceDigest"=evidence THEN
      RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."OpaqueCommittedLocatorId",p."AvailableEvidenceDigest",h."ReservationRevision",h."Fence",p."AvailableAtUtc",p."CleanupDisposition"::text; RETURN; END IF;
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN;
  END IF;
  commit_evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
    p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
    pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
    pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  IF p."PublicationState"<>'Committed' OR p."PublicationRevision"<>1 OR p."CleanupDisposition"<>'NotPlanned'
     OR h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision OR h."Fence"<>p_expected_fence
     OR h."CurrentEncryptionAttemptId"<>p."AttemptId" OR a."SourceArtifactId"<>p."SourceArtifactId"
     OR a."AttemptKeyReservationId"<>p."AttemptKeyReservationId" OR a."StagedObjectCustodyId"<>p."ObjectCustodyId"
     OR a."StagedCiphertextFingerprintSchemaVersion"<>p."StagedCiphertextFingerprintSchemaVersion"
     OR a."StagedCiphertextFingerprint" IS DISTINCT FROM p."StagedCiphertextFingerprint"
     OR a."StagedFromReservationRevision"+1<>h."ReservationRevision"
     OR k."AttemptId"<>a."AttemptId" OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
     OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
     OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
     OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
     OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
     OR o."VerificationEvidenceDigest" IS DISTINCT FROM a."StagedVerificationEvidenceDigest"
     OR p."CommitEvidenceDigest"<>commit_evidence OR k."PreparationDisposition"<>'Active' THEN
    RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
  SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
  now_:=pg_catalog.clock_timestamp();
  SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
  IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
     OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
     OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
     OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
     OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
     OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
     OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
     OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
     OR consent."PurposeCode"<>'SubjectRawBiometricExport'
     OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
     OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc")
     OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
    RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
  evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
    pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
    pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(locator::text),'-',''),
    pg_catalog.encode(p."CommitEvidenceDigest",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
    pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
    pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
    pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',true);
  INSERT INTO tagekyc.raw_export_source_cleanup_items
  SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'ProvisionalObject',x."ObjectCustodyId",x."AttemptId",x."StateRevision",'Pending',NULL,NULL,NULL,1,1
  FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."ObjectCustodyId"<>p."ObjectCustodyId"
    AND x."State" IN ('PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending','Deleted','Quarantined');
  INSERT INTO tagekyc.raw_export_source_cleanup_items
  SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'AttemptKeyReservation',x."AttemptKeyReservationId",x."AttemptId",x."RowRevision",'Pending',NULL,NULL,NULL,1,1
  FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId"
  WHERE y."SourceArtifactId"=p."SourceArtifactId" AND x."AttemptKeyReservationId"<>p."AttemptKeyReservationId"
    AND NOT (x."PreparationDisposition"='ReadyForFreshPreparation' AND x."WrappedDekCiphertext" IS NULL AND x."CurrentProviderOperationToken" IS NULL);
  SELECT pg_catalog.count(*) INTO item_count FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId";
  IF item_count=0 THEN cleanup_digest:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[
      pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),'NoObsoleteResidue','0',
      pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]); END IF;
  UPDATE tagekyc.raw_export_source_publications x SET "PublicationState"='Available',"OpaqueCommittedLocatorId"=locator,
    "PublishedAuthoritySnapshotSchemaVersion"=authority."AuthoritySnapshotSchemaVersion","PublishedAuthoritySnapshotId"=authority."AuthoritySnapshotId",
    "PublishedAuthorityRevision"=authority."Revision","PublishedConsentPolicyId"=r."ConsentPolicyId","PublishedConsentPolicyVersion"=r."ConsentPolicyVersion",
    "AvailableEvidenceDigest"=evidence,"AvailableAtUtc"=now_,"CleanupDisposition"=CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END,
    "CleanupEvidenceDigest"=cleanup_digest,"FinalizedAtUtc"=CASE WHEN item_count=0 THEN now_ ELSE NULL END,"PublicationRevision"=2
    WHERE x."SourcePublicationId"=p."SourcePublicationId";
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r5-publish-v1',true);
  UPDATE tagekyc.raw_export_source_head x SET "CustodyState"='Available',"ReservationRevision"=x."ReservationRevision"+1
    WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."CustodyState"='Staged' AND x."ReservationRevision"=p_expected_reservation_revision AND x."Fence"=p_expected_fence;
  IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R5_HEAD_CAS_FAILED'; END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
  RETURN QUERY SELECT 'Available'::text,p."SourcePublicationId",p."SourceArtifactId",locator,evidence,p_expected_reservation_revision+1,p_expected_fence,now_,CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
END $fn$;
""";

    private const string ContinuationOperations = """
CREATE FUNCTION tagekyc.raw_export_read_retained_source_continuation(p_source_artifact_id uuid)
RETURNS TABLE("SourceArtifactId" uuid,"CustodyPrincipalId" uuid,"ClientApplicationId" uuid,
 "VerificationSessionId" uuid,"RuntimeBindingId" uuid,"RetentionAuthorityId" uuid,
 "RetentionAuthorityRevision" bigint,"CustodyState" text,"ReservationRevision" bigint,"Fence" bigint,
 "AttemptId" uuid,"EncryptionAttemptRevision" bigint,"AttemptKeyReservationId" uuid,
 "ObjectCustodyId" uuid,"ObjectState" text,"ObjectStateRevision" bigint,
 "SourcePublicationId" uuid,"PublicationRevision" bigint,"PublicationState" text,"CleanupDisposition" text,
 "R2TerminalIntentCode" text,"R2TerminalIntentDisposition" text,"R2TerminalIntentAtUtc" timestamptz,
 "R2TerminationDisposition" text,"R2TerminatedAtUtc" timestamptz,"R2TerminalOutcomeCode" text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $readback$
 SELECT r."SourceArtifactId",s."CustodyPrincipalId",c."ClientApplicationId",
 c."VerificationSessionId",s."RuntimeBindingId",s."RetentionAuthorityId",
 s."RetentionAuthorityRevision",h."CustodyState"::text,h."ReservationRevision",h."Fence",
 a."AttemptId",a."EncryptionAttemptRevision",a."AttemptKeyReservationId",
 o."ObjectCustodyId",o."State"::text,o."StateRevision",
 p."SourcePublicationId",p."PublicationRevision",p."PublicationState"::text,p."CleanupDisposition"::text,
 a."R2TerminalIntentCode"::text,a."R2TerminalIntentDisposition"::text,a."R2TerminalIntentAtUtc",
 a."R2TerminationDisposition"::text,a."R2TerminatedAtUtc",a."R2TerminalOutcomeCode"
 FROM tagekyc.raw_export_source_reservations r
 JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
 JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
 JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=r."SourceArtifactId"
 JOIN tagekyc.raw_export_source_encryption_attempts a ON a."AttemptId"=h."CurrentEncryptionAttemptId"
 JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
 LEFT JOIN tagekyc.raw_export_attempt_key_reservations k ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 LEFT JOIN tagekyc.raw_export_provisional_objects o ON o."AttemptId"=a."AttemptId"
 LEFT JOIN tagekyc.raw_export_source_publications p ON p."SourceArtifactId"=r."SourceArtifactId"
 WHERE r."SourceArtifactId"=p_source_artifact_id
 AND s."AuthorityKind"='SourceRetention' AND s."EventType"='Granted'
 AND s."CustodyPrincipalId" IS NOT NULL AND s."CustodyPrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid
 AND s."RetentionAuthorityId" IS NOT NULL AND s."RetentionAuthorityRevision">=1 AND s."ConsentBindingId" IS NOT NULL
 AND s."ClientApplicationId"=c."ClientApplicationId" AND s."VerificationSessionId"=c."VerificationSessionId"
 AND s."CaptureAcceptanceId"=c."CaptureAcceptanceId" AND s."RawClass"=c."RawClass"
 AND s."CustodyPrincipalId"=c."AuthenticatedPrincipalId"
 AND b."AuthorityMode"='SourceRetention' AND b."PrincipalId"=s."CustodyPrincipalId"
 AND b."ClientApplicationId"=c."ClientApplicationId" AND b."VerificationSessionId"=c."VerificationSessionId"
 AND b."RetentionAuthorityId"=s."RetentionAuthorityId" AND b."RetentionAuthorityRevision"=s."RetentionAuthorityRevision"
 AND b."ConsentBindingId"=s."ConsentBindingId"
 AND a."SourceArtifactId"=r."SourceArtifactId" AND a."Fence"=h."Fence"
 AND h."ReservationRevision">=1 AND a."EncryptionAttemptRevision">=1
 AND h."CustodyState" IN ('Reserved','Staged','Available')
 -- Anchor optional rows by allocated identity, then validate. A contradictory
 -- present row must not disappear as a fabricated NULL/absence projection.
 AND (k."AttemptKeyReservationId" IS NULL OR
  (k."AttemptId"=a."AttemptId" AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"))
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x
  WHERE x."AttemptId"=a."AttemptId" AND x."AttemptKeyReservationId"<>a."AttemptKeyReservationId")
 AND (o."ObjectCustodyId" IS NULL OR (k."AttemptKeyReservationId" IS NOT NULL
  AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."SourceArtifactId"=a."SourceArtifactId"
  AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."AttemptFence"=a."Fence"
  AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision"
  AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
  AND o."State" IS NOT NULL AND o."StateRevision">=1))
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x
  WHERE (x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
  AND x."AttemptId"<>a."AttemptId")
 AND (p."SourcePublicationId" IS NULL OR (o."ObjectCustodyId" IS NOT NULL
  AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=a."AttemptKeyReservationId"
  AND p."ObjectCustodyId"=o."ObjectCustodyId" AND p."PublicationRevision">=1
  AND p."PublicationState" IN ('Committed','Available') AND p."CleanupDisposition" IN ('NotPlanned','Pending','NoObsoleteResidue','Completed')))
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications x
  WHERE x."AttemptId"=a."AttemptId" AND x."SourceArtifactId"<>a."SourceArtifactId")
 AND ((a."R2TerminalIntentCode" IS NULL AND a."R2TerminalIntentDisposition" IS NULL AND a."R2TerminalIntentAtUtc" IS NULL)
  OR (a."R2TerminalIntentCode" IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
   AND a."R2TerminalIntentDisposition" IN ('Terminated','TerminatedBeforeStart') AND a."R2TerminalIntentAtUtc" IS NOT NULL))
 AND ((a."R2TerminationDisposition" IS NULL AND a."R2TerminatedAtUtc" IS NULL)
  OR (a."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart') AND a."R2TerminatedAtUtc" IS NOT NULL))
 AND (a."R2TerminalIntentCode" IS NULL OR a."R2TerminationDisposition" IS NULL
  OR a."R2TerminationDisposition"=a."R2TerminalIntentDisposition")
 AND (a."R2TerminalOutcomeCode" IS NULL OR
  (a."R2TerminalOutcomeCode"=a."R2TerminalIntentCode" AND a."R2TerminationDisposition"=a."R2TerminalIntentDisposition"
   AND a."R2TerminalIntentAtUtc" IS NOT NULL AND a."R2TerminatedAtUtc" IS NOT NULL
   AND (a."R2TerminatedAtUtc">=a."R2TerminalIntentAtUtc" OR
    (a."R2TerminalOutcomeCode"='RECAPTURE_REQUIRED'
     AND r."EffectivePlaintextRetentionExpiresAtUtc" IS NOT NULL AND r."AbsoluteSourceExpiresAtUtc" IS NOT NULL
     AND s."ValidUntilUtc" IS NOT NULL AND b."ExecutionExpiresAtUtc" IS NOT NULL
     AND a."R2TerminalIntentAtUtc">=LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc")
     AND ((EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((a."R2TerminationDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (a."R2TerminationDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint")))))) OR (a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=a."R2TerminatedAtUtc" AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"))))))))
$readback$;
CREATE FUNCTION tagekyc.raw_export_list_retained_source_continuations(p_after_source_artifact_id uuid,p_limit integer)
RETURNS TABLE("SourceArtifactId" uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $scan$
DECLARE scan_now timestamptz:=clock_timestamp();
BEGIN
 IF p_limit IS NULL OR p_limit<1 OR p_limit>100 THEN
  RAISE EXCEPTION 'A3_CONTINUATION_SCAN_ARGUMENT_INVALID';
 END IF;
 RETURN QUERY
 SELECT v."SourceArtifactId"
 FROM tagekyc.raw_export_source_head h
 CROSS JOIN LATERAL tagekyc.raw_export_read_retained_source_continuation(h."SourceArtifactId") v
 JOIN tagekyc.raw_export_source_encryption_attempts a ON a."AttemptId"=v."AttemptId"
 JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=v."SourceArtifactId"
 JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
 JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
 WHERE (p_after_source_artifact_id IS NULL OR v."SourceArtifactId">p_after_source_artifact_id)
 AND ((v."PublicationState"='Available' AND v."CleanupDisposition"='Pending')
 OR ((v."CustodyState" IN ('Reserved','Staged') OR v."R2TerminalIntentCode" IS NOT NULL
  OR (v."R2TerminationDisposition" IS NOT NULL AND v."R2TerminalIntentCode" IS NULL AND v."R2TerminalOutcomeCode" IS NULL
   AND LEAST(b."ExecutionExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc")<=scan_now))
 AND NOT COALESCE((a."R2TerminalOutcomeCode"=a."R2TerminalIntentCode"
  AND a."R2TerminationDisposition"=a."R2TerminalIntentDisposition"
  AND a."R2TerminalIntentAtUtc" IS NOT NULL AND a."R2TerminatedAtUtc" IS NOT NULL
  AND ((EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((a."R2TerminationDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (a."R2TerminationDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint")))))) OR (a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=scan_now AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"))))),false)))
 ORDER BY v."SourceArtifactId" LIMIT p_limit;
END $scan$;
ALTER FUNCTION tagekyc.raw_export_read_retained_source_continuation(uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_read_retained_source_continuation(uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_retained_source_continuation(uuid) TO tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;
ALTER FUNCTION tagekyc.raw_export_list_retained_source_continuations(uuid,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_list_retained_source_continuations(uuid,integer) FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_list_retained_source_continuations(uuid,integer) TO tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;
""";

    private const string TerminalIntentOperations = """
CREATE FUNCTION tagekyc.raw_export_record_retained_r2_terminal_intent(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,
 p_operational_disposition text,p_terminal_outcome_code text)
RETURNS TABLE("Outcome" text,"TerminalIntentCode" text,"TerminalIntentDisposition" text,"TerminalIntentAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $ti$
DECLARE retention_probe record; retention_locked record; a record; h record; r record; s record; c record;
 actor_id uuid; now_utc timestamptz; disposition text; nps_settled boolean; key_settled boolean; termination_result text;
 previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
 IF p_source_artifact_id IS NULL OR p_source_artifact_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_expected_revision IS NULL OR p_expected_revision<1 OR p_expected_fence IS NULL OR p_expected_fence<1
 OR p_operational_disposition IS NULL OR p_operational_disposition NOT IN ('Terminated','TerminatedBeforeStart')
 OR p_terminal_outcome_code IS NULL OR p_terminal_outcome_code NOT IN
 ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED') THEN
 RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='A3_R2_TERMINAL_INTENT_ARGUMENT_INVALID'; END IF;
 actor_id:=tagekyc.raw_export_current_actor();
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88c1:b2-authority:'||retention_probe."ClientApplicationId"::text||':'||
    retention_probe."VerificationSessionId"::text||':'||retention_probe."CaptureAcceptanceId"::text||':'||retention_probe."RawClass"));
 END IF;

 IF retention_probe."AuthorityKind" IS NULL THEN
  RETURN QUERY SELECT 'NotFound'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 IF retention_probe."AuthorityKind"<>'SourceRetention' THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 SELECT aa.* INTO a FROM tagekyc.raw_export_source_encryption_attempts aa
  WHERE aa."AttemptId"=p_attempt_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT hh.* INTO h FROM tagekyc.raw_export_source_head hh
  WHERE hh."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT rr.* INTO r FROM tagekyc.raw_export_source_reservations rr
  WHERE rr."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT ss.* INTO s FROM tagekyc.raw_export_authority_snapshots ss
  WHERE ss."AuthoritySnapshotId"=r."AuthoritySnapshotId";
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT cc.* INTO c FROM tagekyc.raw_export_source_ingress_claims cc
  WHERE cc."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked)
  OR a."SourceArtifactId" IS DISTINCT FROM p_source_artifact_id
  OR retention_probe."SourceArtifactId" IS DISTINCT FROM r."SourceArtifactId"
  OR retention_probe."AuthoritySnapshotId" IS DISTINCT FROM s."AuthoritySnapshotId"
  OR retention_probe."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"
  OR retention_probe."ClientApplicationId" IS DISTINCT FROM c."ClientApplicationId"
  OR s."AuthorityKind" IS DISTINCT FROM 'SourceRetention'
  OR c."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id OR s."CustodyPrincipalId" IS DISTINCT FROM actor_id
  OR h."CurrentEncryptionAttemptId" IS DISTINCT FROM a."AttemptId" OR h."CustodyState" IS DISTINCT FROM 'Reserved'
  OR h."Fence" IS DISTINCT FROM a."Fence" OR a."EncryptionAttemptRevision" IS DISTINCT FROM p_expected_revision
  OR a."Fence" IS DISTINCT FROM p_expected_fence
  OR a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
  OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
  OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
  OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
  OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
  OR a."StagedAtUtc" IS NOT NULL
  OR EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId") THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;

 IF a."R2TerminalIntentCode" IS NOT NULL OR a."R2TerminalIntentDisposition" IS NOT NULL OR a."R2TerminalIntentAtUtc" IS NOT NULL THEN
  IF a."R2TerminalIntentCode"=p_terminal_outcome_code AND a."R2TerminalIntentDisposition"=p_operational_disposition
   AND a."R2TerminalIntentAtUtc" IS NOT NULL THEN
   RETURN QUERY SELECT 'ExistingMatch'::text,a."R2TerminalIntentCode"::text,a."R2TerminalIntentDisposition"::text,a."R2TerminalIntentAtUtc";
  ELSE RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; END IF;
  RETURN;
 END IF;
 IF a."R2TerminalOutcomeCode" IS NOT NULL THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 disposition:=p_operational_disposition;
 PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR k."AttemptId"=a."AttemptId" FOR UPDATE;
 PERFORM 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" FOR UPDATE;
 now_utc:=pg_catalog.clock_timestamp();
 nps_settled:=(a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=now_utc AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")));
 key_settled:=(EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((disposition='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (disposition='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"))))));
 IF a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL THEN
  IF NOT (p_terminal_outcome_code='RECAPTURE_REQUIRED'
   AND a."R2TerminationDisposition"=disposition AND a."R2TerminatedAtUtc" IS NOT NULL
   AND (nps_settled OR key_settled) AND EXISTS(SELECT 1 FROM tagekyc.capture_execution_bindings b
 WHERE b."CaptureExecutionBindingId"=s."RuntimeBindingId"
 AND r."EffectivePlaintextRetentionExpiresAtUtc" IS NOT NULL AND r."AbsoluteSourceExpiresAtUtc" IS NOT NULL
 AND s."ValidUntilUtc" IS NOT NULL AND b."ExecutionExpiresAtUtc" IS NOT NULL
 AND LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc")<=now_utc)) THEN
   RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN;
  END IF;
 ELSIF p_terminal_outcome_code='RECAPTURE_REQUIRED' AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")) THEN
  -- Unwitnessed no-key recovery enters NPS01 first, never TI01.
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-a3-r2-terminal-intent-v1',true);
 UPDATE tagekyc.raw_export_source_encryption_attempts aa SET
 "R2TerminalIntentCode"=p_terminal_outcome_code,"R2TerminalIntentDisposition"=disposition,"R2TerminalIntentAtUtc"=now_utc
 WHERE aa."AttemptId"=a."AttemptId";
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RETURN QUERY SELECT 'Recorded'::text,p_terminal_outcome_code,disposition,now_utc;
EXCEPTION WHEN OTHERS THEN
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RAISE;
END $ti$;
CREATE FUNCTION tagekyc.raw_export_finalize_retained_r2_terminal(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
RETURNS TABLE("Outcome" text,"TerminalOutcomeCode" text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $ti$
DECLARE retention_probe record; retention_locked record; a record; h record; r record; s record; c record;
 actor_id uuid; now_utc timestamptz; disposition text; nps_settled boolean; key_settled boolean; termination_result text;
 previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
 IF p_source_artifact_id IS NULL OR p_source_artifact_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_expected_revision IS NULL OR p_expected_revision<1 OR p_expected_fence IS NULL OR p_expected_fence<1 THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
 actor_id:=tagekyc.raw_export_current_actor();
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88c1:b2-authority:'||retention_probe."ClientApplicationId"::text||':'||
    retention_probe."VerificationSessionId"::text||':'||retention_probe."CaptureAcceptanceId"::text||':'||retention_probe."RawClass"));
 END IF;

 IF retention_probe."AuthorityKind" IS NULL THEN
  RETURN QUERY SELECT 'NotFound'::text,NULL::text; RETURN;
 END IF;
 IF retention_probe."AuthorityKind"<>'SourceRetention' THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN;
 END IF;
 SELECT aa.* INTO a FROM tagekyc.raw_export_source_encryption_attempts aa
  WHERE aa."AttemptId"=p_attempt_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::text; RETURN; END IF;
 SELECT hh.* INTO h FROM tagekyc.raw_export_source_head hh
  WHERE hh."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
 SELECT rr.* INTO r FROM tagekyc.raw_export_source_reservations rr
  WHERE rr."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
 SELECT ss.* INTO s FROM tagekyc.raw_export_authority_snapshots ss
  WHERE ss."AuthoritySnapshotId"=r."AuthoritySnapshotId";
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
 SELECT cc.* INTO c FROM tagekyc.raw_export_source_ingress_claims cc
  WHERE cc."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN; END IF;
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked)
  OR a."SourceArtifactId" IS DISTINCT FROM p_source_artifact_id
  OR retention_probe."SourceArtifactId" IS DISTINCT FROM r."SourceArtifactId"
  OR retention_probe."AuthoritySnapshotId" IS DISTINCT FROM s."AuthoritySnapshotId"
  OR retention_probe."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"
  OR retention_probe."ClientApplicationId" IS DISTINCT FROM c."ClientApplicationId"
  OR s."AuthorityKind" IS DISTINCT FROM 'SourceRetention'
  OR c."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id OR s."CustodyPrincipalId" IS DISTINCT FROM actor_id
  OR h."CurrentEncryptionAttemptId" IS DISTINCT FROM a."AttemptId" OR h."CustodyState" IS DISTINCT FROM 'Reserved'
  OR h."Fence" IS DISTINCT FROM a."Fence" OR a."EncryptionAttemptRevision" IS DISTINCT FROM p_expected_revision
  OR a."Fence" IS DISTINCT FROM p_expected_fence
  OR a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
  OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
  OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
  OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
  OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
  OR a."StagedAtUtc" IS NOT NULL
  OR EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId") THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN;
 END IF;

 IF a."R2TerminalIntentCode" IS NULL OR a."R2TerminalIntentDisposition" IS NULL OR a."R2TerminalIntentAtUtc" IS NULL
 OR a."R2TerminalIntentCode" NOT IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
 OR a."R2TerminalIntentDisposition" NOT IN ('Terminated','TerminatedBeforeStart')
 OR (a."R2TerminationDisposition" IS NOT NULL AND a."R2TerminationDisposition"<>a."R2TerminalIntentDisposition") THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text; RETURN;
 END IF;
 disposition:=a."R2TerminalIntentDisposition";
 PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR k."AttemptId"=a."AttemptId" FOR UPDATE;
 PERFORM 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" FOR UPDATE;
 now_utc:=pg_catalog.clock_timestamp();
 nps_settled:=(a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=now_utc AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")));
 key_settled:=(EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((disposition='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (disposition='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"))))));
 IF a."R2TerminalOutcomeCode" IS NOT NULL THEN
  IF a."R2TerminalOutcomeCode"=a."R2TerminalIntentCode" AND a."R2TerminationDisposition"=disposition
   AND a."R2TerminatedAtUtc" IS NOT NULL AND (nps_settled OR key_settled) THEN
   RETURN QUERY SELECT 'ExistingMatch'::text,a."R2TerminalOutcomeCode"::text;
  ELSE RETURN QUERY SELECT 'StateConflict'::text,NULL::text; END IF;
  RETURN;
 END IF;
 IF NOT ((nps_settled AND disposition='TerminatedBeforeStart') OR key_settled) THEN
  RETURN QUERY SELECT 'CleanupPending'::text,NULL::text; RETURN; END IF;
 IF key_settled THEN
  termination_result:=tagekyc.raw_export_terminate_source_encryption_attempt(a."AttemptId",a."EncryptionAttemptRevision",a."Fence",disposition);
  IF termination_result NOT IN ('Terminated','ExistingMatch') OR termination_result IS NULL THEN
   RAISE EXCEPTION 'A3_R2_TERMINAL_FINALIZATION_TERMINATOR_CONFLICT'; END IF;
 END IF;
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-a3-r2-terminal-final-v1',true);
 UPDATE tagekyc.raw_export_source_encryption_attempts aa SET "R2TerminalOutcomeCode"=a."R2TerminalIntentCode"
 WHERE aa."AttemptId"=a."AttemptId";
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RETURN QUERY SELECT 'Finalized'::text,a."R2TerminalIntentCode"::text;
EXCEPTION WHEN OTHERS THEN
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RAISE;
END $ti$;
ALTER FUNCTION tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text) FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint) FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text) TO tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint) TO tagekyc_raw_export_reconciler;
""";

    private const string SameOwnerReentryOperations = """
CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN

  IF TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
   IF TG_OP='INSERT' THEN
    IF NEW."R2TerminalIntentCode" IS NOT NULL OR NEW."R2TerminalIntentDisposition" IS NOT NULL
     OR NEW."R2TerminalIntentAtUtc" IS NOT NULL THEN RAISE EXCEPTION 'A3_R2_TERMINAL_INTENT_INSERT_FORBIDDEN'; END IF;
   ELSIF TG_OP='UPDATE' THEN
    IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-r2-terminal-intent-v1' THEN
     IF OLD."R2TerminalIntentCode" IS NULL AND OLD."R2TerminalIntentDisposition" IS NULL AND OLD."R2TerminalIntentAtUtc" IS NULL
      AND NEW."R2TerminalIntentCode" IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
      AND NEW."R2TerminalIntentDisposition" IN ('Terminated','TerminatedBeforeStart')
      AND NEW."R2TerminalIntentAtUtc" IS NOT NULL AND isfinite(NEW."R2TerminalIntentAtUtc")
      AND NEW."R2TerminalIntentAtUtc">=OLD."CreatedAtUtc" AND NEW."R2TerminalIntentAtUtc"<=pg_catalog.clock_timestamp()
      AND OLD."R2TerminalOutcomeCode" IS NULL
      AND to_jsonb(NEW)-ARRAY['R2TerminalIntentCode','R2TerminalIntentDisposition','R2TerminalIntentAtUtc']
       =to_jsonb(OLD)-ARRAY['R2TerminalIntentCode','R2TerminalIntentDisposition','R2TerminalIntentAtUtc']
      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts a
 JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
 JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
 JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
 JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
 WHERE a."AttemptId"=OLD."AttemptId" AND h."CurrentEncryptionAttemptId"=a."AttemptId"
 AND h."Fence"=a."Fence" AND h."CustodyState"='Reserved' AND s."AuthorityKind"='SourceRetention'
 AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor() AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId"
 AND a."StagedCiphertextFingerprintSchemaVersion" IS NULL AND a."StagedCiphertextFingerprint" IS NULL AND a."StagedObjectCustodyId" IS NULL AND a."StagedObjectStateRevision" IS NULL AND a."StagedFromReservationRevision" IS NULL AND a."VerifiedPlaintextLength" IS NULL AND a."StagedCiphertextLength" IS NULL AND a."StagedCiphertextDigest" IS NULL AND a."StagedProviderReceiptDigest" IS NULL AND a."StagedVerificationEvidenceDigest" IS NULL AND a."StagedAtUtc" IS NULL
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId")
       AND ((a."R2TerminationDisposition" IS NULL AND a."R2TerminatedAtUtc" IS NULL
         AND NOT (NEW."R2TerminalIntentCode"='RECAPTURE_REQUIRED' AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"))))
        OR (NEW."R2TerminalIntentCode"='RECAPTURE_REQUIRED'
         AND a."R2TerminationDisposition"=NEW."R2TerminalIntentDisposition" AND a."R2TerminatedAtUtc" IS NOT NULL
         AND ((a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp() AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"))) OR (EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((NEW."R2TerminalIntentDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (NEW."R2TerminalIntentDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND o."State" IN ('Deleted','Quarantined')))))) AND EXISTS(SELECT 1 FROM tagekyc.capture_execution_bindings b
 WHERE b."CaptureExecutionBindingId"=s."RuntimeBindingId"
 AND r."EffectivePlaintextRetentionExpiresAtUtc" IS NOT NULL AND r."AbsoluteSourceExpiresAtUtc" IS NOT NULL
 AND s."ValidUntilUtc" IS NOT NULL AND b."ExecutionExpiresAtUtc" IS NOT NULL
 AND LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc")<=NEW."R2TerminalIntentAtUtc"))))
       THEN RETURN NEW; END IF;
     RAISE EXCEPTION 'A3_R2_TERMINAL_INTENT_WRITE_FORBIDDEN';
    END IF;
    IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-r2-terminal-final-v1' THEN
     IF OLD."R2TerminalOutcomeCode" IS NULL AND NEW."R2TerminalOutcomeCode"=OLD."R2TerminalIntentCode"
      AND OLD."R2TerminalIntentCode" IS NOT NULL AND OLD."R2TerminalIntentDisposition" IS NOT NULL AND OLD."R2TerminalIntentAtUtc" IS NOT NULL
      AND OLD."R2TerminationDisposition"=OLD."R2TerminalIntentDisposition" AND OLD."R2TerminatedAtUtc" IS NOT NULL
      AND (OLD."R2TerminatedAtUtc">=OLD."R2TerminalIntentAtUtc" OR OLD."R2TerminalIntentCode"='RECAPTURE_REQUIRED')
      AND to_jsonb(NEW)-'R2TerminalOutcomeCode'=to_jsonb(OLD)-'R2TerminalOutcomeCode'
      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts a
 JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
 JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
 JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
 JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
 WHERE a."AttemptId"=OLD."AttemptId" AND h."CurrentEncryptionAttemptId"=a."AttemptId"
 AND h."Fence"=a."Fence" AND h."CustodyState"='Reserved' AND s."AuthorityKind"='SourceRetention'
 AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor() AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId"
 AND a."StagedCiphertextFingerprintSchemaVersion" IS NULL AND a."StagedCiphertextFingerprint" IS NULL AND a."StagedObjectCustodyId" IS NULL AND a."StagedObjectStateRevision" IS NULL AND a."StagedFromReservationRevision" IS NULL AND a."VerifiedPlaintextLength" IS NULL AND a."StagedCiphertextLength" IS NULL AND a."StagedCiphertextDigest" IS NULL AND a."StagedProviderReceiptDigest" IS NULL AND a."StagedVerificationEvidenceDigest" IS NULL AND a."StagedAtUtc" IS NULL
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId") AND ((a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL
 AND a."OwnershipLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp() AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"))) OR (EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((NEW."R2TerminalIntentDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (NEW."R2TerminalIntentDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint")))))))) THEN RETURN NEW; END IF;
     RAISE EXCEPTION 'A3_R2_TERMINAL_FINAL_WRITE_FORBIDDEN';
    END IF;
    IF ROW(NEW."R2TerminalIntentCode",NEW."R2TerminalIntentDisposition",NEW."R2TerminalIntentAtUtc")
      IS DISTINCT FROM ROW(OLD."R2TerminalIntentCode",OLD."R2TerminalIntentDisposition",OLD."R2TerminalIntentAtUtc") THEN
     RAISE EXCEPTION 'A3_R2_TERMINAL_INTENT_IMMUTABLE';
    END IF;
    IF NEW."R2TerminalOutcomeCode" IS DISTINCT FROM OLD."R2TerminalOutcomeCode"
     AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_reservations r
      JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
      WHERE r."SourceArtifactId"=OLD."SourceArtifactId" AND s."AuthorityKind"='SourceRetention') THEN
     RAISE EXCEPTION 'A3_R2_TERMINAL_FINAL_WRITE_FORBIDDEN';
    END IF;
    IF OLD."R2TerminalIntentCode" IS NOT NULL
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1' THEN
     RAISE EXCEPTION 'A3_R2_TERMINAL_INTENT_STAGE_FORBIDDEN';
    END IF;
   END IF;
  END IF;
  IF current_user='tagekyc_raw_export_deployer'
   AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-same-owner-reentry-v1' THEN
   IF TG_OP='INSERT' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
    IF EXISTS(SELECT 1 FROM tagekyc.raw_export_source_head h
     JOIN tagekyc.raw_export_source_encryption_attempts a ON a."AttemptId"=h."CurrentEncryptionAttemptId"
     JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=h."SourceArtifactId"
     JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
     JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
     WHERE h."SourceArtifactId"=NEW."SourceArtifactId" AND h."CustodyState"='Reserved'
      AND a."SourceArtifactId"=h."SourceArtifactId" AND a."Fence"=h."Fence"
      AND a."EncryptionAttemptRevision"<9223372036854775807 AND h."Fence"<9223372036854775807
      AND h."ReservationRevision"<9223372036854775807
      AND NEW."EncryptionAttemptRevision"=a."EncryptionAttemptRevision"+1 AND NEW."Fence"=h."Fence"+1
      AND a."R2TerminatedAtUtc" IS NOT NULL
      AND a."R2TerminalOutcomeCode" IS NULL AND a."R2TerminalIntentCode" IS NULL AND a."R2TerminalIntentDisposition" IS NULL AND a."R2TerminalIntentAtUtc" IS NULL AND a."StagedCiphertextFingerprintSchemaVersion" IS NULL AND a."StagedCiphertextFingerprint" IS NULL AND a."StagedObjectCustodyId" IS NULL AND a."StagedObjectStateRevision" IS NULL AND a."StagedFromReservationRevision" IS NULL AND a."VerifiedPlaintextLength" IS NULL AND a."StagedCiphertextLength" IS NULL AND a."StagedCiphertextDigest" IS NULL AND a."StagedProviderReceiptDigest" IS NULL AND a."StagedVerificationEvidenceDigest" IS NULL AND a."StagedAtUtc" IS NULL
      AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL AND NEW."R2TerminalOutcomeCode" IS NULL AND NEW."R2TerminalIntentCode" IS NULL AND NEW."R2TerminalIntentDisposition" IS NULL AND NEW."R2TerminalIntentAtUtc" IS NULL AND NEW."StagedCiphertextFingerprintSchemaVersion" IS NULL AND NEW."StagedCiphertextFingerprint" IS NULL AND NEW."StagedObjectCustodyId" IS NULL AND NEW."StagedObjectStateRevision" IS NULL AND NEW."StagedFromReservationRevision" IS NULL AND NEW."VerifiedPlaintextLength" IS NULL AND NEW."StagedCiphertextLength" IS NULL AND NEW."StagedCiphertextDigest" IS NULL AND NEW."StagedProviderReceiptDigest" IS NULL AND NEW."StagedVerificationEvidenceDigest" IS NULL AND NEW."StagedAtUtc" IS NULL
      AND s."AuthorityKind"='SourceRetention' AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor()
      AND tagekyc.raw_export_retained_snapshot_is_current(s."AuthoritySnapshotId",NEW."CreatedAtUtc")
      AND NEW."CreatedAtUtc"<=pg_catalog.clock_timestamp() AND NEW."OwnershipLeaseExpiresAtUtc">NEW."CreatedAtUtc"
      AND NEW."OwnershipLeaseExpiresAtUtc"<=LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc")
      AND a."NonceStrategyId"='fixture-nonce-random96-v1' AND a."NonceDerivationSeedReferenceOrWrappedSeed"='none' AND a."SchemaVersion"=1
      AND NEW."AttemptId"<>a."AttemptId" AND NEW."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
      AND NEW."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
      AND NEW."AttemptId"<>NEW."AttemptKeyReservationId" AND NEW."AttemptId"<>NEW."ProvisionalObjectIdentity"
      AND NEW."AttemptKeyReservationId"<>NEW."ProvisionalObjectIdentity"
      AND to_jsonb(NEW)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc','R2TerminalOutcomeCode','R2TerminalIntentCode','R2TerminalIntentDisposition','R2TerminalIntentAtUtc','StagedCiphertextFingerprintSchemaVersion','StagedCiphertextFingerprint','StagedObjectCustodyId','StagedObjectStateRevision','StagedFromReservationRevision','VerifiedPlaintextLength','StagedCiphertextLength','StagedCiphertextDigest','StagedProviderReceiptDigest','StagedVerificationEvidenceDigest','StagedAtUtc','AttemptId','AttemptKeyReservationId','ProvisionalObjectIdentity','EncryptionAttemptRevision','Fence','EncryptionAttemptFingerprint','OwnershipLeaseExpiresAtUtc','CreatedAtUtc']=to_jsonb(a)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc','R2TerminalOutcomeCode','R2TerminalIntentCode','R2TerminalIntentDisposition','R2TerminalIntentAtUtc','StagedCiphertextFingerprintSchemaVersion','StagedCiphertextFingerprint','StagedObjectCustodyId','StagedObjectStateRevision','StagedFromReservationRevision','VerifiedPlaintextLength','StagedCiphertextLength','StagedCiphertextDigest','StagedProviderReceiptDigest','StagedVerificationEvidenceDigest','StagedAtUtc','AttemptId','AttemptKeyReservationId','ProvisionalObjectIdentity','EncryptionAttemptRevision','Fence','EncryptionAttemptFingerprint','OwnershipLeaseExpiresAtUtc','CreatedAtUtc']
      AND NEW."EncryptionAttemptFingerprint"=tagekyc.raw_export_c1_hash_canonical(
 'tip-88c1-encryption-attempt-v1',pg_catalog.encode(r."SourceReservationFingerprint",'hex'),
 NEW."EncryptionAttemptRevision"::text,NEW."Fence"::text,
 pg_catalog.replace(NEW."ProvisionalObjectIdentity"::text,'-',''),
 pg_catalog.replace(NEW."AttemptKeyReservationId"::text,'-',''),
 a."EncryptionSuiteId",a."EncryptionFramingVersion"::text,a."KeyProviderId",a."KekId",a."KekVersion"::text,a."KekFingerprint",
 a."NonceStrategyId",pg_catalog.encode(a."NonceDerivationSeedCommitment",'hex'),a."ChunkSize"::text,pg_catalog.encode(a."FramingParametersDigest",'hex'))
      AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId")
      AND ((a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."OwnershipLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp()
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")))
 OR (EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((a."R2TerminationDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (a."R2TerminationDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint")))))))) THEN RETURN NEW; END IF;
   ELSIF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_reservations' THEN
    IF to_jsonb(NEW)-'ReservationExpiresAtUtc'=to_jsonb(OLD)-'ReservationExpiresAtUtc'
     AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_head h
      JOIN tagekyc.raw_export_source_encryption_attempts a ON a."AttemptId"=h."CurrentEncryptionAttemptId"
      JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=NEW."AuthoritySnapshotId"
      JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
      WHERE h."SourceArtifactId"=NEW."SourceArtifactId" AND a."SourceArtifactId"=h."SourceArtifactId"
       AND h."CustodyState"='Reserved' AND a."Fence"=h."Fence" AND a."EncryptionAttemptRevision">1
       AND s."AuthorityKind"='SourceRetention' AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor()
       AND NEW."ReservationExpiresAtUtc"=a."OwnershipLeaseExpiresAtUtc"
       AND NEW."ReservationExpiresAtUtc"<=LEAST(NEW."EffectivePlaintextRetentionExpiresAtUtc",NEW."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc"))
      THEN RETURN NEW; END IF;
   END IF;
   RAISE EXCEPTION 'A3_REENTRY_CORE_WRITE_FORBIDDEN';
  END IF;
  IF TG_OP='INSERT' THEN
    IF current_user<>'tagekyc_raw_export_deployer'
       OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
      RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
    END IF;
    RETURN NEW;
  END IF;
IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-no-provider-start-v1'
             AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
             AND NEW."R2TerminationDisposition"='TerminatedBeforeStart' AND NEW."R2TerminatedAtUtc" IS NOT NULL
             AND OLD."OwnershipLeaseExpiresAtUtc"<=NEW."R2TerminatedAtUtc"
             AND NEW."R2TerminatedAtUtc"<=pg_catalog.clock_timestamp()
             AND OLD."R2TerminalOutcomeCode" IS NULL AND OLD."R2TerminalIntentCode" IS NULL
             AND OLD."R2TerminalIntentDisposition" IS NULL AND OLD."R2TerminalIntentAtUtc" IS NULL
             AND OLD."StagedAtUtc" IS NULL
             AND to_jsonb(NEW)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
                  =to_jsonb(OLD)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
             AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_head h
              JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=h."SourceArtifactId"
              JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
              JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
              WHERE h."SourceArtifactId"=OLD."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=OLD."AttemptId"
               AND h."Fence"=OLD."Fence" AND h."CustodyState"='Reserved'
               AND s."AuthorityKind"='SourceRetention'
               AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor()
               AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId")
             AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=OLD."SourceArtifactId")
             AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
     WHERE k."AttemptKeyReservationId"=OLD."AttemptKeyReservationId" OR k."AttemptId"=OLD."AttemptId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
     WHERE e."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations p
     WHERE p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects p
     WHERE p."AttemptId"=OLD."AttemptId" OR p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId"
       OR p."ProvisionalObjectIdentity"=OLD."ProvisionalObjectIdentity")) THEN RETURN NEW; END IF;

    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
       AND NEW."R2TerminatedAtUtc" IS NOT NULL
       AND (OLD."R2TerminalIntentCode" IS NULL OR OLD."R2TerminalIntentDisposition"=NEW."R2TerminationDisposition")
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion",
           NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."StagedObjectCustodyId",
           NEW."StagedObjectStateRevision",NEW."StagedFromReservationRevision",NEW."VerifiedPlaintextLength",
           NEW."StagedCiphertextLength",NEW."StagedCiphertextDigest",NEW."StagedProviderReceiptDigest",
           NEW."StagedVerificationEvidenceDigest",NEW."StagedAtUtc")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion",
           OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."StagedObjectCustodyId",
           OLD."StagedObjectStateRevision",OLD."StagedFromReservationRevision",OLD."VerifiedPlaintextLength",
           OLD."StagedCiphertextLength",OLD."StagedCiphertextDigest",OLD."StagedProviderReceiptDigest",
           OLD."StagedVerificationEvidenceDigest",OLD."StagedAtUtc") THEN
      RETURN NEW;
    END IF;
    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL
       AND OLD."StagedCiphertextFingerprintSchemaVersion" IS NULL
       AND OLD."StagedCiphertextFingerprint" IS NULL AND OLD."StagedObjectCustodyId" IS NULL
       AND OLD."StagedObjectStateRevision" IS NULL AND OLD."StagedFromReservationRevision" IS NULL
       AND OLD."VerifiedPlaintextLength" IS NULL AND OLD."StagedCiphertextLength" IS NULL
       AND OLD."StagedCiphertextDigest" IS NULL AND OLD."StagedProviderReceiptDigest" IS NULL
       AND OLD."StagedVerificationEvidenceDigest" IS NULL AND OLD."StagedAtUtc" IS NULL
       AND NEW."StagedCiphertextFingerprintSchemaVersion"=2
       AND pg_catalog.octet_length(NEW."StagedCiphertextFingerprint")=32
       AND NEW."StagedObjectCustodyId" IS NOT NULL AND NEW."StagedObjectStateRevision">=1
       AND NEW."StagedFromReservationRevision">=1 AND NEW."VerifiedPlaintextLength">=1
       AND NEW."StagedCiphertextLength" BETWEEN 1 AND 134217728
       AND pg_catalog.octet_length(NEW."StagedCiphertextDigest")=32
       AND pg_catalog.octet_length(NEW."StagedProviderReceiptDigest")=32
       AND pg_catalog.octet_length(NEW."StagedVerificationEvidenceDigest")=32
       AND NEW."StagedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."R2TerminationDisposition",NEW."R2TerminatedAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."R2TerminationDisposition",OLD."R2TerminatedAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
      RETURN NEW;
    END IF;
  END IF;
  IF TG_OP='UPDATE' THEN
    RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
  END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
END $guard$;

CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN
  IF current_user='tagekyc_raw_export_deployer'
   AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-same-owner-reentry-v1'
   AND TG_OP='UPDATE' AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Reserved'
   AND OLD."ReservationRevision"<9223372036854775807 AND OLD."Fence"<9223372036854775807
   AND NEW."ReservationRevision"=OLD."ReservationRevision"+1 AND NEW."Fence"=OLD."Fence"+1
   AND to_jsonb(NEW)-ARRAY['CurrentEncryptionAttemptId','ReservationRevision','Fence']
       =to_jsonb(OLD)-ARRAY['CurrentEncryptionAttemptId','ReservationRevision','Fence']
   AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts a
    JOIN tagekyc.raw_export_source_encryption_attempts predecessor ON predecessor."AttemptId"=OLD."CurrentEncryptionAttemptId"
    WHERE a."AttemptId"=NEW."CurrentEncryptionAttemptId" AND a."AttemptId"<>predecessor."AttemptId"
     AND a."SourceArtifactId"=NEW."SourceArtifactId" AND predecessor."SourceArtifactId"=NEW."SourceArtifactId"
     AND predecessor."Fence"=OLD."Fence" AND predecessor."EncryptionAttemptRevision"<9223372036854775807
     AND a."EncryptionAttemptRevision"=predecessor."EncryptionAttemptRevision"+1 AND a."Fence"=NEW."Fence"
     AND a."R2TerminationDisposition" IS NULL AND a."R2TerminalIntentCode" IS NULL AND a."R2TerminalOutcomeCode" IS NULL
     AND predecessor."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart') AND predecessor."R2TerminatedAtUtc" IS NOT NULL)
    THEN RETURN NEW; END IF;
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='complete-r1'
     AND TG_OP<>'DELETE' THEN
    RETURN NEW;
  END IF;
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
     AND TG_OP='UPDATE'
     AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Staged'
     AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
     AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
         IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN
    RETURN NEW;
  END IF;
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r5-publish-v1'
     AND TG_OP='UPDATE' AND OLD."CustodyState"='Staged' AND NEW."CustodyState"='Available'
     AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
     AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
         IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN RETURN NEW; END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
END $guard$;

CREATE FUNCTION tagekyc.raw_export_reenter_retained_source(
 p_source_artifact_id uuid,p_expected_attempt_id uuid,p_expected_attempt_revision bigint,
 p_expected_reservation_revision bigint,p_expected_fence bigint,p_runtime_binding_id uuid,
 p_attempt_deadline_seconds integer,p_safety_margin_milliseconds integer,p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text,"SourceArtifactId" uuid,"AttemptKeyReservationId" uuid,"AttemptId" uuid,
 "ExpectedEncryptionAttemptRevision" bigint,"ExpectedFence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $reentry$
DECLARE probe record; locked record; a tagekyc.raw_export_source_encryption_attempts%ROWTYPE;
 h tagekyc.raw_export_source_head%ROWTYPE; r tagekyc.raw_export_source_reservations%ROWTYPE;
 s tagekyc.raw_export_authority_snapshots%ROWTYPE; b tagekyc.capture_execution_bindings%ROWTYPE;
 c tagekyc.raw_export_source_ingress_claims%ROWTYPE; actor_id uuid; now_utc timestamptz; horizon timestamptz;
 next_attempt uuid; next_key uuid; next_object uuid; next_revision bigint; next_fence bigint; next_head_revision bigint;
 next_lease timestamptz; next_fingerprint bytea; requirement text;
 previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
 IF p_source_artifact_id IS NULL OR p_source_artifact_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_expected_attempt_id IS NULL OR p_expected_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_runtime_binding_id IS NULL OR p_runtime_binding_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_expected_attempt_revision IS NULL OR p_expected_attempt_revision<1
  OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
  OR p_expected_fence IS NULL OR p_expected_fence<1
  OR p_attempt_deadline_seconds IS NULL OR p_attempt_deadline_seconds NOT BETWEEN 1 AND 3600
  OR p_safety_margin_milliseconds IS NULL OR p_safety_margin_milliseconds NOT BETWEEN 1 AND 30000
  OR p_ownership_lease_seconds IS NULL OR p_ownership_lease_seconds NOT BETWEEN 1 AND 3600 THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 actor_id:=tagekyc.raw_export_current_actor();
 SELECT rr."AuthoritySnapshotId",cc."IngressClaimId",cc."VerificationSessionId",cc."ClientApplicationId",
  cc."AuthenticatedPrincipalId",ss."CustodyPrincipalId",ss."AuthorityKind",ss."RuntimeBindingId",
  ss."CaptureAcceptanceId",ss."RawClass",ss."ConsentPolicyId",ss."ConsentPolicyVersion",
  cb."ConsentReferenceId",cr."ExternalConsentArtifactRef"
 INTO probe FROM tagekyc.raw_export_source_reservations rr
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 JOIN tagekyc.raw_source_consent_bindings cb ON cb."ConsentBindingId"=ss."ConsentBindingId"
 JOIN tagekyc.raw_source_consent_references cr ON cr."ConsentReferenceId"=cb."ConsentReferenceId"
 WHERE rr."SourceArtifactId"=p_source_artifact_id;
 IF NOT FOUND OR probe."AuthorityKind" IS DISTINCT FROM 'SourceRetention'
  OR probe."RuntimeBindingId" IS DISTINCT FROM p_runtime_binding_id
  OR probe."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id OR probe."CustodyPrincipalId" IS DISTINCT FROM actor_id THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 PERFORM 1 FROM tagekyc.verification_sessions vs WHERE vs."Id"=probe."VerificationSessionId"
  AND vs."ClientApplicationId"=probe."ClientApplicationId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
  'tip88c1:a3:consent-reference:'||probe."ClientApplicationId"::text||':'||probe."ExternalConsentArtifactRef",0));
 PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
 PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
  'tip88b1:lifecycle:'||probe."ConsentPolicyId"::text||':'||probe."ConsentPolicyVersion"::text));
 FOR requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
  WHERE req."PolicyId"=probe."ConsentPolicyId" AND req."PolicyVersion"=probe."ConsentPolicyVersion"
   AND req."RequirementType"<>'ConsentArtifact' ORDER BY req."RequirementType" COLLATE "C" LOOP
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88b1:fulfillment:'||probe."ConsentPolicyId"::text||':'||probe."ConsentPolicyVersion"::text||':'||requirement));
 END LOOP;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
  'tip88c1:b2-authority:'||probe."ClientApplicationId"::text||':'||probe."VerificationSessionId"::text||':'||
  probe."CaptureAcceptanceId"::text||':'||probe."RawClass"));
 SELECT * INTO r FROM tagekyc.raw_export_source_reservations rr WHERE rr."SourceArtifactId"=p_source_artifact_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 SELECT * INTO s FROM tagekyc.raw_export_authority_snapshots ss WHERE ss."AuthoritySnapshotId"=r."AuthoritySnapshotId";
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 SELECT * INTO c FROM tagekyc.raw_export_source_ingress_claims cc WHERE cc."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 PERFORM 1 FROM tagekyc.raw_export_source_ingress_claim_aliases ca WHERE ca."IngressClaimId"=c."IngressClaimId"
  ORDER BY ca."IngressClaimAliasId" FOR UPDATE;
 SELECT * INTO h FROM tagekyc.raw_export_source_head hh WHERE hh."SourceArtifactId"=p_source_artifact_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts aa WHERE aa."AttemptId"=h."CurrentEncryptionAttemptId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 SELECT * INTO b FROM tagekyc.capture_execution_bindings bb WHERE bb."CaptureExecutionBindingId"=s."RuntimeBindingId";
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations k WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR k."AttemptId"=a."AttemptId" FOR UPDATE;
 PERFORM 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
  OR o."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" FOR UPDATE;
 SELECT rr."AuthoritySnapshotId",cc."IngressClaimId",cc."VerificationSessionId",cc."ClientApplicationId",
  cc."AuthenticatedPrincipalId",ss."CustodyPrincipalId",ss."AuthorityKind",ss."RuntimeBindingId",
  ss."CaptureAcceptanceId",ss."RawClass",ss."ConsentPolicyId",ss."ConsentPolicyVersion",
  cb."ConsentReferenceId",cr."ExternalConsentArtifactRef"
 INTO locked FROM tagekyc.raw_export_source_reservations rr
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 JOIN tagekyc.raw_source_consent_bindings cb ON cb."ConsentBindingId"=ss."ConsentBindingId"
 JOIN tagekyc.raw_source_consent_references cr ON cr."ConsentReferenceId"=cb."ConsentReferenceId"
 WHERE rr."SourceArtifactId"=p_source_artifact_id;
 IF to_jsonb(probe) IS DISTINCT FROM to_jsonb(locked)
  OR h."CurrentEncryptionAttemptId" IS DISTINCT FROM p_expected_attempt_id
  OR a."SourceArtifactId" IS DISTINCT FROM p_source_artifact_id
  OR a."EncryptionAttemptRevision" IS DISTINCT FROM p_expected_attempt_revision
  OR h."ReservationRevision" IS DISTINCT FROM p_expected_reservation_revision
  OR h."Fence" IS DISTINCT FROM p_expected_fence OR a."Fence" IS DISTINCT FROM p_expected_fence
  OR b."PrincipalId" IS DISTINCT FROM actor_id OR b."ClientApplicationId" IS DISTINCT FROM c."ClientApplicationId"
  OR b."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"
  OR s."RuntimeBindingId" IS DISTINCT FROM p_runtime_binding_id THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF h."CustodyState"<>'Reserved' OR a."R2TerminationDisposition" IS NULL OR a."R2TerminatedAtUtc" IS NULL
  OR a."R2TerminalOutcomeCode" IS NOT NULL OR a."R2TerminalIntentCode" IS NOT NULL OR a."R2TerminalIntentDisposition" IS NOT NULL OR a."R2TerminalIntentAtUtc" IS NOT NULL OR a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL OR a."StagedAtUtc" IS NOT NULL
  OR EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=p_source_artifact_id) THEN
  RETURN QUERY SELECT 'ReservationBusy'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 now_utc:=pg_catalog.clock_timestamp();
 IF NOT ((a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."OwnershipLeaseExpiresAtUtc"<=now_utc
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."AttemptId"=a."AttemptId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId")
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."AttemptId"=a."AttemptId" OR x."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR x."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")))
 OR (EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
 WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
 AND k."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
 AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))
 AND ((a."R2TerminationDisposition"='TerminatedBeforeStart'
 AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId")
 OR EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o WHERE o."AttemptId"=a."AttemptId"
 AND o."SourceArtifactId"=a."SourceArtifactId" AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
 AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity" AND o."State"='NoObjectEstablished')))
 OR (a."R2TerminationDisposition"='Terminated' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
 WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
 AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
 AND (o."State" IN ('Deleted','Quarantined') OR (a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH' AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence' AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL AND o."OutcomeObservedAtUtc" IS NOT NULL AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision" AND o."AttemptFence"=a."Fence" AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"))))))) THEN
  RETURN QUERY SELECT 'ReservationBusy'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF r."EffectivePlaintextRetentionExpiresAtUtc" IS NULL OR r."AbsoluteSourceExpiresAtUtc" IS NULL
  OR s."ValidUntilUtc" IS NULL OR b."ExecutionExpiresAtUtc" IS NULL
  OR NOT isfinite(r."EffectivePlaintextRetentionExpiresAtUtc") OR NOT isfinite(r."AbsoluteSourceExpiresAtUtc")
  OR NOT isfinite(s."ValidUntilUtc") OR NOT isfinite(b."ExecutionExpiresAtUtc")
  OR r."PlaintextRetentionStartedAtUtc" IS NULL OR r."PlaintextRetentionExpiresAtUtc" IS NULL OR r."PlaintextRetentionBudgetSeconds"<1
  OR a."NonceStrategyId"<>'fixture-nonce-random96-v1' OR a."NonceDerivationSeedReferenceOrWrappedSeed"<>'none' OR a."SchemaVersion"<>1 THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 horizon:=LEAST(r."EffectivePlaintextRetentionExpiresAtUtc",r."AbsoluteSourceExpiresAtUtc",s."ValidUntilUtc",b."ExecutionExpiresAtUtc");
 IF horizon<=now_utc THEN RETURN QUERY SELECT 'RecaptureRequired'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
 IF NOT tagekyc.raw_export_retained_snapshot_is_current(s."AuthoritySnapshotId",now_utc)
  OR horizon-now_utc<pg_catalog.make_interval(secs=>p_attempt_deadline_seconds+p_safety_margin_milliseconds/1000.0) THEN
  RETURN QUERY SELECT 'ReservationBusy'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF a."EncryptionAttemptRevision"=9223372036854775807 OR h."Fence"=9223372036854775807 OR h."ReservationRevision"=9223372036854775807 THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 next_revision:=a."EncryptionAttemptRevision"+1; next_fence:=h."Fence"+1; next_head_revision:=h."ReservationRevision"+1;
 next_attempt:=pg_catalog.gen_random_uuid(); next_key:=pg_catalog.gen_random_uuid(); next_object:=pg_catalog.gen_random_uuid();
 IF next_attempt=next_key OR next_attempt=next_object OR next_key=next_object
  OR next_attempt=a."AttemptId" OR next_key=a."AttemptKeyReservationId" OR next_object=a."ProvisionalObjectIdentity" THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 next_lease:=LEAST(now_utc+pg_catalog.make_interval(secs=>p_ownership_lease_seconds),horizon);
 next_fingerprint:=tagekyc.raw_export_c1_hash_canonical(
 'tip-88c1-encryption-attempt-v1',pg_catalog.encode(r."SourceReservationFingerprint",'hex'),
 next_revision::text,next_fence::text,
 pg_catalog.replace(next_object::text,'-',''),
 pg_catalog.replace(next_key::text,'-',''),
 a."EncryptionSuiteId",a."EncryptionFramingVersion"::text,a."KeyProviderId",a."KekId",a."KekVersion"::text,a."KekFingerprint",
 a."NonceStrategyId",pg_catalog.encode(a."NonceDerivationSeedCommitment",'hex'),a."ChunkSize"::text,pg_catalog.encode(a."FramingParametersDigest",'hex'));
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-a3-same-owner-reentry-v1',true);
 INSERT INTO tagekyc.raw_export_source_encryption_attempts(
  "AttemptId","SourceArtifactId","EncryptionAttemptRevision","Fence","ProvisionalObjectIdentity","AttemptKeyReservationId",
  "KeyProviderId","KekId","KekVersion","KekFingerprint","EncryptionSuiteId","EncryptionFramingVersion",
  "NonceStrategyId","NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize","FramingParametersDigest",
  "EncryptionAttemptFingerprint","OwnershipLeaseExpiresAtUtc","CreatedAtUtc","SchemaVersion",
  "R2TerminationDisposition","R2TerminatedAtUtc","R2TerminalOutcomeCode","R2TerminalIntentCode","R2TerminalIntentDisposition","R2TerminalIntentAtUtc","StagedCiphertextFingerprintSchemaVersion","StagedCiphertextFingerprint","StagedObjectCustodyId","StagedObjectStateRevision","StagedFromReservationRevision","VerifiedPlaintextLength","StagedCiphertextLength","StagedCiphertextDigest","StagedProviderReceiptDigest","StagedVerificationEvidenceDigest","StagedAtUtc")
 VALUES(next_attempt,p_source_artifact_id,next_revision,next_fence,next_object,next_key,
  a."KeyProviderId",a."KekId",a."KekVersion",a."KekFingerprint",a."EncryptionSuiteId",a."EncryptionFramingVersion",
  a."NonceStrategyId",a."NonceDerivationSeedReferenceOrWrappedSeed",a."NonceDerivationSeedCommitment",a."ChunkSize",a."FramingParametersDigest",
  next_fingerprint,next_lease,now_utc,a."SchemaVersion",NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL);
 UPDATE tagekyc.raw_export_source_head hh SET "CurrentEncryptionAttemptId"=next_attempt,
  "ReservationRevision"=next_head_revision,"Fence"=next_fence
 WHERE hh."SourceArtifactId"=p_source_artifact_id AND hh."CurrentEncryptionAttemptId"=p_expected_attempt_id
  AND hh."ReservationRevision"=p_expected_reservation_revision AND hh."Fence"=p_expected_fence AND hh."CustodyState"='Reserved';
 IF NOT FOUND THEN RAISE EXCEPTION 'A3_REENTRY_HEAD_CAS_FAILED'; END IF;
 UPDATE tagekyc.raw_export_source_reservations rr SET "ReservationExpiresAtUtc"=next_lease
 WHERE rr."SourceArtifactId"=p_source_artifact_id AND rr."SourceReservationFingerprint"=r."SourceReservationFingerprint"
  AND rr."ReservationExpiresAtUtc"=r."ReservationExpiresAtUtc";
 IF NOT FOUND THEN RAISE EXCEPTION 'A3_REENTRY_RESERVATION_CAS_FAILED'; END IF;
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RETURN QUERY SELECT 'SameOwnerReentry'::text,p_source_artifact_id,next_key,next_attempt,next_revision,next_fence;
EXCEPTION WHEN OTHERS THEN
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RAISE;
END $reentry$;
ALTER FUNCTION tagekyc.raw_export_reenter_retained_source(uuid,uuid,bigint,bigint,bigint,uuid,integer,integer,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_reenter_retained_source(uuid,uuid,bigint,bigint,bigint,uuid,integer,integer,integer)
 FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator,
 tagekyc_raw_export_claim_broker,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;

CREATE OR REPLACE FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
 p_client_application_id uuid,p_producer_id text,p_capture_agent_instance_id text,p_ingress_idempotency_key text,
 p_claim_evaluation_id uuid,p_claim_evaluation_revision bigint,p_claim_evaluation_fence bigint,p_token_variant text,
 p_token_expires_at_utc timestamptz,p_claim_evaluation_token text,p_producer_envelope_fingerprint bytea,
 p_commitment_schema integer,p_commitment_key_id text,p_commitment_key_version integer,p_content_commitment bytea,
 p_subject_token_schema integer,p_subject_token_key_id text,p_subject_token_key_version integer,p_subject_token bytea,
 p_claimed_plaintext_length bigint,p_media_type text,p_captured_at_utc timestamptz,p_retention_started_at_utc timestamptz,
 p_retention_expires_at_utc timestamptz,p_retention_budget_seconds integer,p_storage_profile_id text,p_source_profile_id text,
 p_source_profile_version integer,p_encryption_suite_id text,p_encryption_framing_version integer,p_nonce_strategy_id text,
 p_nonce_seed_commitment bytea,p_chunk_size integer,p_framing_parameters_digest bytea,p_key_provider_id text,p_kek_id text,
 p_kek_version integer,p_kek_fingerprint text,p_max_continuation_seconds integer,p_attempt_deadline_seconds integer,
 p_safety_margin_milliseconds integer,p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text,"SourceArtifactId" uuid,"AttemptKeyReservationId" uuid,"AttemptId" uuid,
 "ExpectedEncryptionAttemptRevision" bigint,"ExpectedFence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $handoff$
DECLARE completed record; head_row tagekyc.raw_export_source_head%ROWTYPE;
 attempt_row tagekyc.raw_export_source_encryption_attempts%ROWTYPE; publication_row tagekyc.raw_export_source_publications%ROWTYPE;
 provisional_row tagekyc.raw_export_provisional_objects%ROWTYPE;
 retained_binding uuid; retained_kind text; reentered record; nps_result record;
BEGIN
 SELECT * INTO completed FROM tagekyc.complete_raw_export_source_ingress_claim(
  p_client_application_id,p_producer_id,p_capture_agent_instance_id,p_ingress_idempotency_key,
  p_claim_evaluation_id,p_claim_evaluation_revision,p_claim_evaluation_fence,p_token_variant,p_token_expires_at_utc,
  p_claim_evaluation_token,p_producer_envelope_fingerprint,p_commitment_schema,p_commitment_key_id,p_commitment_key_version,
  p_content_commitment,p_subject_token_schema,p_subject_token_key_id,p_subject_token_key_version,p_subject_token,
  p_claimed_plaintext_length,p_media_type,p_captured_at_utc,p_retention_started_at_utc,p_retention_expires_at_utc,
  p_retention_budget_seconds,p_storage_profile_id,p_source_profile_id,p_source_profile_version,p_encryption_suite_id,
  p_encryption_framing_version,p_nonce_strategy_id,p_nonce_seed_commitment,p_chunk_size,p_framing_parameters_digest,
  p_key_provider_id,p_kek_id,p_kek_version,p_kek_fingerprint,p_max_continuation_seconds,p_attempt_deadline_seconds,
  p_safety_margin_milliseconds,p_ownership_lease_seconds);
 IF completed."OutcomeCode" NOT IN ('NewReservation','ExistingMatch') THEN
  RETURN QUERY SELECT completed."OutcomeCode",completed."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF completed."SourceArtifactId" IS NULL THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
 SELECT * INTO STRICT head_row FROM tagekyc.raw_export_source_head h
  WHERE h."SourceArtifactId"=completed."SourceArtifactId" FOR UPDATE;
 SELECT * INTO STRICT attempt_row FROM tagekyc.raw_export_source_encryption_attempts a
  WHERE a."AttemptId"=head_row."CurrentEncryptionAttemptId" AND a."SourceArtifactId"=head_row."SourceArtifactId" FOR UPDATE;
 SELECT * INTO publication_row FROM tagekyc.raw_export_source_publications p
  WHERE p."SourceArtifactId"=head_row."SourceArtifactId" ORDER BY p."PublicationRevision" DESC LIMIT 1 FOR UPDATE;
 SELECT * INTO provisional_row FROM tagekyc.raw_export_provisional_objects o
  WHERE o."SourceArtifactId"=head_row."SourceArtifactId" AND o."AttemptId"=attempt_row."AttemptId"
  ORDER BY o."StateRevision" DESC LIMIT 1 FOR UPDATE;
 IF completed."OutcomeCode"='NewReservation' THEN
  IF head_row."CustodyState"<>'Reserved' OR attempt_row."R2TerminationDisposition" IS NOT NULL
   OR attempt_row."R2TerminalOutcomeCode" IS NOT NULL OR attempt_row."EncryptionAttemptRevision"<>head_row."ReservationRevision"
   OR attempt_row."Fence"<>head_row."Fence" THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
  RETURN QUERY SELECT 'NewReservation'::text,head_row."SourceArtifactId",attempt_row."AttemptKeyReservationId",
   attempt_row."AttemptId",attempt_row."EncryptionAttemptRevision",attempt_row."Fence"; RETURN;
 END IF;
 IF publication_row."PublicationState"='Available' THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_ALREADY_AVAILABLE'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF head_row."CustodyState"='Staged' OR publication_row."PublicationState"='Committed'
    OR attempt_row."StagedAtUtc" IS NOT NULL OR provisional_row."State" IN ('ObjectPresentPendingVerification','VerifiedCompleted') THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESUME_PENDING'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 -- The inner complete above remains the sole token/admission comparison owner.
 -- Its retained session/reference/policy prefix is still held by this same B.
 SELECT s."AuthorityKind",s."RuntimeBindingId" INTO retained_kind,retained_binding
 FROM tagekyc.raw_export_source_reservations r JOIN tagekyc.raw_export_authority_snapshots s
  ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId" WHERE r."SourceArtifactId"=head_row."SourceArtifactId";
 IF retained_kind='SourceRetention' AND attempt_row."R2TerminalOutcomeCode" IS NULL
  AND attempt_row."R2TerminalIntentCode" IS NULL AND attempt_row."R2TerminalIntentDisposition" IS NULL
  AND attempt_row."R2TerminalIntentAtUtc" IS NULL AND head_row."CustodyState"='Reserved' THEN
  IF NOT EXISTS(SELECT 1 FROM tagekyc.capture_execution_bindings b
   JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=head_row."SourceArtifactId"
   JOIN tagekyc.raw_export_source_ingress_claim_aliases alias ON alias."IngressClaimId"=r."IngressClaimId"
   WHERE b."CaptureExecutionBindingId"=retained_binding
    AND alias."ClientApplicationId"=p_client_application_id
    AND alias."ProducerId"=p_producer_id AND alias."CaptureAgentInstanceId"=p_capture_agent_instance_id
    AND alias."IngressIdempotencyKey"=p_ingress_idempotency_key::uuid
    AND pg_catalog.replace(b."CaptureAgentId"::text,'-','')=alias."ProducerId"
    AND pg_catalog.replace(b."DeviceInstallationId"::text,'-','')=alias."CaptureAgentInstanceId") THEN
   RAISE EXCEPTION 'A3_REENTRY_RUNTIME_BINDING_MISMATCH';
  END IF;
  IF attempt_row."R2TerminationDisposition" IS NULL
   AND attempt_row."OwnershipLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp() THEN
   SELECT * INTO nps_result FROM tagekyc.raw_export_terminate_retained_r2_before_provider_start(
    head_row."SourceArtifactId",attempt_row."AttemptId",attempt_row."EncryptionAttemptRevision",attempt_row."Fence");
   IF nps_result."Outcome" NOT IN ('TerminatedBeforeStart','ExistingMatch','LeaseLive','ProviderEvidencePresent') THEN
    RAISE EXCEPTION 'A3_REENTRY_NO_START_STATE_INVALID';
   END IF;
  END IF;
  SELECT * INTO reentered FROM tagekyc.raw_export_reenter_retained_source(
   head_row."SourceArtifactId",attempt_row."AttemptId",attempt_row."EncryptionAttemptRevision",
   head_row."ReservationRevision",head_row."Fence",retained_binding,
   p_attempt_deadline_seconds,p_safety_margin_milliseconds,p_ownership_lease_seconds);
  IF reentered."OutcomeCode"='SameOwnerReentry' THEN
   RETURN QUERY SELECT reentered."OutcomeCode",reentered."SourceArtifactId",reentered."AttemptKeyReservationId",
    reentered."AttemptId",reentered."ExpectedEncryptionAttemptRevision",reentered."ExpectedFence"; RETURN;
  END IF;
  IF reentered."OutcomeCode" NOT IN ('ReservationBusy','RecaptureRequired') THEN
   RAISE EXCEPTION 'A3_REENTRY_STATE_INVALID';
  END IF;
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESERVATION_BUSY'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF attempt_row."R2TerminationDisposition" IS NULL OR attempt_row."R2TerminalOutcomeCode" IS NULL THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESERVATION_BUSY'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 RETURN QUERY SELECT attempt_row."R2TerminalOutcomeCode",head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint;
END;$handoff$;
""";

    private const string SameOwnerReentryOperationsRestore = """
CREATE OR REPLACE FUNCTION tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
 p_client_application_id uuid,p_producer_id text,p_capture_agent_instance_id text,p_ingress_idempotency_key text,
 p_claim_evaluation_id uuid,p_claim_evaluation_revision bigint,p_claim_evaluation_fence bigint,p_token_variant text,
 p_token_expires_at_utc timestamptz,p_claim_evaluation_token text,p_producer_envelope_fingerprint bytea,
 p_commitment_schema integer,p_commitment_key_id text,p_commitment_key_version integer,p_content_commitment bytea,
 p_subject_token_schema integer,p_subject_token_key_id text,p_subject_token_key_version integer,p_subject_token bytea,
 p_claimed_plaintext_length bigint,p_media_type text,p_captured_at_utc timestamptz,p_retention_started_at_utc timestamptz,
 p_retention_expires_at_utc timestamptz,p_retention_budget_seconds integer,p_storage_profile_id text,p_source_profile_id text,
 p_source_profile_version integer,p_encryption_suite_id text,p_encryption_framing_version integer,p_nonce_strategy_id text,
 p_nonce_seed_commitment bytea,p_chunk_size integer,p_framing_parameters_digest bytea,p_key_provider_id text,p_kek_id text,
 p_kek_version integer,p_kek_fingerprint text,p_max_continuation_seconds integer,p_attempt_deadline_seconds integer,
 p_safety_margin_milliseconds integer,p_ownership_lease_seconds integer)
RETURNS TABLE("OutcomeCode" text,"SourceArtifactId" uuid,"AttemptKeyReservationId" uuid,"AttemptId" uuid,
 "ExpectedEncryptionAttemptRevision" bigint,"ExpectedFence" bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $handoff$
DECLARE completed record; head_row tagekyc.raw_export_source_head%ROWTYPE;
 attempt_row tagekyc.raw_export_source_encryption_attempts%ROWTYPE; publication_row tagekyc.raw_export_source_publications%ROWTYPE;
 provisional_row tagekyc.raw_export_provisional_objects%ROWTYPE;
BEGIN
 SELECT * INTO completed FROM tagekyc.complete_raw_export_source_ingress_claim(
  p_client_application_id,p_producer_id,p_capture_agent_instance_id,p_ingress_idempotency_key,
  p_claim_evaluation_id,p_claim_evaluation_revision,p_claim_evaluation_fence,p_token_variant,p_token_expires_at_utc,
  p_claim_evaluation_token,p_producer_envelope_fingerprint,p_commitment_schema,p_commitment_key_id,p_commitment_key_version,
  p_content_commitment,p_subject_token_schema,p_subject_token_key_id,p_subject_token_key_version,p_subject_token,
  p_claimed_plaintext_length,p_media_type,p_captured_at_utc,p_retention_started_at_utc,p_retention_expires_at_utc,
  p_retention_budget_seconds,p_storage_profile_id,p_source_profile_id,p_source_profile_version,p_encryption_suite_id,
  p_encryption_framing_version,p_nonce_strategy_id,p_nonce_seed_commitment,p_chunk_size,p_framing_parameters_digest,
  p_key_provider_id,p_kek_id,p_kek_version,p_kek_fingerprint,p_max_continuation_seconds,p_attempt_deadline_seconds,
  p_safety_margin_milliseconds,p_ownership_lease_seconds);
 IF completed."OutcomeCode" NOT IN ('NewReservation','ExistingMatch') THEN
  RETURN QUERY SELECT completed."OutcomeCode",completed."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF completed."SourceArtifactId" IS NULL THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
 SELECT * INTO STRICT head_row FROM tagekyc.raw_export_source_head h
  WHERE h."SourceArtifactId"=completed."SourceArtifactId" FOR UPDATE;
 SELECT * INTO STRICT attempt_row FROM tagekyc.raw_export_source_encryption_attempts a
  WHERE a."AttemptId"=head_row."CurrentEncryptionAttemptId" AND a."SourceArtifactId"=head_row."SourceArtifactId" FOR UPDATE;
 SELECT * INTO publication_row FROM tagekyc.raw_export_source_publications p
  WHERE p."SourceArtifactId"=head_row."SourceArtifactId" ORDER BY p."PublicationRevision" DESC LIMIT 1 FOR UPDATE;
 SELECT * INTO provisional_row FROM tagekyc.raw_export_provisional_objects o
  WHERE o."SourceArtifactId"=head_row."SourceArtifactId" AND o."AttemptId"=attempt_row."AttemptId"
  ORDER BY o."StateRevision" DESC LIMIT 1 FOR UPDATE;
 IF completed."OutcomeCode"='NewReservation' THEN
  IF head_row."CustodyState"<>'Reserved' OR attempt_row."R2TerminationDisposition" IS NOT NULL
   OR attempt_row."R2TerminalOutcomeCode" IS NOT NULL OR attempt_row."EncryptionAttemptRevision"<>head_row."ReservationRevision"
   OR attempt_row."Fence"<>head_row."Fence" THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_HANDOFF_STATE_INVALID'; END IF;
  RETURN QUERY SELECT 'NewReservation'::text,head_row."SourceArtifactId",attempt_row."AttemptKeyReservationId",
   attempt_row."AttemptId",attempt_row."EncryptionAttemptRevision",attempt_row."Fence"; RETURN;
 END IF;
 IF publication_row."PublicationState"='Available' THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_ALREADY_AVAILABLE'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF head_row."CustodyState"='Staged' OR publication_row."PublicationState"='Committed'
    OR attempt_row."StagedAtUtc" IS NOT NULL OR provisional_row."State" IN ('ObjectPresentPendingVerification','VerifiedCompleted') THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESUME_PENDING'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF attempt_row."R2TerminationDisposition" IS NULL OR attempt_row."R2TerminalOutcomeCode" IS NULL THEN
  RETURN QUERY SELECT 'RAW_EXPORT_SOURCE_RESERVATION_BUSY'::text,head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 RETURN QUERY SELECT attempt_row."R2TerminalOutcomeCode",head_row."SourceArtifactId",NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint;
END;$handoff$;

CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='complete-r1'
     AND TG_OP<>'DELETE' THEN
    RETURN NEW;
  END IF;
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
     AND TG_OP='UPDATE'
     AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Staged'
     AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
     AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
         IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN
    RETURN NEW;
  END IF;
  IF current_user='tagekyc_raw_export_deployer'
     AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r5-publish-v1'
     AND TG_OP='UPDATE' AND OLD."CustodyState"='Staged' AND NEW."CustodyState"='Available'
     AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
     AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
         IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN RETURN NEW; END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
END $guard$;

CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN
  IF TG_OP='INSERT' THEN
    IF current_user<>'tagekyc_raw_export_deployer'
       OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
      RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
    END IF;
    RETURN NEW;
  END IF;
IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-no-provider-start-v1'
             AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
             AND NEW."R2TerminationDisposition"='TerminatedBeforeStart' AND NEW."R2TerminatedAtUtc" IS NOT NULL
             AND OLD."OwnershipLeaseExpiresAtUtc"<=NEW."R2TerminatedAtUtc"
             AND NEW."R2TerminatedAtUtc"<=pg_catalog.clock_timestamp()
             AND OLD."R2TerminalOutcomeCode" IS NULL AND OLD."R2TerminalIntentCode" IS NULL
             AND OLD."R2TerminalIntentDisposition" IS NULL AND OLD."R2TerminalIntentAtUtc" IS NULL
             AND OLD."StagedAtUtc" IS NULL
             AND to_jsonb(NEW)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
                  =to_jsonb(OLD)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
             AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_head h
              JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=h."SourceArtifactId"
              JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
              JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
              WHERE h."SourceArtifactId"=OLD."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=OLD."AttemptId"
               AND h."Fence"=OLD."Fence" AND h."CustodyState"='Reserved'
               AND s."AuthorityKind"='SourceRetention'
               AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor()
               AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId")
             AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=OLD."SourceArtifactId")
             AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
     WHERE k."AttemptKeyReservationId"=OLD."AttemptKeyReservationId" OR k."AttemptId"=OLD."AttemptId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
     WHERE e."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations p
     WHERE p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects p
     WHERE p."AttemptId"=OLD."AttemptId" OR p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId"
       OR p."ProvisionalObjectIdentity"=OLD."ProvisionalObjectIdentity")) THEN RETURN NEW; END IF;

    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
       AND NEW."R2TerminatedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion",
           NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."StagedObjectCustodyId",
           NEW."StagedObjectStateRevision",NEW."StagedFromReservationRevision",NEW."VerifiedPlaintextLength",
           NEW."StagedCiphertextLength",NEW."StagedCiphertextDigest",NEW."StagedProviderReceiptDigest",
           NEW."StagedVerificationEvidenceDigest",NEW."StagedAtUtc")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion",
           OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."StagedObjectCustodyId",
           OLD."StagedObjectStateRevision",OLD."StagedFromReservationRevision",OLD."VerifiedPlaintextLength",
           OLD."StagedCiphertextLength",OLD."StagedCiphertextDigest",OLD."StagedProviderReceiptDigest",
           OLD."StagedVerificationEvidenceDigest",OLD."StagedAtUtc") THEN
      RETURN NEW;
    END IF;
    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL
       AND OLD."StagedCiphertextFingerprintSchemaVersion" IS NULL
       AND OLD."StagedCiphertextFingerprint" IS NULL AND OLD."StagedObjectCustodyId" IS NULL
       AND OLD."StagedObjectStateRevision" IS NULL AND OLD."StagedFromReservationRevision" IS NULL
       AND OLD."VerifiedPlaintextLength" IS NULL AND OLD."StagedCiphertextLength" IS NULL
       AND OLD."StagedCiphertextDigest" IS NULL AND OLD."StagedProviderReceiptDigest" IS NULL
       AND OLD."StagedVerificationEvidenceDigest" IS NULL AND OLD."StagedAtUtc" IS NULL
       AND NEW."StagedCiphertextFingerprintSchemaVersion"=2
       AND pg_catalog.octet_length(NEW."StagedCiphertextFingerprint")=32
       AND NEW."StagedObjectCustodyId" IS NOT NULL AND NEW."StagedObjectStateRevision">=1
       AND NEW."StagedFromReservationRevision">=1 AND NEW."VerifiedPlaintextLength">=1
       AND NEW."StagedCiphertextLength" BETWEEN 1 AND 134217728
       AND pg_catalog.octet_length(NEW."StagedCiphertextDigest")=32
       AND pg_catalog.octet_length(NEW."StagedProviderReceiptDigest")=32
       AND pg_catalog.octet_length(NEW."StagedVerificationEvidenceDigest")=32
       AND NEW."StagedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."R2TerminationDisposition",NEW."R2TerminatedAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."R2TerminationDisposition",OLD."R2TerminatedAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
      RETURN NEW;
    END IF;
  END IF;
  IF TG_OP='UPDATE' THEN
    RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
  END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
END $guard$;
DROP FUNCTION tagekyc.raw_export_reenter_retained_source(uuid,uuid,bigint,bigint,bigint,uuid,integer,integer,integer);
""";

    private const string NoProviderStartOperations = """
CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN
  IF TG_OP='INSERT' THEN
    IF current_user<>'tagekyc_raw_export_deployer'
       OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
      RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
    END IF;
    RETURN NEW;
  END IF;
IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-a3-no-provider-start-v1'
             AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
             AND NEW."R2TerminationDisposition"='TerminatedBeforeStart' AND NEW."R2TerminatedAtUtc" IS NOT NULL
             AND OLD."OwnershipLeaseExpiresAtUtc"<=NEW."R2TerminatedAtUtc"
             AND NEW."R2TerminatedAtUtc"<=pg_catalog.clock_timestamp()
             AND OLD."R2TerminalOutcomeCode" IS NULL AND OLD."R2TerminalIntentCode" IS NULL
             AND OLD."R2TerminalIntentDisposition" IS NULL AND OLD."R2TerminalIntentAtUtc" IS NULL
             AND OLD."StagedAtUtc" IS NULL
             AND to_jsonb(NEW)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
                  =to_jsonb(OLD)-ARRAY['R2TerminationDisposition','R2TerminatedAtUtc']
             AND EXISTS(SELECT 1 FROM tagekyc.raw_export_source_head h
              JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=h."SourceArtifactId"
              JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
              JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
              WHERE h."SourceArtifactId"=OLD."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=OLD."AttemptId"
               AND h."Fence"=OLD."Fence" AND h."CustodyState"='Reserved'
               AND s."AuthorityKind"='SourceRetention'
               AND s."CustodyPrincipalId"=tagekyc.raw_export_current_actor()
               AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId")
             AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=OLD."SourceArtifactId")
             AND (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
     WHERE k."AttemptKeyReservationId"=OLD."AttemptKeyReservationId" OR k."AttemptId"=OLD."AttemptId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
     WHERE e."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations p
     WHERE p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects p
     WHERE p."AttemptId"=OLD."AttemptId" OR p."AttemptKeyReservationId"=OLD."AttemptKeyReservationId"
       OR p."ProvisionalObjectIdentity"=OLD."ProvisionalObjectIdentity")) THEN RETURN NEW; END IF;

    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
       AND NEW."R2TerminatedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion",
           NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."StagedObjectCustodyId",
           NEW."StagedObjectStateRevision",NEW."StagedFromReservationRevision",NEW."VerifiedPlaintextLength",
           NEW."StagedCiphertextLength",NEW."StagedCiphertextDigest",NEW."StagedProviderReceiptDigest",
           NEW."StagedVerificationEvidenceDigest",NEW."StagedAtUtc")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion",
           OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."StagedObjectCustodyId",
           OLD."StagedObjectStateRevision",OLD."StagedFromReservationRevision",OLD."VerifiedPlaintextLength",
           OLD."StagedCiphertextLength",OLD."StagedCiphertextDigest",OLD."StagedProviderReceiptDigest",
           OLD."StagedVerificationEvidenceDigest",OLD."StagedAtUtc") THEN
      RETURN NEW;
    END IF;
    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL
       AND OLD."StagedCiphertextFingerprintSchemaVersion" IS NULL
       AND OLD."StagedCiphertextFingerprint" IS NULL AND OLD."StagedObjectCustodyId" IS NULL
       AND OLD."StagedObjectStateRevision" IS NULL AND OLD."StagedFromReservationRevision" IS NULL
       AND OLD."VerifiedPlaintextLength" IS NULL AND OLD."StagedCiphertextLength" IS NULL
       AND OLD."StagedCiphertextDigest" IS NULL AND OLD."StagedProviderReceiptDigest" IS NULL
       AND OLD."StagedVerificationEvidenceDigest" IS NULL AND OLD."StagedAtUtc" IS NULL
       AND NEW."StagedCiphertextFingerprintSchemaVersion"=2
       AND pg_catalog.octet_length(NEW."StagedCiphertextFingerprint")=32
       AND NEW."StagedObjectCustodyId" IS NOT NULL AND NEW."StagedObjectStateRevision">=1
       AND NEW."StagedFromReservationRevision">=1 AND NEW."VerifiedPlaintextLength">=1
       AND NEW."StagedCiphertextLength" BETWEEN 1 AND 134217728
       AND pg_catalog.octet_length(NEW."StagedCiphertextDigest")=32
       AND pg_catalog.octet_length(NEW."StagedProviderReceiptDigest")=32
       AND pg_catalog.octet_length(NEW."StagedVerificationEvidenceDigest")=32
       AND NEW."StagedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."R2TerminationDisposition",NEW."R2TerminatedAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."R2TerminationDisposition",OLD."R2TerminatedAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
      RETURN NEW;
    END IF;
  END IF;
  IF TG_OP='UPDATE' THEN
    RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
  END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
END $guard$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(
    p_attempt_key_reservation_id uuid,
    p_attempt_id uuid,
    p_source_artifact_id uuid)
RETURNS TABLE(
    outcome text,
    provider_operation_id uuid,
    preparation_id uuid,
    preparation_fence bigint,
    provider_operation_token text,
    preparation_lease_expires_at_utc timestamptz,
    attempt_key_context_fingerprint bytea,
    key_provider_id text,
    kek_id text,
    kek_version integer,
    kek_fingerprint text,
    wrapping_suite_id text,
    wrapping_suite_version integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $prepare$
DECLARE
    retention_probe record; retention_locked record; attempt_found boolean;
    attempt_row record;
    existing_head record;
    operation_row record;
    now_utc timestamptz := pg_catalog.statement_timestamp();
    new_preparation_id uuid := pg_catalog.gen_random_uuid();
    new_operation_id uuid := pg_catalog.gen_random_uuid();
    new_fence bigint;
    new_token text;
    context_fingerprint bytea;
    prior_context text := pg_catalog.current_setting(
        'tagekyc.raw_export_attempt_key_write_context', true);
BEGIN
SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RETURN QUERY SELECT 'HeadNotReserved'::text,
 NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
 NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RETURN QUERY SELECT 'HeadNotReserved'::text,
 NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
 NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
 END IF;
    SELECT a.*, h."CustodyState", h."CurrentEncryptionAttemptId", h."Fence" AS "HeadFence",
           r."ReservationExpiresAtUtc"
    INTO attempt_row
    FROM tagekyc.raw_export_source_encryption_attempts a
    JOIN tagekyc.raw_export_source_head h
      ON h."SourceArtifactId"=a."SourceArtifactId"
    JOIN tagekyc.raw_export_source_reservations r
      ON r."SourceArtifactId"=a."SourceArtifactId"
    WHERE a."AttemptId"=p_attempt_id
      AND a."SourceArtifactId"=p_source_artifact_id
      AND a."AttemptKeyReservationId"=p_attempt_key_reservation_id
    FOR UPDATE OF a,h;

    attempt_found:=FOUND;
    IF retention_probe."AuthorityKind"='SourceRetention' AND attempt_found THEN
      SELECT r."ReservationExpiresAtUtc" INTO attempt_row."ReservationExpiresAtUtc"
       FROM tagekyc.raw_export_source_reservations r
       WHERE r."SourceArtifactId"=p_source_artifact_id FOR UPDATE;
      PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations k
       WHERE k."AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
      SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
      now_utc:=pg_catalog.clock_timestamp();
      IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked)
       OR retention_probe."SourceArtifactId" IS DISTINCT FROM p_source_artifact_id
       OR attempt_row."HeadFence" IS DISTINCT FROM attempt_row."Fence"
       OR attempt_row."R2TerminatedAtUtc" IS NOT NULL
       OR attempt_row."R2TerminalIntentCode" IS NOT NULL OR attempt_row."R2TerminalIntentDisposition" IS NOT NULL
       OR attempt_row."R2TerminalIntentAtUtc" IS NOT NULL OR attempt_row."R2TerminalOutcomeCode" IS NOT NULL THEN
        RETURN QUERY SELECT 'HeadNotReserved'::text,
 NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
 NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
      END IF;
    END IF;
    IF NOT attempt_found OR attempt_row."CustodyState" <> 'Reserved'
       OR attempt_row."CurrentEncryptionAttemptId" <> p_attempt_id
       OR attempt_row."R2TerminationDisposition" IS NOT NULL
       OR attempt_row."OwnershipLeaseExpiresAtUtc" <= now_utc
       OR attempt_row."ReservationExpiresAtUtc" <= now_utc THEN
        RETURN QUERY SELECT 'HeadNotReserved'::text,
            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
        RETURN;
    END IF;

    context_fingerprint := tagekyc.raw_export_c1_hash_canonical(
        'tip-88c1-attempt-key-context-v1',
        pg_catalog.replace(p_attempt_key_reservation_id::text,'-',''),
        pg_catalog.replace(p_attempt_id::text,'-',''),
        pg_catalog.encode(attempt_row."EncryptionAttemptFingerprint",'hex'),
        attempt_row."KeyProviderId",attempt_row."KekId",
        attempt_row."KekVersion"::text,attempt_row."KekFingerprint",
        'AES-256-GCM','1');

    SELECT * INTO existing_head
    FROM tagekyc.raw_export_attempt_key_reservations
    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
    FOR UPDATE;

    IF FOUND THEN
        IF existing_head."AttemptId" <> p_attempt_id
           OR existing_head."EncryptionAttemptFingerprint" IS DISTINCT FROM attempt_row."EncryptionAttemptFingerprint"
           OR existing_head."AttemptKeyContextFingerprint" IS DISTINCT FROM context_fingerprint THEN
            RETURN QUERY SELECT 'Conflict'::text,
                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition"='Active' THEN
            SELECT * INTO operation_row
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
              AND "PreparationFence"=existing_head."CurrentPreparationFence";
            RETURN QUERY SELECT 'ExistingMatch'::text,
                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition" IN
           ('Revoked','ProviderCorruptOrUnverifiable','AbandonRequested','ReservationAbandoned') THEN
            RETURN QUERY SELECT 'Terminated'::text,
                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition" <> 'ReadyForFreshPreparation' THEN
            SELECT * INTO operation_row
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
              AND "PreparationFence"=existing_head."CurrentPreparationFence";
            RETURN QUERY SELECT 'InProgress'::text,
                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
            RETURN;
        END IF;
        new_fence := existing_head."CurrentPreparationFence" + 1;
    ELSE
        new_fence := 1;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM tagekyc.raw_export_key_provider_operations history
        WHERE history."AttemptKeyReservationId"=p_attempt_key_reservation_id
          AND history."PreparationFence">=new_fence) THEN
        RETURN QUERY SELECT 'StaleFence'::text,
            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
        RETURN;
    END IF;

    new_token := pg_catalog.rtrim(pg_catalog.translate(
        pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=');
    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
    BEGIN
        IF existing_head."AttemptKeyReservationId" IS NULL THEN
            INSERT INTO tagekyc.raw_export_attempt_key_reservations(
                "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint",
                "KeyProviderId","KekId","KekVersion","KekFingerprint",
                "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion",
                "PreparationDisposition","CurrentPreparationId","CurrentPreparationFence",
                "CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken",
                "ResolutionAttemptCount","CleanupAttemptCount","CleanupOperatorInterventionRequired",
                "RowRevision","CreatedAtUtc","UpdatedAtUtc")
            VALUES(p_attempt_key_reservation_id,p_attempt_id,attempt_row."EncryptionAttemptFingerprint",
                attempt_row."KeyProviderId",attempt_row."KekId",attempt_row."KekVersion",attempt_row."KekFingerprint",
                context_fingerprint,'AES-256-GCM',1,'PreparingLive',new_preparation_id,new_fence,
                now_utc + interval '15 minutes',new_token,0,0,false,1,now_utc,now_utc);
        ELSE
            UPDATE tagekyc.raw_export_attempt_key_reservations SET
                "PreparationDisposition"='PreparingLive',
                "CurrentPreparationId"=new_preparation_id,
                "CurrentPreparationFence"=new_fence,
                "CurrentPreparationLeaseExpiresAtUtc"=now_utc + interval '15 minutes',
                "CurrentProviderOperationToken"=new_token,
                "ResolutionAttemptCount"=0,"NextResolutionAttemptNotBeforeUtc"=NULL,
                "ResolutionDeadlineUtc"=NULL,"CleanupAttemptCount"=0,
                "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                "CleanupOperatorInterventionRequired"=false,
                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
        END IF;

        INSERT INTO tagekyc.raw_export_key_provider_operations(
            "ProviderOperationId","KeyProviderId","ProviderOperationToken",
            "AttemptKeyReservationId","PreparationId","PreparationFence",
            "AttemptKeyContextFingerprint","ProviderOperationState","IssuedAtUtc","UpdatedAtUtc")
        VALUES(new_operation_id,attempt_row."KeyProviderId",new_token,
            p_attempt_key_reservation_id,new_preparation_id,new_fence,
            context_fingerprint,'Issued',now_utc,now_utc);

        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence",
            "EventSequence","EventKind","ProviderOperationToken","WrappingSuiteId",
            "WrappingSuiteVersion","EventAtUtc")
        SELECT pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,new_preparation_id,new_fence,
            COALESCE(MAX("EventSequence"),0)+1,'Opened',new_token,'AES-256-GCM',1,now_utc
        FROM tagekyc.raw_export_attempt_key_preparation_events
        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
    EXCEPTION WHEN OTHERS THEN
        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
        RAISE;
    END;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);

    RETURN QUERY SELECT 'PreparingLive'::text,new_operation_id,new_preparation_id,new_fence,new_token,
        now_utc + interval '15 minutes',context_fingerprint,attempt_row."KeyProviderId"::text,
        attempt_row."KekId"::text,attempt_row."KekVersion",attempt_row."KekFingerprint"::text,'AES-256-GCM'::text,1;
END
$prepare$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_begin_provisional_object_custody(
  p_attempt_id uuid, p_expected_encryption_attempt_revision bigint,
  p_expected_fence bigint, p_maximum_provisional_objects_per_source integer)
RETURNS TABLE(
  "OutcomeCode" text, "ObjectCustodyId" uuid, "ObjectState" text, "StateRevision" bigint,
  "ObjectKey" text, "ProvisionalObjectIdentity" uuid, "ObjectBindingDigest" bytea,
  "RawClass" text, "ClaimedPlaintextLength" bigint,
  "OwnershipLeaseExpiresAtUtc" timestamptz,
  "EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
  "ReservationExpiresAtUtc" timestamptz, "ProjectionAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE
  retention_probe record; retention_locked record;
  a record; h record; r record; c record; k record; existing record;
  actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
  object_id uuid:=pg_catalog.gen_random_uuid(); object_key text; binding bytea;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context', true);
BEGIN
  IF p_attempt_id IS NULL OR p_expected_encryption_attempt_revision < 1 OR p_expected_fence < 1
     OR p_maximum_provisional_objects_per_source NOT BETWEEN 1 AND 64 THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
 END IF;
  SELECT x.*, sh."CurrentEncryptionAttemptId", sh."ReservationRevision", sh."Fence" AS "HeadFence"
    INTO a FROM tagekyc.raw_export_source_encryption_attempts x
    JOIN tagekyc.raw_export_source_head sh ON sh."SourceArtifactId"=x."SourceArtifactId"
    WHERE x."AttemptId"=p_attempt_id FOR UPDATE OF x,sh;
  IF NOT FOUND OR a."CurrentEncryptionAttemptId"<>p_attempt_id OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision
     OR a."Fence"<>p_expected_fence OR a."HeadFence"<>p_expected_fence
     OR a."R2TerminationDisposition" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE';
  END IF;
  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations kr
    WHERE kr."AttemptKeyReservationId"=a."AttemptKeyReservationId"
      AND kr."AttemptId"=a."AttemptId" AND kr."PreparationDisposition"='Active' FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
  SELECT sr.*, ic."RawClass" INTO r FROM tagekyc.raw_export_source_reservations sr
    JOIN tagekyc.raw_export_source_ingress_claims ic ON ic."IngressClaimId"=sr."IngressClaimId"
    WHERE sr."SourceArtifactId"=a."SourceArtifactId";
  IF retention_probe."AuthorityKind"='SourceRetention' THEN
    SELECT sr."ReservationExpiresAtUtc" INTO r."ReservationExpiresAtUtc"
      FROM tagekyc.raw_export_source_reservations sr WHERE sr."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
    SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
    now_utc:=pg_catalog.clock_timestamp();
    IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked)
       OR a."SourceArtifactId" IS DISTINCT FROM retention_probe."SourceArtifactId"
       OR a."R2TerminatedAtUtc" IS NOT NULL OR a."R2TerminalIntentCode" IS NOT NULL
       OR a."R2TerminalIntentDisposition" IS NOT NULL OR a."R2TerminalIntentAtUtc" IS NOT NULL
       OR a."R2TerminalOutcomeCode" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc
       OR r."ReservationExpiresAtUtc"<=now_utc THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
  END IF;
  IF r."RawClass" NOT IN ('ChipDg2Portrait','LiveSelfieImage') THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_CLASS_NOT_SUPPORTED';
  END IF;
  SELECT * INTO existing FROM tagekyc.raw_export_provisional_objects po WHERE po."AttemptId"=p_attempt_id;
  IF FOUND THEN
    RETURN QUERY SELECT 'ExistingMatch',existing."ObjectCustodyId",existing."State"::text,existing."StateRevision",
      existing."ObjectKey"::text,existing."ProvisionalObjectIdentity",existing."ObjectBindingDigest",r."RawClass"::text,r."ClaimedPlaintextLength",
      a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
    RETURN;
  END IF;
  IF (SELECT pg_catalog.count(*) FROM tagekyc.raw_export_provisional_objects po
      WHERE po."SourceArtifactId"=a."SourceArtifactId" AND po."State" NOT IN ('NoObjectEstablished','Deleted','Quarantined'))
     >= p_maximum_provisional_objects_per_source THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED';
  END IF;
  object_key:='raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower(a."ProvisionalObjectIdentity"::text),'-','');
  binding:=tagekyc.compute_raw_export_provisional_object_binding(a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",
    a."ProvisionalObjectIdentity",a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
  INSERT INTO tagekyc.raw_export_provisional_objects(
    "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
    "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
    "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
  VALUES(object_id,a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",a."ProvisionalObjectIdentity",
    a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key,binding,
    'Initiated',1,now_utc,now_utc,1);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
  INSERT INTO tagekyc.raw_export_provisional_object_events(
    "ObjectCustodyEventId","ObjectCustodyId","EventSequence","FromState","ToState","ActorKind","StateRevision","EvidenceDigest","EventAtUtc","SchemaVersion")
  VALUES(pg_catalog.gen_random_uuid(),object_id,1,NULL,'Initiated','Writer',1,binding,now_utc,1);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
  RETURN QUERY SELECT 'Created',object_id,'Initiated',1::bigint,object_key,a."ProvisionalObjectIdentity",binding,r."RawClass"::text,r."ClaimedPlaintextLength",
    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $fn$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_terminate_source_encryption_attempt(
  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_disposition text)
RETURNS text
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE retention_probe record; retention_locked record; a record; h record; k record; object_count integer; object_state text;
  key_found boolean; actor_id uuid;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
  IF p_attempt_id IS NULL OR p_expected_revision<1 OR p_expected_fence<1
     OR p_disposition NOT IN ('Terminated','TerminatedBeforeStart') THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R2_TERMINATION_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RETURN 'StateConflict'; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
 END IF;
  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts
    WHERE "AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND THEN RETURN 'NotFound'; END IF;
  IF retention_probe."AuthorityKind"='SourceRetention' THEN
    SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
    IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked) THEN RETURN 'StateConflict'; END IF;
  END IF;
  IF a."EncryptionAttemptRevision"<>p_expected_revision OR a."Fence"<>p_expected_fence THEN
    RETURN 'StateConflict';
  END IF;
  IF retention_probe."AuthorityKind"='SourceRetention' AND a."R2TerminalIntentCode" IS NOT NULL
     AND a."R2TerminalIntentDisposition" IS DISTINCT FROM p_disposition THEN
    RETURN 'StateConflict';
  END IF;
  IF a."R2TerminationDisposition"=p_disposition AND a."R2TerminatedAtUtc" IS NOT NULL THEN
    RETURN 'ExistingMatch';
  END IF;
  IF a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL THEN
    RETURN 'StateConflict';
  END IF;
  SELECT * INTO h FROM tagekyc.raw_export_source_head
    WHERE "SourceArtifactId"=a."SourceArtifactId" AND "CurrentEncryptionAttemptId"=a."AttemptId"
      AND "Fence"=a."Fence" FOR UPDATE;
  IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations
    WHERE "AttemptKeyReservationId"=a."AttemptKeyReservationId" AND "AttemptId"=a."AttemptId" FOR UPDATE;
  key_found:=FOUND;
  SELECT po."State"::text
    INTO object_state FROM tagekyc.raw_export_provisional_objects po
    WHERE po."AttemptId"=a."AttemptId" FOR UPDATE;
  object_count:=CASE WHEN FOUND THEN 1 ELSE 0 END;
  IF p_disposition='TerminatedBeforeStart' THEN
    IF NOT ((object_count=1 AND object_state='NoObjectEstablished')
       OR (object_count=0 AND key_found AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))) THEN
      RETURN 'StateConflict';
    END IF;
   ELSIF object_count<>1 OR NOT (object_state IN ('Deleted','Quarantined')
     OR (retention_probe."AuthorityKind"='SourceRetention'
      AND a."R2TerminalIntentCode"='CONTENT_COMMITMENT_MISMATCH'
      AND a."R2TerminalIntentDisposition"='Terminated'
      AND key_found AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned')
      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects o
       WHERE o."AttemptId"=a."AttemptId" AND o."SourceArtifactId"=a."SourceArtifactId"
        AND o."AttemptKeyReservationId"=a."AttemptKeyReservationId"
        AND o."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity"
        AND o."EncryptionAttemptRevision"=a."EncryptionAttemptRevision"
        AND o."AttemptFence"=a."Fence"
        AND o."EncryptionAttemptFingerprint"=a."EncryptionAttemptFingerprint"
        AND o."State"='NoObjectEstablished' AND o."PutOutcomeKind"='PositiveAbsence'
        AND o."PutOperationId" IS NOT NULL AND o."PutArmedAtUtc" IS NOT NULL
        AND o."OutcomeObservedAtUtc" IS NOT NULL))) THEN
    RETURN 'StateConflict';
  END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r2-termination-v1',true);
  UPDATE tagekyc.raw_export_source_encryption_attempts
    SET "R2TerminationDisposition"=p_disposition,"R2TerminatedAtUtc"=CASE
      WHEN retention_probe."AuthorityKind"='SourceRetention' THEN pg_catalog.clock_timestamp()
      ELSE pg_catalog.statement_timestamp() END
    WHERE "AttemptId"=p_attempt_id;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RETURN 'Terminated';
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $fn$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,
 p_operational_disposition text,p_terminal_outcome_code text)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $terminal$
BEGIN
 IF EXISTS(SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts a
  JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
  JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
  WHERE a."AttemptId"=p_attempt_id AND s."AuthorityKind"='SourceRetention') THEN
  RETURN false;
 END IF;
 IF p_operational_disposition NOT IN ('Terminated','TerminatedBeforeStart') OR
    p_terminal_outcome_code NOT IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
 THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_TERMINAL_OUTCOME_INVALID'; END IF;
 UPDATE tagekyc.raw_export_source_encryption_attempts a SET
  "R2TerminationDisposition"=p_operational_disposition,"R2TerminatedAtUtc"=pg_catalog.statement_timestamp(),
  "R2TerminalOutcomeCode"=p_terminal_outcome_code
 FROM tagekyc.raw_export_source_head h
 WHERE a."AttemptId"=p_attempt_id AND a."SourceArtifactId"=p_source_artifact_id
  AND a."EncryptionAttemptRevision"=p_expected_revision AND a."Fence"=p_expected_fence
  AND a."R2TerminationDisposition" IS NULL AND a."R2TerminalOutcomeCode" IS NULL
  AND h."SourceArtifactId"=a."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=a."AttemptId"
  AND h."ReservationRevision"=a."EncryptionAttemptRevision" AND h."Fence"=a."Fence" AND h."CustodyState"='Reserved';
 RETURN FOUND;
END;$terminal$;

CREATE FUNCTION tagekyc.raw_export_terminate_retained_r2_before_provider_start(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
RETURNS TABLE("Outcome" text,"R2TerminationDisposition" text,"R2TerminatedAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $nps$
DECLARE retention_probe record; retention_locked record; a record; h record; r record; s record; c record;
 actor_id uuid; now_utc timestamptz;
 previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
 IF p_source_artifact_id IS NULL OR p_source_artifact_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
  OR p_expected_revision IS NULL OR p_expected_revision<1 OR p_expected_fence IS NULL OR p_expected_fence<1 THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 actor_id:=tagekyc.raw_export_current_actor();
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_probe
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF retention_probe."AuthorityKind"='SourceRetention' THEN
   IF retention_probe."AuthenticatedPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."CustodyPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor()
      OR retention_probe."ConsentReferenceId" IS NULL THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
   PERFORM 1 FROM tagekyc.verification_sessions vs
    WHERE vs."Id"=retention_probe."VerificationSessionId"
     AND vs."ClientApplicationId"=retention_probe."ClientApplicationId" FOR UPDATE;
   IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtextextended(
    'tip88c1:a3:consent-reference:'||retention_probe."ClientApplicationId"::text||':'||retention_probe."ExternalConsentArtifactRef",0));
   PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
    'tip88c1:b2-authority:'||retention_probe."ClientApplicationId"::text||':'||
    retention_probe."VerificationSessionId"::text||':'||retention_probe."CaptureAcceptanceId"::text||':'||retention_probe."RawClass"));
 END IF;

 IF retention_probe."AuthorityKind" IS NULL THEN
  RETURN QUERY SELECT 'NotFound'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 IF retention_probe."AuthorityKind"<>'SourceRetention' THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 SELECT aa.* INTO a FROM tagekyc.raw_export_source_encryption_attempts aa
  WHERE aa."AttemptId"=p_attempt_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT hh.* INTO h FROM tagekyc.raw_export_source_head hh
  WHERE hh."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT rr.* INTO r FROM tagekyc.raw_export_source_reservations rr
  WHERE rr."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT ss.* INTO s FROM tagekyc.raw_export_authority_snapshots ss
  WHERE ss."AuthoritySnapshotId"=r."AuthoritySnapshotId";
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT cc.* INTO c FROM tagekyc.raw_export_source_ingress_claims cc
  WHERE cc."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN; END IF;
 SELECT ss."AuthorityKind", rr."SourceArtifactId", rr."AuthoritySnapshotId",
 cc."ClientApplicationId",cc."VerificationSessionId",cc."AuthenticatedPrincipalId",
 ss."CustodyPrincipalId",ss."RuntimeBindingId",ss."CaptureAcceptanceId",ss."RawClass",bb."ConsentReferenceId",hh."ExternalConsentArtifactRef"
 INTO retention_locked
 FROM tagekyc.raw_export_source_encryption_attempts aa
 JOIN tagekyc.raw_export_source_reservations rr ON rr."SourceArtifactId"=aa."SourceArtifactId"
 JOIN tagekyc.raw_export_source_ingress_claims cc ON cc."IngressClaimId"=rr."IngressClaimId"
 JOIN tagekyc.raw_export_authority_snapshots ss ON ss."AuthoritySnapshotId"=rr."AuthoritySnapshotId"
 LEFT JOIN tagekyc.raw_source_consent_bindings bb ON bb."ConsentBindingId"=ss."ConsentBindingId"
 LEFT JOIN tagekyc.raw_source_consent_references hh ON hh."ConsentReferenceId"=bb."ConsentReferenceId"
 WHERE aa."AttemptId"=p_attempt_id;
 IF to_jsonb(retention_probe) IS DISTINCT FROM to_jsonb(retention_locked)
  OR a."SourceArtifactId" IS DISTINCT FROM p_source_artifact_id
  OR retention_probe."SourceArtifactId" IS DISTINCT FROM r."SourceArtifactId"
  OR retention_probe."AuthoritySnapshotId" IS DISTINCT FROM s."AuthoritySnapshotId"
  OR retention_probe."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"
  OR retention_probe."ClientApplicationId" IS DISTINCT FROM c."ClientApplicationId"
  OR s."AuthorityKind" IS DISTINCT FROM 'SourceRetention'
  OR c."AuthenticatedPrincipalId" IS DISTINCT FROM actor_id OR s."CustodyPrincipalId" IS DISTINCT FROM actor_id
  OR h."CurrentEncryptionAttemptId" IS DISTINCT FROM a."AttemptId" OR h."CustodyState" IS DISTINCT FROM 'Reserved'
  OR h."Fence" IS DISTINCT FROM a."Fence" OR a."EncryptionAttemptRevision" IS DISTINCT FROM p_expected_revision
  OR a."Fence" IS DISTINCT FROM p_expected_fence
  OR a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
  OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
  OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
  OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
  OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
  OR a."StagedAtUtc" IS NOT NULL
  OR EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications p WHERE p."SourceArtifactId"=a."SourceArtifactId") THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 now_utc:=pg_catalog.clock_timestamp();
 IF a."OwnershipLeaseExpiresAtUtc">now_utc THEN
  RETURN QUERY SELECT 'LeaseLive'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 IF NOT (NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations k
     WHERE k."AttemptKeyReservationId"=a."AttemptKeyReservationId" OR k."AttemptId"=a."AttemptId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
     WHERE e."AttemptKeyReservationId"=a."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations p
     WHERE p."AttemptKeyReservationId"=a."AttemptKeyReservationId")
   AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_provisional_objects p
     WHERE p."AttemptId"=a."AttemptId" OR p."AttemptKeyReservationId"=a."AttemptKeyReservationId"
       OR p."ProvisionalObjectIdentity"=a."ProvisionalObjectIdentity")) THEN
  RETURN QUERY SELECT 'ProviderEvidencePresent'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 IF a."R2TerminationDisposition"='TerminatedBeforeStart' AND a."R2TerminatedAtUtc" IS NOT NULL THEN
  RETURN QUERY SELECT 'ExistingMatch'::text,a."R2TerminationDisposition"::text,a."R2TerminatedAtUtc"; RETURN;
 END IF;
 IF a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL
  OR a."R2TerminalOutcomeCode" IS NOT NULL OR a."R2TerminalIntentCode" IS NOT NULL
  OR a."R2TerminalIntentDisposition" IS NOT NULL OR a."R2TerminalIntentAtUtc" IS NOT NULL THEN
  RETURN QUERY SELECT 'StateConflict'::text,NULL::text,NULL::timestamptz; RETURN;
 END IF;
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-a3-no-provider-start-v1',true);
 UPDATE tagekyc.raw_export_source_encryption_attempts aa
  SET "R2TerminationDisposition"='TerminatedBeforeStart',"R2TerminatedAtUtc"=now_utc
  WHERE aa."AttemptId"=a."AttemptId";
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RETURN QUERY SELECT 'TerminatedBeforeStart'::text,'TerminatedBeforeStart'::text,now_utc;
EXCEPTION WHEN OTHERS THEN
 PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
 RAISE;
END $nps$;
ALTER FUNCTION tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint) TO tagekyc_raw_export_reconciler;
""";

    private const string NoProviderStartOperationsRestore = """
CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
BEGIN
  IF TG_OP='INSERT' THEN
    IF current_user<>'tagekyc_raw_export_deployer'
       OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
      RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
    END IF;
    RETURN NEW;
  END IF;
IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
       AND NEW."R2TerminatedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion",
           NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."StagedObjectCustodyId",
           NEW."StagedObjectStateRevision",NEW."StagedFromReservationRevision",NEW."VerifiedPlaintextLength",
           NEW."StagedCiphertextLength",NEW."StagedCiphertextDigest",NEW."StagedProviderReceiptDigest",
           NEW."StagedVerificationEvidenceDigest",NEW."StagedAtUtc")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion",
           OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."StagedObjectCustodyId",
           OLD."StagedObjectStateRevision",OLD."StagedFromReservationRevision",OLD."VerifiedPlaintextLength",
           OLD."StagedCiphertextLength",OLD."StagedCiphertextDigest",OLD."StagedProviderReceiptDigest",
           OLD."StagedVerificationEvidenceDigest",OLD."StagedAtUtc") THEN
      RETURN NEW;
    END IF;
    IF current_user='tagekyc_raw_export_deployer'
       AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
       AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
       AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL
       AND OLD."StagedCiphertextFingerprintSchemaVersion" IS NULL
       AND OLD."StagedCiphertextFingerprint" IS NULL AND OLD."StagedObjectCustodyId" IS NULL
       AND OLD."StagedObjectStateRevision" IS NULL AND OLD."StagedFromReservationRevision" IS NULL
       AND OLD."VerifiedPlaintextLength" IS NULL AND OLD."StagedCiphertextLength" IS NULL
       AND OLD."StagedCiphertextDigest" IS NULL AND OLD."StagedProviderReceiptDigest" IS NULL
       AND OLD."StagedVerificationEvidenceDigest" IS NULL AND OLD."StagedAtUtc" IS NULL
       AND NEW."StagedCiphertextFingerprintSchemaVersion"=2
       AND pg_catalog.octet_length(NEW."StagedCiphertextFingerprint")=32
       AND NEW."StagedObjectCustodyId" IS NOT NULL AND NEW."StagedObjectStateRevision">=1
       AND NEW."StagedFromReservationRevision">=1 AND NEW."VerifiedPlaintextLength">=1
       AND NEW."StagedCiphertextLength" BETWEEN 1 AND 134217728
       AND pg_catalog.octet_length(NEW."StagedCiphertextDigest")=32
       AND pg_catalog.octet_length(NEW."StagedProviderReceiptDigest")=32
       AND pg_catalog.octet_length(NEW."StagedVerificationEvidenceDigest")=32
       AND NEW."StagedAtUtc" IS NOT NULL
       AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
           NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
           NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
           NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
           NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
           NEW."OwnershipLeaseExpiresAtUtc",NEW."R2TerminationDisposition",NEW."R2TerminatedAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
           IS NOT DISTINCT FROM
           ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
           OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
           OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
           OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
           OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
           OLD."OwnershipLeaseExpiresAtUtc",OLD."R2TerminationDisposition",OLD."R2TerminatedAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
      RETURN NEW;
    END IF;
  END IF;
  IF TG_OP='UPDATE' THEN
    RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
  END IF;
  RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
END $guard$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(
    p_attempt_key_reservation_id uuid,
    p_attempt_id uuid,
    p_source_artifact_id uuid)
RETURNS TABLE(
    outcome text,
    provider_operation_id uuid,
    preparation_id uuid,
    preparation_fence bigint,
    provider_operation_token text,
    preparation_lease_expires_at_utc timestamptz,
    attempt_key_context_fingerprint bytea,
    key_provider_id text,
    kek_id text,
    kek_version integer,
    kek_fingerprint text,
    wrapping_suite_id text,
    wrapping_suite_version integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $prepare$
DECLARE
    attempt_row record;
    existing_head record;
    operation_row record;
    now_utc timestamptz := pg_catalog.statement_timestamp();
    new_preparation_id uuid := pg_catalog.gen_random_uuid();
    new_operation_id uuid := pg_catalog.gen_random_uuid();
    new_fence bigint;
    new_token text;
    context_fingerprint bytea;
    prior_context text := pg_catalog.current_setting(
        'tagekyc.raw_export_attempt_key_write_context', true);
BEGIN
    SELECT a.*, h."CustodyState", h."CurrentEncryptionAttemptId",
           r."ReservationExpiresAtUtc"
    INTO attempt_row
    FROM tagekyc.raw_export_source_encryption_attempts a
    JOIN tagekyc.raw_export_source_head h
      ON h."SourceArtifactId"=a."SourceArtifactId"
    JOIN tagekyc.raw_export_source_reservations r
      ON r."SourceArtifactId"=a."SourceArtifactId"
    WHERE a."AttemptId"=p_attempt_id
      AND a."SourceArtifactId"=p_source_artifact_id
      AND a."AttemptKeyReservationId"=p_attempt_key_reservation_id
    FOR UPDATE OF a,h;

    IF NOT FOUND OR attempt_row."CustodyState" <> 'Reserved'
       OR attempt_row."CurrentEncryptionAttemptId" <> p_attempt_id
       OR attempt_row."R2TerminationDisposition" IS NOT NULL
       OR attempt_row."OwnershipLeaseExpiresAtUtc" <= now_utc
       OR attempt_row."ReservationExpiresAtUtc" <= now_utc THEN
        RETURN QUERY SELECT 'HeadNotReserved'::text,
            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
        RETURN;
    END IF;

    context_fingerprint := tagekyc.raw_export_c1_hash_canonical(
        'tip-88c1-attempt-key-context-v1',
        pg_catalog.replace(p_attempt_key_reservation_id::text,'-',''),
        pg_catalog.replace(p_attempt_id::text,'-',''),
        pg_catalog.encode(attempt_row."EncryptionAttemptFingerprint",'hex'),
        attempt_row."KeyProviderId",attempt_row."KekId",
        attempt_row."KekVersion"::text,attempt_row."KekFingerprint",
        'AES-256-GCM','1');

    SELECT * INTO existing_head
    FROM tagekyc.raw_export_attempt_key_reservations
    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
    FOR UPDATE;

    IF FOUND THEN
        IF existing_head."AttemptId" <> p_attempt_id
           OR existing_head."EncryptionAttemptFingerprint" IS DISTINCT FROM attempt_row."EncryptionAttemptFingerprint"
           OR existing_head."AttemptKeyContextFingerprint" IS DISTINCT FROM context_fingerprint THEN
            RETURN QUERY SELECT 'Conflict'::text,
                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition"='Active' THEN
            SELECT * INTO operation_row
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
              AND "PreparationFence"=existing_head."CurrentPreparationFence";
            RETURN QUERY SELECT 'ExistingMatch'::text,
                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition" IN
           ('Revoked','ProviderCorruptOrUnverifiable','AbandonRequested','ReservationAbandoned') THEN
            RETURN QUERY SELECT 'Terminated'::text,
                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
            RETURN;
        END IF;
        IF existing_head."PreparationDisposition" <> 'ReadyForFreshPreparation' THEN
            SELECT * INTO operation_row
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
              AND "PreparationFence"=existing_head."CurrentPreparationFence";
            RETURN QUERY SELECT 'InProgress'::text,
                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
            RETURN;
        END IF;
        new_fence := existing_head."CurrentPreparationFence" + 1;
    ELSE
        new_fence := 1;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM tagekyc.raw_export_key_provider_operations history
        WHERE history."AttemptKeyReservationId"=p_attempt_key_reservation_id
          AND history."PreparationFence">=new_fence) THEN
        RETURN QUERY SELECT 'StaleFence'::text,
            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
        RETURN;
    END IF;

    new_token := pg_catalog.rtrim(pg_catalog.translate(
        pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=');
    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
    BEGIN
        IF existing_head."AttemptKeyReservationId" IS NULL THEN
            INSERT INTO tagekyc.raw_export_attempt_key_reservations(
                "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint",
                "KeyProviderId","KekId","KekVersion","KekFingerprint",
                "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion",
                "PreparationDisposition","CurrentPreparationId","CurrentPreparationFence",
                "CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken",
                "ResolutionAttemptCount","CleanupAttemptCount","CleanupOperatorInterventionRequired",
                "RowRevision","CreatedAtUtc","UpdatedAtUtc")
            VALUES(p_attempt_key_reservation_id,p_attempt_id,attempt_row."EncryptionAttemptFingerprint",
                attempt_row."KeyProviderId",attempt_row."KekId",attempt_row."KekVersion",attempt_row."KekFingerprint",
                context_fingerprint,'AES-256-GCM',1,'PreparingLive',new_preparation_id,new_fence,
                now_utc + interval '15 minutes',new_token,0,0,false,1,now_utc,now_utc);
        ELSE
            UPDATE tagekyc.raw_export_attempt_key_reservations SET
                "PreparationDisposition"='PreparingLive',
                "CurrentPreparationId"=new_preparation_id,
                "CurrentPreparationFence"=new_fence,
                "CurrentPreparationLeaseExpiresAtUtc"=now_utc + interval '15 minutes',
                "CurrentProviderOperationToken"=new_token,
                "ResolutionAttemptCount"=0,"NextResolutionAttemptNotBeforeUtc"=NULL,
                "ResolutionDeadlineUtc"=NULL,"CleanupAttemptCount"=0,
                "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                "CleanupOperatorInterventionRequired"=false,
                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
        END IF;

        INSERT INTO tagekyc.raw_export_key_provider_operations(
            "ProviderOperationId","KeyProviderId","ProviderOperationToken",
            "AttemptKeyReservationId","PreparationId","PreparationFence",
            "AttemptKeyContextFingerprint","ProviderOperationState","IssuedAtUtc","UpdatedAtUtc")
        VALUES(new_operation_id,attempt_row."KeyProviderId",new_token,
            p_attempt_key_reservation_id,new_preparation_id,new_fence,
            context_fingerprint,'Issued',now_utc,now_utc);

        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence",
            "EventSequence","EventKind","ProviderOperationToken","WrappingSuiteId",
            "WrappingSuiteVersion","EventAtUtc")
        SELECT pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,new_preparation_id,new_fence,
            COALESCE(MAX("EventSequence"),0)+1,'Opened',new_token,'AES-256-GCM',1,now_utc
        FROM tagekyc.raw_export_attempt_key_preparation_events
        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
    EXCEPTION WHEN OTHERS THEN
        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
        RAISE;
    END;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);

    RETURN QUERY SELECT 'PreparingLive'::text,new_operation_id,new_preparation_id,new_fence,new_token,
        now_utc + interval '15 minutes',context_fingerprint,attempt_row."KeyProviderId"::text,
        attempt_row."KekId"::text,attempt_row."KekVersion",attempt_row."KekFingerprint"::text,'AES-256-GCM'::text,1;
END
$prepare$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_terminate_source_encryption_attempt(
  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_disposition text)
RETURNS text
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE a record; h record; k record; object_count integer; object_state text;
  key_found boolean; actor_id uuid;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
BEGIN
  IF p_attempt_id IS NULL OR p_expected_revision<1 OR p_expected_fence<1
     OR p_disposition NOT IN ('Terminated','TerminatedBeforeStart') THEN
    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R2_TERMINATION_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts
    WHERE "AttemptId"=p_attempt_id FOR UPDATE;
  IF NOT FOUND THEN RETURN 'NotFound'; END IF;
  IF a."EncryptionAttemptRevision"<>p_expected_revision OR a."Fence"<>p_expected_fence THEN
    RETURN 'StateConflict';
  END IF;
  IF a."R2TerminationDisposition"=p_disposition AND a."R2TerminatedAtUtc" IS NOT NULL THEN
    RETURN 'ExistingMatch';
  END IF;
  IF a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL THEN
    RETURN 'StateConflict';
  END IF;
  SELECT * INTO h FROM tagekyc.raw_export_source_head
    WHERE "SourceArtifactId"=a."SourceArtifactId" AND "CurrentEncryptionAttemptId"=a."AttemptId"
      AND "Fence"=a."Fence" FOR UPDATE;
  IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations
    WHERE "AttemptKeyReservationId"=a."AttemptKeyReservationId" AND "AttemptId"=a."AttemptId" FOR UPDATE;
  key_found:=FOUND;
  SELECT po."State"::text
    INTO object_state FROM tagekyc.raw_export_provisional_objects po
    WHERE po."AttemptId"=a."AttemptId" FOR UPDATE;
  object_count:=CASE WHEN FOUND THEN 1 ELSE 0 END;
  IF p_disposition='TerminatedBeforeStart' THEN
    IF NOT ((object_count=1 AND object_state='NoObjectEstablished')
       OR (object_count=0 AND key_found AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))) THEN
      RETURN 'StateConflict';
    END IF;
  ELSIF object_count<>1 OR object_state NOT IN ('Deleted','Quarantined') THEN
    RETURN 'StateConflict';
  END IF;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r2-termination-v1',true);
  UPDATE tagekyc.raw_export_source_encryption_attempts
    SET "R2TerminationDisposition"=p_disposition,"R2TerminatedAtUtc"=pg_catalog.statement_timestamp()
    WHERE "AttemptId"=p_attempt_id;
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RETURN 'Terminated';
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $fn$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_record_r2_terminal_outcome(
 p_source_artifact_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,
 p_operational_disposition text,p_terminal_outcome_code text)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $terminal$
BEGIN
 IF p_operational_disposition NOT IN ('Terminated','TerminatedBeforeStart') OR
    p_terminal_outcome_code NOT IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
 THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_R2_TERMINAL_OUTCOME_INVALID'; END IF;
 UPDATE tagekyc.raw_export_source_encryption_attempts a SET
  "R2TerminationDisposition"=p_operational_disposition,"R2TerminatedAtUtc"=pg_catalog.statement_timestamp(),
  "R2TerminalOutcomeCode"=p_terminal_outcome_code
 FROM tagekyc.raw_export_source_head h
 WHERE a."AttemptId"=p_attempt_id AND a."SourceArtifactId"=p_source_artifact_id
  AND a."EncryptionAttemptRevision"=p_expected_revision AND a."Fence"=p_expected_fence
  AND a."R2TerminationDisposition" IS NULL AND a."R2TerminalOutcomeCode" IS NULL
  AND h."SourceArtifactId"=a."SourceArtifactId" AND h."CurrentEncryptionAttemptId"=a."AttemptId"
  AND h."ReservationRevision"=a."EncryptionAttemptRevision" AND h."Fence"=a."Fence" AND h."CustodyState"='Reserved';
 RETURN FOUND;
END;$terminal$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_begin_provisional_object_custody(
  p_attempt_id uuid, p_expected_encryption_attempt_revision bigint,
  p_expected_fence bigint, p_maximum_provisional_objects_per_source integer)
RETURNS TABLE(
  "OutcomeCode" text, "ObjectCustodyId" uuid, "ObjectState" text, "StateRevision" bigint,
  "ObjectKey" text, "ProvisionalObjectIdentity" uuid, "ObjectBindingDigest" bytea,
  "RawClass" text, "ClaimedPlaintextLength" bigint,
  "OwnershipLeaseExpiresAtUtc" timestamptz,
  "EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
  "ReservationExpiresAtUtc" timestamptz, "ProjectionAtUtc" timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
DECLARE
  a record; h record; r record; c record; k record; existing record;
  actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
  object_id uuid:=pg_catalog.gen_random_uuid(); object_key text; binding bytea;
  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context', true);
BEGIN
  IF p_attempt_id IS NULL OR p_expected_encryption_attempt_revision < 1 OR p_expected_fence < 1
     OR p_maximum_provisional_objects_per_source NOT BETWEEN 1 AND 64 THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID';
  END IF;
  actor_id:=tagekyc.raw_export_current_actor();
  SELECT x.*, sh."CurrentEncryptionAttemptId", sh."ReservationRevision", sh."Fence" AS "HeadFence"
    INTO a FROM tagekyc.raw_export_source_encryption_attempts x
    JOIN tagekyc.raw_export_source_head sh ON sh."SourceArtifactId"=x."SourceArtifactId"
    WHERE x."AttemptId"=p_attempt_id FOR UPDATE OF x,sh;
  IF NOT FOUND OR a."CurrentEncryptionAttemptId"<>p_attempt_id OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision
     OR a."Fence"<>p_expected_fence OR a."HeadFence"<>p_expected_fence
     OR a."R2TerminationDisposition" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE';
  END IF;
  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations kr
    WHERE kr."AttemptKeyReservationId"=a."AttemptKeyReservationId"
      AND kr."AttemptId"=a."AttemptId" AND kr."PreparationDisposition"='Active' FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
  SELECT sr.*, ic."RawClass" INTO r FROM tagekyc.raw_export_source_reservations sr
    JOIN tagekyc.raw_export_source_ingress_claims ic ON ic."IngressClaimId"=sr."IngressClaimId"
    WHERE sr."SourceArtifactId"=a."SourceArtifactId";
  IF r."RawClass" NOT IN ('ChipDg2Portrait','LiveSelfieImage') THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_CLASS_NOT_SUPPORTED';
  END IF;
  SELECT * INTO existing FROM tagekyc.raw_export_provisional_objects po WHERE po."AttemptId"=p_attempt_id;
  IF FOUND THEN
    RETURN QUERY SELECT 'ExistingMatch',existing."ObjectCustodyId",existing."State"::text,existing."StateRevision",
      existing."ObjectKey"::text,existing."ProvisionalObjectIdentity",existing."ObjectBindingDigest",r."RawClass"::text,r."ClaimedPlaintextLength",
      a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
    RETURN;
  END IF;
  IF (SELECT pg_catalog.count(*) FROM tagekyc.raw_export_provisional_objects po
      WHERE po."SourceArtifactId"=a."SourceArtifactId" AND po."State" NOT IN ('NoObjectEstablished','Deleted','Quarantined'))
     >= p_maximum_provisional_objects_per_source THEN
    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED';
  END IF;
  object_key:='raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower(a."ProvisionalObjectIdentity"::text),'-','');
  binding:=tagekyc.compute_raw_export_provisional_object_binding(a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",
    a."ProvisionalObjectIdentity",a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
  INSERT INTO tagekyc.raw_export_provisional_objects(
    "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
    "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
    "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
  VALUES(object_id,a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",a."ProvisionalObjectIdentity",
    a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key,binding,
    'Initiated',1,now_utc,now_utc,1);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
  INSERT INTO tagekyc.raw_export_provisional_object_events(
    "ObjectCustodyEventId","ObjectCustodyId","EventSequence","FromState","ToState","ActorKind","StateRevision","EvidenceDigest","EventAtUtc","SchemaVersion")
  VALUES(pg_catalog.gen_random_uuid(),object_id,1,NULL,'Initiated','Writer',1,binding,now_utc,1);
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
  RETURN QUERY SELECT 'Created',object_id,'Initiated',1::bigint,object_key,a."ProvisionalObjectIdentity",binding,r."RawClass"::text,r."ClaimedPlaintextLength",
    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
EXCEPTION WHEN OTHERS THEN
  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
  RAISE;
END $fn$;
DROP FUNCTION tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint);
""";

    private const string TerminalIntentSchema = """
ALTER TABLE tagekyc.raw_export_source_encryption_attempts
 ADD COLUMN "R2TerminalIntentCode" character varying(64) NULL,
 ADD COLUMN "R2TerminalIntentDisposition" character varying(32) NULL,
 ADD COLUMN "R2TerminalIntentAtUtc" timestamp with time zone NULL,
 ADD CONSTRAINT ck_a3_r2_terminal_intent CHECK (
(("R2TerminalIntentCode" IS NULL AND "R2TerminalIntentDisposition" IS NULL AND "R2TerminalIntentAtUtc" IS NULL)
 OR ("R2TerminalIntentCode" IS NOT NULL AND "R2TerminalIntentDisposition" IS NOT NULL AND "R2TerminalIntentAtUtc" IS NOT NULL
 AND "R2TerminalIntentCode" IN ('RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED','CONTENT_COMMITMENT_MISMATCH','RECAPTURE_REQUIRED')
 AND "R2TerminalIntentDisposition" IN ('Terminated','TerminatedBeforeStart')))
 AND ("R2TerminalOutcomeCode" IS NULL OR "R2TerminalIntentCode" IS NULL
 OR ("R2TerminalOutcomeCode"="R2TerminalIntentCode" AND "R2TerminationDisposition" IS NOT NULL
 AND "R2TerminationDisposition"="R2TerminalIntentDisposition"))
 );
""";

    private const string TerminalIntentSchemaRestore = """
ALTER TABLE tagekyc.raw_export_source_encryption_attempts
 DROP CONSTRAINT ck_a3_r2_terminal_intent,
 DROP COLUMN "R2TerminalIntentCode",
 DROP COLUMN "R2TerminalIntentDisposition",
 DROP COLUMN "R2TerminalIntentAtUtc";
""";

    private const string SnapshotOperations = """
CREATE OR REPLACE FUNCTION
    tagekyc.enforce_raw_export_authority_snapshot_insert()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog
AS $$
DECLARE
    expected_context text;
    append_context text;
    actor_id uuid;
    acceptance_client_application_id uuid;
    acceptance_verification_session_id uuid;
    acceptance_raw_class text;
    current_revision bigint;
    current_kind text;
    snapshot_now timestamptz;
    reference_client uuid;
    reference_ref text;
    current_event_type text;
    current_is_effective boolean;
BEGIN
    expected_context := CASE NEW."EventType"
        WHEN 'Granted' THEN 'grant'
        WHEN 'Withdrawn' THEN 'withdraw'
        WHEN 'Revoked' THEN 'revoke'
        ELSE NULL
    END;
    append_context := pg_catalog.current_setting(
        'tagekyc.raw_export_authority_snapshot_append_context',
        true);
    IF current_user <> 'tagekyc_raw_export_deployer'
       OR append_context IS DISTINCT FROM expected_context THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_DIRECT_INSERT_UNSUPPORTED';
    END IF;

    actor_id := tagekyc.raw_export_current_actor();
    SELECT
        acceptance."ClientApplicationId",
        acceptance."VerificationSessionId",
        acceptance."RawClass"
    INTO
        acceptance_client_application_id,
        acceptance_verification_session_id,
        acceptance_raw_class
    FROM tagekyc.raw_export_capture_acceptance_events AS acceptance
    WHERE acceptance."CaptureAcceptanceId" =
        NEW."CaptureAcceptanceId";
    IF NOT FOUND
       OR acceptance_client_application_id IS DISTINCT FROM
            NEW."ClientApplicationId"
       OR acceptance_verification_session_id IS DISTINCT FROM
            NEW."VerificationSessionId"
       OR acceptance_raw_class IS DISTINCT FROM NEW."RawClass" THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID';
    END IF;

    IF NEW."AuthorityKind"='SourceRetention' AND NEW."EventType"='Granted' THEN
 SELECT h."ClientApplicationId",h."ExternalConsentArtifactRef" INTO reference_client,reference_ref
 FROM tagekyc.raw_source_consent_bindings b
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
 WHERE b."ConsentBindingId"=NEW."ConsentBindingId";
 IF FOUND THEN
  PERFORM pg_advisory_xact_lock_shared(hashtextextended(
   'tip88c1:a3:consent-reference:'||reference_client::text||':'||reference_ref,0));
 END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permits p
 JOIN tagekyc.raw_source_consent_bindings b ON b."ConsentBindingId"=p."ConsentBindingId"
  AND b."PrincipalId"=p."PrincipalId" AND b."ClientApplicationId"=p."ClientApplicationId"
  AND b."VerificationSessionId"=p."VerificationSessionId"
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
  AND h."ClientApplicationId"=b."ClientApplicationId" AND h."SubjectRef"=b."SubjectRef"
  AND h."CurrentRevision"=b."ConsentReferenceRevision"
 JOIN tagekyc.raw_source_consent_reference_events e ON e."ConsentReferenceId"=h."ConsentReferenceId"
  AND e."Revision"=b."ConsentReferenceRevision"
 JOIN tagekyc.capture_execution_bindings r ON r."CaptureExecutionBindingId"=NEW."RuntimeBindingId"
  AND r."AuthorityMode"='SourceRetention' AND r."PrincipalId"=p."PrincipalId"
  AND r."ClientApplicationId"=p."ClientApplicationId" AND r."VerificationSessionId"=p."VerificationSessionId"
  AND r."RetentionAuthorityId"=p."RetentionAuthorityId" AND r."RetentionAuthorityRevision"=p."Revision"
  AND r."ConsentBindingId"=b."ConsentBindingId"
 JOIN tagekyc.verification_sessions vs ON vs."Id"=p."VerificationSessionId"
  AND vs."ClientApplicationId"=p."ClientApplicationId" AND vs."SubjectRef"=b."SubjectRef"
 JOIN tagekyc.raw_export_capture_acceptance_events a ON a."CaptureAcceptanceId"=NEW."CaptureAcceptanceId"
  AND a."VerificationSessionId"=p."VerificationSessionId" AND a."ClientApplicationId"=p."ClientApplicationId"
  AND a."RawClass"=NEW."RawClass"
 JOIN tagekyc.capture_artifacts ca ON ca."Id"=a."CaptureArtifactId" AND ca."VerificationSessionId"=a."VerificationSessionId"
 WHERE p."RetentionAuthorityId"=NEW."RetentionAuthorityId" AND p."Revision"=NEW."RetentionAuthorityRevision"
  AND p."ConsentBindingId"=NEW."ConsentBindingId" AND p."PrincipalId"=NEW."CustodyPrincipalId"
  AND p."PrincipalId"=NEW."CapturedByPrincipalId"
  AND p."ClientApplicationId"=NEW."ClientApplicationId" AND p."VerificationSessionId"=NEW."VerificationSessionId"
  AND p."PolicyId"=NEW."ConsentPolicyId" AND p."PolicyVersion"=NEW."ConsentPolicyVersion"
  AND NEW."AuthorityArtifactId"=p."RetentionAuthorityId" AND NEW."AuthorityArtifactVersion"=1
  AND NEW."ControllerIdentity"=p."ControllerIdentity" AND NEW."StableDataScopeId"=p."StableDataScopeId"
  AND NEW."RetentionPolicyId"=p."RetentionPolicyId" AND NEW."RetentionPolicyVersion"=p."RetentionPolicyVersion"
  AND NEW."RetentionClass"=p."RetentionClass" AND NEW."RetentionStartEvent"=p."RetentionStartEvent"
  AND NEW."RevocationPolicyId"=p."RevocationPolicyId" AND NEW."PurgePolicyId"=p."PurgePolicyId"
  AND NEW."LegalHoldPolicyId"=p."LegalHoldPolicyId"
  AND NEW."AbsoluteSourceExpiresAtUtc"=p."ExpiresAtUtc"
  AND NEW."ValidUntilUtc"=LEAST(p."ExpiresAtUtc",e."ValidUntilUtc")
  AND EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permit_classes pc
   WHERE pc."RetentionAuthorityId"=p."RetentionAuthorityId" AND pc."Revision"=p."Revision" AND pc."RawClass"=NEW."RawClass")) THEN
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_LINEAGE_MISMATCH';
 END IF;
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            NEW."ClientApplicationId"::text || ':' ||
            NEW."VerificationSessionId"::text || ':' ||
            NEW."CaptureAcceptanceId"::text || ':' ||
            NEW."RawClass"));

    snapshot_now:=CASE WHEN NEW."AuthorityKind"='SourceRetention' THEN pg_catalog.clock_timestamp()
        ELSE pg_catalog.transaction_timestamp() END;
    SELECT
        snapshot."AuthorityKind",
        snapshot."Revision",
        snapshot."EventType",
        (
            snapshot."EventType" = 'Granted'
            AND snapshot."ValidFromUtc" <=
                snapshot_now
            AND (
                snapshot."ValidUntilUtc" IS NULL
                OR snapshot_now <
                    snapshot."ValidUntilUtc")
        )
    INTO
        current_kind,
        current_revision,
        current_event_type,
        current_is_effective
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            NEW."ClientApplicationId"
      AND snapshot."VerificationSessionId" =
            NEW."VerificationSessionId"
      AND snapshot."CaptureAcceptanceId" =
            NEW."CaptureAcceptanceId"
      AND snapshot."RawClass" = NEW."RawClass"
    ORDER BY snapshot."Revision" DESC
    LIMIT 1;
    current_revision := COALESCE(current_revision, 0);

    IF NEW."Revision" <> current_revision + 1 THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_REVISION_CONFLICT';
    END IF;

    IF NEW."EventType" = 'Granted' THEN
        IF NEW."CapturedByPrincipalId" IS DISTINCT FROM actor_id
           OR NEW."TargetRevision" IS NOT NULL THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
        END IF;
    ELSIF NEW."EventType" = 'Withdrawn' THEN
        IF NEW."AuthorityKind" IS DISTINCT FROM current_kind THEN
            RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_LINEAGE_MISMATCH';
        END IF;
        IF NEW."WithdrawnByPrincipalId" IS DISTINCT FROM actor_id
           OR current_event_type IS DISTINCT FROM 'Granted'
           OR current_is_effective IS DISTINCT FROM true
           OR NEW."TargetRevision" IS DISTINCT FROM
                current_revision THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
        END IF;
    ELSIF NEW."EventType" = 'Revoked' THEN
        IF NEW."AuthorityKind" IS DISTINCT FROM current_kind THEN
            RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_LINEAGE_MISMATCH';
        END IF;
        IF NEW."RevokedByPrincipalId" IS DISTINCT FROM actor_id
           OR current_event_type IS DISTINCT FROM 'Granted'
           OR current_is_effective IS DISTINCT FROM true
           OR NEW."TargetRevision" IS DISTINCT FROM
                current_revision THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
        END IF;
    ELSE
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
    END IF;

    NEW."RecordedAtUtc" :=
        snapshot_now;
    RETURN NEW;
END;
$$;

CREATE FUNCTION tagekyc.raw_export_retained_snapshot_is_current(
 p_authority_snapshot_id uuid,p_evaluated_at_utc timestamptz)
RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE s tagekyc.raw_export_authority_snapshots%ROWTYPE; reference_client uuid; reference_ref text;
BEGIN
 IF p_evaluated_at_utc IS NULL OR NOT isfinite(p_evaluated_at_utc) THEN RETURN false; END IF;
 SELECT * INTO s FROM tagekyc.raw_export_authority_snapshots x
 WHERE x."AuthoritySnapshotId"=p_authority_snapshot_id AND x."EventType"='Granted';
 IF NOT FOUND OR s."AuthorityKind"<>'SourceRetention' OR s."ApprovedPurpose"<>'SourceRetention'
  OR s."ValidFromUtc">p_evaluated_at_utc OR s."ValidUntilUtc"<=p_evaluated_at_utc THEN RETURN false; END IF;
 -- CP04 callers hold the actual session prefix and sample this clock after all admission locks.
 SELECT h."ClientApplicationId",h."ExternalConsentArtifactRef" INTO reference_client,reference_ref
 FROM tagekyc.raw_source_consent_bindings b
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
 WHERE b."ConsentBindingId"=s."ConsentBindingId";
 IF FOUND THEN
  PERFORM pg_advisory_xact_lock_shared(hashtextextended(
   'tip88c1:a3:consent-reference:'||reference_client::text||':'||reference_ref,0));
 END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permits p
 JOIN tagekyc.raw_source_consent_bindings b ON b."ConsentBindingId"=p."ConsentBindingId"
  AND b."PrincipalId"=p."PrincipalId" AND b."ClientApplicationId"=p."ClientApplicationId"
  AND b."VerificationSessionId"=p."VerificationSessionId"
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
  AND h."ClientApplicationId"=b."ClientApplicationId" AND h."SubjectRef"=b."SubjectRef"
 JOIN tagekyc.raw_source_consent_reference_events e ON e."ConsentReferenceId"=h."ConsentReferenceId"
  AND e."Revision"=b."ConsentReferenceRevision"
 JOIN tagekyc.capture_execution_bindings r ON r."CaptureExecutionBindingId"=s."RuntimeBindingId"
  AND r."AuthorityMode"='SourceRetention' AND r."PrincipalId"=p."PrincipalId"
  AND r."ClientApplicationId"=p."ClientApplicationId" AND r."VerificationSessionId"=p."VerificationSessionId"
  AND r."RetentionAuthorityId"=p."RetentionAuthorityId" AND r."RetentionAuthorityRevision"=p."Revision"
  AND r."ConsentBindingId"=b."ConsentBindingId"
 JOIN tagekyc.verification_sessions vs ON vs."Id"=p."VerificationSessionId"
  AND vs."ClientApplicationId"=p."ClientApplicationId" AND vs."SubjectRef"=b."SubjectRef"
 JOIN tagekyc.raw_export_capture_acceptance_events a ON a."CaptureAcceptanceId"=s."CaptureAcceptanceId"
  AND a."VerificationSessionId"=p."VerificationSessionId" AND a."ClientApplicationId"=p."ClientApplicationId"
  AND a."RawClass"=s."RawClass"
 JOIN tagekyc.capture_artifacts ca ON ca."Id"=a."CaptureArtifactId" AND ca."VerificationSessionId"=a."VerificationSessionId"
 WHERE p."RetentionAuthorityId"=s."RetentionAuthorityId" AND p."Revision"=s."RetentionAuthorityRevision"
  AND p."ConsentBindingId"=s."ConsentBindingId" AND p."PrincipalId"=s."CustodyPrincipalId"
  AND p."PrincipalId"=s."CapturedByPrincipalId"
  AND p."ClientApplicationId"=s."ClientApplicationId" AND p."VerificationSessionId"=s."VerificationSessionId"
  AND p."PolicyId"=s."ConsentPolicyId" AND p."PolicyVersion"=s."ConsentPolicyVersion"
  AND s."AuthorityArtifactId"=p."RetentionAuthorityId" AND s."AuthorityArtifactVersion"=1
  AND s."ControllerIdentity"=p."ControllerIdentity" AND s."StableDataScopeId"=p."StableDataScopeId"
  AND s."RetentionPolicyId"=p."RetentionPolicyId" AND s."RetentionPolicyVersion"=p."RetentionPolicyVersion"
  AND s."RetentionClass"=p."RetentionClass" AND s."RetentionStartEvent"=p."RetentionStartEvent"
  AND s."RevocationPolicyId"=p."RevocationPolicyId" AND s."PurgePolicyId"=p."PurgePolicyId"
  AND s."LegalHoldPolicyId"=p."LegalHoldPolicyId"
  AND s."AbsoluteSourceExpiresAtUtc"=p."ExpiresAtUtc"
  AND s."ValidUntilUtc"=LEAST(p."ExpiresAtUtc",e."ValidUntilUtc")
  AND EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permit_classes pc
   WHERE pc."RetentionAuthorityId"=p."RetentionAuthorityId" AND pc."Revision"=p."Revision" AND pc."RawClass"=s."RawClass")) THEN RETURN false; END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_authority_snapshots x
 WHERE x."ClientApplicationId"=s."ClientApplicationId" AND x."VerificationSessionId"=s."VerificationSessionId"
 AND x."CaptureAcceptanceId"=s."CaptureAcceptanceId" AND x."RawClass"=s."RawClass"
 GROUP BY x."ClientApplicationId" HAVING max(x."Revision")=s."Revision") THEN RETURN false; END IF;
 RETURN EXISTS(SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(
  s."RetentionAuthorityId",s."RetentionAuthorityRevision",s."CustodyPrincipalId",s."ClientApplicationId",
  s."VerificationSessionId",s."RawClass",p_evaluated_at_utc));
END $body$;

CREATE FUNCTION tagekyc.raw_export_append_retained_authority_snapshot(
 p_runtime_binding_id uuid,p_capture_acceptance_id uuid,p_raw_class text,
 p_retention_authority_id uuid,p_retention_authority_revision bigint)
RETURNS TABLE("Revision" bigint,"AuthoritySnapshotId" uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
 r tagekyc.capture_execution_bindings%ROWTYPE;
 p tagekyc.raw_source_retention_permits%ROWTYPE;
 b tagekyc.raw_source_consent_bindings%ROWTYPE;
 h tagekyc.raw_source_consent_references%ROWTYPE;
 e tagekyc.raw_source_consent_reference_events%ROWTYPE;
 previous tagekyc.raw_export_authority_snapshots%ROWTYPE;
 session_id uuid; client_id uuid; requirement text; now_utc timestamptz;
 next_revision bigint; snapshot_id uuid; prior_context text;
BEGIN
 -- Immutable probe is not authorization; rejoin exact IDs after the actual row prefix.
 SELECT x."VerificationSessionId",x."ClientApplicationId" INTO session_id,client_id
 FROM tagekyc.capture_execution_bindings x WHERE x."CaptureExecutionBindingId"=p_runtime_binding_id;
 IF NOT FOUND THEN RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 PERFORM 1 FROM tagekyc.verification_sessions s WHERE s."Id"=session_id AND s."ClientApplicationId"=client_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 SELECT * INTO r FROM tagekyc.capture_execution_bindings x
 WHERE x."CaptureExecutionBindingId"=p_runtime_binding_id AND x."VerificationSessionId"=session_id AND x."ClientApplicationId"=client_id;
 IF NOT FOUND OR r."AuthorityMode"<>'SourceRetention'
  OR r."RetentionAuthorityId" IS DISTINCT FROM p_retention_authority_id
  OR r."RetentionAuthorityRevision" IS DISTINCT FROM p_retention_authority_revision THEN
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 SELECT * INTO p FROM tagekyc.raw_source_retention_permits x
 WHERE x."RetentionAuthorityId"=p_retention_authority_id AND x."Revision"=p_retention_authority_revision
 AND x."ConsentBindingId"=r."ConsentBindingId" AND x."PrincipalId"=r."PrincipalId"
 AND x."ClientApplicationId"=client_id AND x."VerificationSessionId"=session_id;
 IF NOT FOUND OR p."PrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor() THEN
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 SELECT * INTO b FROM tagekyc.raw_source_consent_bindings x WHERE x."ConsentBindingId"=p."ConsentBindingId";
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x WHERE x."ConsentReferenceId"=b."ConsentReferenceId";
 IF NOT FOUND THEN RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtextextended(
  'tip88c1:a3:consent-reference:'||client_id::text||':'||h."ExternalConsentArtifactRef",0));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||p."PolicyId"::text||':'||p."PolicyVersion"::text));
 FOR requirement IN SELECT req."RequirementType" FROM tagekyc.raw_export_policy_requirements req
 WHERE req."PolicyId"=p."PolicyId" AND req."PolicyVersion"=p."PolicyVersion" AND req."RequirementType"<>'ConsentArtifact'
 ORDER BY req."RequirementType" COLLATE "C"
 LOOP PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||p."PolicyId"::text||':'||p."PolicyVersion"::text||':'||requirement)); END LOOP;
 PERFORM pg_advisory_xact_lock(hashtext('tip88c1:b2-authority:'||client_id::text||':'||session_id::text||':'||p_capture_acceptance_id::text||':'||p_raw_class));
 now_utc:=clock_timestamp();
 IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(
  p."RetentionAuthorityId",p."Revision",p."PrincipalId",client_id,session_id,p_raw_class,now_utc)) THEN
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 SELECT * INTO previous FROM tagekyc.raw_export_authority_snapshots x
 WHERE x."ClientApplicationId"=client_id AND x."VerificationSessionId"=session_id
 AND x."CaptureAcceptanceId"=p_capture_acceptance_id AND x."RawClass"=p_raw_class ORDER BY x."Revision" DESC LIMIT 1;
 IF FOUND THEN
  IF previous."EventType"<>'Granted' THEN RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
  IF previous."AuthorityKind"='SourceRetention' AND previous."RetentionAuthorityId"=p_retention_authority_id
   AND previous."RetentionAuthorityRevision"=p_retention_authority_revision AND previous."RuntimeBindingId"=p_runtime_binding_id
   AND tagekyc.raw_export_retained_snapshot_is_current(previous."AuthoritySnapshotId",now_utc) THEN
   RETURN QUERY SELECT previous."Revision",previous."AuthoritySnapshotId"; RETURN;
  END IF;
  -- An existing source cannot be rebound to another authority under the same acceptance.
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED';
 END IF;
 SELECT * INTO e FROM tagekyc.raw_source_consent_reference_events x
 WHERE x."ConsentReferenceId"=b."ConsentReferenceId" AND x."Revision"=b."ConsentReferenceRevision";
 IF NOT FOUND OR LEAST(p."ExpiresAtUtc",e."ValidUntilUtc")<=now_utc THEN
  RAISE EXCEPTION 'A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED'; END IF;
 next_revision:=1; snapshot_id:=gen_random_uuid();
 prior_context:=current_setting('tagekyc.raw_export_authority_snapshot_append_context',true);
 PERFORM set_config('tagekyc.raw_export_authority_snapshot_append_context','grant',true);
 INSERT INTO tagekyc.raw_export_authority_snapshots(
  "AuthorityKind","RetentionAuthorityId","RetentionAuthorityRevision","ConsentBindingId","CustodyPrincipalId","RuntimeBindingId",
  "AuthoritySnapshotEventId","EventType","Revision","ValidFromUtc","ValidUntilUtc","RecordedAtUtc",
  "CapturedByPrincipalId","ClientApplicationId","VerificationSessionId","CaptureAcceptanceId","RawClass",
  "AuthoritySnapshotSchemaVersion","AuthoritySnapshotId","AuthorityArtifactId","AuthorityArtifactVersion",
  "ControllerIdentity","ApprovedPurpose","StableDataScopeId","RetentionPolicyId","RetentionPolicyVersion",
  "ConsentPolicyId","ConsentPolicyVersion","RetentionClass","RetentionStartEvent","AbsoluteSourceExpiresAtUtc",
  "ReuseDisposition","ExtensionDisposition","RevocationPolicyId","PurgePolicyId","LegalHoldPolicyId","EvaluatedAtUtc")
 VALUES(
  'SourceRetention',p."RetentionAuthorityId",p."Revision",p."ConsentBindingId",p."PrincipalId",p_runtime_binding_id,
  gen_random_uuid(),'Granted',next_revision,now_utc,LEAST(p."ExpiresAtUtc",e."ValidUntilUtc"),now_utc,
  p."PrincipalId",client_id,session_id,p_capture_acceptance_id,p_raw_class,
  1,snapshot_id,p."RetentionAuthorityId",1,p."ControllerIdentity",'SourceRetention',p."StableDataScopeId",
  p."RetentionPolicyId",p."RetentionPolicyVersion",p."PolicyId",p."PolicyVersion",p."RetentionClass",
  p."RetentionStartEvent",p."ExpiresAtUtc",'FreshAuthorityRequired','Forbidden',p."RevocationPolicyId",
  p."PurgePolicyId",p."LegalHoldPolicyId",now_utc);
 PERFORM set_config('tagekyc.raw_export_authority_snapshot_append_context',COALESCE(prior_context,''),true);
 RETURN QUERY SELECT next_revision,snapshot_id;
EXCEPTION WHEN OTHERS THEN
 PERFORM set_config('tagekyc.raw_export_authority_snapshot_append_context',COALESCE(prior_context,''),true);
 RAISE;
END $body$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_authority_snapshot(
    p_client_application_id uuid,
    p_verification_session_id uuid,
    p_capture_acceptance_id uuid,
    p_raw_class text,
    p_authority_artifact_id uuid,
    p_authority_artifact_version integer,
    p_controller_identity text,
    p_stable_data_scope_id text,
    p_retention_policy_id text,
    p_retention_policy_version integer,
    p_consent_policy_id uuid,
    p_consent_policy_version integer,
    p_retention_class text,
    p_retention_start_event text,
    p_absolute_source_expires_at_utc timestamptz,
    p_revocation_policy_id text,
    p_purge_policy_id text,
    p_legal_hold_policy_id text,
    p_evaluated_at_utc timestamptz,
    p_valid_until_utc timestamptz)
RETURNS TABLE("Revision" bigint, "AuthoritySnapshotId" uuid)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
    snapshot_id uuid;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_client_application_id IS NULL
       OR p_client_application_id =
            '00000000-0000-0000-0000-000000000000'::uuid
       OR p_verification_session_id IS NULL
       OR p_capture_acceptance_id IS NULL
       OR p_authority_artifact_id IS NULL
       OR p_authority_artifact_version < 1
       OR p_retention_policy_version < 1
       OR p_consent_policy_id IS NULL
       OR p_consent_policy_id =
            '00000000-0000-0000-0000-000000000000'::uuid
       OR p_consent_policy_version < 1
       OR pg_catalog.btrim(p_raw_class) = ''
       OR pg_catalog.btrim(p_controller_identity) = ''
       OR pg_catalog.btrim(p_stable_data_scope_id) = ''
       OR pg_catalog.btrim(p_retention_policy_id) = ''
       OR pg_catalog.btrim(p_retention_class) = ''
       OR pg_catalog.btrim(p_retention_start_event) = ''
       OR pg_catalog.btrim(p_revocation_policy_id) = ''
       OR pg_catalog.btrim(p_purge_policy_id) = ''
       OR pg_catalog.btrim(p_legal_hold_policy_id) = ''
       OR p_absolute_source_expires_at_utc IS NULL
       OR p_evaluated_at_utc IS NULL THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_VALUE_INVALID';
    END IF;
    IF p_absolute_source_expires_at_utc <= p_evaluated_at_utc
       OR (
            p_valid_until_utc IS NOT NULL
            AND p_valid_until_utc <=
                pg_catalog.transaction_timestamp()) THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_TIME_INVALID';
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;

    snapshot_id := pg_catalog.gen_random_uuid();
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'grant',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthorityKind","AuthoritySnapshotEventId","EventType","Revision",
            "ValidFromUtc","ValidUntilUtc","RecordedAtUtc",
            "CapturedByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass","AuthoritySnapshotSchemaVersion",
            "AuthoritySnapshotId","AuthorityArtifactId",
            "AuthorityArtifactVersion","ControllerIdentity",
            "ApprovedPurpose","StableDataScopeId",
            "RetentionPolicyId","RetentionPolicyVersion",
            "ConsentPolicyId","ConsentPolicyVersion",
            "RetentionClass","RetentionStartEvent",
            "AbsoluteSourceExpiresAtUtc","ReuseDisposition",
            "ExtensionDisposition","RevocationPolicyId",
            "PurgePolicyId","LegalHoldPolicyId","EvaluatedAtUtc")
    VALUES
        (
            'LegacyExport',pg_catalog.gen_random_uuid(),'Granted',next_revision,
            pg_catalog.transaction_timestamp(),p_valid_until_utc,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class,1,snapshot_id,
            p_authority_artifact_id,p_authority_artifact_version,
            p_controller_identity,'SubjectRawBiometricExport',
            p_stable_data_scope_id,p_retention_policy_id,
            p_retention_policy_version,p_consent_policy_id,
            p_consent_policy_version,p_retention_class,
            p_retention_start_event,
            p_absolute_source_expires_at_utc,
            'FreshAuthorityRequired','Forbidden',
            p_revocation_policy_id,p_purge_policy_id,
            p_legal_hold_policy_id,p_evaluated_at_utc);
    RETURN QUERY SELECT next_revision, snapshot_id;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_withdraw_authority_snapshot(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_target_revision bigint,
        p_withdrawn_by uuid)
RETURNS bigint
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
    target_kind text;
    target_binding uuid;
    probed_kind text;
    probed_binding uuid;
    reference_client uuid;
    reference_ref text;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_withdrawn_by IS DISTINCT FROM actor_id THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
    END IF;
    -- Probe only: retained targets take the actual session row before reference/scope.
    SELECT s."AuthorityKind",s."ConsentBindingId" INTO target_kind,target_binding
    FROM tagekyc.raw_export_authority_snapshots s
    WHERE s."ClientApplicationId"=p_client_application_id AND s."VerificationSessionId"=p_verification_session_id
      AND s."CaptureAcceptanceId"=p_capture_acceptance_id AND s."RawClass"=p_raw_class
      AND s."Revision"=p_target_revision AND s."EventType"='Granted';
    probed_kind:=target_kind; probed_binding:=target_binding;
    IF target_kind='SourceRetention' THEN
     PERFORM 1 FROM tagekyc.verification_sessions s WHERE s."Id"=p_verification_session_id
      AND s."ClientApplicationId"=p_client_application_id FOR UPDATE;
     IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID'; END IF;
     SELECT h."ClientApplicationId",h."ExternalConsentArtifactRef" INTO reference_client,reference_ref
 FROM tagekyc.raw_source_consent_bindings b
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
 WHERE b."ConsentBindingId"=target_binding;
 IF FOUND THEN
  PERFORM pg_advisory_xact_lock_shared(hashtextextended(
   'tip88c1:a3:consent-reference:'||reference_client::text||':'||reference_ref,0));
 END IF;
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;
    SELECT s."AuthorityKind",s."ConsentBindingId" INTO target_kind,target_binding FROM tagekyc.raw_export_authority_snapshots s
    WHERE s."ClientApplicationId"=p_client_application_id AND s."VerificationSessionId"=p_verification_session_id
      AND s."CaptureAcceptanceId"=p_capture_acceptance_id AND s."RawClass"=p_raw_class
      AND s."Revision"=p_target_revision AND s."EventType"='Granted';
    IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION'; END IF;
    IF target_kind='SourceRetention' AND
       (probed_kind IS DISTINCT FROM target_kind OR probed_binding IS DISTINCT FROM target_binding) THEN
      -- A target that became visible during the scope wait did not take the prefix.
      -- Do not acquire earlier locks now, and do not silently switch authority.
      RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
    END IF;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'withdraw',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthorityKind","AuthoritySnapshotEventId","EventType","Revision",
            "TargetRevision","RecordedAtUtc",
            "WithdrawnByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass")
    VALUES
        (
            target_kind,pg_catalog.gen_random_uuid(),'Withdrawn',
            next_revision,p_target_revision,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class);
    RETURN next_revision;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_revoke_authority_snapshot(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_target_revision bigint,
        p_revoked_by uuid)
RETURNS bigint
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
    target_kind text;
    target_binding uuid;
    probed_kind text;
    probed_binding uuid;
    reference_client uuid;
    reference_ref text;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_revoked_by IS DISTINCT FROM actor_id THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
    END IF;
    -- Probe only: retained targets take the actual session row before reference/scope.
    SELECT s."AuthorityKind",s."ConsentBindingId" INTO target_kind,target_binding
    FROM tagekyc.raw_export_authority_snapshots s
    WHERE s."ClientApplicationId"=p_client_application_id AND s."VerificationSessionId"=p_verification_session_id
      AND s."CaptureAcceptanceId"=p_capture_acceptance_id AND s."RawClass"=p_raw_class
      AND s."Revision"=p_target_revision AND s."EventType"='Granted';
    probed_kind:=target_kind; probed_binding:=target_binding;
    IF target_kind='SourceRetention' THEN
     PERFORM 1 FROM tagekyc.verification_sessions s WHERE s."Id"=p_verification_session_id
      AND s."ClientApplicationId"=p_client_application_id FOR UPDATE;
     IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID'; END IF;
     SELECT h."ClientApplicationId",h."ExternalConsentArtifactRef" INTO reference_client,reference_ref
 FROM tagekyc.raw_source_consent_bindings b
 JOIN tagekyc.raw_source_consent_references h ON h."ConsentReferenceId"=b."ConsentReferenceId"
 WHERE b."ConsentBindingId"=target_binding;
 IF FOUND THEN
  PERFORM pg_advisory_xact_lock_shared(hashtextextended(
   'tip88c1:a3:consent-reference:'||reference_client::text||':'||reference_ref,0));
 END IF;
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;
    SELECT s."AuthorityKind",s."ConsentBindingId" INTO target_kind,target_binding FROM tagekyc.raw_export_authority_snapshots s
    WHERE s."ClientApplicationId"=p_client_application_id AND s."VerificationSessionId"=p_verification_session_id
      AND s."CaptureAcceptanceId"=p_capture_acceptance_id AND s."RawClass"=p_raw_class
      AND s."Revision"=p_target_revision AND s."EventType"='Granted';
    IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION'; END IF;
    IF target_kind='SourceRetention' AND
       (probed_kind IS DISTINCT FROM target_kind OR probed_binding IS DISTINCT FROM target_binding) THEN
      -- A target that became visible during the scope wait did not take the prefix.
      -- Do not acquire earlier locks now, and do not silently switch authority.
      RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
    END IF;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'revoke',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthorityKind","AuthoritySnapshotEventId","EventType","Revision",
            "TargetRevision","RecordedAtUtc",
            "RevokedByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass")
    VALUES
        (
            target_kind,pg_catalog.gen_random_uuid(),'Revoked',
            next_revision,p_target_revision,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class);
    RETURN next_revision;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_resolve_current_authority_for_source(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_evaluated_at_utc timestamptz)
RETURNS TABLE(
    "Revision" bigint,
    "ValidFromUtc" timestamptz,
    "ValidUntilUtc" timestamptz,
    "AuthoritySnapshotSchemaVersion" integer,
    "AuthoritySnapshotId" uuid,
    "AuthorityArtifactId" uuid,
    "AuthorityArtifactVersion" integer,
    "ControllerIdentity" text,
    "ApprovedPurpose" text,
    "StableDataScopeId" text,
    "RetentionPolicyId" text,
    "RetentionPolicyVersion" integer,
    "ConsentPolicyId" uuid,
    "ConsentPolicyVersion" integer,
    "RetentionClass" text,
    "RetentionStartEvent" text,
    "AbsoluteSourceExpiresAtUtc" timestamptz,
    "ReuseDisposition" text,
    "ExtensionDisposition" text,
    "RevocationPolicyId" text,
    "PurgePolicyId" text,
    "LegalHoldPolicyId" text,
    "EvaluatedAtUtc" timestamptz)
LANGUAGE sql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
    WITH latest AS (
        SELECT snapshot.*
        FROM tagekyc.raw_export_authority_snapshots AS snapshot
        WHERE snapshot."ClientApplicationId" =
                p_client_application_id
          AND snapshot."VerificationSessionId" =
                p_verification_session_id
          AND snapshot."CaptureAcceptanceId" =
                p_capture_acceptance_id
          AND snapshot."RawClass" = p_raw_class
        ORDER BY snapshot."Revision" DESC
        LIMIT 1)
    SELECT
        latest."Revision",latest."ValidFromUtc",
        latest."ValidUntilUtc",
        latest."AuthoritySnapshotSchemaVersion",
        latest."AuthoritySnapshotId",
        latest."AuthorityArtifactId",
        latest."AuthorityArtifactVersion",
        latest."ControllerIdentity"::text,
        latest."ApprovedPurpose"::text,
        latest."StableDataScopeId"::text,
        latest."RetentionPolicyId"::text,
        latest."RetentionPolicyVersion",
        latest."ConsentPolicyId",
        latest."ConsentPolicyVersion",
        latest."RetentionClass"::text,
        latest."RetentionStartEvent"::text,
        latest."AbsoluteSourceExpiresAtUtc",
        latest."ReuseDisposition"::text,
        latest."ExtensionDisposition"::text,
        latest."RevocationPolicyId"::text,
        latest."PurgePolicyId"::text,
        latest."LegalHoldPolicyId"::text,
        latest."EvaluatedAtUtc"
    FROM latest
    WHERE latest."EventType" = 'Granted'
      AND p_evaluated_at_utc >= latest."ValidFromUtc"
      AND (
            latest."ValidUntilUtc" IS NULL
            OR p_evaluated_at_utc < latest."ValidUntilUtc")
      AND (latest."AuthorityKind"='LegacyExport'
       OR (latest."AuthorityKind"='SourceRetention' AND
        tagekyc.raw_export_retained_snapshot_is_current(latest."AuthoritySnapshotId",p_evaluated_at_utc)));
$$;
ALTER FUNCTION tagekyc.raw_export_retained_snapshot_is_current(uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_retained_snapshot_is_current(uuid,timestamptz) FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

ALTER FUNCTION tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint) FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

GRANT EXECUTE ON FUNCTION tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint) TO tagekyc_raw_export_claim_broker;
""";

    private const string SnapshotOperationsRestore = """
CREATE OR REPLACE FUNCTION
    tagekyc.enforce_raw_export_authority_snapshot_insert()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog
AS $$
DECLARE
    expected_context text;
    append_context text;
    actor_id uuid;
    acceptance_client_application_id uuid;
    acceptance_verification_session_id uuid;
    acceptance_raw_class text;
    current_revision bigint;
    current_event_type text;
    current_is_effective boolean;
BEGIN
    expected_context := CASE NEW."EventType"
        WHEN 'Granted' THEN 'grant'
        WHEN 'Withdrawn' THEN 'withdraw'
        WHEN 'Revoked' THEN 'revoke'
        ELSE NULL
    END;
    append_context := pg_catalog.current_setting(
        'tagekyc.raw_export_authority_snapshot_append_context',
        true);
    IF current_user <> 'tagekyc_raw_export_deployer'
       OR append_context IS DISTINCT FROM expected_context THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_DIRECT_INSERT_UNSUPPORTED';
    END IF;

    actor_id := tagekyc.raw_export_current_actor();
    SELECT
        acceptance."ClientApplicationId",
        acceptance."VerificationSessionId",
        acceptance."RawClass"
    INTO
        acceptance_client_application_id,
        acceptance_verification_session_id,
        acceptance_raw_class
    FROM tagekyc.raw_export_capture_acceptance_events AS acceptance
    WHERE acceptance."CaptureAcceptanceId" =
        NEW."CaptureAcceptanceId";
    IF NOT FOUND
       OR acceptance_client_application_id IS DISTINCT FROM
            NEW."ClientApplicationId"
       OR acceptance_verification_session_id IS DISTINCT FROM
            NEW."VerificationSessionId"
       OR acceptance_raw_class IS DISTINCT FROM NEW."RawClass" THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID';
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            NEW."ClientApplicationId"::text || ':' ||
            NEW."VerificationSessionId"::text || ':' ||
            NEW."CaptureAcceptanceId"::text || ':' ||
            NEW."RawClass"));

    SELECT
        snapshot."Revision",
        snapshot."EventType",
        (
            snapshot."EventType" = 'Granted'
            AND snapshot."ValidFromUtc" <=
                pg_catalog.transaction_timestamp()
            AND (
                snapshot."ValidUntilUtc" IS NULL
                OR pg_catalog.transaction_timestamp() <
                    snapshot."ValidUntilUtc")
        )
    INTO
        current_revision,
        current_event_type,
        current_is_effective
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            NEW."ClientApplicationId"
      AND snapshot."VerificationSessionId" =
            NEW."VerificationSessionId"
      AND snapshot."CaptureAcceptanceId" =
            NEW."CaptureAcceptanceId"
      AND snapshot."RawClass" = NEW."RawClass"
    ORDER BY snapshot."Revision" DESC
    LIMIT 1;
    current_revision := COALESCE(current_revision, 0);

    IF NEW."Revision" <> current_revision + 1 THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_REVISION_CONFLICT';
    END IF;

    IF NEW."EventType" = 'Granted' THEN
        IF NEW."CapturedByPrincipalId" IS DISTINCT FROM actor_id
           OR NEW."TargetRevision" IS NOT NULL THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
        END IF;
    ELSIF NEW."EventType" = 'Withdrawn' THEN
        IF NEW."WithdrawnByPrincipalId" IS DISTINCT FROM actor_id
           OR current_event_type IS DISTINCT FROM 'Granted'
           OR current_is_effective IS DISTINCT FROM true
           OR NEW."TargetRevision" IS DISTINCT FROM
                current_revision THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
        END IF;
    ELSIF NEW."EventType" = 'Revoked' THEN
        IF NEW."RevokedByPrincipalId" IS DISTINCT FROM actor_id
           OR current_event_type IS DISTINCT FROM 'Granted'
           OR current_is_effective IS DISTINCT FROM true
           OR NEW."TargetRevision" IS DISTINCT FROM
                current_revision THEN
            RAISE EXCEPTION
                'RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION';
        END IF;
    ELSE
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_EVENT_INVALID';
    END IF;

    NEW."RecordedAtUtc" :=
        pg_catalog.transaction_timestamp();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_withdraw_authority_snapshot(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_target_revision bigint,
        p_withdrawn_by uuid)
RETURNS bigint
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_withdrawn_by IS DISTINCT FROM actor_id THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'withdraw',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthoritySnapshotEventId","EventType","Revision",
            "TargetRevision","RecordedAtUtc",
            "WithdrawnByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass")
    VALUES
        (
            pg_catalog.gen_random_uuid(),'Withdrawn',
            next_revision,p_target_revision,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class);
    RETURN next_revision;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_revoke_authority_snapshot(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_target_revision bigint,
        p_revoked_by uuid)
RETURNS bigint
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_revoked_by IS DISTINCT FROM actor_id THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH';
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'revoke',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthoritySnapshotEventId","EventType","Revision",
            "TargetRevision","RecordedAtUtc",
            "RevokedByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass")
    VALUES
        (
            pg_catalog.gen_random_uuid(),'Revoked',
            next_revision,p_target_revision,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class);
    RETURN next_revision;
END;
$$;

CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_authority_snapshot(
    p_client_application_id uuid,
    p_verification_session_id uuid,
    p_capture_acceptance_id uuid,
    p_raw_class text,
    p_authority_artifact_id uuid,
    p_authority_artifact_version integer,
    p_controller_identity text,
    p_stable_data_scope_id text,
    p_retention_policy_id text,
    p_retention_policy_version integer,
    p_consent_policy_id uuid,
    p_consent_policy_version integer,
    p_retention_class text,
    p_retention_start_event text,
    p_absolute_source_expires_at_utc timestamptz,
    p_revocation_policy_id text,
    p_purge_policy_id text,
    p_legal_hold_policy_id text,
    p_evaluated_at_utc timestamptz,
    p_valid_until_utc timestamptz)
RETURNS TABLE("Revision" bigint, "AuthoritySnapshotId" uuid)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    next_revision bigint;
    snapshot_id uuid;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    IF p_client_application_id IS NULL
       OR p_client_application_id =
            '00000000-0000-0000-0000-000000000000'::uuid
       OR p_verification_session_id IS NULL
       OR p_capture_acceptance_id IS NULL
       OR p_authority_artifact_id IS NULL
       OR p_authority_artifact_version < 1
       OR p_retention_policy_version < 1
       OR p_consent_policy_id IS NULL
       OR p_consent_policy_id =
            '00000000-0000-0000-0000-000000000000'::uuid
       OR p_consent_policy_version < 1
       OR pg_catalog.btrim(p_raw_class) = ''
       OR pg_catalog.btrim(p_controller_identity) = ''
       OR pg_catalog.btrim(p_stable_data_scope_id) = ''
       OR pg_catalog.btrim(p_retention_policy_id) = ''
       OR pg_catalog.btrim(p_retention_class) = ''
       OR pg_catalog.btrim(p_retention_start_event) = ''
       OR pg_catalog.btrim(p_revocation_policy_id) = ''
       OR pg_catalog.btrim(p_purge_policy_id) = ''
       OR pg_catalog.btrim(p_legal_hold_policy_id) = ''
       OR p_absolute_source_expires_at_utc IS NULL
       OR p_evaluated_at_utc IS NULL THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_VALUE_INVALID';
    END IF;
    IF p_absolute_source_expires_at_utc <= p_evaluated_at_utc
       OR (
            p_valid_until_utc IS NOT NULL
            AND p_valid_until_utc <=
                pg_catalog.transaction_timestamp()) THEN
        RAISE EXCEPTION
            'RAW_EXPORT_AUTHORITY_SNAPSHOT_TIME_INVALID';
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(
        pg_catalog.hashtext(
            'tip88c1:b2-authority:' ||
            p_client_application_id::text || ':' ||
            p_verification_session_id::text || ':' ||
            p_capture_acceptance_id::text || ':' ||
            p_raw_class));
    SELECT COALESCE(MAX(snapshot."Revision"), 0) + 1
    INTO next_revision
    FROM tagekyc.raw_export_authority_snapshots AS snapshot
    WHERE snapshot."ClientApplicationId" =
            p_client_application_id
      AND snapshot."VerificationSessionId" =
            p_verification_session_id
      AND snapshot."CaptureAcceptanceId" =
            p_capture_acceptance_id
      AND snapshot."RawClass" = p_raw_class;

    snapshot_id := pg_catalog.gen_random_uuid();
    PERFORM pg_catalog.set_config(
        'tagekyc.raw_export_authority_snapshot_append_context',
        'grant',
        true);
    INSERT INTO tagekyc.raw_export_authority_snapshots
        (
            "AuthoritySnapshotEventId","EventType","Revision",
            "ValidFromUtc","ValidUntilUtc","RecordedAtUtc",
            "CapturedByPrincipalId","ClientApplicationId",
            "VerificationSessionId","CaptureAcceptanceId",
            "RawClass","AuthoritySnapshotSchemaVersion",
            "AuthoritySnapshotId","AuthorityArtifactId",
            "AuthorityArtifactVersion","ControllerIdentity",
            "ApprovedPurpose","StableDataScopeId",
            "RetentionPolicyId","RetentionPolicyVersion",
            "ConsentPolicyId","ConsentPolicyVersion",
            "RetentionClass","RetentionStartEvent",
            "AbsoluteSourceExpiresAtUtc","ReuseDisposition",
            "ExtensionDisposition","RevocationPolicyId",
            "PurgePolicyId","LegalHoldPolicyId","EvaluatedAtUtc")
    VALUES
        (
            pg_catalog.gen_random_uuid(),'Granted',next_revision,
            pg_catalog.transaction_timestamp(),p_valid_until_utc,
            pg_catalog.transaction_timestamp(),actor_id,
            p_client_application_id,p_verification_session_id,
            p_capture_acceptance_id,p_raw_class,1,snapshot_id,
            p_authority_artifact_id,p_authority_artifact_version,
            p_controller_identity,'SubjectRawBiometricExport',
            p_stable_data_scope_id,p_retention_policy_id,
            p_retention_policy_version,p_consent_policy_id,
            p_consent_policy_version,p_retention_class,
            p_retention_start_event,
            p_absolute_source_expires_at_utc,
            'FreshAuthorityRequired','Forbidden',
            p_revocation_policy_id,p_purge_policy_id,
            p_legal_hold_policy_id,p_evaluated_at_utc);
    RETURN QUERY SELECT next_revision, snapshot_id;
END;
$$;

CREATE OR REPLACE FUNCTION
    tagekyc.raw_export_resolve_current_authority_for_source(
        p_client_application_id uuid,
        p_verification_session_id uuid,
        p_capture_acceptance_id uuid,
        p_raw_class text,
        p_evaluated_at_utc timestamptz)
RETURNS TABLE(
    "Revision" bigint,
    "ValidFromUtc" timestamptz,
    "ValidUntilUtc" timestamptz,
    "AuthoritySnapshotSchemaVersion" integer,
    "AuthoritySnapshotId" uuid,
    "AuthorityArtifactId" uuid,
    "AuthorityArtifactVersion" integer,
    "ControllerIdentity" text,
    "ApprovedPurpose" text,
    "StableDataScopeId" text,
    "RetentionPolicyId" text,
    "RetentionPolicyVersion" integer,
    "ConsentPolicyId" uuid,
    "ConsentPolicyVersion" integer,
    "RetentionClass" text,
    "RetentionStartEvent" text,
    "AbsoluteSourceExpiresAtUtc" timestamptz,
    "ReuseDisposition" text,
    "ExtensionDisposition" text,
    "RevocationPolicyId" text,
    "PurgePolicyId" text,
    "LegalHoldPolicyId" text,
    "EvaluatedAtUtc" timestamptz)
LANGUAGE sql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
    WITH latest AS (
        SELECT snapshot.*
        FROM tagekyc.raw_export_authority_snapshots AS snapshot
        WHERE snapshot."ClientApplicationId" =
                p_client_application_id
          AND snapshot."VerificationSessionId" =
                p_verification_session_id
          AND snapshot."CaptureAcceptanceId" =
                p_capture_acceptance_id
          AND snapshot."RawClass" = p_raw_class
        ORDER BY snapshot."Revision" DESC
        LIMIT 1)
    SELECT
        latest."Revision",latest."ValidFromUtc",
        latest."ValidUntilUtc",
        latest."AuthoritySnapshotSchemaVersion",
        latest."AuthoritySnapshotId",
        latest."AuthorityArtifactId",
        latest."AuthorityArtifactVersion",
        latest."ControllerIdentity"::text,
        latest."ApprovedPurpose"::text,
        latest."StableDataScopeId"::text,
        latest."RetentionPolicyId"::text,
        latest."RetentionPolicyVersion",
        latest."ConsentPolicyId",
        latest."ConsentPolicyVersion",
        latest."RetentionClass"::text,
        latest."RetentionStartEvent"::text,
        latest."AbsoluteSourceExpiresAtUtc",
        latest."ReuseDisposition"::text,
        latest."ExtensionDisposition"::text,
        latest."RevocationPolicyId"::text,
        latest."PurgePolicyId"::text,
        latest."LegalHoldPolicyId"::text,
        latest."EvaluatedAtUtc"
    FROM latest
    WHERE latest."EventType" = 'Granted'
      AND p_evaluated_at_utc >= latest."ValidFromUtc"
      AND (
            latest."ValidUntilUtc" IS NULL
            OR p_evaluated_at_utc < latest."ValidUntilUtc");
$$;
DROP FUNCTION tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint);
DROP FUNCTION tagekyc.raw_export_retained_snapshot_is_current(uuid,timestamptz);
""";

    private const string SnapshotSchema = """
ALTER TABLE tagekyc.raw_export_authority_snapshots
 ADD COLUMN "AuthorityKind" varchar(32) NOT NULL DEFAULT 'LegacyExport',
 ADD COLUMN "RetentionAuthorityId" uuid,
 ADD COLUMN "RetentionAuthorityRevision" bigint,
 ADD COLUMN "ConsentBindingId" uuid,
 ADD COLUMN "CustodyPrincipalId" uuid,
 ADD COLUMN "RuntimeBindingId" uuid,
 ADD CONSTRAINT ck_a3_snapshot_authority_kind CHECK ("AuthorityKind" IN ('LegacyExport','SourceRetention')),
 ADD CONSTRAINT ck_a3_snapshot_retention_shape CHECK (("RetentionAuthorityId" IS NULL OR "RetentionAuthorityId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("RetentionAuthorityRevision" IS NULL OR "RetentionAuthorityRevision">0)
AND ("ConsentBindingId" IS NULL OR "ConsentBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("CustodyPrincipalId" IS NULL OR "CustodyPrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("RuntimeBindingId" IS NULL OR "RuntimeBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND (
  ("AuthorityKind"='LegacyExport' AND "RetentionAuthorityId" IS NULL
   AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL
   AND "CustodyPrincipalId" IS NULL AND "RuntimeBindingId" IS NULL
   AND ("ApprovedPurpose" IS NULL OR "ApprovedPurpose"='SubjectRawBiometricExport'))
  OR ("AuthorityKind"='SourceRetention' AND (
    ("EventType"='Granted' AND "ApprovedPurpose"='SourceRetention'
     AND "RetentionAuthorityId" IS NOT NULL AND "RetentionAuthorityRevision" IS NOT NULL
     AND "RetentionAuthorityRevision">=1 AND "ConsentBindingId" IS NOT NULL
     AND "CustodyPrincipalId" IS NOT NULL AND "RuntimeBindingId" IS NOT NULL
     AND "CustodyPrincipalId"="CapturedByPrincipalId")
    OR ("EventType" IN ('Withdrawn','Revoked') AND "RetentionAuthorityId" IS NULL
     AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL
     AND "CustodyPrincipalId" IS NULL AND "RuntimeBindingId" IS NULL AND "ApprovedPurpose" IS NULL)
  ))
)),
 ADD CONSTRAINT fk_a3_snapshot_retention_permit FOREIGN KEY ("RetentionAuthorityId","RetentionAuthorityRevision")
 REFERENCES tagekyc.raw_source_retention_permits("RetentionAuthorityId","Revision") ON DELETE RESTRICT,
 ADD CONSTRAINT fk_a3_snapshot_consent_binding FOREIGN KEY ("ConsentBindingId")
 REFERENCES tagekyc.raw_source_consent_bindings("ConsentBindingId") ON DELETE RESTRICT,
 ADD CONSTRAINT fk_a3_snapshot_runtime_binding FOREIGN KEY ("RuntimeBindingId")
 REFERENCES tagekyc.capture_execution_bindings("CaptureExecutionBindingId") ON DELETE RESTRICT;
ALTER TABLE tagekyc.raw_export_authority_snapshots DROP CONSTRAINT ck_raw_export_authority_snapshot_values;
ALTER TABLE tagekyc.raw_export_authority_snapshots ADD CONSTRAINT ck_raw_export_authority_snapshot_values CHECK ("Revision" >= 1
AND ("TargetRevision" IS NULL OR "TargetRevision" >= 1)
AND "ClientApplicationId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND "VerificationSessionId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND "CaptureAcceptanceId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND btrim("RawClass") <> ''
AND ("AuthoritySnapshotSchemaVersion" IS NULL OR "AuthoritySnapshotSchemaVersion" = 1)
AND ("AuthorityArtifactVersion" IS NULL OR "AuthorityArtifactVersion" >= 1)
AND ("RetentionPolicyVersion" IS NULL OR "RetentionPolicyVersion" >= 1)
AND ("ConsentPolicyVersion" IS NULL OR "ConsentPolicyVersion" >= 1)
AND ("AuthoritySnapshotId" IS NULL OR "AuthoritySnapshotId" <> '00000000-0000-0000-0000-000000000000'::uuid)
AND ("AuthorityArtifactId" IS NULL OR "AuthorityArtifactId" <> '00000000-0000-0000-0000-000000000000'::uuid)
AND ("ControllerIdentity" IS NULL OR btrim("ControllerIdentity") <> '')
AND ("ApprovedPurpose" IS NULL OR "ApprovedPurpose" IN ('SubjectRawBiometricExport','SourceRetention'))
AND ("StableDataScopeId" IS NULL OR btrim("StableDataScopeId") <> '')
AND ("RetentionPolicyId" IS NULL OR btrim("RetentionPolicyId") <> '')
AND ("RetentionClass" IS NULL OR btrim("RetentionClass") <> '')
AND ("RetentionStartEvent" IS NULL OR btrim("RetentionStartEvent") <> '')
AND ("ReuseDisposition" IS NULL OR "ReuseDisposition" = 'FreshAuthorityRequired')
AND ("ExtensionDisposition" IS NULL OR "ExtensionDisposition" = 'Forbidden')
AND ("RevocationPolicyId" IS NULL OR btrim("RevocationPolicyId") <> '')
AND ("PurgePolicyId" IS NULL OR btrim("PurgePolicyId") <> '')
AND ("LegalHoldPolicyId" IS NULL OR btrim("LegalHoldPolicyId") <> '')
AND (
    "AbsoluteSourceExpiresAtUtc" IS NULL
    OR "EvaluatedAtUtc" IS NULL
    OR "AbsoluteSourceExpiresAtUtc" > "EvaluatedAtUtc"
));
CREATE INDEX ix_a3_snapshot_retention_permit
 ON tagekyc.raw_export_authority_snapshots("RetentionAuthorityId","RetentionAuthorityRevision")
 WHERE "AuthorityKind"='SourceRetention' AND "EventType"='Granted';
CREATE UNIQUE INDEX uq_a3_snapshot_retained_grant
 ON tagekyc.raw_export_authority_snapshots("ClientApplicationId","VerificationSessionId","CaptureAcceptanceId","RawClass","RetentionAuthorityId","RetentionAuthorityRevision","RuntimeBindingId")
 WHERE "AuthorityKind"='SourceRetention' AND "EventType"='Granted';
""";

    private const string SnapshotSchemaRestore = """
ALTER TABLE tagekyc.raw_export_authority_snapshots
 DROP CONSTRAINT fk_a3_snapshot_runtime_binding,
 DROP CONSTRAINT fk_a3_snapshot_consent_binding,
 DROP CONSTRAINT fk_a3_snapshot_retention_permit,
 DROP CONSTRAINT ck_a3_snapshot_retention_shape,
 DROP CONSTRAINT ck_a3_snapshot_authority_kind,
 DROP CONSTRAINT ck_raw_export_authority_snapshot_values;
DROP INDEX tagekyc.uq_a3_snapshot_retained_grant;
DROP INDEX tagekyc.ix_a3_snapshot_retention_permit;
ALTER TABLE tagekyc.raw_export_authority_snapshots
 DROP COLUMN "RuntimeBindingId", DROP COLUMN "CustodyPrincipalId", DROP COLUMN "ConsentBindingId",
 DROP COLUMN "RetentionAuthorityRevision", DROP COLUMN "RetentionAuthorityId", DROP COLUMN "AuthorityKind",
 ADD CONSTRAINT ck_raw_export_authority_snapshot_values CHECK ("Revision" >= 1
AND ("TargetRevision" IS NULL OR "TargetRevision" >= 1)
AND "ClientApplicationId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND "VerificationSessionId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND "CaptureAcceptanceId" <> '00000000-0000-0000-0000-000000000000'::uuid
AND btrim("RawClass") <> ''
AND ("AuthoritySnapshotSchemaVersion" IS NULL OR "AuthoritySnapshotSchemaVersion" = 1)
AND ("AuthorityArtifactVersion" IS NULL OR "AuthorityArtifactVersion" >= 1)
AND ("RetentionPolicyVersion" IS NULL OR "RetentionPolicyVersion" >= 1)
AND ("ConsentPolicyVersion" IS NULL OR "ConsentPolicyVersion" >= 1)
AND ("AuthoritySnapshotId" IS NULL OR "AuthoritySnapshotId" <> '00000000-0000-0000-0000-000000000000'::uuid)
AND ("AuthorityArtifactId" IS NULL OR "AuthorityArtifactId" <> '00000000-0000-0000-0000-000000000000'::uuid)
AND ("ControllerIdentity" IS NULL OR btrim("ControllerIdentity") <> '')
AND ("ApprovedPurpose" IS NULL OR "ApprovedPurpose" = 'SubjectRawBiometricExport')
AND ("StableDataScopeId" IS NULL OR btrim("StableDataScopeId") <> '')
AND ("RetentionPolicyId" IS NULL OR btrim("RetentionPolicyId") <> '')
AND ("RetentionClass" IS NULL OR btrim("RetentionClass") <> '')
AND ("RetentionStartEvent" IS NULL OR btrim("RetentionStartEvent") <> '')
AND ("ReuseDisposition" IS NULL OR "ReuseDisposition" = 'FreshAuthorityRequired')
AND ("ExtensionDisposition" IS NULL OR "ExtensionDisposition" = 'Forbidden')
AND ("RevocationPolicyId" IS NULL OR btrim("RevocationPolicyId") <> '')
AND ("PurgePolicyId" IS NULL OR btrim("PurgePolicyId") <> '')
AND ("LegalHoldPolicyId" IS NULL OR btrim("LegalHoldPolicyId") <> '')
AND (
    "AbsoluteSourceExpiresAtUtc" IS NULL
    OR "EvaluatedAtUtc" IS NULL
    OR "AbsoluteSourceExpiresAtUtc" > "EvaluatedAtUtc"
));
""";

    private const string B2WithdrawalRestore = """
CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_withdrawn(
    verification_session_id uuid,
    policy_id uuid,
    policy_version integer,
    expected_revision integer,
    target_revision integer,
    decision_ref text,
    external_consent_artifact_ref text)
RETURNS integer
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    session_subject text;
    session_owner uuid;
    scope_hash bytea;
    current_revision integer;
    current_event text;
    current_valid boolean;
    next_revision integer;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    SELECT "SubjectRef", "ClientApplicationId"
    INTO session_subject, session_owner
    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
    IF NOT FOUND THEN
        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentWithdrawer'));
    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentWithdrawer') THEN
        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
    END IF;
    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
    END IF;

    scope_hash := tagekyc.raw_export_consent_scope_hash(
        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
    SELECT "Revision", "EventType",
           ("EventType" = 'Granted'
            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
    INTO current_revision, current_event, current_valid
    FROM tagekyc.raw_export_subject_consent_events
    WHERE "VerificationSessionId" = verification_session_id
      AND "SubjectRef" = session_subject
      AND "PolicyId" = policy_id
      AND "PolicyVersion" = policy_version
      AND "PurposeCode" = 'SubjectRawBiometricExport'
      AND "RecipientClientApplicationId" = session_owner
    ORDER BY "Revision" DESC
    LIMIT 1;
    current_revision := COALESCE(current_revision, 0);
    IF current_revision <> expected_revision THEN
        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_REVISION_CONFLICT';
    END IF;
    IF current_event IS DISTINCT FROM 'Granted' OR current_valid IS DISTINCT FROM true OR target_revision IS DISTINCT FROM current_revision THEN
        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_STALE_TARGET_REVISION';
    END IF;
    next_revision := current_revision + 1;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
    INSERT INTO tagekyc.raw_export_subject_consent_events
        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
         "Revision","EventType","TargetRevision","ExternalConsentArtifactRef","DecisionRef","WithdrawnByPrincipalId","RecordedAtUtc")
    VALUES
        (pg_catalog.gen_random_uuid(), scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
         next_revision, 'Withdrawn', target_revision, external_consent_artifact_ref, decision_ref, actor_id, pg_catalog.transaction_timestamp());
    RETURN next_revision;
END;
$$;
""";

    private const string B2WithdrawalOperation = """
CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_withdrawn(
    verification_session_id uuid,
    policy_id uuid,
    policy_version integer,
    expected_revision integer,
    target_revision integer,
    decision_ref text,
    external_consent_artifact_ref text)
RETURNS integer
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $$
DECLARE
    actor_id uuid;
    session_subject text;
    session_owner uuid;
    scope_hash bytea;
    current_revision integer;
    current_event text;
    current_valid boolean;
    next_revision integer;
    a3_target_id uuid;
    a3_reference text;
    a3_head tagekyc.raw_source_consent_references%ROWTYPE;
    a3_previous tagekyc.raw_source_consent_reference_events%ROWTYPE;
    a3_b2_event_id uuid;
    a3_saved_context text;
    a3_now timestamptz;
BEGIN
    actor_id := tagekyc.raw_export_current_actor();
    SELECT "SubjectRef", "ClientApplicationId"
    INTO session_subject, session_owner
    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
    IF NOT FOUND THEN
        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentWithdrawer'));
    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentWithdrawer') THEN
        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
    END IF;
    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
    END IF;

    -- Immutable Granted target supplies the reference, never the optional
    -- caller observation. Session/authority prefix precedes reference/scope.
    SELECT e."SubjectConsentRecordId",e."ExternalConsentArtifactRef"
    INTO a3_target_id,a3_reference
    FROM tagekyc.raw_export_subject_consent_events e
    WHERE e."VerificationSessionId"=verification_session_id AND e."SubjectRef"=session_subject
      AND e."PolicyId"=policy_id AND e."PolicyVersion"=policy_version
      AND e."PurposeCode"='SubjectRawBiometricExport'
      AND e."RecipientClientApplicationId"=session_owner
      AND e."Revision"=target_revision AND e."EventType"='Granted';
    IF a3_reference IS NOT NULL THEN
      PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(
        'tip88c1:a3:consent-reference:'||session_owner::text||':'||a3_reference,0));
    END IF;
    scope_hash := tagekyc.raw_export_consent_scope_hash(
        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
    SELECT "Revision", "EventType",
           ("EventType" = 'Granted'
            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
    INTO current_revision, current_event, current_valid
    FROM tagekyc.raw_export_subject_consent_events
    WHERE "VerificationSessionId" = verification_session_id
      AND "SubjectRef" = session_subject
      AND "PolicyId" = policy_id
      AND "PolicyVersion" = policy_version
      AND "PurposeCode" = 'SubjectRawBiometricExport'
      AND "RecipientClientApplicationId" = session_owner
    ORDER BY "Revision" DESC
    LIMIT 1;
    current_revision := COALESCE(current_revision, 0);
    IF current_revision <> expected_revision THEN
        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_REVISION_CONFLICT';
    END IF;
    IF current_event IS DISTINCT FROM 'Granted' OR current_valid IS DISTINCT FROM true OR target_revision IS DISTINCT FROM current_revision THEN
        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_STALE_TARGET_REVISION';
    END IF;
    IF NOT EXISTS (
      SELECT 1 FROM tagekyc.raw_export_subject_consent_events e
      WHERE e."SubjectConsentRecordId"=a3_target_id AND e."ExternalConsentArtifactRef"=a3_reference
       AND e."VerificationSessionId"=verification_session_id AND e."SubjectRef"=session_subject
       AND e."PolicyId"=policy_id AND e."PolicyVersion"=policy_version
       AND e."PurposeCode"='SubjectRawBiometricExport'
       AND e."RecipientClientApplicationId"=session_owner AND e."Revision"=target_revision
       AND e."EventType"='Granted') THEN
      RAISE EXCEPTION 'RAW_EXPORT_CONSENT_STALE_TARGET_REVISION';
    END IF;
    SELECT * INTO a3_head FROM tagekyc.raw_source_consent_references r
    WHERE r."ClientApplicationId"=session_owner AND r."ExternalConsentArtifactRef"=a3_reference
      AND r."SubjectRef"=session_subject
    FOR UPDATE;
    next_revision := current_revision + 1;
    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
    INSERT INTO tagekyc.raw_export_subject_consent_events
        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
         "Revision","EventType","TargetRevision","ExternalConsentArtifactRef","DecisionRef","WithdrawnByPrincipalId","RecordedAtUtc")
    VALUES
        (pg_catalog.gen_random_uuid(), scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
         next_revision, 'Withdrawn', target_revision, external_consent_artifact_ref, decision_ref, actor_id, pg_catalog.transaction_timestamp())
    RETURNING "SubjectConsentRecordId" INTO a3_b2_event_id;
    IF a3_head."ConsentReferenceId" IS NOT NULL THEN
      SELECT * INTO STRICT a3_previous FROM tagekyc.raw_source_consent_reference_events e
      WHERE e."ConsentReferenceId"=a3_head."ConsentReferenceId" AND e."Revision"=a3_head."CurrentRevision";
      IF a3_previous."EventType"<>'Withdrawn' THEN
        a3_saved_context:=pg_catalog.current_setting('tagekyc.a3_authority_write',true);
        BEGIN
          PERFORM pg_catalog.set_config('tagekyc.a3_authority_write','B2-Withdrawal',true);
          a3_now:=pg_catalog.clock_timestamp();
          INSERT INTO tagekyc.raw_source_consent_reference_events
           ("ConsentReferenceId","Revision","EventType","SourceVersion","ConsentTextVersion","ConsentTextContentHash",
            "ValidFromUtc","ValidUntilUtc","RecordedByPrincipalId","RecordedAtUtc","OperationDomain",
            "IdempotencyKey","RequestFingerprint","DecisionRef")
          VALUES(a3_head."ConsentReferenceId",a3_head."CurrentRevision"+1,'Withdrawn',a3_previous."SourceVersion",
           a3_previous."ConsentTextVersion",a3_previous."ConsentTextContentHash",a3_previous."ValidFromUtc",
           a3_previous."ValidUntilUtc",actor_id,a3_now,'B2-Withdrawal',a3_b2_event_id,
           tagekyc_extensions.digest(pg_catalog.uuid_send(a3_b2_event_id),'sha256'),decision_ref);
          UPDATE tagekyc.raw_source_consent_references SET "CurrentRevision"="CurrentRevision"+1
          WHERE "ConsentReferenceId"=a3_head."ConsentReferenceId" AND "CurrentRevision"=a3_head."CurrentRevision";
          IF NOT FOUND THEN RAISE EXCEPTION 'A3_B2_WITHDRAWAL_HEAD_CONFLICT'; END IF;
          PERFORM pg_catalog.set_config('tagekyc.a3_authority_write',COALESCE(a3_saved_context,''),true);
        EXCEPTION WHEN OTHERS THEN
          PERFORM pg_catalog.set_config('tagekyc.a3_authority_write',COALESCE(a3_saved_context,''),true);
          RAISE;
        END;
      END IF;
    END IF;
    RETURN next_revision;
END;
$$;
""";

    private const string CapturePredecessorGuard = """
DO $guard$
DECLARE expected record; actual text;
BEGIN
 FOR expected IN SELECT * FROM (VALUES
  ('tagekyc.enforce_raw_export_source_core_write()','1af0b6698d797bdecd83bf1b3d4b537e176d5f658db3b1f8b771508ee34b90fe'),
  ('tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)','84848b69cf9afe73a30341fc7159741b9372fbf25a960f411afdf5f52bafedfe'),
  ('tagekyc.enforce_raw_export_source_head_write()','2d3762f5c3e6e4b1bd8c37457b21ec6a9e50747df4daae104037ce6f146107f4'),
  ('tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)','300d901463c0c684ccdc1bed43a07410302b277dc426ef12835c765704af72cc'),
  ('tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)','ece10e154991d513bf3a8cc04c6b450d0687a483c941abb89c6377b5296497aa'),
  ('tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)','ac8adde6c830f269cfe841cce04a45e5121ba2f7aafc40adb9c3516d8c3c3dc1'),
  ('tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text)','b50f46227428922e5fa6b358daad34ad562733e62d7918d92b6023455447f55a'),
  ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)','5d8510bc5acc479b4292e71891669b77d5b54fc48f4875c5cf6ebd68407cb2be'),
  ('tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz)','56cf6091fd3082a4501be4139fa69b612218ce9ec9513321be64bb28c4938744'),
  ('tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz)','fe355513262c49da8863907a3261e895bba66dfdad968e65c3bc4666c671cf06'),
  ('tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text)','e1ddb424a95e69aa3cfb043acd4d4b394f99c9cfd500c1e9d9b5ac0bab8778b0'),
  ('tagekyc.enforce_raw_export_authority_snapshot_insert()','ce5797b3c11b3702188fb455e78296f441224d9daacae0b091c3630103fa3cee'),
  ('tagekyc.raw_export_withdraw_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)','33a77dc488160683d8d57f433d7c17c938d8871c8c4eaf22d82c70996eddb139'),
  ('tagekyc.raw_export_revoke_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)','689c358cd90a7d73cbd742ba45d38cb3eda5f9bf73d6f39ad49980e442899858'),
  ('tagekyc.raw_export_append_authority_snapshot(uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,uuid,integer,text,text,timestamptz,text,text,text,timestamptz,timestamptz)','4fc04ccb8d14e63bd83c17a67b656affa55b49c0e77e9633993c97437155356a'),
  ('tagekyc.raw_export_resolve_current_authority_for_source(uuid,uuid,uuid,text,timestamptz)','6b972fd28c391487e139bf3cffcb2fce1a72bb4b922d15761e73f9d19d915997'),
  ('tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)','4a2c88fa71817445ae8766f47b6748051792e741a8af65c52036a3d47ee3627b'),
  ('tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)','005f61a0a8a79cd825fff29062e017ec6c1aea819ceaf21a006bf29396340b6a'),
  ('tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)','7f1105d02573d57f9c596edf9e8646310d46947e5b1f61b24d5df844e4422d58'),
  ('tagekyc.complete_raw_export_source_ingress_claim(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)','b7abf8f3f21d70a9af7be33d8ea780773bf09dc41aee82ff9fcaddd79b11e8a9')
 ) AS bodies(signature,sha256) LOOP
  SELECT p.prosrc INTO actual FROM pg_catalog.pg_proc p WHERE p.oid=pg_catalog.to_regprocedure(expected.signature);
  IF NOT FOUND OR encode(tagekyc_extensions.digest(convert_to(replace(actual,chr(13)||chr(10),chr(10)),'UTF8'),'sha256'),'hex')<>expected.sha256 THEN
   RAISE EXCEPTION 'A3_CAPTURE_PREDECESSOR_BODY_MISMATCH';
  END IF;
 END LOOP;
END $guard$;
""";

    private const string CaptureCurrentGuard = """
DO $guard$
DECLARE expected record; actual text;
BEGIN
 FOR expected IN SELECT * FROM (VALUES
  ('tagekyc.enforce_raw_export_source_core_write()','16e03c5fa37026d83460bc83aa001706cb1dc9a8141dd763f52fcd8fc898d54b'),
  ('tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)','b98e3708bdd0c4beae2eb81e4c926377afc7a0f7050cdc6a1c20fb15b5c6ea4c'),
  ('tagekyc.raw_export_read_retained_source_continuation(uuid)','45da395d2e8db6e57262bcafdfab88ca28ab7096227cdbc0ae69abdd0228e3ce'),
  ('tagekyc.raw_export_list_retained_source_continuations(uuid,integer)','0b6f63b6ff51f53020ef1d19d715da490a159c21ff71ccc128fa9acdbbcf36bf'),
  ('tagekyc.raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text)','a4fbb5a01ab6fb1b748b21d52999bf636a4b3b677020f178000cd4e001d6806a'),
  ('tagekyc.raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint)','e19ad98bd9ac8b04058c69404c42ce04357b6551b77ac3d094e5a1db42c76d0a'),
  ('tagekyc.enforce_raw_export_source_head_write()','15c41029e8fe82f21dd50531192ae5cdbbef1f59aa8d44a856880022b6c59212'),
  ('tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)','8ac3abba7ebd36276c5b24ad322fa59f76b797ba69e62fb40a2d7892e75abc26'),
  ('tagekyc.raw_export_reenter_retained_source(uuid,uuid,bigint,bigint,bigint,uuid,integer,integer,integer)','a66c40d29f7e3bd33cf65c5bb58fd52d11a5dc8591f23fe9cddd7622e2e098b7'),
  ('tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)','c9207a7fbd9192bea2ad534fa1f26d43c246e03f4704de1e6055c198f956f66b'),
  ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)','47cadb9dcd3d74044ba50e8c0ebe2c3f5835fd79154a4b9a6474f8e29dd02465'),
  ('tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)','b2866ebbc926436a84fea9fdc77d890a13c763725d6c83af8f993a625c7efe03'),
  ('tagekyc.raw_export_record_r2_terminal_outcome(uuid,uuid,bigint,bigint,text,text)','5ee03515210fee71b6f186f00e51764702362899f8afc90d6a05648ec2ad7f6c'),
  ('tagekyc.raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)','60e304b6bebbb05699d4db8cdab6b8f2e5a50ddb574daccb9fd83525feea7dea'),
  ('tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb)','399d854e4fd57af4cd261a8916720368cf327437a3c302fa3f084e170621ff1c'),
  ('tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz)','847daac9543ecb677efd92f3b01f4a5804529cca4105c9ea80972a8c5af699cd'),
  ('tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text)','314c3aee644285e9117a97d006eac6dd345a74a4d2705e9e75b8c5d3f0d4ca0b'),
  ('tagekyc.enforce_raw_export_authority_snapshot_insert()','0797d3ee40b3f28137565262a0f6e755a3ed4f7401ee340535af6a60cda48133'),
  ('tagekyc.raw_export_retained_snapshot_is_current(uuid,timestamptz)','328a24ec9e8b9e8505b56b90741acfb6821009bfd31f097cdff015fd202fc7a5'),
  ('tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint)','a5bc6a7ac200a4ce40fbedeaa1d3663c9de0c0574da1305efad5680822544046'),
  ('tagekyc.raw_export_append_authority_snapshot(uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,uuid,integer,text,text,timestamptz,text,text,text,timestamptz,timestamptz)','da8e2dbac6555f2d7a00284728ebfb91785d8b22224b878561cc131e6c195767'),
  ('tagekyc.raw_export_withdraw_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)','6e4065aa9a614f8e0b1c41266b8467d013426670824f17068828bdd8b1cf17b6'),
  ('tagekyc.raw_export_revoke_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)','351aa273b10f0ecf7062507f9c6aa5f82f1369a90d5d65496521bc5ce678d03a'),
  ('tagekyc.raw_export_resolve_current_authority_for_source(uuid,uuid,uuid,text,timestamptz)','2c57c5eef8b121ea200edbb34fc3081d106cb052fd7c7b84e8ee1c3c49729cc9'),
  ('tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)','a3eb26e32906ad5383f0a9d6eb21af99b7b5b0a46e77c1a4812273ea272fc34a'),
  ('tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)','5649c358a968f24c0960f2aff291a458c9f9b73ee3e6b2a371d8ee5bc7923af1'),
  ('tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)','dbe80ef39afe8614b02ea6b7c2fa17c67b9d9ee66621aa43a9406e9d3ac46116'),
  ('tagekyc.complete_raw_export_source_ingress_claim(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)','4eefedc274d1d2c6152dd4100ed75f4ab4cdccdc997b04290da82bcc83a87b65'),
  ('tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint)','f761acfe2123d34bb1bde39aee31eb302ac720b3117ff8dc2f845bc11ae089ba'),
  ('tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz)','39bc73673df8801b8c09461636694140dc01d3108edf1c44530151cb053a7f10')
 ) AS bodies(signature,sha256) LOOP
  SELECT p.prosrc INTO actual FROM pg_catalog.pg_proc p WHERE p.oid=pg_catalog.to_regprocedure(expected.signature);
  IF NOT FOUND OR encode(tagekyc_extensions.digest(convert_to(replace(actual,chr(13)||chr(10),chr(10)),'UTF8'),'sha256'),'hex')<>expected.sha256 THEN
   RAISE EXCEPTION 'A3_CAPTURE_CURRENT_BODY_MISMATCH';
  END IF;
 END LOOP;
END $guard$;
""";

    private const string CaptureLineageRestore = """
DROP FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb);
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
CREATE OR REPLACE FUNCTION tagekyc.capture_runtime_bind_capability(
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
DROP TRIGGER tr_a3_capability_authority_lineage ON tagekyc.capture_capabilities;
DROP TRIGGER tr_a3_binding_authority_lineage ON tagekyc.capture_execution_bindings;
DROP FUNCTION tagekyc.enforce_a3_capture_authority_lineage();
ALTER TABLE tagekyc.capture_execution_bindings DROP COLUMN "ConsentBindingId",DROP COLUMN "RetentionAuthorityRevision",DROP COLUMN "RetentionAuthorityId",DROP COLUMN "PrincipalId",DROP COLUMN "AuthorityMode";
ALTER TABLE tagekyc.capture_capabilities DROP COLUMN "ConsentBindingId",DROP COLUMN "RetentionAuthorityRevision",DROP COLUMN "RetentionAuthorityId",DROP COLUMN "PrincipalId",DROP COLUMN "AuthorityMode";

""";

    private const string CapabilityBindOperation = """
CREATE OR REPLACE FUNCTION tagekyc.capture_runtime_bind_capability(
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
    v_saved_permit tagekyc.raw_source_retention_permits%ROWTYPE;
    v_requirement text; v_ref text;
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

    SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_credential_generation FOR UPDATE;
    SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=v_c."VerificationSessionId" AND "ClientApplicationId"=v_c."ClientApplicationId" FOR UPDATE;
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id FOR UPDATE;
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



    IF v_c."AuthorityMode"='SourceRetention' THEN
          SELECT * INTO v_saved_permit FROM tagekyc.raw_source_retention_permits
          WHERE "RetentionAuthorityId"=v_c."RetentionAuthorityId" AND "Revision"=v_c."RetentionAuthorityRevision";
          SELECT r."ExternalConsentArtifactRef" INTO v_ref FROM tagekyc.raw_source_consent_bindings b
          JOIN tagekyc.raw_source_consent_references r ON r."ConsentReferenceId"=b."ConsentReferenceId"
          WHERE b."ConsentBindingId"=v_c."ConsentBindingId";
          PERFORM pg_advisory_xact_lock_shared(hashtextextended('tip88c1:a3:consent-reference:'||v_c."ClientApplicationId"::text||':'||v_ref,0));
          PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
          PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||v_saved_permit."PolicyId"::text||':'||v_saved_permit."PolicyVersion"::text));
          FOR v_requirement IN SELECT r."RequirementType" FROM tagekyc.raw_export_policy_requirements r
          WHERE r."PolicyId"=v_saved_permit."PolicyId" AND r."PolicyVersion"=v_saved_permit."PolicyVersion"
           AND r."RequirementType"<>'ConsentArtifact' ORDER BY r."RequirementType" COLLATE "C"
          LOOP PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||v_saved_permit."PolicyId"::text||':'||v_saved_permit."PolicyVersion"::text||':'||v_requirement)); END LOOP;
    END IF;
    p_now:=clock_timestamp();
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
    IF v_c."AuthorityMode"='HistoricalNonRetained' OR
      (v_c."AuthorityMode"='SourceRetention' AND
       (NOT EXISTS (SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(v_c."RetentionAuthorityId",v_c."RetentionAuthorityRevision",v_c."PrincipalId",v_c."ClientApplicationId",v_c."VerificationSessionId",'ChipDg2Portrait',p_now))
        OR NOT EXISTS (SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(v_c."RetentionAuthorityId",v_c."RetentionAuthorityRevision",v_c."PrincipalId",v_c."ClientApplicationId",v_c."VerificationSessionId",'LiveSelfieImage',p_now)))) THEN
        RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
    END IF;
    v_binding_id:=pg_catalog.gen_random_uuid();
    v_horizon:=LEAST(v_c."ExpiresAtUtc",v_s."ExpiresAt",p_now+interval '30 minutes');
    INSERT INTO tagekyc.capture_execution_bindings VALUES(v_binding_id,v_c."VerificationSessionId",p_capture_capability_id,v_c."ClientApplicationId",p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,v_g."PublicKeyThumbprint",v_r."Revision",v_i."Revision",v_g."Revision",v_r."TrustProfileId",v_r."TrustProfileRevision",v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."ConfigurationId",v_r."ConfigurationRevision",v_c."Challenge",p_bind_operation_id,p_now,v_horizon,v_c."AuthorityMode",v_c."PrincipalId",v_c."RetentionAuthorityId",v_c."RetentionAuthorityRevision",v_c."ConsentBindingId");
    UPDATE tagekyc.capture_capabilities SET "State"='Bound',"Revision"="Revision"+1,"BoundAtUtc"=p_now WHERE "CaptureCapabilityId"=p_capture_capability_id;
    INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","RuntimeCaptureAgentId","RuntimeInstallationId","RuntimeCredentialId","RuntimeCredentialGeneration","ResultCode","ResultCapabilityId","ResultBindingId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_request_fingerprint,p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,'Applied',p_capture_capability_id,v_binding_id,v_c."Revision"+1,p_now,p_now);
    INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","RuntimeCaptureAgentId","RuntimeInstallationId","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_capture_capability_id,'Bound',p_capture_agent_id,p_device_installation_id,v_c."Revision",v_c."Revision"+1,p_now);
    RETURN QUERY SELECT 'CREATED',v_binding_id,v_horizon,v_r."Revision",v_i."Revision",v_g."Revision",v_c."Revision"+1;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) TO tagekyc_capture_runtime_application;
""";

    private const string CapabilityIssueOperation = """
DROP FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz);
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
    p_now timestamptz,
    p_principal_id uuid,p_consent_binding_id uuid,p_retention_profile jsonb)
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
    v_authority_mode text := 'NonRetained'; v_retention_id uuid; v_retention_revision bigint; v_consent_binding_id uuid;
    v_permit record; v_key text; v_requirement text; v_ref text;
    v_saved_permit tagekyc.raw_source_retention_permits%ROWTYPE;
BEGIN
    IF p_principal_id IS NULL OR p_principal_id='00000000-0000-0000-0000-000000000000'::uuid
       OR p_principal_id IS DISTINCT FROM tagekyc.raw_export_current_actor() THEN
       RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
    END IF;
    IF p_action IS NULL OR p_client_application_id IS NULL OR p_verification_session_id IS NULL
       OR p_idempotency_key IS NULL OR p_new_capability_id IS NULL OR p_key_lookup_prefix IS NULL
       OR p_secret_digest IS NULL OR p_verifier_pepper_version IS NULL OR p_request_fingerprint IS NULL
       OR p_now IS NULL OR NOT pg_catalog.isfinite(p_now)
       OR p_consent_binding_id='00000000-0000-0000-0000-000000000000'::uuid
       OR (p_action='Replace' AND (p_consent_binding_id IS NOT NULL OR p_retention_profile IS NOT NULL))
       OR (p_action='Issue' AND ((p_consent_binding_id IS NULL)<>(p_retention_profile IS NULL)))
       OR p_action NOT IN ('Issue','Replace')
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


    IF p_retention_profile IS NOT NULL THEN
      IF jsonb_typeof(p_retention_profile)<>'object' THEN
       RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
      END IF;
      IF (SELECT array_agg(k ORDER BY k COLLATE "C") FROM jsonb_object_keys(p_retention_profile) k)
       IS DISTINCT FROM ARRAY['ControllerIdentity','LegalHoldPolicyId','MaximumRetentionSeconds','PolicyId','PolicyVersion','PurgePolicyId','RawClasses','RetentionClass','RetentionPolicyId','RetentionPolicyVersion','RevocationPolicyId','StableDataScopeId']::text[]
       OR p_retention_profile->'RawClasses' IS DISTINCT FROM '["ChipDg2Portrait","LiveSelfieImage"]'::jsonb THEN
       RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
      END IF;
      FOREACH v_key IN ARRAY ARRAY['PolicyId','ControllerIdentity','StableDataScopeId','RetentionPolicyId','RetentionClass','RevocationPolicyId','PurgePolicyId','LegalHoldPolicyId'] LOOP
       IF jsonb_typeof(p_retention_profile->v_key)<>'string' OR p_retention_profile->>v_key ~ '^[[:space:]]*$'
        OR position(chr(10) IN p_retention_profile->>v_key)>0 OR position(chr(13) IN p_retention_profile->>v_key)>0
        OR octet_length(p_retention_profile->>v_key)>(CASE WHEN v_key='RetentionClass' THEN 64 ELSE 128 END) THEN
        RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
       END IF;
      END LOOP;
      FOREACH v_key IN ARRAY ARRAY['PolicyVersion','RetentionPolicyVersion','MaximumRetentionSeconds'] LOOP
       IF jsonb_typeof(p_retention_profile->v_key)<>'number' OR (p_retention_profile->>v_key)!~'^[1-9][0-9]*$' THEN
        RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
       END IF;
      END LOOP;
      BEGIN
       IF (p_retention_profile->>'PolicyId')::uuid='00000000-0000-0000-0000-000000000000'::uuid
        OR (p_retention_profile->>'PolicyVersion')::integer<1
        OR (p_retention_profile->>'RetentionPolicyVersion')::integer<1
        OR (p_retention_profile->>'MaximumRetentionSeconds')::integer NOT BETWEEN 1 AND 31536000 THEN
        RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
       END IF;
      EXCEPTION WHEN invalid_text_representation OR numeric_value_out_of_range THEN
       RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
      END;
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
    v_capability_lock_a:=pg_catalog.hashtextextended(COALESCE(p_current_capability_id,p_new_capability_id)::text,80);
    v_capability_lock_b:=pg_catalog.hashtextextended(p_new_capability_id::text,80);
    PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(v_capability_lock_a,v_capability_lock_b));
    IF v_capability_lock_a<>v_capability_lock_b THEN
      PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(v_capability_lock_a,v_capability_lock_b));
    END IF;

    SELECT * INTO v_session FROM tagekyc.verification_sessions
    WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id
    FOR UPDATE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;
    IF p_action='Replace' THEN
      SELECT * INTO v_current FROM tagekyc.capture_capabilities
      WHERE "CaptureCapabilityId"=p_current_capability_id AND "ClientApplicationId"=p_client_application_id
       AND "VerificationSessionId"=p_verification_session_id FOR UPDATE;
      IF NOT FOUND OR v_current."PrincipalId" IS DISTINCT FROM p_principal_id THEN
       RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
      END IF;
    END IF;
    p_now:=clock_timestamp(); v_expiry:=LEAST(p_now+interval '5 minutes',v_session."ExpiresAt");

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

        v_authority_mode:=v_current."AuthorityMode"; v_retention_id:=v_current."RetentionAuthorityId";
        v_retention_revision:=v_current."RetentionAuthorityRevision"; v_consent_binding_id:=v_current."ConsentBindingId";
        IF v_authority_mode='SourceRetention' THEN
          SELECT * INTO v_saved_permit FROM tagekyc.raw_source_retention_permits
          WHERE "RetentionAuthorityId"=v_retention_id AND "Revision"=v_retention_revision;
          SELECT r."ExternalConsentArtifactRef" INTO v_ref FROM tagekyc.raw_source_consent_bindings b
          JOIN tagekyc.raw_source_consent_references r ON r."ConsentReferenceId"=b."ConsentReferenceId"
          WHERE b."ConsentBindingId"=v_consent_binding_id;
          PERFORM pg_advisory_xact_lock_shared(hashtextextended('tip88c1:a3:consent-reference:'||p_client_application_id::text||':'||v_ref,0));
          PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
          PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||v_saved_permit."PolicyId"::text||':'||v_saved_permit."PolicyVersion"::text));
          FOR v_requirement IN SELECT r."RequirementType" FROM tagekyc.raw_export_policy_requirements r
          WHERE r."PolicyId"=v_saved_permit."PolicyId" AND r."PolicyVersion"=v_saved_permit."PolicyVersion"
           AND r."RequirementType"<>'ConsentArtifact' ORDER BY r."RequirementType" COLLATE "C"
          LOOP PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||v_saved_permit."PolicyId"::text||':'||v_saved_permit."PolicyVersion"::text||':'||v_requirement)); END LOOP;
          p_now:=clock_timestamp();
          IF v_current."ExpiresAtUtc"<=p_now THEN
        UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_current."CaptureCapabilityId";
        INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,p_request_fingerprint,'Expired',v_current."CaptureCapabilityId",v_current."Revision"+1,p_now,p_now);
        INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,v_current."CaptureCapabilityId",'Expired',v_current."Revision",v_current."Revision"+1,p_now);
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',v_current."CaptureCapabilityId",false,v_current."ExpiresAtUtc",'Expired',v_current."Revision"+1; RETURN;
          END IF;
          IF NOT EXISTS (SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(v_retention_id,v_retention_revision,p_principal_id,p_client_application_id,p_verification_session_id,'ChipDg2Portrait',p_now))
           OR NOT EXISTS (SELECT 1 FROM tagekyc.raw_source_resolve_retention_authority(v_retention_id,v_retention_revision,p_principal_id,p_client_application_id,p_verification_session_id,'LiveSelfieImage',p_now))
           OR v_session."ExpiresAt"<=p_now THEN
           RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
          END IF;
          v_expiry:=LEAST(p_now+interval '5 minutes',v_saved_permit."ExpiresAtUtc",v_session."ExpiresAt");
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


    IF p_action='Issue' AND p_consent_binding_id IS NOT NULL THEN
      SELECT * INTO v_permit FROM tagekyc.raw_source_issue_retention_authority(
       p_principal_id,p_client_application_id,p_verification_session_id,p_consent_binding_id,
       (p_retention_profile->>'PolicyId')::uuid,(p_retention_profile->>'PolicyVersion')::integer,
       ARRAY['ChipDg2Portrait','LiveSelfieImage']::text[],
       p_retention_profile->>'ControllerIdentity',p_retention_profile->>'StableDataScopeId',
       p_retention_profile->>'RetentionPolicyId',(p_retention_profile->>'RetentionPolicyVersion')::integer,
       p_retention_profile->>'RetentionClass',p_retention_profile->>'RevocationPolicyId',
       p_retention_profile->>'PurgePolicyId',p_retention_profile->>'LegalHoldPolicyId',
       (p_retention_profile->>'MaximumRetentionSeconds')::integer,p_idempotency_key,p_request_fingerprint);
      IF v_permit.result_code IS DISTINCT FROM 'Granted' THEN
       RETURN QUERY SELECT CASE WHEN v_permit.result_code='Conflict' THEN 'CONFLICT' ELSE 'ACCESS_DENIED' END,NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
      END IF;
      v_authority_mode:='SourceRetention'; v_retention_id:=v_permit.retention_authority_id;
      v_retention_revision:=v_permit.retention_authority_revision; v_consent_binding_id:=p_consent_binding_id;
      p_now:=clock_timestamp(); v_expiry:=LEAST(p_now+interval '5 minutes',v_permit.expires_at_utc,v_session."ExpiresAt");
      IF v_expiry<=p_now THEN
       -- Roll B back: a just-minted permit cannot survive failed issuance.
       RAISE EXCEPTION 'A3_RETENTION_ISSUANCE_HORIZON_EXHAUSTED';
      END IF;
    END IF;

    INSERT INTO tagekyc.capture_capabilities(
        "CaptureCapabilityId","VerificationSessionId","ClientApplicationId",
        "KeyLookupPrefix","SecretDigest","VerifierPepperVersion","Audience",
        "Challenge","IssuedAtUtc","ExpiresAtUtc","State","Revision",
        "PredecessorCapabilityId","AuthorityMode","PrincipalId","RetentionAuthorityId","RetentionAuthorityRevision","ConsentBindingId")
    VALUES (p_new_capability_id,p_verification_session_id,p_client_application_id,
        p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,
        'ManagedCaptureRuntime',v_session."BindingNonceHash",p_now,v_expiry,
        'ActiveUnbound',1,p_current_capability_id,v_authority_mode,p_principal_id,v_retention_id,v_retention_revision,v_consent_binding_id);

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

ALTER FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb) TO tagekyc_capture_runtime_application,tagekyc_runtime;
""";

    private const string CaptureLineageSchema = """
DO $guard$
BEGIN
 IF EXISTS (SELECT 1 FROM tagekyc.capture_capabilities WHERE "State" IN ('ActiveUnbound','Bound') AND "ExpiresAtUtc">clock_timestamp())
 OR EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings WHERE "ExecutionExpiresAtUtc">clock_timestamp()) THEN
  RAISE EXCEPTION 'A3_CAPTURE_LINEAGE_CUTOVER_ACTIVE';
 END IF;
END $guard$;
ALTER TABLE tagekyc.capture_capabilities
 ADD COLUMN "AuthorityMode" varchar(24) NOT NULL DEFAULT 'HistoricalNonRetained',
 ADD COLUMN "PrincipalId" uuid,
 ADD COLUMN "RetentionAuthorityId" uuid,
 ADD COLUMN "RetentionAuthorityRevision" bigint,
 ADD COLUMN "ConsentBindingId" uuid,
 ADD CONSTRAINT ck_a3_capability_authority_shape CHECK (
 ("PrincipalId" IS NULL OR "PrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND ("RetentionAuthorityId" IS NULL OR "RetentionAuthorityId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND ("RetentionAuthorityRevision" IS NULL OR "RetentionAuthorityRevision">0)
 AND ("ConsentBindingId" IS NULL OR "ConsentBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND (("AuthorityMode"='HistoricalNonRetained' AND "PrincipalId" IS NULL AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL)
 OR ("AuthorityMode"='NonRetained' AND "PrincipalId" IS NOT NULL AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL)
 OR ("AuthorityMode"='SourceRetention' AND "PrincipalId" IS NOT NULL AND "RetentionAuthorityId" IS NOT NULL AND "RetentionAuthorityRevision" IS NOT NULL AND "ConsentBindingId" IS NOT NULL))),
 ADD CONSTRAINT fk_a3_capability_retention_authority FOREIGN KEY ("RetentionAuthorityId","RetentionAuthorityRevision")
 REFERENCES tagekyc.raw_source_retention_permits("RetentionAuthorityId","Revision") ON DELETE RESTRICT,
 ADD CONSTRAINT fk_a3_capability_consent_binding FOREIGN KEY ("ConsentBindingId")
 REFERENCES tagekyc.raw_source_consent_bindings("ConsentBindingId") ON DELETE RESTRICT;
CREATE INDEX ix_a3_capability_retention_authority ON tagekyc.capture_capabilities("RetentionAuthorityId","RetentionAuthorityRevision");
CREATE INDEX ix_a3_capability_consent_binding ON tagekyc.capture_capabilities("ConsentBindingId");
ALTER TABLE tagekyc.capture_execution_bindings
 ADD COLUMN "AuthorityMode" varchar(24) NOT NULL DEFAULT 'HistoricalNonRetained',
 ADD COLUMN "PrincipalId" uuid,
 ADD COLUMN "RetentionAuthorityId" uuid,
 ADD COLUMN "RetentionAuthorityRevision" bigint,
 ADD COLUMN "ConsentBindingId" uuid,
 ADD CONSTRAINT ck_a3_binding_authority_shape CHECK (
 ("PrincipalId" IS NULL OR "PrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND ("RetentionAuthorityId" IS NULL OR "RetentionAuthorityId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND ("RetentionAuthorityRevision" IS NULL OR "RetentionAuthorityRevision">0)
 AND ("ConsentBindingId" IS NULL OR "ConsentBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
 AND (("AuthorityMode"='HistoricalNonRetained' AND "PrincipalId" IS NULL AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL)
 OR ("AuthorityMode"='NonRetained' AND "PrincipalId" IS NOT NULL AND "RetentionAuthorityId" IS NULL AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL)
 OR ("AuthorityMode"='SourceRetention' AND "PrincipalId" IS NOT NULL AND "RetentionAuthorityId" IS NOT NULL AND "RetentionAuthorityRevision" IS NOT NULL AND "ConsentBindingId" IS NOT NULL))),
 ADD CONSTRAINT fk_a3_binding_retention_authority FOREIGN KEY ("RetentionAuthorityId","RetentionAuthorityRevision")
 REFERENCES tagekyc.raw_source_retention_permits("RetentionAuthorityId","Revision") ON DELETE RESTRICT,
 ADD CONSTRAINT fk_a3_binding_consent_binding FOREIGN KEY ("ConsentBindingId")
 REFERENCES tagekyc.raw_source_consent_bindings("ConsentBindingId") ON DELETE RESTRICT;
CREATE INDEX ix_a3_binding_retention_authority ON tagekyc.capture_execution_bindings("RetentionAuthorityId","RetentionAuthorityRevision");
CREATE INDEX ix_a3_binding_consent_binding ON tagekyc.capture_execution_bindings("ConsentBindingId");
CREATE FUNCTION tagekyc.enforce_a3_capture_authority_lineage() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE previous tagekyc.capture_capabilities%ROWTYPE;
BEGIN
 IF TG_OP='UPDATE' THEN
  IF ROW(NEW."AuthorityMode",NEW."PrincipalId",NEW."RetentionAuthorityId",NEW."RetentionAuthorityRevision",NEW."ConsentBindingId")
   IS DISTINCT FROM ROW(OLD."AuthorityMode",OLD."PrincipalId",OLD."RetentionAuthorityId",OLD."RetentionAuthorityRevision",OLD."ConsentBindingId") THEN
   RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH';
  END IF;
  RETURN NEW;
 END IF;
 IF NEW."AuthorityMode"='HistoricalNonRetained' THEN RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH'; END IF;
 IF TG_TABLE_NAME='capture_capabilities' THEN
  IF NEW."PrincipalId" IS DISTINCT FROM NULLIF(pg_catalog.current_setting('tagekyc.actor_principal_id',true),'')::uuid THEN
   RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH';
  END IF;
  IF NEW."AuthorityMode"='SourceRetention' AND NOT EXISTS (
   SELECT 1 FROM tagekyc.raw_source_retention_permits p JOIN tagekyc.raw_source_consent_bindings b ON b."ConsentBindingId"=p."ConsentBindingId"
   WHERE p."RetentionAuthorityId"=NEW."RetentionAuthorityId" AND p."Revision"=NEW."RetentionAuthorityRevision"
   AND p."ConsentBindingId"=NEW."ConsentBindingId" AND p."PrincipalId"=NEW."PrincipalId"
   AND p."ClientApplicationId"=NEW."ClientApplicationId" AND p."VerificationSessionId"=NEW."VerificationSessionId"
   AND ROW(b."PrincipalId",b."ClientApplicationId",b."VerificationSessionId")
     =ROW(NEW."PrincipalId",NEW."ClientApplicationId",NEW."VerificationSessionId")) THEN
   RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH';
  END IF;
  IF NEW."PredecessorCapabilityId" IS NOT NULL THEN
   SELECT * INTO previous FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=NEW."PredecessorCapabilityId";
   IF NOT FOUND OR ROW(previous."AuthorityMode",previous."PrincipalId",previous."RetentionAuthorityId",previous."RetentionAuthorityRevision",previous."ConsentBindingId")
    IS DISTINCT FROM ROW(NEW."AuthorityMode",NEW."PrincipalId",NEW."RetentionAuthorityId",NEW."RetentionAuthorityRevision",NEW."ConsentBindingId") THEN
    RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH';
   END IF;
  END IF;
 ELSE
  SELECT * INTO previous FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=NEW."CaptureCapabilityId";
  IF NOT FOUND OR ROW(previous."AuthorityMode",previous."PrincipalId",previous."RetentionAuthorityId",previous."RetentionAuthorityRevision",previous."ConsentBindingId")
   IS DISTINCT FROM ROW(NEW."AuthorityMode",NEW."PrincipalId",NEW."RetentionAuthorityId",NEW."RetentionAuthorityRevision",NEW."ConsentBindingId") THEN
   RAISE EXCEPTION 'A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH';
  END IF;
 END IF;
 RETURN NEW;
END $body$;
ALTER FUNCTION tagekyc.enforce_a3_capture_authority_lineage() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.enforce_a3_capture_authority_lineage() FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
CREATE TRIGGER tr_a3_capability_authority_lineage BEFORE INSERT OR UPDATE ON tagekyc.capture_capabilities FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_a3_capture_authority_lineage();
CREATE TRIGGER tr_a3_binding_authority_lineage BEFORE INSERT OR UPDATE ON tagekyc.capture_execution_bindings FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_a3_capture_authority_lineage();
""";

    private const string ConsentGuards = """

CREATE FUNCTION tagekyc.raw_source_enforce_authority_write()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $body$
DECLARE context text := pg_catalog.current_setting('tagekyc.a3_authority_write',true);
BEGIN
    IF current_user <> 'tagekyc_raw_export_deployer' OR context IS NULL
      OR TG_OP='DELETE'
      OR NOT (
        (TG_TABLE_NAME='raw_source_consent_references' AND
          ((TG_OP='INSERT' AND context='E01-R') OR
           (TG_OP='UPDATE' AND context IN ('E01-R','E01-W','B2-Withdrawal'))))
        OR (TG_TABLE_NAME='raw_source_consent_reference_events' AND TG_OP='INSERT'
          AND context IN ('E01-R','E01-W','B2-Withdrawal'))
        OR (TG_TABLE_NAME='raw_source_consent_bindings' AND TG_OP='INSERT' AND context='E01-R')
        OR (TG_TABLE_NAME IN ('raw_source_retention_permits','raw_source_retention_permit_classes')
          AND TG_OP='INSERT' AND context='R20-Issue')
      ) THEN RAISE EXCEPTION 'A3_AUTHORITY_WRITE_FORBIDDEN'; END IF;
    IF TG_OP='UPDATE' THEN
        IF (pg_catalog.to_jsonb(NEW)-'CurrentRevision') IS DISTINCT FROM
           (pg_catalog.to_jsonb(OLD)-'CurrentRevision')
           OR NEW."CurrentRevision"<>OLD."CurrentRevision"+1 THEN
            RAISE EXCEPTION 'A3_AUTHORITY_HEAD_IMMUTABLE';
        END IF;
    END IF;
    IF TG_TABLE_NAME='raw_source_consent_reference_events' THEN
        IF NEW."OperationDomain" IS DISTINCT FROM context OR
           NEW."RecordedByPrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor() THEN
            RAISE EXCEPTION 'A3_AUTHORITY_EVENT_ACTOR_MISMATCH';
        END IF;
    ELSIF TG_TABLE_NAME IN ('raw_source_consent_bindings','raw_source_retention_permits') THEN
        IF NEW."PrincipalId" IS DISTINCT FROM tagekyc.raw_export_current_actor() THEN
            RAISE EXCEPTION 'A3_AUTHORITY_ACTOR_MISMATCH';
        END IF;
    END IF;
    RETURN NEW;
END $body$;
ALTER FUNCTION tagekyc.raw_source_enforce_authority_write() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_enforce_authority_write() FROM PUBLIC;

CREATE FUNCTION tagekyc.raw_source_require_authority_graph()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
    reference_id uuid; authority_id uuid; authority_revision bigint;
    h tagekyc.raw_source_consent_references%ROWTYPE;
    e tagekyc.raw_source_consent_reference_events%ROWTYPE;
    prev tagekyc.raw_source_consent_reference_events%ROWTYPE;
    p tagekyc.raw_source_retention_permits%ROWTYPE;
    event_count bigint; event_max bigint;
BEGIN
    IF TG_TABLE_NAME IN ('raw_source_consent_references','raw_source_consent_reference_events') THEN
        reference_id:=NEW."ConsentReferenceId";
        SELECT * INTO h FROM tagekyc.raw_source_consent_references r WHERE r."ConsentReferenceId"=reference_id;
        SELECT count(*),max(r."Revision") INTO event_count,event_max
          FROM tagekyc.raw_source_consent_reference_events r WHERE r."ConsentReferenceId"=reference_id;
        IF h."ConsentReferenceId" IS NULL OR event_max IS DISTINCT FROM h."CurrentRevision"
          OR event_count IS DISTINCT FROM event_max THEN
            RAISE EXCEPTION 'A3_AUTHORITY_GRAPH_INCOMPLETE';
        END IF;
        IF TG_TABLE_NAME='raw_source_consent_reference_events' THEN
            e:=NEW;
            IF e."Revision">1 THEN
                SELECT * INTO prev FROM tagekyc.raw_source_consent_reference_events r
                  WHERE r."ConsentReferenceId"=reference_id AND r."Revision"=e."Revision"-1;
                IF prev."Revision" IS NULL OR prev."EventType"='Withdrawn' THEN
                    RAISE EXCEPTION 'A3_AUTHORITY_REVISION_INVALID';
                END IF;
                IF e."EventType"='Withdrawn' AND
                  ROW(e."SourceVersion",e."ConsentTextVersion",e."ConsentTextContentHash",e."ValidFromUtc",e."ValidUntilUtc")
                  IS DISTINCT FROM ROW(prev."SourceVersion",prev."ConsentTextVersion",prev."ConsentTextContentHash",prev."ValidFromUtc",prev."ValidUntilUtc") THEN
                    RAISE EXCEPTION 'A3_AUTHORITY_WITHDRAWAL_PROVENANCE_MISMATCH';
                END IF;
                IF e."EventType"='Updated' AND EXISTS (
                  SELECT 1 FROM tagekyc.raw_source_consent_reference_events r
                  WHERE r."ConsentReferenceId"=reference_id AND r."Revision"<e."Revision"
                  AND r."EventType" IN ('Recorded','Updated') AND r."SourceVersion"=e."SourceVersion") THEN
                    RAISE EXCEPTION 'A3_AUTHORITY_SOURCE_VERSION_REUSED';
                END IF;
            END IF;
            IF e."EventType" IN ('Recorded','Updated') AND NOT EXISTS (
              SELECT 1 FROM tagekyc.raw_source_consent_bindings b
              WHERE b."ConsentReferenceId"=reference_id AND b."ConsentReferenceRevision"=e."Revision"
                AND b."PrincipalId"=e."RecordedByPrincipalId" AND b."IdempotencyKey"=e."IdempotencyKey"
                AND b."RequestFingerprint"=e."RequestFingerprint" AND b."RecordedAtUtc"=e."RecordedAtUtc") THEN
                RAISE EXCEPTION 'A3_AUTHORITY_RECORD_BINDING_MISSING';
            END IF;
        END IF;
    ELSIF TG_TABLE_NAME='raw_source_consent_bindings' THEN
        IF NOT EXISTS (
          SELECT 1 FROM tagekyc.raw_source_consent_references r
          JOIN tagekyc.raw_source_consent_reference_events bound_event
            ON bound_event."ConsentReferenceId"=r."ConsentReferenceId" AND bound_event."Revision"=NEW."ConsentReferenceRevision"
          JOIN tagekyc.verification_sessions s ON s."Id"=NEW."VerificationSessionId"
          WHERE r."ConsentReferenceId"=NEW."ConsentReferenceId" AND r."ClientApplicationId"=NEW."ClientApplicationId"
            AND r."SubjectRef"=NEW."SubjectRef" AND s."ClientApplicationId"=NEW."ClientApplicationId"
            AND s."SubjectRef"=NEW."SubjectRef" AND bound_event."EventType" IN ('Recorded','Updated')) THEN
            RAISE EXCEPTION 'A3_CONSENT_BINDING_LINEAGE_MISMATCH';
        END IF;
    ELSIF TG_TABLE_NAME IN ('raw_source_retention_permits','raw_source_retention_permit_classes') THEN
        authority_id:=NEW."RetentionAuthorityId"; authority_revision:=NEW."Revision";
        SELECT * INTO p FROM tagekyc.raw_source_retention_permits x
          WHERE x."RetentionAuthorityId"=authority_id AND x."Revision"=authority_revision;
        IF p."RetentionAuthorityId" IS NULL OR NOT EXISTS (
          SELECT 1 FROM tagekyc.raw_source_consent_bindings b WHERE b."ConsentBindingId"=p."ConsentBindingId"
          AND ROW(b."PrincipalId",b."ClientApplicationId",b."VerificationSessionId")
            =ROW(p."PrincipalId",p."ClientApplicationId",p."VerificationSessionId")) THEN
            RAISE EXCEPTION 'A3_RETENTION_PERMIT_LINEAGE_MISMATCH';
        END IF;
        IF (SELECT array_agg(c."RawClass"::text ORDER BY c."RawClass") FROM tagekyc.raw_source_retention_permit_classes c
          WHERE c."RetentionAuthorityId"=authority_id AND c."Revision"=authority_revision)
          IS DISTINCT FROM ARRAY['ChipDg2Portrait','LiveSelfieImage']::text[]
          OR EXISTS (SELECT 1 FROM tagekyc.raw_source_retention_permit_classes c
            WHERE c."RetentionAuthorityId"=authority_id AND c."Revision"=authority_revision AND NOT EXISTS (
              SELECT 1 FROM tagekyc.raw_export_policy_allowed_classes allowed
              WHERE allowed."PolicyId"=p."PolicyId" AND allowed."PolicyVersion"=p."PolicyVersion"
                AND allowed."RawClass"=c."RawClass")) THEN
            RAISE EXCEPTION 'A3_RETENTION_PERMIT_CLASSES_INVALID';
        END IF;
    END IF;
    RETURN NULL;
END $body$;
ALTER FUNCTION tagekyc.raw_source_require_authority_graph() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_require_authority_graph() FROM PUBLIC;
CREATE TRIGGER tr_a3_reference_write BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_source_consent_references
FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_enforce_authority_write();
CREATE CONSTRAINT TRIGGER tr_a3_reference_graph AFTER INSERT OR UPDATE ON tagekyc.raw_source_consent_references
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_require_authority_graph();
CREATE TRIGGER tr_a3_event_write BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_source_consent_reference_events
FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_enforce_authority_write();
CREATE CONSTRAINT TRIGGER tr_a3_event_graph AFTER INSERT OR UPDATE ON tagekyc.raw_source_consent_reference_events
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_require_authority_graph();
CREATE TRIGGER tr_a3_binding_write BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_source_consent_bindings
FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_enforce_authority_write();
CREATE CONSTRAINT TRIGGER tr_a3_binding_graph AFTER INSERT OR UPDATE ON tagekyc.raw_source_consent_bindings
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_require_authority_graph();
CREATE TRIGGER tr_a3_permit_write BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_source_retention_permits
FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_enforce_authority_write();
CREATE CONSTRAINT TRIGGER tr_a3_permit_graph AFTER INSERT OR UPDATE ON tagekyc.raw_source_retention_permits
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_require_authority_graph();
CREATE TRIGGER tr_a3_class_write BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_source_retention_permit_classes
FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_enforce_authority_write();
CREATE CONSTRAINT TRIGGER tr_a3_class_graph AFTER INSERT OR UPDATE ON tagekyc.raw_source_retention_permit_classes
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_source_require_authority_graph();

""";

    private const string ConsentOperations = """

CREATE FUNCTION tagekyc.raw_source_record_consent_reference(
 p_principal_id uuid,p_client_id uuid,p_session_id uuid,p_external_ref text,
 p_source_version text,p_expected_revision bigint,p_text_version text,
 p_text_hash text,p_valid_from timestamptz,p_valid_until timestamptz,
 p_idempotency_key uuid,p_request_fingerprint bytea)
RETURNS TABLE(result_code text,consent_reference_id uuid,consent_reference_revision bigint,consent_binding_id uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
 s tagekyc.verification_sessions%ROWTYPE;
 h tagekyc.raw_source_consent_references%ROWTYPE;
 e tagekyc.raw_source_consent_reference_events%ROWTYPE;
 replay tagekyc.raw_source_consent_bindings%ROWTYPE;
 new_ref uuid; new_revision bigint; new_binding uuid;
 admission_now timestamptz; has_admission_authority boolean; prior_context text;
BEGIN
 IF p_principal_id IS NULL OR p_client_id IS NULL OR p_session_id IS NULL OR p_idempotency_key IS NULL
 OR '00000000-0000-0000-0000-000000000000'::uuid=ANY(ARRAY[p_principal_id,p_client_id,p_session_id,p_idempotency_key])
 OR p_principal_id IS DISTINCT FROM tagekyc.raw_export_current_actor()
 OR p_request_fingerprint IS NULL OR octet_length(p_request_fingerprint)<>32
 OR p_expected_revision IS NULL OR p_expected_revision<0
 OR p_external_ref IS NULL OR p_external_ref ~ '^[[:space:]]*$' OR octet_length(p_external_ref) NOT BETWEEN 1 AND 512 OR position(chr(13) IN p_external_ref)>0 OR position(chr(10) IN p_external_ref)>0 OR p_source_version IS NULL OR p_source_version ~ '^[[:space:]]*$' OR octet_length(p_source_version) NOT BETWEEN 1 AND 128 OR position(chr(13) IN p_source_version)>0 OR position(chr(10) IN p_source_version)>0
 OR p_text_version IS NULL OR p_text_version ~ '^[[:space:]]*$' OR octet_length(p_text_version) NOT BETWEEN 1 AND 128 OR position(chr(13) IN p_text_version)>0 OR position(chr(10) IN p_text_version)>0 OR p_text_hash IS NULL OR p_text_hash ~ '^[[:space:]]*$' OR octet_length(p_text_hash) NOT BETWEEN 1 AND 256 OR position(chr(13) IN p_text_hash)>0 OR position(chr(10) IN p_text_hash)>0
 OR p_valid_from IS NULL OR p_valid_until IS NULL OR NOT isfinite(p_valid_from) OR NOT isfinite(p_valid_until)
 OR p_valid_from>=p_valid_until THEN
  RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
 END IF;
 SELECT * INTO s FROM tagekyc.verification_sessions x
 WHERE x."Id"=p_session_id AND x."ClientApplicationId"=p_client_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b2:subject-consent-authority:'||p_principal_id::text||':'||p_client_id::text||':SubjectConsentRecorder'));
 PERFORM pg_advisory_xact_lock(hashtextextended('tip88c1:a3:consent-operation:'||p_principal_id::text||':E01-R:'||p_idempotency_key::text,0));
 PERFORM pg_advisory_xact_lock(hashtextextended('tip88c1:a3:consent-reference:'||p_client_id::text||':'||p_external_ref,0));
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x
 WHERE x."ClientApplicationId"=p_client_id AND x."ExternalConsentArtifactRef"=p_external_ref FOR UPDATE;
 IF h."ConsentReferenceId" IS NOT NULL THEN
  SELECT * INTO e FROM tagekyc.raw_source_consent_reference_events x
   WHERE x."ConsentReferenceId"=h."ConsentReferenceId" AND x."Revision"=h."CurrentRevision" FOR UPDATE;
 END IF;
 SELECT * INTO replay FROM tagekyc.raw_source_consent_bindings b
 WHERE b."PrincipalId"=p_principal_id AND b."IdempotencyKey"=p_idempotency_key;
 IF FOUND THEN
  IF replay."RequestFingerprint"=p_request_fingerprint AND replay."ClientApplicationId"=p_client_id
    AND replay."VerificationSessionId"=p_session_id AND replay."ConsentReferenceId"=h."ConsentReferenceId"
    AND replay."SubjectRef"=s."SubjectRef" THEN
   RETURN QUERY SELECT 'Replay'::text,replay."ConsentReferenceId",replay."ConsentReferenceRevision",replay."ConsentBindingId";
  ELSE RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::uuid; END IF;
  RETURN;
 END IF;
 admission_now:=clock_timestamp();
 WITH latest AS (
 SELECT a."EventType",a."ValidFromUtc",a."ValidUntilUtc"
 FROM tagekyc.raw_export_subject_consent_authorities a
 WHERE a."AuthorityPrincipalId"=p_principal_id AND a."ClientApplicationId"=p_client_id
 AND a."AuthorityType"='SubjectConsentRecorder' ORDER BY a."Revision" DESC LIMIT 1)
SELECT EXISTS(SELECT 1 FROM latest WHERE "EventType"='Granted' AND "ValidFromUtc"<=admission_now
 AND ("ValidUntilUtc" IS NULL OR admission_now<"ValidUntilUtc")) INTO has_admission_authority;
 IF NOT has_admission_authority OR s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal')
 OR s."ExpiresAt"<=admission_now OR p_valid_from>admission_now OR p_valid_until<=admission_now
 OR (h."ConsentReferenceId" IS NOT NULL AND h."SubjectRef" IS DISTINCT FROM s."SubjectRef") THEN
  RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
 END IF;
 IF h."ConsentReferenceId" IS NULL THEN
  IF p_expected_revision<>0 THEN RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN; END IF;
  new_ref:=gen_random_uuid(); new_revision:=1;
 ELSE
  IF h."CurrentRevision"<>p_expected_revision OR e."EventType"='Withdrawn' THEN
   RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
  END IF;
  new_ref:=h."ConsentReferenceId"; new_revision:=h."CurrentRevision";
  IF e."SourceVersion"=p_source_version THEN
   IF ROW(e."ConsentTextVersion",e."ConsentTextContentHash",e."ValidFromUtc",e."ValidUntilUtc")
     IS DISTINCT FROM ROW(p_text_version,p_text_hash,p_valid_from,p_valid_until) THEN
    RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
   END IF;
   IF e."ValidFromUtc">admission_now OR e."ValidUntilUtc"<=admission_now THEN
    RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
   END IF;
  ELSE
   IF EXISTS(SELECT 1 FROM tagekyc.raw_source_consent_reference_events x
     WHERE x."ConsentReferenceId"=new_ref AND x."SourceVersion"=p_source_version
       AND x."EventType" IN ('Recorded','Updated')) OR new_revision=9223372036854775807 THEN
    RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::uuid; RETURN;
   END IF;
   new_revision:=new_revision+1;
  END IF;
 END IF;
 prior_context:=current_setting('tagekyc.a3_authority_write',true);
 PERFORM set_config('tagekyc.a3_authority_write','E01-R',true);
 IF h."ConsentReferenceId" IS NULL THEN
  INSERT INTO tagekyc.raw_source_consent_references VALUES(new_ref,p_client_id,s."SubjectRef",p_external_ref,1);
 ELSIF new_revision<>h."CurrentRevision" THEN
  UPDATE tagekyc.raw_source_consent_references SET "CurrentRevision"=new_revision WHERE "ConsentReferenceId"=new_ref;
 END IF;
 IF h."ConsentReferenceId" IS NULL OR new_revision<>h."CurrentRevision" THEN
  INSERT INTO tagekyc.raw_source_consent_reference_events VALUES
   (new_ref,new_revision,CASE WHEN new_revision=1 THEN 'Recorded' ELSE 'Updated' END,
    p_source_version,p_text_version,p_text_hash,p_valid_from,p_valid_until,p_principal_id,admission_now,
    'E01-R',p_idempotency_key,p_request_fingerprint,NULL);
 END IF;
 new_binding:=gen_random_uuid();
 INSERT INTO tagekyc.raw_source_consent_bindings VALUES
  (new_binding,new_ref,new_revision,p_principal_id,p_client_id,p_session_id,s."SubjectRef",
   p_idempotency_key,p_request_fingerprint,admission_now);
 PERFORM set_config('tagekyc.a3_authority_write',coalesce(prior_context,''),true);
 RETURN QUERY SELECT 'Bound'::text,new_ref,new_revision,new_binding;
END $body$;
ALTER FUNCTION tagekyc.raw_source_record_consent_reference(uuid,uuid,uuid,text,text,bigint,text,text,timestamptz,timestamptz,uuid,bytea) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_record_consent_reference(uuid,uuid,uuid,text,text,bigint,text,text,timestamptz,timestamptz,uuid,bytea) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_source_record_consent_reference(uuid,uuid,uuid,text,text,bigint,text,text,timestamptz,timestamptz,uuid,bytea) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.raw_source_withdraw_consent_reference(
 p_principal_id uuid,p_client_id uuid,p_reference_id uuid,p_expected_revision bigint,
 p_source_version text,p_decision_ref text,p_idempotency_key uuid,p_request_fingerprint bytea)
RETURNS TABLE(result_code text,consent_reference_id uuid,consent_reference_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
 h tagekyc.raw_source_consent_references%ROWTYPE;
 e tagekyc.raw_source_consent_reference_events%ROWTYPE;
 replay tagekyc.raw_source_consent_reference_events%ROWTYPE;
 admission_now timestamptz; has_admission_authority boolean; prior_context text;
BEGIN
 IF p_principal_id IS NULL OR p_client_id IS NULL OR p_reference_id IS NULL OR p_idempotency_key IS NULL
 OR '00000000-0000-0000-0000-000000000000'::uuid=ANY(ARRAY[p_principal_id,p_client_id,p_reference_id,p_idempotency_key])
 OR p_principal_id IS DISTINCT FROM tagekyc.raw_export_current_actor()
 OR p_request_fingerprint IS NULL OR octet_length(p_request_fingerprint)<>32
 OR p_expected_revision IS NULL OR p_expected_revision<=0
 OR p_source_version IS NULL OR p_source_version ~ '^[[:space:]]*$' OR octet_length(p_source_version) NOT BETWEEN 1 AND 128 OR position(chr(13) IN p_source_version)>0 OR position(chr(10) IN p_source_version)>0 OR p_decision_ref IS NULL OR p_decision_ref ~ '^[[:space:]]*$' OR octet_length(p_decision_ref) NOT BETWEEN 1 AND 256 OR position(chr(13) IN p_decision_ref)>0 OR position(chr(10) IN p_decision_ref)>0 THEN
  RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint; RETURN;
 END IF;
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x
 WHERE x."ConsentReferenceId"=p_reference_id AND x."ClientApplicationId"=p_client_id;
 IF NOT FOUND THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint; RETURN; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b2:subject-consent-authority:'||p_principal_id::text||':'||p_client_id::text||':SubjectConsentWithdrawer'));
 PERFORM pg_advisory_xact_lock(hashtextextended('tip88c1:a3:consent-operation:'||p_principal_id::text||':E01-W:'||p_idempotency_key::text,0));
 PERFORM pg_advisory_xact_lock(hashtextextended('tip88c1:a3:consent-reference:'||p_client_id::text||':'||h."ExternalConsentArtifactRef",0));
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x
 WHERE x."ConsentReferenceId"=p_reference_id AND x."ClientApplicationId"=p_client_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint; RETURN; END IF;
 SELECT * INTO e FROM tagekyc.raw_source_consent_reference_events x
 WHERE x."ConsentReferenceId"=p_reference_id AND x."Revision"=h."CurrentRevision" FOR UPDATE;
 SELECT * INTO replay FROM tagekyc.raw_source_consent_reference_events x
 WHERE x."RecordedByPrincipalId"=p_principal_id AND x."OperationDomain"='E01-W' AND x."IdempotencyKey"=p_idempotency_key;
 IF FOUND THEN
  IF replay."RequestFingerprint"=p_request_fingerprint AND replay."ConsentReferenceId"=p_reference_id THEN
   RETURN QUERY SELECT 'Replay'::text,replay."ConsentReferenceId",replay."Revision";
  ELSE RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint; END IF;
  RETURN;
 END IF;
 admission_now:=clock_timestamp();
 WITH latest AS (
 SELECT a."EventType",a."ValidFromUtc",a."ValidUntilUtc"
 FROM tagekyc.raw_export_subject_consent_authorities a
 WHERE a."AuthorityPrincipalId"=p_principal_id AND a."ClientApplicationId"=p_client_id
 AND a."AuthorityType"='SubjectConsentWithdrawer' ORDER BY a."Revision" DESC LIMIT 1)
SELECT EXISTS(SELECT 1 FROM latest WHERE "EventType"='Granted' AND "ValidFromUtc"<=admission_now
 AND ("ValidUntilUtc" IS NULL OR admission_now<"ValidUntilUtc")) INTO has_admission_authority;
 IF NOT has_admission_authority THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint; RETURN; END IF;
 IF h."CurrentRevision"<>p_expected_revision OR e."SourceVersion"<>p_source_version
 OR e."EventType"='Withdrawn' OR h."CurrentRevision"=9223372036854775807 THEN
  RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint; RETURN;
 END IF;
 prior_context:=current_setting('tagekyc.a3_authority_write',true);
 PERFORM set_config('tagekyc.a3_authority_write','E01-W',true);
 INSERT INTO tagekyc.raw_source_consent_reference_events VALUES
  (p_reference_id,h."CurrentRevision"+1,'Withdrawn',e."SourceVersion",e."ConsentTextVersion",e."ConsentTextContentHash",
   e."ValidFromUtc",e."ValidUntilUtc",p_principal_id,admission_now,'E01-W',p_idempotency_key,p_request_fingerprint,p_decision_ref);
 UPDATE tagekyc.raw_source_consent_references SET "CurrentRevision"=h."CurrentRevision"+1 WHERE "ConsentReferenceId"=p_reference_id;
 PERFORM set_config('tagekyc.a3_authority_write',coalesce(prior_context,''),true);
 RETURN QUERY SELECT 'Withdrawn'::text,p_reference_id,h."CurrentRevision"+1;
END $body$;
ALTER FUNCTION tagekyc.raw_source_withdraw_consent_reference(uuid,uuid,uuid,bigint,text,text,uuid,bytea) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_withdraw_consent_reference(uuid,uuid,uuid,bigint,text,text,uuid,bytea) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.raw_source_withdraw_consent_reference(uuid,uuid,uuid,bigint,text,text,uuid,bytea) TO tagekyc_capture_runtime_application;

""";

    private const string RetentionOperations = """

CREATE FUNCTION tagekyc.raw_source_issue_retention_authority(
 p_principal_id uuid,p_client_id uuid,p_session_id uuid,p_consent_binding_id uuid,
 p_policy_id uuid,p_policy_version integer,p_raw_classes text[],p_controller_identity text,
 p_stable_scope_id text,p_retention_policy_id text,p_retention_policy_version integer,
 p_retention_class text,p_revocation_policy_id text,p_purge_policy_id text,p_legal_hold_policy_id text,
 p_maximum_retention_seconds integer,p_issue_operation_id uuid,p_request_fingerprint bytea)
RETURNS TABLE(result_code text,retention_authority_id uuid,retention_authority_revision bigint,expires_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
 s tagekyc.verification_sessions%ROWTYPE; b tagekyc.raw_source_consent_bindings%ROWTYPE;
 h tagekyc.raw_source_consent_references%ROWTYPE; e tagekyc.raw_source_consent_reference_events%ROWTYPE;
 old_permit tagekyc.raw_source_retention_permits%ROWTYPE;
 admission_now timestamptz; expiry timestamptz; authority_id uuid; requirement text;
 has_admission_authority boolean; prior_context text;
BEGIN
 IF p_principal_id IS NULL OR p_client_id IS NULL OR p_session_id IS NULL OR p_consent_binding_id IS NULL
 OR p_policy_id IS NULL OR p_issue_operation_id IS NULL
 OR '00000000-0000-0000-0000-000000000000'::uuid=ANY(ARRAY[p_principal_id,p_client_id,p_session_id,p_consent_binding_id,p_policy_id,p_issue_operation_id])
 OR p_principal_id IS DISTINCT FROM tagekyc.raw_export_current_actor()
 OR p_request_fingerprint IS NULL OR octet_length(p_request_fingerprint)<>32
 OR p_raw_classes IS DISTINCT FROM ARRAY['ChipDg2Portrait','LiveSelfieImage']::text[]
 OR p_policy_version IS NULL OR p_policy_version<1 OR p_retention_policy_version IS NULL OR p_retention_policy_version<1
 OR p_maximum_retention_seconds IS NULL OR p_maximum_retention_seconds NOT BETWEEN 1 AND 31536000 THEN
  RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; RETURN;
 END IF;
 SELECT * INTO s FROM tagekyc.verification_sessions x WHERE x."Id"=p_session_id AND x."ClientApplicationId"=p_client_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 SELECT * INTO b FROM tagekyc.raw_source_consent_bindings x WHERE x."ConsentBindingId"=p_consent_binding_id
 AND x."PrincipalId"=p_principal_id AND x."ClientApplicationId"=p_client_id AND x."VerificationSessionId"=p_session_id;
 IF NOT FOUND OR b."SubjectRef"<>s."SubjectRef" THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b2:subject-consent-authority:'||p_principal_id::text||':'||p_client_id::text||':SubjectConsentRecorder'));
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x WHERE x."ConsentReferenceId"=b."ConsentReferenceId" AND x."ClientApplicationId"=p_client_id;
 IF NOT FOUND THEN RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtextextended('tip88c1:a3:consent-reference:'||p_client_id::text||':'||h."ExternalConsentArtifactRef",0));
 SELECT * INTO b FROM tagekyc.raw_source_consent_bindings x WHERE x."ConsentBindingId"=p_consent_binding_id FOR SHARE;
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x WHERE x."ConsentReferenceId"=b."ConsentReferenceId" FOR SHARE;
 SELECT * INTO e FROM tagekyc.raw_source_consent_reference_events x WHERE x."ConsentReferenceId"=h."ConsentReferenceId" AND x."Revision"=h."CurrentRevision" FOR SHARE;
 SELECT * INTO old_permit FROM tagekyc.raw_source_retention_permits p WHERE p."PrincipalId"=p_principal_id AND p."IssueOperationId"=p_issue_operation_id;
 IF FOUND THEN
  IF old_permit."RequestFingerprint"=p_request_fingerprint AND old_permit."ConsentBindingId"=p_consent_binding_id
    AND old_permit."ClientApplicationId"=p_client_id AND old_permit."VerificationSessionId"=p_session_id THEN
   RETURN QUERY SELECT 'Replay'::text,old_permit."RetentionAuthorityId",old_permit."Revision",old_permit."ExpiresAtUtc";
  ELSE RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; END IF; RETURN;
 END IF;
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||p_policy_id::text||':'||p_policy_version::text));
 FOR requirement IN SELECT r."RequirementType" FROM tagekyc.raw_export_policy_requirements r
 WHERE r."PolicyId"=p_policy_id AND r."PolicyVersion"=p_policy_version AND r."RequirementType"<>'ConsentArtifact' ORDER BY r."RequirementType" COLLATE "C"
 LOOP PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||p_policy_id::text||':'||p_policy_version::text||':'||requirement)); END LOOP;
 admission_now:=clock_timestamp();
 WITH latest AS(SELECT a."EventType",a."ValidFromUtc",a."ValidUntilUtc" FROM tagekyc.raw_export_subject_consent_authorities a
 WHERE a."AuthorityPrincipalId"=p_principal_id AND a."ClientApplicationId"=p_client_id AND a."AuthorityType"='SubjectConsentRecorder'
 ORDER BY a."Revision" DESC LIMIT 1)
 SELECT EXISTS(SELECT 1 FROM latest WHERE "EventType"='Granted' AND "ValidFromUtc"<=admission_now
 AND ("ValidUntilUtc" IS NULL OR admission_now<"ValidUntilUtc")) INTO has_admission_authority;
 IF NOT has_admission_authority OR s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal')
 OR s."ExpiresAt"<=admission_now OR h."CurrentRevision"<>b."ConsentReferenceRevision"
 OR h."SubjectRef"<>s."SubjectRef" OR e."EventType" NOT IN ('Recorded','Updated')
 OR e."ValidFromUtc">admission_now OR e."ValidUntilUtc"<=admission_now
 OR NOT EXISTS(
 SELECT 1 FROM tagekyc.raw_export_policy_versions p
 JOIN tagekyc.raw_export_policy_closures c ON c."PolicyId"=p."PolicyId" AND c."PolicyVersion"=p."PolicyVersion"
 WHERE p."PolicyId"=p_policy_id AND p."PolicyVersion"=p_policy_version
 AND p."Mode"='EncryptedRawVaultRetained' AND c."ClosureType"='CatalogApproved'
 AND p."RequirementRuleSetVersion"=(SELECT max(r."RuleSetVersion") FROM tagekyc.raw_export_requirement_rule_sets r WHERE r."RuleSetId"=p."RequirementRuleSetId")
 AND (SELECT l."EventType" FROM tagekyc.raw_export_policy_lifecycle l WHERE l."PolicyId"=p."PolicyId" AND l."PolicyVersion"=p."PolicyVersion" ORDER BY l."Revision" DESC LIMIT 1)='Activated'
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_policy_requirements req
 LEFT JOIN LATERAL(SELECT f."EventType",f."ValidFromUtc",f."ValidUntilUtc"
 FROM tagekyc.raw_export_fulfillments f WHERE f."PolicyId"=req."PolicyId" AND f."PolicyVersion"=req."PolicyVersion"
 AND f."RequirementType"=req."RequirementType" ORDER BY f."Revision" DESC LIMIT 1) latest ON true
 WHERE req."PolicyId"=p."PolicyId" AND req."PolicyVersion"=p."PolicyVersion" AND req."RequirementType"<>'ConsentArtifact'
 AND (latest."EventType" IS DISTINCT FROM 'Accepted' OR latest."ValidFromUtc" IS NULL OR latest."ValidFromUtc">admission_now
 OR (latest."ValidUntilUtc" IS NOT NULL AND latest."ValidUntilUtc"<=admission_now))))
 OR EXISTS(SELECT 1 FROM unnest(p_raw_classes) raw_class WHERE NOT EXISTS(
 SELECT 1 FROM tagekyc.raw_export_policy_allowed_classes a WHERE a."PolicyId"=p_policy_id
 AND a."PolicyVersion"=p_policy_version AND a."RawClass"=raw_class)) THEN
  RETURN QUERY SELECT 'Denied'::text,NULL::uuid,NULL::bigint,NULL::timestamptz; RETURN;
 END IF;
 expiry:=LEAST(e."ValidUntilUtc",s."ExpiresAt",admission_now+make_interval(secs=>p_maximum_retention_seconds));
 authority_id:=gen_random_uuid(); prior_context:=current_setting('tagekyc.a3_authority_write',true);
 PERFORM set_config('tagekyc.a3_authority_write','R20-Issue',true);
 INSERT INTO tagekyc.raw_source_retention_permits VALUES
 (authority_id,1,p_consent_binding_id,p_principal_id,p_client_id,p_session_id,p_policy_id,p_policy_version,
 'SourceRetention',admission_now,expiry,p_controller_identity,p_stable_scope_id,p_retention_policy_id,
 p_retention_policy_version,p_retention_class,'ServerCustodyAccepted',p_revocation_policy_id,p_purge_policy_id,
 p_legal_hold_policy_id,p_maximum_retention_seconds,p_issue_operation_id,p_request_fingerprint);
 INSERT INTO tagekyc.raw_source_retention_permit_classes VALUES(authority_id,1,'ChipDg2Portrait'),(authority_id,1,'LiveSelfieImage');
 PERFORM set_config('tagekyc.a3_authority_write',coalesce(prior_context,''),true);
 RETURN QUERY SELECT 'Granted'::text,authority_id,1::bigint,expiry;
END $body$;
ALTER FUNCTION tagekyc.raw_source_issue_retention_authority(uuid,uuid,uuid,uuid,uuid,integer,text[],text,text,text,integer,text,text,text,text,integer,uuid,bytea) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_issue_retention_authority(uuid,uuid,uuid,uuid,uuid,integer,text[],text,text,text,integer,text,text,text,text,integer,uuid,bytea) FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

CREATE FUNCTION tagekyc.raw_source_resolve_retention_authority(
 p_authority_id uuid,p_authority_revision bigint,p_principal_id uuid,p_client_id uuid,
 p_session_id uuid,p_raw_class text,p_now timestamptz)
RETURNS TABLE(is_effective boolean,consent_binding_id uuid,consent_reference_id uuid,
 consent_reference_revision bigint,policy_id uuid,policy_version integer,expires_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $body$
DECLARE
 p tagekyc.raw_source_retention_permits%ROWTYPE; b tagekyc.raw_source_consent_bindings%ROWTYPE;
 h tagekyc.raw_source_consent_references%ROWTYPE; requirement text;
BEGIN
 IF p_now IS NULL OR NOT isfinite(p_now) THEN RETURN; END IF;
 SELECT * INTO p FROM tagekyc.raw_source_retention_permits x
 WHERE x."RetentionAuthorityId"=p_authority_id AND x."Revision"=p_authority_revision
 AND x."PrincipalId"=p_principal_id AND x."ClientApplicationId"=p_client_id AND x."VerificationSessionId"=p_session_id;
 IF NOT FOUND THEN RETURN; END IF;
 SELECT * INTO b FROM tagekyc.raw_source_consent_bindings x WHERE x."ConsentBindingId"=p."ConsentBindingId"
 AND x."PrincipalId"=p_principal_id AND x."ClientApplicationId"=p_client_id AND x."VerificationSessionId"=p_session_id;
 IF NOT FOUND THEN RETURN; END IF;
 SELECT * INTO h FROM tagekyc.raw_source_consent_references x WHERE x."ConsentReferenceId"=b."ConsentReferenceId" AND x."ClientApplicationId"=p_client_id;
 IF NOT FOUND THEN RETURN; END IF;
 -- Caller already holds CP04's actual session-row and policy prefix before its shared clock.
 PERFORM pg_advisory_xact_lock_shared(hashtextextended('tip88c1:a3:consent-reference:'||p_client_id::text||':'||h."ExternalConsentArtifactRef",0));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:raw_export_requirement_rule_set_publish'));
 PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:lifecycle:'||p."PolicyId"::text||':'||p."PolicyVersion"::text));
 FOR requirement IN SELECT r."RequirementType" FROM tagekyc.raw_export_policy_requirements r
 WHERE r."PolicyId"=p."PolicyId" AND r."PolicyVersion"=p."PolicyVersion" AND r."RequirementType"<>'ConsentArtifact' ORDER BY r."RequirementType" COLLATE "C"
 LOOP PERFORM pg_advisory_xact_lock_shared(hashtext('tip88b1:fulfillment:'||p."PolicyId"::text||':'||p."PolicyVersion"::text||':'||requirement)); END LOOP;
 RETURN QUERY SELECT true,b."ConsentBindingId",b."ConsentReferenceId",b."ConsentReferenceRevision",p."PolicyId",p."PolicyVersion",p."ExpiresAtUtc"
 FROM tagekyc.raw_source_consent_references r
 JOIN tagekyc.raw_source_consent_reference_events e ON e."ConsentReferenceId"=r."ConsentReferenceId" AND e."Revision"=r."CurrentRevision"
 JOIN tagekyc.verification_sessions s ON s."Id"=p_session_id
 WHERE r."ConsentReferenceId"=b."ConsentReferenceId" AND r."CurrentRevision"=b."ConsentReferenceRevision"
 AND r."ClientApplicationId"=p_client_id AND s."ClientApplicationId"=p_client_id
 AND r."SubjectRef"=b."SubjectRef" AND s."SubjectRef"=b."SubjectRef"
 AND e."EventType" IN ('Recorded','Updated') AND e."ValidFromUtc"<=p_now AND p_now<e."ValidUntilUtc"
 AND p."IssuedAtUtc"<=p_now AND p_now<p."ExpiresAtUtc"
 AND EXISTS(SELECT 1 FROM tagekyc.raw_source_retention_permit_classes c WHERE c."RetentionAuthorityId"=p_authority_id AND c."Revision"=p_authority_revision AND c."RawClass"=p_raw_class)
 AND EXISTS(SELECT 1 FROM tagekyc.raw_export_policy_allowed_classes c WHERE c."PolicyId"=p."PolicyId" AND c."PolicyVersion"=p."PolicyVersion" AND c."RawClass"=p_raw_class)
 AND EXISTS(
 SELECT 1 FROM tagekyc.raw_export_policy_versions pol
 JOIN tagekyc.raw_export_policy_closures c ON c."PolicyId"=pol."PolicyId" AND c."PolicyVersion"=pol."PolicyVersion"
 WHERE pol."PolicyId"=p."PolicyId" AND pol."PolicyVersion"=p."PolicyVersion"
 AND pol."Mode"='EncryptedRawVaultRetained' AND c."ClosureType"='CatalogApproved'
 AND pol."RequirementRuleSetVersion"=(SELECT max(r."RuleSetVersion") FROM tagekyc.raw_export_requirement_rule_sets r WHERE r."RuleSetId"=pol."RequirementRuleSetId")
 AND (SELECT l."EventType" FROM tagekyc.raw_export_policy_lifecycle l WHERE l."PolicyId"=pol."PolicyId" AND l."PolicyVersion"=pol."PolicyVersion" ORDER BY l."Revision" DESC LIMIT 1)='Activated'
 AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_policy_requirements req
 LEFT JOIN LATERAL(SELECT f."EventType",f."ValidFromUtc",f."ValidUntilUtc"
 FROM tagekyc.raw_export_fulfillments f WHERE f."PolicyId"=req."PolicyId" AND f."PolicyVersion"=req."PolicyVersion"
 AND f."RequirementType"=req."RequirementType" ORDER BY f."Revision" DESC LIMIT 1) latest ON true
 WHERE req."PolicyId"=pol."PolicyId" AND req."PolicyVersion"=pol."PolicyVersion" AND req."RequirementType"<>'ConsentArtifact'
 AND (latest."EventType" IS DISTINCT FROM 'Accepted' OR latest."ValidFromUtc" IS NULL OR latest."ValidFromUtc">p_now
 OR (latest."ValidUntilUtc" IS NOT NULL AND latest."ValidUntilUtc"<=p_now))));
END $body$;
ALTER FUNCTION tagekyc.raw_source_resolve_retention_authority(uuid,bigint,uuid,uuid,uuid,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.raw_source_resolve_retention_authority(uuid,bigint,uuid,uuid,uuid,text,timestamptz) FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

""";

    private const string ConsentSchema = """
CREATE TABLE tagekyc.raw_source_consent_references (
"ConsentReferenceId" uuid NOT NULL,
"ClientApplicationId" uuid NOT NULL,
"SubjectRef" text NOT NULL,
"ExternalConsentArtifactRef" varchar(512) NOT NULL,
"CurrentRevision" bigint NOT NULL,
CONSTRAINT "PK_a3_consent_reference" PRIMARY KEY ("ConsentReferenceId"),
CONSTRAINT "UQ_a3_consent_reference_client_ref" UNIQUE ("ClientApplicationId","ExternalConsentArtifactRef"),
CONSTRAINT "UQ_a3_consent_reference_id_client" UNIQUE ("ConsentReferenceId","ClientApplicationId"),
CONSTRAINT "CK_a3_consent_reference_values" CHECK (
"CurrentRevision">=1
AND ("ConsentReferenceId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("ClientApplicationId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("SubjectRef" !~ '^[[:space:]]*$')
AND ("ExternalConsentArtifactRef" !~ '^[[:space:]]*$' AND position(chr(13) IN "ExternalConsentArtifactRef")=0 AND position(chr(10) IN "ExternalConsentArtifactRef")=0 AND pg_catalog.octet_length("ExternalConsentArtifactRef") BETWEEN 1 AND 512)));
ALTER TABLE tagekyc.raw_source_consent_references OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON TABLE tagekyc.raw_source_consent_references FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

CREATE TABLE tagekyc.raw_source_consent_reference_events (
"ConsentReferenceId" uuid NOT NULL,
"Revision" bigint NOT NULL,
"EventType" varchar(16) NOT NULL,
"SourceVersion" varchar(128) NOT NULL,
"ConsentTextVersion" varchar(128) NOT NULL,
"ConsentTextContentHash" varchar(256) NOT NULL,
"ValidFromUtc" timestamptz NOT NULL,
"ValidUntilUtc" timestamptz NOT NULL,
"RecordedByPrincipalId" uuid NOT NULL,
"RecordedAtUtc" timestamptz NOT NULL,
"OperationDomain" varchar(16) NOT NULL,
"IdempotencyKey" uuid NOT NULL,
"RequestFingerprint" bytea NOT NULL,
"DecisionRef" varchar(256),
CONSTRAINT "PK_a3_consent_reference_event" PRIMARY KEY ("ConsentReferenceId","Revision"),
CONSTRAINT "FK_a3_consent_reference_event_head" FOREIGN KEY ("ConsentReferenceId") REFERENCES tagekyc.raw_source_consent_references("ConsentReferenceId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
CONSTRAINT "UQ_a3_consent_reference_event_operation" UNIQUE ("RecordedByPrincipalId","OperationDomain","IdempotencyKey"),
CONSTRAINT "CK_a3_consent_reference_event_shape" CHECK (
"Revision">=1 AND "EventType" IN ('Recorded','Updated','Withdrawn')
AND "OperationDomain" IN ('E01-R','E01-W','B2-Withdrawal')
AND ("EventType"='Recorded')=("Revision"=1)
AND (("EventType" IN ('Recorded','Updated') AND "OperationDomain"='E01-R' AND "DecisionRef" IS NULL)
 OR ("EventType"='Withdrawn' AND "OperationDomain" IN ('E01-W','B2-Withdrawal')
 AND ("OperationDomain"='B2-Withdrawal' OR "DecisionRef" IS NOT NULL)))
AND "ValidFromUtc"<"ValidUntilUtc"
AND ("ConsentReferenceId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("EventType" !~ '^[[:space:]]*$' AND position(chr(13) IN "EventType")=0 AND position(chr(10) IN "EventType")=0 AND pg_catalog.octet_length("EventType") BETWEEN 1 AND 16)
AND ("SourceVersion" !~ '^[[:space:]]*$' AND position(chr(13) IN "SourceVersion")=0 AND position(chr(10) IN "SourceVersion")=0 AND pg_catalog.octet_length("SourceVersion") BETWEEN 1 AND 128)
AND ("ConsentTextVersion" !~ '^[[:space:]]*$' AND position(chr(13) IN "ConsentTextVersion")=0 AND position(chr(10) IN "ConsentTextVersion")=0 AND pg_catalog.octet_length("ConsentTextVersion") BETWEEN 1 AND 128)
AND ("ConsentTextContentHash" !~ '^[[:space:]]*$' AND position(chr(13) IN "ConsentTextContentHash")=0 AND position(chr(10) IN "ConsentTextContentHash")=0 AND pg_catalog.octet_length("ConsentTextContentHash") BETWEEN 1 AND 256)
AND (pg_catalog.isfinite("ValidFromUtc"))
AND (pg_catalog.isfinite("ValidUntilUtc"))
AND ("RecordedByPrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND (pg_catalog.isfinite("RecordedAtUtc"))
AND ("OperationDomain" !~ '^[[:space:]]*$' AND position(chr(13) IN "OperationDomain")=0 AND position(chr(10) IN "OperationDomain")=0 AND pg_catalog.octet_length("OperationDomain") BETWEEN 1 AND 16)
AND ("IdempotencyKey"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND (pg_catalog.octet_length("RequestFingerprint")=32)
AND ("DecisionRef" IS NULL
 OR ("OperationDomain"='B2-Withdrawal' AND pg_catalog.btrim("DecisionRef")<>'')
 OR ("OperationDomain"='E01-W' AND "DecisionRef" !~ '^[[:space:]]*$' AND position(chr(13) IN "DecisionRef")=0 AND position(chr(10) IN "DecisionRef")=0 AND pg_catalog.octet_length("DecisionRef") BETWEEN 1 AND 256))));
ALTER TABLE tagekyc.raw_source_consent_reference_events OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON TABLE tagekyc.raw_source_consent_reference_events FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

CREATE TABLE tagekyc.raw_source_consent_bindings (
"ConsentBindingId" uuid NOT NULL,
"ConsentReferenceId" uuid NOT NULL,
"ConsentReferenceRevision" bigint NOT NULL,
"PrincipalId" uuid NOT NULL,
"ClientApplicationId" uuid NOT NULL,
"VerificationSessionId" uuid NOT NULL,
"SubjectRef" text NOT NULL,
"IdempotencyKey" uuid NOT NULL,
"RequestFingerprint" bytea NOT NULL,
"RecordedAtUtc" timestamptz NOT NULL,
CONSTRAINT "PK_a3_consent_binding" PRIMARY KEY ("ConsentBindingId"),
CONSTRAINT "UQ_a3_consent_binding_operation" UNIQUE ("PrincipalId","IdempotencyKey"),
CONSTRAINT "FK_a3_consent_binding_event" FOREIGN KEY ("ConsentReferenceId","ConsentReferenceRevision") REFERENCES tagekyc.raw_source_consent_reference_events("ConsentReferenceId","Revision") ON DELETE RESTRICT,
CONSTRAINT "FK_a3_consent_binding_client" FOREIGN KEY ("ConsentReferenceId","ClientApplicationId") REFERENCES tagekyc.raw_source_consent_references("ConsentReferenceId","ClientApplicationId") ON DELETE RESTRICT,
CONSTRAINT "FK_a3_consent_binding_session" FOREIGN KEY ("VerificationSessionId") REFERENCES tagekyc.verification_sessions("Id") ON DELETE RESTRICT,
CONSTRAINT "CK_a3_consent_binding_values" CHECK (
"ConsentReferenceRevision">=1
AND ("ConsentBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("ConsentReferenceId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("PrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("ClientApplicationId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("VerificationSessionId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("SubjectRef" !~ '^[[:space:]]*$')
AND ("IdempotencyKey"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND (pg_catalog.octet_length("RequestFingerprint")=32)
AND (pg_catalog.isfinite("RecordedAtUtc"))));
ALTER TABLE tagekyc.raw_source_consent_bindings OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON TABLE tagekyc.raw_source_consent_bindings FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

CREATE TABLE tagekyc.raw_source_retention_permits (
"RetentionAuthorityId" uuid NOT NULL,
"Revision" bigint NOT NULL,
"ConsentBindingId" uuid NOT NULL,
"PrincipalId" uuid NOT NULL,
"ClientApplicationId" uuid NOT NULL,
"VerificationSessionId" uuid NOT NULL,
"PolicyId" uuid NOT NULL,
"PolicyVersion" integer NOT NULL,
"Purpose" varchar(32) NOT NULL,
"IssuedAtUtc" timestamptz NOT NULL,
"ExpiresAtUtc" timestamptz NOT NULL,
"ControllerIdentity" varchar(128) NOT NULL,
"StableDataScopeId" varchar(128) NOT NULL,
"RetentionPolicyId" varchar(128) NOT NULL,
"RetentionPolicyVersion" integer NOT NULL,
"RetentionClass" varchar(64) NOT NULL,
"RetentionStartEvent" varchar(32) NOT NULL,
"RevocationPolicyId" varchar(128) NOT NULL,
"PurgePolicyId" varchar(128) NOT NULL,
"LegalHoldPolicyId" varchar(128) NOT NULL,
"MaximumRetentionSeconds" integer NOT NULL,
"IssueOperationId" uuid NOT NULL,
"RequestFingerprint" bytea NOT NULL,
CONSTRAINT "PK_a3_retention_permit" PRIMARY KEY ("RetentionAuthorityId","Revision"),
CONSTRAINT "UQ_a3_retention_permit_id" UNIQUE ("RetentionAuthorityId"),
CONSTRAINT "UQ_a3_retention_permit_operation" UNIQUE ("PrincipalId","IssueOperationId"),
CONSTRAINT "FK_a3_retention_permit_binding" FOREIGN KEY ("ConsentBindingId") REFERENCES tagekyc.raw_source_consent_bindings("ConsentBindingId") ON DELETE RESTRICT,
CONSTRAINT "FK_a3_retention_permit_policy" FOREIGN KEY ("PolicyId","PolicyVersion") REFERENCES tagekyc.raw_export_policy_versions("PolicyId","PolicyVersion") ON DELETE RESTRICT,
CONSTRAINT "CK_a3_retention_permit_values" CHECK (
"Revision"=1 AND "PolicyVersion">=1 AND "RetentionPolicyVersion">=1
AND "Purpose"='SourceRetention' AND "RetentionStartEvent"='ServerCustodyAccepted'
AND "MaximumRetentionSeconds" BETWEEN 1 AND 31536000 AND "IssuedAtUtc"<"ExpiresAtUtc"
AND ("RetentionAuthorityId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("ConsentBindingId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("PrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("ClientApplicationId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("VerificationSessionId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("PolicyId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("Purpose" !~ '^[[:space:]]*$' AND position(chr(13) IN "Purpose")=0 AND position(chr(10) IN "Purpose")=0 AND pg_catalog.octet_length("Purpose") BETWEEN 1 AND 32)
AND (pg_catalog.isfinite("IssuedAtUtc"))
AND (pg_catalog.isfinite("ExpiresAtUtc"))
AND ("ControllerIdentity" !~ '^[[:space:]]*$' AND position(chr(13) IN "ControllerIdentity")=0 AND position(chr(10) IN "ControllerIdentity")=0 AND pg_catalog.octet_length("ControllerIdentity") BETWEEN 1 AND 128)
AND ("StableDataScopeId" !~ '^[[:space:]]*$' AND position(chr(13) IN "StableDataScopeId")=0 AND position(chr(10) IN "StableDataScopeId")=0 AND pg_catalog.octet_length("StableDataScopeId") BETWEEN 1 AND 128)
AND ("RetentionPolicyId" !~ '^[[:space:]]*$' AND position(chr(13) IN "RetentionPolicyId")=0 AND position(chr(10) IN "RetentionPolicyId")=0 AND pg_catalog.octet_length("RetentionPolicyId") BETWEEN 1 AND 128)
AND ("RetentionClass" !~ '^[[:space:]]*$' AND position(chr(13) IN "RetentionClass")=0 AND position(chr(10) IN "RetentionClass")=0 AND pg_catalog.octet_length("RetentionClass") BETWEEN 1 AND 64)
AND ("RetentionStartEvent" !~ '^[[:space:]]*$' AND position(chr(13) IN "RetentionStartEvent")=0 AND position(chr(10) IN "RetentionStartEvent")=0 AND pg_catalog.octet_length("RetentionStartEvent") BETWEEN 1 AND 32)
AND ("RevocationPolicyId" !~ '^[[:space:]]*$' AND position(chr(13) IN "RevocationPolicyId")=0 AND position(chr(10) IN "RevocationPolicyId")=0 AND pg_catalog.octet_length("RevocationPolicyId") BETWEEN 1 AND 128)
AND ("PurgePolicyId" !~ '^[[:space:]]*$' AND position(chr(13) IN "PurgePolicyId")=0 AND position(chr(10) IN "PurgePolicyId")=0 AND pg_catalog.octet_length("PurgePolicyId") BETWEEN 1 AND 128)
AND ("LegalHoldPolicyId" !~ '^[[:space:]]*$' AND position(chr(13) IN "LegalHoldPolicyId")=0 AND position(chr(10) IN "LegalHoldPolicyId")=0 AND pg_catalog.octet_length("LegalHoldPolicyId") BETWEEN 1 AND 128)
AND ("IssueOperationId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND (pg_catalog.octet_length("RequestFingerprint")=32)));
ALTER TABLE tagekyc.raw_source_retention_permits OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON TABLE tagekyc.raw_source_retention_permits FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

CREATE TABLE tagekyc.raw_source_retention_permit_classes (
"RetentionAuthorityId" uuid NOT NULL,
"Revision" bigint NOT NULL,
"RawClass" varchar(32) NOT NULL,
CONSTRAINT "PK_a3_retention_permit_class" PRIMARY KEY ("RetentionAuthorityId","Revision","RawClass"),
CONSTRAINT "FK_a3_retention_permit_class_permit" FOREIGN KEY ("RetentionAuthorityId","Revision") REFERENCES tagekyc.raw_source_retention_permits("RetentionAuthorityId","Revision") ON DELETE RESTRICT,
CONSTRAINT "CK_a3_retention_permit_class" CHECK (
"Revision"=1 AND "RawClass" IN ('ChipDg2Portrait','LiveSelfieImage')
AND ("RetentionAuthorityId"<>'00000000-0000-0000-0000-000000000000'::uuid)
AND ("RawClass" !~ '^[[:space:]]*$' AND position(chr(13) IN "RawClass")=0 AND position(chr(10) IN "RawClass")=0 AND pg_catalog.octet_length("RawClass") BETWEEN 1 AND 32)));
ALTER TABLE tagekyc.raw_source_retention_permit_classes OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON TABLE tagekyc.raw_source_retention_permit_classes FROM PUBLIC, tagekyc_runtime, tagekyc_capture_runtime_application, tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_operator, tagekyc_raw_export_claim_broker;

ALTER TABLE tagekyc.raw_source_consent_references ADD CONSTRAINT "FK_a3_consent_reference_current"
FOREIGN KEY ("ConsentReferenceId","CurrentRevision") REFERENCES tagekyc.raw_source_consent_reference_events("ConsentReferenceId","Revision")
DEFERRABLE INITIALLY DEFERRED;
CREATE INDEX "IX_a3_consent_binding_session_principal" ON tagekyc.raw_source_consent_bindings("VerificationSessionId","PrincipalId");
CREATE INDEX "IX_a3_retention_permit_binding" ON tagekyc.raw_source_retention_permits("ConsentBindingId");
""";
}
