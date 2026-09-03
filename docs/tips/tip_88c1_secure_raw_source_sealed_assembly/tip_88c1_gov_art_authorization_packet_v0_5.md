# TIP-88C1 GOV/ART Authorization Packet v0.5

**File:** `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_v0_5.md`

**Version:** 0.5

**Status:** DRAFT FOR INDEPENDENT REVIEW — FIXTURE EVIDENCE INACTIVE — IMPLEMENTATION BLOCKED

**Date:** 2026-08-04

**Baseline:** `672f3e4eeba24b6082bbb3c0696e597c8821e4c7`

**Packet id:** `TIP-88C1-GOV-ART-FIXTURE-EVIDENCE-v0.5`

**Purpose:** Define the narrow GOV/ART boundary that, after two clean
independent reviews and a committed append-only Homeowner ratification record,
may allow a later TIP-88C1 Build Brief to authorize generated, non-patient
reference-provider fixture evidence. This packet is neither a Build Brief nor
execution authority.

## Changelog

### v0.5 — committed re-anchor and DURABLE-OBJECT synchronization

- Added a provenance-preserving `COMMITTED_REANCHOR` transition. It binds the
  exact committed inactive v0.3 record and explicitly ends propagation of the
  historical uncommitted sentinel without treating history as authority.
- Reconciled the Gate A identity graph with DURABLE-OBJECT v0.5: writer,
  reconciler, lifecycle, posture-probe and external fixture-cleanup capabilities
  are distinct; assembly read remains deferred.
- Added lifecycle-configuration and public-access posture to Model A.
- Split delete acknowledgement from two-observation positive absence, including
  exact actor and evidence-kind separation.
- Replaced the false physical 24-hour deletion claim with an immutable maximum
  authorized fixture-retention deadline and an irreversible expiry-breach/debt
  transition.
- Bound expected negative-test faults to exact operation, capability, executing
  identity, source state and source revision.
- Unified `EvidenceRunTerminalizedAtUtc` for graceful and abandoned terminal
  CASes.
- Superseded packet v0.4, SHA-256
  `B37D2537793B3D4347D3FB2D8016FC0E6CCF93237CD91F8872A32F6F067A55E5`.

### v0.4 — joint-satisfiability closure

- Removed the impossible self-containing-commit binding. A record binds the
  predecessor's SHA-256 and containing commit; a later Build Brief verifies the
  final record's path, blob SHA-256, and containing commit directly from Git.
- Added `ExecutionFinishedCleanupPending` and
  `AbandonedCleanupPending`; no run reaches a terminal state while lifecycle
  work remains open.
- Derived `BucketDeleteDeadlineUtc` from every open cleanup obligation and
  bounded it by the absolute `BucketDeleteHardCapUtc`.
- Repaired quarantine reachability: the expected reviewer branch closes as
  `QuarantineCleaned` without a cleanup-debt aggregate; unexpected or timed-out
  branches open debt before any close.
- Bound the expected negative-test fault before execution and made every
  unmatched integrity failure irrevocably fail the run.
- Added complete deadline joint-satisfiability and closed-state reachability
  tables.
- Superseded packet v0.3, SHA-256
  `56E52E5AAE7ABEC18EE3D93168256B2815B6D2DCA789AACB4CC4105D043E3EF5`.

### v0.3 — scope, anchor, ledger-chain, and term-register closure

- Selected one fresh, whole-bucket-enumerated fixture bucket per evidence run;
  pinned create/delete privilege and removed every run-prefix-limited residue
  claim.
- Added persisted absolute run anchors, a total `EffectiveRunEndUtc`,
  independent watchdog/reconciler behavior, and absolute deadline derivations.
- Made all four hold/quarantine terminal branches structurally symmetric:
  authorizer, executing identity, CAS transition, and terminal proof.
- Replaced mutable ratification state with a versioned hash chain; future Build
  Briefs must bind the active record path, hash, and normative repository
  commit.
- Added a marker-delimited normative-term population and a complete term/scope
  register with no blank cells.
- Superseded packet v0.2, SHA-256
  `10C703AC30B5B2E75CCF3B351252E59FEC023A1D3515DDB878ACBCAB4D9C71BB`.

### v0.2 — residue, hold/quarantine, ratification, and platform correction

- Selected restricted-fixture residue Model A.
- Rejected current-key NotFound as zero-residue proof.
- Added finite synthetic hold/quarantine paths and a separate ratification
  record.
- Required later runnable-platform resolution.

## 0. Repository evidence and supersession

| Evidence | Value |
| --- | --- |
| Repository/branch | `D:/Task/Remote Signing/TagEkyc` / `tip-88a-raw-export-policy-catalog-build` |
| Baseline | `672f3e4eeba24b6082bbb3c0696e597c8821e4c7` |
| v0.5 packet path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_v0_5.md` |
| Ratification-record path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_ratification_v0_4.md` |
| Superseded v0.4 | SHA-256 `B37D2537793B3D4347D3FB2D8016FC0E6CCF93237CD91F8872A32F6F067A55E5`; committed at `880a2005ad08a850dade2fcf476b12f5e912c3cd` |
| Superseded v0.3 | SHA-256 `56E52E5AAE7ABEC18EE3D93168256B2815B6D2DCA789AACB4CC4105D043E3EF5` |
| Superseded v0.2 | SHA-256 `10C703AC30B5B2E75CCF3B351252E59FEC023A1D3515DDB878ACBCAB4D9C71BB` |
| Superseded v0.1 | SHA-256 `2967117F082B0F19BF3F8E6B4B7D291F7BFD648C8964CA5502E5400BA52ACA60` |
| Locked Planning Brief | v0.17, SHA-256 `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC`, verify-only |
| HLD/LLD | Existing D9 synchronization remains sufficient; verify-only and byte-unchanged |

v0.5 supersedes v0.4, v0.3, v0.2, and v0.1 for future review or ratification.
Superseded files remain immutable history and grant no authority.

## 1. Authority and append-only ratification chain

This packet is governed by TIP-88C1 Planning Brief v0.17 D9, TIP-50 sections
6–9, and the TIP-51 bounded-packet precedent.

Authorization state is held only by versioned ratification records. Every
review binding, activation, invalidation, or revocation creates a new record
version with:

```text
RatificationRecordVersion
PreviousRatificationRecordSHA256
PreviousRatificationRecordRepositoryCommit
StateSequence
PacketSHA256
TransitionType
TransitionActor
TransitionAtUtc
```

Silent in-place mutation of an authoritative record is forbidden. A record
binds backward and sideways only. It never contains or predicts the hash of the
Git commit that contains its own bytes.

The git commit chain is normative for this ledger. Each ordinary successor
record binds the exact `PreviousRatificationRecordSHA256` and
`PreviousRatificationRecordRepositoryCommit`. That predecessor commit must
contain the predecessor blob at its recorded path and must be in the successor
record's containing-commit ancestry.

The v0.1–v0.3 records are `SUPERSEDED_INACTIVE_HISTORY`. Their historical
`UNCOMMITTED_PRE_RATIFICATION` sentinel makes those records and every
pre-re-anchor descendant ineligible to grant authority. It does not erase their
provenance.

Exactly one `COMMITTED_REANCHOR` may begin the activation-eligible segment. It
must bind the exact v0.3 record SHA-256
`17422B760C93BFDF7179629377883327D5BC98EE4722796C7C59C82C283E6860`,
its exact path, and containing commit
`880a2005ad08a850dade2fcf476b12f5e912c3cd`. The re-anchor record becomes a
valid predecessor only after its own blob is committed and a later successor
verifies that containing commit from Git. The historical sentinel's
disqualification does not propagate beyond this verified re-anchor. The
re-anchor remains inactive, cannot contain a review or Homeowner approval by
inference, and never activates evidence by itself. `NONE` is reserved for the
true original genesis and is forbidden for this re-anchor.

A future Build Brief must bind:

```text
RecordPath
RecordBlobSHA256
ContainingRepositoryCommit
```

This is how the final active record's containing commit is bound when it has no
successor yet: the Build Brief reads Git directly and verifies that
`ContainingRepositoryCommit` contains `RecordBlobSHA256` at `RecordPath`, that
the record is active and binds this exact packet SHA-256, and that the commit
descends from the predecessor chain. `ContainingRepositoryCommit` is never
required to live inside the record blob it identifies. Packet/record drafting,
review, bundling, or commit does not itself activate fixture evidence.

## 2. TIP Analytical Summary / Intent Ledger

### Intent

Close D9's fixture-evidence packet without allowing scope drift, undefined
clocks, mutable authorization history, or green-but-incomplete residue claims.

### Expected Outcome

One exact run uses one fresh empty bucket, every deadline remains enforceable
after harness failure, every cleanup path has a fixed actor/transition/proof,
and every normative term has one registered scope and failure behavior.

### Accepted Decisions

| Decision | Reason | Scope impact | Non-claim |
| --- | --- | --- | --- |
| Fresh bucket per evidence run | Whole-bucket claims require whole-bucket visibility. | One run, one bucket, whole-bucket enumeration, delete after proof. | No production bucket topology. |
| Persist absolute clocks at run open | A missing graceful terminal event cannot erase deadlines. | Watchdog can settle abandoned runs independently. | No runtime implementation exists. |
| Append-only record chain with normative git ancestry | Authorization history must not be silently rewritten. | Every state transition creates a committed record version. | This draft is not committed or active. |
| Register all normative terms | Cross-section scope/anchor drift must be mechanically detectable. | Unregistered obligation term is a defect. | Register is planning, not capability proof. |
| Cleanup-pending run states | Terminal states cannot coexist with open lifecycle work. | Graceful and abandoned terminal CASes share one no-open-work predicate. | No runtime state machine exists. |
| Pre-authorized expected fixture fault | A real integrity failure must never green a deliberate negative-test cell. | The immutable authorization binds exact operation, capability, executing identity, source state and revision. | No fault injection is authorized. |
| Joint-satisfiability tables | Definition completeness does not prove obligations can coexist. | All deadline relations and closed states receive a proof row. | Tables are design evidence, not runtime proof. |
| Preserve Gate A/B/C and ART-003 separation | Persistence, readability, and package completeness remain different gates. | Fixture dispositions cannot close later gates. | No real-artifact or package claim. |
| Committed authority re-anchor | Historical sentinel wording was ambiguous after the inactive records were committed. | One provenance-preserving re-anchor starts the eligible segment without rewriting history. | Re-anchor is not activation. |
| DURABLE-OBJECT capability alignment | Gate A must not merge application posture with cleanup administration. | Writer, reconciler, lifecycle, posture probe and cleanup admin remain distinct; assembly is deferred. | No production IAM or provider qualification. |
| Retention authorization deadline | Time cannot safely override hold, quarantine or residue. | Expiry irreversibly fails the run, opens or escalates cleanup debt and permits only safe delete-only cleanup. | No physical deletion guarantee. |

