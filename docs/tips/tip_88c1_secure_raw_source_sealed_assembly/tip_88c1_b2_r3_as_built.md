# TIP-88C1-B2-R3 - As-built evidence

Version: 0.4
Status: IMPLEMENTATION COMPLETE - READY FOR INDEPENDENT CLOSEOUT REVIEW
Evidence date: 2026-08-11
Repository baseline and current HEAD: `5c0be51d838b65d58d4026713148321b0d64e42b`
Ratified dispatch version: `0.4`
Ratified dispatch SHA-256: `5A426518A41E9A88F0C2A6D5FD2B5E7D30549FA81DDCEDAB8CA9284E9C8E0E8A`
Reviewed dispatch bundle SHA-256: `8CFB9F0C304202E5A1DE2CF590FC2CE1700CF78E716DF055A7D1880FE0C7737B`

## Outcome versus intent

R3 advances exactly one source attempt from a landed R2
`VerifiedCompleted` provisional object to the metadata-only `Staged` state.
It persists the exact verified ciphertext/evidence projection and the v2 staged
fingerprint, then advances the source head with a guarded revision/fence CAS.

The successful new-stage order is:

```text
lock attempt/head/reservation/claim/key/object rows
-> validate exact predecessor identity and revisions
-> acquire authority shared advisory lock
-> acquire consent shared advisory lock through the landed resolver
-> capture one clock_timestamp()
-> revalidate authority, consent window and both staging deadlines
-> persist the staged attempt shape
-> advance Reserved -> Staged with one head revision increment
```

The same captured timestamp is written as `StagedAtUtc`. An exact replay is
resolved from the complete persisted staged shape before fresh authority and
returns `ExistingMatch` without a second write.

R3 adds no raw-byte, plaintext-digest, DEK, object-key, credential, locator,
resolver, R4/R5, package or delivery surface. It performs no S3, key-provider,
AEAD, HMAC, raw-source or network operation inside the staging transaction.

## Persisted and executable surfaces

- Eleven nullable staging columns are added to the existing encryption-attempt
  row. The check constraint permits only the complete pre-stage or complete
  staged shape.
- The head-state constraint is expanded only from `Reserved` to
  `{Reserved, Staged}` for the active attempt path.
- `raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)`
  is `SECURITY DEFINER`, owned by `tagekyc_raw_export_deployer`, executable only
  by `tagekyc_raw_export_reconciler`, and accepts no digest, locator or raw data.
- The repository and staging service are internal and depend only on
  `TagEkycDbContext`/Npgsql. No DI, API, controller or runtime endpoint is added.
- `StagedCiphertextFingerprint` schema version 2 is derived independently in
  SQL and C# from locked durable metadata. The absolute expected vector is
  `6B7B1CE324D3CE6687C33A6D1AA6A58F90E17107EBC6133247E486CAB7144FC5`.

## Implementation defect found by full regression

The first unfiltered Integration run found one genuine shared-trigger
regression:

```text
C1B2CORE_source_reservation_is_append_only_even_as_owner
expected RAW_EXPORT_SOURCE_CORE_APPEND_ONLY
observed RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN
```

Root cause: the R3 re-emission kept `RAW_EXPORT_SOURCE_CORE_APPEND_ONLY` inside
the encryption-attempt-only branch. UPDATEs on sibling CORE tables therefore
fell through to the generic write-forbidden error.

The migration was corrected without changing the R3 contract: the two allowed
encryption-attempt UPDATE branches remain table-routed, while every other
`TG_OP='UPDATE'` reaches the landed append-only error before the generic
write-forbidden branch. Targeted CORE and R310 tests passed, followed by a clean
unfiltered Integration rerun.

Pre-closeout-correction migration SHA-256:

```text
3A76222A126462F740C9F7F01C7CE8A549D3EB89E0B0D089BF460346E0CD412B
```

## Independent-closeout RRI corrections

