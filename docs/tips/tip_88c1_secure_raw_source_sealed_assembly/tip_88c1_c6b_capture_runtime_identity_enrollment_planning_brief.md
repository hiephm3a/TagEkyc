# TIP-88C1-C6B-A — TagEkyc Capture Runtime Identity and Enrollment Planning Brief

**Status:** v1.7 — CAPABILITY-HANDOFF OWNERSHIP CORRECTED — BOUNDED RE-REVIEW REQUIRED  
**Date:** 2026-09-07  
**Risk tier:** High-risk  
**Owner:** Homeowner  
**Purpose:** Reconcile the CaptureAgent/embedded-SDK trust boundary before C6B implementation resumes.  
**Implementation authority:** NOT GRANTED  
**Stage/commit/push authority:** NOT GRANTED

## 0. Changelog

### v1.7

- Names the real handoff principals: SignFlow backend, SignAgent and AgentEkyc.
- Removes the obsolete Client-owner production evidence/readiness STOP. The
  existing SignFlow backend-to-SignAgent channel remains a stated integration
  contract, not a new TagEkyc workflow or readiness dependency.
- Keeps TagEkyc ownership on the testable intake boundaries: SignAgent-to-
  AgentEkyc one-shot loopback and direct in-process Embedded SDK handoff.

### v1.6

- Corrects capability-handoff ownership: the Client owns only its existing
  backend-to-local-application delivery; TagEkyc owns the standalone Agent's
  one-shot loopback intake, while Embedded SDK uses an in-process call.
- Records the Homeowner amendment to D8 and D12: there is no live production
  legacy population to preserve, so identity/authentication cutover is direct
  with no dual-read, legacy fallback or sunset gate.
- Closes the former external Client-integration planning prerequisite. The
  existing authenticated SignFlow channel is an integration input, not a new
  protocol for TagEkyc to design or a question for Client.

### v1.5

- Records that both independent reviewers passed exact v1.4 bytes with zero
  actionable findings after complete D9 removal.
- Records Homeowner ratification of D1-D8 and D10-D13 exactly as stated in the
  v1.4 Recommended default column. D9 remains removed, not implicitly restored.
- Leaves the separately named external Client-integration prerequisite open;
  no implementation or dispatch authority is inferred from decision ratification.

### v1.4

- Removed D9 and every operative SiteId/eligible-runtime-pool/client-routing
  authorization mechanism. Capture runtimes are TagEkyc-owned and may be common
  across on-premise or cloud deployments; Embedded SDK executes in its runtime.
- First bind now requires the one-time session capability plus an authenticated,
  eligible runtime/installation. Possessing a valid capability is the explicit
  session-selection authority; no fictitious ClientApplication-to-Agent or competing
  Site-to-Agent relationship is added.

### v1.3

- Closed `C6B-A-PLATFORM-OPERATOR-AUTH-CONTEXT-01`: platform OperatorAdmin
  authentication returns a dedicated client-independent context, never
  `AuthenticatedClientContext` with an empty or fabricated ClientApplicationId.
- Preserved the landed ClientApplication authentication context and C5 behavior;
  this correction authorizes only the new Capture Runtime management surface to
  consume the dedicated platform-operator context.

### v1.2

- Corrected the OperatorAdmin root credential: independent platform-operator
  credential partition, not a ClientApplication-bound `api_keys` row and not a
  tool-only allowlist edit.
- Added non-secret bind-restart reconciliation and recorded the Managed Agent
  loopback listener as an actual missing attack/callability surface.

### v1.1

- Applied the authorized post-hard-stop correction for the four Round-10
  findings: external Client backend handoff ownership, expiry-transition commit
  outcome, installation/current-generation FK feasibility and stale nonce proof.
- This is not an eleventh PI-TAG-001 round and makes no clean-review claim.

### v1.0

- Removed wall-clock index predicates and unbounded nonce terminal updates;
  materialized expirable authority state under locks and bounded nonce retention
  by the accepted signed-request window.
- Completed route-to-transaction reconciliation, config assignment, catalog
  current-revision CAS, offline diagnostic/pepper operations and discriminating
  proofs for every authority-producing surface.

### v0.9

- Added issuers and publication operations for every trust/config/routing/role
  authority, immutable per-generation role semantics and exact rotation races.
- Added per-route transaction/outcome/proof ownership, safe nonce terminal
  retention and an honest deployment-local readiness boundary.

### v0.8

- Made the §8 route inventory canonical for management and runtime surfaces;
  added operation, auth-scheme and persistence owners for every route.
- Closed role/config/routing authority storage, credential-generation ownership,
  capability uniqueness, rotation/cancel state, transaction-local lifecycle
  checks, bootstrap handoff and digest-key lifecycle.

### v0.7

- Projected the checkpoint model onto landed service signatures, authentication
  selection, nonce commit point, session cancellation and evidence append core.
- Added persistence shapes, bootstrap delivery and two-party key-rotation
  ceremony; completed management/route/conflict callability.

### v0.6

- Applied the mandatory Round-5 root-cause checkpoint instead of local prose
  patching: narrowed initial implementation to the existing Managed
  CaptureAgent process boundary, removed the unnecessary runtime-token issuer
  and the fictitious same-key TrustedEvidenceProducer separation.
- Added root-of-trust closure, transaction-conflict, process/key/plaintext and
  exact route/management-operation ledgers.

### v0.5

- Closed runtime-token acquisition, request-PoP replay ownership, capability
  envelope/handoff, capability race and exact route/principal census gaps.
- Added end-to-end observation/verdict/acceptance/R1 producer ledger.

### v0.4

- Added operation/principal and secret-lifecycle feasibility matrices.
- Removed impossible digest-only capability secret re-emission.
- Separated CaptureRuntime observations from TrustedAdapter analytic verdicts
  and pinned public verification material plus request proof-of-possession.

### v0.3

- Distinguished pre-R1 authorized mutations, exact response replay and post-R1
  internal custody convergence.
- Reconciled server capability with landed `Challenge`; pinned issuance,
  routing, rotation, audit and endpoint migration semantics.

### v0.2

- Closed Round-1 capability/replay, revocation, bootstrap, credential,
  lifecycle, execution-provenance, configuration, audit and migration gaps.
- Expanded the Homeowner decision aid and negative proofs.

### v0.1

- Opened the prerequisite after C6B proved that a self-bound Agent configuration
  cannot be derived from `AllowedCaptureAgentIds` when one client credential may
  authorize multiple Agent IDs.
- Separated ClientApplication, TagEkyc Capture Runtime, device installation and
  verification-session identities.
- Preserved the Homeowner direction that the Client supplies only an opaque
  challenge/session capability to the runtime; TagEkyc resolves the owning
  ClientApplication internally.
- Added PI-TAG-001 matrices, decision aid, migration impact and C6B re-entry gate.

## 1. PI-TAG-001 activation

```text
PI-TAG-001 PILOT
Pilot TIP: TIP-88C1
Slice: TIP-88C1-C6B-A
Risk tier: High-risk
Selected modules: API; SQL/schema/transaction; cryptography/key management;
  raw/restricted data; governance/docs
Required matrices: Invariant Trace; Outcome and Precedence; Shape and
  Nullability; Ordering Graph; Test-Bite
Worker/queue module: applicable to existing C6B continuation/cleanup only; no
  new queue or worker topology is selected.
Independent reviewer: required — identity, credential and raw-data boundaries
  are load-bearing and cross C6B plus the CaptureAgent repository.
Round-5 root-cause checkpoint: enabled
Round-10 hard stop: enabled
Pilot metrics report: required before closeout
```

Review rounds use complete-artifact review. A reviewer must read the complete
brief, named source evidence and adjacent C6B contracts; it must not stop after
finding one issue. From round 3 onward every round records why convergence has
or has not occurred and what drafting/reviewer mechanism needs improvement.

## 2. Analytical Summary / Intent Ledger

### Intent

Create the planning basis for a TagEkyc-owned Capture Runtime identity and
enrollment boundary that works for managed Agents and future embedded SDKs
without distributing a ClientApplication secret or requiring the runtime to
know the ClientApplication identity.

### Expected outcome

```text
ClientApplication authenticates to TagEkyc and opens a VerificationSession
-> TagEkyc issues an opaque capture challenge/session capability
-> Client passes only that opaque value to Capture Runtime
-> Capture Runtime authenticates with its own TagEkyc-managed credential
-> TagEkyc resolves ClientApplicationId from the session, not from runtime input
-> TagEkyc resolves CaptureAgentId and DeviceInstallationId from runtime credential
-> TagEkyc creates an exact session-to-runtime execution binding
-> capture/evidence/raw ingress proceed internally
-> Client receives only the authorized result/status surface
```

### Accepted Homeowner direction

1. CaptureAgent and future embedded SDK runtime are inside the TagEkyc trust
   architecture and are not identities managed by ClientApplication.
2. TagEkyc owns runtime enrollment, registration, suspension, revocation,
   credential rotation and cleanup.
3. ClientApplication does not distribute its API key to kiosk, PC or patient app.
4. Client passes the runtime only an opaque TagEkyc-issued challenge/session
   capability. The runtime does not need `ClientApplicationId`.
5. Server resolves `ClientApplicationId` from the authoritative session bound to
   the challenge/capability.
6. Client receives result/status only; raw capture processing and custody remain
   internal to TagEkyc except for separately authorized recipient delivery.

### Rejected branches

- one shared ClientApplication API key copied to every runtime;
- manual `AllowedCaptureAgentIds` as the source of runtime identity;
- caller-authored `ClientApplicationId`, `CaptureAgentId` or `DeviceId` authority;
- `CaptureAgentId := ApiKey.KeyPrefix` fallback;
- `PrincipalId -> ProducerId` mapping in application configuration;
- treating a bare SDK library running in a hostile host process as trusted
  without cryptographic runtime/installation identity;
- importing SignFlow business semantics into TagEkyc.

### Deferred branches

- mTLS and private-network hardening;
- vendor-specific mobile/device attestation;
- push configuration invalidation;
- a shared cross-product issuer/token-exchange implementation with SignFlow;
- SoftwareEmbedded/mobile enrollment and retained capture implementation;
- production activation and real retained-biometric ingress;
- purge/legal-hold lifecycle implementation, which remains a separate blocker.

### Non-claims

This brief does not select an OAuth/OIDC product, CA, MDM, attestation vendor,
hardware keystore, transport, token format or database implementation. It does
not authorize code, schema, endpoint, credential issuance, raw-data access,
deployment, production readiness, stage, commit or push.

### Dispatch readiness

Implementation dispatch is prohibited until the decisions in section 7 are
ratified, all five internal review rounds complete, external CC/GPT review is
clean, and the Homeowner grants exact-byte implementation authority.

## 3. Current landed state and defect

### 3.1 Client application identity

`ApiKeyProvisioningCommand` binds an API key to one `ClientApplicationId` and
optionally stores `AllowedCaptureAgentIds`. Authentication returns the same
client ID and allowed set in `AuthenticatedClientContext`.

Evidence:

- `src/TagEkyc.Infrastructure/Auth/ApiKeyProvisioningService.cs:37-45`;
- `src/TagEkyc.Application/AuthenticatedClientContext.cs:11-18`;
- `src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs:90-100`.

No tenant/client-application hierarchy or production tenant enrollment is
established by those surfaces. `ClientApplicationId` is the current resource
partition; `PrincipalId`, caller category and scope remain independent mandatory
authorization inputs.

### 3.2 CaptureAgent and device observations

Capture artifact input currently contains nullable caller-authored
`CaptureAgentId` and `DeviceId`. Artifact append checks the Agent ID against the
credential key prefix or `AllowedCaptureAgentIds`, then persists both values.

Evidence:

- `src/TagEkyc.Contracts/CaptureAgent/CaptureAgentContracts.cs:25-30`;
- `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs:88-98,125-130`;
- `src/TagEkyc.Infrastructure/Persistence/Entities/CaptureArtifactRow.cs:7-12`.

There is no authoritative Agent registry, device-installation registry,
enrollment API, runtime credential lifecycle or singular authenticated Agent
identity. Therefore the current system cannot prove which runtime process or
installation authored an observation when a credential covers multiple Agent
IDs.

### 3.3 C6B contradiction

C6B requires self-bound server configuration, but authentication can expose a
set of Agent IDs rather than one runtime identity. Selecting the first member,
requiring singleton cardinality or adding a configuration-file mapping would
invent authority. C6B implementation remains paused at this prerequisite.

### 3.4 Reuse-before-invention census

The Round-5 checkpoint performed a whole-repo symbol, route, entity and
migration census before selecting new surfaces:

| Need | Classification | Evidence/disposition |
| --- | --- | --- |
| OperatorAdmin category/actor attribution | `REUSE_AS_IS` semantics only | category, non-empty `PrincipalId`, hashing/fixed-time authentication pattern and authorization checks are reusable; no end-to-end production credential exists today |
| OperatorAdmin credential persistence/provisioning/authentication | `ACTUAL_MISSING` independent partition; `REUSE_PATTERN_ONLY` from API keys | current `ApiKeyProvisioningService.ScopesMatchCategory` rejects OperatorAdmin and `ApiKeyRow.ClientApplicationId` is mandatory; C5 fixture creates admin by direct SQL; never invent a ClientApplication/Guid.Empty mapping |
| C5 recipient management admission/idempotency/audit/readiness | `REUSE_PATTERN_ONLY` | recipient/client/delivery-specific contracts and tables cannot represent runtimes |
| recipient public-key registry | `REUSE_PATTERN_ONLY` | reuse canonical key/fingerprint/version/rotation discipline, not RSA-OAEP recipient encryption semantics |
| session route and append idempotency | `EXTEND_EXISTING` | add capability child operation; refactor neutral capture append core behind the runtime wrapper, then remove the legacy identity/authentication wrapper at cutover |
| TrustedAdapter evidence DTO/domain append and Client result reads | evidence `EXTEND_EXISTING`; reads `REUSE_AS_IS` | preserve neutral evidence/domain semantics and Client result reads; replace legacy producer authentication with the execution-binding runtime wrapper |
| candidate config/raw ingress | `EXTEND_EXISTING` | reuse ETag/transport/R1 skeleton; replace PrincipalId mapping and unmap old config route |
| runtime registration/install/verifier/generation | `ACTUAL_MISSING` | artifact Agent/Device fields and allowed sets are observations/policy, not identity |
| bootstrap, PoP nonce, capability and execution binding | `ACTUAL_MISSING` | no equivalent durable domain found |
| authoritative runtime configuration store | `ACTUAL_MISSING` | candidate appsettings mapping is not authority |
| Agent-side loopback capability listener | `ACTUAL_MISSING` | CaptureAgent has no HTTP/Kestrel/listener host; this is its first inbound socket inside the plaintext-holding process and requires explicit lifecycle/readiness/attack-surface implementation |

No recipient entity, `api_keys` row, `AllowedCaptureAgentIds`, artifact label or
session audit row may be renamed into runtime identity. New runtime-specific
persistence is justified only for `ACTUAL_MISSING` rows. C5 transaction,
idempotency, CAS, append-only audit and readiness patterns must be reused rather
than replaced by a second generic governance framework.

## 4. Proposed platform model

### 4.1 Independent principals

```text
ClientApplicationPrincipal
  requests a verification session and reads its authorized result

CaptureRuntimePrincipal
  is enrolled and credentialed by TagEkyc
  performs capture/evidence/raw-ingress operations

VerificationSessionCapability
  is the short-lived, replay-controlled link between the two principals
```

Neither principal impersonates the other. A runtime credential does not grant
business result-read authority. A client credential does not grant capture or
raw-ingress authority.

### 4.2 Runtime and installation identities

```text
CaptureRuntimeRegistration
  CaptureAgentId
  RuntimeType
  TrustProfile
  LifecycleState
  ConfigurationProfileRef

CaptureRuntimeInstallation
  DeviceInstallationId
  CaptureAgentId
  CurrentCredentialId
  CurrentCredentialGeneration
  LifecycleState
  EnrolledAtUtc
  RevokedAtUtc
```

`CaptureAgentId` is a TagEkyc-issued logical runtime identity.
`DeviceInstallationId` is a TagEkyc-issued installation identity bound to one
credential/public key. Caller-supplied serial number, MAC address, hostname or
platform advertising ID is metadata only and never authority.

The current-credential pair is a constrained nullable FK only while installation
is Pending and is updated atomically with generation activation. The immutable
canonical public verification key (JWK or equivalent),
algorithm/key type, `CredentialId`, generation and thumbprint are non-secret.
The installation private key is generated/held by the runtime and never reaches
TagEkyc. Thumbprint is derived canonically from the stored public key and is not
itself signature-verification material.

The pair is both-null or both-non-null. Redemption inserts the installation as
Pending with a null pair, inserts generation 1 referencing that installation,
then updates the pair and installation to Active before the same transaction
commits. The composite FK points to the credential-generation key; it may be
`DEFERRABLE INITIALLY DEFERRED`, although the stated insert order does not depend
on deferral. Transaction guards and readiness require every Active installation
to point to its exact Active generation; terminalization/rotation updates both
under the installation/generation locks. No second verifier authority exists.

One logical Agent may have multiple historical installations. The active
cardinality policy by runtime type remains a Homeowner decision in section 7.

Persistence is runtime-specific but follows landed C5 disciplines:

| Aggregate | Required key/shape constraints |
| --- | --- |
| platform operator credential | PK credential ID; nonzero PrincipalId; unique `teo_` prefix/hash; no ClientApplication FK; closed `operator.*` scopes; durable Active/Revoked sparse shape and revision; immutable `ExpiresAtUtc` is evaluated at every authentication and is not a decorative durable Expired state |
| runtime registration | PK `CaptureAgentId`; type and trust/config refs; nonzero revision; closed lifecycle |
| installation | PK `DeviceInstallationId`; FK runtime; immutable runtime link; closed lifecycle/revision; at most one Active per Managed runtime |
| credential generation | PK `CredentialId+Generation`; FK installation; sole authoritative canonical verifier/algorithm/thumbprint; valid interval; closed state; one Active generation; installation current pair FK points here |
| bootstrap issuance | PK issuance ID; unique operator+idempotency digest; keyed secret digest; runtime type/trust target; issue/expiry/redeem/revoke sparse shape |
| request nonce | unique credential+generation+nonce; signed/admitted time; immutable `PurgeAfter = SignedTimestamp + MaxRequestAge + MaxClockSkew`; no target/session fields before authentication |
| capability | PK public ID; FK session; keyed secret digest; predecessor/successor; audience/horizon/revision; persisted `ActiveUnbound`,`Bound`,`Revoked`,`Expired`; partial unique session only where state in (`ActiveUnbound`,`Bound`) |
| execution binding | immutable PK binding ID; unique session and capability; exact runtime/install/credential/trust revisions |
| runtime management operation/event | operator+idempotency unique operation and append-only event; actor/target/revisions/result/reason; no mandatory ClientApplication |
| role policy | versioned closed set `Bind`,`CaptureObservation`,`TrustedEvidence`,`RawIngress`,`Configuration`,`CredentialRotation`; server-owned next-generation assignment; credential generation records immutable policy revision |
| trust/config | catalog key + monotonic immutable revisions/effective interval + one CAS current pointer; versioned trust/config aggregates; rollback publishes a new revision |
| rotation authorization | PK rotation ID; exact installation/current generation; expiry/state/revision/idempotency; successor verifier commitment captured at completion |

All FKs use restrict/no-cascade for provenance. Terminal sparse-shape checks,
partial unique indexes, immutable-event triggers and replay result fields are
mandatory dispatch details, not optional Builder invention.

Bootstrap/capability keyed digests use a dedicated domain-separated
`CaptureRuntimeVerifierPepper` from the deployment secret store, with persisted
non-secret key version. Readiness requires current plus explicitly overlapping
previous verifier versions during rotation; secrets are never in database or
logs. Reusing recipient/API-key pepper without separate domain label and
reviewed rotation policy is forbidden.

### 4.3 Session capability

The Client receives an opaque value minted by TagEkyc. The runtime forwards it
without interpreting ClientApplication identity. Server validation must bind at
least session, challenge/nonce, purpose/profile, audience, expiry and replay
state. Whether this is a signed self-contained capability or an opaque random
handle to server state is deliberately not selected before decision D3.

For the recommended opaque-handle profile, server state is exactly
`ActiveUnbound -> Bound -> Expired|Revoked` or `ActiveUnbound -> Revoked`. The first
successful guarded transaction validates runtime, installation, credential,
capability and session, then atomically inserts the execution binding and moves
the handle to Bound. Response loss does not consume it again: the same complete
runtime/install/credential lineage and session return the same binding. A
different runtime/install, another session or expired/revoked handle fails
without session disclosure. Bound permits only the enumerated independently
idempotent artifact-observation and raw-ingress operations before the execution
horizon. After expiry/revoke it permits only read-only replay of already durable
operation results, never mutation or rebind.

The server derives:

```text
ClientApplicationId      := authoritative VerificationSession.ClientApplicationId
ProducerId               := authenticated CaptureRuntimePrincipal.CaptureAgentId
CaptureAgentInstanceId   := authenticated installation.DeviceInstallationId
```

The landed caller-authored opaque `VerificationSession.Challenge` remains
byte-for-byte unchanged and continues to feed existing evidence challenge-hash
bindings. It is not replaced by capability bytes. TagEkyc adds a distinct
non-secret `CaptureCapabilityId` plus a separately generated, at-least-256-bit
`CaptureCapabilitySecret`. The ID locates server state; a keyed/salted digest
verifies the secret in constant time. That state binds the existing
session/challenge hash. Neither the public ID nor its digest is bearer
authority. Existing challenge DTO/echo/hash semantics
cannot change without a separately versioned migration.

Capability issuance is owned by the authenticated ClientApplication session
boundary. Only an eligible Active, unbound, non-terminal session may receive
one; a required caller v4 issuance key makes issuance replay-safe. Server policy
fixes audience and TTL—Client cannot request audience, AgentId or TTL.
Issuance, replacement and bind use the same session-scoped advisory-lock/CAS
domain. Exactly one Active capability exists per session. A Bound capability
wins every race and cannot be replaced. Replacement requires the exact current
ActiveUnbound revision and atomically revokes it, inserts its successor and
writes audit. A losing request returns conflict or exact non-secret state; it
never receives another active secret. Cancel/terminal session state
revokes it, including after Bound. The capability is bearer-secret material,
returned once and retained by the Client until handoff; server stores only its
digest and can never re-emit it. Issue-response loss is not falsely replayed:
the authenticated Client may atomically revoke an unbound lost capability and
mint one replacement under a new issuance key. If already Bound, only non-secret
status is returned and the existing execution proceeds.

Under the shared session lock, issue/replace/bind/cancel first materializes any
past immutable horizon as persisted `Expired`, increments revision and appends
one event, then evaluates the requested transition. The partial unique index is
only on persisted `ActiveUnbound|Bound`; it never references wall-clock time.
Exact replay after materialization returns the same non-secret terminal status.

Materialization is a committed internal transition even when the requested bind
or replacement is denied. The persistence command returns a typed
`TerminalizedExpiredAndDenied` result; the application commits the expiry,
event and idempotency result, then maps that typed result to the public denied
response. It must not throw/rollback merely because requested work was denied.
An issue operation may materialize the prior expiry and create one successor in
the same successful transaction; exact replay returns its original result.

Execution horizon is `min(CapabilityExpiresAtUtc, SessionExpiresAtUtc)`. While
Bound and before that horizon, the exact runtime/install may perform the ordered
artifact/evidence/raw mutations, each with its own durable idempotency identity
and binding check. Expiry forbids new mutation and R1. Metadata replay may return
already-durable state but grants no work. After R1 commits, body consumption
already admitted in that request and internal R2-R6 convergence may finish;
later source use remains under its existing authority gates.

The Client-to-runtime handoff carries `CaptureCapabilityId` and
`CaptureCapabilitySecret` separately through a protected non-URL channel. They
must never appear in query strings, deep-link URLs, process arguments,
clipboard, crash reports, analytics or persistent logs. Platform-specific IPC
or app-link realization is a dispatch gate: if the selected platform cannot
protect the handoff from unrelated applications and logs, that profile STOPs.
The Client erases its copy after acknowledged handoff or on replacement,
cancellation or expiry. Only bind sends the ID and secret in separately named
body fields; request PoP covers the ID and hash of the presented secret, never
the server digest. After bind, operations send only the non-secret binding ID
plus installation-key proof. Runtime erases the secret after confirmed bind or
on cancellation/expiry; restart without an acknowledged binding requires status
resolution, never secret persistence or re-emission.

Restart recovery is callable through signed non-secret reconciliation:

```text
POST /api/ekyc/capture-runtime/executions/reconcile
body: CaptureCapabilityId + BindOperationId
auth: current installation CredentialId/Generation + timestamp/nonce/signature
```

