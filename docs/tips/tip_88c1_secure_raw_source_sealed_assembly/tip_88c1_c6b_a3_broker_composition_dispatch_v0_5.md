# TIP-88C1-C6B-A3 — Source retention authority and ingress composition

**Version:** v0.5 — executable contract catalogue candidate
**Date:** 2026-09-13
**Status:** PROPOSED EXECUTABLE CATALOGUE FOR EXTERNAL REVIEW — NO IMPLEMENTATION AUTHORITY
**Authority this turn:** documentation/census/review only. No product/schema/test/config/project mutation; no stage/commit/push; no A4/production.
**Predecessor:** `tip_88c1_c6b_a3_broker_composition_dispatch_v0_4.md`, SHA `17C7E47D891454894172DC807CD97D744B48FEE77EEE28A8227DF80CAE27C611`, preserved byte-for-byte. Its business decisions remain; this successor materializes technical contracts, not a second consent decision.
**Server HEAD:** `5df5f60a6dc4d992c71fc2b160673e4abca6488d`
**Agent HEAD:** `e3bd625bbe357b1f3c9620d20bcba121cfb61974`

## 1. Homeowner decision record — operative semantics

Source: Homeowner attachment `7e310eea-adf2-457d-b68c-e3957ebf9319/pasted-text.txt`, raw SHA-256 `5FD38341CC41D188301E5259C436B483CFED53952939073F4F4177388DACDA3D` (8,289 bytes / 265 lines), supplied 2026-09-13. This section records the decision in the repository; it is not Builder self-authorization.

**RATIFIED: SourceRetention authority before Completed is distinct from RawExport authorization after Completed.**
**RATIFIED: S01 direction 1, authenticated BusinessConsumer principal lineage; generic custody/service principal direction rejected.**
**S01/S05 semantic disposition: CLOSED BY HOMEOWNER.** Missing executable evidence/DDL is not permission to reopen these choices or call them undecided.

**Homeowner clarification, 2026-09-13:** “trong consent đồng ý sử dụng xử lý dữ liệu cá nhân đã đủ hết rồi đừng vẽ thêm nữa”, followed by “Tiếp tục đi”. For this approved design, reuse that existing personal-data processing consent. Do not request a separate DG2/selfie consent, new checkbox/screen, new subject signature, repeated confirmation or new consent-collection workflow. This is a product-scope decision, not a new legal sufficiency assessment by Builder. The same underlying consent evidence may support retention and later export checks; the two authority/permit classes remain distinct.

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
The v0.2 finding was not “upload infrastructure missing”: it was an absent authority lineage and a Completed/permit/Available cycle. Both are resolved semantically above; §3 now treats the existing consent as the approved business input and its pre-completion reference as technical integration work, not a new Homeowner consent decision.
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
| Same-owner transient R2 re-entry | ACTUAL_MISSING | Beta ExistingMatch returns the old reservation; the C6B wrapper projects a terminated attempt without a semantic terminal code as busy. The broker companion must specify the new bounded internal successor-attempt transition, not describe it as landed behavior |
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


## 3. Executable catalogue ownership

This parent owns intent, the 36-outcome census, delivery order and final authority status. The companions own the technical master rows below; a parent summary cannot weaken a companion predicate or introduce another copy of a signature.

| Companion | Sole normative surface within this candidate | SHA binding |
| --- | --- | --- |
| `tip_88c1_c6b_a3_e01_contract_v0_5.md` | Existing-consent authenticated reference projection, update/withdrawal, retention permit schema/operations/ACL | `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA` |
| `tip_88c1_c6b_a3_retention_checkpoint_contract_v0_5.md` | Capability/binding/snapshot lineage, complete/R3/R4/R5 dispatch, C1/C3 export compatibility | `601DA50B691C026FB4E285E96CFBCA08B22A3D53E36E189CB604D7B6FBF6D491` |
| `tip_88c1_c6b_a3_broker_pipeline_contract_v0_5.md` | Private HTTP shape/config, one B transaction, 12/24/42 calls, staged provider composition | `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48` |
| `tip_88c1_c6b_a3_agent_result_contract_v0_5.md` | Canonical A1 amendment, public result projection, acceptance/completion callers, Agent streaming/receipt | `1749DEE69B9DD177BF99BAC64FB29C8BF7E95282EFCC86B5CAEF44301B79189D` |
| `tip_88c1_c6b_a3_mutation_inventory_v0_5.md` | Exact proposed future files, raw-source SHA and ownership/proof join | `94E27FAC32BB55EFD6E3195F25064AC27F350483D20805BDAD081D9CF0D4B910` |

