# TIP-88C1-B2-R2 - Durable Custody Encryption - BUILD DISPATCH (REDRAFT v4.3)

**Version:** 4.3-review-candidate<br>
**Status:** READY FOR INDEPENDENT REVIEW - NOT RATIFIED - NOT DISPATCHED<br>
**Date:** 2026-08-07<br>
**Repository:** `D:\Task\Remote Signing\TagEkyc`<br>
**Baseline:** `3d7a50ec812f158c26373028fe7a2e97017125af`

This redraft is documentation-only. It does not authorize implementation,
migration execution, staging, commit, push, merge, PR, deployment, production
activation, real Raw BIO access, R3 staging or delivery.

## 0. Why v4 exists

### v4.1 review reconciliation

V4.1 closes the six execution findings from the first independent review:

- provisioning now precedes the active-only encryption projection;
- termination is bound inside SQL to the exact terminal object state;
- deterministic invalidity is separated from retryable verification
  uncertainty;
- orchestration accepts the exact landed fixture profile tuple while the small
  absolute vector remains codec-only;
- pre-custody rejection is separated from the landed post-Begin `NotArmed`
  transition;
- the historic commitment payload is built in R2-owned zeroizable bytes
  without a digest-bearing managed string.

### v4.2 closure reconciliation

V4.2 closes the two execution blockers found by the v4.1 closure review:

- the landed verification AEAD boundary is evolved narrowly to return a typed
  stage outcome, so key/envelope/unwrap uncertainty can never be mistaken for
  deterministic ciphertext authentication failure;
- pre-custody provisioning outcomes are partitioned explicitly, and a
  zero-object `TerminatedBeforeStart` CAS is permitted only when the same
  attempt/key reservation is locked with durable `Revoked` or
  `ReservationAbandoned` evidence.

### v4.3 exact-binding closure

V4.3 corrects the zero-object terminal-key predicate to compare each identifier
within its own domain: key `AttemptId` to the locked source attempt's
`AttemptId`, and key `AttemptKeyReservationId` to the locked source attempt's
`AttemptKeyReservationId`. It also clarifies that section 3.1.1 owns
provisioning outcomes while section 7 owns the remaining pre-custody failures.

R2 v3 predated the landed durable foundations and therefore designed its own
wrapped-key lifecycle, object write-session registry, multipart recovery,
roles and readiness topology. Those designs are superseded.

At the v4 baseline the repository already contains:

| Landed foundation | Authoritative/current anchor | What R2 consumes unchanged |
| --- | --- | --- |
| DK-PROD | `tip_88c1_b2_durable_key_prod_as_built.md`, SHA-256 `AAF9E89A20C5BFF09C3A69DE7239BF677FE4665746AFB09AA732A69BCF1F89E7` | durable attempt-key lifecycle, active wrapped envelope, operation-scoped AEAD, fixed timing, capability roles |
| DK-FIXTURE-PROOF | `tip_88c1_b2_durable_key_fixture_proof_as_built.md`, SHA-256 `8BC73AC4DF7280CE53BE5F6A4FF91AB369660A15D96923F8A3B6A1F2F28CC896` | restart-safe test KEK boundary and fixture evidence only; not production qualification |
| DURABLE-OBJECT contract | `tip_88c1_b2_durable_object_custody_build_dispatch.md`, SHA-256 `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918` | bounded single-part conditional write, exact-object reconcile/read, lifecycle delete, state/ACL/readiness contracts |
| DURABLE-OBJECT as built | `tip_88c1_b2_durable_object_custody_as_built.md`, SHA-256 `B9E8873F45F826DC8889B01A23E9393F81A12B75F587E87153BDE34F71536A6E` | landed PostgreSQL/MinIO provider surfaces at this baseline |

The following code anchors were verified at this baseline:

```text
37A7310107092F38CD9EAB048F6CD5AA089EB83E2D4C79C04215DA49FD4A18E8  AttemptAeadOperationContracts.cs
01496A29227C2417CFF860012642EA33BE5071B6CA665784F78BF5F5E0939BC1  AttemptAeadOperationService.cs
8815604ACC33CDD309DBEB941898C39FEC51D4ECC67AE43239FC40779D5B77BE  ProvisionalObjectCustodyContracts.cs
6E22CE2994D1C4A156498B6BC8A62F0298EEDED06BDC3A30BCF69EE20CDAD45A  ProvisionalObjectCustodyRepository.cs
6EEBEE68DA5222C1B28635B1A2C28F679197BD637C8F7F160F331012BD15116E  RawExportSourceEncryptionAttemptRow.cs
EFC8B0C99A6B2E5EE930CE749BFB70202AD6EE6770E909C3D51AC79FFF49FC3B  RawExportAttemptKeyReservationRow.cs
E69C8DB733782B673A5816F2CDFC77855D08020DA7B9B6620F12822A8536EB3F  CustodyProfileProvider.cs
690A9DA49BED1580AB4A3455640A375A85F8A78C3280DAB9FBEDB3239FF8CF89  RawExportSourceClaimComparisonBroker.cs
90952A72FFCA170B824CFAF6ABAF42632A257C5B4A0090D5465BA0DFE8E18A0C  C1HashCanonical.cs
B54BCBE5EE1402DE5D2B35DBB53D7593AF0E063363CE877B6654BF32B8E33836  PostgresAttemptKeyReservationProvider.cs
```

V4 removes, rather than renames, all v3 greenfield key-provider tables,
write-session/part tables, multipart/UploadId recovery, provider package
selection, capability-role creation, membership management and provider
readiness design.

## 1. Ownership boundary

### 1.1 R2 owns exactly

1. Reading the frozen source-encryption and verification context through two
   narrow SECURITY DEFINER projections.
2. Reproducing the landed content commitment from a transient streaming
   plaintext digest under the frozen historic selector.
3. The byte-exact framed ciphertext format, per-frame AEAD calls, the narrow
   typed verification outcome boundary, bounded streaming and locally owned
   buffer zeroization.
4. Orchestration of the landed key-provisioning operation and the landed
   single-part conditional object writer.
5. Verification of an exact durable object through the landed reconciler
   read capability, followed by the landed `mark verified` transition.
