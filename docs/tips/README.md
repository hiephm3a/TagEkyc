# TagEkyc TIP Index

**File:** `docs/tips/README.md`
**Version:** 0.55
**Status:** Active
**Date:** 2026-06-18
**Baseline:** Product Brief v0.1.1
**Purpose:** Indexes TIP folders and records the TIP document naming convention.

## Changelog

### v0.55 - TIP-33 S2 final closeout / S3 handoff indexed

- Added TIP-33 S2 Final Closeout / S3 Handoff brief to the index.
- Recorded TIP-33 as draft docs-only / closeout-only / handoff-only / provider-neutral S2 final closeout and S3 handoff.
- Recorded S2 as closed only as provider-neutral durable metadata foundation / evidence readiness, with result `READY_TO_PROPOSE_PROVIDER_DECISION_SLICE`.
- Recorded that S2 does not choose, name, compare, score, shortlist, recommend, accept, or evidence any provider and does not authorize implementation.
- Recorded that TIP-33 hands off to S3 for any separately governed provider decision work.
- Recorded final TIP-25 gap register status: `G-001` resolved at decision-class level by accepted `DMT-LOSSLESS-VALIDATED`, `G-002` resolved at planning level by TIP-27, `G-003` resolved at planning level by TIP-28, and `G-004` resolved at planning level by TIP-29.
- Preserved all TIP-17 through TIP-32 boundaries, including `IDurableMetadataRepository`, `DurableMetadataWriteSet`, TIP-19 same-boundary semantics, forbidden-data absence, credential and secret non-storage boundaries, criteria-before-choice, decision-path-before-provider-choice, LocalDev evidence limits, provider-neutral evidence packet discipline, visible gap register discipline, and all capability/readiness non-claims.

### v0.54 - TIP-32 provider-neutral evidence gate recheck closeout indexed

- Added TIP-32 Provider-Neutral Evidence Gate Recheck / Provider Decision Readiness Precheck closeout draft to the index.
- Recorded TIP-32 as closed docs-only / precheck-only / provider-neutral evidence gate recheck and provider decision readiness precheck.
- Recorded homeowner/GPT acceptance of TIP-32 result `READY_TO_PROPOSE_PROVIDER_DECISION_SLICE`.
- Recorded that TIP-32 result means ready to propose a later separate provider decision slice, not ready to choose, compare, score, shortlist, recommend, accept, name, or evidence any provider in TIP-32.
- Recorded that TIP-32 closes with `G-001` resolved at decision-class level by accepted `DMT-LOSSLESS-VALIDATED`, `G-002` resolved at planning level by TIP-27, `G-003` resolved at planning level by TIP-28, and `G-004` resolved at planning level by TIP-29.
- Preserved that TIP-32 closeout authorizes no provider decision, provider naming, provider comparison, provider scoring, provider shortlisting, provider recommendation, provider acceptance, provider-specific evidence collection, implementation, runtime work, schema/index/migration work, package/dependency work, adapter work, LocalDev adapter work, backup/restore implementation, support, capability, readiness, legal reliance, external audit reliance, durable audit-store readiness, real durability, or provider suitability claim.

### v0.53 - TIP-32 provider-neutral evidence gate recheck indexed

- Added TIP-32 Provider-Neutral Evidence Gate Recheck / Provider Decision Readiness Precheck brief to the index.
- Recorded TIP-32 as draft docs-only / precheck-only / provider-neutral evidence gate recheck and provider decision readiness precheck.
- Recorded that TIP-32 rechecks the TIP-25 gap register after accepted TIP-31 closeout resolved `G-001` at decision-class level by accepted target class `DMT-LOSSLESS-VALIDATED`.
- Recorded that `G-001` is resolved at decision-class level only, `G-002` is resolved at planning level by TIP-27, `G-003` is resolved at planning level by TIP-28, and `G-004` is resolved at planning level by TIP-29, with no support, capability, implementation, or readiness claim.
- Recorded TIP-32 classification `READY_TO_PROPOSE_PROVIDER_DECISION_SLICE`, meaning ready to propose a later provider decision slice, not ready to choose or name a provider in TIP-32.
- Preserved that TIP-32 chooses no provider, compares no provider options, scores no provider options, shortlists no provider options, recommends no provider, accepts no provider, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no backup/recovery capability, restore capability, RPO/RTO support, provider suitability, production readiness, pilot readiness, certification readiness, legal reliance, external audit reliance, durable audit-store readiness, real durability, or operational readiness claim.

### v0.52 - TIP-31 RPO RTO target class decision closeout indexed

- Added TIP-31 Concrete RPO / RTO Target Class Decision closeout draft to the index.
- Recorded TIP-31 as closed docs-only / decision-only provider-neutral RPO/RTO target class decision.
- Recorded homeowner/GPT acceptance of target class `DMT-LOSSLESS-VALIDATED` as the requirement-only decision-class resolution for TIP-25 `G-001`.
- Recorded that `G-001` is resolved at decision-class level by accepted target class `DMT-LOSSLESS-VALIDATED`, with no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim.
- Preserved that provider decision remains blocked until accepted provider-neutral evidence packet gates are rechecked and satisfied under a separate governed slice.
- Preserved that TIP-31 closeout chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no backup, restore, monitoring, alerting, logging, runbook, LocalDev adapter, schema, migration, repository, adapter, package, or dependency work.

### v0.51 - TIP-31 RPO RTO target class decision brief indexed

- Added TIP-31 Concrete RPO / RTO Target Class Decision brief to the index.
- Recorded TIP-31 as a docs-only / decision-only provider-neutral target-class decision brief for the remaining TIP-25 `G-001` target-decision blocker.
- Recorded recommended target class `DMT-LOSSLESS-VALIDATED` as a requirement-only decision candidate: no accepted successful `DurableMetadataWriteSet` loss is tolerated, and restored durable metadata cannot be used as accepted durable truth until validation proves write-set completeness, audit/business consistency, idempotency/duplicate/conflict continuity, package/completion consistency, forbidden-data absence, environment separation, and provider-mechanics containment.
- Recorded that TIP-31 classifies `G-001` as resolved by accepted target class `DMT-LOSSLESS-VALIDATED`, subject to homeowner/GPT acceptance of the decision brief, with no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim.
- Preserved that provider decision remains blocked until homeowner/GPT accepts this decision brief and all prior evidence packet gates remain satisfied.
- Preserved that TIP-31 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no backup, restore, monitoring, alerting, logging, runbook, LocalDev adapter, schema, migration, repository, adapter, package, or dependency work.

