# TIP-03 Core Domain, Contracts, and S1 Persistence Boundary Kickoff v0.2

**File:** `docs/tips/tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md`
**Version:** 0.2
**Status:** Accepted with v0.2 review patches - TIP-03 implementation authorized within documented boundaries
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1
**Prerequisite:** TIP-01 Project Skeleton fully accepted as clean S1 skeleton baseline through TIP-02A.
**Purpose:** Pins the TIP-03 pre-start decisions, STOP+ASK gates, and implementation boundaries before any TIP-03 code is written.

## Changelog

### v0.2 - Review acceptance patches

- Narrowed development-only persistence to in-memory/test-only behavior unless explicitly approved; EF providers, DbContext, migrations, schema, and production-like persistence remain out of scope.
- Clarified `EvidenceResult` submission DTOs as trusted-adapter/internal contract shapes only; placement in a Contracts assembly does not imply business-client access.
- Required separated contract namespaces/projections for `BusinessConsumer`, `CaptureAgent`, `TrustedAdapter`, `InternalAudit.Manifest`, and `SignFlowProfile`.
- Clarified API key model scope as metadata/policy only, with no production secret storage, hashing/rotation, middleware, or external secret manager behavior.
- Marked all S1 signature-related fields as `PLACEHOLDER_UNVERIFIED` and non-authoritative for authenticity, replay protection, and audit reliance.

### v0.1 - Kickoff checklist created

- Recorded TIP-03 pre-start decisions requested after TIP-02A acceptance.
- Pinned adapter trust boundary, capture artifact model, VaultRef exposure boundary, lifecycle/result separation, SignFlow required checks, SignFlow dependency boundary, and production database gate.
- Preserved TIP-03 as planning-only until this checklist is reviewed and accepted.

## 1. Current Acceptance State

User accepted the TIP-02A result on 2026-06-10.

Accepted baseline:

```text
TIP-01 Project Skeleton: FULLY ACCEPTED AS CLEAN S1 SKELETON BASELINE
```

Assumption carried into TIP-03:

- The TIP-02A confirmation report is included in the intended source set.
- `.gitignore` is included in the intended source set.
- No generated `bin/`, `obj/`, or `TestResults/` outputs are tracked.

## 2. TIP-03 Goal

TIP-03 may define the S1 core domain, separated contract projections, repository ports, application service ports, and narrow development/test persistence boundary needed by later S1 implementation.

TIP-03 must stay aligned with:

- Product Brief v0.1.1.
- HLD.
- LLD01 data model.
- LLD03 API contracts.
- LLD04 engine adapter contracts.
- TIP-02 roadmap and review findings.

TIP-03 implementation is authorized after the v0.2 review patches in this kickoff/checklist are applied, and only within the documented boundaries.

## 3. Pinned Decisions

### 3.1 Adapter Trust Boundary

Proposed decision:

- Business clients create sessions and consume sanitized results only.
- Capture agents or device gateways submit artifacts or capture metadata only.
- Internal or explicitly trusted adapters submit `EvidenceResult`.
- Ordinary business clients must not submit arbitrary `PASSED` evidence or final verification results.
- `EvidenceResult` submission DTOs are trusted-adapter/internal contract shapes only.
- The existence of `EvidenceResult` submission DTOs in a Contracts project or package must not be interpreted as granting business-client access.

Implementation boundary:

- TIP-03 domain/contracts must model caller separation clearly enough for TIP-04 and TIP-05 enforcement.
- TIP-03 must not implement auth, scopes, middleware, trusted adapter credentials, or runtime authorization behavior. Those belong to later API/runtime TIPs.
- Contract names, namespaces, projections, and documentation must avoid implying that a business client may directly submit final evidence outcomes.

### 3.2 Capture Artifact Upload/Reference Model For S1

Proposed decision:

- S1 accepts capture artifact records as sanitized metadata plus a stable artifact reference.
- The stable reference may be a development `captureArtifactId`, external/pre-staged artifact handle, or future vault-backed handle.
- Raw artifact bytes are not part of default business-facing DTOs.
- Base64 raw document, biometric, video, NFC, or fingerprint payloads must not be introduced as normal business API fields in TIP-03.
- If later S1 needs local PoC file upload, that must be handled by a later API/runtime TIP using a bounded upload mechanism, not by broadening TIP-03 contracts to carry raw data.

Implementation boundary:

- TIP-03 may define metadata contracts such as artifact type, artifact hash, content hints, capture timestamp, source category, and sanitized refs.
- TIP-03 must not implement upload endpoints, filesystem/object storage, vault storage, retention, deletion, legal hold, or raw artifact processing.

### 3.3 VaultRef Exposure Boundary

Proposed decision:

- Internal/audit evidence manifests may contain internal VaultRefs.
- Default business-consumer payloads must not expose internal VaultRefs.
- Default business-consumer payloads must not expose raw artifacts, raw artifact paths, raw biometric fields, raw document/NFC payloads, or plaintext sensitive identity fields.
- Default business-consumer payloads should expose only sanitized summaries, evidence refs, package ids, hashes, statuses, timestamps, and non-sensitive metadata needed by consumers.

