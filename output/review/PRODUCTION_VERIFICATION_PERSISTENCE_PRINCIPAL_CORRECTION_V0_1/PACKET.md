# PRODUCTION_APPLICATION_PERSISTENCE_PRINCIPAL_AMENDMENT V0.6

## Disposition

`REVIEW_CANDIDATE / NO COMMIT / NO RE-FREEZE / NO PUSH`

Baseline: `b02898d02c9204fb8afb7d381d32a31bfa3d322e`, seal revision 26.
The Homeowner-ratified amendment is implemented. Backup/PITR remains paused.

## Production boundary

The ordinary production API LOGIN must have exactly two recursively reachable
capabilities and no third role:

```text
tagekyc_runtime
tagekyc_application_persistence
```

Both direct edges require `ADMIN FALSE / INHERIT TRUE / SET FALSE`; both
capabilities have zero outgoing membership. The unlanded former role name is
absent from active source, tests, runbook, current packet and intent ledger.

Migration `20261002120000_ProductionApplicationPersistencePrincipal` grants the
application capability exactly:

- explicit schema `USAGE` on `tagekyc` and `public`, never `CREATE`;
- `SELECT, INSERT` on the eight ratified verification-core tables;
- column `UPDATE` on the ten ratified `verification_sessions` columns;
- `SELECT` only on `tagekyc.api_keys` and `public."__EFMigrationsHistory"`;
- `EXECUTE` only on
  `tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)`.

The recipient function is owned by `tagekyc_raw_export_deployer`, is
`SECURITY DEFINER`, pins `search_path=pg_catalog`, returns only a boolean, has
no PUBLIC/runtime execute, and is pinned by normalized `prosrc` SHA-256
`3639AB57514727BD06C85BF69FFBFD8A1306D8D28B5EE1022FA735D12F4F831E`.
The application capability has zero table/column privilege on all three managed
recipient tables. `PostgresHashedApiKeyStore` uses that function instead of EF
LINQ for its managed-recipient check. This is hardening of the new capability,
not a fix for an old 42501: historical C5 migrations already grant
`tagekyc_runtime` direct SELECT on the same three tables, and the ordinary LOGIN
inherits runtime. That pre-existing read surface is recorded as a separate debt;
it is neither revoked nor misrepresented by this amendment.

`tagekyc_runtime` was not widened. Its existing raw-export grants are preserved;
the amendment specifically proves it cannot update a non-ratified session
column and cannot DELETE or TRUNCATE `verification_sessions` (SQLSTATE 42501).

## Readiness and diagnosis

`ApplicationPersistenceReadinessValidator` measures the exact role graph,
safe role attributes, exact allowlisted grants, ten update columns, zero writes
on API-key/migration-history tables, and a database-wide non-system ACL/owner
census for schemas, relations `r/p/v/m/f`, columns, sequences and functions.
Expected EF objects must be base tables `r/p`. Out-of-allowlist grants to the
capability or current LOGIN fail closed; relation grants to PUBLIC also fail
closed. The capability/current LOGIN may own no non-system schema, relation,
sequence or function. Every allowlisted schema/relation/column/function ACL must
have `is_grantable=false`. At the database level, the current LOGIN and both
ordinary capabilities must not own the current database or have effective
`CREATE` on it; `CONNECT` remains available as required.

Stable codes:

```text
PROD_APPLICATION_PERSISTENCE_ROLE_INVALID
PROD_APPLICATION_PERSISTENCE_PRIVILEGE_INVALID
PROD_APPLICATION_PERSISTENCE_RECIPIENT_FUNCTION_INVALID
PROD_DB_PRIVILEGE_INVALID
```

`PostgresProductionReadinessValidator` now tests object existence through
`pg_catalog` before privilege, so missing SELECT is not mislabeled as a missing
table, missing migration history, or pending migration.

## Proof on the exact ordinary LOGIN

- application-principal final: `5/5 PASS`, zero skip;
- production-shaped core flow: `1/1 PASS`;
- E3 F3 exact two-capability runtime proof: `1/1 PASS`;
- E3 F6 Down/reapply: `1/1 PASS`;
- A1 startup composition: `1/1 PASS` after removing obsolete direct LOGIN grants;
- real Program HTTP flow: create session → artifact → evidence → complete, all on
  the restricted Postgres connection and with no SQLSTATE 42501;
