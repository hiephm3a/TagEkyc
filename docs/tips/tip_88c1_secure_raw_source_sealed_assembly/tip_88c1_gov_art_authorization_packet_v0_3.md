# TIP-88C1 GOV/ART Authorization Packet v0.3

**File:** `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_v0_3.md`

**Version:** 0.3

**Status:** DRAFT FOR INDEPENDENT REVIEW — FIXTURE EVIDENCE INACTIVE — IMPLEMENTATION BLOCKED

**Date:** 2026-07-30

**Baseline:** `d1f0aba06ca72bd16a4d9b3d381ad91db1ac2bb8`

**Packet id:** `TIP-88C1-GOV-ART-FIXTURE-EVIDENCE-v0.3`

**Purpose:** Define the narrow GOV/ART boundary that, after two clean
independent reviews and a committed append-only Homeowner ratification record,
may allow a later TIP-88C1 Build Brief to authorize generated, non-patient
reference-provider fixture evidence. This packet is neither a Build Brief nor
execution authority.

## Changelog

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
| Baseline | `d1f0aba06ca72bd16a4d9b3d381ad91db1ac2bb8` |
| v0.3 packet path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_v0_3.md` |
| Ratification-record path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_ratification_v0_2.md` |
| Superseded v0.2 | SHA-256 `10C703AC30B5B2E75CCF3B351252E59FEC023A1D3515DDB878ACBCAB4D9C71BB` |
| Superseded v0.1 | SHA-256 `2967117F082B0F19BF3F8E6B4B7D291F7BFD648C8964CA5502E5400BA52ACA60` |
| Locked Planning Brief | v0.17, SHA-256 `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC`, verify-only |
| HLD/LLD | Existing D9 synchronization remains sufficient; verify-only and byte-unchanged |

v0.3 supersedes v0.2 and v0.1 for future review or ratification. Superseded
files remain immutable history and grant no authority.

## 1. Authority and append-only ratification chain

This packet is governed by TIP-88C1 Planning Brief v0.17 D9, TIP-50 sections
6–9, and the TIP-51 bounded-packet precedent.

Authorization state is held only by versioned ratification records. Every
review binding, activation, invalidation, or revocation creates a new record
version with:

```text
RatificationRecordVersion
PreviousRatificationRecordSHA256
StateSequence
PacketSHA256
TransitionType
TransitionActor
TransitionAtUtc
```

Silent in-place mutation of an authoritative record is forbidden.

The git commit chain is normative for this ledger. A record is ineligible for
activation until committed. Each transition record must bind its repository
commit, and its commit ancestry must contain the committed predecessor record.
The hash-linked files are the authorization state; git ancestry is mandatory
provenance and cannot substitute for a missing record or hash.

A future Build Brief must bind:

```text
ActiveRatificationRecordPath
ActiveRatificationRecordSHA256
RepositoryCommit
```

It must verify the record is committed, active, bound to this exact packet
SHA-256, and descended from the prior record chain. Packet/record drafting,
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
| Preserve Gate A/B/C and ART-003 separation | Persistence, readability, and package completeness remain different gates. | Fixture dispositions cannot close later gates. | No real-artifact or package claim. |

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
| Mutable ratification | New version per transition and commit binding. | Append-only authorization history. | Record remains inactive. |
| Real persistence/readability/package | Preserve open. | No real capability. | Gate B, Gate C, C2. |

### Dispatch Readiness

- Build Brief: **not authorized**.
- Implementation/evidence execution: **not authorized**.
- Remaining gates: two clean reviews, committed active ratification-chain
  record, platform pins, retry-accounting gate, pending D1/D3-D9, Gate B, Gate
  C, C2 `ART-003`, and production/legal/operational gates.

## 3. TIP-50 master packet fields

