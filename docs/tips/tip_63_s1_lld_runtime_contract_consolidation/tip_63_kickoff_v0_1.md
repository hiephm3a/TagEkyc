# TIP-63 S1 LLD Runtime-Contract Consolidation — Kickoff v0.1

**File:** `docs/tips/tip_63_s1_lld_runtime_contract_consolidation/tip_63_kickoff_v0_1.md`
**Version:** 0.2.3
**Status:** Draft — dispatch-ready spec; GPT/Codex review patches + Contractor self-check applied; pending convergence confirmation before build
**Date:** 2026-06-21
**Baseline:** `19666fb` (master, accepted post-D-01 persistence HEAD = slice `8454fbd` + FK patch `19666fb`, docs `6fd8243`; 154/154). Planning brief: `tip_63_planning_brief_v0_1.md` v0.2.3.
**Purpose:** The exact execution contract for Codex to consolidate the as-built S1 runtime spec into `lld_02` + `lld_03`. Docs-only. No code/test/behavior change.

---

## Changelog

### v0.2.3 — Contractor self-check (2026-06-21)
- §4: added persistence-agnostic note — lld_02/03 must not import FK/durability/append-only/migration facts (those are lld_01/infrastructure); the `19666fb` baseline is for as-built API/flow accuracy only. Removes a possible scope-creep misread.

### v0.2.2 — Baseline rebase + P2 hardening (2026-06-21)
- P1: baseline `6fd8243` → `19666fb` (accepted persistence HEAD = slice `8454fbd` + FK patch `19666fb`, docs `6fd8243`, 154/154).
- P2: added §2(4) read-current-and-preserve rule (no blind rewrite); §9 static-validation `rg` grep (catch stale C-1 / sanitization exception / accidental webhook-route wording) + `git diff` of the two LLD files.

### v0.2.1 — P1-a sweep completion (2026-06-21)
- Removed residual `lld_00` / "3 files" / "3-file scope" references the v0.2 patch missed (scope floor §7, STOP/RRI §8, validation §9). Kickoff now consistently states the 2-file scope (`lld_02` + `lld_03`). Found by grepping all occurrences.

### v0.2 — GPT/Codex review patches (2026-06-21)
- P1-a: removed the non-existent `lld_00_README.md` from scope. TagEkyc has no dedicated LLD status-table file (`docs/00_README.md` is a version-less Document Index); version tracking lives in each LLD file's own metadata. Scope is now `lld_02` + `lld_03` only.
- P1-b: corrected the sanitization rule — `clientApplicationId` is NOT universally forbidden; the as-built completion-notification projection (`VerificationCompletedEventDto`) DOES expose it. Rule reworded to "default session/completion-response/package-summary responses MUST NOT expose it; completion-notification projection exposes it per as-built."
- P2: required lld_02/03 to mark the completion notification as projection / application-query only (no public route); only the 6 routes in `VerificationSessionEndpoints.cs` are public endpoints.

### v0.1 — Initial kickoff
- Authored dispatch-ready consolidation spec: source-of-truth precedence, exact scope, contradiction fixes, DoD checklist, scope floor, STOP/RRI, validation + report format.

## 1. Objective / precondition

Refresh `lld_02_sequence_flows_v0_1.md` and `lld_03_api_contracts_v0_1.md` (edit in place, bump internal Version → 0.2, do not rename) to authoritative descriptions of the AS-BUILT S1 runtime. **Precondition: GPT + Codex review converged and Homeowner dispatch.** Until then, do not build.

## 2. Source of truth + precedence (mandatory)

