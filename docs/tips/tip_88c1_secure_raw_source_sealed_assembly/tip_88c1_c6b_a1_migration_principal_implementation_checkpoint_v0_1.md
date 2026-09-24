# C6B-A1 — implementation checkpoint: migration principal / R27 authority stop

Date: 2026-09-11. This is a NEW evidence artifact, not an RRI, ratification or completion claim.

## 1. Status and exact authority

- SPECIFIED: Homeowner attachment 64d96076-e7ec-4a2f-9fa8-5a644b2c4b1e ratifies migration principal v0.3, then Stage 2 under typed gateway v0.9; ordinary bugs are fix/retest, genuine authority contradiction is STOP.
- MEASURED: v0.3 SHA `94EB6D2004449F122B045A2C91A90ADD428CCA020329D710389433C2A6464B8F`.
- MEASURED: v0.9 SHA `92EBC14DF4CF7574F92BB9F06A7A3195281EDC6C4A8A12EA09AD0B2F5B08C273`.
- MEASURED: Operation Master SHA remains `172B1C9550C2DDB750CD251E4D2A2E272DE24998B581D23A0654BF76C37314CE`.
- MEASURED: HEAD `c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9`.
- MEASURED: bounded Stage-1 PostgreSQL tests 9/9 passed, including all eight predecessor methods and one new mutation method. This does not mean full A1 or all catalogue reconciliation gates passed.
- MEASURED: Stage-2 product mutation count in this turn = 0.
- SPECIFIED: Stop reason is the contradictory R27 signed-metadata/CRT1 requirements in §8 below, not a compile/test failure.

## 2. DONE — changed implementation bytes

The preimage census was captured before edits; full SHA comparison after edits found exactly these three changed source files, zero deleted paths and no unrelated change.

| File | Pre SHA-256 | Post SHA-256 |
| --- | --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.cs` | `90215C0EE6F0FA6B26DD83F7C81A9CEAEC316868DEC59C2F2804CA4DA056F524` | `8B24F6E52C54570AD09E481F2BEBD12341C0B5DCCBE761434F6F691A4B1C0E82` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AclTests.cs` | `EF1D68121E666F8BF6DCAF7C077681F0450675B8D4CAAB9AC85C6F7D27ACB429` | `328AC34E5CBE617690962D9EA1343B938C11CC4B2E03C8E4CA727B091E8E614D` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | `5A965159A4D9ED405E68B9C7A2AF34B5AC3766CFC89DEEC5D43B3045BF28BCF8` | `46EB964237707793A5CE54F2B34F10DBD657EB874711476904D9E27583FBFE8B` |

DONE: Foundation migration replaces the executor-name check with durable-owner attributes; creates/validates exactly three capability roles; transfers 27 table owners; keeps 47 function owners; Down revokes dependencies before dropping only those three roles without CASCADE. Designer/model bytes unchanged.

DONE: Persistence/ACL proofs use the primary nonparallel fixture. The normal Down/reapply path and an injected post-Down failure both run; finally attempts restoration and latest migration plus full catalogue equality are asserted afterwards. Shared fixture source was not modified.

DONE: genuinely wrong test expectations were corrected, not relaxed:
- Before: constraint census used `capture_runtime_%` while asserting `CK_capability_event_type` on `capture_capabilities`. After: full 27-table manifest.
- Before: `Assert.Contains("ResultCode", functionBody)` for positional INSERT bodies. After: execute R01/R02, verify durable Applied/fingerprints/revisions, frozen replay tuples, changed-fingerprint Conflict, unchanged first secret digest, one matching event per operation; deferred constraints run before rollback.
- Before: display-only SQL used literal brace delimiters consumed by EF format parsing. After: display brackets; unchanged assertions and projection keys.
- DONE: supplementary `pg_get_function_identity_arguments` is output only, never comparison identity. Full definitions remain in the shape comparison. The two landed SECURITY INVOKER helpers remain distinct from 45 SECURITY DEFINER functions.

## 3. MEASURED — persistence projections

Command: `dotnet test tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj --filter FullyQualifiedName~Tip88C1C6BA1 --logger "trx;LogFileName=a1-stage1-principal-04.trx" --results-directory TestResults/a1-principal-stage1`.

