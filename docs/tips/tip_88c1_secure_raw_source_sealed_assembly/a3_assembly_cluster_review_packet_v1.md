# A3 ASSEMBLY P29–P36 cluster review packet v1

Status: `DOC-CORRECTED AFTER INDEPENDENT COMPONENT REVIEW — NO ROW CLOSURE`

Date: 2026-09-19

## Verdict requested

Accept the bounded component evidence for P29, P30, P32 and P33, and accept the blocker classification for P31, P34, P35 and P36. Do **not** ratify any P29–P36 row as `IMPLEMENTED`. Official and candidate census stay **57 open = 56 normative + 1 seam**. A3 remains HOLD.

## Preflight corrections applied

The second-barrier P29 scenario now inventories the actual durable order: pass-1 bindings freeze, source read/verification, authentication and `RegisterPreparingAsync`, then the second freeze. If authority disappears at that second barrier, C2 is never invoked, the bindings remain byte-equal and one durable `Preparing` disposition can remain; no assembly identity/item, `Pending`, `SealCommitted` or `Finalized` row is allowed.

The cluster is bound to `a3_assembly_allowed_delta_v1.tsv` and `a3_assembly_manifest_compare.ps1`. The final gate rejects additions, modifications or deletions outside the allowlist, any final production-source SHA drift, candidate omission of Git-visible/new-cluster-TRX files, or a live mutant. `a3_assembly_trx_verify.ps1` independently rejects baseline TRX drift, unclassified failed runs and failed filenames outside the cluster family.

P30 and P32 were explicitly downgraded to `TESTABLE_COMPONENT_NON_CLOSING`. P31 wording no longer infers downstream joined-candidate cardinality from the unique authoritative `(VerificationSessionId, RawClass)` selection.

The original preflight manifest of **1,781 files**, SHA-256 `B0E3B6C79F0F0952D58D95D659AE1212173A441725ED4C77A2D230B72F5E582`, was superseded before any component edit or mutation after the preflight-review corrections. The corrected frozen baseline is **1,785 files**, SHA-256 `E523206588471FA297A45EAFE50EE1A345CA73963DB114D6753FB2649494ADC7`. The exact candidate-manifest SHA is carried by the adjacent `a3_assembly_candidate_manifest.tsv.sha256` sidecar rather than embedded here, because the candidate manifest inventories this packet and an inline candidate hash would create a self-reference cycle.

## Exact component evidence

Production/test final SHA-256:

```text
RawExportAssemblyOrchestrator.cs=5DD6CD25AA92E924CAC0C9955E1F088DE572779F917CBE1F083101158F66573E
20260815120000_Tip88C1C1ResolverAssembly.cs=125EC8B98ED3F95B2504DA59A548C32814A61C497391E29138D4288A8FC5FBEF
Tip88C1C1ResolverAssemblyTests.cs=2059A871540A93D783418DFCD45572FE3CAACF418D426F58D08BF0D18C1F99E2
```

GREEN:

- `a3-assembly-component-baseline.trx`, SHA-256 `97B9D9501090AAEC8D1AD4B7EAA2636772AB1C23A410679E11FAE15962CE6160`, **5/5 PASS**.
- `a3-assembly-final-restored-joined.trx`, SHA-256 `4DD9EDDE23BAEB336A4690FEBBAE8FACFA179CE793F0579A6EF5219461333C59`, **5/5 PASS**, zero skip, after exact restoration and rebuild.

The five tests are:

1. `C102_missing_duplicate_or_mismatched_source_has_zero_bindings`: production repository and orchestrator both return broad `SourceUnavailable` with no binding, preparation, identity/item or C2 call.
2. `C104_frozen_binding_is_insert_once_and_exact_replay_only`: first freeze is `Frozen`, replay is `ExistingMatch`, and the JSON representation of every binding stays byte-text equal.
3. `C112_first_fresh_barrier_precedes_source_read`: withdrawn authority stops before object read, binding and preparation.
4. `C113_second_fresh_barrier_immediately_precedes_prepare`: withdrawal after pass 1 gives one object read, zero C2 prepare, byte-equal frozen bindings and exactly one durable `Preparing` row with no later disposition/identity/item.
5. `A3_ASSEMBLY_P33_SelectedCommittedSourceIsUnavailableWithoutProviderRead`: an actually selected source with a durable `Committed` publication is rejected before binding, object read and C2.

## Mutation evidence

`AS-G1` mutant SHA-256 `123B48BF0AE09721C7D744B8AA719A754D898037D4532F93C3B9E2BB19A8D0A5` removes only the second-barrier decision. `a3-assembly-g1-second-barrier-red.trx`, SHA-256 `9FC439D660948FC6E2EB55B363E1E425F5CF2352B83EE24C8F7FB6167FADC718`, is **1 PASS / 1 FAIL**: C112 stays green and C113 fails `Expected 1 / Actual 2` object reads. This establishes that the second barrier prevents the second pass/provider path after authority loss.