Implementation boundary:

- TIP-03 must separate internal/audit manifest models from business-consumer summary DTOs.
- Tests should assert that default business payload contracts do not include internal VaultRefs, raw artifact fields, or plaintext sensitive identity fields.
- TIP-03 must not implement vault runtime behavior.

### 3.4 Session Lifecycle State vs Verification Result Separation

Proposed decision:

- Session lifecycle state and verification result are separate concepts.
- Lifecycle state describes where the session is in processing, for example `CREATED`, `IN_PROGRESS`, `READY_TO_COMPLETE`, `COMPLETED`, `EXPIRED`, `CANCELLED`, or a documented technical terminal state.
- Verification result describes the outcome, for example `NOT_AVAILABLE`, `PASSED`, `FAILED_IDENTITY`, `FAILED_CAPTURE_QUALITY`, `RETRY_REQUIRED`, `REVIEW_REQUIRED`, or `TECHNICAL_ERROR`.
- A failed identity outcome should not force the lifecycle state itself to be named `FAILED`.

Implementation boundary:

- TIP-03 domain and contracts must keep these enum families separate.
- State transition runtime enforcement may be modeled but not exposed as full API behavior in TIP-03 unless explicitly scoped.

### 3.5 SignFlow S1 Required Checks

Proposed decision:

Default SignFlow S1 required checks:

- `CAPTURE_QUALITY`
- `DOCUMENT_NFC`
- `FACE_MATCH`
- `LIVENESS`

Additional policy:

- `FINGERPRINT` is optional/demo-only unless a client policy explicitly enables it.
- `DOCUMENT_OCR` is optional/supporting unless explicitly required by policy.
- `DOCUMENT_NFC` does not silently imply that OCR is required unless policy says so.

Implementation boundary:

- TIP-03 may model required check enums and policy validation inputs.
- TIP-03 must not implement actual NFC, OCR, face, liveness, or fingerprint engine behavior.
- TIP-03 must not decide production assurance level or legal certification semantics.

### 3.6 SignFlow Dependency Boundary

Proposed decision:

- TagEkyc may keep TagEkyc-owned SignFlow-oriented placeholder contracts/helpers for transaction-bound profile support.
- TagEkyc must not depend on SignFlow source code, database, runtime packages, internal models, or internal service behavior.
- SignFlow remains a consuming profile/contract boundary, not a platform dependency.

Implementation boundary:

- TIP-03 must not add project/package references to any external SignFlow runtime or internal repository.
- Architecture tests should continue to guard this boundary where practical.

### 3.7 Production Database And Migration Boundary

Proposed decision:

- TIP-03 must not choose production database technology unless explicitly approved.
- TIP-03 must not add production migrations or production schema.
- TIP-03 may define repository ports.
- TIP-03 may use in-memory/test-only persistence if needed for unit tests or contract tests.
- TIP-03 must not add EF providers, `DbContext`, migrations, schema/model mapping, production-like persistence adapters, or storage infrastructure unless explicitly approved.
- Evidence results and audit events must be modeled as append-only records.
- Repository ports should not expose update/delete methods for evidence results or audit events.

Implementation boundary:

- Any EF Core provider, `DbContext`, production SQL choice, schema/model mapping, migration, durable local store, or production deployment infrastructure requires STOP+ASK.
- Any in-memory/test-only persistence must be clearly labeled as S1 development/test-only and non-production.

### 3.8 Contract Namespace And Projection Boundary

Proposed decision:

TIP-03 contracts must be separated by caller/use-case projection rather than exposed as one flat public surface:

- `BusinessConsumer`: session creation inputs and sanitized result summaries for ordinary client applications.
- `CaptureAgent`: capture artifact metadata/reference submission shapes, without final result authority.
- `TrustedAdapter`: internal or explicitly trusted adapter result submission shapes, including `EvidenceResult` submission DTOs.
- `InternalAudit.Manifest`: internal evidence package, audit manifest, VaultRef, and manifest hash shapes.
- `SignFlowProfile`: TagEkyc-owned transaction-bound profile placeholders/helpers for SignFlow integration only.

Implementation boundary:

- A DTO in `BusinessConsumer` must not expose internal VaultRefs, trusted result submission authority, raw artifacts, raw biometric fields, or plaintext sensitive identity fields.
- `TrustedAdapter` DTOs must not be reused as default business-client request DTOs.
- `InternalAudit.Manifest` DTOs must not be returned by default business-consumer APIs.
- `SignFlowProfile` contracts must remain TagEkyc-owned consumer-profile placeholders and must not import SignFlow runtime code or internal models.

### 3.9 API Key Model Boundary

Proposed decision:

- TIP-03 may model client application/API key metadata and policy inputs needed by the domain, such as key id, display name, owning client application, allowed profile types, allowed checks, status, and audit metadata.
- TIP-03 must treat API key material as outside runtime implementation scope.

Implementation boundary:

- TIP-03 must not implement production API key secret storage.
- TIP-03 must not implement secret hashing, rotation, verification, middleware, request authentication, or external secret manager integration.
- Any key-like sample values in tests or docs must be fake placeholders and must not imply a production key lifecycle.

