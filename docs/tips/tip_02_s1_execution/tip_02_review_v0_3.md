# TIP-02 S1 Execution Review v0.3

**File:** `docs/tips/tip_02_s1_execution/tip_02_review_v0_3.md`
**Version:** 0.3
**Status:** Reconciled - roadmap patched
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1
**Purpose:** Records external review findings for the S1 execution roadmap and triages required follow-up before TIP-03.

## Changelog

### v0.3 - GPT findings reconciled into roadmap

- Re-checked GPT web findings against the current roadmap after AGY triage.
- Accepted all five GPT findings as still valid roadmap hardening items.
- Patched `tip_02_roadmap_v0_2.md` to move invariants earlier and strengthen TIP-03/TIP-04/TIP-06/TIP-08 responsibilities.

### v0.2 - GPT web review captured; reconciliation required

- Added five GPT web findings against the S1 review packet and roadmap sequencing.
- Marked the review set as requiring reconciliation because AGY findings had already been triaged first and may have shifted the review context.
- Identified transaction-bound invariants, append-only persistence, payload projections, placeholder signature labeling, and TIP-03 invariant checklist as candidate pre-implementation requirements.

### v0.1 - AGY review triage recorded

- Imported the external AGY review findings at a planning level.
- Classified findings by recommended handling before TIP-03.
- Identified LLD/API clarification items that should be resolved before business implementation.

## 1. Review Sources And Snapshot Warning

External review report:

```text
C:\Users\Admin\.gemini\antigravity\brain\edfdf4fc-8ada-4af4-b38c-b3bb3443a54e\tag_ekyc_review_report.md
```

The AGY report contains 10 findings. Several are material and should be handled before TIP-03 runtime work starts.

GPT web reviewed the initial S1 review packet; the packet has since been updated as `docs/review_packets/tagekyc_s1_docs_review_packet_v0_2.md`.

Important reconciliation note:

- AGY and GPT web findings should not be treated as if they reviewed the exact same final document state.
- AGY triage was recorded first, and that changed the planning context available in this repository.
- GPT web findings were re-checked against the current roadmap/review state before patching roadmap.
- The reconciliation decision is recorded below. No Product Brief, HLD, or LLD semantics were changed by this reconciliation patch.

## 2. Triage Summary

| ID | Severity | Triage | Recommended handling |
| --- | --- | --- | --- |
| F-01 | P1 | Accept as blocker | Clarify adapter result submission model and VaultRef boundary before TIP-03/TIP-05 implementation. |
| F-02 | P1 | Accept as blocker | Define S1 capture artifact upload mechanism before capture API implementation. |
| F-03 | P1 | Accept as blocker | Tighten trust boundary: clients submit artifacts; scoped internal adapters submit evidence results. |
| F-04 | P2 | Needs user/design decision | Decide whether `TagEkyc.SignFlow` remains as named consumer contract placeholder or becomes generic transaction-bound profile project. |
| F-05 | P2 | Accept but defer from S1 blocker | Clarify name matching/privacy strategy before production identity matching; S1 may keep hashes and avoid plaintext persistence. |
| F-06 | P2 | Accept as pre-webhook blocker | Align generic and SignFlow webhook payload schema before TIP-07. |
| F-07 | P2 | Reject for now | Versioned filenames were explicitly requested; avoid changing unless user reverses naming convention. Mitigate with stable TIP index links. |
| F-08 | P2 | Accept as model clarification | Separate session lifecycle state from verification result before TIP-03 domain model implementation. |
| F-09 | P2 | Accept as policy clarification | Clarify whether `DOCUMENT_NFC` implies `DOCUMENT_OCR`, or list both checks explicitly for SignFlow S1. |
| F-10 | P3 | Accept as governance improvement | Add dependency/license STOP+ASK gate for non-permissive or commercial packages. |
| GPT-01 | P1 | Accepted after reconciliation | Moved transaction-bound invariants into TIP-03 domain/contracts and TIP-04 create-session validation; TIP-08 now verifies SignFlow profile behavior and E2E cases. |
| GPT-02 | P1 | Accepted after reconciliation | Added append-only repository/persistence invariants for evidence/audit in TIP-03. |
| GPT-03 | P1 | Accepted after reconciliation | Split internal evidence manifest contracts from default business-consumer result/evidence summaries. |
| GPT-04 | P2 | Accepted after reconciliation | Labeled S1 signature placeholders as non-authoritative and not valid for authenticity/replay/audit reliance. |
| GPT-05 | P2 | Accepted after reconciliation | Added a TIP-03 pre-start invariant checklist against Product Brief, LLD01, and LLD03. |

## 3. Reconciled GPT Findings Patched Into Roadmap

The items in this section were re-validated against the current document state and patched into `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`.

### GPT-01 - Transaction-Bound Invariants Are Too Late In Roadmap

Problem:

- Transaction-bound validation was described mainly under TIP-08.
- TIP-04 already owns create-session APIs and client policy validation.

