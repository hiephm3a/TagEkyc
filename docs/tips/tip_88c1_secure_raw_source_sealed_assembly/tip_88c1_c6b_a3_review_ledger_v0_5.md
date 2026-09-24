# A3 v0.5 — PI-TAG-001 internal review ledger

Date: 2026-09-13. Docs-only; no implementation authorization. Independent external CC/GPT PASS on v0.4 covers its consent correction, not this v0.5 executable catalogue. No source/SQL/test/provider execution is claimed.

## V1 — full source-boundary review

Reviewer: independent a3_v05_full_review; read all six normative v0.5 documents, AGENTS, playbook §6/10–13 and active PI-TAG-001. Frozen parent reviewed: E9E44F8BF5DDA070B1A2F658A08F6288BC7D0F857BF943A8F5994058A1D42A62.
Verdict: NEEDS PATCHES — 6 implementation-blocking LATENT_SPEC_GAP findings, 2 HIGH + 4 MEDIUM. No new business decision.

| ID | Severity | Source-confirmed defect | Correction owner / invalidated coverage |
| --- | --- | --- | --- |
| V1-01 | HIGH | R2 framed stream mismatch is caught by S3/writer as OutcomeUnknown; result has no cause, so O19 vs transient cannot be implemented | BR internal cause, full stage mapping, terminal persistence/proofs; CP continuation read/scan; inventory |
| V1-02 | HIGH | E01 transaction_timestamp and existing B2 current-authority helper accept authority expired during lock wait | E01 post-lock clock/current authority predicates and replay/freshness proof |
| V1-03 | MEDIUM | Completion write lacks UUID actor; selection calls raw_export_current_actor; unexpected DB error lacks exact mapping | AR completion actor, protected selection bridge, rollback and HTTP projection |
| V1-04 | MEDIUM | Valid one-class profile cannot satisfy fixed two-class completion | E01 exact two-class readiness/issuance and policy coverage; CP/AR invariants |
| V1-05 | MEDIUM | Six existing R20 direct-call tests omitted from mutation set while old12 function is dropped | Inventory exact hashes and required successor15 test-call migration |
| V1-06 | MEDIUM | Acceptance-policy owner/config/bounds/replay reader absent | AR closed server config and protected acceptance bridge; inventory |

Adjacent review included A1 auth/N/R20/R21/R25, E01/B2/E3, snapshots/begin/complete, R2 framed/provider/writer/verifier, R3–R6, C1/C3, Agent codec/client/orchestrator/lease/config, finalization and required A1/A2 test callers. Lifecycle, authority, DTO shape/precedence, concurrency, evidence/fingerprint, provider/raw scope, tests and module/governance boundaries covered. Mechanical check: 78 mutation rows and 50 inherited/additional source rows matched live bytes/absence; both HEADs matched. No fixture/test/product mutation.

Plausible risks dismissed with reasons: separate subject agreement is expressly prohibited; CP06 ANDs source retention with independent export authority; broker provider scopes are separated and fixture fallback prohibited. Execution remains unproven.

## Drafting correction audit — not another review round

Broker source audit independently discovered that the existing terminal-outcome helper cannot be composed after existing Terminate: it requires termination NULL and lacks the core write context; incomplete cleanup also loses an in-memory terminal cause at crash. This is part of V1-01's implementation closure, not a clean review or new consent/architecture decision. The correction must preserve settlement predicates and keep pending observed cause distinct from final R2TerminalOutcomeCode.

Source inspection also found protected acceptance/binding tables do not grant runtime SELECT. AR therefore specifies narrowly bounded SD bridges with exact signatures/ACL, instead of assuming EF can read them or granting table privileges. These bridges and the terminal-intent amendment require invalidation review including their whole callers/callees, not just wording.

## Review protocol and current state

V1 corrections were applied, then frozen parent `6A07DD01FDB290F70693A13F61DF3C6312EE4E840F3F3D06A92361C0768706C6` was read independently in V2 and V3. No normative edits occurred during those reviews. Do not edit a frozen version while a reviewer reads it.
From round3, any non-convergence requires accumulated-cause analysis; round5 mandatory checkpoint, round10 hard stop. Drafting/census lanes do not count as review.

