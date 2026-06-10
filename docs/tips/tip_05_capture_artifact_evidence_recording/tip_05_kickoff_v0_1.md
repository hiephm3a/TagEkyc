# TIP-05 Capture Artifact and Evidence Result Recording Kickoff v0.1

**File:** `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_1.md`
**Version:** 0.1
**Status:** Draft for review
**Date:** 2026-06-11
**Baseline:** Product Brief v0.1.1 + TIP-03 closed + TIP-04 implemented at `5b243ee`
**Purpose:** Defines the planning boundary for TIP-05 before any capture artifact or evidence result runtime implementation is dispatched.

## Changelog

### v0.1 - Initial kickoff draft

- Opened TIP-05 planning for capture artifact metadata/reference recording and trusted evidence result recording.
- Selected a narrow S1 runtime/API shape that avoids raw payload upload and avoids broad specialized evidence endpoints before their contracts are reviewed.
- Preserved TIP-04 business session auth/policy guarantees and kept BusinessConsumer callers out of CaptureAgent and TrustedAdapter write paths.
- Added forward-compatibility boundaries for TIP-06 final decision/evidence package and TIP-07 webhook work.
- Added self-check/reviewer sections required before review handoff.

## 1. Current Baseline and Commit Anchors

Accepted baseline:

- TIP-01: CLOSED.
- TIP-02A: CLOSED.
- TIP-03: CLOSED.
- TIP-04: implemented and awaiting/ready for review under the accepted TIP-04 kickoff boundary.

Commit anchors:

- `9ab27b1` - `chore: add TIP-01 TIP-02A bootstrap baseline`
- `19aa700` - `feat: implement TIP-03 domain contracts and ports`
- `473f766` - `docs: close TIP-03`
- Current inspected HEAD: `5b243ee` - `feat: implement TIP-04 API key policy and session lifecycle`

Preflight on 2026-06-11:

- Current HEAD is exactly `5b243ee`, which satisfies the prompt gate.
- Existing dirty files before TIP-05 planning:
  - `docs/00_AGENT_COORDINATION_BUS.md`
  - `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
  - `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
  - `Note.txt`
- TIP-05 planning must not modify those files unless explicitly brought into TIP-05 planning scope.

TIP-04 delivered:

- LocalDev/NonProduction API key authentication using `X-TagEkyc-Api-Key`.
- Explicit `AuthenticatedClientContext` with caller category and scopes.
- BusinessConsumer create/read verification session endpoints.
- LocalDev client policy validation for purpose/profile/RequiredChecks/transaction-bound behavior.
- LocalDev session and audit repositories.
- Session creation in `CREATED` with result `NOT_AVAILABLE`.
- Sanitized BusinessConsumer session summary read.

TIP-04 did not deliver capture artifact runtime behavior, evidence result runtime behavior, final decision calculation, evidence package creation/retrieval, webhook runtime, production storage, production credential lifecycle, or production eKYC readiness.

## 2. TIP-05 Goal

TIP-05 may implement the first narrow S1 runtime slice for append-only capture artifact metadata/reference recording and append-only trusted evidence result recording.

TIP-05 must make it possible for a scoped CaptureAgent to record sanitized capture artifact references and for a scoped TrustedAdapter to record sanitized evidence summaries that map back to a session's RequiredChecks. TIP-05 must not calculate a final verification decision and must not create or retrieve an evidence package.

Successful TIP-05 implementation must leave a session with structured, queryable capture/evidence records that TIP-06 can later evaluate for completion and evidence packaging, while preserving correlation signals for TIP-07 webhooks.

## 3. In-Scope

- `POST /api/ekyc/verification-sessions/{id}/capture-artifacts` for CaptureAgent-scoped metadata/reference submission only.
- A generic TrustedAdapter evidence result recording endpoint:
  - `POST /api/ekyc/verification-sessions/{id}/evidence-results`
- No specialized evidence endpoints are implemented in TIP-05. Existing specialized DTOs remain contract placeholders unless a later accepted TIP authorizes runtime routes for them.
- Additive LocalDev/NonProduction auth fixture entries for CaptureAgent and TrustedAdapter callers, scoped separately from BusinessConsumer.
- Additive application port signature changes so `ICaptureArtifactCommands` and `ITrustedEvidenceResultCommands` accept `AuthenticatedClientContext caller`.
- LocalDev/NonProduction in-memory append-only repositories for capture artifacts and evidence results, hidden behind existing ports.
- Application-service enforcement that evidence result types map to session RequiredChecks and client policy.
- Append-only audit events for accepted artifact/evidence records and denied cross-boundary attempts where safe.
- TIP-05-only lifecycle movement:
  - `CREATED -> IN_PROGRESS` after the first accepted capture artifact or evidence result.
  - `IN_PROGRESS -> READY_TO_COMPLETE` only when all required checks have at least one `Passed` TrustedAdapter evidence summary under TIP-05's documented readiness heuristic.
- Unit, contract, integration/API, and architecture tests for the above behavior and non-goals.

## 4. Out-of-Scope

TIP-05 must not implement:

- Final verification decision calculation.
- Evidence package creation, manifest finalization, package signing, or evidence package retrieval.
- Webhook dispatcher/runtime/retry/signing.
- Production vault/file/object storage.
- Raw artifact upload, streaming, storage, or retrieval.
- Raw base64 image, video, NFC, biometric, fingerprint, or document payloads in default business APIs.
- BusinessConsumer ability to submit `EvidenceResult`, `PASSED`, `FAILED`, or final outcomes.
- Production adapter engines, OCR, NFC reader, face matcher, liveness detector, fingerprint matcher, fraud/risk engine, or mock engine behavior generation.
- Production API key secret lifecycle, hashing, rotation, external secret manager integration, or production credential governance.
- EF providers, `DbContext`, migrations, schema/model mapping, durable store, production DB choice, or production storage infrastructure.
- SignFlow source/database/runtime package/internal model dependency.
- Production-certified eKYC readiness claims.

## 5. Proposed Runtime/API Shape

TIP-05 selects this endpoint shape:

```text
POST /api/ekyc/verification-sessions/{id}/capture-artifacts
POST /api/ekyc/verification-sessions/{id}/evidence-results
```

Rationale:

- The capture endpoint is needed because TIP-03 already has a CaptureAgent contract and repository port for metadata/reference submission.
- A generic evidence endpoint is preferred for TIP-05 because TIP-03 already has `EvidenceResultSubmissionRequestDto`, `EvidenceResultTypeDto`, and `IEvidenceResultRepository`.
- Generic evidence recording keeps S1 narrow while preserving all result types needed by TIP-06.
- TIP-05 does not add or expose specialized evidence endpoints. This avoids a route-wiring loophole where a BusinessConsumer endpoint could accidentally reuse TrustedAdapter DTOs.

Deferred specialized endpoint candidates:

```text
POST /api/ekyc/verification-sessions/{id}/capture-quality-result
POST /api/ekyc/verification-sessions/{id}/document-result
POST /api/ekyc/verification-sessions/{id}/nfc-result
POST /api/ekyc/verification-sessions/{id}/face-result
POST /api/ekyc/verification-sessions/{id}/liveness-result
POST /api/ekyc/verification-sessions/{id}/fingerprint-result
```

These are all deferred for runtime implementation. `capture-quality-result` and `document-result` have partial existing DTOs, but TIP-05 does not expose them because generic evidence recording is enough for S1 planning and avoids widening specialized contracts prematurely. NFC, face, liveness, and fingerprint specialized DTOs are not yet reviewed runtime shapes. TIP-05 still records all allowed evidence categories through the generic TrustedAdapter endpoint using `EvidenceResultTypeDto`.

Raw upload decision:

- TIP-05 explicitly does not expose raw payload upload.
- `IFormFile`, multipart upload, base64 payload fields, filesystem paths, `FileStream`, raw NFC data groups, raw face images, raw liveness media, raw fingerprint images/templates, and plaintext document OCR fields are not allowed.
- Capture artifacts are metadata/reference records only. A future reviewed TIP must authorize any bounded upload mechanism.

## 6. Caller/Auth/Scope Model

TIP-05 must preserve TIP-04 caller categories and scopes.

BusinessConsumer:

- May continue to create and read its own sessions through TIP-04 endpoints only.
- Must not call capture artifact or evidence result write endpoints.
- Must not submit `EvidenceResult`, `PASSED`, `FAILED`, final outcomes, adapter metadata, VaultRefs, or raw artifacts.
- Must receive sanitized session summaries only.

CaptureAgent:

- May call `POST /capture-artifacts` only with `capture.artifact.append`.
- May submit sanitized metadata/reference values such as artifact type, capture source, capture agent id, device id, artifact hash, metadata hash, request id, and correlation id.
- Must not submit evidence results or final verification outcomes.
- Must not mark identity result as `PASSED` or `FAILED_IDENTITY`.

TrustedAdapter:

- May call `POST /evidence-results` with `trusted.evidence.append`.
- May submit structured, sanitized evidence summaries, including result enum, confidence, reason codes, retry reason code, payload hash, placeholder signature status, engine name/version, request id, and correlation id.
- May submit `Passed` only as a check-level evidence result, never as final verification decision.
- Must not be exposed through BusinessConsumer DTOs.

Future OperatorAdmin:

- Reserved for privileged administrative review/read/override workflows.
- TIP-05 must not add OperatorAdmin endpoints.
- Any future OperatorAdmin action requires scoped authorization and audit.

Required LocalDev auth additions:

- Add a fake LocalDev CaptureAgent key with `callerCategory = CaptureAgent` and scope `capture.artifact.append`.
- Add a fake LocalDev TrustedAdapter key with `callerCategory = TrustedAdapter` and scope `trusted.evidence.append`.
- BusinessConsumer test keys must not receive these scopes by default.
- LocalDev key names, options, fixtures, and comments must contain `LocalDev`, `DevelopmentOnly`, or `NonProduction`.

## 7. Capture Artifact Metadata/Reference Model

TIP-05 uses the existing `CaptureArtifactSubmissionRequestDto` shape:

- `ArtifactType`
- `CaptureSource`
- `CaptureAgentId`
- `DeviceId`
- `ArtifactHash`
- `MetadataHash`
- `RequestId`
- `CorrelationId`

