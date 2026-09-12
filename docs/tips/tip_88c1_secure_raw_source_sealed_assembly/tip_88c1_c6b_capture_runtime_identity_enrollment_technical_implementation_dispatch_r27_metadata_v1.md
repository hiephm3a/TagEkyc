# TIP-88C1-C6B-A — Capture Runtime Identity and Enrollment Technical Implementation Dispatch

> R27 metadata amendment successor, 2026-09-11. Implements Homeowner attachment 033da395-183e-45b6-b716-7d5816d14594. Preserved predecessor: `tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch.md`, SHA-256 `F41C24BD0BD58F6B070EFE17BB69E7F89823F37B94F48A339870CD19D67825FD`. This successor changes only R27 binding/vector and mechanical references; prior decisions otherwise remain operative.

**Version:** v0.5  
**Status:** ROUND-5 CHECKPOINT — ARCHITECTURE UMBRELLA / NON-EXECUTABLE  
**Date:** 2026-09-07  
**Risk tier:** High-risk  
**Owner:** Homeowner  
**Implementation authority:** NOT GRANTED  
**Stage/commit/push authority:** NOT GRANTED

> **NON-OPERATIVE HISTORICAL BODY NOTICE**  
> Sections 2-18 preserve the terminated monolithic design/review evidence.
> They authorize no mutation and MUST NOT be copied as executable clauses.
> Known-invalid or superseded details there are resolved only by Planning v1.7
> and independently reviewed A1-A4 child dispatches. No Homeowner implementation
> grant may ever target this umbrella SHA.

## 0. Binding authority and exact working basis

This dispatch consumes, and does not amend, Planning Brief v1.7:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/
tip_88c1_c6b_capture_runtime_identity_enrollment_planning_brief.md
SHA-256 E9AF9C0C76A41736037DD2B6259F24F64569516C187D40A822300DAAB01A9696
```

Repository bases inspected by the Builder census:

```text
TagEkyc HEAD                 c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9
TagEkyc.CaptureAgent HEAD    b193f6316e13a82fb2f9f985f68500feae194cfa
```

Both worktrees contain pre-existing C6B and unrelated dirty/untracked files.
The implementation must preserve unrelated bytes. “Exists” below means the
exact path/symbol was read in the current working tree; it does not imply it is
committed, ratified or present at HEAD. Every untracked C6B dependency must be
bound by hash again before implementation begins.

Precedence:

1. explicit later Homeowner instruction;
2. Planning Brief v1.7 exact bytes;
3. this dispatch only after independent clean review and Homeowner grant;
4. landed C1-C6A contracts and the separately reviewed C6B candidate;
5. implementation choices only inside the frozen allowlist and contracts.

## 1. PI-TAG-001 activation

```text
PI-TAG-001 PILOT
Pilot TIP: TIP-88C1-C6B-A
Risk tier: High-risk
Selected modules: SQL/schema/transaction; API; cryptography/key management;
                  raw/restricted data; governance/docs
Worker/queue: Not applicable — no queue/worker is authorized; expiry is
              materialized by the owning request boundary and bounded cleanup.
Required matrices: Invariant Trace; Outcome/Precedence; Shape/Nullability;
                   Ordering Graph; Test-Bite
Independent reviewers: three internal reviewers plus final CC/GPT
Round-5 root-cause checkpoint: enabled
Round-10 hard stop: enabled
Pilot metrics report: required before closeout
```

No reviewer round counts unless it reads this complete dispatch and the source
surfaces required for its assigned module, reports all findings, and does not
stop at a quota or first finding. From round 3 onward every non-clean round must
record cumulative non-convergence causes and the concrete review/patch-method
change for the next round.

## 2. Builder objective and non-objectives

This artifact is now an architecture umbrella, not an executable grant. The
bounded child dispatches below build the prerequisite that lets an independently enrolled TagEkyc Managed
CaptureAgent authenticate, receive one session capability, bind exactly one
VerificationSession, submit capture/evidence/Raw ingress under server-derived
identity and obtain self configuration without a ClientApplication API key.

This implementation must also complete the identity-facing seams of the
already-started C6B candidate: concrete R1 broker/body-pipeline composition and
Agent orchestration may be wired only where the frozen C6B contracts already
authorize them. It must not redesign R1-R6.

Not authorized:

- Embedded SDK implementation or a new SDK/project;
- SoftwareEmbedded retained activation;
- a new Agent executable called AgentEkyc;
- SiteId, routing pool or ClientApplication-to-Agent eligibility;
- Client API-key distribution to Agent/SDK;
- dual-read, legacy producer-auth fallback or sunset machinery;
- a second Raw upload route or a second custody state machine;
- mTLS/private-HTTPS hardening, purge/legal-hold or production activation;
- real patient/biometric proof data;
- changes to C5 recipient semantics or platform credential enablement on C5.

## 3. Verified reuse-before-invention census

| Capability | Classification | Exact evidence and disposition |
| --- | --- | --- |
| Client/API-key context | `REUSE_AS_IS` for Client routes only | `src/TagEkyc.Application/AuthenticatedClientContext.cs`; never add runtime/platform fields |
| API-key CSPRNG/hash/fixed-time/pepper | `REUSE_PATTERN_ONLY` | `Infrastructure/Auth/ApiKeyProvisioningService.cs`, `ApiKeyHasher.cs`, `PostgresHashedApiKeyStore.cs`, `ApiKeyStorePepperResolver.cs`; platform partition remains separate |
| C5 management idempotency/audit/ACL | `REUSE_PATTERN_ONLY` | C5 recipient repository/config/migration; nouns, tables and operation kinds are not reusable |
| PlatformOperator credential/context/auth | `ACTUAL_MISSING` | no `teo_`, platform credential row, service, store, authenticator or `AuthenticatedPlatformOperatorContext` exists |
| Runtime registration/installation/generation | `ACTUAL_MISSING` | artifact Agent/Device values are nullable observations, not identity registry |
| Runtime role/trust/config catalogs | `ACTUAL_MISSING` | current C6B config provider is appsettings/allowed-set candidate, not durable authority |
| Bootstrap/rotation/nonce | `ACTUAL_MISSING` | no digest-only bootstrap, dual-PoP rotation authorization or generation nonce owner exists |
| Capability/execution binding/reconcile | `ACTUAL_MISSING` | landed `VerificationSession.Challenge` is separate and unchanged |
| Session cancellation | `EXTEND_EXISTING` | `EfVerificationFinalizationBoundary.TryCancelAsync`; capability terminalization must share its transaction |
| Capture/evidence mutation | `EXTEND_EXISTING` | `VerificationEvidenceApplicationService` and `EfAppendIdempotencyBoundary`; extract neutral core and replace legacy producer wrapper |
| C6B config/raw API | `EXISTS_UNTRACKED_OR_DIRTY / EXTEND` | old config route/provider and raw route/service exist in current worktree; both remain client/API-key shaped |
| C6B R1 SQL composition | `EXISTS_UNTRACKED / EXTEND_BY_FORWARD_MIGRATION` | `20260906055502_Tip88C1C6BIngressSqlComposition*`; never rewrite after dispatch basis is frozen |
| C6B broker/body pipeline implementation | `ACTUAL_MISSING` | ports exist; no concrete DI registrations/classes exist |
| Agent retained buffer/config/raw client | `EXISTS_DIRTY_OR_UNTRACKED / EXTEND` | sibling CaptureAgent Core/Client C6B files; currently disconnected and API-key based |
| Agent installation key/signing/enrollment | `ACTUAL_MISSING` | chip Active Authentication crypto is unrelated and must not be reused as runtime identity |
| Agent loopback intake | `ACTUAL_MISSING` | existing Host and WPF UI are outbound-only; no listener/slot/ACK exists |
| Embedded SDK intake | `ACTUAL_MISSING / DEFERRED` | no SDK package/project exists; exclude from initial allowlist |

## 4. Frozen architecture and context shapes

### 4.1 Authentication results

```text
AuthenticatedPlatformOperatorContext
  CredentialId             non-empty
  PrincipalId              non-empty
  CallerCategory           OperatorAdmin
  Scopes                   exact closed operator scope set
  KeyPrefix
  CredentialRevision
  CredentialState/expiry evidence
  // no ClientApplicationId, ApiKeyId or fake tenant

