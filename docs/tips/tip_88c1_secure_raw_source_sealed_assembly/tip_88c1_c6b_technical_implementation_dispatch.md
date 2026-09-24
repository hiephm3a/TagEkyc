# TIP-88C1-C6B — Technical Implementation Dispatch

**Status:** CANDIDATE v1.6 — BOUNDED RRI PROPAGATION CORRECTION APPLIED — REVIEW REQUIRED  
**Date:** 2026-09-05  
**Risk tier:** High-risk  
**Planning basis:** C6B Planning Brief v1.8  
**Planning SHA-256:** `D7487BCC040101412A45B0AC58CCB3BE126298913A761E3030F227EA63736425`  
**Server baseline:** `c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9`  
**CaptureAgent baseline:** `b193f6316e13a82fb2f9f985f68500feae194cfa`  
**Implementation authority:** **NOT GRANTED**

## 1. Objective and exact boundary

C6B adds only the missing producer-to-custody integration edge:

```text
CaptureAgent-owned DG2/selfie buffer
  -> authenticated metadata admission
  -> committed R1 reservation/re-entry
  -> one bounded plaintext body
  -> existing R2 -> R3 -> R4 -> R5 Available -> R6 cleanup
  -> frozen CaptureAgent final result
```

It must reuse the landed policy, consent, AuthoritySnapshot, claim broker,
idempotency, key custody, streaming AEAD, provisional object, finalization,
publication and cleanup machinery. It must not build another upload service,
authority engine, encryption format, object store, worker topology, package or
delivery path.

Exactly two raw classes are allowed:

- `ChipDg2Portrait`;
- `LiveSelfieImage`.

Liveness video, diagnostic frames, SDK dumps and every unlisted class are
rejected before a body read or retained-buffer ownership transfer.

## 2. Closed decisions and activation boundary

The following are inputs, not Builder decisions:

- hospital is controller; TagEkyc is processor/custodian;
- mode is `EncryptedRawVaultRetained`;
- local CaptureAgent mode remains `VerifyAndDiscard`;
- controller custody retention begins at accepted server custody and ends at the earliest
  applicable consent expiry, purpose completion or subject withdrawal, using
  the shortest applicable class;
- purpose-specific consent uses the landed subject-consent event machinery;
- caller never chooses Policy, Grant, Permit, AuthoritySnapshot, retention
  policy or server deadline;
- authority loss is refused at the next existing checkpoint;
- purge-policy and legal-hold semantics are ratified, but their enforcement is
  owned by a separate lifecycle TIP.

Planning Brief v1.8 is the authoritative record that the Homeowner ratified
these C6B prerequisites on 2026-09-06; this dispatch consumes rather than
self-authorizes them:

- `C6B-CAPTURE-ACCEPTANCE-POLICY-01 = RATIFIED`: one server-owned,
  configuration-backed, versioned acceptance profile is active per deployment;
  CaptureAgent/request input cannot select or override its policy ID/version;
  accepted NFC maps only to `ChipDg2Portrait`, accepted face comparison maps
  only to `LiveSelfieImage`, and every other evidence kind is not applicable;
  the exact policy ID/version is frozen into each acceptance event; missing,
  invalid, multiple or unsupported active profiles fail readiness closed. C6B
  adds no policy table or management API.
- `C6B-BROKER-PROCESS-TRANSPORT-01 = RATIFIED`: the isolated claim broker is a
  separate OS-independent ASP.NET Core API host consumed through a versioned
  private HTTP JSON contract. Bootstrap transport is ordinary private HTTP;
  HTTPS/mTLS is deferred transport hardening and does not block synthetic C6B
  implementation. The broker receives metadata only, never raw BIO, owns its
  distinct database LOGIN/capability, and the TagEkyc API receives neither the
  broker database credential nor broker-only SQL EXECUTE.
  `C6B-BROKER-MTLS-HARDENING-01` remains a named forward item: HTTPS/mTLS is
  deferred, not waived.
- `C6B-CAPTURE-PLAINTEXT-BUDGET-SOURCE-01 = RATIFIED`: the authoritative
  actual plaintext budget is server-owned CaptureAgent enrollment/configuration
  metadata, not local environment configuration and not the server hard
  maximum. Missing, stale, expired, malformed or out-of-range configuration
  disables retained capture without a default.
- `C6B-AGENT-CONFIG-PLANE-01 = RATIFIED`: the initial authoritative plane is
  authenticated self-bound GET plus startup, reconnect and bounded periodic
  ETag polling. `ConfigurationRevision` remains business authority while ETag
  is only an opaque HTTP cache validator.
- `C6B-AGENT-CONFIG-PUSH-HARDENING-01 = DEFERRED FORWARD ITEM`: push is neither
  an authority source nor a C6B implementation prerequisite.

C6B may be implemented and tested only with synthetic/non-patient fixtures.
Production activation and all real retained-biometric ingress remain hard
stopped until the separately dispatched purge and legal-hold enforcement TIP
has landed and passed independent review.

## 3. Reuse census

| Concern | Landed owner | C6B action |
| --- | --- | --- |
| policy/class/consent | TIP-88A/88B | call unchanged |
| AuthoritySnapshot derivation pattern | C6A | extend for retained mode without caller authority fields |
| alias/exact lock and claim replay | C1 B1/B2 | preserve exactly |
| R1 complete fresh-authority check | C1 B2 | traverse; do not duplicate at R2 |
| content commitment/subject token | claim-comparison broker | reuse |
| KEK/DEK and framed AEAD | R2 | reuse |
| provisional object custody | R2 | reuse |
| re-read, staging and verification | R3 | reuse |
| commit/publication/cleanup | R4/R5/R6 | reuse |
| C1/C6A later-use checks | C1/C6A | preserve |
| external raw ingress route | absent | add one route |
| CaptureAgent raw ingress client | absent | add one method |
| local retained-buffer capacity ownership | absent | add bounded owner/token |
| purge/legal-hold enforcement | absent | separate TIP; do not implement here |

## 4. One-call transport qualification and protocol

### 4.1 Qualification gate

No product route, schema or application mutation may begin until an isolated
transport qualification harness proves the selected one-call HTTP transport can
implement the ratified P4/P5 boundary. HTTP with `Expect: 100-continue` remains
the selected first candidate; qualification proves properties, not control of a
framework-private interim-response API. C6B02 and C6B12 must prove the real
deployed server and proxy topology has all of these properties:

1. CaptureAgent sends headers before the body; the HTTP handler has no internal
   timeout fall-through that transmits the body, so it must set
   `SocketsHttpHandler.Expect100ContinueTimeout = Timeout.InfiniteTimeSpan`;
2. no delegating handler retries or clones the body;
3. any unavoidable pre-admission network/kernel/TLS/proxy memory buffering is
   bounded by `RawExportIngressMaximumPreAdmissionBufferedBytes`, remains
   memory-only, never spills to disk/temp storage, and is discarded on rejection;
4. authentication, exact scope, metadata validation, server capacity,
   authority+begin, broker complete and committed R1 all finish before any
   body reader or provider writer is constructed or armed;
5. only after committed R1 may application code begin reading `Request.Body`;
   Kestrel may then emit its normal HTTP `100 Continue` as body reading begins;
6. a metadata rejection performs zero application body reads, creates no
   R2/key/object activity, and discards bounded early network-buffer residue.

The qualification distinguishes physical network/kernel/TLS buffering from
application ownership or consumption of plaintext. The first is bounded and
may be unavoidable. The second is forbidden before committed R1: no application
code reads or copies body content, no R2/provider/body writer is armed, and no
application-owned plaintext buffer contains request body bytes.

No explicit application-controlled Kestrel interim-response API is required.
HTTP is not disqualified merely because Kestrel emits `100 Continue` when the
application begins its post-R1 read. An intermediary that auto-continues, fully
buffers, spills, or otherwise defeats the bounded posture fails qualification;
so does any client timer fall-through. Do not use reflection/internal Kestrel,
invent a custom interim API, switch to gRPC for this reason, add a second public
endpoint, or add multipart/init/part/complete/resumable semantics.

### 4.2 Route, authentication and scope

The following route and headers are conditional on the HTTP candidate passing
§4.1. If it fails, a replacement dispatch correction must freeze an equivalent
single-operation duplex mapping before implementation:

```http
POST /api/ekyc/raw-export/source-ingress
Expect: 100-continue
Content-Length: <exact declared plaintext length>
Content-Type: <class-compatible media type>
```

The route uses the existing API-key authenticator. It requires caller category
`CaptureAgent` and the new exact scope:

```text
capture.raw-export.source.ingress
```

The scope is added to the existing CaptureAgent scope catalogs and production
provisioning policy only. Recipient, business, trusted-adapter, operator,
runtime, broker and management credentials must fail before resource lookup.
No broker, authority+begin, complete or R2-handoff SQL EXECUTE privilege is
granted to the API runtime role. The two acceptance wrappers in §5.0 are the
only C6B-added runtime EXECUTE grants.

`GET /api/ekyc/capture-agents/self/configuration` is a separate ordinary
authenticated JSON GET. It is not the one-call raw-ingress route and is not
subject to the P4/P5 admission boundary, `Expect: 100-continue`, or the
pre-admission buffering qualification harness. It uses the same CaptureAgent
authentication/category and `capture.raw-export.source.ingress` scope discipline,
accepts no Agent ID selector, appears in the mutation allowlist, and must be
covered by readiness.

### 4.3 Metadata envelope

The request supplies the closed observation manifest below. The server then
constructs canonical internal `RawExportSourceIngressMetadata` by adding three
server-derived identity fields. The wire manifest is exact and may not grow:

| Wire header / content header | DTO field | Rule |
| --- | --- | --- |
| `X-TagEkyc-Agent-Configuration-Revision` | AgentConfigurationRevision | positive server-issued enrollment/configuration revision; exact authenticated-Agent binding |
| `X-TagEkyc-Verification-Session-Id` | VerificationSessionId | required UUID |
| `X-TagEkyc-Capture-Artifact-Id` | CaptureArtifactId | required UUID |
| `X-TagEkyc-Capture-Revision` | CaptureRevision | positive integer |
| `X-TagEkyc-Raw-Class` | RawClass | exact two-value allowlist |
| `Idempotency-Key` | IngressIdempotencyKey | nonzero RFC-4122 variant/version-4 UUID in exactly 32 lowercase hexadecimal `Guid N` form; stable across permitted retry |
| `Content-Type` | MediaType | exact class-compatible value |
| `Content-Length` | ClaimedPlaintextLength | positive Int64 and exact body length |
| `X-TagEkyc-Plaintext-Sha256` | ClaimedPlaintextDigest | exactly 32 raw SHA-256 bytes encoded lower hex |
| `X-TagEkyc-Captured-At-Utc` | CapturedAtUtc | RFC 3339 UTC instant |
| `X-TagEkyc-Retention-Started-At-Utc` | PlaintextRetentionStartedAtUtc | RFC 3339 UTC producer observation |
| `X-TagEkyc-Retention-Expires-At-Utc` | PlaintextRetentionExpiresAtUtc | RFC 3339 UTC producer horizon |
| `X-TagEkyc-Retention-Budget-Seconds` | PlaintextRetentionBudgetSeconds | positive base-10 integer |

Reject all-zero, non-v4, non-RFC-4122, uppercase, braced or hyphenated
idempotency values before capacity/broker work. Reject duplicate headers,
folded/ambiguous values, chunked requests, absent or
mismatched Content-Length, unsupported content encoding and trailers. Digest,
raw bytes and identity metadata must not be logged. The three removed identity
headers are forbidden; a compatibility observation is not retained.

Canonical identity is constructed only after exact acceptance/artifact
resolution:

```text
ClientApplicationId    := AuthenticatedClientContext.ClientApplicationId
ProducerId             := exact capture_artifact.CaptureAgentId
CaptureAgentInstanceId := same capture_artifact.DeviceId
```

The artifact `CaptureAgentId` must be a member of the authenticated
`AllowedCaptureAgentIds`; that set is authorization only and never an identity
selector. Do not select first/random/single set members. Null `CaptureAgentId`
or null `DeviceId`, cross-client, cross-agent or cross-device binding returns
existing `RAW_EXPORT_SOURCE_BINDING_INVALID` before claim/broker/R1 and
produces no retained-ingress-eligible acceptance.

The DTO must not contain acceptance/authority PolicyId/version, Grant, Permit, job,
AuthoritySnapshotId, controller policy, retention class, absolute source
expiry, reservation deadline, keyed content commitment, KEK/DEK material or
storage locator. Those values remain server-derived.

### 4.4 Body and final response

Only a committed `NewReservation` or expressly valid same-owner R1 re-entry
may open the request body. The body is a single non-seekable stream bounded by
the exact declared length and class limit. The coordinator must fail if the
stream ends early, yields one byte beyond the declaration, or its computed
SHA-256 differs from `ClaimedPlaintextDigest`.

The server never materializes the full body, enables request buffering, writes
plaintext to disk, logs content, or retains plaintext after the active R2
window. It streams into the landed R2 orchestrator and disposes/zeroes its
bounded buffers on every exit.

The final JSON response is the frozen exhaustive `CaptureAgentFinalResult`:

| Variant | Required fields | Forbidden fields |
| --- | --- | --- |
| Available | OutcomeCode, SourceArtifactId, CurrentSourceState, CurrentDisposition | RetryNotBeforeUtc, internals |
| AlreadyAvailable | OutcomeCode, SourceArtifactId, CurrentSourceState, CurrentDisposition | RetryNotBeforeUtc, internals |
| ClaimEvaluationInProgress | OutcomeCode, RetryNotBeforeUtc equal to current token expiry | source/state/disposition, internals |
| OutcomeOnly | OutcomeCode only | source/state/disposition/retry, internals |

`InternalClaimResult`, SQL identifiers, raw digest, object locator, claim token,
AuthoritySnapshot, key references and exception details never cross the API.

### 4.5 In-flight wait deadline

`SocketsHttpHandler.Expect100ContinueTimeout = Timeout.InfiniteTimeSpan`
prevents the HTTP stack's
one-second automatic body-send fallback; it does not authorize an unbounded
operation. Each call also receives a linked cancellation deadline derived from
the monotonic minimum of remaining producer-buffer lifetime and registered
continuation budget, less the configured safety margin. A peer that sends
neither admission nor a final response is aborted before that deadline, with
zero application body reads, bounded early network residue discarded,
agent/server capacity release and normal buffer zeroization.

## 5. Server composition and transaction order

### 5.0 Capture-acceptance provenance prerequisite

The landed acceptance tables/functions are not currently called by production
Application/API code. C6B must close that wiring rather than treating a plain
`capture_artifacts` row as authority or asking CaptureAgent to invent a
revision.

Artifact append alone is not acceptance. The server-owned acceptance
coordinator runs only after the corresponding existing evidence-result append
has durably accepted qualifying verification evidence. It uses the
authenticated session/client, exact prior artifact ID, that durable evidence
reference and the current server acceptance policy ID/version through one new
idempotent event-append wrapper:

```text
tagekyc.raw_export_accept_capture_for_ingress
  -> under the landed session/class acceptance advisory lock:
     exact existing artifact/evidence acceptance -> return it unchanged
     no acceptance -> derive MAX(CaptureRevision)+1
                   -> raw_export_append_capture_acceptance
                   -> return CaptureAcceptanceId + CaptureRevision
```

The qualifying mapping is closed: accepted NFC evidence appends
`ChipDg2Portrait`; accepted face-comparison evidence appends
`LiveSelfieImage`; liveness and every other evidence kind create no raw
acceptance. `AcceptedEvidenceRef` is exactly the durable accepted
`EvidenceResultId`, never free text supplied by CaptureAgent.

`C6B-CAPTURE-ACCEPTANCE-POLICY-01 = RATIFIED`. Its production owner is the
single active server-owned configuration profile described in section 2. The
profile explicitly provisions `AcceptancePolicyId` and
`AcceptancePolicyVersion`; neither columns nor SQL parameters act as the
source. No latest-version inference, fixture fallback or caller override is
permitted. Readiness validates the exact configured profile and mapping before
retained mode can be active.

After that profile is ratified, the wrapper and landed append function run in
one transaction; PostgreSQL advisory
transaction locks are re-entrant for the same session. A different artifact
for the same artifact/evidence identity is a typed conflict, not a new revision
or replacement. The evidence-result response gains a state-shaped optional
`RawCaptureAcceptance` object. It is required with exactly
`CaptureArtifactId`, `CaptureAcceptanceId`, `CaptureRevision` and `RawClass`
for the two qualifying accepted evidence kinds, and absent for liveness,
rejection and every non-raw kind. It never returns policy/evidence internals.

Raw ingress supplies artifact/revision observations, then the server resolves
that exact acceptance event and requires equality of client, session, class,
artifact and revision before claim work. CaptureAgent never supplies
`CaptureAcceptanceId`; the server cannot select first/latest/arbitrary
acceptance for ingress. Evidence-response loss replays to the same event.

The same resolver must join the acceptance to its exact durable
`capture_artifacts` row and require non-null `CaptureAgentId` and `DeviceId`
before the acceptance becomes retained-ingress eligible. Landed evidence is:

- authenticated client and authorization set:
  `src/TagEkyc.Application/AuthenticatedClientContext.cs:11-18`;
- durable artifact columns:
  `src/TagEkyc.Infrastructure/Persistence/Migrations/20260621075836_InitialPostgresPersistence.cs`
  (`CaptureAgentId` and `DeviceId`, both nullable in the historical schema).

The canonical claim receives client ID from authentication, producer ID from
that artifact's `CaptureAgentId`, and instance ID from the same artifact's
`DeviceId`. The resolver requires the artifact producer to belong to
`AllowedCaptureAgentIds`. Missing/null/mismatched identity uses existing
`RAW_EXPORT_SOURCE_BINDING_INVALID`; it is not a new acceptance/outcome code.

The authoritative session-completion transaction—not evidence append or raw
ingress—calls a second server wrapper:

```text
tagekyc.raw_export_select_capture_acceptances_on_session_completion
```

The hospital/BusinessConsumer `session.complete` workflow—not CaptureAgent—owns
this call. Applicability is server-derived from the same ratified, provisioned
acceptance/retained-source policy profile required by
`C6B-CAPTURE-ACCEPTANCE-POLICY-01`; no request flag or CaptureAgent field may
enable or disable it. Sessions for which that profile does not require retained
raw custody follow the pre-C6B completion path byte-for-byte and never call the
wrapper. A session to which the retained profile applies must pass the wrapper;
missing applicability facts fail closed before completion rather than defaulting
to legacy behavior.

The wrapper derives each acceptance from the durable Available source and its
exact ingress binding for the authenticated
session/class; acceptance IDs are not accepted from CaptureAgent, chosen as
latest or retained only in process memory. It requires exactly one matching
event per class and exact artifact/revision equality with the durable ingress.

In one transaction it computes the two landed session/class advisory keys,
sorts them by numeric value and acquires both with advisory transaction locks.
Only then it reads both selections:

- both exact selections already exist: replay success, return them unchanged;
- neither exists and both exact acceptance events resolve: invoke the landed
  selection function twice and return both;
- one-sided, different, missing, multiple or ambiguous state: typed completion
  conflict; insert neither.

The transaction makes the pair atomic. Response loss and concurrent completion
return the same pair rather than leaking a uniqueness exception. Later B4
resolution must match the exact artifact/revision used by ingress.