6. Append-once R2 termination metadata on the landed encryption-attempt row.
7. A typed durable handoff to reconciliation when the writer cannot prove a
   completed result. R2 never performs a blind same-attempt rewrite.

### 1.2 Consumed unchanged except for section 1.3

- `IAttemptKeyReservationProvisioningOperation` provisions or recovers the
  durable key reservation.
- `IAttemptAeadEncryptionOperation` remains unchanged. The existing
  `IAttemptAeadVerificationOperation` remains the only verification crypto
  boundary but is narrowed as specified in section 1.3. R2 never receives a
  plaintext DEK or a KEK.
- `IContentCommitmentService` is called with the selector frozen on the source
  reservation. R2 does not select a current/latest key.
- `IProvisionalObjectWriter`, `IProvisionalObjectReconciler`,
  `ProvisionalObjectCustodyRepository`, the landed object tables/functions,
  and the fixed 128 MiB ciphertext ceiling remain authoritative.
- The landed encryptor, reconciler and lifecycle LOGIN/capability graph and all
  DK-PROD/DURABLE-OBJECT readiness codes remain authoritative.

### 1.3 Narrow typed AEAD verification evolution

The current landed method returns `Task<byte[]>` after performing active-envelope
read, KEK unwrap and `AesGcm.Decrypt` inside one call. That shape cannot prove
whether failure occurred before or during ciphertext authentication. The R2
build therefore changes only the verification return contract and its landed
service implementation:

```text
AttemptAeadVerificationOutcome =
  Verified
  | KeyAccessIndeterminate
  | AuthenticationFailed

AttemptAeadVerificationResult =
  Outcome
  Output  // non-NULL only for Verified
```

`DecryptAndVerifyBoundedChunkAsync` returns
`Task<AttemptAeadVerificationResult>`. Request-shape validation remains a caller
error. Active-envelope read, key availability, timeout or unwrap/provider
failure can produce only `KeyAccessIndeterminate` or propagate as infrastructure
uncertainty; neither can produce `AuthenticationFailed`. Only a
`CryptographicException` raised by `AesGcm.Decrypt` after a DEK lease was
successfully acquired produces `AuthenticationFailed`. Successful decrypt
produces `Verified` with the owned plaintext chunk. Every non-success path has
NULL output and zeroizes any allocated output buffer. No raw exception type or
message is used by the R2 caller to infer the failure stage.

This evolution does not alter encryption, key provisioning, wrapped-key
lifecycle, KEK contracts, key roles, SQL, ACL or readiness. It adds no public
raw-byte API; the contract remains the existing operation-scoped crypto
boundary.

### 1.4 Explicitly out of scope

- a new key table/provider, raw-DEK lease, KEK implementation or key readiness;
- a new object table/provider, bucket configuration or object readiness;
- multipart, parts, UploadId, write-session resume, versioning or Object Lock;
- role creation, LOGIN creation, membership mutation or new provider identity;
- a public/raw-byte API, real CaptureAgent/vault adapter or real Raw BIO;
- R3 staging, package assembly, delivery, download or receipt;
- replacement-attempt allocation. Reconciliation may terminate and hand off;
  only a later custody-ingress/current-owner path may re-authorize and allocate
  a replacement attempt.

## 2. Load-bearing invariants

1. No database transaction or row lock spans plaintext-source, key-provider,
   AEAD or object-provider I/O.
2. R2 writes one bounded single-part object through one conditional
   `PutIfAbsentAsync` call. It never overwrites or retries a possibly written
   object.
3. A complete existing object is reconciled and verified; it is never
   re-encrypted.
4. A partial, malformed, unauthenticated, conflicting or possibly written
   object never becomes `VerifiedCompleted`.
5. A FINAL frame is emitted only after plaintext length and historic content
   commitment match the frozen R1 evidence.
6. The transient plaintext digest is never returned, persisted or logged.
7. The writer cannot inspect/read/delete/list. The reconciler cannot write,
   overwrite, delete, list or allocate a replacement attempt.
8. The caller owns the source stream. R2 zeroizes only buffers and crypto
   scratch that R2 allocated or rented. No abrupt-process-loss zeroization
   claim is made.
9. Same-attempt encryption is never resumed. Nonces therefore do not need a
   new durable write-session registry: after uncertainty, reconciliation
   inspects the exact landed object; after terminal invalidity/absence, a later
   authorized path creates a wholly new attempt/key/object identity.
10. PostgreSQL stores metadata and digests only. Plaintext and ciphertext bytes
    remain outside PostgreSQL.

## 3. Exact orchestration

### 3.1 Writer path

The encryptor executes this order:

```text
1. Validate the request tuple (AttemptKeyReservationId, AttemptId,
   SourceArtifactId, expected revision/fence) and caller-owned readable stream.
2. Call IAttemptKeyReservationProvisioningOperation outside a DB transaction
   with its landed tuple (AttemptKeyReservationId, AttemptId, SourceArtifactId).
3. Partition every non-success provisioning outcome exactly as section 3.1.1;
   do not collapse it to a generic rejection.
4. Accept only `Activated` or `ExistingMatch` for encryption.
5. Set the landed actor GUC and read the active-only R2 encryption context.
6. Require exact request/context AttemptId, SourceArtifactId, revision, fence and key-reservation equality.
7. Validate the exact profile/class and compute ciphertext length from frozen plaintext length/chunk size/frame format.
8. Reject zero length, unsupported profile/class or >128 MiB ciphertext before object custody exists.
9. Call landed raw_export_begin_provisional_object_custody and commit.
10. Create the framed non-seekable ciphertext stream; no provider I/O has begun yet.
11. Call landed raw_export_arm_provisional_object_put and commit.
12. Call IProvisionalObjectWriter.PutIfAbsentAsync exactly once.
13. Record the landed Created, ConditionalConflict or OutcomeUnknown result and commit.
14. Return only the durable object state/revision and typed handoff disposition.
```

`Created` advances the landed object to
`ObjectPresentPendingVerification`. `ConditionalConflict` and transport/body
uncertainty remain `PutOutcomeUnknown`. After arm, cancellation, source read
failure, AEAD failure, commitment mismatch or an unclassified provider result
is not absence; it is recorded as `OutcomeUnknown` because the provider may
have observed a request. Only reconciliation decides what exists.

