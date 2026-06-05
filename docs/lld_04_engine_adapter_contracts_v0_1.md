# Engine Adapter Contracts v0.1

This document defines conceptual language-neutral adapter contracts. S1 MAY implement mocks or PoC adapters, but application code SHOULD depend on these contracts rather than engine-specific SDKs.

## Common Adapter Rules

- Adapters MUST return structured result enums, confidence values, and reason codes.
- Adapters MUST NOT leak raw sensitive artifacts into consumer-facing payloads.
- Adapters SHOULD include `engineName`, `engineVersion`, and execution metadata.
- Adapter failures MUST distinguish business failure from technical error.
- Adapter inputs SHOULD be `CaptureArtifact` refs, VaultRefs, secure local artifact handles, or sanitized metadata.
- Adapter outputs SHOULD be sanitized `EvidenceResult` records.
- Business clients MUST NOT receive raw artifacts from adapters.

Suggested result enum: `PASSED`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, `NOT_SUPPORTED`, `TECHNICAL_ERROR`.

Result semantics:

- `RETRY_REQUIRED`: capture quality is insufficient and another capture should be attempted.
- `FAILED_CAPTURE_QUALITY`: artifact cannot be used for verification.
- `FAILED_IDENTITY`: identity verification failed, not merely poor capture.
- `REVIEW_REQUIRED`: evidence is ambiguous or policy requires manual review.
- `TECHNICAL_ERROR`: adapter, device, or service failure.

## ICaptureQualityEvaluator

Responsibility: Evaluates whether capture artifacts are usable before identity verification proceeds.

Input: Capture artifact refs or secure artifact handles, artifact type, capture/device metadata, quality policy, session context.

Output: Capture quality result, specialized quality type where available, reason codes, retry instruction code, artifact hash, engine metadata.

Error/failure model: `RETRY_REQUIRED` for remediable poor quality; `FAILED_CAPTURE_QUALITY` for unusable artifacts; `TECHNICAL_ERROR` for adapter/device/runtime failure.

Mockable in S1: Yes. S1 MAY implement only generic `CAPTURE_QUALITY`.

Production replacement notes: SHOULD support specialized checks such as `DOCUMENT_IMAGE_QUALITY`, `SELFIE_IMAGE_QUALITY`, `LIVENESS_MEDIA_QUALITY`, `FINGERPRINT_CAPTURE_QUALITY`, and `NFC_READ_QUALITY`.

## IDocumentOcrEngine

Responsibility: Extracts document metadata and confidence signals from document images.

Input: Document image VaultRef or secure local artifact handle, document type hint, issuing country hint, session context.

Output: Document type, hashed document identifiers, extracted-field hashes, OCR confidence, result enum, reason codes, artifact hash, engine metadata.

Error/failure model: `FAILED_CAPTURE_QUALITY` or `RETRY_REQUIRED` for poor/unreadable images; `FAILED_IDENTITY` for document mismatch or invalid identity evidence; `REVIEW_REQUIRED` for low confidence; `TECHNICAL_ERROR` for adapter/runtime failure.

Mockable in S1: Yes.

Production replacement notes: SHOULD be replaced with validated OCR/document verification engine. Raw OCR text handling MUST be reviewed for privacy.

## INfcDocumentReader

Responsibility: Reads and validates NFC/e-chip data for supported identity documents.

Input: NFC capture session handle, document access parameters where applicable, device metadata, session context.

Output: Chip read status, passive authentication result, active authentication result, data group hashes, result enum, VaultRef, artifact hash, reader metadata.

Error/failure model: `FAILED_IDENTITY` for authentication failure or document mismatch; `RETRY_REQUIRED` for weak/incomplete read that can be attempted again; `FAILED_CAPTURE_QUALITY` for unusable NFC capture; `NOT_SUPPORTED` for unsupported document/chip feature; `TECHNICAL_ERROR` for reader/device failure.

Mockable in S1: Yes.

Production replacement notes: Production readiness depends on certified device support, country/document compatibility, and legal acceptance of NFC validation.

## IFaceMatcher

