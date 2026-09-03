# TIP-88B1-E1 Fulfillment Expiry Projection - Closeout

Status: CLOSED — implementation landed at `ccc2e37` (parent `5169cd3`); not merged into master as of 2026-07-20. Committed local, push pending Homeowner authorization.
Spec: `tip_88b1e1_planning_brief.md` v1.0 (READY_FOR_BUILD, after GPT+Codex review + 2 patch rounds). Baseline: `5169cd3`.
Date: 2026-07-20

## What shipped (as-built, reconfirmed on-code @ ccc2e37)

Additive extension to the landed TIP-88B1 eligibility resolver. Two-line production diff:
- `RawExportFulfillmentRef` ([`RawExportControlPlane.cs:71`](src/TagEkyc.Domain/RawExportControlPlane.cs:71)) gains a **trailing** `DateTimeOffset? ValidUntilUtc` (6th positional field). Accessibility unchanged (`public sealed record`).
- The resolver ([`EfRawExportControlPlaneRepository.cs:147`](src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:147)) populates it from the `latest.ValidUntilUtc` already read at `:141` for the missing/expired decision — no new query, no re-read, same evaluation transaction.

**Actual field shape for downstream (TIP-88B3) binding:** `RawExportFulfillmentRef(RawExportRequirementType RequirementType, Guid FulfillmentEventId, int Revision, string ArtifactRef, string ArtifactVersion, DateTimeOffset? ValidUntilUtc)`. `ValidUntilUtc` is the current effective `Accepted` fulfillment's persisted Postgres `timestamptz` instant, or NULL when that fulfillment has no expiry.

## Verify record (on-code)

The "no eligibility-semantic change" invariant is proven **structurally**, not merely by green tests: the entire production diff is the two lines above; the eligibility decision computation (`State`/`PrimaryCause`/ordered `Causes[]`, the missing-or-expired predicate, lock order, ambient-txn) is byte-untouched. Adding a projected already-read value to the returned ref cannot alter the decision.

CC verify-on-code confirmed: scope = exactly the 4 allowlist files (migration/Designer/ModelSnapshot untouched by grep); no `DateTimeOffset.MaxValue`, no application `UtcNow` on the production path; accessibility preserved; the no-fallback test uses a bounded-timeout poll on DB `transaction_timestamp() >= expiry` (not `Task.Delay` as the assertion basis) with state-based assertions, CI-stable; the projection test compares the NORMALIZED persisted instant; the ArchTest proves `RawExportFulfillmentRef` is not HTTP/controller/DTO-bound and asserts it stays `public`.

Tests: Arch 47/47 (+1), Contract 13/13, Unit 180/180, Integration 183 passed / 1 skipped (+3), full build 0 warnings. First slice this raw-export-spine cycle to pass verify-on-code on the first build round — attributable to the tightly-locked additive scope.

## Files changed (allowlist, exact)
- `src/TagEkyc.Domain/RawExportControlPlane.cs`
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs`
- `tests/TagEkyc.ArchTests/Tip88B1ControlPlaneBoundaryTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs`

## Disposition
- **TIP-88B3 gate-2 (B1 fulfillment-expiry projection): CLOSED** at `ccc2e37`. B3's expiry formula binds `RawExportFulfillmentRef.ValidUntilUtc` (per-ref) + the landed B2 consent `ValidUntilUtc`; B3 derives `EarliestFulfillmentValidUntilUtc` = min of the non-null per-ref values (NULL = no fulfillment-derived bound), and must NOT re-query B1 fulfillment outside the resolver.
- No new debt. No P1/P2/P3 raw-export debt row touched.
- TIP-88B3 remains NOT READY_FOR_BUILD: gate-1 CLOSED, gate-2 CLOSED (this slice), **gates 3 (pin PermitTtl) and 4 (rewrite brief from landed HEAD) still OPEN.**
