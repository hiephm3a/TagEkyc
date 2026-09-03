# TIP-88B4 Permit-to-Job Consumption Foundation — Closeout

**Status:** CLOSED — IMPLEMENTATION SETTLED — DOCS-ONLY CLOSEOUT COMMIT PENDING

**Closeout date:** 2026-07-27

**Planning contract:** `tip_88b4_planning_brief.md` v0.22

**Implementation build brief:** `tip_88b4_implementation_build_brief.md` v0.19

**Settled baseline:** `92d64e6f890fa4b38069a7dfe085653bc2ac6eaa`

**Closeout commit:** PENDING — separate Homeowner authorization required

## Commit chain

| Commit | Purpose | Result |
| --- | --- | --- |
| `0ae9caf` | ratify contract | Planning v0.20 and Build Brief v0.17 |
| `adaf1c7` | Amendment D | Planning v0.21 and Build Brief v0.18; removed the B4-specific `Enlist=false` plus connection-string capture/mutate/restore contract |
| `6eb1fbb` | implementation | 22 files, +11216/-6 |
| `568689c` | E3 tripwire refresh | one separately authorized model-snapshot hash constant |
| `d09929d` | closeout-review fixes | five files, +156/-14 |
| `92d64e6` | Amendment D1/D2 | three files, +207/-110; Planning v0.22 and Build Brief v0.19 |

## Scope delivered

TIP-88B4 delivers the metadata-only permit-to-job consumption foundation:

- five append-only/CAS tables;
- eight runtime `SECURITY DEFINER` entry functions;
- six internal guard functions;
- ten triggers;
- one six-method repository port;
- the canonical job fingerprint codec;
- bounded lease options;
- the B4 readiness validator and DI/API readiness wiring; and
- migration, catalog, ACL, transaction, race, mutation, and lifecycle evidence.

Permit consumption is derived from the existence of the immutable job identity
and its `UNIQUE(PermitId)` constraint. The landed B3 permit row remains immutable;
there is no mutable consumed flag and no separate consumption ledger.

This slice stores metadata only. It does not read or persist Raw BIO bytes, build
a package or manifest, encrypt, deliver, issue a download handle, or record
external delivery. Those capabilities remain owned by TIP-88C1 through TIP-88C4.

## Security posture

- Runtime has zero table-level and column-level privilege on all five B4 tables.
  It holds `EXECUTE` on exactly eight entry functions. The deployer owns the B4
  tables and functions.
- The migration ACL assertions exceed the landed E3 norm: they verify column
  `attacl`, table `relacl`, exact grantee/grantor/grantability, and run `GRANT`
  under `SET LOCAL ROLE` so the grantor is deterministic.
- The schema is additive. B4 foreign keys reference landed decision and permit
  rows without altering those rows.
- B4 never assigns a connection string and never mutates persistence, provider,
  service, or pool configuration. Ambient, EF-current, provider-current, and
  currently-open connection rejection is the admission mechanism, with the final
  check adjacent to `BeginTransactionAsync`.
- The B4 state machine can reach only `Claimed`, `Assembling`, and the three
  terminal states. Future C1-C3 state names exist only in CHECK vocabularies.
  There is no generic transition entry that a later slice can widen.
- Authoritative time comes from the database clock. The actor is the atomic
  authenticated triple at the application boundary; callers cannot supply the
  frozen policy, session, subject, recipient, mode, classes, deadlines, or state.
- `DEBT-E3-A` remains unchanged: the actor GUC provides provenance and
  anti-mixing, not authentication against a compromised trusted backend or
  database-owner credential.
- The binding deployment topology remains one hospital per database.

## Outcome vs intent

| Intended outcome | Actual result | Status | Notes / carry-forward |
| --- | --- | --- | --- |
| One logical job per immutable permit | `UNIQUE(PermitId)` on job identity with atomic `NewJob`/`ExistingMatch`/`FingerprintConflict` handling | DELIVERED | Permit state remains derived; no mutable consumed flag |
| Crash/retry-safe job orchestration | Revision, monotonic fencing token, immutable attempts, released/reclaimed lease semantics, and terminal states landed | DELIVERED | No exactly-once external delivery claim |
| Frozen authorization identity | Principal, client, API-key provenance, decision, permit, session, subject, policy/version, purpose, recipient, mode, classes, and deadlines are frozen | DELIVERED | Raw artifacts and package identity remain future slices |
| Runtime capability boundary | Exact eight-function runtime surface with zero table/column privilege and deployer ownership | DELIVERED | Earlier E3 rogue-function readiness gap remains carried debt |
| Same-transaction head/evidence integrity | Head mutation and append-only transition are guarded and atomic | DELIVERED | Positional SQL inserts remain migration debt |
| No Raw BIO data plane in B4 | No raw-byte, package, encryption, delivery, or HTTP surface landed | DELIVERED | TIP-88C1-C4 remain required |

## Final validation

An independent reviewer reran the final validation against the settled
implementation:

- contract tests: 13/13 passed;
- architecture tests: 69/69 passed;
- unit tests: 188/188 passed;
- integration tests: 524 passed and 1 intentional manual-generator skip;
- total: 794 passed, 0 failed, 1 intentional skip;
- full build: clean;
- EF pending-model gate: clean; and
- `git diff --check`: clean.

No tests were rerun for this docs-only closeout preparation. The independently
rerun accepted validation above is reused unchanged.

## Mutation adequacy

### Connection lifecycle

The canonical cleanup mutation is mutation 7b, not deletion of the conditional
`CloseConnectionAsync` call by itself. EF closes a connection inside
commit/rollback when EF opened it, so deleting that conditional statement targets
redundant happy-path code and cannot discriminate a good lifecycle test.