### v0.50 - TIP-30 RPO RTO target decision closeout indexed

- Added TIP-30 RPO / RTO Target Decision Planning closeout draft to the index.
- Recorded TIP-30 as closed docs-only / planning-only provider-neutral RPO/RTO target decision planning.
- Recorded that TIP-30 accepts target-decision forms, target-decision acceptance criteria, RPO requirement baseline, RTO requirement baseline, restore validation evidence requirements, false-success prevention requirements, partial/unknown/interrupted outcome requirements, same-boundary write-set preservation requirements, idempotency/duplicate/conflict preservation requirements, audit/business consistency preservation requirements, package/completion preservation requirements, forbidden-data preservation requirements, operational ownership alignment, configuration/environment separation alignment, migration/rollback/exit alignment, evidence requirements, forbidden claims, and STOP/RRI requirements.
- Recorded that TIP-30 partially narrows TIP-25 `G-001` at planning level only, subject to homeowner/GPT acceptance of the closeout, with no concrete RPO/RTO target decision, no target class acceptance, no backup implementation, no restore implementation, no RPO/RTO support claim, no provider decision, no capability claim, and no readiness claim.
- Preserved that `G-001` remains blocked pending homeowner/GPT acceptance of concrete RPO/RTO target decisions or accepted target classes, while `G-002`, `G-003`, and `G-004` remain resolved at planning level and provider decision remains blocked.
- Preserved that TIP-30 closeout chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no backup, restore, monitoring, alerting, logging, runbook, LocalDev adapter, schema, migration, repository, adapter, package, or dependency work.

### v0.49 - TIP-30 RPO RTO target decision planning draft indexed

- Added TIP-30 RPO / RTO Target Decision Planning draft to the index.
- Recorded TIP-30 as docs-only, planning-only, provider-neutral RPO/RTO target decision planning for the remaining TIP-25 `G-001` blocker.
- Recorded that TIP-30 defines acceptable target-decision forms, recommended acceptance criteria, RPO requirement baseline, RTO requirement baseline, restore validation evidence requirements, false-success prevention requirements, partial/unknown/interrupted outcome requirements, same-boundary write-set preservation requirements, idempotency/duplicate/conflict preservation requirements, audit/business consistency preservation requirements, package/completion preservation requirements, forbidden-data preservation requirements, operational ownership alignment, configuration/environment separation alignment, migration/rollback/exit alignment, evidence requirements, forbidden claims, and STOP/RRI requirements.
- Recorded that TIP-30 does not set numeric or otherwise concrete RPO/RTO targets because no homeowner/GPT-accepted concrete target decision exists in the prompt or accepted repo baseline.
- Recorded that TIP-30 partially narrows `G-001`, which remains blocked pending homeowner/GPT acceptance of concrete RPO/RTO target decisions or accepted target classes.
- Preserved that `G-002`, `G-003`, and `G-004` remain resolved at planning level by prior accepted baselines, while provider decision remains blocked by unresolved `G-001`.
- Preserved that TIP-30 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no backup, restore, monitoring, alerting, logging, runbook, LocalDev adapter, schema, migration, repository, adapter, package, or dependency work.

### v0.48 - TIP-29 migration rollback exit closeout indexed

- Added TIP-29 Migration / Reversibility / Rollback / Exit Planning closeout draft to the index.
- Recorded TIP-29 as closed docs-only / planning-only provider-neutral migration, reversibility, rollback, abandon, replacement, exit, and provider-mechanics containment requirements planning.
- Recorded that TIP-29 accepts introduction requirements, provider-mechanics containment requirements, schema/index/migration authorization requirements, metadata shape evolution requirements, rollback requirements, abandon/failed decision requirements, replacement/exit requirements, durable truth preservation requirements, audit history/correction requirements, idempotency/duplicate/conflict preservation requirements, package/completion preservation requirements, forbidden-data preservation requirements, configuration/environment separation alignment, operational/incident alignment, backup/restore alignment, evidence requirements, forbidden claims, and STOP/RRI requirements.
- Recorded that TIP-29 resolves TIP-25 `G-004` at planning level only, subject to homeowner/GPT acceptance of the closeout, with no migration implementation, rollback implementation, provider-specific exit evidence, provider decision, capability, or readiness claim.
- Preserved that `G-001` remains partially blocked pending accepted RPO/RTO target decisions, `G-002` remains resolved at planning level, `G-003` remains resolved at planning level, and provider decision remains blocked.
- Preserved that TIP-29 closeout chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.47 - TIP-29 migration reversibility rollback exit planning draft indexed

- Added TIP-29 Migration / Reversibility / Rollback / Exit Planning draft to the index.
- Recorded TIP-29 as docs-only, planning-only, provider-neutral migration, reversibility, rollback, abandon, replacement, exit, and provider-mechanics containment requirements planning.
- Recorded that TIP-29 defines introduction requirements, provider-mechanics containment requirements, schema/index/migration authorization requirements, metadata shape evolution requirements, rollback requirements, abandon/failed decision requirements, replacement/exit requirements, durable truth preservation requirements, audit history/correction requirements, idempotency/duplicate/conflict preservation requirements, package/completion preservation requirements, forbidden-data preservation requirements, configuration/environment separation alignment, operational/incident alignment, backup/restore alignment, and evidence required before provider decision.
- Recorded that TIP-29 resolves TIP-25 `G-004` at planning level only, subject to homeowner/GPT acceptance, with no migration implementation, rollback implementation, provider-specific exit evidence, provider decision, capability, or readiness claim.
- Preserved that `G-001` remains partially blocked pending accepted RPO/RTO target decisions, `G-002` remains resolved at planning level, `G-003` remains resolved at planning level, and provider decision remains blocked.
- Preserved that TIP-29 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.46 - TIP-28 configuration environment separation closeout indexed

