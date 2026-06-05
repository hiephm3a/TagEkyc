# REST API Contracts v0.1

This document is a draft contract. It does not prescribe framework, storage, or implementation details.

## Common Rules

- APIs MUST require client application authentication unless explicitly public.
- API keys MUST be sent over TLS only.
- Mutating requests SHOULD include `Idempotency-Key`.
- Requests and responses SHOULD carry `requestId` and/or `correlationId` for support and audit.
- The authenticated `clientApplicationId` MUST be derived from the caller credential. `externalSystem` or `clientCode` MUST be derived from or validated against it.
- Responses MUST NOT include raw CCCD, raw face image, raw liveness capture, or raw fingerprint data.
- Status values MUST use explicit enums.
- APIs SHOULD use normal HTTP status semantics. TagEkyc MUST NOT copy vendor "HTTP 200 for every outcome" semantics.
- Business client scopes MUST be distinct from capture agent, device gateway, internal adapter, and operator/admin scopes.
- Error responses SHOULD use a consistent shape:

```json
{
  "error": {
    "code": "SESSION_NOT_FOUND",
    "message": "Verification session was not found.",
    "correlationId": "corr_123"
  }
}
```

## POST /api/ekyc/verification-sessions

Purpose: Creates a verification session for a client application.

Request sample:

```json
{
  "externalSessionId": "his_intake_123",
  "subjectRef": "patient_789",
  "purpose": "PATIENT_REGISTRATION",
  "profile": "STANDARD_EKYC_PROFILE",
  "requiredChecks": [
    { "checkType": "CAPTURE_QUALITY", "required": true },
    { "checkType": "DOCUMENT_NFC", "required": true },
    { "checkType": "FACE_MATCH", "required": true },
    { "checkType": "LIVENESS", "required": true }
  ],
  "expiresAt": "2026-05-24T10:00:00Z"
}
```

Response sample:

```json
{
  "verificationSessionId": "vs_01HY",
  "profile": "STANDARD_EKYC_PROFILE",
  "state": "CREATED",
  "result": "NOT_AVAILABLE",
  "requestId": "req_123",
  "correlationId": "corr_123",
  "expiresAt": "2026-05-24T10:00:00Z"
}
```

Error cases: `INVALID_API_KEY`, `UNAUTHORIZED_PURPOSE`, `INVALID_REQUIRED_CHECKS`, `DUPLICATE_EXTERNAL_SESSION`, `INVALID_BINDING_NONCE_HASH`.

Idempotency notes: Reusing the same `Idempotency-Key` with the same request SHOULD return the original session. Reusing it with a different request MUST fail.

Security notes: API key MUST be scoped for session creation. `bindingNonceHash` is not required for `STANDARD_EKYC_PROFILE`.

### Transaction-bound SignFlow create-session example

`TRANSACTION_BOUND_EKYC_PROFILE` is used when the identity result must be bound to a specific external transaction. SignFlow is the first named transaction-bound consumer profile.

```json
{
  "externalSessionId": "sf_session_123",
  "externalTransactionId": "sf_tx_456",
  "subjectRef": "patient_789",
  "purpose": "SIGNING_AUTH",
  "profile": "TRANSACTION_BOUND_EKYC_PROFILE",
  "bindingNonceHash": "sha256:abc123",
  "requiredChecks": [
    { "checkType": "CAPTURE_QUALITY", "required": true },
    { "checkType": "DOCUMENT_NFC", "required": true },
    { "checkType": "FACE_MATCH", "required": true },
    { "checkType": "LIVENESS", "required": true }
  ],
  "expiresAt": "2026-05-24T10:00:00Z"
}
```

For transaction-bound sessions, `bindingNonceHash` MUST be present and MUST be a hash, not the raw nonce. `externalSystem = SignFlow`, if exposed, MUST be derived from or validated against the authenticated `clientApplicationId`.

## GET /api/ekyc/verification-sessions/{id}

Purpose: Returns current session status and sanitized result summary.

Request sample:

```http
GET /api/ekyc/verification-sessions/vs_01HY
```

Response sample:

```json
{
  "verificationSessionId": "vs_01HY",
  "profile": "STANDARD_EKYC_PROFILE",
  "externalSessionId": "his_intake_123",
  "purpose": "PATIENT_REGISTRATION",
  "state": "COMPLETED",
  "result": "PASSED",
  "assuranceLevel": "MEDIUM",
  "evidencePackageId": "ep_01HY",
  "evidencePackageHash": "sha256:package",
  "requestId": "req_124",
  "correlationId": "corr_123",
  "completedAt": "2026-05-24T09:20:00Z"
}
```