Evidence: `TestResults/a1-principal-stage1/a1-stage1-principal-04.trx`, output of `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes`. Both owner and grant comparisons use only `format('%I.%I(%s)',nspname,proname,oidvectortypes(proargtypes))`. Each expected full identity is matched, and extra overloads with an A1 name are included then rejected by set equality.

The following is copied from PostgreSQL test output, not a declaration of expected values. All 27 tables and 47 functions are enumerated. Empty EXECUTE arrays mean no non-owner grantee; 35 nonempty / 12 empty.

```text
TABLE OWNERS 27/27
capture_capabilities|tagekyc_raw_export_deployer
capture_capability_events|tagekyc_raw_export_deployer
capture_capability_operations|tagekyc_raw_export_deployer
capture_execution_bindings|tagekyc_raw_export_deployer
capture_runtime_bootstrap_issuances|tagekyc_raw_export_deployer
capture_runtime_bootstrap_redemption_events|tagekyc_raw_export_deployer
capture_runtime_bootstrap_redemption_operations|tagekyc_raw_export_deployer
capture_runtime_configuration_heads|tagekyc_raw_export_deployer
capture_runtime_configuration_overrides|tagekyc_raw_export_deployer
capture_runtime_configuration_revisions|tagekyc_raw_export_deployer
capture_runtime_credential_generations|tagekyc_raw_export_deployer
capture_runtime_cutover_state|tagekyc_raw_export_deployer
capture_runtime_installations|tagekyc_raw_export_deployer
capture_runtime_management_events|tagekyc_raw_export_deployer
capture_runtime_management_operations|tagekyc_raw_export_deployer
capture_runtime_registrations|tagekyc_raw_export_deployer
capture_runtime_request_nonces|tagekyc_raw_export_deployer
capture_runtime_role_policy_heads|tagekyc_raw_export_deployer
capture_runtime_role_policy_revisions|tagekyc_raw_export_deployer
capture_runtime_rotation_authorizations|tagekyc_raw_export_deployer
capture_runtime_rotation_completion_events|tagekyc_raw_export_deployer
capture_runtime_rotation_completion_operations|tagekyc_raw_export_deployer
capture_runtime_trust_profile_heads|tagekyc_raw_export_deployer
capture_runtime_trust_profile_revisions|tagekyc_raw_export_deployer
platform_operator_credentials|tagekyc_raw_export_deployer
platform_operator_root_events|tagekyc_raw_export_deployer
platform_operator_root_operations|tagekyc_raw_export_deployer
FUNCTION OWNERS + EXECUTE 47/47
tagekyc.c6ba_assert_current_operator(uuid, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_reject_row_mutation()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_require_capability_event()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_require_management_event()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_require_redemption_event()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_require_root_event()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_require_rotation_completion_event()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_roles_are_canonical(text[])|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_root_provision_platform_credential(uuid, uuid, timestamp with time zone, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.c6ba_root_revoke_platform_credential(uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.c6ba_transition_runtime_lifecycle(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, text, text, text, boolean)|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_validate_capability_graph()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_validate_configuration_override()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.c6ba_validate_current_generation()|tagekyc_raw_export_deployer|EXECUTE=[]
tagekyc.capture_runtime_assign_configuration(uuid, uuid, uuid, uuid, bigint, bigint, uuid, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_assign_role_policy(uuid, uuid, uuid, uuid, bigint, bigint, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_authorize_rotation(uuid, uuid, uuid, uuid, uuid, bigint, bigint, timestamp with time zone, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_bind_capability(uuid, uuid, uuid, bigint, uuid, boolean, uuid, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_cancel_session_with_capability(uuid, uuid, text, text, text, uuid, bytea, timestamp with time zone, text, uuid)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application,tagekyc_runtime]
tagekyc.capture_runtime_claim_nonce(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_authenticator]
tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_authenticator]
tagekyc.capture_runtime_cleanup_nonces(timestamp with time zone, integer)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_authenticator]
tagekyc.capture_runtime_complete_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_issue_bootstrap(uuid, uuid, text, uuid, bigint, uuid, bigint, uuid, bigint, timestamp with time zone, bytea, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_issue_or_replace_capability(uuid, uuid, text, uuid, bigint, uuid, uuid, text, bytea, integer, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application,tagekyc_runtime]
tagekyc.capture_runtime_materialize_capability_expiry(uuid, uuid, timestamp with time zone, uuid)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_publish_configuration(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, boolean, integer, integer, integer, integer, integer, bigint, integer, bigint, integer, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_publish_role_policy(uuid, uuid, uuid, bigint, timestamp with time zone, text[], bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_publish_trust_profile(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, text, boolean, boolean, boolean, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_reactivate(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_read_cutover_state(text)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_read_readiness(uuid, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_reconcile_binding(uuid, uuid, uuid, bigint, uuid, uuid, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_redeem_bootstrap(uuid, uuid, bytea, uuid, bytea, bytea, timestamp with time zone, bytea, bytea, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_replay_completed_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_resolve_capability_verifier(uuid)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_resolve_configuration(uuid, uuid, uuid, bigint, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_application]
tagekyc.capture_runtime_resolve_verifier(uuid, bigint, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_authenticator]
tagekyc.capture_runtime_retire(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_revoke(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_revoke_bootstrap(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_revoke_credential(uuid, uuid, uuid, uuid, uuid, bigint, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_revoke_rotation(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_suspend(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_operator]
tagekyc.capture_runtime_validate_append_authority(uuid, uuid, uuid, bigint, uuid, uuid, text, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_runtime]
tagekyc.platform_operator_authenticate(text, timestamp with time zone)|tagekyc_raw_export_deployer|EXECUTE=[tagekyc_capture_runtime_authenticator]
```