The meaningful mutation makes B4 own the open connection and removes the entire
`finally` cleanup path. It makes the lifecycle and same-scope-reopen gates red
with `Expected: Closed; Actual: Open`. A mutation aimed only at redundant code is
not evidence that a test is weak or strong.

### Closeout-review fixes

- Restoring the paired-null preflight rejects the valid released-Assembling
  `(attempt, NULL)` terminalize shape and makes
  `M7_public_terminalize_accepts_released_assembling_head` red with
  `RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED`.
- Removing the `PrincipalId` and `ClientApplicationId` ownership predicates from
  the existing-job lookup makes
  `M6_foreign_direct_claim_cannot_use_existing_job_as_existence_oracle` red.
- Both production sources were restored byte-identically after their scratch
  mutations.

### D1 evidence accounting

- The admission surface has approximately twelve independently discriminating
  cells: six ambient-transaction and six open-connection behaviors.
- The preflight-precedence test executes 24 cells, but they collapse to six
  equivalence classes, one per repository method, because invalid actor
  validation exits before `ExecuteAsync` and topology cannot influence the
  unmutated result.
- Provider-current rejection shares the open-connection predicate.
- EF-current with a closed connection is not constructible on a supported EF
  path.
- Source/static and non-constructible cells are not claimed as behavioral
  mutation-red evidence.

## Defects found and fixed after first landing

The first implementation landing was not accepted as defect-free. Final
adversarial review found and the follow-up commit fixed:

1. A valid terminalize shape was rejected before the database. Paired
   nullability incorrectly rejected `(attempt, NULL)`, which planning section 8
   ratifies for a released `Assembling` head. This was production-reachable after
   a `Recorded` attempt failure and prevented public cancellation,
   non-retryable failure, or authority terminalization of that job.
2. The M9 crash test contained a tautological assertion: a local literal was
   asserted against itself.
3. Three typed-failure lifecycle cells asserted only the exception type, which
   could not distinguish the intended database failure from cleanup-path failure.
4. The strongest failure injection, `pg_terminate_backend`, checked connection
   state but not absence of a partial head/ledger write.
5. Existing-job lookup filtered only by `PermitId`, creating a one-bit existence
   oracle for a foreign permit identifier.
6. Architecture method extraction ran to end-of-file for the last-declared
   method, allowing the open-call prohibition to pass by accident.

## Decision / branch disposition

| Decision / option | Final disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Mutate B3 permit with a consumed flag | REJECTED | B3 permit is immutable; job identity existence derives binding | None |
| Separate consumption ledger | DEFERRED | B4 identity/head/evidence surfaces are sufficient for this slice | Re-evaluate only if a later data-plane requirement needs distinct evidence |
| B4 connection-string capture/mutate/restore | REJECTED by Amendment D | Npgsql credential redaction made the contract unsafe and unimplementable | Preserve no-assignment/no-configuration-mutation posture |
| Early `ExistingMatch` return | ACCEPTED | First claim was validated and persisted; `ExportMode` is fingerprint-bound | New-job mode/closure validation remains mandatory |
| Exactly-once external delivery | NOT CLAIMED | No network-delivery system can infer client receipt after connection loss | Owned by later delivery semantics |
| Raw resolver/package/encryption/delivery | DEFERRED | Outside metadata-only B4 boundary | TIP-88C1 through TIP-88C4 |

## Remaining B4 debt

| Debt | Final state | Resolved? | Carry-forward |
| --- | --- | --- | --- |
| Positional `INSERT ... VALUES` without column lists | Recorded | No | Add explicit column lists before a future schema extension can silently mis-bind |
| `AcquireCoreAsync` re-terminalizes inside its catch with a possibly stale revision/fence | Fail-closed but fragile | No | Revisit before orchestration is widened |
| `ReadCoreAsync` trusts SQL ordering without ordinal-contiguity validation | Recorded | No | Align with sibling parsers in a later hardening slice |
| `TryAddSingleton` fail-closed behavior depends on registration order | Recorded | No | Pin or redesign ordering before composition changes |
| No stale-worker `TerminalizeAsync` test | Recorded | No | Add parity with stale renew and record-failure coverage |

## Carried debt from earlier slices

- E3 readiness LEFT JOINs from its expected function list and therefore catches
  missing entries but cannot detect a rogue out-of-band function with runtime
  `EXECUTE`. This is the highest-value carried item; B4's function readiness
  universe is exhaustive, but E3 remains unchanged.
- E3 F4 lock-mutation evidence is recorded generically rather than per mutation.
- EOL normalization invalidated E3's migration whole-file hash comparison.
- The E3 whole-file ModelSnapshot hash expires whenever a later legitimate slice
  adds an entity; it expired immediately on B4 and required the separately
  authorized `568689c` refresh. A structural, slice-aware assertion would be more
  durable.

## Process lessons

- Three STOP/RRIs occurred and all were correct: an unimplementable
  credential-restoration contract, a vacuous mutation, and documentation
  divergence. None was avoidance.
- Mutation design must target an executable responsibility. Removing redundant
  happy-path code proves nothing about whether the test bites.
- A single wrong number survived in the main matrix, a parallel matrix, and an
  amendment record across two documents. A patch is incomplete until sibling
  anchors are swept and classified.
- Green tests did not excuse reachable contract mismatches. The released-head
  terminalize defect required a public-entry positive test and observed-red
  mutation.

## Remaining non-goals

- No Raw BIO bytes, artifact resolver, package, manifest, encryption, recipient
  key registry, delivery surface, receipt, or download API.
- No exactly-once external delivery claim.
- No multi-hospital shared-database tenant model.
- No claim that the actor GUC authenticates against full backend compromise.
- No SignFlow-specific signing-session concept in TagEkyc core.
- No production activation, deployment, push, or merge is authorized by this
  closeout document.

