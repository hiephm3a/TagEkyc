# R2_R6_CLUSTER_REVIEW_PACKET — bounded final candidate v1

Disposition requested: independent review of one frozen R2–R6 cluster candidate. **This is not an A3 PASS, landing request, or production authorization.** The earlier `a3_r2r6_cluster_interim_review_packet_v1.md` is `INTERNAL_CHECKPOINT_NOT_FOR_RATIFICATION`; this is the sole cluster handoff. Official A3 open census remains **63**. BP16 is `READY_FOR_FINAL_RATIFICATION`; BP11 is `PROVISIONAL_PENDING_FINAL_FREEZE`. Neither phrase is a ratification. The reconciliation table mechanically has **60 normative + 1 seam = 61 candidate-open** because those two rows were provisionally marked `IMPLEMENTED`; the official 63 does not change without independent approval.

## Scope and immutable candidate

The exact candidate is the adjacent `a3_r2r6_cluster_final_candidate_manifest.tsv` plus its SHA-256 sidecar. The manifest includes this packet, the cluster matrix, both A3 ledgers, the complete R2–R6 TRX inventory and failed-run census, **all 109 cluster TRX including unreferenced diagnostics**, all Git-visible Server/Agent files, and no `bin/obj` outputs. The verifier indexes the matrix and both audit TSV files as evidence ledgers; its raw counters are below. Test source `Tip88C1C6BA3R2R6ClusterHttpTests.cs` SHA-256 `0588D3FCCDFA8E77B296FCF6C7B8A013E724F4D03D975DC3DA8B947F9E1EC4A3` contains nine shared HTTPS/Kestrel scenarios. Terminal test source SHA-256 `AC6721EC2C6DAF8C18E759E3AFE6C4954C3B3128C5FA23EC1B6CEDAAA457CFD8`; TH10 declaration test SHA-256 `375D037ED9BBE6DF3EF600DB712A49592F2185EB6312EC61F009C701F3A08F5D`.

Restored product source pins:

| File | SHA-256 |
| --- | --- |
| A3 migration / NPS01 / RE01 / TI | `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4` |
| `CaptureRuntimeSourcePipeline.cs` | `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2` |
| `CaptureRuntimeRawIngressAdmissionService.cs` | `87F6741623601E1B6150F7DB025BEC5A37A6D0ADC1650063838CD3BA14D55A63` |

## All 20 historical R2–R6 rows

`RATIFIED` means a pre-existing accepted anchor, not a new claim from this packet. `OPEN` is an explicit disposition, not an unprocessed scenario. All row-level gaps are cross-referenced to the scenario/guard matrix.

