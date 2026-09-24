# TIP-88C1-C6B-A1 — Bounded RRI: Migration Execution Principal

RRI ID: `C6B-A1-MIGRATION-EXECUTION-PRINCIPAL-01`  
Date: `2026-09-11`  
Status: `READY_FOR_BOUNDED_INDEPENDENT_REVIEW`  
Implementation authority: `NOT_GRANTED`  
Stage 2 authority: `NOT_GRANTED`

## 1. Purpose and boundary

This RRI corrects one A1 migration-executability contract containing three
measured defects. It does not change R03-R27 business semantics, CRT1, ENROLL1,
pepper derivation, request fingerprints, A1/A3 ownership, A2/A3 implementation,
the project graph, or any production LOGIN topology.

The correction separates the principal executing an EF migration from the
durable owner of the objects created by that migration:

```text
migration executor != durable object owner

test executor       = existing PostgreSQL fixture/admin login
production executor = deployment-managed migration identity; credential owned by A4
durable owner        = tagekyc_raw_export_deployer, NOLOGIN
```

No username, password, LOGIN creation, permanent CREATEROLE grant, or production
credential is authorized by this RRI. A4 remains responsible for provisioning
the production migration identity and its bounded deployment-time privileges.

The executor capability contract is exact:

```text
during migration only:
  may execute DDL in the target database and tagekyc schema
  may CREATE ROLE
  may SET ROLE / transfer ownership to tagekyc_raw_export_deployer
  must not be an application/runtime/operator/authenticator identity

after migration:
  temporary CREATEROLE is revoked
  temporary membership/SET authority over tagekyc_raw_export_deployer is revoked
  executor owns zero A1 tables and zero A1 functions
```

The isolated test admin/superuser satisfies the during-migration capabilities
without fixture impersonation. Production A4 may satisfy the same contract with
a bounded migration identity using temporary `CREATEROLE` plus direct membership
carrying SET authority over `tagekyc_raw_export_deployer`, or an equivalently
isolated deployment superuser. A4 must remove that temporary authority after
migration and retain evidence. A1 pins capabilities and lifecycle, not a
production username, password or secret representation.

## 2. Exact measured defects

Target migration:

`src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.cs`

### D1 — unsatisfiable migration-time identity

The migration begins with:

```sql
IF current_user <> 'tagekyc_raw_export_deployer' THEN
    RAISE EXCEPTION 'TIP88C1C6BA_MIGRATION_OWNER_REQUIRED';
END IF;
```

`tagekyc_raw_export_deployer` is a pre-existing `NOLOGIN` durable-owner role.
The PostgreSQL fixture runs `IMigrator` as its existing admin login and does not
`SET ROLE`. The production runbook invokes EF migration without defining a
direct deployer login. Other migrations keep deployer checks inside runtime
write guards rather than making that NOLOGIN role the EF connection identity.

Disposition: `ACTUAL_IMPLEMENTATION_CONTRADICTION — CORRECTION AUTHORITY REQUIRED`.

### D2 — capability roles referenced but absent

The migration grants privileges to these exact A1 capability roles but creates
none of them:

| role | measured GRANT references | measured CREATE references |
|---|---:|---:|
| `tagekyc_capture_runtime_operator` | 14 | 0 |
| `tagekyc_capture_runtime_authenticator` | 5 | 0 |
| `tagekyc_capture_runtime_application` | 12 | 0 |

Disposition: `ACTUAL_MISSING_DDL`.

### D3 — table owner implicitly depends on executor

The migration contains 27 raw-SQL `CREATE TABLE` statements, 47 existing
`ALTER FUNCTION ... OWNER TO tagekyc_raw_export_deployer` statements, and zero
`ALTER TABLE ... OWNER TO` statements. Removing D1 alone would silently leave
all A1 tables owned by the migration executor.

The count is 47 complete ownership statements, not 43 physical source lines:
one source line contains four statements.

Disposition: `ACTUAL_MISSING_OWNERSHIP_TRANSFER`.

## 3. Ratification target

If the Homeowner ratifies the exact SHA of this RRI, the Builder is authorized
to apply all and only the following correction as one unit.

### 3.1 Replace the migration-executor preflight

Delete the requirement that `current_user` equal
`tagekyc_raw_export_deployer`. Replace it with a fail-closed catalogue preflight
that requires the pre-existing durable-owner role to exist and have exactly:

```text
rolcanlogin     = false
rolsuper        = false
rolcreatedb     = false
rolcreaterole   = false
rolreplication  = false
rolbypassrls    = false
rolinherit      = true
rolpassword     = null
inherited roles = empty
```

The migration must not grant permanent membership in the deployer role, must
not make it LOGIN, and must not infer that the executor is the durable owner.
Insufficient executor DDL/role/ownership privileges fail naturally and abort
the migration transaction.

### 3.2 Create and validate the three capability roles

Inside the A1 migration, use one guarded, idempotent role-initialization block
for exactly:

```text
tagekyc_capture_runtime_operator
tagekyc_capture_runtime_authenticator
tagekyc_capture_runtime_application
```

