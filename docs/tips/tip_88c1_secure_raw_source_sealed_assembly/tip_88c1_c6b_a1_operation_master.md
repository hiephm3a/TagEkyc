# TIP-88C1-C6B-A1 — Literal Operation Master

**Status:** REVIEW CANDIDATE — NOT IMPLEMENTATION AUTHORITY  
**Parent dispatch:** `tip_88c1_c6b_a1_server_identity_control_dispatch.md` v0.12  
**Bound verifier-pepper RRI:** `tip_88c1_c6b_a1_versioned_verifier_pepper_source_rri.md`; SHA-256 `FFECB648A0FA5F654C0765CC0927E38C95BD9489920F49A88E491E15B8E470F0`  
**Bound exact-material amendment:** `tip_88c1_c6b_a1_versioned_verifier_pepper_secretref_resolution_amendment.md`; SHA-256 `6CB725C1217DDDA5E59CD65393FCC62953F36FA7C003B71460B1DAD09377F09A`  
**Bound R01/R02 root-fingerprint RRI:** `tip_88c1_c6b_a1_r01_r02_root_request_fingerprint_rri.md`; SHA-256 `509D3951F08830539150B35A88E2BAF709CB1E297831A45D841E8927CBE5A42B`  
**Bound CRT1 exact-signed-bytes micro-RRI:** `tip_88c1_c6b_a1_crt1_exact_signed_bytes_contract_rri.md`; SHA-256 `EDA8703C3969F6A7043E496553FEC3E2B3AD72462C319EE10BE3491490489903`  
**Bound CRT1 proof-owner correction:** `tip_88c1_c6b_a1_crt1_proof_owner_project_boundary_correction.md`; SHA-256 `A79601F3944DAD71F12FCEABEE6B6BE6E1DACAC88C4E134BECF44B3D5519FFB5`  
**Profile:** Managed; synthetic/non-patient proof only

## 1. Wire atoms and common behavior

| Atom | Literal representation |
| --- | --- |
| `U` | lower-case RFC-4122 UUIDv4 `N`, exactly 32 ASCII hexadecimal characters |
| `V` | invariant-decimal signed Int64 in `[1,9223372036854775807]`, no sign or leading zero |
| `T` | UTC `yyyy-MM-ddTHH:mm:ss.fffffffZ` |
| `H32` | exactly 64 lower-case hexadecimal characters encoding 32 bytes |
| `S32` | exactly 43 canonical unpadded base64url characters encoding 32 bytes |
| `SIG64` | exactly 86 canonical unpadded base64url characters encoding 64-byte IEEE-P1363 P-256 signature |
| `SPKI91` | exactly 122 canonical unpadded base64url characters encoding 91-byte RFC-5480 P-256 SubjectPublicKeyInfo |

Every JSON object is case-sensitive and rejects unknown members, duplicate
members, trailing bytes, non-canonical UUID/time/base64 encodings and C0/C1
characters in strings. POST bodies are at most 16,384 bytes except configuration
publication, whose ceiling is 32,768 bytes. Runtime routes reject query strings.
Mutation HTTP routes carry exactly one `Idempotency-Key: U`; it never appears in
JSON. R19, R22, R23a and R23b are read-only and carry no `Idempotency-Key`.

Platform routes accept only `X-TagEkyc-Platform-Operator-Key`. Client routes
accept only the landed Client API-key header. Runtime routes accept only the five
CRT1 headers, except R05 enrollment redemption, which accepts only the bootstrap
secret plus ENROLL1 and has no CRT1 envelope. Mixed envelopes deny
`403 ACCESS_DENIED` without fallback.
No external HTTP/API caller may select, supply or override
`VerifierPepperVersion`; API-hosted issuance always uses the server-owned
`ICaptureRuntimeVerifierPepperSource.CurrentVersion`. Failure to resolve an
exact required version is a dependency failure mapped to `NOT_READY`, not a
credential/secret mismatch or `ACCESS_DENIED`.

For R03–R18, R20a, R20b, R21 and R24–R26, request fingerprint is:

```text
SHA256(
  UTF8("TAG-EKYC-A1-<physical-operation-id>-v1\n") ||
  UTF8(canonical route) || 0x00 ||
  SHA256(exact received closed JSON bytes) || 0x00 ||
  ordered secret-commitment fields, each encoded as
    one-byte field ordinal || SHA256(decoded secret bytes) || 0x00 ||
  UUID bytes of Idempotency-Key || 0x00 ||
  canonical authenticated actor-partition bytes)
```

The JSON hash is computed before deserialization and never requires
reserialization or property reordering. Secret commitments are additional
domain-separated inputs; they do not replace bytes inside JSON. The ordered
secret list is empty except R05 ordinal 1 bootstrap secret and R21 ordinal 1
presented capability secret. A newly generated response secret is never a
request-fingerprint input. The fingerprint stores only the outer 32-byte
digest.

Same operation identity plus equal fingerprint returns its durable non-secret
result. A different fingerprint returns `409 CONFLICT`. A secret appears only in
the first successful response; its exact replay returns
`409 EXISTING_MATCH_SECRET_UNAVAILABLE`. Errors contain only `code` and
`correlationId`. Malformed input is 400; authentication/pre-target lifecycle is
403; an authorized canonical target miss is 404; a state/revision race is 409;
missing schema, pepper, connection, sentinel or required port is 503.

### R01/R02 root-CLI request fingerprints

R01 and R02 do not use the HTTP/API JSON grammar above. They use the dedicated
ASCII-only UTF-8, LF-only, exactly-one-final-LF grammar bound by
`C6B-A1-R01-R02-REQUEST-FINGERPRINT-01`.

R01 is `SHA256` raw 32 bytes over exactly:

```text
TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1
<OperationId N>
<PrincipalId N>
<ExpiresAtUtc T>
```

It excludes generated secret, prefix, digest, pepper version, SecretRef and
`p_now_utc`. Golden: 145 bytes and
`BF365A4BCBFBAD1AE637F396887D20C3382DD996260DFD3A7A1B3A55D80429E7`.

R02 is `SHA256` raw 32 bytes over exactly:

```text
TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1
<OperationId N>
<CredentialId N>
<ExpectedRevision invariant-decimal>
DeploymentRevocation
```

It excludes `p_now_utc` and execution/result state. Golden: 136 bytes and
`7793EA47F3170F101BEE163EB3E5BF1DA5DBDF4133E6F57F5E37CDF58E986D07`.
The labels are distinct domains; the CLI calls the sole helpers in
`CaptureRuntimeVerifierCryptography.cs` and never computes a competing digest.

## 2. Literal ceremonies

Bootstrap and capability secrets are `S32`. Their digests are respectively:

```text
HMAC-SHA256(K_domain[V, BootstrapDigest],
  ASCII("bootstrap-digest-v1") || 0x00 || decoded-secret-32)
HMAC-SHA256(K_domain[V, CapabilityDigest],
  ASCII("capability-digest-v1") || 0x00 || decoded-secret-32)
```

ENROLL1 signs UTF-8, LF-only, final-LF bytes containing these lines in order:

```text
TAG-EKYC-ENROLL1
<BootstrapIssuanceId U>
<CandidateKeyId U>
<SPKI91>
<H32 thumbprint>
<T signed time>
<S32 nonce>
<Idempotency-Key U>
<H32 SHA256(decoded bootstrap secret)>
```

ROTATE1 signs UTF-8, LF-only, final-LF bytes containing these lines in order:

```text
TAG-EKYC-ROTATE1
<RotationId U>
<CaptureAgentId U>
<DeviceInstallationId U>
<stable CredentialId U>
<predecessor Generation V>
<CandidateKeyId U>
<SPKI91>
<H32 thumbprint>
<Idempotency-Key U>
```

Both signatures are `SIG64`. `CandidateKeyId` is key identity inside the
fingerprint, never operation identity. R13 operation identity is
`(RotationId, Idempotency-Key)`.

## 3. DTO catalogue

`RuntimeLifecycleRequest` contains `CaptureAgentId:U`, `ExpectedRevision:V`, and
`Reason`, one of `OperatorSuspension`, `OperatorReactivation`,
`OperatorRevocation`, `OperatorRetirement` matching the route.

`CaptureRuntimeCaptureArtifactRequest` contains `BindingId:U` and `Payload`.
Payload contains `ArtifactType` in `DocumentFrontImage|DocumentBackImage|
SelfieImage|LivenessMedia|NfcReadArtifact|FingerprintCapture|
DeviceCaptureMetadata`; `CaptureSource` in `MobileSdk|PcAgent|KioskAgent|
DeviceGateway|InternalAdapter|ExternalPreStaged`; nullable `ArtifactHash`,
`MetadataHash`, `RequestId`, `CorrelationId`, each at most 128 characters and C0/C1
free. It contains no caller identity.

`CaptureRuntimeEvidenceResultRequest` contains `BindingId:U` and `Payload`.
Payload contains `ResultType` in `CaptureQuality|DocumentOcr|NfcValidation|
FaceMatch|Liveness|FingerprintMatch|FraudRisk`; one to 32 unique
`InputCaptureArtifactIds`, each 1–128 characters; `Result` in `NotAvailable|
Passed|RetryRequired|FailedCaptureQuality|FailedIdentity|ReviewRequired|
TechnicalError|NotSupported`; nullable decimal `Confidence` in `[0,1]`; zero to
32 sorted unique `ReasonCodes`, each 1–64 characters; nullable
`RetryReasonCode:64`, `SanitizedSummaryRef:256`, `PayloadHash:128`; signature state
`PlaceholderUnverified|Signed`; `EngineName:1..128`, `EngineVersion:1..64`;
nullable `RequestId:128`, `CorrelationId:128`; and nullable NFC, face-match and
liveness decision bases. Every nested capture binding contains only nullable
`ChallengeHash:128`, `SessionId:128`, `CapturedAt:T`, `ArtifactHash:128`.
No object at any depth contains `CaptureAgentId`, `DeviceId`,
`ClientApplicationId` or `ProducerId`.

## 4. Thirty-three physical operation rows

Each SQL function below is `SECURITY DEFINER`, owned by
`tagekyc_raw_export_deployer`, and has `SET search_path=pg_catalog`. “B” is one
transaction containing the stated advisory locks, row locks, CAS, operation row,
event row and result. Lock numbers are global domains.

### Authentication and nonce helper catalogue

These helpers are physical SQL surfaces owned by
`tagekyc_raw_export_deployer`; each is `SECURITY DEFINER`, has
`SET search_path=pg_catalog`, and is implemented with R10 in transition
companion 04:

- `tagekyc.platform_operator_authenticate(text,timestamptz) RETURNS TABLE(credential_id uuid,principal_id uuid,secret_digest bytea,verifier_pepper_version integer,scopes text[],revision bigint)`;
- `tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) RETURNS TABLE(capture_agent_id uuid,device_installation_id uuid,credential_id uuid,generation bigint,public_verifier_spki bytea,public_key_thumbprint bytea,role_policy_id uuid,role_policy_revision bigint,runtime_revision bigint,installation_revision bigint,credential_revision bigint)`;
- `tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) RETURNS TABLE(result_code text,runtime_revision bigint,installation_revision bigint,credential_revision bigint,role_policy_id uuid,role_policy_revision bigint)`;
- `tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) RETURNS TABLE(deleted_count bigint)`.

Only `tagekyc_capture_runtime_authenticator` receives EXECUTE. Resolver output
is verifier material for fixed-time Application verification, never an
authentication result by itself. After signature verification, nonce claim
re-locks 1→10→20→30, rechecks the exact active current lineage, the frozen
role-policy revision and required route role, then commits N. Cleanup deletes
at most 10,000 rows and only when `PurgeAfterUtc < now`.

### R01 — platform provision CLI

- Callable/process/role: `TagEkyc.ApiKeyProvisioner platform-operator provision`;
  A4 deployment process; `tagekyc_capture_runtime_operator`.
