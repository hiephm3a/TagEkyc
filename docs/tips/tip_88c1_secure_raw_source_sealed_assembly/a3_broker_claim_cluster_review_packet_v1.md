# BROKER_CLAIM_CLUSTER_REVIEW_PACKET — bounded candidate v1

Disposition requested: independent review of one frozen BROKER_CLAIM cluster candidate. **This is not A3 PASS, landing, staging, commit/push or production authorization.** Official A3 open census remains **61**. The candidate reconciliation table has **57 open = 56 normative + 1 seam** after provisionally marking BP07, P09, P10 and P15 `IMPLEMENTED`; none becomes official without independent ratification.

## Nine-row disposition

| Row | Candidate disposition | Exact boundary |
| --- | --- | --- |
| BP07 `ExistingMatchReadsNoBodyAndNoNewKey` | **CANDIDATE_PASS** | Frozen R2–R6 durable-head matrix plus the new actual-child B0/B1/B3/B4 matrix prove Final-before-body dispatch, rollback before commit, durable replay after commit/receipt loss and unchanged key/object/provider counts. B2 after B-C/before commit is explicitly unreachable without a new product barrier and is not claimed. |
| P09 O09 `IdempotencyBusy` | **CANDIDATE_PASS** | Real PostgreSQL advisory-lock producer → Kestrel TLS → production Agent; outcome-only result, no body/provider/durable mutation. Existing Agent original-deadline proof has an O09-specific RED and O08 control. |
| P10 O10 `ClaimEvaluationInProgress` | **CANDIDATE_PASS** | Exact persisted `CurrentTokenExpiresAtUtc` reaches Agent; no body/provider/durable mutation. `CurrentTokenExpiresAtUtc → issued_at` makes only O10 RED. B3/B4 kill/replay preserves the committed alias and retry boundary. |
| P11 O11 `ClaimTokenInvalid` | OPEN | Public Kestrel/Agent denial and existing O11 projection RED are present, but the required next-retry internal-begin restart join is absent. |
| P12 O12 `ClaimRestartRequired` | OPEN — reachability/semantic blocker | B2 withdrawal and pre-B-B expiry both fail earlier as O02 `BINDING_INVALID`; no real O12 producer was found. No synthetic Final is accepted. |
| P13 O13 `SourceRetentionNotAuthorized` | OPEN — reachability/semantic blocker | Withdrawal cannot commit during the B1 preflight because B holds the conflicting consent-reference lock; pre-B-B withdrawal/expiry fails earlier as O02. |
| P14 O14 `HistoricCommitmentKeyUnavailable` | OPEN | Exact historic selector/no-latest fallback and public no-body denial are proved, but exact-old-key recovery plus a targeted current-byte guard mutation are not joined. |
| P15 O15 `FingerprintConflict` | **CANDIDATE_PASS** | Real conflict → Kestrel/Agent; no source/state/disposition leak or body/provider work; exactly one current `ConflictTombstone`, all non-alias durable rows unchanged. Tombstone→Bound makes only O15 RED. |
| P16 O16 `ReservationBusy` | OPEN | Public no-body result, live-lease residue, post-expiry retry sentinel and current-source state-gate RED exist. One output mutation does not independently prove every lease/revision/fence operand, so the row remains open. |

All scenario families BC-A through BC-E now have a PASS, a concrete OPEN boundary or a semantic/reachability blocker. There is no unprocessed scenario. The detailed mapping and current source pins are in `a3_broker_claim_cluster_execution_matrix_v1.md`.

## Actual Process.Kill matrix

`BrokerClaimProcessKillMatrixUsesOneChildHarnessAndDurableReplay` is one Fact that compiles one child probe and iterates four isolated cuts:

- B0: child is live before broker work; kill leaves the seeded canonical graph byte-equal.
- B1: child reaches the existing production content-commitment call after B-B while the root transaction is open; kill rolls the transaction back, independently observed.
- B3: O14 alias/evaluation state commits in the child; parent kills before receiving the result and later replays O10.
- B4: child reads already-durable O10 state; parent kills before receipt and repeats the same O10 result.

Every cut asserts a distinct child PID, a live process at `BC-CUT:<pid>:<cut>`, `Process.Kill(entireProcessTree: true)`, nonzero child exit, independent PostgreSQL state before/after kill, no provider-row drift and replay through loopback Kestrel TLS plus production `CaptureRuntimeHttpClient` with zero request-body reads. No plaintext, broker result, receipt or checkpoint state is serialized into the child input; only broker connection metadata, the production admission context and cut name are passed. B2 remains unclaimed because the production B-B/B-C transaction exposes no callback after B-C and before `CommitAsync`.

## Frozen source and focused evidence

