# TIP-88C1-B2-R4-R6 source finalization - as-built evidence

Version: 0.9
Status: POST-CORRECTION EXECUTION CLOSED - INDEPENDENT R5 CLOSEOUT REVIEW PENDING
Evidence date: 2026-08-15
Repository baseline and current HEAD: `81f02ab4668de4ac3d8ec2b9ba3cceb4a50151e4`
Branch: `tip-88a-raw-export-policy-catalog-build`
Ratified dispatch version: `0.4`
Ratified dispatch SHA-256: `CA3A9B50898563AAB9FA9DA431F4A8A4FD9A82CD1127AF8F7DDE6A1DAE1A58C1`
Reviewed dispatch bundle SHA-256: `CFF9B78804693E102351D6A190882636D8F7224E3ED1B4ED2BAC3EB46B1ED2CE`

## Outcome versus intent

The slice implements the metadata-only finalization sequence after landed R3:

```text
Staged
-> R4 Committed
-> R5 Available
-> R6 durable cleanup planning and settlement
```

R4 commits exactly one staged winning attempt and records the publication
identity, frozen staged fingerprint and commit evidence. R5 publishes only a
freshly authorized and consented committed source, creates an opaque committed
locator, moves the head to `Available`, and durably plans cleanup only for
obsolete object/key resources. R6 is capability-separated into two durable
passes. Reconciliation reads the exact cleanup item/context, performs only its
authorized provider/object reconciliation, persists or observes durable
acknowledgement/resource evidence, and returns non-terminally. After a possible
process/instance loss, lifecycle fresh-reads the item and typed resource/event,
performs any authorized terminalization, completes the item, and finalizes the
publication only after all exact items are durably complete. `Available` and
the winning resources remain unchanged throughout cleanup.

The implementation preserves the ratified common lock order:

```text
publication when present
-> encryption attempts in ascending identity order
-> source head
-> source reservation
-> ingress claim
-> key reservations in ascending identity order
-> provisional objects in ascending identity order
-> cleanup items
-> typed lifecycle events
-> advisory authority/consent locks
-> one post-lock clock
```

R6 locks the exact resource before its cleanup item and revalidates the item
probe after both locks. Exact response-loss replay is evaluated before generic
stale-revision rejection. `Available` is stable while cleanup is pending or
retryable. The winning object stays `VerifiedCompleted`; the winning key stays
`Active`.

No Raw BIO, plaintext, plaintext digest, DEK, KEK, bucket/key locator,
credential, resolver/assembly, package, or delivery surface was added.

## Persisted and executable surfaces

- `raw_export_source_publications` records the append-once R4/R5 publication
  lifecycle and the opaque committed locator.
- `raw_export_source_cleanup_items` records one durable cleanup obligation for
  each obsolete object or key resource. Winning resources are excluded.
- R4 moves the publication row to `Committed` while the source head remains
  `Staged`. R5 moves the publication row to `Available` and the source head
  directly from `Staged` to `Available`, while retaining the landed R1-R3
  guard branches.
- The five `SECURITY DEFINER` functions are:
  - `raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)`;
  - `raw_export_publish_available_source(uuid,bigint,bigint)`;
  - `raw_export_read_next_source_cleanup_item(uuid,bigint)`;
  - `raw_export_complete_source_cleanup_item(uuid,bigint)`;
  - `raw_export_finalize_source_cleanup(uuid,bigint)`.
- All five are owned by `tagekyc_raw_export_deployer`. Commit and publish are
  executable by `tagekyc_raw_export_reconciler`; cleanup-read is executable by
  both `tagekyc_raw_export_reconciler` and `tagekyc_raw_export_lifecycle`;
  cleanup-complete and cleanup-finalize are executable by
  `tagekyc_raw_export_lifecycle`. The owner retains PostgreSQL owner authority.
  PUBLIC, runtime, application-login, encryptor, and unrelated capability
  access is denied.
- Direct table privileges remain absent for runtime and capability identities;
  writes must pass the exact function-local write context and row guards.
- Publication and cleanup services are internal infrastructure types and add
  no endpoint or DI registration.

## Evidence codecs and absolute vectors

SQL and C# independently reproduce these canonical values:

| Domain | Preimage bytes | SHA-256 |
| --- | ---: | --- |
| `tip-88c1-source-commit-v1` | 364 | `2df9964cfb6d0148248d9c893022607737c06d687075f9037ac262aa37ef3dba` |
| `tip-88c1-source-available-v1` | 434 | `46186c52cf2101d6cb12aab53a946aba1148e25dda4e49d81b82b6514309e2de` |
| `tip-88c1-source-finalization-cleanup-v1` | 172 | `5b3899bc0678e04876dabf2647dc64281bbf6f053e8e828891fed38e77116c4f` |
| `tip-88c1-source-object-cleanup-item-v1` | 260 | `7cbdb5fe1a882fc150399f612005e6786854c1ae587238110f9894af362e8d92` |
| `tip-88c1-source-key-cleanup-item-v1` | 257 | `a1c47ba47a3d8c02d928df4c2fb737ad399b5971698c0fe666296b95836505ed` |

Commit/available vectors bind the winning object and key. Cleanup-item vectors
bind the obsolete resource identity. No `CleanupItemId` is smuggled into an
evidence preimage.

## Executable defects found and corrected

### Branch-local SQL record dereference

The first F409/F411 execution raised PostgreSQL `55000` because the cleanup
planner dereferenced fields of a PL/pgSQL `record` that had not been assigned
on the opposite resource-kind branch. The function now branches on
`ResourceKind` before reading object-only or key-only fields. This is an
implementation correction only; the resource census, state model, signatures,
tokens, codec and ACL contract did not change.

### Caller-selected cleanup resource

