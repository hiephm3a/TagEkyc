# TIP-88C1-C1 Resolver + Authenticated Assembly — Controlled Build Dispatch

Status: `ROUND_2_RECONCILED — ROUND_3_EXACT_SHA_CANDIDATE — NO BUILD AUTHORITY`  
Version: `0.2`  
Date: `2026-08-16`  
Repository: `TagEkyc`  
Branch: `tip-88a-raw-export-policy-catalog-build`  
Baseline: `c2956b168f8475e6285dc51d4037bab55234e32c`  
Scope predecessor: `tip_88c1_c1_resolver_assembly_scope_brief.md` v0.2  
Scope SHA-256: `403D79D549F19213166631A78BA99CA5D6A3C4D2F47656F339FEC8E8B6F161C3`  
Review-ledger predecessor: `tip_88c1_c1_resolver_assembly_review_ledger.md` v0.3  
Review-ledger predecessor SHA-256: `694E38D873DECEDCD0B0C572DAC2FC1071C9B7FC1B018E168AF12432224E43CF`

This is the Round-2 executable candidate required by the Homeowner's
three-round convergence policy. It is a review artifact, not implementation
authority. Do not edit production or test code until an exact successor SHA is
independently reviewed and ratified.

### 0.1 v0.2 Round-2 reconciliation

This successor changes only the bounded Round-2 findings:

- adds `AssemblyAuthenticationKeyId` and version to the exact
  `ManifestDigest` preimage and regenerates every dependent vector;
- adds the integration PostgreSQL fixture as path 39 so deployment-owned LOGIN
  roles exist before migration Up;
- carries the exact final independent R4-R6 review as governance evidence and
  classifies R2/R3/R4-R6 as committed implementation anchors, not newly reused
  review verdicts;
- binds the exact v0.3 review-open ledger snapshot above;
- distinguishes the public deterministic vector key from runtime/provider
  fixture key material;
- labels and tightens the already-present global lock order and adds explicit
  adjacent-order mutation obligations;
- discloses the exact B4 and R2 predecessor surfaces touched and requires their
  affected proofs plus per-branch preservation mutations.

No product flow, table set, SQL outcome, C2 graph, readiness code or proof id
is added by v0.2.

## 1. Outcome and fixed boundary

C1 consumes a B4 job in `Assembling` and the exact landed C1-A selections. It
freezes exactly one already-`Available` source for each ordered B4 class,
re-reads and verifies the winning durable ciphertext without mutating it,
constructs a canonical authenticated assembly with bounded memory, persists a
recoverable C2 preparation graph, and atomically advances the job to
`AssemblySealed`.

The controlled build is complete only when synthetic fixture evidence proves:

```text
B4 Assembling under exact attempt/revision/fence/lease
-> immutable C1-A selected Available sources frozen
-> pass 1: exact-object read + typed AEAD/historic-commitment verification
-> assembly digest + manifest digest + manifest authentication derived
-> fresh authority/consent/retention barrier
-> durable Preparing row
-> pass 2: exact-object re-read + typed verification + bounded assembly stream
-> C2 Prepare outside every database transaction
-> durable Pending row
-> new transaction: exact replay first, then locks, one post-lock clock,
   fresh authority/consent/retention barrier, SealCommitted, assembly rows,
   B4 Assembling -> AssemblySealed
-> C2 Finalize outside the transaction
-> durable Finalized
```

The build is fixture/synthetic evidence. It is not production activation and
does not provide a production raw-source adapter, production assembly
authentication key provider, production C2 provider, R4/R5 packaging or
delivery.

## 2. Round-1 decisions incorporated

| Finding | Executable disposition in this dispatch |
|---|---|
| C2 ordering | `Preparing -> Pending -> SealCommitted -> Finalized`; Finalize never precedes committed seal |
| Freshness | exact fresh barrier before first source read, immediately before Prepare, and inside the seal transaction after all locks |
| Authentication target | authenticate the exact `ManifestDigest`; never authenticate bare `AssemblyDigest` as the capability token |
| Capture selection | consume landed `raw_export_session_capture_selections` and acceptance events; never latest/best/caller-selected artifact |
| Fixture plaintext | memory-only bounded sink; never durable plaintext, hash-and-discard, unbounded buffering or crash-recovery evidence |
| AUTH disposition | require landed `FreshAuthorityRequired` and `Forbidden`; no new disposition gate |
| Locator/key scope | restricted custody may read opaque locator and approved wrapped-key metadata; no public/log/error/assembly exposure |
| B4 sealed transition | new migration changes the event CHECK and re-emits guards; landed B4 migration bytes remain immutable |
| Lease terminality | `AssemblySealed` is not claimable, reclaimable, renewable or reversible to `Assembling` |
| Typed verification | C1 uses one typed framed-source verification boundary shared with R2; it does not call the R2 encryption orchestrator |
| Plaintext memory | every source and assembly path is chunk-bounded; a complete-buffer mutation must turn proof C119 RED |

The Round-1 claims that capture selection was absent and that the AUTH
disposition gate was unresolved are rejected by landed bytes. Their exact
on-code anchors are carried in the Round-2 review bundle.

### 2.1 Declared predecessor-contract impact

C1 deliberately touches two ratified predecessor surfaces; this is not
described as a purely additive greenfield build:

1. **B4:** landed migration bytes remain immutable, but the new C1 migration
   replaces the current transition CHECK and re-emits two B4 guard functions to
   admit exactly `Assembling -> AssemblySealed`. C121 and C125 rerun the affected
   B4 lifecycle/catalog proofs. C125 mutates each landed CHECK branch and each
   landed guard context independently; deleting or weakening any predecessor
   branch must turn the same proof RED before canonical restoration.
2. **R2:** `RawExportR2CompletionVerifier.cs` is refactored only to consume the
   shared framed-source verification service used by C1. Its externally
   observable R2 dispositions, state mutations and vectors remain unchanged.
   C108 reruns the affected R2 verifier methods and their discriminating
   mutations on final bytes.

The predecessor accounting unit is semantic coverage, not only path count.
At closeout the as-built lists every predecessor method rerun, its canonical
result, its mutation result where owned, and the before/after SHA of the two
touched surfaces. No PASS from an older byte set substitutes for these affected
proofs.

Governance evidence for the source-finalization predecessor is the pair:

```text
R4-R6 final evidence bundle:
359C7061998C484EC2E4DC95590A6FC3BEAE404CB6408C9A18CDDF5D43290DB7

Exact independent final closeout report:
B58CED4E5577772E8FF596ADA41EF2D388C9A49CE4C79AF9AC88A56ECAEC93BD

Containing and pushed implementation commit:
c2956b168f8475e6285dc51d4037bab55234e32c
```

The report follows the evidence bundle and records
`PASS — READY FOR HOMEOWNER CONTROLLED-COMMIT REVIEW`. The Round-3 bundle must
carry both exact artifacts. R2, R3 and R4-R6 are consumed as committed
implementation anchors; this C1 review does not claim to independently re-review
their full histories.

## 3. Immutable source set and eligibility

### 3.1 Required classes and selection

The ordered required class list is the B4 `raw_export_job_classes` list ordered
by `Ordinal`. For each class C1 must join, with exact equality:

```text
job.VerificationSessionId
-> raw_export_session_capture_selections.VerificationSessionId
selection.RawClass = job class
selection.CaptureAcceptanceId
-> raw_export_capture_acceptance_events.CaptureAcceptanceId
acceptance.CaptureArtifactId + CaptureRevision + RawClass
-> landed claim/reservation/publication graph
```

There must be exactly one selection row and exactly one eligible publication
for every required class. Missing, duplicate or mismatched rows fail closed.
No ordering by timestamp, source age, quality, artifact UUID or provider order
is permitted.

### 3.2 Eligibility at every barrier

An eligible source has all of these properties at the barrier's one captured
time:

- publication state is exactly `Available` and `AvailableAtUtc` is non-null;
- publication identifies the same winning attempt, key reservation and object
  as the selected capture's source graph;
- source attempt is staged and not R2-terminated;
- object state is exactly `VerifiedCompleted`;
- key preparation disposition is exactly `Active`;
- source reservation has not crossed `AbsoluteSourceExpiresAtUtc`,
  `EffectivePlaintextRetentionExpiresAtUtc` or its applicable retained-source
  deadline;
- job has not crossed `JobExpiresAt` or permit expiry;
- current AUTH snapshot is granted, current revision/schema/session/artifact/
  revision/class bindings match, `ReuseDisposition` is exactly
  `FreshAuthorityRequired`, and `ExtensionDisposition` is exactly `Forbidden`;
- subject consent resolves with the authority-declared policy id/version and
  is valid for the job recipient, class and captured time;
- no revocation, withdrawal or hold posture blocks use.

`NULL`, unknown token, absent resolver row, provider indeterminacy and any
three-valued comparison are rejection, never eligibility.

### 3.3 Frozen per-job binding

`raw_export_job_source_bindings` is append-only and has exactly one row for
each `(JobId, Ordinal)` and `(JobId, RawClass)`. Its columns are:

```text
JobSourceBindingId uuid PK
JobId uuid
Ordinal integer
RawClass text
VerificationSessionId uuid
SessionCaptureSelectionId uuid
CaptureAcceptanceId uuid
CaptureArtifactId uuid
CaptureRevision integer
SourceArtifactId uuid
SourcePublicationId uuid
SourcePublicationRevision bigint
EncryptionAttemptId uuid
EncryptionAttemptRevision bigint
EncryptionAttemptFence bigint
AttemptKeyReservationId uuid
ObjectCustodyId uuid
ObjectStateRevision bigint
SubjectRefTokenSchemaVersion integer
SubjectRefTokenKeyId text
SubjectRefTokenKeyVersion integer
SubjectRefToken bytea(32)
ContentCommitmentSchemaVersion integer
ContentCommitmentKeyId text
ContentCommitmentKeyVersion integer
ContentCommitment bytea(32)
PlaintextLength bigint
MediaType text
AuthoritySnapshotSchemaVersion integer
AuthoritySnapshotId uuid
AuthorityRevision bigint
ConsentPolicyId uuid
ConsentPolicyVersion integer
AbsoluteSourceExpiresAtUtc timestamptz
EffectivePlaintextRetentionExpiresAtUtc timestamptz
StableDataScopeId text
ControllerIdentity text
BindingFingerprint bytea(32)
CreatedAtUtc timestamptz
SchemaVersion integer = 1
```

