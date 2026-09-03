# TIP-88C1-C3 Authenticated Package Delivery — As Built

Status: `IMPLEMENTATION_CLOSED_INDEPENDENT_CLOSEOUT_REVIEW_PENDING`

Authoritative dispatch: v0.2  
Dispatch SHA-256: `AB415A0526D65307E0036A450374928BA81BEF53C4E8BACAA7D837997BB86988`  
Baseline: `7dd15cca6b2159d08220167fe5f0b4cf95fca195`

## Allowlist amendment 38 -> 40

Homeowner authorized two additional count-neutral snapshot-tripwire paths:

- `tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs`

Together with the already-allowlisted E3 tripwire, all three pins use the raw
checked-out ModelSnapshot byte hash:

`467D7B65128C5BDCE18B2417E19AFD4C74E21C2D468087E52511695604D71A9E`

Only the three pinned values changed. Targeted C201 passed. This third absolute
site is recorded under `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01`; all three pins
remain dependent on raw worktree bytes rather than the LF-normalized Git blob.

## C308 proof self-correction

Mandatory mutation C3M11 initially remained GREEN after the real package/key
lock order was inverted. C308 had compared the first textual table-name
occurrences over the entire function. Those occurrences were `%ROWTYPE`
declarations, so the proof measured declaration order rather than the extracted
`SELECT ... FOR UPDATE` statements.

The mutation-mandated HARD STOP worked as designed. Under narrow Homeowner
authority, the existing count-neutral C308 method was strengthened to extract
only semicolon-delimited statements containing `FOR UPDATE`, require exactly
three, and require relation order key -> package -> delivery. Its existing
single-clock, no-transaction-timestamp and last-lock-before-clock assertions
were preserved.

Evidence on canonical/restored bytes:

- canonical C308: PASS;
- C3M11 package-before-key: RED at the new lock-order assertion with
  `Assert.True() Failure`, `Expected: True`, `Actual: False`;
- scratch fourth `FOR UPDATE`: RED at the count assertion with
  `Expected: 3`, `Actual: 4`;
- C3M10 `transaction_timestamp()`: RED with `Expected: 1`, `Actual: 0`;
- canonical restore after every scratch mutation: PASS;
- restored migration SHA-256:
  `3AB1F7F838ABF9D914B7149C73E3A5CD8F15C95FFD4B2A2A1CB427D244EF250F`.

No production, schema, migration, SQL, role, state, codec, vector, proof-ID or
mutation-ID change was retained by this correction.

Both independent reviewers accepted bundle
`A9D37871BF7624EF5375D709DF9713828DF7FB4BC136006A4BF5EE9058FB9C96`
as `PASS — C308 PROOF CORRECTION CLOSED; CONTROLLED C3 BUILD MAY RESUME`.

## C3M24 proof STOP/RRI

C3M24 removed the coordinator's final length and ciphertext-digest rejection,
allowing an early EOF to reach the envelope parser. C316 nevertheless remained
GREEN. Its current truncated fixture is the minimal package bytes
`TIP-88C1-C2-PACKAGE-V1 + uint32(2) + {}` with no ciphertext body. Removing the
last byte therefore truncates the two-byte envelope JSON itself. The neighboring
`TryReadEnvelopeDigest` check still returns `EnvelopeMalformed`, and C316 only
asserts the generic public `IntegrityUnavailable` result. It does not
discriminate the early-EOF length boundary.

Evidence:

- canonical coordinator SHA-256:
  `5CE1F904321D20BE7FE0D1C21C8B8936851B8708764961A8AE209901868E74DB`;
- C3M24 mutated SHA-256:
  `57945EF56F12E9F5F0E82BF9CDD2C8AFE7309621BD3F81EBA2E47CEA51D54703`;
- C316 under C3M24: GREEN (`1 passed / 0 failed`), which is prohibited;
- restored coordinator SHA-256:
  `5CE1F904321D20BE7FE0D1C21C8B8936851B8708764961A8AE209901868E74DB`;
- canonical C316 after rebuild/restore: PASS.

The controlled mutation sweep stopped immediately under Dispatch §15. No
correction or further mutation was attempted without Homeowner authority.

### C3M24 correction closure

Under narrow Homeowner authority, C316 now builds a valid complete envelope
followed by a 64-byte body and removes exactly the final body byte. Canonical
execution therefore reaches the final length/ciphertext comparator and persists
the exact `IntegrityFailureKind = CiphertextMismatch`.

The same count-neutral test file now asserts exact persisted kinds at all four
previous generic-only sites:

- C316 early EOF: `CiphertextMismatch`;
- C317 non-envelope ciphertext tamper: `CiphertextMismatch`;
- C317 persisted envelope mismatch: `EnvelopeMismatch`;
- C318 positive object absence: `ObjectAbsent`.

