# TIP-88C1-C6B-A3 — Intermediate implementation review packet v0.1

**Date:** 2026-09-14  
**Status:** REVIEW CHECKPOINT — A3 IMPLEMENTATION INCOMPLETE; NOT A LANDING CANDIDATE  
**Purpose:** Give CC and GPT the same exact-byte code/evidence checkpoint, not another planning RRI.  
**Scope:** E01, R20/R21 lineage, B2 withdrawal synchronization, CP02/CP03 snapshots, and partial CP05 stage/intent schema work.  
**Restrictions:** Synthetic/non-patient only. No A4/production, stage, commit or push.

## 1. Read this first — what verdict is being requested

Review correctness and evidence of the implementation that exists, plus adjacent callers/callees. Do not infer full A3 completion from green tests or from a bounded internal source review.

A3 as a whole is NOT COMPLETE. The following are explicit remaining gates, not silently deferred acceptance requirements:

- Actual retained R3/R4/R5 CrossBorderAssessment lock-wait/expiry proofs, with positive controls and independent persisted observations.
- TI01/TI02/NPS01 terminalization, write-guard adaptations, continuation/readback/scan and their executable proofs.
- Remaining retained admission/broker/provider composition and full R2–R6 continuation.
- Agent retained-path integration and receipt completion.
- Final generated migration Designer/model snapshot, complete function/ACL catalogue, independently compiled CRLF/LF migration evidence and normative successor rebind.
- Full A3 implementation/architecture/integration acceptance and free adversarial closeout review.

The current migration is an intermediate candidate, not an EF-discovered deployable release. The current tests execute its actual Up/Down operations directly. Do not describe these as final EF migration-chain acceptance.

## 2. Authority, corrections and repository state

Server: `D:/Task/Remote Signing/TagEkyc`  
Server HEAD: `5df5f60a6dc4d992c71fc2b160673e4abca6488d`  
Agent: `D:/Task/Remote Signing/TagEkyc.CaptureAgent`  
Agent HEAD: `e3bd625bbe357b1f3c9620d20bcba121cfb61974`

Both repositories: staged=0, conflicted=0 when assembled. Agent has no tracked product diff at this checkpoint. Existing unrelated dirty/untracked files remain; this packet is not a staging allowlist. Server historical C3/C5 migration representation dirt and unrelated governance/docs are not claimed as this implementation's edits.

The Homeowner ratified the following parent plus its five bound companions. All six raw hashes were recomputed during packet assembly; no predecessor was changed.

| Authority file, relative to Server | Raw SHA-256 | Bytes | Lines |
| --- | --- | ---: | ---: |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_broker_composition_dispatch_v0_6.md` | `3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813` | 39495 | 320 |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_e01_contract_v0_5.md` | `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA` | 42623 | 217 |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_retention_checkpoint_contract_v0_6.md` | `EC42987AA5F7A8FEA3642C60C23A80D57033645870E4D98DE7B62728E41CB21D` | 63217 | 323 |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_broker_pipeline_contract_v0_5.md` | `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48` | 84714 | 414 |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_agent_result_contract_v0_6.md` | `D97F33F688F88CAF21F0B8C7EE9384A31D1F11C5C6142804A34530D9056C9B74` | 37841 | 261 |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_mutation_inventory_v0_6.md` | `E2B4EB58C1D0CE0C71BDE1033B4E103C458E45F255BA60C967144D5010FF325D` | 28474 | 186 |

Two explicit bounded Homeowner corrections apply in addition to these immutable predecessor bytes:

1. Remove only the nonexistent `FK_a3_consent_reference_client` / reference ClientApplicationId -> client_applications.Id. No client registry, no api_keys FK/uniqueness workaround. Preserve authenticated Client/P/session lineage, nonzero identity, composite FK, unique keys, recorder/withdrawer authority and checkpoint locking.
2. Add only `tests/TagEkyc.UnitTests/Tip88C1C6BA1ExecutionTests.cs` for deterministic nonempty PrincipalId success fixture, Issue/Replace empty-principal rejection before secret/persistence, and exact PrincipalId forwarding. Preserve old Client/category/replay semantics.