Both wrappers are `SECURITY DEFINER`, owned by
`tagekyc_raw_export_deployer`, use `SET search_path=pg_catalog`, are revoked
from PUBLIC and every non-API role, and grant EXECUTE exactly to
`tagekyc_runtime`. They accept no caller policy/evidence override; those facts
come only from the server coordinators. If this cannot be implemented without
a schema/table or new authority model, STOP/RRI.

The same migration must revoke direct EXECUTE on
`raw_export_append_capture_acceptance` and
`raw_export_select_session_capture_acceptance` from `tagekyc_runtime`; otherwise
runtime could bypass evidence/policy validation. Only the deployer-owned
wrappers invoke them. Down restores their exact pre-C6B ACL. C6B26 proves
runtime direct calls fail and only qualifying evidence/completion through the wrappers
succeeds.

The bounded assertion sweep found two operative expectations that must move
together with that revoke: `RawExportControlPlaneReadinessValidator.cs:167-178`
sets `RuntimeExecute=true` for both functions, and
`Tip88C1AAcceptanceSurfaceTests.cs:161-166` reads/asserts the same runtime
privilege. The validator change is narrowly allowlisted in §9.1 and the test is
inside the existing test allowlist. The C1A architecture test only enumerates
identifiers; the other located integration calls use these functions as fixture
setup and do not independently require runtime EXECUTE. The original C1A
migration remains historical pre-C6B ACL evidence and is not rewritten.

### 5.1 Application coordinator

Add one Application port and coordinator for the closed metadata/body/result
contract. The API endpoint owns authentication and HTTP projection; the
Application coordinator owns order; Infrastructure owns PostgreSQL, crypto,
object and transport adapters.

Required order:

```text
authenticate + require CaptureAgent/exact scope
-> parse and validate metadata without touching Request.Body
-> resolve exact server-authored CaptureAcceptanceId/artifact/revision event
-> reserve server stream-count and exact-byte capacity
-> submit authority+begin to isolated claim-comparison broker
-> broker completes R1 and returns the frozen R2 handoff
-> commit R1 while reader/writer remain unarmed
-> only then construct/arm Request.Body reader; Kestrel may emit normal 100 Continue
-> exact-length/digest bounded stream into landed R2
-> durable/restart-capable R2 completion verification
-> only VerifiedCompleted enters landed R3 verification/staging
-> landed R4 commit re-read
-> landed R5 publication re-read and transition to Available
-> landed R6 obsolete-residue cleanup
-> closed final-result projection
-> release server capacity
```

No R2/post-R1 authority checkpoint is added. Recovery and replay must enter the
next landed durable phase; they must not shortcut R3, R4 or R5.

### 5.2 Retained-mode authority + begin

Do not copy the historical C6A evaluator. A new migration must extract one
owner-only internal composition primitive from the exact C6A body and replace
the existing public C6A function with a mode-closed wrapper over it. Add a
second mode-closed retained wrapper:

```text
internal: tagekyc.raw_export_begin_source_ingress_with_authority_core
existing wrapper: tagekyc.raw_export_begin_production_source_ingress_with_authority
retained wrapper: tagekyc.raw_export_begin_retained_source_ingress_with_authority
```

The shared primitive must contain the single copy of these invariants:

- normalize the same immutable claim identity;
- compute the same `alias_lock` and `exact_lock` before inspecting state;
- acquire `LEAST(alias_lock, exact_lock)` then
  `GREATEST(alias_lock, exact_lock)` with the landed bounded
  `pg_try_advisory_xact_lock` timeout semantics;
- with both locks held, exact replay recovers the frozen SnapshotId and appends
  no second Grant;
- only a genuinely new claim resolves the wrapper-pinned policy mode,
  derives/appends one Granted snapshot and invokes the landed begin function in
  the same transaction;
- caller supplies no authority or policy field;
- transaction failure rolls back snapshot and claim together.

Each external wrapper independently pins exactly one mode and accepts no mode
parameter. C6A remains `EncryptedExportPacket`; C6B is
`EncryptedRawVaultRetained`. Tests must prove both wrappers share the primitive
and cross-mode policy selection fails.

ACL is exact: primitive and wrappers are `SECURITY DEFINER`, owner
`tagekyc_raw_export_deployer` and use `SET search_path=pg_catalog`. The internal
primitive is revoked from PUBLIC, runtime, broker and every login; only its
deployer-owner may execute it through a SECURITY DEFINER wrapper. The two
mode-closed wrappers are revoked from PUBLIC/runtime and grant EXECUTE only to
`tagekyc_raw_export_claim_broker`. A broker call to the core or a caller-
selected mode must fail.

Down embeds and restores the complete pre-C6B C6A function definition,
signature, owner, `proconfig`, security-definer flag and ACL; it drops only the
C6B retained wrapper/core after revocation. Up/Down/Up tests compare exact
catalog/body/ACL fingerprints so rollback cannot erase or weaken C6A.

The API process must not receive a broker credential or invoke either wrapper
or `complete_*`. `C6B-BROKER-PROCESS-TRANSPORT-01 = RATIFIED`: add one separate
ASP.NET Core broker host and one TagEkyc API HTTP client adapter behind
`IRawExportSourceClaimComparisonBroker`. Their contract is versioned, closed,
JSON metadata only and bounded for request size, timeout, cancellation,
idempotency and replay; it contains no raw BIO, arbitrary extensions, SQL name,
function selector, connection string or caller-authored authority result.

The bootstrap endpoint uses ordinary HTTP bound only to the deployment-private
network/interface and is not publicly routed. HTTPS/mTLS, OAuth, API-key or
certificate lifecycle is not invented by C6B and remains deferred hardening.
This bootstrap authority does not permit treating network privacy as database
authority: the broker has a distinct database LOGIN inheriting the required
runtime resolver capability plus the NOLOGIN
`tagekyc_raw_export_claim_broker` role; API has neither that credential nor
broker-only SQL EXECUTE. Broker ownership of authority+begin and complete/R1
transactions remains unchanged. Transport readiness fails closed if the broker
endpoint is missing, unreachable, wrong-version, oversized, malformed or does
not return the closed response shape.

The isolated host uses exactly one dedicated database service principal,
`tagekyc_raw_export_claim_broker_login`, provisioned as `LOGIN NOSUPERUSER
NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS` by the deployment DBA/operator
through the existing protected database-secret deployment path. It inherits
only `tagekyc_runtime` for the landed resolver surface and the existing NOLOGIN
`tagekyc_raw_export_claim_broker` capability role for broker-only functions; it
receives no direct table privilege, ownership, grant option or additional
authority role. The public TagEkyc API login must not inherit, authenticate as,
or receive the credential of this principal. Removal revokes both memberships,
terminates its sessions, drops the LOGIN and removes its protected credential;
the two existing NOLOGIN capability roles remain. Broker readiness fails closed
when the LOGIN, exact attributes, memberships, credential or connected identity
is missing or differs. Creating this one service LOGIN is specifically
authorized and is not authority to create another broker capability model.

### 5.3 R1 complete and body admission

`RawExportSourceClaimComparisonBroker` remains the owner of authority+begin,
content commitment, subject token, custody profile and
`complete_raw_export_source_ingress_claim`. Its closed success response must
include the exact durable R2 handoff:

```text
OutcomeCode
SourceArtifactId
AttemptKeyReservationId
AttemptId
ExpectedEncryptionAttemptRevision
ExpectedFence
```

Add exactly one broker-only SQL wrapper:

```text
tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff
```

Its input signature is exactly the landed `complete_*` signature. In one outer
transaction it calls the landed completion function and applies this exhaustive
result/state projection:

| Complete result/current state | Projection | R2 handoff |
| --- | --- | --- |
| `NewReservation`; exact source head `Reserved`; current attempt matches source, revision and fence; termination null | `NewReservation` | all five handoff fields required |
| `ExistingMatch`; immutable publication is `Available` | `AlreadyAvailable` | all five absent |
| `ExistingMatch`; R2 complete/provisional or R3-R6 continuation is durably pending | existing ratified `RAW_EXPORT_SOURCE_RESUME_PENDING` from the frozen 36-row spine table | all five absent; reconciler continues |
| `ExistingMatch`; current attempt active/termination not durable | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` | all five absent |
| `ExistingMatch`; exact terminal disposition | that normative terminal OutcomeCode | all five absent |
| any non-success `complete_*` result | same normative metadata-only OutcomeCode | all five absent |
| missing, ambiguous, cross-source or impossible head/attempt/publication state | fail closed/internal fault; no admission | all five absent |

The wrapper locks the exact source head and current attempt before projecting.
No `ExistingMatch` path returns an R2 handoff; eligible same-owner re-entry must
have already CAS-created a new reservation/attempt and reach the
`NewReservation` row above. In one outer
transaction it returns the six fields above. It is `SECURITY DEFINER`, deployer-owned, search-path
pinned, revoked from PUBLIC/runtime and executable only by the broker role.
Down revokes then drops this wrapper without changing landed `complete_*`.

Do not query tables from API runtime. The wrapper must select the exact current
R1 head created/re-entered by that ceremony in the same broker-owned operation. Replay
returns the current eligible handoff or the normative metadata-only outcome;
a stale revision/fence can never start R2. The ingress coordinator observes a
successful committed broker response before admission signaling.

Metadata-only outcomes return immediately with zero body reads. Exact replay
of an already terminal/available source returns the frozen external result and
does not generate another key, object, R2 attempt or event.
`RAW_EXPORT_SOURCE_RESUME_PENDING` is not introduced by C6B: it is the existing
P6 row in the incorporated spine §10.0 table. The broker may use an internal
state discriminator to select that existing projection, but may not expose a
new code, union variant or 37th row. The authoritative external outcome count
remains exactly 36.

#### 5.3.1 Durable semantic terminal outcome

Add nullable `R2TerminalOutcomeCode` to the existing
`raw_export_source_encryption_attempts` surface in this new C6B forward
migration. Do not alter historical migrations or overload operational
`R2TerminationDisposition` (`Terminated` / `TerminatedBeforeStart`). The new
field is constrained to exactly:

```text
RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED
CONTENT_COMMITMENT_MISMATCH
RECAPTURE_REQUIRED
```

It is null while an attempt is active, retryable/re-entry, recoverable or only
operationally terminated without a replay-stable terminal result. A guarded
deployer-owned SQL CAS must write operational termination and the exact
semantic code atomically. Constraints reject active/recoverable attempts with
a non-null code and reject replay-stable terminal transitions with a null code.
Runtime and PUBLIC receive no direct table update authority. Down removes only
the new constraint/field/functions after restoring the pre-C6B function/ACL
state; it does not rewrite history or create a second ledger.

The three-code allowlist applies only to server-observed terminal outcomes of
the exact current custody attempt. A CaptureAgent that finds its local producer
buffer disposed, unavailable or expired before another server call returns
client-local `RECAPTURE_REQUIRED` without a database mutation and without
`R2TerminalOutcomeCode`. Only a server-observed `RECAPTURE_REQUIRED` attached
to the exact terminal attempt is persisted. The field is not an oracle for
every occurrence of the same external code.

The complete-to-handoff wrapper uses durable replay precedence only:

```text
1. Available publication
   -> RAW_EXPORT_SOURCE_ALREADY_AVAILABLE
