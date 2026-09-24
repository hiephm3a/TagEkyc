# TIP-88C1-B2-R3 — Verified Ciphertext Staging — BUILD DISPATCH

**Version:** 0.4-review-candidate  
**Status:** DRAFT FOR INDEPENDENT REVIEW — NOT RATIFIED — NOT DISPATCHED  
**Date:** 2026-08-10  
**Repository:** `D:\Task\Remote Signing\TagEkyc`  
**Required branch:** `tip-88a-raw-export-policy-catalog-build`  
**Baseline:** `5c0be51d838b65d58d4026713148321b0d64e42b`  
**Risk tier:** High-risk  
**PI-TAG-001:** ACTIVE for TIP-88C1; round-5 checkpoint and round-10 hard stop apply
**Review round:** 4

This artifact is documentation-only until an independent clean review and an
explicit Homeowner ratification bind this exact file SHA-256. It does not
authorize implementation, migration execution, staging, commit, push, merge,
PR, deployment, production activation, real Raw BIO, a production raw-source
adapter, R4 promotion, R5 `Available`, R6 cleanup, package assembly or delivery.

## Changelog

### v0.4 — Post-lock admission-time correction

- Replaced the transaction-start timestamp with one post-blocking
  `clock_timestamp()` captured only after both authority and consent locks are
  held.
- Required authority, consent validity windows, reservation/source deadlines
  and `StagedAtUtc` to use that exact admission timestamp.
- Added count-neutral authority-lock-wait and consent-lock-wait expiry
  discriminators to R306/R307; a mutation reverting to pre-wait
  `transaction_timestamp()` must RED for the admission outcome.
- Preserved the 16-proof census and exact 14-path implementation allowlist.

### v0.3 — Second-review reconciliation

- Added the exact shared advisory authority lock on the new-stage path and a
  revoke-first/stage-first discriminator; exact replay remains lock-free with
  respect to fresh authority.
- Narrowed R313 to forbid external/provider/key/AEAD/HMAC/plaintext/service
  edges while explicitly allowing PostgreSQL I/O and the pure deterministic
  SHA-256 calculation owned by the staged-fingerprint codec.
- Recorded the repo-real CORE `Up()` resolver shape to prevent its earlier
  `Down()` restoration shape from being mistaken for the active baseline.
- Preserved the 16-proof census and exact 14-path implementation allowlist.

### v0.2 — First-review reconciliation

- Corrected the absolute vector's displayed `ContentCommitment` to the intended
  sequential `e0..ff` bytes; the independently recomputed 489-byte digest is
  unchanged.
- Added an exact key-reservation row lock and concurrent revoke discriminator
  so a new stage cannot race an `Active → Revoked` transition.
- Split immutable replay bindings from new-stage-only `Active` and
  `VerifiedCompleted` prerequisites; a response-loss replay remains an
  observation of the frozen staged result.
- Corrected the landed actor GUC name and made the one stable transaction-time
  source explicit across authority, consent, deadlines and `StagedAtUtc`.
- Assigned count-neutral mutation bites to occupied-downgrade protection and
  single-timestamp use, and closed the R313/R316 invariant-trace gap.
- Preserved the 16-proof census and exact 14-path implementation allowlist.

### v0.1 — Initial R3 executable contract

- Re-anchored R3 to the landed R2 commit `5c0be51d...`.
- Restricted the slice to the short transaction that consumes a durable
  `VerifiedCompleted` R2 object and publishes exactly one `Staged` attempt.
- Reconciled the planning-era staged fingerprint with the landed
  never-versioned Durable Object contract by defining the versioned
  `StagedCiphertextFingerprint` v2 codec.
- Made the database derive staged actuals from locked durable rows; no caller
  may supply a digest, commitment, evidence digest or staged fingerprint.
- Pinned fresh authority/consent revalidation, replay precedence, one-way
  attempt/head writes, exact ACL ownership, migration rollback and mutation
  proofs.
- Added the PI-TAG-001 invariant, outcome/precedence, shape/nullability,
  ordering and test-bite matrices.

## 0. Source-of-truth register and baseline facts

The following bytes were verified while drafting this version:

| Artifact | SHA-256 | Authority consumed by R3 |
| --- | --- | --- |
| `tip_88c1_planning_brief.md` | `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` | R3 short transaction, fresh authority, one staged attempt, no bare digest |
| `tip_88c1_b2_r2_build_dispatch.md` | `9A758E07D3283017AD84E489072F3EB7782EEFE526B1999DD14A53515633FFED` | framed ciphertext and typed verification contract |
| `tip_88c1_b2_r2_as_built.md` | `760403BCF6434119B83D9FD0DE104867D6A92998071CF53257DD1FCFE1EAF42D` | landed R2 evidence and `VerifiedCompleted` handoff |
| `tip_88c1_b2_durable_key_prod_as_built.md` | `AAF9E89A20C5BFF09C3A69DE7239BF677FE4665746AFB09AA732A69BCF1F89E7` | durable key reservation and capability graph |
| `tip_88c1_b2_durable_key_fixture_proof_as_built.md` | `8BC73AC4DF7280CE53BE5F6A4FF91AB369660A15D96923F8A3B6A1F2F28CC896` | fixture-only restart-safe key proof |
| `tip_88c1_b2_durable_object_custody_build_dispatch.md` | `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918` | single-part conditional object and never-versioned topology |
| `tip_88c1_b2_durable_object_custody_as_built.md` | `B9E8873F45F826DC8889B01A23E9393F81A12B75F587E87153BDE34F71536A6E` | landed object rows, functions, roles and provider behavior |

Relevant landed code anchors at the baseline are:

```text
61DC7373AB8941A1EC076F3FAEA6E57054BD80DB18877E980F42277EC74C9A62  RawExportSourceEncryptionAttemptRow.cs
66D7EB2B5672211AA83AEF8B4472AAAF51AA687226E7934C321D9C6DEFFFB154  RawExportSourceHeadRow.cs
087D7D9DE0469E913BAD226EFB466FAF5E1DEA8E669A7E43A07717D026027A9F  RawExportProvisionalObjectRow.cs
E3D7EF8BBC55F381D7465530422556C4E9F21BC2294A272EB0F0123043592E4C  RawExportR2Contracts.cs
0A18785B2BDCB3530DC6881D4561BB27CF17B3A7727A199A19DB42402BE8558A  RawExportR2CompletionVerifier.cs
7BE40AC01FE99E5A74B8219258B450C76632906205A06C2D429C7F2514674BF9  RawExportR2Repository.cs
E8DAD1649ACF9D57F654F8D5EFA7874D777D276EA1B00604A983C0016D9BCC2C  RawExportR2FrameCodec.cs
1EF43A74562B62C14DC1F6667689260ECB30928B7B2DF7E2A8C10389DF9A4F34  TagEkycDbContext.cs
1AC9DB8F7CB790AF51459D4886BDA26B510CF9F73C16D2B9A27B4FEE25A12BC1  TagEkycDbContextModelSnapshot.cs
```

