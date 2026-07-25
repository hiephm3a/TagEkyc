# TIP-88B1-E3 Post-Build Assurance Remediation Plan v3

Status: **APPROVED FOR REMEDIATION IMPLEMENTATION**

Date: 2026-07-24

Approved: 2026-07-25

HEAD anchor: `774a94e30909d7257477e230f82a95b660793b1f`

Source baseline: the post-build E3 working tree pinned in section 0. HEAD alone
does not represent the source being remediated.

This document records why the completed E3 working-tree implementation is on
deployment hold, the exact remediation contract, and the gates that must be
observed before E3 can be accepted. It supersedes conflicting instructions in
the original planning brief and build dispatch. It does not authorize a commit,
push, merge, or production deployment.

## 0. Remediation Task 0 — Baseline A: post-build working tree

Task 0 does not require `src/` or `tests/` to be clean. The existing E3
implementation is intentionally uncommitted.

Before any remediation edit:

1. Assert HEAD remains
   `774a94e30909d7257477e230f82a95b660793b1f`.
2. Recompute every source/test hash below.
3. Recompute the migration and ModelSnapshot hashes.
4. Apply the current working-tree migration to a fresh PostgreSQL 16 cluster,
   assert `160000 <= server_version_num < 170000`, and capture current migration,
   function bodies, function ACLs, role attributes, memberships, and the
   deployer table capability manifest.
5. Classify every dirty file as E3 post-build baseline, unrelated pre-existing
   dirt, or remediation allowlist.
6. Stop before editing if any pinned value differs, except that the PostgreSQL
   patch release may differ while remaining inside the pinned major-16 range.
   Explain any other drift rather than silently replacing this baseline.

Pinned source/test SHA-256:

