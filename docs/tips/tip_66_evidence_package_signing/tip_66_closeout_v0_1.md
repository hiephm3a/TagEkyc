# TIP-66 Evidence Package Signing (IEvidenceSigner + JWS) — Closeout v0.1

**File:** `docs/tips/tip_66_evidence_package_signing/tip_66_closeout_v0_1.md`
**Version:** 0.1
**Status:** Closed — Tier-2 / EBS-07 code change accepted (Contractor adversarial spot-check ACCEPT after one patch round); ready for Codex commit + push
**Date:** 2026-06-22
**Baseline:** `a98f278 docs: finalize TIP-65 closeout v0.2 + README v1.18 (post-commit)` (master).
**Purpose:** Close TIP-66 after adding a real evidence-package signature (`IEvidenceSigner` + ES256 JWS over the stable `manifestHash`) and resolving Tier-2 open item T2-2 at the dev level.

## Changelog

### v0.1 — Initial closeout
- Closed TIP-66 as a Tier-2 code change. Recorded outcome vs intent, Decision/Branch Disposition, the build + one patch round (crypto-tamper test + signedAt binding), the two-round adversarial spot-check, the verified facts, the debt, the exact file list, a playbook-shaped Review Ladder (V1/V2/V3), GDrive sync/hash posture, lessons, and the next step.

## Status / Disposition

```text
READY_TO_COMMIT_TIP_66 (Codex commits + pushes); T2-1 already resolved (TIP-65); T2-2 RESOLVED AT DEV LEVEL; prod HSM/KMS + CA cert + non-repudiation + trusted timestamp (RFC 3161) STILL P0 DEBT
```

## Outcome vs Intent

| Intended outcome | Actual result | Status |
| --- | --- | --- |
| `IEvidenceSigner` abstraction (one stable contract) | `EvidenceSignerPorts.cs` — `SignAsync(EvidenceSignatureRequest)`→`EvidenceSignatureEnvelope`; no algorithm-specific port | Accepted |
| ES256 local/P12 dev adapter | `Infrastructure/Signing/LocalDevEs256JwsEvidenceSigner.cs` — real ECDSA P-256; key from configured P12 or in-process generated dev key | Accepted |
| Attached compact JWS over a binding claim | claim `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}`; header `alg=ES256`+`kid` | Accepted |
| Durable self-describing envelope, single source | `signatureFormat`/`signatureScheme`/`signatureAlgorithm`/`keyId`/`signedAt`/`signatureValue` on the **manifest** row only (migration alters `evidence_manifests` only) + internal DTO; package row NOT extended | Accepted |
| `Signed` status + legacy preserved | domain + DTO enums gain `Signed`; legacy packages stay `PlaceholderUnverified` (nullable cols); mapper fail-closed both directions | Accepted |
| Signing downstream/additive | signer called after `manifestHash`; signature never feeds the hash chain; golden vectors byte-identical | Accepted |
| Verify by test, no runtime engine | round-trip + full negative matrix in test fixtures; no runtime verify endpoint | Accepted |
| Docs | `lld_01` (signature + verifier rule, T2-2 dev-resolved + prod limits), minimal `lld_03` enum note | Accepted |

## Decision / Branch Disposition

| Decision / option | Final disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| ES256 (ECDSA P-256) for the dev signature | Accepted | Modern, compact, KMS-native; behind `IEvidenceSigner` so prod can swap | — |
| Dev backend = local key / P12 | Accepted | Simplest CI-portable real signer; proves the abstraction | SoftHSM/PKCS#11 = optional later adapter |
| One `IEvidenceSigner` contract (no algorithm-specific port) | Accepted | Stable abstraction; RSA/HSM/KMS are adapters behind it | — |
| Attached-claim JWS + single-source manifest envelope | Accepted | Self-describing, binds context; one authoritative row | — |
| Production HSM/KMS or CA-issued cert | Deferred | Out of S1 scope; needs key lifecycle + legal cert | **P0** before legal reliance |
| Runtime signature-verification endpoint | Deferred | No consumer verifier yet; avoids dead-code engine (TIP-65 lesson) | Later verifier slice |
| RFC 3161 / trusted third-party timestamp | Deferred | `signedAt` bound into the signature is tamper-evident within our own signature, not a trusted TSA | **P0** prod debt |
| Payload signature + webhook signature | Out of scope | Separate evidence/transport surfaces | Separate debts |
| Public exposure of the JWS value | Deferred | Internal manifest is the S1 verifier surface | Later, if a consumer verifier needs it |
| Enum rename to drop "Placeholder" | Deferred (cosmetic) | Functional with `Signed` added | Later hygiene pass |

## Build + Patch

