# TIP-66 Evidence Package Signing (IEvidenceSigner + JWS) — Planning Brief v0.2

**File:** `docs/tips/tip_66_evidence_package_signing/tip_66_planning_brief_v0_1.md`
**Version:** 0.2
**Status:** Draft — dispatch-ready after GPT/Codex review convergence (Tier-2 / evidence-bearing, EBS-07); awaiting Homeowner dispatch before build
**Date:** 2026-06-22
**Baseline:** `a98f278` (master; TIP-65 JCS canonicalization done — the manifestHash is now stable + portable).
**Purpose:** Resolve Tier-2 open item **T2-2** at the dev level: replace the placeholder evidence-package signature with a REAL signature — an `IEvidenceSigner` abstraction producing a **JWS over the stable `manifestHash`**, with a local-key/P12 **ES256** dev backend, self-describing crypto-agility metadata, and round-trip verification proven by test. Production HSM/KMS + CA-cert + non-repudiation remain P0 debt.

## Changelog

### v0.2 — GPT/Codex review round 1 patches (2026-06-22)
- Port signs an `EvidenceSignatureRequest` claim object (binds manifestHash + package metadata + purpose), not a bare hash; RSA/HSM/KMS are adapters behind the SAME `IEvidenceSigner` (removed "different port" wording).
- Single source of truth (manifest row); envelope separates `signatureFormat`/`signatureScheme`/`signatureAlgorithm`; added `signedAt`; attached-claim JWS pinned.
- Legacy packages stay `PlaceholderUnverified`; full negative verify matrix (wrong-key/alg-mismatch/unknown/tamper/claim-vs-row); minimal `lld_03` enum note; SoftHSM supersession note.

### v0.1 — Initial planning brief
- Opened TIP-66 per the Homeowner T2-2 decision (2026-06-21) and the algorithm/backend/verify-scope decisions (2026-06-22): ES256, dev = local key/P12 behind `IEvidenceSigner`, verify = round-trip test (no runtime endpoint), and self-describing signature metadata (alg/kid/scheme) for crypto-agility.

## 0. Decision basis

