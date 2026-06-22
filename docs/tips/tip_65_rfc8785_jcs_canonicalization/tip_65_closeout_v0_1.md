# TIP-65 RFC 8785 JCS Canonicalization — Closeout v0.2

**File:** `docs/tips/tip_65_rfc8785_jcs_canonicalization/tip_65_closeout_v0_1.md`
**Version:** 0.2
**Status:** Closed — Tier-2 / EBS-01 code change accepted + committed `f494025` (Contractor adversarial spot-check ACCEPT after one patch round)
**Date:** 2026-06-22
**Baseline:** `951694a docs: close TIP-64 S1 evidence-integrity consolidation (lld_01 v0.2)` (master).
**Purpose:** Close TIP-65 after replacing the implementation-deterministic Web-JSON canonicalization with RFC 8785 JCS, recording durable hash metadata, and resolving Tier-2 open item T2-1.

## Changelog

### v0.2 — Post-commit finalize
- Updated disposition + next-step to post-commit (TIP-65 committed `f494025`, parked-not-pushed); corrected the Exact Files list (`ProjectionBoundaryTests` is in `TagEkyc.ContractTests`; added `TagEkycDbContext.cs` and `Tip06CompletionApplicationTests.cs`); softened the JCS number-path wording to match the documented limit.

### v0.1 — Initial closeout
- Closed TIP-65 as a Tier-2 code change. Recorded outcome vs intent, the build, the two-round adversarial spot-check (build then patch), the fixes, the spun-off debt, validation, the review ladder, lessons, and the next step (signing = TIP-66).

## Status / Disposition

```text
COMMITTED f494025 (master, parked-not-pushed); T2-1 RESOLVED (S1 evidence hash chain); T2-2 (signing) STILL OPEN → TIP-66; decision-basis binding REGISTERED AS P1 DEBT (future TIP after TIP-66)
```

## Outcome vs Intent

| Intended outcome | Actual result | Status |
| --- | --- | --- |
| Replace Web-JSON canonicalization with RFC 8785 JCS in `HashCanonical`/`DeterministicGuid` | `EvidenceCanonicalization.cs` canonicalizes via JCS (ordinal/UTF-16 key order, minimal string escaping) for the S1 evidence graph; wired through both. The number path is defensive/not load-bearing — evidence forbids raw numbers (and `FormatNumber` is not proven vs the full official RFC number vectors) | Accepted |
| Pin deterministic timestamp/guid formatting | Timestamp `yyyy-MM-ddTHH:mm:ss.fffffffZ` UTC invariant via converters; guids `N` | Accepted |
| Record three durable + retrievable metadata fields | `packageVersion=evidence-package-v2`, `canonicalizationScheme=rfc8785-jcs-v1`, `hashAlgorithm=sha256` — hashed in manifest body + persisted (EF rows/mapper + minimal migration) + exposed via the INTERNAL manifest DTO | Accepted |
| Public summary + lld_02/03 untouched (scope-bound) | Public `EvidencePackageSummaryDto` unchanged (contract test asserts `DoesNotContain`); lld_02/03 not touched | Accepted |
| Version deterministic ids + golden vectors | Golden vectors for `decisionId`/`evidencePackageId`/completion `auditEventId` are hard-coded literals | Accepted |
| Legacy + verifier rule (rule + fixtures, no engine) | Classifier + fail-closed RULE; legacy backfill default = `web-json-deterministic-v1`; fail-closed enforced at the EF read path (see Patch) | Accepted (after patch) |
| Independent cross-verification | Node.js (non-app) reproduces the app hashes; numeric convention adopted so the number path is not load-bearing | Accepted (after patch) |
| Update lld_01, mark T2-1 resolved | `lld_01` v0.3; T2-1 resolved, T2-2 kept open | Accepted |

## Build