2. durable recoverable R2/R3-R6 continuation
   -> RAW_EXPORT_SOURCE_RESUME_PENDING
3. active attempt or terminal transition not durable
   -> RAW_EXPORT_SOURCE_RESERVATION_BUSY (or exact frozen busy result)
4. exact durable R2TerminalOutcomeCode
   -> that exact existing terminal code
5. another explicitly authoritative landed terminal surface, only if census-proved
   -> its exact existing terminal code
6. impossible, ambiguous or contradictory state
   -> internal fail-closed; no admission
```

Never infer the semantic result from exception text, logs, process memory,
cleanup reason alone, generic termination, object absence or current execution
path. Available publication takes precedence over stale terminal residue from
an older attempt generation.

#### 5.3.2 Frozen 36-outcome P4-P6 census

`STRUCTURALLY_UNREACHABLE` below means unreachable as a value of the new
attempt field; earlier-phase reachability remains unchanged.

| Classification | Exact outcomes |
| --- | --- |
| `REPLAY_STABLE_TERMINAL` | `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` for the P5 actual-over-limit branch only; `CONTENT_COMMITMENT_MISMATCH`; `RECAPTURE_REQUIRED` |
| `RETRYABLE_OR_REENTRY` | `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`; `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_RESERVATION_BUSY`; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY`; `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS`; `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID`; `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED`; `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE`; `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` |
| `RECOVERABLE_CONTINUATION` | `RAW_EXPORT_SOURCE_RESUME_PENDING` |
| `AVAILABLE_OR_PUBLISHED` | `RAW_EXPORT_SOURCE_AVAILABLE`; `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE` |
| `STRUCTURALLY_UNREACHABLE` as the attempt semantic field | `ACCESS_DENIED`; `RAW_EXPORT_SOURCE_BINDING_INVALID`; `NOT_FOUND_OR_NOT_ALLOWED`; `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID`; `SOURCE_RETENTION_NOT_AUTHORIZED`; `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT`; `SOURCE_ENCRYPTION_FAILED`; `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`; `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID`; `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID`; `RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID`; `RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID`; `RAW_EXPORT_AUTHORITY_INVALID`; `RAW_EXPORT_SOURCE_SELECTION_NONE`; `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS`; `RAW_EXPORT_SOURCE_SELECTION_CONFLICT`; `RAW_EXPORT_SOURCE_UNAVAILABLE`; `RAW_EXPORT_SOURCE_INTEGRITY_INVALID`; `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED`; `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` |

This classifies all 36 existing rows without adding, merging, renaming or
reinterpreting an outcome. Temporary-unavailable and transport-protocol-invalid
retain re-entry semantics; resume-pending remains continuation; available
codes remain publication-derived.

### 5.4 R2-R6

The admitted stream calls `RawExportR2EncryptionOrchestrator`. Its
`ObjectPresentPendingVerification` result is not R3 admission. The coordinator
must then call the existing restart-capable `RawExportR2CompletionVerifier`,
which independently reopens the exact object, verifies AEAD and historic
commitment, and reaches `VerifiedCompleted`. Only that durable state enters R3
and the existing source-finalization services. C6B adds composition only. It
must not copy their SQL, frame codec, verifier, state machine, retry fence or
cleanup logic.

The mandatory authority sequence is:

```text
R1 complete check
-> R2 provisional ciphertext only
-> R2 completion verifier -> VerifiedCompleted
-> R3 fresh re-read
-> R4 commit fresh re-read
-> R5 publication fresh re-read -> Available
-> R6 obsolete-residue cleanup
```

Withdrawal between checkpoints must prevent the next progression. Provisional
ciphertext may exist only within the already-ratified bounded cleanup behavior
and never become Available after a failed checkpoint.

## 6. Capacity and readiness

### 6.1 CaptureAgent capacity owner

Add a process-wide atomic retained-buffer capacity owner returning a disposable
reservation token. It tracks both class-specific length and aggregate live
bytes/count. Reservation occurs after the SDK/device transient bytes reveal
exact class and length, but before construction or transfer of the
application-owned `SensitiveByteBuffer`.

Independent DG2 and selfie tokens remain held through verification and raw
ingress. Reservation failure immediately zeroes/disposes the transient bytes,
makes no network call, and returns a typed local capacity result. Token dispose
is idempotent and releases count/bytes on success, failure, cancellation and
exception.

Two clock domains are distinct:

- `ProducerPlaintextBufferLifetime` begins at the successful
  transient-to-retained CaptureAgent ownership transition and ends at local
  zeroization. It alone feeds the three authenticated producer claims.
- `ControllerCustodyRetention` begins when server custody is accepted at
  committed R1 and ends by the ratified controller earliest-end rule. It is
  expressed through server authority/custody facts, not the producer timer.

Neither starts, resets or extends the other. The ownership transition is the
sole producer-plaintext clock origin. In one operation the agent samples monotonic
start plus UTC wall clock, obtains the actual budget from its current validated
server-issued enrollment/configuration revision, computes one monotonic deadline
and one UTC-microsecond start/expiry projection, and binds the revision, clocks
and budget to the buffer lease. Retries reuse those frozen values and
deadline; HTTP dispatch, admission, retry or recapture logic never restarts or
extends them. Delay before first send consumes the same budget.

`C6B-CAPTURE-PLAINTEXT-BUDGET-SOURCE-01 = RATIFIED` and
`C6B-AGENT-CONFIG-PLANE-01 = RATIFIED`. Create only the bounded configuration
plane authorized here; no landed enrollment/configuration client, cache, or
notification consumer is assumed. The closed authoritative document contains
exactly `ConfigurationRevision`, `EffectiveAtUtc`, `ExpiresAtUtc`,
`RawExportEnabled`, `PlaintextBudgetSeconds`, and
`RawExportSourceClaimSafetyMarginMilliseconds`; it has no extension bag.
Provisioning remains a
server/controller deployment operation; the Agent or capture request cannot
choose or override the value. The existing
`RawExportSourceMaximumPlaintextRetentionBudgetSeconds` remains only an
independent server safety ceiling and must not be substituted or defaulted as
the actual value.

The Agent performs authenticated `GET /api/ekyc/capture-agents/self/configuration`
at first C6B-capable activation/enrollment/provisioning, startup, reconnect and
bounded periodic refresh. It sends `If-None-Match: "<opaque-etag>"` when cached.
The server returns either `200 OK` with the closed JSON document and ETag, or
`304 Not Modified` with the same or current opaque ETag. ETag is an HTTP cache
validator only, may change independently, and is never interpreted as or made
numerically equal to `ConfigurationRevision`.

`CaptureAgentConfigurationPollingIntervalSeconds` is a mandatory positive
deployment input; there is no activating default. Its upper bound is the strict
per-document freshness relation below rather than a silent hard-coded interval.
At fetch/validation time, readiness and the Agent must prove:

```text
nowUtc + PollingInterval + RawExportSourceClaimSafetyMargin < ExpiresAtUtc
```

and `nowUtc >= EffectiveAtUtc`. Failure means the document cannot authorize a
retained buffer. Missing, malformed, expired, not-yet-effective, stale, disabled,
wrong-Agent-bound, wrong-revision, out-of-range, or server-maximum/safety-margin
inconsistent configuration forbids retained ownership; otherwise permitted
`VerifyAndDiscard` behavior remains separate.

`C6B-AGENT-CONFIG-PUSH-HARDENING-01 = DEFERRED FORWARD ITEM`. Current C6B adds
no push notification, notification consumer, message broker, WebSocket or SSE.
A later push may only invalidate and accelerate this GET; it is never a second
authority source.

Every retained buffer freezes its configuration revision, actual budget, safety
margin, monotonic ownership start/deadline and UTC start/expiry once. A later budget increase never extends
an existing buffer. A budget decrease recomputes `ownership start + new budget`
and can only shorten the deadline; if already elapsed the Agent immediately
zeroes/disposes the buffer and performs no further retry. Disabling retained
mode prevents new retained buffers and disposes every pre-R1 buffer; state at or
after committed R1 remains owned by server custody machinery.

Ingress carries the frozen `AgentConfigurationRevision` with the three producer
clock claims. Before body read, the server resolves the authenticated Agent's
enrollment configuration and requires an accepted current revision, exact
budget equality, enabled retained mode, unexpired configuration, exact
`StartedAt + Budget = ExpiresAt`, unelapsed deadline and actual budget in
`[1, server maximum]`. No server response restarts or extends the producer
deadline.

Required CaptureAgent keys and fail-closed ranges:

| Key | Type/range |
| --- | --- |
| RawExportSourceMaximumChipDg2PortraitBytes | Int64 `[1, 67108864]` |
| RawExportSourceMaximumLiveSelfieImageBytes | Int64 `[1, 67108864]` |
| RawExportCaptureMaximumConcurrentRetainedBuffersPerHost | Int32 `[1, 32]` |
| RawExportCaptureMaximumAggregatePlaintextBytesPerHost | Int64 `[1, 2147483647]` |

### 6.2 Server capacity owner

Before authority/begin, atomically reserve a per-producer stream slot plus
deployment stream slot and exact declared bytes. Release on every terminal
path. No queueing of plaintext-bearing requests is allowed.

| Key | Type/range |
| --- | --- |
| RawExportCustodyMaximumPlaintextWindowBytesPerStream | Int64 `[1, 16777216]` |
| RawExportCustodyMaximumConcurrentStreamsPerProducer | Int32 `[1, 32]` |
| RawExportCustodyMaximumConcurrentStreamsPerDeployment | Int32 `[1, 256]` |
| RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment | Int64 `[1, 2147483647]` |
| RawExportIngressMaximumPreAdmissionBufferedBytes | Int32 `[1, 65536]` |

The pre-admission value is a qualification/readiness/telemetry ceiling for
unavoidable physical network/kernel/TLS/proxy memory buffering, not permission
for application body ownership or consumption. Exceeding the bound, any disk or
temp spill, any full-body intermediary buffer, or any application read/copy
before committed R1 fails qualification and readiness. Bounded physical early
bytes alone do not fail if they remain memory-only and are discarded on rejection.

### 6.3 Time/retry readiness

Use the landed custody-profile timing owner. Do not add competing TTLs.
Validate at least:

| Key | Range |
| --- | --- |
| RawExportSourceMaximumPlaintextRetentionBudgetSeconds | `[1, 86400]` |
| RawExportSourceMaximumRemainingContinuationWindowSeconds | `[1, 3600]` |
| RawExportSourceBusyRetryBackoffMilliseconds | `[1, 30000]` |
| RawExportSourceClaimSafetyMarginMilliseconds | exact landed server range and value; mandatory on CaptureAgent too |

Cross-field readiness must prove class maximum <= server per-stream maximum;
aggregate limits can hold the configured concurrent maxima without overflow;
CaptureAgent receives `RawExportSourceClaimSafetyMarginMilliseconds` through
the same server-owned enrollment/configuration document; the server validates
the accepted revision and identical value, and neither side publishes or
defaults a second margin;
all durations use monotonic elapsed-time decisions where local waiting is
involved; producer retention expiry, absolute source expiry and reservation
expiry remain distinct clocks.

Missing, malformed, out-of-range or inconsistent values fail readiness. No
development default may activate retained production ingress.

## 7. CaptureAgent implementation

### 7.1 Client port

Add one method to `ITagEkycClient` accepting closed metadata and a caller-owned
bounded buffer/stream lease. Implement it in `TagEkycHttpClient` with:

- `ExpectContinue = true`;
- exact `SocketsHttpHandler.Expect100ContinueTimeout = Timeout.InfiniteTimeSpan`
  on the dedicated handler;
- no automatic retry, redirect, decompression, buffering or body cloning;
- exact Content-Length and content type;
- deferred content serialization that reads the source only after continue;
- response-size limit and exhaustive final-union parsing;
- cancellation that never disposes the caller's buffer prematurely.

### 7.2 Device ownership boundary

Modify the device/result port so the capacity reservation is acquired at the
actual ownership transition. Required paths include:

- `TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs`;
- `TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs`;
- `TagEkyc.CaptureAgent.Devices/Hn212CccdReader.cs`;
- `TagEkyc.CaptureAgent.Devices/NoTempFileFaceCamera.cs`;
- `TagEkyc.CaptureAgent.Devices/Hn212FaceCaptureFrameStore.cs`;
- orchestration and composition wiring.

Do not reserve before exact size is known. Do not retain or hand a
`SensitiveByteBuffer` to Application before reservation succeeds.

### 7.3 Orchestration and retry

For each class, order is exact:

```text
capture and hold capacity-backed buffer
-> perform the existing verification use
-> append capture artifact/evidence
-> receive server-frozen CaptureAcceptanceId + CaptureRevision
-> submit raw ingress for that exact artifact/revision
```

DG2 completes this sequence before live-selfie raw ingress begins. Liveness
evidence keeps its existing submission order but liveness bytes are never sent
to raw ingress. CaptureAgent never calls `session.complete`, receives a
BusinessConsumer credential or freezes session selections. After both raw
results are `Available` or `AlreadyAvailable`, its existing `SUBMITTED_ALL`
means only that every CaptureAgent-owned artifact/evidence/raw submission was
accepted; it makes no session-completion claim. The hospital/BusinessConsumer
workflow later owns session completion and the atomic selection pair in §5.0.

Extend the existing receipt vocabulary with exactly two submission slots:

```text
RawChipDg2Portrait
RawLiveSelfieImage
```

Each is added to `AcceptedSubmissions` only after `Available` or
`AlreadyAvailable`. If the first raw class succeeds and the second terminates,
the existing outer terminal status is `FailedAfterPartialSubmit`; neither the
first source nor its durable custody is rolled back. Preserve the landed rule:
any already accepted existing or raw submission makes a later failure
`FailedAfterPartialSubmit`; `FailedBeforeSubmit` is legal only when
`AcceptedSubmissions` is empty. Ambiguous final-response loss uses the
existing `SubmissionStateUnknown` status until same-key replay resolves it.
The top-level success/status cannot be `SUBMITTED_ALL` unless both slots and
all pre-existing required submission slots are accepted. For both raw slots,
`SubmissionKind` is exactly `raw_export_source` and `SubmissionId` is the
server `SourceArtifactId` in lowercase `Guid D` form. No receipt field is added
for disposition: presence means the call returned `Available` or
`AlreadyAvailable`, while the per-call typed result remains process-local.
Receipt serialization never contains CaptureAcceptanceId, raw digest, locator,
key or internal claim state.

Retry is an explicit projector, not a generic HTTP retry policy:

- exact replay keeps the same ingress UUID and immutable identity;
- evaluation/busy waits use their exact returned clock/backoff;
- before waiting, monotonic remaining buffer lifetime and continuation budget
  must cover wait plus safety margin;
- disposed/lost buffer, invalid retention, terminal mismatch or insufficient
  lifetime returns `RECAPTURE_REQUIRED` and sends no body;
- cancellation releases and zeroes through the existing ownership path.

## 8. Exhaustive outcome, phase and residue contract

The complete 36-row table at `tip_88c1_planning_brief.md` §10.0, lines
3421–3458 of exact SHA
`D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC`,
is incorporated byte-for-byte as the only P0–P7 outcome source. Builder must
generate a counted manifest test proving exactly 36 rows/codes and reject a
missing, extra or duplicate mapping. This dispatch does not abbreviate or
override its phase, body, admission, residue, retry or carrying-union cells.

Transport projection is also closed:

- unauthenticated transport uses the existing authenticator's 401 response and
  has no application result when no authenticated response channel exists;
- authenticated missing-category/scope is 403 with `ACCESS_DENIED` only;
- an authenticated one-call operation that reaches P1–P7 returns HTTP 200 with
  exactly one `CaptureAgentFinalResult`, including semantic failures;
- malformed framing that prevents authenticated union parsing closes the
  connection or returns 400 with no internal detail and follows the normative
  protocol-invalid residue cell;
- readiness-only/structurally-unreachable codes never appear as endpoint
  responses;
- HTTP redirects, HTML/problem-details substitution and generic exception-body
  projection are disabled for this route.

Therefore retry classification is based solely on the exact typed outcome,
never on a generic HTTP status. Retryable, recapture-required, conflict and
terminal-cleanup outcomes may not be collapsed.

## 9. Exact mutation allowlist

### 9.1 Server repository

Existing paths that may be modified:

- `src/TagEkyc.Api/Program.cs`;
- `src/TagEkyc.Api/VerificationSessionEndpoints.cs`;
- existing evidence-result request/response contract and Application service paths;
- `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs`;
- `src/TagEkyc.Application/LocalDev/LocalDevApiKeyStore.cs`;
- `src/TagEkyc.Application/VerificationSessions/ApplicationAuthorization.cs`;
- `src/TagEkyc.Infrastructure/Auth/ApiKeyProvisioningService.cs`;
- `src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonBroker.cs`;
- `src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonServiceCollectionExtensions.cs`;
- `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs`
  — exact purpose only: change `RuntimeExecute` from true to false for
  `raw_export_append_capture_acceptance` and
  `raw_export_select_session_capture_acceptance`; no other expectation in this
  file may change;
- existing configuration/example deployment manifests that own the exact keys;
- existing protected database-principal provisioning/removal manifest or script
  — exact purpose only: provision/remove
  `tagekyc_raw_export_claim_broker_login` with the attributes and two
  memberships pinned in §5.2; no additional role or privilege is authorized;
- existing test projects and model snapshot/designer required by the migration.

Conditional new paths, exact purpose only:

- `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs`;
- `src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs` — authenticated
  self-bound configuration GET only;
- `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs`;
- `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs`;
- `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs`;
- one closed server configuration contract plus one server-owned provider/owner
  for the exact §6.1 document and ETag projection;
- `src/TagEkyc.Infrastructure/RawExport/RawExportIngressCapacity.cs`;
- `src/TagEkyc.Infrastructure/RawExport/RawExportCaptureAcceptanceCoordinator.cs`;
- one new OS-independent ASP.NET Core broker API project, its versioned
  metadata-only HTTP contracts/readiness, and one TagEkyc API HTTP client
  adapter implementing `IRawExportSourceClaimComparisonBroker` are authorized
  solely for `C6B-BROKER-PROCESS-TRANSPORT-01`;
- one new EF forward migration plus designer/model snapshot changes for the
  retained composition functions and the single additive
  `R2TerminalOutcomeCode` field on the existing encryption-attempt surface,
  its exact three-code constraint and guarded atomic terminal mutation;
