# TIP-88C1-B2 DURABLE-OBJECT — As-built evidence

Version: 0.11
Status: WHOLE-TREE R3 REMEDIATED AND VALIDATED — R4 INDEPENDENT REVIEW PENDING
Evidence date: 2026-08-06
Repository HEAD: `0e301445ab12c3890e7bec72ef88ca562c4e39a8`
Ratified dispatch SHA-256: `EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918`

## DK-PROD CHECK representation normalization

The Homeowner-authorized representation-only correction added
`.ReplaceLineEndings("\r\n")` to exactly these three runtime CHECK-constraint
SQL literals. No visible SQL token, predicate, identifier, operator or database
semantic changed. Comparing the old and new extracted SQL after normalizing both
to LF returned `VISIBLE_EQUAL=True` for all three constraints.

| Constraint | File SHA-256 before | File SHA-256 after | Normalized visible-SQL SHA-256 |
| --- | --- | --- | --- |
| `ck_raw_export_key_provider_operation_text` | `E834BEF18E4F857C611185972A5AB3E8278662D12E60E9BFD479D5DCECAE8314` | `089D1738EEC0B03BD15B0E9069747138D7EE8C64C37BFCF94217D01F79A4CAA3` | `4942863DA75F7A058DCD559C11C42A154E8C3252DD06DAA7BC2C413492BDAD96` |
| `ck_raw_export_attempt_key_reservation_text` | `E70AAF8DCA0C4393CF52BB7B2BFF1926D63E196EC6C79353125DA34D7A8679B6` | `150FB4F05D5F9FF96367B23CF9A0CE422EA62C4F92BEBE8788652A4C4FEB3C62` | `6FCBFC458A73AFF019FA644EBEDCBCF38F2DA748F661027BEE55488B9E005EF9` |
| `ck_raw_export_attempt_key_event_text` | `50712A54E728847918F5B9AA09286CAB7F7EC04033E5B3B2675CAD8F3D623768` | `3B878D38607455AB389535E1B76250FE1897D989374763E19CEF20E1BAEFF7E5` | `4C65EB44BC49E5554E219B63FA2B5F85FDF193AA4A5F7D840E1658E802C64011` |

After rebuilding the Debug Infrastructure assembly, the corrected gate returned:

```text
No changes have been made to the model since the last migration.
```

No snapshot or landed migration was changed for this correction. Release build
completed with 0 warnings and 0 errors. The canonical positive controls passed:

```text
Tip88C1B2DurableObjectCustodyArchTests: 6 passed, 0 failed, 0 skipped
Tip88C1B2DurableObjectCustodyTests:     22 passed, 0 failed, 0 skipped
```

## Open mutation-gate STOP/RRI

The required O06 scratch mutation removed only the expected-revision predicate
from `raw_export_arm_provisional_object_put`:

```text
h."StateRevision" <> p_expected_state_revision
```

`O06_writer_arm_is_revision_fenced_and_idempotent` remained GREEN (1 passed).
Its current assertion only searches the function definition for the text
`StateRevision`; that text remains elsewhere after the load-bearing predicate is
removed. The test therefore does not prove stale-revision rejection and cannot
satisfy the ratified mutation manifest.

The migration was restored byte-identically after the scratch run:

```text
SHA-256 3519080464F9A74C94E017E668E347A364F9111EEF106AA621C386EB234AD4B6
```

Per the dispatch STOP/RRI rule, the remaining mutation sweep and full-suite
closeout were not continued. No stage, commit, push, merge, PR or deployment
occurred.

## Behavioralization remediation STOP/RRI

The Homeowner-authorized behavioralization pass first ran the required
non-governed MinIO feasibility probe against:

```text
minio/minio:RELEASE.2025-09-07T16-13-09Z
sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e
```

Observed results were `FIRST_STATUS=200`, `SECOND_STATUS=412` and
`ORIGINAL_UNCHANGED=True`. The throwaway container was removed afterward.

O06 was then rewritten to execute the real begin/arm operations and observe the
returned outcome, persisted state, revision and event count. Its positive run
exposed a frozen-migration defect before the arm operation could be reached:

```text
SQLSTATE 42883
function pg_catalog.coalesce(text, unknown) does not exist
```

The migration schema-qualifies the SQL `COALESCE` construct as
`pg_catalog.coalesce(...)`. PostgreSQL accepts creation of the PL/pgSQL bodies
but fails when the affected statement is executed. The migration contains 19
such occurrences across begin and state-transition success/exception cleanup
paths. Consequently the behavioral state-machine tests cannot execute against
the frozen bytes.

The remediation packet forbids migration/SQL edits and requires STOP/RRI when a
migration change is necessary. No migration byte was changed, and no provider
fix or further behavioral test rewrite was attempted after this result.

## Superseding behavioralization evidence

The historical STOP above was closed by the later Homeowner migration/provider
implementation-bug authorization. The migration was corrected only where real
behavioral tests surfaced executable defects; the state model, signatures,
ACL manifest, outcome tokens, evidence domains and readiness codes were not
changed. The corrected migration SHA-256 is:

```text
4351B6EE40B13A0A7267265C97E2044B44F9B27C66CD5FF9241DFBFADF9BC428
```

All twenty exact `pg_catalog.coalesce(previous_context,'')` occurrences were
changed to `COALESCE(previous_context,'')`. The original affected source lines
were 278, 282, 306, 308, 332, 334, 367, 369, 403, 405, 422, 424, 441, 443,
460, 462, 479, 481, 498 and 500. A final sweep found zero schema-qualified
`coalesce`, `nullif`, `greatest`, `least` or `case` constructs. O06 then passed,
and removing its expected-revision predicate made the behavioral stale-arm
assertion RED before byte-identical restoration.

The positive-control census after behavioralization and before the narrow
precedence proof was:

```text
Tip88C1B2DurableObjectCustodyTests:     22 passed / 0 failed / 0 skipped
Tip88C1B2DurableObjectCustodyArchTests:  6 passed / 0 failed / 0 skipped
Release build:                           0 warnings / 0 errors
```

## Readiness owner-ACL false-negative correction

Canonical catalog inspection proved both DURABLE-OBJECT tables are owned by
`tagekyc_raw_export_deployer`. Their fourteen exploded ACL rows all have
`acl.grantee = c.relowner`; the count of non-owner ACL rows is zero. The old
readiness query rejected those legitimate owner rows and returned
`PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID` before reaching the posture
probe.

Exactly one predicate was added:

```sql
AND acl.grantee <> c.relowner
```

| Validator state | SHA-256 |
| --- | --- |
| Before owner exclusion | `52CEAB7B8683BE96E77CBD8C0DF52131071FD821E6928C95FFDCDDCD4497F6F0` |
| After owner exclusion, before precedence correction | `C1083CF6C7405F09F4CC23D30EB4C7392FF7552A26A581B2E01969ADCCADAA96` |

Negative controls independently granted table `SELECT` to PUBLIC,
`tagekyc_runtime`, `tagekyc_raw_export_custody_encryptor` and a disposable
unrelated NOLOGIN role. Every case returned exact
`PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID`; each grant was revoked and the
disposable role dropped. The post-restoration ACL manifest equalled the
canonical manifest. Removing the owner-exclusion predicate made O17 RED:

```text
Expected: PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE
Observed: PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID
```

After restoration, O04/O17/O18 passed 3/3.

## Object-Lock/versioning precedence correction

Real MinIO proved that an Object-Lock-enabled bucket necessarily has versioning
enabled. With the v0.5 order, the Object-Lock scenario was shadowed:

```text
Expected: PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED
Observed: PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED
```

Under the temporary Homeowner proof bridge, only the two adjacent checks were
swapped so Object Lock is evaluated before versioning. Dispatch v0.5 remains
byte-immutable. The uncommitted validator proof bytes are:

```text
Before: C1083CF6C7405F09F4CC23D30EB4C7392FF7552A26A581B2E01969ADCCADAA96
After:  0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42
```

Real-provider O17 distinguished missing bucket, versioning-only,
Object-Lock-plus-versioning, lifecycle-rule, public-policy and compliant
postures. Both lock and versioning codes became reachable. Three separate
mutations bit for the required reasons:

1. old order: Object-Lock expected lock code, observed versioning code;
2. Object-Lock check removed: expected lock code, observed versioning code;
3. versioning check removed: expected versioning code, observed
   `PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE`.

All validator mutations were restored to SHA-256
`0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42`.

### Option-B executable provider interpretation repair

Separately from the precedence contract correction, the same real-provider
pass exposed one executable adapter interpretation bug:
MinIO reports exact status `Off` when versioning has never been enabled. The
adapter now accepts only null/empty or exact `Off`; `Enabled` and `Suspended`
remain fail-closed. Provider hashes:

```text
Before: 83AFB62C2A95C469C6CE63E59A355A28D8214CB8659621E00579160D6DED5235
After:  A2A0F3EC08CD1AECA4508979328CCFF4768523A8A73E15E67D61264CA4C52116
```

This provider repair was made under Addendum 3A standing Option-B executable
defect authority. It does not change the readiness contract and is not part of
the precedence correction's ratification scope. The real-provider reachability
proof consumes both independently classified fixes without conflating their
authority.

The reviewed correction is
`tip_88c1_b2_durable_object_readiness_precedence_correction_v0_2.md`, SHA-256
`4AB2FBED6270829BE2AFD4FAD1D9347291C85B4365D01EFA7F7BFB68F01FC9B2`.
It supersedes the unratified v0.1 draft, whose review identified the scope
conflation.

Homeowner ratification is recorded append-only in
`tip_88c1_b2_durable_object_readiness_precedence_correction_ratification_v0_1.md`,
SHA-256
`4CEBF53549AF70BF1A49888BFFA71C97A9A78FDD8C6285EDC70D6C2C97E3400B`,
with status `RATIFIED_NARROW_CORRECTION`. The record binds the independent
`PASS — READY FOR HOMEOWNER RATIFICATION` verdict by
`OpenAI GPT-5.6 Thinking` on 2026-08-05 and identifies O17/O20 as the affected
proofs. It authorizes no implementation change, commit, push, deployment,
production activation, Raw BIO, R2 or delivery.

## Full-suite STOP/RRI: disabled-topology DI registration

The unfiltered Release-suite closeout exposed a production registration
regression outside the three-artifact precedence-correction allowlist. A focused
rerun reproduced it independently in:

```text
Tip83E1ReadinessEndpointTests
  .Non_production_readiness_returns_ready_without_running_checks
```

The host fails at `builder.Build()` before any PostgreSQL or MinIO operation:

```text
Unable to resolve service for type
TagEkyc.Infrastructure.Persistence.TagEkycDbContext
while attempting to activate
TagEkyc.Infrastructure.RawExport.ProvisionalObjectCustodyRepository

Unable to resolve service for type
TagEkyc.Infrastructure.Persistence.TagEkycDbContext
while attempting to activate
TagEkyc.Infrastructure.RawExport.ProvisionalObjectCustodyReadinessValidator
```

`AddTagEkycProvisionalObjectCustody` currently registers both scoped services
before returning for every topology other than `S3CompatibleDurable`. The
development host uses the disabled/non-durable topology and does not register
`TagEkycDbContext`, so `ValidateOnBuild` rejects the service graph. This is not
a SoftHSM, PostgreSQL, MinIO or readiness-precedence failure.

The minimal repair requires the second production file
`ProvisionalObjectCustodyServiceCollectionExtensions.cs` and corresponding
behavioral coverage. The active correction packet explicitly requires STOP/RRI
when a second production file is needed. No registration or test byte was
changed. The full-suite result is therefore not accepted as a closeout census;
the precedence candidate remains uncommitted and unstaged pending narrow
Homeowner authority.

## Disabled-topology DI relocation and test-authority STOP/RRI

Addendum 3 authorized relocating the two existing scoped registrations after
the existing topology/capability guard. No predicate, type, interface, lifetime,
switch arm or provider registration changed:

```text
Before: 7696EEA62122D6605D2C938AA0DD9A4C02154CA66B2C0DC7F25D037AB40740AD
After:  9EFAE47D24DBDC6841995A30350DBEB21496CBDE167ED975EE0E4A813CD0B6B1
```

After rebuilding the Release test project, the focused non-production host
test passed 1/1 and the build completed with zero warnings and zero errors.
The first post-edit run used stale `--no-build` output and correctly was not
accepted as evidence; the rebuilt run is the canonical result.

Mutation A was executed independently for both registrations. Moving only the
repository before the guard made the focused host test RED because
`TagEkycDbContext` could not activate `ProvisionalObjectCustodyRepository`.
Moving only the readiness validator before the guard produced the corresponding
RED for `ProvisionalObjectCustodyReadinessValidator`. The source was restored to
SHA-256 `9EFAE47D24DBDC6841995A30350DBEB21496CBDE167ED975EE0E4A813CD0B6B1`,
and the focused positive control passed again.

The existing permanent tests cannot express all remaining Addendum 3 proof
obligations. `OA2_runtime_graph_cannot_resolve_s3_or_foreign_object_capabilities`
checks only the four provider capability interfaces. It does not assert:

* disabled-topology absence of repository and readiness-validator descriptors;
* enabled-topology exact descriptor count and scoped lifetime for both types;
* exact source occurrence count for the two `TryAddScoped` statements;
* enabled-topology resolution of both types with the PostgreSQL fixture.

The source-occurrence assertion is necessary for Mutation C: duplicating a
`TryAddScoped<T>` statement is descriptor-idempotent, so a descriptor-count-only
test would remain GREEN and be vacuous. The smallest proposed count-neutral
coverage is:

1. extend existing architecture method OA2 with disabled absence, enabled
   exact-count/lifetime and exact source-occurrence assertions;
2. extend existing integration method O17 with resolution of the repository and
   readiness validator through a service provider using its landed PostgreSQL
   fixture.

Those two permanent test files are not authorized by Addendum 3. Per its
TEST AUTHORITY and STOP/RRI clauses, Mutations B/C and the remaining regression
closeout were not run. No test file was modified, no path was staged and no
commit, push, merge, PR or deployment occurred.

