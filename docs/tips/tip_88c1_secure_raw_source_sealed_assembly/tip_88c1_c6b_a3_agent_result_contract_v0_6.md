# A3 v0.6 — public result, acceptance and Agent integration contract

**Date:** 2026-09-13
**Status:** executable-contract proposal; no implementation authority.
**Owner:** A3 parent v0.6. Existing A1/A2 code is the baseline, not evidence of A3 execution.

## 1. Canonical amendment scope

The canonical A1 Operation Master is `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md` (not the older untracked Operation Master). Its R27 result paragraph/table and R24/R25 response/transaction cells must be edited under the eventual A3 implementation grant, then rehashed in its parent in the same change. Do not leave six-outcome or optional-evaluation-retry prose authoritative beneath an A3 override. Preserve predecessor bytes before this coordinated amendment. No alteration of CRT1 signed bytes, N transaction or authentication precedences is proposed.

The parent §6 owns the exact finite 36-row ledger: one A1 outcome, 21 A3 business outcomes and 14 excluded internal/later/readiness outcomes. This companion owns the implementation mapping, not a second independently maintained outcome list.

`CaptureRuntimeRawIngressOutcome` becomes exactly these 21 named members, each joining the parent's numbered row:

| O-row | C# member |
| --- | --- |
| O02 | BindingInvalid |
| O03 | NotFoundOrNotAllowed |
| O04 | TransportProtocolInvalid |
| O05 | CapabilityUnavailable |
| O06 | ArtifactSizeLimitExceeded |
| O07 | PlaintextRetentionInvalid |
| O08 | CapacityUnavailable |
| O09 | IdempotencyBusy |
| O10 | EvaluationInProgress |
| O11 | ClaimTokenInvalid |
| O12 | ClaimRestartRequired |
| O13 | SourceRetentionNotAuthorized |
| O14 | HistoricCommitmentKeyUnavailable |
| O15 | FingerprintConflict |
| O16 | ReservationBusy |
| O17 | AlreadyAvailable |
| O18 | TemporarilyUnavailable |
| O19 | ContentCommitmentMismatch |
| O20 | RecaptureRequired |
| O21 | ResumePending |
| O22 | Available |

`CaptureRuntimeRawIngressAdmissionResult` retains its existing five members: Outcome, SourceArtifactId, CurrentSourceState, CurrentDisposition, RetryNotBeforeUtc. No broker/custody field is added. The enum-to-code/status/shape map has exactly 21 rows, no enum.ToString wire conversion, no enum integer acceptance and no default-success arm. O01 and O23–O36 cannot be constructed as a valid A3 enum/result.

## 2. Exact public result grammar and HTTP behavior

`CaptureAgentFinalResult` uses case-sensitive camelCase JSON. Unknown/duplicate properties, unknown codes, wrong JSON kinds, explicit null on an omitted optional field, missing required fields and extra fields are invalid. Output omits all forbidden fields; do not use a global serializer setting to alter unrelated APIs. Apply omission on this DTO/result writer only.

| Variant | Exact property set | Value constraints |
| --- | --- | --- |
| O | outcomeCode | Parent outcome-only code; no other property |
| E | outcomeCode, retryNotBeforeUtc | EvaluationInProgress code; exact persisted evaluation expiry, UTC timestamp, not a locally invented backoff |
| S | outcomeCode, sourceArtifactId, currentSourceState, currentDisposition | Available/AlreadyAvailable code; nonempty UUID from published row; both enums exactly Available; no retry property |

UUID output follows the existing System.Text.Json Guid representation; the client parses the UUID and does not reinterpret it as an ingress idempotency identity. UTC response timestamps use the existing System.Text.Json DateTimeOffset representation, offset zero; response JSON is not signed or canonicalized. Input CRT1/header grammar remains unchanged and is specified in §4 below.

`RawExportSourceIngressEndpoints.MapRuntimeResult` checks required/forbidden fields **before** serialization. Invalid A3 shape -> existing 503 `{code:"NOT_READY",correlationId}`; preserve N, A3Calls=1, no exception detail/body drain. A3 unavailable before authentication keeps A3Calls=0 and the original no-N dependency path. Do not parse business `outcomeCode` from the separate A1 `code` envelope.

Agent `SubmitRawExportSourceAsync` uses ResponseHeadersRead, bounded response read of 65,536 bytes, no redirects, decompression, cookies, proxy, credentials or retry handler. For each known status in parent §6, parse the closed business envelope before deciding success/denial. A known code with the wrong HTTP status is invalid. A1 envelopes are parsed separately by their exact `code,correlationId` grammar. HTML, an unknown code, truncated JSON or a body above the bound is a protocol failure, never an Available receipt. No EnsureSuccessStatusCode/legacy throw-before-parse on business replies.

