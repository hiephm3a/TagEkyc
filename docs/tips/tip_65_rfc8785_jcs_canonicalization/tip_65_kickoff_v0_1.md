# TIP-65 RFC 8785 JCS Canonicalization — Kickoff v0.1

**File:** `docs/tips/tip_65_rfc8785_jcs_canonicalization/tip_65_kickoff_v0_1.md`
**Version:** 0.3
**Status:** Draft — dispatch-ready spec (Tier-2 / EBS-01, code change); GPT/Codex review round 2 patches applied; pending convergence confirmation before build
**Date:** 2026-06-21
**Baseline:** `951694a` (master). Planning brief: `tip_65_planning_brief_v0_1.md` v0.3.
**Purpose:** Exact execution contract for Codex to replace Web-JSON canonicalization with RFC 8785 JCS (portable, independently-reproducible), recording the three metadata fields (`packageVersion` + `canonicalizationScheme` + `hashAlgorithm`) with independent cross-verification. Resolves T2-1.

## Changelog

### v0.3 — GPT/Codex review round 2 patches (2026-06-21)
- P1: §7 STOP no longer contradicts §3.3 — STOP only on persistence/contract changes BEYOND the three authorized metadata fields; the additive fields + minimal migration are explicitly in scope (§3.3/§4/§6/§7).
- P1: authorized the minimal EF migration for exactly the three metadata columns (§3.3/§2).
- Scope-bound (Contractor decision): metadata exposed via the INTERNAL manifest DTO only; public BusinessConsumer `EvidencePackageSummaryDto` unchanged → lld_02/03 OUT of scope (public-summary exposure deferred to a consumer-verifier slice).
- P2: independent cross-verification MUST NOT reuse the app canonicalizer/JCS package; report both implementations + why independent (§5).
- P2/P3: replaced "scheme + version" summaries with the three fields (`packageVersion`+`canonicalizationScheme`+`hashAlgorithm`).

### v0.2 — GPT/Codex review patches (2026-06-21)
- P1: metadata made durable+retrievable — added `hashAlgorithm`; the three fields hashed + persisted (EF rows/mapper) + exposed (read DTOs) (§3.3); authorized the minimal additive contract/persistence delta; expanded source list (§2) and DoD (readback test).
- P1: legacy classification + verifier RULE (§3.5) — fail-closed, future-field safety, re-issue-not-mutate; documented rule + test fixtures, NOT a built verifier engine (S1 has none).
- P1: DeterministicGuid versioning explicit + golden vectors (§3.4).
- P1: timestamp PINNED `yyyy-MM-ddTHH:mm:ss.fffffffZ` full-precision (§3.2) — no longer "confirm in review".
- P2: resolved the chain-vs-metadata contradiction — topology unchanged, field set adds the 3 metadata fields, JCS key-order replaces declaration-order (§4/§5/§6).
- P3: machine-stable lowercase constants pinned.

### v0.1 — Initial kickoff
- Authored dispatch-ready spec: source-of-truth, exact scope (canonicalization + deterministic value formatting + scheme/version + tests + lld_01), DoD with independent cross-verification, scope floor, STOP/RRI, validation, report.

## 1. Objective / precondition
Replace the evidence canonicalization with **RFC 8785 (JCS)** so the S1 evidence hashes are portable and independently reproducible; record the three metadata fields (`packageVersion` + `canonicalizationScheme` + `hashAlgorithm`); update `lld_01` and mark **T2-1 resolved**. **Precondition: GPT + Codex review converged + Homeowner dispatch.** Until then, do not build. (Signing/T2-2 is TIP-66 and depends on this stable hash — do not implement signing here.)

