# TIP-88C1-C6B-A2 — Windows Managed CaptureAgent implementation dispatch

**Version:** v0.2 — review candidate, not implementation authority  
**Date:** 2026-09-12  
**Scope:** Windows x64 Managed Agent; synthetic/non-patient evidence only  
**Implementation / stage / commit / push / production authority:** NOT GRANTED  
**Predecessor:** `tip_88c1_c6b_a2_windows_agent_profile_dispatch.md` v0.1, preserved unchanged.  
**Server baseline:** `4fafa17a43ad38f46c2e0c69d570f65b8d01abfe`, already pushed.  
**Agent repository:** `D:/Task/Remote Signing/TagEkyc.CaptureAgent`; HEAD `b193f6316e13a82fb2f9f985f68500feae194cfa` plus exact working candidates in §9.

## 0. Authority, precedence and intent

A2 consumes A1; it does not reopen it. Bound A1 Parent:
`tip_88c1_c6b_a1_server_identity_control_dispatch_r27_metadata_v1.md`,
SHA-256 `9BC62E297098A2E06E20DF6B14786F7BDF0DC3008B7F652178119DC01FE2E661`.
Bound Operation Master (OM):
`tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`,
SHA-256 `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`.
Bound A1 as-built:
`tip_88c1_c6b_a1_as_built.md`,
SHA-256 `37D4BBE0AB17000BE85E408DA4260C304B18270E6BD82674E9C2A2B511716D67`.

Planning brief `tip_88c1_c6b_capture_runtime_identity_enrollment_planning_brief.md`
SHA `DACA8141CC68E03AAB9D858C547DBC4FAB7A542E6CAD0F66AB43E37116B0F0D6`
and umbrella `tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md`
SHA `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062`
supply Agent custody/handoff requirements. Their superseded A1 details are not
alternative server authority. In particular umbrella §10.2's old cross-platform
completion prerequisite is superseded by A2 v0.1's recorded Windows x64 ratification
(2026-09-07). No cross-platform/SDK sunset or D9 site-pinning decision is reopened.

Intent: give Host and WPF one real installation identity and one shared execution
intake; consume existing server contracts without distributing Client API keys.
Expected outcome: diagnose/enroll, restart-safe key lineage, CRT1 signing,
configuration refresh, capability bind/reconcile and runtime capture/evidence
client calls. A2 completion is not retained-raw end-to-end completion.

| Decision / branch | Disposition | Consequence |
| --- | --- | --- |
| Windows x64 / CurrentUser CNG | Already ratified | Initial profile only; no TPM or hardware-attestation claim |
| ClientApplication identity | Server-owned business principal | Agent never stores/selects ClientId, platform key, API key or DB credential |
| D8/D12 direct cutover | Already ratified | No producer API-key fallback/dual-read against Activated server |
| Existing SignFlow backend → SignAgent | Input-channel assumption | Not implemented/certified or readiness-gated by TagEkyc |
| SignAgent → AgentEkyc | A2 shared loopback owner | Same-host exclusive-process trust assumption; no HTTP peer-auth claim |
| Embedded SDK | Deferred profile | No listener requirement imported into later in-process profile |
| A3 broker / R1–R6 | Separate implementation | A2 cannot report actual retained source Available by fixture echo |
| A4 activation / deployment / operator launch | Separate | No production secrets, accounts, firewall changes or LOGIN provisioning |
| Purge/legal hold | Separate lifecycle TIP | Real retained-biometric ingress and production activation remain prohibited |

Technical details first pinned here are proposals for this dispatch's review and
ratification, not claims that they were already built or already authorized.
Only this successor and its review record may be edited in the current docs-only turn.

## 1. Repo census — reuse before invention

Evidence locations below are relative to the named repository. Census is code
inspection, not a runtime PASS. Exact consumed Agent hashes are in §9.

| Resource | Actual evidence | Classification / use |
| --- | --- | --- |
| Host/WPF composition | Agent Host/Program.cs:19–43; Ui/CaptureAgentComposition.cs:39–60 | Existing; both currently construct API-key clients. Replace producer wiring, not devices/engines |
| External session submit-only | Core/CaptureAgentOrchestrator.cs:200,516; CaptureAgentPorts.cs | Landed; reuse capture/evidence sequencing and sanitized receipts, not business-client calls |
| Mutable buffer disposal | Core/SensitiveByteBuffer.cs | Landed; reuse ownership/disposal; public backing-array access is not immutable custody |
| Configuration cache/polling | Core/RawExportIngressContracts.cs:37–185 | Untracked working candidate, NOT landed; reuse synchronization/revision/freshness pattern, adapt full A1 response and disabled-state handling |
| Retained capacity/lease | Core/RetainedRawBufferCapacity.cs | Untracked working candidate; retain for A3 seam; no new buffer subsystem |
| HTTP Expect discipline | Client/TagEkycHttpClient.cs:41–48 | Dirty working candidate; keep exact InfiniteTimeSpan handler pin; no raw send activation by A2 |
| Config route/auth | Same file:50–95 | Obsolete six-field lowercase JSON and API-key /capture-agents/self/configuration; cannot reuse unchanged |
| Producer routes | Same file AppendCaptureArtifactAsync / AppendEvidenceResultAsync | Old session capture route and API-key auth; adapt to R24/R25 wrappers |
| Agent identity | Host:55–56; Ui composition:114–115 | Environment/MachineName selectors; replace with enrollment lineage, never hardware identifier |
| Retention activation | Core/CaptureAgentProductionConfigGate.cs ValidateRetentionMode | Explicit RawVault denial remains; A2 must not silently remove production retention fence |
| CNG installation key / CRT1 / enrollment / rotation / listener | Whole Agent src/tests search for CngKey, ECDsaCng, CRT1, ENROLL1, ROTATE1, CandidateKeyId, TcpListener, HttpListener, Kestrel: no implementation matches | ACTUAL_MISSING, not a reuse claim |
| Existing crypto project | Crypto/TagEkyc.CaptureAgent.Crypto.csproj + ActiveAuthenticationVerifier.cs | ICAO active-auth/BouncyCastle, not installation custody; do not repurpose chip verifier |
| Contracts dependency | Core and Client csproj already reference sibling TagEkyc.Contracts | REUSE_AS_IS; no Agent → server Application/Infrastructure/Api dependency |
| A1 server public surface | CaptureRuntimeEndpoints.cs; CaptureRuntimeExecutionEndpoints.cs; CaptureRuntimeRotationEndpoints.cs | Landed at pinned commit; readonly acceptance target |
| Offline bootstrap launcher | tools/TagEkyc.ApiKeyProvisioner source search has no bootstrap-enroll/diagnose orchestration | A4-owned missing launcher, not an existing helper. A2 supplies child CLI and synthetic pipe harness only |
| Build lock state | Crypto/packages.lock.json dirty adds empty net8.0/win-x86 group; Crypto csproj only net8.0 | Existing dirty dependency evidence; do not revert or claim NU1004 resolved without measurement |