The outer transport may report an unavailable response, but it never overwrites an already durable server terminal code. It does not assert server rollback on response loss. Raw buffer retry permission is determined separately by original lease lifetime and §5; an A1 NOT_READY is not a new 37th A3 business outcome.

## 3. R24/R25 acceptance and completion joins

The production caller is **new integration**; the SQL is landed. `CaptureRuntimeAppendApplicationService.AppendEvidenceResultAsync` already owns the B callback around authority recheck, planning and neutral evidence append. Add a typed `IRawExportCaptureAcceptanceWriter` call **inside that callback after the neutral writer has persisted its result, before B commits**. Do not alter the authority-neutral evidence planner into a raw-ingress authority service.

The new writer method is:

```csharp
Task<RawCaptureAcceptanceDto?> BindAcceptedEvidenceAsync(
    Guid bindingId, Guid verificationSessionId, Guid evidenceResultId,
    RawExportCaptureAcceptancePolicy? configuredPolicy,
    CancellationToken cancellationToken);
```

Implementation `EfRawExportCaptureAcceptanceWriter` requires the scoped DbContext's existing transaction. It invokes the SD bridge specified below, which derives Client/P/session from the locked runtime binding and verifies the exact persisted evidence/artifact. Inside that same B callback, Application calls `policyProvider.Find(session.ClientApplicationId)` on the already-validated session and passes that exact typed result as `configuredPolicy`; neither EF nor the Agent performs another policy lookup. The record contains the three configured fields below; a non-null record must match that session Client. The provider returns null for an absent entry in an otherwise valid configuration, not a default policy. The writer sends SQL NULL/NULL for a null record, otherwise its policy ID/version. This permits non-retained/ineligible zero-row success and a stored-policy replay without imposing a new non-retained configuration prerequisite. Only a fresh eligible retained acceptance requires the supplied pair. No actor/Client/policy/class/challenge comes from the Agent DTO. Runtime append audit remains runtime-attributed; the bridge uses the immutable custody principal only for raw acceptance and restores the previous transaction-local raw-export actor. Direct SELECT on the protected acceptance/binding tables is not granted to implement this reader.

For a non-retained binding return null, with no acceptance write. For retained bindings: NfcValidation + Passed -> its sole NfcReadArtifact -> ChipDg2Portrait; FaceMatch + Passed -> exact LiveFaceArtifactId of SelfieImage type -> LiveSelfieImage. Liveness or non-Passed evidence returns null and grants no raw acceptance. Multiple/absent/wrong-session/wrong-runtime artifacts fail closed; never choose the first input artifact. Do not treat the NFC aggregate hash as the DG2 body digest.

The new SD bridge calls the existing exact function, not a duplicate acceptance table:

```sql
tagekyc.raw_export_accept_capture_for_ingress(
 p_verification_session_id uuid,p_client_application_id uuid,p_capture_artifact_id uuid,
 p_evidence_result_id uuid,p_session_challenge_hash text,
 p_acceptance_policy_id text,p_acceptance_policy_version integer)
RETURNS TABLE("CaptureAcceptanceId" uuid,"CaptureRevision" integer,"RawClass" text)
```

An exact R25 replay reads the persisted evidence ID from the neutral replay result and returns the same acceptance tuple. It never creates acceptance for the new incoming payload on a replay. A policy reassignment does not re-author an existing acceptance: return its pinned policy/version when tuple equality is proven. Fresh eligible retained acceptances use server configuration; missing/invalid policy -> NOT_READY and roll back B including evidence/acceptance. An absent entry does not reject non-retained or ineligible evidence. Rejected append/idempotency conflict never invokes the writer. At most one matching accepted raw artifact per eligible evidence; ambiguous result is a failure.

### 3.1 Literal acceptance policy and protected-table bridge

