# TIP-88C1-B2-DURABLE-KEY-FIXTURE-PROOF — Durable provider test-double proof — BUILD DISPATCH (v0.3)

**Status: READY FOR CLOSURE-ONLY REVIEW — NOT RATIFIED — NOT DISPATCHED.** This document is a docs-only candidate. It does not authorize implementation, migration execution, staging, commit, push, merge, PR, deployment, production activation, Raw BIO access, R2, object storage or delivery.

**Repository:** `D:\Task\Remote Signing\TagEkyc`
**Frozen code baseline:** `111b0f5ef6e9e5ccdc55bf559ee3d34e10ef4d10`
**Branch:** `tip-88a-raw-export-policy-catalog-build`
**Risk:** Tier-0 / PI-TAG-001 High-risk pilot
**Parent:** TIP-88C1-B2-DURABLE-KEY-PROD (DK-PROD)

## 0. Authority, dependencies and immutable anchors

This slice was decomposed from the abandoned monolithic durable-custody draft. It owns only a durable, non-production-qualified KEK provider test double and its proof surface. DK-PROD remains authoritative for the production key lifecycle, state machine, recovery semantics, capability boundaries, timing profile and provider-neutral contracts.

The builder must verify these files byte-for-byte before any implementation edit:

| Artifact | Required SHA-256 |
|---|---|
| `tip_88c1_b2_durable_key_prod_build_dispatch.md` | `0D4E5D6E59FF19421108A2BF0F5C659E53896B26473F1952C38BEC9AD6B1C359` |
| `tip_88c1_b2_durable_key_prod_state_model.py` | `AFC56ED767D59F5D74AD86462779B93670F35000548356BCA978F9269351F4BB` |
| `tip_88c1_b2_durable_key_csprng_prereq.md` | `C42B8562588D83AED835F120CD878E49C2E67355E85D9697B414BD114C34AF37` |
| `tip_88c1_b2_durable_key_prod_as_built.md` | `AAF9E89A20C5BFF09C3A69DE7239BF677FE4665746AFB09AA732A69BCF1F89E7` |

The current neutral contracts are the landed `IKekOperationProvider`, `IKekProvisioningRecoveryOperation`, `IDurableKekProviderCapabilitySource`, `ProviderOperationToken`, `KekReference`, `IAttemptDekCandidate`, `KekWrapResult`, `KekWrappedMaterial`, `KekOperationLookup`, `KekProvisioningResolution`, `KekProvisioningCleanupResult`, `DurableKekProviderCapabilities` and `AttemptDekLease` under `TagEkyc.Infrastructure.RawExport`. `WrapDekAsync` returns `KekWrapResult`; `LookupByOperationTokenAsync` returns `KekOperationLookup`. This slice adapts to those exact contracts; it does not edit or fork them.

Stale anchors are prohibited. In particular, do not cite the former monolith's “row 13”, “test #57” or metadata “§5”. The current recovered-activation contract is DK-PROD §1/T20; the metadata digest domain is DK-PROD §8; all fixture proof method names are local to this document.

STOP/RRI before editing if HEAD, any authoritative hash, or the landed neutral provider signatures differ.

## 1. Objective and non-goals

### 1.1 Objective

Prove on real PostgreSQL that one provider operation can create an AES-256-GCM wrapped DEK result once, persist that result durably, lose the process response, and recover the exact immutable result from a fresh process using only `(KeyProviderId, ProviderOperationToken, AttemptKeyContextFingerprint)`.

The proof must also show:

1. concurrent CreateOrGet calls produce one canonical journal row and both callers observe that row;
2. journal corruption and adapter-result divergence never become `Found`;
3. the fixture is durable and recovery-capable but never production-qualified or executable by production capability roles;
4. positive absence is proven by an independent in-test provider, not by an unreachable journal state;
5. production composition does not register this provider and cannot select it from configuration.

### 1.2 Non-goals

No production KMS/HSM/OpenBao integration; no S3/MinIO; no Raw BIO or patient-derived plaintext input; no R2 encryption pipeline; no object custody; no change to the DK-PROD state machine; no new public contract; no production provider selection; no public/runtime raw-key surface; no provider-operation cleanup state machine; no attempt to use this fixture as production readiness evidence. Every cryptographic test input is a synthetic fixed or randomly generated test buffer.

## 2. Analytical summary and Intent Ledger

### 2.1 Analytical summary

