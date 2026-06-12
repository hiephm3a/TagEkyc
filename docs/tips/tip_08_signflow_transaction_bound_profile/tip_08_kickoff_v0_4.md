# TIP-08 SignFlow Transaction-Bound Profile and S1 End-to-End Proof Kickoff v0.4

**File:** `docs/tips/tip_08_signflow_transaction_bound_profile/tip_08_kickoff_v0_4.md`
**Version:** 0.4
**Status:** Closed - implementation accepted and committed
**Date:** 2026-06-12
**Accepted Planning Brief:** `docs/tips/tip_08_signflow_transaction_bound_profile/tip_08_planning_brief_v0_3.md`
**Baseline:** `05a8e9faf628e28e740d287d2e9afc76667f4836`
**Purpose:** Prepare a narrow implementation plan for a LocalDev S1 end-to-end proof of `TransactionBoundEkycProfile` for SignFlow consumption without adding SignFlow runtime, source, database, network, endpoint, webhook, outbox, or durable persistence behavior.

## Changelog

### Post-implementation closeout - 2026-06-12

- Recorded accepted TIP-08 code/test commit `282eb821b7500f2965b336a5e67467bffc68adf4` (`test: prove TIP-08 transaction-bound SignFlow S1 flow`).
- Recorded post-commit validation: `dotnet test TagEkyc.sln --no-restore` passed with 64 passed, 0 failed, 0 skipped (`TagEkyc.ContractTests` 9 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 39 passed).
- Confirmed TIP-08 remained test/proof-only with no `src/**` changes, no endpoint/query/service/runtime projection, no DTO/contract changes, and no SignFlow runtime/source/database/network dependency.
- Recorded semantic proof validity corrections for stored-session evidence, computed digest-shaped `bindingNonceHash`, sentinel-backed leakage assertions where feasible, canonical `DocumentNfc` to `NfcValidation` evidence mapping, and package `PackageHash` versus completion/notification `EvidencePackageHash` linkage.

### v0.4 - Semantic proof validity patches

- Required sentinel-backed leakage assertions where feasible instead of broad absence checks.
- Required the implementation report to include a Semantic Proof Evidence Map for every proof claim.
- Required the binding nonce hash to be computed from the raw nonce sentinel, or the hard-coded digest to be verified against that computation, while never submitting the raw nonce to TagEkyc.
- Reopened kickoff status as draft pending semantic proof validity review.

### v0.3 - TIP-local docs preflight clarification

- Clarified that implementation preflight should stop on unexpected dirty status, while allowing accepted/current TIP-08 planning and kickoff docs to remain dirty until docs commit/closeout.
- Updated the accepted planning reference to Planning Brief v0.3 after final sentinel alignment of RequiredChecks negative-test guidance.
- Preserved the no-code/no-runtime dispatch boundary.

### v0.2 - A/B review targeted patches

- Added missing pre-edit evidence surfaces for the SignFlow placeholder boundary, BusinessConsumer DTOs, LocalDev policy authorization, and review playbook rules.
- Clarified that `c7fa9a5` is a relevant historical runtime anchor, not a separate HEAD baseline requirement.
- Clarified that TIP-08 must prove use of canonical SignFlow S1 RequiredChecks without adding a new allowed-but-non-S1 rejection policy.
- Added a never-submitted raw nonce sentinel for meaningful raw nonce leakage assertions.
- Added conditional hardening guidance for domain/contract equality of SignFlow S1 RequiredChecks.
- Added pre-dispatch A/B review corrections for package-summary leakage assertions, deterministic digest-shaped binding hash, stored-session binding verification, and package `PackageHash` versus completion/notification `EvidencePackageHash` wording.

### v0.1 - Initial kickoff draft

- Prepared the first TIP-08 kickoff from accepted Planning Brief v0.2.
- Scoped implementation to a LocalDev application/unit proof with test-level SignFlow-facing composition only.

## 0. Post-Implementation Closeout

TIP-08 code/test implementation was accepted and committed at:

- `282eb821b7500f2965b336a5e67467bffc68adf4` - `test: prove TIP-08 transaction-bound SignFlow S1 flow`.

Post-commit validation:

- `dotnet test TagEkyc.sln --no-restore`.
- Passed: 64, Failed: 0, Skipped: 0.
- `TagEkyc.ContractTests`: 9 passed.
- `TagEkyc.ArchTests`: 16 passed.
- `TagEkyc.UnitTests`: 39 passed.

