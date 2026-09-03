# TIP-88C1-B2-DURABLE-KEY-PROD — As-Built

**Version:** 0.2<br>
**Status:** PROOF BUILD CLOSED — READY FOR CONTROLLED COMMIT — NOT PRODUCTION<br>
**Date:** 2026-08-03<br>
**Baseline:** `23e0154805e468919e513bf04b0a24d374d1ef13`

## Authority binding

This working as-built records the Homeowner packets that corrected the recovery-context signature, external LOGIN-role proof
topology, CSPRNG-owner configuration, fixed current timing profile, `HISTORICAL_SUPERSEDED` active-T34 disposition and Phase-A partial-work
preservation. It does not authorize Raw BIO, R2, object storage, delivery, provider qualification, production activation,
deployment, staging, commit, push, merge or PR creation.

| Artifact | Pre-correction SHA-256 | Phase-A corrected SHA-256 |
|---|---|---|
| DK-PROD build dispatch | `B7A464A771CB4B368050401C2CD2E58BC7DF474757762EF3F90425CB960BA24D` | `0D4E5D6E59FF19421108A2BF0F5C659E53896B26473F1952C38BEC9AD6B1C359` |
| Executable state model | `C03DDCBB7A157A9D39BE5DDD3B46B217CBD2467356AB4472D7E1F0E6CAEE2A5F` | `AFC56ED767D59F5D74AD86462779B93670F35000548356BCA978F9269351F4BB` |
| CSPRNG prerequisite | `C42B8562588D83AED835F120CD878E49C2E67355E85D9697B414BD114C34AF37` | byte-identical and read-only |

The corrected DK-PROD and state-model hashes become authoritative only after every Phase-A preservation and checker gate passes.

## Phase-A lifecycle record

Partial M8 implementation had started before the fixed-timing/`HISTORICAL_SUPERSEDED` active-T34 contradiction was resolved.

Before Phase A, the partial implementation working tree was frozen in a byte-bound preservation manifest.

No partial implementation, test, migration, project or configuration byte changed while Phase A corrected the authority model.

After Phase A passed, targeted M8 implementation resumed from that preserved working tree and was reconciled to the corrected
deadline-only current profile.

The Phase-A start preservation baseline is outside the repository:

```text
D:\Task\Remote Signing\ReviewBundles\DK-PROD-Phase-A-Preservation-20260802T132415Z
phase_a_start_manifest.tsv SHA-256:
08685D4530A8639E5355B0D1645216C37F278558DB2E8E26764C9EBAA8A24402
```

It contains 48 recorded dirty/untracked paths: 31 preserved partial-M8 implementation paths, four authority paths (including
the clean/absent authority destinations) and preserved unrelated dirt. There were zero staged paths and zero unexplained
`src/` or `tests/` paths outside M8. No historical byte equality to an earlier STOP report is claimed.

## Fixed current timing profile

| Field | Fixed value |
|---|---:|
| PreparationLeaseDuration | 15 minutes |
| ResolutionInitialRetryDelay | 30 seconds |
| ResolutionRetryMultiplier | 2.0 |
| ResolutionMaxBackoff | 30 minutes |
| ResolutionDeadline | 24 hours |
| ResolutionMaxAttemptCount | 0 |
| CleanupInitialRetryDelay | 1 minute |
| CleanupRetryMultiplier | 2.0 |
| CleanupMaxBackoff | 1 hour |
| CleanupDeadline | 7 days |
| BoundedAeadOperationDuration | 30 seconds |

Configuration values are conformance inputs only. An absent value resolves to the fixed value; a present value must parse and
equal it exactly. Malformed or non-fixed values fail closed with `PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID`. Operational timing
always uses the fixed profile and cannot be changed through hot reload, SQL parameters, GUCs or caller/environment selection.

`ResolutionMaxAttemptCount = 0` means deadline-only resolution governance. T33 remains active. `FUTURE_GATE T34a/T34b`,
`FUTURE_GATE MaxAttemptExceeded` and `FUTURE_GATE #26` are reserved under
`DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1`. `ResolutionAttemptCount` remains persisted and increments for evidence, observability and
retry/backoff calculation.

## Corrected current-profile census

```text
mutating_edges=46
reachable_pairs=18
conceptual_heads=11 persisted_heads=10
conceptual_mappings=6 persisted_mappings=5
cartesian_cells=66 rejected_cells=48
active_tests=78
```

Unchanged structural census:

```text
callable SQL functions=15
owner-only helpers=2
trigger instances=5
unique/FK indexes=7
active DK-PROD readiness codes=21
legacy fixture readiness codes=3
authorized M8 paths=36
evidence domains=8
golden vectors=8
```

## Evidence chronology

### Task-0 historical baseline evidence

The following baseline evidence was completed before implementation and remains historical only:

| Command | Reported result |
|---|---|
| `dotnet build TagEkyc.sln -c Release` | 0 warnings, 0 errors |
| `dotnet ef migrations has-pending-model-changes --project src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj --startup-project src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj --context TagEkycDbContext --no-build` | no pending model changes |
| `dotnet test TagEkyc.sln -c Release --no-build` | 859 passed, 0 failed, 1 skipped; 860 total |
| pre-correction state-model checker with `--self-test` | exit 0 at the superseded 48-edge/79-test model |

