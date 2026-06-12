# TIP-11 Option B Kickoff Draft v0.1

**File:** `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_kickoff_option_b_v0_1.md`
**Version:** 0.4
**Status:** Accepted - implementation dispatch authorized separately
**Date:** 2026-06-12
**Baseline:** `87d277b7e6911c0a455e570ec526437c966a6c1f`
**Purpose:** Drafts a future implementation kickoff for TIP-11 Option B, limited to domain/application metadata boundary preparation without DB provider, migrations, raw artifact storage, webhook/outbox/retry, production crypto, vendor selection, pilot readiness, production readiness, or SignFlow runtime dependency.

## Changelog

### v0.4 - Kickoff accepted

- Recorded GPT Gate acceptance of TIP-11 Option B kickoff v0.3.
- Confirmed public contract/API/BusinessConsumer escape hatches are resolved.
- Confirmed `PolicySnapshotId` is internal domain/application metadata only and excluded from package, manifest, hash, completion, package summary, notification, and public JSON surfaces.
- Confirmed API/serialization impact triggers STOP/RRI.
- Preserved implementation as a separate next step under the accepted Option B dispatch allowlist.

### v0.3 - Public contract and package/hash blocker patch

- Removed public-contract escape hatches from STOP/RRI, test expectations, route wording, and dispatch allowlist wording.
- Reconfirmed that public DTO fields, BusinessConsumer metadata exposure, API request/response JSON changes, route behavior changes, status code changes, authorization/ownership/idempotency changes, completion semantics changes, package hash semantics changes, and completion notification payload changes are out of scope for Option B.
- Replaced policy snapshot package-construction wording with an internal metadata-only boundary that excludes evidence package manifest inputs, `PackageHash`, `ManifestHash`, completion response DTO, evidence package summary DTO, completion notification payload, and public contract JSON.
- Reran self-review A/B with no blocker findings after the patch.

### v0.2 - Gate blocker patch

- Added Option B implementation pattern / allowed shape section.
- Pinned that Option B must not add public DTO fields, expose metadata in BusinessConsumer contracts, change API JSON, or change route/status/auth/ownership/idempotency/completion/package/notification semantics.
- Added five required conditions before metadata may attach to domain aggregates.
- Pinned LocalDev default `PolicySnapshotId` token as `LOCALDEV-S1-POLICY-V1`.
- Constrained `PurgeBlockReason` to enum/code-only; free text is out of scope.
- Reran self-review A/B with no blocker findings after the patch.

### v0.1 - Initial Option B kickoff draft

- Created a review-only kickoff draft for `Option B - Domain/application metadata boundary`.
- Defined metadata-only domain/application boundary concepts for durable-state compatibility while preserving LocalDev behavior.
- Added field-level boundary table, policy snapshot boundary, metadata-only retention/legal-hold boundary, STOP/RRI gates, future test expectations, future dispatch allowlist, and reviewer checklist.
- Recorded A/B self-review with no blocker findings.

## 1. Purpose

TIP-11 Option B prepares the smallest future implementation scope for domain/application metadata boundaries only.

The intended future implementation may introduce or adjust domain and application abstractions so TagEkyc can become durable-state-compatible later while preserving current LocalDev behavior. This kickoff draft is for review only and does not authorize implementation.

Option B must not introduce persistence infrastructure, legal enforcement, production behavior, provider selection, or operational delivery behavior. It should only make metadata concepts explicit enough that later durable persistence, vault lifecycle, policy versioning, evidence signing, and webhook/outbox planning can attach to stable domain/application boundaries.

## 2. Baseline Inputs

- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md`
- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/tips/tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/lld_04_engine_adapter_contracts_v0_1.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
- `docs/00_AGENT_COORDINATION_BUS.md`

Repo-real surfaces inspected during draft preparation:

- `src/TagEkyc.Domain/VerificationSession.cs`
- `src/TagEkyc.Domain/CaptureArtifact.cs`
- `src/TagEkyc.Domain/EvidenceResult.cs`
- `src/TagEkyc.Domain/EvidencePackage.cs`
- `src/TagEkyc.Domain/AuditEvent.cs`
- `src/TagEkyc.Domain/RequiredCheck.cs`
- `src/TagEkyc.Domain/RequiredCheckPolicy.cs`
- `src/TagEkyc.Domain/VaultRef.cs`
- `src/TagEkyc.Domain/HashRef.cs`
- `src/TagEkyc.Application/Ports/RepositoryPorts.cs`
- `src/TagEkyc.Application/LocalDev/LocalDevInMemoryRepositories.cs`
- `src/TagEkyc.Application/VerificationSessions/LocalDevClientPolicy.cs`
- Existing TIP-04 through TIP-08 unit, contract, and architecture tests.

## 3. Explicit Selected Option

Selected option:

```text
Option B - Domain/application metadata boundary
```

Option B only.

Option B means a future accepted implementation may add or adjust domain/application metadata concepts and in-memory-compatible service/repository abstractions. It must not add DB provider, EF, DbContext, migrations, schema, durable persistence adapter, local durable storage, vault provider, webhook/outbox/retry, production crypto, vendor selection, raw artifact storage, pilot readiness, production readiness, or SignFlow runtime dependency.

## 4. In Scope

A later accepted Option B implementation may:

- Add domain value objects, enums, or records for metadata-only concepts such as:
  - `PolicySnapshotId`;
  - required-check-set policy identity;
  - decision reproducibility boundary;
  - `RetentionClass`;
  - `DeletionEligibility`;
  - `LegalHoldStatus`;
  - `PurgeBlockReason`;
  - `AccessAuditRequired`;
  - actor references;
  - request/correlation metadata where currently missing.
- Add metadata fields to domain aggregates only when the five allowed-shape conditions in Section 7 are all met.
- Add application-layer policy snapshot abstractions that preserve existing LocalDev policy behavior.
- Add repository-port comments, interface refinements, or in-memory-compatible metadata contracts when no durable provider is introduced.
- Add tests proving:
  - metadata-only status;
  - LocalDev behavior remains stable;
  - no raw/internal data appears in BusinessConsumer DTOs;
  - no EF/DbContext/migrations/provider dependency appears;
  - no SignFlow runtime dependency appears.
- Update docs and execution report for the selected Option B implementation after dispatch.

## 5. Out of Scope / Non-Goals

This kickoff draft does not authorize any implementation.

Even after acceptance, Option B implementation must not include:

- DB provider selection or integration.
- EF, DbContext, migrations, schema, or durable adapter.
- Local durable storage implementation.
- Vault provider or vault lifecycle implementation.
- Raw artifact, biometric, NFC payload, fingerprint template, raw provider payload, device secret, or plaintext sensitive identity storage.
- Retention, deletion, or legal-hold enforcement.
- Webhook delivery, retry, outbox, subscription model, dispatcher, delivery ledger, or dead-letter behavior.
- Production crypto, evidence package signing, webhook signing, replay protection, HMAC, JWS, KMS, HSM, key rotation, or secret lifecycle.
- Database, vault, provider, biometric engine, NFC vendor, liveness provider, or legal/compliance vendor selection.
- Specialized evidence endpoints.
- Production auth/client trust implementation.
- Pilot readiness claim.
- Production readiness claim.
- SignFlow runtime, source, database, package, deployment, network, or internal-model dependency.

## 6. Proposed Domain/Application Surfaces

Candidate future domain surfaces:

- `PolicySnapshotId`: value object or simple metadata record that identifies the policy snapshot used by a session or final decision.
- `PolicyIdentity` or equivalent: metadata-only description of required-check set version and decision-policy identity.
- `RetentionClass`: enum or value object that classifies metadata/vault-reference retention category without enforcing retention.
- `DeletionEligibility`: enum that records metadata eligibility only; it must not purge or schedule deletion.
- `LegalHoldStatus`: enum that records hold metadata only; it must not enforce legal hold.
- `PurgeBlockReason`: enum/code-only metadata reason; free text is out of scope for Option B.
- `AccessAuditRequired`: boolean or enum indicating access should be audited; it must not implement audit access control.
- `ActorRef`: metadata reference for BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, or Admin without changing auth implementation.

Candidate future application surfaces:

- Extend LocalDev policy source to provide deterministic policy snapshot identity for current policies.
- Preserve current `LocalDevRuntimePolicySource` behavior and currently accepted RequiredChecks.
- Preserve current in-memory repositories; do not replace them with durable storage.
- Preserve public DTO behavior; Option B must not expose metadata through public DTOs or BusinessConsumer contracts.
- Keep BusinessConsumer responses sanitized and free of raw/internal fields.

## 7. Option B Implementation Pattern / Allowed Shape

Option B may introduce only:

- metadata value objects/enums;
- internal domain metadata concepts;
- internal application metadata carriers;
- tests proving existing behavior remains stable.

Option B must not:

- add public DTO fields;
- expose metadata in BusinessConsumer contracts;
- change API request JSON;
- change API response JSON;
- change route behavior;
- change status codes;
- change authorization behavior;
- change ownership behavior;
- change idempotency behavior;
- change completion semantics;
- change evidence package hash semantics;
- change completion notification payload semantics.

Metadata may attach to domain aggregates only when all five conditions are true:

1. A deterministic LocalDev default exists.
2. No public contract changes are required.
3. No behavior or enforcement changes are required.
4. No durable persistence assumption is introduced.
5. Tests prove existing S1 flows remain semantically unchanged.

If any metadata field cannot meet all five conditions, it must remain a standalone value object/type only or trigger STOP/RRI before implementation.

## 8. Field-Level Boundary Table

| Field / concept | Allowed location | Raw/sensitive risk | Implementation status | STOP/RRI condition |
| --- | --- | --- | --- | --- |
| `PolicySnapshotId` | Domain session/decision metadata; application policy source; tests. | Low if opaque id only. | Candidate Option B metadata with LocalDev default `LOCALDEV-S1-POLICY-V1`. | STOP if it requires new external policy store or production policy registry. |
| Required-check-set version | Domain/application policy metadata. | Low if version id only. | Candidate Option B metadata. | STOP if it changes existing RequiredChecks enforcement semantics. |
| Decision reproducibility boundary | Domain/application metadata and tests. | Medium if it implies audit/legal reliance. | Candidate Option B metadata. | STOP if it claims legal audit reliance or production certification. |
| `RetentionClass` | Domain metadata on artifact/evidence/package references. | Medium; can imply retention policy. | Candidate Option B metadata-only. | STOP if it enforces retention, deletion, timers, purge jobs, or legal policy. |
| `DeletionEligibility` | Domain metadata only. | Medium; can imply legal deletion behavior. | Candidate Option B metadata-only. | STOP if it deletes, schedules deletion, or defines final legal semantics. |
| `LegalHoldStatus` | Domain metadata only. | Medium; legal posture marker. | Candidate Option B metadata-only. | STOP if it enforces legal hold or changes deletion behavior. |
| `PurgeBlockReason` | Domain metadata only. | Low when constrained to enum/code. | Candidate Option B enum/code-only metadata; free text is out of scope. | STOP if implementation needs free text, legal narrative, raw PII, or secrets. |
| `AccessAuditRequired` | Domain metadata; application access policy hints. | Low if boolean/enum only. | Candidate Option B metadata-only. | STOP if it implements new access control or operator audit workflow. |
| `VaultRef` | Domain/internal manifest metadata; never default BusinessConsumer output. | High if it exposes raw object location. | Existing concept; may be boundary-hardened. | STOP if exposed to BusinessConsumer default DTOs or used to implement raw storage. |
| `ArtifactHash` | Capture artifact metadata; sanitized package refs where already allowed. | Low if hash only. | Existing concept; may be boundary-hardened. | STOP if used as raw artifact substitute or leaks sensitive payload. |
| `PayloadHash` | TrustedAdapter evidence/internal manifest metadata. | Medium if exposed publicly. | Existing concept; internal boundary only. | STOP if exposed to default BusinessConsumer DTOs. |
| Actor references | Domain/application metadata and audit events. | Medium if it reveals device/provider details. | Candidate Option B metadata. | STOP if it creates production trust/onboarding implementation. |
| `RequestId` | Existing request/session/artifact/evidence/package metadata. | Low if caller-safe correlation id. | Existing concept; may be made more consistent internally only. | STOP if it changes API error precedence, JSON, or idempotency semantics. |
| `CorrelationId` | Existing request/session/artifact/evidence/package metadata. | Low if caller-safe correlation id. | Existing concept; may be made more consistent. | STOP if it becomes cross-client lookup authority. |
| Timestamps | Domain/application metadata. | Low. | Existing concept; may be normalized. | STOP if it introduces scheduling, retention timers, retry timers, or purge timers. |
| Plaintext identity fields | Not allowed in Option B. | High. | Out of scope. | STOP if any implementation requires storing plaintext identity. |
| Raw biometric/artifact data | Not allowed. | Critical. | Out of scope. | STOP immediately. |
| Provider secrets/device secrets | Not allowed. | Critical. | Out of scope. | STOP immediately. |

## 9. Policy Snapshot Boundary

Option B should make policy identity explicit without changing policy behavior.

Future implementation target:

- Every created session can carry an opaque policy snapshot identity.
- `PolicySnapshotId` is internal domain/application metadata only in Option B.
- `PolicySnapshotId` must not be included in evidence package manifest inputs, `PackageHash`, `ManifestHash`, completion response DTO, evidence package summary DTO, completion notification payload, or public contract JSON.
- Any change to package, manifest, hash, or public payload semantics requires a separate reviewed TIP and is not approvable inside this Option B kickoff.
- LocalDev policies should use this deterministic default policy snapshot token:

```text
LOCALDEV-S1-POLICY-V1
```

- Any implementation that cannot use `LOCALDEV-S1-POLICY-V1` as the deterministic LocalDev default must STOP/RRI before implementation.
- RequiredChecks continue to be validated exactly as today. Any RequiredChecks behavior change requires a separate reviewed TIP and is not approvable inside this Option B kickoff.
- Fingerprint remains optional/demo/deferred and not part of the default SignFlow S1 RequiredChecks.

Option B must not:

- create a production policy registry;
- introduce external policy service dependency;
- change SignFlow S1 RequiredChecks;
- change allowed check enforcement;
- claim decision reproducibility is legally sufficient;
- expose policy internals that leak raw/provider data.

## 10. Retention / Deletion / Legal-Hold Boundary

Option B may define metadata markers only.

Allowed:

- enum/value-object definitions;
- deterministic LocalDev default values;
- tests proving markers do not enforce deletion or legal hold;
- documentation that legal/compliance acceptance is still required.
- `PurgeBlockReason` enum/code values only.

Disallowed:

- purge jobs;
- retention timers;
- deletion APIs;
- legal-hold APIs;
- legal policy semantics;
- raw vault object lifecycle;
- any claim that S2 satisfies retention or legal hold obligations.
- free-text `PurgeBlockReason`.

Default semantic posture for metadata:

- `RetentionClass`: classification only.
- `DeletionEligibility`: informational only.
- `LegalHoldStatus`: marker only.
- `PurgeBlockReason`: enum/code-only metadata.
- `AccessAuditRequired`: access-audit hint only, not authorization enforcement.

## 11. LocalDev Behavior Preservation

Future Option B implementation must preserve current S1 LocalDev behavior:

- Existing public routes remain unchanged. Any route change requires a separate reviewed TIP/kickoff and is not approvable inside this Option B kickoff.
- Existing DTO JSON shape remains unchanged.
- No public DTO fields are added.
- No metadata is exposed in BusinessConsumer contracts.
- Existing `LocalDevInMemoryRepositories` remain in-memory.
- Existing API key LocalDev auth remains LocalDev/non-production.
- Existing completion, evidence package, and notification placeholder semantics remain non-authoritative.
- Existing tests should continue to pass.
- No new production readiness, pilot readiness, legal reliance, durable persistence, signing, webhook, or provider readiness claim is introduced.

If any metadata addition forces a behavior change to create/session/evidence/complete/package/notification flows, STOP/RRI before implementation.

## 12. STOP/RRI Questions

Before starting the separate implementation step, confirm:

| Gate | Question | Default recommendation |
| --- | --- | --- |
| Implementation authorization | Has GPT/Homeowner accepted this kickoff for implementation? | Yes, accepted at v0.3; implementation must be a separate step and must stay inside the accepted Option B dispatch allowlist. |
| Metadata vs enforcement | Is each new concept metadata-only? | STOP if unclear. |
| Allowed shape | Can every domain-attached metadata field satisfy all five Section 7 conditions? | STOP if any condition fails. |
| DB/provider/migration | Does the implementation require choosing storage infrastructure? | STOP; out of Option B. |
| Local durable adapter | Does the implementation persist across process restart? | STOP; out of Option B. |
| Raw evidence | Does any field require raw artifact, biometric, NFC payload, fingerprint template, raw provider payload, or plaintext sensitive identity? | STOP immediately. |
| Vault lifecycle | Does it implement vault provider, retention, deletion, legal hold, or object lifecycle? | STOP; out of Option B. |
| Webhook | Does it touch webhook/outbox/retry/subscription/dispatcher/delivery ledger? | STOP; out of Option B. |
| Crypto | Does it require production signing, replay protection, HMAC/JWS/KMS/HSM/secret lifecycle? | STOP; out of Option B. |
| Vendor/provider | Does it select a database, vault, biometric, NFC, liveness, or legal/compliance provider? | STOP; out of Option B. |
| Public contract | Does it expose metadata in BusinessConsumer DTOs? | STOP; out of Option B. Public DTO fields, BusinessConsumer metadata exposure, API request/response JSON changes, route behavior changes, status code changes, authorization/ownership/idempotency changes, completion semantics changes, package hash semantics changes, and completion notification payload changes require a separate reviewed TIP/kickoff and are not approvable inside this Option B kickoff. |
| API behavior | Does it change route behavior, status codes, authorization, ownership, idempotency, completion, package hash, or notification payload semantics? | STOP; out of Option B. |
| `PolicySnapshotId` | Can LocalDev use `LOCALDEV-S1-POLICY-V1` as deterministic default? | STOP if not. |
| `PurgeBlockReason` | Does it require free text? | STOP; free text is out of scope. |
| SignFlow | Does it add SignFlow runtime/source/database/network/package dependency? | STOP; prohibited. |

## 13. Acceptance Criteria

This kickoff is accepted for Option B implementation dispatch when:

- It selects Option B only.
- It remains a draft and does not authorize implementation.
- It defines deterministic in-scope and out-of-scope boundaries.
- It includes field-level metadata boundaries and STOP/RRI triggers.
- It keeps retention/deletion/legal-hold as metadata-only.
- It pins `LOCALDEV-S1-POLICY-V1` as LocalDev default `PolicySnapshotId`.
- It constrains `PurgeBlockReason` to enum/code-only.
- It prohibits public DTO/API JSON and behavior changes.
- It preserves current LocalDev behavior.
- It defines later implementation test expectations.
- It defines later implementation dispatch allowlist.
- A/B self-review finds no blocker findings.

GPT Gate accepted this kickoff at v0.3 after the v0.3 blocker patch. Implementation remains a separate next step and must stay inside the accepted Option B dispatch allowlist.

## 14. Test Expectations for Later Implementation

During the separate Option B implementation step, tests should include:

- Domain tests for new metadata value object/enum validation and safe defaults.
- Application tests proving LocalDev policy snapshot identity is deterministically `LOCALDEV-S1-POLICY-V1` and does not change existing RequiredChecks behavior.
- Application tests proving session creation, evidence recording, completion, package read, and completion notification behavior remain semantically unchanged except accepted internal metadata.
- If an implementation path could affect public contract serialization or API JSON, STOP/RRI before editing. Otherwise, tests or existing contract coverage must prove BusinessConsumer contract and JSON shape remain unchanged.
- Contract tests proving BusinessConsumer DTOs still exclude raw artifacts, VaultRefs, PayloadHash, plaintext identity, biometric data, provider internals, and retention/legal-hold internals.
- Architecture tests proving no EF, DbContext, migrations, database provider, durable adapter, webhook runtime, production crypto, or SignFlow runtime dependency was added.
- Regression tests proving placeholder signature statuses remain non-authoritative.
- Boundary tests proving retention/legal-hold fields are metadata-only and do not schedule deletion, block deletion, purge data, or enforce legal workflow.

Do not add endpoint/API pipeline tests for Option B because route changes are out of scope. Any route change requires a separate reviewed TIP/kickoff and is not approvable inside this Option B kickoff.

## 15. Dispatch Allowlist for Later Implementation

Accepted Option B implementation may be limited to:

- `src/TagEkyc.Domain/**`
- `src/TagEkyc.Application/**`
- `tests/TagEkyc.UnitTests/**`
- `tests/TagEkyc.ContractTests/**`
- `tests/TagEkyc.ArchTests/**`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/**`
- `docs/tips/README.md`
- `docs/00_AGENT_COORDINATION_BUS.md`

Not allowed for Option B implementation. If any of these paths or scopes are needed, STOP/RRI and open a separate reviewed TIP/kickoff outside Option B:

- `src/TagEkyc.Api/**`
- `src/TagEkyc.Infrastructure/**`
- `src/TagEkyc.Adapters/**`
- `src/TagEkyc.SignFlow/**`
- any migration folder;
- any database/provider package reference;
- deployment, Docker, infrastructure, secrets, or external service configuration.

If implementation needs to touch a non-allowlisted path, STOP/RRI before editing.

## 16. Reviewer Checklist

Reviewers should verify:

- Selected option is unambiguously Option B only.
- The draft cannot be read as implementation authorization.
- In-scope changes are metadata/domain/application only.
- Out-of-scope list blocks DB/provider/migrations and local durable adapters.
- Raw artifact/biometric/vault lifecycle boundaries are preserved.
- Retention/deletion/legal-hold is metadata-only.
- Webhook/outbox/retry remains deferred.
- Production crypto/signing/replay remains deferred.
- Vendor/provider selection remains deferred.
- Pilot and production readiness are not claimed.
- SignFlow remains an external consumer profile only.
- Dispatch allowlist does not accidentally include infrastructure, API route, adapter, or SignFlow runtime work.
- Test expectations are sufficient for a future builder.

## 17. Implementation Authorization Statement

This kickoff is accepted, but implementation must be performed as a separate step and must remain inside the accepted Option B scope. During implementation:

- do not edit `src/**`;
- do not edit `tests/**`;
- do not add DB/provider/migrations;
- do not add local durable storage;
- do not add vault lifecycle;
- do not add webhook/outbox/retry;
- do not add production crypto;
- do not select vendors/providers;
- do not store raw artifacts or biometrics;
- do not claim pilot or production readiness;
- do not add SignFlow runtime dependency.

## 18. Self-Review Record

### Self-Review A - Boundary / Scope Review

Verdict: No blocker findings.

Coverage:

- DB/provider/migrations: not authorized; excluded from Option B.
- Local durable adapter: not authorized; excluded from Option B.
- Raw artifact/biometric storage: prohibited with STOP/RRI.
- Vault implementation: prohibited with STOP/RRI.
- Retention/deletion/legal-hold enforcement: prohibited; metadata-only.
- Webhook/outbox/retry: prohibited with STOP/RRI.
- Production crypto/signing/replay: prohibited with STOP/RRI.
- Vendor/provider selection: prohibited with STOP/RRI.
- Pilot/production readiness: not claimed.
- SignFlow runtime dependency: prohibited; SignFlow remains external consumer profile only.

### Self-Review B - Dispatch Readiness Review

Verdict: No blocker findings.

Coverage:

- Selected option is explicit: Option B only.
- In-scope/out-of-scope boundaries are deterministic.
- Field-level metadata boundary table lists allowed location, raw/sensitive risk, implementation status, and STOP/RRI condition.
- `PolicySnapshotId` uses `LOCALDEV-S1-POLICY-V1` as deterministic LocalDev default.
- `PurgeBlockReason` is enum/code-only and free text is out of scope.
- Retention/legal-hold markers are scoped as metadata-only.
- Current LocalDev behavior preservation is explicit.
- Public DTO fields, BusinessConsumer contract exposure, API JSON, route behavior, status codes, authorization, ownership, idempotency, completion semantics, package hash semantics, and notification payload semantics remain unchanged.
- Test expectations cover domain/application, contract leakage, architecture boundaries, and metadata-only retention/legal-hold behavior.
- Dispatch allowlist is explicit and excludes API, Infrastructure, Adapters, SignFlow, migrations, DB/provider packages, deployment, and external service config by default.
- STOP/RRI gates cover ambiguous metadata-vs-enforcement cases.

### v0.2 Self-Review A - Boundary / Scope Review

Verdict: No blocker findings.

Patch coverage:

- Option B allowed shape now explicitly blocks public DTO fields, BusinessConsumer metadata exposure, API JSON changes, route behavior changes, status code changes, authorization/ownership/idempotency changes, completion semantics changes, package hash changes, and notification payload changes.
- Domain aggregate metadata attachment is gated by five deterministic conditions.
- `LOCALDEV-S1-POLICY-V1` is pinned as LocalDev default `PolicySnapshotId`.
- `PurgeBlockReason` is constrained to enum/code-only; free text is out of scope.
- No DB/provider/migration, local durable adapter, raw storage, vault lifecycle, retention enforcement, webhook/outbox/retry, production crypto, vendor/provider selection, pilot/production readiness, or SignFlow runtime dependency is authorized.

### v0.2 Self-Review B - Dispatch Readiness Review

Verdict: No blocker findings.

Patch coverage:

- Future builder can determine the only allowed implementation shape: metadata value objects/enums and internal application carriers.
- The five conditions decide whether metadata may attach to domain aggregates; otherwise it remains standalone or triggers STOP/RRI.
- LocalDev `PolicySnapshotId` default is deterministic.
- `PurgeBlockReason` representation is deterministic and non-free-text.
- Test expectations now include semantic stability and JSON stability where relevant.
- STOP/RRI gates cover any failure to preserve public contracts or existing S1 behavior.

### v0.3 Self-Review A - Boundary / Scope Review

Verdict: No blocker findings.

Patch coverage:

- Searched for remaining public-contract escape hatches after the patch.
- No wording remains that allows public DTO fields, BusinessConsumer metadata exposure, API request/response JSON changes, route behavior changes, status code changes, authorization/ownership/idempotency changes, completion semantics changes, package hash semantics changes, or completion notification payload changes inside Option B.
- Remaining references to separate reviewed TIP/kickoff are STOP/RRI exits outside Option B, not approval paths inside Option B.
- `PolicySnapshotId` is internal domain/application metadata only and is excluded from package manifest inputs, `PackageHash`, `ManifestHash`, completion response DTO, evidence package summary DTO, completion notification payload, and public contract JSON.

### v0.3 Self-Review B - Dispatch Readiness Review

Verdict: No blocker findings.

Patch coverage:

- Package/hash/manifest/notification semantics are explicitly unchanged for Option B.
- Any implementation path that could affect public contract serialization or API JSON must STOP/RRI before editing.
- Tests or existing contract coverage must prove BusinessConsumer contract and JSON shape remain unchanged.
- Public route/API behavior changes are out of scope and require a separate reviewed TIP/kickoff outside Option B.
