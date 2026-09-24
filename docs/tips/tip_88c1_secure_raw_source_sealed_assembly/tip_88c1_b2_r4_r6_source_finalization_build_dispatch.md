# TIP-88C1-B2-R4R6 — Source Finalization — BUILD DISPATCH

**Version:** 0.4-review-candidate
**Status:** DRAFT FOR INDEPENDENT REVIEW — NOT RATIFIED — NOT DISPATCHED
**Date:** 2026-08-12
**Repository:** `D:\Task\Remote Signing\TagEkyc`
**Required branch:** `tip-88a-raw-export-policy-catalog-build`
**Baseline:** `81f02ab4668de4ac3d8ec2b9ba3cceb4a50151e4`
**Risk tier:** High-risk
**PI-TAG-001:** ACTIVE for TIP-88C1; round-5 checkpoint and round-10 hard stop apply
**Review round:** 4

This file is documentation-only until an independent clean review and an
explicit Homeowner ratification bind its exact SHA-256. It does not authorize
implementation, migration execution, staging, commit, push, merge, PR,
deployment, production activation, real Raw BIO, a production raw-source
adapter, resolver/assembly work, recipient encryption, package construction or
delivery.

## Changelog

### v0.4 — R3-compatible locking, vector mapping and replay precedence

- Reordered shared rows to match landed R3: attempt before head, key before
  object, while retaining publication first where it exists.
- Extended F408 to force concurrent landed R3 replay against both R4 replay and
  R5 publish; head-before-attempt mutations must RED by deadlock/timeout.
- Removed absolute-vector ambiguity by naming winning versus obsolete object
  and key identifiers explicitly; the five already-correct digests are
  unchanged.
- Pinned R6 exact-response-loss replay before general stale-revision rejection
  for both item completion and publication finalization.
- Reconciled R6 with the same total order by using a non-locking item identity
  probe followed by publication, exact resource, item and terminal-event locks
  plus post-lock probe revalidation.

### v0.3 — R6 command, catalog and concurrency closure

- Pinned one global publication-first row-lock order and a concurrent R4 replay
  versus R5 proof so response-loss recovery cannot create an AB-BA deadlock.
- Pinned the complete R6 object/key lifecycle command and reason matrix using
  only landed Durable Object and DK-PROD surfaces.
- Pinned exact argument and return catalogs for all three R6 SQL functions and
  selected the landed lifecycle-context function as the only object-locator
  source.
- Split F413's column-required `NOT NULL` proofs from nullable shape-CHECK
  proofs so each mutation fails for its claimed discriminator.
- Corrected the cleanup-vector display to a 32+32 hexadecimal split while
  preserving the 64-byte textual value.

### v0.2 — First-review execution closure

- Added the mandatory fresh authority/consent/post-lock-time barrier to R4 and
  the `SourceRetentionNotAuthorized` outcome while keeping exact replay first.
- Bound R4 and R5 evidence to the landed authority resolver's exact
  `Revision`, `AuthoritySnapshotSchemaVersion` and `AuthoritySnapshotId`; no
  synthetic authority scalar is admitted.
- Made the R5 publication update shape-valid in one statement after computing
  the cleanup census; `Available + NotPlanned` is impossible.
- Added exact publication/item revisions, exact R6 parameter ownership,
  kind/disposition pairs, two per-item codecs and absolute vectors.
- Pinned exact foreign keys versus function-enforced cross-row bindings,
  explicit NULL/three-valued CHECK discipline and the R5 head-guard re-emission.
- Named all write contexts and required byte-exact R3 guard restoration in
  `Down()`.
- Preserved the 18-path allowlist and F401–F418 proof census.

## 0. Source-of-truth register and baseline facts

The following bytes were verified while drafting this version:

| Artifact | SHA-256 | Authority consumed here |
| --- | --- | --- |
| `tip_88c1_planning_brief.md` | `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` | R4 commit, R5 publication, R6 obsolete-residue cleanup; Available-only visibility |
| `tip_88c1_b2_r2_build_dispatch.md` | `9A758E07D3283017AD84E489072F3EB7782EEFE526B1999DD14A53515633FFED` | authenticated framed ciphertext and typed verification |
| `tip_88c1_b2_r2_as_built.md` | `760403BCF6434119B83D9FD0DE104867D6A92998071CF53257DD1FCFE1EAF42D` | landed `VerifiedCompleted` handoff |
| `tip_88c1_b2_r3_build_dispatch.md` | `5A426518A41E9A88F0C2A6D5FD2B5E7D30549FA81DDCEDAB8CA9284E9C8E0E8A` | exact staged fingerprint, post-lock authority clock and `Staged` CAS |
| `tip_88c1_b2_r3_as_built.md` | `1F666C460A14CE78F8BE4AE575658979B4B6A538F13B604A4A3920AD5B6C0ED8` | landed R3 schema/function/evidence |
| `tip_88c1_b2_durable_key_prod_as_built.md` | `AAF9E89A20C5BFF09C3A69DE7239BF677FE4665746AFB09AA732A69BCF1F89E7` | durable attempt-key states and lifecycle functions |
| `tip_88c1_b2_durable_key_fixture_proof_as_built.md` | `8BC73AC4DF7280CE53BE5F6A4FF91AB369660A15D96923F8A3B6A1F2F28CC896` | fixture-only restart-safe key proof |
| `tip_88c1_b2_durable_object_custody_build_dispatch.md` | `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918` | conditional single-part never-versioned object contract |
| `tip_88c1_b2_durable_object_custody_as_built.md` | `B9E8873F45F826DC8889B01A23E9393F81A12B75F587E87153BDE34F71536A6E` | landed exact-object roles/functions/provider behavior |
| `tip_88c1_gov_art_authorization_packet_ratification_v0_7.md` | `F70E6155488B0F6AE6F1ADB1CDB4AD888283E000B792685274873C1988FF23C8` | fixture-only generated non-patient evidence authority |

Relevant landed code anchors at the baseline are:

```text
66D7EB2B5672211AA83AEF8B4472AAAF51AA687226E7934C321D9C6DEFFFB154  RawExportSourceHeadRow.cs
DC0F1DF21AF67EB180775CED4451D138B27618FDD77FDCB5DAD39B4D8A0EE84B  RawExportSourceEncryptionAttemptRow.cs
087D7D9DE0469E913BAD226EFB466FAF5E1DEA8E669A7E43A07717D026027A9F  RawExportProvisionalObjectRow.cs
447BD6B1DC6A9247E18F3E32AF987EAD6E5EA5729A19B907C306C5FDE98723DA  TagEkycDbContext.cs
D81B8593166009279EFC770A5DDD55C3020FD71A4A822CEBCCC54BF3FF0BA510  TagEkycDbContextModelSnapshot.cs
```

The active GOV/ART chain is fixture-only. Controller/legal basis, production
retention, purge, legal-hold and access decisions remain open. Accordingly this
build may prove publication only with generated non-patient fixture content and
fixture authority. It must fail production readiness and makes no real-artifact
retention claim.

## 1. TIP Analytical Summary / Intent Ledger

### Intent

Finish the metadata lifecycle of one exact R3-staged encrypted source:

```text
R4: exact StagedCiphertextFingerprint -> durable metadata commit
R5: fresh authority/consent -> opaque locator + Available descriptor
R6: remove only obsolete losing-attempt residue; preserve the winning object/key
```

### Expected outcome

```text
exact current Staged attempt
+ exact VerifiedCompleted object
+ exact Active winning key reservation
+ fresh R4 authority and consent
-> one committed-source row
-> fresh post-lock R5 authority and consent barrier
-> one opaque Available locator
-> source head Staged -> Available
-> cleanup work only for non-winning objects/keys
-> Available remains stable while cleanup retries
```

R4 and R5 are short PostgreSQL transactions. R6 is a restart-safe lifecycle
saga over exact cleanup items. No step reads plaintext or decrypts ciphertext.

### Accepted decisions