- Added TIP-28 Configuration / Environment Separation Planning closeout draft to the index.
- Recorded TIP-28 as closed docs-only / planning-only provider-neutral configuration and environment separation requirements planning.
- Recorded that TIP-28 accepts environment category requirements, production versus non-production separation requirements, LocalDev exclusion requirements, missing/ambiguous/invalid configuration behavior, fallback/default behavior requirements, credential/identity/reference separation requirements, runtime registration guardrail requirements, restore/incident/operations environment requirements, evidence requirements, and STOP/RRI requirements.
- Recorded that TIP-28 resolves TIP-25 `G-003` at planning level only, subject to homeowner/GPT acceptance of the closeout, with no runtime configuration implementation, environment enforcement, configuration enforcement capability, environment separation capability, provider decision, provider comparison, provider-specific evidence collection, readiness claim, or capability claim.
- Preserved that `G-001` remains partially blocked pending accepted RPO/RTO target decisions, `G-002` remains resolved at planning level, `G-004` remains blocked, and provider decision remains blocked.
- Preserved that TIP-28 closeout chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.45 - TIP-28 configuration environment separation planning draft indexed

- Added TIP-28 Configuration / Environment Separation Planning draft to the index.
- Recorded TIP-28 as docs-only, planning-only, provider-neutral configuration and environment separation requirements planning.
- Recorded that TIP-28 defines environment category requirements, production versus non-production separation requirements, LocalDev exclusion requirements, missing/ambiguous/invalid configuration behavior, fallback/default behavior requirements, credential/identity/reference separation requirements, runtime registration guardrail requirements, restore/incident/operations environment requirements, and evidence required before provider decision.
- Recorded that TIP-28 resolves TIP-25 `G-003` at planning level only, subject to homeowner/GPT acceptance, with no runtime configuration implementation, environment enforcement, provider decision, provider comparison, provider-specific evidence collection, readiness claim, or capability claim.
- Preserved that `G-001` remains partially blocked pending accepted RPO/RTO target decisions, `G-002` remains resolved at planning level, `G-004` remains blocked, and provider decision remains blocked.
- Preserved that TIP-28 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.44 - TIP-27 operational ownership incident handling closeout indexed

- Added TIP-27 Operational Ownership / Incident Handling Planning closeout draft to the index.
- Recorded TIP-27 as closed docs-only / planning-only provider-neutral operational ownership and incident handling requirements planning.
- Recorded that TIP-27 accepts the role/function ownership baseline for durable metadata operations, incident handling, quarantine, reconciliation, correction workflow, backup/recovery requirement approval, restore validation evidence, RPO/RTO target decision preparation or review, monitoring, alerting, logging, runbook, escalation, and STOP/RRI requirements.
- Recorded that TIP-27 resolves TIP-25 `G-002` at planning level only, subject to homeowner/GPT acceptance of the closeout, with no operational readiness, incident readiness, monitoring readiness, alerting readiness, logging readiness, runbook readiness, support, capability, provider suitability, or implementation claim.
- Recorded that TIP-27 further narrows the ownership-alignment side of `G-001`, while `G-001` remains partially blocked pending accepted RPO/RTO target decisions, and `G-003` and `G-004` remain blocked.
- Preserved that provider decision remains blocked and that TIP-27 closeout chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.43 - TIP-27 operational ownership incident handling planning draft indexed

- Added TIP-27 Operational Ownership / Incident Handling Planning draft to the index.
- Recorded TIP-27 as docs-only, planning-only, provider-neutral operational ownership and incident handling requirements planning.
- Recorded that TIP-27 defines role/function ownership for durable metadata operations, incident handling, quarantine, reconciliation, correction workflow, backup/recovery requirement approval, restore validation evidence, and RPO/RTO target decision preparation or review.
- Recorded that TIP-27 resolves TIP-25 `G-002` at planning level only, subject to homeowner/GPT acceptance, without claiming operational readiness, monitoring implementation, alerting implementation, logging implementation, runbook execution, backup/recovery support, restore capability, RPO/RTO support, provider suitability, or implementation.
- Recorded that TIP-27 further narrows the ownership-alignment side of `G-001`, while `G-001` remains partially blocked pending accepted RPO/RTO target decisions.
- Preserved that provider decision remains blocked by unresolved `G-001`, `G-003`, and `G-004`, and that TIP-27 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no runtime implementation, and opens no LocalDev adapter work.

### v0.42 - TIP-26 closeout draft indexed

- Added TIP-26 Backup / Recovery Requirements Planning closeout draft to the index.
- Recorded TIP-26 as closed docs-only / planning-only provider-neutral backup/recovery requirements planning.
- Recorded that TIP-26 accepts the backup/recovery requirement shape only and narrows TIP-25 `G-001` without fully resolving it.
- Preserved that `G-001` remains partially blocked pending accepted RPO/RTO target decisions and operational ownership alignment, while `G-002`, `G-003`, and `G-004` remain blocked.
- Preserved that TIP-26 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no backup/recovery, restore capability, RPO/RTO support, operational durability, readiness, legal reliance, external audit reliance, durable audit-store, or provider suitability claim.

### v0.41 - TIP-26 backup recovery requirements planning draft indexed

- Added TIP-26 Backup / Recovery Requirements Planning draft to the index.
- Recorded TIP-26 as docs-only, planning-only, provider-neutral backup/recovery requirements planning.
- Preserved that TIP-26 defines backup/recovery, restore consistency, restore scenarios, RPO/RTO, restore validation, quarantine, reconciliation, and false-success prevention requirements as requirements only.
- Recorded that TIP-26 narrows TIP-25 `G-001` by defining provider-neutral backup/recovery requirements, while `G-001` remains partially blocked pending accepted RPO/RTO target decisions and operational ownership alignment.
- Preserved that TIP-26 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no backup/recovery, restore capability, RPO/RTO support, operational durability, readiness, legal reliance, external audit reliance, durable audit-store, or provider suitability claim.

### v0.40 - TIP-25 evidence packet draft indexed

