# TIP-04 API Key Authentication, Client Policy, and Session Lifecycle Kickoff v0.2

**File:** `docs/tips/tip_04_api_key_policy_session_lifecycle/tip_04_kickoff_v0_2.md`
**Version:** 0.2
**Status:** Accept with minor patches applied - ready for review
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1 + TIP-03 accepted closeout
**Purpose:** Defines the planning boundary for TIP-04 before any runtime implementation is dispatched.

## Changelog

### v0.2 - Review minor patches applied

- Pinned `X-TagEkyc-Api-Key` as the single S1 local/dev API key transport.
- Pinned `Idempotency-Key` as accepted inert metadata only in TIP-04, with no reliable/durable idempotency behavior.
- Pinned duplicate `externalSessionId` behavior to local/dev per-client rejection with `409 DUPLICATE_EXTERNAL_SESSION`.
- Pinned `Required = false` RequiredChecks rejection with `INVALID_REQUIRED_CHECKS`.
- Pinned expiration behavior so GET does not mutate sessions to `EXPIRED` in TIP-04.
- Clarified LocalDev audit behavior for successful session creation, authenticated cross-client access denied, and optional safe-marker failed authentication events.

### v0.1 - Initial kickoff draft

- Opened TIP-04 planning for S1 local/dev API key authentication, client policy validation, session create/get runtime behavior, lifecycle foundation, and audit append behavior.
- Preserved TIP-05 capture/evidence recording, TIP-06 completion/evidence package calculation, and TIP-07 webhook runtime as explicit future work.
- Added forward-compatibility boundaries so TIP-04 stays narrow without forcing later rewrites of session identity, policy, scoped credentials, or lifecycle foundations.
- Added subagent self-check results and patched authenticated caller context, inert webhook metadata, and local/dev shortcut naming guardrails.

## 1. Current Baseline and Commit Anchors

Accepted baseline:

- TIP-01 Project Skeleton: fully accepted and closed.
- TIP-02A: accepted and closed.
- TIP-03 Kickoff v0.2: accepted.
- TIP-03 Implementation: fully accepted and closed.

Commit anchors:

- `9ab27b1` - `chore: add TIP-01 TIP-02A bootstrap baseline`
- `19aa700` - `feat: implement TIP-03 domain contracts and ports`
- Current inspected HEAD: `473f766` - `docs: close TIP-03`

Baseline check:

- `473f766` is an approved descendant of `19aa700`.
- Existing working tree dirt observed before this TIP-04 document was created:
  - `docs/00_AGENT_COORDINATION_BUS.md`
  - `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
  - `Note.txt`
- TIP-04 planning must not rely on or modify those pre-existing dirty files.

TIP-03 delivered the domain/contracts/ports foundation only. It did not implement runtime API endpoints, API key middleware, production secret handling, durable persistence, capture artifact runtime behavior, evidence result runtime behavior, final decision calculation, evidence package creation, webhook dispatch, production cryptography, or SignFlow runtime dependencies.

## 2. TIP-04 Goal

TIP-04 may implement the first narrow S1 runtime slice for:

- Local/dev API key authentication shape.
- Authenticated client application policy lookup and validation.
- `POST /api/ekyc/verification-sessions`.
- `GET /api/ekyc/verification-sessions/{id}`.
- Transaction-bound profile validation.
- RequiredChecks validation against authenticated client policy.
- Session lifecycle creation and basic state transition rules needed for future TIPs.
- Audit event append behavior for session creation and state changes.
- Tests for the expected HTTP/API behavior and boundary enforcement.

TIP-04 must make a business client able to create and read its own session, while preserving separate future caller categories for CaptureAgent and TrustedAdapter flows.

## 3. In-Scope

- API project runtime routes for create-session and get-session only.
- Local/dev API key extraction and validation using fake configured key material suitable for tests and local development.
- Deriving `clientApplicationId`, caller category, and caller scopes from the authenticated credential.
- Client policy validation for:
  - active/disabled client application status,
  - active/revoked/expired API key status,
  - allowed profiles,
  - allowed purposes,
  - allowed required checks,
  - allowed caller scopes,
  - transaction-bound permission.
- Session creation using the existing `BusinessConsumer.CreateVerificationSessionRequestDto` contract shape unless a review-approved additive field is needed.
- Session summary read using the existing `BusinessConsumer.VerificationSessionSummaryDto` contract shape unless a review-approved additive field is needed.
- RequiredChecks validation that persists the requested checks on the session root and rejects checks not allowed by client policy.
- Basic lifecycle state handling:
  - create sessions in `CREATED`;
  - preserve `VerificationResult.NOT_AVAILABLE` at creation;
  - expose current state/result separately in get-session;
  - allow only minimal state transition helpers required by create/read tests and future capture/completion readiness.
- Append-only audit events for:
  - successful session creation;
  - authenticated cross-client access denied where safe;
  - optional failed authentication safe markers in LocalDev only;
  - explicit audited write-path lifecycle state changes if a future accepted scope adds them.
- In-memory/test-only runtime storage if needed for this S1 slice, clearly labeled non-production.
- Unit, contract, integration/API, and architecture tests focused on TIP-04 behavior and non-goals.

## 4. Out-of-Scope

TIP-04 must not implement:

- Capture artifact upload endpoints or capture artifact runtime behavior.
- `EvidenceResult` recording endpoints or runtime behavior.
- Document/NFC/face/liveness/fingerprint evidence adapter behavior.
- Final verification decision calculation.
- Evidence package creation, manifest finalization, or evidence package retrieval behavior.
- Webhook dispatcher, webhook retry, or webhook signing runtime behavior.
- Adapter runtime behavior or mock engine result generation.
- Vault/file/object storage, raw artifact storage, raw artifact streaming, or raw artifact handling.
- Production API key secret storage, hashing, rotation, external secret manager integration, or production credential lifecycle.
- Production cryptography, signature verification, replay protection, non-repudiation, or legal audit reliance.
- EF providers, `DbContext`, migrations, schema/model mapping, durable local store, or production database selection.
- SignFlow source code, database, runtime package, internal model, or internal service dependency.
- Production-certified eKYC readiness claims.

## 5. Proposed Runtime/API Shape

TIP-04 should keep the public runtime surface to these endpoints:

```text
POST /api/ekyc/verification-sessions
GET  /api/ekyc/verification-sessions/{id}
```

Common request rules:

- Requests must be authenticated by an S1 local/dev API key.
- TIP-04 pins one S1 local/dev API key transport: `X-TagEkyc-Api-Key`.
- `Authorization: Bearer <api-key>` is not part of TIP-04 unless a later review patch explicitly adds it.
- `Idempotency-Key` may be accepted and propagated as inert request metadata for logs/tests, but TIP-04 must not implement reliable replay, same-response return, or durable idempotency.
- TIP-04 must not add an in-memory idempotency registry unless it is explicitly named LocalDev/NonProduction and tests assert its non-production behavior. Durable idempotency requires STOP+ASK.
- Requests and responses should preserve `requestId` and `correlationId` where provided, or create deterministic local/dev values for observability.
- The authenticated `clientApplicationId` must be derived from the credential, not accepted from the request body.
- Error responses should use the LLD03 error envelope:

```json
{
  "error": {
    "code": "INVALID_REQUIRED_CHECKS",
    "message": "Requested checks are not allowed for this client application.",
    "correlationId": "corr_123"
  }
}
```

Create-session success should return:

- `verificationSessionId`
- `profile`
- `state = CREATED`
- `result = NOT_AVAILABLE`
- `requestId`
- `correlationId`
- `expiresAt`

Get-session success should return a sanitized business-consumer summary only. It must not return raw artifacts, internal VaultRefs, plaintext sensitive identity fields, or trusted-adapter evidence submission shapes.

## 6. Authentication Model for S1 Local/Dev

TIP-04 may implement a local/dev API key authenticator with these constraints:

- The only preferred S1 local/dev transport is the `X-TagEkyc-Api-Key` HTTP header.
- API key material may be supplied by local development configuration or test fixtures only.
- Sample values must be fake and must be labeled non-production.
- The implementation may compare configured fake values directly for S1 local/dev if clearly isolated behind a development-only abstraction.
- The authenticated principal must include:
  - `apiKeyId`,
  - `clientApplicationId`,
  - `keyPrefix`,
  - `callerCategory`,
  - `scopes`,
  - expiration/status metadata.
- The authenticator must reject:
  - missing key,
  - unknown key,
  - revoked key,
  - expired key,
  - key linked to disabled client application,
  - key missing the required scope for the endpoint.
- TIP-04 scopes should include at least:
  - `business.session.create`
  - `business.session.read`
- TIP-04 must reserve, but not implement endpoint behavior for, future scopes such as:
  - `capture.artifact.append`
  - `trusted.evidence.append`
  - `session.complete`
  - `webhook.retry`
  - `admin.policy.manage`

Caller categories:

- `BusinessConsumer`: may create and read sessions allowed by its policy.
- `CaptureAgent`: reserved for TIP-05 capture artifact submission. TIP-04 may model it in auth/policy data, but must not implement capture endpoints.
- `TrustedAdapter`: reserved for TIP-05/TIP-06 evidence result submission or internal adapter flows. TIP-04 may model it in auth/policy data, but must not let business clients submit evidence results or final outcomes.
- `OperatorAdmin`: reserved for explicit future administrative APIs.

Non-production boundary:

- TIP-04 S1 local/dev authentication is not production secret management.
- No production hash storage, rotation, external secret manager, signing key, replay cache, or TLS termination design is accepted by this kickoff.
- Before any real credential or pilot credential is used, production API key lifecycle design requires STOP+ASK.

## 7. Client Policy Model and Validation Rules

TIP-04 may add runtime policy configuration and application-layer validation around the existing TIP-03 domain concepts. If the TIP-03 domain shape must be extended, the extension should be additive and policy-oriented, not persistence-implementation-oriented.

Client policy should structurally support:

- `clientApplicationId`
- `status`
- `allowedProfiles`
- `allowedPurposes`
- `allowedRequiredChecks`
- `allowedCallerScopes`
- `allowsTransactionBoundProfile`
- optional `defaultRequiredChecks`
- optional `maxSessionTtl`
- optional inert `webhookBaseUrl` metadata for future TIP-07 only

Validation rules:

- Disabled clients cannot authenticate or create/read sessions.
- Revoked or expired API keys cannot authenticate.
- A key must be scoped for the endpoint action.
- `profile` must be in `allowedProfiles`.
- `purpose` must be in `allowedPurposes`.
- Every requested required check must be in `allowedRequiredChecks`.
- Required checks must not be empty.
- `CAPTURE_QUALITY`, `DOCUMENT_NFC`, `FACE_MATCH`, and `LIVENESS` remain the default SignFlow S1 required checks when the transaction-bound profile/purpose requires SignFlow-compatible assurance.
- `FINGERPRINT`, `DOCUMENT_OCR`, and `RISK_EVALUATION` remain policy-enabled only; they must not become mandatory by default in TIP-04.
- `expiresAt` must be in the future and within any policy max TTL.
- `subjectRef` must be a caller-owned reference, not plaintext sensitive identity data. TIP-04 must not expand it into raw CCCD, name, DOB, biometric, or document payload fields.
- In LocalDev storage, duplicate `externalSessionId` values must be rejected per authenticated client application with `409 Conflict` and `DUPLICATE_EXTERNAL_SESSION`.
- Production or distributed `externalSessionId` uniqueness remains deferred until durable persistence is approved.
- If the implementation cannot enforce LocalDev duplicate rejection without introducing durable persistence or a production storage choice, STOP+ASK before weakening this rule.

Application boundary rule:

- TIP-04 should pass authenticated caller data into application services through an explicit context object, for example `AuthenticatedCaller` or `AuthenticatedClientContext`, rather than through ambient HTTP context inside application logic.
- That context should carry `clientApplicationId`, `apiKeyId`, `keyPrefix`, `callerCategory`, and scopes derived from authentication.
- Business-consumer request DTOs must not add `clientApplicationId`, caller category, scope, or API key fields supplied by the request body.
- Existing TIP-03 application ports may be extended additively to accept this explicit context if implementation needs it; this is preferred over widening `CreateVerificationSessionRequestDto`.
- `webhookBaseUrl`, if present in existing client metadata, must remain inert in TIP-04. TIP-04 must not validate webhook subscriptions, activate delivery config, dispatch webhooks, retry webhooks, or implement webhook signing.

## 8. Create-Session Rules

`POST /api/ekyc/verification-sessions` must:

- Authenticate the caller before reading policy-sensitive behavior.
- Derive `clientApplicationId` from the API key.
- Validate request shape and required fields:
  - `subjectRef`
  - `purpose`
  - `profile`
  - `requiredChecks`
  - `expiresAt`
- Validate client policy before creating the session.
- Reject duplicate `externalSessionId` for the authenticated client in LocalDev storage with `409 DUPLICATE_EXTERNAL_SESSION`.
- Persist or store the session with:
  - generated `verificationSessionId`,
  - authenticated `clientApplicationId`,
  - `externalSessionId`,
  - `subjectRef`,
  - `purpose`,
  - `profile`,
  - `requiredChecks`,
  - `expiresAt`,
  - `externalTransactionId`,
  - `bindingNonceHash`,
  - `requestId`,
  - `correlationId`,
  - `state = CREATED`,
  - `result = NOT_AVAILABLE`,
  - `createdAt`.
- Append an audit event for successful session creation.
- Return normal HTTP semantics, not vendor-style HTTP 200 for every outcome.

Create-session must not:

- Accept `clientApplicationId` from the body.
- Accept final `result`, `decision`, `assuranceLevel`, `evidencePackageId`, or arbitrary evidence outcome fields from a business client.
- Create capture artifacts.
- Create evidence results.
- Complete the session.
- Dispatch webhooks.
- Store raw artifacts or plaintext sensitive identity fields.

## 9. Get-Session Rules

`GET /api/ekyc/verification-sessions/{id}` must:

- Authenticate the caller.
- Require `business.session.read` or an explicit future admin scope.
- Return only sessions owned by the authenticated client application unless a future privileged admin policy is explicitly approved.
- Return `404 SESSION_NOT_FOUND` when the session id does not exist.
- Return `403 FORBIDDEN_CLIENT_APPLICATION` or `SESSION_ACCESS_DENIED` when the session belongs to another client.
- Return sanitized business-consumer fields:
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
- Preserve lifecycle state and verification result as separate values.

In TIP-04, get-session may return `assuranceLevel = NONE` or `UNKNOWN`, and evidence package fields may be null because TIP-06 is not implemented.

## 10. Transaction-Bound Profile Rules

`TRANSACTION_BOUND_EKYC_PROFILE` is a platform profile used when an identity session must bind to an external transaction. SignFlow is the first named consumer profile, but SignFlow must not become a platform dependency.

Rules:

- `TRANSACTION_BOUND_EKYC_PROFILE` requires:
  - `externalTransactionId`
  - `bindingNonceHash`
- `bindingNonceHash` must be a hash reference, not the raw nonce.
- The expected local/dev format is `sha256:<value>`, aligned to `HashRef`.
- Client policy must explicitly allow transaction-bound sessions.
- Client policy must allow the requested purpose, such as `SIGNING_AUTH`, before accepting it.
- `STANDARD_EKYC_PROFILE` must not require `externalTransactionId` or `bindingNonceHash`.
- `STANDARD_EKYC_PROFILE` must not default to SignFlow-specific purpose, transaction fields, or required checks unless the authenticated client policy explicitly says so.
- `externalSystem`, if introduced later, must be derived from or validated against the authenticated client application. It must not be blindly accepted from the business request.

## 11. RequiredChecks Enforcement Rules

RequiredChecks are policy inputs for later capture/evidence/final decision enforcement. TIP-04 must preserve them on session creation, but must not record evidence for them.

Rules:

- At least one required check is required.
- Duplicate checks should normalize to one required check or return a clear validation error; the chosen behavior must be tested.
- Checks with `Required = false` must be rejected in TIP-04 with `INVALID_REQUIRED_CHECKS`.
- Optional checks are deferred until TIP-05/TIP-06 defines optional evidence recording and final decision enforcement.
- Requested checks must be a subset of the authenticated client policy.
- Transaction-bound SignFlow-compatible sessions should require the default S1 set:
  - `CAPTURE_QUALITY`
  - `DOCUMENT_NFC`
  - `FACE_MATCH`
  - `LIVENESS`
- `DOCUMENT_NFC` must not silently imply `DOCUMENT_OCR`.
- `FINGERPRINT` must remain optional/demo-only unless policy enables it.
- Business consumers must not submit `EvidenceResult`, `PASSED`, `FAILED`, or other final outcomes through RequiredChecks.

Persistence/modeling expectation:

- The selected required checks must be stored with the session root in a way later TIP-05 evidence recording and TIP-06 final decision calculation can query.
- TIP-04 must not collapse checks into a single unstructured string that would block later enforcement.

## 12. Session Lifecycle State Model

TIP-04 must keep lifecycle state and verification result separate.

Lifecycle state describes processing stage:

- `CREATED`
- `IN_PROGRESS`
- `READY_TO_COMPLETE`
- `COMPLETED`
- `EXPIRED`
- `CANCELLED`
- `TECHNICAL_TERMINAL`

Verification result describes outcome:

- `NOT_AVAILABLE`
- `PASSED`
- `RETRY_REQUIRED`
- `FAILED_CAPTURE_QUALITY`
- `FAILED_IDENTITY`
- `REVIEW_REQUIRED`
- `TECHNICAL_ERROR`
- `NOT_SUPPORTED`

TIP-04 create-session behavior:

- New sessions start as `CREATED`.
- New sessions start with `result = NOT_AVAILABLE`.
- `expiresAt` is recorded.
- GET must not mutate session state to `EXPIRED` in TIP-04.
- Background expiration jobs, durable expiration transitions, and read-time expiration writes are out of scope.
- Any expiration mutation requires an explicit audited write path and STOP+ASK or a future accepted TIP.

Basic transition rules:

- Allowed in TIP-04:
  - no session -> `CREATED` on successful create.
  - `CREATED` -> `CANCELLED` only if a cancellation endpoint is explicitly added by accepted scope. This kickoff does not authorize that endpoint.
- Not allowed in TIP-04:
  - GET-driven `CREATED` -> `EXPIRED` mutation.
  - background job-driven expiration mutation.
- Reserved for TIP-05/TIP-06:
  - `CREATED` -> `IN_PROGRESS`
  - `IN_PROGRESS` -> `READY_TO_COMPLETE`
  - `READY_TO_COMPLETE` -> `COMPLETED`
  - any evidence-driven result change.
- Terminal states must not be mutated without explicit future rules.

TIP-04 must not name failed identity or failed capture outcomes as lifecycle states.

## 13. Audit Event Append Rules

Audit events remain append-only.

TIP-04 should append audit events for:

- successful session creation;
- authenticated cross-client session read denied when recording is safe and useful;
- lifecycle state changes only if a future accepted write path introduces them.

Failed authentication audit behavior:

- Failed authentication audit is optional in TIP-04 and LocalDev-only.
- If implemented, it must store only safe markers such as event type, timestamp, request id, correlation id, remote test marker if already safe, and a non-secret key prefix only when safely parsed.
- It must never store raw API keys, raw headers, bearer values, or full submitted credential strings.
- Production failed-authentication audit policy requires a future production security design.

Audit payload boundary:

- Audit events should store event type, actor type, actor id or key prefix, client application id, session id where available, request id, correlation id, timestamp, and payload hash/ref where appropriate.
- Audit events must not store raw API keys.
- Audit events must not store raw biometric, raw document, raw NFC, raw fingerprint, or plaintext sensitive identity payloads.
- Audit events must not store raw binding nonce. Only `bindingNonceHash` may be referenced.

Implementation boundary:

- Append-only audit behavior may use in-memory/test-only storage in TIP-04.
- Durable audit storage, retention policy, legal hold, external SIEM integration, or production audit assurance requires STOP+ASK.

## 14. Error/Status Response Expectations

Expected HTTP status behavior:

- `201 Created` for successful session creation.
- `200 OK` for successful session read.
- `400 Bad Request` for malformed request or invalid field shape.
- `401 Unauthorized` for missing/invalid/expired/revoked API key.
- `403 Forbidden` for valid credential lacking scope, disallowed policy, or cross-client access.
- `404 Not Found` for missing session id.
- `409 Conflict` for duplicate external session or conflicting idempotency request if implemented.
- `422 Unprocessable Entity` may be used for semantically invalid profile/check combinations if the API convention chooses it; otherwise use `400` consistently.

Expected error codes include:

- `INVALID_API_KEY`
- `API_KEY_EXPIRED`
- `API_KEY_REVOKED`
- `CLIENT_APPLICATION_DISABLED`
- `MISSING_SCOPE`
- `UNAUTHORIZED_PURPOSE`
- `UNAUTHORIZED_PROFILE`
- `TRANSACTION_BOUND_NOT_ALLOWED`
- `INVALID_REQUIRED_CHECKS`
- `INVALID_BINDING_NONCE_HASH`
- `MISSING_TRANSACTION_BINDING`
- `DUPLICATE_EXTERNAL_SESSION`
- `SESSION_NOT_FOUND`
- `FORBIDDEN_CLIENT_APPLICATION`
- `SESSION_ACCESS_DENIED`
- `SESSION_EXPIRED`
- `VALIDATION_ERROR`

Response messages should be useful for developers but must not leak raw key material, internal policy secrets, raw artifact locations, or sensitive identity data.

## 15. Forward-Compatibility / No-Regret Boundaries

TIP-04 must remain narrow but avoid choices that block TIP-05, TIP-06, or TIP-07.

Lifecycle compatibility:

- Session states must preserve a clean path from `CREATED` to TIP-05 capture/evidence recording and TIP-06 completion.
- TIP-04 must not invent a terminal result during create-session.
- TIP-04 must not collapse lifecycle state into verification result.

RequiredChecks compatibility:

- RequiredChecks must be structured and queryable by session id.
- RequiredChecks must preserve check type and required/optional semantics enough for later evidence enforcement.
- TIP-04 should reject optional checks if optional enforcement is not ready, rather than pretending they were enforced.

Client policy compatibility:

- Policy shape must structurally support:
  - allowed profiles,
  - allowed purposes,
  - allowed required checks,
  - allowed caller scopes,
  - transaction-bound permission.
- Policy should leave room for versioning later, but TIP-04 must not implement policy version governance unless explicitly approved.

Correlation compatibility:

- Create-session contracts must preserve:
  - `externalSessionId`
  - `subjectRef`
  - `purpose`
  - `profile`
  - `requiredChecks`
  - `expiresAt`
  - `externalTransactionId`
  - `bindingNonceHash`
  - `requestId`
  - `correlationId`

Credential compatibility:

- The API key/auth model must not assume every future caller is an ordinary BusinessConsumer.
- The authenticated principal must leave room for CaptureAgent and TrustedAdapter scoped credentials.
- BusinessConsumer credentials must not be accepted on future TrustedAdapter evidence result endpoints.

Data boundary compatibility:

- TIP-04 must not introduce raw artifact, raw biometric, raw document/NFC, raw fingerprint, or plaintext sensitive identity fields to create/get session contracts.
- Any future raw payload handling must remain behind explicit vault/secure adapter boundaries.

Storage compatibility:

- In-memory/test-only storage must be hidden behind ports or runtime abstractions that can later be replaced.
- TIP-04 must not choose EF, database technology, migrations, or durable storage schema.

Webhook compatibility:

- TIP-04 must preserve `correlationId`, `externalSessionId`, profile, result, and session id fields needed for TIP-07 webhook payloads.
- TIP-04 must not dispatch webhooks or implement webhook signatures.

## 16. Test Plan

Unit tests:

- API key metadata/status validation.
- API key transport uses `X-TagEkyc-Api-Key` for stable S1 local/dev tests.
- Client application active/disabled validation.
- Scope validation for create/read endpoints.
- Client policy profile/purpose/required check validation.
- Transaction-bound profile requires `externalTransactionId` and `bindingNonceHash`.
- `STANDARD_EKYC_PROFILE` does not require transaction-bound fields and does not default to SignFlow-specific fields.
- RequiredChecks subset/duplicate/empty behavior.
- `Required = false` checks are rejected with `INVALID_REQUIRED_CHECKS`.
- Session creation initializes `CREATED` and `NOT_AVAILABLE`.
- Lifecycle state and verification result remain separate.
- GET does not mutate expired sessions to `EXPIRED`.
- Audit event append avoids raw key/raw sensitive payload fields.
- Failed authentication audit, if implemented, stores only LocalDev safe markers.

Contract tests:

- BusinessConsumer create/get DTOs remain sanitized.
- BusinessConsumer DTOs do not expose `EvidenceResult` submission fields, internal VaultRefs, raw artifact fields, raw biometric fields, raw document/NFC payloads, or plaintext sensitive identity fields.
- TrustedAdapter DTOs are not reused as business create-session request DTOs.
- Required enum mappings remain aligned with TIP-03 projections.
- Error envelope shape matches LLD03.

Integration/API tests:

- Missing API key returns `401`.
- Unknown/revoked/expired key returns `401`.
- API key is accepted through `X-TagEkyc-Api-Key`.
- Missing scope returns `403`.
- Disabled client returns `401` or `403` consistently.
- Create session succeeds for allowed client/purpose/profile/checks.
- Create session rejects duplicate `externalSessionId` per authenticated client with `409 DUPLICATE_EXTERNAL_SESSION`.
- Create session rejects unauthorized purpose/profile/check.
- Create session treats `Idempotency-Key` as inert metadata only; no reliable/durable replay or same-response return is asserted in TIP-04.
- Create transaction-bound session succeeds only with allowed policy and valid hash.
- Create transaction-bound session rejects missing transaction id or binding nonce hash.
- Get session returns own session.
- Get session rejects cross-client access.
- Get missing session returns `404`.
- Create/get responses do not contain raw artifacts, raw identity fields, or evidence result submission fields.

Architecture/boundary tests:

- No EF provider, `DbContext`, migrations, schema/model mapping, or durable persistence implementation.
- No SignFlow runtime/source/database/package/internal model dependency.
- No production secret lifecycle implementation.
- No capture/evidence/final decision/webhook endpoints added in TIP-04.
- BusinessConsumer API does not reference TrustedAdapter request DTOs.
- Append-only audit/evidence repository conventions remain intact.

Validation commands expected after implementation:

```text
dotnet test TagEkyc.sln --no-restore
rg -n "EntityFramework|DbContext|Migration|Npgsql|SqlServer|Mongo|IFormFile|FileStream|raw|Raw|keyHash|secret|rotation|middleware" src tests -g "!**/bin/**" -g "!**/obj/**"
git ls-files | rg "(^|/)(bin|obj|TestResults)/"
```

Any expected scan hit must be reviewed as either an allowed guardrail/test/doc mention or a STOP+ASK finding.

## 17. STOP+ASK Gates

STOP+ASK before doing any of the following:

- Implementing capture artifact upload or capture artifact runtime behavior.
- Implementing `EvidenceResult` recording endpoints or runtime behavior.
- Allowing ordinary business clients to submit `EvidenceResult`, `PASSED`, `FAILED`, or final verification outcomes.
- Reusing `TrustedAdapter` result submission DTOs as business-client request DTOs.
- Implementing final verification decision calculation.
- Implementing evidence package creation, final manifest calculation, or evidence package retrieval runtime.
- Implementing webhook dispatcher/runtime/retry/signing behavior.
- Adding raw artifacts, raw artifact paths, base64 payloads, raw biometric fields, raw document/NFC fields, raw fingerprint fields, or plaintext sensitive identity fields to default business payloads.
- Exposing internal VaultRefs in default business-consumer responses.
- Making `FINGERPRINT`, `DOCUMENT_OCR`, or `RISK_EVALUATION` mandatory by default.
- Changing SignFlow S1 default required checks from `CAPTURE_QUALITY`, `DOCUMENT_NFC`, `FACE_MATCH`, and `LIVENESS`.
- Collapsing session lifecycle state and verification result into one enum or concept.
- Adding SignFlow source code, database, runtime packages, internal models, or internal service dependencies.
- Choosing production database technology.
- Adding EF providers, `DbContext`, schema/model mapping, durable local stores, migrations, or production schema.
- Implementing production API key secret storage, hashing, rotation, external secret manager integration, or production credential lifecycle.
- Implementing reliable or durable idempotency, idempotency replay, or same-response return semantics.
- Implementing production or distributed `externalSessionId` uniqueness.
- Allowing duplicate `externalSessionId` per client in LocalDev storage after this v0.2 decision.
- Accepting `Required = false` checks as optional checks before TIP-05/TIP-06 defines optional evidence enforcement.
- Implementing production cryptography, payload signatures, webhook signatures, replay protection, evidence package signatures, non-repudiation, or external audit reliance.
- Treating S1 local/dev API key comparison as production-ready credential handling.
- Adding an API key transport other than `X-TagEkyc-Api-Key`.
- Adding GET-driven expiration mutation, background expiration jobs, durable expiration transition, or durable schedulers for expiration without a reviewed storage/runtime decision.
- Changing Product Brief, HLD, LLD, S1 scope, or legal/compliance/certification posture.

## 18. Explicit Non-Production Boundaries

TIP-04 S1 runtime, if implemented, remains local/dev only for:

- API key material handling.
- In-memory/test-only session and policy storage.
- In-memory/test-only audit append behavior.
- Any fake sample clients, API keys, prefixes, scopes, purposes, or policies.
- `Idempotency-Key` acceptance as inert metadata only.
- LocalDev duplicate `externalSessionId` rejection.
- Any future idempotency or duplicate external session handling that is not backed by durable storage.

Any local/dev shortcut types, options sections, or test fixtures introduced by TIP-04 should include `LocalDev`, `DevelopmentOnly`, or `NonProduction` in their names or comments so they cannot be mistaken for production credential, persistence, idempotency, or expiration infrastructure.

TIP-04 must not claim:

- production credential safety,
- production API security completeness,
- legal eKYC certification,
- production audit durability,
- production data retention compliance,
- production replay protection,
- production evidence assurance.

Before a real pilot or production use, separate review is required for production secret management, durable persistence, retention, privacy controls, audit durability, TLS/deployment boundaries, rate limiting, abuse controls, and legal/compliance posture.

## 19. Open Questions Requiring Homeowner/Reviewer Decision Before Implementation

Resolved by v0.2 review patches:

- API key transport is `X-TagEkyc-Api-Key`.
- `Idempotency-Key` is inert metadata only in TIP-04; reliable/durable idempotency is deferred.
- Duplicate `externalSessionId` is rejected per authenticated client in LocalDev storage with `409 DUPLICATE_EXTERNAL_SESSION`.
- `Required = false` checks are rejected with `INVALID_REQUIRED_CHECKS`.
- GET does not mutate expired sessions to `EXPIRED`.
- Failed authentication audit is optional LocalDev-only and must store only safe markers.

Remaining open questions:

1. Client policy shape: should TIP-04 add `allowedProfiles` and `allowsTransactionBoundProfile` to the TIP-03 domain model, or keep them in a runtime policy options model until a persistence design is approved?
2. Error status convention: should semantic policy violations use `403`, `400`, or `422` consistently?
3. Scope names: are the proposed scope strings acceptable, especially the reserved CaptureAgent and TrustedAdapter scope categories?
4. Review handoff: should the reviewer authorize implementation immediately after this kickoff, or require another patched kickoff before dispatch?

## 20. Self-Check Report

Self-check method:

- Local coordinator review against TIP-04 boundaries.
- Explorer subagent review using `gpt-5.4-mini` for TIP-03 contract/domain boundary context.
- Independent self-check reviewer subagent using `gpt-5.5` for draft review before handoff.

| Finding ID | Severity | Issue | Patch applied or reason no patch needed |
| --- | --- | --- | --- |
| TIP04-SC-01 | Medium | The kickoff derived `clientApplicationId` from credentials, but did not explicitly say how authenticated caller context should cross the application boundary without widening business DTOs or relying on ambient HTTP context. | Patched. Added an application boundary rule requiring explicit `AuthenticatedCaller`/`AuthenticatedClientContext` style context and prohibiting `clientApplicationId`, caller category, scope, or API key fields in business request DTOs. |
| TIP04-SC-02 | Low | `webhookBaseUrl` appeared in client policy support and could be mistaken as active TIP-04 webhook config. | Patched. Clarified it is inert future TIP-07 metadata only and TIP-04 must not validate subscriptions, activate delivery config, dispatch/retry webhooks, or implement webhook signing. |
| TIP04-SC-03 | Low | Temporary S1 shortcuts most likely to become debt are direct fake key comparison, in-memory idempotency/uniqueness, and read-time expiration mutation. | Patched as a guardrail. Required local/dev shortcut types, options, or fixtures to include `LocalDev`, `DevelopmentOnly`, or `NonProduction` naming/comments. Existing STOP+ASK gates remain. |
| TIP04-V02-01 | Low | v0.1 left several reviewer decisions open for implementation, including idempotency, duplicate `externalSessionId`, optional checks, expiration mutation, audit failures, and API key transport. | Patched. v0.2 pins each decision, moves resolved questions out of open questions, updates STOP+ASK gates and tests, and keeps all pinned shortcuts LocalDev/NonProduction. |

Explicit self-check answers:

1. Does this kickoff keep TIP-04 narrow without blocking TIP-05? Yes. Capture artifact and `EvidenceResult` runtime are deferred, while caller scopes and structured RequiredChecks are preserved for future capture/evidence enforcement.
2. Does this kickoff keep TIP-04 narrow without blocking TIP-06? Yes. Final decision and evidence package creation are deferred, while session state/result separation and queryable RequiredChecks remain available for completion.
3. Does this kickoff keep TIP-04 narrow without blocking TIP-07? Yes after patch. Webhook fields are inert metadata only; TIP-04 preserves correlation/session fields but does not implement dispatcher, retry, delivery config activation, or signing.
4. Are any temporary S1 shortcuts likely to become architectural debt? Yes, if allowed to escape local/dev boundaries: fake key comparison, inert `Idempotency-Key` handling, LocalDev duplicate `externalSessionId` rejection, and any future expiration mutation. The v0.2 pinned decisions, naming/comment guardrail, and STOP+ASK gates keep them explicit.
5. Are caller categories and future scoped credentials preserved enough for CaptureAgent and TrustedAdapter flows? Yes. `BusinessConsumer`, `CaptureAgent`, and `TrustedAdapter` categories/scopes are reserved, and business clients remain blocked from submitting `EvidenceResult` or arbitrary outcomes.

Final self-check recommendation:

```text
ACCEPT FOR REVIEW
```

No unresolved self-check findings remain in this kickoff draft.

## 21. Recommended Review Decision

Review decision requested:

```text
TIP-04 Kickoff v0.2 accepted for review.
No TIP-04 implementation is authorized until reviewer/homeowner accepts this kickoff or requests patches.
```