| File | SHA-256 |
|---|---|
| `src/TagEkyc.Application/Ports/RepositoryPorts.cs` | `BE684ED798261A836A634561E809F9373F586EC8E4934983FD337909440A8038` |
| `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationProjectionReader.cs` | `F3A825617148C11B53EFC6A3ECAF4EF5B2E4F9E4C768CF00CF1B5FF9C2FF42CD` |
| `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationRepository.cs` | `21B79DA50F586444061970C074157800488F1DCD0FEB4EE5DA5D4D7084DC2DC6` |
| `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs` | `E243F8631915F54BCF9443D72042F6CAD27717AC1736FAA321F3DD8F5F69ECE2` |
| `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | `76DF46FAE187F15D83289A206CA5EA51F1AEA1138F18EB0557E8F3D13ED8FC5A` |
| `src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs` | `16E7E54E4E94131B89198416741BF046F47210E4B9A0AF534C3AF27401CA4212` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260724015546_Tip88B1E3ResolverReadBoundary.cs` | `885627346DB000D511A21F1710EAD6B55B4E539FE5620B609BA89D24CA1115E0` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260724015546_Tip88B1E3ResolverReadBoundary.Designer.cs` | `BF9E25702AF812B1AFB627301E9A9EA26B23BC1F9F18483A9EE3A326DDA08E0E` |
| `tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs` | `FB10ACAAB68F5122EAD3944C1AADCC29FFE57CC6F00AFC6B83309CBF18843AB3` |
| `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs` | `B4FC24B36FDAE68D12EB8CAF80BD46B1F17AD47BDC0D8FE99EE4617E903E4DE0` |
| `tests/TagEkyc.IntegrationTests/Tip88B2SubjectExportConsentTests.cs` | `B78ADFFA2E68A6992F0E525335B07C6901088550F509BE8853083A3E8900D44B` |
| `tests/TagEkyc.IntegrationTests/Tip88B31RawExportAuthorizationSchemaTests.cs` | `EAC4DD19EEA3645356F36F95DB05E9170D0B213855A2AB33F98FFDC2DA044363` |
| `tests/TagEkyc.IntegrationTests/Tip88B34AuthorizationEngineTests.cs` | `6EDB5C15A86CD9FCD13A384776878CFBE3E866C98E2D891C39CB02C9C1C0DB05` |

ModelSnapshot SHA-256:

`523E2823CACFB22091DDD01C3CC5D45241FC047121390A7A0647F2F645E708F6`

Pinned clean-cluster catalog baseline:

- observed `server_version_num = 160013`; the reproducibility contract pins
  major 16 (`160000 <= server_version_num < 170000`), not patch 16.13;
- current migration =
  `20260724015546_Tip88B1E3ResolverReadBoundary`;
- eligibility body SHA-256 =
  `b3852ce11556bad68ec4bb6f3ff0b8074fb9eb37e05dda45c2eafcb79c883fcf`;
- policy body SHA-256 =
  `9476121061bff3df52c7ae522b27f4f7b2878f70c99f5029883d180f47a39712`;
- root-health body SHA-256 =
  `364393e456b50ec7127dd32261ee45858b2a29753450e27f914afe9f4b49bf46`;
- B2 hardened Granted body MD5 =
  `dc736348efbee50ce45373a7302e8a53`;
- each E3 function is owned by `tagekyc_raw_export_deployer`, owner `NOLOGIN`,
  `SECURITY DEFINER`, `search_path=pg_catalog`, with exactly one explicit
  non-owner ACL row:
  `(grantor=tagekyc_raw_export_deployer,
  grantee=tagekyc_runtime, privilege=EXECUTE, is_grantable=false)`.

Pinned capability-role attributes observed on the clean cluster:

| Role | LOGIN | INHERIT | SUPERUSER | CREATEDB | CREATEROLE | REPLICATION | BYPASSRLS |
|---|---:|---:|---:|---:|---:|---:|---:|
| `tagekyc_runtime` | false | true | false | false | false | false | false |
| `tagekyc_raw_export_deployer` | false | true | false | false | false | false | false |
| `tagekyc_raw_export_bootstrapper` | false | true | false | false | false | false | false |

All three definitions above are required-role manifests. Do not infer a
different bootstrapper posture. If the accepted baseline differs, stop for
review.

Pinned pre-E3 deployer capability baseline on the nine dependencies:

| Table | Owner | SELECT | INSERT | UPDATE | DELETE | TRUNCATE | REFERENCES | TRIGGER |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| `raw_export_control_authorities` | `tagekyc` | true | true | false | false | false | false | false |
| `raw_export_fulfillments` | `tagekyc` | true | true | false | false | false | false | false |
| `raw_export_grants` | `tagekyc` | true | true | false | false | false | false | false |
| `raw_export_policy_allowed_classes` | `tagekyc` | false | false | false | false | false | false | false |
| `raw_export_policy_closures` | `tagekyc` | true | false | false | false | false | false | false |
| `raw_export_policy_lifecycle` | `tagekyc` | true | true | false | false | false | false | false |
| `raw_export_policy_requirements` | `tagekyc` | true | false | false | false | false | false | false |
| `raw_export_policy_versions` | `tagekyc` | true | false | false | false | false | false | false |
| `raw_export_requirement_rule_sets` | `tagekyc` | true | false | false | false | false | false | false |

The post-E3 table is identical except
`raw_export_policy_allowed_classes.SELECT = true`.

Unrelated dirty files to preserve and exclude from remediation:

- `.gitignore`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/00_GDRIVE_FILE_INDEX.md`
- `docs/deployment/hospital_trial/consent_and_dpia_draft.md`

The pinned SHA-256 table above covers every implementation-bearing source and test
file that a remediation or scratch mutation may alter. E3 planning and audit
documents are reviewed by explicit diff and may change only inside the
documentation allowlist; they are not mutation restore targets.

Baseline A is immutable evidence of the source being remediated. It is used to
audit the authorized remediation delta and detect unrelated drift; it is not the
restore target for mutation proofs that run after remediation code exists.

After all authorized permanent edits are complete and before the first mutation
proof, capture Baseline B: the remediated candidate hashes for every allowlisted
source/test file plus the exact candidate catalog state for migration history,
function bodies and ACL rows (including grantor), roles, memberships, table
capabilities, and the B2 hardened body. Every scratch mutation must record its
candidate pre-state and restore that exact Baseline-B byte/catalog state, not
merely return tests to green.

The final audit proves both:

