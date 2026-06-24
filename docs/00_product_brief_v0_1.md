# TagEkyc Product Brief v0.1.2

**File:** `00_product_brief_v0_1.md`
**Version:** 0.1.2 - TIP-67/TIP-68 neutrality and proof-signing alignment
**Date:** 24/06/2026
**Status:** Draft - product brief baseline with TIP-67/TIP-68 supersession notes
**Purpose:** Product brief for TagEkyc as an independent eKYC / identity assurance platform.

---

## Changelog

### v0.1.2 - TIP-67/TIP-68 neutrality and proof-signing alignment (24/06/2026)

- Clarified that TagEkyc still must not perform document signing, transaction-consent signing, qualified digital signing, or non-repudiation signing for SignFlow, while it may sign its own eKYC proof/evidence envelope for integrity, origin identification, and audit verification.
- Superseded transaction-bound product language with the neutral `CHALLENGE_BOUND_EKYC_PROFILE`: TagEkyc stores and echoes an opaque `Challenge` and optional `ClientReference`, without interpreting them as transaction, document, consent, or nonce-hash semantics.
- Marked legacy `TRANSACTION_BOUND_EKYC_PROFILE`, `externalTransactionId`, and `bindingNonceHash` as compatibility aliases only; they are not the authoritative product model.
- Updated signature-layer status: evidence/proof signing exists at dev level through TIP-66/TIP-67B, production HSM custody is TIP-68 Slice A, and payload/webhook/CA/TSA/qualified non-repudiation remain deferred.

### v0.1.1 - Vendor reference enrichment + document history (05/06/2026)

- Added `VerificationSession` as the root business correlation object.
- Added `CaptureArtifact` vs `EvidenceResult` distinction.
- Added `CAPTURE_QUALITY` and retry/failure semantics.
- Added explicit `STANDARD_EKYC_PROFILE` vs `TRANSACTION_BOUND_EKYC_PROFILE` language; superseded by v0.1.2 neutral `CHALLENGE_BOUND_EKYC_PROFILE` terminology.
- Added signature layer distinction: `payloadSignature`, `webhookSignature`, and `evidencePackageSignature`.
- Added vendor eKYC reference note for Zalo/Fiza patterns without adopting vendor API semantics.
- Added missing open decisions for capture quality, artifact retention, signatures, correlation ids, and profile policy mapping.
- Added document metadata and changelog tracking.

### v0.1 - Initial product brief baseline (24/05/2026)

- Defined TagEkyc product purpose, non-goals, client types, generic session model, transaction-bound profile, SignFlow profile, S1 happy path, data return boundary, and initial open product decisions; transaction-bound terminology was later superseded by v0.1.2.

---

## 1. Product Purpose

TagEkyc is an independent eKYC / identity assurance platform for external client applications.

TagEkyc verifies identity evidence such as identity documents, CCCD/NFC, face match, liveness, fingerprint, and risk signals. It produces sanitized verification results and auditable evidence package references for consuming systems.

TagEkyc MUST answer: "who is this person?"

TagEkyc MUST NOT answer: "what did this person agree to sign?"

## 2. Non-Goals

TagEkyc MUST NOT perform document signing, transaction-consent signing, qualified digital signing, or non-repudiation signing for SignFlow. TagEkyc MAY sign its own eKYC proof/evidence envelope for integrity, origin identification, and audit verification.

TagEkyc MUST NOT perform WYSIWYS or signing document rendering.

TagEkyc MUST NOT capture, decide, or prove signing consent.

TagEkyc MUST NOT perform TSP integration.

TagEkyc MUST NOT implement or depend on SignFlow internal workflows, code, database, deployment, or runtime.

TagEkyc S1 MUST NOT claim production-certified legal eKYC readiness, regulatory approval, or production biometric assurance.

## 3. Client Types

TagEkyc supports different caller categories. These caller types MUST have different permissions, scopes, and audit treatment.

Business client applications SHOULD include SignFlow, HIS, EMR, Patient Portal, onboarding systems, registration systems, and other systems that need identity assurance results.

Capture agents / device gateways MAY collect document, NFC, face, liveness, or fingerprint evidence on behalf of a verification session.

Internal adapter runtimes MAY execute OCR, NFC reading, face matching, liveness detection, fingerprint matching, risk evaluation, vault storage, or webhook delivery behind TagEkyc adapter contracts.

Operator/admin tools MAY support configuration, monitoring, review, retry, and audit operations when explicitly authorized.

Business client applications MUST NOT be treated as automatically trusted to submit arbitrary `PASSED` evidence.

Capture and adapter submission permissions MUST be scoped separately from ordinary business client permissions.

## 4. Generic Verification Session Model