The corrections are recorded in the implementation ledger, not retroactively represented as bytes of the ratified predecessor. Normative successor update is required before final review. No new authority is created by this packet.

| Implementation history | Raw SHA-256 | Bytes | Lines |
| --- | --- | ---: | ---: |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_implementation_ledger.md` | `DDBC85E2C1F5438AFB6868D09F43A90F4D11487F10570C1A06B5E6172837CA21` | 17662 | 274 |

The ledger is chronological: earlier "current SHA", counts and pending statements describe their checkpoints. Use this packet's source table for current bytes, and section 1 for current completion boundaries.

## 3. Affected-surface / semantic trace

| Surface | Required behavior and current implementation | Evidence / remaining obligation |
| --- | --- | --- |
| R20 fixture and early authority | Fixed principal `a3000000-0000-4000-8000-000000000020`; empty P denied before pepper/gateway; success forwards exact P/Client | Focused 11 tests. No-gateway assertions are unit evidence, not direct database row counts |
| E01 API/Application/SQL | Existing consent reference, authenticated Client/P and owned session; no Client ID from JSON, no fake registry; closed request/result shapes | HTTP + actual PostgreSQL tests, cross-client rejection, credential independence, SubjectRef compatibility |
| CP11 R20/R21 | Frozen Principal/Client/session/retention lineage; one persistence transaction; replay before new issuance; post-lock expiry | Current capability/lineage tests and earlier lock/clock mutations |
| B2 withdrawal | Actual B2 target Granted event -> existing reference; session -> reference -> B2 scope; preserve nullable/Unicode DecisionRef | Real two-connection orderings and hook-removal RED controls |
| CP02/CP03 | Tagged snapshot graph and current reference, broker-only append, current retained resolver, immutable terminal kind | Role invocation, lineage mutation, actual PG visibility/time tests |
| CP05 R3/R4/R5 | Actual owned-session prefix, shared reference, full policy requirements, existing stage locks, then clock; preserve historical replay and fingerprints | Source corrections and actual legacy R2/Minio -> R3/R4/R5/replay positive; retained race/expiry proof OPEN |
| BR4.4 schema/model | Three nullable intent fields, named CHECK; existing outcome remains nullable text; protected Down census | Actual catalogue/roundtrip assertions. Terminal writer/guard completeness OPEN |
| Prepared composition | No fixture defaults posing as activated dependencies, no early export consent or retained-only export permission | Adjacent DI/API review required; no activated/full A3 readiness claim |

Retained authorization remains ONE existing personal-data consent with separate SourceRetention and later export permits. It must not create a second user-consent step. SourceRetention is not export authority. Post-Completed export checks remain independently mandatory.

## 4. Current implementation bytes — read full files, then adjacent surfaces

All paths below are relative to Server. Hashes are raw working-file SHA-256, not Git blob identifiers or normalized hashes. This is the implemented checkpoint inventory (27 files), NOT the full prospective A3 M/N allowlist and NOT permission to stage these files.

| File | Raw SHA-256 | Bytes | Lines |
| --- | --- | ---: | ---: |
| `src/TagEkyc.Api/RawSourceConsentEndpoints.cs` | `F47FBFC4F1DC02B375944C34AD49BCE2A718A7157FEAD87E168D8166B6D422B3` | 5167 | 79 |
| `src/TagEkyc.Api/Program.cs` | `84A4CD9BD5F4336AA3D00E449BB889CF4849CDD2B2BB2F2F1B62652A6B29D2FD` | 30014 | 624 |
| `src/TagEkyc.Api/RawExportSourceIngressServiceRegistration.cs` | `E109B65A8A61301DFE5C573D1193DA08DD2EC159377F03D9476F5F2FA8893EED` | 1653 | 31 |
| `src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs` | `101874EDB76EE3C62F6F794866486A896F23DE03BBA43A9FA741D7938C5EE3D4` | 13101 | 185 |
| `src/TagEkyc.Application/CaptureRuntime/RawSourceConsentApplicationService.cs` | `696C3082749B5035991A4C81977496AF2F4F4E872C1347E3A04B514B1F79799A` | 5466 | 77 |
| `src/TagEkyc.Application/CaptureRuntime/RawSourceRetentionProfile.cs` | `0A45F7CB76E13001FF415ABF98B2A371E17A986FEAAC89324BF6A5603F4B0D4F` | 1472 | 31 |
| `src/TagEkyc.Application/Ports/RawSourceRetentionPorts.cs` | `FBB42F099B29C915C9EF69FB885A7B66243ADAB40DEC659C41371392E5CA45BE` | 1758 | 32 |
| `src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` | `66CA48B1EA2B3E25541B911C4C48D466D059D6E64AFDD902CCF4F25BF9B2A32E` | 19542 | 351 |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs` | `4865BE9F58C9DA999CA633B3243B0FDC4B8EF69FF9F63228E6D421324C83FC62` | 13876 | 201 |
| `src/TagEkyc.Contracts/CaptureRuntime/RawSourceConsentContracts.cs` | `287A8BDF29951ADBB9FF93FB7D69616B2A71DE9F60C394EE52DF7E7629935E67` | 1557 | 27 |
| `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs` | `17492C715D73D9944A6C216D503ED85393D71B3168BCF3F7D5A9EDA2702AE0C6` | 4709 | 92 |
| `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionGateway.cs` | `BA6B660EBD4F0227D7560E890199839254F5967734EE2C07145D40706A17B3FD` | 6894 | 104 |
| `src/TagEkyc.Infrastructure/Persistence/Entities/RawSourceRetentionEntities.cs` | `95EA613B8063A55B8D065AB11777815EE629AC2588BC6FE8B7B1C6A5BE9B6F6A` | 3178 | 76 |
| `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionEntityConfigurations.cs` | `CAE7C7ABD69AE7A5D3F7D67C57130026F11473CB7FA3462C73E6709C77C20FC9` | 7536 | 106 |
| `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionProfileValidator.cs` | `65DF53861CB4DFBB0DA90D51BA4DBAD9465821BE9113840EA7B690955D4D3AA3` | 3370 | 61 |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeExecutionPersistenceBoundary.cs` | `279CA55E5BD8C1CA2936D1780F9279556E508885514C7BE3CA32FB5BD7AADB4F` | 13597 | 205 |
| `src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntimeEntities.cs` | `3E7666C34304014A48B8A663660553798A5385566996EECE0DA0BD4AA28EA1A9` | 19281 | 443 |
| `src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntimeEntityConfigurations.cs` | `8FFF412BC2E1E377CB72A02B655BA3204A9C66F0D51274A5C3D2239CCFF51EAF` | 55082 | 582 |
| `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAuthoritySnapshotRow.cs` | `7459EBC267E22408ACF4159D4C034545E90C98554B1995E7B5ED4A3CA765C90F` | 2210 | 45 |
| `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceEncryptionAttemptRow.cs` | `AD0C0A0F0DD7967CD39EC774BCC74048440E4C65F42E64B31A57058825638B19` | 2295 | 43 |
| `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` | `EAC6FDF8513F7EA850C7B9122BDCB89993E6E02AE199E829C1EA3F236CF32127` | 109572 | 1564 |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `C3BFDC5FB5A3A837E93746236164EFCEF98CF980177043CFCB849C758C411A4B` | 298202 | 4107 |
| `tests/TagEkyc.UnitTests/Tip88C1C6BA1ExecutionTests.cs` | `AA715EC51D82549B4F7E270D36237A366F840D7FD0E682CC2D46D5922168C918` | 10598 | 178 |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs` | `F28578587066B612BA6F2908679F2C8D0D886C93236FDF81310332B7A984F8A8` | 49714 | 765 |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3CapabilityLineageTests.cs` | `B4F9B046C396997397C81DBF6AA4B0E38AB9E1D4C2F1C4583FBCEB701E8C467D` | 12332 | 184 |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs` | `E44527EF2CC73D020DACEE106C7C0781722775A19F9B0753FB6B37D48F4543C6` | 11515 | 189 |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs` | `242C868C48DE5B9D4713DE6183322987BB308734F3BE885368F7D5F7A49A40CE` | 26154 | 409 |

For modified tracked files compare against the exact HEAD above. Untracked candidate files must be read directly; `git diff` alone will omit them. Expand review to actual neighboring auth, profile, policy, provider, stage, migration and fixture code as required; cite what was inspected.

Particularly inspect the historical stage functions in:
- `src/TagEkyc.Infrastructure/Persistence/Migrations/20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/20260812120000_Tip88C1B2R4R6SourceFinalization.cs`
- Existing B2 consent and C6B terminal behavior referenced by the candidate.
- `tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs`, `PostgresPersistenceFixture.cs` and `DurableObjectMinioFixture.cs` for the reused real legacy proof.

These are adjacent source references, not new mutation authority. If inspected live bytes have changed, identify the difference rather than assuming the old review covers it.

## 5. Evidence — independently rehashed and TRX counters read

Packet assembly did NOT rerun suites. The following are existing Builder-run TRX artifacts, rehashed and parsed directly at assembly. No external reviewer execution is being claimed.

The 11 focused tests are included in the 324 full UnitTests. The one legacy stage test is included in the 58 current A3 integration cases. Do not add overlapping totals. The 58 are NOT full repository IntegrationTests. No fresh ArchTests result is claimed.

| Evidence class | TRX path, relative to Server | Actual counters | Raw SHA-256 |
| --- | --- | --- | --- |
| Current/positive | `tests/TagEkyc.UnitTests/TestResults/a3-r20-principal-current.trx` | 11/11 passed; 0 failed; 0 skipped | `EAEFDC52AD8BEDA3428C592A78AFD771CBE3024C416722D68032B95314BE0F7E` |
| Current/positive | `tests/TagEkyc.UnitTests/TestResults/a3-unit-stage-current.trx` | 324/324 passed; 0 failed; 0 skipped | `89E7BE7926809168DB21B029B244460BC249957088DF76065C66828AFADC3802` |
| Current/positive | `tests/TagEkyc.IntegrationTests/TestResults/a3-stage-v2-current.trx` | 58/58 passed; 0 failed; 0 skipped | `0B69E3CADF90C3D6AF9A3C42DE5DFBFA68DC46F604BAED35FFF0D22481BFFCFE` |
| Current/positive | `tests/TagEkyc.IntegrationTests/TestResults/a3-stage-legacy.trx` | 1/1 passed; 0 failed; 0 skipped | `CABA71C76D6AA2A72E6E22302F8D152F40277F53F8EC359BDBC2DA220DA275B3` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-cp11-lock-clock-mutation.trx` | 1/4 passed; 3 failed; 0 skipped | `1D86B2714CB840A9365E30962AAAE05110953A2C89A6B820E9196B72DEB0AB7D` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-b2-hook-removed-mutation.trx` | 0/3 passed; 3 failed; 0 skipped | `BA711FD733FD687BE204EC720ECCA9C2984E54019728064D42BFF9C91114104B` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-snapshot-acl-mutation.trx` | 0/1 passed; 1 failed; 0 skipped | `B59E3D2605FBE69D920A6F8CCDACFD24B852E59F5144378ABA446811E3025488` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-snapshot-clock-mutation.trx` | 2/8 passed; 6 failed; 0 skipped | `4903D9D796E6A22751BD1366BA642247F50E9D8C4BEF8F9801811A74C0D80839` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-snapshot-prefix-mutation.trx` | 2/4 passed; 2 failed; 0 skipped | `E3E612248CF4CC6AD0A1356C07045706C72C70DF7D6B367C352DA8FF54C957DE` |
| Historical intentional mutation | `tests/TagEkyc.IntegrationTests/TestResults/a3-snapshot-head-mutation.trx` | 5/6 passed; 1 failed; 0 skipped | `139339E5D3D8E7F300CB0876E6842631BE60E2594FB30980C4B9418CC1B8994C` |

