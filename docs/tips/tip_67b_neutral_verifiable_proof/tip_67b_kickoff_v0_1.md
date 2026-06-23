# TIP-67B Neutral Verifiable eKYC Proof + Verification View — Kickoff v0.4

**File:** `docs/tips/tip_67b_neutral_verifiable_proof/tip_67b_kickoff_v0_1.md`
**Version:** 0.4
**Status:** Draft — dispatch-ready spec (Tier-2 / EBS-01+07 + integration, code change); GPT/Codex review round 3 patches applied; pending convergence before build
**Date:** 2026-06-22
**Baseline:** `72a7b41` (master; TIP-67A committed — opaque-challenge/profile contract FROZEN). Planning brief: `tip_67b_planning_brief_v0_1.md` v0.9. Decision: `docs/adr/ekyc_neutrality_decision_v0_1.md`.
**Purpose:** Make the eKYC result an independently-verifiable NEUTRAL PROOF: enrich the signed JWS claim with the decision-relevant facts + the echoed opaque challenge + a stable `resultHash`, expose a verification view + public key (trust-anchored), and publish the fail-closed verifier contract + a reference consumer test. eKYC stays neutral; the client (SignFlow) verifies and does its OWN binding.

## Changelog
### v0.4 — GPT/Codex review round 3 (2026-06-22)
- P1: **sign-time public-key binding** (§3.1c) — the view serves the key that signed THIS package (persisted in envelope or `keyId`→historical registry), NOT the current signer (LocalDev key regenerates on restart / prod rotates); rotate-key test.
- P1: `requiredChecks`/`completedChecks` **deterministic ordering** (enum-name ordinal) before claim/`resultHash` (sets→stable hash); shuffled-input test.
- P2: sanitization classifies `challenge`/`clientReference` as caller-supplied echoes OUTSIDE TagEkyc's PII guarantee (no-PII test targets TagEkyc-derived leakage); scope floor lists `EvidenceProofSignatureRequest` + provider-neutral material.
- P3: reordered §3.1a → §3.1b → §3.1c.

### v0.3 — GPT/Codex review round 2 (2026-06-22)
- P1: provider-neutral public-key material (§3.1c) — extend the envelope with `PublicKeyJwk`/`PublicKeyFingerprint` (or a port); App/API never casts to `LocalDevEs256JwsEvidenceSigner` / private-key internals.
- P1: pinned a dedicated `EvidenceProofSignatureRequest` (don't overload TIP-66's request); interim thin claim superseded; TIP-66 metadata kept + tested.
- P1: `identityRef` ALWAYS-HASH (removed the direct-use escape hatch — no enforcement that SubjectRef is pseudonymous).
- P2: §2 route wording fixed (dedicated `/verification-view`; existing retrieval unchanged); planning-brief ref → v0.8; `result_hash`→`resultHash` canonical.

### v0.2 — GPT/Codex review round 1 (2026-06-22)
- P1: claim is now a SUPERSET (kept TIP-66 packageId/packageVersion/canonicalizationScheme/hashAlgorithm — self-containment); `resultHash` recipe pinned field-exact (preimage object + named label + JCS + self-describing `resultHashAlgorithm`/`resultHashCanonicalizationScheme`); `identityRef` pseudonymization pinned (no raw SubjectRef/PII) + test.
- P1: unsigned-view-field tamper is fail-closed — verifier reads ONLY from the decoded claim; mirrored view fields must equal it; negative test for tampered unsigned `result`/`assurance`/`identity`/`challenge`.
- P2: dedicated route `/verification-view` (summary unchanged); `evidenceEngines[]` item gains `evidenceResultId`+`checkType`; single `assuranceLevel` (dropped `achievedAssurance`); `clientReference` = unsigned correlation only; header `kid`/`alg` cross-check; JWK public-params-only (reject `d`); "all decision facts" softened to "selected MVP facts".

## 1. Objective
After TIP-67A neutralized the core (opaque `Challenge`, no transaction concept), 67B lets any client (SignFlow) verify the eKYC result independently and bind it to its own context — without eKYC ever binding the document/transaction.

