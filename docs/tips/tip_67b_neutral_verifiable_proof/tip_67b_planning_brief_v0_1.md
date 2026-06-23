# TIP-67B Neutral Verifiable eKYC Proof + Verification View — Planning Brief v0.5

**File:** `docs/tips/tip_67b_neutral_verifiable_proof/tip_67b_planning_brief_v0_1.md`
**Version:** 0.5
**Status:** Draft — implementation planning (Tier-2 / evidence-bearing, EBS-01/07 + integration); GPT/Codex review round 4 patches applied; **BLOCKED on TIP-67A** (kickoff held until 67A freezes the opaque-challenge/profile contract)
**Date:** 2026-06-22
**Baseline:** to be set to the committed TIP-67A SHA when 67A closes.
**Decision basis:** `docs/adr/ekyc_neutrality_decision_v0_1.md`. Second of the 2-TIP split (67A neutralize → 67B proof/view).
**Purpose:** Make the eKYC result an independently-verifiable, neutral PROOF that SignFlow (any client) can verify: sign a neutral proof claim (result/assurance/identity/method/timestamp + echoed opaque challenge), expose a verification view + public key, and publish the fail-closed verifier contract. The client verifies and does its OWN binding (`H(challenge ‖ document_hash ‖ result_hash)`).

## Changelog
### v0.5 — GPT/Codex review round 4 (2026-06-22)
- `resultHash` recipe pinned to EXCLUDE `resultHash` itself + `signatureValue` (no recursive self-hash). ADR casing reconciled — canonical proof field is `resultHash` (`result_hash` = formula notation only).

### v0.4 — GPT/Codex review round 3 (2026-06-22)
- P2: `method/checks` → an ordered `evidenceEngines[]` collection (per evidenceResultType; a package mixes engines), with explicit single-engine fail-closed fallback. Fingerprint recipe pinned to a SINGLE mandatory recipe (JWK JCS `{kty,crv,x,y}`; DER = future alternate, not "or").

### v0.3 — GPT/Codex review round 2 (2026-06-22)
- P1: added a stable signed **`resultHash`** to the proof (clients bind to it via `H(challenge ‖ document_hash ‖ resultHash)`; no client-invented hash recipe). Pinned **`method/checks`** field-exact (requiredChecks/completedChecks/evidenceResultTypes/engineName/engineVersion/achievedAssurance); confidence/reasonCodes excluded = explicit P1 debt, marked unsigned.
- P2: pinned the `publicKeyFingerprint` recipe (sha256 over canonical JWK `{kty,crv,x,y}` via JCS, or DER SPKI) for the trust anchor + negative tests.

### v0.2 — GPT/Codex review round 1 (2026-06-22)
- P1 (security): added the **trust anchor** requirement — the consumer pins expected `kid` + key fingerprint out-of-band (even dev); the embedded `publicKeyJwk` must match it (a key in the same response is not a trust anchor); negative test for forged-JWS + attacker-JWK.
- P2 (honesty): softened the "deferring full preimage loses no assurance" claim — the consumer verifies the signed proof CLAIM, not the full evidence-package preimage (commitment exists via manifestHash; preimage exposable on demand = Tier-2).

### v0.1 — Initial planning brief (blocked on 67A)
- Opened as the proof/view half of the neutrality split. Carries forward the sound parts of the superseded TIP-67 v0.3 (ES256 JWS, fail-closed verifier contract, no-leak sanitization, reference consumer test, trace matrix), reframed NEUTRAL (sign+echo opaque challenge instead of binding SignFlow's transaction). The earlier "bind transaction into the hash" direction is dropped (binding is the client's job per the ADR).

## 0. Dependency on 67A (why the kickoff is held)
67B signs and exposes the **opaque challenge** + profile shape that 67A defines. The signed-claim fields, the verification-view DTO, the verifier contract, and the reference test all reference 67A's frozen challenge/profile names. 67A is a SHIPPED-CODE refactor → its field shape can shift under compat pressure. So: write this brief now (the conceptual contract is stable — opaque challenge, echo+sign, client-binds), but FREEZE the kickoff (field-exact DoD) only after 67A converges.

## 1. Intent
A client receiving an eKYC result can, using ONLY public data, verify: (a) the result is authentic (signature), (b) it carries the assurance/identity/method it claims (all in the signed proof), and (c) it is the answer to the client's own challenge (echoed challenge matches). The client then binds it to its context. eKYC stays neutral — it never binds the document/transaction.

