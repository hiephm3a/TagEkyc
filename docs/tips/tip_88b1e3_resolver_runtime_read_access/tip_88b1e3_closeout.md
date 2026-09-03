# TIP-88B1-E3 Resolver Runtime Read Boundary — Closeout

**Status:** CLOSED

**Closeout date:** 2026-07-25

**Implementation commit:** `0a0eae82ac046450479e71f3c3dc8b215ba2fb48`

**Closeout commit:** `SELF`

## Scope delivered

TIP-88B1-E3 closes the resolver runtime read-boundary slice with:

- two typed `SECURITY DEFINER` authorization projections;
- one bounded root-health function;
- runtime zero-of-seven direct table privileges on all fourteen protected tables;
- a dedicated PostgreSQL 16 runtime `LOGIN` posture;
- exact transitive role closure and exact membership-edge options;
- exact function ACL validation, including grantor;
- an explicit deployer backing-read manifest;
- deterministic migration-time ACL enforcement;
- fail-closed materialization for fulfillment `Accepted` and `Withdrawn` shapes;
- B2 named-constraint mode normalization;
- a runtime-only B1 readiness test helper;
- migration apply/rollback/reapply equivalence.

## Security posture

- The application login inherits only `tagekyc_runtime`.
- Production readiness requires `session_user == current_user`; active `SET ROLE`
  is unsupported.
- PostgreSQL 16 is the minimum supported server version for the required
  membership-edge options.
- The actor GUC is provenance and anti-mixing evidence. It is not authentication
  against compromise of the full backend or database-owner credentials.
- The binding deployment topology remains one hospital per database.
- Runtime direct `SELECT` is not restored on any of the fourteen protected
  tables; reads cross only the reviewed capabilities.

## Final validation

The accepted final validation produced:

- full suite: 580 passed, 0 failed, 1 intentional manual-generator skip;
- architecture tests: 49/49 passed;
- contract tests: 13/13 passed;
- unit tests: 180/180 passed;
- integration tests: 338 passed and 1 intentional skip;
- full build: 0 warnings and 0 errors;
- EF pending-model gate: clean;
- `TagEkycDbContextModelSnapshot.cs` SHA-256 unchanged at
  `523E2823CACFB22091DDD01C3CC5D45241FC047121390A7A0647F2F645E708F6`;
- all mutation proofs red for the intended reason and restored afterward;
- no scratch role, default ACL, probe object, container, or mutation source
  remained.

No production tests were rerun for this docs-only closeout commit. The accepted
final validation above is reused unchanged.

## Mutation adequacy

The final adversarial review confirmed that:

- removing the empty `EventId` guard makes
  `R4_every_invalid_fulfillment_cell_fails_in_materialization_with_zero_authorization_residue`
  red;
- removing the blank `ArtifactRef` guard makes the same R4 test red;
- removing the B2 class-table `SELECT` restoration from the E3 migration
  `Down()` makes both
  `F6_migration_apply_rollback_reapply_restores_snapshot_functions_and_acls_exactly`
  and
  `G4_migration_apply_rollback_reapply_preserves_acl_equivalence`
  red;
- after restoration, R4, F6, and G4 are green.

## EOL note

The migration source whole-file hash changed because a mixed-EOL file was
normalized to CRLF during scratch restoration. Independent review verified the
semantic migration content, function bodies, exact ACLs, B2 `Down()` behavior,
catalog equivalence, and absence of mutation residue. The EOL-only hash change is
not an unresolved source mutation.

## Remaining debts and non-goals

- No multi-hospital shared database is introduced.
- No tenant model is introduced.
- The actor GUC is not claimed to authenticate against full backend compromise.
- No direct Raw BIO payload or raw-byte surface is added.
- No SignFlow code or contract is changed by this TIP.