`JobSourceBindingId` is
`DeterministicGuid("tip-88c1-job-source-binding-id-v1", {JobId, Ordinal,
RawClass})`. `BindingFingerprint` uses
`C1HashCanonical("tip-88c1-job-source-binding-v1", ...)` over every non-audit
field in the order printed above, excluding the id, fingerprint and
`CreatedAtUtc`.

Freeze is insert-once. Exact replay returns `ExistingMatch`; any same-job
different binding returns `BindingConflict`. A frozen binding is never updated
to a newer authority, source publication, attempt, key, object or selection.

## 4. Restricted resolver and typed verification

### 4.1 Capability split

The migration creates exactly two NOLOGIN capability roles:

```text
tagekyc_raw_export_assembly_resolver
tagekyc_raw_export_assembly_sealer
```

Deployment supplies these LOGIN roles before migration Up:

```text
tagekyc_raw_export_assembly_resolver_login
tagekyc_raw_export_assembly_sealer_login
```

All four are `INHERIT`, `NOSUPERUSER`, `NOCREATEDB`, `NOCREATEROLE`,
`NOREPLICATION`, `NOBYPASSRLS`; capability roles are `NOLOGIN`, deployment
roles are `LOGIN`. Membership is exactly resolver-login -> resolver and
sealer-login -> sealer with `ADMIN=false`, `INHERIT=true`, `SET=false`.
There is no cross-membership and no membership to existing encryptor,
reconciler or lifecycle capabilities.

Resolver can execute only the freeze/read functions required to obtain an
exact restricted context. It cannot insert assembly identities, advance B4,
authorize abort, finalize C2 or mutate source custody. Sealer cannot read an
object locator, wrapped-key metadata or plaintext. `tagekyc_runtime`, PUBLIC
and every other capability have zero effective privilege on the new tables and
functions unless explicitly named here.

### 4.2 Read contract

The resolver returns an internal `ResolvedAssemblySource` whose public surface
is only metadata plus a callback-driven bounded plaintext read operation. The
caller never receives object key, provider credentials, wrapped DEK bytes,
unwrapped DEK or a seekable provider stream.

Internally the resolver may read the frozen opaque object locator and approved
wrapped-key metadata through exact SECURITY DEFINER projections. It opens the
exact durable object through `IProvisionalObjectReconciler`, acquires a bounded
DEK lease through the landed typed AEAD verification operation, parses the R2
frame, verifies all chunk/final tags, verifies framing order/truncation, and
recomputes the historic commitment with the frozen historic selector.

The shared internal boundary reports only:

```text
Verified
KeyAccessIndeterminate
ObjectReadIndeterminate
DeterministicCiphertextInvalid
HistoricCommitmentMismatch
CallerCancelled
```

C1 maps only `Verified` to plaintext chunks. All other outcomes leave source,
job and preparation state unchanged. C1 does not request cleanup of an already
`Available` winning source. Logs and exceptions contain stable codes and ids,
never raw bytes, tags, DEK, locator, token or commitment preimages.

`RawExportR2CompletionVerifier` must consume the same shared framed-source
verification service for its cryptographic parse; a duplicated C1 parser is
forbidden. R2 retains its existing state mutation and outcome mapping.

### 4.3 Bounded memory and two passes

The maximum plaintext chunk retained by C1 is the frozen R2 `ChunkSize`; the
maximum assembly framing scratch is 64 KiB. A source callback must release and
zero custody-owned plaintext/scratch before requesting the next chunk. The
caller/provider-owned ciphertext and source buffer are not falsely claimed as
zeroized.

C1 performs two independent exact reads:

1. pass 1 verifies each source and computes the canonical `AssemblyDigest`,
   manifest body, `ManifestDigest` and authentication value;
2. after the pre-Prepare barrier, pass 2 independently re-opens and verifies
   every frozen object and streams the same canonical bytes to C2.

No plaintext or complete assembly survives between passes. A process cache,
`MemoryStream`/`byte[]` sized to total source length, temp plaintext file,
database large object or hash-and-discard substitute is forbidden. The fixture
C2 sink applies backpressure and rejects a stream whose length/digest/auth do
not equal the declared values.

## 5. Canonical assembly and authentication

### 5.1 Assembly byte stream

The exact stream is:

```text
ASCII("TIP-88C1-ASSEMBLY-V1")
UInt32BE(headerByteLength)
UTF8(RFC8785-JCS(header))
for each item in Ordinal ascending:
    UInt32BE(Ordinal)
    UInt64BE(PlaintextLength)
    exactly PlaintextLength verified plaintext bytes
```

Header keys are ASCII and ordered by RFC 8785, with these exact semantic
members:

```text
assemblyId
clientApplicationId
createdAtUtc
exportMode
jobId
manifestVersion
permitId
policyId
policyVersion
purposeCode
recipientClientApplicationId
subjectRefToken
verificationSessionId
items[] {
  ordinal, rawClass, sourceArtifactId, captureArtifactId, captureRevision,
  mediaType, plaintextLength, contentCommitmentSchemaVersion,
  contentCommitmentKeyId, contentCommitmentKeyVersion, contentCommitment
}
```