| Decision | Reason | Consequence |
| --- | --- | --- |
| R4 promotion is metadata-only | Landed object storage prohibits copy, multipart and versioning; the exact object is already immutable and authenticated | No S3 copy/move/rename and no second ciphertext write |
| R4 preserves the winning object row in `VerifiedCompleted` | That row is durable byte-verification provenance, not obsolete residue | R4 does not reopen the Durable Object state machine |
| R5 generates one opaque UUID locator in PostgreSQL | Resolver consumers must not receive bucket/key/provider details | Locator maps server-side to one committed row only |
| R5 alone makes the head `Available` | Publication requires fresh authority, consent and deadline checks | `Committed` remains non-readable |
| R6 is after-publication cleanup | Planning explicitly allows Available while R6 cleanup is pending | Cleanup failure cannot roll back or hide an already Available descriptor |
| R6 excludes the winning object and winning key by exact identifiers | They are required to decrypt the Available source later | Cleanup may never revoke/delete the live source material |
| Reuse landed reconciliation and lifecycle capabilities | Capability graph already separates metadata reconciliation from exact deletion/key lifecycle | No new role, LOGIN, membership or public runtime grant |

### Rejected or deferred branches

| Branch | Disposition | Reason / follow-up |
| --- | --- | --- |
| Copy object from a provisional key to a committed key | Rejected | Would require a new provider operation and duplicate ciphertext; the opaque locator is metadata |
| Put/delete/list/presign/multipart in R4/R5 | Rejected | R4/R5 are DB-only |
| Destroy the winning attempt key in R6 | Rejected | Would make Available ciphertext undecryptable |
| Make R6 completion a prerequisite for Available | Rejected | Contradicts recovery after R5 response loss and planning's Available-with-cleanup-pending rule |
| Physical disposition after authority loss | Deferred and fail-closed | Purge/legal-hold predicate remains unratified; no descriptor is published and no physical action is invented here |
| Expose a resolver/API DTO | Deferred | A later C1 resolver/assembly slice consumes only Available descriptors |
| Real raw-source adapter or patient content | Deferred | Requires separate legal, GOV/ART and production authority |
| C2 recipient encryption/package/delivery | Deferred | Separate TIP-88C2/88C3 work |

### Debt impact and non-claims

This slice narrows `C1-B2-CORE-DEFERRED-OUTCOMES-GATE` by creating the internal
Available descriptor required for a later `AlreadyAvailable` mapping. It does
not re-emit the ingress `begin` function or expose `AlreadyAvailable` externally.
It does not close `C1-A-CLASS-PROVENANCE-GATE`, the production authority/profile
gates, vault/HSM catalog gates, purge/hold governance, or real Raw BIO readiness.

## 2. Ownership and phase boundaries

### 2.1 This slice owns exactly

1. One committed-source publication row with a two-shape
   `Committed | Available` contract.
2. One append-only cleanup-item table for losing object/key identities.
3. R4 metadata commit of the exact staged result.
4. R5 fresh publication and `Staged -> Available` head CAS.
5. R6 exact-resource cleanup progress and finalization evidence.
6. Internal C# contracts, repository/services and evidence codecs.
7. Exact SQL/catalog/ACL/rollback and fixture-only behavioral proofs.

### 2.2 It consumes unchanged

- R1 reservation/claim/authority identity and source deadline;
- R2 durable key/object and verified evidence;
- R3 staged projection and `StagedCiphertextFingerprint` v2;
- authority and consent resolvers and their advisory-lock domains;
- `IProvisionalObjectLifecycle.DeleteExactAsync` for exact single-key deletion;
- DK-PROD lifecycle functions for exact non-winning key revocation/abandonment;
- landed reconciler and lifecycle roles and LOGIN membership topology.

### 2.3 It must not own

- object PUT, copy, multipart, list, presign, bucket operation or versioning;
- plaintext, bare plaintext digest, DEK/KEK material or AEAD work;
- provider/key catalog redesign or readiness topology;
- authority policy, purge/hold policy or legal decision-making;
- public resolver, endpoint, runtime DTO, queue, assembly or delivery.

## 3. Exact persisted shape

### 3.1 `raw_export_source_publications`

Add one deployer-owned, direct-DML-denied table:

| Column | Type | Committed | Available |
| --- | --- | --- | --- |
| `SourcePublicationId` | uuid PK | present | unchanged |
| `SourceArtifactId` | uuid unique FK | present | unchanged |
| `AttemptId` | uuid unique FK | present | unchanged |
| `ObjectCustodyId` | uuid unique FK | present | unchanged |
| `AttemptKeyReservationId` | uuid FK | present | unchanged |
| `StagedCiphertextFingerprintSchemaVersion` | integer | exactly 2 | unchanged |
| `StagedCiphertextFingerprint` | bytea | 32 bytes | unchanged |
| `CommittedAuthoritySnapshotSchemaVersion` | integer | positive | unchanged |
| `CommittedAuthoritySnapshotId` | uuid | present | unchanged |
| `CommittedAuthorityRevision` | bigint | positive | unchanged |
| `CommittedConsentPolicyId` | uuid | present | unchanged |
| `CommittedConsentPolicyVersion` | integer | positive | unchanged |
| `CommitEvidenceDigest` | bytea | 32 bytes | unchanged |
| `CommittedAtUtc` | timestamptz | present | unchanged |
| `PublicationState` | text | `Committed` | `Available` |
| `OpaqueCommittedLocatorId` | uuid unique | NULL | present |
| `PublishedAuthoritySnapshotSchemaVersion` | integer | NULL | positive |
| `PublishedAuthoritySnapshotId` | uuid | NULL | present |
| `PublishedAuthorityRevision` | bigint | NULL | positive |
| `PublishedConsentPolicyId` | uuid | NULL | present |
| `PublishedConsentPolicyVersion` | integer | NULL | positive |
| `AvailableEvidenceDigest` | bytea | NULL | 32 bytes |
| `AvailableAtUtc` | timestamptz | NULL | present |
| `CleanupDisposition` | text | `NotPlanned` | `Pending`, `NoObsoleteResidue`, or `Completed` |
| `CleanupEvidenceDigest` | bytea | NULL | NULL until terminal, then 32 bytes |
| `FinalizedAtUtc` | timestamptz | NULL | NULL until terminal, then present |
| `PublicationRevision` | bigint | exactly 1 | 2 while pending/no-residue; 3 after R6 completion |
| `SchemaVersion` | integer | exactly 1 | exactly 1 |

The named check `ck_raw_export_source_publication_shape` defines exactly these
two publication shapes and the cleanup sub-shape. There is no partially
Available row and no `Available + NotPlanned` statement-visible shape.

Every shape-required nullable member in every CHECK branch is first tested with
explicit `IS NOT NULL`; every forbidden member is tested with `IS NULL`. Only
after presence is established may the predicate use `=`, `IN`, `BETWEEN` or
`octet_length`. PostgreSQL UNKNOWN must never satisfy a shape. Column-required
identity members, including both primary keys, are mapped `NOT NULL` and are
not misclassified as CHECK-owned members.

F413 has two discriminator groups: setting each column-required member NULL
must produce exact `23502`/the expected NOT-NULL column before any shape claim;
with the write trigger disabled, setting each shape-required nullable member
NULL must produce exact `23514` naming the corresponding shape CHECK. A
failure from the other group is RED for the wrong reason.

The exact restrictive foreign-key catalog is:

```text
fk_raw_export_source_publication_reservation:
publication.SourceArtifactId
  -> raw_export_source_reservations.SourceArtifactId
fk_raw_export_source_publication_attempt:
publication.AttemptId
  -> raw_export_source_encryption_attempts.AttemptId
fk_raw_export_source_publication_object:
publication.ObjectCustodyId
  -> raw_export_provisional_objects.ObjectCustodyId
fk_raw_export_source_publication_key:
publication.AttemptKeyReservationId
  -> raw_export_attempt_key_reservations.AttemptKeyReservationId
fk_raw_export_source_publication_attempt_key_binding:
(publication.AttemptId, publication.AttemptKeyReservationId)
  -> raw_export_source_encryption_attempts
     (AttemptId, AttemptKeyReservationId)
```