| Row | Disposition at handoff | What prevents further closure |
| --- | --- | --- |
| BP11 `AvailableOnlyAfterPublicationCommit` | **PROVISIONAL_PENDING_FINAL_FREEZE** | Real Kestrel/Agent Committed-before-R5 negative, O22-after-commit positive and current-byte G-P RED are present; independent final review still required. |
| BP12 `CleanupSurvivesHttpReceiptLoss` | OPEN | Same-source HTTPS response loss→R6 cleanup is proved, and separate four real kill cuts are proved; their same-source `Process.Kill` join is absent. |
| BP16 `VerifierRunsOnDurableCiphertextNotRequestBuffer` | **READY_FOR_FINAL_RATIFICATION** | Exact-object selection, non-null plaintext-consumer mutant and real kill/restart are proved; await final independent disposition only. |
| BP17 `ProviderUnknownDoesNotInventInputMismatch` | OPEN — semantic blocker | Unknown provider outcome still needs an authoritative result-projection rule; unknown is not positive absence. |
| BP17 `CiphertextFailureIsNotProducerMismatch` | RATIFIED anchor | Retained as 2-case sentinel in final gate. |
| BP18 `StageCannotOvertakeTerminalIntent` | RATIFIED anchor | Retained as two-order sentinel in final gate. |
| BP19 `AllStageEnumMembersHaveExactProjection` | OPEN — semantic/product blocker | Eight enum declarations/40 members are counted, but real producer/result-shape matrix is incomplete; undefined numeric enum can fabricate O20. |
| BP20 `TransientO18SettlesThenSameOwnerRetryReachesAvailable` | OPEN | O18→positive absence→bodyless O16 and separate RE01→O22 exist; no single safely settled transient source joins all stages. |
| BP20 `ReentryCannotRaceStageOrTerminalIntent` | OPEN | Two TI/R3 orders and concurrent RE01 controls exist, but the same head is not contested by RE01 against R3/TI01 in one proof. |
| BP20 `TerminalOrDifferentOwnerCannotReenter` | OPEN | Actor/binding/fence and terminal controls exist; exact same-source terminal/different-owner negative family is not mutation-complete. |
| BP20 `ExpiredTransientAttemptFinalizesRecaptureWithoutHistoryRewrite` | OPEN | Post-H terminal proof and RE01 original-H controls are separate; exact transient successor-to-finalization join is absent. |
| BP21 `LostR1ResponseBeforeKeyRecoversWithoutMaterial` | OPEN | Same-source HTTPS cancellation→NPS01/RE01→O22 is proved; same-source actual child `Process.Kill` plus Agent retry is not. |
| BP21 `ProviderEvidenceCannotMasqueradeAsNoStart` | RATIFIED anchor | All four evidence variants and the four-case mutant repeat are preserved. |
| P17 O17 `AlreadyAvailable` | OPEN | Genuine Agent HTTPS replay/bodyless result exists, but complete row-level phase/residue/authority join remains unproven. |
| P18 O18 `TemporarilyUnavailable` | OPEN | Actual AEAD failure→O18 and same-lease reevaluation exist; provider-unknown semantics and expiry/terminal join remain open. |
| P19 O19 `ContentCommitmentMismatch` | OPEN | Actual transit-byte corruption→positive absence→TI02/O19 and seven-case negative family pass; full row-level phase/residue/HTTP authorization proof is not closed. |
| P20 O20 `RecaptureRequired` | OPEN | Actual ciphertext tamper→settlement→409/O20 and bodyless replay pass; Agent local expired-lease/no-send is a separate source. |
| P21 O21 `ResumePending` | OPEN | Same-source request cancellation after R4→O21 with no reread passes; same-source kill/restart and complete row shape remain absent. |
| P22 O22 `Available` | OPEN | Agent first-send O22 after R5 and TH8 RE01 successor O22 pass; full R2–R6/publication/cleanup/receipt-loss join is not one proof. |
| P23 O23 `EncryptionFailed` no-egress | OPEN | Actual KEK unwrap failure emits O18, not O23, on HTTPS wire; exact safe-settlement/terminal variants and guard mutation are missing. |

## Scenario disposition, not row-wise tickets

The shared matrix `a3_r2r6_cluster_execution_matrix_v1.md` gives pre-state, host, TLS/auth premise, client, body-read expectation, response, mapped rows and guard for TH0–TH10. At freeze: TH0 transport positive PASS; TH1/TH2 Kestrel/Agent R5/O17 components PASS; TH3 O21 after request cancellation PASS but same-source kill/O22 OPEN; TH4 O18→positive-absence→O16 PASS with provider-unknown semantic blocker; TH5 O19 positive and seven negative branches PASS within its stated post-authentication scope; TH6 public O20 and separate local no-send controls PASS with same-source join OPEN; TH7 receipt-loss/R6 PASS with same-source kill join OPEN; TH8 same-source Agent lost-R1→RE01/O22 PASS with child-kill join OPEN; TH9 actual encryption failure/no O23 wire PASS with terminal variants OPEN; TH10 enum declaration census PASS with real producer/unknown-numeric projection BLOCKED. No scenario is left without PASS, OPEN with a concrete proof gap, or semantic/product blocker.

The real 10-cut process-kill method remains a frozen shared fixture: six full-R2 boundaries and four R6 cleanup boundaries, each with an actual killed child, fresh child PID, isolated database/source/object and durable readback. It is one of the 27 executed test identities below. The newer TH3/TH8 request-loss cases use real HTTP cancellation, **not** `Process.Kill`; this packet does not conflate them.

## Raw focused evidence and mutation outcomes