### 3.10 S1 Signature Placeholder Boundary

Proposed decision:

- Any S1 signature-related fields are `PLACEHOLDER_UNVERIFIED`.
- `payloadSignature`, `webhookSignature`, `evidencePackageSignature`, signature timestamps, signature algorithms, key ids, and related signature metadata are non-authoritative in TIP-03.

Implementation boundary:

- TIP-03 must not claim authenticity, replay protection, legal audit reliance, non-repudiation, or production verification guarantees from S1 signature fields.
- TIP-03 must not implement production payload signing, webhook signing, replay cache, evidence package signing, key management, or cryptographic verification.
- Contracts and tests should make placeholder status visible enough to prevent accidental downstream reliance.

## 4. TIP-03 Implementation Boundaries

Allowed after kickoff acceptance:

- Domain models/enums for client applications, API keys, verification sessions, required checks, capture artifacts, evidence results, verification results, evidence packages, audit events, webhook subscriptions, and webhook deliveries.
- Separated DTO contract projections aligned to LLD03 S1 shapes and caller boundaries.
- Repository interfaces and application service ports.
- In-memory/test-only persistence helpers if needed for tests and explicitly kept non-production.
- Unit/contract/architecture tests for model invariants and data exposure boundaries.

Not allowed in TIP-03 without explicit approval:

- LLD03 runtime API implementation.
- API key middleware or runtime auth behavior.
- Production API key secret storage, hashing, rotation, verification, or external secret manager integration.
- Real adapter behavior or mock engine result generation.
- Vault runtime, object storage, file storage, raw artifact handling, retention, deletion, or legal hold.
- Webhook dispatcher/runtime behavior.
- Production cryptography or production signature/replay implementation.
- Production database technology choice.
- EF providers, `DbContext`, schema/model mapping, durable local stores, or production-like persistence adapters.
- Production migrations or schema.
- SignFlow source/database/runtime package/internal model dependency.
- Production-certified eKYC claims.

## 5. STOP+ASK Gates

STOP+ASK before doing any of the following:

- Allowing ordinary business clients to submit `EvidenceResult`, `PASSED`, `FAILED`, or final verification outcomes.
- Reusing `TrustedAdapter` result submission DTOs as business-client request DTOs.
- Adding raw artifacts, raw artifact paths, base64 payloads, raw biometric fields, raw document/NFC fields, or plaintext sensitive identity fields to default business payloads.
- Exposing internal VaultRefs in default business-consumer responses.
- Changing SignFlow required checks from `CAPTURE_QUALITY`, `DOCUMENT_NFC`, `FACE_MATCH`, and `LIVENESS`.
- Making `FINGERPRINT` mandatory.
- Making `DOCUMENT_OCR` mandatory.
- Collapsing session lifecycle state and verification result into one enum.
- Adding SignFlow source code, database, runtime packages, internal models, or internal service dependencies.
- Choosing production database technology.
- Adding EF providers, `DbContext`, schema/model mapping, durable local stores, migrations, or production schema.
- Implementing API key secret storage, hashing, rotation, middleware, request authentication, or external secret manager integration.
- Adding production cryptography, production webhook signing, production replay protection, or external audit reliance claims.
- Treating `PLACEHOLDER_UNVERIFIED` signature fields as authoritative for authenticity, replay protection, non-repudiation, or audit reliance.
- Changing Product Brief, HLD, LLD, S1 scope, or legal/compliance/certification posture.

## 6. Pre-Start Checklist

Before TIP-03 implementation starts, confirm:

- [ ] TIP-02A confirmation report is accepted.
- [ ] `.gitignore` is included in the intended source set.
- [ ] No generated `bin/`, `obj/`, or `TestResults/` outputs are tracked.
- [ ] Adapter trust boundary is accepted.
- [ ] Capture artifact upload/reference model is accepted.
- [ ] VaultRef exposure boundary is accepted.
- [ ] Session lifecycle state and verification result separation is accepted.
- [ ] SignFlow S1 required checks and optional OCR/fingerprint policy are accepted.
- [ ] SignFlow dependency boundary is accepted.
- [ ] Production database and migration boundary is accepted.
- [ ] Development/test persistence is narrowed to in-memory/test-only unless explicitly approved.
- [ ] `EvidenceResult` submission DTOs are accepted as trusted-adapter/internal contract shapes only.
- [ ] Contract namespaces/projections are separated across `BusinessConsumer`, `CaptureAgent`, `TrustedAdapter`, `InternalAudit.Manifest`, and `SignFlowProfile`.
- [ ] API key model is accepted as metadata/policy only, with no production secret lifecycle or runtime auth implementation.
- [ ] S1 signature-related fields are marked `PLACEHOLDER_UNVERIFIED` and non-authoritative.

## 7. Review Decision Needed

Review decision:

```text
TIP-03 Kickoff v0.2 accepted.
TIP-03 implementation is authorized within the documented boundaries.
```

TIP-03 implementation must remain inside the v0.2 boundaries and STOP+ASK gates above.
