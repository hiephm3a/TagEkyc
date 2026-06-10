# TIP-05 Capture Artifact and Evidence Result Recording Execution Report v0.1

**File:** `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_execution_report_v0_1.md`
**Version:** 0.1
**Status:** Implemented - accepted after confirmation review
**Date:** 2026-06-11
**Baseline:** TIP-05 Kickoff v0.3 accepted + implementation HEAD `5b243ee`
**Purpose:** Records TIP-05 implementation evidence, validation output, boundary scan interpretation, and deferred items.

## Changelog

### v0.1 - TIP-05 implementation report

- Recorded implementation of LocalDev CaptureAgent artifact recording and TrustedAdapter evidence result recording.
- Captured validation output and boundary scan interpretation.
- Confirmed implementation stayed inside accepted TIP-05 v0.3 boundaries.
- Added tests-only confirmation patch with HTTP/API pipeline coverage using `Microsoft.AspNetCore.TestHost`.

## 1. Implementation Summary

Implemented within TIP-05 boundary:

- `POST /api/ekyc/verification-sessions/{id}/capture-artifacts`.
- `POST /api/ekyc/verification-sessions/{id}/evidence-results`.
- LocalDev CaptureAgent and TrustedAdapter API key records and scopes.
- Application-layer caller category/scope enforcement using `AuthenticatedClientContext`.
- CaptureAgent/TrustedAdapter write authorization against target session `clientApplicationId`.
- CaptureAgentId binding to authenticated caller policy and DeviceId metadata-only behavior.
- Append-only LocalDev in-memory capture artifact and evidence result repositories.
- Metadata/reference-only capture artifact recording with stable hash requirements.
- TrustedAdapter evidence result recording with required same-session input artifacts and required `PayloadHash`.
- ArtifactType to RequiredChecks/policy enforcement.
- EvidenceResultType to input CaptureArtifactType compatibility enforcement.
- Latest accepted evidence result readiness semantics.
- `CREATED -> IN_PROGRESS` and evidence-result-only same-write `CREATED -> IN_PROGRESS -> READY_TO_COMPLETE`.
- Rejection of further capture/evidence writes after `READY_TO_COMPLETE`.
- Audit append events for capture/evidence recording, state changes, and denied session access.
- BusinessConsumer GET remains sanitized and non-final.

## 2. Files Changed

Implementation files:

- `src/TagEkyc.Api/LocalDev/LocalDevApiKeyAuthenticator.cs`
- `src/TagEkyc.Api/Program.cs`
- `src/TagEkyc.Api/VerificationSessionEndpoints.cs`
- `src/TagEkyc.Application/AuthenticatedClientContext.cs`
- `src/TagEkyc.Application/LocalDev/LocalDevApiKeyValidator.cs`
- `src/TagEkyc.Application/LocalDev/LocalDevInMemoryRepositories.cs`
- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs`
- `src/TagEkyc.Application/Ports/ApplicationServicePorts.cs`
- `src/TagEkyc.Application/Ports/RepositoryPorts.cs`
- `src/TagEkyc.Application/VerificationSessions/LocalDevClientPolicy.cs`
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs`
- `src/TagEkyc.Domain/VerificationSession.cs`

Test files:

- `tests/TagEkyc.ArchTests/Tip04BoundaryTests.cs`
- `tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj`
- `tests/TagEkyc.UnitTests/Tip04ApiEndpointTests.cs`
- `tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs`
- `tests/TagEkyc.UnitTests/Tip05EvidenceApplicationTests.cs`

Package added for API pipeline tests:

- `Microsoft.AspNetCore.TestHost` `8.0.0`

Documentation files:

- `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_1.md`
- `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_2.md`
- `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_3.md`
- `docs/tips/tip_05_capture_artifact_evidence_recording/tip_05_execution_report_v0_1.md`

Pre-existing dirty files were not modified as part of TIP-05 implementation:

- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
- `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
- `Note.txt`

## 3. Endpoint Inventory

Allowed runtime endpoints now mapped:

```text
POST /api/ekyc/verification-sessions
GET  /api/ekyc/verification-sessions/{id}
POST /api/ekyc/verification-sessions/{id}/capture-artifacts
POST /api/ekyc/verification-sessions/{id}/evidence-results
```

Deferred specialized endpoints were not added:

```text
capture-quality-result
document-result
nfc-result
face-result
liveness-result
fingerprint-result
```

## 4. Auth/Caller Boundary Evidence

- BusinessConsumer create/read behavior remains on TIP-04 endpoints.
- CaptureAgent write path requires `callerCategory = CaptureAgent` and `capture.artifact.append`.
- TrustedAdapter write path requires `callerCategory = TrustedAdapter` and `trusted.evidence.append`.
- Wrong caller category returns `CALLER_CATEGORY_NOT_ALLOWED` before missing scope handling in the TIP-05 application service.
- CaptureAgent and TrustedAdapter writes require same `clientApplicationId` or LocalDev `allowedClientApplicationIds`.
- BusinessConsumer keys cannot submit capture artifacts or evidence results.
- TrustedAdapter DTOs are not reused in BusinessConsumer contracts.

## 5. Capture Artifact Behavior Evidence

- Capture artifact append is metadata/reference-only.
- `ArtifactHash` or `MetadataHash` is required for every artifact.
- Captured media/NFC/fingerprint artifact types require `ArtifactHash`.
- `DeviceCaptureMetadata` may use `MetadataHash` only.
- CaptureAgentId mismatch returns `CAPTURE_AGENT_MISMATCH`.
- DeviceId is stored as metadata only and does not establish device trust.
- ArtifactType policy mapping is enforced with deterministic `ARTIFACT_TYPE_NOT_ALLOWED` and `FINGERPRINT_NOT_ENABLED` behavior.
- Capture artifacts are appended and never updated in place.

## 6. Evidence Result Behavior Evidence

- Evidence result append is TrustedAdapter-only.
- `InputCaptureArtifactIds` is required for TIP-05 evidence result types.
- Input artifacts must resolve within the same verification session.
- Input artifact type compatibility is enforced per EvidenceResultType.
- `PayloadHash` is required and must use `sha256:`.
- `SanitizedSummaryRef` is optional but rejects VaultRefs, raw paths, sensitive URLs, raw refs, and plaintext identity markers.
- `FraudRisk` recording is rejected as deferred.
- `NotSupported` is rejected in TIP-05.
- Evidence results are appended and never updated in place.

## 7. RequiredChecks Linkage Evidence

- EvidenceResultType maps to RequiredCheckType before append.
- Non-required and non-policy-allowed evidence is rejected with `CHECK_NOT_ALLOWED`.
- Optional evidence requires LocalDev `allowedOptionalEvidenceChecks`.
- Supporting artifacts require LocalDev `allowedSupportingArtifactTypes`.
- `DocumentNfc` does not silently imply `DocumentOcr`.
- `NfcValidation` requires `NfcReadArtifact`.
- Fingerprint remains policy-enabled/demo-only.

## 8. Lifecycle/Readiness Evidence

- First accepted capture/evidence input moves `CREATED -> IN_PROGRESS`.
- Capture-artifact-only writes cannot move a session to `READY_TO_COMPLETE`.
- Latest accepted evidence result by append order controls readiness per RequiredCheck.
- `READY_TO_COMPLETE` requires latest accepted TrustedAdapter evidence result `Passed` for every required check.
- A first accepted `Passed` evidence result may move a one-required-check session `CREATED -> IN_PROGRESS -> READY_TO_COMPLETE`.
- After `READY_TO_COMPLETE`, further capture/evidence writes are rejected with `SESSION_READY_TO_COMPLETE`.
- No `COMPLETED` transition, final result, assurance level, or evidence package data is calculated.

## 9. BusinessConsumer Read-Surface Evidence

- BusinessConsumer GET continues to return sanitized session summary only.
- After TIP-05 `READY_TO_COMPLETE`, GET still returns:
  - `result = NotAvailable`
  - `assuranceLevel = None`
  - `evidencePackageId = null`
  - `evidencePackageHash = null`
  - `completedAt = null`
- BusinessConsumer responses do not expose raw artifacts, internal VaultRefs, capture DTOs, trusted adapter DTOs, evidence internals, or check-level `Passed` as final result.

## 10. Tests Added and Validation Output

Added/updated tests covering:

- TIP-05 route inventory and absence of specialized evidence endpoints.
- Caller category precedence and BusinessConsumer rejection on TIP-05 write paths.
- Capture artifact stable hash/reference and policy mapping.
- Fingerprint rejection determinism.
- Evidence result required `PayloadHash`.
- Evidence result required same-session input artifacts.
- EvidenceResultType to CaptureArtifactType compatibility.
- Latest-result readiness and post-ready write rejection.
- BusinessConsumer summary defaults after `READY_TO_COMPLETE`.
- Architecture boundary update for deferred runtime surfaces.
- HTTP/API pipeline coverage using `UseTestServer()` and `GetTestClient()`.
- BusinessConsumer cannot `POST /api/ekyc/verification-sessions/{id}/capture-artifacts`.
- BusinessConsumer cannot `POST /api/ekyc/verification-sessions/{id}/evidence-results`.
- CaptureAgent can `POST /api/ekyc/verification-sessions/{id}/capture-artifacts`.
- CaptureAgent cannot `POST /api/ekyc/verification-sessions/{id}/evidence-results`.
- TrustedAdapter can `POST /api/ekyc/verification-sessions/{id}/evidence-results`.
- TrustedAdapter cannot `POST /api/ekyc/verification-sessions/{id}/capture-artifacts`.
- Missing API key returns a `401` error envelope.
- Unknown API key returns a `401` error envelope.
- Error envelope contains `error.code`, `error.message`, and `error.correlationId`.
- Specialized endpoints are not mapped through the HTTP pipeline.
- HTTP happy chain: BusinessConsumer create session, CaptureAgent append artifact, TrustedAdapter append evidence, then BusinessConsumer GET.
- HTTP GET after `READY_TO_COMPLETE` remains sanitized and non-final.
- `READY_TO_COMPLETE` rejects further capture/evidence writes with `409`.
- Missing `PayloadHash` is rejected through HTTP.
- Incompatible input artifact type is rejected through HTTP.

Command:

```text
dotnet test TagEkyc.sln --no-restore
```

Result:

```text
TagEkyc.ContractTests: Passed - 8
TagEkyc.ArchTests: Passed - 13
TagEkyc.UnitTests: Passed - 26
Total: 47 passed, 0 failed, 0 skipped
```

Command:

```text
rg -n "EntityFramework|DbContext|Migration|Npgsql|SqlServer|Mongo|IFormFile|FileStream|FromForm|base64|Base64|byte\\[\\]|raw|Raw|VaultRef|keyHash|secret|rotation|webhook|EvidencePackage" src tests -g "!**/bin/**" -g "!**/obj/**"
```

Result: expected placeholders, contracts, and tests only. No scan hit indicates EF provider, `DbContext`, migrations, durable store, file upload, raw upload runtime, production auth lifecycle, production secret lifecycle, or production storage implementation.

Command:

```text
git ls-files | rg "(^|/)(bin|obj|TestResults)/"
```

Result:

```text
No tracked generated output paths.
```

## 11. Boundary Scan Interpretation

Boundary scan hits are expected:

- Existing domain/contracts include `VaultRef` and `EvidencePackage` placeholder models from TIP-03.
- BusinessConsumer summaries retain nullable evidence package fields as sanitized non-ready defaults.
- Internal manifest contracts are the only contracts with manifest VaultRefs.
- Tests intentionally assert raw/VaultRef/evidence package boundaries.
- TIP-05 service explicitly rejects raw-like `SanitizedSummaryRef` values and stores LocalDev `VaultRef = null` for capture artifacts.
- README hits are existing boundary documentation.

No scan hit indicates EF provider, `DbContext`, migrations, production database, raw upload, multipart upload, file streaming, production webhook runtime, production credential lifecycle, or production storage implementation.

## 12. Confirmation of No Scope Breach

Confirmed not implemented:

- Final verification decision calculation.
- `COMPLETED` transition.
- Final session `Passed` result.
- Assurance level calculation.
- Evidence package creation or retrieval.
- Webhook dispatcher/runtime/retry/signing.
- Raw artifact upload, multipart, `IFormFile`, `FileStream`, base64/raw payload handling.
- Production vault/file/object storage.
- BusinessConsumer evidence result submission.
- Specialized evidence runtime endpoints.
- Production adapter engines or mock engine result generation.
- EF provider, `DbContext`, migrations, schema/model mapping, durable store, or production DB.
- Production credential lifecycle/secret/hash/rotation/external secret manager.
- SignFlow source/database/runtime/internal dependency.
- FraudRisk runtime recording.
- NotSupported evidence acceptance.
- READY_TO_COMPLETE demotion/correction flow.

## 13. Residual Risks and Deferred Items

- All TIP-05 runtime storage is LocalDev/NonProduction in-memory only.
- Durable persistence and production storage remain deferred.
- Production vault/storage, retention, and privacy controls remain deferred.
- Production CaptureAgent/device trust and adapter onboarding remain deferred.
- Final decision/evidence package remains TIP-06.
- Webhook runtime remains TIP-07.
- Specialized evidence endpoints remain deferred.
- FraudRisk and `NotSupported` policy semantics remain deferred.
- Post-`READY_TO_COMPLETE` correction/demotion behavior remains deferred.

## 14. Recommendation for Review

```text
ACCEPT FOR REVIEW
```

TIP-05 implementation is complete within the accepted v0.3 kickoff boundaries and is ready for review.
