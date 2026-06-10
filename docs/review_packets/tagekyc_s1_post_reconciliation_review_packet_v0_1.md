# TagEkyc S1 Post-Reconciliation Review Packet v0.1

**File:** `docs/review_packets/tagekyc_s1_post_reconciliation_review_packet_v0_1.md`
**Version:** 0.1
**Status:** Ready for external review
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1
**Purpose:** Self-contained packet for a second GPT web review after AGY and GPT findings were reconciled into the S1 roadmap.

## Changelog

### v0.1 - Post-reconciliation challenge packet

- Created a focused second-round review packet.
- Asked reviewers to challenge the reconciled roadmap rather than repeat the first review.
- Highlighted unresolved design blockers before TIP-03.
- Added an output format that separates must-fix-before-TIP-03 issues from later risks.

## 1. Reviewer Task

Please perform a critical second-round review of the TagEkyc S1 plan after prior AGY and GPT findings were reconciled.

Do not praise the plan. Challenge it.

Assume earlier reviewers may have been partially wrong, the reconciliation may have introduced contradictions, and some accepted findings may still be under-specified. Your job is to identify remaining blockers before implementation continues.

Focus on whether it is safe to proceed from:

```text
TIP-02A: Repository Hygiene and Baseline Acceptance
TIP-03: Core Domain, Contracts, and S1 Persistence Boundary
```

If TIP-02A is safe but TIP-03 is not, say that explicitly.

## 2. Repository Context

Project path in the owner's local workspace:

```text
D:\Task\Remote Signing\TagEkyc
```

TagEkyc is an independent eKYC / identity assurance platform. It verifies identity evidence and produces sanitized verification results plus auditable evidence package references.

TagEkyc answers:

```text
Who is this person?
```

TagEkyc does not answer:

```text
What did this person see or agree to sign?
```

SignFlow is the first named consumer profile. It is not the base platform model and must not become a TagEkyc runtime dependency.

## 3. Current Artifacts Under Review

Primary files:

```text
docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md
docs/tips/tip_02_s1_execution/tip_02_review_v0_3.md
docs/review_packets/tagekyc_s1_docs_review_packet_v0_2.md
docs/00_DOCS_GOVERNANCE.md
docs/00_AGENT_COORDINATION_BUS.md
docs/tips/README.md
```

Prior review packet:

```text
docs/review_packets/tagekyc_s1_docs_review_packet_v0_2.md
```

This new packet exists because the first GPT review was done against an earlier context, while AGY review findings had already shifted the roadmap. The current task is not to re-run the same review. It is to verify whether reconciliation is coherent and complete.

## 4. Baseline Constraints

TagEkyc must:

- Create and manage verification sessions for external client applications.
- Apply explicit RequiredChecks policy per session.
- Record document, CCCD/NFC, face match, liveness, fingerprint, and risk result shapes.
- Produce an evidence package with evidence manifest, hashes, VaultRefs, timestamps, and audit references.
- Keep evidence and audit records append-only by design.
- Deliver completion results through webhook/callback contracts.
- Use mock or PoC verification engines in S1 only behind adapter interfaces.

TagEkyc must not:

- Perform digital signing.
- Perform WYSIWYS or signing document rendering.
- Capture, decide, or prove signing consent.
- Perform TSP integration.
- Implement or depend on SignFlow internal workflows, code, database, deployment, or runtime.
- Claim production-certified legal eKYC readiness, regulatory approval, or production biometric assurance in S1.
- Return raw CCCD images, raw NFC data groups, raw face images, liveness media, fingerprint images, fingerprint templates, plaintext identity fields, or internal VaultRefs to business consumers by default.

S1 is evidence-ready, not production-certified eKYC.

## 5. Current Reconciliation Result

The current review ledger is:

```text
docs/tips/tip_02_s1_execution/tip_02_review_v0_3.md
```

Its status is:

```text
Reconciled - roadmap patched
```

It records:

- AGY findings were triaged first.
- GPT web reviewed an initial review packet after the AGY context had already changed.
- GPT findings were re-checked against the current roadmap/review state.
- Roadmap v0.2 was patched.
- No Product Brief, HLD, or LLD semantics were intentionally changed by the reconciliation patch.

Your review should verify whether that claim is true.

## 6. Roadmap Changes Now Present In v0.2

The active roadmap is:

```text
docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md
```

Roadmap v0.2 added or moved these responsibilities:

- `TRANSACTION_BOUND_EKYC_PROFILE` requires `externalTransactionId` and `bindingNonceHash`.
- Transaction-bound profile invariants are now in TIP-03 domain/contracts and TIP-04 create-session validation.
- TIP-08 now verifies SignFlow profile behavior and E2E transaction-bound flows instead of owning the core invariant.
- TIP-03 now distinguishes mutable session current-state projection from append-only evidence results and audit events.
- TIP-03 repository ports should not expose update/delete methods for evidence results or audit events.
- TIP-03 and TIP-06 now separate internal/audit evidence manifest contracts from default business-consumer summaries.
- Default business-consumer payloads must exclude raw artifact fields, plaintext identity fields, and internal VaultRefs.
- S1 placeholder signature fields are non-authoritative and must not be treated as authenticity, replay-protection, or external audit guarantees.
- TIP-03 now has a pre-start invariant checklist.

## 7. TIP-03 Pre-Start Checklist

Before TIP-03 implementation starts, the roadmap now requires checking:

- `STANDARD_EKYC_PROFILE` has no SignFlow defaults.
- `TRANSACTION_BOUND_EKYC_PROFILE` requires transaction binding fields.
- RequiredChecks are explicit and validated against client policy.
- Capture-quality outcomes remain distinct from identity failure outcomes.
- Raw artifacts, plaintext identity data, and internal VaultRefs are excluded from default business-consumer payloads.
- Evidence results and audit events are append-only.
- S1 signature placeholders are non-authoritative.
- No SignFlow source code, database, runtime packages, or internal models are referenced.

Review whether this checklist is sufficient before domain models, DTOs, repository ports, and persistence boundaries are implemented.

## 8. Known Findings After Reconciliation

The review ledger still flags these AGY findings as important:

### F-01 Adapter Push/Pull and VaultRef Boundary

Issue:

- Sequence flows describe adapters processing artifacts behind TagEkyc-controlled boundaries.
- LLD03 exposes S1 evidence-result endpoints such as `/document-result`, `/nfc-result`, `/face-result`, `/fingerprint-result`, and `/capture-quality-result`.
- Some examples include internal-looking `vaultRef` values.

Risk:

- Public or semi-public endpoints may accept internal VaultRefs from untrusted callers.
- The boundary between capture, vault, adapter, and consumer payloads may remain ambiguous.

Current recommendation:

- Clarify evidence result submission endpoints are for scoped internal adapters or explicitly trusted PoC components only.
- Prefer accepting `captureArtifactId`, `evidenceRef`, or `inputArtifactRefs` rather than caller-supplied internal `vaultRef` where feasible.
- Keep internal VaultRef resolution inside TagEkyc infrastructure/adapter boundaries.

### F-02 Capture Artifact Upload Mechanism

Issue:

- `POST /capture-artifacts` documents JSON metadata but does not specify how raw image/video/NFC/fingerprint artifacts are submitted or referenced.

Risk:

- Implementation may choose unsafe or inconsistent patterns such as ad hoc base64, local file paths, or raw bytes in business APIs.

Current recommendation:

- For S1, choose a development-safe pattern:
  - `multipart/form-data` for local PoC uploads, or
  - metadata-only `externalArtifactRef` / pre-staged secure handle for adapter-mediated capture.

### F-03 Evidence Result Submission Trust Boundary

Issue:

- Docs distinguish business clients, capture agents/device gateways, and internal adapters, but LLD03 could still be read as allowing client-side SDKs to submit final `PASSED` evidence results.

Risk:

- A compromised client device could bypass verification by submitting forged evidence results.

Current recommendation:

- Business clients create sessions and consume sanitized results.
- Capture agents/device gateways submit artifacts or capture metadata only.
- Internal adapters or trusted adapter runtimes submit evidence results.
- Ordinary business clients must not submit arbitrary `PASSED` results.

### F-04 SignFlow Project Naming

Issue:

- The skeleton contains `TagEkyc.SignFlow`.

Risk:

- Future agents may treat SignFlow as a platform dependency instead of a consumer contract/profile.

Current recommendation:

- Decide whether `TagEkyc.SignFlow` remains as a named consumer contract placeholder or becomes a generic transaction-bound profile project.
- This may require user/design decision if renaming affects project boundaries.

### F-08 Session State vs Verification Result

Issue:

- `FAILED` may be used ambiguously as both session lifecycle state and verification result.

Risk:

- A session that completed with failed identity evidence may be modeled inconsistently.

