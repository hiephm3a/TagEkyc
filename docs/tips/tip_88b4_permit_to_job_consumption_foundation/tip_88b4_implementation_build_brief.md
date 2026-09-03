# TIP-88B4 — Permit-to-Job Consumption Foundation — Implementation Build Brief

**Version:** 0.19
**Status:** IMPLEMENTED — D1/D2 EVIDENCE CORRECTION SYNCHRONIZED — DOCS-ONLY COMMIT PENDING
**Date:** 2026-07-27
**Repository:** `D:\Task\Remote Signing\TagEkyc`
**Candidate source baseline:** `bf90d5453f2cf8fb45009ccc2dcdb42c335a4711`
**D1/D2 docs-amendment baseline:** `d09929df3d5f5eacf1968480c9fcbb6c5b13a200`
**Ratified contract:** `tip_88b4_planning_brief.md` v0.22, including coordinated
Amendments A–D and the D1/D2 evidence correction
**Authority:** docs-only D1/D2 evidence correction; implementation is settled;
commit and closeout are not authorized

This docs-only amendment does not authorize migration creation or execution,
model/snapshot change, test or production-code work, commit, push, merge,
deployment, Raw BIO access, package creation, encryption, delivery, or production
activation.

If this brief conflicts with the ratified planning contract, the planning
contract wins. The implementation is already landed; the docs-only amendment
commit and B4 closeout require separate Homeowner instructions.

## 1. TIP Analytical Summary / Intent Ledger

### Intent

Implement the metadata-only bridge from one immutable B3 authorization permit to
one logical export job. The slice must prove atomic permit binding, immutable
identity freeze, idempotent replay, append-only attempt/transition evidence,
mutable fenced CAS head semantics, current-authority revalidation, crash/reclaim
behavior, and exact runtime/database privilege boundaries without reading or
storing raw material.

### Expected Outcome

After a separately authorized build:

- one B3 permit has at most one B4 job identity;
- a committed equal-fingerprint retry returns that same job;
- a conflicting retry reveals no job identity;
- every head mutation has exactly one same-transaction transition;
- every acquisition/reclaim creates a fresh attempt and monotonically increasing
  fence;
- stale workers cannot renew, release, terminalize, or append authoritative
  evidence;
- every `IRawExportJobRepository` method owns one fresh explicit Read Committed
  transaction and observes the committed state required by its command;
- runtime reaches B4 only through the eight ratified `SECURITY DEFINER`
  functions and has no table or column privilege;
- the five B4 tables contain metadata only; and
- no raw source, package, crypto, delivery, receipt, HTTP, consumer,
  hosted/background worker, fixture-worker production registration,
  raw-processing worker, queue consumer, or non-database execution capability
  exists.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Permit and job remain separate aggregates | B3 permit is immutable authorization evidence | B4 inserts a separate identity with `UNIQUE(PermitId)` | No consumed flag or permit update |
| Immutable identity + append-only evidence + mutable CAS head | Separates audit truth from operational coordination | Five exact tables | Does not make DB state an external exactly-once guarantee |
| Every `IRawExportJobRepository` method owns one fresh explicit Read Committed transaction | Post-wait authority, winner visibility, and transaction-local actor context require method-scoped admission | Repository enforces all six methods; claim and attempt-lock retain the two direct-runtime SQL guards | Physical time alone is not visibility |
| Runtime function boundary | Preserves E3 no-direct-table posture | Eight runtime entries, internal guards, exact ACLs | Not authentication against a compromised backend |
| B4 enters only `Claimed`, `Assembling`, and terminal states | Later phases belong to C1–C3 | Future state names may exist in CHECK only | No assembly, protection, or delivery |
| Retry is mode-aware | No-retain and unapproved retained-vault modes cannot claim safe replay | Fixture-positive retry uses `EncryptedExportPacket` | No raw-byte retry authorization |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Update B3 permit to Consumed | Rejected | Violates immutable permit evidence | None |
| Generic transition function | Rejected | Would let B4 enter later-slice states | C1–C3 add phase-specific functions |
| Raw resolver or vault | Deferred | B4 is metadata-only | TIP-88C1 |
| Package, manifest, signing, encryption | Deferred | Requires secure source and package contract | TIP-88C1/C2 |
| HTTP status/download API | Deferred | Delivery sees only protected sealed packages | TIP-88C3 |
| SignFlow transaction/session fields | Rejected from core | SignFlow is a separate consumer | Consumer integration slice |
| Shared-hospital tenant model | Deferred and out of scope | Current topology is single tenant per deployment | Separate governance/design decision |
| Exactly-once network delivery | Rejected claim | Indeterminate network outcomes remain possible | C3 owns `DeliveryOutcomeUnknown` |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| `DEBT-E3-A` compromised-backend actor context | Preserve landed boundary | No widening or false authentication claim | Future actor-authentication architecture |
| Raw source absent | No action in B4 | Job remains metadata-only | C1 cannot enable resolver without sealed assembly |
| Recipient key/custody absent | No action in B4 | Frozen recipient only | C2 trusted key lifecycle |
| Delivery receipt/reconciliation absent | No action in B4 | No Delivered claim | C3/C4 |
| Retained-vault retry pair unratified | Fail closed | No retry for that mode | Separate mode × controller decision |

### Non-Claims

This TIP does not prove or provide Raw BIO availability, raw-byte access,
artifact resolution, package completeness, encryption, signature, recipient key
trust, delivery, receipt, exactly-once external work, production activation,
legal sufficiency, multi-tenant isolation, or resistance to a fully compromised
trusted backend.

### Implementation disposition

- **Implementation state:** Landed at
  `d09929df3d5f5eacf1968480c9fcbb6c5b13a200`.
- **Current authority:** D1/D2 documentation correction in the planning brief,
  this build brief, and the TIP index only.
- **Implementation surfaces:** Frozen and verify-only for this amendment.
- **Remaining gates:** controlled docs-only D1/D2 commit, followed by separately
  authorized B4 closeout.

## 2. Binding contract and implementation posture

The builder must read the complete ratified planning brief before editing. The
following sections are incorporated without reinterpretation:

- sections 2–3: B3/E3 grounding, application port, commands, and return surfaces;
- section 4: five-table schema and constraints;
- sections 5–8: bind, fingerprint, lease, fencing, revalidation, state ownership;
- section 9: exact SQL entry manifest, dependency/token map, ACLs, guards, and
  readiness;
- section 10: stable outcomes and precedence;
- section 12: M1–M13 gates; and
- sections 11/13/14: redlines, build shape, and review attacks.

No builder choice may weaken or rename a ratified database identifier, function
signature, result column, SQL outcome, stable application code, state/event/code
token, ACL tuple, guard context, constraint, or named test.

Private C# helper names may vary only where this brief does not pin a public or
catalog surface. Such variation must not create another application port,
repository, transaction owner, readiness validator, or SQL entry.

### 2.1 Ratified coordinated Amendments A–D

On 2026-07-26, the Homeowner ratified coordinated Amendments A–C. On 2026-07-27,
the Homeowner ratified connection-lifecycle Amendment D. Their authoritative,
self-contained wording is incorporated without reinterpretation in Planning
Brief v0.22:

- Amendment A: sections 3.2 and 5.1, covering the typed bind `Terminal` outcome
  and safely identified committed-job `GraphInvalid` race closure;
- Amendment B: sections 3.3 and 9.1a, covering fresh explicit Read Committed
  ownership for all six repository methods and their exact SQL-entry admission
  paths; and
- Amendment C: sections 10.1 and 10.2, covering bounded acquire/renew lease
  configuration, precedence, and result mapping.
- Amendment D: sections 3.3, 9.1a, M3, M5, and review attacks, removing per-call
  connection-string mutation while preserving stricter preflight, final no-gap
  admission, one fresh explicit Read Committed transaction, same-instance
  ownership, and closed/transaction-free cleanup.
- D1/D2 evidence correction: M3 evidence accounting and claim replay wording,
  without changing runtime semantics.

The builder must implement those planning sections directly. This section is a
ratification/incorporation record, not a second copy, replacement instruction,
or reinterpretation of the planning contract. Proposal and review history
remains in section 18 only.

The current synchronization authority is docs-only. It does not authorize
migration or code/test work, commit, push, merge, deployment, Raw BIO access, or
production activation.

## 3. Task 0 — final re-anchor before any authorized build

The current candidate source anchor is
`bf90d5453f2cf8fb45009ccc2dcdb42c335a4711`. At dispatch or resume, the builder
captures the exact runtime HEAD in `$dispatchedHead`; the brief itself never
embeds or replaces that value. The gate proves that `src/` and `tests/` remain
byte-equivalent to this candidate source anchor.

Before editing, the builder must:

1. capture and assert the dispatched full HEAD hash:

   ```powershell
   $dispatchedHead = git rev-parse HEAD
   if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($dispatchedHead)) {
     throw 'TIP88B4_DISPATCH_HEAD_INVALID'
   }
   ```

