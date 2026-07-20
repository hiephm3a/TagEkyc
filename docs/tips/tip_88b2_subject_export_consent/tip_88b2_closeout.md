# TIP-88B2 Subject Export Consent - Closeout

Status: CLOSED — implementation landed and pushed at 8cd52a344e4cc443b9fee09f3be07b7d88d5b129; not merged into master as of 2026-07-20.
Raw-byte inertness: this slice RECORDS and RESOLVES subject consent only; it does NOT read, assemble, or return any raw biometric bytes, and does NOT authorize export, issue a permit, evaluate class coverage, or emit authorization denial causes (those are TIP-88B3 and the later assembly slice).
Spec: `tip_88b2_planning_brief.md` v1.0 (READY_FOR_BUILD, BUILD-LOCKED, after 9 review rounds across GPT + Codex + CC). Baseline: 97c7763.
Date: 2026-07-20

## Outcome vs Intent

| Intended outcome | Actual result | Status | Notes / carry-forward |
|---|---|---|---|
| Append-only subject-consent foundation | 3 append-only tables (events/classes/authorities), UPDATE/DELETE denied by trigger; xmin same-transaction child guard | ACHIEVED | Defence-in-depth triggers now mutation-tested |
| Dedicated recorder/withdrawer authority | Aggregate keyed `(AuthorityPrincipalId, ClientApplicationId, AuthorityType)`, `{SubjectConsentRecorder, SubjectConsentWithdrawer}` × `{Granted, Revoked}` | ACHIEVED | Grant/revoke deployment-only; runtime cannot self-grant |
| Full semantic `ConsentScopeHash` | SHA-256 over 6 length-prefixed typed fields, server/DB-computed; typed columns authoritative | ACHIEVED | Single-`bigint` advisory-lock mapping, golden-vector-pinned |
| Server-derived purpose/recipient | `PurposeCode` = Slice-1 constant; `RecipientClientApplicationId` = locked session owner; resolver takes only `(sessionId, policyId, policyVersion)` | ACHIEVED | No caller-influenceable scope field |
| Latest-event-overall / no-fallback | Applied to BOTH consent and authority aggregates; only a later `Granted` reactivates | ACHIEVED | Expiring authority silently halts consent capture — monitor `ValidUntilUtc` |
| Asymmetric Granted vs Withdrawn session gates | `Granted` requires `Completed`; `Withdrawn` requires session-exists only; no generic `Completed` trigger | ACHIEVED | Withdrawal survives Expired/Cancelled/TechnicalTerminal — do not add a deployment guard blocking it |
| Authorization-grade ambient-transaction resolver | Single SECURITY DEFINER fn; session row lock then consent-scope shared lock, both held to caller commit; throws without ambient txn | ACHIEVED | B3 consumes this landed 3-parameter resolver |
| Runtime least-privilege posture | Runtime EXECUTE on exactly 3 high-level fns; no direct DML on any B2 table; no UPDATE on `verification_sessions` | ACHIEVED | Enforced by `/readiness`; `has_table_privilege` pinned in tests |
| Consented-class storage without coverage evaluation | Classes stored + returned; NO subset/coverage check, NO requested-class derivation, NO denial causes | ACHIEVED | Coverage is TIP-88B3's |
| Raw-byte inertness | No raw read/assemble/return/hash path; only `bytea` is the 32-byte scope hash | ACHIEVED | Confirmed by inertness test |

## Decision / Branch Disposition

