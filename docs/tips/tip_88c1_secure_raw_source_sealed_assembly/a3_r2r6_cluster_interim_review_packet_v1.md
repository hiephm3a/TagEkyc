# TIP-88C1-C6B A3 — R2–R6 consolidated interim review packet v1

Status: **INTERNAL_CHECKPOINT_NOT_FOR_RATIFICATION**. This historical interim snapshot records all 20 mapped R2–R6 rows and the TH5/TH7 components, but it is not a review handoff or the final cluster packet. The cluster-atomic final handoff condition is not met: TH3/TH4/TH6/TH8/TH9/TH10 still contain independent uncompleted scenarios, and there is no restored joined gate covering every current scenario on one frozen candidate. No full suite, stage, commit, push, landing or production activation is claimed. A3 remains HOLD.

## Requested review disposition

Three rows were independently ratified before this checkpoint and remain regression anchors: BP17 `CiphertextFailureIsNotProducerMismatch`, BP18 `StageCannotOvertakeTerminalIntent`, and BP21 `ProviderEvidenceCannotMasqueradeAsNoStart`. Two rows are marked `IMPLEMENTED` only as builder candidates in the reconciliation: BP16 is **READY_FOR_FINAL_RATIFICATION** and BP11 is **PROVISIONAL_PENDING_FINAL_FREEZE**. Neither is ratified by this document. BP11's publication mutant predates the later O19/pipeline correction and is to be repinned once after final candidate freeze. The remaining 15 rows stay `NOT IMPLEMENTED`. TH5's conditional-positive-absence correction and TH7's same-source receipt-loss/R6 join are bounded components, not P19/BP12/P17/P22 closure.

Ratification chronology resolves the earlier one-row accounting discrepancy: E01 left 66 open; BP18 → 65; BP21 provider-evidence → 64; the cluster directive ratified BP17 ciphertext-cause → **63 official open**. BP16 and BP11 are two unratified builder candidates; the reconciliation table mechanically shows **60 normative + 1 seam = 61 candidate open**. Do not call 61 official before independent ratification. The historical 64-official figure preceded BP17's ratification.

## Full 20-row disposition

| Row | Current status | Exact bounded evidence or missing dimension |
| --- | --- | --- |
| BP11 AvailableOnlyAfterPublicationCommit | PROVISIONAL_PENDING_FINAL_FREEZE | Actual Kestrel R4 Committed/R5 held/response pending then O22; projection mutant RED and historical restored GREEN. Current four-scenario HTTPS run retains positive control; repin exactly once after final candidate freeze. |
| BP12 CleanupSurvivesHttpReceiptLoss | OPEN | TH7 now joins one source through dropped R5 receipt, R6 Pending→Completed and O17 replay. Same-source real `Process.Kill` remains unjoined. |
| BP16 VerifierRunsOnDurableCiphertextNotRequestBuffer | READY_FOR_FINAL_RATIFICATION | R2 stores exact MinIO ciphertext; request stream disposed; CP08/verifier use exact object, no plaintext consumer; selector and marker-only consumer mutants RED; real cut-0 restart control. Do not rerun unless an affected source or guard changes. |
| BP17 ProviderUnknownDoesNotInventInputMismatch | OPEN, semantic blocker | Unknown-provider outcome must not be interpreted as absent provider or clean content mismatch; public projection unresolved. |
| BP17 CiphertextFailureIsNotProducerMismatch | RATIFIED anchor | One-bit corruption of stored ciphertext gives durable O20/cleanup, not O19; valid control and cause mutant. |
| BP18 StageCannotOvertakeTerminalIntent | RATIFIED anchor | Actual TI01/R3 two-order race and R3 intent-gate mutation. |
| BP19 AllStageEnumMembersHaveExactProjection | OPEN, semantic/product blocker | Unknown enum may fabricate O20; exhaustive real-result census and invalid-shape control absent. |
| BP20 TransientO18SettlesThenSameOwnerRetryReachesAvailable | OPEN | RE01 components exist; O18→settlement→new attempt→R2–R5 Available public join absent. |
| BP20 ReentryCannotRaceStageOrTerminalIntent | OPEN | Exact B-C versus R3/TI01 joined race absent. |
| BP20 TerminalOrDifferentOwnerCannotReenter | OPEN | Exact terminal/different-owner matrix absent. |
| BP20 ExpiredTransientAttemptFinalizesRecaptureWithoutHistoryRewrite | OPEN | Original-horizon expiry/O20 finalization with old-row byte equality not joined. |
| BP21 LostR1ResponseBeforeKeyRecoversWithoutMaterial | OPEN | NPS01 and RE01 components exist; same lost-R1 source to Available without key/material duplication absent. |
| BP21 ProviderEvidenceCannotMasqueradeAsNoStart | RATIFIED anchor | Four positive evidence states; repeated predicate mutant yields three targeted RED, one GREEN, independent append-only guard remains effective. |
| P17 O17 AlreadyAvailable | OPEN | Real Agent O17 replay, no body reread/key/object duplication and capacity controls are components; all required phase/residue and same-source restart dimensions not closed. |
| P18 O18 TemporarilyUnavailable | OPEN | Actual cleanup-incomplete/unknown-provider emitter and retry re-evaluation public join absent. |
| P19 O19 ContentCommitmentMismatch | OPEN | TH5 clean complete mismatch, conditional positive-absence correction and exact-cause negative controls are components; full row phase/residue matrix remains. |
| P20 O20 RecaptureRequired | OPEN | SQL terminal and local no-call components exist; exact server/Agent plus local/expiry join remains. |
| P21 O21 ResumePending | OPEN | Ten real kill cuts and ciphertext preservation are components; same-source public O21→O22 Agent continuation remains. |
| P22 O22 Available | OPEN | R5 publication, TH7 R6 cleanup and O17 replay are components; full row-specific phase/residue/restart join remains. |
| P23 O23 EncryptionFailed / NO EGRESS | OPEN | Aggregate no-egress is ratified elsewhere; actual encryption-failure producer through safe settlement/public O18-or-O20 remains. |