The active resolver shape is the `Up()` re-emission in
`20260731082733_Tip88C1B2CoreNewCandidate.cs`. It includes
`ConsentPolicyId uuid` and `ConsentPolicyVersion integer`. The later definition
in that same source file belongs to `Down()` and intentionally restores the
older pre-CORE shape without those columns; it is not the baseline function
consumed by R3.

The active fixture-only GOV/ART chain ends in
`tip_88c1_gov_art_authorization_packet_ratification_v0_7.md` with status
`RATIFIED_ACTIVE`. It authorizes only generated non-patient Gate-A fixture
evidence under separately ratified build contracts. It is not real-artifact or
production authority.

## 1. TIP Analytical Summary / Intent Ledger

### Intent

Convert one exact, durably authenticated R2 result into one immutable staged
ciphertext identity and atomically advance the source head from `Reserved` to
`Staged`.

### Expected Outcome

After the slice:

```text
exact current source attempt
+ exact durable object in VerifiedCompleted
+ exact durable R2 verification evidence
+ fresh current authority and exact consent policy
+ current reservation revision/fence
→ one immutable staged result
→ head Reserved → Staged
```

A response-loss retry returns `ExistingMatch` from durable state. A stale,
mismatched, unauthorized or non-verified input causes zero staged residue. R3
uses PostgreSQL metadata I/O and pure staged-fingerprint SHA-256 only. It
performs no S3/provider, external key provisioning/unwrap, AEAD,
HMAC/content-commitment, plaintext or non-database service I/O.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Consume only durable `VerifiedCompleted` | R2 already authenticates frames, historic commitment and exact object, then persists its evidence | R3 is a metadata/CAS transaction | R3 does not independently decrypt ciphertext |
| Derive every staged value inside the SECURITY DEFINER function | Prevents process-memory or caller-supplied digest authority | Command carries only identity and expected CAS values | Caller input is not evidence |
| Reuse `tagekyc_raw_export_reconciler` | R3 is the reconciliation handoff already assigned by C1 planning | No new role or credential graph | No API/runtime access |
| `Reserved → Staged`; no synthetic `Encrypting` transition | Landed R1/R2 never introduced an `Encrypting` head state | One additive head-state transition only | Does not erase the planning concept for a future replacement-attempt design |
| Version-bump staged fingerprint to v2 | Landed object custody prohibits versioning and intentionally removed `VersionId` | Replaces an uninstantiated planning-era v1 formula | No v1 row is reinterpreted or migrated |
| Exact same-command replay precedes fresh authority | Replaying a committed staging result is observation, not new publication | Response-loss recovery remains deterministic without admitting a different CAS command | R4/R5 remain blocked by later authority loss |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Accept `RawExportR2VerifierResult` digests as R3 command input | Rejected | Process-local result must not become durable authority | Use locked object/attempt/reservation rows |
| Re-run decryption or historic commitment verification in R3 | Rejected | Duplicates R2 and lengthens the transaction | R2 `VerifiedCompleted` plus evidence is the sole byte-verification gate |
| Keep `ProviderObjectVersion` in staged fingerprint v1 | Rejected | Versioning and `VersionId` are prohibited by Durable Object | v2 codec below; heavy-media/versioned topology requires separate ratification |
| Persist bare actual plaintext digest | Rejected | Violates C1 data boundary | Never add to schema/log/result/audit |
| R4 promotion, R5 `Available`, R6 cleanup | Deferred | Separate external side effects and publication semantics | Subsequent controlled slices |
| Real Raw BIO/source adapter | Deferred | Current proof uses generated non-patient plaintext | ART/legal/adapter authorization and separate build |
| Resolver/API/package/delivery surface | Deferred | `Staged` is deliberately non-readable | R4/R5, C2 and C3 |

### Debt / Gap Impact

| Debt/gap | Action in R3 | Result | Carry-forward gate |
| --- | --- | --- | --- |
| Planning-era staged fingerprint references provider version | Supersede only the uninstantiated formula with v2 | Executable under never-versioned storage | Homeowner ratification of this exact dispatch |
| No production raw-source adapter | No action | Synthetic evidence only | Separate real-source adapter ratification |
| No `Available` descriptor | Preserve | R3 remains non-readable | R4/R5 build contracts |
| R2A1/R2A3/R2A4 source-text hardening debt | Preserve unchanged | Non-blocking explicit debt remains | Existing R2 closeout disposition |

### Non-Claims

This slice does not prove or authorize production readiness, real-patient Raw
BIO custody, resolver readability, package completeness, recipient encryption,
delivery, purge completeness, legal/DPO approval, production MinIO, AWS-hosted
infrastructure or deployment.

### Dispatch Readiness

Implementation is not allowed while this document remains a review candidate.
After a clean independent review, only an explicit Homeowner ratification of
this exact SHA may authorize the later build. The exact implementation
allowlist is section 10. Any required path outside it is STOP/RRI before edit.

## 2. Ownership and non-ownership

### 2.1 R3 owns exactly

1. A single SECURITY DEFINER staging function callable only by the landed
   reconciliation capability.
2. One-way nullable post-R1 staging fields on the exact encryption-attempt row.
3. The head-state expansion from `Reserved` to `{Reserved, Staged}` and the
   atomic `Reserved → Staged` CAS.
4. The `StagedCiphertextFingerprint` v2 codec and absolute vector.
5. Exact replay, authority/consent barrier, ACL, rollback and mutation proofs.
6. An internal repository/service result. There is no HTTP, public DTO, queue
   or general-purpose staging API.

### 2.2 R3 consumes unchanged

- R1 source reservation, head, attempt and frozen authority/key/framing data.
- R2 framing, typed AEAD verification and historic commitment verification.
- Durable Object `VerifiedCompleted`, `ObjectBindingDigest`, ciphertext,
  provider-receipt and verification-evidence fields.
- DK-PROD key lifecycle and all existing capability roles/memberships.
- The exact authority and consent resolvers already landed.

### 2.3 R3 must not own

- provider GET/HEAD/PUT/DELETE or any S3 client;
- key provisioning, unwrap, AEAD, HMAC or plaintext streaming;
- attempt replacement, owner reclaim, lease renewal or R2 termination;
- object promotion, committed locator, resolver visibility or `Available`;
- cleanup, quarantine, delete or key revocation;
- raw-source capture/adaptation, package assembly or delivery;
- public/runtime/controller/endpoint registration.