A generic TagEkyc verification session represents one identity assurance transaction for one authenticated client application.

`VerificationSession` is the root business correlation object for a TagEkyc verification flow. It correlates the client application, purpose, `subjectRef`, profile, required checks, capture artifacts, verification checks, evidence results, audit events, evidence package, callbacks/webhooks, and expiry. It is not merely a technical session id.

The authenticated `clientApplicationId` MUST be derived from the API key or equivalent client credential. It MUST NOT be trusted from arbitrary client-supplied request body fields.

Generic session creation SHOULD include:

- `purpose`
- `subjectRef` or equivalent subject correlation
- `requiredChecks`
- `expiresAt`
- optional `externalSessionId`

Challenge/correlation fields are optional at the generic platform level and profile-gated when required by policy:

- `clientReference`
- `challenge`

Legacy request keys `externalTransactionId` and `bindingNonceHash` MAY be accepted for compatibility, but TagEkyc MUST map them into the neutral `ClientReference` and `Challenge` model and MUST NOT treat them as authoritative transaction semantics.

If `clientCode`, `externalSystem`, or similar fields exist, they MUST be derived from or validated against the authenticated `clientApplicationId`. Client-supplied `externalSystem` MUST NOT be treated as authoritative by itself.

`requiredChecks` MUST be validated against the authenticated client application's allowed purposes, allowed checks, and policy.

## 5. CaptureArtifact vs EvidenceResult

`CaptureArtifact` represents captured, uploaded, or received input artifacts for a verification session.

Capture artifact examples include:

- document front image
- document back image
- selfie image
- liveness media
- NFC read artifact
- fingerprint capture
- device/capture metadata

`EvidenceResult` represents processed verification output derived from one or more capture artifacts.

Evidence result examples include:

- OCR result
- NFC validation result
- face match result
- liveness result
- fingerprint match result
- fraud/risk result
- capture quality result

Capture artifacts MAY contain sensitive raw data and MUST remain inside vault or secure adapter boundaries.

Evidence results exposed to business clients MUST be sanitized.

TagEkyc MUST NOT collapse artifact ingestion and verification result into a single vendor-style upload-result API model.

## 6. Capture Quality and Retry Semantics

Capture quality is a first-class product concept. `CAPTURE_QUALITY` is a category, not just a single flat check.

Possible specialized capture quality checks include:

- `DOCUMENT_IMAGE_QUALITY`
- `SELFIE_IMAGE_QUALITY`
- `LIVENESS_MEDIA_QUALITY`
- `FINGERPRINT_CAPTURE_QUALITY`
- `NFC_READ_QUALITY`

S1 MAY implement only generic `CAPTURE_QUALITY`, but the product model SHOULD allow specialized sub-checks later.

Capture quality and verification outcomes SHOULD distinguish:

- `RETRY_REQUIRED`: artifact quality is insufficient; user/operator should capture again.
- `FAILED_CAPTURE_QUALITY`: artifact cannot be used for verification.
- `FAILED_IDENTITY`: identity verification failed, not merely bad capture.
- `REVIEW_REQUIRED`: evidence is ambiguous or policy requires manual review.
- `TECHNICAL_ERROR`: adapter/device/service failure.

A blurry image, poor selfie, weak NFC read, or low-quality fingerprint capture MUST NOT be treated the same as identity mismatch.

Client UX SHOULD distinguish "try again" from "identity failed".

## 7. Challenge-Bound Verification Profile

`STANDARD_EKYC_PROFILE` applies to ordinary onboarding, patient registration, account verification, or identity update flows.

`CHALLENGE_BOUND_EKYC_PROFILE` applies when a consuming client needs the eKYC result to carry an opaque caller-owned challenge for the client's own downstream binding workflow.

Challenge-bound purposes MAY include:

- signing authorization
- high-risk approval
- consent authorization
- account recovery
- sensitive identity assertion

For challenge-bound purposes, `challenge` MUST be present. `clientReference` SHOULD be present when the caller needs a separate correlation value.

The challenge is an opaque caller-supplied string. TagEkyc MUST NOT interpret it as a transaction id, document id, consent proof, nonce hash, or signing authorization.

TagEkyc MUST store, echo, and, where proof signing is enabled, sign the challenge exactly as the neutral eKYC proof requires. TagEkyc MUST NOT validate whether the challenge binds to any external transaction.

`TRANSACTION_BOUND_EKYC_PROFILE`, `externalTransactionId`, and `bindingNonceHash` are legacy compatibility aliases only. They MUST map to `CHALLENGE_BOUND_EKYC_PROFILE`, `ClientReference`, and `Challenge` and MUST NOT reintroduce transaction-aware behavior into TagEkyc.

