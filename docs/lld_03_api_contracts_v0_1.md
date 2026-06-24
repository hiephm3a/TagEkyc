# REST API Contracts

**File:** `docs/lld_03_api_contracts_v0_1.md`
**Version:** 0.5
**Status:** Active - S1 runtime-contract consolidation + TIP-67B neutral proof view
**Date:** 2026-06-23
**Baseline:** `a98f278`
**Purpose:** Authoritative S1 public API, DTO, authorization, error, and sanitization contract for the as-built TagEkyc runtime.

## Changelog

### v0.5 - TIP-67B neutral verifiable proof view

- Added `GET /api/ekyc/evidence-packages/{id}/verification-view` and `EvidencePackageVerificationViewDto`.
- Published the consumer verification contract: pinned key id plus public-key fingerprint, JWS verification, mirrored-field checks, recomputed resultHash, and challenge match.
- Preserved `EvidencePackageSummaryDto` unchanged; proof exposure is isolated to the dedicated verification-view route.

### v0.4 - TIP-67A eKYC neutrality opaque challenge

- Renamed the shipped transaction-bound profile to the neutral `ChallengeBoundEkycProfile` domain/DTO value with profile-only snake-upper wire output (`CHALLENGE_BOUND_EKYC_PROFILE`).
- Replaced public request/response semantics from `ExternalTransactionId`/`BindingNonceHash` to optional `ClientReference` plus opaque `Challenge`; old profile and old field keys are input-only compatibility aliases.
- Added challenge-bound error codes and pinned echo surfaces. `Challenge`/`ClientReference` are not included in the evidence hash graph.

### v0.3 - TIP-66 evidence-package signing enum note

- Added the minimal public-contract note that `EvidencePackageSignatureStatus` may now be `Signed` for TIP-66 packages while legacy packages remain `PlaceholderUnverified`.
- Preserved DTO shapes and public exposure boundaries: the public BusinessConsumer contracts do not expose the JWS value, signature envelope, key material, or internal manifest.

### v0.2 - TIP-63 S1 runtime-contract consolidation

- Replaced the v0.1 draft and specialized-route sketches with the six as-built routes in `VerificationSessionEndpoints`.
- Added endpoint inventory, contract DTO field tables, error-code to HTTP-status registry, and scope/caller-category catalog.
- Reconciled TIP-07/TIP-09 completion notification scope: projection-only, no public route, no webhook delivery.
- Corrected sanitization wording: default BusinessConsumer session/completion/package responses do not expose `clientApplicationId`, `PayloadHash`, `VaultRef`, raw artifacts, internal manifest, or per-evidence internals; `VerificationCompletedEventDto` is the sole completion-notification exception for `ClientApplicationId`.

## 1. Common API Rules

All public S1 routes use `X-TagEkyc-Api-Key` LocalDev authentication through `VerificationSessionEndpoints` and the LocalDev authenticator. Production credential lifecycle and production transport/security design are outside this API contract.

Errors are returned as:

```json
{
  "error": {
    "code": "SESSION_NOT_FOUND",
    "message": "Verification session was not found.",
    "correlationId": "trace-or-request-correlation"
  }
}
```

The API uses normal HTTP status semantics. It does not use vendor-style HTTP 200 for every outcome.

This document is persistence-agnostic. It does not specify database schema, FK constraints, migration behavior, append-only triggers, or durability.

## 2. Endpoint Inventory

| Method | Route | Success status | Caller category | Required scope | Contract request | Contract response |
| --- | --- | --- | --- | --- | --- | --- |
| `POST` | `/api/ekyc/verification-sessions` | `201 Created` | `BusinessConsumer` | `business.session.create` | `CreateVerificationSessionRequestDto` | `CreateVerificationSessionResponseDto` |
| `GET` | `/api/ekyc/verification-sessions/{id}` | `200 OK` | `BusinessConsumer` | `business.session.read` | route `id` | `VerificationSessionSummaryDto` |
| `POST` | `/api/ekyc/verification-sessions/{id}/capture-artifacts` | `201 Created` | `CaptureAgent` | `capture.artifact.append` | `CaptureArtifactSubmissionRequestDto` | `CaptureArtifactSubmissionResponseDto` |
| `POST` | `/api/ekyc/verification-sessions/{id}/evidence-results` | `201 Created` | `TrustedAdapter` | `trusted.evidence.append` | `EvidenceResultSubmissionRequestDto` | `EvidenceResultSubmissionResponseDto` |
| `POST` | `/api/ekyc/verification-sessions/{id}/complete` | `200 OK` | `BusinessConsumer` | `session.complete` | `CompleteVerificationSessionRequestDto` | `CompleteVerificationSessionResponseDto` |
| `GET` | `/api/ekyc/evidence-packages/{id}` | `200 OK` | `BusinessConsumer` | `business.session.read` | route `id` | `EvidencePackageSummaryDto` |
| `GET` | `/api/ekyc/evidence-packages/{id}/verification-view` | `200 OK` | `BusinessConsumer` | `business.session.read` | route `id` | `EvidencePackageVerificationViewDto` |