1. `Baseline B - Baseline A = authorized remediation delta only`;
2. `post-mutation candidate = pre-mutation Baseline B` byte- and catalog-exact.

## 1. Why this remediation exists

Independent post-build review found production configurations and transaction
shapes that the green suite did not distinguish:

1. an elevated database `session_user` can be hidden behind `SET ROLE`;
2. transitive role membership and membership-edge options are not fully pinned;
3. readiness does not prove every SECURITY DEFINER owner backing read;
4. a migration creator's default function ACL can add an unexpected grantee;
5. malformed fulfillment rows are not rejected at the projection materializer;
6. missing named roles can bypass the pinned readiness code;
7. the B2 granted-consent compatibility function can leak the named constraint's
   `IMMEDIATE` mode into later calls in the same transaction.

The original E3 design remains valid: runtime authorization reads cross three
bounded SECURITY DEFINER capabilities, runtime has no direct privilege on the
fourteen protected tables, verdict logic remains in C#, and the topology remains
one hospital per database.

## 2. Binding production posture

- Production minimum PostgreSQL version is 16. Readiness maps
  `server_version_num < 160000` to
  `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID`.
- The application connects with a dedicated `LOGIN` role.
- The login is `INHERIT`, `NOSUPERUSER`, `NOCREATEDB`, `NOCREATEROLE`, and
  `NOREPLICATION`, `NOBYPASSRLS`.
- `session_user` must equal `current_user` on the readiness connection. This
  detects unsupported active `SET ROLE`; it is not proof of the originally
  authenticated identity against a malicious superuser using
  `SET SESSION AUTHORIZATION`. Production application connectivity is
  `NOSUPERUSER`; superuser connectivity remains an operationally forbidden
  break-glass boundary.
- Production E3 does not support `SET ROLE`.
- The login's complete transitive reachable-role set is exactly
  `{tagekyc_runtime}`.
- Exactly one `pg_auth_members` row exists across all grantors for the direct
  login-to-runtime pair. That single row has:
  - `ADMIN OPTION = false`;
  - `inherit_option = true`;
  - `set_option = false`.
- `tagekyc_runtime` is `NOLOGIN`, `INHERIT`, and is not directly or transitively
  a member of another role.
- Runtime remains at zero of the seven table privileges on all fourteen
  protected tables.
- Deployer capabilities are not confused with runtime capabilities. E3 must
  preserve accepted landed deployer write/ownership capabilities and may add
  only the approved missing backing read.

## 3. Pinned dependency and capability manifests

### 3.1 Per-function backing reads

The manifest is derived from the actual SQL bodies.

Eligibility projection:

- `raw_export_policy_versions`
- `raw_export_requirement_rule_sets`
- `raw_export_policy_closures`
- `raw_export_grants`
- `raw_export_policy_lifecycle`
- `raw_export_policy_requirements`
- `raw_export_fulfillments`

Policy projection:

- `raw_export_policy_versions`
- `raw_export_policy_closures`
- `raw_export_policy_allowed_classes`

Root-health function:

- `raw_export_control_authorities`

The union is nine tables. `raw_export_requirement_rules` is protected from
runtime direct access but is not a backing read of these three E3 functions.

### 3.2 Runtime manifest

For each of the fourteen protected tables, `tagekyc_runtime` and the effective
application login must have none of:

- `SELECT`
- `INSERT`
- `UPDATE`
- `DELETE`
- `TRUNCATE`
- `REFERENCES`
- `TRIGGER`

This includes PUBLIC and inherited effective privileges.

### 3.3 Projection-owner read manifest

`tagekyc_raw_export_deployer` must effectively have `SELECT` on every
per-function dependency in section 3.1. A missing backing read maps to
`PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`.

The backing-read check runs before root-health is invoked. Therefore a missing
deployer read on `raw_export_control_authorities` is classified deterministically
instead of surfacing as an unclassified database exception.

### 3.4 Deployer landed mutation manifest

Task 0 must capture the normalized pre-E3 effective deployer DML/ownership
manifest per table. E3 must:

- preserve all landed B1/B2/B3 command-function capabilities;
- add no new deployer write capability;
- revoke no accepted landed deployer write/ownership capability;
- add only the approved missing `SELECT` on
  `raw_export_policy_allowed_classes`.

Do not assert that the deployer has zero write capability. The deployer owns
SECURITY DEFINER command functions and requires the landed capabilities those
functions use.

## 4. Pinned readiness algorithm and precedence

Caller identity attributes are collected early but, except for identity mismatch,
are evaluated at the pinned stage below.

1. Read `session_user`, `current_user`, both identities' role attributes,
   `server_version_num`, required role definitions, and role graph edges without
   calling a named-role privilege helper. Attributes are collected here but the
   ordinary caller attributes are evaluated at step 7.
2. If `session_user != current_user`, return
   `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID`.
3. If `server_version_num < 160000`, or a required named role is missing or has
   invalid required attributes,
   return `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID`.
4. Validate exact function manifests/ACLs, migration-owned ACL invariants, and
   the per-function owner backing-read manifest. Drift returns
   `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`.
5. Invoke bounded root-health:
   - missing root -> `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING`;
   - development default -> `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT`.
6. For a direct owner/superuser connection with healthy roots and event-table
   mutation capability, preserve
   `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE`.
7. Validate the ordinary application login's attributes, transitive role
   closure, and membership-edge options. Drift returns
   `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID`.
8. Validate the runtime/current-user 14-by-7 privilege floor. Drift returns
   `PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE`.

Pinned first-code cases:

| Condition | First code |
|---|---|
| `SET ROLE` active | `...DEPLOYMENT_ROLE_INVALID` |
| PostgreSQL older than 16 | `...DEPLOYMENT_ROLE_INVALID` |
| required named role missing | `...DEPLOYMENT_ROLE_INVALID` |
| root missing through owner fixture | `...ROOT_AUTHORITY_MISSING` |
| development root through owner fixture | `...ROOT_AUTHORITY_DEV_DEFAULT` |
| healthy roots plus owner event-table mutation | `...TABLE_MUTATION_PRIVILEGE` |
| ordinary application login has invalid attributes/closure/edge | `...DEPLOYMENT_ROLE_INVALID` |

## 5. Deterministic migration-time function ACL

After creating the three exact overloads, changing owner, revoking PUBLIC, and
granting runtime EXECUTE, the E3 migration must validate:

- exact schema/name/identity arguments;
- owner equals `tagekyc_raw_export_deployer`;
- owner is `NOLOGIN`;
- normalized non-owner ACL set contains exactly one row whose
  `(grantor_oid, grantee_oid, privilege_type, is_grantable)` equals
  `(tagekyc_raw_export_deployer, tagekyc_runtime, EXECUTE, false)`;
- no PUBLIC, grant option, or unrelated grantee exists.

Owner authority is validated separately. The owner need not be represented as
an explicit ACL item, and any owner/default ACL row is excluded from the
non-owner comparison only by matching `grantee_oid = proowner`, not by dropping
the `grantor` dimension.

The check uses:

```sql
aclexplode(COALESCE(proacl, acldefault('f', proowner)))
```

Unexpected state raises inside the migration transaction before its history row
can commit. The migration does not dynamically revoke arbitrary external roles.

### Alternate-grantor mutation proof

Keep the expected deployer-granted runtime EXECUTE row present. Use a scratch
`NOLOGIN` grantor with an effective grant option to add a second runtime EXECUTE
row, and assert from `aclexplode` that its `grantor_oid` differs from the pinned
deployer grantor before readiness is called. Readiness must return
`PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`. Restore the exact
Baseline-B ACL rows in `finally`.

The migration-time proof also runs a default-ACL case where the actual migration
creator grants EXECUTE to `tagekyc_runtime`. Its pre-probe must show the
creator-granted runtime row, while the migration's expected deployer-granted row
is still created. E3 apply must fail before its history row or functions commit.

### Default-ACL mutation proof

1. Roll back E3.
2. Resolve the actual role used by the migration connection to create objects.
3. Create a scratch `NOLOGIN` grantee. Run the unrelated-grantee case and the
   alternate-grantor-to-runtime case independently.