| Required field | C1 v0.3 value |
| --- | --- |
| Packet id/name | `TIP-88C1-GOV-ART-FIXTURE-EVIDENCE-v0.3` |
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
| Retention/expiry | Hard 24-hour maximum, one-hour normal cleanup target, shorter absolute hold/quarantine deadlines, no extension. | Real policy and Gate B `ART-004`. |
| Purge/disposal and cleanup | Exact delete plus live/version/delete-marker/multipart enumeration; NotFound alone insufficient. | Real authority and Gate B `ART-005`. |
| Legal-hold conflict disposition | Exact synthetic hold/release lifecycle in section 8; no legal meaning. | Real hold authority/sync and Gate B `ART-006`. |
| Least-privilege access | Ingress exact create/write; reconciler exact fenced provisional inspect/read/commit/abort; assembly exact committed read; lifecycle exact opaque delete/hold; fixture evidence identity whole-bucket/run enumeration only; no other listing. | Production IAM/key authority and Gate B `ART-007`. |
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

`FixtureBucketEvidenceIdentity` may:

- create exactly the derived bucket when holding `BucketCreateAuthorization`;
- enumerate the whole derived bucket, including live objects, versions, delete
  markers, multipart uploads, and parts;
- inspect bucket versioning/Object-Lock/retention posture; and
- delete exactly the derived bucket only with `BucketDeleteAuthorization`
  after `ModelAZeroResidueProved`.

It cannot list/access/create/delete another bucket, derive another name, access
production, read plaintext, or become a runtime application identity.
`FixtureRunPrefix` is fixed to `objects/` only for key organization; it never
limits posture or residue enumeration.

### 6.2 Model A preflight

Before object write, whole-bucket evidence must prove:

```text
VersioningPosture = DisabledNeverEnabled
ObjectLockPosture = Disabled
ProviderRetentionPosture = None
MultipartPosture = Prohibited
FixtureBucketState = EmptyVerified
```

The bucket must contain no live object, historical version, delete marker,
multipart upload, or part. Unknown/inaccessible/suspended-with-history posture
fails closed.

### 6.3 Terminal proof and bucket deletion

After every branch, whole-bucket enumeration proves:

```text
no live object
AND no historical object version
AND no delete marker
AND no active multipart upload
AND no uploaded part
```

Only then may CAS publish `ModelAZeroResidueProved`, issue
`BucketDeleteAuthorization`, delete the exact bucket, and publish
`BucketDeleted`. Current-key `GET`/`HEAD` NotFound is never sufficient alone.
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

`EvidenceRunTerminalizedAtUtc` is the persisted graceful terminal timestamp or
null. The total function is:

```text
EffectiveRunEndUtc =
  min(
    coalesce(EvidenceRunTerminalizedAtUtc, EvidenceRunDeadlineUtc),
    EvidenceRunDeadlineUtc
  )
```

It is therefore defined from persisted values even when the harness crashes.
A graceful CAS changes `Open` to `GracefullyTerminalized`. At
`EvidenceRunDeadlineUtc`, `RunWatchdog` CASes a remaining `Open` run to
`AbandonedTerminal` without requiring a harness terminal record.

### 7.3 Absolute deadline derivations

| Deadline | Persisted derivation |
| --- | --- |
| `EvidenceRunDeadlineUtc` | `EvidenceRunStartedAtUtc + 30 minutes` at run open |
| `HoldReleaseDeadlineUtc` | `min(SyntheticHoldAppliedAtUtc + 15 minutes, EvidenceRunDeadlineUtc)` |
| `QuarantineDispositionDeadlineUtc` | `min(QuarantinedAtUtc + 15 minutes, EvidenceRunDeadlineUtc)` |
| `NormalCleanupDueAtUtc` | `EffectiveRunEndUtc + 1 hour` |
| `HoldCleanupDebtDueAtUtc` | `min(HoldReleaseDeadlineUtc + 1 hour, EvidenceRunDeadlineUtc + 1 hour)` |
| `QuarantineCleanupDebtDueAtUtc` | `min(QuarantineDispositionDeadlineUtc + 1 hour, EvidenceRunDeadlineUtc + 1 hour)` |
| `FixtureExpiresAtUtc` | `FixtureObjectCreatedAtUtc + 24 hours`; it does not override hold/quarantine CAS |
| `BucketDeleteDeadlineUtc` | `NormalCleanupDueAtUtc` |
| `WatchdogNextEvaluationAtUtc` | earliest unresolved persisted deadline among run, hold, quarantine, cleanup debt, fixture expiry, and bucket deletion |

Each derived timestamp is persisted when its source state is created or
terminalized. Recalculation may verify equality but cannot extend it.

