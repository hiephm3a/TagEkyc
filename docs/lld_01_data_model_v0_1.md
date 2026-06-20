# Logical Data Model v0.1

This document defines the logical data model only. It is not a SQL migration and does not prescribe database technology.

## Classification

- Public: non-sensitive operational metadata.
- Internal: business metadata that should not be public.
- Confidential: identifiers, correlation values, or operational secrets.
- Restricted: raw identity document data, biometrics, biometric templates, or high-risk evidence artifacts.

## Provider-Neutral Artifact Evidence Lifecycle Design Requirements

This section carries provider-neutral artifact evidence lifecycle design requirements only. It is not a schema, migration, DTO, API contract, resolver design, storage design, access-control design, audit schema, security mechanism, legal-hold implementation, or package-builder implementation.

Durable metadata fields may hold classified metadata-safe references, hashes, identifiers, and sanitized summaries only. A metadata reference is not evidence availability proof, and a package completeness candidate is not a complete package. Artifact/raw evidence persistence remains denied unless a later reviewed storage authorization packet explicitly permits a narrow classified scope.

Packet/checklist references in this LLD are requirements to carry into later review work, not approved packets:

- Storage authorization packet for `ART-001`.
- Reference resolution packet for `ART-002`.
- Package completeness packet for `ART-003`.
- Retention/expiry packet for `ART-004`.
- Purge/disposal packet for `ART-005`.
- Legal-hold sync packet for `ART-006`.
- Access/audit/security packet for `ART-007`.
- Orphan handling packet for `ART-008`.
- Provider evidence authorization packet for `ART-009`.

The design must carry these state families as planning/design requirements only:

| State family | Required states to carry |
| --- | --- |
| Reference resolution | `NotPresent`, `PresentButUnresolved`, `ResolvedAvailable`, `Missing`, `Expired`, `Deleted`, `Inaccessible`, `Unauthorized`, `Quarantined`, `OrphanSuspected` |
| Orphan handling | `NotChecked`, `NoReference`, `ReferencePresentUnresolved`, `ArtifactAvailable`, `ArtifactMissing`, `ArtifactExpired`, `ArtifactDeleted`, `ArtifactInaccessible`, `ArtifactUnauthorized`, `ArtifactQuarantined`, `OrphanSuspected`, `OrphanConfirmed`, `Reconciled` |
| Package completeness | `NotProfiled`, `ProfiledNotChecked`, `MissingRequiredClass`, `ReferenceUnresolved`, `OrphanRiskUnresolved`, `LifecycleBlocked`, `AccessBlocked`, `Quarantined`, `ReviewPending`, `CompleteCandidate`, `CompleteForReviewedUse`, `Invalidated` |
| Retention/expiry | `RetentionUnclassified`, `RetentionClassifiedNotReviewed`, `RetainedWithinWindow`, `ReviewWindowOpen`, `ReviewWindowClosed`, `Expired`, `ExpiryUnknown`, `DisputeReviewHoldPending`, `DisputeReviewHoldAccepted`, `EnvironmentMismatch`, `ExpiredReferenceNonSuccess` |
| Purge/disposal | `DisposalUnclassified`, `DisposalNotAuthorized`, `DisposalAuthorizedNotExecuted`, `DisposalBlockedByHold`, `DisposalQuarantined`, `DisposalFailed`, `DisposalPartial`, `DisposalRetried`, `DisposedTombstoned`, `ReferenceInvalidated` |
| Legal-hold sync | `HoldUnclassified`, `HoldUnknown`, `HoldCandidate`, `HoldAccepted`, `HoldConflicted`, `HoldReleased`, `HoldRejected`, `HoldStale` |
| Access/audit/security | `AccessUnclassified`, `AccessDeniedDefault`, `AccessRestricted`, `AccessApprovedPlanning`, `AccessRevokedOrExpired`, `AccessConflicted`, `AuditExpected`, `AuditMissing`, `SecurityUnproven`, `DependencyBlocked` |

Non-success states include missing, unresolved, expired, deleted, inaccessible, unauthorized, quarantined, orphan-suspected, orphan-confirmed, inconsistent, unreviewed, hold-conflicted, access-denied, audit-missing, security-unproven, dependency-blocked, and raw-payload-denied states. These states must not support evidence availability or package completeness claims without a later reviewed packet.

