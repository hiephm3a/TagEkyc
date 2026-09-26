# RAW-EXPORT POST-SEAL RECOVERY — BOUNDED FIX REVIEW PACKET V0.1

## Disposition

The four accepted defects are corrected at TagEkyc commit
`75c890347a013840c67646ce3e221e5b615b6270`.

| Area | Result |
|---|---|
| F1 fresh work-source rediscovers `AssemblySealed + SealCommitted` | **PASS** |
| F2 post-seal result recording avoids stale pre-seal revision | **PASS** |
| Candidate-to-acquire lost race is harmless polling | **PASS** |
| C116 exact concurrent replay joins committed result | **PASS** |
| Exact digest/authentication/fingerprint match | **PASS** |
| Claim owner + generation fencing | **PASS** |
| Migration Up/Down/Reapply and security metadata | **PASS** |
| C105 exact ACL surface | **PASS** |
| C126 default graph + missing-authenticator branch | **PASS** |
| C125 | **OUT OF SCOPE / UNCHANGED** |
| Process kill / OS restart | **NOT PROVEN** |
| Layer 2 site measurement ownership | **PAUSED / UNCHANGED** |

The restored focused gate is **20/20 PASS, zero skip**. Seven credited
mutations fail at the named boundary. Two attempted mutants stayed green and
are retained as `NON_DISCRIMINATING_MUTANT`; neither is credited.

## Repository posture

```text
Baseline product HEAD       c43bc9bb0c4bd08c35bab65c1fe0696f88ce3610
Characterization commit     578590a290dff4a97eb2e9d1c406038d5930f3fb
Correction commit           75c890347a013840c67646ce3e221e5b615b6270
Push                        NO
Deploy                      NO
Seal/governance re-freeze   NO
Layer 2                     NOT STARTED
SDK delta                   0
API/raw-ingress delta       0
Site-qualification delta    0
User file preserved         docs/00_GDRIVE_FILE_INDEX.md
```

The current seal is expected to report
`ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` because product/test bytes
changed and re-freeze is explicitly reserved until both reviewers accept this
packet.

## Search-before-build result

```text
SEARCHED:
  existing assembly work source, orchestrator recovery helpers, C1 recovery
  context, C2 finalize inspection, job lease mutations, readiness ACL surface,
  migration tripwires, same-Job delivery E2E

FOUND AND REUSED:
  DurableRawExportAssemblyWorkSource
  RawExportAssemblyOrchestrator.TryRecoverCommittedAsync
  FinalizeOrRecoverAsync
  C1 SealCommitted/Finalized preparation dispositions
  existing C2 preparation inspection/finalization
  existing DurableWorker hosted service and same-Job SDK E2E

MISSING AND ADDED:
  durable post-seal claim ownership/backoff state
  exact claim/defer/claimed-finalize SQL functions
  recovery-aware work-source recording
  narrow acquire-lost-race handling

NOT REIMPLEMENTED:
  assembly engine, work source, raw ingress, SDK, API, site gate, A3,
  P29-P36, runtime qualification policy
```

## Product correction

### F1 — durable rediscovery

When `raw_export_next_assembly_candidate()` returns no normal pre-seal work,
the production work source now attempts a dedicated post-seal claim. Discovery
is rooted in the C1 preparation disposition `SealCommitted`; it does not depend
on a pre-existing recovery row. Both shipping package modes are eligible:

- `EncryptedExportPacket`
- `EncryptedRawVaultRetained`

`ExternalExportOnlyNoRetain` remains excluded from background discovery.
Completed `Finalized` rows are not reclaimed.

The acquired request preserves the original job, attempt, fence and pre-seal
revision and carries a separate recovery claim owner/generation.

### F2 — recovery-aware recording

Results produced after a committed seal no longer call pre-seal attempt-failure
or terminalization mutations with revision `R`. Success is already recorded by
the claimed finalize function. Retryable/unresolved results defer the dedicated
claim with bounded exponential backoff. Logs retain the stage names:

```text
FaultStage=TryAcquireAsync
FaultStage=RecordAsync
```

### Acquire contention

Only `RAW_EXPORT_JOB_CONCURRENCY_CONFLICT` thrown by the normal candidate
acquire is converted to no-work. Other exception codes and failures still
escape. The hosted service therefore uses its existing poll delay for a known
lost race without changing global `BackgroundServiceExceptionBehavior`.

### C116 exact replay

After an uncertain or losing seal call, the orchestrator attempts exact
committed recovery before abort authorization. The join requires the original
job/attempt/fence and committed `AssemblySealed` transition, plus any supplied:

- assembly digest;
- manifest digest;
- authentication value;
- assembly fingerprint.

Mismatch returns `PreparationConflict`; it is not widened into same-Job
success. The original C116 contract now observes `Sealed` plus
`ExistingMatch | LeaseLost` rather than `PreparationConflict` from abort
handling.

### Claim coordination

The new durable claim has:

- owner identity and monotonically increasing generation;
- bounded 1..3600 second lease (production uses 300 seconds);
- bounded exponential retry backoff;
- `SKIP LOCKED` selection so one active/stuck claim does not block later work;
- a trigger/GUC mutation guard;
- deployer ownership, `SECURITY DEFINER`, `search_path=pg_catalog` and exact
  Sealer-only EXECUTE grants;
- no direct table write grants.

A displaced owner cannot defer the successor claim and cannot mark it
finalized. Expired jobs may finish the already-authorized sealed obligation,
but completion does not extend permit/job/delivery validity.

## Migration proof