## 3. Reconciliation of planning-era semantics with landed R2/object custody

### 3.1 Durable verification authority

The landed R2 verifier performs the byte work before R3:

```text
exact object read
→ frame/header/final-record validation
→ typed AEAD verification
→ plaintext length and historic keyed commitment recomputation
→ ciphertext length/digest validation
→ verification evidence digest
→ object State = VerifiedCompleted
```

Therefore R3 must not accept a second process-local verifier result as truth.
It locks the object row and requires all of:

```text
State = VerifiedCompleted
CiphertextLength is present and in the landed bound
CiphertextDigest is exactly 32 bytes
ProviderReceiptDigest is exactly 32 bytes
VerificationEvidenceDigest is exactly 32 bytes
VerifiedAtUtc is present
```

`CONTENT_COMMITMENT_MISMATCH` remains a deterministic R2 verification failure.
Such an object never reaches `VerifiedCompleted` and therefore cannot enter R3.
R3 does not relabel that failure as staged or claim to recompute it.

### 3.2 `StagedCiphertextFingerprint` v2

The planning-era v1 formula included `ProviderObjectVersion`. The landed
single-part object contract prohibits versioning/Object Lock and removed
`VersionId`; retaining that field would make the codec unsatisfiable or invite
a fabricated sentinel.

No v1 staged fingerprint has ever been persisted. Upon Homeowner ratification,
this dispatch supersedes only that uninstantiated formula with schema version
2:

```text
StagedCiphertextFingerprintSchemaVersion = 2

StagedCiphertextFingerprint =
  C1HashCanonical("tip-88c1-staged-ciphertext-v2", {
    lower_hex(EncryptionAttemptFingerprint),
    lower_uuid_n(ObjectCustodyId),
    lower_hex(ObjectBindingDigest),
    invariant_decimal(VerifiedPlaintextLength),
    lower_hex(ContentCommitment),
    invariant_decimal(StagedCiphertextLength),
    lower_hex(StagedCiphertextDigest),
    lower_hex(StagedProviderReceiptDigest),
    lower_hex(StagedVerificationEvidenceDigest)
  })
```

Encoding is the landed `C1HashCanonical` profile: NFC text, then a signed
32-bit big-endian UTF-8 byte length and UTF-8 bytes for the domain and every
ordered field. UUIDs are 32 lowercase hex characters without hyphens. Byte
arrays are lowercase hex. Integers are unsigned/non-negative invariant decimal
without sign, grouping or leading zeroes except the literal `0`.

The v2 fields retain the planning invariant:

- `EncryptionAttemptFingerprint` binds source reservation, key, suite,
  framing, nonce domain and provisional identity;
- `ObjectCustodyId` and `ObjectBindingDigest` bind the exact durable object;
- the verified plaintext length and stored keyed `ContentCommitment` bind the
  authenticated content claim without persisting a bare digest;
- ciphertext length/digest bind exact bytes;
- provider receipt binds the landed write/reconcile result;
- verification evidence binds R2 header/data/final-record, authentication
  chain, envelope metadata and wrapped-key metadata.

No mutable object state revision, timestamp, lease or head revision enters the
fingerprint.

### 3.3 Absolute v2 vector

```text
EncryptionAttemptFingerprint = 000102030405060708090a0b0c0d0e0f
                               101112131415161718191a1b1c1d1e1f
ObjectCustodyId               = 22222222222222222222222222222222
ObjectBindingDigest           = 202122232425262728292a2b2c2d2e2f
                               303132333435363738393a3b3c3d3e3f
VerifiedPlaintextLength       = 3
ContentCommitment             = e0e1e2e3e4e5e6e7e8e9eaebecedeeef
                               f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff
StagedCiphertextLength        = 462
StagedCiphertextDigest        = e2bdff09933146098393a45537023e0e7
                               460fe396a9d6fed078d8768c4fc9c06
StagedProviderReceiptDigest   = 404142434445464748494a4b4c4d4e4f
                               505152535455565758595a5b5c5d5e5f
StagedVerificationEvidenceDigest
                              = 606162636465666768696a6b6c6d6e6f
                                707172737475767778797a7b7c7d7e7f

Canonical preimage length     = 489 bytes
StagedCiphertextFingerprint   = 6b7b1ce324d3ce6687c33a6d1aa6a58f
                               90e17107ebc6133247e486cab7144fc5
```

The test oracle must independently construct the 489 bytes. It must not call
the production codec to obtain the expected digest. A common-mode mutation of
both SQL and C# field order must still turn the absolute assertion RED.

## 4. Exact persisted shape

### 4.1 Attempt staging fields

Add these nullable fields to
`tagekyc.raw_export_source_encryption_attempts` and its EF entity:

| Field | Type | Pre-stage | Staged |
| --- | --- | --- | --- |
| `StagedCiphertextFingerprintSchemaVersion` | integer | NULL | exactly `2` |
| `StagedCiphertextFingerprint` | bytea | NULL | exactly 32 bytes |
| `StagedObjectCustodyId` | uuid | NULL | exact locked object id |
| `StagedObjectStateRevision` | bigint | NULL | exact `VerifiedCompleted` predecessor revision, `>=1` |
| `StagedFromReservationRevision` | bigint | NULL | exact head predecessor revision, `>=1` |
| `VerifiedPlaintextLength` | bigint | NULL | exact reservation claimed length, `>=1` for the initial still-image classes |
| `StagedCiphertextLength` | bigint | NULL | exact object length, `1..134217728` |
| `StagedCiphertextDigest` | bytea | NULL | exactly 32 bytes |
| `StagedProviderReceiptDigest` | bytea | NULL | exactly 32 bytes |
| `StagedVerificationEvidenceDigest` | bytea | NULL | exactly 32 bytes |
| `StagedAtUtc` | timestamptz | NULL | one post-lock admission `clock_timestamp()` shared with authority/consent windows and both strict deadlines |

The existing attempt check becomes a total two-shape predicate:

```text
all eleven staging fields NULL
OR
all eleven staging fields present and valid
```

There is no partially staged shape. Existing R2 termination fields remain
unchanged and must both be NULL on a staged attempt.

The migration expands the existing named constraint
`ck_raw_export_source_attempt_values` (35 UTF-8 bytes); it does not create a
second overlapping staging-shape check. It also adds the nullable, restrictive
foreign key:

```text
fk_raw_export_source_attempt_staged_object (42 UTF-8 bytes)
  raw_export_source_encryption_attempts.StagedObjectCustodyId
  → raw_export_provisional_objects.ObjectCustodyId
  ON DELETE RESTRICT
```

