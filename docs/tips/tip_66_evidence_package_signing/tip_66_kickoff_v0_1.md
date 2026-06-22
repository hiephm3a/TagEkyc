# TIP-66 Evidence Package Signing (IEvidenceSigner + JWS) — Kickoff v0.2

**File:** `docs/tips/tip_66_evidence_package_signing/tip_66_kickoff_v0_1.md`
**Version:** 0.2
**Status:** Draft — dispatch-ready after GPT/Codex review convergence (Tier-2 / EBS-07, code change); awaiting Homeowner dispatch before build
**Date:** 2026-06-22
**Baseline:** `a98f278` (master). Planning brief: `tip_66_planning_brief_v0_1.md` v0.2.
**Purpose:** Execution contract for Codex to add a REAL evidence-package signature — `IEvidenceSigner` abstraction + a local-key/P12 **ES256** dev backend producing a **JWS over `manifestHash`**, with self-describing crypto-agility metadata and round-trip verification by test. Resolves T2-2 at dev level (prod HSM/KMS stays P0).

## Changelog

### v0.2 — GPT/Codex review round 1 patches (2026-06-22)
- P1: `IEvidenceSigner.SignAsync(EvidenceSignatureRequest)` → `EvidenceSignatureEnvelope` (binds packageId/version/canonicalization/hashAlgorithm/purpose); sign a stable **claim object**, not a bare hash. RSA/HSM/KMS = adapters behind the SAME port (removed all "different port" wording).
- P1: legacy packages stay `PlaceholderUnverified` (nullable columns, no synthesized `Signed`) + test.
- P1: full negative verify matrix (wrong-key, header-alg vs recorded-alg mismatch, unknown alg, unknown scheme, tamper, claim-vs-row mismatch).
- P1: single source of truth — envelope persisted on the authoritative **manifest** row only (not duplicated on the package row); consistency test.
- P2: minimal `lld_03` enum note authorized (status may be `Signed`); supersession note over the TIP-65/README "SoftHSM" shorthand.
- P2/P3: envelope separates `signatureFormat=JWS` from `signatureAlgorithm=ES256`; `signatureScheme` = profile id (not a type lock); added `signedAt`; pinned attached-claim JWS payload mode.

### v0.1 — Initial kickoff

## 1. Objective
Replace the placeholder `EvidencePackageSignatureStatus = PlaceholderUnverified` with a real signature over the stable JCS `manifestHash`, behind one `IEvidenceSigner` abstraction so a later prod HSM/KMS/RSA/CA **adapter (same contract)** swaps in. The signature output must be self-describing (alg/kid/scheme) so a future algorithm swap does not break old signatures.

## 2. Source of truth + impacted surfaces (read first)
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` — `manifestHash` computation (sign AFTER it; must NOT change it) and where `EvidencePackageSignatureStatus`/`PlaceholderUnverified` is set (`:237`, `:600`, `:629`).
- `src/TagEkyc.Domain/SignaturePlaceholderStatus.cs` (extend with `Signed`), `EvidencePackage`, `EvidenceManifest*`.
- `src/TagEkyc.Contracts/Common/ContractEnums.cs` — `SignaturePlaceholderStatusDto` (add `Signed`).
- `src/TagEkyc.Application/Ports/` — add `IEvidenceSigner` here.
- `src/TagEkyc.Contracts/InternalAudit/Manifest/ManifestContracts.cs` — `EvidenceManifestDto` (expose signature value + metadata here, INTERNAL).
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` — read to CONFIRM the public summary's `EvidencePackageSignatureStatus` flows the new `Signed` state but the JWS value is NOT added (deferred).
- `src/TagEkyc.Infrastructure/Persistence/Entities/EvidenceManifestRow.cs` (+ `DomainRowMapper.cs`, `TagEkycDbContext.cs`, minimal migration) — persist the signature envelope/value/metadata HERE only (authoritative manifest row). `EvidencePackageRow.cs` — touched only for the existing `EvidencePackageSignatureStatus`/consistency, NOT the envelope.
- `docs/lld_01_data_model_v0_1.md` — Evidence-Integrity / signature section (update + mark T2-2 dev-resolved). `docs/lld_03_api_contracts_v0_1.md` — MINIMAL enum-note update only (status may be `Signed`; DTO shape unchanged). `lld_02`/`lld_04` OUT of scope.

## 3. Scope

### 3.1 `IEvidenceSigner` abstraction (ONE stable contract)
- New port `IEvidenceSigner` (Application/Ports): `SignAsync(EvidenceSignatureRequest request, CancellationToken ct)` returning an `EvidenceSignatureEnvelope`. `EvidenceSignatureRequest` carries the binding context: `packageId`, `manifestHash`, `packageVersion`, `canonicalizationScheme`, `hashAlgorithm`, `purpose = EvidencePackageManifestSignature`.
- ES256/P12 (dev) AND any later RSA/PS256/RS256/HSM/KMS/CA-backed signing MUST be **adapters/config behind the SAME `IEvidenceSigner` contract** — never a different application/domain port, never an ES256/P12-specific contract.
- Dev implementation in Infrastructure: a local-key/P12 **ES256** signer. Key loaded from a configured P12 (or a fixed test key in tests). No real/prod key material in the repo.