Historical mutation TRX failures are intentional tests of broken mechanisms, not current candidate failures. Their controls and restoration lineage are described in the ledger. Recheck actual failing assertions before accepting a claimed discriminating RED; counters alone are not sufficient.

Current positive commands (run from Server):

```powershell
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore --filter FullyQualifiedName~Tip88C1C6BA1ExecutionTests
dotnet test tests/TagEkyc.UnitTests/TagEkyc.UnitTests.csproj --no-restore
dotnet test tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Tip88C1C6BA3
```

Inspect PostgreSQL fixture lifecycle before running the third command: it owns Docker Compose/reset lifecycle, with disposable child databases and real Minio in the legacy proof. Do not run it concurrently with another process owning that fixture or against any production database. Coordinate a dedicated synthetic run; otherwise inspect code/TRX and explicitly state PostgreSQL NOT_RUN_BY_REVIEWER. Do not overwrite the bound TRX paths; use a distinct review filename if logging a rerun.

## 6. Internal review history and open evidence

These are bounded source-review reports, not CC/GPT external acceptance and not newly executed reviews during packaging.

| Window | V1 finding(s) | Correction / V2 disposition |
| --- | --- | --- |
| E01 | Existing SubjectRef CR/LF values rejected too broadly | Removed unratified restriction; V2 source clean; real negative/positive controls recorded |
| CP11 | Unlocked migration population census; stale Replace expiry clock | Locks precede census; expiry rechecked after wait; V2 source clean; discriminating PG mutations recorded |
| B2 synchronization | Bounded preservation review | Source clean; actual hook-removal mutation and restored tests recorded |
| CP02/CP03 | Missing broker EXECUTE; transaction-start clock; initially invisible retained target; missing current reference head equality | Four corrections and intended RED controls; V2 source clean |
| CP05 stage | HIGH: hard-coded fulfillment list omitted CrossBorderAssessment, allowing a later wait after clock sampling | Dynamic exact policy requirements excluding ConsentArtifact, COLLATE C, now before clock; V2 source correction verified; actual retained-stage wait/expiry proofs still OPEN |

