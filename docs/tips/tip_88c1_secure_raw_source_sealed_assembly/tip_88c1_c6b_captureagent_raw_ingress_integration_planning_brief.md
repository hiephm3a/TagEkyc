# TIP-88C1-C6B — CaptureAgent raw-ingress integration planning brief

**Status:** CANDIDATE v1.8 — BOUNDED RRI PROPAGATION CORRECTION APPLIED — REVIEW REQUIRED  
**Date:** 2026-09-05  
**Risk tier:** High-risk  
**Server baseline:** merge commit `bf10ec8da93c5dd2c22e808e08f30cf835023601`  
**CaptureAgent baseline:** `b193f6316e13a82fb2f9f985f68500feae194cfa`

## PI-TAG-001 pilot activation

```text
PI-TAG-001 PILOT
Pilot TIP: TIP-88C1
Reviewed slice: TIP-88C1-C6B
Risk tier: High-risk
Selected modules: SQL/schema/transaction; API; cryptography/key management;
                  raw/restricted data; governance/docs
Worker/queue module: applicable only to existing R3-R6 continuation and cleanup;
                     no new queue topology is authorized
Required matrices: Invariant Trace; Outcome and Precedence; Shape and
                   Nullability; Ordering Graph; Test-Bite
Independent reviewers: required — cross-repository raw-data transport changes
Round-5 root-cause checkpoint: enabled
Round-10 hard stop: enabled
Pilot metrics report: required before closeout
```

## 1. Objective

C6B connects the existing `TagEkyc.CaptureAgent` producer to the already-landed
TIP-88C1 custody pipeline. It adds the missing external transport/composition
edge only. It does not create another upload system, authority engine,
retention model, encryption pipeline, object store, package flow or delivery
surface.

The initial external flow is the one-call protocol already ratified by the
TIP-88C1 planning spine:

```text
CaptureAgent metadata phase
  -> server authentication and metadata validation
  -> consent-backed retained-source authority + ingress begin
  -> broker complete + committed R1 reservation/re-entry
  -> application begins one bounded plaintext stream only after committed R1
  -> existing R2 provisional encryption/object custody
  -> mandatory landed R3 authority/consent re-read
  -> R4 commit re-read -> R5 publication re-read and `Available`
  -> R6 obsolete-residue cleanup
  -> C1/C6A re-read where later reuse/delivery applies
  -> one typed final result
```

Before committed R1 no application code may read/copy body content, arm an
R2/provider writer, or own request-body plaintext. Unavoidable physical
network/kernel/TLS/proxy bytes may exist only within the configured memory-only
pre-admission bound; they may never spill and must be discarded on rejection.

## 2. Reconciliation: what already exists

| Capability | Existing owner | C6B disposition |
| --- | --- | --- |
| policy/class authorization | TIP-88A/88B | reuse unchanged |
| Consent, Grant, Permit and job identity | TIP-88B | reuse unchanged |
| AuthoritySnapshot and production composition pattern | C6A | reuse its lock/derivation pattern for the consent-backed retained-source profile; current function is `EncryptedExportPacket`-only and requires the bounded retained-mode composition extension |
| alias/exact locks, replay and R1 reservation | C1 B1/B2 | reuse unchanged |
| attempt-key reservation and KEK operation | B2 key custody | reuse unchanged |
| framed streaming AEAD and commitment verification | R2 | reuse codec/custody mechanics; the ingress path must pass the existing fresh-authority barrier inside R1 `complete`; add no R2/post-R1 barrier |
| provisional object conditional write | R2/object custody | reuse unchanged |
| R3 verification/staging | R3 | reuse unchanged |
| publication/finalization/cleanup | R4-R6 | reuse unchanged |
| assembly, recipient package and delivery | C1-C5 | out of C6B mutation scope |
| qualified custody-ingress transport | absent | select only after transport qualification; an HTTP route is conditional, not pre-decided |
| CaptureAgent raw-ingress client method | absent | implement |
| CaptureAgent buffer-to-request wiring | absent | implement |

Evidence of the missing transport edge:

- server API maps verification/capture-metadata, recipient-management,
  package-reference and delivery routes, but no raw-source ingress route;
- `RawExportR2EncryptionOrchestrator` accepts a caller-owned `Stream` but is an
  internal service reached today by integration/fixture composition;
- CaptureAgent `TagEkycHttpClient` submits capture metadata as JSON and has no
  raw-stream method;
- CaptureAgent already owns DG2 and live-selfie `SensitiveByteBuffer` instances
  and zeroes/disposes them at run termination.

### 2.1 D1 decision-aid reconciliation against landed code

The fourteen planning-era D1 rows are not fourteen open Homeowner questions.
Their current disposition is:

| D1 sub-decision | Current classification | Landed evidence / remaining boundary |
| --- | --- | --- |
| Mode × controller pair | `RATIFIED_BY_HOMEOWNER` | Hospital is controller; TagEkyc deployment/operator is processor/custodian. Data-subject rights, notice and accountability remain with the hospital. TagEkyc executes controller instructions and does not decide independently. `ControllerIdentity` is durably required at `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs:37,56-58`. |
| Legal basis and retention authority | `ANSWERED_BY_CONSENT_MACHINERY` | The purpose-specific-consent option is implemented by `raw_export_subject_consent_events`: PolicyId/version, purpose, revisions, Granted/Withdrawn, text version/hash, external artifact and actor evidence at `20260720022629_Tip88B2SubjectExportConsent.cs:41-74`; class scope at `:77-89`; recorder/withdrawer authority at `:14-38,417-455`. This machinery records and withdraws exact purpose/class consent. Jurisdictional legal sufficiency remains a DPO/counsel attestation about use of this implemented option, not missing technical machinery or a new authority model. |
| Retention start/end and classes | `RATIFIED_BY_HOMEOWNER` | Retention starts when server custody is accepted and ends at the earliest of consent expiry, purpose completion or data-subject withdrawal instruction; use the shortest applicable class. Snapshot columns carry `RetentionClass`, `RetentionStartEvent` and `AbsoluteSourceExpiresAtUtc`: `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs:44-46,56-58`. |
| Extension and reuse authority | `ANSWERED_BY_LANDED_CODE` | Snapshot constraints freeze `ReuseDisposition='FreshAuthorityRequired'` and `ExtensionDisposition='Forbidden'`: `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs:45-46,58`; C1 rechecks both before binding: `20260815120000_Tip88C1C1ResolverAssembly.cs:519-526`. |
| Purge authority | `RATIFIED_BY_HOMEOWNER`; enforcement `MISSING — SEPARATE LIFECYCLE TIP / C6B PRODUCTION-ACTIVATION BLOCKER` | Hospital/controller owns deletion policy. The isolated custody service executes it idempotently and records execution evidence. `PurgePolicyId` is required metadata and object cleanup supports `SourceExpired`: `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs:56-58`; `20260804120000_Tip88C1B2DurableObjectCustody.cs:472-476`. Semantic authority is closed; the missing checkpoint is owned by a separately dispatched lifecycle-enforcement TIP, not C6B ingress implementation. |
| Legal hold and precedence | `RATIFIED_BY_HOMEOWNER`; enforcement `MISSING — SEPARATE LIFECYCLE TIP / C6B PRODUCTION-ACTIVATION BLOCKER` | Hold blocks both deletion and ordinary read/reuse. Hold is not use authority; access during hold requires separate authority. `LegalHoldPolicyId` is required metadata at `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs:56-58`. Semantic precedence is closed; the missing enforcement consumer is owned by the same separate lifecycle TIP, not C6B ingress implementation. |
| Access authority | `ANSWERED_BY_LANDED_CODE` | Normal reuse is per-job: B4 freezes Permit/job expiry (`20260726145547_Tip88B4RawExportJobFoundation.cs:14-43`), and C1 binds each selected source with fresh authority (`20260815120000_Tip88C1C1ResolverAssembly.cs:483-526,561-567`). |
| Capture-time ingress identity | `ANSWERED_BY_LANDED_CODE` | Acceptance/selection tables freeze CaptureAcceptanceId, CaptureArtifactId and CaptureRevision (`20260730090000_Tip88C1ACaptureAcceptanceSurface.cs:19-35,57-71`); B1 exact identity uniqueness includes client/producer/session/artifact/revision/class (`20260730214000_Tip88C1B1IngressClaimIdempotencySkeleton.cs:21-41`). |
| Authority loss before `Available` | `ANSWERED_BY_LANDED_CODE` | R4 commit and R5 publication freshly resolve authority and block progression when it no longer matches or has expired: `20260812120000_Tip88C1B2R4R6SourceFinalization.cs:369-380,492-504`. R5 is the transition to `Available`; R6 only cleans obsolete residue. Physical purge/hold semantics are ratified above; their missing enforcement checkpoints are the separate lifecycle-TIP activation blockers recorded in §9.1. |
| Authority loss after `Available` | `ANSWERED_BY_LANDED_CODE` | C1 rejects expired/current-authority mismatch before reuse (`20260815120000_Tip88C1C1ResolverAssembly.cs:514-526`); C6A injects the same current-authority barrier into delivery (`20260904154848_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs:221-241`). Default is immediate logical deny; `SourceExpired` uses the landed cleanup path (`20260804120000_Tip88C1B2DurableObjectCustody.cs:472-476`). |
| C2 authority cutoff | `ANSWERED_BY_LANDED_CODE` | C1/C2 freezes the source authority into job/source bindings and preserves exact committed convergence; fresh authority checks precede new use (`20260815120000_Tip88C1C1ResolverAssembly.cs:703-746`; `20260818120000_Tip88C1C2RecipientPackage.cs:376-420`). |
| Accepted-source discriminator | `ANSWERED_BY_LANDED_CODE` | Exact acceptance and server selection surfaces are landed with immutable foreign keys and uniqueness (`20260730090000_Tip88C1ACaptureAcceptanceSurface.cs:19-85`), and B1 consumes those exact identifiers (`20260730214000_Tip88C1B1IngressClaimIdempotencySkeleton.cs:363-380`). |
| B4 amendment ownership | `ANSWERED_BY_LANDED_CODE` | B4 job/permit foundation is landed (`20260726145547_Tip88B4RawExportJobFoundation.cs:14-43`), and C1 adds the source-binding compatibility surface (`20260815120000_Tip88C1C1ResolverAssembly.cs:61-157`). No ownership decision remains. |
| Delivery boundary | `ANSWERED_BY_LANDED_CODE` | Delivery stayed outside C1: C2 owns recipient package preparation (`20260818120000_Tip88C1C2RecipientPackage.cs:376-420`) and C3 owns authenticated delivery (`20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs:14-104`). |

Purpose-specific consent is technically complete but legal sufficiency remains
an explicit production acceptance condition. Consent withdrawal may race after
committed R1; encrypted provisional ciphertext may then exist transiently. It
must never become `Available` or be delivered: the next mandatory landed
consent/current-authority gate refuses progression, standard cleanup deletes
the provisional ciphertext, and deletion evidence is retained. Production
activation requires DPO/counsel acceptance that this bounded encrypted
provisional residue is compatible with the applicable withdrawal and retention
obligations. If it is not accepted, the architecture must be revisited rather
than the wording adjusted.

The four formerly open D1 rows are ratified as follows:

| Ratified row | Homeowner disposition | Consequence |
| --- | --- | --- |
| Mode × controller pair | Hospital is controller; TagEkyc deployment/operator is processor/custodian. | Hospital owns rights, notice and accountability; TagEkyc only executes controller instruction. |
| Retention start/end and classes | Start at accepted server custody; end at the earliest of consent expiry, purpose completion or subject withdrawal; shortest applicable class. | No longer retention and no in-place extension may be inferred. |
| Purge authority | Hospital/controller owns policy; custody service executes idempotently with evidence. | Execution authority does not transfer policy ownership to TagEkyc. |
| Legal hold and precedence | Hold blocks deletion and ordinary read/reuse; exceptional access requires separate authority. | Hold can never be interpreted as reuse permission. |

`D1_HOMEOWNER_SUBDECISIONS_OPEN = 0`. The nine
`ANSWERED_BY_LANDED_CODE` rows and one
`ANSWERED_BY_CONSENT_MACHINERY` row remain closed and are not reopened.

## 3. Frozen initial profile

| Item | C6B value |
| --- | --- |
| producer | `TagEkyc.CaptureAgent` |
| operation topology | one authenticated metadata + bounded-body operation |
| initial raw classes | exactly `ChipDg2Portrait`, `LiveSelfieImage` |
| liveness/heavy media | rejected before source read |
| server source/custody mode | TIP-88C1 D1 `EncryptedRawVaultRetained` |
| C6A current policy profile | `EncryptedExportPacket`; not interchangeable with the D1 retained-source mode |
| CaptureAgent local retention mode | remains `VerifyAndDiscard`; server encrypted custody never enables agent `RawVault` |
| plaintext ownership | CaptureAgent owns source buffer; server owns only bounded active-stream buffers |
| retry identity | same immutable client/producer/session/capture/revision/class identity |
| upload sessions/parts/resume | prohibited for the initial profile |
| public completion endpoint | prohibited; completion is internal to the one call |

This brief does not reopen the initial mode or class decisions and does not
substitute `ExternalExportOnlyNoRetain` for the ratified custody profile. The
C6A function currently hard-requires policy `Mode='EncryptedExportPacket'`.
Calling it unchanged for this retained-source flow would deterministically fail
with `RAW_EXPORT_PRODUCTION_AUTHORITY_NOT_FOUND`. Section 2.1 establishes that
the consent authority path, fresh per-job reuse, capture identity, authority
loss behavior, C2/B4 ownership and delivery boundary are already answered.
The controller pair, retention rule, purge authority and legal-hold precedence
are now Homeowner-ratified in section 2.1. A bounded server implementation may
therefore extend the C6A pattern for
`EncryptedRawVaultRetained`; CaptureAgent never selects the policy.

## 4. External operation contract

The public transport shape remains conditional on
`C6B-TRANSPORT-FRAMING-01`. If the deployed HTTPS/proxy topology proves a
metadata-first admission boundary (for example a qualified Expect/continue
exchange), the candidate route is:

```http
POST /api/ekyc/raw-export/source-ingress
```

Otherwise the implementation dispatch must select another already-supported
qualified one-call transport, such as a duplex RPC, that preserves the same
external contract. C6B must not add upload sessions, parts, resume or a public
completion operation merely to realize the transport.