### 3.2 JWS over a stable signed claim (self-describing, portable)
- Produce an **attached compact JWS**. The signed payload is a stable canonical **signed claim object**: `{ purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt }` — so the signature attests not just "hash X" but "hash X belongs to this package/version/canonicalization/hashAlgorithm for this purpose at this signing time." Protected header carries `alg=ES256` + `kid`. `manifestHash` is the primary signed claim, not the sole input.
- **Pin the payload mode** (attached compact, with the exact claim object) in the implementation report + tests; do NOT rely on library/runtime detached-payload defaults.
- Signing runs AFTER `manifestHash`; it does NOT feed back into any hash. `manifestHash`/`packageHash`/`manifestBodyHash` stay byte-identical (golden vectors unchanged).

### 3.3 Signature envelope — durable + self-describing + SINGLE source of truth
The `EvidenceSignatureEnvelope` separates format from algorithm:
```
signatureStatus     = Signed
signatureFormat     = JWS                (format — NOT the algorithm)
signatureScheme     = jws-es256-v1       (profile id / version — NOT a domain type lock)
signatureAlgorithm  = ES256              (algorithm selector)
keyId               = <dev kid>
signedAt            = <UTC yyyy-MM-ddTHH:mm:ss.fffffffZ>
signatureValue      = <compact JWS>
```
- `signatureScheme` is a profile identifier; a future RS256/PS256 profile must be representable by changing the `signatureFormat`/`signatureScheme`/`signatureAlgorithm` VALUES without renaming any domain/application field.
- **Single source of truth:** persist the envelope on ONE authoritative row — the **manifest** (`EvidenceManifestRow`, the verifier surface) + `DomainRowMapper` + minimal migration + internal `EvidenceManifestDto`. Do NOT duplicate the envelope on the package row. `EvidencePackageSignatureStatus` (`Signed`) is the existing status field, mirrored on package + manifest as today (same-transaction writes). JWS public exposure deferred.
- **Verifier RULE (documented + test fixtures, NOT a runtime engine — mirrors TIP-65):** a verifier selects by the recorded `signatureFormat`/`signatureScheme`/`signatureAlgorithm` + header `alg`/`kid`; cross-checks the signed-claim metadata against the persisted package/manifest values; and **fails closed** if the signed claim, the durable metadata, and the row values disagree — or on unknown/inconsistent scheme/alg — or if persisted surfaces disagree.

### 3.4 Status + legacy
- Extend `SignaturePlaceholderStatus` (domain) AND `SignaturePlaceholderStatusDto` (contract) with a real `Signed` value; set `EvidencePackageSignatureStatus = Signed` only on TIP-66-signed packages/manifests. (Enum rename to drop "Placeholder" = deferred cosmetic debt; do not rename now.)
- **Legacy:** the new signature columns are nullable; existing (pre-TIP-66) packages keep `EvidencePackageSignatureStatus = PlaceholderUnverified` with a null envelope. Migration/backfill MUST preserve legacy placeholder state; the mapper/API MUST NOT synthesize `Signed` (or treat as verifiable) for a row without a real envelope, unless explicitly migrated by a later reviewed TIP.
- Payload signature and webhook signature stay `PlaceholderUnverified` (out of scope).