All use `ON DELETE RESTRICT`. The landed catalog has no unique principal key
for `(AttemptId,ObjectCustodyId)` on the object table or for
`(AttemptId,AttemptKeyReservationId)` on the key-reservation table. Therefore
no fictional composite FK is claimed. R4/R5 instead lock the exact object and
key rows and prove their `AttemptId`, source id and immutable fingerprints
against the attempt before INSERT/UPDATE. The publication row is then
append-once/one-way guarded, so that function-enforced binding cannot drift.

The remaining exact catalog names are:

```text
pk_raw_export_source_publications
uq_raw_export_source_publication_source
uq_raw_export_source_publication_attempt
uq_raw_export_source_publication_object
uq_raw_export_source_publication_locator
ck_raw_export_source_publication_shape
tr_raw_export_source_publication_guard
enforce_raw_export_source_publication_write
```

The locator uniqueness is a unique nullable index: multiple Committed rows may
have NULL, while every non-NULL Available locator is globally unique.

### 3.2 `raw_export_source_cleanup_items`

R5 inserts one immutable work identity per obsolete resource:

```text
CleanupItemId uuid PK
SourcePublicationId uuid FK
ResourceKind text in {ProvisionalObject, AttemptKeyReservation}
ResourceId uuid
ResourceAttemptId uuid
PlannedResourceRevision bigint >= 1
CleanupState text in {Pending, Completed}
CompletionDisposition text NULL | kind-compatible terminal token
CleanupEvidenceDigest bytea NULL | 32 bytes
CompletedAtUtc timestamptz NULL | present
RowRevision bigint: 1 Pending, 2 Completed
SchemaVersion integer = 1
```

Unique `(SourcePublicationId, ResourceKind, ResourceId)` prevents duplicate
work. A Pending row has all completion fields NULL; a Completed row has all
present. INSERT occurs only in R5. UPDATE is one-way `Pending -> Completed` and
changes only completion fields under the lifecycle write context. DELETE is
forbidden.

The exact compatibility matrix enforced by
`ck_raw_export_source_cleanup_item_shape` is:

```text
ProvisionalObject     -> Deleted | Quarantined
AttemptKeyReservation -> Revoked | ReservationAbandoned
```

Cross-kind tokens are invalid. `ResourceId` is deliberately polymorphic and
therefore has no impossible polymorphic FK. R5 is the only creator: while
holding the publication, attempt, object and key rows, it validates the
resource kind, resource id, `ResourceAttemptId` and current resource revision.
R6 re-locks the typed resource before completion. The item guard makes those
identity fields immutable.

The same explicit `IS NOT NULL`/`IS NULL` discipline and the same separation of
column-required versus shape-required-nullable proof ownership apply to every
cleanup-item predicate before any value or length comparison.

Its exact catalog names are:

```text
pk_raw_export_source_cleanup_items
uq_raw_export_source_cleanup_item_resource
fk_raw_export_source_cleanup_item_publication
ix_raw_export_source_cleanup_item_pending
ck_raw_export_source_cleanup_item_shape
tr_raw_export_source_cleanup_item_guard
enforce_raw_export_source_cleanup_item_write
```

The publication FK is `ON DELETE RESTRICT`. The pending index is non-authority
operational acceleration; the lifecycle read function still locks and validates
the publication/item/resource identities and revisions.

### 3.3 Source head

Expand the existing named check to:

```text
CustodyState IN ('Reserved','Staged','Available')
AND ReservationRevision >= 1
AND Fence >= 1
```

R5 changes only:

```text
CustodyState:         Staged -> Available
ReservationRevision: old -> old + 1
```

Source id, current attempt id and fence remain unchanged. R4 and R6 do not
modify the head.

The new migration must `CREATE OR REPLACE`, never edit landed migration bytes,
`tagekyc.enforce_raw_export_source_head_write()`. It preserves the landed R1
and R3 branches and adds exactly one R5 branch:

```text
current_user = tagekyc_raw_export_deployer
write context = tip88c1-r5-publish-v1
TG_OP = UPDATE
OLD.CustodyState = Staged
NEW.CustodyState = Available
NEW.ReservationRevision = OLD.ReservationRevision + 1
SourceArtifactId, CurrentEncryptionAttemptId and Fence unchanged
```

Every other transition, sibling mutation and DELETE remains forbidden. The
publication/cleanup guards and head branch use these exact transaction-local
contexts, captured and restored on success and exception:

```text
tip88c1-r4-commit-v1
tip88c1-r5-publish-v1
tip88c1-r6-cleanup-v1
```

The publication guard admits only INSERT under the R4 context, the exact
Committed-to-Available update under the R5 context, and the exact publication
cleanup-disposition Pending-to-Completed update under the R6 context. The
cleanup-item guard admits only INSERT under the R5 context and exact
Pending-to-Completed update under the R6 context. Both require
`current_user=tagekyc_raw_export_deployer`, compare every non-owned column
`IS NOT DISTINCT FROM OLD`, reject DELETE and restore the prior transaction-local
context on every exit.

`Down()` restores the complete landed R3 head-guard body byte/semantic-exactly,
including both predecessor branches; its proof compares against the baseline
forward body, not against a self-captured post-Up definition.

### 3.4 Global row-lock order

R4, R5 and R6 use one global order whenever the corresponding row exists:

```text
publication
-> encryption attempts in ascending AttemptId
-> source head
-> source reservation
-> ingress claim
-> attempt-key reservations in ascending AttemptKeyReservationId
-> provisional objects in ascending ObjectCustodyId
-> cleanup items in ascending CleanupItemId
-> exact typed terminal event rows
-> authority shared advisory lock
-> consent shared advisory lock
-> one admission clock for R4/R5 only
```

R4 may perform one non-locking identity probe from `p_attempt_id` solely to
locate the possible publication and source tuple. It then locks an existing
publication row before any other row and revalidates every probed value after
the ordered locks. If no publication exists and that probe does not observe the
complete landed `Staged` shape, R4 returns `StateConflict` before acquiring any
row lock; it never waits head-first against an in-flight R3 attempt-first
transaction. A concurrent R3 commit may therefore cause one conservative
`StateConflict`, followed by successful retry, but cannot deadlock or authorize
early commit. If the probe observes complete `Staged`, R3's transaction is
already committed. No R5/R6 transaction can yet own a missing publication; the
unique INSERT plus the ordered locks serialize the first R4 commit.

R5 and R6 always lock the publication first. R4/R5 then match the landed R3
shared order exactly for every common row: attempt, head, reservation, claim,
key, object. R5 locks all attempts before the head, then all keys before all
objects, using the ascending order above. An R6 item-completion call performs a
non-locking item identity probe only to locate the publication and typed
resource, then locks publication, exact typed resource, cleanup item and exact
terminal event in that order and revalidates every probed value. R6 `read_next`
and finalization need only the publication then cleanup-item subset. No function
reverses this order.

An R3 response-loss replay running concurrently with either R4 replay or R5,
and an R4 replay running concurrently with R5, must all return closed outcomes
after ordinary lock waiting; PostgreSQL `40P01` is not an accepted business
outcome. F408 executes all three concurrency pairs. Mutating R4 or R5 back to
head-before-attempt, or object-before-key, must RED by the forced interleaving.

## 4. Evidence codecs

All codecs use the landed `C1HashCanonical`: NFC UTF-8 text; signed 32-bit
big-endian byte-length prefix for domain and every field; lowercase UUID-N,
lowercase hex and invariant decimal. Timestamps use UTC microseconds exactly as
`yyyy-MM-ddTHH:mm:ss.ffffffZ`.

### 4.1 R4 commit evidence

```text
C1HashCanonical("tip-88c1-source-commit-v1", {
  SourceArtifactId,
  AttemptId,
  ObjectCustodyId,
  AttemptKeyReservationId,
  StagedCiphertextFingerprintSchemaVersion,
  StagedCiphertextFingerprint,
  CommittedAuthoritySnapshotSchemaVersion,
  CommittedAuthoritySnapshotId,
  CommittedAuthorityRevision,
  CommittedConsentPolicyId,
  CommittedConsentPolicyVersion,
  CommittedAtUtc
})
```

### 4.2 R5 Available evidence

