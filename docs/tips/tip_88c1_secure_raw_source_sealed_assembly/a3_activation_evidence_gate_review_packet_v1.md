# A3 activation evidence gate — bounded review packet v1

## STATUS

`IMPLEMENTED / BOUNDED PASS / A3 HOLD`

This packet implements only the pre-Wave-4 startup fuse. It closes and reclassifies no A3 row, changes no durable activation authority, starts no Wave 4 work and grants no production activation authority. The canonical authority-open census remains **48**.

## FILES CHANGED

Product/build surface:

- `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeStartup.cs`
- `src/TagEkyc.Api/Program.cs`
- `src/TagEkyc.Api/TagEkyc.Api.csproj`
- `tools/Generate-CaptureRuntimeActivationEvidenceSeal.ps1`
- `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_evidence_seal_v1.tsv`
- `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_macro_wave_w2_ratification_v1.md`
- `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_evidence_failed_run_census_v1.tsv`
- `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_evidence_generator_self_test_v1.txt`

Focused/compatibility tests and cumulative mutation registry:

- `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs`
- `tests/TagEkyc.UnitTests/Tip88C1C6BA1StartupTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1StartupCompositionTests.cs`
- `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_preflight_recorded_mutants.tsv`

No CaptureAgent source changed.

## EVIDENCE SEAL DESIGN

The Api build target runs `a3_macro_wave_partition_verify.ps1` before compilation. That verifier currently reports:

```text
official_open_rows=48
partition_rows=48
assigned_once=48
duplicate=0
unassigned=0
extra=0
```

Only after that verifier succeeds does the generator hash the live partition and reconciliation ledger and emit an immutable provider into `obj/.../Generated/CaptureRuntimeActivationEvidenceSeal.Generated.cs`. Runtime code consumes compiled constants and never opens a governance document.

The ratification-owned descriptor pins both identities from the most recent explicit Homeowner ratification (W2), plus its manifest and ratification record:

```text
approved partition = 8DC5F521999AB9E5F6DEBC66D3343C3FB616D69C9A0F6AFFE82612464EADBF2B
approved ledger    = E742C10986253B421F85DCC89786884D3C708AEB5717155056555B0BE9EB7042
approved open rows = 48
manifest           = 509DE41AC51002A25E50691005C2AE9C92EAF7103AE08CBD7439F2FB42489754
ratification       = F77558E28FBAD9A0277EC9132559DCF4DC76F7099307FF3DEBD531E71559D002
```

The ratification record now exposes and the generator parses exactly one occurrence of each machine-readable field:

```text
A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 48
A3-Reviewed-Manifest-SHA256: 509DE41AC51002A25E50691005C2AE9C92EAF7103AE08CBD7439F2FB42489754
```

The generator compares exact field values. It does not accept a count merely because the same digit occurs elsewhere in the record, and it does not accept an artifact that lacks the exact `RATIFIED` decision. Duplicate or missing fields fail closed.

The build pair currently hashes to:

```text
build partition = 967AEE2D468818C4CDC82D6E103E714B513EF5363B29A6AA9CE6A64921D54C92
build ledger    = 613667D1CB4D9058FC36BE0BA12BEED01E705D77CFD5FC857C71FAFB4190D24F
build open rows = 48
```

The post-W2 identity drift is therefore compiled as an invalid/stale seal, not silently re-minted. A normal build can verify and embed the pair but cannot edit the ratification-owned descriptor. Re-minting means changing that descriptor to a new verifier-passing pair and pinning the manifest plus the explicit Homeowner ratification record created by the same census-changing authority act.

## STARTUP INVARIANT

`CaptureRuntimeStartup.SelectAsync()` reads the durable route state once. Immediately after a valid `Activated` selection and before pepper/readiness work it applies exactly one gate:

```text
missing, malformed or partition/ledger/count mismatch
    => CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID

valid seal and AuthorityOpenRowCount != 0
    => CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE

valid seal and AuthorityOpenRowCount == 0
    => continue to the existing A3 operational-readiness gate
```

`Program.cs` still invokes `SelectAsync()` before route mapping and listener startup. There is no per-request/background re-check, config flag, environment bypass, development exception, warning-only mode or fallback to Prepared.

## FOCUSED TEST RESULTS

Final current-byte gate:

```text
a3-activation-evidence-final-restored-green-v2.trx
7/7 PASS, zero skip
SHA-256 EE9B952351D09B6BE2ECD089FAF1EB6E018170D4ED13FDE411D34F2AEAD35EEE
```

The seven executions are four focused methods, with the invalid method parameterized as missing, malformed, partition mismatch and ledger mismatch. Every test crosses the real WebApplicationFactory startup-selection boundary.

Compatibility tests:

```text
a3-activation-evidence-unit-startup-compat.trx             13/13 PASS
SHA-256 C415CB9DB485F4DD05B717A4252351A78B1644B65F596F45B384F13D4854C809

a3-activation-evidence-startup-composition-compat.trx       1/1 PASS
SHA-256 E918B7EBC4B6D9CFA9C102638E0941D104D45E138EBF05D42E2A78A992F33CBA
```