| Item | Decision |
|---|---|
| **T2-2** signing | Abstract `IEvidenceSigner`; dev = local key/P12; prod = HSM/KMS (gated P0); format = **JWS** over `manifestHash`. |
| Algorithm | **ES256** (ECDSA P-256). ONE `IEvidenceSigner` contract; ES256/P12/RSA/HSM/KMS/CA are **adapters behind the SAME port** (never a different/algorithm-specific port). |
| Crypto-agility (Homeowner emphasis) | Self-describing output: JWS header `alg`+`kid`; PLUS a durable envelope `signatureFormat=JWS` / `signatureScheme=jws-es256-v1` / `signatureAlgorithm=ES256` / `keyId` / `signedAt`; AND the JWS signs a **claim object** binding `manifestHash`+`packageVersion`+`canonicalizationScheme`+`hashAlgorithm`+`purpose`. A verifier selects by recorded params, cross-checks claim-vs-rows, fails closed, and a future algorithm swap is non-breaking (mirrors TIP-65). |
| SoftHSM supersession | Supersedes the TIP-65-closeout/README "SoftHSM" shorthand for T2-2: TIP-66 = local non-production key/P12 ES256 only (no SoftHSM/PKCS#11/HSM/KMS/Key Vault in this slice). |
| Verify scope | Produce + store real signature + **round-trip verify TEST** (sign→verify OK; tamper→fail; unknown-scheme→fail-closed). **No runtime verify endpoint** (consumer concern, deferred — avoids the TIP-65 dead-code-engine trap). |

## 1. Status / Authorization basis
Implementation TIP (code + tests + lld_01), **Tier-2 / EBS-07** (cryptographic signing is evidentiary/legal). Authorization: Homeowner T2-2 decision (§0). Depends on TIP-65 (signs the stable JCS `manifestHash`). Role split: Contractor drafts → GPT + Codex review/converge → Codex builds → Contractor adversarial spot-check.

## 2. As-built grounding (read first)
- `manifestHash` is the top of the hash chain (`manifestBodyHash → packageHash → manifestHash`) in `VerificationCompletionApplicationService.cs` — the thing to sign.
- Signature model today is placeholder-only: `SignaturePlaceholderStatus` enum (every value set to `PlaceholderUnverified`), with THREE separate status surfaces — `EvidencePackageSignatureStatus` (package + manifest), `PayloadSignatureStatus` (per-evidence, adapter-submitted), `WebhookSignatureStatus`. There is **no signature value** stored anywhere and **no `IEvidenceSigner`**.
- TIP-66 targets ONLY the **EvidencePackage signature** (over `manifestHash`). Payload signature and webhook signature are separate debts — out of scope.

## 3. Scope (this slice)
- New `IEvidenceSigner` port (Application): `SignAsync(EvidenceSignatureRequest)`→`EvidenceSignatureEnvelope`, binding `packageId`/`manifestHash`/`packageVersion`/`canonicalizationScheme`/`hashAlgorithm`/`purpose`. ES256/P12/RSA/HSM/KMS are adapters behind the SAME port. Dev = local-key/P12 ES256 adapter (Infrastructure).
- Produce an **attached compact JWS** over a stable **claim object** `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}`; header `alg=ES256`+`kid`.
- Durable self-describing **envelope** (`signatureFormat=JWS`, `signatureScheme=jws-es256-v1`, `signatureAlgorithm=ES256`, `keyId`, `signedAt`, `signatureValue`) — persisted on the SINGLE authoritative **manifest** row (mapper + minimal migration) + internal `EvidenceManifestDto`; NOT duplicated on the package row; public JWS exposure deferred.
- Real **Signed** value on the domain + DTO enums; legacy packages stay `PlaceholderUnverified` (nullable columns, no synthesized Signed).
- Sign AFTER `manifestHash` — downstream/additive; **must not change `manifestHash`** (golden vectors byte-identical).
- Tests: round-trip + full negative matrix (wrong-key; header-alg vs recorded-alg mismatch; unknown alg/scheme fail-closed; tamper; claim-vs-row mismatch); legacy-placeholder; persist+readback+consistency; manifestHash golden UNCHANGED.
- Docs: `lld_01` (signature + verifier fail-closed rule; T2-2 dev-resolved + prod limits); MINIMAL `lld_03` enum note (status may be `Signed`).

## 4. Non-goals (out of scope)
Payload signature (adapter) and webhook signature (separate debts); a runtime verify endpoint; production HSM/KMS, CA-issued certs, key rotation/lifecycle, non-repudiation/replay protection, legal sufficiency (all P0 prod debt); changing `manifestHash` or the hash chain; public exposure of the JWS value (deferred); duplicating the envelope onto the package row; an algorithm/P12-specific port; lld_02/04; changing API routes / business behavior beyond the additive envelope + `Signed` status.

## 5. Key design decisions (PINNED)
- **Algorithm:** ES256, behind `IEvidenceSigner` so a prod HSM/KMS/RSA **adapter** (same port) swaps in without touching the completion logic.
- **Signed claim, not bare hash:** the JWS signs a claim object binding manifestHash + package metadata + purpose; envelope separates `signatureFormat=JWS` from `signatureAlgorithm=ES256` (`signatureScheme` = profile id, not a type lock); single authoritative persistence (manifest row).
- **Self-describing output:** JWS header (`alg`,`kid`) + durable `signatureScheme`/`signatureAlgorithm`/`kid` so swapping algorithm later is non-breaking and old signatures verify under their recorded params (fail-closed on unknown).
- **Non-determinism:** ES256 (ECDSA) signatures are randomized → no byte-stable golden vector for the signature; verification is by round-trip, while the `manifestHash` it signs stays deterministic/golden.
- **Dev key:** loaded from a configured P12 (or a fixed test key for tests); future prod key/backend = a later-authorized adapter behind the same `IEvidenceSigner` (not built in TIP-66, gated P0).

## 6. Acceptance criteria (DoD summary)
Full DoD in kickoff. Summary: `IEvidenceSigner` + `EvidenceSignatureRequest`/`EvidenceSignatureEnvelope` + ES256 local/P12 dev adapter; attached compact JWS over the stable signed claim object `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}`; durable envelope `signatureFormat`/`signatureScheme`/`signatureAlgorithm`/`keyId`/`signedAt`/`signatureValue` persisted on the authoritative **manifest** row (not the package row) + read back via the internal manifest DTO; status `Signed` (legacy stays `PlaceholderUnverified`); full positive/negative matrix (wrong-key, header-alg vs recorded-alg mismatch, unknown alg/scheme, tamper, claim-vs-row) + legacy test; `manifestHash`/`packageHash`/`manifestBodyHash` golden vectors byte-identical; `lld_01` + minimal `lld_03` updated; T2-2 resolved at dev level only.

## 7. STOP/RRI
- Signing must NOT alter `manifestHash` / the hash chain → STOP.
- Envelope must NOT be duplicated onto the package row (manifest = single source) → STOP.
- Must stay ONE `IEvidenceSigner` contract (no algorithm/P12-specific port) → STOP.
- Any change beyond the additive envelope + `Signed` enum value + dev adapter + tests + `lld_01` + minimal `lld_03` note (e.g. payload/webhook signature, runtime verify endpoint, public JWS exposure, prod HSM/KMS, API routes, lld_02/04) → STOP.
- No real key material committed (test key only; prod keys via config/secret store).

## 8. Recommended next action
Submit brief + kickoff for GPT + Codex review (Tier-2: confirm the JWS/ES256 approach, the self-describing metadata, and that signing does not perturb the hash chain). Converge, dispatch, build, Contractor adversarial spot-check (re-verify the signature round-trips + manifestHash unchanged on CODE). Then the decision-basis-binding debt (legal lens).