Error cases: `SESSION_NOT_FOUND`, `FORBIDDEN_CLIENT_APPLICATION`, `SESSION_ACCESS_DENIED`.

Idempotency notes: Safe read operation.

Security notes: Caller MUST only access sessions owned by its client application unless privileged admin access is explicitly granted.

## POST /api/ekyc/verification-sessions/{id}/capture-artifacts

Purpose: Accepts captured or uploaded input artifacts from a scoped capture agent, SDK, device gateway, or secure adapter boundary.

Request sample:

```json
{
  "artifactType": "DOCUMENT_FRONT_IMAGE",
  "captureSource": "MOBILE_SDK",
  "captureAgentId": "agent_123",
  "deviceId": "device_456",
  "metadataHash": "sha256:metadata",
  "requestId": "req_125"
}
```

Response sample:

```json
{
  "captureArtifactId": "ca_01HY",
  "verificationSessionId": "vs_01HY",
  "artifactHash": "sha256:artifact",
  "accepted": true,
  "sessionState": "IN_PROGRESS",
  "correlationId": "corr_123"
}
```

Error cases: `SESSION_NOT_FOUND`, `SESSION_TERMINAL`, `ARTIFACT_TYPE_NOT_ALLOWED`, `CAPTURE_SCOPE_REQUIRED`, `VAULT_WRITE_FAILED`, `SESSION_EXPIRED`.

Idempotency notes: Repeated submissions SHOULD be deduplicated by `Idempotency-Key`, artifact hash, or capture-agent request id.

Security notes: Raw artifact bytes MUST remain inside the vault or secure adapter boundary. Business clients receive sanitized refs, hashes, summaries, and correlation fields only.

## S1 Specialized EvidenceResult Submission Endpoints

`/document-result`, `/nfc-result`, `/face-result`, `/fingerprint-result`, and `/capture-quality-result` are S1 specialized `EvidenceResult` submission endpoints. The long-term conceptual model remains `CaptureArtifact -> VerificationCheck -> EvidenceResult -> EvidencePackage`.

These endpoints MUST NOT be interpreted as a vendor-style upload-result API model. Business clients MUST NOT be trusted to submit arbitrary `PASSED` evidence.

## POST /api/ekyc/verification-sessions/{id}/document-result

Purpose: Accepts document OCR/visual inspection evidence result metadata from an internal adapter or separately scoped PoC/capture component.

Request sample:

```json
{
  "documentType": "CCCD",
  "issuingCountry": "VN",
  "documentNumberHash": "sha256:docnum",
  "fullNameHash": "sha256:name",
  "dateOfBirthHash": "sha256:dob",
  "ocrConfidence": 0.94,
  "result": "PASSED",
  "vaultRef": "vault://document/vs_01HY/doc_01",
  "artifactHash": "sha256:artifact",
  "engineName": "mock-cccd-ocr",
  "engineVersion": "s1.0",
  "requestId": "req_126",
  "correlationId": "corr_123"
}
```

Response sample:

```json
{
  "documentEvidenceId": "doc_ev_01HY",
  "accepted": true,
  "sessionState": "IN_PROGRESS"
}
```

Error cases: `SESSION_NOT_FOUND`, `SESSION_TERMINAL`, `CHECK_NOT_ALLOWED`, `INVALID_RESULT_SHAPE`, `VAULT_REF_REQUIRED`.

Idempotency notes: Repeated submissions SHOULD be deduplicated by `Idempotency-Key` or artifact hash. Corrections MUST create new append-only evidence.

Security notes: Raw document image and plaintext extracted fields MUST NOT be submitted to general business client APIs. Business clients MUST NOT be trusted to submit arbitrary `PASSED` document evidence.

## POST /api/ekyc/verification-sessions/{id}/nfc-result

Purpose: Accepts CCCD/NFC or e-document chip evidence result metadata from a scoped reader/adapter.

Request sample:

```json
{
  "documentType": "CCCD",
  "chipReadStatus": "READ_SUCCESS",
  "passiveAuthResult": "PASSED",
  "activeAuthResult": "NOT_SUPPORTED",
  "dataGroupHashes": {
    "DG1": "sha256:dg1",
    "DG2": "sha256:dg2"
  },
  "result": "PASSED",
  "vaultRef": "vault://nfc/vs_01HY/nfc_01",
  "artifactHash": "sha256:nfcartifact",
  "readerName": "mock-nfc-reader",
  "readerVersion": "s1.0",
  "requestId": "req_127",
  "correlationId": "corr_123"
}
```

