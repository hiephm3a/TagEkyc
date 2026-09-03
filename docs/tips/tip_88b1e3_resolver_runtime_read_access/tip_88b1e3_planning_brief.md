# TIP-88B1-E3 Resolver Runtime Read Boundary (SECURITY DEFINER projection) - Planning Brief

Status: **BUILT IN WORKTREE — POST-BUILD REMEDIATION HOLD** (2026-07-24). The original build completed, but independent production-assurance review found additional caller-role, owner-backing-read, default-ACL, fulfillment-materialization, role-precedence, and B2 constraint-mode gates. The binding remediation contract is `tip_88b1e3_remediation_plan_v3.md`. Do not deploy, commit, or treat the original `READY_FOR_BUILD` disposition as closeout approval until that plan is reviewed and implemented. Baseline remains `774a94e` (not pushed).
Date: 2026-07-24

## 0. Homeowner decisions (binding)
- **Topology: SINGLE-TENANT PER DEPLOYMENT.** One hospital -> one application deployment -> one PostgreSQL database. Many hospital deployments exist, but hospitals do NOT share a database/schema. A hospital may have multiple `ClientApplicationId`s inside its own deployment; `ClientApplicationId` is application/session ownership identity, NOT a `TenantId`. Onboarding another hospital creates another deployment/database; DB credential, network boundary, backup/restore scope are separate per hospital; no central runtime credential spans hospital databases.
- Therefore: NO multi-tenant scoping, NO `TenantId`/`PolicyId->TenantId` data model, NO tenant-binding spike-2 in E3. Moving to many-hospitals-one-database is a FUTURE architecture change that MUST introduce explicit tenant/policy binding first (DEBT-TENANCY).
- Runtime read path goes through a narrow SECURITY DEFINER projection boundary; verdict/cause logic stays in C#. Option A (direct GRANT SELECT to runtime) is deleted.

## 1. Grounding (on-code recon @ 774a94e; spike-confirmed)

### 1.1 The gap
The engine reads control-plane data by DIRECT EF at step 7 (`EfRawExportControlPlaneRepository.ResolveExportEligibilityForAuthorizationAsync`, reads + shared advisory locks) and step 8 (`EfRawExportPolicyRepository.GetVersionAsync` -> `ToDomainAsync`, reads exact version + AllowedClasses + requirements + closure). **Runtime LACKS SELECT on the eight step-7/8 backing tables (1.2)**, so under the production runtime role the engine FAILS-CLOSED at steps 7/8. (Runtime does hold SELECT on the seven B3 decision-graph tables, and - from landed B2 - on four session/consent tables (1.3); NONE of those are the eight authorization dependencies.) Masked because every test runs under the fixture login.

### 1.2 E3 backing-read census — nine-table union (post-build correction)
The authorization engine's step-7/8 projections read eight unique tables: `raw_export_policy_versions`, `raw_export_policy_allowed_classes`, `raw_export_policy_requirements`, `raw_export_policy_closures`, `raw_export_requirement_rule_sets`, `raw_export_grants`, `raw_export_fulfillments`, and `raw_export_policy_lifecycle`. The third E3 function, bounded root-health, additionally reads `raw_export_control_authorities`, making the full per-function backing-read union NINE. `raw_export_requirement_rules` (`GetRulesAsync`, policy-creation only) is not an E3 backing read. The exact per-function manifest and deployer capability rules are pinned in `tip_88b1e3_remediation_plan_v3.md`.

### 1.3 Landed runtime ACL is BROADER than the target posture (CORRECTED - was wrong in earlier drafts)
Migration `20260720022629_Tip88B2SubjectExportConsent.cs` lines 123-128 GRANTS `tagekyc_runtime` direct SELECT on `verification_sessions`, `raw_export_subject_consent_authorities`, `raw_export_subject_consent_events`, `raw_export_subject_consent_classes` (verified on-code; an earlier draft wrongly read the Down()-side REVOKE at line 935 as the live posture). So runtime CURRENTLY reads sessions + consent backing tables directly. E3 REVOKES these four (section 3.5). The B2 consent resolver + B3 session-lock function are SECURITY DEFINER and keep working through EXECUTE.

