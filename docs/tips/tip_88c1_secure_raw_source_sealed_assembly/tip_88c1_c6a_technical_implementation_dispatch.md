# TIP-88C1-C6A — Technical Implementation Dispatch

**Status:** CANDIDATE v0.6 — BOUNDED ACL REVIEW REQUIRED — IMPLEMENTATION NOT AUTHORIZED  
**Version:** 0.6  
**Date:** 2026-09-04  
**Risk tier:** High-risk  
**Owner:** Homeowner + Contractor/Drafter  
**Planning basis:** C6A Planning Brief v0.7.1  
**Planning SHA-256:** 5CB8D4B2307D4316AC43A1E1A5CF2C55FC1C9B5D521242A2FC099C74B9C60B60  
**Repository baseline:** 0a2d605bc0b1174825803e1982105d680718500c  
**Implementation authority:** **NOT GRANTED**

## 1. Objective and frozen scope

This dispatch designs the smallest implementation for four planning gates:

1. C6A-SOURCE-GRANT-DERIVATION-01;
2. C6A-C6B-INGRESS-ADMISSION-01;
3. C6A-C3-DELIVERY-SOURCE-VALIDITY-01;
4. C6A-PROFILE-01.

RB-12 is the only current executable defect. The other three gates establish
production derivation, integration ordering and readiness contracts for a
production source path that is not yet implemented.

For the current `EncryptedExportPacket` production profile, the Homeowner
architecture amendment defers a separately enforced package-custody deadline.
PackageExpiresAt is therefore not a current RB-12 prerequisite. The historical
requirement remains valid architecture and may be reactivated only by a future
explicit amendment.

The implementation must preserve:

- policy → grant → permit as the only access-authorization engine;
- AuthoritySnapshot as an independent necessary-but-insufficient source barrier;
- 88B2 consent lifecycle and 88A/88B policy/grant ownership;
- C1 committed replay and phase barriers;
- C2 exact custody convergence after committed seal;
- C3 state-only reconciliation;
- metadata-only C6A behavior and separation from C6B/C6C.

## 2. Explicit non-goals

Do not add:

- external controller/provider/trust/source-shape machinery;
- a generic AuthoritySnapshot management API, actor, category or scope;
- production exposure of snapshot Withdraw/Revoke functions;
- a second policy, grant, permit, consent or audit system;
- a recipient-management semantic dependency;
- physical purge automation or a purge-completeness claim;
- a Raw BIO reader, provider SDK, queue, webhook or real-source test;
- patient data, deployment or activation.

## 3. Decision summary

| Gate | Frozen implementation direction | Result required |
| --- | --- | --- |
| Source Grant derivation | derive server-owned snapshot facts at the R1 production admission boundary; no caller-authored authority fields | deterministic derivation contract and durable binding |
| C6B ingress order | C6B must complete current admission and successful R1 reservation before exposing the first body byte to R2 | zero-byte negative proof |
| C3 RB-12 | perform full current access/source revalidation inside each begin-stream transaction before Streaming CAS | new/retry disclosure fails closed |
| Production profile | extend existing authority readiness with one closed non-Fixture production profile | exact three old errors preserved |

## 4. Source Grant derivation contract

### 4.1 Linearization point

The production AuthoritySnapshot Grant is derived and appended by the
server-owned admission orchestrator before it calls
begin_raw_export_source_ingress_claim. The resulting server-owned SnapshotId is
then supplied to begin and becomes part of its immutable ingress identity.

This order is mandatory because the landed begin function requires a non-empty
p_authority_snapshot_id, includes it in IngressIdentityFingerprint and persists
it NOT NULL on the claim. The landed complete function does not bind a new
snapshot; it freshly resolves authority and compares the current snapshot with
the already-frozen claim SnapshotId.

The future C6B adapter supplies source observations only. It cannot supply
ApprovedPurpose, stable scope, policy/consent references, validity, expiry,
actor, revision or snapshot identity.

The R1 admission service derives those values from durable current facts:

| Derived field | Durable owner |
| --- | --- |
| client/session/capture/raw class | authenticated ingress context + capture acceptance |
| approved purpose | bound effective policy purpose |
| stable data scope/controller identity | accepted capture/source registration facts |
| policy ID/version and retention rules | current catalog-approved policy |
| consent policy ID/version | current effective 88B2 consent |
| valid-from/evaluated time | one post-lock database clock |
| valid-until | minimum applicable finite authoritative bound, otherwise null only where existing schema permits |
| absolute source expiry | minimum ratified source/retention horizon; never local receipt plus an invented age |
| reuse/extension | FreshAuthorityRequired / Forbidden |
| actor | authenticated server-side principal propagated through existing actor plumbing |
| IDs/revision | server-generated/deterministic identity plus append-only lifecycle rules |

### 4.2 Atomicity and replay

Required order:

~~~text
authenticate production ingress actor
  -> enter one owner-only atomic admission function
  -> normalize identity inputs exactly as landed begin
  -> compute the same alias_lock and exact_lock before any snapshot append
  -> first_lock := LEAST(alias_lock, exact_lock)
  -> second_lock := GREATEST(alias_lock, exact_lock)
  -> acquire first_lock and then second_lock with the landed bounded
     pg_try_advisory_* timeout semantics
  -> only after both locks are held, inspect alias/claim state
  -> exact-existing claim: recover its frozen SnapshotId and execute the
     existing begin replay path without appending a Grant
  -> new claim only: lock/read capture acceptance and applicable
     policy/grant/consent facts
  -> one database clock
  -> derive and append canonical Granted snapshot
  -> call begin with that resulting server-owned SnapshotId in the same
     database transaction
  -> begin freezes SnapshotId into ingress identity and claim
  -> commit snapshot append and begin together, or roll both back
  -> complete freshly revalidates the same SnapshotId
  -> complete R1 reservation
  -> only then permit C6B body progression to R2
~~~

This pins permitted topology class A: **ATOMIC COMPOSITION**. The Builder may
not substitute a third topology. Same ingress identity and same derived facts
must converge to the same durable admission. Same identity with changed
derived semantics is a conflict. No generic management-operation table is
authorized.

### 4.3 Exact retry and crash rule

Do not defer snapshot binding to complete and do not make AuthoritySnapshotId
nullable. Doing either would change begin's fingerprint, exact-artifact replay,
schema and readiness-pinned function contract.

The landed `raw_export_append_authority_snapshot` is not replay-idempotent: it
increments revision, generates a random SnapshotId and unconditionally appends
Granted. It must never be called before the atomic wrapper has determined that
the ingress identity is new.

The wrapper must reuse the exact begin identity normalization, both lock keys,
global numeric lock order, timeout budget and existing-claim comparison; it
must not create a competing identity algorithm. In particular it must never
hold alias_lock first merely to discover an existing claim. The landed begin
paths use `LEAST(alias_lock, exact_lock)` then
`GREATEST(alias_lock, exact_lock)`; reversing that order when
`exact_lock < alias_lock` would create a lock inversion with concurrent begin.
The exact lock is computable before SnapshotId because its inputs are the
client/producer/session/capture/revision/raw-class identity.
If an exact claim already exists, its persisted SnapshotId is authoritative for
the retry. If the claim does not exist, snapshot append and begin execute in
one transaction. An exception, cancellation or induced fault between them
rolls both back. A response loss after commit re-enters the existing-claim path
and returns the same SnapshotId/claim identity without another append.

Complete remains the fresh-current-authority check. A new mutable staging
surface, caller ID field, nullable claim binding, weaker fingerprint or actor
impersonation is forbidden.

`C6A-SOURCE-GRANT-REPLAY-01 = CLOSED AT BOUNDED DISPATCH REVIEW`.

`C6A-SOURCE-GRANT-LOCK-ORDER-01 = CLOSED AT BOUNDED DISPATCH REVIEW`.

### 4.4 Authorized composition surface

No landed function composes `raw_export_append_authority_snapshot` with
`begin_raw_export_source_ingress_claim`. The reviewed implementation surface is
one new owner-only SQL function named:

~~~text
tagekyc.raw_export_begin_production_source_ingress_with_authority
~~~

It is authorized as new SQL content inside the already-allowlisted C6A EF
migration path:

~~~text
src/TagEkyc.Infrastructure/Persistence/Migrations/
  <generated>_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/
  <generated>_Tip88C1C6AProductionAuthorityDeliveryBarrier.Designer.cs
~~~

