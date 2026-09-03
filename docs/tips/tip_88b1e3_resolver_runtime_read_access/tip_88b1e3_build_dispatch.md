# TIP-88B1-E3 — Resolver Runtime Read Boundary — BUILD DISPATCH

**Status: SUPERSEDED FOR REMEDIATION. DO NOT REDISPATCH THIS ORIGINAL BUILD
INSTRUCTION.**

The original implementation exists in the working tree, but closeout is on hold.
All further work is governed by
`docs/tips/tip_88b1e3_resolver_runtime_read_access/tip_88b1e3_remediation_plan_v3.md`.
Where this dispatch conflicts with that plan—especially caller identity, role
closure/edge options, per-function backing reads, deployer capability baselines,
default ACLs, fulfillment materialization, or B2 constraint mode—the remediation
plan wins. No commit, push, merge, or deployment is authorized by this document.

**Build-locked brief:** `docs/tips/tip_88b1e3_resolver_runtime_read_access/tip_88b1e3_planning_brief.md`

**Repository:** `D:\Task\Remote Signing\TagEkyc`

**Baseline:** `774a94e30909d7257477e230f82a95b660793b1f`

**Tier:** Tier-1 — production database ACL and authorization evidence consistency.

Build the ratified E3 slice exactly. Do not reopen the feasibility, tenancy, or actor-authentication decisions. Do not commit, push, merge, or modify files outside the allowlist.

## 0. Binding decisions

- Production topology is **single tenant per deployment**: one hospital deployment, one PostgreSQL database. Do not add `TenantId`, tenant joins, tenant policy ownership, or shared-hospital database support.
- `ClientApplicationId` remains application/session ownership identity, not a tenant identifier.
- `raw_export_current_actor()` is provenance/anti-mixing, not authentication. `DEBT-E3-A` is accepted hardening debt and does not block this build or deployment under the selected topology.
- Runtime authorization reads cross a narrow `SECURITY DEFINER` projection boundary. Runtime receives no direct table privilege on the fourteen protected tables.
- SQL projects typed facts and takes the existing locks. C# retains the complete eligibility cause taxonomy, authorization verdict, policy checks, and evidence construction.
- The landed seven-table B3 decision-graph `SELECT` posture and all landed B1/B2/B3 function capabilities stay unchanged except for the four explicitly revoked B2 table-`SELECT` grants.

## 1. Task 0 — re-anchor before editing

