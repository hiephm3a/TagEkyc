# TIP-88C1-C1 Resolver + Authenticated Assembly — As Built

Status: `IMPLEMENTATION_CLOSED / INDEPENDENT_CLOSEOUT_REVIEW_PENDING`

## Authority

- baseline: `c2956b168f8475e6285dc51d4037bab55234e32c`
- ratified dispatch v0.2 SHA-256:
  `A05A597583FC706A61050EBDCC7B0CC5A406C2D2C3D49645F60FDC8B22B7557D`
- reviewed dispatch bundle SHA-256:
  `302A364397E9C86D149826BA5E6077914057177EAFA36CB94C56C1F137867534`
- original ratified permanent path ceiling: 39
- Homeowner-amended permanent path ceiling: 44
- stage/commit/push/deployment authority: none

The ratified scope brief and dispatch remain byte-authoritative. Executable
findings discovered during the build are recorded append-only in the review
ledger and summarized here; they were not folded back into the ratified file.

## Bounded closeout correction cycle

The first closeout bundle, SHA-256
`4E19C6240DE7CBF8AA8A32EBE94EB673DC0EA8F7B7A5179C43CADF3D78232F94`,
is `REJECTED_FOR_COMMIT` and retained only as historical evidence. The
Homeowner-authorized bounded correction cycle closed four C1 defects without
using the then-unopened path 43, adding a role, public outcome, crypto format
or C2 protocol change:

1. public `ExecuteAsync` now reads exact committed recovery context before
   Freeze; a fresh retry finalizes an already committed seal without source,
   object, key or admission work;
2. all C101-C126 proof methods were audited and the weak literal/reflection/
   source-only cells were replaced or paired with executable discriminators;
3. readiness now owns an exact ten-signature function surface and rejects
   grant option, alternate grantor, unexpected grantee and overload drift;
4. commitment-provider failure has a typed internal boundary and remains
   distinct from object-read failure without adding a public disposition.

Current corrected implementation anchors are:

| Artifact | SHA-256 |
|---|---|
| `RawExportAssemblyOrchestrator.cs` | `C7843701C5F4043E1077FDB25877FFFC7EA833D533AB4ABACCE5DFA34D03B65D` |
| `RawExportAssemblyRepository.cs` | `6293A0DC480C3825E081D15DA6EF1FB253A20C007919D4A4DE85673FAEAEC687` |
| `RawExportAssemblyServiceCollectionExtensions.cs` | `C312103918ACFFC64A81720CA1919C88CB6F317116A004F6B5A9A9E4F39DB774` |
| `RawExportAssemblySourceResolver.cs` | `6E03A2487FD1D9B0BDA27DC74649379E8D8CFD06743FD10548E69D019F707FCA` |
| `RawExportFramedSourceVerificationService.cs` | `48F712D677E230C93B49C9EEB8FF80FE0CB841D501A4F88FCFDC0ECE90481FD1` |
| C1 migration | `125EC8B98ED3F95B2504DA59A548C32814A61C497391E29138D4288A8FC5FBEF` |
| C101-C126 integration proof file | `82F8E53F52D5A14B8EFCF056CFF703DCF1778E9E6923C30705C32D36A5CE0CA2` |

### Compiler-like proof audit

The current canonical C101-C126 run is `26 passed / 0 failed / 0 skipped`;
TRX SHA-256 is
`9D1BC79749748BC8C8CC9EDEA02C66D32C9A4D6FF4B24872F19AAEC39F0CE220`.
`N/A` below is used only when the named mutation cannot be produced through a
landed public/fixture surface without bypassing an append-only guard; the
infeasibility is explicit rather than silently treating source text as proof.

