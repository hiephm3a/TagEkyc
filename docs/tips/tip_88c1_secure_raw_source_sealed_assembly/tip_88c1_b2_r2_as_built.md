# TIP-88C1-B2-R2 — As-built evidence

Version: 0.15
Status: F4 COMPLETE — READY FOR INDEPENDENT CONTROLLED-COMMIT REVIEW
Evidence date: 2026-08-10
Repository baseline and current HEAD: `3d7a50ec812f158c26373028fe7a2e97017125af`
Ratified dispatch SHA-256: `9A758E07D3283017AD84E489072F3EB7782EEFE526B1999DD14A53515633FFED`

## Scope disposition

R2 now provides the controlled synthetic-stream path:

```text
synthetic plaintext stream
→ durable key provisioning
→ framed bounded AEAD encryption
→ conditional single-part durable-object write
→ process/provider restart
→ exact-object read
→ typed AEAD verification
→ historic commitment verification
→ VerifiedCompleted
```

The implementation reuses the landed Durable Key, DK-FIXTURE-PROOF and Durable
Object boundaries. It adds no second key store, object store, write-session,
role topology or readiness topology. It does not provide real Raw BIO access, a
production raw-source adapter, R3 staging, package assembly or delivery.

## Implemented surfaces

- The landed AEAD contract now separates key-acquisition/provider failures from
  authenticated-ciphertext failures with typed verification outcomes.
- The R2 frame codec pins the header, ordered DATA frames, FINAL frame,
  length-prefixing, nonce namespaces, AAD and absolute golden vector.
- The encryption orchestrator streams bounded chunks without holding a database
  transaction across source, key, AEAD or object I/O.
- The completion verifier reads the exact durable object after restart, verifies
  framing and typed AEAD results, then verifies the historic content commitment
  before writing verification evidence.
- The repository exposes only the exact encryption/verification projections and
  the fenced termination transition. Plaintext, plaintext digest, DEK and raw
  ciphertext are not returned or persisted in PostgreSQL.
- `TerminatedBeforeStart` accepts zero object rows only for the exact locked
  attempt/key pair with durable `Revoked` or `ReservationAbandoned` evidence.
  Object-backed termination still requires the exact terminal object state.

## Migration and landed-function correction

Migration:

```text
20260807120000_Tip88C1B2R2DurableCustodyEncryption
SHA-256 78A55EAAE3A043D14CCCA8BAEF846B59A418426AEF2CF99FE84DB367A4BF6BD7
```

The migration adds only nullable `R2TerminatedAtUtc`, expands the existing
attempt-shape check for the two ratified terminal dispositions, creates the two
R2 read functions and the fenced termination function, and re-emits the landed
core-write trigger with its exact Down restoration.

The Homeowner-authorized landed Durable Object read-context correction was
implemented in the same migration. Exactly five return expressions are cast to
the declared text result types:

```text
reconcile: ObjectKey, State
lifecycle: ObjectKey, State, CleanupReasonCode
```

Five independent scratch removals each reproduced PostgreSQL `42804`; the
migration was restored after every run. R217 proves apply/Down/reapply restores
the landed definitions, owner, ACL and OIDs without an orphan overload.

The first full regression run also exposed a real sibling-table defect in the
re-emitted shared trigger: termination-only fields were resolved for UPDATEs on
`raw_export_source_reservations`, producing `42703` before the landed append-only
error. The trigger now routes on `TG_TABLE_NAME` in an outer branch before it
touches termination fields. The landed CORE test changed from observed `42703`
back to the required `P0001 / RAW_EXPORT_SOURCE_CORE_APPEND_ONLY`, while R215
continued to pass.

## Snapshot and E3 tripwire

Current ModelSnapshot SHA-256:

```text
1AC9DB8F7CB790AF51459D4886BDA26B510CF9F73C16D2B9A27B4FEE25A12BC1
```

The earlier padded snapshot at
`AD51325D3DA1CA5DEE7F1F26341028CA7A6709DA4C9D005D76BD43419B7FB3E0`
was rejected. It retained four obsolete `HasCheckConstraint` annotations before
their current replacements merely to satisfy the old DK-FIXTURE textual-
subsequence proof. Canonical EF generation removed those dead duplicates. In the
current snapshot, every protected `(entity/table, constraint-name)` annotation
occurs exactly once and carries its current effective expression.

The Homeowner-authorized DK-FIXTURE E3 correction replaced only the existing
`E3_ModelSnapshotDelta_IsAdditiveAndTripwireRoundTrips` implementation and its
private helpers. It now proves the fixture KEK journal's entity/table, primary
key, required property shape, indexes, relationships and protected named-check
semantics structurally. It rejects a duplicate/dead protected annotation,
removal of protected model structure and an unauthorized protected-expression
change, while accepting the canonical later-slice replacement. The proof remains
count-neutral and contains no snapshot hash constant.