1. Confirm `HEAD` equals the full baseline above.
2. Confirm `git diff -- src tests` is empty. The known docs/GDrive worktree dirt is not E3 source and must not be reverted.
3. Record the SHA-256 of `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`.
4. Run:

   ```powershell
   dotnet ef migrations has-pending-model-changes `
     --project src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj `
     --startup-project src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj `
     --context TagEkycDbContext
   ```

   It must report no pending model changes before the E3 migration is created.
5. If HEAD differs, `src/` or `tests/` is already dirty, pending-model is not clean, or the database cannot apply the baseline migration chain, STOP and report. Do not rebase, revert, regenerate landed migrations, or carry drift into E3.

## 2. Exact database surface

Create exactly one raw-SQL EF migration pair:

`<timestamp>_Tip88B1E3ResolverReadBoundary.cs` and its `.Designer.cs`.

The model snapshot is verify-only and must remain byte-identical.

### 2.1 Intended E3 functions

Create exactly these three functions; every intended identifier must be at most 63 UTF-8 bytes and round-trip verbatim through `pg_proc`:

| Function | Signature | Purpose |
|---|---|---|
| `tagekyc.raw_export_read_authorization_eligibility_inputs` | `(principal_id uuid, policy_id uuid, policy_version integer)` | Step-7 facts and B1 shared locks |
| `tagekyc.raw_export_read_authorization_policy_inputs` | `(principal_id uuid, policy_id uuid, policy_version integer)` | Step-8 policy/allowed-class facts |
| `tagekyc.raw_export_control_plane_root_health` | `()` | Bounded root-authority readiness |

A fourth runtime function is a STOP/RRI.

Every function must be:

- `LANGUAGE plpgsql`;
- `SECURITY DEFINER`;
- owned by `tagekyc_raw_export_deployer` (`NOLOGIN`);
- `SET search_path = pg_catalog`;
- fully schema-qualified internally;
- free of dynamic SQL;
- `REVOKE ALL ... FROM PUBLIC`;
- granted `EXECUTE` to `tagekyc_runtime`;
- represented by an exact readiness manifest containing identity arguments, result shape, language, `prosecdef`, owner, owner-login flag, `proconfig`, PUBLIC/runtime ACL, and a SHA-256 of the exact `prosrc` body.

Use the existing readiness code `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID` for any E3 function signature/body/owner/config/ACL/EXECUTE drift.

### 2.2 Actor binding and error contract

Both projection functions take `principal_id` only as an actor-binding input in addition to its eligibility scoping use. Before returning data:

1. call `tagekyc.raw_export_current_actor()`;
2. compare it to `principal_id`;
3. on mismatch raise SQLSTATE `P0001` with the exact message:

   `RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH`

The existing helper messages remain exact:

- unset, blank, or context set only on another connection: `P0001 RAW_EXPORT_ACTOR_CONTEXT_MISSING`;
- malformed or zero UUID: `P0001 RAW_EXPORT_ACTOR_CONTEXT_INVALID`.

Do not catch and collapse these into a substring or generic SQL error.

### 2.3 Eligibility projection and lock order

`raw_export_read_authorization_eligibility_inputs(...)` must preserve the exact existing acquisition order:

1. shared advisory lock `tip88b1:raw_export_requirement_rule_set_publish`;
2. shared advisory lock `tip88b1:grant:{principalId}:{policyId}:{policyVersion}`;
3. shared advisory lock `tip88b1:lifecycle:{policyId}:{policyVersion}`;
4. shared fulfillment locks ordered ordinally by requirement name:
   `CrossBorderAssessment`, `Dpia`, `LegalApproval`, `RetentionSchedule`.

Use the existing `hashtext(key)` + `pg_advisory_xact_lock_shared` convention. Locks are transaction-scoped and remain held until the outer B3-4 authorization transaction commits or rolls back.

The function returns typed rows, never JSON. It must return at least one scalar row even when the policy or declared requirements are absent. The typed shape must carry only:

- echoed `PolicyId`, `PolicyVersion`, and one `EvaluatedAtUtc = transaction_timestamp()`;
- policy existence and nullable bound `RequirementRuleSetVersion`;
- current maximum rule-set version for fixed rule-set id `RAW_EXPORT_REQUIREMENTS`;
- nullable closure type;
- nullable latest grant identity/revision/event fields;
- nullable latest lifecycle identity/revision/event fields;
- one canonically ordered row per declared non-`ConsentArtifact` requirement, including ordinal and requirement type;
- nullable latest fulfillment identity/revision/event, artifact ref/version, `ValidFromUtc`, and `ValidUntilUtc` for that requirement.

The SQL function selects facts only. It must not decide Active/Inactive, choose a B1 cause, order causes, decide fulfillment validity, or construct a B3 verdict.

`EfRawExportControlPlaneRepository.ResolveExportEligibilityForAuthorizationAsync` consumes this typed projection and preserves the landed C# computation byte-semantically:

- exact eight-cause taxonomy and priority;
- latest-event interpretation;
- `ValidFromUtc <= EvaluatedAtUtc < ValidUntilUtc` validity;
- `ConsentArtifact` exclusion;
- refs and deterministic order.

Remove only its direct EF reads and in-C# lock calls that the projection function now performs. Its ambient-transaction requirement and public interface stay intact.

### 2.4 Policy projection

`raw_export_read_authorization_policy_inputs(...)` returns a typed policy-specific projection containing only:

- echoed `PolicyId` and `PolicyVersion`;
- policy existence;
- `PermitTtlSeconds`;
- nullable closure type;
- allowed raw classes in canonical ordinal order;
- its own `transaction_timestamp()`.

It takes no additional advisory lock: policy version, closure, and allowed-class families are immutable after publication, and the mutable-stream locks were already acquired at step 7. Do not invent a new key or reacquire the B1 mutable-stream locks in a different order.

It must return a scalar row even when the policy does not exist or has no allowed-class row. It must not return or reconstruct the full `RawExportPolicyVersion`, and it must not return mode, purpose, retention, controller, recipient, jurisdiction, transfer, provenance, or closure-audit columns.

`EfRawExportAuthorizationRepository` replaces only step 8's `policies.GetVersionAsync` call with this projection. It derives the existing minimal status check from policy existence + closure type, applies the existing TTL bounds, class coverage, expiry, and evidence logic in C#, and does not change step order or public outcomes.

`EfRawExportPolicyRepository`, `GetVersionAsync`, `AddVersionAsync`, and `CloseAsync` remain byte-unchanged.

### 2.5 Projection materialization must fail closed

Use a new `IRawExportAuthorizationProjectionReader` application port and a single `EfRawExportAuthorizationProjectionReader` implementation. The SQL calls live only in that implementation.

Place the port and its typed projection records in `RepositoryPorts.cs`; do not create an additional application file. The records expose only the fields in sections 2.3/2.4 and no EF/Npgsql type.

Both reader methods require the caller's ambient authorization transaction. Preserve `RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION` for a direct B1 resolver call without one; never open or commit a nested transaction in the reader.

Materialization must fail closed on:

- zero eligibility rows;
- echoed policy identity mismatch;
- inconsistent repeated scalar fields;
- duplicate or non-contiguous ordinals;
- duplicate allowed classes or requirement rows;
- an unknown enum token;
- a partially populated grant/lifecycle/fulfillment identity;
- projection timestamps that differ inside the same authorization transaction.

At the authorization-engine boundary these are the existing `RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE`; do not invent a public business denial for infrastructure drift.

### 2.6 Root health projection

`raw_export_control_plane_root_health()` takes no arguments and returns exactly:

- bounded boolean `IsHealthy`;
- bounded status code.

Allowed status codes:

- `OK`;
- `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING`;
- `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT`.

It checks the fixed production root authority types `GrantAdmin`, `RecorderAuthorityAdmin`, and `ActivationAuthority`, preserving the current latest-event and missing-before-dev-default behavior. It returns no principal, authority, scope, revision, event, decision, or timestamp detail.

`RawExportControlPlaneReadinessValidator` must call this function instead of directly reading `raw_export_control_authorities`. A non-healthy result raises the returned pinned code. Do not grant runtime `SELECT` on the table.

## 3. ACL changes

### 3.1 Up

The E3 migration:

1. grants `SELECT` on `tagekyc.raw_export_policy_allowed_classes` to `tagekyc_raw_export_deployer` only;
2. creates and secures the three functions above;
3. revokes `SELECT` from `tagekyc_runtime` on exactly:
   - `tagekyc.verification_sessions`;
   - `tagekyc.raw_export_subject_consent_authorities`;
   - `tagekyc.raw_export_subject_consent_events`;
   - `tagekyc.raw_export_subject_consent_classes`.

It grants no table privilege to runtime.

### 3.2 Fourteen-table forbidden privilege manifest

For `tagekyc_runtime`, all 98 cells — fourteen tables times seven privileges — must be false:

Privileges: `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER`.

Tables:

1. `raw_export_policy_versions`
2. `raw_export_policy_allowed_classes`
3. `raw_export_policy_requirements`
4. `raw_export_policy_closures`
5. `raw_export_requirement_rule_sets`
6. `raw_export_grants`
7. `raw_export_fulfillments`
8. `raw_export_policy_lifecycle`
9. `verification_sessions`
10. `raw_export_subject_consent_authorities`
11. `raw_export_subject_consent_events`
12. `raw_export_subject_consent_classes`
13. `raw_export_requirement_rules`
14. `raw_export_control_authorities`

`RawExportControlPlaneReadinessValidator` checks the full set with `has_table_privilege` or an equivalent set-based catalog query that includes privileges inherited through role membership and PUBLIC.

Pin the new failure code:

`PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE`

Any one true cell raises that code and maps to HTTP 503 through the existing readiness exception path.

### 3.3 Down

`Down()` must:

1. remove the three E3 runtime EXECUTE grants and drop exactly the three E3 functions;
2. revoke the E3 deployer-only `raw_export_policy_allowed_classes` SELECT;
3. restore exactly the four B2 `tagekyc_runtime` SELECT grants listed in 3.1.

It must not restore or manage out-of-band grants on the eight authorization tables, `raw_export_requirement_rules`, or `raw_export_control_authorities`.

## 4. Ratified C# seam

The production call graph is fixed:

```text
Step 7:
EfRawExportAuthorizationRepository
  -> IRawExportControlPlaneRepository.ResolveExportEligibilityForAuthorizationAsync
  -> EfRawExportControlPlaneRepository
  -> IRawExportAuthorizationProjectionReader eligibility method