| Proof | Ratified claim | Canonical proof/result | Mutation or negative input | Observed RED discriminator / named N/A |
|---|---|---|---|---|
| C101 | exact selected source/order | real R2->R6->C1 synthetic path seals one selected class; PASS | historical selected-alias defect | SQL `42703`; corrected canonical seals |
| C102 | missing/duplicate/mismatch leaves zero bindings | missing source and wrong fence both leave zero rows; PASS | missing selection; fence +1 | `SourceUnavailable`; `LeaseLost`, row count 0 |
| C103 | closed source eligibility | eligible Available/Verified/Active/Staged source seals; authority/deadline negatives are exercised by C112/C117; PASS | N/A: remaining individual persisted-state mutations have no landed reverse transition and raw row fabrication would bypass append-only guards | `INFEASIBLE_WITHOUT_RAW_STATE_FABRICATION`; no false RED claimed |
| C104 | insert-once exact frozen replay | first `Frozen`, replay `ExistingMatch`, one immutable row; wrong fence rejected; PASS | update-to-latest N/A: no landed replacement/update surface exists | `INFEASIBLE_BY_APPEND_ONLY_BINDING`; wrong fence -> `LeaseLost` |
| C105 | resolver/sealer ACL split | exact resolver/sealer grants and cross-role denials; PASS | cross-role calls/grants | access denied / exact grant census mismatch |
| C106 | exact durable object reopened | two independent exact opens use persisted identity/key/binding; no public locator; PASS | N/A: a process-cache adapter is not exposed by the landed reconciler interface; complete-buffer bypass is separately executable in C119 | `NO_CACHE_INJECTION_SURFACE`; locator/open count remains behavioral |
| C107 | typed source failure partition | typed commitment-provider failure -> `KeyAccessIndeterminate`; generic I/O -> `ObjectReadIndeterminate`; PASS | collapse provider exception to generic object-read mapping | exact disposition assertion RED |
| C108 | shared R2/C1 framed verifier | both consumers bind the same concrete verifier and affected R2 integration remained PASS | N/A locally: moved failure discriminators are owned by the existing R2 named mutation cells, not duplicated in C1 | `MUTATION_OWNED_BY_R2`; C1 adds no second verifier |
| C109 | absolute AssemblyDigest | canonical bytes recompute `901a7044...5c6f`; PASS | mutate second plaintext byte | digest inequality assertion proves target changes |
| C110 | manifest/auth absolute vectors | manifest `eb3ad4b3...c41b`, auth `d73771a5...be2`; PASS | digest/key-id/key-version changed independently | each derived manifest differs |
| C111 | deterministic IDs/fingerprints | absolute assembly/preparation IDs and fingerprints; PASS | flip assembly-fingerprint byte | preparation ID changes |
| C112 | fresh barrier before read | withdraw authority before Execute; PASS | authority withdrawal | `AuthorityInvalid`, exact reads 0, Prepare 0 |
| C113 | second barrier before provider | withdraw after pass 1 in authenticator hook; PASS | post-pass-1 withdrawal | `AuthorityInvalid`, reads 1, Prepare 0 |
| C114 | durable Preparing before provider/recovery | lost Prepare response inspects and returns exact existing preparation; PASS | response loss | one Prepare, inspect recovery, no blind second Prepare |
| C115 | Pending exact receipt/revision | wrong revision/digest reject; exact write/replay persists one digest; PASS | revision +1; changed receipt | `PreparationConflict`; row unchanged |
| C116 | global lock subsequence | two independent orchestrators serialize safely on one job; exact lock order asserted; PASS | publication/attempt inversion installed and executed | C116 RED; TRX `1A02649B...35CCC` |
| C117 | one fresh post-lock clock | authority and consent exclusive locks held past 10-second lease; both reject after release; PASS | seal clock changed to transaction/pre-wait time | mutation sealed after expiry; C117 RED; TRX `E44AD612...2E509` |
| C118 | public committed recovery precedes Freeze | first call leaves `SealCommitted`; fresh public retry finalizes as `ExistingMatch` with zero reread/prepare; PASS | remove pre-Freeze committed recovery | `Expected ExistingMatch / Actual LeaseLost`; TRX `815619FD...57D38` |
| C119 | bounded two-pass streaming/zeroization | 3 MiB source, two exact reads, max destination write below plaintext length, retained then erased; PASS | buffer complete assembly and emit one write | max-write assertion RED; TRX `B12228BD...96937` |
| C120 | atomic seal rows/head/event | SealCommitted preparation, identity, item, head and transition bind exact IDs/revision/fence; PASS | omit AssemblySealed transition | missing transition makes C120 RED; TRX `21D2982A...08841` |
| C121 | AssemblySealed closed/lease-ineligible | reacquire rejected; head remains sealed with no lease; PASS | inherited B4 state/transition branch mutations | B4 M1/M12 predecessor cells RED; no duplicate C1 mutation |
| C122 | finalize only exact committed preparation | wrong fingerprint conflicts, lost finalize response recovers exact Finalized; PASS | changed fingerprint; response loss | Conflict before finalize; exact inspect returns ExistingMatch |
| C123 | abort only after durable authorization | pre-authorization call count 0; durable digest permits one abort; PASS | caller digest without durable row | Conflict, provider abort count 0 |
| C124 | exact schema/signature/owner/ACL | exact 4 tables and 10 function signatures; internal mutation/restore matrix PASS | drop recovery function; grant option; runtime grantee; alternate grantor; extra overload | each readiness call throws `PROD_RAW_EXPORT_ASSEMBLY_SCHEMA_INVALID`, independently restored |
| C125 | apply/Down/reapply and B4 restoration | canonical migration cycle and pending-model gate PASS | N/A locally for every inherited B4 branch: owned, named B4 mutation cells were rerun after C1 migration | `MUTATION_OWNED_BY_B4`; C1 does not duplicate predecessor tests |
| C126 | production topology/readiness codes | Disabled/FixtureProof/Durable matrices and exact codes PASS | DK/B4 table/function ownership wildcard restorations | named readiness controls RED, then byte-exact restore |

