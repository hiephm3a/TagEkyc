# TIP-88C1-C6B-A3 — Source retention authority and ingress composition

**Version:** v0.3 — semantic ratification and contract materialization candidate
**Date:** 2026-09-13
**Status:** S01/S05 SEMANTICALLY CLOSED; IMPLEMENTATION DISPATCH NOT READY
**Authority this turn:** documentation/census/review only. No product/schema/test/config/project mutation; no stage/commit/push; no A4/production.
**Predecessor:** `tip_88c1_c6b_a3_broker_composition_dispatch_v0_2.md`, SHA `6A2FD46B024C28ED1E5D5D164387B8FCCE2F437FDF9A6ADCD4695E4EA8CD4C7D`, preserved.
**Server HEAD:** `5df5f60a6dc4d992c71fc2b160673e4abca6488d`
**Agent HEAD:** `e3bd625bbe357b1f3c9620d20bcba121cfb61974`

## 1. Homeowner decision record — operative semantics

Source: Homeowner attachment `7e310eea-adf2-457d-b68c-e3957ebf9319/pasted-text.txt`, raw SHA-256 `5FD38341CC41D188301E5259C436B483CFED53952939073F4F4177388DACDA3D` (8,289 bytes / 265 lines), supplied 2026-09-13. This section records the decision in the repository; it is not Builder self-authorization.

**RATIFIED: SourceRetention authority before Completed is distinct from RawExport authorization after Completed.**
**RATIFIED: S01 direction 1, authenticated BusinessConsumer principal lineage; generic custody/service principal direction rejected.**
**S01/S05 semantic disposition: CLOSED BY HOMEOWNER.** Missing executable evidence/DDL is not permission to reopen these choices or call them undecided.

The retained path is:
`authenticated Client P -> pre-completion retention authority -> capability -> binding -> accepted DG2/selfie -> R1/R2/R3/R4/R5 -> Available -> applicable Client completion -> Completed -> existing export authorization -> export permit`.

- Existing `EfRawExportAuthorizationRepository` and its `SESSION_NOT_COMPLETED` gate stay unchanged.
- Existing B2 subject-export consent remains Completed-gated and purpose `SubjectRawBiometricExport`.
- SourceRetention grants custody/encryption/sealing only. It grants no plaintext export/read, package, recipient delivery or permission to bypass later export authorization.
- Available does not mean Completed, export-authorized or deliverable.
- Persist non-empty authenticated `PrincipalId + ClientApplicationId + VerificationSessionId` from issuance. Carry that immutable lineage through capability, binding, custody and R2–R6 audit.
- Agent neither receives a Client API key nor supplies/selects a PrincipalId.
- R20b Replace within a lineage cannot change P1 to P2. An explicit new authority issuance is required for a principal change; Replace is not that operation.
- Retention authority binds policy ID/version, exact allowed classes, durable consent evidence, revision/issuance/expiry and purpose SourceRetention. An export decision/permit is never accepted in its place.
- Fresh R1 rejects expired/revoked retention authority. After committed R1, existing checkpoint/continuation/reconciliation rules apply; do not add arbitrary deletion or a new runtime-auth checkpoint.
- Synthetic/non-patient only. Legal-hold/purge and production activation remain separately gated. Private HTTP broker bootstrap and deferred HTTPS/mTLS remain unchanged.
- This semantic ratification does **not** grant implementation authority.

### Intent / non-claims

A3 connects the already-built A1/A2 and custody stages; it does not invent another upload protocol, encryption algorithm, Recipient model or SignFlow/TSP workflow. There is one raw-ingress operation, no multipart/init/part/complete/resumable public API. This proposal changes source-retention authority, not export semantics.
The v0.2 finding was not “upload infrastructure missing”: it was an absent authority lineage and a Completed/permit/Available cycle. Both are resolved semantically above; the actual pre-completion consent input still needs a concrete producer (§3).
All proposed types, mappings and functions below are **candidate design**, not landed components or independently ratified implementation detail.

## 2. Repository-real census

“Missing” below means no implementation/caller found under product `src`, not absence of a test double or document name.
Existing files were read, and inspected product source matches HEAD. Historical/untracked docs are separately labelled in §10.

| Resource | State | Evidence / consequence |
| --- | --- | --- |
| Public R27 + CRT1/N | LANDED | `RawExportSourceIngressEndpoints.cs:25-106`; nonce commit precedes one call with the unread Stream |
| A3 admission/readiness ports | LANDED CONTRACT / IMPLEMENTATION MISSING | `CaptureRuntimePorts.cs:78-106` (admission); `CaptureRuntimeStartup.cs:17-19,51` (readiness); startup requires a real A3 implementation only in Activated |
| Legacy ingress application | LANDED / NOT REUSABLE AS RUNTIME AUTHORITY | `RawExportSourceIngressApplicationService.cs:20-78` uses AuthenticatedClientContext and artifact-derived identity |
| Legacy acceptance resolver | LANDED / READER ONLY | `RawExportCaptureAcceptanceResolver.cs`; AllowedCaptureAgentIds is not a runtime binding |
| Claim-comparison broker | LANDED / IN-PROCESS COMPONENT | `RawExportSourceClaimComparisonBroker.cs`; different interface from IRawExportSourceIngressBroker; opens its own completion transaction |
| Private ASP.NET broker host + HTTP adapter | ACTUAL_MISSING | No broker host project or product ingress-broker implementation |
| Retained begin + complete/handoff + terminal SQL | LANDED | `20260906055502_Tip88C1C6BIngressSqlComposition.cs:273-379`; do not recreate or rewrite this migration |
| R2/R3/R4/R5/R6 | LANDED INTERNAL STAGES / COMPOSITION MISSING | Infrastructure-owned types; no complete stage registration/façade exists |
| Evidence acceptance writer | SQL LANDED / PRODUCTION CALLER MISSING | same migration:380-417; evidence response constructors currently leave RawCaptureAcceptance null |
| Session-completion selection | SQL LANDED / PRODUCTION CALLER MISSING | same migration:418 onward; do not confuse durable Available with selected completion inputs |
| Active Agent raw sender | ACTUAL_MISSING | CaptureRuntimeHttpClient does not implement SubmitRawExportSourceAsync; interface default throws |
| Old Agent raw sender | LANDED / OBSOLETE FOR ACTIVATED | TagEkycHttpClient uses API key, which R27 deliberately rejects |
| Retained lease | LANDED / DEVICE-TO-INGRESS CONSUMER MISSING | RetainedRawBufferCapacity exists; device/orchestrator paths do not use the lease for raw submission |
| Durable key provider | ABSTRACTIONS/READINESS LANDED / PRODUCTION ADAPTER NOT ESTABLISHED | registration needs IKekOperationProvider and recovery; fixture implementation is not production evidence |
| Object capabilities | LANDED / ROLE-SPECIFIC | provider registration selects Writer, Reconciler or Lifecycle; a single registration does not provide all three |
| Capacity | LANDED / PROCESS-LOCAL | RawExportIngressCapacity reserves declared length; Program supplies custody-window limits. Not a proven deployment-wide quota |