AuthenticatedCaptureRuntimeContext
  CaptureAgentId
  DeviceInstallationId
  CredentialId
  CredentialGeneration
  PublicKeyThumbprint
  RuntimeType              Managed
  TrustProfileRevision
  RolePolicyRevision
  RuntimeRevision
  InstallationRevision
  CredentialRevision
  Roles                    closed set
  AuthenticatedAtUtc
```

`AuthenticatedClientContext` remains unchanged. Authentication envelopes are
mutually exclusive. Platform `teo_`, Client API key and runtime signed envelope
are selected before protected target lookup; mixed/unknown/malformed envelopes
return one generic access denial without fallback.

### 4.2 Runtime request proof

The only initial proof scheme is `TAG-EKYC-CRT1-ECDSA-P256-SHA256`.
There is no negotiation or fallback. The enrolled verifier is RFC 5480
SubjectPublicKeyInfo DER for `id-ecPublicKey` / `prime256v1`, stored as
unpadded base64url. `PublicKeyThumbprint` is lower-case hex SHA-256 of those
exact DER bytes. Signatures are ECDSA-SHA256 IEEE-P1363 `r || s`, exactly 64
bytes, unpadded base64url. DER signatures, another curve/algorithm, or a
non-64-byte signature deny.

Every post-enrollment request carries exactly one value for:

```text
X-TagEkyc-Capture-Runtime-Credential-Id          lower-case UUID N
X-TagEkyc-Capture-Runtime-Credential-Generation  positive invariant decimal
X-TagEkyc-Capture-Runtime-Timestamp               yyyy-MM-ddTHH:mm:ss.fffffffZ
X-TagEkyc-Capture-Runtime-Nonce                   32 random bytes, base64url
X-TagEkyc-Capture-Runtime-Signature               64 bytes P1363, base64url
```

Duplicates, comma folding, leading/trailing whitespace, mixed Client API-key
or platform envelopes, and unknown scheme headers deny without fallback. Query
strings are forbidden on all initial runtime routes. Signed bytes are UTF-8,
without BOM, over this exact LF-terminated sequence (no CR):

```text
TAG-EKYC-CRT1
<UPPERCASE-METHOD>
<Path.Value exactly as route-matched>
<credential-id-N-lower>
<generation-decimal>
<timestamp-header-exact>
<nonce-header-exact>
<lowercase media-type token or empty>
<content-length invariant decimal or 0>
<lowercase hex SHA-256 body commitment>
<operation-binding-line>
```

No Unicode/path normalization or proxy reconstruction is allowed. Closed JSON
uses SHA-256 of exact received bytes; GET uses SHA-256(empty). Raw ingress uses
the declared `X-TagEkyc-Plaintext-Sha256` commitment because Request.Body is
not consumed before committed R1; R2-R6 verifies actual bytes. Binding lines:
bind=`CaptureCapabilityId=<N>;BindOperationId=<N>;SecretSha256=<hex>`;
reconcile=`CaptureCapabilityId=<N>;BindOperationId=<N>`; bound operations=
`BindingId=<N>;IdempotencyKey=<N>`; raw ingress=`ingress=IngressMetadataSha256=<hex>` (exact closed ten-member metadata definition and vector in the R27-amended Operation Master). Other operations use an empty line. The ingress form is forbidden outside raw ingress.

`MaxRequestAge=120s`, future `MaxClockSkew=30s`, and
`PurgeAfterUtc=SignedTimestampUtc+150s`; these have no initial config override.

#### 4.2.1 Durable nonce before business work

Authentication transaction N parses the envelope/time, resolves the verifier,
verifies the signature, and before nonce insertion checks exact current
runtime/install/generation Active state, current-generation pointer, trust and
route role (but no protected business target). It then inserts unique
`(CredentialId,CredentialGeneration,Nonce)` and commits. No protected target
lookup occurs before that commit. A duplicate denies generically. Once N
commits, no later denial, exception or rollback may delete/release the nonce.

Business transaction B starts afterwards, locks and freshly rechecks runtime,
installation, current generation, trust and role state, then locks the target
and applies business idempotency/mutation/audit. Retry uses a fresh nonce and
the same business idempotency key. Read-only reconcile/config uses the same
fresh lineage check in a separate consistent-read transaction.

## 5. Durable model

All following are new `ACTUAL_MISSING` persistence owned by C6B-A:

| Aggregate | Required durable shape |
| --- | --- |
| `PlatformOperatorCredential` | CredentialId PK, non-empty PrincipalId, unique `teo_` prefix/hash, pepper version, exact operator scopes, issue/expiry/revoke state, revision; no Client FK |
| `CaptureRuntimeRegistration` | CaptureAgentId PK, Managed type, trust/config refs, lifecycle, revision |
| `CaptureRuntimeInstallation` | DeviceInstallationId PK, runtime FK, lifecycle/revision, nullable current credential pair only while Pending; at most one Active installation per Managed runtime |
| `CaptureRuntimeCredentialGeneration` | `(CredentialId,Generation)` PK, installation FK, canonical public verifier, algorithm/thumbprint, validity/state/revision, immutable RolePolicyRevision |
| `CaptureRuntimeBootstrapIssuance` | issuance ID, keyed digest/version, runtime type/trust target, idempotency/fingerprint, issued/expiry/redeem/revoke shape, secret-once result metadata |
| `CaptureRuntimeRequestNonce` | unique generation+nonce, signed/admitted time, immutable indexed PurgeAfterUtc |
| `CaptureCapability` | public ID, session FK, keyed secret digest/version, predecessor/successor, audience, expiry, state/revision; one ActiveUnbound/Bound per session |
| `CaptureExecutionBinding` | immutable unique session and capability; exact runtime/install/generation/challenge/trust/role/revisions and ExecutionExpiresAtUtc |
| `CaptureRuntimeRolePolicy` | immutable versioned closed role set and CAS current pointer |
| `CaptureRuntimeTrustProfile` | immutable versioned Managed profile and CAS current pointer |
| `CaptureRuntimeConfiguration` | immutable version/effective interval/closed limits and CAS current pointer; registration assignment/closed reducing override |
| `CaptureRuntimeRotationAuthorization` | exact runtime/install/current generation, expiry/state/revision/idempotency and successor commitment at completion |
| runtime management operation/event | platform actor/idempotency/fingerprint/result plus append-only event; no ClientApplication required |
| capability operation/event | Client/session/idempotency/fingerprint/result plus append-only state evidence |

FKs are Restrict/no-cascade. Events are immutable. Sparse state checks,
alternate keys, partial unique Active constraints and positive revisions are
mandatory. Installation activation sequence is one transaction:

```text
insert Pending installation with both current-credential fields NULL
-> insert generation 1
-> update both current fields together
-> set installation Active
-> append audit/event
-> commit
```

Both fields must be both-null or both-non-null; every Active installation points
to its exact Active generation.

## 6. Exact API and local intake surface

| Surface | Principal | Operation |
| --- | --- | --- |
| offline `TagEkyc.ApiKeyProvisioner platform-operator` | deployment operator | provision/revoke `teo_` credential through dedicated service |
| offline `TagEkyc.ApiKeyProvisioner capture-runtime bootstrap-enroll` | deployment operator | diagnose, issue, direct-child stdin handoff, redeem/reconcile |
| offline `TagEkyc.ApiKeyProvisioner capture-runtime-cutover activate` | deployment operator | zero-row guard and Prepared→Activated expected-revision CAS |
| `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances` and `/revoke` | PlatformOperator | issue/revoke one-time bootstrap |
| `POST /api/ekyc/capture-runtime/enrollments/redeem` | bootstrap + candidate-key PoP | atomically create runtime/install/generation |
| `POST /api/ekyc/operator/capture-runtimes/suspend`, `/reactivate`, `/revoke`, `/retire` | PlatformOperator | lifecycle CAS |
| `POST /api/ekyc/operator/capture-runtimes/credential-rotations` and `/credential-rotations/revoke` | PlatformOperator | rotation authorization lifecycle |
| `POST /api/ekyc/capture-runtime/credential-rotations/{id}/complete` | current key + successor PoP | atomic generation cutover |
| `POST /api/ekyc/operator/capture-runtimes/role-policies/assign` and `/configurations/assign` | PlatformOperator | next-generation role/current config assignment |
| `POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish`, `/role-policies/publish`, `/configurations/publish` | PlatformOperator | immutable catalog revision/current CAS |
| `GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness` | PlatformOperator | exact runtime readiness |
| `POST /api/ekyc/verification-sessions/{id}/capture-capabilities` | Client session owner | closed `Action=Issue|Replace`; Replace requires current capability ID+revision; secret once |
| `POST /api/ekyc/capture-runtime/executions/bind` | runtime signature + capability | atomic first binding |
| `POST /api/ekyc/capture-runtime/executions/reconcile` | current runtime signature | read exact committed binding by capability ID + bind operation ID; no secret/state mutation |
| `GET /api/ekyc/capture-runtime/self/configuration` | runtime signature | self-bound current configuration + ETag |
| `POST /api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts` | runtime signature + role | runtime capture wrapper |
| existing `POST /api/ekyc/verification-sessions/{id}/evidence-results` after cutover | runtime signature + TrustedEvidence role | runtime evidence wrapper; no API-key producer auth |
| existing `POST /api/ekyc/raw-export/source-ingress` | runtime signature + RawIngress role | binding/acceptance-derived C6B ingress |
| `POST http://127.0.0.1:{managed-port}/tag-ekyc/capture-capabilities/accept` | SignAgent on same managed host | AgentEkyc one-slot handoff/ACK; TagEkyc-owned local surface |