`ResolvedAvailable`, `ArtifactAvailable`, `CompleteForReviewedUse`, `RetainedWithinWindow`, `DisputeReviewHoldAccepted`, `DisposedTombstoned`, `HoldAccepted`, `HoldReleased`, `HoldRejected`, and `AccessApprovedPlanning` are narrow packet-scoped states only. They are not general readiness, capability, implementation, evidence availability, package completeness, legal, audit, security, production, pilot, certification, or support claims.

Raw payload collection and persistence are denied by default. Provider-specific evidence collection requires a later reviewed provider evidence authorization packet and must STOP/RRI before any exception. Restricted artifact access requires a later reviewed access/audit/security packet and must STOP/RRI before access is treated as authorized.

STOP/RRI is required before runtime implementation, provider-specific evidence collection, raw payload handling, artifact/raw evidence persistence, restricted artifact access, or any claim that this LLD section provides readiness, legal, audit, security, production, pilot, certification, support, evidence availability, package completeness, or capability proof.

Existing sequence, API, and adapter LLD wording that mentions vault, storage, package, or artifact handling is governed by this lifecycle section. Those existing mentions do not authorize artifact/raw evidence persistence, raw payload handling, resolver capability, package completeness, restricted artifact access, evidence availability, or runtime implementation.

`GOV-001` branch/deferred-scope traceability and `ART-001` through `ART-009` must be carried until later reviewed TIPs resolve them beyond planning/design requirements.

## Entity: client_applications

Purpose: Represents an external system allowed to create verification sessions.

Key fields: `id`, `name`, `status`, `allowedPurposes`, `allowedChecks`, `webhookBaseUrl`, `createdAt`, `disabledAt`.

Append-only requirement: Not append-only, but status changes MUST be audited.

Sensitive classification: Confidential.

Raw data policy: Raw identity or biometric data MUST NOT be stored here.

## Entity: api_keys

Purpose: Authenticates client applications.

Key fields: `id`, `clientApplicationId`, `keyPrefix`, `keyHash`, `scopes`, `status`, `expiresAt`, `createdAt`, `revokedAt`.

Append-only requirement: Key creation and revocation MUST be audited. Key hash MUST NOT be mutated after creation.

Sensitive classification: Confidential.

Raw data policy: Only hashed API keys are allowed. Raw API keys MUST NOT be stored.

Scope notes: Ordinary business client scopes MUST be distinct from capture agent, device gateway, internal adapter, vault, and operator/admin scopes. Business clients MUST NOT be automatically trusted to submit arbitrary `PASSED` evidence.

## Entity: subjects

Purpose: Holds TagEkyc subject reference metadata when needed across sessions.

Key fields: `id`, `clientApplicationId`, `subjectRef`, `subjectType`, `createdAt`.

Append-only requirement: Not append-only, but merges and corrections MUST be audited.

Sensitive classification: Confidential.

Raw data policy: Raw CCCD, face, fingerprint, and liveness artifacts MUST NOT be stored here.

## Entity: verification_sessions

Purpose: Tracks lifecycle and root business correlation for an eKYC verification transaction. `VerificationSession` correlates the client application, purpose, `subjectRef`, profile, required checks, capture artifacts, verification checks, evidence results, audit events, evidence package, callbacks/webhooks, and expiry.

Key fields: `id`, `clientApplicationId`, `subjectId`, `profile`, `externalSystem`, `externalSessionId`, `externalTransactionId`, `purpose`, `bindingNonceHash`, `requestId`, `correlationId`, `state`, `result`, `assuranceLevel`, `expiresAt`, `createdAt`, `completedAt`.

Append-only requirement: State transitions SHOULD be represented by append-only audit events. The current row MAY store current state as a projection.

Sensitive classification: Confidential.

Raw data policy: Only correlation identifiers, policy references, and hashes are allowed.

Profile rules: `STANDARD_EKYC_PROFILE` is the generic default for ordinary identity assurance. `TRANSACTION_BOUND_EKYC_PROFILE` requires `bindingNonceHash` and transaction correlation fields by policy. `externalSystem` or `clientCode` MUST be derived from or validated against `clientApplicationId`, not trusted from the request body by itself.

Suggested states: `CREATED`, `IN_PROGRESS`, `READY_TO_COMPLETE`, `COMPLETED`, `FAILED`, `EXPIRED`, `CANCELLED`.

Suggested results: `NOT_AVAILABLE`, `PASSED`, `FAILED`, `REVIEW_REQUIRED`, `EXPIRED`, `ERROR`.

## Entity: required_checks

Purpose: Stores the policy required for a verification session.

Key fields: `id`, `verificationSessionId`, `checkType`, `required`, `minimumConfidence`, `policyVersion`, `profile`, `createdAt`.

