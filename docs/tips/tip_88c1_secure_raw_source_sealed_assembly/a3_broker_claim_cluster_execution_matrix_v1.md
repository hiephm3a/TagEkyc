# A3 BROKER_CLAIM cluster — Phase A mechanism inventory

Status: **preflight only; no new test execution or row closure**. Incoming ratification is BP16/BP11 on frozen R2_R6 manifest `4DBCB74FB003D9B6ED489EE2D1481528378D37068115FC96120EDD96C0899223`. Official and candidate A3 open censuses both equal **61**; A3 remains HOLD. The route matrix has **9 BROKER_CLAIM rows**, BP07 plus P09–P16. Its older ten-row count included BP19, which was reassigned to R2_R6. This file does not reopen the two ratified rows or reinterpret BP19.

Authority: `tip_88c1_c6b_a3_broker_composition_dispatch_v0_6.md` §6.2 O09–O16, `tip_88c1_c6b_a3_broker_pipeline_contract_v0_5.md` §6 BP07, and `a3_preflight_open_matrix.tsv`. Production path under test is API admission → private broker transport → `RawIngressBrokerTransactionFacade.AdmitAsync` → existing B-B/B-C SQL and durable readback → API mapper → production Agent client. A synthetic/non-patient setup may supply prerequisites, but not a Final result or the claim decision. Fixture authentication qualifies only a post-authentication claim unless the real authenticator is exercised.

## Nine-row disposition inventory

| Row | Required producer/transition | Shared mechanism | Still-open dimension |
| --- | --- | --- | --- |
| BP07 ExistingMatchReadsNoBodyAndNoNewKey | Replay existing Available, busy, preserved-ciphertext and semantic-terminal durable heads | B-C state projection + final-before-body gate | Matrix over exact durable heads; no blanket ExistingMatch→Handoff |
| P09 O09 IdempotencyBusy | Competing same-alias advisory lock | B-B lock + transaction/HTTP response | Joined HTTP→Agent bounded backoff, byte-exact residue, no body/provider work |
| P10 O10 ClaimEvaluationInProgress | Live persisted alias-evaluation token | B-B token/clock | Exact stored RetryNotBeforeUtc on wire and Agent; no token/attempt/read; targeted RED |
| P11 O11 ClaimTokenInvalid | Stale/wrong B-C claim token | token CAS and internal restart | No token/target egress; no body; internal-begin restart, not public upload-init |
| P12 O12 ClaimRestartRequired | Expired/moved internal claim evaluation | B-B begin/restart | Real producer, preserved identity, next retry internal begin; no second upload-init |
| P13 O13 SourceRetentionNotAuthorized | Fresh retention authority loss | B-B/B-C authority checks | Rejection at claimed checkpoints with zero R1/key/object/body and no export-permit fallback |
| P14 O14 HistoricCommitmentKeyUnavailable | Exact historical commitment selector unavailable | historic-key preflight | No latest-key fallback/second source read; recovery when exact old key returns |
| P15 O15 FingerprintConflict | Competing claimed fingerprint on bound alias | B-C comparison + ConflictTombstone | Public HTTP→Agent denial, no source disclosure/second read, replay/lease residue |
| P16 O16 ReservationBusy | Live/unsettled exact current attempt | B-C state gate + lease/CAS | Independently test gate operands, no second attempt/body, exact post-settlement retry |

## Shared mechanism/scenario matrix — complete before edits

`B0` means no B transaction committed; `B1` means B-B evaluation in the transaction; `B2` means B-C has returned but B commit/response is not yet acknowledged; `B3` means the broker transaction committed while the HTTP receipt may be lost; `B4` means a durable head exists for replay. Each planned scenario has a positive control and an expected negative guard; a genuinely unreachable branch is classified as OPEN/blocker rather than filled with a canned Final.

| Scenario family | Durable pre-state and scheduling | HTTP/Agent/body observation | Rows mapped | Shared guard to mutate after all controls pass |
| --- | --- | --- | --- | --- |
| BC-A lock/token | Same alias, two broker transactions; live/expired evaluation token; exact PostgreSQL clock | One Kestrel host and production Agent client; no body read on Final; retry time/code/shape | P09–P12 | alias lock; token CAS; persisted retry-time projection; internal-begin restart |
| BC-B existing-head | Available, preserved ciphertext, live Reserved, semantic terminal; exact source and attempt IDs | Same Kestrel/Agent pair; zero replay body/key/object; exact Final vs Handoff | BP07, P16 | B-C state gate and final-before-body dispatch |
| BC-C historical/authority | Historic selector unavailable/returned; consent/permit revoked before and during B | Same host/client; zero fresh provider/body; no latest-key/export fallback | P13, P14 | exact historic selector and fresh authority check |
| BC-D conflict | Bound alias with conflicting fingerprint and ConflictTombstone | Same host/client; no source ID/claim token leak; durable alias/attempt unchanged | P15, BP07 | B-C comparison/tombstone projection |
| BC-E crash/receipt | One shared child-process harness at B0/B1/B2/B3 boundaries, each with isolated database/alias and independent observer; actual `Process.Kill`, new child/PID, no serialized in-memory result | Replay through the same public Kestrel/Agent arrangement; prove commit-vs-rollback and zero second body/key/provider work | BP07, P09–P12, P16 where each state is reachable | B commit/ack boundary and durable readback; no per-row child harness |

