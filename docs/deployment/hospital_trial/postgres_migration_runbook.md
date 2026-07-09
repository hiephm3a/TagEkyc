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