Implementation rules:

- `verificationSessionId` comes from the route and must match an existing session.
- The authenticated CaptureAgent context must be passed explicitly into application logic.
- `ICaptureArtifactCommands.AppendCaptureArtifactAsync` must accept `AuthenticatedClientContext caller`; route-only authorization is not sufficient.
- `ArtifactHash`, when provided, must be a hash reference shape such as `sha256:<value>`.
- `MetadataHash`, when provided, must be a hash reference shape such as `sha256:<value>`.
- `VaultRef` must not be accepted from the CaptureAgent request in TIP-05.
- `VaultRef` may remain null in LocalDev records because TIP-05 is metadata/reference-only.
- `QualityState` initializes as `Pending` unless the artifact submission itself is rejected.
- Recapture attempts create new artifact records. Existing artifact records are never updated in place.
- Capture artifacts must be append-only and queryable by session id.

Response rules:

- Return `captureArtifactId`, `verificationSessionId`, optional `artifactHash`, `accepted = true`, `sessionState`, and `correlationId`.
- Do not return raw artifacts, raw paths, internal VaultRefs, raw device payloads, or sensitive metadata.

Allowed artifact types:

- `DocumentFrontImage`
- `DocumentBackImage`
- `SelfieImage`
- `LivenessMedia`
- `NfcReadArtifact`
- `FingerprintCapture`
- `DeviceCaptureMetadata`

`FingerprintCapture` is allowed only when the authenticated client policy enables `Fingerprint` as a required/optional policy check; otherwise reject it.

## 8. EvidenceResult Submission Model

TIP-05 uses the existing `EvidenceResultSubmissionRequestDto` shape:

- `ResultType`
- `InputCaptureArtifactIds`
- `Result`
- `Confidence`
- `ReasonCodes`
- `RetryReasonCode`
- `SanitizedSummaryRef`
- `PayloadHash`
- `PayloadSignatureStatus`
- `EngineName`
- `EngineVersion`
- `RequestId`
- `CorrelationId`

Result types:

- `CaptureQuality`
- `DocumentOcr`
- `NfcValidation`
- `FaceMatch`
- `Liveness`
- `FingerprintMatch`
- `FraudRisk`

Implementation rules:

- `verificationSessionId` comes from the route and must match an existing session.
- The authenticated TrustedAdapter context must be passed explicitly into application logic.
- `ITrustedEvidenceResultCommands.AppendEvidenceResultAsync` must accept `AuthenticatedClientContext caller`; route-only authorization is not sufficient.
- Input artifact ids must be valid GUID/string ids that resolve to capture artifacts on the same session when supplied.
- Evidence result records are append-only. Corrections create new records and must not mutate prior evidence.
- `PayloadHash`, when provided, must be a hash reference shape such as `sha256:<value>`.
- `PayloadSignatureStatus` remains `PlaceholderUnverified`; TIP-05 must not implement production signature verification.
- `SanitizedSummaryRef` is a sanitized logical summary reference, not a VaultRef and not raw evidence.
- `Confidence`, if provided, must be in the inclusive range `0.0` to `1.0`.
- `EngineName` and `EngineVersion` are required for TrustedAdapter submissions.
- `NotAvailable` must not be accepted as a submitted evidence result.
- `NotSupported` is rejected in TIP-05 with `INVALID_RESULT_STATUS`. Unsupported-check recording is deferred until a later TIP defines policy semantics.

Specialized existing DTO rules:

- `CaptureQualityResultSubmissionRequestDto` is not exposed as a runtime endpoint in TIP-05.
- `DocumentResultSubmissionRequestDto` is not exposed as a runtime endpoint in TIP-05.
- Any future specialized endpoint must normalize into append-only evidence records and must not add raw OCR text, document numbers, face images, NFC data groups, fingerprint templates, or VaultRefs.

## 9. RequiredChecks Linkage and Enforcement Rules

TIP-05 must map each evidence result to the session's RequiredChecks before accepting it.

Mapping:

| EvidenceResultTypeDto | RequiredCheckTypeDto |
| --- | --- |
| `CaptureQuality` | `CaptureQuality` |
| `DocumentOcr` | `DocumentOcr` |
| `NfcValidation` | `DocumentNfc` |
| `FaceMatch` | `FaceMatch` |
| `Liveness` | `Liveness` |
| `FingerprintMatch` | `Fingerprint` |
| `FraudRisk` | `RiskEvaluation` |

Enforcement:

- If the mapped check is required by the session, accept only a shape valid for that check.
- If the mapped check is not required but is allowed by authenticated client policy and TIP-05 optional checks policy is explicitly enabled for LocalDev, accept it as optional evidence.
- If the mapped check is neither required nor optional-policy-enabled, reject it with `CHECK_NOT_ALLOWED`.
- TIP-05 default is no optional evidence acceptance unless the LocalDev policy explicitly names it.
- `DocumentNfc` must not silently imply `DocumentOcr`.
- `DocumentOcr` must not satisfy `DocumentNfc`.
- `Fingerprint` remains policy-enabled/demo-only and must not become mandatory by default.
- `RiskEvaluation` remains deferred for final decision unless policy explicitly enables optional evidence recording; it must not calculate final risk or final decision in TIP-05.

