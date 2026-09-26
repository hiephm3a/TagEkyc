# RAW-EXPORT POST-SEAL RECOVERY — BOUNDED FIX REVIEW PACKET V0.1

## Disposition

The original four accepted defects are corrected at TagEkyc commit
`75c890347a013840c67646ce3e221e5b615b6270`. The bounded review correction
requested after the first independent review is committed at
`b6f1e66a444cf34d89f5f85cfc3c77e91cc5d63b`.

| Area | Result |
|---|---|
| F1 fresh work-source rediscovers `AssemblySealed + SealCommitted` | **PASS** |
| F2 post-seal result recording avoids stale pre-seal revision | **PASS** |
| Candidate-to-acquire lost race is harmless polling | **PASS** |
| C116 exact concurrent replay joins committed result | **PASS** |
| Exact digest/authentication/fingerprint match | **PASS** |
| Claim owner + generation fencing | **PASS** |
| Mandatory NULL/shape rejection at recovery capability boundaries | **PASS** |
| Lease time sampled after row-lock acquisition | **PASS** |
| C116 winner-completes-before-loser-`RecordPending` join | **PASS** |
| Bounded recovery scheduling under a non-empty normal queue | **PASS** |
| Migration Up/Down/Reapply and security metadata | **PASS** |
| C105 exact ACL surface | **PASS** |
| C126 default graph + missing-authenticator branch | **PASS** |
| C125 | **OUT OF SCOPE / UNCHANGED** |
| Process kill / OS restart | **NOT PROVEN** |
| Layer 2 site measurement ownership | **PAUSED / UNCHANGED** |

The current runtime gate is **20/20 PASS, zero skip** and the migration
Down/Reapply gate is **1/1 PASS, zero skip**. Eleven credited mutations
fail at the named boundary. Two earlier attempted mutants stayed green and are
retained as `NON_DISCRIMINATING_MUTANT`; neither is credited.

## Repository posture

```text
Baseline product HEAD       c43bc9bb0c4bd08c35bab65c1fe0696f88ce3610
Characterization commit     578590a290dff4a97eb2e9d1c406038d5930f3fb
Correction commit           75c890347a013840c67646ce3e221e5b615b6270
Review correction commit    b6f1e66a444cf34d89f5f85cfc3c77e91cc5d63b
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
finalized. Same-owner stale generations are rejected independently from owner
fencing. Expired jobs may finish the already-sealed obligation without
changing `JobExpiresAtUtc`; end-to-end delivery authorization after expiry is
not claimed by this slice.

### Bounded review correction

All recovery capability functions now reject missing/zero identifiers,
missing/non-positive revisions or generations, malformed fingerprints and
malformed optional digests before reaching mutation logic. SQL comparisons
that protect exact identity, owner and generation use NULL-safe semantics.

Exact-claim, defer and claimed-finalize decisions sample
`clock_timestamp()` only after the preparation/claim rows needed for the
decision have been locked. A waiter therefore cannot use a lease timestamp
captured before it was blocked.

If a concurrent exact execution seals and finalizes after provider preparation
but before this execution records Pending, the orchestrator performs the same
digest/authentication/fingerprint-qualified committed join before returning a
conflict. The retained C116 proof controls both sides of that ordering; it does
not rely on thread-start timing.

Normal work remains preferred, but a singleton worker identity now schedules a
recovery-first probe every fourth poll. The mixed-queue proof leaves normal
jobs eligible and observes recovery on the bounded fourth turn, preventing a
perpetually non-empty normal queue from starving post-seal obligations.

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
the first correction's runs are added. The consolidated successor census,
including the independent pre-correction investigation, is now **83/83 across
34 retained failed runs**.

`runs/sql-lock-final/post-seal-sql-lock-final.trx` retains the database query
output for `raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint)`,
including its full function definition and metadata. The acquire-contention
claim therefore no longer relies on the earlier transcript that omitted this
direct lock/acquire link.

## Focused restored proof

`runs/correction2-runtime-restored-final-v4/post-seal-correction2-runtime-restored-final-v4.trx`
is the current **20/20 PASS** runtime gate. The migration proof is retained
separately as
`runs/correction2-migration-restored/post-seal-correction2-migration-restored.trx`
at **1/1 PASS**, so Down/Reapply cannot interfere with runtime tests sharing
the PostgreSQL fixture.

Together with the predecessor 20/20 gate, the current proof covers:

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
- expired-job completion without changing the job expiry timestamp;
- same-owner stale-generation rejection;
- direct NULL/shape capability rejection with no residue mutation;
- lock-wait expiry at exact claim, defer and finalize boundaries;
- pre-`RecordPending` C116 committed-result recovery;
- bounded recovery selection while normal work remains eligible;
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
| Remove pre-`RecordPending` committed-result recovery | 0/1 |
| Disable bounded recovery-first scheduling | 0/1 |
| Restore SQL NULL comparison bypass | 0/1 |
| Sample lease time before row locks | 0/1 |

Two trial mutants were rejected rather than credited:

- changing only the orchestrator's `ExactMismatch` mapping did not affect the
  repository-level mismatch test; the successor SQL-boundary mutant did;
- removing only generation comparison stayed green because independent owner
  comparison still fenced the stale claimant; separate owner+generation
  mutants then failed defer and finalize independently.

Every mutated product file was restored before the current 20/20 gate. Current
Git-object SHA-256 values at `b6f1e66` are:

```text
RawExportAssemblyRuntimeInfrastructure.cs
  EF58055A79040D24515474BAF04C85A45C9924CAB4904003B55DE10388B5E5DD
RawExportAssemblyOrchestrator.cs
  BB5003D913A3BDBE4BEC0098C209AAE531AE6491F55B70C78314743C5E6928CF
20260926120000_RawExportAssemblyPostSealRecovery.cs
  8C4CD8814724D3B924416DC650C82D1E48A0C804CB8518055FCCBDEF4A1542AA
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

The bounded review correction `b6f1e66` changes only the existing migration,
orchestrator, durable work-source implementation and the existing resolver
assembly test file. It adds no API, endpoint, schema object, engine, client or
SDK surface.

## Evidence accounting and hashing

`failed_run_census_v1.tsv` classifies **83/83** failed results across 34
retained runs. Mutation REDs and superseded harness failures are not relabeled
as PASS.

`evidence_manifest_v1.tsv` names the hash basis per row:

- product/test SHA-256 values are over Git object content at review correction
  commit `b6f1e66`;
- retained evidence SHA-256 values are over Git object content at the evidence
  snapshot named by the manifest.

The report and manifest exclude their own hashes to avoid a circular
dependency.

## Final boundary

```text
POST-SEAL RECOVERY CORRECTION       TECHNICAL PASS / READY FOR REVIEW
Correction commit                  75c890347a013840c67646ce3e221e5b615b6270
Review correction commit           b6f1e66a444cf34d89f5f85cfc3c77e91cc5d63b
Seal/governance                    INTENTIONALLY STALE; NOT RE-MINTED
Process kill / OS restart          NOT PROVEN
C125                               OUT OF SCOPE / UNCHANGED
Layer 2                            PAUSED
Push / deploy                      NO / NO
```

After two independent reviewer PASS decisions, the next action is a separate
minimal evidence successor/re-freeze. It must not alter product semantics.