Every item is a proposed implementation contract awaiting final review and Homeowner grant. No companion independently ratifies itself. Current docs-only edits cannot silently broaden an already-granted product allowlist.

### 3.1 E01 is a reference integration, not a second consent

The repository implements B2 as authorized-recorder attestation with a current authority registry, owned session and provenance. It does not contain an external consent-database API. E01 uses that existing trust pattern to record the existing consent reference before Completed; it does not pretend to fetch a nonexistent external source or accept any nonempty reference from any Client.

The companion must make update/withdrawal visible through one durable current reference revision and shared/exclusive locking at each retention checkpoint. A local projection is technical provenance of the same original agreement. B2 Granted/Withdrawn projections after Completed remain valid and are not prohibited as “second consent database events”. No new subject interaction is introduced.

### 3.2 Intent closure and rejected branches

| Intent | Implementation owner | Closure evidence required |
| --- | --- | --- |
| Bind actual existing consent, not a made-up session reference | E01 authenticated recorder boundary + resolver | Authorized assertion writes real source projection; wrong actor/ref/session/version and withdrawn state independently RED |
| P/Client/session unchanged from capability to custody | Retention companion + broker B | Separate observer reads exact equality at capability/binding/snapshot/attempt audit |
| Retain before Completed, export only after its own authorization | Retention companion + unchanged export repository | Positive full retained path without export decision; early export negative |
| Read raw only after committed R1 | Broker + Agent/result companion | Observer sees commit before first application read; Expect mutations RED |
| No hidden dependency/fake provider | Broker readiness + file inventory | Real named synthetic implementations and role scopes; production remains closed |

Rejected: separate DG2/selfie consent, early B2 export append, generic service principal, Agent-selected principal, latest export decision as retention, raw through broker, public init/complete/resumable route, automatic body retry, direct production activation.

## 4. Ordered execution and race ownership

```text
existing consent authenticated reference projection
 -> exact SourceRetention permit + BusinessConsumer P/Client/session
 -> capability R20a / same-lineage Replace R20b
 -> runtime binding R21 (R22 recovery unchanged)
 -> evidence R24/R25 + exact acceptance in the same B
 -> retained Agent lease (origin was first capture ownership)
 -> R27 CRT1; N commit; same unread Stream to A3
 -> config/capacity; private broker B revalidates binding/acceptance/retention
 -> begin -> pure/keyed metadata work -> complete/handoff -> B/R1 commit
 -> first body consumption -> R2 -> verified completion -> R3 -> R4
 -> R5 Available -> durable R6 cleanup/reconciliation
 -> Client completion source selection in finalization transaction
 -> Completed -> existing independent export authorization/C1/C3
```

Consent update/withdrawal races are serialized with fresh R1 and R3/R4/R5. Each transition owns its original CAS, durable residue and replay behavior. Post-R1 retention denial is not mechanically uppercased to the pre-R1 P3 code O13, nor a delete instruction. Exact projection is owned by the checkpoint companion.

A2 PendingBind, HandoffAccepted, DelegationAttempted and R22-first recovery remain unchanged. The new raw sender does not reacquire device execution authority. No Site-A/Site-B ownership model is introduced.


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