This closes the class finding that the durable `IntegrityFailureKind` column was
required by the sparse state but never asserted by a proof.

Correction evidence:

- corrected canonical C316: PASS;
- C3M24 final validation removed: RED with `Assert.False() Failure`,
  `Expected: False`, `Actual: True`;
- scratch kind changed to `EnvelopeMalformed`: RED with
  `Expected: CiphertextMismatch`, `Actual: EnvelopeMalformed`;
- C3M25 ciphertext comparator removed: RED at its tampered-content subcase;
- C3M26 envelope comparator removed: RED at its persisted-envelope subcase;
- final C316/C317/C318 positive controls: 3/3 PASS;
- prior-owner Unit controls: 3/3 PASS;
- prior-owner Integration controls: 8/8 PASS;
- coordinator restored SHA-256:
  `5CE1F904321D20BE7FE0D1C21C8B8936851B8708764961A8AE209901868E74DB`.

Only C316, its three sibling exact-kind assertions and private test helpers
changed permanently. No production byte changed. The mutation-mandated HARD
STOP again worked as designed.

## C3M27-C3M33 continuation and C3M34 STOP/RRI

After the independent C316 correction PASS, the controlled mutation sweep
resumed. The following mutations RED for their assigned reason and each owner
passed again after byte-exact restoration:

- C3M27 started the HTTP response before content preparation: C319 RED because
  the failure path could no longer set its 503 status after response start;
- C3M28 mapped `IntegrityUnavailable` to retryable `Unavailable`: C318 RED on
  the exact public error-code assertion;
- C3M29 reused a process-local PackageId outcome cache across DeliveryIds:
  C318 RED because Delivery B did not perform its required fresh provider read;
- C3M30 rejected completion at the 30-minute authorization boundary while the
  35-minute stream lease remained live: C320 RED with expected `Completed`,
  actual `OutcomeUnknown`;
- C3M31 bound the receipt to POST creator identity instead of the stream actor:
  C321 RED at the SQL/C# receipt digest equality;
- C3M32 changed the receipt codec domain: C321 RED independently at the same
  SQL/C# equivalence gate;
- C3M33 cached exact-object bytes in process memory: C317 RED after the object
  was changed because the second read returned cached canonical bytes.

C3M34 then removed the `p.prosecdef` requirement from C3 delivery readiness
function validation. C324 remained GREEN (`1 passed / 0 failed`). Its catalog
query counts nine wildcard-matched function names and eight grants but does not
execute valid durable readiness or assert the exact function posture. It
therefore does not detect widening away from `SECURITY DEFINER`.

The mutated result is prohibited. The readiness validator was restored to
SHA-256
`66754B9F6F0CCDCCC6BCEB91FEC6E8D73052812C7DB733E56A71AA57BADC343E`,
and canonical C324 passed after restoration. The controlled build stops here;
no closeout validation, stage, commit or push is authorized or claimed.

### C324 correction attempt exposed an executable catalog contradiction

The authorized test-only C324 correction invoked exact name/signature census
before durable readiness. On a successfully provisioned canonical database the
catalog returned this actual function name:

`raw_export_record_recipient_package_delivery_integrity_unavaila`

PostgreSQL truncates identifiers to 63 bytes. The migration declares and calls
the longer token
`raw_export_record_recipient_package_delivery_integrity_unavailable`; SQL
identifier resolution tolerates the truncation, but the readiness validator's
`expected_functions` text comparison requires the untruncated string. Exact
catalog census therefore fails before durable readiness can be certified, and
the real validator's exact join is deterministically unsatisfiable for that
function name.

The proof-only authority prohibits changing the migration, validator or
production call sites, so the correction cannot truthfully reach its required
canonical C324 PASS. The proof candidate was removed and the C3 test restored
byte-identically to
`13B682B61F1D3F46DE5A5CB5DBEDB6F9D4A209DE7855134688603F2B196624FD`.
An additional attempted diagnostic was invalid because Docker Desktop entered
a read-only-filesystem failure during fixture startup; it is not used as
evidence for this conclusion.

This is a genuine executable contract contradiction requiring separate
Homeowner adjudication of the canonical function identifier and coordinated
migration/validator/repository/test bytes. No such correction was made here.

### Canonical function rename applied; C324 exposed a second readiness defect

Under the subsequent narrow Homeowner authority, the single over-length
function was renamed in lockstep from the 66-byte identifier
`raw_export_record_recipient_package_delivery_integrity_unavailable` to the
57-byte canonical identifier
`raw_export_record_recipient_package_integrity_unavailable`.