New required setting `TagEkyc:RawExport:CaptureAcceptancePolicies:Entries` is an array of closed records with exactly `ClientApplicationId` (nonempty UUID), `AcceptancePolicyId` (1..128 ASCII characters from `[A-Za-z0-9._-]`) and `AcceptancePolicyVersion` (Int32 1..2147483647). Exactly one entry per Client; no wildcard/default/fallback, no RawClass mapping override. New Application record/provider `RawExportCaptureAcceptancePolicy` / `IRawExportCaptureAcceptancePolicyProvider.Find(Guid clientApplicationId) -> RawExportCaptureAcceptancePolicy?` is implemented by `ConfiguredRawExportCaptureAcceptancePolicyProvider` in Infrastructure. The typed owner uses configuration at startup and validates the entire entry set; every retained-enabled Client profile needs an entry. Prepared can remain unactivated without entries; Activated missing/duplicate/malformed entry is NOT_READY before new retained work. No Agent-local policy selection. Synthetic explicit value is `fixture-a3-accepted-evidence`, version1, bound to the fixture Client; it is not a product default. A changed configured version governs fresh acceptances only. An existing receipt uses its persisted version, even if no longer the currently selected one; malformed global config is still a readiness failure, not permission to guess.

Proposed exact bridge in the single A3 forward migration:

```sql
tagekyc.raw_export_accept_runtime_evidence(
 p_binding_id uuid,p_session_id uuid,p_evidence_result_id uuid,
 p_acceptance_policy_id text,p_acceptance_policy_version integer)
RETURNS TABLE("CaptureArtifactId" uuid,"CaptureAcceptanceId" uuid,
 "CaptureRevision" integer,"RawClass" text)
```

Owner deployer, SECURITY DEFINER/search_path=pg_catalog; EXECUTE only `tagekyc_runtime`, matching the existing neutral-append B connection. Revoke PUBLIC and all specialized runtime application/authenticator/operator/broker roles; do not add table DML/SELECT. It runs only inside the already-authorized R25 B, not a public route. It locks/revalidates exact binding/session/evidence, requires the same session Client and all CP11 lineage, returns zero rows for a non-retained binding or ineligible evidence, otherwise derives the single exact artifact by the fixed NfcValidation/FaceMatch rule above. No LIMIT1 on an ambiguous set. Acquire the existing class acceptance advisory lock before lookup; SELECT the exact prior tuple by session/class/artifact/evidence. Existing exact tuple supplies its stored AcceptancePolicyId/Version; contradictory or multiple matches raise `RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT`. If no prior tuple exists, require the supplied pair to be non-null, well formed and bound by the Application to the validated session Client; otherwise raise `RAW_EXPORT_CAPTURE_ACCEPTANCE_POLICY_NOT_READY`, mapped to503 with full B rollback. Only then use the configured pair. One-null/one-non-null is invalid. Prior exact tuple lookup precedes this missing-pair check, and non-retained/ineligible returns precede both; neither path invents a policy. Save `current_setting('tagekyc.actor_principal_id',true)`, set it to binding.PrincipalId, invoke the unchanged seven-input acceptance function, restore prior setting (NULL becomes empty setting) before returning the four DTO columns. Exception rollback restores transaction state; no secret or policy value is returned publicly. The bridge does not regrant consent or make an acceptance a retention permit.

Its caller maps named evidence-invalid/ineligible/acceptance-conflict SQL failures to `409 CONFLICT` and all unexpected/permission/provider failures to `503 NOT_READY`, with existing generic R25 envelope, no SQL message or row detail. Any such failure rolls back B including a newly appended evidence row. Do not return a failure value from the callback that would nevertheless commit that newly appended row: throw a bounded internal rollback signal, catch it outside `transaction.ExecuteAsync`, then project the typed result. Zero-row ineligible/non-retained is normal success without RawCaptureAcceptance, not that rollback signal.

The response uses existing `RawCaptureAcceptanceDto`. Closed mapping:

| Stored/ingress RawClass | Evidence response/Agent slot |
| --- | --- |
| ChipDg2Portrait | RawChipDg2Portrait |
| LiveSelfieImage | RawLiveSelfieImage |

No identity/principal/consent/key/internal custody fields are added to evidence response. Acceptance and evidence are visible to an observer only after the same B commits. Crash/rollback leaves neither half; response loss replay returns the same durable acceptance.

### 3.2 Completion actor, protected selection and exact failures

`EfVerificationFinalizationBoundary.TryFinalizeAsync` owns the real finalization transaction. Append `Guid? CompletionPrincipalId = null` to `VerificationFinalizationWrite`; `VerificationCompletionApplicationService.CompleteAsync` passes authenticated `caller.PrincipalId` without using KeyPrefix/ApiKeyId or changing the existing completion-audit payload/hash. This optional constructor tail preserves existing non-retained test callers; it never provides authority for retained completion. Empty/missing principal is rejected for retained work. The completion caller remains the existing authenticated BusinessConsumer of the owned Client; it need not become the frozen custody principal, and cannot replace that principal on a source. No additional public field is added.

