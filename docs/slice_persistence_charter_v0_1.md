# Slice Charter — Durable Persistence (EF Core + PostgreSQL) v0.1

**File:** `docs/slice_persistence_charter_v0_1.md`
**Version:** 0.2
**Status:** ACCEPTED (code) — slice built + Tier-1-fixed (152/152). Adversarial review found 2 SHOULD-FIX (fault-injector in prod DI; finalize CAS without DB concurrency token), both resolved by `slice_persistence_patch_v0_1.md` and re-verified CONFIRMED-SAFE; arch-test changes verified as legitimate relocation to `PersistenceBoundaryTests` (not weakening). 2 NITs deferred. Open: clean scope-floored commit of slice+patch+docs (see patch doc status).
**Date:** 2026-06-21
**Baseline:** D-01 Decision Brief v0.2; Decision Register v0.2; current S1 codebase (143/143 tests, all repos LocalDevInMemory).
**Purpose:** A single approved charter for the first durable-persistence vertical slice, so the builder runs the whole slice autonomously against a concrete Definition of Done instead of a new planning-TIP chain. This is the "govern-light" model: one charter → one bundle → exception-based review.

---

## Changelog

### v0.2 — Codex charter review patches (2026-06-21)
- Patch A (policy provider): removed `IClientApplicationPolicyReader` from the durable port list (defined but unused by the happy path — services use `ILocalDevClientPolicyProvider`/`LocalDevRuntimePolicySource`). Client policy/api-key reference data is scoped OUT of slice 1 (startup-seeded), with the `→ IClientPolicyProvider` rename + EF policy store as a follow-up; Homeowner may opt to include it via STOP/ASK.
- Patch B (composition / arch test): explicitly authorized editing `TagEkyc.Api.csproj` (add Infrastructure ref, Api = composition root) and `ProjectDependencyTests.cs` (allow `Api → {Application, Contracts, Infrastructure}`), since the current arch test locks Api to Application+Contracts and would STOP/RRI the EF wiring otherwise.
- Patch C (CAS wording): corrected — the current `IVerificationFinalizationBoundary` CAS boundary is the whole `ExpectedSession` snapshot, not a per-check token; EF may realize it via rowversion/concurrency token and/or checked predicates. A finer per-check token needs a separate model/port change (out of slice).

### v0.1 — Initial charter
- Authored after Codex review of the D-01 brief. Encodes the concrete DoD Codex required (schema/migration, transaction write-set, idempotency/conflict tests, restart-durability test, fail-closed config, real-Postgres integration test), the granular-ports target, the scope floor, and the EBS review tier.

---

## 1. Trigger / precondition

Starts ONLY after **D-01 is signed = PostgreSQL + EF Core** (see brief §10). ✅ **MET 21/06/2026** — precondition satisfied, charter is live.

## 2. Objective + scope floor

Make the S1 runtime **durable** end-to-end against real PostgreSQL, behind the existing ports, with no change to API/DTO/business behavior.

**Scope floor (anti-shrink):** this slice is the FULL set of granular runtime ports + the finalization boundary as ONE bundle. A slice that implements only one repository, or only reads, or stops at "EF project scaffolded," is a **defect** unless a concrete blocker is logged. DoD = the S1 happy path runs on Postgres and survives a process restart.

## 3. Target architecture (port resolution — Codex patches A/B/C)

Implement EF Core + PostgreSQL for the **granular runtime ports** that the application services actually use:
`IVerificationSessionRepository`, `ICaptureArtifactRepository`, `IEvidenceResultRepository`, `IVerificationDecisionRepository`, `IEvidencePackageRepository`, `IInternalEvidenceManifestRepository`, `IAuditEventRepository`, **and `IVerificationFinalizationBoundary`**.

- **Atomic finalization (CAS — patch C):** `IVerificationFinalizationBoundary.TryFinalizeAsync` becomes **one EF transaction** writing decision + package + manifest + completion audit + the session-state transition. The current port's CAS boundary is the whole `ExpectedSession` snapshot (not a per-check token); the EF implementation may realize this via a **rowversion/concurrency token and/or checked predicates** over the expected-session fields. This is where the TIP-19 `DurableMetadataWriteSet` "commit-all-or-none" atomicity lands. A finer per-check conflict token would require a model/port change and is **out of this slice** (authorize separately).
- **Client policy / API-key reference data — OUT of durable scope in slice 1 (patch A).** The services read policy via `ILocalDevClientPolicyProvider` (impl `LocalDevRuntimePolicySource`) — reference/config data, not transactional evidence. It stays **startup-seeded (in-memory)** this slice; the durable path covers the transactional evidence aggregates only, and "restart durability" is asserted on those aggregates (config re-seeds deterministically as today). **Do NOT implement `IClientApplicationPolicyReader`** (defined in `RepositoryPorts.cs` but unused by the happy path). Durable policy/api-key storage + the `ILocalDevClientPolicyProvider → IClientPolicyProvider` rename is a separate follow-up; if the Homeowner wants it in slice 1, that authorizes the rename refactor + EF policy impl — **STOP/ASK to confirm first**.
- **Composition pattern (patch B).** `TagEkyc.Api` is the composition root and is authorized to reference `TagEkyc.Infrastructure` for DI wiring only. This requires editing `src/TagEkyc.Api/TagEkyc.Api.csproj` (+Infrastructure ref) and `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs` (allow `Api → {Application, Contracts, Infrastructure}`). Application/Domain/Contracts gain no EF reference (DoD item 10). If keeping Api EF-free is preferred, the alternative is a dedicated composition/bootstrap project — **STOP/ASK before taking that path**.
- **`IDurableMetadataRepository` / `DurableMetadataWriteSet` is OUT of this slice** (separate sanitized-metadata projection, TIP-11/17, currently un-wired). Defer its persistence to a follow-up; do not refactor services onto it now. Record as carry-forward, not done.
- Application services and their port signatures are **not refactored**. Only the LocalDev in-memory implementations gain EF siblings + a config-driven wiring switch.