The permanent rename is confined to the exact seven authorized production
sites: migration CREATE / ALTER OWNER / REVOKE / GRANT / DROP, readiness
manifest and repository caller. The old identifier now occurs zero times in
`src` and `tests`; the new identifier occurs exactly seven times in production.
Function signature, body, SECURITY DEFINER posture, search path, ACL, owner,
roles, state, codec and vector semantics are unchanged. Current post-rename
SHA-256 values are:

- migration: `75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`;
- readiness validator: `B50538F1BA0FD19FEA561E598F5FAB6B64633C71BA994C667A61F141222C3DAD`;
- repository: `3652ABC50AA4C088290E30FA29C487FCAD281A4362272ECCDB3984B453999FB9`.

The count-neutral C324 candidate then reached the real durable readiness
validator. Before provider access, canonical execution failed with:

`System.ArgumentException: RAW_EXPORT_RECIPIENT_PACKAGE_OBJECT_KEY_INVALID
(Parameter 'objectKey')`

The validator constructs
`raw-export/c2-package/v1/readiness-missing-{Guid:N}`, while the landed C2
codec admits the immutable object-key form
`raw-export/c2-package/v1/{Guid:N}`. Consequently its own readiness probe cannot
derive the required object binding and valid durable readiness is currently
unreachable. This defect is independent of the function rename and is not an
environment failure.

The narrowest executable correction is to construct the missing-object probe
key through `RecipientPackageCodec.ObjectKey(Guid.NewGuid())`. That one-line
behavioral edit is outside the present rename/proof-only authority, so it was
not made. The incomplete C324 proof candidate and standing-length-guard
candidate were removed; the C3 integration proof file is restored to SHA-256
`13B682B61F1D3F46DE5A5CB5DBEDB6F9D4A209DE7855134688603F2B196624FD`.

The failed positive-control TRX is retained as diagnostic evidence at SHA-256
`899AB2F53A4AA0BEDDF44C163EA6706AAB7053E1B00EF8506FBC9371A91E3A64`.
No C3M34 posture mutation, catalog round-trip claim or closeout claim is made.
The earlier Docker Desktop read-only-filesystem diagnostic remains excluded.

### Readiness object-key correction and C3M34 closure

Homeowner authorized the single executable correction in
`RecipientPackageDeliveryReadinessValidator`: the hand-built
`raw-export/c2-package/v1/readiness-missing-{Guid:N}` probe was replaced by
`RecipientPackageCodec.ObjectKey(Guid.NewGuid())`. The random identifier still
selects a deliberately absent object, while the key now satisfies the exact C2
object-binding shape. A full construction-site sweep found no second hand-built
production producer for this prefix.

The strengthened count-neutral C324 proof now:

- invokes the real C3 readiness validator with a valid durable topology,
  correctly provisioned database, C2 predecessor readiness and a real
  delivery-reader absence probe;
- compares the exact nine function names and signatures returned by
  `pg_proc`, replacing both former wildcard census sites (C309 and C324);
- enforces a 63-UTF-8-byte maximum for every role, table and function
  identifier declared by the readiness manifest;
- changes, one at a time, SECURITY DEFINER posture, owner, search path,
  grant-option ACL and overload surface, requires exact
  `PROD_RAW_EXPORT_PACKAGE_DELIVERY_CATALOG_INVALID`, restores the catalog in
  `finally`, and requires canonical readiness again after every restore.

Canonical C324 passed after the object-key correction and the final C309/C324
restore control passed `2/2`. Catalog round-trip therefore proves all nine
source manifest names/signatures equal their actual `pg_proc` identities with
no truncation gap.

Independent comparator mutations all RED at their assigned negative subcase:

- remove `p.prosecdef`: `Assert.Throws() Failure: No exception was thrown`;
- remove exact owner: the same failure at the owner subcase;
- remove exact `search_path=pg_catalog`: the same failure at the search-path
  subcase;
- remove `is_grantable`: the same failure at the grant-option subcase;
- widen surface count from `=9` to `>=9`: the same failure at the extra-overload
  subcase.

A deliberate 64-byte manifest identifier RED before database/provider access
with `PostgreSQL identifier exceeds 63 UTF-8 bytes`; the canonical identifier
was then restored. One earlier SECURITY DEFINER run made before the negative
posture partition was present stayed GREEN and is explicitly excluded from
closure evidence.

Final canonical SHA-256 values:

- readiness validator:
  `C873C283CA9AFCD3BE7C3310A19931EA2E2CF9A4AC5AAF7650F7F3F037BA7A03`;
- repository:
  `3652ABC50AA4C088290E30FA29C487FCAD281A4362272ECCDB3984B453999FB9`;
- migration:
  `75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`;
- C3 integration proof file:
  `7AE80441EF8D7F927910F27093407B48D5A43518095E9B8312136FBC087A0516`.

