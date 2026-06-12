# TIP-02 S1 Execution Roadmap v0.2

**File:** `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
**Version:** 0.6
**Status:** Planning - TIP-08 implemented; TIP-09 hardening/closeout remains
**Date:** 2026-06-12
**Baseline:** Product Brief v0.1.1
**Purpose:** Defines the execution plan from the completed TIP-01 skeleton to TagEkyc S1 completion.

## Changelog

### v0.6 - TIP-08 state synchronized

- Recorded TIP-08 code/test commit `282eb821b7500f2965b336a5e67467bffc68adf4`.
- Recorded post-commit validation: 64 passed, 0 failed, 0 skipped.
- Clarified that TIP-08 stayed test/proof-only and did not add SignFlow runtime/source/database/network dependency or public runtime behavior.

### v0.5 - TIP-07 state synchronized

- Recorded TIP-07 Option A completion notification projection implementation commit `916dd2918c2ab47ab0658ebf271fae45e22fb3ca`.
- Clarified that TIP-07 delivered the accepted LocalDev completion-notification projection only, with no public route or webhook runtime.
- Deferred webhook delivery/retry to a later separate planning slice instead of keeping it as the immediate next action.

### v0.4 - TIP-06 state synchronized

- Recorded that TIP-04, TIP-05, and TIP-06 runtime slices have implementation records after the TIP-03 closeout.
- Recorded TIP-06 implementation commit `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef` and docs closeout commit `dd878e5`.
- Set TIP-07 webhook delivery and retry planning as the next roadmap action.

### v0.3 - TIP-03 acceptance synchronized

- Recorded TIP-03 review completion and accepted closeout state.
- Set TIP-04 kickoff preparation as the next recommended action.

### v0.2 - Review hardening before TIP-03

- Moved transaction-bound profile invariants into TIP-03/TIP-04 responsibilities.
- Added append-only evidence/audit repository and persistence invariants to TIP-03.
- Split internal/audit evidence manifest from default business-consumer evidence summary contracts.
- Marked S1 signature placeholders as non-authoritative.
- Added a TIP-03 pre-start invariant checklist.

### Acceptance update - 2026-06-10

- TIP-02A confirmation passed and was accepted by the user.
- TIP-01 is now fully accepted as the clean S1 skeleton baseline.
- TIP-03 kickoff/checklist v0.2 was accepted with minor review patches.
- TIP-03 implementation was reviewed, accepted, and closed within the accepted v0.2 boundaries. Final artifacts: `tip_03_execution_report_v0_1.md`, `tip_03_review_v0_1.md`, and `tip_03_closeout_v0_1.md`.
- TIP-04 implementation records exist in `tip_04_execution_report_v0_1.md`.
- TIP-05 implementation was accepted after confirmation review.
- TIP-06 implementation was accepted and committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`; TIP-06 docs closeout was committed at `dd878e5`.

### v0.1 - Initial S1 execution roadmap

- Mapped S1 exit criteria to implementation TIPs.
- Defined recommended implementation order after TIP-01.
- Identified automation-safe work and STOP+ASK gates.
- Preserved S1 non-production and SignFlow consumer-profile boundaries.

## 1. Current State

TIP-01 created the .NET 8 solution skeleton and placeholder project boundaries.

Current implementation state:

- `TagEkyc.Api`, `TagEkyc.Application`, `TagEkyc.Domain`, `TagEkyc.Contracts`, `TagEkyc.Infrastructure`, `TagEkyc.Adapters`, and `TagEkyc.SignFlow` have progressed beyond the original skeleton through the TIP-06 runtime slice.
- Unit, contract, and architecture smoke tests exist.
- TIP-03 through TIP-06 cover core domain/contracts, local development persistence boundary, API key/session lifecycle, capture/evidence recording, final decision, evidence package, and audit manifest behavior.
- TIP-07 Option A adds an internal/application-service completion notification projection with deterministic LocalDev placeholder delivery semantics and no public route.
- TIP-08 adds a test/proof-only LocalDev transaction-bound SignFlow S1 flow at `282eb821b7500f2965b336a5e67467bffc68adf4`, with no `src/**`, DTO/contract, endpoint/query/service/runtime projection, or SignFlow runtime/source/database/network changes.
- Webhook runtime, retry/outbox behavior, production cryptography, production storage/vault lifecycle, production adapter trust, and real raw artifact handling remain deferred to later accepted TIPs.