Append-only requirement: MUST be append-only for a session. Policy changes MUST create a new version or new session.

Sensitive classification: Internal.

Raw data policy: No raw sensitive data allowed.

Suggested check types: `CAPTURE_QUALITY`, `DOCUMENT_OCR`, `DOCUMENT_NFC`, `FACE_MATCH`, `LIVENESS`, `FINGERPRINT`, `RISK_EVALUATION`.

Future specialized capture quality check types MAY include `DOCUMENT_IMAGE_QUALITY`, `SELFIE_IMAGE_QUALITY`, `LIVENESS_MEDIA_QUALITY`, `FINGERPRINT_CAPTURE_QUALITY`, and `NFC_READ_QUALITY`.

## Entity: capture_artifacts

Purpose: Represents captured, uploaded, or received input artifacts for a verification session.

Key fields: `id`, `verificationSessionId`, `artifactType`, `captureSource`, `captureAgentId`, `deviceId`, `vaultRef`, `artifactHash`, `metadataHash`, `qualityState`, `retryReasonCode`, `requestId`, `correlationId`, `createdAt`, `expiresAt`.

Append-only requirement: MUST be append-only. Recapture attempts MUST create new artifacts rather than replacing previous records.

Sensitive classification: Restricted.

Raw data policy: Raw document images, selfie images, liveness media, NFC read artifacts, fingerprint captures, and sensitive device metadata MUST remain inside vault or secure adapter boundaries. Business clients receive only sanitized refs, hashes, summaries, and correlation fields.

Suggested artifact types: `DOCUMENT_FRONT_IMAGE`, `DOCUMENT_BACK_IMAGE`, `SELFIE_IMAGE`, `LIVENESS_MEDIA`, `NFC_READ_ARTIFACT`, `FINGERPRINT_CAPTURE`, `DEVICE_CAPTURE_METADATA`.

Suggested quality states: `PENDING`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `ACCEPTED_FOR_VERIFICATION`, `TECHNICAL_ERROR`.

## Entity: verification_checks

Purpose: Tracks execution attempts for required or policy-driven checks against one or more capture artifacts.

Key fields: `id`, `verificationSessionId`, `requiredCheckId`, `checkType`, `status`, `attemptNumber`, `inputArtifactRefs`, `adapterName`, `adapterVersion`, `resultRef`, `reasonCodes`, `requestId`, `correlationId`, `startedAt`, `completedAt`.

Append-only requirement: SHOULD be append-only per attempt. Current check status MAY be stored as a projection.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Stores artifact refs and hashes only. Raw artifact bytes MUST NOT be stored here.

Suggested statuses: `PENDING`, `RUNNING`, `PASSED`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, `TECHNICAL_ERROR`.

## Entity: evidence_results

Purpose: Stores sanitized processed verification outputs derived from one or more capture artifacts.

Key fields: `id`, `verificationSessionId`, `verificationCheckId`, `resultType`, `inputArtifactRefs`, `result`, `confidence`, `reasonCodes`, `retryReasonCode`, `sanitizedSummaryRef`, `payloadHash`, `payloadSignature`, `engineName`, `engineVersion`, `requestId`, `correlationId`, `createdAt`.

Append-only requirement: MUST be append-only. Corrections MUST create a new evidence result version.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Evidence results MUST expose sanitized outputs only. Raw sensitive data remains in vault or secure adapter boundary.

Suggested result types: `CAPTURE_QUALITY`, `DOCUMENT_OCR`, `NFC_VALIDATION`, `FACE_MATCH`, `LIVENESS`, `FINGERPRINT_MATCH`, `FRAUD_RISK`.

Suggested result values: `PASSED`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, `TECHNICAL_ERROR`, `NOT_SUPPORTED`.

## Entity: document_evidence

Purpose: Stores specialized document OCR/visual inspection evidence result metadata derived from capture artifacts.

Key fields: `id`, `verificationSessionId`, `documentType`, `issuingCountry`, `documentNumberHash`, `fullNameHash`, `dateOfBirthHash`, `ocrConfidence`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw document images and extracted plaintext SHOULD NOT be stored in this entity. Use VaultRef/hash.

## Entity: nfc_evidence

Purpose: Stores specialized NFC/e-chip evidence result metadata for supported identity documents.

