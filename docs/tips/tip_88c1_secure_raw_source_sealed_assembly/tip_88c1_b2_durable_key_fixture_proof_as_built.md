# TIP-88C1-B2-DURABLE-KEY-FIXTURE-PROOF — As built

Version: 0.4
Status: PROOF BUILD CLOSED — READY FOR CONTROLLED COMMIT — NOT COMMITTED
Date: 2026-08-04
Baseline: `111b0f5ef6e9e5ccdc55bf559ee3d34e10ef4d10`
Ratified dispatch SHA-256: `AEFFC7C8DB5FB3F459A60E396575C2330435CC9557E6A9B61D886D216D87859B`

## 0. Changelog

### v0.4 — independent closeout accepted

- Recorded the accepted R2 verdict `PASS — READY FOR CONTROLLED
  CLOSEOUT/COMMIT AUTHORIZATION` and bound it to the reviewed closure bundle.

### v0.3 — independent-closeout corrections

- Extended F13 count-neutrally to prove the journal's exact owner-only
  `pg_class.relacl`, zero effective table privileges for runtime, DK-PROD
  capability/deployment roles and fixture roles/logins, exact fixture-role
  schema grants, no PUBLIC schema grant, and no column ACL.
- Redacted `FixtureKekJournalResult.ToString()` and extended F19 count-neutrally
  to prove that neither fixture resource reference nor receipt is rendered.
- Mutation-proved both corrections, restored the migration/provider bytes, and
  reran the complete Release validation census.

## 1. Boundary

This slice lands a durable PostgreSQL-backed **test double** for the neutral DK-PROD KEK port. It does not register a production provider and does not add Raw BIO, R2, object storage, staging, delivery, deployment or production activation.

The synthetic KEK material is fixture-only. Durability belongs to the append-only journal. A fresh provider and fresh `DbContext` recover the canonical wrapped result using the frozen provider-operation token and attempt-key-context fingerprint.

## 2. Exact implementation allowlist

1. `src/TagEkyc.Infrastructure/Properties/AssemblyInfo.cs`
2. `src/TagEkyc.Infrastructure/RawExport/FixtureDurableKekOperationProvider.cs`
3. `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportFixtureKekWrapJournalRow.cs`
4. `src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportFixtureKekWrapJournalConfig.cs`
5. `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`
6. `src/TagEkyc.Infrastructure/Persistence/Migrations/20260803151824_Tip88C1B2DurableKeyFixtureProof.cs`
7. `src/TagEkyc.Infrastructure/Persistence/Migrations/20260803151824_Tip88C1B2DurableKeyFixtureProof.Designer.cs`
8. `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`
9. `tests/TagEkyc.IntegrationTests/Tip88C1B2DurableKeyFixtureProofTests.cs`
10. `tests/TagEkyc.ArchTests/Tip88C1B2DurableKeyFixtureProofArchTests.cs`
11. `tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs` — one snapshot-hash constant only
12. this as-built document

No `.csproj`, `Program.cs`, appsettings, production service-registration extension, DK-PROD migration or ratified artifact changed.

## 3. Landed catalog

### 3.1 Table and guard

- Table: `tagekyc.raw_export_fixture_kek_wrap_journal`
- Owner: `tagekyc_raw_export_deployer`
- Columns: 13; no raw DEK, plaintext digest, absence state or cleanup state
- Constraints: `pk_raw_export_fixture_kek_wrap_journal`, `ck_raw_export_fixture_kek_wrap_shape`, `ck_raw_export_fixture_kek_wrap_text`
- Unique indexes: `uq_raw_export_fixture_kek_provider_token`, `uq_raw_export_fixture_kek_resource_ref`
- Trigger: `trg_raw_export_fixture_kek_wrap_guard`
- Guard: `enforce_raw_export_fixture_kek_wrap_guard()`

The guard is invoker-rights. This is load-bearing: a direct fixture-role insert with a spoofed GUC remains the fixture caller and is rejected; an insert nested under the owner-executed SECURITY DEFINER entry function observes the deployer identity. UPDATE and DELETE always raise `P0001 RAW_EXPORT_FIXTURE_KEK_JOURNAL_APPEND_ONLY`.

### 3.2 Entry functions

```text
tagekyc.raw_export_fixture_kek_wrap(
  text,text,bytea,bytea,bytea,bytea,text,integer)

tagekyc.raw_export_fixture_kek_lookup(text,text,bytea)
```