For an absent role, execute the landed shape:

```sql
CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
NOREPLICATION NOBYPASSRLS INHERIT
```

For an existing role on a supported reapply path, validate the same exact
closed attributes, `rolpassword IS NULL`, and an empty inbound membership set
(`pg_auth_members.member = role oid` has no row). Any incompatible attribute or
membership raises a named `P0001` exception; the migration must never broaden
or repair the role silently. No password, LOGIN, SUPERUSER, CREATEDB,
CREATEROLE, REPLICATION, BYPASSRLS or inherited capability is authorized.

These three names are A1-exclusive cluster-scope capability roles. A compatible
pre-existing instance is treated as residue from a prior A1 apply/reapply, not
as an externally owned role. Ratification explicitly authorizes A1 Down to drop
these exact roles after removing every dependency. A dependency in this or
another database makes `DROP ROLE` fail closed; Down must not use `CASCADE` or
silently retain a partially torn-down A1 authority surface.

This copies the established guarded patterns in:

- `20260815120000_Tip88C1C1ResolverAssembly.cs`;
- `20260818120000_Tip88C1C2RecipientPackage.cs`.

### 3.3 Transfer every A1 table to the durable owner

After table creation and before final ACL assertions, issue an explicit:

```sql
ALTER TABLE tagekyc.<exact_table> OWNER TO tagekyc_raw_export_deployer;
```

for every one of these 27 tables:

1. `platform_operator_credentials`
2. `capture_runtime_role_policy_revisions`
3. `capture_runtime_role_policy_heads`
4. `capture_runtime_trust_profile_revisions`
5. `capture_runtime_trust_profile_heads`
6. `capture_runtime_configuration_revisions`
7. `capture_runtime_configuration_heads`
8. `capture_runtime_registrations`
9. `capture_runtime_installations`
10. `capture_runtime_credential_generations`
11. `capture_runtime_request_nonces`
12. `capture_runtime_bootstrap_issuances`
13. `capture_capabilities`
14. `capture_execution_bindings`
15. `capture_runtime_configuration_overrides`
16. `capture_runtime_rotation_authorizations`
17. `capture_runtime_management_operations`
18. `capture_runtime_management_events`
19. `capture_capability_operations`
20. `capture_capability_events`
21. `capture_runtime_bootstrap_redemption_operations`
22. `capture_runtime_rotation_completion_operations`
23. `capture_runtime_bootstrap_redemption_events`
24. `capture_runtime_rotation_completion_events`
25. `platform_operator_root_operations`
26. `platform_operator_root_events`
27. `capture_runtime_cutover_state`

The 47 existing function ownership transfers remain exact. The closed expected
function-name manifest is:

```text
c6ba_roles_are_canonical
c6ba_reject_row_mutation
c6ba_validate_current_generation
c6ba_validate_capability_graph
c6ba_validate_configuration_override
c6ba_require_management_event
c6ba_require_capability_event
c6ba_require_redemption_event
c6ba_require_rotation_completion_event
c6ba_require_root_event
c6ba_root_provision_platform_credential
c6ba_root_revoke_platform_credential
capture_runtime_issue_bootstrap
capture_runtime_revoke_bootstrap
capture_runtime_redeem_bootstrap
c6ba_transition_runtime_lifecycle
capture_runtime_suspend
capture_runtime_reactivate
capture_runtime_revoke
capture_runtime_retire
c6ba_assert_current_operator
capture_runtime_authorize_rotation
capture_runtime_revoke_rotation
capture_runtime_complete_rotation
capture_runtime_replay_completed_rotation
capture_runtime_publish_trust_profile
capture_runtime_publish_role_policy
capture_runtime_assign_role_policy
capture_runtime_assign_configuration
capture_runtime_publish_configuration
capture_runtime_issue_or_replace_capability
capture_runtime_resolve_capability_verifier
capture_runtime_bind_capability
capture_runtime_reconcile_binding
capture_runtime_materialize_capability_expiry
capture_runtime_resolve_configuration
capture_runtime_read_readiness
capture_runtime_cancel_session_with_capability
capture_runtime_validate_append_authority
capture_runtime_read_cutover_state
platform_operator_authenticate
capture_runtime_resolve_verifier
capture_runtime_resolve_bootstrap_verifier
capture_runtime_claim_nonce
capture_runtime_claim_rotation_completion_nonce
capture_runtime_cleanup_nonces
capture_runtime_revoke_credential
```

The proof must assert set equality and count exactly 47; duplicate names or an
extra/missing overload fail. Indexes, constraints and triggers follow their
owning objects. No A1 table or function
may remain owned by the fixture/admin migration executor or by an application,
runtime, authenticator or operator role.

### 3.4 Down ordering

Down must execute in this dependency-safe order:

1. revoke `EXECUTE`, schema usage and other grants;
2. drop dependent triggers and functions;
3. drop the 27 A1 tables in the existing safe order;
4. drop the three A1 capability roles only after no owned object, membership or
   privilege dependency remains.