For every negative, first establish a matching positive fixture, mutate exactly the guard, observe intended RED, restore and rerun. RET-04 is not satisfied by calling an unmapped route or failing unrelated authentication. RET-12 must run through the implemented existing-consent reference boundary, not a synthetic pre-authorized retention/export decision. It does not require another subject consent action.
## 6. S02 — literal one-call outcome projection (candidate A3 amendment)

This section is the **exact candidate transport projection**, not a claim that A1 already implements it. The frozen spine has 36 rows; only 22 belong to the one-call producer surface. ACCESS_DENIED belongs to A1, leaving **21 A3 business results + 14 excluded outcomes + 1 A1 outcome = 36**.

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

`Pnn` denotes proposed method `A3_S02_O<nn>_ExactOutcomeShapeStatusAndResidue`; none has run. NO EGRESS means not an A3 business-enum member. Additional HTTP statuses below are pinned candidate choices; existing six business statuses remain unchanged.

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

## 7. Canonical amendment and cross-ledger closure

S02 parent §6 is the sole public outcome set; Agent/result companion pins the enum, parser, status and O/E/S shapes. Under a later implementation grant, edit the canonical A1 Operation Master and source in lockstep: do not leave the old six-result contract active and merely describe it as superseded elsewhere. The same amendment updates its R24/R25 response and completion transaction joins. CRT1 canonical bytes, N and A2 control JSON stay unchanged.

The broker companion owns private metadata and handoff shapes. Private Handoff is not a public success. Public Available/AlreadyAvailable require actual persisted publication; ResumePending contains no source/key/claim fields. R2/R3/R4/R5 functions remain internal to Infrastructure; API consumes a public Application port/facade, never internal provider types.

A file/route/function/secret/test that does not join its owner in the companion and inventory is an executable-dispatch defect. No unresolved required row may be passed because another section says “reuse existing”. New means proposed, not landed.

## 8. Delivery batches and mutation fence

One A3, with sequential implementation batches after a future grant:

| Batch | Concrete output | Exit proof |
| --- | --- | --- |
| A3.0 | Exact reference/retention/lineage forward schema, canonical A1 amendments and fresh checkpoint resolver | Apply/Down/reapply; denied/withdrawn/expiry and role negative matrix |
| A3.1 | Metadata-only broker, single B/R1 transaction and API adapter | N/R1 observer ordering; commit-loss replay; no raw in broker |
| A3.2 | R2–R6 facade, role-isolated object/key scopes, durable continuation and export-compatible reads | Stage disposition/terminal-intent/cleanup and checkpoint race proofs |
| A3.3 | R25 acceptance, retained Agent sender/lease/receipt and Client completion join | Two real class uploads through A3.2 + no pre-R1 read; non-retained and independent export regression |
| A3.4 | Synthetic two-repo execution and final evidence freeze | Both snapshot builds, Agent/Server suites, architecture, PostgreSQL, transport qualification |

The exact proposed file set is in the mutation inventory; family names alone do not authorize edits. Migration effects are forward-only; historical migrations remain untouched. Missing/dirty source baseline changes require a precise rebind, not silent normalization. Product build/test defects inside a granted file set are FIX + RETEST; a real semantic/module/provider expansion is not silently self-authorized.

No A4, production activation, real retained biometrics, production secrets/LOGIN provisioning, stage, commit or push is granted by this draft. Synthetic fixtures do not certify an HTTP proxy, physical NIC/kernel buffer or production key provider.


## 9. PI-TAG-001 review/evidence boundary

PI-TAG-001 PILOT: TIP-88C1; High-risk; SQL/transaction, API, crypto/provider reuse, raw custody, recovery and governance modules. Invariant/shape/outcome/ordering/Test-Bite matrices apply. No independent queue protocol is introduced.
Independent full-source-boundary review required. Census lanes do not count as review. From round3 record accumulated non-convergence causes; round5 root-cause checkpoint and round10 hard stop remain enabled. Stop at a real missing authority/evidence dependency, not after a fixed quota of findings.