- focused C6B unit/integration/transport/readiness tests.

### 9.2 CaptureAgent repository

Existing paths that may be modified:

- `src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs`;
- `src/TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs`;
- `src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs`;
- `src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs`;
- `src/TagEkyc.CaptureAgent.Client/TagEkycHttpClient.cs`;
- `src/TagEkyc.CaptureAgent.Devices/Hn212CccdReader.cs`;
- `src/TagEkyc.CaptureAgent.Devices/NoTempFileFaceCamera.cs`;
- `src/TagEkyc.CaptureAgent.Devices/Hn212FaceCaptureFrameStore.cs`;
- `src/TagEkyc.CaptureAgent.Devices/ReflectionHn212SdkBridge.cs`;
- `src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs`;
- `src/TagEkyc.CaptureAgent.Host/Program.cs`;
- packaging/example configuration and focused tests.

Conditional new paths:

- `src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs`;
- `src/TagEkyc.CaptureAgent.Core/RetainedRawBufferCapacity.cs`;
- one bounded versioned Agent raw-export configuration contract, authenticated
  GET client, ETag-aware in-memory cache, startup/reconnect/periodic polling
  owner, and readiness wiring; no push/notification consumer is authorized;
- focused C6B tests under `tests/TagEkyc.CaptureAgent.Tests`.

No other path is authorized by this candidate. Generated artifacts may change
only when mechanically required and must be separately identified.

## 10. Named proof plan

| ID | Proof |
| --- | --- |
| C6B01 | exact auth/category/scope, caller-observation metadata and final-union shape; authenticated client plus exact accepted artifact derive the three canonical identity fields; removed identity headers, every extra/missing field, UUID lexical mutation and cross-category credential fail |
| C6B02 | real client+Kestrel+production-equivalent proxy: before committed R1 there is no application read/copy, armed R2/provider writer or application-owned body buffer; bounded physical memory-only residue stays within the configured ceiling and rejection discards it with zero application reads |
| C6B03 | null/cross-client/cross-agent/cross-device artifact identity, AlreadyAvailable, conflict, invalid metadata and no-authority paths read zero bytes and create no claim/broker/R1/R2/key/object generation; existing binding-invalid result is exact |
| C6B04 | retained authority+begin is atomic; canonical client/producer/instance identity comes only from authentication plus one accepted artifact; pre-commit fault rolls back both; response loss replays without a second snapshot/claim |
| C6B05 | only DG2 portrait and live selfie pass; liveness and limit+1 fail before source read |
| C6B06 | declared length, actual length, digest, Agent configuration revision and producer retention fields are exact; omission/mutation is RED |
| C6B07 | admitted success and every server-observed abort enters landed R2/R3/R4/R5/R6 dispositions; server-observed replay-stable terminal outcomes of the exact attempt atomically persist exact `R2TerminalOutcomeCode`, while retry/re-entry, continuation and client-local pre-call recapture keep it null/no-mutation |
| C6B08 | server/agent application memory, temp, logs, telemetry, DB and object scan finds no plaintext residue or request spill; bounded pre-admission network/kernel/TLS/proxy memory is measured separately and never spills |
| C6B09 | response-loss replay returns exact durable semantic terminal code, retry/re-entry, continuation or publication result by the frozen precedence; two terminal meanings cannot collapse into generic termination; at most one logical source/object publication |
| C6B10 | local capacity is acquired after exact size and before retained ownership; count/bytes release on every path |
| C6B11 | consent/retained authority loss returns exact code; caller-authored authority and server TTL fallback are impossible |
| C6B12 | two-repository synthetic end-to-end exercises actual CaptureAgent client with infinite expect wait, deployed non-auto-continuing/non-buffering framing, post-R1 first application body consumption and R1→R6 to Available |
| C6B13 | exhaustive retry projector uses correct clocks and census class: temporary/protocol-invalid re-enter only as specified, resume remains continuation and server-observed replay-stable terminal never retries; local buffer loss/expiry before another call returns client-local `RECAPTURE_REQUIRED`, sends no body and performs no DB mutation |
| C6B14 | exact `tagekyc_raw_export_claim_broker_login` attributes and memberships permit only the mode-closed wrappers/complete-handoff; API cannot authenticate as or obtain its credential; core direct EXECUTE and PUBLIC/runtime/other roles are denied; owner/superuser is not the negative-test actor; removal revokes memberships, terminates sessions, drops LOGIN and removes credential without dropping capability roles |
| C6B15 | withdraw between R1/R3, R3/R4, R4/R5, R5/C1 and C1/C6A; next mandatory gate refuses and no unauthorized Available/delivery occurs |
| C6B16 | transport mutation removes infinite expect wait or enables proxy/request buffering; qualification/readiness must fail RED; an intermediary that auto-continues (emits `100 Continue` itself) must also fail qualification RED. C6B16 must be able to fail: a harness that stays GREEN under any of these three mutations is vacuous, not a pass |
| C6B17 | concurrent retained begin with `exact_lock < alias_lock` preserves global lock order, has no inversion timeout and no duplicate snapshot/claim |
| C6B18 | readiness rejects every missing/out-of-range/cross-inconsistent capacity/time key and blocks retained profile activation |
| C6B19 | qualifying evidence-response loss yields one acceptance event; concurrent BusinessConsumer completion acquires sorted two-class locks, derives exact events from durable Available ingress and atomically replays/selects the same pair; early/arbitrary/latest/caller-ID selection is RED |
| C6B20 | R1 broker result carries exact current R2 key-reservation/attempt/revision/fence; stale/cross-source handoff cannot invoke R2; ExistingMatch returns no handoff and obeys durable replay precedence |
| C6B21 | R2 writer result cannot enter R3 until restart-capable completion verifier durably returns VerifiedCompleted |
| C6B22 | DG2 then selfie ordering and receipt aggregation forbid SUBMITTED_ALL after either raw failure; success means CaptureAgent submissions only and never invokes/claims BusinessConsumer session completion |
| C6B23 | a peer that emits neither admission nor final response is cancelled before producer lifetime, reads zero body and releases both capacity tokens |
| C6B24 | spine §10.0 remains exactly 36 unique rows; all are classified by the five-way P4-P6 census and only server-observed instances of its three replay-stable terminal codes can populate `R2TerminalOutcomeCode`; client-local recapture never requires persistence; removing/replacing the field with generic termination is RED |
| C6B25 | acceptance-policy readiness rejects missing/stale/unprovisioned profile and arbitrary/caller policy; exact EvidenceResultId must belong to the artifact/session/kind |
| C6B26 | `tagekyc_runtime` cannot directly execute underlying append/select; wrapper succeeds only after qualifying durable evidence; control-plane readiness remains GREEN with both `RuntimeExecute=false` expectations and turns RED if either runtime EXECUTE is restored or the expectation is left true |
| C6B27 | migration Down restores exact pre-C6B C6A body/signature/owner/proconfig/ACL and acceptance-function ACL; Up/Down/Up fingerprints match. Down does not rewrite compiled readiness code: operational rollback pairs the restored pre-C6B database ACL with the pre-C6B binary, while migration tests assert the restored ACL directly before reapplying Up |
| C6B28 | legacy/non-retained session completion is byte/semantic unchanged; retained-profile completion cannot bypass the exact two-source selection; missing applicability fails closed |
| C6B29 | first activation/startup/reconnect/periodic conditional GET yields one self-bound closed versioned expiring Agent configuration; 200/304 and opaque ETag semantics are exact; polling range and freshness inequality fail closed; ownership freezes revision, actual budget, margin and producer lifetime/UTC projection; delay consumes it; increase never extends, decrease only shortens, disable disposes pre-R1; committed R1 separately starts controller custody retention; server maximum never substitutes for actual budget; no push path exists |

All database proofs run as the actual runtime/broker roles. All external proofs
use synthetic bytes and assert reader open-count, durable events, object/key
operations, cleanup evidence and memory/disk/log residue—not source-string
inspection alone.

## 11. PI-TAG-001 semantic matrices

### 11.1 Invariant Trace

| ID | Requirement | Owner | Required order/interface | Outcome/residue | Proof / mutation |
| --- | --- | --- | --- | --- | --- |
| I01 | qualified one-call transport | transport adapter | commit R1 -> begin application body read; normal Kestrel continue may follow | exact §10.0 P4/P5; physical buffering bounded, application ownership forbidden pre-R1 | C6B02/12/16; early read, timer fall-through, buffering or auto-continue |
| I02 | exact acceptance provenance | evidence coordinator + BusinessConsumer completion | server profile decides applicability; artifact -> evidence -> event -> ingress; applicable completion locks/derives/selects, non-applicable completion unchanged | atomic pair/conflict only when applicable | C6B19/28; caller flag, wrong-profile enable or retained bypass |
| I03 | retained authority | API resolver + isolated broker/shared SQL core | auth client -> exact accepted artifact producer/device -> membership -> locks -> append+begin -> complete R1 | binding-invalid before broker; §10.0 P3 residue | C6B01/03/04/11/17; caller/set-member identity or copy evaluator |
| I04 | executable R1→R2 handoff and replay | isolated broker + attempt durability | committed current head -> six-field New handoff; ExistingMatch -> publication/continuation/busy/exact durable terminal precedence | stale fence cannot write; no handoff on replay | C6B07/09/20/24; collapse outcomes/remove semantic field |
| I05 | complete custody chain | custody services | R2 writer -> completion verifier -> R3 -> R4 -> R5 -> R6 | Available only after all gates | C6B07/15/21; call R3 early |
| I06 | bounded plaintext ownership | agent/server capacity/config owners | conditional config fetch -> validate freshness -> exact size -> reserve + freeze revision/budget/margin/lifetime -> retain/read -> dispose/release | no default/extension/spill/residue; no pre-R1 application body ownership | C6B06/08/10/18/29; stale config/late clock/reset/limit+1/leak |
| I07 | closed caller/result shapes | API/contracts | auth -> exact metadata -> exhaustive union | 36-row contract | C6B01/24; extra/missing code/field |
| I08 | bounded agent completion | CaptureAgent orchestrator/receipt | DG2 raw -> selfie raw -> agent terminal; BusinessConsumer completion is separate | no false SUBMITTED_ALL or completion claim | C6B13/22/23; second failure/agent completion call |
| I09 | capability isolation | isolated broker | API adapter -> broker process -> exact dedicated LOGIN with only runtime-resolver + existing broker-role memberships; never broker credential in API | exact role/readiness denial and removable credential | C6B14/26; runtime EXECUTE, missing/extra membership, API credential reuse |
| I10 | lifecycle activation stop | deployment readiness | separate purge/hold TIP before real data | no real retained ingress | C6B18; enable without gates |