Risk:

- Builders may implement generic session creation first and retrofit `TRANSACTION_BOUND_EKYC_PROFILE` invariants later.
- `externalTransactionId` and `bindingNonceHash` requirements could become unstable API behavior.

Recommended action:

- Put profile-level invariants into TIP-03 domain/contracts.
- Put create-session validation into TIP-04.
- Keep TIP-08 focused on TagEkyc-owned SignFlow helpers, E2E tests, and negative binding tests.

Reconciliation result:

- Accepted. Patched TIP-03/TIP-04/TIP-08 roadmap responsibilities. No user decision required because this only moves existing Product Brief invariants earlier in the implementation plan.

### GPT-02 - Append-Only Evidence/Audit Must Be A TIP-03 Persistence Invariant

Problem:

- Append-only evidence and audit are product/S1 requirements, but the roadmap previously emphasized append-only behavior mainly in TIP-06.

Risk:

- TIP-03 repository ports and development persistence may accidentally expose update/delete paths for evidence results or audit events.

Recommended action:

- TIP-03 must distinguish mutable session current-state projections from append-only evidence/audit records.
- Add repository interface and test expectations that prevent update/delete behavior for evidence and audit.

Reconciliation result:

- Accepted. Patched TIP-03 roadmap responsibilities and test expectations. No user decision required.

### GPT-03 - Internal Evidence Manifest vs Business Consumer Summary

Problem:

- S1 evidence packages use VaultRefs/hashes, but business consumers must not receive internal VaultRefs or raw artifacts by default.

Risk:

- `GET /api/ekyc/evidence-packages/{id}` could accidentally expose internal VaultRefs in a default consumer payload.

Recommended action:

- TIP-03 and TIP-06 must define two projections:
  - internal/audit evidence manifest MAY contain internal VaultRefs;
  - default business-consumer result/evidence summary MUST expose only allowed hashes, statuses, timestamps, package ids, evidence refs, and sanitized summaries.
- Add contract tests proving default business payloads contain no raw artifact fields, plaintext identity fields, or internal VaultRefs.

Reconciliation result:

- Accepted. Patched TIP-03/TIP-06 roadmap responsibilities and test expectations. No user decision required because default evidence access policy is preserved.

### GPT-04 - Placeholder Signature Fields Must Be Non-Authoritative

Problem:

- S1 contains `payloadSignature`, `webhookSignature`, and `evidencePackageSignature` placeholders while production cryptography and replay protection are deferred.

Risk:

- Future agents or consuming systems may treat placeholder fields as real authenticity guarantees.

Recommended action:

- Document S1 placeholder signatures as `PLACEHOLDER_UNVERIFIED` or equivalent non-authoritative status.
- Add tests/docs stating S1 consumers must not rely on placeholder signatures for authenticity, replay protection, or external audit reliance.

Reconciliation result:

- Accepted. Patched default assumptions, TIP-03, TIP-06, and S1 test expectations. No user decision required.

### GPT-05 - TIP-03 Needs Pre-Start Invariant Checklist

Problem:

- TIP-03 creates domain models, public DTOs, repository interfaces, and persistence boundaries.
- Starting it without an invariant checklist risks wrong enum names, unsafe DTOs, or overbroad persistence semantics.

Recommended action:

- Before or inside TIP-03 kickoff, require a checklist covering:
  - profile rules;
  - RequiredChecks;
  - capture-quality outcomes;
  - raw/VaultRef exposure;
  - append-only evidence/audit;
  - placeholder crypto labeling;
  - no SignFlow runtime dependency.

Reconciliation result:

- Accepted. Patched TIP-03 pre-start invariant checklist. No user decision required because the checklist only restates existing Product Brief/HLD/LLD constraints.

### F-01 - Adapter Push/Pull and VaultRef Boundary

Problem:

- Sequence flows describe adapters processing artifacts behind TagEkyc-controlled boundaries.
- LLD03 exposes S1 evidence-result endpoints such as `/document-result`, `/nfc-result`, `/face-result`, `/fingerprint-result`, and `/capture-quality-result`.
- Some request examples include internal-looking `vaultRef` values.

Risk:

- Agents may implement public or semi-public endpoints that accept internal VaultRefs from untrusted callers.
- The S1 boundary between capture, vault, adapter, and consumer payloads may become unclear.

Recommended action:

- Clarify that evidence result submission endpoints are for scoped internal adapters or explicitly trusted PoC components only.
- Prefer accepting `captureArtifactId`, `evidenceRef`, or `inputArtifactRefs` rather than caller-supplied internal `vaultRef` where feasible.
- Keep internal VaultRef resolution inside TagEkyc infrastructure/adapter boundaries.

Gate:

- User/design gate if changing LLD03 examples or endpoint semantics.

### F-02 - Capture Artifact Upload Mechanism

Problem:

- `POST /capture-artifacts` documents JSON metadata but does not specify how raw image/video/NFC/fingerprint artifacts are submitted or referenced.

Risk:

- Implementation may choose an unsafe or inconsistent pattern such as ad hoc base64, local file paths, or raw bytes in business APIs.

Recommended action:

- For S1, explicitly choose one development-safe pattern:
  - `multipart/form-data` for local PoC uploads, or
  - metadata-only `externalArtifactRef` / pre-staged secure handle for adapter-mediated capture.
- State that business consumers still receive sanitized results only.

Gate:

- User/design gate because it affects LLD03 API contract.

### F-03 - Evidence Result Submission Trust Boundary

Problem:

- The docs distinguish business clients, capture agents/device gateways, and internal adapters, but LLD03 could still be read as allowing client-side SDKs to submit final `PASSED` evidence results.

Risk:

- A compromised client device could bypass verification by submitting forged evidence results.

Recommended action:

- Explicitly state:
  - business clients create sessions and consume sanitized results;
  - capture agents/device gateways submit artifacts or capture metadata only;
  - internal adapters or trusted adapter runtimes submit evidence results;
  - no ordinary business client may submit arbitrary `PASSED` results.

Gate:

- User/design gate only if introducing or changing caller categories/scopes beyond current baseline.

### F-08 - Session State vs Result

Problem:

- `verification_sessions` suggests `FAILED` as a state and `FAILED` as a result.

Risk:

- A session that completed with a failed identity result may be modeled inconsistently as either `COMPLETED` or `FAILED`.

Recommended action:

- Define lifecycle states separately from outcome results.
- Prefer terminal lifecycle states such as `COMPLETED`, `EXPIRED`, `CANCELLED`, `ERROR_TERMINATED` or similar.
- Keep `FAILED` as a verification result, not a lifecycle state, unless explicitly defined as technical terminal state.

Gate:

- User/design gate if changing LLD01 state enum.

### F-09 - DOCUMENT_NFC vs DOCUMENT_OCR Policy

Problem:

- Product Brief says SignFlow S1 required checks are CCCD/NFC, face match, and liveness.
- Some response examples include visual document/OCR-style summaries.

Risk:

- Implementation may not know whether NFC alone satisfies document evidence, or whether OCR/visual inspection is also required.

Recommended action:

- Clarify S1 policy explicitly:
  - either `DOCUMENT_NFC` includes chip validation only and `DOCUMENT_OCR` is separate, or
  - SignFlow S1 requires both `DOCUMENT_OCR` and `DOCUMENT_NFC`.

Gate:

- User/product gate because it changes required checks and UX/capture requirements.

## 4. Findings For Later TIPs

### F-06 - Webhook Schema Alignment

Handle before TIP-07.

Recommended action:

- Align generic `VerificationCompleted` payload with transaction-bound optional fields:
  - `externalSystem`
  - `externalSessionId`
  - `externalTransactionId`
  - `bindingNonceHash`
- Keep these nullable/optional for standard profile.

### F-05 - Fuzzy Match and Privacy Strategy

Handle before production identity matching or before OCR/name matching becomes decision-critical.

Recommended action:

- Define canonicalization if comparing hashes.
- Prefer in-memory plaintext comparison inside a trusted adapter boundary if fuzzy matching is required.
- Do not persist plaintext identity fields by default.

### F-10 - Third-Party Dependency License Gate

Handle in governance before agents add dependencies.

Recommended action:

- Add STOP+ASK for commercial, GPL, AGPL, unknown-license, biometric vendor SDK, or cloud service dependencies.
- Allow routine permissive dependencies such as MIT, Apache-2.0, BSD only when technically justified and recorded.

## 5. Disputed Finding

### F-07 - Version In Physical TIP Filenames

Triage:

- Do not change now.

Reason:

- The user explicitly requested a naming convention shaped as `tip xxx_brief|kickoff|..._version`.
- Current governance intentionally uses names such as `tip_01_brief_v0_1.md`.

Mitigation:

- Keep `docs/tips/README.md` as the stable index.
- When a TIP artifact version changes, update links in the index and referencing docs in the same patch.

## 6. Recommended Next Action

Before starting TIP-03 implementation, open a clarification patch or kickoff that resolves:

1. Adapter result submission model and VaultRef boundary.
2. Capture artifact upload/reference mechanism.
3. Caller trust boundary for evidence result submission.
4. Session state vs verification result enum semantics.
5. SignFlow S1 document check policy: NFC only vs OCR plus NFC.
6. Dependency/license STOP+ASK gate.
7. Transaction-bound invariants in TIP-03/TIP-04.
8. Append-only evidence/audit repository and persistence invariants.
9. Internal/audit evidence manifest vs business-consumer evidence summary projections.
10. Non-authoritative S1 placeholder signature labeling.
11. TIP-03 pre-start invariant checklist.

These items should be handled as planning/LLD clarification work, not as runtime code.
