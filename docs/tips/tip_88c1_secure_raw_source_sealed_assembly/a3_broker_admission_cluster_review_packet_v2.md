# A3 BROKER_ADMISSION P02 successor review packet v2

## Homeowner disposition

- P02 `A3_S02_O02_ExactOutcomeShapeStatusAndResidue`: **RATIFIED / IMPLEMENTED** on the v2 evidence.
- P07: **OPEN**. The v2 positive path and outcome/custody evidence remain valid components, but no current mutation directly discriminates the multi-source `effective_expires` calculation.
- Keep P03/P04/P05 open: no production emitter exists in the current broker graph.
- Keep P06 open: actual-first-excess has component proof, but declared-excess-before-body remains unreachable because production A3 admission is not supplied the ratified per-class maxima.
- Official incoming census before this Homeowner decision: **57 = 56 normative + 1 seam**. Official census after P02 only: **56 = 55 normative + 1 seam**. A3 remains HOLD.

## Corrections from v1 review

The shared fixture now uses the production-valid capacity tuple `(1, 2, 2_097_152, 1_048_576)` in constructor order `(perProducer, perDeployment, aggregatePerDeployment, perStream)`, with the owner per-stream value also `1_048_576`. Production would accept this tuple; the v1 reversed aggregate/per-stream tuple is no longer used by current-source evidence.

P02 now covers both absence and nondisclosure. `P02-missing` uses an absent artifact. `P02-foreign` creates a complete real graph for a different client, including consent, capability, binding and capture acceptance, and then sends the foreign artifact ID through loopback Kestrel/TLS and the production Agent client under the caller client's context. The direct production reader resolves one row for the foreign owner and zero rows for the caller/foreign pair. Both public responses are exact 403/O02 outcome-only results, read zero request-body bytes, never enter the body pipeline, preserve the caller and foreign durable/provider snapshots byte-for-byte, and contain neither the foreign artifact nor binding identifier.

The only authentication double supplies an already-authenticated caller. Authentication is not claimed. The Server endpoint, admission service, broker façade, SQL reader, result mapper, HTTPS transport and Agent client are production code.

## Current-source machine evidence

| Artifact | SHA-256 | Counter / meaning |
|---|---|---|
| Server baseline `a3-broker-admission-v2-final-source-baseline.trx` | `5AFDABFB05F6AF9231F2619EECEEC126159BF5192BE35CCD660D45930DC4A0B7` | 3/3 PASS |
| BA-G1 `a3-broker-admission-v2-binding-final-source-red.trx` | `316417D7FBF61EDC72B50936CC46669008E461776126AA11EFFAC71EA50AB712` | 1 pass / 2 intended fails; both P02 variants become O05 |
| BA-G2 `a3-broker-admission-v2-retention-final-source-red.trx` | `0DEC3D57F440C0730210B7ACBF7AD89F27860C999F9B7602A539CB52FC7DF01D` | 2 pass / 1 intended fail; only P07 changes O07→O15 |
| BA-G4 `a3-broker-admission-v2-foreign-artifact-final-source-red.trx` | `AADCE3B718D2688446ADEC692302904B7CF1562BCD816004822DB33193EFAE09` | 1 pass / 2 intended fails; foreign isolation reader changes 0→1 and missing-artifact control also leaves O02 |
| Server restored `a3-broker-admission-v2-server-final-restored.trx` | `E57994AA3143DE85F08444BA382BB397AB1CC437D46FA174603AE8820291CD59` | 3/3 PASS |
| Frozen Agent baseline | `F63550A49EC862AE0C829483A6861DAAE4F0D6663D50F5E6D63AC20512C83152` | 2/2 PASS |
| Frozen BA-G3 Agent RED | `21E28017A80BD45AA96B50537A34894AD943804CCDADED12AB5A21544C731427` | 0/2; terminal-classification guard removed |
| Frozen Agent restored | `A73BDC0CE67272F54F85A68BB29333B5CB9768848CF58854A8E9CEDAAC4813F8` | 2/2 PASS |
| Frozen predecessor sentinel | `0AF30DA75DA0DA9E1961250F99888D8BF64F157983AC74EE35694F65B6C916C6` | 14/14 PASS on predecessor bytes; retained as historical coverage, not claimed as a current-source regression sentinel |