NULL pre-stage rows remain legal. A present staged object id must reference the
exact durable row; no unbound UUID can satisfy the staged shape.

### 4.2 Head shape

The existing named constraint `ck_raw_export_source_head_values` (32 UTF-8
bytes) becomes:

```text
CustodyState IN ('Reserved','Staged')
AND ReservationRevision >= 1
AND Fence >= 1
```

R3 changes only:

```text
CustodyState:        Reserved → Staged
ReservationRevision: old → old + 1
```

`SourceArtifactId`, `CurrentEncryptionAttemptId` and `Fence` remain byte/value
identical. R3 never creates a new attempt or fence.

### 4.3 One-way write guards

The R3 migration must `CREATE OR REPLACE`, not edit landed migration bytes:

- `tagekyc.enforce_raw_export_source_core_write()` to retain landed R1 INSERT
  and R2 termination UPDATE branches and add one exact R3 staging UPDATE branch
  on `raw_export_source_encryption_attempts`;
- `tagekyc.enforce_raw_export_source_head_write()` to retain landed R1 behavior
  and add one exact R3 `Reserved → Staged` branch.

The staging function sets a transaction-local, captured-and-restored context
literal:

```text
tagekyc.raw_export_source_core_write_context = tip88c1-r3-stage-v1
```

Both guards require `current_user = tagekyc_raw_export_deployer`, the exact
context and exact table. The attempt guard permits NULL-all → present-all once
and compares every non-staging column `IS NOT DISTINCT FROM OLD`. The head
guard permits only the two fields above and compares every other field. DELETE
remains forbidden. Context restoration is required on success and exception.

`Down()` restores the exact landed R2 guard bodies and the Reserved-only head
check/guard, drops the staging foreign key before removing R3 fields/function,
and preserves all landed roles.
Because a `Staged` row cannot be truthfully represented by the pre-R3 schema,
`Down()` first requires zero `Staged` heads and zero attempts with any staging
field present. An occupied downgrade fails closed; it never erases or
reinterprets staged evidence. Apply/Down/reapply proof runs only after its
isolated fixture rows have been removed through test teardown.

## 5. Exact staging function

The migration adds exactly:

```sql
tagekyc.raw_export_stage_verified_source_ciphertext(
    p_attempt_id uuid,
    p_object_custody_id uuid,
    p_expected_reservation_revision bigint,
    p_expected_encryption_attempt_revision bigint,
    p_expected_fence bigint,
    p_expected_object_state_revision bigint
)
RETURNS TABLE(
    "Outcome" text,
    "SourceArtifactId" uuid,
    "AttemptId" uuid,
    "ObjectCustodyId" uuid,
    "StagedCiphertextFingerprintSchemaVersion" integer,
    "StagedCiphertextFingerprint" bytea,
    "ReservationRevision" bigint,
    "Fence" bigint,
    "StagedAtUtc" timestamptz
)
```

Properties:

```text
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
OWNER tagekyc_raw_export_deployer
EXECUTE tagekyc_raw_export_reconciler only
```

`PUBLIC`, `tagekyc_runtime`, claim broker, encryptor, lifecycle, posture and all
LOGIN roles have no direct EXECUTE unless the landed role-membership graph
reaches the one reconciliation capability exactly as already ratified. No table
grant is added.

### 5.1 Command authority

The caller supplies only:

- non-empty attempt/object ids;
- positive expected reservation/attempt/fence/object revisions.

The caller does not supply source id, state, commitment, object binding,
ciphertext metadata, evidence digest, timestamp or staged fingerprint. SQL
derives all of them from rows locked in the same transaction.

Invalid scalar arguments raise:

```text
SQLSTATE P0001
RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID
```

Actor absence/malformed GUC retains the landed actor failure and occurs before
row disclosure.

### 5.2 Lock and derivation order

The function uses one database transaction and this exact order:

```text
validate arguments
→ resolve exact actor through tagekyc.raw_export_current_actor()
→ lock attempt by AttemptId
→ lock source head by attempt.SourceArtifactId
→ lock source reservation and ingress claim
→ lock exact key reservation by (AttemptId, AttemptKeyReservationId)
→ lock exact object by ObjectCustodyId
→ prove all attempt/head/key/object identifiers and immutable fingerprints bind
→ evaluate exact-same-command existing staged replay
→ require expected reservation revision, attempt revision/fence and object revision
→ require head Reserved/current attempt; attempt not R2-terminated
→ require key Active and object VerifiedCompleted with complete evidence shape
→ acquire exact shared advisory authority lock for this source tuple
→ invoke exact consent resolver so its shared lock is acquired and retained
→ capture one post-blocking StageEvaluationTimestamp with clock_timestamp()
→ freshly resolve exact current authority at StageEvaluationTimestamp
→ revalidate returned consent identity/state/window at StageEvaluationTimestamp
→ enforce reservation/source deadlines at StageEvaluationTimestamp
→ derive v2 fingerprint from locked rows
→ set guarded write context
→ write attempt staging fields once
→ CAS head Reserved → Staged and increment revision exactly once
→ restore guarded context
→ return Staged
```

Allowed work inside the transaction is limited to PostgreSQL I/O and the pure,
deterministic SHA-256/`C1HashCanonical` calculation required for the v2 staged
fingerprint. There is no S3/provider I/O, key provisioning/unwrap, AEAD,
HMAC/content-commitment computation, plaintext I/O, external HTTP/network or
other service call. No connection is released between the locks and CAS.

### 5.3 Exact binding predicate

Before either replay or new-stage admission, SQL proves these immutable
bindings from rows held `FOR UPDATE`:

```text
head.SourceArtifactId                  = attempt.SourceArtifactId
head.CurrentEncryptionAttemptId        = attempt.AttemptId
head.Fence                             = attempt.Fence
object.ObjectCustodyId                 = requested ObjectCustodyId
object.AttemptId                       = attempt.AttemptId
object.AttemptKeyReservationId         = attempt.AttemptKeyReservationId
object.SourceArtifactId                = attempt.SourceArtifactId
object.ProvisionalObjectIdentity       = attempt.ProvisionalObjectIdentity
object.EncryptionAttemptRevision       = attempt.EncryptionAttemptRevision
object.AttemptFence                    = attempt.Fence
object.EncryptionAttemptFingerprint    = attempt.EncryptionAttemptFingerprint
key.AttemptId                          = attempt.AttemptId
key.AttemptKeyReservationId            = attempt.AttemptKeyReservationId
key.EncryptionAttemptFingerprint       = attempt.EncryptionAttemptFingerprint
```

