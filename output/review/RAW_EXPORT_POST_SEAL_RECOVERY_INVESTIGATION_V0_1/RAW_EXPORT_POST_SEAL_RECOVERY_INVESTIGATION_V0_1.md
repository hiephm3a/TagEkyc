# RAW-EXPORT POST-SEAL RECOVERY INVESTIGATION V0.1

## Verdict

**CONFIRMED** at the current production source/SQL dispatch boundary.

The production seal path does write `AssemblySealed` + `SealCommitted`, increments the job revision, and clears the lease before C2 finalize and C1 `RecordFinalized`. The only production work selector excludes `AssemblySealed`, while the committed-recovery helper requires the exact pre-seal request that a fresh worker cannot currently reconstruct through any production dispatcher.

The exact process-kill/restart scenario is **NOT_PROVEN** because no existing test starts a fresh production worker after the post-seal checkpoint. Existing C118 proves replay only when the old request is retained by the test.

Layer 2 remains **PAUSED**. This investigation made no product, SDK, migration, test, governance, census, ratification, or seal changes.

## Repository evidence gate

- Repository: `D:/Task/Remote Signing/TagEkyc`
- HEAD: `c43bc9bb0c4bd08c35bab65c1fe0696f88ce3610`
- Branch: `tip-88a-raw-export-policy-catalog-build`
- Tracking: `+0/-0`; direct `git ls-remote` also matched `c43bc9b...` during the gate.
- Pre-existing dirty file preserved: `docs/00_GDRIVE_FILE_INDEX.md`.
- Investigation output is the only new tree: `output/review/RAW_EXPORT_POST_SEAL_RECOVERY_INVESTIGATION_V0_1/`.
- Effective latest migration, verified from isolated PostgreSQL `__EFMigrationsHistory`: `20260925090000_RawExportAssemblyRetainedModeWorkSource`.
- `pg_get_functiondef` was queried after that migration was present. It confirmed the effective selector, committed-recovery reader, and attempt-failure mutation match the source conclusions below.

## Claim adjudication

| Claim | Conclusion | Evidence class | Mechanism and limit |
|---|---|---|---|
| CC: post-`Assembling` job states are not written by production | **REFUTED** | `SOURCE_CONFIRMED` + `EXISTING_TEST_ONLY` | `RawExportAssemblyOrchestrator.ExecuteAsync` calls `SealAsync` at `RawExportAssemblyOrchestrator.cs:116-118`; repository calls the SQL seal at `RawExportAssemblyRepository.cs:263-287`; SQL writes C1 `SealCommitted` at migration `20260815120000...cs:852`, job `AssemblySealed`, revision `+1`, and null lease at `:862`, then returns at `:866`. C120 observes that durable residue at `Tip88C1C1ResolverAssemblyTests.cs:1000-1046`. |
| CC: selecting only `Claimed`/`Assembling` is sufficient | **REFUTED** | `SOURCE_CONFIRMED` + current-DB `RUNTIME_REPRODUCED` | Effective `raw_export_next_assembly_candidate()` selects only `Claimed` or expired/unowned `Assembling` (`20260925090000...cs:24-47`). The isolated current DB returned the same definition. No production selector returns `AssemblySealed`. |
| CC: this is future debt only | **REFUTED** | `SOURCE_CONFIRMED` | The activating condition exists now: production SQL writes `AssemblySealed` before two later operations. A crash/failure window is therefore current, not hypothetical future schema use. Exact OS-process crash reproduction remains absent. |
| GPT: seal writes `AssemblySealed` + `SealCommitted`, clears lease, then C2 finalize and C1 record follow | **CONFIRMED** | `SOURCE_CONFIRMED` + `EXISTING_TEST_ONLY` | SQL writes occur at migration `20260815120000...cs:852-866`. Orchestrator calls C2 finalize at `RawExportAssemblyOrchestrator.cs:130-137` and only then C1 `RecordFinalized` at `:139-145`. C120 retains the intermediate residue. |
| GPT: a fresh worker cannot rediscover the post-seal obligation | **CONFIRMED at dispatch/source boundary** | `SOURCE_CONFIRMED`; process-kill `NOT_PROVEN` | Host gets work only from `IRawExportAssemblyWorkSource.TryAcquireAsync` (`RawExportAssemblyHostedService.cs:23-36`). Durable source only invokes `raw_export_next_assembly_candidate` then acquire (`RawExportAssemblyRuntimeInfrastructure.cs:149-179`). DI registers no second recovery source (`RawExportAssemblyServiceCollectionExtensions.cs:43-55`; `Program.cs:64-70`). The recovery helper exists, but it is called only inside `ExecuteAsync(request)` (`RawExportAssemblyOrchestrator.cs:27`, `:174-215`) and needs exact job/attempt/pre-seal revision/fence (`RawExportAssemblyRepository.cs:322-339`). |
| GPT: post-seal finalize failure returns null revision; `RecordAsync` reuses stale revision and conflicts | **CONFIRMED** | `SOURCE_CONFIRMED`; direct runtime mutation call `NOT_PROVEN` | All `Failure(...)` results carry null revisions (`RawExportAssemblyOrchestrator.cs:408-412`). `RecordAsync` falls back to `request.ExpectedJobRevision` (`RawExportAssemblyRuntimeInfrastructure.cs:195`) and calls `RecordAttemptFailureAsync` (`:196-206`). Seal has already changed head revision to `R+1` and removed lease. Effective SQL checks revision first and returns `ConcurrencyConflict` (`20260726145547...cs:566-570`); repository converts that into `RAW_EXPORT_JOB_CONCURRENCY_CONFLICT` (`EfRawExportJobRepository.cs:130-149`, `:938-950`). The hosted worker has no local catch around `RecordAsync` (`RawExportAssemblyHostedService.cs:35-36`), so its execution loop faults; wider host behavior was not runtime-reproduced here. |

