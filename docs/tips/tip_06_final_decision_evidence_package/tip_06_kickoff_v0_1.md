# TIP-06 Final Decision, Evidence Package, and Audit Manifest Kickoff v0.1

**File:** `docs/tips/tip_06_final_decision_evidence_package/tip_06_kickoff_v0_1.md`
**Version:** 0.1
**Internal review draft:** Historical v0.31 planning record; post-implementation closeout note added
**Status:** Historical planning record - TIP-06 accepted externally and implemented
**Date:** 2026-06-11
**Baseline:** Product Brief v0.1.1 + TIP-03 closed + TIP-04 implemented + TIP-05 implemented at `51d449f` + TIP-06 implemented at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`
**Purpose:** Historical planning boundary for TIP-06 final decision, evidence package, and audit manifest runtime implementation.

## Closeout / Final Gate Note

TIP-06 implementation accepted and committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`; final external architect gate supersedes the stale in-document NEEDS PATCHES wording.

Post-commit validation passed: `TagEkyc.ContractTests` 8 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 36 passed, total 60 passed and 0 failed.

This document is retained as a historical planning/review record. It is not an active implementation gate.

## Changelog

### Post-implementation closeout - 2026-06-11

- Recorded that TIP-06 implementation was accepted externally and committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`.
- Recorded post-commit validation totals: 60 passed, 0 failed.
- Marked the packet as a historical planning record so stale pre-implementation `NEEDS PATCHES` language is not interpreted as current governance.

### v0.1 - Initial TIP-06 kickoff draft

- Opened TIP-06 planning for final decision calculation, evidence package creation, sanitized package read, and internal audit manifest construction.
- Pinned final decision precedence for `Passed`, `RetryRequired`, `FailedCaptureQuality`, `FailedIdentity`, `ReviewRequired`, and `TechnicalError`.
- Pinned completion lifecycle and atomicity rules, including no `Completed` session without a final decision, evidence package id, package hash, and manifest hash.
- Separated internal/audit manifest contents from default BusinessConsumer evidence package summaries.
- Preserved placeholder-only evidence package signature semantics and deferred production cryptography, production vault/storage, webhook runtime, durable persistence, and legal certification claims.
- Added required multi-agent review report sections for security/data-boundary, lifecycle/decision, ambiguity, evidence-chain, proof-level, and final neutral self-check.
- Added a required builder-reviewer pass to catch sections where implementers would otherwise need to infer behavior and could drift from the intended design.

### Internal v0.2 - Round 1 A/B implementation-affecting patches

- Required a single LocalDev completion persistence boundary that compare-and-sets completion and returns an existing completion snapshot for idempotent retry.
- Required completed session projection data to include final result, assurance level, decision id, evidence package id, package hash, manifest hash, and completedAt.
- Replaced free-form package evidence refs with additive structured provenance entries for domain/internal/public projections.
- Required the internal manifest schema to include RequiredChecks, mapped required check type per evidence item, decision summary fields, and provenance fields.
- Split audit references into pre-package manifest audit refs included in canonical hash and post-package audit refs stored outside the hash to avoid circularity.
- Defined a canonical manifest payload separate from the published manifest envelope that carries `manifestHash`.
- Explicitly superseded older LLD03 `payloadSignature` / `evidencePackageSignature: "placeholder"` response examples for TIP-06; this slice exposes signature status only.
- Pinned additive BusinessConsumer completion/session/package fields needed by TIP-07 and clarified that TIP-07 derives per-check sanitized result blocks from internal evidence/manifest records, not from raw BusinessConsumer package summary fields.
- Added builder-reviewer patches for explicit command/query ports, DTOs, route/DI registration, LocalDev `session.complete` policy fixtures, missing-check error shape, request/correlation id precedence, and shared canonicalization helper/test vectors.

### Internal v0.3 - Round 2 convergence patches

- Pinned one successful completion sequence and removed contradictory audit/manifest ordering.
- Required the LocalDev finalization boundary to publish decision, package, completed projection, and idempotent snapshot under one lock/CAS unit.
- Defined retry-after-package-failure behavior: no decision, pre-package audit, package, or completed projection is visible unless finalization succeeds.
- Defined `postPackageAuditEventRefs` as read-time projection from append-only audit events, not stored by mutating the package record after creation.
- Standardized structured evidence ref naming on `evidenceResultId`; public DTO `id` may remain only as an alias outside canonical hashing.
- Pinned cross-client access error semantics for complete and package read.
- Added missing API pipeline tests for `forceReview`, expired sessions, and package-read caller/scope denials.

### Internal v0.4 - Round 3 convergence patches

- Prepared `evidencePackageId` and package `createdAt` before manifest/package hash computation.
- Clarified that pre-package audit refs are prepared before hashing but become visible only inside finalization.
- Pinned `/complete` precedence: authentication, caller category/scope, ownership, completed snapshot return, then expiry/lifecycle/completeness.
- Pinned completed-session retry after expiry to return the existing snapshot only after auth/scope/ownership succeeds.
- Pinned `InProgress` with all latest required evidence `Passed` as `409 SESSION_STATE_CONFLICT`.
- Pinned `NotAvailable` evidence input to `409 UNSUPPORTED_EVIDENCE_RESULT`.
- Moved post-package audit events into the LocalDev finalization visible unit so failure cannot leave partial post-completion audit state.
- Added missing API pipeline tests for missing complete-session id, completed retry authorization cases, state-drift conflict, unsupported evidence input, and post-package audit failure.

### Internal v0.5 - Round 4 convergence patches

- Removed ambiguous `requestedBy` from complete request.
- Added `forceReview` validation to the exact `/complete` precedence ladder before idempotent completed snapshot return.
- Replaced string audit refs with explicit canonical audit ref objects containing `eventId`, `eventType`, and `eventPayloadHash`.
- Pinned exact S1 audit ref member sets and semantic ordering.
- Added normative derivation rules for `CompletedChecks`, `FailedChecks`, `DecisionReasonCodes`, and `RetryReasonCodes`.
- Added a single evidence-ref projection field matrix and standardized the evidence kind field on `resultType`.

### Internal v0.6 - Round 5 convergence patches

- Aligned the final `Passed` manifest example with `DecisionReasonCodes = ["FINAL_PASSED"]`.
- Clarified that `postPackageAuditEventRefs` filters only `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED` in fixed semantic order and excludes package-read/later events.
- Marked the opening complete rule list as a precedence summary and aligned `forceReview` before expiry.
- Required canonical public `EvidenceRefSummaryDto` fields `EvidenceResultId`, `ResultType`, and `RequiredCheckType`; legacy `Id`/`Type` aliases are not part of TIP-06.
- Updated the complete success example to include all required TIP-07-compatible fields.
- Added a LocalDev package-read missing-scope fixture requirement.

### Internal v0.7 - Round 6 convergence patches

- Chose additive BusinessConsumer evidence-ref compatibility: keep existing `Id`/`Type` aliases as deprecated public-only fields while adding canonical `EvidenceResultId`/`ResultType`; aliases are excluded from hashing and cannot be used as implementation source of truth.
- Added an exact package-read precedence ladder and corresponding API tests.
- Required a named TIP-07 compatibility contract artifact with the full future webhook field set, without dispatching webhook runtime.

### Internal v0.8 - Round 7 convergence patch

- Reconciled owner identity for TIP-07 compatibility: preserve authenticated client application identity in internal completion/package projection and the TIP-07 compatibility artifact, but do not expose `clientApplicationId` in default BusinessConsumer completion/session/package DTOs.

### Internal v0.9 - Round 8 convergence patches

- Defined the exact completion conflict token: expected session state plus ordered latest required evidence result ids and append ordinals.
- Defined canonical audit payload schemas for successful completion audit events and required golden payload hash fixtures.
- Pinned validation order before final-result precedence for unsupported required checks, unsupported evidence statuses, missing evidence, and retry-required evidence.
- Chose one TIP-07 artifact: update existing `BusinessConsumer.VerificationCompletedEventDto` additively, preserving `WebhookSignatureStatus` as webhook-only placeholder and adding `EvidencePackageSignatureStatus`.
- Added API pipeline assertions for full response field sets and `error.details.missingChecks`.
- Added STOP+ASK gate for exposing owner identity/client-safe application refs to default BusinessConsumer responses.

### Internal v0.10 - Round 9 convergence patches

- Pinned TIP-06 semantics for existing `VerificationCompletedEventDto` delivery fields: fixed `EventType`, deterministic LocalDev placeholder `DeliveryId`, and `SentAt = completedAt`; no webhook dispatch is implied.
- Narrowed effective request/correlation id persistence to completion snapshot, package/manifest metadata, and audit events; `VerificationDecision` is not extended for those fields in TIP-06.
- Marked existing `EvidencePackage.AuditEventRefs` string list as legacy/untrusted if retained; structured canonical audit refs are the source of truth.

### Internal v0.11 - Round 10 convergence patches

- Pinned deterministic prepared audit event ids for successful completion audit refs before manifest hashing.
- Pinned exact `VerificationCompletedEventDto.DeliveryId = "localdev-not-dispatched"`.
- Pinned unknown `requestedBy` request body field behavior to ignored under current default JSON binding.

### Internal v0.12 - Round 11 convergence patches

- Prepared deterministic `completedAt` before `SESSION_COMPLETED` audit payload hashing and TIP-07 compatibility projection.
- Required completed session summaries to source requestId/correlationId from the completion snapshot effective values, not pre-completion session defaults.
- Expanded completed session API pipeline assertions to cover the full TIP-06/TIP-07 completed field set.
- Added an exhaustive-review protocol requiring reviewers to complete the full checklist instead of stopping after the first findings.

### Internal v0.13 - Round 12 exhaustive convergence patches

- Required one deterministic completion value preparer for all ids and timestamps that feed decision/package/audit/hash outputs.
- Removed the non-deterministic retry-after-failure branch.
- Clarified TIP-07 compatibility wording: BusinessConsumer reads expose only client-safe fields and never `clientApplicationId`.
- Deferred non-success/read audit side effects out of TIP-06 except the three successful completion audit events.
- Added `/complete` unresolved policy/context `SESSION_ACCESS_DENIED` rule and API test.
- Added idempotent retry API test proving second requestId/correlationId values are ignored and original snapshot values are returned.

### Internal v0.14 - Round 13 exhaustive convergence patches

- Removed stale optional denial/failure audit language; TIP-06 emits exactly the three successful completion audit events and no package-read, denial, incomplete-check, or package-creation-failure audit events.
- Removed `SESSION_ALREADY_COMPLETED` as an allowed TIP-06 runtime code; completed retries must return the existing snapshot or fail earlier auth/category/scope/ownership/request-option validation.
- Added one state/evidence validation ladder for `/complete` evidence-validation step, including `Created + RiskEvaluation`, `ReadyToComplete + unsupported latest evidence`, and `ReadyToComplete + non-passed latest evidence` overlap behavior.
- Pinned `decisionCreatedAt` as a canonical manifest/hash input and clarified canonical payload `createdAt` as prepared package/manifest creation time.
- Added API tests for the new validation overlap cases.

### Internal v0.15 - Round 14 exhaustive convergence patches

- Pinned post-package audit preparation/publication failure to `500 COMPLETION_AUDIT_PUBLICATION_FAILED` with no visible completion artifacts.
- Added terminal-state precedence before evidence loading for non-completed expired/terminal sessions; terminal overlap cases return `SESSION_TERMINAL` before RequiredChecks/evidence validation.
- Required `ExternalSessionId` to remain populated on completed `VerificationSessionSummaryDto` and added completed-session API assertion coverage.
- Recorded Round 14 same-version findings and required v0.15 A/B re-review.

### Internal v0.16 - Round 15 exhaustive convergence patches

- Added the missing `ExternalSessionId` entry to the completed-session summary field list.
- Replaced ambiguous timestamp round-trip wording with exact whole-second UTC `yyyy-MM-dd'T'HH:mm:ss'Z'` canonical timestamp formatting.
- Pinned `VerificationSessionState.Expired` to `403 SESSION_EXPIRED` before terminal/evidence validation, matching elapsed `ExpiresAt` expiry behavior.
- Added reason-code and retry-reason-code safety rules: sanitized uppercase token format, invalid-code rejection, deterministic final markers, and test coverage.
- Recorded Round 15 same-version findings and required v0.16 A/B re-review.

### Internal v0.17 - Round 16 exhaustive convergence patches

- Added `verificationSessionId` and authenticated `clientApplicationId` to deterministic completion value preparer inputs and required cross-session distinctness tests.
- Pinned `confidence` as ignored for TIP-06 final decision/assurance/provenance/hash behavior unless a future reviewed slice adds it.
- Added API pipeline tests for invalid `ReasonCodes` and `RetryReasonCode` values.
- Added `/complete` caller-category precedence tests for CaptureAgent/TrustedAdapter before scope/session lookup/ownership.
- Added contract/boundary tests proving `ClientApplicationId` is allowed only on `VerificationCompletedEventDto` and absent from complete/session/package API JSON.
- Recorded Round 16 same-version findings and required v0.17 A/B re-review.

### Internal v0.18 - Round 17 exhaustive convergence patches

- Marked the completed-session summary field list in section 5 as the authoritative field set to avoid DTO scaffolding drift.
- Pinned `COMPLETION_AUDIT_PUBLICATION_FAILED` for preparation/publication/append failure of any of the three successful completion audit events, including `FINAL_DECISION_CALCULATED`.
- Required reason-code safety validation on every latest required EvidenceResult before decision/package creation, including latest `Passed` evidence.
- Added operation-time semantics: capture one `operationNow` before expiry validation, use it for expiry comparison and deterministic preparer input, and do not recheck wall clock mid-finalization.
- Added package-read malformed-id API coverage and explicit unauthenticated coverage for both TIP-06 endpoints.
- Recorded Round 17 same-version findings and required v0.18 A/B re-review.

### Internal v0.19 - Round 18 exhaustive convergence patches

- Reconciled `operationNow` with retry-after-failure semantics: an external retry after no-visible package-construction failure is a new attempt with a fresh `operationNow` and may produce a new prepared value set.
- Added reason-code safety as an explicit validation-ladder step after missing evidence and before `RetryRequired`, with overlap API tests.
- Added exact missing-session behavior to the `/complete` precedence ladder: syntactically valid unknown session id returns `404 SESSION_NOT_FOUND`.
- Recorded Round 18 same-version findings and required v0.19 A/B re-review.

### Internal v0.20 - Round 19 high-rigor convergence patches

- Removed contradictory `payloadHash` public exposure wording; default BusinessConsumer complete/session/package API JSON must not expose `payloadHash` in TIP-06.
- Added reason-code overlap API tests for missing evidence plus invalid reason code and retry-required evidence plus invalid reason/retry reason code.
- Added retry-after-no-visible-failure clock-advance test proving second attempt timestamps/hash inputs use the second attempt's `operationNow`.
- Pinned manifest construction/canonicalization/hash and package envelope/hash failures to `500 EVIDENCE_PACKAGE_CREATION_FAILED` with no visible artifacts.
- Expanded API JSON absence assertions across complete response, completed session GET, and package GET.
- Distinguished effective completion request/correlation ids from evidence-record request/correlation ids; evidence-level ids are excluded from TIP-06 decision/provenance/hash outputs.
- Recorded Round 19 high-rigor findings and required v0.20 A/B re-review.

### Internal v0.21 - Round 20 high-rigor convergence patches

- Qualified package inputs as effective completion request/correlation ids only and excluded source evidence-record request/correlation ids from package inputs.
- Replaced broad audit-ref package input wording with the exact TIP-06 audit ref set: `FINAL_DECISION_CALCULATED` as the only pre-package manifest ref and `EVIDENCE_PACKAGE_CREATED` / `SESSION_COMPLETED` as read-time post-package projections.
- Added unit/golden/contract/API coverage for evidence-record request/correlation id exclusion and invariance.
- Added `PayloadHash` PascalCase DTO/API absence assertions alongside `payloadHash`.
- Added test proving unrelated prior session/evidence/read audit events do not enter package records, manifest hash, package hash, or published envelope.
- Recorded Round 20 high-rigor findings and required v0.21 A/B re-review.

### Internal v0.22 - Round 21 high-rigor convergence patches

- Scoped `postPackageAuditEventRefs` projection to this completion's prepared `evidencePackageCreatedAuditEventId` and `sessionCompletedAuditEventId` or matching session/package ids, not global event-type filtering.
- Corrected Patch Impact Matrix wording so source evidence-record request/correlation ids are excluded while `evidenceResultId` remains required and hash-participating.
- Added golden coverage that changing `evidenceResultId` changes canonical manifest/package hash while changing only evidence-record request/correlation ids does not.
- Updated public exposure prohibition and STOP+ASK gate to forbid both JSON `payloadHash` and DTO/property `PayloadHash`.
- Recorded Round 21 high-rigor findings and required v0.22 A/B re-review.

### Internal v0.23 - Round 22 high-rigor convergence patches

- Added `result` and `assuranceLevel` to the authoritative completed-session summary field/assertion set.
- Added completed-session GET tests for `Passed -> Medium` and a non-passed final result -> `None`.
- Added `/complete forceReview=true` overlap tests proving authentication/category/scope/session lookup/ownership/policy gates run before request-option validation.
- Recorded Round 22 high-rigor findings and required v0.23 A/B re-review.

### Internal v0.24 - Round 23 high-rigor convergence patches

- Aligned the earlier `VerificationSessionSummaryDto` scaffolding list with the authoritative completed-session field set by adding `Result` and `AssuranceLevel`.
- Tightened completed-session GET API assertions so `result` must equal the final decision for both `Passed` and non-passed outcomes.
- Recorded Round 23 high-rigor findings and required v0.24 A/B re-review.

### Internal v0.25 - Round 24 high-rigor convergence patches

- Aligned the `VerificationSessionSummaryDto` scaffolding list with the full authoritative completed-session field set, including package ids/hashes, request/correlation ids, and completedAt.
- Chose the default BusinessConsumer evidence-ref contract: per-evidence `result` is not exposed by default in TIP-06 package evidence refs.
- Tightened completed-session GET API assertions for every final outcome: `Passed`, `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, and `TechnicalError`.
- Corrected Patch Impact Matrix boundary/status separation and stale v0.19 self-check wording.
- Recorded Round 24 high-rigor findings and required v0.25 A/B re-review.

### Internal v0.26 - Round 25 high-rigor convergence patches

- Aligned the explicit completion snapshot and completed projection operation with the full effective request/correlation id persistence contract.
- Corrected Patch Impact Matrix v0.19-v0.23 boundary-test impact cells so reviewed status is recorded only in the carry-through column.
- Added an explicit SignFlow-style carry-through scan for the v0.26 patch before re-review.
- Recorded Round 25 high-rigor findings and required v0.26 A/B re-review.

### Internal v0.27 - Round 26 high-rigor convergence patches

- Aligned the Section 19 `INCOMPLETE_REQUIRED_CHECKS` error-envelope example with the Section 8/21 requirement for sanitized sorted `error.details.missingChecks`.
- Added an explicit SignFlow-style carry-through scan for the v0.27 patch before re-review.
- Recorded Round 26 high-rigor findings and required v0.27 A/B re-review.

### Internal v0.28 - Round 27 high-rigor convergence patches

- Added API overlap tests for `RiskEvaluation` precedence over unsupported/missing evidence and unsupported evidence precedence over missing evidence.
- Added `/complete forceReview=true` terminal-session overlap coverage for `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal`.
- Added package-read no-audit-side-effect coverage proving successful package reads do not append `EVIDENCE_PACKAGE_READ` or any read audit event.
- Added an explicit SignFlow-style carry-through scan for the v0.28 test-expectation patch before re-review.
- Recorded Round 27 high-rigor findings and required v0.28 A/B re-review.

### Internal v0.29 - Round 28 batch patches

- Clarified `Created` state precedence: `Created` state eligibility is evaluated only after RiskEvaluation, unsupported evidence, missing evidence, invalid reason-code, and retry-required gates pass.
- Clarified `RetryReasonCode` null/absent versus blank/whitespace behavior: null/absent is ignored, present blank/whitespace is invalid.
- Enumerated terminal states exactly as `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal`.
- Narrowed the Section 3 BusinessConsumer package summary wording to summary-level final result/status/timestamp fields and excluded per-evidence result/status/timestamps from default public evidence refs.
- Expanded no-audit API/integration tests for representative package-read denial and `/complete` non-success paths.
- Added affected-surface map, Patch Impact Matrix row, Review Loop Trace row, Builder Result update, Final Self-Check update, and Final Recommendation update for v0.29.

### Internal v0.30 - external GPT review targeted patches

- Superseded the old same-exhaustive-checklist convergence rule for post-v0.29 reviews with the Lane A/Lane B scoped protocol.
- Strengthened no-audit test wording so successful and denied package reads plus representative `/complete` non-success paths assert audit store count unchanged and no audit event of any type is appended or visible, with only explicitly fault-injected successful-completion audit append failures covered by rollback/no-visible-event expectations.
- Added implementation preflight requiring builders to verify `VerificationSessionState.TechnicalTerminal` exists in the current repo before implementation, and to STOP+ASK before adding or renaming lifecycle states.

### Internal v0.31 - v0.30 Lane A bookkeeping patch

- Updated top-level status metadata from superseded same-checklist convergence wording to v0.31 Lane A carry-through wording.

## 1. Current Baseline and Commit Anchors

Accepted baseline:

- TIP-01: CLOSED.
- TIP-02A: CLOSED.
- TIP-03: CLOSED.
- TIP-04: CLOSED.
- TIP-05: CLOSED.

Commit anchors:

- `9ab27b1` - `chore: add TIP-01 TIP-02A bootstrap baseline`
- `19aa700` - `feat: implement TIP-03 domain contracts and ports`
- `473f766` - `docs: close TIP-03`
- `5b243ee` - `feat: implement TIP-04 API key policy and session lifecycle`
- `51d449f9484115d86f50719eecbdc073bd0804b0` - `feat: implement TIP-05 capture artifact and evidence recording`

Preflight on 2026-06-11:

- Current repository root is:

```text
D:\Task\Remote Signing\TagEkyc
```

- All `git`, `dotnet`, and `rg` commands for TIP-06 must run from this root.
- Current inspected HEAD is exactly `51d449f9484115d86f50719eecbdc073bd0804b0`, satisfying the prompt gate.
- Current dirty files before TIP-06 planning:
  - `docs/00_AGENT_COORDINATION_BUS.md`
  - `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
  - `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
  - `Note.txt`
- Those files are pre-existing non-TIP-06 dirty files and must not be modified by TIP-06 planning or implementation unless explicitly brought into scope.
- Tracked generated output check:

```text
git ls-files | rg '(^|/)(bin|obj|TestResults)/'
```

Result: no tracked generated output paths.

TIP-05 delivered:

- CaptureAgent-scoped capture artifact metadata/reference recording.
- TrustedAdapter-scoped evidence result recording.
- Append-only LocalDev capture artifact and evidence result repositories.
- Evidence result linkage to RequiredChecks and input capture artifacts.
- Required `PayloadHash` for accepted evidence results.
- Latest accepted evidence result by append order for readiness.
- `Created -> InProgress` and evidence-result-only `InProgress -> ReadyToComplete`.
- `ReadyToComplete` only when the latest accepted TrustedAdapter evidence result for every required check is `Passed`.
- Rejection of capture/evidence writes after `ReadyToComplete`.
- BusinessConsumer session read remains sanitized and non-final.

TIP-05 did not deliver final decision calculation, `Completed` transition, assurance level calculation, evidence package creation/read, webhook runtime, production vault/storage, production persistence, or production cryptography.

## 2. TIP-06 Goal

TIP-06 may implement the first narrow S1 runtime slice for completing a verification session by calculating the final verification result from recorded RequiredChecks and evidence summaries, creating a deterministic evidence package and internal audit manifest, and exposing a sanitized BusinessConsumer evidence package summary.

TIP-06 must prove the chain:

```text
CaptureArtifact -> EvidenceResult -> RequiredChecks -> completion eligibility -> VerificationDecision -> EvidencePackage -> internal audit manifest -> future webhook payload fields
```

