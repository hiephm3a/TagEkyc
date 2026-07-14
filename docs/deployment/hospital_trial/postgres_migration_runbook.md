# Hospital Trial Postgres Migration Runbook

## Purpose

Apply TagEkyc EF migrations as an explicit deploy step. Production startup never runs `Database.Migrate()`; it only performs a read-only readiness gate.

## Preconditions

- `TagEkyc:Persistence:Provider=Postgres`
- `TagEkyc:Persistence:ConnectionStringSecretRef=env:TAGEKYC_POSTGRES_CONNECTION_STRING` or `file:<absolute protected path>`
- The resolved secret is a Postgres connection string stored outside appsettings.
- Hospital IT confirms encryption at rest for the Postgres data volume or database service.

## Apply

From the repo root or release workspace:

```powershell
dotnet ef database update --project src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj --startup-project src/TagEkyc.Api/TagEkyc.Api.csproj
```

For a release bundle, build and execute an EF migration bundle with the same project/startup-project pair and the production connection string supplied from the approved secret store.

## Readiness Checks

After migration, production startup must fail closed unless all are true:

- EF provider is Npgsql/Postgres.
- Database is reachable.
- `__EFMigrationsHistory` exists.
- There are no pending EF migrations.
- `tagekyc.append_idempotency_records` exists.

Failure codes are sanitized: `PROD_DB_UNREACHABLE`, `PROD_DB_PROVIDER_INVALID`, `PROD_DB_MIGRATION_HISTORY_MISSING`, `PROD_DB_MIGRATIONS_PENDING`, `PROD_DB_REQUIRED_TABLE_MISSING`.

## Audit Append-Only Posture

The enforced database control for the append-only evidence/audit tables is the `tagekyc.deny_append_only_mutation()` trigger function installed by `20260621075836_InitialPostgresPersistence`. It creates `BEFORE UPDATE OR DELETE` triggers on `capture_artifacts`, `evidence_results`, `verification_decisions`, `evidence_packages`, `evidence_manifests`, and `audit_events`, including `tr_audit_events_append_only` for `tagekyc.audit_events`. This control is covered by the Postgres persistence slice tests.

For stronger runtime hardening, deploy with separate identities:

- Use an owner/migrator identity to own the schema and apply migrations.
- Use a non-owner runtime application role for the API service.
- Grant the runtime role only the privileges needed by the application.
- For the append-only tables, the runtime role should have `INSERT`/`SELECT` only and no `UPDATE`, `DELETE`, `ALTER`, `DROP`, or trigger-disable capability.

This is deployment guidance, not auto-applied SQL. Concrete role names and grants belong to the hospital deployment plan. A migration run by the schema owner cannot make that same owner least-privileged; the runtime role split must be provisioned operationally.

## Raw-Export Rule-Table Privilege Gate (TIP-88A)

The raw-export requirement-rule tables `tagekyc.raw_export_requirement_rule_sets` and `tagekyc.raw_export_requirement_rules` (added by `20260711132410_Tip88ARawExportPolicyCatalog`) are migration-seeded and immutable at runtime. Two layers protect them:

- In-DB enforcement, always on and role-independent: the `reject_raw_export_rule_runtime_mutation()` trigger raises on any `INSERT`/`UPDATE`/`DELETE` from any caller. Derivation rules can only change via a new migration.
- Deployment gate, must be provisioned operationally: the production runtime application role MUST have `SELECT` only on both rule tables — no `INSERT`, `UPDATE`, or `DELETE`. Run by the schema owner (adjust the runtime role name to the hospital deployment plan):

  ```sql
  REVOKE INSERT, UPDATE, DELETE ON tagekyc.raw_export_requirement_rule_sets FROM <runtime_role>;
  REVOKE INSERT, UPDATE, DELETE ON tagekyc.raw_export_requirement_rules FROM <runtime_role>;
  GRANT SELECT ON tagekyc.raw_export_requirement_rule_sets, tagekyc.raw_export_requirement_rules TO <runtime_role>;
  ```

Production `/readiness` fails closed with `PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE` (HTTP 503) whenever the runtime principal (`current_user`) holds any mutation privilege on either rule table. A single-role deployment where the runtime identity equals the schema owner will therefore never become ready — this is intentional: provision the SELECT-only runtime role before go-live. Tracked as a P1 deployment gate in `docs/phase1_scope_and_debt_registry_v0_1.md`.

## Raw-Export Control-Plane Role & Bootstrap Gate (TIP-88B1)

