# TIP-17 Provider-Neutral Durable Metadata Repository Boundary Kickoff v0.1

**File:** `docs/tips/tip_17_provider_neutral_durable_metadata_repository_boundary/tip_17_kickoff_v0_1.md`
**Version:** 0.1
**Status:** Draft - kickoff only, implementation not dispatched
**Date:** 2026-06-14
**Baseline:** `1af460192f849d9e6fcb3e178342008982b7fc52`
**Purpose:** Defines a narrow, provider-neutral durable metadata repository boundary candidate for later review without implementing repositories, adapters, DB/EF/migrations, production auth, credential storage, raw artifact storage, webhook/outbox delivery, retention enforcement, or readiness claims.

## Changelog

### v0.1 - Initial kickoff draft

- Opened TIP-17 as a docs-only kickoff draft for provider-neutral durable metadata repository boundaries.
- Preserved TIP-16 as the immediate planning baseline and TIP-15 provider-neutral identity concepts as required repository metadata inputs.
- Defined a possible later implementation shape, explicitly not dispatched by this document.
- Limited the possible later implementation allowlist to provider-neutral repository/port contracts for durable metadata, safe metadata reference models if explicitly allowed, audit identity shape without public contract changes, forbidden-data leakage tests only if later accepted, and an optional LocalDev-only in-memory adapter only if later review accepts it.
- Recorded mandatory denylist items: DB/EF/migrations, provider selection, raw or hashed secret storage, credential store, production auth, vault/raw artifact storage, webhook/outbox delivery, retention/legal enforcement, public API/DTO changes, readiness claims, and SignFlow dependency.
- Added STOP/RRI gates for source/test touch policy, repository abstraction location, audit shape, transaction consistency, policy catalog reference shape, completion authority metadata, retention/legal markers, and outbox substrate deferral.
- Preserved no implementation dispatch, no `src/**`, no `tests/**`, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations/packages, no durable repository implementation, no production auth implementation, no credential store, no raw secret or hashed secret storage, no secret backend, no raw artifact or biometric storage, no vault lifecycle, no retention enforcement, no deletion/purge/legal hold workflow, no webhook/outbox/retry/delivery implementation, no crypto/signing/replay, no provider/vendor integration, no pilot/production/certification readiness claim, and no SignFlow runtime/source/database/network/package/internal-model dependency.

## 1. Baseline and Current HEAD

TIP-17 follows this accepted planning and closeout state:

- S1 is closed as LocalDev evidence-ready only, non-production, and non-certified.
- TIP-11 Option B is closed as a domain/application metadata boundary only.
- TIP-13 Option A is closed as current LocalDev application authorization boundary hardening only.
- TIP-14 S2 Debt Registry Convergence is accepted and committed.
- TIP-15 Production Auth / Credential Lifecycle Boundary is accepted and committed.
- TIP-16 Durable Persistence Foundation Planning is accepted and committed.
- No runtime implementation is currently open.

Required source documents:

- `docs/tips/tip_16_durable_persistence_foundation/tip_16_planning_brief_v0_1.md`
- `docs/tips/tip_15_production_auth_credential_lifecycle_boundary/tip_15_planning_brief_v0_1.md`
- `docs/tips/tip_14_post_tip_13_s2_debt_registry_convergence/tip_14_planning_brief_v0_1.md`
- `docs/tips/tip_13_application_authorization_boundary_foundation/tip_13_closeout_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/tips/README.md`

Baseline check before drafting:

```text
HEAD: 1af460192f849d9e6fcb3e178342008982b7fc52
Worktree before draft: clean
Latest validation supplied by user: dotnet test TagEkyc.sln --no-restore = 81 passed, 0 failed, 0 skipped
```

## 2. Purpose and Non-Goals

TIP-17 is a kickoff draft only. It does not dispatch implementation.

Purpose:

- define the provider-neutral durable metadata repository boundary that a later implementation kickoff may use;
- decide what belongs behind application-facing durable metadata ports versus domain model concepts versus infrastructure adapters;
- preserve TIP-15 identity concepts and TIP-16 durable metadata concepts without freezing a DB provider, EF posture, migration strategy, credential backend, vault backend, outbox substrate, or production auth provider;
- give homeowner/GPT reviewers a narrow packet before any `src/**` or `tests/**` changes are made.

Non-goals:

- no implementation dispatch;
- no `src/**` changes;
- no `tests/**` changes;
- no public API/DTO/JSON/status/error behavior changes;
- no DB provider selection;
- no EF, DbContext, migrations, schema files, or provider packages;
- no durable repository implementation;
- no production auth implementation;
- no credential store, secret backend, raw secret storage, hashed secret storage, API-key store, token store, OAuth/OIDC, mTLS, certificate, or service-principal implementation;
- no raw artifact, biometric, image, NFC, liveness, document, provider payload, vault object, or evidence package raw storage;
- no retention enforcement, deletion workflow, purge workflow, or legal hold workflow;
- no webhook route, subscription, outbox, dispatcher, retry, delivery ledger, signing, replay, or delivery implementation;
- no crypto/signing/replay implementation;
- no provider/vendor integration or selection;
- no pilot readiness, production readiness, certification, external audit reliance, or legal reliance claim;
- no SignFlow runtime/source/database/network/package/internal-model dependency.

## 3. Implementation Candidate Shape - Not Dispatched

The candidate shape for later review is:

```text
Application layer owns provider-neutral durable metadata ports.
Domain layer owns provider-neutral value concepts and invariants only when explicitly allowed.
Infrastructure layer may later adapt a chosen provider to those ports, but provider choice and adapter implementation are not part of this draft.
```

This shape is not implementation authorization.

Possible later structure, only if accepted in a separate implementation dispatch:

- application-facing durable metadata repository contracts for session, audit, policy reference, package metadata, retention/legal marker, and completion authority metadata;
- provider-neutral reference/value types where they prevent leakage of provider details or raw secrets;
- no direct domain dependency on infrastructure persistence technology;
- no infrastructure adapter until explicitly allowed;
- no DB/EF/migration package until a later RRI accepts provider and migration posture.

## 4. Very Narrow Possible Later Implementation Allowlist

A later accepted implementation dispatch may allow only a narrow subset such as:

- provider-neutral repository/port contracts for durable metadata only;
- safe metadata reference models if explicitly allowed by the later kickoff;
- audit identity shape only if it does not change public API, DTO, JSON, status, or error behavior;
- forbidden-data leakage tests only if the later kickoff explicitly allows tests;
- optional LocalDev-only in-memory adapter only if later homeowner/GPT review accepts it.

The later dispatch must define exact files, assemblies, and tests before work begins. This TIP-17 draft does not authorize touching `src/**` or `tests/**`.

## 5. Mandatory Denylist

TIP-17 and any direct follow-on implementation kickoff must deny the following unless a separate explicit RRI changes the boundary:

- DB/EF/migrations;
- provider selection;
- provider packages;
- raw secret or hashed secret storage;
- credential store;
- secret backend;
- production auth;
- OAuth/OIDC, mTLS, certificate, token validation, service-principal, or IdP integration;
- vault/raw artifact storage;
- raw artifact, raw biometric, raw provider payload, or raw evidence package storage;
- webhook/outbox delivery;
- retry, dispatcher, delivery ledger, signing, replay, or external HTTP delivery;
- retention/legal enforcement;
- deletion workflow, purge workflow, or legal hold workflow;
- public API/DTO/JSON/status/error behavior changes;
- pilot readiness, production readiness, certification, legal reliance, or external audit reliance claims;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 6. Durable Metadata Boundary

Durable metadata repository contracts may later need to represent these concepts without storing raw secrets, raw artifacts, provider internals, or public DTO data:

| Metadata concept | Boundary |
| --- | --- |
| Session metadata | Stable session id, lifecycle state, timestamps, owner context, policy reference, package linkage, and safe diagnostics only. |
| `clientApplicationId` / tenant placeholder | Client ownership is required; tenant remains placeholder/STOP/RRI until accepted. |
| `principalId` | Stable actor/service identity independent of credential value and rotation. |
| `actorCategory` | BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, System/InternalService as provider-neutral metadata. |
| `credentialRef` | Safe reference only; never a raw API key, bearer token, client secret, private key, password-equivalent, hashed secret, or reconstructable credential. |
| `credentialType` / status | Metadata only for provider-neutral classification and lifecycle state; no credential validation or store. |
| `scopeGrantSetId` | Versioned/reviewable scope grant reference, not a production auth implementation. |
| `policySnapshotId` / future policy catalog reference | Safe reference for reproducible decision metadata; LocalDev `LOCALDEV-S1-POLICY-V1` is not a production catalog. |
| Audit identity | Actor, principal, credential ref/type/status, client/tenant context, operation, decision, reason, request/correlation id, and timestamp without raw secrets or payloads. |
| Evidence package metadata | Package id/ref/hash/classification, manifest ref/classification, result summaries, and completion linkage; no raw package internals or raw artifact storage. |
| Completion authority actor metadata | Requester/finalizer actor refs and authority metadata; production authority remains STOP/RRI. |
| Retention/legal-hold/delete markers | Retention class, legal hold status, delete requested/eligible marker, purge block marker; markers only, no enforcement. |

## 7. Repository Boundary Decision

Homeowner/GPT review must decide the repository abstraction posture before implementation.

Candidate options:

| Option | Shape | Risk |
| --- | --- | --- |
| Application port | Application services depend on provider-neutral repository interfaces; infrastructure later implements them. | Preferred candidate because it keeps provider details out of domain and application logic. |
| Domain interface | Domain declares repository interfaces directly. | Higher risk of persistence concerns leaking into domain; only acceptable if it matches an explicit local pattern and remains provider-neutral. |
| Infrastructure adapter only | Infrastructure owns concrete persistence and application calls it through existing services. | Not acceptable as the first boundary if it exposes provider details or bypasses application authorization/audit boundaries. |

Allowed dependency direction for any later implementation:

```text
Domain -> no Infrastructure dependency
Application -> Domain and provider-neutral application ports only
Infrastructure -> Application/Domain contracts and provider adapters
Api/Adapters -> Application contracts only
Tests -> accepted target assemblies only
```

Provider leakage rules:

- no provider-specific types in domain/application contracts;
- no EF attributes, DbContext types, migration types, SQL provider names, cloud provider names, or provider connection details in domain/application;
- no provider/vendor identifiers except safe opaque references explicitly accepted as metadata;
- no SignFlow internal identifiers or package internals in repository contracts;
- no public DTOs reused as persistence models.

## 8. Transaction and Audit Boundary

Future durable metadata persistence must later decide what facts are persisted atomically. TIP-17 does not implement transactions.

Candidate atomic fact groups for later review:

- session creation with client ownership, principal identity, actor category, credential reference metadata, scope grant reference, policy reference, and audit event;
- capture metadata append with session ownership, capture actor identity, artifact metadata reference, lifecycle transition, and audit event;
- trusted evidence result metadata append with TrustedAdapter identity, policy/evidence refs, decision metadata, and audit event;
- completion request/finalization metadata with requester/finalizer identity, policy snapshot/reference, package metadata refs, final decision, and audit event;
- retention/legal marker mutation with actor/authority, reason, marker value, timestamp, and audit event;
- credential reference lifecycle metadata, if later allowed, with principal identity, credential ref/status transition, actor/authority, reason, and audit event.

STOP/RRI items that remain unresolved:

- whether audit is append-only and immutable;
- whether audit and session metadata must share one transaction boundary;
- whether outbox substrate participates in the same transaction;
- whether policy catalog snapshots are copied onto sessions or referenced by id;
- whether legal hold/delete markers can mutate after finalization and who authorizes them;
- whether completion authority is BusinessConsumer, System/InternalService, Operator, or split request/finalize model.