UUID is lower-case `D`; timestamps are UTC RFC3339 with exactly six fractional
digits; byte values are lower-case hexadecimal; lengths are JSON integers;
strings are NFC. `AssemblyDigest = SHA-256(exact complete stream)`.

### 5.2 Manifest and capability token

`ManifestDigest` is:

```text
C1HashCanonical(
  "tip-88c1-assembly-manifest-v1",
  { ManifestVersion, AssemblyId, JobId, AttemptId, FencingToken,
    JobExpiresAtUtc, CreatedAtUtc, SubjectRefTokenSchemaVersion,
    SubjectRefTokenKeyId, SubjectRefTokenKeyVersion, SubjectRefToken,
    AssemblyAuthenticationKeyId, AssemblyAuthenticationKeyVersion,
    AssemblyDigest, ordered item descriptors })
```

`AssemblyAuthenticationValue` is HMAC-SHA-256 under the selected dedicated
assembly-authentication key over this exact byte payload:

```text
UInt32BE(35)
ASCII("tip-88c1-assembly-authentication-v1")
UInt32BE(32)
ManifestDigest
```

It never authenticates raw source bytes or bare `AssemblyDigest`. The selected
`AssemblyAuthenticationKeyId` and version are both inside `ManifestDigest` and
are persisted beside the 32-byte value. Runtime/provider fixture key material
must not be persisted in repository artifacts, logs or as-built and fails
production readiness. Explicitly published non-secret deterministic vector-key
bytes are permitted solely for independent vector recomputation.

Every `C1HashCanonical` UUID scalar in this section is lower-case `N`, every
integer is invariant decimal, every timestamp is the six-fraction UTC form in
§5.1, and every byte value is lower-case hexadecimal. An ordered item element
uses exactly the item-field order printed in §5.1.

`AssemblyId = DeterministicGuid("tip-88c1-assembly-id-v1",
new { jobId = JobId.ToString("N") })`.
`AssemblyFingerprint = C1HashCanonical("tip-88c1-assembly-fingerprint-v2",
{AssemblyId, JobId, AttemptId, FencingToken, ManifestDigest,
AssemblyAuthenticationKeyId, AssemblyAuthenticationKeyVersion,
AssemblyAuthenticationValue})`.

### 5.3 Absolute fixture vector

The builder must reproduce this vector before schema work. It is not a
generator-only test; every value is asserted literally and independently
recomputed.

```text
JobId                         11111111-1111-1111-1111-111111111111
PermitId                      22222222-2222-2222-2222-222222222222
VerificationSessionId         44444444-4444-4444-4444-444444444444
ClientApplicationId           55555555-5555-5555-5555-555555555555
RecipientClientApplicationId  66666666-6666-6666-6666-666666666666
PolicyId                      77777777-7777-7777-7777-777777777777
AttemptId                     88888888-8888-8888-8888-888888888888
FencingToken                  7
ManifestVersion               1
CreatedAtUtc                  2026-08-15T15:00:00.123456Z
JobExpiresAtUtc               2026-08-15T15:30:00.000000Z
SubjectRefToken               000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f
AssemblyAuthenticationKeyId   fixture-assembly-authentication
AssemblyAuthenticationKeyVersion 1
HMAC key bytes                00 01 02 ... 1f (test-only vector, not provider config)

item 0:
  RawClass                    ChipDg2Portrait
  SourceArtifactId            aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
  CaptureArtifactId           cccccccc-cccc-cccc-cccc-cccccccccccc
  CaptureRevision             1
  MediaType                   image/jpeg
  PlaintextLength             3
  Plaintext                   01 02 03
  ContentCommitment           11 repeated 32 bytes

item 1:
  RawClass                    LiveSelfieImage
  SourceArtifactId            bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
  CaptureArtifactId           dddddddd-dddd-dddd-dddd-dddddddddddd
  CaptureRevision             2
  MediaType                   image/jpeg
  PlaintextLength             2
  Plaintext                   04 05
  ContentCommitment           22 repeated 32 bytes

both items:
  ContentCommitmentSchemaVersion 1
  ContentCommitmentKeyId          fixture-content-commitment
  ContentCommitmentKeyVersion     1

AssemblyId                   aea6e706-6f9d-58ff-b4c3-9a2dc40c43a6
HeaderLength                 1521
CompleteAssemblyLength       1574
AssemblyDigest               901a70447c9fe9f7a2884cd473359afef12b2e5d5b17ac1f74b49a7535f25c6f
ManifestPreimageLength       885
ManifestDigest               eb3ad4b3ddc4b49e33c8e4eff34a5e50f8574a438a70e95fd79d4ec88e7fc41b
AuthenticationPreimageLength 75
AuthenticationValue          d73771a5d97f1cd152836d597a5c3ddfbc7ecda007bbd2bbdc41f27996f96be2
AssemblyFingerprint          09dba4977031ec99b7fe89a7b942b13508185015ffe2d45820850a2fd591757f
C2PreparationId              8e22d1f0-079a-5804-9c7d-63c47190644d
PreparationFingerprint       3285f039e1fe9ec64e1ebca30e7c86e0b608cd5fd06bafbf76381ee4bd75dd4f
```