The independently executed correction matrix was:

```text
Mutation A — duplicate/dead protected annotation: RED at exact uniqueness assertion
Mutation B — protected DK-FIXTURE structure removed: RED at exact structural assertion
Mutation C — protected expression changed: RED at exact semantic assertion
Positive D — canonical later-slice replacement: PASS
```

The snapshot was restored after each scratch mutation to
`1AC9DB8F7CB790AF51459D4886BDA26B510CF9F73C16D2B9A27B4FEE25A12BC1`.
The current method was also invoked directly during closeout-R1 remediation and
passed without changing repository bytes; the normal fixture initialization was
intentionally bypassed for that focused invocation because this proof reads only
the canonical snapshot and EF design-time model.

Formal pending-model result:

```text
No changes have been made to the model since the last migration.
```

`Tip88B1E3ResolverReadBoundaryTests.cs` retains the separate byte-exact
`ExpectedModelSnapshotSha256` tripwire bound to the hash above. The complete E3
class and the corrected DK-FIXTURE semantic proof pass in the unfiltered suite.
The semantic correction was made under the separate Homeowner packet
`TIP-88C1-B2-R2 — AUTHORIZE DK-FIXTURE SNAPSHOT PROOF CORRECTION`; it is an
authorized eighteenth closeout path, not an amendment of the ratified R2
17-path implementation allowlist.

## Proof manifest

Canonical positive controls:

```text
R201–R218 integration proofs: 18 passed / 0 failed / 0 skipped
R2A1–R2A4 architecture proofs: 4 passed / 0 failed / 0 skipped
```

The earlier whole-row statement that all 22 ratified mutations were RED is
superseded by the resumed per-mutation F4 sweep. The current evidence is recorded
per mutation; a proof ID is not treated as closed merely because one sibling
mutation made it RED.

R206 is closed with two independent layers: outward result/reflection checks and
IL inspection of the ENCRYPT path, VERIFY path and shared historic-commitment
helper. Four independent mutations were RED: managed-string conversion in
ENCRYPT, non-public outward digest exposure, managed-string conversion in the
shared helper, and managed-string conversion in VERIFY. The canonical verifier
includes the previously authorized R211 caller-cancellation mapping to
`VerificationIndeterminateRetry`.

The single conditional-Put mutation was found to be misassigned to R207. Its
canonical executable owner is R208: with the exact double-Put scratch mutation,
R207 remained PASS by design while R208 was RED at the existing assertion with
`Expected: 1` and `Actual: 2`. The orchestrator was restored byte-identically and
canonical R207 and R208 both passed. The mutation set is unchanged; only proof
ownership moved from R207 to R208.

The resumed R207 sweep independently made the provisioning-order, retryable
partition, operator-required partition, terminal-key partition and
trust-Terminated-without-inspection mutations RED, with canonical restoration and
positive control after each.

The direct construction mutation was reclassified from behavioral R207 to
structural R2A2. Count-neutral R2A2 IL inspection now resolves `newobj` operands
across every declared method of both orchestrator and verifier, including every
async state-machine `MoveNext` and private helpers. It rejects concrete types
assignable to key-provisioning, object-writer or object-reconciler capabilities.
Three independent mutations were RED for the exact R2A2 assertion: direct writer
construction in orchestrator `ExecuteAsync`, direct reconciler construction in
verifier `ExecuteAsync`, and writer construction moved into a private helper.
Both production files were restored byte-identically and canonical R2A2, R207
and R208 pass. R207 records the structural mutation as N/A by design.

R207 exact-profile admission was then exercised field by field rather than with
one representative mutation. Independently accepting alternate encryption suite,
framing version, chunk size, nonce strategy and RawClass each made the existing
corresponding `Assert.False` RED; canonical restoration and positive control
followed every mutation.

R208 was independently mutated so the writer attempted the reconciler-only
verified transition. The exact capability denial changed the writer disposition
from `PendingVerification` to `ReconciliationRequired`, making R208 RED; canonical
R208 passed after restoration. R209 invoked the object writer a second time after
both uncertainty outcomes and was RED at `Expected: 1`, `Actual: 2`; canonical
R209 passed after restoration. R210 invoked the encryption operation from the
verification/reconciliation path through the dual-capability probe and was RED at
`EncryptionCallCount`, `Expected: 0`, `Actual: 1`; canonical R210 passed after
restoration.

### R211 generalized deterministic-predicate isolation

The existing count-neutral R211 method now owns one canonical valid-object
control, the complete D1-D10 deterministic matrix and the unchanged U1-U6
uncertainty matrix. The R211-only dependency-repair helper starts from the
canonical synthetic object, mutates only the selected DATA type or ordinal,
recomputes the causally dependent DATA-frame digest, re-seals only FINAL with
the same synthetic DEK/nonce/AAD, and updates only the fixture's expected
whole-object digest. Its manifest asserts the target field, canonical/crafted
SHA-256 and length, changed serialized ranges, changed semantic fields,
recomputed dependants, re-sealed frames and fixture-context changes. A second
FINAL decrypt proves every semantic field outside the declared causal closure
equals the canonical value; unchanged serialized regions are byte-equal.

