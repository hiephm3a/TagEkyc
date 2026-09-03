# TIP-88 Raw Export Program Spine — Governance Index

**Status:** AUTHORITATIVE SEQUENCE; DATA-PLANE DECOMPOSITION RATIFIED

**Ratified:** 2026-07-25

**Purpose:** Canonical scope, dependency, and naming spine for the Raw BIO export
program. This is not an implementation dispatch.

The original combined TIP-88 policy-gate brief was split for
adversarial-verifiability. The policy, control-plane, consent, authorization, and
runtime-read-boundary foundations are now implemented on the program branch.
Actual Raw BIO resolution, package construction, encryption, and delivery remain
future slices below.

## 1. Landed foundation

- **TIP-88A — Raw Export Policy Catalog Foundation**: inert six-table,
  append-only policy catalog. Landed at `e63cdf9`; closeout `62fed65`.
- **TIP-88B1 — Raw Export Control Plane**: identity surface, exact-version
  grant/revoke, scoped fulfillment authority, revocable lifecycle, activation
  gate, and lock-capable effective-state resolver. Landed at `2c684cf`; closeout
  `075bdcd`.
- **TIP-88B2 — Subject Export Consent**: append-only subject consent and
  consent-authority aggregates, exact session/subject/policy/purpose/recipient
  scope, raw-class consent set, fail-closed resolver, and concurrency contract.
  Landed at `8cd52a3`.
- **TIP-88B1-E1 — Fulfillment Expiry Projection**: exposes the authoritative
  fulfillment `ValidUntilUtc` consumed by authorization. Landed at `ccc2e37`;
  closeout `4b25e2f`.
- **TIP-88A-E2 — Policy Permit TTL Field**: immutable policy-version permit TTL
  and operational bounds. Landed at `09c9359`.
- **TIP-88B3 — Raw Export Authorization Decision + Permit**: built through four
  slices:
  - B3-1 inert typed schema at `5f23dcb`;
  - B3-2 identity functions and fingerprint codec at `bbec0f1`;
  - B3-3 persist function and evidence oracle at `4e30fa6`;
  - B3-4 fifteen-step authorization engine at `774a94e`.

  B3 is repository-only and deliberately inert of raw bytes. It creates immutable
  Authorized/Denied evidence and a metadata permit freezing the exact authorized
  classes, subject, session, recipient, policy/version, decision, and expiry. It
  does not authenticate an HTTP caller, resolve artifacts, assemble a package,
  encrypt, deliver, or consume a permit.
- **TIP-88B1-E3 — Resolver Runtime Read Boundary**: PostgreSQL 16 dedicated
  runtime-login posture, typed `SECURITY DEFINER` projections, zero direct runtime
  privileges on the protected tables, exact ACL/grantor manifests, fail-closed
  fulfillment materialization, and B2 constraint-mode normalization.
  Implementation `0a0eae8`; closeout `bf0b52e`.

## 2. Canonical sequence

```text
TIP-88A policy catalog
→ TIP-88B1 control plane
→ TIP-88B2 subject consent
→ TIP-88B1-E1 fulfillment expiry
→ TIP-88A-E2 permit TTL
→ TIP-88B3 authorization decision + metadata permit
→ TIP-88B1-E3 runtime read boundary
→ TIP-88B4 permit-to-job consumption foundation
→ TIP-88C1 secure raw source + sealed assembly
→ TIP-88C2 recipient encryption + package custody
→ TIP-88C3 authenticated delivery surface
→ TIP-88C4 lifecycle, operations + neutral technical E2E
→ Production Readiness Review / real-patient approval
```

`TIP-88B4` and `TIP-88C1..C4` are the ratified implementation-slice identifiers.
Their implementation folders and build briefs do not exist yet and must be
created only through their own reviewed planning dispatches.

SignFlow integration is a separate consumer-side slice in the SignFlow
repository. No SignFlow signing-session or transaction concept may enter the
TagEkyc Raw BIO core.

## 3. Program-wide locked invariants

### 3.1 Permit and ExportJob are separate aggregates

A permit has only this derived one-way logical lifecycle:

```text
Unclaimed → BoundToJob
```