Accepted evidence result values and readiness:

- `Passed`: acceptable evidence summary for the mapped check only when submitted by TrustedAdapter.
- `ReviewRequired`: recorded for TIP-06 review policy, but it does not count as clean pass and does not contribute to `READY_TO_COMPLETE`.
- `RetryRequired`: recorded for remediable capture/input issue; does not satisfy readiness for the mapped required check.
- `FailedCaptureQuality`: recorded; does not satisfy readiness.
- `FailedIdentity`: recorded; does not satisfy readiness and must not complete the session.
- `TechnicalError`: recorded; does not satisfy readiness.
- `NotSupported`: rejected in TIP-05 with `INVALID_RESULT_STATUS`.

## 10. Result Status Semantics

`RetryRequired`:

- Means another capture/read attempt may be useful.
- Common for capture quality, NFC read, liveness media, fingerprint capture, and poor document images.
- Must not be treated as identity failure.

`FailedCaptureQuality`:

- Means an artifact is not usable for verification.
- Must not be treated as identity mismatch.

`FailedIdentity`:

- Means a check-level identity assertion failed, such as face mismatch or document authentication failure.
- Must not be submitted by CaptureAgent or BusinessConsumer.
- Must not finalize the session in TIP-05.

`ReviewRequired`:

- Means evidence is ambiguous or policy wants human/manual review later.
- TIP-05 records it only; future OperatorAdmin/review work remains out of scope.

`TechnicalError`:

- Means adapter/device/service/runtime failure.
- Does not satisfy required-check readiness.

`Passed`:

- Allowed only from TrustedAdapter-scoped evidence result submission.
- Means only a check-level evidence result passed.
- Does not mean final session result is `Passed`.

## 11. Session Lifecycle Rules for TIP-05

Allowed TIP-05 lifecycle changes:

- `CREATED -> IN_PROGRESS` after the first accepted capture artifact or evidence result.
- `IN_PROGRESS -> READY_TO_COMPLETE` only when every session RequiredCheck has at least one `Passed` TrustedAdapter evidence summary under the documented heuristic below.

Readiness heuristic:

- A required check is ready when at least one append-only TrustedAdapter evidence result maps to it and has `Passed`.
- `ReviewRequired`, `RetryRequired`, `FailedCaptureQuality`, `FailedIdentity`, and `TechnicalError` are recorded but do not mark the check ready. `NotSupported` is rejected in TIP-05.
- `READY_TO_COMPLETE` means TIP-06 can attempt final decision; it does not mean final success.

Same-write precedence:

- On an accepted capture/evidence write, TIP-05 first moves `CREATED -> IN_PROGRESS` when the session is still `Created`.
- The same accepted evidence write may then move `IN_PROGRESS -> READY_TO_COMPLETE` if it causes every required check to have `Passed` TrustedAdapter evidence.
- A one-required-check session may therefore return `ReadyToComplete` after the first accepted `Passed` evidence result.
- When both transitions happen in one write, audit events must be appended in order: first `SESSION_STATE_CHANGED` for `Created -> InProgress`, then `SESSION_STATE_CHANGED` for `InProgress -> ReadyToComplete`, alongside the accepted evidence audit event.

Disallowed TIP-05 lifecycle changes:

- No transition to `COMPLETED`.
- No final `VerificationResult` calculation.
- No `AssuranceLevel` calculation.
- No evidence package id/hash assignment.
- No GET-driven expiration mutation.
- No background expiration job or durable scheduler.
- No state mutation from terminal states except future reviewed workflows.

Terminal/restricted state behavior:

- `COMPLETED`, `EXPIRED`, `CANCELLED`, and `TECHNICAL_TERMINAL` sessions must reject new capture/evidence writes with `SESSION_TERMINAL` or a more specific code.
- Expired-by-time sessions reject writes with `SESSION_EXPIRED`, but TIP-05 must not persist an expiration transition unless a later reviewed TIP authorizes it.

Repository/model note:

- Existing `VerificationSession` is immutable after creation in current code. TIP-05 implementation must add a narrow LocalDev/NonProduction state projection method behind an application/repository abstraction for only `InProgress` and `ReadyToComplete` state movement. It must not add durable persistence or final-result mutation.

## 12. Audit Event Append Rules

TIP-05 must append audit events for:

- Accepted capture artifact record: `CAPTURE_ARTIFACT_RECORDED`.
- Accepted evidence result record: `EVIDENCE_RESULT_RECORDED`.
- Session state movement caused by TIP-05, if represented as a distinct write: `SESSION_STATE_CHANGED`.
- Authenticated caller category/scope denial where safe: `CAPTURE_ARTIFACT_ACCESS_DENIED` or `EVIDENCE_RESULT_ACCESS_DENIED`.
- Cross-client/session ownership denial where safe.

Audit payload boundary:

- Include safe markers: event type, actor type, key prefix or actor id, client application id, session id where available, request id, correlation id, timestamp, result type/check type, capture artifact id/evidence result id, and payload hash/ref where appropriate.
- Do not store raw API keys.
- Do not store raw request bodies.
- Do not store raw artifacts, raw base64 payloads, raw biometric data, raw document data, raw NFC data groups, raw fingerprint data, plaintext OCR text, raw binding nonce, raw headers, or raw VaultRefs from BusinessConsumer surfaces.

Storage boundary:

- LocalDev in-memory audit append is allowed.
- Durable audit storage, retention policy, legal hold, SIEM integration, external audit reliance, or production audit assurance requires STOP+ASK.

## 13. BusinessConsumer Read-Surface Rules

TIP-05 must not broaden the BusinessConsumer read surface by default.

BusinessConsumer `GET /api/ekyc/verification-sessions/{id}` may continue to return only the sanitized TIP-04 summary:

- `verificationSessionId`
- `profile`
- `externalSessionId`
- `purpose`
- `state`
- `result`
- `assuranceLevel`
- `evidencePackageId`
- `evidencePackageHash`
- `requestId`
- `correlationId`
- `completedAt`

Before TIP-06, not-yet-completed sessions must still use:

- `result = NotAvailable` unless a previous accepted TIP explicitly sets another non-final projection.
- `assuranceLevel = None`
- `evidencePackageId = null`
- `evidencePackageHash = null`
- `completedAt = null`

BusinessConsumer responses must not include:

- Raw artifacts.
- Raw artifact paths.
- Internal VaultRefs.
- TrustedAdapter DTOs.
- CaptureAgent submission DTOs.
- Raw document/OCR/NFC/face/liveness/fingerprint payloads.
- Check-level `Passed` as final session result.
- Evidence package manifests.

TIP-05 may add an internal/application query used by tests to verify LocalDev append behavior, but it must not be a default BusinessConsumer API response.

## 14. Error/Status Response Expectations

Expected HTTP status behavior:

- `201 Created` for accepted capture artifact recording.
- `201 Created` for accepted evidence result recording.
- `400 Bad Request` for malformed ids, invalid field shape, invalid hash shape, invalid confidence range, missing engine metadata, or unsupported result shape.
- `401 Unauthorized` for missing, unknown, revoked, expired, or disabled API key.
- `403 Forbidden` for authenticated caller category/scope mismatch, cross-client access, unauthorized policy, or check not allowed.
- `404 Not Found` for missing session id or missing referenced capture artifact id.
- `409 Conflict` for terminal session state.
- `422 Unprocessable Entity` must not be introduced in TIP-05.

Expected error codes:

- `INVALID_API_KEY`
- `API_KEY_EXPIRED`
- `API_KEY_REVOKED`
- `CLIENT_APPLICATION_DISABLED`
- `MISSING_SCOPE`
- `CALLER_CATEGORY_NOT_ALLOWED`
- `SESSION_NOT_FOUND`
- `FORBIDDEN_CLIENT_APPLICATION`
- `SESSION_ACCESS_DENIED`
- `SESSION_EXPIRED`
- `SESSION_TERMINAL`
- `INVALID_CAPTURE_ARTIFACT`
- `ARTIFACT_TYPE_NOT_ALLOWED`
- `CAPTURE_ARTIFACT_NOT_FOUND`
- `INVALID_EVIDENCE_RESULT`
- `INVALID_EVIDENCE_RESULT_TYPE`
- `INVALID_RESULT_STATUS`
- `INVALID_CONFIDENCE`
- `INVALID_HASH_REF`
- `CHECK_NOT_ALLOWED`
- `OPTIONAL_CHECK_NOT_ALLOWED`
- `FINGERPRINT_NOT_ENABLED`
- `VALIDATION_ERROR`

Error envelope:

```json
{
  "error": {
    "code": "CHECK_NOT_ALLOWED",
    "message": "The evidence result type does not map to a required or policy-allowed check.",
    "correlationId": "corr_123"
  }
}
```

Precedence:

1. Authentication failure before session lookup.
2. Caller category/scope failure before request body authority is trusted.
3. Route id parse failure before repository lookup.
4. Missing session before cross-client policy checks when ownership cannot be known.
5. Cross-client access denial before capture/evidence write.
6. Terminal/expired session before appending records.
7. Request shape validation before append.
8. RequiredChecks/policy mapping before append.
9. Append-only record creation and audit.

## 15. Forward-Compatibility / No-Regret Boundaries

TIP-05 must be narrow in behavior and still avoid dead ends for TIP-06/TIP-07.

1. Capture artifacts and EvidenceResults must be structured and queryable enough for TIP-06 final decision calculation. Records must include session id, type/check mapping, result enum, reason codes, timestamps, correlation ids, input artifact ids, payload/artifact hashes, and engine metadata where applicable.
2. TIP-05 must not calculate final decision, but it must preserve enough evidence summary data for TIP-06 to decide whether checks passed, failed, require retry, require review, or ended in technical error.
3. TIP-05 must not create evidence packages, but it must preserve hashes/refs/timestamps/adapter metadata needed for future manifest generation.
4. TIP-05 must not dispatch webhooks, but it must preserve session id, client application id, external session id, request id, correlation id, state, result summary signals, and audit events needed for TIP-07.
5. RequiredChecks state must remain structured, enforceable, and queryable. TIP-05 must not collapse RequiredChecks into loose strings/lists that TIP-06 cannot reason about.
6. CaptureAgent and TrustedAdapter caller boundaries must stay clear and must not collapse into BusinessConsumer permissions.
7. LocalDev shortcuts must be explicitly named `LocalDev`, `DevelopmentOnly`, or `NonProduction`.
8. The capture artifact reference model must remain compatible with future vault-backed storage without rewriting business contracts. TIP-05 records hashes and sanitized refs now, while keeping raw bytes and VaultRef authority outside default business APIs.

