# TIP-88A-E2 Policy Permit TTL Field - Planning Brief

Status: READY_FOR_BUILD — final confirmation passed (Homeowner 2026-07-20). BUILD-LOCKED: build to THIS text; any scope change needs a new Homeowner lock. **Additive schema extension to the LANDED TIP-88A policy catalog.** Adds an immutable per-`(PolicyId, PolicyVersion)` `PermitTtlSeconds`, the app-config bounds that constrain it, and ONE Homeowner-authorized additive extension to the 88A CatalogApproved closure-completeness trigger. Prerequisite for TIP-88B3 gate-3. Homeowner-authorized additive scope into landed 88A.
Grounded on: HEAD `4b25e2f`. Baseline resolved at dispatch.
Date: 2026-07-20

## 0. Grounding (on-code census @ 4b25e2f)
- **Domain** `RawExportPolicyVersion` ([`RawExportPolicyCatalog.cs:60`](src/TagEkyc.Domain/RawExportPolicyCatalog.cs:60)); **EF row** `RawExportPolicyVersionRow` ([`Entities/RawExportPolicyVersionRow.cs:3`](src/TagEkyc.Infrastructure/Persistence/Entities/RawExportPolicyVersionRow.cs:3)); **table** `raw_export_policy_versions` (88A migration `20260711132410`, PK `(PolicyId, PolicyVersion)`).
- **Write path.** `AddRawExportPolicyVersionCommand` ([`RepositoryPorts.cs:131`](src/TagEkyc.Application/Ports/RepositoryPorts.cs:131)) -> `AddVersionAsync` (Draft) -> `CatalogApproveAsync`/`AbandonDraftAsync`. Impl `EfRawExportPolicyRepository` (cmd->row `:36`, row->domain `:225`).
- **Immutability enforced:** `tr_raw_export_policy_versions_append_only` (BEFORE UPDATE OR DELETE -> `deny_append_only_mutation`, 88A `:418`) — rows immutable, so TTL can only change via a new version.
- **Closure-completeness trigger:** `tr_raw_export_policy_closures_insert_guard` (BEFORE INSERT ON `raw_export_policy_closures`, 88A `:446`); its function branches on `NEW."ClosureType" <> 'CatalogApproved'` (`:330`). This is the ONE function E2 extends (2.2).
- **Config precedent:** `RetentionOptions` (`src/TagEkyc.Infrastructure/Retention/RetentionOptions.cs`, SectionName `TagEkyc:Retention`). Readiness/config test precedent: `Tip83E3RetentionConfigTests.cs`. Last pre-E2 migration id: `20260720022629_Tip88B2SubjectExportConsent`.
- **No 88A ArchTest exists.** 88A integration tests: `Tip88ARawExportPolicyCatalogTests.cs`.

## 1. Intent
Add an immutable `PermitTtlSeconds` to the 88A policy version + app-config bounds, so TIP-88B3 can derive the permit-lifetime term from the exact approved policy version. TTL is a DURATION, not an allow/deny flag.

## 2. Scope - ALLOWED

### 2.1 The field + the NOT-VALID CHECK (raw-SQL owned)
- Add `int? PermitTtlSeconds` as a **trailing** field on `RawExportPolicyVersion` (nullable domain/DB — additive, no backfill).
- **EF maps ONLY the nullable column** on `RawExportPolicyVersionRow` (`.IsRequired(false)`); EF must NOT model the CHECK via `HasCheckConstraint`, and Designer/ModelSnapshot must NOT represent it as a normal validated CHECK (a future EF model diff must not be primed to recreate a validated check).
- **The constraint is RAW-SQL migration-owned** in the NEW E2 migration:
  ```
  ALTER TABLE tagekyc.raw_export_policy_versions
    ADD CONSTRAINT "CK_raw_export_policy_versions_PermitTtlSeconds"
    CHECK ("PermitTtlSeconds" IS NOT NULL AND "PermitTtlSeconds" > 0) NOT VALID;
  ```
  `Down()` drops the constraint (then the column). A schema-assert test reads `pg_constraint` and proves `convalidated = false` (pre-E2 rows never validated; every post-E2 INSERT/UPDATE checked; no backfill).