## Checkpoint table

| Checkpoint | Job head | C1 preparation | C2 package | Committed now | Outstanding obligation |
|---|---|---|---|---|---|
| Before SQL seal | `Assembling`, revision `R`, attempt `A`, fence `F`, live worker lease | `Pending`, revision `P` | `Prepared` | source binding, assembly derivation, C2 prepared package | seal, C2 finalize, C1 finalized record |
| After SQL seal commit | `AssemblySealed`, revision `R+1`, same `A/F`, lease owner/expiry null | `SealCommitted`, revision `P+1` | `Prepared` | assembly identity/items, `AssemblySealed` event, sealed head and C1 disposition | C2 finalize, then C1 finalized record |
| After C2 finalize | unchanged `AssemblySealed/R+1`, no lease | still `SealCommitted/P+1` | `Finalized` | recipient package finalized | C1 `RecordFinalized` only |
| After C1 `RecordFinalized` | still `AssemblySealed/R+1`, no lease | `Finalized`, revision `P+2` | `Finalized` | all intended post-seal records | none |
| Finalize returns unavailable/unknown while still incomplete | `AssemblySealed/R+1`, no lease | `SealCommitted/P+1` | normally `Prepared` or indeterminate | seal remains durable | recovery remains necessary, but `RecordAsync` tries stale `R` and conflicts |

The SQL seal is one database function call. Its C1 disposition, identity/items, job-head revision/state, lease clearing, and transition are committed atomically by the database call. C2 finalize is a later, separate SQL operation (`20260818120000_Tip88C1C2RecipientPackage.cs:420-433`). C1 `RecordFinalized` is another later SQL operation (`20260815120000_Tip88C1C1ResolverAssembly.cs:885-894`).

## Production dispatch and recovery graph

```text
RawExportAssemblyHostedService
  -> DurableRawExportAssemblyWorkSource.TryAcquireAsync
     -> raw_export_next_assembly_candidate
        -> Claimed / recoverable Assembling only
     -> AcquireOrReclaimLeaseAsync
  -> RawExportAssemblyOrchestrator.ExecuteAsync(request)
     -> TryRecoverCommittedAsync(request)
        -> exact job + original attempt + original fence + pre-seal revision
     -> normal assembly path
  -> DurableRawExportAssemblyWorkSource.RecordAsync
```

There is no production edge from a fresh worker to `AssemblySealed + SealCommitted`. The recipient delivery reconciler handles delivery rows only (`RecipientPackageDeliveryReconciler.cs:5-29`); it does not dispatch C1 post-seal recovery. `RecipientPackagePreparationProvider` can finalize an already-known C2 preparation (`RecipientPackageServiceCollectionExtensions.cs:172-185`) but does not discover the C1 obligation itself.

The recovery primitive is real and properly guarded; the missing piece is durable dispatch into it.