The runtime persists only those two non-secret IDs before bind. The server
looks up no session by caller input and returns `BindingId` plus execution
horizon only when the committed binding matches the exact runtime,
installation and credential lineage and the bind operation ID. Unbound,
mismatched, revoked or unknown inputs return one non-enumerating result. The
route never receives or returns capability secret, never creates/rebinds state,
and cannot recover another installation's binding. If current credential was
rotated/revoked, ClientApplication remains the only result reader and the
session must follow cancel/new-session policy; no revoked-key recovery exists.

For the SignFlow Managed profile, `ClientApplication` means the SignFlow backend
that keeps its TagEkyc API key. SignAgent is the local application and is not
that principal; it never receives the API key. After capability issuance, the
SignFlow backend delivers the one-time ID/secret to the exact authenticated
SignAgent session over SignFlow's existing protected channel. That existing
SignFlow-owned channel ends at SignAgent. TagEkyc owns
the next boundary: standalone Managed Agent exposes the one-shot loopback intake
for SignAgent-to-AgentEkyc transfer; Embedded SDK accepts the capability through
a direct in-process call. No additional Client transport protocol is a TagEkyc
planning prerequisite.
The envelope is body-only, `no-store`, non-redirecting, one-recipient and bound
to the SignFlow user/session, TagEkyc VerificationSessionId, capability ID and
expiry. SignAgent acknowledges receipt; the backend erases after acknowledgement
and SignAgent erases after AgentEkyc acknowledgement/cancel/expiry. Response loss queries
non-secret capability status and otherwise uses the authorized unbound
revoke/reissue flow—never secret replay.

The existing SignFlow channel is assumed to provide authentication,
confidentiality, exact recipient/session binding, replay resistance and
log/cache redaction. C6B-A does not implement, certify or gate readiness on that
channel. Its tests begin at the TagEkyc-owned loopback or in-process intake and
must prove that neither intake accepts a Client API key or leaks capability
material.

### 4.4 Execution binding

Before accepting capture evidence or raw body bytes, TagEkyc freezes one exact:

```text
CaptureExecutionBinding
  VerificationSessionId
  ClientApplicationId
  CaptureAgentId
  DeviceInstallationId
  CaptureCapabilityId
  SessionChallengeHash
  BoundAtUtc
  TrustProfileRevision
  RuntimeRegistrationRevision
  InstallationRevision
  CredentialId
  CredentialGeneration
  PublicKeyThumbprint
  RolePolicyRevision
  ExecutionExpiresAtUtc
```

Retries reuse the binding. A different runtime or installation cannot take over
unless an explicit server-owned rebind transition is later ratified.

Initial persistence is immutable insert-once with unique ownership of both
`VerificationSessionId` and `CaptureCapabilityId`. Guarded bind compares the entire
frozen identity. No mutable binding state or takeover exists initially.

## 5. Integration profiles

### 5.1 Managed kiosk/PC

Enrollment may be bootstrapped by a one-time TagEkyc provisioning artifact,
operator workflow or deployment automation. The installation private key is
always generated and protected locally by the runtime; TagEkyc never creates or
delivers it. Manual creation of every Agent ID
inside ClientApplication provisioning is prohibited.

Every bootstrap realization must preserve these implementation-neutral
properties: only TagEkyc enrollment authority may mint it; at least 256 bits of
server entropy; digest-only storage; exact audience/runtime type/trust profile;
explicit issue/expiry times with no default;
single atomic redemption; exact response-loss replay returns the same identity;
proof of possession before Active; wrong/expired/replayed/concurrent redemption
creates no Active credential; mandatory rate/cardinality readiness; and no
secret/bootstrap plaintext in audit residue.

Redemption requires a caller-generated v4 idempotency ID and a proof-of-
possession signature over that ID, bootstrap audience and submitted public-key
thumbprint. The atomic fingerprint contains the complete tuple. Exact replay
returns only the same non-secret registration/install/credential identifiers;
private key or bearer material is never re-emitted. Mismatch conflicts without
revealing the original registration.

### 5.2 Embedded SDK/patient application

Embedding SDK code does not make the host application trusted. The initial
profile must treat a software-only installation identity as a bounded trust tier.
Automatic high-volume registration may be allowed only through a server-owned
bootstrap/risk policy with the same audience, expiry, replay, possession and
cardinality properties. Vendor attestation may strengthen this later but is not
silently required by this brief.

The patient app never receives a hospital ClientApplication secret. It receives
only the session capability needed to invoke the TagEkyc runtime flow. This
profile is architecture-only and is not eligible for the first implementation
dispatch; mobile bootstrap, protected handoff and attestation/risk controls get
a separate slice after the Managed profile lands.

### 5.3 SignFlow coexistence

This brief does not make SignFlow an identity source. A future common issuer may
allow one installation key to obtain short-lived audience-separated tokens for
TagEkyc and SignFlow, but each service validates its own audience and scopes.
Until separately ratified, no TagEkyc credential is automatically valid for
SignFlow and no SignFlow credential is automatically valid for TagEkyc.

## 6. Lifecycle requirements

Runtime-registration and installation transitions:

```text
Pending -> Active
Active <-> Suspended
Pending | Active | Suspended -> Revoked
Revoked -> Retired
```

`Revoked` and `Retired` are terminal. One logical `Managed` Agent has at most
one Active installation. `SoftwareEmbedded` names a runtime type/distribution,
not one shared principal: every app installation receives its own logical
runtime registration, installation identity and key lineage.
Credential transitions are
`Pending -> Active -> Rotated|Revoked|Expired`; all states after Active are
terminal. Every transition has monotonic revision/CAS, server effective time,
actor and reason. Suspended alone may reactivate; no credential is revived.

Required rules:

- only an Active runtime registration, Active installation and Active
  credential generation may start a new execution;
- suspension/revocation is checked at every externally reachable capture and
  configuration boundary;
- credential rotation atomically activates one successor generation and makes
  the old generation unusable for new requests;
- reinstall creates a new installation identity; historical identity is not
  overwritten;
- cleanup never deletes audit/session/evidence provenance merely because a
  runtime becomes inactive;
- inactivity may trigger expiry/suspension but cannot silently prove revocation;
- no lifecycle transition grants ClientApplication result-read authority or
  raw-recipient authority.

Runtime/install/credential state is resolved fresh for every new external
runtime operation. A signed request cannot override durable suspension/revocation
and no positive identity cache may outlive the request. Before
R1, suspension/revocation denies with no custody. After committed R1, exact
replay and existing R2-R6 custody convergence continue under durable fences;
revocation still denies new capture, bind, raw read/reuse or delivery.

Here “raw read” means later retained-source use, not body consumption already
admitted by committed R1. Rotation invalidates all pre-R1 bindings frozen to the
old credential; caller cancels/starts a new session. The successor cannot take
over or replay the old binding. After R1, server-owned convergence needs no
caller authentication. A revoked/rotated runtime cannot retrieve a lost
response; ClientApplication may read its authorized session result. No special
path authenticates revoked credentials.

Successful enrollment/lifecycle/rotation/bind mutation and its audit event
commit in one transaction; audit failure rolls back mutation. Rejected or
unauthenticated attempts use bounded non-authoritative security telemetry whose
failure cannot authorize or create state.

Lifecycle mutations acquire deterministic locks in this order:
runtime-registration, installation, credential-generation, then session where
an operation also touches a capability. IDs at the same level use one canonical
unsigned/hash order. Rotation, capability replacement and bind must publish
their exact lock/CAS plan before implementation; no path may invert it.

### 6.1 Direct installation-key request proof

The initial Managed profile has no runtime bearer-token issuer. Every
post-enrollment external CaptureRuntime request carries non-secret
`CredentialId`, `CredentialGeneration`, timestamp and unique nonce and is signed
directly by the enrolled installation private key. Bootstrap redemption is the
sole exception: it carries the bootstrap ID+secret and candidate public key plus
candidate-key signature over the closed redemption DTO. The
server resolves the immutable public verifier and freshly checks the exact
credential generation, runtime registration, installation, trust profile and
route scope before any protected target/resource lookup. There is no ClientApplication, Agent,
installation or audience selector in a runtime request.

The signature binds credential ID/generation, method, canonical path, request
timestamp, nonce, closed DTO bytes and capability/binding identity as applicable.
Raw ingress additionally binds operation identity, declared content length,
media type and content digest without a pre-R1 body read. Nonce uniqueness is
durably and atomically claimed under `(CredentialId, CredentialGeneration,
Nonce)` in a short authentication transaction after timestamp-window validation
and before protected resource lookup. It stores immutable `PurgeAfter` from the
signed timestamp plus the ratified maximum request age and clock skew, so a row
is deleted only after its original envelope can no longer pass timestamp
validation. Once committed, a downstream failure does not release the
nonce; retry uses a new nonce plus the same business idempotency key and obtains
the business operation's exact replay/result. Exact concurrent nonce replay
loses with one generic authentication/replay result, creates no protected
mutation and reveals no target existence. Every business mutation transaction
rechecks current credential/runtime/install state, so a rotate/suspend/revoke
between nonce admission and business mutation denies the request. Bounded
opportunistic cleanup uses the immutable indexed `PurgeAfter`; generation
terminalization performs no child-row update. Retries retain
the application idempotency ID but use a new nonce.
Exact algorithm, nonce size and clock-skew window remain D11 dispatch inputs;
there is no default.

Authenticated semantic mismatch after canonical identity resolution writes a
durable audit record. Invalid credential, signature, timestamp or nonce writes
bounded security telemetry only. Neither path discloses session/runtime
existence, and telemetry failure cannot authorize state.

### 6.2 Root management and enrollment callability

The trust graph terminates at a TagEkyc platform `OperatorAdmin` principal with
non-empty `PrincipalId` and exact scope `operator.capture-runtime.manage`.
Category, actor attribution and authorization checks are reused, but current
client-bound credential storage is not an executable root.

Initial implementation adds a dedicated `PlatformOperatorCredential` partition
with no `ClientApplicationId`: credential ID, non-empty PrincipalId, `teo_`
prefix, keyed secret hash/version, closed `operator.*` scopes, issue/expiry/
revoke state and revision. It reuses the existing CSPRNG, hash/fixed-time
verification and secret-once provisioning patterns. The offline
`TagEkyc.ApiKeyProvisioner` adds an explicit platform-operator command that calls
a dedicated provisioning service; merely adding `OperatorAdmin` to
`ApiKeyProvisioningService.ScopesMatchCategory` is insufficient and forbidden.
Authentication selects the platform partition from the non-overlapping prefix
and returns a dedicated `AuthenticatedPlatformOperatorContext` containing exact
`CredentialId`, non-empty `PrincipalId`, `OperatorAdmin` caller category, closed
`operator.*` scopes, key prefix and credential revision/lifecycle evidence.
`ClientApplicationId` is not a member of this context because it is not
applicable to a platform principal. `CredentialId` is the authenticated
credential identity used for audit and authorization; it must never be empty
and must not be populated through a fabricated `ApiKeyId` or client partition.
The new Capture Runtime operator endpoints consume this context directly and
require exact scope `operator.capture-runtime.manage`.

The existing `AuthenticatedClientContext`, `IApiKeyAuthenticator`, C5 recipient
management endpoints and their byte-semantic behavior remain unchanged. The
platform authenticator must not project a platform credential into
`AuthenticatedClientContext`; enabling a platform credential on an existing C5
surface would require a separately reviewed authority-neutral management
context/refactor and is outside this slice. No public API may mint the root
credential.

| Route | Caller | Effect |
| --- | --- | --- |
| `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances` | OperatorAdmin + runtime-manage | issue one runtime-type/trust-bound bootstrap secret |
| `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke` | same | revoke unused exact issuance |
| `POST /api/ekyc/capture-runtime/enrollments/redeem` | bootstrap secret + installation-key PoP | atomically activate runtime, installation and credential generation |
| `POST /api/ekyc/operator/capture-runtimes/suspend` / `reactivate` | OperatorAdmin | lifecycle CAS |
| `POST /api/ekyc/operator/capture-runtimes/credential-rotations` | OperatorAdmin | create expiring authorization bound to current generation |
| `POST /api/ekyc/operator/capture-runtimes/credential-rotations/revoke` | OperatorAdmin | revoke unused rotation authorization |
| `POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete` | old-generation signature + new-key PoP | activate successor and terminalize predecessor |
| `POST /api/ekyc/operator/capture-runtimes/credentials/revoke` | OperatorAdmin | terminally revoke exact generation |
| `POST /api/ekyc/operator/capture-runtimes/role-policies/assign` | OperatorAdmin | stage one reviewed closed policy revision for the next credential generation; no free-form scopes |
| `POST /api/ekyc/operator/capture-runtimes/configurations/assign` | OperatorAdmin | assign exact published config revision/closed reducing override to registration |
| `POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish` | OperatorAdmin | publish immutable closed Managed trust revision |
| `POST /api/ekyc/operator/capture-runtime-control/role-policies/publish` | OperatorAdmin | publish immutable closed role-set revision |
| `POST /api/ekyc/operator/capture-runtime-control/configurations/publish` | OperatorAdmin | publish immutable effective configuration revision |
| offline `capture-runtime-verifier-key provision|rotate|retire|readiness` | deployment secret authority | manage versioned domain-separated pepper; retirement denied while referenced |
| local `capture-agent diagnose --handoff` | deployment operator on Managed host | bounded non-secret attestation digest for bootstrap target/runtime version |
| `POST /api/ekyc/operator/capture-runtimes/revoke` / `retire` | OperatorAdmin | terminal lifecycle CAS |
| `GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness` | OperatorAdmin | identity/credential/config/role readiness |