A2 v0.4:276-279 explicitly leaves R27 signing/transport to A3; filling that seam does not reopen A2's installation/receipt/DelegationAttempted semantics.


## 3. Pre-completion consent evidence — actual source census, not invented reuse

### E01 — evidence producer is ACTUAL_MISSING in the inspected product

| Candidate | Evidence | Disposition |
| --- | --- | --- |
| B2 consent append | `20260720022629_Tip88B2SubjectExportConsent.cs:673-755` | Not usable pre-completion; explicit Completed gate and export purpose |
| Latest replacement of that append | `20260724015546_Tip88B1E3ResolverReadBoundary.cs:34-120` | Still rejects non-Completed at :71-74; still uses SubjectRawBiometricExport. E3 did not remove the gate |
| Session creation | `BusinessConsumerContracts.cs:13-31`; `VerificationSessionApplicationService.cs:138-154`; `VerificationSessionRow.cs` | SubjectRef, purpose and Client authentication exist; no retention consent assertion/text-version/artifact evidence is recorded |
| Passed capture/evidence | `EvidenceResultRow.cs`, capture/evidence contracts | Proves verification results, not the subject's consent to retention; cannot substitute |
| Client API authentication | `AuthenticatedClientContext` | Identifies the caller, not proof that the subject agreed |
| A1 capability/binding | `CaptureRuntimeEntities.cs`; `CaptureCapabilityPersistenceRequest` | Authority lineage extension authorized, but no existing pre-completion consent producer |

The inspected production consent APIs/repositories are subject-export machinery. No existing callable pre-completion source-retention assertion ingestion/validation/withdrawal path was found. No database/environment evidence outside these repositories was inspected.

**Required input contract, not a claimed existing record:** durable evidence must bind exact session/subject, authenticated attesting principal and Client, SourceRetention purpose, policy/version, exact class set, consent text/version/content commitment or equivalent reviewed assertion provenance, capture time/validity, immutable evidence ID/revision and withdrawal/current-status semantics. Raw biometric content is not part of this evidence record.

A Client-supplied reference string, a checkbox boolean, valid API key, Passed evidence or nonempty SubjectRef is insufficient by itself. Before freezing a new intake, identify who attests, how they are authorized to record the subject's choice, how the evidence is authenticated and bound to this session, and how withdrawal is observed. No synthetic decision insertion can discharge this obligation.

The Homeowner was asked where the actual consent record is captured. Until that input/producer is known, this document does not invent an assertion issuer, signature verifier, new trust root, UI workflow or proof of subject consent. This is **one concrete evidence-input dependency**, not reopening S01/S05 and not a new TIP. If no source exists, a minimal new retention-evidence intake must be explicitly designed/reviewed in A3; implementation cannot assume it exists.

## 4. Retention authority and immutable lineage contract

### 4.1 Separate durable authority class

Proposed logical records are named `SourceRetentionDecision` and `SourceRetentionPermit`; these are **new, not aliases** for export tables. Grant creation is server-owned after E01 evidence validation, before capability issuance, never by calling AuthorizeExportAsync.

| Required value | Candidate exact type / source | Rule |
| --- | --- | --- |
| RetentionAuthorityId | UUID, server generated | Nonempty; exact lookup, never latest/first export decision |
| Revision | Int64 | Positive, immutable referenced revision |
| PrincipalId | UUID from authenticated BusinessConsumer context | Nonempty; cannot be body-selected |
| ClientApplicationId | UUID from same context | Must own exact session |
| VerificationSessionId | UUID from owned durable session | Not a caller-chosen foreign session |
| PolicyId / PolicyVersion | UUID / Int32 from server-owned eligible retention profile | Positive version; exact immutable policy version |
| RawClasses | Closed set: ChipDg2Portrait, LiveSelfieImage | Nonempty subset within evidence and profile; no LivenessMedia; exact canonical set |
| ConsentEvidenceId / ConsentEvidenceRevision | UUID / Int64 from validated durable E01 source | Nonempty/positive; exact provenance, not B2 export consent |
| Purpose | Closed literal SourceRetention | Cannot become SubjectRawBiometricExport |
| IssuedAtUtc / ExpiresAtUtc | Database timestamptz | Issued < expiry; finite expiry no later than applicable evidence/profile/session horizons |
| State | Granted, Revoked, Expired | Revocation/expiry cannot restore Granted on the same revision |

This is a field/authority contract, **not complete DDL**. E01's physical evidence schema/FK/issuer must close before literal new tables/functions/migration/ACL can be frozen. Existing export authorization tables, their FKs, purpose checks and public response semantics are not repurposed.

### 4.2 Capability/binding propagation

Future `CaptureCapabilityPersistenceRequest` must add authenticated PrincipalId and the exact server-selected RetentionAuthorityId/Revision for retained mode. Its current Client-only actor partition cannot be reused to hide a principal difference; the candidate capability fingerprint/operation partition must bind the new lineage. Existing non-retained paths must remain explicitly classified, not assigned fake retention authority.