Failure before step 9 has one of the exact pre-custody dispositions defined by
section 3.1.1 for provisioning or section 7 for all other pre-custody checks:
no object row exists, no landed `NotArmed` transition is called and no
object-provider call is made. Failure
after Begin commits but before Arm uses the landed `RecordNotArmed` transition
on the new `ObjectCustodyId`; it is the only phase that returns `NotArmed`.

#### 3.1.1 Exact key-provisioning outcome partition

The writer maps the landed `AttemptKeyProvisioningOutcome` values exactly:

```text
Activated | ExistingMatch
  -> continue to the active-only encryption projection

InProgress | ProviderUnavailable | ProviderOutcomeUnknown
  -> PreCustodyRetryable

Conflict | HeadNotReserved | ProviderCorruptOrUnverifiable | StateConflict
  -> PreCustodyOperatorRequired

Terminated
  -> inspect the same AttemptKeyReservationId through the landed exact inspect
     surface
  -> Revoked | ReservationAbandoned
       -> PreCustodyTerminalKey
       -> reconciliation handoff for TerminatedBeforeStart
  -> ProviderCorruptOrUnverifiable | AbandonRequested | every other/absent,
     mismatched or indeterminate disposition
       -> PreCustodyOperatorRequired
```

An inspection result is advisory to routing only. The termination function
re-locks and re-proves the attempt/key relationship and terminal disposition in
the same transaction as its CAS. The writer never calls termination, never
allocates a replacement and never treats `Terminated` alone as sufficient
terminal evidence.

### 3.2 Reconciliation and verification handoff

The reconciler first uses the landed exact-object state machine:

```text
PutInFlight / PutOutcomeUnknown
-> exact inspect
-> PositivelyAbsent | ObjectConflict | ObjectPresentPendingVerification
```

Only `ObjectPresentPendingVerification` enters the R2 verifier. The verifier:

1. reads the narrow verification context and the landed exact object locator;
2. opens one bounded exact-object read through
   `IProvisionalObjectReconciler.OpenExactReadAsync`;
3. parses the header and every DATA/FINAL frame with exact EOF;
4. calls `IAttemptAeadVerificationOperation` per frame;
5. transiently recomputes plaintext SHA-256, historic content commitment,
   total length, whole-object SHA-256, DATA-frame digest and tag-chain
   commitment;
6. fixed-time compares every frozen/final value;
7. on success, computes the canonical 32-byte verification evidence and calls
   the landed `raw_export_mark_provisional_object_verified` transition;
8. returns verified metadata only. It does not return plaintext, plaintext
   digest, ciphertext or key material to R3.

The evidence is exact:

```text
VerificationEvidenceDigest = SHA256(
  LP_UTF8("tip-88c1-r2-verification-evidence-v1")
  || AttemptId[16]
  || ObjectCustodyId[16]
  || ProvisionalObjectIdentity[16]
  || ObjectBindingDigest[32]
  || EncryptionAttemptFingerprint[32]
  || CiphertextLength(u64)
  || CiphertextDigest[32]
  || ObjectHeaderDigest[32]
  || DataCiphertextDigest[32]
  || ContentCommitment[32]
  || AuthenticationChunkCommitment[32]
  || EncryptionEnvelopeMetadataDigest[32]
  || WrappedKeyMetadataDigest[32])
```

Verifier outcomes are three-way:

```text
Verified
VerificationIndeterminateRetry
VerificationFailedRequiresCleanup
```

Network/read interruption, cancellation, provider unavailability,
`KeyAccessIndeterminate`, commitment-provider failure and a transient database
failure are `VerificationIndeterminateRetry`. An exception before the typed
AEAD result is available is also indeterminate and can never be inferred to be
authentication failure from exception type/message. The object remains
`ObjectPresentPendingVerification`; there is no cleanup, termination or
re-encryption, and reconciliation retries the exact object later.

`VerificationFailedRequiresCleanup` is reserved for deterministic invalidity
after the required inputs were available: invalid header/frame grammar,
binding/ordinal/length/EOF mismatch, the exact typed
`AuthenticationFailed` AEAD outcome, or a
successfully recomputed historic commitment mismatch. The reconciler may then
invoke the landed cleanup-required transition. It does not rewrite the object
or allocate an attempt.

### 3.3 Termination and replacement handoff order

Termination is evidence, not cleanup authority and not replacement authority:

```text
NotArmed / reconciled positive absence
-> object state NoObjectEstablished
-> TerminatedBeforeStart CAS
-> handoff EligibleForFreshAuthorization

key provisioning returns Terminated before object custody
-> exact inspect routes only Revoked | ReservationAbandoned as terminal
-> termination SQL locks the same attempt and key reservation
-> zero object rows + durable terminal key evidence
-> TerminatedBeforeStart CAS
-> handoff EligibleForFreshAuthorization

invalid/incomplete owned object
-> CleanupPending
-> exact delete acknowledgement or two-observation positive absence
-> object state Deleted
-> Terminated CAS
-> handoff EligibleForFreshAuthorization

foreign/conflicting or cleanup-failed object
-> Quarantined
-> Terminated CAS
-> handoff OperatorResolutionRequired (not replacement-eligible)

VerifiedCompleted
-> no R2 termination
-> future R3 handoff only
```

Reconciliation may record termination after the listed durable object
evidence exists. It cannot create the replacement. A later ingress/current-
owner slice must perform fresh authority, consent and retention checks and an
exact source-head CAS before allocating a new attempt/revision/fence/key
reservation/object identity.

## 4. Frozen database additions owned by R2

One additive migration is permitted. It must not edit any landed migration.

### 4.1 Attempt termination metadata

Add to `tagekyc.raw_export_source_encryption_attempts`:

```text
R2TerminatedAtUtc timestamptz NULL
```

Replace only `ck_raw_export_source_attempt_values` with the landed predicates
plus this exact sparse shape:

```text
(R2TerminationDisposition IS NULL AND R2TerminatedAtUtc IS NULL)
OR
(R2TerminationDisposition IN ('Terminated','TerminatedBeforeStart')
 AND R2TerminatedAtUtc IS NOT NULL)
```

Create:

```text
tagekyc.raw_export_terminate_source_encryption_attempt(
  p_attempt_id uuid,
  p_expected_encryption_attempt_revision bigint,
  p_expected_fence bigint,
  p_disposition text)
RETURNS text
```