Current recommendation:

- Separate lifecycle state from outcome result.
- Prefer terminal lifecycle states such as `COMPLETED`, `EXPIRED`, `CANCELLED`, `ERROR_TERMINATED`, or equivalent.
- Keep `FAILED` as a verification result unless explicitly defined as a technical terminal state.

### F-09 DOCUMENT_NFC vs DOCUMENT_OCR Policy

Issue:

- Product Brief says SignFlow S1 required checks are CCCD/NFC, face match, and liveness.
- Some examples include visual document/OCR-style summaries.

Risk:

- Implementation may not know whether NFC alone satisfies document evidence, or whether OCR/visual inspection is also required.

Current recommendation:

- Clarify whether `DOCUMENT_NFC` includes chip validation only and `DOCUMENT_OCR` is separate, or whether SignFlow S1 requires both.

## 9. Specific Review Questions

Please answer these directly:

1. Is `TIP-02A` safe to start now?
2. Is `TIP-03` safe to start now without an additional clarification patch?
3. Did roadmap v0.2 accidentally change Product Brief/HLD/LLD semantics while claiming it did not?
4. Are the transaction-bound profile invariants now early enough in TIP-03/TIP-04?
5. Are append-only evidence/audit rules specific enough for repository ports and development persistence?
6. Is the split between internal/audit evidence manifest and business-consumer summary specific enough to prevent VaultRef/raw/plaintext leakage?
7. Is the placeholder signature language strong enough to prevent false security assumptions?
8. Are F-01, F-02, F-03, F-08, or F-09 blockers before TIP-03 domain/contracts work?
9. Does `TagEkyc.SignFlow` project naming require a user decision before TIP-03, or can it be deferred with guardrails?
10. Does the TIP filename/version convention create link or maintenance risk that is not adequately mitigated by `docs/tips/README.md`?
11. Are any user gates missing from `docs/00_AGENT_COORDINATION_BUS.md` before agents continue autonomously?
12. Is dependency/license governance strong enough before implementation agents add packages?

## 10. What To Look For

Look for:

- Contradictions introduced by reconciliation.
- Missing LLD clarification before DTOs or repository ports are created.
- Overbroad automation scope that should require a user gate.
- Any path where a business client can submit trusted final evidence results.
- Any path where raw artifacts, plaintext identity data, or internal VaultRefs leak to business consumers.
- Any path where placeholder signatures are treated as real security.
- Any path where SignFlow becomes a platform dependency.
- Any ambiguous enum or state model likely to create wrong domain code.
- Any capture artifact ingestion decision that should be made before API DTOs are frozen.
- Any legal/compliance/certification wording that overclaims S1 readiness.

## 11. Expected Review Output Format

Return findings in this format:

```text
Finding <number>
Severity: P0 / P1 / P2 / P3
Current artifact/section:
Issue:
Evidence:
Recommendation:
Must fix before TIP-03: Yes / No
Requires user decision: Yes / No
```

Severity guidance:

- `P0`: Blocks S1 plan correctness, creates legal/security boundary breach, or contradicts Product Brief.
- `P1`: Material implementation risk, missing S1 exit criterion, or unclear gate likely to cause wrong work.
- `P2`: Documentation clarity, sequencing, test coverage, or maintainability issue.
- `P3`: Minor wording, naming, or formatting issue.

If no issues are found, say:

```text
No blocking findings.

TIP-02A safe to start: Yes / No
TIP-03 safe to start: Yes / No

Residual risks:
- ...

Recommended next action:
- ...
```

## 12. Constraints For Reviewer

Do not recommend:

- Making SignFlow a TagEkyc platform dependency.
- Adding digital signing, WYSIWYS, TSP/TSA, signing consent proof, or signing document handling to TagEkyc.
- Claiming production-certified legal eKYC in S1.
- Exposing raw evidence, plaintext identity data, or internal VaultRefs to business consumers by default.
- Storing real raw biometric artifacts without explicit user/legal/retention gate.
- Choosing production vendors, paid services, credentials, or deployment infrastructure without user approval.
- Treating placeholder signatures as production authenticity, replay protection, or external audit guarantees.
- Treating ordinary business clients as trusted evidence-result issuers.

## 13. Reviewer Conclusion Required

End your response with exactly this block:

```text
Conclusion
TIP-02A safe to start: Yes / No
TIP-03 safe to start: Yes / No
Must ask user before TIP-03: Yes / No
Main blocker, if any:
```