The key lock is acquired before its disposition is interpreted and is retained
through commit/rollback. R3 reads no wrapped ciphertext, nonce, tag, key bytes
or provider credential.

An `ExistingMatch` replay is exact only when the complete persisted staging
shape is internally consistent and all predecessor values supplied by the
command equal the values frozen by the winning stage:

```text
requested ObjectCustodyId                 = attempt.StagedObjectCustodyId
expected reservation revision             = attempt.StagedFromReservationRevision
expected encryption-attempt revision      = attempt.EncryptionAttemptRevision
expected fence                            = attempt.Fence
expected object-state revision            = attempt.StagedObjectStateRevision
recomputed v2 fingerprint from locked rows = attempt.StagedCiphertextFingerprint
head.CustodyState                          = Staged
head.ReservationRevision                   = StagedFromReservationRevision + 1
```

A staged row with a partial shape, mismatched head/object evidence, or a
different predecessor command is `StateConflict`, never `ExistingMatch`.
Replay does not require the key still to be `Active` or the object still to be
in its predecessor `VerifiedCompleted` state: those are new-publication
prerequisites, while replay observes the already frozen staging result. It
does require the immutable object metadata used by the v2 fingerprint to
remain equal to the persisted staged projection.

Only after no exact replay exists does a new stage require:

```text
key.PreparationDisposition = Active
object.State = VerifiedCompleted
object complete VerifiedCompleted evidence shape
```

The `FOR UPDATE` key lock prevents an `Active → Revoked` commit between that
check and the attempt/head CAS. A revoker that wins the key lock first causes
`StateConflict`; a stager that wins holds the key stable through its commit.

### 5.4 Fresh authority and consent barrier

For a new stage, `StageEvaluationTimestamp` is not transaction-start time.
Every blocking prerequisite lock must be held first. The function executes
this exact order:

Before resolving current authority, and only after the exact replay
discriminator has found no committed match, acquire and hold through
commit/rollback:

```sql
pg_catalog.pg_advisory_xact_lock_shared(
  pg_catalog.hashtext(
    'tip88c1:b2-authority:' ||
    ClientApplicationId::text || ':' ||
    VerificationSessionId::text || ':' ||
    CaptureAcceptanceId::text || ':' ||
    RawClass))
```

The domain, separators, PostgreSQL UUID text encoding and `RawClass` are
byte-for-byte identical to the landed exclusive lock used by authority grant,
withdraw and revoke. The shared lock is not acquired on exact replay because
replay is observation of an already committed stage, not fresh publication.

Next call the landed three-argument consent resolver with the locked
reservation's policy id/version. That resolver acquires and retains the exact
shared consent advisory lock before it reads the current consent event. Its
internal `transaction_timestamp()` result is a prerequisite signal only; it is
not R3's final time authority.

Only after both authority and consent locks have returned does R3 capture once:

```sql
StageEvaluationTimestamp := pg_catalog.clock_timestamp();
```

No blocking lock may follow this capture. The already locked attempt, head,
reservation, key and object rows plus both advisory locks remain held through
commit/rollback.

Then call the current-authority resolver with the exact source claim keys and
the final admission timestamp:

```text
raw_export_resolve_current_authority_for_source(
  ClientApplicationId,
  VerificationSessionId,
  CaptureAcceptanceId,
  RawClass,
  StageEvaluationTimestamp)
```

Require the resolved authority to be current and to match the frozen
reservation exactly on:

```text
AuthoritySnapshotSchemaVersion
AuthoritySnapshotId
ControllerIdentity
StableDataScopeId
ConsentPolicyId
ConsentPolicyVersion
AbsoluteSourceExpiresAtUtc
ApprovedPurpose = SubjectRawBiometricExport
```

The two consent-policy fields above are present in the active CORE `Up()`
resolver return shape and are compared to the locked reservation; they are not
read from the older resolver restored only by CORE `Down()`.

The earlier consent result must now be revalidated at the final admission
timestamp. Require exact:

```text
State = Effective
VerificationSessionId = locked claim.VerificationSessionId
PolicyId = locked reservation.ConsentPolicyId
PolicyVersion = locked reservation.ConsentPolicyVersion
PurposeCode = SubjectRawBiometricExport
RecipientClientApplicationId = locked claim.ClientApplicationId
RawClass = locked claim.RawClass
ValidFromUtc <= StageEvaluationTimestamp
ValidUntilUtc IS NULL OR StageEvaluationTimestamp < ValidUntilUtc
```

Equality at `ValidFromUtc` is admitted; equality at `ValidUntilUtc` fails
closed. Also require:

```text
StageEvaluationTimestamp < ReservationExpiresAtUtc
StageEvaluationTimestamp < AbsoluteSourceExpiresAtUtc
```

Equality at either source deadline fails closed. The same
`StageEvaluationTimestamp` is persisted as `StagedAtUtc`; no second wall-clock
read is authoritative. R3 does not require the producer plaintext-retention
deadline because a complete R2 object is recoverable without the producer
buffer; it does require the source/authority staging deadlines above.

Failure returns `SourceRetentionNotAuthorized` with zero mutation. It does not
delete, quarantine, revoke or reveal the source. Those physical dispositions
remain lifecycle-owned.

## 6. Closed outcomes and precedence

### 6.1 Result union

The internal C# disposition is exactly:

```text
Staged
ExistingMatch
NotFound
StateConflict
SourceRetentionNotAuthorized
```

Shape:

| Outcome | Required fields | Forbidden/NULL fields |
| --- | --- | --- |
| `Staged` | all returned identity, schema `2`, fingerprint, new revision/fence, staged time | none |
| `ExistingMatch` | same complete durable staged projection | none |
| `NotFound` | `Outcome` only | every other field |
| `StateConflict` | `Outcome` only | every other field |
| `SourceRetentionNotAuthorized` | `Outcome` only | every other field |

No result contains raw bytes, plaintext digest, object key, storage endpoint,
credential, wrapped key, lease owner or internal policy detail. `ToString()` is
redacted.

### 6.2 Precedence matrix

