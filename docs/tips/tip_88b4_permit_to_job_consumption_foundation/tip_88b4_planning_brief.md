# TIP-88B4 Permit-to-Job Consumption Foundation — Planning Brief

**Version:** 0.21

**Status:** RATIFIED AS AMENDED — AMENDMENT D SYNCHRONIZED — IMPLEMENTATION STOPPED

**Date:** 2026-07-27

**Grounded baseline:** `bf90d5453f2cf8fb45009ccc2dcdb42c335a4711`

**Parent contract:** `docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md`

## 0. Purpose and boundary

TIP-88B4 creates the durable orchestration foundation between the landed B3
authorization permit and the future Raw BIO data plane. It proves that one
immutable permit can bind to exactly one logical export job and that the same job
survives retry, process failure, lease expiry, and fenced reclaim.

B4 remains inert of Raw BIO bytes. It does not establish a raw source, read an
artifact, construct a real manifest or package, enroll a recipient key, encrypt,
deliver, expose HTTP, or claim physical delivery. Those capabilities remain in
TIP-88C1 through TIP-88C4.

## 1. Grounded landed contracts

### 1.1 B3 decision and permit

The landed B3 graph is immutable and append-only:

- `raw_export_authorization_decisions` carries the authenticated
  `PrincipalId`, `ClientApplicationId`, credential-evidence `ApiKeyId`, exact
  policy/version, resolved session and subject, purpose, recipient, decision
  expiry, and authorization evidence;
- `raw_export_authorization_permits` exists only for an Authorized decision,
  binds one decision through a unique FK, and carries `PermitId`,
  `ResolvedVerificationSessionId`, `SubjectRef`, policy/version, purpose,
  `RecipientClientApplicationId`, `DecisionExpiresAtUtc`, and schema version;
- `raw_export_permit_classes` is the non-empty ordered exact class set;
- runtime has read access to the immutable seven-table B3 decision graph but no
  direct mutation privilege; B3 writes remain behind landed
  `SECURITY DEFINER` functions.

B4 must not add a consumed flag to the B3 permit, update it, delete it, or create
an unbind operation. Permit state remains derived:

```text
no ExportJobIdentity for PermitId  = Unclaimed
one ExportJobIdentity for PermitId = BoundToJob
```

### 1.2 Mode projection gap

The ratified job contract freezes `RawExportMode`, but the landed B3 permit does
not contain it. The E3 authorization-policy projection deliberately omits
`Mode`, because B3 authorization did not consume it. B4 must not change the
landed E3 function or repurpose the general policy repository.

B4 therefore owns one narrow actor-bound projection that reads:

- the exact B3 permit, Authorized decision, and ordered permit classes;
- `Mode` from the exact immutable `raw_export_policy_versions` row;
- the exact closure needed to prove the policy version remains
  `CatalogApproved`;
- the optional committed B4 `JobId` and bind fingerprint already associated
  with that `PermitId`.

It returns typed/scalar rows, not JSON, takes no raw-source reference, and does
not implement an authorization verdict in SQL. It does not return or freeze a
transaction-start clock; section 6.5 owns the physical-time rule.

### 1.3 E3 runtime topology

The production application connects through a dedicated LOGIN principal that
inherits only the `tagekyc_runtime` capability. The connection begins with
`session_user == current_user`; production does not enter with `SET ROLE`.
`tagekyc_runtime` has zero direct privileges on the fourteen E3-protected
backing tables and reaches authorization inputs only through reviewed
`SECURITY DEFINER` projections.

B4 reuses:

- the same NOLOGIN `tagekyc_raw_export_deployer` function owner;
- the same NOLOGIN `tagekyc_runtime` capability role;
- `tagekyc.raw_export_current_actor()` as provenance/anti-mixing, not
  authentication;
- the existing B3 session-lock function;
- the existing B1 eligibility and B2 subject-consent resolver seams.

B4 does not close DEBT-E3-A, DEBT-E3-B, or DEBT-TENANCY and must not claim that
the actor GUC authenticates a fully compromised backend.

## 2. Intent

Provide a repository-only, metadata-only job aggregate with:

- exactly one logical job per permit;
- immutable frozen job identity and exact class children;
- append-only attempt and transition evidence;
- one mutable operational head updated only by CAS;
- monotonic, non-reusable fencing;
- crash-safe lease acquisition, renewal, release, expiry, and reclaim;
- fresh authorization revalidation at bind and before each new/reclaimed work
  attempt;
- exact idempotent bind behavior;
- no DB transaction or lock held across future raw, crypto, object-store, or
  network I/O.

## 3. Application surface

### 3.1 Atomic actor and commands

The public application port introduced by B4 is repository-only:

```text
IRawExportJobRepository
```

Its commands carry one atomic `AuthenticatedRawExportActor` copied from the
landed authenticated context. No overload accepts independent principal, client,
or API-key GUIDs.

The bind command contains exactly:

```text
Actor
PermitId
IdempotencyKey
```

The caller cannot submit policy, session, subject, recipient, mode, classes,
decision identity, permit expiry, or job expiry. B4 resolves and freezes those
from landed authoritative rows.

`IdempotencyKey` uses the landed raw-export grammar: 1–256 ASCII characters from
`[A-Za-z0-9._:-]`, case-sensitive, non-blank, with no whitespace. The C#
repository preflight and the runtime-granted claim function independently enforce
that same grammar. Empty GUIDs, invalid keys, or undefined enum values fail before
a transaction on the repository path and leave no rows; a direct claim call with
a valid actor fails inside the function before any B4 lookup or mutation.

Lease and transition commands use an internal execution context constructed from
the frozen job identity and a server-generated worker identity. `LeaseOwnerId`,
`AttemptId`, `JobId`, expected revision, and expected fencing token are never
accepted from a public HTTP request in B4.

Renew and record-attempt-failure are worker-owned commands and require a live
lease. Authority/deadline/cancellation terminalization is a separate
repository-orchestration command under a held operational-head lock; it is not
exposed through the worker-owned execution path.

### 3.2 Return surfaces

The bind operation returns one typed outcome:

```text
NewJob(JobId)
ExistingMatch(JobId)
Terminal(JobId, RawExportJobTerminalizeResult)
FingerprintConflict
```

`NewJob` and `ExistingMatch` carry a non-empty `JobId` and no terminal result.
`Terminal` is legal only for the safely identified committed-job `GraphInvalid`
path in section 5.1; it carries the same non-empty `JobId` and the exact non-null
terminal result. `FingerprintConflict` returns no existing `JobId` or job detail
and maps to the stable idempotency exception. Normal concurrency is an outcome,
not an untyped database exception.

Job reads are actor-bound and return the immutable identity, exact ordered
classes, operational head, current attempt identity, and bounded transition
summary needed by orchestration. B4 has no public status DTO and no
not-found/not-owned HTTP mapping.

### 3.3 Owned transaction isolation

Every `IRawExportJobRepository` method owns one fresh explicit transaction opened
with `BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)`:
`BindAsync`, `ReadAsync`, `AcquireOrReclaimLeaseAsync`, `RenewLeaseAsync`,
`RecordAttemptFailureAsync`, and `TerminalizeAsync`.
They do not inherit a database, role, connection, ambient-transaction, provider,
or caller default. No B4 public or repository command accepts a caller-owned
transaction.

All in-memory command-shape preflight runs first. An invalid GUID, idempotency
key, enum, worker identity, lease value, or other syntactic command field returns
its existing validation outcome before transaction-ownership/isolation checks.
Only a preflight-valid command reaches the checks below.

For a preflight-valid command, every entry requires all of the following before
any B4 lookup, lock, mutation, or transition:

- `Transaction.Current == null`;
- `db.Database.CurrentTransaction == null`;
- the scoped connection is closed at method entry; and
- the underlying provider has no active transaction.

Violation maps to `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; the repository
never silently joins caller-owned state. The ambient/current-transaction check is
repeated immediately before invoking `OpenAsync` or
`BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)`. No
application-level `await`, callback, resolver invocation, or database command may
occur between that final check and the invocation that opens the connection or
begins the explicit transaction.

A scoped connection that was previously opened and is now closed at method entry
is explicitly supported and is the common production topology: B1/B2/B3
authority work routinely opens and closes the same scoped connection first. A
connection that is currently open at entry is rejected.

Every method uses the injected scoped `TagEkycDbContext`, its exact
`DbConnection`, and its fresh explicit Read Committed transaction throughout.
Where the ordered path requires B1, policy projection, B2, and B4, all those
operations share those same instances. B4 creates no second DbContext,
connection, data source, connection factory, service scope, or nested
transaction.

B4 never assigns the scoped connection string and never changes configured
persistence options, provider transaction-participation settings, service
registration, or global pool configuration. This no-assignment guarantee is
strictly stronger than the superseded byte-restoration design: there is no
mutation window and no restoration path that can fail. Byte equality of the live
provider connection-string property is therefore not an invariant; Npgsql may
redact credential material after an open.

On every exit, including typed failure, provider exception, timeout, and
cancellation, cleanup disposes the transaction, closes only the connection B4
opened, and requires `db.Database.CurrentTransaction == null`, no underlying
provider transaction, and `ConnectionState.Closed`. Cleanup is non-cancellable.
The explicit transaction overload remains a ratified source contract even though
the current Npgsql provider default is also Read Committed.

This isolation requirement is load-bearing. B4 relies on a new committed snapshot
for each statement after a lock wait, both for visibility-safe winner re-read and
for current session/B1/policy/B2 authority resolution. Physical database time and
MVCC visibility are independent requirements.

## 4. Persistence model

B4 adds five tables in schema `tagekyc`. The first two form the immutable
identity aggregate, the next two are append-only evidence, and the last is the
only mutable surface.

### 4.1 `raw_export_job_identities`

Immutable columns:

```text
JobId uuid primary key
PermitId uuid not null unique
AuthorizationDecisionId uuid not null
PrincipalId uuid not null
ClientApplicationId uuid not null
CreatedByApiKeyId uuid not null
VerificationSessionId uuid not null
SubjectRef text not null
PolicyId uuid not null
PolicyVersion integer not null
PurposeCode text not null
RecipientClientApplicationId uuid not null
ExportMode text not null
PermitExpiresAt timestamptz not null
JobExpiresAt timestamptz not null
IdempotencyKey varchar(256) not null
IdempotencyFingerprintHash bytea not null
SchemaVersion integer not null
CreatedAt timestamptz not null
```

Required integrity:

- FK `PermitId` to the landed permit;
- FK `AuthorizationDecisionId` to the landed decision;
- FK `VerificationSessionId` to the landed session;
- composite equality with the permit/decision is asserted inside the sole bind
  function;
- `PolicyVersion >= 1`;
- `SchemaVersion = 1`;
- exact CHECK `CK_raw_export_job_identities_IdempotencyKey`:
  `length("IdempotencyKey") BETWEEN 1 AND 256 AND
  "IdempotencyKey" COLLATE "C" ~ '^[A-Za-z0-9._:-]+$'`;
- fingerprint length exactly 32 bytes;
- closed `ExportMode` set:
  `ExternalExportOnlyNoRetain`, `EncryptedExportPacket`,
  `EncryptedRawVaultRetained`;
- `PermitExpiresAt ==` landed `DecisionExpiresAtUtc`;
- `JobExpiresAt == PermitExpiresAt` in B4 schema version 1;
- all timestamps are database-derived and stored in UTC instants.

`CreatedByApiKeyId` is immutable provenance from the first successful bind. It
is not a later consumption predicate and is excluded from the idempotency
fingerprint, so credential rotation does not break a valid retry.

### 4.2 `raw_export_job_classes`

Immutable child columns:

```text
JobId uuid not null
RawClass text not null
Ordinal integer not null
```

The PK is `(JobId, RawClass)`, with a unique `(JobId, Ordinal)`, non-negative
contiguous ordinals, a closed raw-class CHECK, a same-transaction child guard,
and a deferred non-empty-classes constraint. The sole bind function verifies
that this set is byte-semantically identical in membership and order to the B3
permit-class set.

The exact deferred constraint-trigger name is
`tagekyc.tr_b4_job_identity_has_classes`; its internal trigger function is
`tagekyc.enforce_raw_export_job_identity_has_classes()`. That completeness
trigger function remains `SECURITY INVOKER`; making it `SECURITY DEFINER` to
hide a missing mode transition is forbidden. The
`raw_export_claim_or_read_job` body owns the named constraint mode:

```sql
SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;

-- insert identity
-- insert ordered class children
-- insert head and initial transition

SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes IMMEDIATE;
SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
```

It normalizes the exact constraint to `DEFERRED` at entry, forces
`IMMEDIATE` after the complete aggregate insert but before returning
`NewJob`, and restores `DEFERRED` on every successful return. It never uses
`SET CONSTRAINTS ALL`. This makes the deferred check run while the
`SECURITY DEFINER` function still has deployer authority and prevents a
runtime-only caller from reaching a commit-time `42501`. A caller-entered
`IMMEDIATE` mode and two valid binds in one outer transaction are supported.

### 4.3 `raw_export_job_attempts`

Each lease acquisition or reclaim inserts one immutable attempt:

```text
AttemptId uuid primary key
JobId uuid not null
AttemptOrdinal integer not null
Phase text not null
LeaseOwnerId uuid not null
FencingToken bigint not null
AcquiredAt timestamptz not null
InitialLeaseExpiresAt timestamptz not null
```

The B4 schema-v1 row-local domain CHECKs are exact:

```text
CK_raw_export_job_attempts_Phase:
  Phase = 'Assembling'
CK_raw_export_job_attempts_AttemptOrdinal:
  AttemptOrdinal >= 0
CK_raw_export_job_attempts_FencingToken:
  FencingToken >= 1
CK_raw_export_job_attempts_LeaseTime:
  InitialLeaseExpiresAt > AcquiredAt