- Added TIP-25 Provider-Neutral Evidence Packet Assembly draft to the index.
- Recorded TIP-25 as docs-only, provider-neutral evidence packet assembly only.
- Preserved that TIP-25 assembles evidence from accepted repo docs and prior TIPs only; it chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no durability, backup/recovery, readiness, legal reliance, external audit reliance, or durable audit-store claim.
- Recorded that TIP-25 may assemble the packet while still blocking provider decision acceptance because backup/recovery requirements, operational ownership and incident handling, configuration/environment separation, and migration/reversibility/exit evidence remain planning gaps.

### v0.39 - TIP-24 closeout draft indexed

- Added TIP-24 Provider Decision Evidence Packet Assembly Planning closeout draft to the index.
- Recorded TIP-24 as closed docs-only / planning-only provider-neutral evidence packet assembly planning.
- Preserved that TIP-24 accepts only the packet assembly discipline for a future provider-neutral evidence packet: mandatory sections, source mapping to TIP-17 through TIP-23, proof checklist, pass/fail/block criteria, visible evidence gap register, reviewer responsibilities, and STOP/RRI gates; it chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, collects no provider-specific evidence, authorizes no implementation, and makes no durability, backup/recovery, readiness, legal reliance, external audit reliance, or durable audit-store claim.

### v0.38 - TIP-24 provider decision evidence packet assembly planning draft indexed

- Added TIP-24 Provider Decision Evidence Packet Assembly Planning planning brief to the index.
- Recorded TIP-24 as docs-only, planning-only, provider-neutral evidence packet assembly planning.
- Preserved that TIP-24 defines packet structure, evidence sources, proof checklist, pass/fail criteria, missing-evidence handling, reviewer responsibilities, and STOP/RRI gates only; it chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, authorizes no implementation, and makes no durability, backup/recovery, readiness, legal reliance, external audit reliance, or durable audit-store claim.

### v0.37 - TIP-23 closeout draft indexed

- Added TIP-23 Production Provider Decision Evidence Packet Planning closeout draft to the index.
- Recorded TIP-23 as closed docs-only / planning-only provider-neutral evidence-packet planning.
- Preserved that TIP-23 accepts only that a provider-neutral evidence packet must exist and be accepted before any future production durable metadata provider decision TIP; it chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, authorizes no implementation, and makes no durability, backup/recovery, readiness, legal reliance, external audit reliance, or durable audit-store claim.

### v0.36 - TIP-23 production provider decision evidence packet planning draft indexed

- Added TIP-23 Production Provider Decision Evidence Packet Planning planning brief to the index.
- Recorded TIP-23 as docs-only, planning-only, provider-neutral evidence-packet planning before any production durable metadata provider decision.
- Preserved that TIP-23 chooses no provider, compares no provider options, names no concrete provider/package/tool/runtime dependency, authorizes no implementation, and makes no durability, backup/recovery, readiness, legal reliance, external audit reliance, or durable audit-store claim.

### v0.35 - TIP-22 closeout draft indexed

- Added TIP-22 LocalDev-Only Durable Metadata Adapter Planning closeout draft to the index.
- Recorded TIP-22 as closed docs-only / planning-only LocalDev-only durable metadata adapter planning.
- Preserved that TIP-22 accepts only a strictly non-production LocalDev-only planning posture, authorizes no LocalDev adapter implementation, chooses no production provider, and opens no runtime, project/package, schema/index/migration, repository, Infrastructure adapter, backup/recovery, readiness, public contract, credential/raw storage, or SignFlow dependency work.

### v0.34 - TIP-22 LocalDev-only durable metadata adapter planning draft indexed

- Added TIP-22 LocalDev-Only Durable Metadata Adapter Planning planning brief to the index.
- Recorded TIP-22 as planning-only, docs-only, and LocalDev-only durable metadata adapter planning.
- Preserved that TIP-22 does not authorize LocalDev adapter implementation, does not choose a production provider, and does not open runtime, project/package, schema/index/migration, repository, Infrastructure adapter, backup/recovery, readiness, or SignFlow dependency work.

### v0.33 - TIP-21 closeout draft indexed

- Added TIP-21 Provider Decision Path Planning closeout draft to the index.
- Recorded TIP-21 as closed docs-only / planning-only provider decision path planning.
- Preserved that TIP-21 chose no provider, authorized no runtime implementation, opened no project/package, schema/index/migration, repository, Infrastructure adapter, LocalDev adapter, backup/recovery, readiness, or SignFlow dependency work.

### v0.32 - TIP-21 provider decision path planning draft indexed

- Added TIP-21 Provider Decision Path Planning planning brief to the index.
- Recorded TIP-21 as planning-only, docs-only, and provider-neutral provider decision path planning.
- Preserved that TIP-21 does not choose a provider, authorize runtime implementation, open project/package changes, decide schema/index/migration ownership, introduce repository or adapter work, claim backup/recovery or readiness, or create a SignFlow dependency.

### v0.31 - TIP-20 closeout draft indexed

- Added TIP-20 DB / Provider Evaluation Criteria Planning closeout draft to the index.
- Recorded TIP-20 as closed docs-only / planning-only provider evaluation criteria.
- Preserved that TIP-20 selected no provider, made no EF/non-EF decision, authorized no runtime implementation, and opened no project/package, schema/index/migration, repository, adapter, backup/recovery, readiness, or SignFlow dependency work.

### v0.30 - TIP-20 provider evaluation criteria planning draft indexed

- Added TIP-20 DB / Provider Evaluation Criteria Planning planning brief to the index.
- Recorded TIP-20 as planning-only, docs-only, and provider-neutral criteria before any durable metadata provider choice.
- Preserved that TIP-20 does not accept or close a provider decision and does not open runtime implementation, project/package changes, schema/index/migration decisions, repository implementation, adapters, backup/recovery claims, readiness claims, or SignFlow runtime/source/database/package/internal-model dependency.

### v0.29 - TIP-19 closeout draft indexed

