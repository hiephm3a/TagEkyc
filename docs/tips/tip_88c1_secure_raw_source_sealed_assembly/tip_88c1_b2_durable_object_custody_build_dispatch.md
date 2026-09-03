# TIP-88C1-B2-DURABLE-OBJECT — Bounded Single-Part Conditional Object Custody — BUILD DISPATCH CANDIDATE

**Version:** 0.5

**Status:** DOCS-ONLY FINAL CLOSURE PATCH FOR INDEPENDENT REVIEW — NOT RATIFIED FOR BUILD

**Date:** 2026-08-04

**Repository:** `D:\Task\Remote Signing\TagEkyc`

**Baseline:** `672f3e4eeba24b6082bbb3c0696e597c8821e4c7`

**Branch:** `tip-88a-raw-export-policy-catalog-build`

**Tier:** Tier-0

This redraft implements the Homeowner's 2026-08-04 documentation decision:
the initial `ChipDg2Portrait` and `LiveSelfieImage` slice uses bounded
single-part conditional object custody. Multipart upload, provider versioning,
Object Lock, heavy media, resume, copy/rename, public delivery and Raw BIO are
prohibited. Multipart belongs to a separately authorized heavy-media transport
slice.

This document is not implementation authority. Do not edit code, tests,
migrations or project files from this candidate. Do not stage, commit, push,
merge, create a PR, deploy or activate production. R2 remains suspended until
its own corrected dispatch is ratified. This slice may later prove its object
boundary only with generated ciphertext fixtures; it never reads real Raw BIO.

## 0. Changelog and authority

### v0.5 — two-finding closure patch

- Added `DeleteObjects`, `DeleteObjectsAsync` and
  `Amazon.S3.Model.DeleteObjectsRequest` to the forbidden SDK manifest because
  multi-object delete shares object-delete authority but violates exact-key
  lifecycle semantics.
- Pinned the lifecycle adapter to exactly one `DeleteObjectAsync` request with
  exactly one derived key and added count-neutral O13/OA3 mutations.
- Split the quarantine evidence codec by predecessor: O-T15 uses only
  `CleanupEvidenceDigest`; O-T16 uses only `ProviderReceiptDigest`, with
  separate domains and absolute golden vectors.
- Preserved the v0.4 state/ACL/readiness repairs and the locked single-part
  transport decision.

### v0.4 — independent-review closure patch

- Replaced REST-operation shorthand with the exact forbidden AWSSDK.S3 v3
  sync/async member manifest, including `InitiateMultipartUploadAsync` and
  `CopyPartAsync`.
- Split lifecycle delete acknowledgement from reconciler-confirmed positive
  absence into distinct transitions, functions, ACLs and evidence kinds.
- Added exact lifecycle-configuration posture probing, permission, precedence
  and readiness failure code.
- Pinned outcome/reason vocabularies, canonical evidence domains, independent
  validators and absolute golden-vector obligations; local `NotArmed` no longer
  masquerades as provider `PositiveAbsence`.
- Expanded OA6/O07 so every declared single-put request and stream property has
  a discriminating assertion and mutation.
- Preserved the Homeowner's bounded single-part decision; multipart, versioning
  and Object Lock remain deferred and prohibited.

### v0.3 — bounded single-part redraft

- Replaced the invalid multipart registry, part numbering, upload discovery,
  version-id and Object-Lock contract with one conditional `PutObject`.
- Pinned one durable row per landed `AttemptId`, deterministic object-key
  derivation, guarded state transitions and append-only transition evidence.
- Split writer, reconciler, lifecycle and posture-probe capabilities.
- Pinned lost-response recovery without list operations or blind rewrite.
- Added exact SQL, ACL, readiness, test/mutation and implementation-file
  manifests for independent review.
- Kept GOV/ART fixture execution inactive; this pass changes documentation
  only.

### Authoritative landed anchors

| Anchor | SHA-256 / value |
| --- | --- |
| DK-PROD dispatch | `0D4E5D6E59FF19421108A2BF0F5C659E53896B26473F1952C38BEC9AD6B1C359` |
| DK-PROD as-built | `AAF9E89A20C5BFF09C3A69DE7239BF677FE4665746AFB09AA732A69BCF1F89E7` |
| DK-FIXTURE-PROOF dispatch | `AEFFC7C8DB5FB3F459A60E396575C2330435CC9557E6A9B61D886D216D87859B` |
| DK-FIXTURE-PROOF committed as-built | `8BC73AC4DF7280CE53BE5F6A4FF91AB369660A15D96923F8A3B6A1F2F28CC896` |
| GOV/ART packet v0.4 | `B37D2537793B3D4347D3FB2D8016FC0E6CCF93237CD91F8872A32F6F067A55E5`; draft/inactive |
| Initial source classes | `ChipDg2Portrait`, `LiveSelfieImage` only |
| Per-class plaintext ceiling | mandatory configured value in `[1,67108864]` bytes |
| Current single-part ciphertext ceiling | fixed `134217728` bytes |

The landed source-attempt PK is `AttemptId`. The landed alternate key is
`(AttemptId, AttemptKeyReservationId)`. The landed attempt also freezes
`SourceArtifactId`, `ProvisionalObjectIdentity`,
`EncryptionAttemptRevision`, `Fence` and
`EncryptionAttemptFingerprint`. This slice must consume those values without
reinterpretation.

The landed capability roles are reused and must not be duplicated:

- `tagekyc_raw_export_custody_encryptor`;
- `tagekyc_raw_export_reconciler`;
- `tagekyc_raw_export_lifecycle`.

The matching deployment-owned LOGIN roles remain external prerequisites. This
migration neither creates nor drops those LOGIN roles.

## 1. Scope and non-negotiable invariants

### 1.1 Owned by this slice

- one durable provisional-object custody record per landed source-encryption
  attempt;
- deterministic exact object key derived from the frozen
  `ProvisionalObjectIdentity`;
- a single conditional `PutObject` with `If-None-Match: *`;
- provider outcome recording, lost-response resolution and restart recovery;
- exact-object reconciliation read and lifecycle delete;
- append-only state-transition evidence;
- object-provider configuration, role, ACL and readiness enforcement;
- a pinned S3-compatible adapter and a pinned MinIO integration fixture;
- generated-ciphertext tests and mutation proof.

### 1.2 Consumed, unchanged

- the landed R1 source attempt and key-reservation binding;
- DK-PROD wrapped-key lifecycle and operation-scoped AEAD services;
- the R2 frame and authenticated completion-record contract after R2 is
  separately corrected and ratified;
- the existing three database capability roles and three deployment LOGIN
  identities.

### 1.3 Explicitly not owned

- plaintext acquisition, Raw BIO, R2 encryption, R3 staging or package
  assembly;
- multipart upload, upload sessions, parts, resume or upload enumeration;
- bucket versioning, object versions, delete markers or Object Lock;
- public or runtime download, presigned URL, delivery or receipt;
- arbitrary caller bucket/key selection;
- copy, rename, server-side transform or provider-side encryption claims;
- production provider qualification or production activation.

### 1.4 Load-bearing invariants

1. Exactly one custody row exists for one `AttemptId`.
2. A writer never overwrites an existing key. A conditional conflict is
   reconciled exactly: the same valid object may continue, while a mismatch is
   terminal for that attempt and is never converted to success by deletion.
3. Once the writer has armed a provider request, uncertainty is resolved by
   the reconciler. The writer never retries the same attempt blindly.
4. A complete valid existing object is inspected and continued; it is never
   re-encrypted.
5. An incomplete, invalid, conflicting or possibly written object never
   authorizes reuse of the attempt, DEK, nonce domain or object identity.
6. `VerifiedCompleted` means write-protocol completion, not lifecycle
   deletion. It may transition only to `CleanupPending`.
7. PostgreSQL stores metadata and digests only, never ciphertext or plaintext.
8. The provider adapter never exposes list, multipart, version, copy, rename,
   public ACL or presign operations.
9. `LivenessMedia` and every class outside the exact two-class manifest fail
   before object-provider I/O.
10. No database transaction or lock spans provider/network I/O.

`raw_export_mark_provisional_object_verified` is a narrow future-R2 handoff
seam. In this slice, its positive control may use only a test-only synthetic
verification issuer bound to generated ciphertext. That proves transition/ACL
mechanics, not real payload authenticity. In production, only the separately
ratified R2 reconciler may issue the verification evidence; object readiness
cannot make R2 or Raw BIO ready by itself.

## 2. Exact object state model

### 2.1 States

```text
Initiated
PutInFlight
PutOutcomeUnknown
ObjectPresentPendingVerification
VerifiedCompleted
NoObjectEstablished
ObjectConflict
CleanupPending
Deleted
Quarantined
```

`NoObjectEstablished`, `Deleted` and `Quarantined` are lifecycle terminal.
`ObjectConflict` blocks automated rewrite/delete and can transition only to
`Quarantined`. `VerifiedCompleted` is write-protocol terminal but not lifecycle
terminal. `Quarantined` is terminal for automated application transitions, not
a claim that provider residue was deleted. It creates an explicit operational
debt/alert; the GOV/ART evidence run cannot close until its separate cleanup
authority removes and enumerates the whole fixture bucket to zero residue.