The deterministic C2 input is exactly
`new { assemblyFingerprint = lowerHex, assemblyId = AssemblyId.ToString("N") }`.
The vector's header is the RFC8785-JCS serialization of §5.1 with the values
above, `ExportMode=EncryptedRawVaultRetained`,
`PurposeCode=SubjectRawBiometricExport`, and the two item objects. No audit,
receipt, locator, key or preparation field is present in the assembly header.

## 6. C2 preparation state and persistence

### 6.1 C2 interface

`IC2AssemblyPreparationProvider` exposes exactly:

```text
PrepareAsync(request, boundedAssemblyWriter, cancellationToken)
GetPreparationAsync(C2PreparationId, cancellationToken)
FinalizeAsync(C2PreparationId, AssemblyFingerprint, cancellationToken)
AbortAsync(C2PreparationId, AbortAuthorizationDigest, cancellationToken)
```

Typed provider outcomes are:

```text
Prepare: Prepared | ExistingMatch | Conflict | Unavailable | OutcomeUnknown
Inspect: Missing | Preparing | Prepared | Finalized | Aborted |
         Conflict | Unavailable | OutcomeUnknown
Finalize: Finalized | ExistingMatch | Conflict | Unavailable | OutcomeUnknown
Abort: Aborted | ExistingMatch | Conflict | Unavailable | OutcomeUnknown
```

The fixture provider is process-lifetime, memory-only and bounded by one
assembly whose declared total plaintext length is at most 32 MiB. It is erased
after Finalized/Aborted and is explicitly not crash-recovery or production
durability evidence. Larger/production media is deferred.

### 6.2 Durable preparation graph

`raw_export_assembly_preparation_dispositions` is one row per deterministic
preparation:

```text
C2PreparationId uuid PK
AssemblyId uuid UNIQUE
JobId uuid UNIQUE
AttemptId uuid
FencingToken bigint
AssemblyFingerprint bytea(32)
PreparationFingerprint bytea(32)
Disposition text
ProviderReceiptDigest bytea(32) NULL
AbortAuthorizationDigest bytea(32) NULL
RowRevision bigint
PreparingAtUtc timestamptz
PendingAtUtc timestamptz NULL
SealCommittedAtUtc timestamptz NULL
FinalizedAtUtc timestamptz NULL
AbortAuthorizedAtUtc timestamptz NULL
AbortedAtUtc timestamptz NULL
SchemaVersion integer = 1
```

Allowed graph:

```text
Preparing -> Pending -> SealCommitted -> Finalized
Preparing -> AbortAuthorized -> Aborted
Pending   -> AbortAuthorized -> Aborted
```

`SealCommitted` and `AbortAuthorized` are mutually exclusive. Provider
`Unavailable` or `OutcomeUnknown` never authorizes abort. Inspect exact state;
only durable `Missing` after an armed/response-loss policy has proved no
provider object, or an explicit terminal `Conflict`, may enter
`AbortAuthorized`. The fixture proof covers response loss without claiming
process-loss recovery.

`C2PreparationId = DeterministicGuid("tip-88c1-c2-preparation-id-v1",
new { assemblyFingerprint = lowerHex,
assemblyId = AssemblyId.ToString("N") })`.
`PreparationFingerprint = C1HashCanonical("tip-88c1-c2-preparation-v1",
{C2PreparationId, AssemblyId, AssemblyFingerprint, ManifestDigest,
AssemblyDigest, declared total length})`.

### 6.3 Assembly identities and items

`raw_export_assembly_identities` is append-only, primary keyed by `AssemblyId`
and unique by `JobId`. It persists all scalar manifest/authentication fields,
`AssemblyDigest`, `ManifestDigest`, `AssemblyFingerprint`, total length,
item count, `C2PreparationId`, attempt/fence, `SealedAtUtc`, schema version and
no plaintext.

`raw_export_assembly_items` is append-only, primary keyed by
`(AssemblyId, Ordinal)`, unique by `(AssemblyId, RawClass)` and foreign keyed to
the exact `JobSourceBindingId`. It persists the exact item descriptor printed
in §5.1 and no plaintext.

## 7. Database functions and outcomes

All functions are `SECURITY DEFINER`, owner
`tagekyc_raw_export_deployer`, `SET search_path=pg_catalog`, use only
schema-qualified objects, derive actor with
`tagekyc.raw_export_current_actor()`, restore every transaction-local mutation
context on normal and exceptional exit, and revoke PUBLIC/runtime access.

Exact new signatures:

```text
raw_export_freeze_job_source_bindings(
  uuid, uuid, bigint, bigint, uuid)

raw_export_read_job_source_verification_context(
  uuid, integer, uuid, bigint, bigint, uuid)

raw_export_register_assembly_preparing(
  uuid, uuid, uuid, bigint, bigint, bytea, bytea)

raw_export_record_assembly_pending(
  uuid, bigint, bytea)

raw_export_seal_authenticated_assembly(
  uuid, uuid, bigint, bigint, bigint, uuid, bytea, bytea, bytea,
  text, integer, bytea, bigint, integer, jsonb)

raw_export_authorize_assembly_abort(
  uuid, bigint, bytea)

raw_export_record_assembly_finalized(
  uuid, bigint, bytea)

raw_export_record_assembly_aborted(
  uuid, bigint, bytea)

raw_export_read_assembly_recovery_context(
  uuid)
```

The first UUID of job-owned calls is `JobId`; calls containing a preparation
begin with `C2PreparationId`. Function comments and repository parameter names
must record the mapping. No overload is permitted.