It is SECURITY DEFINER, owner `tagekyc_raw_export_deployer`, uses
`SET search_path=pg_catalog`, obtains the actor only through
`tagekyc.raw_export_current_actor()`, and is granted only to
`tagekyc_raw_export_reconciler`. It permits exactly one
`NULL/NULL -> Terminated|TerminatedBeforeStart/statement_timestamp()` CAS on
the exact attempt/revision/fence. It locks the attempt, the same
`AttemptKeyReservationId` head and the zero-or-one provisional-object row in
the same transaction. The terminal predicate is mandatory:

```text
TerminatedBeforeStart -> exactly one object row in NoObjectEstablished
                      | zero object rows AND the key reservation:
                          key.AttemptId equals the locked source attempt's AttemptId
                          key.AttemptKeyReservationId equals the locked source attempt's AttemptKeyReservationId
                          PreparationDisposition is Revoked
                            or ReservationAbandoned
Terminated            -> exactly one object row in Deleted | Quarantined
```

Zero object rows without the exact terminal key evidence, more than one object
row or any other object state returns `StateConflict` without mutation. A key
head in `ProviderCorruptOrUnverifiable`, `AbandonRequested`, `Active`, a
preparing/retry state, an absent/mismatched key row or any unrecognized state
cannot authorize zero-object termination. In particular `Initiated`,
`PutInFlight`, `PutOutcomeUnknown`, `ObjectPresentPendingVerification`,
`VerifiedCompleted` and `CleanupPending` cannot authorize object-backed
termination. Replay of the exact
terminal value returns `ExistingMatch`; a different terminal value or stale
revision/fence returns `StateConflict`; absence of the attempt returns
`NotFound`. It has no caller-supplied actor.

The migration CREATE OR REPLACEs the landed
`enforce_raw_export_source_core_write()` and restores its exact pre-R2 body in
`Down()`. The replacement retains the landed `complete-r1` INSERT behavior and
permits UPDATE only when all of these are true:

- `TG_TABLE_NAME = 'raw_export_source_encryption_attempts'`;
- current user is the deployer;
- the transaction-local write context is the private R2 termination token;
- OLD termination fields are both NULL;
- NEW has the exact sparse terminal shape above;
- every other column is `IS NOT DISTINCT FROM OLD`.

UPDATE of reservation/head rows and DELETE of every CORE row remain forbidden.
The termination function captures the prior GUC value and restores it on both
success and exception. Direct GUC spoofing cannot bypass table ACL or the
current-user/table/column guard.

### 4.2 Split read projections

Both functions are SECURITY DEFINER, owner
`tagekyc_raw_export_deployer`, use `SET search_path=pg_catalog`, require the
landed actor GUC, have one exact overload and return one row only when the
attempt/source/head binding is exact. The encryptor projection additionally
requires the attempt lease, effective plaintext-retention deadline and source
reservation deadline to be in the future. The verification projection may run
after writer-lease expiry but requires the same current head/fence and an
unterminated attempt. Both require `R2TerminationDisposition` and
`R2TerminatedAtUtc` to be NULL. Both join the exact attempt-key reservation in
`PreparationDisposition='Active'` and require a 32-byte
`WrappedDekMetadataDigest`; neither silently accepts an in-progress, revoked,
abandoned or malformed key reservation.

Encryptor-only projection:

```text
raw_export_read_source_encryption_context(uuid,bigint,bigint)
RETURNS TABLE(
  AttemptId uuid, SourceArtifactId uuid,
  EncryptionAttemptRevision bigint, Fence bigint,
  AttemptKeyReservationId uuid, ProvisionalObjectIdentity uuid,
  EncryptionAttemptFingerprint bytea,
  KeyProviderId text, KekId text, KekVersion integer, KekFingerprint text,
  EncryptionSuiteId text, EncryptionFramingVersion integer,
  ChunkSize integer, NonceStrategyId text,
  NonceDerivationSeedReferenceOrWrappedSeed text,
  NonceDerivationSeedCommitment bytea, FramingParametersDigest bytea,
  WrappedDekMetadataDigest bytea,
  VerificationSessionId uuid, CaptureArtifactId uuid,
  CaptureRevision integer, RawClass text,
  StableDataScopeId text, ControllerIdentity text,
  ClaimedPlaintextLength bigint, MediaType text,
  ContentCommitmentSchemaVersion integer,
  ContentCommitmentKeyId text, ContentCommitmentKeyVersion integer,
  ContentCommitment bytea,
  OwnershipLeaseExpiresAtUtc timestamptz,
  EffectivePlaintextRetentionExpiresAtUtc timestamptz,
  ReservationExpiresAtUtc timestamptz)
```

Reconciler-only projection:

```text
raw_export_read_source_verification_context(uuid,bigint,bigint)
RETURNS TABLE(
  AttemptId uuid, SourceArtifactId uuid,
  EncryptionAttemptRevision bigint, Fence bigint,
  AttemptKeyReservationId uuid, ProvisionalObjectIdentity uuid,
  EncryptionAttemptFingerprint bytea,
  KeyProviderId text, KekId text, KekVersion integer, KekFingerprint text,
  EncryptionSuiteId text, EncryptionFramingVersion integer,
  ChunkSize integer, NonceStrategyId text, FramingParametersDigest bytea,
  WrappedDekMetadataDigest bytea,
  VerificationSessionId uuid, CaptureArtifactId uuid,
  CaptureRevision integer, RawClass text,
  StableDataScopeId text, ControllerIdentity text,
  ClaimedPlaintextLength bigint, MediaType text,
  ContentCommitmentSchemaVersion integer,
  ContentCommitmentKeyId text, ContentCommitmentKeyVersion integer,
  ContentCommitment bytea)
```

The encryptor function is granted only to
`tagekyc_raw_export_custody_encryptor`; the verification function only to
`tagekyc_raw_export_reconciler`. PUBLIC, runtime and the opposite capability
have no EXECUTE. No direct table or column privilege is added. Neither
projection returns wrapped key bytes, plaintext, ciphertext, provider
credentials or receipt text.

### 4.3 Migration/ACL limits

