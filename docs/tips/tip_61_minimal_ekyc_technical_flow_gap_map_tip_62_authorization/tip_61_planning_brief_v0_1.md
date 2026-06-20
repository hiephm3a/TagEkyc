# TIP-61 Minimal eKYC Technical Flow Gap Map and TIP-62 Integration Authorization Planning Brief v0.1

**File:** `docs/tips/tip_61_minimal_ekyc_technical_flow_gap_map_tip_62_authorization/tip_61_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Accepted - docs-only authorization envelope
**Date:** 2026-06-21
**Baseline:** `f4d82d2814b4af0ba7a1da9e34543d9fd0e6659c feat: implement TIP-60 localdev metadata reference registry`
**Purpose:** Record the current minimal technical eKYC flow, identify where the TIP-60 registry is isolated, and authorize the exact future TIP-62 internal integration envelope.

## Changelog

### v0.1 - Initial planning brief

- Opened TIP-61 as docs-only flow gap mapping and TIP-62 authorization.
- Incorporated the read-only pre-TIP-61 audit evidence.
- Selected Option A: internal metadata reference registry integration only.
- Defined the exact future TIP-62 objective, allowed file scope, required tests, forbidden scope, GOV/ART gate mapping, no-extra-planning-gate rule, and review ladder requirements.

## Repo Evidence / Dry-Run Audit Incorporation

Read-only audit evidence before TIP-61 docs edits:

```text
git rev-parse HEAD
f4d82d2814b4af0ba7a1da9e34543d9fd0e6659c

