# Logical Data Model

**File:** `docs/lld_01_data_model_v0_1.md`
**Version:** 0.4
**Status:** Active - S1 data-model and evidence-integrity consolidation
**Date:** 2026-06-22
**Baseline:** `a98f278`
**Purpose:** Authoritative S1 logical data model and as-built evidence-integrity contract for TagEkyc. This document is not a SQL migration and does not prescribe database technology.

## Changelog

### v0.4 - TIP-66 evidence-package signing

- Added S1 package-level real JWS signing over the stable TIP-65 `manifestHash` through the `IEvidenceSigner` abstraction and local non-production ES256 dev adapter with configured P12 support.
- Recorded the internal signature envelope (`signatureFormat`, `signatureScheme`, `signatureAlgorithm`, `keyId`, `signedAt`, `signatureValue`) as manifest-row/internal-manifest metadata only; public BusinessConsumer DTOs expose only the existing signature status field.
- Marked T2-2 resolved at dev-foundation level only; production HSM/KMS/CA signing, legal sufficiency, non-repudiation, replay protection, payload signing, webhook signing, and decision-basis binding remain unresolved debt.

### v0.2 - TIP-64 S1 evidence-integrity consolidation

- Added the as-built Evidence-Integrity contract for `HashCanonical`, deterministic ids, manifest/package hash chaining, completion audit hashing, and placeholder signature statuses.
- Reconciled stale signature-field names on `evidence_results` and `evidence_packages` to as-built `...SignatureStatus` fields.
- Recorded Tier-2 open items for non-JCS canonicalization and placeholder-only signatures.

### v0.3 - TIP-65 RFC 8785 JCS canonicalization

- Replaced implementation-deterministic Web JSON hashing with RFC 8785 JCS canonicalization for evidence hash/id inputs.
- Pinned timestamp/guid value formatting and recorded durable hash metadata (`packageVersion`, `canonicalizationScheme`, `hashAlgorithm`) in the package/manifest integrity surface.
- Added post-spot-check enforcement notes: hash metadata fail-closed is enforced on package/manifest read, hashed evidence graphs forbid raw JSON numbers, and S1 retains no legacy canonicalizer/corpus.
- Marked T2-1 resolved by TIP-65 and kept T2-2 placeholder signatures open.

### v0.1 - Initial logical data model

- Defined the initial provider-neutral logical entities and artifact evidence lifecycle requirements.

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

## Evidence-Integrity

This section describes the as-built S1 evidence-integrity behavior in `VerificationCompletionApplicationService`, the domain records, and the internal manifest/BusinessConsumer contracts. Code wins over older TIP wording. This section is persistence-agnostic except for naming the integrity metadata that TIP-65 persists on package/manifest rows; it does not define append-only triggers, durability behavior, provider-specific storage behavior, or raw artifact lifecycle behavior.

### Canonicalization

`HashCanonical(label, value)` serializes `value` through the application evidence canonicalizer, pins non-JSON-native values, canonicalizes the resulting JSON with RFC 8785 JCS, then computes SHA-256 over the UTF-8 bytes of:

```text
{label}
{jcsCanonicalJson}
```

The returned hash format is `sha256:<lowercase-hex>`, enforced by `HashRef`.

JCS canonicalization sorts object member names by ordinal UTF-16 code unit order, emits minimal JSON whitespace, and uses RFC 8785 string/number rules. Property declaration order is no longer part of the canonical hash contract; tests assert field names and canonical output.

Timestamps are converted to UTC before canonicalization and formatted as `yyyy-MM-ddTHH:mm:ss.fffffffZ` using invariant culture and exactly seven fractional digits. Guids are formatted consistently with the `N` format where they enter hash/id inputs as strings.

For `rfc8785-jcs-v1`, hashed evidence graphs do not use raw JSON numbers. Numeric values that enter any hash/id evidence seed MUST be encoded as strings before canonicalization: integer values use `ToString(CultureInfo.InvariantCulture)`, and fractional/score values use `decimal` formatted as `F6` with invariant culture. Scale 6 is part of `rfc8785-jcs-v1`. The as-built S1 hashed evidence objects are string/bool/null/object/array only; tests parse every actual hash/id seed and fail if a `JsonValueKind.Number` appears. The canonicalizer's number formatting remains defensive general-purpose code and rejects NaN/Infinity, but S1 evidence is not allowed to rely on it.

