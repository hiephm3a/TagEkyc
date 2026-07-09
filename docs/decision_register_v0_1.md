# TagEkyc Decision Register v0.1

**File:** `docs/decision_register_v0_1.md`
**Version:** 0.2
**Status:** Draft — consolidated decision anchor (Codex review patches applied 2026-06-21)
**Date:** 2026-06-21
**Baseline:** Full harvest of TIP-01..62 (planning briefs + kickoffs + closeouts), the 6 official design docs, and the current S1 codebase, 2026-06-21.
**Purpose:** Single anchor that consolidates the decision content currently scattered across a large TIP corpus (docs/tips ≈42k physical lines / ≈32k non-blank, the majority in docs-only planning TIPs). It separates DECIDED material (→ authoritative LLD) from OPEN decisions (Homeowner must choose) from DEFERRED items, so future TIPs become thin deltas against this register instead of re-deriving context per TIP.

---

## Changelog

### v0.2 — Codex review patches (2026-06-21)
- Line-count stated with method (≈42k physical / ≈32k non-blank) instead of a single disputable figure.
- Added the granular-ports vs `IDurableMetadataRepository` distinction to the persistence rows (§3 D-01 note, §7).
- D-01 unblock claim reframed as necessary-but-not-sufficient.

### v0.1 — Initial consolidation
- Harvested all 62 TIPs via parallel review; tagged every finding by epistemic status; deduped open decisions into a tiered Master Decision Register (D-01..D-12); recorded the Coverage Map and the live cross-doc contradictions to fix during LLD consolidation.

---

## 1. How to read this

Every item carries an epistemic-status tag:
- **DECIDED+BUILT** — settled and implemented; lift into authoritative LLD as descriptive of reality.
- **DECIDED (planning)** — settled at planning/decision-class level, not yet built.
- **OPEN** — analyzed but not chosen; Homeowner must decide (these populate the Master Decision Register).
- **DEFERRED** — explicitly punted, with a trigger/owner.

Core finding: 62 TIPs compress to **a small set of state-models + packet-schemas (DECIDED) + ~12 OPEN decisions, of which ONE is the keystone (D-01).** Most of the corpus is repeated non-claim boilerplate, not unique decision content.

## 2. Coverage Map (by layer)

| Layer | Status | Where it lives | Note |
|---|---|---|---|
| Core S1 (session→capture→evidence→complete→package→audit→notify) | DECIDED+BUILT (~4/5 depth) | stranded in TIP-04/05/06/07 kickoffs (NOT in lld_02/03/04) | full state machine, decision precedence, assurance map, canonicalization, error→status, audit rules |
| Ops / DR (backup, RPO/RTO, config/env, migration/exit) | DECIDED (planning / decision-class) | TIP-26..31 | RPO/RTO class `DMT-LOSSLESS-VALIDATED`; G-001..G-004 resolved |
| **Durable persistence (DB)** | **DECIDED — signed 21/06/2026** (PostgreSQL + EF Core) | `d01_..._brief.md` v0.3 → `slice_persistence_charter_v0_1.md` ACTIVE | keystone resolved; persistence slice dispatched |
| Artifact lifecycle ART-001..009 | analyzed; UNRESOLVED at runtime; packet-gated | TIP-38..47 | compresses to ~1 state machine + ~1 packet schema |
| Metadata-reference runtime | DECIDED+BUILT (LocalDev slivers) | TIP-55/57/60/62 | durable version blocked on D-01 |
| Governance (review ladder, intent ledger, bundle model) | DECIDED+BUILT | TIP-36/53/59; HLD+lld_01 patched by TIP-49 | lld_02/03/04 NOT yet patched |

## 3. Master Decision Register

### Tier 0 — KEYSTONE

| ID | Decision | Pre-done analysis | Blocks | Owner |
|---|---|---|---|---|
| **D-01** | Choose durable persistence mechanism (relational/document/event-store/cloud/LocalDev-adapter) + EF-vs-non-EF + LocalDev-first sequencing | TIP-20 18 criteria; TIP-19 write-set+idempotency; TIP-21 9-stage process; TIP-34 option taxonomy. **NOT blocked by ART-009** (that gates eKYC-engine evidence, a different "provider"). **SIGNED 21/06/2026 = PostgreSQL + EF Core** (no deviations). | Directly: persistence slice (durable repos, audit durability, idempotency, restart). Prerequisite for: all ART runtime, durable registry, production | Homeowner — DONE |

> **Port note (Codex patch):** the slice targets the **granular runtime ports** (`IVerificationSessionRepository`, `IAuditEventRepository`, `IEvidencePackageRepository`, … + `IVerificationFinalizationBoundary`) that services actually use — NOT `IDurableMetadataRepository`, which is a separate sanitized-metadata projection (TIP-11/17). See `slice_persistence_charter_v0_1.md`.

### Tier B — Structural consolidation (assistant drafts, Homeowner confirms)

| ID | Decision |
|---|---|
| D-02 | Consolidate the stranded buildable S1 spec (TIP-04/05/06/07 kickoffs) into `lld_02/03/04` — TIP-49 patched only `hld`+`lld_01` (governance rules); the domain spec is still stranded |
| D-03 | Unify artifact-lifecycle state vocabulary (ART-002 10-state + ART-008 13-state + ART-003/004/005/006/007 state models) into ONE enum; merge the ~9 packet field-sets into ONE Artifact-Evidence Packet schema |
| D-04 | Fix live contradictions (see §4) |