| Decision / option | Final disposition | Why | Follow-up debt / gate |
|---|---|---|---|
| Raw classes in scope identity | EXCLUDED from `ConsentScopeHash` / lock key | Classes are child content of a revision, not scope identity; keeps one consent scope per (session,subject,policy,purpose,recipient) | — |
| Recipient selection | SAME-OWNER only (`= session.ClientApplicationId`) | Slice-1 exports go to the session's own client | External delegation → future governance TIP |
| External recipient delegation | DEFERRED | Out of Slice-1 scope; needs a delegation/governance model | Future governance TIP |
| Purpose taxonomy | ONE Slice-1 constant `SubjectRawBiometricExport` | Single purpose for the hospital trial; server-derived | Multi-purpose → separate TIP |
| Authority administration | DEPLOYMENT-ONLY grant/revoke | Runtime must not be able to self-authorize consent capture | Bootstrap gate in runbook |
| Effective-state rule | LATEST-EVENT-OVERALL, no fallback | An expired/withdrawn latest event must not silently fall back to an older valid one | — |
| Withdrawal after terminal session | ALLOWED (Withdrawn not gated on `Completed`) | Otherwise consent is trapped non-revocable once the session leaves `Completed` | Runbook warns against a deployment-level guard |
| DecisionRef | NULLABLE, non-blank when present | Governance ref is optional; blank is meaningless | DB CHECK on both tables |
| Hash/lock path | SINGLE SQL source (server/DB-side) | Two live encoders would silently lock different keys | — |
| C# scope-hash/lock-key codec | RETAINED only as independent test oracle (NOT a live path) | Independent oracle for golden-vector parity | — |
| `RawExportConsentScopeCodec.CanonicalizeClasses` | REMAINS a runtime helper (class ordering/dedup), NOT a hashing path | It is on the write path but is not part of hash/lock computation | Do not mistake it for the hash codec |
| Authorization decision / permit | DEFERRED to TIP-88B3 | This slice is consent record/resolve only | B3 post-landing gates |
| Raw assembly / delivery / consumption | DEFERRED to the post-B3 assembly slice | INERT of raw bytes here | Assembly slice |

## Debt / Gap Final State

| Item | State |
|---|---|
| Aggregate cross-slice function manifest (namespace tripwire) | **P1 OPEN** — restore before hospital deploy |
| Trigger defence asymmetries (classes append-context guard; unproven current_user branch) | **P2 OPEN** |
| Resolver SHARED-vs-EXCLUSIVE lock-mode proof | **P3 OPEN** |
| TIP-88B3 post-landing gates (resolver/schema recon, B1 expiry projection, PermitTtl, rewrite-from-HEAD) | **OPEN** |
| B1 readiness function-manifest compatibility (would fail prod) | **RESOLVED** (exact whitelist) |
| Runtime resolver permission (could not run under `tagekyc_runtime`) | **RESOLVED** (high-level SECURITY DEFINER resolver) |
| Privileged direct-insert consent-scope-lock bypass | **RESOLVED** (scope exclusive lock before revision read) |
| Rollback ACL asymmetry (`Down()` revoked an ungranted privilege) | **RESOLVED** (stale revoke removed; snapshot expanded) |
| Non-discriminating lock/concurrency tests | **RESOLVED** (mutation-tested; `pg_locks` waiter assertions) |

## Final Authoritative Validation Evidence

Values from landed HEAD `8cd52a3` (git-derived facts verified in this pass; test counts as reported by the build at that HEAD — the round-4 final report — not independently re-executed in this docs-only pass).

| Item | Value |
|---|---|
| Implementation commit | `8cd52a344e4cc443b9fee09f3be07b7d88d5b129` |
| Baseline | `97c7763` |
| Changed files / stat | 19 files, +5310 / -69 |
| Full build | `dotnet build TagEkyc.sln` — PASS (0 warnings, 0 errors) |
| Contract tests | 13 / 13 pass |
| Arch tests | 46 / 46 pass |
| Unit tests | 180 / 180 pass |
| Integration tests | 180 pass, 1 skipped |
| Skipped test (FQN + reason) | `TagEkyc.IntegrationTests.Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors` — "Manual one-shot fixture generator; do not run during normal dotnet test." (pre-existing TIP-67G manual generator, unrelated to 88B2) |
| Targeted TIP-88B2 tests | 20 / 20 pass |
| Targeted TIP-88B1 tests | 16 / 16 pass |
| Runtime-role positive matrix | resolver + grant + withdraw succeed under `SET ROLE tagekyc_runtime`; session-transition and withdrawal block under the held locks |
| Privilege-negative matrix (42501) | 12/12 runtime table×verb DML negatives + EXECUTE negatives (authority-mgmt + bare helpers) assert `SqlState == 42501`; business-rule/trigger negatives keep their exact custom error text |
| Mutation-test guards (delete → a named test goes red) | append-only triggers ×3; xmin child guard; self-escalation raise; scope-hash raise; direct-insert consent-scope lock (`:524`); resolver consent-scope shared lock (`:285`); rollback `Down()` broad `REVOKE ALL` |
| Migration apply / rollback / reapply | PASS (B1 baseline → apply B2 → rollback → re-run B1 readiness → reapply B2 → B2 readiness) |
| ACL pre-B2 vs post-rollback equivalence | PASS — snapshot covers `verification_sessions` (runtime/deployer × SELECT/UPDATE) + `raw_export_policy_versions` (runtime/deployer × SELECT) + B1 function ACL; test fails under a broad `REVOKE ALL` |
| Designer vs ModelSnapshot | Three-way agreement (migration CHECKs, Designer, ModelSnapshot) verified on-code; no model drift in the docs pass |
| Golden / proof-bound diff | Empty — `docs/contracts/golden_neutral_proof_vectors.json`, TIP-67G/67B, Tip05/06/08 proof tests unchanged |

