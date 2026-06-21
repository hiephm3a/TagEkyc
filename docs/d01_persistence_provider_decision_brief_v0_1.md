# D-01 Durable Persistence Provider — Decision Brief v0.1

**File:** `docs/d01_persistence_provider_decision_brief_v0_1.md`
**Version:** 0.3
**Status:** SIGNED 21/06/2026 — D-01 = **PostgreSQL + EF Core** (recommended option accepted, no deviations; LocalDev = in-memory tests + Postgres prod target). Charter `slice_persistence_charter_v0_1.md` is now ACTIVE.
**Date:** 2026-06-21
**Baseline:** Full TIP-01..62 decision harvest (2026-06-21); consolidates TIP-17/18/19/20/21/22/23/24/25/26/27/28/29/30/31/34.
**Purpose:** Package every pre-done analysis around the durable-persistence decision into a single page so the decision can be MADE now, instead of opening another planning TIP. This brief does not itself select a provider; it presents the loaded decision for Homeowner sign-off.

---

## Changelog

### v0.3 — SIGNED (2026-06-21)
- Homeowner signed D-01 = PostgreSQL + EF Core (recommended option, no deviations); LocalDev = in-memory tests + Postgres production target. Charter activated for builder dispatch.

### v0.2.1 — Codex follow-up patch (2026-06-21)
- Removed the stale `IClientApplicationPolicyReader` from the §9 granular-ports list and the §7 "repository-boundary" line (it is defined but unused; services read policy via `ILocalDevClientPolicyProvider`). Aligns the brief with charter v0.2: client policy/api-key durable storage is OUT of slice 1, startup-seeded.

### v0.2 — Codex review patches (2026-06-21)
- Codex review verdict: ACCEPT WITH PATCHES; direction PostgreSQL + EF Core confirmed. Repo-verified: `IDurableMetadataRepository`/`DurableMetadataWriteSet` exist, runtime wired all-LocalDevInMemory, no EF/Npgsql in csproj, 143/143 tests pass.
- Patch 1: softened "append-only/no-orphan trivially enforced" → "straightforward but must be proven" (§7).
- Patch 2: corrected the persistence-target description — distinguished the granular runtime ports (what services use) from `IDurableMetadataRepository` (a separate sanitized-metadata projection); the slice targets the granular ports + finalization boundary (§9).
- Patch 3: line-count claim stated with method (≈42k physical / ≈32k non-blank).
- Patch 4: replaced "~22 TIPs" with a direct-vs-indirect unblock statement (§2, §9).
- Patch 5: added explicit "what D-01 does NOT do" scope limit (§9a).

### v0.1 — Initial decision brief
- Assembled the option space, the 18 fixed evaluation criteria, the already-resolved dependency gates, and an assistant recommendation, from the existing TIP corpus. No new analysis invented.

---

## 1. The decision (one sentence)

**Which durable-persistence mechanism will TagEkyc use to store its own metadata (sessions, audit identity, evidence packages, completion authority, idempotency facts) — relational / document / event-store / cloud-managed / LocalDev-only adapter — and via EF Core or non-EF, and is a LocalDev adapter built first?**

It is the **necessary-but-not-sufficient prerequisite** for nearly all remaining runtime work: it *directly* unblocks the persistence slice (durable repos, audit durability, real idempotency, restart durability); it is a *required precondition* (not by itself enough) for the ART-001..008 lifecycle, the durable metadata-reference registry, backup/restore *capability*, and production readiness — each of those still needs its own slice on top of D-01.

## 2. Why this is the keystone

Until this is chosen, ALL of the following stay blocked (TIP-18 §6, TIP-21 §4, TIP-19 §11):
durable repository implementation · any Infrastructure adapter · schema/migrations/indexes · real durability across restart · durable audit store · backup/recovery capability · outbox/webhook substrate · production auth & credential store · retention/legal-hold/purge enforcement · the entire ART-001..009 runtime lifecycle · any pilot/production readiness. **TagEkyc is frozen as a non-production in-memory system until this resolves.**

## 3. Critical clarification — this is NOT blocked by ART-009

The TIP corpus overloads the word "provider" across two different decisions:
- **(this decision)** the *database/persistence mechanism* for TagEkyc's own metadata — **not gated by any artifact-evidence rule.**
- the *eKYC engine vendor* that supplies raw identity evidence — gated by `ART-009` (raw-payload hard blocker).

