# TIP-88C1-C2 — Package Expiry Durability Correction Dispatch

**Status:** SUPERSEDED_BY_CURRENT_PROFILE_HOMEOWNER_AMENDMENT — HISTORICAL ARTIFACT — IMPLEMENTATION NOT AUTHORIZED  
**Version:** 0.1  
**Date:** 2026-09-04  
**Risk tier:** High-risk  
**Owner:** Homeowner + Contractor/Drafter  
**Repository baseline:** `0a2d605bc0b1174825803e1982105d680718500c`  
**Trigger:** `C6A-C2-PACKAGE-EXPIRY-DURABILITY-01`  
**Implementation authority:** **NOT GRANTED**

## Current-profile amendment marker

For the current `EncryptedExportPacket` production profile, the Homeowner has
deferred a separately enforced package-custody deadline. This artifact is
therefore superseded as an implementation dispatch for that profile only.

This marker does not erase or globally supersede the historical
`PackageExpiresAt` architecture concept. Reactivation requires a future
explicit product, legal, custody or retention amendment and a new bounded
review.

Exact dispositions:

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

## HISTORICAL BODY NOTICE

Sections 1-14 below preserve the pre-amendment dispatch as historical evidence.
All OPEN/RRI/STOP/Recommended-next-action statements inside those sections are
historical statuses only and are NON-OPERATIVE for the current
`EncryptedExportPacket` profile. The authoritative current-profile
dispositions are the amendment marker above.

## 1. Objective

Add the missing durable `PackageExpiresAt` contract required by the ratified
TIP-88 spine without changing C2 package custody, provider I/O, committed
convergence, recipient-key binding or C1 seal semantics.

The correction must:

1. resolve an approved package-custody expiry from the exact frozen policy and
   export mode;
2. freeze that expiry atomically with the existing C2 transition to
   `Finalized`, which is the landed package-seal/finalization linearization;
3. make it immutable and visible through durable C2/C3 lineage;
4. preserve exact `Finalized` replay and recovery convergence; and
5. allow C3 to compute
   `min(PermitExpiresAt, JobExpiresAt, PackageExpiresAt)`.

## 2. Corpus-resolved semantics

The following are closed requirements, not Homeowner questions:

- `PackageExpiresAt` is the custody/delivery expiry for the sealed package;
- it is derived from the approved mode and policy;
- it is frozen atomically with `PackageSealed`;
- `DeliveryAuthorityExpiresAt` is derived, never operator-set;
- after delivery authority expiry, no new/resumed byte delivery is allowed;
- extending the window requires a new decision, permit and job.

Absence of a landed column does not make the term optional.

## 3. Landed topology

The C2 durable row is
`raw_export_recipient_package_preparations`. Its current lifecycle is:

~~~text
Reserved -> PutInFlight / PutOutcomeUnknown -> Prepared -> Finalized
~~~

`raw_export_finalize_recipient_package` currently:

1. locks the preparation row;
2. returns `ExistingMatch` for exact already-`Finalized` replay;
3. admits only exact `Prepared` revision + assembly fingerprint;
4. obtains one database timestamp;
5. changes state to `Finalized`, increments revision and stamps
   `FinalizedAtUtc`;
6. appends the `Finalized` event.

That transaction is the correction's freeze point. Do not move package-expiry
derivation to delivery creation, application memory or provider metadata.

## 4. Open derivation RRI

### C2-PACKAGE-EXPIRY-PROFILE-RESOLUTION-01

**Status:** OPEN — POLICY REFERENCE EXISTS / MACHINE-READABLE TTL MISSING

The approved policy stores `Mode`, `RetentionProfileRef` and
`RetentionPurposeCode`. TIP-88A deliberately forbids an inline retention
duration. The repository contains no authoritative resolver that turns the
frozen retention-profile reference into a package TTL or absolute expiry.

Therefore the Builder must not invent:

- a fixed duration;
- a mapping from a test value such as `retention:test`;
- `PermitTtlSeconds` as package TTL;
- `RecipientKeyValidUntilUtc`, source retention or C3's 30-minute attempt
  window as package expiry;
- a configuration default or `MaxValue` sentinel.