- Input: `OperationId:U`, `PrincipalId:U`, `ExpiresAtUtc:T`; offline caller
  generates the 32-byte secret and supplies `KeyLookupPrefix:12 canonical
  base64url characters`, `SecretDigest:H32`, `VerifierPepperVersion:Int32>0`.
  The deployment operator MUST also supply the two canonical CLI arguments
  `--capture-runtime-verifier-pepper-version <positive Int32>` and
  `--capture-runtime-verifier-pepper-secret-ref <SecretRef>`. The first value
  is the exact CurrentVersion for this isolated invocation; the dedicated
public `CaptureRuntimeVerifierPepperSecretRefResolver` resolves the second
  with exact `env:`/raw-byte `file:` semantics. The tool accepts no plaintext-pepper
  argument, does not bind API-host configuration/DI, and does not use
  `TagEkyc:ApiKeyStore:PepperSecretRef`, `ApiKeyStorePepperResolver` or another
  Client API-key pepper source. It persists the same version whose resolved
  pepper produced `SecretDigest`, clears A1-owned buffers and never logs the
  pepper or complete SecretRef.
- Exact helper contract: both R01 and the API provider call public static
  `CaptureRuntimeVerifierPepperSecretRefResolver.Resolve(string)` and receive
  closed status `Success|ReferenceInvalid|MaterialUnavailable|MaterialInvalid`.
  Success alone carries a disposable `CaptureRuntimeVerifierPepperMaterialLease`
  exposing the exact 43 owned ASCII bytes as `ReadOnlyMemory<byte>`; every
  failure carries no material. The lease zeroes its mutable backing array on
  disposal. No secret string, caller-specific overload or raw exception/path
  detail crosses this boundary.
- SQL: `tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) RETURNS TABLE(result_code text,credential_id uuid,key_lookup_prefix text,secret_available boolean,expires_at_utc timestamptz,revision bigint)`.
- Secret boundary: SQL never receives or returns plaintext. The offline caller
  returns its mutable presented key only when SQL commits `Created` with
  `secret_available=true`, then zeroes it; replay returns false.
- Transaction/state: B; root-operation lock then credential row; insert Active.
- CLI result: exit 0 Created; 10 ExistingMatchSecretUnavailable; 20 Conflict; 30
  InvalidInput; 40 NotReady. No HTTP status or target disclosure.
- Proof: `PlatformCredentialAuthentication_HasNoClientPartition`,
  `SecretOnce_DigestsAndPepperRetirementAreClosed` and
  `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed`,
  `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` and the
  root-fingerprint cross-domain proof.

### R02 — platform revoke CLI

- Callable/process/role: `TagEkyc.ApiKeyProvisioner platform-operator revoke`;
  A4 deployment process; `tagekyc_capture_runtime_operator`.
- Input: `OperationId:U`, `CredentialId:U`, `ExpectedRevision:V`, reason
  `DeploymentRevocation`.
- SQL: `tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,credential_id uuid,state text,revision bigint,revoked_at_utc timestamptz)`.
- Transaction/state: B; root-operation then credential row; Active→Revoked.
- CLI result: exit 0 Revoked/replay; 20 Conflict; 30 InvalidInput; 40 NotReady;
  50 ResourceNotAvailable.
- Proof: `PlatformRootRevoke_IsIdempotentAndAudited`,
  `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` and the
  root-fingerprint cross-domain proof.

### R03 — bootstrap issue

- Route/auth/scope: `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances`;
  PlatformOperator; `operator.capture-runtime.manage`.
- Request: `RuntimeType="Managed"`, `TrustProfileId:U`,
  `TrustProfileRevision:V`, `RolePolicyId:U`, `RolePolicyRevision:V`,
  `ConfigurationId:U`, `ConfigurationRevision:V`, `ExpiresAtUtc:T`,
  `HandoffAttestationDigest:H32`; application generates the 32-byte bootstrap
  secret and supplies `KeyLookupPrefix:12 canonical base64url characters`,
  `SecretDigest:H32`, `VerifierPepperVersion:Int32>0`.
- SQL: `tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,secret_available boolean,expires_at_utc timestamptz,revision bigint)`.
- Secret boundary: SQL receives only verifier material. Application emits its
  mutable bootstrap secret only for committed `Created` with
  `secret_available=true`, never for replay, then zeroes it. The API-hosted
  Application obtains the pepper and `VerifierPepperVersion` exclusively from
  `ICaptureRuntimeVerifierPepperSource.CurrentVersion`; no HTTP field may
  select, supply or override that version.
- Locks/state: B; domain 5; catalog rows; insert Active issuance and management
  operation/event.
- Outcomes: 201 secret-once; replay 409 secret unavailable; 400/403/404/409/503.
- Proof: `BootstrapIssue_SecretOnceAndExactReplay`.

### R04 — bootstrap revoke

- Route/auth/scope: `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke`;
  PlatformOperator; runtime-manage.
- Request: `BootstrapIssuanceId:U`, `ExpectedRevision:V`, reason
  `OperatorRevocation`.
- SQL: `tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,state text,revision bigint,revoked_at_utc timestamptz)`.
- Locks/state: B; 5; Active→Revoked, one management event.
- Outcomes: 200/replay; 400/403/404/409/503.
- Proof: `BootstrapIssueRevoke_OneTerminalWinner`.

### R05 — enrollment redeem

- Route/auth: `POST /api/ekyc/capture-runtime/enrollments/redeem`; bootstrap
  secret plus ENROLL1; no platform, Client or CRT1 envelope.
- Request: `BootstrapIssuanceId:U`, `BootstrapSecret:S32`, `CandidateKeyId:U`,
  `PublicVerifierSpki:SPKI91`, `PublicKeyThumbprint:H32`, `SignedAtUtc:T`,
  `Nonce:S32`, `CandidateProof:SIG64`; Idempotency-Key is RedeemOperationId.
- SQL: `tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,device_installation_id uuid,credential_id uuid,generation bigint,runtime_revision bigint,installation_revision bigint,credential_revision bigint)`.
- Locks/state: B; 5→10→20→30; issuance Active→Redeemed; registration Active;
  installation Pending→Active; generation 1 Active; redemption operation/event.
- Outcomes: 201; exact replay 200; 400/403/409/503; invalid secret/PoP is 403.
- An Active issuance first touched after its horizon is atomically changed to
  `Expired`; the requested redemption operation persists `ResultCode=Expired`
  with sparse/null success lineage and one `Expired` event, then returns
  `TerminalizedExpiredAndDenied`. Exact replay returns that frozen terminal
  result before mutable rechecks and appends no second event.
- Proof: `Enrollment_AtomicActivationAndLostResponseReplay`.

### R06 — suspend

`POST /api/ekyc/operator/capture-runtimes/suspend`; PlatformOperator runtime-manage;
`RuntimeLifecycleRequest` with reason `OperatorSuspension`;
`tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)`; B
10→20→30; Active→Suspended; management operation/event; 200 or
400/403/404/409/503; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`.

### R07 — reactivate

`POST /api/ekyc/operator/capture-runtimes/reactivate`; PlatformOperator runtime-manage;
`RuntimeLifecycleRequest` with reason `OperatorReactivation`;
`tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)`; B
10→20→30; Suspended→Active; management operation/event; 200 or
400/403/404/409/503; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`.

### R08 — revoke

`POST /api/ekyc/operator/capture-runtimes/revoke`; PlatformOperator runtime-manage;
`RuntimeLifecycleRequest` with reason `OperatorRevocation`;
`tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)`; B
10→20→30; Active|Suspended→Revoked; management operation/event; 200 or
400/403/404/409/503; `RuntimeRevokeRetire_TerminalWins`.

### R09 — retire

`POST /api/ekyc/operator/capture-runtimes/retire`; PlatformOperator runtime-manage;
`RuntimeLifecycleRequest` with reason `OperatorRetirement`;
`tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)`; B
10→20→30; Revoked→Retired; management operation/event; 200 or
400/403/404/409/503; `RuntimeRevokeRetire_TerminalWins`.

### R10 — credential revoke

- Route/auth: `POST /api/ekyc/operator/capture-runtimes/credentials/revoke`;
  PlatformOperator runtime-manage.
- Request: Agent, installation, credential UUIDs; `Generation:V`,
  `ExpectedCredentialRevision:V`, reason `CredentialCompromise`.
- SQL: `tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,credential_id uuid,generation bigint,state text,revision bigint,revoked_at_utc timestamptz)`.
- Locks/state: B 10→20→30; the exact current Active generation and its Active
  installation atomically become Revoked, retaining the lineage pointer; the
  management operation/event and deferred current-generation invariant commit
  in the same transaction.
- Outcomes: 200/replay; 400/403/404/409/503.
- Proof: `CredentialRevoke_RacesRotationWithoutSplitGeneration`.

### R11 — rotation authorize

- Route/auth: `POST /api/ekyc/operator/capture-runtimes/credential-rotations/authorize`;
  PlatformOperator with `runtime-manage`.
- Request: `CaptureAgentId:U`, `InstallationId:U`, `CredentialId:U`,
  `CurrentGeneration:V`, `ExpectedCredentialRevision:V`, `ExpiresAtUtc:T`;
  header `Idempotency-Key:U`.
- SQL: `tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) RETURNS TABLE(result_code text,rotation_id uuid,capture_agent_id uuid,installation_id uuid,predecessor_credential_id uuid,current_generation bigint,state text,revision bigint,expires_at_utc timestamptz)`.
- `rotation_id` is a server-generated opaque UUID and is exactly the durable
  `RotationAuthorizationId`; it is never copied from `Idempotency-Key`.
- Transaction/locks/state: B, lock ranks 10→20→30→60; insert the Active
  authorization, management operation and event atomically.
- Outcomes: 201 first commit, 200 exact replay, or 400/403/404/409/503.
- Proof: `RotationAuthorization_RevokeExpiryOneWinner`.

### R12 — rotation revoke

- Route/auth: `POST /api/ekyc/operator/capture-runtimes/credential-rotations/revoke`;
  PlatformOperator with `runtime-manage`.
- Request: `RotationId:U`, `ExpectedRevision:V`, reason literal
  `OperatorRevocation`; header `Idempotency-Key:U`.
- SQL: `tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,rotation_id uuid,state text,revision bigint,revoked_at_utc timestamptz)`.
- Transaction/locks/state: B, lock ranks 10→20→30→60; Active→Revoked with
  management operation and event atomically.
- Outcomes: 200 first commit or exact replay, or 400/403/404/409/503.
- Proof: `RotationAuthorization_RevokeExpiryOneWinner`.

### R13a — predecessor completion

- Route: `POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete`.
- Request: `CandidateKeyId:U`, `SuccessorPublicVerifierSpki:SPKI91`,
  `SuccessorPublicKeyThumbprint:H32`, `SuccessorProof:SIG64`.
- Auth: predecessor CRT1, `CredentialRotation` role, ROTATE1 envelope.
- SQL: `tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) RETURNS TABLE(result_code text,credential_id uuid,generation bigint,candidate_key_id uuid,public_key_thumbprint bytea,credential_revision bigint,installation_revision bigint,rotation_revision bigint)`.
- Transaction/locks/state: B, ranks 10→20→30→60; predecessor Active→Rotated,
  successor Pending→Active, lineage pointer, completion operation and event atomic.
- The assigned next-generation role-policy revision must exist and have
  `EffectiveAtUtc <= now`; a future-effective assignment cannot activate a successor.
- Outcomes: 200; 400/403/404/409/503.
- If the locked Active authorization is past its horizon, completion atomically
  materializes it as `Expired`, persists the requested completion operation as
  `ResultCode=Expired` with null successor result fields and one `Expired`
  event, and returns `TerminalizedExpiredAndDenied`. Exact predecessor replay
  returns the frozen terminal result; no successor is created.
- Proof: `Rotation_DualProofAtomicCutoverAndSameRouteReplay`.

### R13b — successor replay

- Route: `POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete`.
- Request: `CandidateKeyId:U`, `SuccessorPublicVerifierSpki:SPKI91`,
  `SuccessorPublicKeyThumbprint:H32`, `SuccessorProof:SIG64`.
- Auth: Active/current successor CRT1; no `CredentialRotation` role.
- SQL: `tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) RETURNS TABLE(result_code text,credential_id uuid,generation bigint,candidate_key_id uuid,public_key_thumbprint bytea,credential_revision bigint,installation_revision bigint,rotation_revision bigint)`.
- Transaction/locks/state: nonce transaction N commits; business lookup is
  read-only and matches the exact committed operation tuple and fingerprint.