The landed B3 permit row remains immutable: no later slice may add a mutable
consumed flag, update it, or unbind it. `Unclaimed` means no immutable job
identity exists for the permit; `BoundToJob` means one does. The bind is atomic
with job creation and protected by `UNIQUE(PermitId)`. A permit that has been
bound never becomes logically `Unclaimed` again, including after assembly
failure, delivery failure, cancellation, or expiry. Every mode-permitted retry
resumes the same logical job. A new logical export after terminal failure
requires a new authorization decision and permit.

B4 must keep three persistence surfaces distinct:

```text
Immutable ExportJobIdentity
- JobId
- UNIQUE PermitId
- frozen job contract
- never updated

Append-only ExportJobTransition / ExportAttempt
- every state transition, attempt, failure, reclaim, and reconciliation fact

Mutable ExportJobOperationalHead
- CurrentState
- Revision
- CurrentAttemptId
- LeaseOwner
- LeaseExpiresAt
- FencingToken
```

The atomic permit-to-job bind is one database transaction:

```text
validate immutable permit
→ insert ExportJobIdentity with UNIQUE(PermitId)
→ insert initial ExportJobOperationalHead
→ append initial transition/attempt evidence
```

Under concurrent bind attempts, `UNIQUE(PermitId)` selects one winner. A losing
request with the same valid idempotency fingerprint returns the existing
`JobId`. A request with a different fingerprint fails closed and creates no
second job.

Every operational-head transition uses compare-and-swap on:

```text
JobId + ExpectedRevision + ExpectedFencingToken
```

The head update and corresponding append-only ledger evidence are committed in
the same database transaction. Operational mutability is not permission to
rewrite business history.

The ExportJob owns the long-running lifecycle:

```text
Claimed
→ Assembling
→ AssemblySealed
→ Protecting
→ PackageSealed
→ ReadyForDelivery
→ DeliveryInProgress
→ Delivered
```

Permitted exceptional outcomes are:

```text
DeliveryOutcomeUnknown
ReconciliationExpired
TerminalFailed
Cancelled
Expired
```

`DeliveryOutcomeUnknown` is a reconciliation state, not automatically a success
or a terminal failure:

```text
DeliveryOutcomeUnknown
→ Delivered              only after a verified recipient receipt
→ ReconciliationExpired  after the reconciliation window
```

`ReconciliationExpired` preserves the fact that physical delivery remains
unknown; it must not be relabeled as failure merely because no receipt arrived.
`TerminalFailed` is reserved for affirmative evidence that delivery did not
occur, such as a verified recipient rejection before any bytes were accepted.

Retry or resume while the job is `DeliveryOutcomeUnknown` is permitted only when
the mode and delivery contract explicitly accept at-least-once or resumable
transfer of the same immutable sealed package. The job head remains
`DeliveryOutcomeUnknown`; each retry is a new append-only delivery attempt. A
verified receipt may move the head to `Delivered`; expiry of the reconciliation
window moves it to `ReconciliationExpired`. No retry may assemble a second
package.

A verified receipt must bind at least:

- immutable `PackageId` and package digest;
- the exact `DeliveryAttemptId`;
- frozen recipient and client identities;
- receipt result and timestamp;
- the recipient authentication/signature mechanism selected by TIP-88C3.

A receipt that does not bind the exact package and delivery attempt cannot move
the job to `Delivered`.

Transient conditions such as `AssemblyFailed` and `DeliveryFailedRetryable`
belong to append-only attempt records; they must not create ambiguous parallel
job states.

### 3.2 Exactly-once claim is limited and honest

The program may claim only:

- exactly one logical ExportJob per permit;
- exactly one immutable assembly identity per job;
- exactly one immutable final sealed-package identity per job;
- when the approved mode permits durable retry, idempotent retry/resume against
  that same job and final package; no-retain mode has no durable retry.

It must not claim exactly-once physical delivery over a network. A client may
receive all bytes and disconnect before a receipt persists. `Delivered` therefore
requires the receipt rule selected by TIP-88C3; otherwise the honest state is
`DeliveryOutcomeUnknown`.

### 3.3 Job identity and frozen contract

Job creation must freeze at least:

- authenticated principal;
- `ClientApplicationId`;
- verification session;
- subject binding;
- authorization decision and permit;
- policy and exact version;
- recipient identity;
- export mode;
- exact authorized raw classes;
- permit expiry;
- `JobExpiresAt`, derived and frozen during the atomic B4 job creation;
- idempotency identity/fingerprint.

The caller may not replace the recipient, key, class set, mode, session, or
subject on retry.

### 3.4 Lease, revision, and fencing

