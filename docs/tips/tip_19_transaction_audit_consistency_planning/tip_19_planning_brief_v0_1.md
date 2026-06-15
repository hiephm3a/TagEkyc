# TIP-19 Transaction / Audit Consistency Planning Brief v0.1

**File:** `docs/tips/tip_19_transaction_audit_consistency_planning/tip_19_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-15
**Baseline:** `59fb7303bd99d2e386fcb2654903e931d70570c2`
**Purpose:** Defines provider-neutral transaction and audit consistency semantics before any database provider, EF, migration, schema, repository implementation, Infrastructure adapter, LocalDev adapter, outbox, webhook, retry, or runtime test work.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-19 as a docs-only transaction/audit consistency planning brief.
- Preserved TIP-18 provider-neutral hold: no production DB/provider selection, no EF/non-EF decision, no migration/schema/package decision, no repository implementation, no Infrastructure adapter, and no LocalDev adapter.
- Treated `DurableMetadataWriteSet` as the future same-boundary atomic metadata unit for session metadata, audit identity metadata, and any included evidence package or completion authority metadata.
- Accepted append-only audit identity posture for future durable audit metadata, with corrections represented as additional corrective audit records instead of mutation.
- Defined failure, idempotency, retry, transaction-boundary, outbox, proof, and future-slice expectations without implementing runtime behavior.
- Preserved no `src/**`, no `tests/**`, no project/package/migration changes, no readiness claim, and no SignFlow runtime/source/database/package/internal-model dependency.

## 1. Context / Baseline

TIP-19 follows the accepted S2 planning sequence:

- S1 is closed as LocalDev evidence-ready only.
- TagEkyc remains non-production and non-certified.
- TIP-17 implemented the provider-neutral durable metadata repository boundary.
- TIP-18 closed as a docs-only DB/provider posture decision with provider-neutral hold.
- TIP-18 preserved `IDurableMetadataRepository` as the Application port and `DurableMetadataWriteSet` as a future consistency expectation only.

TIP-19 is planning-only. It defines future consistency semantics that later implementation slices must satisfy, but it does not authorize persistence code, runtime tests, provider mechanics, DB schema, migrations, packages, LocalDev adapters, or outbox/webhook/retry implementation.

## 2. Section 0 Repo Evidence

Read-only dry-run evidence:

```text
Repository root: D:/Task/Remote Signing/TagEkyc
HEAD: 59fb7303bd99d2e386fcb2654903e931d70570c2
Latest commit: 59fb730 docs: close TIP-18 DB provider posture decision
Latest accepted validation supplied by prompt: dotnet test TagEkyc.sln --no-restore = 103 passed, 0 failed, 0 skipped
```

Known dirty files before TIP-19 and outside this scope:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

Baseline documents and boundary evidence:

- TIP-18 closeout exists at `docs/tips/tip_18_db_provider_posture_decision/tip_18_closeout_v0_1.md` and is the current provider-neutral DB/provider posture baseline.
- TIP-17 closeout exists at `docs/tips/tip_17_provider_neutral_durable_metadata_repository_boundary/tip_17_closeout_v0_1.md`.
- `IDurableMetadataRepository` and `DurableMetadataWriteSet` are located in `src/TagEkyc.Application/Ports/DurableMetadataRepositoryPorts.cs`.
- `DurableMetadataWriteSet` currently contains `DurableSessionMetadata`, `DurableAuditIdentityMetadata`, optional `DurableEvidencePackageMetadata`, and optional `DurableCompletionAuthorityMetadata`.
- Current runtime state remains LocalDev/in-memory and existing repository ports are not durable metadata implementations.
- Current source projects do not select EF, SQL Server, PostgreSQL, SQLite, Dapper, MongoDB, document DB, cloud DB, or any production DB/provider package for the durable metadata boundary.
- Existing architecture tests treat EF/provider dependencies as forbidden for the current boundary.

No runtime implementation is open. No STOP/RRI item was triggered during drafting because the patch is limited to the two allowed docs paths.

## 3. Proposed v0.1 Planning Posture

TIP-19 proposes this posture for v0.1 review:

```text
Define consistency semantics before adapter/provider work.
Keep the semantics provider-neutral and planning-only.
Do not select provider transaction mechanics.
Do not implement repository, adapter, schema, migration, package, outbox, webhook, retry, or runtime tests.
```

The future implementation requirement is semantic, not mechanical: any later adapter/provider must be able to prove that the accepted metadata write-set succeeds as one consistency boundary or fails without accepting a partial business/audit fact as durable truth.

## 4. Atomic Metadata Write-Set Semantics

Future durable metadata persistence must treat these records as the core same-boundary write set when the operation creates or mutates session business state:

| Record | Same-boundary posture | Notes |
| --- | --- | --- |
| `DurableSessionMetadata` | Required in the same boundary. | Represents accepted session lifecycle/business state. |
| `DurableAuditIdentityMetadata` | Required in the same boundary. | Represents actor, principal, credential ref, operation, decision, reason, request, correlation, and timestamp facts for the same operation. |
| `DurableEvidencePackageMetadata` | Same boundary when the operation creates package metadata. | Must not be accepted independently from the related completion/session transition when package metadata is part of finalization. |
| `DurableCompletionAuthorityMetadata` | Same boundary when the operation records completion authority. | Must not be accepted independently from the related completed session/package facts. |

Same-boundary atomic means all records included in one `DurableMetadataWriteSet` become durable together, or none of them is accepted as the durable result of that operation.

Separately durable records are allowed only when the business operation itself is independently meaningful and has its own audit identity record in the same boundary. Examples may include future credential lifecycle metadata, retention/legal marker metadata, privileged support metadata, or capture/evidence append metadata, but each requires a later explicit write-set shape and proof expectation.

TIP-19 does not change the TIP-17 port shape. A later implementation TIP may decide whether individual save/append methods remain low-level primitives, become internal adapter helpers, or require stronger guidance so application services do not bypass same-boundary semantics for operations that need `SaveMetadataWriteSetAsync`.

## 5. Audit Consistency

Future audit identity metadata must be append-only for durable audit purposes. Corrections are allowed only as additional corrective audit records that link to or identify the corrected event by accepted safe reference or correlation identity. Corrections must not mutate, delete, or overwrite the original audit identity record.

Audit orphaning posture:

- Audit metadata must not be orphaned when business metadata write fails for a same-boundary business operation.
- Business metadata must not be accepted when required audit identity metadata write fails for the same operation.
- If a later legal/security requirement needs rejected-attempt audit that intentionally has no business state change, that audit event must be modeled as its own independently meaningful audit write with a distinct operation, decision, reason, request id, and correlation id.

Audit consistency does not claim immutable storage, WORM storage, SIEM export, retention period, legal reliance, or external audit readiness. Those remain later legal/compliance and production-readiness decisions.

## 6. Failure Semantics

Required future failure posture:

| Failure case | Required semantic result | Later proof expectation |
| --- | --- | --- |
| Session metadata succeeds / audit fails | Overall operation fails; session metadata must not be accepted as durable truth. | Prove rollback, non-commit, or provider-neutral reconciliation/quarantine that reports unknown/non-success until repaired; never accepted partial durable truth. |
| Audit succeeds / session metadata fails | Overall operation fails; audit must not stand as accepted business-operation audit unless modeled as an independent denied/failed-attempt audit. | Prove no orphan audit for accepted business operation. |
| Evidence package metadata succeeds / completion authority metadata fails | Overall completion write set fails; package metadata must not be accepted as finalization truth. | Prove package/completion/session consistency for finalization. |
| Completion authority metadata succeeds / evidence package metadata fails | Overall completion write set fails; completion authority metadata must not imply completed package state. | Prove no completed authority fact without package/session completion. |
| Partial write detected after interruption | Operation result is unknown until reconciled; system must not report success from partial evidence. | Prove detection by idempotency identity, write-set version, or accepted provider-neutral equivalent. |
| Retryable provider failure | Caller may retry with the same idempotency identity and receive the prior applied result or a clean retry result. | Prove duplicate suppression and conflict handling. |
| Terminal consistency failure | Operation must STOP/RRI and surface a non-success outcome without claiming accepted business state. | Prove no silent partial acceptance. |

STOP/RRI cases for future implementation:

- The provider cannot give same-boundary semantics or equivalent partial-write detection.
- The adapter cannot distinguish retryable unknown outcomes from terminal conflicts.
- Audit and business facts would need to be written through separate providers without an accepted consistency design.
- Any design would accept business metadata without audit identity metadata, or audit identity metadata as an orphan for a successful business operation.

## 7. Idempotency and Retry Posture

Future durable metadata implementation must define an idempotency identity before adapter/provider work begins.

Candidate idempotency inputs:

- operation name;
- `VerificationSessionId` when present;
- `EvidencePackageId` when package metadata is part of the operation;
- `ClientApplicationId`;
- `RequestId`;
- `CorrelationId`;
- actor identity metadata: `PrincipalId`, `ActorCategory`, `CredentialRef`, `CredentialType`, and `ScopeGrantSetId`;
- expected prior session state or write-set version, if later accepted;
- policy snapshot/reference identity, if needed for reproducible decision writes.

Replay behavior:

- Same idempotency identity with the same write-set facts should return the prior applied result or an equivalent already-applied result.
- Same idempotency identity with conflicting facts must be treated as conflict, not a second write.
- Different idempotency identity that attempts to repeat a terminal lifecycle transition must follow accepted state-transition conflict rules.

Duplicate suppression must apply to audit identity metadata as well as business metadata. Retrying an operation must not produce duplicate audit records unless the retry itself is explicitly modeled as a separate audit-worthy attempt.

Deferred items:

- exact idempotency key format;
- unique index or storage mechanism;
- conflict error contract;
- retry window;
- clock-skew handling;
- provider-specific transaction isolation;
- generated version/concurrency token shape;
- replay of outbox or webhook delivery.

## 8. Transaction Boundary Placement

Future transaction semantics should be placed as follows:

| Boundary | Responsibility | TIP-19 posture |
| --- | --- | --- |
| Application service boundary | Decides the business operation, authorization result, audit identity, expected state transition, and metadata write-set content. | Must call a same-boundary durable metadata operation for operations that require audit/business consistency. |
| Repository port boundary | Exposes provider-neutral persistence operations such as `SaveMetadataWriteSetAsync`. | Owns semantic contract, not provider mechanics. |
| Infrastructure adapter boundary | Implements the port using a later accepted provider. | Deferred; no adapter authorized. |
| Provider transaction boundary | Provides commit/rollback/isolation or accepted equivalent. | Deferred; no DB/provider selected. |

TIP-19 does not decide whether a future implementation uses database transactions, optimistic concurrency, event store append, document transactional batch, queue-plus-store choreography, or another provider-specific mechanism. It only states the consistency contract a future mechanism must satisfy.

## 9. Outbox Relationship

Outbox is deferred in TIP-19.

Preferred v0.1 posture:

- Do not include outbox records in the required TIP-19 same-boundary metadata write set.
- Do not authorize webhook, outbox, retry, delivery ledger, dispatcher, signing, or replay implementation.
- Require a later outbox/webhook planning TIP to decide whether delivery-intent records must share the same transaction boundary as session/package/completion metadata.

If a later slice decides that delivery intent must be atomic with metadata, that slice must explicitly define delivery-intent metadata, duplicate suppression, replay rules, signing/replay boundaries, and proof expectations without weakening the business/audit write-set semantics defined here.

## 10. Test / Proof Expectations

No tests are added by TIP-19.

Future implementation proof should include, at minimum:

- metadata write-set success writes all included records once;
- audit failure prevents business metadata acceptance;
- business metadata failure prevents accepted audit orphaning;
- package/completion finalization cannot partially commit;
- same idempotency identity with same facts is duplicate-suppressed;
- same idempotency identity with conflicting facts fails as conflict;
- unknown/interrupted write path is detectable and does not report false success;
- provider-specific transaction/isolation behavior satisfies the provider-neutral semantic contract;
- forbidden data remains absent from durable metadata;
- SignFlow runtime/source/database/package/internal models remain out of the boundary.

These are proof expectations for a later reviewed implementation slice only.

## 11. Relation to Later Slices

TIP-19 coordinates with, but does not authorize, these later slices:

| Later slice | Relationship to TIP-19 | Boundary preserved |
| --- | --- | --- |
| LocalDev-only adapter planning | May use TIP-19 semantics to decide whether a strictly non-production adapter is useful. | Deferred; no LocalDev adapter now. |
| Backup/recovery requirements | Must know which write-set facts require restore consistency. | Deferred; no RPO/RTO/restore claim now. |
| DB/provider decision criteria | Must evaluate provider ability to satisfy same-boundary write-set semantics and idempotency. | Deferred; no provider selected now. |
| EF/migration/schema | Must model write-set atomicity, append-only audit, duplicate suppression, and conflicts if later accepted. | Deferred; no EF/schema/migration now. |
| Durable repository implementation | Must implement the semantic contract accepted here. | Deferred; no repository implementation now. |
| Policy catalog durability | May become part of reproducible decision metadata or idempotency conflict rules. | Deferred; no catalog implementation now. |
| Production auth/credential store | Provides stable principal and credential references used in audit identity. | Deferred; no auth/credential store now. |
| Retention/legal hold/delete/purge | May require separately atomic marker plus audit write sets. | Deferred; no enforcement now. |
| Webhook/outbox/retry | May later include delivery-intent atomicity. | Deferred; no outbox/webhook/retry now. |

## 12. Hard Non-Goals

TIP-19 does not authorize:

- runtime implementation;
- changes under `src/**`;
- changes under `tests/**`;
- project, solution, package, dependency, or migration changes;
- database provider selection;
- SQL Server, PostgreSQL, SQLite, cloud DB, document DB, event store, queue, or other provider selection;
- EF, non-EF decision, `DbContext`, migrations, schema files, generated SQL, or migration tooling;
- durable repository implementation;
- Infrastructure adapter implementation;
- LocalDev durable metadata adapter implementation;
- outbox, webhook, retry, delivery ledger, dispatcher, signing, replay, or delivery implementation;
- production auth implementation;
- credential store, API-key store, secret backend, raw secret storage, hashed secret storage, token storage, private key storage, or certificate lifecycle;
- raw artifact, biometric, provider payload, vault object, or evidence package raw storage;
- retention enforcement, legal hold workflow, delete workflow, purge workflow, or vault lifecycle;
- public API/DTO/JSON/status/error behavior changes;
- provider/vendor integration;
- production readiness, pilot readiness, certification readiness, external audit reliance, legal reliance, backup/recovery capability, real durability, transaction implementation, or durable audit-store implementation claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## 13. STOP/RRI Gates

Stop and request review before any later implementation if any of these become necessary:

| Gate | STOP/RRI condition | Why it matters |
| --- | --- | --- |
| Provider mechanics | A plan needs SQL Server, PostgreSQL, SQLite, cloud DB, document DB, event store, queue, or another provider. | Provider choice determines actual transaction, isolation, idempotency, recovery, and schema mechanics. |
| EF/migration/schema | A plan needs EF, non-EF, migrations, generated SQL, schema, indexes, or package changes. | Persistence shape becomes durable contract and must be reviewed separately. |
| Repository implementation | A plan implements `IDurableMetadataRepository` or changes its methods. | Implementation creates runtime behavior and consistency obligations. |
| Idempotency identity | A plan cannot define a stable operation identity, duplicate suppression rule, or conflict rule before implementation. | Idempotency is required to distinguish replay, retry, unknown outcome, conflict, and duplicate audit/business facts. |
| Infrastructure adapter | A plan touches Infrastructure for durable metadata persistence. | Adapter dependencies must not leak into Domain, Application, Contracts, BusinessConsumer, or SignFlow. |
| LocalDev adapter | A plan adds a local-only durable metadata adapter. | It must remain strictly non-production and must not claim durability, backup, recovery, or production credentials. |
| Audit orphaning | A plan permits business metadata without audit or orphan audit for successful business operations. | This violates the accepted same-boundary consistency semantics. |
| Outbox inclusion | A plan includes delivery intent in the same transaction boundary. | Delivery/retry/signing/replay semantics need their own reviewed design. |
| Backup/recovery | A plan claims recoverability, restore capability, RPO/RTO, or operational durability. | Recovery claims create production and compliance expectations. |
| Credential material | A plan stores raw secrets, hashed secrets, tokens, private keys, API keys, or reconstructable credential material. | TIP-17 and TIP-18 permit safe references only. |
| Raw artifact/vault data | A plan stores raw artifacts, biometrics, provider payloads, or vault bytes in application persistence. | TIP-19 covers metadata consistency only. |
| Public contracts | A plan changes public API/DTO/JSON/status/error behavior. | TIP-19 is internal planning and must not change external behavior. |
| SignFlow dependency | A plan references SignFlow runtime, source, database, packages, network, or internal models. | SignFlow remains a consumer profile, not base platform persistence. |
| Readiness claim | A plan implies pilot, production, certification, legal, external audit, real durability, or recoverability readiness. | TagEkyc remains non-production and non-certified. |

## 14. Validation

Recommended validation after docs-only edits:

```text
git diff -- docs/tips/README.md
git diff --no-index -- /dev/null docs/tips/tip_19_transaction_audit_consistency_planning/tip_19_planning_brief_v0_1.md
git status --short
```

No runtime validation is required for this planning-only draft unless reviewer policy asks for the full suite. Do not stage or commit TIP-19 during drafting.

## 15. Recommended Next Action

Keep TIP-19 as a planning draft for homeowner/GPT review.

If accepted, the next governed slice should remain narrow and choose exactly one topic, such as DB/provider criteria, LocalDev-only adapter planning, backup/recovery requirements, outbox relationship planning, or durable repository implementation kickoff. Any implementation kickoff must include an explicit allowlist, explicit denylist, provider-neutral consistency proof expectations, and STOP/RRI handling for provider, migration, idempotency, audit, backup/recovery, credential, raw artifact, public contract, and SignFlow boundaries.