## Current-byte freeze and guard scope

All hashes below are raw SHA-256 of live files at packet preparation. Historical TRX are attributed to their own source generation; this table is **not** a claim that every older run executed these bytes.

| File | SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4` |
| `src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs` | `87F6741623601E1B6150F7DB025BEC5A37A6D0ADC1650063838CD3BA14D55A63` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportR2CompletionVerifier.cs` | `58AF7E6639382DDF292F080C6E32868F26B83F78ABB86BEF74538391FD4E62A6` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `AD99B4DC69DC14638B8093505C44E95F07729ABDB871FFBD9ACAE45BC1EDE3D3` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2R6ClusterHttpTests.cs` | `4B37C2901F1E9B8CDBCA3E2887CF40D44C94350CF54671824BB2825EBD81B274` |
| `tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs` | `EA8948FFC509880CF5BCFE1EE5CBAA75B6FA140FFF8EDB4D6F26129DA3213382` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs` | `EBBD041D126212100ACBCBAB3E7A73E857A1096FB7EB804F883D3C1DF15E7BB7` |

The test fixture now pulls `minio` and `mc` from Quay at the **same exact image digests** previously pinned at Docker Hub. Docker local image inspection confirmed both RepoDigests. This restores the missing dependency; it does not upgrade or replace the object provider. The prior 0/4 image/setup diagnostic remains excluded.

## Raw machine counters and artifact hashes

All paths in this block are under `tests/TagEkyc.IntegrationTests/TestResults/`. Fields are `total/passed/failed/notExecuted`, read from TRX `ResultSummary.Counters`; SHA-256 was computed from the raw TRX bytes. REDs are product-guard mutations only where the named assertion target was inspected. The 10-cut child-process matrix is one test within its 12/12 joined TRX, not ten separate counted test identities.

```text
a3-r2r6-cluster-restored-joined.trx               12/12/0/0  867A92F335CBE3BB002797FB96A0B33CE5057CA16061436B4052A0ECD87F1C9D
a3-r2r6-bp16-consumer-final-source-red.trx         1/0/1/0   90D030F61BF771C0F921BB23DA50BB3D28EB1737BCDFBA648AE3112E80D6F399
a3-r2r6-bp21-four-evidence-final-source-red.trx    4/1/3/0   393D382B439CA26F55D4E5D0DF6D8AB1530819C0D61DF1FE236F397A6A7001F2
a3-r2r6-replay-body-read-final-source-red.trx      2/0/2/0   9DC6A612A2B8255930A1FFD07EF64F8CBC6B99158075871C36020BF925125FFA
a3-r2r6-o19-notarmed-guard-red.trx                 1/0/1/0   A49881CEF1A3315F07613753317AD940E9226CF7064869D33B53A1D6B533D1E8
a3-r2r6-o19-wrong-cause-guard-red.trx              1/0/1/0   EC40D37DBD218ADB1DEDA46F8FD4137642C1E98EF2F767F0C0EACDFF7F5058A8
a3-r2r6-o19-restored-joined.trx                    6/6/0/0   8907756FB7A201A2360D313757C4FD317A1B6CA5957A6B8064BFAA1889C9A910
a3-r2r6-o19-ratified-sentinels.trx                10/10/0/0  B1B92174A37FB6B7B89DDCA74D941BD8EE29BA17A961EF58762BF59595EB5BDE
a3-r2r6-bp11-kestrel-final-source-red.trx          1/0/1/0   57DE71AAFE6C362BA9E9498A8F23B530F3FDBD0DEF5AD2A83AC7BC3D238984C0
a3-r2r6-p17-kestrel-server-read-red.trx            1/0/1/0   1B4F7542DE5CD4FE7B70B234C1322550750D06EE6A80BA4B22F6C20FEA115A39
a3-r2r6-bp11-p17-kestrel-final-restored.trx        1/1/0/0   9E1CCC04F55284DC836FF70DF30B4949B64DF6AE4DFD1DC13A063967A3184441
a3-r2r6-th7-quay-restored-four-scenario.trx        4/4/0/0   149F985C270F6A97A4C70F95836F21FA8B2DEB97165C984B6E85400B3272CD32
a3-r2r6-th7-skip-cleanup-red.trx                   1/0/1/0   44DC0101C3EA6A647E6D4B04F1B44F559A5D93B8E99ED326FC730C1E969FE175
a3-r2r6-th7-quay-fixture-sentinels.trx             2/2/0/0   6D32C1CACEF160CBFE491653827EB820D9A61C02ADBE786B05CF8BCC9E8B806D
```

