# TIP-62 LocalDev Metadata Reference Flow Integration Planning Brief v0.1

**File:** `docs/tips/tip_62_localdev_metadata_reference_flow_integration/tip_62_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Accepted - autonomous implementation slice
**Date:** 2026-06-21
**Baseline:** `8c8897dcbcf48ab1c7e89e51422fd8d7d59bb024 docs: close TIP-61 flow gap authorization`
**Purpose:** Plan the authorized internal integration from accepted LocalDev capture artifact metadata/hash-only records to the TIP-60 metadata reference registry.

## Changelog

### v0.1 - Initial planning brief

- Opened TIP-62 as the implementation slice authorized by TIP-61.
- Recorded Phase 0 read-only compatibility audit findings.
- Defined implementation scope, exact allowlist, expected tests, non-claims, STOP/RRI gates, review ladder plan, and convergence/learning rule.

## Phase 0 Read-Only Compatibility Audit

Phase 0 ran before editing, staging, committing, formatting, or fixing files.

Commands run:

```text
git status --short
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
?? docs/00_GDRIVE_FILE_INDEX.md

git rev-parse HEAD
8c8897dcbcf48ab1c7e89e51422fd8d7d59bb024

git log --oneline -8
8c8897d docs: close TIP-61 flow gap authorization
f4d82d2 feat: implement TIP-60 localdev metadata reference registry
2efc020 docs: close TIP-59 s3 bundle grouping authorization
667ae5d docs: close TIP-58 metadata reference runtime authorization planning
2d7540c docs: close TIP-57 metadata reference query semantics implementation
7649ae3 feat: implement TIP-57 metadata reference query semantics
59fe1b1 docs: open TIP-57 metadata reference query semantics implementation
57189c9 docs: close TIP-56 metadata reference query semantics planning
```

Known unrelated dirty files remain out of scope and must not be staged or committed:

```text
.gitignore
docs/00_AGENT_COORDINATION_BUS.md
docs/00_GDRIVE_FILE_INDEX.md
```

Mandatory files inspected read-only:

```text
docs/00_REVIEW_AND_TIP_PLAYBOOK.md
docs/tips/README.md
docs/tips/tip_61_minimal_ekyc_technical_flow_gap_map_tip_62_authorization/tip_61_planning_brief_v0_1.md
docs/tips/tip_61_minimal_ekyc_technical_flow_gap_map_tip_62_authorization/tip_61_closeout_v0_1.md
src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs
src/TagEkyc.Application/Ports/ApplicationServicePorts.cs
src/TagEkyc.Application/Ports/MetadataReferencePorts.cs
src/TagEkyc.Application/LocalDev/LocalDevInMemoryMetadataReferenceRegistry.cs
src/TagEkyc.Api/Program.cs
tests/TagEkyc.UnitTests/Tip05EvidenceApplicationTests.cs
tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs
tests/TagEkyc.UnitTests/Tip60LocalDevMetadataReferenceRegistryTests.cs
tests/TagEkyc.ArchTests/Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs
```

Audit findings:

| Required audit item | Finding | Scope result |
| --- | --- | --- |
| Actual constructor dependencies of `VerificationEvidenceApplicationService` | Existing constructor dependencies are `IVerificationSessionRepository`, `ICaptureArtifactRepository`, `IEvidenceResultRepository`, `IAuditEventRepository`, and `ILocalDevClientPolicyProvider`. | Adding `IMetadataReferenceRegistry` as an optional trailing dependency is compatible with existing out-of-allowlist direct constructors. |
| Whether `IMetadataReferenceRegistry` can be injected without changing `ApplicationServicePorts.cs` | Yes. Existing service implements `ICaptureArtifactCommands` and can use the registry internally without changing command/query interfaces. | No port/interface edit needed. |
| Whether `Program.cs` needs DI changes | No production composition change is needed because `Program.cs` already registers `LocalDevInMemoryMetadataReferenceRegistry` and maps `IMetadataReferenceRegistry`. | `Program.cs` does not need editing. |
| Current capture artifact accepted path | `AppendCaptureArtifactAsync` validates caller, writable session, policy, artifact hashes, creates a `CaptureArtifact`, appends it, advances session state to `InProgress` if needed, appends audit, and returns accepted response. | Safe integration point is after `captureArtifacts.AppendAsync` succeeds. |
| Current evidence result accepted path | `AppendEvidenceResultAsync` validates trusted caller, input artifacts, result status, payload hash, sanitized summary, appends `EvidenceResult`, updates readiness state, appends audit, and returns accepted response. | No evidence-result registration is required for TIP-62. |
| Exact test fixture impacts | `Tip05EvidenceApplicationTests` constructs the service directly and can receive an in-memory registry. `Tip05ApiPipelineTests` has a local DI fixture that must mirror registry DI. Other tests outside the allowlist also construct the service directly. | Use optional constructor dependency to avoid editing out-of-scope tests. |
| Old sentinel or route/Contracts test conflicts | Existing API route sentinel rejects specialized endpoints. TIP-55/TIP-57/TIP-60 architecture tests enforce no API/Contracts metadata exposure and exactly one LocalDev registry implementation. | No conflict if no public endpoint/Contracts DTO is added. |
| Whether TIP-61 allowed file list is sufficient | Yes. Implementation and tests fit inside `VerificationEvidenceApplicationService.cs`, Tip05 tests, TIP-62 docs, and README. | No STOP/RRI triggered. |

## TIP-61 Authorization Summary

TIP-61 final disposition:

```text
READY_TO_DISPATCH_TIP_62_LOCALDEV_METADATA_REFERENCE_FLOW_INTEGRATION
```

TIP-61 authorized Option A only:

```text
internal metadata reference registry integration only
```

TIP-62 may connect accepted capture artifact metadata/hash-only records to `IMetadataReferenceRegistry` internally. TIP-62 must not add public metadata/reference API, Contracts DTO exposure, persistence, schema/migration/database changes, provider/storage/resolver/tool selection, raw payload handling, artifact/raw byte persistence, restricted artifact access, package completeness proof, reference availability proof, artifact existence/access proof, provider evidence proof, or readiness/legal/audit/security/production/certification/capability claim.

## Exact File Allowlist

TIP-62 may edit only:

```text
src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs
src/TagEkyc.Api/Program.cs
tests/TagEkyc.UnitTests/Tip05EvidenceApplicationTests.cs
tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs
tests/TagEkyc.UnitTests/Tip60LocalDevMetadataReferenceRegistryTests.cs
tests/TagEkyc.ArchTests/Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs
docs/tips/README.md
```

TIP-62 may add only:

```text
docs/tips/tip_62_localdev_metadata_reference_flow_integration/tip_62_planning_brief_v0_1.md
docs/tips/tip_62_localdev_metadata_reference_flow_integration/tip_62_closeout_v0_1.md
```

`src/TagEkyc.Api/Program.cs` remains DI/composition-only if needed. Phase 0 found no need to edit it.

## Implementation Plan

1. Add an optional `IMetadataReferenceRegistry` dependency to `VerificationEvidenceApplicationService`.
2. After a capture artifact record is accepted and appended, register a metadata reference internally.
3. Use existing metadata reference semantics:
   - `MetadataReferenceId` value based on the accepted capture artifact id;
   - reference kind `capture-artifact-metadata`;
   - state `RegisteredMetadata`;
   - metadata hash from the accepted capture artifact metadata hash, falling back to existing artifact hash only when no metadata hash was supplied.
4. Keep public request/response contracts unchanged.
5. Keep API routes unchanged.
6. Update allowed Tip05 tests to prove internal registration and no public metadata/reference route exposure.
7. Keep TIP-55/TIP-57/TIP-60 non-proof semantics intact.

## Expected Tests

Required validation:

```powershell
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore --filter "Tip05|Tip55|Tip57|Tip60"
dotnet test tests/TagEkyc.ArchTests/TagEkyc.ArchTests.csproj --no-restore --filter "Tip55|Tip57|Tip60"
dotnet test TagEkyc.sln --no-restore
git diff --check
git diff --cached --check
git diff --cached --name-only
git status --short
```

Tests must prove:

- capture/evidence flow registers a metadata reference internally;
- the registry can query the registered reference after capture flow;
- unknown reference remains non-success and not reliable;
- registered metadata still denies artifact access, package completeness, provider evidence availability, and readiness proofs;
- no public API/Contracts exposure appears;
- existing session/capture/evidence/package behavior is not weakened;
- existing TIP-55/TIP-57/TIP-60 metadata invariants are not weakened.

## Non-Claims

TIP-62 must not be used to claim:

```text
artifact existence proof
artifact access proof
evidence package completeness proof
provider evidence availability proof
reference availability proof for reliance
production readiness
readiness/legal/audit/security/certification/capability proof
```

## STOP/RRI Gates

STOP/RRI is required before:

- editing files outside the TIP-62 allowlist;
- changing `ApplicationServicePorts.cs` or `MetadataReferencePorts.cs`;
- changing Contracts DTOs or endpoint routes;
- adding persistence, schema, migration, database, provider, storage, resolver, tool, raw payload, artifact/raw byte, or restricted artifact access behavior;
- claiming package completeness, reference availability proof, artifact existence/access proof, provider evidence proof, or readiness/legal/audit/security/production/certification/capability;
- staging or committing unrelated dirty files;
- weakening TIP-55/TIP-57/TIP-60 tests or semantics;
- continuing after five non-convergent review rounds.

## Review Ladder Plan

TIP-62 follows the Autonomous Slice Review Ladder / Quality Gate:

1. Phase 0 read-only compatibility audit.
2. Author implementation pass.
3. V1 deep bounded review.
4. Patch findings inside authorized scope.
5. V2 patch verification.
6. V3 free adversarial review.
7. Final validation.
8. Commit.
9. Closeout reviewer pass.
10. Patch closeout if needed.
11. Final report.

## Convergence / Learning Rule

TIP-62 may use up to five total review rounds. If review does not converge, STOP without committing and produce Review Failure Analysis with unresolved findings, convergence failure cause, exact changed files, risk classification, recommended next action, lessons learned, and prompt/rule improvement suggestions.