Additional no-regret decisions:

- Generic evidence endpoint now, specialized endpoint expansion later.
- `Idempotency-Key` remains inert metadata only in TIP-05. Evidence and artifact records are append-only; TIP-05 does not implement same-response replay or duplicate evidence rejection.
- Append-only records now, no mutable evidence result updates.
- Check-level result recording now, final session decision later.
- LocalDev in-memory storage now, production persistence later.
- Placeholder payload signature status now, production signature verification later.

## 16. Test Plan

Unit tests:

- CaptureAgent scope/category validation rejects BusinessConsumer and TrustedAdapter callers on capture endpoint.
- TrustedAdapter scope/category validation rejects BusinessConsumer and CaptureAgent callers on evidence endpoint.
- Capture artifact request validates artifact type, hash refs, session id, and no VaultRef/body payload fields.
- Evidence result request validates result type, result status, confidence range, hash refs, engine metadata, and input artifact ids.
- Capture/evidence application commands require `AuthenticatedClientContext caller` and enforce caller category/scope in application logic.
- Evidence result type maps to RequiredChecks exactly.
- Optional evidence is rejected by default unless LocalDev policy explicitly enables it.
- `DocumentNfc` does not satisfy `DocumentOcr`; `DocumentOcr` does not satisfy `DocumentNfc`.
- `FingerprintMatch` rejects when fingerprint is not policy-enabled.
- Append-only repositories do not expose update/delete behavior.
- Session state changes from `Created` to `InProgress` after first accepted input.
- `ReadyToComplete` appears only when all required checks have `Passed` TrustedAdapter evidence summaries.
- One-required-check sessions can move from `Created` to `ReadyToComplete` on the first accepted `Passed` evidence result, with ordered state-change audit events.
- `Passed` check evidence from TrustedAdapter does not set final session result to `Passed`.
- Audit events contain safe markers and no raw payloads.

Contract tests:

- BusinessConsumer DTOs do not expose CaptureAgent or TrustedAdapter submission DTOs.
- BusinessConsumer responses do not expose raw artifacts, internal VaultRefs, raw biometric/document/NFC/fingerprint fields, or TrustedAdapter evidence shapes.
- CaptureAgent DTOs do not include final verification result authority.
- TrustedAdapter DTOs are not in the `BusinessConsumer` namespace.
- `EvidenceResultTypeDto` to `RequiredCheckTypeDto` mapping remains deterministic.
- Result status enum includes `RetryRequired`, `FailedCaptureQuality`, `FailedIdentity`, `ReviewRequired`, `TechnicalError`, and `Passed`.
- `NotSupported` remains in the enum for future compatibility but is rejected by TIP-05 runtime behavior.
- Existing `PayloadSignatureStatus` remains `PlaceholderUnverified` only.

Integration/API tests:

- Missing/unknown/revoked/expired API key behavior remains TIP-04-compatible.
- BusinessConsumer key cannot call capture/evidence endpoints.
- CaptureAgent key can append a metadata-only capture artifact.
- CaptureAgent key cannot append evidence result.
- TrustedAdapter key can append evidence result for a required check.
- TrustedAdapter key cannot append capture artifact unless separately scoped as CaptureAgent by policy.
- Cross-client access is denied.
- Missing session returns `404`.
- Terminal/expired session rejects writes.
- Invalid artifact hash/payload hash returns `400 INVALID_HASH_REF`.
- Evidence result for non-required/non-policy-allowed check returns `403 CHECK_NOT_ALLOWED`.
- Accepted capture/evidence appends audit events.
- First accepted input moves session to `InProgress`.
- All required checks with `Passed` TrustedAdapter evidence summaries moves session to `ReadyToComplete`.
- A first accepted `Passed` evidence result for a one-required-check session returns `ReadyToComplete` and appends ordered state-change audit events.
- GET BusinessConsumer summary remains sanitized and does not expose evidence internals.

Architecture/boundary tests:

- No EF provider, `DbContext`, migrations, schema/model mapping, durable store, or production database package.
- No SignFlow runtime/source/database/package/internal model dependency.
- No production secret lifecycle, hashing, rotation, external secret manager, or production credential store.
- No `IFormFile`, multipart upload, `FileStream`, base64 raw payload DTO fields, or raw artifact byte handling in API/contracts.
- No evidence package creation/retrieval endpoints.
- No webhook dispatcher/runtime/retry/signing endpoints.
- No production OCR/NFC/face/liveness/fingerprint/fraud engine implementation.
- BusinessConsumer API does not reference `TagEkyc.Contracts.TrustedAdapter` request DTOs.

