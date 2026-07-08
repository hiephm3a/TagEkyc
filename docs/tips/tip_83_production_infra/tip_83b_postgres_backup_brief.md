# TIP-83B - Postgres Production + Migrations + Backup/Restore Drill - Planning Brief

Status: v0.4 - **READY_FOR_BUILD** (Codex confirmed after 2 patch rounds). **Dispatch caution:** the DB-readiness gate must register/execute AFTER TIP-83A signing/config validation - current `Program.cs` has `ConfigurePersistence` BEFORE `ConfigureEvidenceSigning`, so putting the DB gate in persistence setup (hosted service) would re-introduce signing-error masking; tests must prove a signing-invalid case returns `PROD_SIGNING_*`, not `PROD_DB_*`. v0.4 closed round-2 (the "fail-closed on paper but leak/mask at runtime" class): **P1-01** each DB-readiness failure mode has its OWN sanitized code (`PROD_DB_UNREACHABLE`/`_PROVIDER_INVALID`/`_MIGRATION_HISTORY_MISSING`/`_MIGRATIONS_PENDING`/`_REQUIRED_TABLE_MISSING`), no raw-exception/secret leak; **P1-02** startup ordering runs config/signing fail-fast BEFORE the DB gate; **P2-01** drill fixture adds caller category/scope/key-alias; **P2-02** pinned runbook artifact paths; **P2-03** fixed the stale recon line. v0.3 had fixed: **P1-01** restore-drill needs a stored synthetic drill-session REQUEST fixture so the dedup-replay is executable (DB holds only the fingerprint); **P1-02** proof-verify needs an explicit signing-key/JWKS CONTINUITY prerequisite (companion key/JWKS backup, not silently assumed); **P1-03** the generic `SecretRefResolver` returns typed context-neutral errors + the CALLER maps them, so P12 keeps `PROD_SIGNING_P12_PASSWORD_SECRET_*` and never leaks `PROD_DB_*`; **P2-01** example/runtime config uses `ConnectionStringSecretRef`; **P2-02** full secret-ref invalid/missing taxonomy (unsupported-scheme/blank/directory/etc.). Baseline: live-fetch current HEAD (`7b7dab3` post TIP-67G; persistence/signing recon facts verified still hold). Second TIP-83 slice: durable Postgres persistence on hospital on-prem infra.
Repo: D:\Task\Remote Signing\TagEkyc (server). Baseline: live-fetch current HEAD (7b7dab3 post TIP-67G at drafting; §0 recon-facts - persistence selection, TIP-80S-I migration, TIP-83A secret-ref pattern - verified still hold). Umbrella: tip_83_planning_brief.md.
Tier: Tier-1 (patient records + evidence/proofs at rest + durability).

## 0. Recon facts to build on
- `TagEkycPersistenceOptions`: Provider = "InMemory" (default) | "Postgres". **[P2-03] Pre-slice state:** raw `ConnectionString` is currently required for Postgres; THIS slice changes that - raw `ConnectionString` becomes dev/test/design-time ONLY, and **production requires `ConnectionStringSecretRef`** (2.2).
- Program.cs persistence selection (~:69-110): if Production && not Postgres => throws; Postgres => EF repositories; InMemory => in-memory singletons. So production already refuses in-memory (confirm + test + strengthen).
- EF migrations exist incl. TIP-80S-I `append_idempotency_records` (composite PK). TIP-80S-L ledger reads existing tables; TIP-80S-I added the idempotency table + boundary.
- Secret-ref pattern established in TIP-83A (`env:`/`file:` resolver) - reuse for the connection string.

## 1. Intent
Make Postgres the enforced, durable production store: connection string from a secret-ref (not plaintext), migrations applied via a controlled step + a startup readiness check, encryption at rest, and a tested restore drill proving records/idempotency/proof-verification survive a restore.

