# TIP-83E-2 Audit durability baseline - code-layer no-mutation lock + least-privilege runbook - Planning Brief

Status: READY_FOR_BUILD v1.0 (Codex + GPT both READY_FOR_BUILD @v0.4; Homeowner flip pending). v0.4 - Codex round 2 (5 findings) patched; GPT already READY_FOR_BUILD. R2 applied: [P1] the six repos' exact allowlists PINNED in-brief (verified on-code, no builder re-derive); [P1] runbook target file PINNED = postgres_migration_runbook.md; [P2] trigger grounding de-over-claimed (blocks while enabled, owner can drop/disable -> least-privilege role still needed); [P2] STOP reconciled with the recommended UPDATE assertion (MAY add one, no more); [P2] reflection targets interface + EF PRODUCTION impl only (not LocalDev). Codex R2: "after these -> READY_FOR_BUILD"; GPT: READY_FOR_BUILD. BUILD-LOCKABLE; awaits Homeowner READY_FOR_BUILD. v0.3 base - Codex + GPT reviews patched (identical, convergent, ACCEPT-WITH-PATCHES). Applied: OQ-1/2/3 ALL LOCKED (all-six repos each own exact allowlist / document least-privilege app role / include runbook note this slice); ArchTest reflection design PINNED (declared public instance methods only, exclude ctor/property/Object/compiler-gen, set-equality not Contains, FAIL if types missing - non-vacuous); runbook posture concretized (owner/migrator vs non-owner runtime role, append-only tables INSERT/SELECT only, GUIDANCE not auto-SQL, no role-names/SQL shipped); UPDATE trigger-test case promoted optional->recommended-if-low-cost. Believed BUILD-LOCKABLE; awaits Homeowner READY_FOR_BUILD. v0.2 base - self-checked (blacklist->exact-allowlist). Second slice of the split TIP-83E (83E-1 `/readiness` DONE+PUSHED `99bdd81`). Repo: D:\Task\Remote Signing\TagEkyc (server), baseline origin/master 99bdd81. **Scope shrank after recon (09/07): the DB-level append-only enforcement ALREADY EXISTS + is tested** - so 83E-2 is a small CODE convention-lock (ArchTest) + a deployment RUNBOOK note, NOT a new audit-durability build. NOT READY_FOR_BUILD until review converges.
Date: 2026-07-09