Tests/build/PG executed in this documentation turn: NONE.
Product/schema/test/config mutation: ZERO.
Stage/commit/push: NONE.
External final review and Homeowner implementation grant: still required.

## V2 — affected-boundary verification

Reviewer a3_v05_full_review: NEEDS PATCHES, three MEDIUM executable gaps: CP08 omitted final terminal fields needed by its consumer; AR's exact writer signature omitted its typed configured-policy argument; BR mapped existing terminal text as varchar(64). All three were independently rediscovered by V3. V1 fixes were directionally valid, but the resulting producer/consumer joins were incomplete. No test execution is claimed.

## V3 — free adversarial review and accumulated-cause analysis

Reviewer a3_v05_free_review read all six normative documents and traced adjacent production source, not just the patch. NEEDS PATCHES, six MEDIUM findings: the three V2 findings plus three new gaps. Aggregate outstanding count is SIX UNIQUE findings, not nine.

| ID | Defect | Correction / required invalidation review |
| --- | --- | --- |
| V2-01 | CP08 cannot distinguish pending intent from final semantic termination | Append final persisted triple and prove pre/post-finalization projections |
| V2-02 | Application-selected acceptance policy cannot reach exact writer signature | Typed nullable argument, explicit validated-Client provider caller, stored-policy replay and non-retained absent-entry control |
| V2-03 | Existing R2TerminalOutcomeCode is text, not varchar(64) | Preserve actual column type; no disguised type migration |
| V3-01 | C1/C3 reference-first path meets hidden B2 session FOR UPDATE in reverse order | Common retained session-before-reference prefix and actual B2 withdrawal interleavings |
| V3-02 | Broker claim/head/attempt order conflicts with TI/R3/R4/R5 | Enumerate all reachable retained row writers and share serialization prefix, including existing R2 terminator |
| V3-03 | O18 same-owner retry names a re-entry path absent from landed code | Materialize bounded successor-attempt transition, caller/fence/cleanup/horizon semantics; no terminal-code revival |

Accumulated non-convergence causes: V1 focused on local contract completeness and exposed missing authority/cause/config ownership; its patches introduced interfaces whose outputs and parameters were not joined end-to-end. V2 caught those producer/consumer gaps. V3 followed hidden locks and actual retry branches across function boundaries, exposing that preserving each legacy function's internal order does not preserve a safe composed order, and that a named legacy re-entry behavior was not implemented. These are technical gaps, not unresolved consent decisions.

Improvement for the next patch/review: derive one finite whole-call serialization prefix for every retained mutator; join every persisted field to its exact readback projection and consumer; label landed behavior separately from newly specified transitions; exercise reviewer traces through retry, cleanup and withdrawal in both competing orders. Review all six affected boundaries after patching, not merely the sentences edited. From V4 onward compare closure against this cumulative table. Round5 remains a mandatory convergence checkpoint and round10 a hard stop; do not label drafting/census as clean rounds.

## V4 / V5 — frozen verification in progress

Frozen parent: `5EEA13D1D3AD003AEF3A6BBBEFA300E57516A47C45B99B6CFC1C84B102D77269` (36,815 bytes / 311 lines), with five exact companion bindings. Source census: 93 mutation rows and 52 inherited/additional read-only rows, zero mismatches; predecessor v0.4 unchanged. V4 verifies all six prior findings through whole affected call graphs; V5 is a fresh independent full-catalogue review. They read the same immutable candidate; these are two independent review passes, not two patch iterations.

Drafting also discovered that CP08/CP09 could lose a fresh R1 row by requiring a key row before key preparation. The reader and scan now explicitly use an optional key join and return the allocated ID from the attempt, with a pre-key positive proof. This is a drafting-discovered matrix gap, not an extra reviewer finding. Readability of an unprepared row does not itself prove that its later cleanup/retry path is executable; V4 specifically traces that remaining risk.

No clean verdict is claimed while either review is running. The catalogue remains a proposal, not an implementation grant. No product tests have run in this documentation turn.