- Outcomes: 200 exact replay; generic 403 on mismatch; 503 on dependency failure.
- Proof: `Rotation_SuccessorWithoutRoleCanReplayOnlyExactCommittedOperation`.

### R14 — role-policy assignment

`POST /api/ekyc/operator/capture-runtimes/role-policies/assign`;
PlatformOperator `operator.capture-runtime.manage`;
`CaptureRuntimeRolePolicyAssignmentRequest { AgentId:U, RolePolicyId:U,
RolePolicyRevision:V, ExpectedRuntimeRevision:V }`; request AgentId uuid, RolePolicyId uuid,
RolePolicyRevision bigint, ExpectedRuntimeRevision bigint; SQL
`tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,role_policy_id uuid,role_policy_revision bigint,runtime_revision bigint)`; B 10→40;
response `CaptureRuntimeRolePolicyAssignmentResponse { CaptureAgentId:U,
RolePolicyId:U, RolePolicyRevision:V, RuntimeRevision:V }`; 200 or
400/403/404/409/503; `RoleAssignment_IsNextGenerationOnly`.

### R15 — configuration assignment

`POST /api/ekyc/operator/capture-runtimes/configurations/assign`;
PlatformOperator `operator.capture-runtime.manage`;
`CaptureRuntimeConfigurationAssignmentRequest { AgentId:U, ConfigurationId:U,
ConfigurationRevision:V, ExpectedRuntimeRevision:V, OverrideId:U? }`; request AgentId uuid, ConfigurationId uuid,
ConfigurationRevision bigint, ExpectedRuntimeRevision bigint, nullable OverrideId uuid; SQL
`tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,configuration_revision bigint,override_id uuid,runtime_revision bigint)`; B 10→40;
response `CaptureRuntimeConfigurationAssignmentResponse { CaptureAgentId:U,
ConfigurationId:U, ConfigurationRevision:V, OverrideId:U?,
RuntimeRevision:V }`; 200 or 400/403/404/409/503;
`ConfigurationAssignment_ReducesAndEtagIsOpaque`.

### R16 — trust-profile publication

`POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish`;
PlatformOperator `operator.capture-runtime.manage`;
`CaptureRuntimeTrustProfilePublicationRequest { CatalogId:U,
ExpectedHeadRevision:V, EffectiveAtUtc:T, ExpiresAtUtc:T,
RuntimeType:"Managed", RetainedRawEnabled:bool, AllowTrustedEvidence:bool,
RequireHandoffAttestation:bool }`; request CatalogId uuid, ExpectedHeadRevision bigint,
EffectiveAtUtc timestamptz, ExpiresAtUtc timestamptz greater than EffectiveAtUtc,
RuntimeType text fixed Managed, RetainedRawEnabled boolean,
AllowTrustedEvidence boolean, RequireHandoffAttestation boolean; SQL
`tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)`; B 40;
response `CaptureRuntimeCatalogPublicationResponse { CatalogId:U, Revision:V,
HeadRevision:V }`; 201/200 or 400/403/404/409/503;
`CatalogPublication_ExpectedHeadAndRollbackAsRevision`.

### R17 — role-policy publication

`POST /api/ekyc/operator/capture-runtime-control/role-policies/publish`;
PlatformOperator `operator.capture-runtime.manage`;
`CaptureRuntimeRolePolicyPublicationRequest { CatalogId:U,
ExpectedHeadRevision:V, EffectiveAtUtc:T, Roles:string[] }`; request CatalogId uuid, ExpectedHeadRevision bigint,
EffectiveAtUtc timestamptz and sorted unique closed Roles text[]; SQL
`tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)`; B 40;
response `CaptureRuntimeCatalogPublicationResponse { CatalogId:U, Revision:V,
HeadRevision:V }`; 201/200 or 400/403/404/409/503;
`CatalogPublication_ExpectedHeadAndRollbackAsRevision`.

### R18 — configuration publication

`POST /api/ekyc/operator/capture-runtime-control/configurations/publish`;
PlatformOperator `operator.capture-runtime.manage`; header `Idempotency-Key:U`;
request DTO is `CaptureRuntimeConfigurationPublicationRequest`; the closed request
fields and PostgreSQL types are exactly `CatalogId uuid`,
`ExpectedHeadRevision bigint`, `EffectiveAtUtc timestamptz`,
`ExpiresAtUtc timestamptz`, `RawExportEnabled boolean`,
`PlaintextBudgetSeconds integer`,
`RawExportSourceClaimSafetyMarginMilliseconds integer`,
`CaptureAgentConfigurationPollingIntervalSeconds integer`,
`RawExportSourceMaximumChipDg2PortraitBytes integer`,
`RawExportSourceMaximumLiveSelfieImageBytes integer`,
`RawExportCaptureMaximumAggregatePlaintextBytesPerHost bigint`,
`RawExportCustodyMaximumPlaintextWindowBytesPerStream integer`,
`RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment bigint`, and
`RawExportIngressMaximumPreAdmissionBufferedBytes integer`. The full function is
`tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)`.
Response is `CaptureRuntimeCatalogPublicationResponse { CatalogId:U,
Revision:V, HeadRevision:V }`.
Transaction B takes catalog lock rank 40 and atomically appends the configuration
revision, management operation and event. Outcomes are 201 first commit, 200 exact
replay, or 400/403/404/409/503. Proofs are
`ConfigurationAssignment_ReducesAndEtagIsOpaque` and
`CatalogPublication_ExpectedHeadAndRollbackAsRevision`.

### R19 — readiness

`GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness` uses
PlatformOperator `runtime-manage`; it has no body, query, or `Idempotency-Key`.
`tagekyc.capture_runtime_read_readiness(uuid,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,runtime_state text,runtime_revision bigint,installation_state text,installation_revision bigint,credential_state text,credential_generation bigint,credential_revision bigint,trust_profile_revision bigint,role_policy_revision bigint,configuration_revision bigint,required_pepper_versions integer[],nonce_store_ready boolean,cutover_sentinel_ready boolean,database_ready boolean)` is read-only. SQL reports only referenced pepper version IDs and DB-observable facts; it never claims external secret availability. The application resolves the finite union `{ ICaptureRuntimeVerifierPepperSource.CurrentVersion } UNION { required_pepper_versions }` through that exact versioned source and composes `platform_pepper_ready` and final `is_ready`, failing closed as `NOT_READY` when any member is absent, invalid or unavailable. A historical version may be removed only when it is neither CurrentVersion nor present in SQL `required_pepper_versions`; deleting a still-referenced entry never retires it successfully. Outcomes: 200/403/404/503. Proof:
`Readiness_ValidatesSchemaAclPepperNonceAndConnections`.

### R20a — capability issue

`POST /api/ekyc/verification-sessions/{sessionId}/capture-capabilities`; landed
Client session-owner authentication; body exactly `{ "Action":"Issue" }` and
header `Idempotency-Key:U`; B locks 70→80 and inserts ActiveUnbound; 201 returns
CapabilityId, secret once, expiry, state and revision; exact replay is 409
`EXISTING_MATCH_SECRET_UNAVAILABLE` with only nonsecret metadata; other outcomes
400/403/404/409/503;
application generates the 32-byte secret and supplies a 12-character lookup
projection, 32-byte domain-separated digest and positive pepper version. The
API-hosted Application resolves and stamps exactly
`ICaptureRuntimeVerifierPepperSource.CurrentVersion`; neither the Client nor
another HTTP caller supplies or overrides it;
`tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) RETURNS TABLE(result_code text,capture_capability_id uuid,secret_available boolean,expires_at_utc timestamptz,state text,revision bigint)`;
SQL derives expiry as `p_now + interval '5 minutes'` and never receives/returns
plaintext; application emits and then zeroes it only for committed Created;
if an older ActiveUnbound capability is already expired, Issue first records
one standalone `Expire` operation/event and may then create its successor in
the same transaction; exact Issue replay uses its frozen result and never
re-emits a secret;
`CapabilityIssue_SecretOnceAndExactReplay`.

### R20b — capability replace

`POST /api/ekyc/verification-sessions/{sessionId}/capture-capabilities`; landed
Client session-owner authentication; body exactly `{ "Action":"Replace",
"CurrentCapabilityId":"<U>", "ExpectedRevision":<V> }` and header
`Idempotency-Key:U`; B locks 70→80, revokes the exact ActiveUnbound predecessor
and inserts one successor; 201 returns CapabilityId, secret once, expiry, state
and revision; exact replay is 409 `EXISTING_MATCH_SECRET_UNAVAILABLE` with only
nonsecret metadata; other outcomes 400/403/404/409/503;
application generates the successor UUID and 32-byte secret and supplies a
12-character lookup projection, 32-byte domain-separated digest and positive
pepper version selected exactly from
`ICaptureRuntimeVerifierPepperSource.CurrentVersion`; no external HTTP/API
caller selects or overrides it;
`tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) RETURNS TABLE(result_code text,capture_capability_id uuid,secret_available boolean,expires_at_utc timestamptz,state text,revision bigint)`;
SQL derives expiry as `p_now + interval '5 minutes'` and never receives/returns
plaintext; application emits and zeroes it only for committed Created;
an expired exact predecessor is first materialized as `Expired`; the requested
Replace operation then persists `ResultCode=Expired` with one `Expired` event,
returns `TerminalizedExpiredAndDenied`, and exact replay creates no successor;
`CapabilityReplace_ExpectedCurrentAndBoundWins`.

The Application resolves verifier material only through the R21-owned internal
`tagekyc.capture_runtime_resolve_capability_verifier(uuid) RETURNS TABLE(secret_digest bytea,verifier_pepper_version integer)`, performs the keyed digest and fixed-time comparison outside SQL, then passes only `p_capability_secret_verified boolean` to R21. The resolver is executable only by `tagekyc_capture_runtime_application`; a false result is generic denial and R21 always re-locks and rechecks the exact capability row. Time-derived ActiveUnbound expiry is materialized by internal
`tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) RETURNS TABLE(result_code text,state text,revision bigint)`, where the final UUID is the deterministic expiry operation identity; it locks 70→80 and writes one `Expire` operation plus one `Expired` event. SQL derives, and persists in that operation, `SHA-256(UTF8("tip-88c1-c6b-a1-capability-expiry-v1") || uuid_send(VerificationSessionId) || uuid_send(CaptureCapabilityId) || timestamptz_send(the durable capability ExpiresAtUtc) || uuid_send(deterministic expiry operation id))`. Replay requires both this fingerprint and `ResultCapabilityId` to match; reusing the same operation identity for another capability or a changed durable deadline returns `Conflict` without mutation.
Both helper signatures are owned by `tagekyc_raw_export_deployer`, executable
only by `tagekyc_capture_runtime_application`, covered by
`Bind_OneWinnerSameLineageReplayNoTakeover`,
`CapabilityIssue_SecretOnceAndExactReplay` and
`DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown`, and dropped by the
capability/binding companion Down. Expiry replay additionally requires the
stored `ResultCapabilityId` to equal the requested capability; reuse of one
deterministic operation UUID for another capability returns conflict.

### R21 — bind

`POST /api/ekyc/capture-runtime/executions/bind`; CRT1 `Bind`; body exactly
`CaptureCapabilityId:U`, `CaptureCapabilitySecret:S32`, `BindOperationId:U`, with
`BindOperationId` equal to the sole `Idempotency-Key` header; state-changing B
locks 10→20→30→70→80; 201 first bind, 200 exact nonsecret replay, or
400/403/404/409/503;
`tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,runtime_revision bigint,installation_revision bigint,credential_revision bigint,capability_revision bigint)`;
an expired exact target is materialized under the same locks and the requested
Bind operation persists `ResultCode=Expired` plus one `Expired` event; it
returns `TerminalizedExpiredAndDenied`, creates no binding, and exact replay
returns the frozen terminal result;
`Bind_OneWinnerSameLineageReplayNoTakeover`.

### R22 — binding reconcile