## 0. Grounding (recon 09/07, file:line) - what ALREADY exists (do NOT rebuild)
- **DB-level append-only is ENFORCED + TESTED:** migration `20260621075836_InitialPostgresPersistence.cs:284-319` installs plpgsql `tagekyc.deny_append_only_mutation()` + a `BEFORE UPDATE OR DELETE` trigger **`tr_audit_events_append_only` on `tagekyc.audit_events`** (`:316-318`, + 5 sibling append-only tables). **[R2-P2-3, no over-claim]** while installed/enabled the trigger blocks UPDATE/DELETE attempts by ANY role including owner DML attempts; but an owner/superuser can still `ALTER`/`DROP`/disable it - hence a non-owner least-privilege runtime role remains a required ops hardening (§2.2). Test `PostgresPersistenceSliceTests.cs:369-381` (`Append_only_tables_reject_update_and_delete`) asserts `DELETE FROM tagekyc.audit_events` throws. **This corrects the earlier "append-only by construction" note - it is DB-enforced.**
- **Audit is append-only at the code layer too:** port `IAuditEventRepository` (`Application/Ports/RepositoryPorts.cs:62-67`) exposes ONLY `AppendAsync` + `ListBySessionAsync`; impl `EfAuditEventRepository` (`Persistence/EfAuditEventRepository.cs:9,15`) has only those two. Grep = 0 UPDATE/DELETE/ExecuteUpdate/ExecuteDelete on audit.
- **Payload is PII-safe by construction:** `AuditEvent` stores a payload HASH + optional ref, not raw (`AuditEvent.cs:10-12`; `AuditEventRow.cs:11-12`).
- **Transactional-atomic append (the redline - do NOT disturb):** audit rows are added inside the finalization txn (`EfVerificationFinalizationBoundary.cs:44,:96`) + committed atomically (`:53-54,:100-101`).
- **DB role model = single OWNER identity:** the app connects via ONE `ConnectionStringSecretRef` (`Program.cs:204-243`); the migrator (`dotnet ef database update`, `postgres_migration_runbook.md:19-22`) uses the SAME role. So the app connects as the schema OWNER (it CREATEs the `tagekyc.*` objects). There is NO app-vs-owner role split; grep for `CREATE ROLE`/`GRANT ON`/`REVOKE`/least-priv in `src/`+`docs/deployment/` = nothing.
- **ArchTest suite:** `tests/TagEkyc.ArchTests/` (plain xUnit + `System.Reflection`, NOT NetArchTest), ProjectReferences all 7 source projects incl. Application + Infrastructure. Mirror `Tip17DurableMetadataBoundaryTests.cs:38-48` (reflects a port's shape to forbid a class shape). Feasible to reflect `IAuditEventRepository`/`EfAuditEventRepository` members.

## 1. Intent
Lock the audit **append-only convention at the CODE layer** with an ArchTest (so a future change can't quietly add a mutation method to the audit repo/port - a build-time guard complementing the existing runtime DB trigger), and DOCUMENT the one real remaining hardening - a **non-owner least-privilege app role** - as a deployment/ops runbook item (the app currently connects as OWNER, which can DROP the trigger; a least-privilege role cannot). 83E-2 does NOT rebuild the already-enforced+tested DB append-only trigger, does NOT ship a REVOKE migration (can't restrict the owner = theater), and does NOT add a hash-chain seam (deferred).

## 2. Scope - ALLOWED

### 2.1 ArchTest: audit repo exposes EXACTLY the append-only member set (the buildable core)
- New ArchTest (e.g. `Tip83E2AuditDurabilityBoundaryTests` in `tests/TagEkyc.ArchTests/`), mirroring `Tip17DurableMetadataBoundaryTests.cs:38-48`: reflect `typeof(IAuditEventRepository).GetMethods()` AND the public surface of `EfAuditEventRepository`, and assert the member set EQUALS the exact allowlist **`{ AppendAsync, ListBySessionAsync }`**.
- **[self-check] EXACT-ALLOWLIST, not a mutation-name blacklist:** a blacklist of prefixes (`Update`/`Delete`/...) would MISS a differently-named mutator (`Overwrite`/`Replace`/`Void`/`Amend`/`SaveChanges`). The exact allowlist locks the surface so ANY new/renamed method (mutating OR not) FAILS the test until consciously added to the allowlist - inherently non-vacuous, and forces a deliberate review when the surface changes (that IS the convention lock's point). This is stronger than the runtime trigger's backstop at the code/CI layer.
- **[R:P1-03/P1-04 - PIN the reflection design so the test is not vacuous]** for EACH append-only repo (all six per OQ-1):
  - assert the interface type AND the impl type EXIST (resolve by name) - **FAIL if either cannot be found** (a missing type must not silently pass).
  - reflect **DECLARED public INSTANCE METHODS only** (`BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly`) - EXCLUDE constructors, property accessors (`get_`/`set_`), inherited `Object` methods, and compiler-generated members. Do NOT reflect "all public members".
  - assert the method-name set EQUALS the repo's OWN exact allowlist (set equality, NOT a loose `Contains`).
  - **[R2-P2-5] reflect the interface + the EF PRODUCTION repository impl ONLY** (`Ef*Repository`), NOT the LocalDev impl (LocalDev repos carry public helper properties like `Events`/`Records` that are not the production append-only surface).
- **[R2-P1-1 - the six exact allowlists PINNED, verified on-code, do NOT let the builder re-derive]:**
  | Interface / EF impl | Exact allowed method set |
  |---|---|
  | `ICaptureArtifactRepository` / `EfCaptureArtifactRepository` | `{ AppendAsync, ListBySessionAsync }` |
  | `IEvidenceResultRepository` / `EfEvidenceResultRepository` | `{ AppendAsync, ListBySessionAsync }` |
  | `IVerificationDecisionRepository` / `EfVerificationDecisionRepository` | `{ AppendAsync, ListBySessionAsync, GetAsync }` |
  | `IEvidencePackageRepository` / `EfEvidencePackageRepository` | `{ AppendAsync, GetAsync, GetBySessionAsync }` |
  | `IInternalEvidenceManifestRepository` / `EfEvidenceManifestRepository` | `{ AppendAsync, GetByPackageAsync }` |
  | `IAuditEventRepository` / `EfAuditEventRepository` | `{ AppendAsync, ListBySessionAsync }` |
  (Builder confirms each set against HEAD at build; a drift => the test fails, forcing a conscious allowlist update.)

### 2.2 Deployment runbook: least-privilege app role (the real ops hardening)
- Add a subsection to **`docs/deployment/hospital_trial/postgres_migration_runbook.md`** (R2-P1-2: role/migrator/runtime-role posture is deploy/migration ops, not backup/restore) stating: (i) the ENFORCED DB tamper-evidence control for `audit_events` (+ the 5 siblings) is the `tr_audit_events_append_only` trigger (cite the migration), already live + tested; (ii) the app currently connects as the schema OWNER, which can `DROP` the trigger - so a non-owner least-privilege application role is a DEPLOYMENT responsibility (Homeowner + Hospital-IT), NOT something a code migration can enforce from the owner identity.
- **[R:P1-05] Recommended deployment posture (GUIDANCE text, NOT auto-applied SQL):**
  - the migration/OWNER role owns the schema and runs migrations (separately, by the owner/migrator identity);
  - the runtime APP role is NON-owner, with only the privileges the business needs;
  - for the append-only tables (`audit_events` + the 5 siblings) the runtime role gets **`INSERT`/`SELECT` only - NO `UPDATE`/`DELETE`/`ALTER`/`DROP`/trigger-disable**;
  - so a compromised runtime role cannot mutate audit rows NOR drop the trigger.
- This is docs-only GUIDANCE; NO code, NO migration, NO REVOKE/GRANT SQL shipped (a REVOKE run by the owner on itself = theater; and role NAMES are a deployment detail - do NOT ship concrete SQL until the actual role names are known at deploy time).

### 2.3 [recommended-if-low-cost, R:P1-06] Explicit audit-UPDATE trigger-test case
- The existing test proves `DELETE FROM tagekyc.audit_events` is blocked; add an `UPDATE tagekyc.audit_events` rejection assertion alongside it for symmetry (the trigger is `UPDATE OR DELETE`). Not required for readiness, but PREFERRED since this slice is already touching tests - do it if it can be done without test fragility.

## 3. STOP / NOT
- NO REVOKE/GRANT migration on `audit_events` - the app connects as OWNER; a REVOKE authored/run by the owner cannot restrict the owner (theater). The durable DB control (the trigger) already exists; least-privilege is a deployment role (runbook, §2.2).
- NO rewrite/rebuild of the append-only trigger or the existing `Append_only_tables_reject_update_and_delete` test - MAY add one `UPDATE tagekyc.audit_events` rejection assertion to it for symmetry (§2.3), nothing more.
- NO hash-chain / `sequence` / `prev_hash` / `row_hash` seam column - DEFER to the actual hash-chain build (a naive identity/serial is NOT gap-free under rollback = wrong-shaped seam; and it risks the transactional-append redline). Roadmap per `tip_83_planning_brief.md:15`.
- NO change to the transactional-atomic append (`EfVerificationFinalizationBoundary`), the audit model, or the repo's two methods.
- NO new migration; NO PII in audit (already hash+ref); NO proof/decision-basis change; NO retention work (that is 83E-3).

## 4. Definition of Done
- [ ] New ArchTest locks ALL SIX append-only repos (OQ-1) via EXACT per-repo allowlist: for each, interface+impl types resolved (FAIL if missing); DECLARED public INSTANCE methods only (exclude ctor/property-accessor/Object/compiler-generated); method-name set EQUALS the repo's own allowlist (audit = `{AppendAsync, ListBySessionAsync}`; siblings = their own). Any added method fails. Green.
- [ ] Runbook subsection added (§2.2): the trigger is the enforced DB control (cited); the recommended posture (owner/migrator vs non-owner runtime role; append-only tables INSERT/SELECT only) is documented GUIDANCE; no REVOKE/GRANT SQL shipped.
- [ ] [recommended] UPDATE-rejection assertion added to the existing append-only trigger test for `audit_events` (if no fragility).
- [ ] The existing `Append_only_tables_reject_update_and_delete` test still green; the transactional-atomic append path + audit model unchanged (diff shows only the new ArchTest, the optional test line, + the runbook doc).
- [ ] NO new migration; NO change to `src/` behavior; `dotnet build` + `dotnet test` green.

## 5. Review tier & attacks
Tier-2 (a durability convention-lock + an ops doc; VERY low blast-radius - a test + a runbook, no production code path). Attacks: (a) does the brief promise a REVOKE migration that cannot restrict the owner (unenforceable theater)? (b) does it accidentally rebuild/duplicate the existing trigger or its test? (c) does the ArchTest actually catch a newly-added mutation method (not a no-op assertion)? - the exact-allowlist form makes this structural (any new member fails), but confirm it is not accidentally reflecting only inherited/object members or using a too-loose filter that lets a new method slip; (d) does anything touch the transactional-atomic append or the audit model (redline)? (e) does it add a hash-chain seam prematurely (wrong-shaped, redline-risky)? (f) does the runbook overclaim that a code migration enforces least-privilege (it cannot from the owner identity)?

## 6. Open questions - ALL LOCKED (both reviewers required these closed before build)
- **OQ-1 [LOCKED]:** ArchTest exact-allowlist covers ALL SIX append-only repos (capture/evidence/decision/package/manifest/audit), NOT audit-only - the trigger already protects all six tables, so the convention lock prevents sibling drift. **Each repo asserted against ITS OWN exact allowed member set** (do NOT force the same `{AppendAsync,ListBySessionAsync}` on all - each has its own current surface, e.g. reads differ).
- **OQ-2 [LOCKED]:** document the non-owner least-privilege app role as a deployment RECOMMENDATION/responsibility (§2.2).
- **OQ-3 [LOCKED]:** INCLUDE the short runbook note in THIS slice (the honest companion to the ArchTest - without it the docs omit the real ops risk that the app-as-owner can DROP the trigger).

## 7. Companion / boundary
- **Hash-chain tamper-evidence** (sequence + prev_hash + row_hash, gap-free, a verify job) = the real roadmap item (umbrella `tip_83_planning_brief.md:15`); DEFERRED - design as one coherent scheme when built, not a premature seam.
- **Least-privilege app role provisioning** = a deployment/Hospital-IT task; 83E-2 documents it, does not build it.
- **83E-3 retention config** = the remaining sibling slice (Legal/DPO-gated); separate brief.