Both entry functions are SECURITY DEFINER, owned by `tagekyc_raw_export_deployer`, use `search_path=pg_catalog`, and have exactly one overload. Returned varchar-backed fields are explicitly cast to the ratified `text` return manifest.

Pinned failures:

```text
P0001 RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID
P0001 RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH
P0001 RAW_EXPORT_FIXTURE_KEK_JOURNAL_APPEND_ONLY
P0001 RAW_EXPORT_FIXTURE_KEK_WRITE_CONTEXT_INVALID
```

### 3.3 Roles and ACL

Migration-managed NOLOGIN roles:

```text
tagekyc_raw_export_fixture_kek_wrap_executor
tagekyc_raw_export_fixture_kek_lookup_executor
```

The wrap role executes only the wrap function. The lookup role executes only the lookup function. Neither role has table privileges or migration-created membership. PUBLIC, runtime, DK-PROD capability roles and DK-PROD deployment LOGIN roles have zero fixture-function privilege and zero fixture-role membership. Test LOGIN roles are externally provisioned with random credentials, exact one-to-one memberships and `ADMIN=false, INHERIT=true, SET=false`, then removed in `finally`.

F13 additionally reads the table ACL from `pg_class.relacl` through
`aclexplode(COALESCE(relacl,acldefault(...)))`: the deployer is the sole table
grantee with the exact seven owner privileges. Effective `has_table_privilege`
checks cover runtime, all DK-PROD capabilities/logins, both fixture roles and
the disposable fixture logins for every table privilege. The two direct
fixture schema grants are exactly `USAGE`, with no PUBLIC schema grant, and the
journal has no column ACL.

## 4. C# surface

New internal implementation types:

```text
TagEkyc.Infrastructure.RawExport.FixtureDurableKekOperationProvider
TagEkyc.Infrastructure.RawExport.PostgresFixtureKekJournal
TagEkyc.Infrastructure.RawExport.IFixtureKekWrapJournal
TagEkyc.Infrastructure.RawExport.IFixtureKekLookupJournal
TagEkyc.Infrastructure.RawExport.FixtureKekJournalResult
```

The provider implements `IKekOperationProvider`, `IKekProvisioningRecoveryOperation` and `IDurableKekProviderCapabilitySource`. Its capability manifest is exactly `(durable=true, KEK-qualified=false, recovery=true, cleanup=true)`. It is not production-registered. Wrap/lookup validate the SQL-derived metadata digest, resource reference and receipt independently in C#. AAD binds the full attempt-key context. Locally owned ciphertext/nonce/tag and AAD scratch are zeroed on all exits.

`FixtureKekJournalResult.ToString()` is exactly
`FixtureKekJournalResult:<redacted>`; it cannot render the resource reference,
provider receipt or wrapped-material fields.

Readiness reaches and fails exactly at `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`; it neither fails early as non-durable nor passes production readiness.

## 5. Model and migration evidence

- Snapshot delta: `+75/-0`, containing only `RawExportFixtureKekWrapJournalRow` and its table metadata.
- E3 snapshot constant: `E71FCC9A473EBA48A8242B146A76AD5AEB7B9CE3561B4EBA6EDFD894E343C941`.
- Pending-model command uses Infrastructure as both project and startup project and reports no pending changes.
- Apply → Down to `20260802105416_Tip88C1B2DurableKeyProd` → reapply restores the exact fixture catalog/ACL digest while preserving DK-PROD objects and deployment LOGIN roles.

## 6. Proof and mutation evidence

Positive proof census: 20/20 (`18` integration, `2` architecture), 0 failed, 0 skipped.