## 2. Scope - ALLOWED
### 2.1 Postgres enforced in production (no in-memory)
- Production MUST use Postgres; in-memory in production => fail-closed at startup (confirm the existing throw, give it a distinct reason code `PROD_PERSISTENCE_INMEMORY_FORBIDDEN`, add a test). In-memory stays for demo/dev only.
### 2.2 Connection string from a secret (no plaintext) - OQ-2 LOCKED
- **LOCKED:** the Postgres connection string comes from `TagEkyc:Persistence:ConnectionStringSecretRef`, resolved via a **GENERIC secret-ref resolver** (refactor TIP-83A's `ProductionTrialP12SecretResolver` into a shared `SecretRefResolver.Resolve("env:<VAR>"|"file:<ABS_PATH>")` that BOTH the P12 password and the DB connection use - do NOT have the DB call `ProductionTrialP12SecretResolver` directly). A hospital secret-store provider may be added behind the same resolver later.
- **[P1-03] The generic resolver returns TYPED/context-NEUTRAL results/errors (e.g. an enum `Invalid`/`Missing` + a redacted source descriptor); the CALLER maps them to context-specific public codes.** Do NOT let the shared resolver emit either domain's prefixed codes. Mapping: P12-password caller keeps `PROD_SIGNING_P12_PASSWORD_SECRET_*` (TIP-83A, unchanged); DB caller emits `PROD_DB_CONNECTION_SECRET_*`. The signing path MUST NEVER surface a `PROD_DB_*` code and vice-versa.
- Production REJECTS a raw plaintext `TagEkyc:Persistence:ConnectionString` => fail-closed `PROD_DB_CONNECTION_PLAINTEXT_FORBIDDEN`.
- Secret-ref error taxonomy (full):
  - `PROD_DB_CONNECTION_SECRET_INVALID` = malformed ref: unsupported scheme (not `env:`/`file:`), blank env-var name, blank file path, relative `file:` path, or a `file:` path that is a DIRECTORY.
  - `PROD_DB_CONNECTION_SECRET_MISSING` = well-formed ref but the target is absent/empty: env var unset, file not found, or resolved content empty/whitespace.
  Logs show source TYPE + a redacted identifier only - never the env value / file path / connection content.
- **Grep scope (P2):** the "no plaintext DB password" grep/test targets PRODUCTION/runtime appsettings + runbook only. Design-time/test local defaults (e.g. `TagEkycDesignTimeDbContextFactory.cs`, test fixtures) are allowed ONLY under test/design-time and MUST never be reachable by production; do not delete them as part of this slice unless chosen (state which).
### 2.3 Migrations - OQ-1 LOCKED (no auto-migrate; fail-serving readiness)
- **LOCKED:** production NEVER auto-migrates on startup (no `Database.Migrate()` in the startup path). Migrations are applied ONLY by an explicit controlled deploy command/bundle.
- **Startup readiness = FAIL-SERVING (not just a /health flag):** the production app MUST refuse to serve traffic if any of the checks fail. The check is **READ-ONLY** (connect + read migration state; NO write/migrate during readiness).
- **[P1-01] Each failure mode has its OWN sanitized code (do NOT throw a raw `NpgsqlException`/EF exception - it leaks host/db/user/path):**
  - DB unreachable / connect fail => `PROD_DB_UNREACHABLE`
  - provider not Npgsql/Postgres => `PROD_DB_PROVIDER_INVALID`
  - `__EFMigrationsHistory` missing => `PROD_DB_MIGRATION_HISTORY_MISSING`
  - pending EF migrations non-empty => `PROD_DB_MIGRATIONS_PENDING`
  - required `append_idempotency_records` table absent => `PROD_DB_REQUIRED_TABLE_MISSING`
  Each maps a caught underlying exception to its code; the **public message + logs contain NO connection string / password / env value / file path / raw exception detail** (redacted only).
- **[P1-02] Startup validation ORDERING (so DB readiness does NOT mask signing/config errors):** config + signing fail-fast validations (TIP-83A: P12/secret/JWKS self-test, config-invalid cases) run BEFORE the DB readiness gate, so a signing-config error surfaces its OWN code (`PROD_SIGNING_*`), not `PROD_DB_UNREACHABLE`. The **TIP-83A signing-production tests MUST NOT be masked** by the new DB gate - either the ordering guarantees signing validation runs first for config-invalid cases, OR those tests provide a valid readiness DB (Postgres fixture). DoD asserts the expected `PROD_SIGNING_*` codes still appear (not replaced by `PROD_DB_*`).
- The readiness check uses EF migrations history / provider metadata only - NO new domain table for TIP-83B (an HTTP `/health/readiness` endpoint, if any, belongs to TIP-83E; this slice = the startup fail-serving gate).
- Provide the exact migration-apply command (EF bundle / `dotnet ef database update` with the correct startup project) in the runbook.
### 2.4 Encryption at rest
- Patient/evidence data + audit at rest encrypted (Postgres TDE if available, or hospital disk/volume encryption). This is a hospital-infra control; the brief documents the requirement + how the deployment satisfies it (owner = hospital IT).
### 2.5 Backup/restore drill (runbook evidence, not a unit test) - FUNCTIONAL steps
- **[P1-01] PRE-DRILL synthetic fixture (in the runbook test pack, NOT from a real customer):** the drill requires a pre-seeded **synthetic drill session** whose exact append request is stored so it can be REPLAYED after restore (the DB holds only the dedup fingerprint, not the raw request - without the stored request the replay step cannot execute). The test pack pins: `sessionId`, the append endpoint, the `Idempotency-Key`, the EXACT sanitized request JSON, the expected minted id, and **[P2-01] the caller boundary needed to replay** - caller category + scope + a non-secret API-key ALIAS (e.g. a drill business-key alias), so the operator knows which credential boundary to use (the actual key value stays in the secret store, never in the runbook). All SYNTHETIC, no PII.
- **[P1-02] PREREQUISITE - signing-key / JWKS continuity (a DB restore does NOT restore key material):** the restored environment MUST have key continuity for proof-verify to be meaningful - EITHER the SAME current P12 key (+ its password secret) OR a restored `Jwks:PreviousKeysFile` registry containing the proof's old `kid`/`fingerprint`. The P12/secret/JWKS-previous-file are a **COMPANION backup item** (backed up + restored ALONGSIDE the DB), NOT silently assumed. Record `kid`/`fingerprint` before backup and after restore; they must match a JWKS entry.
- Documented backup schedule + a TESTED restore drill: restore a backup into a CLEAN database, start the server (with the companion key/JWKS continuity above), and run a FUNCTIONAL smoke (not just row counts):
  1. Restore a backup containing the synthetic drill session (completed, signed proof) + its `append_idempotency_records` rows.
  2. Migrations current + `append_idempotency_records` table exists.
  3. **Idempotency replay (using the stored fixture request):** re-send the pinned same-key/same-payload append -> assert `Deduplicated=true` + the SAME minted id (proves the TIP-80S-I table restored + functional); optionally same-key/different-payload -> 409.
  4. **Proof verify:** fetch the verification-view + verify its JWS by `kid` against the JWKS present in the restored environment (current or previous key per the continuity prerequisite); assert the `kid`/`fingerprint` match.
- Restore-drill record template: date, operator, backup id, migration-state, dedup-replay result, proof `kid`+fingerprint verify pass, **key-continuity source (current-P12 vs restored-previous-JWKS)** - record only opaque ids/hash/fingerprint, NO subjectRef/PII/connection-string/secret.
### 2.6 Backup security
- Backups encrypted + access-controlled; backup location documented; retention of backups per Legal/DPO (input, not hardcoded - umbrella OQ-3).

## 3. STOP / NOT
- NO in-memory persistence in production (fail-closed).
- NO plaintext DB connection string/password in appsettings/repo/logs; connection secret via secret-ref only; never logged.
- NO silent auto-migrate-on-startup in production by default (controlled apply + readiness check).
- NO raw biometric stored (verify_and_discard - TIP-82; not this slice's concern but restated).
- NO eKYC/proof/decision-basis logic change; NO schema change beyond applying existing migrations (this slice adds no new domain tables).
- NO backup containing plaintext secrets outside the encrypted-backup control.

## 4. Definition of Done
- [ ] Production forces Postgres (via the existing Program.cs production-mode source); tests: Production+InMemory => `PROD_PERSISTENCE_INMEMORY_FORBIDDEN`, Production+Provider-missing/default => same, demo/dev+InMemory allowed.
- [ ] Generic `SecretRefResolver` (refactored from TIP-83A) used by both P12 + DB; connection via `ConnectionStringSecretRef`; raw production `ConnectionString` => `PROD_DB_CONNECTION_PLAINTEXT_FORBIDDEN`; taxonomy (2.2): unsupported-scheme/blank-name/blank-path/relative/directory => `PROD_DB_CONNECTION_SECRET_INVALID`; unset-env/file-not-found/empty-content => `PROD_DB_CONNECTION_SECRET_MISSING`; logs source-type/redacted only (tests). Production/runtime appsettings grep = no DB password (design-time/test defaults scoped out).
- [ ] **[P1-03] Resolver taxonomy preserved:** the generic resolver returns typed context-neutral results; caller maps them; **TIP-83A signing tests still green + P12 path still emits `PROD_SIGNING_P12_PASSWORD_SECRET_*`, NEVER a `PROD_DB_*` code** (regression test both directions).
- [ ] **[P2-01] Example/runtime config:** `config/appsettings.example.json` (and any production/runtime appsettings) use `ConnectionStringSecretRef`, NOT a raw `ConnectionString`; any remaining raw `ConnectionString` is clearly dev/design-time-scoped and a production run REJECTS it (`PROD_DB_CONNECTION_PLAINTEXT_FORBIDDEN`, test).
- [ ] Migrations: no production auto-migrate; startup read-only fail-serving readiness => refuses to serve. **[P1-01]** each failure mode => its own sanitized code (`PROD_DB_UNREACHABLE`/`PROD_DB_PROVIDER_INVALID`/`PROD_DB_MIGRATION_HISTORY_MISSING`/`PROD_DB_MIGRATIONS_PENDING`/`PROD_DB_REQUIRED_TABLE_MISSING`), tested per mode; public message + logs contain NO conn-string/password/env-value/file-path/raw-exception (assert). Controlled apply command in runbook.
- [ ] **[P1-02] Signing not masked by DB readiness:** config/signing fail-fast runs before the DB gate (or TIP-83A tests use a valid readiness DB); test asserts a signing-config-invalid case still surfaces its `PROD_SIGNING_*` code, NOT `PROD_DB_UNREACHABLE`.
- [ ] **Update the TIP-83A production integration tests** that set `TagEkyc:Persistence:ConnectionString` to use `ConnectionStringSecretRef` (this slice changes the production connection contract).
- [ ] Encryption-at-rest requirement documented + deployment method (hospital IT owner).
- [ ] Backup schedule + restore-drill runbook + record template, committed at **[P2-02]** pinned paths: `docs/deployment/hospital_trial/postgres_migration_runbook.md`, `.../postgres_backup_restore_drill.md`, `.../postgres_restore_drill_record_template.md`. **[P1-01]** a synthetic drill-session request fixture (sessionId/endpoint/Idempotency-Key/exact sanitized JSON/expected-id + caller category/scope/key-alias) is stored so the dedup-replay step is executable. **[P1-02]** the key/JWKS-continuity prerequisite (companion key/JWKS backup; current-P12 or restored-previous-JWKS) is documented + the record captures the continuity source + kid/fingerprint match. No secrets/PII in the record.
- [ ] dotnet test green (env-fixture reds run isolated per the shared-infra note); no eKYC logic change; no secrets committed.

## 5. Review tier & attacks
Tier-1. (a) in-memory reachable in production? (b) plaintext connection string/password anywhere (appsettings/logs/backup)? (c) silent auto-migrate in production? (d) startup readiness actually catches a pending migration? (e) does the restore drill actually prove idempotency + proof-verify survive, or just row presence? (f) backups unencrypted / secrets in backup? (g) any eKYC/schema change beyond applying migrations? (h) encryption-at-rest owner + method defined? (i) **can the dedup-replay step actually EXECUTE (stored fixture request) or is it a slogan?** (j) **does proof-verify have real key/JWKS continuity or silently assume the restored env has the key?** (k) **does the generic resolver cross-contaminate P12/DB codes (a `PROD_DB_*` leaking into the signing path)?** (l) does the example/runtime config still teach a raw `ConnectionString`?

## 6. Open questions - RESOLVED
- OQ-1: RESOLVED = no production auto-migrate; explicit controlled apply command/bundle; startup read-only fail-serving readiness; pending => `PROD_DB_MIGRATIONS_PENDING` (2.3).
- OQ-2: RESOLVED = `TagEkyc:Persistence:ConnectionStringSecretRef` via a generic `env:`/`file:` resolver; raw production `ConnectionString` rejected; hospital secret store = later adapter behind the same resolver (2.2).
- OQ-3: RESOLVED = backup retention is Legal/DPO input; build provides config/runbook placeholders, no hardcoded 7/30/90.

## 7. Dependencies / companions
- Hospital IT: Postgres host, encryption-at-rest, backup storage + schedule owner, secret store.
- TIP-83A signing key: the restore drill's proof-verify uses it.
- TIP-80S-I migration `append_idempotency_records` must be in the applied set.
- Umbrella: OQ-2 (Windows Service + hospital Postgres) + OQ-3 (retention) already resolved there.

## STOP / RRI (Homeowner)
STOP + ask if the build: allows in-memory in production; puts a plaintext DB connection/password in appsettings/logs/backup; silently auto-migrates production; leaves backups unencrypted or secrets in a backup; hardcodes Legal-owned backup retention; or changes eKYC/proof logic.
