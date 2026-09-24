# TIP-88C1-C6B-A3 — Broker and retained-ingress composition dispatch

**Version:** v0.2 — builder reconciliation candidate
**Date:** 2026-09-13
**Status:** HOLD — source-authority lineage and issuance lifecycle unresolved; not an executable implementation dispatch
**Implementation / stage / commit / push / A4 / production authority:** NOT GRANTED by this document
**Current work:** documentation and repository census only
**Predecessor:** `tip_88c1_c6b_a3_broker_composition_dispatch.md`, v0.1, preserved byte-exact
**Server HEAD:** `5df5f60a6dc4d992c71fc2b160673e4abca6488d`
**Agent HEAD:** `e3bd625bbe357b1f3c9620d20bcba121cfb61974`
Both HEADs were pushed and independently matched by `git ls-remote` on 2026-09-13.

## 1. Intent and actual delivery boundary

A1 supplies authenticated runtime identity, nonce transaction N, execution binding and the public R27 HTTP boundary.
A2 supplies the Windows Agent lifecycle, installation key custody, signing, configuration and at-most-once device delegation.
A3 must connect actual accepted DG2/selfie bytes to committed retained custody through the already-landed R1/R2/R3/R4/R5/R6 machinery.

It must NOT build another upload protocol, key algorithm, encryption frame, source state machine, package-download/Recipient service, consent engine or signing/TSP workflow.
Synthetic/non-patient tests only. Purge/legal-hold and production activation remain separate lifecycle/A4 gates. Private broker HTTP remains the bootstrap choice; private HTTPS/mTLS remains deferred, not silently introduced.

The predecessor's phrase “existing body pipeline” was inaccurate: callable stages exist, but there is no product implementation of the body-pipeline port.
The predecessor's “only private broker” scope was insufficient for its own end-to-end completion claim: acceptance production, Agent raw transport and runtime admission are also missing.

### Intent ledger

| Item | Disposition |
| --- | --- |
| Outcome intended | One authenticated operation, exact accepted artifact, committed R1 before plaintext consumption, durable convergence to Available or an existing named non-success result |
| Reuse | Landed A1/A2, source claim/authority SQL, R2 encryption/verifier, R3 staging, R4 commit, R5 publication, R6 reconciliation/cleanup |
| Preserve | Bound-session identity; CRT1/N once; 36-outcome semantics; exact attempt/revision/fence; no raw on broker metadata route |
| Reject | Fake AuthenticatedClientContext; assigning CredentialId to PrincipalId; key-prefix mining; API-key fallback; synthetic provider called production-ready |
| Carry-forward | Production provider qualification, multi-instance quota enforcement, mTLS, purge/legal-hold, actual deployment |
| Non-claim | This draft is not authorization to implement. A1/A2 PASS is not A3 end-to-end proof |

## 2. Repository-real census

“Missing” below means no implementation/caller found under product `src`, not absence of a test double or document name.
Existing files were read, and inspected product source matches HEAD. Historical/untracked docs are separately labelled in §9.

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

## 3. Closed order — correct the predecessor, do not add checkpoints

Required custody composition order, conditional on resolving S01 and S05. This graph is not yet an executable fresh-session flow: obtaining the prerequisite authority is blocked by S05.

```text
Agent: accepted evidence + bounded retained lease + frozen configuration
  -> CRT1 signed ingress metadata + Expect:100-continue
A1: parse/authenticate -> commit N -> pass same unread Stream exactly once
A3: configuration/limits/capacity, then broker metadata call
Broker B: runtime/installation/generation/bound session/capability recheck
  -> exact acceptance equality
  -> retained authority+begin
  -> keyed commitment / subject token / custody profile
  -> complete_raw_export_source_ingress_claim_with_r2_handoff
  -> commit B / R1
API-side A3: only now first plaintext read
  -> R2 writer -> durable R2 completion verifier
  -> R3 staging -> R4 commit -> R5 Available
  -> R6 durable cleanup / reconciliation
  -> existing exact external result; dispose transient resources
```

A1 receiving a Stream reference is not a body read. No Stream, raw bytes, DEK, object locator or provider plaintext buffer may cross the metadata-only broker HTTP route.
Complete/handoff belongs BEFORE the first body read, not after R2. Begin alone does not return the five R2 handoff values.
Only NewReservation with an exact current head/attempt/fence admits a body. ExistingMatch is metadata-only; AlreadyAvailable, ResumePending, busy and terminal replay never create another key or reader.
No new post-R1 runtime authentication/checkpoint is introduced. Existing authority checks in complete, R3, R4 and R5 remain load-bearing.
R2 PendingVerification is not VerifiedCompleted and is not Available. Cleanup remains durable even if HTTP response/cancellation is lost.