Required future execution evidence:
- Real fresh noncompleted session traverses existing-consent reference resolution, retention permit, capability/binding, both raw classes, actual R1–R6 and Client completion; zero preseed-export decisions.
- Separate observer sees committed N and R1 before first body read. Actual Expect client with infinite fallback; mutations auto-Continue/proxybuffer/earlytimer RED.
- Every retention tuple/lineage substitution, revoked/expired authority before R1, Replace principal change and export-only permit at ingress fails closed.
- Retention-only source cannot export/package/decrypt/deliver through otherwise-valid real boundaries. Completed plus actual independent export authorization is the positive control.
- Real R3/R4/R5 checkpoint failures preserve current SourceRetentionNotAuthorized/recovery semantics. Do not prove by constant denial or unrelated auth failure.
- All36 ownership and 21 business roundtrips/shape cross-product; no generic non2xx parser bypass.
- Exact snapshot-based two-repo closure and explicit acceptance runner when landing is separately authorized. No dependency silently absent, no new skips, no CRLF/hash bypass.

No tests/build/migrations/provider execution occurred in this docs-only turn. Existing A1/A2 PASS counts are regression baselines, not A3 proof. Review findings/results are recorded separately, not embedded as duplicate normative runtime rules.

## 10. Exact evidence inventory and provenance

The following inherited evidence is checked against live bytes for v0.5; values describe raw working representation, not assumed Git LF exports. This is a census inventory, **not complete future implementation allowlist**. Predecessor versions and unrelated work remain untouched.

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
| `src/TagEkyc.Domain/RawExportSubjectConsent.cs` | `AA8B44162FC287EEAF668819A81E13773DD5D046F772EB169FBDCD57B73CCEA7` | TRACKED_CLEAN |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs` | `C52CCB3DB8E523817271F42ACFE53DD94300D0C33FC61246B7932B460D1A1CC0` | TRACKED_CLEAN |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260724015546_Tip88B1E3ResolverReadBoundary.cs` | `D4A24EC2AD5F42ED457899C44DD635AEE4115DD4EB9C3BCD0F7DAD6D3F2A24ED` | TRACKED_CLEAN |
| `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` | `1FA7E3EAA7BE25673340731B7E9FD400D667C1AD27FC113BDD8685F64F38E2E0` | TRACKED_CLEAN |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs` | `C512FB6DDE8D834E5D67CF68BC28519AB64C66B51A1A59F2FFBB430932D1A274` | TRACKED_CLEAN |

## 11. Current disposition

S01/S05 business semantics remain CLOSED. This v0.5 candidate replaces the remaining prose-only seams with technical master companions and an explicit file inventory. Internal review must assess the complete catalogue before a READY claim. A correction-only PASS on v0.4 is not a full executable-dispatch PASS on this larger delta.

Implementation authority remains NOT GRANTED. Required tests are proof specifications, not executed A3 evidence. The separate review ledger records actual review coverage/findings; it does not duplicate normative runtime rules.

Preserve every predecessor, including v0.4 exact bytes. No new consent question or separate TIP/RRI is opened by this draft.


## Changelog

- v0.5: materialize executable reference/retention/checkpoint/broker/Agent contracts and exact proposed mutation inventory; keep one consent and no implementation authority. Internal review evidence and remaining execution obligations are recorded in the separate review ledger.

- v0.4: record Homeowner existing-consent clarification; replace E01 new-consent/input-question framing with bounded reference integration; preserve separate permits, runtime validation, withdrawal and export Completed gate. No new consent UI/signature/workflow, no product change.
- v0.3: materialize Homeowner S01/S05 ratification; reject generic principal and early export; RET-01–12; confirm latest B2/E3 remains Completed-gated; honest precompletion evidence census; literal 36-outcome candidate and broker function signatures; expose complete/R3/R4/R5 and C1/C3 authority-kind impacts rather than claiming begin-only closure.
- v0.2 and v0.1: preserved unchanged as prior reconciliation candidates.