## Addendum 3A DI registration proof closure

Addendum 3A authorized count-neutral changes only in OA2 and O17. The method
names and DURABLE-OBJECT discovery census remained unchanged. OA2 now proves
the real production registration graph on both sides of the topology guard:

```text
Disabled or missing-capability topology:
  repository descriptors = 0
  readiness-validator descriptors = 0

Enabled durable topology:
  repository descriptors = 1, Scoped, exact implementation
  readiness-validator descriptors = 1, Scoped, exact implementation
  strict production-source registration occurrences = 1 each
  both occurrences are after the disabled-topology return
```

O17 builds the real enabled service provider with the landed PostgreSQL
fixture, creates a scope, resolves both services, and proves same-scope
identity while preserving the existing ACL and real-MinIO posture matrix.

Final proof hashes after the Addendum 3A OA2/O17 patch were:

```text
Tip88C1B2DurableObjectCustodyArchTests.cs
  491A977C082D35489BC39760C8A8CC723476BF532E644FC60E1AAFE990C86C30

Tip88C1B2DurableObjectCustodyTests.cs (OA2/O17 checkpoint)
  6F2B0E53CC619F9457E8585611B13A0600B59A5A055F2B2269F5A90FA5852C57
```

Mutation A independently moved each scoped registration before the guard.
The focused Tip83E1 host test went RED from an unresolved
`TagEkycDbContext`, naming the repository and validator respectively.
Mutation B independently removed each registration: OA2 observed an empty
descriptor set, and O17 failed exact scoped resolution for the removed type.
Mutation C independently duplicated each exact `TryAddScoped<T>()` source
statement: OA2 observed two strict source occurrences even though
`TryAddScoped` kept the effective descriptor count idempotent. All mutations
were restored to the canonical service-extension SHA-256:

```text
9EFAE47D24DBDC6841995A30350DBEB21496CBDE167ED975EE0E4A813CD0B6B1
```

The mandatory post-DI unfiltered checkpoint was executed project by project
to avoid the solution-level parallel-run timeout while retaining zero filters:

```text
TagEkyc.Contracts.Tests:     13 passed / 0 failed / 0 skipped
TagEkyc.ArchTests:          116 passed / 0 failed / 0 skipped
TagEkyc.UnitTests:          188 passed / 0 failed / 0 skipped
TagEkyc.IntegrationTests:   670 passed / 0 failed / 1 skipped
Aggregate:                  987 passed / 0 failed / 1 skipped / 988 total
```

The single skip remained the authorized manual golden-vector generator.
Tip83E1 passed, and all five TIP-68 SoftHSM tests passed under the disposable
SoftHSM2 2.5.0 environment. The Integration run completed in 19 minutes
48 seconds; earlier tool timeouts were not accepted as test evidence.

## Standing-sweep behavioralization and O03 contract STOP/RRI

Before starting the remaining mutations, three existing structural proofs
were strengthened count-neutrally inside their assigned obligations:

* O03 now performs a controlled second insert and pins SQLSTATE `23505`, the
  exact `uq_raw_export_provisional_objects_attempt` constraint, canonical-row
  survival and one-row residue;
* O14 now executes owner UPDATE/DELETE/TRUNCATE attempts against the head and
  event tables and pins the two exact guard messages plus unchanged state;
* O20 now executes the active readiness precedence chain, including combined
  failures for topology, limits, endpoint, credentials, database ACL,
  capability, bucket, Object Lock, versioning, lifecycle, public access and
  GOV/ART, while retaining the exact fourteen-code manifest.

Their canonical positive control passed 3/3. The resulting integration-test
SHA-256 is:

```text
6D0C198EA0981850B956FA0A3FD0156B5C1D525BE33A7728CE7FA9C99C5FDDE8
```

Representative OA1-OA6 scratch mutations each made their assigned method RED:
capability interface inheritance, runtime `IAmazonS3` registration, batch
delete type use, arbitrary string bucket input, credential rendering and
removal of `IfNoneMatch="*"`. All production sources were restored; the
canonical hashes are:

```text
ProvisionalObjectCustodyServiceCollectionExtensions.cs
  9EFAE47D24DBDC6841995A30350DBEB21496CBDE167ED975EE0E4A813CD0B6B1
S3CompatibleProvisionalObjectProvider.cs
  A2A0F3EC08CD1AECA4508979328CCFF4768523A8A73E15E67D61264CA4C52116
ProvisionalObjectCustodyContracts.cs
  8815604ACC33CDD309DBEB941898C39FEC51D4ECC67AE43239FC40779D5B77BE
ProvisionalObjectCustodyOptions.cs
  9F89F17BEF347C141A41B57E6457D6816242AF81E7F398FB503F79DD23440BCA
```

The O03 mutation then dropped only
`uq_raw_export_provisional_objects_attempt`, exactly as dispatch section 11.1
requires. O03 went RED, but for the wrong reason:

```text
Expected SQLSTATE: 23505
Observed SQLSTATE: 23503
Observed blocker:  fk_raw_export_provisional_objects_attempt_binding
```

This is not a weak implementation guard. The ratified mutation is
unsatisfiable against the redundant defenses: the composite FK requires an
object row for a given `AttemptId` to reuse that attempt's frozen identity,
while the independent identity and object-key unique constraints prohibit the
same frozen values from appearing in a second object row. Removing only the
attempt unique constraint therefore cannot make the second row succeed.

The migration was restored byte-identically after repairing the scratch
patch's mixed-line-ending context. Canonical SHA-256:

```text
4351B6EE40B13A0A7267265C97E2044B44F9B27C66CD5FF9241DFBFADF9BC428
```

Per Addendum 3A section 12, the standing sweep stops here because the named
mutation cannot RED for its assigned behavior without correcting the
ratified proof manifest. No contract, migration, schema or proof ID was
silently changed. No path was staged and no commit, push, merge, PR or deploy
occurred.

## O03 two-layer correction and resumed behavioral sweep

The Homeowner correction superseded only the infeasible O03 mutation wording;
the four production constraints and their definitions remained frozen. O03 was
kept count-neutral and made capable of reporting both an exact neighboring
constraint and a successful second insert.

Mutation O03-A scratch-dropped only
`uq_raw_export_provisional_objects_attempt`. The canonical assertion expected
`23505 / uq_raw_export_provisional_objects_attempt`; the observed result was
`23503 / fk_raw_export_provisional_objects_attempt_binding`, so O03 went RED for
`EXPECTED_CANONICAL_ATTEMPT_UNIQUE_REJECTION_BUT_NEIGHBORING_ATTEMPT_BINDING_FK_REJECTED`.
The migration and schema were restored before O03-B.

Mutation O03-B scratch-dropped exactly the attempt unique constraint and the
attempt-binding composite FK. The controlled row reused the `AttemptId` but used
a fresh valid object identity and object key. The insert succeeded, the observed
row count became two and O03 went RED for
`EXPECTED_ONE_OBJECT_PER_ATTEMPT_BUT_COMBINED_UNIQUE_AND_BINDING_DEFENSES_WERE_REMOVED`.
No identity or key unique constraint was removed. After restoration the catalog
contained the exact four frozen definitions, canonical O03 returned
`23505 / uq_raw_export_provisional_objects_attempt`, and the row count remained
one.