Affected final controls passed Integration `22/22`, Unit `3/3` and Architecture
`1/1`, totaling the unchanged C301-C326 census `26/26`. Release build completed
with zero warnings and zero errors. Previously closed C3M01-C3M33 mutation
artifacts and owner surfaces were not weakened; the permanent proof delta is
confined to C309/C324 and their private helpers. Census remains
40 paths / 9 SQL functions / 7 states / 10 transitions / 26 proofs /
34 mutations.

The optional prefix-producer architecture clause is carried as an observation:
a robust count-neutral semantic assertion was not available without adding a
source-text construction scan. The executable one-line correction and runtime
length/catalog guards are retained; no new proof ID was created.

This disposition resumes controlled build closeout only. It does not authorize
stage, commit, push, merge, PR, deployment or production activation.

## Mutation provenance reconstruction and C3M07 closure

Final closeout review requires durable evidence for every cell, not only a
summary claim. A filesystem and surviving-history sweep found no retained TRX
provenance for C3M01-C3M09, C3M12-C3M13, C3M16 or C3M17. Those cells were
therefore reconstructed from canonical positive controls rather than inherited
from the earlier `previously closed` statement.

C3M01-C3M06 each RED for its assigned semantic discriminator. The first
reconstructed C3M07 run exposed another proof false-green: C307 changed the key
to `Revoked` and also incremented its revision. Removing only the production
`k."State"<>'Active'` comparator remained GREEN because the independent frozen
revision comparator still rejected the row. The migration was restored to
`75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`
and the canonical C307 owner passed before any correction.

Under narrow Homeowner authority, only the existing C307 state fixture was
changed count-neutrally. It now changes `State` to `Revoked` and sets the
state-required `RevokedAtUtc`, while revision, fingerprint, identity, version
and validity remain matching. C307 remained PASS on canonical production
bytes. Its proof-file SHA-256 changed from
`7AE80441EF8D7F927910F27093407B48D5A43518095E9B8312136FBC087A0516`
to `AED07D3CDEB1C71DBAA493C4DCF1BA50366D2089747D3DE9B6AD8A55926EF89E`.

The corrected independent partition is:

- C3M07 remove only key-state comparator: RED, expected `Ineligible`, actual
  `Created` at the first state scenario;
- C3M08 remove only fingerprint comparator: RED at the fingerprint scenario;
- C3M08 remove only revision comparator: RED at the revision scenario;
- C3M09 remove only `ValidFromUtc` comparator: RED at the future-start scenario;
- C3M09 remove only `ValidUntilUtc` comparator: RED at the expired-key scenario.

C3M12 weakened only the non-null attempt/fence event shape and RED because the
invalid event insert no longer threw. C3M13 removed only the append-only trigger
and RED because the event update no longer threw. C3M16 removed only the
completion revision comparator and RED with expected `StateConflict`, actual
`Completed`. C3M17 bypassed only the post-lease completion classification and
RED with expected `OutcomeUnknown`, actual `Completed`.

Every scratch production mutation was restored byte-identically to migration
SHA-256 `75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`;
canonical C307, C309, C311 and C320 restore controls passed. Durable artifact
census now contains at least one named artifact for each C3M01-C3M34 cell,
with no missing ID. Census remains 40/9/7/10/26/34.

The final contract binding is the byte-authoritative dispatch v0.2 plus the
Homeowner canonical-function-name amendment. Dispatch v0.2 itself retains the
superseded 66-byte function spelling and is not represented as a byte-exact
final function-name manifest. The canonical implemented 57-byte identifier is
`raw_export_record_recipient_package_integrity_unavailable`.

Both independent C324 correction reviews are recorded as PASS. Their PASS
closes the narrow readiness/proof correction only; the final complete
unfiltered Release suite remains pending on the frozen closeout bytes.

## Final full-suite STOP/RRI

After the reconstructed 34/34 mutation census closed, final affected controls
passed Integration `22/22`, Unit `3/3` and Architecture `1/1`. Release build
then passed with zero warnings and zero errors. The single final unfiltered run
was started on those bytes and produced non-canonical failures, so it was
terminated under the dispatch HARD STOP without retry or correction.

Completed project results before termination:

- Contract: `13 passed / 0 failed`;
- Architecture: `133 passed / 2 failed`;
- Unit: `185 passed / 10 failed`;
- Integration: multiple host-startup failures observed; the project was
  interrupted under HARD STOP and has no valid final census.

The two Architecture failures are predecessor C2 proofs C225 and C226. They
reject the newly landed public C3 delivery contracts because the C2 proof still
asserts that no public `RecipientPackageDelivery*` surface exists. The owning
test file is a reviewed unchanged C2 dependency and is outside the amended
40-path C3 allowlist.