The first service shape could pair a caller-selected resource identity with a
separately read cleanup item. `SettleNextAsync` now reads the next durable item
and internally dispatches exactly that returned `ResourceKind` and
`ResourceId`. No external caller can choose a second resource.

### Key terminal-evidence reason

The initial key cleanup proof accepted any landed terminal revocation or
abandonment event. That was too broad for source-finalization ownership. The
migration now requires the exact reason
`SourceFinalizationSupersededAttempt` for both the `Revoked` and
`ReservationAbandoned` evidence branches. F411 asserts the exact event reason
and its 32-byte evidence digest. A different valid lifecycle reason returns
`ResourceNotTerminal`.

These corrections stayed within the ratified schema and function surfaces.

### Independent-closeout remediation

The first independent closeout bundle exposed five executable/proof gaps. The
remediation stayed inside the ratified 18-path allowlist and did not change the
R4-R6 state names, public function signatures, ACL contract, codecs, or proof
census:

- R4 uses the ratified attempt-row lock, `INSERT ... ON CONFLICT DO NOTHING`,
  and a durable publication re-read to adjudicate a lost first-commit race. No
  private R4 advisory mutex remains. A real two-caller race proves exactly one
  `Committed`, one `ExistingMatch`, and one publication row.
- Both R4 replay branches bind the caller's expected reservation revision to
  `StagedFromReservationRevision + 1`; a changed-command replay returns
  `StateConflict`.
- R5's post-lock admission proof now forces both authority-lock and consent-lock
  waits past the live retention window. The single post-lock
  `clock_timestamp()` rejects both with `SourceRetentionNotAuthorized` and zero
  publication mutation.
- R6 rejects a cleanup item that names the winning object or winning key before
  any lifecycle/provider operation. Object `PutOutcomeUnknown` reconciliation
  uses two durable positive-absence observations and persists the authorized
  acknowledgement while the item remains `Pending`. An indeterminate object
  remains `Pending`; this slice contains no orchestration that creates
  `Quarantined` from process-local state or a caller flag. Key
  `ProviderOutcomeUnknown` reconciliation performs only the landed provider
  resolution/cleanup acknowledgement and returns non-terminally. A later
  lifecycle invocation fresh-reads that durable state, performs deletion,
  revocation, or final-abandon terminalization when its exact preconditions are
  present, and only then completes the item and converges the publication.
- F412 now checks exact `regprocedure` identities, return columns/order/types,
  owners, SECURITY DEFINER/search paths, EXECUTE ACLs, login-to-capability
  membership options, effective EXECUTE edges, and typed service boundaries.
  F413 now includes exact PK and publication-FK negative cases.
- F401, F404, and F411 compare the SQL-persisted digest to the independent C#
  codec rather than testing the C# codec alone.

### Closeout correction authority

The final closeout correction removed the non-ratified private R4 advisory
mutex, made both R4 replay paths return the original
`StagedFromReservationRevision + 1`, restored the finalization write-context on
the `ON CONFLICT` mismatch exit, and made `CleanupItemId` an independent UUID.
F409 uses equal object/key resource UUIDs and still observes two distinct
cleanup-item identities, proving that cross-kind resources cannot collide on
the cleanup primary key. F413 drives each R5 head-guard comparator independently
and requires exact `P0001 / RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN` before a
sibling constraint can adjudicate the mutation.

The following gate is OPEN:

```text
R4R6-DURABLE-O-T15-QUARANTINE-EVIDENCE
```

It owns future orchestration from bounded, durable O-T15 failure evidence to a
quarantine transition. Until a separately authorized slice lands that evidence,
`ProviderUnavailable`, `OutcomeUnknown`, a failed delete, and `CleanupPending`
remain retryable `Pending` states. The gate authorizes no current schema, state,
function, readiness code, or implementation path. R6 may consume a
`Quarantined` row only when that terminal state and its evidence were already
created by a separately authorized landed surface.

## Proof census and mutation evidence

### R1 independent-review history and F-01 correction

The first independent closeout review did not pass. Its disposition is
preserved verbatim in substance:

```text
F-01 OPEN - capability-separation blocker
F-02 OPEN - PostgreSQL exact-event adjudication blocker
F-03 OPEN - MinIO raw packaging evidence blocker
```

Findings reconciliation recorded:

```text
F-01 CONFIRMED
F-02 CONFIRMED
F-03 CLOSED by packaging addendum
```

The historical R1 packaging closure bundle is
`TIP88C1-B2-R4R6-closeout-r1-minio-raw-evidence-20260815.zip`, SHA-256
`7D01E26FFEA0F6FCF0280D67B05B982584E7724444F12BC106CE4F38075620E5`.
The later corrected execution and the new R2 bundle provide fresh raw MinIO
evidence rather than relying on that historical bundle alone.

F-01 was real: the former `RawExportSourceCleanupReconciliationService`
crossed the ratified capability boundary into lifecycle/item-completion
behavior. The corrected executable flow is:

```text
RECONCILIATION PASS
-> exact cleanup-item/context read
-> authorized provider/object reconciliation
-> durable acknowledgement/resource evidence
-> non-terminal return

PROCESS/INSTANCE LOSS MAY OCCUR

LIFECYCLE PASS
-> fresh cleanup-item read
-> fresh typed resource/event read
-> lifecycle terminalization where required
-> item completion
-> publication finalization
```

Reconciliation does not complete cleanup items, finalize publications, perform
lifecycle delete/revoke/final-abandon terminalization, or use
`RawExportSourceCleanupService` as a completion shortcut. Lifecycle owns the
fresh read, required terminalization, item completion, and publication
convergence. Provider unavailable/unknown remains non-terminal; the winning
object and key remain excluded. No process-memory handoff is authoritative.