## 4. In scope / Out of scope

**In:** EF Core 8 + Npgsql; `DbContext` + entity configs for session/capture/evidence/decision/package/manifest/audit (the transactional evidence aggregates); migrations; EF implementations of the **8 granular ports** above; transactional finalization; append-only enforcement; config-driven provider selection (in-memory for unit tests, Postgres for integration/prod); the Api→Infrastructure composition edit (`TagEkyc.Api.csproj` + `ProjectDependencyTests.cs`); docker-compose Postgres for integration tests; integration test suite.

**Out (explicit carry-forward):** durable client-policy / client-application / api-key storage (stays startup-seeded — §3 patch A); `IClientApplicationPolicyReader`; `IDurableMetadataRepository` projection wiring; ART-001..009 lifecycle; raw-payload/artifact storage; signature layers; webhook/outbox; retention/legal-hold enforcement; production credential/secret store; any API/DTO/business-rule change; multi-tenant schema; backup/restore *automation* (the slice proves restart-durability, not operational backup tooling).

## 5. Definition of Done (Codex-required — verifiable checklist)

1. **Schema + migrations:** EF migrations create all S1 tables with types/PK/FK/unique/index; migration is reviewable and reversible; `dotnet ef migrations` + apply runs clean.
2. **Append-only enforcement:** audit, evidence-result, decision, package, manifest tables are append-only at the DB level (revoked UPDATE/DELETE and/or deny-modification triggers), proven by a test that an UPDATE/DELETE fails.
3. **Transactional write-set:** `TryFinalizeAsync` commits decision + package + manifest + completion audit + session-state in ONE transaction; an injected mid-transaction failure leaves **zero** partial rows (proven by test).
4. **Idempotency:** unique constraint on `(clientApplicationId, externalSessionId)`; deterministic decision/package IDs persist idempotently; re-running complete on a completed session returns the same result with no duplicate rows (test).
5. **Conflict detection:** concurrent finalize with a stale CAS token yields a conflict result, not a partial/overwrite (test).
6. **Restart durability:** a session created + completed, then the process/DbContext is recycled, is fully retrievable with identical hashes/IDs (integration test).
7. **Fail-closed config:** missing/invalid connection config in a production-flagged environment **refuses to start / rejects**, never silently falls back to in-memory; LocalDev/test config cannot select the production path by default (test).
8. **Real-Postgres integration test:** the full S1 happy path (create → capture → evidence → complete → package → audit → GET) runs against a real Postgres (docker-compose), green.
9. **No regression:** existing 143 unit tests stay green (unit tests keep using in-memory repos); no API/DTO/business behavior change.
10. **Boundary preserved:** EF/Npgsql appears only in Infrastructure; Domain/Application/Contracts have no EF reference (ArchTest).

## 6. EBS / review tier

This slice touches **EBS-03 (audit append-only durability)** and **EBS-09 (finalization atomicity)** → **Tier 1**. Requires one adversarial red-team review pass: *try to produce a partial write, forge an out-of-order finalize, bypass append-only, or make a stale-config production start succeed.* Tier-0 plumbing (entity configs, mapping) gets the light pass. No legal/Tier-2 sign-off needed (no evidence-model or legal-mapping change).

## 7. Anti-pattern guard (TIP-21 §7)

Postgres+EF chosen against the 18 criteria, not by familiarity. Slice must not: add the package before the schema is criteria-justified, let the in-memory path remain the production default, or leak provider mechanics into Domain/Application/Contracts/consumers/SignFlow.

## 8. Files expected to change (indicative)

`src/TagEkyc.Infrastructure/**` (new: DbContext, entity configs, EF repo implementations, migrations); `src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj` (+ EF Core 8 + Npgsql); `src/TagEkyc.Api/TagEkyc.Api.csproj` (+Infrastructure reference — composition root); `src/TagEkyc.Api/Program.cs` (config-driven wiring switch in-memory↔Postgres); `tests/TagEkyc.ArchTests/ProjectDependencyTests.cs` (allow `Api → {Application, Contracts, Infrastructure}`); `config/appsettings*.json` (connection + provider flag); `tests/TagEkyc.IntegrationTests/**` (new project, real-Postgres); `docker-compose` for test Postgres; new ArchTest asserting EF/Npgsql is referenced only by Infrastructure (not Domain/Application/Contracts). **No** change to Domain, Application services, Contracts, or existing API shapes.

## 9. STOP/RRI conditions

Stop and report if: implementing durability forces an API/DTO/business-rule change; the append-only or atomic-write guarantee cannot be met with the chosen isolation level; the finalization CAS cannot be expressed in EF without a service refactor; or the slice would need to touch `IDurableMetadataRepository`/ART surfaces to compile.
