# TIP-88C1-C6B-A1 — Typed Gateway, R05 Resolver and R27 Proof Boundary RRI

**Version:** v0.9 successor candidate  
**Supersedes for implementation:** v0.8 SHA-256 `FEB18334B05E4DBD4E1BEFE62159AF77D7A86ED47C8A3B0FA8E1B9CEF439D946`; v0.8 bytes are preserved; the v0.7 ratification record and SHA `2DB31ED6EFD55EC57166D19F43130EC21D87524FC5309C92434194B9C4219EDD` are preserved but its untracked bytes were removed during successor creation and are unavailable; implementation remains paused until this exact v0.9 successor is reviewed and ratified  
**Status:** REVIEW CANDIDATE — NOT IMPLEMENTATION AUTHORITY  
**Date:** 2026-09-11  
**Supersedes:** base RRI SHA-256 `104D692AD6345372D1FF7CA2EC891A6F93760DD7F43A78A3DB9A8B5F24AA10D9`; do not ratify or implement from that predecessor  
**Authority basis:** Homeowner-ratified Parent v0.12 SHA-256 `BB3B2AEED01753F563B5DC56720172DBD9EB1EA27EE88EEBA11F65038CCB2881`  
**Operation Master:** SHA-256 `B1E3F320C32691756DACEF5A206B6D0303E811377B7304623C43C56ECBEAC277`  

## 1. Purpose and exact radius

This successor closes two confirmed executability contradictions and carries
forward the predecessor authority for one required internal R05 resolver. The
confirmed contradictions are:

1. the generic `string`/`object`/unconstrained-`T` gateway surfaces cannot safely
   implement the literal R03–R26 catalogue; and
2. the two R27 HTTP/A3-boundary proofs are assigned to a ContractTests project
   that cannot reach the required production surfaces.

The R05 resolver, atomic R13 nonce-classifier and R24/R25 append-authority
validator are three proposed bounded `SECURITY DEFINER` SQL functions. Only the
R05 resolver was authorized by the predecessor amendment. The R13 and R24/R25
functions are explicit successor deltas that acquire authority only if the
Homeowner ratifies this exact v0.9 SHA; this candidate does not self-authorize
them. Therefore the eventual
implementation radius includes the DDL master, transition SQL 01 and 02/04 as
applicable, migration SQL, ACL and Down ordering, PostgreSQL mutation/runtime
evidence, ACL tests, typed ports/services/persistence, R27 integration proof and
exact SHA cascade. This is not a documentation-only proof-owner correction.

No A2 or A3 implementation, business-semantic change, route activation, stage,
commit or push is authorized by this candidate.

## 2. Typed-boundary rule

Each callable A1 Application operation exposes a closed typed boundary whose
actor, request, identity, response, execution path, SQL or non-SQL boundary and
result mapping are mechanically traceable to one Operation Master row. Method
names below are non-semantic mechanical names.

Forbidden throughout the affected surface:

- `string operation` or `string catalogKind` dispatch;
- `object` or `object?` requests;
- unconstrained generic result `T` casting;
- reflection routing;
- runtime string-to-handler registries;
- fallback/default operation mapping.

One physical operation does not imply one SQL function. The shared and non-SQL
exceptions below are normative.

## 3. Normative typed-boundary matrix

`PlatformOperator` means `AuthenticatedPlatformOperatorContext`; `Runtime` means
`AuthenticatedCaptureRuntimeContext`; `Client` means the landed
`AuthenticatedClientContext`. Every persistence method also receives only the
internal fingerprint, actor/lineage and time inputs frozen by its Operation
Master SQL signature; those inputs do not become HTTP DTO fields.