```text
C1HashCanonical("tip-88c1-source-available-v1", {
  SourcePublicationId,
  SourceArtifactId,
  AttemptId,
  ObjectCustodyId,
  AttemptKeyReservationId,
  OpaqueCommittedLocatorId,
  CommitEvidenceDigest,
  PublishedAuthoritySnapshotSchemaVersion,
  PublishedAuthoritySnapshotId,
  PublishedAuthorityRevision,
  PublishedConsentPolicyId,
  PublishedConsentPolicyVersion,
  AvailableAtUtc
})
```

### 4.3 R6 terminal cleanup evidence

```text
C1HashCanonical("tip-88c1-source-finalization-cleanup-v1", {
  SourcePublicationId,
  SourceArtifactId,
  CleanupDisposition,
  CompletedCleanupItemCount,
  FinalizedAtUtc
})
```

The exact per-item codecs are:

```text
C1HashCanonical("tip-88c1-source-object-cleanup-item-v1", {
  SourcePublicationId,
  ResourceAttemptId,
  ObjectCustodyId,
  CompletionDisposition,
  UnderlyingObjectTerminalEvidenceDigest,
  CompletedAtUtc
})

C1HashCanonical("tip-88c1-source-key-cleanup-item-v1", {
  SourcePublicationId,
  ResourceAttemptId,
  AttemptKeyReservationId,
  CompletionDisposition,
  UnderlyingKeyTerminalEvidenceDigest,
  CompletedAtUtc
})
```

For `Deleted`, the underlying object digest is exactly the landed
`DeletionEvidenceDigest`; for `Quarantined` it is exactly
`QuarantineEvidenceDigest`. For `Revoked`, the underlying key digest is exactly
the terminal preparation event's `RevocationEvidenceDigest`; for
`ReservationAbandoned` it is exactly `AbandonmentEvidenceDigest`. Each must be
32 bytes and belong to the locked typed resource/revision. No caller-supplied,
fallback or cross-kind digest is admitted.

### 4.4 Absolute vectors

For:

```text
SourceArtifactId       = 11111111111111111111111111111111
AttemptId              = 22222222222222222222222222222222
Winning ObjectCustodyId= 33333333333333333333333333333333
Winning key reservation= 44444444444444444444444444444444
Staged schema          = 2
Staged fingerprint     = 000102030405060708090a0b0c0d0e0f
                         101112131415161718191a1b1c1d1e1f
SourcePublicationId    = 55555555555555555555555555555555
Opaque locator         = 66666666666666666666666666666666
Authority snapshot     = 77777777777777777777777777777777
Authority schema       = 1
Authority revision     = 9
Consent policy         = 88888888888888888888888888888888
Consent version        = 1
Timestamp              = 2026-08-12T00:00:00.123456Z
ResourceAttemptId      = 99999999999999999999999999999999
Obsolete ObjectCustodyId
                       = aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
Obsolete key reservation
                       = bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
Object terminal digest = a0a1a2a3a4a5a6a7a8a9aaabacadaeaf
                         b0b1b2b3b4b5b6b7b8b9babbbcbdbebf
Key terminal digest    = c0c1c2c3c4c5c6c7c8c9cacbcccdcecf
                         d0d1d2d3d4d5d6d7d8d9dadbdcdddedf
```

the expected digests are:

```text
CommitEvidenceDigest   = 2df9964cfb6d0148248d9c8930226077
                         37c06d687075f9037ac262aa37ef3dba
AvailableEvidenceDigest= 46186c52cf2101d6cb12aab53a946aba
                         1148e25dda4e49d81b82b6514309e2de
CleanupEvidenceDigest  = 5b3899bc0678e04876dabf2647dc6428
                         1bbf6f053e8e828891fed38e77116c4f
ObjectItemEvidence     = 7cbdb5fe1a882fc150399f612005e678
                         6854c1ae587238110f9894af362e8d92
KeyItemEvidence        = a1c47ba47a3d8c02d928df4c2fb737ad
                         399b5971698c0fe666296b95836505ed
```

The cleanup vector uses `NoObsoleteResidue`, count `0` and the same timestamp.
The commit/available vectors use the winning object/key values `333...` and
`444...`. The object/key item vectors use the distinct obsolete resource IDs
`aaa...` and `bbb...` as their `ObjectCustodyId` and
`AttemptKeyReservationId`, respectively; those values are not `CleanupItemId`
and no cleanup-item UUID participates in either pinned codec. The item vectors
use `Deleted` and `Revoked`, respectively. Tests construct all five expected
preimages independently. A common-mode SQL+C# field reordering must still make
the absolute oracle RED.

The authority values above are not invented scalars. The landed
`raw_export_resolve_current_authority_for_source` return shape contains exact
`Revision`, `AuthoritySnapshotSchemaVersion` and `AuthoritySnapshotId` columns;
the consent resolver returns the exact policy identity/version and validity
window. R4 and R5 persist only values returned by those resolvers and matched
to the locked reservation/claim. A schema version is never relabeled as a
revision.

## 5. R4 exact metadata commit

Add exactly:

```sql
tagekyc.raw_export_commit_staged_source(
  p_attempt_id uuid,
  p_expected_reservation_revision bigint,
  p_expected_attempt_revision bigint,
  p_expected_fence bigint,
  p_expected_object_state_revision bigint
)
RETURNS TABLE(
  "Outcome" text,
  "SourcePublicationId" uuid,
  "SourceArtifactId" uuid,
  "AttemptId" uuid,
  "ObjectCustodyId" uuid,
  "CommitEvidenceDigest" bytea,
  "CommittedAtUtc" timestamptz,
  "ReservationRevision" bigint,
  "Fence" bigint)
```

It is `SECURITY DEFINER`, `search_path=pg_catalog`, deployer-owned and executable
only by `tagekyc_raw_export_reconciler`.

Exact order:

```text
validate scalar arguments and actor
-> non-locking identity probe only to locate publication/source tuple
-> when publication is absent, require the probe to observe complete Staged or
   return StateConflict before taking a row lock
-> lock existing publication row first, if present
-> lock attempt
-> lock head
-> lock reservation
-> lock ingress claim
-> lock exact key reservation
-> lock exact object
-> prove full R3 immutable binding
-> evaluate exact committed replay
-> require head Staged and exact revisions/fence
-> require complete R3 staged shape
-> require object still VerifiedCompleted with matching immutable evidence
-> require winning key Active
-> acquire the exact shared authority advisory lock
-> invoke the exact consent resolver and retain its shared lock
-> capture one post-blocking clock_timestamp(); no blocking lock follows
-> fresh authority resolver at that timestamp
-> exact frozen identity plus authority/consent/window/deadline revalidation
-> derive commit evidence
-> insert exactly one Committed publication row with PublicationRevision = 1
   and the exact resolved authority schema/id/revision and consent policy
-> return Committed
```

R4 performs no provider or key-provider operation. It does not change head,
attempt, object or key rows. Replays return `ExistingMatch` only if every frozen
binding and commit digest matches; all other occupied shapes are `StateConflict`.
The replay branch precedes fresh authority because it observes an already
committed result. A new commit uses the byte/behavior-identical authority lock,
consent lock and post-lock admission-time discipline already landed for R3.
Authority, consent or strict source-deadline failure returns
`SourceRetentionNotAuthorized`, leaves the head `Staged`, creates no publication
row and performs no provider/key action.

Closed R4 outcomes:

```text
Committed
ExistingMatch
NotFound
StateConflict
SourceRetentionNotAuthorized
```

## 6. R5 exact publication

Add exactly:

```sql
tagekyc.raw_export_publish_available_source(
  p_source_publication_id uuid,
  p_expected_reservation_revision bigint,
  p_expected_fence bigint
)
RETURNS TABLE(
  "Outcome" text,
  "SourcePublicationId" uuid,
  "SourceArtifactId" uuid,
  "OpaqueCommittedLocatorId" uuid,
  "AvailableEvidenceDigest" bytea,
  "ReservationRevision" bigint,
  "Fence" bigint,
  "AvailableAtUtc" timestamptz,
  "CleanupDisposition" text)
```