2. use that runtime variable to prove its complete commit delta from the
   candidate source anchor is exactly the three ratification documents:

   ```powershell
   $expectedRatificationPaths = @(
     'docs/tips/README.md'
     'docs/tips/tip_88b4_permit_to_job_consumption_foundation/tip_88b4_planning_brief.md'
     'docs/tips/tip_88b4_permit_to_job_consumption_foundation/tip_88b4_implementation_build_brief.md'
   )

   [string[]] $actualRatificationPaths = @(
      git diff --no-renames --name-only `
        bf90d5453f2cf8fb45009ccc2dcdb42c335a4711 `
        $dispatchedHead
   )

   if ($LASTEXITCODE -ne 0) {
     throw 'TIP88B4_DISPATCH_COMMIT_DIFF_FAILED'
   }

   [string[]] $expectedSorted = @(
     $expectedRatificationPaths | Sort-Object
   )
   [string[]] $actualSorted = @(
     $actualRatificationPaths | Sort-Object
   )

   if ($actualSorted.Count -ne $expectedSorted.Count) {
     throw 'TIP88B4_DISPATCH_COMMIT_SCOPE_INVALID'
   }

   for ($index = 0; $index -lt $expectedSorted.Count; $index++) {
     if (-not [string]::Equals(
       $expectedSorted[$index],
       $actualSorted[$index],
       [System.StringComparison]::Ordinal)) {
       throw 'TIP88B4_DISPATCH_COMMIT_SCOPE_INVALID'
     }
   }
   ```

   Any committed path outside this exact docs-only list, or any missing listed
   path, rename, invalid revision, or failed Git command is a STOP. Do not accept
   a mixed governance/source/build-graph commit;
3. prove the root build graph is byte-equivalent across commits and clean in
   staged, unstaged, and untracked state:

   ```powershell
   $buildGraphPaths = @(
     'TagEkyc.sln'
     'global.json'
     'Directory.Build.props'
     'Directory.Build.targets'
     'Directory.Packages.props'
     'NuGet.config'
     ':(glob)**/*.props'
     ':(glob)**/*.targets'
   )

    git diff --exit-code `
      bf90d5453f2cf8fb45009ccc2dcdb42c335a4711 `
      $dispatchedHead -- $buildGraphPaths

   git diff --exit-code -- $buildGraphPaths
   git diff --cached --exit-code -- $buildGraphPaths
   git status --porcelain=v1 --untracked-files=all -- $buildGraphPaths
   ```

   All diff commands must exit zero and status must emit no output. At the
   candidate anchor only `TagEkyc.sln` exists among the six named root files;
   creation of a currently absent named file or any `.props`/`.targets` file is
   drift and stops dispatch;
4. prove source/test equivalence by running:

   ```powershell
    git diff --exit-code `
      bf90d5453f2cf8fb45009ccc2dcdb42c335a4711 `
      $dispatchedHead -- src tests

   git diff --exit-code -- src tests
   git diff --cached --exit-code -- src tests
   git status --porcelain=v1 --untracked-files=all -- src tests
   ```

   All three diff commands must exit zero and the status command must emit no
   output. This proves commit-to-commit, unstaged, staged, and untracked
   source/test equivalence rather than only comparing the working tree to HEAD;
5. record the complete source/test blob/path manifests:

   ```powershell
   git ls-tree -r --full-tree `
     bf90d5453f2cf8fb45009ccc2dcdb42c335a4711 -- src tests

    git ls-tree -r --full-tree $dispatchedHead -- src tests
   ```

   Store both outputs in the builder report and require exact equality;
6. record `git status --short` and classify all unrelated dirty files;
7. record SHA-256 for:
   - `TagEkycDbContextModelSnapshot.cs`;
   - all B3 migrations;
   - the E3 migration;
   - `EfRawExportAuthorizationRepository.cs`;
   - `EfRawExportAuthorizationProjectionReader.cs`;
   - `RawExportAuthorizationReadinessValidator.cs`;
8. run `dotnet ef migrations has-pending-model-changes` for
   `TagEkycDbContext`; it must report no changes;
9. apply the current full migration chain to a fresh PostgreSQL 16 database;
10. prove E3 readiness is healthy before B4;
11. record the complete pre-B4 table/function/trigger/constraint/ACL/role catalog
   snapshot required by M12; and
12. verify `160000 <= server_version_num < 170000`.

STOP on commit-scope mismatch, root-build-graph drift, source/test mismatch or
dirt, pending-model drift, unapplicable migrations, non-PostgreSQL-16 fixture,
unhealthy E3 readiness, or a catalog state that cannot be captured/restored
exactly. Do not rebase, revert, regenerate landed migrations, or bless drift.

## 4. Exact C# surface

### 4.1 Domain

Create one file:

`src/TagEkyc.Domain/RawExportJob.cs`

It contains only B4 records, closed enums, commands/results, and
`RawExportJobException`. It reuses `AuthenticatedRawExportActor`,
`RawExportRawClass`, and `RawExportMode`; it does not duplicate them.

The public repository-only declarations are exact. The later builder must add
the following declaration manifest to `RawExportJob.cs`; changing a type name,
member name/order/type/nullability, enum value, or constructor surface is a
STOP/RRI rather than a private implementation choice:

```csharp
public enum RawExportJobState
{
    Claimed = 0,
    Assembling = 1,
    AssemblySealed = 2,
    Protecting = 3,
    PackageSealed = 4,
    ReadyForDelivery = 5,
    DeliveryInProgress = 6,
    DeliveryOutcomeUnknown = 7,
    Delivered = 8,
    ReconciliationExpired = 9,
    TerminalFailed = 10,
    Cancelled = 11,
    Expired = 12,
}

public enum RawExportJobAttemptPhase
{
    Assembling = 0,
}

public enum RawExportJobEventType
{
    JobBound = 0,
    LeaseAcquired = 1,
    LeaseRenewed = 2,
    AttemptFailedRetryable = 3,
    LeaseAcquiredAfterRetryableFailure = 4,
    LeaseReclaimed = 5,
    JobTerminalFailed = 6,
    JobCancelled = 7,
    JobExpired = 8,
}

public enum RawExportJobAttemptFailureCode
{
    ATTEMPT_EXECUTION_FAILED_RETRYABLE = 0,
}

public enum RawExportJobTerminalReasonCode
{
    AUTHORITY_REVALIDATION_FAILED = 0,
    ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE = 1,
    JOB_GRAPH_INVARIANT_FAILURE = 2,
    MODE_RETRY_NOT_AUTHORIZED = 3,
    PERMIT_OR_JOB_EXPIRED = 4,
    REQUEST_CANCELLED = 5,
}

public enum RawExportJobBindStatus
{
    NewJob = 0,
    ExistingMatch = 1,
    Terminal = 2,
}

public enum RawExportJobReadStatus
{
    Found = 0,
    NotFound = 1,
}

public enum RawExportJobLeaseStatus
{
    Acquired = 0,
    AcquiredAfterRetryableFailure = 1,
    Reclaimed = 2,
    NotFound = 3,
    AlreadyTerminal = 4,
    TerminalFailed = 5,
    Expired = 6,
}

public enum RawExportJobRenewStatus
{
    Renewed = 0,
}

public enum RawExportJobAttemptFailureStatus
{
    Recorded = 0,
    TerminalFailed = 1,
}

public enum RawExportJobTerminalizeStatus
{
    Terminalized = 0,
    AlreadyTerminal = 1,
    Expired = 2,
}

public sealed record BindRawExportJobCommand(
    AuthenticatedRawExportActor Actor,
    Guid PermitId,
    string IdempotencyKey);

public sealed record ReadRawExportJobCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId);

public sealed record AcquireOrReclaimRawExportJobLeaseCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid LeaseOwnerId);

public sealed record RenewRawExportJobLeaseCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid AttemptId,
    Guid LeaseOwnerId);

public sealed record RecordRawExportJobAttemptFailureCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid AttemptId,
    Guid LeaseOwnerId,
    RawExportJobAttemptFailureCode FailureCode);

public sealed record TerminalizeRawExportJobCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid? AttemptId,
    Guid? LeaseOwnerId,
    RawExportJobState TerminalState,
    RawExportJobTerminalReasonCode ReasonCode);

public sealed record RawExportJobIdentityView(
    Guid JobId,
    Guid PermitId,
    Guid AuthorizationDecisionId,
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid CreatedByApiKeyId,
    Guid VerificationSessionId,
    string SubjectRef,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    RawExportMode ExportMode,
    DateTimeOffset PermitExpiresAt,
    DateTimeOffset JobExpiresAt,
    int SchemaVersion,
    DateTimeOffset CreatedAt);

public sealed record RawExportJobClassView(
    int Ordinal,
    RawExportRawClass RawClass);

public sealed record RawExportJobOperationalHeadView(
    RawExportJobState CurrentState,
    long Revision,
    Guid? CurrentAttemptId,
    Guid? LeaseOwnerId,
    DateTimeOffset? LeaseExpiresAt,
    long FencingToken);

public sealed record RawExportJobTransitionSummary(
    RawExportJobEventType LatestEventType,
    string? LatestFailureCode,
    DateTimeOffset LatestOccurredAt);

public sealed record RawExportJobView(
    RawExportJobIdentityView Identity,
    IReadOnlyList<RawExportJobClassView> Classes,
    RawExportJobOperationalHeadView Head,
    RawExportJobTransitionSummary LatestTransition);

public sealed record RawExportJobBindResult(
    RawExportJobBindStatus Status,
    Guid JobId,
    RawExportJobTerminalizeResult? TerminalResult);

public sealed record RawExportJobReadResult(
    RawExportJobReadStatus Status,
    RawExportJobView? Job);

public sealed record RawExportJobLeaseResult(
    RawExportJobLeaseStatus Status,
    Guid? AttemptId,
    RawExportJobState? State,
    long? Revision,
    long? FencingToken,
    DateTimeOffset? LeaseExpiresAt,
    RawExportJobTerminalReasonCode? TerminalReason,
    string? StableCode);

public sealed record RawExportJobRenewResult(
    RawExportJobRenewStatus Status,
    long Revision,
    long FencingToken,
    DateTimeOffset LeaseExpiresAt);

public sealed record RawExportJobAttemptFailureResult(
    RawExportJobAttemptFailureStatus Status,
    RawExportJobState State,
    long Revision,
    long FencingToken,
    RawExportJobTerminalReasonCode? TerminalReason,
    string? StableCode);