Response sample:

```json
{
  "nfcEvidenceId": "nfc_ev_01HY",
  "accepted": true,
  "sessionState": "IN_PROGRESS"
}
```

Error cases: `NFC_CHECK_NOT_REQUIRED`, `INVALID_CHIP_STATUS`, `VAULT_REF_REQUIRED`, `SESSION_EXPIRED`.

Idempotency notes: Evidence MUST be append-only. Duplicate artifact hashes SHOULD be rejected or linked to the original evidence record.

Security notes: Raw NFC data groups MUST NOT be returned to consumers.

## POST /api/ekyc/verification-sessions/{id}/face-result

Purpose: Accepts face match and liveness evidence result metadata from a scoped biometric adapter.

Request sample:

```json
{
  "faceMatch": {
    "referenceSource": "DOCUMENT_NFC",
    "probeVaultRef": "vault://face/vs_01HY/probe_01",
    "referenceVaultRef": "vault://face/vs_01HY/ref_01",
    "matchScore": 0.91,
    "threshold": 0.82,
    "result": "PASSED"
  },
  "liveness": {
    "challengeType": "PASSIVE",
    "livenessScore": 0.88,
    "threshold": 0.8,
    "presentationAttackSignals": ["NONE"],
    "result": "PASSED",
    "vaultRef": "vault://liveness/vs_01HY/live_01"
  },
  "artifactHash": "sha256:faceartifact",
  "engineName": "mock-face-liveness",
  "engineVersion": "s1.0",
  "requestId": "req_128",
  "correlationId": "corr_123"
}
```

Response sample:

```json
{
  "faceEvidenceId": "face_ev_01HY",
  "livenessEvidenceId": "live_ev_01HY",
  "accepted": true,
  "sessionState": "READY_TO_COMPLETE"
}
```

Error cases: `FACE_CHECK_NOT_REQUIRED`, `LIVENESS_CHECK_NOT_REQUIRED`, `INVALID_SCORE_RANGE`, `VAULT_REF_REQUIRED`, `SESSION_TERMINAL`.

Idempotency notes: Repeated submissions SHOULD be deduplicated by `Idempotency-Key`.

Security notes: Raw face image and liveness media MUST stay in the vault boundary.

## POST /api/ekyc/verification-sessions/{id}/fingerprint-result

Purpose: Accepts fingerprint match evidence result metadata from a scoped fingerprint adapter. Fingerprint is optional/demo-only unless enabled by policy.

Request sample:

```json
{
  "fingerPosition": "RIGHT_INDEX",
  "captureDeviceId": "fp_device_001",
  "templateHash": "sha256:fptemplate",
  "matchScore": 0.87,
  "threshold": 0.8,
  "result": "PASSED",
  "vaultRef": "vault://fingerprint/vs_01HY/fp_01",
  "artifactHash": "sha256:fpartifact",
  "engineName": "mock-fingerprint",
  "engineVersion": "s1.0",
  "requestId": "req_129",
  "correlationId": "corr_123"
}
```

Response sample:

```json
{
  "fingerprintEvidenceId": "fp_ev_01HY",
  "accepted": true,
  "sessionState": "READY_TO_COMPLETE"
}
```

Error cases: `FINGERPRINT_CHECK_NOT_REQUIRED`, `UNSUPPORTED_FINGER_POSITION`, `DEVICE_NOT_TRUSTED`, `VAULT_REF_REQUIRED`.

Idempotency notes: Corrections MUST create new evidence records and MUST NOT mutate previous evidence.

Security notes: Fingerprint templates and images are Restricted data and MUST NOT be sent to SignFlow.

## POST /api/ekyc/verification-sessions/{id}/capture-quality-result

Purpose: Accepts sanitized capture quality evidence result metadata from a scoped quality adapter.

Request sample:

```json
{
  "inputCaptureArtifactIds": ["ca_01HY"],
  "checkType": "CAPTURE_QUALITY",
  "result": "RETRY_REQUIRED",
  "reasonCodes": ["BLURRY_IMAGE"],
  "retryInstructionCode": "RECAPTURE_DOCUMENT_FRONT",
  "engineName": "mock-capture-quality",
  "engineVersion": "s1.0",
  "requestId": "req_130",
  "correlationId": "corr_123"
}
```

Response sample:

```json
{
  "evidenceResultId": "evr_01HY",
  "accepted": true,
  "sessionState": "IN_PROGRESS",
  "nextAction": "RETRY_CAPTURE"
}
```