`POST /api/ekyc/capture-runtime/executions/reconcile`; CRT1 `Bind`; body exactly
`CaptureCapabilityId:U`, `BindOperationId:U`; no `Idempotency-Key`; nonce N then read-only exact operation lookup;
200 or 400/403/404/503; `BindingReconcile_IsReadOnlyAndNonEnumerating`.
SQL is `tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,runtime_revision bigint,installation_revision bigint,credential_revision bigint,capability_revision bigint)`.

### R23a — configuration 200

`GET /api/ekyc/capture-runtime/self/configuration`; CRT1 `Configuration`; no
body, query or `Idempotency-Key`; missing or unequal `If-None-Match`; read-only configuration resolution;
200 JSON+ETag or 400/403/503; `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag`.
SQL is `tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,configuration_revision bigint,effective_at_utc timestamptz,expires_at_utc timestamptz,raw_export_enabled boolean,plaintext_budget_seconds integer,raw_export_source_claim_safety_margin_milliseconds integer,capture_agent_configuration_polling_interval_seconds integer,raw_export_source_maximum_chip_dg2_portrait_bytes integer,raw_export_source_maximum_live_selfie_image_bytes integer,raw_export_capture_maximum_aggregate_plaintext_bytes_per_host bigint,raw_export_custody_maximum_plaintext_window_bytes_per_stream integer,raw_export_custody_max_aggregate_bytes_per_deployment bigint,raw_export_ingress_maximum_pre_admission_buffered_bytes integer)`. The shortened SQL alias maps to the full C# property `RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment`. Application canonical-serializes the closed 200 document and computes a quoted unpadded-base64url SHA-256 ETag. ETag is not stored and is not an encoding of ConfigurationRevision.

### R23b — configuration 304

`GET /api/ekyc/capture-runtime/self/configuration`; CRT1 `Configuration`; no
body, query or `Idempotency-Key`; equal quoted opaque `If-None-Match`; read-only configuration resolution;
304 empty body+ETag or 400/403/503; `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag`.
SQL uses the identical five-argument resolver and return shape from R23a. Application computes the same canonical-content SHA-256 ETag and returns 304 with an empty body only when the exact quoted `If-None-Match` matches. No DB ETag column or secret is authorized.

### R24 — runtime capture artifact

Route `/api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts`; CRT1
`CaptureObservation`; request is exactly
`CaptureRuntimeCaptureArtifactRequest { Guid BindingId; CaptureRuntimeCaptureArtifactPayload Payload; }`, with the payload fields and closed enums listed in §3 and no caller identity selector. `ICaptureRuntimeExecutionService.AppendCaptureArtifactAsync(bindingId, request, cancellationToken)` validates capability, runtime, installation and credential under business transaction B with lock ranks 10→20→30→70→80, derives the accepted artifact identity, then invokes the existing authority-neutral append boundary in that same B transaction. This operation creates no new public SQL function. Response is exactly the landed `CaptureArtifactSubmissionResponseDto { string CaptureArtifactId; string VerificationSessionId; string? ArtifactHash; bool Accepted; string SessionState; string CorrelationId; bool Deduplicated = false; }`. Outcomes are 200/400/403/404/409/503. Proofs:
`CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction` and
`RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors`.

### R25 — runtime evidence

Route `/api/ekyc/verification-sessions/{id}/evidence-results`; CRT1
`TrustedEvidence`; request is exactly
`CaptureRuntimeEvidenceResultRequest { Guid BindingId; CaptureRuntimeEvidenceResultPayload Payload; }`, with the payload fields, nested decision bases and closed enums listed in §3 and no caller identity selector. `ICaptureRuntimeExecutionService.AppendEvidenceResultAsync(verificationSessionId, request, cancellationToken)` validates capability, runtime, installation and credential under business transaction B with lock ranks 10→20→30→70→80, derives the accepted evidence identity, then invokes the existing authority-neutral evidence append boundary in that same B transaction. This operation creates no new public SQL function. Response is exactly the landed `EvidenceResultSubmissionResponseDto { string EvidenceResultId; bool Accepted; string SessionState; string? NextAction; bool Deduplicated = false; RawCaptureAcceptanceDto? RawCaptureAcceptance = null; }`, with landed `RawCaptureAcceptanceDto { Guid CaptureArtifactId; Guid CaptureAcceptanceId; int CaptureRevision; string RawClass; }`. Outcomes are 200/400/403/404/409/503. Proofs:
`EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity` and
`RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors`.

### R26 — Client cancellation

Client-authenticated `/api/ekyc/verification-sessions/{id}/cancel`. Request
`Reason` is nullable: Application maps null/empty to its landed default; a
present reason is the landed non-sensitive token grammar with maximum 64.
Nullable `RequestId` and `CorrelationId` are resolved by landed `FirstNonEmpty`
precedence to the request value, then stored session value, then respectively
`req-{sessionId:N}` and `corr-{sessionId:N}`; the normalized SQL inputs are
non-null and at most 128. The extended
finalization B locks 70→80 and invokes
`tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,uuid,bytea,timestamptz,text,uuid) RETURNS TABLE(result_code text,verification_session_id uuid,state text,request_id text,correlation_id text)`. The final two inputs are authenticated Client API-key prefix and caller-generated audit-event UUID. SQL derives `ClientApplicationId` from the authenticated Client argument, `ActorType='ClientApplication'`, `ActorId` from that prefix, `EventType='SESSION_CANCELLED'`, the landed payload-hash literal `sha256:localdev-session-cancelled`, payload ref from reason and occurrence from `p_now`.
It atomically terminalizes session and ActiveUnbound or Bound capability.
If the live capability is already past its horizon, its standalone Expire
operation/event is materialized first; Cancel then terminalizes the session
without changing that capability from `Expired` to `Revoked`.
Every success writes one `Cancel` operation and one `Cancelled` event; the
event capability/revisions are null before issue, or identify the atomically
revoked live capability when present.
Outcomes 200/400/403/404/409/503. Proof:
`SessionCancel_TerminalizesCapabilityAtomically`.

### R27a — raw auth handoff

`POST /api/ekyc/raw-export/source-ingress`; CRT1 `RawIngress`; the five CRT1
headers sign the exact method, path, closed ingress metadata digest and claimed
body digest. Authentication transaction N validates current runtime,
installation, credential generation and role policy, inserts the nonce, and
commits before any body read. A1 invokes
`ICaptureRuntimeRawIngressAdmission.AdmitAsync(CaptureRuntimeRawIngressAdmissionContext, Stream, CancellationToken)` exactly once with the still-unread
`HttpRequest.Body` and the immutable context fields defined in §6. A3 owns B,
binding/acceptance/R1 and the domain result; A1 maps only the closed §6 result to
the fixed HTTP status/body. Proof:
`RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce`.

### R27b — raw auth denial

`POST /api/ekyc/raw-export/source-ingress`; malformed closed metadata returns 400,
failed CRT1 `RawIngress` authentication or current-lineage/role validation returns
403, and missing readiness dependency returns 503. Transaction N either rejects
without nonce insertion or commits only a successfully verified nonce. Every
denial performs zero request-body reads, zero drains and zero calls to
`ICaptureRuntimeRawIngressAdmission`. Error bodies contain only `code` and
`correlationId`. Proof: `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3`.

### R28a — Prepared startup

SQL is `tagekyc.capture_runtime_read_cutover_state(text) RETURNS TABLE(profile text,state text,revision bigint,prepared_at_utc timestamptz,activated_at_utc timestamptz,activated_by_credential_id uuid)`. Prepared sentinel maps only legacy routes before listener start;
`PreparedRouteSet_IsLegacyOnly`.

### R28b — Activated startup

SQL is `tagekyc.capture_runtime_read_cutover_state(text) RETURNS TABLE(profile text,state text,revision bigint,prepared_at_utc timestamptz,activated_at_utc timestamptz,activated_by_credential_id uuid)`. Activated sentinel plus schema/ACL/pepper/connections/A3 readiness maps only runtime routes before listener start; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3`.

## 4.1 Canonical transition lock matrix

Every listed advisory key is the one-bigint result of
`hashtextextended(canonical UUID text, rank)` and every advisory acquisition
precedes every `FOR UPDATE`. Rows are acquired only in the shown order.

| Operation | Principal check before replay/disclosure | Advisory order | Row-lock order |
| --- | --- | --- | --- |
| R06 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, IdempotencyKey/60 | registration, installation, credential generation |
| R07 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, IdempotencyKey/60 | registration, installation, credential generation |
| R08 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, IdempotencyKey/60 | registration, installation, credential generation |
| R09 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, IdempotencyKey/60 | registration, installation, credential generation |
| R10 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, Installation/20, Credential/30, IdempotencyKey/60 | registration, installation, credential generation |
| R11 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, Installation/20, Credential/30, IdempotencyKey/60 | registration, installation, credential generation |
| R12 | current PlatformOperator, exact `operator.capture-runtime.manage` | RotationAuthorization/60 | rotation authorization |
| R13a | current predecessor CRT1 plus ROTATE1 successor proof | Agent/10, Installation/20, Credential/30, RotationAuthorization/60 | rotation authorization, registration, installation, predecessor credential generation |
| R13b | current successor CRT1 | nonce transaction only | none; stable read |
| R14 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, RolePolicyCatalog/40 | role-policy revision, registration |
| R15 | current PlatformOperator, exact `operator.capture-runtime.manage` | Agent/10, ConfigurationCatalog/40 | configuration revision, override when supplied, registration |
| R16 | current PlatformOperator, exact `operator.capture-runtime.manage` | TrustProfileCatalog/40 | trust-profile head |
| R17 | current PlatformOperator, exact `operator.capture-runtime.manage` | RolePolicyCatalog/40 | role-policy head |
| R18 | current PlatformOperator, exact `operator.capture-runtime.manage` | ConfigurationCatalog/40 | configuration head |

## 4.2 Duplicated-lineage and audit-field matrix

| Child tuple | Authoritative parent tuple | Enforcement |
| --- | --- | --- |
| binding CapabilityId, SessionId, ClientId | capability CapabilityId, SessionId, ClientId | composite FK |
| binding InstallationId, AgentId | installation InstallationId, AgentId | composite FK |
| binding InstallationId, CredentialId, Generation | credential generation InstallationId, CredentialId, Generation | composite FK |
| bootstrap result InstallationId, AgentId | installation InstallationId, AgentId | deferred composite FK |
| redemption result InstallationId, AgentId | installation InstallationId, AgentId | deferred composite FK |
| redemption result InstallationId, CredentialId, Generation | credential generation InstallationId, CredentialId, Generation | composite FK |
| rotation authorization InstallationId, AgentId | installation InstallationId, AgentId | composite FK |
| rotation completion AuthorizationId, InstallationId, CredentialId, PredecessorGeneration | authorization AuthorizationId, InstallationId, CredentialId, CurrentGeneration | composite FK |
| management event actor, kind, idempotency, target kind, target ID, after revision | management operation same fields and result revision | deferred null-safe equality trigger |
| capability event client, session, kind, idempotency, capability, runtime Agent, runtime installation, after revision | capability operation same fields and result fields | deferred null-safe equality trigger |
| redemption event issuance, operation, Agent, installation, credential, generation | redemption operation same fields | deferred null-safe equality trigger |
| rotation event authorization, operation, credential, predecessor, successor | rotation operation same fields | deferred null-safe equality trigger |
| root event operation, principal, target credential, after revision | root operation same fields and result revision | deferred null-safe equality trigger |

## 5. Required audit and architecture proofs

Every mutating row above asserts one operation row, one event row, no secret in
either, and rollback of business mutation when event insertion fails. Reflection
tests recursively reject caller identity members from both runtime wrapper object
graphs. ACL tests execute every full signature using the sole allowed ephemeral
role and prove PUBLIC, broker, unrelated A1 roles and table DML are denied.

## 6. Normative integration contracts

### CRT1 exact signed-bytes ownership

The API HTTP parser is the sole server-side CRT1 preimage constructor. It
validates the exact wire lexemes and creates one dedicated immutable-view
buffer containing the already-ratified CRT1 bytes. The authenticator verifies
that supplied byte sequence and never reconstructs it.

```csharp
public sealed record CaptureRuntimeSignedRequest(
    Guid CredentialId,
    long CredentialGeneration,
    DateTimeOffset SignedAtUtc,
    byte[] Nonce,
    byte[] Signature,
    string RequiredRole,
    ReadOnlyMemory<byte> ExactSignedPreimage);