F418 remains the existing F418 proof; no F419 was added. It now proves durable
acknowledgement followed by a pending item, a new service-instance boundary,
lifecycle fresh-read and terminalization, completion/finalization, restart,
response-loss replay, and recovery without process-local authority. F412 uses
the effective reconciler and lifecycle database identities: reconciler can
read/reconcile but receives exact PostgreSQL `42501` when invoking lifecycle
completion/finalization; lifecycle can execute its assigned completion and
finalization surfaces. The architecture proof rejects a reconciliation-to-
lifecycle/completion dependency. Owner-fixture execution alone is not treated
as capability evidence.

The active proof census remains exactly F401-F418 (18 methods). The table below
records the accumulated named mutation evidence. Closeout remediation also
strengthened the canonical proof bodies count-neutrally; it does not claim that
every newly added catalog/shape assertion was re-mutated during this correction
turn.

| Proof | Mutation | Required RED observed |
| --- | --- | --- |
| F401 | change the commit evidence domain | absolute commit digest mismatch |
| F402 | remove the `ON CONFLICT` mismatch write-context restore | exact restoration-count assertion expected 3, observed 2 |
| F403 | add an `IAttemptAeadVerificationOperation` dependency | forbidden architecture edge observed |
| F404 | omit `PublishedConsentPolicyId` from the Available shape | PostgreSQL `23514`, exact publication-shape constraint |
| F405 | remove the reservation-revision comparator | expected `StateConflict`, observed `SourceRetentionNotAuthorized` |
| F406 | persist/expose a committed locator during R4 | PostgreSQL `23514`, exact publication-shape constraint |
| F407 | add a forbidden result string surface | forbidden-surface reflection assertion RED |
| F408 | return the mutable current head revision after R5 | replay expected original revision 2, observed 3 |
| F409 | derive `CleanupItemId` from equal cross-kind `ResourceId` values | PostgreSQL `23505` on the cleanup-item primary key |
| F410 | reintroduce completion/quarantine from unavailable or unknown process result | expected durable `Pending` / `ResourceNotTerminal` boundary is violated |
| F411 | use a different landed lifecycle reason | expected `Completed`, observed `ResourceNotTerminal` |
| F412 | change one function owner from deployer to runtime | expected five exact functions, observed four |
| F413 | independently remove each R5 head-guard old-state, new-state, revision, source, attempt, or fence comparator | every sibling mutation ceased returning exact `P0001 / RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN` (no exception, `23514`, or `23503`) |
| F414 | add a `PlaintextLeak` property | forbidden persisted/contract surface observed |
| F415 | remove the occupied-data Down guard | expected Down rejection; no exception was raised |
| F416 | remove cleanup `SchemaVersion` from the snapshot only | pending-model changed from clean to dirty |
| F417 | persist an all-zero staged fingerprint | exact R3 fingerprint preservation assertion RED |
| F418 | continue from process-memory-only state instead of the persisted acknowledgement and lifecycle fresh-read | expected `Completed`, observed `ResourceNotTerminal`; restart/replay cannot converge without the durable handoff |

F411 originally stayed GREEN when only the reason literal changed. That was
reported as a real test/implementation gap, not counted as proof. The exact
reason predicate and event assertion above made the same mutation RED for the
intended reason.

The F-01 correction mutation history is also preserved exactly:

| Mutation | Change | Result |
| --- | --- | --- |
| M-CAP-1 | reconciliation calls cleanup-item completion | architecture proof RED |
| M-CAP-2 | reconciliation calls publication finalization | architecture proof RED |
| M-CAP-3 initial | weak final-abandon mutation | GREEN and not accepted as proof |
| M-CAP-3 strengthened | reconciliation calls final-abandon lifecycle terminalization | architecture proof RED |
| M-CAP-4 | completion/finalization under the effective reconciler identity | exact PostgreSQL `42501` capability denial |
| M-DURABLE-1 | continue from process memory without the persisted handoff | F418 RED: expected `Completed`, observed `ResourceNotTerminal` |

After each temporary mutation, the canonical service, Integration proof and
Architecture proof were restored to SHA-256
`86D205F319F468F83922B913331714F40B3EE2A4CAE436A9B9643DAA9244D6E9`,
`C826FF9FE77C6561350531020580A2D7557B8ACFB3CA5D0DA306C503ADD620E9`,
and `B3B081A9338B20B38D7B3DFD56B54EB16D81FD8458B25D432F0D1BA23FD36740`
respectively. No mutation byte remains in the source snapshot.

The closeout remediation added a focused 14-cell discriminator sweep without
adding proof methods. Each cell was applied alone, rebuilt, executed against a
fresh migrated database, and restored to migration SHA-256
`06BF054E6333F40CFF5BBD6AEC88FE3819FD3C0146F2AE2DD5C87A307E9684CB`:

| Cells | Mutation family | Exact RED reason |
| --- | --- | --- |
| 5 | SQL commit, available, object-item, key-item, and final-cleanup domains | persisted SQL digest differed from the C# oracle at F401/F404/F411 |
| 4 | remove expected reservation, attempt, fence, or object-revision comparator | the corresponding F402 call observed `Committed` instead of `StateConflict` |
| 1 | R5 `head -> attempts` | F408's non-winning-attempt waiter was not blocked by R5 |
| 1 | R5 `objects -> keys` | F408's key waiter was not blocked by R5 |
| 1 | remove winning-object guard | lifecycle was called before F409's `StateConflict` assertion |
| 1 | remove winning-key guard | the winning key became `Revoked`, not `Active` |
| 1 | replace post-lock `clock_timestamp()` with `transaction_timestamp()` | F405 observed `Available`, not `SourceRetentionNotAuthorized`, after expiry while blocked |

The first lock-order mutation remained GREEN against the earlier F408 shape.
It was not counted. F408 was strengthened count-neutrally with an actual
non-winning-attempt lock waiter; canonical F408 passed and both independent
lock-order mutations then RED at the intended blocking relation.