The migration creates no role or membership and changes no landed
DK-PROD/DURABLE-OBJECT function signature, grant, table or readiness code.
`Down()` revokes only the three R2 functions, drops them, restores the exact
pre-R2 trigger body/check constraint, removes `R2TerminatedAtUtc`, and
preserves all deployment LOGIN and capability roles.

## 5. Byte-exact framed ciphertext contract

All integers are unsigned big-endian. All GUIDs are RFC-4122/network byte
order (`Guid.ToByteArray(bigEndian: true)`). `LP_UTF8(x)` is `u32` byte length
followed by strict UTF-8 of NFC-normalized text. A decoder rejects invalid
UTF-8, non-NFC text, overlong encodings and values above their pinned maximum.

```text
Domain family: TAG-EKYC:RAW-EXPORT:PROVISIONAL-OBJECT:C1:R2:V1
Magic:         ASCII TAGEKYC-C1-R2 (13 bytes)
FormatVersion: u16 0x0001
AEAD tag:      16 bytes
Nonce:         12 bytes
FINAL plaintext length: 181 bytes
```

Pinned text maxima after UTF-8 encoding:

```text
EncryptionSuiteId: 128 bytes
NonceStrategyId:   128 bytes
```

The initial orchestration admits only the exact landed fixture profile tuple:

```text
EncryptionSuiteId        = fixture-aead-aes256gcm-v1
EncryptionFramingVersion = 1
ChunkSize                 = 1048576
NonceStrategyId           = fixture-nonce-random96-v1
```

Those exact frozen identifiers are serialized in the object header and used
in metadata digests; they are never rewritten to algorithm labels.
Internally, that profile maps to the landed AES-256-GCM operation and the
`0x00|random88` / `0x01|random88` nonce construction in section 5.3.

Only the current raw classes `ChipDg2Portrait` and `LiveSelfieImage` are
admitted. Zero-length plaintext is invalid. `ClaimedPlaintextLength` must be
positive, at most the landed class limit, and produce ciphertext no larger
than `134217728` bytes. Any different profile tuple fails before object
custody is created.

### 5.1 Header

Serialized once:

```text
Magic[13]
FormatVersion u16
AttemptId[16]
AttemptKeyReservationId[16]
SourceArtifactId[16]
ProvisionalObjectIdentity[16]
EncryptionAttemptFingerprint[32]
ObjectBindingDigest[32]
EncryptionSuiteId LP_UTF8
EncryptionFramingVersion u16
ChunkSize u32
NonceStrategyId LP_UTF8
FramingParametersDigest[32]
```

`ObjectHeaderDigest = SHA256(exact serialized header bytes)`.

### 5.2 DATA frame

For zero-based contiguous ordinals:

```text
FrameType = u8 0x01
ChunkOrdinal u32
PlaintextLength u32
Nonce[12]
Ciphertext[PlaintextLength]
AuthTag[16]
```

DATA AAD is:

```text
LP_UTF8("tip-88c1-r2-data-aad-v1")
|| EncryptionAttemptFingerprint[32]
|| ObjectHeaderDigest[32]
|| u8(0x01)
|| ChunkOrdinal u32
|| PlaintextLength u32
```

`DataCiphertextLength` and `DataCiphertextDigest` cover the concatenation of
the exact serialized DATA frames, including frame headers, nonce, ciphertext
and tag, but excluding the object header and FINAL frame.

The tag-chain is:

```text
h0 = SHA256(LP_UTF8("tip-88c1-r2-chunk-chain-v1") || ObjectHeaderDigest)
hi = SHA256(h(i-1) || ChunkOrdinal(u32) || AuthTag[16])
AuthenticationChunkCommitment = hN
```

At least one DATA frame is required; the last frame has length `1..ChunkSize`.
Every earlier frame has length exactly `ChunkSize`.

### 5.3 Nonces

R2 obtains 11 random bytes from the platform CSPRNG for each frame:

```text
DATA nonce  = 0x00 || random88
FINAL nonce = 0x01 || random88
```

The namespaces are disjoint. R2 also rejects any duplicate 11-byte DATA
suffix within the attempt before encrypting that frame. A detected duplicate
is not regenerated in the same attempt: the stream fails, the armed operation
becomes `OutcomeUnknown`, and reconciliation follows section 3.3 after exact
object resolution. The FINAL nonce is generated only after source evidence
matches and is necessarily distinct from every DATA nonce by its first byte.

No nonce is persisted for same-attempt replay because same-attempt encryption
replay is forbidden. Lost response recovery uses the exact durable object;
replacement uses a new attempt, key reservation, fence, nonce domain and
object identity.

### 5.4 Historic content-commitment verification

While streaming, R2 computes SHA-256 over plaintext bytes. After exact EOF it
constructs bytes exactly equivalent to the landed
`C1HashCanonical.EncodeLengthPrefixedPayload` result in this order:

```text
"TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1"
StableDataScopeId
ControllerIdentity
VerificationSessionId.ToString("N")
CaptureArtifactId.ToString("N")
CaptureRevision invariant decimal
RawClass
lowercase hex(transient plaintext SHA-256)
ClaimedPlaintextLength invariant decimal
MediaType
```

R2 must not create a digest-bearing managed `string`. It writes the raw
32-byte digest directly as 64 lowercase ASCII hex bytes into an R2-owned
zeroizable buffer, prefixes that field with big-endian `u32(64)`, and writes
all other fields with the landed NFC/strict-UTF8/u32-length rules. The complete
LP payload is an R2-owned zeroizable byte buffer. An independent equivalence
test compares it byte-for-byte with the landed helper for non-sensitive fixed
vectors.

It calls `IContentCommitmentService.ComputeAsync` with only
`ContentCommitmentKeyId/ContentCommitmentKeyVersion` from the frozen context.
The computed MAC, exact length and stored `ContentCommitment` are compared in
fixed time. The raw digest and full encoded commitment payload cross only the
internal R2-to-commitment-service call boundary; they are never returned,
persisted or logged. R2 zeroizes both buffers and every R2-owned encoding
scratch in `finally`. No claim is made that immutable non-secret source fields
or provider-owned internal copies are actively zeroized.

### 5.5 Envelope metadata digest

