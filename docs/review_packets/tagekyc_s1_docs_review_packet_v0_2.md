# TagEkyc S1 Documentation Review Packet v0.2

**File:** `docs/review_packets/tagekyc_s1_docs_review_packet_v0_2.md`
**Version:** 0.2
**Status:** Ready for external review
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1
**Purpose:** Self-contained packet for GPT web or human reviewers to review TagEkyc S1 documentation and execution planning without local repository access.

## Changelog

### v0.2 - Review findings incorporated

- Moved transaction-bound invariants into TIP-03/TIP-04 responsibilities.
- Added append-only evidence/audit persistence invariants before TIP-03.
- Split internal/audit evidence manifest from default business-consumer evidence summary.
- Marked S1 signature placeholders as non-authoritative.
- Added TIP-03 pre-start invariant checklist requirements.

### v0.1 - Initial external review packet

- Summarized product baseline, S1 scope, non-goals, TIP roadmap, and reviewer checklist.
- Added expected finding format and review constraints.
- Preserved TagEkyc vs SignFlow, legal readiness, and raw evidence boundary context.

## 1. Reviewer Task

Please review the TagEkyc S1 documentation and implementation plan for consistency, missing scope, unsafe assumptions, and unclear gates.

This is a review-only request. Do not rewrite the documents unless explicitly asked. Return findings with severity, file/section references, rationale, and recommended action.

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

## 3. Source-of-Truth and Reading Order

Product source-of-truth:

```text
docs/00_product_brief_v0_1.md
```

Required reading order for repository agents:

1. `docs/00_README.md`
2. `docs/00_DOCS_GOVERNANCE.md`
3. `docs/00_product_brief_v0_1.md`
4. `docs/tag_ekyc_docs_baseline_closeout_v0_1_1.md`
5. Active TIP or target document
6. Supporting HLD/LLD/integration docs relevant to the task

Current TIP structure:

```text
docs/tips/tip_XX_short_slug/
tip_XX_<artifact>_vA_B.md
```

Canonical TIP artifact names:

- `brief`
- `kickoff`
- `review`
- `execution_report`
- `closeout`
- `roadmap`

Current TIP files:

- `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md`
- `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`

## 4. Product Baseline Summary

TagEkyc must:

- Create and manage verification sessions for external client applications.
- Apply explicit RequiredChecks policy per session.
- Record document, CCCD/NFC, face match, liveness, fingerprint, and risk result shapes.
- Produce an `EkycEvidencePackage` with evidence manifest, hashes, VaultRefs, timestamps, and audit references.
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
- Return raw CCCD images, raw NFC data groups, raw face images, liveness media, fingerprint images, fingerprint templates, or plaintext identity fields to business consumers by default.

## 5. Key Product Decisions

- TagEkyc is an independent eKYC / identity assurance platform.
- SignFlow is a consumer profile, not the base platform.
- `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE` are defined.
- `bindingNonceHash` is required for transaction-bound profile, not for every generic session.
- `VerificationSession` is the root business correlation object.
- `CaptureArtifact` and `EvidenceResult` are separate concepts.
- `CAPTURE_QUALITY` and retry semantics are part of the S1 model.
- Business clients do not receive raw artifacts or internal VaultRefs by default.
- Signature layers are conceptually separated:
  - `payloadSignature`
  - `webhookSignature`
  - `evidencePackageSignature`
- S1 is evidence-ready, not production-certified eKYC.

## 6. Generic Session and SignFlow Boundary

Generic sessions:

- Represent one identity assurance transaction for one authenticated client application.
- Derive `clientApplicationId` from API key or equivalent credential.
- Must not trust arbitrary client-supplied `clientApplicationId`, `clientCode`, or `externalSystem`.
- May include `purpose`, `subjectRef`, `requiredChecks`, `expiresAt`, and optional `externalSessionId`.
- Do not default to SignFlow-specific values.

Transaction-bound sessions:

- Apply when the eKYC result must be tied to a specific external business transaction.
- Should include `externalTransactionId`.
- Must include `bindingNonceHash`.
- Must never receive or store the raw `bindingNonce`.
- Must echo only `bindingNonceHash`.

SignFlow profile:

- Uses `TRANSACTION_BOUND_EKYC_PROFILE`.
- Sends `externalSessionId`, `externalTransactionId`, `subjectRef`, `purpose = SIGNING_AUTH`, `bindingNonceHash`, and `requiredChecks`.
- Must validate `externalSessionId`, `externalTransactionId`, `bindingNonceHash`, final `result`, `evidencePackageHash`, and authenticity when implemented.
- Must reject the eKYC result if binding validation fails.
- Must not cause TagEkyc to depend on SignFlow source code, database, runtime packages, or internal models.

## 7. S1 Scope

S1 in-scope:

- Verification Session API
- Client Application and API Key authentication model
- RequiredChecks policy model
- `VerificationSession` as root business correlation object
- `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE`
- CaptureArtifact and EvidenceResult logical model
- Generic `CAPTURE_QUALITY` result category
- CCCD/NFC result shape
- Face match result shape
- Liveness result shape
- Fingerprint result shape
- Evidence VaultRef/hash model
- `EkycEvidencePackage` manifest model
- Append-only audit event model
- Webhook/callback result delivery model
- SignFlow integration contract
- Mock or PoC adapters behind interfaces

S1 out-of-scope:

- Production-certified legal eKYC
- Digital signing
- SignFlow database or code dependency
- Certified NFC document validation
- Certified biometric/liveness assurance
- Production fingerprint hardware fleet management
- Production capture artifact retention policy enforcement
- Production capture quality retry policy enforcement
- Production payload signature implementation
- Production webhook signature and replay protection
- Production evidence package signature implementation
- Production capture agent/device trust program
- Full operator review console
- Formal regulatory reporting
- Database migrations and implementation code in the documentation baseline

## 8. S1 Definition of Done

S1 is complete when:

- A client application can create a verification session using API key authentication.
- RequiredChecks are persisted and enforced by session lifecycle.
- Mock/PoC capture artifacts and document, NFC, face, liveness, and fingerprint evidence results can be recorded using stable result shapes.
- Capture quality can produce `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `REVIEW_REQUIRED`, or `TECHNICAL_ERROR` without collapsing those outcomes into identity failure.
- Final verification result can be calculated from evidence summaries.
- Evidence package can be built using VaultRefs/hashes and a deterministic manifest hash.
- Audit events are written for key lifecycle actions.
- Webhook completion result can be delivered or retried.
- SignFlow contract is documented and covered as a transaction-bound profile with binding validation fields.
- Documentation clearly states S1 is not production-certified eKYC.

## 9. Current Implementation State

TIP-01 has been executed locally as a skeleton-only change.

Implemented:

- `.NET 8` solution skeleton.
- `TagEkyc.Api` smoke-only endpoints.
- Placeholder projects:
  - `TagEkyc.Application`
  - `TagEkyc.Domain`
  - `TagEkyc.Contracts`
  - `TagEkyc.Infrastructure`
  - `TagEkyc.Adapters`
  - `TagEkyc.SignFlow`
- xUnit UnitTests, ContractTests, and ArchTests placeholders.
- Dependency-direction smoke tests.
- Root README with build/test notes.

Explicitly not implemented yet:

- LLD03 business API routes.
- Persistence, EF, repositories, migrations, vault, webhook runtime behavior, or storage adapters.
- Mock engine results.
- Cryptography.
- Raw artifact handling.
- eKYC business logic.

Recorded validation for TIP-01:

```powershell
dotnet restore TagEkyc.sln
dotnet build TagEkyc.sln -c Release
dotnet test TagEkyc.sln -c Release
```

## 10. S1 Execution Roadmap

Current roadmap file:

```text
docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md
```

Roadmap:

1. `TIP-02A` - Repository Hygiene and Baseline Acceptance
   - Add or verify `.gitignore`.
   - Separate source files from generated `bin/` and `obj/`.
   - Re-run restore/build/test.
   - Record TIP-01 acceptance or findings.

2. `TIP-03` - Core Domain, Contracts, and S1 Persistence Boundary
   - Pre-start invariant checklist against Product Brief, LLD01, and LLD03.
   - Domain models/enums.
   - Public DTO contracts for LLD03 S1 endpoints.
   - Repository interfaces and application service ports.
   - S1 development persistence for sanitized metadata and refs only.
   - No production migration claim.
   - Profile invariants for `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE`.
   - Append-only evidence/audit repository and persistence invariants.
   - Separate internal/audit evidence manifest contracts from default business-consumer summaries.
   - Non-authoritative S1 placeholder signature status/fields.

3. `TIP-04` - API Key Authentication, Client Policy, and Session Lifecycle
   - API key authentication.
   - Client policy for purposes/profiles/checks.
   - Create/get verification session APIs.
   - Create-session validation for transaction-bound `externalTransactionId` and `bindingNonceHash`.
   - Session lifecycle and RequiredChecks enforcement.
   - Audit events.

4. `TIP-05` - Capture Artifact and Evidence Result Recording
   - Capture artifact API.
   - Document, NFC, face, fingerprint, and capture quality result endpoints.
   - Distinct scopes for business clients, capture agents/device gateways, and internal adapters.
   - Sanitized result storage.
   - Capture quality retry/failure semantics.

5. `TIP-06` - Final Decision, Evidence Package, and Audit Manifest
   - Complete session API.
   - Evidence package retrieval API.
   - RequiredChecks completeness enforcement.
   - S1 decision evaluator.
   - Deterministic manifest hash.
   - Append-only audit behavior.
   - Placeholder evidence package signature.
   - Default business-consumer evidence package summary excluding internal VaultRefs and raw/plaintext sensitive fields.

6. `TIP-07` - Webhook Delivery and Retry
   - Subscription model.
   - Local/dev dispatcher.
   - `VerificationCompleted` payload.
   - Retry endpoint.
   - Delivery metadata.
   - Placeholder webhook signature.

7. `TIP-08` - SignFlow Transaction-Bound Profile and End-to-End S1 Flow
   - E2E verification of transaction-bound validation implemented in TIP-04.
   - Preserve `externalSessionId`, `externalTransactionId`, and `bindingNonceHash`.
   - TagEkyc-owned SignFlow contract helpers.
   - End-to-end tests.
   - Negative tests for missing/mismatched binding fields.

8. `TIP-09` - S1 Hardening, Documentation, and Closeout
   - Full build/test pass.
   - API contract review against LLD03.
   - Security/data-boundary review.
   - Documentation update.
   - S1 closeout report with known debts and production blockers.

## 11. Default S1 Assumptions

Unless the user changes the baseline:

- Default SignFlow S1 required checks are `CAPTURE_QUALITY`, `DOCUMENT_NFC`, `FACE_MATCH`, and `LIVENESS`.
- `FINGERPRINT` remains optional/demo-only unless explicitly enabled by policy.
- `TRANSACTION_BOUND_EKYC_PROFILE` requires `externalTransactionId` and `bindingNonceHash`.
- Generic `STANDARD_EKYC_PROFILE` must not default to SignFlow-specific purpose or binding fields.
- S1 may use mock/PoC adapters behind interfaces.
- S1 must not store real raw biometric or raw identity artifacts outside a vault abstraction.
- S1 must not expose raw artifacts, internal VaultRefs, or plaintext sensitive identity data to business consumers by default.
- S1 uses placeholder signature fields only; production cryptography is deferred.
- S1 signature placeholders must be visibly non-authoritative and must not be treated as authenticity, replay-protection, or external audit guarantees.
- Internal/audit evidence manifests may contain internal VaultRefs; default business-consumer evidence summaries must not.

## 12. User Gate Register

The coordinator should ask the user only for:

- Accepting TIP-01 skeleton baseline if explicit phase acceptance is required.
- Accepting S1 closeout after TIP-09.
- Any change to Product Brief, HLD, LLD, S1 scope, or SignFlow boundary.
- Any legal, compliance, certification, retention, deletion, legal hold, production cryptography, or real biometric/raw artifact storage decision.
- Any production vendor, paid service, credential, external account, or deployment infrastructure decision.

Default recommendation:

- Proceed with TIP-02A and TIP-03 without changing Product Brief/HLD/LLD.
- Use Product Brief S1 defaults.
- Keep raw sensitive data out of default business-consumer payloads.
- Keep S1 explicitly evidence-ready, not production-certified.

## 12.1 TIP-03 Pre-Start Invariant Checklist

Before TIP-03 implementation starts, the kickoff should confirm:

- Profile rules: `STANDARD_EKYC_PROFILE` has no SignFlow defaults; `TRANSACTION_BOUND_EKYC_PROFILE` requires transaction binding fields.
- RequiredChecks are explicit and validated against client policy.
- Capture-quality outcomes remain distinct from identity failure outcomes.
- Raw artifacts, plaintext identity data, and internal VaultRefs are excluded from default business-consumer payloads.
- Evidence results and audit events are append-only.
- S1 signature placeholders are non-authoritative.
- No SignFlow source code, database, runtime packages, or internal models are referenced.

## 13. Known P0/P1 Debts

P0:

- Raw biometric protection before real biometric data is stored.
- Capture artifact retention policy before real user artifacts are stored.
- Capture agent trust/scoping model before pilot with real capture devices.
- Legal certification gap before production claim.
- Evidence package signature before external audit reliance.

P1:

- NFC production readiness before NFC is used for production decisions.
- Fingerprint hardware dependency before fingerprint check becomes mandatory.
- Capture quality retry policy before production capture flows.
- Face liveness quality before production biometric assurance.
- Policy versioning before multiple client policies.
- Payload signature model before signed API payload reliance.
- Webhook signature/replay protection before production webhook reliance.
- Request/correlation id conventions before multi-client pilot.
- Profile naming and policy mapping before additional consumer profiles.

## 14. Review Questions

Please check:

1. Does the S1 roadmap cover every S1 exit criterion?
2. Is the implementation order technically sensible?
3. Are any important prerequisites missing before TIP-03 starts?
4. Does the roadmap accidentally require production persistence, migrations, legal certification, production cryptography, or real raw artifact handling?
5. Are TagEkyc and SignFlow boundaries preserved?
6. Are `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE` rules clear enough?
7. Are `bindingNonceHash` rules clear and safe?
8. Are raw artifact, biometric, plaintext identity data, and VaultRef exposure boundaries preserved?
9. Are capture quality outcomes distinct from identity failure?
10. Are webhook, payload, and evidence package signature layers kept separate?
11. Are STOP+ASK gates sufficient and not too broad?
12. Is the TIP folder and filename convention clear enough for future agents?
13. Are there contradictions between product baseline, Phase 1 scope, TIP-01 execution, and TIP-02 roadmap?
14. Are there missing tests or review checkpoints in the roadmap?
15. Is anything phrased in a way that could be misread as production-certified eKYC readiness?
16. Are transaction-bound invariants enforced early enough in TIP-03/TIP-04 rather than deferred to TIP-08?
17. Does TIP-03 prevent update/delete semantics for append-only evidence and audit records?
18. Are internal/audit evidence manifests clearly separated from default business-consumer evidence summaries?
19. Are S1 signature placeholders clearly non-authoritative?

## 15. Expected Review Output Format

Return findings in this format:

```text
Finding <number>
Severity: P0 / P1 / P2 / P3
File/section:
Issue:
Why it matters:
Recommendation:
Requires user decision: Yes / No
```

Severity guidance:

- `P0`: Blocks S1 plan correctness, creates legal/security boundary breach, or contradicts Product Brief.
- `P1`: Material implementation risk, missing S1 exit criterion, or unclear gate likely to cause wrong work.
- `P2`: Documentation clarity, sequencing, test coverage, or maintainability issue.
- `P3`: Minor wording, naming, or formatting issue.

If no issues are found, say:

```text
No blocking findings. Residual risks:
- ...
Recommended next action:
- ...
```

## 16. Constraints for Reviewer

Do not recommend:

- Making SignFlow a TagEkyc platform dependency.
- Adding digital signing, WYSIWYS, TSP/TSA, signing consent proof, or signing document handling to TagEkyc.
- Claiming production-certified legal eKYC in S1.
- Exposing raw evidence or internal VaultRefs to business consumers by default.
- Storing real raw biometric artifacts without explicit user/legal/retention gate.
- Choosing production vendors, paid services, credentials, or deployment infrastructure without user approval.
- Treating every API outcome as HTTP 200 just because a vendor API does so.

## 17. Immediate Next Action Under Review

The current recommended next work is:

```text
TIP-02A: repository hygiene and TIP-01 acceptance
TIP-03: core domain, contracts, and S1 persistence boundary
```

Please state whether that next step is acceptable or whether a missing review/decision should happen first.