`AS-G2` mutant SHA-256 `149A9B3F6C40BFA692BDD9D0873816FAD7A2318705123CB5F4E8496879742EB1` changes only the first no-eligible-source return from `SourceUnavailable` to `BindingConflict`. `a3-assembly-g2-shared-unavailable-red.trx`, SHA-256 `4219403DFA105725E80E88B612C949914AF38C811E0CB40162F2F52F6709E001`, is **0/2**: both P30 no-selection and P33 selected-but-Committed fail with the same expected/actual pair. This proves the common gate is load-bearing and simultaneously confirms the present surface does not provide distinct P30/P33 business codes.

`AS-G3` coarse mutant SHA-256 `E316FA55AFDD31EE2C4D7803627BC200ED059B30D53722CD2E4515CCF03DDF42` made the initial freeze conflict; its RED is `SUPERSEDED_RED`. The narrower successor SHA-256 `A55F2DD3A5A0C0BE100A9005F6C8C846E78A6F06D73E10BC3021A505B990F11C` changes only replay handling. `a3-assembly-g3-fingerprint-replay-red-v2.trx`, SHA-256 `5C0F1184FF1DB7211BFB637FDE5B31A4ED1B87373AB8996ADF26CC2B1487AE8D`, is **0/1** at `Expected ExistingMatch / Actual BindingConflict`. It is component evidence for exact replay, not a synthetic P32 producer.

All four failed TRX are enumerated in `a3_assembly_failed_run_census.tsv`: three `EVIDENCE_RED`, one `SUPERSEDED_RED`, zero unclassified diagnostic.

## Row dispositions

| Row | Disposition | Reason |
| --- | --- | --- |
| P29 | `OPEN — COMPONENT PROVEN` | Both production barriers and exact residue are proven, but no production caller resolves/invokes the registered assembly orchestrator. |
| P30 | `OPEN — COMPONENT PROVEN / SEMANTIC GAP` | No-selection is fail-closed, but the product emits broad `SourceUnavailable`, not exact `SelectionNone`. |
| P31 | `BLOCKED — NO APPROVED PRODUCER` | Duplicate authoritative session/class selection is structurally prohibited; downstream join cardinality is not inferred from that uniqueness. |
| P32 | `OPEN — COMPONENT CONTROL ONLY` | Exact replay/fingerprint guard is live, but no approved exact selection-conflict producer/adapter reaches the row. |
| P33 | `OPEN — COMPONENT PROVEN` | Selected-but-Committed is distinguished by pre-state/residue and stops before provider work, but no production caller exists. |
| P34 | `BLOCKED — PRODUCT SEMANTICS` | Source read failures collapse to `VerificationIndeterminate`; no owned quarantine write implements the row. |
| P35 | `BLOCKED — PRODUCT SEMANTICS` | Current C2 results are broad conflict/unavailable/unknown and may retain `Preparing`; no exact prepare-failed/automatic-abort producer exists. |
| P36 | `BLOCKED — REACHABILITY/SEMANTICS` | Seal SQL has broad `AssemblyConflict`; orchestrator derives items from the same frozen bindings and has no natural exact class-set mismatch producer. |

The production source contains the assembly implementation and DI extension, but the runtime assembly graph is constructed only for `FixtureProof` in a non-production host. Production therefore currently has neither an enabled assembly runtime composition nor an ordinary endpoint, worker or caller invoking `IRawExportAssemblyOrchestrator`. Fixture construction is not represented as a shipping workflow.

## Non-claims

No P29–P36 row is closed. The packet does not claim production authentication/provider provisioning, production caller reachability, public egress, C3 delivery, full-suite PASS, A3 PASS, landing or production activation. No stage, commit or push was performed.

The exact candidate is the adjacent `a3_assembly_candidate_manifest.tsv` plus SHA sidecar. The review should also require the raw outputs of `a3_assembly_manifest_compare.ps1` and `a3_assembly_trx_verify.ps1`, with zero outside-allowlist drift, zero baseline TRX drift, zero unclassified failed run and zero live mutant match.

Raw machine output on the frozen candidate:

```text
baseline_manifest_files=1785
candidate_manifest_files=1794
added_paths=9
modified_paths=5
deleted_paths=0
new_paths_outside_allowlist=0
modified_paths_outside_allowlist=0
deleted_paths_outside_allowlist=0
restored_production_sha_mismatch=0
recorded_mutants=75
live_mutant_matches=0
manifest_drift=0
baseline_trx_total=1017
new_trx_total=6
failed_runs_total=4
classified_failed_runs=4
unclassified_failed_runs=0
outside_name_family_failed=0
baseline_trx_drift=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```
