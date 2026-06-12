# TIP-08 SignFlow Transaction-Bound Profile and S1 End-to-End Proof Planning Brief v0.3

**File:** `docs/tips/tip_08_signflow_transaction_bound_profile/tip_08_planning_brief_v0_3.md`
**Version:** 0.3
**Status:** Closed - accepted planning record implemented by Kickoff v0.4
**Date:** 2026-06-11
**Baseline:** `05a8e9faf628e28e740d287d2e9afc76667f4836`
**Purpose:** Define a narrow LocalDev proof for the transaction-bound eKYC profile intended for SignFlow consumption, without adding any SignFlow runtime, source, or database dependency.

## Changelog

### Post-implementation closeout - 2026-06-12

- Recorded that the accepted planning record was implemented by Kickoff v0.4 and committed at `282eb821b7500f2965b336a5e67467bffc68adf4`.
- Recorded post-commit validation: 64 passed, 0 failed, 0 skipped.
- Confirmed implementation stayed test/proof-only with no `src/**`, DTO/contract, endpoint/query/service/runtime projection, or SignFlow runtime/source/database/network changes.
- Cross-referenced the proof evidence-source lesson now preserved as `L-TAG-Proof-01` in `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`.

### v0.3 - Kickoff alignment after pre-dispatch sentinel

- Aligned representative negative-test guidance with Kickoff v0.3: prove canonical SignFlow S1 RequiredChecks without assuming a new rejection policy for allowed-but-non-S1 checks.
- Preserved the Planning Brief as the accepted planning record for Kickoff v0.3 dispatch.

### v0.2 - Targeted review patches

- Pinned the default SignFlow-facing summary path to test/proof-level composition from existing outputs and existing `SignFlowProfile` contracts only.
- Required builders to re-verify Section 7 repo-real behavior with source/test evidence and exact file/line anchors before implementation.
- Narrowed negative-test guidance to representative TIP-08 proof cases instead of duplicating the full TIP-04 through TIP-07 negative matrix.

### Acceptance status - 2026-06-12

- Recorded that TIP-08 Planning Brief v0.2 was accepted for kickoff preparation and that Kickoff v0.3 was later accepted for implementation dispatch subject to pre-dispatch A/B hygiene.
- Updated to Planning Brief v0.3 after the final pre-dispatch sentinel found residual v0.2 wording that could imply a new RequiredChecks rejection policy.

### v0.1 - Initial planning brief

- Opened TIP-08 planning for the SignFlow transaction-bound profile and S1 end-to-end LocalDev proof.

## 1. Current Baseline and Commit Anchors

Repository baseline at TIP-08 planning open:

- `05a8e9faf628e28e740d287d2e9afc76667f4836` / `05a8e9f` - `docs: sync TIP-07 roadmap and index state`.
- `fe26c73cae02fca7999132a9117661ee5e82079b` / `fe26c73` - `docs: close TIP-07 governance state`.
- `916dd2918c2ab47ab0658ebf271fae45e22fb3ca` / `916dd29` - `feat: add TIP-07 completion notification projection`.
- `761ad98e83f63e4a6414fa2fa9704e258303758c` / `761ad98` - `docs: codify TIP-06 review stop rules`.

Preflight at TIP-08 planning open:

- `git status --short`: clean.
- `git log --oneline -8`: matched expected baseline sequence through TIP-07 roadmap/index synchronization.
- `dotnet test TagEkyc.sln --no-restore`: passed; `TagEkyc.ContractTests` 9 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 38 passed, total 63 passed and 0 failed.

TIP-07 leaves these relevant anchors for TIP-08:

- Completed session, evidence package, package hash, manifest hash, and completion notification projection are implemented.
- `VerificationCompletionApplicationService.GetCompletionNotificationAsync` returns deterministic LocalDev notification semantics only.
- No public webhook route, dispatcher/subscription runtime, retry scheduler, outbox, EF/DbContext/migration, durable persistence, or SignFlow runtime dependency was added.

## 2. TIP-08 Goal

TIP-08 should prove a narrow S1 end-to-end LocalDev flow for `TransactionBoundEkycProfile` intended for SignFlow consumption.

The goal is a contract/profile proof, not real SignFlow integration. A successful implementation should show that TagEkyc can create a transaction-bound session, preserve SignFlow binding inputs, record capture and TrustedAdapter evidence, complete the session, produce the evidence package/hash/manifest, produce the TIP-07 completion notification projection, and expose or compose a SignFlow-facing binding/result summary using existing TagEkyc-owned contracts/placeholders.

TIP-08 must not add SignFlow runtime, source, database, network, or operational dependency.

## 3. S1 End-to-End Proof Target

The LocalDev proof should exercise this single happy-path shape:

1. Create a `TransactionBoundEkycProfile` session for `SIGNING_AUTH`.
2. Submit and preserve:
   - `externalSessionId`.
   - `externalTransactionId`.
   - `bindingNonceHash`.
3. Use the SignFlow S1 required checks:
   - `CaptureQuality`.
   - `DocumentNfc`.
   - `FaceMatch`.
   - `Liveness`.
4. Append at least one capture artifact needed by the proof and any additional artifacts needed to satisfy the required evidence path.
5. Record TrustedAdapter evidence for each required check.
6. Complete the session.
7. Read the evidence package summary and assert package/hash/manifest linkage.
8. Read the completion notification projection and assert deterministic LocalDev placeholder semantics.
9. Produce or read a SignFlow-facing result/binding summary from existing TagEkyc-owned contracts/placeholders.
10. Assert no dependency on SignFlow runtime/source/database beyond existing `TagEkyc.SignFlow` and `TagEkyc.Contracts.SignFlowProfile` placeholders/contracts.

The proof must remain narrow. It should not become a full signing lifecycle, callback, durable persistence, webhook delivery, or production assurance test.

## 4. In-Scope

TIP-08 may include:

- A focused LocalDev application-level E2E proof test that chains existing application services:
  - session creation,
  - capture artifact append,
  - TrustedAdapter evidence append,
  - completion,
  - evidence package read,
  - completion notification projection read,
  - SignFlow-facing binding/result projection or composition.
- Validation that `TransactionBoundEkycProfile` creation preserves `externalSessionId`, `externalTransactionId`, and `bindingNonceHash`.
- Validation that SignFlow S1 required checks remain contract/profile-only and match the existing domain and contracts lists.
- Test/proof-level composition of the SignFlow-facing binding/result summary from existing create/session/package/notification outputs and existing `SignFlowProfile` contracts.
- Focused tests proving sanitized fields, deterministic hashes/placeholders, client ownership boundaries, and no raw/internal leakage.
- Architecture or boundary tests asserting no new SignFlow runtime/source/database dependency.

## 5. Out-of-Scope

TIP-08 must not implement:

- Real SignFlow API integration.
- Importing SignFlow internals, source, database, packages, deployment, or runtime workflows.
- Production signing authorization.
- Real transaction signing lifecycle.
- Webhook delivery, retry, subscription, dispatcher, or outbox behavior.
- EF, DbContext, migrations, database schema, or durable persistence.
- Production identity assurance claims.
- Production cryptographic signing, legal non-repudiation, HSM/KMS, key rotation, or replay protection.
- New production storage, vault, retrieval, or raw artifact storage.
- New external network calls.
- New public endpoint unless an accepted planning patch proves existing APIs/application services cannot demonstrate the TIP-08 proof.

## 6. Existing Repo Surfaces to Inspect Before Implementation

Implementation must inspect and cite current behavior from these surfaces before editing code:

- `src/TagEkyc.Domain/VerificationSession.cs` for `TransactionBoundEkycProfile` creation invariants.
- `src/TagEkyc.Domain/RequiredCheckPolicy.cs` for `SignFlowS1RequiredChecks`.
- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs` for `SignFlowS1RequiredChecks`, `SigningAuthorizationBindingDto`, and `SignFlowVerificationResultDto`.
- `src/TagEkyc.SignFlow/SigningAuthorizationBindingPlaceholder.cs` for the existing SignFlow placeholder boundary.
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` for session, completion, package, and notification DTOs.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs` for create-session behavior and summary reads.
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` for capture artifact and TrustedAdapter evidence writes.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` for completion, evidence package reads, and completion notification reads.
- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs` for LocalDev client policy and allowed transaction-bound behavior.
- Existing TIP-04 through TIP-07 tests for expected error/status and boundary behavior.
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`, especially the `761ad98` review stop rules.

## 7. TransactionBoundEkycProfile Behavior and RequiredChecks

Current repo behavior observed during planning:

- Domain creation requires a non-empty `externalTransactionId` and a non-null `bindingNonceHash` when `profile == TransactionBoundEkycProfile`.
- Application creation rejects missing transaction binding fields with `MISSING_TRANSACTION_BINDING` / `400`.
- Application creation rejects non-`sha256:` binding nonce hashes with `INVALID_BINDING_NONCE_HASH` / `400`.
- Application creation rejects transaction-bound profile use when the authenticated client policy disallows it with `TRANSACTION_BOUND_NOT_ALLOWED` / `403`.
- `externalSessionId` duplicates are rejected per authenticated client in LocalDev storage with `DUPLICATE_EXTERNAL_SESSION` / `409`.
- Current domain `RequiredCheckPolicy.SignFlowS1RequiredChecks` contains `CaptureQuality`, `DocumentNfc`, `FaceMatch`, and `Liveness`.
- Current contract `SignFlowS1RequiredChecks.Values` mirrors the same set using DTO values.

Before implementation, the builder must re-verify these repo-real behaviors with source/test evidence and report exact file/line anchors. If any observed behavior differs, STOP+ASK before adapting the proof, changing profile behavior, or changing RequiredChecks.

## 8. SignFlow-Facing Contract/Projection Options

Current repo already has TagEkyc-owned SignFlow-facing contracts/placeholders:

- `SigningAuthorizationBindingDto` with `ExternalSessionId`, `ExternalTransactionId`, `BindingNonceHash`, `EvidencePackageId`, and `EvidencePackageHash`.
- `SignFlowVerificationResultDto` with `VerificationSessionSummaryDto Session` and `SigningAuthorizationBindingDto Binding`.
- `SigningAuthorizationBindingPlaceholder` in `TagEkyc.SignFlow`, referencing only contracts.

Default TIP-08 path:

- Compose the SignFlow-facing summary only at test/proof level from existing create/session/package/notification outputs and existing `SignFlowVerificationResultDto` / `SigningAuthorizationBindingDto` contracts.
- Do not add a new application query, public endpoint, or runtime projection by default.
- If implementation proves existing outputs cannot compose the summary, STOP+ASK before adding any new service surface, runtime projection, public endpoint, or DTO field.
- Do not create a new SignFlow-facing DTO in TIP-08 unless reviewers first confirm the existing contract is insufficient.

Known planning constraint:

- `VerificationSessionSummaryDto` currently exposes `ExternalSessionId` but not `ExternalTransactionId` or `BindingNonceHash`.
- The binding DTO does include all three binding fields. If implementation cannot populate it by proof-level composition from existing outputs and allowed in-test state, STOP+ASK before adding a new service surface or changing public DTOs.

## 9. End-to-End Flow Shape

Default proof flow:

1. Create authenticated LocalDev business caller with transaction-bound profile allowed.
2. Call existing create-session service with:
   - `Profile = TransactionBoundEkycProfile`.
   - `Purpose = SIGNING_AUTH`.
   - `ExternalSessionId = sf_session_*`.
   - `ExternalTransactionId = sf_tx_*`.
   - `BindingNonceHash = sha256:*`.
   - required checks matching SignFlow S1.
3. Append capture artifacts using a CaptureAgent caller and existing capture artifact command path.
4. Append TrustedAdapter evidence for every required check using existing evidence result command path.
5. Complete the session using existing completion command path.
6. Read package summary using `GetEvidencePackageAsync`.
7. Read completion notification projection using `GetCompletionNotificationAsync`.
8. Produce or read the SignFlow-facing result/binding summary using existing contracts.
9. Assert binding fields and package fields match the original session input and completed output.
10. Assert no raw artifacts, VaultRefs, adapter internals, raw binding nonce, or SignFlow runtime data appear in client-facing projection JSON.

The proof may remain application-service level unless implementation introduces or changes runtime HTTP endpoints. If a runtime endpoint is added or changed, TIP-05/TIP-06 playbook rules require API pipeline coverage and a prior STOP+ASK.

## 10. Data-Boundary and Dependency Rules

TIP-08 must preserve these boundaries:

- TagEkyc remains independent from SignFlow code, database, deployment, runtime packages, and internal models.
- `TagEkyc.SignFlow` remains a TagEkyc-owned placeholder/contract assembly only.
- `bindingNonceHash` is stored and echoed only as a hash reference; raw binding nonce is never accepted, stored, or logged.
- Completion/package/SignFlow-facing projections must not expose raw artifacts, plaintext identity fields, internal VaultRefs, adapter internal payloads, SignFlow internals, or production signing authorization claims.
- Package hash, manifest hash, evidence refs, placeholder signature statuses, and LocalDev deterministic notification fields must remain non-authoritative S1 proof artifacts.
- No new external network calls are allowed.
- No new persistence infrastructure is allowed.

Boundary tests should scan project references and output JSON for dependency/data leakage where feasible.

## 11. Error/Status Behavior

TIP-08 should not redesign error semantics. It should reuse existing status conventions:

- `400` for malformed or missing transaction binding fields, invalid hash shape, invalid RequiredChecks shape, and missing input artifacts.
- `403` for authenticated policy/scope/profile/check/caller-category denials.
- `404` for session, package, or input artifact not found within the caller boundary.
- `409` for duplicate `externalSessionId` per authenticated client and already-finalized/write-conflict behavior where currently defined.

TIP-08 should not duplicate the full TIP-04 through TIP-07 negative test matrix. Existing endpoint/service negative behavior may be referenced rather than duplicated.

Representative negative proof cases:

- Missing `externalTransactionId` or `bindingNonceHash` is rejected.
- Non-`sha256:` `bindingNonceHash` is rejected.
- TIP-08 proves use of canonical SignFlow S1 RequiredChecks. Do not add a rejection test for allowed-but-non-S1 RequiredChecks unless repo-real behavior already enforces that rejection; otherwise STOP+ASK before adding or implying new policy behavior.
- Cross-client SignFlow-facing composition is tested only if a new application boundary is added after STOP+ASK approval.
- Pre-completion SignFlow-facing summary behavior is tested only if such a summary boundary is introduced after STOP+ASK approval.

If implementation needs a new error code/status for SignFlow-facing projection, STOP+ASK before adding it.

## 12. Test Plan

Minimum test additions for implementation:

- Unit/application E2E proof:
  - create transaction-bound session,
  - preserve `externalSessionId`, `externalTransactionId`, `bindingNonceHash`,
  - append capture artifact,
  - record TrustedAdapter evidence for all SignFlow S1 required checks,
  - complete session,
  - read evidence package/hash/manifest,
  - read completion notification projection,
  - compose/read SignFlow-facing result/binding summary.
- Contract test:
  - `SignFlowS1RequiredChecks.Values` equals domain `RequiredCheckPolicy.SignFlowS1RequiredChecks`.
  - `SigningAuthorizationBindingDto` / `SignFlowVerificationResultDto` exposes only expected binding/result fields.
- Boundary/architecture test:
  - no new project references from core TagEkyc runtime to SignFlow runtime/source/database.
  - `TagEkyc.SignFlow` remains contracts-only or placeholder-only according to the current dependency rule.
- Sanitization test:
  - SignFlow-facing summary and completion notification JSON exclude raw artifacts, VaultRefs, adapter internals, raw binding nonce, and SignFlow runtime data.
- Error tests:
   - missing/invalid binding fields,
   - canonical SignFlow S1 RequiredChecks are used without adding an allowed-but-non-S1 rejection policy unless repo-real behavior already enforces it,
   - cross-client SignFlow-facing access only if a new application boundary is added,
  - premature SignFlow-facing summary before completion only if such a summary boundary is introduced,
  - no endpoint added unless explicitly accepted.

If a runtime HTTP endpoint is added or changed after STOP+ASK approval, add API pipeline tests through the actual HTTP path for auth, scope, JSON binding, status codes, and response envelope behavior.

## 13. STOP+ASK Gates

STOP+ASK before:

- Adding a new SignFlow-facing DTO, public contract, or serialized field not already represented by `SignFlowProfileContracts`.
- Adding or changing a public endpoint.
- Using SignFlow source, database, runtime packages, internal models, deployment settings, or network calls.
- Importing any real SignFlow API client.
- Modifying `TransactionBoundEkycProfile` creation rules.
- Modifying `RequiredCheckPolicy.SignFlowS1RequiredChecks` or `SignFlowS1RequiredChecks.Values`.
- Changing existing error/status semantics.
- Adding webhook delivery, retry, subscription, dispatcher, durable outbox, or callback behavior.
- Adding EF, DbContext, migrations, durable persistence, production storage/vault/retrieval, or external services.
- Claiming production signing authorization, identity assurance certification, cryptographic non-repudiation, or legal audit reliance.
- Exposing raw binding nonce, raw artifacts, plaintext identity fields, internal VaultRefs, adapter internals, or SignFlow internals.
- Finding that existing contracts/placeholders are insufficient to produce a SignFlow-facing binding/result summary.

## 14. Review Protocol

Use the playbook rules committed at `761ad98e83f63e4a6414fa2fa9704e258303758c`.

Required review sequence:

1. First serious draft receives one full-system review with coverage attestation.
2. After patches, provide an affected-surface map and review only invalidated areas plus a lightweight blocker-only sentinel sweep.
3. Avoid endless full A/B reruns after every patch unless a patch changes core invariants, changes scope, introduces new runtime/API surface, or contradicts a previously clean coverage area.

Full-system review coverage must include:

- Lifecycle/state behavior.
- Security and public/private data boundaries.
- Hash, audit, evidence chain, idempotency, and determinism.
- API/error precedence and DTO defaults.
- Test/proof level.
- Scope boundaries and STOP gates.
- Builder ambiguity.
- Repo-real feasibility.

Finding classifications:

- `BLOCKER`.
- `PATCH_REGRESSION`.
- `LATENT_SPEC_GAP`.
- `TEST_HARDENING_ONLY`.
- `BOOKKEEPING_ONLY`.
- `DEFERRED`.

Only `BLOCKER`, `PATCH_REGRESSION`, and implementation-blocking `LATENT_SPEC_GAP` findings require immediate patch and re-review.

After two consecutive rounds with no blocker-category findings, planning review must stop and move hardening-only items to deferred notes unless a new patch changes core invariants, scope, runtime/API surface, or a previously clean coverage area.