### 2.2 Transition manifest

| ID | Source | Actor | Required evidence | Target | Retry meaning |
| --- | --- | --- | --- | --- | --- |
| O-T01 | row absent | writer | exact active attempt/key binding | `Initiated` | same begin returns existing match |
| O-T02 | `Initiated` | writer | new `PutOperationId`, expected revision | `PutInFlight` | arm once; same id is replay |
| O-T03 | `Initiated` | writer | canonical local `NotArmed` evidence; provider request was never armed | `NoObjectEstablished` | terminal; higher layer may create a replacement attempt |
| O-T04 | `PutInFlight` | writer | provider `Created` response plus exact length/digests | `ObjectPresentPendingVerification` | no second put |
| O-T05 | `PutInFlight` | writer | transport ended without an authoritative provider result | `PutOutcomeUnknown` | reconciler only |
| O-T06 | `PutInFlight` | writer | conditional `409` or `412` result plus receipt digest | `PutOutcomeUnknown` | reconciler distinguishes same valid object from mismatch |
| O-T07 | `PutInFlight` | reconciler | request pump quiesced plus exact-key positive absence | `NoObjectEstablished` | terminal; replacement attempt required |
| O-T08 | `PutInFlight` or `PutOutcomeUnknown` | reconciler | exact-key object matches binding and bounded ciphertext | `ObjectPresentPendingVerification` | continue existing object |
| O-T09 | `PutOutcomeUnknown` | reconciler | request pump quiesced plus exact-key positive absence | `NoObjectEstablished` | replacement attempt required |
| O-T10 | `PutInFlight` or `PutOutcomeUnknown` | reconciler | exact-key object exists but binding/length/format is wrong | `ObjectConflict` | quarantine only |
| O-T11 | `ObjectPresentPendingVerification` | reconciler | authenticated completion and full ciphertext verification | `VerifiedCompleted` | idempotent readback |
| O-T12 | `ObjectPresentPendingVerification` | reconciler | ownership proven but verification/continuation failed | `CleanupPending` | exact delete only |
| O-T13 | `VerifiedCompleted` | lifecycle | typed expiry/consumption/cancellation authorization | `CleanupPending` | exact delete only |
| O-T14A | `CleanupPending` | lifecycle | canonical provider `DeleteAcknowledged` evidence | `Deleted` | terminal |
| O-T14B | `CleanupPending` | reconciler | two exact-key absence observations after an unknown delete response | `Deleted` | terminal |
| O-T15 | `CleanupPending` | lifecycle | bounded delete failure requiring retained evidence | `Quarantined` | terminal/operator follow-up |
| O-T16 | `ObjectConflict` | lifecycle | conflict quarantine reason and inspection evidence digest | `Quarantined` | terminal; foreign/mismatched object is not auto-deleted |

Every transition is a database CAS over `(ObjectCustodyId, expected State,
expected StateRevision)`. A successful transition increments `StateRevision`
exactly once and appends exactly one event in the same transaction. Same-input
replay returns `ExistingMatch` without an event. Stale revision/state returns
`StateConflict` without mutation.

Reconciliation may enter from `PutInFlight` only after the landed attempt
ownership lease has expired and writer entry functions reject the old fence.
Entering from `PutOutcomeUnknown` requires the writer to have quiesced before
recording that state; there is then no further writer transition from that
state. These predicates are part of the SQL/repository preflight and are
mutation-proven. Elapsed wall time alone is not proof of quiescence before the
lease boundary.

### 2.3 Outcome precedence

Every SECURITY DEFINER entry follows this order:

```text
argument shape
-> current actor context
-> row existence
-> immutable attempt/object binding
-> expected revision and source state
-> exact idempotent replay
-> requested transition and sparse evidence shape
-> mutation
```

Expected concurrency/provider outcomes are typed return values. Invalid actor,
identity or evidence shape raises SQLSTATE `P0001` with the exact message from
section 6.5. No function converts a missing row into a create except the begin
function.

## 3. PostgreSQL schema contract

### 3.1 Head table

`tagekyc.raw_export_provisional_objects`:

| Column | Type | Nullability / rule |
| --- | --- | --- |
| `ObjectCustodyId` | uuid | PK, generated by begin |
| `AttemptId` | uuid | required, unique |
| `AttemptKeyReservationId` | uuid | required |
| `SourceArtifactId` | uuid | required |
| `ProvisionalObjectIdentity` | uuid | required, unique |
| `EncryptionAttemptRevision` | bigint | required, `>= 1` |
| `AttemptFence` | bigint | required, `>= 1` |
| `EncryptionAttemptFingerprint` | bytea | required, exactly 32 bytes |
| `ObjectKey` | text | required, unique, exact canonical form |
| `ObjectBindingDigest` | bytea | required, exactly 32 bytes |
| `State` | text | required, exact state manifest |
| `StateRevision` | bigint | required, starts at 1 |
| `PutOperationId` | uuid | nullable until arm |
| `PutArmedAtUtc` | timestamptz | nullable until arm |
| `PutOutcomeKind` | text | nullable; exact typed values only |
| `OutcomeObservedAtUtc` | timestamptz | nullable until a local/provider result or reconciler observation |
| `CiphertextLength` | bigint | nullable; when present in `[1,134217728]` |
| `CiphertextDigest` | bytea | nullable; when present exactly 32 bytes |
| `ProviderReceiptDigest` | bytea | nullable; when present exactly 32 bytes |
| `VerificationEvidenceDigest` | bytea | nullable; when present exactly 32 bytes |
| `VerifiedAtUtc` | timestamptz | nullable |
| `CleanupReasonCode` | text | nullable, max 64 ASCII bytes |
| `CleanupEvidenceDigest` | bytea | nullable; when present exactly 32 bytes |
| `CleanupRequestedAtUtc` | timestamptz | nullable |
| `DeletionEvidenceDigest` | bytea | nullable; when present exactly 32 bytes |
| `DeletionEvidenceKind` | text | nullable; exactly `DeleteAcknowledged` or `PositiveAbsenceConfirmed` |
| `DeletedAtUtc` | timestamptz | nullable |
| `QuarantineReasonCode` | text | nullable, max 64 ASCII bytes |
| `QuarantineEvidenceDigest` | bytea | nullable; when present exactly 32 bytes |
| `QuarantinedAtUtc` | timestamptz | nullable |
| `CreatedAtUtc` | timestamptz | required, database time |
| `UpdatedAtUtc` | timestamptz | required, database time |
| `SchemaVersion` | integer | required, exactly 1 |

The migration adds the alternate key
`(AttemptId, AttemptKeyReservationId, SourceArtifactId,
ProvisionalObjectIdentity)` to the landed attempt table. The head has one
four-column `ON DELETE RESTRICT` FK to that exact key. No caller supplies the
copied frozen values; begin selects them from the attempt row.

Begin locks the exact attempt/source-head rows and requires all of the
following in the same transaction: source head points to the supplied
`AttemptId` and `Fence`; attempt revision/fence equal the supplied expectations;
`R2TerminationDisposition IS NULL`; the ownership lease has not expired; and
the exact landed key reservation has `PreparationDisposition='Active'` through
the existing active-envelope surface. It joins the frozen source reservation
to its ingress claim and requires `RawClass` to be exactly
`ChipDg2Portrait` or `LiveSelfieImage`; it returns that class and the frozen
`ClaimedPlaintextLength` for the C# fixed-cap preflight. Inactive binding fails
`RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE`; a different class fails
`RAW_EXPORT_PROVISIONAL_OBJECT_CLASS_NOT_SUPPORTED`. The transaction commits
before any provider operation starts.

### 3.2 Event table

`tagekyc.raw_export_provisional_object_events`:

| Column | Type | Rule |
| --- | --- | --- |
| `ObjectCustodyEventId` | uuid | PK |
| `ObjectCustodyId` | uuid | FK to head, `ON DELETE RESTRICT` |
| `EventSequence` | bigint | `>= 1`, unique per object |
| `FromState` | text | NULL only for `Created` event |
| `ToState` | text | exact state manifest |
| `ActorKind` | text | `Writer`, `Reconciler` or `Lifecycle` |
| `StateRevision` | bigint | `>= 1`, unique per object |
| `EvidenceDigest` | bytea | exactly 32 bytes |
| `EventAtUtc` | timestamptz | database time |
| `SchemaVersion` | integer | exactly 1 |

Events are append-only. UPDATE, DELETE and TRUNCATE fail even for the table
owner. The head is mutable only through the exact functions in section 6.
`EventSequence` and `StateRevision` both start at 1 and are equal for every
event of an object.

