# TIP-88A-E2 Policy Permit TTL Field - Closeout

Status: CLOSED — implementation committed local at `09c9359` (parent `c5c4682`); not pushed and not merged into master as of 2026-07-21.
Spec: `tip_88a_e2_planning_brief.md` (READY_FOR_BUILD, after GPT+Codex review + 4 patch rounds + 1 mid-build RRI). Baseline: `c5c4682`.
Date: 2026-07-21

## What shipped (as-built, reconfirmed on-code @ 09c9359)

Additive schema extension to the landed TIP-88A policy catalog. 16 files, +2524/-23. Landed 88A migration (`20260711132410`) byte-unchanged.

**Field.** `RawExportPolicyVersion` gained a trailing `int? PermitTtlSeconds` ([`RawExportPolicyCatalog.cs:83`](src/TagEkyc.Domain/RawExportPolicyCatalog.cs:83)); `RawExportPolicyVersionRow` + `AddRawExportPolicyVersionCommand` extended to match. Nullable `PermitTtlSeconds integer` column on `raw_export_policy_versions` (new migration `20260721070118_Tip88AE2PolicyPermitTtlField`). The immutable-catalog append-only trigger already prevents in-place update — changing a TTL requires a new policy version.

**Constraint (raw-SQL, NOT VALID).** `CK_raw_export_policy_versions_PermitTtlSeconds CHECK ("PermitTtlSeconds" IS NOT NULL AND "PermitTtlSeconds" > 0) NOT VALID` — pre-E2 rows stay NULL, every post-E2 INSERT/UPDATE is checked, no backfill. **Not EF-modeled** (DbContext/Designer/ModelSnapshot map only the nullable column; the CHECK is absent from the EF model, so a future model diff will not recreate a validated check).

**Closure-completeness trigger (the one authorized 88A change).** `CREATE OR REPLACE` of the CatalogApproved closure-completeness function adds a single block: a new `CatalogApproved` closure additionally requires the target version's stored `PermitTtlSeconds` non-null AND > 0, else bare `RAISE EXCEPTION 'RAW_EXPORT_POLICY_PERMIT_TTL_INVALID'` (SQLSTATE `P0001`). Verified strict-superset: every other branch/error surface unchanged; `Abandoned` untouched. `Down()` restores the byte-identical pre-E2 function body, then drops the constraint, then the column. Structural only — the trigger does NOT enforce config bounds.

**Bounds (app config, one immutable resolver).** `RawExportPermitTtlOptions` ([`RawExport/RawExportPermitTtlOptions.cs`](src/TagEkyc.Infrastructure/RawExport/RawExportPermitTtlOptions.cs)): `SectionName = "TagEkyc:RawExport"`, keys `PermitTtlMinSeconds` / `PermitTtlMaxSeconds`, `AbsoluteMaxSeconds = 3600`. Resolved ONCE at startup into an immutable `Valid(Min,Max)|Invalid` singleton (no `IOptionsMonitor`, no hot reload, single `IConfiguration` read). Both absent => `[60, 900]`; exactly one absent / malformed / overflow / `min<=0` / `max<min` / `max>3600` => `Invalid`.

**Validation + readiness.** `AddVersionAsync` rejects out-of-bounds before appending; `CatalogApproveAsync` re-validates the stored TTL against the injected resolved-bounds state (no config reload) and appends no closure on failure; `AbandonDraftAsync` does not validate TTL. App-side code `RAW_EXPORT_POLICY_PERMIT_TTL_INVALID`. `/readiness` returns 503 `PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID` on an `Invalid` bounds state — config-only, sanitized (no raw values / binder text), no policy-row scan, no startup fail-fast.

## Verify record (on-code, adversarial)

Two independent adversarial reviewers over the as-built code + CC scope checks confirmed **production/schema code CLEAN** on the highest-risk surfaces: the trigger `CREATE OR REPLACE` is a strict superset (normalized diff = the sole addition is the TTL block, gated to CatalogApproved, bare P0001); `Down()` restores a byte-identical (normalized) pre-E2 function body+metadata in the correct order; the CHECK is raw-SQL-owned and provably absent from the EF model; the bounds resolver is a single immutable singleton; validation paths and readiness wiring are correct. Full suite: Contract 13/13, Arch 48/48, Unit 180/180, Integration 203 passed / 1 skipped.

**Mid-build RRI (Codex STOPPed correctly):** the `NOT VALID` CHECK correctly rejected two landed test seed helpers (B1/B2) that create a policy version via raw SQL without a TTL -> 30 integration failures. Root cause was a census gap in the brief: the consumer inventory grepped the TYPE names, missing raw-SQL `INSERT INTO raw_export_policy_versions` seed helpers that bypass the domain type. Resolved by a Homeowner-authorized FIXTURE-ONLY allowlist expansion (2 test files: add a valid in-bounds TTL to their seeds; no B1/B2 production or semantic change). Verified byte-level as seed-only.

**Two §5 test-fidelity gaps found by verify-on-code and closed** (both test-only): the config-change scenario now performs a genuine service-provider A -> dispose -> B rebuild (not a constructor bounds-swap); and a legacy pre-E2 `CatalogApproved` NULL-TTL row is now asserted to remain readable with `PermitTtlSeconds == null` (distinct policy id, no closure-PK reuse).

## Legacy disposition (operational)
- Existing `CatalogApproved` rows with NULL `PermitTtlSeconds` (created before E2) REMAIN READABLE; TIP-88B3 will REJECT them at authorization — publish a NEW policy version with a valid TTL before raw-export use.
- Legacy `Draft` rows with NULL TTL CANNOT be CatalogApproved (closure-completeness rejects) — abandon and create a new version.
- NO backfill, NO in-place mutation.

## Disposition
- **TIP-88B3 gate-3 (policy-TTL + bounds contract): CLOSED** at `09c9359` (as-built shape reconfirmed above). B3 binds the exact selected version's `PermitTtlSeconds` + the E2 bounds resolver, computes `PermitExpiresAtUtc = min(transaction_timestamp() + PolicyPermitTtlSeconds, finite consent expiry, finite fulfillment expiry)`.
- TIP-88B3 remains NOT READY_FOR_BUILD: gate-1 CLOSED, gate-2 CLOSED, gate-3 CLOSED (this slice), **gate-4 (rewrite the brief from the landed HEAD) still OPEN.**
- No new debt beyond the existing raw-export P1/P2/P3; the census-gap lesson is recorded in the brief §7.
- Push pending Homeowner authorization.