TIP-06 must remain LocalDev/S1 and non-production. It must not implement production cryptography, production evidence package signing, production vault/file/object storage, durable persistence, webhook dispatch, OperatorAdmin review, FraudRisk production scoring, or legal certification claims.

## 3. In-Scope

- `POST /api/ekyc/verification-sessions/{id}/complete`.
- `GET /api/ekyc/evidence-packages/{id}` returning a sanitized BusinessConsumer package summary.
- Application service and ports for completion and package read.
- LocalDev/NonProduction in-memory append-only repositories for verification decisions and evidence packages if needed to back existing ports.
- RequiredChecks completeness enforcement.
- Final decision calculation from latest accepted TIP-05 evidence summaries.
- Explicit handling for:
  - `Passed`
  - `RetryRequired`
  - `FailedCaptureQuality`
  - `FailedIdentity`
  - `ReviewRequired`
  - `TechnicalError`
- Evidence package creation with deterministic manifest hash and package hash.
- Internal/audit manifest shape that may include internal VaultRefs only if the model already has internal-domain VaultRefs and only through `InternalAudit.Manifest` contracts.
- Sanitized BusinessConsumer evidence package summary using package ids, evidence refs, hashes, correlation ids, non-sensitive metadata, summary-level final `result`, summary-level `assuranceLevel`, summary-level `evidencePackageSignatureStatus`, and summary-level `completedAt` only.
- Default BusinessConsumer evidence refs must not expose per-evidence result/status values or per-evidence timestamps such as evidence `createdAt`; TIP-07 per-check result/status blocks must be derived later through a reviewed mapper.
- Placeholder `EvidencePackageSignatureStatus = PlaceholderUnverified`.
- Append-only audit events for successful final decision calculation, evidence package creation, and session completion.
- Denied/failure/read audit side effects are deferred unless this document explicitly names a mandatory event. TIP-06 must not add discretionary audit side effects that tests do not pin.
- Unit, contract, integration/API pipeline, architecture, boundary scan, and proof-level tests.

## 4. Out-of-Scope

TIP-06 must not implement:

- Production cryptography.
- Production evidence package signing or signature verification.
- Authenticity, replay protection, non-repudiation, legal audit reliance, or external audit reliance claims.
- Webhook dispatcher/runtime/retry/signing. TIP-07 owns webhook runtime.
- Production vault/file/object storage or artifact retrieval.
- Raw artifact access, raw biometric access, raw document/NFC access, raw face/liveness/fingerprint access, or plaintext sensitive identity fields.
- Durable persistence, EF provider, `DbContext`, migrations, schema/model mapping, durable local store, production database choice, or production storage infrastructure.
- SignFlow source/database/runtime/internal dependency.
- Manual OperatorAdmin review, override, or review queue behavior.
- Production FraudRisk model, scoring engine, or policy versioning.
- Production-certified eKYC readiness.
- Specialized evidence endpoint runtime changes.
- New capture/evidence write semantics after `ReadyToComplete`.

## 5. Proposed Runtime/API Shape

TIP-06 runtime surface:

```text
POST /api/ekyc/verification-sessions/{id}/complete
GET  /api/ekyc/evidence-packages/{id}
```

Required implementation scaffolding:

- Add BusinessConsumer DTOs:
  - `CompleteVerificationSessionRequestDto`
  - `CompleteVerificationSessionResponseDto`
- Add or update `EvidenceRefSummaryDto` with canonical public fields:
  - `EvidenceResultId`
  - `ResultType`
  - `RequiredCheckType`
  - optional `ArtifactHash`
- Keep existing `Id` and `Type` as deprecated BusinessConsumer-only compatibility aliases if required by current contract compatibility.
- Deprecated `Id` must equal `EvidenceResultId`; deprecated `Type` must equal `ResultType`.
- Deprecated aliases are excluded from manifest/package hashing and must not be used as implementation source of truth.
- Contract tests must assert canonical fields are present and aliases, if retained, mirror canonical values.
- Add additive fields to `EvidencePackageSummaryDto` for TIP-07 compatibility:
  - `ExternalSessionId`
  - `ExternalTransactionId`
  - `BindingNonceHash`
  - `RequestId`
  - `CorrelationId`
  - `CompletedAt`
- Add additive completed fields to `VerificationSessionSummaryDto`:
  - `ExternalSessionId`
  - `ExternalTransactionId`
  - `BindingNonceHash`
  - `Result`
  - `AssuranceLevel`
  - `EvidencePackageId`
  - `EvidencePackageHash`
  - `ManifestHash`
  - `EvidencePackageSignatureStatus`
  - `RequestId`
  - `CorrelationId`
  - `CompletedAt`
- Add application ports:
  - `IVerificationSessionCompletionCommands.CompleteAsync(...)`
  - `IEvidencePackageQueries.GetPackageSummaryAsync(...)`
- Add LocalDev application service implementation and register it in `Program`.
- Map both routes in `VerificationSessionEndpoints`.
- Add LocalDev repository implementations for verification decisions, evidence packages, and completion projection if they do not exist yet.
- Existing `SetStateAsync` remains for TIP-05 write-path transitions only; TIP-06 completion must use the finalization boundary.

LocalDev auth/policy additions:

- Add scope constant `session.complete`.
- Add at least one LocalDev BusinessConsumer key with `business.session.create`, `business.session.read`, and `session.complete`.
- Add at least one LocalDev BusinessConsumer key without `session.complete` for `403 MISSING_SCOPE` tests.
- Add at least one same-client LocalDev BusinessConsumer key without `business.session.read` for package-read `403 MISSING_SCOPE` tests.
- Client policy `AllowedCallerScopes` must include `session.complete` only for clients intended to complete sessions in tests/local development.

`POST /complete` request:

```json
{
  "forceReview": false,
  "requestId": "req_200",
  "correlationId": "corr_123"
}
```

Manual review override is not in scope. The deterministic S1 behavior is:

- `forceReview = false` or omitted: evaluate normally.
- `forceReview = true`: return `400 FORCE_REVIEW_NOT_SUPPORTED` and do not create a decision/package.
- `requestedBy` is not part of `CompleteVerificationSessionRequestDto`; authorization and actor identity come only from authenticated client context.
- If a request body contains an extra `requestedBy` JSON property, TIP-06 ignores it under the current default JSON binding behavior. It must not affect authorization, audit actor identity, manifest content, or response fields.

`POST /complete` success response:

```json
{
  "verificationSessionId": "vs_01",
  "state": "Completed",
  "result": "Passed",
  "assuranceLevel": "Medium",
  "evidencePackageId": "ep_01",
  "evidencePackageHash": "sha256:package",
  "manifestHash": "sha256:manifest",
  "evidencePackageSignatureStatus": "PlaceholderUnverified",
  "externalSessionId": "sf_session_123",
  "externalTransactionId": "sf_tx_456",
  "bindingNonceHash": "sha256:binding",
  "requestId": "req_200",
  "correlationId": "corr_123",
  "completedAt": "2026-06-11T00:00:00Z"
}
```

TIP-06 must add a sanitized BusinessConsumer completion response DTO, for example `CompleteVerificationSessionResponseDto`, rather than returning an internal domain object. Required fields:

- `verificationSessionId`
- `state`
- `result`
- `assuranceLevel`
- `evidencePackageId`
- `evidencePackageHash`
- `manifestHash`
- `evidencePackageSignatureStatus`
- `externalSessionId`
- `externalTransactionId`
- `bindingNonceHash`
- `requestId`
- `correlationId`
- `completedAt`

Default BusinessConsumer completion/session/package DTOs must not expose `clientApplicationId`. Owner identity is preserved internally for authorization/audit/TIP-07 compatibility, not returned to ordinary BusinessConsumer reads.

TIP-06 must add these fields to the completed session summary when available, without changing not-yet-completed TIP-04/TIP-05 defaults. This list is the authoritative completed-session summary field set for TIP-06:

- `externalSessionId`
- `externalTransactionId`
- `bindingNonceHash`
- `result`
- `assuranceLevel`
- `evidencePackageId`
- `evidencePackageHash`
- `manifestHash`
- `evidencePackageSignatureStatus`
- `requestId`
- `correlationId`
- `completedAt`

`GET /evidence-packages/{id}` success response uses `BusinessConsumer.EvidencePackageSummaryDto` with additive sanitized fields, not the internal manifest DTO:

```json
{
  "evidencePackageId": "ep_01",
  "verificationSessionId": "vs_01",
  "packageVersion": "0.1",
  "packageHash": "sha256:package",
  "manifestHash": "sha256:manifest",
  "result": "Passed",
  "assuranceLevel": "Medium",
  "externalSessionId": "sf_session_123",
  "externalTransactionId": "sf_tx_456",
  "bindingNonceHash": "sha256:binding",
  "requestId": "req_200",
  "correlationId": "corr_123",
  "completedAt": "2026-06-11T00:00:00Z",
  "evidenceRefs": [
    {
      "resultType": "NfcValidation",
      "evidenceResultId": "ev_01",
      "requiredCheckType": "DocumentNfc",
      "artifactHash": "sha256:artifact"
    }
  ],
  "evidencePackageSignatureStatus": "PlaceholderUnverified"
}
```

The default BusinessConsumer response must not include `InternalAudit.Manifest.EvidenceManifestDto`, `ManifestVaultRefDto`, internal VaultRefs, raw artifact refs, TrustedAdapter request DTOs, CaptureAgent request DTOs, raw payload locations, or plaintext identity data.

TIP-06 explicitly supersedes older LLD03 sample fields for this slice:

- Do not expose `payloadSignature` in the complete response.
- Do not expose `evidencePackageSignature` string material in complete or package-read responses.
- Expose only `evidencePackageSignatureStatus = PlaceholderUnverified`.

## 6. Caller/Auth/Scope Model

TIP-06 must preserve TIP-04/TIP-05 caller categories and add only narrow scopes.

BusinessConsumer:

- May call `POST /complete` only with `callerCategory = BusinessConsumer`, ownership of the target session, and scope `session.complete`.
- May call `GET /evidence-packages/{id}` with `callerCategory = BusinessConsumer`, ownership of the package's session, and scope `business.session.read`.
- May not submit evidence, adapter metadata, raw artifacts, final result values, or arbitrary manifest contents.

CaptureAgent:

- Must not call `POST /complete`.
- Must not call `GET /evidence-packages/{id}` unless a future reviewed evidence access policy explicitly authorizes it.

TrustedAdapter:

- Must not call BusinessConsumer package read endpoints.
- Must not call `POST /complete` in TIP-06. Completion is a BusinessConsumer/application orchestration action, not adapter result submission.

OperatorAdmin:

- Reserved for future privileged workflows.
- TIP-06 must not add OperatorAdmin complete, review, override, package read, or manifest read endpoints.

Wrong caller precedence:

- Missing/unknown/revoked/expired key: `401`.
- Valid key with wrong caller category: `403 CALLER_CATEGORY_NOT_ALLOWED`.
- Valid category missing required scope: `403 MISSING_SCOPE`.
- Valid caller and scope but known target session/package belongs to another client application: `403 FORBIDDEN_CLIENT_APPLICATION`.
- Valid caller and scope but client policy or broader authorization context cannot be resolved safely: `403 SESSION_ACCESS_DENIED`.

Package-read precedence:

1. Authenticate the caller. Missing/unknown/revoked/expired key returns `401`.
2. Validate `callerCategory = BusinessConsumer`; wrong category returns `403 CALLER_CATEGORY_NOT_ALLOWED`.
3. Validate `business.session.read`; missing scope returns `403 MISSING_SCOPE`.
4. Parse and look up the evidence package id; malformed or missing package returns `404 EVIDENCE_PACKAGE_NOT_FOUND`.
5. Load the package's owning session/completion projection. Missing backing session or unresolved policy context returns `403 SESSION_ACCESS_DENIED`.
6. Verify package/session owner equals authenticated `clientApplicationId`; known cross-client package returns `403 FORBIDDEN_CLIENT_APPLICATION`.
7. Return sanitized BusinessConsumer package summary.

LocalDev package-read disclosure policy:

- Known cross-client package returns `403 FORBIDDEN_CLIENT_APPLICATION`; missing or malformed package returns `404 EVIDENCE_PACKAGE_NOT_FOUND`.
- This distinction is intentional LocalDev behavior for TIP-06 API/test determinism.
- Production anti-enumeration hardening, including uniform denial/not-found behavior, is deferred to a future reviewed production-security slice.

Tests must cover overlapping failure precedence, including wrong category with missing package id, missing scope with cross-client package id, missing package id for an otherwise authorized caller, and cross-client known package.

## 7. Complete-Session Rules

Completion is an application-layer operation. Route-only validation is not sufficient.

`POST /complete` must follow the exact precedence ladder in the successful-completion sequence below. The checklist here is a summary, not a second ordering source:

- Authenticate before loading session state.
- Resolve the target session by route id.
- Verify the caller owns the session through authenticated `clientApplicationId`.
- Reject `forceReview = true` with `400 FORCE_REVIEW_NOT_SUPPORTED`.
- Reject expired non-completed sessions with `403 SESSION_EXPIRED`.
- Reject `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal` sessions with `409 SESSION_TERMINAL`.
- Load RequiredChecks from the session root.
- Load accepted EvidenceResults by session id.
- Resolve the latest accepted EvidenceResult for each RequiredCheck by append order.
- Enforce completeness before creating a decision/package.
- Calculate final result from deterministic precedence.
- Prepare a `VerificationDecision` append-only record inside the finalization unit.
- Build the internal/audit manifest and sanitized BusinessConsumer evidence refs from the same decision inputs.
- Prepare an `EvidencePackage` append-only record with `ManifestHash`, `PackageHash`, `ResultRef`, and `EvidencePackageSignatureStatus = PlaceholderUnverified` inside the finalization unit.
- Atomically publish the decision, package, completed projection, and idempotent completion snapshot.
- Append or materialize audit events in the exact successful-completion sequence below.

Successful completion sequence:

1. Authenticate the caller.
2. Validate caller category and required scope.
3. Load the session by route id. A syntactically valid but unknown session id returns `404 SESSION_NOT_FOUND`.
4. Verify ownership. Known cross-client targets return `403 FORBIDDEN_CLIENT_APPLICATION`.
5. If client policy or broader authorization context cannot be resolved safely for the loaded session, return `403 SESSION_ACCESS_DENIED`.
6. Validate request options. `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED`, even for already completed or expired sessions.
7. If an existing completed snapshot exists, return it without creating decision/package/audit duplicates. This idempotent return is allowed even when `ExpiresAt` is now in the past, because the session already completed before this retry; auth/scope/ownership and request-option validation still apply first.
8. Capture one `operationNow` instant immediately before expiry validation, using the same clock source as the deterministic completion value preparer.
9. Reject non-completed expired sessions with `SESSION_EXPIRED`. This includes either elapsed time expiry (`ExpiresAt <= operationNow`) or `VerificationSessionState.Expired`.
10. Reject non-completed `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal` sessions with `SESSION_TERMINAL`.
11. Load evidence and validate state, completeness, retry, and decision precedence.
12. Run the deterministic completion value preparer for `verificationSessionId`, authenticated `clientApplicationId`, `operationNow`, the captured conflict token, and effective request/correlation ids.
   - It must output `verificationDecisionId`, `evidencePackageId`, `finalDecisionCalculatedAuditEventId`, `evidencePackageCreatedAuditEventId`, `sessionCompletedAuditEventId`, decision `createdAt`, package `createdAt`, and `completedAt`.
   - The same `verificationSessionId`, authenticated `clientApplicationId`, `operationNow`, conflict token, and effective request/correlation ids must produce the same prepared values in LocalDev tests.
   - Different `verificationSessionId` or authenticated `clientApplicationId` values must produce a distinct prepared id/hash value set even when the conflict token and effective request/correlation ids match.
   - Random GUIDs, repository-assigned ids, or later clock reads are not allowed for those prepared values in TIP-06.
13. Use the deterministic successful-completion audit event ids from the preparer:
   - `finalDecisionCalculatedAuditEventId`
   - `evidencePackageCreatedAuditEventId`
   - `sessionCompletedAuditEventId`
14. Prepare the `FINAL_DECISION_CALCULATED` audit ref object, using `finalDecisionCalculatedAuditEventId`, that will be part of `prePackageManifestAuditEventRefs`.
15. Build the canonical manifest payload using the prepared decision id, prepared evidence package id, prepared decision createdAt, prepared package createdAt, and prepared pre-package audit ref.
16. Compute `ManifestHash` and `PackageHash`.
17. Prepare deterministic `EvidencePackage` values, including `evidencePackageId`, `resultRef`, `manifestHash`, `packageHash`, and `createdAt`.
18. Prepare `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED` audit ref objects using `evidencePackageCreatedAuditEventId` and `sessionCompletedAuditEventId`.
19. Enter the LocalDev finalization boundary and compare the expected prior session state/evidence snapshot.
20. Publish the `VerificationDecision`, `FINAL_DECISION_CALCULATED` audit event, `EvidencePackage`, completed projection, idempotent snapshot, `EVIDENCE_PACKAGE_CREATED` audit event, and `SESSION_COMPLETED` audit event as one visible unit.
21. Return the completed snapshot.

Failure policy:

- If validation, manifest construction, package construction, successful-completion audit preparation, or finalization fails before step 20 publishes the visible unit, no `VerificationDecision`, `FINAL_DECISION_CALCULATED` audit event, `EvidencePackage`, completed projection, idempotent snapshot, `EVIDENCE_PACKAGE_CREATED`, or `SESSION_COMPLETED` audit event may be visible.
- If a LocalDev finalization conflict is detected at step 19/20, return `409 SESSION_STATE_CONFLICT` unless an existing completed snapshot is now visible for the same authorized caller and the request options are valid, in which case return that snapshot with `200 OK`.
- If `FINAL_DECISION_CALCULATED`, `EVIDENCE_PACKAGE_CREATED`, or `SESSION_COMPLETED` audit ref/event preparation, publication, or append fails, return `500 COMPLETION_AUDIT_PUBLICATION_FAILED`; no decision, audit event, package, completed projection, idempotent snapshot, or post-package audit event may be visible.
- A new external retry after no-visible package-construction failure is a new `/complete` attempt. It captures a fresh `operationNow` and may produce a new deterministic prepared value set, timestamps, manifest hash, and package hash for that new attempt. No hidden failed-attempt state may be persisted just to reuse the previous `operationNow`.
- If that new external retry supplies different requestId/correlationId values, those new non-blank values become the effective request/correlation ids for the new attempt because no previous completion snapshot exists.
- Tests must cover retry after no-visible package/finalization failure with different requestId/correlationId values and assert a new attempt uses the new effective ids while still leaving no orphan records from the failed attempt.
- TIP-06 must test that package failure and finalization conflict paths do not leave visible orphan decisions/packages or duplicate successful packages.

Prepared timestamp rule:

- Prepared `completedAt` is the single source of truth for the completion snapshot, complete response, completed session summary, package summary, `VerificationCompletedEventDto.SentAt`, and `SESSION_COMPLETED` audit payload hash.
- Prepared decision `createdAt` is stored as `VerificationDecision.CreatedAt` and is included in the canonical manifest payload as `decisionCreatedAt`.
- Prepared package `createdAt` is stored as `EvidencePackage.CreatedAt` and is included in the canonical manifest payload as `createdAt`; in the manifest context, `createdAt` means package/manifest creation time, not decision creation time.
- `operationNow` is the only wall-clock instant used for expiry evaluation and deterministic prepared timestamps during a `/complete` attempt. After step 8 captures `operationNow`, TIP-06 must not re-read wall-clock time or re-check expiry inside package preparation, finalization, or response mapping.
- If `ExpiresAt > operationNow`, the completion attempt may finish even if real wall-clock time passes `ExpiresAt` before step 20 publishes the visible unit.
- Idempotent retries must return the original prepared `completedAt`.
- The implementation must not read a second clock value during finalization or response mapping for completedAt/SentAt.

Completion persistence boundary:

- TIP-06 must introduce one application/repository boundary for finalization, for example `TryFinalizeCompletionAsync` or `FinalizeCompletionAsync`.
- That boundary must cover decision append, pre-package audit append, package append, completed projection publication, idempotent snapshot publication, and post-package audit append under one lock/CAS unit.
- That boundary must compare-and-set from the expected prior session state/evidence snapshot to `Completed` and persist or return one completion snapshot.
- The completion snapshot must include at least:
  - `verificationSessionId`
  - `state = Completed`
  - `result`
  - `assuranceLevel`
  - `verificationDecisionId`
  - `evidencePackageId`
  - `evidencePackageHash`
  - `manifestHash`
  - `evidencePackageSignatureStatus`
  - effective `requestId`
  - effective `correlationId`
  - `completedAt`
- The completion path must not be implemented as visible independent `AppendDecision`, `AppendPackage`, then `SetState` calls because concurrent `/complete` calls or package failures could append duplicate/orphan decisions/packages.
- LocalDev may implement the boundary with an in-memory lock or compare-and-set inside the LocalDev repository. Durable distributed idempotency remains out of scope.
- If the boundary detects an already completed session, return the existing completion snapshot and do not append duplicate decision/package records.
- If the boundary detects that another writer changed the session or package state during completion, return `409 SESSION_STATE_CONFLICT`.
- `IEvidencePackageRepository` must add a lookup by session id, for example `GetBySessionIdAsync` or `GetLatestBySessionIdAsync`, or the completion projection repository must expose equivalent lookup data.
- The completion service must check for an existing completed snapshot/package before creating a new decision/package.
- The completion service must not reconstruct idempotent responses by scanning free-form `EvidenceRefs` strings.

Request/correlation id precedence:

- `CompleteVerificationSessionRequestDto.RequestId`, when provided and non-blank, is the effective completion request id.
- Otherwise the session `RequestId` is reused.
- `CompleteVerificationSessionRequestDto.CorrelationId`, when provided and non-blank, is the effective completion correlation id.
- Otherwise the session `CorrelationId` is reused.
- The effective request id and correlation id must be used consistently in the completion response, completion snapshot, package/manifest metadata, and audit events.
- Current `VerificationDecision` does not carry request/correlation ids. TIP-06 does not need to extend `VerificationDecision` for those fields.
- Effective request id and correlation id must be persisted in the completion snapshot, package/manifest metadata, and audit events. They are not required on the `VerificationDecision` record unless a later implementation review chooses to add them additively.
- Completed `GET /api/ekyc/verification-sessions/{id}` must source `requestId` and `correlationId` from the completion snapshot effective values. It must not fall back to the pre-completion session defaults when the complete request supplied override values.

Completion conflict token:

- The finalization boundary must compare an explicit token captured immediately before decision/package preparation.
- Token fields:
  - `expectedSessionState`
  - ordered entries for every RequiredCheck, sorted by RequiredCheck enum string name
  - each entry contains `requiredCheckType`, `latestAcceptedEvidenceResultId`, and `latestAcceptedEvidenceAppendOrdinal`
- `latestAcceptedEvidenceAppendOrdinal` is a LocalDev monotonic append order assigned by the evidence repository, or an equivalent deterministic in-memory sequence number if the existing record does not yet expose one.
- Any change in `expectedSessionState` or any required-check latest evidence id/append ordinal between token capture and finalization returns `409 SESSION_STATE_CONFLICT`, even if recomputing the decision would produce the same final result.
- Appending optional/supporting evidence that is not the latest accepted evidence for a RequiredCheck does not change this token unless it changes a required-check latest entry.
- Unit and API tests must cover a concurrent required-check evidence append race that changes the token and causes `SESSION_STATE_CONFLICT`.

Deterministic successful audit event id rule:

- Successful completion audit event ids are prepared before manifest hashing and must be stable for the prepared completion attempt.
- The finalization boundary must append audit events using those exact prepared ids; repository-assigned random audit ids are not allowed for the three successful completion events.
- Golden manifest/hash tests must prove `finalDecisionCalculatedAuditEventId` is stable and participates in `ManifestHash`.
- Projected post-package audit refs must use `evidencePackageCreatedAuditEventId` and `sessionCompletedAuditEventId`.

Deterministic completion value preparer tests:

- Given the same `verificationSessionId`, authenticated `clientApplicationId`, `operationNow`, conflict token, and effective request/correlation ids, the preparer returns the same ids and timestamps.
- Given a different `verificationSessionId` with the same caller/token/request/correlation values, the preparer returns distinct ids and hashes.
- Given a different authenticated `clientApplicationId` with the same session/token/request/correlation values, the preparer returns distinct ids and hashes.
- Given the same `operationNow`, prepared timestamps are stable; tests must inject the clock so no later wall-clock read can change `completedAt`, `decisionCreatedAt`, package `createdAt`, `SentAt`, or audit timestamp hash inputs.
- Given a changed conflict token, the preparer returns a different prepared value set or finalization returns `SESSION_STATE_CONFLICT` before publishing.
- Same-input recomputation inside one attempt produces the same ids, timestamps, manifest hash, and package hash.
- A later external retry after a no-visible package/finalization failure is a new attempt and may produce different prepared values because it captures a new `operationNow` and may receive different request/correlation ids.

State/evidence validation ladder for `/complete` step 11:

0. This ladder runs only after authentication, category/scope, session load, ownership, request-option validation, existing completed snapshot return, expiry rejection, and terminal-state rejection. `VerificationSessionState.Expired` and elapsed `ExpiresAt <= operationNow` return `403 SESSION_EXPIRED` before this ladder. `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal` return `409 SESSION_TERMINAL` before RequiredChecks/evidence validation, even if they also have `RiskEvaluation`, missing evidence, unsupported latest evidence, invalid reason codes, or retry evidence.
1. Load RequiredChecks and latest accepted evidence by RequiredCheck before state eligibility decisions for any non-completed active session.
2. If RequiredChecks contains `RiskEvaluation`, return `409 RISK_EVALUATION_NOT_SUPPORTED` before missing, retry, unsupported evidence status, or lifecycle drift errors. Example: `Created + RiskEvaluation` returns `RISK_EVALUATION_NOT_SUPPORTED`.
3. If any latest required evidence result is `NotAvailable` or `NotSupported`, return `409 UNSUPPORTED_EVIDENCE_RESULT` before missing, retry, or lifecycle drift errors. Example: `ReadyToComplete + NotSupported` returns `UNSUPPORTED_EVIDENCE_RESULT`.
4. If any required check has no latest accepted evidence, return `409 INCOMPLETE_REQUIRED_CHECKS`.
5. Validate `ReasonCodes` and `RetryReasonCode` on every latest required EvidenceResult. Any invalid value returns `409 UNSUPPORTED_EVIDENCE_RESULT` before retry or lifecycle drift errors.
6. If any latest required evidence result is `RetryRequired`, return `409 CAPTURE_RETRY_REQUIRED`.
7. After steps 1-6 pass, apply state eligibility:
   - `Created` with a complete evidence chain is projection drift and returns `409 SESSION_STATE_CONFLICT`; this state-eligibility rule applies only after ladder steps 1-6 pass.
   - `InProgress` with all latest required evidence results `Passed` returns `409 SESSION_STATE_CONFLICT`.
   - `InProgress` with at least one non-passed final/review evidence result may complete to that deterministic final result.
   - `ReadyToComplete` with all latest required evidence results `Passed` may complete to `Passed`.
   - `ReadyToComplete` with any latest required evidence result other than `Passed`, after unsupported/retry cases are excluded, returns `409 SESSION_STATE_CONFLICT`.

State eligibility:

- `ReadyToComplete` may complete to `Passed` only after final decision and package creation succeed.
- `InProgress` may complete only to a non-passed final/review evidence result when every required check has a latest accepted evidence result and no latest required check is `RetryRequired`.
- `InProgress` with all latest required evidence results `Passed` returns `409 SESSION_STATE_CONFLICT` because TIP-05 should already have projected the session to `ReadyToComplete`.
- `Created` cannot complete. `Created` follows the same ladder order before state eligibility: `Created + RiskEvaluation` returns `RISK_EVALUATION_NOT_SUPPORTED`; `Created + NotAvailable/NotSupported` returns `UNSUPPORTED_EVIDENCE_RESULT`; `Created + missing required evidence` returns `INCOMPLETE_REQUIRED_CHECKS`; `Created + invalid ReasonCodes/RetryReasonCode` returns `UNSUPPORTED_EVIDENCE_RESULT`; `Created + RetryRequired` returns `CAPTURE_RETRY_REQUIRED`; only `Created` with a complete evidence chain that passes steps 1-6 returns `SESSION_STATE_CONFLICT` because TIP-05 state projection drift is present.
- `ReadyToComplete` cannot produce a non-passed result unless the loaded evidence set proves a conflicting latest required evidence result. That conflict means state projection drift and must return `409 SESSION_STATE_CONFLICT` without creating a decision/package.
- `Completed` retry behavior is idempotent in LocalDev: return the existing final completion snapshot and existing evidence package refs with `200 OK`; do not append duplicate decisions or packages.
- Reliable durable idempotency, replay windows, and distributed duplicate suppression remain out of scope.

## 8. RequiredChecks Completeness Rules

For each `RequiredCheckType` on the session:

- There must be at least one accepted EvidenceResult mapped to that RequiredCheck.
- The latest accepted EvidenceResult by append order is authoritative for final calculation.
- `DocumentNfc` maps from `EvidenceResultType.NfcValidation`.
- `DocumentOcr` maps from `EvidenceResultType.DocumentOcr`.
- `CaptureQuality` maps from `EvidenceResultType.CaptureQuality`.
- `FaceMatch` maps from `EvidenceResultType.FaceMatch`.
- `Liveness` maps from `EvidenceResultType.Liveness`.
- `Fingerprint` maps from `EvidenceResultType.FingerprintMatch`.
- `RiskEvaluation` remains deferred unless TIP-06 explicitly receives accepted `FraudRisk` evidence from a prior scope. Current TIP-05 rejects `FraudRisk`, so a session requiring `RiskEvaluation` cannot complete in TIP-06 and must return `409 RISK_EVALUATION_NOT_SUPPORTED`.

Completeness failure behavior:

- Missing any required evidence returns `409 INCOMPLETE_REQUIRED_CHECKS`.
- The error body must include sanitized missing check names only, not raw evidence details.
- TIP-06 must extend `SessionOperationError` and the API error envelope with an optional `details` object for this case:

```json
{
  "error": {
    "code": "INCOMPLETE_REQUIRED_CHECKS",
    "message": "Required evidence is incomplete.",
    "correlationId": "corr_123",
    "details": {
      "missingChecks": ["DocumentNfc", "FaceMatch"]
    }
  }
}
```

- `missingChecks` must be sorted by enum string name and contain only RequiredCheckType names.
- No artifact ids, evidence ids, VaultRefs, payload hashes, raw refs, or plaintext identity values may appear in the error details.
- Missing required evidence must not create a `VerificationDecision`, `EvidencePackage`, internal manifest, or `Completed` state.
- `RetryRequired` as the latest result for any required check returns `409 CAPTURE_RETRY_REQUIRED`, does not complete, and does not create a package because remediation is still expected.

Validation order before final decision precedence:

1. Unsupported required-check configuration: if RequiredChecks contains `RiskEvaluation`, return `409 RISK_EVALUATION_NOT_SUPPORTED` before evaluating missing evidence or retry evidence.
2. Unsupported latest evidence status: if any latest required evidence result is `NotAvailable` or `NotSupported`, return `409 UNSUPPORTED_EVIDENCE_RESULT` before missing/retry checks.
3. Missing required evidence: if any required check has no latest accepted evidence, return `409 INCOMPLETE_REQUIRED_CHECKS`.
4. Reason-code safety: validate `ReasonCodes` and `RetryReasonCode` on every latest required EvidenceResult. Any invalid value returns `409 UNSUPPORTED_EVIDENCE_RESULT`.
5. Retry-required evidence: if any latest required evidence result is `RetryRequired`, return `409 CAPTURE_RETRY_REQUIRED`.
6. Only after steps 1-5 pass may final-result precedence evaluate `TechnicalError`, `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, and `Passed`.

Overlap examples:

- `RiskEvaluation` plus missing `FaceMatch` returns `RISK_EVALUATION_NOT_SUPPORTED`.
- `RiskEvaluation` plus latest `RetryRequired` returns `RISK_EVALUATION_NOT_SUPPORTED`.
- Latest `NotSupported` plus latest `RetryRequired` returns `UNSUPPORTED_EVIDENCE_RESULT`.
- Missing `FaceMatch` plus invalid reason code on latest `DocumentNfc` returns `INCOMPLETE_REQUIRED_CHECKS`.
- Latest `RetryRequired` plus invalid reason code on that latest evidence returns `UNSUPPORTED_EVIDENCE_RESULT`.

## 9. Final Decision Calculation Model

Final calculation inputs:

- The session RequiredChecks.
- The latest accepted EvidenceResult for each RequiredCheck.
- Evidence result statuses, reason codes, retry reason codes, payload hashes, engine names, engine versions, and created timestamps.
- Effective completion `requestId` and `correlationId` are completion/package/manifest/audit metadata and participate in TIP-06 canonical outputs where listed.
- Evidence-record request/correlation ids, if present on source evidence records, are not TIP-06 final-decision inputs and are excluded from TIP-06 evidence refs, canonical manifest payloads, package hashes, audit payload hashes, BusinessConsumer DTOs, and TIP-07 compatibility projections.
- Evidence `confidence` is read-only source metadata in current TIP-05 records but is ignored by TIP-06 for final result, assurance level, `CompletedChecks`, `FailedChecks`, reason-code derivation, manifest/package hash inputs, and BusinessConsumer/TIP-07 compatibility projections.
- TIP-06 must not add confidence to evidence refs, manifests, package summaries, or hashes unless a future reviewed slice defines exact confidence semantics and sanitization.

Final calculation output:

- `VerificationDecision.Result`
- `AssuranceLevel`
- `CompletedChecks`
- `FailedChecks`
- `DecisionReasonCodes`
- `RetryReasonCodes`
- `CreatedAt` on `VerificationDecision`; this value is also named `decisionCreatedAt` in the canonical manifest payload.

Precedence, after completeness checks and after rejecting any latest `RetryRequired`:

1. Any latest required check result is `TechnicalError` -> final result `TechnicalError`.
2. Any latest required check result is `FailedIdentity` -> final result `FailedIdentity`.
3. Any latest required check result is `FailedCaptureQuality` -> final result `FailedCaptureQuality`.
4. Any latest required check result is `ReviewRequired` -> final result `ReviewRequired`.
5. All latest required check results are `Passed` -> final result `Passed`.

Unsupported statuses:

- `NotAvailable` must not be accepted as an EvidenceResult final input. If encountered from corrupt or future data, return `409 UNSUPPORTED_EVIDENCE_RESULT` without completing.
- `NotSupported` must not be accepted for TIP-06 final decision. If encountered from corrupt or future data, return `409 UNSUPPORTED_EVIDENCE_RESULT` without completing.

Assurance level:

- `Passed` -> `Medium` in S1 LocalDev unless a future policy explicitly maps assurance levels.
- Non-passed final/review evidence results -> `None`.
- `Unknown` must not be used for TIP-06 deterministic outcomes.

Justification:

- `TechnicalError` is highest precedence because it means the decision pipeline cannot make a business failure claim reliably.
- `FailedIdentity` outranks capture-quality/review because a confirmed identity mismatch must not be softened.
- `FailedCaptureQuality` outranks review because unusable required evidence is a deterministic terminal capture-quality failure.
- `ReviewRequired` completes as a final result in TIP-06 so TIP-07 can later notify clients without implementing OperatorAdmin review now.
- `RetryRequired` prevents completion because TIP-05 still allows recapture while the session remains `InProgress`; turning it into `Completed` would prematurely close remediation.

Decision summary derivation:

- `CompletedChecks` contains every RequiredCheck whose latest accepted evidence result is `Passed`.
- For final `Passed`, `CompletedChecks` contains all RequiredChecks sorted by enum string name.
- `FailedChecks` contains every RequiredCheck whose latest accepted evidence result equals the chosen final result when final result is `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, or `TechnicalError`.
- `FailedChecks` must not include `Passed` checks.
- `RetryRequired` does not produce a final decision/package; therefore it does not produce `CompletedChecks`/`FailedChecks`.
- If multiple required checks have non-passed statuses, `FailedChecks` is filtered to checks matching the winning precedence result only. Lower-precedence non-passed checks still contribute reason codes but not `FailedChecks`.
- `DecisionReasonCodes` is the distinct union of `ReasonCodes` from latest required evidence results that are not `Passed`, plus one deterministic status marker for the final result:
  - `FINAL_TECHNICAL_ERROR`
  - `FINAL_FAILED_IDENTITY`
  - `FINAL_FAILED_CAPTURE_QUALITY`
  - `FINAL_REVIEW_REQUIRED`
  - `FINAL_PASSED`
- For final `Passed`, `DecisionReasonCodes = ["FINAL_PASSED"]` unless future policy explicitly adds non-sensitive pass reason codes.
- `RetryReasonCodes` is the distinct union of present, non-null, safety-validated `RetryReasonCode` values from latest required evidence results. Absent or null `RetryReasonCode` is ignored. Present empty or whitespace-only `RetryReasonCode` is invalid and returns `409 UNSUPPORTED_EVIDENCE_RESULT` before decision/package/manifest creation. Because `RetryRequired` prevents completion, TIP-06 completed packages normally have an empty `RetryReasonCodes` list unless a non-retry terminal evidence record carries a retry reason; this remains allowed only if the value is sanitized.
- Both reason-code arrays are sorted ordinal ascending after de-duplication.
- All check arrays are sorted by enum string name before persistence and canonical manifest hashing.
- Reason-code safety rejects invalid token values with `409 UNSUPPORTED_EVIDENCE_RESULT` before decision/package/manifest creation.
- Canonical timestamp formatting truncates sub-second precision and emits whole-second UTC `Z` strings for manifest, package, and audit hash inputs.
- Unit tests and golden manifest fixtures must assert the exact derived arrays for at least final `Passed`, mixed `FailedIdentity` plus lower-precedence status, `ReviewRequired`, and `TechnicalError`.

Reason-code safety rules:

- Evidence `ReasonCodes` and `RetryReasonCode` values may enter `VerificationDecision`, canonical manifest payloads, package hashes, and TIP-07 compatibility projections only after safety validation.
- Accepted reason-code token format is ASCII uppercase letters, digits, and underscores only: `^[A-Z0-9_]{1,64}$`.
- Empty, whitespace-only, lowercase, mixed-case, punctuation-bearing, path-like, JSON-like, URI-like, or plaintext-looking values are invalid.
- TIP-06 final marker codes are fixed safe tokens: `FINAL_TECHNICAL_ERROR`, `FINAL_FAILED_IDENTITY`, `FINAL_FAILED_CAPTURE_QUALITY`, `FINAL_REVIEW_REQUIRED`, and `FINAL_PASSED`.
- Validate `ReasonCodes` and `RetryReasonCode` on every latest required EvidenceResult before final-result calculation, including latest `Passed` evidence whose reason codes would not otherwise be projected.
- If any latest required evidence result contains an invalid `ReasonCodes` or `RetryReasonCode` value, return `409 UNSUPPORTED_EVIDENCE_RESULT` and do not create a decision/package.
- Invalid reason-code values must not be copied into logs, error details, manifests, package summaries, or compatibility DTOs; tests may assert only the sanitized error code.
- Tests must prove valid codes are sorted/de-duplicated and invalid codes are rejected before decision, manifest, package, audit, or compatibility projection materialization.

## 10. Evidence Status Semantics

`Passed`:

- The required check evidence passed.
- Final `Passed` is allowed only when every required check latest result is `Passed`.
- Final `Passed` requires `ReadyToComplete` state before completion.

`RetryRequired`:

- The latest required evidence says recapture/remediation is still expected.
- `POST /complete` returns `409 CAPTURE_RETRY_REQUIRED`.
- No final decision/package is created.

`FailedCaptureQuality`:

- The required evidence is terminally unusable for verification.
- `POST /complete` may create a final decision/package with result `FailedCaptureQuality` when all required checks have latest evidence and no latest evidence is `RetryRequired`.

`FailedIdentity`:

- The required evidence indicates identity verification failed.
- `POST /complete` may create a final decision/package with result `FailedIdentity`.

`ReviewRequired`:

- Evidence is ambiguous or policy requires manual review.
- TIP-06 completes with final result `ReviewRequired` and creates a package.
- TIP-06 does not create OperatorAdmin review queues, overrides, or manual approval.

`TechnicalError`:

- Adapter/device/service processing failed.
- TIP-06 completes with final result `TechnicalError` only when all required checks have latest evidence and at least one required latest evidence is `TechnicalError`.
- Missing evidence remains `INCOMPLETE_REQUIRED_CHECKS`, not `TechnicalError`.

## 11. Evidence Package Creation Rules

Evidence package creation must use the exact same latest required evidence set used for final decision calculation.

Package inputs:

- Session id.
- Client application id.
- Profile.
- Purpose.
- External session id.
- External transaction id.
- Binding nonce hash.
- Effective completion request id.
- Effective completion correlation id.
- RequiredChecks.
- Latest required EvidenceResults.
- Input capture artifact ids for those EvidenceResults.
- Capture artifact hashes and metadata hashes available from TIP-05 records.
- Evidence payload hashes.
- Engine names and versions.
- Evidence timestamps.
- Final decision id/result.
- Exact TIP-06 audit refs only:
  - `FINAL_DECISION_CALCULATED` as the only canonical pre-package manifest audit ref.
  - `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED` as read-time post-package projections only.
  - Prior session, evidence, package-read, denied-access, incomplete-check, and unrelated audit events are excluded from package records, manifest hash inputs, package hash inputs, and published manifest envelopes.

Creation rules:

- Package creation is append-only.
- Package version is `0.1`.
- `ResultRef` points to the new `VerificationDecision.Id`.
- `EvidenceRefs` must be structured provenance entries, not free-form strings.
- Each structured package evidence entry must carry at least:
  - `evidenceResultId`
  - `resultType`
  - `requiredCheckType`
  - `inputCaptureArtifactIds`
  - `artifactHash` where available
  - `metadataHash` where available
  - `payloadHash`
  - `result`
  - `engineName`
  - `engineVersion`
  - `createdAt`
  - optional internal `vaultRef` only in internal/audit projection
- TIP-06 may add a domain record such as `EvidencePackageEvidenceRef` and contract records such as `ManifestEvidenceRefDto` / `EvidenceRefSummaryDto` fields additively.
- TIP-06 must not pack provenance into strings inside `EvidencePackage.EvidenceRefs`.
- If the current `EvidencePackage.EvidenceRefs` string list remains for compatibility, it must be treated as legacy/untrusted display metadata and must not be the source for BusinessConsumer package summaries or manifest hashing.
- If the current `EvidencePackage.AuditEventRefs` string list remains for compatibility, it must be treated as legacy/untrusted display metadata and must not be the source for canonical manifest payloads, published manifest envelopes, package hashes, or BusinessConsumer summaries.
- Structured `prePackageManifestAuditEventRefs` and projected `postPackageAuditEventRefs` are the TIP-06 source of truth for audit refs.
- `ManifestHash` is calculated before the `EvidencePackage` record is appended.
- `PackageHash` is calculated from the manifest hash plus package envelope fields.
- If manifest construction, manifest canonicalization, manifest hashing, package envelope construction, package hashing, or package creation fails, the session must remain not `Completed`.
- No `Completed` state without `EvidencePackage.Id`, `ManifestHash`, and `PackageHash`.
- Manifest/package construction and hashing failures return `500 EVIDENCE_PACKAGE_CREATION_FAILED` in LocalDev and append no audit event in TIP-06.

## 12. Internal/Audit Manifest Shape

Internal manifest is not the default BusinessConsumer response.

Internal manifest must additively extend `TagEkyc.Contracts.InternalAudit.Manifest`. The current manifest DTO is not sufficient by itself because TIP-06 must hash RequiredChecks, mapped check provenance, and decision summary fields.

TIP-06 must model two internal shapes:

- Canonical manifest payload: the exact fixed-order object hashed into `ManifestHash`; it excludes `manifestHash`.
- Published manifest envelope: wraps the canonical payload with computed `manifestHash`, `packageHash`, and any post-package audit refs.

Canonical manifest payload required shape:

```json
{
  "evidencePackageId": "ep_01",
  "verificationSessionId": "vs_01",
  "clientApplicationId": "client_01",
  "packageVersion": "0.1",
  "profile": "TransactionBoundEkycProfile",
  "purpose": "SIGNING_AUTH",
  "externalSessionId": "sf_session_123",
  "externalTransactionId": "sf_tx_456",
  "bindingNonceHash": "sha256:binding",
  "requestId": "req_200",
  "correlationId": "corr_123",
  "requiredChecks": ["CaptureQuality", "DocumentNfc", "FaceMatch", "Liveness"],
  "resultRef": "decision_01",
  "result": "Passed",
  "assuranceLevel": "Medium",
  "completedChecks": ["CaptureQuality", "DocumentNfc", "FaceMatch", "Liveness"],
  "failedChecks": [],
  "decisionReasonCodes": ["FINAL_PASSED"],
  "retryReasonCodes": [],
  "decisionCreatedAt": "2026-06-11T00:00:00Z",
  "evidenceRefs": [
    {
      "resultType": "NfcValidation",
      "evidenceResultId": "ev_01",
      "requiredCheckType": "DocumentNfc",
      "inputCaptureArtifactIds": ["ca_01"],
      "vaultRef": null,
      "artifactHash": "sha256:artifact",
      "metadataHash": "sha256:metadata",
      "payloadHash": "sha256:payload",
      "engineName": "mock-nfc-reader",
      "engineVersion": "s1.0",
      "result": "Passed",
      "createdAt": "2026-06-11T00:00:00Z"
    }
  ],
  "prePackageManifestAuditEventRefs": [
    {
      "eventId": "audit_decision_01",
      "eventType": "FINAL_DECISION_CALCULATED",
      "eventPayloadHash": "sha256:auditdecision"
    }
  ],
  "evidencePackageSignatureStatus": "PlaceholderUnverified",
  "createdAt": "2026-06-11T00:00:00Z"
}
```

Canonical manifest timestamp fields:

- `decisionCreatedAt` is the prepared `VerificationDecision.CreatedAt` and participates in `ManifestHash`.
- `createdAt` is the prepared `EvidencePackage.CreatedAt` / manifest creation timestamp and participates in `ManifestHash`.
- `decisionCreatedAt` and `createdAt` may have the same LocalDev value if the deterministic preparer chooses so, but tests must assert them by name so an implementation cannot swap their meanings.

Published manifest envelope required shape:

```json
{
  "manifestHash": "sha256:manifest",
  "packageHash": "sha256:package",
  "canonicalPayload": {},
  "postPackageAuditEventRefs": [
    {
      "eventId": "audit_package_01",
      "eventType": "EVIDENCE_PACKAGE_CREATED",
      "eventPayloadHash": "sha256:auditpackage"
    },
    {
      "eventId": "audit_completed_01",
      "eventType": "SESSION_COMPLETED",
      "eventPayloadHash": "sha256:auditcompleted"
    }
  ]
}
```

Canonical audit ref shape:

```json
{
  "eventId": "audit_decision_01",
  "eventType": "FINAL_DECISION_CALCULATED",
  "eventPayloadHash": "sha256:auditdecision"
}
```

Successful audit payload hash schemas:

- Audit payload hashes use the same LocalDev canonicalization helper and `sha256:<lowercase-hex>` format as manifest hashing.
- `eventId`, `occurredAt`, actor key prefix, and raw request body are excluded from audit payload hash inputs.
- Any timestamp included in an audit payload hash input, such as `completedAt`, uses the exact whole-second UTC `yyyy-MM-dd'T'HH:mm:ss'Z'` canonical timestamp format.
- `FINAL_DECISION_CALCULATED` payload hash input fields, in fixed order:
  - `eventType`
  - `verificationSessionId`
  - `clientApplicationId`
  - `verificationDecisionId`
  - `result`
  - `assuranceLevel`
  - `completedChecks`
  - `failedChecks`
  - `decisionReasonCodes`
  - `retryReasonCodes`
  - `requestId`
  - `correlationId`