## 2. Source of truth + impacted surfaces (read first)
- `src/TagEkyc.Infrastructure/Signing/LocalDevEs256JwsEvidenceSigner.cs` + `EvidenceSignerPorts.cs` (`EvidenceSignatureRequest`/`Envelope`) — the JWS claim to enrich (TIP-66 claim today = `{purpose, signedManifestHash, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, signedAt}`; `signedAt` is microsecond-normalized). The public-key material (JWK + fingerprint) for the view MUST come from a provider-neutral source (§3.1c), NOT a cast to the concrete signer.
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` — where the proof is built/signed (after `manifestHash`); the decision facts (`result`, `assuranceLevel`, checks), `Challenge` (echoed from 67A), evidence results (engines).
- `src/TagEkyc.Domain/EvidenceResult.cs` — `Confidence`/`ReasonCodes` (EXCLUDED = P1 debt), engine name/version, result type.
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` — add `EvidencePackageVerificationViewDto`.
- `src/TagEkyc.Api/VerificationSessionEndpoints.cs` — ADD the dedicated `GET /api/ekyc/evidence-packages/{id}/verification-view` route; the existing evidence-package retrieval is UNCHANGED (does NOT return the view).
- `docs/signflow_integration_contract_v0_1.md`, `docs/lld_01_data_model_v0_1.md`, `docs/lld_03_api_contracts_v0_1.md` — publish the proof + verifier contract.

## 3. Scope
### 3.1 Enriched neutral proof claim (field-exact)
- The signed JWS claim is a **SUPERSET of the TIP-66 claim** (keep its package metadata) + the neutral-proof facts:
  `{ proofVersion, purpose, sessionId, identityRef, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, result, assuranceLevel, requiredChecks, completedChecks, evidenceEngines[], signedAt, challenge, signedManifestHash, resultHash, resultHashAlgorithm, resultHashCanonicalizationScheme }`
  - `proofVersion = neutral-proof-v1` (pins the claim shape; a future shape change bumps it).
  - **Keep** TIP-66 `packageId`/`packageVersion`/`canonicalizationScheme`/`hashAlgorithm` — do NOT drop (preserves self-containment + the claim-vs-row cross-check).
  - `assuranceLevel` is the SINGLE achieved-assurance field (no duplicate `achievedAssurance`).
  - `evidenceEngines[]`: ordered, each `{ evidenceResultType, evidenceResultId, engineName, engineVersion, checkType }`, sorted by `evidenceResultType` then `evidenceResultId`.
  - `requiredChecks`/`completedChecks`: deterministically SORTED (enum-name ordinal) before the claim/`resultHash` (current domain uses sets in places). **Test:** shuffled/set input yields an IDENTICAL `resultHash`.
  - `challenge`: opaque, echoed from 67A (verbatim). `clientReference` is NOT signed (unsigned correlation only — §3.2).
  - `confidence`/`reasonCodes` are NOT included → explicit **P1 decision-basis debt** (NOT legally-complete rationale).
- This carries the **selected MVP decision facts** (result, assuranceLevel, identityRef, required/completed checks, evidenceEngines, challenge, signedManifestHash, resultHash) — NOT "all" facts.
- Signed by `IEvidenceSigner` (ES256 dev, unchanged). Signed AFTER `manifestHash`; **the evidence hash chain is NOT changed** (golden vectors byte-identical).

### 3.1a `identityRef` — pseudonymous, non-PII (PINNED: ALWAYS HASH)
`identityRef` MUST be pseudonymous + non-PII. Because the repo has NO classification/enforcement that `SubjectRef` is pseudonymous (it can hold CCCD/phone/email/name), 67B **ALWAYS derives** it — there is **NO direct-use escape hatch**:
`identityRef = sha256:<hex>` over a LABELED, client-scoped canonical input: `sha256("tip-67b-identity-ref-v1\n" + clientApplicationId + "\n" + subjectRef)`.
(A direct pseudonymous-subject-id path is allowed ONLY if a separate, contract-validated pseudonymous-id field is later introduced — NOT in 67B.) **Sanitization test:** raw `SubjectRef`/PII appears in NEITHER the verification view NOR the JWS payload.