- Build: Codex implemented TIP-66 v0.2 from baseline `a98f278`; `dotnet build` clean, `dotnet test` 167/167.
- Patch (after spot-check round 2): **Fix A** — added `ReplaceSignature` (mutate `parts[2]`) + `VerifySignatureOnly` (bypass the echo-compare) so a test proves cryptographic tamper-rejection of both signature bytes and signed content; **Fix B** — bound `signedAt` into the signed claim (signer uses one value for the JWS payload + the envelope; verifier rebuilds the claim with `envelope.SignedAt`). Re-test 167/167; golden vectors unchanged.

## Contractor Adversarial Spot-Check (EBS-07) — verified on CODE, not the report

### Round 2 (build) — core ACCEPT + 2 gaps
Confirmed on code: golden `manifestHash`/`packageHash`/`manifestBodyHash` byte-identical (signing did not perturb the hash chain); real ECDSA P-256; envelope single-source on the manifest row (package row clean, migration alters `evidence_manifests` only); mapper fail-closed both directions; no key material committed (dev key generated in-process; P12 from config; the P12 test self-signs at runtime + deletes); public `EvidencePackageSummaryDto` has no JWS field; `lld_01`/`lld_03` honest. Gaps found: (1) LOW-MED — the test verifier's echo-compare short-circuited payload-tamper before `VerifyData`, so cryptographic tamper-rejection of signed content was not isolated by any test; (2) LOW — `signedAt` was recorded in the envelope but NOT in the signed claim (signing time not tamper-evident).

### Round 3 (patch) — ACCEPT
Verified on code: `Tip66EvidenceSigningTests` now asserts `VerifySignatureOnly` returns false for a content-tampered payload AND for a `ReplaceSignature` signature-byte mutation (header+payload intact) — the cryptographic check itself rejects forgery/tamper. `signedAt` is in the signed claim and the envelope with one value (`FormatTimestamp`), and the verifier rebuilds the claim from `envelope.SignedAt` (a tampered `signedAt` is rejected). Golden vectors remain byte-identical. The wide test edits are legitimate: `+1`-line DI wiring of `IEvidenceSigner` into existing completion tests, and `DomainInvariantTests`/`ProjectionBoundaryTests` correctly updated to the new "PlaceholderUnverified + Signed" invariant (the old "placeholder-only" invariant was intentionally superseded), not assertion-weakening.

**Verdict: ACCEPT.** 167/167.

## Debt / Gap Final State

| Debt/gap | Final state | Resolved? | Next gate |
| --- | --- | --- | --- |
| T2-2 placeholder signatures (EBS-07) | Real ES256 JWS over manifestHash, self-describing, fail-closed | **Dev level only** | Production: HSM/KMS or CA-issued cert behind the same `IEvidenceSigner`; **P0** |
| Trusted signing time | `signedAt` is now bound into the signature (tamper-evident within our own signature) | Partial | A trusted third-party timestamp (RFC 3161 / TSA) is **P0 prod debt** |
| Runtime signature verification | Verifier exists as test fixtures only; documented in `lld_01` | By design | A consumer/runtime verifier is a later slice (no dead-code engine now) |
| Payload + webhook signatures | Unchanged (`PlaceholderUnverified`) | No (out of scope) | Separate debts |
| Decision basis not hash-bound | Unchanged | No | P1 debt (registered) — later evidence-model TIP, needs legal lens |
| Enum name `SignaturePlaceholderStatus` now holds `Signed` | Functional; name is a misnomer | Deferred (cosmetic) | Optional rename in a later hygiene pass |

## Exact Files Changed (TIP-66 allowlist)

New:
- `src/TagEkyc.Application/Ports/EvidenceSignerPorts.cs` (`IEvidenceSigner`, `EvidenceSignatureRequest`)
- `src/TagEkyc.Domain/EvidenceSignatureEnvelope.cs`
- `src/TagEkyc.Infrastructure/Signing/` (`LocalDevEs256JwsEvidenceSigner.cs` + options)
- `src/TagEkyc.Infrastructure/Persistence/Migrations/20260622141420_Tip66EvidencePackageSigning.cs` (+ `.Designer.cs`)
- `tests/TagEkyc.IntegrationTests/Tip66EvidenceSigningTests.cs`, `Tip66EvidenceSignatureTestVerifier.cs`; `tests/TagEkyc.UnitTests/TestEvidenceSigner.cs`
- `docs/tips/tip_66_evidence_package_signing/` (brief + kickoff v0.2 + this closeout)

Modified:
- `src/TagEkyc.Api/Program.cs` (register `IEvidenceSigner`)
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` (sign after manifestHash; status `Signed`)
- `src/TagEkyc.Contracts/Common/ContractEnums.cs` (`SignaturePlaceholderStatusDto.Signed`); `src/TagEkyc.Domain/SignaturePlaceholderStatus.cs` (`Signed`)
- `src/TagEkyc.Contracts/InternalAudit/Manifest/ManifestContracts.cs` (envelope fields on `EvidenceManifestDto`)
- `src/TagEkyc.Infrastructure/Persistence/Entities/EvidenceManifestRow.cs`, `DomainRowMapper.cs`, `TagEkycDbContext.cs`, `Migrations/TagEkycDbContextModelSnapshot.cs`
- `tests/...` DI wiring (`Tip04/05/06/07/08/13`, `Tip06CompletionApplicationTests`) + `DomainInvariantTests` + `ProjectionBoundaryTests` + `PostgresPersistenceSliceTests` (legacy/signing tests)
- `docs/lld_01_data_model_v0_1.md`, `docs/lld_03_api_contracts_v0_1.md`, `docs/tips/README.md`

Excluded (known-dirty): `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, all `bin/obj`.