| Row | Target and dependency closure | Canonical rejection | Single-condition observation | Classification |
| --- | --- | --- | --- | --- |
| D1 | Invalid DATA frame type; repair DATA-frame digest, authenticated FINAL and fixture object digest | `RAW_EXPORT_R2_DATA_FRAME_INVALID` | Omitting only the DATA-type comparison returned `Verified`; R211 RED (`expected cleanup`, `actual Verified`) | `INDEPENDENTLY_ISOLATED` |
| D2 | Invalid DATA ordinal; same minimal downstream closure as D1 | `RAW_EXPORT_R2_DATA_FRAME_INVALID`, with the manifest anchoring the ordinal field | Omitting only the ordinal comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D3 | One trailing byte after canonical FINAL; no dependent repair | `RAW_EXPORT_R2_TRAILING_DATA` | Omitting only the trailing-data comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D4 | Individually re-encrypted completion fields; fixture digest follows the crafted object, while the canonical comparison target remains unchanged | `RAW_EXPORT_R2_COMPLETION_BINDING_INVALID`; finalized marker uses `RAW_EXPORT_R2_COMPLETION_INVALID` | Omitting the shared completion comparison returned `Verified`; separately omitting only the finalized-marker parser comparison also returned `Verified`; both made R211 RED | `INDEPENDENTLY_ISOLATED` |
| D5 | One envelope-metadata mismatch in authenticated FINAL; fixture object digest repaired | `RAW_EXPORT_R2_ENVELOPE_BINDING_INVALID` | Omitting only the envelope comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D6 | Canonical object with intentionally wrong frozen expected digest | `RAW_EXPORT_R2_OBJECT_DIGEST_INVALID` | Omitting only the object-digest comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D7 | Commitment provider succeeds, but computed commitment differs from the frozen stored commitment | `RAW_EXPORT_R2_CONTENT_COMMITMENT_INVALID` | Omitting only the mismatch comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D8 | Physical truncation removes required bytes; no legitimate repair can retain that target and still form a complete authenticated object | explicit short-read/truncation check rejects first; after its omission the zero-filled incomplete frame fails deeper AEAD authentication as `RAW_EXPORT_R2_AUTHENTICATION_FAILED` | Changing only zero-read handling to return left R211 GREEN for the required deeper rejection | `NOT_INDEPENDENTLY_ISOLABLE / DEFENSE_IN_DEPTH`, covered by authenticated-frame verification |
| D9 | Canonical object with intentionally wrong frozen expected length | `RAW_EXPORT_R2_OBJECT_LENGTH_INVALID` | Omitting the two enforcement points of the one shared object-length invariant returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |
| D10 | Invalid FINAL frame type with otherwise authenticated/canonical structure | `RAW_EXPORT_R2_FINAL_FRAME_INVALID` | Omitting only the FINAL-type comparison returned `Verified`; R211 RED | `INDEPENDENTLY_ISOLATED` |

D4's shared comparison covers `ChunkCount`, `TotalDataPlaintextLength`,
`DataCiphertextLength`, `DataCiphertextDigest`, authentication tag-chain,
content commitment and wrapped-key metadata. The finalized marker is deliberately
reported separately because it is owned by `ParseCompletion`, not that shared
comparison. No new diagnostic token was added.

The uncertainty matrix remained unchanged and was exercised against the exact
production branches. U1 key access and U2 caller cancellation each became RED
when routed to deterministic cleanup. U3 exact-object-read and U4 commitment
provider uncertainty share the verifier's outer generic retry branch; changing
that exact branch to cleanup made R211 RED at U3, and code inspection confirms
U4 reaches the same branch. U5 database-read uncertainty became RED when its
separate pre-context catch returned cleanup. U6 database-transition uncertainty
became RED when only the `MarkVerifiedAsync` catch returned cleanup. Every row
retains `ObjectPresentPendingVerification` and no cleanup evidence in canonical
form.

After every temporary condition change, production bytes were restored. Final
canonical hashes are:

```text
RawExportR2CompletionVerifier.cs
0A18785B2BDCB3530DC6881D4561BB27CF17B3A7727A199A19DB42402BE8558A

RawExportR2FrameCodec.cs
E8DAD1649ACF9D57F654F8D5EFA7874D777D276EA1B00604A983C0016D9BCC2C
```

The restored canonical R211 passed after the complete D/U sequence. R211 is
closed without counting D8 as an independently isolated proof. The remaining
F4 work resumes at R212-R218 and R2A1-R2A4 under the previously corrected proof
ownership; this document does not claim that remaining sweep complete yet.

