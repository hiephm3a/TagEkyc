# TIP-31 Concrete RPO / RTO Target Class Decision Brief v0.1

**File:** `docs/tips/tip_31_rpo_rto_target_class_decision/tip_31_decision_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - decision brief
**Date:** 2026-06-18
**Baseline:** `74bd1b5`
**Purpose:** Propose a provider-neutral RPO/RTO target class for durable metadata as a requirement-only decision input for resolving TIP-25 `G-001` at target-class level.

## Changelog

### v0.1 - Initial decision brief draft

- Opened TIP-31 as a docs-only, decision-only, provider-neutral RPO/RTO target class decision brief.
- Proposed target class `DMT-LOSSLESS-VALIDATED` for homeowner/GPT acceptance.
- Defined RPO posture as no tolerated accepted successful `DurableMetadataWriteSet` loss.
- Defined RTO posture as validation-gated recovery semantics in S2, not readiness-timed recovery support.
- Preserved TIP-19 same-boundary write-set semantics, idempotency, duplicate suppression, conflict detection, audit/business orphan prevention, package/completion consistency, unknown/interrupted outcome handling, forbidden-data absence, and credential/secret non-storage boundaries.
- Preserved TIP-20 criteria before choice, TIP-21 decision path before provider choice, TIP-22 LocalDev-only planning limits, TIP-23 provider-neutral evidence packet gate, TIP-24 packet assembly discipline, TIP-25 evidence packet gap register discipline, TIP-26 backup/recovery requirement shape, TIP-27 ownership baseline, TIP-28 environment separation baseline, TIP-29 migration/rollback/exit baseline, and TIP-30 RPO/RTO target decision posture.
- Classified `G-001` as resolved by accepted target class `DMT-LOSSLESS-VALIDATED`, subject to homeowner/GPT acceptance of this decision brief, with no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim.
- Preserved that TIP-31 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no backup/recovery, restore capability, RPO/RTO support, operational durability, recoverability, production readiness, pilot readiness, certification readiness, legal reliance, external audit reliance, durable audit-store readiness, or provider suitability claim.

## Status: Draft - decision brief

TIP-31 is draft documentation for homeowner/GPT review. It is docs-only, decision-only, provider-neutral, and limited to proposing an RPO/RTO target class for durable metadata as a requirement.

This brief does not claim that any current system, provider, adapter, backup path, restore path, operational process, runtime path, or evidence packet can meet the target class. It does not authorize implementation, provider decision, provider comparison, provider-specific evidence collection, package/tool decision, schema/migration/index work, LocalDev adapter work, repository implementation, Infrastructure adapter implementation, runtime registration behavior, provider wiring, backup implementation, restore implementation, monitoring implementation, alerting implementation, logging implementation, runbook implementation, operational durability claim, backup/recovery support claim, restore capability claim, RPO/RTO support claim, production readiness claim, pilot readiness claim, certification readiness claim, legal reliance claim, external audit reliance claim, durable audit-store claim, or SignFlow dependency.

## Baseline

TIP-31 follows the closed provider-neutral durable metadata planning sequence:

- HEAD `74bd1b5`.
- Latest commit `74bd1b5 docs: close TIP-30 RPO RTO target decision planning`.
- Latest accepted validation remains unchanged from the prompt: 103 passed, 0 failed, 0 skipped.
- TIP-17 closed as provider-neutral durable metadata repository boundary.
- TIP-18 closed as DB/provider posture decision with no production provider selected.
- TIP-19 closed as transaction/audit consistency semantics planning.
- TIP-20 closed as provider evaluation criteria before choice.
- TIP-21 closed as provider decision path before provider choice.
- TIP-22 closed as LocalDev-only durable metadata adapter planning with no implementation authorized and no production evidence claim.
- TIP-23 closed as provider-neutral evidence packet planning, requiring an accepted provider-neutral evidence packet before provider decision.
- TIP-24 closed as provider-neutral evidence packet assembly planning.
- TIP-25 assembled a draft provider-neutral evidence packet and kept provider decision blocked by visible gaps.
- TIP-26 closed as backup/recovery requirements planning and narrowed `G-001` by requirement shape only.
- TIP-27 closed as operational ownership and incident handling planning, resolving `G-002` at planning level and narrowing the ownership side of `G-001`.
- TIP-28 closed as configuration and environment separation planning, resolving `G-003` at planning level.
- TIP-29 closed as migration, reversibility, rollback, and exit planning, resolving `G-004` at planning level.
- TIP-30 closed as RPO/RTO target decision planning and kept `G-001` blocked pending accepted concrete targets or target classes.

Known dirty files before TIP-31 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

TIP-31 changed files are limited to:

- `docs/tips/README.md`
- `docs/tips/tip_31_rpo_rto_target_class_decision/tip_31_decision_brief_v0_1.md`

## Section 0 Repo Evidence

Read-only evidence used by this decision brief:

| Evidence | Current decision finding |
| --- | --- |
| Repository root | `D:/Task/Remote Signing/TagEkyc` |
| Baseline commit | `74bd1b5` |
| Latest accepted validation | Supplied by prompt as 103 passed, 0 failed, 0 skipped. |
| Current Application boundary | `IDurableMetadataRepository` remains the durable metadata repository boundary from TIP-17. |
| Current same-boundary semantic unit | `DurableMetadataWriteSet` remains the same-boundary semantic unit carried through TIP-19 and later TIPs. |
| Provider posture | TIP-18 keeps production provider selection deferred. |
| Evidence-packet posture | TIP-23 and TIP-24 require a provider-neutral evidence packet gate before provider decision; TIP-25 preserves visible gaps. |
| Backup/recovery posture | TIP-26 defines backup/recovery requirement shape only; no capability is claimed. |
| RPO/RTO posture | TIP-30 accepts target-decision forms and acceptance criteria but leaves `G-001` blocked pending accepted targets or target classes. |
| Operational posture | TIP-27 defines ownership and incident handling requirements only; `G-002` is resolved at planning level without readiness or implementation claim. |
| Configuration posture | TIP-28 defines configuration and environment separation requirements only; `G-003` is resolved at planning level without enforcement, capability, or implementation claim. |
| Migration/exit posture | TIP-29 defines migration, rollback, reversibility, abandon, replacement, exit, and provider-mechanics containment requirements only; `G-004` is resolved at planning level without capability or implementation claim. |
| Implementation posture | No runtime implementation, repository implementation, adapter implementation, schema/index/migration, package/dependency, backup, restore, monitoring, alerting, logging, runbook, provider wiring, or LocalDev adapter work is authorized by TIP-31. |
| Forbidden-data posture | Prior TIPs preserve absence of raw artifacts, biometrics, provider payloads, vault bytes, raw secrets, hashed secrets, tokens, private keys, API keys, and reconstructable credential material from durable metadata scope. |
| Consumer boundary posture | SignFlow remains outside runtime/source/database/package/internal-model dependency boundaries. |

## Purpose

TIP-31 proposes a concrete provider-neutral target class for the remaining target-decision portion of TIP-25 `G-001`.

The proposed target class is a requirement and decision input only. It defines what loss posture and validation posture durable metadata must satisfy before a later provider decision can rely on the target class. It does not prove that backup, restore, RPO/RTO support, operations, monitoring, alerting, logging, runbooks, migration, rollback, exit, or provider suitability exists.

## Decision Question

TIP-31 asks:

```text
Should durable metadata adopt the provider-neutral target class DMT-LOSSLESS-VALIDATED as the accepted RPO/RTO requirement class for resolving the remaining TIP-25 G-001 target-decision blocker?
```

Draft answer:

Adopt `DMT-LOSSLESS-VALIDATED` as the requirement-only target class, subject to homeowner/GPT acceptance of this decision brief. This class tolerates no accepted successful `DurableMetadataWriteSet` loss and treats recovery as validation-gated rather than readiness-timed. Provider decision remains blocked until homeowner/GPT accepts this brief and all prior evidence packet gates remain satisfied.

## Decision Candidate

Recommended target class:

```text
DMT-LOSSLESS-VALIDATED
```

Decision posture:

- The class is provider-neutral.
- The class is a requirement, not support proof.
- The class does not name, compare, score, shortlist, recommend, accept, or select a provider.
- The class does not authorize backup, restore, monitoring, alerting, logging, runbook, migration, schema, repository, adapter, package, dependency, LocalDev, or runtime work.
- The class must preserve prior accepted durable metadata semantics before any later provider decision can proceed.

## Accepted Target Class Definition

`DMT-LOSSLESS-VALIDATED` means:

```text
No accepted successful DurableMetadataWriteSet loss is tolerated. Durable metadata cannot be treated as accepted durable truth after restore unless validation proves completeness, consistency, continuity, forbidden-data absence, environment separation, and provider-mechanics containment.
```

This target class does not define a readiness-timed guarantee. If accepted durable truth cannot be proven after restore, the restored state must not be reported as success. It must remain non-success, quarantined, reconciled, corrected, or stopped through STOP/RRI.

## RPO Target Class

The RPO posture for `DMT-LOSSLESS-VALIDATED` is lossless at accepted write-set level:

- No accepted successful `DurableMetadataWriteSet` loss is tolerated.
- Accepted session, audit identity, package, completion, idempotency, conflict, correlation, and safe-reference facts must remain complete as accepted durable truth or must not be treated as accepted durable truth.
- Missing accepted facts are not an acceptable success condition.
- Retry after uncertain restore must preserve duplicate suppression and conflict detection, or remain non-success, quarantined, reconciled, corrected, or stopped through STOP/RRI.
- Audit/business orphan prevention remains mandatory.
- Package/completion partial finalization remains forbidden.
- Forbidden-data absence must remain provable before restored state is accepted.

This RPO target class is a requirement only. It does not prove that any backup path, restore path, provider, adapter, runtime, or operation can achieve it.

## RTO Target Class

The RTO posture for `DMT-LOSSLESS-VALIDATED` is validation-gated in S2:

- Durable metadata is not usable as accepted durable truth until restore validation proves the required conditions.
- Operations that depend on accepted durable truth must remain stopped, blocked, degraded, read-only, non-success, quarantined, review-gated, or escalated until validation passes under accepted future procedures.
- Incomplete, conflicting, uncertain, or missing validation is not a success state.
- Missed, unknown, or unproven timing expectations remain incident, quarantine, correction, or STOP/RRI conditions instead of readiness claims.

This RTO target class intentionally does not set a readiness-time guarantee. It defines the validation gate that must pass before restored durable metadata can be trusted.

## Validation-Gated Recovery Semantics

Under `DMT-LOSSLESS-VALIDATED`, recovery is gated by proof, not by availability language.

Restore validation must prove:

- write-set completeness;
- audit/business consistency;
- idempotency continuity;
- duplicate suppression continuity;
- conflict detection continuity;
- package/completion consistency;
- unknown/interrupted outcome preservation;
- forbidden-data absence;
- credential and secret non-storage boundaries;
- production/non-production separation;
- LocalDev exclusion from production evidence;
- provider-mechanics containment.

If validation cannot prove these requirements, durable metadata must not be accepted as durable truth.

## False-Success Prevention

The target class forbids false success:

- Unknown restore outcome is not success.
- Interrupted restore outcome is not success.
- Partial write-set recovery is not success.
- Conflicting recovered facts are not success.
- Missing audit identity facts for accepted business facts are not success.
- Package or completion partial finalization is not success.
- Unproven forbidden-data absence is not success.
- Unproven environment category is not success.

Non-success handling may include quarantine, reconciliation, correction, stopped operations, or STOP/RRI, but TIP-31 does not implement those paths.

## Same-Boundary Write-Set Preservation

TIP-31 preserves `IDurableMetadataRepository` as the current Application boundary and `DurableMetadataWriteSet` as the same-boundary semantic unit.

`DMT-LOSSLESS-VALIDATED` requires any later evidence to preserve TIP-19 same-boundary semantics:

- session metadata and required audit identity metadata for the same business operation are accepted together or not accepted as durable truth;
- evidence package metadata, completion authority metadata, and completed session facts cannot become partial finalization truth;
- corrections preserve accepted audit history through additional corrective facts instead of weakening accepted audit identity metadata.

## Audit / Business Consistency Preservation

The target class requires audit/business consistency preservation:

- accepted business metadata must not be orphaned from required audit identity metadata;
- successful-operation audit must not be orphaned from business facts;
- conflicting restored facts must not be normalized into success without accepted correction;
- unknown or interrupted outcomes must remain unknown, non-success, quarantined, reconciled, corrected, or stopped until accepted evidence proves durable truth.

## Idempotency / Duplicate / Conflict Preservation

The target class requires idempotency, duplicate, and conflict continuity:

- stable operation identity must survive or remain provably reconstructable as safe metadata;
- duplicate replay after restore must not create a second accepted write;
- the same idempotency identity with different facts must remain conflict, not success;
- restore uncertainty that breaks duplicate suppression or conflict detection must not be accepted as durable truth.

## Package / Completion Preservation

The target class requires package and completion consistency:

- evidence package metadata must not be partially accepted as durable truth;
- completion authority metadata must not be partially accepted as durable truth;
- completed session facts must not be accepted when required same-boundary facts are missing or uncertain;
- restored package/completion state must remain non-success, quarantined, reconciled, corrected, or stopped when completeness cannot be proven.

## Forbidden-Data Preservation

The target class preserves forbidden-data boundaries:

- raw artifacts, biometrics, provider payloads, vault bytes, raw secrets, hashed secrets, tokens, private keys, API keys, and reconstructable credential material remain outside durable metadata scope;
- target evidence must not require forbidden data in durable metadata, backup evidence, restore evidence, incident evidence, quarantine evidence, reconciliation evidence, correction evidence, migration evidence, rollback evidence, exit evidence, logs, or review packets;
- credential and secret non-storage boundaries remain mandatory.

## Operational Ownership Alignment

TIP-31 aligns with TIP-27:

- target acceptance does not prove operations readiness;
- missed, uncertain, conflicting, incomplete, or unproven validation remains owned through future accepted incident, quarantine, reconciliation, correction, escalation, monitoring, alerting, logging, and runbook requirements;
- homeowner/GPT acceptance is required before the target class can resolve `G-001` at decision-class level;
- operations or incident handling implementation is not authorized by this brief.

## Configuration / Environment Separation Alignment

TIP-31 aligns with TIP-28:

- production and non-production evidence remain separated;
- LocalDev behavior remains non-production evidence and cannot prove production durability, backup/recovery support, restore capability, RPO/RTO support, operations support, readiness, or provider suitability;
- missing, ambiguous, invalid, or incomplete production configuration cannot be treated as success;
- target evidence must preserve credential, identity, reference, and forbidden-data separation across environment boundaries.

## Migration / Rollback / Exit Alignment

TIP-31 aligns with TIP-29:

- target acceptance does not weaken migration, rollback, abandon, replacement, exit, or provider-mechanics containment requirements;
- future migration, rollback, replacement, or exit work must preserve `DMT-LOSSLESS-VALIDATED` as a requirement until separately proven;
- restore uncertainty during future introduction or exit work remains non-success, quarantine, reconciliation, correction, or STOP/RRI rather than provider suitability proof;
- provider mechanics must not leak into Domain, Application contracts beyond the accepted boundary, public contracts, consumers, or SignFlow.

## Capability / Readiness Non-Claims

TIP-31 makes no claim of:

- backup support;
- recovery support;
- restore capability;
- RPO support;
- RTO support;
- RPO/RTO support;
- recoverability;
- operational durability;
- operational readiness;
- incident readiness;
- monitoring readiness;
- alerting readiness;
- logging readiness;
- runbook readiness;
- production readiness;
- pilot readiness;
- certification readiness;
- legal reliance;
- external audit reliance;
- durable audit-store readiness;
- provider suitability;
- LocalDev durability or recoverability evidence;
- provider-specific evidence acceptance;
- concrete provider, package, tool, product, service, vendor, or runtime dependency selection.

TIP-31 also makes no implementation claim for runtime checks, durable storage, schema, migration, index, repository, adapter, Infrastructure, LocalDev, monitoring, alerting, logging, runbook, backup, restore, quarantine, reconciliation, correction workflow, export, import, rollback, replacement, exit, or SignFlow dependency work.

## Impact on TIP-25 G-001

TIP-31 classifies `G-001` as:

```text
G-001: Resolved by accepted target class DMT-LOSSLESS-VALIDATED, subject to homeowner/GPT acceptance of this decision brief; no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim.
```

This classification is decision-class level only. If homeowner/GPT rejects or returns this brief, `G-001` remains blocked pending accepted concrete RPO/RTO target decisions or accepted target classes.

If homeowner/GPT accepts this brief, the remaining target-decision blocker identified by TIP-30 is resolved at requirement level. Acceptance does not prove capability and does not itself authorize provider decision unless all prior evidence packet gates remain satisfied.

## Provider Decision Impact

TIP-31 does not unlock provider decision by itself.

Provider decision remains blocked unless all of the following are true:

- homeowner/GPT accepts this TIP-31 decision brief;
- the provider-neutral evidence packet gate from TIP-23 and TIP-24 remains satisfied;
- TIP-25 gap register discipline remains visible and accepted;
- `G-002`, `G-003`, and `G-004` remain resolved at planning level by accepted TIP-27, TIP-28, and TIP-29 baselines;
- no provider, package, tool, product, vendor, service, or runtime dependency is named, compared, scored, shortlisted, recommended, accepted, or selected by TIP-31;
- no capability, support, readiness, legal reliance, external audit reliance, durable audit-store, provider suitability, or implementation claim is made.

## Out-of-Scope / Non-Goals

TIP-31 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, or dependency changes;
- production provider selection;
- concrete provider, package, tool, product, vendor, or service naming;
- provider comparison, scoring, shortlisting, recommendation, acceptance, or selection;
- provider-specific evidence collection;
- concrete data-access style decision;
- runtime persistence context;
- runtime configuration implementation;
- environment detection implementation;
- environment enforcement implementation;
- runtime registration implementation;
- adapter selection;
- provider wiring;
- fallback/default behavior implementation;
- migrations, schema, indexes, generated provider scripts, or migration tooling;
- durable repository implementation;
- Infrastructure adapter implementation;
- LocalDev durable metadata adapter implementation;
- export tooling;
- import tooling;
- rollback tooling;
- exit tooling;
- replacement tooling;
- monitoring implementation;
- alerting implementation;
- logging implementation;
- runbook implementation or execution;
- quarantine implementation;
- reconciliation tooling implementation;
- correction workflow implementation;
- backup implementation;
- restore implementation;
- runtime validation checks implementation;
- RPO/RTO target support proof;
- backup/recovery support, restore capability, operational durability, recoverability, or readiness claim;
- production auth implementation;
- credential store, API-key store, secret backend, raw secret storage, hashed secret storage, token storage, private key storage, or certificate lifecycle;
- raw artifact, biometric, provider payload, vault object, vault byte, or evidence package raw storage;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- production readiness, pilot readiness, certification readiness, legal reliance, external audit reliance, real durability, or durable audit-store implementation claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## STOP/RRI Gates

Stop and request review before any later work if any of these become necessary:

| Gate | STOP/RRI condition |
| --- | --- |
| Target-class overclaim | `DMT-LOSSLESS-VALIDATED` is treated as current support, capability, durability, recoverability, readiness, provider suitability, or implementation proof. |
| G-001 overclaim | `G-001` is marked resolved without homeowner/GPT acceptance of this decision brief or another accepted target decision. |
| Provider decision pressure | Provider decision proceeds before homeowner/GPT accepts this brief and all prior evidence packet gates remain satisfied. |
| Concrete name leakage | Any concrete provider, package, tool, product, vendor, service, or runtime dependency is named. |
| Provider comparison | Provider options are compared, scored, shortlisted, recommended, accepted, or selected. |
| Provider-specific evidence | Evidence collection depends on provider-specific facts or documentation. |
| Implementation pressure | Runtime, repository, adapter, Infrastructure, LocalDev, project, package, schema, migration, index, generated artifact, export, import, rollback, replacement, exit, backup, restore, monitoring, alerting, logging, runbook, reconciliation, correction workflow, or dependency change is required. |
| Readiness-timing claim | Validation-gated RTO posture is converted into a readiness-time guarantee or support claim. |
| Capability claim | Backup/recovery, restore, RPO/RTO support, operational durability, recoverability, migration, rollback, reversibility, abandon, replacement, exit, configuration enforcement, environment enforcement, or provider suitability capability is claimed instead of defined as a requirement. |
| Same-boundary gap | `DurableMetadataWriteSet` same-boundary semantics cannot be preserved under the target class. |
| Idempotency gap | Stable operation identity, duplicate suppression, or conflict handling cannot be preserved under the target class. |
| Audit/business gap | Accepted business metadata could be orphaned from required audit identity metadata because of target loss or validation posture. |
| Completion/package gap | Evidence package metadata, completion authority metadata, or completed session facts could be accepted as partial finalization truth because of target loss or validation posture. |
| Unknown outcome gap | Interrupted or unknown outcomes could be reported as success because target evidence is uncertain. |
| Forbidden-data gap | Forbidden data or credential material would enter durable metadata, target evidence, logs, backup, restore, quarantine, reconciliation, correction workflow, migration evidence, rollback evidence, or exit evidence. |
| Environment gap | Production/non-production separation, LocalDev exclusion, or missing/ambiguous/invalid configuration handling cannot be preserved. |
| Operational gap | Owner/reviewer, incident, quarantine, reconciliation, correction, escalation, monitoring, alerting, logging, or runbook requirements remain undefined where target class acceptance needs them. |
| Migration/exit gap | Future work depends on unresolved migration, rollback, abandon, reversibility, replacement, exit, or provider-mechanics containment requirements. |
| Criteria bypass | TIP-20 criteria or TIP-21 decision path requirements are skipped or softened. |
| Gap register bypass | Blocking gaps are omitted, hidden, or treated as non-blocking without homeowner/GPT acceptance. |
| SignFlow dependency | SignFlow runtime, source, database, package, network, or internal-model dependency is required. |

## Validation

Recommended docs-only validation:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_31_rpo_rto_target_class_decision/tip_31_decision_brief_v0_1.md
git diff --check
git status --short
```

Also run:

- concrete-name guardrail scan over added TIP-31 lines;
- numeric-target scan to confirm no accidental numeric RPO/RTO minutes/hours target was added.

Do not run runtime tests unless docs-only scope is accidentally violated.

## Recommended Next Action

Stop for homeowner/GPT review of TIP-31.

Do not stage or commit until accepted.

If accepted, TIP-31 resolves TIP-25 `G-001` at decision-class level by accepting `DMT-LOSSLESS-VALIDATED` as the provider-neutral RPO/RTO target class requirement. Provider decision remains blocked unless the accepted provider-neutral evidence packet exists, all prior evidence packet gates remain satisfied, and no provider decision, provider comparison, provider naming, provider-specific evidence collection, runtime implementation, LocalDev adapter implementation, package/project change, schema/migration/index work, backup implementation, restore implementation, monitoring/alerting/logging/runbook implementation, backup/recovery claim, restore capability claim, RPO/RTO support claim, readiness claim, legal reliance claim, external audit reliance claim, durable audit-store claim, provider suitability claim, or SignFlow dependency proceeds from TIP-31 alone.
