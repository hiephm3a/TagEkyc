# TIP-88B1 Raw Export Control Plane - Closeout

Status: LANDED + PUSHED to origin/tip-88a-raw-export-policy-catalog-build at 2c684cfc99d53ec56d8efe9426d624836ae298a5 (chain e63cdf9 88A -> 62fed65 88A-closeout -> 2c684cf 88B1; master 4e721c0 untouched, ahead 3, not merged). INERT of raw bytes - the control plane authorizes/records; it does NOT read, assemble, or return raw BIO.
Spec: tip_88b1_planning_brief.md v0.11 (READY_FOR_BUILD after 10 GPT+Codex review rounds). Baseline: 62fed65 (88A landed).

## What shipped (as-built)

The raw-export CONTROL PLANE over the landed 88A catalog. 88B1 builds NO authorization decision, session binding, subject-consent, idempotency, evidence, permit, or raw-byte path (those are TIP-88B2 / the later assembly slice).

Four append-only event tables, each with a monotonic contiguous `Revision`, per-aggregate advisory-lock ordering, one-winner optimistic concurrency (`ExpectedRevision`, stable conflict, no auto-retry), and UPDATE/DELETE-deny triggers:
- `raw_export_grants` - exact-version grant `(PrincipalId, PolicyId, PolicyVersion)`, Granted/Revoked. A v1 grant never authorizes v2.
- `raw_export_control_authorities` - scoped authority `(PrincipalId, AuthorityType, ScopeType, ScopeId, RequirementType)`, scope `Policy`|`Global` only (88A policy rows have no tenant/client binding), DB CHECKs on row shape, NULL-safe unique stream index, anti-escalation (runtime `Global` admin grants only `Policy`, equal-or-narrower, no self-grant/self-escalation).
- `raw_export_fulfillments` - policy-scoped requirements `{LegalApproval, Dpia, CrossBorderAssessment, RetentionSchedule}` only (`ConsentArtifact` is subject-scoped -> 88B2); two events `Accepted(SupersedesRevision?)` / `Withdrawn(TargetRevision)`; renewal path; current effective = latest non-withdrawn valid Accepted, no fallback.
- `raw_export_policy_lifecycle` - `{Activated, Suspended, Revoked}`, `Revoked` terminal; fail-closed DB-enforced activation gate (CatalogApproved + every policy-scoped requirement currently-valid-fulfilled + bound RuleSetVersion == current MAX + legal transition).

Identity: `PrincipalId` (Guid, from `ApiKeyRow.PrincipalId`) surfaced through `PostgresHashedApiKeyStore` -> `ResolvedApiKey` -> `AuthenticatedClientContext` (purely additive, no existing auth-gate change).

Effective-state resolver `ResolveExportEligibilityForAuthorizationAsync(...)`: lock-capable (takes SHARED advisory locks in canonical order, held to txn end), authoritative DB time (`transaction_timestamp()`, `EvaluatedAtUtc`), typed refs (`GrantRef`/`LifecycleRef`/`FulfillmentRefs[]`), deterministic `PrimaryCause` (Abandoned>NotCatalogApproved>Revoked>Suspended>NotActivated>NoGrant>StaleRuleSet>MissingOrInvalidFulfillment) + full `Causes[]`. **REQUIRES an ambient transaction** (throws `RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION` otherwise) so 88B2 evaluates eligibility and writes the decision in one transaction.

Write-authority: SECURITY DEFINER append functions (owner = non-login `tagekyc_raw_export_deployer`, `search_path=pg_catalog`, no PUBLIC execute, EXECUTE to `tagekyc_runtime` only, no dynamic SQL); actor from `SET LOCAL tagekyc.actor_principal_id` (transaction-local, fail-closed); direct raw-SQL event INSERT rejected. 88A tables/triggers/port UNCHANGED.

## Adversarial verify record (on-code, not test-green attestation)

Three parallel adversarial reviewers over the as-built code confirmed all crux invariants BYTE-LEVEL: lock-capable resolver (shared-lock held-to-txn-end, real two-connection blocked-race tests, deadlock-free canonical order, one-winner, DB clock); SECURITY DEFINER hardening (SD + fixed search_path + non-login owner + no PUBLIC execute + no dynamic SQL, actor SET-LOCAL fail-closed, direct-insert guard); authority CHECKs + NULL-safe unique + scope anti-escalation; fulfillment renewal state machine; activation gate fail-closed; inertness (no B2/raw surface); append-only x4; no fake-green (88A migration unchanged, ArchTest additive, existing expected values untouched).