The active TIP-65 metadata constants are:

| Field | Value | Surface |
| --- | --- | --- |
| `packageVersion` | `evidence-package-v2` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |
| `canonicalizationScheme` | `rfc8785-jcs-v1` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |
| `hashAlgorithm` | `sha256` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |

The public BusinessConsumer `EvidencePackageSummaryDto` remains unchanged by TIP-65: it still exposes `PackageVersion`, but it does not expose `canonicalizationScheme` or `hashAlgorithm`. The internal manifest DTO is the S1 verifier metadata surface.

### Deterministic Ids

`DeterministicGuid(label, value)` uses the same `"{label}\n{jcsCanonicalJson}"` input style as `HashCanonical`, SHA-256 hashes it, copies the first 16 bytes, sets the GUID version nibble to 5, sets the RFC 4122 variant bits, and returns a `Guid`.

The as-built labels and input fields are:

| Derived id | Label | Input field set |
| --- | --- | --- |
| `decisionId` | `tip-06-decision` | `SessionId`, `EvidenceIds`, `Result`, `AssuranceLevel`, `ForceReview`, `RequestId`, `CorrelationId`, `CompletedAt` |
| `evidencePackageId` | `tip-06-evidence-package` | `SessionId`, `DecisionId` |
| completion `auditEventId` | `tip-06-completion-audit` | `SessionId`, `PackageId` |

TIP-65 changes deterministic-id canonicalization to `rfc8785-jcs-v1`; legacy ids produced under `web-json-deterministic-v1` remain historical/localdev values and are not re-derived or mutated.

### Hash Chain

The S1 completion path builds a three-step hash chain:

| Hash | Label | Input field set |
| --- | --- | --- |
| `manifestBodyHash` | `tip-06-manifest-body` | `EvidencePackageId`, `VerificationSessionId`, `PackageVersion`, `CanonicalizationScheme`, `HashAlgorithm`, `EvidenceRefs`, `AuditEventRefs`, `ResultRef`, `Result`, `AssuranceLevel`, `RequestId`, `CorrelationId`, `CreatedAt` |
| `packageHash` | `tip-06-evidence-package` | `EvidencePackageId`, `VerificationSessionId`, `PackageVersion`, `CanonicalizationScheme`, `HashAlgorithm`, `ManifestBodyHash`, `ResultRef`, `EvidenceRefs`, `Result`, `AssuranceLevel`, `CreatedAt` |
| `manifestHash` | `tip-06-evidence-manifest` | `BodyHash`, `PackageHash` |

`PackageVersion` is `evidence-package-v2`; `CanonicalizationScheme` is `rfc8785-jcs-v1`; `HashAlgorithm` is `sha256`. `ResultRef` is the deterministic final decision id. In `manifestBodyHash`, `EvidenceRefs` are `ManifestEvidenceRefDto` values ordered by `ResultType` then evidence result id, with fields `Type`, `Id`, `VaultRef`, `ArtifactHash`, and `PayloadHash`. `VaultRef` is `null` in the as-built completion service. `AuditEventRefs` are `ManifestAuditRefDto` values with `EventId`, `EventType`, and `EventPayloadHash`.

`packageHash` uses only the selected evidence result ids for `EvidenceRefs`, in the selected evidence order used by the completion service, not the full manifest evidence-ref objects.

### Audit Hashing And Manifest Audit Refs

The as-built completion path creates exactly one completion audit event:

| Audit event | Value |
| --- | --- |
| `EventType` | `VERIFICATION_COMPLETED` |
| Event id label | `tip-06-completion-audit` |
| Payload-hash label | `tip-06-completion-audit-payload` |
| Payload-hash fields | `SessionId`, `DecisionId`, `EvidencePackageId`, `Result`, `RequestId`, `CorrelationId`, `CompletedAt` |