## Scenario disposition

### A. Stop after SQL seal, before C2 finalize

**CONFIRMED at source/SQL boundary; process kill NOT_PROVEN.**

Durable residue is job `AssemblySealed`, C1 `SealCommitted`, C2 `Prepared`. A fresh production work source cannot select that job. No other registered production service reconstructs the request. C118 can recover only because the test calls the orchestrator again with `fixture.Request` still in memory (`Tip88C1C1ResolverAssemblyTests.cs:944-970`).

### B. Stop after C2 finalize, before C1 `RecordFinalized`

**CONFIRMED at source/SQL boundary; process kill NOT_PROVEN.**

C2 is already `Finalized`, while C1 remains `SealCommitted` and job remains `AssemblySealed`. The same selector gap applies. If the exact old request is explicitly replayed, inspection/finalize is idempotent and C1 can be recorded; no production dispatcher currently supplies that replay after restart.

### C. Finalize returns `Unavailable` or unresolved `OutcomeUnknown` after seal

**CONFIRMED at source boundary; direct runtime call NOT_PROVEN.**

- `Unavailable` maps immediately to `ProviderUnavailable`.
- `OutcomeUnknown` is inspected; if C2 is already finalized it becomes `ExistingMatch`, otherwise Prepared/Missing/unknown remains `OutcomeUnknown` (`RawExportAssemblyOrchestrator.cs:244-257`, `:373-379`).
- Failure results have `JobRevision=null`.
- `RecordAsync` uses pre-seal revision `R`; current head is `R+1`.
- `raw_export_record_job_attempt_failure` returns `ConcurrencyConflict`; repository throws.
- The exception is uncaught by the assembly hosted-service loop. Whether the overall host stops under its framework host options was not directly reproduced.

### D. Wrong attempt/fence/fingerprint

**CONFIRMED guards remain closed.**

Committed recovery requires exact job, attempt, fence, `head.Revision = expected pre-seal revision + 1`, `AssemblySealed` transition, identity/item consistency (`20260815120000...cs:913-948`). C118 verifies wrong fence does not reach C2 and returns `LeaseLost` (`Tip88C1C1ResolverAssemblyTests.cs:974-980`). C122 verifies wrong fingerprint is rejected by C2 finalization (`:1074-1102`).

### E. Fully finalized work

**CONFIRMED not reselected.**

The job head deliberately remains the closed state `AssemblySealed`; C1 preparation becomes `Finalized`. The candidate selector excludes it, so it cannot loop and starve later jobs. Explicit exact replay returns `ExistingMatch`. The defect is not that fully finalized jobs are reselected; it is that incomplete `SealCommitted` jobs share the same excluded job state.

## Evidence limits of existing tests

- C118: proves exact replay and guards when the test retains the old request. It is not fresh-worker discovery and not process restart.
- C120: proves actual durable intermediate residue after seal with first finalize unavailable.
- C121: proves `AssemblySealed` is closed and cannot be acquired by the normal lease API.
- Same-Job E2E: proves the uninterrupted happy path through durable source, assembly, package, delivery, and SDK. It has no crash/restart cut.
- C116: current test expects concurrent duplicate execution to yield `Sealed` plus `ExistingMatch/LeaseLost`, but twice produced `Sealed` plus `PreparationConflict`. This is an adjacent current test failure, not a fresh-worker crash proof and not used to establish the main verdict. It should be triaged separately before relying on concurrent replay semantics.

## Tests and evidence actually run

| Evidence | Result | SHA-256 |
|---|---:|---|
| `existing-c118-c120-c121.trx` | 3/3 PASS | `CE65814FA89FAFB3D2826CA52261D28C62E61B59BDBB12E0DF725DF3FA4E8264` |
| `pg-current-existing-tests.trx` | 3/3 PASS; used while querying fully migrated isolated DB | `51C9B50DCA99CAD06E76A18E7231467F85AF50F2D02355A27E9D38E03326986A` |
| `existing-same-job-e2e.trx` | 1/1 PASS | `2D4C08CA8CC084CC6385F64407C4664371BADA89615AD9A296B4ACB832250935` |
| `existing-c116-rerun.trx` | 0/1 FAIL: `Sealed + PreparationConflict` | `84DBE03AABAE492A3B715D2CD8B1B8FE90FD97D2E4B490F1986462E331B7F626` |
| `existing-c1-full.trx` | 25/29 PASS; failures C105, C116, C125, C126 | `44C94E0CF720062186E41A850AA703827AAE628717DE8C2589136391997427C3` |

