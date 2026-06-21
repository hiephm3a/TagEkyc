# TIP-63 S1 LLD Runtime-Contract Consolidation — Closeout v0.1

**File:** `docs/tips/tip_63_s1_lld_runtime_contract_consolidation/tip_63_closeout_v0_1.md`
**Version:** 0.1
**Status:** Closed — docs-only consolidation accepted (Contractor adversarial spot-check ACCEPT)
**Date:** 2026-06-21
**Baseline:** `19666fb fix: add foreign-key constraints and orphan-reject tests to persistence schema` (accepted post-D-01 persistence HEAD).
**Purpose:** Close TIP-63 after consolidating the as-built S1 runtime contract into `lld_02` + `lld_03` (v0.1 sketch → v0.2 authoritative).

## Changelog

### v0.1 — Initial closeout
- Closed TIP-63 as docs-only D-02 slice 1. Recorded outcome vs intent, the multi-round review convergence under the new role split, the Contractor adversarial spot-check result, code-wins reconciliations, OPEN items, validation, lessons, and recommended next step.

## Status

TIP-63 is closed as a docs-only consolidation. Final disposition:

```text
READY_TO_COMMIT_TIP_63; D-02 SLICE 2 (lld_04 decision/canonicalization, Tier-2) IS THE NEXT CANDIDATE AFTER USER DISPATCH
```

## Outcome vs Intent

| Intended outcome | Actual result | Status | Notes / carry-forward |
| --- | --- | --- | --- |
| Move as-built S1 runtime contract from TIP-04/05/06/07 kickoffs into durable LLD | `lld_02` + `lld_03` refreshed v0.1 → v0.2 as authoritative as-built spec | Accepted | Future TIPs reference LLD, not kickoffs |
| Complete session state-transition table | `lld_02` §9 full table matching `VerificationSessionState` + service logic | Accepted | Honest "reserved / no public route sets" for Expired/Cancelled/TechnicalTerminal |
| Authoritative API contract (endpoints, DTOs, error→status, scope, sanitization) | `lld_03` §2–§6 complete | Accepted | Error registry verified against code |
| Fix C-1 (EventType) | `VERIFICATION_COMPLETED` pinned; stale literal kept as note only | Accepted | — |
| Fix C-5 (stale storage/vault/webhook wording) | Removed public webhook/retry route + raw vault flow claims | Accepted | S1 carries hashes/sanitized refs/ids |
| Stay docs-only, persistence-agnostic, no new design | Only `lld_02`/`lld_03` changed; §1 of each states no DB/FK/durability facts | Accepted | Scope floor held both directions |
| Defer lld_04 (Tier-2) | Untouched | Accepted | D-02 slice 2 |

## Decision / Branch Disposition

| Decision / option | Final disposition | Why |
| --- | --- | --- |
| Consolidate as-built only; code wins on conflict | Accepted | Docs describe reality; code is truth |
| RetryRequired handling (kickoff `409 CAPTURE_RETRY_REQUIRED` vs code) | Resolved to as-built: RetryRequired is an accepted result + a `RetryRequired` final result (no 409); `CAPTURE_RETRY_REQUIRED` absent from code | Code wins (verified by grep + `VerificationEvidenceApplicationService.cs:288`) |
| Specialized evidence routes | Deferred/OPEN (generic `/evidence-results` is as-built) | Code wins (TIP-09) |
| Completion notification | Application-service projection only, no public route | Code wins (TIP-07 Option A) |
| lld_04 consolidation | Deferred | Tier-2 (assuranceLevel mapping, manifest canonicalization) needs legal lens |

## Debt / Gap Final State

| Debt/gap | Final state | Resolved? | Next gate |
| --- | --- | --- | --- |
| D-02 stranded S1 spec | lld_02 + lld_03 consolidated | Partial | D-02 slice 2 = lld_04 |
| C-1 EventType | Pinned as-built | Yes | — |
| C-5 stale storage/webhook wording | Removed/corrected | Yes | — |
| lld_04 (decision model, canonicalization, adapter) | Untouched, deferred | No | Tier-2 slice with legal review |

## Exact Files Changed