The old `/api/ekyc/capture-agents/self/configuration` and legacy API-key capture
artifact producer route are unmapped at direct cutover. Client session/result
routes remain unchanged. Embedded SDK in-process intake is a future neutral
contract only and receives no implementation path in this dispatch.

### 6.1 Closed HTTP/DTO rules

JSON rejects unknown/duplicate members, trailing bytes and route ceiling
violations. Mutations require one lower-case UUIDv4-N `Idempotency-Key`.
Secret-bearing responses set `Cache-Control:no-store` and `Pragma:no-cache`.
Platform routes accept only `X-TagEkyc-Platform-Operator-Key`; Client routes
only the existing API-key header; runtime routes only CRT1. Mixed envelopes are
`403 ACCESS_DENIED`. Common mapping is malformed=`400 REQUEST_INVALID`, auth or
pre-target lifecycle=`403 ACCESS_DENIED`, canonical unavailable target=`404
RESOURCE_NOT_AVAILABLE`, revision/fingerprint conflict=`409 CONFLICT`, exact
replay=`200` except secret-once replay=`409 EXISTING_MATCH_SECRET_UNAVAILABLE`,
readiness=`503 NOT_READY`. Errors contain only code+correlationId.

Closed request/response shapes are:

- bootstrap issue: Managed runtime type, trust/role/config revisions, expiry,
  handoff-attestation digest and idempotency -> issuance ID, secret-once,
  expiry, revision; revoke adds expected revision;
- enrollment redeem: issuance ID+secret, CandidateKeyId, candidate SPKI/thumbprint, timestamp,
  nonce, idempotency and `TAG-EKYC-ENROLL1` candidate PoP -> exact runtime,
  installation, credential/generation and three revisions;
- runtime lifecycle: runtime ID, expected revision, idempotency -> state/revision;
  credential revoke additionally identifies credential/generation/revision;
- rotation authorize/revoke identifies exact runtime/install/current generation,
  expiry/revision/idempotency; completion carries successor SPKI/thumbprint and
  `TAG-EKYC-ROTATE1` successor PoP while CRT1 proves predecessor possession;
- role/config assignment carries exact published revision, expected runtime
  revision and idempotency; catalog publication carries complete closed payload,
  expected head revision and idempotency. The allowed role-policy, trust-profile,
  configuration and reducing-override fields are exactly the named fields in
  Planning v1.7's ratified D1-D13/config/trust/role matrices; the migration and
  C# records must copy those names one-for-one and adding a field is STOP/RRI;
- capability request has session only in path plus `Action`; `Issue` requires
  no current fields, `Replace` requires exact current ActiveUnbound ID+revision.
  TTL/audience remain server-owned. It returns ID, secret-once, expiry/state.
  Bind body is capability ID+secret+
  BindOperationId; reconcile omits secret and never mutates;
- runtime capture/evidence DTOs remove caller Agent/Device identity. Raw ingress
  retains landed C6B metadata/body headers, adds signed BindingId and forbids all
  caller Client/Producer/Agent identity headers.

## 7. Capability, bind and lifecycle ordering

### 7.1 Capability issue/replace

```text
authenticate ClientApplication/session ownership
-> validate closed request and UUIDv4 idempotency
-> lock session/capability domain
-> recheck session Active/unbound/nonterminal
-> materialize expired current capability if necessary
-> issue at least 256-bit secret, persist only keyed digest/version
-> insert operation + event
-> commit
-> return secret once with no-store
```

Exact replay returns metadata only plus `ExistingMatchSecretUnavailable`.
Recovery explicitly revokes/reissues an unbound lost capability under a new key.

### 7.2 Local intake

The existing SignFlow backend-to-SignAgent channel is input, not TagEkyc
readiness. In the sibling CaptureAgent repository one reusable component owned
by both Host and WPF UI:

- binds only `127.0.0.1`/`::1`;
- exposes exactly one closed POST route and no diagnostics/static/proxy/redirect;
- starts only while one explicit pending slot exists;
- limits header/body/time and rejects unknown JSON members;
- stores capability only in bounded memory;
- first valid delivery ACKs and closes slot/listener;
- timeout/cancel/first terminal error also closes and zeroes;
- duplicate/late/wrong delivery denies;
- port preemption/start failure fails readiness;
- never exposes capture buffer, config or logs.

### 7.3 Bind

```text
validate timestamp/envelope and authenticate runtime public-key signature
-> transaction N inserts unique nonce and COMMITs
-> start independent business transaction B
-> lock runtime/install/generation
-> lock session/capability common domain
-> recheck lifecycle, Bind role, trust, capability digest/state/horizon
-> resolve session and ClientApplication internally
-> insert immutable execution binding
-> transition capability Bound + append event/audit
-> commit B
-> erase Agent capability secret
```

No Site/pool predicate exists. A different runtime loses the unique binding
race. Exact same-lineage retry returns the same BindingId without a second event.

### 7.4 Reconcile and cancellation

Agent persists before bind only non-secret `CaptureCapabilityId` and
`BindOperationId`. Reconcile is signed, read-only, non-enumerating and returns
only same-lineage BindingId/horizon. It cannot bind, rebind or re-emit secret.

Session cancel/finalization locks the common session/capability domain and
terminalizes capability in the existing finalization transaction. Bound wins
replacement; terminal session prevents new bind. Expiry-on-denied materializes
one Expired transition/event and commits through a typed terminal result rather
than being rolled back by API error mapping.

## 8. Capture/evidence/C6B direct cutover

Extract authority-neutral capture and evidence append commands inside the
existing transaction boundary. The only producer wrapper after cutover:

```text
runtime signed context
-> execution binding
-> route role and lifecycle recheck in transaction
-> server-derived ClientApplicationId/CaptureAgentId/DeviceInstallationId
-> exact existing domain/idempotency/evidence mutation
-> runtime audit
-> one commit
```

