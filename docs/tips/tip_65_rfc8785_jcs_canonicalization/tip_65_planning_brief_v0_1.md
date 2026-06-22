# TIP-65 RFC 8785 JCS Canonicalization — Planning Brief v0.1

**File:** `docs/tips/tip_65_rfc8785_jcs_canonicalization/tip_65_planning_brief_v0_1.md`
**Version:** 0.3
**Status:** Draft — implementation planning (Tier-2 / evidence-bearing, EBS-01); GPT/Codex review round 2 patches applied; pending convergence before dispatch
**Date:** 2026-06-21
**Baseline:** `951694a docs: close TIP-64 S1 evidence-integrity consolidation (lld_01 v0.2)` (master).
**Purpose:** Resolve Tier-2 open item **T2-1** by replacing the implementation-deterministic Web-JSON canonicalization with a portable, independently-reproducible **RFC 8785 (JCS)** canonicalization, recording the three metadata fields (`packageVersion` + `canonicalizationScheme` + `hashAlgorithm`) so verifiers/migrations are unambiguous. Code change. This is the first of the two T2 decisions; signing (T2-2) follows in TIP-66 and depends on this.

## Changelog

### v0.3 — GPT/Codex review round 2 patches (2026-06-21)
- STOP/RRI no longer contradicts the authorized metadata delta; authorized the minimal EF migration for the three columns.
- Scope-bound (Contractor decision): metadata exposed via INTERNAL manifest only; public BusinessConsumer summary unchanged → lld_02/03 out of scope (public exposure deferred). Resolves the lld_02/03 contradiction by bounding, not expanding.
- Summary wording uses the three fields (not "scheme + version"); cross-verification pinned to a non-app implementation.