These results do not replace final post-implementation validation.

### Partial pre-STOP implementation evidence

Before the fixed-profile STOP, the working implementation reported: Debug API/Infrastructure builds with 0 warnings and 0
errors; seven targeted DK-PROD architecture tests passed; six targeted DK-PROD integration tests passed; one existing CORE seed
test passed; pending-model was clean; migration apply/Down/reapply succeeded on PostgreSQL 16; capability workflow
prepare→ResultObserved→Activated succeeded; direct capability-table SELECT failed with SQLSTATE `42501`; and revocation hid the
active envelope. These are partial observations, not final closeout proof, and every mandatory gate will be rerun after the
current-profile reconciliation.

### Phase-A authority-correction evidence

Phase A passed before implementation resumed:

| Gate | Result |
|---|---|
| `python docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_state_model.py --self-test` | exit 0; ten exact `SELF_TEST_RED`; 46 mutating edges, 18 reachable pairs, 48 rejected cells, 78 active tests; exact 18 `PAIR` lines |
| independent Python recomputation of the §8 LP/NFC/SHA-256 vectors | 8/8 byte-exact |
| structural census | 15 callable functions; 2 helpers; 5 triggers; 7 unique/FK indexes; 21 active + 3 legacy readiness codes; 36 M8 paths; 8 domains; 8 vectors |
| `git diff --check` | exit 0; line-ending warnings only |
| CSPRNG prerequisite | `C42B8562588D83AED835F120CD878E49C2E67355E85D9697B414BD114C34AF37`, byte-identical |
| Phase-A preservation comparison | zero mismatches, zero added paths, zero removed paths, zero staged paths |

Final Phase-A authority hashes:

```text
DK-PROD: 0D4E5D6E59FF19421108A2BF0F5C659E53896B26473F1952C38BEC9AD6B1C359
state model: AFC56ED767D59F5D74AD86462779B93670F35000548356BCA978F9269351F4BB
debt registry: 36E8C7B3E4F2D96C2752D1B0D2BDB277EA5AA9242EDBF27603145ED5935E5B39
```

The final end-manifest and preservation-comparison hashes are stored beside the start evidence and reported externally after
the as-built bytes are fixed, avoiding a self-referential hash cycle.

### Final post-implementation closeout evidence

Pending. It must include the corrected checker, pending-model, Release build, full suite census, migration apply/Down/reapply,
catalog/ACL/readiness evidence, active mutation proofs, 8/8 vectors, M8 path proof, stale-current-profile sweep and
`git diff --check`.

## Outcome vs intent — working

| Intended outcome | Current result | Status | Carry-forward |
|---|---|---|---|
| Durable wrapped-key reservation/recovery foundation | Partial M8 implementation preserved | IN PROGRESS | complete 78-test proof build |
| Deadline-only fixed current profile | Authority model corrected and preservation-proven | PHASE A PASSED | reconcile SQL/C# and tests under M8 |
| Positive max-attempt profile | Explicitly not active | FUTURE_GATE | `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1` |
| Raw BIO/R2/object/delivery | Not implemented or authorized | OUT OF SCOPE | later separately authorized slices |

## Proof-build closeout evidence

Final independent verdict: **PASS — READY FOR CONTROLLED DK-PROD CLOSEOUT**.

| Gate | Final result |
|---|---|
| reviewed proof bundle | SHA-256 `3456D24FA6A37889C892858DDC3EA8997D11492C4C647A5337BF9B9C634DFC64` |
| state-model checker | exit 0; 46 mutating edges; 18 reachable pairs; 48 rejected cells; 78 active tests; ten `SELF_TEST_RED`; exactly 18 `PAIR` lines |
| independent golden-vector recomputation | 8/8 byte-exact |
| Release build | 0 warnings; 0 errors |
| pending-model gate | clean; no changes since the last migration |
| full sequential Release suite | 939 passed; 0 failed; 1 intentional skip; 940 total |
| intentional skip | `Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors` only |
| DK-PROD integration | 74/74 passed; 0 failed; 0 skipped |
| DK-PROD architecture | 6/6 passed; 0 failed; 0 skipped |
| migration apply/Down/reapply | passed; no orphan overload or dropped deployment LOGIN role; CSPRNG prerequisite preserved |
| active mutation obligations | 55/55 `PASS_MUTATION_RED_AND_RESTORED` |
| proof #26 | `FUTURE_GATED_NOT_ACTIVE` under `DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1` |

Proof #11 was corrected to discriminate the live-lease positive-absence path. Proof #33 was corrected to assert the exact
composite-FK constraint name and zero-write invariants. Proof #36 used the corrected session-scoped scratch mutation; no
permanent production or test byte was changed by that scratch mutation. Proof #48 completed the count-neutral positive-control
plus mutation evidence, with `DurableKeyCustodyOptions` restored to SHA-256
`7BB372519719B43F2BDD9DB5A957B545CA4AD1010F3D19E5C0398DBF022012E7`.

This closeout does not provide production-activation evidence. Production activation remains blocked on a qualified native key
provider and deployment-owned CSPRNG owner/provisioning. It does not implement, authorize or claim Raw BIO access, R2,
object storage, delivery or production activation.