## 4. DONE / MEASURED — role and helper definitions

DONE: exact migration preflight/role block (runtime assertions verified it in PostgreSQL):

```sql
DO $preflight$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_authid AS r
        WHERE r.rolname = 'tagekyc_raw_export_deployer'
          AND NOT r.rolcanlogin AND NOT r.rolsuper AND NOT r.rolcreatedb
          AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls
          AND r.rolinherit AND r.rolpassword IS NULL
          AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members AS m WHERE m.member = r.oid)
    ) THEN
        RAISE EXCEPTION 'TIP88C1C6BA_DEPLOYER_ROLE_INVALID' USING ERRCODE = 'P0001';
    END IF;
END
$preflight$;

DO $capability_roles$
DECLARE
    role_name text;
BEGIN
    FOREACH role_name IN ARRAY ARRAY[
        'tagekyc_capture_runtime_operator',
        'tagekyc_capture_runtime_authenticator',
        'tagekyc_capture_runtime_application'
    ]::text[] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles AS r WHERE r.rolname = role_name) THEN
            EXECUTE pg_catalog.format(
                'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',
                role_name);
        END IF;
        IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_authid AS r
            WHERE r.rolname = role_name
              AND NOT r.rolcanlogin AND NOT r.rolsuper AND NOT r.rolcreatedb
              AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls
              AND r.rolinherit AND r.rolpassword IS NULL
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members AS m WHERE m.member = r.oid)
        ) THEN
            RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_ROLE_INVALID: %', role_name USING ERRCODE = 'P0001';
        END IF;
    END LOOP;
END
$capability_roles$;

GRANT USAGE ON SCHEMA tagekyc TO tagekyc_capture_runtime_operator,
    tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_application;


```

DONE: helper SQL below is copied from migration post-SHA in §2. For each helper the listed REVOKE EXECUTE and DROP belong to Down; the preceding owner/revoke/grant belong to Up. MEASURED: Stage1 helper shape/ACL tests passed. R13 selected-candidate null handling is included verbatim; no SQL business body changed in this correction.

### capture_runtime_resolve_bootstrap_verifier

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
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) FROM tagekyc_capture_runtime_application;
DROP FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid);
```

### capture_runtime_claim_rotation_completion_nonce

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
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) FROM tagekyc_capture_runtime_authenticator;
DROP FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz);
```

### capture_runtime_validate_append_authority