It has the same owner/search-path/reconciler-only ACL as R4.

Exact order:

```text
validate scalar arguments and actor
-> lock publication
-> lock every winning and non-winning attempt in ascending AttemptId
-> lock head
-> lock reservation, then ingress claim
-> lock every winning and non-winning key reservation in ascending
   AttemptKeyReservationId
-> lock every winning and non-winning object in ascending ObjectCustodyId and
   retain the complete deterministic cleanup census
-> prove every R4/R3 immutable binding
-> evaluate exact Available replay
-> require publication Committed at PublicationRevision 1, head Staged,
   exact revision/fence
-> require winning key Active and winning object VerifiedCompleted
-> acquire exact shared authority advisory lock
-> invoke exact consent resolver and retain its shared lock
-> capture one post-blocking clock_timestamp(); no blocking lock follows
-> fresh authority resolver at that timestamp
-> exact consent identity/state/window revalidation at that timestamp
-> strict authority/source-expiry checks at that timestamp
-> generate one opaque UUID locator
-> derive Available evidence
-> insert deterministic Pending cleanup items from the already locked census
-> update publication in one statement:
   Committed -> Available, PublicationRevision 1 -> 2,
   locator/authority/Available evidence present, and
   CleanupDisposition = Pending when census > 0, otherwise
   CleanupDisposition = NoObsoleteResidue with terminal evidence/time
-> CAS head Staged -> Available and increment revision once
-> return Available
```

The advisory lock domains and post-lock time rule are byte/behavior identical
to R3. Exact replay is observation and precedes fresh authority. A new
publication must pass fresh authority. Authority/consent/deadline failure
returns `SourceRetentionNotAuthorized` with publication still `Committed`, head
still `Staged`, no locator, no cleanup item and no physical provider/key action.
This is the only allowed behavior while purge/hold policy remains unratified.
At no statement boundary can `PublicationState=Available` coexist with
`CleanupDisposition=NotPlanned`. Any failure of item insertion, publication
update or head CAS rolls the entire R5 transaction back.

Cleanup enumeration excludes exactly:

```text
attempt.AttemptId = publication.AttemptId
object.ObjectCustodyId = publication.ObjectCustodyId
key.AttemptKeyReservationId = publication.AttemptKeyReservationId
```

Any attempted inclusion of the winning object/key is a transaction failure,
not a cleanup item.

The non-winning census is also state-exact. An object row is planned only in
`PutInFlight`, `PutOutcomeUnknown`, `ObjectPresentPendingVerification`,
`VerifiedCompleted`, `ObjectConflict`, `CleanupPending`, `Deleted` or
`Quarantined`; `Initiated` and `NoObjectEstablished` prove no established
object residue and create no item. A key row is planned in every disposition
except `ReadyForFreshPreparation` when its landed sparse shape proves no wrapped
key and no current provider resource. Already-terminal rows are still planned
so R6 durably records their exact terminal evidence against this publication.
Operator-required/indeterminate rows remain Pending rather than being silently
omitted or reinterpreted.

Closed R5 outcomes:

```text
Available
ExistingMatch
NotFound
StateConflict
SourceRetentionNotAuthorized
```

## 7. R6 exact obsolete-residue cleanup

R6 has two capability-separated components:

1. reconciliation SQL identifies exact Pending items and exposes no generic
   list or public locator;
2. lifecycle service uses only the landed lifecycle object credential and
   lifecycle key functions to settle one exact item.

It never has decrypt/AEAD/read/HMAC capability. The object path may only use
`DeleteExactAsync` for the exact stored key; no list, batch delete, copy,
presign, multipart or bucket operation. The key path may only drive the landed
exact reservation to a durable terminal disposition. It may not operate on the
winning key.

Add exactly these SQL catalogs:

```sql
tagekyc.raw_export_read_next_source_cleanup_item(
  p_source_publication_id uuid,
  p_expected_publication_revision bigint)
RETURNS TABLE(
  "Outcome" text,
  "SourcePublicationId" uuid,
  "PublicationRevision" bigint,
  "CleanupItemId" uuid,
  "ResourceKind" text,
  "ResourceId" uuid,
  "ResourceAttemptId" uuid,
  "CleanupItemRevision" bigint,
  "PlannedResourceRevision" bigint)

tagekyc.raw_export_complete_source_cleanup_item(
  p_cleanup_item_id uuid,
  p_expected_cleanup_item_revision bigint)
RETURNS TABLE(
  "Outcome" text,
  "SourcePublicationId" uuid,
  "PublicationRevision" bigint,
  "CleanupItemId" uuid,
  "ResourceKind" text,
  "ResourceId" uuid,
  "CompletionDisposition" text,
  "CleanupEvidenceDigest" bytea,
  "CompletedAtUtc" timestamptz,
  "CleanupItemRevision" bigint)

tagekyc.raw_export_finalize_source_cleanup(
  p_source_publication_id uuid,
  p_expected_publication_revision bigint)
RETURNS TABLE(
  "Outcome" text,
  "SourcePublicationId" uuid,
  "PublicationRevision" bigint,
  "CleanupDisposition" text,
  "CleanupEvidenceDigest" bytea,
  "FinalizedAtUtc" timestamptz)
```

The two publication calls compare only
`raw_export_source_publications.PublicationRevision`; the item completion call
compares only `raw_export_source_cleanup_items.RowRevision`. No argument is
interpreted as head reservation, object state or key row revision.

`read_next` returns identity and revision metadata only. It never returns an
object key, bucket, provider identifier, credential or key material. For an
object item, the lifecycle component must call the already-landed
`raw_export_read_provisional_object_lifecycle_context(ResourceId)` and construct
`ExactObjectLocator(ProvisionalObjectIdentity,ObjectKey,ObjectBindingDigest)`
from exactly that typed result. The reconciliation component must instead call
the landed `raw_export_read_provisional_object_reconcile_context(ResourceId)`.
No generic table SELECT or new locator interpretation is allowed. A key item is
addressed only by its exact `AttemptKeyReservationId=ResourceId` through landed
DK-PROD functions.

All three functions are deployer-owned `SECURITY DEFINER` with
`search_path=pg_catalog`. `read_next` is executable only by the existing
reconciler and lifecycle capability roles. `complete` and `finalize` are
executable only by lifecycle. `PUBLIC`, runtime, encryptor and writer have no
EXECUTE; no capability has direct table privilege. The complete function
accepts no outcome token or evidence digest from the caller: it derives the
exact kind-compatible terminal disposition and underlying evidence from the
locked object/key row and terminal event. It requires current resource revision
to be at least `PlannedResourceRevision`, then performs the one-way item
revision `1 -> 2`.

Closed results are:

```text
read:     ItemAvailable | NoPendingItem | NotFound | StateConflict
complete: Completed | ExistingMatch | NotFound | ResourceNotTerminal | StateConflict
finalize: Completed | ExistingMatch | CleanupPending | NotFound | StateConflict
```

Response-loss replay is evaluated before generic stale-revision rejection, but
only against exact durable state. For
`raw_export_complete_source_cleanup_item` the function performs the permitted
non-locking item identity probe, then locks the publication, exact resource,
item and exact terminal event in the global order, revalidates the probe and
applies this precedence:

1. `ExistingMatch` only when the item is already `Completed` at row revision 2,
   the caller supplies the predecessor expected revision 1, and the persisted
   completion disposition/time satisfy the terminal shape and the evidence
   digest recomputes exactly from the locked terminal resource/event plus the
   persisted `CompletedAtUtc`;
2. otherwise require the current item revision to equal the caller's expected
   revision, and admit only `Pending` revision 1 to `Completed` revision 2;
3. any arbitrary stale revision, current-but-nonmatching occupied terminal row,
   mismatched resource/event or altered evidence is `StateConflict` with zero
   mutation.

For `raw_export_finalize_source_cleanup` the function locks the publication and
all cleanup items in the global order, then applies this precedence:

1. `ExistingMatch` only when the publication is already `Available` with
   `CleanupDisposition=Completed` at publication revision 3, the caller supplies
   predecessor expected revision 2, the persisted finalization time satisfies
   the terminal shape, and the cleanup evidence recomputes exactly from the
   complete locked item set plus persisted `FinalizedAtUtc`;