Responsibility: Compares a probe face image against a trusted reference source.

Input: Probe face VaultRef, reference face VaultRef, matching policy, session context.

Output: Match score, threshold, result enum, reason codes, artifact hash, engine metadata.

Error/failure model: `FAILED_IDENTITY` for below-threshold match; `REVIEW_REQUIRED` for borderline score; `RETRY_REQUIRED` or `FAILED_CAPTURE_QUALITY` for poor image quality; `TECHNICAL_ERROR` for runtime failure.

Mockable in S1: Yes.

Production replacement notes: SHOULD use a production-grade biometric matcher with documented FAR/FRR characteristics and bias testing.

## ILivenessDetector

Responsibility: Determines whether a face capture represents a live person and not a presentation attack.

Input: Liveness media VaultRef, challenge metadata, capture device/browser metadata, session context.

Output: Liveness score, threshold, challenge type, presentation attack signals, result enum, artifact hash, engine metadata.

Error/failure model: `FAILED_IDENTITY` for likely spoof/presentation attack; `REVIEW_REQUIRED` for ambiguous liveness; `RETRY_REQUIRED` or `FAILED_CAPTURE_QUALITY` for poor media quality; `TECHNICAL_ERROR` for capture/runtime failure.

Mockable in S1: Yes.

Production replacement notes: Production use SHOULD rely on tested PAD/liveness capability appropriate to the assurance level.

## IFingerprintMatcher

Responsibility: Matches fingerprint capture or template against a trusted reference.

Input: Fingerprint capture VaultRef or template handle, reference template VaultRef, finger position, device metadata, matching policy.

Output: Match score, threshold, template hash, finger position, result enum, artifact hash, engine metadata.

Error/failure model: `FAILED_IDENTITY` for below-threshold match; `REVIEW_REQUIRED` for ambiguous score; `RETRY_REQUIRED` or `FAILED_CAPTURE_QUALITY` for poor capture quality; `TECHNICAL_ERROR` for device or SDK failure.

Mockable in S1: Yes.

Production replacement notes: Requires hardware-specific integration, device trust model, secure template handling, and privacy controls.

## IRiskEvaluator

Responsibility: Aggregates required checks, evidence signals, policy thresholds, and reason codes into final verification result and assurance level.

Input: Verification session, RequiredChecks policy, evidence summaries, client/purpose policy, optional fraud/risk signals.

Output: Final result, assurance level, risk score, failed checks, completed checks, decision reason codes.

Error/failure model: `FAILED_IDENTITY` for policy-confirmed identity failure; `FAILED_CAPTURE_QUALITY` for unusable required artifacts; `REVIEW_REQUIRED` for incomplete/ambiguous evidence; `TECHNICAL_ERROR` for evaluator runtime failure.

Mockable in S1: Yes.

Production replacement notes: SHOULD become policy-versioned, explainable, auditable, and configurable per client/purpose.

## IEvidenceVault

Responsibility: Stores sensitive artifacts or metadata references and returns VaultRefs/hashes.

Input: Capture artifact stream or existing secure object reference, object type, session context, retention class, encryption metadata.

Output: VaultRef, content hash, storage metadata, retention metadata.

Error/failure model: `TECHNICAL_ERROR` for storage, encryption, access, or integrity failure.

Mockable in S1: Yes.

Production replacement notes: MUST use strong encryption, access control, retention enforcement, audit logging, and key management.

## IWebhookDispatcher

Responsibility: Sends event payloads to subscribed client application endpoints and tracks delivery attempts.

Input: Subscription, event type, payload, signing secret reference, retry policy.

Output: Delivery status, attempt count, response status code, payload hash, next retry timestamp.

Error/failure model: `FAILED_RETRYABLE` for transient network/5xx errors; `FAILED_FINAL` for non-retryable errors; `TECHNICAL_ERROR` for dispatcher runtime failure.

Mockable in S1: Yes.

Production replacement notes: SHOULD support `webhookSignature` with delivery id, timestamp, replay protection, idempotent delivery identifiers, retry backoff, dead-letter handling, and operator visibility.
