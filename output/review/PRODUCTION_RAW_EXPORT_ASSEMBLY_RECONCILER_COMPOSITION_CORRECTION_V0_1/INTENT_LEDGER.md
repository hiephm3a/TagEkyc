# Production Raw Export Assembly Reconciler Composition — Intent Ledger

Status: IMPLEMENTATION COMPLETE / INDEPENDENT REVIEW REQUIRED

Baseline: `1be2891b9857a1017630985660ef967eac5eb055` (seal revision 27).

## Intent

Close the bounded Production `DurableWorker` composition defect without registering a root/global `IProvisionalObjectReconciler`. Assembly exact-source reads must reuse the already-ratified reconciler-owned role scope.

## Accepted decisions

| Decision | Reason | Scope impact | Non-claim |
| --- | --- | --- | --- |
| Add one internal bounded reconciler-scope seam | The assembly resolver needs to hold the role scope across exact read and framed consumption without depending on a scoped provider outside its lifetime | One factory contract and one owned async scope contract | No new provider or credential selector |
| Production seam delegates only to `CaptureRuntimeCustodyProviderScopes.OpenReconcilerAsync()` | Reuses the existing actor, membership, ACL, stage-right and provider qualification path | Production registration is one exact singleton implementation | Does not create a second custody graph |
| FixtureProof uses a minimal fixture adapter | Preserves existing fixture assembly tests without requiring Production RuntimeOwners | Non-Production registration only | Fixture adapter is absent from Production registrations |
| Resolver owns both async disposals | C# reverse `await using` disposal keeps the role scope alive through the exact read and consumer | Exact read disposes before role scope | No stream or scoped provider escapes the operation |

## Rejected branches

| Branch | Disposition | Reason |
| --- | --- | --- |
| Register root/global `IProvisionalObjectReconciler` | Rejected | Violates the ratified three-role custody boundary |
| Enable legacy global `ObjectCustody` | Rejected | It is not a Production-qualified workaround and remains `Disabled` in the lab |
| Add provider, storage path, credential model, role, ACL, schema, migration, endpoint, or protocol | Rejected | Outside the bounded correction and unnecessary |
| Weaken A3, Layer 2, KEK, claim-provider, persistence, or site-qualification semantics | Rejected | No such semantic change is required |
| Fix predecessor A3/C118/architecture assertions in this slice | Deferred | Exact baseline controls fail identically; they are not regressions caused by this correction |

## Proof matrix

| Ratified proof | Evidence |
| --- | --- |
| Production composition with no root reconciler | `RawExportAssemblyReconcilerCompositionTests`; real `Program.cs` graph with `ValidateOnBuild=true`, `ValidateScopes=true` |
| Exact Production seam type/lifetime | Exactly one singleton `CaptureRuntimeAssemblyReconcilerScopeFactory`; fixture adapter absent; root reconciler registration count zero |
| Existing reconciler authority | `custody-authority-restored-final.trx` (35/35); lab principal is `tagekyc_raw_export_reconciler_login` with only `tagekyc_raw_export_reconciler` |
| Scope lifetime through full verification/consumer | `Assembly_exact_read_keeps_reconciler_scope_alive_through_framed_consumer_and_disposes_in_order` in focused 6/6 |
| Fail closed on invalid ownership/roles/ACL | Existing custody role-scope gates in `custody-authority-restored-final.trx` |
| Root-dependency mutation bites | `mutant-root-di.trx` 0/1 RED at real `Program.cs` container validation |
| Byte-exact restore | Resolver SHA before mutation and after inverse restore: `D3C27C04F77072E65086EBDFD37D9F5516B0C8AB5097B1EE1B74C46626B2C188`; `mutant-root-di-restored.trx` 1/1 |
| FixtureProof regression | Fixture adapter registration plus existing assembly affected gate; no Production fixture registration |
| Real lab — review condition | `lab-production-proof.log`: Production API active, health 200, root resolution errors zero, legacy ObjectCustody disabled |
| Real lab — post-re-freeze push condition | Not yet executable: after successor seal generation, rerun the real Production process and require `/health/site-transport-qualification = 503 Missing`; continuing `200 NotRequired` blocks push |

## Baseline controls

Three assertions outside this correction fail identically on the exact rev27 baseline and candidate:

- A3 retained seal: expected `AuthorityInvalid`, actual `PreparationConflict`;
- C118 replay: expected `ExistingMatch`, actual `LeaseLost`;
- composition architecture: expected one raw-ingress composition overload, baseline contains two.

They are retained and classified as `PREEXISTING_BASELINE_FAILURE`; no claim about their deeper root cause is made here.

## Governance

No product commit, evidence commit, seal re-freeze, or push is authorized before independent GPTW and CC review PASS. After both reviews PASS, product/evidence commits and successor seal generation remain separate Homeowner-authorized actions. Push additionally requires the post-re-freeze Production-process condition above.