```sql
CREATE FUNCTION tagekyc.capture_runtime_validate_append_authority(
    p_capture_agent_id uuid,p_installation_id uuid,p_credential_id uuid,
    p_generation bigint,p_binding_id uuid,p_verification_session_id uuid,
    p_required_role text,p_now timestamptz)
RETURNS TABLE(role_policy_id uuid,role_policy_revision bigint,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,capability_id uuid,
    capability_revision bigint,binding_id uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_b tagekyc.capture_execution_bindings%ROWTYPE;
        v_c tagekyc.capture_capabilities%ROWTYPE;
        v_s tagekyc.verification_sessions%ROWTYPE;
BEGIN
  IF p_generation<=0 OR p_required_role NOT IN ('CaptureObservation','TrustedEvidence') THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id;
  IF NOT FOUND THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_b."CaptureCapabilityId"::text,80));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_installation_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id FOR UPDATE;
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=v_b."CaptureCapabilityId" FOR UPDATE;
  SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=p_verification_session_id FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."DeviceInstallationId" IS DISTINCT FROM p_installation_id OR v_g."State" IS DISTINCT FROM 'Active'
     OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
     OR v_b."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_b."DeviceInstallationId" IS DISTINCT FROM p_installation_id
     OR v_b."CredentialId" IS DISTINCT FROM p_credential_id OR v_b."CredentialGeneration" IS DISTINCT FROM p_generation
     OR v_b."VerificationSessionId" IS DISTINCT FROM p_verification_session_id OR v_b."ExecutionExpiresAtUtc"<=p_now
     OR v_b."RuntimeRevision" IS DISTINCT FROM v_r."Revision" OR v_b."InstallationRevision" IS DISTINCT FROM v_i."Revision"
     OR v_b."CredentialRevision" IS DISTINCT FROM v_g."Revision"
     OR v_b."TrustProfileId" IS DISTINCT FROM v_r."TrustProfileId" OR v_b."TrustProfileRevision" IS DISTINCT FROM v_r."TrustProfileRevision"
     OR v_b."ConfigurationId" IS DISTINCT FROM v_r."ConfigurationId" OR v_b."ConfigurationRevision" IS DISTINCT FROM v_r."ConfigurationRevision"
     OR v_b."RolePolicyId" IS DISTINCT FROM v_g."RolePolicyId" OR v_b."RolePolicyRevision" IS DISTINCT FROM v_g."RolePolicyRevision"
     OR v_c."VerificationSessionId" IS DISTINCT FROM p_verification_session_id OR v_c."ClientApplicationId" IS DISTINCT FROM v_b."ClientApplicationId"
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
  RETURN QUERY SELECT v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."Revision",v_i."Revision",v_g."Revision",v_c."CaptureCapabilityId",v_c."Revision",v_b."CaptureExecutionBindingId";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) TO tagekyc_runtime;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) FROM tagekyc_runtime;
DROP FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz);
```

## 5. MEASURED — commands and test results

| Command / run | Result |
| --- | --- |
| First scoped run | 9 fixture failures before test bodies; Docker daemon absent. Started existing Docker Desktop, no fixture bypass. |
| Second scoped run | 8/9; invalid source-token assertion failed. |
| Third scoped run | 8/9; display-only EF brace-formatting error. R01/R02 executable proof passed. |
| Fourth scoped run | 9/9, zero skipped. New database migration, Down/reapply and injected failure restoration pass. |
| `dotnet build TagEkyc.sln --no-restore` | PASS; final incremental build 0 errors/0 warnings. IntegrationTests compilation separately emits EF1002 warnings for closed literal-manifest SQL; not hidden or treated as proof failures. |
| Full UnitTests --no-build | 210/210 PASS |
| Full ContractTests --no-build | 15/15 PASS |
| Full ArchTests --no-build | 148/150; C527 and R2A4 remain red, matching previously reported historical failures; neither test changed. |
| Full IntegrationTests / CaptureAgent suite | NOT EXECUTED in this turn. Scoped integration evidence is not full-suite green. |
| R27 HTTP success/denial | NOT EXECUTED; no substitute parser/authenticator/A3 or test-local endpoint introduced. |

MEASURED: exact scoped methods:

- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1AclTests.DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1PersistenceTests.FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1PersistenceTests.R01R02Replay_UsesDurableOperationFingerprintAndFrozenResult` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1AclTests.Foundation_functions_are_security_definer_deployer_owned_and_not_public` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1PersistenceTests.Foundation_catalogue_contains_the_lineage_event_and_expiry_guards` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1AclTests.Stage1_internal_helpers_are_callable_only_by_their_exact_roles` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1PersistenceTests.Stage1_internal_authority_helpers_have_closed_shapes_and_null_safe_rotation_candidates` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1AclTests.Public_entrypoints_have_an_explicit_single_capability_role_or_the_frozen_dual_runtime_grant` — Passed.
- MEASURED: `TagEkyc.IntegrationTests.Tip88C1C6BA1AclTests.Owner_role_identity_and_acl_negative_controls_are_discriminating` — Passed.