### 11.2 Outcome and Precedence

The exact 36-row normative table incorporated in §8 is the detailed matrix.
The command-level precedence is:

| Path | Higher-to-lower precedence | External result | Durable/no-residue rule |
| --- | --- | --- | --- |
| arrival | transport framing -> authentication -> category/scope -> existence | §10.0 P0/P1 | no disclosure; zero new ingress state |
| metadata | acceptance-event binding -> class/size/retention -> capacity -> broker | §10.0 P1-P3 | body forbidden; exact row residue |
| admission/body | R1 commit -> application reader starts -> normal HTTP continue/body -> R2 -> verifier | §10.0 P4-P6 | bounded physical residue; no pre-R1 application ownership; P4/P5 cleanup differs exactly |
| publication | VerifiedCompleted -> R3 -> R4 -> R5 Available -> R6 | §10.0 P6/P7 | no bypass; exact cleanup evidence |
| retry | replay-stable terminal -> active evaluation/busy -> eligible re-entry | exact union | no duplicate source/attempt/publication |
| outer agent | typed raw result -> lifetime -> receipt aggregation | retry/recapture/outer status | no retry after disposal/expiry |

### 11.3 Shape and Nullability

| Shape | Required | Absent/forbidden | Consumer |
| --- | --- | --- | --- |
| ingress wire observations | exact 13 fields in §4.3; Agent configuration revision plus three retention claims describe ProducerPlaintextBufferLifetime | ClientApplicationId, ProducerId, CaptureAgentInstanceId, acceptance ID, authority/policy/key/controller-custody clocks | API/coordinator |
| canonical ingress identity | authenticated ClientApplicationId + exact accepted artifact CaptureAgentId/DeviceId + §4.3 observations | null artifact identity, caller identity override, arbitrary AllowedCaptureAgentIds member | coordinator/broker/R1 |
| qualifying evidence response | RawCaptureAcceptance with artifact ID, acceptance ID, revision, class | absent for liveness/non-raw/rejection; policy/evidence internals always absent | CaptureAgent |
| broker request | authenticated claim/producer facts | selectors, keys, policy choice | isolated broker |
| R2 handoff | outcome + source + key reservation + attempt + revision + fence | raw/key bytes | coordinator/R2 |
| attempt terminal semantics | nullable `R2TerminalOutcomeCode`, exact three-code allowlist; non-null only for replay-stable terminal CAS | free text, retry/re-entry/resume/available codes, generic operational disposition substitution | complete/replay projector |
| Available/AlreadyAvailable | outcome/source/state/disposition | retry/internal fields | agent |
| EvaluationInProgress | outcome/retry-not-before | source/state/disposition | agent |
| OutcomeOnly | outcome | all optional payload fields | agent |
| receipt raw slots | exact two slot names; kind `raw_export_source`; SubmissionId = SourceArtifactId lowercase Guid D | disposition/digest/locator/key/internal claim | operator receipt |

### 11.4 Ordering Graph

```text
device transient bytes -> exact length/class -> local capacity -> retained buffer
-> verification -> artifact append -> qualifying evidence append/commit
-> idempotent server acceptance-event append -> raw ingress
-> metadata/auth/scope -> authenticated ClientApplicationId
-> exact acceptance/artifact -> non-null CaptureAgentId/DeviceId -> allowed-agent membership
-> canonical ingress identity -> server capacity
-> isolated broker shared authority core + begin -> complete -> committed R1 handoff
-> application reader/writer armed only after R1 -> normal HTTP 100 Continue/body -> R2 writer
-> retry/re-entry/continuation OR atomic operational+semantic terminal CAS
-> R2 completion verifier -> VerifiedCompleted -> R3 -> R4 -> R5 Available -> R6
-> final union -> CaptureAgent receipt aggregation -> zero/dispose -> capacity release

separate hospital/BusinessConsumer flow:
session.complete -> sorted two-class locks -> derive exact Available ingress
-> atomic acceptance selections -> existing completion result
```

Metadata rejection exits before signal/body. Response-loss retry returns through
the same acceptance and claim identities. Reconciliation starts only from its
landed durable state and never asks the producer for another body.

### 11.5 Test-Bite

| Claim | Positive control | Negative/mutation | Expected RED | Restoration proof |
| --- | --- | --- | --- | --- |
| P4/P5 boundary | trace shows committed R1 then first application body read; bounded physical residue remains memory-only | remove infinite wait, enable proxy/request buffering, auto-continue, spill or read before R1 | C6B02/16 | restore client/intermediary discipline, bound and post-R1 application read; harness must turn RED for all three transport mutations |
| identity authority | authenticated client plus exact accepted artifact yields client/producer/device and membership passes | restore removed identity headers, choose first allowed Agent, null/cross-device artifact | C6B01/03/04 | restore server derivation and binding-invalid pre-broker exit |
| terminal semantic durability | each of three allowlisted terminal codes commits atomically and replays exactly; available publication wins | null the field, use generic termination, persist temporary/resume, active+non-null, old terminal beats publication | C6B07/09/13/20/24 | restore constraint, guarded CAS and precedence |
| acceptance identity | evidence replay returns same event; concurrent BusinessConsumer completion returns same pair | agent/caller IDs, unsorted locks, one-sided or latest selection | C6B19 | restore durable derivation + sorted locks + atomic pair |
| acceptance policy/bypass | current ratified profile + exact EvidenceResultId succeeds | stale/arbitrary policy, cross-evidence ref or runtime direct append/select | C6B25/26 | restore profile readiness and wrapper-only ACL |
| completion applicability | retained profile requires pair; legacy profile unchanged | caller flag enables/disables or missing profile defaults legacy | C6B28 | restore server-derived predicate/fail-closed rule |
| broker isolation | exact dedicated login calls wrappers/complete and readiness is GREEN | put broker credential/EXECUTE in API, mutate LOGIN attributes/membership, or restore runtime EXECUTE while readiness expects denial | C6B14/26 | restore exact LOGIN, two memberships, credential separation and readiness graph |
| shared evaluator | both wrappers enter same core, distinct modes | copy core or accept caller mode | C6B04/17 | restore shared core/mode wrappers |
| R1→R2 fence | current six-field handoff succeeds | stale/cross-source fence | C6B20 | restore exact head projection |
| R2 verification | VerifiedCompleted reaches R3 | call R3 from writer result | C6B21 | restore verifier gate |
| capacity | exact-size reservation precedes ownership | retain before reserve/leak token | C6B10/18 | restore counters to zero |
| closed outcomes | all 36 rows map once | delete/duplicate/swap one row | C6B24 | restore exact hashed manifest |
| outer completion | both raw slots permit agent SUBMITTED_ALL only | second raw fails or agent calls/claims session.complete | C6B22 | restore partial-failure and owner separation |
| bounded wait | timely admission succeeds | silent peer past lifetime | C6B23 | restore linked monotonic deadline |
| producer/custody clocks | delay consumes producer budget; R1 independently starts custody retention | start producer at send/retry, reset either, conflate clocks or default actual to maximum | C6B29 | restore two origins and external actual budget |
| authority checkpoints | each next gate refuses withdrawal | bypass one gate | C6B15 | restore mandatory call/order |

## 12. PI-TAG-001 review ledger

| Round | Candidate SHA | HIGH | MEDIUM | LOW | Disposition |
| --- | --- | ---: | ---: | ---: | --- |
| 1 | `85A0D9077AA2586D9FA5D9E55ECEB247CF93A36273FE4B378B5203FAD2A127A6` | 7 | 5 | 0 | HOLD; 12 accepted findings applied to v0.2 |
| 2 | `F0C1D7BD57D9FC15626A9FC1C5555152A35EE704F8D3414DA13731D72DD72B8D` | 6 | 3 | 0 | HOLD; 9 accepted findings applied to v0.3 |
| 3 | `F00553EDDCB38F221A298AC201A461221DDA6F849AA2D1AAD9EC19C4DAB37627` | 4 | 2 | 0 | HOLD; 6 accepted findings applied to v0.4 |
| 4 | `75F09890479A277EC7D1B276B713192414C99D367959C40012C317ACE9D4B102` | 1 | 1 | 1 | HOLD; 3 accepted findings applied to v0.5 |
| 5 | `14BF0F92FB1151253E4BAEEF57364960584AE68155EC37246FC29ED2A1DB1354` | 2 | 1 | 0 | HOLD; mandatory root-cause checkpoint used; 3 accepted findings applied to v0.6 |
| 6 | `1B10146080251AEF393407F7973299BF6E2E4EB3B8C59D4816F7AF4969C4F973` | 1 | 0 | 1 | HOLD; applicability and stale selection term corrected in v0.7 |
| 7 | `BCFEF8FE6A08A86401B1B3B936D6E4AEA5F23E232C1170A298A88E819D09CA81` | 1 | 0 | 2 | HOLD; lifetime origin pinned and missing actual-budget source exposed in v0.8 |
| 8 | `FA3A825CF9247605D5F21095F04CD2EFB60E0DEFC60D31B89E536DCDF998D5DC` | 0 | 1 | 1 | HOLD; producer/custody clocks separated and stale durable-state term corrected in v0.9 |
| 9 | `9112DF391ED71BE3D2525AF625DC1E16C3E59B14E2CB49ADD9FE5A1412E5BCB0` | 0 | 0 | 0 | PASS — 0 actionable findings; clean result recorded in v1.0 for exact-byte hard-stop review |

