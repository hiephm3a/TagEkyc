# Patch Charter — Persistence Slice Tier-1 Fixes v0.1

**File:** `docs/slice_persistence_patch_v0_1.md`
**Version:** 0.1
**Status:** DONE — patch executed (152/152). Focused Tier-1 re-verify (2026-06-21) = both fixes CONFIRMED-SAFE; arch-test changes investigated and VERIFIED LEGITIMATE (EF-boundary governance relocated to a stronger `PersistenceBoundaryTests`, not weakened). One open item before the persistence slice is closed in history: commit the slice + patch + governance docs as a clean scope-floored commit (allowlist; exclude pre-existing known-dirty `00_AGENT_COORDINATION_BUS.md`/`00_GDRIVE_FILE_INDEX.md`). Code ACCEPTED.
**Date:** 2026-06-21
**Baseline:** `slice_persistence_charter_v0_1.md` v0.2 (EXECUTED, 149/149); Tier-1 adversarial review verdict ACCEPT-WITH-FIXES.
**Purpose:** Resolve the exactly two SHOULD-FIX findings on the finalization (evidence-bearing) surface before the persistence slice is finally accepted. This is a tight patch, not a new slice.

---

## Changelog

### v0.1 — Initial patch charter
- Authored after the Tier-1 adversarial review of the persistence slice. Encodes the two SHOULD-FIX fixes, their tests, the scope floor, and the re-review requirement.

---

## 1. Trigger / precondition

Tier-1 review of the persistence slice = ACCEPT-WITH-FIXES. Core durability guarantees CONFIRMED-SAFE; two SHOULD-FIX items remain, both on the legally-weighty finalization surface. This patch closes them, then the slice is accepted.

## 2. Scope floor (anti-creep)

**Exactly the two fixes below + their tests. Nothing else.** No NIT fixes (ResultRef type asymmetry, PersistenceJson Web defaults — both deferred), no refactor, no new feature, no API/DTO/business change. A patch that touches anything outside Fix 1 / Fix 2 and their tests is a **defect** → STOP/RRI.

## 3. Fix 1 — Move `EfPersistenceFaultInjector` out of the production DI graph

**Problem (verified):** `TagEkycPersistenceServiceCollectionExtensions.cs:19` registers `EfPersistenceFaultInjector` inside `AddTagEkycPostgresPersistence` (the production wiring, called from `Program.cs:78`). It exposes a public mutable `ThrowAfterSessionUpdateInFinalization` honoured on the finalization hot path (`EfVerificationFinalizationBoundary.cs:39`). A live abort-transaction switch must not exist in the prod container.

**Fix:**
- Remove the `services.AddScoped<EfPersistenceFaultInjector>()` registration from `AddTagEkycPostgresPersistence`.
- Register `EfPersistenceFaultInjector` **only in the integration-test composition** (the test fixture's service provider). The boundary already accepts `EfPersistenceFaultInjector? faultInjector = null`, so production resolves null and the switch is unreachable. No change to `EfVerificationFinalizationBoundary`'s constructor needed.
- (Optional hardening, allowed: `#if DEBUG` guard or moving the type to a test-only location — but the minimum is "not in the prod registration.")

**DoD 1:**
- Production DI graph no longer contains `EfPersistenceFaultInjector` (assert via a focused DI/ArchTest, or a test that resolving it from the prod composition fails).
- The existing fault-injection atomicity test still passes, now wiring the injector through the test composition.

## 4. Fix 2 — DB-enforced finalize concurrency (remove reliance on incidental PK collision)

**Problem (verified):** `EfVerificationFinalizationBoundary.cs` does a transactional read (`:17`) → snapshot compare (`:29-36`) → `ApplySession` (`:38`) → `SaveChangesAsync` (`:46`). The emitted session UPDATE is `WHERE Id = @id` only — no state predicate, no rowversion/`xmin`. Two concurrent finalizes both read `ReadyToComplete`, both pass the snapshot match; the second is currently blocked **only** because the deterministic-GUID decision/package/audit rows collide on PK and roll back. The no-double-commit guarantee is incidental, not designed.

**Fix:**
- Add an `xmin` concurrency token to `VerificationSessionRow` (Npgsql `UseXminAsConcurrencyToken()` in the entity configuration — `xmin` is a system column, no schema/migration column added). The session UPDATE then carries `WHERE xmin = @original`; a concurrent finalize raises `DbUpdateConcurrencyException`.
- Map `DbUpdateConcurrencyException` in `TryFinalizeAsync` to `VerificationFinalizationWriteStatus.StateMismatch` (return the conflict result, full rollback, zero partial rows). Acceptable alternative/addition: a `WHERE State = @expectedState` predicate on the session update.
- Keep the existing single-threaded stale-CAS test green.

**DoD 2:**
- A model-level assertion that `VerificationSessionRow` has a configured concurrency token (so the guarantee is by design, verifiable without a race).
- A **concurrent-finalize integration test**: two `TryFinalizeAsync` run in parallel on the same `ReadyToComplete` session → exactly **one** `Applied`, one conflict (`StateMismatch`), **zero** partial rows (decision/package/manifest/audit counts == one set), and the conflict is surfaced via the concurrency path (not a raw PK-collision exception).
- `DbUpdateConcurrencyException` is caught and mapped; it never escapes as an unhandled 500.

## 5. Combined DoD

- [ ] Fix 1 applied; fault injector absent from prod DI (test-proven).
- [ ] Fix 2 applied; `xmin` (or equivalent) concurrency token on session; exception mapped to `StateMismatch`.
- [ ] New concurrent-finalize integration test passes and proves DB-enforced conflict.
- [ ] Existing fault-injection + stale-CAS tests still pass.
- [ ] Full suite green (≥ 149 prior + new tests); no regression.
- [ ] No file changed outside Fix 1 / Fix 2 / their tests (scope floor).

## 6. Re-review (Tier-1, focused)

After patch, a focused Tier-1 re-verify (not a full re-review): confirm (a) the prod DI graph excludes the fault injector; (b) the concurrency token is present and the concurrent-finalize test genuinely exercises the DB guard (not the PK-collision path); (c) no scope creep; (d) suite green. Then the persistence slice is ACCEPTED.

## 7. Files expected to change

`src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs` (remove injector registration); `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` and/or the session entity config (xmin token); `src/TagEkyc.Infrastructure/Persistence/EfVerificationFinalizationBoundary.cs` (catch+map concurrency exception); `tests/TagEkyc.IntegrationTests/**` (test-only injector wiring + concurrent-finalize test); possibly a small ArchTest/DI test for Fix 1. **No** change to Domain, Application services, Contracts, API shapes, or the NIT items.

## 8. STOP/RRI

Stop and report if: the `xmin` token cannot be configured without a migration/schema change beyond a system-column mapping; mapping the concurrency exception forces an API/DTO change; or either fix cannot be done without touching files outside §7.
