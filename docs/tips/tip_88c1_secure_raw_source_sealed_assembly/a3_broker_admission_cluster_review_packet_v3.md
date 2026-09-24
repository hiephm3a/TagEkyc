# A3 BROKER_ADMISSION P07 strict-boundary successor packet v3

## Disposition

- P02 remains ratified `IMPLEMENTED`.
- P07 remains `OPEN`; this packet closes only the strict comparator defect.
- Official census remains **55 normative + 1 seam = 56**. A3 remains HOLD.

## Product correction and proof

Fresh completion requires effective remaining retention to be strictly greater than encryption-attempt deadline plus safety margin. Equality and one microsecond short fail closed. `RetainedCompletionOperation` therefore uses `<=`; `RetainedCompletionRestore` is the historical `Down()` rollback definition and remains `<`. RE01 is a separate re-entry path and was not changed.

| Artifact | SHA-256 | Result |
|---|---|---|
| Pre-correction `a3-p07-boundary-baseline-red.trx` | `E66F8B0926D87D9C7C30306A59B998A8222D20A7DDF4D51A0775130F240F7AEA` | 3 pass / 1 fail; equality incorrectly returned `NewReservation` |
| Corrected `a3-p07-boundary-corrected-green.trx` | `DBF0D5273974B7174EF71C0E1DE15E0632142ADDFB57B5BD71F8FFB64E7D2826` | 4/4 PASS |
| BA-G5 `a3-p07-strict-comparator-mutant-red.trx` | `F2BCB9B4679F9A39610E5863B0858EDA9C730DEF28276FF420DE957E3CA6EE1C` | 3 pass / 1 fail; equality only |
| Restored `a3-p07-boundary-restored-green.trx` | `82F0795DE4C7A45FA026E498F120599FED3D53EE351F7B3FEAAFD59BA4D8B9D7` | 4/4 PASS |
| Current-source sentinel `a3-broker-admission-current-source-ratified-sentinel-v2.trx` | `9C7A002E2FCB11F22B0816432D212E7512704DB3ECFC3AAA027EE38DFC1B1D37` | 14/14 PASS, zero skip |

Current migration SHA-256: `34199F72C5261A6E70ADB35C226D9AE8BB78353CCA3469274FE82DE6DFA942A3`. Test source SHA-256: `3F110C80504CD2B11F0A69A1C6B0553E55767032E0EE73FBEF3A0075E0EEDF4F`. BA-G5 mutant SHA-256: `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4`.

The first sentinel invocation, `a3-broker-admission-current-source-ratified-sentinel.trx`, omitted `EnableCaptureAgentA3Acceptance=true` and executed only 7 controls. It is retained in the successor manifest as an excluded invocation diagnostic and is not evidence. The v2 invocation above is the current-source sentinel.

## Remaining P07 limit

The production-broker test pins the server-cap equality boundary and PostgreSQL interval precision at minus one microsecond, equality and plus one microsecond. It does not independently make each SourceRetention-specific member of the five-way `LEAST(...)` the unique minimum or remove each member in isolation. P07 therefore remains open.

No full suite, stage, commit, push, production activation or A3 PASS is claimed.

## Successor machine audit

```text
candidate_manifest_files=1845
referenced_trx_existing=565
referenced_trx_missing=0
referenced_trx_omitted=0
recorded_mutants=80
distinct_mutants=80
live_mutant_matches=0
cluster_failed_runs_total=14
evidence_red=5
superseded_red=5
excluded_diagnostic=4
unclassified_failed_runs=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```