`ART-009` and the `ART-001..008` carry-forward apply to the second, NOT to this one. **This decision is blocked only by self-imposed process** (the TIP-21 9-stage gate + the recurring "open another planning TIP first" reflex), not by any real dependency. It can be made now.

## 4. What is ALREADY resolved (do not re-derive)

The decision is fully "loaded" — these are settled and become the acceptance frame for whatever provider is chosen:

| Area | Resolved state | Source |
|---|---|---|
| **Repository boundary** | `IDurableMetadataRepository` (Application port) is THE boundary; provider mechanics must not leak into Domain/Contracts/consumers/SignFlow | TIP-17, TIP-29 |
| **Atomic write unit** | `DurableMetadataWriteSet` = {session (req), audit-identity (req), evidence-package (opt), completion-authority (opt)} — commit-all-or-none; detect unknown/interrupted | TIP-19 §4 |
| **Audit** | append-only; corrections = added records, never mutate | TIP-19 §5 |
| **No-orphan rule** | business write must not stand without its required audit write, and vice-versa | TIP-19 §5 |
| **Idempotency inputs** | candidate key inputs enumerated (operation, sessionId, packageId, clientAppId, requestId, correlationId, actor identity, expected-state, policy-snapshot) | TIP-19 §7 |
| **RPO/RTO target** | class `DMT-LOSSLESS-VALIDATED` = lossless write-set RPO + validation-gated RTO (no numeric SLA required) | TIP-31 |
| **Backup/restore** | 8 fact groups, 9 restore scenarios, 9 validation areas, 10 quarantine triggers (requirements) | TIP-26 |
| **Ops ownership** | 17 role/function owners, 10 incident classes | TIP-27 |
| **Config/env separation** | 6 env categories, fail-closed posture, no LocalDev/test fallback into prod | TIP-28 |
| **Migration/reversibility** | provider-mechanics containment; rollback/abandon/exit criteria; schema work gated by a future authorization slice | TIP-29 |
| **Forbidden data** | durable metadata = safe refs/hashes only; never raw bytes/biometric/provider-payload/vault-bytes/secrets/keys | TIP-19/39 |

G-001..G-004 (backup, ops ownership, config/env, migration) are all **resolved at planning/decision-class level**. The only thing missing is the choice itself.

## 5. The option space (TIP-20 §5, TIP-21 §4)

1. **Relational database** (ACID, transactions, mature migration tooling)
2. **Document database**
3. **Event store**
4. **Cloud-managed database**
5. **LocalDev-only adapter** (non-production; sequencing question, not a production answer)

EF Core vs non-EF is an orthogonal sub-choice. LocalDev-first sequencing is a third sub-choice.

## 6. The evaluation lens (TIP-20 §6 — 18 fixed criteria, condensed)

Any chosen provider must demonstrably satisfy: same-boundary write-set atomicity · append-only audit · audit/business orphan prevention · package/completion finalization consistency · definable idempotency identity · duplicate suppression · conflict detection · unknown/interrupted-outcome detection · concurrency/versioning · transaction/isolation (or accepted equivalent) · repository-boundary preservation · forbidden-data controls · schema/index/migration governance · backup/recovery suitability · operational complexity + LocalDev/prod separation · test/proof feasibility · architecture compatibility · no SignFlow dependency.

## 7. Assistant recommendation (for Homeowner confirmation)

**Recommended: Relational database = PostgreSQL, accessed via EF Core 8, with the existing in-memory repositories retained as the LocalDev/test path and PostgreSQL as the single explicit production target.**

Rationale mapped to the criteria (NOT to familiarity — see the guard in §8):
- **Write-set atomicity + transaction/isolation + finalization consistency:** a relational engine with real ACID transactions is the most direct fit for "commit-all-or-none" `DurableMetadataWriteSet` semantics; document/event stores would need application-side compensation to emulate it.
- **Append-only audit + no-orphan:** straightforward in PostgreSQL but **must be proven, not assumed** — requires an explicit append-only table policy (revoke UPDATE/DELETE and/or deny-modification triggers), a transaction boundary spanning business + audit rows, a chosen isolation level, a concurrency token, and migration + consistency tests. Not "trivial."
- **Idempotency + duplicate/conflict detection:** unique constraints + optimistic-concurrency tokens give first-class duplicate-suppression and conflict-detection (same-id+same-facts vs same-id+different-facts).
- **Repository-boundary preservation:** the repository ports (`IVerificationSessionRepository`, `IAuditEventRepository`, … + `IVerificationFinalizationBoundary`) already abstract persistence; EF Core sits entirely in Infrastructure, satisfying TIP-29 provider-mechanics containment.
- **Schema/migration governance:** EF Core migrations give reviewable, reversible schema changes — fits the TIP-29 future-authorization-slice model.
- **Backup/recovery suitability for `DMT-LOSSLESS-VALIDATED`:** PostgreSQL's WAL/PITR supports a lossless RPO posture and validation-gated restore.
- **Operational complexity + portfolio fit:** the sibling SignFlow project already runs **PostgreSQL 16 + EF Core 8 + .NET 8** — one operational/backup/skills surface across both projects (a legitimate operational-complexity and test-feasibility criterion, not mere preference).
- **No SignFlow dependency:** sharing a *technology* is not a code/runtime dependency; TagEkyc keeps its own database, schema, and connection — the SignFlow boundary is untouched.