### Rejected / Deferred Branches

| Branch | Disposition | Reason | Gate |
| --- | --- | --- | --- |
| Shared bucket/run-prefix residue proof | Rejected | Other-prefix residue could be invisible. | Reopen only by reviewed amendment. |
| Current-key NotFound as residue proof | Rejected | Versions, markers, or multipart parts may remain. | Whole-bucket Model A proof. |
| Mutable ratification record | Rejected | Reviewer/verdict/revocation history could be rewritten. | Versioned chain. |
| Real Raw BIO or production provider | Deferred and unauthorized | Real lifecycle and qualification remain open. | Gate B/C and production review. |
| C2 package completeness | Not applicable | No C2 package use exists here. | `ART-003`. |

### Debt / Gap Impact

| Gap | Action | Result | Carry-forward |
| --- | --- | --- | --- |
| Bucket scope contradiction | Fresh bucket + whole-bucket identity. | One proof scope. | Build Brief pins provider commands. |
| Undefined terminal anchor | Absolute clocks + total terminal function + watchdog. | Crash-safe deadlines. | Implementation proof still absent. |
| Asymmetric timeout cleanup | Four explicit transition rows. | No actor/action inference. | Mutation proof belongs to Build Brief. |
| Mutable/self-referential ratification | New version per transition, committed re-anchor, predecessor commit binding, and external final-record Git verification. | Append-only authorization history without self-reference or sentinel ambiguity. | Record remains inactive. |
| Deadline conflict | Bucket deadline is the bounded maximum of normal and every open debt deadline. | Deletion cannot become due before legitimate cleanup debt. | Missed hard cap remains failure/debt. |
| Terminal-with-open-work conflict | Cleanup-pending states plus shared terminal CAS predicate. | No terminal run contains an active lifecycle obligation. | Watchdog cleanup remains required. |
| Quarantine orphan-close/false-green | Expected branch has `QuarantineCleaned` and no debt; failure branches open debt first; expected fault is pre-bound. | Every close has an open and genuine faults fail irreversibly. | Real integrity handling remains Gate B. |
| Stale object-custody graph | Reconcile Gate A to DURABLE-OBJECT v0.5. | Exact five-capability qualification graph plus actor-separated deletion evidence. | R2, assembly and production remain deferred. |
| False physical hard-cap claim | Treat 24 hours as maximum authorization, not guaranteed deletion. | Exact breach event, failure and debt escalation while safe cleanup continues. | No hold/quarantine bypass. |
| Real persistence/readability/package | Preserve open. | No real capability. | Gate B, Gate C, C2. |

### Dispatch Readiness

- Build Brief: **not authorized**.
- Implementation/evidence execution: **not authorized**.
- Remaining gates: two clean reviews, committed re-anchor followed by an active
  ratification-chain record, platform pins, retry-accounting gate, pending D1/D3-D9, Gate B, Gate
  C, C2 `ART-003`, and production/legal/operational gates.

## 3. TIP-50 master packet fields

| Required field | C1 v0.5 value |
| --- | --- |
| Packet id/name | `TIP-88C1-GOV-ART-FIXTURE-EVIDENCE-v0.5` |
| Target gates | The exact ten-row D9 table in section 4 |
| Purpose | Permit only a later Build Brief to request bounded generated fixture evidence |
| Scope boundary | Generated non-biometric fixture bytes; application-encrypted ciphertext; one fresh fixture bucket per evidence run |
| Environment boundary | Isolated non-production reference environment; no patient, production, shared SignFlow, or general-development storage |
| Actor/reviewer boundary | Exact roles and identities in sections 8 and 12 |
| Object classes | Fixture plaintext in bounded memory; encrypted objects; descriptors; hold/quarantine/debt/audit records |
| Data classification | Restricted non-patient fixture data; credentials/keys/locators/object bytes excluded from general outputs |
| Allowed evidence use | Single-part provider mechanics, exact reads, deny paths, lifecycle transitions, and whole-bucket residue proof |
| Forbidden uses | Multipart, versioning, Object Lock, provider retention, real Raw BIO, public access, production/readiness/package claims |
| Dependency gates | D9/TIP-50 Gate A only; Gate B, Gate C, C2, retry accounting, and pending decisions remain open |
| Required inputs | Packet/record hashes; run anchors; bucket authorizations; image/platform pins; identity matrix; lifecycle and residue evidence |
| Non-success handling | Any indeterminate registered term emits its registered failure code; unresolved lifecycle/debt/residue is failure |
| Retention/expiry | Absolute deadlines in section 7; no bare event-relative obligation |
| Purge/disposal | Exact CAS/authorization/delete/proof paths in section 8 |
| Legal-hold impact | Synthetic fixture semantics only; no legal meaning |
| Access/audit/security | Least privilege, whole-fixture-bucket evidence identity, redacted metadata-only records |
| Raw-payload posture | Default deny outside generated fixture bytes; provider receives encrypted fixture ciphertext only |
| Provider-specific posture | Exact top-level digest plus later platform/child/config pins; no production approval |
| Audit/review record | Run/packet/record/commit/platform/posture/transition/residue bindings |
| Validation evidence | Later Build Brief pins commands; this packet pins required observations |
| Approval criteria | Two clean reviews, active committed record chain, exact pins, closed lifecycles, whole-bucket zero residue, bucket deletion |
| Invalidation criteria | Any registered indeterminate term, scope/pin/record drift, forbidden input/access/claim, missed absolute deadline, or residue |
| Revalidation trigger | Any packet, provider, platform, role, scope, clock, lifecycle, gate, or chain change |
| STOP/RRI | Any blank/missing register cell, population mismatch, unregistered obligation term, inferred actor/action/deadline, or forbidden authority |

## 4. Exact GOV/ART disposition table

| Gate | Owner | Draft disposition |
| --- | --- | --- |
| `GOV-001` | `Homeowner` + `C1GovernanceOwner` | `CARRIED_OPEN`; traceability only |
| `ART-009` provider raw-payload default deny | `C1GovernanceOwner` + `SecurityReviewer` | Gate A generated-fixture exception proposed; Gate B open |
| `ART-001` storage boundary | `ReferenceEnvironmentOwner` + `C1AdapterOwner` | Fresh fixture bucket only; real persistence open |
| `ART-002` reference resolution | `C1ResolverOwner` | Fixture conformance only; Gate C open; no `Available` descriptor |
| `ART-008` orphan handling | `C1ReconcilerOwner` | Whole-fixture-bucket reconciliation only; real gate open |
| `ART-004` retention/expiry | `C1LifecycleOwner` | Absolute fixture clocks only; real policy open |
| `ART-005` purge/disposal | `C1LifecycleOwner` | Exact fixture cleanup only; real disposal open |
| `ART-006` legal-hold synchronization | `FixtureHoldOwner` + `DataGovernanceReviewer` | Synthetic fixture conflict/release only; real hold open |
| `ART-007` access/audit/security | `SecurityReviewer` + `ReferenceEnvironmentOwner` | Fixture least-privilege evidence only; real readiness open |
| `ART-003` final package completeness (C2) | `C2Owner` | `NOT_APPLICABLE`; no package/completeness claim |

Every row is non-patient and reference-environment-only. None closes or weakens
real-artifact, production, legal/compliance, audit, security, readiness,
performance, retention-policy, provider-qualification, evidence-availability,
or package-completeness gates.

### 4.1 Nine composite provider-evidence fields

| Field | Exact Gate A boundary | What remains open |
| --- | --- | --- |
| Storage boundary and environment | Model A isolated non-production fresh bucket, exact image/platform pin, dedicated identities, only encrypted generated fixture ciphertext. | Production topology/durability/provider qualification. |
| Resolver semantics | Exact fixture ciphertext inspect/read by restricted identity; every missing/expired/deleted/unauthorized/quarantined/orphan state is non-success; no C1 `Available`. | Gate C and runtime/raw readability. |
| Orphan detection/reconciliation | Durable fixture reservation plus deterministic object identity and whole-bucket enumeration converge to exact retained fixture state or zero residue; no blind deletion. | Real crash recovery and Gate B `ART-008`. |
| Retention/expiry | Maximum authorized fixture-retention deadline of 24 hours, one-hour normal cleanup target, shorter absolute hold/quarantine deadlines, no extension; residue at expiry is an irreversible evidence failure and cleanup debt. | Real policy and Gate B `ART-004`. |
| Purge/disposal and cleanup | Exact delete plus live/version/delete-marker/multipart enumeration; NotFound alone insufficient. | Real authority and Gate B `ART-005`. |
| Legal-hold conflict disposition | Exact synthetic hold/release lifecycle in section 8; no legal meaning. | Real hold authority/sync and Gate B `ART-006`. |
| Least-privilege access | Ingress/writer exact conditional create/write; reconciler exact-key inspect/read; lifecycle exact single-object delete; posture probe bucket-posture reads only; fixture evidence cleanup admin exact bucket create/whole-bucket enumerate/empty-bucket delete outside application DI. `AssemblyIdentity` is inactive and deferred. | Production IAM/key authority, R2/assembly and Gate B `ART-007`. |
| Audit/security evidence | Metadata-only run-bound posture, operation, lifecycle, deny, and residue records; no secret/raw/object content. | Production audit/security monitoring/readiness. |
| Raw-payload default deny | Generated non-patient fixture plaintext is transient in the bounded process; only encrypted fixture ciphertext reaches the provider. | Every real Raw BIO and Gate B `ART-009`. |

## 5. Reference image and runnable-platform contract

```text
Repository: minio/minio
Release: RELEASE.2025-09-07T16-13-09Z
TopLevelDigest:
sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e
```

The top-level digest alone is insufficient for reproducibility. A later Build
Brief and evidence record must bind `PlatformOS`, `PlatformArchitecture`,
`TopLevelDigest`, `ResolvedChildManifestDigest`, and
`ResolvedImageConfigDigestOrImageId`. Any mismatch fails before provider use.
No registry resolution is authorized here.

## 6. Option A — one fresh whole-enumerated bucket per evidence run

### 6.1 Bucket identity and privilege

`BucketNamingPattern` is:

```text
tagekyc-c1-fixture-{EvidenceRunId:N-lowercase}
```

`BucketName` is derived once from `EvidenceRunId`; it is not caller-selected.
The run creates exactly one new `FixtureBucket`. Reuse, pre-existence, name
collision, or another run's object is failure.

The exact Gate A qualification mapping is:

| DURABLE-OBJECT capability | Gate A identity | Exact authority |
| --- | --- | --- |
| Writer | `IngressIdentity` | one conditional single-part create/write for one internally derived exact key |
| Reconciler | `ReconciliationIdentity` | exact-key metadata inspect/read only |
| Lifecycle | `FixtureLifecycleIdentity` | exactly one internally derived single-object delete only |
| PostureProbe | `PostureProbeIdentity` | bucket versioning, Object Lock, lifecycle configuration and public-access posture reads only |
| Fixture cleanup administrator | `FixtureBucketEvidenceIdentity` | exact derived bucket create, whole-bucket residue enumeration and empty-bucket delete; outside application DI |

`AssemblyIdentity` is inactive in this qualification run. It gains no
credential, provider, read or evidence authority until a later R2/assembly
packet and Build Brief are separately ratified.

`FixtureBucketEvidenceIdentity` cannot inspect or read object content, perform
application posture probes, list/access/create/delete another bucket, derive
another name, access production, read plaintext, or become a runtime
application identity. `PostureProbeIdentity` cannot create, enumerate objects,
read object content, write or delete. `FixtureRunPrefix` is fixed to `objects/`
only for key organization; it never limits residue enumeration.

### 6.2 Model A preflight

Before object write, whole-bucket evidence must prove:

```text
VersioningPosture = DisabledNeverEnabled
ObjectLockPosture = Disabled
ProviderRetentionPosture = None
LifecycleConfigurationPosture = NoLifecycleRules
PublicAccessPosture = NoPublicAccess
MultipartPosture = Prohibited
FixtureBucketState = EmptyVerified
```

The bucket must contain no live object, historical version, delete marker,
multipart upload, or part. Unknown, inaccessible, suspended-with-history, any
enabled or disabled lifecycle rule, public ACL/policy, or indeterminate access
posture fails closed. `NoLifecycleRules` means the provider's typed
no-configuration result or an exact empty rule collection; any rule is
prohibited. `NoPublicAccess` means both bucket ACL and bucket policy prove no
public access.

### 6.3 Terminal proof and bucket deletion

After every branch, whole-bucket enumeration proves:

```text
no live object
AND no historical object version
AND no delete marker
AND no active multipart upload
AND no uploaded part
```

Only then may `FixtureBucketEvidenceIdentity` CAS-publish
`ModelAZeroResidueProved`, consume `BucketDeleteAuthorization`, delete the
exact empty bucket, and publish `BucketDeleted`. Current-key `GET`/`HEAD`
NotFound is never sufficient alone.

Gate A proves both exact deletion evidence kinds. `DeleteAcknowledged` is
accepted only from `FixtureLifecycleIdentity` after one exact delete receives
an authoritative provider acknowledgement. `PositiveAbsenceConfirmed` is
accepted only from `ReconciliationIdentity` after the request-body producer is
quiesced and two bounded exact-key absence observations agree. They are distinct
`DeletionEvidenceKind` values, use distinct evidence preimages, and cannot be
substituted or asserted by the opposite identity.

The production prohibition on provider-wide listing remains unchanged.

## 7. Persisted run anchors, total terminal function, and watchdog

### 7.1 Run-open transaction

`RunOpenIdentity` persists atomically:

```text
EvidenceRunId = fresh RFC-4122 v4 UUID
EvidenceRunStartedAtUtc = database UTC
MaximumEvidenceRunDuration = 30 minutes
EvidenceRunDeadlineUtc =
  EvidenceRunStartedAtUtc + MaximumEvidenceRunDuration
EvidenceRunState = Open
StateSequence = previous sequence + 1
```

These values are immutable. No harness timestamp is authoritative.

### 7.2 Total terminal anchor

`EvidenceRunTerminalizedAtUtc` is the nullable database UTC of any terminal
CAS—`GracefullyTerminalized` or `AbandonedTerminal`—and is null while either
cleanup-pending state remains. The total function is:

```text
EffectiveRunEndUtc =
  min(
    coalesce(EvidenceRunTerminalizedAtUtc, EvidenceRunDeadlineUtc),
    EvidenceRunDeadlineUtc
  )
```

It is therefore defined from persisted values even when the harness crashes.
Execution completion first CASes `Open` to
`ExecutionFinishedCleanupPending`. A graceful CAS may then change
`ExecutionFinishedCleanupPending` to `GracefullyTerminalized` only when all of
these predicates hold in the same transaction:

```text
no SyntheticHoldActive
AND no Quarantined or ReviewerCleanupAuthorized quarantine
AND no CleanupDebtOpen or CleanupDebtOverdue
AND no pending object disposition
AND ModelAZeroResidueProved
AND BucketDeleted
```

At `EvidenceRunDeadlineUtc`, `RunWatchdog` evaluates both active states. If no
lifecycle work is open it CASes `Open` or
`ExecutionFinishedCleanupPending` to `AbandonedTerminal`. If lifecycle work is
open it CASes either state to `AbandonedCleanupPending`, irrevocably fails the
evidence outcome, and continues cleanup. Only after the same no-open-work
predicate holds may `AbandonedCleanupPending` CAS to `AbandonedTerminal`.
Therefore no run reaches any terminal state while lifecycle work remains open.

### 7.3 Absolute deadline derivations

| Deadline | Persisted derivation |
| --- | --- |
| `EvidenceRunDeadlineUtc` | `EvidenceRunStartedAtUtc + 30 minutes` at run open |
| `HoldReleaseDeadlineUtc` | `min(SyntheticHoldAppliedAtUtc + 15 minutes, EvidenceRunDeadlineUtc)` |
| `QuarantineDispositionDeadlineUtc` | `min(QuarantinedAtUtc + 15 minutes, EvidenceRunDeadlineUtc)` |
| `NormalCleanupDueAtUtc` | `EffectiveRunEndUtc + 1 hour` |
| `HoldCleanupDebtDueAtUtc` | `min(HoldReleaseDeadlineUtc + 1 hour, EvidenceRunDeadlineUtc + 1 hour)` |
| `QuarantineCleanupDebtDueAtUtc` | `min(QuarantineDispositionDeadlineUtc + 1 hour, EvidenceRunDeadlineUtc + 1 hour)` |
| `FixtureExpiresAtUtc` | `FixtureObjectCreatedAtUtc + 24 hours`; immutable maximum authorized fixture-retention deadline; it never overrides hold/quarantine or safe-delete CAS |
| `BucketDeleteHardCapUtc` | `EvidenceRunDeadlineUtc + 1 hour` |
| `BucketDeleteDeadlineUtc` | `max(NormalCleanupDueAtUtc, every nonclosed HoldCleanupDebtDueAtUtc, every nonclosed QuarantineCleanupDebtDueAtUtc)`; nonclosed includes `CleanupDebtOpen` and `CleanupDebtOverdue`, and empty debt sets contribute `NormalCleanupDueAtUtc` |
| `WatchdogNextEvaluationAtUtc` | earliest unresolved persisted deadline among run, hold, quarantine, cleanup debt, fixture expiry, and bucket deletion |

The derivations prove:

```text
NormalCleanupDueAtUtc <= BucketDeleteHardCapUtc
HoldCleanupDebtDueAtUtc <= BucketDeleteHardCapUtc
QuarantineCleanupDebtDueAtUtc <= BucketDeleteHardCapUtc
BucketDeleteDeadlineUtc <= BucketDeleteHardCapUtc
```

Source deadlines are immutable. `BucketDeleteDeadlineUtc` is re-derived and
persisted whenever a nonclosed cleanup obligation changes; it may never exceed
`BucketDeleteHardCapUtc`. Time alone never permits deletion. Even after the
deadline or hard cap, `FixtureBucketEvidenceIdentity` may delete the bucket
only after no active hold, quarantine, cleanup debt, or pending object
disposition remains and whole-bucket enumeration has published
`ModelAZeroResidueProved`.

If any object or lifecycle residue exists at `FixtureExpiresAtUtc`, the
watchdog atomically records `FixtureExpiryBreached`, emits
`FIXTURE_EXPIRY_BREACH`, irreversibly fails the evidence run, moves any
nonterminal run to `AbandonedCleanupPending`, and creates `CleanupDebtOpen`
unless the same residue is already covered by an open or overdue debt. The
transition is idempotent and cannot extend the deadline. Cleanup continues
through the registered safe delete-only paths; expiry never bypasses hold,
quarantine, actor, revision, zero-residue or bucket-delete guards. An expired
run can never PASS.

### 7.4 Independent watchdog/reconciler

`RunWatchdog` uses database UTC and persisted state only. It:

1. wakes no later than `WatchdogNextEvaluationAtUtc`;
2. moves an active run at `EvidenceRunDeadlineUtc` to
   `AbandonedTerminal` only when no lifecycle work is open, otherwise to
   `AbandonedCleanupPending`;
3. CASes expired active holds to `SyntheticHoldReleaseTimeout`;
4. CASes expired quarantines to `QuarantineDispositionTimeout`;
5. creates the exact delete-only `CleanupDebtOpen` with its absolute due time;
6. invokes only the registered authorizer/executing-identity workflow; and
7. records `FixtureExpiryBreached` at the immutable fixture expiry when residue remains;
8. CASes missed debt to `CleanupDebtOverdue`, records a hard-cap breach, keeps
   the run in `AbandonedCleanupPending`, withholds bucket deletion, and
   continues delete-only reconciliation and escalation until cleanup closes.

Restart re-derives the next evaluation from persisted timestamps. Harness
silence cannot postpone or erase an obligation.

## 8. Four symmetric hold/quarantine branches

### 8.1 Expected-fixture-fault gate

Before any deliberate negative-test operation, `FixtureEvidenceReviewer`
creates one immutable, single-use `ExpectedFixtureFault` containing:

```text
TestCaseId
RunId
ObjectId
ExpectedFaultType
ExpectedFaultOperation
ExpectedFaultCapability
ExpectedFaultExecutingIdentity
ExpectedPreState
ExpectedPreStateRevision
AuthorizedAtUtc
PacketSHA256
```

`RunId` must equal `EvidenceRunId`; `ObjectId` must equal
`FixtureObjectIdentity`; `PacketSHA256` must equal this packet's digest; and
`AuthorizedAtUtc` must be earlier than the database-UTC
`FaultOperationStartedAtUtc`. A quarantine matches only when every immutable
field, the observed integrity-fault type, actual authenticated
operation/capability/identity, and the current run/object/source-state/
source-revision/packet bindings are exact. The Build Brief must define a closed
mapping from each `ExpectedFaultType` to its sole permitted operation,
capability, identity and predecessor state. Authorization is consumed once and
cannot be replayed or broadened. Any mismatch takes the unexpected-fault branch.