Caller Agent/Device identity fields are absent; non-null compatibility values
deny rather than being overwritten. Producer identity never comes from key
prefix, PrincipalId, request fields or `AllowedCaptureAgentIds`.

For Raw ingress, `ClientApplicationId` comes from the bound session,
`ProducerId` comes only from binding `CaptureAgentId`, and
`CaptureAgentInstanceId` comes only from binding `DeviceInstallationId`.
The exact accepted artifact/revision is an equality predicate and never an
identity source; a mismatch returns the existing binding-invalid result before
R1/body. Existing R1 dual-lock order, committed-R1-before-body,
Expect:100-continue posture, 36 outcomes and R2-R6 convergence remain unchanged.

The concrete server composition must implement and register the existing
`IRawExportSourceIngressBroker` and `IRawExportSourceBodyPipeline` ports by
composing the reviewed SQL R1/R2 handoff and landed R2-R6 services. It must not
create a second state machine. Any required rewrite of an earlier migration,
new upload route or changed R1-R6 outcome is STOP/RRI.

## 9. Direct-cutover migration guard

No production legacy population is expected. Before unmapping/removing legacy
producer authentication, the forward cutover migration checks all affected
durable producer surfaces. At minimum:

- `api_keys` CaptureAgent/TrustedAdapter categories and non-null allowed sets;
- capture artifacts with Agent/Device observations;
- producer audit events;
- capture acceptance events;
- Raw ingress claims and aliases.

No reliable landed fixture discriminator exists. The exact rule is therefore
zero rows across every surface listed above, without name/ID heuristics. Any
row raises `TIP88C1C6BA_LEGACY_PRODUCER_POPULATION_PRESENT` and leaves a new
cutover sentinel in `Prepared`. Deployment order is foundation/cutover
migration -> sentinel/ACL readiness -> new binary. SQL rollback is not claimed
to restore compiled routes: failed migration leaves the previously deployed
binary untouched; rollback restores a compatible old binary before Down. No
automatic conversion, reinterpretation or dual-read is allowed.

## 10. ACL and privilege model

Reuse PostgreSQL C5/C6A discipline, not their nouns:

- every privileged function is `SECURITY DEFINER`, owned by the existing
  deployer role and `SET search_path=pg_catalog`;
- PUBLIC and ordinary runtime role have no table DML;
- platform-operator authentication receives read/execute only for its partition;
- runtime authentication/nonce/bind functions receive only their named
  capability role;
- C6B broker retains its existing broker-owned transaction/DB capability;
- application runtime, broker and operator capabilities are not mutually
  inherited unless an exact call requires it;
- Down revokes EXECUTE before dropping functions/roles/objects;
- readiness proves exact owner, search path, grant set, PUBLIC denial and table
  DML denial.

Create exactly three NOLOGIN capability roles:
`tagekyc_capture_runtime_operator`, `tagekyc_capture_runtime_authenticator`,
and `tagekyc_capture_runtime_application`. Two deployment-managed LOGINs are
required and may not be created by product migration:
`tagekyc_capture_runtime_online_login` inherits authenticator+application only;
`tagekyc_capture_runtime_operator_login` inherits operator only. Neither
inherits `tagekyc_runtime`; the general TagEkyc API connection receives no new
membership. Existing broker login remains
only in `tagekyc_raw_export_claim_broker` and inherits none of these roles.

All new objects are owned by `tagekyc_raw_export_deployer`. All table/sequence
DML is revoked from PUBLIC, `tagekyc_runtime`, broker and the three roles.
Operator executes only management/catalog/readiness functions; authenticator
only credential verification/verifier lookup/nonce claim; application only
capability, bind, reconcile and runtime wrappers. Broker executes only the new
binding/role validation composed immediately before its existing R1. Down
revokes memberships and EXECUTE, drops functions/triggers/tables in FK order,
then drops the three roles. Readiness compares the entire graph.

### 10.1 Executable persistence contract

Foundation creates exactly these `tagekyc` tables: `platform_operator_credentials`,
`capture_runtime_registrations`, `capture_runtime_installations`,
`capture_runtime_credential_generations`, `capture_runtime_bootstrap_issuances`,
`capture_runtime_request_nonces`, `capture_capabilities`,
`capture_execution_bindings`, revision+head pairs for role policy, trust profile
and configuration, `capture_runtime_rotation_authorizations`, management
operation+event, capability operation+event, and singleton
`capture_runtime_cutover_state`. No JSON/open bag carries state, scopes, roles,
replay results or references. IDs/idempotency are uuid; revisions bigint >0;
times timestamptz; SHA-256 values bytea length 32; nonce bytea length 32.

Registration PK is `CaptureAgentId`. Installation PK is
`DeviceInstallationId`, FK runtime RESTRICT, with current credential ID and
generation both NULL iff Pending and both non-NULL otherwise. Generation PK is
`(CredentialId,Generation)`, alternate key
`(DeviceInstallationId,CredentialId,Generation)`; the installation current
pointer is a DEFERRABLE INITIALLY DEFERRED composite FK to that key. Partial
unique constraints allow one Active Managed installation/runtime and one
Active generation/installation. `ValidFromUtc < ValidUntilUtc`.

Capability PK is `CaptureCapabilityId`, session FK RESTRICT, digest+pepper
version, issue/expiry, closed state/revision, and partial unique SessionId where
state is ActiveUnbound/Bound. Predecessor/successor RESTRICT and cannot self
reference. Binding PK is `CaptureExecutionBindingId`, unique SessionId and
CapabilityId, with exact composite generation lineage and all frozen IDs,
revisions and expiry non-null/immutable. Catalog revision PK is
`(CatalogId,Revision)` and head composite-FKs to current revision; rollback is a
new revision. Operations freeze fingerprint, non-secret result code/IDs and
unique actor/aggregate+idempotency. Events FK RESTRICT and UPDATE/DELETE deny.

Locks use two-argument advisory locks `(domain,hashtext(canonical UUID/text))`;
collision safety comes from row/key recheck. Frozen domain order is bootstrap 5,
runtime 10, installation 20, generation 30, catalog 40, rotation 60, session
70, capability 80. Within a domain signed keys sort ascending. Row `FOR UPDATE`
and expected-revision CAS follow locks. Bind is 10->20->30->70->80; cancel
70->80; capability issue 70->80; bootstrap redeem 5->10->20->30; rotation
10->20->30->60. Losers write no
second event.

Nonce cleanup deletes at most 500 expired rows (`PurgeAfterUtc < now`) ordered
by purge time/credential/generation using `FOR UPDATE SKIP LOCKED`; equality is
retained. Index `(PurgeAfterUtc,CredentialId,CredentialGeneration)` is required.
Production readiness requires explicit `MaximumNonceRows` and
`MaximumExpiredNonceAgeSeconds`, both positive, with no default. Cleanup failure
cannot authorize or remove an unexpired nonce.

After canonical identity, semantic denial freezes one operation result and one
audit event in transaction B. Audit failure rolls back target mutation and can
never return success. Pre-identity/malformed/signature/timestamp/duplicate-nonce
denials are telemetry-only and contain no target facts.

### 10.2 Initial Managed key-custody profile

The first build packet may implement only the verified Windows WPF profile,
but this is a build-order limitation, not a narrowing of the ratified Managed
contract. It MUST NOT claim Managed completion or production readiness until a
Homeowner-reviewed cross-platform secure-store profile exists. The Windows
packet creates one named, non-exportable ECDSA P-256 CNG key in
`Microsoft Software Key Storage Provider`, CurrentUser scope of the dedicated
account running both Host/WPF. No PKCS#8/private bytes, recovery copy or config
secret is allowed. Before redeem, Agent generates UUIDv4 `CandidateKeyId`; the
local locator is `tagekyc-capture-runtime-<CandidateKeyId-N>`. Redemption
persists the returned DeviceInstallationId/generation-to-locator mapping only
after exact replay/reconcile resolution; it never renames or regenerates the
candidate key after ambiguous response loss. Opening proves provider, curve,
non-exportability and SPKI thumbprint before signing.