- Added TIP-19 Transaction / Audit Consistency Planning closeout draft to the index.
- Recorded TIP-19 as closed docs-only / planning-only around provider-neutral transaction and audit consistency semantics.
- Preserved that TIP-19 opened no runtime implementation, selected no DB/provider, made no EF/non-EF, migration, schema, package, repository, Infrastructure adapter, LocalDev adapter, outbox/webhook/retry, backup/recovery, readiness, or SignFlow runtime dependency claim.

### v0.28 - TIP-19 transaction/audit consistency planning draft indexed

- Added TIP-19 Transaction / Audit Consistency Planning planning brief to the index.
- Recorded TIP-19 as docs-only, provider-neutral consistency semantics planning before any DB/provider, EF, migration, schema, package, repository, Infrastructure adapter, LocalDev adapter, outbox, webhook, retry, runtime test, readiness claim, or SignFlow runtime dependency work.
- Preserved that TIP-19 does not implement transaction consistency, audit durability, outbox delivery, backup/recovery, repository behavior, or production durability.

### v0.27 - TIP-18 closeout draft indexed

- Added TIP-18 closeout draft to the index.
- Recorded TIP-18 as closed docs-only around provider-neutral DB/provider posture, with no production DB/provider selection, EF/migration decision, schema, packages, repository implementation, infrastructure adapter, LocalDev adapter, readiness claim, backup/recovery capability, real durability claim, or SignFlow runtime dependency.
- Preserved that TIP-18 opens no runtime implementation.

### v0.26 - TIP-18 DB/provider posture decision draft indexed

- Added TIP-18 DB / Provider Posture Decision planning brief to the index.
- Recorded TIP-18 as docs-only planning/decision draft, not implemented and not closed.
- Preserved that TIP-18 does not select a production DB/provider, does not authorize EF/non-EF, migrations, packages, schema, repository implementation, Infrastructure adapter, LocalDev adapter, runtime tests, readiness claims, or SignFlow runtime dependency.

### v0.25 - TIP-17 implementation closeout indexed

- Recorded TIP-17 implementation commit `f6f65b8` as the provider-neutral durable metadata repository boundary baseline.
- Added TIP-17 closeout draft to the index.
- Preserved that TIP-17 closeout opens no TIP-18, DB/EF/migrations, adapter, production auth, credential store, public API/DTO behavior, webhook/outbox/retry, crypto/signing/replay, provider/vendor integration, readiness claim, or SignFlow runtime dependency.

### v0.24 - TIP-17 kickoff draft indexed

- Added TIP-17 Provider-Neutral Durable Metadata Repository Boundary kickoff draft to the index.
- Recorded TIP-17 as docs-only kickoff draft with no implementation dispatch, no `src/**`, no `tests/**`, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations/packages, no durable repository implementation, no production auth, no credential store, no secret backend, no raw secret or hashed secret storage, no raw artifact or biometric storage, no vault lifecycle, no retention enforcement, no deletion/purge/legal hold workflow, no webhook/outbox/retry/delivery implementation, no crypto/signing/replay, no provider/vendor integration, no readiness claim, and no SignFlow platform dependency.
- Recorded TIP-17 recommendation: remain kickoff-only pending homeowner/GPT review, then either prepare a separate extremely narrow implementation dispatch limited to provider-neutral durable metadata repository boundaries and forbidden-data leakage tests or keep TIP-17 kickoff-only if STOP/RRI gates remain unresolved.
- Synchronized TIP-16 as the accepted planning baseline feeding TIP-17.

### v0.23 - TIP-16 planning/kickoff draft indexed

- Added TIP-16 Durable Persistence Foundation planning/kickoff brief to the index.
- Recorded TIP-16 as docs-only planning/kickoff draft with no runtime implementation, no `src/**`, no tests, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations, no durable repository implementation, no production auth, no credential store, no secret backend, no raw secret storage, no raw artifact or biometric storage, no vault lifecycle, no webhook/outbox/retry/delivery implementation, no crypto/signing/replay, no provider/vendor integration, no readiness claim, and no SignFlow platform dependency.
- Recorded TIP-16 recommendation: remain planning-only now and prepare a later extremely narrow implementation kickoff only after homeowner/GPT review accepts the durable metadata repository posture and STOP/RRI guardrails.
- Synchronized TIP-15 as the accepted planning-only baseline feeding TIP-16.

### v0.22 - TIP-15 planning indexed

- Added TIP-15 Production Auth / Credential Lifecycle Boundary planning brief to the index.
- Recorded TIP-15 as docs-only planning with no runtime implementation, no `src/**`, no tests, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations, no credential store, no secret backend, no production identity provider integration, no OAuth/OIDC/mTLS/certificate implementation, no durable persistence, no webhook/outbox/retry, no vault/raw artifact lifecycle, no crypto/signing/replay, no provider/vendor integration, no readiness claim, and no SignFlow platform dependency.
- Synchronized TIP-14 as accepted planning-only baseline feeding TIP-15.

### v0.21 - TIP-14 planning indexed

- Added TIP-14 Post-TIP-13 S2 Debt Registry Convergence planning brief to the index.
- Recorded TIP-14 as planning-only with no runtime implementation, no source/test/API changes, no DB/EF/migrations, no durable persistence, no production auth, no credential store, no webhook/outbox/retry, no vault lifecycle, no crypto/signing/replay, no provider/vendor integration, no readiness claim, and no SignFlow platform dependency.

### v0.20 - TIP-13 closeout indexed

- Added the TIP-13 closeout document to the index.
- Recorded TIP-13 Option A as closed after implementation commit `6b9c672`.
- Preserved that no TIP-14 or new runtime work is opened by the closeout.

### v0.19 - TIP-13 implementation indexed

- Recorded TIP-13 Option A implementation commit `6b9c672`.
- Added the TIP-13 execution report to the index row.
- Cleared the stale "implementation not yet dispatched" wording.

### v0.18 - TIP-13 kickoff accepted

- Recorded GPT Gate acceptance of TIP-13 Option A kickoff.
- Preserved TIP-13 as kickoff-only with implementation not yet dispatched.
- Reconfirmed public API/DTO behavior changes are out of scope for TIP-13 Option A and require a separate reviewed TIP/kickoff.

### v0.17 - TIP-12 accepted and TIP-13 kickoff draft opened