4. Configure function default privileges for that exact creator, schema
   `tagekyc`, and object type `FUNCTIONS`.
5. Create a probe function as the same creator. In each case, assert the
   intended grantee effectively receives EXECUTE and inspect
   `(grantor_oid, grantee_oid, privilege_type, is_grantable)` to prove the
   expected unexpected row exists. If not, stop because the mutation is
   vacuous.
6. Drop the probe, then apply E3.
7. E3 must fail with no history row or E3 functions committed.
8. Remove only the scratch default-ACL entry and scratch role.
9. Reapply E3 and restore latest state in `finally`.

## 6. Fulfillment materialization contract

Every projected fulfillment:

- has non-null and non-empty `FulfillmentEventId`;
- has non-null `Revision > 0`;
- has non-null exact `EventType` of `Accepted` or `Withdrawn`.

`Accepted`:

- `ArtifactRef` is nonblank;
- `ArtifactVersion` is nonblank;
- `ValidFromUtc` is present;
- `ValidUntilUtc` may be null;
- when present, `ValidUntilUtc > ValidFromUtc`.

`Withdrawn`:

- `FulfillmentEventId` and positive revision remain required;
- `ArtifactRef`, `ArtifactVersion`, `ValidFromUtc`, and `ValidUntilUtc` are null.

`TargetRevision` and `SupersedesRevision` are not projected by E3 and are not
revalidated by this materializer. Stream monotonicity remains the landed append
function/lock contract.

Any materialization violation maps to
`RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE`. No missing artifact value is
converted to an empty string.

Permanent negative coverage includes separate cases for:

- null and empty event id;
- null, zero, and negative revision;
- null and unknown event type;
- missing/blank accepted artifact reference;
- missing/blank accepted artifact version;
- missing accepted start time;
- accepted invalid end-time ordering;
- each accepted-only field present on a withdrawn row.

Each case proves zero residue in idempotency, decisions, permits, and all decision
child-evidence tables. The test must identify the actual enforcement point; it
must not claim the materializer checked a value if Npgsql or an earlier boundary
rejected it first.

## 7. B2 named-constraint mode contract

The E3-hardened B2 granted-consent function owns the mode of:

`tagekyc.tr_raw_export_subject_consent_granted_classes_required`

It guarantees the constraint is `DEFERRED` on successful return, regardless of
the caller's input mode:

```sql
SET CONSTRAINTS
  tagekyc.tr_raw_export_subject_consent_granted_classes_required
  DEFERRED;

-- parent insert
-- class child inserts

SET CONSTRAINTS
  tagekyc.tr_raw_export_subject_consent_granted_classes_required
  IMMEDIATE;

SET CONSTRAINTS
  tagekyc.tr_raw_export_subject_consent_granted_classes_required
  DEFERRED;
```

Down restores the exact pre-E3 body. Hardened body hashes and only authorized
E3/G4 assertions are updated.

Required positive and mutation gates:

- caller first sets the named constraint `IMMEDIATE`; one valid call succeeds
  and returns with the constraint normalized to `DEFERRED`;
- two distinct valid granted-consent records execute in one runtime transaction
  and commit;
- removing entry normalization makes the input-IMMEDIATE test red;
- removing successful-return restoration makes the second-call test red.

## 8. Required role and edge gates

- Rename each required role individually; readiness must return the exact
  deployment-role code before a named-role privilege query.
- Application login -> runtime with `ADMIN OPTION = true` is invalid.
- `inherit_option = false` is invalid.
- `set_option = true` is invalid.
- A safe login-to-runtime row plus a second parallel row from another grantor is
  invalid even when the first row remains safe.
- Application login -> scratch role -> deployer-like capability is invalid.
- Runtime -> scratch privileged role is invalid.
- Recursive traversal uses role OIDs and retains defensive cycle protection.
- PostgreSQL rejects a live circular membership before readiness can inspect it.
  An integration gate asserts exact SQLSTATE `0LP01` for that rejection.
  Readiness-red gates cover valid-but-forbidden direct and transitive graphs;
  no gate claims to create a live cyclic catalog graph.