TIP-88B1 (migration `20260712133151_Tip88B1RawExportControlPlane`) adds 4 append-only event tables written ONLY via `SECURITY DEFINER` functions, plus a root-authority model. Three layers:

- In-DB enforcement, always on: direct raw-SQL INSERT into `raw_export_grants` / `raw_export_control_authorities` / `raw_export_fulfillments` / `raw_export_policy_lifecycle` is rejected; every append goes through a SD function that stamps the actor from a transaction-local GUC (`SET LOCAL tagekyc.actor_principal_id`), fail-closed on missing/blank/malformed.
- Deployment gate, must be provisioned operationally (the migration creates the roles as `NOLOGIN`; wiring the app to the runtime role is a deployment step):
  - `tagekyc_raw_export_deployer` — owns the SD functions; has INSERT on the 4 event tables. NOT the app's connection role.
  - `tagekyc_runtime` — a least-privilege runtime CAPABILITY role (created `NOLOGIN`): the migration grants it `USAGE` on the schema + `EXECUTE` on the 4 append functions. It does NOT grant it table `SELECT` and it CANNOT be a login principal directly.
  - `tagekyc_raw_export_bootstrapper` — deploy-only: `EXECUTE` on `raw_export_bootstrap_global_authority(...)` to seed the initial root authorities.
  - **The app connects as a LOGIN principal `<app_login_role>` that HOLDS the `tagekyc_runtime` capability** (via role membership/inheritance, or `SET ROLE tagekyc_runtime` per session) — NOT as the deployer/bootstrapper. `current_user` (or the effective role) that `/readiness` and the append functions see must be the runtime capability, must fail the direct-event-table-write privilege check, and must pass the function-ACL check.
  - **Grant the read set the resolver/readiness need (the SD append functions cover writes, but the resolver reads tables directly), with NO write. Grant it DIRECTLY to the `tagekyc_runtime` capability role** so it is the EFFECTIVE role's privilege under BOTH modes — role-inheritance AND `SET ROLE tagekyc_runtime` (a grant to only the app login role is SUSPENDED under `SET ROLE`, causing a generic resolver/readiness failure):
    ```sql
    GRANT SELECT ON
      tagekyc.raw_export_policy_versions, tagekyc.raw_export_policy_requirements,
      tagekyc.raw_export_policy_closures, tagekyc.raw_export_requirement_rule_sets,
      tagekyc.raw_export_requirement_rules,
      tagekyc.raw_export_grants, tagekyc.raw_export_control_authorities,
      tagekyc.raw_export_fulfillments, tagekyc.raw_export_policy_lifecycle
    TO tagekyc_runtime;   -- capability role; no INSERT/UPDATE/DELETE
    ```
    (A missing/ineffective SELECT surfaces as a generic resolver/readiness failure, not one of the five B1 codes below, until a dedicated read-privilege check is added.)
  - Root `GrantAdmin` / `RecorderAuthorityAdmin` / `ActivationAuthority` authorities MUST be bootstrap-seeded with REAL operator principals (never a dev/default) before the control plane is used. Bootstrapping is deploy-time (bootstrapper role), never a runtime command.

Production `/readiness` fails closed (HTTP 503) with any of: `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING` (an authority class has no root), `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT` (a dev/default principal seeded as root), `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE` (runtime principal can write the event tables directly), `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID` (SD/ACL/search_path drift), `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID` (role setup wrong). NOTE: the append actor principal is APPLICATION-ASSERTED, not DB-authenticated — its non-forgeability depends on the runtime/deployer role separation above being provisioned. Tracked as a P1 gate in `docs/phase1_scope_and_debt_registry_v0_1.md`.

## Retention Policy Declaration

Production requires a declared retention window for regulated evidence at `TagEkyc:Retention:RegulatedEvidenceRetentionDays`. The value is supplied by Legal/DPO under the governing Vietnamese legal basis; this runbook intentionally ships no day-count value. `LocalDevEphemeral` has no production retention window.

Startup refuses production mode when the regulated-evidence retention window is missing or invalid. Until automated retention enforcement is built, the declared window is an operational/legal control: record it in deployment configuration, review it with Legal/DPO, and use it for manual retention, export, and decommission decisions.

The application does not auto-enforce retention in this slice. Automated purge remains deferred until the append-only evidence/audit retention mechanism is decided.

## Evidence Record

Record only:

- Date/time.
- Operator.
- Release SHA.
- Migration command/bundle id.
- Migration state: current / pending / failed.
- Sanitized readiness code if failed.

Do not record connection strings, passwords, secret values, file paths, patient data, or raw evidence.