## Shape, catalog and model evidence

- Publication EF shape: 28 properties, 4 keys, 3 indexes, 5 foreign keys, and
  exactly one `ck_raw_export_source_publication_shape` check.
- Cleanup EF shape: 12 properties, 1 key, 2 indexes, 1 foreign key, and exactly
  one `ck_raw_export_source_cleanup_item_shape` check.
- F412 proves five exact function identities, owner/ACL manifests, capability
  roles/memberships, and absence of direct table privileges.
- F413 distinguishes NOT NULL (`23502`) from shape CHECK (`23514`) and exact
  write-guard (`P0001`) failures; every nullable shape member is independently
  exercised.
- F416 compares the design-time model with the snapshot and reports no pending
  model changes. The final snapshot SHA-256 is
  `0A7713E431F90B5E23652FA47CE40030940FE3ADA1E5C77E084C9407038252CD`.
- The E3 snapshot tripwire and the R3 handoff tripwire bind that exact snapshot
  hash. The snapshot delta is additive for the two R4-R6 entities and their
  relationships; no landed entity semantics were changed manually.
- Occupied Down rejects before destructive DDL; clean teardown, Down, reapply,
  and the restored R3 head guard are proved.

## Stable closeout harness and amendment provenance

The replacement runner and all closeout evidence now live in durable,
task-owned directories outside `%TEMP%`:

```text
Runner:
D:\Task\Remote Signing\.test-harness\r4r6\TIP88C1-R4R6-replacement-runner.ps1
SHA-256:
7A9DD32EF361BC6B41796B525ED29E9E050BBE159CB8AC85A2747918C0F3BFCA

SoftHSM package:
D:\Task\Remote Signing\.tooling\softhsm2\SoftHSM2-2.5.0-portable.zip
SHA-256:
85273BCC1A6B90E877F7BB4F7E90221D57103D8F5241D154A79DD730A135B910
```

The verified SoftHSM2 distribution is version 2.5.0 / X64. Its utility SHA is
`EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E`;
its loaded module SHA is
`1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635`.
The earlier use of a purgeable temporary path is retained as historical
evidence only and is not part of the final execution topology.

The effective authority set grew append-only from the ratified 18 paths:

```text
18 original R4-R6 paths
+ 2 readiness-amendment paths
+ 1 canonical PostgreSQL fixture path (Path-21)
+ 1 MinIO fixture path (Path-22)
+ 1 TIP-68 test-harness path (Path-23)
= 23 effective authorized paths
```

The original dispatch remains an 18-path dispatch. The amendments do not
rewrite that history, and no Path-24 was used. Path-23 changed only the three
direct host-startup sibling tests in `Tip68ProdHsmSigningTests.cs` to resolve
services from the test factory without creating an HTTP client. This is
classified as `TEST-HARNESS HARDENING`, not as a production TIP-68/PKCS#11
correction and not as proof of the historical `ObjectDisposedException` root
cause. No production TIP-68 signing bytes changed.

RQ3 formerly conflated deterministic synthetic startup transients with
variable real PostgreSQL startup transients in one total-attempt assertion.
The accepted proof-ownership correction separately requires: injector-owned
synthetic count exactly two; real protocol usability succeeds inside
`ProtocolUsabilityTimeout`; persistent startup transient fails closed; and a
non-startup failure fails immediately. This is test-harness proof ownership,
not a production-readiness semantic change.

## Historical pre-F-01 validation evidence

The evidence in this section predates the F-01 capability-separation
correction. It remains historical execution evidence but is not the source of
truth for corrected-byte closeout and must not be substituted for the
post-correction evidence below.

The stable replacement Integration run completed at:

```text
D:\Task\Remote Signing\.test-evidence\integration-replacement-20260814T222352
```

Its Integration TRX SHA-256 is
`EFAFECBD4BC14CB977BD2ACCC2CBB25D407A5F5F5D588E458F548D65DC53CAEE`.
The exact census was:

```text
Integration: 725 passed / 0 failed / 1 skipped / 726 total
TIP-68 production HSM siblings: 9/9 passed
Tip83E1 readiness tests:        12/12 passed
RQ3:                            passed
F416:                           passed
```

The single skip is the intentional manual TIP-67G golden-vector generator.
Post-run SoftHSM utility/module hashes, version, and architecture matched the
pre-run evidence. Cleanup exited zero; all task-created PostgreSQL/MinIO
containers, networks, volumes, and processes were absent afterward.

Gate D then completed on unchanged bytes at:

```text
D:\Task\Remote Signing\.test-evidence\gate-d-20260814T230215
```

```text
Release build:                         0 warnings / 0 errors
Architecture:                          124 passed / 0 failed
E3/F6/F416 affected Integration:       29 passed / 0 failed
Exact FQN union:                       125
Exact FQN manifest SHA-256:            F85862ECC5D34BE678A8E872E8E996FE1FD16A75900C501A6BC6F567EB6EA1CC
Pending model changes:                 clean
git diff --check:                      pass
Staged paths:                          zero
```

After Gate D, exactly one complete unfiltered Release suite was run on the
same final repository bytes. Evidence is at:

```text
D:\Task\Remote Signing\.test-evidence\full-closeout-20260814T230531
```

The run completed in approximately 29 minutes 48 seconds:

| Project | Passed | Failed | Skipped | Total | TRX SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- |
| Contract | 13 | 0 | 0 | 13 | `9CBC789B199394ACA68584B5A1D15B3A68D846BA81EF8CA3AD17C8AF187F81F0` |
| Architecture | 124 | 0 | 0 | 124 | `CF914C5ECCC1AB029515B56D9AB9D060C5120379C784670612325A1CF8290ECE` |
| Unit | 188 | 0 | 0 | 188 | `29F10E478E31CCE178BAF63D08DA57BA0CA1CD5A34C38EA7ABF8174F8F7C29EF` |
| Integration | 725 | 0 | 1 | 726 | `3CE3174357AC7359C589C3158C042BDE237C41046F061D5D23005956DBD91E16` |
| **Aggregate** | **1050** | **0** | **1** | **1051** | — |