## What shipped (as-built)

Greenfield append-only subject-export consent over the landed 88A catalog and 88B1 control plane. 19 files, +5310/-69.

**Three append-only tables**, all UPDATE/DELETE-denied by trigger:
- `raw_export_subject_consent_events` - two events `{Granted, Withdrawn}`, monotonic contiguous `Revision` per semantic scope.
- `raw_export_subject_consent_classes` - immutable class children of a `Granted` revision, non-empty, written in the SAME transaction as the parent (xmin same-transaction guard, no later append). A `Withdrawn` carries no children (DB-enforced).
- `raw_export_subject_consent_authorities` - dedicated authority aggregate keyed `(AuthorityPrincipalId, ClientApplicationId, AuthorityType)` with `AuthorityType in {SubjectConsentRecorder, SubjectConsentWithdrawer}` and `EventType in {Granted, Revoked}`.

**Scope identity.** `ConsentScopeHash` = SHA-256 over namespace `"tagekyc:consent-scope:v1"` plus six 4-byte-big-endian length-prefixed typed fields: `VerificationSessionId`, authoritative `SubjectRef` (NFC UTF-8), `PolicyId`, `PolicyVersion` (8-byte BE), `PurposeCode`, `RecipientClientApplicationId`. The `ConsentScopeHash` and advisory-lock key are computed **server/DB-side only** on every live path; the C# `RawExportConsentScopeCodec` scope-hash / lock-key methods are retained purely as an independent golden-vector test oracle (they are NOT on any live path). `RawExportConsentScopeCodec.CanonicalizeClasses` DOES remain a runtime helper (class ordering/dedup on the write path) — it is not a hashing path. Typed columns remain authoritative, so a hash collision cannot merge two scopes. Advisory-lock key = the **first 8 bytes as a signed big-endian int64**, single-`bigint` `pg_advisory_xact_lock` - one pinned mapping, no `hashtext`, no alternative form. C#/SQL parity and the RFC-4122 big-endian Guid encoding are pinned by frozen golden vectors including a non-ASCII NFC case.

**Server-derived scope fields.** `PurposeCode` is the Slice-1 constant `SubjectRawBiometricExport`; `RecipientClientApplicationId` is read from the LOCKED `VerificationSession` row (same-owner only). Neither is caller-selectable, and the resolver takes only `(verificationSessionId, policyId, policyVersion)` - so no caller can influence any scope field.

**Asymmetric session gates (the load-bearing correctness decision).** Consent `Granted` requires `VerificationSession.State == Completed`. Consent `Withdrawn` requires ONLY that the session EXISTS - deliberately NOT `Completed` - so a withdrawal still succeeds after the session reaches `Expired` / `Cancelled` / `TechnicalTerminal`. Gating withdrawal on `Completed` would trap consent in a non-revocable state. There is **no generic DB-enforced `Completed` trigger**; the privileged/direct-insert enforcement is split per event type.

**Latest-event-overall, no fallback** - applied to BOTH the consent aggregate and the authority aggregate. An expired or withdrawn/revoked latest event never falls back to an older still-valid revision; only a later `Granted` reactivates. `TargetRevision` is NOT NULL on `Withdrawn`/`Revoked`, must equal the current effective `Granted` revision under the aggregate lock, and a stale target raises a stable conflict.

**Access model.** All writes go through SECURITY DEFINER functions owned by the non-login `tagekyc_raw_export_deployer` with a fixed `search_path=pg_catalog` and no unsafe dynamic SQL; PUBLIC EXECUTE revoked on all of them. `tagekyc_runtime` holds EXECUTE on exactly **three** high-level functions - the authorization resolver, append-granted, append-withdrawn - and has **no direct DML** on any of the three tables. Authority grant/revoke is deployment-only; runtime cannot self-grant (explicit self-escalation guard). Actor identity comes from `SET LOCAL tagekyc.actor_principal_id`, never a request body.