## Mandatory V5 root-cause checkpoint — before further normative patching

V5 full review: NEEDS PATCHES, two HIGH implementation-blocking LATENT_SPEC_GAP findings on the same frozen parent. (1) RE01 renewed the attempt lease but froze ReservationExpiresAtUtc, which landed prepare-key/encryption/staging consumers still enforce. (2) A committed pre-key R1 crash becomes discoverable but cannot terminate/re-enter: missing key cannot satisfy settlement and expired lease prevents creating it. V4 is finishing its whole-call verification; do not patch normative files until that reviewer releases the freeze.

Classification and accumulated origin:

- Reservation expiry: drafter patch-local semantic omission in new RE01, plus incomplete earlier consumer review. The ratified spine :3910 already derives ReservationExpiresAtUtc at each R1/re-entry/reclaim; treating every reservation column as immutable contradicted that source, not a new Homeowner choice. Earlier matrices joined new attempt fields but omitted the old expiry read by downstream consumers.
- No-key crash: incomplete earlier state-space review. The optional-key readback correction repaired observation, not recovery. Existing cleanup requires a key row; no function can create it after expiry. The matrix missed the Cartesian case committed R1 × no provisioning row × expired original lease.
- Neither finding is unsupported, repeated without new evidence or a request to broaden consent/provider/product scope. There is no implementation false PASS: all candidates remain ungranted and no product mutation has begun.

Correction rules for V6: distinguish immutable source/producer/authority horizons from renewable owner-derived lease fields; trace every deadline through all downstream readers; make each discoverable state join a callable transition and terminal predicate, including absence of a resource. Define bounded no-provider-start settlement with actual current-head/fence, actor, absence and lease-expiry guards; do not manufacture key material, provider receipts or raw reads. Add actual retry-after-old-lease and kill-after-R1-before-key proofs. Include key-prepare-versus-settlement race and unchanged old-attempt/source-history assertions. New state predicates must propagate to RE01, TI01/TI02, CP08/CP09, guards, manifest and proof rows as one patch.

Decision: rounds6–10 can still converge inside the authorized documentation scope. Both defects have source-grounded technical corrections; consent/business decisions stay closed. No implementation or production expansion is authorized. Review the complete invalidated recovery/clock surfaces after the correction; round10 remains the hard stop. The overall catalogue is NOT PASS at this checkpoint.

V4 final: three MEDIUM actionable gaps; its two recovery findings overlap the two HIGH V5 findings. Adopt the higher severity in the aggregate: TWO HIGH + ONE MEDIUM unique outstanding. The third is C3's outer begin-stream clock: the helper-only prefix leaves the outer caller's timestamp sampled before the new session wait. This is incomplete propagation of V3's whole-call lock correction, not an additional proven recipient/package deadlock. Add the exact outer entry to the forward-replacement census and sample one shared time after all blocking authority locks. Its expiry proof must hold the real session lock across the deadline and assert zero Streaming event/provider opens. All other V2/V3 closures were verified, but RE01's downstream lease compatibility and C3 outer closure remained incomplete.

V4 has released the freeze; V6 correction may now proceed. Additional root cause: a local helper's safe clock does not replace the timestamp captured by its caller. The next review must trace the actual outer callable and its shared time value, not only the new helper. The speculative extra C3 lock-cycle was not established and is not counted as an actionable finding. No normative patch was made during V4/V5 review.

## V6 / V7 — independent clean verification

Both reviewed exact parent `038C187AECD5F7F9391B3759C553177B5EA0CB4A72E8C0F741D79E405A16F305` and its five exact bindings after the root-cause correction. V6 affected-boundary reviewer and V7 independent full-catalogue reviewer each returned PASS: 0 HIGH / 0 MEDIUM / 0 LOW actionable findings. V7 read all 1,619 normative lines. Both traced actual preparation/provider ordering, lease consumers, guard updates, outer C3 and its helper/B2 locks, terminal/scan/replay predicates, ACL and proof obligations. First clean review round: V6. These are specification/source-review passes, not executed proof.

