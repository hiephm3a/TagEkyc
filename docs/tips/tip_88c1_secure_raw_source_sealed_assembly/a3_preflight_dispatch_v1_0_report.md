# A3 continuous proof closure — first-turn preflight

Status: **SUPERSEDED PREFLIGHT**. Its 1,301-file manifest was incomplete for ignored ledger-referenced TRX; see the successor preflight correction before using any count or SHA below as a current baseline. No E01 product/test edit, test run, mutation, full suite, stage, commit, push, A4, or production activation was performed.

## Authority and candidate

- Dispatch attachment SHA-256: `E174D6E1F3F5B899A7E12C9A748BD09BC112AEB0CBD01AEEF3C980D25118A977`.
- Parent A3 v0.6: `3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813`.
- Five bound companions: E01 `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA`; checkpoint `EC42987AA5F7A8FEA3642C60C23A80D57033645870E4D98DE7B62728E41CB21D`; broker `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48`; Agent/result `D97F33F688F88CAF21F0B8C7EE9384A31D1F11C5C6142804A34530D9056C9B74`; inventory `E2B4EB58C1D0CE0C71BDE1033B4E103C458E45F255BA60C967144D5010FF325D`.
- Previously approved Homeowner corrections remain in force, including removal of only the fictional `client_applications` FK. No replacement FK to `api_keys` or client registry is inferred.
- Server HEAD `5df5f60a6dc4d992c71fc2b160673e4abca6488d`; Agent HEAD `e3bd625bbe357b1f3c9620d20bcba121cfb61974`. Both had staged=0 and conflicted=0 at intake. Their dirty/untracked work is preserved.

The reconciliation ledger was `258804B1F3ED6A596B741EFAA9B0929F382E2D5E9AE411034ABFFA290C9A6519` before the two provenance-label corrections and is `E56EA6BA0BF59C828A9C2036E4D35027A154BA46D7D7B1F798F382D96623D484` afterward. No normative row changed.

Direct census from the 134 table rows: normative 75 `NOT IMPLEMENTED` + 24 `IMPLEMENTED` + 27 `RENAMED`; cross-repo seams 1 `NOT IMPLEMENTED` + 7 `IMPLEMENTED`. Thus **76 open = 75 normative + 1 seam**, all mandatory. Total 31 implemented + 76 open + 27 renamed = 134. The remaining seam is real Agent `Expect` → Kestrel/PostgreSQL B/R1, not the already-closed TestServer result seam.

## Eleven current/restored-label audit

Nine live bytes matched their current pins: parent v0.6, public ingress constants, outcome enum, Server mapper, Agent HTTP client, S02 result-acceptance test, Agent retained/malformed test, composition, and broker-pipeline test. Two historical pins were incorrectly worded as current; only those two phrases were changed to `SUPERSEDED` in the reconciliation ledger:

| File | Superseded SHA | Live successor SHA | Superseding evidence |
| --- | --- | --- | --- |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ClientServerAcceptanceTests.cs` | `106C4392C8E3C9089D580F5B529D99360D757073253425C10F4C32717EFE98A6` | `07259F625A052AD549B77CA0465E1EEEF612E86C645B5A149F5937F185E03077` | S02 All14, `a3-s02-all14-joined-final-v2.trx` SHA `D2C7A8618D4B998C7ABEBF7394000084DDB6CCD32F73C30F822BB8254ED6974B` |
| `src/TagEkyc.Api/Program.cs` | `6B73B55CD0FB7A62CCFD87F67E5726BC21B31C76C7D0CF63A450DEA3165A333D` | `6313ED4F65BF721C75414B5DB5D90190891DF5BE6A3BCADE8C2F6E7CFB959314` | P25 capacity correction; baseline RED `7310E63DEF03381F2C94A45C4980D836D1AA533E7AAB5AA21D91B560760DC1F2`, corrected GREEN `2DEC99281E14C8E6335820558D6CFA6C8993C2516378EA2308FF0D25667CF733` |

Audit result: `labels_checked=11`, `live_matches=9`, `superseded_with_successor=2`, `stale_current_labels=0`. A historical SHA is still retained as provenance, not silently deleted or presented as the current source.

## Candidate baseline and global matrix

The byte-level [baseline manifest](a3_preflight_baseline_manifest.tsv) is sorted ordinal by `repo<TAB>repo_relative_path`, UTF-8 without BOM, LF, with `repo`, relative path, role, size, and SHA-256 for each file. Its [sidecar](a3_preflight_baseline_manifest.tsv.sha256) pins manifest SHA `296A8A5D4903F258DB41E9FFB4F258E05F3A81E8C0F532CE8B25183968F50CF9`.

```text
server_manifest_files=1155
agent_manifest_files=146
manifest_files_total=1301
baseline_manifest_sha=296A8A5D4903F258DB41E9FFB4F258E05F3A81E8C0F532CE8B25183968F50CF9
```

Inventory rule for reproduction: run `git ls-files --cached --others --exclude-standard` in each repo; add existing ignored `tests/**/TestResults/*.trx` paths explicitly referenced by the reconciliation/implementation ledgers; exclude only `.git`, `bin`, `obj`, caches, reproducible build outputs, unreferenced transient test output, and the manifest itself. Normalize repo-relative separators to `/`; classify role in precedence order as evidence TRX, reconciliation ledger, SQL/migration, test source, product source, project file, configuration, script, docs/authority, other controlled. Sort using ordinal string comparison, compute raw byte size and SHA-256, and write tab-separated UTF-8 without BOM with final LF. Rehash the manifest itself into the sidecar. This is a baseline of the two scoped repos, not a claim of whole-disk backup; the matrix, mutant registry and this report are subsequent preflight artifacts, not part of the captured baseline.

Run [`a3_preflight_manifest_verify.ps1`](a3_preflight_manifest_verify.ps1) to rehash every recorded file, check sizes/ordinal order/sidecar and detect any newly visible Git path. It completed on this candidate with exactly the four count/hash lines above. It uses the captured baseline path inventory for ignored historical TRX, whose discovery from free-form ledger prose is not a substitute for this explicit frozen list.

The [global open-row matrix](a3_preflight_open_matrix.tsv) has all 76 distinct open row IDs and the dispatch's claim, caller, producer, phase, transport, result, body, residue, Agent custody, existing proof, missing dimension, shared mechanism, test-double and classification fields; none of those required fields is blank. SHA-256 `C7C84BE7FBAD3F319702D397D216F26C70E6E638182B4DADF8B2A27CB62B121C`. Preliminary classifications: 73 `TEST_EXTENSION`, 3 `SEMANTIC_BLOCKER` (P03–P05, whose production emission is not established). These are triage assignments, not row closures or approval of their production reachability.

Mechanism clusters (row counts): E01 Record 5, Issue 3, graph 1, checkpoint 1; R2–R6 19; broker claim 10, admission 6; assembly 8; readiness 5; Expect 4; continuation 3; retention core 3; Agent result 2; capacity 2; export C3 2; A1 auth 1; migration 1. Sum = 76. The cluster order is a work plan, not permission to bypass each row's missing dimensions.

The [recorded mutant registry](a3_preflight_recorded_mutants.tsv) contains 39 distinct explicitly recorded source-mutant hashes from the ledgers (SHA `3F36D4D16F6715D308B9798F52609D6893CBB988D6B5AD920F82833D43317EE7`). None matches any live byte SHA in the baseline manifest: `recorded_mutants=39`, `live_mutant_matches=0`. Older mutation TRX lacking a source-mutant SHA cannot be silently represented as byte-level source entries.

## E01 pilot declaration for Homeowner decision

1. **Production caller/path, corrected distinction.** E01-R and E01-W both have public HTTP routes mapped by ordinary `TagEkyc.Api/Program.cs`. E01-W uses `POST /api/ekyc/source-consent-references/{referenceId}/withdraw` → authenticator → `RawSourceConsentApplicationService.WithdrawAsync` → `RawSourceRetentionGateway.WithdrawAsync` → `tagekyc.raw_source_withdraw_consent_reference`, with operation domain E01-W. B2 subject-consent withdrawal is a different write: `IRawExportSubjectConsentRepository.RecordSubjectConsentWithdrawnAsync` → `EfRawExportSubjectConsentRepository` → `tagekyc.raw_export_append_subject_consent_withdrawn` → synchronization hook, operation domain B2-Withdrawal. No `src` caller above that repository B2 write has been found. Thus pilot E01-R/E01-W should exercise their real HTTP paths; any B2 hook proof is repository→SQL only and must never be relabelled as public B2 workflow. Activated-only R20 and later checkpoints remain separately scoped.
2. **Production-real components and test doubles.** SUT: actual endpoints, authentication where claimed, application, gateway, PostgreSQL transactions/locks/functions/constraints/ACL, R20 issuance, existing checkpoint repositories and broker/worker paths. Synthetic non-patient session/profile/policy/recorder grants establish fixtures only. A deterministic competing connection/lock holder may schedule races but is not the SUT. Current `Tip88C1C6BA3ConsentRetentionTests.Start()` uses a custom TestServer and, by default, a fixed-identity authenticator; its `RunR20` helper directly calls production SQL and `PrelockRetentionRead` manually takes locks. Those helpers prove only their stated SQL/precondition dimensions. A real-key test variant exists and must be used for any credential-auth claim. Test-only owner/object/key provider configuration, when needed for Activated, is explicitly supplied and is not a production default.
3. **Why each double cannot/should not be replaced.** Synthetic person/session/profile values are required by the synthetic/non-patient boundary. The competing lock holder is necessary to make the interleaving deterministic, but the actual Record/Withdraw/Issue/checkpoint operation must run in production code on separate real PostgreSQL connections and be observed via `pg_locks`/durable rows. The fake authenticator is replaceable and cannot carry an authentication claim; use the real-key variant or an actual Program-host proof for that dimension. Fixture grants and `RunR20` direct SQL are acceptable only as setup/SQL probes, never substitutes for provisioning or the public R20 caller. No in-memory SQL substitute, canned Final, injected provider result or test-only lifecycle path may stand in for the behavior being claimed.
4. **This cluster proves / does not prove.** After approval and execution, E01 may close only rows supported by exact test identity plus targeted RED/restored GREEN: Record CAS/replay/update, Recorder authority, five-table graph/lineage/ACL, real PG withdrawal-vs-Issue/checkpoint serialization, post-lock database clock and residue. It must preserve the approved no-client-registry-FK correction. It does **not** prove an actual patient's consent, external source-of-truth state, production owner/provider provisioning, public B2 notification reachability, Kestrel `Expect` seam, C1/C3 independent export authority beyond their specifically executed checkpoints, or A3 overall readiness. Existing custom TestServer/fake-auth/manual-lock tests may be joined only under their narrower labels.

One read-only RRI independently traced the production graph and verified these limits; it made no edits and ran no tests. In particular, it confirmed the B2 missing caller, E01's ordinary Program registration, Activated's explicit-owner requirement, and the current test-host/auth/direct-SQL doubles.

**Gate:** Homeowner must approve or amend this four-part E01 declaration before any E01 product/test work. The global matrix should be treated as preliminary until that decision.
