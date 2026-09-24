# A3 P29-P36 DurableWorker joined-proof review packet v1

Status: `TECHNICAL_CLOSURE_CANDIDATE — HOMEOWNER RATIFICATION NOT YET GRANTED`

Candidate manifest SHA-256: `1933DE579DBAA41B29B6A37837455573190B927D1D2CC6A3B9B8B819585A6333`

## Scope and non-claims

This packet covers only the eight active assembly outcome rows P29-P36 and the shared exit condition `EXACT_DURABLE_WORKER_JOIN_AND_HOMEOWNER_RATIFICATION`.

- It does not add or change a production API, engine, worker, work-source, assembly primitive, SDK surface, migration, or product outcome mapping.
- It does not ratify any row, reduce the activation census, update the active partition, or re-mint the activation seal.
- Current governance remains: seal format 2, revision 11, approved topology `DurableWorker`, authority-open count 8, A3 HOLD.
- `NOT_IMPLEMENTED` in the active partition must not be read as missing product code. The production work-source, hosted worker, orchestrator, same-Job delivery path, and all eight outcome producers already exist. This packet supplies the missing joined row proof only.

## Production path exercised

Each row proof now traverses the same production mechanism:

```text
EncryptedExportPacket permit and JobId
  -> DurableRawExportAssemblyWorkSource.TryAcquireAsync()
  -> exact acquired RawExportAssemblyExecutionRequest
  -> RawExportAssemblyHostedService (DurableWorker)
  -> RawExportAssemblyOrchestrator.ExecuteAsync()
  -> DurableRawExportAssemblyWorkSource.RecordAsync()
  -> PostgreSQL job state/event/failure residue
```

The small scheduler in the test does not invent a job, result, or durable residue. It replays exactly once the request already acquired by the real production work-source so the hosted worker is deterministic, then delegates recording to that same production work-source. The test asserts the exact request and exactly one `RecordAsync` call.

## One-to-one row map

The machine-readable companion is `a3_p29_p36_row_evidence_map_v1.tsv` (8 records, one per active RowId).

| Row | Public outcome | Production execution outcome | Named assertion | Required residue/no-egress assertion |
|---|---|---|---|---|
| P29 | `RAW_EXPORT_AUTHORITY_INVALID` | `AuthorityInvalid` | `C112_first_fresh_barrier_precedes_source_read`; `C113_second_fresh_barrier_immediately_precedes_prepare` | First barrier: no source read, C2 prepare, binding, disposition, assembly identity, or item. Second barrier: frozen bindings unchanged, no C2 call, no sealed identity/item. Durable job is `TerminalFailed / JobTerminalFailed / AUTHORITY_REVALIDATION_FAILED`. |
| P30 | `RAW_EXPORT_SOURCE_SELECTION_NONE` | `SelectionNone` | `C102_missing_duplicate_or_mismatched_source_has_zero_bindings` | No binding, provider preparation, assembly identity, or item. Durable job is `TerminalFailed / JobTerminalFailed / ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE`. |
| P31 | `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS` | `SelectionAmbiguous` | named subcase `P31-selection-ambiguous` in `A3_W2_AssemblyExactOutcomeMatrixUsesOneSharedProviderFixture` | No object/provider read and no assembly residue. Durable job is terminal non-retryable. |
| P32 | `RAW_EXPORT_SOURCE_SELECTION_CONFLICT` | `SourceBindingInvalid` | named subcase `P32-binding-conflict` in the same matrix | Frozen binding bytes remain unchanged; no object/provider read or assembly residue. Durable job is terminal non-retryable. |
| P33 | `RAW_EXPORT_SOURCE_UNAVAILABLE` | `SourceUnavailable` | `A3_ASSEMBLY_P33_SelectedCommittedSourceIsUnavailableWithoutProviderRead` | Existing committed selection/publication remains, but no binding, object/provider read, preparation, assembly identity, or item. Durable job is `Assembling / AttemptFailedRetryable / ATTEMPT_EXECUTION_FAILED_RETRYABLE`. |
| P34 | `RAW_EXPORT_SOURCE_INTEGRITY_INVALID` | `SourceIntegrityInvalid` | named subcase `P34-integrity-evidence` in the same matrix | Exact integrity-failure evidence is durable; no provider prepare or assembly identity/item. Durable job is terminal non-retryable. |
| P35 | `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED` | `AssemblyPrepareFailed` | named subcase `P35-prepare-abort` in the same matrix | Exactly one prepare and abort; disposition `Aborted`; no assembly identity/item. Durable job is terminal non-retryable. |
| P36 | `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` | `AssemblyClassSetMismatch` | named subcase `P36-class-set-abort` in the same matrix | Exactly one prepare and abort; disposition `Aborted`; no seal/assembly identity/item. Durable job is `TerminalFailed / JobTerminalFailed / JOB_GRAPH_INVARIANT_FAILURE`. |