Wrong account, unsupported OS, inaccessible/corrupt/exportable/mismatched key
is NOT READY and must not create a replacement for the same credential.
Rotation creates a distinct successor key; deletes it on failed terminal
completion, or deletes predecessor only after committed cutover and locally
durable successor receipt. Retire/uninstall deletes only after server terminal
confirmation or explicit offline cleanup; deletion failure remains NOT READY.

### 10.3 Loopback slot and ACK contract

The shared Handoff library owns one slot and exposes only `OpenSlotAsync` and
`CloseSlotAsync`. Before delivery the slot contains only non-secret SlotId,
OpenedAtUtc, NotAfterUtc and Timeout. Host/UI opens it for explicit local
begin-capture; the first syntactically valid closed envelope atomically binds
its SignFlowLocalSessionRef, VerificationSessionId and expiry to the slot before
capability secret publication to the single consumer. Parsing uses a bounded
streaming tokenizer over pooled mutable byte/char buffers; the secret must not
enter `string`, `JsonDocument`, model binding or logs. Every application-owned
mutable buffer is zeroed on all exits. Kernel/runtime residue is honestly
bounded, not claimed absent. A second open is Busy.
`TAGEKYC_CAPABILITY_HANDOFF_PORT` is mandatory, decimal 1024..65535, no
default/dynamic fallback, distributed identically to SignAgent and AgentEkyc.
Both `127.0.0.1` and `::1` bind or readiness fails; wildcard, forwarding headers,
redirect, proxy and non-literal Host deny.

The only POST body (max 4096 bytes, header max 8192 bytes, header/body timeout
5 seconds, slot timeout 120 seconds; no overrides initially) is closed JSON:
`SchemaVersion=1`, `SignFlowLocalSessionRef`, `VerificationSessionId`,
`CaptureCapabilityId`, `CaptureCapabilitySecret`, `ExpiresAtUtc`. Unknown,
duplicate, empty, noncanonical or mismatched fields close+zero the slot. First
matching request atomically claims it into a single-consumer result. Response is
HTTP 200 with `Cache-Control:no-store` and closed `{accepted:true}`; it must be
flushed before listener disposal. Abort before flush is no ACK. Timeout,
cancellation, malformed/slow/oversized request, terminal bind/reconcile result
and shutdown stop listeners and zero secrets. No slot means no bound socket.

### 10.4 Verifier-pepper lifecycle

Offline `TagEkyc.ApiKeyProvisioner capture-runtime-verifier-key
provision|rotate|retire|readiness` owns versioned deployment-secret versions.
HKDF-SHA256 derives disjoint `bootstrap-digest-v1`, `capability-digest-v1` and
`platform-credential-v1` subkeys. Provision/rotate emits no secret; retirement
is blocked while any durable row references the version; missing referenced
version makes readiness RED. This is separate from Agent installation keys.

### 10.5 Closed secret and ceremony constants

Platform operator key is `teo_` plus 32 CSPRNG bytes encoded base64url; stored
digest is HMAC-SHA256(`platform-credential-v1\0` + decoded bytes) under the
referenced pepper version. Bootstrap and capability secrets are exactly 32
CSPRNG bytes base64url; their stored digests are respectively HMAC-SHA256 of
`bootstrap-digest-v1\0`/`capability-digest-v1\0` + decoded bytes. Comparisons
are fixed-time over 32 bytes. Prefix+hash uniqueness is enforced. Initial
server-owned maxima: bootstrap 10 minutes, capability 5 minutes, execution
30 minutes, rotation authorization 10 minutes. Callers cannot choose a longer
TTL. Production config must set nonce row ceiling 1,000,000 and expired-age
ceiling 900 seconds; no fallback activates readiness.

Bootstrap issue does not accept CaptureAgentId. Its closed input is
`RuntimeType=Managed`, trust-profile revision, role-policy revision,
configuration revision, expiry, idempotency and handoff-attestation digest.
Offline deployment runs `capture-agent diagnose --handoff --json`, whose closed
nonsecret output is schema version, OS/profile, executable SHA-256, CNG provider,
loopback IPv4/IPv6 availability, configured port and evidence digest. The
provisioner directly launches the enrollment process and writes the one-time
bootstrap envelope only to inherited stdin; child ACK causes parent and child
to zero it. Clipboard, argv, environment, file and log transport are forbidden.

Candidate key PoP signs UTF-8 LF-only bytes:
`TAG-EKYC-ENROLL1\n` + issuance UUID-N + candidate-key UUID-N + SPKI base64url +
thumbprint hex + timestamp exact + nonce base64url + idempotency UUID-N +
bootstrap-secret SHA-256 hex, each on its own line with final LF. P-256/P1363,
timestamp 120s/skew30s and 32-byte nonce rules equal CRT1. Issuance lookup,
digest, audience/trust revisions and attestation digest are verified before one
atomic identity activation. Exact replay returns the same nonsecret lineage.

Successor PoP signs UTF-8 LF-only bytes:
`TAG-EKYC-ROTATE1\n` + rotation UUID-N + runtime UUID-N + installation UUID-N +
old credential UUID-N + old generation decimal + CandidateKeyId UUID-N + SPKI
base64url + thumbprint hex + idempotency UUID-N, final LF. The request also has
normal CRT1 proof by the predecessor. Ambiguous timeout/5xx retains both keys
and retries/reconciles; successor is deleted only after authoritative
noncommitted terminal denial, revoke or expiry. Predecessor deletion occurs
only after exact committed successor lineage is durable locally.

### 10.6 Callable roles, sentinel and broker composition

The API host uses a separate named connection profile backed by
`tagekyc_capture_runtime_online_login` only for runtime-authenticated endpoints;
ordinary Client routes retain their existing connection. Offline provisioner
uses only `tagekyc_capture_runtime_operator_login`. Password/certificate values
are deployment secrets, never migration/config defaults. Broker receives none
of the three roles. Readiness asserts memberships exactly and rejects any
cross-membership or LOGIN membership in `tagekyc_runtime`.

Sentinel row is `(Profile='Managed',State Prepared|Activated,Revision>0,
ActivatedAtUtc nullable iff Prepared)`. Foundation inserts Prepared. The
cutover SECURITY DEFINER operation, sole grantee operator role, locks the row,
requires expected revision, runs literal zero-row predicates, and changes to
Activated in the same transaction. New binary maps runtime routes and unmaps
legacy producer routes only when startup readiness observes Activated plus the
exact schema/ACL; otherwise startup fails. Production remains prohibited by
the separate C6B lifecycle gates.

Broker-owned function is a fully typed forward wrapper named
`tagekyc.capture_runtime_begin_bound_raw_ingress`. Its first three parameters
are `(p_binding_id uuid,p_acceptance_id uuid,p_acceptance_revision bigint)`;
the remaining parameters copy, in identical order and PostgreSQL types, every
argument of landed `raw_export_begin_retained_source_ingress_with_authority`.
Its `RETURNS TABLE` copies that landed function's result columns one-for-one.
No json/jsonb/composite/open bag is permitted. Owner deployer, search_path
pg_catalog, sole EXECUTE grantee broker. It locks runtime 10 -> installation 20
-> generation 30 -> session 70 -> binding/capability 80, validates current
lineage/role/acceptance equality, then invokes the existing typed begin and
complete/handoff functions in the broker-owned transaction. The dispatch must
freeze the generated migration's literal signature against bound migration
`20260906055502` before implementation continues; mismatch is STOP. Only the
frozen 36 outcomes may be mapped.

## 11. Frozen mutation allowlist

### 11.1 TagEkyc repository — existing paths permitted to modify