Step 8:
EfRawExportAuthorizationRepository
  -> IRawExportAuthorizationProjectionReader policy method
```

Do not let the authorization repository call the eligibility projection directly. `PausingControlPlaneRepository` must remain the real B3-4 step-7 race seam.

Register the reader as scoped. Ensure the control-plane repository and authorization repository receive the same scoped reader instance under DI.

Preserve the existing one-`DbContext` convenience constructors used by landed integration tests, delegating them to the new concrete reader. DI must select the reader-injected constructor; do not add a service locator or create a second reader inside the DI path.

Update the B3-4 test constructor/fakes only as needed for the new step-8 dependency:

- preserve all existing L-tests;
- preserve `PausingControlPlaneRepository`;
- replace `MissingVersionPolicyRepository` with a projection-reader fake that proves the actual production step-8 path;
- do not present the now-unused policy fake as coverage.

Direct B1 resolver integration tests must set the correct transaction-local actor GUC inside the ambient transaction before invoking the actor-bound function. Keep the no-ambient test unchanged, and do not weaken E3's missing/blank/malformed/mismatch/other-connection negatives.

The landed B1 readiness test currently provisions broad runtime table `SELECT`; remove that obsolete provisioning when the test runs against the current E3 schema, while preserving its B1 function-drift cases under the expanded manifest. The B2 rollback test deliberately runs at the historical B1 migration: do not invoke the current E3-aware validator while the schema is rolled back before E3 exists. Its B1 ACL snapshot equivalence remains mandatory, and B2 readiness must still pass after B2 reapply.

## 5. Exact verification gates and permanent test names

All permanent E3 tests live in:

`tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs`

except the narrowly adapted landed B1/B3-4 tests named below.

### F1 — privilege floor

- `F1_runtime_has_zero_of_seven_privileges_on_all_fourteen_tables`
  - reads all 98 catalog cells and asserts each is false.
- `F1_runtime_direct_select_and_mutations_fail_42501_on_all_fourteen_tables`
  - uses a real `LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION` role whose only membership is `tagekyc_runtime`;
  - does not use `SET ROLE`;
  - for every table, direct `SELECT`, `INSERT`, `UPDATE`, `DELETE`, and `TRUNCATE` each fail with exact SQLSTATE `42501`.
- `F1_each_forbidden_privilege_cell_turns_readiness_red`
  - scratch-grants each of the 98 cells one at a time;
  - each one makes the validator fail with exact code `PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE`;
  - revoke in `finally` after each case.
- `F1_forbidden_privilege_code_maps_to_http_503`
  - one representative committed scratch grant produces readiness HTTP 503 carrying the pinned code; restore it in `finally`.
- `F1_landed_security_definer_capabilities_still_succeed`
  - proves landed B1 append, B2 consent write/resolver, B3 session-lock/claim/persist capabilities needed by the engine still work after the table revokes.

### F2 — function identity, body, ACL, actor binding

- `F2_intended_function_names_are_within_63_bytes_and_round_trip_exactly`
- `F2_each_e3_function_has_exact_signature_result_language_body_owner_search_path_and_acl`
- `F2_each_function_manifest_drift_turns_readiness_red`
  - cover at least body, owner, `SECURITY DEFINER`, search path, PUBLIC EXECUTE, and missing runtime EXECUTE;
  - pin `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`;
  - every mutation is restored byte-/catalog-exact in `finally`.
- `F2_actor_context_positive_and_five_negative_controls_are_exact`
  - correct actor succeeds for both projections;
  - missing, blank, malformed, mismatch, and context set only on another connection each fail `P0001` with the exact message from section 2.2.

### F3 — real runtime-only engine

- `F3_runtime_only_login_authorizes_and_denies_without_set_role`
  - use the real login shape from F1;
  - assert `current_user` is the ephemeral login, `session_user` is the same login, it inherits only `tagekyc_runtime`, and it is not owner/deployer/bootstrapper;
  - full Authorized succeeds;
  - `EXPORT_ELIGIBILITY_INACTIVE` Denied succeeds and persists the B1 primary/full ordered causes;
  - no fixture-owner DbContext may execute the engine under test.
- `F3_eligibility_projection_execute_is_required_by_inactive_engine_path`
- `F3_policy_projection_execute_is_required_by_authorized_engine_path`
- `F3_root_health_execute_is_required_by_readiness_path`

For each necessity test: revoke exactly that E3 function's runtime EXECUTE, run the named path, assert exact `42501` at the repository boundary or the pinned function-ACL readiness 503 as applicable, and restore in `finally`. Do not fold the three functions into one undifferentiated green test.

### F4 — lock preservation and mutation proof

The landed permanent test remains:

`L5_b1_mutation_waits_for_authorization_commit_without_stale_authorized`

Its three named theory cases are:

- `grant-revoke`;
- `lifecycle-suspend`;
- `fulfillment-withdraw`.

Scratch-mutate the new eligibility projection function one lock path at a time:

- remove the grant shared lock -> `grant-revoke` must go RED;
- restore; remove the lifecycle shared lock -> `lifecycle-suspend` must go RED;
- restore; remove the fulfillment shared-lock path -> `fulfillment-withdraw` must go RED.

Restore the migration/function byte-identically after each mutation and report each observed red. A green mutation run is a STOP; do not weaken or rewrite the race test to force red.

### F5 — one transaction timestamp

- `F5_both_projection_times_equal_b2_and_persisted_decision_time`
  - step-7 projection time equals step-8 projection time;
  - both equal the B2 consent snapshot `EvaluatedAtUtc`;
  - persisted B3 decision/evidence time is the same value;
  - no `DateTimeOffset.UtcNow` or second DB clock read may substitute.

### F6 — apply / rollback / reapply

- `F6_migration_apply_rollback_reapply_restores_snapshot_functions_and_acls_exactly`
  - capture the complete pre-E3 function/owner/ACL and relevant role/table ACL catalog;
  - apply E3 and prove the intended delta only;
  - rollback E3 and prove pre-E3 catalog equivalence, including restoration of the four B2 SELECT grants;
  - reapply and prove the E3 state again;
  - prove runtime retains landed function capabilities;
  - prove the ModelSnapshot hash is unchanged.

### F7 — bounded root readiness

- `F7_root_health_is_bounded_and_returns_no_authority_identifiers`
  - assert the result schema and allowed values only;
  - grep/catalog-test that no authority/principal identifiers or detail columns are returned.
- `F7_root_readiness_uses_sd_health_and_preserves_exact_failure_codes`
  - healthy roots pass under the runtime-only login while direct table SELECT fails `42501`;
  - missing root produces HTTP 503 with `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING`;
  - dev/default root produces HTTP 503 with `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT`;
  - the validator performs no EF/direct table read of `raw_export_control_authorities`.

## 6. Runbook

Update `docs/deployment/hospital_trial/postgres_migration_runbook.md`:

- remove the broad runtime table-SELECT instructions;
- state the zero-of-seven privilege floor for all fourteen tables;
- add the three **new E3** EXECUTE grants without replacing landed B1/B2/B3 function grants;
- document deployer-only allowed-class SELECT;
- document the four B2 runtime SELECT revokes;
- retain per-hospital DB isolation, runtime LOGIN/capability-role provisioning, secret rotation, and break-glass audit as operational responsibilities;
- do not claim resistance to full backend compromise or multi-hospital shared-DB support.

## 7. File allowlist

Permanent edits are limited to:

- `src/TagEkyc.Application/Ports/RepositoryPorts.cs`
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationProjectionReader.cs` (new)
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationRepository.cs`
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs`
- `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs`
- `src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/<timestamp>_Tip88B1E3ResolverReadBoundary.cs` (new)
- `src/TagEkyc.Infrastructure/Persistence/Migrations/<timestamp>_Tip88B1E3ResolverReadBoundary.Designer.cs` (new)
- `tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs` (new)
- `tests/TagEkyc.IntegrationTests/Tip88B34AuthorizationEngineTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88B2SubjectExportConsentTests.cs`
- `docs/deployment/hospital_trial/postgres_migration_runbook.md`