2. an exact `NoObsoleteResidue` terminal publication at revision 2 returns its
   existing terminal result when the caller supplies expected revision 2;
3. otherwise require the current publication revision to equal the caller's
   expected revision, and admit only `Pending` revision 2 to `Completed`
   revision 3;
4. any arbitrary stale revision, current-but-nonmatching occupied terminal row,
   nonterminal item set or altered evidence is `StateConflict` or
   `CleanupPending` as pinned by the closed outcome manifest, with zero terminal
   rewrite.

Moving either exact-replay branch after the generic revision check, accepting a
different predecessor revision, or weakening the evidence rederivation is an
F411 mutation and must RED.

Object completion is admitted only after the landed object row is durably
`Deleted` or `Quarantined` under its existing evidence rules. Key completion is
admitted only after the exact non-winning reservation is durably `Revoked` or
`ReservationAbandoned`. Provider unavailable/unknown and in-progress key
cleanup leave the item Pending. No process-memory result alone completes it.

The exact R6 command and reason matrix is:

| Resource/state | Normative action | Exact reason/evidence authority | R6 item effect |
| --- | --- | --- | --- |
| object `VerifiedCompleted` | derive the landed cleanup digest; call `raw_export_mark_provisional_object_cleanup_required`; read lifecycle context; call only `DeleteExactAsync` | `CleanupReasonCode=SourceConsumed`; landed `tip-88c1-object-cleanup-evidence-v1` preimage | remain Pending until durable terminal row |
| object `CleanupPending` | resume exact delete from landed lifecycle context | retain the already-persisted cleanup reason/digest; never rewrite it | remain Pending until durable terminal row |
| delete returns acknowledged 204 | call `raw_export_record_provisional_object_delete_acknowledged` | landed `tip-88c1-object-delete-ack-evidence-v1` | later complete as `Deleted` |
| delete outcome unknown/unavailable | do not complete; reconciler uses exact inspect; two landed positive-absence observations may call `raw_export_record_provisional_object_absence_confirmed` | landed absence evidence only | Pending; retry or later `Deleted` |
| landed bounded delete failure reaches O-T15 | call `raw_export_record_provisional_object_quarantined` | `QuarantineReasonCode=DeleteOutcomeIndeterminateTerminal`; landed cleanup-quarantine evidence | later complete as `Quarantined` |
| object already `Deleted` or `Quarantined` | no provider call | locked terminal event/digest | complete exact item |
| any other object state | no invented transition | none | `ResourceNotTerminal`, zero item mutation |
| key `Active` | call `raw_export_revoke_attempt_key_reservation` | exact reason `SourceFinalizationSupersededAttempt`; function derives landed revocation evidence | later complete as `Revoked` |
| key in a landed abandon-eligible preparation state | call `raw_export_request_abandon_attempt_key_reservation`; reconciler drives the landed provider resolution/cleanup surfaces; lifecycle calls `raw_export_finalize_abandon_attempt_key_reservation` only after durable cleanup/absence | exact operator reason `SourceFinalizationSupersededAttempt`; landed abandonment evidence chain | Pending, then complete as `ReservationAbandoned` |
| key already `Revoked` or `ReservationAbandoned` | no provider call | locked terminal preparation event/digest | complete exact item |
| key `ProviderCorruptOrUnverifiable` or another state not admitted by the landed revoke/abandon functions | no invented transition | existing operator-required state | `ResourceNotTerminal`, Pending |
| key `ReadyForFreshPreparation` with no wrapped key/current provider resource | exclude from R5 cleanup census as no destroyable key resource | locked sparse shape | no cleanup item |

For the key abandon row, “landed provider resolution/cleanup surfaces” means
the existing typed `IKekProvisioningRecoveryOperation` plus the exact DK-PROD
record/acknowledge functions and their existing role split. R6 neither adds a
provider operation nor treats an in-memory cleanup result as terminal. Changing
either exact `SourceConsumed` or `SourceFinalizationSupersededAttempt` to a
different otherwise-valid reason is an F411 mutation and must RED even when the
eventual terminal state is the same.

After all items are Completed, finalization derives terminal evidence and sets:

```text
CleanupDisposition = Completed
CleanupEvidenceDigest = exact terminal digest
FinalizedAtUtc = one clock_timestamp()
PublicationRevision = 3
```

If R5 found no items it already wrote `NoObsoleteResidue`; exact replays return
that same terminal result at publication revision 2. A Pending publication is
finalized only from expected publication revision 2. Exact predecessor-revision
response-loss replay follows the precedence above; all other stale publication
or item revisions fail `StateConflict` without mutation. Throughout R6:

```text
PublicationState = Available
head.CustodyState = Available
OpaqueCommittedLocatorId unchanged
winning object unchanged
winning key Active and unchanged
```

## 8. C# surface and connection lifecycle

Add internal contracts, repository and three capability-separated services in
the allowlist:

- `RawExportSourceFinalizationService`: R4/R5 PostgreSQL-only reconciliation;
- `RawExportSourceCleanupReconciliationService`: R6 exact-object inspection and
  landed key-provider resolution/cleanup only; it cannot finalize an item;
- `RawExportSourceCleanupService`: R6 lifecycle delete/revoke/abandon and
  item/publication completion only.

The services expose closed enums mirroring the SQL outcomes, validate arguments,
use one explicit connection/transaction per database phase and restore every
actor/write context on success and exception. No service is registered in the
public API composition root in this slice. Tests instantiate the exact
capability topology.

Result `ToString()` methods redact locator, object key, evidence and provider
references. No logs contain raw bytes, plaintext digest, key bytes, S3 secret,
bucket/key locator or wrapped-DEK fields.

## 9. PI-TAG-001 semantic matrices

### 9.1 Invariant Trace Matrix

| ID | Requirement | Owner/order | Surface/outcome | Residue/evidence | Proof / mutation |
| --- | --- | --- | --- | --- | --- |
| F-I01 | only exact staged winner with fresh authority commits | R4 locks/binds, then authority+consent locks and post-lock time | R4 `Committed/ExistingMatch/SourceRetentionNotAuthorized` | one commit row or zero mutation | F401/F402; remove each binding/barrier RED |
| F-I02 | promotion performs no object copy/write | R4 DB-only | architecture boundary | same object id/key | F403; inject provider/copy edge RED |
| F-I03 | only fresh authority commits or publishes | R4/R5 exact replay first; new path advisory locks, post-lock clock, revalidation | commit/Available or retention denied | no row/locator on denial | F402/F404/F405; omit/reorder/old-clock RED |
| F-I04 | only Available is resolver-eligible | publication/head shape | internal descriptor only | Committed non-readable | F406; add committed/read surface RED |
| F-I05 | opaque locator leaks no provider identity | SQL-generated UUID | R5 result | server-side FK mapping | F407; return bucket/key/provider RED |
| F-I06 | exact replay is idempotent | R4/R5 replay before fresh authority; R6 exact durable replay before generic stale CAS | `ExistingMatch` | no second row/revision | F408/F411; reorder, ignore predecessor or weaken rederivation RED |
| F-I07 | cleanup never touches winner | R5 exclusion + R6 guards | `StateConflict` on winner | winning object/key intact | F409; remove each exclusion RED |
| F-I08 | Available survives cleanup lag | R6 after R5 | `Pending` retry | descriptor remains Available | F410; couple publication to cleanup RED |
| F-I09 | cleanup is durable and exact | lifecycle operation then DB evidence | Pending/Completed | append-once item evidence | F411; process-result-only completion RED |
| F-I10 | actor/ACL topology stays separated | migration catalog | reconciler R4/R5; lifecycle R6 | no table privilege | F412; extra/missing grant RED |
| F-I11 | shapes are total and one-way | constraints/triggers | direct DML denied | no partial row | F413; null/sibling/guard mutations RED |
| F-I12 | no raw/plaintext/key leakage | schema/contracts/logs | fail build/test | metadata only | F414; forbidden field/edge RED |
| F-I13 | rollback is exact and occupied-safe | migration Down | fail before destructive DDL | data preserved | F415; remove guard/restore item RED |
| F-I14 | EF/catalog stay synchronized | model/snapshot/E3 | pending-model clean | additive delta | F416; omit/duplicate mapping RED |
| F-I15 | R3 handoff remains exact | R3 -> R4 -> R5 | Available | same staged fingerprint | F417; process-only shortcut RED |
| F-I16 | restart recovers R4/R5/R6 | durable DB/provider/key rows | replay/resume | no process authority | F418; in-memory cache shortcut RED |