- `EVIDENCE_PACKAGE_CREATED` payload hash input fields, in fixed order:
  - `eventType`
  - `verificationSessionId`
  - `clientApplicationId`
  - `verificationDecisionId`
  - `evidencePackageId`
  - `manifestHash`
  - `packageHash`
  - `evidencePackageSignatureStatus`
  - `requestId`
  - `correlationId`
- `SESSION_COMPLETED` payload hash input fields, in fixed order:
  - `eventType`
  - `verificationSessionId`
  - `clientApplicationId`
  - `verificationDecisionId`
  - `evidencePackageId`
  - `result`
  - `assuranceLevel`
  - `completedAt`
  - `requestId`
  - `correlationId`
- TIP-06 defines no denial, incomplete-check, package-read, or package-creation-failure audit payload hash schemas. Those audit event types must not be appended in TIP-06.
- Any future slice that adds denial or failure audit events must define fixed safe fields and tests in that later slice, not opportunistically inside TIP-06.
- Tests must include golden canonical payload strings and hashes for the three successful audit payloads.

Rules:

- Internal manifest may contain internal VaultRefs only inside internal/audit projection types.
- Internal manifest must not be returned by default BusinessConsumer package read.
- Internal manifest must not contain raw artifact bytes, raw NFC data groups, raw face/liveness/fingerprint media, fingerprint templates, plaintext document numbers, full name, date of birth, or raw binding nonce.
- If current domain records do not have a safe internal VaultRef for a package item, use `null`; do not invent raw paths or fake vault URIs.
- `RequiredChecks`, `requiredCheckType`, `completedChecks`, `failedChecks`, `decisionReasonCodes`, `retryReasonCodes`, and all structured evidence provenance fields must participate in canonical manifest hashing.
- `prePackageManifestAuditEventRefs` contains exactly one S1 audit ref object: `FINAL_DECISION_CALCULATED`.
- `postPackageAuditEventRefs` is a read-time projection from append-only audit events created after the manifest/package exists, normally `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED`.
- `postPackageAuditEventRefs` contains exactly two S1 audit ref objects in semantic order: `EVIDENCE_PACKAGE_CREATED`, then `SESSION_COMPLETED`.
- `postPackageAuditEventRefs` is scoped to this completion only. It must use the prepared `evidencePackageCreatedAuditEventId` and `sessionCompletedAuditEventId`, or audit records matching this `verificationSessionId` and `evidencePackageId` plus event types `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED`.
- `postPackageAuditEventRefs` must not be built by a global event-type-only filter. Successful completion audit events for other sessions or packages are excluded.
- `postPackageAuditEventRefs` excludes `EVIDENCE_PACKAGE_READ` and any later audit event types.
- `postPackageAuditEventRefs` is not stored by mutating the append-only package record and is not included in `ManifestHash`.
- Tests must prove that the manifest hash does not depend on audit events that cannot exist until after package creation.

## 13. BusinessConsumer Evidence Summary Shape

Default BusinessConsumer package summary must contain only:

- `evidencePackageId`
- `verificationSessionId`
- `packageVersion`
- `packageHash`
- `manifestHash`
- `result`
- `assuranceLevel`
- evidence refs with:
  - evidence type,
  - evidence result id,
  - mapped required check type,
  - artifact hash where available,
  - no VaultRef,
  - no raw path,
  - no TrustedAdapter request body,
  - no CaptureAgent request body,
  - no plaintext identity fields
- `evidencePackageSignatureStatus = PlaceholderUnverified`

TIP-06 must add the following sanitized fields for TIP-07 compatibility:

- `externalSessionId`
- `externalTransactionId`
- `bindingNonceHash`
- `requestId`
- `correlationId`
- `completedAt`

Default BusinessConsumer package summaries do not expose `clientApplicationId`. The package's owning client application identity is available only in internal completion/package projection and the TIP-07 compatibility artifact.

TIP-06 does not need to expose full `documentResult` or `biometricResult` blocks in the default BusinessConsumer package summary. TIP-07 must derive any future webhook per-check sanitized result blocks from internal evidence/manifest records through an explicit mapper, not from raw artifacts and not from the public package summary alone.

Any addition must be contract-tested for raw/internal leakage.

Structured evidence ref projection matrix:

| Field | Domain/package record | Canonical internal manifest ref | BusinessConsumer summary ref |
| --- | --- | --- | --- |
| `evidenceResultId` | Required | Required, hash input | Required |
| `resultType` | Required | Required, hash input | Required |
| `requiredCheckType` | Required | Required, hash input | Required |
| `inputCaptureArtifactIds` | Required | Required, hash input | Not exposed by default |
| `artifactHash` | Optional when available | Optional/null, hash input | Optional when available |
| `metadataHash` | Optional when available | Optional/null, hash input | Not exposed by default |
| `payloadHash` | Required | Required, hash input | Not exposed by default |
| `result` | Required | Required, hash input | Not exposed by default |
| `engineName` | Required | Required, hash input | Not exposed by default |
| `engineVersion` | Required | Required, hash input | Not exposed by default |
| `createdAt` | Required | Required, hash input | Not exposed by default |
| `vaultRef` | Internal only or null | Optional/null, internal only, hash input when present | Never exposed |

Rules:

- Canonical/internal field name for evidence kind is `resultType`.
- Canonical/internal/public identifier field is `evidenceResultId`.
- Public `id` is not a TIP-06 canonical field. If retained for backward compatibility, it must be documented as an alias of `evidenceResultId`, must not participate in manifest/package hashing, and must not replace `evidenceResultId` in tests.
- Public `type` is not a TIP-06 canonical field. If retained for backward compatibility, it must be documented as an alias of `resultType`, must not participate in manifest/package hashing, and must not replace `resultType` in tests.
- BusinessConsumer summary is intentionally a sanitized subset. It does not expose `inputCaptureArtifactIds`, per-evidence `result`, `payloadHash`, engine metadata, `createdAt`, `metadataHash`, or `vaultRef` by default.
- Default BusinessConsumer complete/session/package responses MUST NOT expose JSON `payloadHash` or DTO/property `PayloadHash` in TIP-06. `payloadHash` remains internal/audit manifest data and may be used for future sanitized mappings only after a reviewed contract change.

## 14. Manifest Hash and Deterministic Canonicalization Plan

TIP-06 uses deterministic LocalDev canonicalization. This is not production JCS and is not a cryptographic signature scheme.

Canonicalization rules:

- Build a canonical manifest payload from fixed fields listed in section 12.
- Use fixed property order exactly as documented by the implementation test fixture.
- Use invariant enum names matching contract DTO string serialization.
- Convert timestamps to exact whole-second UTC text format `yyyy-MM-dd'T'HH:mm:ss'Z'`.
- Timestamp canonicalization must first convert to UTC, then truncate sub-second precision toward zero before formatting.
- Canonical timestamps must always end with literal `Z`; `+00:00`, local offsets, fractional seconds, omitted seconds, and framework-default round-trip strings are invalid for manifest, package-envelope, and audit-payload hash inputs.
- Represent absent optional fields as `null` only when the field is part of the canonical schema.
- Sort `requiredChecks`, `completedChecks`, and `failedChecks` by enum string name.
- Sort `evidenceRefs` by `(resultType, evidenceResultId)`.
- Sort each evidence ref `inputCaptureArtifactIds` ascending by string id.
- `prePackageManifestAuditEventRefs` has exact S1 semantic order: `FINAL_DECISION_CALCULATED`.
- `postPackageAuditEventRefs` has exact S1 semantic order when projected: `EVIDENCE_PACKAGE_CREATED`, then `SESSION_COMPLETED`.
- Do not include `postPackageAuditEventRefs` in the manifest-hash input.
- Do not include `manifestHash` itself in the manifest-hash input.
- Hash UTF-8 bytes with SHA-256 and format as `sha256:<lowercase-hex>`.
- Add a golden canonical string/hash fixture in tests that fixes property order, null handling, timestamp format, enum names, and sorting.

`PackageHash` canonicalization:

- Build a package envelope from `evidencePackageId`, `verificationSessionId`, `packageVersion`, `manifestHash`, `resultRef`, `evidencePackageSignatureStatus`, and `createdAt`.
- Use the same fixed-order UTC canonicalization and exact whole-second `Z` timestamp format.
- Hash UTF-8 bytes with SHA-256 and format as `sha256:<lowercase-hex>`.

Boundary:

- This provides deterministic LocalDev integrity metadata only.
- It does not prove authenticity, replay resistance, non-repudiation, or production audit reliance.
- Replacing this with JCS, COSE/JWS, HSM/KMS-backed signing, or production verification requires a future reviewed TIP.

Implementation helper:

- TIP-06 must implement one shared LocalDev canonicalization helper used by both package-building code and tests.
- Tests must include at least one golden canonical manifest payload string and expected `sha256:` hash vector.
- Tests must include at least one golden package envelope string and expected `sha256:` hash vector.
- The helper must reject unsupported value shapes rather than silently serializing framework-default JSON.

## 15. Placeholder Signature and Non-Authoritative Boundary

TIP-06 must set:

```text
EvidencePackageSignatureStatus = PlaceholderUnverified
```

Rules:

- Do not add signature bytes, algorithm, certificate, public key, key id, timestamp authority, or verification endpoint.
- Do not claim the package is signed.
- Do not claim authenticity.
- Do not claim replay protection.
- Do not claim non-repudiation.
- Do not claim external audit reliance.
- `PlaceholderUnverified` must be visible in complete and package-read responses.
- Tests must assert the placeholder value.
- TIP-06 complete and package-read responses must not expose `payloadSignature`, `evidencePackageSignature`, `signature`, `algorithm`, `keyId`, `certificate`, `publicKey`, or signature bytes.
- Older LLD03 examples that show string placeholder signatures are superseded for TIP-06 implementation by this status-only rule.

## 16. Audit Event Append Rules

Audit events are append-only and use safe payload hashes/refs only.

Successful completion must append events in this order:

1. `FINAL_DECISION_CALCULATED`
2. `EVIDENCE_PACKAGE_CREATED`
3. `SESSION_COMPLETED`

Manifest-hash boundary:

- `FINAL_DECISION_CALCULATED` may be included in `prePackageManifestAuditEventRefs` because its id and payload hash are deterministically prepared before manifest hashing. It becomes visible only inside the finalization unit.
- `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED` must be projected as `postPackageAuditEventRefs` outside the canonical manifest payload because those events depend on the package or completed state existing.
- `postPackageAuditEventRefs` is materialized at read time from append-only audit events by selecting this completion's prepared `evidencePackageCreatedAuditEventId` and `sessionCompletedAuditEventId`, or by matching this `verificationSessionId`/`evidencePackageId` plus event types `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED`, then emitting them in that semantic order; event id is identity data, not the ordering key.
- Those events are published inside the same LocalDev finalization unit as completion, and the package record must not be mutated after creation to add them.
- If either post-package audit event cannot be prepared/published, finalization fails and no completion artifacts become visible.
- The implementation must not create a circular hash dependency where a package hash depends on audit events that depend on the package hash.

Deferred non-success/read audit side effects:

- TIP-06 does not append package-read, denied-access, incomplete-check, or package-creation-failure audit events.
- Future audit expansion must pin exact event names, payload schemas, ordering, and tests before implementation.
- Runtime error responses still return deterministic status/error codes, but no audit side effect is required for those non-success paths in TIP-06.

Audit events must not store:

- raw API keys,
- raw request bodies,
- raw artifacts,
- raw document/NFC payloads,
- raw biometric/fingerprint/liveness data,
- plaintext sensitive identity fields,
- raw binding nonce,
- internal secrets.

## 17. BusinessConsumer Read-Surface and Data Exposure Rules

BusinessConsumer APIs must not expose:

- `InternalAudit.Manifest.EvidenceManifestDto`.
- `ManifestVaultRefDto`.
- `VaultRef`.
- Raw artifact refs or raw storage paths.
- Raw document image data.
- Raw NFC data groups.
- Raw face images.
- Raw liveness media.
- Raw fingerprint images or templates.
- Plaintext document number, full name, date of birth, or other sensitive identity fields.
- TrustedAdapter request DTOs.
- CaptureAgent request DTOs.
- Internal adapter execution payloads.
- Production-looking signature material.

Allowed exposure:

- session/package ids,
- final result,
- assurance level,
- evidence result ids,
- artifact hashes,
- manifest hash,
- package hash,
- request id,
- correlation id,
- external session/transaction ids,
- binding nonce hash,
- timestamps,
- placeholder signature status.

TIP-06 default BusinessConsumer complete/session/package DTOs and API JSON must not expose DTO/property `PayloadHash` or JSON `payloadHash`, even as an additive sanitized field. Future public payload hash exposure requires a reviewed contract change and explicit boundary tests.

Default BusinessConsumer exposure explicitly excludes `clientApplicationId`. TIP-06 may expose `externalSessionId`, `externalTransactionId`, and `bindingNonceHash` because those are client-provided correlation/hash fields, but it must keep authenticated owner identity internal unless a future reviewed policy introduces a client-safe public application reference.

## 18. Session Lifecycle Rules

TIP-06 must preserve lifecycle state and verification result as separate concepts.

Rules:

- Final `Passed` can occur only from `ReadyToComplete`.
- `ReadyToComplete -> Completed` is allowed only after final decision and evidence package creation succeed.
- `InProgress -> Completed` is allowed only for non-passed final/review outcomes with complete latest evidence for every RequiredCheck and no `RetryRequired`.
- `RetryRequired` prevents completion and keeps the session `InProgress`.
- `Created` cannot complete.
- `Completed` sessions must have existing final decision and evidence package refs.
- No `Completed` session without package.
- No `Completed` session without manifest hash.
- No final `Passed` without all required checks latest `Passed`.
- No final result may be written before the evidence package and hashes are available in the same application operation.
- If any part of final decision/package creation fails, leave the session in its prior state.

Implementation expectation:

- Current `VerificationSession` is immutable and has `WithState` only. TIP-06 must add a narrow LocalDev/NonProduction completion projection method or repository operation to set `Completed`, final `Result`, `AssuranceLevel`, `VerificationDecisionId`, `EvidencePackageId`, `EvidencePackageHash`, `ManifestHash`, `EvidencePackageSignatureStatus`, effective `RequestId`, effective `CorrelationId`, and `CompletedAt` together.
- That projection must remain behind application/repository abstractions and must not introduce durable persistence.
- A completed projection without decision id, package id, package hash, manifest hash, effective request id, effective correlation id, and completedAt is invalid and must be impossible through the application service.
- Idempotent retry must read from the completion projection or a repository query that reliably resolves the package by session id. TIP-06 must not scan free-form package ref strings to reconstruct completion.

## 19. Error/Status Response Expectations

Expected HTTP status behavior:

- `200 OK` for successful completion.
- `200 OK` for repeated completion after prior success, returning existing summary.
- `200 OK` for successful package read.
- `400 Bad Request` for malformed request or `FORCE_REVIEW_NOT_SUPPORTED`.
- `401 Unauthorized` for missing/invalid/revoked/expired API key.
- `403 Forbidden` for missing scope, wrong caller category, cross-client access, or expired session.
- `404 Not Found` for missing session/package id.
- `409 Conflict` for lifecycle/completeness conflicts.
- `500 Internal Server Error` for LocalDev package creation failure after safe failure handling.
- `500 Internal Server Error` for LocalDev manifest construction/canonicalization/hash or package envelope/hash failure after safe failure handling.
- `500 Internal Server Error` for LocalDev successful-completion audit preparation/publication failure after safe rollback.

Expected error codes:

- `SESSION_NOT_FOUND`
- `EVIDENCE_PACKAGE_NOT_FOUND`
- `FORBIDDEN_CLIENT_APPLICATION`
- `SESSION_ACCESS_DENIED`
- `CALLER_CATEGORY_NOT_ALLOWED`
- `MISSING_SCOPE`
- `SESSION_EXPIRED`
- `SESSION_TERMINAL`
- `INCOMPLETE_REQUIRED_CHECKS`
- `CAPTURE_RETRY_REQUIRED`
- `RISK_EVALUATION_NOT_SUPPORTED`
- `UNSUPPORTED_EVIDENCE_RESULT`
- `SESSION_STATE_CONFLICT`
- `FORCE_REVIEW_NOT_SUPPORTED`
- `EVIDENCE_PACKAGE_CREATION_FAILED`
- `COMPLETION_AUDIT_PUBLICATION_FAILED`

`SESSION_ALREADY_COMPLETED` is legacy/out-of-scope for TIP-06 runtime behavior. No TIP-06 `/complete` path may emit it: completed retries must return the existing snapshot after successful auth/category/scope/ownership/request-option validation, or fail at one of those earlier gates.

Error envelopes must follow existing LLD03/API shape:

```json
{
  "error": {
    "code": "INCOMPLETE_REQUIRED_CHECKS",
    "message": "Required evidence is incomplete.",
    "correlationId": "corr_123",
    "details": {
      "missingChecks": ["DocumentNfc", "FaceMatch"]
    }
  }
}
```

For `INCOMPLETE_REQUIRED_CHECKS`, Section 19 requires the same optional `details.missingChecks` shape defined in Section 8: sorted RequiredCheckType names only, with no raw evidence details. Other error codes may omit `details` unless their own section explicitly defines a sanitized details payload.

Error messages must not leak raw/internal evidence data.

## 20. Forward-Compatibility / No-Regret Boundaries

TIP-07 webhook compatibility:

TIP-06 must preserve these fields for a future webhook payload, without dispatching a webhook:

- `verificationSessionId`
- authenticated `clientApplicationId` in internal completion/package projection and the TIP-07 compatibility artifact only
- `externalSessionId`
- `externalTransactionId`
- `bindingNonceHash`
- final `result`
- `assuranceLevel`
- `evidencePackageId`
- `evidencePackageHash`
- `manifestHash`
- `requestId`
- `correlationId`
- `completedAt`
- `evidencePackageSignatureStatus`

Additive contract expectation:

- TIP-06 must add or update sanitized BusinessConsumer DTOs so all client-safe fields listed above, except internal `clientApplicationId`, are available from completion/session/package reads without exposing the internal manifest.
- BusinessConsumer completion/session/package reads must not expose `clientApplicationId`, owner identity, or a client-safe application alias in TIP-06.
- TIP-06 must update the existing `BusinessConsumer.VerificationCompletedEventDto` additively as the single TIP-07 compatibility artifact, without dispatching webhooks.
- The existing `WebhookSignatureStatus` remains webhook-only and `PlaceholderUnverified`; TIP-06 does not sign or dispatch webhooks.
- Add `EvidencePackageSignatureStatus` separately; it describes the evidence package signature placeholder, not the webhook signature placeholder.
- Add the full field set listed above, including internal `clientApplicationId`, `manifestHash`, `externalTransactionId`, `bindingNonceHash`, `requestId`, `completedAt`, and `evidencePackageSignatureStatus`.
- Existing delivery-oriented fields have TIP-06 placeholder semantics:
  - `EventType = "EKYC_VERIFICATION_COMPLETED"`
  - `DeliveryId = "localdev-not-dispatched"`
  - `SentAt = completedAt`
- Those placeholder values do not imply webhook dispatch, delivery attempt creation, retry behavior, signing, or replay protection.
- Contract tests must assert those TIP-06 placeholder semantics.
- Do not create a second competing verification-completed compatibility DTO in TIP-06 unless reviewer/homeowner explicitly changes this decision.
- Contract tests must prove the compatibility artifact can be populated from the TIP-06 completion/package projection without internal manifest, VaultRef, raw artifacts, or webhook runtime.
- If TIP-07 needs `documentResult` or `biometricResult` blocks, it must derive them from sanitized internal evidence/manifest records through a reviewed mapper. TIP-06 must preserve enough structured evidence provenance to make that possible, but it must not expose raw or internal-only data to BusinessConsumer package reads.

Future production evidence signing:

- Keep `evidencePackageSignatureStatus` distinct from `payloadSignature` and `webhookSignature`.
- Keep manifest canonicalization isolated so production JCS/signing can replace LocalDev hashing.
- Do not bake LocalDev placeholder semantics into contract names beyond `PlaceholderUnverified`.

Future vault-backed retrieval:

- Keep package refs and hashes stable.
- Do not expose raw vault handles in BusinessConsumer DTOs.
- Internal manifest may carry internal refs so future authorized vault retrieval can audit the chain.

Future manual review:

- `ReviewRequired` completes as a final result in TIP-06.
- OperatorAdmin review/override remains future work and must create a new audited decision or follow a reviewed correction model.

Future durable persistence:

- Keep all write behavior behind ports.
- Do not add EF, migrations, durable local store, or production DB schema.
- LocalDev atomicity may be application-level only; production transaction/outbox behavior is deferred.

Retention/audit/legal:

- Package and audit models must avoid raw data and keep hashes/refs for future retention policy.
- No legal certification or external audit reliance claim is allowed.

## 21. Test Plan

Unit tests:

- RequiredCheck to EvidenceResultType mapping.
- Completeness returns `INCOMPLETE_REQUIRED_CHECKS` for missing evidence.
- `RetryRequired` returns `CAPTURE_RETRY_REQUIRED` and does not complete.
- Decision precedence: `TechnicalError`, `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, `Passed`.
- `CompletedChecks`, `FailedChecks`, `DecisionReasonCodes`, and `RetryReasonCodes` derive deterministically from latest required evidence, including sort/de-dup rules.
- `Passed` requires all latest required evidence `Passed`.
- Latest accepted evidence by append order controls final decision.
- `RiskEvaluation` required check returns `RISK_EVALUATION_NOT_SUPPORTED` under current TIP-05 constraints.
- `NotSupported` or `NotAvailable` evidence inputs return deterministic conflict.
- `NotAvailable` and `NotSupported` both return `409 UNSUPPORTED_EVIDENCE_RESULT`.
- State/evidence validation ladder tests cover `Created + RiskEvaluation`, `Created + complete required evidence`, `ReadyToComplete + NotSupported`, and `ReadyToComplete + non-passed terminal/review evidence`.
- Assurance level mapping: `Passed -> Medium`, non-passed -> `None`.
- Completion idempotent retry returns existing package and does not append duplicates.
- Completed-session retry validates auth, caller category, scope, and ownership before returning existing snapshot.
- Completed-session retry after expiry returns existing snapshot only after auth/scope/ownership succeeds.
- Package creation failure leaves session not `Completed`.
- Package creation/finalization failure leaves no visible decision, pre-package audit event, package, completion projection, or idempotent snapshot.
- Post-package audit event preparation/publication failure leaves no visible decision, pre-package audit event, package, completion projection, idempotent snapshot, or post-package audit event.
- Retry after package creation/finalization failure creates the first visible decision/package only after success.
- Retry after no-visible package/finalization failure with different requestId/correlationId values is a new attempt and uses the new effective ids.
- Retry after no-visible package/finalization failure advances the injected clock and proves the second attempt's `completedAt`, `decisionCreatedAt`, package `createdAt`, `VerificationCompletedEventDto.SentAt`, manifest hash input, and package hash input use the second attempt's `operationNow`.
- Manifest construction, canonicalization, manifest hashing, package envelope construction, and package hashing failures return `EVIDENCE_PACKAGE_CREATION_FAILED` and leave no visible decision, audit event, package, completion projection, or idempotent snapshot.
- Manifest canonicalization produces stable hashes independent of insertion ordering.
- Manifest hash includes RequiredChecks, mapped required check types, structured provenance, and decision summary fields.
- Manifest hash excludes `manifestHash` and post-package audit refs.
- Manifest pre-package audit refs use exact object shape and exact S1 member set `[FINAL_DECISION_CALCULATED]`.
- Successful completion audit event ids are prepared deterministically before manifest hashing and used unchanged by the finalization boundary.
- Projected post-package audit refs use exact object shape and exact S1 semantic order `[EVIDENCE_PACKAGE_CREATED, SESSION_COMPLETED]`.
- Projected post-package audit refs exclude `EVIDENCE_PACKAGE_READ` and later audit event types.
- Successful audit payload hashes have golden fixtures for `FINAL_DECISION_CALCULATED`, `EVIDENCE_PACKAGE_CREATED`, and `SESSION_COMPLETED`.
- Golden canonical manifest fixture fixes property order, null handling, timestamp format, enum names, and sorting.
- Final `Passed` golden manifest fixture uses `DecisionReasonCodes = ["FINAL_PASSED"]`.
- Golden fixtures use evidence records whose source `RequestId`/`CorrelationId` differ from the effective completion request/correlation ids; changing only evidence-record request/correlation ids must not change decision, package, manifest, audit, or TIP-07 compatibility outputs.
- Golden fixtures prove `evidenceResultId` remains required and hash-participating: changing `evidenceResultId` changes the canonical manifest hash and package hash.
- Unrelated prior session/evidence/read audit events do not enter package records, canonical manifest hash inputs, package hash inputs, or published manifest envelopes.
- Successful completion audit events for another session/package, even with event types `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED`, do not enter this package record, canonical manifest hash input, package hash input, or published manifest envelope.
- Package hash changes when manifest hash changes.

Contract tests:

- Complete response DTO is sanitized.
- BusinessConsumer evidence package summary does not expose `VaultRef`, `Raw`, `Biometric`, `Template`, `Plaintext`, `TrustedAdapter`, `CaptureAgent`, or `InternalAudit`.
- `ClientApplicationId` is allowed only on the internal completion/package projection and `BusinessConsumer.VerificationCompletedEventDto`; complete response DTOs, session summary DTOs, evidence package summary DTOs, and their API JSON must assert `clientApplicationId` / `ClientApplicationId` absence.
- Internal manifest projection is the only projection allowed to contain `ManifestVaultRefDto`.
- Placeholder signature status remains `PlaceholderUnverified`.
- Evidence package summary does not reuse `EvidenceManifestDto`.
- TIP-07 compatibility fields remain available from completion/package/session summaries without exposing internal manifest.
- Structured package evidence refs carry `evidenceResultId`, `resultType`, `requiredCheckType`, `inputCaptureArtifactIds`, artifact hash, payload hash, and optional internal-only VaultRef.
- BusinessConsumer evidence refs expose `evidenceResultId`, `resultType`, `requiredCheckType`, and optional artifact hash only by default.
- BusinessConsumer package evidence refs must not expose per-evidence `result` by default in TIP-06; final result is exposed at the completion/session/package summary level, and TIP-07 per-check result blocks must be derived from sanitized internal evidence/manifest records through a reviewed mapper.
- Public `id` does not replace canonical `evidenceResultId`.
- Deprecated public aliases `Id`/`Type`, if retained, mirror `EvidenceResultId`/`ResultType` and are excluded from hash inputs.
- Public evidence refs cannot contain `vault://`, `file:`, `raw`, `VaultRef`, or `InternalAudit`.
- Complete and package-read DTOs expose only `evidencePackageSignatureStatus`, not `payloadSignature`, `evidencePackageSignature`, algorithm, key id, certificate, public key, or signature bytes.
- Complete, completed-session, and package-read DTOs do not expose `clientApplicationId`, `payloadHash`, `PayloadHash`, `InternalAudit`, `VaultRef`, raw/path markers, TrustedAdapter/CaptureAgent markers, or signature material.
- Reflection/contract tests prove no default BusinessConsumer complete/session/package DTO has a public `PayloadHash` member.
- TIP-07 compatibility artifact exposes the full future webhook field set without webhook dispatch/runtime.
- TIP-07 compatibility artifact asserts `EventType`, LocalDev placeholder `DeliveryId`, `SentAt = completedAt`, `WebhookSignatureStatus`, and `EvidencePackageSignatureStatus` semantics.
- `EvidencePackage.AuditEventRefs`, if retained, is legacy/untrusted and not a source for canonical manifest/package projections.

Integration/API pipeline tests:

- Missing/unknown API key returns `401` through HTTP for `POST /api/ekyc/verification-sessions/{id}/complete`.
- Missing/unknown API key returns `401` through HTTP for `GET /api/ekyc/evidence-packages/{id}`.
- BusinessConsumer with `session.complete` can complete a ready passed session through HTTP.
- BusinessConsumer without `session.complete` gets `403 MISSING_SCOPE`.
- `POST /api/ekyc/verification-sessions/{missingId}/complete` as an otherwise authorized BusinessConsumer returns `404 SESSION_NOT_FOUND` and creates no decision/package/audit completion records.
- `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED` through HTTP and creates no decision/package.
- `forceReview = true` with missing/invalid API key returns `401` before request-option validation.
- `forceReview = true` with wrong caller category returns `403 CALLER_CATEGORY_NOT_ALLOWED` before request-option validation.
- `forceReview = true` without `session.complete` returns `403 MISSING_SCOPE` before request-option validation.
- `forceReview = true` for a syntactically valid unknown session id returns `404 SESSION_NOT_FOUND` before request-option validation.
- `forceReview = true` for a known cross-client session returns `403 FORBIDDEN_CLIENT_APPLICATION` before request-option validation.
- `forceReview = true` with unresolved client policy or broader authorization context returns `403 SESSION_ACCESS_DENIED` before request-option validation.
- Completed-session retry with `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED`, not idempotent success.
- Expired non-completed session with `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED`, not `SESSION_EXPIRED`, after auth/scope/ownership succeeds.
- `VerificationSessionState.Cancelled` with `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED`, not `SESSION_TERMINAL`, after auth/scope/ownership succeeds and creates no decision/package/audit/completed projection.
- `VerificationSessionState.TechnicalTerminal` with `forceReview = true` returns `400 FORCE_REVIEW_NOT_SUPPORTED`, not `SESSION_TERMINAL`, after auth/scope/ownership succeeds and creates no decision/package/audit/completed projection.
- Request body containing extra `requestedBy` is ignored by current default JSON binding and must not affect authorization, audit actor identity, manifest content, or response fields.
- Expired session completion returns `403 SESSION_EXPIRED` through HTTP and creates no decision/package.
- `VerificationSessionState.Expired` returns `403 SESSION_EXPIRED` through HTTP before terminal/evidence validation and creates no decision/package.
- Completion captures one `operationNow` before expiry validation; if `ExpiresAt > operationNow`, no later expiry recheck can fail the same attempt during package preparation/finalization.
- `VerificationSessionState.Cancelled` completion returns `409 SESSION_TERMINAL` through HTTP and creates no decision/package/completed projection/idempotent snapshot.
- `VerificationSessionState.TechnicalTerminal` completion returns `409 SESSION_TERMINAL` through HTTP and creates no decision/package/completed projection/idempotent snapshot.
- Terminal-state overlap tests assert `VerificationSessionState.Expired` returns `403 SESSION_EXPIRED`, and `VerificationSessionState.Cancelled` plus `VerificationSessionState.TechnicalTerminal` return `409 SESSION_TERMINAL`, before `RiskEvaluation`, missing evidence, unsupported evidence, invalid reason-code, or retry evidence validation.
- Session requiring `RiskEvaluation` returns `409 RISK_EVALUATION_NOT_SUPPORTED` through HTTP before missing/retry errors and creates no decision/package/completed projection/idempotent snapshot.
- Package creation failure returns `500 EVIDENCE_PACKAGE_CREATION_FAILED` through HTTP and creates no visible decision/package/completed projection/idempotent snapshot.
- Successful-completion audit preparation/publication failure returns `500 COMPLETION_AUDIT_PUBLICATION_FAILED` through HTTP and creates no visible decision/package/completed projection/idempotent snapshot/audit event.
- `FINAL_DECISION_CALCULATED` audit preparation/publication/append failure returns `500 COMPLETION_AUDIT_PUBLICATION_FAILED` through HTTP and creates no visible decision/package/completed projection/idempotent snapshot/audit event.
- Validation overlap tests assert `RiskEvaluation + missing`, `RiskEvaluation + retry`, `Created + RiskEvaluation`, `NotSupported + RetryRequired`, `ReadyToComplete + NotSupported`, and `ReadyToComplete + non-passed terminal/review latest evidence` precedence.
- Validation overlap tests assert `RiskEvaluation + NotSupported` and `RiskEvaluation + NotAvailable` return `409 RISK_EVALUATION_NOT_SUPPORTED` and create no decision/package/audit/completed projection.
- Validation overlap tests assert latest `NotSupported` or `NotAvailable` plus another missing required check returns `409 UNSUPPORTED_EVIDENCE_RESULT` and creates no decision/package/audit/completed projection.
- Validation overlap tests assert `Created + NotSupported` and `Created + NotAvailable` return `409 UNSUPPORTED_EVIDENCE_RESULT` and create no decision/package/audit/completed projection.
- Validation overlap tests assert `Created + RetryRequired` returns `409 CAPTURE_RETRY_REQUIRED` and creates no decision/package/audit/completed projection.
- Validation overlap tests assert `Created + invalid ReasonCodes` and `Created + invalid RetryReasonCode` return `409 UNSUPPORTED_EVIDENCE_RESULT` and create no decision/package/audit/completed projection.
- Validation overlap tests assert missing required evidence plus invalid reason code returns `INCOMPLETE_REQUIRED_CHECKS`.
- Validation overlap tests assert latest `RetryRequired` evidence plus invalid `ReasonCodes` or invalid `RetryReasonCode` returns `UNSUPPORTED_EVIDENCE_RESULT`.
- Completed-session retry with missing `session.complete` returns `403 MISSING_SCOPE`.
- Completed-session retry by another client returns `403 FORBIDDEN_CLIENT_APPLICATION`.
- Completed-session retry after `ExpiresAt` has passed returns `200 OK` only for the authorized owning caller with scope.
- Repeated complete with different second `requestId`/`correlationId` returns the original completed snapshot values and leaves package/session/audit metadata unchanged.
- New external retry after no-visible package/finalization failure with different requestId/correlationId values uses the new effective request/correlation ids and creates no orphan records from the failed attempt.
- New external retry after no-visible package/finalization failure with an advanced injected clock uses the second attempt's `operationNow` for all prepared timestamps and hash inputs.
- `/complete` with unresolved client policy or broader authorization context returns `403 SESSION_ACCESS_DENIED`.
- CaptureAgent/TrustedAdapter cannot call `/complete`: return `403 CALLER_CATEGORY_NOT_ALLOWED` before scope checks, session lookup, or ownership checks.
- Caller-category precedence tests for `/complete` cover CaptureAgent/TrustedAdapter with missing route session, missing `session.complete`, and cross-client target; all return `403 CALLER_CATEGORY_NOT_ALLOWED`.
- Cross-client complete for a known session returns `403 FORBIDDEN_CLIENT_APPLICATION`.
- Missing required evidence returns `409 INCOMPLETE_REQUIRED_CHECKS` through HTTP.
- `INCOMPLETE_REQUIRED_CHECKS` HTTP response includes `error.details.missingChecks`, sorted by enum string name and sanitized.
- `InProgress` session with all latest required evidence `Passed` returns `409 SESSION_STATE_CONFLICT` through HTTP.
- `NotAvailable` or `NotSupported` evidence input returns `409 UNSUPPORTED_EVIDENCE_RESULT` through HTTP.
- Invalid evidence `ReasonCodes` values return `409 UNSUPPORTED_EVIDENCE_RESULT` through HTTP and create no decision/package/audit/completed projection.
- Invalid evidence `RetryReasonCode` values return `409 UNSUPPORTED_EVIDENCE_RESULT` through HTTP and create no decision/package/audit/completed projection.
- Present empty or whitespace-only `RetryReasonCode` returns `409 UNSUPPORTED_EVIDENCE_RESULT`; absent or null `RetryReasonCode` is ignored for `RetryReasonCodes` derivation.
- Invalid evidence `ReasonCodes` on a latest `Passed` required evidence result still returns `409 UNSUPPORTED_EVIDENCE_RESULT` before final decision calculation.
- Latest `RetryRequired` returns `409 CAPTURE_RETRY_REQUIRED` through HTTP.
- FailedIdentity/FailedCaptureQuality/ReviewRequired/TechnicalError completion paths return sanitized final responses when eligible.
- Successful complete returns `Completed`, final result, evidence package id/hash, manifest hash, placeholder signature status, request id, correlation id, and completedAt.
- Successful complete response asserts full required field set, including `externalSessionId`, `externalTransactionId`, and `bindingNonceHash`.
- Completed `GET /api/ekyc/verification-sessions/{id}` asserts full completed field set:
  - `externalSessionId`
  - `externalTransactionId`
  - `bindingNonceHash`
  - `result`
  - `assuranceLevel`
  - `evidencePackageId`
  - `evidencePackageHash`
  - `manifestHash`
  - `evidencePackageSignatureStatus`
  - `requestId`
  - `correlationId`
  - `completedAt`
- Completed session read after a complete request with requestId/correlationId overrides returns the effective completion values, not the original create-session values.
- Completed session GET for final `Passed` asserts `result = Passed` and `assuranceLevel = Medium`, sourced from the completion snapshot/final decision.
- Completed session GET for final `FailedIdentity` asserts `result = FailedIdentity` and `assuranceLevel = None`, sourced from the completion snapshot/final decision.
- Completed session GET for final `FailedCaptureQuality` asserts `result = FailedCaptureQuality` and `assuranceLevel = None`, sourced from the completion snapshot/final decision.
- Completed session GET for final `ReviewRequired` asserts `result = ReviewRequired` and `assuranceLevel = None`, sourced from the completion snapshot/final decision.
- Completed session GET for final `TechnicalError` asserts `result = TechnicalError` and `assuranceLevel = None`, sourced from the completion snapshot/final decision.
- Completed session GET JSON does not contain `clientApplicationId`, `ClientApplicationId`, `payloadHash`, `PayloadHash`, `payloadSignature`, `evidencePackageSignature`, `algorithm`, `keyId`, `certificate`, `publicKey`, `VaultRef`, `vault://`, `file:`, `raw`, `InternalAudit`, `TrustedAdapter`, or `CaptureAgent`.
- Repeated complete after success returns existing final summary without duplicate package.
- Concurrent or repeated complete attempts cannot append duplicate decisions/packages; one completion snapshot wins or a deterministic conflict is returned.
- Any successful-completion audit append/preparation failure, including `FINAL_DECISION_CALCULATED`, `EVIDENCE_PACKAGE_CREATED`, or `SESSION_COMPLETED`, returns `500 COMPLETION_AUDIT_PUBLICATION_FAILED` and creates no visible completed artifacts.
- Completion JSON does not contain `clientApplicationId`, `ClientApplicationId`, `payloadHash`, `PayloadHash`, `payloadSignature`, `evidencePackageSignature`, `algorithm`, `keyId`, `certificate`, `publicKey`, `VaultRef`, `vault://`, `file:`, `raw`, `InternalAudit`, `TrustedAdapter`, or `CaptureAgent`.
- GET evidence package returns sanitized BusinessConsumer summary.
- GET evidence package response asserts full required field set, including `externalSessionId`, `externalTransactionId`, `bindingNonceHash`, `requestId`, `correlationId`, and `completedAt`.
- GET evidence package JSON does not serialize the internal manifest, free-form domain evidence refs, `clientApplicationId`, `ClientApplicationId`, `payloadHash`, `PayloadHash`, `payloadSignature`, `evidencePackageSignature`, `algorithm`, `keyId`, `certificate`, `publicKey`, `VaultRef`, `vault://`, `file:`, `raw`, `InternalAudit`, `TrustedAdapter`, or `CaptureAgent`.
- Evidence records with source request/correlation ids different from the complete request override do not leak those evidence-level ids into complete JSON, completed session GET JSON, package GET JSON, internal manifest canonical payloads, package/audit hash inputs, or `VerificationCompletedEventDto`.
- GET evidence package as non-BusinessConsumer returns `403 CALLER_CATEGORY_NOT_ALLOWED`.
- GET evidence package without `business.session.read` returns `403 MISSING_SCOPE`.
- GET evidence package precedence tests cover wrong category before package lookup, missing scope before cross-client ownership, missing package for an otherwise authorized caller, unresolved backing session/policy, and cross-client known package.
- GET evidence package with a malformed package id as an otherwise authorized BusinessConsumer returns `404 EVIDENCE_PACKAGE_NOT_FOUND` before backing-session or ownership checks.
- Cross-client package read for a known package returns `403 FORBIDDEN_CLIENT_APPLICATION`.
- Missing package read returns `404`.
- Successful GET evidence package does not append `EVIDENCE_PACKAGE_READ` or any read audit event; the audit store count remains unchanged and projected `postPackageAuditEventRefs` remains exactly `EVIDENCE_PACKAGE_CREATED` then `SESSION_COMPLETED`.
- Package-read denial paths for wrong caller category, missing `business.session.read`, malformed package id, missing package, unresolved backing session/policy, and known cross-client package must assert the audit store count is unchanged and no audit event of any type is appended. Where a package exists, projected `postPackageAuditEventRefs` remains exactly `EVIDENCE_PACKAGE_CREATED` then `SESSION_COMPLETED`.
- Representative `/complete` non-success paths for missing scope, known cross-client session, expired session, `VerificationSessionState.Cancelled`, `VerificationSessionState.TechnicalTerminal`, missing evidence, `RetryRequired`, unsupported latest evidence, invalid reason-code, and package creation failure must assert the audit store count is unchanged and no audit event of any type is appended; no decision/package/completed projection/idempotent snapshot becomes visible.
- Successful-completion audit append/preparation/publication failure tests are explicit fault-injection paths. They may exercise a failed append attempt internally, but no audit event of any type may be visible after rollback/failure handling, and no decision/package/completed projection/idempotent snapshot may be visible.
- Complete happy path includes BusinessConsumer create, CaptureAgent artifact append, TrustedAdapter evidence append, BusinessConsumer complete, BusinessConsumer GET package.

Architecture/boundary tests:

- No EF provider, `DbContext`, migrations, durable local store, schema/model mapping, or production DB choice.
- No production vault/file/object storage or raw artifact retrieval.
- No `IFormFile`, `FileStream`, multipart upload, base64/raw payload runtime.
- No webhook dispatcher/runtime/retry/signing endpoint.
- No SignFlow runtime/source/database/internal package dependency.
- BusinessConsumer contracts do not reference TrustedAdapter or InternalAudit manifest DTOs.
- Internal manifest contracts are not returned by API endpoint delegates.

Proof-level/API pipeline tests:

- Every new runtime endpoint has HTTP pipeline tests, not service-only tests.
- Route inventory confirms only the two TIP-06 routes are added and deferred webhook/retry/specialized evidence/admin routes remain absent.
- Boundary grep is run and interpreted.

Expected validation commands after implementation:

```text
dotnet test TagEkyc.sln --no-restore
rg -n "EntityFramework|DbContext|Migration|Npgsql|SqlServer|Mongo|IFormFile|FileStream|FromForm|base64|Base64|byte\\[\\]|raw|Raw|VaultRef|keyHash|secret|rotation|webhook|Webhook|Signature|EvidenceManifest" src tests -g "!**/bin/**" -g "!**/obj/**"
git ls-files | rg '(^|/)(bin|obj|TestResults)/'
```

Any expected scan hit must be reviewed as allowed contract/test/doc usage or treated as STOP+ASK.

## 22. STOP+ASK Gates

STOP+ASK before doing any of the following:

- Implementing production cryptography or package signing.
- Adding signature bytes, algorithms, keys, certificates, KMS/HSM, timestamp authority, or verification runtime.
- Claiming authenticity, replay protection, non-repudiation, legal audit reliance, external audit reliance, or production-certified eKYC readiness.
- Implementing webhook dispatcher/runtime/retry/signing or `/api/ekyc/webhooks/retry`.
- Implementing production vault/file/object storage or artifact retrieval.
- Exposing internal VaultRefs in BusinessConsumer responses.
- Exposing `clientApplicationId`, owner identity, or a newly defined client-safe application reference in default BusinessConsumer completion/session/package responses.
- Exposing DTO/property `PayloadHash` or JSON `payloadHash` in default BusinessConsumer completion/session/package responses.
- Exposing raw artifacts, raw payload refs, raw biometrics, raw document/NFC data, raw face/liveness/fingerprint data, fingerprint templates, or plaintext identity fields.
- Implementing durable persistence, EF provider, `DbContext`, migrations, schema/model mapping, durable local store, production DB, or production storage infrastructure.
- Adding SignFlow source/database/runtime/internal dependency.
- Implementing OperatorAdmin review, override, or review queue.
- Implementing production FraudRisk model or accepting `RiskEvaluation` completion without a reviewed evidence source.
- Completing a session without evidence package id, package hash, and manifest hash.
- Allowing final `Passed` without all required checks latest `Passed`.
- Collapsing `RetryRequired`, `FailedCaptureQuality`, `FailedIdentity`, `ReviewRequired`, or `TechnicalError` into generic failure.
- Returning internal/audit manifest from the default BusinessConsumer package read endpoint.
- Adding runtime endpoints without API pipeline tests.
- Changing TIP-05 capture/evidence write behavior after `ReadyToComplete`.
- Adding, removing, or renaming lifecycle states. Builder must verify `VerificationSessionState.TechnicalTerminal` exists in the current repo before implementation; if not, STOP+ASK before adding or remapping terminal-state behavior.
- Modifying pre-existing dirty non-TIP-06 files unless explicitly authorized.

## 23. Explicit Non-Production Boundaries

TIP-06 S1 runtime remains LocalDev/NonProduction for:

- In-memory decision/package storage.
- Application-level atomicity.
- Manifest canonicalization.
- Manifest/package hashing.
- Placeholder signature status.
- Fake/sample client credentials and scopes.
- Audit append behavior.

TIP-06 must not claim:

- production persistence durability,
- production audit durability,
- production cryptographic integrity,
- production signature verification,
- replay protection,
- legal certification,
- regulatory approval,
- production biometric assurance,
- production vault readiness,
- production webhook reliability.

Before pilot or production use, separate review is required for durable transactions, outbox/webhook delivery, production signing keys, vault storage/retrieval, retention/deletion/legal hold, privacy controls, incident response, rate limiting, and legal/compliance posture.

## 24. Open Questions Requiring Homeowner/Reviewer Decision Before Implementation

No implementation-blocking open questions remain in this draft. The following decisions are pinned for review:

- `RetryRequired` prevents completion and returns `409 CAPTURE_RETRY_REQUIRED`.
- `FailedCaptureQuality`, `FailedIdentity`, `ReviewRequired`, and `TechnicalError` can complete with final package when every required check has latest evidence and no required check is `RetryRequired`.
- `ReviewRequired` completes as final `ReviewRequired`; OperatorAdmin review is future work.
- Repeated complete after success returns existing summary/package in LocalDev and does not append duplicates.
- `RiskEvaluation` cannot complete under current TIP-05 because `FraudRisk` evidence is deferred.
- LocalDev canonicalization is deterministic but non-production and not JCS.
- GET evidence package returns BusinessConsumer summary only, not internal manifest.

Reviewer/homeowner may request patches to these choices before implementation dispatch. If any pinned decision changes, update sections 7-21 and test expectations together.

## 25. Multi-Agent Self-Check Report

Review loop protocol:

- Reviews are tracked by internal draft version.
- For rounds before v0.29, a draft is not converged unless Reviewer A and Reviewer B review the same patched version using the same full convergence checklist and both report no implementation-affecting findings.
- From v0.29 onward, convergence uses the scoped Lane A/Lane B protocol recorded below instead of the old same-full-checklist A/B protocol.
- If any patch is applied after a pre-v0.29 convergence review, the same-version convergence review must be rerun. If any patch is applied after a post-v0.29 lane/final architect review, at least Lane A carry-through review must be rerun; Lane B reruns only for blocker-risk scope changes.
- Specialized builder/adversarial/evidence/proof/final checks do not replace the applicable convergence protocol for that draft version.
- Reviewers must stay within their assigned checklist and include a coverage attestation for that checklist. Pre-v0.29 A/B reviewers must perform exhaustive full-document/full-checklist passes; post-v0.29 Lane A/Lane B reviewers follow their scoped lane checklists.
- If a reviewer returns findings without attesting that the assigned checklist was scanned, the review is treated as incomplete and must be rerun with the applicable prompt.