DK-PROD landed a provider-neutral lifecycle but deliberately did not land a durable KEK implementation. The existing `FixtureAttemptKeyProvider` is process-local and belongs to the earlier `IAttemptKeyProvider` surface. It cannot prove a lost-response restart. This slice adds a database-backed provider journal behind the landed neutral KEK port. The fixture KEK remains synthetic and non-production-qualified; durability belongs to the journal, not to process memory.

### 2.2 Intent Ledger

| Intent | Authoritative source | Implementation surface | Proof owner |
|---|---|---|---|
| Same provider token returns one immutable wrapped result | DK-PROD §3 | fixture journal + `raw_export_fixture_kek_wrap` | F01/F02/F03 |
| Fresh process recovers the exact result | DK-PROD T20 and provider port | `raw_export_fixture_kek_lookup` + fixture provider | F04 |
| Corruption never becomes `Found` | DK-PROD §8 | SQL recompute + C# recompute | F05/F06 |
| Positive absence is provider evidence, not missing-row inference | DK-PROD resolution taxonomy | in-test provider only | F07 |
| Fixture cannot pass production KEK readiness | DK-PROD readiness order | capability manifest `IsKekQualified=false` | F08 |
| No production selection or public key surface | DK-PROD capability boundary | no Program/config registration; internal classes | F09/F10 |
| Journal is immutable and callable only through exact roles | DK-PROD ACL posture | guard + ACL manifest | F11/F12/F13 |
| Migration round-trips without changing DK-PROD objects | DK-PROD migration invariants | additive migration | F14/F15 |

## 3. Chosen architecture

### 3.1 Deliberate fixture topology

The database journal is migration-managed so a fresh process and fresh `DbContext` can see the same provider result. The schema object may therefore exist in an installed database, but its entry functions are executable only by two fixture-only NOLOGIN roles that have no production membership. It is not registered by `Program.cs`, `AddTagEkycDurableKeyCustody`, configuration binding, reflection scanning or a production composition root. DK-PROD capability roles and deployment LOGIN roles receive no fixture-function privilege or fixture-role membership.

The fixture provider reports exactly:

```text
IsDurable       = true
IsKekQualified  = false
SupportsRecovery = true
SupportsCleanup  = true
```

`SupportsCleanup=true` is limited to the provider's defined no-external-resource behavior: this fixture never emits `ProviderResourceCleanupRequired`; `CleanupProvisioningOperationAsync` returns `KekProvisioningCleanupResult.Cleaned` only for `fixture-wrap:<32 lowercase hex>` and a 32-byte `AttemptKeyContextFingerprint`, with receipt `fixture-cleaned:<64 lowercase hex>` derived from `C1HashCanonical("tip-88c1-fixture-kek-cleanup-v1", cleanup-reference, context-lower-hex)`. It never deletes or rewrites journal evidence. This is test-double behavior, not production cleanup evidence. Readiness must progress past durability/recovery/cleanup capability and fail exactly at `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED`.

### 3.2 Fixture cryptography

The fixture KEK is synthetic test material derived as:

```text
SHA256(UTF8("tagekyc-tip88c1-fixture-attempt-kek-material-v1"))
```

It is never a production secret. Wrapping is AES-256-GCM with a fresh Random96 nonce, 32-byte DEK candidate, 32-byte ciphertext and 16-byte tag. AAD is:

```text
C1HashCanonical(
  "tip-88c1-fixture-kek-wrap-aad-v1",
  AttemptKeyContextFingerprint[lower-hex],
  KeyProviderId,
  KekId,
  KekVersion[decimal],
  KekFingerprint,
  WrappingSuiteId,
  WrappingSuiteVersion[decimal])
```

The provider accepts only these landed fixture KEK reference constants:

```text
KeyProviderId  = fixture-kek-provider-v1
KekId          = fixture-kek-v1
KekVersion     = 1
KekFingerprint = f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2
```

All owned DEK candidates, unwrapped leases, KEK scratch and AAD scratch are disposed/zeroized on success and failure. No raw DEK is persisted, logged, returned from a public member or stored in the journal.

The caller does not create provider handles or receipts. On the winning INSERT, SQL generates `FixtureWrapId = pg_catalog.gen_random_uuid()`, derives `ProviderResourceReference = "fixture-wrap:" + FixtureWrapId[N-guid]`, then derives `ProviderOperationReceipt = "fixture-receipt:" + lower-hex(C1HashCanonical("tip-88c1-fixture-kek-receipt-v1", ProviderResourceReference, AttemptKeyContextFingerprint[lower-hex], WrappedDekMetadataDigest[lower-hex]))`. A losing concurrent caller returns the canonical stored winner. The C# adapter verifies both derivations as well as the metadata digest, so divergence in a wrapped field, resource reference or receipt cannot become `Found`.