public sealed record RawExportJobTerminalizeResult(
    RawExportJobTerminalizeStatus Status,
    RawExportJobState State,
    long Revision,
    long FencingToken,
    RawExportJobTerminalReasonCode TerminalReason,
    string? StableCode);

public sealed class RawExportJobException(string code)
    : InvalidOperationException(code)
{
    public string Code { get; } = code;
}
```

`RawExportJobBindResult` `NewJob`/`ExistingMatch` require a non-empty `JobId` and
null `TerminalResult`. `Terminal` is legal only for the safely identified
committed-job `GraphInvalid` path and requires the same non-empty `JobId` plus a
non-null exact terminal result. `FingerprintConflict` is the exact stable
exception and therefore cannot appear as a result with a `JobId`. `NotFound`
requires `Job = null`; `Found` requires a non-null view and classes in ascending
unique ordinal order. `LatestFailureCode` is the sanitized closed evidence token
stored on the latest transition, not raw exception text.

`NotFound` requires every nullable lease-result field to be null and reveals no
job state. Successful acquire/reclaim outcomes require a non-null fresh
`AttemptId`, `State = Assembling`, non-null revision/fence/lease expiry, and null
terminal fields. `AlreadyTerminal` follows the required actor-scoped reread and
returns the exact current `TerminalFailed`, `Cancelled`, or `Expired` state,
non-null revision/fence, null lease expiry, the exact latest terminal reason,
and a stable code only where planning section 10 defines one.

`AttemptId` on `RawExportJobLeaseResult` identifies only an attempt newly
authorized by this acquire/reclaim result. It is non-null only for `Acquired`,
`AcquiredAfterRetryableFailure`, and `Reclaimed`; it is null for `NotFound` and
every terminal status, including `AlreadyTerminal`. A historical
`CurrentAttemptId` remains available only through actor-scoped job read and must
not be mistaken for a new work-authorizing lease.

A newly produced `TerminalFailed` requires the exact terminal reason plus its
pinned stable code. A newly produced `Expired` requires
`PERMIT_OR_JOB_EXPIRED` and `StableCode = null`; no expiry application exception
code is invented. Renew is only `Renewed`; all normal conflicts are stable
exceptions. `Recorded` has null terminal fields; its `TerminalFailed` alternative
has both terminal fields. Terminalize always returns the exact terminal reason
and uses `StableCode` only when planning section 10 defines an operational stable
code for that result.

Every GUID is non-empty; expected revision and fence are non-negative; command
enum values must be defined; the terminal nullable attempt/owner tuple and
state/reason pair satisfy planning sections 8 and 10.3. The idempotency grammar
is exact. These checks run in memory before transaction checks. Repository
methods have `CancellationToken cancellationToken = default`; command records do
not contain cancellation tokens. The repository generates the prospective
UUIDv4 `AttemptId`; `LeaseOwnerId` is an internal server-generated identity.
No command accepts policy/session/subject/recipient/mode/classes/deadline/current
state/resulting revision/resulting fence/timestamp/event name from a caller.

Architecture/reflection tests must assert this entire public declaration
manifest, including constructor parameter order/types/nullability, enum names
and numeric values, repository signatures, and the exception `Code` surface.
No additional public B4 Domain/application-port type or member is permitted;
the separately pinned Infrastructure lease-state type in section 4.3 is not part
of this Domain manifest.

The C# state/event/failure/reason tokens must serialize with exact ordinal string
names from the planning contract. No numeric persistence and no tolerant fallback
for unknown database tokens.

### 4.2 Application port

Modify only `src/TagEkyc.Application/Ports/RepositoryPorts.cs` to add one
interface:

```text
IRawExportJobRepository
```

Its method names and return families are exactly:

```csharp
Task<RawExportJobBindResult> BindAsync(
    BindRawExportJobCommand command,
    CancellationToken cancellationToken = default);
Task<RawExportJobReadResult> ReadAsync(
    ReadRawExportJobCommand command,
    CancellationToken cancellationToken = default);
Task<RawExportJobLeaseResult> AcquireOrReclaimLeaseAsync(
    AcquireOrReclaimRawExportJobLeaseCommand command,
    CancellationToken cancellationToken = default);
Task<RawExportJobRenewResult> RenewLeaseAsync(
    RenewRawExportJobLeaseCommand command,
    CancellationToken cancellationToken = default);
Task<RawExportJobAttemptFailureResult> RecordAttemptFailureAsync(
    RecordRawExportJobAttemptFailureCommand command,
    CancellationToken cancellationToken = default);
Task<RawExportJobTerminalizeResult> TerminalizeAsync(
    TerminalizeRawExportJobCommand command,
    CancellationToken cancellationToken = default);
```

SQL `FingerprintConflict` maps to the stable
`RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT` exception and returns no result/JobId.
All remaining normal and exceptional mappings are exactly the declaration
invariants above plus planning section 10.1.

The port is repository only: no API service, endpoint, Contracts DTO, SignFlow
adapter, queue, hosted worker, or controller is added.

### 4.3 Fingerprint and lease configuration

Create:

- `src/TagEkyc.Infrastructure/RawExport/RawExportJobFingerprintCodec.cs`;
- `src/TagEkyc.Infrastructure/RawExport/RawExportJobLeaseOptions.cs`.

The codec implements the independent namespace and field order from planning
section 5.2. It must not call or wrap
`RawExportAuthorizationFingerprintCodec`. API-key provenance is excluded exactly.

Lease configuration:

```text
Key: TagEkyc:RawExport:JobLeaseSeconds
Default: 60
Valid: 10..300 inclusive
Invalid readiness code: PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID
Hot reload: forbidden
```

Resolution occurs once during application composition and registers one immutable
state. Missing uses 60; blank/malformed/non-positive/out-of-range is invalid.

The exact state type is:

```csharp
public sealed record RawExportJobLeaseState(
    int LeaseSeconds,
    bool IsValid,
    string? InvalidCode);
```

Valid state is exactly `(10..300, true, null)`. Invalid state has
`IsValid = false` and
`InvalidCode = "PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID"`; it never silently
substitutes the default. Acquire/reclaim and renew reject invalid state with that
exact code before opening a connection or issuing a B4 command.

Direct calls to either lease function with `lease_seconds` outside 10..300 raise
exact `P0001 / RAW_EXPORT_JOB_LEASE_CONFIG_INVALID`; the repository maps that
message to `PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` without exposing database
text. Dedicated tests prove repository zero-command/zero-residue behavior and
direct-function rejection at 9 and 301 seconds.

Direct-function precedence is exact: landed actor-context missing/invalid runs
first, passed-principal mismatch second, `lease_seconds` bounds third and before
any job lookup/lock, then actor-scoped ownership/internal graph, expected
revision/fence/attempt/owner, live-lease/deadline/mode, and mutation precedence
from planning section 10. Thus invalid actor plus invalid seconds returns the
actor code; valid actor plus invalid seconds returns
`RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` without revealing job existence; valid
seconds reaches normal ownership/CAS rules. Overlap tests for both lease
functions pin these orderings, not only the isolated 9/301 cases.

## 5. EF model and migration

### 5.1 Row files

Create exactly:

- `RawExportJobIdentityRow.cs`;
- `RawExportJobClassRow.cs`;
- `RawExportJobAttemptRow.cs`;
- `RawExportJobTransitionRow.cs`;
- `RawExportJobOperationalHeadRow.cs`

under `src/TagEkyc.Infrastructure/Persistence/Entities/`.

Modify `TagEkycDbContext.cs` to add the five `DbSet`s and exact EF mappings.
The table/column/PK/UK/FK/index/CHECK model must equal planning section 4.
No JSON, payload, content, blob, raw-byte, provider, vault, package, encryption,
delivery, receipt, tenant, or SignFlow column is permitted.

### 5.2 One migration

Generate exactly one EF migration pair with suffix:

```text
Tip88B4RawExportJobFoundation
```

The migration creates only the five B4 tables plus their exact indexes,
constraints, functions, triggers, guards, ACLs, and migration-time assertions.
The Designer and `TagEkycDbContextModelSnapshot.cs` may change only for the five
B4 EF entities. No landed migration may change.

`Down()` drops B4 objects in dependency-safe order and restores the complete
pre-B4 catalog/ACL state. It does not alter any B1/B2/B3/E3 grant, function,
trigger, table, or role.

### 5.3 Exact row-local CHECK additions

In addition to the complete planning schema, pin:

```text
CK_raw_export_job_identities_IdempotencyKey
  length("IdempotencyKey") BETWEEN 1 AND 256
  AND "IdempotencyKey" COLLATE "C" ~ '^[A-Za-z0-9._:-]+$'

CK_raw_export_job_attempts_Phase
  "Phase" = 'Assembling'

CK_raw_export_job_attempts_AttemptOrdinal
  "AttemptOrdinal" >= 0

CK_raw_export_job_attempts_FencingToken
  "FencingToken" >= 1

CK_raw_export_job_attempts_LeaseTime
  "InitialLeaseExpiresAt" > "AcquiredAt"
