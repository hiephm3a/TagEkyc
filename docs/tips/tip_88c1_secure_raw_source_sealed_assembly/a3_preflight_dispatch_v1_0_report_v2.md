# A3 §20 preflight correction — independent-review candidate

Status: `PREFLIGHT_CORRECTED / E01_NOT_AUTHORIZED`. This report was created **after** the baseline manifest. It is the sole known post-baseline Git-visible delta and must be enumerated with its raw SHA in the first E01 cluster delta; it is not silently whitelisted by the verifier. The earlier `a3_preflight_dispatch_v1_0_report.md` is marked `SUPERSEDED` and its 1,301-file manifest is not evidence-complete.

No product/test source was changed. No E01, PostgreSQL product, mutation-product or full-suite test was run. No stage, commit, push, A4 or production activation occurred.

## Authority and correction

The ratified A3 v0.6 parent remains `3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813`, with its five bound companions and the approved no-client-registry-FK correction unchanged. Reconciliation SHA remains `E56EA6BA0BF59C828A9C2036E4D35027A154BA46D7D7B1F798F382D96623D484`. The two stale historical current/restored labels were changed only to `SUPERSEDED` in the prior preflight; all 11 audited labels now have either live bytes or an explicit successor: `stale_current_labels=0`.

The E01/B2 production-path declaration below corrects an ambiguity in the prior report. `Program.cs:167` maps the public E01 routes. A fake authenticator in some existing TestServer tests limits what *those tests prove*, not whether the production route exists. A repository method with no `src` caller above it is the separate B2 subject-consent write, not E01-W.

## Manifest reconstruction and self-check

[`a3_preflight_manifest_verify.ps1`](a3_preflight_manifest_verify.ps1) now rebuilds and verifies the manifest from both repos' Git-visible files **plus every live `.trx` referenced by both reconciliation and implementation ledgers**. It scans absolute, repo-relative, abbreviated and bare `.trx` mentions by basename, resolves against files in the two repos, requires a unique destination, and checks any explicit `tests/<project>/TestResults/<name>` path against that destination. An ambiguous basename fails with `AMBIGUOUS_TRX_REFERENCE`. The [reference-resolution inventory](a3_preflight_trx_reference_resolution.tsv) records all 334 distinct references and ledger locators; the [missing-evidence list](a3_preflight_referenced_trx_missing.tsv) has its header and zero missing rows. Repeated mentions of one TRX resolve to one manifest path. Nothing missing is silently counted as covered.

The script builds the [baseline manifest](a3_preflight_baseline_manifest.tsv) after the matrix, mutant registry, resolution inventory, missing-evidence list and verifier are complete. It excludes only the manifest and its [sidecar](a3_preflight_baseline_manifest.tsv.sha256) from self-inclusion. Source, tests, migrations/SQL, project files, configuration, scripts, documents, untracked candidate files and ledger-referenced ignored TRX are retained. The first `Verify` run against the old manifest failed exactly `REFERENCED_TRX_OMITTED` on `a3-e01-reviewed-restored.trx`; it was not used as a green baseline.

For the required negative self-check, a temporary copy of the rebuilt manifest was made with exactly the live `a3-e01-reviewed-restored.trx` row removed. The evidence file itself was untouched. `Verify` returned `negative_self_check=RED REFERENCED_TRX_OMITTED`; the unmodified rebuilt manifest then returned GREEN with the counts below. Temporary copies were removed. This exercises the verifier's reference-derived coverage check, not a product mutation.

Four specifically reported omissions now appear with their raw bytes in the new manifest: `a3-e01-reviewed-restored.trx` `D4B17BECF39567D1F00EE70460299FEC6909B04C3A848D0FFBC29B3DC2ECBBBD`, `a3-r20-principal-restored.trx` `BE277A2357C99C13B59497C0F0CF36045544A860D6EABA63A9003384B831C327`, `tip88c1-a3-scopes-v5-final-unit.trx` `676EC99E3A8BF2B3D48650FCF7BC2DBFDEA666D248F3472D9589EAFDC9F9BB8A`, and `tip88c1-a3-re01-final-regression.trx` `96775D5FBA7B4B22F7358E756E31D1F02C27FF94D5B0C7D4B2FAB129D3D4CC5B`.

## Raw pre-report gate output