**Authorization-grade resolver.** `raw_export_resolve_subject_consent_for_authorization(uuid, uuid, integer)` - a single SECURITY DEFINER function that, inside the CALLER's ambient transaction, locks the session row, loads the authoritative `SubjectRef`/owner, derives purpose + recipient, computes the scope hash, takes the consent-scope SHARED advisory lock, selects the latest event overall, evaluates missing/withdrawn/expired/effective, and returns the typed snapshot plus consented classes. **Both locks are held until the caller commits/rolls back.** It emits a FACTUAL consent-state cause only (`Missing`/`Withdrawn`/`Expired`) - never an authorization denial cause.

**Provenance.** `ConsentTextVersion` + `ConsentTextContentHash` + `ExternalConsentArtifactRef` are mandatory for `Granted` and are classified **trusted-recorder assertions**, not server verification - no server-proof-of-shown-text is claimed absent a consent-text registry. No artifact bytes are stored anywhere (the only `bytea` is the 32-byte scope hash, length-constrained). `DecisionRef` is OPTIONAL per the brief: NULL when no governance artifact exists, non-empty preserved verbatim, blank/whitespace rejected by DB CHECK on both tables.

## Adversarial verify record (on-code, across FOUR rounds)

Every round began with the builder reporting a clean build and a fully green suite (400+ tests). Every round still found defects. This record exists because "tests pass" was never once sufficient on this slice.