The remaining sweep was then completed. The table below records the
discriminating scratch change and the observed RED reason; every mutation was
run from restored canonical bytes and removed afterward.

| Proof | Discriminating mutation and RED observation |
| --- | --- |
| O01 | Added one intended 64-byte function name and matching DDL. `Assert.Multiple` reported both byte length 64 outside 1..63 and the exact `pg_proc` round-trip mismatch caused by PostgreSQL truncation. |
| O02 | Removed the positive `StateRevision` predicate; invalid revision was accepted instead of producing the named CHECK violation. |
| O03 | Both corrected layers above went RED independently; no neighboring production defense was weakened permanently. |
| O04 | Removed the four-column attempt-binding FK; the exact composite-FK assertion failed. |
| O05 | Applied the same preimage-order drift to both SQL recomputations; the absolute golden digest still detected the common-mode error. |
| O06 | Removed the expected-revision predicate; a stale mutation succeeded and the behavioral state/revision/event assertions went RED. |
| O07 | Removing only the function upper bound exposed the independent table CHECK (`23514`). A combined scratch removal of the function and table upper bounds then accepted 134217729 bytes, so the expected `P0001 / RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID` assertion went RED for the assigned behavior. |
| O08 | Returned `PositivelyAbsent` before the real MinIO inspection; the lost-response recovery expected `Present` and went RED. The canonical proof performs a real conditional write, inspect and readback. |
| O09 | Removed the second-observation guard; one observation was accepted and the two-observation absence proof went RED. |
| O10 | Removed `IfNoneMatch = "*"`; the real MinIO collision returned `Created` instead of `ConditionalConflict`. |
| O11 | Routed mismatch through the recovered-present state/outcome; the exact `ConditionalConflict` assertion went RED. |
| O12 | Removed the SQL null guard and both sparse/NOT-NULL backstops; verification completed with NULL evidence and failed explicitly with `EXPECTED_AUTHENTICATED_VERIFICATION_EVIDENCE_BUT_NULL_WAS_ACCEPTED`, state `VerifiedCompleted`, `evidence_is_null=True`. |
| O13 | Swapped the cleanup-quarantine source digest to the provider-receipt digest; the source-swapped evidence was incorrectly accepted and `Assert.Throws` went RED. |
| O14 | Removed the head write-guard trigger; direct owner mutation no longer produced the pinned append-only error. |
| O15 | An extra runtime function EXECUTE made the exact grantor/grantee/function manifest RED; an extra reconciler table SELECT independently made the zero non-owner table-ACL assertion RED (`expected 0, actual 1`). |
| O16 | Removed the MinIO volume during restart; the canonical durable read expected `Present` and observed `PositivelyAbsent`. |
| O17 | The precedence, posture and ACL mutations recorded above each changed the pinned readiness result and made O17 RED; canonical enabled-scope DI resolution remained behavioral. |
| O18 | Omitted the event-table drop in `Down`; apply/down/reapply detected catalog residue and went RED. |
| O19 | Added `PayloadBytes bytea` to the object head; the raw-byte-surface count changed from zero to one. |
| O20 | Swapped topology and limit checks; the combined failure returned the limits code instead of the pinned topology-first code. |
| O21 | Ignoring the configured cap allowed an over-cap begin. The original same-attempt race stayed GREEN after removing the source-head lock because the attempt unique constraint masked the lock. O21 was therefore strengthened count-neutrally: it now holds the real source-head row lock, proves begin is blocked, releases it and proves `Created`. Removing `FOR UPDATE OF x, sh` now goes RED with `EXPECTED_SOURCE_HEAD_LOCK_TO_SERIALIZE_CAPACITY_CHECK_BUT_BEGIN_COMPLETED_WHILE_LOCK_WAS_HELD`. |
| O22 | Removed each minimum operand independently. Operation timeout, ownership lease, retention expiry and reservation expiry each changed its dedicated expected deadline and made O22 RED. |
| OA1-OA6 | Capability-interface inheritance, runtime `IAmazonS3` registration, batch delete, arbitrary string bucket input, credential rendering and removal of `IfNoneMatch="*"` each made its assigned architecture proof RED, as recorded above. |

The final restored hashes for the load-bearing files were:

```text
20260804120000_Tip88C1B2DurableObjectCustody.cs
  4351B6EE40B13A0A7267265C97E2044B44F9B27C66CD5FF9241DFBFADF9BC428
ProvisionalObjectCustodyRepository.cs
  6E22CE2994D1C4A156498B6BC8A62F0298EEDED06BDC3A30BCF69EE20CDAD45A
Tip88C1B2DurableObjectCustodyTests.cs
  68A74FAF086F61907B241EF9B4D9AA03FDE98620C667CD53C13A9BEF7DB93109
Tip88C1B2DurableObjectCustodyArchTests.cs
  491A977C082D35489BC39760C8A8CC723476BF532E644FC60E1AAFE990C86C30
```

After the final restoration, the canonical positive classes passed:

```text
O01-O22 integration: 22 passed / 0 failed / 0 skipped
OA1-OA6 architecture: 6 passed / 0 failed / 0 skipped
Release build:        0 warnings / 0 errors
pending model:        No changes have been made to the model since the last migration.
```

## Final-suite STOP/RRI: non-canonical Base64URL tamper in landed B1 test

The unfiltered final checkpoint passed the three non-integration projects:

```text
TagEkyc.ContractTests: 13 passed / 0 failed / 0 skipped
TagEkyc.ArchTests:    116 passed / 0 failed / 0 skipped
TagEkyc.UnitTests:    188 passed / 0 failed / 0 skipped
```

The unfiltered Integration run completed with:

```text
669 passed / 1 failed / 1 skipped / 671 total
```

The only failure was outside the DURABLE-OBJECT test allowlist:

```text
Tip88C1B1IngressClaimTests
  .C1B1_token_validator_rejects_tamper_wrong_manifest_expiry_and_stale_cas
line 428: expected False, actual True
```

The test changes only the final Base64URL character from `A` to `B` when the
canonical token ends in `A`. For a 43-character Base64URL encoding of 32 bytes,
the final character contains padding bits; `A` and `B` can decode to the same
32-byte value when a decoder accepts a non-canonical final quantum. The
validator therefore receives unchanged token bytes and correctly validates the
same digest. A focused clean-environment rerun reproduced the same failure 0/1,
so it was not accepted as a transient full-suite flake.

The narrow count-neutral correction is to mutate a character carrying payload
bits (for example the first character), preserving the assigned B1 tamper
obligation while guaranteeing different decoded bytes. That test file is not
one of the two DURABLE-OBJECT test files authorized by Addendum 3A. No B1 byte
was changed. Per the standing STOP boundary, final closeout remains blocked
pending explicit test-only authority.

The SoftHSM2 2.5.0 portable prerequisite used the previously verified binaries:

```text
softhsm2-util.exe: EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E
softhsm2-x64.dll: 1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635
```

