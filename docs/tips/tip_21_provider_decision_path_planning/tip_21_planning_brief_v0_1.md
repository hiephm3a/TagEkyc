# TIP-21 Provider Decision Path Planning Brief v0.1

**File:** `docs/tips/tip_21_provider_decision_path_planning/tip_21_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-16
**Baseline:** `ad24ded`
**Purpose:** Define the decision path and evidence requirements for a future durable metadata provider decision. Do not choose a provider. Do not recommend a provider. Do not implement anything.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-21 as a docs-only provider decision path planning brief.
- Defined the governed path for future provider decision evidence, proof mapping, STOP/RRI gates, and anti-stealth controls.
- Kept provider choice, concrete provider/package/tool names, data-access style decisions, schema/index/migration ownership decisions, repository implementation, Infrastructure adapter work, LocalDev adapter work, backup/recovery claims, readiness claims, and SignFlow dependency out of scope.

## 1. Baseline

TIP-21 follows the closed provider-neutral durable metadata planning sequence:

- HEAD `ad24ded`.
- Latest commit `ad24ded docs: close TIP-20 provider evaluation criteria`.
- TIP-17 closed as provider-neutral durable metadata repository boundary.
- TIP-18 closed as DB/provider posture hold.
- TIP-19 closed as transaction/audit consistency semantics planning.
- TIP-20 closed as provider evaluation criteria.

TIP-21 is decision-process planning only. It defines how a future provider decision must be made, which evidence is required, which proof packet must exist before a choice, and which gates stop the work. It does not select, compare, score, recommend, or implement any provider, category, package, tool, schema, migration, index, adapter, or runtime behavior.

## 2. Section 0 Repo Evidence

Read-only evidence:

```text
Repository root: D:/Task/Remote Signing/TagEkyc
HEAD: ad24ded
Latest commit: ad24ded docs: close TIP-20 provider evaluation criteria
Latest accepted validation supplied by prompt: dotnet test TagEkyc.sln --no-restore = 103 passed, 0 failed, 0 skipped
```

Known dirty files before TIP-21 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

Boundary evidence:

- `IDurableMetadataRepository` and `DurableMetadataWriteSet` remain the current Application boundary for future durable metadata persistence.
- `DurableMetadataWriteSet` remains the same-boundary semantic unit from TIP-19 for session metadata, audit identity metadata, and any included evidence package or completion authority metadata.
- No runtime implementation is opened by TIP-21.
- No provider, data-access style, schema, migration, index, package, repository implementation, Infrastructure adapter, LocalDev adapter, outbox, webhook, retry, backup/recovery, readiness, or SignFlow dependency is opened.
- No raw artifact, biometric, provider payload, vault byte, raw secret, hashed secret, token, private key, API key, or reconstructable credential material storage is opened.

## 3. Purpose

TIP-21 defines the governed path for how a future durable metadata provider decision will be made.

The path exists to prevent provider choice by stealth. A later decision must be evidence-led, measured against TIP-20 criteria, mapped back to TIP-19 consistency semantics, checked against the TIP-17 repository boundary, and explicitly reviewed before any implementation or dependency work begins.

TIP-21 does not choose a provider. It does not recommend a provider. It does not implement anything.

## 4. Decision Path Stages

Future provider decision work must move through these stages in order:

| Stage | Name | Required outcome |
| --- | --- | --- |
| Stage A | Candidate category inventory | List candidate categories only: relational database, document database, event store, cloud managed database, and LocalDev-only adapter where relevant. Do not name concrete providers, packages, or tools. |
| Stage B | Evidence packet requirements | Derive required evidence from every TIP-20 criterion before any category is preferred. |
| Stage C | TIP-19 proof mapping | Map candidate evidence to same-boundary `DurableMetadataWriteSet`, idempotency, audit/business orphan prevention, package/completion finalization, and unknown/interrupted outcome semantics. |
| Stage D | TIP-17 boundary check | Prove `IDurableMetadataRepository` remains the Application port and provider mechanics stay outside Domain, Contracts, consumers, and SignFlow. |
| Stage E | Forbidden-data and credential-material review | Prove the path stores safe metadata and references only, with no raw artifact, biometric, provider payload, vault byte, credential material, token, key, or reconstructable secret storage. |
| Stage F | Operational posture review | Evaluate operational suitability, including backup/recovery suitability as future criteria only, without claiming recovery objectives, restore capability, durability, readiness, or recoverability. |
| Stage G | LocalDev-vs-production sequencing decision | Decide whether LocalDev-only adapter planning happens before, after, or separately from production provider choice, and prove it cannot imply production durability. |
| Stage H | Homeowner/GPT gate | Stop for homeowner/GPT review before any concrete provider decision, concrete package/tool decision, schema/index/migration ownership decision, repository implementation, or adapter work. |
| Stage I | Separate provider decision TIP | Open a later provider decision TIP only if the evidence packet is sufficient and the STOP/RRI list is explicit. |

## 5. Required Future Evidence Packet

A later provider decision TIP must provide the following proof packet without choosing a provider inside TIP-21:

- Candidate category.
- Fit against every TIP-20 matrix criterion.
- Proof plan for TIP-19 same-boundary `DurableMetadataWriteSet`.
- Idempotency identity proposal.
- Duplicate suppression and conflict handling proposal.
- Unknown/interrupted outcome handling proposal.
- Audit/business orphan prevention proof.
- Package/completion finalization consistency proof.
- Concurrency/versioning proof.
- Transaction/isolation or accepted equivalent explanation.
- Repository boundary preservation proof.
- Forbidden-data controls proof.
- Schema/index/migration governance proposal, future only.
- Backup/recovery suitability analysis, with no recovery objective or capability claim.
- LocalDev separation analysis.
- Test/proof feasibility.
- Dependency impact.
- STOP/RRI list.
- Rollback/abandon criteria.

The proof packet must be complete before any provider decision can be accepted. Missing evidence stops the decision and returns the work to planning.

## 6. LocalDev Sequencing

TIP-21 does not decide LocalDev adapter implementation and does not authorize a LocalDev adapter.

TIP-21 only defines the LocalDev sequencing decision question:

- Should LocalDev-only adapter planning happen before production provider choice?
- Should LocalDev-only adapter planning be separate and explicitly non-production?
- What proof prevents LocalDev behavior from implying real durability, backup/recovery, recoverability, or production readiness?

Any future LocalDev-only adapter planning must remain separate from production provider choice unless a later reviewed TIP explicitly accepts a different sequencing plan. A LocalDev-only adapter must never become the production path by default, by convenience, or by absence of a production decision.

## 7. Provider Decision Anti-Patterns

Future work must reject these provider decision anti-patterns:

- Choosing by familiarity.
- Choosing by existing machine availability.
- Adding a package before decision.
- Writing schema first and then retrofitting criteria.
- Implementing a LocalDev adapter that becomes production by accident.
- Allowing provider mechanics to leak into Domain, Application, Contracts, or consumers.
- Accepting partial business/audit persistence.
- Using SignFlow internals as persistence infrastructure.
- Treating backup/recovery as implied by using a database.

## 8. Hard Non-Goals

TIP-21 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, or dependency changes;
- database provider selection;
- concrete provider selection;
- concrete package/tool selection;
- concrete data-access style decision;
- runtime persistence context;
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

## 9. STOP/RRI Gates

Stop and request review before any later work if any of these become necessary:

| Gate | STOP/RRI condition |
| --- | --- |
| Concrete DB/provider name | Any concrete database or provider is named, compared, recommended, accepted, or selected. |
| Concrete package/tool name | Any concrete package or tool is named, added, compared, recommended, accepted, or selected. |
| Data-access style decision | Any concrete data-access style decision is required. |
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
| Insufficient evidence packet | The future decision packet omits required evidence or leaves proof gaps unresolved. |
| Criteria bypass pressure | The work is pressured to skip or soften the TIP-20 criteria matrix. |

## 10. Anti-Stealth Provider Choice Controls

TIP-21 prevents provider choice by stealth through these controls:

- Candidate discussion stays at category level until a later provider decision TIP is opened.
- No dependency, package, schema, migration, index, repository implementation, Infrastructure adapter, or LocalDev adapter work can happen before the decision proof packet is reviewed.
- No LocalDev-only path can be treated as evidence of production durability, backup/recovery, recoverability, or readiness.
- No public contracts, Domain models, Application ports, consumers, or SignFlow paths may expose provider mechanics.
- Any concrete name, concrete package/tool, implementation step, or readiness claim triggers STOP/RRI before further work.

## 11. Validation

Recommended docs-only validation:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_21_provider_decision_path_planning/tip_21_planning_brief_v0_1.md
git diff --check
git status --short
```

Do not run `dotnet test` unless docs-only scope is accidentally violated.

## 12. Recommended Next Action

Keep TIP-21 as draft for homeowner/GPT review.

Do not stage or commit until reviewed. No implementation is recommended next.

A later slice may be one of:

- LocalDev-only adapter planning;
- provider decision evidence packet;
- backup/recovery requirements planning;
- outbox relationship planning.

Do not create any of those files in TIP-21.
