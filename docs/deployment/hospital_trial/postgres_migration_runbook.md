# Hospital Trial Postgres Migration Runbook

## Purpose

Apply TagEkyc EF migrations as an explicit deploy step. Production startup never runs `Database.Migrate()`; it only performs a read-only readiness gate.

## Preconditions

- PostgreSQL 16 or later. E3 requires PostgreSQL 16 membership-edge options so
  the runtime membership can inherit capability while explicitly disallowing
  SET ROLE. Readiness treats an older server as an invalid deployment posture.
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
- Deployment gate after TIP-88B1-E3: the production runtime role has no direct table privileges; the E3 eligibility capability performs the required read.

  ```sql
  REVOKE SELECT, INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER ON
    tagekyc.raw_export_requirement_rule_sets,
    tagekyc.raw_export_requirement_rules
  FROM tagekyc_runtime;
  ```

Production `/readiness` retains the rule-table mutation code and E3 additionally returns `PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE` for any direct runtime privilege on these tables. A single-role deployment where runtime equals the schema owner cannot become ready.

## Raw-Export Control-Plane Role & Bootstrap Gate (TIP-88B1)

TIP-88B1 (migration `20260712133151_Tip88B1RawExportControlPlane`) adds 4 append-only event tables written ONLY via `SECURITY DEFINER` functions, plus a root-authority model. Three layers:

- In-DB enforcement, always on: direct raw-SQL INSERT into `raw_export_grants` / `raw_export_control_authorities` / `raw_export_fulfillments` / `raw_export_policy_lifecycle` is rejected; every append goes through a SD function that stamps the actor from a transaction-local GUC (`SET LOCAL tagekyc.actor_principal_id`), fail-closed on missing/blank/malformed.
- Deployment gate, must be provisioned operationally (the migration creates the roles as `NOLOGIN`; wiring the app to the runtime role is a deployment step):
  - `tagekyc_raw_export_deployer` — owns the SD functions; has INSERT on the 4 event tables. NOT the app's connection role.
  - `tagekyc_runtime` — a least-privilege runtime CAPABILITY role (created `NOLOGIN`): the migration grants it `USAGE` on the schema + `EXECUTE` on the 4 append functions. It does NOT grant it table `SELECT` and it CANNOT be a login principal directly.
  - `tagekyc_raw_export_bootstrapper` — deploy-only: `EXECUTE` on `raw_export_bootstrap_global_authority(...)` to seed the initial root authorities.
  - **The app connects as a dedicated inheriting LOGIN principal `<app_login_role>` whose only transitive role membership is `tagekyc_runtime`** — NOT as the deployer/bootstrapper. `SET ROLE` is not supported for the production E3 readiness connection: `session_user` must equal `current_user`. Exactly one `pg_auth_members` row may exist across all grantors for the login-to-runtime pair; that row must have no admin option, must inherit, and must not permit SET. The effective login must fail the direct-event-table-write privilege check and pass the function/owner-backing-read checks.
  - The former direct resolver/readiness table-SELECT set is obsolete after TIP-88B1-E3. Do not restore it:
    ```sql
    -- Obsolete after TIP-88B1-E3: do not grant direct table SELECT to tagekyc_runtime.
    ```
    TIP-88B1-E3 supersedes the preceding legacy guidance: the runtime role now reads eligibility and root health only through the three E3 capabilities documented below.
  - Root `GrantAdmin` / `RecorderAuthorityAdmin` / `ActivationAuthority` authorities MUST be bootstrap-seeded with REAL operator principals (never a dev/default) before the control plane is used. Bootstrapping is deploy-time (bootstrapper role), never a runtime command.

Production `/readiness` fails closed (HTTP 503) with any of: `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING` (an authority class has no root), `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT` (a dev/default principal seeded as root), `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE` (runtime principal can write the event tables directly), `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID` (SD/ACL/search_path drift), `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID` (role setup wrong). NOTE: the append actor principal is APPLICATION-ASSERTED, not DB-authenticated — its non-forgeability depends on the runtime/deployer role separation above being provisioned. Tracked as a P1 gate in `docs/phase1_scope_and_debt_registry_v0_1.md`.

## Raw-Export Subject-Consent Role & Bootstrap Gate (TIP-88B2)