This order is a correction back to C6B §5.3/§5.4, not a new state-machine decision.

## 4. Actual seams and proposed dispositions

### A3-S01 — source-authority principal (UNRESOLVED SEMANTIC DECISION)

Retained begin requires `p_authenticated_principal_id`.
At migration `20260906055502:147`, it selects an Authorized decision by exact PrincipalId, ClientApplicationId, session, class and unexpired permit.
`EfRawExportAuthorizationRepository.cs:571` persists that principal from the independently authenticated authorization actor.
R2 writer/verifier and R3/R4/R5/R6 also require ActorPrincipalId.

A1 runtime context, registration, installation, capability, binding and VerificationSession do not store that authorization principal or a link to the source decision/permit.
The platform operator's PrincipalId is a different authority; it is not the runtime's source principal.
No caller-provided principal, Guid.Empty, random Guid, CredentialId, CaptureAgentId, ClientApplicationId, first matching decision, or historical audit KeyPrefix may fill the hole.

Two possible identity-lineage directions need explicit authority; neither is selected by this candidate and neither alone resolves authority issuance in S05:

1. Persist the already-authenticated Client authorization principal when issuing the capture capability and preserve its exact lineage into binding/custody. This extends A1's durable capability contract; it does not send the Client API key to Agent. An existing authorized decision/permit must still exist for that exact principal; carrying identity alone does not create a Grant.
2. Resolve an explicitly provisioned server-owned custody principal/profile for the exact bound client/session/policy. This requires a reviewed binding/selection contract and cannot be a hidden default or generic service credential standing in for consent.

Before implementation, pin the selected source, caller that obtains the decision/permit, persistence/rotation/revocation behavior, exact unique selection and downstream audit identity. Consent machinery and retained-policy semantics are NOT reopened.
Principal lineage and the S05 issuance lifecycle must close together; identity persistence alone is not an implementable authority flow. Other seams below are proposed integration corrections subject to review/ratification.

### A3-S02 — six-value A1 result port versus 36-outcome custody contract

Current `CaptureRuntimeRawIngressOutcome` has six members. R27 converts any other enum/result shape/exception to 503 NOT_READY.
Available/AlreadyAvailable currently require source ID but forbid state/disposition fields; the older C6B result shape requires those fields.
Thus “all 36 outcomes unchanged” cannot be implemented by merely registering an A3 service.

Proposed bounded correction for the future A3 dispatch: extend the typed Application result and API projection to enumerate the existing reachable custody outcomes with exact shape/status/retry/residue rules from the frozen table. Preserve generic NOT_READY only for invalid/internal results.
No arbitrary string bag, no 37th outcome, no mapping ResumePending, terminal recapture or retention denial to a generic 503.
The final literal outcome ledger must partition all 36 rows into route-reachable cases and separately named earlier/config/later assembly surfaces; it must not imply every assembly outcome occurs on R27.
A1 operation-master canonical §A1-to-A3 and the result-shape tests must be amended together, not merely declared superseded in an A3 footnote.
Until that finite ledger is materialized and reviewed, the result seam remains an implementation gate.

### A3-S03 — runtime B transaction and typed broker wrapper

The tracked runtime umbrella's historical, explicitly non-operative §10.6 proposes a wrapper copying begin's inputs/returns, yet also tells it to perform complete/handoff. This is an inherited design inconsistency to avoid, not current authority requiring that obsolete signature.
Begin returns a claim token tuple; completion requires keyed commitment, subject token and custody profile inputs that the begin signature does not carry, and returns a different handoff tuple.
The existing in-process broker computes those materials and opens its own transaction; calling it unchanged inside an outer transaction is not established reuse.

Proposed correction: an Infrastructure-owned broker transaction façade holds one B connection/transaction, performs the typed bound-runtime recheck and begin, obtains server-owned commitment/profile inputs, then calls the existing complete/handoff function on that transaction. It returns only after commit.
The exact private DTO, SQL forward wrapper signature/return and caller→parameter→grantee ledger must be generated from this flow, not copied from the contradictory begin-only promise.
Existing append validator is NOT a RawIngress validator: it accepts only CaptureObservation/TrustedEvidence and requires a known BindingId. Do not pass a forged role or silently widen it.
No new arbitrary capability role, no reuse of API default DbContext as broker identity, and no migration rewrite.
After S01 and S05 close, specify exact binding discovery/recheck, actor ownership and bounded locks before freezing the forward wrapper.

### A3-S04 — acceptance, Agent transport, stage façade and capacity (IMPLEMENTATION WORK, NOT ALREADY BUILT)