The 2026-08-11 closeout RRI identified three executable defects and two proof
gaps. They were corrected without changing the R3 contract or expanding the
14-path allowlist:

- the staged CHECK branch now requires all eleven staging fields to be
  explicitly non-NULL before applying version, length, digest and range checks;
  the migration, DbContext, Designer and ModelSnapshot carry the same predicate;
- the SECURITY DEFINER argument guard rejects NULL and zero UUIDs and rejects
  NULL or sub-one values for every expected revision/fence scalar with exact
  `P0001 / RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID`;
- R310 now separates semantic CHECK proof from trigger proof: every staging
  field is independently set to NULL on a complete staged row with only the
  attempt trigger isolated. The trigger phase opens a transaction, assumes
  `tagekyc_raw_export_deployer` with `SET LOCAL ROLE`, sets the exact local R3
  write context, and then proves rejection of partial staging. Attempt sibling
  cases first supply all eleven otherwise-valid staging values and then change
  exactly `Fence` or `CreatedAtUtc`. Head cases independently prove unchanged
  revision with `Staged`, incremented revision with unchanged `Reserved`, and
  a valid `Reserved` to `Staged` plus-one transition with exactly one of the
  three immutable head fields changed (`SourceArtifactId`,
  `CurrentEncryptionAttemptId`, or `Fence`);
- R306 injects an independent mismatched projection for every final authority
  and consent comparator, plus isolated reservation and absolute-source
  deadline fixtures. Every case returns `SourceRetentionNotAuthorized` and
  leaves attempt/head unstaged;
- Down now performs occupied preflight, drops the staging FK, revokes/drops the
  stage function and restores the exact R2 guards, then removes the remaining
  R3 index/check/columns. R301 creates a database from empty and migrates
  forward only to R2 before it captures the expected catalog/ACL fingerprint;
  the expected value therefore does not pass through candidate R3 `Down()`.
  It then proves occupied Down fails before DDL, recreates the forward-only R2
  baseline, applies R3, performs clean Down, and compares the exact fingerprint
  before reapply. The fingerprint includes guard bodies, checks, owner, ACL,
  `proconfig`, and R3 function/FK/index/column absence. This stronger proof
  exposed and closed representation drift in the restored R2 guard bodies.

Corrected ModelSnapshot SHA-256:

```text
D81B8593166009279EFC770A5DDD55C3020FD71A4A822CEBCCC54BF3FF0BA510
```

Final corrected migration SHA-256:

```text
020B9C75FBBF7582BD495245B57042C0FC3101C0FF579DFD37D786F4066F54DD
```

## Mutation evidence

Each executed mutation was applied alone, built, run against its named proof,
restored, and followed by a canonical positive control or the final complete
R301-R316 run.

