# TIP-88C1-C6B-A1 — Bounded RRI: Migration Execution Principal

RRI ID: `C6B-A1-MIGRATION-EXECUTION-PRINCIPAL-01`  
Version: `v0.3`  
Date: `2026-09-11`  
Status: `READY_FOR_BOUNDED_INDEPENDENT_REVIEW`  
Implementation authority: `NOT_GRANTED`  
Stage 2 authority: `NOT_GRANTED`

Successor of exact v0.2 SHA-256:
`A0239B055A0C8A87C0838FAB623234F03BAE24EABD8F7F985EE00FD389D92DE5`.
All v0.2 decisions remain operative except the two bounded proof corrections
in §§4.1-4.2 below.

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

The 47 existing function ownership transfers remain exact. The current
migration has 47 distinct names and zero overloads. The previous name-only
proof was nevertheless vacuous against a wrong signature or overload drift.
The closed expected function-identity manifest is therefore:

```text
tagekyc.c6ba_roles_are_canonical(text[])
tagekyc.c6ba_reject_row_mutation()
tagekyc.c6ba_validate_current_generation()
tagekyc.c6ba_validate_capability_graph()
tagekyc.c6ba_validate_configuration_override()
tagekyc.c6ba_require_management_event()
tagekyc.c6ba_require_capability_event()
tagekyc.c6ba_require_redemption_event()
tagekyc.c6ba_require_rotation_completion_event()
tagekyc.c6ba_require_root_event()
tagekyc.c6ba_root_provision_platform_credential(uuid, uuid, timestamp with time zone, text, bytea, integer, bytea, timestamp with time zone)
tagekyc.c6ba_root_revoke_platform_credential(uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_issue_bootstrap(uuid, uuid, text, uuid, bigint, uuid, bigint, uuid, bigint, timestamp with time zone, bytea, text, bytea, integer, bytea, timestamp with time zone)
tagekyc.capture_runtime_revoke_bootstrap(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_redeem_bootstrap(uuid, uuid, bytea, uuid, bytea, bytea, timestamp with time zone, bytea, bytea, bytea, timestamp with time zone)
tagekyc.c6ba_transition_runtime_lifecycle(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, text, text, text, boolean)
tagekyc.capture_runtime_suspend(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_reactivate(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_revoke(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_retire(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_authorize_rotation(uuid, uuid, uuid, uuid, uuid, bigint, bigint, timestamp with time zone, bytea, timestamp with time zone)
tagekyc.c6ba_assert_current_operator(uuid, timestamp with time zone)
tagekyc.capture_runtime_revoke_rotation(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone)
tagekyc.capture_runtime_complete_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone)
tagekyc.capture_runtime_replay_completed_rotation(uuid, uuid, bigint, uuid, uuid, bytea, bytea, bytea, timestamp with time zone)
tagekyc.capture_runtime_publish_trust_profile(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, text, boolean, boolean, boolean, bytea, timestamp with time zone)
tagekyc.capture_runtime_publish_role_policy(uuid, uuid, uuid, bigint, timestamp with time zone, text[], bytea, timestamp with time zone)
tagekyc.capture_runtime_assign_role_policy(uuid, uuid, uuid, uuid, bigint, bigint, bytea, timestamp with time zone)
tagekyc.capture_runtime_assign_configuration(uuid, uuid, uuid, uuid, bigint, bigint, uuid, bytea, timestamp with time zone)
tagekyc.capture_runtime_publish_configuration(uuid, uuid, uuid, bigint, timestamp with time zone, timestamp with time zone, boolean, integer, integer, integer, integer, integer, bigint, integer, bigint, integer, bytea, timestamp with time zone)
tagekyc.capture_runtime_issue_or_replace_capability(uuid, uuid, text, uuid, bigint, uuid, uuid, text, bytea, integer, bytea, timestamp with time zone)
tagekyc.capture_runtime_resolve_capability_verifier(uuid)
tagekyc.capture_runtime_bind_capability(uuid, uuid, uuid, bigint, uuid, boolean, uuid, bytea, timestamp with time zone)
tagekyc.capture_runtime_reconcile_binding(uuid, uuid, uuid, bigint, uuid, uuid, timestamp with time zone)
tagekyc.capture_runtime_materialize_capability_expiry(uuid, uuid, timestamp with time zone, uuid)
tagekyc.capture_runtime_resolve_configuration(uuid, uuid, uuid, bigint, timestamp with time zone)
tagekyc.capture_runtime_read_readiness(uuid, timestamp with time zone)
tagekyc.capture_runtime_cancel_session_with_capability(uuid, uuid, text, text, text, uuid, bytea, timestamp with time zone, text, uuid)
tagekyc.capture_runtime_validate_append_authority(uuid, uuid, uuid, bigint, uuid, uuid, text, timestamp with time zone)
tagekyc.capture_runtime_read_cutover_state(text)
tagekyc.platform_operator_authenticate(text, timestamp with time zone)
tagekyc.capture_runtime_resolve_verifier(uuid, bigint, timestamp with time zone)
tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid)
tagekyc.capture_runtime_claim_nonce(uuid, uuid, uuid, bigint, text, bytea, timestamp with time zone, timestamp with time zone)
tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid, bigint, uuid, uuid, bytea, bytea, bytea, bytea, timestamp with time zone, timestamp with time zone)
tagekyc.capture_runtime_cleanup_nonces(timestamp with time zone, integer)
tagekyc.capture_runtime_revoke_credential(uuid, uuid, uuid, uuid, uuid, bigint, bigint, text, bytea, timestamp with time zone)
```