Round-1 causes were incomplete end-to-end trace at four boundaries: P4/P5
transport linearization, acceptance provenance, R1/R2 handoff and R2 verifier;
plus incomplete propagation into outcome and PI matrices. No finding was
deferred. The broker-topology finding corrected a local implementation
assumption back to the ratified isolated-worker boundary.

Round-2 findings were incomplete propagation and overstatement after the first
patch: the isolated role had been mistaken for a landed process, the new
shared core was accidentally broker-executable, and acceptance was placed
before its evidence existed. All were corrected at their owning boundaries;
no Homeowner semantic decision was opened.

Round-3 exposed two source-of-truth gaps rather than authorizing inventions:
the broker process credential/IPC profile and capture-acceptance policy source
are not landed or ratified. They are now explicit blocking prerequisites. The
other findings were patch-local ACL, state-table, ordering and receipt-shape
omissions and were corrected in-place.

Round-4 corrected a patch-local acceptance linearization error: acceptance
events are appended after qualifying evidence, while immutable selection stays
inside authoritative session completion. It also reconciled the sole runtime
SQL grant wording and one stale proof ID.

### 12.1 Mandatory round-5 root-cause checkpoint

Patching was frozen after the round-5 verdict. Findings grouped as:

- acceptance lifecycle/owner propagation: the graph named session completion
  but did not carry its BusinessConsumer credential owner, incorrectly making
  CaptureAgent success depend on a command it cannot execute;
- completion concurrency/replay: the wrapper promised idempotency without
  tracing the landed selection function's insert-only behavior;
- source-of-truth dependencies: acceptance-policy source and isolated-broker
  process/transport remain explicitly unresolved from earlier rounds.

Cause classification: the first two are Drafter patch-local regressions caused
by incomplete end-to-end propagation; the last two are genuine missing
prerequisites, not reviewer invention. The matrices failed because caller
category was absent at the completion node and the Test-Bite row did not break
the selection lock/replay algorithm.

Corrective rules applied to v0.6:

1. every cross-process/command node names its credential owner;
2. every claimed replay path traces locks, existing-state branches and durable
   derivation, not only unique constraints;
3. CaptureAgent terminal language is limited to CaptureAgent-owned submissions;
4. open source-of-truth dependencies remain blockers and cannot be filled by a
   Builder choice.

Rounds 6–10 may converge for dispatch correctness because the three accepted
findings are within the current documentation scope and have exact proofs.
They cannot grant implementation authority: the external prerequisites
must first be separately ratified/resolved. Round 10 remains the hard stop.

Round-6 found one missing applicability edge after owner separation. V0.7
binds it to the same server acceptance/retained profile prerequisite, preserves
legacy completion exactly and fails closed for an applicable session. It adds
no caller mode flag or third blocker.

Round-7 found that the retention algorithm still lacked the producer-owned
actual-budget source. V0.8 freezes the clock at retained ownership and records
the missing source as a third explicit prerequisite; it does not substitute the
server maximum or create an implicit default.

Round-8 separated producer plaintext-buffer lifetime from controller custody
retention and removed `AlreadyAvailable` from durable-state terminology. Both
were propagation corrections; no architecture decision changed.

Round-9 was the first clean full-artifact verdict after the mandatory
checkpoint. V1.0 changes only status and this review ledger; round 10 reviews
that exact final byte and is the PI-TAG-001 hard stop.

## 13. Required verification commands

Builder records exact commands and results for both repositories:

```text
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

Additionally run focused PostgreSQL integration tests with actual ACL roles,
the production-equivalent proxy framing harness, CaptureAgent packaging/config
tests, migration Up/Down/Up, model-snapshot consistency and a clean sensitive-
residue scan. A pre-existing unrelated failure must be proved against baseline;
it cannot be silently waived or attributed to C6B.

Current verification record:

```text
CAPTUREAGENT_FULL_TEST_STATUS = NOT_EXECUTED

Restore is blocked by NU1004 involving the pre-existing/dirty
src/TagEkyc.CaptureAgent.Crypto/packages.lock.json state. The same NU1004 was
reproduced on clean baseline b193f631, so available evidence classifies it as a
baseline lock-file/restore failure rather than a C6B candidate regression. The
reported PASS set covers the server-side build/focused tests, SQL acceptance
tests, and CaptureAgent Core/Client build and format only. The full CaptureAgent
suite must execute successfully before any C6B completion claim.
```

## 14. HARD STOP conditions

STOP/RRI before mutation beyond the allowlist if implementation requires:

- a new database table, authority/consent model, arbitrary role, additional
  capability/authority role or privilege-model expansion; the single dedicated
  `tagekyc_raw_export_claim_broker_login` expressly authorized in §5.2 is the
  only new LOGIN exception;
- changing broker transaction ownership or granting broker SQL to runtime;
- a caller-selected policy, TTL, Grant, Snapshot, Permit or job;
- caller/local-config authority over ClientApplicationId, ProducerId or
  CaptureAgentInstanceId, selection from `AllowedCaptureAgentIds`, or null
  artifact identity reaching claim/broker/R1;
- multipart, upload sessions/parts/resume or public completion;
- a new encryption codec, key provider, object store or worker topology;
- application request buffering/consumption before committed R1, unbounded
  intermediary buffering, full-body buffering or any disk/temp spill;
- a third raw class or liveness media;
- package/delivery changes;
- purge/legal-hold implementation inside C6B;
- real patient/biometric data, deployment or production activation;
- weakening any landed R1/R3/R4/R5/C1/C6A authority checkpoint.
- overloading `R2TerminationDisposition`, inferring terminal semantics from
  generic state/cleanup/logs, adding a fourth semantic terminal code, a new
  outcome/table/ledger, or non-atomic terminal/outcome durability.

All ratified implementation prerequisites are recorded in Planning Brief v1.8
and consumed in section 2 without reopening them. The config-push item remains
deferred. Implementation remains unauthorized until a
bounded independent review confirms this v1.6 correction has no actionable
finding and the Homeowner separately grants implementation authority.

## 15. Review questions

The bounded independent review must answer, from exact bytes:

1. Does the deployed HTTP topology keep first application body consumption
   after committed R1 while enforcing infinite client wait, bounded memory-only
   physical buffering, no auto-continue intermediary and no spill?
2. Does the retained composition reproduce C6A lock/replay/ACL discipline
   without duplicating an authority engine?
3. Does the endpoint call the existing broker and R2-R6 services rather than
   reimplement them?
4. Is capacity acquired at both correct ownership boundaries and released on
   every path?
5. Are all metadata, result, retry and readiness shapes closed and exact?
6. Can C6B15 genuinely fail if any landed authority checkpoint is bypassed?
7. Are lifecycle enforcement and real-data activation still hard-stopped?
8. Is configuration GET self-bound and outside P4/P5 qualification, with exact
   200/304 ETag behavior, mandatory polling freshness and no push authority?

## 16. Terminal state

```text
C6B_PLANNING_BASIS:
  v1.8 / D7487BCC040101412A45B0AC58CCB3BE126298913A761E3030F227EA63736425

C6B_DISPATCH:
  CANDIDATE v1.6
  TRANSPORT_AND_AGENT_CONFIG_CORRECTION_APPLIED
  BOUNDED_V1_6_REVIEW_REQUIRED
  HTTP_EXPECT_100_CONTINUE_SELECTED_WITH_BOUNDED_PRE_ADMISSION_MEMORY
  EXPLICIT_KESTREL_INTERIM_API_NOT_REQUIRED
  C6B16_THREE_MUTATION_NEGATIVE_CONTROL_REQUIRED
  AGENT_CONFIG_GET_SEPARATE_FROM_RAW_INGRESS_QUALIFICATION
  AGENT_CONFIG_PUSH_DEFERRED
  PRIOR_HOMEOWNER_RATIFICATIONS_UNCHANGED
  IDENTITY_SOURCE_SERVER_DERIVED_FROM_AUTH_AND_EXACT_ARTIFACT
  R2_TERMINAL_OUTCOME_DURABILITY_THREE_CODE_ALLOWLIST
  EXTERNAL_OUTCOME_COUNT_36_UNCHANGED

IMPLEMENTATION_AUTHORITY:
  PAUSED — RRI DOCUMENT REVIEW AND RENEWED HOMEOWNER AUTHORITY REQUIRED

OPEN_BLOCKING_DEPENDENCIES:
  C6B-IDENTITY-SOURCE-01 — DOCUMENT-CORRECTED / REVIEW REQUIRED
  C6B-R2-TERMINAL-DISPOSITION-DURABILITY-01 — DOCUMENT-CORRECTED / REVIEW REQUIRED

RATIFIED_PREREQUISITES:
  PLANNING_BRIEF_v1.8 / D7487BCC040101412A45B0AC58CCB3BE126298913A761E3030F227EA63736425
  C6B-CAPTURE-ACCEPTANCE-POLICY-01
  C6B-BROKER-PROCESS-TRANSPORT-01
  C6B-CAPTURE-PLAINTEXT-BUDGET-SOURCE-01
  C6B-AGENT-CONFIG-PLANE-01
  C6B-IDENTITY-SOURCE-01
  C6B-R2-TERMINAL-DISPOSITION-DURABILITY-01

DEFERRED_FORWARD_ITEM:
  C6B-AGENT-CONFIG-PUSH-HARDENING-01

PRODUCTION_ACTIVATION:
  HARD_STOP_PENDING_SEPARATE_PURGE_AND_LEGAL_HOLD_LIFECYCLE_TIP
```