Closed implementation scope:

- Test/proof-only.
- No `src/**` changes.
- No endpoint, query, service, runtime projection, webhook, outbox, retry, EF/migration, durable persistence, or SignFlow runtime/source/database/network dependency.
- No DTO or contract changes.

Proof validity corrections accepted in the final implementation:

- Preservation and binding assertions read stored LocalDev session state and application outputs, not only input variables.
- `bindingNonceHash` is computed from the raw nonce sentinel, asserted to match `sha256:[0-9a-f]{64}`, and the raw sentinel is asserted absent from serialized outputs.
- Leakage assertions are sentinel-backed where the current fixture path can legally carry a marker: raw nonce, artifact-related device marker, plaintext identity, adapter internal marker, and SignFlow internal marker. VaultRef and production assurance/legal claim are documented as non-applicable because the current DTO path has no first-class input fields for those categories; the proof keeps a weak generic `"vault:"` absence check.
- Canonical SignFlow S1 RequiredCheck `DocumentNfc` remains in `SignFlowS1RequiredChecks.Values`; current repo behavior evidences it with `EvidenceResultTypeDto.NfcValidation`.
- Package summary `PackageHash` is asserted against completion and notification `EvidencePackageHash`, with `EvidencePackageId` and `ManifestHash` linkage.

Semantic proof review rubric preserved for future TIPs:

| Claim | Evidence Source | Runtime/Persisted? | DTO field exact? | Leakage boundary? | Could pass by input echo? |
| --- | --- | --- | --- | --- | --- |

Reusable rule: `L-TAG-Proof-01 - Proof Claim Evidence Source Rule` in `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`.

## 1. Baseline Commits

Implementation must start from:

- `05a8e9faf628e28e740d287d2e9afc76667f4836` / `05a8e9f` - `docs: sync TIP-07 roadmap and index state`.
- `fe26c73cae02fca7999132a9117661ee5e82079b` / `fe26c73` - `docs: close TIP-07 governance state`.
- `916dd2918c2ab47ab0658ebf271fae45e22fb3ca` / `916dd29` - `feat: add TIP-07 completion notification projection`.
- `761ad98e83f63e4a6414fa2fa9704e258303758c` / `761ad98` - `docs: codify TIP-06 review stop rules`.

Relevant historical runtime anchor, not a separate HEAD baseline requirement:

- `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef` / `c7fa9a5` - `feat: implement TIP-06 completion and evidence package runtime`.

Required preflight before implementation:

```powershell
git status --short
git log --oneline -8
dotnet test TagEkyc.sln --no-restore
```

STOP+REPORT if tests fail, or if `git status --short` shows dirty files outside the accepted/current TIP-08 docs:

- `docs/tips/tip_08_signflow_transaction_bound_profile/tip_08_planning_brief_v0_3.md`
- `docs/tips/tip_08_signflow_transaction_bound_profile/tip_08_kickoff_v0_4.md`

Dirty TIP-08 planning/kickoff docs may remain uncommitted during the active TIP-08 planning/dispatch handoff, but implementation must not modify `src/` or `tests/` before this preflight passes under that allowlist.

## 2. Implementation Objective

Add a focused LocalDev application/unit S1 end-to-end proof for `TransactionBoundEkycProfile`.

The proof must demonstrate:

1. Create a transaction-bound session for `SIGNING_AUTH`.
2. Preserve `externalSessionId`, `externalTransactionId`, and `bindingNonceHash`.
3. Append capture artifacts needed by the proof.
4. Append TrustedAdapter evidence for all SignFlow S1 required checks.
5. Complete the session.
6. Read the evidence package summary and assert package `PackageHash` equals completion/notification `EvidencePackageHash`, with matching `evidencePackageId` and `manifestHash` linkage.
7. Read the TIP-07 completion notification projection.
8. Compose `SignFlowVerificationResultDto` and `SigningAuthorizationBindingDto` at test/proof level from existing outputs and existing `SignFlowProfile` contracts.
9. Assert no raw artifact, plaintext identity field, VaultRef, adapter internal payload, raw binding nonce, SignFlow internal/runtime data, or production assurance claim leaks into the package summary, SignFlow-facing proof summary, or completion notification.
10. Assert no new SignFlow runtime/source/database/network dependency is introduced.

The implementation objective is proof only. It must not add runtime product behavior.

### Sentinel-Backed Leakage Rules