This is not authority for another feature path. The function MUST be
`SECURITY DEFINER`, owned by `tagekyc_raw_export_deployer`, use
`SET search_path=pg_catalog`, be revoked from PUBLIC and `tagekyc_runtime`, and
be granted EXECUTE only to the existing
`tagekyc_raw_export_claim_broker` role.

No other runtime, broker or application role may receive direct EXECUTE under
this dispatch. The broker remains the transaction owner. Any requirement to
expose this function directly to `tagekyc_runtime` or another role is
STOP/RRI.

`C6A-SOURCE-GRANT-COMPOSITION-ACL-01 = OPEN — BOUNDED ROLE-PIN CORRECTION`
until this exact ACL delta receives clean bounded review.

If atomic composition requires a new table, new role/login, caller-owned
transaction boundary, or a change to the ratified C1-B2-CORE broker transaction
boundary, HARD STOP and request separate authority.

## 5. C6B ingress admission-order contract

C6B must implement this ordering:

~~~text
source metadata available
  -> server-derived source authority + current consent/retention admission
  -> successful R1 reservation/admission committed
  -> obtain bounded body/source stream
  -> first byte enters R2
~~~

Forbidden:

- opening or prefetching the body before admission;
- buffering a first chunk while admission is pending;
- passing a lazy stream whose constructor/provider already consumed bytes;
- retrying R2 from a stale admission after a current authority loss;
- adding duplicate consent/snapshot resolution inside R2 as a workaround.

C6A supplies the contract and proof seam. Actual production adapter I/O remains
C6B scope.

## 6. RB-12 C3 delivery barrier design

### 6.1 Barrier placement

The barrier belongs in the same database transaction as
raw_export_begin_recipient_package_delivery_stream, after the exact key,
package and delivery rows are locked and before the Authorized/Interrupted →
Streaming CAS.

Do not place the authoritative check only:

- in the API/Application layer;
- in the object reader after Streaming commit;
- at delivery creation only;
- in the state-only reconciler;
- in C1/C2 replay/finalization.

Every fresh stream and byte-zero retry must pass the barrier. Historical status
reads and exact idempotent delivery creation replay remain non-mutating and do
not reauthorize.

### 6.2 Required lineage

The transaction resolves:

~~~text
DeliveryId
  -> recipient package delivery
  -> finalized C2 preparation
  -> exact AssemblyId / JobId / AttemptId
  -> immutable B4 job identity and classes
  -> each C1 job source binding
  -> current policy/grant/requirements and original Permit
  -> current subject consent per class
  -> current AuthoritySnapshot per source
  -> source/retention/job/package/delivery deadlines
~~~

No request parameter supplies upstream authority identity.

### 6.3 Exact current barrier set

Before Streaming CAS, require:

1. current actor/API key remains authenticated and holds the existing
   package-download scope;
2. current actor PrincipalId equals the original authorization-decision/job
   PrincipalId;
3. current actor ClientApplicationId equals the frozen decision/job
   ClientApplicationId;
4. package and delivery remain bound to the exact
   RecipientClientApplicationId, and that recipient client equals the current
   actor client wherever the ratified direct-recipient contract requires it;
5. session remains Completed and owned by the frozen decision/job client;
6. Grant remains effective;
7. Policy remains catalog-approved/export-active;
8. bound rule set remains current;
9. every required fulfillment remains current/unexpired;
10. original Permit matches job identity/classes/recipient and now is before
   PermitExpiresAt;
11. current Consent is Effective for every frozen class, purpose, session,
   recipient and policy version;
12. current AuthoritySnapshot matches each bound source scope and is effective;
13. now is before AbsoluteSourceExpiresAtUtc and
    EffectivePlaintextRetentionExpiresAtUtc for every source;
14. now is before JobExpiresAt and current-profile upstream authorization
    ceiling PermitExpiresAt;
15. frozen recipient key remains active/exact/in-window;
16. package remains Finalized and exact.

Use one post-lock database clock. A failed barrier appends no StreamingStarted
event, performs no provider read and returns no content.

For the current `EncryptedExportPacket` profile, the upstream authorization
ceiling is `PermitExpiresAt`. B4 schema version 1 additionally enforces
`JobExpiresAt = PermitExpiresAt`, so the two-field minimum equals
`PermitExpiresAt` and is not an independent second clock.

`PermitExpiresAt` is the persisted B3 `DecisionExpiresAtUtc`, derived as the
minimum of all applicable finite bounds:

- `AuthorizationEvaluatedAtUtc + PolicyPermitTtlSeconds`;
- current Consent `ValidUntilUtc`, when finite; and
- earliest applicable Fulfillment `ValidUntilUtc`, when finite.

It is not a bare `permit-issued-at + PermitTtlSeconds` value. These frozen
finite inputs do not replace fresh C3 revalidation of consent, fulfillment,
policy, grant or source authority.

The Homeowner amendment defers PackageExpiresAt only for this current profile.
Do not add it, treat it as null, replace it, or claim the historical concept was
invalid. `AuthorizationExpiresAtUtc`, recipient-key validity and source
retention remain separate necessary barriers and are not substitutes.

### 6.4 Reuse strategy

Reuse the semantics and proof logic of:

- EfRawExportControlPlaneRepository eligibility resolution;
- EfRawExportJobRepository ResolveAuthorityAsync;
- raw_export_resolve_subject_consent_for_authorization;
- raw_export_resolve_current_authority_for_source;
- C1 source-binding lineage and deadline comparisons;
- existing C3 key/package/delivery lock and CAS order.

Do not call raw_export_read_job_binding_inputs by impersonating the original job
principal: that function requires current actor = decision principal, whereas
C3 actor is the recipient. Do not weaken that guard.

The implementation may add one owner-only SQL helper used only from the C3
begin-stream SECURITY DEFINER function if needed for atomic reuse. It must:

- have no PUBLIC or runtime direct EXECUTE;
- accept only durable delivery identity plus the post-lock clock;
- derive all upstream identities from durable rows;
- return a closed eligible/ineligible cause, never authority payload;
- preserve existing C3 function owner/search_path/ACL conventions.

This helper is conditional, not pre-approved until the design spike proves
existing functions cannot be composed atomically.

### 6.5 Outcomes and disclosure

Map any current access/source failure to existing non-disclosing Ineligible
unless independent review approves a new stable aggregate token. Do not reveal
which source, class, consent, grant or policy failed.

Precedence remains:

~~~text
nonexistent/not-owned
  -> terminal/current live-stream state
  -> delivery authorization expiry
  -> current upstream access/source barrier
  -> recipient key/package exactness
  -> Streaming CAS
~~~

The final ordering between upstream barrier and key/package exactness must
follow the established lock order and avoid an existence oracle; tests must
freeze the chosen order.

## 7. Production profile/readiness

Extend RawExportAuthoritySnapshotProfileState and its validator with exact
profile literal Production. Do not accept arbitrary non-Fixture strings.

The profile proves:

- closed registration for the server-derived R1 authority path;
- R1-to-R2 admission-order capability;
- C3 delivery barrier function/body/ACL readiness;
- expected migration/schema identity;
- absence of Fixture registration/fallback in Production.

Preserve exact existing failures:

~~~text
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE
~~~

Readiness performs no subject lookup, snapshot append, source read or delivery.

## 8. Transaction and race requirements

| Race | Required winner |
| --- | --- |
| policy/grant/consent/snapshot changes before C3 barrier locks | change wins; stream denied |
| change waits behind held shared/row/advisory barrier locks | admitted stream linearizes first; change applies afterward |
| delivery retry after authority loss | retry re-enters barrier and is denied |
| authority loss after committed C1 seal | exact C2 finalization still converges; C3 denied |
| R1 authority loss before first C6B byte | no byte reaches R2 |
| exact committed replay | returns persisted result without creating a new admission |

No database/advisory lock remains held during provider/object I/O or HTTP copy.

## 9. Minimal affected-surface allowlist

This is a review ceiling, not implementation authority.

### Existing paths expected

- src/TagEkyc.Infrastructure/Persistence/RawExportAuthoritySnapshotReadinessValidator.cs
- src/TagEkyc.Api/Program.cs
- src/TagEkyc.Api/ReadinessEndpoint.cs only if stable mapping changes
- src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryRepository.cs
  only if result parsing or function invocation changes
- src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryReadinessValidator.cs
- tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs
- tests/TagEkyc.IntegrationTests/Tip88C1B2AuthoritySnapshotTests.cs
- applicable existing C1/C2 regression tests, assertions only where required
- tests/TagEkyc.ArchTests/Tip88C1C3RecipientPackageDeliveryArchTests.cs
- docs/deployment/hospital_trial/postgres_migration_runbook.md
- C6A planning/dispatch/as-built lineage