Only a matching `ExpectedFixtureFault` may enter the reviewer-cleanup branch
and later allow that deliberate negative-test cell to PASS. A quarantine with
no exact pre-authorized match atomically CASes to
`UnexpectedFaultCleanupRequired`, opens `CleanupDebtOpen`, irrevocably fails
the run, and follows delete-only cleanup. Later cleanup can close debt and
remove residue but can never rehabilitate the evidence result.

### 8.2 Symmetric branch matrix

Every branch has an `Authorizer`, `ExecutingIdentity`, conditional state
transition, and terminal proof.

| Branch | Authorizer | Executing identity | CAS/state transition | Terminal proof |
| --- | --- | --- | --- | --- |
| Hold released on time | `FixtureHoldOwner` issues run/object/hold/packet-bound `SyntheticHoldReleaseId` | `FixtureLifecycleIdentity` | `SyntheticHoldActive → SyntheticHoldReleased` before `HoldReleaseDeadlineUtc` | Delete exact object/residue, then whole-bucket enumeration publishes `ModelAZeroResidueProved`; deliberate hold cell may PASS |
| Hold release timeout | `FixtureHoldOwner` issues cleanup-only `SyntheticHoldCleanupReleaseId` and `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `SyntheticHoldActive → SyntheticHoldReleaseTimeout → CleanupDebtOpen → CleanupDebtClosed` | Delete by `HoldCleanupDebtDueAtUtc`, whole-bucket zero residue, debt closed; run remains failed |
| Quarantine reviewer cleanup | A matching pre-operation `ExpectedFixtureFault` exists; `FixtureEvidenceReviewer` issues `FixtureQuarantineDispositionId` and `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `Quarantined → ReviewerCleanupAuthorized → QuarantineCleaned` before `QuarantineDispositionDeadlineUtc`; no cleanup-debt aggregate is opened or closed | Exact object/residue delete + whole-bucket zero residue; only the matched deliberate-fault branch may PASS |
| Quarantine timeout cleanup | `ReferenceEnvironmentOwner` issues run/object/quarantine/packet-bound `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `Quarantined → QuarantineDispositionTimeout → CleanupDebtOpen → CleanupDebtClosed` | Delete by `QuarantineCleanupDebtDueAtUtc`, whole-bucket zero residue, debt closed; run remains failed |

All authorizations are immutable, single-run, single-object, single-state-
revision, packet-SHA-bound records. Cross-run/replay/stale revision fails.
Timeout cleanup never converts a failed run into PASS. No actor may choose
retain, export, restore, retry, reuse, re-encrypt, or another physical action.

The unexpected-fault branch is not a fifth PASS-capable branch:

```text
Quarantined
→ UnexpectedFaultCleanupRequired + CleanupDebtOpen
→ CleanupDebtClosed after exact delete and zero-residue proof
```

`RunWatchdog` performs the first CAS when it observes no matching
`ExpectedFixtureFault`; `ReferenceEnvironmentOwner` authorizes exact
delete-only cleanup; `FixtureLifecycleIdentity` deletes object/residue. The run
remains failed.

Synthetic holds have no real legal meaning. `SyntheticHoldAppliedAtUtc`,
release/cleanup authorization times, and CAS times are database UTC.

## 9. Distinct phase gates

- **Gate A:** bounded generated fixture evidence under this packet, an active
  committed ratification chain, future Build Brief, exact platform pin, and
  whole-bucket Model A contract.
- **Gate B:** before real persistence, reviewed evidence resolves `ART-001`,
  `ART-004`, `ART-005`, `ART-006`, `ART-007`, `ART-008`, and `ART-009`.
  Nothing here resolves them.
- **Gate C:** `ART-002` closes before any descriptor becomes resolver-readable
  or `Available`, before any raw-source read, and before availability reliance.
- **C2:** `ART-003` is outside Gates A/B/C and remains not applicable here.

## 10. Lifecycle deletion-versus-state

| State | Fixture rule | Deferred boundary |
| --- | --- | --- |
| Active B4 attempt | Settle/release synthetic owner before delete authorization | Real B4 authority/lease remains Gate B |
| Sealed C1 assembly | Immutable until fixture evidence finalization and registered cleanup | Real sealed-source lifecycle remains Gate B |
| C2 preparation/package | None created; claimed C2 reference is failure | C2 `ART-003` |
| Synthetic hold | Exact section 8 branch | Real legal hold remains Gate B |
| Corruption quarantine | Exact section 8 branch | Real quarantine authority/retention remains Gate B |
| Post-withdrawal purge | Synthetic event only after owners/hold settle | Real withdrawal/purge remains Gate B |

### 10.1 Deadline joint-satisfiability table

This is the complete set of required same-run deadline ordering relations.
Pairs not listed have no ordering dependency. `max(open set)` below contributes
`NormalCleanupDueAtUtc` when the set is empty.

| Pair or family | Required relation | Derivation/proof |
| --- | --- | --- |
| Run and hold release | `HoldReleaseDeadlineUtc <= EvidenceRunDeadlineUtc` | The hold deadline is a `min` with the run deadline. |
| Run and quarantine disposition | `QuarantineDispositionDeadlineUtc <= EvidenceRunDeadlineUtc` | The quarantine deadline is a `min` with the run deadline. |
| Effective end and run | `EffectiveRunEndUtc <= EvidenceRunDeadlineUtc` | The total function is a `min` with the run deadline. |
| Normal cleanup and hard cap | `NormalCleanupDueAtUtc <= BucketDeleteHardCapUtc` | Add one hour to the preceding effective-end relation. |
| Hold debt and hard cap | Every `HoldCleanupDebtDueAtUtc <= BucketDeleteHardCapUtc` | The debt derivation is a `min` with run deadline plus one hour. |
| Quarantine debt and hard cap | Every `QuarantineCleanupDebtDueAtUtc <= BucketDeleteHardCapUtc` | The debt derivation is a `min` with run deadline plus one hour. |
| Normal cleanup and bucket delete | `NormalCleanupDueAtUtc <= BucketDeleteDeadlineUtc` | Normal cleanup is an operand of the bucket-deadline `max`. |
| Hold debt and bucket delete | Every nonclosed `HoldCleanupDebtDueAtUtc <= BucketDeleteDeadlineUtc` | Every open or overdue hold-debt deadline is an operand of the same `max`. |
| Quarantine debt and bucket delete | Every nonclosed `QuarantineCleanupDebtDueAtUtc <= BucketDeleteDeadlineUtc` | Every open or overdue quarantine-debt deadline is an operand of the same `max`. |
| Bucket delete and hard cap | `BucketDeleteDeadlineUtc <= BucketDeleteHardCapUtc` | Every operand of the `max` is at or before the hard cap. |
| Bucket delete and fixture expiry | `BucketDeleteDeadlineUtc < FixtureExpiresAtUtc` | The hard cap is at most run start plus 90 minutes; an in-run object is created no earlier than run start and its authorization expires 24 hours later. |
| Fixture expiry and breach | `FixtureExpiryBreached` is recorded at or after `FixtureExpiresAtUtc` exactly when residue remains | The watchdog compares immutable database UTC, atomically fails the run and opens or reuses exact cleanup debt without authorizing deletion. |
| Branch debt alias | `CleanupDebtDueAtUtc = HoldCleanupDebtDueAtUtc` or `QuarantineCleanupDebtDueAtUtc` for its exact branch | Debt creation persists the selected immutable branch deadline. |
| Watchdog and all unresolved deadlines | `WatchdogNextEvaluationAtUtc <= each unresolved deadline` | It is the minimum of the complete unresolved deadline set. |

Coverage is 14/14 required relation families. In particular, the bucket-delete
deadline is never earlier than a still-open hold or quarantine cleanup-debt
deadline. A missed deadline is an evidence failure and debt escalation, never
permission to violate an active hold or delete a non-empty bucket.

### 10.2 Closed-state reachability table

| Closed state set | State | Exact creating transition |
| --- | --- | --- |
| `EvidenceRunState` | `Open` | Run-open transaction creates the persisted run. |
| `EvidenceRunState` | `ExecutionFinishedCleanupPending` | Execution-complete CAS from `Open`. |
| `EvidenceRunState` | `GracefullyTerminalized` | CAS from `ExecutionFinishedCleanupPending` only after the no-open-work predicate and `BucketDeleted`. |
| `EvidenceRunState` | `AbandonedCleanupPending` | Watchdog CAS from `Open` or `ExecutionFinishedCleanupPending` at run deadline when lifecycle work is open. |
| `EvidenceRunState` | `AbandonedTerminal` | Watchdog CAS from an active state at run deadline when no work is open, or CAS from `AbandonedCleanupPending` after cleanup and bucket deletion. |
| `SyntheticHoldState` | `SyntheticHoldActive` | Hold-create CAS with `SyntheticHoldId` and applied time. |
| `SyntheticHoldState` | `SyntheticHoldReleased` | On-time authorized release CAS from `SyntheticHoldActive`. |
| `SyntheticHoldState` | `SyntheticHoldReleaseTimeout` | Watchdog deadline CAS from `SyntheticHoldActive`; opens `CleanupDebtOpen`. |
| `QuarantineState` | `Quarantined` | Integrity-mismatch CAS creates `QuarantineId` and mismatch evidence. |
| `QuarantineState` | `ReviewerCleanupAuthorized` | CAS from `Quarantined` only after exact `ExpectedFixtureFault` match and reviewer authorization. |
| `QuarantineState` | `QuarantineCleaned` | CAS from `ReviewerCleanupAuthorized` after object/residue deletion and zero-residue proof; no debt exists. |
| `QuarantineState` | `UnexpectedFaultCleanupRequired` | CAS from `Quarantined` when no exact expected-fault match; atomically opens `CleanupDebtOpen`. |
| `QuarantineState` | `QuarantineDispositionTimeout` | Watchdog deadline CAS from `Quarantined`; atomically opens `CleanupDebtOpen`. |
| `CleanupDebtState` | `CleanupDebtOpen` | Atomic companion to hold timeout, quarantine timeout, unexpected fault, or another registered failure transition. |
| `CleanupDebtState` | `CleanupDebtOverdue` | Watchdog deadline CAS from `CleanupDebtOpen`. |
| `CleanupDebtState` | `CleanupDebtClosed` | CAS only from `CleanupDebtOpen` or `CleanupDebtOverdue`, after exact deletion and zero-residue proof. |
| `FixtureBucketState` | `EmptyVerified` | Whole-bucket preflight CAS after Model A empty/posture proof. |
| `FixtureBucketState` | `ModelAZeroResidueProved` | Whole-bucket terminal-enumeration CAS after all object dispositions finish. |
| `FixtureBucketState` | `BucketDeleted` | `FixtureBucketEvidenceIdentity` deletes the exact empty bucket under `BucketDeleteAuthorization`, then records confirmation. |

Coverage is 19/19 states across all five closed sets. Every listed state has a
creating transition. Every `CleanupDebtClosed` has a preceding
`CleanupDebtOpen`; the reviewer-cleanup success path never touches the cleanup
debt aggregate. There is no orphan close.

## 11. Non-authorization

This packet does not authorize:

- a Build Brief, implementation, evidence run, source/test/schema/migration/API,
  adapter, resolver, worker, provider, key, project, or deployment change;
- registry/provider/object-store/MinIO/KMS/vault/HSM access;
- real Raw BIO capture, persistence, read, resolution, reuse, export, or
  delivery;
- production storage/key provider, credentials, deployment, legal basis,
  controller decision, consent wording, retention policy, real hold, or
  compliance position;
- production qualification, support, licensing, security, audit, HA,
  backup/restore, performance, readiness, certification, or capability;
- closure/ratification of D1 or D3–D9, the acceptance surface, producer
  allowlist, CaptureAgent change, retry-accounting gate, Gate B, Gate C, or C2;
  or
- ratification, commit, push, merge, PR, deployment, or activation.

## 12. Normative term and scope register

Only identifiers present in the population below are normative defined terms
for this packet. External gate codes and failure-code literals are fixed
external/output identifiers, not register terms. An identifier used
normatively in an obligation but absent from both this population and its
register is a defect.

### 12.1 Ordinally sorted normative-term population
<!-- TERM_POPULATION_BEGIN -->
```text
ART-001
ART-002
ART-003
ART-004
ART-005
ART-006
ART-007
ART-008
ART-009
AbandonedCleanupPending
AbandonedTerminal
AssemblyIdentity
AuthorizedAtUtc
Authorizer
Available
BucketCreateAuthorization
BucketCreatedAtUtc
BucketDeleteAuthorization
BucketDeleteDeadlineUtc
BucketDeleteHardCapUtc
BucketDeleted
BucketDeletedAtUtc
BucketName
BucketNamingPattern
C1AdapterOwner
C1GovernanceOwner
C1LifecycleOwner
C1ReconcilerOwner
C1ResolverOwner
C2Owner
CARRIED_OPEN
COMMITTED_REANCHOR
CleanupDebt
CleanupDebtClosed
CleanupDebtClosedAtUtc
CleanupDebtDueAtUtc
CleanupDebtOpen
CleanupDebtOverdue
CleanupDebtState
ContainingRepositoryCommit
DataGovernanceReviewer
DeleteAcknowledged
DeleteOnlyCleanupAuthorization
DeletionEvidenceKind
Disabled
DisabledNeverEnabled
EffectiveRunEndUtc
EmptyVerified
EvidenceRun
EvidenceRunDeadlineUtc
EvidenceRunId
EvidenceRunStartedAtUtc
EvidenceRunState
EvidenceRunTerminalizedAtUtc
ExecutingIdentity
ExecutionFinishedCleanupPending
ExpectedFaultCapability
ExpectedFaultExecutingIdentity
ExpectedFaultOperation
ExpectedFaultType
ExpectedFixtureFault
ExpectedPreState
ExpectedPreStateRevision
FaultOperationStartedAtUtc
FixtureBucket
FixtureBucketEvidenceIdentity
FixtureBucketState
FixtureEvidenceReviewer
FixtureExpiresAtUtc
FixtureExpiryBreached
FixtureHoldOwner
FixtureLifecycleIdentity
FixtureObjectCreatedAtUtc
FixtureObjectIdentity
FixtureQuarantineDispositionId
FixtureRunPrefix
GET
GOV-001
GracefullyTerminalized
HEAD
HoldCleanupDebtDueAtUtc
HoldReleaseDeadlineUtc
Homeowner
IndependentReviewer
IngressIdentity
LifecycleConfigurationPosture
MaximumEvidenceRunDuration
ModelAZeroResidue
ModelAZeroResidueProved
MultipartPosture
NOT_APPLICABLE
NoLifecycleRules
NoPublicAccess
None
NormalCleanupDueAtUtc
ObjectId
ObjectLockPosture
Open
PacketSHA256
PlatformArchitecture
PlatformOS
PositiveAbsenceConfirmed
PostureProbeIdentity
PreviousRatificationRecordRepositoryCommit
PreviousRatificationRecordSHA256
Prohibited
ProviderRetentionPosture
PublicAccessPosture
QuarantineCleaned
QuarantineCleanupDebtDueAtUtc
QuarantineDispositionDeadlineUtc
QuarantineDispositionTimeout
QuarantineId
QuarantineState
Quarantined
QuarantinedAtUtc
RatificationRecordVersion
ReconciliationIdentity
RecordBlobSHA256
RecordPath
ReferenceEnvironmentOwner
Residue
ResolvedChildManifestDigest
ResolvedImageConfigDigestOrImageId
ReviewerCleanupAuthorized
RunId
RunOpenIdentity
RunWatchdog
SUPERSEDED_INACTIVE_HISTORY
SecurityReviewer
StateSequence
SyntheticHold
SyntheticHoldActive
SyntheticHoldAppliedAtUtc
SyntheticHoldCleanupReleaseId
SyntheticHoldConflict
SyntheticHoldId
SyntheticHoldReleaseId
SyntheticHoldReleaseTimeout
SyntheticHoldReleased
SyntheticHoldReleasedAtUtc
SyntheticHoldState
TestCaseId
TopLevelDigest
TransitionActor
TransitionAtUtc
TransitionType
UNCOMMITTED_PRE_RATIFICATION
UnexpectedFaultCleanupRequired
VersioningPosture
WatchdogNextEvaluationAtUtc
```
<!-- TERM_POPULATION_END -->

### 12.2 Register

| Term | Exact definition | Scope | Observer/evidence | Authorized actor | Persistence | Indeterminate failure code |
| --- | --- | --- | --- | --- | --- | --- |
| `ART-001` | D9 storage-boundary gate | Gate B real persistence; Gate A fixture boundary only | Governance reviewer from packet/record | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-002` | D9 resolver/readability gate | Gate C only; not Gate B | Governance reviewer from packet/record | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-003` | D9 C2 package-completeness gate | C2 only; outside A/B/C | C2/governance reviewer | `Homeowner` in C2 review | C2 packet chain, not this run | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-004` | D9 retention/expiry gate | Gate B real policy; fixture clocks Gate A only | Lifecycle/governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-005` | D9 purge/disposal gate | Gate B real disposal; fixture cleanup Gate A only | Lifecycle/governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-006` | D9 legal-hold synchronization gate | Gate B real hold; synthetic Gate A only | Governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-007` | D9 access/audit/security gate | Gate B real readiness; fixture evidence Gate A only | Security/governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-008` | D9 orphan-handling gate | Gate B real orphan lifecycle; fixture residue Gate A only | Reconciler/governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `ART-009` | D9 provider raw-payload default-deny gate | Gate B real payload; generated fixture exception Gate A only | Security/governance reviewer | `Homeowner` ratification only | Packet/ratification chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `AbandonedCleanupPending` | Irrevocably failed nonterminal run state while lifecycle work remains | One evidence run after run deadline | `RunWatchdog` from persisted state/deadlines | `RunWatchdog` CAS only | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `AbandonedTerminal` | Failed terminal run disposition created at run deadline only when no work is open, or after abandoned cleanup finishes | One evidence run; never success | `RunWatchdog` from run/lifecycle rows and database UTC | `RunWatchdog` guarded CAS only | Run state/history, append-only transition | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `AssemblyIdentity` | Deferred identity name for a later exact committed fixture-read proof; inactive here | No credential, provider or evidence authority in this qualification run | Later R2/assembly packet only | No current actor | Not provisioned by this packet | `FIXTURE_IDENTITY_INDETERMINATE` |
| `AuthorizedAtUtc` | Database UTC at which an expected fixture fault was authorized | One expected-fault record; strictly before operation start | Expected-fault repository/database UTC | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_TIME_INDETERMINATE` |
| `Authorizer` | Role allowed to issue the branch-specific immutable authorization | One transition row; no execution privilege implied | Transition record and capability manifest | Role named by section 8 | Authorization record | `FIXTURE_AUTHORIZER_INDETERMINATE` |
| `Available` | External C1 descriptor state whose entry requires Gate C closure | Runtime descriptor only; never produced or relied upon by Gate A | C1 descriptor repository under a later implementation | No Gate A actor | Not persisted by this packet/evidence run | `FIXTURE_GATE_C_NOT_CLOSED` |
| `BucketCreateAuthorization` | Immutable permit to create exact derived bucket name | One run/one bucket; no other name | Evidence identity from active record/run | `ReferenceEnvironmentOwner` | Authorization record before create | `FIXTURE_BUCKET_AUTH_INDETERMINATE` |
| `BucketCreatedAtUtc` | Database UTC observed after exact bucket creation | One fixture bucket | Evidence harness plus provider response/database UTC | `FixtureBucketEvidenceIdentity` records | Run/bucket record | `FIXTURE_BUCKET_TIME_INDETERMINATE` |
| `BucketDeleteAuthorization` | Immutable permit issued only after zero-residue CAS | One run/one bucket; delete only | Evidence identity from zero-residue transition | `ReferenceEnvironmentOwner` | Authorization record | `FIXTURE_BUCKET_AUTH_INDETERMINATE` |
| `BucketDeleteDeadlineUtc` | Maximum of normal cleanup and every nonclosed hold/quarantine debt deadline, never above hard cap | One fixture bucket/run | Watchdog from persisted run/debt rows | `RunWatchdog` monitors; `FixtureBucketEvidenceIdentity` deletes the exact empty bucket | Bucket/run record, re-derived on debt changes | `FIXTURE_DEADLINE_INDETERMINATE` |
| `BucketDeleteHardCapUtc` | Immutable absolute cap equal to run deadline plus one hour | One fixture bucket/run | Watchdog/reviewer from persisted run row | Run-open transaction derives; no actor extends | Run/bucket record | `FIXTURE_BUCKET_HARD_CAP_INDETERMINATE` |
| `BucketDeleted` | Terminal bucket disposition after provider confirms exact deletion | One fixture bucket | Evidence identity and whole-bucket proof | `FixtureBucketEvidenceIdentity` CAS | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `BucketDeletedAtUtc` | Database UTC of confirmed bucket deletion | One fixture bucket | Evidence identity/provider response/database UTC | `FixtureBucketEvidenceIdentity` records | Bucket state/history | `FIXTURE_BUCKET_TIME_INDETERMINATE` |
| `BucketName` | Exact lowercase name derived from naming pattern and run UUID | One fresh bucket; no caller choice | Run-open owner from persisted UUID | `FixtureBucketEvidenceIdentity` uses | Run/bucket record | `FIXTURE_BUCKET_NAME_INDETERMINATE` |
| `BucketNamingPattern` | `tagekyc-c1-fixture-{EvidenceRunId:N-lowercase}` | Reference environment; no production/general bucket | Governance reviewer from packet | Packet amendment only | Packet and run derivation version | `FIXTURE_BUCKET_NAME_INDETERMINATE` |
| `C1AdapterOwner` | Owner of fixture adapter contract/evidence | Gate A adapter only | Governance/capability manifest | Future Build Brief assignment | Review/evidence record | `FIXTURE_OWNER_INDETERMINATE` |
| `C1GovernanceOwner` | Owner maintaining packet/record consistency | TIP-88C1 governance only | Repository history | Homeowner assignment | Packet/record metadata | `FIXTURE_OWNER_INDETERMINATE` |
| `C1LifecycleOwner` | Owner of fixture lifecycle contract | Gate A fixture lifecycle only | Capability manifest | Future Build Brief assignment | Review/evidence record | `FIXTURE_OWNER_INDETERMINATE` |
| `C1ReconcilerOwner` | Owner of exact fixture reconciliation contract | Gate A fixture bucket only | Capability manifest | Future Build Brief assignment | Review/evidence record | `FIXTURE_OWNER_INDETERMINATE` |
| `C1ResolverOwner` | Owner of fixture read semantics | Gate A conformance; Gate C remains open | Capability manifest | Future Build Brief assignment | Review/evidence record | `FIXTURE_OWNER_INDETERMINATE` |
| `C2Owner` | Owner of separate package-completeness gate | C2 only; no Gate A action | C2 governance record | Homeowner assignment | C2 docs | `FIXTURE_OWNER_INDETERMINATE` |
| `CARRIED_OPEN` | Exact draft disposition that preserves an unresolved gate without granting authority | One named GOV/ART row; not closure or waiver | Governance reviewer from packet and debt registry | Homeowner may resolve only through a later reviewed transition | Packet, ratification chain, and debt registry | `FIXTURE_GOV_ART_INDETERMINATE` |
| `COMMITTED_REANCHOR` | Single provenance-preserving transition that starts an activation-eligible segment after committed inactive history | One packet/ledger history; never activation itself | Reviewer verifies predecessor blob/path/commit and later verifies re-anchor commit | Transition author only | New inactive ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `CleanupDebt` | Immutable delete-only obligation created after timeout/failure | One run/object/bucket; never success evidence | Watchdog and debt repository | Registered branch authorizer; lifecycle executes | Durable debt row/history | `FIXTURE_CLEANUP_DEBT_INDETERMINATE` |
| `CleanupDebtClosed` | Terminal debt disposition after exact object/residue deletion and whole-bucket zero-residue proof; bucket deletion follows separately | One debt; no run rehabilitation | Watchdog from terminal proof | `FixtureLifecycleIdentity` proposes; CAS guard applies | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `CleanupDebtClosedAtUtc` | Database UTC of debt-close CAS | One cleanup debt | Debt repository/database UTC | `FixtureLifecycleIdentity` records | Debt state/history | `FIXTURE_CLEANUP_DEBT_TIME_INDETERMINATE` |
| `CleanupDebtDueAtUtc` | Branch-selected absolute hold/quarantine cleanup deadline | One cleanup debt | Watchdog from persisted derivation | `RunWatchdog` monitors | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `CleanupDebtOpen` | Debt disposition created by timeout/failure transition | One debt | Watchdog from CAS evidence | `RunWatchdog` CAS only | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `CleanupDebtOverdue` | Failed debt disposition after its immutable due time; cleanup remains mandatory | One open cleanup debt | `RunWatchdog` from debt row/database UTC | `RunWatchdog` CAS only | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `CleanupDebtState` | Closed set `CleanupDebtOpen`, `CleanupDebtOverdue`, `CleanupDebtClosed` | One cleanup-debt aggregate | Debt repository/history | Watchdog and guarded lifecycle transitions only | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `ContainingRepositoryCommit` | Git commit verified externally to contain `RecordBlobSHA256` at `RecordPath` | One final active record/Build Brief binding | Builder from Git object database and ancestry | Build Brief records observation; record blob never self-binds | Build Brief/evidence record outside ratification blob | `FIXTURE_REPOSITORY_COMMIT_INDETERMINATE` |
| `DataGovernanceReviewer` | Reviewer of synthetic-vs-real governance boundary | This packet/review only | Review identity/record | Homeowner assignment | Review record | `FIXTURE_OWNER_INDETERMINATE` |
| `DeleteAcknowledged` | Deletion evidence from an authoritative exact-delete provider acknowledgement | One exact object/delete attempt | Lifecycle operation record and provider acknowledgement | `FixtureLifecycleIdentity` only | Deletion evidence/history | `FIXTURE_DELETION_EVIDENCE_INDETERMINATE` |
| `DeleteOnlyCleanupAuthorization` | Immutable authorization whose sole physical action is exact deletion | One run/object/debt/state revision | Lifecycle identity validates record | Branch `Authorizer` only | Authorization record | `FIXTURE_CLEANUP_AUTH_INDETERMINATE` |
| `DeletionEvidenceKind` | Closed deletion evidence set `DeleteAcknowledged` or `PositiveAbsenceConfirmed` | One exact object terminal cleanup proof | Actor-separated operation/observation evidence | Identity fixed by the selected kind | Deletion evidence/history | `FIXTURE_DELETION_EVIDENCE_INDETERMINATE` |
| `Disabled` | Exact required value of `ObjectLockPosture` | Whole one-run fixture bucket | Evidence identity from provider posture | Bucket creation configuration | Bucket posture record | `FIXTURE_OBJECT_LOCK_POSTURE_INDETERMINATE` |
| `DisabledNeverEnabled` | Exact required value of `VersioningPosture`, excluding suspended/history-bearing buckets | Whole one-run fixture bucket | Evidence identity from posture and full enumeration | Bucket creation configuration | Bucket posture record | `FIXTURE_VERSIONING_POSTURE_INDETERMINATE` |
| `EffectiveRunEndUtc` | `min(coalesce(EvidenceRunTerminalizedAtUtc,EvidenceRunDeadlineUtc),EvidenceRunDeadlineUtc)` | One evidence run; always defined | Watchdog/reviewer from persisted clocks | No actor may alter; run state logic uses | Persisted on terminalization and recomputable | `FIXTURE_EFFECTIVE_END_INDETERMINATE` |
| `EmptyVerified` | Bucket-state disposition after whole-bucket preflight proves no residue | One fresh fixture bucket | Evidence identity by whole-bucket enumeration | `FixtureBucketEvidenceIdentity` CAS | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `EvidenceRun` | One bounded Gate A evidence attempt | One run, one fresh bucket; no production/other run | Run repository and active record | Run-open owner under future Build Brief | Durable run row/history | `FIXTURE_RUN_INDETERMINATE` |
| `EvidenceRunDeadlineUtc` | Immutable started-at plus 30 minutes | One evidence run | Watchdog/reviewer from run row | Run-open transaction sets | Run row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `EvidenceRunId` | Fresh RFC-4122 v4 UUID generated at run open | One evidence run and derived bucket | Run repository | Run-open transaction sets | Run row and every child record | `FIXTURE_RUN_ID_INDETERMINATE` |
| `EvidenceRunStartedAtUtc` | Database UTC captured at run-open transaction | One evidence run | Run repository/database UTC | Run-open transaction sets | Run row | `FIXTURE_RUN_TIME_INDETERMINATE` |
| `EvidenceRunState` | Closed set `Open`, `ExecutionFinishedCleanupPending`, `GracefullyTerminalized`, `AbandonedCleanupPending`, `AbandonedTerminal` | One run | Run repository/history | Run-open/execution/graceful/watchdog CAS only | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `EvidenceRunTerminalizedAtUtc` | Nullable database UTC of graceful or abandoned terminal CAS | One run; null while cleanup pending | Run repository | Guarded terminal CAS sets | Run row/history | `FIXTURE_RUN_TIME_INDETERMINATE` |
| `ExecutingIdentity` | Identity permitted to perform branch physical action | One transition/object; no authorization power implied | Capability/transition record | Identity named in section 8 | Operation/audit evidence | `FIXTURE_EXECUTOR_INDETERMINATE` |
| `ExecutionFinishedCleanupPending` | Nonterminal run state after execution completes and before lifecycle closure | One evidence run | Run repository/history | Execution-complete CAS only | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `ExpectedFaultCapability` | Exact capability permitted to execute the deliberate fault operation | One expected-fault authorization | Capability manifest and authenticated operation audit | `FixtureEvidenceReviewer` records reviewed mapping | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `ExpectedFaultExecutingIdentity` | Exact authenticated identity permitted to execute the deliberate fault operation | One expected-fault authorization | Authenticated operation audit | `FixtureEvidenceReviewer` records reviewed mapping | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `ExpectedFaultOperation` | Exact deliberate operation that may produce the expected fault | One expected-fault authorization | Operation audit and closed test taxonomy | `FixtureEvidenceReviewer` records reviewed mapping | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `ExpectedFaultType` | Exact pre-authorized integrity-fault kind with one closed mapping to operation, capability, identity and predecessor state | One test/run/object; closed Build-Brief test taxonomy | Expected-fault and observed-fault evidence | `FixtureEvidenceReviewer` selects from reviewed taxonomy | Expected-fault record | `FIXTURE_EXPECTED_FAULT_TYPE_INDETERMINATE` |
| `ExpectedFixtureFault` | Immutable single-use authorization for one deliberate negative-test integrity fault | Exact test/run/object/type/operation/capability/identity/pre-state/pre-revision/packet before operation | Expected-fault repository and authenticated operation audit | `FixtureEvidenceReviewer` creates | Expected-fault record and consumption evidence | `FIXTURE_EXPECTED_FAULT_INDETERMINATE` |
| `ExpectedPreState` | Exact source state required before the deliberate fault operation | One expected-fault authorization | Source row/history before operation | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `ExpectedPreStateRevision` | Exact source revision required before the deliberate fault operation | One expected-fault authorization | Source row/history before operation | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `FaultOperationStartedAtUtc` | Database UTC at start of the exact deliberate fault operation | One test/run/object operation | Operation audit/database UTC | Harness records; cannot precede authorization | Operation audit record | `FIXTURE_EXPECTED_FAULT_TIME_INDETERMINATE` |
| `FixtureBucket` | Fresh bucket named exactly by pattern for one run | Whole bucket; never shared; no other bucket | Evidence identity/provider metadata | Create/delete per exact authorizations | Run/bucket record | `FIXTURE_BUCKET_INDETERMINATE` |
| `FixtureBucketEvidenceIdentity` | Identity scoped to create/enumerate/delete exact derived bucket | Whole one-run bucket; no other bucket/production/plaintext | Capability manifest and negative proofs | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `FixtureBucketState` | Closed set `EmptyVerified`, `ModelAZeroResidueProved`, `BucketDeleted` | One fixture bucket | Bucket repository/history | Evidence identity CAS with proof | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `FixtureEvidenceReviewer` | Authorizer of exact expected-fault and matched reviewer-cleanup records | One test/quarantine/run/object | Review identity and authorization | Homeowner/build assignment | Expected-fault, authorization, and review records | `FIXTURE_OWNER_INDETERMINATE` |
| `FixtureExpiresAtUtc` | Object-created-at plus 24 hours; maximum authorized fixture-retention deadline, not a physical deletion guarantee | One fixture object; does not override hold/quarantine or safe-delete guards | Watchdog from object row | Object-create transaction sets | Object/run record | `FIXTURE_DEADLINE_INDETERMINATE` |
| `FixtureExpiryBreached` | Irreversible failed evidence event recorded when residue remains at fixture expiry | One run/object expiry; never success | `RunWatchdog` from persisted expiry, residue and database UTC | `RunWatchdog` guarded CAS only | Run/debt history | `FIXTURE_EXPIRY_INDETERMINATE` |
| `FixtureHoldOwner` | Sole issuer of synthetic hold release/cleanup-release authorization | One synthetic hold; no real legal meaning | Capability/authorization record | Future Build Brief assignment | Identity and transition records | `FIXTURE_OWNER_INDETERMINATE` |
| `FixtureLifecycleIdentity` | Exact object/residue delete executor with no bucket-delete, read, list, or authorizer power | One authorized object/residue operation | Capability/operation record | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `FixtureObjectCreatedAtUtc` | Database UTC bound to exact fixture-object create record | One fixture object | Object repository/database UTC | Create transaction sets | Object record | `FIXTURE_OBJECT_TIME_INDETERMINATE` |
| `FixtureObjectIdentity` | Opaque exact object identity bound to run/bucket/key/revision | One object; no unrestricted locator | Object repository | Server derivation only | Object/transition records | `FIXTURE_OBJECT_ID_INDETERMINATE` |
| `FixtureQuarantineDispositionId` | Fresh UUID authorization for reviewer cleanup | One run/object/quarantine/revision | Lifecycle validates authorization record | `FixtureEvidenceReviewer` issues | Authorization record | `FIXTURE_QUARANTINE_AUTH_INDETERMINATE` |
| `FixtureRunPrefix` | Fixed key prefix `objects/` for organization | One fixture bucket; never limits enumeration | Evidence identity/provider metadata | Packet amendment only | Run/bucket record | `FIXTURE_PREFIX_INDETERMINATE` |
| `GET` | Current-key object read operation used only as one observation, never zero-residue proof | One exact fixture key; not versions/markers/multipart/bucket | Allowed exact reader from operation result | Identity allowed by capability graph | Redacted operation evidence | `FIXTURE_CURRENT_KEY_OBSERVATION_INDETERMINATE` |
| `GOV-001` | D9 governance traceability gate | C1 governance only | Governance reviewer | `Homeowner` ratification only | Packet/record chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `GracefullyTerminalized` | Successful terminal run disposition after execution, all lifecycle closure, zero residue, and bucket deletion | One run | Run repository plus lifecycle/bucket proofs | Run owner guarded CAS before deadline | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `HEAD` | Current-key metadata operation used only as one observation, never zero-residue proof | One exact fixture key; not versions/markers/multipart/bucket | Allowed exact reader from operation result | Identity allowed by capability graph | Redacted operation evidence | `FIXTURE_CURRENT_KEY_OBSERVATION_INDETERMINATE` |
| `HoldCleanupDebtDueAtUtc` | Minimum of hold deadline plus one hour and run deadline plus one hour | One timed-out hold debt | Watchdog from persisted clocks | `RunWatchdog` persists | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `HoldReleaseDeadlineUtc` | Minimum of applied-at plus 15 minutes and run deadline | One synthetic hold | Watchdog from hold/run rows | Hold-create transaction sets | Hold row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `Homeowner` | Authority allowed to ratify/revoke exact packet record | One governance transition | Signed/attributed instruction and record | Homeowner only | Ratification chain | `FIXTURE_OWNER_INDETERMINATE` |
| `IndependentReviewer` | Distinct reviewer of exact packet SHA | One review binding; not Homeowner/drafter | Review record | Assigned reviewer | Ratification transition record | `FIXTURE_REVIEWER_INDETERMINATE` |
| `IngressIdentity` | Identity allowed exact single-part provisional create/write | One exact provisional object; no read/list/committed access | Capability manifest/negative proof | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `LifecycleConfigurationPosture` | Required absence of every lifecycle rule | Whole fresh fixture bucket | `PostureProbeIdentity` from typed provider response | No actor may weaken; Build Brief configures | Bucket posture evidence | `FIXTURE_LIFECYCLE_POSTURE_INDETERMINATE` |
| `MaximumEvidenceRunDuration` | Constant 30 minutes | Every Gate A evidence run in v0.5 | Reviewer from packet/run derivation | Packet amendment only | Packet and run row derivation version | `FIXTURE_DURATION_INDETERMINATE` |
| `ModelAZeroResidue` | Predicate: no live/version/marker/multipart/part in whole fixture bucket | One whole fixture bucket; not prefix/other bucket | Evidence identity via whole-bucket enumeration | No actor changes predicate | Enumeration evidence | `FIXTURE_RESIDUE_INDETERMINATE` |
| `ModelAZeroResidueProved` | Bucket disposition after predicate is proven | One fixture bucket | Evidence identity and proof record | Evidence identity CAS | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `MultipartPosture` | Required value `Prohibited` with no uploads/parts | Whole fixture bucket | Evidence identity by posture/enumeration | Build Brief config; evidence identity observes | Bucket posture record | `FIXTURE_MULTIPART_POSTURE_INDETERMINATE` |
| `NOT_APPLICABLE` | Exact disposition excluding a gate because its owning slice is outside this packet | One named D9 row; never evidence that the gate passed | Governance reviewer from packet ownership map | Homeowner may change only through owning-slice review | Packet and owning-slice record | `FIXTURE_GOV_ART_INDETERMINATE` |
| `NoLifecycleRules` | Exact acceptable lifecycle posture: typed no-configuration or exact empty rule collection | Whole fresh fixture bucket | `PostureProbeIdentity` | No actor changes value | Bucket posture evidence | `FIXTURE_LIFECYCLE_POSTURE_INDETERMINATE` |
| `NoPublicAccess` | Exact acceptable public-access posture: no public bucket ACL and no public bucket policy | Whole fresh fixture bucket | `PostureProbeIdentity` | No actor changes value | Bucket posture evidence | `FIXTURE_PUBLIC_ACCESS_POSTURE_INDETERMINATE` |
| `None` | Exact required value of `ProviderRetentionPosture` at bucket and object levels | Whole fixture bucket and every fixture object | Evidence identity from provider posture | Bucket/object configuration | Posture/object evidence | `FIXTURE_RETENTION_POSTURE_INDETERMINATE` |
| `NormalCleanupDueAtUtc` | Effective terminal time plus one hour | One evidence run/bucket | Watchdog from persisted terminal clocks | Watchdog persists at terminalization | Run/bucket record | `FIXTURE_DEADLINE_INDETERMINATE` |
| `ObjectId` | Expected-fault field equal to exact `FixtureObjectIdentity` | One expected-fault record | Expected-fault repository | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `ObjectLockPosture` | Required value `Disabled` | Whole fixture bucket | Evidence identity from provider posture | Bucket creation configuration | Bucket posture record | `FIXTURE_OBJECT_LOCK_POSTURE_INDETERMINATE` |
| `Open` | Initial active run disposition | One evidence run | Run repository | Run-open transaction | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `PacketSHA256` | SHA-256 of exact packet bytes | One packet version/record/authorization | Reviewer/builder hashes committed file | No actor mutates; new version required | Ratification and evidence records | `FIXTURE_PACKET_HASH_INDETERMINATE` |
| `PlatformArchitecture` | Exact runnable CPU architecture | One evidence image execution | Future Build Brief/resolver record | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `PlatformOS` | Exact runnable operating system | One evidence image execution | Future Build Brief/resolver record | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `PositiveAbsenceConfirmed` | Deletion evidence from quiescence plus two bounded exact-key absence observations | One exact object after unknown delete/write outcome | `ReconciliationIdentity` observation evidence | `ReconciliationIdentity` only | Deletion evidence/history | `FIXTURE_DELETION_EVIDENCE_INDETERMINATE` |
| `PostureProbeIdentity` | Identity allowed bucket posture reads only | One fresh fixture bucket; no object data or mutation | Capability manifest and negative proofs | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `PreviousRatificationRecordRepositoryCommit` | Git commit that contains the immediate predecessor record; inactive draft predecessor may use the explicit uncommitted sentinel | One record transition | Transition author verifies predecessor Git object or records inactive sentinel | Transition author records; no self-reference | New ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `PreviousRatificationRecordSHA256` | SHA-256 of immediate predecessor record | One record transition; genesis may use `NONE` | Reviewer hashes predecessor | Transition author records | New ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `Prohibited` | Exact required value of `MultipartPosture`; no initiation, upload ID, part, or completion path | Whole fixture bucket/adapter operation set | Capability manifest plus whole-bucket enumeration | Build Brief configuration | Posture and negative-operation evidence | `FIXTURE_MULTIPART_POSTURE_INDETERMINATE` |
| `ProviderRetentionPosture` | Required bucket/object value `None` | Whole fixture bucket and every object | Evidence identity from provider posture | Bucket/object configuration | Posture/object evidence | `FIXTURE_RETENTION_POSTURE_INDETERMINATE` |
| `PublicAccessPosture` | Required proof that bucket ACL and policy grant no public access | Whole fresh fixture bucket | `PostureProbeIdentity` | No actor may weaken; Build Brief configures | Bucket posture evidence | `FIXTURE_PUBLIC_ACCESS_POSTURE_INDETERMINATE` |
| `QuarantineCleaned` | Successful terminal quarantine state after matched expected fault, authorized deletion, and zero residue; no debt exists | One expected-fault quarantine | Quarantine/object/residue evidence | `FixtureLifecycleIdentity` proposes guarded CAS | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `QuarantineCleanupDebtDueAtUtc` | Minimum of quarantine deadline plus one hour and run deadline plus one hour | One timed-out quarantine debt | Watchdog from persisted clocks | `RunWatchdog` persists | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `QuarantineDispositionDeadlineUtc` | Minimum of quarantine time plus 15 minutes and run deadline | One quarantine | Watchdog from quarantine/run rows | Quarantine-create transaction sets | Quarantine row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `QuarantineDispositionTimeout` | Quarantine state after watchdog deadline CAS | One quarantine; run remains failed | Watchdog from persisted deadline | `RunWatchdog` CAS | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `QuarantineId` | Fresh UUID for exact quarantine event | One run/object/integrity mismatch | Quarantine repository | Reconciler creates | Quarantine/authorization/debt records | `FIXTURE_QUARANTINE_ID_INDETERMINATE` |
| `QuarantineState` | Closed set `Quarantined`, `ReviewerCleanupAuthorized`, `QuarantineCleaned`, `UnexpectedFaultCleanupRequired`, `QuarantineDispositionTimeout` | One quarantine | Quarantine repository/history | Reconciler/reviewer/watchdog/lifecycle CAS | Quarantine state/history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `Quarantined` | Initial quarantine disposition after integrity mismatch | One quarantine | Reconciler from integrity evidence | Reconciler CAS | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `QuarantinedAtUtc` | Database UTC of quarantine CAS | One quarantine | Quarantine repository/database UTC | Reconciler records | Quarantine row | `FIXTURE_QUARANTINE_TIME_INDETERMINATE` |
| `RatificationRecordVersion` | Monotonic document version of one ledger entry | One record chain; never reused | Reviewer from committed file | Transition author creates new file/version | Ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `ReconciliationIdentity` | Identity allowed exact fenced provisional inspect/read/commit/abort | One provisional attempt; no bucket-wide/general access | Capability manifest/negative proof | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `RecordBlobSHA256` | SHA-256 of exact final active record bytes verified from Git | One future Build Brief binding | Builder hashes blob at bound commit/path | Build Brief records observation; record cannot self-write it | Build Brief/evidence record | `FIXTURE_RATIFICATION_RECORD_INDETERMINATE` |
| `RecordPath` | Exact repository path of final active record | One future Build Brief binding; no alias | Builder from bound Git tree | Build Brief records observation | Build Brief/evidence record | `FIXTURE_RATIFICATION_RECORD_INDETERMINATE` |
| `ReferenceEnvironmentOwner` | Owner of isolated fixture environment and timeout cleanup authorization | Reference environment only | Governance/capability record | Homeowner/build assignment | Review/authorization record | `FIXTURE_OWNER_INDETERMINATE` |
| `Residue` | Any live object, historical version, delete marker, multipart upload, or part | Whole one-run fixture bucket | Evidence identity via whole-bucket enumeration | Lifecycle deletes only with authorization | Enumeration/cleanup evidence | `FIXTURE_RESIDUE_INDETERMINATE` |
| `ResolvedChildManifestDigest` | Digest of platform-specific child manifest | One runnable image/platform | Future resolver evidence | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `ResolvedImageConfigDigestOrImageId` | Digest/ID of exact runnable image config | One runnable image/platform | Future resolver evidence | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `ReviewerCleanupAuthorized` | Quarantine state after exact pre-operation expected-fault match and valid reviewer delete-only authorization | One quarantine | Lifecycle validates expected-fault and authorization records | `FixtureEvidenceReviewer` CAS/authorization | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `RunId` | Expected-fault field equal to exact `EvidenceRunId` | One expected-fault record | Expected-fault repository | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `RunOpenIdentity` | Identity permitted to create one evidence run and its derived fresh bucket authorization request | One run; no lifecycle/review/ratification power | Capability manifest and run-open audit | Bootstrap under future Build Brief | Identity/ACL and run-open evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `RunWatchdog` | Independent reconciler using database UTC and persisted state | All open fixture runs in reference deployment; no harness dependency | Watchdog manifest/heartbeat/run rows | Dedicated watchdog identity | Watchdog transition/audit evidence | `FIXTURE_WATCHDOG_INDETERMINATE` |
| `SUPERSEDED_INACTIVE_HISTORY` | Historical record segment retained for provenance but forbidden as authority | v0.1–v0.3 inactive chain only | Git history and re-anchor record | No actor can activate history | Immutable repository history | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `SecurityReviewer` | Reviewer of fixture access/default-deny boundary | This packet/evidence review | Review identity/record | Homeowner assignment | Review record | `FIXTURE_OWNER_INDETERMINATE` |
| `StateSequence` | Strictly increasing, never-reused sequence on each ratification transition and state family where named | One chain or aggregate; no cross-aggregate comparison | Repository/history | Conditional transition only | Record/state history | `FIXTURE_STATE_SEQUENCE_INDETERMINATE` |
| `SyntheticHold` | Fixture-only deletion-blocking control with no legal meaning | One run/object | Hold repository | `FixtureHoldOwner` applies/releases | Hold state/history | `FIXTURE_HOLD_INDETERMINATE` |
| `SyntheticHoldActive` | Hold state that blocks delete | One hold | Hold repository | Hold-create CAS | Hold history | `FIXTURE_HOLD_STATE_INDETERMINATE` |
| `SyntheticHoldAppliedAtUtc` | Database UTC of hold-active CAS | One hold | Hold repository/database UTC | Hold-create transaction | Hold row | `FIXTURE_HOLD_TIME_INDETERMINATE` |
| `SyntheticHoldCleanupReleaseId` | Fresh UUID cleanup-only late release authorization | One timed-out hold/run/object/revision | Lifecycle validates authorization | `FixtureHoldOwner` issues | Authorization/hold/debt records | `FIXTURE_HOLD_AUTH_INDETERMINATE` |
| `SyntheticHoldConflict` | Evidence that delete was denied while hold active | One hold/object/run | Lifecycle deny result | Lifecycle records only | Hold/audit record | `FIXTURE_HOLD_CONFLICT_INDETERMINATE` |
| `SyntheticHoldId` | Fresh UUID identifying one synthetic hold | One run/object | Hold repository | Hold-create transaction | Hold/release/debt records | `FIXTURE_HOLD_ID_INDETERMINATE` |
| `SyntheticHoldReleaseId` | Fresh UUID on-time release authorization | One hold/run/object/revision | Lifecycle validates authorization | `FixtureHoldOwner` issues | Authorization/hold records | `FIXTURE_HOLD_AUTH_INDETERMINATE` |
| `SyntheticHoldReleaseTimeout` | Hold state after watchdog deadline CAS | One hold; run remains failed | Watchdog from deadline | `RunWatchdog` CAS | Hold/debt history | `FIXTURE_HOLD_STATE_INDETERMINATE` |
| `SyntheticHoldReleased` | Hold state after valid on-time release CAS | One hold | Hold repository/history | Lifecycle CAS with release authorization | Hold history | `FIXTURE_HOLD_STATE_INDETERMINATE` |
| `SyntheticHoldReleasedAtUtc` | Database UTC of released CAS | One hold | Hold repository/database UTC | Lifecycle records | Hold history | `FIXTURE_HOLD_TIME_INDETERMINATE` |
| `SyntheticHoldState` | Closed set `SyntheticHoldActive`, `SyntheticHoldReleased`, `SyntheticHoldReleaseTimeout` | One hold | Hold repository/history | Hold/lifecycle/watchdog conditional transitions | Hold state/history | `FIXTURE_HOLD_STATE_INDETERMINATE` |
| `TestCaseId` | Stable reviewed identifier of the deliberate negative-test case | One expected-fault record | Test catalog and expected-fault repository | `FixtureEvidenceReviewer` records | Expected-fault record | `FIXTURE_EXPECTED_FAULT_BINDING_INDETERMINATE` |
| `TopLevelDigest` | Pinned top-level image digest in section 5 | One reference image index | Future image resolver/evidence | Packet amendment only | Packet/build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `TransitionActor` | Exact authenticated actor creating a ratification ledger transition | One record version | Git/record/reviewer evidence | Homeowner or explicitly authorized drafter/reviewer per transition | Ratification record | `FIXTURE_TRANSITION_ACTOR_INDETERMINATE` |
| `TransitionAtUtc` | UTC timestamp recorded for ratification ledger transition | One record version | Record/commit evidence | Transition actor records | Ratification record | `FIXTURE_TRANSITION_TIME_INDETERMINATE` |
| `TransitionType` | Closed transition reason named by ratification record | One record version | Reviewer from record | Transition actor selects allowed value | Ratification record | `FIXTURE_TRANSITION_TYPE_INDETERMINATE` |
| `UNCOMMITTED_PRE_RATIFICATION` | Explicit sentinel for a superseded inactive predecessor that has no containing commit; never activation-eligible | One draft transition field | Reviewer verifies predecessor is absent from Git history | Transition author records only before any authority | Ratification record history | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `UnexpectedFaultCleanupRequired` | Failed quarantine state created when no exact pre-authorized expected-fault match exists | One quarantine/run/object | Watchdog from expected/observed fault evidence | `RunWatchdog` CAS; opens debt atomically | Quarantine and debt history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `VersioningPosture` | Required `DisabledNeverEnabled` with no historical versions/markers | Whole fresh fixture bucket | Evidence identity via posture/enumeration | Bucket creation configuration | Bucket posture record | `FIXTURE_VERSIONING_POSTURE_INDETERMINATE` |
| `WatchdogNextEvaluationAtUtc` | Earliest unresolved persisted absolute deadline | One run/watchdog schedule | Watchdog from rows | `RunWatchdog` derives/persists | Watchdog/run record | `FIXTURE_WATCHDOG_TIME_INDETERMINATE` |

## 13. Review and mechanical closure

The review bundle must publish:

- the marker population sorted ordinally;
- register first-column terms;
- population count and register count;
- `population - register`;
- `register - population`;
- blank-cell count; and
- a search proving no bare terminal-event deadline phrase remains;
- complete 14-row deadline-relation coverage with no unsatisfiable pair;
- complete 19-state reachability coverage with no orphan close; and
- a search proving no record requires its own containing-commit hash.

Both set differences and blank-cell count must be zero. Any new normative
obligation term requires a register row in the same packet version. Every
closed state and every required same-run deadline relation must appear in the
two satisfiability tables.

## 14. Recommended next action

Create ratification record v0.4 as an inactive `COMMITTED_REANCHOR` bound to
this packet SHA-256, the exact committed v0.3 record SHA-256/path, and commit
`880a2005ad08a850dade2fcf476b12f5e912c3cd`. Return the packet and record for
two clean independent reviews. Do not request Build Brief, implementation,
evidence execution, or activation.
