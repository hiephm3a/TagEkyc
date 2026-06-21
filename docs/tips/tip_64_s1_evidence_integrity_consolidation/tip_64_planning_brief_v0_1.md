# TIP-64 S1 Evidence-Integrity Consolidation — Planning Brief v0.1

**File:** `docs/tips/tip_64_s1_evidence_integrity_consolidation/tip_64_planning_brief_v0_1.md`
**Version:** 0.3.1
**Status:** Draft — docs-only consolidation planning (Tier-2 / evidence-bearing); GPT/Codex review round 3 patches applied; pending convergence before dispatch
**Date:** 2026-06-21
**Baseline:** `dc9d0ee docs: close TIP-63 S1 LLD runtime-contract consolidation (lld_02/lld_03 v0.2)` (master).
**Purpose:** Consolidate the as-built S1 evidence-integrity spec — hash canonicalization, manifest/package hash chain, audit-event hashing, and the signature-placeholder model — into `lld_01`, and explicitly flag the Tier-2 legal/crypto open items it surfaces. This is D-02 slice 2.

---

## Changelog

### v0.3.1 — GPT/Codex review round 3 (2026-06-21)
- P1: brief §8 STOP/RRI brought in line with kickoff §5 — code-wins RESOLVES discrepancies (signature naming, timestamp, audit model); no STOP-trap. Removes the doc-set inconsistency where the brief still forced STOP on timestamp/audit.

### v0.3 — GPT/Codex review round 2 (2026-06-21)
- P2: softened "carry the evidence's legal weight / legally-weightiest" → "highest legal/crypto review sensitivity" (avoids implying legal reliance). Aligned with kickoff v0.3 (STOP-rule/code-wins fix + as-built audit-model documentation).

### v0.2.1 — Contractor self-check (2026-06-21)
- Removed `audit_events` from the signature-reconciliation lists (it has no signature field — only audit-hashing in the integrity section); aligns the brief with kickoff §3.2 (which correctly lists only evidence_packages + evidence_results). Tightened "2-3 entities" → "2 entities".

### v0.2 — GPT/Codex review patches (2026-06-21)
- Aligned with kickoff v0.2 patches (timestamp not whole-second; webhook projection-only guard; stale-name validation; lld_01 metadata-block addition).
- Downgraded "D-02 substantially complete" → "substantially reduced; lld_04 remains conceptual/deferred."

### v0.1 — Initial planning brief
- Opened TIP-64 as docs-only Tier-2 consolidation of the as-built evidence-integrity spec into `lld_01`. Recorded scope, source-of-truth precedence (as-built code wins), Intent Ledger, the Tier-2 legal/crypto open items (canonicalization-not-JCS; placeholder signatures), non-goals, and STOP/RRI.

## 0. Repo Evidence

| Evidence | Finding |
|---|---|
| Branch / HEAD | `master` / `dc9d0ee` |
| Scope precedent | TIP-63 consolidated lld_02/03; EBS-05/06 (decision/assurance) now in lld_02 §6 |
| As-built evidence-integrity | `VerificationCompletionApplicationService`: `HashCanonical` (SHA-256 over `"{label}\n{JSON}"`, `CanonicalJsonOptions = JsonSerializerDefaults.Web`), chain `manifestBodyHash → packageHash → manifestHash`, completion audit payload hashes, `DeterministicGuid`, `SignaturePlaceholderStatus.PlaceholderUnverified` |
| Decided spec source | TIP-06 kickoff §12 (manifest shape), §14 (canonicalization plan), §15 (placeholder signature), §16 (audit append rules) |
| `lld_01` current state | Has `evidence_packages`, `evidence_results`, `audit_events` entities with hash fields, BUT no canonicalization/hash-chain rule, and stale signature field names (`evidencePackageSignature`/`payloadSignature` vs as-built `...SignatureStatus`) |
| lld_04 | Conceptual/forward only (no engine adapter interface exists in code) — NOT this slice |

## 1. Status / Purpose / Authorization basis

TIP-64 is **docs-only** and **Tier-2 / evidence-bearing** (it documents the hash chain and signature model that have the highest legal/crypto review sensitivity — see the Tier-2 limits, which prevent any legal-reliance claim). It refreshes `lld_01` in place to reflect AS-BUILT evidence integrity and to flag the legal/crypto decisions the as-built surfaces. It does not change code, tests, behavior, or any other doc. Authorization basis: `decision_register_v0_1.md` D-02; EBS registry (EBS-01/02/07); precedent TIP-49/TIP-63 (docs-only LLD consolidation). Role split: Contractor drafts → GPT + Codex review/converge → Codex builds → Contractor adversarial spot-check.

## 2. TIP Analytical Summary / Intent Ledger

### Intent
Move the as-built evidence-integrity spec (canonicalization + hash chain + audit hashing + signature model) from the TIP-06 kickoff into durable `lld_01`, and surface the Tier-2 legal/crypto open items honestly.

### Expected Outcome
After TIP-64: `lld_01` has an authoritative Evidence-Integrity section describing the as-built canonicalization, hash chain, audit hashing, deterministic ids, and signature-status model — all traceable to code; the existing `evidence_packages`/`evidence_results` signature fields are reconciled to as-built status naming (audit_events has no signature field — it appears only in the audit-hashing part of the integrity section); and a clearly-marked Tier-2 Open Items block flags the canonicalization-not-JCS and placeholder-signature concerns for legal/crypto sign-off. No legal-sufficiency claim is made.