### 7.4 Independent watchdog/reconciler

`RunWatchdog` uses database UTC and persisted state only. It:

1. wakes no later than `WatchdogNextEvaluationAtUtc`;
2. terminalizes abandoned `Open` runs at `EvidenceRunDeadlineUtc`;
3. CASes expired active holds to `SyntheticHoldReleaseTimeout`;
4. CASes expired quarantines to `QuarantineDispositionTimeout`;
5. creates the exact delete-only `CleanupDebt` with its absolute due time;
6. invokes only the registered authorizer/executing-identity workflow; and
7. records missed cleanup deadlines as fail-closed debt, never success.

Restart re-derives the next evaluation from persisted timestamps. Harness
silence cannot postpone or erase an obligation.

## 8. Four symmetric hold/quarantine branches

Every branch has an `Authorizer`, `ExecutingIdentity`, conditional state
transition, and terminal proof.

| Branch | Authorizer | Executing identity | CAS/state transition | Terminal proof |
| --- | --- | --- | --- | --- |
| Hold released on time | `FixtureHoldOwner` issues run/object/hold/packet-bound `SyntheticHoldReleaseId` | `FixtureLifecycleIdentity` | `SyntheticHoldActive → SyntheticHoldReleased` before `HoldReleaseDeadlineUtc` | Delete exact object/residue → `ModelAZeroResidueProved`; deliberate hold cell may PASS |
| Hold release timeout | `FixtureHoldOwner` issues cleanup-only `SyntheticHoldCleanupReleaseId` and `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `SyntheticHoldActive → SyntheticHoldReleaseTimeout → CleanupDebtOpen → CleanupDebtClosed` | Delete by `HoldCleanupDebtDueAtUtc`, whole-bucket zero residue, debt closed; run remains failed |
| Quarantine reviewer cleanup | `FixtureEvidenceReviewer` issues `FixtureQuarantineDispositionId` and `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `Quarantined → ReviewerCleanupAuthorized → CleanupDebtClosed` before `QuarantineDispositionDeadlineUtc` | Exact delete + whole-bucket zero residue; branch may PASS |
| Quarantine timeout cleanup | `ReferenceEnvironmentOwner` issues run/object/quarantine/packet-bound `DeleteOnlyCleanupAuthorization` | `FixtureLifecycleIdentity` | `Quarantined → QuarantineDispositionTimeout → CleanupDebtOpen → CleanupDebtClosed` | Delete by `QuarantineCleanupDebtDueAtUtc`, whole-bucket zero residue, debt closed; run remains failed |