Two independent reviews then returned `PASS — R211 CLOSED; F4 MAY RESUME`.
The first reviewed the complete evidence packet and explicitly limited its
verdict to evidence content. The second recomputed the four current hashes,
read the live R211 method/helper/manifest and confirmed count neutrality and
production restoration. Neither review authorized or claimed whole-F4 or R2
closeout.

### Resumed F4: R212 and R213

R212 is closed with three independent condition-omission checks against
`raw_export_mark_provisional_object_verified`:

- removing the NULL/length verification-evidence guard changed the required
  `P0001 / RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID` result to check
  violation `23514`, making R212 RED at the exact SQLSTATE assertion;
- removing the expected state-revision comparison changed stale-revision
  `StateConflict` to `Verified`, making R212 RED;
- accepting any digest for a completed replay changed different-digest
  `StateConflict` to `ExistingMatch`, making R212 RED.

The landed Durable Object migration was restored after every check to:

```text
24EB721704EC78C1B925A2E761B2ADF1FF157A3BE1BED957094C30582077AD99
```

Canonical R212 passed after restoration.

R213 was exercised separately for the writer and verifier database contexts.
Leaving an EF transaction active after the writer projection caused the first
external-boundary assertion to be caught by the orchestrator and changed the
writer result from `PendingVerification` to `ReconciliationRequired`; R213 RED.
Leaving an EF transaction active after the verifier projection caused the
reconciler boundary assertion to be caught by the verifier and changed the
result from `Verified` to `VerificationIndeterminateRetry`; R213 RED. The
repository was restored to:

```text
7BE40AC01FE99E5A74B8219258B450C76632906205A06C2D429C7F2514674BF9
```

Canonical R213 passed after restoration. Its canonical call-count and stream
read assertions continue to prove that key, AEAD, commitment, writer,
reconciler and caller-source boundaries are reached with no current
transaction.

### R214 exception-path zeroization closure

The original R214 method proved only successful completion. It did not inject a
failure after R2-owned plaintext, AEAD inputs or the historic commitment payload
had become non-zero. The Homeowner accepted that Class-2 coverage gap and
authorized count-neutral strengthening inside the existing R214 method and its
private helpers. No production behavior changed.

The expanded method preserves the original success assertions and adds four
pinned failure scenarios:

- E1 writes controlled non-zero plaintext directly into the exact destination
  `Memory<byte>` supplied by R2's source read, retains the same underlying array
  and throws `R214_SOURCE_READ_SENTINEL` before a successful byte count returns;
- E2 retains the exact `ReadOnlyMemory<byte>` backing arrays supplied to the DATA
  AEAD call and throws `R214_AEAD_SENTINEL` after DATA plaintext, nonce and AAD
  have non-zero anchors;
- E3 retains the exact encoded historic-commitment payload supplied to
  `IContentCommitmentService` and throws `R214_COMMITMENT_SENTINEL` after the
  payload is non-zero;
- the second-call AEAD scenario reaches FINAL, retains the exact completion
  plaintext, FINAL nonce and FINAL AAD backing arrays, then throws the same
  pinned AEAD sentinel.

Every exception scenario verifies exact sentinel identity and call count. Each
behavioral diagnostic stores the same array, offset and count observed at the
canonical boundary; it does not use a copied array as zeroization evidence. It
records a positive pre-failure non-zero count and then inspects that exact
storage after unwind. Explicit `Disposed`, `CanRead`, `CanSeek` and post-operation
read checks prove that the caller's source remains caller-owned in success and
every exception scenario.

#### Complete sensitive-buffer census