Long-running work must use at least:

```text
AttemptId
LeaseOwner
LeaseExpiresAt
FencingToken
Revision
```

Every state transition and publication intent must validate the current revision
and fencing token. If worker A loses its lease and worker B reclaims the job,
worker A must be unable to commit a late DB transition, mark an object
authoritative, mark the package deliverable, or persist `Delivered`.

`FencingToken` is monotonically increasing per `JobId` and is never reused.
Every new lease acquisition after no owner, lease expiry, or reclaim increments
it. Renewal by the same valid owner may extend `LeaseExpiresAt` while retaining
the current token, but it must never reduce or reset the token or reuse an older
one. Every `AttemptId` is unique, and reclaim creates a new `AttemptId`.
`Revision` increments on every successful operational-head mutation.

Fencing/CAS protects database state and publication intent; by itself it cannot
make an external network side effect exactly once. Object-store writes must be
conditional/idempotent by immutable package identity. Delivery must carry the
immutable package identity and a recipient idempotency key when the recipient
supports one. If an external sink cannot participate in fencing, a stale worker
may still physically send bytes after lease loss. That send remains within the
at-least-once/unknown-outcome contract, and the stale worker still cannot persist
`Delivered`.

A lease is not authorization. Lease acquisition or renewal never substitutes for
the revalidation checkpoints below.

### 3.5 Base and phase-specific revalidation

The assembly/delivery path must freshly revalidate:

1. when the permit is atomically bound to a job;
2. before every raw-read/assembly attempt;
3. after lease reclaim and before work resumes;
4. before a sealed package becomes deliverable;
5. before every delivery attempt, including retries.

The base authorization revalidation set includes:

- consuming principal equals the decision principal;
- consuming `ClientApplicationId` equals the frozen `ClientApplicationId`;
- session owner equals the frozen `ClientApplicationId`;
- frozen recipient identity remains unchanged;
- session subject equals the frozen subject;
- session remains `Completed`;
- grant remains effective;
- policy lifecycle remains export-active;
- bound rule set remains current;
- every required fulfillment remains current and unexpired;
- subject consent remains effective and covers every frozen class;
- permit remains within its valid use window.

Before C2 seals a package and before every C3 delivery attempt, phase-specific
revalidation additionally requires:

- the selected recipient key and delivery target bind to the exact frozen
  recipient under the approved policy/controller pair;
- `JobExpiresAt` has not passed;
- `PackageExpiresAt` has not passed;
- `DeliveryAuthorityExpiresAt` has not passed;
- the selected recipient-key version remains active and not revoked under the
  approved delivery policy;
- the sealed package integrity/authentication/signature is valid;
- the package still binds the exact frozen job identity and assembly identity.

C2's final manifest must bind the exact `AssemblyManifestHash`; it may not change
class order, digest, size, content type, or other C1 assembly facts. The exact
recipient-key version is frozen when the job reaches `PackageSealed`. After that
transition, the same job may not be re-encrypted or re-keyed. Recipient-key
revocation before delivery blocks delivery. A new key/package requires a new
decision, permit, and job unless a future reviewed slice explicitly ratifies a
rewrap protocol.

Revocation, withdrawal, session transition, rule-set change, fulfillment expiry,
or permit expiry after assembly but before delivery must prevent delivery and
trigger the selected purge/crypto-shred policy.

No database transaction or row/advisory lock may remain held across vault I/O,
package construction, KMS/HSM calls, object-store operations, or network
delivery.

### 3.6 Authorization, orchestration, package, and reconciliation deadlines

The program must keep these values distinct:

- `PermitExpiresAt`: absolute authorization ceiling; the permit must be bound
  before it, and no permit-authorized raw read, seal, or delivery may occur after
  it;
- `JobExpiresAt`: latest time at which orchestration may continue;
- `PackageExpiresAt`: custody/delivery expiry for the sealed package;
- `DeliveryAuthorityExpiresAt`:
  `min(PermitExpiresAt, JobExpiresAt, PackageExpiresAt)`;
- `ReconciliationExpiresAt`: latest time at which a late verified receipt may
  reconcile an unknown delivery outcome.

`JobExpiresAt` is derived and frozen during the atomic B4 job creation.
`PackageExpiresAt` is derived from the approved mode and policy and frozen
atomically with `PackageSealed`. `DeliveryAuthorityExpiresAt` is derived from
the three deadlines above and is never independently operator-set.
`ReconciliationExpiresAt` is frozen no later than the first delivery attempt or
the transition to `DeliveryOutcomeUnknown`.

