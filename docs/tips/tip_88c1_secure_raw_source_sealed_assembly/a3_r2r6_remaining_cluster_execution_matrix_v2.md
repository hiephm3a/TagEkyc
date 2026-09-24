# A3 R2–R6 remaining cluster execution matrix v2

This is the successor execution inventory for the remaining R2–R6 work after the accepted BROKER_ADMISSION v3 freeze. It is not a review packet and does not change the official census. Incoming official state is **56 open = 55 normative + 1 seam**; A3 remains HOLD.

## 1. Current-byte baseline

The affected-source rule requires one current-byte joined baseline because the fresh-complete B-C migration changed for the P07 strict-boundary correction. The exact historical 27-case R2–R6 filter was rebuilt and rerun once:

- `tests/TagEkyc.IntegrationTests/TestResults/a3-r2r6-current-source-baseline-joined.trx`
- SHA-256 `67D556B33B6F94CA80706D86746B15D9AF61BE664477D012EB4E9F66E3B6A88E`
- **27/27 PASS**, zero fail/skip, duration 9m27s.

Pinned current sources before any successor edit or mutation:

| File | SHA-256 |
| --- | --- |
| `Tip88C1C6BA3R2R6ClusterHttpTests.cs` | `0588D3FCCDFA8E77B296FCF6C7B8A013E724F4D03D975DC3DA8B947F9E1EC4A3` |
| `Tip88C1C6BA3R2TerminalProjectionTests.cs` | `AC6721EC2C6DAF8C18E759E3AFE6C4954C3B3128C5FA23EC1B6CEDAAA457CFD8` |
| `RawExportSourceIngressServiceRegistration.cs` | `D61D21B836027EA5149CEDFF25BD9A220E9CF1C31F1E6261B2150DDBCF945AAF` |
| `CaptureRuntimeSourcePipeline.cs` | `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2` |
| A3 retained-ingress migration | `34199F72C5261A6E70ADB35C226D9AE8BB78353CCA3469274FE82DE6DFA942A3` |

## 2. Exact remaining inventory

The shorthand instruction names ten labels. `BP20` expands to four distinct normative ledger rows, so the exact table inventory is thirteen rows. No row is silently merged for census purposes.

Two other R2–R6 rows remain officially open but were already dispositioned before this successor worklist: BP17 `ProviderUnknownDoesNotInventInputMismatch` remains a semantic/product blocker, and BP21 `LostR1ResponseBeforeKeyRecoversWithoutMaterial` retains bounded components without its same-source process-death join. They receive unchanged entries in the final cluster packet; neither is silently closed or removed from the global census.

| Label / exact ledger row | Shared scenarios | Required guard(s) | Candidate condition |
| --- | --- | --- | --- |
| BP12 `CleanupSurvivesHttpReceiptLoss` | TH7 + shared process-kill | G-L, G-H | same source must join real process death, durable pending cleanup, independent-scope completion and replay |
| BP19 `AllStageEnumMembersHaveExactProjection` | TH10 | G-M | every real producer/result shape plus unknown numeric fail-closed; current unknown-enum behavior is a semantic/product blocker |
| BP20 `TransientO18SettlesThenSameOwnerRetryReachesAvailable` | TH4 + TH8 | G-U, G-E, G-S, G-C, G-P | one source must join transient settlement to successor Available |
| BP20 `ReentryCannotRaceStageOrTerminalIntent` | TH8 + BP18 controls | G-E, G-S, G-T | same head contested by RE01 against R3/TI01, both lock orders |
| BP20 `TerminalOrDifferentOwnerCannotReenter` | TH5/TH6 + RE01 family | G-E, G-T | exact terminal/different-owner/binding/head negative family, mutation-sensitive |
| BP20 `ExpiredTransientAttemptFinalizesRecaptureWithoutHistoryRewrite` | TH4/TH6 + RE01 | G-E, G-T | original horizon expiry, O20 finalization and old-row byte equality in one source |
| P17 O17 `AlreadyAvailable` | TH2/TH7 | G-P, G-H | Kestrel/TLS + production Agent, original descriptor, zero body/key/object, released capacity |
| P18 O18 `TemporarilyUnavailable` | TH4/TH9 | G-U, G-T, G-H | reached transient producer, exact O18 wire/Agent, no false terminal, later reevaluation |
| P19 O19 `ContentCommitmentMismatch` | TH5 | G-U, G-T, G-H, G-O19 | clean complete mismatch, exact first public 422/O19, settlement/residue and bodyless terminal replay |
| P20 O20 `RecaptureRequired` | TH6 | G-T, G-E, G-H, G-O20 | server terminal plus local expired/lost-buffer no-call, with exact residue |
| P21 O21 `ResumePending` | TH3 | G-V, G-S, G-C, G-P, G-H, G-O21 | complete durable ciphertext, no resend/restart, deterministic continuation |
| P22 O22 `Available` | TH1/TH3/TH7/TH8 | G-S, G-C, G-P, G-L, G-H | actual R5 head, exact response, later O17 and durable cleanup |
| P23 O23 `EncryptionFailed` internal/no-egress | TH9 | G-U, G-T, G-H | actual encryption failure, safe recovery to O18/O20 and no O23 wire leak |