- an approved owner/provisioning identity inserts production keys through
  `ApiKeyProvisioningService`; the ordinary LOGIN is proven unable to INSERT;
- `PostgresHashedApiKeyStore` and `C5CredentialAwareApiKeyAuthenticator` resolve
  BusinessConsumer, CaptureAgent, TrustedAdapter, delivery-operator and
  download-only credentials on the exact ordinary restricted connection;
- the HTTP flow covers BusinessConsumer, CaptureAgent and TrustedAdapter route
  surfaces; recipient delivery-operator and download-only scope authentication
  is exercised through the production store/authenticator/function path.

The old C526 whole-chain run is retained as a non-evidence predecessor: it
reached the unrelated site-qualification gate (`503`) and observed no 42501.
It is not used to claim this amendment GREEN.

Final successor evidence added by this correction:

```text
amendment-correction5-focused.trx   5/5 PASS, zero skip
amendment-correction5-affected.trx  3/3 PASS, zero skip
Infrastructure build               PASS, 0 errors
IntegrationTests build             PASS, 0 errors
API build                          PASS, 0 errors / 2 declared drift/generated warnings
Manifest                           109/109 byte-hash match
Census                             47 TRX rows / 25 failed tests / 0 unclassified
```

## Load-bearing mutations

```text
skip normalized recipient-function body hash
  0/1 RED at body-drift assertion: expected readiness exception, none thrown

bypass managed-recipient companion function
  0/1 RED at recipient key resolution: expected ApiKeyId, actual null

internal live-ACL arms in the final 5/5 gate
  missing membership, extra member, missing SELECT, capability DELETE,
  direct-login table/column ACL, column REFERENCES, runtime session SELECT,
  out-of-allowlist column grant, PUBLIC-readable definer view, foreign-schema
  relation, schema/relation/function ownership, schema/table/column/function
  grant options, api_keys UPDATE, migration-history INSERT, body drift and
  broker membership each go RED and restore to GREEN. Database `CREATE` granted
  to the exact LOGIN and database ownership transferred to that LOGIN each go
  RED with `PROD_APPLICATION_PERSISTENCE_PRIVILEGE_INVALID`, restore, then GREEN.
```

Restored byte hashes:

```text
ApplicationPersistenceReadinessValidator.cs
518A084AFB1F0D06780882B28ECAAEA0EE30FF0BC674ED2AEE648C912B2DEC97

PostgresHashedApiKeyStore.cs
B2345920D3286902C0704707B5D74CB7BA5386E6FF46632F5A1FD2F579D1AABF
```

Three stale model-snapshot test pins are reconciled to the already-landed
rev24 snapshot bytes (`4D244D...108C5`); no model or schema bytes changed for
that reconciliation. Startup evidence is likewise updated to the already
ratified format-2/DurableWorker/site-policy-v1 seal shape.

## Operations and documentation

The deployment runbook names the shipped `TagEkyc.ApiKeyProvisioner` command as
the production Active-key provisioning step and requires an approved
owner/migrator/provisioning write identity. The ordinary runtime LOGIN is
SELECT-only and cannot provision; ad-hoc SQL insertion remains forbidden.
The E3 closeout keeps its historical sentence and adds an explicit superseding
line. The debt registry records both the separate E3 cross-schema census gap and
the historical C5 runtime recipient-table SELECT surface without changing either.
The focused gate positively pins all three historical C5 `tagekyc_runtime`
recipient-table SELECT grants, so the factual debt cannot silently become stale.
Up and Down remove column ACLs across `r/p/v/m/f`.

The retained `amendment-e3-f6-final.trx` is a zero-test `NO_TEST_MATCH`
diagnostic and is not counted as an executing run. The former incomplete API
build log was replaced by a successful final build: zero errors, with two
declared warnings (intentional pre-review seal drift and the generated nullable
warning). No incomplete log is described as PASS.

## Boundary

No endpoint, protocol, state machine, data table/column, A3, Layer 2, KEK,
Claim Provider, authority, consent, retention, SignFlow or Raw BIO semantic was
changed. No seal was re-minted. No commit or push exists. The current seal is
expected to drift until independent review PASS and a separately authorized
product commit/evidence-only successor. Backup/PITR remains OPEN and paused.