Proposed protected selection bridge (same A3 migration; deployer SD/pg_catalog, EXECUTE only tagekyc_runtime, no PUBLIC/direct-table grants):

```sql
tagekyc.raw_export_select_runtime_sources_on_completion(
 p_session_id uuid,p_client_id uuid,p_completion_principal_id uuid)
RETURNS TABLE("Required" boolean,"RawClass" text,
 "CaptureAcceptanceId" uuid,"CaptureRevision" integer)
```

Within finalization B, take the existing A1 session70 advisory lock `pg_advisory_xact_lock(hashtextextended(p_session_id::text,70))`, then lock/revalidate owned session. This precedes any acceptance lock and serializes R21/R25/R26 activity; caller cannot choose a binding/source. An exact SourceRetention execution binding makes selection required; HistoricalNonRetained/NonRetained/no binding emits one `(false,NULL,NULL,NULL)` row and performs no selection. Retained requires nonzero completion principal; save/set actor to that authenticated principal, call unchanged `raw_export_select_capture_acceptances_on_session_completion(uuid,uuid)`, require exactly its two nonempty class/acceptance/revision rows, then restore prior actor. Return two Required=true rows. This bridge reads protected binding tables internally; EF does not gain a new SELECT grant. Existing source/custody principal remains frozen and is not compared to a key prefix or silently transferred to this completion actor.

Call before `ApplySession` or adding decision/package/manifest/completion-audit. Both classes must be Available, exactly matching the existing selection predicate; no earlier Completed, source-ID input or single-class completion. Read/serialize Completed replay from existing state without a new selection. SQL failure rolls back the entire B, including any partial first-class selection. Existing signer computation before B is not a persisted completion and is not promoted to one on failure.

| SQL/boundary condition | Persistence result | Application HTTP result / residue |
| --- | --- | --- |
| Missing required class (`RAW_EXPORT_SESSION_CAPTURE_SELECTION_MISSING`) | StateMismatch | 409 FINALIZATION_CONFLICT, no selection/Completed/decision/package/manifest/audit mutation |
| Multiple or different pinned candidate (`..._AMBIGUOUS`, `..._CONFLICT`) | StateMismatch | Same 409, no internal class/source/SQL detail |
| Missing/empty retained completion principal or owned-session Client mismatch | AccessDenied | 403 ACCESS_DENIED, generic message, no selection |
| Unexpected PostgreSQL/DbUpdate failure, permission/actor-setting error, invalid bridge return shape | NotReady | 503 NOT_READY, generic message, no SQL details; never mislabel as409 |
| Existing optimistic concurrency mismatch | StateMismatch | Existing409 behavior |
| Applied / AlreadyCompleted / NotFound | Existing matching status | Existing success/replay/SESSION_NOT_FOUND behavior |

The completion application switch must explicitly handle AccessDenied/NotReady; the persistence boundary catches DB errors around open/begin/select/save/commit, not only DbUpdateConcurrencyException. Unknown commit is NOT_READY without asserting that durable state rolled back; retry uses existing Completed replay. TryCancelAsync and its R26 historical/durable semantics are not changed. Named proofs cover null actor, different key prefix with same true principal, each named SQL denial, unexpected database failure, prior actor restoration and observer-visible atomic rollback of first-class selection.

## 4. Agent raw transport — existing CRT1, separate streaming entry

The landed `CaptureRuntimeWireCodec.Crt1` accepts only bounded JSON control bodies. Do not weaken it to permit raw bodies or copy up to 64MiB into its `exactBody` parameter. Add a bounded metadata-only helper in the same codec:

```csharp
byte[] Crt1RawIngress(Guid credentialId, long generation,
 DateTimeOffset signedAtUtc, ReadOnlySpan<byte> nonce,
 RawExportSourceIngressMetadata metadata);
```

This emits the existing CRT1 eleven lines using the existing Lines formatter (UTF-8, LF, final LF). Method POST; path `/api/ekyc/raw-export/source-ingress`; credential N-lowercase; positive decimal generation; existing CRT1 UTC-Z timestamp; nonce base64url; media `image/jpeg`; exact plaintext length decimal; lowercase SHA-256 of the retained body; binding `ingress=IngressMetadataSha256=<digest>`.

The metadata digest is SHA-256 of the following **ordered** ten `header=value\n` lines (UTF-8, final LF), exactly matching the landed server `TryComputeIngressMetadataDigest`. No JSON canonicalization, trimming, sorting incoming headers, aliases or normalizing malformed input:

```text
X-TagEkyc-Agent-Configuration-Revision
X-TagEkyc-Verification-Session-Id
X-TagEkyc-Capture-Artifact-Id
X-TagEkyc-Capture-Revision
X-TagEkyc-Raw-Class
Idempotency-Key
X-TagEkyc-Captured-At-Utc
X-TagEkyc-Retention-Started-At-Utc
X-TagEkyc-Retention-Expires-At-Utc
X-TagEkyc-Retention-Budget-Seconds
```

IDs use the already-landed lowercase UUID-v4 N grammar; numeric fields use invariant positive decimal; these three metadata timestamps use `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` with literal `+00:00`, **not** the CRT1-Z timestamp formatter. Body SHA is derived once from exact retained bytes, never artifact/evidence aggregate hashes. Media, digest and length are bound directly by CRT1 as well as validated by request grammar.

Add `CreateRawIngressHandler` with `SocketsHttpHandler.Expect100ContinueTimeout = Timeout.InfiniteTimeSpan`, same no-redirect/proxy/cookie/decompression/credential posture as control handler. Each send sets ExpectContinue=true, exact Content-Length and image/jpeg, no chunking/compression/trailers; signs a fresh timestamp/nonce with the current installed key exactly once. The raw content wraps the existing lease bytes without copy, file, multipart or SerializeToStreamAsync eager staging. A manual retry uses the same ingress idempotency key, artifact/revision and original frozen metadata but a fresh CRT1 nonce/signature. It is not a generic HTTP replay handler.

No body send fallback timer. An operation deadline cancels the request, never starts its body. Deadline is the minimum of remaining monotonic lease lifetime less frozen safety margin and the existing execution horizon; an elapsed/nonpositive interval returns client-local RECAPTURE_REQUIRED without HTTP/DB activity. Pre-admission early network memory remains bounded as in the broker/transport qualification; the application cannot own/read body before committed R1. SDK/real hardware qualification is not inferred from synthetic proof.

## 5. Retained owner and submit-only wiring

Activated `CaptureAgentOrchestrator.RunSubmitOnlyAsync` is the only new device-to-ingress consumer. `RunAsync` legacy/demo and VerifyAndDiscard stay independent. Host and WPF composition inject `CaptureRuntimeHttpClient`, the existing server config cache and a single `RawExportRetainedSubmissionOwner`; missing dependency fails retained mode closed before device delegation. Configuration may disable raw but cannot enlarge a buffer's lifetime. No local manually supplied retention policy/API key is added.

### 5.1 Managed local gate is a request, not authority

Source reality: `CaptureAgentProductionConfigGate.ValidateCore` calls `ValidateRetentionMode` before its Demo/Integration/Production branch. The landed retention validator admits only VerifyAndDiscard and throws `RETENTION_POLICY_UNSUPPORTED` otherwise. `ValidateManaged` already rejects all three Client/legacy API-key settings and calls that same core. Removing only the orchestrator guard leaves this earlier production gate closed. Both exact files are proposed M rows; do not claim retained composition is reachable without them.

The successor gate must distinguish its existing Managed entry from the legacy `Validate` entry (use its existing `requireLegacyCredentials` distinction, not a new environment flag). VerifyAndDiscard preserves existing behavior in both. RawVault is a supported **local request** only via `ValidateManaged` in Integration/Production submit-only composition with the A3 retained dependencies; legacy `Validate`, Demo, unknown mode and missing A3 composition still reject it with `RETENTION_POLICY_UNSUPPORTED` or the existing dependency `NOT_READY`. Continue every existing transport, consent, host, capacity and credential check; do not return early from `ValidateCore` on RawVault. No local value changes Prepared/Activated or production authorization.

A supported local request is not retained admission. Coordinator/orchestrator must require current authenticated server configuration for the installed Agent, its enabled retained setting and original freshness/budget constraints, plus the exact confirmed retained capability/binding and SourceRetention authority at the existing server checkpoints. Local mode, receipt contents, an API key, a supplied principal or a fallback/default can grant none of these. Missing/disabled/expired/not-yet-effective/stale/wrong-bound configuration denies retained capture before device/plaintext ownership; missing retained dependencies denies before delegation. The reference/permit and withdrawal checks remain E01/CP, not a second local consent authority. VerifyAndDiscard does not acquire a retention-permit prerequisite.

### 5.2 Owned plaintext and server admission