All authorizations are immutable, single-run, single-object, single-state-
revision, packet-SHA-bound records. Cross-run/replay/stale revision fails.
Timeout cleanup never converts a failed run into PASS. No actor may choose
retain, export, restore, retry, reuse, re-encrypt, or another physical action.

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
AbandonedTerminal
ActiveRatificationRecordPath
ActiveRatificationRecordSHA256
AssemblyIdentity
Authorizer
Available
BucketCreateAuthorization
BucketCreatedAtUtc
BucketDeleteAuthorization
BucketDeleteDeadlineUtc
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
CleanupDebt
CleanupDebtClosed
CleanupDebtClosedAtUtc
CleanupDebtDueAtUtc
CleanupDebtOpen
DataGovernanceReviewer
DeleteOnlyCleanupAuthorization
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
FixtureBucket
FixtureBucketEvidenceIdentity
FixtureBucketState
FixtureEvidenceReviewer
FixtureExpiresAtUtc
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
MaximumEvidenceRunDuration
ModelAZeroResidue
ModelAZeroResidueProved
MultipartPosture
NOT_APPLICABLE
None
NormalCleanupDueAtUtc
ObjectLockPosture
Open
PacketSHA256
PlatformArchitecture
PlatformOS
PreviousRatificationRecordSHA256
Prohibited
ProviderRetentionPosture
QuarantineCleanupDebtDueAtUtc
QuarantineDispositionDeadlineUtc
QuarantineDispositionTimeout
QuarantineId
QuarantineState
Quarantined
QuarantinedAtUtc
RatificationRecordVersion
ReconciliationIdentity
ReferenceEnvironmentOwner
RepositoryCommit
Residue
ResolvedChildManifestDigest
ResolvedImageConfigDigestOrImageId
ReviewerCleanupAuthorized
RunOpenIdentity
RunWatchdog
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
TopLevelDigest
TransitionActor
TransitionAtUtc
TransitionType
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
| `AbandonedTerminal` | Terminal run disposition set by watchdog after the absolute deadline | One evidence run; not graceful success | `RunWatchdog` from run row and database UTC | `RunWatchdog` CAS only | Run state/history, append-only transition | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `ActiveRatificationRecordPath` | Exact repository path of the committed active record | One packet authorization chain; no filesystem alias | Builder from committed tree | Homeowner-authorized record transition | Future Build Brief and evidence record | `FIXTURE_RATIFICATION_RECORD_INDETERMINATE` |
| `ActiveRatificationRecordSHA256` | SHA-256 of the exact active record bytes | One record version | Builder hashes committed path | Homeowner-authorized record transition | Future Build Brief and evidence record | `FIXTURE_RATIFICATION_RECORD_INDETERMINATE` |
| `AssemblyIdentity` | Identity allowed exact committed fixture read only | One committed object in one run; no list/write/delete | Capability manifest and deny evidence | Deployment bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `Authorizer` | Role allowed to issue the branch-specific immutable authorization | One transition row; no execution privilege implied | Transition record and capability manifest | Role named by section 8 | Authorization record | `FIXTURE_AUTHORIZER_INDETERMINATE` |
| `Available` | External C1 descriptor state whose entry requires Gate C closure | Runtime descriptor only; never produced or relied upon by Gate A | C1 descriptor repository under a later implementation | No Gate A actor | Not persisted by this packet/evidence run | `FIXTURE_GATE_C_NOT_CLOSED` |
| `BucketCreateAuthorization` | Immutable permit to create exact derived bucket name | One run/one bucket; no other name | Evidence identity from active record/run | `ReferenceEnvironmentOwner` | Authorization record before create | `FIXTURE_BUCKET_AUTH_INDETERMINATE` |
| `BucketCreatedAtUtc` | Database UTC observed after exact bucket creation | One fixture bucket | Evidence harness plus provider response/database UTC | `FixtureBucketEvidenceIdentity` records | Run/bucket record | `FIXTURE_BUCKET_TIME_INDETERMINATE` |
| `BucketDeleteAuthorization` | Immutable permit issued only after zero-residue CAS | One run/one bucket; delete only | Evidence identity from zero-residue transition | `ReferenceEnvironmentOwner` | Authorization record | `FIXTURE_BUCKET_AUTH_INDETERMINATE` |
| `BucketDeleteDeadlineUtc` | Absolute deadline equal to `NormalCleanupDueAtUtc` | One fixture bucket | Watchdog from persisted run row | `RunWatchdog` monitors; lifecycle executes | Bucket/run record | `FIXTURE_DEADLINE_INDETERMINATE` |
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
| `CleanupDebt` | Immutable delete-only obligation created after timeout/failure | One run/object/bucket; never success evidence | Watchdog and debt repository | Registered branch authorizer; lifecycle executes | Durable debt row/history | `FIXTURE_CLEANUP_DEBT_INDETERMINATE` |
| `CleanupDebtClosed` | Terminal debt disposition after zero residue and bucket deletion as applicable | One debt; no run rehabilitation | Watchdog from terminal proof | `FixtureLifecycleIdentity` proposes; CAS guard applies | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `CleanupDebtClosedAtUtc` | Database UTC of debt-close CAS | One cleanup debt | Debt repository/database UTC | `FixtureLifecycleIdentity` records | Debt state/history | `FIXTURE_CLEANUP_DEBT_TIME_INDETERMINATE` |
| `CleanupDebtDueAtUtc` | Branch-selected absolute hold/quarantine cleanup deadline | One cleanup debt | Watchdog from persisted derivation | `RunWatchdog` monitors | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `CleanupDebtOpen` | Debt disposition created by timeout/failure transition | One debt | Watchdog from CAS evidence | `RunWatchdog` CAS only | Debt state/history | `FIXTURE_CLEANUP_DEBT_STATE_INDETERMINATE` |
| `DataGovernanceReviewer` | Reviewer of synthetic-vs-real governance boundary | This packet/review only | Review identity/record | Homeowner assignment | Review record | `FIXTURE_OWNER_INDETERMINATE` |
| `DeleteOnlyCleanupAuthorization` | Immutable authorization whose sole physical action is exact deletion | One run/object/debt/state revision | Lifecycle identity validates record | Branch `Authorizer` only | Authorization record | `FIXTURE_CLEANUP_AUTH_INDETERMINATE` |
| `Disabled` | Exact required value of `ObjectLockPosture` | Whole one-run fixture bucket | Evidence identity from provider posture | Bucket creation configuration | Bucket posture record | `FIXTURE_OBJECT_LOCK_POSTURE_INDETERMINATE` |
| `DisabledNeverEnabled` | Exact required value of `VersioningPosture`, excluding suspended/history-bearing buckets | Whole one-run fixture bucket | Evidence identity from posture and full enumeration | Bucket creation configuration | Bucket posture record | `FIXTURE_VERSIONING_POSTURE_INDETERMINATE` |
| `EffectiveRunEndUtc` | `min(coalesce(EvidenceRunTerminalizedAtUtc,EvidenceRunDeadlineUtc),EvidenceRunDeadlineUtc)` | One evidence run; always defined | Watchdog/reviewer from persisted clocks | No actor may alter; run state logic uses | Persisted on terminalization and recomputable | `FIXTURE_EFFECTIVE_END_INDETERMINATE` |
| `EmptyVerified` | Bucket-state disposition after whole-bucket preflight proves no residue | One fresh fixture bucket | Evidence identity by whole-bucket enumeration | `FixtureBucketEvidenceIdentity` CAS | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `EvidenceRun` | One bounded Gate A evidence attempt | One run, one fresh bucket; no production/other run | Run repository and active record | Run-open owner under future Build Brief | Durable run row/history | `FIXTURE_RUN_INDETERMINATE` |
| `EvidenceRunDeadlineUtc` | Immutable started-at plus 30 minutes | One evidence run | Watchdog/reviewer from run row | Run-open transaction sets | Run row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `EvidenceRunId` | Fresh RFC-4122 v4 UUID generated at run open | One evidence run and derived bucket | Run repository | Run-open transaction sets | Run row and every child record | `FIXTURE_RUN_ID_INDETERMINATE` |
| `EvidenceRunStartedAtUtc` | Database UTC captured at run-open transaction | One evidence run | Run repository/database UTC | Run-open transaction sets | Run row | `FIXTURE_RUN_TIME_INDETERMINATE` |
| `EvidenceRunState` | Closed set `Open`, `GracefullyTerminalized`, `AbandonedTerminal` | One run | Run repository/history | Run-open/graceful/watchdog CAS only | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `EvidenceRunTerminalizedAtUtc` | Nullable database UTC of graceful terminal CAS | One run; null on crash/abandon | Run repository | Graceful terminal CAS sets | Run row/history | `FIXTURE_RUN_TIME_INDETERMINATE` |
| `ExecutingIdentity` | Identity permitted to perform branch physical action | One transition/object; no authorization power implied | Capability/transition record | Identity named in section 8 | Operation/audit evidence | `FIXTURE_EXECUTOR_INDETERMINATE` |
| `FixtureBucket` | Fresh bucket named exactly by pattern for one run | Whole bucket; never shared; no other bucket | Evidence identity/provider metadata | Create/delete per exact authorizations | Run/bucket record | `FIXTURE_BUCKET_INDETERMINATE` |
| `FixtureBucketEvidenceIdentity` | Identity scoped to create/enumerate/delete exact derived bucket | Whole one-run bucket; no other bucket/production/plaintext | Capability manifest and negative proofs | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `FixtureBucketState` | Closed set `EmptyVerified`, `ModelAZeroResidueProved`, `BucketDeleted` | One fixture bucket | Bucket repository/history | Evidence identity CAS with proof | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `FixtureEvidenceReviewer` | Authorizer of reviewer cleanup branch | One quarantine/run/object | Review identity and authorization | Homeowner/build assignment | Authorization/review record | `FIXTURE_OWNER_INDETERMINATE` |
| `FixtureExpiresAtUtc` | Object-created-at plus 24 hours | One fixture object; does not override hold/quarantine | Watchdog from object row | Object-create transaction sets | Object/run record | `FIXTURE_DEADLINE_INDETERMINATE` |
| `FixtureHoldOwner` | Sole issuer of synthetic hold release/cleanup-release authorization | One synthetic hold; no real legal meaning | Capability/authorization record | Future Build Brief assignment | Identity and transition records | `FIXTURE_OWNER_INDETERMINATE` |
| `FixtureLifecycleIdentity` | Exact delete executor with no read/list/authorizer power | One authorized object/bucket operation | Capability/operation record | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `FixtureObjectCreatedAtUtc` | Database UTC bound to exact fixture-object create record | One fixture object | Object repository/database UTC | Create transaction sets | Object record | `FIXTURE_OBJECT_TIME_INDETERMINATE` |
| `FixtureObjectIdentity` | Opaque exact object identity bound to run/bucket/key/revision | One object; no unrestricted locator | Object repository | Server derivation only | Object/transition records | `FIXTURE_OBJECT_ID_INDETERMINATE` |
| `FixtureQuarantineDispositionId` | Fresh UUID authorization for reviewer cleanup | One run/object/quarantine/revision | Lifecycle validates authorization record | `FixtureEvidenceReviewer` issues | Authorization record | `FIXTURE_QUARANTINE_AUTH_INDETERMINATE` |
| `FixtureRunPrefix` | Fixed key prefix `objects/` for organization | One fixture bucket; never limits enumeration | Evidence identity/provider metadata | Packet amendment only | Run/bucket record | `FIXTURE_PREFIX_INDETERMINATE` |
| `GET` | Current-key object read operation used only as one observation, never zero-residue proof | One exact fixture key; not versions/markers/multipart/bucket | Allowed exact reader from operation result | Identity allowed by capability graph | Redacted operation evidence | `FIXTURE_CURRENT_KEY_OBSERVATION_INDETERMINATE` |
| `GOV-001` | D9 governance traceability gate | C1 governance only | Governance reviewer | `Homeowner` ratification only | Packet/record chain | `FIXTURE_GOV_ART_INDETERMINATE` |
| `GracefullyTerminalized` | Run disposition set by successful graceful CAS | One run | Run repository/history | Run owner CAS before deadline | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `HEAD` | Current-key metadata operation used only as one observation, never zero-residue proof | One exact fixture key; not versions/markers/multipart/bucket | Allowed exact reader from operation result | Identity allowed by capability graph | Redacted operation evidence | `FIXTURE_CURRENT_KEY_OBSERVATION_INDETERMINATE` |
| `HoldCleanupDebtDueAtUtc` | Minimum of hold deadline plus one hour and run deadline plus one hour | One timed-out hold debt | Watchdog from persisted clocks | `RunWatchdog` persists | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `HoldReleaseDeadlineUtc` | Minimum of applied-at plus 15 minutes and run deadline | One synthetic hold | Watchdog from hold/run rows | Hold-create transaction sets | Hold row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `Homeowner` | Authority allowed to ratify/revoke exact packet record | One governance transition | Signed/attributed instruction and record | Homeowner only | Ratification chain | `FIXTURE_OWNER_INDETERMINATE` |
| `IndependentReviewer` | Distinct reviewer of exact packet SHA | One review binding; not Homeowner/drafter | Review record | Assigned reviewer | Ratification transition record | `FIXTURE_REVIEWER_INDETERMINATE` |
| `IngressIdentity` | Identity allowed exact single-part provisional create/write | One exact provisional object; no read/list/committed access | Capability manifest/negative proof | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `MaximumEvidenceRunDuration` | Constant 30 minutes | Every Gate A evidence run in v0.3 | Reviewer from packet/run derivation | Packet amendment only | Packet and run row derivation version | `FIXTURE_DURATION_INDETERMINATE` |
| `ModelAZeroResidue` | Predicate: no live/version/marker/multipart/part in whole fixture bucket | One whole fixture bucket; not prefix/other bucket | Evidence identity via whole-bucket enumeration | No actor changes predicate | Enumeration evidence | `FIXTURE_RESIDUE_INDETERMINATE` |
| `ModelAZeroResidueProved` | Bucket disposition after predicate is proven | One fixture bucket | Evidence identity and proof record | Evidence identity CAS | Bucket state/history | `FIXTURE_BUCKET_STATE_INDETERMINATE` |
| `MultipartPosture` | Required value `Prohibited` with no uploads/parts | Whole fixture bucket | Evidence identity by posture/enumeration | Build Brief config; evidence identity observes | Bucket posture record | `FIXTURE_MULTIPART_POSTURE_INDETERMINATE` |
| `NOT_APPLICABLE` | Exact disposition excluding a gate because its owning slice is outside this packet | One named D9 row; never evidence that the gate passed | Governance reviewer from packet ownership map | Homeowner may change only through owning-slice review | Packet and owning-slice record | `FIXTURE_GOV_ART_INDETERMINATE` |
| `None` | Exact required value of `ProviderRetentionPosture` at bucket and object levels | Whole fixture bucket and every fixture object | Evidence identity from provider posture | Bucket/object configuration | Posture/object evidence | `FIXTURE_RETENTION_POSTURE_INDETERMINATE` |
| `NormalCleanupDueAtUtc` | Effective terminal time plus one hour | One evidence run/bucket | Watchdog from persisted terminal clocks | Watchdog persists at terminalization | Run/bucket record | `FIXTURE_DEADLINE_INDETERMINATE` |
| `ObjectLockPosture` | Required value `Disabled` | Whole fixture bucket | Evidence identity from provider posture | Bucket creation configuration | Bucket posture record | `FIXTURE_OBJECT_LOCK_POSTURE_INDETERMINATE` |
| `Open` | Initial active run disposition | One evidence run | Run repository | Run-open transaction | Run state/history | `FIXTURE_RUN_STATE_INDETERMINATE` |
| `PacketSHA256` | SHA-256 of exact packet bytes | One packet version/record/authorization | Reviewer/builder hashes committed file | No actor mutates; new version required | Ratification and evidence records | `FIXTURE_PACKET_HASH_INDETERMINATE` |
| `PlatformArchitecture` | Exact runnable CPU architecture | One evidence image execution | Future Build Brief/resolver record | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `PlatformOS` | Exact runnable operating system | One evidence image execution | Future Build Brief/resolver record | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `PreviousRatificationRecordSHA256` | SHA-256 of immediate predecessor record | One record transition; genesis may use `NONE` | Reviewer hashes predecessor | Transition author records | New ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `Prohibited` | Exact required value of `MultipartPosture`; no initiation, upload ID, part, or completion path | Whole fixture bucket/adapter operation set | Capability manifest plus whole-bucket enumeration | Build Brief configuration | Posture and negative-operation evidence | `FIXTURE_MULTIPART_POSTURE_INDETERMINATE` |
| `ProviderRetentionPosture` | Required bucket/object value `None` | Whole fixture bucket and every object | Evidence identity from provider posture | Bucket/object configuration | Posture/object evidence | `FIXTURE_RETENTION_POSTURE_INDETERMINATE` |
| `QuarantineCleanupDebtDueAtUtc` | Minimum of quarantine deadline plus one hour and run deadline plus one hour | One timed-out quarantine debt | Watchdog from persisted clocks | `RunWatchdog` persists | Debt row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `QuarantineDispositionDeadlineUtc` | Minimum of quarantine time plus 15 minutes and run deadline | One quarantine | Watchdog from quarantine/run rows | Quarantine-create transaction sets | Quarantine row | `FIXTURE_DEADLINE_INDETERMINATE` |
| `QuarantineDispositionTimeout` | Quarantine state after watchdog deadline CAS | One quarantine; run remains failed | Watchdog from persisted deadline | `RunWatchdog` CAS | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `QuarantineId` | Fresh UUID for exact quarantine event | One run/object/integrity mismatch | Quarantine repository | Reconciler creates | Quarantine/authorization/debt records | `FIXTURE_QUARANTINE_ID_INDETERMINATE` |
| `QuarantineState` | Closed set `Quarantined`, `ReviewerCleanupAuthorized`, `QuarantineDispositionTimeout` | One quarantine | Quarantine repository/history | Reconciler/reviewer/watchdog CAS | Quarantine state/history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `Quarantined` | Initial quarantine disposition after integrity mismatch | One quarantine | Reconciler from integrity evidence | Reconciler CAS | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `QuarantinedAtUtc` | Database UTC of quarantine CAS | One quarantine | Quarantine repository/database UTC | Reconciler records | Quarantine row | `FIXTURE_QUARANTINE_TIME_INDETERMINATE` |
| `RatificationRecordVersion` | Monotonic document version of one ledger entry | One record chain; never reused | Reviewer from committed file | Transition author creates new file/version | Ratification record | `FIXTURE_RATIFICATION_CHAIN_INDETERMINATE` |
| `ReconciliationIdentity` | Identity allowed exact fenced provisional inspect/read/commit/abort | One provisional attempt; no bucket-wide/general access | Capability manifest/negative proof | Bootstrap under future Build Brief | Identity/ACL evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `ReferenceEnvironmentOwner` | Owner of isolated fixture environment and timeout cleanup authorization | Reference environment only | Governance/capability record | Homeowner/build assignment | Review/authorization record | `FIXTURE_OWNER_INDETERMINATE` |
| `RepositoryCommit` | Exact git commit containing packet and active record chain | One Build Brief/evidence run | Builder verifies git object/ancestry | Commit authority outside this packet | Build Brief/evidence record | `FIXTURE_REPOSITORY_COMMIT_INDETERMINATE` |
| `Residue` | Any live object, historical version, delete marker, multipart upload, or part | Whole one-run fixture bucket | Evidence identity via whole-bucket enumeration | Lifecycle deletes only with authorization | Enumeration/cleanup evidence | `FIXTURE_RESIDUE_INDETERMINATE` |
| `ResolvedChildManifestDigest` | Digest of platform-specific child manifest | One runnable image/platform | Future resolver evidence | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `ResolvedImageConfigDigestOrImageId` | Digest/ID of exact runnable image config | One runnable image/platform | Future resolver evidence | Build Brief ratification | Build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `ReviewerCleanupAuthorized` | Quarantine state after valid reviewer delete-only authorization | One quarantine | Lifecycle validates record | `FixtureEvidenceReviewer` CAS/authorization | Quarantine history | `FIXTURE_QUARANTINE_STATE_INDETERMINATE` |
| `RunOpenIdentity` | Identity permitted to create one evidence run and its derived fresh bucket authorization request | One run; no lifecycle/review/ratification power | Capability manifest and run-open audit | Bootstrap under future Build Brief | Identity/ACL and run-open evidence | `FIXTURE_IDENTITY_INDETERMINATE` |
| `RunWatchdog` | Independent reconciler using database UTC and persisted state | All open fixture runs in reference deployment; no harness dependency | Watchdog manifest/heartbeat/run rows | Dedicated watchdog identity | Watchdog transition/audit evidence | `FIXTURE_WATCHDOG_INDETERMINATE` |
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
| `TopLevelDigest` | Pinned top-level image digest in section 5 | One reference image index | Future image resolver/evidence | Packet amendment only | Packet/build/evidence record | `FIXTURE_PLATFORM_INDETERMINATE` |
| `TransitionActor` | Exact authenticated actor creating a ratification ledger transition | One record version | Git/record/reviewer evidence | Homeowner or explicitly authorized drafter/reviewer per transition | Ratification record | `FIXTURE_TRANSITION_ACTOR_INDETERMINATE` |
| `TransitionAtUtc` | UTC timestamp recorded for ratification ledger transition | One record version | Record/commit evidence | Transition actor records | Ratification record | `FIXTURE_TRANSITION_TIME_INDETERMINATE` |
| `TransitionType` | Closed transition reason named by ratification record | One record version | Reviewer from record | Transition actor selects allowed value | Ratification record | `FIXTURE_TRANSITION_TYPE_INDETERMINATE` |
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
- a search proving no bare terminal-event deadline phrase remains.

Both set differences and blank-cell count must be zero. Any new normative
obligation term requires a register row in the same packet version.

## 14. Recommended next action

Create ratification record v0.2 bound to this packet SHA-256 and v0.1 record
SHA-256, then return both for independent review. Do not request Build Brief,
implementation, evidence execution, or ratification.