### v0.2 — GPT/Codex review patches (2026-06-21)
- Metadata made **durable + retrievable** (not just a hash input): added `hashAlgorithm` as a required field; the three fields (`packageVersion`/`canonicalizationScheme`/`hashAlgorithm`) are hashed + persisted (EF rows/mapper) + exposed (read DTOs). Authorized this as the only additive contract/persistence delta (corrected the earlier "no persistence change" contradiction).
- Timestamp PINNED to `yyyy-MM-ddTHH:mm:ss.fffffffZ` full-precision (no longer "confirm in review"; switched from whole-second to avoid truncation/mismatch).
- DeterministicGuid versioning made explicit + golden vectors per id.
- Legacy classification + verifier RULE documented (fail-closed, future-field safety, re-issue-not-mutate) — scoped as rule + test fixtures, NOT a built verifier engine (pushed back on reviewer's "build a verifier" implication).
- Machine-stable lowercase constants pinned (`rfc8785-jcs-v1`, `web-json-deterministic-v1`, `sha256`, `evidence-package-v2`).

### v0.1 — Initial planning brief
- Opened TIP-65 to adopt RFC 8785 JCS canonicalization per Homeowner decision (2026-06-21). Recorded the decision basis, scope, the JCS-before-signing dependency, the versioning approach, the cross-verification proof requirement, non-goals, and STOP/RRI.

## 0. Homeowner Decision Basis (T2-1 resolution)

| T2 item | Homeowner decision (2026-06-21) |
|---|---|
| **T2-1** canonicalization | **Adopt RFC 8785 JCS.** Versioned rollout: record `canonicalizationScheme` + bump evidence/package version; old-scheme packages (if any) stay valid under their scheme. |
| **T2-2** signing | Abstract `IEvidenceSigner`; dev = SoftHSM; prod = HSM/KMS; format = JWS. **TIP-66, after TIP-65.** |

JCS adoption is the resolution of T2-1: it removes the .NET-serializer/property-order fragility and makes the evidence hashes portable and independently reproducible.

## 1. Status / Purpose / Authorization basis
TIP-65 is an **implementation** TIP (code + tests + the lld_01 doc), **Tier-2 / EBS-01** (the canonicalization is the root of the entire evidence hash chain). Authorization: Homeowner T2-1 decision (§0); EBS-01; lld_01 Evidence-Integrity §Tier-2. Role split: Contractor drafts → GPT + Codex review/converge → Codex builds → Contractor adversarial spot-check (with independent cross-verification).

## 2. TIP Analytical Summary / Intent Ledger

### Intent
Make the S1 evidence hashes portable and independently reproducible by adopting RFC 8785 JCS canonicalization, with the three metadata fields (`packageVersion` + `canonicalizationScheme` + `hashAlgorithm`) recorded.

### Expected Outcome
After TIP-65: `HashCanonical`/`DeterministicGuid` canonicalize via RFC 8785 JCS; non-JSON-native values use pinned deterministic formats; the three metadata fields (`packageVersion`+`canonicalizationScheme`+`hashAlgorithm`) are hashed + persisted + read back via the internal manifest; an **independent JCS implementation (not the app's) reproduces the same hashes** (proven by test); `lld_01` is updated and T2-1 marked resolved. Public summary + lld_02/03 untouched.

### Accepted Decisions
| Decision | Why | Scope impact | Non-claims |
|---|---|---|---|
| RFC 8785 JCS for canonical JSON structure | Portable, deterministic key order/number format; independently verifiable | `HashCanonical` serialization changes | Not a legal-sufficiency claim; T2-2 (signing) still open |
| Fixed deterministic value formatting (timestamps, guids) | JCS canonicalizes structure but preserves string values — timestamps must be format-pinned to be portable | Timestamp/guid serialization pinned | — |
| Versioned hash metadata: record `packageVersion` + `canonicalizationScheme` + `hashAlgorithm` | Verifiers/migrations must know which scheme/algorithm produced a hash | New three metadata fields on domain + internal manifest DTO + EF rows/mapper + minimal migration | Old-scheme packages valid under their recorded scheme |
| Cross-verify with an independent JCS implementation | The whole point of JCS is portability — prove it | New conformance/cross-verify tests | — |

### Rejected / Deferred Branches
| Branch | Disposition | Why |
|---|---|---|
| Hard-switch without versioning | Rejected | Leaves the scheme ambiguous; Homeowner chose versioned |
| Implement signing in this TIP | Deferred | T2-2 = TIP-66; depends on a stable JCS hash first |
| Change the hash-chain structure (manifestBody→package→manifest) | Rejected | Only the canonicalization function changes; the chain stays |

### Debt / Gap Impact
| Debt/gap | Action | Result |
|---|---|---|
| T2-1 canonicalization not JCS (EBS-01) | Adopt JCS + versioning + cross-verify | **Resolved** by TIP-65 |
| T2-2 placeholder signatures (EBS-07) | — | Still open → TIP-66 |

### Non-Claims
TIP-65 does not: implement signatures; claim production/legal sufficiency or non-repudiation; change the hash-chain topology, API routes, or business behavior; change any contract/persistence field beyond the three authorized metadata fields; build a runtime verifier engine; resolve T2-2.

### Dispatch Readiness
- Dispatch allowed? Not yet — pending GPT + Codex review.
- Files that may change: canonicalization code (`src/TagEkyc.Application/**`, optional JCS helper/package); the three metadata fields on `EvidencePackage`, the manifest DTO (`ManifestContracts`), `EvidencePackageRow`/`EvidenceManifestRow`/`DomainRowMapper` + minimal migration, and INTERNAL manifest readback only (public BusinessConsumer `EvidencePackageSummaryDto` remains unchanged and is out of scope); `DeterministicGuid` versioning; tests (`tests/**`); `docs/lld_01_data_model_v0_1.md`. Exact allowlist + the authorized minimal additive delta in kickoff §3.3/§4.
- STOP/RRI: see §7 + kickoff.

## 3. Scope (this slice)
- Replace the Web-JSON canonicalization in `HashCanonical` + the `DeterministicGuid` input with RFC 8785 JCS canonical JSON.
- Pin deterministic value formats (timestamps `yyyy-MM-ddTHH:mm:ss.fffffffZ` UTC full-precision; guids fixed) so hashes are portable.
- Record **three metadata fields, durable + retrievable** (not just a hash input): `packageVersion=evidence-package-v2`, `canonicalizationScheme=rfc8785-jcs-v1`, `hashAlgorithm=sha256` — in the manifest body (hashed), persisted (EF rows + mapper + minimal migration), exposed via the **internal** manifest DTO. Public BusinessConsumer summary unchanged → `lld_02`/`lld_03` out of scope (public exposure deferred).
- Version the deterministic-id derivation under the scheme; golden vectors per id.
- Document the legacy classification + verifier RULE (select by recorded fields, fail-closed on unknown, legacy=`web-json-deterministic-v1`/not-production, re-issue=new package, future-field safety) — a documented rule + test fixtures, **NOT a built verifier engine** (S1 has none).
- Tests: RFC 8785 conformance vectors; **independent cross-verification**; deterministic-id golden vectors; legacy fixture; fail-closed; future-field regression; metadata readback.
- Update `lld_01` Evidence-Integrity to JCS + the verifier rule; mark T2-1 resolved.

## 4. Non-goals
Signing (T2-2 / TIP-66); changing the hash-chain TOPOLOGY; any contract/persistence change beyond the three authorized metadata fields (§3); API routes / business-state / response semantics; a runtime verifier engine; lld_02/03/04/hld changes.

## 5. Key design decisions (PINNED)
- **JCS implementation:** vetted RFC 8785 library for .NET 8 if sound; else implement per RFC 8785. Must pass the RFC conformance vectors byte-for-byte.
- **Timestamp format (pinned):** `yyyy-MM-ddTHH:mm:ss.fffffffZ` (UTC, invariant, full-precision) — lossless vs stored `CompletedAt`, round-trippable by a non-.NET verifier. Chosen over whole-second to avoid truncation/mismatch.
- **Metadata constants (durable + retrievable):** `packageVersion=evidence-package-v2`, `canonicalizationScheme=rfc8785-jcs-v1`, `hashAlgorithm=sha256`; legacy `web-json-deterministic-v1` / `tip-06-localdev-v1`.
- **Verifier:** documented RULE + test fixtures; NO runtime verifier engine built in S1.

## 6. Acceptance criteria (DoD summary)
Full DoD in kickoff. Summary: JCS canonicalization in place; deterministic value formatting; the three metadata fields (`packageVersion` + `canonicalizationScheme` + `hashAlgorithm`) recorded — hashed + persisted + read back via the internal manifest; independent-implementation (non-app) cross-verification test passes; RFC 8785 conformance vectors pass; full `dotnet test` green; lld_01 updated + T2-1 resolved; no API/business change beyond the three metadata fields; public summary + lld_02/03 untouched.

## 7. STOP/RRI
- If a vetted JCS library is unavailable AND a conformant implementation cannot be completed within scope → STOP and report (do not ship a non-conformant canonicalizer).
- Any change beyond canonicalization + deterministic value formatting + deterministic-id versioning + the three metadata fields (`packageVersion`/`canonicalizationScheme`/`hashAlgorithm` on domain + internal manifest DTO + EF rows/mapper + minimal migration) + tests + `lld_01` → STOP. (Signing, hash-chain topology, public summary, lld_02/03, API routes, business state, verifier engine, or persistence beyond the three fields = STOP.)
- Cross-verification against an independent JCS implementation fails → STOP (the portability claim is unproven; do not accept).

## 8. Recommended next action
Submit brief + kickoff for GPT + Codex review (Tier-2: reviewers should confirm the JCS approach is conformant and the cross-verification proof is real). Converge, dispatch, build, Contractor adversarial spot-check (re-run the independent cross-verification). Then TIP-66 (signing) builds on the stable JCS hash.