Key fields: `id`, `verificationSessionId`, `documentType`, `chipReadStatus`, `passiveAuthResult`, `activeAuthResult`, `dataGroupHashes`, `result`, `vaultRef`, `artifactHash`, `readerName`, `readerVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw NFC data groups MUST be stored only through VaultRef or omitted in S1.

## Entity: face_evidence

Purpose: Stores specialized face match evidence result metadata.

Key fields: `id`, `verificationSessionId`, `referenceSource`, `probeVaultRef`, `referenceVaultRef`, `matchScore`, `threshold`, `result`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw face images MUST NOT be returned to consumers. Storage MUST use VaultRef/hash.

## Entity: liveness_evidence

Purpose: Stores specialized liveness detection evidence result metadata.

Key fields: `id`, `verificationSessionId`, `challengeType`, `livenessScore`, `threshold`, `presentationAttackSignals`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw liveness video/images MUST use VaultRef/hash and MUST NOT be returned to consumers.

## Entity: fingerprint_evidence

Purpose: Stores specialized fingerprint match evidence result metadata.

Key fields: `id`, `verificationSessionId`, `fingerPosition`, `captureDeviceId`, `templateHash`, `matchScore`, `threshold`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw fingerprint images/templates MUST use VaultRef/hash. Consumer payloads MUST NOT include raw fingerprint data.

## Entity: verification_results

Purpose: Stores final aggregated verification decision.

Key fields: `id`, `verificationSessionId`, `result`, `assuranceLevel`, `riskScore`, `failedChecks`, `completedChecks`, `decisionReasonCodes`, `retryReasonCodes`, `createdAt`.

Append-only requirement: MUST be append-only. Corrections MUST create a new result version.

Sensitive classification: Confidential.

Raw data policy: No raw sensitive artifacts allowed.

Suggested assurance levels: `NONE`, `LOW`, `MEDIUM`, `HIGH`, `UNKNOWN`.

## Entity: evidence_packages

Purpose: Stores the manifest and integrity metadata for evidence delivered to consumers.

Key fields: `id`, `verificationSessionId`, `packageVersion`, `manifestHash`, `evidenceRefs`, `auditEventRefs`, `resultRef`, `packageHash`, `evidencePackageSignature`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Package MUST contain VaultRefs/hashes, not raw CCCD, face, liveness, or fingerprint data.

Signature notes: `evidencePackageSignature` is the long-term audit signature placeholder. It is distinct from per-payload `payloadSignature` and callback `webhookSignature`.

## Entity: vault_objects

Purpose: Represents stored evidence artifacts or references to secure external object storage.

Key fields: `id`, `vaultRef`, `objectType`, `contentHash`, `encryptionKeyRef`, `retentionClass`, `storageUri`, `createdAt`, `expiresAt`.

Append-only requirement: MUST be append-only for object metadata. Retention changes MUST be audited.

Sensitive classification: Restricted.

Raw data policy: Raw data MAY exist only inside the vault storage boundary. Application tables MUST use VaultRef/hash.

## Entity: audit_events

Purpose: Records immutable operational and evidence lifecycle events.

Key fields: `id`, `clientApplicationId`, `verificationSessionId`, `actorType`, `actorId`, `eventType`, `eventPayloadHash`, `eventPayloadRef`, `requestId`, `correlationId`, `occurredAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Confidential.

Raw data policy: Event payloads SHOULD avoid raw sensitive data. Large/sensitive payloads MUST use VaultRef/hash.

## Entity: webhook_subscriptions

Purpose: Defines callback endpoints for client applications.

Key fields: `id`, `clientApplicationId`, `eventType`, `targetUrl`, `secretRef`, `status`, `createdAt`, `disabledAt`.

Append-only requirement: Not append-only, but creation, update, and disable actions MUST be audited.

Sensitive classification: Confidential.

Raw data policy: No raw sensitive data allowed.

## Entity: webhook_deliveries

Purpose: Tracks webhook delivery attempts and retry state.

Key fields: `id`, `subscriptionId`, `verificationSessionId`, `eventType`, `deliveryId`, `payloadHash`, `webhookSignature`, `signatureTimestamp`, `deliveryStatus`, `attemptCount`, `lastAttemptAt`, `nextRetryAt`, `responseStatusCode`, `responseBodyHash`, `createdAt`.

Append-only requirement: Delivery attempts SHOULD be append-only or stored as immutable attempt records. Current status MAY be a projection.

Sensitive classification: Confidential.

Raw data policy: Payload bodies SHOULD be referenced by hash/ref. Raw response bodies SHOULD NOT be stored.

Signature notes: Future production `webhookSignature` SHOULD include delivery id, timestamp, and replay protection. S1 MAY use placeholders.