```

These checks own only row-local shape. Cross-row contiguity, current-attempt
equality, fresh AttemptId, monotonic fence, revision contiguity, and head/
transition equality remain function/lock/FK/guard invariants.

### 5.4 Intended constraint and index names

The static M1 manifest must declare these names before the migration is
generated. They are not left to provider truncation:

Primary/unique:

```text
PK_b4_job_identities
PK_b4_job_classes
PK_b4_job_attempts
PK_b4_job_transitions
PK_b4_job_operational_heads
UQ_b4_job_identity_permit
UQ_b4_job_class_ordinal
UQ_b4_job_attempt_ordinal
UQ_b4_job_attempt_fence
UQ_b4_job_transition_revision
```

Exact unique tuples:

```text
UQ_b4_job_identity_permit       (PermitId)
UQ_b4_job_class_ordinal         (JobId, Ordinal)
UQ_b4_job_attempt_ordinal       (JobId, AttemptOrdinal)
UQ_b4_job_attempt_fence         (JobId, AttemptId, FencingToken)
UQ_b4_job_transition_revision   (JobId, ResultingRevision)
```

Non-unique indexes:

```text
IX_b4_job_identity_decision          (AuthorizationDecisionId)
IX_b4_job_identity_session           (VerificationSessionId)
IX_b4_job_transition_attempt_fence   (JobId, AttemptId, FencingToken)
IX_b4_job_head_attempt_fence         (JobId, CurrentAttemptId, FencingToken)
```

Every index is configured explicitly with `HasDatabaseName(...)`; no provider
default name is accepted. M1 statically declares each name/tuple and round-trips
it from `pg_class`/`pg_index`. A direct-database negative must prove that a
duplicate `(JobId, Ordinal)` is rejected independently of bind-function class
validation.

Foreign keys:

```text
FK_b4_job_identity_permit
FK_b4_job_identity_decision
FK_b4_job_identity_session
FK_b4_job_class_job
FK_b4_job_attempt_job
FK_b4_job_transition_job
FK_b4_job_head_job
FK_b4_job_head_attempt
FK_b4_job_transition_attempt
```

Row-local CHECKs:

```text
CK_b4_job_identity_policy_version
CK_b4_job_identity_schema_version
CK_b4_job_identity_fingerprint_length
CK_b4_job_identity_export_mode
CK_b4_job_identity_deadlines
CK_raw_export_job_identities_IdempotencyKey
CK_b4_job_class_ordinal
CK_b4_job_class_raw_class
CK_raw_export_job_attempts_Phase
CK_raw_export_job_attempts_AttemptOrdinal
CK_raw_export_job_attempts_FencingToken
CK_raw_export_job_attempts_LeaseTime
CK_b4_job_transition_revision
CK_b4_job_transition_event_shape
CK_b4_job_head_shape
```

`UQ_b4_job_attempt_fence` is the referenced unique tuple
`(JobId, AttemptId, FencingToken)`. Every name and every table/column/function/
trigger identifier must be at most 63 UTF-8 bytes and round-trip verbatim. If EF
generates a different name, configure the intended name; do not bless the
provider output after generation.

## 6. Exact SQL surface

Create exactly the eight runtime-callable functions from planning section 9.1:

1. `raw_export_read_job_binding_inputs(uuid,uuid,uuid)`;
2. `raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[])`;
3. `raw_export_read_job(uuid,uuid,uuid)`;
4. `raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint)`;
5. `raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)`;
6. `raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)`;
7. `raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text)`;
8. `raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text)`.

Their exact result columns, outcome/nullability shapes, read dependencies,
mutation-context tokens, actor taxonomy, owner, language, `SECURITY DEFINER`,
search path, body manifest, and ACLs are the planning section 9 contract.

Internal functions/triggers are B4-specific and never runtime granted. They
implement:

- identity/class/attempt/transition append-only guards;
- same-transaction child guards;
- deferred `tagekyc.tr_b4_job_identity_has_classes` through
  `tagekyc.enforce_raw_export_job_identity_has_classes()`;
- command-specific operational-head guard tokens; and
- transition revision/current-attempt/head-result integrity.

The intended internal function names are exactly:

```text
enforce_raw_export_job_identity_insert
enforce_raw_export_job_class_insert
enforce_raw_export_job_attempt_insert
enforce_raw_export_job_transition_insert
enforce_raw_export_job_head_mutation
enforce_raw_export_job_identity_has_classes
```

The intended trigger names are exactly:

```text
tr_b4_job_identity_insert_guard
tr_b4_job_identity_append_only
tr_b4_job_class_insert_guard
tr_b4_job_class_append_only
tr_b4_job_attempt_insert_guard
tr_b4_job_attempt_append_only
tr_b4_job_transition_insert_guard
tr_b4_job_transition_append_only
tr_b4_job_head_mutation_guard
tr_b4_job_identity_has_classes
```

The first five internal functions may consolidate checks only for the table
named in the function. One table's context token cannot authorize another
table. The deferred completeness function remains separate. A different helper
or trigger count/name is a STOP/RRI because M1 and readiness require a static
intended manifest before DDL generation.

The claim function must execute only the named constraint sequence:

```text
DEFERRED at entry
→ identity/classes/head/initial transition inserts
→ IMMEDIATE validation
→ DEFERRED restoration before NewJob return
```

It must not use `SET CONSTRAINTS ALL`.

Before actor validation or lookup, claim and attempt-lock require:

```sql
current_setting('transaction_isolation') = 'read committed'
```

Otherwise they raise exact
`P0001 / RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID` with no authority/B4
lookup, lock, mutation, or transition.

Claim validates the idempotency grammar after actor binding but before
authoritative/B4 lookup and raises exact
`P0001 / RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED`.

## 7. Repository implementation

Create:

`src/TagEkyc.Infrastructure/Persistence/EfRawExportJobRepository.cs`

It is the sole `IRawExportJobRepository` implementation and sole owner of B4
transaction orchestration.

### 7.1 Transaction ownership

For every repository entry:

1. perform all in-memory command preflight;
2. for a preflight-valid command, require `Transaction.Current == null`;
3. require `db.Database.CurrentTransaction == null`;
4. reject a connection that is currently open at method entry or has an
   underlying active provider transaction;
5. repeat the ambient/current-transaction check immediately before invoking
   `OpenAsync` or the explicit transaction-begin call;
6. permit no application-level `await`, callback, resolver invocation, or
   database command between that final check and the open/begin invocation;
7. call the explicit
   `BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)`
   overload;
8. verify the opened transaction reports Read Committed; and
9. preserve one connection/transaction through its actor GUC, reads, locks,
   resolvers, mutation, commit/rollback, and typed result mapping.

Preflight invalidity outranks transaction isolation. A preflight-valid ambient,
caller-owned, active, or wrong-isolation transaction maps to
`RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`. The rejection is explicit and
occurs before B4 lookup, lock, mutation, or transition.

The exact connection-lifecycle path is:

1. `EfRawExportJobRepository` uses the existing scoped `TagEkycDbContext`
   injected by production DI;
2. it uses `db.Database.GetDbConnection()` as that scope's sole connection;
3. it creates no second DbContext, service scope, connection, data source, or
   connection factory;
4. it never assigns or normalizes the connection string and never changes
   configured persistence options, provider transaction-participation settings,
   service registration, or global pool configuration;
5. inside `try`, it opens the fresh explicit Read Committed transaction through
   that same DbContext and executes the complete method; and
6. inside `finally`, after commit/rollback and transaction disposal, it closes
   only the connection opened by B4 using non-cancellable cleanup and requires
   `db.Database.CurrentTransaction == null`, no underlying provider transaction,
   and `ConnectionState.Closed`.

Cleanup runs after success, typed failure, provider exception, timeout, and
cancellation. A cancellation token cannot cancel cleanup. If transaction
disposal or close cannot be completed, fail closed with
`RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`; never return a typed result while
leaving active scoped transaction/connection state.

The no-assignment rule is strictly stronger than the superseded
mutate-and-restore design: there is no mutation window and no restoration path
that can fail. Byte equality of the provider-owned live connection-string
property is not an invariant because Npgsql 8.0.3 may redact credential material
after an open.

The production-scoped B1 control-plane repository, authorization policy
projection, B2 consent repository, and B4 repository must all hold that same
`TagEkycDbContext` instance. Every B1/policy/B2 call made by B4 must observe the
same `DbConnection`, `IDbContextTransaction`, and underlying
`NpgsqlTransaction`; none may begin a nested transaction or switch connection.
An already-open scoped connection or any active EF/provider transaction at
method entry fails before a B4 command with
`RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID`. A scoped connection that was
previously opened and is now closed is supported and is the common production
entry topology. Tests must perform a landed B1/B2/B3 read before B4 and a landed
same-scope repository call after B4; a fresh never-opened DbContext is not
acceptable evidence.

### 7.1a Six-method transaction matrix

| Repository method | Owned transaction and actor context | Opening SQL/lock order | Completion |
| --- | --- | --- | --- |
| `BindAsync` | After bind preflight and ambient/active rejection, open explicit Read Committed and set `tagekyc.actor_principal_id` transaction-locally | binding-input projection, then the exact section-7.2 session → B1 → policy → B2 → claim order; committed-job `GraphInvalid` actor-reads then calls terminalize directly as the head-first seam | commit `NewJob`/`ExistingMatch` or exact typed `Terminal`; otherwise rollback |
| `ReadAsync` | After read preflight and ambient/active rejection, open explicit Read Committed and set the actor GUC | call only actor-scoped `raw_export_read_job`; no authority lock or mutation | commit `Found`/`NotFound`; rollback on exception |
| `AcquireOrReclaimLeaseAsync` | After command and lease-state preflight and ambient/active rejection, open explicit Read Committed and set actor GUC | `raw_export_lock_job_for_attempt`, then exact section-7.3 head → session → B1 → policy → B2 → mutation order | commit typed success/terminal result; rollback every exceptional outcome |
| `RenewLeaseAsync` | After command and lease-state preflight and ambient/active rejection, open a new explicit Read Committed transaction and set actor GUC; never reuse the acquisition transaction across worker work | call `raw_export_renew_job_lease` directly; that function locks/validates identity, head, and current attempt before mutation; do not call attempt-lock because a live lease would return `LeaseHeld` | commit `Renewed`; rollback every exceptional outcome |
| `RecordAttemptFailureAsync` | After command preflight and ambient/active rejection, open a new explicit Read Committed transaction and set actor GUC | call `raw_export_record_job_attempt_failure` directly; its internal head lock and live-lease validation are the opening seam | commit `Recorded` or typed `TerminalFailed`; rollback every exceptional outcome |
| `TerminalizeAsync` | After command preflight and ambient/active rejection, open a new explicit Read Committed transaction and set actor GUC | call `raw_export_terminalize_job` as the head-first lock/mutation seam; do not pre-call attempt-lock or authority resolvers | commit `Terminalized`/`AlreadyTerminal`/`Expired`, including the required actor-scoped reread for `AlreadyTerminal`; rollback every exceptional outcome |

The four mutation functions run only inside these admitted repository-owned
transactions. Renew/failure/terminalize do not add a second SQL isolation guard;
claim and attempt-lock retain the two ratified direct-runtime guards. Source and
behavioral tests cover all six methods: invalid preflight first, matching
Read-Committed ambient rejection, already-active Read-Committed connection
rejection, zero B4 command before rejection, explicit-overload use, actor-GUC
lifetime, and commit/rollback. Read has a dedicated test proving the GUC remains
set through `raw_export_read_job`; renew/failure/terminalize each prove a fresh
transaction after acquisition has committed.

For each row, the final ambient/current-transaction check is immediately adjacent
to the open/begin invocation. No application-level await, callback, resolver, or
database command may intervene. The six-method matrix covers success, typed
failure, provider exception, and cancellation for every method: exactly 24
method/exit cells, with no omitted or not-applicable cell. Every exit must leave
the scoped connection closed and both EF/provider transaction state absent.

### 7.2 Bind

Implement the exact 15-step planning-section-5.1 sequence. Reuse landed:

- `raw_export_lock_verification_session_for_authorization`;
- `IRawExportControlPlaneRepository.ResolveExportEligibilityForAuthorizationAsync`;
- exact policy projection through `IRawExportAuthorizationProjectionReader`;
- `IRawExportSubjectConsentRepository.ResolveSubjectExportConsentForAuthorizationAsync`.

Do not call the general policy repository, duplicate B1/B2 logic, change lock
order, or add a nested transaction.

Committed equal-fingerprint replay returns the immutable result validated and
persisted by the first claim. `ExportMode` is part of the semantic fingerprint,
so the same idempotency key with a different mode is `FingerprintConflict`, not
`ExistingMatch`. Replay therefore does not re-run mode/closure business
evaluation. On the prospective `NewJob` path, exact mode/closure validation
remains mandatory before insert, all mutable authority and physical time are
revalidated, and the fresh post-claim finite B1/B2 bound check runs before commit.

For binding projection `GraphInvalid` with a safely identified committed job,
actor-read the exact current identity/head and call
`raw_export_terminalize_job` directly; do not pre-call attempt-lock. Commit and
return `Terminal(JobId, result)` for `Terminalized`, higher-precedence `Expired`,
or actor-reread `AlreadyTerminal`. A concurrent revision/fence/attempt/owner
change rolls back with the existing stable exception. Do not mutate from a stale
tuple, hide the conflict, or spin/retry internally; a caller retry starts the
ordered bind transaction again.

### 7.3 Attempt orchestration

Implement planning section 7 exactly:

```text
B4 head lock
→ verification session
→ B1 locks/resolver
→ immutable policy
→ B2 consent scope
→ fresh physical clock
→ re-entrant B4 mutation
→ commit
```

CAS conflict returns before authority locks. No session/B1/B2 lock may be held
before waiting on the B4 head. No transaction/lock crosses fixture or future
non-database work.

Renew and record-failure are live-lease worker commands. Authority/deadline/
cancellation terminalization is head-first repository orchestration, not worker
authority. Apply the exact planning section 10 precedence.

## 8. Readiness, DI, and API composition

Create:

`src/TagEkyc.Infrastructure/Persistence/RawExportJobReadinessValidator.cs`

It pins:

- all five tables, columns, identifiers, indexes, constraints, triggers;
- exact CHECK definitions;
- eight entry functions and every internal helper;
- signatures/result shapes/dependencies/owner/language/`prosecdef`/search path;
- normalized grantor-aware function ACLs and body digests;
- exact table `relacl` and column `pg_attribute.attacl` posture;
- runtime execute completeness and absence of extra runtime-granted B4 entries;
- operational-head row shape; and
- immutable resolved lease configuration.

For absence-of-extra-entry validation, the exact B4 function universe is every
`tagekyc` function whose `proname` matches either
`raw_export_%job%` or `enforce_raw_export_job_%`. Readiness requires exact set
equality against the eight runtime entries and six internal functions in this
brief, then applies the pinned per-function ACL manifest. This is a deliberately
slice-local recurrence gate; it does not close the deferred aggregate
cross-slice raw-export function-manifest debt.

Stable codes are exactly:

```text
PROD_RAW_EXPORT_JOB_SCHEMA_INVALID
PROD_RAW_EXPORT_JOB_FUNCTION_ACL_INVALID
PROD_RAW_EXPORT_JOB_TABLE_PRIVILEGE_INVALID
PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID
```

Modify:

- `TagEkycPersistenceServiceCollectionExtensions.cs` to register the repository
  and validator scoped;
- `Program.cs` to resolve/register the immutable lease state and production
  readiness check;
- `ReadinessEndpoint.cs` to add only
  `RawExportJobReadinessCheck`.

The readiness check maps the exact code through the existing database-order HTTP
503 pipeline. No endpoint, request/response DTO, or status route is added.

## 9. ACL and migration-time assertions

All B4 tables have no runtime or PUBLIC table privilege. The normalized explicit
non-owner table ACL set is empty. All non-dropped user columns have an empty
explicit non-owner column ACL set through `pg_attribute.attacl`.

Every runtime entry has exactly:

```text
grantor = tagekyc_raw_export_deployer
grantee = tagekyc_runtime
privilege = EXECUTE
is_grantable = false
```

Every internal function has no non-owner ACL row. Deployer owns every B4 object.

After object creation/revoke/grant, but before migration history can commit, the
migration asserts the exact function/table/column ACL manifests:

```text
TIP88B4_FUNCTION_ACL_INVALID
TIP88B4_TABLE_ACL_INVALID
```

Unexpected default/out-of-band drift aborts apply. The migration never repairs
such drift by revoking the unexpected role.

## 10. Test placement and binding gates

Create:

- `tests/TagEkyc.UnitTests/Tip88B4RawExportJobTests.cs`;
- `tests/TagEkyc.ArchTests/Tip88B4RawExportJobBoundaryTests.cs`;
- `tests/TagEkyc.IntegrationTests/Tip88B4RawExportJobFoundationTests.cs`.

Database function/catalog contracts are integration contracts and live in the
B4 integration file; do not add a TagEkyc.Contracts DTO or project reference
solely to place them in `TagEkyc.ContractTests`.

Every named M1–M13 test in planning section 12 is permanent and must retain its
exact method name. Test methods may be distributed across the three files by
proof level, but no gate may be folded into a broad green test.

The amendment tests are also permanent and their exact names are:

```text
B4_does_not_assign_or_normalize_connection_string
B4_does_not_reference_enlist_or_npgsql_connection_string_builder
B4_uses_same_scoped_dbcontext_connection_and_transaction_for_B1_B2
B4_does_not_create_second_dbcontext_connection_or_datasource
B4_all_six_methods_preflight_precedes_transaction_admission
B4_all_six_methods_reject_ambient_transaction_before_database
B4_all_six_methods_reject_existing_ef_transaction_before_database
B4_all_six_methods_reject_existing_provider_transaction_before_database
B4_all_six_methods_reject_open_connection_before_database
B4_all_six_methods_own_fresh_read_committed_transactions
B4_all_six_methods_final_admission_is_adjacent_to_each_open_or_begin
B4_same_scope_landed_repository_can_reopen_after_B4
B4_connection_is_closed_and_transaction_free_after_every_exit
B4_global_persistence_options_remain_unchanged