TIP-88B2 (subject export consent, landed `8cd52a3`) adds three append-only tables — `raw_export_subject_consent_events`, `raw_export_subject_consent_classes`, `raw_export_subject_consent_authorities` — written ONLY through `SECURITY DEFINER` functions owned by the non-login `tagekyc_raw_export_deployer` with a fixed `search_path=pg_catalog`. In-DB enforcement is always on: UPDATE/DELETE are denied by trigger on all three tables; a direct INSERT is rejected unless it comes through the intended append path; the actor principal comes from the transaction-local GUC `SET LOCAL tagekyc.actor_principal_id`, fail-closed; and class child rows may only be written in the SAME transaction as their `Granted` parent (xmin guard).

**Deployment gate (operational, NOT an EF-migration artifact):**
  - The app must connect as the dedicated inheriting LOGIN described above. Its complete transitive reachable-role set is exactly `{tagekyc_runtime}`; `session_user == current_user`; deployer/bootstrapper must not be reachable directly or indirectly.
  - `tagekyc_runtime` must hold `EXECUTE` on **exactly three** functions: `raw_export_resolve_subject_consent_for_authorization`, `raw_export_append_subject_consent_granted`, `raw_export_append_subject_consent_withdrawn`. It must NOT hold EXECUTE on the authority-management function or on the bare hash / lock-key / session-lock helpers.
  - `tagekyc_runtime` must hold **none** of the seven table privileges (`SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER`) on the three consent tables or `tagekyc.verification_sessions`. The lock, resolver, and granted-consent completeness check stay inside controlled SECURITY DEFINER paths.
  - **Consent recorder/withdrawer authorities MUST be bootstrap-seeded by the DEPLOYMENT role before any consent write.** `AuthorityType` is `SubjectConsentRecorder` (governs consent `Granted`) or `SubjectConsentWithdrawer` (governs consent `Withdrawn`). Runtime has no EXECUTE on authority grant/revoke and cannot self-grant. A consent write by a principal with no CURRENT effective authority is denied.
  - Authority state follows latest-event-overall with **no fallback**: an expired or revoked latest event makes the authority INACTIVE, and only a LATER `Granted` reactivates it. An expiring authority therefore silently stops consent capture — monitor `ValidUntilUtc` on seeded authorities.

Production `/readiness` fails closed (HTTP 503) on `PROD_RAW_EXPORT_SUBJECT_CONSENT_FUNCTION_ACL_INVALID` / `..._TABLE_MUTATION_PRIVILEGE` for any drift in the B2 function manifest — exact signature, `prosecdef`, non-login owner, exact `search_path=pg_catalog`, no PUBLIC EXECUTE, and the per-function runtime EXECUTE expectation — or if the runtime principal can mutate a consent table directly.

**Operational note on withdrawal:** consent `Granted` requires the verification session to be `Completed`, but consent `Withdrawn` deliberately does NOT — a subject can withdraw after the session reaches `Expired`, `Cancelled` or `TechnicalTerminal`. Do not add any deployment-level guard that blocks withdrawal on session state; doing so would trap consent in a non-revocable state. Tracked as a P1 gate in `docs/phase1_scope_and_debt_registry_v0_1.md`.

## Resolver Runtime Read Boundary (TIP-88B1-E3)

**Deployment hold (2026-07-24):** the E3 implementation is present only in the
working tree and is undergoing the post-build remediation recorded in
`docs/tips/tip_88b1e3_resolver_runtime_read_access/tip_88b1e3_remediation_plan_v3.md`.
Do not deploy or close out E3 until that document's identity, transitive-role,
backing-read, deterministic-ACL, fulfillment-materialization, role-precedence,
and B2 constraint-mode gates are implemented and green.

TIP-88B1-E3 supersedes the older SELECT-only guidance above. Authorization and readiness are capability-only: `tagekyc_runtime` must have zero of `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `REFERENCES`, and `TRIGGER` on all fourteen protected tables (the four policy-catalog tables, both requirement-rule tables, four B1 event tables, `verification_sessions`, and the three B2 consent tables).

Keep every landed B1/B2/B3 function grant and add these E3 grants:

```sql
GRANT EXECUTE ON FUNCTION
  tagekyc.raw_export_read_authorization_eligibility_inputs(uuid,uuid,integer),
  tagekyc.raw_export_read_authorization_policy_inputs(uuid,uuid,integer),
  tagekyc.raw_export_control_plane_root_health()