| Rxx | Application method | Persistence/gateway method | Actor | Request DTO | Response DTO | Identity inputs | Execution path | Exact boundary | Proof owner |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| R03 | `IssueBootstrapAsync` | `IssueBootstrapAsync` | PlatformOperator | `CaptureRuntimeBootstrapIssueRequest` | `CaptureRuntimeBootstrapIssueResponse` | actor credential/principal + IdempotencyKey | Application creates secret/prefix/digest with CurrentVersion; B lock 5 | `capture_runtime_issue_bootstrap(...)` | `BootstrapIssue_SecretOnceAndExactReplay` |
| R04 | `RevokeBootstrapAsync` | `RevokeBootstrapAsync` | PlatformOperator | `CaptureRuntimeBootstrapRevokeRequest` | new exact `CaptureRuntimeBootstrapLifecycleResponse(BootstrapIssuanceId,State,Revision,RevokedAtUtc)` | actor + issuance + expected revision + IdempotencyKey | B lock 5 | `capture_runtime_revoke_bootstrap(...)` | `BootstrapIssueRevoke_OneTerminalWinner` |
| R05 | `RedeemAsync` | `ResolveBootstrapVerifierVersionAsync`; then `RedeemBootstrapAsync` | none; bootstrap secret + ENROLL1 | `CaptureRuntimeEnrollmentRedeemRequest` | `CaptureRuntimeEnrollmentResponse` | issuance + RedeemOperationId + CandidateKeyId | exact preprocessing in §4; B locks 5→10→20→30 | new internal resolver, then unchanged `capture_runtime_redeem_bootstrap(...)` | `Enrollment_AtomicActivationAndLostResponseReplay` |
| R06 | `SuspendRuntimeAsync` | same | PlatformOperator | `RuntimeLifecycleRequest` | `CaptureRuntimeLifecycleResponse` | actor + AgentId + expected revision + IdempotencyKey | B 10→20→30 | `capture_runtime_suspend(...)` | `RuntimeSuspendReactivate_ExpectedRevisionSerializes` |
| R07 | `ReactivateRuntimeAsync` | `ReactivateRuntimeAsync` | PlatformOperator | `RuntimeLifecycleRequest` | `CaptureRuntimeLifecycleResponse` | actor + AgentId + expected revision + IdempotencyKey | B 10→20→30 | signature in §3.1 | §6 row 8 exact owner+method |
| R08 | `RevokeRuntimeAsync` | same | PlatformOperator | `RuntimeLifecycleRequest` | `CaptureRuntimeLifecycleResponse` | same closed identity | B 10→20→30 | `capture_runtime_revoke(...)` | `RuntimeRevokeRetire_TerminalWins` |
| R09 | `RetireRuntimeAsync` | `RetireRuntimeAsync` | PlatformOperator | `RuntimeLifecycleRequest` | `CaptureRuntimeLifecycleResponse` | actor + AgentId + expected revision + IdempotencyKey | B 10→20→30 | signature in §3.1 | §6 row 9 exact owner+method |
| R10 | `RevokeCredentialAsync` | same | PlatformOperator | `CaptureRuntimeCredentialRevokeRequest` | `CaptureRuntimeCredentialResponse` | actor + Agent/installation/credential/generation/revision + IdempotencyKey | B 10→20→30 | `capture_runtime_revoke_credential(...)` | `CredentialRevoke_RacesRotationWithoutSplitGeneration` |
| R11 | `AuthorizeRotationAsync` | same | PlatformOperator | `CaptureRuntimeRotationAuthorizeRequest` | `CaptureRuntimeRotationResponse` | actor + IdempotencyKey; server RotationId | B 10→20→30→60 | `capture_runtime_authorize_rotation(...)` | `RotationAuthorization_RevokeExpiryOneWinner` |
| R12 | `RevokeRotationAsync` | `RevokeRotationAsync` | PlatformOperator | `CaptureRuntimeRotationRevokeRequest` | new exact `CaptureRuntimeRotationLifecycleResponse(RotationId,State,Revision,RevokedAtUtc)` | actor + RotationId/revision + IdempotencyKey | B 10→20→30→60 | signature in §3.1 | §6 row 12 exact owner+method |
| R13a | `CompleteRotationAsync` | same | Runtime predecessor + `CredentialRotation` | `CaptureRuntimeRotationCompleteRequest` | `CaptureRuntimeRotationCompletionResponse` | route RotationId + predecessor lineage + operation fingerprint | N then B 10→20→30→60 | `capture_runtime_complete_rotation(...)` | `Rotation_DualProofAtomicCutoverAndSameRouteReplay` |
| R13b | `ReplayCompletedRotationAsync` | `ReplayCompletedRotationAsync` | Runtime current successor; no rotation role | `CaptureRuntimeRotationCompleteRequest` | `CaptureRuntimeRotationCompletionResponse` | exact committed rotation tuple/fingerprint | N then read-only lookup | signature in §3.1 | §6 row 13 exact owner+method |
| R14 | `AssignRolePolicyAsync` | same | PlatformOperator | `CaptureRuntimeRolePolicyAssignmentRequest` | `CaptureRuntimeRolePolicyAssignmentResponse` | actor + Agent/policy/revision + IdempotencyKey | B 10→40 | `capture_runtime_assign_role_policy(...)` | `RoleAssignment_IsNextGenerationOnly` |
| R15 | `AssignConfigurationAsync` | same | PlatformOperator | `CaptureRuntimeConfigurationAssignmentRequest` | `CaptureRuntimeConfigurationAssignmentResponse` | actor + Agent/config/override/revision + IdempotencyKey | B 10→40 | `capture_runtime_assign_configuration(...)` | `ConfigurationAssignment_ReducesAndEtagIsOpaque` |
| R16 | `PublishTrustProfileAsync` | same | PlatformOperator | `CaptureRuntimeTrustProfilePublicationRequest` | `CaptureRuntimeCatalogPublicationResponse` | actor + CatalogId/head + IdempotencyKey | B lock 40 | `capture_runtime_publish_trust_profile(...)` | `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| R17 | `PublishRolePolicyAsync` | `PublishRolePolicyAsync` | PlatformOperator | `CaptureRuntimeRolePolicyPublicationRequest` | `CaptureRuntimeCatalogPublicationResponse` | actor + CatalogId/head + IdempotencyKey | B lock 40 | signature in §3.1 | §6 row 23 exact owner+method |
| R18 | `PublishConfigurationAsync` | same | PlatformOperator | `CaptureRuntimeConfigurationPublicationRequest` | same catalog response | same identity class | B lock 40 | `capture_runtime_publish_configuration(...)` | R16 proof plus configuration proof |
| R19 | `ReadReadinessAsync` | `ReadReadinessAsync` | PlatformOperator | route `CaptureAgentId`; no body | `CaptureRuntimeReadinessResponse` | actor + AgentId; no idempotency | SQL read; Application resolves `{CurrentVersion} UNION required_pepper_versions` and composes pepper/final readiness | `capture_runtime_read_readiness(...)` plus Application pepper source | `Readiness_ValidatesSchemaAclPepperNonceAndConnections` |
| R20a | `IssueCapabilityAsync` | `IssueOrReplaceCapabilityAsync` | Client | sessionId + `CaptureCapabilityRequest(Action=Issue)` | `CaptureCapabilityResponse` | Client/session + IdempotencyKey | Application secret/digest/version; B 70→80 | shared `capture_runtime_issue_or_replace_capability(...)` | `CapabilityIssue_SecretOnceAndExactReplay` |
| R20b | `ReplaceCapabilityAsync` | `IssueOrReplaceCapabilityAsync` | Client | sessionId + `CaptureCapabilityRequest(Action=Replace,CurrentCapabilityId,ExpectedRevision)` | `CaptureCapabilityResponse` | Client/session + predecessor/revision + IdempotencyKey | B 70→80 | exact R20a shared signature in §3.1 | §6 row 27 exact owner+method |
| R21 | `BindAsync` | `ResolveCapabilityVerifierAsync`; optional `MaterializeCapabilityExpiryAsync`; then `BindCapabilityAsync` | Runtime `Bind` | `CaptureRuntimeBindRequest` | `CaptureRuntimeBindingResponse` | runtime lineage + BindOperationId equal IdempotencyKey | N then B 10→20→30→70→80 | existing internal verifier/expiry helpers and `capture_runtime_bind_capability(...)` | `Bind_OneWinnerSameLineageReplayNoTakeover` |
| R22 | `ReconcileAsync` | `ReconcileBindingAsync` | Runtime `Bind` | `CaptureRuntimeReconcileRequest` | `CaptureRuntimeBindingResponse` | capability + BindOperationId; no IdempotencyKey | N then read-only | `capture_runtime_reconcile_binding(...)` | `BindingReconcile_IsReadOnlyAndNonEnumerating` |
| R23a | `ResolveConfigurationAsync` | same | Runtime `Configuration` | no body | `CaptureRuntimeConfigurationResponse` plus computed ETag | runtime lineage; no idempotency | read-only SQL; Application canonicalizes and hashes ETag | shared `capture_runtime_resolve_configuration(...)` | `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag` |
| R23b | HTTP projection of R23a; no second Application/persistence operation | none | Runtime `Configuration` | equal quoted `If-None-Match` | 304 empty body + R23a ETag | runtime lineage; no new identity | compare only the R23a computed ETag | exact R23a resolver signature in §3.1 | §6 row 32 exact owner+method |
| R24 | `AppendCaptureArtifactAsync` | typed validation/append boundary | Runtime `CaptureObservation` | route bindingId + `CaptureRuntimeCaptureArtifactRequest` + UUIDv4-N Idempotency-Key | exact landed `CaptureArtifactSubmissionResponseDto` | runtime/binding-derived identity | one B transaction 10→20→30→70→80 | existing authority-neutral capture append; no new public SQL | capture wrapper + object-graph proofs |
| R25 | `AppendEvidenceResultAsync` | typed validation/append boundary | Runtime `TrustedEvidence` | sessionId + `CaptureRuntimeEvidenceResultRequest` + UUIDv4-N Idempotency-Key | exact landed `EvidenceResultSubmissionResponseDto` | runtime/binding/session-derived identity | same B ownership as R24 | existing authority-neutral evidence append; no new public SQL | evidence wrapper + object-graph proofs |
| R26 | keep the landed Client cancellation method; no Capture Runtime gateway method | existing finalization boundary | Client | landed cancel request + sessionId | landed cancel response | Client/API-key prefix + normalized request/correlation identity | existing B 70→80 path | existing Client path calls `capture_runtime_cancel_session_with_capability(...)` | `SessionCancel_TerminalizesCapabilityAtomically` |

R13a and R13b share one HTTP route but remain distinct typed Application and
persistence boundaries. The route coordinator may select only from an already
authenticated current credential lineage and the exact committed rotation tuple;
it must not implement “try predecessor then fall back to successor” authentication
or disclose which branch existed. If the current authentication contract cannot
produce that closed branch without fallback, implementation STOPs for a bounded
R13 authentication-path correction rather than inventing one.

R24 and R25 join through the exact authority-neutral planner and one-B boundary
defined in §3.3; no unresolved transaction-seam choice remains. They must not
commit validation before append or add a public SQL function.

### 3.1 Literal method, SQL and public-result projection

The words `same` and `(...)` in the compact table above are display shorthand
only and grant no inference authority. The executable identifiers are:

```text
R06 SuspendRuntimeAsync -> SuspendRuntimeAsync -> capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
R07 ReactivateRuntimeAsync -> ReactivateRuntimeAsync -> capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
R08 RevokeRuntimeAsync -> RevokeRuntimeAsync -> capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
R09 RetireRuntimeAsync -> RetireRuntimeAsync -> capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
R10 RevokeCredentialAsync -> RevokeCredentialAsync -> capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz)
R11 AuthorizeRotationAsync -> AuthorizeRotationAsync -> capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz)
R12 RevokeRotationAsync -> RevokeRotationAsync -> capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
R13a CompleteRotationAsync -> CompleteRotationAsync -> capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz)
R13b ReplayCompletedRotationAsync -> ReplayCompletedRotationAsync -> capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz)
R14 AssignRolePolicyAsync -> AssignRolePolicyAsync -> capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz)
R15 AssignConfigurationAsync -> AssignConfigurationAsync -> capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz)
R16 PublishTrustProfileAsync -> PublishTrustProfileAsync -> capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz)
R17 PublishRolePolicyAsync -> PublishRolePolicyAsync -> capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz)
R18 PublishConfigurationAsync -> PublishConfigurationAsync -> capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz)
R19 ReadReadinessAsync -> ReadReadinessAsync -> capture_runtime_read_readiness(uuid,timestamptz)
R20a IssueCapabilityAsync -> IssueOrReplaceCapabilityAsync -> capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz)
R20b ReplaceCapabilityAsync -> IssueOrReplaceCapabilityAsync -> same exact R20a SQL signature
R21 BindAsync -> ResolveCapabilityVerifierAsync + MaterializeCapabilityExpiryAsync when due + BindCapabilityAsync -> the three exact Operation Master signatures
R22 ReconcileAsync -> ReconcileBindingAsync -> capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz)
R23a ResolveConfigurationAsync -> ResolveConfigurationAsync -> capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz)
R23b no second method -> exact R23a result projection only
R24 AppendCaptureArtifactAsync -> AppendCaptureArtifactInRuntimeTransactionAsync -> no public SQL
R25 AppendEvidenceResultAsync -> AppendEvidenceResultInRuntimeTransactionAsync -> no public SQL
R26 landed Client cancellation method -> landed finalization boundary -> capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,uuid,bytea,timestamptz,text,uuid)
```

Every method returns its row's closed `SessionOperationResult<ResponseDto>`.
The public projection is literal: success uses the Operation Master success
status (200 or 201, with R23b 304); invalid input 400; unauthenticated or
non-enumerating denial 403; absent authorized target 404; conflict/replay-secret-
unavailable 409; dependency/readiness failure 503. A row may emit only the
subset listed in its Operation Master Rxx paragraph. SQL `result_code` mapping
is a closed switch; unknown/null results map to 503 and never a success.

Proof ownership is the exact file+method mapping in §6, not the compact labels
in the final column of the table. No `same as`, grouped proof label or inferred
owner is operative outside that 53-row census.

### 3.2 R13 single-pass authentication branch

The shared completion route uses one new closed authenticator operation,
`AuthenticateRotationCompletionAsync(CaptureRuntimeSignedRequest request,
Guid rotationId, CaptureRuntimeRotationCompleteRequest body, CancellationToken)`.
It resolves the presented credential and verifies CRT1 once, then calls exactly
one new atomic R13 nonce-classification function:

```sql
tagekyc.capture_runtime_claim_rotation_completion_nonce(
  p_credential_id uuid,
  p_generation bigint,
  p_rotation_id uuid,
  p_candidate_key_id uuid,
  p_successor_thumbprint bytea,
  p_request_fingerprint_as_predecessor bytea,
  p_request_fingerprint_as_successor bytea,
  p_nonce bytea,
  p_signed_at timestamptz,
  p_now timestamptz)