Stable outcomes are the closed set:

```text
Created | ExistingMatch | Frozen | Pending | Sealed | Finalized |
AbortAuthorized | Aborted | NotFoundOrNotAllowed | AuthorityInvalid |
SourceUnavailable | BindingConflict | AssemblyConflict |
PreparationConflict | LeaseLost | Expired | StateConflict |
ConcurrencyConflict
```

Provider `Unavailable` and `OutcomeUnknown` are C# orchestration dispositions,
not SQL outcomes and not persisted as false terminal facts.

## 8. Atomic seal, replay and B4 lifecycle amendment

### 8.1 Replay precedence

`raw_export_seal_authenticated_assembly` checks exact committed replay first.
It returns `ExistingMatch` only when the persisted identity, all items,
preparation, assembly/manifest/authentication digests and the predecessor
attempt/fence inputs rederive byte-identically. This replay does not rerun
fresh authority, change timestamps, reacquire lease or call C2.

Any non-exact row is `AssemblyConflict`. Generic revision/fence/state failures
are evaluated only after exact replay.

### 8.2 Global row-lock order and new-seal transaction

Every C1 database function acquires only the subset it needs while preserving
this one global order. A function may skip an absent/unneeded participant but
must never reverse two participants:

```text
B4 operational head
-> exact current B4 attempt
-> B4 job identity/classes
-> source publications ordered by SourceArtifactId
-> source encryption attempts ordered by AttemptId
-> source heads ordered by SourceArtifactId
-> source reservations ordered by SourceArtifactId
-> ingress claims ordered by IngressClaimId
-> key reservations ordered by AttemptKeyReservationId
-> provisional objects ordered by ObjectCustodyId
-> frozen C1 source bindings ordered by Ordinal
-> C2 preparation row
-> assembly identity/items
-> landed authority shared advisory lock
-> landed consent shared advisory lock
-> one clock_timestamp()
```

The B4 head is first because landed B4 acquire/renew/reclaim/failure functions
lock that head and do not lock an existing current-attempt row before it. The
source subsequence is exactly the landed R4-R6 publication-first order:
publication -> attempts -> head -> reservation -> claim -> keys -> objects.
B4 functions never acquire source rows and R2-R6 functions never acquire B4
rows, so the two domains cannot form a cross-domain cycle. Concurrent C1
sealers serialize on the B4 head.

Forced interleavings cover C1 seal versus B4 renew/reclaim, C1 freeze/seal
versus R3/R4-R6 on the same source, and two C1 sealers. Reversing
publication/attempt, head/reservation, claim/key or key/object independently
must turn C116 RED by a forced interleaving; reversing B4 head/current-attempt
must turn C116 RED by its exact acquired-lock trace. PostgreSQL `40P01` is not
an accepted business outcome.

No blocking lock follows the timestamp. At that timestamp revalidate job,
lease, authority, consent windows, retention/hold/revocation, every source
identity/revision/state/deadline and the C2 Pending fingerprint. Then atomically:

1. transition preparation `Pending -> SealCommitted`;
2. insert assembly identity/items;
3. update B4 head `Assembling -> AssemblySealed`, revision +1, preserve current
   attempt/fence, clear lease owner/expiry, set `UpdatedAt` to the same clock;
4. append B4 transition `AssemblySealed`, exact from/to/attempt/fence, null
   resulting lease and no failure code, same clock.

### 8.3 B4 correction in the C1 migration

Do not edit `20260726145547_Tip88B4RawExportJobFoundation.cs` or any landed
migration. The C1 migration:

- drops/recreates `CK_b4_job_transition_event_shape` with every landed branch
  byte-for-byte plus the one `AssemblySealed` branch;
- `CREATE OR REPLACE`s `enforce_raw_export_job_transition_insert()` preserving
  the landed graph checks;
- `CREATE OR REPLACE`s `enforce_raw_export_job_head_mutation()` preserving all
  landed contexts and adding only `job_head_assembly_sealed`;
- restores owner/ACL exactly;
- Down restores the exact landed B4 CHECK and both exact landed function bodies;
- leaves no extra overload, trigger, grant or mutation context.

`RawExportJobEventType` gains exactly `AssemblySealed`. Attempt phase remains
`Assembling`.

After sealing, acquire/reclaim/renew/failure paths must not admit the job and no
function may return it to `Assembling`.

## 9. Fresh authority barriers and failure ownership

The same closed eligibility predicate in §3.2 runs:

1. in the freeze transaction immediately before first source read;
2. after pass 1 and immediately before `PrepareAsync`;
3. inside new-seal transaction at the one post-lock clock.

Authority or consent loss at any barrier wins over provider/key ambiguity and
returns `AuthorityInvalid` with no new read/prepare/seal mutation after that
barrier. A C2 provider error before committed seal remains recoverable through
the preparation row. Caller cancellation is indeterminate/retryable and does
not invent a terminal state.

The assembly orchestrator never keeps a DB transaction, B4/source row lock,
DEK lease or authority/consent advisory lock open across object, key,
authentication or C2 provider I/O.

## 10. Readiness and production posture

Configuration topology is exactly:

```text
Disabled
FixtureProof
```