The PostgreSQL server log for the full run is captured from the exact test
container at
`postgres-log-follow-full.stderr.log`, SHA-256
`5FB9375D4FDD921ED1C220E9A161F256BAA6C856999729ED057EACF6AD8185EA`.
It contains 600 `ERROR`, 1,883 `FATAL`, and zero `PANIC` records. All 600
`ERROR` records occur during passing negative-path tests and consist of the
expected SQLSTATE/constraint/ACL/timeout failures exercised by those tests.
Of the `FATAL` records, 1,866 are the known template/reset probe
`database "tagekyc_persistence_tests" does not exist`; the remaining 17 are
administrator-command connection/autovacuum termination during database
reset or teardown. No server crash was observed.

The full-run MinIO evidence contains 43 provider logs / 35,782 bytes and zero
matches for startup-not-ready, signature mismatch, invalid access key, access
denied, internal error, or unexpected server error. Cleanup again exited zero;
the only running Docker container remaining afterward was the unrelated
pre-existing `signflow-postgres` container. Pre-existing stopped SignFlow
containers are outside this task and were left untouched.

The repeated reset-FATAL volume remains a test-harness debt: it is noisy and
costly but did not produce a failing test or a production behavior change in
this closeout. The historical TIP-68 `CreateClient`/
`ObjectDisposedException` observation also remains recorded as a harness
observation; its root cause was not reproduced and is not claimed closed by a
production correction.

## Post-correction accepted execution

The canonical post-correction Gate C ran the exact F401-F418 identities and
passed 18/18. Its TRX SHA-256 is
`3C233EE7D9A412D05DE4C26E702A1D23CE57E6CE1001516E5A167783EDB80DF6`.
The focused F411/F412/F418 TRX SHA-256 is
`5CE8993669B31750A123F896D44810239239944386F909C2C43631C42F88FE03`;
the focused Architecture TRX SHA-256 is
`CBC10F95F58386CF32FA9E25F4A7195CC03594A96792B8EC7B45E01FD181877E`.

Gate D on the corrected bytes passed 124/124 Architecture tests (TRX
`F5CA349C2482F4EAA26DC2597CD6AFAD56FF67EA03CA4363252311B739258621`)
and 174/174 affected-regression Integration tests (TRX
`D65843DFA558D8D901549D7294B269533BA09B04A4EB80414DCB324D6A456D3A`).
The R2 review bundle carries the exact executed Gate-D FQN manifest derived
from that TRX; aggregate counts are not used as a substitute for identity.

The post-F-01 replacement Integration execution passed 725, failed zero,
skipped the one authorized generator, and totalled 726. Its TRX,
stdout/stderr, raw PostgreSQL stdout/stderr, F-02 manifests, raw MinIO evidence,
SoftHSM identity, run metadata, and repository mechanics are included in R2.
Replacement F-02 represented 2,472/2,472 captured events, with `Unexpected=0`
and `Unresolved=0`.

Exactly one final full unfiltered Release suite was executed under the
correction authority on the frozen corrected bytes. It was not a
rerun-until-green:

| Project | Passed | Failed | Skipped | Total | TRX SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- |
| Contract | 13 | 0 | 0 | 13 | `820DD8E1F486B3760F2C7D290CE20477E190ADDDE0CE8CBF302BCE685FA0D0B5` |
| Architecture | 124 | 0 | 0 | 124 | `2322C8F0A28F560E3E385C47B4B0BB242A300EE71C72D554D22C3EEA7C4059C3` |
| Unit | 188 | 0 | 0 | 188 | `82E48F539E1A6E9940C5AFEE3D0076E6CD5DC06C5FCBA91964872934C9E316B1` |
| Integration | 725 | 0 | 1 | 726 | `FE4EAB99719E97FD65ECA5E4F1E3ACDD3DA01619D17427B0E6BF8CDAF5058A38` |
| **Aggregate** | **1050** | **0** | **1** | **1051** | - |