Proposed A3 scope includes:
- create acceptance via the landed wrapper after durable qualifying Passed evidence; replay the exact acceptance;
- add the existing selection wrapper to applicable Client session completion atomically, keeping non-retained completion unchanged;
- consume acceptance in Agent and submit the matching retained lease using active CRT1 client, never the API-key sender;
- reconcile the current Agent acceptance validator's RawChipDg2Portrait/RawLiveSelfieImage spelling with SQL/ingress ChipDg2Portrait/LiveSelfieImage using a closed explicit mapping and discriminating test;
- preserve server-owned configuration/ETag semantics, original monotonic lease deadline, cancellation, zeroization and same ingress idempotency on retry;
- Infrastructure façade composes internal stage types without exposing internals to a new host or adding Infrastructure→Api;
- pin required actor and later reservation/object/publication revision readbacks to durable stage results, never constants or a handoff field that does not exist;
- distinguish full object class-size ceiling from bounded plaintext window accounting. The current full-length capacity call must not be labelled a window proof;
- synthetic provider wiring stays test-only and explicitly non-production; no fake A3 readiness can activate a product host.

A3 cannot claim end-to-end completion if it leaves any of those mandatory consumers as an unspecified future caller.

### A3-S05 — source-authority issuance versus session completion (UNRESOLVED LIFECYCLE CONTRACT)

Round 1 independent review found a second authority gap which selecting a principal does not solve:

- `EfRawExportAuthorizationRepository.cs:128-141` denies authorization with `SESSION_NOT_COMPLETED` unless the session is already Completed.
- `20260906055502_Tip88C1C6BIngressSqlComposition.cs:138-155` requires an existing Authorized decision and unexpired permit for a fresh retained snapshot.
- The same migration `:429-443` requires Available sources for both completion selections.
- The reference C6B dispatch §5.0 (`:353-371`) and §7.3 (`:856-874`) require those sources before applicable Client session completion.

Unchanged composition would therefore require `Completed -> Authorized permit -> R1 -> Available -> Completed`. No initial fresh retained session can traverse that cycle. A fixture that precompletes the session or directly seeds an authorization decision would hide the problem.

Before implementation, pin an authorized source-retention authority issuance path at the actual pre-completion state, including its eligibility, principal lineage, persisted evidence and replay semantics. Distinguish retaining an accepted source from later authorizing export of a completed session; do not silently relax the existing export authorization gate or move session completion earlier. A separate pre-completion retention path is a design direction to evaluate against the ratified source-consent machinery, not authorization granted here to create it.

The final ordering must explicitly include actual issuance before R1 and actual Client completion after Available. S01/S05 require a bounded authority reconciliation; no existing consent or export policy may be weakened to make the test pass.

## 5. Proposed delivery batches — one A3, no extra authority implied

| Batch | Work | Completion evidence |
| --- | --- | --- |
| A3.0 | Resolve S01/S05 principal lineage and pre-completion issuance; literal S02/S03 outcome/function/role/field ledgers; freeze exact new paths and package/project deltas | Fresh-session authority flow has no completion cycle; Builder need not invent principal, issuance or result semantics |
| A3.1 | Metadata-only broker host, API adapter, runtime B + R1 commit; no provider body read on denial/replay | Real private HTTP + PostgreSQL observer sees committed R1 before first read; lost broker response does not duplicate claim/grant |
| A3.2 | Acceptance/session-completion and Agent retained lease + CRT1 sender | Accepted DG2/selfie reaches correct one-call route; liveness/rejected evidence cannot; no Client API key on wire |
| A3.3 | Infrastructure body/stage façade and durable continuation | R2 verifier→R3→R4→R5→R6, exact replay, authority loss and fault residues |
| A3.4 | Synthetic cross-repo integration/readiness and as-built review | Full test/golden/architecture/provenance gates; production remains prohibited |

These batches are work ordering, not five new TIPs or five independent permission requests. A3.0 is incomplete until S01/S05 and the dependent literal contracts close.

## 6. Candidate mutation ownership — NOT an executable allowlist yet

Existing scoped families:
- Server API R27 result mapping/registration/Program, Application runtime ingress/result ports/coordinator, RawExport contracts.
- Infrastructure claim broker transaction/registration, acceptance reader/writer composition, capacity, R2–R6 façade and readiness.
- Application evidence response and Client session-completion consumers.
- Agent CaptureRuntimeHttpClient/WireCodec, existing orchestrator/retained lease and actual device ownership paths as proven necessary.
- Focused Server/Agent tests and explicit acceptance runner; project graph changes only for the reviewed broker host and test composition.
- One new forward migration/designer/model delta only if required by the selected S01/S03/S05 contract; none is authorized or named as already existing here.