Leakage assertions must be sentinel-backed where feasible.

For every no-leak claim, the test should introduce explicit sentinel values into the nearest allowed internal/test fixture source, then assert those sentinels are absent from serialized public/proof outputs.

Minimum sentinel categories:

- Raw nonce sentinel.
- Raw artifact payload/content sentinel.
- Internal VaultRef sentinel or `vault:`-style sentinel if current test fixtures can carry it.
- Adapter internal payload sentinel.
- Plaintext identity sentinel if current evidence/profile fixture can carry identity-like data.
- SignFlow runtime/internal marker sentinel.
- Production assurance/legal claim sentinel.

If the current fixture/path has no legal place to inject a sentinel for a category, the implementation report must state why that category is not applicable. Do not use broad string absence checks as proof unless the corresponding sentinel was actually introduced or the non-applicability is explained.

## 3. Exact Files Likely Touched

Default intended implementation file:

- `tests/TagEkyc.UnitTests/Tip08TransactionBoundE2eProofTests.cs` - new focused application/unit E2E proof.

Conditional test files, only if an actual proof gap remains after reading existing coverage:

- `tests/TagEkyc.ContractTests/ProjectionBoundaryTests.cs` - only if the existing SignFlow contract RequiredChecks or DTO boundary assertion needs a narrow hardening check.
- `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs` - only if existing project dependency coverage cannot prove the no-SignFlow-runtime-dependency rule.

Implementation should not need production code changes. If the proof cannot be completed without changing production code, STOP+ASK before editing any `src/` file.

## 4. Files That Must Not Be Touched By Default

Do not touch these by default:

- `src/**`
- `src/TagEkyc.Api/**`
- `src/TagEkyc.Application/**`
- `src/TagEkyc.Contracts/**`
- `src/TagEkyc.Domain/**`
- `src/TagEkyc.Infrastructure/**`
- `src/TagEkyc.Adapters/**`
- `src/TagEkyc.SignFlow/**`
- EF, DbContext, migrations, persistence, webhook, endpoint, dispatcher, retry, outbox, or network files if any are later introduced.

Do not modify docs during implementation except an execution report or explicitly requested closeout document after the implementation is reviewed.

## 5. Repo-Real Evidence To Collect Before Edit

Before editing tests, the builder must re-verify and report exact source/test file/line anchors. Current planning-time anchors are listed below and must be treated as starting evidence, not as a substitute for re-verification.

### Transaction-Bound Profile Invariants

- `src/TagEkyc.Domain/VerificationSession.cs:108` checks `profile == VerificationProfile.TransactionBoundEkycProfile`.
- `src/TagEkyc.Domain/VerificationSession.cs:110` requires non-empty `externalTransactionId`.
- `src/TagEkyc.Domain/VerificationSession.cs:115` requires non-null `bindingNonceHash`.
- `src/TagEkyc.Domain/VerificationSession.cs:129` and `src/TagEkyc.Domain/VerificationSession.cs:130` preserve `externalTransactionId` and `bindingNonceHash` in the session.

### Application Create-Session Error Behavior

- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:68` returns `TRANSACTION_BOUND_NOT_ALLOWED` for policy denial.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:77` returns `MISSING_TRANSACTION_BINDING`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:78` states transaction-bound sessions require `externalTransactionId` and `bindingNonceHash`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:85` returns `INVALID_BINDING_NONCE_HASH`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:86` requires the `sha256:` prefix.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:103` returns `DUPLICATE_EXTERNAL_SESSION`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:104` scopes duplicate `externalSessionId` to the client application.

### SignFlow S1 RequiredChecks

- `src/TagEkyc.Domain/RequiredCheckPolicy.cs:5` defines `SignFlowS1RequiredChecks`.
- `src/TagEkyc.Domain/RequiredCheckPolicy.cs:8` through `src/TagEkyc.Domain/RequiredCheckPolicy.cs:11` list `CaptureQuality`, `DocumentNfc`, `FaceMatch`, and `Liveness`.
- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:6` defines contract `SignFlowS1RequiredChecks`.
- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:10` through `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:13` mirror the DTO values.

### Existing SignFlow-Facing Contracts

- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:17` defines `SigningAuthorizationBindingDto`.
- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:24` defines `SignFlowVerificationResultDto`.
- `src/TagEkyc.Contracts/SignFlowProfile/SignFlowProfileContracts.cs:26` includes the binding DTO in the result DTO.
- `src/TagEkyc.SignFlow/SigningAuthorizationBindingPlaceholder.cs:5` defines the existing SignFlow placeholder.

### Existing BusinessConsumer DTO Boundary

- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs:10` defines `CreateVerificationSessionRequestDto`.
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs:31` defines `VerificationSessionSummaryDto`.
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs:86` defines `VerificationCompletedEventDto`.