Auxiliary DB-observation attempts are retained rather than deleted. The first queried before PostgreSQL became ready; the second queried while migrations were only at `20260730090000`, so neither is used as current-SQL evidence. The final observation waited until `20260925090000_RawExportAssemblyRetainedModeWorkSource`, obtained current `pg_get_functiondef`, and its companion test exited 0.

The focused build emitted `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` because investigation evidence under `output/` changed whole-tree bytes. No seal/governance update was performed, per scope.

## Finding

### F1 — current product recovery/dispatch defect

`AssemblySealed + SealCommitted` is a durable, production-written intermediate condition, but no fresh production worker can discover it. This can strand a package before C2 finalize. After C2 finalize it can leave C1 finalization residue incomplete. This is not future debt.

### F2 — current post-seal error-recording defect

When C2 finalize remains unavailable/unknown after seal, the result drops the seal revision and the work source attempts an `Assembling` retry-failure mutation with stale revision and no longer-held lease. Current SQL rejects it, and the worker loop does not catch the resulting repository exception.

### Adjacent signal — C116

The existing concurrent-execution proof currently fails consistently with `PreparationConflict` for the loser. It is not sufficient evidence to widen F1/F2, but it must not be cited as green concurrent replay evidence.

## Safe correction direction — no patch applied

Do **not** add `AssemblySealed` to the normal selector and call the old acquire path. `raw_export_acquire_or_reclaim_job_lease` creates a new attempt/fence and moves the head to `Assembling` (`20260726145547...cs:509-539`), while the head trigger only permits acquire from `Claimed/Assembling` (`20260815120000...cs:452`). That would either fail or break sealed lineage.

The narrow correction should:

1. Add a dedicated durable selector for `AssemblySealed` whose C1 preparation is `SealCommitted`, returning the original job/attempt/fence and the pre-seal revision (`head.Revision - 1`) only after exact identity/transition checks.
2. Dispatch that exact request into the existing `TryRecoverCommittedAsync` path without acquiring a new lease, creating a new attempt, reassembling, or changing the fingerprint.
3. Keep failed post-seal recovery discoverable with bounded backoff; never call `RecordAttemptFailureAsync` for an already-sealed job.
4. Exclude C1 `Finalized` rows from the recovery selector so completed work is not looped.
5. Preserve the existing exact attempt/fence/fingerprint and idempotency guards.

## Exact next-step allowlists proposed

### Test-only proof before implementation

Permit only:

- `tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs`

Add named tests for A-E using current production repositories/work source. They must create a new `DurableRawExportAssemblyWorkSource` after the durable cut, prove it returns no request today for A/B, invoke current `RecordAsync` for C and assert the exact conflict, preserve D guards, and prove E is not selected. These are fresh-scope runtime proofs, not process-kill claims.

If true process-kill/restart proof is required, separately approve:

- `tests/TagEkyc.AssemblyRecoveryProbe/TagEkyc.AssemblyRecoveryProbe.csproj`
- `tests/TagEkyc.AssemblyRecoveryProbe/Program.cs`
- `tests/TagEkyc.IntegrationTests/RawExportAssemblyPostSealProcessRecoveryTests.cs`
- `tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj`
- `TagEkyc.sln`

### Candidate implementation write-set, only after review

- `src/TagEkyc.Infrastructure/Persistence/Migrations/<timestamp>_RawExportAssemblyPostSealRecovery.cs`
- matching migration designer and `TagEkycDbContextModelSnapshot.cs`
- `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs`
- `src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs`
- `src/TagEkyc.Api/RawExportAssemblyHostedService.cs` only if a bounded recovery backoff/catch belongs at the loop boundary
- focused integration/migration/readiness tests only

No raw ingress, SDK, A3, P29-P36, Layer 2, site qualification, or recipient delivery API changes are indicated by this investigation.

## Final posture

- Product/source files changed: 0
- Test files changed: 0
- Migrations changed: 0
- SDK changed: 0
- Governance/seal changed: 0
- Commit/push/reset/stash: none
- Pre-existing dirty file preserved: `docs/00_GDRIVE_FILE_INDEX.md`
- New files: only this report and investigation logs/TRX under the approved output directory
- Layer 2: **PAUSED**