There is no claim that WPF currently runs polling or retained upload: declarations
and isolated tests do not establish composition reachability.

## 2. Component and file ownership

Use existing projects; no new assembly is needed. Core owns interfaces, state
machines, mutable secret leases and host-independent orchestration. Client owns
HTTP serialization/signing adaptation and Windows-specific local custody/intake
implementations behind Core ports. Both Host and Ui reference Client already.
Core does not reference Client, Windows UI, ASP.NET or server Application.
Client uses framework CNG and socket APIs, not the ICAO Crypto verifier.
No new NuGet dependency or PRODUCT project reference is proposed. The sole
test-only reference below is not a production dependency. If a framework API is
unavailable at existing target, report the exact API/target evidence and minimal
dependency delta for review; do not add server-layer coupling to make it compile.

Proposed new Core files (all under src/TagEkyc.CaptureAgent.Core):
- CaptureRuntimeAgentPorts.cs: ICaptureRuntimeKeyStore, ICaptureRuntimeJournal,
  ICaptureRuntimeClient, ICapabilityIntake and typed state/lease contracts.
- CaptureRuntimeAgentCoordinator.cs: enrollment/rotation/config/bind orchestration;
  sole transition owner and ICaptureAgentOrchestrator decorator for both RunAsync
  and RunSubmitOnlyAsync. It gates before delegating to existing capture logic.

Proposed new Client files (all under src/TagEkyc.CaptureAgent.Client):
- WindowsCaptureRuntimeKeyStore.cs: create/open/sign/delete CNG handle.
- WindowsCaptureRuntimeJournal.cs: nonsecret atomic local journal.
- CaptureRuntimeWireCodec.cs: sole Agent ENROLL1/ROTATE1/CRT1 serializer;
  mutable bounded secret serialization.
- CaptureRuntimeHttpClient.cs: exact R05/R13/R21/R22/R23/R24/R25 client.
- CaptureCapabilityLoopbackIntake.cs: one-slot HTTP/1.1 receiver.
- CaptureRuntimeAgentCommands.cs: shared diagnose/enroll/rotation command entry.

Proposed new test files, existing tests/TagEkyc.CaptureAgent.Tests project:
- Tip88C1C6BA2KeyCustodyTests.cs
- Tip88C1C6BA2CeremonyTests.cs
- Tip88C1C6BA2WireTests.cs
- Tip88C1C6BA2HandoffTests.cs
- Tip88C1C6BA2CompositionTests.cs

Exact existing mutation set: Client/TagEkycHttpClient.cs (remove legacy producer
methods/wiring after migration); Core/CaptureAgentPorts.cs,
CaptureAgentProductionConfigGate.cs, RawExportIngressContracts.cs,
CaptureAgentOrchestrator.cs; Host/Program.cs; Ui/CaptureAgentComposition.cs;
tests Tip74HttpClientTests.cs, Tip74CaptureAgentHostTests.cs,
Tip75WpfCaptureAppTests.cs, Tip88C1C6BCaptureAgentTests.cs.
All other §9 paths are read-only. No blanket directory or test-suite mutation authority.
No Agent csproj/lock-file change is proposed. In the SERVER repository the only
proposed A2 test mutations are tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj
(add ProjectReference to ../../../TagEkyc.CaptureAgent/src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj)
and new tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs
plus new tools/Invoke-CaptureAgentA2Acceptance.ps1 (synthetic test runner only).
Dependency direction is server TESTS -> Agent.Client -> Core -> server Contracts;
there is no product reverse dependency or reference cycle. Reuse existing
PostgresPersistenceFixture/TestServer; no invented external server-launch framework.
Existing receipt/loggers, engine algorithms, device bridges, packaging and server
files are read-only. CaptureAgentComposition.Orchestrator changes from concrete
CaptureAgentOrchestrator to existing ICaptureAgentOrchestrator and returns the
coordinator decorator. MainWindow.xaml.cs:48–55 remains an unchanged actual caller.
Host uses the same decorated interface. Composition defers native reader/camera
initialization until coordinator admission. Its pre-bind Preview is a gated
IFaceCameraPreview proxy: no StartPreview/frame acquisition before readiness
(MainWindow starts a timer before awaiting Run). Cancel/dispose closes both.
P09 exercises that actual caller order, not merely a registration.

## 3. Key and local state contract

Windows x64, Microsoft Software Key Storage Provider, CurrentUser, P-256,
CngKeyUsages.Signing, CngExportPolicies.None. CandidateKeyId UUIDv4 exists BEFORE
server IDs; key name exactly tagekyc-capture-runtime-<CandidateKeyId-N>.
Public SPKI is 91 bytes; SHA-256 thumbprint 32 bytes. Sign SHA-256, P1363 64 bytes.
Every open proves provider/curve/export policy/thumbprint; never infer readiness
from a key filename. Wrong account, missing/corrupt/exportable/mismatched key
fails closed without making a replacement identity.