Error cases: `CAPTURE_QUALITY_CHECK_NOT_ALLOWED`, `INVALID_REASON_CODE`, `CAPTURE_ARTIFACT_NOT_FOUND`, `SESSION_TERMINAL`.

Idempotency notes: Corrections MUST create new append-only evidence results.

Security notes: `RETRY_REQUIRED` and `FAILED_CAPTURE_QUALITY` describe artifact usability, not identity mismatch.

## POST /api/ekyc/verification-sessions/{id}/complete

Purpose: Completes verification, evaluates required checks, creates final result, and builds evidence package.

Request sample:

```json
{
  "requestedBy": "client_application",
  "forceReview": false
}
```

Response sample:

```json
{
  "verificationSessionId": "vs_01HY",
  "state": "COMPLETED",
  "result": "PASSED",
  "assuranceLevel": "MEDIUM",
  "evidencePackageId": "ep_01HY",
  "evidencePackageHash": "sha256:package",
  "payloadSignature": "placeholder",
  "completedAt": "2026-05-24T09:20:00Z"
}
```

Error cases: `SESSION_NOT_FOUND`, `SESSION_EXPIRED`, `MISSING_REQUIRED_CHECK`, `CAPTURE_RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `SESSION_ALREADY_COMPLETED`, `RISK_EVALUATION_FAILED`.

Idempotency notes: Repeated completion requests after success SHOULD return the same final result and evidence package.

Security notes: Completion MUST audit decision inputs and MUST NOT expose raw evidence.

## Outgoing VerificationCompleted webhook

Purpose: Delivers sanitized completion or terminal result events to subscribed business clients.

Payload sample:

```json
{
  "eventType": "EKYC_VERIFICATION_COMPLETED",
  "deliveryId": "whd_01HY",
  "sentAt": "2026-05-24T09:20:01Z",
  "verificationSessionId": "vs_01HY",
  "profile": "STANDARD_EKYC_PROFILE",
  "externalSessionId": "his_intake_123",
  "result": "PASSED",
  "assuranceLevel": "MEDIUM",
  "evidencePackageId": "ep_01HY",
  "evidencePackageHash": "sha256:package",
  "correlationId": "corr_123",
  "webhookSignature": "placeholder"
}
```

Retry/failure result values SHOULD distinguish `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, and `TECHNICAL_ERROR` in reason codes where applicable.

Security notes: Future production `webhookSignature` SHOULD include delivery id, timestamp, and replay protection. Raw artifacts MUST NOT be included.

## GET /api/ekyc/evidence-packages/{id}

Purpose: Returns sanitized evidence package manifest.

Request sample:

```http
GET /api/ekyc/evidence-packages/ep_01HY
```

Response sample:

```json
{
  "evidencePackageId": "ep_01HY",
  "verificationSessionId": "vs_01HY",
  "packageVersion": "0.1",
  "packageHash": "sha256:package",
  "manifestHash": "sha256:manifest",
  "result": "PASSED",
  "assuranceLevel": "MEDIUM",
  "evidenceRefs": [
    { "type": "NFC", "id": "nfc_ev_01HY", "artifactHash": "sha256:nfcartifact" },
    { "type": "FACE", "id": "face_ev_01HY", "artifactHash": "sha256:faceartifact" }
  ],
  "evidencePackageSignature": "placeholder"
}
```

Error cases: `EVIDENCE_PACKAGE_NOT_FOUND`, `FORBIDDEN_CLIENT_APPLICATION`, `PACKAGE_NOT_READY`.

Idempotency notes: Safe read operation.

Security notes: Default business-consumer package responses MUST use evidence refs, package refs, artifact hashes, and sanitized summaries. Internal VaultRefs MAY appear only under explicit evidence-access policy, scoped authorization, and audit. Raw sensitive payloads MUST NOT be included. `evidencePackageSignature` is distinct from `payloadSignature` and `webhookSignature`.

## POST /api/ekyc/webhooks/retry

Purpose: Requests retry for failed webhook deliveries.

Request sample:

```json
{
  "deliveryId": "whd_01HY",
  "reason": "manual_retry"
}
```

Response sample:

```json
{
  "deliveryId": "whd_01HY",
  "deliveryStatus": "RETRY_SCHEDULED",
  "nextRetryAt": "2026-05-24T09:25:00Z"
}
```

Error cases: `DELIVERY_NOT_FOUND`, `DELIVERY_NOT_RETRYABLE`, `FORBIDDEN_CLIENT_APPLICATION`, `RATE_LIMITED`.

Idempotency notes: Repeated retry requests SHOULD not create concurrent duplicate deliveries.

Security notes: Manual retry MUST be authenticated and audited.