TIP-02A recorded clean post-hygiene evidence and the user accepted TIP-01 as the clean S1 skeleton baseline. TIP-03 kickoff/checklist v0.2 was accepted, implemented, reviewed, and closed on 2026-06-10. TIP-04, TIP-05, and TIP-06 moved the roadmap through final decision and evidence package implementation, with TIP-06 code/test committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`. TIP-07 Planning Brief v0.3 was then accepted for Option A only and implemented at `916dd2918c2ab47ab0658ebf271fae45e22fb3ca` with post-commit validation of 63 passed and 0 failed. TIP-08 was implemented at `282eb821b7500f2965b336a5e67467bffc68adf4` with post-commit validation of 64 passed and 0 failed.

## 2. S1 Definition of Done

S1 is complete when the Phase 1 exit criteria are met without claiming production-certified legal eKYC readiness:

- A client application can create a verification session using API key authentication.
- RequiredChecks are persisted and enforced by session lifecycle.
- Mock/PoC capture artifacts and document, NFC, face, liveness, and fingerprint evidence results can be recorded using stable result shapes.
- Capture quality can produce `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `REVIEW_REQUIRED`, or `TECHNICAL_ERROR` without collapsing those outcomes into identity failure.
- Final verification result can be calculated from evidence summaries.
- Evidence package can be built using VaultRefs/hashes and a deterministic manifest hash.
- Audit events are written for key lifecycle actions.
- Webhook completion result can be delivered or retried.
- SignFlow contract is documented and covered as a transaction-bound profile with binding validation fields.
- Documentation clearly states S1 is not production-certified eKYC.

## 3. Default S1 Product Assumptions

Unless the user changes the baseline, implementation should use these defaults:

- Default SignFlow S1 required checks: `CAPTURE_QUALITY`, `DOCUMENT_NFC`, `FACE_MATCH`, and `LIVENESS`.
- `FINGERPRINT` remains optional/demo-only unless explicitly enabled by policy.
- `TRANSACTION_BOUND_EKYC_PROFILE` requires `externalTransactionId` and `bindingNonceHash`.
- Generic `STANDARD_EKYC_PROFILE` must not default to SignFlow-specific purpose or binding fields.
- S1 may use mock/PoC adapters behind interfaces.
- S1 must not store real raw biometric or raw identity artifacts outside a vault abstraction.
- S1 must not expose raw artifacts, internal VaultRefs, or plaintext sensitive identity data to business consumers by default.
- S1 uses placeholder signature fields only; production cryptography is deferred.
- S1 signature placeholders are non-authoritative and MUST NOT be treated as authenticity, replay-protection, or external audit guarantees.
- Internal/audit evidence manifests may contain internal VaultRefs, but default business-consumer payloads must use sanitized summaries, hashes, package ids, evidence refs, statuses, and timestamps only.

## 4. Execution TIP Roadmap

### TIP-02A - Repository Hygiene and Baseline Acceptance

Goal: Make the TIP-01 skeleton clean enough to build future S1 work on.

Scope:

- Add or verify `.gitignore` for .NET `bin/`, `obj/`, user files, local data, and test artifacts.
- Remove generated build outputs from the intended source change set.
- Re-run restore/build/test.
- Record TIP-01 acceptance or remaining review findings.

Automation-safe:

- Add `.gitignore`.
- Report generated artifacts.
- Re-run build/test.

STOP+ASK:

- Deleting untracked generated files if the user has not authorized cleanup.
- Accepting TIP-01 as a phase gate if explicit user acceptance is required.