Two issues caught + fixed BEFORE commit (verify-on-code value; the build was test-green throughout):
- **Resolver ambient-transaction footgun:** the resolver self-opened + committed its own transaction when called without an ambient one, releasing the shared advisory locks BEFORE the decision write - silently voiding the no-stale-Authorized guarantee. Fixed: the authorization variant now THROWS if there is no ambient transaction.
- **Builder scope-creep (over-reach):** a test-request ("seed rule-set v2 to test stale-bound activation") induced the builder to SHIP a production `raw_export_publish_requirement_rule_set_v2` function + REDEFINE 88A's `reject_raw_export_rule_runtime_mutation` trigger + GRANT INSERT on 88A rule tables - changing a landed slice, out of 88B1 scope. Security was not regressed (runtime role still blocked) but the STOP "no change to 88A triggers/invariants" was violated. REVERTED; the stale-ruleset test was re-done via test-only `DISABLE/ENABLE TRIGGER` seeding, shipping no runtime publisher.

## Deployment gates / debt (must be met before hospital-trial deploy)

Provision three NOLOGIN capability roles and connect the app via a LOGIN principal that inherits/holds the least-privileged `tagekyc_runtime` capability, then bootstrap real root authorities:
- `tagekyc_raw_export_deployer` (owns SD functions + INSERT on the 4 event tables), `tagekyc_runtime` (least-privilege runtime CAPABILITY role, created NOLOGIN: `USAGE` + `EXECUTE` on the 4 append functions; NO table SELECT and NOT a login principal), `tagekyc_raw_export_bootstrapper` (deploy-only: EXECUTE on `raw_export_bootstrap_global_authority`).
- The app connects as a LOGIN principal that HOLDS the `tagekyc_runtime` capability (membership/inheritance or `SET ROLE tagekyc_runtime`), NOT as deployer/bootstrapper. The read-set `SELECT` (no write) on the 88A policy/rule/closure/requirements tables + the 4 88B1 event tables is granted DIRECTLY to the `tagekyc_runtime` capability role (effective under both inheritance and `SET ROLE`; a login-role-only grant is suspended under `SET ROLE`). This SELECT is NOT migration-provided - it is a deployment step (a missing/ineffective SELECT is a generic resolver/readiness failure, not one of the five B1 codes).
- Bootstrap `GrantAdmin`/`RecorderAuthorityAdmin`/`ActivationAuthority` roots with REAL operator principals (no dev/default), deploy-time only.
- Production `/readiness` fails closed with `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING` / `_DEV_DEFAULT` / `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE` / `_FUNCTION_ACL_INVALID` / `_DEPLOYMENT_ROLE_INVALID` if mis-provisioned. The append actor is application-asserted (not DB-authenticated); its non-forgeability depends on this role separation. Tracked P1 in `docs/phase1_scope_and_debt_registry_v0_1.md`; procedure in `docs/deployment/hospital_trial/postgres_migration_runbook.md` (Raw-Export Control-Plane Role & Bootstrap Gate).

## Deferred to TIP-88B2 (authorization decision) + later

- TIP-88B2 (boundary only, opens after a verification-session recon): per-attempt authorization hot path - session binding, subject `ConsentArtifact` consent, idempotency, single-row typed decision/evidence, metadata permit (freezes exact allowed classes; NOT a bearer capability - the assembly slice revalidates current gates). B2 MUST consume `ResolveExportEligibilityForAuthorizationAsync` inside its atomic transaction and MUST NOT duplicate B1's state logic.
- Raw-assembly slice (after B2): actual raw read/assemble, real package/manifest hash, delivery, one-time consumption.
- Rule-set runtime publishing is NOT in 88B1 (rule sets remain 88A migration-only). The resolver's shared lock on the publication key is a forward-looking hook; `StaleRuleSet` is guarded today by the bound-vs-`MAX(RuleSetVersion)` comparison. A runtime publisher, if ever needed, is a separate reviewed TIP (it changes 88A's migration-only-rules governance property).

## Known deltas at closeout

- SECURITY DEFINER schema-assert test asserts owner/ACL/search_path over the full function inventory; the `prosecdef` (SECURITY DEFINER flag) direct assertion + all-function loop were added in the post-verify batch.
- Full suite: Contract 13, Arch 46, Unit 180, Integration 159 + 1 skipped (the skip is the known shared-Postgres parallel-DB flakiness, not a 88B1 regression). EF model check: no pending model changes.

## Non-normative extension note (TIP-88B1-E1, 2026-07-20)

The B1 eligibility resolver contract was later extended additively by **TIP-88B1-E1 Fulfillment Expiry Projection** (landed `ccc2e37`): `RawExportFulfillmentRef` gained a trailing nullable `DateTimeOffset? ValidUntilUtc`, projected from the current effective fulfillment row the resolver already reads. This is a downstream-consumption extension for TIP-88B3's expiry formula; it does NOT change any TIP-88B1 eligibility semantics, schema, ACL, or lock behaviour. See `docs/tips/tip_88b1e1_fulfillment_expiry_projection/tip_88b1e1_closeout.md`.