## 2. Scope (this slice — to finalize after 67A)
### 2.1 Neutral signed proof
- Enrich the signed JWS claim to a complete neutral proof: `{ purpose, sessionId, identityRef (pseudonymous), result, assuranceLevel, requiredChecks, completedChecks, evidenceEngines[], achievedAssurance, timestamp, challenge (opaque, echoed), signedManifestHash, resultHash }`. (E-Q02 — self-contained, not just hashes/metadata.)
- **`method/checks` field-exact (PINNED before kickoff):** `requiredChecks`, `completedChecks`, `achievedAssurance`, and an **ordered `evidenceEngines[]`** — each item = `{ evidenceResultType, engineName, engineVersion, check/method identity }`, sorted deterministically by `evidenceResultType` then `evidenceResultId` (a package may mix OCR/NFC/face/liveness from different engines; singular engineName/Version is wrong). If 67B deliberately supports only one engine, state that explicitly and FAIL CLOSED on mixed-engine packages. `confidence`/`reasonCodes` are NOT included → explicit **P1 decision-basis debt**, marked unsigned/untrusted for legal rationale.
- **Stable signed `resultHash` (PINNED):** the proof MUST carry a stable `resultHash` computed over the canonical neutral-proof facts (sessionId, identityRef, result, assuranceLevel, the checks/engine fields, timestamp, challenge, signedManifestHash) — **EXCLUDING `resultHash` itself and `signatureValue`** (no recursive/self-hash) — with the hash algorithm + canonicalization scheme recorded. **Clients bind to this signed `resultHash`** (`H(challenge ‖ document_hash ‖ resultHash)`); clients MUST NOT invent their own result-hash recipe.
- Signed by `IEvidenceSigner` (ES256 dev, unchanged from TIP-66). Signing the proof does not change the existing evidence hash graph beyond the claim fields the signer covers; STANDARD golden vectors stay byte-identical where applicable.
### 2.2 Verification view + public key
- A dedicated consumer **verification view** exposes the signed proof (compact JWS) + `publicKeyJwk` + `keyId` + `signatureFormat`/`signatureScheme`/`signatureAlgorithm` + the echoed challenge + the proof fields needed to verify. Reverses TIP-66's deferred public exposure (the consumer now exists).
- **Sanitization preserved:** hashes/ids/enums/timestamps/public-key only; NO VaultRef/raw-artifact/PII/private-key.
### 2.3 Verifier contract + reference test
- Publish the fail-closed verification algorithm: **(0) establish a TRUST ANCHOR** — the consumer pins the expected `keyId` + a key fingerprint OUT-OF-BAND (config, even for dev; a JWKS endpoint is the prod form = P0). The embedded `publicKeyJwk` is a convenience and MUST match the pinned anchor — **a key embedded in the same response is NOT a trust anchor by itself** (else an attacker self-signs + swaps the JWK). (1) select by recorded scheme/alg; (2) verify the JWS with the trust-anchored key; (3) read result/assurance/identity/challenge from the verified proof; (4) the client checks echoed challenge == its own + does its binding. **Fail closed** on: key not matching the pinned anchor; unknown scheme/alg; kid mismatch; missing/invalid JWK; invalid JWS; challenge != expected.
- A reference consumer-verification test using ONLY the view (no internal access) — MUST include a **negative test: a forged JWS signed by an attacker key + the attacker's JWK in the response FAILS against the pinned trust anchor**. NOT a runtime verifier endpoint.
### 2.4 Evidence-binding trace matrix (carried from TIP-67 v0.3, reframed)
- Field × Stored/Public/Signed/Echoed/Consumer-verifies, with `challenge` (not transaction-binding) as the integration row; decision-basis (confidence/reasonCodes) marked ✗ = P1 debt; payload/webhook ✗ = placeholder. `Signed` status is a marker, NOT proof.

## 3. Non-goals
The profile/challenge neutralization (TIP-67A); document/transaction binding (client's job); full canonical-preimage reproduction (Tier-2 — exposable on demand, deferred; the commitment already exists via manifestHash); production HSM/KMS/CA/JWKS-rotation/RFC-3161 timestamp (P0); webhook signing; decision-basis binding (P1); building SignFlow's verifier (separate repo — TagEkyc is its own provider; SignFlow writes the adapter).

## 4. Key design decisions (PINNED at the conceptual level; field-exact after 67A)
- **Sign the neutral proof, echo the opaque challenge** — NOT bind the client's transaction.
- **Enriched claim carries all decision-relevant facts** (result/assurance/identity/method) so the client trusts NO unsigned field.
- **Client binds** (`H(challenge ‖ document_hash ‖ result_hash)`); eKYC neutral.
- **Trust anchor (security):** verify against a pre-established trust anchor (pinned `kid` + key fingerprint out-of-band; JWKS = prod/P0), NEVER the embedded JWK alone. **`publicKeyFingerprint` defined exactly (single MANDATORY recipe):** `sha256` over the RFC 8785 JCS canonical JWK public params `{kty, crv, x, y}`. (DER SubjectPublicKeyInfo MAY be a future/alternate profile, but the 67B verifier contract + tests use the JWK recipe ONLY — no "or", to avoid consumer divergence.)
- **Verification scope (honest):** the consumer verifies the **neutral proof CLAIM** (signed assertions: result/assurance/identity/challenge), NOT the full evidence-package preimage. The `manifestHash` commitment to the full body exists and the preimage is **exposable on demand** (Tier-2) for deeper independent audit — but as built the consumer trusts the signed assertions; it does not independently reproduce the full hash chain.
- Verifier = contract + reference test (no runtime engine — TIP-66 lesson).

## 5. STOP/RRI
- Re-introducing transaction/document binding into eKYC → STOP (client's job).
- Any sanitization leak (raw/VaultRef/PII/private-key) → STOP.
- Production crypto / webhook / decision-basis work → STOP (out).

## 6. Recommended next action
Hold the kickoff until TIP-67A commits and freezes the opaque-challenge/profile contract; set the baseline; finalize the field-exact kickoff against 67A's names; then the normal review → build → adversarial spot-check loop. Production signing (P0) + decision-basis binding (P1) remain the open evidence items afterward.
