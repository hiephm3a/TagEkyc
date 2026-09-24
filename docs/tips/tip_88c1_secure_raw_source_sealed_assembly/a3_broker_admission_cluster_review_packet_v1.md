# A3 BROKER_ADMISSION P02–P07 cluster review packet v1

## Requested disposition

- Ratify P02 and P07 as `IMPLEMENTED`.
- Keep P03/P04/P05 open: no production emitter exists in the current broker graph.
- Keep P06 open: actual-first-excess is already component-proven, but declared-excess-before-body is unreachable because production A3 admission is not supplied the ratified per-class maxima.
- Official incoming census: **57**. Candidate census after P02/P07: **55 = 54 normative + 1 seam**. A3 remains HOLD.

## Proof shape

The Server proof uses loopback Kestrel/TLS, production Agent HTTP client, public Server endpoint/mapper, production admission and broker façade, and isolated PostgreSQL. The only double supplies an already-authenticated caller; authentication is not claimed. The body is wrapped independently and the body pipeline rejects any call.

P02 returns exact HTTP 403/O02 before body access and leaves snapshot, alias, claim, reservation, head, attempt, provider-key and provider-object rows byte-text equal. P07 returns exact HTTP 422/O07 before body access, preserves an anti-reuse alias/claim (`Evaluating`/`Completed`, `ClaimEvaluating`), creates no reservation and no provider rows. The Agent owner proof separately establishes that both terminal results zero/dispose the owned lease, return `RECAPTURE_REQUIRED` on a second call and send no second body.

## Machine evidence

| Artifact | SHA-256 | Counter |
|---|---|---|
| Server baseline `a3-broker-admission-server-baseline.trx` | `DB48CADB3356C082E0183B2854A625C2BCD20446238021E9F8E2ACB6DE86DC2E` | 2/2 |
| BA-G1 RED `a3-broker-admission-binding-red.trx` | `6F3BEE4E281D5C57EF745BDA3B2924531F59437AEF908C83C5C41660E9842372` | 1 pass / 1 fail |
| BA-G2 RED `a3-broker-admission-retention-red.trx` | `18F6F538C3EFDF5C44710CEE683635B39E9C9CC835BACE8B00B23862BC14A093` | 1 pass / 1 fail |
| Server restored `a3-broker-admission-server-final-restored.trx` | `685EE9F800F9E3659FBFA2091857BD09D5EC7E77281F2E540A95BB91636E843A` | 2/2 |
| Agent baseline `a3-broker-admission-agent-baseline.trx` | `F63550A49EC862AE0C829483A6861DAAE4F0D6663D50F5E6D63AC20512C83152` | 2/2 |
| BA-G3 RED `a3-broker-admission-agent-terminal-red.trx` | `21E28017A80BD45AA96B50537A34894AD943804CCDADED12AB5A21544C731427` | 0 pass / 2 fail |
| Agent restored `a3-broker-admission-agent-final-restored.trx` | `A73BDC0CE67272F54F85A68BB29333B5CB9768848CF58854A8E9CEDAAC4813F8` | 2/2 |
| Ratified affected-source sentinel `a3-broker-admission-ratified-sentinel.trx` | `0AF30DA75DA0DA9E1961250F99888D8BF64F157983AC74EE35694F65B6C916C6` | 14/14 |

Mutants: BA-G1 `B51215EFB3790F4B0463556A007F934C9238536B0FA2D42FB3017D54142C318F`; BA-G2 `5E051208C93475C6A9D49CA9CDED9E59F6626798D993CD0BE3F61CA119F5A191`; BA-G3 `EC50A2EBBAD831975E566573A0435B339183D12445ED2F0CE5BFB39C455A2D56`.

Restored production: façade `8ADCA9B698B0E738B641BE281686669C2E79F47CFC191EE3C30249AFE2CB5C09`; migration `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4`; Agent owner `3546F6438DB07D407FB70E83F92983FE6B301034B8CA61738F58CA0AFAEE3D33`.

Final test sources: Server `7101B0261A8FE275234A5249C5E65B0AA4B6DBEB6BA44B5CC9D9E280F095C486`; Agent `EC25461C62E4C5CCF560962E42A9A52E9D1D9205CC4FE92A1DC44A7EB2C9A486`.

Failed-run census contains exactly 3 classified `EVIDENCE_RED`, 0 superseded, 0 excluded, 0 unclassified. No full suite ran; no stage, commit, push or product activation occurred.

The first discovery attempt used the wrong MSBuild property and produced `a3-broker-admission-baseline.trx` SHA-256 `94D82533DCC23588D21E76DE0EF5BECC668BBD4564BA2B1A4B10FA72D7224B2D`, Completed with 0/0 tests. It is retained in the manifest as a non-evidence diagnostic; the A3 opt-in graph was then restored with both documented acceptance properties before any accepted baseline or mutation run.

## Final machine audit

```text
baseline_manifest_files=1794
candidate_manifest_files=1817
added_paths=23
modified_paths=6
deleted_paths=0
candidate_trx_total=1032
cluster_failed_runs_total=3
evidence_red=3
superseded_red=0
excluded_diagnostic_failed=0
classified_failed_runs=3
unclassified_failed_runs=0
all_repo_failed_runs=399
all_repo_failed_runs_unrecorded=0
referenced_trx_existing=548
referenced_trx_missing=0
recorded_mutants=78
live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

The delta counters include the newly added drift ledger itself. Its row list intentionally excludes that self-referential file (and candidate manifest/sidecar); every other added/modified/deleted path is recorded with before/after SHA where applicable.