TO tagekyc_runtime;

GRANT SELECT ON tagekyc.raw_export_policy_allowed_classes
TO tagekyc_raw_export_deployer;

REVOKE SELECT ON
  tagekyc.verification_sessions,
  tagekyc.raw_export_subject_consent_authorities,
  tagekyc.raw_export_subject_consent_events,
  tagekyc.raw_export_subject_consent_classes
FROM tagekyc_runtime;
```

The B2 granted-consent append capability forces its deferred class-completeness check while still inside the `SECURITY DEFINER` boundary; do not compensate by restoring runtime SELECT. `/readiness` returns HTTP 503 with `PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE` for any forbidden table-privilege cell and validates the exact E3 function bodies, owners, fixed search paths, and ACLs. Root health returns only a bounded boolean and status code, never authority or principal identifiers.

The target remediated posture additionally pins the function-owner backing-read
manifest. Eligibility reads policy versions, requirement rule sets, closures,
grants, lifecycle, policy requirements, and fulfillments. Policy projection reads
policy versions, closures, and allowed classes. Root-health reads control
authorities. Deployer write/ownership capability must equal the accepted pre-E3
landed manifest; E3 neither adds a new write capability nor removes a capability
required by landed command functions.

Each E3 projection function has exactly one explicit non-owner ACL row:
`grantor=tagekyc_raw_export_deployer`,
`grantee=tagekyc_runtime`, `privilege=EXECUTE`,
`is_grantable=false`. Owner authority is checked separately. An otherwise
identical runtime grant from another grantor is ACL drift and readiness must
reject it.

The production topology remains one hospital per database. Hospital IT must
provision a dedicated inheriting runtime LOGIN whose transitive reachable-role
set is exactly `{tagekyc_runtime}`. Exactly one `pg_auth_members` row may exist
across all grantors for that pair; on PostgreSQL 16 it has
`admin_option=false`, `inherit_option=true`, and `set_option=false`. The LOGIN is
`INHERIT`, `NOSUPERUSER`, `NOCREATEDB`, `NOCREATEROLE`, `NOREPLICATION`, and
`NOBYPASSRLS`. Rotate its secret and audit break-glass use. This boundary reduces
accidental/direct SQL reach; it does not claim resistance to compromise of the
full backend or database-owner credentials, and it does not add shared-database
multi-hospital support.

`session_user == current_user` detects an unsupported active `SET ROLE`; it is
not proof of the originally authenticated identity against a malicious
superuser using `SET SESSION AUTHORIZATION`. The production application login is
`NOSUPERUSER`. Superuser connectivity is operationally forbidden except through
an audited break-glass procedure.

## Raw-Export Permit-TTL Bounds Config (TIP-88A-E2)

TIP-88A-E2 (committed local `09c9359`) adds a per-policy-version `PermitTtlSeconds` (the raw-export permit lifetime) plus app-config BOUNDS that constrain what TTL a policy version may declare. The TTL VALUE lives on the approved policy version (immutable, versioned); the BOUNDS are operational config, changeable between deploy/restart (NOT hot-reloaded).

**Config keys** (section `TagEkyc:RawExport`):
- `TagEkyc:RawExport:PermitTtlMinSeconds`
- `TagEkyc:RawExport:PermitTtlMaxSeconds`

**Semantics:** both keys absent => default `[60, 900]`; exactly one absent, malformed/non-integer/overflow, `min <= 0`, `max < min`, or `max > 3600` (the absolute maximum) => INVALID. On an INVALID bounds state the process stays up but `/readiness` fails closed (HTTP 503) with `PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID`, and all raw-export policy write commands (add-version / catalog-approve) fail closed. Bounds are resolved ONCE at startup into an immutable state; change requires a restart. The readiness payload is sanitized (it does not echo the configured min/max).

**Legacy disposition:** a policy version created before E2 has NULL `PermitTtlSeconds`. Existing `CatalogApproved` NULL rows remain readable but TIP-88B3 will reject them at authorization — publish a new policy version with a valid in-bounds TTL before raw-export use. Legacy `Draft` NULL rows cannot be catalog-approved (closure-completeness rejects); abandon and create a new version. No backfill, no in-place mutation.

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