- R20a: authenticate BusinessConsumer, own session, validate eligible nonterminal session and exact current SourceRetention evidence/permit; persist lineage atomically with capability and operation/event.
- R20b: lock existing capability; authenticated caller principal must equal the persisted principal; preserve authority lineage. A changed principal yields existing non-disclosing denial and **no replacement/secret/event**. New authority issuance is not silently performed inside Replace.
- R21: copy lineage from the exact server capability to binding under the existing bind transaction; no new principal wire field, no lookup by “any Client principal”.
- R22 reconciliation: return existing binding result, no change of lineage, no secret re-emission.
- R27 B: resolve binding from authenticated runtime plus signed session; exact equality of capability/binding/retention lineage. Derive actor from that record; never from Agent, operator or runtime credential ID.
- R2–R6: actor comes from the frozen custody lineage and durable handoff/readback, not reselected current Client credentials.
- Old rows with absent retention lineage cannot be silently backfilled from today's caller, audit prefix or an arbitrary API key. Exact migration/cutover disposition must distinguish non-retained records from retained claims.

No ownership-transfer API, automatic P1→P2 migration or retrospective consent is introduced. API-key rotation that preserves the same authenticated PrincipalId does not itself select a new authority principal; credential lifecycle and actual authority revocation remain separate facts.

### 4.3 Ordered lifecycle and transaction separation

```text
Owned non-Completed session + authenticated BusinessConsumer P
 -> valid pre-completion evidence + eligible SourceRetention policy
 -> persist retention decision/permit
 -> capability issue preserving P/Client/session/authority lineage
 -> Agent binding
 -> Passed evidence -> exact acceptance
 -> Agent retained lease + CRT1 Expect request
 -> A1 authenticate; N commits; same unread Stream passed once
 -> A3 capacity/config checks
 -> metadata broker B: bound-runtime + acceptance + exact retention checks
 -> begin -> keyed metadata -> complete/handoff -> B/R1 commits
 -> first application Request.Body read
 -> R2 encryption -> durable verifier -> R3 -> R4 -> R5 Available
 -> Client required-source selection -> Completed
 -> existing export eligibility + export consent -> export permit
```

R6 durable cleanup/reconciliation still applies independently of HTTP receipt loss. Existing replay before body-read does not create new custody. A1 reference to Stream is not plaintext ownership. Keep `SocketsHttpHandler.Expect100ContinueTimeout = Timeout.InfiniteTimeSpan`; no pre-R1 application read/copy, provider writer or temp spill. Bounded unavoidable network buffering is not confused with application buffering. No explicit Kestrel interim-flush API is required.

Consent withdrawal/expiry before fresh R1 must serialize with its exact validation. After R1, retain committed state and use existing checkpoint outcomes; never infer “delete” from an expired HTTP request. Finite lock/CAS details for the new evidence source remain part of E01-dependent DDL, not a license to skip serialization.

## 5. RET-01–RET-12 trace / test-bite matrix

All rows below are ratified requirements, not claims that tests already exist or passed. Named tests are proposed A3 acceptance methods; evidence must be database/runtime output, never echoed inputs.

| ID | Owner / required behavior | Positive proof | Mutation expected RED |
| --- | --- | --- | --- |
| RET-01 | Retention repository and export repository: separate durable authority classes | RetentionAndExport_AreDistinctDurableAuthorities reads independent records | Feed retention ID into export-permit path and accept it |
| RET-02 | Retention issuance: allowed before Completed only with valid evidence | FreshSession_IssuesRetentionBeforeCompletion reads non-Completed state and real permit | Restore Completed prerequisite in retention issuance |
| RET-03 | Existing export repository: Completed remains mandatory | ExportBeforeCompletion_RemainsDenied reads SESSION_NOT_COMPLETED | Remove export state gate |
| RET-04 | C1/C3/package/delivery: retention alone grants none | RetentionOnly_CannotExportReadPackageOrDeliver with otherwise-valid actor | Treat retention permit as export authorization |
| RET-05 | R20/R21/custody: immutable authenticated Client principal | PrincipalLineage_IsDurableThroughCustody reads rows through separate observer | Substitute CredentialId/AgentId/ClientId/operator |
| RET-06 | API/Agent wire: no caller principal or Client key | AgentCannotChoosePrincipalOrUseClientKey through real HTTP parser | Accept PrincipalId field or API-key fallback |
| RET-07 | Issuance/B: exact authority tuple and class coverage | RetentionAuthority_ExactTuple validates each independently varied field | Drop one Client/principal/session/policy/class/evidence comparison |
| RET-08 | R20b: replace preserves principal | ReplaceCannotTransferPrincipal checks no successor/operation/event/secret | Accept P2 replacement of P1 |
| RET-09 | Completion service: both required sources Available | CompletionWaitsForBothRetainedSources through real completion route | Complete with one source absent/PendingVerification |
| RET-10 | Fresh B: no export decision dependency | FreshIngress_UsesRetentionWithoutExportDecision reads zero export permits | Query old export decision for fresh ingress |
| RET-11 | Export schema/consent unchanged | ExportAuthoritySchemaAndPurpose_RemainSeparate catalogue plus functional export controls | Store SourceRetention in export authority/purpose |
| RET-12 | End-to-end fixture uses actual issuance | FreshSession_RealEvidenceToAvailableToCompletion traverses actual evidence/authority APIs | Seed export decision or precomplete session to bypass missing source |

For every negative, first establish a matching positive fixture, mutate exactly the guard, observe intended RED, restore and rerun. RET-04 is not satisfied by calling an unmapped route or failing unrelated authentication. RET-12 cannot run meaningfully until E01 is implemented under a reviewed contract.
## 6. S02 — literal one-call outcome projection (candidate A3 amendment)

This section is a **proposed transport projection for review**, not a claim that A1 already has it. The frozen spine has 36 rows; only 22 belong to the one-call producer surface. ACCESS_DENIED belongs to A1, leaving **21 A3 business results + 14 excluded outcomes + 1 A1 outcome = 36**.

Sources: planning spine :1297–1356 and :3423–3458; canonical A1 OM `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md:958–1042`; API `RawExportSourceIngressEndpoints.cs:25–106`; Agent `TagEkycHttpClient.cs:87–90,273–288`.
Current A1 success forbids state/disposition and Evaluation permits absent retry; spine requires those fields. This is a real canonical contract change, not DI wiring.