```

`AttemptId` is globally unique. `(JobId, AttemptOrdinal)` is unique and ordinals
are contiguous. A reclaim always creates a new `AttemptId` and increments both
attempt ordinal and fencing token. Attempt rows are never updated to record
completion; later facts are transition rows.

B4 permits production attempts only for phase `Assembling`. It does not permit a
production attempt for sealing, protecting, delivery, or reconciliation. Later
slices may extend the exact phase allowlist only through a reviewed migration;
`Phase` is never unrestricted text. Cross-row ordinal contiguity and monotonic
fencing remain command-function, head-lock, and unique-constraint invariants, not
claims made by the row-local CHECKs above.

### 4.4 `raw_export_job_transitions`

Every authoritative head mutation has exactly one append-only transition row in
the same transaction:

```text
TransitionId uuid primary key
JobId uuid not null
ResultingRevision bigint not null
EventType text not null
FromState text null
ToState text not null
AttemptId uuid null
FencingToken bigint not null
ResultingLeaseOwnerId uuid null
ResultingLeaseExpiresAt timestamptz null
FailureCode text null
OccurredAt timestamptz not null
```

`(JobId, ResultingRevision)` is unique and contiguous. The initial `JobBound`
transition uses revision 0, `FromState = NULL`, `ToState = Claimed`,
`AttemptId = NULL`, and fencing token 0. Every later transition carries the
resulting head revision and current fencing token.

`ResultingLeaseOwnerId` and `ResultingLeaseExpiresAt` equal the resulting head
values in the same transaction. Both are non-null for `LeaseAcquired`,
`LeaseRenewed`, `LeaseAcquiredAfterRetryableFailure`, and `LeaseReclaimed`, and
both are null for release and terminal transitions. The initial/renewed expiry,
subsequent reclaim time, and later mutable head must remain reconstructible from
immutable evidence after any number of mutations.

`FailureCode` is a closed, sanitized operational token. It may never contain raw
payload, exception text, provider output, subject data, or stack traces.

The transition table has a row-local event-shape CHECK equivalent to:

| EventType | From → To | Attempt/fence | Resulting lease | FailureCode |
| --- | --- | --- | --- | --- |
| `JobBound` | NULL → `Claimed` | attempt NULL, fence 0 | owner/expiry NULL | NULL |
| `LeaseAcquired` | `Claimed` → `Assembling` | attempt non-null, fence ≥1 | owner/expiry non-null | NULL |
| `LeaseRenewed` | `Assembling` → `Assembling` | attempt non-null, fence ≥1 | owner/expiry non-null | NULL |
| `AttemptFailedRetryable` | `Assembling` → `Assembling` | attempt non-null, fence ≥1 | owner/expiry NULL | `ATTEMPT_EXECUTION_FAILED_RETRYABLE` |
| `LeaseAcquiredAfterRetryableFailure` | `Assembling` → `Assembling` | attempt non-null, fence ≥1 | owner/expiry non-null | NULL |
| `LeaseReclaimed` | `Assembling` → `Assembling` | attempt non-null, fence ≥1 | owner/expiry non-null | NULL |
| `JobTerminalFailed` | `Claimed` or `Assembling` → `TerminalFailed` | attempt nullable, fence non-negative | owner/expiry NULL | one terminal-failure reason |
| `JobCancelled` | `Claimed` or `Assembling` → `Cancelled` | attempt nullable, fence non-negative | owner/expiry NULL | `REQUEST_CANCELLED` |
| `JobExpired` | `Claimed` or `Assembling` → `Expired` | attempt nullable, fence non-negative | owner/expiry NULL | `PERMIT_OR_JOB_EXPIRED` |

No other event/state/attempt/lease/failure shape satisfies the B4 CHECK.

This row-local CHECK enforces only the closed EventType,
`FromState`/`ToState` pairing, attempt/fence nullability and range, resulting
lease pair, and failure-code allowlist/pairing. It does not query another row.
Composite FKs prove that a non-null
`(JobId, AttemptId, FencingToken)` identifies one immutable attempt. While the
operational head is locked, the command function plus command-specific
guard/trigger enforces current-attempt equality, a fresh `AttemptId`, strictly
higher non-reused fence, revision contiguity, and equality between transition
result values and the resulting head.

### 4.5 `raw_export_job_operational_heads`

The only mutable table contains:

```text
JobId uuid primary key
CurrentState text not null
Revision bigint not null
CurrentAttemptId uuid null
LeaseOwnerId uuid null
LeaseExpiresAt timestamptz null
FencingToken bigint not null
UpdatedAt timestamptz not null
```

Initial values are:

```text
CurrentState = Claimed
Revision = 0
CurrentAttemptId = NULL
LeaseOwnerId = NULL
LeaseExpiresAt = NULL
FencingToken = 0
```

`LeaseOwnerId` and `LeaseExpiresAt` are both null or both non-null. An active
lease requires a non-null `CurrentAttemptId`. After a lease is released or the
job becomes terminal, the latest `CurrentAttemptId` remains as immutable
reference while owner and lease expiry become null.

The complete closed state taxonomy is installed now:

```text
Claimed
Assembling
AssemblySealed
Protecting
PackageSealed
ReadyForDelivery
DeliveryInProgress
DeliveryOutcomeUnknown
Delivered
ReconciliationExpired
TerminalFailed
Cancelled
Expired
```

B4 production functions may enter only `Claimed`, `Assembling`,
`TerminalFailed`, `Cancelled`, and `Expired`. Merely listing later states in the
CHECK does not authorize B4 to enter them.

The operational-head CHECK is equivalent to this truth table:

- all rows require `Revision >= 0` and `FencingToken >= 0`;
- null `CurrentAttemptId` implies fencing token 0; non-null
  `CurrentAttemptId` implies fencing token at least 1;
- `Claimed` is exactly the initial shape: revision 0, null attempt, null
  owner/expiry, and fence 0;
- `Assembling` requires a non-null current attempt, fence at least 1, and either
  both owner/expiry non-null or both null;
- every later nonterminal state already listed for C1–C3 requires a non-null
  current attempt and fence at least 1; its phase-specific owner/lease shape is
  added only by that later reviewed slice before the state is reachable;
- `TerminalFailed`, `Cancelled`, and `Expired` require owner/expiry null; their
  current attempt is null with fence 0 when terminalized from `Claimed`, or the
  exact latest attempt with fence at least 1 when terminalized from
  `Assembling`.

The CHECK does not treat a state as reachable merely because its name is in the
closed taxonomy.

### 4.6 Required relational integrity

- `raw_export_job_classes.JobId`,
  `raw_export_job_attempts.JobId`,
  `raw_export_job_transitions.JobId`, and
  `raw_export_job_operational_heads.JobId` each have an FK to
  `raw_export_job_identities.JobId`;
- attempts expose a unique `(JobId, AttemptId, FencingToken)` tuple;
- when non-null, head `(JobId, CurrentAttemptId, FencingToken)` references that
  exact attempt tuple;
- when non-null, transition `(JobId, AttemptId, FencingToken)` references that
  exact attempt tuple;
- cross-job attempt references and orphan head/attempt/transition rows are
  impossible by constraint, not only by application convention.
- the four attempt row-local domain CHECKs in section 4.3 reject an unsupported
  phase, a negative ordinal, a non-positive fence, or a non-increasing initial
  lease interval independently of cross-row command invariants.

## 5. Atomic permit-to-job bind

### 5.1 Ordered transaction

After preflight and the no-caller-transaction check, B4 opens one repository-owned
transaction explicitly at `IsolationLevel.ReadCommitted`. It verifies the opened
transaction reports Read Committed before executing step 1; otherwise it fails
with `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` and executes no projection,
lock, or mutation.

Within that transaction:

1. set `tagekyc.actor_principal_id` transaction-locally from
   `Actor.PrincipalId`;
2. call the B4 binding-input projection;
3. if it returns `GraphInvalid`:
   - with no committed B4 job, throw
     `RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` and roll back with zero B4 rows;
   - with a safely identified committed job, actor-scope read its valid B4
     identity/head, acquire the operational-head lock through the head-first
     orchestration path, and terminalize it as
     `TerminalFailed / JOB_GRAPH_INVARIANT_FAILURE`, commit the transition/head,
     and return the pinned typed terminal result;
   - if the B4 identity/head cannot be safely validated, raise
     `P0001 / RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` and roll back;

   The committed-job `GraphInvalid` path actor-reads the current identity/head
   tuple and calls `raw_export_terminalize_job` directly as the head-first
   lock/mutation seam; it does not pre-call attempt-lock because an active lease
   would return `LeaseHeld`. `Terminalized`, higher-precedence `Expired`, and
   actor-reread `AlreadyTerminal` commit and return `Terminal(JobId, exact
   result)`. A concurrent revision, fence, attempt, or owner change returns the
   existing stable concurrency/fence/lease exception and rolls back; a caller
   retry re-reads current state and may terminalize. The repository never mutates
   from a stale tuple and does not internally spin.
4. otherwise require a structurally valid Authorized permit graph, derive
   `JobExpiresAt`, and compute the canonical bind fingerprint;
5. if the projection returns a committed job for the permit:
   - equal valid fingerprint returns `ExistingMatch` with that `JobId` without
     re-running mutable B1/B2/session authorization;
   - different fingerprint returns `FingerprintConflict` with no `JobId`;
   - commit/return, with no session/B1/B2 lock and no mutation;
6. only when no committed job exists, call the landed B3 session-lock function
   and bind the locked session owner,
   subject, and state;
7. after any wait for that session lock, call the binding projection again:
   - a now-committed valid job follows the same
     `ExistingMatch`/`FingerprintConflict` rule and returns without mutation;
   - a safely attributable `GraphInvalid` committed job follows step 3;
   - only a still-unbound valid graph may proceed;
8. require:
   - actor principal equals decision principal;
   - `Actor.ClientApplicationId == decision.ClientApplicationId ==
     permit.RecipientClientApplicationId == locked session owner`;
   - permit recipient equals decision recipient;
   - locked session subject equals permit subject;
   - locked session state is `Completed`;
9. call the landed B1 eligibility resolver for exact principal/policy/version
   and require `Active`;
10. require the exact policy version still exists, remains
   `CatalogApproved`, and matches the projected `Mode`;
11. call the landed B2 consent resolver and require an Effective snapshot whose
    session, subject, policy/version, purpose, and recipient equal the frozen
    contract, whose recipient equals the permit recipient, and whose classes cover
    every permit class;
12. after all preceding authorization and session locks have been acquired,
    observe fresh physical database time with `clock_timestamp()` and immediately
    recheck every time-dependent authority value plus
    `DecisionExpiresAtUtc`; a transaction-start timestamp is forbidden;
13. call the sole claim/read function, which independently observes fresh
    physical database time after its final internal wait/re-read and either
    inserts the complete
    identity/classes/head/initial-transition aggregate or returns the committed
    concurrent winner after a visibility-safe re-read;
14. handle the claim result:
    - for `NewJob`, before commit observe another fresh
      `clock_timestamp()` after the claim's final possible permit-lock wait and
      compare every finite B1 fulfillment and B2 consent bound resolved under
      the still-held locks; if any bound has crossed while the permit/job
      deadline remains live, throw
      `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE` and roll back the entire
      transaction with zero B4 rows;
    - for a concurrent winner, apply the same equal-fingerprint
      `ExistingMatch`/different-fingerprint `FingerprintConflict` rule without
      mutable revalidation, preserving committed replay;
15. commit.

All session, B1, and B2 locks are held through step 15. B4 acquires them in the
same canonical order as B3: session row, B1 control-plane locks, immutable policy
read, then B2 consent scope. No job row is inserted before the current
authorization gates succeed.

On the no-committed-job branch, any missing, expired, revoked, withdrawn,
wrong-owner, wrong-subject, wrong-recipient, non-Completed, inactive-policy,
malformed graph, or class mismatch fails closed with zero B4 rows. A committed
equal-fingerprint replay remains recoverable after grant revoke, consent
withdrawal, session change, or permit expiry; those changes block every new or
reclaimed work attempt, not retrieval of the already-bound `JobId`. The B3
permit remains immutable and may never be marked failed or consumed in place.

### 5.2 Canonical fingerprint

The bind fingerprint is SHA-256 over namespace
`tagekyc:raw-export-job-bind:v1` and length-prefixed canonical fields in this
exact order:

```text
PrincipalId
ClientApplicationId
PermitId
AuthorizationDecisionId
VerificationSessionId
SubjectRef
PolicyId
PolicyVersion
PurposeCode
RecipientClientApplicationId
ExportMode
PermitExpiresAt
JobExpiresAt
IdempotencyKey
ordered (Ordinal, RawClass) permit classes
```

GUIDs use RFC-4122/network byte order, integers use big-endian fixed width,
timestamps use UTC ticks, text uses NFC UTF-8, and every variable-length field is
prefixed by an unsigned 32-bit big-endian byte length. The codec is C#-only; SQL
stores and compares the 32-byte result.

Independent golden vectors must cover:

- every identity field mutation;
- idempotency-key case sensitivity;
- reordered, missing, extra, duplicate, and altered classes;
- all three mode values;
- non-ASCII NFC subject/purpose;
- permit/job timestamp mutation;
- API-key rotation producing the same fingerprint.

### 5.3 Concurrent outcome

`UNIQUE(PermitId)` selects one winning job. The claim/read function uses a
retry/re-read loop equivalent to the landed B3 idempotency visibility-safe
pattern:

- winner commits; loser re-reads;
- equal valid fingerprint returns `ExistingMatch` with the winner's `JobId`;
- different fingerprint returns `FingerprintConflict` with null `JobId`;
- winner rolls back; loser may insert its own fresh UUIDv4 `JobId`;
- no committed identity may lack its classes, head, or initial transition.

This loop requires Read Committed statement-level snapshots. Repeatable Read or
Serializable is not an alternative implementation: after a wait it may retain a
pre-winner snapshot or surface serialization behavior outside the pinned outcome
contract. The claim function rejects either isolation before inspecting B4 or
authority rows.

A loser that waited for a winner never reuses a time observation made before the
wait. The production repository's second projection at step 7 normally resolves
the race before authority revalidation or insert. Independently, the claim
function first derives the authoritative verification-session ID from the
immutable B3 permit/decision graph, calls the landed session-lock function, and
only then locks the exact B3 permit row `FOR UPDATE` before any B4 insert. On the
normal repository path the session lock is re-entrant; on a direct function call
it restores the same canonical session → permit order. After acquiring both
locks it re-reads the B4 identity:

- a committed job follows `ExistingMatch`/`FingerprintConflict`;
- no job after a competing rollback captures a new `v_now :=
  clock_timestamp()`, rechecks the authoritative permit/job deadline and graph,
  then may insert;
- a crossed deadline returns `AuthorityNotEffective` with zero B4 rows.

The claim function does not duplicate the landed B1/B2 resolvers. The
repository's step-14 `NewJob` post-check therefore owns finite
fulfillment/consent expiry that can occur while claim waits on the permit row.
It uses the already-resolved finite bounds protected by the still-held B1/B2
locks and a fresh post-claim database clock. Failure rolls back the newly
inserted aggregate. This post-check never runs for `ExistingMatch`.

No conforming claim can newly wait on `UNIQUE(PermitId)` after `v_now`; the
unique constraint remains a fail-closed backstop. On the application path, the
locked session and canonical B1/B2 locks serialize the same authority graph;
after a winner rollback the repository performs steps 8–12 afresh before
permitting a new insert. A stale transaction-start instant is never an
eligibility or lease input.

A different fingerprint never creates a second job and never reveals the first
job identity.

The early committed-existing probe and final concurrent-winner re-read are both
mandatory. Tests must prove: successful bind, simulated lost response, then
grant revoke, consent withdrawal, session change, and permit expiry each still
return the same `JobId` for an exact replay, while all new/reclaimed work fails
fresh revalidation.

## 6. Lease, revision, fencing, and attempts

### 6.1 Acquisition

The first acquisition requires `CurrentState = Claimed`, no active lease, and an
exact expected revision. In one transaction it:

- creates a fresh UUIDv4 `AttemptId`;
- sets `AttemptOrdinal = 0`;
- increments `FencingToken` from 0 to 1;
- moves state to `Assembling`;
- increments revision;
- sets owner and
  `LeaseExpiresAt = min(v_now + resolved lease duration,
  PermitExpiresAt, JobExpiresAt)`;
- inserts the immutable attempt and matching transition.

Every acquisition after no owner, retryable release, lease expiry, or reclaim
creates a new attempt and increments the fencing token. Tokens are monotonically
increasing per `JobId`, never reset, and never reused.

If database time is at or after the lease-expiry cap, no attempt is inserted and
the job is atomically terminalized as `Expired`.

### 6.2 Renewal

Renewal requires:

```text
JobId
ExpectedRevision
ExpectedFencingToken
CurrentAttemptId
LeaseOwnerId
```

All values must equal the current head, and the lease must not already be
expired. Renewal:

- retains the same attempt and fencing token;
- extends expiry from database time using the resolved lease duration;
- caps expiry at `min(PermitExpiresAt, JobExpiresAt)`;
- increments revision;
- appends a `LeaseRenewed` transition in the same transaction.

It cannot reduce/reset fencing, revive a terminal job, or create authorization.

### 6.3 Retryable release and reclaim

A retry or reclaim is mode-aware:

- `EncryptedExportPacket` may create a new attempt for the same job while all
  current authority and deadlines remain valid;
- `ExternalExportOnlyNoRetain` must not reacquire after its first real
  assembly/submission attempt begins; crash or failed submission reaches the
  honest terminal/unknown outcome owned by the applicable later slice, and
  another logical export requires a new decision, permit, and job;
- `EncryptedRawVaultRetained` fails closed until a separately ratified
  mode × controller contract explicitly authorizes its retry behavior.

B4 fixture reclaim tests use `EncryptedExportPacket` and include negative
no-retain and unapproved retained-vault cases. Metadata-only fixture behavior
does not authorize future raw retry.

`AttemptFailedRetryable` and a lease-clearing retryable release are legal only
when the frozen mode already has a ratified retry contract. For no-retain and an
unapproved retained-vault pair, neither the failure command nor
acquire/reclaim may emit retryable evidence or leave an ownerless/expired
nonterminal `Assembling` head. The same transaction records
`TerminalFailed / MODE_RETRY_NOT_AUTHORIZED` and append-only terminal evidence.
Later slices may refine only their separately owned unknown-delivery state.

For a mode with a ratified retry contract, a retryable attempt failure:

- requires exact revision, attempt, owner, and fence;
- requires a currently held lease with database
  `v_now < LeaseExpiresAt`;
- leaves `CurrentState = Assembling`;
- clears `LeaseOwnerId` and `LeaseExpiresAt`;
- increments revision;
- appends a sanitized `AttemptFailedRetryable` transition.

An expired or absent lease returns `LeaseNotHeld` with no head mutation and no
transition, even when reclaim has not yet occurred and the old attempt/fence
still match the head. `raw_export_renew_job_lease` has the same live-lease
requirement. A stale worker cannot use either worker-owned command as a
terminalization seam.

The next acquisition creates a new attempt and higher fencing token.
That voluntary reacquisition is recorded as
`LeaseAcquiredAfterRetryableFailure`, not `LeaseReclaimed`.

If a worker dies while holding a lease, reclaim is allowed only after database
time is at or after `LeaseExpiresAt`. For a retry-authorized mode, reclaim
creates a new attempt and higher fence while state remains `Assembling`; it also
sets
`LeaseExpiresAt = min(v_now + resolved lease duration,
PermitExpiresAt, JobExpiresAt)`. If database time is at or after that cap, no
attempt is inserted and the job becomes `Expired`. `LeaseReclaimed` is reserved
for a head that still carries the expired owner/lease; it is not used after a
voluntary retryable release. The stale worker can no longer mutate the head or
append authoritative success/failure.

For no-retain or an unapproved retained-vault pair, an attempted reacquisition,
reclaim, or retryable-failure classification atomically terminalizes the job as
`TerminalFailed / MODE_RETRY_NOT_AUTHORIZED`; it does not merely return an error
while leaving `Assembling`.

### 6.4 Revision and CAS

Every successful operational-head mutation increments `Revision` exactly once.
The head mutation and matching transition append occur in the same database
transaction and use:

```text
JobId + ExpectedRevision + ExpectedFencingToken
```

Lease acquisition from the initial unfenced head additionally requires fence 0.
No application-side read-then-unconditional-write is permitted.

### 6.5 Physical database time

The claim function, attempt-head lock function, and four mutation functions each
capture one authoritative local `v_now := clock_timestamp()` after their final
possible blocking read/lock. Each mutation function captures it before mutation
and uses that same `v_now` for the time comparisons it owns and all timestamps
produced by that mutation. Claim independently rechecks the absolute deadline;
attempt orchestration compares the fresh landed resolver values after the head
lock and before the re-entrant mutation call.

Fresh `clock_timestamp()` does not refresh MVCC visibility. The repository-owned
Read Committed transaction supplies a new committed snapshot for each post-wait
statement; both that snapshot rule and the physical-clock rule must hold.

`transaction_timestamp()`, `statement_timestamp()`, application `UtcNow`, and a
timestamp captured before a blocking operation are forbidden for eligibility,
expiry, lease, or event time. If a command begins before a deadline but resumes
after it, physical database time and the per-command precedence in section 10.2
win. No command may silently continue with its pre-wait result.

Fencing protects authoritative DB state and later publication intent only. It
does not create exactly-once external I/O and cannot prevent a stale process from
having performed an external side effect before observing lease loss.

### 6.6 Lease duration

B4 resolves one immutable startup option:

```text
TagEkyc:RawExport:JobLeaseSeconds
```

Both default and production value are 60 seconds. Valid range is 10–300 seconds.
Missing uses 60; malformed, non-positive, below 10, or above 300 is invalid.
Invalid configuration fails readiness with
`PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID`; it is not hot-reloaded.

Tests use an injected valid resolved state and database clock; production logic
does not use application `UtcNow`, sleeps, or `DateTimeOffset.MaxValue`.

## 7. Revalidation after bind

Every new or reclaimed attempt uses one short repository transaction with this
executable order:

Before step 1, reject any caller-owned, ambient, or already-active transaction,
open a repository-owned transaction explicitly with
`IsolationLevel.ReadCommitted`, and verify its reported isolation. Failure raises
`RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` before actor binding, projection,
head/authority lock, or mutation.

1. bind the actor GUC, principal, client, and job identity;
2. call `raw_export_lock_job_for_attempt`, which locks the B4 operational head
   `FOR UPDATE`, validates its structural graph and expected revision/fence, and
   returns a normal conflict without taking authority locks when CAS does not
   match;
3. call the landed B3 session-lock function and require exact owner, subject, and
   `Completed` state;
4. acquire the B1 control-plane locks and resolve current Active eligibility;
5. read the exact policy/closure and require `CatalogApproved` plus frozen-mode
   equality;
6. acquire the B2 consent-scope locks and resolve Effective consent with exact
   scope and full class coverage;
7. after the last resolver wait, observe fresh `clock_timestamp()` and compare
   every finite fulfillment/consent validity bound plus permit/job deadlines;
8. without leaving the connection or transaction, call
   `raw_export_acquire_or_reclaim_job_lease` or the exact terminalization command
   required by section 10.2; its B4 head lock is re-entrant and therefore cannot
   newly block after authority evaluation;
9. commit before fixture/future work starts.

The attempt lock order is therefore:

```text
B4 operational head
→ verification session
→ B1 control plane
→ immutable policy
→ B2 consent scope
→ fresh physical clock
→ head/attempt/transition mutation
```

No existing-job path may first hold a session/B1/B2 lock and then wait for the B4
head. The repository reuses the landed B1/B2 resolvers under the held head lock;
it does not implement a second eligibility/consent engine in PL/pgSQL and does
not trust caller-supplied validity values. The mutation functions are privileged
repository seams within the already-recorded DEBT-E3-A compromised-backend
boundary; B4 does not claim they authenticate an arbitrary holder of the runtime
credential.

No DB transaction, row lock, or advisory lock crosses fixture work or future
raw-source, crypto, object-store, or network I/O.

If revalidation fails after the permit is already bound:

- a crossed permit/job deadline records `Expired`;
- expired B1 fulfillment or B2 consent while permit/job remains live records
  `TerminalFailed / AUTHORITY_REVALIDATION_FAILED`;
- explicit cancellation records `Cancelled`;
- session, grant, lifecycle, rule-set, fulfillment, policy, consent, binding, or
  graph failure records `TerminalFailed` with a sanitized closed reason code;
- the terminal transition and head mutation are atomic;
- no later retry or unbind is allowed.

Lease acquisition or renewal is never treated as authorization. Later C1–C3
must still perform their ratified phase-specific revalidation checkpoints.

## 8. State-transition ownership

B4 runtime functions authorize only:

```text
NULL → Claimed                      JobBound
Claimed → Assembling               LeaseAcquired
Assembling → Assembling            LeaseRenewed
Assembling → Assembling            AttemptFailedRetryable
Assembling → Assembling            LeaseAcquiredAfterRetryableFailure
Assembling → Assembling            LeaseReclaimed
Claimed|Assembling → TerminalFailed
Claimed|Assembling → Cancelled
Claimed|Assembling → Expired
```

No B4 production function may enter:

```text
AssemblySealed
Protecting
PackageSealed
ReadyForDelivery
DeliveryInProgress
DeliveryOutcomeUnknown
Delivered
ReconciliationExpired
```

TIP-88C1, C2, and C3 must add separately reviewed phase-specific commands. They
may not widen a generic B4 transition function because B4 exposes none.

`raw_export_terminalize_job` accepts nullable `attempt_id` and
`lease_owner_id` values under this exact shape matrix:

| Current head | Required terminalize tuple |
| --- | --- |
| `Claimed` | `attempt_id = NULL`, `lease_owner_id = NULL`, expected fence 0, exact revision |
| released `Assembling` | exact current attempt, `lease_owner_id = NULL`, exact positive fence/revision |
| actively leased or lease-expired `Assembling` | exact current attempt, exact current/stale owner, exact positive fence/revision |

A correct nullable shape is not `LeaseNotHeld`. Any non-matching attempt,
owner, fence, or revision returns without mutation: revision mismatch is
`ConcurrencyConflict`, fence mismatch is `FenceStale`, and attempt/owner-shape
mismatch is `LeaseNotHeld`. Terminalization from `Claimed` therefore remains
feasible for a post-bind cancellation/expiry/revalidation command, while
terminalization from `Assembling` preserves the exact latest-attempt provenance.

`raw_export_terminalize_job` is a repository-orchestration command, not a
worker-owned lease command. Authority/deadline/cancellation terminalization
must be head-first and run under the repository's held B4 operational-head lock
with the command-specific `job_head_terminal` guard context. The nullable tuple
above records exact head provenance; accepting the exact stale owner on an
expired `Assembling` head does not prove that the old worker still owns a live
lease. Worker failure must use
`raw_export_record_job_attempt_failure` and cannot reuse terminalization.

The B4 fixture work adapter is test-only. It simulates a bounded assembly attempt
far enough to prove lease loss, retryable release, crash, reclaim, and stale
worker rejection. It also includes a bounded fake external-delivery side effect
solely to prove that fencing cannot prevent an already-started external effect
while still rejecting the stale worker's authoritative DB mutation. The adapter
persists no assembly seal, package, delivery record, receipt, delivery state, or
`Delivered` claim and is never production-registered.

## 9. Database functions and privilege posture

### 9.1 Runtime entry manifest

B4 owns these exact runtime-callable `SECURITY DEFINER` functions:

1. `raw_export_read_job_binding_inputs(uuid,uuid,uuid)`;
2. `raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[])`;
3. `raw_export_read_job(uuid,uuid,uuid)`;
4. `raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint)`;
5. `raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)`;
6. `raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)`;
7. `raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text)`;
8. `raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text)`.

These identities are ratified here. The build dispatch may copy them but may not
invent another argument, change an argument type/order, widen a return surface,
or create an overload.

Every runtime-entry function:

- is owned by NOLOGIN `tagekyc_raw_export_deployer`;
- uses `LANGUAGE plpgsql`;
- is `SECURITY DEFINER`;
- pins `search_path=pg_catalog`;
- schema-qualifies every non-`pg_catalog` object;
- uses no dynamic SQL;
- revokes all PUBLIC execute;
- has exactly one explicit non-owner ACL row after normalization:
  `(grantor=tagekyc_raw_export_deployer, grantee=tagekyc_runtime,
  privilege=EXECUTE, is_grantable=false)`;
- binds the passed principal to
  `tagekyc.raw_export_current_actor()`;
- validates actor/job ownership independently of the lease owner.

The ACL comparison uses
`aclexplode(COALESCE(proacl, acldefault('f', proowner)))`, validates owner
authority separately, and excludes owner/default rows only where
`grantee = proowner`; it never drops the `grantor` dimension.

Every B4 internal guard, trigger, or helper function has the same exact NOLOGIN
deployer owner, `search_path=pg_catalog`, and no PUBLIC EXECUTE, unrelated
grantee, grant option, or runtime EXECUTE. Only the eight entries above have
the one runtime ACL row.

#### 9.1a Exact entry contracts

`raw_export_claim_or_read_job` and `raw_export_lock_job_for_attempt` begin with
the same transaction-isolation precondition before actor validation, lookup, or
lock:

```sql
current_setting('transaction_isolation') = 'read committed'
```

Any other value raises exact
`P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` with no authority/B4
lookup, row/advisory lock, mutation, or transition. This operational precondition
has highest precedence because it cannot reveal object existence. Its exact guard
and position are part of both function body manifests.

`raw_export_read_job` and the four mutation entries do not add independent SQL
isolation guards. On the production path each runs only inside a fresh
repository-owned transaction that has passed section 3.3 preflight,
ambient/active-transaction rejection, the final no-gap admission check immediately
before opening/beginning, explicit
`BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)`, reported
isolation verification, and transaction-local actor-GUC binding.

A normal valid bind that reaches claim is admitted through claim;
acquire/reclaim is admitted through attempt-lock. The safely identified
committed-job `GraphInvalid` bind path is repository-admitted and calls
`raw_export_terminalize_job` directly as specified in section 5.1, without
calling claim or attempt-lock. Standalone read, renew, record-failure, and
terminalize use their own fresh repository-owned admitted transactions and call
their exact entry directly. They must not call claim or attempt-lock merely to
obtain admission. In particular, renew and record-failure run only after
acquisition has committed and no database transaction crosses worker or other
non-database work.

Their direct-call threat boundary remains the recorded DEBT-E3-A posture. This
correction does not add a ninth function, another isolation level, a runtime
table privilege, or a new authority boundary; claim and attempt-lock retain the
two direct-runtime isolation guards.

`raw_export_read_job_binding_inputs` arguments:

```text
principal_id uuid
client_application_id uuid
permit_id uuid
```

Its exact return columns are:

```text
ProjectionOutcome text not null
ExistingJobId uuid null
ExistingFingerprintHash bytea null
AuthorizationDecisionId uuid null
DecisionPrincipalId uuid null
DecisionClientApplicationId uuid null
VerificationSessionId uuid null
SubjectRef text null
PolicyId uuid null
PolicyVersion integer null
PurposeCode text null
DecisionRecipientClientApplicationId uuid null
PermitRecipientClientApplicationId uuid null
PermitExpiresAt timestamptz null
PermitSchemaVersion integer null
ExportMode text null
ClosureType text null
ClassOrdinal integer null
RawClass text null
EvaluatedAtUtc timestamptz null
```

`ProjectionOutcome` is exactly `Valid` or `GraphInvalid`; no other nullability
shape is legal:

| Outcome | Row count and nullability |
| --- | --- |
| `Valid` | one row per permit class; every authoritative graph/class column from `AuthorizationDecisionId` through `EvaluatedAtUtc` is non-null; `ExistingJobId` and `ExistingFingerprintHash` are either both null or both non-null |
| `GraphInvalid` | exactly one sentinel row; every column except `ProjectionOutcome` and optional `ExistingJobId` is null, including `ExistingFingerprintHash`, class fields, and `EvaluatedAtUtc` |

`GraphInvalid` is non-throwing and returned only after actor-principal and client
ownership can be established without ambiguity. `ExistingJobId` is non-null only
when one committed B4 identity can be safely identified for terminalization. If
ownership cannot be established, the function returns zero rows and leaks neither
the malformed graph nor an existing job ID. An internal B4 identity/head defect
that prevents safe terminalization still raises
`P0001 / RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE`.

`raw_export_claim_or_read_job` arguments, in order:

```text
prospective_job_id uuid
permit_id uuid
authorization_decision_id uuid
principal_id uuid
client_application_id uuid
created_by_api_key_id uuid
verification_session_id uuid
subject_ref text
policy_id uuid
policy_version integer
purpose_code text
recipient_client_application_id uuid
export_mode text
permit_expires_at timestamptz
job_expires_at timestamptz
idempotency_key text
fingerprint_hash bytea
raw_classes text[]
```

It returns exactly `(outcome text, job_id uuid)` where outcome is `NewJob`,
`ExistingMatch`, `FingerprintConflict`, or `AuthorityNotEffective`. The ID is
non-null only for `NewJob` and `ExistingMatch`. `AuthorityNotEffective` is the
non-throwing post-wait deadline outcome for a prospective new bind and commits no
B4 row. All identity/class equality is rechecked against landed authoritative
rows inside the function before insert. Caller-provided `prospective_job_id` is
used only for a winning new insert. Times, initial state/revision/fence, head
timestamp, class ordinals, and transition identity/time are database-derived;
caller cannot supply them.

After preserving the landed actor missing/invalid taxonomy and validating that
the passed principal matches the actor context, the claim function validates
`idempotency_key` against the exact section-3.1 grammar before any B4 lookup or
insert. Empty, whitespace, newline, non-ASCII, disallowed punctuation, or a value
longer than 256 characters raises
`P0001 / RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED` and leaves zero B4 rows. The
table CHECK remains an independent backstop for every insert path.

The claim function itself reads the exact
`raw_export_policy_versions` and `raw_export_policy_closures` rows, requires
`CatalogApproved`, and requires caller-supplied `export_mode` to byte-equal the
stored `Mode` before either inserting or returning `ExistingMatch`. The
application projection is a read seam, not an unforgeable capability token.

For a prospective `NewJob`, `AuthorityNotEffective` is returned when the
function's fresh `v_now`, captured only after its authoritative landed session
lock, exact B3 permit-row `FOR UPDATE` serialization lock, and B4 identity
re-read, is at or after the authoritative permit/job deadline. B1/B2 mutable
authority remains owned by the ordered repository transaction in section 5; the
function does not duplicate the landed C# resolvers. A committed
equal-fingerprint replay is detected before mutable revalidation and still
returns `ExistingMatch`.

`raw_export_read_job` arguments are
`(principal_id uuid, client_application_id uuid, job_id uuid)`. Its exact return
columns are:

```text
JobId uuid
PermitId uuid
AuthorizationDecisionId uuid
PrincipalId uuid
ClientApplicationId uuid
CreatedByApiKeyId uuid
VerificationSessionId uuid
SubjectRef text
PolicyId uuid
PolicyVersion integer
PurposeCode text
RecipientClientApplicationId uuid
ExportMode text
PermitExpiresAt timestamptz
JobExpiresAt timestamptz
SchemaVersion integer
CreatedAt timestamptz
ClassOrdinal integer
RawClass text
CurrentState text
Revision bigint
CurrentAttemptId uuid null
LeaseOwnerId uuid null
LeaseExpiresAt timestamptz null
FencingToken bigint
LatestEventType text
LatestFailureCode text null
LatestOccurredAt timestamptz
```

It returns zero rows for absent/not-owned and never returns another actor's job.

`raw_export_lock_job_for_attempt` arguments are
`(job_id uuid, principal_id uuid, client_application_id uuid,
expected_revision bigint, expected_fencing_token bigint)`. It actor-scopes the
job, locks the operational head `FOR UPDATE`, and returns exactly:

```text
outcome text
current_state text null
current_revision bigint null
current_fencing_token bigint null
```

Outcomes are `Locked`, `NotFound`, `AlreadyTerminal`, `LeaseHeld`,
`ConcurrencyConflict`, or `FenceStale`. Exact result shape:

| Outcome | Current state/revision/fence |
| --- | --- |
| `Locked`, `AlreadyTerminal`, `LeaseHeld` | all three non-null |
| `NotFound`, `ConcurrencyConflict`, `FenceStale` | all three null |

`Locked` authorizes no mutation by itself. `LeaseHeld` is returned before
authority resolution only when an unexpired active lease and the absolute
permit/job deadline are both still live; an absolute deadline crossing remains
`Locked` so the repository can record `Expired`. `NotFound` returns no job
detail. The repository must keep the same connection and transaction through the
section 7 authority resolvers and mutation call; disposal, commit, rollback, or
transaction replacement releases the lock and invalidates the command.

The four mutation functions all return:

```text
outcome text
resulting_revision bigint null
resulting_fencing_token bigint null
resulting_lease_expires_at timestamptz null
```

Their exact arguments and normal outcomes are:

| Function | Arguments after `job_id, principal_id, client_application_id` | Normal outcomes |
| --- | --- | --- |
| `raw_export_acquire_or_reclaim_job_lease` | `expected_revision bigint, expected_fencing_token bigint, attempt_id uuid, lease_owner_id uuid, lease_seconds integer` | `Acquired`, `AcquiredAfterRetryableFailure`, `Reclaimed`, `TerminalizedModeRetryForbidden`, `ConcurrencyConflict`, `LeaseHeld`, `Expired` |
| `raw_export_renew_job_lease` | `expected_revision bigint, expected_fencing_token bigint, attempt_id uuid, lease_owner_id uuid, lease_seconds integer` | `Renewed`, `ConcurrencyConflict`, `LeaseNotHeld`, `FenceStale` |
| `raw_export_record_job_attempt_failure` | `expected_revision bigint, expected_fencing_token bigint, attempt_id uuid, lease_owner_id uuid, failure_code text` | `Recorded`, `TerminalizedModeRetryForbidden`, `ConcurrencyConflict`, `LeaseNotHeld`, `FenceStale`, `TransitionInvalid` |
| `raw_export_terminalize_job` | `expected_revision bigint, expected_fencing_token bigint, attempt_id uuid, lease_owner_id uuid, terminal_state text, reason_code text` | `Terminalized`, `AlreadyTerminal`, `Expired`, `ConcurrencyConflict`, `LeaseNotHeld`, `FenceStale`, `TransitionInvalid` |

`lease_seconds` comes only from the immutable resolved configuration, but each
lease function independently enforces 10–300. Lease expiry and all timestamps
are database-derived. `terminal_state` accepts only `TerminalFailed`,
`Cancelled`, or `Expired`; `Expired` additionally requires database time at or
after the frozen deadline. Failure/reason codes use the exact section 10
allowlist. No function accepts `CurrentState`, next non-terminal state,
`FencingToken` result, revision result, timestamp, or transition event name from
the caller.

For a nonterminal head, acquire/reclaim and head-first explicit terminalization
may force `Expired` after actor/graph/CAS validation when the permit/job
deadline has crossed. They append exactly
`JobExpired / PERMIT_OR_JOB_EXPIRED`; explicit terminalization does not append
the caller's lower-precedence failure or cancellation reason.

Renew and record-attempt-failure are different: after
actor/graph/CAS/attempt/owner validation they require a live current lease
before any terminal/deadline or mode mutation. An absent or expired lease
returns `LeaseNotHeld` with zero mutation. If an absolute deadline has also
crossed, head-first repository orchestration owns the later `Expired`
terminalization. An already-terminal head remains `AlreadyTerminal` and is
never rewritten as `Expired`.

When acquire/reclaim returns `TerminalizedModeRetryForbidden`, the prospective
`attempt_id` is neither inserted nor referenced. The terminal transition derives
its nullable attempt/owner/fence tuple from the current head under the section 8
matrix.

Actor context preserves the landed helper taxonomy exactly:

- unset or blank GUC raises
  `P0001 / RAW_EXPORT_ACTOR_CONTEXT_MISSING`;
- malformed text or the zero UUID raises
  `P0001 / RAW_EXPORT_ACTOR_CONTEXT_INVALID`;
- a valid actor different from the passed principal raises
  `P0001 / RAW_EXPORT_JOB_ACTOR_MISMATCH`.

No B4 entry may collapse a missing/invalid helper failure into the mismatch
code. The binding projection uses the
non-aborting `GraphInvalid` outcome above for a safely attributable malformed
authoritative graph. The claim function rejects a new-bind authoritative mismatch
with `P0001 / RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE`; mutation functions use
that `P0001` only for an internal B4 structural defect that prevents safe
terminalization. Expected concurrency, lease, fence, mode, terminal, and
fingerprint cases return the typed outcomes above and are mapped to section 10
without exposing database exception text.

#### 9.1b Function dependencies and guard contexts

| Entry | Read dependencies | Mutation-context tokens |
| --- | --- | --- |
| binding inputs | B3 decision/permit/permit classes, exact 88A policy/closure, B4 identity | none |
| claim/read | landed verification-session lock seam, B3 decision/permit/permit classes, exact 88A policy/closure, B4 identity/classes/head/transition | `job_identity`, `job_class`, `job_head_insert`, `job_transition` |
| actor-scoped read | B4 identity/classes/head/attempt/transition | none |
| attempt head lock | B4 identity/head | none |
| acquire/reclaim | B4 identity/head/attempt/transition | `job_attempt`, `job_head_acquire`, `job_transition` |
| renew | B4 identity/head/attempt/transition | `job_head_renew`, `job_transition` |
| attempt failure | B4 identity/head/attempt/transition | `job_head_release`, `job_transition` |
| terminalize | B4 identity/head/attempt/transition | `job_head_terminal`, `job_transition` |

The head guard accepts only the command-specific token in this table. A token
for one command cannot authorize another head mutation. Exact
`pg_get_function_identity_arguments`, return columns, function dependencies,
owner, language, `prosecdef`, search path, body digest, ACL, and guard-token
mapping are all readiness-manifest inputs.

The claim body manifest also pins the exact named-constraint sequence:

```text
DEFERRED at entry
→ parent/classes/head/initial transition inserts
→ IMMEDIATE validation
→ DEFERRED restoration before successful NewJob return
```

### 9.2 Table privileges

For all five B4 tables, `tagekyc_runtime` and PUBLIC have none of:

```text
SELECT
INSERT
UPDATE
DELETE
TRUNCATE
REFERENCES
TRIGGER
```

Runtime reads and commands cross only the eight functions above. Deployer owns
the B4 objects. For each table, the normalized explicit non-owner ACL set is
exactly empty after
`aclexplode(COALESCE(relacl, acldefault('r', relowner)))`; this is stronger
than checking only runtime and PUBLIC. Any unrelated grantee, privilege, grant
option, or alternate grantor is invalid.

For every non-dropped user column on those tables (`attnum > 0` and
`attisdropped = false`), the exact explicit non-owner column ACL set is also
empty. The manifest reads `pg_attribute.attacl` and expands each non-null value
with `aclexplode(attacl)`, preserving table, column, grantor, grantee, privilege,
and grantable. Column-level `SELECT`, `INSERT`, `UPDATE`, or `REFERENCES` is
invalid even when the containing table's `relacl` manifest is clean.

The migration must not grant new privilege on a landed
88A/B1/B2/B3/E3 table or replace an existing landed function. A pre-build
capability census must prove the deployer already holds every backing read
required by the binding projection; otherwise implementation stops for a
separately reviewed extension.

After all B4 objects are created, owned, revoked, and granted, the migration
must assert the exact function, table ACL, and column ACL manifests inside the
migration transaction before its history row can commit. Unexpected function
state raises `TIP88B4_FUNCTION_ACL_INVALID`; unexpected table or column state raises
`TIP88B4_TABLE_ACL_INVALID`. The migration must abort rather than dynamically
revoke an unrelated role introduced by default privileges or pre-existing
drift.

### 9.3 Mutation guards

Identity, classes, attempts, and transitions:

- reject UPDATE and DELETE even as owner;
- reject direct INSERT unless the corresponding B4 function sets the exact
  transaction-local insert context;
- reject child append outside the parent/attempt transaction where required.

Operational head:

- rejects direct INSERT, UPDATE, and DELETE unless a B4 function sets the exact
  per-command mutation context;
- validates state/lease row shape in a DB CHECK;
- cannot be truncated by runtime;
- is never updated by ordinary EF change tracking.

The migration must use B4-specific guard functions and context tokens. It must
not weaken or reuse a context token owned by another slice.

### 9.4 Readiness

One B4 readiness validator fails closed on:

- missing/drifted table, column, constraint, trigger, or identifier manifest;
- missing or definition-drifted idempotency-key or attempt row-local CHECK;
- wrong function signature, owner, `prosecdef`, search path, exact
  grantor-aware ACL, or body dependency;
- any non-owner ACL row on a B4 table;
- any explicit non-owner column ACL row on a non-dropped user column of a B4
  table, including an alternate grantor or grant option;
- missing runtime execute on any required entry;
- an extra B4 runtime-granted function;
- PUBLIC/runtime/unrelated execute on an internal B4 helper;
- invalid lease configuration;
- impossible operational-head row shape.

Stable readiness codes:

```text
PROD_RAW_EXPORT_JOB_SCHEMA_INVALID
PROD_RAW_EXPORT_JOB_FUNCTION_ACL_INVALID
PROD_RAW_EXPORT_JOB_TABLE_PRIVILEGE_INVALID
PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID
```

Readiness returns HTTP 503 through the existing readiness pipeline and exposes
no job, permit, subject, or actor values.

Transaction isolation is per transaction, not static deployment state, so it
adds no readiness code. The existing function body manifest detects removal or
reordering of either SQL isolation guard; repository/source and direct-runtime
tests prove the live transaction behavior.

## 10. Stable application outcomes

The repository uses typed outcomes/exceptions with stable codes:

```text
RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED
RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID
RAW_EXPORT_JOB_PERMIT_UNAVAILABLE
RAW_EXPORT_ACTOR_CONTEXT_MISSING
RAW_EXPORT_ACTOR_CONTEXT_INVALID
RAW_EXPORT_JOB_ACTOR_MISMATCH
RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE
RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT
RAW_EXPORT_JOB_CONCURRENCY_CONFLICT
RAW_EXPORT_JOB_LEASE_HELD
RAW_EXPORT_JOB_LEASE_NOT_HELD
RAW_EXPORT_JOB_FENCE_STALE
RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED
RAW_EXPORT_JOB_TRANSITION_INVALID
RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE
PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID
```

`PERMIT_UNAVAILABLE` normalizes absent permit and actor-not-owned permit at the
repository boundary. Exact internal invariant failures may be logged only as
sanitized codes, never with subject or policy contents.
`TRANSACTION_ISOLATION_INVALID` is an operational/integration failure. It is not
an authority denial, request-validation failure, concurrency conflict, or graph
invariant failure.
`PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` is the shared fail-closed application
code for invalid resolved lease configuration and direct acquire/renew
lease-bound rejection; the internal SQL message is not exposed.

### 10.1 SQL/result mapping

| Entry/outcome | Repository result/code | Mutation | Returned revision/fence/lease expiry |
| --- | --- | --- | --- |
| any preflight-valid `IRawExportJobRepository` method receives a caller-owned, ambient, active, or non-Read-Committed transaction | throw `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` before any B4 database command | none | n/a |
| direct claim or attempt-lock runs outside Read Committed and raises `P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` | preserve exact `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` | none; no lookup, lock, or B4 rows | n/a |
| direct acquire or renew receives `lease_seconds` outside 10–300 after valid actor/principal binding and raises `P0001 / RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` | map to `PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` without revealing job existence | none; no lookup, lock, or B4 rows | n/a |
| direct claim idempotency key violates the exact grammar | raise `P0001`; map to `RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED` | none; zero B4 rows | n/a |
| binding projection returns zero rows | throw `RAW_EXPORT_JOB_PERMIT_UNAVAILABLE` | none | n/a |
| binding projection `GraphInvalid` with no committed B4 job | throw `RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` | transaction rolls back; zero B4 rows | n/a |
| binding projection `GraphInvalid` with a safely identified committed B4 job and direct terminalize returns `Terminalized`, higher-precedence `Expired`, or actor-reread `AlreadyTerminal` | return `Terminal(JobId, exact RawExportJobTerminalizeResult)` after commit | exact terminal head + evidence for `Terminalized`/`Expired`; none for `AlreadyTerminal` | exact committed/current result |
| committed-job `GraphInvalid` direct terminalize detects concurrent revision, fence, attempt, or owner change | throw the existing stable concurrency/fence/lease exception and roll back; caller retry re-reads current state | none | all null |
| new-bind authority revalidation fails while no B4 job is committed | throw `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE` | transaction rolls back; zero B4 rows | n/a |
| already-bound attempt authority revalidation fails while the B4 identity/head terminal tuple is valid | typed `TerminalFailed(RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE)` | `TerminalFailed / AUTHORITY_REVALIDATION_FAILED` head + evidence commit atomically | revision/fence non-null; lease expiry null |
| already-bound authoritative graph failure is detected while the B4 identity/head terminal tuple is valid | typed `TerminalFailed(RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE)` | `TerminalFailed / JOB_GRAPH_INVARIANT_FAILURE` head + evidence commit atomically | revision/fence non-null; lease expiry null |
| claim `NewJob` | success `NewJob(JobId)` | complete initial aggregate | n/a |
| claim `ExistingMatch` | success `ExistingMatch(JobId)` | none | n/a |
| claim `FingerprintConflict` | throw `RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT`; no JobId | none | n/a |
| claim `AuthorityNotEffective` | throw `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE`; no JobId | transaction rolls back; zero B4 rows | n/a |
| claim authoritative mismatch `P0001 / RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` | throw `RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE`; no JobId | transaction rolls back; zero B4 rows | n/a |
| repository post-claim `NewJob` finite B1/B2 bound has crossed | throw `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE`; no JobId | transaction rolls back the inserted aggregate; zero B4 rows | n/a |
| actor-scoped read returns zero rows | typed `NotFound` | none | n/a |
| attempt-head lock `Locked` | continue authority revalidation in the same transaction | row lock only | current revision/fence non-null |
| attempt-head lock `NotFound` | typed `NotFound` | none | all null |
| attempt-head lock `AlreadyTerminal` | actor-scoped reread, then idempotent current-terminal result | none | current revision/fence non-null |
| attempt-head lock `LeaseHeld` | throw `RAW_EXPORT_JOB_LEASE_HELD` | none | current revision/fence non-null |
| acquire `Acquired`, `AcquiredAfterRetryableFailure`, or `Reclaimed` | success with new attempt/head | yes | revision/fence/lease expiry non-null |
| acquire/failure `TerminalizedModeRetryForbidden` | typed `TerminalFailed(RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED)` | `TerminalFailed / MODE_RETRY_NOT_AUTHORIZED` head + evidence commit atomically | revision/fence non-null; lease expiry null |
| acquire `LeaseHeld` | throw `RAW_EXPORT_JOB_LEASE_HELD` | none | all null |
| acquire/terminalize `Expired` | success `Expired` | `Expired / PERMIT_OR_JOB_EXPIRED` head + evidence | revision/fence non-null; lease expiry null |
| renew `Renewed` | success | head + evidence | revision/fence/lease expiry non-null |
| failure `Recorded` | success retryable release | head + evidence | revision/fence non-null; lease expiry null |
| terminalize `Terminalized` | success with terminal state | head + evidence | revision/fence non-null; lease expiry null |
| terminalize `AlreadyTerminal` | idempotent SQL success; repository performs one actor-scoped `raw_export_read_job` reread before surfacing the exact current terminal state | none | current revision/fence non-null; lease expiry null |
| any `ConcurrencyConflict` | throw `RAW_EXPORT_JOB_CONCURRENCY_CONFLICT` | none | all null |
| any `LeaseNotHeld` | throw `RAW_EXPORT_JOB_LEASE_NOT_HELD` | none | all null |
| any `FenceStale` | throw `RAW_EXPORT_JOB_FENCE_STALE` | none | all null |
| any `TransitionInvalid` | throw `RAW_EXPORT_JOB_TRANSITION_INVALID` | none | all null |
| actor GUC unset or blank raises `P0001 / RAW_EXPORT_ACTOR_CONTEXT_MISSING` | preserve exact `RAW_EXPORT_ACTOR_CONTEXT_MISSING` | none | n/a |
| actor GUC malformed or zero UUID raises `P0001 / RAW_EXPORT_ACTOR_CONTEXT_INVALID` | preserve exact `RAW_EXPORT_ACTOR_CONTEXT_INVALID` | none | n/a |
| valid GUC actor differs from passed principal and raises `P0001 / RAW_EXPORT_JOB_ACTOR_MISMATCH` | throw `RAW_EXPORT_JOB_ACTOR_MISMATCH` | none | n/a |
| internal B4 structural failure prevents safe validation of the identity/head terminal tuple | throw `RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` | transaction rolls back; no partial mutation | n/a |

No other mapping is permitted. An already-bound authority or safely
terminalizable authoritative-graph failure is not thrown before commit: the
repository first commits the terminal head and transition, then surfaces the
pinned typed terminal result. Only an internal B4 structural failure that makes
that terminal tuple unsafe to validate may raise `P0001` and roll back. In
particular, mode-forbidden retry terminalizes honestly; it does not append
retryable evidence and then throw while leaving an ownerless `Assembling` job.
The four-column mutation return surface does not contain state; `AlreadyTerminal`
therefore cannot be translated to a state-bearing repository result without the
pinned actor-scoped reread.

### 10.2 Per-command precedence

The claim and attempt-lock seams first validate Read Committed isolation before
actor context, lookup, or lock. Every function then validates actor context and
passed-principal binding. The claim function next validates the exact
idempotency-key grammar before any authoritative or B4 lookup or insert, then
validates ownership and its internal graph. Other mutation functions next
validate ownership/internal graph shape and then expected revision, fence,
attempt, and lease-owner shape before a caller may cause a terminal mutation.
This prevents a stale or foreign worker from expiring or terminalizing a job.

For `raw_export_acquire_or_reclaim_job_lease` and
`raw_export_renew_job_lease` only, after actor-context and passed-principal
validation, validate `lease_seconds` is between 10 and 300 inclusive before
ownership, graph, job lookup, row lock, CAS, fence, attempt, or lease-owner
validation. An invalid bound raises exact
`P0001 / RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` with no lookup, lock, mutation, or
residue. Invalid actor plus invalid seconds returns the actor code; valid actor
plus invalid seconds returns the lease-config code without revealing job
existence; valid seconds proceeds to the existing mutation precedence.

After those checks, the exact precedence is:

| Command | Higher-to-lower precedence after the command-specific validations above |
| --- | --- |
| repository bind plus claim/read | invalid repository command → preflight `RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED`; preflight-valid caller-owned/ambient/active transaction or opened isolation mismatch → `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; direct claim non-Read-Committed → `P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; direct claim invalid idempotency grammar after actor validation → `P0001 / RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED`; committed equal fingerprint → `ExistingMatch`; committed different fingerprint → `FingerprintConflict`; prospective new bind with ineffective session/B1/policy/B2 → repository throws `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE` before claim; claim post-wait permit/job deadline crossing → SQL `AuthorityNotEffective` mapped to the same code; claim `NewJob` followed by a crossed finite B1/B2 bound → repository rolls back the aggregate and throws the same code; otherwise `NewJob` |
| attempt orchestration before acquire/reclaim | invalid repository command → its pinned request/command-validation outcome; preflight-valid caller-owned/ambient/active transaction or opened isolation mismatch → `RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; direct attempt-lock non-Read-Committed → `P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; otherwise `raw_export_lock_job_for_attempt` actor/graph/CAS/active-lease result; crossed permit/job deadline → terminalize `Expired`; session/B1/policy/B2/finite fulfillment-or-consent invalid while absolute deadline remains live → terminalize `TerminalFailed / AUTHORITY_REVALIDATION_FAILED`; safely attributable authoritative graph invalid → terminalize `TerminalFailed / JOB_GRAPH_INVARIANT_FAILURE`; retry forbidden by mode → acquire returns `TerminalizedModeRetryForbidden`; otherwise acquire/reclaim |
| renew | absent/expired current lease → `LeaseNotHeld` with no mutation; otherwise `Renewed`; any crossed absolute deadline is terminalized later by head-first repository orchestration |
| record attempt failure | absent/expired current lease → `LeaseNotHeld` with no mutation; retry forbidden by mode → `TerminalizedModeRetryForbidden`; otherwise `Recorded` for the sole allowed retryable code; any absolute expiry or non-retryable failure is requested through head-first repository orchestration, not the worker command |
| explicit terminalize | repository orchestration holds the B4 head lock and supplies the exact command-specific tuple; already terminal → `AlreadyTerminal`; crossed permit/job deadline → `Expired`; otherwise the validated requested `TerminalFailed`/`Cancelled`/`Expired` reason; the exact stale-owner tuple is provenance, not worker lease authority |

Finite B1 fulfillment or B2 consent expiry is authority loss, not job expiry:
while `PermitExpiresAt` and `JobExpiresAt` remain live it maps only to
`TerminalFailed / AUTHORITY_REVALIDATION_FAILED`. Absolute permit/job expiry
maps only to `Expired / PERMIT_OR_JOB_EXPIRED`. Tests must exercise coincident
boundaries so the order cannot be reversed accidentally.

### 10.3 Closed evidence-code allowlist

The only B4 retryable `failure_code` is:

```text
ATTEMPT_EXECUTION_FAILED_RETRYABLE
```

It is valid only with `AttemptFailedRetryable`, an `EncryptedExportPacket` job,
and the retryable-release row shape.

The complete B4 terminal `reason_code` set is:

```text
AUTHORITY_REVALIDATION_FAILED
ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE
JOB_GRAPH_INVARIANT_FAILURE
MODE_RETRY_NOT_AUTHORIZED
PERMIT_OR_JOB_EXPIRED
REQUEST_CANCELLED
```

Exact pairing:

| Terminal state/EventType | Allowed reason |
| --- | --- |
| `TerminalFailed` / `JobTerminalFailed` | `AUTHORITY_REVALIDATION_FAILED`, `ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE`, `JOB_GRAPH_INVARIANT_FAILURE`, or `MODE_RETRY_NOT_AUTHORIZED` |
| `Cancelled` / `JobCancelled` | `REQUEST_CANCELLED` only |
| `Expired` / `JobExpired` | `PERMIT_OR_JOB_EXPIRED` only |

The DB CHECK and command functions both reject every unlisted token,
wrong-event pairing, blank value, or value containing free-form detail. Later
slices add codes only through their own reviewed schema/function extension.

## 11. STOP / NOT

- No raw bytes, raw vault, provider credential, source establishment, artifact
  reference, manifest, package, encryption, recipient key, delivery, receipt,
  HTTP endpoint, public DTO, queue transport, or background worker.
- No fake production assembler or delivery implementation.
- No mutable consumed flag or update on the landed B3 permit.
- No second job for one permit, no unbind, no job cloning, and no reassembly
  semantics.
- No general runtime-granted transition function.
- No entry into C1/C2/C3-owned states.
- No direct runtime table privilege on B4 tables.
- No alteration to landed 88A/B1/B2/B3/E1/E2/E3 tables, functions, triggers,
  roles, grants, repositories, or decision/permit schemas.
- No `TenantId`, shared-hospital database, SignFlow transaction, signing-session,
  or consumer-specific model.
- No claim of exactly-once external work or physical delivery.
- No transaction/lock across non-database work.
- No application clock for authoritative deadlines or leases.
- No `MaxValue` sentinel and no in-place deadline extension.
- No implementation until this planning brief is ratified and a separate build
  dispatch defines the exact allowlist.

## 12. Verification gates for the future build

Each gate requires a dedicated named test; several gates may not be folded into
one broad happy-path assertion.

### M1 — Grounding and identifier round-trip

`M1_intended_b4_identifiers_fit_and_round_trip_exactly`

Static UTF-8 byte-length checks and exact catalog round-trip cover every B4
table, column, constraint, index, trigger, and function. Mutation: add an
over-63-byte intended identifier and matching DDL; the static and round-trip
halves both go red.

### M2 — Immutable identity and exact B3/policy freeze

`M2_bind_freezes_exact_authoritative_identity_mode_and_classes`

Assert every identity field, ordered class, mode, permit/job deadline, and
database timestamp. Mutating any copied field or removing one class must make
the test red. A named direct-claim mutation changes the `export_mode` argument
after the authoritative projection; the claim function must raise
`RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` and create zero B4 rows.
Separate reader-contract cases assert every field in the `Valid` and
`GraphInvalid` nullability table, including null `EvaluatedAtUtc` on the
sentinel, so the C# mapping cannot silently assume the union branch is non-null.

### M3 — One permit, one job, idempotent concurrency

`M3_concurrent_bind_same_fingerprint_returns_one_job`

`M3_concurrent_bind_different_fingerprint_fails_closed`

`M3_lost_response_replay_returns_existing_job_after_authority_changes`

`M3_blocked_bind_rechecks_physical_time_after_winner_rollback`

`M3_claim_function_permit_lock_wait_rechecks_physical_time`

`M3_new_bind_permit_lock_wait_crossing_finite_authority_rolls_back`

`M3_production_bind_and_direct_claim_share_session_then_permit_order`

`M3_repository_bind_owns_explicit_read_committed_transaction`

`M3_bind_rejects_caller_owned_or_ambient_transaction`

`M3_bind_preflight_precedes_transaction_ownership_failure`

`M3_bind_rejects_matching_read_committed_ambient_before_database`

`M3_bind_rejects_active_read_committed_connection_transaction`

`M3_direct_claim_rejects_repeatable_read_and_serializable`

`B4_does_not_assign_or_normalize_connection_string`

`B4_does_not_reference_enlist_or_npgsql_connection_string_builder`

The `enlist` token in that test identifier names the superseded mechanism whose
reintroduction it prohibits; it is not an active configuration requirement.

`B4_uses_same_scoped_dbcontext_connection_and_transaction_for_B1_B2`

`B4_does_not_create_second_dbcontext_connection_or_datasource`

`B4_all_six_methods_preflight_precedes_transaction_admission`

`B4_all_six_methods_reject_ambient_transaction_before_database`

`B4_all_six_methods_reject_existing_ef_transaction_before_database`

`B4_all_six_methods_reject_existing_provider_transaction_before_database`

`B4_all_six_methods_reject_open_connection_before_database`

`B4_all_six_methods_own_fresh_read_committed_transactions`

`B4_all_six_methods_final_admission_is_adjacent_to_each_open_or_begin`

`B4_same_scope_landed_repository_can_reopen_after_B4`

`B4_connection_is_closed_and_transaction_free_after_every_exit`

`B4_global_persistence_options_remain_unchanged`

Two real connections prove winner-commit, winner-rollback, same fingerprint,
different fingerprint, exactly one identity/head/initial transition, and zero
partial rows. The replay test binds successfully, simulates response loss, then
separately mutates grant, consent, session, and database time beyond permit
expiry; every exact replay returns the same `JobId`, while a new/reclaimed
attempt fails revalidation. The blocked-bind test begins before expiry, waits
behind the competing bind/session lock, resumes after database time crosses the
deadline, and proves the repository throws exact
`RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE` before invoking claim and creates zero
B4 rows after the winner rolls back.

The separate direct-function race holds the exact B3 permit row in connection A,
starts `raw_export_claim_or_read_job` in connection B while the deadline is live,
waits past expiry, then rolls A back. Connection B must acquire the permit lock,
capture fresh `v_now`, return SQL outcome `AuthorityNotEffective` with null
`job_id`, and leave zero B4 rows. Removing the permit-row lock or moving `v_now`
before it must make this named test red.

The lock-order race runs a normal repository bind and a direct claim concurrently
against the same authoritative session/permit, pausing each at discriminating
lock boundaries. Both paths must acquire session before permit, must never raise
SQLSTATE `40P01`, and must produce only the exact winner-commit,
winner-rollback, `ExistingMatch`, or zero-residue outcomes already allowed.
Scratch-removing the claim's landed session-lock call must make this named test
red before the source is restored.

The transaction-isolation matrix changes the dedicated runtime role and database
defaults to Repeatable Read. The production repository must open its own
transaction explicitly with
`IsolationLevel.ReadCommitted`, report Read Committed, and preserve the normal
bind outcomes. Direct claim calls in Repeatable Read and Serializable must raise
exact `P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` before any lookup or
lock and leave zero B4 rows.

Separate early-rejection cases use an ambient **Read Committed** transaction and
an injected connection already carrying a caller-owned **Read Committed**
transaction. Matching isolation prevents the later SQL guard from masking a
missing repository gate. Each case asserts the stable isolation code and proves
the ambient case acquires/opens no B4 connection and neither case executes a B4
database command: no actor GUC, binding projection, claim call,
authority/advisory/row lock, or mutation. The injected connection is already open
only as fixture setup; the repository issues nothing on it. An overlap case
supplies an invalid bind command inside the same ambient scope and must return
`RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED` before the isolation code.

The source/architecture half asserts the explicit
`BeginTransactionAsync(IsolationLevel.ReadCommitted, ...)` contract, the
pre-open ambient/current-transaction gates, and the absence of any B4
connection-string assignment or normalization. The source-prohibition scan is
limited exactly to `EfRawExportJobRepository.cs` and the B4 migration pair;
landed slices elsewhere are not part of that assertion. This is intentionally
separate from the runtime positive control:
Npgsql currently defaults `BeginTransactionAsync()` to Read Committed, so merely
deleting the explicit argument could otherwise leave a runtime test green.
Scratch-changing the explicit isolation to Repeatable Read, removing an ambient
or current-transaction gate, inserting a connection-string assignment or
provider-specific builder, removing the architecture assertion's required
overload, or removing the claim isolation guard must make its corresponding
named test red. Creating a second context, connection, data source, connection
factory, or service scope must red the shared-instance test.

Each all-six admission test is data-driven with one independently reported case
per repository method. The preflight-precedence test supplies an invalid command
while ambient, EF-current, provider-current, and currently-open states are each
present; every method must return request validation before observing or
reporting transaction admission. Moving preflight after any admission check in
one method must red that method's named case.

The final-admission adjacency test reports each repository method and each
invocation kind separately. When a method explicitly invokes both `OpenAsync`
and `BeginTransactionAsync`, it must repeat the final ambient/current check
immediately before each invocation; the awaited completion of `OpenAsync` is
followed by a new final check before begin. When one invocation kind is absent,
that method's named case asserts its absence rather than silently omitting the
cell. Inserting an application-level await, callback, resolver, or database
command between either final check and its invocation must red the exact
method/invocation case.

The cleanup and same-scope-reopen evidence is a complete 24-cell matrix:
six repository methods multiplied by success, typed failure, provider exception,
and cancellation. No cell is “where applicable” or may be omitted. Every cell
asserts transaction disposal, EF/provider transaction absence,
`ConnectionState.Closed`, and successful landed same-scope reuse after the B4
exit. A cleanup mutation in any exit class must red that method/exit cell in both
`B4_connection_is_closed_and_transaction_free_after_every_exit` and
`B4_same_scope_landed_repository_can_reopen_after_B4`.

The same-scope reopen test must first execute a landed B1/B2/B3 read that opens
and closes the scoped connection, then execute B4, then call a landed repository
through that same scope. A fresh never-opened DbContext is not acceptable
evidence for the supported production topology.

The finite-authority race iterates the B2 consent bound and every finite B1
fulfillment requirement stream while keeping `PermitExpiresAt` and
`JobExpiresAt` live. Connection A holds the exact B3 permit row; connection B
starts a production repository bind while the selected finite bound is live.
After that bound expires, A releases the permit lock. Claim may return
`NewJob` internally, but the repository must observe fresh post-claim database
time, roll back the complete B4 aggregate, and throw exact
`RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE`. Scratch-removing the step-14
post-check must make this named test red.

### M4 — Fingerprint codec

`M4_job_bind_fingerprint_matches_independent_golden_vectors`

Every field and class mutation is discriminating; API-key rotation is
non-discriminating by design. Golden expected hashes are independent of the
production codec.

### M5 — Revalidation and lock races

`M5_bind_and_attempt_revalidate_all_authority_gates`

`M5_grant_revoke_blocks_behind_attempt_revalidation`

`M5_lifecycle_suspend_or_revoke_blocks_behind_bind_and_attempt_revalidation`

`M5_rule_set_publish_blocks_behind_bind_and_attempt_revalidation`

`M5_each_fulfillment_withdraw_blocks_behind_bind_and_attempt_revalidation`

`M5_consent_withdraw_blocks_behind_attempt_revalidation`

`M5_session_transition_blocks_behind_attempt_revalidation`

`M5_head_wait_crossing_consent_or_fulfillment_expiry_fails_authority`

`M5_attempt_lock_rejects_non_read_committed_isolation`

`M5_attempt_rejects_matching_read_committed_ambient_before_database`

`M5_attempt_rejects_active_read_committed_connection_transaction`

`M5_head_wait_then_revocation_is_visible_to_revalidation`

The positive and negative matrix covers session, actor/client, subject,
recipient, B1 grant/lifecycle/rules/fulfillment, policy status/mode, B2 consent
scope/class coverage, and deadline. Scratch removal of each independent shared
lock makes its corresponding named race test red on both the bind path and a
new/reclaimed-attempt path. Fulfillment coverage iterates every requirement
stream rather than proving only one representative lock. Every new-bind failure
asserts `RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE` and zero B4 rows. The
corresponding already-bound failure asserts the typed terminal result, exact
`TerminalFailed / AUTHORITY_REVALIDATION_FAILED` head, one matching transition,
and no acquired attempt. The physical-time race uses two connections: one holds
the B4 head, the attempt call starts while consent/fulfillment is live, the
validity deadline passes, and the holder rolls back. The resumed function must
return `Locked`; the resumed repository must re-resolve while retaining that
head-first lock, insert no attempt, and commit the exact authority-failure
terminal evidence while the absolute job deadline remains live.

The isolation-negative matrix calls `raw_export_lock_job_for_attempt` directly
under Repeatable Read and Serializable and asserts exact
`P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`, no authority/head lock,
and zero mutation. The fresh-visibility race starts the production attempt path
under the repository-owned Read Committed transaction, blocks connection B on
the B4 head, commits in connection A each representative grant revoke and consent
withdrawal, then releases the head. B must observe the newly committed authority
loss and terminalize `TerminalFailed / AUTHORITY_REVALIDATION_FAILED` with no new
attempt. Changing the repository isolation, accepting an ambient transaction, or
removing the attempt-lock isolation guard must make a named test red.

The repository-level attempt cases separately use an ambient Read Committed
transaction and an injected already-active caller-owned Read Committed
connection transaction. They assert the isolation code before connection
open for the ambient case and before any B4 command in both cases; the
already-open injected connection is fixture setup only. They prove no actor GUC,
actor-scoped read, attempt-lock call, authority lock, head lock, mutation, or
transition. Scratch-bypassing each early gate must make its own named test red;
the SQL isolation guard cannot satisfy either test because the ambient/caller
transaction already has the otherwise-valid isolation.

The attempt path also shares the section-3.3 final admission rule: after its last
ambient/current-transaction check, no application-level await, callback,
resolver, or database command may occur before the open/begin invocation.
Previously-opened-now-closed scoped connections are positive controls; a
currently open connection remains an early rejection.

### M6 — Runtime-only role and ACL

`M6_runtime_only_login_can_use_all_b4_entries`

`M6_function_acl_manifest_is_exact_and_grantor_aware`

`M6_table_nonowner_acl_manifest_is_empty`

`M6_column_nonowner_acl_manifest_is_empty`

`M6_default_function_acl_grantee_aborts_apply`

`M6_alternate_function_acl_grantor_aborts_apply`

`M6_unrelated_table_grantee_fails_readiness`

`M6_unrelated_column_grants_fail_readiness`

`M6_column_alternate_grantor_or_grant_option_fails_readiness`

`M6_internal_helper_public_execute_fails_readiness`

`M6_actor_context_missing_invalid_mismatch_and_valid_are_distinct`

`M6_direct_claim_rejects_invalid_idempotency_grammar`

A dedicated LOGIN INHERIT principal holds only `tagekyc_runtime`, with membership
`ADMIN=false`, `INHERIT=true`, `SET=false`; `session_user == current_user`.
Every required entry works, including a real bind and outer commit. The
five-table normalized explicit non-owner table and column ACL sets are empty,
executable direct SQL fails with exact `42501`, and every ungranted helper fails.
Temporary roles are removed in `finally`.

The function manifest asserts every runtime entry has exactly the deployer-
granted, non-grantable runtime EXECUTE row and every internal function has no
non-owner row. Mutation cases independently prove readiness or migration apply
goes red for:

- a default EXECUTE grant to an unrelated role before B4 function creation;
- an alternate grantor adding a second runtime EXECUTE row while the expected
  row remains;
- unrelated-role `SELECT`, `INSERT`, and `UPDATE` on each B4 table;
- unrelated-role `SELECT(SubjectRef)`, runtime `SELECT(JobId)`, column-level
  `UPDATE(CurrentState)`, and column-level `REFERENCES(PermitId)`;
- an alternate grantor or grant option on one B4 column while all table ACLs
  remain clean;
- PUBLIC EXECUTE on an internal helper.

The direct runtime claim matrix uses a valid actor context and covers empty,
whitespace, newline, non-ASCII, disallowed punctuation, and 257-character
idempotency keys. It asserts exact
`P0001 / RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED` and zero B4 rows. One- and
256-character allowlisted values are positive boundary controls.

The migration-time cases assert exact
`TIP88B4_FUNCTION_ACL_INVALID` or `TIP88B4_TABLE_ACL_INVALID`; post-apply
readiness cases assert the corresponding stable 503 code. No test repairs drift
by revoking the unexpected role.

Actor-context cases independently cover unset, blank, malformed, zero UUID,
valid-context principal mismatch, and a valid matching control. They assert
exact SQLSTATE `P0001` and the three distinct landed/B4 messages.

### M7 — Lease, renewal, retry, and reclaim

`M7_lease_tokens_are_monotonic_and_attempts_are_unique`

`M7_renewal_keeps_fence_and_increments_revision`

`M7_retryable_release_and_reclaim_create_new_attempt_and_fence`

`M7_acquire_and_reclaim_cap_lease_at_frozen_deadlines`

`M7_mode_specific_retry_and_reclaim_fail_closed`

`M7_blocked_acquire_rechecks_physical_time_after_head_lock`

All timing uses database time. Tests assert exact head plus append-only evidence,
including `ResultingLeaseExpiresAt`, not only exceptions. Boundary cases prove
that acquisition and reclaim at/after the deadline insert no attempt and
atomically expire the job. Mode tests allow retry only for
`EncryptedExportPacket`, reject no-retain reacquisition, and reject retained-vault
retry without a separately ratified pair. They also invoke the failure command:
both forbidden cases must append terminal rather than retryable evidence and
leave `TerminalFailed / MODE_RETRY_NOT_AUTHORIZED`, never ownerless
`Assembling`. The blocked-acquire test begins before expiry, waits on the locked
operational head, resumes after the frozen deadline, and proves zero new attempt
plus one exact `Expired / PERMIT_OR_JOB_EXPIRED` head/transition mutation.
Every successful acquisition/reclaim also asserts the persisted attempt satisfies
the exact phase, non-negative ordinal, positive-fence, and increasing-lease-time
CHECK definitions from section 4.3.

### M8 — Stale worker mutation proof

`M8_expired_unreclaimed_worker_cannot_renew_or_record_failure`

`M8_stale_worker_cannot_mutate_after_reclaim`

`M8_head_first_terminalization_does_not_grant_stale_worker_authority`

Before reclaim, an old worker whose lease has expired but whose attempt/fence
still match must receive `LeaseNotHeld` from both renew and record-failure, with
zero head/transition mutation. Scratch removal of the record-failure live-lease
predicate must make the first named test red. After reclaim, the same worker is
fence-stale; scratch removal of the fencing predicate must make the second test
red. Head-first authority/deadline/cancellation terminalization must still
accept the exact section-8 provenance tuple under repository orchestration,
without making either worker-owned command succeed. Restoration must return
every test to green.

### M9 — Head/ledger atomicity and crash matrix

`M9_every_head_mutation_has_same_transaction_transition`

`M9_crash_after_lease_expiry_reclaims_same_job_without_duplicate_consumption`

Rollback at every command boundary leaves neither orphan ledger evidence nor a
head mutation without evidence. The same `JobId` survives crash/reclaim for the
mode-permitted fixture case. A test-only fake delivery side effect demonstrates
that an already-started external effect can occur after lease loss while the
stale worker's authoritative DB mutation is rejected.

### M10 — Guard mutation proof

`M10_identity_attempt_transition_and_head_guards_bite`

`M10_runtime_bind_forces_named_class_constraint_and_restores_deferred`

`M10_two_valid_binds_in_one_transaction_preserve_constraint_mode`

`M10_named_class_constraint_body_mutations_go_red`

`M10_attempt_phase_check_rejects_non_assembling`

`M10_attempt_ordinal_check_rejects_negative`

`M10_attempt_fence_check_rejects_nonpositive`

`M10_attempt_lease_time_check_rejects_nonincreasing`

`M10_idempotency_key_function_and_table_guards_bite`

Temporarily remove, one at a time, the append-only guard, child
same-transaction guard, head mutation guard, non-empty class guard, and
transition-revision contiguity check. A named assertion must go red for each
mutation. Separate cross-job attempt/head/transition inserts prove the composite
FKs reject mixed-job tuples; dropping each composite FK makes the corresponding
named assertion red. Every row-local head and EventType CHECK branch has a
positive control plus an over/under-population negative; removing its exact CHECK
clause makes the named test red. Cross-row current-attempt equality, fresh
AttemptId, strictly higher fencing token, revision contiguity, and equality
between the resulting transition and locked head are proved separately. Their
scratch mutations remove the exact command-function predicate or command-specific
guard/trigger clause that owns the invariant; each corresponding named assertion
must go red. No test attributes a cross-row invariant to a row-local CHECK.

Each attempt domain test has a valid positive row plus its exact invalid case:
`Phase='Delivery'`, ordinal `-1`, fence `0`, and initial lease expiry equal to or
before acquisition. Scratch-removing each exact CHECK clause must make its named
discriminating test red; ordinal contiguity and fence monotonicity remain separate
cross-row command tests.

The idempotency guard proof is two independent mutations. Removing claim-function
validation must make a valid-actor direct call with an in-range disallowed
character return a database constraint error instead of the exact pinned
`P0001`, making the function test red. Removing the table CHECK must make an
authorized scratch direct insert with an in-range invalid key succeed, making the
table-backstop test red. Both sources are restored byte-identically.

The named-constraint controls use a dedicated runtime-only LOGIN and real outer
commit. One case starts with
`tagekyc.tr_b4_job_identity_has_classes IMMEDIATE`; the bind must normalize it,
validate while still inside the SD function, and return with the constraint
`DEFERRED`. A second case performs two valid binds in one outer transaction and
commits both. Scratch-removing the `IMMEDIATE` validation must make the
runtime-commit/non-empty assertion red; scratch-removing the final `DEFERRED`
restoration must make the exact body/readiness-manifest assertion red. The
caller-entered-immediate and two-bind cases remain positive behavioral controls.
The source is restored byte-identically after each mutation.
`SET CONSTRAINTS ALL` is never accepted.

### M11 — Terminal and deadline semantics

`M11_terminal_jobs_cannot_reenter_or_reacquire`

`M11_expiry_uses_database_time_and_never_extends_deadlines`

`M11_new_bind_graph_invalid_rolls_back_without_job`

`M11_bound_revalidation_and_safe_graph_failures_terminalize_with_exact_evidence`

`M11_each_command_obeys_expiry_and_authority_precedence`

Terminal state clears the lease, retains the latest attempt reference, appends
one transition, and blocks every later mutation except bounded actor-scoped
read. The bound-failure test independently asserts the exact typed repository result,
terminal reason, resulting revision/fence/lease tuple, and single matching
transition for both authority revalidation and safely terminalizable
authoritative-graph failure. A separate malformed internal B4 head mutation
proves that an unsafe terminal tuple instead raises
`RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE` and commits no partial mutation. The
precedence test covers claim, acquire/reclaim, renew, attempt failure, and
explicit terminalization, including coincident absolute expiry and
consent/fulfillment expiry; it asserts the exact SQL outcome, repository code,
head, transition, and zero-attempt behavior for every branch. Renew and attempt
failure at an expired lease assert `LeaseNotHeld` and zero mutation even when an
absolute deadline has also crossed; a separate head-first orchestration call
then records the exact `Expired` terminal evidence.

### M12 — Migration/readiness equivalence

`M12_apply_rollback_reapply_preserves_pre_b4_catalog_and_acls`

`M12_readiness_returns_exact_code_for_each_manifest_drift`

Apply, rollback, and reapply preserve the complete pre-B4 function/ACL/role
snapshot, E3 readiness remains healthy, and EF reports no pending model changes.
Every readiness branch has a positive control and a mutation that reaches its
exact 503 code. Separate isolated-cluster apply mutations configure an
unrelated default function-EXECUTE grantee before B4 apply, or scratch-inject an
alternate-grantor runtime EXECUTE row after function creation but before the
migration ACL assertion; each must abort with exact
`TIP88B4_FUNCTION_ACL_INVALID` before the B4 history row commits. Equivalent
table-default/unrelated-grantee drift, plus a scratch column grant injected after
table creation but before the migration ACL assertion, must abort with
`TIP88B4_TABLE_ACL_INVALID`. Post-apply mutations independently add unrelated
table privileges, each listed column-level privilege shape, alternate-grantor or
grant-option column drift, and internal-helper PUBLIC EXECUTE; readiness must
return the corresponding exact stable 503 code. Every scratch role, default ACL,
grant, and migration-body mutation is removed or restored in `finally`.

### M13 — Inertness

`M13_b4_contains_no_raw_byte_package_or_delivery_surface`

No B4 table column or application type exposes payload/bytes/blob/content/raw
artifact/package/delivery data. The landed B3 inertness assertion remains
stronger or byte-semantically unchanged.

## 13. Future build shape

The later build dispatch may authorize only:

- B4 domain records/enums/errors and repository port;
- B4 EF rows/configuration/repository/readiness/DI;
- one EF migration with suffix `Tip88B4RawExportJobFoundation` and its Designer;
- ModelSnapshot changes generated solely from the five B4 tables;
- B4 unit, contract, architecture, and integration tests;
- minimal readiness/runbook registration;
- this planning brief, the TIP index, and a B4 closeout.

Files belonging to B1/B2/B3/E3 production code are not part of the default B4
allowlist. If the pre-build census finds a missing landed seam or deployer
capability, implementation must stop for a reviewed extension rather than edit a
landed slice opportunistically.

## 14. Review attacks

The independent reviewer must try to prove:

1. B4 mutates or semantically consumes the B3 permit instead of deriving
   binding;
2. a same permit can create two identities during commit/rollback races;
3. idempotency conflict reveals the existing job;
4. API-key rotation incorrectly changes the bind fingerprint;
5. mode is caller-supplied, omitted, or read through an unauthorized E3 change;
6. client/session owner and recipient are conflated;
7. stale fences can match again after reset/reuse;
8. an attempt or transition needs an UPDATE despite being declared append-only;
9. a head mutation can commit without evidence;
10. a lease or retry substitutes for current authorization;
11. DB locks cross fixture/non-DB work;
12. a B4 function can enter a C1/C2/C3 state;
13. runtime has direct table access or a broad generic transition capability;
14. app time, sentinel expiry, mutable deadline, or uncapped renewal exists;
15. tests stay green when a load-bearing guard or lock is removed;
16. wording claims raw assembly, delivery, exactly-once network behavior,
    multi-tenancy, or actor authentication that B4 does not provide.
17. deferred class completeness escapes the SD function and fails at runtime
    commit because the caller has no table privilege;
18. default ACL, unrelated grantee, or alternate grantor creates an unmanifested
    B4 capability;
19. a finite consent/fulfillment bound expires while new bind waits on the
    permit row but the inserted aggregate still commits;
20. missing/blank, malformed/zero, and valid-mismatched actor contexts collapse
    into one error;
21. an expired but unreclaimed worker records failure or uses orchestration
    terminalization as lease authority.
22. a column-level grant bypasses a clean table `relacl` manifest;
23. an attempt stores an unsupported phase, negative ordinal, non-positive fence,
    or non-increasing initial lease interval;
24. a direct runtime claim bypasses the C# idempotency-key grammar or returns an
    unstable storage/constraint error.
25. bind or attempt orchestration inherits Repeatable Read/Serializable, retains
    a pre-wait snapshot, or admits a direct non-Read-Committed opening seam.
26. a matching-Read-Committed ambient/active transaction reaches database work,
    or isolation failure outranks invalid-command preflight.
27. B4 assigns or normalizes a connection string, changes provider/global
    persistence configuration, or relies on restoring provider-redacted
    credential text.
28. B4 rejects a previously-opened-now-closed scoped connection, accepts a
    currently open connection, or creates a second context/connection/data source.
29. an application-level await, callback, resolver, or database command creates
    a gap between the final ambient/current-transaction check and the
    open/begin invocation, or exit cleanup leaves a connection or transaction
    active.

## 15. Review record

### Round 1 — v0.1 reviewed, v0.2 patched

The independent reviewer read all 23 TIP-88 Markdown files, TIP-82R, the debt
registry, the hospital PostgreSQL runbook, and the landed B1/B2/B3/E3 source
contracts before reporting 2 HIGH, 6 MEDIUM, and 2 LOW findings. Version 0.2
closes them by:

- returning a committed equal-fingerprint replay before mutable revalidation;
- making retry/reclaim mode-aware;
- recording resulting lease expiry in immutable transitions and capping every
  acquisition/reclaim at frozen deadlines;
- pinning cross-job relational integrity;
- ratifying exact runtime function identities, return surfaces, outcomes,
  dependencies, and guard tokens;
- restoring the exact actor/client/recipient equality chain;
- adding discriminating B1 lock-race tests;
- separating voluntary reacquisition from expired-lease reclaim;
- retaining the ratified test-only fake-delivery side-effect proof without
  persisting a delivery claim.

### Round 2 — v0.2 reviewed, v0.3 patched

Round 2 re-read the full brief and found 2 HIGH and 3 MEDIUM residuals. Version
0.3 closes them by:

- requiring the claim function itself to verify exact authoritative policy Mode
  and closure;
- preventing retry-forbidden modes from emitting retryable evidence or remaining
  ownerless in `Assembling`;
- defining nullable terminalization tuples for `Claimed`, released
  `Assembling`, and actively/expired-leased `Assembling`;
- pinning the complete SQL-outcome-to-repository mapping and closed
  failure/reason token set;
- defining exact operational-head and per-EventType transition row-shape truth
  tables plus discriminating mutation tests.

### Round 3 — v0.3 reviewed, v0.4 patched

Round 3 re-read the full brief and found 0 HIGH, 2 MEDIUM, and 0 LOW residuals.
Version 0.4 closes them by:

- distinguishing rollback-only new-bind authority failure from committed
  already-bound terminal evidence, including the safe authoritative-graph and
  unsafe internal-graph cases;
- mapping every committed terminalization to an exact typed result/code so no
  success code is dead or thrown before commit;
- limiting row CHECK claims to row-local shape and assigning cross-row attempt,
  fence, revision, and resulting-head invariants to their exact composite FK,
  command-function, and guard/trigger mechanisms;
- requiring mutation proof against the mechanism that actually owns each
  invariant.

### Round 4 — v0.4 reviewed, v0.5 patched

Round 4 re-read all 1,404 lines of v0.4 and found 2 HIGH, 2 MEDIUM, and 0 LOW
residuals. Version 0.5 closes them by:

- replacing the stale transaction-start clock with fresh post-lock/post-wait
  `clock_timestamp()` observations, one authoritative `v_now` per SQL mutation,
  and blocking-across-expiry proofs;
- making safely attributable authoritative-graph invalidity a non-aborting
  projection outcome, while keeping unsafe internal B4 corruption on the
  rollback-only `P0001` path;
- mapping new-bind graph invalidity to an exact zero-residue result;
- removing the last cross-row claim from the row-local transition CHECK; and
- pinning the actor-scoped reread required to translate `AlreadyTerminal` into a
  state-bearing repository result without widening the SQL mutation surface.

### Round 5 — v0.5 reviewed, convergence analysis, v0.6 patched

Round 5 re-read all 1,490 lines of v0.5 and found 1 HIGH, 3 MEDIUM, and 0 LOW
residuals. The mandatory convergence analysis classified them as drafter-owned
Round-4 patch regressions/incomplete closure, not reviewer invention: the clock
rule lacked an executable post-head-lock authority path, and the new outcome,
precedence, and sentinel contracts had not been propagated across every closed
surface.

Version 0.6 applies these corrective drafting rules:

- trace each invariant through lock order, function dependency/signature, SQL
  outcome, repository mapping, state/evidence, and mutation test;
- enumerate every new outcome on the SQL surface and mapping table;
- define explicit nullability for every branch of a sentinel/union;
- use a per-command precedence matrix instead of an unsupported universal rule;
- add an actor-scoped attempt-head lock entry and require the repository to retain
  that lock while reusing the landed session/B1/policy/B2 resolvers, so no head
  wait occurs after revalidation; and
- require the next reviewer to distinguish a contract defect from preference or
  future-slice scope.

### Round 6 — v0.6 reviewed, v0.7 patched

Round 6 found 0 HIGH, 1 MEDIUM, and 0 LOW residuals. Version 0.7 closes the final
claim-race reachability gap by:

- pinning the exact immutable B3 permit-row `FOR UPDATE` serialization point
  before the claim function's final identity re-read, physical clock, and insert;
- correcting the blocked repository bind test to expect pre-claim authority
  rejection; and
- adding a separate direct-function permit-lock race that must reach SQL
  `AuthorityNotEffective` and go red if the lock or post-lock clock moves.

### Round 7 — v0.7 reviewed, v0.8 patched

Round 7 found 0 HIGH, 1 MEDIUM, and 0 LOW residuals. Version 0.8 removes the
direct-call lock inversion by:

- deriving the authoritative session inside claim and taking the landed session
  lock before the B3 permit serialization lock;
- adding that session-lock seam to the exact claim dependency/readiness
  manifest; and
- adding a production-bind-versus-direct-claim race that rejects SQLSTATE
  `40P01` and goes red when the claim-side session lock is removed.

### Round 8 — v0.8 reviewed, clean

Round 8 re-read all 1,697 lines of v0.8 and returned
`PASS — 0 HIGH / 0 MEDIUM / 0 LOW`. It confirmed the canonical
session → permit → identity/FK claim order, the head-first attempt path, every
closed lock/dependency/signature/outcome/mapping/evidence/test trace, ACL and
readiness posture, wording/version synchronization, and the no-build boundary.

Version 0.9 changes only the closeout metadata and this review record. Status
`READY_FOR_HOMEOWNER_RATIFICATION` means the planning brief is eligible for the
Homeowner's governance decision. It is not ratified, dispatch-ready,
`READY_FOR_BUILD`, or authorization to implement, migrate, commit, push, merge,
or deploy.

### Round 9 — v0.9 reviewed, v0.10 patched, final verification pending

Round 9 reported 3 HIGH and 2 MEDIUM findings. Source verification accepted all
five:

- the B4 no-direct-table-privilege posture requires the exact named deferred
  class-completeness constraint to run `DEFERRED → IMMEDIATE → DEFERRED` inside
  the SD claim function;
- function and table ACLs require exact normalized full-grantee and grantor
  manifests plus migration-time abort gates;
- a `NewJob` requires a fresh post-claim finite B1/B2 bound check because time
  can pass while claim waits on the B3 permit row;
- actor context must preserve the landed missing/invalid distinction before the
  B4 passed-principal mismatch code;
- renew and record-failure are worker-owned live-lease commands, while
  head-first terminalization is repository orchestration.

Version 0.10 propagates those corrections through persistence, ordered bind,
function/readiness/ACL contracts, stable outcomes, precedence, and dedicated
mutation gates. Its status is `READY_FOR_FINAL_VERIFICATION`, not ratified,
dispatch-ready, `READY_FOR_BUILD`, or authorization to implement, migrate,
commit, push, merge, or deploy.

### Round 10 — v0.10 reviewed, v0.11 patched, final verification pending

Round 10 confirmed the five Round-9 corrections and reported 1 HIGH and 2 MEDIUM
residuals. Source and PostgreSQL-16 catalog verification accepted all three:

- table `relacl` cannot reveal column grants stored in `pg_attribute.attacl`, so
  the exact no-direct-access manifest now covers both table and column ACL rows,
  preserves grantor/grantee/grantability, and has migration/readiness mutations;
- immutable attempt evidence now has exact schema-v1 row-local CHECKs for
  `Assembling`, non-negative ordinal, positive fence, and increasing initial
  lease time, without misattributing cross-row invariants to those CHECKs;
- the canonical idempotency-key grammar is enforced independently by C#
  preflight, the runtime-granted claim function, and a table CHECK, with
  direct-login boundary and mutation-red proofs.

Version 0.11 propagates those corrections through persistence, claim precedence,
ACL/readiness manifests, migration abort gates, named tests, and review attacks.
Its status remains `READY_FOR_FINAL_VERIFICATION`, not ratified, dispatch-ready,
`READY_FOR_BUILD`, or authorization to implement, migrate, commit, push, merge,
or deploy.

### Round 11 — v0.11 reviewed, clean

The independent reviewer read the root instructions, the complete review
playbook, all 2,061 lines of v0.11 byte-to-EOF, and the synchronized TIP-index
entries before returning `PASS — 0 HIGH / 0 MEDIUM / 0 LOW`. It confirmed:

- the Round-10 table plus column ACL manifest, migration abort, readiness code,
  and discriminating mutation traces;
- the exact attempt row-local CHECKs without cross-row overclaim;
- the C# preflight, SD claim, table-backstop, error-precedence, boundary, and
  mutation traces for idempotency grammar;
- all previously closed claim ordering, physical-time, authority, replay,
  deferred-constraint, CAS/fencing, evidence, ACL/readiness, and inert-scope
  contracts; and
- version/status/index synchronization with no stale current wording.

Version 0.12 changes only closeout metadata and this review record. Status
`READY_FOR_HOMEOWNER_RATIFICATION` means the planning brief is eligible for the
Homeowner's governance decision. It is not ratified, dispatch-ready,
`READY_FOR_BUILD`, or authorization to implement, migrate, commit, push, merge,
or deploy.

### Round 12 — v0.12 reviewed, v0.13 patched, final verification pending

Round 12 confirmed the three Round-10 corrections and reported one HIGH
transaction-isolation residual. PostgreSQL-16 and Npgsql source verification
accepted the invariant but corrected the proposed mutation proof:

- Read Committed supplies a new snapshot per statement after a lock wait, while
  Repeatable Read and Serializable can retain an earlier transaction snapshot;
- fresh `clock_timestamp()` therefore cannot make authority reads fresh by
  itself;
- bind and attempt orchestration now own explicit Read Committed transactions,
  reject caller/ambient transactions, and the claim and attempt-lock SQL seams
  reject any other isolation before lookup or lock;
- the isolation failure has one distinct stable operational code and exact
  precedence, mapping, zero-residue, race, and mutation gates; and
- because Npgsql's parameterless transaction API currently defaults to Read
  Committed, an architecture assertion pins the explicit overload so deleting
  only the enum argument cannot masquerade as a meaningful red mutation.

Version 0.13 propagates the correction through repository transaction ownership,
ordered bind/attempt paths, MVCC/physical-time semantics, SQL entry contracts,
stable outcomes, concurrency tests, and review attacks. Its status is
`READY_FOR_FINAL_VERIFICATION`, not ratified, dispatch-ready,
`READY_FOR_BUILD`, or authorization to implement, migrate, commit, push, merge,
or deploy.

### Round 13 — v0.13 reviewed, v0.14 patched, final verification pending

Round 13 returned 0 HIGH, 2 MEDIUM, and 0 LOW. It confirmed the Round-12
isolation invariant but found two proof/precedence residuals introduced or left
open by that patch:

- one precedence row placed transaction ownership before the established
  in-memory request preflight; and
- Serializable ambient tests could receive the expected code from the later SQL
  guard, failing to prove that bind and attempt repositories reject ambient or
  active caller transactions before database commands.

Version 0.14 restores preflight-first ordering and adds independent bind and
attempt gates using matching Read Committed ambient and already-active
transactions. Those fixtures make the SQL isolation guard non-discriminating, so
zero B4 connection/command probes and scratch-bypassed early gates must carry the
proof. Its status remains `READY_FOR_FINAL_VERIFICATION`, not ratified,
dispatch-ready, `READY_FOR_BUILD`, or authorization to implement, migrate,
commit, push, merge, or deploy.

### Round 14 — v0.14 reviewed, clean

The independent reviewer read all 2,288 lines of v0.14 byte-to-EOF and returned
`PASS — 0 HIGH / 0 MEDIUM / 0 LOW`. It confirmed:

- preflight-first precedence, including the invalid-command plus ambient overlap;
- discriminating matching-Read-Committed ambient and already-active connection
  gates on both bind and attempt paths, with zero B4 command probes and
  independently red early-gate mutations;
- the non-vacuous explicit-overload assertion despite Npgsql's current matching
  default;
- the complete Round-12 Read Committed, per-statement snapshot, SQL guard, and
  post-head-wait visibility closure; and
- no regression in replay, lock order, physical time, authority, fencing/CAS,
  immutable evidence, ACL/readiness, or inert scope.

Version 0.15 changes only closeout metadata and this review record. Status
`READY_FOR_HOMEOWNER_RATIFICATION` means the planning brief is eligible for the
Homeowner's governance decision. It is not ratified, dispatch-ready,
`READY_FOR_BUILD`, or authorization to implement, migrate, commit, push, merge,
or deploy.

## 16. Homeowner ratification

On 2026-07-26, the Homeowner ratified TIP-88B4 Planning Brief v0.15 as the
authoritative scope and design contract for the Permit-to-Job Consumption
Foundation.

This decision authorizes preparation of the implementation build brief only. It
does not authorize implementation, migration creation or execution, model or
snapshot change, test/code production work, commit, push, merge, deployment,
Raw BIO access, package/encryption/delivery capability, or production activation.
The future build brief must preserve every v0.15 invariant and must define its own
exact baseline, allowlist, STOP conditions, verification gates, mutation proofs,
and report format before any build can be dispatched.

Version 0.16 changes only the header metadata and this ratification record. The
substantive authoritative design contract remains the independently reviewed
v0.15 content; this record does not silently convert the planning brief into a
build dispatch or `READY_FOR_BUILD` authorization.

### 16.1 Homeowner ratification of coordinated Amendments A–C

On 2026-07-26, the Homeowner ratified coordinated Amendments A–C from the
independently reviewed TIP-88B4 Implementation Build Brief v0.12:

- Amendment A closes the committed-job `GraphInvalid` bind result and direct
  head-first terminalization contract;
- Amendment B makes fresh explicit Read Committed transaction ownership apply to
  every repository method and records the exact direct-entry admission paths; and
- Amendment C pins bounded lease configuration and its fail-closed precedence.

Version 0.17 synchronizes those ratified semantics into sections 3.2, 3.3, 5.1,
9.1a, and 10.2. This ratification authorizes synchronization review and continued
build-brief preparation only. It does not authorize implementation, migration
creation or execution, model or snapshot change, test/code production work,
commit, push, merge, deployment, Raw BIO access, package/encryption/delivery
capability, or production activation.

### 16.2 Round-1 synchronization correction

The first independent byte-to-EOF synchronization review found that section
10.1's exhaustive mapping table had not carried through all three ratified
effects. Version 0.18 corrects that table so:

- transaction-ownership rejection covers every preflight-valid repository method;
- the safely identified committed-job `GraphInvalid` path returns the exact
  committed/current `Terminal(JobId, RawExportJobTerminalizeResult)` or rolls back
  on stale-tuple concurrency; and
- invalid direct acquire/renew lease bounds map without lookup or existence
  disclosure to `PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID`.

This is a synchronization correction of the already-ratified A–C semantics, not a
new design amendment or build dispatch. Independent correction verification is
required. No implementation, migration, code/test, commit, push, merge,
deployment, Raw BIO, or production-activation authority is created.

### 16.3 Round-2 synchronization correction

The second independent byte-to-EOF review confirmed every Round-1 finding closed
and found one remaining MEDIUM omission: section 10's stable-code manifest did
not list the lease-configuration code required by its exhaustive result mapping.
Version 0.19 adds `PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` to that manifest and
pins its shared fail-closed application meaning without exposing the internal SQL
message.

This is a synchronization correction of the already-ratified Amendment C
semantics. Independent correction verification is required. No implementation,
migration, code/test, commit, push, merge, deployment, Raw BIO, or
production-activation authority is created.

### 16.4 Synchronization verification closeout

The fourth independent byte-to-EOF convergence review returned
**PASS — 0 HIGH / 0 MEDIUM / 0 LOW**. It confirmed:

- all ratified A–C semantics, mappings, stable-code/readiness contracts,
  transaction ownership, terminalization, and lease precedence align;
- operative status, gates, authority, and version references are synchronized;
- historical proposal wording remains historical and no stale operative proposal
  remains; and
- `src/` and `tests/` are unchanged from the candidate source baseline.

Version 0.20 changes only header metadata and this closeout record. The
substantive authoritative amended contract remains v0.19. This closeout does not
authorize implementation, migration, code/test work, commit, push, merge,
deployment, Raw BIO access, or production activation.

### 16.5 Homeowner-ratified connection-lifecycle Amendment D

On 2026-07-27, the Homeowner adopted Amendment D after the pinned Npgsql 8.0.3
provider proved that the former per-call connection-string mutation/restoration
design was not safe for the supported previously-opened-now-closed scoped
connection topology. Amendment D removes that design and ratifies:

- preflight-first ambient, EF-current, provider-transaction, and currently-open
  connection rejection;
- a final no-gap ambient/current-transaction check immediately before the
  open/begin invocation;
- one fresh explicit Read Committed transaction per repository method;
- one unchanged scoped DbContext/connection/transaction across B1, policy, B2,
  and B4 where required;
- no B4 connection-string assignment or persistence/provider/global
  configuration mutation;
- non-cancellable closed/transaction-free cleanup on every exit; and
- positive support for a scoped connection that was previously opened and is
  closed at B4 method entry.

This is a stricter no-mutation contract, not a relaxation of the superseded
restore-after-mutation design. Version 0.21 synchronizes Amendment D into the
operative transaction, SQL-entry, M3/M5, mutation, and review-attack surfaces.
Implementation remains stopped. This docs preparation does not authorize
implementation resume, migration execution, commit, push, merge, PR, deployment,
Raw BIO access, or production activation.