### TIP-03 - Core Domain, Contracts, and S1 Persistence Boundary

Goal: Define the runtime model needed by S1 while staying aligned with LLD01 and LLD03.

Scope:

- Pre-start invariant checklist against Product Brief, LLD01, and LLD03 before model/DTO work begins.
- Domain models/enums for client applications, API keys, verification sessions, required checks, capture artifacts, evidence results, verification results, evidence packages, audit events, webhook subscriptions, and webhook deliveries.
- Public DTO contracts for LLD03 S1 endpoints.
- Repository interfaces and application service ports.
- S1 development persistence implementation with no production migration claim.
- Unit tests for validation and state-independent model rules.
- Domain/contract rules for `STANDARD_EKYC_PROFILE` vs `TRANSACTION_BOUND_EKYC_PROFILE`, including `externalTransactionId` and `bindingNonceHash` requirements.
- Separate internal/audit evidence manifest contracts from default business-consumer evidence summary contracts.
- Non-authoritative placeholder signature status/fields for S1.

Recommended persistence approach:

- Use a simple local development persistence adapter for S1 sanitized metadata and refs only.
- Avoid production DB migrations in S1.
- Keep persistence behind interfaces so a later production database TIP can replace it.
- Distinguish mutable session current-state projection from append-only evidence results and audit events.
- Repository ports SHOULD NOT expose update/delete methods for evidence results or audit events.

Automation-safe:

- Implement LLD-aligned domain and DTO shapes.
- Add development-only persistence behind interfaces.
- Add tests for model validation.
- Add tests or architecture checks for append-only evidence/audit repositories.
- Add contract tests that default business payloads contain no raw artifact fields, plaintext identity fields, or internal VaultRefs.
- Add tests/docs proving S1 placeholder signatures are `PLACEHOLDER_UNVERIFIED` or equivalent and are not authenticity guarantees.

Pre-start invariant checklist:

- Profile rules: `STANDARD_EKYC_PROFILE` has no SignFlow defaults; `TRANSACTION_BOUND_EKYC_PROFILE` requires transaction binding fields.
- RequiredChecks are explicit and validated against client policy.
- Capture-quality outcomes remain distinct from identity failure outcomes.
- Raw artifacts, plaintext identity data, and internal VaultRefs are excluded from default business-consumer payloads.
- Evidence results and audit events are append-only.
- S1 signature placeholders are non-authoritative.
- No SignFlow source code, database, runtime packages, or internal models are referenced.

STOP+ASK:

- Choosing production database technology.
- Adding migrations or production schema.
- Storing real raw biometric, raw document, or raw NFC data.
- Changing LLD data model semantics.

### TIP-04 - API Key Authentication, Client Policy, and Session Lifecycle

Goal: Allow business clients to create and retrieve verification sessions safely.

Scope:

- API key authentication middleware or filter.
- Client application policy model for allowed purposes, profiles, and checks.
- `POST /api/ekyc/verification-sessions`.
- `GET /api/ekyc/verification-sessions/{id}`.
- Create-session validation for profile-level invariants, including transaction-bound `externalTransactionId` and `bindingNonceHash`.
- Session state transitions for create, expire, complete-ready, and terminal states.
- RequiredChecks validation and enforcement.
- Audit events for session creation and state changes.

Automation-safe:

- Implement authentication and session APIs from LLD03.
- Use local/dev API key configuration.
- Add contract and integration-style tests.

STOP+ASK:

- Production credential storage or external secret manager.
- Expanding accepted profiles or purposes beyond baseline.
- Treating client-supplied `externalSystem` as authoritative.

### TIP-05 - Capture Artifact and Evidence Result Recording

Goal: Record S1 evidence inputs and sanitized evidence results with stable shapes.

Scope:

- `POST /api/ekyc/verification-sessions/{id}/capture-artifacts`.
- S1 specialized evidence result endpoints:
  - `document-result`
  - `nfc-result`
  - `face-result`
  - `fingerprint-result`
  - `capture-quality-result`