The pre-existing `tagekyc_raw_export_deployer` role must never be dropped.
Before each capability-role drop, Down must prove no membership edge exists in
either direction: neither `pg_auth_members.roleid = capability-role oid` nor
`pg_auth_members.member = capability-role oid`. It must not rely on `DROP ROLE`
to erase a membership edge implicitly.

## 4. Test and proof contract

The existing PostgreSQL fixture remains the migration executor. It must not be
patched to fake `current_user`, create fixture-only A1 roles/schema, skip the
preflight, or weaken the migration chain.

The owner/ACL proof must derive the complete A1 table and function sets from a
closed expected manifest and assert the expected count as well as every member.
Sampling is forbidden.

The corrected candidate must prove:

1. migration executor identity differs from the durable A1 object owner;
2. all 27 listed A1 tables exist and are owned by
   `tagekyc_raw_export_deployer`, with count exactly 27;
3. every A1 `SECURITY DEFINER` and helper function is enumerated, count asserted,
   and owned by `tagekyc_raw_export_deployer`;
4. the three capability roles exist with the exact closed NOLOGIN attributes,
   null passwords and no inherited role memberships;
5. no runtime/application/authenticator/operator role owns DDL objects;
6. PUBLIC and unrelated-role ACL denial remains exact;
7. migration-from-empty succeeds through the existing executor;
8. apply, rollback and reapply succeed from the real migration history;
9. all eight Stage-1 PostgreSQL proof methods reach their bodies and pass;
10. full solution build passes;
11. no fixture-only bypass, schema, role or LOGIN exists;
12. no Stage 2 product mutation exists.

Negative controls must make the proof RED when:

- one expected table is omitted from the owner manifest;
- one A1 table is reassigned to the executor or a capability role;
- one A1 function is reassigned away from the deployer;
- one capability role is LOGIN or gains any forbidden role attribute;
- one capability role gains a password or inherited role membership;
- PUBLIC or an unrelated role gains an A1 callable privilege;
- Down attempts to drop a role before removing its dependencies.

## 5. Reconciliation and mutation boundary

After ratification, correction may update only the already-authorized A1
catalogue/proof surfaces required to make this ownership contract literal:

- the A1 parent wording where stale;
- Operation Master;
- Literal DDL Master;
- A1 Foundation migration and its Designer only if the model snapshot requires
  an exact regeneration consequence;
- transition companions carrying executor/owner assumptions;
- A1 ACL, persistence, Down, mutation and runtime proof artifacts;
- exact SHA/provenance bindings affected by those byte changes.

No `.csproj` change is expected or authorized. No business route, DTO, SQL
transition semantics, CRT1, ENROLL1, pepper, fingerprint, A2/A3, A1/A3 seam or
project dependency may change under this RRI.

## 6. Explicit prohibitions

This RRI does not authorize:

- making `tagekyc_raw_export_deployer` a LOGIN;
- connecting EF directly as that NOLOGIN role;
- creating any product LOGIN or credential;
- granting the deployer runtime capabilities or permanent CREATEROLE;
- granting the migration executor durable A1 ownership;
- adding compatibility schema, aliases or fixture-only objects;
- weakening or narrowing any Stage-1 proof;
- Stage 2 mutation;
- stage, commit or push.

## 7. PASS gates

The bounded A1 successor implementation is PASS only when gates 1-15 below are
evidenced:

1. no requirement that the deployer be a LOGIN;
2. no direct EF connection as the NOLOGIN deployer;
3. authorized executor completes migration;
4. three capability roles have the exact closed NOLOGIN attributes, null
   passwords and empty inherited-role membership sets;
5. 27/27 A1 tables are owned by the deployer;
6. all enumerated A1 functions retain deployer ownership;
7. PUBLIC/runtime/unrelated-role ACL denial is exact;
8. apply/rollback/reapply passes;
9. migration-from-empty passes;
10. eight/eight Stage-1 PostgreSQL proofs execute and pass;
11. no fixture-only bypass or product LOGIN exists;
12. full solution build passes;
13. Stage 2 mutation count is zero;
14. staged count is zero, conflicted count is zero and `.csproj` delta is zero;
15. all changed normative artifacts are rebound by full 64-character SHA-256.

The following is a separate deferred A4 production-deployment/closeout gate. It
does not block bounded A1 implementation acceptance because this RRI authorizes
neither deployment nor A4 mutation:

16. before production activation, A4 evidence proves removal of temporary
    production migration `CREATEROLE` and deployer SET/membership authority
    after deployment. The isolated test admin does not create a production
    privilege-removal obligation.

## 8. Terminal state

```text
C6B-A1-MIGRATION-EXECUTION-PRINCIPAL-01:
READY_FOR_BOUNDED_INDEPENDENT_REVIEW

IMPLEMENTATION:
NOT AUTHORIZED UNTIL HOMEOWNER RATIFIES THIS EXACT RRI SHA

STAGE 2:
NOT AUTHORIZED

STAGE / COMMIT / PUSH:
NOT AUTHORIZED
```