Round 1 reviewed draft:

- Draft version: v0.1 initial draft.
- Reviewer A checklist: Security/Data Boundary/Evidence Integrity full lane.
- Reviewer B checklist: Lifecycle/Decision/Forward Compatibility full lane.
- Builder reviewer checklist: implementer ambiguity pass.

Round 1 Reviewer A findings:

| Finding ID | Severity | Issue | Patch applied |
| --- | --- | --- | --- |
| R1A-01 | High | Audit refs for package/complete events could create circular manifest hash dependency. | Split audit refs into `prePackageManifestAuditEventRefs` included in canonical hash and `postPackageAuditEventRefs` excluded from hash. |
| R1A-02 | High | Manifest did not include RequiredChecks, mapped check type, completed/failed checks, or decision reason codes. | Made TIP-06 internal manifest schema mandatory and added those fields to canonical payload/hash inputs. |
| R1A-03 | Medium | Manifest DTO/example included `manifestHash` while canonicalization excluded it. | Defined canonical manifest payload excluding `manifestHash` and published envelope carrying computed hash. |
| R1A-04 | Medium | Free-form `EvidencePackage.EvidenceRefs` could leak VaultRefs/raw refs into BusinessConsumer summary. | Required structured internal/public evidence provenance entries and prohibited string-packing as source for summary/hash. |
| R1A-05 | Medium | Older LLD03 `payloadSignature` / string placeholder examples conflict with TIP-06 status-only boundary. | TIP-06 now explicitly supersedes those examples and requires status-only responses/tests. |
| R1A-06 | Medium | Completed state/idempotency lacked package/hash/manifest projection data. | Required finalization projection/snapshot with decision id, package id/hash, manifest hash, assurance, and completedAt. |

Round 1 Reviewer B findings:

| Finding ID | Severity | Issue | Patch applied |
| --- | --- | --- | --- |
| TIP06-RB-01 | High | Atomic completion/idempotency could be violated by separate append/package/set-state calls. | Required single compare-and-set completion finalization boundary and completed snapshot. |
| TIP06-RB-02 | High | Current package refs and manifest contracts cannot represent evidence-result to artifact provenance cleanly. | Required additive structured domain/contracts for package evidence entries. |
| TIP06-RB-03 | Medium | TIP-07 compatibility fields and per-check result derivation were not pinned. | Required additive sanitized completion/session/package fields and stated TIP-07 derives per-check blocks from internal evidence/manifest records. |

Round 1 Builder reviewer findings:

| Finding ID | Severity | Issue | Patch applied |
| --- | --- | --- | --- |
| TIP06-A01 | High | Completion projection path missing for result/assurance/decision/package/hash persistence. | Covered by finalization boundary and completed projection requirements. |
| TIP06-A02 | High | Idempotent complete needs package lookup by session. | Required `GetBySessionIdAsync`/equivalent completion lookup. |
| TIP06-A03 | High | Provenance types were under-specified relative to current string refs. | Required explicit structured provenance types and fields. |
| TIP06-A04 | High | Endpoint implementation scaffolding could be partial. | Required DTOs, ports, route mapping, DI registration, and LocalDev repos. |
| TIP06-A05 | High | LocalDev policy lacks `session.complete` scope fixtures. | Required scope constant, success key, missing-scope key, and policy updates. |
| TIP06-A06 | Medium | Completed session summary fields are currently hardcoded to non-final defaults. | Required completion response DTO and additive completed summary fields. |
| TIP06-A07 | Medium | Missing-check error detail shape was not supported by current error envelope. | Required optional `details.missingChecks` error shape. |
| TIP06-A08 | Medium | RequestId/correlationId completion precedence was not pinned. | Pinned request values override session defaults when non-blank. |
| TIP06-A09 | Medium | Canonicalization lacked shared helper and exact test vectors. | Required shared helper and golden canonical/hash fixtures. |
| TIP06-A10 | Medium | `requiredCheckType` appeared in sample but not DTO. | Required additive `RequiredCheckTypeDto RequiredCheckType` on `EvidenceRefSummaryDto`. |

Patches after Round 1:

- Applied all implementation-affecting Reviewer A findings.
- Applied all implementation-affecting Reviewer B findings.
- Applied all implementation-affecting Builder reviewer findings.
- Internal draft label after patches: v0.2.

Re-review status:

- Reviewer A/B same-checklist convergence on v0.2: completed with implementation-affecting findings.
- Builder reviewer re-run is not required for A/B convergence, but any later builder/adversarial/proof patch will trigger A/B same-checklist re-review.

Round 2 reviewed draft:

- Draft version: v0.2 after Round 1 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 3 implementation-affecting findings; Reviewer B found 5 implementation-affecting findings.

Round 2 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.3 |
| --- | --- | --- | --- |
| TIP06-R2-01 | High | Completion atomicity still allowed visible orphan decision/package records on conflict/failure. | Required LocalDev finalization boundary to publish decision, pre-package audit, package, completed projection, and idempotent snapshot as one visible unit; failure leaves none visible. |
| TIP06-R2-02 | High | `postPackageAuditEventRefs` storage/update behavior was not deterministic. | Defined post-package refs as read-time projection from append-only audit events, sorted by audit event id, never package-record mutation. |
| TIP06-R2-03 | Medium | Report status still said pending/NEEDS PATCHES while v0.2 was under convergence review. | Deferred final status update until same-version convergence and later passes complete; report now records v0.2 findings and v0.3 re-review requirement. |
| TIP06-CONV-01 | High | Audit append ordering contradicted pre-package manifest audit refs. | Pinned one successful completion sequence with prepared pre-package audit ref before canonical manifest hashing. |
| TIP06-CONV-02 | High | Retry after package failure could reuse or duplicate orphan decisions inconsistently. | Chose no-visible-partial-record failure policy; retry after failure creates the first visible records only on success. |
| TIP06-CONV-03 | Medium | Evidence ref identifier naming mixed `id` and `evidenceResultId`. | Standardized canonical/internal/public fields on `evidenceResultId`; any public `id` is only a compatibility alias outside canonical hashing. |
| TIP06-CONV-04 | Medium | Cross-client ownership denial allowed two error codes. | Pinned known cross-client target to `FORBIDDEN_CLIENT_APPLICATION`; unresolved policy/auth context to `SESSION_ACCESS_DENIED`. |
| TIP06-CONV-05 | Medium | API pipeline plan missed `forceReview`, expired session, and package-read caller/scope denials. | Added explicit API pipeline tests with status/code expectations. |

Patches after Round 2:

- Applied all implementation-affecting Round 2 findings.
- Internal draft label after patches: v0.3.

Re-review after Round 2 patches:

- Reviewer A/B same-checklist convergence on v0.3: completed with implementation-affecting findings.

Round 3 reviewed draft:

- Draft version: v0.3 after Round 2 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 3 implementation-affecting findings; Reviewer B found 4 implementation-affecting findings.

Round 3 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.4 |
| --- | --- | --- | --- |
| TIP06-R3-01 | High | Hash computation happened before prepared `evidencePackageId` and package `createdAt`, even though those fields are hash inputs. | Moved deterministic preparation of `evidencePackageId` and package `createdAt` before manifest/package hash computation. |
| TIP06-R3-02 | Medium | Pre-package audit ref wording implied visible audit append before hashing, conflicting with no-visible-partial failure policy. | Clarified pre-package audit refs are prepared before hashing and published only inside finalization. |
| TIP06-R3-03 | Medium | Missing `/complete` session id lacked API pipeline coverage. | Added API pipeline test for authorized complete on missing session returning `404 SESSION_NOT_FOUND` and no artifacts. |
| TIP06-R3-04 | High | Completed retry precedence was security-sensitive and contradictory. | Pinned auth/category/scope/ownership before completed snapshot return; expired completed sessions may return existing snapshot only after those checks pass. |
| TIP06-R3-05 | Medium | `InProgress` with all latest evidence `Passed` had no exact outcome. | Pinned `409 SESSION_STATE_CONFLICT` and test coverage. |
| TIP06-R3-06 | Medium | `NotAvailable` evidence input had no exact error code. | Pinned `409 UNSUPPORTED_EVIDENCE_RESULT` and test coverage. |
| TIP06-R3-07 | Medium | Failure after post-package audit append began was not defined. | Moved post-package audit publication into finalization; if it fails, no completion artifacts become visible. |

Patches after Round 3:

- Applied all implementation-affecting Round 3 findings.
- Internal draft label after patches: v0.4.

Re-review after Round 3 patches:

- Reviewer A/B same-checklist convergence on v0.4: completed with implementation-affecting findings.

Round 4 reviewed draft:

- Draft version: v0.4 after Round 3 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 3 implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.

Round 4 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.5 |
| --- | --- | --- | --- |
| TIP06-V04-A01 | High | Pre-package audit refs mixed event id and payload hash semantics while examples used string ids. | Defined canonical audit ref object with `eventId`, `eventType`, and `eventPayloadHash`; updated examples and hash rules. |
| TIP06-V04-A02 | Medium | `requestedBy` appeared in request sample without semantics. | Removed `requestedBy` from request DTO/sample and stated actor identity comes only from authenticated context. |
| TIP06-V04-A03 | Medium | `forceReview` precedence was undefined for completed/expired combined cases. | Added request-option validation before idempotent snapshot and expiry checks; added combined-case tests. |
| TIP06-R4-01 | High | Decision summary arrays lacked derivation and stable ordering rules. | Added normative derivation for `CompletedChecks`, `FailedChecks`, `DecisionReasonCodes`, and `RetryReasonCodes`. |
| TIP06-R4-02 | High | Audit-ref member set and ordering were still not fully deterministic. | Pinned exact S1 member sets and semantic ordering for pre/post package audit refs. |
| TIP06-R4-03 | Medium | Evidence-ref field names conflicted across projections. | Added projection field matrix and standardized on `resultType` and `evidenceResultId`. |

Patches after Round 4:

- Applied all implementation-affecting Round 4 findings.
- Internal draft label after patches: v0.5.

Re-review after Round 4 patches:

- Reviewer A/B same-checklist convergence on v0.5: completed with implementation-affecting findings.

Round 5 reviewed draft:

- Draft version: v0.5 after Round 4 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 4 implementation-affecting findings; Reviewer B found 5 implementation-affecting findings.

Round 5 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.6 |
| --- | --- | --- | --- |
| TIP06-R5-A01 / TIP06-R5-05 | High/Medium | Passed manifest example used empty `decisionReasonCodes` despite normative `FINAL_PASSED`. | Updated example and test expectation to `["FINAL_PASSED"]`. |
| TIP06-R5-A02 / TIP06-R5-01 | High | `postPackageAuditEventRefs` still mixed semantic ordering with audit id sorting and package-read events. | Defined filtered projection of only `EVIDENCE_PACKAGE_CREATED` and `SESSION_COMPLETED` in fixed semantic order; excluded `EVIDENCE_PACKAGE_READ`. |
| TIP06-R5-A03 / TIP06-R5-02 | Medium | Opening `/complete` rule list conflicted with forceReview precedence. | Marked the list as summary and aligned request-option validation before non-completed expiry. |
| TIP06-R5-A04 | Medium | Missing LocalDev fixture for package-read missing `business.session.read`. | Required same-client BusinessConsumer key without read scope. |
| TIP06-R5-03 | Medium | Scaffolding did not require canonical public `EvidenceRefSummaryDto` fields. | Required `EvidenceResultId`, `ResultType`, `RequiredCheckType`, optional `ArtifactHash`; disallowed legacy aliases for TIP-06. |
| TIP06-R5-04 | Medium | Complete success example omitted required TIP-07-compatible fields. | Added `externalSessionId`, `externalTransactionId`, and `bindingNonceHash` to success example. |

Patches after Round 5:

- Applied all implementation-affecting Round 5 findings.
- Internal draft label after patches: v0.6.

Re-review after Round 5 patches:

- Reviewer A/B same-checklist convergence on v0.6: completed with implementation-affecting findings.

Round 6 reviewed draft:

- Draft version: v0.6 after Round 5 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found no implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.

Round 6 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.7 |
| --- | --- | --- | --- |
| TIP06-R6-01 | Medium | Evidence ref alias policy left implementer choosing breaking change vs additive compatibility. | Chose additive canonical fields while retaining deprecated `Id`/`Type` aliases as BusinessConsumer-only mirror fields excluded from hashing. |
| TIP06-R6-02 | Medium | Package-read endpoint lacked exact failure precedence. | Added package-read precedence ladder and overlap tests. |
| TIP06-R6-03 | Medium | TIP-07 compatibility artifact was optional despite existing incomplete webhook DTO. | Required explicit compatibility DTO/artifact with full future webhook field set and tests. |

Patches after Round 6:

- Applied all implementation-affecting Round 6 findings.
- Internal draft label after patches: v0.7.

Re-review after Round 6 patches:

- Reviewer A/B same-checklist convergence on v0.7: completed with implementation-affecting finding.

Round 7 reviewed draft:

- Draft version: v0.7 after Round 6 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found no implementation-affecting findings; Reviewer B found 1 implementation-affecting finding.

Round 7 convergence finding:

| Finding ID | Severity | Issue | Patch applied in internal v0.8 |
| --- | --- | --- | --- |
| TIP06-R7-01 | High | Owner identity for TIP-07 compatibility conflicted with BusinessConsumer exposure boundary, and `client-safe app ref` was undefined. | Preserved `clientApplicationId` only in internal completion/package projection and TIP-07 compatibility artifact; explicitly excluded it from default BusinessConsumer DTOs. |

Patches after Round 7:

- Applied the implementation-affecting Round 7 finding.
- Internal draft label after patches: v0.8.

Re-review after Round 7 patch:

- Reviewer A/B same-checklist convergence on v0.8: completed with implementation-affecting findings.

Round 8 reviewed draft:

- Draft version: v0.8 after Round 7 patch.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 2 implementation-affecting findings; Reviewer B found 5 implementation-affecting findings.

Round 8 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.9 |
| --- | --- | --- | --- |
| TIP06-V08-A01 | Medium | STOP+ASK gates did not include owner identity/clientApplicationId exposure. | Added STOP+ASK gate for exposing owner identity or client-safe app refs in default BusinessConsumer responses. |
| TIP06-V08-A02 | Medium | API pipeline tests missed `SESSION_TERMINAL`, `RISK_EVALUATION_NOT_SUPPORTED`, and package creation failure. | Added HTTP/API pipeline bullets for those outcomes and no-visible-artifact assertions. |
| TIP06-V08-01 | High | Completion finalization conflict token was undefined. | Defined exact token: expected session state plus ordered latest required evidence id/append ordinal map. |
| TIP06-V08-02 | High | Audit payload hash schemas were undefined. | Added fixed canonical payload schemas and golden hash fixture requirements for successful audit events. |
| TIP06-V08-03 | High | Validation overlap precedence was incomplete. | Added validation order before final-result precedence and overlap examples/tests. |
| TIP06-V08-04 | Medium | TIP-07 compatibility artifact was still either/or. | Chose additive update of existing `BusinessConsumer.VerificationCompletedEventDto`. |
| TIP06-V08-05 | Medium | API pipeline plan missed full field assertions and `missingChecks` details. | Added full field assertions for complete/package/session reads and `error.details.missingChecks`. |

Patches after Round 8:

- Applied all implementation-affecting Round 8 findings.
- Internal draft label after patches: v0.9.

Re-review after Round 8 patches:

- Reviewer A/B same-checklist convergence on v0.9: completed with implementation-affecting findings.

Round 9 reviewed draft:

- Draft version: v0.9 after Round 8 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found no implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.

Round 9 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.10 |
| --- | --- | --- | --- |
| TIP06-V09-01 | High | Existing `VerificationCompletedEventDto` delivery fields lacked TIP-06 placeholder semantics. | Pinned `EventType`, LocalDev placeholder `DeliveryId`, `SentAt = completedAt`, and separate webhook/evidence signature statuses. |
| TIP06-V09-02 | Medium | Effective request/correlation ids were required on decision record though current `VerificationDecision` lacks fields. | Narrowed persistence to completion snapshot, package/manifest metadata, and audit events; no `VerificationDecision` extension required. |
| TIP06-V09-03 | Medium | Existing `EvidencePackage.AuditEventRefs` compatibility fate was undefined. | Marked it legacy/untrusted if retained; structured audit refs are source of truth. |

Patches after Round 9:

- Applied all implementation-affecting Round 9 findings.
- Internal draft label after patches: v0.10.

Re-review after Round 9 patches:

- Reviewer A/B same-checklist convergence on v0.10: completed with implementation-affecting findings.

Round 10 reviewed draft:

- Draft version: v0.10 after Round 9 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 2 implementation-affecting findings.

Round 10 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.11 |
| --- | --- | --- | --- |
| TIP06-V10-A01 | High | Audit event ids in canonical audit refs could be random/repository-assigned, making manifest hashes unstable. | Required deterministic prepared audit event ids before hashing and finalization must use those exact ids. |
| TIP06-V10-CR-01 | Medium | `DeliveryId` still allowed an equivalent placeholder constant. | Pinned exact `DeliveryId = "localdev-not-dispatched"`. |
| TIP06-V10-CR-02 | Medium | Extra `requestedBy` could be ignored or rejected, leaving API behavior non-deterministic. | Pinned current default binding behavior: ignored and non-authoritative. |

Patches after Round 10:

- Applied all implementation-affecting Round 10 findings.
- Internal draft label after patches: v0.11.

Re-review after Round 10 patches:

- Reviewer A/B same-checklist convergence on v0.11: completed with implementation-affecting findings.

Round 11 reviewed draft:

- Draft version: v0.11 after Round 10 patches.
- Reviewer A checklist: full convergence checklist.
- Reviewer B checklist: same full convergence checklist.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 3 implementation-affecting findings.

Round 11 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.12 |
| --- | --- | --- | --- |
| TIP06-V11-A01 | Medium | `completedAt` was used in audit/webhook compatibility but not deterministically prepared before hashing. | Prepared deterministic `completedAt` with other ids/timestamps and made it source of truth for snapshot/responses/audit/TIP-07. |
| TIP06-R11-C01 | High | Completed session reads could use original session request/correlation ids instead of effective completion overrides. | Required completed session summary to source requestId/correlationId from completion snapshot effective values. |
| TIP06-R11-C02 | Medium | Completed session API pipeline assertions did not cover full promised TIP-06/TIP-07 field set. | Expanded completed session read API assertions to the full field set. |
| TIP06-R11-C03 | Medium | Tail sections still pending/final recommendation unresolved. | Deferred final status until convergence and later passes complete; added exhaustive-review protocol now. |

Patches after Round 11:

- Applied all implementation-affecting Round 11 findings.
- Internal draft label after patches: v0.12.
- Added exhaustive-review requirement to prevent first-finding-only reviewer behavior.

Re-review after Round 11 patches:

- Reviewer A/B same-checklist convergence on v0.12: completed with implementation-affecting findings.

Round 12 reviewed draft:

- Draft version: v0.12 after Round 11 patches.
- Reviewer A checklist: full exhaustive convergence checklist.
- Reviewer B checklist: same full exhaustive convergence checklist.
- Finding count: Reviewer A found 3 implementation-affecting findings; Reviewer B found 5 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 12 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.13 |
| --- | --- | --- | --- |
| TIP06-V12-A01 / TIP06-R12-01 | High | Deterministic prepared ids/timestamps still allowed a non-deterministic retry branch. | Required one deterministic completion value preparer and removed the non-deterministic branch. |
| TIP06-V12-A02 | Medium | API pipeline full field assertions were incomplete. | Expanded endpoint field assertions and absence assertions. |
| TIP06-V12-A03 | Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |
| TIP06-R12-02 | Medium | TIP-07 owner identity wording could imply public owner exposure. | Rewrote additive contract wording to expose only client-safe fields except internal `clientApplicationId`. |
| TIP06-R12-03 | Medium | Non-success/read audit behavior remained discretionary. | Deferred non-success/read audit side effects out of TIP-06 except three successful completion events. |
| TIP06-R12-04 | Medium | `/complete` unresolved policy/context branch lacked exact rule/test. | Added `403 SESSION_ACCESS_DENIED` rule and API test. |
| TIP06-R12-05 | Medium | Idempotent retry with different second request/correlation ids lacked test coverage. | Added API test requiring original snapshot values and unchanged metadata. |

Patches after Round 12:

- Applied all implementation-affecting Round 12 findings.
- Internal draft label after patches: v0.13.

Re-review after Round 12 patches:

- Reviewer A/B same-checklist convergence on v0.13: completed with implementation-affecting findings.

Round 13 reviewed draft:

- Draft version: v0.13 after Round 12 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 3 implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 13 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.14 |
| --- | --- | --- | --- |
| TIP06-R13-A01 / TIP06-R13-B-01 | Medium | Stale optional denial/failure audit language conflicted with the rule that TIP-06 emits no non-success/read audit events. | Removed the optional audit language and pinned exactly three successful completion audit events only. |
| TIP06-R13-A02 | High | Lifecycle state rules and RequiredChecks/evidence validation precedence still overlapped for `Created + RiskEvaluation` and `ReadyToComplete + unsupported/non-passed latest evidence`. | Added one state/evidence validation ladder and required overlap tests. |
| TIP06-R13-A03 | High | `decision.CreatedAt` and package/manifest `createdAt` could be swapped or excluded inconsistently from manifest/hash inputs. | Added `decisionCreatedAt` to the canonical manifest payload and defined canonical `createdAt` as package/manifest creation time. |
| TIP06-R13-B-02 | Medium | `SESSION_ALREADY_COMPLETED` remained in the expected error-code list as an implementation choice. | Removed it from allowed TIP-06 runtime codes and marked it legacy/out-of-scope. |
| TIP06-R13-B-03 | Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |

Patches after Round 13:

- Applied all implementation-affecting Round 13 findings except final handoff status, which cannot be marked clean until same-version convergence and later passes complete.
- Internal draft label after patches: v0.14.

Re-review after Round 13 patches:

- Reviewer A/B same-checklist convergence on v0.14: completed with implementation-affecting findings.

Round 14 reviewed draft:

- Draft version: v0.14 after Round 13 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 4 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 14 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.15 |
| --- | --- | --- | --- |
| TIP06-R14-A01 / RB14-04 | Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |
| RB14-01 | Medium | Post-package audit preparation/publication failure lacked exact HTTP status/error code. | Pinned `500 COMPLETION_AUDIT_PUBLICATION_FAILED` with no visible completion artifacts and added API assertion. |
| RB14-02 | Medium | Terminal-state overlap precedence with RequiredChecks/evidence validation was not pinned. | Added terminal-state rejection before evidence loading and terminal-overlap API tests. |
| RB14-03 | Medium | Completed session summary/test field set omitted `externalSessionId`. | Required `ExternalSessionId` on completed `VerificationSessionSummaryDto` and API assertions. |

Patches after Round 14:

- Applied all implementation-affecting Round 14 findings except final handoff status, which cannot be marked clean until same-version convergence and later passes complete.
- Internal draft label after patches: v0.15.

Re-review after Round 14 patches:

- Reviewer A/B same-checklist convergence on v0.15: completed with implementation-affecting findings.

Round 15 reviewed draft:

- Draft version: v0.15 after Round 14 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 4 implementation-affecting findings; Reviewer B found 2 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 15 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.16 |
| --- | --- | --- | --- |
| R15-A-01 | High | Timestamp canonicalization allowed framework-dependent round-trip strings, fractional seconds, or `+00:00`. | Pinned exact whole-second UTC `yyyy-MM-dd'T'HH:mm:ss'Z'` timestamp format for manifest/package/audit hash inputs. |
| R15-A-02 | Medium | `VerificationSessionState.Expired` precedence was ambiguous versus elapsed `ExpiresAt` expiry and terminal state. | Pinned `VerificationSessionState.Expired` to `403 SESSION_EXPIRED` before terminal/evidence validation and added overlap tests. |
| R15-A-03 | Medium | Evidence reason codes could be copied into decision/manifest data without safety validation. | Added uppercase token safety rule, invalid-code rejection, and tests for sorted/de-duplicated sanitized codes. |
| R15-A-04 / RB15-02 | Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |
| RB15-01 | Medium | Completed-session summary field list still omitted `externalSessionId`. | Added `externalSessionId` and aligned the completed-session summary list with API assertions. |