RETURNS TABLE(branch text, selected_request_fingerprint bytea,
  runtime_revision bigint,
  installation_revision bigint, credential_revision bigint,
  role_policy_id uuid, role_policy_revision bigint)
```

The function is `LANGUAGE plpgsql SECURITY DEFINER`, owner
`tagekyc_raw_export_deployer`, `SET search_path=pg_catalog`, and executable only
by `tagekyc_capture_runtime_authenticator`. PUBLIC,
`tagekyc_capture_runtime_application` and all unrelated roles are
revoked. In one transaction N it locks/revalidates the exact current runtime,
installation, credential/generation, rotation authorization/completion tuple and
role-policy revision, applies the existing timestamp/nonce checks, inserts the
one nonce claim, and returns zero rows or exactly one branch:

Both `bytea` candidate arguments are nullable by PostgreSQL's normal function-
argument semantics; no invalid `NULL` type modifier is added to the declaration.

```text
Predecessor
Successor
```

For `Predecessor`, the same locked role-policy revision must contain
`CredentialRotation`. For `Successor`, the credential must be the exact current
Active successor of the already completed rotation and the candidate key,
thumbprint and request fingerprint must equal the committed completion tuple;
no role-policy role is required. No generic `RotationReplay` role or purpose is
added to `capture_runtime_claim_nonce`.

Before any R13 target lookup, role decision or database call, Application
mechanically computes two pure candidate fingerprints from presented generation
`G` and the same already-ratified R13 domain, canonical request bytes and
Idempotency-Key:

```text
request_fingerprint_as_predecessor := present only for G >= 1 and G < Int64.MaxValue;
                                      binds generation pair (G, G+1)