### 3.1b `resultHash` — recipe PINNED
- `resultHash = sha256:<hex>` (`resultHashAlgorithm = sha256`) over the RFC 8785 JCS canonicalization (`resultHashCanonicalizationScheme = rfc8785-jcs-v1`) of the `resultHashPreimage`, prefixed by the named label `"tip-67b-neutral-proof-result\n"`.
- `resultHashPreimage` = the claim fields EXCLUDING `resultHash`, `resultHashAlgorithm`, `resultHashCanonicalizationScheme`, and `signatureValue` — i.e. `{ proofVersion, purpose, sessionId, identityRef, packageId, packageVersion, canonicalizationScheme, hashAlgorithm, result, assuranceLevel, requiredChecks, completedChecks, evidenceEngines, signedAt, challenge, signedManifestHash }` (JCS-sorted).
- Clients bind to this signed `resultHash`; they MUST NOT invent their own recipe. The reference test **recomputes `resultHash` from the verified claim** + compares.

### 3.1c Signing request shape + SIGN-TIME provider-neutral verification material (PINNED)
- **Request shape:** introduce a dedicated `EvidenceProofSignatureRequest` (neutral-proof-v1; superset carrying the package metadata + the neutral facts) — do NOT ad-hoc overload TIP-66's `EvidenceSignatureRequest`. The signer builds exactly ONE canonical claim for `neutral-proof-v1`; TIP-66 package-metadata fields remain present + tested. The interim TIP-66 thin-claim shape is SUPERSEDED by 67B (no production thin-claim packages exist in S1 — test DBs are recreated); 67B tests cover the enriched claim.
- **SIGN-TIME public-key material (provider-neutral + package-bound):** the view MUST expose the public key that ACTUALLY signed THIS package — NOT "the current signer's key" (the LocalDev key is regenerated on restart absent a P12, and prod rotates). So **persist the sign-time `PublicKeyJwk` + `PublicKeyFingerprint` in the signature envelope** (produced by the signer adapter at sign time), OR a key registry resolving the envelope's `keyId` → its HISTORICAL public key. Application/API MUST NOT cast to `LocalDevEs256JwsEvidenceSigner` / P12 / X509 / private-key internals, and MUST NOT derive from the current signer. **Test:** sign a package, then change/rotate the signer key — the view still serves the ORIGINAL signing key (verification passes) OR fails closed as not-verifiable; never silently serves the new key.

### 3.2 Verification view + route (PINNED)
- **Route (PINNED):** a DEDICATED route `GET /api/ekyc/evidence-packages/{id}/verification-view` returns `EvidencePackageVerificationViewDto` for TIP-67B-signed packages; legacy/placeholder packages return 404 / not-verifiable. The existing evidence-package retrieval + `EvidencePackageSummaryDto` are UNCHANGED (backward-compat; no new field on the summary). No runtime verifier endpoint.
- `EvidencePackageVerificationViewDto` exposes: the proof claim fields (mirrored, convenience only — see §3.3 cross-check), `signatureValue` (compact JWS), `signatureFormat`/`signatureScheme`/`signatureAlgorithm`, `keyId`, `publicKeyJwk`, `resultHashAlgorithm`/`resultHashCanonicalizationScheme`, the echoed `challenge`, and `clientReference` (**UNSIGNED correlation echo ONLY — NOT proof/binding**).
- **`publicKeyJwk` (PINNED):** ONLY public params `{kty, crv, x, y}` for ES256; MUST NOT contain private `d`, `key_ops`, or cert/P12 material. Serialization test asserts this.
- **Sanitization (test):** TagEkyc-DERIVED fields are hashes/ids/enums/timestamps/public-key only — NO VaultRef, NO raw artifacts/images, NO plaintext PII (identityRef pseudonymous per §3.1a), NO private key. `challenge`/`clientReference` are **caller-supplied opaque echoes OUTSIDE TagEkyc's PII guarantee** (the client may put anything there; eKYC echoes verbatim, does not interpret/sanitize). The no-PII test targets TagEkyc-derived leakage (raw `SubjectRef`/`VaultRef`/raw artifacts/private key), NOT the caller-supplied echoes.

