# TIP-22 LocalDev-Only Durable Metadata Adapter Planning Brief v0.1

**File:** `docs/tips/tip_22_localdev_only_durable_metadata_adapter_planning/tip_22_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-16
**Baseline:** `937bc9c`
**Purpose:** Define whether and how a strictly non-production LocalDev-only durable metadata adapter planning path should be governed before any production provider decision. Do not implement an adapter. Do not choose a provider.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-22 as a docs-only LocalDev-only durable metadata adapter planning brief.
- Defined LocalDev-only boundaries that prevent LocalDev behavior from implying real durability, backup/recovery, production readiness, or provider selection.
- Preserved that TIP-22 does not authorize runtime implementation, provider choice, repository implementation, Infrastructure adapter work, LocalDev adapter work, project/package changes, backup/recovery claims, readiness claims, or SignFlow dependency work.

## 1. Baseline

TIP-22 follows the closed provider-neutral durable metadata planning sequence:

- HEAD `937bc9c`.
- Latest commit `937bc9c docs: close TIP-21 provider decision path`.
- TIP-17 closed as provider-neutral durable metadata repository boundary.
- TIP-18 closed as DB/provider posture hold.
- TIP-19 closed as transaction/audit consistency semantics planning.
- TIP-20 closed as provider evaluation criteria.
- TIP-21 closed as provider decision path planning.

TIP-22 is LocalDev planning before adapter implementation. It decides whether a strictly non-production LocalDev-only durable metadata adapter planning slice should happen before any production provider decision, and which boundaries would prevent that path from becoming production by accident.

## 2. Section 0 Repo Evidence

Read-only evidence:

```text
Repository root: D:/Task/Remote Signing/TagEkyc
HEAD: 937bc9c
Latest commit: 937bc9c docs: close TIP-21 provider decision path
Latest accepted validation supplied by prompt: dotnet test TagEkyc.sln --no-restore = 103 passed, 0 failed, 0 skipped
```

Known dirty files before TIP-22 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

Boundary evidence:

- `IDurableMetadataRepository` and `DurableMetadataWriteSet` remain the current Application boundary for future durable metadata persistence.
- `DurableMetadataWriteSet` remains the same-boundary semantic unit from TIP-19 for session metadata, audit identity metadata, and any included evidence package or completion authority metadata.
- No runtime implementation is opened by TIP-22.
- No provider, specific ORM/non-ORM style, schema, migration, index, package, repository implementation, Infrastructure adapter, LocalDev adapter, outbox, webhook, retry, backup/recovery, readiness, or SignFlow dependency is opened.
- No raw artifact, biometric, provider payload, vault byte, raw secret, hashed secret, token, private key, API key, or reconstructable credential material storage is opened.

## 3. Purpose

TIP-22 defines whether and how a strictly non-production LocalDev-only durable metadata adapter planning path should be governed before any production provider decision.

The slice exists to prevent LocalDev behavior from becoming a stealth provider decision or a stealth durability claim. LocalDev-only planning may be useful for proving semantics later, but only if it stays explicitly non-production, does not claim real durability, and does not bypass TIP-20 criteria or the TIP-21 decision path.

TIP-22 does not implement an adapter. It does not choose or recommend a provider. It does not authorize a LocalDev adapter.

## 4. LocalDev Planning Question

TIP-22 frames the core decision questions for homeowner/GPT review:

- Should a LocalDev-only durable metadata adapter planning slice happen before production provider decision?
- If yes, what boundaries prevent it from becoming production by accident?
- If no, what evidence is still needed before production provider decision?
- Should LocalDev-only behavior be allowed to exercise semantics without claiming real durability?

The recommended draft posture is to allow LocalDev-only planning to proceed only as a non-production planning question. Any later implementation would require a separate reviewed kickoff with explicit allowlist and STOP/RRI gates.

## 5. LocalDev-Only Boundaries

Any future LocalDev-only path must preserve these boundaries:

- Explicitly non-production.
- Disabled or not registered in production configuration.
- No production credential material.
- No real durability claim.
- No backup/recovery claim.
- No RPO/RTO.
- No restore capability claim.
- No legal reliance.
- No external audit reliance.
- No provider/vendor integration.
- No SignFlow runtime/source/database/package/internal-model dependency.
- No public API/DTO/JSON/status/error behavior changes.
- No raw artifact, biometric, provider payload, vault byte, raw secret, hashed secret, token, private key, API key, or reconstructable credential material storage.

LocalDev-only behavior must never become production by default, by convenience, by environment drift, by missing production provider decision, or by documentation ambiguity.

## 6. Relation to TIP-19 Semantics

Any future LocalDev-only adapter implementation, if later authorized, must still be able to simulate or prove these semantics at the LocalDev semantic level:

- Same-boundary `DurableMetadataWriteSet` behavior.
- Append-only audit identity metadata posture.
- Audit/business orphan prevention.
- Package/completion finalization consistency.
- Idempotency identity.
- Duplicate suppression.
- Conflict detection.
- Unknown/interrupted outcome handling.
- Forbidden-data absence.

TIP-22 itself does not implement or test these semantics.

## 7. Relation to TIP-20 and TIP-21

TIP-20 criteria remain the evaluation baseline.

TIP-21 decision path remains required before any production provider choice.

LocalDev-only planning must not bypass TIP-20 criteria or TIP-21 evidence packet requirements. A LocalDev-only adapter, if ever implemented, is not evidence that a production provider is selected, production-ready, recoverable, externally auditable, legally reliable, or operationally durable.

## 8. Allowed Future Planning Outcomes

TIP-22 may recommend one of these future paths, but does not execute them:

- Open a later LocalDev-only adapter kickoff with strict non-production allowlist.
- Defer LocalDev adapter until production provider decision path is clearer.
- Open backup/recovery requirements planning first.
- Open outbox relationship planning first.
- Open provider decision evidence packet planning first.

Do not create any of those files in TIP-22.

## 9. Hard Non-Goals

TIP-22 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, or dependency changes;
- concrete DB/provider selection;
- concrete package/tool selection;
- specific ORM/non-ORM or concrete data-access style decision;
- runtime persistence context;
- migrations, schema, indexes, generated provider scripts, or migration tooling;
- durable repository implementation;
- Infrastructure adapter implementation;
- LocalDev durable metadata adapter implementation;
- outbox, webhook, retry, delivery ledger, dispatcher, signing, replay, or delivery implementation;
- backup/recovery implementation, RPO/RTO, restore capability, operational durability, or recoverability claim;
- production auth implementation;
- credential store, API-key store, secret backend, raw secret storage, hashed secret storage, token storage, private key storage, or certificate lifecycle;
- raw artifact, biometric, provider payload, vault object, or evidence package raw storage;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- production readiness, pilot readiness, certification readiness, legal reliance, external audit reliance, real durability, or durable audit-store implementation claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 10. STOP/RRI Gates

Stop and request review before any later work if any of these become necessary:

| Gate | STOP/RRI condition |
| --- | --- |
| TIP-21 closeout baseline mismatch | Latest commit is not `937bc9c docs: close TIP-21 provider decision path`. |
| Concrete DB/provider name | Any concrete database or provider is named, compared, recommended, accepted, or selected. |
| Concrete package/tool name | Any concrete package or tool is named, added, compared, recommended, accepted, or selected. |
| Data-access style decision | Any specific ORM/non-ORM or concrete data-access style decision is required. |
| Package/project change | Any project, solution, package, or dependency change is required. |
| Schema/migration/index ownership | Any schema, migration, index, generated provider script, or ownership decision is required. |
| Repository implementation | `IDurableMetadataRepository` is implemented or its runtime behavior is changed. |
| Infrastructure adapter | Infrastructure persistence adapter work is introduced. |
| LocalDev adapter implementation | A LocalDev durable metadata adapter is introduced. |
| Outbox inclusion | Outbox, webhook, retry, delivery ledger, dispatcher, signing, replay, or delivery behavior is included. |
| Backup/recovery claim | Backup/recovery, RPO/RTO, restore capability, operational durability, or recoverability is claimed. |
| Readiness claim | Production, pilot, certification, legal, external audit, real durability, or durable audit-store readiness is implied. |
| Credential material storage | Raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material would be stored. |
| Raw artifact/vault data storage | Raw artifacts, biometrics, provider payloads, vault bytes, vault objects, or evidence package raw data would be stored. |
| Public contract change | Public API/DTO/JSON/status/error behavior would change. |
| SignFlow dependency | SignFlow runtime, source, database, package, network, or internal-model dependency is required. |
| LocalDev implies production durability | LocalDev behavior is described as real durability, backup/recovery, recoverability, production readiness, or external audit support. |
| LocalDev production default | LocalDev behavior can become production by default, missing configuration, fallback, convenience, or absence of a production provider decision. |
| TIP-19 semantics gap | The LocalDev path cannot preserve same-boundary `DurableMetadataWriteSet` semantics even at planning/proof level. |
| Criteria bypass pressure | The work is pressured to skip or soften TIP-20 criteria or TIP-21 evidence path requirements. |

## 11. Validation

Recommended docs-only validation:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_22_localdev_only_durable_metadata_adapter_planning/tip_22_planning_brief_v0_1.md
git diff --check
git status --short
```

Do not run `dotnet test` unless docs-only scope is accidentally violated.

## 12. Recommended Next Action

Keep TIP-22 as draft for homeowner/GPT review.

Do not stage or commit until reviewed.