The process-kill family is **one matrix by boundary × durable state × owner**, not separate tests/fixtures per row. Before implementing it, confirm exactly which B1/B2 sub-boundaries can be externally held without changing the product's single-transaction semantics. A kill before B commit must not be treated as a committed claim; a lost response after commit must not create a second Handoff. TH process-kill evidence from the ratified R2_R6 cluster is a frozen sentinel, not a substitute for B commit/receipt-loss proof and is not rerun merely to populate this matrix.

## Unique production-guard inventory before any mutation

The following is an inventory of *mechanisms*, not nine independent row worklists. Line locators refer to the current `20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` and the current broker/admission sources; final evidence must bind exact source SHA and test identity, not these movable line numbers.

| Guard | Owning decision | Reused by | Current reachability / proof caveat |
| --- | --- | --- | --- |
| BC-G1 | B-B paired alias/exact advisory-lock deadline (`:1023–1061`) | P09, BP07 | Existing real-PostgreSQL component drives O09; public Kestrel→Agent and first-body-read observation remain to join. |
| BC-G2 | B-B exact alias identity/envelope conflict, live evaluation token, and persisted retry time (`:1080–1109`) | P10, P11, P15 | The O10 token must be persisted by an earlier committed B denial, not fabricated in a test row. Historic-key denial is one candidate predecessor; verify its durable alias state first. |
| BC-G3 | B-B expired-token revision/fence CAS, `rows_changed == 1` (`:1124–1198`) | P12, P11 | Do not claim O12 reachable merely because SQL has a raise. The alias lock plus `FOR UPDATE` may exclude an ordinary caller interleaving; require a real producer or classify a precise reachability blocker. |
| BC-G4 | B-C token validation and post-lock expiry (`:1519–1527`, `:1591–1593`) | P11, P12, P13 | B-C denial is not an independent public upload-init. Exact token/target must never be emitted to the Agent. |
| BC-G5 | B-C current retention authority / consent revalidation (`:1550–1632`) | P13, BP07 | Test loss before B-B and while B is held; compare exact durable rows and no R1/key/object/body read. |
| BC-G6 | Existing-claim historic selector in production preflight; `NULL` commitment maps to O14 (`RetainedSourceClaimPreflight.ComputeAsync`, SQL `:1637–1643`) | P14, P10 | The preflight must use locked historic selector, never current/latest; recovery needs the exact old key restored. |
| BC-G7 | Existing reservation admission-fingerprint equality versus conflict tombstone (`:1686–1740`) | P15, BP07 | An existing alias and a new alias can take different conflict paths. Preserve the bound alias, claim and attempt bytes. |
| BC-G8 | B-C durable-head projection: Available, staged/committed, live/unsettled R2, terminal (`:4404–4470`) | BP07, P16 | No generic `ExistingMatch → Handoff`. The lease/termination and revision/fence operands need distinct controls; existing O16 component does not close those dimensions. |
| BC-G9 | `RawIngressBrokerTransactionFacade.AdmitAsync` commits B-B and B-C in **one** transaction (`:40–89`); `CaptureRuntimeRawIngressAdmissionService` chooses Final before body pipeline (`:22–35`) | all nine, especially BP07/P09–P16 | An in-flight B-B or B-C write is not durable until B commits. A child kill before commit must show rollback through an independent connection, not a second persisted claim; a lost response after commit is a separate case. |

For BC-E, the existing `BrokerUsesOneTransactionAndDerivedActor` test already has a non-substituting `IContentCommitmentService` probe between B-B and B-C and independently observes one root transaction and no committed rows. This supplies a plausible **B1** pause. There is no demonstrated callback after B-C but before `CommitAsync`; **B2 is not yet a reachable deterministic cut**. Do not add a product callback solely to make B2 testable or claim two distinct durable states for B1/B2. The shared child-process design must first pin an externally observable barrier; otherwise record B2 as an explicit harness/reachability limit and continue B0/B1/B3/B4 and other mechanisms.

## Reachability findings from the shared positive-control phase