```text
EncryptionEnvelopeMetadataDigest = SHA256(
  LP_UTF8("tip-88c1-r2-envelope-metadata-v1")
  || AttemptId[16]
  || AttemptKeyReservationId[16]
  || LP_UTF8(KeyProviderId)
  || LP_UTF8(KekId)
  || KekVersion(u32)
  || LP_UTF8(KekFingerprint)
  || LP_UTF8(EncryptionSuiteId)
  || EncryptionFramingVersion(u16)
  || ChunkSize(u32)
  || LP_UTF8(NonceStrategyId)
  || FramingParametersDigest[32]
  || WrappedDekMetadataDigest[32])
```

Key-provider identifiers have the already-landed maximum lengths. This codec
does not weaken them.

### 5.6 FINAL frame

Completion plaintext is exactly 181 bytes:

```text
ChunkCount u32                                      4
TotalDataPlaintextLength u64                        8
DataCiphertextLength u64                            8
DataCiphertextDigest[32]                           32
ContentCommitment[32]                              32
AuthenticationChunkCommitment[32]                  32
EncryptionEnvelopeMetadataDigest[32]               32
WrappedKeyMetadataDigest[32]                       32
Finalized u8 = 0x01                                 1
                                                   ---
                                                   181
```

FINAL AAD is:

```text
LP_UTF8("tip-88c1-r2-final-aad-v1")
|| EncryptionAttemptFingerprint[32]
|| ObjectHeaderDigest[32]
|| u8(0x02)
|| ChunkCount(u32)
|| TotalDataPlaintextLength(u64)
```

FINAL serialization is:

```text
FrameType = u8 0x02
Nonce[12]
CompletionCiphertextLength u32 = 181
CompletionCiphertext[181]
AuthTag[16]
```

The complete object ends exactly after the FINAL tag. A missing, duplicate,
reordered or trailing byte is invalid.

### 5.7 Exact ciphertext length

If `N = ceil(ClaimedPlaintextLength / ChunkSize)`, then:

```text
HeaderLength = 189
             + utf8_length(EncryptionSuiteId)
             + utf8_length(NonceStrategyId)

DataFramesLength = ClaimedPlaintextLength + 37 * N
FinalFrameLength = 214
CiphertextLength = HeaderLength + DataFramesLength + FinalFrameLength
```

The writer supplies this exact value to the landed object provider before I/O.
The stream must yield exactly this count and exact EOF.

### 5.8 Absolute codec vector

R202 pins this complete fixture vector. The 32-byte test DEK is test input to
the independent codec oracle only; it is never a production R2 surface.
The vector deliberately uses `AES-256-GCM`, `Random96` and `ChunkSize=4` as
codec inputs. It does not pass orchestration admission and does not replace or
rename the exact landed tuple at the opening of section 5. R207 separately
proves that orchestration accepts exactly that landed tuple.

```text
AttemptId                    = 00010203-0405-0607-0809-0a0b0c0d0e0f
AttemptKeyReservationId      = 10111213-1415-1617-1819-1a1b1c1d1e1f
SourceArtifactId             = 20212223-2425-2627-2829-2a2b2c2d2e2f
ProvisionalObjectIdentity    = 30313233-3435-3637-3839-3a3b3c3d3e3f
EncryptionAttemptFingerprint = bytes 0x40..0x5f
ObjectBindingDigest          = bytes 0x60..0x7f
FramingParametersDigest      = bytes 0x80..0x9f
EncryptionSuiteId            = AES-256-GCM
EncryptionFramingVersion     = 1
ChunkSize                    = 4
NonceStrategyId              = Random96
KeyProviderId                = fixture-provider
KekId                        = fixture-kek
KekVersion                   = 1
KekFingerprint               = fixture-fingerprint
WrappedKeyMetadataDigest     = bytes 0x00..0x1f
test DEK                     = bytes 0xc0..0xdf
plaintext                    = ASCII abc
DATA nonce                   = 00a1a2a3a4a5a6a7a8a9aaab
ContentCommitment            = bytes 0xe0..0xff
FINAL nonce                  = 01b1b2b3b4b5b6b7b8b9babb
```

Expected values:

```text
HeaderLength                     = 208
ObjectHeaderDigest               = b5aa36aec08187300564247f29e6f04605021861f68fa5f935a5a43fe0729e10
DATA ciphertext                  = 82b28a
DATA tag                         = 31639cf9eb1c50489f9a2980da8044bf
DataCiphertextDigest             = 1c5f69573a7e8599a420a949340ef52642bd2bf7fb3fbbeaeb462a2c28a95024
AuthenticationChunkCommitment    = 33210073ab93895bd564de97ad5ac6d1a78f6351a9ce86cd7a7e9684b1bdd4f6
EncryptionEnvelopeMetadataDigest = aac48eda26c784318ef49cd1a6ca9ee901eaf1a76e1d0d32c88e13a73854f338
CompletionPlaintextLength        = 181
FINAL ciphertext                 = 71496650f485d2a4741e8fb02c6060a50a5b4b863fb16774061b7e6ff7e48deb83453ccacee26637e984b4352134935aa300d1e41af5bfc6ebef0d94cc9cc808cf153baadf01428dc56d9aec657f132cab0839793009b9bed3cea2363120fbab7c61ab11ecc84b04b38fc1da6827a0578b745b7e6f4eef00cc1e76f67cb52d5b0926a7f2cc9d6dfb42a099a170b96fdd662c9fa7847fbb3e2108d288f56ee2ef4db41cb5cf22ab63d4c400c5ca7cc123a2ca9a332b
FINAL tag                        = 6275f608220e9b58e6c881a56ed604b3
CompleteObjectLength             = 462
CompleteObjectDigest             = e2bdff09933146098393a45537023e0e7460fe396a9d6fed078d8768c4fc9c06
```

The build test must recompute these values through the production codec and a
structurally independent oracle. R202's common-mode mutation changes both
encoders while the absolute values above remain fixed.

### 5.9 Complete verification rule

An object is authentic and complete only when all of the following hold:

- header fields equal the frozen DB/object context;
- ordinals are contiguous and frame lengths obey section 5.2;
- every DATA and FINAL AEAD operation verifies under the active historic
  attempt-key reservation;
- observed plaintext length and historic content commitment match;
- DATA digest/length, tag-chain and both metadata digests match the FINAL
  plaintext;