The bounded cycle also ran Release build `0 warnings / 0 errors`, C1 unit
`1/1`, C1 architecture `4/4`, and the restored C101-C126 matrix `26/26`.
No complete unfiltered Release suite was run under this correction authority.

### Ten-function count sweep

The active C1 count/signature sites are now consistent:

| Site | Result |
|---|---|
| migration create/owner/revoke/grant/Down lists | 10 exact functions |
| readiness `expected_functions` plus exact surface census | 10; no overload |
| C105 grant census | 10 |
| C124 signature/ACL and surface census | 10 |
| dispatch section 7 signature manifest | 10 |
| this as-built readiness statement | ten |
| ArchTests | no numeric C1 function-count pin exists |

Repository search found no remaining active C1 function-count pin of nine.
Dropping only the committed-recovery function makes C124 readiness RED,
independently of each four ACL mutation.

## Implemented product boundary

The fixture/synthetic C1 path now executes:

```text
B4 authorized job + exact selected Available source
-> resolver capability freezes exact source bindings
-> exact MinIO object is opened twice
-> shared R2 framed verifier authenticates/decrypts bounded chunks
-> pass 1 computes AssemblyDigest
-> manifest is authenticated
-> durable Preparing row precedes C2 I/O
-> pass 2 writes one bounded assembly to the fixture C2 provider
-> Pending CAS
-> one SQL transaction writes SealCommitted + assembly identity/items
   + B4 AssemblySealed head/event
-> provider Finalize
-> durable Finalized CAS
```

No production raw-source adapter, production C2 provider, real Raw BIO,
package construction or delivery was added.

## Security and recovery properties implemented

- Resolver and sealer database capabilities use separate connections and exact
  SECURITY DEFINER surfaces. Resolver actor identity is transaction-local.
- The application receives neither object locators nor wrapped/unwrapped key
  material. Source plaintext is exposed only through a bounded callback.
- R2 and C1 use one framed-ciphertext verification implementation. Every
  custody-owned plaintext chunk and cryptographic scratch buffer is zeroized.
- The fixture C2 provider retains at most one assembly of at most 32 MiB in
  memory, then actively zeroizes it on Finalized or Aborted. It is explicitly
  not durable or production evidence.
- Prepare and Finalize response loss are recovered by exact provider inspect.
  Fingerprint/receipt mismatch fails closed and no blind second Prepare occurs.
- Provider Abort is not callable through the recovery helper until an exact
  durable `AbortAuthorized` row and digest have been read back. Unavailable or
  unknown provider outcomes do not create false terminal state.
- Seal replay binds the committed preparation, identity, all persisted item
  descriptors, the original AssemblySealed transition revision/attempt/fence,
  every digest and authentication selector. A changed item returns
  `AssemblyConflict` before generic stale-state handling and mutates nothing.
