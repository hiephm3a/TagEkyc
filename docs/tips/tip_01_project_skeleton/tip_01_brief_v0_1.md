# TIP-01 Project Skeleton Brief v0.1

**File:** `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md`
**Version:** 0.1
**Status:** Fully accepted as clean S1 skeleton baseline
**Date:** 2026-06-08
**Baseline:** Product Brief v0.1.1
**Purpose:** Defines skeleton-only execution scope for TIP-01. Execution evidence is recorded separately in `tip_01_execution_report_v0_1.md`.

## Changelog

### v0.1 - Stack resolved and skeleton boundary tightened

- Selected .NET 8 backend Web API skeleton.
- Selected xUnit for UnitTests, ContractTests, and ArchTests placeholders.
- Deferred worker host, persistence, migrations, and Docker runtime services.
- Tightened skeleton-only boundary to exclude LLD03 business APIs, eKYC behavior, adapters, cryptography, and raw artifact storage.
- Clarified SignFlow project boundary as TagEkyc-owned transaction-bound profile placeholders only.
- Decision source: user direction and pre-dispatch review accepted TIP-01 for skeleton-only dispatch.
- Rationale: .NET 8 Web API plus xUnit gives a minimal backend skeleton with familiar build/test support while avoiding worker, persistence, Docker runtime services, and business behavior in TIP-01.
- Added dispatch guardrails for Infrastructure placeholder-only scope, contract placeholder data boundaries, persistence exclusion, and basic scope separation.

### Acceptance update - 2026-06-10

- TIP-02A confirmation passed.
- User accepted TIP-02A result and marked TIP-01 as fully accepted.
- Final status: `TIP-01 Project Skeleton: FULLY ACCEPTED AS CLEAN S1 SKELETON BASELINE`.

## 1. Purpose

TIP-01 prepares the repository and project skeleton for TagEkyc after the accepted documentation baseline v0.1.1.

TIP-01 MUST create structure only. It MUST NOT implement eKYC business logic.

## 2. Baseline Inputs

- `docs/00_DOCS_GOVERNANCE.md`
- `docs/00_product_brief_v0_1.md`
- `docs/00_README.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_02_sequence_flows_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/lld_04_engine_adapter_contracts_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/signflow_integration_contract_v0_1.md`
- `docs/tag_ekyc_docs_baseline_closeout_v0_1_1.md`

## 3. TIP-01 Scope

TIP-01 SHOULD create:

- Repository/project skeleton.
- Build/test placeholder setup.
- Basic dependency management files.
- Configuration template files.
- Module/folder boundaries that match the documentation baseline.
- README or developer notes for running future builds/tests.
- Empty or placeholder project references only where needed to verify structure.

Default technical scope:

- Backend stack: .NET 8 Web API skeleton.
- Test framework: xUnit.
- Initial host: Web API only.
- Worker host: deferred.
- Persistence and migrations: deferred.
- Docker runtime services: deferred.

TIP-01 SHOULD prepare module boundaries for:

- Client applications and API key authentication.
- Verification sessions.
- Required checks and policy.
- Capture artifacts.
- Verification checks.
- Evidence results.
- Evidence packages.
- Evidence vault abstraction.
- Audit events.
- Webhook delivery.
- Engine adapter contracts.
- SignFlow integration boundary.
- Operator/admin boundary placeholder.

## 4. TIP-01 Non-Goals

TIP-01 MUST NOT:

- Implement eKYC business logic.
- Implement OCR, NFC, face matching, liveness, fingerprint, or fraud/risk engines.
- Implement cryptography or production signatures.
- Create database migrations.
- Create production schema.
- Implement raw artifact storage.
- Implement SignFlow business workflows.
- Claim production-certified eKYC readiness.
- Implement state transitions, lifecycle validation, RequiredChecks enforcement, evidence aggregation, risk evaluation, or verification session behavior.
- Implement LLD03 business API controllers/routes, except an optional health/build smoke endpoint.
- Fully implement the LLD03 DTO surface in runtime contracts.
- Add fake `PASSED`/`FAILED` adapter responses, engine simulation, or mock verification behavior.

## 5. Proposed Skeleton Shape

The proposed folder/project names are canonical for TIP-01 unless Builder STOP+REPORTs a concrete stack-specific reason. The skeleton SHOULD preserve these boundaries:

```text
src/
  TagEkyc.Api/
  TagEkyc.Application/
  TagEkyc.Domain/
  TagEkyc.Infrastructure/
  TagEkyc.Adapters/
  TagEkyc.Contracts/
  TagEkyc.SignFlow/
tests/
  TagEkyc.UnitTests/
  TagEkyc.ContractTests/
  TagEkyc.ArchTests/
config/
  appsettings.example.*
docs/
```