### 9.2 Outcome and precedence matrix

| Simultaneous condition | Winning discriminator | Outcome | Mutation residue |
| --- | --- | --- | --- |
| exact R4 replay + authority later lost | replay before authority | `ExistingMatch` | unchanged |
| new R4 commit + authority/consent invalid | post-lock fresh barrier | `SourceRetentionNotAuthorized` | no publication row |
| R4 waits past consent/source expiry | post-lock clock | `SourceRetentionNotAuthorized` | zero mutation |
| stale R4 revision + otherwise valid | CAS before commit | `StateConflict` | none |
| R5 exact Available replay + authority later lost | replay before authority | `ExistingMatch` | unchanged |
| R5 stale fence + authority lost | binding/CAS first | `StateConflict` | none |
| R5 new publication + authority/consent invalid | post-lock fresh barrier | `SourceRetentionNotAuthorized` | Committed only; no locator/items |
| R5 waits past consent/source expiry | post-lock clock | `SourceRetentionNotAuthorized` | zero publication mutation |
| R3 response-loss replay races R4 replay | publication then common attempt-first order | closed replay/commit outcomes; never `40P01` | at most one publication transition |
| R3 response-loss replay races R5 publish | publication then common attempt-first order | closed replay/publish outcomes; never `40P01` | one publication/head transition |
| R4 response-loss replay races R5 publish | publication then common attempt-first order | closed replay/publish outcomes; never `40P01` | one publication/head transition |
| R6 provider unknown + cleanup deadline not modeled | durable uncertainty | `Pending` | no false completion |
| R6 item points at winner | winner guard | `StateConflict` | winner unchanged |
| R6 response lost after item completion | persisted item state | `ExistingMatch` | no second evidence |

### 9.3 Shape/nullability matrix

Section 3 is normative. Tests separately prove column-level `NOT NULL` members
with exact 23502 ownership and shape-required nullable members with exact named
CHECK 23514 ownership; they also toggle every other nullable publication and
cleanup field, duplicate every unique identity and mutate each FK. A row is
either complete Committed or complete Available; cleanup is exactly
NotPlanned, Pending, NoObsoleteResidue or Completed with its pinned fields.

### 9.4 Ordering graph

```text
R3 Staged
-> R4 Committed
-> process loss -> R4 ExistingMatch
-> R5 authority locks -> post-lock time -> Available
-> process loss -> R5 ExistingMatch
-> zero obsolete resources -> NoObsoleteResidue
or
-> Pending items -> lifecycle exact operations -> Completed
```

No blocking lock follows either R4 or R5's admission clock. No external provider
call occurs inside R4/R5 transactions. No database transaction spans an R6
provider call.

### 9.5 Test-bite matrix

| Claim | Positive | Broken mechanism | Expected RED |
| --- | --- | --- | --- |
| exact staged winner + fresh R4 authority | F401 | omit each id/revision/fingerprint/authority predicate or use pre-wait time | F402 |
| metadata-only promote | F401 | call copy/put/delete in R4 | F403 |
| post-lock fresh publication | F404 | pre-wait timestamp, missing lock/window | F405 |
| opaque locator | F406 | expose object key/bucket/provider | F407 |
| replay/concurrency | F408 | fresh authority first, second insert, head-before-attempt or object-before-key lock reversal in any R3/R4/R5 concurrency pair | F408 |
| winner exclusion | F409 | include winning object/key | F409 |
| cleanup lag | F410 | downgrade/unpublish on Pending | F410 |
| durable cleanup | F411 | trust process result, swap either pinned lifecycle reason, terminal token/evidence source or typed locator component | F411 |
| ACL/catalog | F412 | alter an argument/return column, omit one locator component, add runtime/opposite-role/table grant | F412 |
| total shapes/guards | F413 | exact 23502 for column-required NULL; exact named 23514 for each shape-required nullable NULL; each sibling/direct-DML mutation | F413 |
| no secret/raw | F414 | add forbidden member/dependency/log | F414 |
| rollback/model | F415/F416 | remove Down guard or model member | F415/F416 |
| durable handoff/restart | F417/F418 | use process cache/result-only handoff | F417/F418 |

### 9.6 Risk modules

| Module | Disposition |
| --- | --- |
| SQL/schema/transaction | Required: two tables, guards, locks, CAS, ACL, apply/Down/reapply |
| Cryptography/key | Evidence SHA-256 only; R6 invokes existing key lifecycle, never unwraps or reads key material |
| Raw/restricted data | Required: synthetic-only; no plaintext/digest/key/provider-locator leakage |
| Worker/queue | Restart-safe service entry only; no durable queue introduced |
| API | Not applicable; no endpoint/DTO/controller/DI registration |
| Governance/docs | Required: fixture-only boundary, purge/hold deferral, review convergence |

## 10. Exact proof manifest

| ID | Exact test method | Required bite |
| --- | --- | --- |
| F401 | `F401_exact_Staged_winner_with_fresh_authority_commits_once_without_provider_operation` | canonical R4 authority-bound row and unchanged object/head/key |
| F402 | `F402_R4_identity_revision_fence_fingerprint_and_fresh_authority_barriers_are_exact` | each comparator plus revoke/withdraw/expiry/lock-wait-past-expiry independently RED; denial leaves zero publication rows |
| F403 | `F403_R4_R5_have_no_S3_key_provider_AEAD_plaintext_or_external_service_edge` | permits PostgreSQL and pure evidence hash only |
| F404 | `F404_fresh_authority_consent_and_deadlines_publish_exact_Available_shape` | full R5 positive and row readback |
| F405 | `F405_R5_post_lock_admission_time_publication_shape_and_precedence_are_exact` | revoke/withdraw/expiry, both lock-wait-past-expiry cases and direct `Available + NotPlanned` CHECK rejection |
| F406 | `F406_Committed_is_non_readable_and_only_Available_is_descriptor_eligible` | no resolver yet; structural boundary |
| F407 | `F407_opaque_locator_and_results_expose_no_bucket_key_provider_or_secret` | schema/contract/log forbidden census |
| F408 | `F408_R3_R4_R5_response_loss_replays_are_exact_zero_write_and_deadlock_free` | replay after authority loss, changed-command negatives, concurrent R3 replay versus R4, R3 replay versus R5 and R4 replay versus R5; head-before-attempt and object-before-key reversals RED |
| F409 | `F409_R6_cleanup_plan_excludes_winning_object_and_key_under_every_census` | independent object/key exclusion mutations |
| F410 | `F410_Available_remains_stable_while_cleanup_is_pending_or_retried` | provider unavailable/unknown, no unpublish |
| F411 | `F411_R6_completes_only_from_durable_exact_object_and_key_terminal_evidence` | exact command/reason/kind/token/evidence pairs; item probe revalidated after resource-first locking; exact predecessor-revision replay before generic stale rejection; arbitrary stale/current-nonmatching, process-result-only and wrong-resource mutations RED |
| F412 | `F412_function_catalog_owner_ACL_roles_memberships_and_table_privileges_are_exact` | exact argument/return catalog, typed lifecycle/reconcile locator source, missing/extra/effective grant graph |
| F413 | `F413_publication_cleanup_shapes_and_write_guards_are_total_one_way` | column-required NULL -> exact 23502; trigger-disabled shape-required nullable NULL -> exact named CHECK 23514; cross-kind token, `Available+NotPlanned`, sibling/direct-DML and each R5 head-guard comparator mutation |
| F414 | `F414_schema_contracts_services_results_and_logs_contain_no_raw_plaintext_digest_or_key_material` | forbidden surfaces and redaction |
| F415 | `F415_apply_occupied_Down_rejection_teardown_Down_reapply_restores_R3` | occupied-safe rollback and baseline-forward byte/semantic-exact R3 head/core guard restoration; no self-referential capture |
| F416 | `F416_model_snapshot_catalog_and_E3_tripwire_are_synchronized` | pending-model and additive model proof |
| F417 | `F417_R3_Staged_to_R4_Committed_to_R5_Available_preserves_exact_fingerprint` | executable synthetic chain |
| F418 | `F418_restart_after_each_R4_R5_R6_commit_recovers_only_from_durable_state` | no process-memory authority |