### 6.1 Envelope and precedence

Preserve the A1 pre-N check order: mixed API/platform credentials -> metadata grammar/digest -> CRT1 parse -> required dependency availability -> authentication. Existing statuses remain 403 ACCESS_DENIED, 400 REQUEST_INVALID, 503 NOT_READY and 403 ACCESS_DENIED respectively. Existing `{code,correlationId}` envelope remains separate. N is committed only by successful authentication; metadata/parse/dependency rejection does not call A3. Authenticator rejection retains its existing nonce semantics, not a new blanket no-mutation promise.

After N, exact closed business shapes:
- **O:** `{outcomeCode}` only. Optional properties omitted, not explicit null.
- **E:** `{outcomeCode,retryNotBeforeUtc}`; retry required from persisted evaluation expiry.
- **S:** `{outcomeCode,sourceArtifactId,currentSourceState,currentDisposition}`; nonempty source UUID, state and disposition both Available from the actual published durable head.
- Unknown enum, wrong required/forbidden combination, malformed or unrecognized state -> existing 503 NOT_READY envelope; no raw/internal details, no second A3 call, preserve committed N. Do not drain body as error handling.

Within A3: exact runtime/binding/acceptance -> metadata/time/class/config -> capacity -> locked alias/idempotency/existing-claim classification -> fresh retention authority -> complete/handoff -> body/stage outcomes. An exact ExistingMatch replay does not turn into a fresh authority issuance. Locked replay/terminal precedence and cleanup are inherited from the exact stage transition, not a new global “latest failure wins” rule.

### 6.2 Complete 36-row ownership/status/shape ledger

`Pnn` denotes proposed method `A3_S02_O<nn>_ExactOutcomeShapeStatusAndResidue`; none has run. NO EGRESS means not an A3 business-enum member. Additional HTTP statuses below are draft choices; existing six business statuses remain unchanged.

| ID | Exact code | Owner/phase | HTTP/shape | Durable/body/retry rule | Proof |
| --- | --- | --- | --- | --- | --- |
| O01 | ACCESS_DENIED | A1 P0 | 403 existing A1 | No A3/body; original auth/N semantics | P01 |
| O02 | RAW_EXPORT_SOURCE_BINDING_INVALID | A3 P1 | 403 O | No body/R1; do not disclose other binding | P02 |
| O03 | NOT_FOUND_OR_NOT_ALLOWED | A3 P1 | 403 O | No existence disclosure/new custody | P03 |
| O04 | RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID | A3 P1–P4 | 400 O | No body; if R1 exists use exact pre-start terminalization/fenced cleanup | P04 |
| O05 | RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE | A3 P1–P3 | 503 O | No body/R1; preserve existing Evaluating shell/token; original budget only | P05 |
| O06 | RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED | A3 P1/P5 | 413 O | Declared excess: no body; actual excess: stop first excess, terminal exact attempt/cleanup | P06 |
| O07 | RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID | A3 P1/P3 | 422 O | No body/R1; preserve anti-reuse identity; no same-buffer retry | P07 |
| O08 | RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE | A3 P2 | 503 O | No alias/R1/body; release partial capacity; bounded same-buffer retry | P08 |
| O09 | RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY | A3 P3 | 409 O | No new source/attempt/read; preserve exact current residue; bounded backoff | P09 |
| O10 | RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS | A3 P3 | 409 E | Exact persisted retry time; no new token/attempt/read | P10 |
| O11 | RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID | A3 broker P3 | 403 O | Token never egresses; no body/target disclosure; restart internal begin | P11 |
| O12 | RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED | A3 broker P3 | 409 O | Preserve identity; next public retry restarts internal begin, not public upload-init | P12 |
| O13 | SOURCE_RETENTION_NOT_AUTHORIZED | A3 P3 | 403 O | Fresh rejection: no R1/key/object/read; no export-permit fallback | P13 |
| O14 | RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE | A3 P3 | 503 O | No second source/read; recover exact historic key, never latest fallback | P14 |
| O15 | RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT | A3 P3 | 409 O | Preserve bound alias/ConflictTombstone; no source disclosure/second source | P15 |
| O16 | RAW_EXPORT_SOURCE_RESERVATION_BUSY | A3 P3 | 409 O | Current owner unchanged; no second attempt/read; retry after exact lease/CAS conditions | P16 |
| O17 | RAW_EXPORT_SOURCE_ALREADY_AVAILABLE | A3 P3 | 200 S | Durable original descriptor; no new body/key/object; release capacity | P17 |
| O18 | RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE | A3 P4/P5 | 503 O | Cleanup incomplete read/transport; not replay-stable terminal; next retry re-evaluates | P18 |
| O19 | CONTENT_COMMITMENT_MISMATCH | A3 P6 | 422 O | Clean complete body mismatch; exact terminal attempt/cleanup; no Available | P19 |
| O20 | RECAPTURE_REQUIRED | A3 P4–P6 or client-local | 409 O server only | Server exact-attempt terminal; local lost/expired buffer makes no call/DB mutation | P20 |
| O21 | RAW_EXPORT_SOURCE_RESUME_PENDING | A3 P6 | 202 O | Preserve complete ciphertext; no body resend/R2 restart; deterministic continuation | P21 |
| O22 | RAW_EXPORT_SOURCE_AVAILABLE | A3 P6/P7 | 200 S | Actual R5 published head; later replay O17; retain durable R6 cleanup | P22 |
| O23 | SOURCE_ENCRYPTION_FAILED | Internal P5 cause | NO EGRESS | Project recoverability to O18/O20 after exact cleanup | P23 |
| O24 | RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID | Readiness | NO EGRESS | Block activation, preserve existing residue | P24 |
| O25 | RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID | Readiness | NO EGRESS | Invalid capacity config blocks new work | P25 |
| O26 | RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID | Ingress readiness | NO EGRESS | Block admission; no silent transport downgrade | P26 |
| O27 | RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID | Agent readiness | NO EGRESS | Block local retained submission | P27 |
| O28 | RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID | Worker readiness | NO EGRESS | Preserve residue; block unqualified worker | P28 |
| O29 | RAW_EXPORT_AUTHORITY_INVALID | Assembly/B4 | NO EGRESS | Do not import export authority code into ingress | P29 |
| O30 | RAW_EXPORT_SOURCE_SELECTION_NONE | Selection/assembly | NO EGRESS | No source binding/provider read | P30 |
| O31 | RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS | Selection/assembly | NO EGRESS | No selection mutation/provider read | P31 |
| O32 | RAW_EXPORT_SOURCE_SELECTION_CONFLICT | Selection CAS | NO EGRESS | Frozen mapping unchanged | P32 |
| O33 | RAW_EXPORT_SOURCE_UNAVAILABLE | Later resolver/assembly | NO EGRESS | No assembly/provider read; no false Available | P33 |
| O34 | RAW_EXPORT_SOURCE_INTEGRITY_INVALID | Assembly verification | NO EGRESS | Owned corruption/quarantine evidence, not R27 mismatch | P34 |
| O35 | RAW_EXPORT_ASSEMBLY_PREPARE_FAILED | C2 preparation | NO EGRESS | Exact prepare abort, no assembly identity | P35 |
| O36 | RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH | Seal | NO EGRESS | No seal/assembly identity | P36 |