```text
RESTORED CLUSTER GATE
tests/TagEkyc.IntegrationTests/TestResults/a3-r2r6-cluster-restored-joined-final-candidate.trx
SHA256 57ACAFEA0BB576F25761250984EC4D5CA4755C1CA93E96531F1BCAC9E6B130BA
total=27 executed=27 passed=27 failed=0 skipped=0 duration=9m12s
partition: HTTPS 9 + RE01 6 + O19 negative family 1 + TH10 1
         + actual 10-cut child-process matrix 1 + BP16 1
         + BP17 ciphertext 2 + BP18 race 2 + BP21 evidence 4 = 27

AGENT LOCAL CONTROL (separate project/source)
tests/TagEkyc.CaptureAgent.Tests/TestResults/a3-r2r6-th6-agent-expired-lease-phase-a.trx
SHA256 24F623E9A378D5A7C6577AB0752BC85B5A69A77833BB0EE8DFF94426FC8B9083
total=2 executed=2 passed=2 failed=0 skipped=0

CURRENT-SOURCE G-P PAIRED-STATE MUTANT
product SHA256 378B2DDC36BFD1C71598CDA07F3C669C9853002F20F67FE5F8174D1393389757
TRX SHA256 379638B98B5BED58B6588A11FEC0A8604D39772136EB5064D0F8923DE929F0FA
total=1 passed=0 failed=1; Agent R5 waiter: Expected True / Actual False

CURRENT-SOURCE G-H FINAL-BODY-READ MUTANT
product SHA256 864AB93DE457C49DBE009EF94AB2A69DC3D60E6242D8F54E0568AE882C72D550
TRX SHA256 9B50285D203843F317B70CA48ADAA039030E56541E54C837D790CC027389D9C2
total=1 passed=0 failed=1; server Request.Body reads: Expected 2 / Actual 3

EXCLUDED G-P DIAGNOSTIC
product SHA256 A3C1F5F19D792058D6D17D91867ECC0E9AD6947B95394E328B4401F30DB94AA5
TRX SHA256 F77BA2BFBFE5E3C9AFA75B4AF4752380A8EEFA3EBFFAA05CE833FEF625954C34
total=1 passed=1 failed=0; mutation did not reach the forbidden Staged/Committed tuple
```

The G-P RED is at the R5 waiter rather than a wrong committed SQL row: the mutant skips R5 and returns early. The G-H RED is at the server read counter, not client serialization. The excluded GREEN is retained for audit and counted in the mutant registry, never counted as proof. The O19 `NotArmed` and wrong-cause isolated mutants, R6 skip-cleanup mutant, four-case NPS mutant and BP16 consumer mutant are pinned with exact TRX/hash/assertion in the matrix and reconciliation. All mutation product files were restored byte-exact before the one final build/gate. No mutation is claimed for an unreachable guard merely to improve the row tally.

## Failed-run audit

This documentation-only correction does **not** add a mutation, rerun a test, change product/test source, or alter the 20-row disposition. `a3_r2r6_trx_inventory.tsv` enumerates all **109** `a3-r2r6-*.trx` files under both repositories (108 Server, one Agent); `a3_r2r6_failed_run_census.tsv` has exactly one `FR001`–`FR048` row for each `ResultSummary=Failed`. The inventory also records the zero-executed `a3-r2r6-response-loss-diagnostic.trx`, rather than silently treating it as a PASS. The scope scan additionally checked all other TRX names for failed R2–R6 test identities and found none outside this naming family. Each census row contains the raw SHA-256, counters, failed test identity, first failure text, classification, applicable guard/row or successor/exclusion reason, and this packet/matrix/ledger locator. The TSV is the authoritative per-run locator; do not infer classification from a filename or from a RED counter alone.

Machine census: **48 failed runs = 11 EVIDENCE_RED + 9 SUPERSEDED_RED + 28 EXCLUDED_DIAGNOSTIC**. In particular, `FR006` (`a3-r2r6-bp16-consumer-red.trx`) is superseded by current-source `FR005`; `FR011` (`a3-r2r6-bp21-four-evidence-red.trx`) is superseded by current-source `FR010`. **BP11's accepted G-P RED is `FR013`, `a3-r2r6-gp-agent-committed-red-v2.trx`**; the BP21 NPS RED (`FR010`) cannot be counted for BP11. Earlier BP11 TestServer/Kestrel RED runs `FR001`–`FR003` are historical and superseded by `FR013` for the final candidate. These labels do not ratify BP16 or BP11.