All mutations require UUIDv4 idempotency, closed DTOs, request fingerprint and
operation plus append-only audit in one PostgreSQL transactional authority
boundary. Exact replay returns the same result except secret-bearing issuance,
whose replay returns non-secret status. Bootstrap issue returns its secret once with
`Cache-Control: no-store`; the server keeps only a keyed digest. Response loss
returns non-secret status on replay, after which the operator revokes/reissues.
Redemption generates the private key locally, submits only canonical public
verifier, and exact replay returns the same non-secret identities. Bootstrap
output is non-URL, non-redirecting and redacted from proxy/access/client logs;
the enrollee erases it after success or terminal failure.

Bootstrap delivery is deployment-controlled: issuer response lives only in
deployment-process memory and reaches the intended Agent through inherited
stdin of the directly launched enrollment process—never file, command line,
URL, clipboard or log. Agent acknowledges successful redemption; all holders erase
on acknowledgement, revoke/reissue or expiry. Sites lacking this bounded channel
STOP enrollment.

For rotation, the Agent generates its successor key locally. Completion proves
both current-generation possession and new-key possession. Response loss replays
the non-secret successor identity. A lost/revoked old key is not rotation: the
operator revokes that installation and uses fresh bootstrap enrollment.

### Rotation identity and response-loss amendment — RATIFIED_BY_HOMEOWNER (2026-09-07)

`CredentialId` is the installation-stable credential-lineage identity and does
not change across rotations of that installation. `Generation` is the position
inside that lineage; the successor is `(CredentialId, predecessor Generation +
1)`. `CandidateKeyId` is a globally unique per-rotation key identity selected by
the Agent. It is bound into the exact rotation-complete request fingerprint and
is the replay/idempotency identity of that completion; it is not CredentialId.
The successor SPKI and thumbprint are likewise known to the Agent and bound into
the request before transmission.

Rotation commit atomically activates that exact successor generation, marks the
predecessor `Rotated`, advances the installation current pointer and persists the
exact replay/result identity. The predecessor loses effect at commit. No special
authentication path may accept a `Rotated` or `Revoked` credential, and no
rotation-reconcile or status route is authorized.

After an ambiguous response, recovery is bounded to the existing rotation
completion route:

1. retry the identical R13 fingerprint authenticated by successor CRT1; success
   proves that the original transaction committed and returns the exact durable
   successor result;
2. if generically denied, retry R13 once with the original predecessor CRT1 plus
   successor ROTATE1 envelope, the same RotationId and CandidateKeyId; success
   means the first transaction had not committed and this retry committed it;
3. if both attempts are generically denied, rotation recovery terminates and the
   runtime requires operator-authorized re-enrollment through a fresh bootstrap.

All denials use one non-enumerating result. The Agent learns state only from
which bounded attempt succeeds, never from a code disclosing runtime, credential
or rotation state. No polling loop is authorized. Self-configuration is not a
commit probe and no generation is forced to carry the `Configuration` role for
transport recovery.

For R13 only, after ordinary successor CRT1 cryptographic, lifecycle and
current-generation authentication plus committed nonce transaction N, an exact
committed-operation replay is permitted even when the successor's frozen role
policy omits `CredentialRotation`. This is a read-only recovery exception: the
request must match stable CredentialId, successor Generation, RotationId,
CandidateKeyId, SPKI, thumbprint, Idempotency-Key and the committed fingerprint.
Any mismatch denies generically before protected-target disclosure. The
`CredentialRotation` role remains mandatory for the predecessor-authenticated
state-changing completion path. The exception grants no first completion,
target lookup or access to any other route.

Bootstrap and rotation expiry use the same rule: immutable expiry denies at the
transaction guard; any operation touching a past-horizon Issued/Authorized row
materializes one persisted `Expired` transition/event under its aggregate lock.
Indexes depend only on persisted state. No background worker is required.

Expired redeem/rotation-complete likewise returns the typed committed-terminal
result: expiry state, event and idempotency outcome commit, while requested
activation/completion remains denied. API mapping does not convert this typed
denial into an exception or outer transaction rollback. Exact replay observes
the persisted terminal outcome without another event.

Role policy is immutable on a credential generation. Assignment records only
the reviewed next-policy revision and requires rotation; the successor freezes
it. Old pre-R1 bindings fail after rotation and cannot use removed roles. A
role-bearing mutation committed before rotation remains valid at that checkpoint;
committed R1 converges internally. Trust/config/role publication is
append-only under the same OperatorAdmin operation/event transaction pattern;
there is no generic policy engine.

Exact role mapping is one-to-one: `Bind`→execution bind,
`CaptureObservation`→runtime artifact append, `TrustedEvidence`→runtime evidence
wrapper, `RawIngress`→C6B source ingress, `Configuration`→runtime self config,
and `CredentialRotation`→Agent rotation completion. Operator lifecycle uses
OperatorAdmin scope, not a runtime role. No alias scope vocabulary creates a
second authority. Staging a reduced role policy is not emergency revocation;
urgent removal uses suspend/revoke, then policy staging and rotation before
reactivation. Readiness exposes pending policy/config cutover.

## 7. Homeowner decision aid

The accepted direction above is not reopened. These implementation-affecting
details and their ratified dispositions are preserved below:

| ID | Decision | Options and consequence | Recommended default |
| --- | --- | --- | --- |
| D1 | Runtime identity cardinality | logical Agent with multiple installations vs one per installation | initial `Managed`: one logical Agent with at most one Active installation; SoftwareEmbedded deferred |
| D2 | Enrollment bootstrap | operator one-time secret vs deployment automation | initial Managed operator issuance, optionally invoked by deployment automation using the same OperatorAdmin contract; mobile bootstrap deferred |
| D3 | Session capability representation | opaque random server-state handle vs signed self-contained token | opaque high-entropy one-time/replay-controlled server-state handle for initial profile |
| D4 | Runtime credential | managed API key vs asymmetric installation key | asymmetric installation key signs each request directly; no shared/bearer client key and no runtime token in initial profile |
| D5 | Device binding | server installation ID + key thumbprint; hardware identifier; attestation | server ID + public-key thumbprint; hardware/attestation metadata optional by trust tier |
| D6 | Rebind/takeover | never; operator-approved; automatic before capture starts | no takeover after first binding; explicit cancel/new session initially |
| D7 | Configuration ownership | per Agent; per trust profile; layered | server-owned trust-profile base plus Agent-registration override, exact revision frozen |
| D8 | Legacy migration | immediate removal vs dual-read bounded migration | **AMENDED 2026-09-07:** direct identity/authentication cutover; no dual-read or legacy fallback because no live production legacy population exists |
| D10 | Initial trust catalog | fixed tiers vs open policy | fixed versioned `Managed` and `SoftwareEmbedded`; only Managed may activate retained raw ingress until separately ratified |
| D11 | Request-proof envelope | direct signature algorithm, nonce and clock window | direct installation-key signature; dispatch pins exact algorithm/canonicalization/nonce size/skew and zero post-revocation identity-cache tolerance |
| D12 | Legacy sunset | date; completion gate; indefinite | **AMENDED 2026-09-07:** not applicable; legacy identity/authentication path is removed in the implementation cutover, with no sunset gate |
| D13 | Analytic producer topology | Managed runtime trusted for both capture and analytics vs separate component | initial Managed CaptureAgent is one explicitly trusted process/principal and may call the landed TrustedAdapter boundary under separately authorized scope using the same installation credential; no false component/key separation; SoftwareEmbedded excluded |

`D9` is removed by Homeowner decision. There is no ClientApplication-to-Agent,
Site-to-Agent or eligible-runtime-pool authorization policy. First-bind
selection is conveyed by possession of the exact one-time session capability;
authentication, lifecycle, trust-profile, route-role, horizon and atomic
single-binding checks remain independent and mandatory.

```text
HOMEOWNER RATIFICATION — 2026-09-07
D1-D7:   RATIFIED exactly as Recommended default
D8:      RATIFIED AS AMENDED — DIRECT CUTOVER / NO DUAL-READ
D9:      REMOVED / NOT APPLICABLE
D10-D11: RATIFIED exactly as Recommended default
D12:     RATIFIED AS AMENDED — NOT APPLICABLE / NO SUNSET GATE
D13:     RATIFIED exactly as Recommended default
```

The 2026-09-07 Homeowner amendment supersedes only the original D8 and D12
recommended defaults: D8 is direct cutover and D12 is not applicable. Schema
migration remains fail-closed and must refuse unexpected non-fixture legacy
population rather than silently reinterpret or dual-read it.

## 8. Impact on landed surfaces

| Surface | Disposition |
| --- | --- |
| `ApiKeyProvisioningCommand.AllowedCaptureAgentIds` | stop using as Capture Runtime identity; remove its producer-authorization use at direct cutover; no dual-read/fallback |
| `AuthenticatedClientContext` | remains ClientApplication principal; do not add ambiguous runtime identity to it |
| `AuthenticatedPlatformOperatorContext` | new client-independent authentication result for Capture Runtime operator routes only; exact non-empty CredentialId and PrincipalId, OperatorAdmin category and closed operator scopes; no ClientApplicationId or fabricated ApiKeyId |
| capture artifact request identity fields | become compatibility observations or are removed; canonical values come from runtime context |
| `CaptureArtifact.CaptureAgentId/DeviceId` | preserve columns and downstream binding; source changes to authenticated runtime/installation; retained profile requires non-null |
| evidence capture bindings | preserve exact comparisons; values become stronger server-derived facts |
| C6B `ProducerId` | preserve meaning; source becomes authenticated runtime |
| C6B `CaptureAgentInstanceId` | preserve meaning; source becomes authenticated installation |
| ingress idempotency/lock identity | preserve shape and lock order; replace authority source only |
| per-producer capacity | preserve; key by authenticated runtime identity |
| Agent configuration GET | authenticate runtime credential and resolve self directly; no ClientApplication selector |
| C6B partial implementation | preserve non-identity work; replace candidate principal/config mapping before resumption |

Configuration composition is fail-closed:

```text
versioned TrustProfile base
-> optional Agent override from a closed field allowlist
-> most restrictive enablement/lifetime/capacity wins
-> one atomically published EffectiveConfigurationRevision
```

Overrides may only disable or reduce limits initially. Config GET returns and
retained buffers freeze the effective revision and effective/expiry interval.
Missing or conflicting layers disable retained capture.

Lifecycle authorization precedes configuration composition. An inactive
runtime, installation or credential receives access denied and no configuration
document; “disabled configuration” is not a substitute for that denial.
No runtime type is assigned to a ClientApplication or Site for first-bind
authorization. An authenticated Active runtime holding the exact unexpired,
one-time session capability may attempt the atomic first bind; its trust profile
and route role must permit the requested operation. The capability resolves the
session and its ClientApplication internally. Client and runtime never author a
ClientApplication-to-Agent, Site-to-Agent or eligible-pool relationship.

Before dispatch, a route/profile census must enumerate every consumer of caller
`CaptureAgentId`, `DeviceId`, key-prefix fallback and `AllowedCaptureAgentIds`
so the direct cutover removes all producer-identity fallbacks together. No
dual-read or legacy producer-authentication route remains authorized.

Initial topology is direct cutover. Capture-artifact and trusted-evidence
producer operations require runtime authentication and execution binding. They
use authority-neutral append cores behind the runtime authorization wrappers;
they must not fabricate `AuthenticatedClientContext`. ClientApplication session
creation/result reads and the landed evidence/challenge domain semantics remain
unchanged; only the legacy producer identity/authentication path is removed.