`Disabled` is a valid production-host composition and performs no role/provider
operation. `FixtureProof` is accepted only in Integration/Development and must
fail production activation. Required stable readiness codes:

```text
PROD_RAW_EXPORT_ASSEMBLY_CONFIG_INVALID
PROD_RAW_EXPORT_ASSEMBLY_ROLE_TOPOLOGY_INVALID
PROD_RAW_EXPORT_ASSEMBLY_AUTHENTICATOR_UNAVAILABLE
PROD_RAW_EXPORT_ASSEMBLY_C2_UNAVAILABLE
```

Readiness verifies exact capability/login attributes, bidirectional membership
graph, exact owners/ACLs/table/function manifests, no PUBLIC/runtime/cross-role
edge, authentication selector availability and C2 fixture posture. It does not
claim that fixture providers prove production readiness.

## 11. Exact permanent path allowlist (39 paths)

No permanent path outside this list may change. If a fortieth path is required,
STOP/RRI before editing it.

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_scope_brief.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_review_ledger.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_build_dispatch.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_as_built.md
src/TagEkyc.Domain/RawExportJob.cs
src/TagEkyc.Application/Ports/RepositoryPorts.cs
src/TagEkyc.Contracts/RawExport/RawExportAssemblyContracts.cs
src/TagEkyc.Infrastructure/Persistence/EfRawExportJobRepository.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobSourceBindingRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAssemblyIdentityRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAssemblyItemRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAssemblyPreparationDispositionRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportJobSourceBindingConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAssemblyIdentityConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAssemblyItemConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAssemblyPreparationDispositionConfig.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/RawExport/RawExportR2CompletionVerifier.cs
src/TagEkyc.Infrastructure/RawExport/RawExportFramedSourceVerificationService.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOptions.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyAuthenticationService.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyCodec.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblySourceResolver.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRepository.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/ReadinessEndpoint.cs
tests/TagEkyc.UnitTests/Tip88C1C1AssemblyCodecTests.cs
tests/TagEkyc.ArchTests/Tip88C1C1ResolverAssemblyArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B4RawExportJobFoundationTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs
tests/TagEkyc.IntegrationTests/Tip83E1ReadinessEndpointTests.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs
```

New migration/Designer names are exact. Do not modify a landed migration.
Fixture authentication/C2 implementations and keys live only as private test
helpers inside the new C1 integration test; there is no fixture provider under
`src/`.

`PostgresPersistenceFixture.BootstrapClusterPrerequisitesAsync()` creates or
verifies the two deployment-owned assembly LOGIN roles before any C1 migration
runs, with the exact attributes in §4.1 and no membership. The migration alone
creates capability roles and grants the two exact memberships. The fixture
change is additive and must preserve every existing DK-PROD role/bootstrap
check byte-semantically.

## 12. Task-0 gates

Before implementation:

1. verify exact branch/HEAD and staged-zero state;
2. verify scope and dispatch SHA, predecessor hashes and the exact R4-R6 final
   review bundle carried with Round 2;
3. verify all 39 allowlist paths are either current bytes or intentionally new;
4. extract the landed B4 CHECK and two function bodies for Down restoration;
5. prove all selected C1-A/AUTH/consent/source projection columns exist with
   the types assumed here;
6. prove the shared R2 parser can be extracted without changing canonical R2
   outcomes or golden vectors;
7. generate and independently recompute one absolute assembly/manifest/auth/C2
   vector before schema implementation;
8. prove no package or `.csproj` edit is needed;
9. STOP/RRI on any mismatch rather than silently editing this contract.

## 13. Proof manifest (C101–C126, 26 methods)

All proof ids map one-to-one to exact test methods. Count-neutral mutation
variants run inside the owning method and restore canonical bytes before the
next proof.

| ID | Required proof and RED discriminator |
|---|---|
| C101 | exact C1-A selection and ordered-class freeze; latest/best/caller-source mutation RED |
| C102 | missing/duplicate/mismatched selection/source fails with zero binding rows |
| C103 | source eligibility covers Available/object/key/staged/deadline/hold/revocation/reuse; remove each comparator independently RED |
| C104 | frozen binding exact replay and different-value conflict; update-to-latest mutation RED |
| C105 | resolver role reads exact restricted context; sealer/runtime/PUBLIC and cross-role access denied |
| C106 | exact durable object is re-opened; process-cache and wrong-credential reads RED |
| C107 | typed key/object/deterministic/historic failure partition; catch-all collapse mutation RED |
| C108 | shared framed-source service preserves every affected existing R2 outcome/vector; each moved R2 failure mapping retains its existing RED discriminator |
| C109 | pass-1 canonical stream yields absolute AssemblyDigest vector |
| C110 | ManifestDigest and exact ManifestDigest authentication yield absolute vectors; AssemblyDigest-target, omitted/changed authentication-key-id and omitted/changed authentication-key-version mutations independently RED |
| C111 | deterministic AssemblyId/C2PreparationId/fingerprints absolute vectors and independent recomputation |
| C112 | first fresh barrier before first read; withdrawal/expiry/hold wins with zero read |
| C113 | second fresh barrier immediately before Prepare; loss after pass 1 gives zero Prepare |
| C114 | Preparing is committed before provider call; provider response loss is recovered by exact inspect/replay |
| C115 | Pending requires exact preparation/provider receipt; mismatch and unknown do not advance |
| C116 | every C1 path follows the §8.2 lock subsequence, one post-lock clock and no blocking lock after it; each named adjacent inversion independently REDs by forced interleaving or exact B4 lock trace |
| C117 | loss while waiting on authority or consent lock fails freshness; pre-wait timestamp mutation RED |
| C118 | exact seal replay precedes stale revision and performs zero authority/provider/state mutation |
| C119 | bounded two-pass streaming; complete-buffer/temp-file/hash-discard mutation RED and owned scratch zeroed |
| C120 | atomic SealCommitted + identity/items + B4 head/event; each partial-write mutation RED |
| C121 | AssemblySealed is lease-ineligible/non-claimable/non-reclaimable and cannot reverse to Assembling |
| C122 | C2 Finalize only after committed seal; Finalize-before-seal and wrong-fingerprint mutations RED |
| C123 | abort only from durable AbortAuthorized; unavailable/unknown/caller boolean/one failure never aborts |
| C124 | table CHECK/FK/unique/append-only shapes, function signatures, owner/ACL/effective grants exact |
| C125 | migration apply/Down/reapply restores exact B4 CHECK/functions, no overload/grant/orphan, pending-model clean; deleting/weakening each landed CHECK branch and each landed guard context independently REDs before byte-exact restore |
| C126 | production host: Disabled valid; FixtureProof non-production only; every readiness failure returns exact code |

Required concurrency interleavings include C1 seal versus B4 renew/reclaim,
C1 freeze/seal versus R3/R4-R6 on the same source, two C1 sealers, and response
loss replay versus a fresh retry. PostgreSQL `40P01`, silent overwrite and
double Prepare/Finalize are not business outcomes.

## 14. Validation policy

During implementation:

- run only the changed C1 unit/architecture/integration tests and the exact
  affected B4/B1-E3/R2/readiness methods;
- run B4/DK-PROD or other expensive suites only when their owned bytes,
  migration/function/constraint/ACL surface or shared harness changes;
- run pending-model after the migration/model is generated and after every
  model-affecting correction;
- use one disposable database episode for affected tests; do not reset the
  entire shared integration database between individual proof methods.

At closeout, on final bytes, run exactly once:

```text
dotnet build TagEkyc.sln -c Release --no-restore
dotnet test TagEkyc.sln -c Release --no-build --no-restore
```

The second command is the complete unfiltered canonical Release suite. A
failed, filtered, stale or diagnostic run cannot replace it. Preserve raw logs,
test census, single intentional skip identity, pending-model result,
apply/Down/reapply evidence, mutation matrix and task-resource cleanup.

## 15. STOP/RRI

Stop before widening or inventing if:

- any permanent fortieth path or package change is required;
- C1-A selection or AUTH/consent inputs do not exist as assumed;
- R2 parsing cannot be shared without changing landed R2 behavior;
- C2 needs durable plaintext or unbounded buffering;
- a production authentication/C2/raw-source provider is required to make the
  fixture proof pass;
- a new source/job state, readiness code, B4 phase or public raw-byte contract
  is required;
- the seal cannot preserve exact replay-before-freshness and atomic B4 closure;
- the B4 Down restoration requires editing landed bytes;
- any named mutation stays GREEN or REDs for an unrelated reason;
- pending-model is non-clean or the final full suite does not produce a clean
  census on final bytes.

## 16. Required as-built and closeout report

The as-built is append-only by execution event and must contain:

- exact branch/baseline/final HEAD and all four document SHAs;
- allowlist and changed-path census;
- schema/function/signature/owner/ACL manifest and Down restoration hashes;
- all absolute vectors with independent recompute commands;
- C101–C126 canonical and mutation results, including raw-log hashes;
- two-pass peak-memory evidence and zeroization ownership statement;
- three fresh-barrier and forced-lock-wait evidence;
- C2 response-loss, replay, finalize and abort evidence;
- pending-model, Release build, final full-suite census and intentional skip;
- task-created PostgreSQL/object/provider/process residue cleanup;
- explicit deferrals and production activation gates;
- every review finding and disposition through Round 3.

Authorized success language after an independently reviewed implementation is
only:

```text
PASS — C1 FIXTURE RESOLVER + AUTHENTICATED ASSEMBLY PROOF BUILD CLOSED
```

It must not say production ready, real Raw BIO ready, package ready or delivery
ready.

## 17. Authority boundary

This candidate authorizes review only.

```text
Implementation:       NOT AUTHORIZED
Migration generation: NOT AUTHORIZED
Provider operation:   NOT AUTHORIZED
Stage/commit/push:     NOT AUTHORIZED
Production activation:NOT AUTHORIZED
Real Raw BIO:          NOT AUTHORIZED
R4/R5/delivery:        NOT AUTHORIZED
```

Round 3 reviews this exact successor SHA and only the bounded Round-2 closure:
manifest key-selector binding/vectors, path-39 pre-migration role bootstrap,
R4-R6 final-review provenance, ledger binding, vector-key wording, explicit
lock-order bite and predecessor-proof accounting. It must not reopen settled
planning without a directly conflicting carried byte. After Round 3,
executable uncertainty moves to code and tests under a ratified dispatch; a
fourth full-document review is prohibited by the phase convergence policy.