None of these values may be extended in place. A policy requiring a later
authority or custody window requires a new authorization decision, permit, and
job. Cleanup may run after the deadlines, but it does not extend authority to
read raw data, seal a package, or deliver bytes.

`ReconciliationExpiresAt` may be later than delivery authority, but after
`DeliveryAuthorityExpiresAt` the system may only accept/verify receipts,
reconcile metadata, and clean up. It may not read raw data, seal, resume
transfer, or send more bytes. Processing a late receipt is not a new delivery
attempt. No `MaxValue` sentinel or silent conversion between these deadlines is
permitted.

### 3.7 Raw security boundary

- No raw BIO bytes are stored in PostgreSQL.
- No raw payload enters logs, exceptions, audit rows, telemetry, DTO diagnostics,
  or general caches.
- The public/runtime API process must not hold raw-vault credentials.
- Raw resolution and assembly execute inside an isolated worker security
  boundary.
- The resolver is scoped to one frozen job/session/class set and is not a
  general-purpose byte API.
- Raw data uses bounded streams or controlled buffers with deterministic
  disposal; no unbounded or long-lived `byte[]`.
- Unsupported classes, modes, providers, or key configurations fail closed and
  are readiness-visible.
- No raw resolver is production-enabled until sealed assembly and its integrity
  contract exist.

### 3.8 Sealed-package authenticity and recipient keys

A package hash alone is not an authenticity claim. C1's immutable assembly
identity is an internal input to C2, not a deliverable package. The final sealed
package created by C2 must bind the decision, permit, session, recipient,
policy/version, mode, exact classes, assembly identity, artifact
digest/size/content type, package expiry, and recipient key fingerprint/version
into a canonical authenticated manifest.

Package authenticity/signature and package encryption are distinct controls.
Production signing/encryption keys require managed custody and rotation.

Recipient keys must come from a trusted registry bound to
`ClientApplicationId`, with controlled enrollment, activation, revocation,
rotation, historical versions, authorization, readiness, and audit. An export
request may not substitute an arbitrary public key.

### 3.9 Mode-specific source, custody, and retry semantics

General retry language in this spine is subordinate to the selected mode:

| Mode | Source/custody contract | Retry contract |
| --- | --- | --- |
| `ExternalExportOnlyNoRetain` | Metadata job/evidence persists, but raw data and any assembled package exist only in bounded memory for one submission attempt. Terminal cleanup disposes them; there is no durable raw/package custody. | No durable retry after crash or failed submission. The bound job reaches its honest terminal/unknown outcome; another logical export requires a new authorization and permit. |
| `EncryptedExportPacket` | One final encrypted sealed package may be retained only for its approved TTL. | Retry/resume may serve the same immutable package while delivery authority remains valid. No reassembly or second package. |
| `EncryptedRawVaultRetained` | Track B. Durable encrypted raw-vault retention exists only for a separately approved mode × controller pair and its explicit retention/controller contract. Export demand alone never authorizes vault retention. | Defined only by that separately ratified pair; it must not inherit retry/custody semantics implicitly from another mode. |

C2 may implement only mode × controller pairs approved before its build
dispatch. Merely listing a mode in this spine or in the landed taxonomy is not
authorization to enable it. The C2 planning brief must identify the controller
binding and approval evidence for every enabled pair. An unapproved pair is
readiness-red and fail-closed.

### 3.10 Neutral core

The technical TagEkyc E2E is:

```text
completed TagEkyc verification session
→ authorization
→ permit-bound job
→ raw resolution
→ sealed authenticated/encrypted package
→ authenticated neutral recipient
→ delivery/receipt
```

SignFlow transaction/session binding remains consumer-side. TagEkyc accepts only
its existing opaque challenge/client-reference model and does not interpret a
SignFlow signing session.

## 4. Ratified implementation slices

### TIP-88B4 — Permit-to-Job Consumption Foundation

**Purpose:** Close logical one-job-per-permit orchestration before introducing a
raw-byte capability.

**Scope:**

- immutable `ExportJobIdentity`, including the frozen contract and
  `UNIQUE(PermitId)`;
- append-only `ExportJobTransition` / `ExportAttempt` evidence;
- mutable `ExportJobOperationalHead` containing current state, revision, current
  attempt, lease, and fencing token;