```text
TagEkyc.sln
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/VerificationSessionEndpoints.cs
src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs
src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs
src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs
src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs
src/TagEkyc.Application/Ports/ApplicationServicePorts.cs
src/TagEkyc.Application/Ports/RepositoryPorts.cs
src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs
src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs
src/TagEkyc.Contracts/CaptureAgent/CaptureAgentContracts.cs
src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs
src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/DomainRowMapper.cs
src/TagEkyc.Infrastructure/Persistence/EfAppendIdempotencyBoundary.cs
src/TagEkyc.Infrastructure/Persistence/EfVerificationFinalizationBoundary.cs
src/TagEkyc.Infrastructure/RawExport/RawExportCaptureAcceptanceResolver.cs
src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
tools/TagEkyc.ApiKeyProvisioner/Program.cs
```

New paths are permitted only in these exact families:

```text
src/TagEkyc.Contracts/CaptureRuntime/*
src/TagEkyc.Application/CaptureRuntime/*
src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs
src/TagEkyc.Api/CaptureRuntimeEndpoints.cs
src/TagEkyc.Api/CaptureRuntimeManagementEndpoints.cs
src/TagEkyc.Api/CaptureRuntimeControlEndpoints.cs
src/TagEkyc.Api/IPlatformOperatorAuthenticator.cs
src/TagEkyc.Api/ICaptureRuntimeRequestAuthenticator.cs
src/TagEkyc.Infrastructure/Auth/PlatformOperatorCredential*.cs
src/TagEkyc.Infrastructure/Auth/CaptureRuntimeRequest*.cs
src/TagEkyc.Infrastructure/CaptureRuntime/*
src/TagEkyc.Infrastructure/Persistence/Entities/PlatformOperatorCredentialRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntime*.cs
src/TagEkyc.Infrastructure/Persistence/Entities/CaptureCapability*.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/PlatformOperatorCredentialConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntime*.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureCapability*.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<new-C6BA-foundation>.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<new-C6BA-foundation>.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<new-C6BA-cutover>.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<new-C6BA-cutover>.Designer.cs
```

Focused new/modified tests are permitted only under the four existing test
projects with filename prefix `Tip88C1C6BA` plus exact existing C6B test files
whose assertions must change for runtime identity. No broad test rewrite.

### 11.2 TagEkyc.CaptureAgent repository

Existing modifications:

```text
TagEkyc.CaptureAgent.sln
src/TagEkyc.CaptureAgent.Host/TagEkyc.CaptureAgent.Host.csproj
src/TagEkyc.CaptureAgent.Host/Program.cs
src/TagEkyc.CaptureAgent.Ui/TagEkyc.CaptureAgent.Ui.csproj
src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs
src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs
src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs
src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs
src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs
src/TagEkyc.CaptureAgent.Client/TagEkycHttpClient.cs
tools/TagEkyc.CaptureAgent.Preflight/Program.cs
packaging/RUNBOOK.md
packaging/packaging-assets.lock.json
build/publish.ps1
tests/TagEkyc.CaptureAgent.Tests/TagEkyc.CaptureAgent.Tests.csproj
tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BCaptureAgentTests.cs
```

New paths:

```text
src/TagEkyc.CaptureAgent.Core/CaptureRuntimeIdentityContracts.cs
src/TagEkyc.CaptureAgent.Core/CaptureCapabilityIntake.cs
src/TagEkyc.CaptureAgent.Crypto/InstallationKeyStore.cs
src/TagEkyc.CaptureAgent.Crypto/CaptureRuntimeRequestSigner.cs
src/TagEkyc.CaptureAgent.Client/CaptureRuntimeEnrollmentClient.cs
src/TagEkyc.CaptureAgent.Handoff/TagEkyc.CaptureAgent.Handoff.csproj
src/TagEkyc.CaptureAgent.Handoff/CaptureCapabilityLoopbackListener.cs
tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA*.cs
```

One reusable Handoff library is authorized; no new executable. Project/lock
files caused solely by adding this library are permitted only after exact
restore diff review. Embedded SDK paths remain forbidden.

### 11.3 Exact-byte dependency manifest

Raw-byte SHA-256; all are dirty/untracked evidence dependencies and must be
recomputed immediately before mutation. Any drift/missing/additional consumed
dependency is STOP/rebind.

```text
TagEkyc
FA274C40E673450286E776B1EC48CD9B8997E788840D30165B4A20EBC7F79E6D src/TagEkyc.Api/Program.cs
923CDF0E0204EFFC1A44FC9CC906DDB5D12A8BBD9290A76FD760906B93EC9012 src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs
AE4408D7B2BF46A3EAFA9F1801D59C37BF3B5950662EBCA73ED1109D147762C7 src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs
1EEFFFB5E632A617C923ABC4A1D83F03C9F93FF5A46244F2DFA077042E9E83E5 src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
248A9D3AA966FF643245CB67C46455D82B13BE59577E621355324FE67F7292F6 docs/.../tip_88c1_c6b_technical_implementation_dispatch.md
532F4196BE430DCDF877F263D6DB37890E399353DE835D579A63F893616B47A8 CaptureAgentConfigurationEndpoints.cs
831D6032AA4C25105924501D808F5CD3E67498D7F97CAC32157BC6747EBF8A88 CaptureAgentConfigurationProvider.cs
1023A127F4A5183C2A9A1D63060C5B7638EC66DE35F87CE084961EFC1F34598F RawExportSourceIngressEndpoints.cs
F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA RawExportSourceIngressPorts.cs
C996F31D4ADBBE7F33225879807741622CB67DDDD8305E5498C8C6CCC8357195 RawExportSourceIngressApplicationService.cs
4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66 RawExportSourceIngressContracts.cs
6F756EA61C02273476D7A26B6328C54DE0F68CEAF2C56545A52323CEF0123E9C 20260906055502_Tip88C1C6BIngressSqlComposition.cs
3003785955C73AD04C3AD85A86B94BA068870D8A83E873363BF760B3A24E1C7A 20260906055502_Tip88C1C6BIngressSqlComposition.Designer.cs
A894E73D3108C5057FB9A3638AADDF9D9EBD365507D68F0805452C7B00FF6380 RawExportCaptureAcceptanceResolver.cs
394B2829A5B86D16BCA55629595A14D802556609E4D229F13335F3D20F2DF806 RawExportIngressCapacity.cs
BC5C856406DDA8B79CBC1D861F5F38D2604C106C80B7E24CB7D70C2395867F0 Tip88C1C6BRawIngressTransportTests.cs
CaptureAgent
B56D118E5B501E8140140D3F0EA57B0F94ED38E328497BA0E80E8B07613966DD TagEkycHttpClient.cs
97A479CDF490261236F62627EE19141643866E1BD5A0C9D0CDCF0C463825C405 CaptureAgentPorts.cs
88B10A286F698773092F035DA5E2D52BE3AB32EFC872B926BD0C486DA2B23B9B CaptureAgentProductionConfigGate.cs
13D8356C07A758428AB41B62DAF2D135BD01588D907ED948B5BD6E02F80F2392 RawExportIngressContracts.cs
8200232C63A6DCB6DEFF4E60F8427B7E8D94E18663A360DCF795CD209D1DA60A RetainedRawBufferCapacity.cs
A2F2CE79B55F1F28E7FA49B456B65372152DCB451586C96A43FBBF0DAA7C7174 Host/Program.cs
A2EB75A697DDFCB34077EC6EFEB5C3D380C4066D2013E1412980774272309B0F Ui/CaptureAgentComposition.cs
AE16804ADE1EB3B70FE15066424B84B765CA8F1FE84069CEAC828400F21A18F3 Tip88C1C6BCaptureAgentTests.cs
```

## 12. Invariant Trace Matrix