The Unit failures share two concrete causes. The C3 scope was added to the
existing `BusinessScopes` profile in `LocalDevApiKeyStore`, changing the exact
scope set of existing keys and causing predecessor API authorization flows to
return `Forbidden`. Separately, the predecessor profile census still expects
the original exact business-key scope set.

Integration host-startup failures share a C3 composition defect. Program
registers `RecipientPackageDeliveryHostedService` unconditionally, while the C3
extension registers `RecipientPackageDeliveryReconciler` only for valid durable
topology. The extension also registers the application service for
Disabled/Invalid topology without an `IRecipientPackageDeliveryGateway`.
Consequently predecessor test hosts fail DI validation before reaching their
own expected readiness/signing assertions. This is deterministic code behavior,
not SoftHSM, Docker or PostgreSQL environment failure.

The correction can remain bounded to existing C3 production/composition paths,
except that predecessor proof ownership requires adding exactly
`tests/TagEkyc.ArchTests/Tip88C1C2RecipientPackageArchTests.cs` as path 41.
No full-suite rerun, closeout bundle, stage, commit or push is authorized or
claimed from this failed run.

## RRI07 bounded composition correction

Homeowner authorized the exact allowlist amendment 40 -> 41 by adding only
`tests/TagEkyc.ArchTests/Tip88C1C2RecipientPackageArchTests.cs`. The C2 frozen
36-path census remains unchanged; the later C3 slice edits only the two C2
public-surface assertions that had classified every `*Delivery*` contract as
C2-owned.

The four deterministic causes were corrected as follows:

- Program now registers `RecipientPackageDeliveryHostedService` only when C3
  resolves to a syntactically valid `S3CompatibleDurable` topology;
- the service extension installs a dependency-free fail-closed gateway for
  Disabled/Invalid and registers the application service only after either
  that gateway or the durable coordinator gateway exists;
- the pre-C3 `BusinessScopes` set is restored exactly, and a separate active
  local-dev identity `localdev-recipient-package-delivery-key` carries only
  `business.raw-export.package.download` with a stable non-empty PrincipalId;
- C225/C226 exempt exactly `RecipientPackageDeliveryDto` and
  `RecipientPackageDeliveryErrorCodes`, not a name pattern or blanket C3/C2
  namespace exemption.

Permanent correction hashes (before -> after):

- Program: `71D70C9611333D92C68390D46F53AB5734887BA0642F078C1DD606ED68208B9D`
  -> `DC9D2426FFA4D7C856385414C26003268290C524173E603ECC6F5D17978B274B`;
- LocalDev API-key store:
  `8C2B4C925B0C65F36E79DEDEE751BC3903EBCAB62F361BFA21012D7B1CF3FCA7`
  -> `A40CCD01D692BCEA871DA602B424526EBB63B2A9B63282B838D4A90921E4B3EF`;
- delivery service registration:
  `0E6D5A7D1FD2F1F090B7861D951C53DCAA1BC7C8C38032995AD58822C6266CA6`
  -> `96851F238AC984CA89FE3324736B1CF96A516F28434D1D1DB79EFCEA474C9066`;
- C2 architecture owner:
  `E73B7DF5098A5EC30C59D01605B9E0FAD98C2128D88B0B366485683AD2042D04`
  -> `B47A5AE5C8488EB55832AE9848F1E22CDF0B9E0A8DAD5E072D793C9C6F9CE85C`;
- C3 application proof:
  `29F823814CCBB1F2E4394663A68E81BE8F0F44A3397BEBF4D04B4C00B859B167`
  -> `C54326D225C9F57A7BD2DA305A7C3DCC58D99DDB678243E5C8F9DA8053B0C0F3`;
- C3 integration proof:
  `AED07D3CDEB1C71DBAA493C4DCF1BA50366D2089747D3DE9B6AD8A55926EF89E`
  -> `5CC223880824C1C2C708B95352277C30FD88B67DB808C1610B7703C9B0A1B405`.

Behavioral evidence:

- Disabled and Invalid hosts start, resolve the topology-safe application
  graph, return exact HTTP 503 for a valid C3 request, and contain no C3 hosted
  reconciler;
- a valid durable host starts and contains exactly the C3 reconciler hosted
  service;
- existing Tip05/Tip06 API flows plus the exact local-dev profile assertion:
  `11/11 PASS`;
- C3 affected controls: Integration `22/22`, Unit `3/3`, Architecture `1/1`;
- C225/C226 canonical: `2/2 PASS`;
- scratch `UnexpectedPublicDeliveryContract`: both C225 and C226 RED with
  `Assert.DoesNotContain() Failure: Filter matched in collection`, followed by
  byte restoration to contracts SHA-256
  `175EEB3571C376CCE713E9E41590813E317EAF3F7C6E9E07F8835DD76413F5F3`;