### 3.3 Trust anchor + verifier contract + reference test (PINNED)
- **Trust anchor:** pin the expected `keyId` + `publicKeyFingerprint` OUT-OF-BAND (config; JWKS = prod/P0). **Fingerprint (single recipe):** `sha256` over the RFC 8785 JCS canonical JWK public params `{kty, crv, x, y}`. The embedded `publicKeyJwk` MUST match the pinned fingerprint — NOT a trust anchor by itself.
- **Verifier algorithm (fail-closed):**
  1. `proofVersion`/`signatureScheme`/`signatureAlgorithm` are known.
  2. JWS protected-header `kid`/`alg` == the view's `keyId`/`signatureAlgorithm` == the pinned anchor.
  3. `publicKeyJwk` matches the pinned fingerprint AND contains no private material (`d`/cert).
  4. Verify the JWS.
  5. **Read ALL facts ONLY from the decoded/verified claim.** The mirrored view fields are convenience only and MUST equal the decoded claim — fail closed on ANY mismatch.
  6. Recompute `resultHash` from the verified claim per §3.1b + compare.
  7. Client checks `challenge` == its own + binds `H(challenge ‖ document_hash ‖ resultHash)`.
  **Fail closed** on: unknown proofVersion/scheme/alg; kid/alg mismatch (header vs view vs anchor); JWK ≠ fingerprint or has private material; invalid JWS; mirrored-view-field ≠ decoded-claim; recomputed `resultHash` ≠ signed; `challenge` ≠ expected.
- **Completion notification stays notification-only** (verification goes through the view).
- **Reference consumer test (ONLY the view, no internal access)** — negative matrix: forged JWS + attacker JWK; kid/alg mismatch; JWK with private `d`; **valid JWS + TAMPERED unsigned view field (`result`/`assuranceLevel`/`identityRef`/`challenge`) → FAIL**; recomputed-`resultHash` mismatch; `challenge` ≠ expected. NOT a runtime verifier endpoint.

### 3.4 Evidence-binding trace matrix (carried from the superseded TIP-67, reframed challenge-based)
- Field × Stored/Public(view)/Signed/Echoed/Consumer-verifies. `challenge` = the anti-substitution integration row (Signed ✓). `result`/`assuranceLevel`/`identityRef`/`resultHash`/`evidenceEngines` signed-in-claim ✓. **`clientReference` = Public/Echoed but NOT Signed / NOT binding** (unsigned correlation only). decision-basis (`confidence`/`reasonCodes`) ✗ = P1 debt. `Signed` status = a marker, NOT proof. Mirrored view fields MUST equal the decoded claim (cross-check). Every ✓ exercised by the reference test.