TLS provenance is one shared-harness story, not five P17 defects. `FR022` was a preliminary Agent/Kestrel reachability assertion failure; `FR023`–`FR026` (`p17-agent-kestrel-diagnostic-v3` through `v6`) failed before the R5 business boundary with TLS handshake `unexpected EOF`. The isolated shared-harness health runs `FR034` (`shared-tls-positive-control`) and `FR033` (`shared-tls-diagnostic-v2`) reproduced that EOF before any business route. A valid local development certificate was then selected for the **same** Kestrel harness; `a3-r2r6-shared-tls-devcert-diagnostic.trx` and `a3-r2r6-shared-tls-final-positive.trx` each passed 1/1, followed by the later HTTPS/Agent cluster controls and final 27/27 gate. All seven failed TLS/reachability runs are `EXCLUDED_DIAGNOSTIC`, not product mutation evidence; the local dev certificate is no production TLS or authentication claim.

The remaining exclusions are individually named in `FR004`, `FR008`, `FR014`, `FR016`–`FR019`, `FR028`–`FR030`, `FR035`–`FR042`, `FR044`–`FR045`, and `FR047`. They include the S3 credential/setup error, invalid disposable-database purpose, O19/phase-c/terminal-unknown pre-correction observations, TH3 timing expectation, TH7 gate/image/response-handler diagnostics, TH8 pre-provider key-row assumption, and PostgreSQL bootstrap failures in TH9 and the five-case TI/R3 restored attempt. Some exposed real product correction work; **none was an isolated accepted mutation RED**. The first exception and exclusion cause are retained for each file instead of being collapsed into a generic fixture label.

The packet is now a single corrected cluster handoff for independent review of BP16/BP11. Official census remains **63**; candidate ledger remains **61**. No full suite, stage, commit, push, landing or production activation follows from this audit.

## Machine checks and drift

```text
manifest_files_total=1741 (Server 1593 + Agent 148)
ledger_matrix_and_audit_trx_references_distinct=508
referenced_trx_existing=508
referenced_trx_missing=0
referenced_trx_ambiguous=0
referenced_trx_in_manifest=508
referenced_trx_omitted=0
recorded_mutants=68 unique_mutants=68 bad_hashes=0 duplicates=0
live_mutant_matches=0 (Git-visible live-file sweep, both repos)
interim_manifest_to_candidate: added=79 modified=8 deleted=0
staged=0 conflicted=0 (both repos)
```

The 79 additions are the prior 34 plus 42 previously unreferenced cluster TRX and the three audit files (all-run inventory, failed-run census, read-only verifier). The eight modified files remain the manifest verifier, mutant registry, TRX-resolution inventory, cluster matrix, internal interim packet, reconciliation ledger and two test sources. **No product or test source was changed in this correction**; the two test-source differences are inherited from the preceding cluster candidate. The final packet is a file in the manifest; the manifest and SHA sidecar intentionally exclude their own bytes to avoid self-reference. No full suite, stage, commit, push or production activation was performed.

The audit files are pinned: all-run inventory SHA-256 `012DE84ECAE723CEEE6052713D2E06B5A2002D13AA800126A74E6EFDB369CCBA`; failed-run census SHA-256 `B0A6AB11B33F0E1B72534B4C8DDF099F093D59E3B3F4B87D80F825DE95441992`; read-only failed-run verifier SHA-256 `A6898637DBA1B6FCA45D194E9EECD8781F5F23F7204D3066E11BCF620F14050A`. The exact final manifest SHA is in its sidecar. Raw output from the failed-run verifier on the corrected candidate:

```text
cluster_trx_total=109
failed_runs_total=48
evidence_red=11
superseded_red=9
excluded_diagnostic=28
classified_failed_runs=48
unclassified_failed_runs=0
failed_runs_in_manifest=48
missing_failed_trx=0
live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

Requested independent review: verify manifest/sidecar, exact 20-row disposition and official-vs-candidate census, current test/source hashes, target assertion/stack for each accepted RED, ineffective GREEN exclusion, final 27 identities including real child work, and the named OPEN/blocker boundaries. Ratify BP16 and assess BP11 only if those exact claims survive; do not infer ratification of any other row. A3 remains HOLD.