### Conditional new path

- `src/TagEkyc.Infrastructure/Persistence/Migrations/<generated>_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/<generated>_Tip88C1C6AProductionAuthorityDeliveryBarrier.Designer.cs`

The repository workflow generates the timestamp. These two files may contain
the new owner-only composition function named in section 4.4 plus the C3
barrier/profile ACL delta; this is expressly authorized dispatch content within
the existing migration allowlist, not authority for a new table or role.

This conditional path does not authorize adding PackageExpiresAt to the C2
package model for the current profile.

### Not allowlisted

- Application/public DTO/endpoint changes;
- C1 or C2 runtime source changes;
- C5 recipient-management changes;
- new entity/table/role/login/package/framework;
- C6B adapter implementation;
- snapshot Withdraw/Revoke exposure;
- configuration values/secrets;
- deployment/activation.

If implementation needs a non-allowlisted path, STOP/RRI before editing it.

## 10. Proof plan

| ID | Proof obligation | Mutation that must make RED |
| --- | --- | --- |
| C6A01 | exact planning baseline and migration topology | wrong function/body/ACL hash |
| C6A02 | server-derived snapshot fields; caller cannot assert authority | accept changed purpose/scope/expiry/snapshot ID |
| C6A03 | same R1 identity/facts converge; changed facts conflict | duplicate Grant/reservation on retry |
| C6A04 | successful R1 commit precedes first R2 byte | stream reads one byte before admission |
| C6A05 | C3 valid current 16-condition set admits one stream | omit durable source binding |
| C6A06 | revoked Grant blocks new and retry stream | retain only frozen Grant |
| C6A07 | suspended/revoked Policy or stale rules/fulfillment blocks | check catalog closure only |
| C6A08 | expired Permit/Job current-profile upstream ceiling blocks | use C3 local 30-minute expiry or raw policy TTL alone |
| C6A09 | withdrawn/expired Consent blocks per class | check only one class |
| C6A10 | terminal/expired AuthoritySnapshot blocks per source | treat frozen snapshot ID as sufficient |
| C6A11 | absolute/retention horizon blocks | omit one deadline |
| C6A12 | failure yields zero provider read/response byte/Streaming event | move check after CAS/provider open |
| C6A13 | state-only reconciliation does not invoke authority engine | add subject resolver to reconciler |
| C6A14 | committed C1 replay and C2 convergence survive authority loss | abort/recreate package after committed seal |
| C6A15 | locks/time close revoke race without I/O lock leakage | use transaction-start/pre-lock clock |
| C6A16 | profile is closed and preserves three exact failures | accept unknown/Fixture in Production |
| C6A17 | no recipient-management semantic coupling | grant recipient scope/table/role authority |
| C6A18 | Down/reapply, model snapshot and runtime ACL are exact | PUBLIC/helper EXECUTE or orphan overload |
| C6A19 | same recipient client with a different principal cannot stream | compare recipient client only |
| C6A20 | package recipient with actor client different from frozen decision/job client cannot stream | omit decision/job client equality |
| C6A21 | fault after snapshot append but before begin commit rolls both back; exact retry yields one snapshot/claim identity | commit append separately or append again on retry |
| C6A22 | concurrent wrapper/begin with `exact_lock < alias_lock` preserves identical global lock order | acquire alias first, deadlock/timeout by inversion, or duplicate snapshot/claim identity |
| C6A23 | composition function EXECUTE is available only to `tagekyc_raw_export_claim_broker` | GRANT EXECUTE to PUBLIC, `tagekyc_runtime` or another runtime/broker/application role |

Proof uses durable read-back, function ACL/body inspection, real PostgreSQL
transactions and the existing object-provider fixture only with synthetic
encrypted content. No patient data or real provider operation.

## 11. Amendment dispositions and remaining blocker

Exact package-custody records:

~~~text
C6A-C2-PACKAGE-EXPIRY-DURABILITY-01
= CLOSED AS CURRENT-PROFILE BLOCKER
/ DEFERRED BY HOMEOWNER ARCHITECTURE AMENDMENT