The manifest `AuditEventRefs` are all existing audit events for the verification session plus the new completion audit event, converted to `ManifestAuditRefDto(EventId, EventType, EventPayloadHash)`, then sorted by `EventId` using ordinal string ordering. The `EvidencePackage.AuditEventRefs` list stores the sorted audit event ids.

> note: TIP-06 section 16 described a three-event audit model (`FINAL_DECISION_CALCULATED`, `EVIDENCE_PACKAGE_CREATED`, `SESSION_COMPLETED`), a pre/post audit-ref split, and exclusion of prior events from the manifest/package refs. That model is not as-built in S1; code wins.

### Signature-Status And Package Signing Model

`SignaturePlaceholderStatus` has two values after TIP-66: `PlaceholderUnverified` and `Signed`. New TIP-66 evidence packages receive `EvidencePackageSignatureStatus = Signed`; legacy/pre-TIP-66 packages remain `PlaceholderUnverified` and must not be synthesized as signed.

As-built signature-status locations:

| Location | Field | Meaning |
| --- | --- | --- |
| `EvidenceResult` | `PayloadSignatureStatus` | Per-evidence payload signature status marker only. |
| `EvidencePackage` and package DTOs | `EvidencePackageSignatureStatus` | Package-level status. `Signed` means the package has a TIP-66 internal JWS envelope over the package manifest hash. |
| `VerificationCompletedEventDto` | `WebhookSignatureStatus` | Completion-notification projection marker only; not a persisted webhook delivery field. |

TIP-66 signs a stable attached compact JWS claim object `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}` after `manifestHash` is computed. The local dev ES256 adapter loads a configured P12 key from `TagEkyc:EvidenceSigning` when provided and otherwise uses an in-process non-production generated key for local/dev execution. The signer generates `signedAt` once and writes the same pinned UTC value into both the JWS payload and the manifest-row envelope. Signing is downstream/additive and does not feed back into `manifestBodyHash`, `packageHash`, or `manifestHash`; TIP-65 golden hash vectors remain byte-identical.

The signature envelope is internal verifier metadata with:

| Field | Meaning |
| --- | --- |
| `signatureFormat` | `JWS` format marker, not the algorithm. |
| `signatureScheme` | `jws-es256-v1` profile/version id. |
| `signatureAlgorithm` | `ES256` algorithm selector. |
| `keyId` | Signing key id from the JWS protected header `kid`. |
| `signedAt` | UTC signing timestamp. |
| `signatureValue` | Attached compact JWS value. |

The envelope is persisted on the authoritative internal manifest row and exposed through the internal `EvidenceManifestDto` only. It is not duplicated onto the package row and is not exposed through the public BusinessConsumer package summary. The package row and manifest row both retain the existing `EvidencePackageSignatureStatus` status field and must be written consistently in the same finalization transaction.

Verifier rule: a verifier selects by the recorded `signatureFormat`/`signatureScheme`/`signatureAlgorithm` plus the JWS protected header `alg`/`kid`, verifies the JWS with the recorded public-key material for that key id, and cross-checks the signed claim against persisted package/manifest metadata. Unknown scheme/algorithm, header-vs-envelope mismatch, wrong key, tampered payload/JWS, missing envelope for `Signed`, envelope material on a placeholder row, or claim-vs-row mismatch fails closed. TIP-66 implements this as production signing plus test fixtures, not as a public/runtime verifier endpoint.

`webhook_deliveries` is a deferred/conceptual entity in S1. The as-built runtime has a completion-notification projection DTO, not webhook dispatch or persisted webhook-delivery signing.

### Verifier And Legacy Rule

A verifier MUST select canonicalization and hashing by the package's own `packageVersion`, `canonicalizationScheme`, and `hashAlgorithm`, never by the latest runtime default. Unknown or inconsistent combinations fail closed. In the as-built EF read path, package and manifest rows are classified while mapping to the domain/internal manifest DTO; an unknown tuple throws a typed hash-metadata error and the package/manifest is not returned.