Every mutation runs one at a time, must RED at the named assertion for the
assigned reason, restore all bytes, then rerun the canonical proof. An
unreachable mutation is N/A only with a named invariant and executable
reachability evidence; it is never presented as passing coverage.

## 11. Test-impact manifest and execution ladder

### OWNED_TESTS

```text
F401-F418 in Tip88C1B2SourceFinalizationTests
all tests in Tip88C1B2SourceFinalizationArchTests
```

### AFFECTED_REGRESSION_TESTS

```text
Tip88C1B2R3VerifiedCiphertextStagingTests
Tip88C1B2R2DurableCustodyEncryptionTests (handoff fixture only, if invoked transitively)
Tip88C1B2DurableObjectCustodyTests
Tip88C1B2DurableKeyProdTests
Tip88C1B2DurableKeyFixtureProofTests
Tip88B1E3ResolverReadBoundaryTests
all TagEkyc.ArchTests
```

### FULL_SUITE_TRIGGERS

Run the complete unfiltered Release suite when any of these occurs:

- migration/Designer/ModelSnapshot or shared DbContext mapping changes;
- shared guard, role, ACL or resolver behavior changes;
- an owned/affected mutation exposes an unexpected cross-slice failure;
- closeout bundle preparation;
- a reviewed file changes after the closeout full-suite run.

Execution ladder:

1. Edit loop: build the affected project and run only the new/modified named
   proof(s).
2. Checkpoint: run OWNED_TESTS plus the smallest affected predecessor tests.
3. Migration/model checkpoint: pending-model plus E3 and catalog/ACL tests.
4. Closeout: one full Release build and one complete unfiltered Release suite.
5. Commit review may reuse the closeout run only when every reviewed file hash
   is unchanged; otherwise rerun the impacted tier, and rerun full suite if a
   FULL_SUITE_TRIGGER applies.

This replaces repeated full-suite runs after every local edit. It does not
waive the final full suite.

## 12. Exact implementation allowlist for a later controlled build

The later build may touch exactly these 18 paths:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_r4_r6_source_finalization_as_built.md
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourcePublicationRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceCleanupItemRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceHeadRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportSourcePublicationConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportSourceCleanupItemConfig.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260812120000_Tip88C1B2R4R6SourceFinalization.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260812120000_Tip88C1B2R4R6SourceFinalization.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationContracts.cs
src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationEvidenceCodec.cs
src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationRepository.cs
src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationServices.cs
tests/TagEkyc.ArchTests/Tip88C1B2SourceFinalizationArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2SourceFinalizationTests.cs
```

This dispatch is verify-only during implementation and must stay byte-identical
to its ratified SHA. The R3 test file is allowed only for count-neutral handoff
fixture/helper work or an exact R3-to-finalization assertion. The E3 file is
allowed only for the additive snapshot tripwire and existing semantic catalog
round-trip. No landed migration may be edited.

Any nineteenth path, package/project/Program/appsettings/readiness change, role
creation, provider change or ingress-function re-emission is STOP/RRI before
editing.

## 13. Task-0 gates for a later controlled build

Before implementation:

1. Verify exact branch and HEAD above.
2. Record `git status --short`; staged paths must be zero.
3. Prove no `src/` or `tests/` dirt exists; preserve unrelated dirt.
4. Verify every source-of-truth SHA in section 0 and the ratified dispatch SHA.
5. Verify the fixture-only GOV/ART active chain and production gates remain
   unchanged.
6. Record exact hashes for all existing allowlist paths.
7. Run `git diff --check`.
8. Run the affected R3, R2, Durable Object, DK-PROD, DK-FIXTURE, authority,
   consent, ArchTests and E3 baseline set; record census.
9. Run pending-model with Infrastructure as both project and startup project;
   require no pending changes.
10. Apply the full migration chain to isolated PostgreSQL, capture catalog/ACL
    baseline and prove Down/reapply feasibility.
11. Confirm the 18-path allowlist is exact and no provider/package change is
    required.

Any failure is STOP/RRI. Do not repair unrelated dirt or modify landed
migration bytes.

## 14. Validation and closeout

The later build closes only with:

- Release build: zero warnings/errors;
- pending-model clean;
- F401-F418 green;
- every assigned mutation RED for the exact reason and restored;
- exact catalog/owner/ACL/trigger/constraint/index/FK census;
- R3 -> R4 -> R5 executable synthetic flow;
- process restart after R4, after R5 and during R6;
- R6 no-item, provider-unknown, retry, item-complete and response-loss paths;
- winner object/key byte/value identity unchanged;
- occupied-data Down rejection, teardown, Down/reapply and R3 equivalence;
- final affected tests and one complete unfiltered Release suite with zero
  failures under section 11;
- no raw/plaintext/digest/key/provider locator leakage;
- `git diff --check`, staged zero and task-resource residue zero;
- as-built Outcome vs Intent, decision/debt, hashes, validation, mutation and
  non-claim evidence;
- review-only bundle outside the repository with manifest and SHA256SUMS.

The only successful pre-review disposition is:

```text
PASS — R4-R6 SOURCE FINALIZATION READY FOR INDEPENDENT CLOSEOUT REVIEW
```

It is not commit, push, production, real Raw BIO, resolver/assembly, package or
delivery readiness.

## 15. STOP/RRI conditions

STOP/RRI on:

- baseline, source-of-truth or active GOV/ART drift;
- a required nineteenth path or any package/provider/role/Program change;
- a need to copy/move/rewrite the durable object;
- any operation that would delete/revoke the winning object/key;
- inability to represent cleanup durably without process-memory authority;
- inability to keep Available stable while cleanup is pending;
- inability to implement the publication-then-common-attempt-first global lock order without
  `40P01` or an unlisted business outcome;
- any need to invent an R6 lifecycle reason, terminal token, locator source or
  SQL return shape outside section 7;
- any attempt to invent a purge/hold outcome after authority loss;
- stale/pre-wait authority time or a blocking lock after either the R4 or R5
  clock capture;
- new public/runtime resolver, DTO, endpoint or delivery surface;
- raw bytes, bare plaintext digest, unwrapped key or provider credential in
  schema/result/log/evidence;
- pending-model/catalog/ACL/Down-reapply failure;
- any named mutation GREEN, vacuous or RED for the wrong reason;
- test-census drift without explicit Homeowner correction;
- round-5 review without root-cause checkpoint, or round 10 without hard stop.

## 16. PI-TAG-001 activation and convergence record

```text
Pilot TIP: TIP-88C1
Risk: High
Selected modules: SQL/schema/transaction; cryptography/key boundary;
                  raw/restricted data; governance/docs
Required matrices: invariant trace; outcome/precedence; shape/nullability;
                   ordering graph; test-bite
Independent reviewer required: yes
Round-5 checkpoint: enabled
Round-10 hard stop: enabled
Metrics before closeout: required
```

Findings must be classified as `BLOCKER`, `PATCH_REGRESSION`,
`LATENT_SPEC_GAP`, `TEST_HARDENING_ONLY`, `BOOKKEEPING_ONLY` or `DEFERRED`.
After two consecutive review rounds with no blocker, planning review stops and
remaining hardening is deferred unless it invalidates an invariant or executable
path. Review must follow executable control flow, not only textual anchors.

## 17. Final boundary

This review candidate authorizes no action beyond independent document review.
Do not implement, migrate, stage, commit, push, merge, create a PR, deploy,
activate production, store real Raw BIO, expose a resolver, assemble a package
or deliver content from this file alone.