## Review Ladder Summary

Model: Contractor drafts → GPT/Codex review/converge → Codex builds → Contractor adversarial spot-check. Mapped to the V1/V2/V3 schema:

- **V1 result (spec review — brief/kickoff):** ACCEPT WITH PATCHES (GPT + Codex; 1 round + a consistency tail) → v0.2 READY.
- **V1 files/surfaces inspected:** `tip_66_planning_brief`, `tip_66_kickoff`, README row, vs the as-built signature model (`SignaturePlaceholderStatus`, `EvidencePackage*`/`EvidenceManifest*`, `BusinessConsumerContracts`).
- **V1 findings:** port must sign a request/envelope claim object (not a bare hash); ONE `IEvidenceSigner` (no "different port"); single-source manifest; full negative matrix; legacy placeholder rule; minimal `lld_03` note; SoftHSM supersession. All applied.
- **V2 result (build spot-check, on CODE):** ACCEPT WITH PATCHES.
- **V2 files/surfaces inspected:** `EvidenceSignerPorts`, `LocalDevEs256JwsEvidenceSigner`, `VerificationCompletionApplicationService`, `EvidenceManifestRow`/`EvidencePackageRow`, the migration, `DomainRowMapper`, `Tip66EvidenceSigning*` tests, `lld_01`/`lld_03`, golden vectors.
- **V2 findings:** (1) crypto tamper-rejection not isolated (test echo-compare short-circuited before `VerifyData`); (2) `signedAt` recorded but unsigned. Both patched.
- **V3 free review result:** ACCEPT on technical re-verify (patch); a separate closeout-governance review added P2/P3 doc patches (this Review Ladder expansion, Decision/Branch Disposition, GDrive posture).
- **V3 free-roam areas sampled:** the signing call site vs the hash-chain construction; the migration vs the package row; the public BusinessConsumer DTO; the test blast-radius diffs; the dev-key handling.
- **V3 risks considered & dismissed:** (1) public JWS exposure — dismissed (DTO has no JWS field; contract test asserts status-only); (2) runtime-verifier creep — dismissed (verifier is test-only, documented in lld_01); (3) hash-chain drift from signing — dismissed (golden vectors byte-identical; signing strictly downstream of `manifestHash`); (4) migration / package-row envelope drift — dismissed (migration alters `evidence_manifests` only, package row untouched, mapper fail-closed both directions); (5) LocalDev-as-production over-claim — dismissed (`lld_01`/`lld_03` state dev-level only with explicit prod P0 limits).
- **Zero-finding justification:** N/A — every round produced material findings.
- **Total review rounds:** 1 spec + 2 adversarial spot-checks + 1 closeout-governance.
- **Non-convergence:** No — converged each round; findings shrank spec → build → patch → governance.
- **Lessons for next slice:** see Lessons Learned.

## GDrive Sync / Hash Posture

Docs changed in this slice (`lld_01`, `lld_03`, the TIP-66 folder, `README`). No live GDrive sync/hash evidence is available in repo evidence; `docs/00_GDRIVE_FILE_INDEX.md` remains an excluded known-dirty file (not committed). No doc hashes were produced or pushed to the index in this slice. A sync/hash pass (with any required re-auth) is a separate operational step owned by the Homeowner; this closeout makes no GDrive sync claim.

## STOP/RRI Result
No STOP/RRI during build. Signing stayed downstream of `manifestHash`; no hash-chain perturbation.

## Lessons Learned
- Fourth confirmation that on an evidence surface, green tests can hide the load-bearing property: 167/167 passed while NO test proved the signature itself rejects tampered signed content (the harness short-circuited on an echo-compare). Only `VerifySignatureOnly` reaching `VerifyData` proves it — added by patch.
- A "self-describing signature" must bind the signing time too: `signedAt` recorded-but-unsigned was not tamper-evident; binding it into the claim closed that.
- When a build touches many test files, read the diffs: here the wide blast radius was legitimate (DI wiring + a correctly-updated enum invariant), not assertion-weakening — confirmed by reading, not assumed.

## Recommended Next Step
Codex commits TIP-66 by allowlist (exclude known-dirty) and pushes master. Then: the decision-basis-binding debt (P1, needs legal lens) is the next evidence-model item; production signing (HSM/KMS + CA cert + RFC 3161 timestamp + non-repudiation) remains the P0 gate before any legal reliance.