Proposal: one local journal per dedicated Windows account and configured server
origin, under LocalApplicationData/TagEkyc/CaptureRuntime/<SHA256(origin-UTF8)-lowerhex>/.
Origin is the configured HTTPS scheme+host+explicit effective port, no path/query/
userinfo; reject origin change for existing lineage. Files are state.json and
same-directory state.next; bounded 32768 bytes, UTF-8, schema 1, closed fields.
Directory/file DACL disables inherited broad access and permits only current
account and SYSTEM; reject reparse points and unexpected ownership.
Lock state.lock using FileShare.None across both processes before key/journal
transition; no independent Host/WPF state writers. Flush(true) staged file then
atomic same-volume replace/move. Crash-injected tests must reopen old or new
complete state, never silently initialize on malformed/truncated state.

Journal fields: SchemaVersion, ServerOrigin, AccountSid, Revision, State,
CandidateKeyId, CaptureAgentId?, DeviceInstallationId?, CredentialId?,
Generation?, PublicVerifierSpki?, PublicKeyThumbprint?, PendingEnrollment?,
PendingRotation?, PendingBind?, SubmissionOperations, CleanupCandidateKeyIds, PayloadSha256.
PayloadSha256 covers exact payload bytes excluding checksum field; detects
corruption only, not malicious same-account rollback. ACL/exclusive-account
assumption and server lineage checks remain authority; local checksum is not
attestation. Nullable IDs must be all absent before enrollment, all nonempty
after confirmed receipt. State = Reserved / Candidate / EnrollmentPending / Active /
RotationPending / BindPending / CleanupRequired. Private keys, bootstrap/
capability secret, BIO, Client credentials and JSON containing secrets NEVER persist.

Flush Reserved with CandidateKeyId/locator BEFORE CNG creation. Only Reserved
allows absent SPKI/thumbprint. After creating, verify key and flush Candidate+
public material before network. Recovery of Reserved opens the same key if
present; creates that locator only if absent and no ceremony could have started.
Missing key in Candidate/EnrollmentPending/Active/RotationPending never regenerates.
Successor and diagnostic keys use the same reserve/create/cleanup rule, so crash
between create and public-material persistence does not lose the exact locator.

| Material | Persisted nonsecret recovery data | Erase / fail rule |
| --- | --- | --- |
| Enrollment | issuance/redeem operation ID, candidate locator, exact signed timestamp/nonce/proof, public verifier and deterministic JSON field order | Secret only in leased stdin/request bytes; clear all copies on ACK/exit. Ambiguous result keeps candidate, never generates another |
| Rotation | RotationId, operation ID, predecessor generation, successor CandidateKeyId/SPKI/thumbprint, exact ROTATE1 proof/body bytes, attempted branch | Keep BOTH keys during timeout/5xx/ambiguity; delete predecessor only after confirmed successor receipt flushed locally |
| Bind | capability ID, BindOperationId, nonsecret local session ref and verification-session observation, exact confirmed BindingId/horizon if known | No secret on disk; erase all secret-bearing request/lease buffers immediately after confirmed R21 or terminal R22, BEFORE capture; restart uses R22 only |
| Config | in-memory document + opaque ETag only | Clear on identity/origin change, revoke, malformed or expiry; retain valid disabled document+ETag, deny retention; 304 cannot extend expiry |
| Abandoned key | exact generated CandidateKeyId in cleanup queue | Delete only proven noncommitted candidate or explicitly authorized offline cleanup; no prefix-wide sweep |

Enrollment request template is nonsecret; reconstruct with the same re-delivered
secret, timestamp, nonce, proof and field order to reproduce exact request bytes.
If the parent cannot redeliver after process loss, report RECOVERY_REQUIRED; do
not pretend there is a server enrollment-reconcile endpoint. Live response-loss
retries retain exact bytes in memory. R05 exact-body fingerprint makes newly
signing the same body with fresh timestamp/nonce/proof a conflict, not a recovery.
A 403 alone is not evidence that no server commit happened: never use it to delete
an ambiguous key. Re-enrollment requires explicit operator resolution.

Rotation sends the exact frozen successor proof/body and same Idempotency-Key
for predecessor completion and successor replay. Only outer CRT1 timestamp/nonce/
signature changes. Stable CredentialId, predecessor G, successor G+1. No config
probe/polling, no new reconcile endpoint and no predecessor-first auth fallback.
After ambiguous completion, a separate recovery attempt uses successor CRT1
against the SAME route/body; it is not a second auth attempt in one request.
Unresolved/denied recovery retains keys and reports RECOVERY_REQUIRED.

## 4. Exact server operation ledger

Rxx denotes frozen OM row, not a new SQL owner. Agent owns no DB transaction.
All runtime HTTP requests use configured HTTPS origin, no redirects/cookies/
proxy fallback or generic automatic retry handler. Synthetic local harness may
use explicitly loopback HTTP; this does not allow remote cleartext API transport.
Do not confuse server API transport with deferred A3 private broker mTLS.

| ID | Method + path | Request / auth / role | Response / owner / proof |
| --- | --- | --- | --- |
| E / R05 | POST /api/ekyc/capture-runtime/enrollments/redeem | CaptureRuntimeEnrollmentRedeemRequest, ENROLL1 only, Idempotency-Key=RedeemOperationId | 201/new or 200/replay CaptureRuntimeEnrollmentResponse; coordinator journals exact lineage; P02 |
| T / R13a,b | POST /api/ekyc/capture-runtime/credential-rotations/{rotationId:N}/complete | CaptureRuntimeRotationCompleteRequest + Idempotency-Key; predecessor or successor CRT1; server alone classifies | 200 CaptureRuntimeRotationCompletionResponse, exact same CredentialId and G+1; P03 |
| B / R21 | POST /api/ekyc/capture-runtime/executions/bind | CaptureRuntimeBindRequest; CRT1 Bind; header Idempotency-Key equals BindOperationId | 201/new or 200/replay CaptureRuntimeBindingResponse; P05 |
| Q / R22 | POST /api/ekyc/capture-runtime/executions/reconcile | CaptureRuntimeReconcileRequest; CRT1 Bind; NO Idempotency-Key | 200 binding/horizon, no secret; P05 |
| C / R23a,b | GET /api/ekyc/capture-runtime/self/configuration | CRT1 Configuration; bodyless; optional If-None-Match; NO Idempotency-Key | 200 full PascalCase configuration or 304; P04 |
| O / R24 | POST /api/ekyc/capture-runtime/executions/{bindingId:N}/capture-artifacts | CaptureRuntimeCaptureArtifactRequest; CRT1 CaptureObservation; Idempotency-Key; route/body BindingId equal | existing CaptureArtifactSubmissionResponseDto; P06 |
| V / R25 | POST /api/ekyc/verification-sessions/{sessionId:N}/evidence-results | CaptureRuntimeEvidenceResultRequest; CRT1 TrustedEvidence; Idempotency-Key; BindingId in body | existing EvidenceResultSubmissionResponseDto; P06 |