The eight rows therefore have eight distinct named assertions. Five are top-level tests; the shared matrix contains five individually named and independently reported subcases, rather than crediting one assertion to five rows.

## Evidence sequence

| Artifact | Result | Use |
|---|---:|---|
| `a3-p29-p36-predecessor-component.trx` | 5/5 PASS | Establishes the pre-existing component assertions before the production join. |
| `a3-p29-p36-durable-worker-joined-a6.trx` | 5/5 PASS | First complete production DurableWorker join. |
| `a3-p29-p36-durable-record-mutant-a7.trx` | 0/5 PASS | Mutation A: skip production durable recording for the eight outcomes; job residue assertions fail. |
| `a3-p29-p36-durable-worker-restored-a8.trx` | 5/5 PASS | Byte-exact restoration after mutation A. |
| `a3-p29-p36-exact-outcome-mutant-a13.trx` | 0/5 PASS | Mutation B: remap every P29-P36 outcome; all eight named exact-outcome assertions fail. |
| `a3-p29-p36-durable-worker-final-a14.trx` | 5/5 PASS | Final restored joined gate. |
| `a3-p29-p36-adjacent-restored-a15.trx` | 2/2 PASS | Adjacent production work-source and hosted-worker caller gate. |

Mutation B mappings were deliberately wrong for all rows:

```text
P29 AuthorityInvalid          -> StateConflict
P30 SelectionNone            -> StateConflict
P31 SelectionAmbiguous       -> StateConflict
P32 SourceBindingInvalid     -> StateConflict
P33 SourceUnavailable        -> StateConflict
P34 SourceIntegrityInvalid   -> VerificationIndeterminate
P35 AssemblyPrepareFailed    -> PreparationConflict
P36 AssemblyClassSetMismatch -> AssemblyConflict
```

The final retained mutant reports exact expected/actual failures; the five matrix rows retain their individual scenario names in the aggregate failure. Mutation A independently proves that correct enum values without the required PostgreSQL residue are insufficient.

## Failed-run accounting

Nine failed TRX are retained and all nine are classified in `a3_p29_p36_failed_run_census_v1.tsv`:

```text
EVIDENCE_RED      2
SUPERSEDED_RED    7
unclassified      0
```

The superseded runs document fixture conversion and mutation stabilization. They are not credited as row evidence.

## Byte and governance posture

Current product-source hashes after both mutations were restored:

```text
RawExportAssemblyRuntimeInfrastructure.cs
AB3995E38321824FBF2CABF224894C651CD515E54DA6757F470CEA01CE81B2E5

RawExportAssemblyOrchestrator.cs
5EDC9B2A79EA7CB98567B779A7A454BC62C478AC927C734489AE966716C235A0
```

Only test/evidence bytes are part of this closure candidate. The existing unrelated `docs/00_GDRIVE_FILE_INDEX.md` worktree change is excluded. Staged and conflicted counts are both zero before packaging.

Builds correctly emit `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` because current evidence bytes are newer than seal revision 11. This is expected fail-closed behavior. No seal re-mint is authorized by this technical packet.

## Requested independent review disposition

Review the eight one-to-one row mappings, production DurableWorker join, exact outcome assertions, durable PostgreSQL residue, mutation discrimination, restoration, and failed-run census.

If independently accepted, the technical half of `EXACT_DURABLE_WORKER_JOIN_AND_HOMEOWNER_RATIFICATION` is complete for P29-P36. The activation census remains 8 until the Homeowner explicitly ratifies all eight rows and a separate governance transaction updates partition/ledger/ownership/manifest and re-mints the seal.