Before implementation, a bounded predecessor dispatch must pin the existing
approved retention-schedule/profile authority and an owner-only resolution
contract returning an exact package custody duration or absolute bound for the
frozen `(PolicyId, PolicyVersion, Mode, RetentionProfileRef,
RetentionPurposeCode)` tuple.

For `EncryptedExportPacket`, the resolver must return one finite approved
bound. Unsupported mode/controller/profile combinations fail closed. This C2
dispatch may consume that resolved value after it is reviewed; it does not
authorize creating a new retention-policy system.

## 5. Frozen correction topology

After the resolver dependency is satisfied, use this order inside the C2
finalize transaction:

~~~text
lock exact C2 preparation
  -> preserve exact Finalized replay branch
  -> validate Prepared revision + assembly fingerprint
  -> derive durable JobId -> job identity -> frozen PolicyId/Version/Mode
  -> prove policy remains the exact approved version and profile binding
  -> resolve approved finite package custody bound
  -> one post-lock database clock
  -> compute PackageExpiresAt without overflow
  -> require now < PackageExpiresAt
  -> atomically set Finalized + FinalizedAtUtc + PackageExpiresAt
  -> append Finalized event carrying expiry-bound evidence
  -> commit
~~~

No provider call or object read occurs while these locks are held.

## 6. Derivation and equality rules

The exact arithmetic remains blocked by
`C2-PACKAGE-EXPIRY-PROFILE-RESOLUTION-01`. Once resolved, it must obey:

- input identity comes only from durable preparation -> job -> policy lineage;
- no request parameter supplies policy, mode, profile, TTL or expiry;
- the calculation uses checked UTC microsecond arithmetic and one post-lock
  database clock;
- the result is finite and strictly later than the freeze timestamp;
- the result cannot exceed any explicit absolute bound returned by the
  approved retention profile;
- exact replay compares/returns the persisted expiry; it never recomputes it
  under a newer profile version;
- changed policy/profile semantics cannot mutate an already finalized package;
- no extension operation exists.

Whether the approved profile defines `sealed_at + duration` or an absolute
deadline must be pinned by the predecessor resolver dispatch. The Builder may
not choose between them.

## 7. Durable model delta

Expected minimal delta after RRI closure:

- add non-null `PackageExpiresAtUtc` to the C2 preparation row for every state
  at/after `Finalized`;
- keep it null before `Finalized` unless the independently reviewed resolver
  contract requires an earlier immutable reservation;
- strengthen the C2 sparse/state CHECK so only the correct lifecycle shapes
  carry the value;
- include the value in recovery/read projections used by C2 and C3;
- include an expiry-bound digest or the canonical expiry in the `Finalized`
  event evidence so durable replay proves the exact frozen value;
- update the model snapshot and migration readiness pins.

Do not add a separate `DeliveryAuthorityExpiresAt` column in this correction.
C3 derives that minimum from durable inputs at delivery admission.

## 8. Committed convergence preservation

The correction must not change these C2 rules:

- exact `Finalized` replay returns `ExistingMatch` before stale predecessor
  rejection;
- `Prepared -> Finalized` remains a single database transaction;
- a provider success or prepared object is never undone because current
  authority later changes;
- C1 committed seal and C2 exact finalization continue to converge;
- no package is re-encrypted, re-uploaded or recreated to add expiry metadata;
- `Finalized` remains terminal for custody mutation;
- abort/cleanup/quarantine rules remain unchanged.

Authority freshness required before seal belongs at the existing seal/finalize
barrier. Later authority loss blocks C3 delivery; it does not rewrite committed
C2 history.

## 9. C3 lineage contract

C3 must be able to resolve, without request-supplied authority fields:

~~~text
DeliveryId -> PackageId -> C2 preparation -> PackageExpiresAtUtc
          -> JobId -> PermitExpiresAt / JobExpiresAt
~~~

At every begin-stream attempt C3 computes:

~~~text
DeliveryAuthorityExpiresAt =
  min(PermitExpiresAt, JobExpiresAt, PackageExpiresAtUtc)
~~~

and requires the post-lock database clock to be strictly before all three
inputs and the derived minimum. C3's local `AuthorizationExpiresAtUtc` is an
additional attempt ceiling, not a substitute.

## 10. Minimal allowlist

This is a review ceiling, not implementation authority.

Expected existing paths:

- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackagePreparationRow.cs`
- `src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackagePreparationConfig.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`
- `src/TagEkyc.Infrastructure/RawExport/RecipientPackageContracts.cs`
- `src/TagEkyc.Infrastructure/RawExport/RecipientPackageRepository.cs`
- C2 readiness validator only if function/model pins change
- `tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs`
- `tests/TagEkyc.ArchTests/Tip88C1C2RecipientPackageArchTests.cs`
- applicable C3 lineage tests after C6A rebind
- C2 as-built/review ledger and C6A dependency lineage

Conditional new path:

- one EF migration plus generated Designer after the derivation RRI is closed.

Not allowlisted:

- policy authoring schema changes;
- arbitrary inline TTL on policy rows;
- provider/object format changes;
- C1 replay or C2 provider workflow redesign;
- public endpoint/DTO/configuration additions;
- purge worker, legal hold, C6B adapter or production activation.

## 11. Proof obligations

| ID | Proof | Mutation that must make RED |
| --- | --- | --- |
| PX01 | exact approved profile resolves one finite bound | use fixed/default TTL |
| PX02 | policy/mode/profile identity is derived from JobId lineage | accept caller policy/profile |
| PX03 | expiry freezes in same transaction as Finalized | write expiry before/after finalize commit |
| PX04 | exact Finalized replay returns persisted expiry | recompute under changed profile |
| PX05 | wrong revision/fingerprint changes no expiry/state/event | write expiry before CAS validation |
| PX06 | unsupported/missing profile fails closed before Finalized | treat missing mapping as unlimited |
| PX07 | expiry equality and elapsed expiry fail closed | use `<=` incorrectly or permit equality |
| PX08 | overflow/MaxValue/sentinel rejected | unchecked duration addition |
| PX09 | C1 seal and C2 recovery convergence remain exact | abort/recreate prepared package |
| PX10 | no provider I/O under expiry-resolution locks | resolve via remote call in transaction |
| PX11 | C3 reads durable PackageExpiresAt and three-term minimum | use local 30-minute window only |
| PX12 | Down/reapply/model snapshot/function ACL exact | orphan overload or stale model |

Use real PostgreSQL transactions and synthetic encrypted package fixtures. No
patient data or production provider operation.

## 12. STOP/RRI

STOP before code/schema edits if:

1. `C2-PACKAGE-EXPIRY-PROFILE-RESOLUTION-01` is not closed;
2. the proposed resolver invents a duration or duplicates policy authority;
3. PackageExpiresAt cannot freeze atomically with Finalized;
4. exact Finalized replay would recompute or mutate expiry;
5. C1 committed seal or C2 custody convergence changes;
6. a new public API, role, table, worker or provider format is requested;
7. a mode/controller pair not already approved would be enabled;
8. schema migration/backfill cannot fail closed for existing finalized rows;
9. implementation authority is inferred from review.

Existing finalized rows require an explicit migration disposition. Silent
backfill from migration time, C3 authorization time, recipient-key expiry or
job expiry is forbidden. If no authoritative historical profile value can be
resolved, those rows must remain non-deliverable until an independently
reviewed disposition exists.

## 13. Review questions

1. Is the TTL/bound resolver authoritative and already owned, rather than a new
   retention-policy system?
2. Are mode, policy and profile derived only from durable job lineage?
3. Is expiry frozen in the exact Finalized transaction and replay-persisted?
4. Are existing finalized rows handled without fabricated history?
5. Are C1 committed replay and C2 convergence byte-for-byte unchanged except
   for the additive expiry evidence?
6. Can C3 consume the three-term minimum without another schema invention?

## 14. Recommended next action

1. Identify and dispatch the authoritative retention-profile resolution
   contract for `C2-PACKAGE-EXPIRY-PROFILE-RESOLUTION-01`.
2. Bind its exact reviewed result into this correction dispatch.
3. Run bounded independent C2 correction review.
4. Only after a clean verdict may Homeowner grant implementation.

HISTORICAL_TERMINAL_STATE_BEFORE_AMENDMENT:
`RRI_OPEN_PACKAGE_EXPIRY_PROFILE_RESOLUTION_REQUIRED`

CURRENT_ARTIFACT_STATE:
`SUPERSEDED_BY_CURRENT_PROFILE_HOMEOWNER_AMENDMENT`