Contract fields come directly from pinned server Contracts/CaptureRuntime
EnrollmentContracts, ManagementContracts, ExecutionContracts, not a second DTO
catalogue. Producer wrapper payload deliberately has no caller-chosen producer
or device identity. Existing engine verdicts/decision bases are mapped fieldwise,
not JSON extension bags; business result/complete/session-create calls are not
runtime capabilities. A2 does not call R20/R26/C5, provision operator credentials,
or claim the Agent can download arbitrary raw resources.

Five exact headers:
X-TagEkyc-Capture-Runtime-Credential-Id,
X-TagEkyc-Capture-Runtime-Credential-Generation,
X-TagEkyc-Capture-Runtime-Timestamp,
X-TagEkyc-Capture-Runtime-Nonce,
X-TagEkyc-Capture-Runtime-Signature.
CRT1 is UTF-8 of eleven lines:
TAG-EKYC-CRT1; uppercase method; exact path; CredentialId-N; generation decimal;
timestamp yyyy-MM-ddTHH:mm:ss.fffffffZ; canonical 32-byte nonce base64url;
media type; length decimal; lowercase SHA256 body; operation-binding;
separated by LF with exactly one final LF. Empty lines remain empty.
Serialize closed JSON once to mutable bounded bytes, compute digest, sign then
send those SAME bytes; never sign one serialization and send JsonContent's other
serialization. Maximum request 16384 bytes, response 65536; reject duplicate/
unknown/null-forbidden fields, redirects, malformed or contradictory success.
Use OM U/T/S32/SPKI91/SIG64 encodings and matching endpoint ClosedJson, not
generic Web camelCase defaults for A1 DTOs. R24/R25 RESPONSES are the explicit
exception: existing CaptureArtifactSubmissionResponseDto/EvidenceResultSubmissionResponseDto
retain host Web camelCase/converters; landed Respond<T> selects by namespace.
Errors remain lowercase code/correlationId. P06 decodes real server responses.
Secrets use mutable byte parsing/writing, not
the string-valued server DTO as an Agent secret holder.

Operation binding: E has ENROLL1 not CRT1; T/C empty; B exactly
CaptureCapabilityId=<N>;BindOperationId=<N>;SecretSha256=<lowerhex>;
Q exactly CaptureCapabilityId=<N>;BindOperationId=<N>;
O/V exactly BindingId=<N>;IdempotencyKey=<N>.
ENROLL1 and ROTATE1 bytes are lifted field-for-field from landed
CaptureRuntimeEnrollmentApplicationService.RedeemAsync and
CaptureRuntimeRotationApplicationService.CompleteRotationAsync. Client proof
must be accepted by the real server verifier, not a test-local echo serializer.

R27 signing/transport remains an A3 integration seam: do not use existing API-key
SubmitRawExportSourceAsync against Activated A1. Preserve InfiniteTimeSpan,
bounded memory-only preadmission, no application body ownership before committed
R1, no resume/init/multipart API, no new outcome. A2 cannot turn missing A3 into
Available or lift RawVault denial to make its own completion green.

## 5. Configuration, capture context and ordering

Consume all 15 PascalCase fields of CaptureRuntimeConfigurationResponse exactly
as SerializeConfiguration writes them; reject a six-field predecessor response.
Validate CaptureAgentId against enrollment, positive configuration ID/revision,
EffectiveAt <= now < ExpiresAt, sane positive limits, and:
PollingIntervalSeconds + SafetyMarginMilliseconds/1000 < ExpiresAtUtc - now.
Use server polling interval, not previous environment default. Initial GET is
mandatory before starting periodic cadence. ETag stays opaque, never substitutes
for business revision. 304 requires an existing same-lineage document and full
freshness recheck; it never advances EffectiveAt/ExpiresAt. Reconnect fetch runs
before next retained capture. Refresh failure invalidates retained eligibility,
not a successful grant to continue indefinitely.

RawExportEnabled=false is a valid configuration, not a malformed document:
block new retained leases, dispose pre-R1 leases, preserve VerifyAndDiscard
where otherwise authorized; never retroactively modify committed server custody.
Reuse cache/lease logic with these corrections, not its current throw-on-disabled
behavior. A2 does not invent missing capacity values from a server maximum.

Order for both Host/WPF: validate OS/account/origin → open exact enrolled key and
journal → fetch current config → freeze/validate existing capture observations
and consent → open one slot → validate envelope → allocate BindOperationId and
flush nonsecret BindPending → flush ACK → R21 or restart R22 → bind success
and horizon → existing submit-only orchestrator with runtime client → cleanup.
No device raw acquisition before execution readiness; tests inject synthetic
device/engine data only, never access a patient's card/camera.

Existing ExternalSession observations include SessionChallenge, subject/purpose,
consent and required checks. A capability is authorization, NOT that full capture
context. Do not fabricate challenge from capability secret/ID or ClientId.
Use the existing CaptureAgentRuntimeConfig.FromEnvironment carrier:
ExternalSessionId/ExternalSessionChallenge plus subject/consent/check observations;
Host/WPF BuildExternalSession already constructs Tip71SessionContext. Snapshot
RunConfiguration before slot open, validate existing consent rules, require
handoff VerificationSessionId equals that observed session UUID. Preserve challenge;
replace local CaptureAgentId/DeviceId with enrolled CaptureAgentId/DeviceInstallationId,
never environment/MachineName. Missing/mismatch fails before capture. Server
binding remains session authority; no new input carrier, read API or ClientId.