### 3.3 Positive absence decision

The durable fixture journal has no `PositivelyAbsent` row or state. A missing row returns `Unknown`, never `PositivelyAbsent` and never `NoProviderResult`. F07 uses a separate in-test `IKekOperationProvider` whose lookup returns `KekOperationLookup.PositivelyAbsent` with a fixed bounded receipt and proves that this provider-only outcome does not create or mutate a journal row. It does not claim to re-test DK-PROD's state-machine mapping. This closes the fixture-journal reachability issue without manufacturing an absence writer.

## 4. Exact database model

### 4.1 Table

Create exactly one table, `tagekyc.raw_export_fixture_kek_wrap_journal`, owned by `tagekyc_raw_export_deployer`, with these columns:

| Column | Type | Null | Rule |
|---|---|---:|---|
| `FixtureWrapId` | `uuid` | no | primary key; generated by SQL |
| `KeyProviderId` | `text` | no | exactly `fixture-kek-provider-v1` |
| `ProviderOperationToken` | `text` | no | exactly 43 base64url chars, no padding |
| `AttemptKeyContextFingerprint` | `bytea` | no | exactly 32 bytes |
| `WrappingSuiteId` | `text` | no | exactly `AES-256-GCM` |
| `WrappingSuiteVersion` | `integer` | no | exactly `1` |
| `WrappedDekCiphertext` | `bytea` | no | exactly 32 bytes |
| `WrappedDekNonce` | `bytea` | no | exactly 12 bytes |
| `WrappedDekTag` | `bytea` | no | exactly 16 bytes |
| `ProviderResourceReference` | `text` | no | exactly `fixture-wrap:` + `FixtureWrapId` as 32 lowercase hex |
| `ProviderOperationReceipt` | `text` | no | bounded opaque fixture receipt |
| `WrappedDekMetadataDigest` | `bytea` | no | exactly 32 bytes; SQL-derived |
| `CreatedAtUtc` | `timestamptz` | no | `statement_timestamp()` |

Exact names:

```text
pk_raw_export_fixture_kek_wrap_journal
uq_raw_export_fixture_kek_provider_token
uq_raw_export_fixture_kek_resource_ref
ck_raw_export_fixture_kek_wrap_shape
ck_raw_export_fixture_kek_wrap_text
trg_raw_export_fixture_kek_wrap_guard
enforce_raw_export_fixture_kek_wrap_guard
```

Unique keys are `(KeyProviderId, ProviderOperationToken)` and `ProviderResourceReference`. There is no update timestamp, absence state, cleanup state, plaintext digest, raw DEK, actor identity or generic payload/blob/content column.

All text is NFC-normalized and trimmed by the C# adapter before SQL. Database checks independently reject empty values, leading/trailing whitespace, non-NFC text, more than 512 UTF-8 bytes, U+0000..U+001F/U+007F, or a token outside `^[A-Za-z0-9_-]{43}$`. `ck_raw_export_fixture_kek_wrap_shape` additionally requires `KeyProviderId='fixture-kek-provider-v1'`.

### 4.2 Canonical metadata digest

SQL and C# independently compute:

```text
C1HashCanonical(
  "tip-88c1-wrapped-dek-metadata-v1",
  AttemptKeyContextFingerprint[lower-hex],
  WrappingSuiteId,
  WrappingSuiteVersion[decimal],
  WrappedDekNonce[lower-hex],
  WrappedDekCiphertext[lower-hex],
  WrappedDekTag[lower-hex])
```

The caller does not supply `WrappedDekMetadataDigest`. `raw_export_fixture_kek_wrap` derives and stores it. `raw_export_fixture_kek_lookup` recomputes it from stored columns, checks the resource-reference derivation and recomputes the receipt before returning. The C# adapter independently repeats all three checks before constructing `KekOperationLookup.Found`.

The DK-PROD §8 golden input must reproduce `607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30`. No fixture-specific variant of the metadata domain is permitted.

## 5. Exact SQL function manifest

Both functions are `SECURITY DEFINER`, owner `tagekyc_raw_export_deployer`, `SET search_path = pg_catalog`, one overload only, and explicitly schema-qualify every non-`pg_catalog` object.