| Competing condition | Higher-to-lower precedence | Result | Durable residue |
| --- | --- | --- | --- |
| invalid argument and any row state | argument/actor first | exception | none |
| missing attempt/head/reservation/object/key | existence/binding before authority | `NotFound` only for absent primary attempt/object; otherwise `StateConflict` | none |
| exact same-command committed replay and later authority loss | validate the complete staged/head/object shape and predecessor command, then replay before fresh authority | `ExistingMatch` | unchanged staged rows |
| staged state and different expected predecessor values | replay discriminator before authority | `StateConflict` | none |
| staged row/head/object immutable evidence disagree | invariant conflict before replay | `StateConflict` | none |
| stale revision/fence and authority loss | exact CAS/binding before authority | `StateConflict` | none |
| key not `Active` or object not `VerifiedCompleted`, together with authority loss | locked durable prerequisites before authority | `StateConflict` | none |
| concurrent key revoke and new stage | exact key-row lock serializes both; revoke-first conflicts, stage-first holds `Active` through stage commit | `StateConflict` or `Staged` according to lock winner | never stage from a value read before a committed revoke |
| concurrent authority revoke and new stage | exact shared/exclusive advisory lock serializes the source tuple; revoke-first is observed by resolver, stage-first holds the shared lock through stage commit | `SourceRetentionNotAuthorized` or `Staged` according to lock winner | never stage from authority read before an already committed revoke |
| transaction begins before expiry but authority/consent lock wait crosses a validity or source deadline | post-lock `clock_timestamp()` is the sole admission time | `SourceRetentionNotAuthorized` | zero mutation; pre-wait time cannot authorize staging |
| exact new candidate and authority/consent loss | fresh authority barrier | `SourceRetentionNotAuthorized` | none |
| two exact concurrent stage calls | winner commits; loser rereads | `Staged` then `ExistingMatch` | one attempt update, one head increment |
| same attempt with different object | exact binding | `StateConflict` | none |

The function does not return `CONTENT_COMMITMENT_MISMATCH`: that is R2's
deterministic verification outcome and cannot coexist with
`VerifiedCompleted`.

## 7. C# surface

Add internal types equivalent to:

```csharp
internal sealed record RawExportR3StageCommand(
    Guid ActorPrincipalId,
    Guid AttemptId,
    Guid ObjectCustodyId,
    long ExpectedReservationRevision,
    long ExpectedEncryptionAttemptRevision,
    long ExpectedFence,
    long ExpectedObjectStateRevision);

internal enum RawExportR3StageDisposition
{
    Staged,
    ExistingMatch,
    NotFound,
    StateConflict,
    SourceRetentionNotAuthorized,
}

internal sealed record RawExportR3StageResult(
    RawExportR3StageDisposition Disposition,
    Guid? SourceArtifactId,
    Guid? AttemptId,
    Guid? ObjectCustodyId,
    int? StagedCiphertextFingerprintSchemaVersion,
    byte[]? StagedCiphertextFingerprint,
    long? ReservationRevision,
    long? Fence,
    DateTimeOffset? StagedAtUtc);
```

`RawExportR3StagingService` validates the command, opens one connection, begins
one transaction, uses `SET LOCAL tagekyc.actor_principal_id`, invokes
the one staging function, maps its closed result, commits and returns. It never
performs external/provider/key/AEAD/HMAC/plaintext/service I/O. Its only network
edge is the required Npgsql/PostgreSQL database connection, and the only
cryptographic primitive it owns is the pure staged-fingerprint SHA-256 codec.
Every success, failure, cancellation and exception leaves the connection
closed and transaction-free under the landed connection-lifecycle pattern.

The service is internal and is not registered in the public API composition
root in this slice. Tests instantiate it through the reconciliation boundary.

## 8. PI-TAG-001 semantic matrices

### 8.1 Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract surface | Outcome/error | State/evidence/residue | Named proof | Negative/mutation proof |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| R3-I01 | only exact active-key/verified-object candidate stages | staging SQL | lock key/object, immutable bindings, then new-stage dispositions | stage function | `Staged`/conflict | one staged attempt | R302/R305/R307 | remove key lock/Active predicate or object state/binding predicate |
| R3-I02 | no caller-derived evidence | SQL derivation | locked rows first | six-scalar signature | argument invalid | no caller digest residue | R303/R312 | add digest/fingerprint parameter |
| R3-I03 | fresh authority gates new stage at one post-blocking admission time | staging SQL | acquire authority+consent locks, capture one clock value, revalidate all windows/deadlines, then write | authority+consent resolvers, deadlines, staged time | retention not authorized | zero mutation | R306/R307 | omit lock/predicate, use pre-wait transaction time or take an independent second clock value |
| R3-I04 | exact same-command replay is monotonic | staging SQL | complete staged/head/object shape and frozen predecessor command before authority | result union | `ExistingMatch` | unchanged staging/head | R303/R304 | move authority before replay, ignore predecessor values or permit partial replay |
| R3-I05 | exactly one winner | staging SQL | row locks + CAS | attempt/head write | staged/existing | one update and revision increment | R309 | remove head/attempt CAS |
| R3-I06 | v2 fingerprint is deterministic | SQL+C# codec | exact field order/encoding | fingerprint columns | staged/conflict | 32-byte digest | R308 | reorder/omit/substitute each field; common-mode mutation |
| R3-I07 | no raw/plaintext/key persistence | schema and result | always | entity/function/result | fail closed | metadata only | R312 | add payload/plaintext digest/key field |
| R3-I08 | role isolation is exact | migration ACL | after function ownership | catalog ACL | 42501/wrong actor | no table grants | R311 | grant runtime/opposite role or table privilege |
| R3-I09 | only one-way staging fields mutate | triggers | exact context | attempt/head guards | direct-DML failure | pre-stage or complete staged shape | R304/R310 | disable guard or mutate sibling column |
| R3-I10 | rollback is reversible and occupied downgrade is fail-closed | migration | apply; occupied Down rejection; teardown; Down/reapply | catalog/model/ACL | occupied failure then clean chain | staged evidence survives failed Down; landed pre-Up restored after teardown | R301 | remove occupied-data guard or omit one Down restore/revoke/drop |
| R3-I11 | staged remains non-readable | R3 boundary | after success | no resolver/API/R4/R5 | no surface | head `Staged` only | R315 | add Available/locator/resolver path |
| R3-I12 | R2 durable restart handoff is real | R2+R3 integration | provider/process restart before verification/stage | landed MinIO/key/object + stage | `Staged` | exact verified evidence then staged row | R314 | serve process cache or pass result-only digests |
| R3-I13 | R3 transaction permits only PostgreSQL I/O and pure staged-fingerprint SHA-256; no external/provider/key/AEAD/HMAC/plaintext/service edge | architecture + service | always | dependency/call graph | build/test failure | DB metadata transaction only | R313 | inject any forbidden dependency/call edge or forbid the two explicitly allowed edges |
| R3-I14 | EF model, snapshot and database catalog remain synchronized | migration + E3 | apply and pending-model before closeout | model/snapshot/catalog | clean or fail closed | one additive staged shape | R316 | omit/duplicate a property, FK or check; stale snapshot hash |

### 8.2 Shape and Nullability Matrix

