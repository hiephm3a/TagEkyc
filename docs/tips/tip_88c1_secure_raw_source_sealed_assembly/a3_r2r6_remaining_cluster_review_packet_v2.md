# A3 R2–R6 remaining-cluster review packet v2

Status: **ONE FROZEN BUILDER CANDIDATE / INDEPENDENT REVIEW REQUIRED**. This packet is the sole handoff for the remaining R2–R6 execution requested after the accepted BROKER_ADMISSION v3 freeze. It is not A3 PASS, does not authorize production activation, and does not revise a row merely because a related test is green.

Incoming official census is **56 open = 55 normative + 1 seam**. Five exact P rows are marked candidate `IMPLEMENTED` in the working reconciliation ledger; therefore the mechanical candidate census is **51 open = 50 normative + 1 seam**. Official census remains 56 until independent ratification.

## 1. Frozen scope and current bytes

No product or test source was permanently edited in this successor. Every change to product/Agent source below was an isolated mutation, followed by byte-exact restoration and rebuild. The current source freeze is:

| File | SHA-256 |
| --- | --- |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2R6ClusterHttpTests.cs` | `0588D3FCCDFA8E77B296FCF6C7B8A013E724F4D03D975DC3DA8B947F9E1EC4A3` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `AC6721EC2C6DAF8C18E759E3AFE6C4954C3B3128C5FA23EC1B6CEDAAA457CFD8` |
| `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RetainedOwnershipTests.cs` | `EC25461C62E4C5CCF560962E42A9A52E9D1D9205CC4FE92A1DC44A7EB2C9A486` |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2` |
| `src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs` | `87F6741623601E1B6150F7DB025BEC5A37A6D0ADC1650063838CD3BA14D55A63` |
| A3 retained-ingress migration | `34199F72C5261A6E70ADB35C226D9AE8BB78353CCA3469274FE82DE6DFA942A3` |
| Agent `RawExportRetainedSubmissionOwner.cs` | `3546F6438DB07D407FB70E83F92983FE6B301034B8CA61738F58CA0AFAEE3D33` |

The rebuilt current-byte baseline `a3-r2r6-current-source-baseline-joined.trx`, SHA-256 `67D556B33B6F94CA80706D86746B15D9AF61BE664477D012EB4E9F66E3B6A88E`, passed **27/27**, zero fail/skip. It was required because the accepted P07 comparator correction changed the B-C migration after the earlier R2–R6 freeze.

## 2. Exact disposition of every still-open R2–R6 row

The execution instruction used ten shorthand labels; `BP20` expands to four table rows. Together with the two previously dispositioned open R2–R6 rows, this packet accounts for all fifteen rows that were still open in this mechanism group.

### Candidate `IMPLEMENTED`, pending review (five)

| Row | Joined production evidence | Discriminating guard evidence |
| --- | --- | --- |
| P17 / O17 `AlreadyAvailable` | Real loopback Kestrel/TLS → production Agent replay after R5 or receipt loss; original descriptor, zero replay body/key/object/provider work, cleanup completed | G-H async Final-body read makes the replay read count RED; G-P prevents premature paired-state projection |
| P18 / O18 `TemporarilyUnavailable` | Actual KEK unwrap failure on R2; exact O18 over Kestrel/Agent, retry timing and zero retry body | G-U maps the encryption failure to O20 and makes only that case RED (8 controls GREEN) |
| P19 / O19 `ContentCommitmentMismatch` | One-byte in-transit corruption; production verifier/TI02; exact 422/O19, Deleted object, Revoked key and bodyless replay | G-T broadens the exact cause and makes the different-cause positive-absence negative RED at `StateConflict` versus `Terminated` |
| P20 / O20 `RecaptureRequired` | Durable ciphertext tamper is read from MinIO, not request memory; exact O20, Deleted/Revoked residue and bodyless terminal replay; Agent expired lease retires locally | Current gate retains the previously discriminating ciphertext control; bypassing the Agent expiry guard makes both local no-call cases RED |
| P22 / O22 `Available` | Real R2–R5 and settled same-owner successor; response waits for R5 Available commit, old attempt bytes/horizon preserved, receipt-loss replay becomes O17 | G-P paired state, G-L cleanup dispatch and G-H bodylessness all RED independently |