Authentication uses the existing CaptureAgent API-key boundary. The caller
must have the existing capture-agent identity and an exact raw-ingress scope;
scope naming and provisioning must be reconciled with the current API-key
catalog before dispatch. C6B must not reuse recipient or management scopes.

### 4.1 Metadata phase input

The operation carries the already-ratified canonical identity and admission
fields, including:

- verification-session and artifact observations supplied by the request;
- canonical client/producer/instance identity derived server-side from
  authentication plus the exact accepted artifact;
- capture artifact id, capture revision and raw class;
- source-event/retry identity and idempotency key;
- media type, declared plaintext length and exact `ClaimedPlaintextDigest`;
- capture timestamp;
- `PlaintextRetentionStartedAtUtc`, `PlaintextRetentionExpiresAtUtc` and
  `PlaintextRetentionBudgetSeconds` as authenticated producer claims.

CaptureAgent does **not** supply policy, decision, Permit, job, AuthoritySnapshot
or a keyed `ContentCommitment`. The server resolves the consent-backed,
Homeowner-ratified retained-source authority and derives
`EffectivePlaintextRetentionExpiresAtUtc` from the authenticated producer
claims and server caps. `AbsoluteSourceExpiresAtUtc` is externally owned by the
separately ratified controller AuthoritySnapshot; the server reads and freezes
it but never synthesizes it from producer claims. `ReservationExpiresAtUtc` is
the server-derived minimum of the lease, effective retention and that absolute
authority horizon. No fallback is supplied for a missing producer claim or
authority horizon. Permit and job are later B3/B4
artifacts and are structurally unavailable at capture-time R1-R6.

The exact DTO is a direct projection of the already-frozen D4.2 external input
shape. Builder invention of a second identity, TTL, authority value or optional
field is prohibited.

### 4.2 Metadata-only outcomes

`AlreadyAvailable`, exact replay, identity/fingerprint conflict, busy,
unsupported class, `SOURCE_RETENTION_NOT_AUTHORIZED`, invalid horizon and capacity rejection
must return without `AdmissionAccepted`, without reading the request body and
without creating another R1/R2/key/object generation unless the landed state
machine expressly requires a same-owner re-entry.

### 4.3 Body-required outcome

Only a committed New/re-entry R1 result may permit application body
consumption. After committed R1 the application may begin reading
`Request.Body`; under the qualified HTTP realization Kestrel may then emit the
normal `100 Continue` and CaptureAgent may transmit the body.

No application body read/copy/ownership or R2/provider writer may occur before
committed R1.

The accepted body is one non-seekable bounded stream. It is passed directly to
the existing R2 orchestrator; it is never materialized as a whole server-side
byte array and never written as plaintext to disk, logs, telemetry or database.

### 4.4 Final output

The response is the already-frozen exhaustive `CaptureAgentFinalResult` union,
distinct from process-internal `InternalClaimResult`. C6B verifies and projects
the exact variant/code/field manifests from the spine; it does not redesign or
extend them. It returns stable identifiers and dispositions only. It must
not return plaintext, a raw digest, provider/storage locator, DEK/KEK material,
internal token, SQL identity or stack detail.

## 5. CaptureAgent integration

C6B adds one raw-ingress method to the existing CaptureAgent client boundary.
The current device adapters construct and transfer already-owned
`SensitiveByteBuffer` values before the orchestrator receives them. Therefore
orchestrator-only wiring cannot satisfy the capacity boundary. The
implementation surface must include the bounded acquisition/result-port and
device-adapter ownership transition (`CaptureAgentPorts.cs`,
`Hn212CccdReader.cs`, `NoTempFileFaceCamera.cs`, and
`Hn212FaceCaptureFrameStore.cs`) so the existing SDK/device transient capture
is distinguished from retained application ownership. The orchestrator calls
raw ingress only while the capacity-backed retained buffer remains alive:

```text
capture DG2/selfie into the existing transient capture buffer
  -> determine the exact retained raw length and class
  -> atomically reserve the applicable independent local buffer slot and exact-byte capacity
  -> if reservation fails, zero/dispose immediately and make no external call
  -> retain DG2/selfie under their separate reservation tokens
  -> complete existing verification use while carrying each existing reservation token
  -> send metadata without starting body transmission
  -> await AdmissionAccepted
  -> expose the existing buffer as a bounded readable stream
  -> await final result
  -> zero/dispose under the existing ownership path
  -> release each reservation on every terminal path
```

The client must support true deferred body production. A normal eager
`StreamContent` request that can send bytes before admission is not sufficient.
The implementation dispatch must pin the qualified transport mechanism that
preserves the metadata/R1/body phase boundary over the selected deployed
transport and intermediary topology. HTTPS/proxy-specific assertions apply
only if the HTTP branch is qualified.

### 5.1 CaptureAgent retry projector

The agent owns an explicit raw-ingress retry projection; generic immediate
retry is prohibited. It preserves the spine's exact distinctions:

- capacity/idempotency/reservation busy waits only for `BusyRetryBackoff`;
- live claim evaluation waits until exact `RetryNotBeforeUtc`;
- before every wait, `ClientPreWaitContinuation` uses monotonic remaining
  buffer lifetime and requires `ProducerBufferAvailable` plus sufficient time
  for wait and full continuation;
- pending termination returns busy; durably settled termination plus sufficient
  lifetime permits same-owner/same-UUID re-entry;
- disposed, unavailable, expired or insufficient-lifetime buffer returns
  `RECAPTURE_REQUIRED`;
- invalid producer retention and clean commitment mismatch never retry the same
  buffer/UUID;
- incomplete transport/temporary-unavailable may retry only through the exact
  fenced-cleanup rule.

## 6. Transaction and I/O boundaries

- No PostgreSQL/advisory lock or EF transaction spans network body I/O, key
  provider I/O or object-provider I/O.
- Two distinct atomic boundaries remain intact: C6A-style server authority
  derivation plus `begin`, followed by broker `complete` plus R1. They are not
  collapsed into one database transaction.
- The server commits R1 before emitting admission.
- C6B adds no post-R1 authority checkpoint. Fresh pre-capture authority is
  already checked inside broker-invoked R1 `complete`, before reservation and
  encryption-attempt admission. After R2 creates only provisional encrypted
  custody, mandatory landed re-reads at R3, R4 commit and R5 publication block
  progression on withdrawal; R6 performs cleanup, while later C1 reuse and C6A
  delivery repeat the check where applicable.
- Consent withdrawal is append-only. The endpoint must not route around any
  mandatory downstream gate. No database/advisory lock may span body, provider
  or raw-reader I/O, and a withdrawal after one checkpoint is observed at the
  next mandatory checkpoint rather than retroactively cancelling completed I/O.
- R2 consumes the body only after admission and owns only its local scratch and
  crypto buffers; it does not dispose the caller-owned request stream.
- Disconnect/incomplete body follows the ratified temporary-unavailable,
  fenced-cleanup and same-identity retry path.
- Clean EOF length/commitment mismatch is terminal and cannot be relabeled as
  a retryable transport failure.
- C6A composition and broker `complete` execute through the existing broker
  capability role. The ordinary runtime DbContext cannot be assumed to hold
  broker EXECUTE; the exact dedicated connection/role boundary is
  `ACTUAL_MISSING_IMPLEMENTATION` until census proves an existing path.

## 7. Reuse prohibitions

C6B must not add:

- a second claim/idempotency ledger;
- a second Consent/Policy/Grant/Permit evaluator;
- a raw vault or plaintext persistence table;
- a new R2 codec, encryption format or object provider;
- multipart, chunk-upload, resume or public-complete endpoints;
- a CaptureAgent-local durable raw cache;
- new retention durations or fallbacks;
- package/download changes;
- production activation claims.

## 8. Required proofs

| Proof | Required observation | Mutation that must fail |
| --- | --- | --- |
| C6B01 external operation contract | exactly one authenticated ingress operation; authenticated context plus exact accepted artifact derive all three canonical identity fields; one final union | add init/part/complete, caller identity authority or an extra public surface |
| C6B02 admission order | R1 commit precedes first application body read/R2 byte; unavoidable physical early bytes remain bounded memory-only | application reads before R1 or physical buffering exceeds the bound/spills |
| C6B03 metadata-only | invalid/cross-client/cross-agent/cross-device/null artifact identity returns existing `RAW_EXPORT_SOURCE_BINDING_INVALID`, with zero request-body reads and zero claim/broker/R1/R2/provider opens | read one byte or reach broker for invalid identity |
| C6B04 exact replay | response loss/retry returns the same server-derived durable identity and exact persisted terminal outcome without duplicate snapshot/claim/R1 | append another generation, use caller identity or collapse terminal causes |
| C6B05 class closure | only DG2 portrait and live selfie reach admission | admit liveness/unknown class |
| C6B06 length bounds | declared oversize fails pre-I/O; actual overflow stops and cleans up | accept limit + 1 |
| C6B07 incomplete vs mismatch | abort is retryable with null semantic terminal field; clean mismatch atomically persists its exact replay-stable code | collapse both outcomes or overload operational disposition |
| C6B08 no plaintext residue | application/DB/files/logs/traces/temp paths contain no raw bytes or bare digest; bounded physical network memory is measured separately | enable application/full-body buffering or spill |
| C6B09 replay and I/O boundary | database/advisory locks are absent during body/key/object I/O; exact terminal outcome is atomically durable before a replay can observe terminal state | retain transaction into R2 or expose terminal-with-null outcome |
| C6B10 CaptureAgent lifetime | device/SDK transient capture obtains exact length/class; independent DG2/selfie capacity tokens are then acquired atomically before constructing/transferring application-owned `SensitiveByteBuffer` results or making any external call; reservation failure immediately zeroes/disposes transient bytes; retained buffers remain alive through final result; zero/dispose and counters return to baseline on every terminal path; liveness is neither counted nor sent | construct/transfer retained buffer before reservation, retain/call externally after failed reservation, share a token, leak a counter or count/send liveness |
| C6B11 retention authority | missing/expired retention authority yields exact `SOURCE_RETENTION_NOT_AUTHORIZED` and zero body; `RAW_EXPORT_AUTHORITY_INVALID` is unreachable at ingress | authorize from agent assertions or emit the B4/assembly code |
| C6B12 two-repo E2E | generated non-patient DG2/selfie traverses the selected real client+transport adapter→R1→R2→R3/R6 with exact provenance; HTTP-specific assertions apply only if HTTP is qualified | bypass the selected real client/adapter |
| C6B13 agent retry projection | table-driven projection preserves temporary/re-entry and resume classes, while exact durable terminal codes never retry; Available publication wins over stale prior terminal residue | persist temporary/resume as terminal, retry clean mismatch, or prefer old attempt residue over publication |
| C6B14 broker ACL | dedicated broker path succeeds; ordinary runtime and PUBLIC are denied; proof never runs as owner/superuser | grant broker SQL EXECUTE to runtime/PUBLIC or run the positive proof as owner |
| C6B15 mandatory-authority-gate non-bypass | Against the external endpoint, prove no path reaches `Available` unless the R1, R3, R4-commit and R5-publication consent/current-authority gates execute and pass; R6 then performs cleanup. No path reaches later reuse or delivery eligibility unless the applicable C1/C6A gate also executes and passes. Trace R1 -> R2 provisional -> R3 -> R4 commit -> R5 publication/Available -> R6 cleanup -> C1/C6A as applicable. Withdrawal between R1/R3, R3/R4, R4/R5, R5/C1 or C1/C6A is refused by the next mandatory gate. Reuse landed TIP proofs for each gate; C6B proves its ingress composition cannot bypass them. | add an endpoint shortcut from R2/provisional or any recovery/replay branch around the next mandatory gate |

Real patient data is prohibited in C6B implementation proof. C6C owns the
separately authorized real-source operational evidence.

## 9. Planning findings to close before implementation dispatch

These are technical reconciliation tasks, not new product semantics:

1. `C6B-TRANSPORT-FRAMING-01`: qualify the selected ASP.NET Core/HTTP client
   `Expect: 100-continue` transport against the bounded memory-only physical
   buffering and post-R1 application-consumption properties. It must keep the
   concrete infinite client wait and reject timer fall-through, intermediary
   auto-continue, full-body buffering or disk spill. No explicit Kestrel interim
   API or gRPC switch is required.
2. `C6B-COMPOSITION-SURFACE-01`: map the existing internal begin/R1/R2-R6
   services into one coordinator without duplicating state transitions, while
   preserving the two atomic boundaries described in section 6.
3. `C6B-AUTH-SCOPE-01`: `ACTUAL_MISSING_IMPLEMENTATION`. Add one exact
   `capture.*` raw-ingress scope through the existing API-key catalog,
   production provisioning and readiness paths; `capture.artifact.append`,
   recipient and management scopes must be denied.
4. `C6B-DTO-PROJECTION-01`: verify direct projection of the already-frozen D4.2
   external input and `CaptureAgentFinalResult` manifests; this is census, not
   a new DTO decision.
5. `C6B-CAPTURE-BUFFER-01`: map the shared Host/UI acquisition flow and the
   exact device/result-port ownership transition. The implementation dispatch
   must include the bounded port/device changes required to reserve capacity
   after transient capture reveals exact length/class but before an
   application-owned `SensitiveByteBuffer` is constructed or transferred.
6. `C6B-READINESS-01`: census every already-ratified capacity key from the spine
   as `IMPLEMENTED` or `MISSING`, preserving its exact name, range, owner and
   local-before-call semantics; no new configuration decision is permitted.
   The same census must cover every spine §5.4 mandatory current-authority
   checkpoint—reservation, encryption admission, R3 staging, R5 publication,
   read, reuse, C2 Prepare, Seal, purge and legal hold—with exact file/line.
   Current census is frozen below. C6B records the two `MISSING` lifecycle rows
   solely as production-activation blockers; the C6B implementation dispatch
   must not implement either gate or invent a second authority engine.
7. `C6B-RETAINED-PROFILE-DERIVATION-01`: `CLOSED_BY_HOMEOWNER_RATIFICATION`.
   Section 2.1 freezes the controller pair, retention rule, purge authority and
   legal-hold precedence. Consent remains the implemented legal-basis
   machinery; do not invent another authority path. The implementation
   dispatch may extend the C6A server-side pattern to
   `EncryptedRawVaultRetained` without weakening or relabeling
   `EncryptedExportPacket`.
   Closure is based on authority, not merely column existence: (a) all six
   values—controller, retention class/start/end, purge policy and legal-hold
   policy—belong to the frozen canonical AuthoritySnapshot field set in spine
   §5.4; a new authority dimension requires a new schema version; (b) the same
   snapshot binds their approved external source through `AuthorityArtifactId`
   and `AuthorityArtifactVersion`; (c) the spine forbids the snapshot from
   inventing these values and required separate ratification; (d) section 2.1
   records that exact Homeowner ratification; (e)
   `AbsoluteSourceExpiresAtUtc` has its own time-bound derivation row, while the
   five non-time values correctly do not appear in that duration table; and
   (f) R1 reservation columns materialize the ratified snapshot but are not its
   authority source.
