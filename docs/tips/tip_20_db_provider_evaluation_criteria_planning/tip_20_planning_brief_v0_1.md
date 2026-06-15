# TIP-20 DB / Provider Evaluation Criteria Planning Brief v0.1

**File:** `docs/tips/tip_20_db_provider_evaluation_criteria_planning/tip_20_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-16
**Baseline:** `57e53f83042d3e69475f7530df6424018cdb5705`
**Purpose:** Define criteria for evaluating a future durable metadata provider. Do not choose a provider. Do not recommend a provider. Do not implement anything.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-20 as a docs-only provider evaluation criteria planning brief.
- Kept provider selection, concrete provider/package/tool names, data-access strategy, schema, index, migration, repository implementation, Infrastructure adapter, LocalDev adapter, backup/recovery claims, readiness claims, and SignFlow dependency out of scope.
- Carried forward TIP-19 same-boundary `DurableMetadataWriteSet` semantics as evaluation criteria for later provider decision work.
- Defined criteria before choice; no category is scored, compared, recommended, accepted, or selected.

## 1. Context / Baseline

TIP-20 follows the accepted S2 planning sequence:

- HEAD `57e53f83042d3e69475f7530df6424018cdb5705`.
- Latest commit `57e53f8 docs: close TIP-19 transaction audit consistency`.
- TIP-17 closed as the provider-neutral durable metadata repository boundary.
- TIP-18 closed as DB/provider posture hold.
- TIP-19 closed as transaction/audit consistency semantics planning.

TIP-20 is criteria before choice. It defines what a later provider decision must prove, but it does not score, compare, recommend, select, or implement any provider, category, package, tool, schema, migration, index, adapter, or runtime behavior.

## 2. Section 0 Repo Evidence

Read-only evidence:

```text
Repository root: D:/Task/Remote Signing/TagEkyc
HEAD: 57e53f83042d3e69475f7530df6424018cdb5705
Latest commit: 57e53f8 docs: close TIP-19 transaction audit consistency
Latest accepted validation supplied by prompt: dotnet test TagEkyc.sln --no-restore = 103 passed, 0 failed, 0 skipped
```

Known dirty files before TIP-20 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

Boundary evidence:

- `IDurableMetadataRepository` and `DurableMetadataWriteSet` remain the current Application boundary for future durable metadata persistence.
- `DurableMetadataWriteSet` remains the same-boundary semantic unit from TIP-19 for session metadata, audit identity metadata, and any included evidence package or completion authority metadata.
- No runtime implementation is opened by TIP-20.
- No concrete provider, data-access strategy, schema, migration, index, package, repository implementation, Infrastructure adapter, LocalDev adapter, outbox, webhook, retry, backup/recovery, readiness, or SignFlow dependency is opened.
- No raw artifact, biometric, provider payload, vault byte, raw secret, hashed secret, token, private key, API key, or reconstructable credential material storage is opened.

## 3. Purpose

TIP-20 defines provider-neutral criteria for evaluating a future durable metadata provider.

The criteria exist so that a later provider decision TIP can prove fit against TagEkyc's accepted durable metadata semantics before any implementation begins. TIP-20 does not choose a provider. It does not recommend a provider. It does not implement anything.

## 4. Evaluation Criteria

A later provider decision must prove the selected path can satisfy the following criteria:

- Ability to satisfy TIP-19 same-boundary `DurableMetadataWriteSet` semantics.
- Append-only audit identity metadata posture, with corrections modeled as additional corrective audit metadata.
- Audit/business orphan prevention for successful business operations.
- Package/completion finalization consistency for evidence package metadata, completion authority metadata, and completed session facts.
- Idempotency identity support before implementation.
- Duplicate suppression for the same idempotency identity with the same facts.
- Conflict detection for the same idempotency identity with different facts.
- Unknown/interrupted write outcome detection without reporting false success.
- Concurrency/versioning support for accepted lifecycle and write-set transitions.
- Transaction/isolation or accepted equivalent strong enough to satisfy TIP-19 same-boundary semantics.
- Provider-neutral repository boundary preservation, with `IDurableMetadataRepository` remaining the Application port.
- Forbidden data controls: no raw artifacts, biometrics, provider payloads, vault bytes, raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material.
- Schema/index/migration governance suitability as future criteria only.
- Backup/recovery suitability as future evaluation criteria only, with no recovery objective, restore capability, or recoverability claim now.
- Operational complexity and LocalDev separation, including clear distinction between any future production path and any LocalDev-only adapter.
- Test/proof feasibility for same-boundary writes, idempotency, conflicts, unknown outcomes, and forbidden-data absence.
- Compatibility with current TagEkyc architecture, including Domain/Application boundary isolation and existing consumer boundaries.
- No SignFlow runtime/source/database/package/internal-model dependency.

Later provider decision must prove how schema/index/migration ownership would be governed if that provider path is selected.

## 5. Provider Categories

TIP-20 mentions only future evaluation categories:

- relational database;
- document database;
- event store;
- cloud managed database;
- LocalDev-only adapter.

These are categories for later evaluation only. TIP-20 does not score, compare, recommend, accept, or select any category.

## 6. Provider Evaluation Matrix

| Criterion | Why it matters | Later decision must prove | Status in TIP-20 |
| --- | --- | --- | --- |
| Same-boundary write-set semantics | TIP-19 requires included business and audit metadata to become durable together or not be accepted as durable truth. | The provider path can commit, reject, or detect unknown outcomes for one `DurableMetadataWriteSet` without partial accepted truth. | Criteria only |
| Append-only audit identity metadata | Audit identity facts must remain traceable and corrections must not erase prior facts. | Audit metadata can be appended and corrected through additional records without mutation of accepted audit identity facts. | Criteria only |
| Audit/business orphan prevention | Successful business operations must not lack required audit identity metadata, and audit must not stand as successful-operation audit without business facts. | Business and required audit facts share the accepted boundary or an equivalent proof prevents orphaned acceptance. | Criteria only |
| Package/completion finalization consistency | Finalization truth depends on evidence package metadata, completion authority metadata, and session completion facts agreeing. | Finalization metadata cannot be partially accepted as completed truth. | Criteria only |
| Idempotency identity support | Retries, replays, and interrupted outcomes require a stable operation identity. | A stable identity can be defined before implementation and applied to write-set acceptance. | Criteria only |
| Duplicate suppression | Retrying the same operation with the same facts must not create duplicate business or audit truth. | Same identity plus same facts returns prior accepted result or equivalent already-applied outcome. | Criteria only |
| Conflict detection | Same identity with different facts must not be treated as another valid write. | Same identity plus different facts is detected and reported as conflict. | Criteria only |
| Unknown/interrupted outcome detection | Provider or process interruption must not be reported as success from incomplete evidence. | Unknown outcomes can be detected and held as non-success until reconciled by an accepted later design. | Criteria only |
| Concurrency/versioning | Lifecycle transitions need protection from conflicting writers and stale assumptions. | The provider path supports versioning, expected-state checks, or accepted equivalent conflict control. | Deferred |
| Transaction/isolation or accepted equivalent | Same-boundary semantics require more than independent best-effort writes. | The provider path proves the chosen consistency mechanism is strong enough for TIP-19. | Deferred |
| Repository boundary preservation | Persistence mechanics must not leak into Domain, Contracts, consumers, or SignFlow. | `IDurableMetadataRepository` remains the Application boundary and provider details stay outside it. | Criteria only |
| Forbidden data controls | TIP-17 through TIP-19 permit safe metadata and references only. | The provider path prevents raw artifacts, biometrics, provider payloads, vault bytes, secrets, tokens, keys, and reconstructable credential material. | Criteria only |
| Schema/index/migration governance | Durable persistence shape must be governed before implementation. | Later provider decision must prove how schema/index/migration ownership would be governed if that provider path is selected. | Deferred |
| Backup/recovery suitability | Durable metadata must eventually have reviewed operational expectations. | Later provider decision can evaluate backup/recovery suitability without claiming recovery objectives or recoverability now. | Deferred |
| Operational complexity and LocalDev separation | Non-production local behavior must not imply production durability. | The provider path separates production evaluation from any LocalDev-only adapter and states operational burden. | Criteria only |
| Test/proof feasibility | Criteria must be verifiable before implementation is accepted. | A later implementation slice can prove same-boundary, idempotency, conflict, unknown-outcome, and forbidden-data behavior. | Criteria only |
| Architecture compatibility | The provider path must fit current TagEkyc boundaries. | No provider mechanics leak into existing public contracts, Domain, consumers, or SignFlow internals. | Criteria only |
| SignFlow independence | SignFlow remains a consumer profile, not base persistence infrastructure. | No SignFlow runtime, source, database, package, network, or internal model dependency is required. | Criteria only |

## 7. Hard Non-Goals

TIP-20 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, or dependency changes;
- database provider selection;
- concrete provider selection;
- concrete package/tool selection;
- data-access implementation-style decision;
- runtime persistence context type;
- migrations, schema, indexes, generated provider scripts, or migration tooling;
- durable repository implementation;
- Infrastructure adapter implementation;
- LocalDev durable metadata adapter implementation;
- outbox, webhook, retry, delivery ledger, dispatcher, signing, replay, or delivery implementation;
- backup/recovery implementation, recovery objectives, restore capability, operational durability, or recoverability claim;
- production auth implementation;
- credential store, API-key store, secret backend, raw secret storage, hashed secret storage, token storage, private key storage, or certificate lifecycle;
- raw artifact, biometric, provider payload, vault object, or evidence package raw storage;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- production readiness, pilot readiness, certification readiness, legal reliance, external audit reliance, real durability, or durable audit-store implementation claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 8. STOP/RRI Gates

Stop and request review before any later work if any of these become necessary:

| Gate | STOP/RRI condition |
| --- | --- |
| Concrete provider selection | Any concrete database/provider is named, compared, recommended, accepted, or selected. |
| Data-access style | Any concrete data-access strategy or tool decision is required. |
| Package/project change | Any project, solution, package, or dependency change is required. |
| Schema/migration/index ownership | Any schema, migration, index, generated provider script, or ownership decision is required. |
| Repository implementation | `IDurableMetadataRepository` is implemented or its runtime behavior is changed. |
| Infrastructure adapter | Infrastructure persistence adapter work is introduced. |
| LocalDev adapter | A LocalDev durable metadata adapter is introduced. |
| Outbox inclusion | Outbox, webhook, retry, delivery ledger, dispatcher, signing, replay, or delivery behavior is included. |
| Backup/recovery claim | Recovery objectives, restore capability, operational durability, or recoverability is claimed. |
| Credential material storage | Raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material would be stored. |
| Raw artifact/vault data storage | Raw artifacts, biometrics, provider payloads, vault bytes, vault objects, or evidence package raw data would be stored. |
| Public contract change | Public API/DTO/JSON/status/error behavior would change. |
| SignFlow dependency | SignFlow runtime, source, database, package, network, or internal-model dependency is required. |
| Readiness claim | Production, pilot, certification, legal, external audit, real durability, or durable audit-store readiness is implied. |
| TIP-19 semantics gap | The provider path cannot satisfy same-boundary `DurableMetadataWriteSet` semantics. |
| Idempotency identity gap | The provider path cannot define stable idempotency identity before implementation. |
| Audit/business orphaning gap | The provider path cannot prevent audit/business orphaning for successful business operations. |
| Unknown outcome gap | The provider path cannot detect unknown/interrupted write outcomes without reporting false success. |
| Concrete name leakage | Any concrete provider/package/tool name appears in the draft. |

## 9. Validation

Recommended docs-only validation:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_20_db_provider_evaluation_criteria_planning/tip_20_planning_brief_v0_1.md
git diff --check
git status --short
```

Do not run `dotnet test` unless docs-only scope is accidentally violated.

## 10. Recommended Next Action

Keep TIP-20 as a draft for homeowner/GPT review.

Do not stage or commit until reviewed. A later provider decision TIP must remain separate and must prove the selected path against these criteria before any runtime implementation, package/project change, schema/index/migration ownership decision, repository implementation, adapter, backup/recovery claim, readiness claim, or SignFlow dependency is introduced.