```text
SOURCE
BrokerClaimClusterTests.cs  761B56F03C891ED4FDB29770B77DD437186D00F36BF0C55E9BE58559BE3BE5F4
A3 migration               4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4
Admission service           87F6741623601E1B6150F7DB025BEC5A37A6D0ADC1650063838CD3BA14D55A63

BASELINE CURRENT SOURCE
a3-broker-claim-six-outcome-final-source-positive-v2.trx
SHA256 7DDF83A5F8B239D71C8A4439AA238EAC7075DE98DC6791D8BF75E17C37267536
6/6 PASS

PROCESS KILL CURRENT SOURCE
a3-broker-claim-process-kill-matrix-positive-v5.trx
SHA256 1F8C1933E91BE000A39385BBEE6B9B05028D66D3BE915109B8B86A66FCD37C7C
1/1 PASS; internal matrix B0/B1/B3/B4

P10 CURRENT-SOURCE RED
product mutant C44274032B76CF92901E2DB8A69E38EB004C53608CB4144451A6B0D718A3A209
a3-broker-claim-p10-retry-time-final-source-red.trx
SHA256 26812E7F98F28017ABB521367756ACDC03EE650F7963EF02C8413FABA74FD286
6 total / 5 pass / 1 fail; only O10, exact persisted expiry replaced by issued_at

P15 CURRENT-SOURCE RED
product mutant E96F5148F0841B4DEFEE617F783B3BF87916FC36CDBA33E41D3DA757F6E51C41
a3-broker-claim-p15-tombstone-final-source-red.trx
SHA256 4B005CCCAAC0221A9196F02724DFB8D3E9601FCCAA09099C42A2423251D7EEFE
6 total / 5 pass / 1 fail; only O15, Expected ConflictTombstone / Actual Bound

P16 CURRENT-SOURCE RED (component only; row stays open)
product mutant A40F4C332C7D1343C3A348B3F54B47CCB4E23A6DE326F0031B70A259EDFCA2B0
a3-broker-claim-p16-state-gate-final-source-red.trx
SHA256 E70DD3603184CC70A3A270FBE69094568311B46528DB1DD9C3992285258ABE24
6 total / 5 pass / 1 fail; only O16, production Agent fails closed NOT_READY

FORCED-REBUILD RESTORED JOINED GATE
a3-broker-claim-final-restored-joined-gate.trx
SHA256 298F9410DB0D168235F1004EC64DAF482BE1C575A13E958C6278603CBB80C5F0
14/14 PASS = six public outcomes + one four-cut Process.Kill matrix
             + six same-transaction broker controls + one lease/retry sentinel
```

The migration was restored byte-exact before a forced non-incremental build. An earlier source-restored `--no-build` diagnostic exposed a stale P15-mutant DLL and is explicitly excluded in the failed-run census; it is not called restored GREEN. The three accepted current-source REDs were then rerun against the final test bytes, followed by another byte restore and forced non-incremental build.

P09 also relies on the already pinned product projection mutant and Agent same-lease/deadline mutant in the reconciliation ledger. Their product sources are unchanged; they were not rerun merely to generate new filenames. BP07 relies on the ratified R2–R6 HTTPS durable-head scenarios whose affected product/test sources did not change, joined here with the new broker-transaction kill matrix.

## Failed-run audit

`a3_broker_claim_trx_inventory.tsv` enumerates all **25** post-baseline TRX, including unreferenced diagnostics and superseded GREEN runs. `a3_broker_claim_failed_run_census.tsv` classifies all 20 new failed TRX:

```text
failed_runs_total=20
evidence_red=3
superseded_red=3
excluded_diagnostic=14
classified_failed_runs=20
unclassified_failed_runs=0
outside_name_family_failed=0
baseline_trx_drift=0
```

The three superseded REDs point uniquely to the three current-source `EVIDENCE_RED` rows. Diagnostics name the O12/O13 reachability discoveries, Process.Kill harness corrections and stale-binary false-restoration run individually; none is counted as mutation evidence. The verifier also scans failed TRX outside the filename family and fails rather than merely printing a warning.

## Candidate audit and limits

The final candidate manifest is `a3_broker_claim_cluster_candidate_manifest.tsv` with adjacent SHA-256 sidecar. It carries the full preflight-pinned file set, every Git-visible cluster delta and every `a3-broker-claim-*.trx` outside `bin/obj`, including all failed diagnostics. Unrelated ignored historical TRX are intentionally governed by the 992-file baseline inventory rather than being introduced as cluster drift. The mutant registry has 71 distinct hashes at this checkpoint; the final sweep must report zero live matches. Staged/conflicted must remain zero in both repositories.

No full suite was run. No authentication-production claim is made: the shared Kestrel host uses the accepted fixture authenticator solely to establish the post-authentication caller premise. No product route or runtime provisioning was changed. A3 remains HOLD.

Final machine audit:

```text
new_cluster_trx=25
failed_runs_total=20
evidence_red=3
superseded_red=3
excluded_diagnostic=14
classified_failed_runs=20
unclassified_failed_runs=0
outside_name_family_failed=0
failed_runs_in_manifest=20
missing_failed_trx=0
ledger_trx_references_distinct=533
referenced_trx_existing=533
referenced_trx_missing=0
referenced_trx_in_manifest=533
manifest_files_total=1777 (Server 1629 + Agent 148)
baseline_to_candidate_added=30 modified=7 deleted=0
recorded_mutants=71 unique_mutants=71
scanned_live_files=1168 live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

Audit artifact pins before manifest self-exclusion: TRX inventory `36C1650A9E30985F889179D678359EEDF9E6912E6973EBD93FB610CB463CC680`; failed-run census `0F38FBA272C14A9B6F7FDEAA40EBE910C30CFD61EB97C9FCB94D9528F98B5021`; cluster failed-run verifier `C9C51F5D4653AC9E618AB60E476FE08D64117BAAB8DDF0A9383DB99A8CFF43BB`; cluster matrix `A8626CC056EFEACE22D3EBFDA67A40E7F8958FE69CC13C36E16B9043E95773BF`; reconciliation ledger `E2DB094E89077A452FD358F6CECDF0B17CB1F019E156C6A4DC36233900BBD0FD`. The manifest and sidecar exclude their own bytes; the sidecar is the sole exact manifest-SHA authority.

Requested review: ratify only BP07/P09/P10/P15 if their exact joined claims survive. Keep P11/P12/P13/P14/P16 open at the stated boundaries. Confirm official **61** versus candidate **57**, exact current-source RED specificity, actual child work at all four reachable cuts, failed-run completeness, manifest/drift and zero live mutants.