### 5.1 CreateOrGet

```sql
tagekyc.raw_export_fixture_kek_wrap(
  text, text, bytea, bytea, bytea, bytea, text, integer)
RETURNS TABLE(
  outcome text,
  fixture_wrap_id uuid,
  wrapped_dek_ciphertext bytea,
  wrapped_dek_nonce bytea,
  wrapped_dek_tag bytea,
  wrapping_suite_id text,
  wrapping_suite_version integer,
  provider_resource_reference text,
  provider_operation_receipt text,
  wrapped_dek_metadata_digest bytea)
```

Argument order is `KeyProviderId, ProviderOperationToken, AttemptKeyContextFingerprint, WrappedDekCiphertext, WrappedDekNonce, WrappedDekTag, WrappingSuiteId, WrappingSuiteVersion`.

The five material arguments `WrappedDekCiphertext`, `WrappedDekNonce`, `WrappedDekTag`, `WrappingSuiteId` and `WrappingSuiteVersion` form one all-or-none creation group. A probe call supplies NULL for the entire creation group; a create call supplies every value. Provider resource reference, receipt and metadata digest are SQL-derived outputs, never input arguments.

Algorithm:

1. require `KeyProviderId='fixture-kek-provider-v1'`, validate all other identity arguments and the all-null/all-present creation group before write context;
2. lock and read the canonical row by `(KeyProviderId,ProviderOperationToken)`;
3. if a row exists with another context, raise SQLSTATE `P0001`, exact `MessageText=RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH`;
4. if a row exists with the same context, return `ExistingMatch` and its canonical material without consulting creation arguments;
5. if no row exists and the creation group is all NULL, return `Missing` with every returned material column NULL and perform no write;
6. for a create call, generate the row UUID, derive metadata digest, provider resource reference and receipt in SQL, set transaction-local `tagekyc.raw_export_fixture_kek_write_context=active` while preserving the prior value, then `INSERT ... ON CONFLICT (KeyProviderId,ProviderOperationToken) DO NOTHING`;
7. select the winner; context mismatch still raises the pinned failure, otherwise return `Created` when this call inserted or `ExistingMatch` with the winner's canonical stored material;
8. restore the prior GUC on success and exception.

Structural/shape failure, including any other provider ID, raises SQLSTATE `P0001`, exact `RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID`. No caller-supplied digest is accepted.

### 5.2 Lookup

```sql
tagekyc.raw_export_fixture_kek_lookup(text, text, bytea)
RETURNS TABLE(
  outcome text,
  fixture_wrap_id uuid,
  wrapped_dek_ciphertext bytea,
  wrapped_dek_nonce bytea,
  wrapped_dek_tag bytea,
  wrapping_suite_id text,
  wrapping_suite_version integer,
  provider_resource_reference text,
  provider_operation_receipt text,
  wrapped_dek_metadata_digest bytea)
```

Argument order is `KeyProviderId, ProviderOperationToken, AttemptKeyContextFingerprint`. The function requires `KeyProviderId='fixture-kek-provider-v1'`; any other value raises SQLSTATE `P0001`, exact `RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID`, before catalog access.

Outcomes are exactly:

```text
Found
Unknown
CorruptOrUnverifiable
```

No row returns `Unknown`. A row with a different context, failed metadata-digest recomputation, invalid resource-reference derivation or failed receipt recomputation returns `CorruptOrUnverifiable` with every material column NULL. A valid row returns `Found` and all material columns. It never returns `PositivelyAbsent`.

### 5.3 Guard

`enforce_raw_export_fixture_kek_wrap_guard()` permits only INSERT where `current_user='tagekyc_raw_export_deployer'` and the transaction-local write-context equals `active`. UPDATE and DELETE always raise SQLSTATE `P0001`, `RAW_EXPORT_FIXTURE_KEK_JOURNAL_APPEND_ONLY`. Missing/spoofed context raises `RAW_EXPORT_FIXTURE_KEK_WRITE_CONTEXT_INVALID`. The trigger runs `BEFORE INSERT OR UPDATE OR DELETE`.

## 6. ACL and role matrix

The migration creates exactly two fixture-only capability roles:

```text
tagekyc_raw_export_fixture_kek_wrap_executor
tagekyc_raw_export_fixture_kek_lookup_executor
```

Both are `NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT`. The migration grants no membership to them. In particular, the landed encryptor, reconciler and lifecycle capability roles and their deployment LOGIN roles are not members and cannot `SET ROLE` to either fixture role.