### 1.4 GetVersionAsync is SHARED - do NOT repurpose
`GetVersionAsync` is also called by `AddVersionAsync` and `CloseAsync` outside the authorization transaction (repo lines 111, 205). The authorization read path uses a SEPARATE authorization-specific reader (section 3.1); general policy-repository semantics stay unchanged.

### 1.5 SD-owner backing ACL
Before E3, `tagekyc_raw_export_deployer` has SELECT on eight of the nine E3 backing dependencies: seven authorization dependencies plus `raw_export_control_authorities`. It lacks only `raw_export_policy_allowed_classes`; E3 grants that one missing read to the deployer, never runtime. Post-build remediation must also prove every required owner read remains effective and that the deployer's landed DML/ownership manifest is preserved rather than incorrectly forced to zero.

## 2. Intent
A narrow SECURITY DEFINER read/projection boundary so `tagekyc_runtime` is EXECUTE-only for session/B2/control-plane/policy backing reads (never direct table SELECT on them), C# keeping all verdict logic; and a mandatory ACL cleanup removing the landed B2 runtime SELECT grants. (The landed B3 immutable decision-graph SELECT on the seven B3 tables is UNCHANGED - that read path is not part of this boundary.) Preserve the advisory-lock keys/order and the single-transaction-timestamp contract. Prove the engine runs end-to-end under a runtime-only login. Least-privilege, additive, no verdict logic in SQL. **Production identity boundary (Homeowner, binding):** API authentication + the atomic Actor + the strict session-owner check ARE the current production identity boundary; full-backend-compromise resistance is an ACCEPTED RESIDUAL RISK under the single-tenant-per-deployment topology; DEBT-E3-A is future hardening that does NOT block E3 build or deployment. The GUC does not authenticate the caller (see 3.2 / DEBT-E3-A).

## 3. Scope - RATIFIED

### 3.1 The SD projection boundary + the authorization seam (RATIFIED)
TWO typed SECURITY DEFINER read functions: (1) eligibility/control-plane inputs (step 7); (2) authorization policy inputs (step 8). Typed `RETURNS TABLE` / scalars / arrays - NO jsonb (spike found no need). Each takes the required shared advisory locks, reads the exact rows for the (principal, policy, version), projects ONLY the required columns (section 9), returns the scoped inputs + DB `transaction_timestamp()`. C# consumes them through a SEPARATE authorization-specific projection reader; the existing `GetVersionAsync`/`AddVersionAsync`/`CloseAsync` semantics are UNCHANGED. The functions MUST NOT reimplement the B1 cause taxonomy or the verdict in SQL. The projection is parameterized by actor/principal + policy/version.

### 3.2 Actor binding (provenance/anti-mixing; NOT authentication)
Each function binds `principal_id` to `tagekyc.raw_export_current_actor()` and rejects a mismatch with pinned `P0001` / `RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH`. This is PROVENANCE / anti-mixing, NOT authentication - authentication happens at the API boundary that constructs the atomic Actor from the validated credential (API secret/key -> ClientApplicationId + PrincipalId + ApiKeyId -> locked session -> strict `session.ClientApplicationId == Actor.ClientApplicationId` else `SESSION_NOT_OWNED`). Do NOT claim the GUC prevents a fully compromised backend from impersonating an actor (see DEBT-E3-A). Every SD function keeps the `raw_export_current_actor()` dependency as the abstraction point so a stronger authentication mechanism can be introduced later without redesigning the projection.