- `ChunkCount`, `TotalDataPlaintextLength`, completion length and
  `Finalized=0x01` match;
- EOF is exact.

Failure is fail-closed and returns no plaintext to the caller.

## 6. Internal C# surface

No HTTP, production Raw BIO or general-purpose byte API contract is added. The
only public code-contract evolution is the operation-scoped typed verification
result in section 1.3; all R2 orchestration, codec and result types are internal.

```text
RawExportR2EncryptionOrchestrator
RawExportR2FramedCiphertextStream
RawExportR2CompletionVerifier
RawExportR2FrameCodec
RawExportR2Repository
```

The writer result contains only:

```text
AttemptId
ObjectCustodyId (NULL for every PreCustody* disposition)
ObjectState (NULL for every PreCustody* disposition)
StateRevision (NULL for every PreCustody* disposition)
Disposition = PreCustodyRejected | PreCustodyRetryable
            | PreCustodyTerminalKey | PreCustodyOperatorRequired
            | PendingVerification | ReconciliationRequired
            | NotArmed | CustodyStateConflict
```

The verifier result contains only:

```text
AttemptId
ObjectCustodyId
VerifiedPlaintextLength
ContentCommitment
CiphertextLength
CiphertextDigest
VerificationEvidenceDigest
Disposition = Verified | VerificationIndeterminateRetry | VerificationFailedRequiresCleanup
```

Verified metadata fields are non-NULL only for `Verified`. Both failure
dispositions return them as NULL and expose only the attempt/object identity
needed for the durable handoff.

No result or `ToString()` contains plaintext, transient plaintext digest,
wrapped key bytes, DEK, nonce seed, provider credential or receipt text.

The source stream is an internal caller-owned argument used by the fixture
proof. It is not registered as a production Raw BIO provider. Production R2
activation remains impossible until a separate secure-source adapter is
ratified and landed.

## 7. Failure and precedence contract

Before object custody exists:

```text
request argument invalid -> PreCustodyRejected without key provisioning
key provisioning InProgress/ProviderUnavailable/ProviderOutcomeUnknown
  -> PreCustodyRetryable
key provisioning Terminated + locked/advisory Revoked|ReservationAbandoned
  -> PreCustodyTerminalKey -> reconciliation termination handoff
key provisioning conflict/head/corrupt/state or non-terminal Terminated detail
  -> PreCustodyOperatorRequired
actor/context or attempt/head/binding invalid -> PreCustodyRejected
profile/class/ciphertext bound invalid -> PreCustodyRejected
All pre-custody paths leave no object row, call no NotArmed transition and perform
zero object-provider calls.
```

After Begin commits but before object arm:

```text
local cancellation/failure while exact writer ownership remains
-> landed RecordNotArmed
-> NotArmed with zero object-provider calls

landed Arm returns StateConflict or NotFound
-> CustodyStateConflict with zero object-provider calls
-> no false NotArmed claim
```

After object arm:

```text
source read / AEAD / commitment / transport / cancellation uncertainty
-> landed OutcomeUnknown
-> ReconciliationRequired
```

During exact-object verification, infrastructure uncertainty and the exact
typed `KeyAccessIndeterminate` AEAD outcome remain
`ObjectPresentPendingVerification` as `VerificationIndeterminateRetry`.
Only deterministic invalidity, including the exact typed
`AuthenticationFailed` AEAD outcome, may become
`VerificationFailedRequiresCleanup`. No post-arm exception is translated to
positive absence. No content mismatch is translated to `Created` or
`VerifiedCompleted`. Exact landed SQLSTATE, object outcome and readiness code
contracts remain unchanged.

## 8. Exact proof manifest

File:
`tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs`.

| ID | Exact test method | Scratch mutation that must make it RED |
| --- | --- | --- |
| R201 | `R201_identifiers_projections_acl_and_63_byte_round_trip_are_exact` | add one 64-byte intended identifier; grant opposite capability; alter one projection type/order |
| R202 | `R202_frame_codec_golden_vectors_are_absolute_and_independently_recomputed` | common-mode swap one header/AAD/final field in codec and test helper; absolute vector differs |
| R203 | `R203_zero_one_and_multiple_chunk_shapes_have_exact_length_and_eof` | admit zero chunks; make one non-final chunk short; append one byte |
| R204 | `R204_data_and_final_nonce_namespaces_are_disjoint_and_duplicates_fail_closed` | use the DATA prefix for FINAL or accept a duplicate DATA suffix |
| R205 | `R205_historic_commitment_must_match_before_final_frame_is_emitted` | emit FINAL before fixed-time length/commitment comparison |
| R206 | `R206_transient_plaintext_digest_is_not_returned_persisted_or_logged` | create a digest-bearing managed string or add the raw/encoded digest to a result, entity property or structured log field |
| R207 | `R207_fresh_key_provisioning_precedes_active_context_and_partitions_every_outcome` | read the active-only projection before `ProvisionAsync`; collapse a retryable, terminal-key or operator-required outcome; trust `Terminated` without exact inspect; instantiate a key/object provider directly; call conditional put twice; or accept a tuple differing from the exact landed profile; every pre-custody outcome leaves zero object rows/provider calls |
| R208 | `R208_created_write_hands_off_for_verification_without_writer_read` | let writer call inspect/open read or mark verified |
| R209 | `R209_unknown_and_conditional_conflict_never_reencrypt_same_attempt` | invoke the framed stream or writer a second time after uncertainty |
| R210 | `R210_complete_existing_object_is_verified_without_reencrypt` | call encrypt operation during reconciliation |
| R211 | `R211_typed_aead_stage_keeps_key_uncertainty_pending_and_auth_failure_requires_cleanup` | return untyped bytes/throw-through from the AEAD boundary; map unwrap failure to `AuthenticationFailed`; map GCM tag failure to `KeyAccessIndeterminate`; bypass one parser/ordinal/EOF predicate; or map provider/read/commitment/database uncertainty to cleanup, one at a time |
| R212 | `R212_verified_transition_binds_exact_object_frame_and_content_evidence` | accept a NULL/different verification digest or wrong object revision |
| R213 | `R213_no_database_transaction_spans_source_key_aead_or_object_io` | retain an EF/Npgsql transaction while entering any external/stream boundary |
| R214 | `R214_r2_owned_buffers_are_zeroized_and_source_stream_remains_caller_owned` | skip zeroization of raw digest, encoded digest/payload or frame scratch on success/exception, or dispose caller stream |
| R215 | `R215_termination_is_append_once_on_exact_revision_fence_object_or_terminal_key_evidence` | remove fence/revision/object-state predicate; permit zero-object termination without locking the same key row in `Revoked|ReservationAbandoned`; admit `ProviderCorruptOrUnverifiable`/`AbandonRequested`; allow second disposition; or mutate a non-termination column |
| R216 | `R216_reconciliation_can_terminate_but_cannot_allocate_replacement_attempt` | grant/use complete-R1 or attempt INSERT from reconciler graph |
| R217 | `R217_apply_down_reapply_restores_guard_constraint_functions_and_acl` | omit one Down restore/revoke/drop |
| R218 | `R218_minio_restart_recovers_and_verifies_exact_single_part_object` | replace exact-object recovery with process memory or re-encryption |