The first profile matches the existing Managed CaptureAgent reality: one
TagEkyc-trusted process owns the in-memory DG2/selfie buffers and performs the
analytics before raw ingress. It is one principal, not two cryptographically
separate components. Its enrolled installation credential may be authorized for
both capture-runtime routes and the landed TrustedAdapter evidence route; the
server resolves a route-specific category/scope from durable credential policy.
This is explicit multi-role trust, not audience-based principal separation.
No new plaintext IPC, second upload path or `TrustedEvidenceProducer` identity
is created. SoftwareEmbedded is excluded from this initial retained profile.

The landed evidence DTO/domain mutation is retained, but its client-shaped
application authorization signature is not reusable as-is. Extract one
authority-neutral evidence append core. Replace the legacy
`AuthenticatedClientContext` producer wrapper with a runtime wrapper
that resolves session ownership solely through `CaptureExecutionBinding`,
projects the existing TrustedAdapter category only after the durable
trusted-evidence role check, and supplies server-derived audit identity. It
never invents `ClientApplicationId` or `AllowedClientApplicationIds` in a client
context.

For the runtime wrapper, canonical Agent/installation identity comes only from
the execution binding. Existing DTO `CaptureAgentId`/`DeviceId` compatibility
fields must be absent; non-null values are rejected, not overwritten. The
neutral command carries authoritative session ClientApplicationId for
partition/FK only plus runtime actor/install/credential/binding identity for
audit. The extended append boundary acquires identity locks, rechecks
lifecycle/role/binding, applies resource/idempotency CAS and inserts evidence
plus audit in one transaction.

Producer routes accept only installation `CredentialId` plus signature.
`X-TagEkyc-Api-Key`, mixed envelopes or malformed runtime proof deny without
fallback. The same no-fallback rule applies to artifact/config/raw routes.

Every mutating runtime wrapper—not an outer service precheck—acquires the shared
runtime/install/credential locks and rechecks role/revisions inside the same
transaction as resource CAS and audit. This applies to bind, capture observation,
evidence and C6B R1. Read-only config uses one consistent database snapshot and
returns no document for inactive lifecycle.

Trust compatibility is closed initially: `Managed` may perform capture,
observations, configuration and retained ingress when required;
`SoftwareEmbedded` is deferred. Analytic evidence still enters through the
landed TrustedAdapter boundary. Profile downgrade denies new
external work/config under stale revision; post-R1 convergence continues.

The complete retained producer chain is:

```text
Managed CaptureAgent captures DG2/selfie bytes in its bounded local buffer
-> the same trusted process computes the analytic verdict from those bytes
-> its installation-signed request calls the landed TrustedAdapter boundary
-> server persists the resulting RawCaptureAcceptance
-> C6B resolves that exact accepted artifact/session/revision at ingress
-> committed R1 precedes body consumption
```

The credential policy explicitly grants or denies the closed route roles;
possession alone cannot select a role absent from policy. A Managed runtime
without the trusted-evidence role cannot submit a verdict.

### 8.1 Exact initial route and principal census

| Route/surface | Principal | Profile/disposition |
| --- | --- | --- |
| offline `TagEkyc.ApiKeyProvisioner platform-operator` | deployment operator | call dedicated platform credential service/partition; issue/revoke exact `operator.capture-runtime.manage`; never ClientApplication-bound or public HTTP |
| `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances` / `.../revoke` | OperatorAdmin | new runtime management operation/event pattern |
| `POST /api/ekyc/capture-runtime/enrollments/redeem` | bootstrap + candidate-key PoP | pre-enrollment exception; no credential envelope; atomic activation |
| `POST /api/ekyc/operator/capture-runtimes/suspend` / `reactivate` / `revoke` / `retire` | OperatorAdmin | runtime lifecycle CAS |
| `POST /api/ekyc/operator/capture-runtimes/credential-rotations` | OperatorAdmin | create expiring current-generation-bound authorization |
| `POST /api/ekyc/operator/capture-runtimes/credential-rotations/revoke` | OperatorAdmin | revoke unused authorization |
| `POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete` | current-generation + new-key PoP | atomic successor activation |
| `POST /api/ekyc/operator/capture-runtimes/credentials/revoke` | OperatorAdmin | terminal credential CAS |
| `POST /api/ekyc/operator/capture-runtimes/role-policies/assign` | OperatorAdmin | stage closed policy revision for next generation, not arbitrary scopes |
| `POST /api/ekyc/operator/capture-runtimes/configurations/assign` | OperatorAdmin | exact published config revision plus closed reducing override; expected registration revision |
| `POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish` | OperatorAdmin | append immutable Managed trust revision |
| `POST /api/ekyc/operator/capture-runtime-control/role-policies/publish` | OperatorAdmin | append immutable closed role-set revision |
| `POST /api/ekyc/operator/capture-runtime-control/configurations/publish` | OperatorAdmin | append immutable effective config revision |
| offline `capture-runtime-verifier-key provision|rotate|retire|readiness` | deployment secret authority | versioned pepper lifecycle; referenced version blocks retirement |
| local `capture-agent diagnose --handoff` | deployment operator on Managed host | produces bounded non-secret target/runtime-version-bound evidence digest; freshness fixed by bootstrap expiry |
| `GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness` | OperatorAdmin | exact runtime/install/credential/config/role readiness |
| `POST /api/ekyc/verification-sessions` | ClientApplication | landed; creates session |
| `POST /api/ekyc/verification-sessions/{id}/cancel` (`VerificationSessionEndpoints.CancelAsync`) | ClientApplication/session owner | `EXTEND_EXISTING`: finalization transaction terminalizes session and revokes capability atomically; exact replay returns same terminal state |
| `POST /api/ekyc/verification-sessions/{id}/capture-artifacts` (`VerificationSessionEndpoints.AppendCaptureArtifactAsync`) | none after cutover | remove/unmap legacy producer-auth route; runtime uses the execution-binding route below |
| `POST /api/ekyc/verification-sessions/{id}/evidence-results` (`VerificationSessionEndpoints.AppendEvidenceResultAsync`) | enrolled Managed runtime with explicit trusted-evidence role | preserve DTO/domain core; replace legacy producer auth with runtime wrapper |
| `POST /api/ekyc/verification-sessions/{sessionId}/capture-capabilities` | ClientApplication/session owner | new; issue/replace unbound capability |
| existing SignFlow backend → authenticated SignAgent capability delivery | SignFlow's existing integration | accepted input boundary only; no TagEkyc implementation/readiness evidence dependency and no new Client protocol prerequisite |
| `POST /api/ekyc/capture-runtime/executions/bind` | CaptureRuntime | new; capability bind |
| `POST /api/ekyc/capture-runtime/executions/reconcile` | current CaptureRuntime installation | signed read-only lookup by non-secret CaptureCapabilityId+BindOperationId; exact-lineage BindingId recovery only |
| `POST http://127.0.0.1:{managed-port}/tag-ekyc/capture-capabilities/accept` (AgentEkyc-owned loopback listener) | SignAgent on same managed host | one-time closed ID/secret DTO; no redirect/log/cache; security boundary is exclusive managed-host/process control, not unverifiable peer pinning; one acknowledged consumer |
| in-process `AcceptCaptureCapability` SDK boundary | embedding application in the same process | direct memory-only transfer; no listener/network surface; same one-time capability lifecycle |
| `GET /api/ekyc/capture-runtime/self/configuration` | CaptureRuntime | new canonical route; remove/unmap candidate `/api/ekyc/capture-agents/self/configuration` and prove it unreachable |
| `POST /api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts` | CaptureRuntime | new retained observation wrapper over neutral append core |
| `POST /api/ekyc/raw-export/source-ingress` (`RawExportSourceIngressEndpoints.IngressAsync`) | CaptureRuntime | extend candidate auth/context with execution binding; preserve one-operation R1-before-body topology |
| `GET /api/ekyc/verification-sessions/{id}` and evidence-package GET routes | ClientApplication/session owner | landed unchanged; no raw/runtime credential exposure |

The dispatch must replace every placeholder above with exact controller, DTO,
policy, database and test paths from a complete repo census. A route absent from
this table receives no authority by inference.

### 8.2 Operation and principal feasibility matrix

| Operation | Authenticating principal | Capability | Scope/audience | Lifecycle checks | Mutation owner |
| --- | --- | --- | --- | --- | --- |
| provision/revoke root manager | deployment operator/offline tool | deployment access | exact OperatorAdmin runtime-manage scope | platform credential lifecycle | dedicated platform credential service using reused hash/auth pattern |
| issue/revoke bootstrap | OperatorAdmin | UUIDv4 idempotency | runtime-manage | operator/runtime-type/trust-profile + issuance state | runtime management operation/event transaction |
| publish/assign trust, role and config | OperatorAdmin | UUIDv4 idempotency | runtime-manage | referenced revisions exist; closed profile validation | runtime control operation/event transaction |
| assign runtime configuration | OperatorAdmin | UUIDv4 idempotency + expected registration revision | runtime-manage | published compatible config; override only reduces | runtime management operation/event transaction |
| manage verifier pepper | deployment secret authority/offline tool | deployment change ID | secret-store access | reference scan + readiness + current-version CAS | deployment secret store; no product DB secret |
| diagnose local handoff | deployment operator/local tool | bootstrap target/runtime version | local deployment access | exclusive-host checks; evidence freshness ≤ bootstrap expiry | non-secret evidence output; bootstrap record only |
| create session | ClientApplication | none | business session-create | client/principal/policy | landed session service |
| cancel session | ClientApplication/session owner | landed idempotency | business cancel | session/capability revision | extended finalization boundary |
| issue/replace unbound capability | same ClientApplication/session owner | issuance idempotency | capture-capability issue | session active/unbound | new capability owner |
| receive capability at standalone AgentEkyc | SignAgent on managed host | one-time ID/secret envelope | one-shot loopback intake | pending slot/capability/expiry binding | AgentEkyc listener lifecycle + ACK |
| receive capability at Embedded SDK | embedding application in same process | one-time ID/secret parameters | in-process call | capability/expiry binding | SDK memory lifecycle + acknowledgement |
| enroll managed runtime | TagEkyc enrollment authority + bootstrap PoP | bootstrap | enrollment audience | bootstrap/runtime/install | new enrollment owner |
| suspend/reactivate/revoke/retire | OperatorAdmin | UUIDv4 idempotency | runtime-manage | expected runtime/install revision | runtime management operation/event transaction |
| authorize rotation | OperatorAdmin | UUIDv4 idempotency | runtime-manage | Active current generation; authorization expiry | rotation-authorization transaction |
| complete rotation | current installation + successor key | rotation ID + dual PoP + UUIDv4 idempotency | `CredentialRotation` | authorization/current generation/runtime/install | rotation + generation + audit transaction |
| revoke credential / assign role profile | OperatorAdmin | UUIDv4 idempotency | runtime-manage | expected generation/profile revision | runtime management operation/event transaction |
| read readiness | OperatorAdmin | none | runtime-manage | current aggregate snapshot | read only |
| bind capability | CaptureRuntime installation | bearer capability + request PoP | `Bind` | fresh runtime/install/credential/trust + session | atomic capability/binding owner |
| reconcile bind after restart | current CaptureRuntime installation | non-secret capability ID + bind operation ID | `Bind` | fresh lineage; exact committed binding match | consistent read only; no secret/state creation |
| local capability accept/ack | SignAgent under managed-host deployment boundary | one-time ID/secret DTO | local-only, no TagEkyc server HTTP auth | AgentEkyc listener/execution status | AgentEkyc memory cleanup; no server mutation until bind |
| config GET | CaptureRuntime installation | none | `Configuration` | fresh runtime/install/credential/trust | configuration reader only |
| append retained capture observation | CaptureRuntime installation | bound execution | `CaptureObservation` | fresh pre-R1 identity + binding/horizon | new runtime wrapper -> neutral append core |
| append analytic evidence verdict | enrolled Managed runtime, projected as TrustedAdapter only when durable credential policy grants that role | existing challenge/binding rules | `TrustedEvidence` | fresh runtime/install/credential + role + binding | runtime wrapper -> neutral evidence append/audit core; legacy producer wrapper removed at cutover |
| raw ingress | CaptureRuntime installation | bound execution | `RawIngress` | fresh pre-R1 identity + binding/horizon | C6B |
| post-R1 convergence | internal custody principal | durable R1 handoff | no external token | custody fences, not runtime reauth | landed R2-R6 |
| read result | ClientApplication/session owner | none | business result-read | client/principal/session | landed read service |

### 8.3 Authentication-scheme selection