Patches after Round 15:

- Applied all implementation-affecting Round 15 findings except final handoff status, which cannot be marked clean until same-version convergence and later passes complete.
- Internal draft label after patches: v0.16.

Re-review after Round 15 patches:

- Reviewer A/B same-checklist convergence on v0.16: completed with implementation-affecting findings.

Round 16 reviewed draft:

- Draft version: v0.16 after Round 15 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 5 implementation-affecting findings; Reviewer B found 1 implementation-affecting finding.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 16 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.17 |
| --- | --- | --- | --- |
| R16-A-01 | High | Deterministic completion value preparer inputs omitted `verificationSessionId` and authenticated `clientApplicationId`. | Added both inputs and cross-session/cross-client distinctness tests. |
| R16-A-02 | Medium | `confidence` was listed as a final calculation input without exact semantics or hash participation. | Pinned confidence as ignored for TIP-06 decision/assurance/provenance/hash/projection behavior. |
| R16-A-03 | Medium | API pipeline tests did not cover invalid reason-code rejection. | Added invalid `ReasonCodes` and `RetryReasonCode` API tests returning `UNSUPPORTED_EVIDENCE_RESULT`. |
| R16-A-04 | Medium | `/complete` wrong caller-category precedence lacked overlap tests and exact code. | Added `CALLER_CATEGORY_NOT_ALLOWED` precedence tests before scope/session lookup/ownership. |
| R16-A-05 | Medium | `ClientApplicationId` boundary between TIP-07 artifact and default BusinessConsumer DTOs needed explicit tests. | Added contract/boundary test rule allowing `ClientApplicationId` only on `VerificationCompletedEventDto`. |
| RB16-01 | Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |

Patches after Round 16:

- Applied all implementation-affecting Round 16 findings except final handoff status, which cannot be marked clean until same-version convergence and later passes complete.
- Internal draft label after patches: v0.17.

Re-review after Round 16 patches:

- Reviewer A/B same-checklist convergence on v0.17: completed with implementation-affecting findings.

Round 17 reviewed draft:

- Draft version: v0.17 after Round 16 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 5 implementation-affecting findings; Reviewer B found 2 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 17 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.18 |
| --- | --- | --- | --- |
| R17-A-01 / RB17-01 | High/Medium | Tail handoff sections still pending. | Deferred final status update until same-version convergence and later passes complete. |
| R17-A-02 | Medium | Completed-session summary field list needed an explicit authoritative field-set marker. | Marked section 5 completed-session summary list as authoritative. |
| R17-A-03 | Medium | `COMPLETION_AUDIT_PUBLICATION_FAILED` did not explicitly cover `FINAL_DECISION_CALCULATED` audit preparation/publication/append failure. | Applied the same error/rollback rule to all three successful completion audit events. |
| R17-A-04 | Medium | Invalid reason-code validation scope was ambiguous for latest `Passed` evidence. | Required safety validation on every latest required EvidenceResult before final-result calculation. |
| R17-A-05 | Medium | Expiry could be re-read during finalization, creating race-dependent behavior. | Added single captured `operationNow` semantics and tests. |
| RB17-02 | Medium | Package-read malformed-id API coverage was missing. | Added `GET /evidence-packages/{malformedId}` API test expecting `404 EVIDENCE_PACKAGE_NOT_FOUND`, plus explicit unauthenticated coverage for both TIP-06 endpoints. |

Patches after Round 17:

- Applied all implementation-affecting Round 17 findings except final handoff status, which cannot be marked clean until same-version convergence and later passes complete.
- Internal draft label after patches: v0.18.

Re-review after Round 17 patches:

- Reviewer A/B same-checklist convergence on v0.18: completed with implementation-affecting findings.

Round 18 reviewed draft:

- Draft version: v0.18 after Round 17 patches.
- Reviewer A checklist: full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Reviewer B checklist: same full exhaustive convergence checklist, with required coverage attestation for every checklist group.
- Finding count: Reviewer A found 2 implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 18 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.19 |
| --- | --- | --- | --- |
| R18-A-01 / RB18-01 | High | `operationNow` was both a fresh per-attempt captured instant and required to remain identical across external retries after no-visible package failure. | Pinned external retry after no-visible package-construction failure as a new attempt with a fresh `operationNow`; only same-input recomputation inside one attempt must be stable. |
| RB18-02 | Medium | Reason-code safety was not placed explicitly in both validation ladders, leaving overlap precedence open. | Added reason-code safety after missing-evidence validation and before `RetryRequired`, with overlap examples/tests. |
| RB18-03 | Medium | Missing-session behavior was not explicit in the exact `/complete` sequence. | Added syntactically valid unknown session id -> `404 SESSION_NOT_FOUND` before ownership/policy checks. |
| R18-A-02 / RB18-01 | Medium | Tail handoff sections still pending. | This tail is now updated to `NEEDS PATCHES` with v0.19 pending same-version convergence review. |

Patches after Round 18:

- Applied all implementation-affecting Round 18 findings.
- Internal draft label after patches: v0.19.

Re-review after Round 18 patches:

- Reviewer A/B same-checklist convergence on v0.19: completed with implementation-affecting findings.

Round 19 reviewed draft:

- Draft version: v0.19 after Round 18 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 6 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 19 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.20 |
| --- | --- | --- | --- |
| R19-A-01 / TIP06-R19-B-01 | Medium | `payloadHash` exposure was contradictory between default BusinessConsumer prohibition and allowed exposure wording. | Removed public exposure wording, added explicit prohibition and STOP+ASK gate. |
| TIP06-R19-B-02 | Medium | Reason-code overlap examples were not carried through to API pipeline tests. | Added missing+invalid and retry+invalid overlap tests. |
| TIP06-R19-B-03 | Medium | Fresh `operationNow` retry semantics lacked a clock-advance test proving retry timestamps/hash inputs use the second attempt. | Added fault-injection test requirement with advanced injected clock. |
| TIP06-R19-B-04 | Medium | Manifest/canonicalization/hash failure lacked exact error code and test. | Pinned `EVIDENCE_PACKAGE_CREATION_FAILED` and no visible artifacts for manifest/package construction and hash failures. |
| TIP06-R19-B-05 | Medium | API JSON leakage absence tests were incomplete for completed session and package reads. | Added public JSON absence assertions across complete, completed session GET, and package GET. |
| TIP06-R19-B-06 | Medium | Evidence-record request/correlation ids could be confused with effective completion request/correlation ids in hash/provenance outputs. | Excluded evidence-record request/correlation ids from TIP-06 decision/provenance/hash outputs. |

Patches after Round 19:

- Applied all implementation-affecting Round 19 findings.
- Internal draft label after patches: v0.20.

Re-review after Round 19 patches:

- Reviewer A/B same-checklist convergence on v0.20: completed with implementation-affecting findings.

Round 20 reviewed draft:

- Draft version: v0.20 after Round 19 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 3 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 20 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.21 |
| --- | --- | --- | --- |
| TIP06-R20-A01 / TIP06-R20-B-01 | Medium | Evidence-record request/correlation id exclusion lacked complete test carry-through and package-input wording was too generic. | Qualified package inputs as effective completion ids only and added unit/golden/contract/API exclusion tests. |
| TIP06-R20-B-02 | Medium | Public leakage tests asserted only `payloadHash`, not PascalCase `PayloadHash`. | Added `PayloadHash` DTO reflection and API JSON absence assertions. |
| TIP06-R20-B-03 | Medium | Broad audit-ref package input wording allowed unrelated session/evidence/read audit refs. | Replaced broad wording with exact TIP-06 audit ref set and added unrelated-audit exclusion tests. |

Patches after Round 20:

- Applied all implementation-affecting Round 20 findings.
- Internal draft label after patches: v0.21.

Re-review after Round 20 patches:

- Reviewer A/B same-checklist convergence on v0.21: completed with implementation-affecting findings.

Round 21 reviewed draft:

- Draft version: v0.21 after Round 20 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 2 implementation-affecting findings; Reviewer B found 2 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 21 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.22 |
| --- | --- | --- | --- |
| TIP06-R21-A01 | Medium | `postPackageAuditEventRefs` could be implemented as a global event-type filter and include another session/package's completion audit refs. | Scoped projection to this completion's prepared post-package event ids or matching session/package ids and added unrelated same-type audit tests. |
| TIP06-R21-A02 / TIP06-R21-B-01 | High/Medium | Patch Impact Matrix wording said evidence ids were excluded, conflicting with required `evidenceResultId` hash participation. | Reworded matrix intent to source evidence-record request/correlation ids only and added hash-change coverage for `evidenceResultId`. |
| TIP06-R21-B-02 | Medium | Payload hash prohibition and STOP+ASK gate named only JSON `payloadHash`, not DTO/property `PayloadHash`. | Prohibited both DTO/property `PayloadHash` and JSON `payloadHash`. |

Patches after Round 21:

- Applied all implementation-affecting Round 21 findings.
- Internal draft label after patches: v0.22.

Re-review after Round 21 patches:

- Reviewer A/B same-checklist convergence on v0.22: completed with implementation-affecting findings.

Round 22 reviewed draft:

- Draft version: v0.22 after Round 21 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 2 implementation-affecting findings; Reviewer B found 0 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 22 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.23 |
| --- | --- | --- | --- |
| TIP06-R22-A01 | Medium | Completed-session GET assertions could miss stale `result`/`assuranceLevel`. | Added `result` and `assuranceLevel` to completed-session summary assertions and tests for `Passed -> Medium` plus non-passed -> `None`. |
| TIP06-R22-A02 | Medium | `forceReview=true` API tests did not prove earlier auth/category/scope/session/ownership/policy gates win. | Added forceReview overlap tests for all earlier gates. |

Patches after Round 22:

- Applied all implementation-affecting Round 22 findings.
- Internal draft label after patches: v0.23.

Re-review after Round 22 patches:

- Reviewer A/B same-checklist convergence on v0.23: completed with implementation-affecting findings.

Round 23 reviewed draft:

- Draft version: v0.23 after Round 22 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 1 implementation-affecting finding; Reviewer B found 1 implementation-affecting finding.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 23 convergence findings:

| Finding ID | Severity | Issue | Patch applied in internal v0.24 |
| --- | --- | --- | --- |
| TIP06-R23-A01 / TIP06-R23-B-01 | Medium | Completed-session `result`/`assuranceLevel` carry-through was not value-tight in scaffolding/API tests. | Added `Result` and `AssuranceLevel` to `VerificationSessionSummaryDto` scaffolding and required exact completed-session GET `result`/`assuranceLevel` assertions for passed and non-passed outcomes. |

Patches after Round 23:

- Applied all implementation-affecting Round 23 findings.
- Internal draft label after patches: v0.24.

Re-review after Round 23 patches:

- Reviewer A/B same-checklist convergence on v0.24: completed with implementation-affecting findings.

Round 24 reviewed draft:

- Draft version: v0.24 after Round 23 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 2 implementation-affecting findings and 2 non-implementation-affecting findings; Reviewer B found 1 implementation-affecting finding and 2 non-implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 24 convergence findings:

| Finding ID | Severity | Implementation-affecting? | Issue | Patch applied in internal v0.25 |
| --- | --- | --- | --- | --- |
| TIP06-R24-A01 | Medium | Yes | `VerificationSessionSummaryDto` scaffolding remained partial and omitted package ids/hashes, request/correlation ids, and completedAt. | Expanded the scaffolding list to the full authoritative completed-session field set. |
| TIP06-R24-A02 | Medium | Yes | BusinessConsumer evidence-ref `result` was optional in the projection matrix while tests implied evidence refs expose only id/type/check/artifact hash by default. | Chose a single contract: default BusinessConsumer evidence refs do not expose per-evidence `result`; TIP-07 derives future per-check result blocks from internal evidence/manifest records. |
| TIP06-R24-B-01 | Medium | Yes | Completed-session GET value assertions covered only one generic non-passed result and could miss stale mapping for other non-passed outcomes. | Required exact completed-session GET `result`/`assuranceLevel` assertions for `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, and `TechnicalError`, plus `Passed`. |
| TIP06-R24-A03 / TIP06-R24-B-02 | Low | No | Patch Impact Matrix mixed boundary-test impact with reviewed/carry-through status. | Split actual boundary impact from carry-through status in v0.24/v0.25 rows. |
| TIP06-R24-A04 / TIP06-R24-B-03 | Low | No | Final self-check still referenced stale v0.19 pending re-review wording. | Replaced stale wording with the current v0.25 pending same-version review state. |

Patches after Round 24:

- Applied all implementation-affecting Round 24 findings.
- Applied non-implementation-affecting bookkeeping fixes to prevent stale convergence reporting.
- Internal draft label after patches: v0.25.

Re-review after Round 24 patches:

- Reviewer A/B same-checklist convergence on v0.25: completed with implementation-affecting findings.

Round 25 reviewed draft:

- Draft version: v0.25 after Round 24 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 0 implementation-affecting findings; Reviewer B found 1 implementation-affecting finding and 1 non-implementation-affecting finding.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 25 convergence findings:

| Finding ID | Severity | Implementation-affecting? | Issue | Patch applied in internal v0.26 |
| --- | --- | --- | --- | --- |
| TIP06-R25-B-01 | Medium | Yes | Completion snapshot/projection explicit field lists omitted effective `RequestId` and `CorrelationId`, even though later rules required completed reads and idempotent retry to source those values from the snapshot. | Added effective `requestId`/`correlationId` to the completion snapshot list and effective `RequestId`/`CorrelationId` to the completed projection operation/invalid-projection rule. |
| TIP06-R25-B-02 | Low | No | Patch Impact Matrix v0.19-v0.23 boundary-test impact cells still carried reviewed status instead of actual boundary impact. | Replaced those cells with actual boundary impact or explicit no-direct-boundary-change statements. |

SignFlow-style carry-through scan before v0.26 re-review:

| Required patch surface | v0.26 status |
| --- | --- |
| Normative rule section | Updated Section 7 completion persistence boundary and Section 18 projection invariant for effective request/correlation ids. |
| Runtime/API shape section | Updated completion snapshot/projection persistence; API behavior in Section 21 already asserts completed GET effective ids and idempotent retry values. |
| DTO field list | Unaffected by this patch; v0.25 already lists `RequestId` and `CorrelationId` in completed/session/package DTO field lists. |
| Examples | Unaffected; examples already include `requestId` and `correlationId` at completion/package/manifest levels. |
| Error/status section | Unaffected; no error precedence or HTTP status change. |
| Test plan | Existing Section 21 tests cover completed GET effective ids and idempotent retry with second request/correlation ignored; v0.26 aligns persistence lists to those tests. |
| STOP+ASK gates | Unaffected; no new public exposure or deferred-scope expansion. |
| Patch Impact Matrix | Updated v0.19-v0.23 boundary cells and added v0.26 row. |
| Review Loop Trace | Added Round 25 result and v0.26 pending re-review. |
| Builder Reviewer Result | Updated current builder status to v0.26 pending re-review. |
| Final Neutral Self-Check | Updated pending version and contradiction statement to v0.26. |
| Final Recommendation | Updated current/latest draft and required re-review to v0.26. |

Patches after Round 25:

- Applied all implementation-affecting Round 25 findings.
- Applied non-implementation-affecting Patch Impact Matrix cleanup.
- Internal draft label after patches: v0.26.

Re-review after Round 25 patches:

- Reviewer A/B same-checklist convergence on v0.26: completed with implementation-affecting findings.

Round 26 reviewed draft:

- Draft version: v0.26 after Round 25 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 0 implementation-affecting findings; Reviewer B found 1 implementation-affecting finding.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 26 convergence findings:

| Finding ID | Severity | Implementation-affecting? | Issue | Patch applied in internal v0.27 |
| --- | --- | --- | --- | --- |
| TIP06-R26-B-01 | Medium | Yes | Section 19 `INCOMPLETE_REQUIRED_CHECKS` example omitted `error.details.missingChecks`, contradicting Sections 8 and 21. | Updated Section 19 error envelope example and rule to require sanitized sorted `details.missingChecks` for `INCOMPLETE_REQUIRED_CHECKS`; other errors may omit details unless explicitly defined. |

SignFlow-style carry-through scan before v0.27 re-review:

| Required patch surface | v0.27 status |
| --- | --- |
| Normative rule section | Section 8 already defines sanitized sorted `missingChecks`; no normative rule change needed. |
| Runtime/API shape section | Updated Section 19 error-envelope example and rule for `INCOMPLETE_REQUIRED_CHECKS`. |
| DTO field list | Unaffected; no DTO field shape change. |
| Examples | Updated Section 19 JSON example to include `details.missingChecks`. |
| Error/status section | Updated Section 19 directly. |
| Test plan | Section 21 already asserts `error.details.missingChecks`; v0.27 aligns Section 19 to that test expectation. |
| STOP+ASK gates | Unaffected; no new public exposure or deferred-scope expansion. |
| Patch Impact Matrix | Added v0.27 row. |
| Review Loop Trace | Added Round 26 result and v0.27 pending re-review. |
| Builder Reviewer Result | Updated current builder status to v0.27 pending re-review. |
| Final Neutral Self-Check | Updated pending version and contradiction statement to v0.27. |
| Final Recommendation | Updated current/latest draft and required re-review to v0.27. |

Patches after Round 26:

- Applied all implementation-affecting Round 26 findings.
- Internal draft label after patches: v0.27.

Re-review after Round 26 patches:

- Reviewer A/B same-checklist convergence on v0.27: completed with implementation-affecting findings.

Round 27 reviewed draft:

- Draft version: v0.27 after Round 26 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 0 implementation-affecting findings; Reviewer B found 3 implementation-affecting findings.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 27 convergence findings:

| Finding ID | Severity | Implementation-affecting? | Issue | Patch applied in internal v0.28 |
| --- | --- | --- | --- | --- |
| TIP06-R27-B-01 | Medium | Yes | Test plan did not cover `RiskEvaluation` precedence over unsupported evidence, or unsupported evidence precedence over missing evidence. | Added API overlap tests for `RiskEvaluation + NotSupported/NotAvailable -> RISK_EVALUATION_NOT_SUPPORTED` and `NotSupported/NotAvailable + missing -> UNSUPPORTED_EVIDENCE_RESULT`, with no artifacts. |
| TIP06-R27-B-02 | Medium | Yes | Test plan omitted `forceReview=true` overlap for `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal`. | Added API test requiring authorized `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal` `forceReview=true` to return `400 FORCE_REVIEW_NOT_SUPPORTED`, not `SESSION_TERMINAL`, with no artifacts. |
| TIP06-R27-B-03 | Medium | Yes | Test plan did not assert successful package reads append no read audit event despite Section 16 deferring read audit side effects. | Added API/integration test requiring successful package read to leave audit store unchanged and projected post-package refs unchanged. |

SignFlow-style carry-through scan before v0.28 re-review:

| Required patch surface | v0.28 status |
| --- | --- |
| Normative rule section | Unaffected; Sections 7, 8, and 16 already define the validated precedence and no-read-audit rules. |
| Runtime/API shape section | Unaffected; no endpoint shape or response field change. |
| DTO field list | Unaffected; no DTO field shape change. |
| Examples | Unaffected; no JSON/example shape change. |
| Error/status section | Unaffected; no status/error code change. |
| Test plan | Updated Section 21 with three missing API/integration test expectations. |
| STOP+ASK gates | Unaffected; no new public exposure or deferred-scope expansion. |
| Patch Impact Matrix | Added v0.28 row. |
| Review Loop Trace | Added Round 27 result and v0.28 pending re-review. |
| Builder Reviewer Result | Updated current builder status to v0.28 pending re-review. |
| Final Neutral Self-Check | Updated pending version and contradiction statement to v0.28. |
| Final Recommendation | Updated current/latest draft and required re-review to v0.28. |

Patches after Round 27:

- Applied all implementation-affecting Round 27 findings.
- Internal draft label after patches: v0.28.

Re-review after Round 27 patches:

- Reviewer A/B same-checklist convergence on v0.28: completed with implementation-affecting findings.

Round 28 reviewed draft:

- Draft version: v0.28 after Round 27 patches.
- Reviewer A checklist: optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Reviewer B checklist: same optimized high-rigor full exhaustive checklist with SignFlow carry-through scan, Patch Impact Matrix audit, Review Loop Trace audit, builder ambiguity audit, and hidden-risk audit.
- Finding count: Reviewer A found 4 implementation-affecting findings; Reviewer B found 2 implementation-affecting findings and 1 non-implementation-affecting finding.
- Coverage attestation: both reviewers reported full checklist coverage.

Round 28 convergence findings patched in batch v0.29:

| Finding ID | Severity | Implementation-affecting? | Classification | Issue | Patch applied in internal v0.29 |
| --- | --- | --- | --- | --- | --- |
| TIP06-R28-A01 | Medium | Yes | pre-existing latent gap / builder runtime risk | `Created` state wording could override unsupported/invalid/retry precedence before lifecycle drift. | Clarified `Created` state eligibility applies only after ladder steps 1-6 and added Created overlap tests for unsupported, retry, and invalid reason-code cases. |
| TIP06-R28-A02 | Medium | Yes | pre-existing latent gap / builder runtime risk | `RetryReasonCode` blank/null behavior was ambiguous. | Defined absent/null as ignored and present empty/whitespace as invalid; added API test expectation. |
| TIP06-R28-A03 / RB28-B-02 | Medium | Yes | pre-existing latent gap / audit invariant risk | No-audit tests did not cover representative package-read denial and `/complete` non-success paths. | Added package-read denial and `/complete` non-success no-audit assertions. |
| TIP06-R28-A04 | Medium | Yes | patch-regression / public API boundary risk | Section 3 still implied public per-evidence statuses/timestamps despite later public evidence-ref exclusions. | Narrowed Section 3 to summary-level final result/status/timestamp fields and explicitly excluded per-evidence result/status/timestamps from default public evidence refs. |
| RB28-B-01 | Medium | Yes | pre-existing latent gap / builder runtime risk | The terminal state was not enumerated as exact `VerificationSessionState.TechnicalTerminal`. | Enumerated terminal states as `VerificationSessionState.Cancelled` and `VerificationSessionState.TechnicalTerminal`; updated terminal and forceReview tests. |
| RB28-B-03 | Low | No | bookkeeping only | Final self-check answers used confusing `YES, prevented` wording and pending-review contradiction wording. | Reworded self-check answers to `NO, prevented` and clarified pending-review status. |

Affected-surface map for v0.29:

| Patch area | Normative sections touched | Runtime/API shape touched | DTO fields touched | Examples touched | Error/status touched | Test plan touched | STOP+ASK gates touched | Patch Impact Matrix touched | Review Loop Trace touched | Builder Result touched | Final Self-Check touched | Final Recommendation touched |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Created precedence | Sections 7 and 8 state/evidence ladder and state eligibility | No endpoint shape change; runtime precedence clarified | Unaffected | Unaffected | Existing status precedence clarified, no new code | Section 21 Created overlap tests added | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | v0.29 pending review | v0.29 pending review |
| RetryReasonCode null/blank | Section 9 derivation and reason-code safety | Runtime validation clarified | Unaffected | Unaffected | Present blank/whitespace maps to existing `UNSUPPORTED_EVIDENCE_RESULT`; null/absent ignored | Section 21 blank/whitespace/null test expectation added | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | v0.29 pending review | v0.29 pending review |
| No-audit denial coverage | Section 16 rule already normative; Section 21 test coverage expanded | No endpoint shape change; no-audit invariant coverage expanded | Unaffected | Unaffected | No status change | Section 21 package-read denial and `/complete` non-success no-audit tests added | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | v0.29 pending review | v0.29 pending review |
| Section 3 public summary boundary | Section 3 scope narrowed | Public package summary boundary clarified | No DTO field additions/removals beyond existing contract | Unaffected | Unaffected | Existing boundary tests remain applicable | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | v0.29 pending review | v0.29 pending review |
| Terminal enum exactness | Sections 7 lifecycle/terminal rules | Runtime states enumerated exactly | Unaffected | Unaffected | `SESSION_TERMINAL` applies to exact states | Section 21 terminal/forceReview tests updated to exact enum values | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | v0.29 pending review | v0.29 pending review |
| Final self-check wording | Unaffected | Unaffected | Unaffected | Unaffected | Unaffected | Unaffected | Unaffected | v0.29 row added | Round 28/v0.29 row added | v0.29 pending re-review | Corrected answers | v0.29 pending review |

Patches after Round 28:

- Applied all implementation-affecting Round 28 findings in one batch.
- Applied non-implementation-affecting final self-check wording cleanup.
- Internal draft label after patches: v0.29.

Re-review after Round 28 patches:

- Lane A Patch Carry-through Reviewer on v0.29: completed with no patch-regression or blocker bookkeeping findings.
- Lane B Latent Spec Reviewer on v0.29: completed with no blocker findings; two deferred non-blocking notes were recorded.
- External GPT review on v0.29: completed with two gate-blocking governance/proof wording findings and one optional implementation preflight.

External GPT review findings patched in v0.30:

| Finding ID | Severity | Classification | Issue | Patch applied in internal v0.30 |
| --- | --- | --- | --- | --- |
| GPT-01 | Medium | gate-blocking governance | Tail governance still required old same-exhaustive A/B convergence while v0.29 switched to scoped Lane A/Lane B review. | Superseded old A/B convergence rule for post-v0.29 drafts and made Lane A/Lane B plus final architect gate the post-v0.29 acceptance source of truth. |
| GPT-02 | Medium | gate-blocking proof wording | No-audit tests named forbidden event types but did not require audit store count unchanged/no event of any type for denial/non-success paths. | Strengthened package-read and `/complete` non-success tests to assert audit store count unchanged and no audit event of any type appended or visible, with explicit rollback expectation for successful-completion audit failure injection. |
| GPT-03 | Low/Medium | implementation preflight | Builders should verify `VerificationSessionState.TechnicalTerminal` exists in the repo before implementation. | Added STOP+ASK preflight for adding/removing/renaming lifecycle states and verifying `TechnicalTerminal`. Repo evidence exists at `src/TagEkyc.Domain/VerificationSessionState.cs`. |

Patches after external GPT review:

- Applied targeted v0.30 governance/proof/preflight patches only.
- Internal draft label after patches: v0.30.

Re-review after v0.30 patches:

- Lane A Patch Carry-through Reviewer on v0.30: completed with one gate-blocking bookkeeping finding.
- Finding `LA-v0.30-01`: top-level status still said `Patched draft for same-checklist convergence re-review`, pointing to the superseded pre/post-v0.29 governance model.
- Patch applied in internal v0.31: top-level status metadata now says `Patched draft pending v0.31 Lane A carry-through review`.
- Lane B rerun is not required unless Lane A or final architect review finds a patch-regression that changes runtime/API behavior, DTO shape, error precedence, audit/hash inputs, lifecycle state, evidence boundary, or test expectations in a way that can create a blocker-category latent gap.
- Historical convergence status before final external architect gate: v0.31 still required Lane A carry-through. That stale pre-implementation gate is superseded by the final external architect gate and implementation commit `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`.

### Consolidated Review Loop Trace

| Round | Draft version | Reviewer | Checklist | Findings | Patch applied | Re-review done | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | v0.1 | Reviewer A | Initial full review | Findings | v0.2 | Yes, Round 2 | Patched |
| 1 | v0.1 | Reviewer B | Initial full review | Findings | v0.2 | Yes, Round 2 | Patched |
| 1 | v0.1 | Builder reviewer | Implementer ambiguity pass | Findings | v0.2 | Covered by later A/B loops | Patched |
| 2 | v0.2 | Reviewer A/B | Full convergence checklist | Findings | v0.3 | Yes, Round 3 | Patched |
| 3 | v0.3 | Reviewer A/B | Same full convergence checklist | Findings | v0.4 | Yes, Round 4 | Patched |
| 4 | v0.4 | Reviewer A/B | Same full convergence checklist | Findings | v0.5 | Yes, Round 5 | Patched |
| 5 | v0.5 | Reviewer A/B | Same full convergence checklist | Findings | v0.6 | Yes, Round 6 | Patched |
| 6 | v0.6 | Reviewer A/B | Same full convergence checklist | Findings | v0.7 | Yes, Round 7 | Patched |
| 7 | v0.7 | Reviewer A/B | Same full convergence checklist | Findings | v0.8 | Yes, Round 8 | Patched |
| 8 | v0.8 | Reviewer A/B | Same full convergence checklist | Findings | v0.9 | Yes, Round 9 | Patched |
| 9 | v0.9 | Reviewer A/B | Same full convergence checklist | Findings | v0.10 | Yes, Round 10 | Patched |
| 10 | v0.10 | Reviewer A/B | Same full convergence checklist | Findings | v0.11 | Yes, Round 11 | Patched |
| 11 | v0.11 | Reviewer A/B | Full exhaustive convergence checklist | Findings | v0.12 | Yes, Round 12 | Patched |
| 12 | v0.12 | Reviewer A/B | Same full exhaustive convergence checklist | A: 3, B: 5 | v0.13 | Yes, Round 13 | Patched |
| 13 | v0.13 | Reviewer A/B | Same full exhaustive convergence checklist | A: 3, B: 3 | v0.14 | Yes, Round 14 | Patched |
| 14 | v0.14 | Reviewer A/B | Same full exhaustive convergence checklist | A: 1, B: 4 | v0.15 | Yes, Round 15 | Patched |
| 15 | v0.15 | Reviewer A/B | Same full exhaustive convergence checklist | A: 4, B: 2 | v0.16 | Yes, Round 16 | Patched |
| 16 | v0.16 | Reviewer A/B | Same full exhaustive convergence checklist | A: 5, B: 1 | v0.17 | Yes, Round 17 | Patched |
| 17 | v0.17 | Reviewer A/B | Same full exhaustive convergence checklist | A: 5, B: 2 | v0.18 | Yes, Round 18 | Patched |
| 18 | v0.18 | Reviewer A/B | Same full exhaustive convergence checklist | A: 2, B: 3 | v0.19 | Yes, Round 19 | Patched |
| 19 | v0.19 | Reviewer A/B | Same optimized high-rigor full checklist | A: 1, B: 6 | v0.20 | Yes, Round 20 | Patched |
| 20 | v0.20 | Reviewer A/B | Same optimized high-rigor full checklist | A: 1, B: 3 | v0.21 | Yes, Round 21 | Patched |
| 21 | v0.21 | Reviewer A/B | Same optimized high-rigor full checklist | A: 2, B: 2 | v0.22 | Yes, Round 22 | Patched |
| 22 | v0.22 | Reviewer A/B | Same optimized high-rigor full checklist | A: 2, B: 0 | v0.23 | Yes, Round 23 | Patched |
| 23 | v0.23 | Reviewer A/B | Same optimized high-rigor full checklist | A: 1, B: 1 | v0.24 | Yes, Round 24 | Patched |
| 24 | v0.24 | Reviewer A/B | Same optimized high-rigor full checklist | A: 2 impl + 2 non-impl, B: 1 impl + 2 non-impl | v0.25 | Yes, Round 25 | Patched |
| 25 | v0.25 | Reviewer A/B | Same optimized high-rigor full checklist | A: 0, B: 1 impl + 1 non-impl | v0.26 | Yes, Round 26 | Patched |
| 26 | v0.26 | Reviewer A/B | Same optimized high-rigor full checklist with carry-through scan | A: 0, B: 1 | v0.27 | Yes, Round 27 | Patched |
| 27 | v0.27 | Reviewer A/B | Same optimized high-rigor full checklist with carry-through scan | A: 0, B: 3 | v0.28 | Yes, Round 28 | Patched |
| 28 | v0.28 | Reviewer A/B | Same optimized high-rigor full checklist with carry-through scan | A: 4, B: 2 impl + 1 non-impl | v0.29 | Yes, post-v0.29 Lane A/B + GPT review | Patched |
| 29 | v0.29 | Lane A/Lane B + external GPT | Scoped post-v0.29 lane protocol plus final architect review | Lane A: 0 blockers, Lane B: 0 blockers, GPT: 2 gate blockers + 1 preflight | v0.30 | Yes, v0.30 Lane A | Patched |
| 30 | v0.30 | Lane A | Patch carry-through review only | 1 gate-blocking bookkeeping | v0.31 | No | NEEDS PATCHES / PENDING v0.31 Lane A |

Convergence rule:

- For rounds before v0.29, a draft was converged only when Reviewer A and Reviewer B reviewed the same patched version using the same exhaustive full checklist and both reported no implementation-affecting findings.
- From v0.29 onward, the old same-exhaustive-checklist A/B rule is superseded by two scoped lanes on the same draft version:
  - Lane A Patch Carry-through Review: no patch-regression and no gate-blocking bookkeeping finding.
  - Lane B Latent Spec Review: no blocker-category finding under the post-v0.29 blocker rules.
- Post-v0.29 lane reviewers intentionally use different checklists. This is valid only because their scopes are explicit and complementary.
- `ACCEPT FOR IMPLEMENTATION` requires Lane A and Lane B both returning clean/blocker-free on the same draft version, external/final architect gate clean, and no patch applied afterward.
- Any patch after post-v0.29 lane or final architect review increments the internal draft label and requires at least Lane A carry-through re-review; Lane B is rerun only if the patch changes runtime/API behavior, DTO shape, error precedence, audit/hash inputs, lifecycle state, evidence boundary, or test expectations in a way that can create a blocker-category latent gap.

### Patch Impact Matrix

| Patch ID | Rule changed | Sections touched | DTO impact | Hash impact | Audit impact | API test impact | Boundary test impact | Carry-through complete? |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| v0.2 | Finalization boundary, completion snapshot, structured refs, builder scaffolding | 5, 7, 11, 12, 21, 25 | Complete/package/session DTO fields added | Manifest/package sources pinned | Audit refs split pre/post package | Missing-check, auth, forceReview, package read | Builder ambiguity categories added | Yes |
| v0.3-v0.4 | Atomic completion, retry after failure, package read precedence, expiry/idempotency | 6, 7, 11, 12, 19, 21 | Idempotent response fields pinned | Prepared package id/createdAt before hash | Post-package refs read-time/projected | Expiry, completed retry, state drift | Orphan/duplicate prevention | Yes |
| v0.5-v0.7 | Request body cleanup, error precedence, evidence ref aliases, TIP-07 artifact | 5, 7, 8, 11, 13, 20, 21 | `requestedBy` removed; aliases deprecated; webhook DTO chosen | Alias fields excluded from hash | Audit ref object shape pinned | `forceReview`, package read, alias assertions | Public/internal projection split | Yes |
| v0.8-v0.12 | Owner identity, conflict token, audit payload hashes, effective ids, `completedAt` | 7, 12, 16, 18, 20, 21 | `clientApplicationId` internal only; effective ids preserved | Conflict token and audit payload hash fixtures | Three successful audit schemas | Full endpoint field assertions | Owner identity STOP+ASK | Yes |
| v0.13 | Deterministic preparer, non-success audit deferral, unresolved policy, retry metadata | 7, 16, 19, 21 | Retry returns original effective ids | Deterministic ids/timestamps/hashes | Only three success events | Unresolved policy and second-id retry tests | Non-success side effects deferred | Yes |
| v0.14 | Failure audit removal, `SESSION_ALREADY_COMPLETED`, state/evidence ladder, timestamps | 7, 8, 9, 11, 12, 19, 21 | Completed retry conflict code removed | `decisionCreatedAt` added | No failure/read audit events | Lifecycle/evidence overlap tests | Error-code stale choice removed | Yes |
| v0.15 | Audit failure code, terminal precedence, `externalSessionId` completed summary | 5, 7, 16, 19, 21 | `ExternalSessionId` required | No direct hash change | `COMPLETION_AUDIT_PUBLICATION_FAILED` | Terminal overlap and audit failure tests | Completed summary field alignment | Yes |
| v0.16 | Timestamp format, Expired state, reason-code safety | 7, 9, 12, 14, 19, 21 | Reason codes sanitized before DTOs | Whole-second UTC `Z`; invalid codes excluded by rejection | Invalid codes blocked before audit | Expired/invalid-code API tests | Sanitization boundary tests | Yes |
| v0.17 | Preparer session/client inputs, confidence ignored, caller-category and client id tests | 7, 9, 13, 20, 21 | `ClientApplicationId` only in TIP-07 artifact; confidence not exposed | Preparer seed includes session/client; confidence excluded | No new audit fields | Invalid-code and caller-category tests | ClientApplicationId absence tests | Yes |
| v0.18 | `operationNow`, all successful audit failures, malformed package id | 5, 7, 8, 16, 19, 21 | Authoritative completed summary field set | `operationNow` for attempt timestamps | All three success audit failures covered | Malformed package id; unauthenticated both endpoints | No hidden failed-attempt state | Yes |
| v0.19 | External retry as new attempt, reason-code ladder placement, missing session branch | 7, 8, 21, 25 | No new DTO change | New external retry may create new deterministic value set | No visible audit on failure | Overlap tests for reason-code precedence and missing session | No direct public boundary change; missing-session branch remains distinct from ownership/policy checks. | Yes |
| v0.20 | PayloadHash prohibition, reason-code overlap tests, retry fresh-clock tests, manifest/hash failure code, API leakage tests, evidence request/correlation exclusion | 9, 11, 13, 17, 19, 21, 22, 25 | Public DTO/API absence assertions expanded | Evidence request/correlation ids excluded; manifest/hash failure code pinned | No audit on manifest/package/hash failure | New overlap, leakage, fresh-clock, and manifest/hash failure tests | Boundary tests cover API absence of public `payloadHash` and source evidence request/correlation id exclusion. | Yes |
| v0.21 | Evidence request/correlation exclusion tests, `PayloadHash` PascalCase absence, exact audit ref package inputs | 11, 12, 13, 21, 25 | DTO reflection forbids `PayloadHash` | Source evidence-record request/correlation ids excluded; `evidenceResultId` remains required and hash-participating | Exact 1 pre-package and 2 post-package audit refs only | New golden/contract/API tests for source evidence-record request/correlation ids, PascalCase, unrelated audits | Boundary tests cover DTO/property `PayloadHash` absence and exact audit-ref scoping. | Yes |
| v0.22 | Scoped post-package audit projection, `evidenceResultId` matrix correction, payload hash property/JSON prohibition | 12, 16, 17, 21, 22, 25 | DTO/property `PayloadHash` forbidden | `evidenceResultId` hash participation explicitly preserved | Post-package refs scoped to this completion/session/package | New tests for unrelated same-type completion audit events and `evidenceResultId` hash changes | Boundary tests cover unrelated same-type audit exclusion and both JSON/property payload hash absence. | Yes |
| v0.23 | Completed session result/assurance assertions and forceReview earlier-gate overlap tests | 5, 7, 21, 25 | Completed session summary asserts `result` and `assuranceLevel` | No direct hash change | No direct audit change | New completed-session result/assurance and forceReview overlap API tests | Boundary tests cover request-option validation occurring only after authentication/category/scope/session lookup/ownership/policy gates. | Yes |
| v0.24 | Completed-session result value assertions and scaffolding alignment | 5, 21, 25 | `VerificationSessionSummaryDto` scaffolding included `Result` and `AssuranceLevel` but was later found partial | No direct hash change | No direct audit change | Completed session GET asserted exact `result` and `assuranceLevel` for passed and generic non-passed outcomes | No direct boundary change; covered by completed-session DTO/API field assertions | Carry-through reviewed in Round 24; patched again in v0.25 |
| v0.25 | Full completed-session scaffolding, BusinessConsumer evidence-ref `result` contract, per-outcome completed-session assertions, trace/matrix/self-check cleanup | 5, 13, 21, 25, 26, 27 | `VerificationSessionSummaryDto` scaffolding matches full completed-session field set; default BusinessConsumer evidence refs exclude per-evidence `result` | No direct hash change; internal manifest evidence `result` remains hash input while public evidence-ref `result` is excluded by default | No direct audit change | Completed session GET asserts exact `result` and `assuranceLevel` for `Passed`, `FailedIdentity`, `FailedCaptureQuality`, `ReviewRequired`, and `TechnicalError` | Contract/boundary tests must prove default BusinessConsumer evidence refs omit per-evidence `result` while summary-level final result remains exposed | Carry-through reviewed in Round 25; patched again in v0.26 |
| v0.26 | Effective request/correlation id carry-through in completion snapshot/projection, Patch Impact Matrix boundary cleanup, explicit carry-through scan | 7, 18, 21, 25, 26, 27 | Completion snapshot/projection explicitly persists effective `RequestId` and `CorrelationId`; no DTO shape change | No direct hash change; effective request/correlation ids already remain package/manifest metadata where listed | No direct audit change; effective request/correlation ids already remain audit metadata where listed | Existing completed GET/idempotent retry tests now align with explicit snapshot/projection persistence contract | No direct public boundary change; scan confirms no new exposure or STOP+ASK change | Carry-through reviewed in Round 26; patched again in v0.27 |
| v0.27 | `INCOMPLETE_REQUIRED_CHECKS` error envelope carry-through and explicit carry-through scan | 8, 19, 21, 25, 26, 27 | No DTO shape change | No direct hash change | No direct audit change | Section 21 `error.details.missingChecks` assertion now aligns with Section 19 example | Boundary preserved: details contain only sanitized sorted RequiredCheckType names and no raw evidence/internal data | Carry-through reviewed in Round 27; patched again in v0.28 |
| v0.28 | API overlap/read-audit test expectations and explicit carry-through scan | 7, 8, 16, 21, 25, 26, 27 | No DTO shape change | No direct hash change | No direct audit change | Added tests for RiskEvaluation/unsupported/missing precedence, `forceReview=true` terminal overlap, and package-read no-audit side effect | Boundary preserved: no new data exposure; read audit remains deferred and post-package refs stay scoped to completion events | Carry-through reviewed in Round 28; patched again in v0.29 |
| v0.29 | Round 28 batch: Created precedence, RetryReasonCode null/blank, no-audit denial coverage, public summary boundary wording, exact terminal states, self-check wording | 3, 7, 9, 16, 21, 25, 26, 27 | No DTO shape change; default public evidence refs still exclude per-evidence result/status/timestamps | No direct hash change | No direct audit change; no-audit invariant now has representative denial-path tests | Added Created overlap tests, RetryReasonCode null/blank tests, package-read denial no-audit tests, `/complete` non-success no-audit tests, exact terminal-state tests | Public boundary tightened for summary-only status/timestamp exposure and no per-evidence result/status/timestamp exposure | Lane A/B blocker-free; external GPT patched governance/proof wording in v0.30 |
| v0.30 | External GPT targeted governance/proof/preflight patch | 21, 22, 25, 26, 27 | No DTO shape change | No direct hash change | No direct audit behavior change; proof wording now requires unchanged audit store/no visible audit event of any type | No-audit tests now assert audit store count unchanged and no audit event of any type for package-read denial and `/complete` non-success paths | Post-v0.29 convergence rule supersedes old A/B full-checklist requirement; lifecycle-state preflight added to STOP+ASK | Lane A found status metadata stale; patched in v0.31 |
| v0.31 | Top-level status metadata carry-through | Header, 25, 26, 27 | No DTO shape change | No hash change | No audit change | No test expectation change | Status metadata now matches post-v0.29 Lane A carry-through protocol | No - pending v0.31 Lane A carry-through review |

### Builder Reviewer Result

Builder reviewer actual result:

- Round 1 Builder reviewer found implementation-affecting ambiguity and patches were applied in v0.2.
- Later A/B exhaustive reviews repeatedly found builder-style ambiguity in DTO field lists, finalization behavior, state mutation semantics, audit ordering, hash inputs, API test coverage, public evidence-ref contract shape, completion snapshot persistence, error-envelope carry-through, API overlap/read-audit test coverage, lifecycle state exactness, and RetryReasonCode validation semantics; those findings are recorded in the round tables above and patched through v0.29.
- External GPT review found gate-blocking governance/proof wording issues and an implementation preflight; those are patched in v0.30.
- v0.30 Lane A found gate-blocking status metadata bookkeeping; this is patched in v0.31.
- Historical builder status for v0.31 before final external architect gate: NEEDS LANE A CARRY-THROUGH REVIEW. Current governance status: superseded by final external architect gate; TIP-06 implementation was committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`.

Builder coverage snapshot for v0.31:

| Builder check group | Current status |
| --- | --- |
| DTO names/field names | Patched; no v0.30 change |
| Service/port names | Patched; no current open finding |
| Repository/finalization behavior | Patched; no v0.30 change |
| State mutation semantics | Patched; no v0.30 runtime change |
| Atomicity/idempotency | Patched; no v0.30 change |
| Response fields | Patched; no v0.30 change |
| Error precedence | Patched; no v0.30 change |
| Hash inputs/canonicalization | Patched; no v0.30 change |
| Audit ordering | Patched; v0.30 strengthens no-audit proof wording |
| API pipeline tests | Patched; no v0.31 change |
| Boundary/security scans | Patched; lifecycle-state STOP+ASK preflight added |

## 26. Final Codex Neutral Self-Check / Architect-Gate Self-Audit

Final neutral self-check status: HISTORICAL. This section records the pre-implementation v0.31 self-check state. Current closeout status is accepted externally, implemented at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`, and post-commit validated with 60 passed and 0 failed.

Required explicit answers:

| Question | Answer | Evidence note |
| --- | --- | --- |
| Does TIP-06 preserve TIP-05 evidence-chain integrity? | YES | Latest accepted evidence by RequiredCheck, append ordinal conflict token, and no raw artifact mutation are pinned in sections 7, 8, 11, and 21. |
| Can final result be calculated deterministically? | YES | Final decision precedence, reason-code safety, and sorted check/reason arrays are pinned in sections 8 and 9. |
| Can `Completed` happen without evidence package/hash? | NO, prevented | Sections 7, 11, and 18 require decision id, evidence package id/hash, manifest hash, signature status, and completed projection in one visible finalization unit. |
| Can BusinessConsumer receive internal manifest, VaultRef, raw artifact, raw identity, or TrustedAdapter DTOs? | NO, prevented | Sections 5, 12, 13, 15, 20, and 21 exclude these from default BusinessConsumer complete/session/package JSON. |
| Are placeholder signatures clearly non-authoritative? | YES | Section 15 keeps `PlaceholderUnverified` status-only and excludes signature bytes, keys, certs, algorithms, authenticity, replay, and non-repudiation claims. |
| Does TIP-06 leave enough fields for TIP-07 webhook without implementing webhook? | YES | Section 20 pins `VerificationCompletedEventDto` as compatibility artifact with placeholder delivery/signature semantics and no dispatch runtime. |
| Are retry/idempotency semantics explicit? | YES | Section 7 distinguishes completed idempotent retry from new external retry after no-visible failure and explicitly persists effective request/correlation ids in the completion snapshot. Historical v0.31 Lane A carry-through wording is superseded by final external architect acceptance. |
| Are all runtime endpoint claims covered by API pipeline tests? | YES, documented | Section 21 lists API coverage for complete/package read, malformed ids, invalid codes, caller categories, expiry, failures, and boundary absences. |
| Did any patch introduce new contradictions? | HISTORICAL PENDING STATE SUPERSEDED | v0.31 applied a status metadata patch after v0.30 Lane A review; that stale pending state is superseded by final external architect acceptance and the TIP-06 implementation commit. |
| Does this TIP avoid both over-narrowing and scope creep? | YES | Sections 20, 22, 23, and 24 defer production crypto/vault/storage/webhook/admin/FraudRisk while preserving required fields. |

## 27. Final Recommendation

```text
HISTORICAL PLANNING RECORD - ACCEPTED EXTERNALLY AND IMPLEMENTED
```

Reason: current implementation state supersedes the pre-implementation v0.31 recommendation. TIP-06 implementation accepted and committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`; final external architect gate supersedes the stale in-document NEEDS PATCHES wording. Post-commit validation passed with 60 passed and 0 failed.