Mutants: BA-G1 `B51215EFB3790F4B0463556A007F934C9238536B0FA2D42FB3017D54142C318F`; BA-G2 `5E051208C93475C6A9D49CA9CDED9E59F6626798D993CD0BE3F61CA119F5A191`; BA-G3 `EC50A2EBBAD831975E566573A0435B339183D12445ED2F0CE5BFB39C455A2D56`; BA-G4 `5E334B42F31038E6918FD8B7A7A22B6347A62DC1EAE228F2AB5F35D928F4C9C2`.

Restored production is byte-exact: façade `8ADCA9B698B0E738B641BE281686669C2E79F47CFC191EE3C30249AFE2CB5C09`; migration `4FE0E4C7FECCE210C8DD34BA9066E97BF776F69FA3E752B529F779E2550EA7E4`; Agent owner `3546F6438DB07D407FB70E83F92983FE6B301034B8CA61738F58CA0AFAEE3D33`.

Current test/support sources: Server cluster test `F6314AAE434EDCAF65F8C28C01BA3E61A79A3AB6447F4D5B23072FE6191D46AE`; retention checkpoint support `6FB2F448815139FB8D78C591E48686D15DA88C33329E9CAABA979514FFD37B88`; consent support `8921EB834528F4E8CF404D81AA0BABC9B644794D5716594C5471562D3734FFD5`; Agent retained-owner test remains the frozen v1 source.

## Failed-run provenance

The failed-run census contains **12** classified failed TRX: **4 EVIDENCE_RED, 5 SUPERSEDED_RED and 3 EXCLUDED_DIAGNOSTIC**, with zero unclassified. The excluded runs are the invalid disposable-database purpose, the missing foreign-client consent/authority seed, and an accidentally combined adjacent fingerprint mutation. Earlier valid RED runs whose test locator predates the final source are retained as superseded and point to unique current-source successors.

BA-G2 is described only as an **outcome-branch mutation**. It does not directly mutate the expiry predicate. The v2 P07 path proves one rejecting input and its durable residue, while the frozen broker success case proves one broad-budget allowing input. Neither independently pins the calculation that takes `LEAST` across up to five sources, branches on `AuthorityKind`, then compares the result with deadline plus safety margin. P07 therefore remains open rather than being described as ratified.

## Scope and census

P03–P05 have no production producer; P06 has an actual-excess producer but lacks the declared pre-body admission input. Those are distinct from the ASSEMBLY readiness/caller gap: the ASSEMBLY subsystem exists and has component evidence but lacks a production caller. No synthetic Final is counted as producer proof in any of these families.

No full suite, stage, commit, push, production activation or A3 PASS is claimed. The Homeowner authorizes exactly one closure from this packet: P02.

## Final machine audit

```text
predecessor_manifest_files=1817
candidate_manifest_files=1835
added_paths=18
modified_paths=9
deleted_paths=0
candidate_trx_total=1044
cluster_failed_runs_total=12
evidence_red=4
superseded_red=5
excluded_diagnostic_failed=3
classified_failed_runs=12
unclassified_failed_runs=0
referenced_trx_existing=559
referenced_trx_missing=0
recorded_mutants=79
live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

The drift table has 17 added and 9 modified rows: it intentionally excludes only itself and the successor manifest/sidecar to avoid self-reference. Raw manifest set-diff is 18 added and 9 modified because it also counts the drift table itself. The frozen v1 manifest and sidecar appear as ordinary predecessor artifacts in the successor manifest.