`TagEkycDbContextModelSnapshot.cs` is verify-only and must not remain modified. No API route/DTO, policy repository, B1/B2/B3 landed migration, raw-export taxonomy, public error, SignFlow, tenant model, or other test file may change. A required permanent edit outside this list is a STOP/RRI.

The E3 planning brief and this build dispatch are coordinator-prepared, reference-only inputs once Task 0 starts; the builder must not rewrite them.

Scratch mutations for F1/F2/F4 are evidence-only, must be restored in `finally`, and must leave every non-allowlisted file and landed database object byte-/catalog-equivalent.

## 8. Validation

Run, in order:

1. targeted E3 integration tests;
2. adapted B1 control-plane tests;
3. full B3-4 authorization-engine tests;
4. pending-model gate again;
5. full solution build;
6. full solution test suite.

Use the solution's real build/test commands, do not filter away failures, and report:

- projects built;
- total passed, failed, skipped;
- every failure and whether it is environmental or product;
- exact F1–F7 test-name mapping;
- all three F4 observed-red mutation results;
- before/after ModelSnapshot SHA-256;
- final `git status --short`;
- every workaround or deviation.

Do not call the slice complete if any required test is skipped, any mutation proof stays green, pending-model is dirty, snapshot differs, an ACL restore is incomplete, or the full suite has an unexplained failure.