**Round 1** - two P1s and a design risk:
- The landed **88B1 readiness validator would have failed permanently in production**. Its function-manifest query used a broad `raw_export_%` predicate with strict set-equality against exactly 10 expected functions; the as-built 88B2 adds 8 functions matching that prefix (the 3 `enforce_raw_export_subject_consent_*` triggers do not match), so a correctly provisioned deployment would read 18 vs 10 and throw `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`. No test reached that branch because it is the last step of `ValidateAsync` and every test threw at an earlier gate. The first patch attempt narrowed the B1 *test* instead of fixing the *validator*, which would have made the defect permanently undetectable while production still failed.
- **STOP violation:** `GRANT SELECT ON tagekyc.raw_export_policy_versions` (an 88A table) was extended to `tagekyc_runtime`, widening the ACL of a landed slice. The read happens inside a SECURITY DEFINER function, so the grant was unnecessary as well as out of scope.
- `ConsentScopeHash` was being computed in two live implementations (C# in the resolver, SQL in the append functions); a divergence would have made the resolver lock a different key than the writer, silently destroying mutual exclusion.

**Round 2** - the production-role defect:
- **The resolver could not run under the production role at all.** The repository issued a bare `SELECT ... FOR UPDATE` on `verification_sessions` as the caller; PostgreSQL requires `UPDATE` privilege for row-locking SELECT, and `tagekyc_runtime` has only `SELECT`. Under `SET ROLE tagekyc_runtime` it fails 42501. It stayed green because no positive test ran the happy path as the runtime role. Fixed by collapsing the whole resolve sequence into one high-level SECURITY DEFINER function, which also returned hash computation to a single SQL source.
- **Privileged direct-insert bypassed the consent-scope lock**: the enforcement trigger took the authority lock, then read `ORDER BY Revision DESC` and validated contiguity/TargetRevision without ever taking the consent-scope lock. The realistic failure was not two inserts racing (UNIQUE catches that) but a direct insert committing a `Withdrawn` while the resolver held only the shared lock and read the consent as Effective - the exact stale-authorized outcome this spine exists to prevent.

**Round 3** - rollback and test-integrity defects:
- `Down()` revoked a privilege `Up()` no longer granted, so a rollback could strip an out-of-band grant (this repo does grant out-of-band).
- The rollback ACL snapshot omitted `verification_sessions` - precisely the table `Down()` touches most - so a broad `REVOKE ALL` would have passed unnoticed.
- **Every concurrency test was non-discriminating**: the blocking transaction already held the session row lock, so deleting the consent-scope lock left them all green.
- **The entire defence-in-depth trigger layer had ZERO coverage**, because the ACL check fires before row triggers. Dropping the three append-only triggers would have failed no test.

**Round 4** - the last gap:
- The **resolver's** shared consent-scope lock still had no regression net: deleting it left all 19 tests green, because the row lock masked it and the new round-3 test never invoked the resolver.

**Current state of the safety net.** Locks, guards and triggers are now mutation-tested: deleting the append-only triggers, the xmin child guard, the self-escalation raise, the scope-hash raise, the direct-insert scope lock, or the resolver scope lock each turns a specific named test red, and restoring source returns the suite to green. Blocking is asserted via `pg_locks` (`granted = false` against the exact canonical key) rather than by timing alone.

**Evidence basis.** The authoritative evidence for this closeout is the landed source at HEAD `8cd52a3` plus the mutation-test results above, verified on-code. No review-packet ZIP SHA is cited as final evidence: any packet produced during the earlier rounds predates the round-4 HEAD and is superseded — regenerate from `8cd52a3` if a packet artifact is needed, rather than relying on a stale one.

**Root cause of the recurring pattern.** Each patch round closed the items it was given but introduced or left defects elsewhere, until round 3. The common cause was that the happy path only ever executed as the table OWNER and never as the production role. The mandatory `SET ROLE tagekyc_runtime` positive matrix is what finally stopped the cycle, and it is the single most transferable gate for TIP-88B3.

## Attribution - what belongs to which slice

Two files of the landed TIP-88B1 were changed under an explicit Homeowner-authorized scope expansion. They are NOT equivalent in origin:

- **`RawExportControlPlaneReadinessValidator` function manifest (broad predicate -> exact `oid::regprocedure` whitelist): CAUSED BY TIP-88B2.** 88B2's new functions are what break the broad query, so fixing it is 88B2's responsibility. The whitelist also means future slices (TIP-88B3) cannot break the B1 gate the same way.
- **`search_path` validation (`Contains` -> exact equality) in BOTH validators: a PRE-EXISTING TIP-88B1 DEFECT, not caused by TIP-88B2.** `Contains("search_path=pg_catalog")` accepted `search_path=pg_catalog,public`, and `public` in a SECURITY DEFINER `search_path` is a classic privilege-escalation path. 88B2 had copied the broken pattern. It was fixed here opportunistically because the file was already open for the item above. **Do not attribute this defect to TIP-88B2 in any later analysis.**

No other 88A or 88B1 file changed. B1 grant, lifecycle, fulfillment and resolver semantics are untouched.

## Deployment gates (must be met before hospital-trial deploy)

Recorded in `docs/phase1_scope_and_debt_registry_v0_1.md` and the runbook section "Raw-Export Subject-Consent Role & Bootstrap Gate (TIP-88B2)":
- The app must connect as a LOGIN principal holding the `tagekyc_runtime` capability (NOT deployer/bootstrapper). Runtime needs EXECUTE on exactly the three high-level consent functions and must hold **no** INSERT/UPDATE/DELETE/TRUNCATE on the three consent tables.
- Consent recorder/withdrawer authorities must be bootstrap-seeded by the DEPLOYMENT role before any consent write; runtime cannot grant them.
- `/readiness` fails closed on ACL / owner / `search_path` / `prosecdef` / function-signature drift for the B2 function manifest.

## Debt carried forward

Canonical debt state is the **Debt / Gap Final State** table above (P1 aggregate cross-slice manifest / P2 trigger defence asymmetries / P3 resolver lock-mode proof, all OPEN; the five round-1..4 defects RESOLVED). Full P1/P2/P3 detail lives in `docs/phase1_scope_and_debt_registry_v0_1.md`.

## What TIP-88B3 inherits

TIP-88B3 (Raw Export Authorization Decision + Permit) is **no longer blocked on TIP-88B2 landing**, but it is NOT yet build-lockable. Its four post-landing gates stand:
1. Recon the LANDED TIP-88B2 resolver + schema (actual signature, return shape, `ConsentScopeHash` encoding, lock contract, object names) and bind to what shipped, not to the brief text.
2. Recon the landed 88B1 fulfillment-expiry projection; if `ValidUntilUtc` is absent from the resolver return, land the ADDITIVE B1 projection field first. B3 must not re-query B1 fulfillment outside the resolver.
3. Pin `PermitTtl`: config source, min/max bounds, production readiness behaviour when unset/out-of-range, DB transaction-time semantics.
4. REWRITE `tip_88b3_planning_brief.md` from the landed HEAD, then route to adversarial review and Homeowner READY_FOR_BUILD.

Carry the `SET ROLE tagekyc_runtime` positive-matrix gate and the mutation-test discipline (delete the guard, prove a named test goes red) into TIP-88B3 from the start.