Key failure targets: BP16 marker-only plaintext consumer `Expected null / Actual "1"`; BP21 weakened NPS conjunction: preparation/provider-unknown/active-key fail under independent append-only guard while object-custody remains GREEN; O19 `NotArmed` and wrong-cause controls each change `Expected StateConflict` to `Actual Terminated`; BP11 early projection violates pending HTTP response; P17 forced body read changes server `ReadCalls` from 0 to 1; TH7 skip-cleanup leaves `completed=false`. No mutation is counted merely because the harness, Docker or PostgreSQL bootstrap failed.

## TH5 and TH7 component boundary

TH5 uses the Homeowner-approved **conditional positive absence** rule for exact O19 intent/attempt/source/key/object/fence and observed provider absence; it must not turn provider-unknown into no-start. The two SQL mutations above are independently RED, SQL is byte-restored, and the 6/6 restored join plus 10/10 ratified sentinels are historical focused evidence. P19 and BP20 terminal remain open.

TH7 is the new same-source join: first production Agent send through HTTPS/Kestrel; R4 Committed; R5 Available with two Pending obsolete cleanup items; first response dropped; fresh production worker in separate owner scopes reaches Completed, old key Revoked, obsolete SQL object Deleted and exact MinIO object `PositivelyAbsent`; fresh production Agent client receives O17 with zero additional server `Request.Body` reads and no new key/object. The fourth theory case was RED only when `Available + Pending` R6 dispatch was disabled, then product source restored byte-exact and all four HTTPS cases passed after rebuild. It is **not** a real `Process.Kill` on the same receipt-loss source. The separate ten-cut matrix remains valid historical evidence, not a fabricated same-source join.

## Manifest and delta

The companion `a3_r2r6_cluster_interim_candidate_manifest.tsv` and `.sha256` sidecar were built **after** this packet and the ledger amendment, avoiding a self-referential packet/manifest hash. The verifier's raw output is:

```text
ledger_trx_references_distinct=436
referenced_trx_existing=436
referenced_trx_missing=0
referenced_trx_ambiguous=0
referenced_trx_in_manifest=436
referenced_trx_omitted=0
server_manifest_files=1515
agent_manifest_files=147
manifest_files_total=1662
```

Against `a3_r2r6_bp16_bp17_candidate_manifest.tsv`, the normalized-path delta is **27 added / 9 modified / 0 deleted**. Additions are the O19 authority note, prior manifest and sidecar, cluster matrix, this packet, new transport/HTTP test source, and focused TRX (including excluded diagnostics). Modifications are the mutant registry, TRX-resolution inventory, reconciliation, A3 migration, pipeline, opt-in test project, MinIO fixture/image assertion and terminal test helper. Agent has **0 delta**. The O19 migration and pipeline modifications are explicit product corrections, not accidental drift; MinIO fixture changes only the registry host at unchanged image digests. Re-run verifier after any packet or source edit.

## Exclusions, drift, and final-cluster prerequisites

Excluded diagnostics remain in the ledger and matrix with their actual causes: earlier TLS handshake failure, PostgreSQL bootstrap/stream abort, incorrect client-side body-read observation, TH7 test-only function insertion failure and missing Docker Hub images. None is treated as a contract RED or restored GREEN. Product source/SQL mutation restoration is supported by live source hashes above; the cumulative mutant registry has 65 distinct hashes and the latest sweep found zero live matches among 734 visible `.cs` files in both repositories. The separate manifest/verifier sidecar must be used for the broader controlled-file and referenced-TRX comparison. Both repositories must remain staged=0 and conflicted=0.

This is **not** `R2_R6_CLUSTER_REVIEW_PACKET` under the cluster-atomic directive and must not be used for ratification. Before the sole final packet: disposition every TH0–TH10 scenario; perform remaining eligible guard mutations by shared mechanism only after candidate freeze; restore exact bytes and rebuild; run one joined restored gate and ratified sentinels; then manifest/drift/mutant sweep. In particular TH3/TH4/TH6/TH8/TH9/TH10, BP17 provider-unknown and BP19 unknown-enum semantics remain open. Do not close a row from a green component alone, run the full suite early, or infer A3 PASS. No further interim packet is to be issued.