- Recorded TIP-12 planning as accepted planning-only.
- Added TIP-13 application authorization boundary foundation kickoff draft to the TIP index.
- Recorded TIP-13 Option A as kickoff/planning only, pending review, with no implementation authorization.
- Preserved current LocalDev auth only and no production auth provider, credential store, durable persistence, webhook/outbox/retry, crypto/signing/replay, provider/vendor selection, pilot/production readiness, or SignFlow runtime dependency work.

### v0.16 - TIP-12 planning opened

- Added TIP-12 actor trust, caller scopes, and access boundary planning brief to the TIP index.
- Recorded TIP-11 Option B closeout commit as the baseline preceding TIP-12.
- Recorded TIP-12 as planning-only with no implementation dispatch.
- Preserved that no `src/**`, `tests/**`, production auth, credential lifecycle, database/durable persistence, webhook/outbox/retry, crypto/signing, provider/vendor, pilot-readiness, production-readiness, or SignFlow runtime dependency work is opened.

### v0.15 - TIP-11 Option B implementation closeout indexed

- Added TIP-11 Option B execution report and closeout to the TIP index.
- Recorded TIP-11 Option B implementation as complete pending homeowner/GPT closeout review.
- Preserved that no future TIPs or new runtime work are opened by the closeout.

### v0.14 - TIP-11 Option B kickoff accepted

- Recorded TIP-11 Option B kickoff as accepted.
- Preserved implementation as a separate next step under the accepted Option B dispatch allowlist.

### v0.13 - TIP-11 Option B kickoff draft indexed

- Added TIP-11 Option B kickoff draft to the TIP index.
- Preserved the kickoff as pending review and not authorized for implementation.

### v0.12 - TIP-11 planning accepted

- Recorded TIP-11 planning brief as accepted.
- Preserved Option B as the next kickoff candidate only, with no implementation dispatch.

### v0.11 - TIP-11 S2 planning opened

- Added TIP-11 production data boundary and durable state foundation planning brief.
- Recorded TIP-11 as S2 planning-only with no implementation dispatch.

### v0.10 - TIP-10 planning accepted

- Recorded TIP-10 planning brief as accepted.
- Preserved TIP-11 as the accepted next runtime-TIP recommendation, not an implementation dispatch.

### v0.9 - TIP-10 planning indexed

- Added TIP-10 production readiness planning compass to the TIP index.
- Preserved TIP-10 as planning-only with no runtime dispatch.

### v0.8 - TIP-09 closeout indexed

- Added TIP-09 S1 hardening closeout to the TIP index.
- Added previously omitted TIP-04, TIP-05, and TIP-06 implementation artifacts to the index table for continuity.

### v0.7 - TIP-08 indexed

- Added TIP-08 planning brief and closed kickoff document to the TIP index.

### v0.6 - TIP-07 indexed

- Added TIP-07 completion notification planning brief to the TIP index.

### v0.5 - TIP-03 closed

- Added TIP-03 closeout to the TIP index.

### v0.4 - TIP-03 execution report indexed

- Added TIP-03 execution report to the TIP index.

### v0.3 - TIP-03 kickoff v0.2 accepted

- Updated TIP-03 kickoff index to the accepted v0.2 review-patched document.

### v0.2 - TIP-03 kickoff indexed

- Added TIP-02A confirmation report to the TIP-02 artifact list.
- Added TIP-03 core domain/contracts kickoff document.

### v0.1 - TIP index introduced

- Added a central index for TIP folders.
- Recorded the canonical TIP folder and artifact filename convention.

## Naming Convention

Each TIP has its own folder:

```text
docs/tips/tip_XX_short_slug/
```

Each TIP artifact uses:

```text
tip_XX_<artifact>_vA_B.md
```

Canonical artifact names:

- `brief`
- `kickoff`
- `review`
- `execution_report`
- `closeout`
- `roadmap`

## TIP Folders

