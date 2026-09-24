# A3 ASSEMBLY cluster execution matrix v1

Status: `CLUSTER_CANDIDATE_COMPLETE — COMPONENT PROOF ONLY — NO ROW CLOSED`

Date: 2026-09-19

Official baseline: **57 open = 56 normative + 1 seam**. A3 remains HOLD.

## 1. Exact cluster scope

This cluster contains exactly eight open parent rows:

| Row | Parent outcome | Required business condition |
| --- | --- | --- |
| P29 | O29 `RAW_EXPORT_AUTHORITY_INVALID` | assembly authority is not current; ingress must not import this code |
| P30 | O30 `RAW_EXPORT_SOURCE_SELECTION_NONE` | no selected source; no source binding or provider read |
| P31 | O31 `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS` | ambiguous selection; no mutation or provider read |
| P32 | O32 `RAW_EXPORT_SOURCE_SELECTION_CONFLICT` | frozen selection conflict; existing mapping unchanged |
| P33 | O33 `RAW_EXPORT_SOURCE_UNAVAILABLE` | selected source unavailable; no assembly/provider read or false Available |
| P34 | O34 `RAW_EXPORT_SOURCE_INTEGRITY_INVALID` | owned deterministic corruption/quarantine evidence, distinct from historic commitment mismatch |
| P35 | O35 `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED` | exact C2 prepare failure/abort with no assembly identity |
| P36 | O36 `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` | class-set mismatch; no committed seal/assembly identity |

No other reconciliation row may be flipped by this cluster.

## 2. Frozen preflight source pins

| Surface | SHA-256 |
| --- | --- |
| `src/TagEkyc.Api/Program.cs` | `FD5610C1981BA0AB33C18E586AD691AA2106795A9A6FB9838C95BB97CF91CF40` |
| `src/TagEkyc.Contracts/RawExport/RawExportAssemblyContracts.cs` | `169207F925066342BDD938347F88AE5CD7E6DA1FD5B9ED5B4FD5CBB7598C34AB` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs` | `5DD6CD25AA92E924CAC0C9955E1F088DE572779F917CBE1F083101158F66573E` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblySourceResolver.cs` | `6E03A2487FD1D9B0BDA27DC74649379E8D8CFD06743FD10548E69D019F707FCA` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRepository.cs` | `6293A0DC480C3825E081D15DA6EF1FB253A20C007919D4A4DE85673FAEAEC687` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs` | `C312103918ACFFC64A81720CA1919C88CB6F317116A004F6B5A9A9E4F39DB774` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.cs` | `125EC8B98ED3F95B2504DA59A548C32814A61C497391E29138D4288A8FC5FBEF` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs` | `C2203FBCDDB970FBCC8921993347D47D9D25AA75CD07B6713C828A9E1D2DFBDC` |
| reconciliation at preflight | `FB372BA678DBD941A6A9A3D2459BAEE84E730E80551A83B2F858D0CEAD5A5E20` |

Different bytes before the first implementation edit are a STOP condition.

## 3. §5 truth declarations

### 3.1 What is product-real

- `RawExportAssemblyOrchestrator`, `RawExportAssemblyRepository`, `RawExportAssemblySourceResolver`, `RawExportFramedSourceVerificationService`, the C1 migration functions, and the C2 protocol interfaces are production source.
- PostgreSQL decisions and residue must be read from the disposable database, not echoed from fixture inputs.
- Exact-object reads may use the production S3-compatible reconciler against the pinned MinIO fixture.
- Any claim about authority, selection, binding, preparation, seal, source state, or residue must be backed by production return values plus persisted rows.

### 3.2 What remains a test premise/double

- The C1 contract explicitly permits a fixture-only assembly authenticator, fixture C2 provider and synthetic non-patient plaintext for proof.
- Those fixtures may create prerequisites and deterministic scheduling only. They may not invent the result being claimed, bypass the repository/orchestrator, or return a canned P29–P36 public result.
- The existing `FixtureAssemblyAuthenticator` and `BoundedFixtureC2Provider` do not prove production key/provider provisioning, production activation, delivery, or authentication of a public caller.

### 3.3 Caller and topology boundary

- `Program.cs` calls the `AddTagEkycRawExportAssembly` extension, but the extension returns before registering the runtime assembly graph unless topology is `FixtureProof` and the host is non-production.
- No production source caller of `IRawExportAssemblyOrchestrator.ExecuteAsync` exists at this baseline. The interface and implementation exist in production source, while the runtime graph is registered only for the permitted fixture topology and used by tests; it is not enabled or mapped to an ordinary production endpoint/worker.
- Therefore a test may prove a bounded C1 component path through production code and PostgreSQL, but must not call it a shipping public C1/C3 workflow.
- A row that requires an ordinary production caller remains OPEN unless the Homeowner separately authorizes that product surface.