- Readiness validates the complete four-role attribute graph, both exact
  memberships, all four table owner/ACL manifests and all ten exact function
  signature/owner/SECURITY DEFINER/search-path/grant manifests.

## Executable defects closed during implementation

| ID | Symptom | Narrow correction | Proof |
|---|---|---|---|
| EX-C1-02 | `42703` during source freeze | use capture-acceptance alias from the selected source | C101 |
| EX-C1-03 | `42702 RowRevision ambiguous` | qualify every preparation UPDATE target alias | C101/C115 |
| EX-C1-04 | `42P01 raw_export_source_heads` | use landed singular `raw_export_source_head` | C101/C116 |
| EX-C1-05 | replay returned `Created` while revision remained 1 | distinguish insert with `GET DIAGNOSTICS ROW_COUNT` | C114 |
| EX-C1-06 | response-loss was returned without inspection | exact Prepared/Finalized inspection recovery | C114/C122 |
| EX-C1-07 | replay did not compare item JSON and mismatch fell to LeaseLost | exact persisted-item and predecessor-transition binding | C118 |
| EX-C1-08 | fixture C2 was hash-and-discard | bounded retained buffer plus zeroization | C101/C119 |
| EX-C1-09 | abort proof was vacuous | durable authorization readback before one provider abort | C123 |
| EX-C1-10 | readiness checked roles only | exact catalog/ACL manifest | C126 |
| EX-C1-11 | assembly login roles matched DK-PROD's open-ended login wildcard | replace ownership inference with the exact three DK login roles | C126; DK wildcard mutation RED |
| EX-C1-12 | valid C1 job source-binding surfaces polluted B4 readiness ownership | replace all seven runtime ownership wildcards with exact five-table/fourteen-function B4 sets; retain exact current-epoch digests | B4 M6/M12; table/function wildcard mutations RED |
| EX-C1-13 | current C1 snapshot invalidated predecessor absolute tripwires | synchronize the two authorized snapshot constants without weakening their assertions | E3 F6; DK-FIXTURE E3; R316; F416 |
| EX-C1-14 | B4 M13 scanned the cumulative C1-aware table manifest and treated source-binding metadata as a B4 raw-byte/package surface | use the exact five-table B4-owned set for M6 ACL and M13 boundary assertions; keep the cumulative manifest for M1 only | M6 table/column ACL + M13 PASS; cumulative-scope mutation RED |

## Allowlist amendments and predecessor accounting

The ratified 39-path allowlist was amended, without reopening the dispatch, by
three exact Homeowner-authorized paths:

| Path | Amendment reason | Pre-change SHA-256 | Final SHA-256 |
|---|---|---|---|
| `src/TagEkyc.Infrastructure/RawExport/CustodyRoleReadinessValidator.cs` | DK readiness must own exactly its three login roles and ignore the two valid C1 assembly logins | `3732E801FEA9C289282BD3E06066A8C47CCE40B89434C3BB3CCD46573F74C3B3` | `AB273C0C1AE032119861EA119C439A5DD83902E408BBEF7BBBC1803BB64DF407` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs` | synchronize the existing absolute model-snapshot tripwire with the additive C1 model | `96BDEEB08D15D0EDDE0AFBD324ED23B79CFC6C051A28F27BFCF28AA9F062B285` | `76C42EDB51BD999D8FFB323124CB6EB4B3EB1EA0353537F751BE66159101216C` |
| `src/TagEkyc.Infrastructure/Persistence/RawExportJobReadinessValidator.cs` | B4 runtime readiness must use finite semantic ownership, not an open-ended catalog wildcard | `564D3DA1D66775B7CB35573C0B6A47993E8DB77AA25EACE397B74250E8BAF1A2` | `1A371D5AC023683FF1D430A9A20B61A7E5FB8B65B9D586D72C18CB4331871B36` |

The C1 correction ceiling at that milestone was `42/42`. TIP-68 subsequently
opened paths 43 and 44 under separate Homeowner environment/test-harness
authority; their provenance and bounded disposition are recorded below.
Semantic predecessor accounting is:

| Predecessor surface | Path/surface and SHA evidence | Why C1 affected it | Rerun proof and result |
|---|---|---|---|
| B4 transition/state | `RawExportJob.cs` `3CCD8872...AA4B09` -> `F4B7CC15...C9C2F`; C1 migration `F9F933E4...2DAB0` re-emits the B4 CHECK/guards while the landed B4 migration remains unedited | adds closed `AssemblySealed` state/event and exact guarded transition | C121/C125 and B4 M1/M12: PASS |
| R2 verifier | `RawExportR2CompletionVerifier.cs` `0A18785B...8558A` -> `58AF7E66...E62A6` | C1 and R2 now consume the same typed framed-ciphertext verification service | affected R2 integration + C108: PASS |
| DK-PROD readiness | `CustodyRoleReadinessValidator.cs` `3732E801...4C3B3` -> `AB273C0C...DF407` | two new C1 login roles exposed the DK wildcard ownership defect | DKPROD-37/C126: PASS; wildcard restoration: RED |
| B4 readiness | `RawExportJobReadinessValidator.cs` `564D3DA1...AF1A2` -> `1A371D5A...71B36` | C1 adds valid job-named surfaces and changes ratified B4 guards/CHECKs | M1/M6/M12: PASS; table and function wildcard restorations: RED |

`READINESS-OWNERSHIP-RULE-01`: open-ended catalog wildcards must not define a
slice's owned tables, functions, triggers, roles or equivalent objects. An
exact finite manifest, or another explicitly bounded ownership predicate, is
required. Pattern matching is allowed only when the pattern itself is the
invariant being validated.

`READINESS-OWNERSHIP-DEBT-01` records the observed
`proname LIKE '%subject_consent%'` in
`RawExportSubjectConsentReadinessValidator.cs`. It is deferred to the Subject
Consent owning slice; C1 did not modify it.

`READINESS-OWNERSHIP-OBS-01` records the provisional-object check resembling
`rolcanlogin = (rolname LIKE '%_login')`. It may be a naming/login convention
rather than ownership inference. C1 did not modify it; its owning review must
classify it before any remediation.

## B4 failure accounting and mutation evidence

All ten B4 failures in the first affected matrix were individually accounted
for and rerun on the corrected bytes:

| Test | Observed diagnostic | Authorized root/path | Final targeted result |
|---|---|---|---|
| `M12_readiness_returns_exact_code_for_each_manifest_drift` | initial healthy check returned `SCHEMA_INVALID` | exact current B4 schema digest in path 42 | PASS |
| `M6_internal_helper_public_execute_fails_readiness` | precondition returned `SCHEMA_INVALID` | exact B4 table/function ownership and current digest in path 42 | PASS |
| `M6_alternate_function_acl_grantor_aborts_apply` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| `M6_unrelated_table_grantee_fails_readiness` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| `M6_default_function_acl_grantee_aborts_apply` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| `M6_unrelated_column_grants_fail_readiness` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| `M6_column_alternate_grantor_or_grant_option_fails_readiness` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| superseded `M6_extra_runtime_granted_b4_entry_fails_readiness` | wildcard treated an unowned lookalike as B4 | count-neutral `M6_valid_c1_job_functions_do_not_pollute_b4_readiness` in the already-authorized B4 test path | PASS |
| `M6_deployer_owner_attributes_fail_readiness` | precondition returned `SCHEMA_INVALID` | path 42 | PASS |
| `M6_function_acl_manifest_is_exact_and_grantor_aware` | exact manifest assertion failed under wildcard scope | exact fourteen-function manifest in path 42 and B4 test | PASS |

The final focused rerun was `10 passed / 0 failed`. Independent scratch
mutations produced the required discriminators and were restored exactly:

- table ownership changed back to `raw_export_job_%` -> M12 RED with
  `PROD_RAW_EXPORT_JOB_SCHEMA_INVALID`;
- function ownership changed back to the job-name wildcards -> M6 RED with
  `PROD_RAW_EXPORT_JOB_FUNCTION_ACL_INVALID`;
- DK member ownership changed back to
  `tagekyc_raw_export_%_login` -> C126 RED with
  `PROD_RAW_EXPORT_CUSTODY_ROLE_GRANT_INVALID`;
- trigger wildcard mutation is `N/A`: the C1 source-binding relation has no
  non-internal trigger, so the current harness offers no independent
  behaviorally distinguishable trigger-ownership mutation. No trigger was
  fabricated.

After restoration, the two production validators returned to SHA-256
`1A371D5A...71B36` and `AB273C0C...DF407`; the three positive controls passed.

## Pre-correction validation evidence (historical)

The following affected validation passed on the current working bytes:

| Surface | Result |
|---|---|
| Release build | 0 warnings / 0 errors |
| C1 unit vector | 1 passed |
| C1 architecture | 4 passed |
| C101–C126 | 26 passed / 0 failed / 0 skipped |
| affected R2 integration | 18 passed / 0 failed / 0 skipped |
| C101 real synthetic path | Sealed; 2 exact object reads; one C2 Prepare; memory erased; Finalized |
| pending model + apply/Down/reapply | passed through C125 |
| Gate A affected matrix | 61 passed / 0 failed / 0 skipped |
| B4 original-failure rerun | 10 passed / 0 failed / 0 skipped |
| mutation restoration controls | 3 passed / 0 failed / 0 skipped |

## Pre-correction full-suite execution history (historical)

The single authorized complete unfiltered Release run was started on the final
working bytes on 2026-08-16. It produced:

| Project | Result |
|---|---|
| ContractTests | 13 passed / 0 failed / 0 skipped |
| ArchTests | 128 passed / 0 failed / 0 skipped |
| UnitTests | 189 passed / 0 failed / 0 skipped |
| IntegrationTests | environment-invalid: 44 passed / 1 skipped / 1385 failed |

The Integration result is not a C1 product verdict and is not accepted as a
canonical census. `Tip68SoftHsmFixture.InitializeAsync` failed because no
SoftHSM2 PKCS#11 module was provisioned. Because initialization failed before
the fixture captured `previousPath`, its cleanup then restored the process
`PATH` to null. The shared PostgreSQL/MinIO fixtures consequently could not
resolve `docker`, producing the downstream failures. A direct post-run Docker
probe succeeded against client/server 29.4.2, confirming that the engine itself
was available.

Evidence is retained outside the repository at:

`D:\Task\Remote Signing\TagEkyc_C1_closeout_20260816`

The Integration TRX SHA-256 is
`CA1193A8EC63C9F7DE70B346AFB7192F83D865D95CEBDBFD02FD873467BD2DEE`.
That first run is retained as historical, non-canonical diagnostic evidence.
The Homeowner subsequently authorized environment self-heal. The pinned
SoftHSM2 2.5.0 module/util and Docker client/server were verified outside the
repository. A replacement diagnostic run reached `730 passed / 21 failed / 1
skipped`; all 21 failures were traced to the C1 host/snapshot/role and B4/DK
readiness defects summarized above. Gate A and the required mutation proof are
now closed on the final corrected bytes.

A complete unfiltered Release suite was then executed once, without retry, on
the Gate-A bytes:

| Project | Result | Wall time |
|---|---|---:|
| ContractTests | 13 passed / 0 failed / 0 skipped | 3.053 s |
| ArchTests | 128 passed / 0 failed / 0 skipped | 4.220 s |
| UnitTests | 189 passed / 0 failed / 0 skipped | 4.743 s |
| IntegrationTests | 750 passed / 1 failed / 1 skipped | 1917.609 s |

The sole failure was
`M13_b4_database_contains_no_raw_byte_package_or_delivery_surface`: its
parameter used the cumulative six-table `Tables` manifest, which intentionally
includes C1 `raw_export_job_source_bindings`; eight legitimate metadata fields
matched the legacy name/type heuristic. Direct catalog measurement against the
exact five B4-owned tables returned zero. The count-neutral correction uses
`B4OwnedTables` consistently for the M6 table/column ACL and M13 assertions;
the cumulative manifest remains limited to M1 catalog accounting.

Post-run targeted evidence is complete: the three canonical tests passed;
mutating M13 back to `Tables` turned RED with `Actual: 8`; restore SHA-256 is
`CDF4D75867C6E367953BAA372206FB9CEDC15D2A8395491FE336011B0322AD93`;
the restored M13 positive control passed. This correction changed no
production, schema, migration, state, readiness code or contract surface.

The diagnostic Integration TRX SHA-256 is
`F80EE7B98BEF547546B2BFC2F4121CD410FB39788677F66A96E16F15DFDF6B82`.
It is retained as historical evidence only and is not the final closeout run.

The Homeowner then authorized exactly one replacement complete unfiltered
Release suite on the corrected, restored C1 bytes. No repository byte was
changed before or during that run, and no retry was performed. The replacement
run completed cleanly on 2026-08-16:

| Project | Result | Command wall time |
|---|---|---:|
| ContractTests | 13 passed / 0 failed / 0 skipped | 1.925 s |
| ArchTests | 128 passed / 0 failed / 0 skipped | 2.007 s |
| UnitTests | 189 passed / 0 failed / 0 skipped | 2.605 s |
| IntegrationTests | 751 passed / 0 failed / 1 skipped | 1897.132 s |
| **Aggregate** | **1081 passed / 0 failed / 1 skipped / 1082 total** | |

The single skip is the intentional manual golden-vector generator:
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
There was no non-canonical failure.

Final closeout evidence SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| Contract TRX | `2E15DDCCC4A2E6847AF383057A1E531D8F7C847F374323334CE6ACEAF2C4E5CB` |
| Architecture TRX | `1E5AF3C4591447D2B3A6340940ECF96A7B41F90FAC6D50C00A178982DC312BDA` |
| Unit TRX | `6036EF34883A7963E7E14E15C4BDC2C57AA5D853D06CFA6117EB2DFA96772BF4` |
| Integration TRX | `6A9B8D18A7F79A3A34A2B0289BD4780F71822CA9BF3AFAC96C73866EC5D54250` |
| execution summary | `D404EF5A973B995DEA71B4F3E2C36C7B4F07B1D910B00F18ED58AC6E5E8C1890` |

The evidence directory is outside the repository at
`D:\Task\Remote Signing\TagEkyc_C1_final_replacement_20260816`. The pinned
SoftHSM2 2.5.0 module and utility and Docker client/server 29.4.2 were used.
No task-created `tagekyc-postgres-test` container remained after the run.

Historical disposition: `PASS - READY FOR INDEPENDENT CLOSEOUT REVIEW`, now
superseded by the rejected-bundle decision and bounded correction section
above.

## Final current-byte closeout and TIP-68 bounded mitigation

After the bounded C1 correction cycle, an intermittent predecessor TIP-68
host-startup failure remained under investigation. Homeowner granted standing
authority to diagnose and remediate TIP-68 test-environment defects without
changing production or governance contracts.

The permanent-path provenance is append-only:

| Segment | Paths | Authority and purpose |
|---|---:|---|
| ratified C1 dispatch | 39 | original controlled implementation ceiling |
| C1 executable amendments | +3 | DK readiness validator, R3 snapshot tripwire and B4 readiness validator |
| TIP-68 mitigation | +2 | `Tip68ProdHsmSigningTests.cs` and `Tip68SoftHsmE2ETests.cs` |
| **final reviewed source set** | **44** | no path 45 required |

The two TIP-68 classes previously had no shared xUnit scheduling boundary even
though the SoftHSM fixture mutates process-wide `PATH` and `SOFTHSM2_CONF`
while the production-host test starts and disposes a real application host.
The two classes now share `Tip68ProcessEnvironmentCollection`, whose collection
definition sets `DisableParallelization = true`. The existing host-startup
proof also verifies semantically that both class attributes and the nonparallel
definition remain present.

Exact TIP-68 anchors:

| Artifact | Before bounded isolation | Final SHA-256 |
|---|---|---|
| `Tip68ProdHsmSigningTests.cs` | `8565A8C7D51D401781D3DA067472E447DCD5891FE7FDBEAEBE9A51367BC7FEB9` | `E3015BD6F4C68E6EA7483F0D2D95A5F0CEFFEB4680A0526B4B709D099ADAAC6A` |
| `Tip68SoftHsmE2ETests.cs` | `6FDA077FCE62A7C2B79070B75601852D135A008EB256487BFFAC8BDB1112F6BE` | `7073DF500258BF89CB4473E25900B26948E3A6255FEC8C475D42991CF7D0A3B7` |

Removing only the SoftHSM class's collection membership turns the existing
host-startup proof RED at the semantic isolation assertion (`Expected:
Tip68ProcessEnvironment; Actual: null`). This mutation proves the guard against
silent removal of isolation. It does not reproduce the timing failure and is
not represented as causal proof for the historical `ObjectDisposedException`.
After byte-exact restoration, 20 repetitions of all 14 TIP-68 tests completed
280/280 PASS with zero failure-diagnostic markers.

The exact disposition is:

```text
ESTABLISHED:
- a real unsafe process-global scheduling condition existed;
- the nonparallel collection removes that overlap deterministically;
- the semantic mutation proves the isolation guard bites.