Event evidence is exact: O-T01 uses `ObjectBindingDigest`; O-T02 uses
SHA-256 of the RFC-4122 `PutOperationId`; O-T03 uses canonical `NotArmed`
evidence; O-T04/O-T06 use the provider-result digest; O-T05 uses canonical
`OutcomeUnknown` evidence; O-T07–O-T10 use reconciler observation evidence;
O-T11 uses verification evidence; O-T12/O-T13 use cleanup evidence; O-T14A
uses delete-acknowledgement evidence; O-T14B uses two-observation absence
evidence; and O-T15/O-T16 use quarantine evidence. Section 4.1 pins every
slice-owned digest. No free-text or provider handle is written to the event
table.

### 3.3 Exact catalog names

Constraints:

```text
pk_raw_export_provisional_objects
uq_raw_export_provisional_objects_attempt
uq_raw_export_provisional_objects_identity
uq_raw_export_provisional_objects_key
uq_raw_export_source_attempt_object_binding
fk_raw_export_provisional_objects_attempt_binding
ck_raw_export_provisional_objects_state
ck_raw_export_provisional_objects_values
ck_raw_export_provisional_objects_sparse
pk_raw_export_provisional_object_events
uq_raw_export_provisional_object_events_sequence
uq_raw_export_provisional_object_events_revision
fk_raw_export_provisional_object_events_head
ck_raw_export_provisional_object_events_values
```

Internal functions and triggers:

```text
compute_raw_export_provisional_object_binding
enforce_raw_export_provisional_object_write
enforce_raw_export_provisional_event_append
trg_raw_export_provisional_object_write
trg_raw_export_provisional_event_append
```

Every intended table, column, constraint, function and trigger name must be
UTF-8 byte length `<= 63` and round-trip exactly from the catalog. Static
intended-name arrays and catalog equality are both required.

### 3.4 Sparse row matrix

`X` means required, `-` means NULL. `B` means the binding/identity base columns
are always required.

| State | Put id/armed | Outcome/observed | Cipher length/digest | Provider receipt | Verify evidence/time | Cleanup triple | Delete triple | Quarantine triple |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Initiated` | - | - | - | - | - | - | - | - |
| `PutInFlight` | X | - | - | - | - | - | - | - |
| `PutOutcomeUnknown` | X | X (`OutcomeUnknown` or `ConditionalConflictObserved`) | - | optional only for conditional observation | - | - | - | - |
| `ObjectPresentPendingVerification` | X | X (`Created` or `RecoveredPresent`) | X | X | - | - | - | - |
| `VerifiedCompleted` | X | X | X | X | X | - | - | - |
| `NoObjectEstablished` | both NULL or both X | X (`NotArmed` or `PositiveAbsence`) | - | - | - | - | - | - |
| `ObjectConflict` | X | X (`RecoveredMismatch`) | optional pair as observed | X | - | - | - | - |
| `CleanupPending` | X | X | X | X | optional prior verify | X | - | - |
| `Deleted` | X | X | X | X | optional prior verify | X | X | - |
| `Quarantined` | X | X | optional | X | optional | optional | - | X |

An `ObjectConflict` receipt digest is inspection/conditional-result evidence,
not evidence that the object belongs to this attempt. No row stores provider
receipt text, ETag, credentials, body bytes or decrypted material.

## 4. Canonical identity and object key

`ProvisionalObjectIdentity` comes from the landed attempt. The database derives:

```text
ObjectKey =
  "raw-export/c1/v1/"
  + lower(replace(ProvisionalObjectIdentity::text, "-", ""))
```

The exact result is 49 ASCII bytes. Bucket is configuration-owned and is never
part of the key argument. No SQL or C# public method accepts an arbitrary key.

`ObjectBindingDigest` is computed with `C1HashCanonical`:

```text
domain = "tip-88c1-provisional-object-binding-v1"
fields in order:
  AttemptId (lowercase Guid "N" text)
  AttemptKeyReservationId (lowercase Guid "N" text)
  SourceArtifactId (lowercase Guid "N" text)
  ProvisionalObjectIdentity (lowercase Guid "N" text)
  EncryptionAttemptRevision (invariant decimal text)
  AttemptFence (invariant decimal text)
  EncryptionAttemptFingerprint (lowercase hex text)
  ObjectKey (LP UTF-8 NFC)
```

Every field, including the domain, is encoded by the landed
`C1HashCanonical` rule as `u32 big-endian UTF-8-byte-length || NFC UTF-8
bytes`. The owner-only SQL helper
`compute_raw_export_provisional_object_binding` reproduces that preimage with
schema-qualified `tagekyc_extensions.digest(..., 'sha256')`; begin computes the
digest inside PostgreSQL. C# independently recomputes the same value for the
provider metadata and golden-vector test. No caller-supplied digest is trusted.

The S3 object has only two application metadata values:

```text
x-amz-meta-tagekyc-binding-sha256 = lowercase hex ObjectBindingDigest
x-amz-meta-tagekyc-put-operation-sha256 = lowercase hex SHA-256(PutOperationId RFC-4122 bytes)
```

Raw UUIDs, actor identifiers, session identifiers, subject references and
provider secrets must not appear in provider metadata.

### 4.1 Typed outcomes, reasons and canonical evidence

The persisted `PutOutcomeKind` manifest is exactly:

```text
NotArmed
Created
OutcomeUnknown
ConditionalConflictObserved
RecoveredPresent
PositiveAbsence
RecoveredMismatch
```

`NotArmed` belongs only to O-T03 and is local evidence that no provider call
was armed. `PositiveAbsence` belongs only to O-T07/O-T09 and requires two
provider observations after the request-body pump is quiescent. The two values
must never be substituted for one another.

The exact reason manifests are:

```text
CleanupReasonCode:
  VerificationFailed
  SourceExpired
  SourceConsumed
  SourceCancelled

QuarantineReasonCode:
  DeleteOutcomeIndeterminateTerminal
  ObjectBindingMismatch
  CiphertextMetadataMismatch
  ObjectFormatInvalid
