# Production Raw Export authority snapshot — review packet v1

## Disposition

`TECHNICAL PASS CANDIDATE — GPTW MEDIUM-01/MEDIUM-02 CORRECTED; RE-REVIEW REQUIRED.`

Baseline is `a7a448f09c53cdea1f943b335ce70acf9c4adda3` (seal revision 25), branch `tip-88a-raw-export-policy-catalog-build`, aligned with its upstream before this working-tree candidate. Nothing in this slice is committed, re-frozen, pushed, or deployed.

The Homeowner-authorized boundary is unchanged: replace the Production authority-snapshot profile's self-declared string pass with readiness bound to the already-landed controller evidence producers; retain exact fixture/missing/unknown fail-closed behavior; add focused proof; reconcile the factual debt row. No schema, migration, authority state-machine, legal/retention disposition, Raw BIO protocol, A3, Layer 2, KEK, Claim Provider, SignFlow, backup/restore, or site-deployment change exists.

## Inventory result

No missing persistence model was found. Both production evidence-producing lineages already exist in the current database contract:

1. `LegacyExport` enters through `raw_export_begin_production_source_ingress_with_authority`, which delegates to the private core that joins the authorization decision, permit/class, current eligibility and subject-consent evidence before using the generic authority append.
2. `SourceRetention` enters through `raw_export_append_retained_authority_snapshot`, which rejoins the capture execution binding, retention permit/class, consent reference/binding/event and retention authority resolver before using the generic authority append.

The correction therefore adds no table, migration, endpoint, protocol, producer, or second authority engine.

## Production readiness correction

`Profile=Production` is no longer sufficient by itself. The validator checks six current controlled functions by exact signature and requires:

- owner `tagekyc_raw_export_deployer`;
- `SECURITY DEFINER`;
- exact `search_path=pg_catalog` posture;
- no `PUBLIC` or `tagekyc_runtime` EXECUTE;
- broker EXECUTE only on the two checked public entry capabilities;
- no broker EXECUTE on the private core or generic append;
- an exact SHA-256 match over normalized `pg_proc.prosrc` for each reviewed function body.

The controlled set includes both checked entry lineages, their private append/core functions, and the authority withdrawal/revocation functions. The latter two must remain unavailable to `PUBLIC`, `tagekyc_runtime`, and the claim broker.

It separately requires `PUBLIC`, `tagekyc_runtime`, and `tagekyc_raw_export_claim_broker` to have no direct authority-table privileges across `SELECT, INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER`.

Readiness deliberately does not scan business rows. The application/runtime principals intentionally have no read access to the authority and A3 retention tables. Granting row visibility only for readiness would weaken the least-privilege boundary. The controlled `SECURITY DEFINER` producers are the evidence contract being qualified.

An empty authority ledger is valid: it means no authority has yet been produced, not that a self-asserted ratification flag has passed.

Stable existing profile failures remain unchanged:

```text
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE
```

Producer/privilege drift uses:

```text
PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PRODUCER_INVALID
```

## Focused proof

Final focused gate:

```text
8 executed / 8 passed / 0 failed / 0 skipped
```

The production readiness test begins with an empty authority ledger and proves the current graph passes. Inside rollback-only transactions it then proves all of these fail closed with the stable producer-invalid code and return to green after rollback:

- replace the LegacyExport wrapper with the same signature, owner, ACL, search-path and mandatory call tokens but a one-byte-equivalent whitespace body drift;
- revoke broker EXECUTE from the LegacyExport production wrapper;
- revoke broker EXECUTE from the SourceRetention checked entry;
- grant broker EXECUTE directly to the private LegacyExport core;
- grant broker or runtime EXECUTE to withdraw authority;
- grant broker or runtime EXECUTE to revoke authority;
- grant broker direct INSERT on the authority table.

Two independent source mutations demonstrate that the assertions are load-bearing:

```text
skip producer verification       0/1 RED — no exception at LegacyExport revoke
skip table privilege boundary    0/1 RED — no exception at direct INSERT grant
skip normalized body SHA         0/1 RED — no exception at body drift
omit withdraw/revoke controls    0/1 RED — no exception at forbidden EXECUTE grant
```

The validator source was restored byte-exact to SHA-256:

```text
1474E0C2A137779024C3B7A6912E9901CB9E12C5982616FC6856C764F79A279C
```

Final focused builds retained:

```text
TagEkyc.Infrastructure       PASS / 2 pre-existing nullable warnings / 0 errors
TagEkyc.IntegrationTests     PASS / 0 warnings / 0 errors
```

No full solution suite was run; it is outside this bounded slice.

## Debt reconciliation

`C1-B2-AUTH-FIXTURE-PRODUCTION-GATE` is updated from missing-product-path to product-path closed. The adjacent `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` remains explicitly open for real legal/retention ratification. The current `FreshAuthorityRequired`, `Forbidden`, and `SubjectRawBiometricExport` values are not reinterpreted or ratified by this correction.

`POSTGRESQL_LOSSLESS_BACKUP_RESTORE_QUALIFICATION` remains separately open and continues to block real-patient go-live.

## Write-set

Product/test/document bytes:

```text
src/TagEkyc.Infrastructure/Persistence/RawExportAuthoritySnapshotReadinessValidator.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2AuthoritySnapshotTests.cs
docs/phase1_scope_and_debt_registry_v0_1.md
```

Review evidence is under this directory only. The Homeowner's modified `docs/00_GDRIVE_FILE_INDEX.md` and pre-existing review ZIPs were not touched or included.

## Stop posture

```text
Commit       NO
Re-freeze    NO
Push         NO
Deployment   NO
```

External review must precede any product commit or seal successor.