### Tier C — Value-filling (frameworks exist, values blank; mostly follow D-01)

| ID | Decision | Owner |
|---|---|---|
| D-05 | Idempotency key format, concurrency token, conflict-error contract | Technical (follows D-01) |
| D-06 | Retention duration per class × environment (ART-004) | Business |
| D-07 | Package profile — which object classes required/optional/excluded (ART-003) | Business |
| D-08 | Production completion authority (BusinessConsumer vs System/InternalService vs Operator vs split) | Homeowner (flagged TIP-12/13/17) |

### Tier D — Deferred (register + trigger + owner)

| ID | Item | Trigger |
|---|---|---|
| D-09 | ART-009 raw-payload policy finalization | before eKYC-engine evidence collection |
| D-10 | 3 real signature layers (payload/webhook/evidencePackage) — EBS-07 | before legal/audit reliance |
| D-11 | Webhook delivery/retry/outbox | when real callback needed (deferred since TIP-07) |
| D-12 | Operator/admin access semantics; cross-client support lookup; GOV-001 crosswalk | S2+ / before cross-client features |
| D-13 | Production auth posture for managed API keys: HMAC-SHA256 keyed hash in Postgres with non-secret prefix selector, pepper via SecretRefResolver, managed API keys for trial actors; full IdP/OAuth/mTLS and lifecycle admin deferred. Schema is provider-neutral (PrincipalId/CredentialRef/CredentialType/CredentialStatus) and pre-stages nullable OAuth/mTLS columns (OAuthClientId/MtlsSubjectDn) as forward-compat scaffold with no runtime path yet | TIP-84B/TIP-84C / before production-auth hardening beyond trial |

## 4. Live contradictions to fix during consolidation

| # | Issue | Resolution |
|---|---|---|
| C-1 | `VerificationCompletedEventDto.EventType`: TIP-06 §20 says `EKYC_VERIFICATION_COMPLETED`; implemented value is `VERIFICATION_COMPLETED` | pin `VERIFICATION_COMPLETED`; treat TIP-06 string as stale |
| C-2 | Artifact state vocab: ART-002 `Missing` vs ART-008 `ArtifactMissing` (parallel, non-identical) | unify into one enum (D-03) |
| C-3 | `ExpiredNonSuccess` (retention class) vs `ExpiredReferenceNonSuccess`/`Expired` (state) naming collision | disambiguate class vs state names |
| C-4 | Doc-header version labels lag changelog bodies across many TIPs (e.g. header v0.1, body v0.3) | cosmetic; normalize on next hygiene pass |
| C-5 | `lld_02/03/04` still carry pre-TIP-49 storage/vault/package wording governed only by reference, not patch | resolve via D-02 |

## 5. Evidence-Bearing Surface cross-reference

Tier-1/2 review (adversarial + legal) concentrates on the evidence-bearing surfaces, not on Tier-0 plumbing. The EBS registry (to be filed separately) anchors: manifest + manifest hash chain, hash canonicalization (**RFC 8785 JCS `rfc8785-jcs-v1`, resolved by TIP-65** — was `JsonSerializerDefaults.Web`), append-only audit (placeholder hash for denied-access events — flagged), **opaque-challenge semantics (TIP-67A `CHALLENGE_BOUND_EKYC_PROFILE`, replaced the old `bindingNonceHash`/transaction-binding)**, assurance-level mapping, final-result precedence, **signature layers (real ES256 JWS at DEV-level via TIP-66/67B; production HSM custody = TIP-68 pending — was all-placeholder)**, data-return/sanitization boundary, finalization atomicity. D-01 + the persistence slice directly touch the audit-durability and finalization-atomicity surfaces, so that slice is Tier-1.

## 6. Provenance (which TIPs fed each row)

Foundation/core S1: TIP-01,02,03,04,05,06,07,08,09. Boundary/authz: TIP-11,12,13. Durable-metadata/runtime: TIP-17,55,57,60,62. Persistence-provider cluster: TIP-18,19,20,21,22,23,24,25,34. Ops/DR: TIP-26,27,28,29,30,31. S2/S3 gov + ART: TIP-32,33,35,36,37,38,39,40,41,42,43,44,45,46,47. Consolidation/packets/bundle: TIP-48,49,50,51,52,53,54,56,58,59.

## 7. Recommended sequence

1. **Make D-01** (use the Decision Brief) — ends the planning loop.
2. **One vertical persistence slice** — EF-Core-Postgres implementation of the **granular runtime ports + `IVerificationFinalizationBoundary`** (NOT `IDurableMetadataRepository`); write-set atomicity + idempotency + restart-durability proven by integration tests against real Postgres. DoD in `slice_persistence_charter_v0_1.md` (working flow + tests, not docs).
3. **D-02 consolidation** of the stranded S1 domain spec into lld_02/03/04 (assistant-drafted).
4. ART-001..008 lifecycle implemented against the real store, as bundles (not micro-TIPs), each with a scope floor.