request_fingerprint_as_successor   := present only for G > 1;
                                      binds generation pair (G-1, G)
```

Both bind the stable `CredentialId`, `RotationId`, `CandidateKeyId`, successor
SPKI, successor thumbprint, Idempotency-Key and every other already-ratified R13
atom identically; only the mechanically derived predecessor/successor generation
pair differs. An unrepresentable alternate candidate is absent (`NULL`), not a
request-wide 400, zero/sentinel digest, fabricated or clamped generation,
unchecked overflow or alternate fingerprint grammar. At least one candidate is
representable for every canonical positive Generation. Neither construction
uses database state, role selection, target existence or branch information.

Inside the atomic function, a locked `Predecessor` branch requires and selects
only `p_request_fingerprint_as_predecessor` and ignores an absent successor
candidate. A locked `Successor` branch requires the successor candidate, ignores
an absent predecessor candidate, and accepts only when
`p_request_fingerprint_as_successor` equals the exact durable completion
fingerprint. Absence of the selected candidate denies without claiming N or
invoking R13a/R13b and never triggers alternate-branch fallback. The returned
`selected_request_fingerprint` is passed unchanged to R13a or compared unchanged
by R13b. Thus predecessor generation G completion and successor generation G+1
lost-response replay select the same durable fingerprint.

The exact order is: parse closed route/body → compute both candidate fingerprints
without lookup → resolve verifier → verify CRT1 → atomically classify and claim
N once → return a discriminated R13 actor plus the selected fingerprint → call
R13a B or R13b read-only function. Both terminal
paths recheck the exact rotation tuple; classifier output is never sufficient
to mutate or disclose it. The classifier selects:

- exact active predecessor credential: require the frozen `CredentialRotation`
  role and return the R13a actor; or
- exact current active successor of that already completed rotation, with the
  same candidate key, thumbprint and request fingerprint: return the R13b actor.

Any other tuple, missing predecessor role, mismatch or ambiguity returns zero
rows and becomes the same generic 403. Dependency failure is 503. There is no
predecessor-first attempt, successor fallback, role-neutral general nonce path,
second nonce claim, target-existence disclosure or branch-specific error. The
coordinator dispatches solely on this discriminated authentication result.

### 3.3 R24/R25 one-B authority-neutral append seam

For R24/R25 only, this successor resolves the stale Parent/Operation-Master
response prose in favor of the Parent's “existing result” rule and the landed
contracts. The old `CaptureArtifactResponse`, `EvidenceResultResponse` and
`RawCaptureAcceptanceResponse` shapes are superseded and non-operative for
R24/R25. The sole operative responses are exactly:

```text
CaptureArtifactSubmissionResponseDto(
  string CaptureArtifactId, string VerificationSessionId,
  string? ArtifactHash, bool Accepted, string SessionState,
  string CorrelationId, bool Deduplicated=false)

EvidenceResultSubmissionResponseDto(
  string EvidenceResultId, bool Accepted, string SessionState,
  string? NextAction, bool Deduplicated=false,
  RawCaptureAcceptanceDto? RawCaptureAcceptance=null)

RawCaptureAcceptanceDto(Guid CaptureArtifactId, Guid CaptureAcceptanceId,
  int CaptureRevision, string RawClass)
```

There is no implicit field rename, Guid/string conversion, alternate nullability
or second response model. Runtime service signatures are exactly:

```csharp
Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>>
    AppendCaptureArtifactAsync(AuthenticatedCaptureRuntimeContext actor,
        Guid bindingId, CaptureRuntimeCaptureArtifactRequest request,
        Guid idempotencyKey, CancellationToken cancellationToken = default);

Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>>
    AppendEvidenceResultAsync(AuthenticatedCaptureRuntimeContext actor,
        Guid verificationSessionId, CaptureRuntimeEvidenceResultRequest request,
        Guid idempotencyKey, CancellationToken cancellationToken = default);
