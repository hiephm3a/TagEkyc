# TIP-63 S1 LLD Runtime-Contract Consolidation — Planning Brief v0.1

**File:** `docs/tips/tip_63_s1_lld_runtime_contract_consolidation/tip_63_planning_brief_v0_1.md`
**Version:** 0.2.3
**Status:** Draft — docs-only consolidation planning; GPT/Codex review patches + Contractor self-check applied; pending convergence before dispatch
**Date:** 2026-06-21
**Baseline:** `19666fb fix: add foreign-key constraints and orphan-reject tests to persistence schema` (master, accepted post-D-01 persistence HEAD). Persistence accepted = slice `8454fbd` + FK/orphan patch `19666fb`, docs `6fd8243`; validation 154/154.
**Purpose:** Consolidate the as-built S1 runtime contract — currently stranded in the TIP-04/05/06/07 kickoffs — into the durable LLD (`lld_02`, `lld_03`), refreshing them from sketch v0.1 to authoritative v0.2, so future TIPs reference the LLD instead of re-deriving from kickoffs. This is D-02 slice 1.

---

## Changelog

### v0.2.3 — Contractor self-check (2026-06-21)
- Added a persistence-agnostic non-goal: do not import FK/durability/append-only facts into lld_02/03 (those are lld_01/infrastructure; the `19666fb` baseline is for as-built API/flow accuracy only). Pre-empts a misread from the baseline-rebase rationale.

### v0.2.2 — Baseline rebase after persistence FK patch (2026-06-21)
- P1: rebased baseline `6fd8243` → `19666fb` (the accepted persistence HEAD) in metadata + §0; recorded persistence accepted = slice `8454fbd` + FK/orphan patch `19666fb`, docs `6fd8243`, 154/154. Ensures the "as-built" consolidation captures FK/orphan/durable facts.
- P2: (kickoff) require reading current `lld_02`/`lld_03` in full and preserving still-correct content before editing; added static-validation grep/diff commands.

### v0.2.1 — P1-a sweep completion (2026-06-21)
- Removed residual `lld_00` / "3 files" references that the v0.2 patch missed (Expected Outcome line + STOP/RRI). Brief now consistently states `lld_02` + `lld_03` only. (Found by grepping all occurrences, not by eye.)

### v0.2 — GPT/Codex review patches (2026-06-21)
- P1-a: dropped the non-existent `lld_00_README.md` from scope (no LLD status-table file exists in TagEkyc; version tracked per-LLD-file). Scope is now `lld_02` + `lld_03` only.
- P1-b: corrected sanitization — `clientApplicationId` exposure is allowed in the as-built completion-notification projection; only default summaries omit it.
- P2: require lld_02/03 to mark completion notification as projection-only (no public route).

### v0.1 — Initial planning brief
- Opened TIP-63 as docs-only consolidation of the S1 sequence-flow + API-contract spec into `lld_02` + `lld_03`. Recorded scope, source-of-truth precedence (as-built code wins), Intent Ledger, non-goals, and STOP/RRI. No code/test/lld_04 change authorized.

## 0. Repo Evidence

| Evidence | Finding |
|---|---|
| Branch / HEAD | `master` / `19666fb` |
| Persistence slice | Accepted after `8454fbd` (slice) + `19666fb` (FK/orphan patch), docs `6fd8243`; 154/154; runtime durable behind ports incl. FK constraints + append-only triggers + xmin |
| `lld_02_sequence_flows_v0_1.md` / `lld_03_api_contracts_v0_1.md` | Still v0.1 sketch; NOT refreshed by TIP-49 (which patched only `hld` + `lld_01` for ART governance rules) |
| As-built S1 runtime | Implemented across `VerificationSessionApplicationService`, `VerificationEvidenceApplicationService`, `VerificationCompletionApplicationService`, `VerificationSessionEndpoints`, plus `Contracts/**` |
| Decided spec source | TIP-04/05/06/07 kickoffs (state machine, transitions, decision precedence, error→status, DTOs, scopes), TIP-08, TIP-09 closeout |
| Known live contradictions | C-1 EventType (`EKYC_VERIFICATION_COMPLETED` in TIP-06 §20 vs as-built `VERIFICATION_COMPLETED`); C-5 stale storage/vault wording in lld_02/03 (per `decision_register_v0_1.md` §4) |

## 1. Status / Purpose / Authorization basis

TIP-63 is **docs-only**. It refreshes two existing LLD files in place to reflect AS-BUILT reality. It does not change code, tests, behavior, public API/DTO shapes, or any other doc. Authorization basis: `decision_register_v0_1.md` D-02; precedent TIP-49 (docs-only HLD/LLD patch). Follows the Autonomous Slice Review Ladder from `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`, plus the new role split: Contractor drafts → GPT + Codex review/converge → Codex builds.

## 2. TIP Analytical Summary / Intent Ledger

### Intent
Move the decided, built S1 runtime contract from throwaway kickoff docs into the durable LLD as single source of truth.