Section 4.1 and section 6.1 are normative. Every pre-stage attempt has all
eleven staging fields NULL; every staged attempt has all eleven present. No
third shape exists. R310 mutates each field independently to prove the check
and trigger reject partial or sibling changes.

### 8.3 Ordering Graph

Reachable entry paths are:

```text
healthy R2 completion → first R3 call
lost R3 response → same-command replay
fresh reconciler after process/provider restart → first R3 call
two concurrent reconcilers → one winner, one replay/conflict reader
concurrent key revoker and new stage → exact key lock serializes the result
concurrent authority revoker and new stage → shared/exclusive advisory lock serializes the result
authority/consent lock wait crosses expiry → post-lock time rejects with zero mutation
invalid/stale caller → rejection before authority/write
```

All use the single order in section 5.2. There is no administrative, runtime,
HTTP, lifecycle, writer or public bypass.

### 8.4 Test-Bite Matrix

| Claim | Positive control | Negative case | Broken mechanism | Named test expected RED | Restoration proof |
| --- | --- | --- | --- | --- | --- |
| exact `VerifiedCompleted` only | canonical R302 | every other object state | remove/widen state predicate | R305 | migration SHA restored; R302 green |
| locked `Active` key | canonical R302 | revoke-first and stage-first concurrency | remove key lock or `Active` predicate | R307 | migration SHA restored; both serialized outcomes green |
| locked current authority | valid grant and shared-lock stage | authority revoke-first and stage-first concurrency | remove exact shared advisory lock | R307 | migration SHA restored; both serialized outcomes green |
| fresh authority at one post-lock admission time | valid grant+consent | withdrawal/revoke/expiry plus authority-lock and consent-lock waits crossing expiry | omit one barrier, capture time before waits, use `transaction_timestamp()` or take a second clock value | R306/R307 | canonical authority/time matrix green |
| exact replay before authority | stage then withdraw then replay the same predecessor command | change each predecessor scalar after staging | reorder branches or ignore a predecessor value | R303/R304/R307 | canonical replay green |
| revision/fence/object CAS | exact values | stale each scalar | omit comparator | R304 | canonical exact values green |
| v2 codec | absolute vector | field/order/encoding mutation | mutate SQL and C# together | R308 | both production bytes restored |
| one winner | two concurrent exact calls | competing object/attempt | remove locks/CAS | R309 | row/event census exactly one |
| guarded writes | function call | owner/raw DML and sibling mutation | disable/broaden guard | R310 | guard catalog+hash restored |
| occupied downgrade | empty-fixture Down restores R2 | valid staged row/head exists | remove occupied-data preflight | R301 | failed Down leaves schema/data intact; teardown then Down/reapply green |
| durable restart handoff | restart then verify+stage | cache/result-only shortcut | bypass durable object row/read | R314 | fresh process and canonical run green |

### 8.5 Risk modules

| Module | Disposition |
| --- | --- |
| SQL/schema/transaction | Required: migration, constraints, triggers, locks, CAS, ACL, apply/Down/reapply |
| Cryptography/key management | Boundary-only: pure SHA-256 for v2 staged fingerprint is allowed and owned by R308; no key provisioning/unwrap, AEAD, HMAC/content-commitment or plaintext cryptography |
| Raw/restricted data | Required: no raw/digest persistence, no plaintext I/O, fixture-only evidence |
| Worker/queue | Reconciliation entry semantics only; no queue/lease implementation in R3 |
| API | Not applicable: no endpoint/DTO/controller/runtime registration |
| Governance/docs | Required: v2 supersession, status/version/hash/non-claims and review convergence |

## 9. Exact proof manifest

| ID | Exact test method | Required bite |
| --- | --- | --- |
| R301 | `R301_apply_down_reapply_restores_R2_catalog_model_and_acl` | apply; valid staged fixture; occupied Down fails before destructive DDL with schema/evidence intact; teardown; Down restores exact pre-Up; reapply succeeds; removing occupied guard turns this method RED |
| R302 | `R302_exact_verified_object_stages_once_with_complete_persisted_shape` | positive stage and exact row readback |
| R303 | `R303_response_loss_replay_returns_existing_match_before_fresh_authority` | exact same-command replay after withdrawal, complete-shape recomputation, zero second write |
| R304 | `R304_stale_reservation_attempt_fence_or_object_revision_cannot_stage` | each scalar independently bites both new-stage and already-staged replay paths |
| R305 | `R305_only_VerifiedCompleted_object_state_can_stage` | complete ten-state landed object partition |
| R306 | `R306_fresh_authority_snapshot_consent_and_deadlines_gate_new_stage` | each barrier independently fails with zero mutation; exact no-state-change authority-lock and consent-lock blockers independently carry the transaction past expiry; pre-wait `transaction_timestamp()` or any second-clock mutation turns the method RED |
| R307 | `R307_stage_precedence_is_exact_under_simultaneous_failures` | matrix in section 6.2, including revoke-first/stage-first key/authority serialization and post-lock expiry precedence |
| R308 | `R308_staged_ciphertext_v2_codec_matches_absolute_vector_and_every_field_bites` | SQL/C# independent vector plus common-mode mutation |
| R309 | `R309_concurrent_exact_stage_has_one_winner_and_one_existing_match` | one attempt mutation and one head increment |
| R310 | `R310_staging_shapes_and_attempt_head_guards_are_exact_and_one_way` | partial/sibling/direct DML mutations RED |
| R311 | `R311_R3_function_owner_acl_role_and_table_privileges_are_exact` | missing and extra grants/owner/role mutations |
| R312 | `R312_R3_schema_contract_result_and_logs_contain_no_raw_plaintext_digest_or_key_material` | forbidden-surface census |
| R313 | `R313_R3_transaction_allows_only_PostgreSQL_and_staged_fingerprint_SHA256_edges` | permits Npgsql/PostgreSQL and pure v2 SHA-256; every S3/provider, external service/network, key provisioning/unwrap, AEAD, HMAC/content-commitment or plaintext edge is forbidden and independently mutated |
| R314 | `R314_restart_recovery_verified_object_advances_to_Staged_without_process_result_handoff` | synthetic R2→restart→verify→R3 executable evidence |
| R315 | `R315_Staged_has_no_locator_resolver_Available_R4_R5_or_delivery_surface` | absence proof and forbidden-edge mutation |
| R316 | `R316_model_snapshot_catalog_and_E3_tripwire_are_synchronized` | pending-model clean and additive snapshot proof |

Every mutation runs one at a time, must RED at the named assertion for the
assigned reason, restores all touched bytes, then reruns the canonical test.
A mutation that cannot execute is marked N/A with a proven invariant; it is not
reported as passing coverage. Test IDs and methods remain count-stable after
ratification unless Homeowner authorizes a correction.