| Family | Allocation / first non-zero point | Canonical cleanup | Observability and proof owner | Assigned discriminator |
| --- | --- | --- | --- | --- |
| plaintext chunk/read buffer | `BuildDataFrameAsync`; source writes into R2 destination | DATA `finally` | behavioral E1, exact destination segment | omit plaintext `ZeroMemory` |
| DATA nonce | `NewNonce`; non-zero suffix copied into 12-byte nonce | DATA `finally` | behavioral E2, exact AEAD request backing | omit DATA nonce cleanup |
| DATA AAD | `CreateDataAad` | DATA `finally` | behavioral E2, exact AEAD request backing | omit DATA AAD cleanup |
| transient plaintext digest | `plaintextHash.GetHashAndReset` into async field | FINAL outer `finally` | structural: exact producer-to-field, same-field `ZeroMemory`, protected `finally` | omit plaintext-digest cleanup |
| DATA ciphertext digest | `dataCiphertextHash.GetHashAndReset` into async field | FINAL outer `finally` | structural: exact producer-to-field, same-field `ZeroMemory`, protected `finally` | omit data-digest cleanup |
| historic commitment payload | `BuildHistoricCommitmentPayload` | FINAL outer `finally` | behavioral E3, exact commitment-boundary backing | omit payload cleanup |
| completion plaintext / FINAL plaintext scratch | `SerializeCompletion` | FINAL outer `finally` | behavioral FINAL AEAD failure, exact request backing | omit completion cleanup |
| FINAL nonce | `NewNonce` with FINAL namespace | FINAL outer `finally` | behavioral FINAL AEAD failure | omit FINAL nonce cleanup |
| FINAL AAD | `CreateFinalAad` | FINAL outer `finally` | behavioral FINAL AEAD failure | omit FINAL AAD cleanup |
| computed commitment comparison buffer | `ContentCommitmentResult.Mac.ToArray` into local | inner fixed-time-comparison `finally` | structural: exact producer-to-local, same-local `ZeroMemory`, protected `finally` | omit comparison-buffer cleanup |
| AEAD ciphertext output | successful DATA/FINAL operation result | matching AEAD-result `finally` | behavioral, exact provider-returned arrays retained by fixture | omit DATA and FINAL output cleanup independently |
| AEAD authentication tag | successful DATA/FINAL operation result | matching AEAD-result `finally` | behavioral, exact provider-returned arrays retained by fixture | omit DATA and FINAL tag cleanup independently |
| nonce-generation scratch | `randomBytes(11)` return | `NewNonce` `finally` | behavioral, fixture retains exact returned array | omit suffix cleanup |
| retained DATA nonce suffix | exact uniqueness copy in `dataNonceSuffixes` | stream `Dispose` | behavioral, reflected exact stored arrays | omit retained-suffix cleanup |
| header digest | constructor hash | stream `Dispose` | behavioral, reflected exact field | omit header-digest cleanup |
| envelope metadata digest | constructor codec | stream `Dispose` | behavioral, reflected exact field | omit envelope-digest cleanup |
| authentication chunk commitment | initial/next tag-chain commitment | old value on update; current value on `Dispose` | behavioral, reflected exact field | omit dispose cleanup |
| serialized frame/current scratch | header, DATA and FINAL frame returned by `BuildNextAsync` | `ReleaseCurrent` | behavioral, exact reflected `current` array across next read | omit current-frame cleanup |

The structural proof resolves `BuildFinalFrameAsync` through
`AsyncStateMachineAttribute` to the real `MoveNext`, parses compiled IL with BCL
reflection only, identifies the exact async field or local from its producer and
store, binds the same load through `Span<byte>.op_Implicit` to
`CryptographicOperations.ZeroMemory`, and requires that cleanup call to be in a
`finally` whose protected region starts at or immediately after the producer
store. Merely finding an unrelated `ZeroMemory` call cannot pass.

The canonical valid path was also inspected for possible sibling buffers. The
one-byte EOF guard is zero on the valid path and therefore never contains
sensitive material in this execution. Serialized headers and `current` frames
contain metadata/ciphertext rather than plaintext or key material; `current` is
nevertheless retained in the census as frame scratch and proven zeroized.
Context arrays remain caller-owned frozen evidence. `IncrementalHash` internal
state is BCL-owned cryptographic state rather than an R2-owned byte array and is
disposed by stream cleanup. No canonical sensitive R2-owned byte-buffer family
is left `NOT_OBSERVABLE`.

#### Independent condition-omission evidence

Each condition was omitted separately, the existing R214 method went RED for
the intended family, production was restored, the stream SHA was recomputed and
canonical R214 passed before the next condition:

```text
E1 plaintext destination       -> exact retained byte remained 114
E2 DATA nonce                  -> exact retained byte remained 1
E2 DATA AAD                    -> exact retained byte remained 23
E3 commitment payload          -> exact retained byte remained 44
plaintextDigest structural     -> R214_FIELD_NOT_RESOLVED:transient plaintext digest
dataDigest structural          -> R214_FIELD_NOT_RESOLVED:DATA ciphertext digest
computed commitment structural -> R214_LOCAL_STORE_NOT_RESOLVED:computed commitment comparison buffer
completion plaintext           -> exact retained byte remained 1
FINAL nonce                    -> exact retained byte remained 1
FINAL AAD                      -> exact retained byte remained 24
nonce-generation scratch       -> exact retained byte remained 1
DATA AEAD output               -> exact retained byte remained 114
DATA AEAD tag                  -> exact retained byte remained 127
FINAL AEAD output              -> 163 of 181 exact retained bytes remained non-zero
FINAL AEAD tag                 -> all 16 exact retained bytes remained 127
header digest                  -> exact retained byte remained 40
envelope metadata digest       -> exact retained byte remained 8
authentication commitment      -> exact retained byte remained 121
retained DATA nonce suffix     -> exact retained byte remained 1
serialized frame/current       -> exact retained byte remained 84
dispose caller source          -> explicit Disposed expected false, actual true
```