### 3.5 Docs (lld_01 + minimal lld_03 + supersession)
- `lld_01`: document the real signature (JWS attached-claim, the envelope fields, single-source-on-manifest, the verifier fail-closed rule). Mark **T2-2 resolved at dev level**; keep production limits explicit (no HSM/KMS, no CA cert, no non-repudiation/replay, not legally sufficient — still P0). Bump version + changelog.
- `lld_03`: **MINIMAL enum-note update only** — `EvidencePackageSignatureStatus` (`SignaturePlaceholderStatusDto`) may now be `Signed` for TIP-66 packages (legacy stays `PlaceholderUnverified`); the DTO field SHAPE is unchanged and there is NO public JWS exposure. No other lld_03 change.
- **Supersession note:** TIP-66 supersedes the TIP-65-closeout / README historical shorthand mentioning "SoftHSM" for T2-2 — TIP-66 authorizes a local non-production key/P12 ES256 backend only (no SoftHSM/PKCS#11/HSM/KMS/Key Vault implementation in this slice).

## 4. Out of scope (do not touch)
Payload signature / webhook signature; runtime verify endpoint; production HSM/KMS, CA certs, key rotation/lifecycle, non-repudiation/replay; changing `manifestHash` / the hash chain; public exposure of the JWS value; duplicating the envelope onto the package row; an algorithm/P12-specific port; API routes / business behavior beyond the additive envelope + the `Signed` status; lld_02/04, hld.
> TIP-66 DOES add: `IEvidenceSigner` + `EvidenceSignatureRequest`/`EvidenceSignatureEnvelope` + ES256 dev adapter; the envelope (JWS + signatureFormat/Scheme/Algorithm/keyId/signedAt) on the domain + internal manifest DTO + manifest EF row/mapper + minimal migration; the `Signed` value on the domain + DTO enums; a MINIMAL lld_03 enum note.

## 5. Definition of Done (verifiable)
- [ ] `IEvidenceSigner` port (`SignAsync(EvidenceSignatureRequest)`→`EvidenceSignatureEnvelope`) + ES256 local/P12 dev adapter; completion logic binds only to the port (RSA/HSM/KMS would be another adapter behind the same contract).
- [ ] Attached compact JWS over the signed claim object `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}`; protected header `alg=ES256` + `kid`; payload mode pinned in report + tests.
- [ ] Envelope (`signatureFormat=JWS`, `signatureScheme=jws-es256-v1`, `signatureAlgorithm=ES256`, `keyId`, `signedAt`, `signatureValue`) persisted on the authoritative **manifest** row (mapper + minimal migration) + read back via the INTERNAL manifest DTO (readback test); NOT duplicated on the package row.
- [ ] **Positive:** round-trip verify with the recorded dev public key succeeds. **Negative matrix:** (a) wrong public key fails; (b) header `alg` ≠ recorded `signatureAlgorithm` fails; (c) unknown `signatureAlgorithm` fails closed; (d) unknown `signatureScheme` fails closed; (e) modified payload/JWS fails; (f) signed-claim metadata ≠ persisted row values fails closed.
- [ ] **Legacy test:** a pre-TIP-66 package (no envelope) stays `PlaceholderUnverified`, is NOT treated as signed/verifiable, and is not synthesized to `Signed` by mapper/API.
- [ ] `EvidencePackageSignatureStatus = Signed` only on TIP-66-signed packages/manifests; payload/webhook stay `PlaceholderUnverified`; package/manifest status consistent (consistency test).
- [ ] **`manifestHash`/`packageHash`/`manifestBodyHash` golden vectors byte-identical** (signing did not perturb the hash chain) — full `dotnet test` green (report counts).
- [ ] No real/prod key in the repo (test key only).
- [ ] `lld_01` updated (T2-2 dev-resolved, production limits explicit); `lld_03` minimal enum note; version + changelog bumped.

## 6. Scope floor (anti-creep)
- Allowed surfaces ONLY: `IEvidenceSigner` + `EvidenceSignatureRequest`/`EvidenceSignatureEnvelope` + ES256 dev adapter; the envelope on the domain + internal `EvidenceManifestDto` + the manifest EF row/`DomainRowMapper`/`TagEkycDbContext` + minimal migration; the `Signed` value on the domain + DTO enums; the completion signing call (after manifestHash); tests; `lld_01`; a MINIMAL `lld_03` enum note. Anything else (payload/webhook signature, runtime verify endpoint, public JWS exposure, envelope duplicated on the package row, an algorithm/P12-specific port, prod HSM/KMS, hash-chain change, lld_02/04) = defect → STOP.

## 7. STOP/RRI
- If signing would change `manifestHash` / any hash-chain value → STOP (signing is strictly downstream/additive).
- If a conformant ES256 JWS cannot be produced within scope (library/key issue) → STOP, report.
- Any change beyond §3 scope → STOP.
- No real key material committed.

## 8. Validation + report
- Run: `dotnet build`; `dotnet test TagEkyc.sln` (unit + arch + integration); confirm the manifestHash golden vectors unchanged; `git diff --stat`.
- Do NOT commit (await Contractor spot-check); Codex's commit/push workflow runs only after acceptance.
- Report: 5-line summary + (a) `IEvidenceSigner`/`EvidenceSignatureRequest`/`EvidenceSignatureEnvelope` shape + dev adapter (key source); (b) JWS structure (attached claim object, header alg/kid, payload mode pinned); (c) the envelope fields persisted on the manifest (single source) + readback (which DTO); (d) positive + full negative matrix (wrong-key, alg-mismatch, unknown-alg, unknown-scheme, tamper, metadata-mismatch) + legacy-placeholder test results; (e) confirmation manifestHash/packageHash/manifestBodyHash golden vectors UNCHANGED; (f) full test counts; (g) lld_01 + lld_03 updates + T2-2 dev-resolved wording.

## 9. Review after build (Contractor)
Contractor adversarial spot-check (EBS-07): verify on CODE — the dev adapter produces an attached-claim JWS that verifies (round-trip) and FAILS on the full negative matrix (wrong key; header-`alg` vs recorded-`signatureAlgorithm` mismatch; unknown alg/scheme; tamper; signed-claim-vs-row mismatch); the envelope is self-describing (format/scheme/alg/keyId/signedAt present, persisted on the SINGLE authoritative manifest row, read back); the signed claim binds packageId/version/canonicalization/hashAlgorithm (not just the bare hash); legacy packages stay `PlaceholderUnverified`/non-verifiable; `manifestHash`/`packageHash`/`manifestBodyHash` golden vectors byte-identical; status `Signed` consistent across package/manifest; payload/webhook untouched; no prod-grade/legal over-claim in lld_01; no real key committed. Then closeout. Prod HSM/KMS + CA cert + non-repudiation remain P0 debt; decision-basis-binding debt is next (legal lens).