No public completion notification route exists. `VerificationCompletedEventDto` is returned only by `ICompletionNotificationQueries.GetCompletionNotificationAsync` as an application-service projection.

No public webhook retry route exists. No specialized evidence routes are mapped. TIP-09 records specialized evidence endpoints and webhook delivery/retry/outbox as deferred.

## 3. Contract DTO Shapes

Field names below are contract member names from `TagEkyc.Contracts/**`.

### 3.1 Common Enums

| Enum | Values |
| --- | --- |
| `VerificationProfileDto` | `StandardEkycProfile`, `ChallengeBoundEkycProfile` |
| `VerificationSessionStateDto` | `Created`, `InProgress`, `ReadyToComplete`, `Completed`, `Expired`, `Cancelled`, `TechnicalTerminal` |
| `VerificationResultDto` | `NotAvailable`, `Passed`, `RetryRequired`, `FailedCaptureQuality`, `FailedIdentity`, `ReviewRequired`, `TechnicalError`, `NotSupported` |
| `RequiredCheckTypeDto` | `CaptureQuality`, `DocumentOcr`, `DocumentNfc`, `FaceMatch`, `Liveness`, `Fingerprint`, `RiskEvaluation` |
| `AssuranceLevelDto` | `None`, `Low`, `Medium`, `High`, `Unknown` |
| `SignaturePlaceholderStatusDto` | `PlaceholderUnverified`, `Signed` |

### 3.2 CreateVerificationSessionRequestDto

| Field | Type | Notes |
| --- | --- | --- |
| `ExternalSessionId` | `string?` | Optional per-client external session id; duplicates for the same client are rejected. |
| `SubjectRef` | `string` | Required caller-owned subject reference. |
| `Purpose` | `string` | Must be allowed by LocalDev client policy. |
| `Profile` | `VerificationProfileDto` | Must be allowed by policy. |
| `RequiredChecks` | `IReadOnlyList<RequiredCheckRequestDto>` | Must be non-empty, all `Required = true`, no duplicate `CheckType`, and policy-allowed. |
| `ExpiresAt` | `DateTimeOffset` | Must be in the future and within policy TTL. |
| `ClientReference` | `string?` | Optional caller-owned correlation value. Legacy input key `externalTransactionId` is accepted input-only. |
| `Challenge` | `string?` | Required for `ChallengeBoundEkycProfile`; opaque string, 128 .NET characters or fewer, no C0/C1 controls, no trim/normalize/hash requirement. Legacy input key `bindingNonceHash` is accepted input-only. |
| `RequestId` | `string?` | Optional request id. |
| `CorrelationId` | `string?` | Optional correlation id. |

`RequiredCheckRequestDto` fields: `CheckType`, `Required`, `MinimumConfidence`. `MinimumConfidence` is present in the contract but is not used as a final-decision threshold in current S1 completion.

Profile wire output is profile-scoped snake-upper: `STANDARD_EKYC_PROFILE` and `CHALLENGE_BOUND_EKYC_PROFILE`. Input also accepts legacy `TRANSACTION_BOUND_EKYC_PROFILE`, `TransactionBoundEkycProfile`, and persisted-row alias `TransactionBoundEkycProfile`, mapping them to `ChallengeBoundEkycProfile`. If both new and legacy request field keys are present and differ, create-session fails with `CONFLICTING_CHALLENGE_FIELDS`.

### 3.3 CreateVerificationSessionResponseDto

Fields: `VerificationSessionId`, `Profile`, `State`, `Result`, `Challenge`, `ClientReference`, `RequestId`, `CorrelationId`, `ExpiresAt`.