The manifest uses PostgreSQL canonical type spelling and comma-space separation.
Its byte representation is the exact output of this types-only identity key:

```sql
pg_catalog.format('%I.%I(%s)', n.nspname, p.proname,
                  pg_catalog.oidvectortypes(p.proargtypes))
```

The existing owner projection using
`pg_get_function_identity_arguments(p.oid)` remains present and unchanged as
the declared-signature evidence field; it is not replaced. Owner and grant
proofs additionally compute the types-only key above and compare that same key
to the manifest. This avoids argument-name and alias serialization ambiguity
without discarding the already-correct owner projection.

The proof asserts count exactly 47 with every identity appearing once. A wrong
argument list, duplicate identity, missing overload, or extra overload carrying
an A1-owned name fails. `proname`-only `LIKE`/`IN` filtering is forbidden for
both owner and grant projections. Indexes, constraints and triggers follow their
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

### 4.1 Rollback/reapply topology for cluster-scope roles

`FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` must not use
`CreateDisposableCurrentDatabaseAsync("c6ba1_down_reapply")`. PostgreSQL roles
are cluster-scope; a clone of the already-migrated primary database in the same
cluster would require its Down to drop roles still referenced by the primary.
That contradicts the fail-closed, no-CASCADE Down contract.

For this proof only, use the primary `PostgresPersistenceFixture` database under
its existing non-parallel collection. The exact sequence is:

1. assert A1 Foundation is the current migration state;
2. capture the complete pre-rollback A1 catalogue;
3. enter `try`;
4. migrate the primary database to the migration immediately preceding A1;
5. prove all A1 database-local objects are absent;
6. prove the three A1 capability roles are absent;
7. migrate forward to A1 Foundation;
8. read the complete post-reapply catalogue and assert exact before/after shape
   equality;
9. in `finally`, if current state is below A1 Foundation, best-effort migrate
   back to A1 Foundation before propagating the original failure.

The shared fixture must never be left below latest migration. No fixture source
change is authorized. The disposable-clone pattern remains untouched for Down
paths that do not remove cluster-scope roles.

### 4.2 Exact function identity projections

Both owner and grant/ACL projections must use the same types-only identity key:

```sql
pg_catalog.format('%I.%I(%s)', n.nspname, p.proname,
                  pg_catalog.oidvectortypes(p.proargtypes))
```

The existing owner projection using `pg_get_function_identity_arguments`
remains unchanged and is accompanied by this canonical comparison key. The
grant projection must stop emitting only `proname || ':' || grantees`; it must
key each row by the same canonical full identity. Actual identity set must equal
the 47-entry manifest in §3.3, with count 47 and each identity occurring once.

The exact non-owner EXECUTE manifest is closed as follows. Each listed identity
uses its full §3.3 signature; every §3.3 identity not listed below has the empty
grantee set `{}`:

```text
{tagekyc_capture_runtime_operator}:
  c6ba_root_provision_platform_credential
  c6ba_root_revoke_platform_credential
  capture_runtime_issue_bootstrap
  capture_runtime_revoke_bootstrap
  capture_runtime_suspend
  capture_runtime_reactivate
  capture_runtime_revoke
  capture_runtime_retire
  capture_runtime_authorize_rotation
  capture_runtime_revoke_rotation
  capture_runtime_publish_trust_profile
  capture_runtime_publish_role_policy
  capture_runtime_assign_role_policy
  capture_runtime_assign_configuration
  capture_runtime_publish_configuration
  capture_runtime_read_readiness
  capture_runtime_revoke_credential

{tagekyc_capture_runtime_application}:
  capture_runtime_redeem_bootstrap
  capture_runtime_complete_rotation
  capture_runtime_replay_completed_rotation
  capture_runtime_resolve_capability_verifier
  capture_runtime_bind_capability
  capture_runtime_reconcile_binding
  capture_runtime_materialize_capability_expiry
  capture_runtime_resolve_configuration
  capture_runtime_read_cutover_state
  capture_runtime_resolve_bootstrap_verifier

{tagekyc_capture_runtime_authenticator}:
  platform_operator_authenticate
  capture_runtime_resolve_verifier
  capture_runtime_claim_nonce
  capture_runtime_claim_rotation_completion_nonce
  capture_runtime_cleanup_nonces

{tagekyc_runtime}:
  capture_runtime_validate_append_authority

{tagekyc_capture_runtime_application, tagekyc_runtime}:
  capture_runtime_issue_or_replace_capability
  capture_runtime_cancel_session_with_capability
```

Names in this grouped display are shorthand references only and must resolve to
exactly one full identity in §3.3; resolution count other than one is RED. The
grant proof materializes all 47 identity rows, including empty grantee arrays,
and compares each row to this mapping. Empty is valid only for identities
omitted from all five non-empty groups.

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
- the rollback/reapply proof uses a same-cluster disposable clone while Down
  drops cluster-scope roles;
- the primary fixture is left below A1 Foundation after a failure;
- an A1 function keeps the correct name but has a wrong argument list;
- an expected overload is missing or an extra overload with an A1-owned name
  appears;
- either owner or grant projection falls back to a `proname`-only key.

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

The bounded A1 successor implementation is PASS only when gates 1-17 below are
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
16. `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` uses the
    primary non-parallel fixture database, restores A1 Foundation in `finally`,
    and does not use a same-cluster disposable clone;
17. owner and grant projections both compare exact 47-entry function identities,
    not names.

The following is a separate deferred A4 production-deployment/closeout gate. It
does not block bounded A1 implementation acceptance because this RRI authorizes
neither deployment nor A4 mutation:

18. before production activation, A4 evidence proves removal of temporary
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