**(1) AS-BUILT code wins absolutely.** Read before writing any statement:
- `src/TagEkyc.Domain/VerificationSessionState.cs` + enums `VerificationResult`, `AssuranceLevel`, `RequiredCheckType`, `VerificationProfile`
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs` (create, validation, error codes, state init)
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` (capture/evidence recording, Created→InProgress→ReadyToComplete)
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` (completion, terminal transitions, EventType)
- `src/TagEkyc.Application/VerificationSessions/ApplicationAuthorization.cs`, `LocalDevClientPolicy.cs` (caller categories, scopes)
- `src/TagEkyc.Api/VerificationSessionEndpoints.cs` (routes, error envelope, status codes)
- `src/TagEkyc.Contracts/**` (request/response DTO shapes)

**(2) Then decided TIP kickoffs:** TIP-04/05/06/07, TIP-08, TIP-09 closeout.

**(3) On any code↔kickoff conflict, code wins** — write the as-built value and add a one-line `> note:` recording the superseded kickoff statement.

**(4) Preserve existing content (P2):** before editing, read `lld_02` and `lld_03` IN FULL; preserve any still-correct content; replace only stale / sketch / contradicting sections. Do not blind-rewrite.

## 3. Exact scope

### 3.1 `lld_02_sequence_flows_v0_1.md` → v0.2
- Refresh the S1 sequence flows (create → capture → evidence → complete → package → audit → completion-notification) to match the as-built services.
- **Add a session state-transition table:** every `VerificationSessionState` value; each legal transition with its trigger + guard; the terminal set; which write rejects in each terminal/expired state and with what error. Must match `VerificationSessionState.cs` and the service logic exactly.
- Note the audit event(s) emitted at each step (event-type names as emitted in code).
- **Completion notification (P2):** depict it explicitly as an **application-query / projection only — NO public HTTP route**. Only the 6 routes in `VerificationSessionEndpoints.cs` are public endpoints (TIP-09: no public webhook/notification route). Do not draw a notification/webhook endpoint in the LLD.

### 3.2 `lld_03_api_contracts_v0_1.md` → v0.2
- **Endpoint inventory:** the exact S1 routes from `VerificationSessionEndpoints.cs`.
- **Per-endpoint request/response DTO shapes** (field names from `Contracts/**`, not invented).
- **Error-code → HTTP-status registry:** ONE table consolidating TIP-04 §14 / TIP-05 §14 / TIP-06 §19, verified against `VerificationSessionEndpoints.cs` + the service failure codes. Each row: error code → status → endpoint(s).
- **Scope → endpoint catalog + caller category** (from `ApplicationAuthorization`/policy): which scope + caller category each endpoint requires.
- **Sanitization / data boundary (P1-b — match as-built exactly):** the default session summary, complete-response, and evidence-package-summary responses MUST NOT expose `PayloadHash`, `VaultRef`, raw artifacts, internal manifest, per-evidence detail, or `clientApplicationId`. **Exception:** the completion-notification projection (`VerificationCompletedEventDto`) DOES expose `clientApplicationId` (and `EventType` etc.) per as-built (`BusinessConsumerContracts.cs:86`, mapped in `VerificationCompletionApplicationService`, asserted by `Tip07CompletionNotificationApplicationTests`). State this exception explicitly rather than an absolute ban.

### 3.3 LLD status tracking
- TagEkyc has **no dedicated LLD status-table file** (`docs/00_README.md` is a version-less Document Index). Version tracking lives in each LLD file's own `**Version:**` metadata + Changelog. So the only version bumps are inside `lld_02` and `lld_03` themselves. **Do NOT touch `docs/00_README.md`** or create any `lld_00_README.md`.

## 4. Out of scope (do not touch)
`lld_04_*` (deferred Tier-2), `lld_01_*`, `tagekyc_hld_*`, `docs/00_README.md` and all other `docs/00_*`, `src/**`, `tests/**`, any other doc. No new design, no forward/ART/Phase-2 content, no behavior/API/DTO change.

**Persistence-agnostic note:** `lld_02` (sequence flows) and `lld_03` (API contracts) describe runtime behavior that is the SAME regardless of store. Do NOT import persistence / FK / durability / append-only / migration facts into them — those are `lld_01` (data model) / infrastructure concerns. The `19666fb` baseline matters only for accuracy of the as-built **API/flow** behavior, not to add persistence content to lld_02/03.

## 5. Contradiction fixes
- **C-1:** pin `VerificationCompletedEventDto.EventType = "VERIFICATION_COMPLETED"` (as-built, per TIP-07 closeout); add a `> note:` that TIP-06 §20's `EKYC_VERIFICATION_COMPLETED` is stale.
- **C-5:** remove/correct any pre-build storage/vault/package wording in lld_02/03 that contradicts the as-built behavior.

## 6. Definition of Done (verifiable)
- [ ] lld_02 has a complete state-transition table matching `VerificationSessionState` + service logic (states, transitions, guards, terminal set, reject-on-terminal errors).
- [ ] lld_03 has: endpoint inventory (6 public routes only), per-endpoint request/response DTO shapes, the error→status registry table, the scope→endpoint catalog, and the sanitization rules (with the completion-notification `clientApplicationId` exception stated).
- [ ] Completion notification documented as projection / application-query only — no public route in the LLD.
- [ ] Every consolidated statement is traceable to a cited code file or decided TIP.
- [ ] C-1 fixed; no statement in lld_02/03 contradicts the code.
- [ ] Each edited file's `**Version:**` bumped to 0.2 + changelog entry added (no lld_00 / docs/00_README change).
- [ ] `git diff --stat` shows ONLY `docs/lld_02_*` and `docs/lld_03_*` changed. Docs-only; no src/tests.
- [ ] OPEN list produced for any kickoff rule lacking code (not consolidated as authoritative).

## 7. Scope floor (anti-creep, both directions)
- Docs-only; only the 2 files (lld_02 + lld_03) in §3. Touching anything else = defect → STOP.
- Every sentence must trace to code or a decided TIP. No invented rules, no "improvements," no new design. Under-consolidation (leaving a clearly-built rule out) is also a defect.

## 8. STOP/RRI
- A kickoff rule with no corresponding code → flag OPEN, do NOT write it as authoritative.
- A code↔TIP contradiction beyond C-1/C-5 → STOP and report; do not silently pick.
- Any required change outside lld_02/03 → STOP.

## 9. Validation + report
- Docs-only → no full test run. Run static validation:
  - `git diff --stat` (confirm only lld_02 + lld_03 changed) and `git diff -- docs/lld_02_sequence_flows_v0_1.md docs/lld_03_api_contracts_v0_1.md` (review the actual edits).
  - `rg "EKYC_VERIFICATION_COMPLETED|clientApplicationId|PayloadHash|VaultRef|completion notification|webhook" docs/lld_02_sequence_flows_v0_1.md docs/lld_03_api_contracts_v0_1.md` — to catch the stale C-1 string, the sanitization exception wording, and any accidental webhook/public-route language.
- Do NOT commit (await Contractor review).
- Report: 5-line summary + (a) sections/tables written in lld_02 and lld_03; (b) every code↔doc discrepancy found and how resolved (code-wins); (c) every rule flagged OPEN.

## 10. Review after build (Contractor)
Contractor runs an adversarial spot-check: sample consolidated statements (state transitions, error→status rows, sanitization) and verify each against the actual code (accurate consolidation vs invented/drifted spec); confirm C-1 fixed; confirm no new design crept in; confirm the OPEN list is honest. Then closeout.