M3_committed_graph_invalid_claimed_terminalizes_and_returns_terminal
M3_committed_graph_invalid_active_lease_uses_head_first_terminalization
M3_committed_graph_invalid_deadline_crossing_returns_expired
M3_committed_graph_invalid_already_terminal_returns_exact_result
M3_committed_graph_invalid_stale_tuple_rolls_back_without_mutation

M7_direct_acquire_invalid_lease_bound_precedes_job_lookup
M7_direct_renew_invalid_lease_bound_precedes_job_lookup
M7_invalid_actor_precedes_invalid_lease_bound
```

The `enlist` token in the source-prohibition test identifier names the
superseded mechanism whose reintroduction it forbids; it is not an active
configuration requirement. That source test scans only:

```text
src/TagEkyc.Infrastructure/Persistence/EfRawExportJobRepository.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260726145547_Tip88B4RawExportJobFoundation.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260726145547_Tip88B4RawExportJobFoundation.Designer.cs
```

It must not scan the whole repository because landed slices legitimately contain
provider connection-string types and configuration.

The seven all-six admission/ownership/adjacency methods remain data-driven and
retain all executed cases. Their proof mechanisms are not interchangeable:
behavioral cases require an observed-red behavioral mutation; bounded source
assertions are source-grep/static proof; and an unsupported state is explicitly
non-constructible/not applicable rather than advertised as a green mutation cell.

The cleanup and same-scope-reopen methods each execute the complete 24-cell
matrix: six repository methods multiplied by success, typed failure, provider
exception, and cancellation. There are no omitted executed cells. Both methods
currently call the same shared assertion helper, which already includes the
landed same-scope reuse assertion; the second 24 executions are therefore
byte-identical reruns, not 24 additional behaviorally distinct proofs.

Exact evidence classification and mutation map:

| Evidence cell or group | Count | Category | Named evidence and accurate mutation claim |
| --- | ---: | --- | --- |
| ambient transaction rejection | 6 | behavioral, mutation-proven | removing `Transaction.Current` rejection makes each method case in `B4_all_six_methods_reject_ambient_transaction_before_database` red |
| currently-open connection rejection | 6 | behavioral, mutation-proven | removing `connection.State != Closed` makes each method case in `B4_all_six_methods_reject_open_connection_before_database` red |
| provider-current transaction rejection | 6 | behavioral, mutation-proven | `B4_all_six_methods_reject_existing_provider_transaction_before_database`; the same connection-state mutation drives this and the open row, so these are not six additional distinct guard behaviors |
| EF-current transaction independent of an open connection | 6 | non-constructible / not applicable | EF `BeginTransactionAsync` opens the connection; `CurrentTransaction != null` with a closed connection cannot be produced on a supported path, so no independent mutation-red claim is made |
| preflight-precedence topology fan-out | 24 | behavioral, mutation-proven | moving preflight after admission makes `B4_all_six_methods_preflight_precedes_transaction_admission` red, but invalid actor validation otherwise exits before `ExecuteAsync`; the 24 executed cells collapse to six equivalence classes, one per method, because the topology dimension has no influence on this test |
| explicit fresh Read Committed ownership | 6 | behavioral, mutation-proven | changing the owned isolation makes each method case in `B4_all_six_methods_own_fresh_read_committed_transactions` red |
| final `BeginTransactionAsync` adjacency | 6 | source-grep/static proof | bounded public-method and `ExecuteAsync` scans in `B4_all_six_methods_final_admission_is_adjacent_to_each_open_or_begin`; no behavioral mutation-red claim |
| explicit B4 `OpenAsync`/`OpenConnectionAsync` adjacency | 6 | non-constructible / not applicable | supported B4 code has no explicit open invocation; `B4_explicit_open_adjacency_cells_are_not_applicable` and method-bounded absence assertions record that fact |
| cleanup matrix | 24 | behavioral, mutation-proven | canonical mutation 7b—B4 owns the open and the complete `finally` cleanup path is removed—makes `B4_connection_is_closed_and_transaction_free_after_every_exit` red; deleting only the conditional close is vacuous because EF closes an EF-opened connection |
| same-scope reopen matrix | 24 | behavioral, mutation-proven | `B4_same_scope_landed_repository_can_reopen_after_B4` repeats the same helper and canonical mutation evidence; it is not 24 additional distinct cells |
| connection-string assignment/provider-builder prohibition | implementation manifest | source-grep/static proof | `B4_does_not_assign_or_normalize_connection_string` and `B4_does_not_reference_enlist_or_npgsql_connection_string_builder`; source mutation is detected statically, not claimed as behavioral evidence |
| one scoped DbContext/connection/transaction and no second source | implementation manifest | source-grep/static proof | `B4_uses_same_scoped_dbcontext_connection_and_transaction_for_B1_B2` and `B4_does_not_create_second_dbcontext_connection_or_datasource` |
| unchanged persistence/provider/global options | implementation manifest | source-grep/static proof | `B4_global_persistence_options_remain_unchanged` |
| committed graph-invalid `Claimed` terminalization | 1 | behavioral, mutation-proven | deletion makes `M3_committed_graph_invalid_claimed_terminalizes_and_returns_terminal` red |
| graph-invalid active-lease head-first path | 1 | behavioral, mutation-proven | pre-calling attempt-lock makes `M3_committed_graph_invalid_active_lease_uses_head_first_terminalization` red |
| graph-invalid deadline precedence | 1 | behavioral, mutation-proven | removing higher-precedence deadline handling makes `M3_committed_graph_invalid_deadline_crossing_returns_expired` red |
| actor-scoped `AlreadyTerminal` reread | 1 | behavioral, mutation-proven | skipping the reread makes `M3_committed_graph_invalid_already_terminal_returns_exact_result` red |
| graph-invalid revision/fence/attempt/owner CAS | 1 | behavioral, mutation-proven | weakening the tuple makes `M3_committed_graph_invalid_stale_tuple_rolls_back_without_mutation` red |
| acquire bound before lookup | 1 | behavioral, mutation-proven | moving the bound makes `M7_direct_acquire_invalid_lease_bound_precedes_job_lookup` red |
| renew bound before lookup | 1 | behavioral, mutation-proven | moving the bound makes `M7_direct_renew_invalid_lease_bound_precedes_job_lookup` red |
| actor validation before lease bounds | 2 functions | behavioral, mutation-proven | moving bounds first makes `M7_invalid_actor_precedes_invalid_lease_bound` red |
| named B4 CHECK/trigger invariants in M1/M10 | manifest-defined | structural/constraint-proven | database constraints and triggers prevent invalid persisted shapes; their dedicated mechanism mutations remain in the M1/M10 gates |

Additional binding:

- M3 source/architecture proof must pin the explicit
  `BeginTransactionAsync(IsolationLevel.ReadCommitted, ...)` overload because
  Npgsql's parameterless API currently defaults to the same isolation;
- matching Read Committed ambient/active-connection tests must prove no B4 DB
  command for every one of the six repository methods;
- committed-job `GraphInvalid` bind tests cover `Claimed`, actively leased
  `Assembling`, deadline crossing to `Expired`, `AlreadyTerminal`, and a
  concurrent revision/fence/attempt/owner change; the last must return the exact
  stable conflict with no stale mutation and must go red if the CAS predicate is
  weakened;
- M4 expected hashes come from an independent test codec, not production output;
- M6 uses a dedicated LOGIN INHERIT role with only `tagekyc_runtime`,
  `ADMIN=false`, `INHERIT=true`, `SET=false`, and no `SET ROLE`;
- M6 includes `M6_extra_runtime_granted_b4_entry_fails_readiness`: create a
  scratch ninth `tagekyc.raw_export_*job*` function, grant runtime EXECUTE,
  observe exact `PROD_RAW_EXPORT_JOB_FUNCTION_ACL_INVALID`, remove it, and prove
  catalog equivalence;
- M9 fixture assembly/delivery side effect is test-only and never registered;
- M12 apply mutations use isolated disposable PostgreSQL-16 clusters; and
- M13 scans database/application/API/Contracts surfaces and preserves the landed
  B3 inertness assertion.

## 11. Mutation protocol

After all permanent edits and green non-mutation targeted tests, capture
Baseline B:

- SHA-256 of every allowlisted source/test/doc file;
- migration/model snapshot hashes;
- complete B4 plus affected landed catalog/ACL/role state.

Each scratch mutation:

1. has one named green positive control;
2. proves the mutation is active;
3. runs only the discriminating named test;
4. must observe RED for the intended assertion;
5. restores source byte-identically or catalog-equivalently in `finally`;
6. reruns the named test GREEN; and
7. proves Baseline-B restoration.

Required mutation families are all those in M1–M12, including identifier
round-trip, idempotency function/table guards, constraint mode, every row-local
CHECK, cross-job FKs, fresh AttemptId/fence/revision/head equality, actor context,
all independent B1/B2/session locks, physical-time placement, explicit isolation,
ambient/EF/provider/open-connection early gates, final-check adjacency,
ACL/default-ACL/alternate-grantor/column grants, append-only/head guards,
terminal/expiry precedence, and readiness branches.
They also include duplicate class ordinal, exact non-unique index names/tuples,
public declaration reflection, all six transaction-owner paths, invalid lease
state/direct-function bounds, committed-GraphInvalid bind terminal/race mapping,
terminal lease-result `AttemptId` nullness, source prohibition of connection-
string assignment/provider-specific builders, same-scope reopen after every exit
path, closed/transaction-free cleanup, unchanged global persistence options,
root build-graph/dispatch-commit scope, and the scratch ninth runtime-granted B4
entry.

A required mutation that stays green is a STOP. Do not weaken the test or mutate
an unrelated earlier guard to manufacture red.

## 12. Frozen implementation allowlist

This allowlist was activated by the separate controlled implementation dispatch.
The implementation has landed and the allowlist remains frozen and verify-only:
the docs-only D1/D2 amendment does not authorize adding, removing, or editing an
implementation surface.

Production:

- `src/TagEkyc.Domain/RawExportJob.cs` (new);
- `src/TagEkyc.Application/Ports/RepositoryPorts.cs`;
- `src/TagEkyc.Infrastructure/RawExport/RawExportJobFingerprintCodec.cs` (new);
- `src/TagEkyc.Infrastructure/RawExport/RawExportJobLeaseOptions.cs` (new);
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobIdentityRow.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobClassRow.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobAttemptRow.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobTransitionRow.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportJobOperationalHeadRow.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`;
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportJobRepository.cs` (new);
- `src/TagEkyc.Infrastructure/Persistence/RawExportJobReadinessValidator.cs`
  (new);
- `src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs`;
- one new migration pair with suffix `Tip88B4RawExportJobFoundation`;
- `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`;
- `src/TagEkyc.Api/Program.cs`;
- `src/TagEkyc.Api/ReadinessEndpoint.cs`.

Tests:

- the three new files from section 10;

The landed
`tests/TagEkyc.IntegrationTests/Tip88B2SubjectExportConsentTests.cs`
`Inertness_no_raw_byte_surface_exists` method remains byte-unchanged. M13 adds
the B4 `raw_export_job_%` scan in the new B4 integration file; it does not
repurpose or weaken the landed B2/B3 tripwire.

Implementation documentation:

- `docs/deployment/hospital_trial/postgres_migration_runbook.md` only.

Verify-only and byte-pinned during implementation:

- the ratified B4 planning brief;
- the final B4 build brief/dispatch;
- `docs/tips/README.md`; and
- `docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md`.

The dispatched Task-0 report records SHA-256 for each verify-only document.
Their hashes must remain identical through implementation, mutation work, and
code acceptance. If implementation discovers that any planning/build contract
must change, STOP/RRI:

```text
review amendment
→ docs-only ratification
→ re-anchor
→ redispatch
```

Only after adversarial code acceptance may a separate docs-only closeout
authorization create the B4 closeout and update the TIP index/status. Those
governance changes are not part of the implementation allowlist or implementation
commit.

No other permanent file may change without STOP/RRI and a reviewed allowlist
extension. In particular, landed B1/B2/B3/E3 production files/migrations,
authorization/policy/consent repositories, Contracts DTOs, API endpoints,
SignFlow, tenant models, and unrelated tests are forbidden.

Scratch mutation copies are evidence-only and must not remain modified.

## 13. Runbook delta

Update the hospital PostgreSQL migration runbook with:

- five B4 metadata tables;
- exact runtime function EXECUTE manifest;
- zero table/column privilege posture;
- lease configuration key/default/range and restart-only resolution;
- readiness codes;
- migration apply/rollback checks;
- no raw/package/delivery capability statement; and
- operational cleanup for scratch roles/default ACLs used only in tests.

The same delta removes the obsolete E3 “working-tree-only / do not deploy” hold
and replaces it with the landed E3 closeout posture, while preserving all final
E3 role, ACL, readiness, and rollback requirements. It must not describe E3 as
pending or reopen E3 design.

Do not claim production activation, raw availability, multi-hospital shared-DB
support, or resistance to full trusted-backend compromise.

## 14. Validation order for a later authorized build

1. Task 0 and pre-B4 catalog capture.
2. Unit/architecture tests for domain, codec, port, explicit isolation, inertness.
3. Non-mutation M1–M13 integration gates individually.
4. Capture Baseline B.
5. Every required observed-red mutation and exact restoration.
6. Targeted B4 unit/architecture/integration suites.
7. B1/E3, B2, B3-1/2/3/4 regression suites.
8. Apply/rollback/reapply on fresh PostgreSQL 16.
9. Pending-model gate.
10. Full solution build.
11. Full solution test suite sequentially.
12. Fresh catalog/readiness/ACL audit.
13. Final allowlist, scratch-cleanup, Baseline-B, and git-status audit.

No required test may be skipped. The manual migration-generator discovery test
may retain its existing intentional skip only if it is unrelated and reported
separately; no B4 gate may be skipped.

## 15. STOP / RRI

STOP rather than improvise if:

- final HEAD/source baseline differs from the dispatched anchor;
- a required permanent file is outside section 12;
- a ninth runtime function or generic transition entry appears necessary;
- JSON/dynamic SQL/caller-supplied authority data is proposed;
- runtime table/column privilege appears necessary;
- Read Committed cannot be explicitly owned and verified;
- an ambient/caller transaction cannot be rejected before B4 commands;
- landed lock order/resolver semantics would need alteration;
- a B1/B2/B3/E3 production file or migration would need modification;
- a raw/provider/vault/package/crypto/delivery/receipt/HTTP/SignFlow/tenant
  surface appears;
- ModelSnapshot contains changes outside the five B4 entities;
- rollback cannot restore pre-B4 catalog/ACL state exactly;
- a required mutation does not turn its named test red;
- a test claims a cross-row invariant is enforced by a row-local CHECK;
- a fixture is proposed for production DI; or
- full validation has an unexplained failure or B4 skip.

## 16. Builder report format

The later builder must report:

1. dispatched baseline and Task-0 results;
2. exact permanent files changed and allowlist comparison;
3. domain/application port surface;
4. five-table schema, constraints, indexes, triggers, and identifier lengths;
5. eight entry functions plus internal manifest/dependencies/ACLs;
6. transaction ownership, final admission-check adjacency, unchanged connection
   configuration, same-scope reopen/cleanup, lock order, and stable precedence
   mapping;
7. M1–M13 to exact test names/results;
8. every observed-red mutation and post-restore green result;
9. apply/rollback/reapply and pre/post catalog/ACL equivalence;
10. ModelSnapshot before/after hashes and five-entity-only delta;
11. lease config/readiness/HTTP-503 evidence;
12. targeted and full build/test totals, including every failure/skip;
13. final scratch-role/default-ACL/container cleanup;
14. final `git status --short`; and
15. deviations, workarounds, STOP/RRI, or incomplete work.

No green-summary claim substitutes for code, catalog, mutation, and restoration
evidence.

## 17. Commit and deployment boundary

This D1/D2 amendment authorizes no commit, push, merge, deployment, migration
execution, Raw BIO access, or production activation. Do not stage or modify
implementation files. The amendment's three documentation files require a
separate controlled docs-only commit instruction, and B4 closeout/status edits
require their own later authorization and commit scope.

## 18. Review state

### V1 — v0.1 deep bounded review, v0.2 patched

The independent reviewer read the required governance, every available TIP-88
planning/build/remediation/closeout document, runbook, debt/decision references,
and the landed B1/B2/B3/E3 source, migrations, readiness, DI, and tests. V1
returned **NO-GO** with 4 HIGH, 2 MEDIUM, and 1 LOW:

- wrong/missing class ordinal uniqueness;
- incomplete non-unique index manifest;
- incomplete public C# declaration manifest;
- ambiguous transaction ownership for read and post-acquisition commands;
- unpinned invalid lease operational behavior;
- undefined extra-B4-entry readiness universe/mutation; and
- obsolete E3 runbook hold.

Version 0.2 patches all seven without changing the ratified planning contract or
authorizing implementation. V2 patch verification remains required.

### V2 — v0.2 patch verification, v0.3 correction candidate

V2 returned **NO-GO** with 2 HIGH and 1 MEDIUM. It confirmed class uniqueness,
all four non-unique indexes, the B4 function universe/ninth-entry mutation,
runbook correction, and authority wording. It found:

- the new lease result could not represent `NotFound`, `AlreadyTerminal`, or
  expiry without inventing a stable code;
- the six-method transaction matrix exposed an impossible sentence in the
  ratified planning section 9.1a; and
- direct lease-bound precedence was not discriminated against actor and
  ownership/CAS failures.

Version 0.3 corrects the result/nullability manifest and pins direct-function
precedence. It records the exact planning erratum required to reconcile section
9.1a but does not silently treat that erratum as ratified. V3 free adversarial
review may assess the complete candidate; dispatch remains blocked until the
Homeowner ratifies the erratum and a subsequent synchronization review is clean.

### V3 — v0.3 free adversarial review, v0.4 amendment package

V3 returned **NO-GO** with 2 HIGH and 1 MEDIUM. It confirmed the transaction
replacement is technically sound and the lease result now represents
`NotFound`, `Cancelled`, and code-less expiry. It found:

- lease-bound precedence is itself an observable planning amendment not covered
  by the first erratum;
- planning section 3.2 cannot represent section 5.1's committed-job
  `GraphInvalid` terminal result or its concurrent head race; and
- terminal lease results did not pin whether `AttemptId` was historical or newly
  work-authorizing.

Version 0.4 makes `AttemptId` new-acquisition-only, adds an exact bind terminal
result/race contract, and replaces the single erratum with coordinated
Amendments A–C. None is treated as ratified. V4 bounded verification is required
before the package is safe to present for Homeowner decision.

### V4 — v0.4 bounded verification, v0.5 one-line correction

V4 returned **NO-GO** with one HIGH patch regression. Amendments A and B were
individually sound but Amendment B's categorical “Bind is admitted through
claim” contradicted Amendment A's required pre-claim committed-job
`GraphInvalid` path. V4 confirmed all other V3 findings closed.

Version 0.5 changes Amendment B only to distinguish normal claim-admitted bind
from repository-admitted committed-`GraphInvalid` direct terminalization. V5 is
the fifth and final convergence review under the playbook threshold. Any V5
finding triggers non-convergence analysis and STOP rather than another edit.

### V5 — v0.5 final convergence verification, clean

The independent reviewer read all 57,258 bytes of v0.5 byte-to-EOF and returned
**PASS — 0 HIGH / 0 MEDIUM / 0 LOW**. It confirmed the V4 admission correction,
the coordinated Amendments A–C package, exact result/transaction/precedence
propagation, README authority wording, and unchanged `src/`/`tests/`.

Version 0.6 changed only header status and this V5 closeout record. A metadata
attestation then found one stale Dispatch Readiness phrase that still named
review convergence as unfinished. Version 0.7 changes only the version and that
remaining-gates line to name Homeowner amendment ratification plus subsequent
synchronization verification instead. The package is safe to present for
Homeowner ratification of Amendments A–C. It is not a planning amendment
ratification, implementation dispatch, build authorization, or authority to
migrate, edit code/tests, commit, push, merge, deploy, access Raw BIO, or
activate production.

### External review — v0.7 corrected in v0.8

The external review returned **APPROVE WITH CORRECTIONS** with 1 HIGH,
4 MEDIUM, and 1 LOW. Direct source verification confirmed that production B1,
policy projection, B2, and authorization repositories share the scoped
`TagEkycDbContext`, so v0.8:

- **Historical, superseded by Amendment D:** pins instance-local `Enlist=false`
  normalization on that same closed scoped connection, forbidding a second
  context/connection and global-option change;
- extends Amendment B to planning section 3.3;
- proves Task-0 source/test equivalence across commits plus staged, unstaged, and
  untracked surfaces with complete tree manifests;
- separates implementation documentation from byte-pinned governance inputs and
  future docs-only closeout;
- pins exact amendment test names and mutation-to-red mappings; and
- corrects the worker non-claim to exclude hosted/background/non-database work,
  not the B4 lease/fencing orchestration contract.

Version 0.8 is verification-required and does not restore ratification readiness
until an independent correction check returns clean.

### External-correction verification — v0.8 clean, v0.9 closeout

The independent reviewer read all 66,498 bytes of v0.8 byte-to-EOF, verified the
six corrections against landed source, Npgsql 8.0.3, planning, and README, and
returned **PASS — 0 findings**. It confirmed same-scoped-DbContext feasibility,
Amendment-B synchronization coverage, complete Task-0 drift detection,
governance/implementation allowlist separation, exact amendment test/mutation
mapping, and corrected worker wording with no status or authority regression.

Version 0.9 changes only header status and this closeout record. The coordinated
Amendments A–C package is safe to present for Homeowner ratification. It remains
unratified and is not an implementation dispatch or authority to migrate, edit
code/tests, commit, push, merge, deploy, access Raw BIO, or activate production.

### v0.9 review — amendments pass, build-dispatch hardening in v0.10

The v0.9 review returned **AMENDMENTS A–C: PASS** and
**BUILD BRIEF: APPROVE WITH CORRECTIONS**, with 1 HIGH, 1 MEDIUM, and 1 LOW.
Version 0.10:

- **Historical, superseded by Amendment D:** restores the exact scoped connection
  string in non-cancellable `finally` cleanup on every exit and proves landed
  same-scope enlistment is unchanged;
- pins the complete dispatch commit to the exact three docs-only ratification
  paths and protects the root solution/build/package graph as well as
  `src/tests`; and
- replaces the two stale bind/attempt summary phrases with the six-method
  transaction contract.

Version 0.10 is verification-required. Amendments A–C remain architecturally
accepted by the review, but this document does not return to Homeowner
ratification readiness until the three corrections pass an independent check.

### v0.10 correction verification — Task-0 gate fixed in v0.11

At that historical stage, the independent correction review accepted connection
restoration (superseded by Amendment D), root build-graph protection, and
six-method summary wording, but reproduced one MEDIUM false-pass in the exact
docs-only commit gate. Empty/failed Git output
could make `Compare-Object` emit only a non-terminating error and leave a
zero-count delta; rename detection was also implicit.

Version 0.11 removes `Compare-Object`, disables rename detection, requires Git
success, requires the exact path count, and compares every sorted path with
ordinal equality. The empty-delta case was executed locally and failed closed
with `TIP88B4_DISPATCH_COMMIT_SCOPE_INVALID`. Independent verification remains
required before restoring Homeowner ratification readiness.

### v0.11 Task-0 verification — clean, v0.12 closeout

The independent reviewer executed the exact v0.11 PowerShell gate against the
correct three-path list, reversed order, empty output, invalid revision, extra,
missing, same-count-wrong, case-changed, and real rename cases. It returned
**PASS**: the valid set alone passes, every invalid case fails closed, Git errors
map to `TIP88B4_DISPATCH_COMMIT_DIFF_FAILED`, scope mismatch maps to
`TIP88B4_DISPATCH_COMMIT_SCOPE_INVALID`, and `--no-renames` exposes both rename
sides.

Version 0.12 changes only header status and this closeout record. Amendments A–C
and the hardened build brief are safe to present for Homeowner amendment
ratification. They remain unratified; this is not an implementation dispatch or
authority to migrate, edit code/tests, commit, push, merge, deploy, access
Raw BIO, or activate production.

Required before build authorization:

- controlled docs-only ratification commit of the exact three approved paths;
- final baseline minting and explicit candidate allowlist acceptance;
- PostgreSQL-16 fixture availability; and
- Homeowner approval of the final build brief as an implementation dispatch.

### Homeowner amendment ratification — planning v0.17 synchronization

On 2026-07-26, the Homeowner ratified coordinated planning Amendments A–C.
Planning Brief v0.17 now contains their self-contained wording in sections 3.2,
3.3, 5.1, 9.1a, and 10.2. In particular, its section 9.1a refers to the
section-5.1 contract directly rather than depending on the historical amendment
label.

Version 0.13 changes only header metadata and this ratification/synchronization
record. Independent byte/status synchronization verification remains required.
This is not an implementation dispatch or authority to migrate, edit code/tests,
commit, push, merge, deploy, access Raw BIO, or activate production.

### Round-1 synchronization review — corrections in v0.14

The first independent byte-to-EOF review returned 2 HIGH and 1 MEDIUM:

- section 2.1 still presented the now-ratified amendments as replacement
  proposals;
- planning section 10.1's exhaustive mapping had not carried through every
  ratified result/transaction/lease-bound effect; and
- the remaining-gate lists still named completed ratification and synchronization
  work.

Version 0.14 converts section 2.1 to a ratified incorporation record, synchronizes
the missing planning mappings in v0.18, and leaves only genuinely open dispatch
gates. Independent correction verification is required. This is not an
implementation dispatch or authority to migrate, edit code/tests, commit, push,
merge, deploy, access Raw BIO, or activate production.

### Round-2 synchronization review — correction in v0.15

The second independent byte-to-EOF review confirmed every Round-1 defect closed
and found one MEDIUM manifest omission. Planning v0.19 now lists
`PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID` in section 10's stable-code manifest
and pins its shared fail-closed application meaning consistently with the exact
result mapping and readiness contract.

The coordinated Planning v0.19 correction changes the stable-code manifest.
Build Brief v0.15 advances its header contract pointer to that planning version
and adds this correction record. Independent correction verification is
required. This is not an implementation dispatch or authority to migrate, edit
code/tests, commit, push, merge, deploy, access Raw BIO, or activate production.

### Round-3 synchronization review — version pointer corrected in v0.16

The third independent byte-to-EOF review confirmed the ratified A–C semantics,
mapping, transaction, precedence, gates, authority, and docs-only scope clean,
but found one MEDIUM version-sync defect: section 2.1 still pointed at Planning
v0.18 even though the current ratified contract and Amendment-C manifest
correction are in v0.19.

Version 0.16 corrects that pointer and corrects the v0.15 change record, which
could no longer truthfully claim a metadata-only change. Independent correction
verification is required. This is not an implementation dispatch or authority
to migrate, edit code/tests, commit, push, merge, deploy, access Raw BIO, or
activate production.

### Round-4 convergence PASS — v0.17 closeout

The fourth independent byte-to-EOF convergence review returned
**PASS — 0 HIGH / 0 MEDIUM / 0 LOW**. It confirmed the complete ratified A–C
contract, exhaustive mappings, stable-code/readiness contract, transaction
ownership, terminalization, lease precedence, operative/historical wording,
remaining gates, authority, and docs-only scope are aligned.

Version 0.17 changes only header metadata, remaining-gate status, and this
closeout record. It is ready only for a controlled docs-only ratification commit
of the exact approved documentation paths. It remains **NOT DISPATCHED** and
does not authorize implementation, migration, code/test work, push, merge,
deployment, Raw BIO access, or production activation.

### Homeowner-ratified Amendment D — v0.18 synchronization

On 2026-07-27, the Homeowner adopted connection-lifecycle Amendment D after the
pinned Npgsql 8.0.3 provider reproduced credential redaction on the supported
previously-opened-now-closed scoped connection topology. Version 0.18:

- removes every active per-call connection-string mutation, normalization, and
  byte-restoration requirement;
- makes no assignment/configuration mutation a strictly stronger invariant;
- pins preflight-first ambient, EF-current, provider-transaction, and
  currently-open connection rejection;
- pins the final no-gap admission check immediately before open/begin;
- retains one fresh explicit Read Committed transaction and one unchanged
  scoped DbContext/connection/transaction per method;
- requires same-scope landed-read → B4 → landed-reopen evidence and
  closed/transaction-free cleanup across success, typed failure, provider
  exception, and cancellation;
- replaces the permanent test and mutation manifests accordingly; and
- changes Task-0 to capture `$dispatchedHead` at runtime without embedding a
  dispatched hash in either brief.

Historical v0.8/v0.10 descriptions of the former design remain only where marked
superseded. At v0.18, implementation remained stopped; that historical status is
superseded by the v0.19 disposition below. The v0.18 synchronization authorized
no implementation resume, migration execution, commit, push, merge, PR,
deployment, Raw BIO access, or production activation.

### Homeowner-authorized D1/D2 evidence correction — v0.19

After implementation and the final closeout-review fixes landed at
`d09929df3d5f5eacf1968480c9fcbb6c5b13a200`, three independent adversarial
reviewers identified two documentation-only divergences. Version 0.19:

- preserves every permanent test and executed matrix cell while replacing false
  distinct-cell and mutation-red claims with the exact behavioral,
  structural/constraint, source/static, and non-constructible classifications;
- records that the 24 preflight executions reduce to six equivalence classes,
  one per method, because the topology dimension has no influence, and that the
  second 24 cleanup/reopen executions repeat the same helper;
- preserves the landed early `ExistingMatch` return because the immutable first
  claim was validated and `ExportMode` is fingerprint-bound; and
- keeps mode/closure validation mandatory on the prospective `NewJob` path.

This is a docs-only evidence correction. It does not alter or authorize code,
tests, migrations, commit, push, merge, PR, deployment, Raw BIO access, or
production activation. A separate instruction is required for the docs-only
commit and for B4 closeout.