Conditional new project proposal: `src/TagEkyc.RawIngressBroker/TagEkyc.RawIngressBroker.csproj`, ASP.NET Core net8.0. Host depends on public Infrastructure composition and Application/Contracts; no Api project reference.
Do not create it under this docs-only task.

The final dispatch must replace this family inventory with exact path + raw and Git representation SHA + purpose + owner + proof rows after S01/S05 resolution.
No staging, commits or pushes of A3 docs/source are requested in this task. A2 push authorization does not extend to new A3 commits.

## 7. PI-TAG-001 invariant and Test-Bite obligations

Pilot TIP TIP-88C1; High-risk. Modules: SQL/transaction, API, crypto/provider reuse, raw custody, recovery/cleanup and governance.
Invariant, outcome/precedence, shape, ordering and Test-Bite matrices all apply.
Census is NOT a review round. Two independent drafting census lanes ran. Formal review ledger is a separate file.
Round-5 root-cause checkpoint and round-10 hard stop remain enabled. From round 3, explain accumulated non-convergence causes before further patching.
Source-authority contradiction is not an ordinary coding defect and cannot be repaired through fixture success.

| Proof obligation | Positive evidence | Mutation that must be RED |
| --- | --- | --- |
| P01 authority principal | Exact authorized durable principal→decision/permit→custody | Substitute installation/client/operator principal or pick first decision |
| P02 N/B/R1 order | Independent DB observer sees N and committed R1 before first body read | Read body before B commit; share N with B rollback |
| P03 exact replay | No second grant/claim/key/object/read on metadata-only replay | Treat ExistingMatch as NewReservation/handoff |
| P04 36-outcome projection | Each reachable row maps exact code/shape; every other row assigned correct owner | Convert a terminal or ResumePending result to NOT_READY |
| P05 acceptance | Durable Passed evidence→exact acceptance; both completion selections atomic | Artifact-only acceptance; one-sided completion; wrong raw class |
| P06 Agent wire | Active CRT1, actual server parser, immutable signed observations and unread pre-R1 body | API-key fallback; Expect timeout fallback; changed class/digest/revision |
| P07 transport | Real Kestrel with controlled broker commit barrier and bounded physical buffering | Auto-Continue intermediary, proxy disk/full buffering or early body timer |
| P08 R2→R6 | Real durable verifier/staging/commit/publication, fault at each boundary | Publish PendingVerification; skip authority checkpoint; lose cleanup |
| P09 lease/capacity | Correct class-size/window constraints, disposal on all exits | Reset deadline on retry; inflate budget after config refresh; count object bytes as proof of window bound |
| P10 isolation/readiness | Restricted broker login and role-specific provider capabilities | API resolves broker credential; fixture advertises production-ready |
| P11 closure | Exact staged snapshots of both repos + explicit opt-in tests | Missing sibling silently excludes acceptance; LF/CRLF hash drift |
| P12 fresh-session authority lifecycle | Noncompleted retained session -> actual authorized issuance -> R1 -> Available for both classes -> Client completion, through real production boundaries | Restore Completed prerequisite to the selected pre-completion issuance path; bypass consent/eligibility; hide cycle with precompleted session or preseeded decision |

Existing A1 unread-body/nonce tests, A2 262-pass suite and R2–R6 component tests are regression inputs, not evidence that these new integrated obligations have run.

## 8. Current result and next authority

Push A2: COMPLETE. A3 source/code mutation: zero. New A3 tests executed: zero.
A3 dispatch executability: HOLD, not PASS.
Do not issue implementation authority for this candidate while S01/S05 remain unresolved or the dependent literal S02/S03 ledgers remain incomplete.
No legal/consent/Recipient/enrollment redesign is requested by the census.

Once principal lineage and pre-completion source-authority issuance are resolved, finish the exact ledgers in this same A3 draft lineage, run independent full coverage then affected-surface re-review, and hand the exact SHA to CC/GPT. No “implementation can start” claim before then.

## 9. Exact source evidence inventory

Raw SHA values below bind this working representation; TRACKED_CLEAN means Git reports no content change against the pinned HEAD. A landing must also freeze Git blob representation, not assume raw checkout SHA equals LF export.
Untracked predecessors are preserved, never replaced or deleted. They are reference material, not evidence of product implementation.

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

## Changelog

- v0.2: push A2 references; replace assumed reuse with actual caller/DI census; restore complete-before-body order; identify missing principal and frozen result seam; enumerate acceptance/Agent integration ownership and discriminate synthetic from production providers. Round 1 correction adds the independently found completion/permit cycle (S05), fresh-session P12 and conditional exit criteria; labels historical umbrella §10.6 non-operative. No code/schema/test/config change.
- v0.1: preserved 35-line split skeleton. Its begin-only/body-pipeline claims are not carried forward as executable facts.