| TIP | Folder | Current artifact |
| --- | --- | --- |
| TIP-01 | `tip_01_project_skeleton/` | [`tip_01_brief_v0_1.md`](tip_01_project_skeleton/tip_01_brief_v0_1.md), [`tip_01_execution_report_v0_1.md`](tip_01_project_skeleton/tip_01_execution_report_v0_1.md), [`tip_01_review_v0_1.md`](tip_01_project_skeleton/tip_01_review_v0_1.md) |
| TIP-02 | `tip_02_s1_execution/` | [`tip_02_roadmap_v0_2.md`](tip_02_s1_execution/tip_02_roadmap_v0_2.md), [`tip_02_review_v0_3.md`](tip_02_s1_execution/tip_02_review_v0_3.md), [`tip_02a_confirmation_report_v0_1.md`](tip_02_s1_execution/tip_02a_confirmation_report_v0_1.md) |
| TIP-03 | `tip_03_core_domain_contracts/` | [`tip_03_kickoff_v0_2.md`](tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md), [`tip_03_execution_report_v0_1.md`](tip_03_core_domain_contracts/tip_03_execution_report_v0_1.md), [`tip_03_review_v0_1.md`](tip_03_core_domain_contracts/tip_03_review_v0_1.md), [`tip_03_closeout_v0_1.md`](tip_03_core_domain_contracts/tip_03_closeout_v0_1.md) |
| TIP-04 | `tip_04_api_key_policy_session_lifecycle/` | [`tip_04_kickoff_v0_3.md`](tip_04_api_key_policy_session_lifecycle/tip_04_kickoff_v0_3.md), [`tip_04_execution_report_v0_1.md`](tip_04_api_key_policy_session_lifecycle/tip_04_execution_report_v0_1.md) |
| TIP-05 | `tip_05_capture_artifact_evidence_recording/` | [`tip_05_kickoff_v0_3.md`](tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_3.md), [`tip_05_execution_report_v0_1.md`](tip_05_capture_artifact_evidence_recording/tip_05_execution_report_v0_1.md) |
| TIP-06 | `tip_06_final_decision_evidence_package/` | [`tip_06_kickoff_v0_1.md`](tip_06_final_decision_evidence_package/tip_06_kickoff_v0_1.md) |
| TIP-07 | `tip_07_completion_notification/` | [`tip_07_planning_brief_v0_3.md`](tip_07_completion_notification/tip_07_planning_brief_v0_3.md) |
| TIP-08 | `tip_08_signflow_transaction_bound_profile/` | [`tip_08_planning_brief_v0_3.md`](tip_08_signflow_transaction_bound_profile/tip_08_planning_brief_v0_3.md), [`tip_08_kickoff_v0_4.md`](tip_08_signflow_transaction_bound_profile/tip_08_kickoff_v0_4.md) |
| TIP-09 | `tip_09_s1_hardening_closeout/` | [`tip_09_closeout_v0_1.md`](tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md) |
| TIP-10 | `tip_10_production_readiness_planning_compass/` | [`tip_10_planning_brief_v0_1.md`](tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md) - accepted planning-only |
| TIP-11 | `tip_11_production_data_boundary_durable_state_foundation/` | [`tip_11_planning_brief_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md) - accepted planning-only, [`tip_11_kickoff_option_b_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_kickoff_option_b_v0_1.md) - accepted Option B kickoff, [`tip_11_option_b_execution_report_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_execution_report_v0_1.md) - implemented, [`tip_11_option_b_closeout_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md) - committed closeout baseline |
| TIP-12 | `tip_12_actor_trust_caller_scopes_access_boundary/` | [`tip_12_planning_brief_v0_1.md`](tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md) - accepted planning-only |
| TIP-13 | `tip_13_application_authorization_boundary_foundation/` | [`tip_13_kickoff_option_a_v0_1.md`](tip_13_application_authorization_boundary_foundation/tip_13_kickoff_option_a_v0_1.md) - accepted kickoff, [`tip_13_option_a_execution_report_v0_1.md`](tip_13_application_authorization_boundary_foundation/tip_13_option_a_execution_report_v0_1.md) - implemented by commit `6b9c672`, [`tip_13_closeout_v0_1.md`](tip_13_application_authorization_boundary_foundation/tip_13_closeout_v0_1.md) - closed |
| TIP-14 | `tip_14_post_tip_13_s2_debt_registry_convergence/` | [`tip_14_planning_brief_v0_1.md`](tip_14_post_tip_13_s2_debt_registry_convergence/tip_14_planning_brief_v0_1.md) - accepted planning-only |
| TIP-15 | `tip_15_production_auth_credential_lifecycle_boundary/` | [`tip_15_planning_brief_v0_1.md`](tip_15_production_auth_credential_lifecycle_boundary/tip_15_planning_brief_v0_1.md) - accepted planning-only |
| TIP-16 | `tip_16_durable_persistence_foundation/` | [`tip_16_planning_brief_v0_1.md`](tip_16_durable_persistence_foundation/tip_16_planning_brief_v0_1.md) - accepted and committed planning/kickoff |
| TIP-17 | `tip_17_provider_neutral_durable_metadata_repository_boundary/` | [`tip_17_kickoff_v0_1.md`](tip_17_provider_neutral_durable_metadata_repository_boundary/tip_17_kickoff_v0_1.md) - accepted kickoff, implemented by commit `f6f65b8`, [`tip_17_closeout_v0_1.md`](tip_17_provider_neutral_durable_metadata_repository_boundary/tip_17_closeout_v0_1.md) - closed by closeout commit `7424d55` |
| TIP-18 | `tip_18_db_provider_posture_decision/` | [`tip_18_planning_brief_v0_1.md`](tip_18_db_provider_posture_decision/tip_18_planning_brief_v0_1.md) - planning/decision draft, [`tip_18_closeout_v0_1.md`](tip_18_db_provider_posture_decision/tip_18_closeout_v0_1.md) - closeout draft; closed docs-only, not implemented |
| TIP-19 | `tip_19_transaction_audit_consistency_planning/` | [`tip_19_planning_brief_v0_1.md`](tip_19_transaction_audit_consistency_planning/tip_19_planning_brief_v0_1.md) - accepted planning baseline, [`tip_19_closeout_v0_1.md`](tip_19_transaction_audit_consistency_planning/tip_19_closeout_v0_1.md) - closed docs-only / planning-only; no runtime implementation and no DB/provider selection |
| TIP-20 | `tip_20_db_provider_evaluation_criteria_planning/` | [`tip_20_planning_brief_v0_1.md`](tip_20_db_provider_evaluation_criteria_planning/tip_20_planning_brief_v0_1.md) - accepted planning baseline, [`tip_20_closeout_v0_1.md`](tip_20_db_provider_evaluation_criteria_planning/tip_20_closeout_v0_1.md) - closed docs-only / planning-only provider evaluation criteria; no provider selected and no runtime implementation authorized |
| TIP-21 | `tip_21_provider_decision_path_planning/` | [`tip_21_planning_brief_v0_1.md`](tip_21_provider_decision_path_planning/tip_21_planning_brief_v0_1.md) - accepted planning baseline, [`tip_21_closeout_v0_1.md`](tip_21_provider_decision_path_planning/tip_21_closeout_v0_1.md) - closed docs-only / planning-only provider decision path planning; no provider selected and no runtime implementation authorized |
| TIP-22 | `tip_22_localdev_only_durable_metadata_adapter_planning/` | [`tip_22_planning_brief_v0_1.md`](tip_22_localdev_only_durable_metadata_adapter_planning/tip_22_planning_brief_v0_1.md) - accepted planning baseline, [`tip_22_closeout_v0_1.md`](tip_22_localdev_only_durable_metadata_adapter_planning/tip_22_closeout_v0_1.md) - closed docs-only / planning-only LocalDev-only durable metadata adapter planning; no LocalDev adapter implementation and no production provider decision authorized |
| TIP-23 | `tip_23_production_provider_decision_evidence_packet_planning/` | [`tip_23_planning_brief_v0_1.md`](tip_23_production_provider_decision_evidence_packet_planning/tip_23_planning_brief_v0_1.md) - accepted planning baseline, [`tip_23_closeout_v0_1.md`](tip_23_production_provider_decision_evidence_packet_planning/tip_23_closeout_v0_1.md) - closed docs-only / planning-only provider decision evidence packet planning; no provider selected, compared, named, or implemented |
| TIP-24 | `tip_24_provider_decision_evidence_packet_assembly_planning/` | [`tip_24_planning_brief_v0_1.md`](tip_24_provider_decision_evidence_packet_assembly_planning/tip_24_planning_brief_v0_1.md) - accepted planning baseline, [`tip_24_closeout_v0_1.md`](tip_24_provider_decision_evidence_packet_assembly_planning/tip_24_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral evidence packet assembly planning; no provider selected, compared, named, evidenced, or implemented |
| TIP-25 | `tip_25_provider_neutral_evidence_packet_assembly/` | [`tip_25_evidence_packet_v0_1.md`](tip_25_provider_neutral_evidence_packet_assembly/tip_25_evidence_packet_v0_1.md) - draft docs-only provider-neutral evidence packet assembly; provider decision remains blocked by visible planning gaps |
| TIP-26 | `tip_26_backup_recovery_requirements_planning/` | [`tip_26_planning_brief_v0_1.md`](tip_26_backup_recovery_requirements_planning/tip_26_planning_brief_v0_1.md) - accepted planning baseline, [`tip_26_closeout_v0_1.md`](tip_26_backup_recovery_requirements_planning/tip_26_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral backup/recovery requirements planning; narrows TIP-25 `G-001` by defining requirement shape while leaving it partially blocked pending accepted RPO/RTO target decisions and operational ownership alignment, with no capability, readiness, provider suitability, provider decision, or implementation claim |
| TIP-27 | `tip_27_operational_ownership_incident_handling_planning/` | [`tip_27_planning_brief_v0_1.md`](tip_27_operational_ownership_incident_handling_planning/tip_27_planning_brief_v0_1.md) - accepted planning baseline, [`tip_27_closeout_v0_1.md`](tip_27_operational_ownership_incident_handling_planning/tip_27_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral operational ownership and incident handling requirements planning; resolves TIP-25 `G-002` at planning level only, further narrows the ownership-alignment side of `G-001`, and leaves provider decision blocked pending unresolved `G-001`, `G-003`, and `G-004` |
| TIP-28 | `tip_28_configuration_environment_separation_planning/` | [`tip_28_planning_brief_v0_1.md`](tip_28_configuration_environment_separation_planning/tip_28_planning_brief_v0_1.md) - accepted planning baseline, [`tip_28_closeout_v0_1.md`](tip_28_configuration_environment_separation_planning/tip_28_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral configuration and environment separation requirements planning; resolves TIP-25 `G-003` at planning level only, leaves provider decision blocked pending unresolved `G-001` and `G-004`, and makes no runtime configuration implementation, environment enforcement, capability, provider decision, or readiness claim |
| TIP-29 | `tip_29_migration_reversibility_rollback_exit_planning/` | [`tip_29_planning_brief_v0_1.md`](tip_29_migration_reversibility_rollback_exit_planning/tip_29_planning_brief_v0_1.md) - accepted planning baseline, [`tip_29_closeout_v0_1.md`](tip_29_migration_reversibility_rollback_exit_planning/tip_29_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral migration, reversibility, rollback, abandon, replacement, exit, and provider-mechanics containment requirements planning; resolves TIP-25 `G-004` at planning level only, leaves provider decision blocked pending unresolved `G-001`, and makes no migration implementation, rollback implementation, provider-specific exit evidence, capability, provider decision, or readiness claim |
| TIP-30 | `tip_30_rpo_rto_target_decision_planning/` | [`tip_30_planning_brief_v0_1.md`](tip_30_rpo_rto_target_decision_planning/tip_30_planning_brief_v0_1.md) - accepted planning baseline, [`tip_30_closeout_v0_1.md`](tip_30_rpo_rto_target_decision_planning/tip_30_closeout_v0_1.md) - closeout draft; closed docs-only / planning-only provider-neutral RPO/RTO target decision planning; partially narrows TIP-25 `G-001` at planning level only, leaves `G-001` blocked pending homeowner/GPT acceptance of concrete RPO/RTO target decisions or accepted target classes, and makes no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim |
| TIP-31 | `tip_31_rpo_rto_target_class_decision/` | [`tip_31_decision_brief_v0_1.md`](tip_31_rpo_rto_target_class_decision/tip_31_decision_brief_v0_1.md) - accepted decision brief, [`tip_31_closeout_v0_1.md`](tip_31_rpo_rto_target_class_decision/tip_31_closeout_v0_1.md) - closeout draft; closed docs-only / decision-only provider-neutral RPO/RTO target class decision; resolves TIP-25 `G-001` at decision-class level by accepted target class `DMT-LOSSLESS-VALIDATED`, with no backup/recovery capability, restore capability, RPO/RTO support, provider decision, implementation, or readiness claim |
| TIP-32 | `tip_32_provider_neutral_evidence_gate_recheck/` | [`tip_32_precheck_brief_v0_1.md`](tip_32_provider_neutral_evidence_gate_recheck/tip_32_precheck_brief_v0_1.md) - accepted precheck brief, [`tip_32_closeout_v0_1.md`](tip_32_provider_neutral_evidence_gate_recheck/tip_32_closeout_v0_1.md) - closeout draft; closed docs-only / precheck-only provider-neutral evidence gate recheck and provider decision readiness precheck; accepts `READY_TO_PROPOSE_PROVIDER_DECISION_SLICE`, meaning ready to propose a later provider decision slice, not ready to choose or name a provider in TIP-32; makes no provider decision, implementation, capability, support, or readiness claim |
| TIP-33 | `tip_33_s2_final_closeout_s3_handoff/` | [`tip_33_s2_closeout_v0_1.md`](tip_33_s2_final_closeout_s3_handoff/tip_33_s2_closeout_v0_1.md) - draft docs-only / closeout-only / handoff-only provider-neutral S2 final closeout and S3 handoff; closes S2 only as provider-neutral durable metadata foundation / evidence readiness with result `READY_TO_PROPOSE_PROVIDER_DECISION_SLICE`; hands off any provider decision work to S3 under separate scope and review; makes no provider decision, implementation, capability, support, or readiness claim |