```

Parsed fields and `ExactSignedPreimage` derive from the same validated HTTP
envelope; malformed/noncanonical header text rejects before the contract exists.
Timestamp and nonce use their exact validated header text in the preimage, not
reformatting/re-encoding. RawIngress preimage construction uses the declared
commitment and reads zero request-body bytes. `Method`, `CanonicalPath` and
`BodyDigest` are not authenticator reconstruction inputs.

### A1-to-A3 port and HTTP ownership

The sole A1-to-A3 port is declared in
`src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` exactly as follows:

```csharp
public interface ICaptureRuntimeRawIngressAdmission
{
    ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context,
        Stream body,
        CancellationToken cancellationToken);
}

public sealed record CaptureRuntimeRawIngressAdmissionContext(
    Guid CaptureAgentId,
    Guid DeviceInstallationId,
    Guid CredentialId,
    long CredentialGeneration,
    Guid RolePolicyId,
    long RolePolicyRevision,
    DateTimeOffset SignedAtUtc,
    byte[] Nonce,
    byte[] SignedEnvelopeFingerprint,
    long AgentConfigurationRevision,
    Guid VerificationSessionId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string RawClass,
    Guid IngressIdempotencyKey,
    string MediaType,
    long ClaimedPlaintextLength,
    string ClaimedPlaintextDigest,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset PlaintextRetentionStartedAtUtc,
    DateTimeOffset PlaintextRetentionExpiresAtUtc,
    long PlaintextRetentionBudgetSeconds);

public enum CaptureRuntimeRawIngressOutcome
{
    Available,
    AlreadyAvailable,
    EvaluationInProgress,
    BindingInvalid,
    CapacityUnavailable,
    TransportProtocolInvalid
}

public sealed record CaptureRuntimeRawIngressAdmissionResult(
    CaptureRuntimeRawIngressOutcome Outcome,
    Guid? SourceArtifactId,
    string? CurrentSourceState,
    string? CurrentDisposition,
    DateTimeOffset? RetryNotBeforeUtc);
```

All byte arrays are defensive copies at the port boundary. The context is
authentication evidence only. It contains no ClientApplicationId, ApiKeyId,
resolved BindingId, session result, acceptance result, capacity result, broker
token or source-authority decision. A1 commits transaction N before invoking the
port and passes `HttpRequest.Body` without opening, reading or draining it. A3
selects the closed domain outcome and optional nonsecret fields; A1 alone maps
that result to HTTP:

| Domain outcome | HTTP status | Exact body rule |
| --- | --- | --- |
| `Available` | 200 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_AVAILABLE` and SourceArtifactId required |
| `AlreadyAvailable` | 200 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE` and SourceArtifactId required |
| `EvaluationInProgress` | 409 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS`; RetryNotBeforeUtc optional |
| `BindingInvalid` | 403 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_BINDING_INVALID`; all optional fields null |
| `CapacityUnavailable` | 503 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`; all optional fields null |
| `TransportProtocolInvalid` | 400 | `CaptureAgentFinalResult` with `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`; all optional fields null |

An undefined enum value, invalid required/forbidden field combination or port
exception is 503 `NOT_READY`, discloses no A3 fact and never causes A1 to read or
drain the body. No Application or A3 type references HttpRequest, HttpContext,
IResult or an HTTP status code.

### Versioned Capture Runtime verifier-pepper source

`src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` owns the sole
API-hosted pepper abstraction:

```csharp
public enum CaptureRuntimeVerifierPepperDomain
{
    PlatformCredential,
    BootstrapDigest,
    CapabilityDigest
}

public interface ICaptureRuntimeVerifierPepperSource
{
    int CurrentVersion { get; }

    ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(
        int version,
        CaptureRuntimeVerifierPepperDomain domain,
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeVerifierPepperLease : IDisposable
{
    int Version { get; }
    CaptureRuntimeVerifierPepperDomain Domain { get; }
    ReadOnlyMemory<byte> Key { get; }
}
```

`CurrentVersion` supplies every new API-hosted R03/R20 verifier digest.
Existing rows are verified only with their persisted `VerifierPepperVersion`;
`TryResolveAsync(v, domain)` resolves exactly that version and domain, with no
current-version or Client API-key-pepper fallback. The lease exposes only the
derived 32-byte domain key; Application never receives the master or HKDF PRK.
The provider owns the mutable lease backing buffer and zeroes it on `Dispose`;
Application sees neither configuration nor `SecretRefResolver`.

`src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeVerifierCryptography.cs`
is the single pure Application authority for canonical secret decoding and
encoding, HKDF-SHA256 domain derivation, verifier-digest construction,
fixed-time comparison and owned-buffer zeroing. It has no configuration,
SecretRef, database, Api, Infrastructure or API-key dependency. The API-host
provider and offline R01 adapter both call this same implementation.

The canonical textual codec applies identically to verifier-pepper master
material, the platform operator key payload after `teo_`, `BootstrapSecret`
and `CaptureCapabilitySecret`. Each value is exactly 43 ASCII characters in
`[A-Za-z0-9_-]{43}`, contains no `=`, whitespace, CR, LF, tab, NUL or BOM, and
is never trimmed, case-folded, URL-decoded, normalized or repaired. Decoding
replaces `-` with `+`, `_` with `/`, appends exactly one `=`, requires exactly
32 decoded bytes, re-encodes to canonical unpadded base64url and compares the
43 received characters ordinally. The last character must be one of
`AEIMQUYcgkosw048`; a non-zero discarded tail-bit alias is invalid. Every
master and generated presented secret is exactly 32 bytes. This exact-size
rule supersedes the earlier variable-length “at least 256 bits” wording for A1.
SecretRef-resolved master text must pass this codec byte-for-byte; a generic
resolver that irreversibly trims or normalizes it triggers STOP and is not
modified by A1.

Domain derivation is literal HKDF-SHA256 with `IKM = K_master[version]`, a
32-byte all-zero salt, `PRK = HMAC-SHA256(zero32, K_master)`, and
`K_domain = HMAC-SHA256(PRK, ASCII(info) || 0x01)`. Output is exactly 32 bytes.
The exact `info` strings are, respectively:

```text
TAG-EKYC-C6BA1-VERIFIER-PEPPER/platform-credential-v1
TAG-EKYC-C6BA1-VERIFIER-PEPPER/bootstrap-digest-v1
TAG-EKYC-C6BA1-VERIFIER-PEPPER/capability-digest-v1
```

No version, environment, machine, deployment, SecretRef, credential, Client or
prefix enters HKDF. Stored digests are exactly:

```text
HMAC-SHA256(K_PlatformCredential,
  ASCII("platform-credential-v1") || 0x00 || decoded-platform-secret-32)
HMAC-SHA256(K_BootstrapDigest,
  ASCII("bootstrap-digest-v1") || 0x00 || decoded-bootstrap-secret-32)
HMAC-SHA256(K_CapabilityDigest,
  ASCII("capability-digest-v1") || 0x00 || decoded-capability-secret-32)
```

They are 32 bytes and compared in fixed time. Any older `K_pepper_version`
shorthand means the derived `K_domain`, never raw `K_master`.

The API-hosted closed options type is exactly
`src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperOptions.cs`
with section name `TagEkyc:CaptureRuntimeVerifierPeppers`, positive Int32
`CurrentVersion`, and a finite `Versions` collection whose entries contain only
positive Int32 `Version` and nonblank `SecretRef`. Versions are unique, exactly
one entry matches CurrentVersion, and no plaintext-secret property is allowed.
Structural invalidity fails startup. Missing/unavailable referenced material
fails readiness, not authentication.

`src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperProvider.cs`
implements the Application port using those options, shared cryptography and
its public `CaptureRuntimeVerifierPepperSecretRefResolver`. The helper is the
single material path for both API provider and R01: exact `env:` value or actual
raw `file:` bytes, without trim, BOM handling, normalization or generic resolver
fallback. It may cache immutable version-to-SecretRef metadata and CurrentVersion,
never plaintext pepper bytes. Each resolution creates a
domain-key lease and clears resolved text, master, PRK and other owned
temporaries; it retains no resolved text, master, PRK or domain-key cache.
Infrastructure DI binds and
validates this closed section on startup and registers the provider; it creates
no Infrastructure-to-Api edge and does not modify project references. The
offline R01 tool is the sole process exception: it uses its explicit version +
SecretRef CLI inputs and the same public material helper, but does not
instantiate this API-hosted options/provider.

### Runtime and operator database contexts

The exact factory contracts are:

```csharp
public interface ICaptureRuntimeDbContextFactory
{
    ValueTask<TagEkycDbContext> CreateAsync(
        CancellationToken cancellationToken = default);
}

public interface ICaptureRuntimeOperatorDbContextFactory
{
    ValueTask<TagEkycDbContext> CreateAsync(
        CancellationToken cancellationToken = default);
}
```

`CaptureRuntimeDatabaseOptions.SectionName` is exactly
`TagEkyc:CaptureRuntimeDatabase`. Its only properties are
`OnlineConnectionString`, `OnlineConnectionStringSecretRef`,
`OperatorConnectionString` and `OperatorConnectionStringSecretRef`, all nullable
strings. Non-production tests may provide both direct connection strings and no
secret references. Production requires both secret references and rejects either
direct connection string. A4 owns production reference values and resolution.

Each concrete factory is singleton-safe, holds one separately built immutable
`DbContextOptions<TagEkycDbContext>` and returns a new caller-await-disposed
context on every call. Neither factory resolves the ordinary scoped
TagEkycDbContext, ordinary DbContextOptions, IDbContextFactory or
IServiceScopeFactory. `AddTagEkycPostgresPersistence` preserves the existing
ordinary scoped registration and additionally registers the options validator,
`CaptureRuntimeDbContextFactory` for `ICaptureRuntimeDbContextFactory`, and
`CaptureRuntimeOperatorDbContextFactory` for
`ICaptureRuntimeOperatorDbContextFactory`.

Validation parses the resolved Online, Operator and ordinary Client strings with
`NpgsqlConnectionStringBuilder`. It compares canonical Host, Port, Database and
Username values using ordinal Host/Database/Username equality and numeric Port
equality. Password or textual keyword ordering does not make an otherwise equal
identity distinct. All three identities must be nonempty and pairwise different.
Live readiness opens all three connections and asserts exact expected
`session_user`, `current_user`, role memberships, function EXECUTE edges and zero
table/sequence DML. A mismatch fails startup and readiness.

### Prepared and Activated route sets

Route selection is immutable for one process lifetime and completes before the
listener accepts traffic.

| Sentinel | Exact A1 operation set | A3 requirement | Legacy compatibility |
| --- | --- | --- | --- |
| `Prepared` | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R28a | not required | existing Client verification/package routes and `/api/ekyc/capture-agents/self/configuration` remain mapped; R20,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b are unmapped |
| `Activated` | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28b | registered and ready before listener | `/api/ekyc/capture-agents/self/configuration` and legacy API-key or allowed-producer capture/evidence/raw mappings are absent; ordinary Client session/package routes remain mapped |

Missing, malformed or unknown sentinel state fails startup. A4 changes Prepared
to Activated only while quiesced; the process is restarted after that durable
CAS. A running process never switches route sets in place.

### Versioned verifier-pepper material ledger