This includes the earlier success-only plaintext omission, where all 25 bytes
remained non-zero, but does not use that earlier observation as a substitute for
E1. After ordering exception scenarios before the preserved success assertions,
the E1 omission reached the exact source sentinel and failed at the retained
destination buffer itself.

Final canonical production hash:

```text
RawExportR2FramedCiphertextStream.cs
BCCBE6AB26C3797E3906CD54ECEC24C0302C1B92222D5FC17B9D8CCD058C9990
```

Canonical expanded R214 passes. R214 is closed and F4 may resume at R215–R218
and R2A1–R2A4; this version does not yet claim those remaining rows closed.

### R215 terminal-key accepted-set closure

The resumed sweep first classified the exact dispatch mutation that admitted
`ProviderCorruptOrUnverifiable` and `AbandonRequested` as a real coverage hole.
The authorized count-neutral strengthening now drives the persisted key-head
partition through landed DK-PROD functions and exact actor/capability roles.

Driven accepted states:

```text
Revoked | ReservationAbandoned
```

Driven rejected states:

```text
PreparingLive
ProviderOutcomeUnknown
ProviderCorruptOrUnverifiable
ProviderCleanupRequired
ReadyForFreshPreparation
Active
AbandonRequested
```

Every scenario proves zero provisional-object rows, exact attempt/revision/fence,
the exact same `AttemptKeyReservationId`, unchanged non-termination fields and
no mutation on rejection. `PreparingExpiredAwaitingResolution` is not bounded-
fixture-reachable through a landed surface because the current profile fixes a
15-minute live lease and exposes no test clock; its transition is owned by the
landed lease predicate and DK-PROD expiry proof. `AlreadyRevoked`,
`AlreadyAbandoned`, `CorruptOrUnverifiable`, `InProgress`, `NotActive` and the
`Cleanup*` tokens are function outcomes or provider-operation mapping states,
not values admitted by the `PreparationDisposition` CHECK; they cannot be added
to the R215 persisted-state partition without violating that named invariant.

Independent discriminators:

```text
widen with ProviderCorruptOrUnverifiable -> Expected StateConflict / Actual Terminated
widen with AbandonRequested              -> Expected StateConflict / Actual Terminated
narrow by removing Revoked               -> Expected Terminated / Actual StateConflict
narrow by removing ReservationAbandoned  -> Expected Terminated / Actual StateConflict
```

After every discriminator the migration was restored to the hash below, rebuilt
with zero warnings/errors and canonical R215 passed. The terminal-key accepted-
set gap is closed.

### R215 object-state accepted-set closure

The Homeowner-authorized count-neutral strengthening drives the complete
persisted provisional-object state partition through landed functions/providers
and explicit capability roles. No raw `UPDATE` manufactures a state.

Accepted for `Terminated`:

```text
Deleted | Quarantined
```

Rejected with `StateConflict`:

```text
Initiated
PutInFlight
PutOutcomeUnknown
ObjectPresentPendingVerification
VerifiedCompleted
NoObjectEstablished
ObjectConflict
CleanupPending
```

For every scenario R215 proves one and only one object row, exact
`ObjectCustodyId`/`AttemptId`/`AttemptKeyReservationId` binding, exact attempt
revision/fence, and the exact pre-termination object state. Rejected calls leave
the attempt and object row byte-for-byte stable with both termination fields
NULL. Accepted calls change only `R2TerminationDisposition` and
`R2TerminatedAtUtc`; the object row and state remain unchanged. The Deleted path
also retains the existing proof that termination blocks on the exact object row
lock.

Independent object-state discriminators were RED for the intended assertions:

```text
remove the complete state predicate
  -> Initiated: Expected StateConflict / Actual Terminated
widen with ObjectPresentPendingVerification
  -> Expected StateConflict / Actual Terminated
widen with CleanupPending
  -> Expected StateConflict / Actual Terminated
narrow by removing Deleted
  -> Expected Terminated / Actual StateConflict
narrow by removing Quarantined
  -> Expected Terminated / Actual StateConflict
```

The remaining R215 discriminators were also RED:

```text
accept an already-terminal row under a different disposition
  -> Expected StateConflict / Actual ExistingMatch
permit and write a non-termination ChunkSize change
  -> stable attempt snapshot changed from 1048576 to 1048577
```

After each discriminator the migration was restored byte-identically to
`78A55EAAE3A043D14CCCA8BAEF846B59A418426AEF2CF99FE84DB367A4BF6BD7`,
rebuilt, and canonical R215 passed. All ten persisted object states were
fixture-reachable through landed surfaces, so the object-state honesty clause
has no unreachable row.

### R216 and R217 closure

R216 was exercised by temporarily granting reconciler `INSERT` on
`raw_export_source_encryption_attempts`. The existing exact privilege assertion
changed from false to true and R216 was RED. After restoration to the canonical
migration SHA above, canonical R216 passed and still proved one terminated
attempt with no replacement.