| Proof | Scratch mutation observed RED for the named reason |
|---|---|
| F01 | common-mode swapped nonce/ciphertext in all three SQL metadata recomputations and in C#; SQL=C#=persisted=lookup remained true, then the absolute golden assertion expected `607a1902...89d30` and observed `f1cac2d6...912d` |
| F02 | returned caller material on ExistingMatch; canonical ciphertext assertion differed (`0xA1` candidate observed) |
| F03 | removed conflict-safe insert; concurrent caller raised `23505 uq_raw_export_fixture_kek_provider_token` |
| F04 | replaced durable lookup with process-local Unknown; expected `WrappedResultRecovered`, observed `ProviderOutcomeUnknown` |
| F05 | removed SQL integrity branch; raw lookup expected `CorruptOrUnverifiable`, observed `Found` |
| F06 | removed C# receipt verification; divergent receipt became `Found` |
| F07 | mapped missing journal row to positive absence; expected `Unknown`, observed `PositivelyAbsent` |
| F08 | set `IsKekQualified=true` and separately changed cleanup receipt domain; capability/readiness and exact receipt assertions went RED |
| F09 | registered fixture provider in production service collection; source/composition assertion went RED |
| F10 | made fixture journal result public; internal-surface assertion went RED |
| F11 | removed append-only branch; expected append-only message, observed write-context message |
| F12 | removed current-user predicate; spoofed direct INSERT succeeded instead of throwing |
| F13 | granted journal-table SELECT to runtime; the exact table ACL gained `tagekyc_runtime|tagekyc_raw_export_deployer|SELECT` at position 7 and F13 went RED |
| F14 | added an 82-byte intended identifier; static 63-byte assertion went RED |
| F15 | left lookup function orphaned in Down; reapply failed because the exact overload already existed |
| F16 | removed context from AAD; wrong-context unwrap unexpectedly succeeded |
| F17 | mapped Unknown to positive absence; exact outcome assertion went RED |
| F18 | removed exact provider-ID gate; invalid-provider probe stopped throwing the pinned failure |
| F19 | removed the `FixtureKekJournalResult.ToString()` override; expected `FixtureKekJournalResult:<redacted>`, observed the compiler-generated record text beginning `FixtureKekJournalResult { Outcome = Found, ... }` |
| F20 | changed one landed snapshot line; additive subsequence assertion named the deleted/changed line |

Every scratch mutation was removed. Restoration SHA-256 values before final positive controls:

```text
D19F2F062D97847F510E7B181E2C6FEF35D3204699832326DFCBC458AFC9B19D  migration
66C40C0C3998D9729F625D5E2F21719E08AF48A4CE8A05F43CD623AB86954F2A  provider
5024202DE94772302BC1ABCBFC8E598FE7BBECF5E9A912D1A948F7F859C358A4  production registration extension (unchanged)
```

## 7. Validation

```text
Release build: 0 warnings, 0 errors
F01–F20: 20 passed, 0 failed, 0 skipped
E3 resolver-read-boundary: 28 passed, 0 failed, 0 skipped
Pending model: clean
Full solution before environment remediation: 953 passed, 6 failed, 1 skipped, 960 total
Full solution after verified SoftHSM2 provisioning: 959 passed, 0 failed, 1 skipped, 960 total
```

The first full-solution run had five TIP-68 SoftHSM failures because
`softhsm2-util` is not installed on this Windows host. The sixth failure was the
E3 snapshot hash tripwire observing the final additive snapshot hash after the
full run had already loaded the previous test binary; the one authorized constant
was refreshed and the complete E3 class was rerun green at 28/28. No commit,
push, merge, PR or deployment was performed.

The historical STOP/RRI environment was Windows 10 Pro 10.0.19045,
Pkcs11Interop 5.3.0. The exact command was
`dotnet test TagEkyc.sln -c Release --no-build --logger "console;verbosity=minimal"`.
At that point the portable SoftHSM module DLL was present without its matching
`softhsm2-util` executable. Section 9 records the subsequent environment-only
closure.

## 8. B1 golden-anchor disposition — 2026-08-04

```text
B1 CLOSED_BY_ABSOLUTE_GOLDEN_AND_COMMON_MODE_MUTATION_PROOF
SOFTHSM PREREQUISITE CLOSED_BY_VERIFIED_SOFTHSM2_PREREQUISITE_PROVISIONING
```

- Finding: `CONTRACT_REQUIRED_GOLDEN_ASSERTION_MISSING`.
- Prior F01 proof: differential SQL == C# only; it did not bind the absolute
  DK-PROD wrapped-metadata vector.
- Corrected method:
  `FixtureWrap_ProbeMissingThenCreate_PersistsSqlDerivedDigest` (count-neutral).
- Canonical expected digest:
  `607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30`.
- Canonical F01: probe returned `Missing` with zero rows; create returned
  `Created`; SQL return, persisted row, independent C# recomputation and lookup
  readback all equalled the complete expected digest; final row count was one.
- Common-mode mutation: in all three SQL metadata-preimage occurrences,
  `nonce, ciphertext` was changed to `ciphertext, nonce`; the C#
  `ComputeMetadataDigest` scalar order was changed identically. The first three
  differential assertions remained satisfied. F01 then went RED at the absolute
  assertion in `Tip88C1B2DurableKeyFixtureProofTests.cs:136` with expected
  `607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30`
  and observed SQL/C#/persisted/lookup digest
  `f1cac2d6f1c9e874ec22929921ee511760fa843a425ae411865292359e61912d`.
  This is
  `EXPECTED_DKPROD_GOLDEN_DIGEST_BUT_COMMON_MODE_PREIMAGE_DRIFT_OBSERVED`.