| Surface class | Accepted envelope | Forbidden/fallback |
| --- | --- | --- |
| Operator management/control | dedicated `teo_` platform OperatorAdmin credential with exact runtime-manage scope | ClientApplication API key, runtime signature and mixed envelopes deny |
| enrollment redemption | bootstrap ID+secret, candidate public key, candidate-key signature, timestamp/nonce/idempotency | no CredentialId required; API-key/runtime fallback denied |
| post-enrollment runtime | CredentialId+Generation+timestamp+nonce+signature | API key/mixed envelope/identity selector denied |
| evidence producer | installation signature + binding, role `TrustedEvidence` | API key/mixed/missing role denied; no legacy producer fallback |
| Client session/result | ClientApplication API key | runtime identity cannot read result |
| local loopback handoff | exclusive managed-host/process deployment boundary | no claim of HTTP peer authentication; unsupported host STOP |

### 8.4 Secret lifecycle matrix

| Material | Generated/exposed | Stored | Verification/recovery | Rotation/revocation | Logging |
| --- | --- | --- | --- | --- | --- |
| Client credential | existing client provisioning; never runtime | existing protected store | existing client auth | existing lifecycle | never secret value |
| bootstrap bearer | TagEkyc; exposed once to enrollee | digest + metadata only | constant-time digest + PoP tuple; no secret recovery | single-use/expiry/revoke | digest prefix prohibited; IDs/result only |
| installation private key | runtime only | device secure storage | never sent; signs canonical request | successor key; old disabled | never |
| installation public key | runtime submits during PoP | immutable canonical verifier bytes + algorithm | signature verification; thumbprint derived | immutable generation lineage | thumbprint/ID allowed |
| session capability ID | TagEkyc; Client/runtime | indexed server state | non-secret locator only | follows capability state | ID allowed after authentication |
| session capability secret | TagEkyc returns once to SignFlow backend; existing authenticated SignFlow channel delivers to SignAgent; loopback delivers to AgentEkyc, or SDK receives it in-process | server stores keyed digest only; transient holder memory only | constant-time verification; cannot re-emit | each holder erases on next-hop ack/cancel/expiry; AgentEkyc/SDK after bind | always redact/no-store |

Runtime request envelope carries `CredentialId`, timestamp, nonce and signature;
capability ID and secret use separate dedicated non-URL header/body fields only
for bind. Later operations carry the non-secret execution-binding ID. Request
PoP covers method, canonical path, timestamp/nonce and metadata/body digest
where available. On bind it covers `CaptureCapabilityId` plus the canonical
hash of the presented secret; the keyed server verifier digest is storage-only
and is never exposed. Exact header names, size ceilings,
canonicalization and algorithms are dispatch gates; access logs redact secrets.

### 8.5 Root-of-trust closure

| Material | Root issuer/actor | Issuance and durable verifier | Consumer | Terminal cleanup |
| --- | --- | --- | --- | --- |
| OperatorAdmin credential | offline platform-operator provisioning | dedicated non-client credential partition using existing hash/fixed-time pattern | runtime management routes | platform credential revoke/expiry; no ClientApplication identity |
| verifier pepper versions | deployment secret authority | versioned secret-store entries; domain-separated bootstrap/capability subkeys; rows store key version | bootstrap/capability verifier | retain old version while any nonterminal/verifiable row references it plus maximum horizon; missing version readiness fails closed; controlled rollback, never DB/log |
| bootstrap secret | authenticated OperatorAdmin | issue route; digest, runtime type/trust, expiry, state, idempotency | enrollment redemption + new-key PoP | redeem/revoke/expire; enrollee erases |
| installation private/public key | Managed runtime locally | redemption stores canonical public verifier + generation; private key never leaves runtime | direct signed runtime/evidence requests | rotate/revoke/expire; secure local deletion |
| rotation authorization + successor key | OperatorAdmin authorizes; Agent generates successor key | authorization binds ID, current credential/generation, expiry and operation ID; completion PoPs old and new keys over authorization plus canonical successor verifier | rotation completion only | expire/revoke authorization; erase abandoned successor private key; exact replay returns successor IDs |
| request nonce | runtime CSPRNG | timestamp-window check then unique durable `(CredentialId,Generation,Nonce)` with immutable PurgeAfter | one canonical signed request | bounded indexed deletion only after envelope cannot be time-valid; readiness checks backlog; raw-BIO purge TIP does not own it |
| capability ID/secret | authenticated Client session owner | session child issue; public ID + keyed secret digest/state | bind once | cancel/revoke/expire; Client erases after acknowledged handoff, runtime after bind/horizon |
| execution binding ID | atomic bind transaction | immutable session/runtime/install/credential/revision row | subsequent observation/evidence/raw calls | historical provenance; no authority after horizon |

Every secret-bearing response is closed JSON over the selected HTTPS profile,
`Cache-Control: no-store`, no redirects, bounded body/header sizes and mandatory
proxy/application log redaction. Initial Managed local handoff is a loopback-only
JSON POST from SignAgent to the single-user managed AgentEkyc, with
the capability secret in the body, no query/referrer/logging, no redirect and an
acknowledged one-time transfer. The deployment assumption forbids untrusted
local tenants/processes. A deployment-local `capture-agent diagnose --handoff`
command verifies loopback-only binding, listener process ownership, firewall and
absence of forwarding/proxy middleware before activation; Operator records its
evidence digest in bootstrap issuance. Server readiness proves only presence and
binding of that attestation, not the remote host facts themselves. There is no
HTTP peer-auth claim. Sites unable to enforce exclusive host/process control
STOP pending hardened IPC.

This listener is new infrastructure, not a landed Agent feature. It is the
first inbound socket in the process holding DG2/selfie plaintext. It binds only
`127.0.0.1`/`::1`, rejects non-loopback Host/remote endpoints, has no proxy,
redirect, static-file, diagnostics or generic middleware, and accepts one
closed size-bounded capability DTO only while an explicit handoff slot is
pending. A slot is single-consumer and closes after valid ACK, timeout,
cancellation or first terminal error; the listener stops when no slot exists.
Port ownership/start-stop, duplicate delivery, malformed/oversized body,
slow-body, port preemption and forwarding are readiness/negative proofs. It may
not expose capture buffers, configuration, logs or any other Agent operation.

### 8.6 Transaction conflict matrix

All operations use one shared lock-key derivation/comparator and collision-safe
domain separation; dispatch must pin exact numeric derivation before SQL.

| Pair/race | Common lock/CAS and precedence | Winner/loser/replay residue |
| --- | --- | --- |
| bootstrap issue vs issue | operator+runtime-type/trust issuance lock; one Active issuance per exact operation identity | first commit wins; replay returns same non-secret status, never secret; mismatch conflict |
| bootstrap issue vs revoke | both serialize on the precomputable issuance identity lock and recheck issuance state | first commit is visible to second; revoke cannot report final NotFound while a racing Active issuance commits |
| bootstrap redeem vs revoke/expire | issuance lock then runtime/install/credential locks | first terminal transition wins; loser creates no partial identity; exact redeem replays IDs |
| redeem vs redeem (different IDs) | same issuance and target runtime locks | one activation; loser conflict, no orphan identity/credential |
| Managed activation vs activation | runtime lock then installation | one Active installation; loser terminal conflict, no orphan Active credential |
| rotation authorize vs authorize/revoke/expiry | runtime→installation→generation→rotation locks; one Active authorization/current generation | one authorization; revoke/expiry terminal; exact replay same non-secret state |
| rotation authorize vs lifecycle terminal | same identity locks; terminal lifecycle wins or later terminalizes authorization | no completion under suspended/revoked/retired identity |
| rotation complete vs complete/authorization terminal | identity→rotation locks; expected authorization/current generation CAS | one successor; exact replay same successor; expired/revoked authorization loses |
| rotation complete vs credential revoke/runtime terminal | same identity locks; terminal revoke/retire wins if first; completion valid only if all Active at commit | no split Active generation; response-loss replay returns committed successor or later terminal state without new key |
| role assignment vs rotation/evidence/R1 | identity/policy locks; assignment is next-generation only | current generation/binding unchanged until rotation; rotation freezes new revision; role removal takes effect for new external work at successor activation; already committed R1 converges |
| concurrent catalog publication/rollback | catalog-key current-pointer lock + expected revision CAS | one next revision/current pointer; loser conflict; rollback is a newer audited revision, never mutation |
| config assignment vs runtime config/read | identity→config locks; assignment CAS | reader gets wholly old or new revision; widening override rejected; exact replay stable |
| pepper rotate/retire vs verifier use/issuance | deployment current-version CAS plus DB reference/readiness scan | issuance snapshots current version; retirement loses while any live/verifiable row references it; missing reference fails closed |
| nonce cleanup vs replay | timestamp validation precedes nonce claim; cleanup only indexed PurgeAfter < now | no timestamp-valid envelope loses its nonce record; cleanup bounded and never joins lifecycle mutation |
| suspend vs reactivate/revoke/retire | runtime→installation locks; terminal revoke/retire wins, otherwise expected-revision first commit | loser returns current state/conflict; exact replay same revision |
| redemption/activation vs suspend/revoke/retire | same runtime/install locks; existing terminal state wins | no new Active install under terminal runtime; suspension prevents activation |
| capability issue/replace vs bind | runtime identity locks when applicable, then shared session/capability lock | existing Bound wins; one replacement per predecessor; exact replay non-secret status |
| capability issue vs issue / replace vs replace | session/capability lock + expected revision/unique Active | one secret-bearing winner; repeats return non-secret status, mismatch conflict |
| replace vs cancel/session terminal | session/capability lock; terminal session wins | no successor Active capability; loser returns terminal state |
| bind vs cancel/session terminal | bind acquires identity then session lock; cancel touches session/capability only and terminalizes under that lock | terminal session prevents Bound; committed Bound may be revoked, never transferred; no deadlock |
| request nonce vs same nonce | credential-generation nonce unique constraint in auth transaction | one request reaches protected lookup; loser generic replay, no mutation/disclosure |
| nonce admission vs rotate/suspend/revoke | nonce commit authorizes nothing; business transaction fresh-checks lifecycle | lifecycle denial wins before protected mutation; consumed nonce remains telemetry-safe residue |
| config/observation/evidence/R1 vs lifecycle transition | request nonce then business owner takes identity locks before resource lock | lifecycle commit first denies; business commit first is valid at its checkpoint; post-R1 convergence unaffected |

The extended finalization boundary accepts expected session revision and optional
current capability ID/revision. Under the session lock: no capability is valid;
ActiveUnbound or Bound becomes Revoked with one capability event; already
Revoked/Expired stays unchanged; concurrent not-yet-committed issue/bind must
recheck the now-terminal session and lose. Session terminal state, capability
transition, finalization result and audit commit together. Exact cancel replay
returns the same terminal result without a second capability event.

Audit event and successful identity/capability mutation share one PostgreSQL
transaction. An external audit sink/outbox/worker is not authorized; inability
to keep this boundary is STOP/RRI.

Per-route transaction ownership is fixed:

| Runtime mutation | Extended/new boundary | Locks/recheck -> resource/audit -> commit/replay |
| --- | --- | --- |
| bind | new capability/binding SQL boundary | identity→session/capability; role/horizon; binding+event; one commit; exact binding replay |
| capture observation | extend `EfAppendIdempotencyBoundary` neutral capture command | identity→session/artifact; role/binding; idempotency+artifact+runtime audit; one EF transaction |
| evidence verdict | extend `EfAppendIdempotencyBoundary` neutral evidence command | identity→session/evidence; TrustedEvidence role/binding; idempotency+evidence+runtime audit; one EF transaction |
| session cancel | extend `EfVerificationFinalizationBoundary.TryCancelAsync` | session/capability; state transitions+events; one EF transaction; exact terminal replay |
| C6B R1 | extend broker-owned R1 SQL composition | identity→binding→existing ingress locks; RawIngress role/horizon; R1+audit; broker commit; existing claim replay |
| lifecycle/control | new runtime management SQL/EF boundary following C5 pattern | runtime/install/generation/policy locks; expected revision; operation+event; one transaction |
| bootstrap issue/revoke | new runtime-management boundary | operator+runtime-type/trust issuance lock; expiry/state/idempotency; issuance+event; secret once/non-secret replay |
| bootstrap redeem | new enrollment boundary | issuance→runtime→install→generation; secret digest+candidate PoP; activation+event; exact identity replay |
| rotation authorize/revoke | new rotation-management boundary | identity→generation→rotation; horizon/state/idempotency; authorization+event; non-secret replay |
| rotation complete | new rotation-completion boundary | identity→generation→rotation; dual PoP+current/policy recheck; successor/current-FK+event; exact replay |
| capability issue/replace | extend session persistence boundary | session/capability; materialize expiry; state/idempotency; capability+event; secret once/non-secret replay |
| expiry-on-denied touch | owning capability/bootstrap/rotation boundary | aggregate lock; persist Expired+event+typed idempotency denial; commit internal transition; API denial does not rollback |
| role/config assignment | runtime-management boundary | identity→policy/config; expected revisions/references; staged assignment+event; exact replay |
| trust/role/config publication | runtime-control boundary | catalog key/current revision lock; closed validation/idempotency; revision+current pointer+event; exact replay |