| Material | Authority/generator | Mutable holder and transfer | Durable representation | Replay/verification | Retirement and proof |
| --- | --- | --- | --- | --- | --- |
| API-host Capture Runtime verifier pepper | immutable process-lifetime `CaptureRuntimeVerifierPepperOptions.CurrentVersion` selects one closed Version→SecretRef entry; public exact-preserving helper consumes exact `env:` value or validates actual raw `file:` bytes; shared cryptography derives requested domain key | provider temporarily owns 32-byte master, PRK and derived key; lease exposes read-only derived key and zeroes its backing buffer; no material cache | none; R03/R20 persist only exact 32-byte domain digest + exact version | new R03/R20 stamp CurrentVersion; platform authentication, R05 and R21 resolve the row's persisted version and exact domain; unavailable/noncanonical material is `NOT_READY`, never mismatch or fallback | readiness resolves CurrentVersion union SQL-required versions; removal only when neither current nor required; restart is required to change CurrentVersion; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| offline R01 Capture Runtime verifier pepper | deployment operator supplies `--capture-runtime-verifier-pepper-version` and `--capture-runtime-verifier-pepper-secret-ref`; the same public exact-preserving helper plus shared cryptography derive PlatformCredential domain | short-lived CLI-owned master/PRK/domain-key/presented-secret buffers, all cleared; no API DI/appsettings/provider | none; credential stores exact 32-byte digest + CLI version | API later verifies with persisted version and PlatformCredential domain; no online synchronization or API-host CurrentVersion inference | actual CLI/source-join proof covers exact arguments, invalid canonical bytes/newline/BOM, no API-key fallback and historical version verification |
| presented verifier secrets | CSPRNG generates exactly 32 bytes for platform operator, bootstrap and capability secrets; shared codec emits exactly 43 canonical characters | operation-local mutable buffers only; secret-once response transfers the canonical text and owner clears local buffers | plaintext never persists; only the domain-specific 32-byte HMAC digest and version persist | replay never returns plaintext; presented text is decoded canonically and compared via fixed-time digest equality | `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors`; `PresentedVerifierSecrets_RejectNonCanonicalTailAliases`; `SecretOnce_DigestsAndPepperRetirementAreClosed` |

The former `capture-runtime-verifier-key provision|rotate|retire|readiness` and
deployment current-version CAS lifecycle is explicitly superseded for A1.
There is no A1 pepper lifecycle table, API, extra CLI family or hot rotation.
Deployment changes metadata/secret material while quiesced and restarts the API;
readiness and SQL reference census are the only retirement gate.

## 7. Normative artifact master

This table is the sole A1 file-mutation authority. Every path is literal. A
`ReadOnly` row is evidence only and is not mutation authority. Existing dirty or
untracked rows are rebound by raw SHA-256 immediately before implementation;
clean existing rows are bound to the repository HEAD named by the parent
dispatch. No directory, prefix or filename pattern grants authority.

| Exact repository-relative path | Disposition | Owner | Operations | Proof methods |
| --- | --- | --- | --- | --- |
| `src/TagEkyc.Api/Program.cs` | Modify | A1 | R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback`; `PreparedRouteSet_IsLegacyOnly`; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| `src/TagEkyc.Api/VerificationSessionEndpoints.cs` | Modify | A1 | R20a,R20b,R26 | `CapabilityIssue_SecretOnceAndExactReplay`; `CapabilityReplace_ExpectedCurrentAndBoundWins`; `SessionCancel_TerminalizesCapabilityAtomically` |
| `src/TagEkyc.Api/ReadinessEndpoint.cs` | Modify | A1 | R19,R28a,R28b | `Readiness_ValidatesSchemaAclPepperNonceAndConnections`; `PreparedRouteSet_IsLegacyOnly`; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` |
| `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | Modify | A1 | R27a,R27b | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce`; `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` |
| `src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs` | Modify | A1 | R23a,R23b,R28a,R28b | `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag`; `PreparedRouteSet_IsLegacyOnly`; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` |
| `src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs` | Modify | A1 | R23a,R23b,R28a,R28b | `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag`; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` |
| `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` | Modify | A1 | R25 | `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity`; `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` |
| `src/TagEkyc.Application/Ports/ApplicationServicePorts.cs` | Modify | A1 | R24,R25,R26 | `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction`; `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity`; `SessionCancel_TerminalizesCapabilityAtomically` |
| `src/TagEkyc.Application/Ports/RepositoryPorts.cs` | Modify | A1 | R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26 | `CapabilityIssue_SecretOnceAndExactReplay`; `Bind_OneWinnerSameLineageReplayNoTakeover`; `SessionCancel_TerminalizesCapabilityAtomically` |
| `src/TagEkyc.Contracts/CaptureAgent/CaptureAgentContracts.cs` | Modify | A1 | R24 | `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors` |
| `src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs` | Modify | A1 | R25 | `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` |
| `src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs` | Modify | A1 | R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | binds/validates `TagEkyc:CaptureRuntimeVerifierPeppers` and registers the versioned source without retaining plaintext; `RuntimeConnection_CannotResolveDefaultClientDbContext`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` | Modify | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` |
| `src/TagEkyc.Infrastructure/Persistence/DomainRowMapper.cs` | Modify | A1 | R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26 | `Enrollment_AtomicActivationAndLostResponseReplay`; `CatalogPublication_ExpectedHeadAndRollbackAsRevision`; `SessionCancel_TerminalizesCapabilityAtomically` |
| `src/TagEkyc.Infrastructure/Persistence/EfAppendIdempotencyBoundary.cs` | Modify | A1 | R24,R25 | `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction`; `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity`; `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` |
| `src/TagEkyc.Infrastructure/Persistence/EfVerificationFinalizationBoundary.cs` | Modify | A1 | R26 | `SessionCancel_TerminalizesCapabilityAtomically`; `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs` | Modify | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` |
| `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | ReadOnly | Landed C6B | R19,R28a,R28b | `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| `tools/TagEkyc.ApiKeyProvisioner/Program.cs` | Modify | A1 | R01,R02 | calls the sole shared root-fingerprint helpers and contains no competing implementation; `PlatformCredentialAuthentication_HasNoClientPartition`; `PlatformRootRevoke_IsIdempotentAndAudited`; `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret`; `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain`; `SecretOnce_DigestsAndPepperRetirementAreClosed`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `tests/TagEkyc.IntegrationTests/Tip88C1AAcceptanceSurfaceTests.cs` | Modify | A1 | R28a,R28b | `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` |
| `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | ReadOnly | A3 | R27a,R27b | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce` |
| `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs` | ReadOnly | A3 | R27a,R27b | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce` |
| `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | ReadOnly | A3 | R27a,R27b | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce`; `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeAuthenticationContracts.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | exact contract carries parsed auth facts plus `ReadOnlyMemory<byte> ExactSignedPreimage`, with no reconstruction-authority fields; `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback`; `Crt1_GoldenVectorsMatchClientAndServer` |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeEnrollmentContracts.cs` | Create | A1 | R05 | `Enrollment_AtomicActivationAndLostResponseReplay` |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeManagementContracts.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R19 | `BootstrapIssue_SecretOnceAndExactReplay`; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`; `RuntimeRevokeRetire_TerminalWins`; `CredentialRevoke_RacesRotationWithoutSplitGeneration`; `Rotation_DualProofAtomicCutoverAndSameRouteReplay` |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeControlContracts.cs` | Create | A1 | R16,R17,R18 | `RoleAssignment_IsNextGenerationOnly`; `ConfigurationAssignment_ReducesAndEtagIsOpaque`; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs` | Create | A1 | R20a,R20b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | `CapabilityIssue_SecretOnceAndExactReplay`; `Bind_OneWinnerSameLineageReplayNoTakeover`; `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors`; `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` |
| `src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | owns both authenticator interfaces, the API-hosted domain enum, `ICaptureRuntimeVerifierPepperSource`/read-only derived-key lease and other A1 Application ports; offline R01 does not instantiate this port; `PlatformCredentialAuthentication_HasNoClientPartition`; `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback`; `Crt1_GoldenVectorsMatchClientAndServer`; `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate`; `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce`; `RuntimeConnection_CannotResolveDefaultClientDbContext`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeVerifierCryptography.cs` | Create | A1 | R01,R02,R03,R05,R19,R20a,R20b,R21,R28a,R28b | sole pure authority for canonical 32-byte/43-character codec, exact HKDF domain derivation, digest formulas, R01/R02 ASCII/LF root request fingerprints, fixed-time comparison and owned-buffer zeroing; `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors`; `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret`; `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain`; root cross-domain proof; `PresentedVerifierSecrets_RejectNonCanonicalTailAliases`; `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeEnrollmentApplicationService.cs` | Create | A1 | R05 | resolves only the bootstrap row's persisted version and BootstrapDigest domain, with no CurrentVersion/Client-pepper fallback; `Enrollment_AtomicActivationAndLostResponseReplay`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeManagementApplicationService.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R19 | R03 uses immutable process CurrentVersion plus BootstrapDigest domain; R19 resolves CurrentVersion union SQL-required versions for every required domain; `BootstrapIssue_SecretOnceAndExactReplay`; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`; `RuntimeRevokeRetire_TerminalWins`; `Rotation_DualProofAtomicCutoverAndSameRouteReplay`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeControlApplicationService.cs` | Create | A1 | R16,R17,R18 | `RoleAssignment_IsNextGenerationOnly`; `ConfigurationAssignment_ReducesAndEtagIsOpaque`; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs` | Create | A1 | R20a,R20b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | R20 uses CurrentVersion plus CapabilityDigest domain; R21 resolves the persisted version plus CapabilityDigest domain; `CapabilityIssue_SecretOnceAndExactReplay`; `Bind_OneWinnerSameLineageReplayNoTakeover`; `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction`; `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity`; `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Api/CaptureRuntimeEndpoints.cs` | Create | A1 | R05,R13a,R13b,R21,R22,R23a,R23b,R24,R25 | `Enrollment_AtomicActivationAndLostResponseReplay`; `Rotation_DualProofAtomicCutoverAndSameRouteReplay`; `Bind_OneWinnerSameLineageReplayNoTakeover`; `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag` |
| `src/TagEkyc.Api/CaptureRuntimeManagementEndpoints.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R19 | `BootstrapIssue_SecretOnceAndExactReplay`; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`; `RuntimeRevokeRetire_TerminalWins`; `CredentialRevoke_RacesRotationWithoutSplitGeneration`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| `src/TagEkyc.Api/CaptureRuntimeControlEndpoints.cs` | Create | A1 | R16,R17,R18 | `RoleAssignment_IsNextGenerationOnly`; `ConfigurationAssignment_ReducesAndEtagIsOpaque`; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| `src/TagEkyc.Infrastructure/Auth/PlatformOperatorCredentialAuthenticator.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R14,R15,R16,R17,R18,R19 | resolves only the credential row's persisted version plus PlatformCredential domain and applies canonical presented-key decoding; missing dependency is NOT_READY, not mismatch; `PlatformCredentialAuthentication_HasNoClientPartition`; `PresentedVerifierSecrets_RejectNonCanonicalTailAliases`; `SecretOnce_DigestsAndPepperRetirementAreClosed`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` |
| `src/TagEkyc.Infrastructure/Auth/CaptureRuntimeRequestAuthenticator.cs` | Create | A1 | R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | verifies only parser-supplied `ExactSignedPreimage`; contains no CRT1 formatter/reconstructor; R05 is ENROLL1-only and excluded; `Crt1_GoldenVectorsMatchClientAndServer`; `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/ICaptureRuntimeDbContextFactory.cs` | Create | A1 | R05,R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | `RuntimeConnection_CannotResolveDefaultClientDbContext` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/ICaptureRuntimeOperatorDbContextFactory.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19 | `RuntimeConnection_CannotResolveDefaultClientDbContext` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDatabaseOptions.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `RuntimeConnection_CannotResolveDefaultClientDbContext`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDbContextFactory.cs` | Create | A1 | R05,R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | `RuntimeConnection_CannotResolveDefaultClientDbContext` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeOperatorDbContextFactory.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19 | `RuntimeConnection_CannotResolveDefaultClientDbContext` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDatabaseOptionsValidator.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `RuntimeConnection_CannotResolveDefaultClientDbContext`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperOptions.cs` | Create | A1 | R03,R05,R19,R20a,R20b,R21,R28a,R28b | closed `TagEkyc:CaptureRuntimeVerifierPeppers` metadata-only API-host contract with immutable process CurrentVersion; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed`; `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperProvider.cs` | Create | A1 | R01,R03,R05,R19,R20a,R20b,R21,R28a,R28b | owns public `CaptureRuntimeVerifierPepperSecretRefResolver`, shared by API provider and R01, with exact `env:` and raw-byte `file:` semantics; then derives exact requested domain through shared cryptography; returns only a zeroing read-only derived-key lease and retains no secret cache; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed`; `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimePersistenceBoundary.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26 | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes`; `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown`; `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` |
| `src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntimeEntities.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` |
| `src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntimeEntityConfigurations.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | canonical filename only; generated bytes remain implementation candidates; `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes`; `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.Designer.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | canonical filename only; generated bytes remain implementation candidates; `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes`; `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` |
| `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b | `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors`; `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback`; `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors`; `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` |
| `tests/TagEkyc.ContractTests/Tip88C1C6BA1ProtocolTests.cs` | Create | A1 | R27a,R27b | pure contract-level `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` only; it does not own or duplicate the API-parser CRT1 golden proof and requires no API project reference |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes`; `Readiness_ValidatesSchemaAclPepperNonceAndConnections`; `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed`; `R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef`; `PresentedVerifierSecrets_RejectNonCanonicalTailAliases`; `TransitionSql02_ExceptionMapping_IsClosedAndRollsBack` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | Create | A1 | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26 | `Enrollment_AtomicActivationAndLostResponseReplay`; `RuntimeSuspendReactivate_ExpectedRevisionSerializes`; `CredentialRevoke_RacesRotationWithoutSplitGeneration`; `Rotation_DualProofAtomicCutoverAndSameRouteReplay`; `Bind_OneWinnerSameLineageReplayNoTakeover` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AclTests.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown`; `RuntimeConnection_CannotResolveDefaultClientDbContext` |
| `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | sole owner of `Crt1_GoldenVectorsMatchClientAndServer`, exercising the actual API parser without project mutation; `Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation`; `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency`; `PreparedRouteSet_IsLegacyOnly`; `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` |
| `tests/TagEkyc.ArchTests/Tip88C1C6BA1DispatchCatalogueTests.cs` | Create | A1 | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | `DispatchCatalogue_IsClosedAndBidirectionallyJoined` |