All task-created SoftHSM and TagEkyc PostgreSQL/MinIO resources were removed.
The unrelated SignFlow PostgreSQL, Redis and MinIO containers were preserved.
Staged paths remained zero; no commit, push, merge, PR or deployment occurred.

## B1 token-tamper micro-patch and final GREEN checkpoint

The Homeowner authorized a count-neutral, test-only correction to the existing
B1 method. The final-character Base64URL mutation was replaced by a
first-character mutation, guaranteeing that decoded token payload bits change:

```text
Tip88C1B1IngressClaimTests.cs
before: 61A538A5D9023E2A75EE85C641F4F6808AA0CE1EA08163D44B093843A5F9CC0E
after:  BEECF0C5959C737EE39F440F53C42954D23E188144686C7EEEDD9547EA5DA536
```

No production code, test method, proof ID or discovery census changed. The
focused B1 positive control passed 1/1.

The complete unfiltered Release checkpoint then passed:

```text
Release build:               0 warnings / 0 errors
TagEkyc.ContractTests:       13 passed / 0 failed / 0 skipped
TagEkyc.ArchTests:          116 passed / 0 failed / 0 skipped
TagEkyc.UnitTests:          188 passed / 0 failed / 0 skipped
TagEkyc.IntegrationTests:   670 passed / 0 failed / 1 skipped
Aggregate:                  987 passed / 0 failed / 1 skipped / 988 total
```

The single skip remained
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`, the
previously authorized manual generator. Pending-model remained clean. This
closes the final-suite STOP/RRI recorded above without altering any production
contract.

Success disposition:

```text
PASS — DURABLE-OBJECT BEHAVIORAL SWEEP COMPLETE
PRECEDENCE CORRECTION RATIFIED
PENDING WHOLE-TREE CLOSEOUT REVIEW
```

## Whole-tree remediation F1–F6

The Homeowner accepted the six whole-tree findings and authorized the exact
remediation overlay at repository HEAD
`0e301445ab12c3890e7bec72ef88ca562c4e39a8`. The permanent implementation and
proof changes remained inside the twelve-path allowlist. `ReadinessEndpoint.cs`
was not edited, and no public contract, SQL function signature, schema state,
transition, outcome token, or readiness code changed.

### Permanent file hashes

The before column is the independently reviewed whole-tree input. The after
column is the restored canonical remediation result; scratch mutations are not
included.

| Path | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| `src/TagEkyc.Api/Program.cs` | `F702A62220D8F3E93CDB67FF6E1A6BBC915A21C783E48C3B6A06E0C474183D83` | `8F92F8AEA3391AE2E608E0EF8288121F2A0E3C2AA73DD74F37DB77C8F8D05F8E` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyOptions.cs` | `9F89F17BEF347C141A41B57E6457D6816242AF81E7F398FB503F79DD23440BCA` | `A146B5225A90C7EB79591A4315A8633A10A27575B32FEBC2365076EA48DC5F4D` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyReadinessValidator.cs` | `0094851231E043EC64DE401C6ACBCB5625004ECD517E837AF17B3FF645905A42` | `9571FE016AFBABC122E25F5E140F67C89774BAFD0FE293EF3C2CCAB060CC3D79` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyServiceCollectionExtensions.cs` | `9EFAE47D24DBDC6841995A30350DBEB21496CBDE167ED975EE0E4A813CD0B6B1` | `14AF64BADB507396D1705ADB4E29653D8C2B0521A41857F00A7F0CBC2B0A46A2` |
| `src/TagEkyc.Infrastructure/RawExport/S3CompatibleProvisionalObjectProvider.cs` | `A2A0F3EC08CD1AECA4508979328CCFF4768523A8A73E15E67D61264CA4C52116` | `7F15E7453C8C33D98F2D078BEE928F727634DC852FF8FF9904C38F09343A2DFE` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260804120000_Tip88C1B2DurableObjectCustody.cs` | `4351B6EE40B13A0A7267265C97E2044B44F9B27C66CD5FF9241DFBFADF9BC428` | `2F1921022DD0AEF3EB5F0417A7BE1925F6335100712ECB36D9BC8202EC567E77` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs` | `68A74FAF086F61907B241EF9B4D9AA03FDE98620C667CD53C13A9BEF7DB93109` | `4C7F3D963892F5D458B7C8F235F4A95205734E7749B04EFCB3C7D7E40396B9D7` |
| `tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs` | `C65E2CC9513D32A06CFCE4BE5C67B65F9D42FC8F8E9580890810976AAA1D061F` | `7B28FC660570171D27073DFB798F0D53B8A829A72D994747145A18C6A201E1C6` |
| `tests/TagEkyc.IntegrationTests/Tip83E1ReadinessEndpointTests.cs` | `D479E8EDABC93590F160EDA2EC55C96AEC809CF1DC3EFDA0532F594EB9AAB9B8` | `BFAFA90A7C1FDA1DDB3DD0547D760A933953FCF1D1B1FFB7D644F33031071FC8` |
| `tests/TagEkyc.ArchTests/Tip88C1B2DurableObjectCustodyArchTests.cs` | `491A977C082D35489BC39760C8A8CC723476BF532E644FC60E1AAFE990C86C30` | `AAE59A3FA7D5EC0AF129DE79B67BFD99463D7F8D5F037347B42804E74F0576CC` |

The as-built pre-remediation SHA-256 was
`2B1E80062A4B312D12447AA1BE508A722372C7DFD8C23C82ACAD20A350D8DB74`.
The new O03 correction draft SHA-256 is
`A8AA373784EC87BB578E8D389653AB65C7B83A78DD93B03ACC51A10BFE800BE7`.

### F1/F2 — configuration and production-host graph

`ProvisionalObjectCustodyOptions.Resolve` now preserves four independent parse
dispositions: topology, fixed limits, endpoint/bucket, and
capability/credentials. The runtime matrix is:

| Input defect | Exact first code |
| --- | --- |
| topology missing, unknown, or malformed | `PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID` |
| cap or timeout missing, malformed, or non-fixed | `PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID` |
| URL, scheme, HTTP posture, or bucket invalid | `PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID` |
| capability missing/unknown or credential blank | `PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID` |

Production registration proved:

| Configuration | Registered object-custody graph |
| --- | --- |
| explicit valid `Disabled` | options only; no validator, wrapper, repository, or provider |
| invalid | options + validator + wrapper only; no repository/provider access |
| valid durable | scoped validator and repository exactly once, wrapper, and exactly one capability provider |

Non-production hosts do not register this production-only subsystem. The real
production-host test kept all real `IReadinessCheck` registrations and proved
healthy Disabled, exact invalid-topology output, and executable durable
readiness. The full suite also proved the existing non-production and signing
host-startup contracts remained intact.

Scratch mutations all turned the host proof RED and were restored to the exact
canonical SHA values above:

```text
unconditional wrapper registration:
  Assert.False failed; HasReadinessWrapper was True for Disabled
missing topology treated as Disabled:
  Assert.True failed; invalid host had no readiness wrapper
invalid-config validator omitted:
  Assert.True failed; exact invalid-config wrapper became unreachable
```

