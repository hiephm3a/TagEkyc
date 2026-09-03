# TIP-88C1-C2 Recipient Package — As-Built

Status: `IMPLEMENTATION_CLOSED / INDEPENDENT_CLOSEOUT_REVIEW_PENDING`
Version: `0.3`
Date: `2026-08-18`
Repository: `TagEkyc`
Branch: `tip-88a-raw-export-policy-catalog-build`
Baseline HEAD: `d9735f84b3946887d8301dda9686a7303aa71683`

## Authority and provenance

The implementation binds the ratified dispatch v0.2.1-R3C1 SHA-256
`0790D1A6828C3365178011F0E10A1B58757B4FE7E2214828C55A6D720B0BB64D`
and reviewed bundle SHA-256
`BFDC723413BDECDB7E55AA2447AD534F87618A69C1038E01978DFD0AC09682CF`.

The permanent-path accounting is append-only:

| Segment | Paths | Disposition |
|---|---:|---|
| ratified dispatch | 34 | exact original allowlist |
| snapshot-tripwire amendment | +1 | `Tip88C1B2R3VerifiedCiphertextStagingTests.cs` |
| catalog-comparison amendment | +1 | `Tip88C1B2SourceFinalizationTests.cs` |
| final source set | **36** | no path 37 required |

The generalized debt `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` covers both raw
ModelSnapshot hashes and catalog text returned through `pg_get_functiondef`.
The two catalog comparisons in R301/F415 are normalized symmetrically for real
and JSON-escaped CRLF/LF. Raw-file snapshot pins remain commit-gated. Closure
requires a future shared normalization/formatting helper; this C2 correction
does not claim to close the inherited pattern.

## Implemented boundary

C2 now consumes the exact server-derived `RecipientClientApplicationId` from
C1, selects and locks an active recipient key, freezes the exact key snapshot,
creates a 32-byte provider-operation token inside the service boundary, stores
only its SHA-256 digest, encrypts the authenticated C1 assembly with a fresh
AES-256-GCM CEK, wraps that CEK with recipient RSA-OAEP-SHA256, and conditionally
creates one single-part object at the immutable key
`raw-export/c2-package/v1/{PackageId:N}`.

The package contains a bounded canonical envelope, ordered authenticated data
frames and an authenticated completion record. TagEkyc persists no usable CEK,
recipient private key or raw provider token. Restart recovery uses the exact
frozen key/provider/object snapshot; `PutInFlight` and `PutOutcomeUnknown`
cannot reset, mint a fresh CEK or silently abort. Mismatch is quarantined.
Armed absence remains non-terminal and requires durable reconciliation.

## Durable model and SQL

The additive migration creates exactly three tables:

- `raw_export_recipient_key_registrations`;
- `raw_export_recipient_package_preparations`;
- `raw_export_recipient_package_events`.

It creates three capability roles, preserves three externally provisioned
LOGIN roles, installs exactly ten C2 SQL functions, pins owner/search-path/ACL,
and binds package lineage through the composite attempt identity/fence FK.
Down removes C2-owned tables/functions/capability roles and preserves LOGIN
roles. Apply → Down-to-C1 → reapply is covered by C201. EF reports:

```text
No changes have been made to the model since the last migration.
```

Canonical artifact anchors before final full-suite execution:

| Artifact | SHA-256 |
|---|---|
| migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` |
| Designer | `4300C92314A6E5C2114FB4463D869113C1D0D96077DC6A3DE56D189DAB73E61F` |
| ModelSnapshot raw bytes | `2DDCC2F21742AE1BC9B025C2AC7B27F40B74369FF8F49C18F0FC976932DD9540` |
| readiness validator | `2399E5035823BF28F755F57C91727F19EB5D2AA333B296A6593E8A457EBF6747` |
| S3 provider | `0F5D1CA658CA62D6B2B261FDC3FEEABE438ABEF8839F9FAA6634E5407321502D` |
| orchestration/service | `83D5F304081C6847B3DE6C61AEB183ABAAC275493C83D1B9BFC03680EB017BF0` |

## Executable findings closed on code

Implementation exposed and closed bounded executable defects without changing
the ratified state/outcome/codec contract:

1. direct equality input had to participate in replay conflict precedence;
2. exact replay had to precede active-key and generic revision rejection;
3. finalized replay had to precede stale predecessor rejection;
4. abort required two independent positive-absence observations;
5. mismatch had to persist quarantine;
6. restart absence had to persist `PutOutcomeUnknown` without fresh Put;
7. C2 MinIO fixture constructed `RecipientPackageProviderConfiguration` with
   writer/reconciler credentials reversed; the exact argument order was fixed.
8. C2 reserve required the post-Seal assembly identity even though canonical C1
   calls C2 Prepare before Seal; reserve now locks the pre-Seal preparation
   disposition and preserves exact JobId/AttemptId/FencingToken/AssemblyId/
   AssemblyFingerprint binding.
9. the frozen public-key fingerprint was not proven to be SHA-256 of the SPKI
   used to wrap the CEK; both the application boundary and locked SQL reserve now
   fail closed before encryption/provider I/O on mismatch.
10. a persisted provider snapshot could drift from restart configuration and
    leak an untyped locator exception; recovery and lifecycle now quarantine the
    existing row as `ProviderIdentityConflict` before any operation against the
    drifted provider.

The last defect produced a real MinIO `403 Access Denied` because the production
provider attempted Put with the read-only reconciler credential. C210 now locks
the mapping: swapping the credentials returns `OutcomeUnknown` and turns the
proof RED. No production or contract change was needed.

## Proof and mutation accounting

The active census remains exactly `C201–C226 = 26` facts:

| Project | Canonical result |
|---|---|
| C2 Unit | 3 passed / 0 failed |
| C2 Architecture | 6 passed / 0 failed |
| C2 Integration | 17 passed / 0 failed |
| **C2 total** | **26 passed / 0 failed** |

Every proof ID has a discriminating mutation that turns its owning assertion
RED, followed by source restoration and canonical positive control. There are
28 mutation TRX files because C203 and C210 retain additional diagnostic
attempts; the proof-ID census remains 26 and count-neutral.

Important behavioral bites include:

- C201 stale ModelSnapshot pin;
- C202 wrong exact function census;
- C203 function-surface drift;
- C204 real C1 orchestrator → real recipient-package provider execution, including
  the pre-Seal `Preparing` observation and exact final lineage;
- C205–C209 domain/AAD/token/wrap/frame-bound mutations;
- C210 removal of `If-None-Match: *`, writer/reconciler swap and restart provider-
  identity drift with zero operations against provider B;
- C211–C216 replay, key-SPKI/fingerprint binding, uncertainty, absence and
  quarantine mutations;
- C217 credential sharing; C218 batch delete; C219 token-zeroization removal;
- C220 a real third C1 source pass (`Expected: 2; Actual: 3`);
- C221 unknown-state mapping; C222 reverse key/package lock order;
- C223 versioning-before-Object-Lock precedence;
- C224 skipped completion-tag verification;
- C225/C226 an exported public package-download locator.

The final canonical test SHA anchors are:

| Artifact | SHA-256 |
|---|---|
| C2 Unit tests | `B5C36A4B6A8E11DE7EEDB85740F2D470A7DEB933F0D750AC706C76258E680250` |
| C2 Architecture tests | `E73B7DF5098A5EC30C59D01605B9E0FAD98C2128D88B0B366485683AD2042D04` |
| C2 Integration tests | `6F55D69536E1860951352AB1EE70BAEBF5C9EF2E6EE8AEDA12CD2183632688EA` |
| C1 Integration tests (real C2 helper) | `C2203FBCDDB970FBCC8921993347D47D9D25AA75CD07B6713C828A9E1D2DFBDC` |
| R301 line-ending proof | `5B7F9EE17565BDA64EE718E6EE3034E246F9E05B44B5F94A70403E6FDC733034` |
| F415 teardown proof | `D293AE48CAD98F9CC8BA5E5D3770A87C1750FCD0348A9E862F1F97BB876F37C1` |
| MinIO fixture | `25BA623AA4949418E541387B2E57BB08896B0F47BC1DA71669A82E21957073B1` |
| C1 orchestrator | `5DD6CD25AA92E924CAC0C9955E1F088DE572779F917CBE1F083101158F66573E` |

## Predecessor and validation evidence

The affected C1 surface remains active and green:

```text
C1 Unit:         1/1 PASS
C1 Architecture:4/4 PASS
C1 Integration: 26/26 PASS
Total:          31/31 PASS
```

C220 executes the landed two-pass C119 boundary rather than merely counting
source text. A real third source read turns C220 RED at `Expected: 2; Actual: 3`.
The Release solution build is `0 warnings / 0 errors`; `git diff --check` passes;
staged paths are zero.

## Final full-suite disposition

The first closeout attempt is retained as superseded historical evidence:

```text
Contract:      13 passed / 0 failed
Architecture: 134 passed / 0 failed
Unit:         192 passed / 0 failed
Integration:  760 passed / 8 failed / 1 skipped
Integration TRX SHA-256:
96D0401E6588BAFC97A90E4D8DD15F2DAB5E8E2F333B6FC737ACF1AE88131F63
```

R301 and F415 were the two root failures, both caused solely by opposite
CRLF/LF representations. F415 stopped before reapply and left the shared DB at
R3. Five C2 failures returned exact `42P01` missing-recipient-table evidence.
C202 was not inferred: after latest-schema restoration it passed explicitly,
proving its opaque failure was also a schema-state cascade. C202 now names each
failed sub-census instead of reporting bare `False`.

R301 and F415 normalize both comparison operands. F415 uses `finally` to migrate
to current latest even when its assertion fails. Independent `BEGIN→BEGIX`
mutations turn each proof RED after normalization, and canonical restoration is
3/3 PASS for R301/F415/C202.

```text
FINAL_COMPLETE_UNFILTERED_RELEASE_SUITE: CLOSED_GREEN
INDEPENDENT_CLOSEOUT_REVIEW:             PENDING
```

Only the final authorized full-suite evidence and review-only bundle may close
these two lines. This file grants no stage, commit, push, merge, PR, deployment,
production activation, real Raw BIO, recipient management, delivery or package
download authority.

The authorized replacement run also did not qualify as closeout evidence:

```text
Contract:      13 passed / 0 failed
Architecture: 134 passed / 0 failed
Unit:         192 passed / 0 failed
Integration:  762 passed / 6 failed / 1 skipped
Integration TRX SHA-256:
2C4B0DE45348CC3B0ED658CF8B2A413C6DB97846CD75B49049668C1247CE89D2
```

R301 and F415 passed; their line-ending correction is closed. The six failures
were C202/C211/C213/C214/C215/C222 on an absent C2 catalog. TRX ordering proves
the immediate owner: C125 passed at `23:42:21` after migrating only to the C1
migration; no later C1 proof changed migration state; the first C2 failure was
C222 at `23:42:40`; C201 did not reapply latest until `23:43:13`. C202's new
diagnostic reported every C2 role/membership/table/function/ACL/surface absent.
This is a passing predecessor test leaving shared DB state stale, not a C2
production defect. C125 requires a separately authorized unconditional latest-
migration restore before another full suite.

The Homeowner shared-DB migration-state correction authority was then applied
inside the existing 36-path allowlist. C125 retains its down/reapply assertions
but now restores the current latest migration parameterlessly in `finally`.
`PostgresPersistenceFixture.AssertLatestMigrationAsync` performs one applied-
migrations query and reports the exact test name, actual migration and expected
latest migration.

The canonical C125-then-C202 control is `2/2 PASS`. Removing only C125's latest
restore produced the exact attributed guard failure:

```text
Shared database migration-state leak: test
'C125_migration_model_is_clean_and_b4_down_contract_is_present' left the
database at '20260815120000_Tip88C1C1ResolverAssembly'; expected latest
'20260818120000_Tip88C1C2RecipientPackage'.
```

In that mutation C202 also turned RED with every C2 catalog category absent.
Independently neutering the guard while retaining the leak made C125 silently
PASS and moved the failure to C202, proving that the guard is behaviorally
discriminating. Canonical bytes were restored and C125-then-C202 returned to
`2/2 PASS`.

The one final replacement complete unfiltered Release suite then completed on
the restored bytes with exit code zero for every project:

```text
Release build: 0 warnings / 0 errors
Contract:      13 passed / 0 failed / 0 skipped
Architecture: 134 passed / 0 failed / 0 skipped
Unit:         192 passed / 0 failed / 0 skipped
Integration:  768 passed / 0 failed / 1 skipped
Aggregate:   1107 passed / 0 failed / 1 skipped / 1108 total
```

The only skip remains the intentional manual golden-vector generator. The
canonical TRX SHA-256 values are:

```text
Contract:     0BFFF91198543B9F8EC4E62784400CCDAA99804699801DBE9537075616FCE14F
Architecture: 69A44C30D8D9E9204C08DDF10FD6E107C1D633E8124CB09A982EFA7AA355CAB5
Unit:         0E2CD7D977805ED990B9B10D247EEBB1A01F9A5449F421336BBC40843FEA23BF
Integration:  9C831B5F2332F5A74BA99410BFBFED64B2A450200AD4F7E182F5FC82B90FC1BE
```

The two shared-DB correction files on the final run are bound as:

```text
Tip88C1C1ResolverAssemblyTests.cs:
681765A90DDD050B0ADE1434C7064C24A1DFA9902632267870793F85EF584ED6