```

The API accepts only canonical UUIDv4-N `Idempotency-Key`, parses it once and
passes the Guid unchanged. At the landed append core Application uses only
`idempotencyKey.ToString("N")`; no arbitrary Client-style string is accepted on
a runtime route. For R24, route `bindingId` must equal `request.BindingId`.
Mismatch is `400 REQUEST_INVALID` before N, B or protected target lookup. The
same BindingId binds CRT1, semantic validation, authority validation and append
planning; route-wins/body-wins fallback is forbidden. Proofs trace the key into
the append idempotency record and demonstrate exact replay/conflict behavior.

The landed Client-facing `ICaptureArtifactCommands` and
`ITrustedEvidenceResultCommands` are not reused as runtime ports because they
require `AuthenticatedClientContext`. Their business rules are extracted without
semantic change into `IAuthorityNeutralVerificationEvidencePlanner` with exact
methods returning the existing write records:

```text
PlanCaptureArtifactAsync(VerifiedAppendPrincipal, VerificationSession,
  LocalDevClientPolicy, NeutralCaptureArtifactPayload, string idempotencyKey,
  DateTimeOffset now) -> SessionOperationResult<AppendCaptureArtifactWrite>

PlanEvidenceResultAsync(VerifiedAppendPrincipal, VerificationSession,
  LocalDevClientPolicy, IReadOnlyList<CaptureArtifact> sessionArtifacts,
  NeutralEvidenceResultPayload, string idempotencyKey, DateTimeOffset now)
  -> SessionOperationResult<AppendEvidenceResultWrite>
```

`VerifiedAppendPrincipal` is a closed discriminated Application record. Its
Client case contains the already-authenticated Client identity/policy facts; its
Runtime case contains CaptureAgentId, DeviceInstallationId, CredentialId,
generation, role-policy identity and exact BindingId derived under B. It has no
caller-selectable Agent/Device/Client field. The planner owns all existing
payload grammar, policy/required-check, hash, artifact-input, writable-session,
state-transition, fingerprint and audit-event construction rules. Infrastructure
owns no duplicate business rule.

The planner does not accept either legacy wire request type. Application owns
two closed neutral payloads whose fields are exactly the runtime payload fields:

```text
NeutralCaptureArtifactPayload = ArtifactType, CaptureSource, ArtifactHash,
  MetadataHash, RequestId, CorrelationId

NeutralEvidenceResultPayload = ResultType, InputCaptureArtifactIds, Result,
  Confidence, ReasonCodes, RetryReasonCode, SanitizedSummaryRef, PayloadHash,
  PayloadSignatureStatus, EngineName, EngineVersion, RequestId, CorrelationId,
  NfcEvidenceDecisionBasis?, FaceMatchEvidenceDecisionBasis?,
  LivenessEvidenceDecisionBasis?
```

The three neutral decision-basis records copy every same-named field from the
corresponding `CaptureRuntime*EvidenceDecisionBasis`: NFC copies Flags,
CaptureBinding, ServerDecisionResult, AdapterRequestedResult, EngineName,
EngineVersion, InputArtifacts, SanitizedSummaryLabel and Extension; FaceMatch
copies all live/reference IDs and hashes, score, threshold, IsMatch,
ReferenceFaceSource, LiveCaptureBinding, both decision results, engine identity,
label and Extension; Liveness copies live-media ID/hash, score, requested verdict,
method, grade, threshold, LiveCaptureBinding, ServerDerivedIsLive, both decision
results, label and Extension. A neutral capture binding contains ChallengeHash,
SessionId, CapturedAt and ArtifactHash plus CaptureAgentId/DeviceId taken only
from `VerifiedAppendPrincipal`; runtime payload data can never populate them.

Exact adapters are
`MapRuntimeCapturePayload(CaptureRuntimeCaptureArtifactPayload,
VerifiedAppendPrincipal.Runtime)` and
`MapRuntimeEvidencePayload(CaptureRuntimeEvidenceResultPayload,
VerifiedAppendPrincipal.Runtime)`. They copy the enumerated non-identity fields
and inject the derived runtime Agent/Device identity only into neutral nested
capture bindings. They never construct or consult
`CaptureArtifactSubmissionRequestDto`, `NfcCaptureBindingDto`,
`FaceMatchCaptureBindingDto` or `LivenessCaptureBindingDto`.

The landed Client path has separate adapters from its legacy DTOs. Those adapters
first apply the existing Client authorization/AllowedCaptureAgentIds and device
binding rules, freeze the accepted identity into `VerifiedAppendPrincipal.Client`,
then strip all caller identity selectors and produce the same neutral payloads.
No runtime adapter accepts null/fabricated Client identity, and no Client adapter
weakens the landed authorization. The object-graph and R24/R25 wrapper proofs
must mutate every legacy top-level and nested Agent/Device selector and prove it
cannot influence a runtime append.

Application adds one exact transaction port:

```text
IAppendBusinessTransaction.ExecuteAsync<T>(
  Func<CancellationToken,Task<SessionOperationResult<T>>> operation,
  CancellationToken cancellationToken)
```

Its Infrastructure implementation uses the existing default scoped
`TagEkycDbContext` and its `tagekyc_runtime` database login, then opens/commits/
rolls back the one transaction B. It does not use
`ICaptureRuntimeDbContextFactory` or the capture-runtime application login.
Runtime R24/R25 first invoke, on that same connection and transaction, the sole
new internal authority function:

```sql
tagekyc.capture_runtime_validate_append_authority(
  p_capture_agent_id uuid, p_installation_id uuid, p_credential_id uuid,
  p_generation bigint, p_binding_id uuid, p_verification_session_id uuid,
  p_required_role text, p_now timestamptz)
RETURNS TABLE(role_policy_id uuid, role_policy_revision bigint,
  runtime_revision bigint, installation_revision bigint,
  credential_revision bigint, capability_id uuid,
  capability_revision bigint, binding_id uuid)
```

This function is `LANGUAGE plpgsql SECURITY DEFINER`, owner deployer,
`search_path=pg_catalog`, revoked from PUBLIC and all other roles, and granted
only to `tagekyc_runtime`. It accepts only literal roles `CaptureObservation` or
`TrustedEvidence`, acquires ranks 10→20→30→70→80, and revalidates exact current
runtime/installation/credential/current-generation/role-policy/binding/session
lineage. It returns zero rows on any mismatch and exposes no secret or unrelated
identity. `tagekyc_runtime` receives EXECUTE only and receives no new direct DML
on A1 tables; SECURITY DEFINER owns those reads/locks.

After that exact result, the same default context loads the existing session,
policy and, for R25, session artifacts; builds `VerifiedAppendPrincipal.Runtime`;
calls the planner; and calls the existing append write core. Client append
methods use the same transaction port without the runtime validation function,
build `VerifiedAppendPrincipal.Client` only after their existing authorization,
and call the same planner/write core. Thus both paths have one owner and one B.

`IAppendIdempotencyBoundary` gains exact transaction-participating methods:

```csharp
Task<AppendIdempotencyApplyResult> ApplyCaptureArtifactWriteAsync(
    AppendCaptureArtifactWrite write,
    CancellationToken cancellationToken = default);