```

O-T12 accepts only `VerificationFailed`. O-T13 accepts only
`SourceExpired`, `SourceConsumed` or `SourceCancelled`. O-T15 accepts only
`DeleteOutcomeIndeterminateTerminal`. O-T16 accepts only the remaining three
quarantine reasons. Unknown, empty, non-ASCII or over-length values fail before
mutation.

`CiphertextDigest` is exactly SHA-256 over the complete object byte stream in
wire order. The writer computes it while consuming the non-seekable input; the
reconciler independently recomputes it while reading the exact object. It is
never a provider ETag/checksum and PostgreSQL never receives the object bytes.

Every other slice-owned evidence digest below uses `C1HashCanonical`: domain
and fields are each encoded as `u32 big-endian UTF-8-byte-length || NFC UTF-8
bytes`, in the stated order. UUIDs use lowercase `N`; integers use invariant
decimal; byte digests use lowercase hex; timestamps use invariant UTC ticks;
the literal `none` represents an absent optional field. The producer computes
the digest in C#, and the named SECURITY DEFINER mutator independently
recomputes and exact-compares it from stored state and typed parameters before
writing. A mismatch fails
`RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID` with zero mutation.

| Evidence | Domain | Ordered fields |
| --- | --- | --- |
| not armed | `tip-88c1-object-not-armed-evidence-v1` | `ObjectBindingDigest`, expected `StateRevision`, literal `NotArmed` |
| put created receipt | `tip-88c1-object-put-created-evidence-v1` | `ObjectBindingDigest`, `PutOperationId`, literal `Created`, provider status `200`, `CiphertextLength`, `CiphertextDigest` |
| conditional conflict receipt | `tip-88c1-object-put-conflict-evidence-v1` | `ObjectBindingDigest`, `PutOperationId`, literal `ConditionalConflictObserved`, provider status `409` or `412` |
| put outcome unknown | `tip-88c1-object-put-unknown-evidence-v1` | `ObjectBindingDigest`, `PutOperationId`, expected `StateRevision`, literal `OutcomeUnknown` |
| reconcile observation | `tip-88c1-object-reconcile-observation-v1` | `ObjectBindingDigest`, `PutOperationId`, resolution kind, first-observation UTC ticks, second-observation UTC ticks or `none`, `CiphertextLength` or `none`, `CiphertextDigest` or `none` |
| cleanup | `tip-88c1-object-cleanup-evidence-v1` | `ObjectBindingDigest`, source state, expected `StateRevision`, `CleanupReasonCode` |
| delete acknowledged | `tip-88c1-object-delete-ack-evidence-v1` | `ObjectBindingDigest`, expected `StateRevision`, literal `DeleteAcknowledged`, provider status `204` |
| positive absence confirmed | `tip-88c1-object-delete-absence-evidence-v1` | `ObjectBindingDigest`, expected `StateRevision`, first-observation UTC ticks, second-observation UTC ticks, literal `PositiveAbsenceConfirmed` |
| cleanup quarantine (O-T15) | `tip-88c1-object-cleanup-quarantine-evidence-v1` | `ObjectBindingDigest`, literal `CleanupPending`, expected `StateRevision`, literal `DeleteOutcomeIndeterminateTerminal`, current stored `CleanupEvidenceDigest` |
| conflict quarantine (O-T16) | `tip-88c1-object-conflict-quarantine-evidence-v1` | `ObjectBindingDigest`, literal `ObjectConflict`, expected `StateRevision`, `QuarantineReasonCode`, current stored `ProviderReceiptDigest` |

The storage mapping is exact. `ProviderReceiptDigest` stores the created or
conditional-conflict digest from the writer, and the reconcile-observation
digest for `RecoveredPresent`/`RecoveredMismatch`; it is NULL for local
`NotArmed`, transport `OutcomeUnknown` and `PositiveAbsence` terminal rows.
`CleanupEvidenceDigest`, `DeletionEvidenceDigest` and
`QuarantineEvidenceDigest` store only their like-named canonical digest.
`DeletionEvidenceKind` identifies which deletion domain produced the stored
digest. The event row receives that same canonical digest for the transition;
no mutator substitutes a different generic receipt digest.

The quarantine supporting digest is source-state-specific and has no fallback:
O-T15 from `CleanupPending` uses `CleanupEvidenceDigest` and must not read
`ProviderReceiptDigest`; O-T16 from `ObjectConflict` uses
`ProviderReceiptDigest` and must not read `CleanupEvidenceDigest`. The SQL
mutator selects the required stored digest itself. The caller supplies neither
source digest nor a selector. Swapping the source or domain fails the separate
O-T15/O-T16 absolute vector and O13 mutation.

For `PositiveAbsence`, both timestamps are required, the second must be later
than the first, and both exact-key observations must be typed provider absence;
for every other resolution the second timestamp is `NULL`. `RecoveredPresent`
requires exact length/digest observations. `RecoveredMismatch` accepts both
length/digest present or both absent: a metadata mismatch may terminate
inspection before a body read, but a partial pair is invalid. `StillUnknown`
requires both absent and returns without mutation. Raw provider
request IDs, ETags, response bodies, exception messages and receipts are never
persisted or included as unconstrained preimage text.

The put-result status argument is required and exactly `200` for `Created`,
`409` or `412` for `ConditionalConflictObserved`, and NULL for
`OutcomeUnknown`. The delete-acknowledgement status is exactly `204`. Any other
status/shape fails before evidence comparison and mutation.

`VerificationEvidenceDigest` remains owned by authenticated R2 completion and
is not redefined here. Until R2 lands, the generated-ciphertext fixture uses
the test-only domain `tip-88c1-object-synthetic-verification-evidence-v1`; that
domain is rejected in production readiness.

Tests must pin one literal golden vector for `ObjectBindingDigest`, raw
`CiphertextDigest`, and every `tip-88c1-object-*-evidence-v1` domain above.
Expected bytes are computed independently outside the production codec and
asserted against both C# and SQL where SQL owns validation. A common-mode
mutation to both implementations must still turn the absolute-vector assertion
RED.

## 5. Provider and application contract

### 5.1 Package and first provider

- Add exactly `AWSSDK.S3` version `3.7.405.4` to
  `TagEkyc.Infrastructure.csproj`.
- The package is an S3-compatible client only; this does not authorize AWS
  hosted infrastructure.
- The first integration target is the pinned image
  `minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e`
  (`RELEASE.2025-09-07T16-13-09Z`).
- `TransferUtility` and every multipart API are forbidden because they can
  select multipart behavior automatically or expose a future transport.

### 5.2 Non-assignable capability interfaces

`IProvisionalObjectWriter` exposes only:

```text
PutIfAbsentAsync(ExactWriteRequest, Stream, CancellationToken)
  -> Created | ConditionalConflict | OutcomeUnknown
```

The request contains an internal exact locator, `PutOperationId`, binding
metadata, exact `CiphertextLength` and no caller bucket/key. The implementation
sets `PutObjectRequest.IfNoneMatch = "*"`, `ContentLength` to the exact length,
sets `AutoResetStreamPosition=false`, configures the dedicated writer client
with `MaxErrorRetry=0`, sets `AutoCloseStream=false`, wraps the body as
non-seekable, and performs one `PutObjectAsync` call. SDK retry must not replay
the body; uncertainty is owned by reconciliation.

`IProvisionalObjectReconciler` exposes only:

```text
InspectExactAsync(ExactObjectLocator, CancellationToken)
  -> Present | PositivelyAbsent | Indeterminate | ProviderUnavailable

OpenExactReadAsync(ExactObjectLocator, CancellationToken)
  -> bounded ciphertext stream or typed failure
```

It cannot put, overwrite, delete, list, presign, copy or rename.

`IProvisionalObjectLifecycle` exposes only:

```text
DeleteExactAsync(ExactObjectLocator, CancellationToken)
  -> DeletedAcknowledged | OutcomeUnknown | ProviderUnavailable
```

It cannot read, put, list, copy, rename or presign. The reconciler performs any
post-delete absence confirmation; S3 does not provide a distinct IAM action for
HEAD versus GET, so lifecycle is deliberately not granted either.

The S3 lifecycle adapter performs exactly one `DeleteObjectAsync` call using a
`DeleteObjectRequest` with exactly the internally derived bucket and one exact
`ObjectKey`. It has no collection-of-keys request surface and never calls
`DeleteObjects`/`DeleteObjectsAsync`. This is enforced in code because the
single-object and multi-object operations rely on the same object-delete
permission and cannot be separated by that permission alone.

`IProvisionalObjectPostureProbe` is readiness-only and exposes bucket posture
inspection. It has no object read/write/delete/list capability and is not
resolvable by runtime object services. Its S3 adapter may call only
`GetBucketVersioningAsync`, `GetObjectLockConfigurationAsync`, `GetACLAsync`,
`GetBucketPolicyAsync` and `GetLifecycleConfigurationAsync`;
missing-policy, missing-object-lock and `NoSuchLifecycleConfiguration`
responses have exact typed interpretations, while unsupported/indeterminate
responses fail readiness. An existing lifecycle configuration is accepted only
when its rule collection is empty; any enabled or disabled rule fails closed.

Architecture tests must prove the four interfaces are not assignable to one
another, no implementation class implements more than one, and no service
constructor receives a broader S3 client or another capability.

Production uses one capability per deployment instance, not four clients in
one container. The same binary may start in exactly one of these modes:

| Deployment mode | DB LOGIN/capability | Provider credential | Resolvable interface |
| --- | --- | --- | --- |
| `Writer` | encryptor LOGIN / `tagekyc_raw_export_custody_encryptor` | conditional-put policy | writer only |
| `Reconciler` | reconciler LOGIN / `tagekyc_raw_export_reconciler` | derived-prefix GetObject policy | reconciler only |
| `Lifecycle` | lifecycle LOGIN / `tagekyc_raw_export_lifecycle` | derived-prefix DeleteObject policy | lifecycle only |
| `PostureProbe` | no object-operation DB capability | bucket-posture-read policy | posture probe only |

Startup fails if configuration for another capability is present or if more
than one object capability is resolvable. Tests use four isolated service
providers. The public API/runtime process contains no reconciler, lifecycle or
posture credential.

### 5.3 Single-part behavior

- The stream is read once and is not seek/retry buffered by the adapter.
- `CiphertextLength` must be known before provider I/O, positive, and at most
  `134217728` bytes.
- The adapter counts consumed bytes independently. After a success response it
  requires the count to equal the declaration and performs one bounded EOF
  probe on the still-open stream. A short read, extra byte or indeterminate EOF
  maps to `OutcomeUnknown` because a provider object may already exist; only
  reconciliation may accept or clean it.
- A transport exception after the request is armed maps to `OutcomeUnknown`,
  not `ProviderUnavailable` and never to absence.
- `409` or `412` from conditional create maps to a conditional-conflict
  observation and durable `PutOutcomeUnknown`. It is not yet an
  `ObjectConflict`; exact reconciliation may prove it is the same valid object.
- ETag and provider checksum values are opaque transport receipts. They are not
  object identity, ciphertext authenticity or a substitute for R2 AEAD.
- Writer cancellation must quiesce the request-body pump before absence can be
  recorded. Abrupt process loss cannot claim active zeroization or immediate
  absence.

The exact call order is: begin and commit; perform all local argument/client/
stream validation; if that fails, record `NoObjectEstablished` through the
not-armed function; arm and commit; then issue the one provider call. After arm,
every exception, cancellation, timeout, DNS/TLS failure or unclassified status
is `OutcomeUnknown` unless the provider returned the exact conditional-conflict
status. `ProviderUnavailable` is a readiness/reconciliation observation, not a
writer proof of absence.

### 5.4 Forbidden SDK operations

The implementation and dependency graph must contain no call/reference to the
following exact AWSSDK.S3 v3 members/types (both synchronous and asynchronous
forms are named where the package exposes both):

```text
InitiateMultipartUpload
InitiateMultipartUploadAsync
UploadPart
UploadPartAsync
CopyPart
CopyPartAsync
CompleteMultipartUpload
CompleteMultipartUploadAsync
AbortMultipartUpload
AbortMultipartUploadAsync
ListMultipartUploads
ListMultipartUploadsAsync
ListParts
ListPartsAsync
ListObjects
ListObjectsAsync
ListObjectsV2
ListObjectsV2Async
PutBucketVersioning
PutBucketVersioningAsync
PutObjectLockConfiguration
PutObjectLockConfigurationAsync
CopyObject
CopyObjectAsync
DeleteObject
DeleteObjects
DeleteObjectsAsync
Amazon.S3.Model.DeleteObjectsRequest
TransferUtility
GetPreSignedURL
```

`DeleteObjectAsync(DeleteObjectRequest, CancellationToken)` is the sole
allowed deletion member and only inside the lifecycle adapter. The synchronous
`DeleteObject` member is forbidden alongside every batch-delete member/type.

The provider credentials may technically target an S3-compatible API where
multipart exists, because S3 IAM does not reliably separate single PutObject
from multipart use. Therefore the current prohibition is enforced by the
narrow adapter, DI graph, code-level forbidden-edge tests and deployment
egress policy. It is not falsely claimed as an IAM-only guarantee.

### 5.5 Primary feasibility references

- AWS SDK for .NET v3 `PutObjectRequest`, including `IfNoneMatch` and stream
  properties:
  <https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/S3/TPutObjectRequest.html>.
- AWS SDK for .NET v3 `IAmazonS3` exact member manifest, including
  `InitiateMultipartUploadAsync`, `CopyPartAsync` and
  `GetLifecycleConfigurationAsync`:
  <https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/S3/TIS3.html>.
- AWS SDK for .NET v3 multi-object delete request, retained only to pin its
  prohibition:
  <https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/S3/TDeleteObjectsRequest.html>.
- S3 conditional-write behavior and `409`/`412` races:
  <https://docs.aws.amazon.com/AmazonS3/latest/userguide/conditional-writes-enforce.html>.
- Multipart limits, retained only as evidence for deferral:
  <https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html>.
- Exact SDK package candidate:
  <https://www.nuget.org/packages/AWSSDK.S3/3.7.405.4>.

These references establish API feasibility only. MinIO compatibility and the
exact pinned adapter behavior still require the named integration and mutation
proofs; AWS documentation is not evidence of a working MinIO deployment.

## 6. SQL functions, guards and ACL

### 6.1 Public SECURITY DEFINER manifest

All functions live in `tagekyc`, are owned by
`tagekyc_raw_export_deployer`, use `SET search_path = pg_catalog`, call
`tagekyc.raw_export_current_actor()` for actor context, use database UTC time,
and restore the local write-context GUC on success and exception.

Every writer mutator rechecks that the landed source head still names the same
attempt/fence, the attempt is unterminated, and
`OwnershipLeaseExpiresAtUtc > statement_timestamp()`. A late provider response
after lease expiry cannot mutate; reconciliation resolves the exact key after
the lease boundary. Reconciler/lifecycle functions never extend the attempt
lease and cannot allocate a replacement attempt.

```text
raw_export_begin_provisional_object_custody(
  p_attempt_id uuid,
  p_expected_encryption_attempt_revision bigint,
  p_expected_fence bigint,
  p_maximum_provisional_objects_per_source integer)