### OPEN with an exact missing dimension (ten)

| Row | Disposition |
| --- | --- |
| BP12 `CleanupSurvivesHttpReceiptLoss` | OPEN: independent-scope cleanup and replay are proven, but the row still lacks one same-source real process-death → durable pending-cleanup → recovery join. Cancellation/response loss is not process death. |
| BP17 `ProviderUnknownDoesNotInventInputMismatch` | SEMANTIC/PRODUCT BLOCKER unchanged: unknown provider evidence must not be collapsed into either absence or a stronger input-mismatch claim without authority. |
| BP19 `AllStageEnumMembersHaveExactProjection` | SEMANTIC/PRODUCT BLOCKER unchanged: declaration census is green, but unknown numeric projection is not a ratified stage result and cannot be invented by the test. |
| BP20 `TransientO18SettlesThenSameOwnerRetryReachesAvailable` | OPEN: O18 settlement and successor-to-Available exist, but not as the required single-source end-to-end join. |
| BP20 `ReentryCannotRaceStageOrTerminalIntent` | OPEN: RE01, R3 and TI01 controls exist; the required two-order same-head race is absent. |
| BP20 `TerminalOrDifferentOwnerCannotReenter` | OPEN: terminal/different-owner/binding/head negatives are components, not one exact mutation-sensitive matrix. |
| BP20 `ExpiredTransientAttemptFinalizesRecaptureWithoutHistoryRewrite` | OPEN: expiry/O20/old-row equality components are not joined on one source. |
| BP21 `LostR1ResponseBeforeKeyRecoversWithoutMaterial` | OPEN: NPS01/RE01 and successor R2–R5 components exist, but the same-source real lost-response/process-death join is absent. |
| P21 / O21 `ResumePending` | OPEN: cancellation-at-Committed produces exact O21 and later O17, but no current guard mutation independently discriminates O21's pending continuation from adjacent state projections. |
| P23 / O23 `EncryptionFailed` no-egress | OPEN: actual encryption failure reaches safe O18/O16 and never leaks O23, but exact safe terminal settlement/recovery alternatives for the internal O23 cause remain incomplete. The aggregate All14 no-egress row is not a substitute. |

Previously ratified R2–R6 rows (including BP11, BP16, BP17 ciphertext, BP18 and BP21 provider evidence) were not reopened. They appear in the current 27-case gate only as affected-source sentinels where applicable.

## 3. Current-source mutations

All mutations were isolated; no two were stacked.

| Guard | Mutant source SHA-256 | Raw RED | Exact result |
| --- | --- | --- | --- |
| G-U encryption failure projection | `E5B07ADE29F6A213654824429FBAB0BFFFA19A445CD036E119465CC8F3DD8253` | `a3-r2r6-gu-encryption-projection-current-red.trx` `16159FCC6F1F18CFED69AC2B52E3DD9FB55F952EB9971EFF938D81A74F978D9D` | **8 pass / 1 fail**; only `encryptionFailure:true`, expected O18, actual O20 |
| G-P R4/R5 paired state | `378B2DDC36BFD1C71598CDA07F3C669C9853002F20F67FE5F8174D1393389757` | `a3-r2r6-gp-agent-committed-current-red-v2.trx` `0211613128269A932C06A7E20C2600303EBDCFD8BE60F0FEBDDAD16C7C545D37` | **0/1** at the R5 waiter, expected pending response but projection completed early |
| G-T exact O19 terminal cause | `5BA4353535D8C36ACC6982C5BB1A35D10B2721F0948FF84A16D77A43F2D636D2` | `a3-r2r6-gt-o19-current-wrong-cause-red-v2.trx` `4C089F600EAD1D2733A16E0971635A1D808B1328CE1B1EF3D5D6C472EE0576A9` | **0/1**, expected `StateConflict`, actual `Terminated` for a different cause |
| Agent expired-lease local no-call | `AD46FAE7684976A42D74F6E8F69ABA53B1F48CD8BD0890F8F724DB114D1EA749` | `a3-r2r6-agent-expired-lease-current-red.trx` `1DA0D77AA511878224DECC5602F2157A436467C4CF2AFC5E50E211E6F4BBB679` | **0/2**, both cases fail because the required exception is no longer thrown |
| G-L independent cleanup dispatch | `AABD39059C1DF8F3F58570E0DCFDD72DA872641FFF0A8ADD791C8E89FEF9FF99` | `a3-r2r6-gl-skip-cleanup-current-red.trx` `3546A28C338546C4FC9E2154BBD14C3793CB667C0748D41A4CAF8839517BC028` | **0/1**, Pending never reaches Completed after receipt loss |
| G-H async Final-body read | `1F53717D475D03906466B855B616B42E0BA04884CCFCD45134223859E867F600` | `a3-r2r6-gh-final-body-read-current-red-v2.trx` `6CB37EB86A95247B7C16E56ED035D9378294076B40D9DD4C8724BA7A7B1298F3` | **0/1**, expected two server reads, actual three |