- Restoration: migration returned to
  `D19F2F062D97847F510E7B181E2C6FEF35D3204699832326DFCBC458AFC9B19D`;
  provider returned to
  `5CF0465C871F91A0F01513AE916E0444AE257023B55417F1B2D0C61C6E3BD9C1`.
- Post-restoration F01: 1 passed, 0 failed, 0 skipped.
- Fixture integration: 18 passed, 0 failed, 0 skipped.
- Fixture architecture: 2 passed, 0 failed, 0 skipped.
- Final F01–F20: 20/20 PASS.
- Final mutation obligations: 20/20 observed RED for the assigned reason and
  restored.
- Post-closeout-correction integration-test SHA-256:
  `297A86784421572B2CAC0BBC86DFEDA14AA606C7540E21AEB1378A3EEBAF6F03`.
- The post-patch as-built SHA-256 is reported externally after byte freeze;
  embedding a file's own digest would change that digest.

## 9. SoftHSM2 environment disposition — 2026-08-04

```text
CLOSED_BY_VERIFIED_SOFTHSM2_PREREQUISITE_PROVISIONING
```

- Provisioning was outside the repository and process-local only. The
  Chocolatey `softhsm.portable` 2.5.0 package SHA-256 was
  `92A11CEA7E0F9D60EF731CEE53F718E46B92F03E7D1EB820D15A63AF93D4ABE1`.
- Its embedded `SoftHSM2-2.5.0-portable.zip` matched the package verification
  checksum
  `85273BCC1A6B90E877F7BB4F7E90221D57103D8F5241D154A79DD730A135B910`.
- `softhsm2-util --version` returned exactly `2.5.0`. The utility and module
  were extracted from that same verified archive. Utility SHA-256:
  `EFB81B0D6691C515EB796BEA7C81C8D9048AB4DFCCD9DDE2FFB6A1BE33596C6E`;
  module SHA-256:
  `1980A74F3088A7273D7EFA502B6CEB8DE6A5285D5BCD36D49512A8717BF89635`.
- The five `Tip68SoftHsmE2ETests` passed: 5 passed, 0 failed, 0 skipped.
- The complete unfiltered Release suite passed: Contract 13/13,
  Architecture 110/110, Unit 188/188, Integration 648 passed with one skip;
  aggregate 959 passed, 0 failed, 1 skipped, 960 total.
- The only skip remained
  `Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`,
  the intentional manual generator.
- No fixture implementation, test, migration, dispatch, DK-PROD, project,
  Program or appsettings byte changed during this environment remediation.

## 10. Independent-closeout correction disposition — 2026-08-04

- F13/F19 correction scope: one fixture provider file and the existing
  integration-test class; no migration, schema, production registration or
  test-method census change.
- F13 mutation RED: expected the seven owner-only table ACL rows; observed the
  additional exact row
  `tagekyc_runtime|tagekyc_raw_export_deployer|SELECT`.
- F19 mutation RED: expected `FixtureKekJournalResult:<redacted>`; observed the
  compiler-generated record representation containing the result fields.
- Restoration SHA-256: migration
  `D19F2F062D97847F510E7B181E2C6FEF35D3204699832326DFCBC458AFC9B19D`;
  provider
  `66C40C0C3998D9729F625D5E2F21719E08AF48A4CE8A05F43CD623AB86954F2A`.
- Restored positive control: F13/F19 2 passed, 0 failed, 0 skipped.
- Final F01–F20: integration 18/18 and architecture 2/2.
- E3 resolver-read-boundary: 28/28; pending model clean.
- Release build: 0 warnings, 0 errors.
- TIP-68 SoftHSM2 E2E: 5/5 using the hash-pinned portable 2.5.0
  prerequisite.
- Complete unfiltered Release suite: 959 passed, 0 failed, 1 skipped, 960
  total; the sole skip remains the intentional manual golden-vector generator.
- Raw TRX, build binlog, pending-model log, prerequisite hashes and mutation
  logs are carried in the external independent-review bundle rather than the
  repository.
- Independent R2 closeout verdict: `PASS — READY FOR CONTROLLED
  CLOSEOUT/COMMIT AUTHORIZATION`.
- Accepted closure bundle SHA-256:
  `A4F375557012DCD2C3F044B4CC316F624235383275071E8EF265B5C6418EC0AC`.