raw_export_arm_provisional_object_put(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_put_operation_id uuid)

raw_export_record_provisional_object_not_armed(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_local_termination_evidence_digest bytea)

raw_export_record_provisional_object_put_result(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_put_operation_id uuid,
  p_result_kind text,
  p_provider_status_code integer,
  p_ciphertext_length bigint,
  p_ciphertext_digest bytea,
  p_provider_receipt_digest bytea)

raw_export_resolve_provisional_object_put_outcome(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_resolution_kind text,
  p_ciphertext_length bigint,
  p_ciphertext_digest bytea,
  p_first_observed_at_utc timestamptz,
  p_second_observed_at_utc timestamptz,
  p_observation_evidence_digest bytea)

raw_export_mark_provisional_object_verified(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_verification_evidence_digest bytea)

raw_export_mark_provisional_object_cleanup_required(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_cleanup_reason_code text,
  p_cleanup_evidence_digest bytea)

raw_export_record_provisional_object_delete_acknowledged(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_provider_status_code integer,
  p_deletion_evidence_digest bytea)

raw_export_record_provisional_object_absence_confirmed(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_first_absence_observed_at_utc timestamptz,
  p_second_absence_observed_at_utc timestamptz,
  p_deletion_evidence_digest bytea)

raw_export_record_provisional_object_quarantined(
  p_object_custody_id uuid,
  p_expected_state_revision bigint,
  p_quarantine_reason_code text,
  p_quarantine_evidence_digest bytea)

raw_export_read_provisional_object_reconcile_context(
  p_object_custody_id uuid)

raw_export_read_provisional_object_lifecycle_context(
  p_object_custody_id uuid)
```

Every mutator returns exactly:

```text
OutcomeCode text
ObjectCustodyId uuid
ObjectState text
StateRevision bigint
```

Begin additionally returns `ObjectKey text`,
`ProvisionalObjectIdentity uuid`, `ObjectBindingDigest bytea` and the exact
frozen attempt projection, including `RawClass text` and
`ClaimedPlaintextLength bigint`, `OwnershipLeaseExpiresAtUtc`,
`EffectivePlaintextRetentionExpiresAtUtc` and `ReservationExpiresAtUtc` from
the immutable attempt/claim/reservation chain, plus one database
`ProjectionAtUtc` used by the deadline formula.

The reconcile read returns exactly:

```text
ObjectCustodyId, AttemptId, AttemptKeyReservationId, SourceArtifactId,
ProvisionalObjectIdentity, EncryptionAttemptRevision, AttemptFence,
EncryptionAttemptFingerprint, ObjectKey, ObjectBindingDigest, State,
StateRevision, PutOperationId, PutArmedAtUtc, CiphertextLength,
CiphertextDigest, ProviderReceiptDigest, VerificationEvidenceDigest
```

The lifecycle read returns exactly:

```text
ObjectCustodyId, AttemptId, ProvisionalObjectIdentity, ObjectKey,
ObjectBindingDigest, State, StateRevision, PutOperationId,
CleanupReasonCode, CleanupEvidenceDigest, CleanupRequestedAtUtc,
ProviderReceiptDigest
```

Neither read returns credentials, key material, provider receipt text or body
bytes. Reconciliation obtains the active wrapped-key envelope only through the
landed `raw_export_read_active_attempt_key_envelope(uuid)` function.

The verified mutator does not validate arbitrary caller prose. Its 32-byte
digest is accepted only from the reconciler DB capability. Until corrected R2
lands, the only registered issuer is the isolated generated-ciphertext test
issuer and production aggregate readiness remains RED.

The owner-only helper has the exact signature:

```text
compute_raw_export_provisional_object_binding(
  uuid, uuid, uuid, uuid, bigint, bigint, bytea, text)
