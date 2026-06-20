# TIP-60 Group A LocalDev Metadata Reference Runtime Registry Planning Brief v0.1

**File:** `docs/tips/tip_60_group_a_localdev_metadata_reference_runtime_registry/tip_60_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - autonomous implementation
**Date:** 2026-06-20
**Baseline:** `2efc020f4767973993ed1cf59caa7d646acdae42 docs: close TIP-59 s3 bundle grouping authorization`
**Purpose:** Implement the Group A LocalDev/non-production in-memory `IMetadataReferenceRegistry` authorized by TIP-59 while preserving metadata-as-non-proof invariants.

## Changelog

### v0.1 - Initial planning brief

- Opened TIP-60 implementation planning after the mandatory read-only start check.
- Recorded the accepted STOP/RRI and scope correction for stale TIP-55/TIP-57 architecture sentinels.
- Defined the exact implementation plan, file allowlist, tests, non-claims, STOP/RRI gates, review ladder plan, and convergence/learning rule.

## Repo Evidence / Start Check

Read-only checks before editing source, tests, or docs:

```text
git rev-parse HEAD
2efc020f4767973993ed1cf59caa7d646acdae42

git log --oneline -5
2efc020 docs: close TIP-59 s3 bundle grouping authorization
667ae5d docs: close TIP-58 metadata reference runtime authorization planning
2d7540c docs: close TIP-57 metadata reference query semantics implementation
7649ae3 feat: implement TIP-57 metadata reference query semantics
59fe1b1 docs: open TIP-57 metadata reference query semantics implementation