| Path | Change |
| --- | --- |
| `docs/lld_02_sequence_flows_v0_1.md` | v0.1 → v0.2: source boundary, 6-route surface, create/capture/evidence/complete/package-read/completion-notification flows, full state-transition table, deferred boundaries |
| `docs/lld_03_api_contracts_v0_1.md` | v0.1 → v0.2: common rules, 6-endpoint inventory, DTO field tables, scope/caller catalog, error→status registry, sanitization (with clientApplicationId exception), OPEN/deferred table |
| `docs/tips/tip_63_.../tip_63_planning_brief_v0_1.md` | This TIP's planning brief (v0.2.3) |
| `docs/tips/tip_63_.../tip_63_kickoff_v0_1.md` | This TIP's kickoff (v0.2.3) |
| `docs/tips/tip_63_.../tip_63_closeout_v0_1.md` | This closeout |
| `docs/tips/README.md` | v1.12 changelog + TIP-63 index row |

TIP-63 scoped diff (lld files): 386 insertions, 560 deletions (net tightening of sketch → authoritative). No `src/**` or `tests/**` change.

## Code-Wins Reconciliations

- C-1: `VERIFICATION_COMPLETED` is the as-built event literal; `EKYC_VERIFICATION_COMPLETED` (TIP-06 §20) marked stale.
- C-5: removed public webhook/retry route and raw vault/storage flow claims.
- Specialized evidence endpoints → deferred; runtime is generic `/evidence-results`.
- Completion notification → projection only, no public route.
- RetryRequired → accepted result + `RetryRequired` final result (NOT a 409); `CAPTURE_RETRY_REQUIRED` does not exist in code.

## OPEN / Deferred (not promoted to authoritative)

Specialized evidence routes; public webhook callback/retry/delivery/outbox; production webhook signature/replay; production vault/storage/raw artifact lifecycle; `FraudRisk` recording (`FRAUD_RISK_DEFERRED`).

## Validation

Docs-only; no full test run (per kickoff §9). Ran `git diff --stat`, `git diff` of the two LLD files, and the `rg` sanitization/route scan; `rg` hits confirmed as intentional notes/boundary checks, not stale active-route claims. Scope confirmed: only `docs/lld_02_*` and `docs/lld_03_*` changed.

## Review Ladder Summary (new role split: Contractor drafts → GPT + Codex review/converge → Codex builds → Contractor verifies)

- Contractor draft v0.1.
- GPT/Codex review round 1: ACCEPT WITH PATCHES — P1-a `lld_00_README` phantom (no LLD status-table file exists), P1-b sanitization (`clientApplicationId` exception), P2 completion-notification projection-only → v0.2; residual `lld_00`/"3 files" sweep → v0.2.1.
- GPT/Codex review round 2: ACCEPT WITH PATCHES — P1 baseline drift (`6fd8243` → accepted `19666fb`) in brief/kickoff/README, P1 README index row, P2 read/preserve current LLD, P2 static-validation commands → v0.2.2.
- Contractor self-check: persistence-agnostic non-goal + README index-row gap → v0.2.3.
- GPT/Codex re-confirm: PASS — "build can start without human judgment calls."
- Codex docs-only build.
- Contractor adversarial spot-check: **ACCEPT** — verified the RetryRequired code-wins crux (grep), error-registry codes real (not invented), state table / precedence / assurance mapping match code, sanitization exception precise, persistence-agnostic honored, OPEN list honest, no drift / no new design.

Total: 2 external review rounds + 1 self-check + 1 re-confirm + 1 build + 1 adversarial verification.

## STOP/RRI Result

No STOP/RRI condition encountered during build. The one code↔kickoff discrepancy beyond C-1/C-5 (RetryRequired) was resolved by the code-wins rule and verified; the Contractor spot-check confirmed the LLD reflects code. (Process nit: the build report did not explicitly list the RetryRequired reconciliation under "discrepancies found"; the LLD is correct regardless.)

## Lessons Learned

- The review loop worked in both directions: GPT/Codex caught the Contractor's baseline drift (`6fd8243` not yet the accepted HEAD); the Contractor caught the reviewer-missed README index-row gap and the undisclosed RetryRequired reconciliation.
- For consolidation TIPs, the decisive adversarial check is "does the doc match the CODE" verified by grep/read — not trusting the build self-report. Verifying the RetryRequired crux (kickoff said 409; code does not) is the example.
- "code wins" must be applied even when a kickoff design was never implemented as designed; the LLD must follow the as-built, and the discrepancy should be disclosed.

## Recommended Next Step

Commit TIP-63 (lld_02 + lld_03 + TIP-63 docs + README; plus the pending `slice_persistence_charter` status governance edit, handled by allowlist). Then D-02 slice 2 (lld_04 decision/canonicalization/adapter, Tier-2) may be considered after user dispatch — it touches EBS Tier-2 surfaces (assuranceLevel mapping, manifest canonicalization) and warrants a legal/evidence lens.