RETURNS bytea
```

It is not granted to any non-owner role.

### 6.2 Exact grants

| Function group | Encryptor | Reconciler | Lifecycle | runtime/PUBLIC |
| --- | --- | --- | --- | --- |
| begin, arm, record not-armed, record put result | EXECUTE | none | none | none |
| resolve put outcome, mark verified, reconcile read | none | EXECUTE | none | none |
| mark cleanup required | none | EXECUTE | EXECUTE | none |
| record delete acknowledged | none | none | EXECUTE | none |
| record absence confirmed | none | EXECUTE | none | none |
| record quarantined, lifecycle read | none | none | EXECUTE | none |

All overloads are included in the ACL manifest. No extra grantee, alternate
grantor, PUBLIC privilege, direct table privilege or column-level ACL is
allowed. The two internal guard functions have zero non-owner ACL rows.

The existing capability membership graph stays exactly three LOGIN-to-role
edges with `ADMIN=false`, `INHERIT=true`, `SET=false`. The migration adds no
role or membership.

### 6.3 Trigger write contexts

One transaction-local GUC is used:

```text
tagekyc.raw_export_provisional_object_write_context
```

Allowed tokens:

```text
tip88c1-object-head-write-v1
tip88c1-object-event-append-v1
```

Head INSERT/UPDATE is allowed only while the matching token is set by an exact
entry function. DELETE/TRUNCATE is always rejected. Event INSERT is allowed
only with the event token; UPDATE/DELETE/TRUNCATE is always rejected. Direct
GUC spoofing cannot grant access because capability roles have no table DML.

### 6.4 Typed outcome tokens

```text
Created
ExistingMatch
Armed
NotArmed
Recorded
RecoveredPresent
PositiveAbsence
ConditionalConflict
OutcomeUnknown
Verified
CleanupRequired
Deleted
Quarantined
StateConflict
NotFound
```

No token outside this manifest may be returned.

`raw_export_record_provisional_object_put_result` accepts only input kinds
`Created`, `ConditionalConflictObserved` and `OutcomeUnknown` from
`PutInFlight`.
`raw_export_resolve_provisional_object_put_outcome` accepts only
`RecoveredPresent`, `PositiveAbsence`, `RecoveredMismatch` and `StillUnknown`
from `PutInFlight` or `PutOutcomeUnknown`; `StillUnknown` returns
`OutcomeUnknown` without mutation. Each input kind has the exact sparse
argument shape in section 3.4. Unknown text fails before any mutation.

| Function | Success/replay return tokens |
| --- | --- |
| begin | `Created`, `ExistingMatch` |
| arm | `Armed`, `ExistingMatch`, `StateConflict`, `NotFound` |
| record not-armed | `NotArmed`, `ExistingMatch`, `StateConflict`, `NotFound` |
| record put result | `Recorded`, `OutcomeUnknown`, `ExistingMatch`, `StateConflict`, `NotFound` |
| resolve put outcome | `RecoveredPresent`, `PositiveAbsence`, `ConditionalConflict`, `OutcomeUnknown`, `ExistingMatch`, `StateConflict`, `NotFound` |
| mark verified | `Verified`, `ExistingMatch`, `StateConflict`, `NotFound` |
| mark cleanup required | `CleanupRequired`, `ExistingMatch`, `StateConflict`, `NotFound` |
| record delete acknowledged | `Deleted`, `ExistingMatch`, `StateConflict`, `NotFound` |
| record absence confirmed | `Deleted`, `ExistingMatch`, `StateConflict`, `NotFound` |
| record quarantined | `Quarantined`, `ExistingMatch`, `StateConflict`, `NotFound` |

### 6.5 Exact failure messages

All use SQLSTATE `P0001`:

```text
RAW_EXPORT_ACTOR_CONTEXT_MISSING
RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID
RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE
RAW_EXPORT_PROVISIONAL_OBJECT_CLASS_NOT_SUPPORTED
RAW_EXPORT_PROVISIONAL_OBJECT_BINDING_INVALID
RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID
RAW_EXPORT_PROVISIONAL_OBJECT_DIRECT_MUTATION_FORBIDDEN
RAW_EXPORT_PROVISIONAL_OBJECT_EVENT_APPEND_FORBIDDEN
RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED
```

The application maps provider/network conditions separately and never exposes
provider handles or key existence in a public error.

## 7. Recovery and crash traces

### R-01 — response lost after successful conditional put

```text
DB PutInFlight
-> provider stores complete object
-> writer loses response / process exits
-> DB records or remains PutOutcomeUnknown
-> fresh reconciler instance inspects exact deterministic key
-> binding metadata matches
-> bounded exact read verifies ciphertext/completion
-> RecoveredPresent
-> VerifiedCompleted
```

No second put occurs.

### R-02 — response lost and provider has no object

```text
DB PutInFlight or PutOutcomeUnknown
-> writer process/request-body pump is proven quiesced
-> reconciler exact HEAD observes positive absence
-> second exact observation after bounded backoff also observes absence
-> NoObjectEstablished
```

The same attempt remains terminal. A higher-layer current-owner flow must
revalidate authority/consent/retention and create a replacement attempt with a
new attempt, fence, key reservation, DEK, nonce domain and object identity.

### R-03 — conditional conflict or mismatched exact-key object

```text
PutObject returns 409/412
-> PutOutcomeUnknown
-> reconciler inspects the exact key
-> same binding and valid object: RecoveredPresent
-> mismatched binding/body: ObjectConflict
-> no overwrite, no auto-delete, no trust in ETag
-> lifecycle records Quarantined with typed evidence
```

### R-04 — delete response lost

```text
CleanupPending
-> lifecycle issues exact delete
-> response unknown
-> state remains CleanupPending
-> reconciler performs two exact-key absence observations
-> reconciler calls only raw_export_record_provisional_object_absence_confirmed
-> Deleted with DeletionEvidenceKind=PositiveAbsenceConfirmed
```

No list operation or version/delete-marker claim is involved because the
bucket is required to be never-versioned.

When lifecycle receives an authoritative provider acknowledgement instead,
only lifecycle calls
`raw_export_record_provisional_object_delete_acknowledged`, producing
`DeletionEvidenceKind=DeleteAcknowledged`. Lifecycle cannot claim positive
absence; reconciler cannot convert a delete acknowledgement. The two functions,
ACLs, canonical evidence domains and event digests are deliberately distinct.

## 8. Configuration and readiness

### 8.1 Exact configuration keys

Prefix: `TagEkyc:RawExport:ObjectCustody`.

```text
Topology = Disabled | S3CompatibleDurable
Capability = Writer | Reconciler | Lifecycle | PostureProbe
ServiceUrl
BucketName
AccessKeyId
SecretAccessKey
AllowLoopbackHttp = false in production; true only in named fixture
MaximumSinglePartCiphertextBytes = 134217728 exactly
OperationTimeoutSeconds = 300 exactly
```

There are no implicit credentials, endpoint, bucket, cap or timeout defaults.
Secrets must be supplied by the deployment secret source and must never be
written to repository files, logs, diagnostics or readiness payloads.

For writer operation `W`, the cancellation deadline is frozen before arm:

```text
W = min(
  database projection time + 300 seconds,
  OwnershipLeaseExpiresAtUtc,
  EffectivePlaintextRetentionExpiresAtUtc,
  ReservationExpiresAtUtc)