Role, membership, role rename, and default-ACL mutation gates run only in a
dedicated disposable PostgreSQL 16 container with a unique generated container
name and dynamically assigned host port. They must not use the shared port-55432
integration cluster. Each gate performs:

1. pre-state catalog capture;
2. mutation-effectiveness assertion;
3. expected readiness red;
4. `finally` restoration;
5. post-state catalog equivalence.

Sequential solution execution alone is not a substitute for this cluster
isolation because roles and role memberships are cluster-wide.

## 9. Backing-read mutation gates

For each required backing read:

1. mutate the actual source of effective privilege;
2. assert `has_table_privilege(deployer, table, 'SELECT') = false` before calling
   readiness;
3. assert readiness returns the exact function ACL code;
4. restore the grant/ownership/membership source in `finally`;
5. assert effective SELECT is true after restoration.

A direct REVOKE that leaves effective SELECT through ownership, PUBLIC, or
inheritance is not a valid mutation.

## 10. Documentation and implementation allowlist

Production:

- `RawExportControlPlaneReadinessValidator.cs`
- `EfRawExportAuthorizationProjectionReader.cs`
- `20260724015546_Tip88B1E3ResolverReadBoundary.cs`

Tests:

- `Tip88B1E3ResolverReadBoundaryTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88B31RawExportAuthorizationSchemaTests.cs`,
  method only:
  `G4_migration_apply_rollback_reapply_preserves_acl_equivalence`
  if the hardened B2 body hash changes

Documentation:

- this remediation plan;
- E3 planning brief;
- E3 build dispatch;
- E3 remediation build dispatch;
- hospital PostgreSQL migration runbook.

Forbidden:

- landed B1/B2/B3 migrations;
- ModelSnapshot;
- public API/contracts;
- SignFlow;
- tenant model;
- new runtime functions;
- runtime direct SELECT;
- unrelated tests.

## 11. Final validation

1. Run every new named non-mutation gate individually.
2. After implementation is complete and before scratch mutation proofs, capture
   Baseline B as defined in section 0.
3. Observe red mutations for role closure/edge options, function ACL grantor,
   default ACL,
   fulfillment shapes, backing-read loss, and both B2 constraint-mode cases.
4. Run E3 and B1 readiness targeted suites.
5. Run B1/B2/B3-1/B3-4 regression suites.
6. Prove pending-model clean.
7. Run full solution build.
8. Run the full suite sequentially.
9. On a fresh PostgreSQL 16 database assert:
   - `160000 <= server_version_num < 170000`;
   - current migration is E3;
   - runtime forbidden privilege cells are 0/98;
   - production-shaped `session_user == current_user`;
   - exact transitive role closure and edge options;
   - every pinned per-function backing read is effective;
   - deployer write/ownership capability equals the normalized pre-E3 baseline,
     except the one approved allowed-classes SELECT delta;
   - exact ACL on the three E3 functions;
   - hardened B2 body hash;
   - zero scratch roles and probe objects;
   - zero default-ACL entries introduced by the scratch C1 creator/grantee/schema
     mutation (not zero `pg_default_acl` rows globally);
   - ModelSnapshot hash unchanged.
10. Prove both Baseline-A/B equations from section 0.

## 12. Stop conditions

Stop rather than improvise if:

- historical readiness-code precedence cannot be preserved;
- recursive membership or edge-option mutation does not turn readiness red;
- the live-cycle test reaches readiness instead of PostgreSQL rejecting the
  circular grant with SQLSTATE `0LP01`;
- the dedicated mutation cluster cannot be isolated from the shared suite
  cluster;
- Baseline B is not captured before the first mutation proof, or a mutation does
  not restore that exact candidate state;
- a backing-read mutation does not first prove effective SELECT is absent;
- the default-ACL probe does not exercise the actual migration creator;
- fulfillment invalid data is intercepted at a different boundary than the test
  claims;
- either B2 constraint-mode mutation stays green;
- remediation requires a public error, ModelSnapshot change, landed migration
  edit, or new runtime function.

## 13. Commit discipline

Do not commit. Do not push. Do not merge. Do not deploy E3 while any required
gate is missing or red.