### LocalDev Policy Authorization

- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs:144` defines `CreateBusinessPolicy`.
- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs:151` allows `VerificationProfile.TransactionBoundEkycProfile`.
- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs:156` allows `SIGNING_AUTH`.

### Existing Application Outputs For Test-Level Composition

- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:17` exposes `CreateAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:153` exposes `GetSummaryAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:189` maps `session.ExternalSessionId` to the summary.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:194` and `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs:196` map evidence package and manifest values to the summary.
- `src/TagEkyc.Application/LocalDev/LocalDevInMemoryRepositories.cs:27` exposes LocalDev session `GetAsync` for test-only verification of stored binding fields.
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs:20` exposes `AppendCaptureArtifactAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs:120` exposes `AppendEvidenceResultAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs:128` requires a `TrustedAdapter` caller for evidence.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:28` exposes `CompleteAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:282` exposes `GetEvidencePackageAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:339` exposes `GetCompletionNotificationAsync`.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:626` defines completion notification mapping.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:630` and `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:631` use `VERIFICATION_COMPLETED` and `localdev-not-dispatched`.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:641` maps `ManifestHash`.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:645` uses placeholder signature status.

### Existing Test Coverage To Reference Instead Of Duplicating

- `tests/TagEkyc.UnitTests/Tip04SessionApplicationTests.cs:138` and `tests/TagEkyc.UnitTests/Tip04SessionApplicationTests.cs:143` cover missing/invalid transaction-bound binding inputs.
- `tests/TagEkyc.UnitTests/Tip04SessionApplicationTests.cs:256` defines the transaction-bound request helper.
- `tests/TagEkyc.UnitTests/Tip06CompletionApplicationTests.cs:20` covers deterministic package snapshot completion.
- `tests/TagEkyc.UnitTests/Tip06CompletionApplicationTests.cs:55` and `tests/TagEkyc.UnitTests/Tip06CompletionApplicationTests.cs:56` assert package and manifest hashes.
- `tests/TagEkyc.UnitTests/Tip07CompletionNotificationApplicationTests.cs:19` covers completion notification mapping.
- `tests/TagEkyc.UnitTests/Tip07CompletionNotificationApplicationTests.cs:39` through `tests/TagEkyc.UnitTests/Tip07CompletionNotificationApplicationTests.cs:53` assert deterministic notification fields and package/hash linkage.
- `tests/TagEkyc.ContractTests/ProjectionBoundaryTests.cs:116` covers SignFlow RequiredChecks contract narrowness.
- `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs:9` and `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs:28` cover project dependency boundaries, including `TagEkyc.SignFlow` referencing only `TagEkyc.Contracts`.

### Review Playbook Rules

- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md:251` defines runtime TIP review rules.
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md:269` defines full coverage first, then invalidation review.
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md:300` defines finding classification and stop rules.
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md:353` keeps SignFlow as TagEkyc-owned transaction-bound profile placeholders only.

If any of these anchors differ at implementation time, STOP+ASK before adapting the proof.

## 6. Step-by-Step Implementation Plan

1. Run required preflight.
2. Re-verify the repo-real evidence checklist with exact source/test anchors.
3. Add `tests/TagEkyc.UnitTests/Tip08TransactionBoundE2eProofTests.cs`.
4. Build a LocalDev fixture using the existing in-memory repositories and application services, following TIP-06/TIP-07 unit test fixture style.
5. Create a `TransactionBoundEkycProfile` session using:
   - `Purpose = "SIGNING_AUTH"`.
   - `ExternalSessionId = "sf-session-tip08"`.
   - `ExternalTransactionId = "sf-transaction-tip08"`.
   - Raw nonce sentinel `"raw-tip08-binding-nonce-never-submit"` kept only in the test and never submitted to TagEkyc.
   - `BindingNonceHash = "sha256:41f9c42906c35e21ee93bf3729dd043baae00f8d06f9ecf362607f7024835c2b"`, the deterministic SHA-256 digest of the raw nonce sentinel.
   - The test should compute the SHA-256 digest from the raw nonce sentinel or assert that the hard-coded digest equals the computed digest before submitting only the `sha256:<digest>` value.
   - RequiredChecks from `SignFlowS1RequiredChecks.Values`.
6. Append capture artifacts needed for the four SignFlow S1 checks:
   - `DeviceCaptureMetadata` or equivalent supporting artifact for capture quality if current policy allows it.
   - `NfcReadArtifact`.
   - `SelfieImage`.
   - `LivenessMedia`.
7. Append TrustedAdapter evidence results for the SignFlow S1 required checks:
   - `CaptureQuality`.
   - `DocumentNfc`.
   - `FaceMatch`.
   - `Liveness`.
   If the current evidence API or test helpers use a lower-level NFC adapter/result name such as `NfcValidation`, map it explicitly to the canonical `DocumentNfc` required check and cite the source/test anchor. Do not replace the SignFlow S1 required check with `NfcValidation` unless repo-real policy proves that is the existing canonical value; otherwise STOP+ASK.
8. Complete the session.
9. Read the stored session from the LocalDev fixture repository and assert TagEkyc persisted `ExternalSessionId`, `ExternalTransactionId`, and `BindingNonceHash`.
10. Read the session summary, evidence package summary, and completion notification using existing application services.
11. Compose a `SigningAuthorizationBindingDto` at test/proof level from the stored session binding fields and completed package output.
12. Compose a `SignFlowVerificationResultDto` at test/proof level from the session summary and binding DTO.
13. Assert:
    - transaction-bound profile was preserved,
    - stored `externalSessionId`, `externalTransactionId`, and `bindingNonceHash` match the original request,
    - binding DTO preserves `externalSessionId`, `externalTransactionId`, and `bindingNonceHash`,
    - `bindingNonceHash` is present where expected and the raw nonce sentinel is absent from serialized outputs,
    - package summary `PackageHash` equals completion/notification `EvidencePackageHash`,
    - binding DTO evidence package id/hash match completion and package summary outputs,
    - completion notification evidence package id/hash/manifest match completion outputs,
    - package summary evidence refs are present for required evidence,
    - placeholder signature statuses remain placeholder/unverified,
    - no raw artifact, plaintext identity field, `vault:`, adapter internal payload, raw nonce, SignFlow internal/runtime marker, or production assurance claim leaks into serialized package summary, SignFlow summary, or completion notification, using sentinel-backed assertions where feasible.
14. Run focused tests, then full solution tests:

```powershell
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore
dotnet test TagEkyc.sln --no-restore
```

15. If contract or architecture hardening is genuinely necessary, add the smallest focused assertion in the conditional test file and rerun the relevant project plus the full solution.

## 7. Test Plan

Primary test:

- `Tip08_transaction_bound_profile_completes_s1_flow_and_composes_signflow_binding_summary`

Primary assertions:

- session creation succeeds for `TransactionBoundEkycProfile`;
- SignFlow S1 RequiredChecks are used as the proof input;
- capture artifacts and TrustedAdapter evidence satisfy all required checks;
- completion returns `Completed`, `Passed`, evidence package id, evidence package hash, and manifest hash;
- package read returns the same package id/manifest values and package `PackageHash` equal to completion/notification `EvidencePackageHash`;
- completion notification returns deterministic TIP-07 LocalDev fields;
- stored session binding fields preserve `ExternalSessionId`, `ExternalTransactionId`, and `BindingNonceHash`;
- `SigningAuthorizationBindingDto` is composed from stored binding values and preserves package linkage;
- `SignFlowVerificationResultDto` contains only the existing session summary and binding DTO;
- serialized package summary, SignFlow proof output, and completion notification include the expected `bindingNonceHash` only where intended and exclude raw artifacts, plaintext identity fields, VaultRefs, adapter internals, the never-submitted raw nonce sentinel, SignFlow internals, and production assurance claims.
- no-leak assertions are backed by introduced sentinels where the fixture path can legally carry them; otherwise the implementation report explains non-applicability by category.

### Semantic Proof Evidence Map

Implementation report must include a Semantic Proof Evidence Map.

For each proof claim, builder must name the evidence source:

- Transaction-bound profile preserved.
- `externalSessionId` preserved.
- `externalTransactionId` preserved.
- `bindingNonceHash` preserved.
- RequiredChecks used canonically.
- Package hash / manifest hash linkage.
- Completion notification linkage.
- SignFlow binding DTO composition source.
- No-leakage claims.

Required columns:

| Claim | Evidence source | Runtime/persisted/application output? | DTO/property exact name | Could pass by input echo? |
| --- | --- | --- | --- | --- |

Reject any proof claim whose assertion can pass only by reusing local test input variables rather than TagEkyc runtime/session/package/application output state.

Conditional hardening assertion:

- Add a narrow domain/contract equality assertion for SignFlow S1 RequiredChecks only if current coverage still pins the domain and contract lists separately without comparing them through an explicit mapping.

Representative negative tests only:

- Missing/invalid binding behavior may be referenced from existing TIP-04 tests unless current evidence has changed.
- TIP-08 must assert that the proof uses the canonical SignFlow S1 RequiredChecks. Do not add a rejection test for an allowed-but-non-S1 RequiredChecks set unless repo-real behavior already enforces that rejection; otherwise STOP+ASK before adding new policy behavior.
- Cross-client SignFlow-facing summary behavior should be tested only if a new application boundary is approved after STOP+ASK.
- Pre-completion SignFlow-facing summary behavior should be tested only if such a summary boundary is approved after STOP+ASK.

Do not duplicate the full TIP-04 through TIP-07 negative matrix.

## 8. STOP+ASK Gates

STOP+ASK before:

- Editing any `src/` file.
- Adding a new application query.
- Adding a public endpoint.
- Adding a runtime projection.
- Adding a new DTO, contract, or serialized field.
- Changing `TransactionBoundEkycProfile` rules.
- Changing `RequiredCheckPolicy.SignFlowS1RequiredChecks` or contract `SignFlowS1RequiredChecks.Values`.
- Changing existing error/status semantics.
- Adding SignFlow source, database, runtime package, network client, deployment dependency, or internal model reference.
- Adding webhook delivery, retry, subscription, dispatcher, outbox, callback, or real external HTTP behavior.
- Adding EF, DbContext, migrations, durable persistence, or production storage/vault/retrieval.
- Adding production cryptography, production signing authorization, non-repudiation, legal audit reliance, or production identity assurance claims.
- Exposing raw binding nonce, raw artifacts, plaintext identity fields, internal VaultRefs, adapter internals, or SignFlow internals.
- Discovering that existing outputs cannot compose `SignFlowVerificationResultDto` / `SigningAuthorizationBindingDto` at test/proof level.
- Finding repo-real behavior that differs from Section 5 anchors.

## 9. Review Protocol

Use the review rules from `761ad98e83f63e4a6414fa2fa9704e258303758c`:

- First serious implementation receives one full-system review with coverage attestation.
- After patches, use affected-surface map plus invalidated-area review.
- Run a lightweight blocker-only sentinel sweep.
- Avoid endless full reruns after every patch unless a patch changes core invariants, scope, runtime/API surface, or a previously clean coverage area.

Finding classifications:

- `BLOCKER`.
- `PATCH_REGRESSION`.
- `LATENT_SPEC_GAP`.
- `TEST_HARDENING_ONLY`.
- `BOOKKEEPING_ONLY`.
- `DEFERRED`.

Only `BLOCKER`, `PATCH_REGRESSION`, and implementation-blocking `LATENT_SPEC_GAP` findings require immediate patch and re-review.

## 10. Commit Plan

Do not stage or commit until implementation and review are accepted.

Expected commit scope after accepted implementation:

- Primary: `tests/TagEkyc.UnitTests/Tip08TransactionBoundE2eProofTests.cs`.
- Conditional: `tests/TagEkyc.ContractTests/ProjectionBoundaryTests.cs` only if contract hardening is necessary.
- Conditional: `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs` only if dependency hardening is necessary.
- Optional after review: TIP-08 execution report or docs closeout only if explicitly requested.

Before staging:

```powershell
git status --short
git diff --name-only
git diff -- tests/TagEkyc.UnitTests/Tip08TransactionBoundE2eProofTests.cs
```

Stage by explicit allowlist only:

```powershell
git add -- tests/TagEkyc.UnitTests/Tip08TransactionBoundE2eProofTests.cs
```

If conditional files are touched after accepted review, add them explicitly by path. Then verify:

```powershell
git diff --cached --name-only
git diff --cached --stat
```

If any `src/` file or unapproved file is staged, STOP and unstage before committing.

Suggested implementation commit message after accepted review:

```text
test: prove TIP-08 transaction-bound SignFlow S1 flow
```