Three final defects closed: RE01 updates only the derived reservation lease under the owner CAS while preserving original horizons/fingerprint; NPS01 records a fenced no-provider-start witness without fabricated key/provider history; the actual outer C3 and retained key preparation use post-lock clocks. Risk controls cover both prepare/NPS race orders, retry past the old lease through R2–R5, and held-session-across-expiry with zero Streaming/provider reads.

V6's possible TI01-before-NPS dead end was investigated: no legitimate prescribed caller was established because named live-input callers already have key/object evidence and CP09 directs unwitnessed no-key work to NPS01 first. It was not counted as a blocker. The final editorial pass nevertheless restates that caller order explicitly. BP20's broad "old rows byte-identical" wording is clarified to exempt only the derived reservation-expiry field already authorized by RE01. No new guard, column, function signature, outcome, consent action or authority is introduced by these clarifications.

## V8 — final bounded propagation verification

Frozen final parent `9A0254769139577D78F0FD6374D1E052ECE4B3D500BFAD5FBB7337531F7FD38F` (36,917 bytes / 311 lines). BR successor binding `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48`; four other companions unchanged. Delta is only the two clarifications above, parent status/changelog and exact SHA rebind. Independent V8 verification returned PASS, zero findings. Reversing the two BR editorial edits in memory reproduced the exact V6-reviewed BR hash; all five bindings match. No catalogue edits occurred during this verification. V6/V7 semantic coverage remains applicable; V8 is not labelled a new full architecture review.

## Pilot metrics for this documentation slice

TIP-88C1/C6B-A3, High-risk. SQL/transactions, API/auth/disclosure, crypto/provider reuse, raw custody, lifecycle/recovery and governance all reviewed. Separate message-queue protocol N/A: bounded database continuation scan is specified instead; no new queue/message broker.

| Round | Actionable findings | Disposition |
| --- | --- | --- |
| V1 | 2 HIGH + 4 MEDIUM | Applied, invalidated producer/consumer joins rereviewed |
| V2 | 3 MEDIUM | Three joins subsequently also found by V3 |
| V3 | 6 MEDIUM, including all three V2 overlaps | Six unique outstanding at that freeze; cumulative analysis recorded |
| V4 | 3 MEDIUM | Two recovery findings overlap V5; one outer-C3 propagation gap |
| V5 | 2 HIGH | Higher severity adopted for the two shared recovery findings; mandatory checkpoint completed |
| V6 | 0 | First clean affected-boundary pass |
| V7 | 0 | Independent full-catalogue/current source graph clean |
| V8 | 0 | Bounded final wording and provenance PASS |

Patch-local incomplete propagations: RE01 derived reservation expiry and C3 outer clock were explicitly identified at the V5 checkpoint; V2's three local producer/consumer joins were also incomplete V1 corrections, not proof of runtime regressions. Unsupported/repeated-without-evidence actionable claims accepted: zero; overlapping independently found defects were deduplicated. Drafting-discovered matrix gaps were recorded separately (terminal helper composition, protected-table reader privilege, pre-key optional readback). Mutation tests/build/PG executed: zero. Builder false implementation PASS or product STOP in this turn: zero; implementation was never granted. Post-dispatch implementation correction count: not yet measurable because the dispatch has not been granted/executed. Governance/pilot ratification itself is not claimed here.

Final source census before V8: 93 proposed M/N rows + 53 inherited/additional read-only rows match full SHA/absence; v0.4 is unchanged. No new product/schema/test/config/project mutation, stage, commit or push. Existing unrelated worktree changes remain outside this slice. Future test effectiveness, DDL/ACL installation, full provider/transport execution and production readiness are explicitly unproven.

Final internal disposition: READY FOR EXTERNAL CC/GPT REVIEW on the final exact parent above, not implementation authority. Both repository HEADs remain the parent baseline; staged=0 and conflicted=0 in both. Internal review count8 includes full, affected-boundary and bounded editorial passes as individually labelled; it does not mean eight complete independent architecture reviews. No further internal round is required absent a new finding; external review and Homeowner implementation grant remain separate.