Disposable integration tests externally create `tagekyc_fixture_kek_wrap_test_login` and `tagekyc_fixture_kek_lookup_test_login` with `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT` and a per-run cryptographically random password that is never written to repository files, logs or reports, then grant exactly one matching membership with `ADMIN=false, INHERIT=true, SET=false`. Tests connect through those LOGIN identities, revoke memberships and drop both LOGIN roles in `finally`; these LOGIN roles and credentials are never created, dropped or referenced by the migration.

| Surface | fixture wrap role | fixture lookup role | DK-PROD capabilities/logins | runtime | PUBLIC | direct deployer owner |
|---|---:|---:|---:|---:|---:|---:|
| schema `tagekyc` USAGE | yes | yes | no new fixture grant | no new grant | no | yes |
| `raw_export_fixture_kek_wrap(...)` EXECUTE | yes | no | no | no | no | yes |
| `raw_export_fixture_kek_lookup(...)` EXECUTE | no | yes | no | no | no | yes |
| journal table privileges | none | none | none | none | none | owner only |
| guard function EXECUTE | none | none | none | none | none | owner only |

Migration must revoke PUBLIC default EXECUTE before exact grants. Effective ACL proof uses `aclexplode`, includes grantor, rejects extra grantees and column ACLs, proves one overload per function, and asserts zero fixture-role membership outside the disposable test window. No `tagekyc_runtime` or DK-PROD capability-role privilege is allowed.

## 7. C# adapter contract

Add internal `FixtureDurableKekOperationProvider` implementing `IKekOperationProvider`, `IKekProvisioningRecoveryOperation` and `IDurableKekProviderCapabilitySource`, plus an internal journal port and PostgreSQL adapter in the same implementation file unless compilation requires a split already declared below.

Behavior:

- `WrapDekAsync`: validate the exact fixture `KekReference`; under the fixture wrap role, probe `raw_export_fixture_kek_wrap` with the all-NULL creation group (the wrap role is not granted the lookup function); return the canonical result on `ExistingMatch`; on `Missing`, wrap the supplied candidate and call the same function with the complete creation group; return the canonical SQL winner; map context/integrity failures to `CorruptOrUnverifiable`; never expose candidate bytes.
- `LookupByOperationTokenAsync`: call SQL lookup and independently verify metadata digest, `ProviderResourceReference == "fixture-wrap:" + FixtureWrapId[N-guid]`, and the canonical receipt before mapping `Found`; map missing to `Unknown`; never synthesize absence.
- `ResolveProvisioningOperationAsync`: `Found→WrappedResultRecovered`, `Unknown→ProviderOutcomeUnknown`, corruption→`CorruptOrUnverifiable`. `NoProviderResult` is impossible for this adapter.
- `UnwrapDekAsync`: require fixture KEK reference, exact suite/version/lengths and valid AES-GCM AAD; return an owned `AttemptDekLease`; zero output on failure.
- `CleanupProvisioningOperationAsync`: for a valid `fixture-wrap:<32 lowercase hex>` and 32-byte context, return `KekProvisioningCleanupResult.Cleaned` with the exact deterministic receipt from §3.1; invalid reference or context returns `KekProvisioningCleanupResult.CleanupFailed`; cancellation observed before execution throws `OperationCanceledException`. Every path leaves journal row count and hashes unchanged. The fixture never emits `ProviderResourceCleanupRequired`.
- every `ToString()` for token, receipt/resource-bearing DTO or provider instance is redacted or contains no sensitive value.

The provider is never registered by production code. Integration tests construct it explicitly. To test internal contracts, this slice may add only `[assembly: InternalsVisibleTo("TagEkyc.IntegrationTests")]`; no other friend assembly is allowed.

## 8. Readiness and composition

No new readiness code is added. With DurableKey topology, exact CSPRNG/bootstrap prerequisites and the fixture instance registered under both `IKekOperationProvider` and `IKekProvisioningRecoveryOperation` only in a test service collection, `DurableKeyProviderReadinessValidator` must evaluate:

```text
topology valid
durability/recovery/cleanup supported
CSPRNG valid
IsKekQualified=false
→ PROD_RAW_EXPORT_KEK_NOT_QUALIFIED
```

It must not fail earlier with `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED`, and it must never pass. `Program.cs`, appsettings and the production service-registration extension remain byte-identical. An architecture test proves there is no configuration literal or production descriptor that resolves `FixtureDurableKekOperationProvider`; an integration catalog test proves no production capability role/login can execute or inherit either fixture entry function.