At first application ownership of DG2/selfie bytes, reserve per-class/host capacity and freeze configuration revision, budget, safety margin, monotonic start/deadline and UTC projection. This happens at capture return, **not** after evidence acceptance. Engines use the same owned buffer under a bounded borrowed lifetime. Add a `SensitiveByteBuffer`-accepting lease ownership transfer to prevent a second plaintext copy or reset of origin. The transfer must make ownership singular; scope disposal and lease disposal must not race an active reader. FaceMatch must finish before releasing its DG2/selfie inputs. Liveness bytes are never adopted by a retained raw lease.

After R25 returns an exact RawCaptureAcceptance for that class, the owner verifies Session/Artifact match, freezes one v4 IngressIdempotencyKey for that lease and submits it. Absent acceptance is not synthesized. Local validation/engine failure, cancellation, withdrawal, disabled configuration or original expiry zeroes owned uncommitted bytes and stops new raw sends. Server custody at/after R1 is not undone by local disposal.

| Result | Local action |
| --- | --- |
| Available / AlreadyAvailable | Verify S shape; append existing raw submission slot with SourceArtifactId; release/zero lease once its local engine borrower is finished |
| ResumePending | Release/zero plaintext; preserve a sanitized pending receipt; do not resend body or start R2; server continuation owns ciphertext |
| EvaluationInProgress / IdempotencyBusy / CapacityUnavailable / CapabilityUnavailable / ReservationBusy / TemporarilyUnavailable | Return exact outcome; only explicit same-lease retry within original deadline is allowed; no background unbounded resend |
| ClaimRestartRequired / ClaimTokenInvalid | The next same-lease public retry starts private begin again; there is no public token/init/restart route |
| HistoricCommitmentKeyUnavailable | No key fallback; bounded same-lease retry only after dependency recovery |
| FingerprintConflict / BindingInvalid / NotFoundOrNotAllowed / SourceRetentionNotAuthorized / PlaintextRetentionInvalid / ArtifactSizeLimitExceeded / ContentCommitmentMismatch / RecaptureRequired / TransportProtocolInvalid | No same-buffer revival; zero/dispose; preserve already accepted metadata and durable server residue |
| Response lost / malformed / unknown | Do not claim success or rollback; preserve only the original live lease for an explicit bounded retry; after loss/expiry, client-local recapture with no new call |

No automatic retry policy or new polling default is invented. The existing caller retry schedule remains bounded by the frozen lease; server-supplied RetryNotBeforeUtc is present only for evaluation. Journal never stores raw, raw body hash for retry reconstruction, or raw send secret. Process restart loses plaintext and cannot reconstruct a raw request from an acceptance/receipt; it must recapture via the existing lifecycle, not secretly redelegate an A2 binding whose DelegationAttempted CAS already won.

### 5.3 Six journal operations, two receipt-only source results

The landed `CaptureRuntimeAgentCoordinator.ReserveSubmission` and `SubmissionSlotAllowed` admit exactly the six R24/R25 metadata slots. `AcknowledgeCompletedReceipt` currently compares all receipt slots against those six, so an eight-slot A3 receipt cannot close. Amend that coordinator's receipt validation, not the journal capacity. `CaptureRuntimeAgentSession.ReceiptWritten` in the already-allowlisted `CaptureRuntimeHttpClient.cs` remains the caller; it cannot derive the expected mode from the receipt's slot count, terminal status, RuntimeMode or EvidenceMode.

The coordinator freezes the requested VerifyAndDiscard/RawVault receipt shape with the exact current BindingId only after the current-process delegation CAS succeeds and before device invocation. Use one shared private mode-aware ClaimDelegation core: the existing public `ClaimDelegation(Guid bindingId)` supplies explicit VerifyAndDiscard (six-only, never retained authority); Managed `RunSubmitOnlyAsync` calls that core with its selected mode after the server configuration/binding and §5.1 checks. Both use the same existing journal CAS; only its winner establishes the live context. A failed/replayed CAS establishes no context. Existing A2 direct-call tests already call ClaimDelegation before acknowledgment, so they establish six-only context without fixture or journal changes.