### 6.3 Coordinated implementation obligations

- Amend canonical A1 OM result enum/shape table, Application result, API mapper, Server tests and active CRT1 Agent sender together under future exact A3 authority. Do not shadow an unchanged OM with this document. The current OM remains preserved; this docs-only candidate is not a silent operative override.
- `A3_S02_All36RowsHaveExactlyOneOwner`: exact set/count 1+21+14, duplicate/omission RED.
- `A3_S02_All21BusinessResultsRoundTripThroughRuntimeHttpAndClient`: actual projection and parser at every status, including non-2xx. Do not copy legacy sender's throw-before-parse behavior.
- `A3_S02_PreNonceA1PrecedenceUnchanged`: real HTTP mixed-auth/metadata/crypto/dependency controls.
- `A3_S02_ResultShapeCrossProductFailsClosed`: remove required/add forbidden fields -> NOT_READY; corresponding valid cases green.
- `A3_S02_All14ExcludedCodesCannotEgress`: invalid result injection never exposes internal codes.
- Both P1/P5 excess, server/client-local recapture, clean mismatch/incomplete transport, continuation progress and replay descriptor readbacks need distinct controls.
- No global JSON canonicalization change: use exact existing CRT1 signed bytes. The result JSON is a closed response schema, not a new signing algorithm.

## 7. S03 — typed broker transaction and exact reused calls

### 7.1 Candidate private boundary and ownership

Proposed new endpoint: `POST /internal/capture-runtime/v1/source-ingress/admit`, `application/json`. One private metadata operation, not a new public admission/complete API. Host proposal: `src/TagEkyc.RawIngressBroker/TagEkyc.RawIngressBroker.csproj`, ASP.NET Core net8.0; public Infrastructure composition + Application/Contracts, never Infrastructure -> Api.

Closed request: ProtocolVersion=1 plus exactly these **22** current AdmissionContext fields:
```text
CaptureAgentId:uuid
DeviceInstallationId:uuid
CredentialId:uuid
CredentialGeneration:int64
RolePolicyId:uuid
RolePolicyRevision:int64
SignedAtUtc:timestamp
Nonce:bytes32
SignedEnvelopeFingerprint:bytes32
AgentConfigurationRevision:int64
VerificationSessionId:uuid
CaptureArtifactId:uuid
CaptureRevision:int32
RawClass:ChipDg2Portrait|LiveSelfieImage
IngressIdempotencyKey:uuid
MediaType:string
ClaimedPlaintextLength:int64
ClaimedPlaintextDigest:string
CapturedAtUtc:timestamp
PlaintextRetentionStartedAtUtc:timestamp
PlaintextRetentionExpiresAtUtc:timestamp
PlaintextRetentionBudgetSeconds:int64
```
No PrincipalId, ClientApplicationId, ProducerId, BindingId, acceptance ID, retention-authority ID, policy, subject, key bytes, claim token, storage selector, stream or raw bytes is accepted in this request. Runtime identities are authenticated-context observations; broker derives authority from durable relations. Private JSON is not a second CRT1 signature format.

Existing private HTTP bootstrap trust is not upgraded to mTLS or described as cryptographic authentication. This route must be restricted to the trusted API-to-broker boundary; checking DB runtime state alone does not prove who sent private HTTP. Exact network/service access qualification, finite request size/timeout and serializer bounds remain required in the final allowlist/runner. They are not silently defaulted here. Nonce existence does not attest metadata: current nonce rows have no signed-envelope fingerprint. Do not claim they do.

Login remains `tagekyc_raw_export_claim_broker_login`, NOSUPERUSER/NOCREATEDB/NOCREATEROLE/NOREPLICATION/NOBYPASSRLS; membership exactly `tagekyc_runtime` and `tagekyc_raw_export_claim_broker`. No authenticator/application/operator membership, no direct table DML. API never receives broker DB credentials or broker EXECUTE.

Broker Infrastructure façade owns one Npgsql connection/transaction **B**. N has already committed separately. No business transaction crosses HTTP as an ambient transaction. Broker response is sent only after B commit.

### 7.2 Runtime/binding read — proposed new callable, not existing append reuse

```sql
tagekyc.capture_runtime_read_bound_raw_ingress(
 p_capture_agent_id uuid, p_installation_id uuid,
 p_credential_id uuid, p_generation bigint,
 p_role_policy_id uuid, p_role_policy_revision bigint,
 p_session_id uuid, p_capture_artifact_id uuid,
 p_capture_revision integer, p_raw_class text,
 p_configuration_revision bigint, p_now timestamptz)
RETURNS TABLE(
 binding_id uuid, capability_id uuid, capability_revision bigint,
 principal_id uuid, client_application_id uuid, verification_session_id uuid,
 capture_acceptance_id uuid, capture_acceptance_revision bigint,
 session_challenge_hash text,
 source_retention_authority_id uuid, source_retention_authority_revision bigint,
 execution_expires_at_utc timestamptz)
```