No outbox implementation is allowed by TIP-17.

## 9. STOP/RRI Gates Before Implementation

Implementation must not start until homeowner/GPT review answers these gates:

| STOP/RRI item | Decision needed before implementation |
| --- | --- |
| Whether repository contracts may touch `src/**` | Decide if a later implementation kickoff may edit source at all, and which projects/files are in scope. |
| Whether tests may be added | Decide if forbidden-data leakage tests, architecture tests, or contract regression tests are allowed. |
| Whether LocalDev adapter is allowed | Decide if an in-memory LocalDev-only adapter is permitted or if contracts only are safer. |
| Repository abstraction location | Decide application port vs domain interface vs another accepted local pattern. |
| Audit shape | Decide required audit identity fields, redaction rules, event categories, and whether audit is append-only. |
| Transaction consistency | Decide atomic fact groups and consistency between session metadata, audit, package metadata, credential refs, policy refs, and legal markers. |
| Policy catalog reference shape | Decide policy snapshot id versus future durable catalog reference without treating LocalDev policy as production catalog. |
| Completion authority metadata | Decide requester/finalizer metadata shape without deciding production authority prematurely. |
| Retention/legal markers | Decide marker names and mutability while preserving no enforcement. |
| Outbox substrate deferred | Confirm outbox tables/interfaces/dispatchers remain out of scope for the first repository boundary implementation. |

If any gate remains unresolved, keep TIP-17 kickoff-only and prepare a narrower decision TIP instead of implementation.

## 10. Review Checklist for GPT/Homeowner

Reviewers should verify:

- TIP-17 remains docs-only and dispatches no implementation.
- The possible later implementation is limited to provider-neutral durable metadata repository boundaries.
- Repository boundary choice prevents provider details from leaking into domain/application.
- The metadata list covers session, client/tenant placeholder, principal, actor category, credential ref/type/status, scope grant, policy reference, audit identity, evidence package metadata, completion authority metadata, and retention/legal markers.
- `credentialRef` is safe reference only and cannot contain raw or hashed secrets.
- Raw artifact, biometric, provider payload, and vault storage are prohibited.
- DB/EF/migrations/provider packages and provider selection remain prohibited.
- Public API/DTO/JSON/status/error behavior changes remain prohibited.
- Production auth, credential store, secret backend, webhook/outbox delivery, retention/legal enforcement, crypto/signing/replay, provider/vendor integration, readiness claims, and SignFlow dependencies remain prohibited.
- STOP/RRI gates are explicit enough to block accidental implementation.
- The recommendation does not imply pilot, production, certification, audit, or legal reliance readiness.

## 11. Recommendation

TIP-17 should remain kickoff-only pending homeowner/GPT review.

Recommended next step:

```text
Review TIP-17. If accepted, prepare a separate explicit implementation dispatch limited to provider-neutral durable metadata repository boundaries and forbidden-data leakage tests, or keep TIP-17 kickoff-only if any STOP/RRI gate remains unresolved.
```

The draft itself does not dispatch implementation.

## 12. Acceptance Criteria

TIP-17 kickoff draft is acceptable when:

- It records the current HEAD and baseline planning state.
- It states purpose and non-goals.
- It describes implementation candidate shape while marking it not dispatched.
- It includes a very narrow possible later implementation allowlist.
- It includes the mandatory denylist.
- It defines durable metadata boundary concepts.
- It frames the repository boundary decision and allowed dependency direction.
- It defines transaction and audit questions without implementing outbox or persistence.
- It records STOP/RRI gates before implementation.
- It includes a homeowner/GPT review checklist.
- It recommends kickoff-only status pending review or a later separate extremely narrow implementation dispatch after acceptance.
- It changes only docs and does not touch `src/**`, `tests/**`, public contracts, runtime behavior, DB/EF/migrations, durable repositories, production auth, credential store, secret backend, raw artifact/vault storage, webhook/outbox/retry, crypto/signing/replay, provider integration, readiness claims, or SignFlow dependency posture.