Keep the context bounded and process-local (one exact binding/mode, no secret/body/digest); do not add a journal field. RawVault context exists only for the Managed retained execution admitted by §5.1. A later configuration change cannot switch an already-running execution to the six-slot success shape or manufacture missing raw slots. Receipt acknowledgment uses that exact binding's frozen mode, never the sender's assertion. Keep it through `RunSubmitOnlyAsync` finally and receipt write so the subsequent `CaptureRuntimeAgentSession.ReceiptWritten` can consume it; clear only after successful acknowledgment or terminal retirement. Missing or mismatched live context is **Unknown**, never VerifyAndDiscard: reject BOTH six-slot and eight-slot receipt closure with `SUBMISSION_STATE_UNKNOWN` before partition validation, leaving journal lineage unchanged. After restart the already-won DelegationAttempted CAS cannot establish a replacement context; neither a truncated six-slot receipt nor a complete eight-slot receipt revives/closes that uncertain attempt. Existing R22-first recovery and expiry retirement remain; no new raw-recovery authority follows. Valid live A2 six-slot acknowledgment retains its original outcome and durable ACK checks.

`CaptureRuntimeSubmissionOperation` remains **metadata-only**: its mandatory BodySha256 is for the existing R24/R25 JSON payloads, not raw. Preserve six-element capacity and the exact whitelist in `ReserveSubmission`, `SubmissionSlotAllowed` and `WindowsCaptureRuntimeJournal`; no raw slot or raw body digest is persisted in this array. Raw slots are not reserved or acknowledged by `ReserveSubmission`/`AcknowledgeSubmission`. No journal schema/version/capacity amendment is authorized.

For `AcknowledgeCompletedReceipt`, all existing HandoffAccepted/DelegationAttempted/exact binding/session/`submitted_all`/durable metadata-ACK conditions remain. First require exactly six journal entries, unique allowed metadata slots, all ResultAcknowledged=true, and compare their exact slot set to the **metadata partition** of AcceptedSubmissions. Then validate the closed receipt shape:

| Frozen live execution | Exact accepted receipt | Failure |
| --- | --- | --- |
| Live VerifyAndDiscard / existing A2 non-retained CAS winner | Exactly the original six metadata slots, once each; zero raw or other slots | Existing `SUBMISSION_STATE_UNKNOWN`; no journal closure |
| Managed retained RawVault | Exactly those six metadata slots once each plus exactly one RawChipDg2Portrait and one RawLiveSelfieImage, total eight; no unknown/duplicate/ninth slot | Same fail-closed error; keep PendingBind/submission lineage, no fake ACK/history |

Raw entries use the existing `CaptureAgentAcceptedSubmission` shape, not a new receipt DTO:
- SubmissionSlot is exactly `RawChipDg2Portrait` or `RawLiveSelfieImage`.
- SubmissionKind is the literal `raw_export_source`.
- SubmissionId is the **nonempty server-returned SourceArtifactId**, formatted `Guid.ToString("D").ToLowerInvariant()` (canonical lowercase UUID-D, distinct from CRT1 metadata UUID-N). Validate parse-exact D plus ordinal reformat equality; reject empty/uppercase/N/unknown kind. Each class uses its own exact submitted acceptance/source result, never an artifact/evidence ID; the two class source IDs must be distinct.

The raw submission owner constructs each such entry only from a validated S response `Available` or `AlreadyAvailable` for that class's exact request. Neither a private Handoff nor ResumePending/denial/body-send completion counts. Receipt validation does not itself grant source authority: server S production and its acceptance/class correlation are proved separately at the actual raw sender. No additional source lookup, raw journal entry, digest, secret, lease extension or raw retry identity is invented to validate the receipt.

Only the valid six+two receipt may close a retained successful attempt; valid six-only closes VerifyAndDiscard as before. Missing one or both raw slots cannot downgrade a retained attempt to non-retained success. ResumePending -> existing `submission_state_unknown` plus NextAction `WAIT_FOR_SOURCE_RECONCILIATION`, not fake raw slots or an export-ready claim. A missing/malformed receipt leaves the original bounded recovery/expiry semantics, never a fresh DelegationAttempted. This receipt contains no consent reference supplied as new authority, broker details, raw/key bytes or export permit.

## 6. Discriminating proof joins