**LocalDev sequencing:** keep the current in-memory repos for fast unit tests; add an EF-Core-PostgreSQL integration path (docker-compose Postgres) as the production target from day one. Do **not** build a separate "LocalDev durable adapter" that could drift into production — production is explicitly Postgres+EF, closing the exact trap TIP-21/22 warned about.

## 8. Guardrail check (TIP-21 §7 anti-patterns)

The recommendation is justified against the 18 criteria above, **not** by "we already know Postgres." Decider should still confirm each criterion is acceptable. The recommendation explicitly avoids: choosing by machine-availability, adding a package before deciding, writing schema before criteria, and letting a LocalDev adapter become production by default.

## 9. What choosing this unblocks (immediate next slice)

**Persistence-target clarification (Codex patch 2).** The codebase has TWO distinct persistence abstractions; the slice must not conflate them:
- **Granular runtime ports** (`RepositoryPorts.cs`): `IVerificationSessionRepository`, `ICaptureArtifactRepository`, `IEvidenceResultRepository`, `IVerificationDecisionRepository`, `IEvidencePackageRepository`, `IInternalEvidenceManifestRepository`, `IAuditEventRepository`, plus `IVerificationFinalizationBoundary.TryFinalizeAsync` (the atomic completion write; CAS over the whole `ExpectedSession` snapshot). **These are what the application services actually use**, currently wired to `LocalDevInMemory…`. (Client policy/api-key reads go through a separate `ILocalDevClientPolicyProvider` — reference/config data; durable policy storage is OUT of slice 1 and stays startup-seeded. `IClientApplicationPolicyReader` is defined in `RepositoryPorts.cs` but unused by the happy path — do not implement it.)
- **`IDurableMetadataRepository` + `DurableMetadataWriteSet`** (`DurableMetadataRepositoryPorts.cs`): a separate **sanitized-metadata projection** (RetentionClass, LegalHold, PolicySnapshot, actor-credential metadata) from TIP-11/17. It is **not** where the session/evidence aggregates live and is largely un-wired into the runtime flow today.

**The slice therefore targets the granular ports + the finalization boundary** (an EF Core implementation of each, with `TryFinalizeAsync` realized as a single EF transaction — this is where the TIP-19 `DurableMetadataWriteSet` atomicity actually lands). `IDurableMetadataRepository` is treated as the consistency/governance-metadata contract; whether its projection is persisted inside the finalization transaction in slice 1 or deferred to a follow-up is an explicit scope line in the charter. The full DoD lives in `slice_persistence_charter_v0_1.md`.

After the slice: the ART-001..008 lifecycle can be implemented against a real store; the metadata-reference registry (TIP-60) gets durable backing. This is the bundle that ends the planning loop.

### 9a. What D-01 does NOT do (scope limit)

Signing D-01 makes TagEkyc *able to be* durable — it does **not** make it production-ready. Still required afterward, each as its own slice: the ART-001..009 artifact lifecycle, `ART-009` raw-payload policy finalization, the 3 real signature layers (payload/webhook/evidencePackage), webhook delivery/retry/outbox, retention/legal-hold enforcement, production completion-authority, production credential/secret lifecycle, and legal/compliance certification. D-01 is the foundation, not the finish line.

## 10. Decision record (to be filled by Homeowner)

```
D-01 decision:        [X] Postgres + EF Core (recommended)   [ ] other: __________
EF vs non-EF:         [X] EF Core   [ ] non-EF: __________
LocalDev sequencing:  [X] in-memory tests + Postgres prod target (recommended)   [ ] other
Accepted by:          __________________   Date: 21/06/2026
Deviations / notes:   __________________
```