Coordinator owns SubmissionOperations[(BindingId,submission-slot)]. Allocate
UUIDv4 once before first append; never forward current RunId|submissionSlot
(orchestrator:617–621) to R24/R25. Journal nonsecret operation UUID/body SHA;
live retries use exact same serialized body/UUID with fresh CRT1. If restart
loses exact payload bytes, report SUBMISSION_STATE_UNKNOWN, never generate a
new operation or different replay body. P06 checks legacy-key rejection and
same-key/body retry. Retain at most one execution and its six existing submission
slots (three artifacts, three evidence results), not an unbounded history.
Keep uncertain entries through restart/reconciliation. Release a completed
execution's entries only after all append results and existing submission
receipt write are acknowledged; Host/Ui composition reports receipt completion
to coordinator. On expired execution horizon, retain one nonsecret terminal
status, stop replay/capture, and compact payload-free slot identities before a
new explicitly requested execution. Never evict unresolved entries to admit a
second execution. Journal size/count overflow is NOT_READY before allocation/
capture and leaves prior bytes untouched; cleanup queue maximum32 known locators,
full queue denies new key creation. No prefix sweep. P10 must run sequential
executions past the original 32768-byte growth threshold, and prove incomplete
receipt/uncertain retry entries survive without silent eviction.

On bind success, zero capability lease, serialized bind body and scratch buffers
BEFORE delegated capture/evidence work. Any terminal failure/expiry/cancel does
the same. A live ambiguous R21 retry may retain memory only until the original
capability expiry, never extend it; restart has no secret and uses R22. P12's
positive bind path must inspect cleared backing storage before the fake device
is permitted to run. Cleanup at the end of capture is not the secret terminalizer.

## 6. TagEkyc-owned intake and CLI contracts

One shared listener implementation, not separate Host/WPF sockets. Explicit
OpenSlotAsync / CloseSlotAsync. Process ownership follows dedicated account;
cross-process file lock serializes Host/WPF and port ownership. One pending
slot; second open returns BUSY. Mandatory TAGEKYC_CAPABILITY_HANDOFF_PORT decimal
1024..65535; no dynamic port/default; bind BOTH 127.0.0.1 and ::1 exclusively.
Both binds must succeed before readiness; partial failure closes both.
No slot = no bound socket. Literal Host including configured port and loopback
remote endpoint only; reject wildcard/forwarded/absolute-form/proxy/extra routes.
No HTTP peer authentication or site-A/site-B claim.

Only POST /tag-ekyc/capture-capabilities/accept, HTTP/1.1, application/json,
Content-Length <=4096, header bytes <=8192, no Transfer-Encoding/Content-Encoding/
Expect/upgrade/pipelining. Header/body absolute timeout 5s (no sliding reset);
slot timeout 120s or envelope expiry, whichever earlier.
Closed UTF-8 JSON: SchemaVersion=1, SignFlowLocalSessionRef (nonempty <=128 UTF-8
bytes), VerificationSessionId UUID-N, CaptureCapabilityId UUID-N,
CaptureCapabilitySecret canonical S32, ExpiresAtUtc T.
No unknown/duplicate/escaped secret/invalid UTF-8/trailing document. First valid
envelope atomically binds the empty slot. Allocate and durably flush its nonsecret
BindOperationId/capability/session IDs BEFORE consumer publication or ACK. No R21
request may start before successful ACK flush. All app-owned buffers including
raw HTTP staging clear on failure. P05/P08 inject crashes on both sides of flush.
Return HTTP200 Cache-Control:no-store, Connection:close, body {"accepted":true};
flush before dispose. Receipt ACK only confirms intake, NOT server binding.
If ACK fails, retain only nonsecret recovery identity and invalidate/zero secret;
the client must not assume bind success. Duplicate/late requests cannot bind
again. Terminal parse error/timeout/cancel/shutdown closes slot and listener.

CLI proposed closed forms: capture-agent diagnose --handoff --json;
capture-agent enroll --stdin; capture-agent rotate --stdin;
capture-agent rotate --resume. Dispatch these commands BEFORE existing capture
config validation demands external session, consent or legacy producer keys.
They validate only their own environment/lineage prerequisites. Never accept
secret in argv/env/files/clipboard. Rotation stdin is NONSECRET closed schema1:
RotationId, CaptureAgentId, DeviceInstallationId, CredentialId, CurrentGeneration,
ExpiresAtUtc. It comes from the already authorized operator rotation result;
Agent does not issue/read authorization or receive an operator key. Validate all
IDs/generation against journal and expiry against now. Allocate/freeze completion
operation UUID once with successor locator before send. --resume consumes that
pending journal only and uses successor CRT1 once, same body/operation; no probe
or fallback. Mismatched/new envelope while pending yields RECOVERY_REQUIRED.
diagnose does not capture BIO or enroll: temporary exclusive dual-loopback bind,
CNG create/open/sign/export-denied/delete probe under a unique TEST locator,
configured binary SHA and account/provider/port evidence. It must report measured
vs unverified host assumptions separately; a bind probe does not prove firewall/
process isolation. A4 owns collection of deployment evidence and bootstrap issue.

Diagnostic is a closed PascalCase object: SchemaVersion=1, Profile=WindowsX64,
ServerOrigin, AccountSid, ExecutableSha256 (lowercase hex of the running Host
assembly file), CngProvider, Ipv4Loopback=true, Ipv6Loopback=true, Port,
MeasuredAtUtc=T, DiagnosticNonce=S32 (nonsecret),
ExclusiveHostAssumption=RequiredNotVerified. Serialize in that order as compact
UTF-8, no BOM/final newline, then EvidenceDigest=lowerhex SHA256 of those exact
bytes. Output {Diagnostic:<object>,EvidenceDigest:<hex>}; the digest is not inside
its own preimage. Diagnostic is at most 2048 bytes; string escaping is canonical
Utf8JsonWriter default, no indentation. Input accepts only exact reserialization.
No claim that CNG or temporary binds prove firewall/process isolation. A4 owns
review of those deployment assumptions and binds this digest to its issuance.