Validation commands expected after implementation:

```text
dotnet test TagEkyc.sln --no-restore
rg -n "EntityFramework|DbContext|Migration|Npgsql|SqlServer|Mongo|IFormFile|FileStream|FromForm|base64|Base64|byte\\[\\]|raw|Raw|VaultRef|keyHash|secret|rotation|webhook|EvidencePackage" src tests -g "!**/bin/**" -g "!**/obj/**"
git ls-files | rg "(^|/)(bin|obj|TestResults)/"
```

Expected scan hits must be reviewed as allowed domain/internal/test guardrail mentions or STOP+ASK findings.

## 17. STOP+ASK Gates

STOP+ASK before doing any of the following:

- Implementing final verification decision calculation.
- Implementing evidence package creation, manifest calculation, evidence package signing, or evidence package retrieval.
- Implementing webhook dispatcher/runtime/retry/signing.
- Adding raw artifact upload, multipart upload, file streaming, raw base64 payloads, raw biometric data, raw document data, raw NFC data groups, raw fingerprint payloads, or plaintext OCR values.
- Accepting raw payload fields in default BusinessConsumer, CaptureAgent, or TrustedAdapter APIs.
- Exposing internal VaultRefs to BusinessConsumer responses.
- Allowing BusinessConsumer to submit `EvidenceResult`, `Passed`, `FailedIdentity`, or any final outcome.
- Reusing TrustedAdapter DTOs as BusinessConsumer request DTOs.
- Letting CaptureAgent submit check-level identity results.
- Treating TrustedAdapter `Passed` as final session `Passed`.
- Implementing production adapter engines or mock engine result generation.
- Implementing production vault/file/object storage or retention policy.
- Adding EF providers, `DbContext`, migrations, schema/model mapping, durable store, production DB choice, or production storage infrastructure.
- Implementing production API key secret lifecycle, hashing, rotation, external secret manager integration, or production credential governance.
- Adding SignFlow source/database/runtime package/internal model dependency.
- Making `Fingerprint`, `DocumentOcr`, or `RiskEvaluation` mandatory by default.
- Letting `DocumentNfc` silently imply `DocumentOcr`.
- Accepting optional evidence by default without explicit LocalDev policy.
- Accepting `NotSupported` evidence results before policy semantics are reviewed.
- Moving sessions to `Completed` or setting final result/assurance level.
- Dispatching webhooks or creating webhook delivery state.
- Claiming production-certified eKYC readiness.

## 18. Explicit Non-Production Boundaries

TIP-05 S1 runtime remains LocalDev/NonProduction only for:

- CaptureAgent and TrustedAdapter fake API keys.
- In-memory capture artifact and evidence result repositories.
- In-memory audit append behavior.
- Session state projection updates.
- Inert `Idempotency-Key` metadata with no same-response replay or duplicate evidence rejection.
- Any fake engine names, versions, payload hashes, artifact hashes, sanitized summary refs, request ids, and correlation ids used in tests.

TIP-05 must not claim:

- Production artifact storage safety.
- Production biometric/document/NFC/fingerprint privacy controls.
- Production audit durability.
- Production evidence integrity.
- Production adapter correctness.
- Production legal/compliance certification.
- Production webhook readiness.
- Production credential safety.

Before pilot/production use, separate reviewed design is required for secure storage/vault, durable persistence, retention/deletion/legal hold, privacy classification, adapter trust onboarding, credential lifecycle, rate limiting/abuse controls, monitoring, audit durability, TLS/deployment boundaries, and compliance posture.

## 19. Open Questions Requiring Homeowner/Reviewer Decision Before Implementation

No implementation-blocking open questions are intentionally left unresolved by this kickoff. TIP-05 implementation may start only after this kickoff is accepted for dispatch.

Reviewer/homeowner decisions that can be deferred without blocking TIP-05:

- Whether a later TIP should add bounded upload/vault write APIs.
- Whether specialized capture-quality/document/NFC/face/liveness/fingerprint runtime endpoints and DTOs should be drafted in a later contract-focused TIP.

Resolved by this v0.1 draft:

- TIP-05 implements only `capture-artifacts` and generic `evidence-results`.
- `capture-quality-result`, `document-result`, `nfc-result`, `face-result`, `liveness-result`, and `fingerprint-result` runtime endpoints are deferred.
- TIP-05 does not implement LocalDev duplicate evidence detection by idempotency key or artifact hash. Append-only records are allowed to contain repeated attempts.
- `ReviewRequired` is recorded but does not count toward `READY_TO_COMPLETE`.
- `NotSupported` is rejected in TIP-05.
- A single accepted `Passed` evidence write may move `Created -> InProgress -> ReadyToComplete` when it satisfies all required checks.

If reviewers consider any deferred item implementation-affecting, patch this kickoff before dispatch.

## 20. Multi-Agent Self-Check Report

Self-check method:

- Local coordinator review against TIP-03/TIP-04 boundaries and LLD01/LLD03/LLD04.
- Reviewer A: scope discipline, data boundary, security/privacy, and BusinessConsumer evidence forgery risk.
- Reviewer B: forward compatibility, no-regret architecture, RequiredChecks linkage, lifecycle, and TIP-06/TIP-07 compatibility.
- Final independent convergence check after reviewer patching.

