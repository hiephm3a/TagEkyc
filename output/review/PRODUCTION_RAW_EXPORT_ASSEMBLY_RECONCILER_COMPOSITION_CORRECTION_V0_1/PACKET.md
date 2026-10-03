# Production Raw Export Assembly Reconciler Composition Correction — Review Packet V0.1

## Disposition

`TECHNICAL PASS CANDIDATE — INDEPENDENT GPTW + CC REVIEW REQUIRED`

Baseline is `1be2891b9857a1017630985660ef967eac5eb055` on `tip-88a-raw-export-policy-catalog-build`, seal revision 27. The candidate is not committed, re-frozen, or pushed.

## Implementation summary

Production `DurableWorker` no longer asks root DI for `IProvisionalObjectReconciler`. The resolver receives an internal bounded-scope factory. Its Production implementation delegates only to `CaptureRuntimeCustodyProviderScopes.OpenReconcilerAsync()`, resolves the scoped reconciler, and owns the returned role scope.

The read operation declares the role scope before the exact read. Reverse async-disposal order therefore guarantees:

```text
role scope open
→ exact read open
→ ciphertext read + framed verification + consumer complete
→ exact read dispose
→ role scope dispose
```

The Production registration is exactly one singleton `CaptureRuntimeAssemblyReconcilerScopeFactory`. Root/global `IProvisionalObjectReconciler` count is zero. FixtureProof gets a minimal scoped fixture adapter, and that type is absent from Production registrations.

The pre-existing blind test now executes `isProduction:true` with valid secret refs instead of exercising the non-Production branch.

## Exact write-set

```text
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyReconcilerScope.cs        new
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblySourceResolver.cs
tests/TagEkyc.IntegrationTests/RawExportAssemblyReconcilerCompositionTests.cs   new
tests/TagEkyc.IntegrationTests/ProductionCompositionCorrectionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs
```

No schema, migration, endpoint, protocol, DB role/login/privilege, provider/storage implementation, credential model, A3/Layer-2 semantic, KEK, Claim Provider, SignFlow, or seal change exists.

## Focused proof

```text
focused-restored-final.trx        6/6 PASS, zero skip
custody-authority-restored-final  35/35 PASS, zero skip
```

The focused set includes:

- exact Production seam descriptor/type/lifetime;
- real `Program.cs` Production graph with `ValidateOnBuild=true` and `ValidateScopes=true`;
- root reconciler registration count zero;
- Production fixture adapter absence;
- unknown pre-registration rejection;
- FixtureProof compatibility;
- corrected Production RuntimeOwners blind test;
- lifetime ordering through the full framed consumer.

The custody set exercises the existing production mechanism rather than a new fake: exact Writer/Reconciler/Lifecycle actors, non-combined memberships and S3 credentials, cross-role SQL privilege rejection, stage-right manifest, `Enlist=false`, and fail-closed invalid owner/capability cases.

## Discriminating mutation

The resolver was temporarily returned to the predecessor root dependency.

```text
mutant-root-di.trx            0/1 RED
reason                        Program.cs ValidateOnBuild cannot resolve
                              IProvisionalObjectReconciler for resolver/orchestrator
inverse restore SHA           D3C27C04F77072E65086EBDFD37D9F5516B0C8AB5097B1EE1B74C46626B2C188
mutant-root-di-restored.trx   1/1 PASS
```

Thus the full-Program assertion is load-bearing and does not pass merely because a reduced `ServiceCollection` was used.

## Affected and architecture screens

```text
affected-restored-final.trx   77/78 PASS
arch-restored-final.trx       12/13 PASS
```

The two failures are not candidate regressions. Each was rerun from a detached exact-baseline worktree at `1be2891...` and failed with the identical assertion:

- C118: expected `ExistingMatch`, actual `LeaseLost`;
- architecture overload census: expected one overload, actual two.

An A3 retained-seal check selected earlier also fails both candidate and exact baseline with expected `AuthorityInvalid`, actual `PreparationConflict`. All occurrences are retained and classified in `FAILED_RUN_CENSUS.tsv`; none is hidden or called transient. Their deeper diagnosis is outside this bounded composition correction.

## Builds

```text
TagEkyc.Infrastructure      PASS / 2 pre-existing nullable warnings / 0 errors
TagEkyc.Api                 PASS / expected pre-review seal-drift warning
                                 + generated nullable warning / 0 errors
TagEkyc.IntegrationTests    PASS / 0 errors
```

No full solution suite was run. Focused, affected, authority and architecture gates were used as required.

## Real Production-shaped lab

Candidate bytes were deployed to the existing synthetic Ubuntu lab. Existing broker and Development API remained active.

The lab initially failed closed on obsolete direct SELECT grants left on `tagekyc_api_login` by the older lab setup. The rev27 migration and exact two role memberships were already present. The lab correction revoked only those three direct legacy grants; it did not grant a new privilege. Effective access remains inherited through exactly `tagekyc_runtime` and `tagekyc_application_persistence`.

Final retained proof:

```text
Production API                         active and stable
Production broker                      active
Development API                        active
Assembly topology                      DurableWorker
RuntimeOwners                          Writer, Reconciler, Lifecycle
legacy global ObjectCustody            Disabled
reconciler DB actor                     tagekyc_raw_export_reconciler_login
reconciler membership                  tagekyc_raw_export_reconciler only
/health                                200
/readiness                             503 at four later legitimate data/ACL/catalog gates
root reconciler resolution errors      0 after current startup
```

Proof 7 is intentionally split into two non-circular conditions:

1. **Review condition — satisfied by this candidate.** The real Production process starts and remains active, `/health` returns `200`, readiness reaches only later legitimate gates, and no root `IProvisionalObjectReconciler` resolution error occurs.
2. **Post-re-freeze push condition — not yet executable.** After an independently reviewed successor seal is minted, the real Production process must be rerun and `/health/site-transport-qualification` must return exactly `503 Missing`. A continuing `200 NotRequired` result blocks push.

The current site-specific endpoint reports `200 NotRequired` because this pre-review working-tree build intentionally has no valid successor seal. This packet does **not** claim site qualification PASS or satisfy the post-re-freeze push condition. Re-freezing before independent review remains forbidden.

## Evidence accounting

The census contains 13 TRX runs, 147 executions, 138 passes, 9 classified failures, zero skipped and zero unclassified. The nine failures consist of one intentional product mutation and eight retained candidate/baseline observations covering three predecessor failures.

## Governance stop

```text
Commit       NO
Re-freeze    NO
Push         NO
Backup/PITR  remains paused at the Production Raw Export E2E boundary
```

STOP for independent GPTW and CC review.