The only skip is
`TagEkyc.IntegrationTests.Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
`FINAL_CLOSEOUT_SUMMARY.json` SHA-256 is
`7D027FEBD86156880B7658847D66E47595EF5F20C24092E9E598B6235D3E2478`;
the evidence checksum manifest SHA-256 is
`7B34BF2E672E48AE1108F814FBF38AD1C3EFE2FFE3754B2884158FF134FC719C`.

### F-02 exact-event closure

F-02 is CLOSED. The final raw PostgreSQL server stderr SHA-256 is
`4EEBF728285BABBAB5A4B1CFD80ABB7C2960F2EF4FD4937E55317EFA008F8650`.
It contains 2,476 adjudicated `ERROR`/`FATAL`/`PANIC` events, represented by
2,476 contiguous and unique JSONL records: 602 `ExpectedNegativeTest`, 1,874
`ExpectedCanonicalReset`, zero `Unexpected`, zero `Unresolved`, and zero
`PANIC`. The JSONL SHA-256 is
`3C36C6861BB1946124F93B2F2FFB96225957E3758E8E70EBB672FD4E3EB1CFE5`;
the immutable classifier SHA-256 is
`B7AD46B2045D46C79D7B10B1BDA5E926051AD82BF97EC9D0B49E76C4DD342D12`.
The independent F-02 validation artifact reports PASS. Evidence retains each
event's timestamp, severity, correlation, discriminator, exact test/reset
owner, source path/line/hash, and resolution; it is not reduced to
message-family counts.

### F-03, MinIO, SoftHSM and residue

F-03 remains CLOSED. The new final-run MinIO evidence contains all 42 raw log
files, 35,154 bytes, and zero prohibited semantic-error hits. Its per-file
manifest SHA-256 is
`AE33A4D5E9995FBEEB21E864D9354B4F82629738FA2C3D44A74498B840927084`;
the raw files themselves are packaged in R2.

SoftHSM before and after the final run are represented separately and are both
SoftHSM2 2.5.0 portable X64. The module SHA-256 is
`1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635`;
the utility SHA-256 is
`EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E`.
Before/after bytes are identical. Task-created container, network, volume,
process and temporary residue is zero.

The three TIP-68 host-startup fail-closed tests intentionally force host
startup directly, while preserving exact startup diagnostics and PIN/secret
redaction assertions. The historical intermittent TIP-68
`CreateClient`/`ObjectDisposedException` root cause was not proven. This does
not claim that a TIP-68 root cause, SoftHSM race, or production PKCS#11 defect
was fixed.

The canonical PostgreSQL reset-FATAL volume remains a harness debt.
`R2-OBS-DEBT-01` also remains open, as does the historical intermittent TIP-68
observation. A green final suite does not silently close any of these records.

## R2-review findings and R3 corrected-byte closeout

The independent R2 closeout review found two remaining issues. F-01R was an
executable replay defect: `ReconcileKeyAsync` read durable
`ProviderOperationState`, but did not use the settled `CleanedUp` and
`AbsenceProven` states as boundaries before invoking provider resolution or
cleanup. F-02P was a bundle self-containment defect: the event records bound
source hashes, but the reviewer could not reproduce those bindings without the
developer working tree.

F-01R was corrected count-neutrally. A fresh reconciliation pass that reads
`CleanedUp` or `AbsenceProven` now returns the existing non-terminal handoff
before any provider resolve or cleanup call. Reconciliation still does not
terminalize lifecycle state or complete the cleanup item. A later lifecycle
service fresh-reads durable state and owns terminal convergence. Unsettled
states continue to perform their required provider operation; no process-local
flag, cache, or completed-operation set became authoritative.

F418 proves the two exact response-loss cases independently:

- after a durable `CleanedUp` acknowledgement, a fresh reconciler instance
  leaves both provider call counters unchanged;
- after durable `AbsenceProven`, a fresh reconciler instance leaves provider
  resolution and cleanup counters unchanged;
- in both cases the cleanup item remains non-terminal until a fresh lifecycle
  pass converges from durable state.

This is not a claim of provider idempotency outside those two tested settled
states. M-RPL-1 and M-RPL-2 independently removed the corresponding boundary;
each made F418 RED because the relevant exact call count increased from 1 to 2.
The mutation TRX SHA-256 values are respectively
`4C9E5FC2153AC6F6B3F120A1837D8238266841872583F6DDA687B37C8A77DF1D`
and
`5056FB16436F57A5ED610ABC152F5293BBF9331258AD65CF1342D6F1F7F9CA1B`.
After byte-identical restoration, F418 passed; its restored TRX SHA-256 is
`07E322CF8E9D27CC5EC3292021669BA4B3686E8816808F75EC4E10EEE8309BF0`.

The corrected-byte validation sequence then passed:

| Gate | Result | Evidence SHA-256 |
| --- | --- | --- |
| F411/F412 focused | 2/2 passed | `F0E66C1F5C101AED272D8145B4EA281488FC554D3C09562324FBF0AC108AF06C` |
| Gate C F401-F418 | 18/18 passed | `B069FB5F28210AA7C79054A04D099C6DA3A56F81989BB1722D8B8B1B6783E82E` |
| Gate D Architecture | 124/124 passed | `87E942151CCF19A61294C710213DC768BA71D3C0B10964EFC22EBD9BCD851830` |
| Gate D affected Integration | 174/174 passed, exact FQN manifest unchanged | `5027486EC6DE11423E9BBB6778345C8D16B7864605702E13E1BB7461BF1E74E8` |
| Gate D E3/F6/pending-model | passed / clean | `AA8A24CDF2673A68303D1A98ACB076DAB77733D34C79FAF0AB897B7449337062` |

Exactly one final unfiltered Release suite was executed on the final corrected
bytes at
`D:\Task\Remote Signing\TagEkyc-review-evidence\full-closeout-r3-20260815T140300Z`.
It was not retried:

| Project | Passed | Failed | Skipped | Total | TRX SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- |
| Contract | 13 | 0 | 0 | 13 | `1E9C42FF3C594FE010C057D485135AEADFE6870722D025C29D5510CB3D44F007` |
| Architecture | 124 | 0 | 0 | 124 | `472AD4145323A66D9BF8B93948CA86311D02381A3A6FAE9750F3ED489AF6D806` |
| Unit | 188 | 0 | 0 | 188 | `8EF54E6F5FCFBB9260619A36658080D0D2533F5023B0BB36D5EE4E769EB19856` |
| Integration | 725 | 0 | 1 | 726 | `D8CB8DFC0872EB712B70A1488B7D10A6787DD04836E00A1EA541975F8D85B238` |
| **Aggregate** | **1050** | **0** | **1** | **1051** | - |

The only skip remained the intentional manual TIP-67G golden-vector generator.
The immutable run record SHA-256 is
`7D33BEC7544061872CB125995B8F89D6DE883201772A5072802D955F7F4DCD54`.

### Final F-02P structural adjudication

The final PostgreSQL stderr SHA-256 is
`A57EF9C6A86CAE9F0BF7E220F91050A2D3FBB41835A8A3CFCEE80DF8607D22C2`.
The non-fail-fast event layer represents all 2,481 raw
`ERROR`/`FATAL`/`PANIC` records exactly once: 602
`ExpectedNegativeTest`, 1,879 `ExpectedCanonicalReset`, zero `Unexpected`,
zero `Unresolved`, and zero `PANIC`. The event JSONL SHA-256 is
`FC18445706C5DA7A8943F42010206338911BD90443F1FE0FFB81765BCFB5864B`.

The reset layer does not infer five zero-event resets from the original
`616 - 611` checkpoint-versus-half-second-cluster observation. Structural
reconstruction found:

```text
checkpoint records:                  616
canonical reset episodes:            612
BoundWithEvents:                      612
BoundZeroEvent:                         0
Unbound:                                0
checkpoint-bounded 3D000 clusters:    612
raw reset 3D000 events:              1863
orphan clusters:                        0
multiply-owned clusters:                0
noncanonical checkpoints:               4
```

The four noncanonical checkpoints are three `dkprod_cs_*` disposable-database
drops inside the CSPRNG proofs and one server-shutdown checkpoint. The legacy
half-second grouping reported 611 clusters with cardinalities 583 x 3,
27 x 4, and 1 x 6. It merged two separate groups of three across a distinct
immediate-force checkpoint. Checkpoint-bounded structural identity therefore
produces 612 clusters with cardinalities 585 x 3 and 27 x 4. Event count is an
observation only; neither cardinality set is reset authority.

The two historical outliers are now pinned by immutable raw-event and preceding-
checkpoint tuples rather than by an ordinal that changes when legacy temporal
groups split. Historical observation 69 binds event 215, raw-event SHA-256
`3FBDCDA0145228A91F9480F49D6D1BF5991AF060A0BE8481C23124CA302D729C`,
and checkpoint raw line 725 to structural `RESET-0069`, `BoundWithEvents`.
Historical observation 178 binds event 692, raw-event SHA-256
`A01656E3DEE957E0DDD2D22C38DA9D1DE6F3F95DE2F654FBD2ADAC125540E188`,
and checkpoint raw line 2444 to structural `RESET-0179`, also
`BoundWithEvents`. The earlier R3 statement that historical observation 178
resolved to `RESET-0178` was incorrect: the legacy six-event observation at
ordinal 113 splits across a real checkpoint, shifting subsequent structural
ordinals by one. The immutable-tuple specification SHA-256 is
`4889131D77D867FF6ACB7E5513DE84407C0A403C17D74CD766430F935030D817`.
Neither binding uses a widened point-distance tolerance or a hard-coded reset
identifier in the classifier.

No genuine canonical zero-event reset exists in this frozen corpus, so no fake
event was created and `bound_reset_zero_event_count` is zero. M-F02-Z1 instead
uses an evidence copy of a zero-event disposable-database checkpoint, removes
the target-database lifecycle binding, and correctly obtains `Unbound`; its
report SHA-256 is
`C3BA5EB157CC0307895D0AD3ED70AA9858C524011F51BA574DC3D4DD88E4C804`.
The reset report and summary SHA-256 values are
`EA110D0752F6894DA0A79A6567BA0DFB4DB6AF54953026997D755F4CB6B6493B`
and
`BAD43F20A8D9D86E22195C35D250E88C0B8DA9DC4460FE69AA000F013EFCD371`.

The required evidence-copy mutation set is complete and does not alter the
frozen raw log, TRX, source snapshot, or canonical reports:

```text
M-F02-R1  break exact reset SQLSTATE/signature          PASS
M-F02-R2  remove event-bearing lifecycle binding       PASS -> Unbound
M-F02-R3  substitute a conflicting real TRX owner      PASS -> Unbound
M-F02-R4  perturb legacy point-distance by +2 seconds  PASS, identity/status unchanged
M-F02-Z1  zero-event checkpoint without lifecycle      PASS -> Unbound
```

The R1-R4 report SHA-256 values are respectively
`1E66C19ED5B6DB5CDA22890A47CF0002D279F5C7887A30AEB4C1198E645EFBD3`,
`442BA3F973D0D3FB37E83C7597304958C286D32FD832BDB9BC414034E5BAC835`,
`E601BAF9ECF72CDAD3015894CC424B919B5FAB5FB5F2F35EF243528E51F54975`,
and
`22933CD2A702BEBF3DCDDE6B75B1BD26BFD330F5386082442125B55660582591`.

The independent validator processes the raw log, final Integration TRX,
complete event/reset/checkpoint reports, immutable outlier tuples, four
required mutation reports, and the final 25-file source snapshot. It derives
544 PostgreSQL test intervals and all 612 reset lifecycles independently,
including exact checkpoint completion, event ownership, owner test FQN,
phase, ordering and status; it does not accept classifier-written owner or
boolean fields as authority. It uses only bundle-local evidence, not the
developer repository, and reported PASS with zero failures. Its output
SHA-256 is
`51D77BCD06DCDADD55B49366E81A41D74762AC08DB9F84177A5C336F3F40B959`.

The R5 evidence-only closure additionally derives every non-3D000 fatal event
from raw markers rather than trusting the classifier manifest. All 16 exact
`FATAL 57P01` records reconcile independently: 14 precede the next canonical
immediate-force checkpoint and bind to that exact structural reset identity,
TRX owner and `EnsureDeletedAsyncForcedConnectionTermination` phase; the final
two occur after the fast-shutdown marker and before the `shutdown immediate`
checkpoint, bind to `FixtureDisposeAsyncDockerComposeDownFastShutdown`, and
have no invented test owner. The validator source SHA-256 is
`3CC6FD22B6B9AE2B598A4411330E17B8D0131A42544BB9B38223F5924A411F0C`.

M-F02-R5-57P01 corrupts event 1205 on an evidence copy by changing its
classification, reset identity and owner simultaneously. The validator exits
2 with exactly three independent failures for classification, phase and owner.
The mutation report SHA-256 is
`42A7E917B48633903004B638066367CE4EC8282085A1F8013BE56A7689E0828B`;
its validator output SHA-256 is
`A340DD86920EA2C0E86B440415ABFDAA31FE94180ACF2BA8477701F55067A259`.
The final 25-path source snapshot is 25/25 present and hash-matching; its
manifest SHA-256 is
`5B7589B01D04629CFA4E73235D991DD18004CE9E39CC1E08210195C09613425E`.
The separately preserved historical R2 snapshot is also 25/25 and has manifest
SHA-256
`DE3FF17BB3A00EC65DAA50DCE2B1AFC3E14828197297DBE64C07939D2B4D3FB3`.
Neither evidence snapshot expands the 23-path repository authority manifest.

The final run captured 43 raw MinIO logs / 36,013 bytes and zero prohibited
semantic-error hits. SoftHSM before/after remained version 2.5.0 portable X64,
with module SHA-256
`1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635`
and utility SHA-256
`EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E`.
Task-created container, network, volume, process, and SoftHSM residue was zero.

## Canonical file hashes before this as-built

| Path | SHA-256 |
| --- | --- |
| `RawExportSourcePublicationRow.cs` | `1331A0ED64ECE431B73C607A56833CB0ABD47E0909ED3898763FD8EBEB1EAC7F` |
| `RawExportSourceCleanupItemRow.cs` | `9BB8CAF532DDA50E15AB8D8B0904F8B526EB723EEA319E5B4613195883B7EFF3` |
| `RawExportSourceHeadRow.cs` (unchanged landed reference) | `66D7EB2B5672211AA83AEF8B4472AAAF51AA687226E7934C321D9C6DEFFFB154` |
| `RawExportSourcePublicationConfig.cs` | `A743C6F68A71E824CB2BD7C64992467790C37258A4DE38791DCFAE7054689EDA` |
| `RawExportSourceCleanupItemConfig.cs` | `A170E5938DC553D7A3E7804955C575CDDD4F4681983517275A447FDC29A5A4A0` |
| `20260812120000_Tip88C1B2R4R6SourceFinalization.cs` | `06BF054E6333F40CFF5BBD6AEC88FE3819FD3C0146F2AE2DD5C87A307E9684CB` |
| `20260812120000_Tip88C1B2R4R6SourceFinalization.Designer.cs` | `A58C96ECC8674AA7EF920A62E8BA6E58540273F577DA61D87EC339D1247B047F` |
| `TagEkycDbContextModelSnapshot.cs` | `0A7713E431F90B5E23652FA47CE40030940FE3ADA1E5C77E084C9407038252CD` |
| `TagEkycDbContext.cs` | `981E0E4062F55E1CCD012DC55ACBB85E5BFFA19FB3862C6213612D8936939BD3` |
| `RawExportSourceFinalizationContracts.cs` | `F723CC51D9FECB2E0CA31891DCCE2028715195F2DDF7FA719240252B78F665B6` |
| `RawExportSourceFinalizationEvidenceCodec.cs` | `F00E6E6C80E6ADB6E7AD2B1E7BDE962EF03006052E25A101958E2DFEFE5DC92F` |
| `RawExportSourceFinalizationRepository.cs` | `B8E0D8B01CA26E320368291D868D135BDB78434E9769B493EB3689AEB8571BC0` |
| `RawExportSourceFinalizationServices.cs` | `52E12C058CB4294359E2DA7F2836A316E7587D5D07F51C21E8196F508AF15B0B` |
| `Tip88C1B2SourceFinalizationArchTests.cs` | `B3B081A9338B20B38D7B3DFD56B54EB16D81FD8458B25D432F0D1BA23FD36740` |
| `Tip88B1E3ResolverReadBoundaryTests.cs` | `233A01B8C703806B9F69E304AF1DB24D12B7D2F0571D0DBF31D1770218EB1FE7` |
| `Tip88C1B2R3VerifiedCiphertextStagingTests.cs` | `96BDEEB08D15D0EDDE0AFBD324ED23B79CFC6C051A28F27BFCF28AA9F062B285` |
| `Tip88C1B2SourceFinalizationTests.cs` | `F4B8F75A42CE5FEDCF6538C16DCD49E20152F591DA8A74F596F1EC9C055D5700` |

Amendment-path hashes before this as-built:

| Path | SHA-256 |
| --- | --- |
| `ReadinessEndpoint.cs` | `13509C3CC6F5DFF399FF10E67A2131CD378BF84CBA92C3AFB49FFBCF0947FC49` |
| `Tip83E1ReadinessEndpointTests.cs` | `52C25ED5B478EA13FC0409CD3A5A6897E57C0D0199A938FC0E8F5E16C1BD71C8` |
| `PostgresPersistenceFixture.cs` (Path-21) | `4E709A530FC77377E4E5E1265AA9A234FC6C471B0BCE36AF1900B101BD967B6F` |
| `DurableObjectMinioFixture.cs` (Path-22) | `31161ADE3BB1F0CF6D223E89369E1A93AA7F9537E4E77C4C342CD0E6CD9035F5` |
| `Tip68ProdHsmSigningTests.cs` (Path-23) | `C87EF82743D496BABEE50D223E0932F067947394E2E0DA6F4B84B2B3D30A506E` |

## Non-claims and remaining boundary

This closes only the synthetic/fixture-backed R4-R6 source-finalization proof
build's corrected executable and validation evidence. Independent semantic
review of `RawExportSourceCleanupReconciliationService` and the lifecycle
handoff remains OPEN and must be completed before controlled-commit review.
This document records readiness to be reviewed, not that review has occurred.

The slice does not make a real Raw BIO capture adapter, resolver/assembly,
package construction, delivery, production configuration, or deployment
ready. It does not authorize staging, commit, push, merge, PR, activation, or
provider operation. Those remain separate Homeowner-gated work.

Current disposition:

```text
PASS - R4-R6 SOURCE FINALIZATION READY FOR INDEPENDENT CLOSEOUT REVIEW
```