| Proof | Mutation | Required RED observed |
| --- | --- | --- |
| R301 | remove occupied-data Down preflight | Down reached a downstream constraint error instead of `RAW_EXPORT_R3_DOWN_OCCUPIED` |
| R301-restore | change one semantic byte in the restored R2 head-guard message | forward-only R2 fingerprint and post-Down fingerprint differed at `RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN_MUTATION` |
| R306-arguments | remove the NULL guard for `p_expected_fence` | direct NULL input reached `RAW_EXPORT_ACTOR_CONTEXT_MISSING` instead of exact `RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID` |
| R304-new | remove the new-stage fence comparator | stale fence returned `Staged`, expected `StateConflict` |
| R304-replay | remove the replay fence comparator | stale replay returned `ExistingMatch`, expected `StateConflict` |
| R305 | widen accepted object states with `CleanupPending` | `CleanupPending` returned `Staged`, expected `StateConflict` |
| R306 | use pre-wait `transaction_timestamp()` | lock wait crossed expiry but returned `Staged`, expected `SourceRetentionNotAuthorized` |
| R306-authority | remove the `ControllerIdentity` comparator | the independently mismatched authority projection returned `Staged` |
| R306-consent | remove the `PurposeCode` comparator | the independently mismatched consent projection returned `Staged` |
| R306-deadline | remove the reservation-expiry comparator | the isolated expired reservation returned `Staged` |
| R307-key | remove the exact key-row lock | concurrent revoke completed while stage was blocked; serialization assertion RED |
| R307-authority | remove the authority shared advisory lock | stage no longer reached the expected authority serialization wait |
| R308 | swap receipt/evidence fields in both SQL and C# | the independent absolute fingerprint vector RED despite SQL/C# agreement |
| R309 | remove the six exact row locks | competing stage reached a duplicate attempt write and raised the append-only guard instead of one `Staged` plus one `ExistingMatch` |
| R310 | return from the attempt guard instead of raising append-only | raw DML reached a database constraint instead of the exact append-only guard |
| R310-shape | remove `StagedCiphertextDigest IS NOT NULL` from the staged CHECK branch | NULL digest UPDATE succeeded; `Assert.Throws<PostgresException>` RED |
| R310-attempt-fence | remove only attempt `Fence` from the R3 immutable tuple | otherwise-valid complete staging reached the composite FK (`23503`) instead of exact `P0001 / RAW_EXPORT_SOURCE_CORE_APPEND_ONLY` |
| R310-attempt-created | remove only attempt `CreatedAtUtc` from the R3 immutable tuple | otherwise-valid complete staging plus the timestamp mutation succeeded; the expected append-only exception was absent |
| R310-head-artifact | remove only head `SourceArtifactId` from the immutable tuple | valid state/revision transition reached the downstream FK (`23503`) instead of exact head-write rejection |
| R310-head-attempt | remove only head `CurrentEncryptionAttemptId` from the immutable tuple | valid state/revision transition reached the downstream FK (`23503`) instead of exact head-write rejection |
| R310-head-fence | remove only head `Fence` from the immutable tuple | valid state/revision transition reached the downstream FK (`23503`) instead of exact head-write rejection |
| R310-head-state | remove only the `Reserved` to `Staged` predicate | revision-plus-one with state left `Reserved` succeeded; the expected head-write exception was absent |
| R310-head-revision | remove only the plus-one revision predicate | `Staged` with unchanged revision succeeded; the expected head-write exception was absent |
| R311 | grant stage EXECUTE to `tagekyc_runtime` | exact negative privilege assertion observed `true` |
| R312 | add a `PlaintextDigest` result property | forbidden-surface reflection assertion RED |
| R313 | inject an `HttpClient` method-signature dependency | forbidden external-edge assertion RED |
| R315 | inject a `CommittedLocator` member | forbidden locator assertion RED |
| R316 | stale the snapshot tripwire constant | exact SHA assertion RED against the canonical snapshot |

R314's requested result/cache shortcut mutation is classified `N/A` by a
proved type-and-call invariant rather than reported as passing coverage. The
R3 command contains only actor/attempt/object identifiers and expected scalar
revisions; the internal service constructs the repository from the DbContext;
the repository always calls the database stage function; there is no result,
ciphertext, digest, provider or cache parameter/field that can be substituted
without first adding a forbidden contract surface. R314's canonical executable
path still performs landed R2 provider restart, exact durable verification and
then R3 staging. R312/R313/R315 independently fail if such a shortcut surface or
dependency is introduced.

## Canonical proof and regression results

```text
Release build:                    0 warnings / 0 errors
Pending model changes:            clean
R301-R312, R314, R316:            14 passed / 0 failed / 0 skipped
R313, R315:                       2 passed / 0 failed / 0 skipped
E3 affected proofs:               2 passed / 0 failed / 0 skipped
R2 affected regression:           18 passed / 0 failed / 0 skipped

Complete Release census:
Contract:                         13 passed
Architecture:                    122 passed
Unit:                            188 passed
Integration canonical rerun:     703 passed / 0 failed / 1 skipped
Aggregate:                      1,026 passed / 0 failed / 1 skipped
Total discovered:               1,027
```