| Proof | Actual boundary / positive control | Mutation expected RED |
| --- | --- | --- |
| A3_S02_All36RowsHaveExactlyOneOwner | Parent table ↔ enum ↔ mapper ↔ test data, exact sets/counts | Drop/duplicate any row or expose an excluded row |
| A3_S02_All21BusinessResultsRoundTripThroughRuntimeHttpAndClient | Real R27 HTTP mapper + production runtime Agent parser, each status | Throw-before-parse on 409/413/422/503 or wrong status/code |
| A3_S02_ResultShapeCrossProductFailsClosed | All O/E/S valid baselines then each missing/forbidden field | Emit null/forbidden SourceArtifactId or optional evaluation retry |
| A3_R25_AcceptanceCommitsWithEvidenceAndReplaysExactly | Real R25 B + observer, lost response, dedup, changed policy config | Commit evidence first; mint new acceptance on replay |
| A3_AcceptancePolicyIsConfiguredAndReplayUsesStoredVersion | Real Application validated-session Client -> Find -> typed writer argument -> bridge in B; explicit Client policy, exact prior acceptance and changed current version; non-retained/ineligible with absent entry succeeds without acceptance; retained prior tuple remains replayable with null current entry | Drop/change typed argument or use Agent Client; fresh retained missing/duplicate/default policy admitted; stored replay rejected by current policy; absent entry makes non-retained evidence fail |
| A3_CompletionActorAndFailureProjection | Real completion caller → B, correct actor and both Available classes | Infer UUID from prefix; omit actor; swallow selection error into success or unexpected DBerror into409 |
| A3_CompletionRequiresRetainedSourceSelections | Actual Client completion → finalization B, two Available sources baseline | Remove one selection/join; Completed/decision/package/audit must remain absent |
| A3_AgentRawCrt1MatchesServerParser | Production helper → real server parser → real signature Verify | Metadata timestamp Z, altered header order, aggregate hash or missing final LF |
| A3_AgentRawExpectDoesNotSendBeforeCommittedR1 | Real HttpClient/Kestrel + transaction observer + counting stream | Finite expect timeout, proxy buffering or intermediary auto-Continue |
| A3_AgentRetainedLeaseStartsAtCaptureNotAcceptance | Delayed evidence receipt, monotonic clock, actual buffer owner | Reset deadline after acceptance/retry or copy plaintext to uncounted owner |
| A3_RawReceiptDoesNotClaimPendingAsAvailable | Runtime source rows + production receipt | Add raw slot for ResumePending or dispose custody state on local cancel |
| A3_ManagedRetentionGate_RequiresServerAuthority | Production ValidateManaged -> coordinator -> real submit-only wiring with synthetic retained config/binding and S results; VerifyAndDiscard control | Restore unconditional RETENTION_POLICY_UNSUPPORTED -> retained positive RED; permit missing/disabled/expired config, missing A3 owner or local/legacy-key authority -> denial controls RED |
| A3_ReceiptClosure_SixMetadataPlusTwoPublishedSources | Actual coordinator and journal + ReceiptWritten caller; six-only VerifyAndDiscard closes; six ACK metadata plus two S-derived raw entries closes retained | Restore full-receipt=six comparison; accept missing/both-missing/duplicate/unknown ninth/raw kind or ID malformed; each intended assertion RED |
| A3_RawReceiptNeverConsumesJournalSlotsOrPersistsRawDigest | Actual bounded Windows journal readback after both raw uploads, six metadata entries only; verify raw publication through server observer | Route raw through ReserveSubmission, increase cap/whitelist, persist raw digest or derive new retry after restart -> RED |
| A3_ReceiptShapeCannotDowngradeOrReviveExecution | Actual CAS winner freezes mode; live six-only A2 and live S pair positive, including acknowledgment after RunSubmitOnly finally; config disable, forged six-only retained receipt and other binding reject. New coordinator on the same six-ACK durable journal rejects BOTH six and eight receipts with no state change; reclaiming already-won CAS creates no context | Choose mode from receipt length, default absent context to six, clear context in finally, or revive/redelegate after restart -> RED |
| A3_VerifyAndDiscardAndA2RecoveryUnchanged | Existing A2 suites plus raw-disabled path | Require retention permit on non-retained flow or re-open device after CAS |

Agent proof owners: `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RetainedOwnershipTests.cs` owns the four gate/receipt rows immediately above (and existing retained lease/receipt rows); `Tip88C1C6BA3RawIngressTests.cs` owns transport/parser controls. Both are already N rows in the inventory. Use actual coordinator/journal/ReceiptWritten boundaries, not a test-local set comparer; the opt-in Server A3 acceptance joins actual S replies and observer publication to the Agent receipt.

All proof names here are **required future tests**, not executed evidence. No product file has been modified by this companion.

## Changelog

- v0.6: F01 adds the missing Managed production-config gate seam; F02 separates six durable metadata operations from two S-derived receipt-only raw results and closes the coordinator/caller/proof joins. Preserve v0.5 bytes and all consent/transport authority decisions.

- v0.5: pin coordinated A1 result amendment, acceptance/completion transaction joins, exact raw CRT1 entry and retained ownership/receipt mapping. No new consent action or global JSON canonicalization.