Create responses start with `State = Created`, `Result = NotAvailable`, and later package/final-decision fields absent from this DTO.

### 3.4 VerificationSessionSummaryDto

Fields: `VerificationSessionId`, `Profile`, `ExternalSessionId`, `Challenge`, `ClientReference`, `Purpose`, `State`, `Result`, `AssuranceLevel`, `EvidencePackageId`, `EvidencePackageHash`, `ManifestHash`, `RequestId`, `CorrelationId`, `CompletedAt`.

For not-yet-completed sessions, package/hash/completed fields are null or absent by value, while state/result remain explicit. Completed summaries expose the final package/hash/manifest values and the effective completion request/correlation ids.

### 3.5 CaptureArtifactSubmissionRequestDto

Fields: `ArtifactType`, `CaptureSource`, `CaptureAgentId`, `DeviceId`, `ArtifactHash`, `MetadataHash`, `RequestId`, `CorrelationId`.

`CaptureArtifactTypeDto` values: `DocumentFrontImage`, `DocumentBackImage`, `SelfieImage`, `LivenessMedia`, `NfcReadArtifact`, `FingerprintCapture`, `DeviceCaptureMetadata`.

`CaptureSourceDto` values: `MobileSdk`, `PcAgent`, `KioskAgent`, `DeviceGateway`, `InternalAdapter`, `ExternalPreStaged`.

S1 requires at least one stable `ArtifactHash` or `MetadataHash`, and captured media/NFC/fingerprint artifacts require `ArtifactHash`. Raw artifact bytes are not a request field.

### 3.6 CaptureArtifactSubmissionResponseDto

Fields: `CaptureArtifactId`, `VerificationSessionId`, `ArtifactHash`, `Accepted`, `SessionState`, `CorrelationId`.

The response can show `SessionState = InProgress` after a capture-only write from `Created`; it cannot make a session `ReadyToComplete`.

### 3.7 EvidenceResultSubmissionRequestDto

Fields: `ResultType`, `InputCaptureArtifactIds`, `Result`, `Confidence`, `ReasonCodes`, `RetryReasonCode`, `SanitizedSummaryRef`, `PayloadHash`, `PayloadSignatureStatus`, `EngineName`, `EngineVersion`, `RequestId`, `CorrelationId`.

`EvidenceResultTypeDto` values: `CaptureQuality`, `DocumentOcr`, `NfcValidation`, `FaceMatch`, `Liveness`, `FingerprintMatch`, `FraudRisk`.

S1 requires non-empty same-session `InputCaptureArtifactIds`, compatible input artifact types, valid confidence when present, non-empty `EngineName` and `EngineVersion`, and a `PayloadHash` with `sha256:`. `FraudRisk` is deferred and rejected by current runtime.

`SanitizedSummaryRef` must not contain vault refs, raw paths, sensitive URLs, raw refs, or plaintext identity payloads. This route is for `TrustedAdapter`, not for BusinessConsumer evidence submission.

### 3.8 EvidenceResultSubmissionResponseDto

Fields: `EvidenceResultId`, `Accepted`, `SessionState`, `NextAction`.

`NextAction = "RETRY_CAPTURE"` only when the accepted result is `RetryRequired`; otherwise it is null. A first evidence write can move `Created -> InProgress -> ReadyToComplete` in the same command when it satisfies all required checks with `Passed` evidence.

### 3.9 CompleteVerificationSessionRequestDto

Fields: `ForceReview`, `RequestId`, `CorrelationId`.

`ForceReview = true` makes final result `ReviewRequired` after the request passes authorization, ownership, lifecycle, and evidence validation.

### 3.10 CompleteVerificationSessionResponseDto

Fields: `VerificationSessionId`, `State`, `Result`, `AssuranceLevel`, `FinalDecisionId`, `EvidencePackageId`, `EvidencePackageHash`, `ManifestHash`, `Challenge`, `ClientReference`, `RequestId`, `CorrelationId`, `CompletedAt`, `EvidencePackageSignatureStatus`.

The signature status is `Signed` for TIP-66 evidence packages and `PlaceholderUnverified` for legacy/pre-TIP-66 packages. This field is a status only; the public completion response does not expose the JWS value or signature envelope.

`Challenge` and `ClientReference` are echoed verbatim in create, summary, and completion responses. They are response contract fields only and do not enter `manifestBodyHash`, `packageHash`, or `manifestHash`.

