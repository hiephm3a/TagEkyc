# TIP-88A Raw Export Policy Catalog Foundation - Closeout

Status: LANDED + PUSHED to origin/tip-88a-raw-export-policy-catalog-build at e63cdf9c9b85a779871521f1b8436fc57ccea319. INERT data foundation — no runtime raw-export capability.
Spec: `tip_88a_planning_brief.md` v0.11. Baseline: `4e721c0` (post TIP-87).

Push verification (2026-07-12):
- Remote feature branch `origin/tip-88a-raw-export-policy-catalog-build` HEAD verified == `e63cdf9`.
- `origin/master` unchanged at baseline `4e721c0`.
- Not merged into master (feature branch only).

## What shipped (as-built)

An append-only, immutable, versioned RAW-EXPORT POLICY CATALOG that a later enforcement slice (88B) will read to gate raw export. 88A builds NO export endpoint, authorize path, grant, export-attempt evidence, `RawBioPackage`, raw read/assemble, or any `Active`/`Retired` runtime state.

Six tables, all append-only (UPDATE/DELETE-deny triggers on every one):

- `raw_export_policy_versions` - insert-only version aggregate; attributes only (Mode, Purpose, retention ref/purpose-code, ConsentRequirement, controller + jurisdiction attributes); binds `RequirementRuleSetId`/`RequirementRuleSetVersion` (non-null composite FK, DB-stamped current-authoring, NOT caller-supplied); NO concrete Recipient, NO Status column, NO cross-border/legal-basis refs.
- `raw_export_policy_allowed_classes` - child table, FK + composite-unique + CHECK domain over the closed atomic raw-class taxonomy (incl. `HandSignatureImage`; excludes derived scores).
- `raw_export_policy_requirements` - declaration-only `(PolicyId, PolicyVersion, RequirementType)`, UNIQUE, FK to version, NO ArtifactRef (fulfillment = 88B).
- `raw_export_policy_closures` - single lifecycle table, PK `(PolicyId, PolicyVersion)` = the CatalogApproved-XOR-Abandoned constraint; `ClosureType` CHECK; `ClosedAtUtc` DB-stamped; non-empty `ClosedByPrincipalId`/`DecisionRef`.
- `raw_export_requirement_rule_sets` - migration-only, single family constant `RAW_EXPORT_REQUIREMENTS` (CHECK), `HomeJurisdictionCode` metadata; current-authoring = `MAX(RuleSetVersion)`.
- `raw_export_requirement_rules` - migration-only, closed selector enum + typed operand-domain CHECK; NULL-safe unique tuple index (DB-owned expression index `COALESCE(SelectorOperand,'')`).

Lifecycle = Draft / CatalogApproved / Abandoned, status DERIVED from the single closure row. Requirements are SYSTEM-DERIVED (global LegalApproval + structural mode + contextual consent/jurisdiction) from the version's BOUND rule set, computed identically in C# and the DB trigger (parity-tested), and must EQUAL the derived set (missing AND extra both rejected).

DB triggers: append-only deny x6; version-chain (first=1 / contiguous / latest-CLOSED, per-PolicyId advisory lock); closure completeness (CatalogApproved re-runs completeness against the bound rule set; Abandoned short-circuits); child same-transaction guard (xmin vs current xid); rule-table runtime-mutation reject.

Port `IRawExportPolicyRepository`: AddVersion / CatalogApprove / AbandonDraft / GetVersion / GetLatestVersion / GetLatestCatalogApprovedVersion / List - no update/delete/fulfillment-write/rule-write (ArchTest exact-set locked).

## Adversarial verify record (on-code, not test-green attestation)

Three parallel adversarial reviewers over the as-built code confirmed 13 core invariants (closure PK-XOR + real-concurrency arbiter, completeness reads bound-not-current set, version chain + advisory lock, single rule-set family, migration-only rule tables, inertness, ArchTest lock, child-guard on both tables). Findings fixed before commit:

- Child same-transaction guard used `xmin::bigint <> txid_current()` - safe direction (over-rejects) but a latent xid-wraparound availability break. Fixed to epoch-safe `parent_xmin <> pg_current_xact_id()::xid`.
- EF/DB index drift: OnModelCreating + snapshot declared a normal nullable unique index while the migration installed a `COALESCE` expression unique index of the same name. Removed the EF-owned normal index; the expression index is now DB-owned; a schema-assertion test verifies the live index def contains `COALESCE`.
- Partial-seed exposure: added a golden-vector test asserting the seeded RuleSetVersion 1 equals EXACTLY the 6 canonical tuples (catches missing/extra/altered). A DB manifest-hash was rejected as circular.
- Rule-table role separation was over-claimed as code-implemented. Corrected: the reject-trigger is the in-DB enforcement; SELECT-only runtime role is a deployment gate surfaced by a Production `/readiness` check (`PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE`).

Concurrency test hardened to assert the loser's SQLSTATE `23505` (closure PK), not just any PostgresException.

## Deployment gates / debt (must be met before hospital-trial deploy)

Keep the two layers distinct:

- **Implemented in code (always on, role-independent):** the `reject_raw_export_rule_runtime_mutation()` `BEFORE INSERT/UPDATE/DELETE` trigger on `tagekyc.raw_export_requirement_rule_sets` and `tagekyc.raw_export_requirement_rules` rejects any runtime mutation. Derivation rules can only change via a new migration.
- **Production deployment gate (NOT code, must be provisioned operationally):** the production runtime DB principal must have `SELECT` only on both rule tables — no `INSERT`/`UPDATE`/`DELETE`.
- **Fail-closed surface:** production `/readiness` returns `PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE` (HTTP 503) if the runtime principal still holds any `INSERT`/`UPDATE`/`DELETE` on either rule table, so a mis-provisioned or single-role deployment blocks readiness rather than silently weakening the derivation-rule gate.
- **No overclaim:** the EF migration does NOT auto-provision the runtime role or the privilege split; it only installs the schema + trigger. Role separation is a named operational gate.

Tracked as a P1 gate in `docs/phase1_scope_and_debt_registry_v0_1.md`; procedure in `docs/deployment/hospital_trial/postgres_migration_runbook.md` (Raw-Export Rule-Table Privilege Gate).

## Deferred to TIP-88B (enforcement spine), grounded on as-built `e63cdf9`

Grant of an already-onboarded client identity to a policy; surface `PrincipalId`; fail-closed activation gate (`Active`) requiring every declared requirement FULFILLED per mode AND the version's bound rule set to be the current-authoring (max) version before go-live; artifact-fulfillment table; durable append-only export-attempt evidence; metadata-only `RawBioPackage` shell. Actual raw read/assemble/hash is a later slice.

## Known deltas at closeout

- The superseded pointer `docs/tips/tip_88_raw_export_policy_spine/` was left untracked (not committed); the split is documented in the brief. Optional to include in a later commit.
- One full-suite run hit transient TIP-68 startup-assertion failures that passed in isolation - consistent with the known shared-Postgres parallel-DB test-infra contention, not a TIP-88A regression. The final exact command passed (Contract 13, Arch 44, Unit 180, Integration 144 + 1 skipped).
