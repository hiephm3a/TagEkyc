# TIP-14 Post-TIP-13 S2 Debt Registry Convergence Planning Brief v0.1

**File:** `docs/tips/tip_14_post_tip_13_s2_debt_registry_convergence/tip_14_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-14
**Baseline:** `c92f305e596a77d1b48a80348d43c5dbe3bdb57d`
**Purpose:** Reclassifies and converges remaining post-S1 / S2 production-readiness debts after TIP-10, TIP-11, TIP-12, and TIP-13 before governance chooses the next implementation slice.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-14 as a docs-only post-TIP-13 S2 debt registry convergence brief.
- Reclassified remaining post-S1 / S2 production-readiness debts into resolved LocalDev/S1 scope, deferred production-readiness debt, STOP/RRI pending decision, hard boundary/non-goal, and candidate next governed slice buckets.
- Recorded precise resolved-scope limits for TIP-11 and TIP-13.
- Added STOP/RRI analysis for production auth, durable persistence, retention/legal hold, provider trust, privileged support, production completion authority, production crypto/signing, webhook reliance, pilot/production readiness, and SignFlow boundary changes.
- Compared production auth / credential lifecycle against durable persistence foundation as the likely next governed slice.
- Preserved no implementation, no source/test/API changes, no DB/EF/migrations, no durable persistence, no credential store, no webhook/outbox/retry, no vault lifecycle, no crypto/signing/replay, no provider integration, no pilot/production/certification readiness claim, and no SignFlow platform dependency.

## 1. Purpose

TIP-14 is planning only. It does not authorize runtime implementation, source changes, tests, public API changes, storage work, credential work, crypto work, webhook work, provider work, pilot readiness, production readiness, or certification claims.

The goal is to converge the debt registry after four accepted post-S1 governance steps:

- TIP-10 ranked production-readiness dependencies and recommended starting with foundational boundaries.
- TIP-11 closed a LocalDev metadata/data-boundary primitive slice only.
- TIP-12 accepted actor trust, caller scopes, ownership, and access-boundary planning.
- TIP-13 closed LocalDev application authorization boundary hardening using current LocalDev auth only.

S1 remains closed as LocalDev evidence-ready only. S1 is non-production and non-certified.

## 2. Baseline Inputs

- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/tips/tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md`
- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md`
- `docs/tips/tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md`
- `docs/tips/tip_13_application_authorization_boundary_foundation/tip_13_closeout_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/tips/README.md`

Baseline check before drafting:

```text
HEAD: c92f305e596a77d1b48a80348d43c5dbe3bdb57d
Worktree: clean
Validation: dotnet test TagEkyc.sln --no-restore = 81 passed, 0 failed, 0 skipped
```

## 3. Current State After TIP-13

Resolved LocalDev/S1 scope:

- S1 is closed as LocalDev evidence-ready, non-production, and non-certified.
- TIP-11 resolved LocalDev metadata/data-boundary primitives only.
- TIP-13 resolved LocalDev application authorization boundary only.

Important precision:

- TIP-11 did not resolve durable persistence, raw artifact storage, retention enforcement, deletion, legal hold workflow, durable recovery, durable audit storage, or a durable policy catalog.
- TIP-13 did not resolve production auth, credential lifecycle, credential store, rotation, revocation, admin lifecycle, external identity provider, or secret backend.

No TIP-14 implementation is open.

## 4. Classification Buckets

TIP-14 uses these classification buckets:

| Bucket | Meaning |
| --- | --- |
| LocalDev/S1 resolved scope | Implemented or accepted only for the current LocalDev/non-production boundary. |
| Deferred production-readiness debt | Required before pilot, production reliance, production support, audit reliance, scale, or certification, but not opened by TIP-14. |
| STOP/RRI pending decision | Requires homeowner/GPT/review risk intake before implementation can safely proceed. |
| Hard boundary / non-goal | Must remain prohibited unless a later explicit homeowner RRI changes the boundary. |
| Candidate next governed slice | Suitable for a later planning, kickoff, or implementation TIP after governance selects it. |

## 5. Debt Convergence Table

| Area | Classification | Priority | Current resolved scope | Remaining debt / decision | Notes |
| --- | --- | --- | --- | --- | --- |
| S1 LocalDev closeout | LocalDev/S1 resolved scope | Closed | TIP-09 closed S1 as LocalDev evidence-ready only. | None for S1 closeout. | Does not imply pilot, production, legal, or certification readiness. |
| TIP-11 metadata/data-boundary primitives | LocalDev/S1 resolved scope | Closed partial | Internal metadata primitives, retention/legal-hold classifications, deletion eligibility markers, and deterministic LocalDev policy snapshot identity. | Durable persistence, durable policy catalog, raw storage, enforcement, deletion, legal hold workflow remain open. | Resolved scope is intentionally metadata-only. |
| TIP-13 application authorization boundary | LocalDev/S1 resolved scope | Closed partial | Centralized LocalDev actor/category/scope/ownership checks through application authorization. | Production auth, credential lifecycle, credential store, rotation, revocation, admin lifecycle, and secret backend remain open. | Resolved scope uses current LocalDev auth only. |
| Production auth / credential lifecycle | Candidate next governed slice; deferred production-readiness debt; STOP/RRI | P0 | LocalDev API key shape only. | Production identity provider or secret store, credential refs, rotation, revocation, onboarding, admin lifecycle, break-glass, and audit identity. | Strong next-slice candidate because credential refs and actor refs shape durable schema. |
| Durable persistence / migrations / recovery | Candidate next governed slice; deferred production-readiness debt; STOP/RRI | P0/P1 | No durable store. TIP-11 only created metadata primitives. | DB/provider posture, repositories, migrations, backup, recovery, durable audit, tenant ownership indexes, outbox substrate. | Strong next-slice candidate but must not freeze unratified identity assumptions. |
| Policy catalog / reproducible decisions | Deferred production-readiness debt; candidate sub-scope | P0/P1 | Deterministic LocalDev policy snapshot identity only. | Durable versioned policy catalog, policy grant lifecycle, reproducible thresholds/checks, decision replay inputs. | Best paired with auth and durable persistence planning. |
| Vault/raw artifact lifecycle | Deferred production-readiness debt; STOP/RRI | P0 | VaultRef/hash concepts and metadata classifications only. | Vault object ownership, access policy, storage boundary, lifecycle states, purge workflow, legal hold interactions. | Must precede real raw artifact storage. |
| Raw biometric protection | Deferred production-readiness debt; STOP/RRI | P0 | Business DTOs exclude raw biometric/internal payloads. No raw storage. | Encryption, access control, vault isolation, retention, deletion, audit, incident response, consent/legal basis. | Blocks real biometric data collection. |
| Retention / legal hold / deletion | Deferred production-readiness debt; STOP/RRI | P0 | TIP-11 metadata enums/markers only. | Legal retention classes, deletion triggers, purge eligibility, hold authority, enforcement workflow. | Governance/legal decision required before implementation. |
| Webhook/outbox/retry/delivery ledger | Deferred production-readiness debt | P1 | TIP-07 internal completion projection only. | Subscription model, durable outbox, dispatcher, retry/backoff, delivery ledger, dead-letter, replay tooling. | Should follow durable state and webhook signing/replay planning. |
| Webhook signing / replay protection | Deferred production-readiness debt; STOP/RRI | P1 | Placeholder status only. | Signing algorithm, secret lifecycle, delivery id, timestamp tolerance, replay cache, rotation, verification rules. | Depends on credential/subscription trust and durable delivery ids. |
| Evidence package signing / key management | Deferred production-readiness debt; STOP/RRI | P1 | Deterministic hashes and placeholder signature status only. | Signing authority, key ownership, KMS/HSM posture, rotation, verification process, audit reliance policy. | Required before external audit reliance. |
| Provider/vendor/device trust | Deferred production-readiness debt; STOP/RRI | P0/P1 | LocalDev CaptureAgent and TrustedAdapter categories with scoped writes. | Device enrollment, adapter onboarding, allowed result types, provider attestation, SDK trust, revocation, vendor assurance. | Must precede real provider authority. |
| Operator/Admin/System privileged support workflow | Deferred production-readiness debt; STOP/RRI | P1/P2 | Operator/Admin/System remain reserved or internal concepts only. | Support lookup, privileged reads, reason codes, approval gates, admin actions, audit review. | Needs explicit privileged access policy. |
| Production completion authority | STOP/RRI pending decision; candidate planning slice | P0/P1 | BusinessConsumer can complete in LocalDev S1 only. | Decide who finalizes in production and how requests, policy evaluation, operator review, and package creation are audited. | Must be settled before production completion implementation. |
| Pilot / production readiness certification | STOP/RRI pending decision; deferred production-readiness debt | P0 | Explicitly not claimed. | Jurisdiction, use case, consent, certification target, legal reliance, regulatory reporting, operational readiness gates. | No future TIP may imply readiness without accepted governance. |
| SignFlow integration boundary | Hard boundary / non-goal | Hard | SignFlow is external consumer profile only. | Any runtime/source/database/network/package/internal-model dependency requires explicit homeowner RRI. | Boundary remains invariant. |
| Specialized evidence endpoints | Deferred production-readiness debt | P2 | Generic TrustedAdapter `/evidence-results` route accepted for LocalDev S1. | Specialized routes only if provider/trust shape requires them later. | Not a near-term foundation blocker. |
| Fingerprint default enablement | Deferred production-readiness debt | P1 | Optional/demo/deferred; not default SignFlow S1. | Device trust, SDK integration, secure template handling, policy enablement, privacy controls. | Must not become mandatory without accepted provider/device/privacy plan. |

## 6. STOP/RRI Table

| STOP/RRI item | Decision needed | Why it blocks or shapes future implementation | Candidate options | Forbidden assumptions |
| --- | --- | --- | --- | --- |
| Production auth / credential lifecycle | Choose identity and credential posture for BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System/InternalService. | Credential refs, actor refs, tenant bindings, audit identity, support access, and secret lifecycle affect nearly every durable schema and authorization rule. | Managed API keys with hashed secrets; OAuth/OIDC clients; mTLS/certificates; service principals; staged LocalDev-compatible abstraction. | Do not assume static LocalDev keys are production secrets. Do not store raw secrets. Do not add a credential store without approval. |
| Durable persistence posture | Decide whether and how to introduce durable repositories, DB provider, migration policy, backup, and recovery. | Persistence locks in tenant keys, actor refs, policy refs, audit schemas, retention metadata, and outbox substrates. | Planning-only schema posture; local dev provider only; production-neutral repository abstraction; explicit DB provider selection. | Do not freeze unratified identity, credential, retention, or support-access assumptions. Do not add DB/EF/migrations in TIP-14. |
| Policy catalog / reproducible decisions | Decide how client policies, RequiredChecks, thresholds, and policy snapshots become versioned and durable. | Production decisions must be explainable and reproducible; policy identity affects session, evidence package, audit, and completion records. | Durable policy catalog; append-only policy versions; policy snapshots copied onto sessions; immutable policy bundle ids. | Do not treat LocalDev `LOCALDEV-S1-POLICY-V1` as a production catalog. Do not expose internal policy metadata through BusinessConsumer DTOs without review. |
| Vault/raw artifact lifecycle | Decide vault boundary, artifact ownership, lifecycle states, access roles, and metadata allowed in application persistence. | Real raw artifacts and biometrics cannot be stored safely without lifecycle, access, audit, and deletion rules. | Metadata-only application store; separate vault service; provider-owned artifact refs; no raw storage until later. | Do not store raw bytes in application persistence. Do not expose internal VaultRefs to BusinessConsumer outputs. |
| Raw biometric protection | Decide encryption, access control, consent/legal basis, audit, and incident-response posture for biometric data. | Biometric storage and processing create high legal, privacy, and security exposure. | No real biometric storage; vault-isolated encrypted storage; provider-tokenized refs only; jurisdiction-specific pilot controls. | Do not collect real biometrics under LocalDev proof assumptions. Do not log or serialize raw biometric payloads. |
| Retention / legal hold / deletion | Decide retention classes, deletion triggers, purge authority, legal hold authority, and enforcement semantics. | Retention and deletion affect vault lifecycle, durable metadata, audit, support workflows, and legal compliance. | Conservative metadata-only markers; legal-hold blocks purge; tenant-specific retention; jurisdiction-specific retention profiles. | Do not implement retention enforcement or legal hold workflow without legal/governance approval. |
| Webhook signing / replay protection | Decide signing format, key/secret owner, timestamp tolerance, delivery id, replay cache, and rotation model. | Webhook reliability without authenticity and replay controls can mislead consumers and create unsafe integration reliance. | HMAC with per-subscription secrets; JWS; mTLS plus signed payloads; no production webhook reliance until later. | Do not ship unsigned production webhooks. Do not reuse LocalDev API keys as webhook signing secrets by assumption. |
| Evidence package signing / key management | Decide signing authority, key custody, algorithm, rotation, verification process, and audit/legal reliance target. | Signing semantics determine whether evidence packages can support external audit or legal non-repudiation claims. | Placeholder-only remains; KMS-backed signing; HSM-backed signing; internal verification only; external verifier package. | Do not claim audit reliance from hashes or placeholder signatures. Do not select KMS/HSM/vendor without RRI. |
| Provider/vendor/device trust | Decide provider selection authority, device enrollment, adapter onboarding, allowed evidence types, attestation, revocation, and vendor assurance. | TrustedAdapter evidence can drive decisions only if provider/device authority and provenance are reliable. | Planning-only trust catalog; LocalDev adapters only; approved provider registry; device-bound CaptureAgent credentials. | Do not let BusinessConsumer submit arbitrary `PASSED` evidence. Do not select vendors by implication. |
| Operator/Admin/System privileged support workflow | Decide support roles, cross-client lookup policy, reason codes, privileged reads, admin actions, and audit requirements. | Privileged workflows can pierce tenant or sensitive-data boundaries and must be designed before implementation. | No privileged support initially; audited operator read-only support; admin credential/policy management only; dual-control overrides. | Do not grant broad cross-client lookup. Do not allow raw artifact/biometric reads by default. |
| Production completion authority | Decide who may finalize production sessions and under what evidence, policy, and audit conditions. | Completion triggers final decision, evidence package creation, notification eligibility, and downstream reliance. | BusinessConsumer completion remains LocalDev-only; System/InternalService finalizes after policy evaluation; Operator review/override under privileged audited workflow; split model where BusinessConsumer requests completion and System finalizes. | Do not assume S1 BusinessConsumer completion is production authority. Do not allow Operator/Admin override without reason, audit, and policy. |
| Pilot / production readiness certification | Decide pilot target, jurisdiction, data categories, consent, certification expectations, and readiness gates. | The system cannot handle real-user or regulated reliance claims without legal/compliance acceptance. | Engineering hardening only; limited no-reliance pilot; jurisdiction-specific pilot; production certification track. | Do not claim pilot readiness, production readiness, certified eKYC, or legal reliance from TIP-14. |
| SignFlow boundary change | Decide whether homeowner explicitly accepts changing SignFlow from external consumer profile to platform dependency. | Any dependency would alter product architecture, deployment coupling, data access, and ownership boundaries. | Keep external consumer profile; explicit RRI for any dependency change. | Do not introduce SignFlow runtime/source/database/network/package/internal-model dependency. |

## 7. Hard Boundary / Non-Goal Table

| Boundary | Invariant |
| --- | --- |
| S1 posture | S1 remains LocalDev evidence-ready only, non-production, and non-certified. |
| TIP-14 scope | TIP-14 is docs-only planning and does not dispatch implementation. |
| Source/test changes | No `src/**`, no tests, no behavior changes. |
| Public contracts | No public API, DTO, JSON, status, or error behavior change. |
| Durable persistence | No DB, EF, migrations, durable repositories, durable adapter, backup, or recovery implementation. |
| Production auth | No production auth provider, credential store, secret backend, rotation, revocation, or admin lifecycle implementation. |
| Webhook | No public webhook route, subscription, dispatcher, outbox, retry, delivery ledger, replay cache, signing, or delivery implementation. |
| Vault/raw artifacts | No raw artifact storage, biometric storage, vault lifecycle implementation, retention enforcement, deletion, or legal hold workflow. |
| Crypto/signing | No HMAC, JWS, KMS, HSM, evidence package signing, webhook signing, replay protection, or key rotation implementation. |
| Providers/vendors | No provider, vendor, engine, device, cloud service, legal/compliance provider, or paid service selection/integration. |
| Readiness claims | No pilot readiness, production readiness, certified eKYC, external audit reliance, or legal reliance claim. |
| BusinessConsumer data boundary | No raw artifacts, biometrics, internal VaultRefs, internal manifests, raw secrets, or sensitive provider internals exposed to BusinessConsumer outputs. |
| SignFlow | SignFlow remains an external consumer profile only. No SignFlow runtime/source/database/network/package/internal-model dependency is allowed without explicit homeowner RRI. |

## 8. Candidate Next Governed Slice Analysis

| Candidate | Verdict | Why now | Why not now | Dependencies | Expected scope | Forbidden scope | Recommended TIP shape |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A. Production auth / credential lifecycle | Recommended next governed slice | It establishes actor refs, credential refs, tenant bindings, secret lifecycle, and audit identity before durable schema hardens around them. | It requires STOP/RRI on identity provider/store, rotation, revocation, admin authority, and break-glass. | TIP-12/TIP-13 actor model; credential lifecycle RRI; support/admin policy bounds. | Planning or kickoff for production-neutral auth boundary, credential reference model, lifecycle rules, audit identity, and LocalDev compatibility constraints. | No credential store implementation, no secret backend, no OAuth/OIDC/mTLS integration, no production readiness claim unless separately approved. | Planning-only decision TIP first, then kickoff if accepted. |
| B. Durable persistence foundation | Strong second candidate | It unblocks durable audit, recovery, policy catalog, vault metadata, and outbox substrate. | If persistence goes first, schema must not freeze an unratified identity model. | Actor/credential refs, tenant model, policy refs, retention/legal-hold metadata, migration posture. | Planning or kickoff for durable metadata schema boundaries and migration rules after identity assumptions are pinned. | No DB/EF/migrations until kickoff; no raw storage; no retention enforcement; no production database claim. | Planning-only unless production auth decisions are accepted first. |
| C. Another planning-only decision TIP | Acceptable if governance wants more risk reduction | Completion authority, operator/admin workflow, retention/legal hold, or provider trust could be decided before implementation. | Too many decision-only TIPs can delay foundational hardening if production auth and persistence remain unresolved. | Depends on selected topic. | Narrow STOP/RRI decision record with explicit allowlist/denylist for later implementation. | No runtime work or readiness claim. | Planning-only. |

### Dependency Ordering: Production Auth vs Durable Persistence

If production auth / credential lifecycle goes first:

- Credential refs, actor refs, tenant bindings, scope grants, and audit identity can shape future persistence deliberately.
- Durable audit and session schemas can reference stable identity concepts instead of retrofitting LocalDev key assumptions.
- Risk: the slice may drift into credential-store implementation unless the TIP explicitly remains planning/kickoff only or uses an implementation allowlist.

If durable persistence goes first:

- Durable audit, session, policy snapshot, package metadata, and future outbox substrate can start sooner.
- Risk: schema may freeze unratified identity assumptions, such as LocalDev-style API keys, insufficient actor separation, weak support access models, or missing credential refs.
- Constraint: any persistence-first TIP must keep actor/credential fields abstract, migration-safe, and revisable, and must not claim production auth readiness.

TIP-14 recommendation:

```text
Next governed slice: production auth / credential lifecycle planning-only decision TIP.
Fallback: durable persistence foundation planning-only TIP only if it explicitly avoids freezing unratified identity and credential assumptions.
```

## 9. Future Implementation TIP Allowlist and Denylist

Any future implementation TIP derived from TIP-14 must include its own reviewed kickoff and must be narrower than this convergence brief.

### Possible future implementation allowlist

Only after a separate accepted kickoff, a future implementation TIP may allow a narrow subset such as:

- production-neutral actor reference model;
- credential reference value objects with no raw secret storage;
- audit identity fields that avoid secret leakage;
- durable metadata repository interfaces without provider selection;
- migration-safe schema planning artifacts;
- policy snapshot reference modeling;
- webhook delivery id model without delivery implementation;
- retention/legal-hold metadata fields without enforcement;
- tests scoped only to the accepted implementation boundary.

### Mandatory future implementation denylist

Unless explicitly accepted in a later RRI and kickoff, future implementation must not include:

- raw credential or secret storage;
- production identity provider integration;
- OAuth/OIDC/certificate/mTLS implementation;
- DB/EF/migrations/provider selection;
- raw artifact or biometric storage;
- retention enforcement, deletion workflow, or legal hold workflow;
- webhook route, subscription, dispatcher, outbox, retry, delivery ledger, signing, replay cache, or external HTTP delivery;
- evidence package signing, KMS/HSM integration, key rotation, or external verification;
- provider/vendor/device integration or vendor selection;
- Operator/Admin cross-client lookup or raw evidence access;
- production completion authority changes;
- public API/DTO/JSON/status/error behavior changes;
- pilot readiness, production readiness, certification, legal reliance, or external audit reliance claims;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 10. Recommended Next Action

Submit TIP-14 for homeowner/GPT review as a planning-only convergence brief.

If accepted, prepare a separate planning-only decision TIP for:

```text
Production Auth / Credential Lifecycle Boundary
```

That next planning TIP should decide credential reference shape, actor identity, lifecycle governance, and admin/support constraints before any durable persistence implementation freezes schema assumptions.

Do not dispatch implementation from TIP-14.

## 11. Acceptance Criteria

TIP-14 planning is acceptable when:

- It uses "post-S1 / S2 production-readiness debts" terminology and avoids wording that implies S1 itself has a production-readiness scope.
- It covers production auth / credential lifecycle, durable persistence / migrations / recovery, policy catalog / reproducible decisions, vault/raw artifact lifecycle, raw biometric protection, retention / legal hold / deletion, webhook/outbox/retry/delivery ledger, webhook signing / replay protection, evidence package signing / key management, provider/vendor/device trust, Operator/Admin/System support workflow, production completion authority, pilot / production readiness certification, SignFlow integration boundary, specialized evidence endpoints, and fingerprint default enablement.
- It precisely records that TIP-11 resolved LocalDev metadata/data-boundary primitives only and did not resolve durable persistence, raw artifact storage, retention enforcement, deletion, legal hold workflow, or durable policy catalog.
- It precisely records that TIP-13 resolved LocalDev application authorization boundary only and did not resolve production auth, credential lifecycle, credential store, rotation, revocation, admin lifecycle, or secret backend.
- It includes STOP/RRI decisions with decision needed, implementation impact, candidate options, and forbidden assumptions.
- It explicitly frames production completion authority options.
- It compares production auth / credential lifecycle against durable persistence foundation and records dependency ordering risk.
- It preserves SignFlow as an external consumer profile only.
- It includes an explicit allowlist and denylist for any future implementation TIP.
- It does not modify `src/**`, tests, public contracts, runtime behavior, persistence, auth, webhook, vault, crypto, provider integration, or readiness claims.