### F3 — exact database manifest

Migration postcondition and runtime readiness now share the same logical
manifest: exact two tables; exact fifteen functions including identity
arguments, owner, SECURITY DEFINER posture and `search_path`; symmetric exact
non-owner ACL rows with deployer grantor; six exact role attribute vectors;
three exact LOGIN-to-capability memberships with
`ADMIN=false/INHERIT=true/SET=false`; and zero column ACLs or unauthorized
table/function grants.

O15 passed its positive control and independently made all eight mutations
return exact `PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID`: table owner,
function owner, membership, role attribute, column ACL, runtime grant on an
entry function, capability grant on an internal function, and a missing exact
function plus same-pattern decoy. Every catalog mutation was restored.

### F4 — provider posture fail-closed

The injected `IAmazonS3` seam is internal; production DI still selects the
options constructor. Successful null/unknown Object Lock, versioning, lifecycle,
ACL, blank/malformed policy and all tested 403/5xx responses map to the exact
posture code without escaping as generic readiness failure. Readiness order
remains bucket, Object Lock, versioning, lifecycle, public access.

Discriminating scratch evidence:

```text
successful unknown Object Lock treated as absent:
  expected LOCK_PROHIBITED; observed GOV_ART_INACTIVE
null lifecycle rules treated as safe:
  expected LIFECYCLE_PROHIBITED; observed GOV_ART_INACTIVE
ACL provider exception rethrown:
  O17 failed with AmazonS3Exception POSTURE_FIXTURE_FAILURE
```

An attempted `Configuration is null` mutation remained GREEN because the AWS
SDK materializes the configuration object. It was not counted as coverage; the
replacement mutation targeted the actually observable null `Rules` member and
turned RED for the assigned reason.

### F5 — real MinIO credential separation

The task companion was resolved and used by immutable platform digest:

```text
tag: minio/mc:RELEASE.2025-08-13T08-35-41Z
linux/amd64 digest and image ID:
  sha256:eb4ea9884b77704230e2423e9004d2fa738dc272876b9cc41a297d29443b8780
manifest-list digest:
  sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727
mc version: RELEASE.2025-08-13T08-35-41Z
commit: 7394ce0dd2a80935aded936b09fa12cbb3cb8096
runtime: go1.24.6 linux/amd64
```

Five fresh credential pairs were distinct by redacted digest. Writer conditional
Put, Reconciler Head/Get, Lifecycle single-object delete, PostureProbe exact
bucket posture reads, and CleanupAdministrator list/cleanup operations passed.
Representative cross-capability Put/Get/Delete/List/posture calls were denied.
No credential value was written to source, logs, or this report. Replacing all
capability options with the Writer credential made real-provider O17 RED
(`VERSIONING_PROHIBITED` expected, `BUCKET_UNAVAILABLE` observed) and was
restored byte-identically. O10 was corrected to inspect immutable ciphertext
through the Reconciler identity, not CleanupAdministrator.

### F6 — O03 two-layer mutation evidence

Canonical O03 passed with `23505 / uq_raw_export_provisional_objects_attempt`
and one row. Dropping only that unique constraint made O03 RED with exact
`23503 / fk_raw_export_provisional_objects_attempt_binding`. Dropping exactly
the unique and composite FK made the controlled second insert succeed with row
count two. The migration was restored byte-identically to
`2F1921022DD0AEF3EB5F0417A7BE1925F6335100712ECB36D9BC8202EC567E77`.

The immutable correction draft remains byte-identical at SHA-256
`A8AA373784EC87BB578E8D389653AB65C7B83A78DD93B03ACC51A10BFE800BE7`.
Independent review returned `PASS — READY FOR HOMEOWNER RATIFICATION` from
`OpenAI GPT-5.6 Thinking — Independent O03 narrow reviewer` on 2026-08-06.
Homeowner ratification is recorded append-only in
`tip_88c1_b2_durable_object_o03_mutation_correction_ratification_v0_1.md`.
The successor binds only dispatch v0.5 §11.1 O03 mutation-proof wording and
does not change production schema, constraints, SQL functions, state model,
ACL, readiness codes, evidence codecs or application behavior. Commit authority
remains `NONE`; the future containing repository commit remains `PENDING`.

### Final validation and cleanup

```text
Focused O03/O15/O17/O20: 4 passed / 0 failed / 0 skipped
Production host matrix:  1 passed / 0 failed / 0 skipped
O01–O22:                 22 passed / 0 failed / 0 skipped
OA1–OA6:                  6 passed / 0 failed / 0 skipped
Release build:            0 warnings / 0 errors
Pending model:            No changes have been made to the model since the last migration.

Full unfiltered Release suite:
  Contract:              13 passed / 0 failed / 0 skipped
  Architecture:         116 passed / 0 failed / 0 skipped
  Unit:                 188 passed / 0 failed / 0 skipped
  Integration:          671 passed / 0 failed / 1 skipped
  Aggregate:            988 passed / 0 failed / 1 skipped / 989 total
```

The only skip remained the intentional manual golden-vector generator. The
full-suite prerequisite reused verified SoftHSM2 2.5.0 bytes
(`softhsm2-util.exe` `EFB81B0D...C6E`, module `1980A74F...635`); its package and
embedded archive hashes were rechecked before execution. The temporary package,
portable extraction, token stores, TagEkyc PostgreSQL, MinIO containers and
volumes were removed. Task container and volume residue counts were zero; the
unrelated `signflow-postgres` container remained untouched.

Current disposition:

```text
F1: CLOSED
F2: CLOSED
F3: CLOSED
F4: CLOSED
F5: CLOSED
F6 implementation/proof: CLOSED
F6 correction governance: CLOSED BY APPEND-ONLY HOMEOWNER RATIFICATION
WHOLE-TREE CLOSEOUT REVIEW: PENDING INDEPENDENT REVIEW
NO STAGE / NO COMMIT / NO PUSH / NO MERGE / NO PR / NO DEPLOY
```

## Whole-tree closeout R1 findings and remediation

Two independent R1 reports disagreed: one returned three actionable findings
and one returned PASS. The findings were adjudicated against the current source
and the pinned MinIO provider rather than accepted or rejected by vote.

### R1-01 — bucket-name validation: confirmed and closed

`ProvisionalObjectCustodyOptions.IsBucket` previously accepted uppercase ASCII,
adjacent dots and IPv4 literals. The validator now accepts only lowercase ASCII
letters, digits, hyphen and dot; requires a lowercase letter or digit at both
ends; rejects `..`; and rejects four-part decimal IPv4 literals. O20 covers
`InvalidBucket`, `example..com` and `192.168.5.4` and returns exact
`PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID` before provider or repository
registration.

Scratch mutation: restoring uppercase acceptance made O20 RED because
`InvalidBucket` reached provider/repository registration instead of failing at
endpoint validation. The canonical source was restored before final validation.

```text
ProvisionalObjectCustodyOptions.cs
before: A146B5225A90C7EB79591A4315A8633A10A27575B32FEBC2365076EA48DC5F4D
after:  5F80926CA24C4A7E87434DF608D93779FF2AB982F909DB18AEA0295C5F68DF5F
```