## 9. Migration lifecycle

One additive EF migration owns the two fixture-only NOLOGIN roles, fixture table, checks, unique indexes, guard trigger/function, two entry functions, owners and ACLs. It grants no LOGIN membership. It also adds the entity/configuration/DbContext mapping and snapshot entry.

`Down()` revokes exact function and schema grants, drops the two entry functions by full signature, drops trigger, guard function and table, then drops only the two fixture-only NOLOGIN roles. Disposable test memberships/logins must already have been removed by the test harness. `Down()` changes no DK-PROD table/function/role/membership/CSPRNG object. Apply→Down→reapply must restore catalog and ACL equivalence. The E3 model-snapshot tripwire may change one constant only after a zero-deletion additive snapshot proof.

No landed migration byte may be edited.

## 10. Exact implementation allowlist

The builder must print the following allowlist before writing and STOP/RRI if it must grow:

```text
src/TagEkyc.Infrastructure/Properties/AssemblyInfo.cs
src/TagEkyc.Infrastructure/RawExport/FixtureDurableKekOperationProvider.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportFixtureKekWrapJournalRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportFixtureKekWrapJournalConfig.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<allocated>_Tip88C1B2DurableKeyFixtureProof.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/<allocated>_Tip88C1B2DurableKeyFixtureProof.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2DurableKeyFixtureProofTests.cs
tests/TagEkyc.ArchTests/Tip88C1B2DurableKeyFixtureProofArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_fixture_proof_as_built.md
```

This dispatch document becomes verify-only once ratified. No `.csproj`, `Program.cs`, readiness endpoint, appsettings, DK-PROD source/test/artifact, debt registry or other document is in the implementation allowlist.

## 11. Proof manifest

Exact test class FQNs:

```text
TagEkyc.IntegrationTests.Tip88C1B2DurableKeyFixtureProofTests
TagEkyc.ArchTests.Tip88C1B2DurableKeyFixtureProofArchTests
```

| ID | Exact method | Property and required mutation bite |
|---|---|---|
| F01 | `FixtureWrap_ProbeMissingThenCreate_PersistsSqlDerivedDigest` | make probe insert, or swap nonce/ciphertext order in the SQL digest preimage → zero-row/golden/readback assertion fails |
| F02 | `FixtureWrap_SameTokenSameContext_ReturnsCanonicalExistingMatch` | replace conflict path with caller material → second result differs |
| F03 | `FixtureWrap_ConcurrentDifferentCandidates_OneRowOneCanonicalResult` | remove unique/CAS → row count or byte equality fails |
| F04 | `FixtureRecovery_FreshProcess_RecoversExactWrappedResult` | replace journal lookup with process memory → fresh instance returns Unknown |
| F05 | `FixtureLookup_CorruptedStoredWrappedField_ReturnsCorruptNeverFound` | in a scratch transaction, disable only the fixture guard trigger as deployer, flip one ciphertext byte without changing the digest, re-enable it, then remove the lookup recompute → expected Corrupt becomes Found; rollback restores the row and trigger state |
| F06 | `FixtureAdapter_DivergentReturnedMaterialHandleOrReceipt_IsRejected` | a journal-port decorator changes one wrapped byte, resource handle or receipt while leaving SQL outputs otherwise unchanged; remove the corresponding C# verification → typed corruption assertion fails |
| F07 | `PositiveAbsence_OnlyIndependentProvider_LeavesFixtureJournalUntouched` | map a missing journal row to positive absence → journal Unknown/zero-row assertion fails |
| F08 | `FixtureCapabilities_ReadinessAndCleanupContract_AreExact` | readiness subcase: set `IsKekQualified=true` → expected exact code disappears; cleanup subcases: valid reference/context returns exact deterministic `Cleaned` receipt with unchanged journal, invalid reference/context returns `CleanupFailed`, pre-cancel throws and mutates nothing; bypass validation or change receipt preimage → exact subcase fails |
| F09 | `ProductionCompositionAndRoles_CannotSelectOrExecuteFixtureProvider` | add production DI descriptor, grant a fixture role/function to a DK-PROD capability/login, or add production membership → descriptor/effective-ACL/membership assertion fails |
| F10 | `FixtureProvider_InternalSurface_DoesNotExposeDekOrHandles` | make provider/public member expose lease/token → architecture assertion fails |
| F11 | `FixtureJournal_UpdateDeleteDenied_AndFixtureRoleInsertSucceeds` | as deployer with active scratch context, remove the append-only branch → UPDATE/DELETE no longer raises exact P0001; fixture-wrap-role entry-function INSERT remains the positive control |
| F12 | `FixtureJournal_SpoofedGucAndDirectInsertDenied` | scratch-grant table INSERT to the fixture wrap role so the trigger is reached, set the GUC outside SD, then remove the current-user predicate → direct INSERT succeeds; restore the grant and function byte-identically |
| F13 | `FixtureFunctions_RolesMembershipAclOwnerGrantorAndOverloads_AreExact` | add PUBLIC/runtime/DK-PROD-role grant, cross-membership or column ACL → effective manifest fails; disposable test memberships are the only scoped positive control and are removed in `finally` |
| F14 | `FixtureIdentifiers_IntendedNamesUnder63AndRoundTripExactly` | add 64-byte intended name → static and catalog assertions fail |
| F15 | `FixtureMigration_ApplyDownReapply_RestoresExactCatalogAndAcl` | leave function/grant orphan in Down → equivalence fails |
| F16 | `FixtureUnwrap_ExactAadRoundTrips_AndWrongContextFails` | omit context from AAD → wrong-context decrypt unexpectedly succeeds |
| F17 | `FixtureLookup_MissingRow_IsUnknownNeverPositiveAbsence` | map missing to absence → exact outcome fails |
| F18 | `FixtureWrap_ProviderOrContextMismatch_RaisesPinnedFailureWithoutMutation` | remove exact provider-ID or context comparison → invalid provider inserts or mismatch returns ExistingMatch |
| F19 | `FixtureProviderIdentityTextBoundsTokensAndRedaction_AreExact` | relax exact `fixture-kek-provider-v1`, token/text/NFC/redaction rule → boundary assertion fails |
| F20 | `E3_ModelSnapshotDelta_IsAdditiveAndTripwireRoundTrips` | change any landed entity/delete snapshot line → additive manifest fails |