- C3M02 unconditional-host mutation: C324 RED with
  `Unable to resolve service for type '...RecipientPackageDeliveryReconciler'`;
- C3M02 existing-key scope-widening mutation: C302 RED with
  `Assert.True() Failure`, expected true, actual false;
- both C3M02 owners restored byte-identically and their canonical controls
  passed.

The compatibility claim is intentionally one-way: C3 may identify its two
exact public contract types, while C2's path census and rejection of any other
download/delivery/locator type stay live. Census is now
41 paths / 9 SQL functions / 7 states / 10 transitions / 26 proofs /
34 mutations. The two earlier full-suite attempts remain rejected historical
evidence and are not C3 closeout evidence.

## RRI08 bounded suite-failure correction

The RRI07 replacement full suite completed Contract `13/13`, Architecture
`135/135`, Unit `195/195`, and Integration `780 PASS / 10 FAIL / 1 SKIP`.
Integration TRX SHA-256 was
`981618633D9DBBD742BE1A288B3A1D924D2BFE837D8777461E5610D7EEC1F255`.
SoftHSM 2.5.0 was discovered successfully; none of the ten failures was a
SoftHSM failure. That run is the third rejected historical full-suite run and
is not closeout evidence.

The ten failures reduced to three deterministic classes and were corrected
inside the existing 41-path allowlist:

- Program registered the C2 `RecipientPackageReadinessValidator` in disabled
  or invalid C2 topologies where `TagEkycDbContext` was not composed. Program
  now invokes C2 service composition only for syntactically valid
  `S3CompatibleDurable`; other topologies retain only their resolved options.
  Validator behavior is unchanged when registered.
- The E3 disposable pre-Up bootstrap provisioned the C2 LOGIN roles but not
  `tagekyc_raw_export_package_delivery_login`. The same exact LOGIN attribute
  verifier/creator now includes that C3 prerequisite before applying C3 Up.
- C202 used three open-ended catalog wildcards: two over `pg_class` and one
  over `pg_proc`. All three now enumerate exact C2-owned names. The CC
  correction that both table sites matched the C3 delivery tables was applied
  in the same count-neutral proof correction.

Permanent hashes (RRI07 bytes -> RRI08 bytes):

- Program: `DC9D2426FFA4D7C856385414C26003268290C524173E603ECC6F5D17978B274B`
  -> `618435C01560E40E49A0C3E88BC193714FB7DE217FDF1BFC971BAF674382F74C`;
- E3 proof/bootstrap:
  `5AAD862DABF319881F2ADA12F9411CC8E94AB602C9299B9881337AB30DF631C8`
  -> `EA499EF8F869C6A1B2767FEC7B0820C19C980CC4AF579311DE3391812A2CE9FB`;
- C2 C202 proof:
  `ADD5AB662DB590764A82530CC404A4452EA410DB25190638D0FA3A01E2530BFF`
  -> `F59C95631E365C851622B3D5585FA1CD4ECBDB914288F5764AF9C87B7E9AA6A3`.

Targeted and discriminating evidence:

- the six host-startup tests pass `6/6`; the three TIP-68 tests keep their
  exact signer-error assertions and pass because C2 composition no longer
  masks them, not because a TIP-68 assertion was relaxed;
- the three E3 isolated-database tests pass `3/3` with the C3 LOGIN role
  provisioned pre-Up;
- canonical C202 passes; adding a C3-named table and function leaves it PASS;
  renaming one genuine C2 function makes it RED with
  `C202_CATALOG_FAILURES=function-signature-owner-config-acl,function-execute-acl,function-surface`;
- C3M02 unconditional C2 composition makes all six host proofs RED with the
  exact missing-`TagEkycDbContext` DI failure; Program restoration hash is the
  RRI08 hash above and the six canonical controls pass;
- affected C3 controls pass Integration `22/22`, Unit `3/3`, Architecture
  `1/1`; all other mutation-owner bytes remain at their previously closed
  canonical hashes.

`READINESS-OWNERSHIP-RULE-01` is amended at proof level: test evidence must not
establish an owned-object census with an open-ended `LIKE`, prefix or suffix
predicate over `pg_class`, `pg_proc`, `pg_roles` or `pg_auth_members`. Every
such proof must enumerate an exact finite owned set. Pattern matching remains
permitted only when the pattern itself is the explicit invariant under test.

Harness pre-control for every later additive slice must budget both a
disposable database and pre-Up provisioning/verification of each new LOGIN
role required by the candidate migration. Census remains
41 paths / 9 SQL functions / 7 states / 10 transitions / 26 proofs /
34 mutations. Final closeout remains gated by one replacement complete
unfiltered Release suite on these restored bytes.

## Final RRI08 replacement full suite