git status --short
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
?? docs/00_GDRIVE_FILE_INDEX.md
```

Known unrelated dirty files remain out of scope and must not be staged or committed:

```text
.gitignore
docs/00_AGENT_COORDINATION_BUS.md
docs/00_GDRIVE_FILE_INDEX.md
```

Audit evidence:

| Area | Repo evidence | TIP-61 finding |
| --- | --- | --- |
| Existing endpoint inventory | `src/TagEkyc.Api/Program.cs` maps `/health`, `/build`, `/`, and `MapVerificationSessionEndpoints()`. `src/TagEkyc.Api/VerificationSessionEndpoints.cs` maps create session, get session summary, append capture artifact, append evidence result, complete session, and get evidence package. | Minimal LocalDev HTTP flow is already connected; no metadata/reference endpoint exists. |
| Existing application ports/services | `src/TagEkyc.Application/Ports/ApplicationServicePorts.cs` defines session commands/queries, capture commands, trusted evidence commands, completion commands, package queries, and completion notification queries. | Application service ports exist for current flow; none expose metadata/reference query or registration. |
| Current capture/evidence service | `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` injects session, capture artifact, evidence result, audit, and policy dependencies. | No product/session/capture/status service injects or calls `IMetadataReferenceRegistry`. |
| Metadata registry runtime | `src/TagEkyc.Application/Ports/MetadataReferencePorts.cs` defines `IMetadataReferenceRegistry`; `src/TagEkyc.Application/LocalDev/LocalDevInMemoryMetadataReferenceRegistry.cs` implements it; `src/TagEkyc.Api/Program.cs` registers it in LocalDev DI. | TIP-60 registry is runtime-available through DI but isolated from product flow. |
| Tests / sentinel risks | `tests/TagEkyc.ArchTests/Tip55MetadataReferenceBoundaryTests.cs`, `Tip57MetadataReferenceQuerySemanticsBoundaryTests.cs`, and `Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs` enforce exactly one LocalDev implementation and no API/Contracts exposure. `tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs` asserts no specialized evidence endpoints. | Public metadata/reference endpoint would require explicit route/API/Contracts/sentinel updates. Internal integration should avoid that. |
| GOV/ART gate lineage | TIP-35 registers `GOV-001` and `ART-001` through `ART-009`; TIP-59/TIP-60 carry metadata registry work as Group A only. | TIP-61 resolves no GOV/ART gate; it only maps which gates TIP-62 may partially advance or must carry. |

UNVERIFIED: no live GDrive sync/hash table was available from repo evidence during TIP-61 drafting.

## Current Minimal Technical eKYC Flow

Current connected LocalDev technical flow:

```text
create session
-> append capture artifact hash/metadata hash
-> append trusted evidence result with payload hash/sanitized summary
-> query session summary
-> complete
-> query evidence package
-> audit events
```

Repo evidence:

| Step | Evidence |
| --- | --- |
| Create session | `VerificationSessionEndpoints.CreateAsync` calls `IVerificationSessionCommands.CreateAsync`; `VerificationSessionApplicationService` appends `SESSION_CREATED`. |
| Append capture artifact hash/metadata hash | `VerificationSessionEndpoints.AppendCaptureArtifactAsync` calls `ICaptureArtifactCommands.AppendCaptureArtifactAsync`; `VerificationEvidenceApplicationService` validates artifact/metadata hashes, appends `CaptureArtifact`, and appends `CAPTURE_ARTIFACT_RECORDED`. |
| Append trusted evidence result | `VerificationSessionEndpoints.AppendEvidenceResultAsync` calls `ITrustedEvidenceResultCommands.AppendEvidenceResultAsync`; `VerificationEvidenceApplicationService` validates input artifacts, sanitized summary, and payload hash before appending `EvidenceResult` and `EVIDENCE_RESULT_RECORDED`. |
| Query session summary | `VerificationSessionEndpoints.GetAsync` calls `IVerificationSessionQueries.GetSummaryAsync`. |
| Complete | `VerificationSessionEndpoints.CompleteAsync` calls `IVerificationSessionCompletionCommands.CompleteAsync`. |
| Query evidence package | `VerificationSessionEndpoints.GetEvidencePackageAsync` calls `IEvidencePackageQueries.GetEvidencePackageAsync`. |
| Audit events | Session, capture/evidence, and completion services append audit events through `IAuditEventRepository`. |

Specific current gap:

```text
metadata reference registry is not part of that flow yet.
No product/session/capture/status service currently injects or calls IMetadataReferenceRegistry.
No API endpoint uses IMetadataReferenceRegistry.
```

The highest-value immediate gap is not that the whole flow is missing.

The immediate gap is that TIP-60 metadata registry is implemented but isolated from the existing capture/evidence/session flow.

## Core Metadata Non-Proof Invariant

Preserve:

```text
metadata reference query result != artifact existence proof
metadata reference query result != artifact access proof
metadata reference query result != evidence package completeness proof
metadata reference query result != provider evidence availability proof
metadata reference query result != production readiness proof
metadata reference query result != readiness/legal/audit/security/certification/capability proof
```

TIP-61 and TIP-62 must treat metadata registration/query as internal metadata state only.

## Flow Area Classification

| Area | Classification | Evidence / notes |
| --- | --- | --- |
| Create/init eKYC session | Implemented and connected | Endpoint, application port, service, LocalDev repository, and tests exist. |
| Capture/register evidence reference | Implemented and connected for capture artifact hashes/metadata hashes; metadata reference registry is not connected | Capture artifact records are accepted; registry registration is absent. |
| Metadata reference registry runtime | Implemented but isolated | `IMetadataReferenceRegistry` has a LocalDev in-memory implementation and DI mapping. |
| Query metadata/reference status | Port/contract exists only internally; no product/API flow | `QueryAsync` exists; no endpoint or application service flow calls it. |
| Produce technical eKYC result | Implemented and connected for current LocalDev completion flow | Completion service exists; no metadata registry dependency. |
| Evidence summary/package | Implemented and connected for current LocalDev package summary | Package query exists; package completeness proof remains denied. |
| Audit trail | Implemented and connected for current flow | Audit repository and events exist. |
| Access/auth/security controls | Partially addressed by current code | LocalDev API key/category/scope controls exist; production security remains not claimed. |
| Artifact lifecycle/storage | Docs-only / blocked by gate | `ART-001`, `ART-004`, `ART-005`, `ART-006`, `ART-008` remain later bundle work. |
| Provider/raw payload | Missing / blocked by gate | `ART-009` blocks provider raw payload work; current trusted evidence accepts payload hashes and sanitized summaries only. |

## TIP Analytical Summary / Intent Ledger

### Intent

Create a docs-only but actionable map of the current minimal eKYC technical flow and authorize the smallest safe next implementation that connects TIP-60 metadata registry behavior into product flow.

### Expected Outcome

After TIP-61:

- the current connected flow is recorded with repo evidence;
- TIP-60 registry isolation is explicit;
- Option A is selected as the next implementation candidate;
- future TIP-62 has exact allowed files, tests, non-goals, and STOP/RRI gates;
- no additional planning-only TIP is allowed before TIP-62 unless concrete read-only repo evidence makes the TIP-62 envelope unsafe or impossible.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Select Option A: internal integration only. | Registry is runtime-available in LocalDev DI and the capture flow already accepts metadata/hash-only records. | TIP-62 may inject and call `IMetadataReferenceRegistry` inside the existing capture/evidence application flow. | No public API/Contracts exposure or proof semantics. |
| Register metadata references when accepted capture artifact metadata/hash-only records are accepted. | This is the smallest product-useful connection from registry to flow. | TIP-62 focuses on capture artifact acceptance path. | Registration is not artifact existence/access/package/provider/readiness proof. |
| Keep public metadata/reference endpoint out of TIP-62. | Route/Contracts/sentinel updates are higher risk and not needed for the immediate gap. | No endpoint or Contracts DTO files are allowed. | No public query capability claim. |
| Require tests to prove internal registration and non-proof invariants. | Prevents false reliance and stale sentinel drift. | Unit/API/arch tests may be adjusted only where listed below. | Tests are LocalDev/internal proof only, not production proof. |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Option B: public metadata/reference status endpoint. | Rejected for TIP-62. | Requires route/Contracts/sentinel updates and is not needed now. | Future explicit API authorization only. |
| Option C: defer metadata registry integration. | Rejected unless concrete blocker appears. | TIP-60 registry is available and isolated; another vague planning TIP would not advance product flow. | STOP/RRI only for evidence-backed blocker. |
| Persistence/provider/storage/raw/artifact access integration. | Forbidden. | Outside Group A and outside immediate internal integration. | Later reviewed bundle/packet. |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| TIP-60 registry isolated from flow | Authorize TIP-62 internal integration. | Product-useful LocalDev metadata registration can start next. | Must stay internal and non-proof. |
| Public metadata/reference query | Defer. | No endpoint authorized. | Future API/Contracts/sentinel TIP only if needed. |
| GOV/ART gates | Carry explicitly. | No gate silently resolved. | Later bundle work remains required. |

### Non-Claims

TIP-61 does not implement code and does not prove artifact existence, artifact access, evidence package completeness, provider evidence availability, reference availability for reliance, production readiness, legal sufficiency, audit readiness, security readiness, certification, regulatory approval, or business capability.

### Dispatch Readiness

Future TIP-62 implementation dispatch is allowed only for:

```text
LocalDev internal metadata reference flow integration
```

Expected TIP-62 objective:

```text
Connect the LocalDev metadata reference registry into the existing capture/evidence application flow so accepted capture artifact metadata/hash-only records register metadata references, while preserving all non-proof invariants and without adding public API/Contracts exposure.
```

No additional planning-only TIP is allowed before TIP-62 unless read-only repo evidence identifies a concrete blocker that makes the TIP-62 internal integration envelope unsafe or impossible.

## Product-Flow Decision

Selected:

```text
Option A: internal integration only
```

TIP-62 may:

- inject `IMetadataReferenceRegistry` into the existing capture/evidence application flow;
- register metadata references when accepted capture artifact metadata/hash-only records are accepted;
- test internal query after capture/evidence flow;
- preserve all non-proof invariants.

TIP-62 must not:

- expose a public metadata/reference API endpoint;
- add or change Contracts DTOs;
- add persistence, provider/storage/resolver/tool selection, raw payload handling, artifact byte persistence, restricted artifact access, package completeness proof, evidence availability proof, readiness proof, or production capability claims.

Option B is not selected because repo evidence shows no endpoint currently uses `IMetadataReferenceRegistry`, and adding one would require explicit route/API/Contracts/sentinel updates.

Option C is rejected unless a concrete blocker is found because it would leave the implemented TIP-60 registry isolated from product flow without new evidence.

## Exact TIP-62 Authorization Envelope

TIP-62 is authorized only for:

```text
LocalDev internal metadata reference flow integration
```

Allowed TIP-62 files:

```text
src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs
src/TagEkyc.Api/Program.cs
tests/TagEkyc.UnitTests/Tip05EvidenceApplicationTests.cs
tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs
tests/TagEkyc.UnitTests/Tip60LocalDevMetadataReferenceRegistryTests.cs
tests/TagEkyc.ArchTests/Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs
docs/tips/README.md
docs/tips/tip_62_localdev_metadata_reference_flow_integration/tip_62_planning_brief_v0_1.md
docs/tips/tip_62_localdev_metadata_reference_flow_integration/tip_62_closeout_v0_1.md
```

Authorization notes:

| Candidate | TIP-62 disposition |
| --- | --- |
| `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` | Allowed; this is the existing capture/evidence flow service and exact isolation point. |
| `src/TagEkyc.Application/Ports/ApplicationServicePorts.cs` | Not authorized by default; existing command/query interfaces are sufficient for internal integration. Only STOP/RRI if compile evidence proves a signature change is unavoidable. |
| `src/TagEkyc.Application/Ports/MetadataReferencePorts.cs` | Not authorized; existing registry contracts are sufficient. |
| `src/TagEkyc.Api/Program.cs` | Allowed only if constructor/DI wiring must remain explicit and tests must mirror production composition; no routes. |
| `tests/TagEkyc.UnitTests/Tip05EvidenceApplicationTests.cs` | Allowed; existing service fixture needs registry dependency and internal registration assertions. |
| `tests/TagEkyc.UnitTests/Tip05ApiPipelineTests.cs` | Allowed; existing API fixture may need registry DI and no-public-exposure regression checks. |
| `tests/TagEkyc.UnitTests/Tip60LocalDevMetadataReferenceRegistryTests.cs` | Allowed only to add/adjust registry non-proof coverage if directly needed. |
| `tests/TagEkyc.ArchTests/Tip60LocalDevMetadataReferenceRegistryBoundaryTests.cs` | Allowed only to preserve no-public-exposure/no-forbidden-dependency guardrails after integration. |
| TIP-62 docs and `docs/tips/README.md` | Allowed for normal TIP open/close/index bookkeeping. |

TIP-62 must STOP/RRI before touching any file outside this allowed list unless the only blocker is a compile/test fixture constructor update that is impossible to resolve otherwise. In that case TIP-62 must report the blocker rather than silently expanding scope.

## TIP-62 Forbidden Scope

TIP-62 must not authorize:

```text
public metadata/reference API endpoint
Contracts DTO exposure
persistence
schema/migration/database changes
provider/storage/resolver/tool selection
raw provider payload handling
artifact/raw byte persistence
restricted artifact access
package completeness proof
reference availability proof
artifact existence/access proof
provider evidence proof
readiness/legal/audit/security/production/certification/capability claim
```

## TIP-62 Required Tests

TIP-62 tests must prove:

```text
capture/evidence flow registers a metadata reference internally;
metadata registry can query the registered reference after capture/evidence flow;
unknown reference remains non-success / not reliable;
registered metadata still does not prove artifact exists;
registered metadata still does not prove artifact access;
registered metadata still does not prove package completeness;
registered metadata still does not prove provider evidence availability;
registered metadata still does not prove readiness;
no public API/Contracts exposure appears;
no persistence/database/provider/raw/artifact storage dependency appears;
existing session/capture/evidence/package behavior is not weakened;
existing TIP-55/TIP-57/TIP-60 metadata invariants are not weakened.
```

Expected TIP-62 validation should include at minimum targeted Tip05/Tip55/Tip57/Tip60 unit and architecture tests plus `git diff --check`, `git diff --cached --check`, `git diff --cached --name-only`, and `git status --short`. Full solution test remains recommended if runtime code changes compile broadly.

## GOV/ART Gate Mapping

No gate is silently resolved by TIP-61.

| Gate | TIP-61 classification | TIP-62 impact |
| --- | --- | --- |
| `GOV-001` | Carried as non-claim | Does not block TIP-62 internal integration; traceability must remain explicit. |
| `ART-001` | Requires later bundle | Blocks storage/raw artifact persistence; does not block internal metadata registration. |
| `ART-002` | Partially addressed by current code and future internal integration, with no reliance proof | TIP-60 runtime registry and TIP-62 integration partially advance metadata runtime/reference handling only; no reference reliance proof is created. |
| `ART-003` | Requires later bundle | Blocks package completeness proof; does not block internal metadata registration. |
| `ART-004` | Requires later bundle | Retention/expiry policy remains later work. |
| `ART-005` | Requires later bundle | Purge/disposal workflow remains later work. |
| `ART-006` | Requires later bundle | Legal-hold sync remains later work. |
| `ART-007` | Requires later bundle | Access/audit/security controls remain later work except no-exposure guardrails. |
| `ART-008` | Requires later bundle | Metadata-artifact orphan handling remains later work. |
| `ART-009` | Blocked by gate for provider/raw payload work | Provider/raw payload work remains blocked; TIP-62 must not touch provider payload behavior. |

Explicit required statements:

- `ART-002` is partially advanced by registry runtime/integration but no reliance proof is created.
- `ART-001`, `ART-003`, `ART-004`, `ART-005`, `ART-006`, `ART-007`, `ART-008`, and `ART-009` remain carried or blocked for later bundles as applicable.
- No gate is silently resolved by TIP-61.

## Tests / Sentinel Risk

TIP-61 records:

```text
Adding public metadata/reference endpoint would require updating route/API/Contracts/sentinel expectations.
TIP-62 internal integration should avoid public API/Contracts exposure.
No test currently blocks injecting IMetadataReferenceRegistry into an application service if no public API/Contracts exposure, provider/storage dependency, or proof semantics are introduced.
```

Repo evidence:

- `Tip60LocalDevMetadataReferenceRegistryBoundaryTests` rejects API/Contracts metadata exposure and forbidden dependencies.
- `Tip55MetadataReferenceBoundaryTests` and `Tip57MetadataReferenceQuerySemanticsBoundaryTests` allow exactly the TIP-60 LocalDev implementation.
- `Tip05ApiPipelineTests` asserts specialized evidence endpoints are not mapped and should remain a sentinel against accidental endpoint drift.

## No-Extra-Planning-Gate Rule

No additional planning-only TIP is allowed before TIP-62 unless read-only repo evidence identifies a concrete blocker that makes the TIP-62 internal integration envelope unsafe or impossible.

Concrete blockers may include:

- compile evidence that the exact allowed files cannot support constructor/DI/test wiring without a different file;
- a sentinel that fails for the intended internal integration despite no public API/Contracts exposure;
- evidence that registering metadata from accepted capture artifacts would weaken an existing authorization, hash, package, or non-proof invariant.

Vague uncertainty, preference for another gap map, or desire for a public endpoint is not enough to insert another planning-only TIP before TIP-62.

## Review Ladder Plan

TIP-61 follows the Autonomous Slice Review Ladder / Quality Gate from `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`.

Required review checks:

1. V1 deep bounded review of changed TIP docs and README, adjacent flow/service/API/test evidence, GOV/ART gate mapping, scope boundaries, and over-claim risk.
2. V2 patch verification if V1 requires patches.
3. V3 free adversarial review checking whether TIP-61 accidentally authorizes public API/Contracts exposure, treats registry integration as artifact/evidence proof, leaves TIP-62 envelope too vague, silently resolves a GOV/ART gate, or introduces another planning TIP unnecessarily.
4. Closeout review before commit.

If review does not converge after five total review rounds, STOP, do not commit, and produce Review Failure Analysis with lessons learned and prompt/rule improvement suggestions.