Every scratch mutation must be restored byte-identically and followed by its positive control. A test is not counted as mutation-proven unless the report includes the mutated command/source, exact failing assertion, observed value and restoration hash. F03 and F04 must fail-not-skip on a real disposable PostgreSQL instance.

## 12. Conditional matrices

### 12.1 Lookup matrix

| Row exists | Context matches | Stored digest matches recompute | SQL outcome | C# outcome |
|---:|---:|---:|---|---|
| no | n/a | n/a | Unknown | Unknown / ProviderOutcomeUnknown |
| yes | no | either | CorruptOrUnverifiable | CorruptOrUnverifiable |
| yes | yes | no | CorruptOrUnverifiable | CorruptOrUnverifiable |
| yes | yes | yes | Found | Found / WrappedResultRecovered |

There is no journal row yielding positive absence.

### 12.2 CreateOrGet matrix

| Canonical row | Context | Caller material | Result | Row mutation |
|---|---|---|---|---|
| absent | valid | all creation fields NULL | Missing | none |
| absent | valid | complete valid creation group | Created + inserted canonical | one INSERT |
| present | same | same or different | ExistingMatch + stored canonical | none |
| present | different | any | P0001 context mismatch | none |
| any | `KeyProviderId` not exact fixture literal | any | P0001 argument invalid | none |
| any | invalid or partially-null creation group | any | P0001 argument invalid | none |

### 12.3 Capability/readiness matrix

| Durable | Recovery | Cleanup | KEK qualified | Expected |
|---:|---:|---:|---:|---|
| false | any | any | any | `PROD_RAW_EXPORT_KEY_PROVIDER_DURABILITY_UNSUPPORTED` |
| true | false | any | any | same unsupported code |
| true | true | false | any | same unsupported code |
| true | true | true | false | `PROD_RAW_EXPORT_KEK_NOT_QUALIFIED` |
| true | true | true | true | forbidden fixture mutation; test must go RED, never accepted as fixture posture |

## 13. Task-0, validation and reporting

Task-0 must prove HEAD/baseline, clean `src/` and `tests/`, authoritative hashes, active allowlist, PostgreSQL version, full migration-chain apply, current E3 readiness, catalog/ACL snapshot and corrected pending-model command using Infrastructure as both project and startup project.

After implementation run and report:

1. `git diff --check` and exact changed-path allowlist;
2. Release full solution build with warnings/errors;
3. ArchTests full census;
4. targeted DK-PROD + DK-FIXTURE-PROOF + E3 integration census;
5. pending-model result;
6. full test-suite passed/failed/skipped numbers;
7. apply/Down/reapply catalog and ACL equivalence;
8. all F01–F20 positive results and required mutation-red evidence;
9. snapshot zero-deletion/additive proof and exact E3 one-line diff;
10. final SHA-256 for every changed file and confirmation of zero staged files.

## 14. STOP/RRI conditions

STOP rather than improvise if any of these occurs:

- any authoritative hash or landed neutral provider signature changes;
- any file outside §10 is required;
- implementation would require Program/appsettings/production DI registration;
- any DK-PROD capability role/login or runtime identity must receive fixture EXECUTE or fixture-role membership;
- a durable journal row must represent `PositivelyAbsent`;
- cleanup requires a new persistent state or function;
- a public contract, DK-PROD state transition or readiness code must change;
- a raw DEK/plaintext/raw-byte payload must be persisted or exposed;
- a landed migration must be edited;
- E3 snapshot delta is not purely additive;
- any mutation target cannot be made RED for its named reason;
- full migration/pending-model/build/test gate fails.

## 15. Review and ratification gate

Because this is a PI-TAG-001 High-risk pilot, the exploratory ladder has completed. The next review is closure-only and must verify only F-FIX-001 through F-FIX-003 plus synchronized sibling wording:

1. fixture entry functions are isolated behind membership-free fixture-only roles;
2. SQL and table shape pin the exact provider identity;
3. F08 executes and mutation-proves cleanup behavior as well as readiness;
4. §§3–15, ACL/migration matrices, F09/F11–F13/F18/F19 and version/status wording are synchronized;
5. no new acceptance criterion is introduced unless this patch created a concrete regression.

If review has not converged by round 5, classify the cause and tighten author/reviewer rules. Stop at round 10 and wait for Homeowner direction. Only an explicit Homeowner ratification may change this document to `READY FOR CONTROLLED BUILD DISPATCH` and authorize implementation.

### 15.1 Author self-review ledger

| Round | Mode | Finding | Disposition |
|---|---|---|---|
| A1 | bounded contract trace | `WrapDekAsync` originally depended on a differently authorized lookup surface, so the wrap caller could not execute its own existing-result path | closed by the fixture-wrap-role-owned NULL probe on the wrap function |
| A2 | sibling/anchor sweep | two authoritative artifact filenames were stale even though their hashes were current | closed by binding the actual `.py` state model and `_prereq.md` paths |
| A2 | mutation feasibility | direct DML and corrupted-row fixtures did not state how the guard was reached/restored | closed by exact scratch grant/trigger procedures in F05/F11/F12 |
| A3 | free adversarial trust-boundary review | caller-generated resource reference was outside `WrappedDekMetadataDigest` and could diverge undetected | closed by SQL-derived UUID/resource/receipt plus independent SQL and C# verification |
| A3 | absence semantics | F07 overclaimed re-testing the DK-PROD state-machine mapping | narrowed to the fixture-owned provider/journal boundary |
| I1 | independent consolidated review | production capability roles could execute fixture SQL despite DI exclusion | closed with two fixture-only NOLOGIN roles, zero production membership and exact ACL proofs |
| I1 | independent consolidated review | SQL accepted arbitrary `KeyProviderId` | closed with exact literal validation in both functions and table CHECK |
| I1 | independent consolidated review | cleanup capability was declared but not executed by any proof | closed count-neutrally in F08 with positive, negative, cancellation, no-mutation and mutation-red cases |

The author rounds and independent findings do not authorize implementation. The next reviewer must perform closure verification only.

## Changelog

- **v0.2 (2026-08-03):** rebased from pre-DK-PROD draft to `111b0f5`; replaced stale anchors; pinned the one-table/two-entry-function provider journal, SQL/C# dual integrity verification, SQL-derived provider handle/receipt, positive-absence split, readiness posture, production-composition exclusion, exact allowlist, migration lifecycle, matrices and F01–F20 proof manifest. Completed three author self-review modes and closed their findings. Status remains docs-only pending independent review and Homeowner ratification.
- **v0.3 (2026-08-03):** closed consolidated findings F-FIX-001..003: replaced production capability grants with membership-free fixture-only roles and disposable test logins; pinned exact provider identity in SQL/table shape; made cleanup behavior mutation-proven within F08. Added exact landed contract names and dual-interface test registration wording. Status is closure-only review, not ratified or dispatched.