The single skip is the intentional manual TIP-67G golden-vector generator.
The first full Integration execution (`702/1/1`) is retained as defect evidence;
the canonical post-fix rerun is `703/0/1`.

An additional closeout execution was classified environment-invalid because the
test process lacked the pinned SoftHSM module/util paths and Docker executable
path. No repository bytes were changed in response. The canonical unfiltered
rerun pinned SoftHSM2 2.5.0 module and utility from the same portable
distribution, preserved the Docker executable path, used the isolated
PostgreSQL 16 scratch instance, and produced `703/0/1` in 24.24 minutes. All
five SoftHSM E2E tests passed in that same unfiltered execution.

After the follow-up R301/R310 proof corrections and exact R2 guard restoration,
the complete unfiltered Integration suite was run again and produced `703/0/1`
in 24 minutes 14 seconds. After the final R310 comparator-isolation correction,
all seven independent comparator/predicate mutations were observed RED, the
canonical R310 and affected R2/R3 regression were green, and a final complete
Release run on the final bytes again produced `703/0/1` in 23 minutes 52
seconds; Contract/Architecture/Unit remained `13/122/188`, respectively.

## Canonical file hashes before this as-built

| Path | SHA-256 |
| --- | --- |
| `RawExportSourceEncryptionAttemptRow.cs` | `DC0F1DF21AF67EB180775CED4451D138B27618FDD77FDCB5DAD39B4D8A0EE84B` |
| `20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs` | `020B9C75FBBF7582BD495245B57042C0FC3101C0FF579DFD37D786F4066F54DD` |
| `20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.Designer.cs` | `DDCCE229B0BEE021DA743795FB95601AE82349018EFBCF3029B1BA2F76423708` |
| `TagEkycDbContextModelSnapshot.cs` | `D81B8593166009279EFC770A5DDD55C3020FD71A4A822CEBCCC54BF3FF0BA510` |
| `TagEkycDbContext.cs` | `447BD6B1DC6A9247E18F3E32AF987EAD6E5EA5729A19B907C306C5FDE98723DA` |
| `RawExportR3Contracts.cs` | `6204932A2998764FEDB32B33CCEB084DA2D5111DD523DE67E11F957A18B5B7E6` |
| `RawExportR3Repository.cs` | `2BF887219886A044080239F99C6CE14DAEF0938BD66B72369D571675F4C27F49` |
| `RawExportR3StagedCiphertextFingerprintCodec.cs` | `1260AFA9F24BE0FAB11EECE67A08BA79FFF07188BDFA0908E5878A08799659F9` |
| `RawExportR3StagingService.cs` | `5D047CF6A4C468DBCE44940E5BC002F7436DEDA6B2D22C041D3B5F5688B2F6BF` |
| `Tip88C1B2R3VerifiedCiphertextStagingArchTests.cs` | `DB444B6F55B0EA8858A78D45C25E7E9AD1F4260E96CECCE11971D6D0B85D0CCA` |
| `Tip88B1E3ResolverReadBoundaryTests.cs` | `87AFB992E6CF592525B0B86A423A1C58AE93FAFC208F32FF7F03E3922740D60E` |
| `Tip88C1B2R2DurableCustodyEncryptionTests.cs` | `C18AF3AC72E804D61E0FF80CE62709C7842B1608F99823ADAFB43F17AEEA9495` |
| `Tip88C1B2R3VerifiedCiphertextStagingTests.cs` | `15D429B30094E89EDA4723CC0086D789025AECE96A48E48ACBFFEC87414DE3F8` |

## Boundaries and non-claims

- This is a synthetic/fixture proof build, not real Raw BIO activation.
- `Staged` is metadata custody state, not `Available`, package-ready or
  delivered.
- No R4/R5, package assembly, delivery, public API, production raw-source
  adapter or production activation is implemented or authorized.
- No stage, commit, push, merge, PR or deployment was performed.
- Unrelated working-tree dirt was not edited, staged, cleaned or reverted.