SPECIFIED: do not interpret existence of the other 53 proof-owner rows as completed executable proof. Stage 2 remains incomplete.

## 6. MEASURED — source provenance and graph

All 11 consumed-source SHA comparisons use the full 64 characters. All match; no partial prefix/suffix comparison.

| Path | Actual SHA-256 | Comparison |
| --- | --- | --- |
| `src/TagEkyc.Api/Program.cs` | `ADC8176B12AF9D7FE4F36A32C040483E2B143E85ED93AB6D8DE7C24702D6E193` | MATCH |
| `src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs` | `923CDF0E0204EFFC1A44FC9CC906DDB5D12A8BBD9290A76FD760906B93EC9012` | MATCH |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs` | `945FD9C6028CE3B310891F21FF3F20E033928CA8C88CDD3DEBAB09F551C7B92E` | MATCH |
| `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | `AE4408D7B2BF46A3EAFA9F1801D59C37BF3B5950662EBCA73ED1109D147762C7` | MATCH |
| `tests/TagEkyc.IntegrationTests/Tip88C1AAcceptanceSurfaceTests.cs` | `B4655A1562C6B3414066D638BD4682F0D736E94877415D967674477B5FA2D2BF` | MATCH |
| `src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs` | `532F4196BE430DCDF877F263D6DB37890E399353DE835D579A63F893616B47A8` | MATCH |
| `src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs` | `831D6032AA4C25105924501D808F5CD3E67498D7F97CAC32157BC6747EBF8A88` | MATCH |
| `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | `1023A127F4A5183C2A9A1D63060C5B7638EC66DE35F87CE084961EFC1F34598F` | MATCH |
| `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | `F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA` | MATCH |
| `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs` | `C996F31D4ADBBE7F33225879807741622CB67DDDD8305E5498C8C6CCC8357195` | MATCH |
| `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | `4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66` | MATCH |

MEASURED: staged 0; conflicted 0; tracked .csproj delta 0; source-file census also finds no new .csproj. Actual references:

- MEASURED: `src/TagEkyc.Adapters/TagEkyc.Adapters.csproj` → `..\TagEkyc.Application\TagEkyc.Application.csproj`, `..\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\TagEkyc.Domain\TagEkyc.Domain.csproj`.
- MEASURED: `src/TagEkyc.Api/TagEkyc.Api.csproj` → `..\TagEkyc.Application\TagEkyc.Application.csproj`, `..\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\TagEkyc.Infrastructure\TagEkyc.Infrastructure.csproj`.
- MEASURED: `src/TagEkyc.Application/TagEkyc.Application.csproj` → `..\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\TagEkyc.Domain\TagEkyc.Domain.csproj`.
- MEASURED: `src/TagEkyc.Contracts/TagEkyc.Contracts.csproj` → (none).
- MEASURED: `src/TagEkyc.Domain/TagEkyc.Domain.csproj` → (none).
- MEASURED: `src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj` → `..\TagEkyc.Application\TagEkyc.Application.csproj`, `..\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\TagEkyc.Domain\TagEkyc.Domain.csproj`.
- MEASURED: `src/TagEkyc.SignFlow/TagEkyc.SignFlow.csproj` → `..\TagEkyc.Contracts\TagEkyc.Contracts.csproj`.
- MEASURED: `tests/TagEkyc.ArchTests/TagEkyc.ArchTests.csproj` → `..\..\src\TagEkyc.Adapters\TagEkyc.Adapters.csproj`, `..\..\src\TagEkyc.Api\TagEkyc.Api.csproj`, `..\..\src\TagEkyc.Application\TagEkyc.Application.csproj`, `..\..\src\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\..\src\TagEkyc.Domain\TagEkyc.Domain.csproj`, `..\..\src\TagEkyc.Infrastructure\TagEkyc.Infrastructure.csproj`, `..\..\src\TagEkyc.SignFlow\TagEkyc.SignFlow.csproj`.
- MEASURED: `tests/TagEkyc.ContractTests/TagEkyc.ContractTests.csproj` → `..\..\src\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\..\src\TagEkyc.SignFlow\TagEkyc.SignFlow.csproj`.
- MEASURED: `tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj` → `..\..\src\TagEkyc.Api\TagEkyc.Api.csproj`, `..\..\src\TagEkyc.Application\TagEkyc.Application.csproj`, `..\..\src\TagEkyc.Contracts\TagEkyc.Contracts.csproj`, `..\..\src\TagEkyc.Domain\TagEkyc.Domain.csproj`, `..\..\src\TagEkyc.Infrastructure\TagEkyc.Infrastructure.csproj`.
- MEASURED: `tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj` → `..\..\src\TagEkyc.Api\TagEkyc.Api.csproj`, `..\..\src\TagEkyc.Application\TagEkyc.Application.csproj`, `..\..\src\TagEkyc.Domain\TagEkyc.Domain.csproj`.
- MEASURED: `tools/TagEkyc.ApiKeyProvisioner/TagEkyc.ApiKeyProvisioner.csproj` → `..\..\src\TagEkyc.Application\TagEkyc.Application.csproj`, `..\..\src\TagEkyc.Infrastructure\TagEkyc.Infrastructure.csproj`.
- MEASURED: `tools/TagEkyc.GDriveSync/TagEkyc.GDriveSync.csproj` → (none).

DONE: no git clean/checkout --/restore/reset, stage, commit or push was used. No predecessor RRI/dispatch/evidence file was deleted or overwritten. This report is the only new documentation file of this turn.

SPECIFIED: parent/DDL/transition successor rebind and full candidate catalogue closure remain pending at this genuine stop; no stale predecessor is falsely reported as matching the changed migration. OM current SHA above is preserved for the eventual successor. No authority SHA was silently rewritten.

## 7. SPECIFIED — gate accounting, not a completion verdict

Migration-principal v0.3:
1–2 MEASURED: deployer remains NOLOGIN; fixture uses existing admin, no direct EF deployer login.
3 MEASURED: authorized executor migrated from empty.
4 MEASURED: exact three closed roles/password/inheritance sets and their mutations passed.
5–6 MEASURED: 27 table and 47 function owners passed exact comparisons.
7 MEASURED: full non-owner function ACL plus direct table DML denial passed.
8–10 MEASURED: Down/reapply/restoration, migration-from-empty, eight original proof methods plus additional mutation passed.
11 MEASURED: no fixture source, product LOGIN or bypass added.
12 MEASURED: full solution build PASS.
13–14 MEASURED: Stage2 mutation0, staged0, conflicts0, project delta0.
15 SPECIFIED: final normative successor catalogue rebind not completed; this report binds actual implementation hashes only.
16–17 MEASURED: primary fixture failure restoration and identical canonical comparison key passed.
18 SPECIFIED: deferred A4 gate recorded below; not implemented.

Typed-gateway v0.9:
1 SPECIFIED: not satisfied; generic service/gateway stubs remain.
2–3 SPECIFIED: matrix implementation/join and shared-operation behavior not completed.
4 SPECIFIED: no public SQL added in this turn; runtime neutral append transaction integration not completed.
5–6 SPECIFIED: readiness pepper composition / Client cancellation integration not completed by this turn.
7–8 MEASURED: the three helper definitions and resolver shape exist and scoped tests passed.
9–10 SPECIFIED: full R05 Application persisted-version/ENROLL1/zeroization path not completed.
11 MEASURED: helper owner/security/search_path/ACL/Down tests passed.
12 SPECIFIED: complete normative SQL-companion reconciliation remains pending.
13 SPECIFIED: R27 test owner remains IntegrationTests; tests not materialized under an invented grammar.
14–15 SPECIFIED: HTTP success/denial proofs not run; blocked by §8.
16 MEASURED: no A3 behavior implemented.
17 SPECIFIED: full Prepared/Activated route/readiness proof not run.
18 SPECIFIED: 53 owner mappings below remain requirements, not a 53/53 runtime PASS.
19 MEASURED: project graph unchanged.
20 SPECIFIED: 11/11 consumed source matches, but full successor catalogue rebind pending.
21 MEASURED: staged/conflicted0.

SPECIFIED: typed-boundary completion by operation:
- R03,R04: management/bootstrap typed realization pending.
- R05: Application verifier-version/ENROLL1/redemption composition pending.
- R06,R07,R08,R09,R10,R11,R12: management typed realization pending.
- R13a,R13b: classifier exists; full typed authenticator/business integration pending.
- R14,R15,R16,R17,R18,R19: control/config/readiness typed realization pending.
- R20a,R20b: shared capability gateway Application realization pending.
- R21,R22,R23a,R23b: bind/reconcile/config realization pending.
- R24,R25: validator exists; one-B neutral planner/core integration pending.
- R26: no new Capture Runtime gateway authorized; existing Client cancellation seam not reimplemented.

## 8. MEASURED — genuine authority contradiction, R27 metadata binding

This is a normative/code trace, NOT a claimed end-to-end exploit test. Both main Builder and independent agent read the relevant spans and found the same incompatibility.

| Exact source | Operative constraint |
| --- | --- |
| Operation Master SHA172B1C…37314CE, R27a lines628–630 | CRT1 headers sign method, path, closed ingress metadata digest AND claimed body digest. |
| CRT1 micro-RRI SHAEDA8703C3969F6A7043E496553FEC3E2B3AD72462C319EE10BE3491490489903, lines57–65 | Raw ingress uses declared body commitment; operation lines fixed for bind/reconcile/bound operations, empty for others. |
| Earlier technical dispatch SHAF41C24BD0BD58F6B070EFE17BB69E7F89823F37B94F48A339870CD19D67825FD, lines183–205 | Exact LF CRT1 grammar; raw commitment is X-TagEkyc-Plaintext-Sha256; remaining operation-binding line empty. |
| Ratified v0.9 §5 and §9 | Real R27 proof required; no replacement metadata encoding supplied; CRT1 must not be reopened by Builder. |
| Current API CRT1 parser | Signed bytes constructed only from fixed envelope/path/media/length/body commitment/operation-binding inputs. |
| Current raw-ingress parser lines48–49 | Configuration revision and verification-session identity are separate metadata headers. |

MEASURED structural witness: take two otherwise identical raw requests and change only
`X-TagEkyc-Agent-Configuration-Revision: 1 → 2` (or only verification-session UUID).
Under the fixed raw CRT1 construction, every signed input is unchanged. Metadata
digest therefore cannot be authenticated as R27a requires. A3 must not be used to
retroactively supply the missing cryptographic binding; A1 has already authenticated N.

SPECIFIED: Adding metadata to a signature line or changing body commitment would change the frozen signed-byte contract. Leaving it unsigned would violate R27a. Neither is an ordinary implementation bug fix authorized by the current no-reopen fence. No new grammar, RRI or weakened negative proof was created.

## 9. SPECIFIED — carried A4 item

A4 production readiness/closeout must fail closed if any LOGIN retains membership
in `tagekyc_raw_export_deployer`; removal of temporary migration CREATEROLE and
SET/membership authority requires evidence. This forward item is recorded, not
implemented. Production activation and real retained-biometric ingress remain prohibited.

## 10. SPECIFIED — full 53 proof-owner requirement census

Copied from ratified v0.9 §6. These are requirement/reachability declarations, NOT
new runtime measurements. Only the named scoped methods in §5 were run here.

| # | Proof | Exact owner | Surface | Reachable as specified | File at authority census |
| --- | --- | --- | --- | --- | --- |
| 1 | `PlatformCredentialAuthentication_HasNoClientPartition` | UnitTests `Tip88C1C6BA1ContractTests.cs` | U | YES | exists |
| 2 | `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback` | UnitTests same | U | YES | exists |
| 3 | `Crt1_GoldenVectorsMatchClientAndServer` | ArchTests `Tip88C1C6BA1ArchitectureTests.cs` | CRT+ARCH | YES | exists |
| 4 | `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate` | IntegrationTests `Tip88C1C6BA1ConcurrencyTests.cs` | C+DB | YES | authorized |
| 5 | `NonceCleanup_IsBoundedAndCannotDeleteTimeValidEnvelope` | IntegrationTests `Tip88C1C6BA1PersistenceTests.cs` | DB | YES | exists |
| 6 | `RuntimeConnection_CannotResolveDefaultClientDbContext` | IntegrationTests `Tip88C1C6BA1AclTests.cs` | DB | YES | exists |
| 7 | `Enrollment_AtomicActivationAndLostResponseReplay` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 8 | `RuntimeSuspendReactivate_ExpectedRevisionSerializes` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 9 | `RuntimeRevokeRetire_TerminalWins` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 10 | `CredentialRevoke_RacesRotationWithoutSplitGeneration` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 11 | `Rotation_DualProofAtomicCutoverAndSameRouteReplay` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 12 | `RotationAuthorization_RevokeExpiryOneWinner` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 13 | `Rotation_SuccessorWithoutRoleCanReplayOnlyExactCommittedOperation` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 14 | `PlatformRootRevoke_IsIdempotentAndAudited` | IntegrationTests PersistenceTests | DB | YES | exists |
| 15 | `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 16 | `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 17 | `RootRequestFingerprint_R01R02_DomainsAreSeparatedAndImplementationIsUnique` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 18 | `BootstrapIssue_SecretOnceAndExactReplay` | IntegrationTests PersistenceTests | DB | YES | exists |
| 19 | `BootstrapIssueRevoke_OneTerminalWinner` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 20 | `RoleAssignment_IsNextGenerationOnly` | IntegrationTests PersistenceTests | DB | YES | exists |
| 21 | `ConfigurationAssignment_ReducesAndEtagIsOpaque` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 22 | `AssignedRevision_SurvivesUnassignedNewHead` | IntegrationTests PersistenceTests | DB | YES | exists |
| 23 | `CatalogPublication_ExpectedHeadAndRollbackAsRevision` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 24 | `TransitionSql02_ExceptionMapping_IsClosedAndRollsBack` | IntegrationTests PersistenceTests | DB | YES | exists |
| 25 | `Readiness_ValidatesSchemaAclPepperNonceAndConnections` | IntegrationTests PersistenceTests | DB+Api+Infrastructure | YES | exists |
| 26 | `CapabilityIssue_SecretOnceAndExactReplay` | IntegrationTests PersistenceTests | DB | YES | exists |
| 27 | `CapabilityReplace_ExpectedCurrentAndBoundWins` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 28 | `Bind_OneWinnerSameLineageReplayNoTakeover` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 29 | `BindingReconcile_IsReadOnlyAndNonEnumerating` | IntegrationTests PersistenceTests | DB | YES | exists |
| 30 | `CapabilityExpiry_FingerprintBindsTargetAndDeadline` | IntegrationTests PersistenceTests | DB | YES | exists |
| 31 | `ExpiryOnDenied_PersistsTerminalResultAndExactReplay` | `tip_88c1_c6b_a1_expiry_on_denied_proof.md` | SQL | YES | exists |
| 32 | `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 33 | `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 34 | `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors` | UnitTests ContractTests | U | YES | exists |
| 35 | `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 36 | `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` | UnitTests ContractTests | U | YES | exists |
| 37 | `SessionCancel_TerminalizesCapabilityAtomically` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 38 | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce` | IntegrationTests `Tip88C1C6BA1RawIngressBoundaryTests.cs` | HTTP27 | YES | proposed; authorized only after exact-v0.9 ratification |
| 39 | `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` | IntegrationTests same | HTTP27 | YES | proposed; authorized only after exact-v0.9 ratification |
| 40 | `PreparedRouteSet_IsLegacyOnly` | ArchTests ArchitectureTests | ARCH+Api | YES | exists |
| 41 | `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` | ArchTests ArchitectureTests | ARCH+Api | YES | exists |
| 42 | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` | IntegrationTests PersistenceTests | DB | YES | exists |
| 43 | `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` | IntegrationTests AclTests | DB+ACL | YES | exists |
| 44 | `SecretOnce_DigestsAndPepperRetirementAreClosed` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 45 | `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors` | UnitTests ContractTests | U+Application crypto | YES | exists |
| 46 | `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` | IntegrationTests PersistenceTests | DB+Infrastructure | YES | exists |
| 47 | `R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 48 | `PresentedVerifierSecrets_RejectNonCanonicalTailAliases` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 49 | `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 50 | `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` | IntegrationTests PersistenceTests | DB | YES | exists; R05 proves B rollback plus no secret and has no N; only CRT1 rows prove committed-N retention |
| 51 | `IdentityAuditGraph_InvariantsRejectMismatch` | `tip_88c1_c6b_a1_identity_audit_mutation_proof.md` | SQL | YES | exists |
| 52 | `Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 53 | `DispatchCatalogue_IsClosedAndBidirectionallyJoined` | ArchTests `Tip88C1C6BA1DispatchCatalogueTests.cs` | ARCH | YES | exists |

STOP — R27 requires signed ingress metadata, but the frozen CRT1 raw-ingress grammar has no authorized metadata commitment placement.