- one-transaction permit validation, immutable identity insert, initial head
  insert, and initial ledger append, without updating the landed B3 permit;
- CAS head transitions on job id, expected revision, and expected fencing token,
  with the corresponding ledger append in the same transaction;
- idempotency identity/fingerprint;
- frozen job contract from section 3.3;
- revision, lease, fencing, reclaim, retry, cancellation, expiry, and terminal
  semantics;
- all non-I/O revalidation required to claim/resume/finalize fixture work;
- a fixture assembler/delivery harness solely to mutation-test orchestration;
- migration, ACL, readiness, race, crash, stale-worker, and rollback gates.

**Exit evidence:**

```text
authorize
→ bind one permit to one job
→ worker crash/lease expiry
→ fenced reclaim
→ retry the same job
→ exactly one logical job and no duplicate logical consumption
```

**Explicit non-scope:** raw artifact reads, vault credentials, raw bytes, real
package/manifest, recipient keys, encryption, public HTTP surface, and any
physical-delivery claim.

### TIP-88C1 — Secure Raw Source + Sealed Assembly

**Purpose:** Establish an approved secure Raw BIO source, resolve only the frozen
authorized classes inside an isolated worker, and create one canonical immutable
assembly result. This assembly seal is not the final encrypted deliverable
package.

**Current-source premise:** today the CaptureAgent keeps raw capture material in
memory, emits evidence/hash metadata, and disposes the raw material. TagEkyc has
no server-side raw source or vault for C1 to resolve. C1 therefore owns source
establishment as well as source resolution; it may not assume a provider exists.

**Scope:**

- trusted capture/source handoff and its authenticated producer boundary;
- a capture/source-establishment gate for selected mode, policy, and the
  applicable consent/legal basis;
- an explicit sequence consistent with landed B2, whose subject export consent
  can be granted only after the verification session is `Completed`; B2 consent
  must be effective before raw read/export and must not be treated as retroactive
  authorization for earlier retention;
- exact session, subject, raw class, producer, and source-artifact binding when
  the source is established;
- stable source-artifact identity and integrity digest;
- per-artifact encryption before persistence for every durable-source mode;
- scoped vault/provider contract and explicit mapping for each supported
  `RawExportRawClass`;
- session/subject/artifact binding and integrity verification;
- bounded streaming and deterministic buffer disposal;
- canonical internal assembly manifest with artifact digest, size, and content
  type;
- one immutable assembly identity per job;
- assembly integrity/authentication contract suitable as the frozen input to C2;
- unsupported class/provider/mode readiness and fail-closed behavior;
- corruption, wrong-session, wrong-class, missing-artifact, cancellation,
  stale-fence, and crash tests.

For `ExternalExportOnlyNoRetain`, C1 must prove an explicit same-process
ephemeral-source sequence through the one submission attempt; it may not silently
stage a durable raw source. For every durable-source mode, the encrypted source
must exist under an approved source-retention contract before the resolver is
enabled.

If no real source provider exists, the handoff cannot satisfy the selected mode,
or the source/provider is not approved and readiness-valid, C1 must STOP. It may
not land a production reader pointed at an assumed or fixture-only source.

**Enablement restriction:** no general raw-reader API; no public/runtime reach;
no durable plaintext assembly. Real raw providers remain production-disabled
until TIP-88C2 supplies the final authenticated/encrypted package and encrypted
custody.

**Explicit non-scope:** recipient-key enrollment, durable encrypted custody,
final delivery-package identity, public download/delivery, or claims that a
recipient received the package.

### TIP-88C2 — Recipient Encryption + Package Custody

**Purpose:** Transform C1's immutable assembly into the final authenticated and
encrypted sealed package for the frozen recipient, then manage its bounded
encrypted lifetime.

**Scope:**

- trusted recipient-key registry bound to `ClientApplicationId`;
- key enrollment, activation, revocation, rotation, historical version, and
  administrative audit;
- canonical final package manifest binding the C1 assembly identity and recipient
  key fingerprint/version;
- one immutable final sealed-package identity per job;
- final manifest authentication/signature;
- envelope encryption and recipient-key fingerprint/version binding;
- production signing/encryption key custody;
- encrypted temporary object custody;
- TTL, retention-mode behavior, purge, and crypto-shred;
- exact implementation/readiness posture for only the mode × controller pairs
  separately approved before C2 dispatch, under section 3.9.