Task<AppendIdempotencyApplyResult> ApplyEvidenceResultWriteAsync(
    AppendEvidenceResultWrite write,
    CancellationToken cancellationToken = default);
```

They require an
already-open transaction and never begin/commit/rollback it. The existing
`TryApply*` methods become compatibility wrappers that open
`IAppendBusinessTransaction` and invoke these cores; their public result mapping
is unchanged. `VerificationEvidenceApplicationService` remains the sole owner of
mapping `AppendIdempotencyApplyResult` (`Applied`, `Deduplicated`, slot/payload
mismatch and terminal session) into the existing public
`SessionOperationResult<CaptureArtifactSubmissionResponseDto>` or
`SessionOperationResult<EvidenceResultSubmissionResponseDto>`; the write core
never returns an HTTP/Application response. Metadata-reference registration remains after committed success,
exactly as landed, and is not moved into the authoritative append transaction.

No public SQL function, Client-context fabrication, duplicated validation,
pre-validation commit or Infrastructure→Api dependency is authorized. Failure
to preserve the existing Client behavior or to join the single B transaction is
a boundary violation and STOP, not permission to add another transaction.

## 4. R05 persisted-version resolver and preprocessing

The R05-specific SQL helper authorized by the predecessor amendment and
restated by this successor is:

```sql
tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid)
RETURNS TABLE(verifier_pepper_version integer)
```

Its input is `BootstrapIssuanceId` only. It returns the positive persisted
`VerifierPepperVersion` for any exact issuance row and deliberately does not
filter state or wall-clock expiry. This is cryptographic key-selection metadata,
not an authority decision. Filtering Active/unexpired rows would break exact
replay and first-touch expiry materialization already owned by R05.

The function is `LANGUAGE sql STABLE SECURITY DEFINER`, owned by
`tagekyc_raw_export_deployer`, has `SET search_path=pg_catalog`, revokes PUBLIC
and every unrelated role, and grants EXECUTE only to
`tagekyc_capture_runtime_application`. It is read-only and returns exactly one
integer column. It returns no digest, SecretRef, pepper material or Client
identity. An absent issuance yields zero rows and maps externally to the existing
non-enumerating R05 403 denial. A persisted null/non-positive version is an
impossible schema/dependency condition and maps to `NOT_READY` 503. An exact
positive version whose pepper reference/material is invalid or unavailable also
maps to `NOT_READY` 503. These internal cases never disclose target existence or
dependency detail externally. No direct Application table SELECT is authorized.

The sole preprocessing owner is `CaptureRuntimeEnrollmentApplicationService`:

1. validate the closed request;
2. resolve the persisted version by issuance ID;
3. map zero rows to the existing generic 403 denial; map corrupt persisted
   version or unavailable/invalid exact-version pepper dependency to the
   existing `NOT_READY` 503, without external existence disclosure;
4. resolve that exact version through `ICaptureRuntimeVerifierPepperSource` with
   `BootstrapDigest` domain—never `CurrentVersion` fallback;
5. canonically decode `BootstrapSecret` using the sole shared codec;
6. compute the ratified bootstrap digest;
7. verify ENROLL1 using the ratified candidate-key PoP grammar;
8. zero decoded secret, derived material and temporary cryptographic buffers;
9. call typed `RedeemBootstrapAsync` with the computed digest; and
10. let unchanged `capture_runtime_redeem_bootstrap(...)` re-lock/recheck replay,
    lifecycle, expiry and the authoritative stored-digest comparison.

Application never receives or compares the stored digest. Infrastructure may not
add a codec, pepper resolver or ENROLL1 implementation. The existing redemption
signature remains unchanged and receives no pepper-version, boolean-verdict or
plaintext-secret parameter.

### Required SQL/evidence propagation

- DDL master: add the helper to reader/callable, ACL and Down catalogues; no table
  or column shape change.
- Transition SQL 01: add exact CREATE/OWNER/REVOKE/GRANT; Down revokes Application
  EXECUTE then drops the helper before the bootstrap table/role dependency.
- Migration: project those exact bytes only.
- Core mutation evidence: exact row returns exact version; absent/null returns
  zero; Redeemed/Revoked/Expired state does not suppress the version; no digest
  column and no DML.
- ACL proof: sole Application EXECUTE; PUBLIC/unrelated roles and direct table
  SELECT/DML denied.
- Runtime proof: persisted-version/BootstrapDigest resolution, no CurrentVersion
  fallback, and locked R05 SQL remains the comparison authority.
- Operation Master, Parent and all transitive hashes are rebound after final bytes.

The atomic R13 nonce-classifier from §3.2 is separately projected into the DDL
master, rotation/auth transition companions,
migration, ACL/Down and PostgreSQL runtime/concurrency proofs. Required negative
proofs are: successor succeeds without a role-policy `CredentialRotation` grant;
predecessor without that grant is denied; no generic replay role/purpose is
accepted by the normal nonce function; tuple mismatch claims no nonce; atomic
classification success claims one N;
concurrent lineage change cannot make either business path skip its exact tuple
recheck; no branch-specific error or target disclosure is observable.

The R13 proof must additionally establish all six fingerprint properties:

1. predecessor generation G first completion and successor generation G+1 replay
   select the same durable fingerprint;
2. swapping the two candidate fingerprint arguments is RED;
3. changing RotationId, CandidateKeyId, successor SPKI, thumbprint,
   Idempotency-Key or either member of the generation pair is RED;
4. successor mismatch produces no valid replay branch and no nonce claim;
5. construction of both candidates performs no rotation/completion lookup; and
6. two digest computations still produce one CRT1 verification, one atomic
   classifier call, one branch decision and one nonce claim—never predecessor-
   first/successor-fallback authentication.

It must also cover the branch-local bounds explicitly:

- first rotation at presented generation 1 constructs predecessor `(1,2)`,
  leaves the successor candidate absent, proceeds on the valid predecessor
  branch, claims exactly one nonce and does not return 400 for the absent
  alternate;
- a normal predecessor generation greater than 1 may construct both candidates,
  but the classifier selects only the locked branch candidate;
- successor generation `G+1` reconstructs `(G,G+1)` and matches the committed
  fingerprint;
- an unrepresentable `G+1` predecessor candidate at the upper bound does not
  invalidate an otherwise valid successor branch; and
- an absent alternate is ignored, while an absent selected candidate never
  authorizes, never claims N and is never replaced by a sentinel, zero, clamp or
  fabricated generation.

## 5. R27 proof ownership

The sole executable owner of both:

```text
RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce
RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3
```

is the proposed existing-project path, authorized only upon Homeowner
ratification of this exact v0.9 SHA:

```text
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RawIngressBoundaryTests.cs
```

Do not create `Tip88C1C6BA1ProtocolTests.cs` in ContractTests, duplicate either
proof, create another test project or modify ContractTests solely to reach API.
The present IntegrationTests graph already reaches Api, Application, Contracts
and Infrastructure and already carries `Microsoft.AspNetCore.Mvc.Testing`.

The success proof exercises the real HTTP/API boundary and proves valid CRT1,
committed N, exactly zero body reads/drains, exactly one A3-port invocation, the
same still-unread `HttpRequest.Body` stream, and immutable context derived from
the authenticated request. Its A3 test double may count/capture/select one closed
result only; it must not implement binding, acceptance, R1, body consumption,
retained-source or broker behavior.

The denial proof exercises actual malformed-request 400, CRT1/current-lineage/
role 403 and missing-dependency 503 paths as applicable. Each has body reads 0,
drains 0, A3 invocations 0, no protected Binding/session/acceptance lookup, and
only the existing closed error body. Prepared stays legacy-only; Activated stays
fail-closed without required A3 readiness.

## 6. Global proof-owner reachability census

Surface keys: `U` public contract/Application; `CRT` actual API parser plus
production verifier; `DB` PostgreSQL/Infrastructure; `C` concurrent DB; `SQL`
bound executable SQL evidence; `ARCH` assembly/source/catalogue; `HTTP27` actual
HTTP endpoint, CRT auth, unread-body and A3 port seam.

Every row below was traced against the current project references. “Authorized”
means the exact absent file is authorized by Parent, or is proposed by this
successor and becomes authorized only after exact-SHA Homeowner ratification.

| # | Proof | Exact owner | Surface | Reachable | File |
| --- | --- | --- | --- | --- | --- |
| 1 | `PlatformCredentialAuthentication_HasNoClientPartition` | UnitTests `Tip88C1C6BA1ContractTests.cs` | U | YES | exists |
| 2 | `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback` | UnitTests same | U | YES | exists |
| 3 | `Crt1_GoldenVectorsMatchClientAndServer` | ArchTests `Tip88C1C6BA1ArchitectureTests.cs` | CRT+ARCH | YES | exists |
| 4 | `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate` | IntegrationTests `Tip88C1C6BA1ConcurrencyTests.cs` | C+DB | YES | authorized |
| 5 | `NonceCleanup_IsBoundedAndCannotDeleteTimeValidEnvelope` | IntegrationTests `Tip88C1C6BA1PersistenceTests.cs` | DB | YES | exists |
| 6 | `RuntimeConnection_CannotResolveDefaultClientDbContext` | IntegrationTests `Tip88C1C6BA1AclTests.cs` | DB | YES | exists |
| 7 | `Enrollment_AtomicActivationAndLostResponseReplay` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 8 | `RuntimeSuspendReactivate_ExpectedRevisionSerializes` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 9 | `RuntimeRevokeRetire_TerminalWins` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 10 | `CredentialRevoke_RacesRotationWithoutSplitGeneration` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 11 | `Rotation_DualProofAtomicCutoverAndSameRouteReplay` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 12 | `RotationAuthorization_RevokeExpiryOneWinner` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 13 | `Rotation_SuccessorWithoutRoleCanReplayOnlyExactCommittedOperation` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 14 | `PlatformRootRevoke_IsIdempotentAndAudited` | IntegrationTests PersistenceTests | DB | YES | exists |
| 15 | `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 16 | `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 17 | `RootRequestFingerprint_R01R02_DomainsAreSeparatedAndImplementationIsUnique` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 18 | `BootstrapIssue_SecretOnceAndExactReplay` | IntegrationTests PersistenceTests | DB | YES | exists |
| 19 | `BootstrapIssueRevoke_OneTerminalWinner` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 20 | `RoleAssignment_IsNextGenerationOnly` | IntegrationTests PersistenceTests | DB | YES | exists |
| 21 | `ConfigurationAssignment_ReducesAndEtagIsOpaque` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 22 | `AssignedRevision_SurvivesUnassignedNewHead` | IntegrationTests PersistenceTests | DB | YES | exists |
| 23 | `CatalogPublication_ExpectedHeadAndRollbackAsRevision` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 24 | `TransitionSql02_ExceptionMapping_IsClosedAndRollsBack` | IntegrationTests PersistenceTests | DB | YES | exists |
| 25 | `Readiness_ValidatesSchemaAclPepperNonceAndConnections` | IntegrationTests PersistenceTests | DB+Api+Infrastructure | YES | exists |
| 26 | `CapabilityIssue_SecretOnceAndExactReplay` | IntegrationTests PersistenceTests | DB | YES | exists |
| 27 | `CapabilityReplace_ExpectedCurrentAndBoundWins` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 28 | `Bind_OneWinnerSameLineageReplayNoTakeover` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 29 | `BindingReconcile_IsReadOnlyAndNonEnumerating` | IntegrationTests PersistenceTests | DB | YES | exists |
| 30 | `CapabilityExpiry_FingerprintBindsTargetAndDeadline` | IntegrationTests PersistenceTests | DB | YES | exists |
| 31 | `ExpiryOnDenied_PersistsTerminalResultAndExactReplay` | `tip_88c1_c6b_a1_expiry_on_denied_proof.md` | SQL | YES | exists |
| 32 | `ConfigurationConditionalGet_200And304HaveExactBodiesAndEtag` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 33 | `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 34 | `RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors` | UnitTests ContractTests | U | YES | exists |
| 35 | `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 36 | `RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors` | UnitTests ContractTests | U | YES | exists |
| 37 | `SessionCancel_TerminalizesCapabilityAtomically` | IntegrationTests ConcurrencyTests | C+DB | YES | authorized |
| 38 | `RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce` | IntegrationTests `Tip88C1C6BA1RawIngressBoundaryTests.cs` | HTTP27 | YES | proposed; authorized only after exact-v0.9 ratification |
| 39 | `RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3` | IntegrationTests same | HTTP27 | YES | proposed; authorized only after exact-v0.9 ratification |
| 40 | `PreparedRouteSet_IsLegacyOnly` | ArchTests ArchitectureTests | ARCH+Api | YES | exists |
| 41 | `ActivatedRouteSet_IsRuntimeOnlyAndRequiresA3` | ArchTests ArchitectureTests | ARCH+Api | YES | exists |
| 42 | `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` | IntegrationTests PersistenceTests | DB | YES | exists |
| 43 | `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` | IntegrationTests AclTests | DB+ACL | YES | exists |
| 44 | `SecretOnce_DigestsAndPepperRetirementAreClosed` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 45 | `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors` | UnitTests ContractTests | U+Application crypto | YES | exists |
| 46 | `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` | IntegrationTests PersistenceTests | DB+Infrastructure | YES | exists |
| 47 | `R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef` | IntegrationTests PersistenceTests | DB+CLI helper | YES | exists |
| 48 | `PresentedVerifierSecrets_RejectNonCanonicalTailAliases` | IntegrationTests PersistenceTests | DB+Application | YES | exists |
| 49 | `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 50 | `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` | IntegrationTests PersistenceTests | DB | YES | exists; R05 proves B rollback plus no secret and has no N; only CRT1 rows prove committed-N retention |
| 51 | `IdentityAuditGraph_InvariantsRejectMismatch` | `tip_88c1_c6b_a1_identity_audit_mutation_proof.md` | SQL | YES | exists |
| 52 | `Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation` | ArchTests ArchitectureTests | ARCH | YES | exists |
| 53 | `DispatchCatalogue_IsClosedAndBidirectionallyJoined` | ArchTests `Tip88C1C6BA1DispatchCatalogueTests.cs` | ARCH | YES | exists |