### R1-02 — wildcard MinIO policies: confirmed and closed

The MinIO fixture no longer grants any `arn:aws:s3:::tagekyc-*` resource.
Writer, Reconciler, Lifecycle, PostureProbe and CleanupAdministrator policies
bind the exact scenario bucket; object capabilities bind the exact
`raw-export/c1/v1/*` prefix. Every additional scenario bucket receives a fresh
exact-bucket PostureProbe identity and an exact cleanup policy. O17 proves the
positive operation for each intended capability, same-bucket cross-prefix
denial, cross-bucket denial for the primary identities, and cleanup under the
dedicated cleanup administrator.

Scratch mutation: restoring the wildcard bucket resource made O17 RED because
the primary Writer successfully wrote to an alternate bucket when an
`AmazonS3Exception` denial was required. The canonical exact-bucket policies
were restored before final validation.

```text
DurableObjectMinioFixture.cs
before: 7B28FC660570171D27073DFB798F0D53B8A829A72D994747145A18C6A201E1C6
after:  55D690942B8552AD0534BF4EF40BA047432ABCCA897AA50ABF8DDCAD205D3EF5

Tip88C1B2DurableObjectCustodyTests.cs
before: 4C7F3D963892F5D458B7C8F235F4A95205734E7749B04EFCB3C7D7E40396B9D7
after:  2851AB00A96F00C2002DCE80B9FB68605F63EA2533C317CA3D40E844CE6CB0D1
```

### R1-03 — independent `s3:GetBucketAcl`: provider limitation, not applied

The standard AWS action is relevant to an AWS S3 policy, but the pinned MinIO
policy engine rejects it. Attempting to create the exact policy returned:

```text
mc: <ERROR> Unable to create new policy: unsupported action 's3:GetBucketAcl'.
```

The pinned MinIO provider successfully serves `GetACLAsync` through its
supported bucket-policy posture capability. The exact supported PostureProbe
manifest remains:

```text
s3:GetBucketVersioning
s3:GetBucketObjectLockConfiguration
s3:GetLifecycleConfiguration
s3:GetBucketPolicy
```

O17 additionally proves that the Writer identity cannot call `GetACLAsync`.
This closes identity separation for the pinned MinIO target without claiming
that MinIO exposes an independently grantable `s3:GetBucketAcl` action. A future
AWS S3 provider must define and prove its own provider-specific action manifest;
this slice does not claim portability of the MinIO IAM vocabulary to AWS.

### R1 final validation

The first full-suite attempt failed in the disposable prerequisite topology,
before exercising the remediation: the recreated database lacked inherited
`tagekyc_extensions` privileges for `tagekyc_raw_export_deployer`. The corrected
bootstrap provisioned the landed deployer and three deployment LOGIN roles,
then created `pgcrypto` in `tagekyc_extensions` in `template1`, revoked PUBLIC,
and granted the deployer exact schema USAGE and function EXECUTE. Targeted
DK-PROD and fixture-proof controls then passed 2/2, proving that the rerun used
the intended prerequisite topology rather than masking readiness.

Final evidence:

```text
O17 canonical:                 1 passed / 0 failed / 0 skipped
O20 canonical:                 1 passed / 0 failed / 0 skipped
O20 uppercase mutation:        RED for endpoint-validation bypass
O17 wildcard-bucket mutation:  RED for successful cross-bucket Writer Put
Pending model:                 No changes have been made to the model since the last migration.
Release build:                 0 warnings / 0 errors

Full unfiltered Release suite:
  Contract:                    13 passed / 0 failed / 0 skipped
  Architecture:               116 passed / 0 failed / 0 skipped
  Unit:                        188 passed / 0 failed / 0 skipped
  Integration:                671 passed / 0 failed / 1 skipped
  Aggregate:                  988 passed / 0 failed / 1 skipped / 989 total
```

The only skip remained
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
No production migration, schema, state model, SQL function, ACL, readiness code
or evidence codec changed for R1. No file was staged, committed or pushed.

Current disposition:

```text
R1-01: CLOSED BY BEHAVIORAL VALIDATION AND MUTATION RED
R1-02: CLOSED BY EXACT-BUCKET POLICIES AND MUTATION RED
R1-03: NOT APPLICABLE TO PINNED MINIO ACTION VOCABULARY; PROVIDER LIMITATION RECORDED
WHOLE-TREE CLOSEOUT R2: PENDING INDEPENDENT REVIEW
NO STAGE / NO COMMIT / NO PUSH / NO MERGE / NO PR / NO DEPLOY
```

## Whole-tree closeout R2 findings and remediation

R2 again produced conflicting verdicts. The FAIL reviewer reported three
specific gaps while the PASS reviewer verified only the R1 delta. All three R2
findings were confirmed against dispatch v0.5 and the current executable paths.

### R2-01 — exact bucket and origin configuration

`BucketName` is no longer trimmed before validation. A whitespace-padded value
therefore remains a different, invalid configured value and returns exact
`PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID`. `ServiceUrl` now requires an empty
or root `/` absolute path in addition to the existing scheme, loopback,
userinfo, query and fragment rules. A path-bearing HTTPS URI is rejected before
provider or repository registration.

O20 exercises both cases through the real
`IConfiguration -> Resolve -> production registration -> readiness` path.

Scratch evidence:

```text
restore BucketName.Trim():
  O20 RED because padded BucketName registered repository/provider services
  instead of failing at ENDPOINT_INVALID.

remove the root-path predicate:
  O20 RED because https://object-store.example/base-path registered
  repository/provider services instead of failing at ENDPOINT_INVALID.
```

### R2-02 — bidirectional exact role-membership graph

The runtime readiness query and migration postcondition now collect every
`pg_auth_members` edge for which either the member or granted role belongs to
the protected six-role set. Bidirectional `EXCEPT` comparison still permits
only the three exact LOGIN-to-capability edges with
`ADMIN=false/INHERIT=true/SET=false`. An outgoing grant from either a
capability role or a deployment LOGIN is therefore visible and rejected.

O15 adds two count-neutral mutations using fresh unrelated roles:

```text
unrelated role -> tagekyc_raw_export_custody_encryptor
unrelated role -> tagekyc_raw_export_encryptor_login
```

Both return exact `PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID` and restore the
temporary edge and role. Narrowing runtime collection back to incoming edges
only made O15 RED: expected `DATABASE_ACL_INVALID`, observed
`GOV_ART_INACTIVE`.

### R2-03 — exact typed posture absence

Object Lock, lifecycle and bucket-policy absence are now accepted only for
HTTP 404 carrying the exact provider error code:

```text
ObjectLockConfigurationNotFoundError
NoSuchLifecycleConfiguration
NoSuchBucketPolicy
```

A generic or differently typed 404 at any later posture probe returns
`BucketAvailable=false`, preserving first-code precedence as exact
`PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE`. Other failures continue to fail
the exact posture condition. O17 contains an all-typed-absence positive control
and three generic-404 negative controls.

Scratch mutation changed all three exact predicates back to status-only 404.
O17 went RED: expected `BUCKET_UNAVAILABLE`, observed `GOV_ART_INACTIVE`.
The pinned MinIO positive control passed with the exact three provider codes.