### 3.11 EvidencePackageSummaryDto And EvidenceRefSummaryDto

`EvidencePackageSummaryDto` fields: `EvidencePackageId`, `VerificationSessionId`, `PackageVersion`, `PackageHash`, `ManifestHash`, `Result`, `AssuranceLevel`, `EvidenceRefs`, `EvidencePackageSignatureStatus`, `RequestId`, `CorrelationId`, `CompletedAt`.

`EvidenceRefSummaryDto` fields: `ResultType`, `EvidenceResultId`, `Type`, `Id`, `ArtifactHash`.

`Type` and `Id` are compatibility aliases for the public summary. Public package summaries do not expose internal manifest records or per-evidence payload internals.

### 3.12 VerificationCompletedEventDto

Fields: `EventType`, `DeliveryId`, `SentAt`, `VerificationSessionId`, `ClientApplicationId`, `Profile`, `ExternalSessionId`, `Result`, `AssuranceLevel`, `EvidencePackageId`, `EvidencePackageHash`, `ManifestHash`, `RequestId`, `CorrelationId`, `CompletedAt`, `WebhookSignatureStatus`, `EvidencePackageSignatureStatus`.

`EventType` is `VERIFICATION_COMPLETED`; `DeliveryId` is `localdev-not-dispatched`; `SentAt = CompletedAt`; `WebhookSignatureStatus` stays `PlaceholderUnverified`; `EvidencePackageSignatureStatus` follows the package status (`Signed` for TIP-66 packages, `PlaceholderUnverified` for legacy packages).

> note: TIP-06 section 20 used the stale event literal `EKYC_VERIFICATION_COMPLETED`. The as-built event literal is `VERIFICATION_COMPLETED`.

### 3.13 EvidencePackageVerificationViewDto

Fields: `ProofVersion`, `Purpose`, `SessionId`, `IdentityRef`, `PackageId`, `PackageVersion`, `CanonicalizationScheme`, `HashAlgorithm`, `Result`, `AssuranceLevel`, `RequiredChecks`, `CompletedChecks`, `EvidenceEngines`, `SignedAt`, `Challenge`, `ClientReference`, `SignedManifestHash`, `ResultHash`, `ResultHashAlgorithm`, `ResultHashCanonicalizationScheme`, `SignatureValue`, `SignatureFormat`, `SignatureScheme`, `SignatureAlgorithm`, `KeyId`, `PublicKeyJwk`, `PublicKeyFingerprint`.

`EvidenceEngines` entries expose `EvidenceResultType`, `EvidenceResultId`, `EngineName`, `EngineVersion`, and `CheckType`. `IdentityRef` is always hashed; raw `SubjectRef` is not exposed. `Challenge` is signed; `ClientReference` is an unsigned correlation echo. `PublicKeyJwk` is public-only `{kty,crv,x,y}` and must not contain private `d`, certificates, P12, or key-operation metadata.

Consumers MUST verify the JWS against an out-of-band pinned `KeyId` plus `PublicKeyFingerprint`, recompute `ResultHash` from the decoded signed claim, compare every mirrored view field to the decoded claim, and compare `Challenge` to the expected client challenge. Consumers MUST NOT trust the embedded JWK without the pinned fingerprint.

## 4. Scope And Caller Catalog

| Scope | Caller category | Endpoint(s) |
| --- | --- | --- |
| `business.session.create` | `BusinessConsumer` | `POST /api/ekyc/verification-sessions` |
| `business.session.read` | `BusinessConsumer` | `GET /api/ekyc/verification-sessions/{id}`, `GET /api/ekyc/evidence-packages/{id}`, `GET /api/ekyc/evidence-packages/{id}/verification-view`, application-service completion notification projection |
| `capture.artifact.append` | `CaptureAgent` | `POST /api/ekyc/verification-sessions/{id}/capture-artifacts` |
| `trusted.evidence.append` | `TrustedAdapter` | `POST /api/ekyc/verification-sessions/{id}/evidence-results` |
| `session.complete` | `BusinessConsumer` | `POST /api/ekyc/verification-sessions/{id}/complete` |

Wrong caller category returns `CALLER_CATEGORY_NOT_ALLOWED` with HTTP 403 from application authorization. A valid category missing the required scope returns `MISSING_SCOPE` with HTTP 403.