### 3.4 NO-EGRESS boundary

- O29–O36 are `NO EGRESS`. The already-ratified All14 proof shows that injecting one of these internal codes into A3 ingress is sanitized to wire `503 NOT_READY` with no body read.
- That aggregate proof is only the egress half. It does not prove the business trigger, phase, residue, or producer for any P29–P36 row.
- This cluster must join real assembly condition/residue evidence to the frozen no-egress proof; it must not create synthetic Final responses to close a row.

## 4. Reachability and semantic inventory before mutation

| Row | Current production branch | Preflight disposition |
| --- | --- | --- |
| P29 | `raw_export_freeze_job_source_bindings` returns `AuthorityInvalid`; orchestrator maps it to `RawExportAssemblyExecutionOutcome.AuthorityInvalid` | `TESTABLE_COMPONENT_NON_CLOSING` via first and second authority barriers; no production caller exists, so this batch cannot close the row |
| P30 | absence of an eligible selected source returns broad `SourceUnavailable` before any binding is written | `TESTABLE_COMPONENT_NON_CLOSING`; current product has no distinct `SelectionNone` producer, so distinguish this component only by pre-state and residue |
| P31 | duplicate authoritative session/class selection is structurally prohibited; no approved `SelectionAmbiguous` producer exists. Downstream joined-candidate cardinality is not inferred from this uniqueness constraint | `SEMANTIC_REACHABILITY_BLOCKER`; do not weaken the uniqueness constraint or fake an ambiguous result |
| P32 | an existing frozen row with a different binding fingerprint returns broad `BindingConflict` | `TESTABLE_COMPONENT_NON_CLOSING`; prove original frozen row byte-equal and no provider read, but do not equate that broad result with exact P32 semantics |
| P33 | missing/non-Available/non-VerifiedCompleted/inactive-key selection returns `SourceUnavailable`; resolver can also return null | `TESTABLE_COMPONENT_NON_CLOSING`; partition from P30 must be observable in setup/residue because the typed outcome is shared, and no production caller exists |
| P34 | deterministic ciphertext and historic-commitment failures are classified internally, but `ExecuteAsync` catches every `RawExportAssemblySourceReadException` as `VerificationIndeterminate`; no owned quarantine write occurs in C1 | `SEMANTIC_PRODUCT_BLOCKER`; exact integrity-invalid/quarantine semantics are absent |
| P35 | C2 prepare conflict/unavailable/unknown map to `PreparationConflict`, `ProviderUnavailable`, or `ProviderOutcomeUnknown`; durable `Preparing` may remain for recovery and abort requires separate authorization | `SEMANTIC_PRODUCT_BLOCKER`; there is no exact prepare-failed/automatic-abort producer |
| P36 | the seal SQL checks item count and item/binding equality and returns broad `AssemblyConflict`; the orchestrator derives its items from the same frozen bindings, so no natural class-set mismatch producer reaches it | `SEMANTIC_REACHABILITY_BLOCKER`; direct SQL proof is component-only and cannot establish the row without an approved interpretation |

Blockers do not stop independent P29/P30/P32/P33 component work. All four are non-closing in this batch because the production entry point is absent and P30/P32 additionally lack exact typed producers. P31/P34/P35/P36 must remain explicit and be carried into the consolidated semantic-blocker packet.

## 5. Scenario matrix