## 10. Exact implementation allowlist for a later controlled build

The later build may touch exactly these 14 paths:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_r3_as_built.md
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceEncryptionAttemptRow.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR3Contracts.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR3Repository.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR3StagedCiphertextFingerprintCodec.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR3StagingService.cs
tests/TagEkyc.ArchTests/Tip88C1B2R3VerifiedCiphertextStagingArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs
```

This dispatch file is verify-only during implementation and must remain
byte-identical to its ratified SHA. The existing R2 integration file is allowed
only for a count-neutral shared fixture/helper adjustment or an exact R2→R3
handoff assertion; no R2 production contract change is authorized. The E3 file
is allowed only for the additive ModelSnapshot tripwire refresh and its
existing semantic round-trip/catalog behavior.

Any required fifteenth path, package, project file, Program/DI change,
readiness file, appsettings file or landed migration edit is STOP/RRI before
editing.

## 11. Task-0 gates for a later controlled build

Before any implementation edit:

1. Verify branch and HEAD equal the metadata above.
2. Record `git status --short`; staged paths must be zero.
3. Prove no `src/` or `tests/` path is dirty.
4. Verify every source-of-truth SHA in section 0.
5. Verify the active fixture-only GOV/ART chain is not invalidated.
6. Record exact current hashes for all existing allowlist paths.
7. Run `git diff --check`.
8. Run Release build and the current affected R2, Durable Object, DK-PROD,
   DK-FIXTURE, authority, consent, ArchTests and E3 tests; record census.
9. Run the corrected pending-model command with Infrastructure as both project
   and startup project and require no pending model changes.
10. Apply the complete migration chain to isolated PostgreSQL, capture catalog
    and ACL baseline, then verify Down/reapply feasibility.
11. Confirm the 14-path allowlist can contain the build exactly.

Any failed gate is STOP/RRI. Do not repair unrelated dirt, package topology,
role topology, landed migrations or snapshot by hand.

## 12. Validation and closeout requirements

The later implementation closeout requires:

- Release build: zero warnings, zero errors;
- pending-model: clean;
- R301–R316 positive controls green;
- every assigned mutation RED for the exact reason and restored;
- full affected suites and complete unfiltered Release suite: zero failures;
- exact catalog, owner, ACL, trigger and column-shape census;
- occupied-data Down rejection with schema/evidence intact, followed by fixture
  teardown and apply/Down/reapply with pre-Up equivalence where owned;
- no raw/plaintext/digest/key/provider locator leakage;
- staged paths zero;
- `git diff --check` clean;
- task-created PostgreSQL/MinIO/process/container residue zero;
- `tip_88c1_b2_r3_as_built.md` with Outcome vs Intent, decisions, debt,
  validation, mutation evidence, hashes and non-claims;
- a review-only bundle outside the repository with manifest and SHA256SUMS.

The only successful pre-review disposition is:

```text
PASS — R3 IMPLEMENTATION READY FOR INDEPENDENT CLOSEOUT REVIEW
```

It is not commit, push, production, real Raw BIO, R4/R5 or delivery readiness.

## 13. STOP/RRI conditions

STOP/RRI if:

- this exact document has not received clean independent review and Homeowner
  ratification before implementation;
- baseline, branch, source hashes or active fixture authority drift;
- any permanent path outside section 10 is required;
- a package, Program/DI, public/runtime API, readiness topology or new role is
  required;
- R3 requires S3/provider, external key provisioning/unwrap, AEAD,
  HMAC/content-commitment, plaintext or non-database external-service I/O;
- a caller-supplied digest/fingerprint/evidence field becomes authoritative;
- a bare plaintext digest, plaintext, ciphertext body, key material, object key
  or credential would enter PostgreSQL, logs or results;
- `VerifiedCompleted` cannot supply the complete durable v2 inputs without
  weakening the fingerprint;
- the v2 formula would need a provider version, fabricated sentinel or mutable
  state/timestamp field;
- exact replay cannot precede fresh authority without weakening R4/R5 gates;
- the exact key row cannot be locked through new-stage commit or the
  `Active → Revoked` race cannot be discriminated;
- the exact landed shared authority advisory lock cannot be held through
  new-stage commit or the grant/withdraw/revoke race cannot be discriminated;
- new staging can pass after authority/consent/deadline loss;
- the one `StageEvaluationTimestamp` cannot be captured after every blocking
  authority/consent lock and shared by final authority/consent windows, strict
  deadlines and `StagedAtUtc`;
- a pre-wait `transaction_timestamp()` mutation or authority/consent lock wait
  crossing expiry cannot be made to RED at R306/R307 for the admission result;
- attempt/head writes cannot be made one-way and guard-complete;
- an occupied Down would erase or reinterpret staged evidence;
- existing R1/R2/Durable Object/DK-PROD behavior, role or migration bytes would
  need reinterpretation;
- pending-model is not clean after the additive migration;
- any named mutation remains GREEN, is vacuous or fails for collateral reason;
- review reaches round 5 without a PI-TAG-001 root-cause checkpoint, or round
  10 without a clean verdict.

## 14. Review instructions and current disposition

Independent review must read this complete file and the exact source artifacts
in section 0. It must verify repo-real field/function/role feasibility rather
than reviewing prose alone. Classify findings under `L-TAG-Review-02` and map
each actionable finding to affected matrix rows and sibling sections.

Review must focus particularly on:

1. whether v2 safely and explicitly replaces the unsatisfiable provider-version
   field without weakening exact object/content binding;
2. whether `VerifiedCompleted` is sufficient durable authority for R3 without
   trusting a process-local result;
3. authority/consent/replay/CAS precedence, shared lock domains and post-lock
   single-time evaluation under expiry-crossing waits;
4. key-lock/revoke serialization and one-way trigger feasibility while
   preserving R1 and R2 branches;
5. exact ACL role reachability and lack of direct table privileges;
6. the exact allowed PostgreSQL/SHA-256 versus forbidden external-edge boundary;
7. occupied-downgrade, timestamp and other mutation non-vacuity, plus the
   14-path allowlist.

Current state:

```text
DocumentAuthoring = COMPLETE_FOR_REVIEW
IndependentReview = PENDING
HomeownerRatification = NOT_RECORDED
ImplementationAuthority = NONE
MigrationExecutionAuthority = NONE
StageCommitPushAuthority = NONE
ProductionRealRawBioR4R5DeliveryAuthority = NONE
```

Required next action: create one review-only bundle outside the repository and
obtain an independent full-document review. Do not implement while this state
remains unchanged.