- Distinct caller scopes for business clients, capture agents/device gateways, and internal adapters.
- Sanitized result storage with artifact hashes and evidence refs.
- Capture quality retry and failure semantics.
- Audit events for artifact and evidence result recording.

Automation-safe:

- Implement sanitized mock/PoC result shapes.
- Enforce no raw artifact bytes in business-facing responses.
- Add tests for `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, and `TECHNICAL_ERROR`.

STOP+ASK:

- Real capture agent trust program.
- Real raw artifact retention, deletion, legal hold, or vault lifecycle policy.
- Exposing internal VaultRefs or raw payloads to business consumers.

### TIP-06 - Final Decision, Evidence Package, and Audit Manifest

Goal: Complete sessions by calculating the final result and producing a deterministic evidence package.

Scope:

- `POST /api/ekyc/verification-sessions/{id}/complete`.
- `GET /api/ekyc/evidence-packages/{id}`.
- RequiredChecks completeness enforcement.
- S1 risk/decision evaluator behind `IRiskEvaluator`.
- Evidence package manifest with evidence refs, artifact hashes, adapter versions, timestamps, audit refs, and deterministic hash.
- Default business-consumer evidence package summary that excludes internal VaultRefs and raw/plaintext sensitive fields.
- Append-only audit event behavior for key lifecycle actions.
- Placeholder `evidencePackageSignature` field without production cryptography and explicitly marked non-authoritative.

Automation-safe:

- Implement deterministic manifest hashing over sanitized metadata.
- Add unit/contract tests for final result calculation.
- Add tests that incomplete required checks cannot pass.

STOP+ASK:

- Production evidence package signing key process.
- Assurance-level policy changes outside baseline.
- Legal audit reliance claims.

### TIP-07 - Completion Notification Projection

Goal: Prepare a deterministic, client-safe LocalDev completion notification projection without opening public webhook runtime.

Scope:

- Internal/application-service projection of `BusinessConsumer.VerificationCompletedEventDto` from the TIP-06 completed session/evidence package state.
- Deterministic LocalDev placeholder delivery semantics only: existing `EventType`, `DeliveryId = "localdev-not-dispatched"`, and `SentAt = completedAt`.
- Placeholder `webhookSignature` and evidence-package signature status semantics remain non-authoritative.
- Tests for exact field mapping, deterministic repeatability, cross-client boundary enforcement, and no raw/internal leakage.

Automation-safe:

- Implement the internal projection/query and focused tests.
- Preserve placeholder signature semantics.
- Keep the implementation free of public routes, dispatcher/subscription runtime, retry scheduling, durable outbox state, EF/migrations, and SignFlow runtime dependencies.

STOP+ASK:

- Any public endpoint exposing the projection.
- Any webhook subscription, dispatcher, retry, outbox, or durable persistence behavior.
- Production webhook signing algorithm, replay protection, key rotation, or external endpoint credentials/infrastructure.

Deferred follow-on after TIP-07:

- A later accepted planning slice must cover actual webhook delivery, retry, outbox/delivery metadata, and operational concerns if those behaviors are needed.

### TIP-08 - SignFlow Transaction-Bound Profile and End-to-End S1 Flow

Goal: Prove the S1 happy path for SignFlow without importing SignFlow internals.

Status: Implemented and accepted at `282eb821b7500f2965b336a5e67467bffc68adf4` (`test: prove TIP-08 transaction-bound SignFlow S1 flow`). Post-commit validation passed with 64 passed and 0 failed.

Scope:

- Verification that transaction-bound create-session validation from TIP-04 is correctly exercised by the SignFlow profile.
- Echo and preservation of `externalSessionId`, `externalTransactionId`, and `bindingNonceHash` in SignFlow-oriented flows.
- SignFlow-specific sanitized result contract placeholders in `TagEkyc.SignFlow`.
- End-to-end tests from session creation through evidence recording, completion, evidence package retrieval, and webhook payload creation.
- Negative tests for missing or mismatched transaction-bound fields.

Automation-safe:

- Implement TagEkyc-owned SignFlow contract helpers.
- Add end-to-end tests using local/dev data.

STOP+ASK:

- Referencing SignFlow source code, database, runtime packages, or internal models.
- Changing SignFlow from consumer profile to platform dependency.
- Adding signing, WYSIWYS, TSP, or consent proof behavior.

### TIP-09 - S1 Hardening, Documentation, and Closeout

Goal: Verify S1 as evidence-ready but not production-certified eKYC.

Scope:

- Full build/test pass.
- API contract review against LLD03.
- Security/data-boundary review.
- Documentation update for running S1.
- S1 closeout report with known debts and production blockers.
- Explicit non-production certification statement.

Automation-safe:

- Run checks and write closeout draft.
- Update docs for implemented behavior.

STOP+ASK:

- Accepting S1 as a phase gate.
- Any legal/compliance/certification claim.
- Moving any P0 debt into production-ready status.

## 5. Suggested Implementation Order

1. TIP-02A repository hygiene and TIP-01 acceptance.
2. TIP-03 core domain, contracts, and S1 persistence boundary.
3. TIP-04 API key authentication, client policy, and session lifecycle.
4. TIP-05 capture artifact and evidence result recording.
5. TIP-06 final decision, evidence package, and audit manifest.
6. TIP-07 completion notification projection.
7. TIP-08 SignFlow transaction-bound profile and E2E S1 flow.
8. TIP-09 S1 hardening, documentation, and closeout.

This order builds the platform core before SignFlow-specific validation. It also keeps legal, retention, production cryptography, and production infrastructure decisions outside automation scope.

## 6. Cross-Cutting Test Strategy

Each implementation TIP should include:

- Unit tests for domain/application rules.
- Contract tests for DTO shape and response boundaries.
- Architecture tests for dependency direction.
- API/integration-style tests for endpoint behavior where feasible.
- Negative tests for raw data exposure and SignFlow boundary violations.

Minimum S1 end-to-end scenarios:

- Standard profile session can be created and retrieved.
- Transaction-bound SignFlow session requires and preserves binding fields.
- Transaction-bound profile validation fails without `externalTransactionId` or `bindingNonceHash`.
- Required checks block completion until satisfied.
- Capture quality retry does not become identity failure.
- Passed required evidence produces final `PASSED` and an evidence package hash.
- Failed identity evidence produces final `FAILED`.
- Ambiguous/incomplete evidence produces `REVIEW_REQUIRED`.
- Technical adapter failure produces `ERROR` or documented terminal behavior.
- Completion notification projection is sanitized and deterministic.
- Default business evidence package payload excludes raw artifacts, plaintext identity fields, and internal VaultRefs.
- Placeholder signature fields are visibly non-authoritative in S1.

## 7. User Gate Register

The coordinator should ask the user only for these gates:

- Accept TIP-01 skeleton baseline if explicit phase acceptance is required.
- Accept S1 closeout after TIP-09.
- Any change to Product Brief, HLD, LLD, S1 scope, or SignFlow boundary.
- Any legal, compliance, certification, retention, deletion, legal hold, production cryptography, or real biometric/raw artifact storage decision.
- Any production vendor, paid service, credential, external account, or deployment infrastructure decision.

Default recommendation:

- Proceed with the next accepted TIP planning without changing Product Brief/HLD/LLD.
- Use the Product Brief S1 defaults: CCCD/NFC, face match, and liveness required for SignFlow; fingerprint optional/demo-only.
- Keep all raw sensitive data out of default business-consumer payloads.
- Keep S1 explicitly evidence-ready, not production-certified.

## 8. Immediate Next Action

No active in-scope implementation is opened by this roadmap update.

If webhook delivery/retry is later needed, open a separate accepted planning slice after TIP-07 Option A rather than treating it as already authorized.