Architecture file:
`tests/TagEkyc.ArchTests/Tip88C1B2R2DurableCustodyEncryptionArchTests.cs`.

| ID | Exact test method | Scratch mutation that must make it RED |
| --- | --- | --- |
| R2A1 | `R2A1_r2_has_no_key_object_role_readiness_or_multipart_implementation` | add forbidden key/object DDL, provider/role/readiness type or multipart SDK symbol to one R2 production file |
| R2A2 | `R2A2_writer_and_verifier_use_only_non_assignable_operation_scoped_capabilities` | inject a broader key/object interface, make writer depend on reconciler, or restore an untyped verification return that cannot distinguish key-access uncertainty from authentication failure |
| R2A3 | `R2A3_r2_contracts_and_logs_expose_no_raw_or_key_material` | make one internal result public or expose a prohibited field/ToString value |
| R2A4 | `R2A4_no_production_raw_source_registration_or_runtime_activation_is_added` | add a public production-registration extension for the fixture source/R2 orchestrator in an R2 production file |

Every mutation is run one at a time, observed RED for the named assertion,
restored byte-identically, and followed by a positive control. A green mutation
is vacuous and blocks closeout. Shared mutations may satisfy multiple cells
only when the exact shared executable branch and every affected assertion are
reported.

## 9. Exact implementation allowlist for a later controlled build

Any required permanent path outside this list is STOP/RRI before editing.

```text
src/TagEkyc.Contracts/RawExport/AttemptAeadOperationContracts.cs
src/TagEkyc.Infrastructure/RawExport/AttemptAeadOperationService.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2Contracts.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2FrameCodec.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2FramedCiphertextStream.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2EncryptionOrchestrator.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2CompletionVerifier.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2Repository.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceEncryptionAttemptRow.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260807120000_Tip88C1B2R2DurableCustodyEncryption.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260807120000_Tip88C1B2R2DurableCustodyEncryption.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs
tests/TagEkyc.ArchTests/Tip88C1B2R2DurableCustodyEncryptionArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_r2_as_built.md
```

The E3 file may change only its expected additive ModelSnapshot SHA-256
constant after the snapshot delta is proved additive. The two landed AEAD files
may change only as specified in section 1.3; encryption behavior and every
other landed DK-PROD source remain frozen. No landed DK-PROD,
DK-FIXTURE-PROOF or DURABLE-OBJECT migration, test, dispatch or as-built file is
editable in this slice. No `.csproj`, `Program.cs`, appsettings or deployment
file is in the allowlist.

## 10. Task-0 and validation

Before implementation authority can be used, a future dispatch must require:

1. exact HEAD and `src/`/`tests/` cleanliness against the ratified baseline;
2. hashes of this dispatch and all four landed anchors in section 0;
3. Release build and complete unfiltered baseline suite;
4. pending-model clean using Infrastructure as project and startup project;
5. PostgreSQL 16, the landed pinned MinIO image and landed key fixture
   prerequisites healthy;
6. catalog/ACL/role/membership snapshot before the R2 migration;
7. exact 17-path allowlist confirmation and staged paths zero.

Post-build validation must include:

- Release build with zero warnings/errors;
- pending-model clean and E3 snapshot tripwire green;
- R201-R218 and R2A1-R2A4 positive controls;
- every named mutation RED and byte-identical restoration;
- migration apply/Down/reapply with exact catalog/ACL/guard equivalence;
- key/provider/process/DbContext and MinIO restart reconstruction;
- complete unfiltered suite with real passed/failed/skipped census;
- database scan proving no plaintext/ciphertext/transient digest column or row;
- structured-log/result scan proving no plaintext digest/key/raw bytes;
- zero task-created container, volume, bucket, object, role, credential,
  process and scratch-database residue;
- `git diff --check`, staged paths zero and unrelated dirt untouched.

The report may state only:

```text
PASS - R2 PROOF BUILD READY FOR INDEPENDENT CLOSEOUT REVIEW
```

It may not claim production readiness, real Raw BIO availability, R3 staging,
delivery, commit or push authority.

## 11. STOP/RRI conditions

Stop before implementation or further edit if:

- a landed key/object function, table, provider, role, readiness code or
  migration must change; the sole landed-source exception is the exact two-file
  typed AEAD verification evolution in section 1.3;
- multipart, versioning, Object Lock, provider list or same-attempt resume is
  required;
- exact ciphertext length cannot be known before the single put;
- the current `IContentCommitmentService` cannot reproduce the frozen
  commitment from the transient SHA-256 payload in section 5.4;
- either raw class can exceed its landed plaintext limit or the 128 MiB object
  limit;
- verification requires returning plaintext or plaintext digest outside the
  verifier;
- a production raw-source adapter or runtime activation is required;
- a path outside section 9 is required;
- any existing migration/snapshot byte would need manual alteration rather
  than the one additive migration and generated Designer/Snapshot delta;
- a named mutation remains green or fails for collateral reasons.

## 12. Current disposition

V4.2 is a review candidate only. It reconciles R2 to the landed durable-key and
single-part durable-object foundations and removes the obsolete v3 greenfield
design. Independent review and explicit Homeowner ratification are required
before any controlled build dispatch may be prepared or executed.