R217 was exercised by omitting the Down drop for
`raw_export_read_source_encryption_context(uuid,bigint,bigint)`. Reapply failed
with PostgreSQL `42723` because the orphan function still existed. After exact
restoration, canonical apply/Down/reapply passed.

### R218 durable-read-after-restart closure

The count-neutral R218 strengthening now proves the byte source consumed after
MinIO restart. The landed Writer credential, which has no `GetObject`
capability, was used as the wrong reconciler credential. A direct exact-key
`GetObject` with that credential returned HTTP 403, providing the anti-vacuity
anchor before the verifier ran.

The verifier using that wrong credential returned
`VerificationIndeterminateRetry`. Whole-row snapshots of the exact provisional
object and the source attempt/key binding were identical before and after the
call: the object remained `ObjectPresentPendingVerification`, cleanup evidence
remained absent, and both R2 termination fields remained NULL. A fresh verifier
using the landed Reconciler credential then reread the object and returned
`Verified`. The persisted row reached `VerifiedCompleted`; object custody ID,
attempt ID, key-reservation ID, plaintext length, content commitment,
ciphertext length/digest and verification-evidence digest matched the exact
durable-object and frozen source bindings.

The required scratch replacement cached the ciphertext before restart and
served it from process memory to the wrong-credential verifier. The direct
wrong-credential MinIO probe still returned 403, but the cached verifier returned
`Verified`; R218 was RED at the intended assertion:

```text
Expected: VerificationIndeterminateRetry
Actual:   Verified
```

The cache adapter was removed. The integration-test file was restored
byte-identically to
`FBC8D7A1D14E16CD0CD317C9A445681CC7F41613FA11B14EE678C847FA6335D0`,
and canonical R218 passed 1/1. No production behavior changed.

### R2A1–R2A4 mutation-cell closure and open proof-quality debt

Canonical architecture proofs passed 4/4. R2A2 retains its previously recorded
three independent direct-construction mutations. The remaining architecture
rows were exercised independently:

- R2A1 added an executable R2 `CreateMultipartUpload` symbol and failed at the
  exact forbidden-symbol assertion;
- R2A3 added a non-public `PlaintextDigest` result property and failed at the
  exact reflected-property assertion;
- R2A4 added a public `AddTagEkycRawExportR2(IServiceCollection)` production
  registration extension and failed at the exact activation assertion.

After every mutation, `RawExportR2Contracts.cs` was restored byte-identically to
`E3D7EF8BBC55F381D7465530422556C4E9F21BC2294A272EB0F0123043592E4C`.
The combined canonical R2A1–R2A4 run then passed 4/4.

This closes the ratified named mutation cells; it does not make every negative
architecture assertion rename- or indirection-safe. R2A1, R2A3 and R2A4 still
contain `File.ReadAllText` plus textual `DoesNotContain` sentinels. Those checks
prove the pinned forbidden spellings used by the ratified mutations, but an
equivalent implementation expressed through renamed symbols or indirection can
escape that textual layer. The additional reflection assertions in R2A3/R2A4 do
not eliminate the remaining source-text dependency.

Known test-quality debt:

```text
R2A1 / R2A3 / R2A4 source-text negative proofs remain
rename- and indirection-sensitive.
```

Homeowner explicitly deferred this known test-quality/proof-robustness debt for
the R2 closeout under authority packet SHA-256
`4AF08AFF143015FA76EAA314450B14B68044487C989BC63876D9714BE48001FC`.
The exact named mutation cells remain CLOSED; the broader rename/indirection-
resistant negative architecture guarantee remains NOT PROVEN by the textual
layer. This governance acceptance makes the debt non-blocking for R2
controlled-commit review, but does not represent it as technically eliminated.
Existing textual and reflection checks remain unchanged and unweakened. Future
hardening requires separate Homeowner authority and should use compiled
reference, resolved identity, IL/call-graph, dataflow, dependency-graph or
host-composition evidence where applicable.

The sweep found and corrected two vacuous forms before closeout:

- R206 originally inspected only public properties, so a non-public
  `PlaintextDigest` mutation remained green. The proof now scans public and
  non-public properties and the same mutation is RED.
- R215 originally tested stale revision/fence while a nonterminal key state
  rejected the request first. The stale checks now run after durable key
  termination; removing either revision or fence predicate produces an
  observable incorrect `Terminated` result and turns R215 RED.

The R208 writer mutation was denied by the exact ACL before verification state
could be written, proving the writer cannot inspect or mark verification. R216
separately proves the reconciler cannot call the R1 completion surface or insert
a replacement attempt.

## Final F4 validation

Release build:

```text
0 warnings / 0 errors
```

Unfiltered Release test census:

```text
Contract:       13 passed / 0 failed / 0 skipped
Architecture:  120 passed / 0 failed / 0 skipped
Unit:          188 passed / 0 failed / 0 skipped
Integration:   689 passed / 0 failed / 1 skipped

Aggregate:    1010 passed / 0 failed / 1 skipped / 1011 total
```

The single skip is the intentional manual TIP-67G golden-vector generator.

Pre-canonical attempts are not hidden. The first targeted run used the default
PostgreSQL fixture without the DK-PROD extension topology and stopped before the
test body with PostgreSQL `42883`. The first solution-wide run was invalid
because concurrent projects perturbed process environment: SoftHSM was unbound
and the integration child process could not resolve Docker. Two subsequent
integration-only runs exceeded harness limits of 10 and 15 minutes while their
child processes continued normally; those child processes were explicitly
removed. The canonical integration run used one testhost, the pinned SoftHSM
2.5.0 distribution, Docker CLI and isolated PostgreSQL with the exact extension
and role prerequisites. It completed in 22 minutes 6 seconds and produced the
census above. Contract, Architecture and Unit were then rerun sequentially.

`git diff --check` passes and the staged set is empty. No commit, push, merge,
PR, deployment or production activation occurred.

The disposable `tagekyc-r218-postgres` container and compose PostgreSQL were
removed after validation. Ephemeral Durable Object containers were absent at
handoff. The already-provisioned DK-FIXTURE SoftHSM 2.5.0 portable prerequisite
was reused rather than created by R2 and was left unchanged.

## Final file hashes

| Surface | SHA-256 |
| --- | --- |
| `AttemptAeadOperationContracts.cs` | `32F225939341B11163938FEB32DD2C5E55CBFC9C5A68DCD6CE1BC68343DF6D97` |
| `AttemptAeadOperationService.cs` | `D2812E1C7FD6960EBDADFD7567B0F02CBAE93AF4281657D5A98924580F4EE09E` |
| `RawExportR2Contracts.cs` | `E3D7EF8BBC55F381D7465530422556C4E9F21BC2294A272EB0F0123043592E4C` |
| `RawExportR2FrameCodec.cs` | `E8DAD1649ACF9D57F654F8D5EFA7874D777D276EA1B00604A983C0016D9BCC2C` |
| `RawExportR2FramedCiphertextStream.cs` | `BCCBE6AB26C3797E3906CD54ECEC24C0302C1B92222D5FC17B9D8CCD058C9990` |
| `RawExportR2EncryptionOrchestrator.cs` | `B2FF9A3606D269308F2CDCD1EF7C95D43067CD53C7D42DA5727CFF54D575860D` |
| `RawExportR2CompletionVerifier.cs` | `0A18785B2BDCB3530DC6881D4561BB27CF17B3A7727A199A19DB42402BE8558A` |
| `RawExportR2Repository.cs` | `7BE40AC01FE99E5A74B8219258B450C76632906205A06C2D429C7F2514674BF9` |
| `RawExportSourceEncryptionAttemptRow.cs` | `61DC7373AB8941A1EC076F3FAEA6E57054BD80DB18877E980F42277EC74C9A62` |
| `TagEkycDbContext.cs` | `1EF43A74562B62C14DC1F6667689260ECB30928B7B2DF7E2A8C10389DF9A4F34` |
| R2 migration | `78A55EAAE3A043D14CCCA8BAEF846B59A418426AEF2CF99FE84DB367A4BF6BD7` |
| R2 migration designer | `57E00509BB81A669D3F3D06CC48606FC1C99E0791AE6C4002F8BC889E7AB658E` |
| ModelSnapshot | `1AC9DB8F7CB790AF51459D4886BDA26B510CF9F73C16D2B9A27B4FEE25A12BC1` |
| R2 integration proofs | `FBC8D7A1D14E16CD0CD317C9A445681CC7F41613FA11B14EE678C847FA6335D0` |
| R2 architecture proofs | `89CDC16122780D338FDAC40EAAC6D4061C5DAC6A7502AA395B6D366C935F5A9D` |
| E3 tripwire test | `D20941C92728F623F0525CB1AEC4E4CD1DFD9F847E33AF790AD95BA718C0E5BB` |
| DK-FIXTURE semantic E3 correction | `C8F5653315B2F049CF911135FAAEF14694ED280FDDA8EF8A72971059401A5970` |

## Remaining boundary

The direct-provider, EOF-fixture, R211 dependency-isolation, R214 exception-path,
both R215 accepted-set and R218 recovery-source RRIs are closed. R212, R213,
R216 and R217 are also closed. The ratified R2A1–R2A4 mutation cells are closed,
while the R2A1/R2A3/R2A4 source-text proof-quality limitation remains KNOWN,
NOT FIXED and EXPLICITLY DEFERRED BY HOMEOWNER. It is non-blocking for the next
independent controlled-commit review. F4 execution is complete. Commit, push,
production raw-source integration, R3 staging, package assembly and delivery
remain outside this build authority.