## 4. Out of scope (do not touch)
The 67A profile/challenge neutralization (done); document/transaction binding (client's job); the evidence hash graph / `manifestHash` (signing the enriched claim must not alter it); production HSM/KMS, CA cert, JWKS endpoint + rotation, RFC 3161 timestamp (P0); webhook signing; decision-basis binding (`confidence`/`reasonCodes` — P1); building SignFlow's verifier (separate repo — TagEkyc is its own provider); lld_02/04.

## 5. Definition of Done (verifiable)
- [ ] Enriched claim signed with the §3.1 SUPERSET fields (incl. TIP-66 `packageId`/`packageVersion`/`canonicalizationScheme`/`hashAlgorithm`), `proofVersion=neutral-proof-v1`, single `assuranceLevel`, `evidenceEngines[]` (incl. `evidenceResultId`, ordered), `requiredChecks`/`completedChecks` deterministically sorted (**shuffled-input → identical `resultHash` test**), and a stable signed `resultHash` + `resultHashAlgorithm`/`resultHashCanonicalizationScheme` per §3.1b (recompute-and-compare test). Signed via a dedicated `EvidenceProofSignatureRequest`.
- [ ] **Sign-time key binding:** the view serves the public key that signed THIS package (persisted in the envelope or resolved by `keyId`), NOT the current signer. Test: rotate/regenerate the signer key → the original key still verifies the old package OR it fails closed as not-verifiable — NEVER silently serves the new key.
- [ ] `identityRef` pseudonymous per §3.1a — **no-PII test**: raw `SubjectRef`/PII absent from BOTH the view and the JWS payload.
- [ ] Dedicated route `GET /api/ekyc/evidence-packages/{id}/verification-view` → `EvidencePackageVerificationViewDto`; legacy → 404/not-verifiable; `EvidencePackageSummaryDto` UNCHANGED (backward-compat test). `publicKeyJwk` = public params only (`{kty,crv,x,y}`, no `d`/cert — serialization test). `clientReference` exposed as unsigned correlation only.
- [ ] **Reference consumer test (ONLY the view):** verify against the pinned trust anchor; read facts ONLY from the verified claim; mirrored view fields MUST equal the decoded claim. **Negative matrix (all FAIL closed):** forged JWS + attacker JWK; kid/alg mismatch (header vs view vs anchor); JWK ≠ fingerprint / has private `d`; unknown proofVersion/scheme/alg; **tampered UNSIGNED view field (`result`/`assuranceLevel`/`identityRef`/`challenge`)**; recomputed `resultHash` ≠ signed; `challenge` ≠ expected.
- [ ] **`manifestHash`/`packageHash`/`manifestBodyHash` golden vectors byte-identical** (enriched claim does not touch the hash chain).
- [ ] Docs publish the proof + verifier contract + trust-anchor/fingerprint recipe; decision-basis (`confidence`/`reasonCodes`) exclusion = P1 debt; `Signed` status = marker not proof; `clientReference` = unsigned correlation.
- [ ] Full `dotnet test` green. No real/prod key committed.

## 6. Scope floor (anti-creep)
- Allowed ONLY: `EvidenceProofSignatureRequest` + the enriched neutral-proof claim + `resultHash` (signer/completion path); provider-neutral **SIGN-TIME** verification material (envelope `PublicKeyJwk`/`PublicKeyFingerprint` or a `keyId`→historical-key registry port); `EvidencePackageVerificationViewDto` + the dedicated `/verification-view` route (existing retrieval UNCHANGED); trust-anchor fingerprint; the verifier-contract docs; tests. Anything touching the hash graph / 67A's profile-challenge / document-transaction binding / prod crypto / a runtime verifier engine / the GLOBAL enum converter / lld_02/04 = defect → STOP.

## 7. STOP/RRI
- Signing the enriched claim must NOT change `manifestHash` / the hash chain → STOP.
- Any VaultRef/raw/PII/private-key leak in the view → STOP.
- Re-introducing document/transaction binding into eKYC → STOP.
- Production crypto / webhook / decision-basis work → STOP (out).

## 8. Validation + report
- `dotnet build`; `dotnet test`; confirm golden vectors unchanged; `git diff --stat`. Do NOT commit (await Contractor spot-check).
- Report: (a) the enriched claim fields + proofVersion + resultHash recipe; (b) the view fields + sanitization test; (c) public-key JWK + fingerprint recipe; (d) the reference verify test + full negative matrix; (e) golden-unchanged proof; (f) docs; (g) test counts.

## 9. Review after build (Contractor)
Contractor adversarial spot-check on CODE: verify the JWS as a consumer using ONLY the view + the pinned trust anchor; confirm forged-JWS + attacker-JWK FAILS (the load-bearing security property); confirm the enriched claim carries the decision facts + resultHash (no unsigned-field trust); confirm no sanitization leak; confirm `manifestHash` golden byte-identical; confirm no document/transaction binding crept into eKYC; no prod/legal over-claim. Then closeout (playbook structure). Production signing (P0) + decision-basis binding (P1) remain the open evidence items.