| ID | Requirement | Owner | Order | Contract | Outcome/evidence | Proof/mutation |
| --- | --- | --- | --- | --- | --- | --- |
| A01 | Client secret never reaches runtime | Client/API | client auth before capability | secret-once capability | no Client API key in Agent | C6BA08/C6BA25; inject key RED |
| A02 | platform root has no client partition | platform auth | authenticate before target | platform context | non-empty credential/principal | C6BA01; fake ClientId RED |
| A03 | runtime identity is TagEkyc-owned | enrollment/auth | PoP before Active | runtime context | exact runtime/install/generation | C6BA03/05; request AgentId RED |
| A04 | request replay is durable | runtime auth | timestamp then nonce then lookup | signed envelope | one winner; bounded residue | C6BA05/06; memory nonce RED |
| A05 | capability secret is one-time | session owner | session lock before issue | capability row | digest only; nonsecret replay | C6BA19; secret replay RED |
| A06 | execution binding is singular | bind owner | identity then session/cap locks | binding row | same-lineage replay only | C6BA09; second bind RED |
| A07 | lifecycle is live pre-operation | every mutation | lock/recheck in transaction | revision/state | deny with no target leakage | C6BA04-07; outer check RED |
| A08 | producer identity is derived | runtime wrappers | binding before append | neutral command | exact Agent/install audit | C6BA12-14; fallback RED |
| A09 | R1 precedes body | C6B broker | acceptance+begin commit first | ingress ports | zero body on denial | C6BA14/15; early read RED |
| A10 | R2-R6 is composed once | C6B pipeline | after R1 handoff | existing ports | frozen outcomes/convergence | C6BA16; second machine RED |
| A11 | local intake is narrow | Agent Handoff | slot before listener/body | loopback DTO | ACK/close/zero | C6BA19-21; wildcard/persistent RED |
| A12 | direct cutover is fail-closed | migration/API | population guard before unmap | named exception | rollback, no dual-read | C6BA22; unexpected row RED |
| A13 | audit is atomic/nonsecret | mutation owners | event before commit | operation/event | exact actor/revisions | C6BA24; omit/leak RED |
| A14 | Embedded SDK remains deferred | governance/arch test | allowlist check | project graph | no SDK/listener artifact | C6BA25; add SDK path RED |

## 13. Outcome and precedence matrix

| Command | Higher-to-lower precedence | External outcome | Durable residue |
| --- | --- | --- | --- |
| platform/runtime auth | malformed/mixed -> invalid credential -> inactive/expired -> scope/role | generic access denied | telemetry only before canonical identity; audit after identity |
| bootstrap redeem | malformed -> expired/revoked/redeemed -> bad secret -> bad PoP -> conflict | generic invalid or exact replay/conflict | one terminal state/event; no partial Active rows |
| signed request | envelope/time -> credential lineage -> nonce replay -> role -> resource | denied/replay/operation result | nonce only after valid time; operation atomic |
| capability issue | auth/session -> idempotency -> current state/horizon | created/nonsecret replay/conflict | operation+capability+event |
| bind | envelope/auth/nonce -> lifecycle/role -> capability -> session/binding | bound/exact replay/ineligible/conflict | one binding + Bound event or no mutation |
| reconcile | auth/lineage -> exact IDs -> committed binding | binding metadata or generic unavailable | none |
| local intake | listener/slot -> size/time/shape -> first valid delivery | ACK or generic refusal | memory only, zeroed on closure |
| capture/evidence/raw | auth/nonce -> lifecycle/role -> binding -> resource/idempotency | existing mapped result | one transaction; no pre-auth existence leak |
| cutover migration | schema preconditions -> legacy population guard -> new constraints/routes | apply or named failure | complete rollback on failure |

## 14. Shape and nullability matrix

| Variant | Required | Nullable/absent | Forbidden |
| --- | --- | --- | --- |
| platform context | CredentialId, PrincipalId, category, scopes, revision | none of identity | ClientApplicationId, ApiKeyId |
| runtime context | runtime/install/credential/generation/thumbprint/revisions/roles | none of identity | ClientApplication identity, private key |
| Pending installation | runtime/install/lifecycle/revision | both current credential fields NULL | one-null current pair |
| Active installation | runtime/install/current pair/lifecycle/revision | none current pair | inactive/missing generation |
| capability issue success | ID, expiry, plaintext secret once | predecessor optional | secret in durable/audit/log |
| capability replay | ID/state/expiry | plaintext secret absent | regenerated/re-emitted secret |
| binding | session/client/runtime/install/capability/generation/revisions/horizon | none | Site/pool/rebind mutable fields |
| runtime capture/evidence request | business observations + binding | compatibility Agent/Device absent | non-null caller Agent/Device authority |
| loopback DTO | schema, SignFlow local session ref, VerificationSessionId, capability ID+secret, expiry | none | API key, ClientId, AgentId, extension bag |

## 15. Ordering graph

```text
offline platform credential provision
-> platform-authenticated catalogs/bootstrap
-> Agent generates installation key
-> bootstrap redeem + candidate PoP
-> Pending installation + generation 1 + current-pair + Active commit
-> runtime self-config

Client creates session
-> issues one-time capability
-> existing SignFlow channel to SignAgent [input assumption]
-> AgentEkyc one-shot loopback intake
-> runtime signed bind + durable nonce
-> immutable execution binding
-> runtime capture observation
-> runtime TrustedEvidence append + exact acceptance
-> runtime Raw ingress
-> broker commits R1 before body read
-> existing R2-R6 convergence
```

Cancellation/expiry/revoke/rotation races use the shared identity/session lock
domains defined above. No post-R1 runtime reauthorization is inserted into
committed custody convergence.

## 16. Test-Bite Matrix and named proof set

The exact named proof ledger is below. Every test invokes the callable boundary,
asserts response plus durable residue/forbidden side effect, and contains the
named RED mutation; source-text inspection cannot close a row.

| ID | Callable proof and required RED mutation |
| --- | --- |
| C6BA01 | platform provision/auth/context; fake ClientApplicationId RED |
| C6BA02 | CRT1 golden vector plus method/path/header/body one-byte mutations RED |
| C6BA03 | enrollment stdin+attestation+ENROLL1 and response-loss replay; argv/file or regenerated key RED |
| C6BA04 | nonce N commits before failed B; shared rollback or revoked-key nonce insertion RED |
| C6BA05 | lifecycle/credential/role/trust fresh denial; outer-only check RED |
| C6BA06 | rotation dual-PoP/lost response/reconcile; ambiguous successor deletion RED |
| C6BA07 | catalog/head CAS and referenced pepper retirement; in-place update/missing version RED |
| C6BA08 | capability issue secret-once/replacement/expiry; re-emission or second active row RED |
| C6BA09 | bind race and exact replay; alternate runtime/rebind/second event RED |
| C6BA10 | reconcile read-only nonenumeration; secret/state mutation RED |
| C6BA11 | cancel/finalize/expiry common-lock races; Bound-session replacement RED |
| C6BA12 | capture runtime wrapper derives identities; caller/artifact authority fallback RED |
| C6BA13 | evidence runtime wrapper/acceptance; legacy API-key producer path RED |
| C6BA14 | broker wrapper exact lineage+acceptance then existing dual locks; precheck/out-of-order lock RED |
| C6BA15 | committed R1 before first body read; early reader/provider/R2 arm RED |
| C6BA16 | full R2-R6 36-outcome mapping/replay/convergence; second state machine/outcome 37 RED |
| C6BA17 | config self/ETag binding and disabled/stale fail closed; caller Agent selector RED |
| C6BA18 | CNG missing/corrupt/exportable/wrong-account/restart cleanup; replacement key RED |
| C6BA19 | loopback open/busy/first-envelope binding/no-slot; pre-required session values RED |
| C6BA20 | IPv4+IPv6/Host/forwarded/wildcard/port-preemption readiness; wildcard bind RED |
| C6BA21 | ACK flush abort/timeout/cancel/shutdown zeroization in Host and WPF; erase-before-flush RED |
| C6BA22 | zero-row cutover/sentinel/startup/rollback; any legacy row or route coexistence RED |
| C6BA23 | complete ACL owner/grantee/membership/table-DML/Down test; extra edge RED |
| C6BA24 | denial audit rollback, nonce cleanup boundary/concurrency and secret/log scan; leak/unbounded delete RED |
| C6BA25 | allowlist/architecture excludes Embedded SDK/new executable/dual-read; add forbidden path RED |

