# TIP-18 DB / Provider Posture Decision Planning Brief v0.1

**File:** `docs/tips/tip_18_db_provider_posture_decision/tip_18_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning/decision only
**Date:** 2026-06-15
**Baseline:** `7424d550a9e6bd9829f638e9a92e37542468236d`
**Purpose:** Decides the current persistence posture after TIP-17 and before any database provider, EF, migration, schema, infrastructure adapter, LocalDev adapter, package, or durable repository implementation work.

## Changelog

### v0.1 - Initial planning/decision draft

- Opened TIP-18 as a docs-only DB/provider posture decision brief.
- Accepted that production DB/provider selection remains deferred.
- Accepted that EF versus non-EF and migration policy remain deferred decision topics.
- Preserved `IDurableMetadataRepository` as the Application port introduced by TIP-17.
- Preserved the atomic metadata write-set as a future required consistency property, not an implemented durability or transaction claim.
- Recorded backup/recovery as a future decision requirement, not an implemented capability.
- Listed LocalDev adapter work only as a future candidate slice that must be strictly non-production and separately authorized.
- Preserved no runtime implementation, no `src/**`, no `tests/**`, no project/package changes, no schema, no migrations, no adapter, no repository implementation, no readiness claim, and no SignFlow runtime dependency.

## 1. Context / Baseline

TIP-18 follows the accepted S2 planning sequence:

- S1 is closed as LocalDev evidence-ready only.
- TagEkyc remains non-production and non-certified.
- TIP-10 production readiness planning is accepted.
- TIP-11 metadata/data-boundary foundation is closed.
- TIP-12 actor trust, caller scopes, and access-boundary planning is accepted.
- TIP-13 Option A application authorization boundary is implemented and closed.
- TIP-14 S2 debt registry convergence is accepted.
- TIP-15 production auth / credential lifecycle boundary is accepted.
- TIP-16 durable persistence foundation planning is accepted.
- TIP-17 provider-neutral durable metadata repository boundary is implemented and closed.

TIP-17 intentionally stopped at provider-neutral contracts. It added the Application port `IDurableMetadataRepository`, durable metadata records, safe credential reference/value-object boundaries, and architecture tests that keep BusinessConsumer contracts, SignFlow runtime, and provider-specific persistence out of the durable metadata boundary.

TIP-18 decides posture only. It does not dispatch implementation.

## 2. Section 0 Repo Evidence

Accepted read-only dry-run evidence:

```text
Repository root: D:/Task/Remote Signing/TagEkyc
HEAD: 7424d550a9e6bd9829f638e9a92e37542468236d
Latest commit: 7424d55 docs: close TIP-17 durable metadata repository boundary
```

Known dirty files before TIP-18 and outside this scope:

```text
 M .gitignore
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

TIP documentation convention:

- TIP folders use `docs/tips/tip_XX_short_slug/`.
- TIP artifacts use `tip_XX_<artifact>_vA_B.md`.
- TIP-17 closeout exists at `docs/tips/tip_17_provider_neutral_durable_metadata_repository_boundary/tip_17_closeout_v0_1.md` and does not open TIP-18 implementation.

TIP-17 boundary evidence:

- `IDurableMetadataRepository` is located in `src/TagEkyc.Application/Ports/DurableMetadataRepositoryPorts.cs`.
- Durable metadata records cover session metadata, actor credential metadata, audit identity metadata, evidence package metadata, completion authority metadata, and atomic metadata write set.
- Runtime project files currently show only internal project references and no runtime EF, SQL Server, PostgreSQL, SQLite, Dapper, MongoDB, or other DB/provider package selection.
- Existing architecture tests treat EF/provider dependencies as forbidden for the current boundary.
- Existing LocalDev repositories are in-memory development proof state, not durable metadata repository implementation.

No STOP/RRI item was triggered during the dry-run.

## 3. Decision Options Considered

| Option | Description | Benefit | Risk | TIP-18 decision |
| --- | --- | --- | --- | --- |
| A - Select a production DB provider now | Choose SQL Server, PostgreSQL, SQLite, cloud DB, document DB, or another provider. | Could accelerate schema work. | Prematurely freezes transaction, migration, deployment, backup, recovery, and compliance assumptions. | Rejected for v0.1. |
| B - Select EF Core and migrations now | Authorize EF packages, `DbContext`, migrations, and schema ownership. | Gives a familiar implementation path. | Adds runtime/provider decisions before transaction, audit, recovery, and retention posture is accepted. | Rejected for v0.1. |
| C - Implement Infrastructure repository adapter now | Implement `IDurableMetadataRepository` against a selected or local store. | Starts durability work. | Crosses from decision posture into runtime implementation and may imply real durability. | Rejected for v0.1. |
| D - Implement LocalDev durable metadata adapter now | Add an in-memory or local-only adapter for development. | Could exercise the TIP-17 port earlier. | Can be mistaken for durability, recovery, or production persistence unless separately bounded. | Deferred to a later candidate TIP only. |
| E - Keep provider-neutral and decision-only | Preserve TIP-17 Application port, document criteria, and defer irreversible choices. | Keeps implementation blocked until provider, migration, consistency, and recovery decisions are reviewed. | Delays DB work. | Accepted for v0.1. |

## 4. Accepted Posture for v0.1

TIP-18 accepts Option E:

```text
Keep TIP-18 as a docs-only planning/decision brief.
Keep production DB/provider selection deferred.
Keep EF versus non-EF and migration policy deferred.
Do not authorize any repository, adapter, schema, migration, package, or runtime implementation.
```

The current durable metadata boundary remains:

- `IDurableMetadataRepository` is the Application port from TIP-17.
- Domain and Application keep provider-neutral metadata shapes.
- Infrastructure remains implementation-free for this boundary.
- BusinessConsumer contracts remain isolated from durable metadata internals.
- SignFlow remains a consumer profile boundary and must not become a runtime/source/database/package dependency.

Atomic metadata write-set posture:

- `DurableMetadataWriteSet` represents the future consistency property expected when session metadata, audit identity metadata, evidence package metadata, and completion authority metadata need to be written together.
- TIP-18 does not claim that atomic durability, database transactions, rollback, retry, idempotent storage, or recovery are implemented.
- A later implementation TIP must decide transaction boundary, failure behavior, write ordering, audit append guarantees, and test strategy before any adapter is built.

Backup/recovery posture:

- Backup and recovery are mandatory future decision requirements for production durability.
- TIP-18 does not define RPO, RTO, backup storage, restore tests, point-in-time recovery, tenant isolation during restore, audit recovery, or operational ownership.
- No current TagEkyc state should be described as production durable, recoverable, or legally reliable because of TIP-18.

LocalDev adapter posture:

- A LocalDev-only adapter may be considered later as a separate candidate slice.
- It must be explicitly non-production, must not claim durability, must not store raw or hashed secrets, must not promote LocalDev API keys to production credentials, and must not be authorized by TIP-18.

## 5. Hard Non-Goals

TIP-18 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, or dependency changes;
- database provider selection;
- SQL Server, PostgreSQL, SQLite, document DB, cloud DB, or other provider selection as production posture;
- EF, `DbContext`, migrations, schema files, generated SQL, or migration tooling;
- durable repository implementation;
- Infrastructure adapter implementation;
- LocalDev durable metadata adapter implementation;
- production auth implementation;
- credential store, API-key store, secret backend, raw secret storage, hashed secret storage, token storage, private key storage, or certificate lifecycle;
- raw biometric, raw artifact, document image, face image, NFC, liveness, provider payload, or vault storage;
- retention enforcement, legal hold workflow, delete workflow, purge workflow, or vault lifecycle;
- webhook, outbox, retry, delivery ledger, dispatcher, signing, or replay implementation;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- pilot readiness, production readiness, certification readiness, external audit reliance, or legal reliance claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 6. Deferred Debts

TIP-18 leaves these debts explicitly deferred:

- production DB/provider selection;
- EF versus non-EF decision;
- migration policy and ownership;
- schema design and naming;
- durable repository implementation;
- Infrastructure adapter boundary and package layout;
- LocalDev-only adapter decision;
- transaction and audit consistency implementation;
- failure behavior for partial metadata writes;
- idempotency and retry behavior for metadata persistence;
- durable audit retention and immutability posture;
- backup/recovery requirements, RPO/RTO, restore testing, and operational ownership;
- policy catalog durability;
- outbox substrate and delivery transaction relationship;
- production auth and credential store;
- credential reference indexing and uniqueness rules;
- vault/raw artifact lifecycle;
- retention, legal hold, delete, and purge enforcement;
- privileged support lookup and cross-client access posture;
- production completion authority model.

## 7. STOP/RRI Gates

Stop and request review before any later implementation if any of these become necessary:

| Gate | STOP/RRI condition | Why it matters |
| --- | --- | --- |
| Provider selection | A later plan needs SQL Server, PostgreSQL, SQLite, cloud DB, document DB, or another provider. | Provider choice affects transactions, migrations, backup, recovery, deployment, and compliance evidence. |
| EF/migration policy | A later plan needs EF, `DbContext`, migrations, generated SQL, migration packages, or schema tooling. | Migration ownership must be accepted before schema becomes durable contract. |
| Repository implementation | A later plan implements `IDurableMetadataRepository`. | Implementation creates real behavior and must define consistency, failure, tests, and boundary rules. |
| Infrastructure adapter | A later plan touches `src/TagEkyc.Infrastructure/**` for durable metadata persistence. | Adapter dependencies must not leak into Domain, Application, Contracts, BusinessConsumer, or SignFlow. |
| LocalDev adapter | A later plan adds a local-only adapter. | It must remain strictly non-production and must not claim durability, backup, recovery, or production credentials. |
| Atomic write behavior | A later plan claims session, audit, package, and completion authority metadata are written atomically. | Transaction semantics and failure behavior must be real, testable, and provider-compatible. |
| Backup/recovery | A later plan claims recoverability, restore capability, RPO/RTO, or operational durability. | Recovery claims create production and compliance expectations. |
| Credential material | A later plan stores raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material. | TIP-17 permits safe references only. |
| LocalDev key promotion | A later plan treats LocalDev API keys as production credentials. | LocalDev proof material must not become production identity or credential store semantics. |
| Raw artifact/vault data | A later plan stores raw artifacts, biometrics, provider payloads, or vault bytes in application persistence. | TIP-18 only covers metadata posture. |
| BusinessConsumer contract exposure | A later plan exposes durable metadata internals through public BusinessConsumer contracts. | TIP-17 preserved public contract isolation. |
| SignFlow dependency | A later plan references SignFlow runtime, source, database, packages, network, or internal models. | SignFlow remains a consumer profile, not base platform persistence. |
| Readiness claim | A later plan implies pilot, production, certification, legal, or external audit readiness. | TagEkyc remains non-production and non-certified. |

## 8. Validation Command

Recommended validation after docs-only edits:

```text
dotnet test TagEkyc.sln --no-restore
```

For this planning brief, no runtime validation is required unless reviewer policy asks for the full suite. Validate the patch by inspecting only the TIP-18 docs diff and comparing `git status --short` against the accepted pre-TIP dirty baseline. The expected non-TIP dirty baseline is `.gitignore` and `tools/TagEkyc.GDriveSync/**`; those files must remain outside the TIP-18 diff.

## 9. Recommended Next Action

Keep TIP-18 as a planning/decision draft for homeowner/GPT review.

Do not dispatch implementation from TIP-18. If accepted, the next governed slice should be a separate, narrow TIP that chooses exactly one of:

- DB/provider decision criteria refinement only;
- LocalDev-only durable metadata adapter planning, strictly non-production;
- transaction/audit consistency planning;
- backup/recovery requirement planning;
- policy catalog durability planning.

Any future implementation TIP must include an explicit allowlist, explicit denylist, validation expectations, and STOP/RRI treatment for provider, migration, consistency, backup/recovery, credential, raw artifact, public contract, and SignFlow boundaries.