Proposed owner deployer, SECURITY DEFINER, search_path=pg_catalog; EXECUTE only broker capability, PUBLIC/runtime/authenticator/application/operator revoked. Exact implementation and new retention FK are not landed. Require exactly one matching binding, never LIMIT 1/latest.

Order: runtime domain10 -> installation20 -> credential30 -> discovered session70 -> capability80 -> post-lock dual-key revalidation -> acceptance lock -> retention/evidence locks -> alias/exact LEAST/GREATEST. Use actual existing runtime lock derivations, not textual sorting. New retention/withdrawal lock derivation must be frozen with E01; this is an explicit remaining executability requirement.

Existing `capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz)` supports CaptureObservation/TrustedEvidence only. Do not pass RawIngress to it or masquerade as either supported role. New reader requires RawIngress, current registration/installation/generation/catalog, exact bound session, acceptance equality and immutable principal/authority lineage.

### 7.3 Begin — explicit retention reference, not export decision search

Candidate new retained-path overload below has **24 parameters** (the landed 22 plus exact retention ID/revision). Its name is a proposed successor overload; old/export callers do not receive permissive fallback.

```sql
tagekyc.raw_export_begin_retained_source_ingress_with_authority(
 p_authenticated_principal_id uuid,
 p_client_application_id uuid,
 p_producer_id text,
 p_capture_agent_instance_id text,
 p_ingress_idempotency_key text,
 p_verification_session_id uuid,
 p_capture_acceptance_id uuid,
 p_capture_artifact_id uuid,
 p_capture_revision integer,
 p_raw_class text,
 p_session_challenge_hash text,
 p_claimed_plaintext_length bigint,
 p_media_type text,
 p_captured_at_utc timestamptz,
 p_plaintext_retention_started_at_utc timestamptz,
 p_plaintext_retention_expires_at_utc timestamptz,
 p_plaintext_retention_budget_seconds integer,
 p_commitment_key_selector_id text,
 p_commitment_key_selector_version integer,
 p_claim_evaluation_owner_id uuid,
 p_claim_evaluation_token_ttl_seconds integer,
 p_idempotency_lock_timeout_milliseconds integer,
 p_source_retention_authority_id uuid,
 p_source_retention_authority_revision bigint)
RETURNS TABLE(
 outcome_code text, claim_evaluation_token text, token_variant text,
 token_expires_at_utc timestamptz, claim_evaluation_id uuid,
 claim_evaluation_revision bigint, claim_evaluation_fence bigint,
 retry_not_before_utc timestamptz)
```

Parameters1–2/6/7/11/23–24 derive from the locked reader; producer/instance are the exact bound runtime/installation canonical IDs, never identity guesses. Artifact/revision/class must equal signed metadata and acceptance. Idempotency/media/length/capture/buffer times derive from the frozen signed metadata. Validate Int64 budget into the existing bounded Int32 domain with checked conversion. Selector/version/evaluation owner/TTL/timeout are broker-owned qualified configuration, never Agent input.

This function selects the exact retained authority reference. It does not query a latest export decision or create an export Grant/permit. Existing exact replay is checked before new authority issuance or custody side effects; ordinary fresh denial has no new R1. Same scope's authority/withdrawal serialization is mandatory.

### 7.4 Complete/handoff — exact 42-argument existing callable

```sql
tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
 p_client_application_id uuid,
 p_producer_id text,
 p_capture_agent_instance_id text,
 p_ingress_idempotency_key text,
 p_claim_evaluation_id uuid,
 p_claim_evaluation_revision bigint,
 p_claim_evaluation_fence bigint,
 p_token_variant text,
 p_token_expires_at_utc timestamptz,
 p_claim_evaluation_token text,
 p_producer_envelope_fingerprint bytea,
 p_commitment_schema integer,
 p_commitment_key_id text,
 p_commitment_key_version integer,
 p_content_commitment bytea,
 p_subject_token_schema integer,
 p_subject_token_key_id text,
 p_subject_token_key_version integer,
 p_subject_token bytea,
 p_claimed_plaintext_length bigint,
 p_media_type text,
 p_captured_at_utc timestamptz,
 p_retention_started_at_utc timestamptz,
 p_retention_expires_at_utc timestamptz,
 p_retention_budget_seconds integer,
 p_storage_profile_id text,
 p_source_profile_id text,
 p_source_profile_version integer,
 p_encryption_suite_id text,
 p_encryption_framing_version integer,
 p_nonce_strategy_id text,
 p_nonce_seed_commitment bytea,
 p_chunk_size integer,
 p_framing_parameters_digest bytea,
 p_key_provider_id text,
 p_kek_id text,
 p_kek_version integer,
 p_kek_fingerprint text,
 p_max_continuation_seconds integer,
 p_attempt_deadline_seconds integer,
 p_safety_margin_milliseconds integer,
 p_ownership_lease_seconds integer)
RETURNS TABLE(
 "OutcomeCode" text, "SourceArtifactId" uuid,
 "AttemptKeyReservationId" uuid, "AttemptId" uuid,
 "ExpectedEncryptionAttemptRevision" bigint, "ExpectedFence" bigint)
```

Caller assignment ledger:
- 1–4: identical bound canonical identity/idempotency supplied to begin.
- 5–10: exact token result from begin; never public metadata.
- 11: existing broker-generated **producer envelope** fingerprint from qualified preflight. It is not automatically the different CRT1 SignedEnvelopeFingerprint.
- 12–15: existing versioned keyed content-commitment provider; exact historic version for replay, not latest fallback.
- 16–19: broker subject-token provider with exact bound subject/profile/version. Current FixtureSubjectTokenCatalog is not production evidence.
- 20–25: same validated immutable metadata tuple as begin.
- 26–42: server-owned validated source/storage/encryption/key/framing/continuation profile; derived output of existing qualified services, not copied from request or arbitrary defaults.