Codex built TIP-65 v0.3 from baseline `951694a`. Self-report: `dotnet build` clean; `dotnet test` 161/161 (pre-patch). Not committed at build time — the Contractor spot-check + patch followed (see below), then committed as `f494025` (163/163 post-patch).

## Contractor Adversarial Spot-Check (EBS-01 — highest stakes) — verified on CODE, not the report

### Round 1 (build) — found 2 real gaps + 1 overclaim the green tests hid
- **MEDIUM — fail-closed was dead code.** `IsKnownHashMetadataCombination`/`IsCurrent`/`IsLegacy` had ZERO production callers (grep-verified); `lld_01` stated "unknown combinations fail closed" but nothing enforced it.
- **LOW/latent — number canonicalization not independently verified.** The "independent" Node cross-verify used naive `JSON.stringify` over string-only payloads, so RFC 8785 number formatting had no real oracle, and the conformance test was a hand-written subset. Mitigated: nothing numeric is ever hashed (verified — all hashed graphs are strings/bools/string-arrays; `Confidence`/`RiskScore` are not in the hashed graph).
- **LOW — "legacy fixture" overclaim.** The legacy test classified string tuples; it did not re-hash a real legacy object.
- Self-correction: my initial "G-format is wrong" claim was softened — .NET 8 `ToString("G")` is shortest-round-trip (since Core 3.0), so `FormatNumber` was closer to conformant than first stated.