Suggested responsibility split:

- `Api`: HTTP/API host and request routing only.
- `Application`: use-case orchestration boundary placeholders only.
- `Domain`: namespace placeholders and minimal enum/type placeholders only if required for compilation.
- `Infrastructure`: placeholder-only boundary for future persistence, audit, vault, webhook, and configuration adapters.
- `Adapters`: empty/shell only.
- `Contracts`: placeholder-only contract namespace or minimal unused placeholder types.
- `SignFlow`: TagEkyc-owned transaction-bound profile contract placeholders only.

Boundary constraints:

- `TagEkyc.Adapters` MUST NOT contain mock engine behavior, fake results, engine simulation, raw artifact storage, file storage, or object storage.
- `TagEkyc.Infrastructure` MUST remain empty/placeholder only. It MUST NOT contain persistence, vault, webhook, storage, EF, migrations, repositories, DbContext, schema/model mapping, in-memory persistence behavior, or runtime infrastructure behavior.
- `TagEkyc.SignFlow` MUST NOT reference SignFlow source code, database, runtime packages, or internal models.
- `TagEkyc.SignFlow` placeholder contracts MUST NOT include internal VaultRefs, raw artifact bytes/paths, raw biometric fields, or raw document fields.
- `TagEkyc.Contracts` MUST NOT become a full LLD03 DTO implementation in TIP-01.
- `TagEkyc.Contracts` placeholder contracts MUST NOT include internal VaultRefs, raw artifact bytes/paths, raw biometric fields, or raw document fields.
- `TagEkyc.Api` MUST NOT expose LLD03 business API routes in TIP-01.
- Skeleton boundaries SHOULD preserve distinct future caller categories for business clients, capture agents/device gateways, internal adapters, and operator/admin scopes.

## 6. Stack Decision

Stack selection is resolved for TIP-01:

- .NET 8 backend Web API skeleton.
- xUnit test projects.
- Web API host only.
- No worker host.
- No persistence implementation.
- No migrations.
- No Docker runtime services.

## 7. Acceptance Criteria

TIP-01 is complete when:

- Project skeleton exists and builds with no business logic.
- Clean build succeeds.
- Placeholder tests pass.
- Module boundaries match TagEkyc baseline docs.
- No migrations are created.
- No persistence implementation is created.
- No EF, DbContext, repositories, schema/model mapping, storage adapters, or in-memory persistence behavior is created.
- No controllers/routes implementing LLD03 business API are created.
- No eKYC engines are implemented.
- No cryptography is implemented.
- No raw artifact storage is implemented.
- No internal VaultRefs, raw artifact bytes/paths, raw biometric fields, or raw document fields are added to external contract placeholders.
- No fake adapter results or engine simulation is implemented.
- Api includes only optional health/build smoke endpoint.
- Infrastructure remains empty/placeholder only with no persistence, vault, webhook, storage, EF, migrations, or runtime behavior.
- Basic dependency direction is verified by ArchTests if feasible; if not feasible, Builder explains why in the Completion Report.
- Future caller-category separation is preserved in skeleton boundaries where applicable.
- Documentation explains how to build and test the skeleton.
- Git status contains only intended skeleton/planning changes.

Test project expectations:

- `TagEkyc.UnitTests`: placeholder/smoke tests only.
- `TagEkyc.ContractTests`: smoke-only in TIP-01.
- `TagEkyc.ArchTests`: verifies basic dependency direction if feasible.

## 8. Deferred Questions After TIP-01

- Should adapter shells live in one project or separate per engine family after TIP-01?
- Should Docker/container files be added in a later infrastructure TIP?
- Which persistence approach should be planned after skeleton acceptance?
- Which architecture test library should be used if basic dependency-direction checks are feasible?

## 9. Historical Recommended Action Before Execution

This was the recommended action before TIP-01 execution. TIP-01 has now been executed locally; see `tip_01_execution_report_v0_1.md` for evidence and the current acceptance status.

The original execution instruction was to execute TIP-01 as a skeleton-only .NET 8 Web API change.

Execution should:

- Create a .NET 8 backend Web API skeleton.
- Add placeholder UnitTests, ContractTests, and ArchTests using xUnit.
- Add module projects/folders matching the baseline docs.
- Add only an optional health/build smoke endpoint.
- Defer persistence, migrations, Docker runtime services, cryptography, and all eKYC business logic.
- Treat the proposed project names as canonical unless a concrete stack-specific issue requires STOP+REPORT.