### 3.3 Function security manifest + SD-owner backing ACL
Every new function: SECURITY DEFINER; owner `tagekyc_raw_export_deployer` (NOLOGIN); `SET search_path = pg_catalog`; fully schema-qualified; no dynamic SQL; `REVOKE ALL FROM PUBLIC`; `GRANT EXECUTE` to `tagekyc_runtime` only where required. The E3 migration ALSO `GRANT SELECT ON raw_export_policy_allowed_classes TO tagekyc_raw_export_deployer` (never runtime); `Down()` revokes exactly that new deployer grant. Exactly THREE new runtime-EXECUTE functions (two projections + one readiness health); a fourth is a STOP/RRI.

### 3.4 Lock + time contract (preserve exactly; spike-confirmed, section 4a)
Preserve the existing B1 advisory-lock KEYS and acquisition ORDER; hold locks to the B3-4 transaction COMMIT (the boundary runs in the engine's ambient transaction); ONE `transaction_timestamp()` shared with B2/B3 (2.6d).

### 3.5 MANDATORY ACL cleanup (permanent, not optional, not debt)
E3 `Up()` REVOKES `tagekyc_runtime` SELECT on `verification_sessions`, `raw_export_subject_consent_authorities`, `raw_export_subject_consent_events`, `raw_export_subject_consent_classes` (the landed B2 grants, 1.3). Deployer backing SELECT remains where landed SD functions require it. `Down()` restores EXACTLY those four pre-E3 runtime SELECT grants. Apply/rollback/reapply proves catalog ACL equivalence; runtime direct SELECT on each returns `42501`; landed B2 consent writes/resolver, the B3 session-lock function and the full authorization engine still work through their SD functions.

## 4. STOP / NOT
- Runtime gets NO direct table SELECT and NO INSERT/UPDATE/DELETE/TRUNCATE/REFERENCES/TRIGGER on the eight authorization tables, the four B2 session/consent tables, OR the two forbidden non-dependency tables `raw_export_requirement_rules` + `raw_export_control_authorities`; approved mutations remain via the landed SD command functions. Runtime direct-SELECT set on all FOURTEEN = EMPTY after E3.
- No verdict/cause logic in SQL; no repurposing of `GetVersionAsync`; no jsonb; no fourth runtime function.
- NO `TenantId`/tenant-binding data model, NO multi-tenant scoping (single-tenant topology).
- No change to the landed B1 cause taxonomy or B3 evidence contract (DEBT-E3-B is a separately reviewed slice).
- ModelSnapshot VERIFY-ONLY, hashed before/after, byte-identical; NOT a stageable file.
- Any file outside the confirmed allowlist after dispatch = STOP/RRI.

## 4a. Lock / consistency census (spike-observed; RESOLVED)
Locks held to the outer engine-transaction commit. Under READ COMMITTED an equal `transaction_timestamp()` != equal MVCC snapshot, so consistency is by immutability (five families) or held locks (three mutable streams). All three mutable-stream writers BLOCKED while the reader transaction held the corresponding shared lock, and each writer stopped blocking when its one shared-lock path was removed (mutation-proven).
| State family | Mutable after publication? | Writer | Shared reader/writer key | Proof |
|---|---|---|---|---|
| policy version | NO (append-only) | `AddVersionAsync` direct raw-SQL INSERT, one txn | n/a | immutability |
| allowed classes | NO | `AddVersionAsync`, same txn | n/a | immutability |
| requirements | NO | `AddVersionAsync`, same txn | n/a | immutability |
| closure | NO after set (XOR, one per version) | `CloseAsync` direct EF + trigger | n/a | immutability (Draft pre-commit vs immutable CatalogApproved post-commit; if not approved, engine stops before step 8) |
| requirement rule-set | NO at runtime (migration-seeded) | migration seed; runtime mutation rejected | `tip88b1:raw_export_requirement_rule_set_publish` (reader) | immutability |
| grant stream | YES | SD `raw_export_append_grant` | `tip88b1:grant:{principalId}:{policyId}:{policyVersion}` | mutation race proof |
| lifecycle stream | YES | SD `raw_export_append_lifecycle` | `tip88b1:lifecycle:{policyId}:{policyVersion}` | mutation race proof |
| fulfillment streams | YES | SD `raw_export_append_fulfillment` | `tip88b1:fulfillment:{policyId}:{policyVersion}:{requirementType}` | mutation race proof |

## 5. Locked invariants
Runtime reaches eligibility+policy data ONLY through SD EXECUTE (direct-SELECT set EMPTY on the eight + four B2 tables); runtime keeps only its landed SD command capabilities. Verdict logic in C#. General `GetVersionAsync` unchanged. Advisory keys/order + single transaction_timestamp preserved; locks to commit. SD-owner holds every backing read (incl. the new deployer allowed-classes grant). Actor binding is provenance/anti-mixing; the actor-authenticity gap is an accepted residual risk under single-tenant-per-deployment and is future-hardening debt (DEBT-E3-A) that does NOT block E3 build or deployment. Single-tenant topology; no tenant data model. The four landed B2 runtime SELECT grants are revoked. The engine provably runs end-to-end under a runtime-only login.

## 6. Readiness + root-authority (RATIFIED)
Replace the validator's direct EF read of `raw_export_control_authorities` (`RawExportControlPlaneReadinessValidator` line 64, root-authority loop over `RequiredRootAuthorities`) with ONE no-argument SECURITY DEFINER health function: no caller-supplied id; returns ONLY a bounded boolean + closed status code; returns no authority ids/details; checks the fixed production root-authority classes; deployer-owned, `search_path=pg_catalog`, no PUBLIC EXECUTE, runtime EXECUTE only; exact body/owner/ACL asserted by readiness. Under runtime: bounded health returns cleanly; direct SELECT on `control_authorities` returns `42501`.
**Stable codes to pin (reuse `PROD_RAW_EXPORT_CONTROL_PLANE_*`):** projection-function ACL/manifest drift; missing runtime EXECUTE on a required function; a forbidden runtime table privilege present. The forbidden-table privilege manifest checks ALL SEVEN exact privileges on EACH of the fourteen tables - `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER` (`REFERENCES`/`TRIGGER` are real grantable privileges that ordinary DML never exercises, so they must be checked explicitly). Any ONE present for `tagekyc_runtime` - including a privilege inherited through role membership or PUBLIC - produces the pinned readiness failure/503. Use `has_table_privilege` / catalog checks that account for inherited + PUBLIC grants, including externally-granted ones E3's Down() does not manage. Also: root-authority readiness failure. (Exact tokens pinned at build-dispatch.)

## 7. Verify gates (exact test names pinned at build-dispatch)
- **F1 runtime privilege floor (two-part proof):** (i) CATALOG PRIVILEGE MATRIX - all 14 x 7 cells (`SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER` on each of the fourteen tables) are FALSE for `tagekyc_runtime` via `has_table_privilege` including inherited/PUBLIC grants; the `REFERENCES`/`TRIGGER` catalog cells are MANDATORY (do NOT pretend ordinary DML exercises them). (ii) RUNTIME-ONLY NEGATIVE SQL - under a runtime-only login, `SELECT` and each executable mutation (`INSERT`/`UPDATE`/`DELETE`/`TRUNCATE`) fail with exact SQLSTATE `42501`. Landed SD command capabilities still work.
- **F2 boundary manifest/ACL:** each of the three new functions SD + deployer-owned + search_path + no PUBLIC EXECUTE + runtime EXECUTE only; readiness 503 on drift.
- **F3 engine-under-runtime:** ephemeral LOGIN INHERIT NOSUPERUSER member-of-`tagekyc_runtime`-only; DbContext from that login; `AuthorizeExportAsync` runs (NOT SET LOCAL ROLE). Full Authorized succeeds; `EXPORT_ELIGIBILITY_INACTIVE` succeeds; direct SELECT on every protected table `42501`. Per-function EXECUTE necessity: for EACH runtime-granted E3 function a SEPARATE revoke -> a named engine/readiness path fails -> restore; a function unreached by a tested branch gets a dedicated branch proving the dependency.
- **F4 lock preservation (mutation proof):** removing each mutable-stream shared-lock path (grant/lifecycle/fulfillment) makes its named B3-4 race test go RED; restore. No manufactured race tests for the immutable families.
- **F5 time contract:** the boundary's `transaction_timestamp()` equals the B2/B3 value (2.6d).
- **F6 migration apply/rollback/reapply:** ModelSnapshot byte-identical. After Down(): E3 functions dropped; E3 EXECUTE grants removed; the new deployer allowed-classes SELECT revoked; the four B2 runtime SELECT grants RESTORED exactly; no direct-SELECT fallback for runtime on the eight; every pre-E3 privilege/function/ACL byte- or catalog-equivalent; runtime RETAINS its landed B1/B2/B3 command capabilities. Down() restores ONLY the exact grants E3 itself changed - it does NOT (and cannot) restore out-of-band operational ACL it never managed; detecting any forbidden externally-granted privilege is the F1 / section 6 readiness manifest's job, not Down()'s.
- **F7 root-readiness:** healthy root authority passes WITHOUT runtime SELECT on `control_authorities`; 503 on the pinned failure; returns no authority ids.

## 8. Spike (DONE - Q1..Q6 RESOLVED)
- Q1 shape/seam: RESOLVED -> two typed SD functions + a separate authorization projection reader; typed, no jsonb; general GetVersionAsync untouched.
- Q2 locks/consistency: RESOLVED -> keys in 4a; locks held to engine commit; three mutable writers block under the reader-held shared lock and stop blocking when the lock path is removed; five families immutable.
- Q3 actor/enumeration: RESOLVED -> mismatch fails P0001; grants actor-bound; policy/fulfillment metadata is hospital-local control-plane data under single-tenant (not cross-hospital) - enumeration minimization tracked as DEBT-E3-B.
- Q4 root readiness SD fn: RESOLVED -> bounded health without control_authorities SELECT.
- Q5 runtime chain: RESOLVED -> ephemeral runtime-only login runs Authorized + Denied; six negative controls (missing/blank/malformed/mismatch/other-connection/NULL-principal) each pinned; external context does not survive onto the engine's connection; engine sets its own transaction-local GUC.
- Q6 column census: RESOLVED -> section 9.
No further spike required.

## 9. Column projection census (spike Q6 - RESOLVED; exclude unused mutation-provenance/config columns)
| Table | Projected (required) | Excluded |
|---|---|---|
| policy_versions | PolicyId, PolicyVersion, RequirementRuleSetVersion, PermitTtlSeconds | Mode, Purpose, retention/controller/jurisdiction fields, CreatedAt (`Status` is DERIVED from the closure row, not a column) |
| allowed_classes | PolicyId, PolicyVersion, RawClass | CreatedAt |
| requirements | PolicyId, PolicyVersion, RequirementType | CreatedAt |
| closures | PolicyId, PolicyVersion, ClosureType | ClosedAtUtc, ClosedByPrincipalId, DecisionRef |
| requirement_rule_sets | RuleSetId, RuleSetVersion | MigrationRef, HomeJurisdictionCode, CreatedAt |
| grants | PrincipalId, PolicyId, PolicyVersion, Revision, EventType | ClientApplicationId, DecisionRef, RecordedByPrincipalId, RecordedAtUtc |
| fulfillments | PolicyId, PolicyVersion, RequirementType, FulfillmentEventId, Revision, EventType, ArtifactRef, ArtifactVersion, ValidFromUtc, ValidUntilUtc | SupersedesRevision, TargetRevision, DecisionRef, RecordedByPrincipalId, RecordedAtUtc |
| policy_lifecycle | PolicyId, PolicyVersion, Revision, EventType | DecisionRef, RecordedByPrincipalId, RecordedAtUtc |
`ValidFromUtc`/`ValidUntilUtc`/`EventType` are required-for-verdict (resolver rejects `ValidFromUtc > evaluatedAt`); `ArtifactRef`/`ArtifactVersion`/`FulfillmentEventId`/`Revision`/`RequirementType` are required-for-evidence (B3 `raw_export_decision_fulfillment_refs` NOT NULL). Minimizing sensitive fulfillment refs on no-grant branches = DEBT-E3-B.

## 10. Runbook + snapshot
- `docs/deployment/hospital_trial/postgres_migration_runbook.md` currently instructs a BROAD runtime GRANT SELECT (incl. `control_authorities`/`requirement_rules`, omitting `policy_allowed_classes`, and it predates the B2 session/consent grants) - REMOVE/REPLACE with the SD-EXECUTE posture: the runtime table-privilege FLOOR on all fourteen forbidden tables (eight authorization + four B2 + `requirement_rules` + `control_authorities`) is ZERO of the seven privileges `SELECT`/`INSERT`/`UPDATE`/`DELETE`/`TRUNCATE`/`REFERENCES`/`TRIGGER`; EXECUTE on the THREE NEW E3 boundary functions (this ADDS to, does not replace, the landed B1/B2/B3 function EXECUTE grants); deployer-only allowed-classes SELECT; the four B2 runtime SELECT grants revoked. Remove any operational broad grant on `requirement_rules`/`control_authorities`. In the final build allowlist.
- ModelSnapshot: VERIFY-ONLY, hashed before/after, byte-identical, not stageable.

## 11. Repo census + build allowlist (final)
- ONE migration pair `<ts>_Tip88B1E3ResolverReadBoundary.cs` + `.Designer.cs` (raw SQL: two projection functions, one readiness health function, deployer allowed-classes SELECT grant, the four B2 runtime SELECT revokes; snapshot byte-unchanged VERIFY-ONLY).
The ratified seam (minimal, preserves the landed B3-4 race-test injection point):
- **Step 7 STAYS**: `EfRawExportAuthorizationRepository` -> `IRawExportControlPlaneRepository.ResolveExportEligibilityForAuthorizationAsync` -> `EfRawExportControlPlaneRepository` -> `IRawExportAuthorizationProjectionReader` (eligibility projection). The orchestrator's step-7 CALL SITE is unchanged, so `PausingControlPlaneRepository` remains the real step-7 race seam - do NOT bypass it by having the authorization repository call the eligibility projection directly.
- **Step 8 CHANGES**: the orchestrator's `policies.GetVersionAsync` (:185) becomes a call to the authorization projection reader (policy projection).
Files:
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationRepository.cs` (MODIFY - step-8 read only: swap `policies.GetVersionAsync` for the reader; the step-7 `controlPlane.Resolve...` call site stays as-is).
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationProjectionReader.cs` (NEW - the two SD-fn calls behind `IRawExportAuthorizationProjectionReader`; the SD-fn seam lives ONLY here).
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs` (MODIFY - `ResolveExportEligibilityForAuthorizationAsync` moves ONLY its backing reads/locks to the reader's eligibility projection; the C# eligibility/cause computation is byte-semantically UNCHANGED; do NOT overload this repo with the SD-fn calls - those are the reader's).
- `src/TagEkyc.Application/Ports/RepositoryPorts.cs` + the authorization projection domain record file(s) + the DI registration file (the reader seam).
- `EfRawExportPolicyRepository.cs` / `GetVersionAsync` / `AddVersionAsync` / `CloseAsync` are NOT modified (authorization policy inputs now come from the reader); NOT in the allowlist. (Note: the B1 resolver has ONE production consumer - the orchestrator - but B1 integration tests also call it directly, so test files are in the allowlist below.)
- `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` (MODIFY - root-authority via the SD health fn + projection manifest) + wiring only if a new code is needed.
- `docs/deployment/hospital_trial/postgres_migration_runbook.md` (MODIFY - the posture above).
- `tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs` (NEW) + the engine-under-runtime proof (NEW test; name it).
- `tests/TagEkyc.IntegrationTests/Tip88B34AuthorizationEngineTests.cs` (MODIFY only as needed - inject/fake the NEW step-8 projection dependency while preserving the existing `PausingControlPlaneRepository` step-7 race seam and all landed L-tests; `MissingVersionPolicyRepository` (:661/:1448) must NOT be presented as proving the production step-8 path once that path stops calling `IRawExportPolicyRepository`).
- `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs` (MODIFY only the direct authorization-resolver tests and the landed B1 readiness compatibility paths: set the correct transaction-local actor GUC before calling the now actor-bound SD projection; remove the obsolete runtime table-SELECT provisioning from current-schema readiness tests; preserve the landed function-drift assertions under the new E3 manifest. This MUST NOT weaken the missing/blank/malformed/mismatch actor negative tests).
- `tests/TagEkyc.IntegrationTests/Tip88B2SubjectExportConsentTests.cs` (MODIFY only the B2 migration rollback test compatibility: the current E3-aware B1 readiness validator must not be invoked against a deliberately rolled-back historical B1 schema where E3 functions do not exist; preserve the pre/post B1 ACL snapshot equivalence and the B2 readiness proof after reapply).

## 12. Debt registry (recorded; NOT in E3 build scope)
- **DEBT-E3-A - TRUSTED ACTOR CONTEXT.** `raw_export_current_actor()` is app-asserted provenance, not authentication; a fully compromised backend can `set_config` an arbitrary principal. Under single-tenant-per-DB with least privilege + narrow SD EXECUTE + append-only + per-hospital DB isolation + credential rotation, a stolen runtime credential is mitigated but a full backend compromise is not solvable in-DB. Revisit if the threat model requires resistance to full backend compromise or a shared cross-hospital runtime credential; any signer/key must live OUTSIDE the compromised backend. This is an ACCEPTED RESIDUAL RISK under the current single-tenant-per-deployment topology and does NOT block E3 build or deployment.
- **DEBT-E3-B - FULFILLMENT-REF MINIMIZATION.** Evaluate whether no-effective-grant denial branches can omit `ArtifactRef`/`ArtifactVersion` without weakening the B1 cause oracle or the B3 evidence contract. Separately reviewed slice; do not change the landed taxonomy/evidence in E3.
- **DEBT-TENANCY.** Many-hospitals-one-database is FORBIDDEN until an explicit `TenantId`, `ClientApplicationId -> TenantId` membership, and immutable `PolicyId -> TenantId` ownership model are designed and verified.
- **DEBT-OPS-RUNTIME-CREDENTIAL.** Per-hospital credential provisioning, secret rotation, network isolation, break-glass audit procedure.

## 13. Review tier & attacks (final adversarial docs review)
Tier-1 (production ACL + security posture on a landed pushed slice). Attacks: (a) any of the seven forbidden privileges surviving in any cell of the fourteen-table runtime matrix after E3 (all 14 x 7 must be FALSE)? (b) verdict/cause logic in SQL? (c) advisory locks or cross-read consistency not preserved through the SD boundary? (d) an overclaim that the GUC authenticates the actor (it does not - DEBT-E3-A)? (e) E3 claims the GUC authenticates the caller, or claims DEBT-E3-A is closed? (DEBT-E3-A stays open accepted hardening debt and does NOT block E3 build/deployment under single-tenant-per-deployment.) (f) root-authority readiness still needing `control_authorities` SELECT / returning ids / an elevated login? (g) the engine-under-runtime harness silently running as fixture-superuser? (h) SD-owner missing a backing read, or the deployer grant broadened beyond allowed-classes? (i) a required-for-evidence field wrongly excluded, or the B1/B3 contract changed in E3? (j) the four B2 revokes not restored byte-exact by Down(), or snapshot drift; runbook left instructing a broad/stale grant; general GetVersionAsync repurposed? (k) any tenant-model assumption leaking into a single-tenant slice?