One earlier G-H mutant used a synchronous body read. `a3-r2r6-gh-final-body-read-current-red.trx` SHA-256 `D757C0751751F1FEA81E6FA514067CBB458109FC7DF2AD9DAEDA13CAA7E68AB7` failed through the ASP.NET synchronous-I/O guard and is `SUPERSEDED_RED`; it is not proof for G-H. The successor async mutant above reaches the intended read counter.

Two non-evidence invocations are retained by name: `a3-r2r6-gp-agent-committed-current-red.trx` executed zero tests because of a bad filter; `a3-r2r6-gt-o19-current-wrong-cause-red.trx` was green because it selected the broad negative-family method rather than the exact different-cause assertion. Neither is counted as RED evidence.

The successor failed-run census is `a3_r2r6_remaining_failed_run_census.tsv`, SHA-256 `83D3CFC6E6BAFE27DD69DD9CD45B43AAB8F45A54B90D51052E7BFD9D403E42E2`: **6 EVIDENCE_RED + 1 SUPERSEDED_RED = 7**, unclassified 0. Historical R2–R6 failed-run census remains frozen and is not rewritten.

## 4. Restoration and joined gate

After every source was restored, the Agent focused gate `a3-r2r6-agent-expired-lease-current-restored.trx`, SHA-256 `53A4DE9C9A4325E39D3C3457FD767FC546DB92C8FA387DE0BAE054EBBFDDD934`, passed **2/2**.

The first restored Server gate `a3-r2r6-remaining-cluster-restored-joined-final.trx`, SHA-256 `7217D79CA809A90C0C3AA953B3219982C7393312146F1A2DB8EB77A30F854012`, passed 27/27 but preceded the final G-L/G-H repins and is retained only as a predecessor. After those repins were restored and the candidate rebuilt, `a3-r2r6-remaining-cluster-restored-joined-final-v2.trx`, SHA-256 `2D94F9830E1A9156B3D0001562AA49B2DF4D65010E360C347728A324C4E6F092`, passed **27/27**, zero fail/skip, in 9m17s. It includes all nine Kestrel/Agent scenarios, RE01 family, O19 negative family, stage census, the shared process-kill matrix, BP16/BP17/BP18/BP21 sentinels and provider-evidence cases.

This is a focused cluster gate, not a full suite. No stage, commit, push, production credential selection or activation occurred. A3 remains HOLD.

## 5. Mechanical freeze

The successor manifest was built only after this packet and the reconciliation ledger were frozen. Its expected machine result is **1,863 files** (1,710 Server + 153 Agent), **578/578** distinct referenced TRX present/in-manifest, zero missing/ambiguous/omitted. Against accepted BROKER_ADMISSION manifest v3 the declared drift is **18 added / 4 modified / 0 deleted**: three successor documents, the predecessor manifest+sidecar, thirteen Server/Agent TRX, the cumulative registry/verifier/reference inventory and this reconciliation ledger. There is no permanent product/test source delta.

The cumulative registry contains **86 distinct mutant hashes**, duplicates zero; the successor-manifest hash join has **0 live mutant matches**. The successor failed-run delta is completely classified, and every failed successor TRX is inside the manifest; `outside_name_family_failed=0` because all seven use the shared `a3-r2r6-` family. Git counters are `staged=0, conflicted=0` in both repositories. Both worktrees remain intentionally dirty with prior unstaged/untracked evidence and no claim of cleanliness.

Any nonzero missing TRX, live-mutant match, staged file or conflict invalidates this handoff.