## 5. Error-Code To HTTP-Status Registry

| Error code | HTTP status | Endpoint(s) / boundary |
| --- | --- | --- |
| `INVALID_API_KEY` | 401 | Any public route authentication |
| `API_KEY_REVOKED` | 401 | Any public route authentication |
| `API_KEY_EXPIRED` | 401 | Any public route authentication |
| `CLIENT_APPLICATION_DISABLED` | 401 | Authentication or create policy |
| `MISSING_SCOPE` | 403 | Any route requiring an absent scope |
| `CALLER_CATEGORY_NOT_ALLOWED` | 403 | Any application service category mismatch |
| `SESSION_ACCESS_DENIED` | 403 | Policy missing or cross-client write path |
| `FORBIDDEN_CLIENT_APPLICATION` | 403 | Cross-client BusinessConsumer read/complete/package/notification |
| `UNAUTHORIZED_PROFILE` | 403 | Create session |
| `UNAUTHORIZED_PURPOSE` | 403 | Create session |
| `CHALLENGE_BOUND_NOT_ALLOWED` | 403 | Create session |
| `INVALID_REQUIRED_CHECKS` | 400 or 403 | Create session; 400 for malformed/duplicate/non-required, 403 for policy-denied checks |
| `VALIDATION_ERROR` | 400 | Create session shape/expiry validation |
| `MISSING_CHALLENGE` | 400 | Create challenge-bound session |
| `INVALID_CHALLENGE` | 400 | Create challenge-bound session |
| `CHALLENGE_TOO_LONG` | 400 | Create challenge-bound session |
| `CONFLICTING_CHALLENGE_FIELDS` | 400 | Create challenge-bound session with differing new/legacy field values |
| `DUPLICATE_EXTERNAL_SESSION` | 409 | Create session |
| `SESSION_NOT_FOUND` | 404 | Session read/write/complete/package/notification when id is malformed, unknown, or owning session missing |
| `CAPTURE_AGENT_MISMATCH` | 403 | Capture artifact append |
| `FINGERPRINT_NOT_ENABLED` | 403 | Capture artifact append |
| `ARTIFACT_TYPE_NOT_ALLOWED` | 403 | Capture artifact append |
| `INVALID_CAPTURE_ARTIFACT` | 400 | Capture artifact append |
| `INVALID_HASH_REF` | 400 | Capture artifact append or evidence result append |
| `SESSION_EXPIRED` | 403 | Capture/evidence writable-session load; complete when expired |
| `SESSION_READY_TO_COMPLETE` | 409 | Capture/evidence writes after `ReadyToComplete` |
| `SESSION_TERMINAL` | 409 | Capture/evidence writes after terminal state; complete for `Cancelled` or `TechnicalTerminal` |
| `FRAUD_RISK_DEFERRED` | 403 | Evidence result append |
| `CHECK_NOT_ALLOWED` | 403 | Evidence result append |
| `INPUT_CAPTURE_ARTIFACTS_REQUIRED` | 400 | Evidence result append |
| `CAPTURE_ARTIFACT_NOT_FOUND` | 404 | Evidence result append |
| `INVALID_EVIDENCE_RESULT` | 400 or 409 | Evidence result append uses 400; complete uses 409 for accepted evidence invariant failures |
| `INVALID_RESULT_STATUS` | 400 | Evidence result append |
| `INVALID_CONFIDENCE` | 400 | Evidence result append |
| `REQUIRED_EVIDENCE_MISSING` | 409 | Complete session |
| `UNSUPPORTED_EVIDENCE_RESULT` | 409 | Complete session |
| `FINALIZATION_CONFLICT` | 409 | Complete session |
| `COMPLETION_SNAPSHOT_INCOMPLETE` | 409 | Completed snapshot response path |
| `EVIDENCE_PACKAGE_NOT_FOUND` | 404 or 409 | Package read uses 404 for missing package; completed snapshot path uses 409 when expected package missing |
| `EVIDENCE_PACKAGE_VERIFICATION_VIEW_NOT_FOUND` | 404 | Verification view read when package id is malformed, missing, legacy, placeholder, or missing required proof material |
| `FINAL_DECISION_NOT_FOUND` | 409 | Evidence package read |
| `EVIDENCE_MANIFEST_NOT_FOUND` | 409 | Evidence package read |
| `SESSION_NOT_COMPLETED` | 409 | Completion notification projection |
| `COMPLETION_NOTIFICATION_INVARIANT_FAILED` | 500 | Completion notification projection |