### Accepted Decisions
| Decision | Why | Scope impact | Non-claims |
|---|---|---|---|
| Consolidate AS-BUILT evidence integrity into lld_01 | The spec is built but stranded in TIP-06 kickoff | lld_01 gains an Evidence-Integrity section | Describes as-built; not a legal-sufficiency claim |
| Document canonicalization exactly as-built, then FLAG it | The as-built uses Web JSON, not RFC 8785 JCS — honesty requires stating the limit | Tier-2 Open Item T2-1 | Not claimed JCS / portable / legally sufficient |
| Reconcile stale signature field names to `...SignatureStatus` | Code wins; lld_01 v0.1 has `evidencePackageSignature`/`payloadSignature` but as-built has status enums | Field reconciliation in 2 entities (evidence_packages, evidence_results) | No behavior change |
| Flag, NOT resolve, the Tier-2 legal/crypto items | Resolving them (adopt JCS / real signing) is a Homeowner+legal decision, not a doc edit | Open Items block + debt link | TIP-64 does not adopt JCS or implement signing |

### Rejected / Deferred Branches
| Branch | Disposition | Why | Follow-up |
|---|---|---|---|
| Adopt RFC 8785 JCS canonicalization now | Rejected | Code change + legal/crypto decision; out of a docs-only consolidation | Separate Homeowner/legal-gated slice |
| Implement real signatures (HSM/KMS) now | Rejected | P0 production debt; code + key-management | Phase-1-exit / production slice |
| Consolidate lld_04 (engine adapters) | Deferred | Conceptual/forward, not as-built | Forward-design slice if/when S2 engines start |

### Debt / Gap Impact
| Debt/gap | Action | Result | Carry-forward |
|---|---|---|---|
| EBS-01 canonicalization (not JCS) | Documented + flagged T2-1 | Risk recorded, not resolved | Homeowner/legal/crypto decision |
| EBS-07 placeholder signatures | Documented + flagged T2-2 | Risk recorded, not resolved | P0 production signing debt |
| D-02 (stranded as-built spec) | lld_01 evidence-integrity consolidated | D-02 substantially reduced (lld_01/02/03 consolidated); lld_04 remains conceptual/deferred | lld_04 forward-design slice if/when S2 engines start |

### Non-Claims
TIP-64 does not: change code/tests/behavior; adopt JCS or any new canonicalization; implement signatures; claim the as-built hashing or signatures are legally sufficient, production-grade, non-repudiable, or audit-reliable; consolidate lld_02/03/04/hld; resolve the Tier-2 items.

### Dispatch Readiness
- Implementation dispatch allowed? **Not yet** — pending GPT + Codex review.
- Files that may change at build: `docs/lld_01_data_model_v0_1.md` only.
- STOP/RRI gates: see §8 + kickoff §8.

## 3. Problem being solved
The evidence-integrity spec (how the manifest/package hashes are computed and chained, and the signature model) has the highest legal/crypto review sensitivity in S1, but it lives only in the TIP-06 kickoff. `lld_01` has the entity fields but not the computation contract, and its signature field names are stale. Future legal/crypto review has nowhere authoritative to look. Consolidating it — and honestly flagging its limits — is the Tier-2 step.

## 4. Scope (this slice)
- `lld_01_data_model_v0_1.md` → v0.2: add an Evidence-Integrity section (canonicalization, hash chain, audit hashing, deterministic ids, signature-status model); reconcile `evidence_packages`/`evidence_results` signature field names to as-built (audit_events = audit-hashing only, no signature field; webhook_deliveries left deferred); add a Tier-2 Open Items block.

## 5. Non-goals
lld_02/03/04, hld; any src/tests change; any behavior change; adopting JCS or implementing signatures; any legal-sufficiency claim; resolving the Tier-2 items.

## 6. Source-of-truth precedence
1. AS-BUILT code wins (esp. `VerificationCompletionApplicationService` canonicalization/hash/signature; domain `EvidencePackage`, `SignaturePlaceholderStatus`, `HashRef`).
2. Then TIP-06 kickoff §12/§14/§15/§16.
3. On conflict, code wins + a one-line note.

## 7. Acceptance criteria (DoD summary)
Full DoD in kickoff §6. Summary: Evidence-Integrity section traceable to code (canonicalization label+format, hash-chain order, audit hashing, deterministic ids, signature-status); signature fields reconciled to as-built; Tier-2 Open Items (T2-1 canonicalization-not-JCS, T2-2 placeholder signatures) flagged with explicit "requires legal/crypto sign-off"; no legal-sufficiency claim; persistence-agnostic; docs-only (lld_01 only); lld_01 Version + changelog updated.

## 8. STOP/RRI
- A kickoff rule with no corresponding code → flag OPEN, do not consolidate as authoritative.
- Code↔TIP-06 discrepancies are resolved by **code-wins** (document code + stale-note), NOT by STOP. Known discrepancies (do NOT STOP): signature-field naming, timestamp (TIP-06 whole-second vs code full-precision), audit model (TIP-06 three-event vs code single `VERIFICATION_COMPLETED` event) — see kickoff §5. STOP only on a discrepancy that genuinely cannot be resolved by reading the code.
- Any temptation to RESOLVE a Tier-2 item (adopt JCS, implement signing) → STOP (out of scope; Homeowner/legal decision).
- Any need to touch a file outside lld_01 → STOP.

## 9. Recommended next action
Submit this brief + the TIP-64 kickoff for GPT + Codex review (Tier-2: reviewers should also sanity-check that the as-built limits are flagged, not glossed). Converge, then dispatch to Codex for build. The Tier-2 Open Items (T2-1/T2-2) become Homeowner+legal decisions tracked in the debt registry — they are NOT resolved by this TIP.