## 9. STOP / redlines

STOP and report rather than improvise if:

- the typed two-function shape cannot carry the required data without JSON/dynamic SQL/a fourth runtime function;
- SQL would need to implement the B1 taxonomy or authorization verdict;
- actor context would need to be presented as authentication;
- a direct runtime table grant appears necessary;
- any lock key/order differs from landed B1;
- a B3-4 pausing/race seam would be bypassed;
- `EfRawExportPolicyRepository` would need modification;
- a raw byte/payload/content surface is introduced;
- a tenant model or shared-hospital database assumption becomes necessary;
- rollback cannot restore the four B2 grants and pre-E3 catalog exactly;
- any F1/F2/F4 mutation proof does not turn its named gate red.

## 10. Commit discipline

Do not commit. Do not push. Do not merge.

Do not revert or stage unrelated dirty files. Do not include `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, GDrive tooling, `bin`, `obj`, local models/datasets, secrets, credentials, raw eKYC material, or review packets.

## 11. Report format

Report:

1. baseline, Task-0 status, and pending-model result;
2. exact files changed;
3. function signatures/result shapes/security manifest;
4. ACL delta and 14 × 7 matrix result;
5. F1–F7 → exact test names and results;
6. F4 three mutation-red observations and byte-identical restoration;
7. migration apply/rollback/reapply evidence;
8. snapshot hashes;
9. full build and full-suite real totals, including failures/skips;
10. deviations, workarounds, or anything not completed.

I will verify on code and catalog evidence, not on a green-summary claim.