Mandatory end-to-end positive path:

```text
platform bootstrap -> Agent enrollment -> config -> Client session/capability
-> loopback -> bind -> capture -> evidence acceptance -> C6B R1 -> R2-R6
```

Mandatory negatives include wrong/mixed credential, duplicate nonce, response
loss, bind race, cancel/revoke/rotation races, wrong binding, caller identity,
pre-R1 body read, missing broker/pipeline, legacy population, loopback wildcard,
port preemption, slow/oversized/duplicate body and secret leakage.

## 17. Build and verification order

1. freeze exact hashes of every dirty/untracked C6B dependency in both repos;
2. add server contracts/application ports and compile;
3. add foundation persistence/config/migration and schema/ACL tests;
4. add platform/runtime authentication and nonce proofs;
5. add management/enrollment/rotation/catalog boundaries;
6. add capability/binding/reconcile and cancellation composition;
7. refactor neutral capture/evidence core and direct-cutover runtime wrappers;
8. replace config/raw ingress identity and add concrete broker/pipeline;
9. add Agent key/enrollment/signing/config/bind/reconcile orchestration;
10. add shared loopback library and wire Host+WPF;
11. apply cutover migration only after population guard proofs;
12. run focused mutations, both full solutions, migration apply/rollback/reapply,
    readiness and secret/log scans;
13. produce exact-byte as-built and independent implementation review packet.

CaptureAgent full tests must run; the earlier NU1004 baseline lock-file failure
cannot be carried as a completion exception. Repairing an unrelated lock file
requires separate authority unless the failure is caused by an allowlisted
project/package change in this implementation.

## 18. HARD STOP / RRI

STOP before mutation if:

- any brief/dispatch SHA or dirty dependency hash differs from the reviewed
  implementation basis;
- an assumed class, route, table, role, project or test path is absent or has
  incompatible semantics;
- direct cutover finds unexpected non-fixture legacy population;
- exact runtime signature algorithm/canonicalization/nonce/skew or platform
  credential ACL remains unpinned after dispatch review;
- implementation needs Site/routing, Client API key on Agent, SDK code, new
  executable, queue/worker, second upload route/state machine or migration
  history rewrite;
- a new DB role/login or privilege edge is needed beyond the exact reviewed ACL;
- C6B R1-R6 outcome/order must change;
- patient/real biometric data, deployment or production activation is needed;
- a path outside the frozen allowlist must change;
- CaptureAgent full tests cannot execute before completion claim.

## 19. Review ledger and convergence control

### 19.1 Mandatory Round-5 root-cause checkpoint

```text
ROOT CAUSE: scope construction, not reviewer strictness.

This monolith combines four independently deployable authorities:
A1 server identity/control persistence and APIs;
A2 Windows Managed Agent key/enrollment/handoff;
A3 isolated C6B broker composition;
A4 DBA/deployment identities plus quiesced direct cutover.

Continuing monolithic patching is prohibited. Findings about literal DDL/DTOs,
runtime-specific connections and audit belong to A1; CNG/material ownership to
A2; pre-body begin versus post-body complete/R2-R6 to A3; LOGIN provisioning,
connection secrets, population fence and route swap to A4.

Dependency order: A1 contract PASS permits A2/A3 design; A1 as-built is required
for A2/A3 integration; A1+A2+A3 exact as-built -> A4. Product route/sentinel
code and online/operator connection selection belong to A1; public Raw ingress
auth/pre-broker contract belongs to A1, while A3 owns only private broker route,
adapter, pre-body R1 and post-body pipeline. A4 owns LOGINs, memberships, secret
references/values, quiescence, activation and deployment—never product code.

`TECHNICAL_IMPLEMENTATION_READY` means A1+A2+A3 PASS/as-built.
`DEPLOYMENT_CUTOVER_COMPLETE` means A4 PASS. Production activation remains a
separate prohibited state. A final non-mutating integration/ratification packet
must bind all exact as-built SHAs and prove the synthetic end-to-end joins.
```

Child artifacts:

- `tip_88c1_c6b_a1_server_identity_control_dispatch.md`;
- `tip_88c1_c6b_a2_windows_agent_profile_dispatch.md`;
- `tip_88c1_c6b_a3_broker_composition_dispatch.md`;
- `tip_88c1_c6b_a4_deployment_cutover_dispatch.md`.
- `tip_88c1_c6b_a5_integration_ratification_packet.md` (non-mutating).

| Round | Complete coverage | Findings | Applied delta | Cumulative non-convergence cause / method improvement |
| --- | --- | --- | --- | --- |
| 0 | Builder census across both repos | initial evidence only | v0.1 created | prevented invention by classifying every assumed surface as existing, extend-only, pattern-only, missing or deferred |
| 1 | three reviewers read full v0.1 and repo evidence | HOLD: 17 reports, 11 distinct root issues after dedupe | v0.2 closes crypto, nonce split, identity authority, persistence/locks/ACL, cutover, key custody, loopback, pepper lifecycle, exact routes and dependency hashes | causes: v0.1 described invariants but delegated executable choices and grouped proofs; method changed to contract-first patching by authority boundary, exact-byte manifest, and contradiction scan before next review |
| 2 | three full reviewers, v0.2 + planning + both repos | HOLD: 36 reports, 17 distinct after dedupe | v0.3 applies ceremony traces, constants, exact PoP, callability, sentinel, broker wrapper and named proof ledger | cumulative causes: patch-by-finding hid temporal/callability cycles and prose aggregates remained non-executable. Method improvement: trace each ceremony chronologically first, derive resources/ACL/schema/proofs second, and reduce ranged proofs to accountable rows |
| 3 | three full reviewers, v0.3 + planning + repos | HOLD: 38 reports, 19 distinct after dedupe | v0.4 replaces false shared-role separation, adds callable offline owners, fixes replacement/key DTO/secret parsing and typed broker contract | cumulative causes: appended remediation contradicted old authority and transitions lacked callers. Method improvement: normalized operation/caller and process-role graphs; authority gap for non-Windows Managed is explicitly retained rather than silently narrowed |
| 4 | three full reviewers, v0.4 + planning + repos | HOLD: independently deployable authority blockers persist | no monolithic patch | cumulative cause confirmed as invalid scope construction; reviewers unanimously require split |
| 5 | mandatory root-cause checkpoint | MONOLITH TERMINATED / PHASED CONVERGENCE SELECTED | v0.5 becomes non-executable umbrella and names A1-A4 | success criterion changes from patching one artifact to independent PASS and exact-byte rebind of four executable dispatches; no Round 6 on monolith |
| 6 | N/A — child loops only | terminated monolith | none | no Round 6 |
| 7 | N/A — child loops only | terminated monolith | none | no Round 7 |
| 8 | N/A — child loops only | terminated monolith | none | no Round 8 |
| 9 | N/A — child loops only | terminated monolith | none | no Round 9 |
| 10 | N/A — child loops only | terminated monolith | none | no Round 10 |

## 20. Terminal state

```text
PLANNING_BRIEF:      PASS / RATIFIED / SHA E9AF9C0C76A41736037DD2B6259F24F64569516C187D40A822300DAAB01A9696
DISPATCH:            NON_EXECUTABLE_ARCHITECTURE_UMBRELLA
IMPLEMENTATION:      NOT_AUTHORIZED
REAL_BIOMETRIC:      PROHIBITED
PRODUCTION:          PROHIBITED
STAGE_COMMIT_PUSH:   NOT_AUTHORIZED
```

This umbrella can never receive implementation authority. Only an independently
reviewed child exact SHA may receive a bounded Homeowner grant.