`ToError` in `VerificationSessionEndpoints` wraps all application `SessionOperationError` values in the common error envelope with the service-provided status code.

## 6. Sanitization And Data Boundary

Default BusinessConsumer create/session summary/completion/package responses must not expose raw capture artifacts, raw biometric/document/NFC/fingerprint payloads, plaintext identity values, `VaultRef`, internal manifest objects, adapter internals, API key material, per-evidence internal detail, `PayloadHash`, or `clientApplicationId`.

`EvidencePackageSummaryDto.EvidenceRefs` exposes only public evidence refs: result type, evidence result id, compatibility aliases, and optional artifact hash. It does not expose `PayloadHash`, `SanitizedSummaryRef`, internal audit refs, internal manifest bodies, or raw inputs.

`EvidencePackageVerificationViewDto` exposes proof material only for consumer verification. It must not expose raw `SubjectRef`, plaintext identity payloads, `VaultRef`, raw artifact refs, raw biometric/document/NFC/fingerprint data, private JWK fields, certificates, P12 material, API keys, or internal manifest rows. `Challenge` and `ClientReference` are caller-supplied echoes outside the PII guarantee.

`CompleteVerificationSessionResponseDto` exposes final result and package/hash identifiers, not evidence internals.

`VerificationSessionSummaryDto` exposes `ManifestHash` and package identifiers after completion, but not internal manifest content.

Completion notification exception: `VerificationCompletedEventDto` does expose `ClientApplicationId` because it is the TIP-07 completion-notification projection contract. This exception applies only to the application-service projection and does not create a public HTTP route. Default BusinessConsumer session, completion, and package DTOs must continue to omit `ClientApplicationId`.

`WebhookSignatureStatus` remains a placeholder compatibility marker only. `EvidencePackageSignatureStatus = Signed` means an internal TIP-66 package-level JWS envelope exists over the package manifest hash, but public BusinessConsumer DTOs still do not expose the JWS value, signature envelope, key material, or verifier endpoint. The status does not claim replay protection, non-repudiation, legal audit reliance, CA compliance, or production signing.

## 7. Contract Reconciliations And Deferred Surfaces

| Surface | As-built status |
| --- | --- |
| Completion notification | `ICompletionNotificationQueries.GetCompletionNotificationAsync` application-service projection only; no public route. |
| Webhook delivery/retry/outbox | Deferred. No dispatcher, retry scheduler, route, delivery ledger, subscription model, or external HTTP delivery exists in S1. |
| Specialized evidence endpoints | Deferred. Use `POST /api/ekyc/verification-sessions/{id}/evidence-results` for current TrustedAdapter evidence recording. |
| Fingerprint | DTO/runtime type exists, but default SignFlow S1 does not require it; unauthorized fingerprint capture returns `FINGERPRINT_NOT_ENABLED`. |
| Fraud risk | DTO/runtime type exists, but current evidence recording rejects it with `FRAUD_RISK_DEFERRED`. |
| Challenge-bound SignFlow profile | Implemented as TagEkyc-owned `CHALLENGE_BOUND_EKYC_PROFILE` semantics with opaque `Challenge`, optional `ClientReference`, and test/proof composition only; legacy transaction-bound request keys are input-only compatibility aliases; no SignFlow runtime/source/database/network dependency. |

## 8. OPEN Rules Not Promoted To Authoritative Runtime

The following kickoff-era or conceptual rules are not promoted as implemented S1 API behavior:

| Rule | Disposition |
| --- | --- |
| Specialized `/document-result`, `/nfc-result`, `/face-result`, `/fingerprint-result`, and `/capture-quality-result` routes | OPEN/deferred by TIP-09; not mapped in `VerificationSessionEndpoints`. |
| Public webhook callback, retry, or delivery route | OPEN/deferred by TIP-07 and TIP-09; completion notification is projection-only. |
| Production webhook signature/replay behavior | OPEN/deferred; placeholder statuses only. |
| Production vault/storage/raw artifact lifecycle | OPEN/deferred; public S1 APIs carry hashes, sanitized refs, ids, and placeholder statuses only. |
| FraudRisk evidence recording | OPEN/deferred in code via `FRAUD_RISK_DEFERRED`. |