C2-PACKAGE-EXPIRY-PROFILE-RESOLUTION-01
= CLOSED AS CURRENT-PROFILE BLOCKER
/ NOT REQUIRED WHILE PACKAGE CUSTODY DEADLINE IS DEFERRED
/ REACTIVATE WITH PACKAGE-CUSTODY REQUIREMENT

C6A-PACKAGE-CUSTODY-DEFERRED-01
= DEFERRED BY HOMEOWNER AMENDMENT — NOT A CURRENT C6A BLOCKER
~~~

The historical requirement is neither rejected nor globally superseded. A
future explicit product, legal, custody or retention requirement may reactivate
it through a separately reviewed amendment.

TIP-88A finding disposition:

~~~text
RetentionProfileRef is mandatory policy metadata whose VALUE currently has
no machine-readable operational retention consumer.
= TIP-88A DESIGN FINDING — RECORDED / NON-BLOCKING FOR CURRENT C6A
~~~

Its presence and metadata shape are read and validated. C6A must not add a
resolver, duration column or remove the existing constraint.

Bounded dispatch-review dispositions:

~~~text
C6A-SOURCE-GRANT-REPLAY-01
= CLOSED AT BOUNDED DISPATCH REVIEW

C6A-SOURCE-GRANT-LOCK-ORDER-01
= CLOSED AT BOUNDED DISPATCH REVIEW

C6A-SOURCE-GRANT-COMPOSITION-ACL-01
= OPEN — BOUNDED ROLE-PIN CORRECTION
~~~

The replay/lock correction is the atomic-composition topology in section 4: the
existing-ingress replay path resolves the persisted SnapshotId before any
append; a new snapshot append and begin claim commit or roll back together.

## 12. STOP/RRI

STOP before implementation if:

1. independent dispatch review is not clean;
2. `C6A-SOURCE-GRANT-COMPOSITION-ACL-01` is not independently clean;
3. R1 derivation cannot use the pinned atomic-composition topology without a
   new durable surface;
4. C3 cannot revalidate without duplicating or weakening authority ownership;
5. lock ordering or actor identity requires impersonation;
6. an error reveals subject/source existence;
7. a proposed change touches C1 replay or C2 convergence semantics;
8. a new role/table/API/package/worker is requested;
9. exact Production profile registration cannot be expressed through the
   existing closed composition path;
10. proof requires real Raw BIO/patient/provider operation; or
11. the direct-recipient C3 actor cannot simultaneously satisfy original
    decision principal/client equality and frozen recipient equality without
    impersonation; or
12. Git/deployment/activation authority is inferred.

The open composition ACL finding already triggers this section. No C6A
implementation authority may be granted from v0.6.

## 13. Review questions

Independent review must answer:

1. Does atomic composition serialize on the exact existing ingress identity
   before deciding whether a snapshot append is needed?
2. Do rollback, response-loss and exact-retry proofs yield one persisted
   SnapshotId and one semantic admission identity?
3. Do wrapper and landed begin compute the same two keys, acquire
   `LEAST` then `GREATEST`, and share the same bounded timeout semantics when
   `exact_lock < alias_lock`?
4. Is direct EXECUTE restricted exactly to
   `tagekyc_raw_export_claim_broker`, with PUBLIC, `tagekyc_runtime` and every
   other runtime/broker/application role denied?
5. Is the R1 Grant derivation point genuinely server-owned and pre-byte?
6. Does reuse avoid a parallel access/source authority engine?
7. Is RB-12 checked inside every begin-stream transaction before CAS?
8. Are all 16 live C3 conditions covered without an oracle, including original
   decision/job principal and client equality?
9. Are committed replay, C2 convergence and state reconciliation untouched?
10. Is the helper/migration truly minimal and least-privileged?
11. Are all negative proofs discriminating and race-safe?
12. Can the Builder implement without inventing a field, role, endpoint or
   semantic decision?

A clean verdict is PASS — 0 actionable findings. Review does not grant
implementation.

## 14. Recommended next action

1. Run bounded independent review only of
   `C6A-SOURCE-GRANT-COMPOSITION-ACL-01` against the exact role pin and C6A23.
2. Resolve findings only inside the current dispatch scope.
3. Homeowner may explicitly grant or withhold implementation only after a
   clean dispatch SHA.

Terminal state:
RRI_OPEN_C6A_SOURCE_GRANT_COMPOSITION_ACL_01.
