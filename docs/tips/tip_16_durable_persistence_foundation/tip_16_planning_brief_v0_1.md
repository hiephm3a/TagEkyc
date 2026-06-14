# TIP-16 Durable Persistence Foundation Planning / Kickoff Brief v0.1

**File:** `docs/tips/tip_16_durable_persistence_foundation/tip_16_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning/kickoff only
**Date:** 2026-06-14
**Baseline:** `d42fedcb340c47106912f12fe953626c9897d4ca`
**Purpose:** Defines the durable persistence foundation boundary for TagEkyc using the provider-neutral identity and credential concepts from TIP-15, without implementing production auth, credential storage, raw secret storage, provider integration, durable storage, or production readiness.

## Changelog

### v0.1 - Initial planning/kickoff draft

- Opened TIP-16 as a docs-only durable persistence foundation planning/kickoff brief.
- Preserved TIP-15 provider-neutral identity concepts: `principalId`, `actorCategory`, `credentialRef`, `credentialType`, `credentialStatus`, tenant/client binding, scope grants, lifecycle timestamps, and audit identity.
- Separated current LocalDev in-memory state from future durable metadata persistence, raw artifact/vault storage, and credential/secret storage.
- Defined durable metadata concepts to preserve for sessions, client ownership, actor identity, credential references, policies, lifecycle markers, audit, retention/legal-hold/delete markers, evidence package metadata, and completion authority.
- Explicitly recorded that `credentialRef` is a safe reference only and that LocalDev API keys must not become production credential records.
- Defined audit durability requirements for authenticated actions, denied authorization, credential lifecycle, privileged access, completion decisions, and evidence/package lifecycle.
- Defined allowed durable metadata and forbidden raw secrets, raw biometrics/artifacts, provider internals, SignFlow internals, production auth, credential store, vault storage, webhook/outbox delivery, and readiness claims.
- Added STOP/RRI topics for DB/provider selection, EF/migration policy, repository boundary, transactional consistency, audit retention, legal hold/delete enforcement, policy catalog durability, credentialRef persistence, System/InternalService completion authority, cross-client support lookup, outbox substrate, raw artifact/vault boundary, backup/recovery, and production readiness.
- Recommended TIP-16 remain planning-only now and prepare a later extremely narrow implementation kickoff only after homeowner/GPT review accepts the persistence posture and STOP/RRI guardrails.
- Preserved no `src/**`, no tests, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations, no durable repository implementation, no production auth, no credential store, no raw secret storage, no secret backend, no production IdP/OAuth/OIDC/mTLS/certificate implementation, no raw artifact or biometric storage, no vault lifecycle, no retention enforcement, no deletion or legal hold workflow, no webhook/outbox/retry/delivery implementation, no crypto/signing/replay implementation, no provider/vendor integration, no pilot/production/certification readiness claim, and no SignFlow runtime/source/database/network/package/internal-model dependency.

## 1. Purpose

TIP-16 is planning/kickoff only. It does not authorize runtime implementation, source changes, test changes, public contract changes, database work, EF work, migrations, durable repositories, credential storage, production authentication, secret backend integration, raw artifact storage, vault lifecycle, webhook/outbox delivery, crypto/signing, provider integration, or readiness claims.

The purpose is to define the durable persistence foundation boundary before TagEkyc introduces any database, repository, migration, recovery, durable audit, policy catalog, vault reference, credential reference, outbox substrate, or production metadata schema.

TIP-16 follows accepted planning and implementation state:

- S1 is closed as LocalDev evidence-ready only, non-production, and non-certified.
- TIP-10 production readiness planning is accepted.
- TIP-11 metadata/data-boundary foundation is closed for LocalDev/non-production primitives only.
- TIP-12 actor trust, caller scopes, and access-boundary planning is accepted.
- TIP-13 Option A application authorization boundary is implemented and closed using current LocalDev auth only.
- TIP-14 S2 debt registry convergence is accepted.
- TIP-15 production auth / credential lifecycle boundary is accepted as the immediate planning input for persistence identity concepts.

TIP-16 should let reviewers decide whether a later narrow implementation kickoff is safe. This document itself opens no implementation.

## 2. Baseline Inputs

- `docs/tips/tip_15_production_auth_credential_lifecycle_boundary/tip_15_planning_brief_v0_1.md`
- `docs/tips/tip_14_post_tip_13_s2_debt_registry_convergence/tip_14_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md`
- `docs/tips/tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md`
- `docs/tips/tip_13_application_authorization_boundary_foundation/tip_13_closeout_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/tips/README.md`

Baseline check before drafting:

```text
HEAD: d42fedcb340c47106912f12fe953626c9897d4ca
Worktree: clean
Latest validation supplied by user: dotnet test TagEkyc.sln --no-restore = 81 passed, 0 failed, 0 skipped
```

## 3. Persistence Purpose and Non-Goals

Durable persistence is needed later to preserve facts that cannot safely remain only in current LocalDev in-memory/application state:

- session identity and lifecycle state;
- tenant and `clientApplicationId` ownership;
- provider-neutral actor/principal identity;
- safe credential references and credential lifecycle status;
- scope grant and policy snapshot references;
- audit identity and security-sensitive decisions;
- evidence package metadata, not raw evidence payloads;
- retention, legal-hold, delete, and purge eligibility markers;
- completion authority actor and decision provenance;
- recovery, replay, and support diagnostics metadata within accepted boundaries.

TIP-16 non-goals:

- no database provider selection;
- no EF, DbContext, migrations, schema files, or provider packages;
- no durable repository implementation;
- no production authentication implementation;
- no credential store, secret backend, raw secret storage, hashed secret storage, or API-key store;
- no raw artifact, raw biometric, image, NFC, liveness, document, or vault object storage;
- no retention enforcement, deletion workflow, legal hold workflow, or purge workflow;
- no webhook route, subscription, outbox dispatcher, retry, delivery ledger, signing, replay, or delivery implementation;
- no provider/vendor integration;
- no public API/DTO/JSON/status/error behavior changes;
- no pilot readiness, production readiness, certification, legal reliance, or external audit reliance claim.

## 4. Boundary Between State Areas

TIP-16 separates four state areas so a future implementation does not blend LocalDev proof state, metadata persistence, secret storage, and raw artifact storage.

| Area | Purpose | Allowed in TIP-16 | Forbidden in TIP-16 |
| --- | --- | --- | --- |
| Current LocalDev in-memory/current state | Proves actor/category/scope/ownership shape for non-production S1/S2 behavior. | Document compatibility needs and migration risks. | Do not persist LocalDev key strings, treat them as production credential ids, or claim production trust. |
| Future durable metadata persistence | Stores session, tenant/client, actor, credential reference, policy reference, lifecycle, audit, package metadata, and retention/legal markers. | Planning concepts and review questions only. | No DB/EF/migration/repository implementation in this brief. |
| Future raw artifact/vault storage | Stores or references raw capture artifacts, biometrics, documents, images, NFC payloads, liveness payloads, or provider evidence payloads. | Boundary definition only: application persistence may hold safe refs/metadata later if accepted. | No raw bytes, vault lifecycle, provider payload storage, deletion workflow, or legal hold workflow. |
| Future credential/secret storage | Stores secrets, hashed API keys, OAuth client secrets, private keys, certificates, secret manager ids, or credential material. | Safe references only: `credentialRef`, `credentialType`, `credentialStatus`. | No raw secrets, hashed secrets, credential store, secret backend, IdP integration, OAuth/OIDC, mTLS, certificate, or key lifecycle implementation. |

Persistence must keep these areas independently reviewable. A future durable metadata schema may reference a credential or vault object only by safe reference and accepted lifecycle metadata.

## 5. Durable Metadata Concepts to Preserve

Future durable metadata should be able to represent the following concepts without choosing a production DB, credential store, provider, vault, or identity provider:

| Concept | Purpose | Persistence boundary |
| --- | --- | --- |
| `sessionId` | Stable verification session identity. | Durable metadata candidate; no public contract change implied. |
| `clientApplicationId` | Application owner boundary for session, package, policy, and projection data. | Must come from trusted identity/registration, not body input alone. |
| `tenantId` | Optional future legal/organization owner above client applications. | STOP/RRI until tenant model is accepted. |
| `principalId` | Stable actor/service identity independent of credential rotation. | Must not equal raw API key, token, private key, or LocalDev key value. |
| `actorCategory` | BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, System/InternalService. | Required for future authorization and audit. |
| `credentialRef` | Safe reference to authentication artifact or provider-owned credential. | Safe reference only; raw secret storage is forbidden. |
| `credentialType` | Provider-neutral classification such as managed API key, OAuth/OIDC client, certificate, mTLS, or service principal. | Planning classification only; provider selection remains STOP/RRI. |
| `credentialStatus` | Pending, active, suspended, revoked, expired, rotated/replaced, or future accepted lifecycle state. | Enables revocation/rotation audit without deleting historical actor identity. |
| `scopeGrantSetId` | Versioned or reviewable scope grant reference. | Must preserve scope decisions without freezing LocalDev policy source. |
| `policySnapshotId` / policy catalog reference | Reproducible decision and policy identity. | Durable policy catalog remains STOP/RRI; LocalDev `LOCALDEV-S1-POLICY-V1` is not a production catalog. |
| lifecycle timestamps | Created, updated, activated, revoked, rotated, suspended, expired, completed, packaged, held, deleted, purge-eligible times as applicable. | Durable facts later; no workflow/enforcement in TIP-16. |
| audit identity | Actor/principal/credential/client/session/reason/request/correlation identity for security-sensitive events. | Must avoid raw secrets and sensitive payloads. |
| retention/legal-hold/delete markers | Retention class, legal hold status, deletion eligibility, purge block reason, delete requested/eligible markers. | Metadata only; no retention enforcement, deletion, legal hold, or purge workflow. |
| evidence package metadata | Package id, package hash/ref, manifest ref/classification, policy ref, evidence result summaries, completion linkage. | Metadata only; no raw package internals or raw artifact storage. |
| completion authority actor | Actor or service principal that requested and/or finalized completion. | Production authority remains STOP/RRI; current BusinessConsumer completion remains LocalDev-only. |

## 6. CredentialRef and LocalDev Credential Boundary

`credentialRef` is a safe reference only. It may later identify a key id, credential id, OAuth client id, certificate id, service-principal id, provider credential id, secret version id, or similar non-secret handle after the credential posture is accepted.

`credentialRef` must not contain:

- raw API keys;
- bearer tokens;
- OAuth client secrets;
- certificate private keys;
- mTLS private key material;
- password-equivalent strings;
- raw secret manager values;
- unhashed or reconstructable credential material.

LocalDev API keys must not become production credential records. LocalDev key strings, names, and policy-source entries are development proof material only. A future migration may map LocalDev-shaped test contexts to safe development placeholders, but it must not migrate LocalDev key material into production credential storage or durable production audit as secret identity.

## 7. Provider Posture Recommendation

TIP-16 should not select a DB provider, EF posture, credential provider, identity provider, vault provider, cloud service, or migration strategy.

Recommended planning posture:

```text
Use provider-neutral repository and schema concepts for review, but do not implement repositories, EF, migrations, or DB provider code in TIP-16.

Allow a later implementation kickoff to consider only a local development metadata adapter or repository boundary if homeowner/GPT review accepts the exact allowlist and confirms no production readiness claim.
```

Persistence provider decisions remain STOP/RRI:

- DB/provider selection;
- EF versus non-EF persistence;
- migration policy and ownership;
- local development adapter shape;
- repository abstraction boundary;
- transaction/consistency model;
- backup/recovery requirements.

## 8. Audit Durability Requirements

Future durable audit must preserve security-sensitive history across credential rotation, revocation, support events, completion decisions, package lifecycle, and legal review.

Minimum audit durability areas:

| Audit area | Required durable facts | Boundary |
| --- | --- | --- |
| Authenticated action audit | actor category, `principalId`, `credentialRef`, `credentialType`, tenant/client context, session/resource id, operation, request/correlation id, timestamp. | No raw secrets, raw payloads, or mutable display names as sole identity. |
| Denied authorization audit | authenticated actor identity, denied operation, target resource/client, decision reason, request/correlation id, timestamp. | Required especially for cross-client, privileged, and sensitive denied attempts. |
| Credential lifecycle audit | principal, credential reference, lifecycle action, status transition, actor/authority, reason, timestamp. | No credential material; revocation must not delete audit history. |
| Privileged access audit | Operator/Admin/System actor, reason code, approval reference if any, minimized target, output classification, timestamp. | Cross-client lookup remains forbidden until accepted. |
| Completion decision audit | requester actor, finalizer actor, policy ref, package/evidence metadata refs, authorization decision, timestamp. | Production completion authority remains STOP/RRI. |
| Evidence/package lifecycle audit | evidence metadata append, trusted result append, package creation, package hash/ref, retention/legal markers, lifecycle transitions. | No raw artifacts, biometrics, provider payloads, internal vault bytes, or public contract changes. |

Audit durability must support incident review and legal/compliance review later, but TIP-16 does not select audit retention classes, legal retention periods, immutable storage, WORM storage, SIEM export, or reporting obligations.

## 9. What Persistence May Store vs Must Not Store

Future durable metadata may store, after accepted implementation kickoff:

- stable session identifiers and lifecycle states;
- tenant/client ownership metadata;
- provider-neutral `principalId` and `actorCategory`;
- safe `credentialRef`, `credentialType`, and `credentialStatus`;
- scope grant set references;
- policy snapshot or policy catalog references;
- metadata lifecycle timestamps;
- audit identity and decision facts;
- retention class, legal-hold status, deletion eligibility, and purge block markers;
- evidence package metadata, hashes, refs, and classification;
- completion request/finalization actor refs;
- request/correlation ids and safe operational diagnostics.

Future durable metadata must not store:

- raw API keys, tokens, passwords, client secrets, private keys, or credential material;
- raw biometrics, raw artifacts, document images, face images, NFC payloads, liveness payloads, or provider raw payloads;
- provider/vendor internals beyond accepted safe references and metadata;
- SignFlow internal models, database identifiers, runtime state, package internals, or network-coupled records;
- raw webhook signing secrets or replay keys;
- legal hold, deletion, purge, vault, outbox, or credential lifecycle enforcement/workflow state unless separately accepted.

## 10. Future Migration Readiness

Any future schema must be migration-safe and must not freeze current LocalDev assumptions.

Migration readiness requirements:

- schema must not treat LocalDev API key strings as production credentials;
- schema must not assume credential value equals actor identity;
- schema must preserve provider-neutral `principalId`, `credentialRef`, `credentialType`, and `credentialStatus`;
- schema must allow credential rotation and revocation without changing `principalId` or deleting audit history;
- schema must allow multiple actor categories and future separate Operator/Admin/System identities;
- schema must allow tenant/client binding to become stricter without data rewrite that weakens audit;
- schema must allow a durable policy catalog later without treating LocalDev policy ids as production catalog records;
- schema must allow retention/legal-hold/delete metadata without implementing enforcement prematurely;
- schema must allow raw artifact/vault references later without storing raw payloads in application persistence;
- schema must not assume BusinessConsumer is production completion authority.

## 11. Relation to Future Slices

TIP-16 should coordinate with, but not implement, these future slices:

| Future slice | Relationship to TIP-16 | TIP-16 boundary |
| --- | --- | --- |
| Production auth implementation | Needs stable identity, credential refs, lifecycle status, audit identity. | No auth implementation, no token validation, no IdP, no credential store. |
| Credential store / secret backend | May own secrets and lifecycle material referenced by `credentialRef`. | Metadata may reference only safe handles; no secret storage. |
| Vault/raw artifact lifecycle | May own raw artifacts and biometrics referenced by safe vault/artifact refs. | No raw storage, vault lifecycle, retention enforcement, deletion, or legal hold workflow. |
| Webhook/outbox | May need durable session/package/delivery metadata and transaction boundaries. | Outbox substrate remains STOP/RRI; no webhook delivery/retry/signing/replay. |
| Policy catalog | May own durable policy versions and reproducible decision snapshots. | Only policy reference concepts; no catalog implementation. |
| Production completion authority | May decide BusinessConsumer, System/InternalService, Operator, or split authority. | Persistable metadata concept only; no authority change. |
| Privileged support workflow | May define cross-client lookup, support reason codes, approval gates, and minimized reads. | Cross-client lookup remains forbidden until accepted. |

## 12. Candidate Later Implementation Allowlist

TIP-16 recommends no implementation now.

After homeowner/GPT review, a later narrow implementation kickoff may be prepared only if it has an explicit allowlist such as:

- provider-neutral repository contracts or planning artifacts for durable metadata only;
- local development in-memory or file-free adapter only if needed for tests and explicitly accepted;
- domain/application metadata value objects only if the later reviewed kickoff explicitly allows source changes;
- audit identity shape extensions that avoid raw secrets and public contract changes;
- tests limited to repository-boundary behavior and forbidden-data leakage checks;
- no provider selection and no production readiness claim.

Any later implementation kickoff must still forbid:

- production auth implementation;
- credential store or secret backend;
- raw secret storage or hashed secret storage;
- raw artifact/vault storage;
- DB/EF/migrations unless separately accepted by explicit RRI;
- webhook/outbox delivery, retry, signing, replay, or dispatcher;
- retention enforcement, deletion workflow, legal hold workflow, or purge workflow;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- SignFlow runtime/source/database/network/package/internal-model dependency;
- pilot/production/certification/legal reliance claims.

## 13. STOP/RRI Table

| STOP/RRI item | Decision needed | Why it shapes implementation | Candidate options | Forbidden assumptions |
| --- | --- | --- | --- | --- |
| DB/provider selection | Decide whether SQL Server, PostgreSQL, SQLite, cloud DB, document DB, or no provider is selected. | Provider affects schema, migrations, transactions, backup, recovery, and deployment. | Defer; local dev only; explicit provider RRI later. | Do not select a provider in TIP-16. |
| EF/migration policy | Decide if EF Core, hand migrations, generated SQL, or another migration tool is allowed. | Migration ownership affects schema evolution and review gates. | Defer; EF later; SQL migrations later; no migrations. | Do not add DbContext, migrations, or packages now. |
| Repository boundary | Decide whether durable persistence enters through interfaces, application services, infrastructure adapters, or direct domain persistence. | Shapes testability and prevents provider leakage. | Provider-neutral repository abstraction; application ports; no repository until implementation kickoff. | Do not implement durable repositories in TIP-16. |
| Transactional consistency model | Decide aggregate boundaries and consistency between session, audit, package metadata, policy refs, credential refs, and outbox substrate. | Prevents partial security facts and misleading audit. | Single transaction; eventual audit; append-only audit; deferred outbox. | Do not imply delivery/outbox or production consistency guarantees. |
| Audit retention class | Decide retention class, immutability, export, review, and legal handling for audit records. | Audit durability can create legal/security obligations. | Metadata markers; legal-defined classes later; immutable audit later. | Do not choose legal retention periods or claim audit reliance. |
| Legal hold/delete enforcement boundary | Decide whether enforcement belongs in app, vault, DB, legal workflow, or external system. | Affects deletion, purge, raw storage, audit, and support operations. | Metadata-only markers; external workflow; later legal RRI. | Do not implement enforcement, deletion, hold, or purge. |
| Policy catalog durability | Decide if policies become versioned durable records, snapshots, or external catalog refs. | Required for reproducible decisions and audit. | Durable catalog later; immutable snapshots; provider-neutral refs. | Do not treat LocalDev policy id as production catalog. |
| `credentialRef` persistence without secret storage | Decide exact allowed shape, uniqueness, redaction, indexing, and provider mapping. | Prevents secret leakage and supports rotation/revocation. | Opaque safe ref; provider key id; secret-version handle; certificate id. | Do not store raw or reconstructable secrets. |
| System/InternalService completion authority persistence | Decide if production completion finalizer is BusinessConsumer, System/InternalService, Operator, or split model. | Determines completion audit, package lifecycle, and service-principal metadata. | BusinessConsumer LocalDev only; System finalizer; Operator review; split request/finalize. | Do not assume current S1 BusinessConsumer completion is production authority. |
| Cross-client support lookup persistence | Decide whether support lookup is forbidden, reason-coded, approval-gated, or tenant-scoped. | Shapes indexes, audit, privacy, and privileged reads. | Forbidden; tenant-scoped; reason-coded; approval-gated. | Do not add broad cross-client indexes or support access by default. |
| Outbox substrate included or deferred | Decide whether future persistence foundation includes outbox tables/interfaces or defers them. | Outbox affects transactions and delivery reliability. | Defer; metadata-only delivery refs; later outbox TIP. | Do not implement webhook/outbox/retry/delivery in TIP-16. |
| Raw artifact/vault boundary | Decide what safe vault/artifact refs may be persisted and who owns raw storage. | Prevents raw payload leakage into application DB. | Metadata-only refs; separate vault service; provider-owned refs; no raw storage. | Do not store raw artifacts, biometrics, or vault bytes. |
| Backup/recovery requirements | Decide RPO/RTO, backup scope, restore testing, tenant isolation, and audit recovery. | Persistence implementation cannot be production-safe without recovery posture. | Defer; local dev only; production RRI later. | Do not claim recoverability or production readiness. |
| Production readiness claim | Decide if any later environment can claim pilot/production/certification readiness. | Legal/compliance exposure. | Engineering-only; no-reliance pilot later; production track later. | TIP-16 explicitly forbids readiness claims. |

## 14. Hard Boundary / Non-Goal Table

| Boundary | Invariant |
| --- | --- |
| TIP-16 scope | Docs-only planning/kickoff draft. It opens no implementation. |
| Runtime/source | No runtime implementation and no `src/**` changes. |
| Tests | No tests are added or changed. |
| Public contracts | No public API, DTO, JSON, status, or error behavior changes. |
| DB/EF/migrations | No DB provider, EF, DbContext, migration, package, repository, adapter, backup, or recovery implementation. |
| Production auth | No production auth provider, credential validation, OAuth/OIDC, mTLS, certificate, service principal, token validation, or IdP integration. |
| Credential store | No credential store, API-key store, hashed secret store, raw secret storage, secret backend, vault secret, key generation, rotation, revocation, or expiry implementation. |
| Webhook/outbox | No webhook route, subscription, outbox, retry, delivery ledger, signing, replay, dispatcher, or external delivery implementation. |
| Vault/raw artifacts | No raw artifact storage, biometric storage, vault lifecycle implementation, retention enforcement, deletion, purge, or legal hold workflow. |
| Crypto/signing | No HMAC, JWS, KMS, HSM, evidence package signing, webhook signing, replay protection, or key rotation implementation. |
| Providers/vendors | No provider, vendor, engine, cloud service, certificate authority, legal/compliance provider, or paid service selection/integration. |
| Readiness claims | No pilot readiness, production readiness, certified eKYC, external audit reliance, or legal reliance claim. |
| SignFlow | No SignFlow runtime/source/database/network/package/internal-model dependency. SignFlow remains an external consumer profile only. |

## 15. Recommendation

TIP-16 should remain planning-only for this draft.

Recommended next step after homeowner/GPT review:

```text
If TIP-16 is accepted, prepare a separate narrow implementation kickoff for provider-neutral durable metadata repository boundaries only if the STOP/RRI review accepts the repository posture, transaction/audit boundary, and explicit denylist.
```

The later implementation kickoff, if prepared, should not include production auth, credential store, raw secret storage, raw artifact/vault storage, webhook/outbox delivery, retention/legal enforcement, provider/vendor integration, public contract changes, or readiness claims.

If reviewer findings leave DB/provider, audit retention, credentialRef persistence, completion authority, support lookup, or vault boundary unresolved, keep TIP-16 as planning-only and route the unresolved topics into narrower decision TIPs.

## 16. Acceptance Criteria

TIP-16 planning/kickoff draft is acceptable when:

- It defines durable persistence purpose and non-goals.
- It separates LocalDev current state, future durable metadata persistence, future raw artifact/vault storage, and future credential/secret storage.
- It preserves provider-neutral identity and credential concepts from TIP-15.
- It explicitly states that `credentialRef` is a safe reference only and not raw secret storage.
- It explicitly states that LocalDev API keys must not become production credential records.
- It frames DB/provider, EF/migration, repository, local adapter, and migration strategy as STOP/RRI or later kickoff topics.
- It defines audit durability requirements for authenticated actions, denied authorization, credential lifecycle, privileged access, completion decisions, and evidence/package lifecycle.
- It defines allowed durable metadata and forbidden raw secrets, raw biometrics/artifacts, provider internals, and SignFlow internals.
- It defines migration readiness without freezing LocalDev auth assumptions.
- It relates durable persistence to future production auth, credential store, vault, webhook/outbox, policy catalog, completion authority, and privileged support slices.
- It includes mandatory STOP/RRI topics for homeowner/GPT review.
- It recommends planning-only status now and a later extremely narrow implementation kickoff only after review.
- It does not modify `src/**`, tests, public contracts, runtime behavior, DB/EF/migrations, durable repositories, production auth, credential store, secret backend, raw artifact/vault storage, webhook/outbox/retry, crypto/signing/replay, provider integration, readiness claims, or SignFlow dependency posture.

## 17. Recommended Review Questions

Homeowner/GPT review should focus on:

1. Is the boundary between metadata persistence, credential/secret storage, and raw artifact/vault storage explicit enough?
2. Are TIP-15 provider-neutral identity fields sufficient before a schema/repository kickoff?
3. Should a later implementation kickoff include only repository interfaces, or also a LocalDev adapter?
4. Should outbox substrate be part of the first persistence foundation, or deferred to a webhook/outbox TIP?
5. Which audit records require durable retention first, and what remains legal/compliance STOP/RRI?
6. How should System/InternalService completion authority be represented without deciding production finalization yet?
7. Is cross-client support lookup fully deferred, or should metadata indexes avoid making it impossible later?

Do not dispatch implementation from TIP-16.