Same B performs runtime read -> transaction-local derived actor -> begin -> internal preflight/commitment/token/profile -> complete -> close reader -> commit. Existing comparison broker opens its own preflight/completion transactions; calling it unchanged inside B is not valid composition. Refactor pure/provider work into transaction-aware Infrastructure components without Infrastructure->Api or granting extra DB roles.

Fresh NewReservation: all five handoff fields non-null/nonempty and revisions/fence positive. Internal private reply discriminates `Handoff` versus `Final`: never both, never neither. Handoff additionally carries DB-derived `CustodyActorPrincipalId`, binding ID, RetentionAuthorityId/Revision for API-side stage/audit use. Agent never sees those fields. Final uses §6's closed business result shape. Exact private JSON union bounds need final literal freeze; SQL token/claim fields never egress to the Agent.

### 7.5 Finite predicate impact — a begin-only patch would still fail

| Surface | Current landed restriction | Required retention/export separation |
| --- | --- | --- |
| Snapshot schema/append, 20260731082733 :1143–1222 | ApprovedPurpose fixed to SubjectRawBiometricExport; export-policy/consent FK | Separate durable retention snapshot/authority kind and correct evidence FK; do not relabel export rows |
| Retained begin/core, 20260906055502 :140–235 | Export decision/permit/consent selected | Exact retention reference and precompletion evidence instead |
| Comparison broker preflight | Export consent, independent transactions, fixture subject selector | Retention-specific same-B preflight; qualified provider ownership |
| Complete, 20260731130919 :833–864 | Export-purpose snapshot + export consent | Dispatch by persisted authority kind with exact retention resolver, not permissive OR or bypass |
| R3 stage, 20260810120000 :380–405 | Export-purpose/consent recheck | Retention checkpoint at existing position; same equality/horizon/failure residue |
| R4 commit, 20260812120000 :365–383 | Same | Same; do not move CAS/commit |
| R5 publish, 20260812120000 :488–505 | Same | Same; Available meaning unchanged |
| C1 source binding/assembly, 20260815120000 :479,518,729,734 | Source ConsentPolicyId participates in export consent | Retention evidence never masquerades as export consent; exact export job/decision authority still required |
| C6A C3 delivery barrier, 20260904154848 | Completed + exact export decision/permit/job/principal + source/export consent | Preserve all export gates; retention-only source must fail package/read/delivery controls |

Only future forward migrations may implement these branches. No historical migration edits; no existing export tables/consent functions retasked. A read adaptation needed for new retention-kind sources must preserve the independently authorized export job tuple and current export consent; it is not early export. Exact dual-authority schema/FK/Down/function ACL remains to be frozen, including C1/C3 compatibility before claiming end-to-end readiness.

S03 has literal transport fields, function call shapes and transition ownership now, but **is not behaviorally closed** until E01/evidence schema, retention snapshot/checkpoint resolver and the private union/config/access bounds are executable. The historical umbrella's non-operative §10.6 is not authority for a begin-only wrapper.

Explicit remaining stage projection: R3/R4/R5 internal `SourceRetentionNotAuthorized` after R1 is not automatically public O13 `SOURCE_RETENTION_NOT_AUTHORIZED`, whose frozen phase is P3 before new R1/body. Preserve each stage's durable failure/recovery residue and freeze its public projection before implementation; no mechanical uppercasing, invented outcome or implicit generic NOT_READY. The 36-row public census does not itself close all internal-stage mappings.

## 8. Delivery batches, mutation fence and remaining closure

One A3, not separate TIPs. Work order:
1. A3.0: close E01 concrete consent evidence intake/verification/withdrawal; freeze retention DDL/function/lock/ACL and immutable principal/authority fingerprint; finalize S02/S03 canonical amendments and exact allowlist.
2. A3.1: private broker + API adapter + B/R1 committed handoff, metadata-only replay.
3. A3.2: actual acceptance producer, Agent retained-buffer/CRT1 sender, applicable completion selection.
4. A3.3: internal R2–R6 façade and durable continuation/checkpoint branches.
5. A3.4: synthetic cross-repo proof, readiness and as-built review. No production activation.

Expected future modifications include Application capability request/service and runtime ports; Infrastructure capability/binding persistence/entities and forward migration; retained authority/evidence/snapshot implementation; existing custody checkpoints and compatibility reads; public R27 result mapping; broker host; Agent active sender/retained leases; acceptance/completion consumers; focused tests and explicit opt-in runner. These are **families, not an executable mutation allowlist**. New project/package/config defaults are not authorized by their mention here.

Keep no Client API key fallback; A2 PendingBind/recovery/HandoffAccepted/DelegationAttempted semantics; original plaintext deadline; configuration override/ETag behavior; exact accepted artifact/revision; no liveness raw; no provider plaintext in broker; bounded pre-admission memory. SourceClass mapping must explicitly reconcile Agent RawChipDg2Portrait/RawLiveSelfieImage with custody ChipDg2Portrait/LiveSelfieImage.

SourceRetention actor is not a new Agent identity or a new Recipient. Post-R1 authority loss uses the existing checkpoint/reconciliation outcome and never discards durable state merely to satisfy HTTP cancellation.

## 9. PI-TAG-001 review/evidence boundary

PI-TAG-001 PILOT: TIP-88C1; High-risk; SQL/transaction, API, crypto/provider reuse, raw custody, recovery and governance modules. Invariant/shape/outcome/ordering/Test-Bite matrices apply. No independent queue protocol is introduced.
Independent full-source-boundary review required. Census lanes do not count as review. From round3 record accumulated non-convergence causes; round5 root-cause checkpoint and round10 hard stop remain enabled. Stop at a real missing authority/evidence dependency, not after a fixed quota of findings.

Required future execution evidence:
- Real fresh noncompleted session traverses evidence issuance, retention permit, capability/binding, both raw classes, actual R1–R6 and Client completion; zero preseed-export decisions.
- Separate observer sees committed N and R1 before first body read. Actual Expect client with infinite fallback; mutations auto-Continue/proxybuffer/earlytimer RED.
- Every retention tuple/lineage substitution, revoked/expired authority before R1, Replace principal change and export-only permit at ingress fails closed.
- Retention-only source cannot export/package/decrypt/deliver through otherwise-valid real boundaries. Completed plus actual independent export authorization is the positive control.
- Real R3/R4/R5 checkpoint failures preserve current SourceRetentionNotAuthorized/recovery semantics. Do not prove by constant denial or unrelated auth failure.
- All36 ownership and 21 business roundtrips/shape cross-product; no generic non2xx parser bypass.
- Exact snapshot-based two-repo closure and explicit acceptance runner when landing is separately authorized. No dependency silently absent, no new skips, no CRLF/hash bypass.