NOT ESTABLISHED:
- that this hazard caused the historical ObjectDisposedException;
- the exact historically disposed object or competing actor;
- that no independent cause can reproduce the symptom.

TIP68-HOST-STARTUP-ROOT-CAUSE-01: MITIGATED — ROOT CAUSE UNPROVEN
```

Passive Path-43 exception-chain, disposed-object and lifecycle instrumentation
remains in place for future full-suite runs. Debt
`TIP68-HOST-STARTUP-ROOT-CAUSE-01` remains open; neither the mutation nor a
green stress/full-suite run closes causation.
Homeowner approved this bounded disposition on 2026-08-17. The mitigation
review R2 bundle SHA-256 is
`B08D897E7387C1A6E8EA7565CFB0631B2F5C109C9D50968A3580F3D2E8EEBB2F`.

One complete unfiltered Release suite was then executed on the final C1 plus
TIP-68 bytes, with no intervening repository edit or retry:

| Project | Result |
|---|---|
| ContractTests | 13 passed / 0 failed / 0 skipped |
| ArchTests | 128 passed / 0 failed / 0 skipped |
| UnitTests | 189 passed / 0 failed / 0 skipped |
| IntegrationTests | 751 passed / 0 failed / 1 skipped |
| **Aggregate** | **1081 passed / 0 failed / 1 skipped / 1082 total** |

Final current-byte evidence SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| Contract TRX | `C1B78ECE0E4B4AC041670DEE801C2023C0793BB96976FEAB256936668476679C` |
| Architecture TRX | `F422A4A7CA260E4F3708E0BBC2D514E3001DF99D8E406CB8A36CE3476FC9D692` |
| Unit TRX | `9644FAF0BA927314156E5728E3C2D766EF091AC9C474744C5A3CDDFB1B75757C` |
| Integration TRX | `01CFF45E7CA2B8EA0C09EA6F5EA98F332B485C8EC7CEAAA97A2484D60946F24C` |

The single skip remains the intentional manual golden-vector generator. The
full run emitted zero `TIP68_HOST_STARTUP_FAILURE_DIAGNOSTIC_V1` markers.
Release build remained `0 warnings / 0 errors`; staged paths and task-created
container/process residue were zero at evidence freeze.

Current disposition is
`IMPLEMENTATION_CLOSED / INDEPENDENT_CLOSEOUT_REVIEW_PENDING`. This does not
authorize stage, commit, push, merge, PR, deployment, production activation,
real Raw BIO, production C2, package construction or delivery.

## Boundaries

No staging, commit, push, merge, PR, deployment or production activation was
performed. Unrelated working-tree changes were not staged or altered as part
of this slice.

## C2 trusted-recipient handoff successor

TIP-88C1-C2 implementation uses the exact C1 server-derived
`RecipientClientApplicationId` already present in the locked job context.
`C2AssemblyPreparationRequest` carries that value directly from
`RawExportAssemblyOrchestrator`; C2 does not add a database lookup, actor bypass,
new C1 SQL function, grant, role or topology member. The C1 two-pass source-read
boundary remains executable and green: the affected C1 census is `31/31 PASS`,
and a real third digest pass turns C220 RED at `Expected: 2; Actual: 3`.

This successor records only the authorized C1→C2 handoff. It does not authorize
recipient management, delivery, download, production activation or real Raw BIO.
