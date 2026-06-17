# TIP-28 Configuration / Environment Separation Planning Brief v0.1

**File:** `docs/tips/tip_28_configuration_environment_separation_planning/tip_28_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-17
**Baseline:** `8cfcaca`
**Purpose:** Define provider-neutral configuration and environment separation requirements as requirements only, resolving or narrowing TIP-25 gap `G-003` without claiming runtime enforcement, provider readiness, or implementation.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-28 as a docs-only, planning-only, provider-neutral configuration and environment separation planning brief.
- Defined required environment categories before provider decision.
- Defined production versus non-production separation requirements.
- Defined LocalDev exclusion requirements so LocalDev behavior cannot become production behavior or production evidence.
- Defined missing, ambiguous, invalid, and incomplete configuration behavior requirements.
- Defined fallback/default behavior requirements that prevent unsafe production registration.
- Defined credential, identity, test reference, fake reference, and non-production reference separation requirements.
- Defined future runtime registration guardrails as requirements only.
- Defined restore, incident, and operations environment requirements.
- Defined evidence requirements before any later provider decision can treat `G-003` as accepted.
- Preserved `IDurableMetadataRepository` as the current Application boundary and `DurableMetadataWriteSet` as the current same-boundary semantic unit.
- Preserved TIP-19 same-boundary semantics, idempotency, duplicate suppression, conflict detection, audit/business orphan prevention, package/completion consistency, unknown/interrupted outcome handling, forbidden-data absence, and credential/secret non-storage boundaries.
- Preserved TIP-20 criteria before choice, TIP-21 decision path before provider choice, TIP-22 LocalDev-only planning limits, TIP-23 provider-neutral evidence packet gate, TIP-24 packet assembly discipline, TIP-25 provider decision blocking gaps, TIP-26 backup/recovery requirement shape, and TIP-27 operational ownership / incident handling baseline.
- Preserved that TIP-28 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no configuration enforcement, environment enforcement, production readiness, legal reliance, external audit reliance, durable audit-store readiness, or provider suitability claim.

## Status: Draft - planning only

TIP-28 is draft documentation for homeowner/GPT review. It is docs-only, planning-only, provider-neutral, and limited to configuration and environment separation requirements planning for TIP-25 gap `G-003`.

No implementation, provider decision, provider comparison, provider-specific evidence collection, package/tool decision, schema/migration/index work, LocalDev adapter work, runtime configuration behavior, environment detection behavior, runtime registration behavior, adapter selection, provider wiring, fallback logic, monitoring implementation, alerting implementation, logging implementation, runbook execution, backup implementation, restore implementation, configuration enforcement, environment enforcement, production readiness claim, legal reliance claim, external audit reliance claim, durable audit-store claim, or SignFlow dependency is authorized by this draft.

TIP-28 defines requirements only. It does not claim that any current or future provider, adapter, runtime path, LocalDev behavior, configuration source, environment gate, operating process, restore path, incident path, or registration path satisfies those requirements.

## Baseline

TIP-28 follows the closed provider-neutral durable metadata planning sequence:

- HEAD `8cfcaca`.
- Latest commit `8cfcaca docs: close TIP-27 operational ownership incident planning`.
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
- TIP-26 closed as backup/recovery requirements planning and kept `G-001` partially blocked pending accepted RPO/RTO target decisions and operational ownership alignment.
- TIP-27 closed as operational ownership and incident handling planning, resolving `G-002` at planning level and narrowing the ownership side of `G-001`.

Known dirty files before TIP-28 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

TIP-28 changed files are limited to:

- `docs/tips/README.md`
- `docs/tips/tip_28_configuration_environment_separation_planning/tip_28_planning_brief_v0_1.md`

## Section 0 Repo Evidence

Read-only evidence used by this planning brief:

| Evidence | Current planning finding |
| --- | --- |
| Repository root | `D:/Task/Remote Signing/TagEkyc` |
| Baseline commit | `8cfcaca` |
| Latest accepted validation | Supplied by prompt as 103 passed, 0 failed, 0 skipped. |
| Current Application boundary | `IDurableMetadataRepository` remains the durable metadata repository boundary from TIP-17. |
| Current same-boundary semantic unit | `DurableMetadataWriteSet` remains the same-boundary semantic unit carried through TIP-19 and later TIPs. |
| Provider posture | TIP-18 keeps production provider selection deferred. |
| Evidence-packet posture | TIP-25 provider decision remains blocked until visible gaps are resolved by accepted evidence or reviewed planning slices. |
| Backup/recovery posture | TIP-26 defines backup/recovery requirement shape only; `G-001` remains partially blocked pending accepted RPO/RTO target decisions. |
| Operational posture | TIP-27 defines operational ownership and incident handling requirements only; `G-002` is resolved at planning level without readiness or implementation claim. |
| Implementation posture | No runtime implementation, repository implementation, adapter implementation, schema/index/migration, package/dependency, configuration behavior, environment detection, registration behavior, provider wiring, fallback behavior, or LocalDev adapter work is authorized by TIP-28. |
| Forbidden-data posture | Prior TIPs preserve absence of raw artifacts, biometrics, provider payloads, vault bytes, raw secrets, hashed secrets, tokens, private keys, API keys, and reconstructable credential material from durable metadata scope. |
| Consumer boundary posture | SignFlow remains outside runtime/source/database/package/internal-model dependency boundaries. |

## Purpose

TIP-28 resolves or narrows the TIP-25 configuration and environment separation gap by defining provider-neutral requirements that must exist before any future production durable metadata provider decision can be accepted.

TIP-28 answers the configuration and environment separation questions at planning level only:

- what environment categories must exist before provider decision;
- what production versus non-production separation requires;
- how LocalDev behavior is excluded from production behavior and production evidence;
- what missing, ambiguous, invalid, or incomplete configuration must do;
- how fallback, default, or convenience behavior is prevented from becoming production behavior;
- how test identities, fake credentials, non-production references, and LocalDev-only settings are prevented from crossing into production;
- how future runtime registration must avoid unsafe fallback, as requirements only;
- what restore, incident, and operations environment requirements must preserve;
- what evidence is required before configuration/environment separation can be accepted;
- how TIP-28 affects `G-003`;
- what remains unresolved after TIP-28.

TIP-28 must not prove that environment separation, runtime configuration enforcement, provider registration, restore, incident handling, or operational processes exist. It only defines what later accepted evidence or implementation planning must prove before reliance.

## G-003 Scope

TIP-25 gap `G-003` covers missing configuration and environment separation evidence:

```text
Accepted production/non-production separation requirements, LocalDev exclusion requirements, missing-configuration behavior, and test identity boundary requirements.
```

TIP-28 scope is limited to this gap. It does not resolve:

- concrete RPO/RTO target decisions under `G-001`;
- `G-004` migration / reversibility / rollback / exit.

TIP-28 recognizes TIP-27's planning-level resolution of `G-002`. It may reference operational ownership and incident handling requirements when environment uncertainty must escalate, but it does not implement or prove any operational capability.

## Configuration / Environment Separation Question

TIP-28 answers this planning question:

```text
What provider-neutral configuration and environment separation requirements must be accepted before a future durable metadata provider decision can proceed?
```

Draft answer:

A future provider decision must be blocked unless the evidence packet includes accepted requirements for explicit environment categories, production and non-production separation, LocalDev exclusion, missing/ambiguous/invalid configuration behavior, fallback/default prohibition, credential/identity/reference separation, future runtime registration guardrails, restore/incident/operations environment handling, and reviewable evidence. Those requirements must preserve `IDurableMetadataRepository`, `DurableMetadataWriteSet`, TIP-19 same-boundary semantics, idempotency, duplicate suppression, conflict detection, audit/business orphan prevention, package/completion consistency, unknown/interrupted outcome handling, forbidden-data absence, credential/secret non-storage boundaries, and LocalDev evidence limits.

## Environment Category Requirements

Before provider decision, the evidence packet must require environment categories at requirement level, at minimum:

| Environment category | Required purpose before provider decision |
| --- | --- |
| LocalDev | Developer-only behavior for local semantic exploration or future non-production adapter planning. It is not production behavior, production evidence, durability evidence, backup/recovery evidence, restore evidence, operations evidence, or readiness evidence. |
| Automated test | Non-production behavior for repeatable test execution. It must not use production credentials, production identities, production references, production configuration, or production evidence. |
| Shared non-production validation | Non-production behavior for team or reviewer validation. It must remain visibly non-production and must not be used as production evidence. |
| Pre-production review | Non-production review posture used only to validate requirements before production reliance. It must not be treated as production readiness, provider suitability, or legal/external audit evidence unless a later accepted TIP explicitly permits that evidence scope. |
| Production | The only category allowed to represent production durable metadata behavior after a future accepted provider decision and implementation. It must require explicit, complete, valid configuration and must reject unsafe fallback. |
| Restore or incident validation | Environment-specific validation posture for future restore or incident requirements. It must preserve production/non-production separation and must not convert non-production evidence into production proof. |

These categories are requirements only. TIP-28 does not implement environment detection, naming, registration, routing, validation, deployment, restore, or incident behavior.

## Production Configuration Requirements

Production configuration requirements before provider decision:

- Production must require an explicit production environment category.
- Production must require an accepted provider decision before any production provider registration or provider behavior can be treated as valid.
- Production must require complete, valid, production-scoped configuration before durable metadata behavior is accepted.
- Production must reject LocalDev settings, automated test settings, fake credentials, test identities, sample references, non-production references, and convenience defaults.
- Production must reject missing, ambiguous, invalid, or incomplete configuration as non-success, startup block, registration block, quarantine, or STOP/RRI behavior in a later accepted design.
- Production must not infer a provider, adapter, configuration source, credential, identity, or environment from absence of configuration.
- Production must not treat prior LocalDev, test, or non-production evidence as real durability, backup/recovery support, restore capability, RPO/RTO support, operational readiness, provider suitability, legal reliance, external audit reliance, or durable audit-store readiness.
- Production must preserve forbidden-data absence and credential/secret non-storage boundaries.
- Production must preserve `IDurableMetadataRepository` as the Application boundary and must not leak provider mechanics into Domain, public contracts, consumers, or SignFlow.

These requirements do not claim that production configuration or production environment separation is implemented.

## Non-Production Configuration Requirements

Non-production configuration requirements before provider decision:

- Non-production environments must be explicitly categorized as non-production.
- Non-production behavior must remain visibly separated from production in configuration, evidence, identity, credential, references, restore evidence, incident evidence, and review language.
- Non-production configuration may support semantic review, test execution, or planning evidence only when its non-production status is explicit.
- Non-production configuration must not contain production credentials, production identities, production references, or production-only operational evidence.
- Non-production behavior must not be promoted to production by rename, missing configuration, fallback, convenience defaults, copy/paste, restore, incident handling, or provider decision pressure.
- Non-production evidence must not be accepted as production durability, backup/recovery support, restore capability, RPO/RTO support, operations support, provider suitability, legal reliance, external audit reliance, or durable audit-store readiness.
- Non-production incidents involving possible production credential, identity, or reference leakage must trigger boundary review, quarantine, or STOP/RRI requirements in a later accepted design.

Non-production requirements do not authorize LocalDev adapter implementation, runtime adapter behavior, provider behavior, or production evidence collection.

## LocalDev Exclusion Requirements

LocalDev behavior must be excluded from production by requirement:

- LocalDev must remain a separate non-production category.
- LocalDev-only settings must be invalid in production.
- LocalDev-only identities, credentials, references, storage paths, sample values, fake values, seeded values, and convenience defaults must be invalid in production.
- LocalDev behavior must not be registered as production behavior by default, missing configuration, ambiguous configuration, absent provider decision, or fallback.
- LocalDev behavior must not prove production durability, production recoverability, backup/recovery support, restore capability, RPO/RTO support, operational readiness, incident readiness, monitoring readiness, alerting readiness, logging readiness, runbook readiness, provider suitability, legal reliance, external audit reliance, or durable audit-store readiness.
- LocalDev evidence may support semantic reasoning only when explicitly labeled non-production and when it preserves TIP-17 through TIP-27 boundaries.
- LocalDev adapter implementation remains unauthorized unless a separate reviewed kickoff explicitly opens it with non-production allowlist boundaries.

If a later design cannot prove LocalDev exclusion without implementing or naming concrete provider mechanics during this planning slice, it must STOP/RRI.

## Missing / Ambiguous / Invalid Configuration Requirements

Missing, ambiguous, invalid, or incomplete configuration must be treated as unsafe before production durable metadata behavior is accepted.

Required behavior for a later accepted design:

| Configuration condition | Required posture |
| --- | --- |
| Missing production environment category | Block production registration or operation; do not infer LocalDev or any other non-production category. |
| Missing accepted provider decision | Block production provider registration; do not select or infer a provider. |
| Missing required production configuration | Block production registration or operation; do not use fallback/default behavior. |
| Ambiguous environment category | Block or STOP/RRI; do not choose the most convenient or first available category. |
| Ambiguous configuration source | Block or STOP/RRI; do not merge unsafe values or infer precedence. |
| Invalid production configuration | Reject, block, quarantine, or STOP/RRI; do not downgrade to LocalDev or test behavior. |
| Incomplete production configuration | Reject, block, quarantine, or STOP/RRI; do not fill gaps with defaults. |
| Non-production value in production scope | Reject and require boundary review; do not accept as production evidence. |
| Production value in non-production scope | Reject, quarantine, or STOP/RRI; do not continue until credential/identity/reference boundary risk is reviewed. |
| Environment cannot be proven | Treat as non-success or blocked; do not treat as production success. |

These requirements define desired failure posture only. TIP-28 does not implement configuration parsing, validation, precedence, registration, error handling, or startup behavior.

## Fallback / Default Behavior Requirements

Fallback and default requirements before provider decision:

- Production must have no implicit provider fallback.
- Production must have no LocalDev fallback.
- Production must have no automated test fallback.
- Production must have no fake identity, fake credential, sample reference, or non-production reference fallback.
- Production must have no convenience default that creates durable metadata behavior without explicit accepted configuration.
- Missing provider decision must not select a provider, adapter, registration path, schema, storage behavior, or dependency by implication.
- Missing configuration must not silently disable durability while reporting success.
- Missing configuration must not convert unknown or interrupted outcomes into success.
- Fallback behavior, if ever allowed for a non-production category by a later accepted design, must be explicitly non-production and must not be reusable as production evidence.
- Any pressure to implement fallback behavior in this TIP must STOP/RRI.

TIP-28 does not define or implement fallback logic. It defines what fallback behavior must not do.

## Credential / Identity / Reference Separation Requirements

Credential, identity, and reference separation requirements before provider decision:

- Production credentials must never be used in LocalDev, automated test, shared non-production validation, or pre-production review unless a later accepted security review explicitly authorizes that boundary. TIP-28 does not authorize such use.
- Non-production credentials must never be accepted in production.
- Fake credentials must never be accepted in production.
- Test identities must never be accepted as production identities.
- Fake identities must never be accepted as production identities.
- Non-production references must never be accepted as production references.
- LocalDev-only settings must never cross into production configuration, restore evidence, incident evidence, or provider decision evidence.
- Credential and secret material must remain outside durable metadata scope.
- Durable metadata may contain safe references only when those references remain non-secret, non-reconstructable, and environment-scoped.
- Logs, incident evidence, restore evidence, and review packets must not include raw artifacts, biometrics, provider payloads, vault bytes, raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material.

If future environment evidence cannot prove credential, identity, and reference separation, provider decision acceptance must remain blocked.

## Runtime Registration Guardrail Requirements

Future runtime registration must satisfy these requirements before production behavior can be accepted:

- Registration must be explicit for each environment category.
- Production registration must require explicit production category and accepted production configuration.
- Production registration must require an accepted provider decision before provider behavior is registered.
- Registration must not infer a provider, adapter, or environment from missing values.
- Registration must not register LocalDev, automated test, fake, or non-production behavior in production.
- Registration must fail closed for missing, ambiguous, invalid, incomplete, or contradictory configuration.
- Registration must preserve `IDurableMetadataRepository` as the Application boundary.
- Registration must preserve `DurableMetadataWriteSet` same-boundary semantics.
- Registration must preserve idempotency, duplicate suppression, conflict detection, audit/business orphan prevention, package/completion consistency, and unknown/interrupted outcome handling.
- Registration must preserve forbidden-data absence and credential/secret non-storage boundaries.
- Registration must not leak provider mechanics into Domain, public contracts, consumers, or SignFlow.

These are runtime registration guardrails as requirements only. TIP-28 does not implement registration behavior, adapter selection, provider wiring, runtime configuration, environment detection, or tests.

## Restore / Incident / Operations Environment Requirements

Restore, incident, and operations requirements before provider decision:

- Restore evidence must identify the environment category of the restored state.
- Restore evidence must not use non-production state as production restore proof.
- Restore validation must reject or quarantine state when environment category cannot be proven.
- Restore validation must preserve TIP-26 write-set completeness, idempotency continuity, conflict detection, forbidden-data absence, and false-success prevention requirements.
- Incident evidence must identify the environment category involved in the incident.
- Incidents involving environment ambiguity must remain non-success, quarantined, or STOP/RRI until reviewed under TIP-27 ownership requirements.
- Operations requirements must define who reviews environment-separation evidence before production provider decision acceptance.
- Monitoring, alerting, logging, and runbook requirements from TIP-27 must preserve environment labels and must not store forbidden data.
- RPO/RTO target decisions under `G-001` must not be accepted using non-production evidence as production proof.

These requirements do not claim restore capability, incident readiness, operational readiness, monitoring readiness, alerting readiness, logging readiness, runbook readiness, or RPO/RTO support.

## Evidence Requirements Before Provider Decision

Before provider decision, accepted evidence must show at requirement level:

| Evidence area | Required evidence before provider decision |
| --- | --- |
| Environment categories | Required categories are defined and their production/non-production roles are unambiguous. |
| Production separation | Production requires explicit category, accepted provider decision, complete valid production configuration, and no non-production fallback. |
| Non-production separation | LocalDev, automated test, shared non-production validation, and pre-production review remain visibly non-production and cannot be used as production proof. |
| LocalDev exclusion | LocalDev settings, values, identities, references, credentials, and behavior are invalid in production by requirement. |
| Missing/ambiguous/invalid configuration | Unsafe configuration states block, reject, quarantine, or STOP/RRI instead of falling back or reporting success. |
| Fallback/default behavior | Production fallback to LocalDev, test, fake, non-production, or convenience defaults is forbidden. |
| Credential/identity/reference boundaries | Test identities, fake credentials, non-production references, and LocalDev-only settings cannot cross into production. |
| Runtime registration guardrails | Future registration requirements fail closed and preserve Application, same-boundary, forbidden-data, and SignFlow boundaries. |
| Restore/incident/operations alignment | Restore, incident, monitoring, alerting, logging, runbook, and RPO/RTO evidence preserve environment category separation. |
| Forbidden claims | The brief makes no implementation, enforcement, provider suitability, readiness, legal reliance, external audit reliance, or durable audit-store claim. |

If any evidence area remains unaccepted, `G-003` must remain blocked or partially narrowed instead of resolved.

## Forbidden Claims

TIP-28 forbids these claims:

- runtime configuration implementation;
- environment detection implementation;
- environment enforcement implementation;
- runtime registration implementation;
- adapter selection implementation;
- provider wiring implementation;
- fallback/default implementation;
- LocalDev adapter implementation;
- production provider selection;
- provider comparison, scoring, shortlisting, recommendation, acceptance, or selection;
- provider-specific evidence acceptance;
- configuration enforcement capability;
- environment separation capability;
- production durability;
- backup/recovery support;
- restore capability;
- RPO support;
- RTO support;
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
- LocalDev production evidence.

TIP-28 also forbids implementation claims for runtime checks, durable storage, schema, migration, index, repository, adapter, Infrastructure, LocalDev, monitoring, alerting, logging, runbook, backup, restore, quarantine, reconciliation, correction workflow, or SignFlow dependency work.

## Relationship to TIP-17/TIP-18/TIP-19/TIP-20/TIP-21/TIP-22/TIP-23/TIP-24/TIP-25/TIP-26/TIP-27

TIP-28 preserves the accepted durable metadata planning chain:

| Source | Relationship preserved by TIP-28 |
| --- | --- |
| TIP-17 | `IDurableMetadataRepository` remains the current Application boundary; forbidden-data and public-contract boundaries remain intact. |
| TIP-18 | Production provider selection remains deferred; no implementation, configuration enforcement, environment enforcement, readiness, or provider suitability is claimed. |
| TIP-19 | `DurableMetadataWriteSet`, same-boundary semantics, idempotency, duplicate suppression, conflict detection, audit/business orphan prevention, package/completion consistency, and unknown/interrupted outcome handling are preserved. |
| TIP-20 | Criteria remain before choice; configuration and environment separation requirements become criteria input, not provider evidence or provider selection. |
| TIP-21 | Decision path remains before provider choice; concrete provider/package/schema/adapter work remains blocked pending accepted evidence. |
| TIP-22 | LocalDev-only planning remains non-production evidence and does not prove production durability, backup/recovery, restore, operations, readiness, or provider suitability. |
| TIP-23 | Provider-neutral evidence packet remains required before any future provider decision. |
| TIP-24 | Packet assembly discipline, proof checklist, gap register, reviewer responsibilities, and STOP/RRI gates remain required. |
| TIP-25 | Provider decision remains blocked until visible gaps are resolved or narrowed by accepted evidence. TIP-28 addresses `G-003`. |
| TIP-26 | Backup/recovery requirement shape remains accepted; environment separation must preserve restore consistency and must not use non-production restore evidence as production proof. |
| TIP-27 | Operational ownership and incident handling baseline remains accepted at planning level; environment ambiguity must escalate under those role/function requirements. |

TIP-28 does not replace prior TIPs, weaken any gate, or authorize a future provider decision.

## Impact on TIP-25 Gap Register

TIP-28 classifies `G-003` after this draft as:

```text
G-003: Resolved by configuration/environment separation planning, subject to homeowner/GPT acceptance; no runtime configuration implementation, environment enforcement, provider decision, or readiness claim.
```

This classification is limited to requirements planning only and depends on homeowner/GPT acceptance of TIP-28.

Rationale:

- Environment category requirements are defined.
- Production versus non-production separation requirements are defined.
- LocalDev exclusion requirements are defined.
- Missing, ambiguous, invalid, and incomplete configuration behavior requirements are defined.
- Fallback/default behavior requirements are defined.
- Test identity, fake credential, non-production reference, and LocalDev-only setting separation requirements are defined.
- Runtime registration guardrail requirements are defined as requirements only.
- Restore, incident, and operations environment requirements are defined.
- Evidence required before provider decision is defined.
- TIP-28 clearly states that no runtime configuration behavior, environment separation capability, production readiness, provider suitability, provider decision, or implementation is claimed.

TIP-25 provider decision remains blocked by:

| Gap ID | Post-TIP-28 status |
| --- | --- |
| `G-001` | Partially narrowed by backup/recovery requirement shape and ownership alignment; still blocked pending accepted RPO/RTO target decisions. |
| `G-002` | Resolved by operational ownership planning at planning level, subject to accepted TIP-27 baseline; no operational readiness or implementation claim. |
| `G-003` | Resolved by configuration/environment separation planning, subject to homeowner/GPT acceptance of TIP-28; no runtime configuration implementation, environment enforcement, provider decision, capability, or readiness claim. |
| `G-004` | Still blocked pending migration, reversibility, rollback, and exit planning. |

If homeowner/GPT review rejects or returns TIP-28, `G-003` remains blocked until revised and accepted.

## Out-of-Scope / Non-Goals

TIP-28 does not authorize:

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
- configuration enforcement, environment enforcement, backup/recovery support, RPO/RTO support, restore capability, operational durability, recoverability, or readiness claim;
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
| G-003 bypass | A provider decision is attempted before TIP-28 is accepted or before configuration/environment separation requirements are otherwise accepted. |
| G-001 overclaim | `G-001` is marked fully resolved before accepted RPO/RTO target decisions exist. |
| G-004 bypass | Provider decision proceeds while migration, reversibility, rollback, and exit planning remains blocked. |
| Concrete name leakage | Any concrete provider, package, tool, product, vendor, service, or runtime dependency is named. |
| Provider comparison | Provider options are compared, scored, shortlisted, recommended, accepted, or selected. |
| Provider-specific evidence | Evidence collection depends on provider-specific facts or documentation. |
| Implementation pressure | Runtime, repository, adapter, Infrastructure, LocalDev, project, package, schema, migration, index, configuration, environment detection, registration, fallback, backup, restore, monitoring, alerting, logging, runbook, reconciliation, correction workflow, or dependency change is required. |
| Configuration enforcement claim | Configuration enforcement or environment separation capability is claimed instead of defined as a requirement. |
| Readiness claim | Production, pilot, certification, legal, external audit, real durability, or durable audit-store readiness is implied. |
| Missing configuration fallback | Missing, ambiguous, invalid, or incomplete configuration could select LocalDev, test, fake, or non-production behavior in production. |
| Provider-decision fallback | Absence of a provider decision could select or infer provider behavior. |
| LocalDev evidence misuse | LocalDev behavior is treated as production durability, backup/recovery, restore, operations, configuration, environment separation, readiness, or provider evidence. |
| Credential/identity/reference leak | Test identities, fake credentials, non-production references, or LocalDev-only settings could enter production. |
| Production value leak | Production credential, identity, reference, or configuration value could enter non-production without accepted boundary review. |
| Same-boundary gap | `DurableMetadataWriteSet` same-boundary semantics cannot be preserved under future configuration or registration requirements. |
| Idempotency gap | Stable operation identity, duplicate suppression, or conflict handling cannot be preserved under future configuration or registration requirements. |
| Audit/business gap | Accepted business metadata could be orphaned from required audit identity metadata because of environment or configuration behavior. |
| Completion/package gap | Evidence package metadata, completion authority metadata, or completed session facts could be accepted as partial finalization truth because of environment or configuration behavior. |
| Unknown outcome gap | Interrupted or unknown outcomes could be reported as success because configuration or environment state is uncertain. |
| Forbidden-data gap | Forbidden data or credential material would enter durable metadata, configuration, incident evidence, logs, backup, restore, quarantine, reconciliation, or correction workflow scope. |
| Boundary leak | Provider, configuration, registration, fallback, environment, backup, restore, monitoring, alerting, logging, runbook, reconciliation, or correction mechanics would leak into Domain, public contracts, consumers, or SignFlow. |
| Criteria bypass | TIP-20 criteria or TIP-21 decision path requirements are skipped or softened. |
| Gap register bypass | Blocking gaps are omitted, hidden, or treated as non-blocking without homeowner/GPT acceptance. |
| SignFlow dependency | SignFlow runtime, source, database, package, network, or internal-model dependency is required. |

## Validation

Recommended docs-only validation:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_28_configuration_environment_separation_planning/tip_28_planning_brief_v0_1.md
git diff --check
git status --short
```

Also run a concrete-name guardrail scan over added TIP-28 lines.

Do not run runtime tests unless docs-only scope is accidentally violated.

## Recommended Next Action

Stop for homeowner/GPT review of TIP-28.

Do not stage or commit until accepted.

If accepted, TIP-28 resolves TIP-25 `G-003` at planning level by defining environment category requirements, production versus non-production separation requirements, LocalDev exclusion requirements, missing/ambiguous/invalid configuration behavior, fallback/default behavior requirements, credential/identity/reference separation requirements, runtime registration guardrail requirements, restore/incident/operations environment requirements, and evidence requirements before provider decision. `G-001` remains partially blocked pending accepted RPO/RTO target decisions, and `G-004` remains blocked pending migration/reversibility/rollback/exit planning. No provider decision, provider comparison, provider naming, provider-specific evidence collection, runtime implementation, LocalDev adapter implementation, package/project change, schema/migration/index work, configuration enforcement claim, environment enforcement claim, backup/recovery claim, restore capability claim, RPO/RTO support claim, readiness claim, legal reliance claim, external audit reliance claim, durable audit-store claim, or SignFlow dependency should proceed from TIP-28 alone.