The consuming client MUST validate its own session, transaction, consent, document, challenge, and reference binding outside TagEkyc before accepting the result for that external workflow.

If binding validation fails, the consuming client MUST reject the eKYC result for that external workflow and MUST NOT bind the evidence package to its transaction/session.

## 8. SignFlow Profile

SignFlow is the first named challenge-bound consumer profile for TagEkyc. SignFlow is a consuming client application, not the base TagEkyc platform model.

For signing authorization, SignFlow sends:

- `externalSessionId`
- `clientReference`
- `subjectRef`
- `purpose = SIGNING_AUTH`
- `challenge`
- `requiredChecks`

TagEkyc returns:

- `result`
- `assuranceLevel`
- `evidencePackageId`
- `evidencePackageHash`
- sanitized document summaries
- sanitized biometric summaries
- `manifestHash`
- verification view / neutral eKYC proof when enabled
- echoed `challenge` and `clientReference`

SignFlow MUST validate:

- `externalSessionId`
- `clientReference`
- `challenge`
- final `result`
- `evidencePackageHash`
- `manifestHash`
- evidence package / proof authenticity using the verification view when enabled
- webhook or result authenticity when that surface is implemented

SignFlow MUST reject the eKYC result if binding validation fails.

SignFlow MUST NOT use a TagEkyc result from another signing session or transaction, and TagEkyc MUST NOT decide that binding on SignFlow's behalf.

## 9. S1 Happy Path

The recommended S1 happy path is:

1. SignFlow creates a challenge-bound verification session.
2. Required checks are CCCD/NFC, face match, and liveness.
3. Fingerprint is optional/demo-only unless explicitly enabled by policy.
4. Capture agents or adapter flows record required evidence.
5. TagEkyc validates required evidence and completes the session.
6. TagEkyc creates an evidence package.
7. TagEkyc sends a completion webhook/result to SignFlow.
8. SignFlow validates `externalSessionId`, `clientReference`, `challenge`, result status, evidence hashes, and proof authenticity before using the result.
9. SignFlow binds `evidencePackageId` and `evidencePackageHash` to the signing transaction only after its own validation succeeds.

## 10. Data Return Boundary

Consumers receive sanitized verification results, `evidencePackageId`, `evidencePackageHash`, evidence refs, artifact hashes, result summaries, timestamps, and correlation fields.

Consumers MUST NOT receive raw CCCD images, raw NFC data groups, raw face images, liveness media, fingerprint images, fingerprint templates, or plaintext identity fields by default.

Raw sensitive artifacts MAY exist only inside the vault boundary or secure adapter boundary.

Any raw evidence access MUST require explicit policy, authorization, and audit.

Raw evidence access is out of scope for the S1 default consumer flow.

## 11. Signature Layers

S1 includes dev-level evidence package / neutral proof signing through TIP-66 and TIP-67B behind the `IEvidenceSigner` contract. TIP-68 dispatches production HSM custody for that existing signing surface.

The product model distinguishes:

- `payloadSignature`: protects a specific API response or payload when applicable.
- `webhookSignature`: protects callback events and SHOULD include delivery id, timestamp, and replay protection in future production design.
- `evidencePackageSignature` / neutral proof signature: protects the evidence package/proof claim for integrity, origin identification, and audit verification.

Payload signing, webhook signing, CA certificates, RFC-3161 timestamping, qualified signatures, and non-repudiation claims remain deferred surfaces unless a later reviewed TIP explicitly promotes them.

## 12. Vendor eKYC Reference Note

Vendor eKYC APIs such as Zalo/Fiza demonstrate useful reference patterns, including session-first workflow, artifact upload/capture before result retrieval, sanity/quality gates, separated OCR/face/liveness/fraud results, request id/correlation id, and signed/encrypted response considerations.

These are reference patterns only. TagEkyc MUST NOT copy vendor API semantics blindly.

TagEkyc SHOULD use normal HTTP status semantics rather than forcing every response to HTTP 200.

Vendor encryption/signature models are reference patterns, not fixed TagEkyc requirements.

TagEkyc remains platform-oriented and evidence-package-oriented.

## 13. Open Product Decisions

The following product decisions remain open before technical hardening:

- exact S1 required checks
- whether fingerprint is mandatory, optional, or demo-only
- capture agent and device gateway model
- capture artifact retention policy
- capture quality retry policy
- assurance level mapping
- webhook signature and replay protection model
- payload signature model
- production evidence/proof signing operations and rotation evidence after TIP-68 build
- requestId/correlationId conventions
- profile policy mapping
- evidence access policy
- retention and deletion policy
- plaintext identity data policy