```

If `W` cannot fit the local validation, arm and one bounded write, the attempt
is not armed and ends through the not-armed path. No timeout/config reload can
extend `W` after begin.

### 8.2 Readiness precedence and codes

Readiness evaluates in this order and returns the first exact code:

| Order | Failure | Code |
| --- | --- | --- |
| 1 | topology missing/unknown | `PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID` |
| 2 | fixed cap/timeout absent, malformed or different | `PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID` |
| 3 | endpoint/bucket syntax, AWS fallback, redirect or TLS posture invalid | `PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID` |
| 4 | capability/credential absent, multiple-capability material present or ambient credential source enabled | `PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID` |
| 5 | database role, membership, owner or ACL manifest mismatch | `PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID` |
| 6 | bucket missing or inaccessible to posture identity | `PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE` |
| 7 | versioning is enabled/suspended/indeterminate | `PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED` |
| 8 | Object Lock configured/indeterminate | `PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED` |
| 9 | lifecycle configuration is present with any rule, unreadable or indeterminate | `PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED` |
| 10 | public ACL/policy or indeterminate access posture | `PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED` |
| 11 | capability positive/negative probes mismatch | `PROD_RAW_EXPORT_OBJECT_CAPABILITY_INVALID` |
| 12 | GOV/ART authority absent for the selected fixture/production mode | `PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE` |
| 13 | controlled qualification-mode conditional-create/exact-read/delete proof fails | `PROD_RAW_EXPORT_OBJECT_OPERATION_UNAVAILABLE` |
| 14 | production mode lacks the ratified R2 verification issuer/readiness | `PROD_RAW_EXPORT_OBJECT_R2_DEPENDENCY_UNAVAILABLE` |

`Disabled` is healthy only when no object-custody service is activated. It does
not satisfy R2 readiness. `S3CompatibleDurable` requires the checks applicable
to the selected capability. One instance never proves another capability
healthy. This proof-build does not implement a production activation-record
schema or parser: production mode always stops at
`PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE` until a separately authorized Gate B
slice adds that contract.

Ordinary process startup never writes a readiness object. Operational
capabilities are exercised only by a bounded qualification run using four
isolated service providers plus the GOV/ART cleanup identity. That run performs
conditional create, same-object read, negative cross-capability probes, exact
delete, positive absence and whole-bucket zero-residue enumeration, then emits
the fixture evidence governed by Gate A. The test harness consumes that
evidence directly; application readiness does not parse it. Gate A may qualify
only the fixture, and production stays RED until a separately ratified Gate B
record and consumer exist.

The endpoint must be an explicitly configured HTTPS origin. HTTP is accepted
only for a loopback MinIO fixture with `AllowLoopbackHttp=true`. Redirects to a
different origin, certificate bypass, AWS default endpoint fallback, IMDS,
environment/profile credential discovery and anonymous credentials are
forbidden. `ForcePathStyle=true` is pinned for the MinIO adapter.

### 8.3 Bucket posture

- fresh dedicated bucket/prefix; no shared application bucket;
- versioning response must prove never-enabled, not `Suspended`;
- Object Lock must be absent/disabled;
- `GetLifecycleConfigurationAsync` must return the typed
  `NoSuchLifecycleConfiguration` absence or an exact empty rule collection;
  any enabled or disabled rule is prohibited;
- no public bucket ACL or public bucket policy;
- no lifecycle that silently retains versions or incomplete multipart state;
- writer: conditional PutObject only;
- reconciler: GetObject/HeadObject only for the exact prefix;
- lifecycle: exactly one `DeleteObjectAsync` request for one derived exact key;
  `DeleteObjects`/`DeleteObjectsAsync` and every multi-key request type are
  forbidden even though the credential's object-delete permission cannot
  distinguish them;
- posture: bucket configuration reads only, including exact
  `s3:GetLifecycleConfiguration` and no bucket/object mutation permission;
- no identity has `ListBucket` in the application topology.

The GOV/ART fixture harness may use a separate ephemeral evidence/cleanup
administrator to enumerate and delete the whole fresh fixture bucket. That
identity is outside application DI, exists only for the bounded evidence run,
is governed by the GOV/ART packet and is destroyed with the fixture. It is not
a fifth production application capability and must never be used by writer,
reconciler, lifecycle or readiness services.

GOV/ART packet v0.4 remains draft/inactive. Its execution record must be
ratified separately before a MinIO evidence run. This dispatch must not
silently activate that packet.

## 9. Bounded-size and capacity rules

- `RawExportSourceMaximumChipDg2PortraitBytes` and
  `RawExportSourceMaximumLiveSelfieImageBytes` remain mandatory and each must be
  in `[1,67108864]`.
- `MaximumSinglePartCiphertextBytes` is fixed at `134217728` for this profile.
- R2 integration must prove its exact object-length projection is positive,
  equals bytes written and is `<= 134217728` before opening a provider request.
- `RawExportSourceMaximumProvisionalObjectsPerSource` remains mandatory in
  `[1,64]` and `>= RawExportSourceMaximumActiveAttemptsPerSource`.
- Begin counts non-terminal provisional-object rows for the exact
  `SourceArtifactId` while holding the existing source admission lock.
- Begin validates `p_maximum_provisional_objects_per_source` in `[1,64]` and
  atomically enforces the readiness-validated deployment value. PostgreSQL's
  unconditional hard ceiling remains 64 even if a capability caller is faulty;
  the exact lower deployment cap is part of the trusted writer configuration
  and named mutation proof.
- Checked arithmetic is mandatory; overflow fails closed.
- Object bytes never enter PostgreSQL, application logs, metrics or exceptions.

The fixed 128 MiB ciphertext cap is not permission to raise either 64 MiB
plaintext class ceiling. Any future larger class or multipart transport needs
a new ratified profile and cannot reuse this dispatch.

## 10. Migration and rollback

Migration identity is pinned:

```text
20260804120000_Tip88C1B2DurableObjectCustody
```

`Up()` must:

1. add the exact attempt alternate key;
2. create the head and event tables, constraints and indexes;
3. create the binding codec helper, internal guards and triggers;
4. create the twelve public SECURITY DEFINER functions;
5. revoke PUBLIC and all non-owner privileges;
6. grant the exact function manifest to the three existing capability roles;
7. assert owner, role attributes, memberships, table/column ACL and function
   ACL in a fail-closed migration DO block.

`Down()` must revoke only this slice's function grants, drop this slice's
functions/triggers/tables and remove only the added attempt alternate key. It
must preserve the three LOGIN roles, three capability roles, every landed
membership and every landed DK-PROD/DK-FIXTURE object. Apply/Down/reapply must
restore the exact pre-Up catalog and ACL before reapplying.

No landed migration byte may change.

## 11. Exact test and mutation manifest

### 11.1 Integration tests

File:
`tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs`.

| ID | Exact method | Scratch mutation that must make it RED | Discriminating assertion |
| --- | --- | --- | --- |
| O01 | `O01_intended_identifiers_round_trip_exactly_and_fit_63_bytes` | add one 64-byte intended function and matching DDL | static byte-length and exact catalog equality both fail |
| O02 | `O02_head_event_sparse_and_typed_value_manifests_are_exact` | remove one sparse predicate or admit one unknown outcome/reason/evidence-kind token | invalid row inserts unexpectedly; local `NotArmed` and provider `PositiveAbsence` controls remain distinct |
| O03 | `O03_one_attempt_can_create_only_one_object_custody_row` | drop attempt unique constraint; controlled owner/GUC insert bypasses begin's friendly replay check | second row for same attempt succeeds |
| O04 | `O04_object_binding_composite_fk_rejects_cross_attempt_values` | drop the four-column binding FK; controlled owner/GUC insert targets the exact FK | mismatched object row succeeds; assert exact constraint name in control |
| O05 | `O05_begin_derives_key_binding_and_absolute_digest_vectors` | accept a caller key, change canonical prefix or common-mode mutate one C#/SQL canonical preimage | exact key/readback or independently pinned absolute digest differs |
| O06 | `O06_writer_arm_is_revision_fenced_and_idempotent` | remove expected-revision predicate | stale arm mutates row |
| O07 | `O07_created_put_records_exact_bounded_ciphertext_and_canonical_evidence` | remove fixed size/count/EOF predicate, mutate raw-byte SHA-256, or mutate one created/unknown receipt preimage | oversized/short/extra body or wrong absolute digest is accepted |
| O08 | `O08_lost_success_response_recovers_the_existing_exact_object` | return absence before exact inspect | recovered object is misclassified and assertion fails |
| O09 | `O09_absence_requires_not_armed_or_quiesced_two_exact_observations` | conflate `NotArmed` with `PositiveAbsence`, remove quiescence/second-observation guard or mutate observation preimage one at a time | early/unsupported absence becomes terminal or absolute digest differs |
| O10 | `O10_conditional_conflict_is_reconciled_without_overwrite_or_auto_delete` | omit `IfNoneMatch="*"` | original sentinel object changes; same valid object and mismatched object controls discriminate |
| O11 | `O11_mismatched_existing_object_becomes_quarantined` | route mismatch to present/verified | mismatch is accepted or deleted |
| O12 | `O12_verified_completion_requires_authenticated_verification_evidence` | allow NULL verification digest | verified row exists without evidence |
| O13 | `O13_cleanup_delete_ack_absence_and_quarantine_evidence_are_exact` | grant/call each deletion function from the opposite role; issue a second/batch delete; remove one evidence-kind guard; mark Deleted directly on unknown response; or swap O-T15 cleanup evidence with O-T16 provider evidence one at a time | wrong actor/request/evidence path succeeds, Deleted appears early, or the branch-specific absolute vector differs |
| O14 | `O14_head_guard_and_event_append_only_rules_bite_as_owner` | disable each trigger in turn | owner UPDATE/DELETE succeeds |
| O15 | `O15_role_acl_and_membership_manifests_are_exact` | grant one function to runtime and one table SELECT to reconciler | exact ACL comparison fails |
| O16 | `O16_minio_restart_preserves_conditional_object_and_recovery` | restart with a fresh empty volume | exact object cannot be recovered |
| O17 | `O17_bucket_versioning_object_lock_lifecycle_and_public_access_fail_closed` | ignore one versioning/lock/lifecycle/public result, return one enabled or disabled lifecycle rule, or remove `s3:GetLifecycleConfiguration` one at a time | readiness stays green or returns the wrong exact code |
| O18 | `O18_apply_down_reapply_restores_catalog_and_acl` | omit one Down revoke/drop | pre-Up catalog/ACL equivalence fails |
| O19 | `O19_no_plaintext_or_ciphertext_bytes_persist_in_postgres` | add `PayloadBytes bytea` to head in scratch DDL | raw-byte surface query becomes nonzero |
| O20 | `O20_readiness_precedence_and_exact_codes_are_total` | swap one adjacent readiness check | combined-failure case returns wrong first code |
| O21 | `O21_per_source_object_capacity_is_config_bound_and_atomic` | ignore the supplied lower cap or remove the source lock one at a time | cap+1 or concurrent admission succeeds |
| O22 | `O22_writer_deadline_is_frozen_to_the_earliest_landed_expiry` | remove each `min` operand or allow options reload one at a time | arm/write survives beyond the earliest deadline |

### 11.2 Architecture tests

File:
`tests/TagEkyc.ArchTests/Tip88C1B2DurableObjectCustodyArchTests.cs`.

| ID | Exact method | Mutation |
| --- | --- | --- |
| OA1 | `OA1_object_capability_interfaces_are_non_assignable` | make one implementation implement writer and reconciler |
| OA2 | `OA2_runtime_graph_cannot_resolve_s3_or_foreign_object_capabilities` | inject `IAmazonS3` or another capability into runtime/writer |
| OA3 | `OA3_multipart_batch_delete_version_copy_list_presign_and_transfer_utility_are_forbidden` | add each exact sync/async forbidden member/type from section 5.4 one at a time, including `InitiateMultipartUploadAsync`, `CopyPartAsync`, `DeleteObjectsAsync` and `DeleteObjectsRequest` |
| OA4 | `OA4_object_provider_contract_has_no_arbitrary_bucket_or_key_input` | add public string bucket/key parameter |
| OA5 | `OA5_object_handles_credentials_and_receipts_are_redacted` | expose one secret/receipt in `ToString` or logging template |
| OA6 | `OA6_s3_sdk_version_and_every_single_put_request_property_are_pinned` | change package version; replace `InputStream`; set `FilePath` or `ContentBody`; alter `ContentLength`; make the wrapper seekable; set `AutoResetStreamPosition` or `AutoCloseStream` true; remove `IfNoneMatch`; set `MaxErrorRetry` above zero; or issue a second `PutObjectAsync`, one mutation at a time |

Each mutation is applied separately, the named test is observed RED for the
specified assertion, and all touched bytes are restored and hashed before the
next mutation. A test that remains green is vacuous and blocks closeout.

OA6 statically pins all named request assignments and the one-call boundary;
O07 uses a capturing fake `IAmazonS3` transport to assert the observed request
and stream at runtime. The control must prove `InputStream` is the exact wrapped
body, `FilePath` and `ContentBody` are unset, `ContentLength` is exact, the
stream is non-seekable, `AutoResetStreamPosition=false`,
`AutoCloseStream=false`, `IfNoneMatch="*"`, client `MaxErrorRetry=0`, exactly
one `PutObjectAsync` invocation occurs, and the caller-owned stream remains
open afterward. Every property has its own mutation and exact failing
assertion; a shared package-version/condition mutation is not accepted as
coverage for the other properties.

### 11.3 Required positive controls

- first conditional create returns `Created` and preserves exact bytes;
- same begin/arm/result replay returns `ExistingMatch` without an event;
- competing put gets `409/412`, cannot change the first object, and exact
  reconciliation accepts a same valid object but rejects a mismatch;
- process/provider/DbContext replacement recovers exact object from the same
  named MinIO volume;
- writer cannot GET/LIST/DELETE, reconciler cannot PUT/LIST/DELETE, lifecycle
  cannot PUT/GET/LIST, posture identity cannot access object data;
- lifecycle positive control captures exactly one `DeleteObjectAsync` request
  containing one internally derived exact key and zero `DeleteObjectsAsync`
  calls;
- unsupported class and oversized declaration perform zero provider calls;
- no application test uses real Raw BIO;
- synthetic verification evidence is clearly fixture-only and cannot make the
  production R2 readiness dependency green.

## 12. Exact implementation allowlist for a later build

The following list is frozen for the future controlled build. The dispatch
itself is verify-only after ratification. Any required path outside this list is
a STOP/RRI before editing.

```text
src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyContracts.cs
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyOptions.cs
src/TagEkyc.Infrastructure/RawExport/S3CompatibleProvisionalObjectProvider.cs
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyRepository.cs
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyReadinessValidator.cs
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportProvisionalObjectRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportProvisionalObjectEventRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportProvisionalObjectConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportProvisionalObjectEventConfig.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260804120000_Tip88C1B2DurableObjectCustody.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260804120000_Tip88C1B2DurableObjectCustody.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/ReadinessEndpoint.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs
tests/TagEkyc.ArchTests/Tip88C1B2DurableObjectCustodyArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_object_custody_as_built.md
```

The E3 file may change only its model-snapshot SHA-256 constant after a purely
additive snapshot delta is proved. No existing E3 method/assertion may change.
No appsettings, Docker compose, deployment, policy, planning, R2 or landed
migration file is in the allowlist.

## 13. Task-0, validation and report

Before any future implementation:

1. verify exact HEAD and clean `src/`/`tests/` relative to the authorized
   baseline;
2. hash this ratified dispatch and every verify-only anchor;
3. confirm the GOV/ART fixture packet and active ratification record;
4. run Release build and the full existing test suite;
5. run corrected EF pending-model command with Infrastructure as both project
   and startup project;
6. verify PostgreSQL and pinned Docker/MinIO prerequisites;
7. capture pre-Up catalog, role, membership, table/column/function ACL and
   default-ACL snapshots;
8. declare no path outside section 12 is required.

Validation after implementation must include:

- Release build: zero warnings, zero errors;
- ArchTests, affected integration tests, E3 F6 and full unfiltered suite with
  real pass/fail/skip census;
- pending-model clean and snapshot hash gate green;
- migration apply/Down/reapply with catalog and ACL equivalence;
- every O01–O22 and OA1–OA6 mutation observed RED and byte-identical restore;
- MinIO restart/lost-response/conditional-conflict proof with zero task-created
  container, process, bucket, object, credential or volume residue;
- `git diff --check`, staged paths zero and unrelated dirt untouched.

The final report must name the exact package/image, configuration posture,
function/constraint lengths, role/ACL results, mutation failures, test census,
hashes, cleanup evidence and any STOP/RRI. It may report only readiness for
independent closeout review. It must not claim commit, push, production or R2
authority.

## 14. PI-TAG-001 convergence matrices

### 14.1 Semantic trace matrix

| Requirement | Schema/function/provider surface | Test | Mutation |
| --- | --- | --- | --- |
| one object per attempt | unique AttemptId + O-T01 | O03 | drop unique |
| exact attempt/key/object binding | four-column FK + begin projection | O04/O05 | drop FK/change key |
| no overwrite | `IfNoneMatch="*"` + conflict state | O10 | omit condition |
| lost-response recovery | PutOutcomeUnknown + exact inspect | O08/O09 | early absence |
| authenticated completion | verification digest + R2 seam | O12 | permit NULL |
| cleanup evidence | separate delete-ack/absence functions + state-specific quarantine codecs | O13 | actor swap/batch delete/source-digest swap/early Deleted |
| capability separation | four interfaces + exact ACL | O15/OA1/OA2 | overgrant/assignability |
| no multipart/batch-delete/version/lock/lifecycle | exact SDK symbols + posture readiness | O13/O17/OA3 | second/batch delete, forbidden reference or ignored posture |
| no raw bytes in DB | metadata/digest-only schema | O19 | scratch payload column |

### 14.2 Delta matrix

| Prior v0.2 concept | v0.3 disposition |
| --- | --- |
| multipart session/parts/upload discovery | removed; separately deferred |
| object version/VersionId | removed; versioning prohibited |
| Object Lock | removed; prohibited |
| `UNIQUE(AttemptId, ProvisionalObjectIdentity)` | replaced by unique AttemptId plus unique identity |
| ambiguous `VerifiedCompleted` terminal | split write-protocol completion from lifecycle terminal |
| list-based recovery | exact deterministic-key inspect only |
| frame-to-provider-part mapping | removed; frames are bytes inside one object body |

v0.4 does not change the v0.3 transport decision. It closes five review gaps:
exact SDK symbols, deletion-actor separation, lifecycle posture, canonical
evidence vocabularies/digests and complete single-put property mutations.

v0.5 closes the two residual v0.4 findings without changing that decision:
multi-object delete is forbidden/mutation-proven, and quarantine evidence has
one exact predecessor-specific source and domain on each branch.

### 14.3 Intent ledger

| Decision | Reason | Reopen trigger |
| --- | --- | --- |
| one conditional PutObject | current classes are bounded still images | separately ratified heavy-media slice |
| 128 MiB ciphertext cap | bounded headroom over 64 MiB plaintext class ceiling | ratified profile/R2 length change |
| versioning/Object Lock off | exact deletion and no hidden versions/markers | separate governance and custody design |
| deterministic opaque key | exact recovery without list | object namespace version change |
| conflicts quarantine, not delete | mismatched object ownership is unproven | reviewed provider ownership proof |
| reuse existing three roles | avoid parallel authority graph | landed role architecture change |

### 14.4 Source-of-truth register

| Topic | Authority |
| --- | --- |
| initial class and plaintext limits | TIP-88C1 Planning Brief |
| attempt/key/AEAD binding | landed DK-PROD and DK-FIXTURE-PROOF |
| current transport choice | Homeowner 2026-08-04 ratification + this reviewed dispatch candidate |
| fixture evidence authority | GOV/ART packet/ratification chain; currently inactive |
| R2 frame/authenticated completion | future corrected and ratified R2 dispatch |
| production retention/legal policy | later GOV/ART Gate B; not this slice |

## 15. STOP/RRI conditions

STOP before implementation if any of the following is true:

- this document has not passed independent review and Homeowner build
  ratification;
- GOV/ART fixture evidence remains inactive when a provider evidence run is
  requested;
- any code path requires multipart, list, version, Object Lock, copy, rename,
  presign or public access;
- an initial object can exceed 128 MiB or a third class is required;
- exact lost-response recovery cannot be proven without overwriting or list;
- the existing three-role graph cannot express the required SQL/provider
  capabilities;
- S3 IAM/provider policy requires an overgrant not disclosed in this document;
- any path outside the exact allowlist is needed;
- a landed migration or DK-PROD/DK-FIXTURE semantic must change;
- a test mutation remains green or fails for a collateral reason;
- provider fixture cannot prove restart durability and zero residue;
- any real Raw BIO, R2, staging, delivery, commit, push or deployment authority
  is inferred from this docs-only redraft.

## 16. Current disposition

```text
DOCS-ONLY v0.5 FINAL CLOSURE PATCH COMPLETE
READY FOR ONE TWO-FINDING CLOSURE REVIEW
NOT READY FOR BUILD
```

Required next action: run one closure review over v0.5 limited to DOBJ4-H01,
DOBJ4-M01 and regressions directly caused by their edits: exact
`DeleteObjects` symbol/type prohibition, one-key `DeleteObjectAsync` behavior,
state-specific quarantine preimages/vectors and O13/OA3 bite. Do not reopen the
ratified single-part decision or previously closed v0.4 classes without new
contradictory evidence. If that review passes, obtain separate Homeowner build
ratification.