| Scenario | Server pre-state | Host/caller | Real dependencies | Double/premise | Read expectation | Expected component result | Durable residue | Mapped rows | Guard |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AS-A1 authority withdrawn before pass 1 | valid Assembling job + one selected Available source; authority then withdrawn | direct production orchestrator, no public endpoint claim | PostgreSQL, resolver/repository | fixture auth/C2; synthetic source | object read 0; C2 prepare 0 | `AuthorityInvalid` | zero bindings/assembly/preparation identity | P29 | AS-G1 |
| AS-A2 authority withdrawn after pass 1 | same; withdrawal scheduled inside authenticator callback after pass 1 | direct production orchestrator | PostgreSQL + exact-object read | fixture auth callback controls timing; fixture C2 | one pass-1 read; C2 prepare 0 | `AuthorityInvalid` | pass-1 bindings remain frozen and byte-equal; one durable assembly disposition may already be `Preparing` after `RegisterPreparingAsync`; no C2 preparation, assembly identity/items, `Pending`, `SealCommitted`, or `Finalized` residue | P29 | AS-G1 |
| AS-S0 no selection | Assembling job has required class but no eligible session selection | repository/orchestrator | PostgreSQL | synthetic job/source setup only | object read 0; C2 prepare 0 | `SourceUnavailable` | zero bindings and assembly rows | P30 | AS-G2 |
| AS-SC frozen selection conflict | exact binding frozen once; source-selection inputs changed so recomputed fingerprint differs | repository/orchestrator | PostgreSQL | fixture setup creates both versions | object/provider read 0 on conflict | `BindingConflict` | original binding byte-equal, count unchanged | P32 | AS-G3 |
| AS-SU selected source unavailable | selection exists but publication/object/key eligibility is removed before freeze or resolver read | repository/orchestrator | PostgreSQL | fixture setup establishes then changes one eligibility premise | no assembly/C2 provider read after denial | `SourceUnavailable` | no false Available; no assembly/preparation | P33 | AS-G2/AS-G4 |
| AS-SA ambiguous selection | duplicate authoritative session/class selection is structurally prohibited; no approved `SelectionAmbiguous` producer exists; downstream joined-candidate multiplicity is not inferred from that constraint | none | none | none permitted | N/A | blocker | no mutation | P31 | AS-G5 |
| AS-I deterministic integrity failure | exact object is changed after selection | production resolver/orchestrator component | PostgreSQL + MinIO + framed verifier | fixture key/auth/C2 only | exact object may be read; C2 prepare 0 | currently `VerificationIndeterminate` | currently no C1 quarantine evidence | P34 | AS-G6 |
| AS-P prepare failure | durable Preparing followed by C2 conflict/unavailable/unknown | production orchestrator/repository | PostgreSQL | deterministic fixture C2 drives actual protocol branch | bounded assembly write may occur; no seal | current broad provider/preparation outcome | Preparing/recovery residue, no assembly identity | P35 | AS-G7 |
| AS-C class-set mismatch | mismatched item set at seal | production seal SQL component only at baseline | PostgreSQL | synthetic prerequisite rows | no new provider read at seal | broad `AssemblyConflict` | no assembly identity/head transition | P36 | AS-G8 |

## 6. Unique guard inventory

| Guard | Production mechanism | Rows | Mutation eligibility |
| --- | --- | --- | --- |
| AS-G1 | current authority/consent equality at freeze plus second pre-Prepare freeze | P29 | eligible: one guard at a time; existing C112/C113 are controls, not row closure by themselves |
| AS-G2 | eligible selected-source join and `SourceUnavailable` early return | P30/P33 | eligible only if the two scenarios remain distinguishable by pre-state and residue |
| AS-G3 | frozen `BindingFingerprint` exact equality / insert-once replay | P32 | eligible |
| AS-G4 | resolver re-read of frozen binding, `VerifiedCompleted`, ciphertext length/digest and exact object | P33/P34 | eligible for P33; P34 blocked until semantics are authorized |
| AS-G5 | unique `(VerificationSessionId, RawClass)` selection authority | P31 | no mutation: weakening a ratified uniqueness invariant is prohibited |
| AS-G6 | typed framed-verification disposition mapped through the orchestrator | P34 | blocked: current catch collapses the required distinction and has no quarantine effect |
| AS-G7 | C2 `PrepareOrRecoverAsync` result mapping plus durable Preparing/abort authorization | P35 | blocked for row closure; component controls may be inventoried without changing semantics |
| AS-G8 | seal item-count/item-binding equality and broad `AssemblyConflict` | P36 | blocked for row closure pending interpretation; direct SQL mutation cannot manufacture orchestrator reachability |

No source mutant may be created until this guard inventory is independently accepted or the continuous-mode preflight is otherwise authorized by the Homeowner.

## 7. Horizontal execution phases after preflight approval

1. Complete all shared test/harness edits for AS-A1/A2/S0/SC/SU in one candidate-edit phase.
2. Build the candidate once.
3. Run all positive controls by mechanism family.
4. Run mutations by AS-G guard, not by row.
5. Restore exact bytes and verify hashes.
6. Rebuild once.
7. Run one joined restored ASSEMBLY gate, then ratified sentinels affected by changed source.
8. Enumerate and classify every new Failed TRX, including names outside the `a3-assembly-*` family; require `outside_name_family_failed=0`.
9. Regenerate candidate manifest, drift ledger, TRX inventory and cumulative mutant sweep.
10. Issue one `A3_ASSEMBLY_CLUSTER_REVIEW_PACKET`; no row-level packets.