The landed taxonomy includes `ExternalExportOnlyNoRetain`,
`EncryptedExportPacket`, and `EncryptedRawVaultRetained`; that enumeration is not
authorization to enable all three. Any unapproved or incomplete pair must be
rejected by policy/readiness. Silent fallback between modes is forbidden.

**Explicit non-scope:** public download API, delivery receipts, and
SignFlow-specific integration.

### TIP-88C3 — Authenticated Delivery Surface

**Purpose:** Expose authorization/job status and delivery of only encrypted sealed
packages to the correctly authenticated neutral recipient.

**Scope:**

- authenticated application actor derived from the existing authentication
  context, never arbitrary actor GUIDs from request JSON;
- same-client ownership and non-enumerating not-found/not-owned normalization;
- request/status/delivery DTOs and application-service boundary;
- opaque delivery handles that are not raw permit or internal vault references;
- mode-permitted retry/resume and partial-transfer semantics over the same
  immutable package; no-retain mode has no durable retry;
- verified receipt contract binding the exact immutable package identity/digest,
  delivery attempt, frozen recipient/client identities, result/timestamp, and
  C3-selected recipient authentication/signature mechanism;
- `DeliveryOutcomeUnknown` reconciliation and a distinct
  `ReconciliationExpiresAt`;
- retry attempts while outcome is unknown remain append-only attempts without
  changing the job head back to `DeliveryInProgress`;
- replay prevention, rate/size/time limits, cancellation, and correlation;
- no-cache/content handling appropriate for Raw BIO;
- base and phase-specific revalidation before every delivery attempt, including
  delivery authority, package expiry, recipient-key status, package
  integrity/signature, and frozen job/assembly identity;
- an explicit acknowledgement that fencing protects authoritative state and
  intent but cannot alone prevent a stale external network send;
- delivery process reads encrypted sealed packages only and holds no raw-vault
  credential.

**Explicit non-scope:** SignFlow transaction semantics, multi-hospital shared
database support, and a claim of exactly-once physical network delivery.

### TIP-88C4 — Lifecycle, Operations + Neutral Technical E2E

**Purpose:** Close the technical production data plane before organizational
go-live approval.

**Scope:**

- metadata-only audit for authorization, claim, assembly, sealing, key choice,
  delivery attempts, receipts, reconciliation, purge, and failure;
- purge/orphan recovery worker;
- legal-hold/retention execution hooks and package-expiry enforcement;
- readiness for vault, providers, KMS/HSM, recipient keys, storage, and workers;
- quotas, disk pressure, backpressure, monitoring, and alerting;
- backup inclusion/exclusion and restore/failure drills;
- full crash/concurrency/tamper/revocation matrix;
- a real technical neutral E2E from completed TagEkyc session through receipt,
  with no SignFlow-specific concept.

Closing TIP-88C4 proves technical readiness of the Raw BIO data plane. It does
not itself authorize real-patient use.

## 5. Gates after the implementation slices

### 5.1 SignFlow consumer integration

SignFlow verifies the neutral package and binds it to its own transaction in the
SignFlow repository. This work consumes the TagEkyc contract and must not add
SignFlow concepts to TagEkyc core.

### 5.2 Production Readiness Review

After TIP-88C4, a separate PRR/go-live evidence package must cover at least:

- stable DG2 artifact-hash disposition;
- DPIA, consent text, purpose, recipient, and jurisdiction approval;
- raw retention, deletion, legal hold, backup, and breach procedures;
- capture-agent/device trust;
- production face/liveness assurance;
- vault/KMS/HSM and recipient-key operational ownership;
- role/bootstrap/readiness evidence;
- threat testing, incident response, recovery drills, and operator training.

PRR and real-patient approval are release gates, not substitutes for the technical
implementation required by TIP-88B4 and TIP-88C1..C4.

## 6. Governance

- Do not build from this spine.
- Each slice requires its own planning brief, adversarial review, explicit
  dispatch, allowlist, migration/ACL gates where applicable, mutation proofs,
  closeout, and controlled commit.
- A later slice may consume only the frozen contract of a closed predecessor.
- No slice may weaken the existing no-raw-byte guarantees outside the explicitly
  authorized isolated worker/package boundary.
- Any change to tenant topology, actor authentication, recipient ownership,
  SignFlow neutrality, or real-patient approval requires a separate decision,
  not an incidental implementation patch.