### Patch (Homeowner-approved) — and Round 2 = ACCEPT
- **Fix A (fail-closed):** `EnsureKnownHashMetadataCombination` wired into `DomainRowMapper.ToDomain` for both package and manifest (read direction, on the row's persisted values), throwing `EvidenceHashMetadataException` on an unknown tuple. Verified: Postgres integration test persists a `bogus-scheme` row and asserts `ThrowsAsync` on all 3 read paths (package-by-id, package-by-session, manifest-by-package), while current and legacy tuples read OK.
- **Fix B (numeric convention — Homeowner's design):** evidence forbids raw JSON numbers; numerics are encoded as strings (integers invariant; decimals `decimal` `F6` invariant), pinned by the scheme version. Enforced by a tripwire test (`AssertCanonicalJsonHasNoNumbers`) applied to all 8 actual hashed seeds (decisionSeed, packageIdSeed, auditEventIdSeed, completionAuditPayload, artifactHashSet, manifestBody, packageBody, manifestHashSeed); recursive, fails on any `JsonValueKind.Number`. `FormatNumber` kept as defensive/unreachable for evidence.
- **Fix C (honesty):** legacy test renamed to tuple-classification; `lld_01` states S1 has no legacy canonicalizer/corpus and that fail-closed is now enforced at the EF read path.
- **Regression (critical):** the golden GUID/hash vectors are byte-identical to pre-patch (decisionId `b4cc7b99…`, packageId `a207cfcd…`, auditId `996ac4c4…`, manifestBodyHash `sha256:e8aa856e…`, packageHash `sha256:0a3cbc9b…`, manifestHash `sha256:124c36f8…`) — the patch did not alter any current hash.

**Verdict: ACCEPT.** Post-patch `dotnet test` 163/163.

## Debt / Gap Final State

| Debt/gap | Final state | Resolved? | Next gate |
| --- | --- | --- | --- |
| T2-1 canonicalization not JCS (EBS-01) | RFC 8785 JCS adopted, versioned, persisted, fail-closed at read | **Yes** (S1 evidence hash chain) | — |
| T2-2 placeholder signatures (EBS-07) | Unchanged | No (by design) | **TIP-66** — `IEvidenceSigner` + SoftHSM dev + JWS; prod HSM/KMS P0 |
| Decision basis not hash-bound (Confidence/reasonCodes/RiskScore) | Persisted + append-only-protected but NOT hash-bound; found in spot-check | No | P1 debt in `phase1_scope_and_debt_registry`; future evidence-model TIP (after TIP-66), needs legal lens |
| `FormatNumber` not proven vs official RFC vectors | Defensive, unreachable for evidence (tripwire enforces no raw numbers) | Accepted | Revisit only if the numeric convention changes |

## Exact Files Changed (TIP-65 allowlist)

- `src/TagEkyc.Application/VerificationSessions/EvidenceCanonicalization.cs` (new — JCS, metadata constants, classifier, `EvidenceHashMetadataException`)
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` (canonicalization wired; metadata in manifest body/package)
- `src/TagEkyc.Domain/EvidencePackage.cs`, `EvidenceManifest*` / `src/TagEkyc.Contracts/InternalAudit/Manifest/ManifestContracts.cs` (metadata fields on domain + internal manifest DTO)
- `src/TagEkyc.Infrastructure/Persistence/Entities/EvidencePackageRow.cs`, `EvidenceManifestRow.cs`; `DomainRowMapper.cs` (persist + readback + fail-closed guard); `Migrations/20260622033429_Tip65EvidenceHashMetadata.*` (+ ModelSnapshot)
- `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` (entity config for the new metadata columns)
- `tests/TagEkyc.UnitTests/Tip65EvidenceCanonicalizationTests.cs` (new); `tests/TagEkyc.IntegrationTests/PostgresPersistenceSliceTests.cs`; `tests/TagEkyc.ContractTests/ProjectionBoundaryTests.cs`; `tests/TagEkyc.UnitTests/Tip06CompletionApplicationTests.cs`
- `docs/lld_01_data_model_v0_1.md` (v0.3, T2-1 resolved); `docs/tips/tip_65_*` (brief/kickoff v0.3 + this closeout); `docs/tips/README.md` (v1.18); `docs/phase1_scope_and_debt_registry_v0_1.md` (decision-basis P1 debt)

Excluded from commit (known-dirty): `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, all `bin/obj`.

## Review Ladder Summary

Contractor drafts → GPT/Codex review/converge → Codex builds → Contractor adversarial spot-check.
- Brief/kickoff converged over **3 external review rounds** (round 1: under-scoped metadata not durable; round 2: scope-expansion not propagated to STOP/migration/wording; round 3: bound-decision not propagated to brief/README/summaries) + Contractor cross-doc grep self-checks → v0.3, READY TO DISPATCH.
- Build → **Contractor spot-check round 1** (2 gaps + 1 overclaim) → Homeowner-approved patch → **spot-check round 2 = ACCEPT**.
Every round produced material findings (under-scope, scope-propagation lag, dead-code fail-closed, oracle-blind-to-numbers) — not bikeshed.

## STOP/RRI Result

No STOP/RRI during build. The numeric-encoding policy was a Homeowner design decision (better than the Contractor's tripwire-only proposal), not a STOP.

## Lessons Learned

- Third confirmation (after the persistence-slice FK and injector/CAS findings) that on a legal/evidence surface, **green tests + a complete build report ≠ correct**: only adversarial CODE-reading aimed at the right tier caught the unenforced fail-closed classifier and the oracle that didn't test the hard part. This is exactly the Homeowner's "test đúng ≠ chứng cứ đúng" fear, vindicated.
- The decisive regression check for a canonicalization change is **golden vectors byte-identical** — proves the refactor didn't silently move any hash.
- **Numbering discipline:** a follow-up TIP must not be pre-allocated a number ahead of its turn (a decision-basis TIP was mis-created as "TIP-67" skipping the reserved signing slot TIP-66; removed — it is debt-only until activated).
- Contractor cross-doc grep is now standard after every scope-changing edit (it caught a §9 leftover this TIP; it did not prevent 3 propagation rounds — front-load it).

## Recommended Next Step

TIP-65 is committed as `f494025` on master (allowlist; known-dirty excluded; parked-not-pushed — push only on Homeowner authorization). Next: **TIP-66** (signing / T2-2) builds on this stable JCS hash. The decision-basis binding stays P1 debt for a later evidence-model TIP (needs the legal lens).