All owners use the same database/connection transaction for the row set shown;
an outer service check or second repository transaction is insufficient.

### 8.7 Process, key and plaintext graph

| Process/boundary | Private key/secret available | Plaintext available | Allowed outbound operation |
| --- | --- | --- | --- |
| SignFlow backend (`ClientApplication`) | Client API key; capability until authenticated SignAgent acknowledgement | no raw BIO | session create/capability issue; existing authenticated one-recipient SignAgent delivery; authorized result read |
| deployment operator process | OperatorAdmin key; bootstrap secret transiently in memory/stdin pipe | no raw BIO | bootstrap issue/revoke and direct Agent enrollment launch |
| SignAgent on managed host | authenticated SignFlow session; capability transiently after backend delivery | no retained raw ownership | acknowledge backend delivery; loopback one-time AgentEkyc handoff |
| application embedding TagEkyc SDK | capability transiently in process memory | SDK-owned bounded capture buffers only | direct in-process capability acceptance and SDK execution |
| Managed CaptureAgent process | installation private key; capability until bind | DG2/selfie bounded buffers | bind, artifact observation, TrustedAdapter evidence when role granted, C6B raw ingress |
| TagEkyc API | public verifier and capability/bootstrap digests only | request-body bytes only after committed R1 | durable session/evidence/custody mutation |
| PostgreSQL | verifier/digests/revisions/identity/audit | no raw BIO | transaction authority only |
| object/key custody providers | provider-specific encrypted/custody material | no application plaintext contract | existing R2-R6 only |

There is no raw-byte transfer to another analytic process in the initial
profile. If deployment separates analytics from Managed CaptureAgent or admits
untrusted local processes, the current profile STOPs and requires a dedicated
key plus bounded plaintext IPC review.

## 9. Outcome and precedence

| Use case | Precedence | Result class | Residue |
| --- | --- | --- | --- |
| invalid Client session capability | capability validity -> session state -> expiry/replay | access/ineligible; exact code deferred to dispatch | no runtime binding, artifact, acceptance or raw state |
| invalid runtime credential | credential -> runtime state -> installation state -> scope | access denied | no session existence disclosure or capture state |
| exact bound operation | credential/runtime/install -> binding -> horizon -> operation idempotency | exact result/replay | no work beyond horizon |
| expired exact operation replay | authenticate current runtime -> binding identity -> operation idempotency read -> horizon | return existing durable response only; otherwise expired | never execute mutation |
| expiry after partial pre-R1 work | current identity -> existing binding -> horizon | deny next mutation/R1 | committed artifact/observation remains immutable; no delete/rebind; Client starts new session |
| rotated pre-R1 credential | current auth fails old; successor mismatches immutable binding | deny/cancel-new-session | no takeover or mixed generation |
| both valid but trust/profile incompatible | capability requirements -> runtime trust profile | ineligible | mandatory durable audit after authenticated canonical identity resolution |
| first valid bind | session unbound -> guarded bind | bound | one durable execution binding |
| exact retry | existing binding -> same runtime/install/capability | replay/re-entry | no duplicate binding |
| bind response lost/process restart | current signed lineage -> capability ID + bind operation ID -> exact committed binding | non-secret BindingId/horizon or generic unavailable | read-only; no secret re-emission, bind or takeover |
| competing runtime | existing binding mismatch | conflict/ineligible | original binding unchanged |
| revoked before R1 | credential/runtime/install state before capability/session detail | deny admission | no bind/R1/body |
| revoked after committed R1 | committed custody/replay fence -> R2-R6 convergence; deny new external work | converge custody; deny new capture/read/reuse | durable prior evidence retained; landed custody lifecycle/cleanup owns residue; C6B only composes it |
| management unauthenticated/missing scope | authenticate + scope before DTO target lookup | generic access denied | no target disclosure/mutation/audit authority event |
| bootstrap issue/revoke | actor/scope -> DTO -> runtime-type/trust-profile -> idempotency/state | created, non-secret replay, conflict or terminal revoke | operation/event atomic; secret once only |
| bootstrap redeem | bootstrap validity before candidate-key PoP detail -> idempotency -> activation CAS | identities, exact replay, generic invalid or conflict | no partial Active rows |
| rotation authorize/complete | actor or current-key auth -> authorization state/expiry -> dual PoP -> generation CAS | authorization/successor, terminal/conflict | one Active generation, atomic event |
| role/control publication | actor/scope -> closed schema/references -> idempotency/revision | published/assigned, replay or conflict | append-only revision/event |
| readiness lookup | actor/scope before target | snapshot or generic not-found | read only; local attestation presence is not host-fact verification |

Exact public codes and HTTP mapping are dispatch work after source census; no
new code is authorized by this planning brief.

## 10. Shape and nullability

| Shape | Required | Forbidden/nullable |
| --- | --- | --- |
| Client session request | authenticated ClientApplication context, purpose/profile inputs | AgentId/DeviceId authority forbidden |
| session capability | non-secret CaptureCapabilityId, one-time secret, keyed digest, session/challenge binding, audience, expiry, replay state | ClientApplicationId need not be visible to runtime; secret never persisted plaintext |
| runtime authentication context | non-null CaptureAgentId, DeviceInstallationId, opaque non-secret CredentialId/generation/key thumbprint, trust/lifecycle revisions and state | Client identity/secret/key material forbidden |
| capture execution binding | non-null session/client/runtime/install/capability IDs and revisions, credential lineage and key thumbprint | rebind/mutable-state fields absent initially |
| artifact provenance | non-null server-derived Agent/installation after direct cutover | nullable/fallback producer identity forbidden |
| client result | sanitized status/result/reference | raw BIO, runtime credential, internal device key, custody locator forbidden |

## 11. Ordering graph

```text
TagEkyc enrolls runtime -> installation key/credential becomes Active
ClientApplication authenticates -> creates VerificationSession
TagEkyc mints opaque session capability -> Client passes it to runtime
runtime authenticates independently -> submits capability
server validates credential/runtime/install state without session disclosure
-> validates capability/session and resolves ClientApplicationId internally
-> checks trust/profile compatibility
-> atomically consume ActiveUnbound capability + immutable CaptureExecutionBinding
-> capture artifact/evidence append with server-derived runtime provenance
-> C6B metadata admission/R1 before raw body consumption
-> R2-R6 custody flow
-> ClientApplication reads only authorized result/status
```

Every retry, reconnect, configuration fetch and raw-ingress entry must enter
through the same authenticated runtime context. No request path may reconstruct
runtime identity from artifact input, allowed-set selection or local config.
Exact-bound replay after committed R1 follows custody convergence and is not a
new runtime authorization/bind operation.

## 12. Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract | Outcome/state | Proof | Negative mutation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A01 | Client secret never reaches runtime | Client/API boundary | client auth before session mint | opaque capability | session only | distribution scan + E2E | inject Client API key into Agent config must RED |
| A02 | runtime identity is TagEkyc-owned | identity service | enrollment before Active | runtime auth context | singular Agent/install | auth integration | restore request AgentId authority must RED |
| A03 | Client ID is session-derived | session service | valid capability/session | execution binding | exact session owner | persisted binding proof | mutate caller ClientId must have no effect/RED |
| A04 | device is key-bound | enrollment service | possession proof before activation | installation record | key thumbprint + state | enrollment/rotation proof | use other installation key must RED |
| A05 | one execution binding | persistence owner | credential + capability validated | guarded bind CAS | one immutable binding | concurrency/replay proof | competing Agent must RED |
| A06 | categories remain isolated | API authorization | authenticate before lookup | distinct scopes/audiences | no cross-category access | pipeline tests | Agent reads business result must RED |
| A07 | retained provenance non-null | capture coordinator | binding exists before append | artifact/acceptance | exact Agent/install | DB/runtime proof | null/fallback reaches R1 must RED |
| A08 | C6B identity shape preserved | C6B coordinator | exact accepted artifact/binding | canonical claim | deterministic retry | claim/replay proof | change Agent/install across retry must RED |
| A09 | revocation is live | auth/config/capture boundaries | state check before operation | lifecycle reader | denied without new capture | revocation race proof | cached Active bypass must RED |
| A10 | SDK host is not automatically trusted | trust policy | trust tier before permitted profile | trust profile revision | profile eligibility | policy negative test | software-only tier claims strong tier must RED |
| A11 | capability bind is atomic/replay-safe | session/binding owner | ActiveUnbound + eligible runtime | immutable unique binding | exact replay/mismatch | response-loss/concurrency | consume without bind or bind twice must RED |
| A12 | audit is mandatory/non-sensitive | enrollment/lifecycle/bind owners | authenticated actor before mutation | append-only audit | exact operation/result/revisions | persisted audit | omit event or record secret/raw capability must RED |
| A13 | route roles are explicit without false principals | API/Application | durable credential role before evidence wrapper | neutral append core + runtime wrapper | route-specific category/result | pipeline/audit proof | absent evidence role, legacy/mixed auth envelope or fabricated client context must RED |
| A14 | direct signed authentication is revocation-live | runtime authenticator | installation PoP + nonce before lookup | route-scoped runtime context | inactive lineage denied | request/revocation proof | remove durable state recheck must RED |
| A15 | request PoP is replay-safe | auth persistence | consume credential-generation nonce before protected lookup | canonical signed envelope | duplicate nonce denied without disclosure | concurrent nonce proof | memory-only nonce cache must RED |
| A16 | capability secret stays bounded | Client/runtime handoff | mint once before bind | separate public ID + secret | destroyed at horizon | leak/redaction scan | place secret in URL/log/process args must RED |
| A17 | analytic verdict uses explicit Managed trust | Managed CaptureAgent | local bytes before TrustedAdapter append | durable trusted-evidence role | acceptance feeds exact C6B artifact/revision | end-to-end producer proof | credential without role submits verdict must RED |
| A18 | retained config has one route/authority | API/runtime config reader | runtime signature/lifecycle before consistent snapshot | canonical runtime self route | ETag/config revision | route/readiness proof | old route or legacy/mixed auth remains reachable must RED |
| A19 | Agent listener is narrow and ephemeral | Managed Agent local host | pending slot before body acceptance | loopback one-shot DTO only | ACK/timeout/error closes slot/listener | Agent host/readiness tests | wildcard bind, persistent listener, extra route, oversized/slow/duplicate/forwarded request must RED |

## 13. Test-Bite Matrix