The earlier focused diagnostics are retained rather than laundered: 5/7 and 4/7 exposed a malformed 60-character test provenance hash and an intermediate missing-provider composition issue; 6/7 then showed the zero-open host required its Activated worker fixture to be isolated from this startup-only proof. None is closure evidence. Their result-neutral names are `attempt-a1`, `attempt-a2` and `attempt-a3`; the former `focused-green*` names were removed because the files are failed runs.

## GENERATOR SELF-TEST

The exact-field parser was exercised directly without running product or integration tests. Raw output is retained in `a3_activation_evidence_generator_self_test_v1.txt`:

```text
valid RATIFIED record + exact count                 PASS
record says count 1 but contains digit 0 elsewhere PASS (rejected as intended)
non-ratification artifact + exact manifest SHA     PASS (rejected as intended)
generator_self_tests                               3/3
```

The partition verifier's pinned census and wave totals must be updated in the same governance transaction as every later ratification. A census change is therefore deliberate and cannot drift through as an unreviewed build.

## AFFECTED SENTINELS

One horizontal invocation ran the three named startup assertions and all six `BrokerUsesOneTransactionAndDerivedActor` scenarios.

```text
first invocation   1/9 PASS, 8 bootstrap failures
                   NpgsqlException/EndOfStream before test bodies
                   excluded environment diagnostic

final current-byte  9/9 PASS, zero skip
TRX                 a3-activation-evidence-final-affected-sentinels.trx
SHA-256             3A2BCDB576EECA45FBC506DF5A22204EAE556ED49106C0203D4E76861F4A19B0
```

No Wave 1–3 suite or full suite was run.

## FAILED-RUN CENSUS

Every failed TRX produced by this bounded gate task is classified in `a3_activation_evidence_failed_run_census_v1.tsv`:

```text
failed_runs_total       = 5
evidence_red            = 1
superseded_red          = 3
excluded_diagnostic     = 1
classified_failed_runs  = 5
unclassified_failed_runs= 0
```

- `attempt-a1` and `attempt-a2` record the malformed synthetic provenance and intermediate missing-provider composition failures.
- `attempt-a3` records the unrelated Activated-worker fixture dependency discovered only after the valid-zero seal passed.
- `affected-sentinels` is the PostgreSQL bootstrap diagnostic that failed before the business assertions; its unchanged successor passed 9/9.
- `zero-guard-red` is the sole `EVIDENCE_RED`, tied to `AE-G-ZERO-AUTHORITY-OPEN`.

The census, rather than filenames or prose, is authoritative for the outcome and evidentiary role of each run. New diagnostic attempts use result-neutral names.

## PREPARED NON-REGRESSION

The evidence provider is not read for `Prepared`. The dedicated real-host test starts successfully with a valid incomplete seal, and the pre-existing Prepared graph sentinel remains part of the focused class. No Prepared route, handler, owner/provider construction or durable cutover state changed.

## MUTATION / RESTORE

One narrow mutation changed only the zero predicate:

```text
AuthorityOpenRowCount != 0  ->  AuthorityOpenRowCount >= 0
mutant SHA-256 44A4A6A0AA0175F7130E24300DE5B060AC2F589F254FF2C7CE74E29FE161BBCB
```

Result:

```text
a3-activation-evidence-zero-guard-red.trx
6 PASS / 1 FAIL
only ActivatedWithValidZeroOpenActivationEvidenceStarts is RED
failure: CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE
SHA-256 75EF826EC723BC88EBF2B73E9803A7D8E6849A5E8D89ED3924F0D6FF9C476174
```

The source was restored byte-exact and rebuilt:

```text
CaptureRuntimeStartup.cs restored SHA-256
F972AB66B9FBF748BD378A970E3A2639971958CC2A971586FFF1D28746B8B38D
```

The restored focused gate is the 7/7 result above. The mutant hash was added to the cumulative registry; live source does not carry that hash.

## STAGED / CONFLICTED

```text
Server staged=0 conflicted=0
Agent  staged=0 conflicted=0

recorded mutants=101 distinct=101
live mutant matches=0 across 1,332 live controlled files
runtime governance-file reads in src/=0
```

Existing unstaged/untracked work is preserved; this packet claims only staged/conflicted counts.

## NEW FINDINGS

- The live governance pair has changed identity since the last explicit W2 ratification even though the open count remains 48. The compiled default seal therefore fails `Activated` as `...EVIDENCE_INVALID` until a later explicit Homeowner ratification re-mints it. This is intended fail-closed behavior.
- The first sentinel invocation encountered one shared PostgreSQL bootstrap stream failure. The same unchanged test binary passed 9/9 on the single grouped retry.
- No A3 row or product semantic defect was discovered or closed.

## FINAL VERDICT

```text
A3 ACTIVATION EVIDENCE GATE     IMPLEMENTED / BOUNDED PASS
Prepared                        unchanged by the gate
Activated                       fails unless seal is current, valid and open count == 0
Partition + ledger provenance   both pinned
Re-mint authority               explicit Homeowner ratification only
A3 census                       unchanged at 48
A3 overall                      HOLD
Wave 4                          not started
```