Enrollment stdin proposed schema1 envelope (<=4096, bounded mutable parser):
SchemaVersion, BootstrapIssuanceId, BootstrapSecret, ExpiresAtUtc,
HandoffAttestationDigest, RedeemOperationId, Diagnostic (the above object).
Child recomputes digest, checks exact origin/account/binary/provider/port binding,
and checks MeasuredAtUtc no more than 120s old / 30s ahead on INITIAL enrollment
plus bootstrap ExpiresAtUtc. Existing pending exact replay keeps original
diagnostic/body; mutable freshness must not force a different business request.
Parent may redeliver same envelope to a restarted child only while its original
memory lease exists. Parent loss means RECOVERY_REQUIRED, not secret recovery
via A1 issuance replay. This is the A2 child contract for future A4 consumption,
not a claim that A4's launcher exists. R05 carries no attestation field: these
local checks do NOT add it to ENROLL1 or claim server verification of the preimage.
Server origin comes from validated local nonsecret config,
not bearer input. ACK is emitted only after durable nonsecret lineage journal;
closed stdout fields: SchemaVersion, Status, CaptureAgentId,
DeviceInstallationId, CredentialId, Generation. Failure stdout carries only
SchemaVersion, Status; stderr never echoes input/exception/secret. Parent/child
clear on ACK/exit. stdin terminates at EOF; total read deadline 5s, no multi-record
framing, duplicate/unknown fields or trailing bytes. Exit 0 only with durable
success ACK; exit 2 invalid config/input; exit 3 unresolved recovery/dependency;
exit 4 definitive rejection. All outputs are bounded to 4096 bytes, no stack trace.
A fresh bootstrap/candidate is never automatically issued by these commands.

## 7. Outcome, shape and concurrency ledger

| Competing condition / order | Result / permissible residue |
| --- | --- |
| Invalid OS/account/key/local journal before network | NOT_READY; no replacement identity, no capture, no request |
| Foreign credential/header or malformed server body | Fail locally; no fallback, no secret-bearing diagnostic |
| HTTP400/403/404/409 | Preserve exact server code, no local rewrite into success; ambiguous prior ceremony remains recoverable |
| HTTP503/timeout/disconnect | Retry only named operation with same business identity/body; new CRT1 nonce, no generic handler; preserve ambiguous key state |
| R05/R13 response nonempty but inconsistent IDs/generation/thumbprint | NOT_READY, no local Active transition/deletion |
| Config refresh/expiry vs start capture | Serialized eligibility check before admission; disabled/expired wins for new retained lease |
| Host vs WPF open / duplicate intake | Same process/account lock and single slot; exactly one consumer; loser BUSY/no material |
| Rotation vs signing/capture | Serialize journal/key selection; existing signature may finish, new requests use only durably confirmed generation; server owns revocation order |
| Bind ACK loss / restart | R22 with stored nonsecret IDs; no persisted secret and no regenerated BindOperationId |
| Successful bind with expired horizon | No capture; erase secret; retained metadata never extends server horizon |
| Cancel/shutdown vs intake completion | One terminalizer; no pending secret retained after teardown; no socket left listening |

Local diagnostic codes are local statuses only: NOT_READY, BUSY,
RECOVERY_REQUIRED, CANCELLED. They add no server outcome or raw-ingress code.
Ordinary server errors are code+correlationId only. No A2 error branch invents
server operation/event/nonce history.

## 8. Invariant / Test-Bite ledger

All tests below are planned, NOT executed evidence. Method name is exact suffix
inside the corresponding new A2 test class; each must have positive control and
the stated mechanism mutation that makes it RED.

| Proof | Owner / invariant / observable result | Negative / mutation |
| --- | --- | --- |
| P01 KeyCustodyTests.Cng_CurrentUserNonExportable_ReopensExactKey | Real Windows CNG, reopen after process restart, sign accepted by independent verifier | exportable key, wrong account/provider/thumbprint; regeneration after missing locator |
| P02 CeremonyTests.Enroll_LostResponse_ReplaysExactBodyAndKeepsCandidate | Real R05 HTTP/PG fixture returns same IDs; pipe ACK only after journal flush | refresh timestamp/proof/order on retry, erase ambiguous candidate, secret in journal |
| P03 CeremonyTests.Rotate_SuccessorReplay_RetainsExactBody | Real R13, G then G+1 CRT1, same durable completion, stable CredentialId | regenerate ROTATE1 signature/body, config probe, two auth attempts, early predecessor delete |
| P04 WireTests.Configuration_FullShapeFreshnessAndIdentity | Real serialized 15-field response + 304, monotonic revision, disabled accepted as state | six-field/camelCase, wrong Agent, 304 extends horizon, cached grant after failure |
| P05 CeremonyTests.Bind_RestartReconcile_UsesNonsecretJournal | R21 then restart/R22; no secret, same BindingId or denial with no new bind | new operation ID, secret on disk, capture before binding/horizon |
| P06 WireTests.RuntimeAppend_UsesBindingAndNoApiKey | Recorded exact request accepted by A1 R24/R25; no environment identity in payload | old route, API-key fallback, wrong BindingId, domain decision-base dropped |
| P07 WireTests.Crt1_ExactBytes_ServerVerifierAccepts | Real signed bytes verified by pinned A1 parser/auth; all eleven lines exercised | each field/final-LF/body serialization changed, alias nonce, foreign header |
| P08 HandoffTests.DualLoopback_OneSlot_BoundedAndZeroed | Real sockets IPv4/IPv6, ACK flush, both disposed; max-size positive control | wildcard, duplicate, port preemption, slowloris, extra path/header/body, secret string/log/cache |
| P09 CompositionTests.HostAndWpf_ReachOneCoordinator | Actual CLI launch and WPF composition STA harness reach same ports; cancellation drains owners | dead registration, separate listener/key owner, device start before bind |
| P10 KeyCustodyTests.Journal_CrashAtomicAndWrongAccountFails | Process fault before/after flush/replace, recover full old/new state; audit allocated keys | partial state accepted, reparse point, generic prefix cleanup, checksum called tamper-proof |
| P11 CompositionTests.NoClientSecretOrProductionActivation | Config gate/client factory/log/receipt negative tests | API key environment activates Managed; RawVault or real broker/prod enabled |
| P12 CeremonyTests.SecretLeaks_AllTerminalPathsClear | Synthetic sentinel bytes absent from captured stdout/log/disk, buffers zero | canceled/error path skips clearing or ACK emitted before persistence |