| Claim | Positive | Negative | Broken mechanism | Expected RED |
| --- | --- | --- | --- | --- |
| independent principals | valid client session + valid runtime | client credential used on runtime endpoint | category/scope separation removed | A01/A06 |
| session-derived client | capability resolves owner | runtime supplies another ClientId | server reads request ClientId | A03 |
| singular runtime | enrolled key resolves one installation | shared allowlist/member selection | legacy identity fallback restored | A02/A07 |
| immutable bind | same runtime retry | second runtime races bind | CAS/unique guard removed | A05/A08 |
| device possession | enrolled private key | copied DeviceId without key | possession check removed | A04 |
| revocation | Active succeeds | revoke between checkpoints | stale auth/config cache accepted | A09 |
| SDK trust tier | permitted profile succeeds | weak tier requests strong profile | trust compatibility bypassed | A10 |
| bootstrap replay | first redemption returns one identity | replay/wrong audience/concurrent redemption | atomic consume removed | A02/A04/A11 |
| rotation | successor authenticates | old key after atomic cutover | generation fence removed | A04/A09 |
| post-R1 revocation | custody converges | revocation aborts R2-R6 replay | runtime reauthorization inserted post-R1 | A08/A09 |
| capability horizon | bind before boundary | session/capability expires between artifact/evidence/raw or response loss crosses expiry | horizon check removed or blocks internal convergence | A08/A11 |
| challenge compatibility | legacy evidence hash remains identical | capability bytes replace Challenge/hash | version separation removed | A03/A08 |
| public-key verification | canonical public key verifies PoP | thumbprint-only/different key/algorithm/tuple | verifier material/canonicalization removed | A02/A04 |
| Managed evidence role | role-bearing Managed runtime submits verdict | same credential without evidence role or with mixed auth envelopes | role/wrapper separation bypassed | A06/A13/A17 |
| direct request auth | Active installation signs allowed route | revoked/rotated key or caller AgentId selector | fresh lineage/derivation removed | A09/A14 |
| request nonce | first signed request succeeds | concurrent/same nonce replay | durable atomic nonce consume removed | A15 |
| capability replacement race | ActiveUnbound replacement succeeds | bind races replacement | common session lock/CAS removed | A05/A11 |
| bind restart reconciliation | committed bind recovered by same current lineage and IDs | other installation/operation ID, revoked key, unbound or unknown target | exact-lineage/non-enumerating read fence removed | A05/A11/A16 |
| capability handoff | protected IPC/app handoff succeeds | URL/query/log/clipboard/process-arg transport | secret-channel fence removed | A01/A16 |
| observation-to-R1 chain | role-bearing Managed runtime -> verdict wrapper -> acceptance -> R1 | missing role, wrapper bypass, or raw ingress invents/selects acceptance | role or exact-revision join removed | A07/A13/A17 |
| management precedence | authorized valid mutation succeeds | remove auth-before-body/target or expose not-found to unauthenticated caller | endpoint admission order | A12/A14 |
| role-policy cutover | successor with reviewed role works | old generation uses newly assigned role or removed role after rotation | immutable generation policy/current recheck removed | A09/A13/A17 |
| old config route | canonical runtime config works | `/api/ekyc/capture-agents/self/configuration` remains mapped or accepts legacy/mixed auth | route unmap/no-fallback removed | A02/A09 |
| bounded nonce retention | duplicate nonce rejected throughout complete timestamp-valid window; cleanup allowed only after immutable PurgeAfter | delete before PurgeAfter, derive it from cleanup clock, omit request-age/skew, claim nonce before timestamp validation, or accept out-of-window replay | signed-window retention/indexed cleanup removed | A15 |
| verifier pepper | current/overlap version verifies | referenced version absent/retired early/wrong domain | readiness/key-version lookup removed | A11/A16 |
| rotation boundary | valid dual-PoP completion succeeds | authorization expires/revokes between nonce and commit | transaction-local recheck removed | A04/A09 |
| local handoff diagnostic | fresh target/runtime-bound evidence gates bootstrap | stale/replayed evidence, forwarded listener or unsupported host | deployment-local check/attestation binding removed | A02/A16 |
| Agent listener boundary | pending one-shot loopback handoff succeeds and closes | wildcard bind, no slot, duplicate, slow/oversized/malformed body, extra route, port preemption or listener stays open | ephemeral listener/closed DTO limits removed | A16/A19 |
| capability intake ownership | existing SignFlow backend-to-SignAgent delivery is treated as an input; AgentEkyc loopback and SDK in-process intake succeed | API key reaches SignAgent, loopback accepts wrong/duplicate/late input, capability leaks through URL/log/cache, or embedded SDK requires listener | TagEkyc intake boundary removed/conflated or external SignFlow channel incorrectly made a readiness dependency | A01/A16/A19 |
| control publication | valid closed revision publishes atomically | free-form role, incompatible profile/ref, widening override, duplicate current revision or audit omission | closed validator/current CAS/event removed | A09/A12/A18 |

Mandatory audit covers bootstrap issue/redeem/reject, activation, suspension/
reactivation, rotation, revocation/retirement, capability bind/replay/mismatch
and configuration refusal. It records actor/principal, runtime/install/credential
IDs and revisions, operation/result/reason, request/correlation and timestamp;
never private keys, tokens, bootstrap secret, raw capability or raw BIO.

## 14. C6B dependency and STOP gates

C6B remains paused. Its non-identity partial work is preserved. Before C6B may
resume, this slice must produce a reviewed dispatch and landed implementation
that provides:

1. singular authenticated runtime and installation identities;
2. server-owned enrollment/lifecycle and credential verification;
3. session capability resolution to authoritative ClientApplicationId;
4. immutable execution binding;
5. self-bound Agent configuration resolution;
6. a direct, fail-closed cutover from current artifact/API-key producer behavior
   with no dual-read or fallback; unexpected non-fixture population stops migration.

STOP/RRI if implementation would:

- distribute a ClientApplication credential to a runtime;
- make challenge text alone an unbounded/guessable bearer authority;
- change landed VerificationSession Challenge/echo/evidence-hash semantics
  without a separately reviewed versioned migration;
- accept a capability/binding beyond its execution horizon for new work;
- let runtime choose ClientApplication identity;
- let client provision or impersonate runtime identity;
- treat `AllowedCaptureAgentIds`, key prefix, appsettings or artifact request as
  canonical runtime identity;
- fabricate `AuthenticatedClientContext`, retain/route runtime proof into the
  legacy API-key producer wrapper, or expose the neutral evidence core directly;
  the separately authenticated execution-binding runtime wrapper on the shared
  path is expressly permitted for role-bearing Managed runtimes;
- store/receive an installation private key or treat thumbprint as verifier;
- accept runtime requests without installation PoP and durable nonce replay ownership;
- put capability secret in URL/query, process arguments, clipboard or logs;
- let a credential lacking the durable trusted-evidence role assert a verdict or let raw ingress invent an acceptance;
- equate caller-supplied hardware metadata with cryptographic device identity;
- silently require a provider, CA, MDM, attestation vendor or SignFlow runtime;
- expose raw BIO or internal custody data to ClientApplication;
- resume real retained-biometric ingress or production activation.

## 15. Review plan and ledger

At least five and at most ten internal rounds are required before external
CC/GPT review. A clean result after round 5 may close; otherwise the documented
checkpoint governs rounds 6-10.

| Round | Coverage | Findings | Applied delta | Convergence assessment |
| --- | --- | --- | --- | --- |
| 1 | complete artifact; architecture, persistence, security, API and repo feasibility | 3 blocker themes; implementation-blocking capability, bootstrap, lifecycle, credential, config, audit and migration gaps | v0.2 closes transaction/revocation/bootstrap/lifecycle/provenance rules and expands decision aid/tests | not converged: v0.1 described topology but left security transactions to Builder; matrices now include lifecycle/capability invalidation |
| 2 | complete artifact; repo feasibility, security, API and dispatch readiness | 5 blocker themes plus lifecycle/rotation/routing/audit/migration gaps | v0.3 adds challenge coexistence, issuance/horizon, operation classes, rotation outcome, audit atomicity and versioned endpoint topology | not converged: v0.2 improved states but failed to distinguish request continuation from authority and omitted landed Challenge compatibility; reviewer prompts now require endpoint/state transition cross-product |
| 3 | complete artifact; adversarial security, API, persistence and feasibility | 7 blocker themes plus concrete shape/replay/role gaps | v0.4 adds operation×principal and secret-lifecycle matrices; fixes capability recovery, public verifier, role split and cancellation closure | not converged: prose-local patches hid impossible recovery and principal reuse; review now mechanically checks every replay source and landed callable/category |
| 4 | complete artifact; persistence/race, security/secret transport and API/callability | callability gaps for token acquisition and analytic verdict; nonce/replacement races; capability envelope/handoff ambiguity; route/site/config/audit residues | v0.5 adds token exchange, durable PoP nonce, exact capability ID/secret lifecycle, shared session CAS, protected handoff, producer chain, route census and lifecycle lock order | not converged at v0.4: entity matrices did not prove that each credential-consuming operation was callable or that secrets crossed process boundaries safely; round 5 now requires callability, race, secret-flow and bidirectional matrix closure |
| 5 | three complete-artifact reviews; security/identity, persistence/concurrency and API/callability; bidirectional ledgers | consensus: same-key producer was not a separate principal; no plaintext path to fictitious producer; root issuers/management callability, bootstrap/nonce atomicity and secret HTTP controls incomplete | mandatory checkpoint froze patching; whole-repo reuse census performed; v0.6 narrows to Managed-only, direct signed requests, same trusted process analytics, explicit management/enrollment roots, root/transaction/data-flow ledgers | not converged at v0.5 because nouns/audiences replaced process-key-data edges and authority graph did not terminate; method changed to finite root-of-trust, conflict and process/plaintext graphs before round 6 |
| 6 | three complete reviews of v0.6 and repo; checkpoint redesign, persistence/race and exact API callability | evidence boundary could not accept runtime context; nonce commit/cancel transaction, persistence shapes, operator provisioning, key rotation/bootstrap handoff and stale matrices incomplete | v0.7 adds neutral evidence core/runtime wrapper, mutually exclusive auth, two-stage nonce, cancel integration, bounded persistence ledger, complete conflict rows, provisioning/bootstrap/rotation ceremonies and regenerated tests | not converged at v0.6 because checkpoint graphs were not joined to actual method signatures or every competing operation; round 7 requires bidirectional credential→scheme→context→binding→method→transaction trace and cross-ledger noun lint |
| 7 | three complete reviews of v0.7; cross-ledger noun and route/service/transaction trace | route census omitted management family; role/config/routing policy lacked durable owner; evidence DTO/core, loopback boundary, bootstrap/rotation joins, lifecycle TOCTOU and persistence constraints incomplete | v0.8 makes §8.1 canonical, adds all operation/auth rows, versioned policy aggregates, exact generation ownership, direct business-transaction lifecycle checks, cancel/evidence commands, stdin-only bootstrap and honest managed-host boundary | not converged at v0.7 because canonical inventories were duplicated instead of derived and noun joins were incomplete; round 8 uses §8.1 as sole route source plus material/operation join and transaction-owner validation |
| 8 | three complete reviews of v0.8; canonical route/noun/transaction joins | control-policy issuers absent; nonce terminal retention impossible; rotation/control races, role cutover, local readiness observability, per-boundary transaction and outcomes/tests incomplete | v0.9 adds control publication roots/routes, immutable generation roles, nullable terminal-set nonce purge, split rotation races, exact boundary ledger, auth/outcome/proof rows and deployment-local diagnostic attestation | not converged at v0.8 because storage nouns lacked issuer/terminalizer and server readiness claimed local observability; round 9 validates one normalized operation ledger across control/offline/local/server surfaces |
| 9 | three complete reviews of v0.9; authority terminalizer, temporal-state, growth-budget and observable-proof pass | wall-clock capability uniqueness impossible; nonce retention unbounded; local diagnostic/pepper absent from canonical operations; publisher proofs, config assignment/current selection and per-mutation boundaries incomplete | v1.0 persists/materializes expiry under lock, bounds nonce by signed window, adds offline/local/control/config operations, CAS catalogs, exact role map, per-family boundaries and negative proofs | not converged at v0.9 because time-dependent predicates and high-frequency growth were not feasibility-checked; round 10 performs final full review with temporal/index/cardinality and literal route-operation-boundary row reconciliation |
| 10 | three final complete reviews of v1.0; temporal/index/cardinality, literal route-operation-boundary and secret-hop checks | backend→hospital-app capability edge absent; expiry-on-denied commit and installation/current-generation FK mechanics unresolved; nonce Test-Bite stale | hard stop invoked; Homeowner authorized bounded v1.1 correction: external integration edge/STOP, typed committed expiry denial, Pending-insert/current-FK sequence and corrected nonce proof | not clean; no Round 11. v1.1 must receive external CC/GPT review and cannot claim internal PASS |

Round-5 reviewers must mechanically check four ledgers in both directions:

1. every credential-consuming operation has an issuance/renewal path and every
   issued credential has a bounded consumer;
2. every atomic claim names lock/CAS owner, winner, loser and replay result;
3. every secret names generator, one-time exposure, inter-process handoff,
   storage/verifier, redaction and destruction;
4. every proposed route names principal, DTO/source, lifecycle checkpoint,
   mutation owner and compatibility disposition.

Reviewers must report files/surfaces read, all findings with classification and
evidence, plausible risks dismissed, remaining uncertainty and an honest zero.
No round counts if the reviewer reads only a delta, stops at its first finding
or fails to inspect repo-real feasibility.

## 16. Recommended next action

Round 10 invoked the PI-TAG-001 hard stop. The bounded successor corrections did
not invent Round 11. Both independent reviewers passed exact v1.4 bytes after
complete D9 removal. The Homeowner ratified D1-D7, D10-D11 and D13 as
recommended, amended D8/D12 to direct cutover with no legacy population, and
confirmed the capability handoff boundary: SignFlow's existing channel ends at
SignAgent; TagEkyc owns SignAgent-to-AgentEkyc loopback intake and Embedded SDK
uses an in-process call. This existing SignFlow channel is not a TagEkyc
readiness/evidence gate. All planning decisions are closed. The next action is
bounded independent review of v1.7, followed by an exact
implementation dispatch; neither step grants product implementation authority.

No implementation, schema, API, credential, test, configuration, stage, commit
or push action is authorized by this brief.