## 2. Source of truth + impacted surfaces (read first)
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` — current `HashCanonical` (`JsonSerializerDefaults.Web`), `DeterministicGuid`, `CanonicalJsonOptions`, the `manifestBody`/`packageHash`/`manifestHash` construction, `PackageVersion` constant, timestamp flow (`operationNow`/`completedAt`).
- `src/TagEkyc.Domain/HashRef.cs` (the `sha256:` format); `src/TagEkyc.Domain/EvidencePackage.cs`, `EvidenceResult.cs`, `SignaturePlaceholderStatus.cs`.
- `src/TagEkyc.Contracts/InternalAudit/Manifest/ManifestContracts.cs` (`EvidenceManifestDto` — the metadata is exposed here); `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` (read to CONFIRM the public package summary is NOT changed — public exposure deferred).
- `src/TagEkyc.Infrastructure/Persistence/Entities/EvidencePackageRow.cs`, `EvidenceManifestRow.cs`, `DomainRowMapper.cs` (the metadata must be PERSISTED + read back); the minimal EF migration for the three columns.
- `docs/lld_01_data_model_v0_1.md` Evidence-Integrity section (update + mark T2-1 resolved). `docs/lld_02_*`, `lld_03_*` are OUT of scope (the public summary is unchanged).
- RFC 8785 (JSON Canonicalization Scheme) + its official test vectors.

## 3. Exact scope

### 3.1 Adopt RFC 8785 JCS
- Replace the canonical-JSON step in `HashCanonical` (and the `DeterministicGuid` input) so the JSON is canonicalized per **RFC 8785** (sorted object member keys by UTF-16 code unit, canonical number formatting, minimal whitespace, canonical string escaping). The SHA-256-over-`"{label}\n{canonicalJson}"` framing and `sha256:<lowercase-hex>` output stay.
- You may extract a dedicated `EvidenceCanonicalizer` (Application) for testability/reuse; keep it pure (no Infrastructure/EF/IO).
- **Implementation:** use a vetted RFC 8785 JCS library for .NET 8 **or** implement per RFC 8785. Either way it MUST pass the RFC 8785 conformance vectors byte-for-byte.

### 3.2 Deterministic value formatting (PINNED — not left for builder interpretation)
JCS canonicalizes structure but preserves string values, so non-JSON-native values are pinned:
- **Timestamps:** convert to UTC and format `yyyy-MM-ddTHH:mm:ss.fffffffZ` (invariant culture, fixed 7 fractional digits, `Z` suffix). Full-precision (lossless vs the stored `CompletedAt`) and round-trip reproducible by a non-.NET verifier. (Chosen over whole-second to avoid truncation/mismatch with the stored timestamp; no precision is lost vs as-built.)
- **Guids:** one fixed format consistently (the as-built `N` format unless code shows otherwise).
- Apply BEFORE canonicalization — emit explicit preformatted strings, never serializer-default `DateTimeOffset`. Cover the formats with golden vectors.

### 3.3 Evidence hash metadata — DURABLE + RETRIEVABLE (not just a hash input)
Record three metadata fields as machine-stable lowercase constants:

```
packageVersion         = evidence-package-v2
canonicalizationScheme = rfc8785-jcs-v1
hashAlgorithm          = sha256
```

These MUST be: (1) included in the **manifest body** (covered by `manifestBodyHash`); (2) **persisted** (`EvidencePackage` domain record + `EvidencePackageRow`/`EvidenceManifestRow` + `DomainRowMapper`; **the minimal EF migration adding ONLY these three columns is authorized** — no other schema/index/constraint/trigger/FK/provider change); (3) **exposed through the INTERNAL manifest DTO `EvidenceManifestDto`** (the verifier surface) so a verifier can read which scheme/algorithm produced the hash. A transient hash-input-only field is NOT sufficient — that fails the no-silent-switch requirement.

**Scope bound (Contractor decision):** the **public BusinessConsumer package summary (`EvidencePackageSummaryDto`) is NOT changed** in TIP-65 — public-summary exposure of the metadata is **deferred** to a future consumer-verifier slice. This keeps `lld_02`/`lld_03` (which document the public summary) OUT of scope (avoids public-API expansion before a consumer needs it; internal manifest is sufficient for S1).

**The ONLY authorized additive contract/persistence delta in TIP-65: exactly these three metadata fields on the domain record + internal manifest DTO + EF rows/mapper + the minimal migration.** No other field, route, business-state, response, public-DTO, or persistence semantic may change.

### 3.4 Deterministic id derivation (versioned, in scope)
`DeterministicGuid` uses the same canonical input style, so it changes with the canonicalization:
- All `DeterministicGuid` inputs are canonicalized under the package `canonicalizationScheme` (rfc8785-jcs-v1) with explicit labels and stable input field sets (keep the as-built labels/fields; only the canonicalization changes).
- Add golden vectors for **every** deterministic id produced on the completion/package/audit path (`decisionId`, `evidencePackageId`, completion `auditEventId`).
- Legacy ids (if any) remain interpretable under `web-json-deterministic-v1`; they MUST NOT be re-derived or mutated.

### 3.5 Legacy classification + verifier RULE (documented rule + tests — NOT a built engine)
TagEkyc S1 has no runtime verifier component; TIP-65 does NOT build one. It records the metadata (§3.3) and documents the rule a verifier MUST follow, proven by test fixtures:
- A verifier selects canonicalization/hashing by the package's own `packageVersion` + `canonicalizationScheme` + `hashAlgorithm`, **never by the latest runtime default**.
- **Unknown/inconsistent combinations fail closed** (reject, do not reinterpret under the latest canonicalizer).
- **Legacy** = any package produced before TIP-65: `packageVersion = tip-06-localdev-v1`, `canonicalizationScheme = web-json-deterministic-v1`, `hashAlgorithm = sha256`, and is explicitly NOT production/legal-reliance evidence. If a legacy package lacks the explicit metadata fields, it is mapped only by the legacy `packageVersion` compatibility rule.
- **Re-issuing** a legacy package under JCS creates a NEW package/version/hash; old hashes MUST NOT be mutated in place.
- **Future-field safety:** adding a field to a later package shape MUST NOT change verification of an old package verified under its recorded `packageVersion`+`canonicalizationScheme`.
Document this rule in `lld_01`; prove it with the fixtures/tests in §6.

### 3.6 Update lld_01
- Update Evidence-Integrity §Canonicalization to the JCS scheme + pinned timestamp/guid formatting + the three metadata fields + the §3.5 verifier rule. Mark **T2-1 resolved** in §Tier-2 (keep T2-2 open). Bump lld_01 Version (→ 0.3) + changelog.

## 4. Out of scope (do not touch)
Signing / `IEvidenceSigner` / JWS (T2-2 = TIP-66); the hash-chain TOPOLOGY (`manifestBodyHash → packageHash → manifestHash` stays); any contract/persistence change BEYOND the three authorized metadata fields (§3.3); the **public BusinessConsumer `EvidencePackageSummaryDto`** (public-summary exposure deferred → so **`lld_02`/`lld_03` are out of scope**); API routes, business state machine, response semantics; a runtime verifier engine; lld_04, hld, other docs.
> Note: TIP-65 DOES make a minimal additive change to the manifest-body field set + the domain record + the **internal** manifest DTO + EF rows/mapper + a minimal migration — limited to `packageVersion`/`canonicalizationScheme`/`hashAlgorithm` (§3.3). That is authorized, not creep. The public summary + lld_02/03 stay untouched.

## 5. Definition of Done (verifiable)
- [ ] Canonicalization produces RFC 8785 JCS output; passes the **RFC 8785 conformance test vectors** byte-for-byte.
- [ ] Timestamps use `yyyy-MM-ddTHH:mm:ss.fffffffZ` (UTC, invariant); guids a fixed format; golden vectors cover them.
- [ ] The three metadata fields (`packageVersion=evidence-package-v2`, `canonicalizationScheme=rfc8785-jcs-v1`, `hashAlgorithm=sha256`) are: in the manifest body (hashed), **persisted** (EF rows + mapper + minimal migration), and **read back via the INTERNAL manifest DTO** — proven by a readback test asserting each field round-trips. The public BusinessConsumer `EvidencePackageSummaryDto` is asserted UNCHANGED (deferred).
- [ ] **Independent cross-verification test:** a SECOND RFC 8785 implementation reproduces the app's `manifestBodyHash`/`packageHash`/`manifestHash` (SHA-256). It MUST NOT reuse the app canonicalizer or the same JCS package instance production code calls (preferably a non-.NET RFC 8785 reference; acceptable fallback = a separately-implemented verifier path sharing no canonicalization code). The report names both implementations + why independent. Without it, do not accept.
- [ ] Golden vectors for **every deterministic id** (`decisionId`/`evidencePackageId`/completion `auditEventId`) under the JCS scheme.
- [ ] **Legacy fixture test:** a `tip-06-localdev-v1`/`web-json-deterministic-v1` package verifies only under the legacy canonicalizer, is explicitly not JCS-compliant, and is not production/legal-reliance.
- [ ] **Fail-closed test:** an unknown/inconsistent `packageVersion`+`canonicalizationScheme`+`hashAlgorithm` combination is rejected, not reinterpreted under the latest canonicalizer.
- [ ] **Future-field regression test:** adding a field to a later package shape does not change verification of an old package verified under its recorded `packageVersion`+`canonicalizationScheme`.
- [ ] Hash-chain TOPOLOGY unchanged; canonical input field set changes ONLY by adding the three metadata fields; tests assert field NAMES, not declaration order.
- [ ] Full `dotnet test` green (report counts). Append-only triggers, FK, xmin untouched (only the additive metadata columns/fields are added).
- [ ] `lld_01` updated to JCS + verifier rule, T2-1 marked resolved, Version + changelog bumped.

## 6. Scope floor (anti-creep)
- Allowed surfaces ONLY: canonicalization code (+ optional `EvidenceCanonicalizer`), the metadata constants, the three additive metadata fields across `EvidencePackage` domain + **internal** `EvidenceManifestDto` + EF rows + `DomainRowMapper` + minimal migration (§3.3), `DeterministicGuid` versioning, tests, and `lld_01`. Anything else (signing, the public `EvidencePackageSummaryDto`, lld_02/03, any other contract field, API routes, business state, a runtime verifier engine, other docs) = defect → STOP.
- Hash-chain TOPOLOGY unchanged (`manifestBodyHash → packageHash → manifestHash`); the canonical input field set changes ONLY by adding the three metadata fields; JCS key-ordering replaces anonymous-object declaration-order semantics — assert field NAMES, not declaration order.

## 7. STOP/RRI
- No conformant JCS (vetted lib unavailable AND inline impl can't pass RFC vectors in scope) → STOP, report.
- Independent cross-verification fails → STOP (portability unproven; do not accept).
- STOP only on changes BEYOND §3 scope: signing, hash-chain topology, API routes, business state, a runtime verifier engine, the public BusinessConsumer summary / lld_02 / lld_03, or persistence/schema/contract changes beyond the three authorized metadata fields (incl. trigger/FK/xmin/append-only/provider changes). The additive `packageVersion`/`canonicalizationScheme`/`hashAlgorithm` on the domain record + internal manifest DTO + EF rows/mapper + the minimal migration ARE in scope — do NOT STOP on those.

## 8. Validation + report
- Run: `dotnet build`; `dotnet test TagEkyc.sln` (unit + arch + integration — Postgres container as before); the RFC 8785 conformance test; the independent cross-verification test; `git diff --stat`.
- Do NOT commit (await Contractor review).
- Report: 5-line summary + (a) JCS implementation chosen (lib name+version or inline) + RFC-vector pass; (b) the pinned timestamp/guid formats; (c) the three metadata fields — where hashed, persisted (which EF rows/columns + mapper), and read back (which DTOs) + the readback test; (d) the independent cross-verification test (which 2nd implementation, result); (e) deterministic-id golden vectors; (f) legacy fixture + fail-closed + future-field regression test results; (g) full test counts; (h) lld_01 update.

## 9. Review after build (Contractor)
Contractor adversarial spot-check (EBS-01 — highest stakes): **re-run the independent cross-verification myself** (an independent JCS reproduces the app hashes); confirm RFC 8785 conformance; confirm the hash-chain TOPOLOGY is unchanged and the canonical field set changed ONLY by the three metadata fields (assert field names, not declaration order); confirm the three metadata fields are hashed + persisted + read back (readback test); confirm the deterministic-id golden vectors + legacy/fail-closed/future-field tests; confirm scheme/version/algorithm recorded + lld_01 updated + T2-1 marked resolved; confirm no signing/API/business drift and no persistence change beyond the three authorized metadata columns. Then closeout. T2-2 (signing) proceeds as TIP-66 on this stable hash.