```text
matrix_rows=76
matrix_columns=18
matrix_required_blank_cells=0
open_normative=75
open_seam=1
git_visible_server=1079
git_visible_agent=123
ledger_trx_references_distinct=334
referenced_trx_existing=334
referenced_trx_missing=0
referenced_trx_ambiguous=0
referenced_trx_in_manifest=334
referenced_trx_omitted=0
server_manifest_files=1387
agent_manifest_files=147
manifest_files_total=1534
recorded_mutants=39
live_mutant_matches=0
stale_current_labels=0
staged_server=0
conflicted_server=0
staged_agent=0
conflicted_agent=0
baseline_manifest_sha=94D6E060D8785FFC60172E5B6857224E71CD824523B707BA452E3C612D8A1E86
matrix_sha=C7C84BE7FBAD3F319702D397D216F26C70E6E638182B4DADF8B2A27CB62B121C
mutant_registry_sha=3F36D4D16F6715D308B9798F52609D6893CBB988D6B5AD920F82833D43317EE7
verifier_sha=160A63018BDAB091D227705885593EB2BC1CAF7C93D47E74B19F617755CC7877
negative_self_check=RED REFERENCED_TRX_OMITTED
restored_verifier=GREEN
```

`report_sha` is emitted externally after this file is written; embedding its own SHA here would be self-referential. The final report is post-baseline and must be shown as a one-file delta, not called manifest drift-free. The matrix has 76 unique row IDs, 18 columns, no blank required cells, 73 preliminary `TEST_EXTENSION` and 3 `SEMANTIC_BLOCKER` (P03–P05). Census is unchanged: 75 normative + 1 seam open; A3 overall remains HOLD.

## E01 four-part declaration — corrected, awaiting Homeowner review

1. **Production caller/path.** E01-R and E01-W both have public HTTP routes mapped by ordinary `TagEkyc.Api/Program.cs`. E01-W is `POST /api/ekyc/source-consent-references/{referenceId}/withdraw` → `IApiKeyAuthenticator` and BusinessConsumer checks → `RawSourceConsentApplicationService.WithdrawAsync` → `RawSourceRetentionGateway.WithdrawAsync` → `tagekyc.raw_source_withdraw_consent_reference`, operation domain `E01-W`. B2 subject-consent withdrawal is a **different write**: `IRawExportSubjectConsentRepository.RecordSubjectConsentWithdrawnAsync` → `EfRawExportSubjectConsentRepository` → `tagekyc.raw_export_append_subject_consent_withdrawn` → synchronization hook, operation domain `B2-Withdrawal`. A fresh `rg` across all `src` found only the B2 method's interface declaration and implementation, no caller above the repository. B2 readers of `ResolveSubjectExportConsentForAuthorizationAsync` are not B2 write callers. The E01 pilot may therefore exercise E01-R/E01-W via public HTTP and B2 repository→SQL synchronization, but cannot label E01-W a public B2 workflow. Activated R20 Issue and R1/R3/R4/R5/C1/C3 checkpoints have separate production paths and remain subject to explicit provider/owner configuration.
2. **Production-real components and test doubles.** The claimed E01 subject is the actual endpoint/authenticator/application/gateway and PostgreSQL transactions, functions, locks, constraints, ACL and durable rows. Synthetic non-patient session/profile/policy/recorder data establish preconditions only. Two real PostgreSQL connections and an adversarial lock holder may schedule a race; the SUT operation itself must be the real Record/Withdraw/Issue/checkpoint call. Existing `Tip88C1C6BA3ConsentRetentionTests.Start()` is a custom TestServer and defaults to a fixed-identity authenticator; `RunR20` directly invokes production SQL and `PrelockRetentionRead` manually takes locks. Their scope is explicitly narrower than ordinary `Program.cs`, real credential authentication or public R20. A real-key variant exists for credential claims. Synthetic object/key providers are test-only, explicitly supplied for Activated; no product fallback is authorized.
3. **Why each double cannot/should not be replaced.** Synthetic/non-patient data are required by the authority boundary. Controlled lock holders make the PostgreSQL interleaving deterministic and must be observed with `pg_locks` plus durable independent-connection reads; they do not impersonate the function under test. The fake authenticator *can* be replaced and must not support a real-auth claim. Direct SQL helpers are acceptable for setup or SQL-only proof, never a replacement for the mapped public caller. No canned HTTP result, in-memory lock model, fabricated provider outcome or synthetic composition is permitted as proof of the corresponding production behavior.
4. **This cluster proves / does not prove.** After approval and execution, the ten E01 rows may close only with exact test identity, targeted RED/restored GREEN, real PostgreSQL serialization, post-lock clock, actor/client/session/principal lineage, graph/ACL and residue, and the existing approved no-client-registry-FK correction. Public E01-R/E01-W HTTP, including authentication where claimed, must be driven as such rather than inferred from the older fake-auth TestServer. A B2 synchronization proof can claim repository→SQL only; it does not establish a public B2 write workflow. E01 does not prove real-patient consent, external source-of-truth state, production owner/provider provisioning, live Kestrel `Expect`, A3 overall PASS or production readiness.

**STOP:** no E01 pilot is authorized by this correction. Await independent review and Homeowner approval of the corrected preflight and four-part declaration.
