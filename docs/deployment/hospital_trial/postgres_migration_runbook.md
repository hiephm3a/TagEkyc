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

## Evidence Record

Record only:

- Date/time.
- Operator.
- Release SHA.
- Migration command/bundle id.
- Migration state: current / pending / failed.
- Sanitized readiness code if failed.

Do not record connection strings, passwords, secret values, file paths, patient data, or raw evidence.