### 2.2 ONE additive 88A trigger extension (the ONLY landed-trigger change permitted)
- In the E2 migration, `CREATE OR REPLACE` the CatalogApproved closure-completeness function so a **NEW `CatalogApproved` closure requires the target policy version's stored `PermitTtlSeconds` to be non-null AND > 0** — else `RAISE EXCEPTION 'RAW_EXPORT_POLICY_PERMIT_TTL_INVALID'` (bare, matching the landed 88A convention — verified on-code: the 88A closure function uses bare `RAISE EXCEPTION` with ZERO ERRCODE, so SQLSTATE is **`P0001`** and `MessageText` is the literal string), appending no closure.
- **The DB trigger enforces ONLY structural completeness (non-null + positive). It CANNOT and MUST NOT enforce config bounds** (bounds are app config, 2.4). Bounds are enforced app-side (2.3) and re-validated by B3 (2.6).
- **Semantic preservation (NOT byte-for-byte — this is a `CREATE OR REPLACE`):** every existing branch and error surface of the function is preserved unchanged (Abandoned branch, version-chain, allowed-class + requirement completeness, bound-rule-set); the ONLY semantic addition is the TTL block. A normalized pre/post function-body diff assertion (5.x) proves the TTL block is the sole semantic addition. Already-approved legacy rows are untouched (the trigger fires only on NEW closure INSERTs).
- This narrow allowance REPLACES the blanket "no 88A trigger change" STOP.

### 2.3 Validation paths (application; bounds via the single resolver 2.4)
- **`AddVersionAsync`:** `AddRawExportPolicyVersionCommand` gains a **required trailing `int PermitTtlSeconds`**; reject (before appending the draft) if outside the effective bounds — stable domain code `RAW_EXPORT_POLICY_PERMIT_TTL_INVALID`.
- **`CatalogApproveAsync`:** reload the stored TTL for the exact version; validate it against the **immutable resolved-bounds state injected into the current service-provider instance** (NOT an `IConfiguration` reload — no command parses/reloads config); reject null/out-of-bounds (append no closure). The DB trigger (2.2) is the structural backstop; the app enforces the config bounds.
- **`AbandonDraftAsync`:** NO TTL/bounds validation.
- All app-side rejections use the same stable domain code `RAW_EXPORT_POLICY_PERMIT_TTL_INVALID` as the DB trigger.

### 2.4 Config / bounds — ONE resolver, immutable resolved state
- One options+resolver type `RawExportPermitTtlOptions` at **`src/TagEkyc.Infrastructure/RawExport/RawExportPermitTtlOptions.cs`**, `SectionName = "TagEkyc:RawExport"`, keys `PermitTtlMinSeconds` / `PermitTtlMaxSeconds`, const `AbsoluteMaxPermitTtlSeconds = 3600` (Homeowner-decided).
- **Immutable resolved state, resolved ONCE per service-provider / application startup** into either `Valid(MinSeconds, MaxSeconds)` or `Invalid`. The SAME resolved state instance is consumed by readiness (2.5), `AddVersionAsync`, and `CatalogApproveAsync`. Pin: **no per-command parsing of `IConfiguration`; no duplicated parser/default/range logic** (in `Program`/`ReadinessEndpoint`/repository); **no `IOptionsMonitor`; no live hot reload.** Config changes take effect only after restart / rebuilding the service provider.
- Effective-bounds rules (evaluated once, at resolution): **both absent => `Valid([60, 900])`**; **exactly one absent => INVALID**; malformed/non-integer/overflow => INVALID; `min <= 0` => INVALID; `max < min` => INVALID; `max > 3600` => INVALID.