No tests/build/migrations/provider execution occurred in this docs-only turn. Existing A1/A2 PASS counts are regression baselines, not A3 proof. Review findings/results are recorded separately, not embedded as duplicate normative runtime rules.

## 10. Exact evidence inventory and provenance

The following v0.2 evidence remains unchanged and was rehashed for v0.3; values describe raw working representation, not assumed Git LF exports. This is a census inventory, **not complete future implementation allowlist**. Predecessor versions and unrelated work remain untouched.

| Server-relative path | Raw SHA-256 | State |
| --- | --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationRepository.cs` | `21B79DA50F586444061970C074157800488F1DCD0FEB4EE5DA5D4D7084DC2DC6` | TRACKED_CLEAN |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeStartup.cs` | `8AA6A6AEA6F5517FE67B38D29D48F2596C1AC81630FF5F9E69812EE545186CDE` | TRACKED_CLEAN |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_broker_composition_dispatch.md` | `C4AADFFB1689685294D39CE44A7897582492D761E65D1C010A1912A3C748EF5F` | UNTRACKED_SOURCE — preserve |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md` | `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018` | TRACKED_CLEAN |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md` | `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062` | TRACKED_CLEAN |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_technical_implementation_dispatch.md` | `248A9D3AA966FF643245CB67C46455D82B13BE59577E621355324FE67F7292F6` | UNTRACKED_SOURCE — preserve |
| `src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` | `16C609A9506998C979997DA32B4B24476220A466ED51E8BF45492B7399ED7CED` | TRACKED_CLEAN |
| `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | `C37599AB9D4EE15F666FDBFBF1CCC082342F8D139D14FB37B9BCEA027D548570` | TRACKED_CLEAN |
| `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | `F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA` | TRACKED_CLEAN |
| `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs` | `C996F31D4ADBBE7F33225879807741622CB67DDDD8305E5498C8C6CCC8357195` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonBroker.cs` | `690A9DA49BED1580AB4A3455640A375A85F8A78C3280DAB9FBEDB3239FF8CF89` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/RawExport/RawExportCaptureAcceptanceResolver.cs` | `A894E73D3108C5057FB9A3638AADDF9D9EBD365507D68F0805452C7B00FF6380` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260906055502_Tip88C1C6BIngressSqlComposition.cs` | `5AC54FCB6F6F5AA7C5799F239E48B535A626E81706CAE4DECD345984DE942126` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.cs` | `BC1EDE3F7F45145F86E117791AA41C00F78450D9CFA0CAB4FCD53F0554D89635` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/RawExport/RawExportR2Contracts.cs` | `E3D7EF8BBC55F381D7465530422556C4E9F21BC2294A272EB0F0123043592E4C` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationContracts.cs` | `F723CC51D9FECB2E0CA31891DCCE2028715195F2DDF7FA719240252B78F665B6` | TRACKED_CLEAN |
| `src/TagEkyc.Api/RawExportSourceIngressServiceRegistration.cs` | `2C24F2148A65B5465CCB5DE3AF2B1B5ECDA78851AA1458F17E12495BCA29C9AD` | TRACKED_CLEAN |
| `src/TagEkyc.Api/Program.cs` | `97A41D2C2EE3025C497C203E44ED8B2BD5758CD9EA962D600F65BA412C80C499` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/RawExport/RawExportIngressCapacity.cs` | `394B2829A5B86D16BCA55629595A14D802556609E4D229F13335F3D20F2DF806` | TRACKED_CLEAN |
| `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | `4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66` | TRACKED_CLEAN |


Additional evidence:
| Server-relative path | Raw SHA-256 | State |
| --- | --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260724015546_Tip88B1E3ResolverReadBoundary.cs` | `D4A24EC2AD5F42ED457899C44DD635AEE4115DD4EB9C3BCD0F7DAD6D3F2A24ED` | TRACKED_CLEAN |
| `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` | `1FA7E3EAA7BE25673340731B7E9FD400D667C1AD27FC113BDD8685F64F38E2E0` | TRACKED_CLEAN |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs` | `C512FB6DDE8D834E5D67CF68BC28519AB64C66B51A1A59F2FFBB430932D1A274` | TRACKED_CLEAN |

## 11. Current disposition

- S01 direction1: RATIFIED; no generic service-principal alternative.
- S05 precompletion retention vs postcompletion export: RATIFIED; no Completed gate relaxation.
- RET-01–RET-12: materialized and mapped to discriminating proofs, not executed.
- S02: 36-row literal candidate projection; final canonical OM amendment still required before implementation.
- S03: literal call/field/owner/predicate inventory; E01-dependent durable/ACL/union bounds unfinished.
- E01: precompletion evidence producer is not present in inspected code; concrete external source or new reviewed intake must be identified.
- Overall: **NOT READY FOR IMPLEMENTATION AUTHORITY**. Do not label this a final PASS executable dispatch merely because semantics are ratified.
- Product/schema/test/config/project mutation: zero. Stage/commit/push: none. A4/production: closed.

No separate TIP/RRI is opened. Continue this v0.3 lineage once the evidence input is identified; finish exact DDL/ACL/allowlist/canonical projections and obtain CC/GPT review on exact bytes before requesting implementation authority.

## Changelog

- v0.3: materialize Homeowner S01/S05 ratification; reject generic principal and early export; RET-01–12; confirm latest B2/E3 remains Completed-gated; honest precompletion evidence census; literal 36-outcome candidate and broker function signatures; expose complete/R3/R4/R5 and C1/C3 authority-kind impacts rather than claiming begin-only closure.
- v0.2 and v0.1: preserved unchanged as prior reconciliation candidates.