PostgresPersistenceFixture.cs:
8A3FE7D0CA02E0CC60205DDDAAF8AC7FA36EAA568EC0763F2F28F0CDCEBE3FF3
```

No test container residue remained, staged paths stayed zero and
`git diff --check` passed. Both earlier failing suites remain historical only
and are not represented as closeout evidence.

## Bounded closeout correction F01/F02/F03

Closeout bundle
`6A059B997D7422FF7A16B7932AA6316B410778561FF6969869B0EC67199906B7`
is `REJECTED_FOR_COMMIT_HISTORICAL_ONLY`. GPT raised `C2-CR-F01/F02/F03` and
CC independently confirmed all three on code; CC's earlier PASS is superseded,
not deleted. The rejected bundle's mechanical `1107/0/1` census did not prove
the canonical product path because every C2 fixture began from an already-sealed
assembly.

The correction stayed inside the exact 36-path allowlist, retained ten C2 SQL
functions and retained proof IDs `C201–C226 = 26`:

- F01: `raw_export_reserve_recipient_package` reads
  `raw_export_assembly_preparation_dispositions`, which exists before provider
  I/O, instead of the post-Seal identity. C204 now constructs the real
  `RawExportAssemblyOrchestrator` with the real
  `RecipientPackagePreparationProvider`. At object Put it observes C1
  `Preparing` and zero assembly-identity rows; the final durable sequence is C1
  `Preparing → Pending → SealCommitted → Finalized` and C2
  `SnapshotFrozen → PutStarted → Prepared → Finalized`. Reintroducing the
  identity-table prerequisite turns C204 RED with
  `Expected: Sealed; Actual: PreparationConflict` before Seal.
- F02: a valid RSA SPKI with a deliberately different 32-byte fingerprint now
  returns `Unavailable`, writes no C2 row and leaves provider Put/Inspect counts
  at zero. Removing both application and SQL digest checks turns C212 RED at
  `ASSEMBLY_MUST_NOT_BE_OPENED` because the malformed key crosses into crypto.
- F03: a `PutInFlight` row persisted under provider A and restarted under
  provider B returns `Conflict`, becomes `Quarantined`, and leaves provider-B
  Put/Inspect/Delete counts at zero. Removing only the pre-operation identity
  guard turns C210 RED at `PROVIDER_B_MUST_NOT_BE_CALLED`.

Each scratch mutation was restored byte-identically. Canonical post-restore
SHA-256 values are migration `874F11A0…D1303`, service
`83D5F304…17BF0`, C2 Integration `6F55D695…688EA`, and C1 Integration
`C2203FBC…DFBDC`. Targeted corrected census is `17 Integration + 6
Architecture + 3 Unit = 26/26 PASS`.

Exactly one replacement complete unfiltered Release suite ran on these frozen
bytes; there was no retry:

```text
Contract:      13 passed / 0 failed / 0 skipped
Architecture: 134 passed / 0 failed / 0 skipped
Unit:         192 passed / 0 failed / 0 skipped
Integration:  768 passed / 0 failed / 1 skipped
Aggregate:   1107 passed / 0 failed / 1 skipped / 1108 total
```

The only skip is the intentional manual golden-vector generator. Final TRX
SHA-256 values are:

```text
Contract:     B5CA7A0904EF481EC1C0759CF0B6308BB052DD8359AFE51AB6A668031C523FBB
Architecture: 2F1D1E20380B95817C5DEA967F263B803528BAD59DAC479F377065E765EB2AE8
Unit:         401C816B7E83E459443BF952D2AB8397382E21CBFB8F78FF3C288AF916C92033
Integration:  4EB83ACFD8AE2C72CFAED6432F155CBBCD5534C6F5B7645FD21B0225C1E7D624
```

The manifests in the successor review bundle are UTF-8 with LF line endings.
Stage, commit and push authority remain `NONE`; independent closeout review is
`PENDING`.

## Open boundaries and debt

- `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` remains open and future-owned.
- `SHARED-DB-MIGRATION-STATE-DEBT-01` remains open and future-owned. Sixteen
  integration-test files contain 71 migrations to non-latest targets with
  uneven teardown coverage. The C125 correction and fixture guard prevent the
  observed silent cascade but do not close the general shared-database pattern.
  Closure requires per-test database isolation for migration proofs or one
  shared restore-to-latest helper used by every down-migration site.
- Recipient enrollment/management, delivery and download remain out of scope.
- Real Raw BIO and production activation remain prohibited.
- The recipient public-key registry is deployment-seeded; this slice exposes no
  management API.
- TagEkyc retains no package-decryption capability after preparation.