### Reviewer A - Boundary/Security Lane

Round 1 finding table:

| Finding ID | Severity | Issue | Affected section | Recommended patch | Whether patch was applied |
| --- | --- | --- | --- | --- | --- |
| TIP05-A-01 | High | Optional `capture-quality-result` / `document-result` aliases created a route-wiring loophole where future implementation could expose TrustedAdapter DTOs through a BusinessConsumer path. | Sections 3, 5, 6, 16 | Remove aliases from TIP-05 or hard-bind every evidence endpoint to TrustedAdapter-only route groups and add negative architecture tests. | Yes. TIP-05 now implements only generic `evidence-results`; all specialized endpoints are deferred. |
| TIP05-A-02 | Medium | Counting `ReviewRequired` as readiness could make `READY_TO_COMPLETE` look like pseudo-success without any positive `Passed` evidence. | Sections 9, 11 | Require `Passed` evidence for every required check before `READY_TO_COMPLETE`, or introduce a distinct review-pending state later. | Yes. `ReviewRequired` is recorded but does not count toward `READY_TO_COMPLETE`. |

Round 2 convergence table using the same full checklist as Reviewer B:

| Finding ID | Severity | Issue | Affected section | Recommended patch | Whether patch was applied |
| --- | --- | --- | --- | --- | --- |
| NO-FINDINGS | N/A | No implementation-affecting findings remain after re-review. | Sections 3-21 | None. | No patch needed. |

Converged for review handoff: Yes.

### Reviewer B - Forward-Compatibility Lane

Round 1 finding table:

| Finding ID | Severity | Issue | Affected section | Recommended patch | Whether patch was applied |
| --- | --- | --- | --- | --- | --- |
| NO-FINDINGS-01 | None | No blocking regressions or forward-compatibility blockers identified relative to TIP-03/TIP-04 boundaries. | Sections 6, 9, 11, 15, 17, 19 | None. | No patch needed. |

Round 2 convergence table using the same full checklist as Reviewer A:

| Finding ID | Severity | Issue | Affected section | Recommended patch | Whether patch was applied |
| --- | --- | --- | --- | --- | --- |
| NO-FINDINGS-01 | None | No implementation-affecting findings remain; the patched draft is converged against the shared checklist. | Sections 1, 5, 6, 7, 8, 9, 11, 13, 14, 15, 16, 17, 18, 19 | None. | No patch needed. |

Converged for review handoff: Yes.

### Final Independent Convergence Check

Final no-regret / cross-TIP integrity finding table:

| Finding ID | Severity | Issue | Affected section | Recommended patch | Whether patch was applied |
| --- | --- | --- | --- | --- | --- |
| TIP05-FINAL-01 | Medium | Existing capture/evidence application ports lacked `AuthenticatedClientContext`, creating a risk that caller category/scope enforcement would live only in route code. | Sections 3, 6, 7, 8, 16 | Explicitly authorize additive port signature changes and tests proving application-layer caller enforcement. | Yes. Added port-signature requirement and test expectations. |
| TIP05-FINAL-02 | Medium | Lifecycle precedence was ambiguous when the first accepted evidence result also satisfied all required checks. | Sections 3, 11, 16 | Pin same-write `Created -> InProgress -> ReadyToComplete` behavior and require one-required-check tests. | Yes. Added same-write precedence and test expectations. |
| TIP05-FINAL-03 | Low | `NotSupported` acceptance depended on undefined LocalDev policy semantics. | Sections 8, 9, 16, 18 | Reject `NotSupported` in TIP-05 or define exact policy semantics. | Yes. `NotSupported` is rejected in TIP-05. |
| NO-FINDINGS | N/A | After patches, prior findings are resolved and the no-regret/cross-TIP integrity checklist is clean. | Sections 3, 6-9, 11, 13-19 | None. | No patch needed. |

Required explicit self-check answers:

1. Does this kickoff keep TIP-05 narrow without blocking TIP-06? Yes.
2. Does this kickoff keep TIP-05 narrow without blocking TIP-07? Yes.
3. Does this kickoff preserve TIP-04 auth/policy/session guarantees? Yes.
4. Could a BusinessConsumer submit `EvidenceResult` or fake `PASSED`/`FAILED` outcome? No.
5. Are raw artifacts, VaultRefs, biometric/document/NFC/fingerprint payloads kept out of BusinessConsumer contracts? Yes.
6. Are LocalDev shortcuts clearly labeled? Yes.
7. Are any temporary S1 shortcuts likely to become architectural debt? No, not as currently gated and named; they remain explicit LocalDev/NonProduction shortcuts.
8. Does any fix for an old risk introduce a new risk? No. The patches close the prior risks without creating a new implementation-affecting ambiguity.

## 21. Final Recommendation

```text
ACCEPT FOR REVIEW
```

No unresolved implementation-affecting reviewer or final no-regret findings remain after shared-checklist convergence. TIP-05 implementation is not authorized until this kickoff is accepted for dispatch.