Legacy packages produced before TIP-65 are `packageVersion = tip-06-localdev-v1`, `canonicalizationScheme = web-json-deterministic-v1`, `hashAlgorithm = sha256`. They are historical/localdev compatibility evidence only, are not JCS-compliant, and are not production/legal-reliance evidence. If re-issued under JCS, a legacy package receives a new package/version/hash; old hashes are not mutated.

Future package field additions require a new package version/scheme mapping and must not change verification of older packages under their recorded metadata.

S1 retains no legacy canonicalizer and has no legacy hash corpus. The current legacy coverage asserts tuple classification and migration/backfill defaults only; it is not legacy hash re-verification.

Minor TIP-65 debt: `FormatNumber` is not proven against the full official RFC 8785 number-vector set. This is accepted for S1 because hashed evidence forbids raw JSON numbers and the tripwire test fails any evidence seed that would invoke number canonicalization.

### Tier-2 Open Items

T2-1 RFC 8785 JCS canonicalization is resolved by TIP-65 for the S1 evidence package hash chain.

T2-2 is resolved by TIP-66 only as an S1 package-level real JWS signing foundation over `manifestHash` using a local non-production ES256 dev/P12 adapter behind `IEvidenceSigner`. This does not resolve production HSM/KMS, CA-issued certificate signing, legal sufficiency, non-repudiation, replay protection, payload signing, webhook signing, runtime consumer verification, or decision-basis binding. Those remain outside S1/TIP-66 reliance. Link: EBS-07.

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

Key fields: `id`, `verificationSessionId`, `verificationCheckId`, `resultType`, `inputArtifactRefs`, `result`, `confidence`, `reasonCodes`, `retryReasonCode`, `sanitizedSummaryRef`, `payloadHash`, `PayloadSignatureStatus`, `engineName`, `engineVersion`, `requestId`, `correlationId`, `createdAt`.

Append-only requirement: MUST be append-only. Corrections MUST create a new evidence result version.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Evidence results MUST expose sanitized outputs only. Raw sensitive data remains in vault or secure adapter boundary.

Signature notes: `payloadSignature` was the stale v0.1 name. The as-built field is `PayloadSignatureStatus = PlaceholderUnverified`; no cryptographic payload signature exists in S1.

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

Key fields: `id`, `verificationSessionId`, `packageVersion`, `canonicalizationScheme`, `hashAlgorithm`, `manifestHash`, `evidenceRefs`, `auditEventRefs`, `resultRef`, `packageHash`, `EvidencePackageSignatureStatus`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Package MUST contain VaultRefs/hashes, not raw CCCD, face, liveness, or fingerprint data.

Signature notes: `evidencePackageSignature` was the stale v0.1 placeholder-material name. The package row keeps `EvidencePackageSignatureStatus`; TIP-66 signature material is stored on the internal manifest row only, not duplicated onto the package row. This package-level signature is distinct from per-evidence `PayloadSignatureStatus` and the completion-notification projection `WebhookSignatureStatus`.

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

Key fields (deferred/conceptual; not as-built S1): `id`, `subscriptionId`, `verificationSessionId`, `eventType`, `deliveryId`, `payloadHash`, `webhookSignature`, `signatureTimestamp`, `deliveryStatus`, `attemptCount`, `lastAttemptAt`, `nextRetryAt`, `responseStatusCode`, `responseBodyHash`, `createdAt`.

Append-only requirement: Delivery attempts SHOULD be append-only or stored as immutable attempt records. Current status MAY be a projection.

Sensitive classification: Confidential.

Raw data policy: Payload bodies SHOULD be referenced by hash/ref. Raw response bodies SHOULD NOT be stored.

Signature notes: Future production `webhookSignature` SHOULD include delivery id, timestamp, and replay protection. S1 MAY use placeholders.

> note: `webhook_deliveries` and `webhookSignature` are deferred/conceptual in S1 and are not promoted to as-built fields by TIP-64. The only as-built webhook signature status is `WebhookSignatureStatus = PlaceholderUnverified` on the `VerificationCompletedEventDto` completion-notification projection.