Real A1 HTTP/PG ceremony fixtures run under isolated synthetic test authority,
not production activation. Server Tip88C1C6BA2ClientServerAcceptanceTests owns
P02/P03/P05/P06/P07 real-boundary acceptance; Agent test classes own local and fault
tests. Server methods use the same exact suffixes named in §8, including
RuntimeAppend_UsesBindingAndNoApiKey. The new server test file reuses PostgresPersistenceFixture,
CreateDisposableCurrentDatabaseAsync and the actual TestServer/DI pattern in
Tip88C1C6BA1ExecutionHttpTests, adding existing enrollment/rotation services.
Use real gateway/authenticator/CNG client, not mocked successful service outputs.
Only fake device data and synthetic configured peppers are allowed. No A3 body
behavior is required to exercise these route-local A1 surfaces.
Fixture has NO cross-process mutex; its normal InitializeAsync/DisposeAsync own
shared Compose up/down and must NOT be used by the new A2 class. Runner
tools/Invoke-CaptureAgentA2Acceptance.ps1 owns a new named mutex
`Global\TagEkycA2Acceptance` for the whole child test-host lifetime and a unique
tagekyc-a2-<UUID-N> container using existing postgres:16 image, bound only to an
ephemeral loopback port. Label with exact run ID; use generated synthetic DB
credential only in child environment, never output/report. Runner exports
TAGEKYC_A2_TEST_CONNECTION_STRING and TAGEKYC_A2_TEST_RUN_ID to its child only.
Missing/nonloopback/nonmatching container identity fails before database access.
The A2 test class uses IAsyncLifetime and the existing INTERNAL
PostgresPersistenceFixture(explicitConnection) constructor, not the shared
PostgresPersistenceCollection. Its own Initialize calls ResetDatabaseAsync only
on that dedicated template; cloned disposable databases use the existing helper.
The class never calls fixture InitializeAsync/DisposeAsync (shared Compose).
Runner finally verifies exact container label/name, removes only its container,
clears child env/material and releases mutex. No ambient/shared/SignFlow/prod DB.
Command from server repo on Windows x64: pwsh -File
tools/Invoke-CaptureAgentA2Acceptance.ps1. Runner executes exactly: dotnet test
tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj --no-restore
--filter FullyQualifiedName~Tip88C1C6BA2ClientServerAcceptanceTests
--logger trx --results-directory TestResults/a2-acceptance.
Also run server ArchTests and both repo builds to prove test-only dependency
direction. Fake transport tests complement these real boundary proofs.
No full-suite claim until the actual Agent suite runs.

## 9. Exact consumed-source freeze

Paths below are Agent-repository relative. Empty Git status was measured as
landed at b193f631; dirty/untracked bytes are intentionally not called landed.
These hashes are a preimplementation rebind gate, not a blanket mutation list.

| Path | Current raw SHA-256 | State |
| --- | --- | --- |
| `src/TagEkyc.CaptureAgent.Client/TagEkycHttpClient.cs` | `B56D118E5B501E8140140D3F0EA57B0F94ED38E328497BA0E80E8B07613966DD` | Working candidate; M |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs` | `97A479CDF490261236F62627EE19141643866E1BD5A0C9D0CDCF0C463825C405` | Working candidate; M |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs` | `88B10A286F698773092F035DA5E2D52BE3AB32EFC872B926BD0C486DA2B23B9B` | Working candidate; M |
| `src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs` | `13D8356C07A758428AB41B62DAF2D135BD01588D907ED948B5BD6E02F80F2392` | Working candidate; ?? |
| `src/TagEkyc.CaptureAgent.Core/RetainedRawBufferCapacity.cs` | `8200232C63A6DCB6DEFF4E60F8427B7E8D94E18663A360DCF795CD209D1DA60A` | Working candidate; ?? |
| `src/TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs` | `B75862BE5AEF0DD7464E66D81E06AF6927233B2B5EF3EF984604B08F73616293` | Landed |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs` | `22782D9EF65DB311136083FF620AF46BF3AF7E5FA4A5EAC5827C182D0084AB55` | Landed |
| `src/TagEkyc.CaptureAgent.Host/Program.cs` | `A2F2CE79B55F1F28E7FA49B456B65372152DCB451586C96A43FBBF0DAA7C7174` | Working candidate; M |
| `src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs` | `A2EB75A697DDFCB34077EC6EFEB5C3D380C4066D2013E1412980774272309B0F` | Working candidate; M |
| `src/TagEkyc.CaptureAgent.Ui/CaptureAgentViewModel.cs` | `6BC53EFB94ADE973684C0CA984DC045064B2E6CF5B117C43F7CE93FCBF0A7185` | Landed |
| `src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj` | `D88A723B4061DF4CD9532818E0A3901C6611BE7E7EAAC68C04AD7EE3F611E5FC` | Landed |
| `src/TagEkyc.CaptureAgent.Core/TagEkyc.CaptureAgent.Core.csproj` | `B6BBBE1099C352C044040993D09DC0F004057307129E27D67642077B772BF890` | Landed |
| `src/TagEkyc.CaptureAgent.Host/TagEkyc.CaptureAgent.Host.csproj` | `DB39A36063784AE5A36CDE8B90DF2E3B65B68F847A68D2DA0CF9500F7DC5D6C1` | Landed |
| `src/TagEkyc.CaptureAgent.Ui/TagEkyc.CaptureAgent.Ui.csproj` | `B1A974DAAEF7B1C490FCEECF8742CC2A09AA6B31256393C955E23E3E80B73478` | Landed |
| `tests/TagEkyc.CaptureAgent.Tests/TagEkyc.CaptureAgent.Tests.csproj` | `37C954258F96A684144F7DD4EB4F1515963109FB135AF0AD9ECA7DBA1C6E8ADB` | Landed |
| `src/TagEkyc.CaptureAgent.Crypto/TagEkyc.CaptureAgent.Crypto.csproj` | `52FC4CAB3907943A878E47274A994C9A79327BB185C7A91B5F266B2BADEEC4FF` | Landed |
| `src/TagEkyc.CaptureAgent.Crypto/packages.lock.json` | `FBC4F5CEA631D4DB165CE47562A59BC1618C97CFF3C0845A55C7EA531BEB54C2` | Working candidate; M |
| `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BCaptureAgentTests.cs` | `AE16804ADE1EB3B70FE15066424B84B765CA8F1FE84069CEAC828400F21A18F3` | Working candidate; ?? |
| `tests/TagEkyc.CaptureAgent.Tests/Tip74HttpClientTests.cs` | `8386970AD78CB0B93001E8E19CEC395F3ACE92AD85861E282989283CCA29AAA9` | Landed |
| `tests/TagEkyc.CaptureAgent.Tests/Tip74CaptureAgentHostTests.cs` | `BCDDA84A5B97C3048C4CCD606E45072C43DABCDE45CDE9D890FA8590869B769A` | Landed |
| `tests/TagEkyc.CaptureAgent.Tests/Tip75WpfCaptureAppTests.cs` | `EB554F7486233CBA8ED729E370E676EDD6115A858639F4C6FAC59919F6EED6EA` | Landed |
| `tests/TagEkyc.CaptureAgent.Tests/Tip85CaptureAgentPackagingTests.cs` | `0E8A09C674FAE72CE3766B8D164202A485B4FCF412D2070FEA97D76F8FDA983A` | Landed |
| `src/TagEkyc.CaptureAgent.Ui/CaptureAgentRunLogger.cs` | `C033FA5B49FFAB9E5F02AD9BA1F198B4CD47F750DC6D1B269DF6CD9E5011C624` | Landed |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentReceiptWriter.cs` | `5A2225730E480FDC8079CC4BF60079162915694CC9412C507C8A9E618A331CD3` | Landed |