Successor result: 53/53 mappings are reachable from the referenced current
project graph or from the exact IntegrationTests file this successor proposes
for authority upon exact-SHA Homeowner ratification.
No operative proof owner points to ContractTests for R27.

## 7. Project graph adjustment rule

The reviewed implementation plan requires no `.csproj` change: IntegrationTests
already reaches every R27 production surface. The conditional rule below is
proposed for ratification with this exact v0.9 successor. It records the
Homeowner instruction supplied in the 2026-09-11 review turn but does not claim
a separate repository addendum or become operative before exact-SHA ratification.
If the exact project/reference structure at implementation time differs from
this verified plan, Builder does not STOP merely because a reasonable
project-reference adjustment is cleanly required. Builder must instead:

1. state the difference;
2. explain why the adjustment is necessary;
3. show that it reduces or avoids coupling;
4. identify the exact new dependency direction; and
5. verify modular-monolith boundaries through build and architecture tests.

STOP applies only when the adjustment would violate the ratified module/provider
boundary or introduce a new semantic dependency requiring Homeowner decision.
The only permitted direction is a consumer/test project toward the module whose
public surface it must consume; no production module may reference Api and no
lower layer may reference a higher delivery layer. No arbitrary edge, reverse
Infrastructure→Api edge, cycle, duplicate parser or test-local production
substitute is authorized. Builder must record the exact changed `.csproj`, old
and new edge, reason and architecture-test result in the as-built report.