8. `C6B-BROKER-EXECUTION-BOUNDARY-01`: `ACTUAL_MISSING_IMPLEMENTATION` unless
   census finds a dedicated existing broker connection. Pin how the selected
   ingress adapter
   invokes broker-only SQL without granting the runtime role or testing as an
   owner/superuser.
9. `C6B-R2-FRESH-AUTHORITY-01`: `ANSWERED_BY_LANDED_CODE`. C6B adds no
   authority checkpoint. The fresh ingress check already executes inside
   broker-invoked R1 `complete` at P3; that same transaction creates the
   encryption attempt, so spine “encryption admission” is covered there.
   Post-R1 protection is the mandatory landed re-read at R3, R4 commit and R5
   publication, followed by R6 cleanup and C1/C6A checks for reuse/delivery.
   C6B retains only the endpoint non-bypass obligation C6B15.
10. `C6B-CAPTURE-ACCEPTANCE-POLICY-01`: `RATIFIED_BY_HOMEOWNER (2026-09-06)`.
    One server-owned, configuration-backed, versioned acceptance profile is
    active per deployment. CaptureAgent/request input cannot select or override
    its policy ID/version. Accepted NFC maps only to `ChipDg2Portrait`;
    accepted face comparison maps only to `LiveSelfieImage`; every other
    evidence kind is not applicable. The exact policy ID/version is frozen into
    each acceptance event. Missing, invalid, multiple or unsupported active
    profiles fail readiness closed. C6B adds no policy table or management API.
11. `C6B-BROKER-PROCESS-TRANSPORT-01`: `RATIFIED_BY_HOMEOWNER (2026-09-06)`.
    The isolated claim broker is a separate OS-independent ASP.NET Core API host
    consumed through a versioned private HTTP JSON contract. Bootstrap transport
    is ordinary private HTTP; HTTPS/mTLS is deferred transport hardening and
    does not block synthetic C6B implementation. The broker receives metadata
    only, never raw BIO, owns its distinct database LOGIN/capability, and the
    TagEkyc API receives neither the broker database credential nor broker-only
    SQL EXECUTE. `C6B-BROKER-MTLS-HARDENING-01` is a named forward item:
    HTTPS/mTLS authentication, certificate lifecycle and transport hardening
    are deferred, not waived.
12. `C6B-CAPTURE-PLAINTEXT-BUDGET-SOURCE-01`:
    `RATIFIED_BY_HOMEOWNER (2026-09-06)`. The authoritative actual plaintext
    budget is server-owned CaptureAgent enrollment/configuration metadata, not
    local environment configuration and not the server hard maximum. The Agent
    fetches a versioned, expiring configuration at first activation/enrollment,
    startup, reconnect and bounded periodic conditional refresh. Missing, stale,
    expired, malformed or out-of-range configuration disables retained capture
    without a default. Push is governed only by forward item 14.
13. `C6B-AGENT-CONFIG-PLANE-01`: `RATIFIED_BY_HOMEOWNER (2026-09-06)`.
    The initial server-owned Agent raw-export configuration plane uses the
    authenticated self-bound route
    `GET /api/ekyc/capture-agents/self/configuration`, startup/reconnect fetch,
    bounded periodic conditional polling and opaque HTTP ETag validation. Its
    closed document carries `ConfigurationRevision`, `EffectiveAtUtc`,
    `ExpiresAtUtc`, `RawExportEnabled`, `PlaintextBudgetSeconds` and
    `RawExportSourceClaimSafetyMarginMilliseconds`. ETag is only an HTTP cache
    validator and is not authority or business revision. Missing, ineffective,
    expired, stale, disabled, wrong-bound, wrong-revision, out-of-range or
    cross-inconsistent configuration fails retained capture closed; authorized
    `VerifyAndDiscard` behavior remains independent.
14. `C6B-AGENT-CONFIG-PUSH-HARDENING-01`:
    `RATIFIED_BY_HOMEOWNER (2026-09-06) — DEFERRED FORWARD ITEM`. Push may later
    accelerate invalidation/refresh, but it is not an authority source and is
    not required for current C6B implementation. C6B adds no push broker,
    WebSocket, SSE or notification consumer.
15. `C6B-IDENTITY-SOURCE-01`: `RATIFIED_BY_HOMEOWNER (2026-09-06)`.
    Canonical `ClientApplicationId` comes only from the successfully
    authenticated `AuthenticatedClientContext.ClientApplicationId`.
    Canonical `ProducerId` and `CaptureAgentInstanceId` come respectively from
    non-null `CaptureAgentId` and `DeviceId` on the one exact durable
    `capture_artifacts` row pinned by the exact acceptance event. The external
    ingress request supplies none of these three authority fields.
    `AllowedCaptureAgentIds` is authorization only: the resolved artifact
    `CaptureAgentId` must be a member; the server never selects first/random
    set members. Null `CaptureAgentId` or `DeviceId` fails at capture acceptance
    with existing `RAW_EXPORT_SOURCE_BINDING_INVALID`, produces no eligible raw
    acceptance and cannot reach claim/broker/R1.
16. `C6B-R2-TERMINAL-DISPOSITION-DURABILITY-01`:
    `RATIFIED_BY_HOMEOWNER (2026-09-06)`. Add nullable
    `R2TerminalOutcomeCode` to the existing encryption-attempt surface in one
    new forward migration. It is distinct from operational
    `R2TerminationDisposition`; it is null for active/retryable/re-entry/
    recoverable attempts and may contain only census-approved existing
    replay-stable terminal codes. The terminal transition and semantic outcome
    are one guarded atomic CAS; runtime/PUBLIC cannot update the column directly.

### 9.2 Frozen P4-P6 terminal-outcome census

This five-way census classifies all 36 frozen outcomes for use by the C6B R2
terminal field. `STRUCTURALLY_UNREACHABLE` here means unreachable as a value of
`R2TerminalOutcomeCode`; it does not redefine whether an earlier phase can
return the code.