## 3. Horizontal scenario matrix

| Scenario | Production path / host | Body expectation | Expected result | Rows carried | Current disposition before successor mutations |
| --- | --- | --- | --- | --- | --- |
| TH1 publication fence | Kestrel TLS → production Agent → broker/R2–R5 | first body once | no response before R5 commit; O22 after commit | P22 | PASS in current 27-case baseline; G-P current-byte RED still required for row closure |
| TH2 published replay | same host/client on durable Available | zero replay reads | O17 with original descriptor | P17/P22 | PASS; historical G-H RED exists on unchanged admission source |
| TH3 lost in-flight request | Agent cancellation after R4 Committed; worker continuation | zero retry reads | O21, later O17 after Available | P21/P22 | PASS component; cancellation is not process death |
| TH4 transient failure | actual AEAD failure and reconciliation | no retry body | O18 then reevaluation, no invented terminal | P18/P23/BP20 | PASS component; provider-unknown semantics remains blocked |
| TH5 clean mismatch | in-transit byte corruption, real R2/TI02, Kestrel Agent | first body once; terminal replay zero | exact first-response 422/O19 | P19/BP20 | PASS after exact assertion; G-O19 current-source projection RED and G-T exact-cause RED |
| TH6 recapture | persisted ciphertext tamper, real cleanup/finalization | terminal replay zero | 409/O20 | P20/BP20 | PASS component; local expired-lease control remains separate |
| TH7 receipt loss and cleanup | response dropped after R5, worker in independent scopes | replay zero | cleanup Completed then O17 | BP12/P17/P22 | PASS component; not same-source process death |
| TH8 settled reentry | lost R1, NPS01/RE01, successor R2–R5 | successor body once | O22 | BP20/P22 | PASS component; process death and RE01-vs-stage race remain open |
| TH9 encryption failure/no-egress | real KEK unwrap failure → R2 recovery → public Agent | bounded first read; retry zero | O18/O16, never O23 wire | P18/P23 | PASS component; current-byte reached guard mutation required |
| TH10 projection | production enum declarations plus reachable producers | state-dependent | exact O18/O21/O22 or fail-closed | BP19/P18/P21/P22 | declaration census PASS; unknown numeric projection remains blocker |

## 4. Batch rule and stop conditions

Work remains cluster-atomic: finish shared test/product edits first, build once, run positive controls by mechanism, mutate by shared guard, restore exact bytes, rebuild once, run one joined restored gate and ratified sentinels, then generate one final R2–R6 packet. No interim row packet is permitted.

The following do not block independent scenarios but must remain explicit in the final disposition:

- BP19 unknown numeric/result semantics;
- provider-unknown O18/O20 semantics;
- BP12 same-source real process-death join;
- BP20 joins that still combine only separate sources.

Any regression in a ratified row, an un-restored mutant, undeclared product drift, or a newly discovered authority conflict is a STOP. No full suite, stage, commit, push or production activation is authorized.

## 5. Independent-review correction

The v2 review exposed two proof gaps without reopening the cluster. P19's test had allowed first-response O18 before terminal O19; it now requires exact first-response O19, while retaining the same terminal residue and bodyless replay assertions. G-O19 changes only the public terminal O19 projection to O18 and must make only the clean-mismatch scenario RED. P20's durable-ciphertext distinction is repinned on current bytes by G-O20, changing only the verifier-failure intent cause from O20 to O19; the tampered case must RED while the untampered control remains GREEN. Both mutations are restored before the successor joined gate.

The five earlier BP rows called “previously ratified” in the packet were already removed from the incoming official census by their recorded independent ratification. Their table prose was stale and is corrected in the reconciliation ledger; they are not subtracted a second time. Incoming official 56 minus the five new candidate P rows remains candidate 51.

## 6. Macro-wave W1 final disposition

Wave 1 consumed the exact nine-row partition in one execution handoff. P21/O21 is `CANDIDATE_PENDING_RATIFICATION` after a current-source G-O21 mutation was 8 pass / 1 fail, only the durable-committed transport-loss scenario RED. Historical G-V remains the separate CP08 durable-object/verifier guard and is not replaced. BP19 remains a semantic/product blocker. BP12, all four BP20 rows, BP21 lost-R1 and P23 remain open with the named same-source crash/race/recovery joins in §2 still absent. No component-only evidence is promoted to row closure.

The byte-restored joined gate `a3-w1-r2r6-restored-joined-final.trx` passed 27/27, zero fail/skip. The sole handoff is `a3_macro_wave_w1_r2r6_review_packet_v1.md`; the scratch ledger, Wave-1 TRX inventory and Wave-1 failed-run census are machine-readable companions. Authority-open stays 52 until P21 is explicitly ratified.