Do not reopen the corrected source issue merely by quoting an old body. Equally, do not promote the CP05 source verdict into behavioral closure. Global A3 V3/free review and full closeout have not occurred. These separate intermediate windows are not one fabricated sequence of ten clean rounds.

## 7. Copyable common review instruction for CC and GPT

Review this exact packet and bound implementation as an INTERMEDIATE A3 CHECKPOINT, not a full-A3 ratification request.

1. Verify the packet SHA supplied by the sender, all six authority hashes, the 27 source hashes, ledger hash and evidence hashes with full 64-character comparisons. Report mismatches precisely; do not rebind or edit.
2. Apply PI-TAG-001 and the repository review playbook. Read this packet and the required authority/code completely, using pagination to EOF. Read E01 and CP contracts fully; read broker terminal-intent/ordering contracts and parent/Agent scope fences. Do not stop at a finding quota.
3. Review auth -> Client/P/session -> reference -> permit -> capability/binding -> snapshot lineage, concurrency, latest-state-before-time evaluation, ordinary denial/replay/expiry residue, exact SQL owner/ACL, C# mapping and Down safety. Check actual caller/callee paths rather than relying on prose, grep counts or a generated hash alone.
4. Inspect critical test assertions and positive controls. Distinguish actual HTTP/PG/provider execution, source-only checks, unit spies and echoed fixture values. For mutation evidence identify the mechanism broken, intended assertion and restored evidence.
5. Inspect all three stage branches, the dynamic CrossBorderAssessment lock set and post-all-lock clock. Report new bugs even in partially implemented code, but list declared missing TI01/TI02/retained-stage proofs as OPEN COMPLETION GATES rather than pretending they were claimed complete.
6. Free-roam adjacent coupling and scope risks. Do not invent a second consent, Client registry, API-key fallback, expanded provider, new RRI or production requirement to fix an ordinary implementation defect.
7. Return exact packet SHA, files/adjacent surfaces read, tests actually run vs TRX-only, at least three plausible risks confirmed/dismissed, all findings with severity/classification/file/line/violated invariant, and a separate list of still-open completion gates.
8. Verdict must be bounded: PASS/HOLD on reviewed implemented source/evidence claims. A3_OVERALL remains NOT_COMPLETE while section 1 gates remain. Zero findings is not landing or production authority. No edits, stage, commit, push, secret access or deployment.

CC and GPT receive the same packet and rubric. Do not use one reviewer's opinion as evidence that the other has reviewed these bytes.

## Changelog

- v0.1: Materialize the existing implementation checkpoint, exact raw hashes, actual TRX counters, acknowledged corrections and open proof/implementation gates for independent review. Documentation only; no product/test/schema or authority change.

