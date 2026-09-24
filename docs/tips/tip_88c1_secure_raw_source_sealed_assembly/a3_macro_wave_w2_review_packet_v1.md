# A3 macro-wave W2 — authorized implementation final review packet v1

**Status:** CANDIDATE — one independent review for the whole wave  
**Date:** 2026-09-20  
**Scope:** exact 14 rows assigned to `W2_AUTHORIZED_IMPLEMENTATION`  
**Disposition:** 13 `CANDIDATE_PENDING_RATIFICATION`, 1 exact open proof gap  
**Non-claims:** not A3 PASS; no full suite, stage, commit, push, landing or production activation

## 1. One-wave disposition

| Family | Rows | Disposition | Exact basis |
| --- | --- | --- | --- |
| Provider-unknown semantics | BP17 | OPEN | Complete durable inspection→O21, incomplete/unknown→O18 and no invented O19 are proven separately, but no single current-source matrix varies complete/incomplete durable inspection under provider-unknown. W2 does not guess across that missing join. |
| Pre-body ingress origins | P03, P04, P05 | **CANDIDATE_PENDING_RATIFICATION** | Production origins now exist at caller ownership, transport grammar and unsupported canonical class gates. Real Kestrel/TLS→Agent checks exact public result, zero body reads and unchanged durable/provider residue. |
| Claim-state reachability | P12, P13 | **CANDIDATE_PENDING_RATIFICATION** | Production claim graph now reaches exact O12/O13. Shared Kestrel/TLS→Agent cases assert closed result shape, no body read and byte-stable residue. |
| Assembly shipping path | P29–P36 | **CANDIDATE_PENDING_RATIFICATION** | `DurableWorker` is an explicit fail-closed product caller requiring a supplied work source and production dependencies. Exact O29–O36 phase/residue tests and shared provider fixture cover the eight outcomes. |

The implementation unit was the mechanism family, the mutation unit was the production guard, and the requested closure unit remains each normative row. No per-row packet or infrastructure restart was created.

## 2. Current-byte baseline

`a3-w2-horizontal-current-baseline.trx`, SHA-256 `1DD276CA80EFEA1B16F7CA7C77832F10080D2E76D8DE420CDAB93D1C0E5BE27E`, passed **19/19** in one physical run:

- six Kestrel/TLS ingress cases: P03/P04/P05 plus adjacent P02/P07 controls;
- eight shared Kestrel/TLS claim cases O09–O16, including O12/O13;
- five assembly methods, including one shared MinIO fixture that executes P31/P32/P34/P35/P36 sequentially and reports every subscenario failure together.

The lightweight production-caller control `a3-w2-worker-current-baseline.trx`, SHA-256 `6CCE9C980F8263B71DC859D1F065DA04589B5D2A120BF11640CF80AB67E8381A`, passed **1/1** without PostgreSQL, MinIO or Kestrel.

## 3. Shared guard mutation

All thirteen row guards were changed in one mutant build across seven production files. `a3-w2-horizontal-current-guards-red.trx`, SHA-256 `916D6B02FF9C5C5486019C8719F06042357BEB673F10B401656C5705403DF5F5`, produced **9 pass / 10 fail**:

- P03, P04 and P05 each fail at their exact public outcome;
- O12 fails as O02 and O13 makes the Agent fail closed;
- the two O29 authority-barrier controls, O30 and O33 fail at exact mappings;
- the aggregate assembly method reports all five independent P31/P32/P34/P35/P36 failures in one invocation;
- nine adjacent scenarios remain green.

The earlier `a3-w2-horizontal-guard-matrix-red.trx` is `SUPERSEDED_RED`: it used the wrong compile flag and the pre-aggregation assembly method stopped at the first subscenario failure. It is retained in the census and manifest but is not closure evidence.

Mutant file hashes are recorded in `a3_preflight_recorded_mutants.tsv` under `macro_wave_w2`. The final sweep must report zero live matches.

## 4. Production assembly caller

`RawExportAssemblyHostedService` is disabled by default. `DurableWorker` requires an explicitly supplied `IRawExportAssemblyWorkSource`, role-scoped graph, authenticator and C2 provider; no fixture/provider fallback is selected in production.

The positive control proves acquisition → production `IRawExportAssemblyOrchestrator.ExecuteAsync` → durable work-source result recording. W2-G-ASSEMBLY-CALLER bypasses only the orchestrator call. `a3-w2-worker-caller-red.trx`, SHA-256 `DBBB3B934F691F9E7EAD8B3B0DF6603723E877D35C6B061FAB0F32E06FDC6C6D`, is **0/1 RED** at expected request versus null. This guard required only a build and a six-millisecond test, not another infrastructure cycle.

## 5. Exact restore and final joined gate

The seven shared-mutant production files and the worker were restored byte-exact. Key restored hashes:

| File | SHA-256 |
| --- | --- |
| `RawIngressBrokerTransactionFacade.cs` | `050D627D63B13621C442CDF0645B910A07225065F940E4EDDE8A93E737D120ED` |
| `RawExportSourceIngressEndpoints.cs` | `86B67AD6751E6270D7EA2BE1053E7C4338A74F94C8FAD65E10133DDFA5C8187F` |
| `CaptureRuntimeRawIngressAdmissionService.cs` | `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C` |
| `20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `F9CE348596EBCC4C79F64675A427AFB179BE5C31D9567C44580AB709ECB313F9` |
| `20260815120000_Tip88C1C1ResolverAssembly.cs` | `E9353063E47F6CA37444EF5A8740FAFB113C5189BBC845FA7B97D9CCFCFC1D3A` |
| `RawExportAssemblyOrchestrator.cs` | `5EDC9B2A79EA7CB98567B779A7A454BC62C478AC927C734489AE966716C235A0` |
| `RawExportAssemblySourceResolver.cs` | `FA907C07D8239582ACAF984713D2607A6D14F5163157EF61DDDC4732FB8D7D87` |
| `RawExportAssemblyHostedService.cs` | `EAEC409E6B20702F143EC0D0A45DDAB0432B2E5B9F0A23EE7D29A09431217CAA` |

After one rebuild, `a3-w2-horizontal-final-restored-joined.trx`, SHA-256 `877C94643D7442F8F26DE07EDE016F7E46CADE86F4A8E797EC30E4166E7B210B`, passed **20/20**, zero fail/skip. This is the only final W2 joined gate.

## 6. Machine evidence

- Scratch ledger: seven W2 entries, including the superseded predecessor.
- TRX inventory: 7/7 present.
- Failed-run census: 2 `EVIDENCE_RED`, 1 `SUPERSEDED_RED`, 0 unclassified.
- Mutant registry: eight W2 file hashes added; cumulative registry is 98 distinct hashes with zero live matches.
- BP17 remains `NOT IMPLEMENTED`; no synthetic Final or semantic inference is used.

Final machine counters:

```text
manifest_files=1907
delta_added=22 delta_modified=25 delta_deleted=0
trx_total=7
failed_runs_total=3
evidence_red=2 superseded_red=1 excluded_diagnostic=0
unclassified_failed_runs=0 outside_name_family_failed=0
mutants=98 live_mutant_matches=0
official_open_rows=51 assigned_once=51 duplicate=0 unassigned=0 extra=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

## 7. Requested single disposition

Ratify the thirteen candidate rows together: **P03, P04, P05, P12, P13 and P29–P36**. Keep BP17 open with the exact missing complete-vs-incomplete provider-unknown join.

Before review, authority-open remains **50 normative + 1 seam = 51** because candidate status is not ratification. If the thirteen rows are ratified together, it becomes **37 normative + 1 seam = 38**. A3 remains HOLD.