## 8. Stop conditions and non-claims

STOP on regression of a ratified row, undeclared product route/API/schema change, new test double replacing the claimed behavior, source drift outside the allowlist, ineffective mutation presented as RED evidence, or non-byte-exact restoration.

The only authorized final deltas are those in `a3_assembly_allowed_delta_v1.tsv` SHA-256 `9994AC765812D267AED8874725A22BDEC4E9B7E083F01A445002D7D69483CF17`. No final production-source change is authorized. `a3_assembly_manifest_compare.ps1` SHA-256 `33AAF1482263BFDDF3F753B3D05DD6FF8DE78CA8E2F539929828B97FD17E1067` is the mandatory candidate drift gate. Run it against the regenerated candidate manifest, then run `a3_assembly_trx_verify.ps1`; both must exit zero. A product defect resets the candidate phase and requires new authority rather than silently widening this allowlist.

This preflight does not authorize or claim a production assembly provider, production authentication key, ordinary production caller, C3 delivery, A3 PASS, full suite, stage, commit, push, landing or production activation.

## 9. Machine baseline

```text
baseline_trx_total=1017
new_trx_total=0
failed_runs_total=0
classified_failed_runs=0
unclassified_failed_runs=0
outside_name_family_failed=0
baseline_trx_drift=0
manifest_drift=0
new_paths_outside_allowlist=0
modified_paths_outside_allowlist=0
deleted_paths_outside_allowlist=0
restored_production_sha_mismatch=0
recorded_mutants=71
unique_mutants=71
live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

Baseline artifacts:

- `a3_assembly_baseline_trx_inventory.tsv`
- `a3_assembly_failed_run_census.tsv`
- `a3_assembly_trx_verify.ps1`
- `a3_assembly_allowed_delta_v1.tsv`
- `a3_assembly_manifest_compare.ps1`
- `a3_assembly_baseline_manifest.tsv` plus sidecar

The manifest excludes itself and its sidecar to avoid a self-hash cycle. Its exact SHA and file count are reported from the sidecar and preflight handoff, not embedded into the hashed matrix.

## 10. Candidate results

The component baseline `a3-assembly-component-baseline.trx` is **5/5 PASS**. It covers both P29 authority barriers, P30 no-selection, P32 exact replay and P33 selected-but-Committed unavailability. The rebuilt final joined gate `a3-assembly-final-restored-joined.trx` is also **5/5 PASS**, zero skip, on restored production bytes.

Guard runs:

- `AS-G1`: removing only the second pre-Prepare barrier produced `a3-assembly-g1-second-barrier-red.trx`, **1 PASS / 1 FAIL**. C112 stayed green; C113 failed at the object-read count (`Expected 1, Actual 2`) before its zero-provider/residue assertions.
- `AS-G2`: changing the shared no-eligible-source projection from `SourceUnavailable` to `BindingConflict` produced `a3-assembly-g2-shared-unavailable-red.trx`, **0/2**. Both the no-selection and selected-but-Committed cases changed together. This is positive evidence that P30 and P33 share one broad product outcome, and negative evidence against claiming exact `SelectionNone` semantics.
- `AS-G3`: the first coarse mutation forced even the initial insert to conflict and is superseded. Its narrower successor forces only replay to conflict; `a3-assembly-g3-fingerprint-replay-red-v2.trx` is **0/1** at `Expected ExistingMatch / Actual BindingConflict`. This proves exact replay is load-bearing but does not manufacture an approved P32 business trigger.

Production source restored byte-exact before the final rebuild:

```text
RawExportAssemblyOrchestrator.cs=5DD6CD25AA92E924CAC0C9955E1F088DE572779F917CBE1F083101158F66573E
20260815120000_Tip88C1C1ResolverAssembly.cs=125EC8B98ED3F95B2504DA59A548C32814A61C497391E29138D4288A8FC5FBEF
Tip88C1C1ResolverAssemblyTests.cs=2059A871540A93D783418DFCD45572FE3CAACF418D426F58D08BF0D18C1F99E2
```

Disposition: P29/P30/P32/P33 have bounded component evidence but remain `NOT IMPLEMENTED`; P31/P34/P35/P36 remain explicit blockers. No P29-P36 row flips in this cluster. Official and candidate census remain **56 normative + 1 seam = 57 open**; A3 remains HOLD.

Final machine counters:

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
```