| Classification | Exact frozen outcomes |
| --- | --- |
| `REPLAY_STABLE_TERMINAL` | `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` only for its P5 actual-over-limit branch; `CONTENT_COMMITMENT_MISMATCH`; `RECAPTURE_REQUIRED` |
| `RETRYABLE_OR_REENTRY` | `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`; `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_RESERVATION_BUSY`; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY`; `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS`; `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID`; `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED`; `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` |
| `RECOVERABLE_CONTINUATION` | `RAW_EXPORT_SOURCE_RESUME_PENDING` |
| `AVAILABLE_OR_PUBLISHED` | `RAW_EXPORT_SOURCE_AVAILABLE`; `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE` |
| `STRUCTURALLY_UNREACHABLE` as `R2TerminalOutcomeCode` | `ACCESS_DENIED`; `RAW_EXPORT_SOURCE_BINDING_INVALID`; `NOT_FOUND_OR_NOT_ALLOWED`; `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID`; `SOURCE_RETENTION_NOT_AUTHORIZED`; `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT`; `SOURCE_ENCRYPTION_FAILED`; `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`; `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID`; `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID`; `RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID`; `RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID`; `RAW_EXPORT_AUTHORITY_INVALID`; `RAW_EXPORT_SOURCE_SELECTION_NONE`; `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS`; `RAW_EXPORT_SOURCE_SELECTION_CONFLICT`; `RAW_EXPORT_SOURCE_UNAVAILABLE`; `RAW_EXPORT_SOURCE_INTEGRITY_INVALID`; `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED`; `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` |

The final semantic-field allowlist is therefore exactly three existing codes:

```text
RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED
CONTENT_COMMITMENT_MISMATCH
RECAPTURE_REQUIRED
```

`RECAPTURE_REQUIRED` belongs in this allowlist only when the server observes it
as the terminal semantic outcome of the exact current custody attempt. A
CaptureAgent may independently return the same external code locally when its
producer buffer is disposed, unavailable or expired before another server call;
that client-local result creates no database mutation and requires no
`R2TerminalOutcomeCode`. The attempt field is the durable oracle only for
server-observed replay-stable terminal outcomes, not for every occurrence of an
external code.

No 37th outcome is created. Publication and continuation states never populate
the terminal field. Available publication wins replay precedence over stale
terminal residue from an older attempt generation.

### 9.1 Mandatory current-authority checkpoint census

| Spine checkpoint | Status | Evidence / disposition |
| --- | --- | --- |
| reservation | `IMPLEMENTED` | Broker `complete` resolves current authority and consent before inserting R1: `20260731082733_Tip88C1B2CoreNewCandidate.cs:701-729,815-887`. |
| encryption admission | `IMPLEMENTED` — reading (i) | The term occurs once in the spine (`tip_88c1_planning_brief.md:1402`). In the landed state machine it means admission of the encryption attempt inside R1 `complete`: resolver at `20260731082733_Tip88C1B2CoreNewCandidate.cs:701-729`, attempt insert at `:887`. Neither R2 migration calls either resolver, confirming it is not a separate R2 checkpoint. |
| R3 staging | `IMPLEMENTED` | `20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs:383-386`. |
| R5 publication | `IMPLEMENTED` | R4 commit and R5 publish/Available transitions both re-read: `20260812120000_Tip88C1B2R4R6SourceFinalization.cs:367-369,490-492`; R6 is cleanup, not the publication gate. |
| read | `IMPLEMENTED` | Delivery eligibility invokes the current-authority/consent barrier: `20260904154848_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs:221-249`. |
| reuse | `IMPLEMENTED` | Job source binding re-reads consent/current authority: `20260815120000_Tip88C1C1ResolverAssembly.cs:479,518-519`. |
| C2 Prepare | `IMPLEMENTED` | Source-binding preparation requires the same re-read before binding: `20260815120000_Tip88C1C1ResolverAssembly.cs:479,518-526`. |
| Seal | `IMPLEMENTED` | Seal repeats consent/current-authority resolution: `20260815120000_Tip88C1C1ResolverAssembly.cs:729,734-735`. |
| purge | `MISSING — SEPARATE LIFECYCLE TIP / C6B PRODUCTION-ACTIVATION BLOCKER` | `SourceExpired` cleanup exists, but no lifecycle enforcement checks the ratified purge policy together with legal-hold state immediately before deletion. A separately reviewed and authorized lifecycle-enforcement TIP—not the C6B ingress implementation dispatch—must add that enforcement through existing policy machinery. Purge does not require live consent as permission to delete. C6B may be implemented and proven with synthetic data, but real retained-biometric ingress and production activation remain prohibited until this gate lands and passes review. |
| legal hold | `MISSING — SEPARATE LIFECYCLE TIP / C6B PRODUCTION-ACTIVATION BLOCKER` | `LegalHoldPolicyId` is stored, but no legal-hold checkpoint consumer/revalidation surface exists. The same separate lifecycle-enforcement TIP—not the C6B ingress implementation dispatch—must add the gate without treating hold as read/reuse authority. C6B may be implemented and proven with synthetic data, but real retained-biometric ingress and production activation remain prohibited until this gate lands and passes review. |

The split is scope ownership, not debt forgiveness. C6B owns the bounded
CaptureAgent ingress edge and its synthetic-data implementation proofs. The
separate lifecycle TIP owns purge and legal-hold enforcement. Deployment may be
prepared, but production activation and real retained-biometric ingress are a
hard STOP until both lifecycle gates are landed and independently reviewed
clean.

## 10. Expected implementation surface

### TagEkyc server

- one qualified RawExport ingress transport adapter and registration; an HTTP
  endpoint is included only if `C6B-TRANSPORT-FRAMING-01` qualifies it;
- public request/result contracts that project the ratified operation;
- one Application-level port/coordinator contract;
- Infrastructure composition adapting existing C6A/R1/R2-R6 services;
- exact broker-capability connection/execution composition;
- API-key scope catalog, provisioning and readiness updates;
- readiness and integration/architecture tests.

### TagEkyc.CaptureAgent

- one qualified transport component for deferred body submission; the current
  eager JSON `HttpClient` helper is not sufficient by itself;
- orchestration wiring plus bounded acquisition/result-port and device-adapter
  changes that place exact-byte capacity between SDK/device transient capture
  and construction/transfer of application-owned DG2/selfie buffers;
- production configuration/readiness reconciliation only where existing keys
  require it;
- unit/integration tests for admission ordering, retry and zeroization.

Exact paths and SHA-bound baselines must be frozen in the implementation
dispatch after the remaining technical reconciliation findings close. No D1
Homeowner sub-decision remains open.

## 11. STOP conditions

STOP and request a bounded correction if implementation requires a new table,
authority model, arbitrary role, additional capability/authority role,
privilege-model expansion, TTL, raw vault, encryption format, object provider,
multipart/resume protocol, package/delivery mutation, patient data, or cannot
prove the deployed transport prevents body progression before admission. The
single dedicated broker service LOGIN required by
`C6B-BROKER-PROCESS-TRANSPORT-01` is expressly authorized and is not a new
authority/capability role.
Independently, STOP production activation and all real retained-biometric
ingress until the separately dispatched purge and legal-hold enforcement TIP
has landed and received a clean independent review.

## 12. Next action

Perform bounded independent review of this v1.8 identity/terminal-durability
RRI correction against the TIP-88C1 planning spine and both repository
baselines. Resume implementation only after a clean verdict and exact renewed
authority. No implementation, stage,
commit, push, deployment or production activation is authorized by this brief.

## 13. PI-TAG-001 semantic matrices

### 13.1 Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract surface | Outcome/error | State/evidence/residue | Named proof | Negative/mutation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| I01 | exact one-call external operation | API ingress | authentication before metadata; no body before admission | frozen D4.2 input + `CaptureAgentFinalResult` | exact P0-P7 table | no extra upload-session state | C6B01 | add init/part/complete |
| I02 | retained-mode authority | Homeowner-ratified D1 + consent machinery + server broker | D1 closed; fresh check before R1 | subject-consent event/classes/authorities + authority record | `SOURCE_RETENTION_NOT_AUTHORIZED` | rejection evidence; zero R1/body | C6B11 | agent asserts authority or add second consent engine |
| I03 | producer retention claims | CaptureAgent | monotonic producer observation before metadata | exact three D4.2 retention fields | plaintext-retention-invalid on omission/mismatch | zero R1/body | C6B06/C6B11 | omit expiry/budget or add server fallback |
| I04 | two atomic ceremonies | broker DB boundary | authority+begin, then complete+R1 | existing C6A/broker functions | exact internal union | bounded alias/shell residue only | C6B04/C6B09 | collapse transaction or ordinary runtime role |
| I05 | admission-before-body | qualified transport | committed R1 before first application body read/R2; physical early buffering is bounded memory-only | external operation framing | protocol-invalid/temporary-unavailable by phase | exact P4/P5 cleanup and early residue discard | C6B02/C6B07/C6B16 | timer fall-through, auto-continue, early app read, unbounded buffer or spill |
| I06 | bounded raw stream | CaptureAgent + R2 | transient capture; determine exact length/class; atomic local capacity before retained ownership/external call; server capacity before begin | `SensitiveByteBuffer` + caller-owned `Stream` | size/capacity exact codes | reservation failure immediately zeroes/disposes; counters released | C6B06/C6B08/C6B10 | reserve before exact size, retain/call before reservation, or accept limit+1 |
| I07 | closed class set | agent + ingress | classify before source read | raw-class enum/manifest | unsupported/capability failure | zero R1/body | C6B05 | admit liveness |
| I08 | R2-R6 reuse | infrastructure | admitted body only | landed orchestrators/repositories | existing dispositions | existing fenced cleanup | C6B07/C6B09/C6B12 | duplicate codec/state machine |
| I09 | closed public result | API mapping | internal result fully mapped after terminal processing | frozen `CaptureAgentFinalResult` | exact exhaustive code set | no internal ids/locator/raw digest | C6B01/C6B04 | expose `InternalClaimResult` |
| I10 | scope isolation | API-key policy | authenticate then exact `capture.*` scope | existing caller-category/catalog paths | access denied precedence | zero disclosure/state | C6B01/C6B11 | reuse append/recipient scope |
| I11 | broker ACL path | infrastructure composition | selected ingress-adapter runtime never gains broker SQL EXECUTE | dedicated broker connection boundary | capability unavailable/fail closed | no partial authority/R1 | C6B14 | execute through ordinary runtime DbContext or grant PUBLIC/runtime |
| I12 | real transport integration proof | two-repo harness | non-patient fixture; production-equivalent deployed transport posture | CaptureAgent client -> qualified ingress -> R1-R6 | exact final union | exact provenance, no residue | C6B12 | bypass real adapter/client |
| I13 | bounded agent retry | CaptureAgent retry projector | exact result class; monotonic pre-wait lifetime; buffer still available | `BusyRetryBackoff`, `RetryNotBeforeUtc`, continuation projection | retry, busy or `RECAPTURE_REQUIRED` exactly | same UUID only where permitted; no body after disposal | C6B13 | immediate/wall-clock retry or retry terminal mismatch |
| I14 | landed authority-gate non-bypass | endpoint composition | R1 authority passes before attempt admission; R2 remains provisional; R3 and R4 commit pass; R5 revalidates before `Available`; R6 cleans obsolete residue; C1/C6A pass before later use | existing consent/current-authority resolvers at each mandatory gate | existing phase-specific landed outcome | bounded provisional ciphertext may exist after withdrawal but never becomes Available/deliverable; standard R6 cleanup retains deletion evidence | C6B15 | endpoint/recovery/replay shortcut bypasses the next gate |

### 13.2 Outcome and Precedence Matrix

The authoritative exhaustive P0-P7 outcome/residue/retry table remains TIP-88C1
planning spine §10.0 and is incorporated without alteration. C6B adds no code.
The delta precedence is:

| Use case | Higher-to-lower precedence | External result | Durable/no-residue rule |
| --- | --- | --- | --- |
| unauthenticated/missing scope | authentication -> exact ingress scope -> existence/metadata | access-denied family | zero alias/R1/body; no existence disclosure |
| metadata rejection | binding/class/declared-size/producer-retention -> capacity -> claim | exact spine code | zero body; residue only where the P0-P3 table permits |
| body path | committed R1 -> AdmissionAccepted -> body -> R2 provisional -> mandatory R3 -> R4 commit -> mandatory R5 re-read/`Available` -> R6 cleanup | exact existing P4-P7 result | withdrawal is refused at the next landed gate; provisional ciphertext is cleaned with deletion evidence and never published |
| retry | terminal/exact replay -> busy/in-progress -> same-owner re-entry | frozen union | no duplicate snapshot/claim/R1/object |
| agent wait/retry | classify outcome -> monotonic `ClientPreWaitContinuation` -> buffer availability -> wait or recapture | exact retry/busy/`RECAPTURE_REQUIRED` | never wait/retry beyond producer lifetime or after disposal |

### 13.3 Shape and Nullability Matrix

| Shape | Required | Forbidden | Consumer |
| --- | --- | --- | --- |
| external metadata | exact wire observations only: session/artifact/revision/class, idempotency, class/length/media/digest/capture and three producer-retention fields | caller-authored `ClientApplicationId`, `ProducerId`, `CaptureAgentInstanceId`; Policy/Decision/Permit/Job/Snapshot, keyed commitment, server deadlines | ingress coordinator |
| canonical ingress identity | authenticated `ClientApplicationId` plus non-null `CaptureAgentId`/`DeviceId` from the same exact accepted artifact | request/local-config identity, arbitrary member selection from `AllowedCaptureAgentIds`, null/fallback identity | acceptance coordinator -> claim/broker/R1 |
| `InternalClaimResult` | exact spine variant fields | serialization/API egress | ingress coordinator only |
| `CaptureAgentFinalResult.Available` / `.AlreadyAvailable` | `OutcomeCode`, `SourceArtifactId`, `CurrentSourceState`, `CurrentDisposition` | `RetryNotBeforeUtc` and internal fields | CaptureAgent client |
| `CaptureAgentFinalResult.ClaimEvaluationInProgress` | exact `OutcomeCode`; `RetryNotBeforeUtc = CurrentTokenExpiresAtUtc` | `SourceArtifactId`, `CurrentSourceState`, `CurrentDisposition`, internal fields | CaptureAgent client |
| `CaptureAgentFinalResult.OutcomeOnly` | `OutcomeCode` only | `SourceArtifactId`, `CurrentSourceState`, `CurrentDisposition`, `RetryNotBeforeUtc`, internal fields | CaptureAgent client |
| admitted body | one bounded non-seekable stream | eager/pre-admission bytes, multipart/resume | existing R2 orchestrator |
| existing encryption attempt | operational `R2TerminationDisposition`; nullable semantic `R2TerminalOutcomeCode`, restricted to the three census-approved replay-stable terminal codes | generic/free-text outcome, retry/re-entry/resume/available code, terminal-with-null or active-with-non-null state | guarded terminal CAS and replay projector |

### 13.4 Ordering Graph

```text
CaptureAgent transient capture -> exact length/class
-> atomic local capacity reservation -> retain buffer (or zero/dispose and stop)
-> authenticate + exact scope -> authenticated ClientApplicationId
-> resolve exact accepted artifact -> non-null CaptureAgentId/DeviceId
-> enforce AllowedCaptureAgentIds membership -> metadata validation
-> retained-mode authority + begin [atomic boundary 1]
-> broker complete + R1 [atomic boundary 2]
-> commit R1 -> application begins body read; normal HTTP 100 Continue may follow
-> R2 provisional encrypted object; any replay-stable terminal CAS atomically freezes exact R2TerminalOutcomeCode
-> R3 mandatory re-read
-> R4 commit re-read -> R5 mandatory publication re-read -> Available
-> R6 obsolete-residue cleanup -> C1/C6A where applicable
-> only then reuse / delivery eligibility
-> final result -> zero/dispose -> local capacity release
```

Metadata-only/replay/failure branches exit before admission/body. Worker cleanup
and R3-R6 continuation enter only through their landed durable contexts; they
cannot re-run ingress authorization or request another body.

Retry branches return to metadata only after the CaptureAgent projector proves
the exact buffer remains available and the monotonic continuation budget fits;
otherwise they terminate in `RECAPTURE_REQUIRED`.

### 13.5 Test-Bite Matrix

| Claim | Positive | Negative | Broken mechanism | Expected RED |
| --- | --- | --- | --- | --- |
| admission ordering | R1 committed then first application read; bounded physical memory-only residue is allowed | application read before R1, timer fall-through, auto-continue, bound breach or spill | broken client/intermediary discipline | C6B02/C6B16 |
| metadata-only zero read | AlreadyAvailable/conflict returns | body reader opens | unconditional body binding | C6B03 |
| producer retention ownership | exact three claims accepted | missing/mutated equality | server default/fallback | C6B11 |
| local capacity | transient capture obtains exact size, then N buffers reserve and retain | retained ownership or external call before reservation; failed reservation not zeroed | premature/late admission | C6B10 |
| broker least privilege | dedicated broker path succeeds as broker role | runtime/PUBLIC executes broker SQL | owner/superuser false-green or over-grant | C6B14 |
| closed shapes | exact union round-trips | commitment/internal field added | open DTO bag | C6B01 |
| cleanup discrimination | abort retryable, clean mismatch terminal | outcomes collapsed | generic exception mapping | C6B07 |
| no residue | successful/failing runs scan clean | buffering/spill enabled | middleware/temp-file path | C6B08 |
| retry classification | every busy/evaluation/termination/buffer/mismatch class maps to its exact wait, re-entry, recapture or terminal result | swap busy/evaluation clocks; retry pending termination, lost buffer, invalid retention or clean mismatch | generic retry policy | C6B13 |
| authority-gate non-bypass | endpoint traverses R1 -> R2 provisional -> R3 -> R4 commit -> R5 publication/Available -> R6 cleanup -> C1/C6A as applicable before consumer-visible eligibility | withdraw between R1/R3, R3/R4, R4/R5, R5/C1 and C1/C6A; the next mandatory gate refuses progression and cleanup retains deletion evidence | endpoint/recovery/replay shortcut bypasses a landed gate | C6B15 |

## 14. PI-TAG-001 review ledger

| Round | Version/SHA | HIGH | MEDIUM | LOW | Disposition |
| --- | --- | ---: | ---: | ---: | --- |
| 1 | v0.2 / `03A97BF4FE6E83B1ADE2C3DB836F40DF9AF44DE222000DBA866F1996FFA70573` | 4 | 4 | 0 | HOLD; eight accepted findings applied to v0.3 |
| 2 | v0.3 / `ED5FEF29B206FB5202872AD9B6713B2B6FCF965B1AA3FDDAE2CBD5D9C0240F4D` | 3 | 1 | 0 | HOLD; four accepted findings applied to v0.4 |
| 3 | v0.4 / `72A7FAA3CE476BBF9FA02A41AE4432E6491200E021709A0F737C6CD5E8B40C6D` | 2 | 4 | 1 | HOLD; seven accepted findings applied to v0.5 |
| 4 | v0.5 / `4E0FC31618854B5FB767CBBC2FC961B3402D07C1A368D56B8C632BD11BDEC552` | 2 | 0 | 0 | HOLD; source-phase contract and agent ownership-boundary findings applied to v0.6 |
| 5 | v0.6 / `171E27CC44DBBE27FB3D99A8CBA4FE97F6F67407363FC194AA7AEB7AE984F13A` | 1 | 1 | 0 | HOLD at required checkpoint; two propagation findings applied to v0.7 |
| 6 | v0.7 / `C8E542FFE4D12BD7E3DB81A1FBD80E08B5527C8A69DBB40DE69C366A866406A6` | 0 | 1 | 0 | HOLD; final transport-vocabulary propagation finding applied to v0.8 |
| 7 | v0.8 / `ED161EE301BA550C0374FC7E42E0256726674EC21C6BDA0412B8E345F8E9B8E7` | 0 | 1 | 0 | HOLD; one remaining deployed-topology wording finding applied to v0.9 |
| 8 | v1.4 / `5B81A0F96347A6B09244A9AEF19034CAE34B99DE50F0D6BCF09DFDFC64E52DF8` | 0 | 1 | 0 | HOLD; three Homeowner prerequisite ratifications were present only in dispatch and are recorded authoritatively in v1.5 |
| 9 | v1.5 / `E25F33CC0ED7513D2D0531E5A5A540979CD8756658F96142C3887B4F28CD9BC1` | 1 | 1 | 0 | HOLD; HTTP qualification over-tightening and the missing concrete Agent configuration plane are ratified/corrected in v1.6 |
| 10 | v1.6 / `E1F1640F7E3D9150DC35DDED7629253DFD9CC81F6FA39D73475125CB6FF73D8A` | 2 | 0 | 0 | HOLD/RRI; caller identity-source ambiguity and missing durable terminal semantic discriminator corrected in v1.7 |

Round-5 root-cause checkpoint: both findings were patch-local incomplete
propagation across required matrices/proofs. The source-contract correction did
not keep the Ordering Graph unresolved, and the transport conditionalization
did not reach C6B01/C6B12. This is bounded drafter drift, not a new architecture
decision or unsupported review expansion. A consistency scan and independent
Round-6 review were required. This paragraph records the historical Round-5
checkpoint only; the later Homeowner D1 reconciliation in v1.0 supersedes the
former source-contract-blocker classification with the technical disposition
in sections 6, 9 and 13.

Round-1 causes: incomplete earlier corpus trace, missing PI matrices, and
patch-local reversal of producer/server retention ownership. No finding was
deferred or rejected as unsupported.

## 15. v0.1 independent review disposition

Three bounded reviewers independently traced the corpus, server and
CaptureAgent. v0.1 was held for two recurring root findings and related
precision corrections:

- agent-owned source facts were incorrectly mixed with server-derived
  policy/authority and later Permit/job identity;
- the retained-source D1 mode was incorrectly treated as directly compatible
  with C6A's current `EncryptedExportPacket`-only composition function;
- ingress used a generic authority failure instead of the exact
  `SOURCE_RETENTION_NOT_AUTHORIZED` disposition;
- frozen final-result and readiness/capacity manifests were presented as open
  design work;
- the two atomic ceremonies were described too loosely;
- CaptureAgent `VerifyAndDiscard` and the non-trivial transport qualification
  requirement needed explicit preservation.

v0.2 applies those corrections without opening a Homeowner semantic decision.