No other file is writable under A1. In particular, A1 does not modify a
CaptureAgent repository, broker host, deployment/IaC file, SDK, solution or
project file.

### Current consumed-byte binding

| Git state | Exact path | Raw SHA-256 | Mutation disposition |
| --- | --- | --- | --- |
| M | `src/TagEkyc.Api/Program.cs` | `ADC8176B12AF9D7FE4F36A32C040483E2B143E85ED93AB6D8DE7C24702D6E193` | Modify |
| M | `src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs` | `923CDF0E0204EFFC1A44FC9CC906DDB5D12A8BBD9290A76FD760906B93EC9012` | Modify |
| M | `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs` | `945FD9C6028CE3B310891F21FF3F20E033928CA8C88CDD3DEBAB09F551C7B92E` | Modify |
| M | `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | `AE4408D7B2BF46A3EAFA9F1801D59C37BF3B5950662EBCA73ED1109D147762C7` | ReadOnly |
| M | `tests/TagEkyc.IntegrationTests/Tip88C1AAcceptanceSurfaceTests.cs` | `B4655A1562C6B3414066D638BD4682F0D736E94877415D967674477B5FA2D2BF` | Modify |
| untracked | `src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs` | `532F4196BE430DCDF877F263D6DB37890E399353DE835D579A63F893616B47A8` | Modify |
| untracked | `src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs` | `831D6032AA4C25105924501D808F5CD3E67498D7F97CAC32157BC6747EBF8A88` | Modify |
| untracked | `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | `1023A127F4A5183C2A9A1D63060C5B7638EC66DE35F87CE084961EFC1F34598F` | Modify |
| untracked | `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | `F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA` | ReadOnly |
| untracked | `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs` | `C996F31D4ADBBE7F33225879807741622CB67DDDD8305E5498C8C6CCC8357195` | ReadOnly |
| untracked | `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | `4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66` | ReadOnly |

Repository HEAD for these bytes is
`c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9`. Staged and conflicted path sets
were empty. All rows are rehashed immediately before implementation; any drift
requires an explicit byte rebind.

### Companion DDL binding

The sibling DDL master is
`docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_literal_ddl_master.md`.
The parent A1 dispatch binds exact SHA-256 for both companions; companions do not
hash-bind each other because mutual content hashes would be circular.

## 8. Normative proof master

The exact test project and file are part of each proof identity. All listed
methods must exist and execute; a source-text assertion cannot satisfy a runtime,
database, concurrency or HTTP proof.

