# A3 macro-wave W1 — R2–R6 final review packet v1

**Status:** CANDIDATE — independent ratification required  
**Date:** 2026-09-20  
**Scope:** all nine rows assigned to `W1_R2_R6_EXECUTION`  
**Non-claims:** not A3 PASS; no full suite, stage, commit, push, landing or production activation

## 1. One-wave disposition

| Row | Disposition | Exact reason |
| --- | --- | --- |
| BP12 `CleanupSurvivesHttpReceiptLoss` | OPEN — named proof gap | Real receipt loss, independent-scope cleanup and a shared ten-cut process-kill matrix exist, but no one source joins process death after the lost receipt to durable pending cleanup and replay. |
| BP19 `AllStageEnumMembersHaveExactProjection` | SEMANTIC/PRODUCT BLOCKER | Declaration census is green; unknown numeric stage-result projection is not ratified and must not be guessed. |
| BP20 `TransientO18SettlesThenSameOwnerRetryReachesAvailable` | OPEN — named proof gap | Transient recovery and RE01→R2–R5 components exist, but not the required same-source failed-R2 settlement → successor Available join. |
| BP20 `ReentryCannotRaceStageOrTerminalIntent` | OPEN — named proof gap | TI01↔R3 and concurrent RE01 controls exist separately; the required same-head RE01 against R3/TI01 in both lock orders is absent. |
| BP20 `TerminalOrDifferentOwnerCannotReenter` | OPEN — named proof gap | Owner/binding/fence and settlement controls cover subsets, not one exact O06/O19/O20 + different-owner mutation-sensitive family. |
| BP20 `ExpiredTransientAttemptFinalizesRecaptureWithoutHistoryRewrite` | OPEN — named proof gap | O20 finalization and byte-preservation components exist, but the original transient attempt's horizon expiry and immutable historical row are not joined in one source. |
| BP21 `LostR1ResponseBeforeKeyRecoversWithoutMaterial` | OPEN — named proof gap | NPS01, RE01, successor R2–R5 and process-kill components exist; there is no same-source child-process death after committed R1 and before key preparation. |
| P21/O21 `ResumePending` | **CANDIDATE_PENDING_RATIFICATION** | Exact Kestrel/TLS → production Agent → broker/PostgreSQL path, bodyless retry, durable residue and current-source discriminating G-O21 RED are complete. Historical G-V remains the separate CP08 durable-object/verifier guard. |
| P23/O23 internal `EncryptionFailed` | OPEN — named proof gap | Actual encryption failure proves O23 does not egress and recovery exposes O18/O16, but the full authorized safe-terminal recovery alternatives are not yet joined. |

Every W1 row therefore has `candidate PASS`, a specific blocker, or a specific missing join. No scenario remains unprocessed.

## 2. P21 candidate

The positive control is the `cancelAtCommitted:true` branch of `R2R6HttpsPublicationReplayAndMismatchScenarios`. It uses real loopback Kestrel/TLS, the production CaptureAgent HTTP client, production admission/broker code, PostgreSQL and durable custody. The first request is cancelled only after R4 is durably `Committed` while R5 is held. A fresh Agent call receives exact O21 with no new raw-body read, key or object; independent production continuation reaches `Available`; later replay returns O17 with the same source.

`a3-w1-p21-baseline-current.trx` is **1/1 PASS** on current source.

G-O21 changes only the broker's preserved-ciphertext projection from O21 to O18. `a3-w1-go21-broker-projection-red.trx` is **8 pass / 1 fail** across all nine HTTP scenarios. Only `cancelAtCommitted:true` fails; the production Agent throws `NOT_READY` instead of receiving O21. The eight adjacent scenarios remain GREEN.

An earlier mutation in the new-body pipeline fallback produced **9/9 PASS** in `a3-w1-go21-adjacent-green-diagnostic.trx`. It is recorded as `EXCLUDED_DIAGNOSTIC`, because this proves the O21 retry is owned by the broker preserved-ciphertext projection rather than that adjacent branch.

## 3. Restoration and joined gate

Production sources were restored byte-exact:

| File | Restored SHA-256 |
| --- | --- |
| `RawIngressBrokerTransactionFacade.cs` | `8ADCA9B698B0E738B641BE281686669C2E79F47CFC191EE3C30249AFE2CB5C09` |
| `CaptureRuntimeSourcePipeline.cs` | `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2` |
| `Tip88C1C6BA3R2R6ClusterHttpTests.cs` | `3506F74EFF64843426CD11BAF46348B99488C9F67E571D85447454168BA024C6` |
| `Tip88C1C6BA3R2TerminalProjectionTests.cs` | `AC6721EC2C6DAF8C18E759E3AFE6C4954C3B3128C5FA23EC1B6CEDAAA457CFD8` |

After rebuild, `a3-w1-r2r6-restored-joined-final.trx`, SHA-256 `1523CCDB2F07F3A88343FF24D41CE34BBBCD68673D82293814E55602DF9F8351`, passed **27/27**, zero fail/skip, in 9m16s. It includes:

- all nine Kestrel/Agent transport scenarios;
- the shared ten-cut process-kill matrix;
- the RE01 family;
- the stage catalogue and O19 negative family;
- affected ratified BP16/BP17/BP18/BP21 sentinels.

## 4. Machine evidence

- Scratch ledger: `a3_macro_wave_scratch_evidence_v1.tsv` — 4 runs.
- Wave TRX inventory: `a3_macro_wave_w1_r2r6_trx_inventory.tsv` — 4/4 present.
- Failed-run census: `a3_macro_wave_w1_r2r6_failed_run_census.tsv` — 1 `EVIDENCE_RED`, 0 unclassified.
- Recorded mutant registry: two W1 source hashes added; cumulative live match must be zero in the candidate manifest.
- Governance partition: 52 authority-open rows, exact assignment 52/52; P21 remains authority-open as `CANDIDATE_PENDING_RATIFICATION` until explicit ratification.

Final verifier counters:

```text
manifest_files=1884
delta_added=11 delta_modified=4 delta_deleted=0
trx_total=4
failed_runs_total=1
evidence_red=1 superseded_red=0 excluded_diagnostic=1
unclassified_failed_runs=0 outside_name_family_failed=0
mutants=90 live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

## 5. Requested review disposition

Ratify P21/O21 only. Keep the other eight W1 rows open or blocked exactly as listed. Authority-open remains **51 normative + 1 seam = 52** before review; ratifying P21 changes it to **50 normative + 1 seam = 51**. A3 remains HOLD.