### R2 file hashes

| Path | R2 input SHA-256 | Remediated SHA-256 |
| --- | --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyOptions.cs` | `5F80926CA24C4A7E87434DF608D93779FF2AB982F909DB18AEA0295C5F68DF5F` | `C4D0E73F290A4049E362F04AB39583B9BB0558516ADA168020754041000198C3` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyReadinessValidator.cs` | `9571FE016AFBABC122E25F5E140F67C89774BAFD0FE293EF3C2CCAB060CC3D79` | `9E43F7A6DF82A4B2FEC87CB4EC46EA30F0F5E4C12A315FBDDAD8449102ADB444` |
| `src/TagEkyc.Infrastructure/RawExport/S3CompatibleProvisionalObjectProvider.cs` | `7F15E7453C8C33D98F2D078BEE928F727634DC852FF8FF9904C38F09343A2DFE` | `96137B71D3B13BE99BCAB32BF0EBDDA4574EC0553352D38F0823A3FC7A36BBEB` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260804120000_Tip88C1B2DurableObjectCustody.cs` | `2F1921022DD0AEF3EB5F0417A7BE1925F6335100712ECB36D9BC8202EC567E77` | `24EB721704EC78C1B925A2E761B2ADF1FF157A3BE1BED957094C30582077AD99` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs` | `2851AB00A96F00C2002DCE80B9FB68605F63EA2533C317CA3D40E844CE6CB0D1` | `FCD4F977127B39AE05C07BFF587C71C787A1F83F95634F11C9FC1EF944DAF17F` |

### R2 final validation and cleanup

```text
O15/O17/O20 canonical:       3 passed / 0 failed / 0 skipped
Bucket-padding mutation:     O20 RED for registration instead of endpoint rejection
Origin-path mutation:        O20 RED for registration instead of endpoint rejection
Membership-direction MUT:    O15 RED; GOV_ART_INACTIVE observed instead of ACL_INVALID
Generic-404 mutation:        O17 RED; GOV_ART_INACTIVE observed instead of BUCKET_UNAVAILABLE
Pending model:               No changes have been made to the model since the last migration.
Release build:               0 warnings / 0 errors

Full unfiltered Release suite:
  Contract:                  13 passed / 0 failed / 0 skipped
  Architecture:             116 passed / 0 failed / 0 skipped
  Unit:                      188 passed / 0 failed / 0 skipped
  Integration:              671 passed / 0 failed / 1 skipped
  Aggregate:                988 passed / 0 failed / 1 skipped / 989 total
```

The only skip remained
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
SoftHSM2 2.5.0 package, embedded archive, utility and module matched the pinned
SHA-256 values before execution. Task-created SoftHSM files, TagEkyc containers
and TagEkyc volumes were removed; unrelated SignFlow resources were untouched.

Current disposition:

```text
R2-01: CLOSED BY TWO DISCRIMINATING O20 MUTATIONS
R2-02: CLOSED BY BIDIRECTIONAL MANIFEST AND O15 MUTATION RED
R2-03: CLOSED BY EXACT TYPED ABSENCE AND O17 MUTATION RED
WHOLE-TREE CLOSEOUT R3: PENDING INDEPENDENT REVIEW
NO STAGE / NO COMMIT / NO PUSH / NO MERGE / NO PR / NO DEPLOY
```

## Whole-tree closeout R3 finding and remediation

R3 reported one actionable evidence-binding gap. The finding was confirmed:
the dispatch pinned the MinIO server image by digest, but the executable test
fixture passed only the corresponding release tag to `docker run`. The
companion `mc` image was already digest-pinned.

`DurableObjectMinioFixture.Image` is now the exact executable reference:

```text
minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e
```

The existing O16 proof asserts this exact value before starting the provider.
This is count-neutral and binds the same constant that `StartContainerAsync`
passes to `docker run`.

### R3 mutation proof

A scratch mutation changed only the executable image reference back to:

```text
minio/minio:RELEASE.2025-09-07T16-13-09Z
```

O16 went RED at the exact assertion:

```text
Expected: minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e
Actual:   minio/minio:RELEASE.2025-09-07T16-13-09Z
```

After restoration, both changed test files matched their post-correction
SHA-256 values and canonical O16 passed.

| Path | R3 input SHA-256 | Remediated SHA-256 |
| --- | --- | --- |
| `tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs` | `55D690942B8552AD0534BF4EF40BA047432ABCCA897AA50ABF8DDCAD205D3EF5` | `DC7285109F069177F533DD52DC53343DC2CA595216D781478F549FDD1552CFC9` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2DurableObjectCustodyTests.cs` | `FCD4F977127B39AE05C07BFF587C71C787A1F83F95634F11C9FC1EF944DAF17F` | `F3668CC427E034EE53BF535674CFF2B025FD645DCC97B3D868B60AC26A75DA50` |

Docker inspection after the canonical positive control returned:

```text
Id=sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e
RepoDigests=["minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e"]
```

### R3 validation and environment provenance

The first full-suite attempt was environment-invalid rather than a product
regression: the disposable PostgreSQL template had `pgcrypto` installed in
`tagekyc_extensions` with PUBLIC revoked, but the bootstrap omitted the exact
deployer USAGE/EXECUTE grants. The resulting 78 integration failures shared
`permission denied for schema tagekyc_extensions` or the consequent
`PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE`. No code was changed in response.

The topology was corrected outside the repository by granting only:

```text
USAGE on schema tagekyc_extensions to tagekyc_raw_export_deployer
EXECUTE on tagekyc_extensions.gen_random_bytes(integer) to tagekyc_raw_export_deployer
```

The fixture-readiness and DKPROD prepare targeted controls then passed 2/2.
The complete unfiltered suite was rerun from that verified topology and is the
canonical R3 validation result:

```text
O16 canonical after restore:    1 passed / 0 failed / 0 skipped
Tag-only mutation:              O16 RED at exact executable-image assertion
Targeted topology controls:     2 passed / 0 failed / 0 skipped
Pending model:                  No changes have been made to the model since the last migration.
Release build:                  0 warnings / 0 errors

Full unfiltered Release suite rerun:
  Contract:                     13 passed / 0 failed / 0 skipped
  Architecture:                116 passed / 0 failed / 0 skipped
  Unit:                         188 passed / 0 failed / 0 skipped
  Integration:                 671 passed / 0 failed / 1 skipped
  Aggregate:                   988 passed / 0 failed / 1 skipped / 989 total
```

The only skip remained
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
SoftHSM2 2.5.0 package, embedded archive, utility and module again matched the
pinned SHA-256 values. No production code, migration, schema, state model, ACL,
readiness code or evidence codec changed for R3-01.

Current disposition:

```text
R3-01: CLOSED BY EXECUTABLE DIGEST PIN, O16 MUTATION RED AND DOCKER IMAGE INSPECTION
WHOLE-TREE CLOSEOUT R4: PENDING INDEPENDENT REVIEW
NO STAGE / NO COMMIT / NO PUSH / NO MERGE / NO PR / NO DEPLOY
```