### 2.5 Readiness (config-only; readiness-path, not startup fail-fast)
- An `Invalid` resolved state **keeps the process alive** so `/readiness` can return **HTTP 503 `PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID`**, while ALL policy write commands (`AddVersionAsync`/`CatalogApproveAsync`) fail closed. Do NOT use `ValidateOnStart()` in a way that crashes startup before `/readiness` can report the code.
- E2 readiness validates the **bounds config ONLY**; it MUST NOT scan historical policy rows (per-policy re-validation is B3's, 2.6).
- **Sanitized:** the readiness payload exposes neither the raw configured min/max values nor any binder/parser exception text.

### 2.6 B3 handoff (documented here; BUILT in TIP-88B3)
**Ownership split (pinned):** E2 readiness validates the bounds CONFIGURATION only. TIP-88B3 AUTHORIZATION validates the TTL for the EXACT selected `(PolicyId, PolicyVersion)` at attempt time — re-validating it against the current effective bounds and DENYING that attempt if null or out-of-bounds. **B3 does NOT globally scan every historical policy version, and a null/out-of-bounds historical row does NOT automatically fail global readiness.** Any stored-policy readiness scan is DEFERRED to the B3 rewrite and would require a deterministic configured policy set. B3 binds `PolicyPermitTtlSeconds` into decision/permit evidence; computes `PermitExpiresAtUtc = min(transaction_timestamp() + PolicyPermitTtlSeconds, finite consent expiry, finite fulfillment expiry)`; treats config ONLY as bounds; never mutates/backfills a policy row.

## 3. STOP / NOT
- NO change to 88A eligibility/activation/closure semantics beyond the single 2.2 trigger extension.
- NO EF-modeled CHECK for the TTL constraint (raw-SQL owned, 2.1); NO `[min,max]` in any DB CHECK; NO backfill; NO in-place TTL update; NO simulating legacy-NULL by inserting NULL after E2 (§5).
- NO editing the landed 88A migration — column + NOT-VALID CHECK + trigger `CREATE OR REPLACE` all live in a NEW E2 migration `.cs` + its Designer + the shared ModelSnapshot.
- NO duplicated bounds parsing outside the 2.4 resolver; NO startup crash that pre-empts the readiness code.
- NO change to 88B1/88B2 tables/ports/resolvers; NO TIP-88B3 authorization/permit/expiry logic; NO raw-byte path; NO ACL widening on the policy table.
- Any file beyond the ALLOWLIST (§7) = STOP and report.

## 4. Locked invariants
Immutable per-`(PolicyId, PolicyVersion)` duration; nullable schema (additive) with a RAW-SQL `NOT VALID` positive-non-null CHECK (legacy NULL preserved, post-E2 writes checked); REQUIRED + config-bounds-validated at `AddVersionAsync`, re-validated at `CatalogApproveAsync`, not at `AbandonDraftAsync`; a NEW CatalogApproved closure requires non-null positive TTL via the one authorized trigger extension (structural only, NOT bounds); bounds are app config resolved in ONE type (default [60,900], absolute max 3600, one-absent=>invalid, invalid=>readiness 503, no hot reload, sanitized); inert to all other 88A semantics (semantic-preserving `CREATE OR REPLACE`); rollback restores the exact pre-E2 function body; B3 owns authorization-time exact selected policy/version TTL validation; any stored-policy readiness scan is deferred to the B3 rewrite and must use a deterministic configured policy set.

## 5. Verify gates
- **88A semantics unchanged (explicit per-field, independent expected values, no whole-record equality):** full 88A suite green; closure/approval lifecycle, version-chain, allowed-classes, requirements, Abandoned identical.
- **Function-body semantic-preservation:** a normalized (whitespace/format-insensitive) pre/post diff of the closure-completeness function body proves the TTL block is the ONLY semantic addition; every pre-existing branch/error surface is present unchanged.
- **APPLICATION-GUARD tests (via the repository, config-bounds enforcement):** these prove the app layer, NOT the DB trigger.
  - `AddVersionAsync` rejects an out-of-bounds TTL BEFORE appending a Draft.
  - `CatalogApproveAsync` rejects (i) a legacy Draft with NULL TTL created pre-E2 then approved post-E2, AND (ii) a Draft created under bounds/config A then approved under bounds/config B where its TTL is now out-of-bounds.
  - Each asserts the stable domain code `RAW_EXPORT_POLICY_PERMIT_TTL_INVALID`, asserts NO closure row was appended, and asserts NO generic-exception masking (the specific code, not a bare `Exception`).
- **DB-TRIGGER defense-in-depth test (raw SQL, structural enforcement — a DIFFERENT layer, do not conflate):**
  - prepare a Draft satisfying EVERY existing CatalogApproved completeness condition EXCEPT TTL;
  - BYPASS `CatalogApproveAsync` and issue a direct privileged SQL `INSERT` into `raw_export_policy_closures`;
  - assert `SqlState == "P0001"` (bare `RAISE EXCEPTION`; unless the migration explicitly sets a different ERRCODE, which it does NOT) AND `MessageText == "RAW_EXPORT_POLICY_PERMIT_TTL_INVALID"`;
  - assert NO closure row was appended; prove all pre-existing completeness requirements pass so the TTL branch is provably the reached branch.
- **CHECK-constraint negative (schema layer):** a direct INSERT into `raw_export_policy_versions` with NULL TTL -> assert `SqlState == "23514"` (check_violation) AND the violated constraint name is exactly `CK_raw_export_policy_versions_PermitTtlSeconds`.
- **Legacy fixture (EXACT mechanism — test-only raw SQL compatible with the pre-E2 schema; the current post-E2 repository code is NEVER called against a pre-E2 schema):**
  1. `migrator.MigrateAsync("20260720022629_Tip88B2SubjectExportConsent")` (pre-E2 schema);
  2. via test-only raw SQL, create one COMPLETE `CatalogApproved` policy version AND one COMPLETE `Draft` version — valid allowed-classes, requirements, and bound rule-set state (so that AFTER E2, TTL is the ONLY missing completeness input);
  3. apply E2;
  4. both legacy rows load with `PermitTtlSeconds` NULL;
  5. a new direct version INSERT with NULL TTL -> rejected by the NOT-VALID CHECK (23514, exact name);
  6. `CatalogApprove` of the legacy Draft-null row -> rejected by the closure-completeness extension (with the exact message);
  7. the legacy CatalogApproved-null row remains readable / null;
  8. a normal new version with a valid TTL is created AND approved;
  9. **rollback function-restoration (schema-compatible harness — BLOCKING contract).** `Down()` order is FIXED: (1) restore the EXACT pre-E2 closure-completeness function definition from baseline `4b25e2f`; (2) drop `CK_raw_export_policy_versions_PermitTtlSeconds`; (3) drop the `PermitTtlSeconds` column. The rolled-back validation **MUST NOT use the current E2 EF model / repository** — the compiled model expects `PermitTtlSeconds` while the rolled-back `raw_export_policy_versions` no longer has that column. Required sequence:
     - apply E2 -> rollback E2;
     - WHILE STILL ROLLED BACK, using ONLY a **pre-E2-compatible test-only raw-SQL harness** (never the current repository/EF model against the rolled-back schema):
       - assert the `PermitTtlSeconds` column is ABSENT;
       - assert `CK_raw_export_policy_versions_PermitTtlSeconds` is ABSENT;
       - compare the restored closure function **body AND metadata** (regprocedure signature, owner, language, volatility, strictness, `prosecdef`, `proconfig`/search_path, EXECUTE ACL) to baseline `4b25e2f` (body-only is insufficient);
       - **use THREE SEPARATE policy versions / Drafts (never reuse one `(PolicyId, PolicyVersion)` across closure assertions — `raw_export_policy_closures` PK is `(PolicyId, PolicyVersion)`, so a version admits exactly ONE closure, approve-XOR-abandon):** Draft A -> direct-insert a valid `CatalogApproved` closure, prove it SUCCEEDS; Draft B -> direct-insert an `Abandoned` closure, prove the unchanged branch SUCCEEDS; Draft C (completeness-NEGATIVE) -> attempt a `CatalogApproved` closure and assert its EXACT original error surface;
     - reapply E2;
     - **only AFTER reapply**, run the full current 88A EF/repository suite.
     No separate pre-E2 binary is required or authorized.
  10. Live schema (post-E2): the schema-assert proves the CHECK is present, `NOT VALID` (`convalidated=false`), and NOT EF-modeled; the closure function body+metadata match the intended E2 definition.
- **EF/model agreement is SEPARATE from migration-owned schema assertions (do not conflate):** DbContext, Designer and ModelSnapshot agree ONLY on the nullable `PermitTtlSeconds` column/property. The migration-OWNED artifacts — the raw-SQL `NOT VALID` CHECK (exists + `convalidated=false` + not EF-modeled/recreated) and the closure function body+metadata (match intended E2 def; rollback restores baseline body+metadata) — are asserted against `pg_constraint`/`pg_proc`, NOT via EF model equality. Do NOT call the whole live E2 schema "three-way EF agreement".
- **Semantic / config matrix:** below min; above max; zero/negative; exact min; exact max; absolute-max (3600) violation; both bounds unset (=> [60,900]); exactly one unset (=> invalid); malformed/non-integer; config changed between Draft creation and CatalogApprove — **exercised by: (1) create the Draft under provider/config A; (2) dispose/rebuild the service provider with config B; (3) approve the SAME persisted Draft under provider B** (approve re-validates against B's resolved state); config changed after approval (E2 does nothing — B3's concern); TTL immutable after approval; `Get`/`List` projections exact.
- **Readiness:** invalid bounds -> `PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID` 503; sanitized (no raw values / binder text); readiness does NOT scan policy rows.
- **No positional break + contract-not-DTO:** repo-wide grep of `RawExportPolicyVersion` + `AddRawExportPolicyVersionCommand` proves every §7 site updated; the new ArchTest passes; accessibility preserved.

## 6. Review tier & attacks
Tier-2. Attacks: (a) other 88A behaviour changed by the column/trigger? (b) range wrongly in a DB CHECK, or the CHECK EF-modeled/validated? (c) CHECK validates existing rows (must be NOT VALID)? (d) legacy NULL simulated post-E2 or unreadable? (e) null-TTL draft approvable? (f) TTL mutable in place? (g) bounds parsed in more than one place? (h) readiness leaks raw config / scans policy rows / startup crashes before readiness reports? (i) `Down()` fails to restore the exact pre-E2 function? (j) E2 edits the landed 88A migration? (k) B3 expiry/authorization creep or ACL widening?

## 7. Intent ledger + consumer inventory + EXACT allowlist

**Accepted:** immutable 88A PolicyVersion field, exact-version, new-version-to-change; raw-SQL NOT-VALID positive-non-null CHECK; one semantic-preserving closure-completeness trigger extension (structural only); one bounds resolver (default [60,900], abs max 3600, one-absent=>invalid); validation split (AddVersion+CatalogApprove here; re-validation in B3); readiness config-only + sanitized + readiness-path not startup-crash; rollback restores exact pre-E2 function body.
**Rejected/deferred:** config-map TTL; range in DB CHECK; EF-modeled CHECK; nullable-sanity CHECK; backfill; duplicated bounds parsing; B3 authorization-time re-validation + expiry (deferred).
**Non-claims:** no 88A eligibility/activation change; no permit issued/computed; no live hot reload; no historical-policy scan.

**Consumer inventory (census @ 4b25e2f):**
- `RawExportPolicyVersion`: def `RawExportPolicyCatalog.cs:60` (add trailing `int? PermitTtlSeconds`); row->domain ctor `EfRawExportPolicyRepository.cs:225` (add trailing arg).
- `RawExportPolicyVersionRow`: def `Entities/RawExportPolicyVersionRow.cs:3` (add property); cmd->row ctor `EfRawExportPolicyRepository.cs:36` (set); EF map in `TagEkycDbContext` + Designer + ModelSnapshot.
- `AddRawExportPolicyVersionCommand`: def `RepositoryPorts.cs:131` (add trailing required `int PermitTtlSeconds`); interface `RepositoryPorts.cs:159` / impl `EfRawExportPolicyRepository.cs:11` (signatures unchanged); **3 test ctors** `Tip88ARawExportPolicyCatalogTests.cs:25, :170, :224`.
- **Zero** deconstruction / with-expression / whole-record equality of either type.

**Implementation allowlist (exact — no wildcard):**
- `src/TagEkyc.Domain/RawExportPolicyCatalog.cs`
- `src/TagEkyc.Application/Ports/RepositoryPorts.cs`
- `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportPolicyVersionRow.cs`
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportPolicyRepository.cs`
- `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs`
- `src/TagEkyc.Infrastructure/RawExport/RawExportPermitTtlOptions.cs` (NEW; the single bounds resolver)
- `src/TagEkyc.Api/Program.cs` (bind options; wire readiness) · `src/TagEkyc.Api/ReadinessEndpoint.cs` (readiness code, calls the resolver)
- EXACTLY ONE generated migration pair under `src/TagEkyc.Infrastructure/Persistence/Migrations/` with suffix **`Tip88AE2PolicyPermitTtlField`** (i.e. `<timestamp>_Tip88AE2PolicyPermitTtlField.cs` + `.Designer.cs`) + the existing `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs`. **No second migration is authorized. Do NOT edit the landed 88A migration `20260711132410_*`.**
- `tests/TagEkyc.IntegrationTests/Tip88ARawExportPolicyCatalogTests.cs` (field/migration/legacy/negative/immutability tests)
- `tests/TagEkyc.IntegrationTests/Tip88AE2PolicyPermitTtlConfigTests.cs` (NEW; bounds/config/readiness tests, mirroring `Tip83E3RetentionConfigTests.cs`)
- `tests/TagEkyc.ArchTests/Tip88APolicyCatalogBoundaryTests.cs` (NEW; contract-not-DTO)
- Any additional source/test/project file after dispatch = STOP and report.

## 8. Landing docs (exact allowlist, on implementation land)
- Create `docs/tips/tip_88a_e2_policy_permit_ttl_field/tip_88a_e2_closeout.md`.
- Append a NON-NORMATIVE extension note to `docs/tips/tip_88a_raw_export_policy_catalog/tip_88a_closeout.md` (policy version gained an immutable `PermitTtlSeconds` + one authorized closure-completeness trigger extension; all other 88A semantics unchanged).
- Update `docs/tips/tip_88b3_raw_export_authorization_decision/tip_88b3_planning_brief.md` gate-3 to CLOSED with the landed commit + as-built field/config shape.
- Update `docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md` (E2 = LANDED; sequence).
- Add a note to `docs/deployment/hospital_trial/postgres_migration_runbook.md`: the config keys `TagEkyc:RawExport:PermitTtlMinSeconds`/`MaxSeconds`, default `[60, 900]`, absolute max `3600`, and the readiness code `PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID`.
- **Legacy disposition (document explicitly):** existing `CatalogApproved` NULL-TTL rows remain READABLE but B3 REJECTS them — publish a NEW policy version with a valid TTL before use; legacy `Draft` NULL-TTL rows CANNOT be approved — abandon and create a new version; NO backfill or in-place mutation.
- Do NOT mark TIP-88B3 READY_FOR_BUILD until gate-4 (rewrite from landed HEAD) also closes.