Exactly one replacement complete unfiltered Release suite ran on the restored
RRI08 bytes. It completed GREEN:

- Contract: `13 passed / 0 failed / 0 skipped`;
- Architecture: `135 passed / 0 failed / 0 skipped`;
- Unit: `195 passed / 0 failed / 0 skipped`;
- Integration: `790 passed / 0 failed / 1 skipped`;
- aggregate: `1133 passed / 0 failed / 1 skipped / 1134 total`.

The only skip is the intentional manual generator
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
TRX SHA-256 values are:

- Contract: `60CACC92154CC8CB80E7A3A2F0EA56A5015CFADDF1238C73B951E62BDCC8DF64`;
- Architecture: `65BF317E1BC37BFA5BFF8CAE7D792BF12685F6137856878E6B743F3EA741D35E`;
- Unit: `D79CF95521BF81E8DC9F1BBE6449B3ED3C2537D1723E699FA838B24962C7DEE0`;
- Integration: `AAC552217ED489DD21B930727CC3A65FD243CDA63C736D8E6F9CC6551106A95B`.

Release build remains `0 warnings / 0 errors`; source hashes recorded before
the suite equal the post-suite hashes. Staged paths are zero,
`git diff --check` passes, and task-created container residue is zero. The three
earlier failed/aborted full-suite runs remain historical-only and are not
combined with this canonical result.

Final state is `INDEPENDENT_CLOSEOUT_REVIEW_PENDING`. This record does not grant
stage, commit, push, merge, PR, deployment or production activation authority.

## C3-CR-F01 bounded T05/T06 correction and replacement closeout

Independent review of the RRI08 closeout found that
`raw_export_begin_recipient_package_delivery_stream` returned `Expired` for a
due `Authorized` or `Interrupted` delivery without durably applying T05/T06.
The reviewed RRI08 bundle
`67D287CE576C071B752FFB41822BA286BD64FDA1DDA361AE4013CF6E10437133`
is therefore retained only as
`REJECTED_FOR_COMMIT_HISTORICAL_CLOSEOUT_EVIDENCE`.

Under the bounded Homeowner authority, the existing Begin function now uses
its single live post-lock `clock_timestamp()` to atomically:

- set `State = Expired`;
- increment `Revision` exactly once;
- set `ExpiredAtUtc` to that clock value; and
- append exactly one canonical `Expired` event using the existing
  `tip-88c1-c3-delivery-expiry-v1` evidence domain.

For `Authorized`, event identity and correlation come from
`CreatorApiKeyId`, `CreatorPrincipalId` and `AuthorizationCorrelationDigest`.
For `Interrupted`, they come from the persisted last-stream
`StreamApiKeyId`, `StreamPrincipalId` and `StreamCorrelationDigest`. The path
does no provider I/O and changes neither `StreamAttemptCount` nor
`DeliveryFence`.

C313 was strengthened count-neutrally. It proves admission expiry from both
`Authorized` and `Interrupted`, exact revision/timestamp/event/evidence actor
bindings, zero provider reads, no attempt/fence admission, replay stability,
and retains reconciler expiry from both predecessor states plus stale
`Streaming -> OutcomeUnknown` coverage.

C3M17 was extended count-neutrally. The scratch mutation retained outward
`Outcome = Expired` but removed the durable Begin update and event. C313 RED
with `Assert.Single() Failure: The collection was empty` at the canonical
Expired-event assertion. The migration was then restored byte-identically and
C313 passed. Canonical final hashes are:

- migration:
  `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD`;
- C3 integration proof:
  `672CE05ED049355DF6E9BE73FB19A5058B4B29026E322CBA90C21E9EDF768071`.

Targeted final controls passed Integration `22/22`, Unit `3/3` and
Architecture `1/1`, preserving C301-C326 at `26/26`. Release build passed with
zero warnings and zero errors. `git diff --check` passed and staged paths were
zero. The permanent census remains exactly
`41 paths / 9 SQL functions / 7 states / 10 transitions / 26 proofs / 34 mutations`.

Exactly one replacement complete unfiltered Release suite then ran on the
final corrected bytes and completed GREEN:

- Contract: `13 passed / 0 failed / 0 skipped`;
- Architecture: `136 passed / 0 failed / 0 skipped`;
- Unit: `195 passed / 0 failed / 0 skipped`;
- Integration: `790 passed / 0 failed / 1 skipped`;
- aggregate: `1134 passed / 0 failed / 1 skipped / 1135 total`.

The Architecture census is one higher than the rejected RRI08 closeout because
the separately owned, untracked Docker lifecycle anti-regression guard was
present during the complete unfiltered run. It is not a C3 allowlist member or
commit payload. The only skip remains the intentional manual golden-vector
generator.

The final tree also contains separately authorized test-harness volume
hardening in three paths that were already members of the C3 allowlist:

- `PostgresPersistenceFixture.cs` tears compose down with
  `down -v --remove-orphans`, including startup-failure cleanup;
- `Tip88B1E3ResolverReadBoundaryTests.cs` and
  `Tip83E1ReadinessEndpointTests.cs` run direct PostgreSQL containers with
  `--rm` and tmpfs-backed `/var/lib/postgresql/data`, while retaining
  idempotent `finally` cleanup.

These are harness-lifecycle changes, not C3 product/schema/SQL semantics. The
separate `DockerTestResourceLifecycleTests.cs` anti-regression guard remains
outside the 41-path C3 source manifest and is not silently admitted as path 42.
No Docker prune was used, no historical volume was removed by this work, and
SignFlow resources were not touched.

Replacement TRX SHA-256 values are:

- Contract: `DA4D299C58140CE3B6B0815004CED75C201A1DD1B1EEB6D96B4A7E633654B063`;
- Architecture: `4F0F8ECFE47E683C21F8963A742314A5694ACA185B1C1DCFA5E434372CFD230B`;
- Unit: `A2F513EDA4425CD5F596A9D4B7B159F7D25D2FA3C074F6809D0DDCF738BBE517`;
- Integration: `0978CE11CFC36642093C83D5FFB8D657AAC6B6BD43B1575E01D286D50CB49E7D`.

The run used pinned SoftHSM2 2.5.0 module
`1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635`
and utility
`EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E`;
both hashes were unchanged after the suite. Anonymous Docker volume census was
`0 -> 0`, task-created container residue was zero, and SignFlow volumes and
containers were not touched.

`C3-CR-F01` is closed. Final state remains
`IMPLEMENTATION_CLOSED_INDEPENDENT_CLOSEOUT_REVIEW_PENDING`. No stage, commit,
push, merge, PR, deployment or production activation occurred.

## Closeout R5 — path 42 ratification and machine-readable targeted evidence

Homeowner ratified the exact harness-only allowlist amendment `41 -> 42` by
adding:

`tests/TagEkyc.ArchTests/DockerTestResourceLifecycleTests.cs`

at SHA-256
`9C1E7AE07CDBBCC04B0C293C0F5F31D0DA879F12E55744A1585F0169F29CA24D`.
The file is the separately authorized anonymous-Docker-volume regression guard
that produced Architecture test 136. It is now an explicit C3 closeout source
member rather than unrelated compiled dirt. This amendment changes no product,
schema, SQL function, role, state, transition, codec, public route or error
code. Final census is `42 paths / 9 SQL functions / 7 states / 10 transitions /
26 proof IDs / 34 mutation IDs`.

Homeowner also accepted the already completed `1134/0/1` full-suite execution
as canonical unchanged-byte evidence. No replacement full suite was required.
The exact Release Architecture assembly used by that run remained
`F5683AF654DF9A7FCC8410D951AD7FFB28D34BA86696A91438AF5014BCA27D41`
after targeted evidence capture. The exact Release Integration assembly likewise
remained
`A9D820DA3A6346777F97D059AFE26EC529B7E1DEDE8ADEFE439C00B35082710F`.
The canonical migration, C313 proof and path-42 guard source hashes were
unchanged before and after the evidence cycle.

Machine-readable targeted evidence now exists:

- C313 canonical PASS TRX:
  `684EDCF4ACDDC1035D0B08673A62DF1C3DAB21A4FA6F4A32CE7A5082962F8354`;
- C3M17 durable-expiry mutation RED TRX:
  `CCA4E5A0942A49C4B48BCBC17F7AE06726886E999AA9EBF7B09AB688A9F48707`,
  exact error `Assert.Single() Failure: The collection was empty`;
- C313 byte-exact restore PASS TRX:
  `521E22E82294E4309B7401ED438CC0CA2CAE2DF6CBBBE9BDE51367220CB8A59E`.

Each run preserved anonymous Docker volumes at `0 -> 0`. Migration restored to
`93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD`;
C313 source remained
`672CE05ED049355DF6E9BE73FB19A5058B4B29026E322CBA90C21E9EDF768071`.
Final Release build passed with zero warnings and zero errors,
`git diff --check` passed, and staged paths remained zero.

The mutation evidence-of-record for unchanged C3M01-C3M16 and C3M18-C3M34
remains immutable historical bundle
`TIP88C1-C3-closeout-RRI08-20260819-R2.zip`, SHA-256
`67D287CE576C071B752FFB41822BA286BD64FDA1DDA361AE4013CF6E10437133`.
Its commit disposition remains rejected because of C3-CR-F01; only the
independently reviewed unaffected mutation artifacts are carried. C3M17 is
superseded by the new R5 machine-readable evidence above.