git status --short
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
?? docs/00_GDRIVE_FILE_INDEX.md
```

Known unrelated dirty files remain out of scope and must not be staged:

```text
.gitignore
docs/00_AGENT_COORDINATION_BUS.md
docs/00_GDRIVE_FILE_INDEX.md
```

Read-only inspection confirmed the authorized surfaces exist:

| Surface | Finding |
| --- | --- |
| `docs/00_REVIEW_AND_TIP_PLAYBOOK.md` | Autonomous Slice Review Ladder / Quality Gate applies. |
| `docs/tips/README.md` | TIP-59 is indexed with final disposition `READY_TO_DISPATCH_TIP_60_GROUP_A_LOCALDEV_METADATA_REFERENCE_RUNTIME_REGISTRY`. |
| TIP-59 planning/closeout | Authorizes Group A only: LocalDev/non-production in-memory `IMetadataReferenceRegistry` plus LocalDev DI and tests. |
| `src/TagEkyc.Application/Ports/MetadataReferencePorts.cs` | `IMetadataReferenceRegistry`, registration, record, and query result contracts exist and remain metadata-only. |
| `src/TagEkyc.Domain/MetadataReferenceId.cs` | Reference IDs reject raw/payload/byte-shaped values and redact `ToString()`. |
| `src/TagEkyc.Domain/MetadataReferenceState.cs` | Query states preserve non-success and proof-denial helpers. |
| `src/TagEkyc.Application/LocalDev/LocalDevInMemoryRepositories.cs` | Existing LocalDev in-memory convention and shared visibility gate exist. |
| `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs` | LocalDev/non-production policy source convention exists. |
| `src/TagEkyc.Api/Program.cs` | LocalDev in-memory DI composition exists. |
| TIP-55/TIP-57 tests | Existing metadata-only unit tests remain usable; old architecture sentinels required a scope correction. |

## RRI Summary / Scope Correction

TIP-60 initially stopped before edits because old architecture sentinels asserted:

```text
no IMetadataReferenceRegistry implementation exists anywhere
```

That was correct before TIP-60, but TIP-60 intentionally adds the first LocalDev/non-production in-memory implementation. The accepted corrected scope authorizes updating:

```text
tests/TagEkyc.ArchTests/Tip55MetadataReferenceBoundaryTests.cs
tests/TagEkyc.ArchTests/Tip57MetadataReferenceQuerySemanticsBoundaryTests.cs
```

The new invariant is:

```text
only the exact LocalDev/non-production in-memory implementation is allowed
no production, infrastructure, adapter, API, persistence, database, provider, storage, resolver, raw payload, artifact storage, package completeness, or readiness implementation is allowed
```

This correction does not authorize persistence, public API/Contracts exposure, provider/storage/raw/artifact behavior, package or reliance proof, or readiness/legal/audit/security/production/certification/capability claims.

## TIP Analytical Summary / Intent Ledger

### Intent

Implement a LocalDev/non-production in-memory registry for metadata references so LocalDev code can register and query metadata references without treating metadata as evidence, artifact access, package completeness, provider evidence, or readiness proof.

### Expected Outcome

After TIP-60:

- `LocalDevInMemoryMetadataReferenceRegistry` exists under `TagEkyc.Application.LocalDev`.
- `IMetadataReferenceRegistry` maps to that implementation in `Program.cs` LocalDev composition.
- Registered metadata references can be queried in memory.
- Unknown references return non-success / not reliable query results.
- TIP-55/TIP-57 metadata-only semantics remain intact.
- Old architecture sentinels now allow exactly one LocalDev implementation and continue to reject broader runtime drift.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Add exactly one LocalDev in-memory implementation. | TIP-59 authorized Group A runtime metadata behavior only. | New Application LocalDev class. | No durability, provider, storage, API, or readiness claim. |
| Register the implementation in `Program.cs`. | TIP-59 authorized LocalDev DI mapping only. | Existing API composition file only. | No endpoint or public DTO exposure. |
| Update TIP-55/TIP-57 stale sentinels. | Required validation would otherwise fail the intentionally added implementation. | Two existing architecture test files now guard exact LocalDev exception. | Not a broad allowance. |
| Keep query semantics on existing `MetadataReferenceQueryResult`. | TIP-55/TIP-57 own metadata-only semantics. | No contract/domain changes. | No evidence reliance proof. |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Add persistence/schema/database behavior. | Rejected. | Outside Group A and forbidden. | Future reviewed Group B or persistence packet only. |
| Add public API/Contracts exposure. | Rejected. | TIP-60 authorizes no endpoint or DTO. | Future explicit API authorization only, if ever. |
| Add provider/storage/resolver/tool behavior. | Rejected. | Group D/B/E work remains blocked. | Later bundle TIPs only. |
| Treat registered metadata as proof. | Rejected. | Core invariant forbids it. | Package/reliance remains packet-gated. |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| Old no-implementation sentinel | Update to exact LocalDev-only exception. | TIP-60 can validate without weakening boundary. | Must fail on any non-LocalDev implementation. |
| `ART-002` runtime metadata behavior | Partially addressed in LocalDev only. | Registry behavior exists for non-production use. | Reference reliance remains denied. |
| `GOV-001`, `ART-001`, `ART-003` through `ART-009` | Carry forward. | Not resolved by TIP-60. | Future bundle-specific reviewed TIPs. |

### Non-Claims

TIP-60 does not prove artifact existence, artifact access, evidence package completeness, provider evidence availability, reference availability for reliance, production readiness, legal sufficiency, audit readiness, security readiness, certification, regulatory approval, or business capability.

### Dispatch Readiness

Implementation dispatch is allowed under the corrected TIP-60 scope.

TIP-60 may edit:

```text
src/TagEkyc.Api/Program.cs
docs/tips/README.md
tests/TagEkyc.ArchTests/Tip55MetadataReferenceBoundaryTests.cs
tests/TagEkyc.ArchTests/Tip57MetadataReferenceQuerySemanticsBoundaryTests.cs
```

TIP-60 may add:

```text
src/TagEkyc.Application/LocalDev/LocalDevInMemoryMetadataReferenceRegistry.cs
tests/TagEkyc.UnitTests/Tip60LocalDevMetadataReferenceRegistryTests.cs
tests/TagEkyc.ArchTests/Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs
docs/tips/tip_60_group_a_localdev_metadata_reference_runtime_registry/tip_60_planning_brief_v0_1.md
docs/tips/tip_60_group_a_localdev_metadata_reference_runtime_registry/tip_60_closeout_v0_1.md
```

## Implementation Plan

1. Add `LocalDevInMemoryMetadataReferenceRegistry` in Application LocalDev.
2. Store metadata records in memory by `MetadataReferenceId`.
3. Register by converting `MetadataReferenceRegistration` to `MetadataReferenceRecord`.
4. Query known references as `MetadataReferenceQueryResult(record)`.
5. Query unknown references as `MetadataReferenceQueryResult(null)`.
6. Add `Program.cs` DI mapping for `IMetadataReferenceRegistry`.
7. Add TIP-60 unit and architecture tests.
8. Update TIP-55/TIP-57 architecture sentinels to allow exactly the LocalDev implementation.

## Expected Tests

Run at minimum:

```powershell
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore --filter Tip60
dotnet test tests/TagEkyc.ArchTests/TagEkyc.ArchTests.csproj --no-restore --filter Tip60
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore --filter "Tip55|Tip57|Tip60"
dotnet test tests/TagEkyc.ArchTests/TagEkyc.ArchTests.csproj --no-restore --filter "Tip55|Tip57|Tip60"
dotnet test TagEkyc.sln --no-restore
git diff --check
git diff --cached --check
git diff --cached --name-only
git status --short
```

## STOP/RRI Gates

STOP before:

- editing files outside the corrected TIP-60 allowlist;
- changing `IMetadataReferenceRegistry`, `MetadataReferenceId`, `MetadataReferenceState`, or `MetadataReferenceQueryResult` semantics;
- adding persistence, schema, migration, database, package, project, or solution changes;
- adding public API/Contracts exposure;
- adding provider/storage/resolver/tool behavior or dependencies;
- handling raw payloads or artifact bytes;
- adding restricted artifact access;
- claiming package completeness, reference availability proof, artifact existence/access proof, provider evidence proof, or readiness/legal/audit/security/production/certification/capability;
- staging or committing unrelated dirty files;
- weakening TIP-55/TIP-57 semantics instead of updating stale sentinels narrowly.

## Review Ladder Plan

1. Author implementation pass.
2. V1 deep bounded review of changed files, corrected scope, old sentinel updates, DI, LocalDev implementation, public exposure, forbidden dependencies, and dirty-file boundary.
3. Patch findings inside corrected TIP-60 scope.
4. V2 patch verification.
5. V3 free adversarial review for LocalDev-as-production, metadata-as-proof, API/Contracts, persistence/provider/raw/artifact, readiness wording, false-pass tests, and accidental extra files.
6. Final validation.
7. Commit.
8. Closeout reviewer pass.
9. Patch closeout if needed.
10. Final report.

## Convergence / Learning Rule

TIP-60 may use up to five total review rounds. If review does not converge, STOP without commit and produce a Review Failure Analysis with unresolved findings, root cause, changed files, risk classification, recommended next action, lessons learned, and prompt/rule improvement suggestions.

TIP-60 closeout must include lessons learned / process feedback covering correct assumptions, wrong or patched assumptions, TIP-59 authorization sufficiency, bundle-TIP execution, and improvements before TIP-61.