| Exact method | Exact project and file | Operations | Required observable assertions | Required RED mutation |
| --- | --- | --- | --- | --- |
| `PlatformCredentialAuthentication_HasNoClientPartition` | `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19 | operator context has no ClientApplicationId or ApiKeyId; invalid scope denies before target lookup | add a Client partition selector |
| `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback` | `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b | mixed platform, Client and CRT1 credentials deny; no alternate authenticator runs | enable fallback authentication |
| `Crt1_GoldenVectorsMatchClientAndServer` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | actual API parser bytes equal client golden; authenticator verifies those bytes only; method/path/timestamp/nonce/media/length/body/binding mutations fail; exactly one final LF passes while missing/extra/CRLF fail; parser rejects noncanonical timestamp/nonce; RawIngress parser reads body zero times; source proof rejects competing builders; R05 remains under ENROLL1 proof | reconstruct in authenticator, route R05 through CRT1, omit/mutate a signed atom/final-LF case, accept noncanonical lexeme, retain mutable alias, duplicate proof owner or read RawIngress body |
| `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | CRT1 transaction N remains after B rollback; duplicate invokes B zero times; R05 has no CRT1/N and remains covered by `Enrollment_AtomicActivationAndLostResponseReplay` | share the N and B transaction or route R05 through CRT1/N |
| `NonceCleanup_IsBoundedAndCannotDeleteTimeValidEnvelope` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | authentication helper | invalid limit deletes zero; at most 10,000 rows with `PurgeAfterUtc < now` are deleted and equality is retained | delete on `<=`, omit ordering/limit or join lifecycle |
| `RuntimeConnection_CannotResolveDefaultClientDbContext` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AclTests.cs` | R05,R13a,R13b,R21,R22,R23a,R23b,R24,R25,R27a,R27b | online and operator factories create new contexts; canonical-equivalent Client identity fails; wrong role cannot execute | resolve the ordinary scoped context |
| `Enrollment_AtomicActivationAndLostResponseReplay` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R05 | one active runtime/install/generation; exact replay returns lineage without secret or extra event | remove the current-generation constraint trigger |
| `RuntimeSuspendReactivate_ExpectedRevisionSerializes` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R06,R07 | one expected-revision winner; loser conflict; exact replay stable | skip expected-revision recheck |
| `RuntimeRevokeRetire_TerminalWins` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R08,R09 | terminal precedence is durable; no reactivation or new business mutation | permit terminal-to-active transition |
| `CredentialRevoke_RacesRotationWithoutSplitGeneration` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R10,R11,R12,R13a,R13b | exactly one legal winner; current pointer and generation state never split | acquire rotation before identity locks |
| `Rotation_DualProofAtomicCutoverAndSameRouteReplay` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R11,R12,R13a,R13b | both proofs required; one successor; predecessor terminal state and pointer commit atomically; exact business replay stable | accept either proof alone |
| `RotationAuthorization_RevokeExpiryOneWinner` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R11,R12,R13a | authorization, revoke and expiry have one terminal winner; loser creates no successor; exact replay is stable | omit the shared rotation lock or terminal-state recheck |
| `Rotation_SuccessorWithoutRoleCanReplayOnlyExactCommittedOperation` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R13a,R13b | valid successor can read exact committed result only; mismatch and uncommitted completion deny without target disclosure | allow successor to create or change rotation state |
| `PlatformRootRevoke_IsIdempotentAndAudited` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R02 | one root operation and event; exact replay stable; revoked credential cannot authenticate | revoke without operation/event |
| `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R01 | externally materialized 145-byte golden; intent fields vary hash; generated secret/prefix/digest/pepper/SecretRef/now do not; regenerated-verifier lost-response retry returns `ExistingMatchSecretUnavailable` with one credential/operation/event and no secret replay | include generated verifier input, omit an intent field, use CRLF/missing final LF/locale formatting, or return conflict/duplicate/secret on exact intent replay |
| `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R02 | externally materialized 136-byte golden; operation/credential/revision vary hash; now does not; exact replay has one root history; changed target/revision conflicts; reason is fixed `DeploymentRevocation` | omit target/revision, accept arbitrary reason, include now, duplicate history, or accept changed intent as replay |
| `RootRequestFingerprint_R01R02_DomainsAreSeparatedAndImplementationIsUnique` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R01,R02 | changing only the exact domain label changes SHA-256; both CLI paths call the sole helper owner; R01 signature cannot accept generated verifier inputs and R02 cannot accept reason | share/unlabel domains, add a second implementation, compute locally in `Program.cs`, broaden either helper signature, or create a project/file edge |
| `BootstrapIssue_SecretOnceAndExactReplay` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R03 | first response carries secret; replay carries no secret; one issuance/event | re-emit persisted plaintext secret |
| `BootstrapIssueRevoke_OneTerminalWinner` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R03,R04 | issue/revoke race produces one visible terminal history and no false NotFound | omit shared issuance lock |
| `RoleAssignment_IsNextGenerationOnly` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R14,R17 | current binding unchanged; next generation receives the assigned role | mutate current frozen generation role |
| `ConfigurationAssignment_ReducesAndEtagIsOpaque` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R15,R18,R23a,R23b | widening override denies; ETag equality alone yields 304; ETag is not revision-derived | accept a widening override |
| `AssignedRevision_SurvivesUnassignedNewHead` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R14,R15,R16,R17,R18,R19,R21,R23a,R23b | publishing a newer role, trust or configuration head without assigning it leaves the frozen generation/registration revision usable until its own effective window ends | require assigned revision to equal catalogue current head |
| `CatalogPublication_ExpectedHeadAndRollbackAsRevision` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R16,R17,R18 | one expected-head winner; rollback is a new audited revision | update an old revision row |
| `TransitionSql02_ExceptionMapping_IsClosedAndRollsBack` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R11,R12,R13a,R14,R15,R16,R17,R18 | every named T2 `P0001` primary message maps to its exact closed public result after full rollback; an unknown database failure maps only to `503 NOT_READY`; every case leaves zero target/operation/event residue | alter one primary message or public mapping, accept prefix matching, or retain one row after failure |
| `Readiness_ValidatesSchemaAclPepperNonceAndConnections` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R19,R28a,R28b | each missing schema, ACL, pepper, nonce and connection dependency independently reports not ready | omit one readiness dependency |
| `CapabilityIssue_SecretOnceAndExactReplay` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R20a,R20b | first 201 has secret; exact replay is nonsecret conflict; one capability/event | return secret on replay |
| `CapabilityReplace_ExpectedCurrentAndBoundWins` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R20a,R20b,R21 | replacement requires exact ActiveUnbound revision; Bound wins and no successor appears | replace a Bound capability |
| `Bind_OneWinnerSameLineageReplayNoTakeover` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R21 | one binding; exact lineage replay stable; competitor learns no owner identity | allow a second runtime binding |
| `BindingReconcile_IsReadOnlyAndNonEnumerating` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R22 | exact operation with still-current runtime, installation, credential and Bound capability returns binding; stale lifecycle/revision/current-generation or mismatch returns one unavailable row; zero writes/events/secrets | create state during reconcile or return zero rows after authority loss |
| `CapabilityExpiry_FingerprintBindsTargetAndDeadline` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R21 | exact replay matches the server-derived session/capability/durable-deadline/operation fingerprint; reuse of the operation UUID for another capability or changed deadline conflicts without a second operation/event | persist a constant fingerprint or compare only the operation UUID |
| `ExpiryOnDenied_PersistsTerminalResultAndExactReplay` | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_expiry_on_denied_proof.md` | R05,R13a,R20a,R20b,R21,R26 | first expired touch commits target expiry, exact operation result and one Expired event; Issue alone may create a successor; exact replay is frozen and creates no second event, successor or binding | return denial or throw without committing expiry, or persist Replay/Conflict as a new operation |
| `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R23a,R23b | unequal ETag returns exact 200 JSON; equal ETag exact empty 304; both return ETag | equate ETag to numeric revision |
| `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R24 | caller identity fields absent; server-derived identity and artifact/audit commit together | accept caller Agent or Device identity |
| `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors` | `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | R24 | recursive object graph contains no Client, API-key, Agent or Device selector | add one forbidden selector |
| `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R25 | accepted evidence and optional RawCaptureAcceptance remain exact; identity is server-derived | call the legacy API-key producer wrapper |
| `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` | `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | R25 | recursive graph contains no Client, API-key, Agent or Device selector | add one forbidden selector |
| `SessionCancel_TerminalizesCapabilityAtomically` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ConcurrencyTests.cs` | R26 | session and capability terminalize in one transaction/event history; bind cannot win afterward | use separate transactions |
| `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce` | `tests/TagEkyc.ContractTests/Tip88C1C6BA1ProtocolTests.cs` | R27a,R27b | N commits; A3 invoked once with immutable context and body ReadCount zero | read body or resolve Binding in A1 |
| `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` | `tests/TagEkyc.ContractTests/Tip88C1C6BA1ProtocolTests.cs` | R27a,R27b | exact 400, 403 and 503 cases read zero bytes and invoke A3 zero times | drain body on denial |
| `PreparedRouteSet_IsLegacyOnly` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R28a,R28b | Prepared starts without A3, maps legacy business routes and no R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b route | map a runtime business route in Prepared |
| `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R28a,R28b | missing A3 prevents listener; ready A3 maps R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b and no legacy producer/config route | map both route sets |
| `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | apply, rollback and reapply; every named constraint and trigger is exercised | remove one constraint, trigger or Down step |
| `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AclTests.cs` | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | every full function signature callable only by declared role; all table DML denied; Down preflight protects A4 membership | grant one extra EXECUTE or DML edge |
| `SecretOnce_DigestsAndPepperRetirementAreClosed` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R01,R03,R20a,R20b | plaintext absent after first response; referenced pepper retirement denies; buffers/logs contain no secret | persist or log one plaintext secret |
| `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors` | `tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs` | R01,R03,R05,R20a,R20b,R21 | master `AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8` yields PRK `46bd320605c5a6b6163ab70bc6345b92a5f908e79fe58979c23ebb47d1a5e307`, platform key `4db8994a75704c012def93ba380e2e6e63191f4901bedac22e8ef9876d6f00bb`, bootstrap key `f091805ec2529b08b4661530f80befd7462824ca19d901324188de655f1a58d3`, capability key `eea3404eea1c83d037a2e459bed78d13ba21862f97220f86bb229ed2ed1bc95f`; canonical platform/bootstrap/capability vectors produce digests `c5a2f56f1325e67b4deb97fa5d5d5ed21a238d0e3c122a5fc30f16f91a72b473`, `ac1d78c9278e787d899465772567e26ee48967fad8b4c05350937a1c0805dec9`, `e249239c0a2e4532c78c7d006d576f9819293264107dc91c4f1a8b04493315a3`; tail aliases ending `Hh9`,`Hh-`,`Hh_` and `9`,`-`,`_` for canonical ending `8` reject | accept one noncanonical 43-character alias, wrong HKDF info/counter/salt, variable-length secret or non-zero tail bits |
| `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R03,R05,R19,R20a,R20b,R21,R28a,R28b | API CurrentVersion is immutable for process lifetime and stamps new R03/R20 rows; persisted N still verifies after restart at N+1; exact version+domain resolution has no fallback; readiness resolves CurrentVersion union SQL-required versions and every required domain. Shared public helper proves exact `env:` values and actual raw `file:` bytes, including positive canonical values containing `-` and `_`; LF, CRLF, BOM, spaces, 42/44-byte, invalid ASCII, non-ASCII, empty, missing and directory cases fail with the frozen taxonomy; duplicate/unmatched version and missing material fail closed without disclosure or API-key fallback; removal succeeds only when neither current nor required | permit HTTP selection, hot-change CurrentVersion, trim/normalize material, reject valid `-`/`_`, fall back across version/domain/API-key pepper, cache master/domain material or keep readiness green with any unresolved requirement |
| `R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R01 | actual offline CLI accepts only the exact version and SecretRef argument pair, calls the same public exact-preserving helper as API provider, uses shared codec/HKDF PlatformCredential domain, stamps digest-producing version and clears buffers; source/architecture proof joins both consumers to the helper; missing/zero/negative/noninteger version, invalid/unavailable material and API-key fallback reject | infer API CurrentVersion, instantiate API DI/provider, implement another resolver, pre-trim a fixture, accept plaintext master or call SQL directly as the only proof |
| `PresentedVerifierSecrets_RejectNonCanonicalTailAliases` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R01,R03,R05,R20a,R20b,R21 | canonical platform/bootstrap/capability presented secret succeeds through its real boundary; three same-byte/noncanonical tail aliases fail without target disclosure, mutation or fallback | normalize, repair, trim or decode an alias as equivalent |
| `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R01,R03,R05,R19,R20a,R20b,R21,R28a,R28b | Application owns domain-aware derived-key port and shared pure cryptography; Infrastructure provider file owns one public exact-preserving material helper used by both API provider and R01; generic resolver/AssemblyInfo remain unchanged; no second resolver, trim-equivalent path, InternalsVisibleTo, raw master crossing, API-key pepper, Infrastructure-to-Api edge, project mutation, writable readiness edge, lifecycle surface, hot rotation or API DI/appsettings dependency from R01 exists | expose master/PRK, duplicate crypto/material resolver, use generic normalized material, diverge R01/provider helper, reject valid alphabet, reference API-key pepper/options, add friend/project edge, modify AssemblyInfo/generic resolver, or make readiness validator writable |
| `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b | event failure rolls back B target; committed N remains; secret absent | commit target without event |
| `IdentityAuditGraph_InvariantsRejectMismatch` | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_identity_audit_mutation_proof.md` | R01,R02,R05,R10,R11,R13a,R13b,R20a,R20b,R21,R26 | sixteen PostgreSQL 16 mutations reject four cross-lineage tuples, exact-field and event-type mismatch in five audit families, and both invalid Bound↔binding directions; terminal rollback leaves zero residue | change only a duplicated lineage field, semantic EventType, or one side of the capability graph and allow commit |
| `Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | changed paths and references remain inside A1 master; authenticator interfaces exist only in Application ports; Api and Infrastructure reference Application; Application and Infrastructure have zero Api references; no project-file, forbidden assembly or forbidden path mutation | add an Api-owned authenticator interface, reverse Infrastructure-to-Api dependency, project-reference mutation, broker, Agent, deployment or SDK mutation |
| `DispatchCatalogue_IsClosedAndBidirectionallyJoined` | `tests/TagEkyc.ArchTests/Tip88C1C6BA1DispatchCatalogueTests.cs` | R01,R02,R03,R04,R05,R06,R07,R08,R09,R10,R11,R12,R13a,R13b,R14,R15,R16,R17,R18,R19,R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b,R28a,R28b | all lint rules in section 10 pass | orphan one identifier |

## 9. Derived-view policy

Sections 1 through 5, the artifact master and the proof master are the normative
sources. Any route list, SQL ACL list, lock graph, outcome list, allowlist, dirty
manifest or proof summary emitted elsewhere is a derived view and must carry:

```text
DERIVED VIEW — NON-NORMATIVE. Generated from the A1 operation, artifact and
proof masters. If this view differs from a master, review fails until regeneration.
```

Derived views may not introduce or narrow a route, DTO member, function
signature, role, lock, state, outcome, file disposition or proof. They are
regenerated, never patched independently.

`PARENT-WRAPPER-R24-R25-PLACEHOLDER-01 = CLOSED`. The parent callable-list
duplication was removed and now delegates to the canonical projection of this
master and T1--T4. Normative R24/R25 continue to reuse the existing
authority-neutral Application append boundaries and authorize no new public SQL
function. The obsolete `capture_runtime_apply_capture_artifact` and
`capture_runtime_apply_evidence_result` names remain prohibited.

## 10. Mechanical catalogue acceptance

`DispatchCatalogue_IsClosedAndBidirectionallyJoined` performs all checks below
against this exact bound document and the companion literal-DDL master. Every
failure is an implementation-authority blocker; the test does not replace the
runtime, database, concurrency or HTTP proofs.

1. Exactly one operation master, artifact master and proof master is present.
2. Operation identifiers and variants are unique and every R01 through R28a,R28b is present.
3. Every route has an exact method, path, request property source/type/grammar,
   authentication scheme, business and signed-envelope fingerprints, outcome
   precedence, owner, full SQL signature when applicable and proof.
4. Every full SQL signature resolves to exactly one transition companion
   function, one owner, its complete grantee set, caller operations, Down
   identity and ACL proof. Internal helper functions join to their owning
   physical operation and are subject to the same rule.
   The exact authentication/nonce helper and R10 bodies are in
   `tip_88c1_c6b_a1_transition_sql_04_auth_nonce_credential_revoke.md`.
5. Every schema identifier referenced by an operation resolves to literal DDL;
   every DDL object joins back to an operation and proof.
6. Every operation, path, SQL signature, schema object and proof reference is
   bidirectional; no orphan is permitted.
7. Every HTTP operation has at least one executable proof for each distinct
   success, replay, authentication denial, authority denial, conflict and
   dependency branch it exposes.
8. Every race pair names the shared lock or CAS, legal winner, loser outcome,
   replay residue and an executable concurrency proof.
9. Every secret names its generator, mutable holders, durable representation,
   replay rule, terminalizer and leak proof.
10. Every writable artifact is owned by A1 and joins to an operation and proof;
    every read-only dependency has no mutation authority.
11. Every existing dirty or untracked consumed artifact has its current raw
    SHA-256; any mismatch blocks implementation pending explicit rebind.
12. The exact migration and Designer filenames in section 7 exist together and
    no second A1 migration is present.
13. No wildcard, path prefix, ellipsis, filename token, abbreviated SHA,
    unresolved choice, `MAY`, `TBD`, `TODO`, `ICaptureRuntimeRawIngressHandoff`
    or credential state outside the DDL-master CHECK is permitted in normative
    catalogue text.
14. R27a,R27b invokes `ICaptureRuntimeRawIngressAdmission` only after committed N,
    gives it an unread stream, and contains no A3 business implementation path.
15. Prepared excludes R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b and does not require A3. Activated requires ready
    A3 before listener acceptance, includes R20a,R20b,R21,R22,R23a,R23b,R24,R25,R26,R27a,R27b and excludes legacy
    CaptureAgent configuration and API-key producer mappings.
16. Online, Operator and ordinary Client Npgsql identities are compared after
    canonical parsing by server, port, database and username; reordered or
    aliased equivalent strings are rejected, and live readiness asserts
    `session_user` and `current_user`.
17. The durable R13a,R13b business fingerprint is distinct from each signed-envelope
    fingerprint; successor replay cannot create or mutate state.
18. Every generated derived view exactly equals the canonical projection of the
    masters; a hand-edited or stale view fails.
19. Staged and conflicted paths are empty before implementation and before the
    as-built packet; unrelated dirty paths remain byte-identical.
20. The architecture proof rejects references or mutations in CaptureAgent,
    broker-host, deployment/IaC, SDK, solution and project files.
21. The bound verifier-pepper RRI SHA is exact; the domain enum, port signature,
    read-only lease and shared cryptography path appear once and join both the
    API provider and offline R01 adapter without an API-host dependency.
22. Every master and presented verifier secret is exactly 32 decoded bytes and
    exactly 43 canonical characters; codec round-trip and tail-bit rules reject
    every normalization, padding, whitespace, wrong-length and alias mutation.
23. The three HKDF info strings, zero salt, one-block counter and three stored
    digest prefixes/formulas are literal and join to their domain callers and
    golden-vector proof; neither raw master nor PRK crosses the Application port.
24. API CurrentVersion is immutable for process lifetime. Lifecycle tooling and
    deployment current-version CAS from earlier drafts are absent; rotation is
    quiesced metadata/material change plus process restart and readiness census.