## 8. Required implementation and review gates

PASS before resumed implementation requires independent review of this complete
successor. After ratification, implementation PASS requires all of the following:

1. the affected generic dispatch forms are absent;
2. every matrix row maps unambiguously to its Operation Master row;
3. R20a/R20b and R23a/R23b sharing remains exact;
4. R24/R25 add no public SQL and preserve one B transaction;
5. R19 performs Application-side pepper composition;
6. R26 remains on the Client cancellation path;
7. exactly one R05 resolver, one atomic R13 nonce-classifier and one R24/R25
   append-authority validator exist, with no other new SQL helper;
8. resolver does not expose digest/SecretRef/material/Client identity or perform
   lifecycle/expiry filtering;
9. R05 uses exact persisted version + BootstrapDigest with no CurrentVersion
   fallback, and unchanged SQL owns locked digest comparison;
10. ENROLL1 and temporary-buffer zeroization have one Application owner;
11. all three helper owner/SECURITY DEFINER/search_path/ACL/Down contracts are exact;
12. DDL/T1/migration/mutation/runtime/ACL evidence and hashes reconcile;
13. R27 owner is exactly the IntegrationTests file named in §5;
14. real HTTP success passes the same unread stream to A3 once;
15. each denial reads/drains zero and invokes A3 zero;
16. no A3 behavior is implemented;
17. Prepared remains legacy-only and Activated fails closed without A3;
18. all 53 proof mappings remain reachable and non-vacuous;
19. project-graph state is unchanged, or any necessary adjustment satisfies §7;
20. exact catalogue and consumed-source hashes reconcile;
21. staged and conflicted counts are zero.

## 9. Do not reopen

Do not reopen R03–R26 business semantics, CRT1, ENROLL1 grammar, verifier-pepper
KDF/digest, R01/R02 fingerprints, capability, rotation, cancellation, R27 A1/A3
ownership, A2 or A3 implementation. Do not implement from the superseded base
RRI. This successor grants no implementation authority until independently
reviewed and ratified by the Homeowner.

## 10. Required successor review output

Return successor SHA/bytes/lines; Parent and Operation Master exact baselines;
the typed matrix verdict; exact R05 signature/output/security/sequence; affected
DDL/T1/ACL/Down evidence radius; exact R27 owner and success/denial requirements;
global proof census count; project graph status; catalogue/consumed-source basis;
build/test baseline; staged/conflicted counts; and every actionable finding.

Terminal verdict is `PASS — 0 ACTIONABLE FINDINGS` or
`HOLD — <exact findings>`.