Additional consumed caller: Agent src/TagEkyc.CaptureAgent.Ui/MainWindow.xaml.cs
SHA D40898C259D4B80089C6E9D45FE4A3D7A88A6F13BF87F45EBFE7EBAE066929BD,
read-only. Proposed server test-project mutation baseline:
tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj SHA
A6C6650796EF85BA40E89E8D8074A4ECB30E725BD9618B1B6F08E4061BFBCCEF.

Server PRODUCT source dependencies are all readonly at commit 4fafa17, including
Contracts/CaptureRuntime/*.cs, CaptureRuntimeEndpoints.cs,
CaptureRuntimeExecutionEndpoints.cs, CaptureRuntimeRotationEndpoints.cs,
CaptureRuntimeEnrollmentApplicationService.cs, CaptureRuntimeRotationApplicationService.cs
and CaptureRuntimeExecutionApplicationService.cs. Exact commit blob identity,
not a working-tree prefix/hash guess, is the source freeze. Drift must be reported
with changed contract and affected proof, not silently rehashed.

## 10. Implementation order and completion gate

After exact dispatch authority only:
1. Verify both repositories/frozen candidates and §11 closures, run locked restore
   diagnostic and record exact baseline. Do not overwrite dirty lock file.
2. Core ports + key/journal + real CNG tests. No network producer activation.
3. Wire codec and E/R13 client + exact replay tests.
4. Config + bind/reconcile + intake/commands and leak tests.
5. Host/WPF wiring + R24/R25 adaptation, no legacy producer fallback.
6. Full Agent Windows x64 build/tests plus exact A1 acceptance fixture; both
   compositions and packaging proof remain green; report skips and changed counts.
7. As-built source hashes, requirement/proof/RED mapping and deferred A3/A4 seams.
   No commit/push without separate authority.

Planned commands: dotnet restore TagEkyc.CaptureAgent.sln --locked-mode;
dotnet build TagEkyc.CaptureAgent.sln --no-restore;
dotnet test tests/TagEkyc.CaptureAgent.Tests/TagEkyc.CaptureAgent.Tests.csproj
--no-restore --logger trx. These are NOT run in this docs-only turn.
The old NU1004 report is historical evidence, not a current measured result.
No product/source/schema/test/config/project/lock mutation is performed here.

## 11. Reconciled execution seams

| Seam | Repo evidence | Technical disposition |
| --- | --- | --- |
| G1 Capture execution context | Existing RuntimeConfig environment observations + Host/WPF BuildExternalSession + consent gate | Reuse with pre-slot snapshot/session match and enrolled identity replacement, §5; no new carrier |
| G2 Diagnose/enroll parent-child contract | A4 launcher not landed; umbrella §10.5 fields/pipe ownership | Exact child schema/digest/ACK/recovery pinned §6; A4 consumes later, no A4 code authority |
| G3 Cross-repo real-server proof | Existing server IntegrationTests/Postgres/TestServer | Inverse test-only reference and new exact test file, §§2/8; no Agent→server implementation dependency |

These source-traced technical dispositions do not reopen D1–D13. Review verifies
them before exact dispatch ratification. Implementation authority remains NOT
GRANTED; source facts or scope-changing contradictions must not be silently
reinterpreted. Ordinary build/test fixes within the eventual allowlist are not
reasons to invent another planning round.

## 12. Review protocol and changelog

PI-TAG-001 PILOT: TIP-88C1, High-risk. Selected modules API, crypto/key,
raw/restricted-data, governance. SQL module N/A for product mutation (server
acceptance fixture only); worker module N/A (in-process finite polling task,
not a distributed queue). Invariant/Test-Bite §8; outcome/shape §4–7;
ordering §§3,5,6. Independent review required. Round-5 root-cause checkpoint
enabled; round-10 hard stop enabled. From round3, cumulative nonconvergence
analysis must distinguish missing source fact, draft contradiction and scope.
No reviewer quota: read complete candidate, adjacent sources and actual callers.
Do not count census as a review. Record all actionable findings and invalidated
rows; do not confuse dispatch review PASS with implemented proof or authority.

Changelog v0.2: replaced 35-line skeleton with current two-repo census, exact
A1 binding, existing dirty-source freeze, component/file ownership, ceremony/
secret timelines, current route/shape ledger, proof plan and explicit missing
joins. No claim that old API-key/config surfaces are reusable unchanged.