### Expected Outcome
After TIP-63: `lld_02` carries a complete session state-transition table + refreshed sequence flows; `lld_03` carries the authoritative endpoint inventory, request/response DTO shapes, a single error-code→HTTP-status registry, a scope→endpoint catalog, and sanitization rules — all traceable to code or a decided TIP. C-1 fixed.

### Accepted Decisions
| Decision | Why | Scope impact | Non-claims |
|---|---|---|---|
| Consolidate AS-BUILT only | Docs must describe reality, not aspiration | lld_02 + lld_03 refreshed to v0.2 | Not a new design; not forward/Phase-2 |
| Code wins over kickoff on conflict | Kickoffs are pre-build intent; code is truth | C-1 resolved to `VERIFICATION_COMPLETED` | No behavior change |
| Defer lld_04 (decision/canonicalization/adapter) | Tier-2 (legal/evidence) — separate slice with legal lens | lld_04 untouched | No assurance/canonicalization rule restated here |

### Rejected / Deferred Branches
| Branch | Disposition | Why | Follow-up |
|---|---|---|---|
| Consolidate lld_04 now | Deferred | EBS Tier-2 (assuranceLevel mapping, manifest canonicalization) needs legal review | D-02 slice 2 |
| Rewrite/rename LLD files to v0.2 filenames | Rejected | Project freezes filenames, bumps internal version | Edit in place |
| Consolidate undecided/unbuilt kickoff rules | Rejected | Would promote non-built intent to authoritative | Flag as OPEN, do not consolidate |

### Debt / Gap Impact
| Debt/gap | Action | Result | Carry-forward |
|---|---|---|---|
| D-02 (stranded S1 spec) | Partially resolved (lld_02/03) | lld_04 still stranded | D-02 slice 2 |
| C-1 EventType contradiction | Resolved | as-built pinned | — |
| C-5 stale storage wording | Resolved in lld_02/03 | — | — |

### Non-Claims
TIP-63 does not: introduce new design; change code/tests/behavior/public API/DTO; consolidate lld_04/lld_01/hld; promote any unbuilt rule to authoritative; make readiness/legal/security/production claims.

### Dispatch Readiness
- Implementation dispatch allowed? **Not yet** — pending GPT + Codex review of brief + kickoff.
- Files that may change at build: `docs/lld_02_sequence_flows_v0_1.md`, `docs/lld_03_api_contracts_v0_1.md` only.
- STOP/RRI gates: see §8 + kickoff §8.

## 3. Problem being solved

The buildable S1 spec (state machine, transitions, decision precedence, error→status, DTO shapes, scopes) lives in the TIP-04/05/06/07 kickoffs (~5k lines), not in the LLD. Every future TIP re-derives context from kickoffs; the durable LLD is a stale v0.1 sketch. This is the upstream cause of per-TIP re-specification. Consolidating the as-built contract into the LLD lets future TIPs be thin deltas.

## 4. Scope (this slice)
- `lld_02_sequence_flows_v0_1.md` → v0.2: refreshed sequence flows + a complete session state-transition table; completion notification marked projection-only (no public route).
- `lld_03_api_contracts_v0_1.md` → v0.2: endpoint inventory (6 public routes), request/response DTO shapes, error-code→HTTP-status registry, scope→endpoint catalog, sanitization rules (with the as-built completion-notification `clientApplicationId` exception).
- (No LLD status-table file exists; version bumps live in each LLD file's own metadata. `docs/00_README.md` is not touched.)

## 5. Non-goals
lld_04 (deferred, Tier-2); lld_01/hld (already patched); any src/tests change; any behavior/API/DTO change; any new design or forward/ART/Phase-2 content; importing persistence / FK / durability / append-only facts into lld_02/03 (those are lld_01 / infrastructure concerns — lld_02/03 are persistence-agnostic; the `19666fb` baseline is for as-built API/flow accuracy only).

## 6. Source-of-truth precedence
1. AS-BUILT code wins absolutely (file list in kickoff §2).
2. Then decided TIP-04/05/06/07/08/09 kickoffs.
3. On conflict, code wins + a one-line note.

## 7. Acceptance criteria (DoD summary)
Full DoD in kickoff §6. Summary: complete state-transition table matching `VerificationSessionState` + service logic; authoritative lld_03 sections (endpoints, DTOs, error→status registry, scope catalog, sanitization); every statement traceable to code or a TIP; C-1 fixed; no statement contradicts code; per-LLD-file Version + changelogs updated; docs-only (lld_02 + lld_03 only).

## 8. STOP/RRI
- A kickoff rule with no corresponding code → flag OPEN, do NOT consolidate as authoritative.
- A code↔TIP contradiction beyond C-1/C-5 → STOP and report, do not silently choose.
- Any need to touch a file outside lld_02/03 → STOP.

## 9. Recommended next action
Submit this brief + the TIP-63 kickoff for GPT + Codex review. Converge (Dispatch Readiness reviewer must confirm "build can start without judgment calls"). Then dispatch to Codex for build. Do not build inside this brief.