Migration `20260926120000_RawExportAssemblyPostSealRecovery` adds one SQL-only
coordination table and four functions:

```text
raw_export_claim_next_post_seal_recovery(uuid, integer)
raw_export_claim_exact_post_seal_recovery(uuid, uuid, bigint, bigint,
  uuid, integer, bytea, bytea, bytea, bytea)
raw_export_defer_post_seal_recovery(uuid, uuid, bigint, text, integer)
raw_export_record_claimed_assembly_finalized(uuid, bigint, bytea, uuid, bigint)
```

The focused migration proof applies, rolls back and reapplies the migration,
compares exact catalog definitions and preserves owner, `SECURITY DEFINER`,
`search_path`, ACL and model cleanliness. Direct mutation is rejected. Current
migration tripwires now point at this migration.

## Predecessor packet corrections

The predecessor failed-run census is stated consistently as **13/13** before
this correction's new runs are added; the consolidated census is now 28/28.

`runs/sql-lock-final/post-seal-sql-lock-final.trx` retains the database query
output for `raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint)`,
including its full function definition and metadata. The acquire-contention
claim therefore no longer relies on the earlier transcript that omitted this
direct lock/acquire link.

## Focused restored proof

`runs/final-restored/post-seal-final-restored.trx` is **20/20 PASS** and covers:

- fresh rediscovery for both shipping modes;
- provider-finalized/C1-SealCommitted rediscovery;
- retryable and terminal post-seal recording;
- real Generic Host record-stage behavior;
- two-host acquire contention;
- original C116 plus the explicit post-seal C116 scenario;
- original runner versus fresh recovery-worker exclusion;
- exact fingerprint mismatch;
- displaced owner defer and completion fencing;
- fairness past an active claim;
- expired-job completion without extending delivery rights;
- current SQL/security metadata;
- C105, C124 and C126;
- migration Up/Down/Reapply.

Adjacent restored evidence:

| Proof | Result |
|---|---|
| Durable production work source | 1/1 PASS |
| Same-Job public SDK delivery | 1/1 PASS |
| Raw-ingress → same Job → SDK decode | 1/1 PASS |
| Current migration discovery/model | 1/1 PASS |

The first combined SDK E2E run had one disposed unpooled Npgsql fixture failure;
the direct and raw-ingress cases both pass as separate retained successor runs.
It is classified, not deleted.

## Mutation discrimination

| Mutation | RED |
|---|---:|
| Remove fresh post-seal discovery | 0/3 |
| Disable recovery-aware `RecordAsync` | 0/4 |
| Rethrow known acquire-lost-race conflict | 0/1 |
| Use wrong exact fingerprint after losing seal | 0/2 |
| Remove SQL fingerprint equality | 0/1 |
| Remove owner/generation guard from defer | 0/1 |
| Remove owner/generation guard from finalize | 0/1 |

Two trial mutants were rejected rather than credited:

- changing only the orchestrator's `ExactMismatch` mapping did not affect the
  repository-level mismatch test; the successor SQL-boundary mutant did;
- removing only generation comparison stayed green because independent owner
  comparison still fenced the stale claimant; separate owner+generation
  mutants then failed defer and finalize independently.

Every mutated product file was restored to its pre-mutation SHA before the
20/20 gate:

```text
RawExportAssemblyRuntimeInfrastructure.cs
  12B4698CBB37352F5200A1F07D6071F9499276B4BE124E4E81F09843D5540DC7
RawExportAssemblyOrchestrator.cs
  708ED11EC72890B6F9C67A9BE27A77CBBEB3BFEC13C17457751968771BCD8741
20260926120000_RawExportAssemblyPostSealRecovery.cs
  9C7F78486B9EF0BB1F8E6AA059E7B5F46855259BFC5CC1FF70F3A41BBCDF486C
```

## Exact correction write-set

Production/migration:

```text
src/TagEkyc.Contracts/RawExport/RawExportAssemblyContracts.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260926120000_RawExportAssemblyPostSealRecovery.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRepository.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs
```

Tests:

```text
tests/TagEkyc.IntegrationTests/RawExportAssemblyDurableWorkSourceTests.cs
tests/TagEkyc.IntegrationTests/RawExportAssemblyPostSealRecoveryMigrationTests.cs
tests/TagEkyc.IntegrationTests/RawExportDeliverySameJobEndToEndTests.cs
tests/TagEkyc.IntegrationTests/Tip88B4RawExportJobFoundationTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs
```

No API, SDK, Agent, raw-ingress, site-qualification, A3 partition/ledger,
P29-P36 or project/solution file changed.

## Evidence accounting and hashing

`failed_run_census_v1.tsv` classifies **28/28** failed results across fourteen
retained runs. Mutation REDs and superseded harness failures are not relabeled
as PASS.

`evidence_manifest_v1.tsv` names the hash basis per row:

- product/test SHA-256 values are over Git object content at correction commit
  `75c8903`;
- TRX/census SHA-256 values are over retained filesystem bytes.

The report and manifest exclude their own hashes to avoid a circular
dependency.

## Final boundary

```text
POST-SEAL RECOVERY CORRECTION       TECHNICAL PASS / READY FOR REVIEW
Correction commit                  75c890347a013840c67646ce3e221e5b615b6270
Seal/governance                    INTENTIONALLY STALE; NOT RE-MINTED
Process kill / OS restart          NOT PROVEN
C125                               OUT OF SCOPE / UNCHANGED
Layer 2                            PAUSED
Push / deploy                      NO / NO
```

After two independent reviewer PASS decisions, the next action is a separate
minimal evidence successor/re-freeze. It must not alter product semantics.