- **P12 / O12 remains OPEN — producer not reached.** B2 withdrawal and pre-B-B permit expiry both fail earlier in `capture_runtime_read_bound_raw_ingress` as O02 `RAW_EXPORT_SOURCE_BINDING_INVALID`; neither is evidence for O12. The ordinary alias lock plus `FOR UPDATE` path still has no demonstrated producer for `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED`.
- **P13 / O13 remains OPEN — B1 authority-loss interleaving is serialized out.** The real provider pause occurs after B-B and before B-C, but B already owns the consent-reference shared lock. E01 withdrawal requires the conflicting lock and cannot commit until B ends. Increasing the qualified request deadline from one to six seconds reproduced the same wait and timeout; this is a structural lock-order result, not a slow-environment diagnostic. Pre-B-B withdrawal/expiry returns the earlier O02 binding denial. A product semantic decision is required before claiming an O13 producer at this checkpoint.
- These blockers do not hold the six independent public Kestrel→production Agent outcomes O09/O10/O11/O14/O15/O16.

## Horizontal execution protocol

1. Finish all product/test edits and shared Kestrel/PostgreSQL/Agent/child-harness wiring before the candidate baseline build. Record every non-reachable producer as a precise semantic/product blocker without holding independent scenarios.
2. One candidate build; run positive controls by mechanism family BC-A through BC-E, not row order. Observe response bytes, first body read, exact durable tuple/history and provider counters through independent connections.
3. Run isolated mutations by **unique production guard**. For each: source SHA, target failing test/first assertion, restored byte identity; do not count fixture/setup failures as RED. If a new product defect forces Phase A edits, reset the candidate baseline.
4. Restore all mutations; rebuild once; run one joined focused cluster gate plus previously ratified sentinels only when affected code changed. No full suite at this phase.
5. Inventory every new TRX, not only ledger references. `a3_broker_claim_failed_run_census.tsv` starts empty and must name/classify every Failed run. The baseline `a3_broker_claim_baseline_trx_inventory.tsv` pins all 992 pre-cluster TRX; `a3_broker_claim_trx_verify.ps1` currently reports `outside_name_family_failed=0` and will fail if a new Failed TRX is omitted or a baseline TRX drifts. All new run names use `a3-broker-claim-*.trx`.
6. Only after all BC-A–BC-E scenarios are PASS, OPEN with a specific blocker, or legitimately STOP, publish **one** BROKER_CLAIM cluster packet with frozen candidate, joined restored gate, raw machine counters, manifest/drift/mutant sweep and all nine row dispositions. Do not issue row-level packets or change the official 61 without independent ratification.

No full suite, stage, commit, push, A4, landing or production activation is authorized by this preflight.

## Final candidate disposition

The cluster was executed horizontally on one shared PostgreSQL/Kestrel TLS/production-Agent arrangement. Four rows are marked candidate-PASS for independent review: BP07, P09, P10 and P15. P11, P14 and P16 retain their precise proof gaps; P12 and P13 retain the reachability/semantic blockers above. Official A3 census remains **61**; the mechanically updated candidate ledger is **57 = 56 normative + 1 seam**.

BC-E is one actual-child matrix, not four row tests. B0 kills before broker work; B1 kills inside the existing content-commitment hook after B-B while the root transaction is open; B3 kills after the O14 transaction commits but before its result reaches the parent; B4 kills a repeated O10 read after durable state already exists. Every child has a PID distinct from the test process, is live at the barrier, is terminated with `Process.Kill(entireProcessTree: true)`, exits nonzero, and is followed by independent durable readback plus public Kestrel/Agent replay. B0/B1 remain byte-equal until replay; B3/B4 preserve their committed rows; all four preserve provider rows and replay without reading the HTTP body. There is still no deterministic B2 callback after B-C and before commit, so no B2 claim is made.

Current frozen source pins: broker-claim test `761B56F03C891ED4FDB29770B77DD437186D00F36BF0C55E9BE58559BE3BE5F4`; A3 migration `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4`; admission service `87F6741623601E1B6150F7DB025BEC5A37A6D0ADC1650063838CD3BA14D55A63`.

Final restored joined gate `a3-broker-claim-final-restored-joined-gate.trx` is **14/14 PASS**: six public outcome cases, one four-cut child-process matrix, six same-transaction broker cases and one lease/retry sentinel. Current-source guard runs each execute all six public cases and are specific: P10 **5/6** (only O10 RED), P15 **5/6** (only O15 RED), P16 **5/6** (only O16 RED). P16 remains open because one state-gate output mutation does not independently prove every lease/revision/fence operand. The forced baseline rebuild and final forced restored rebuild prevent source-only false restoration; the stale-binary diagnostic is retained in the failed-run census.
