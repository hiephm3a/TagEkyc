# TIP-68 Production HSM Signing Backend + Key Rotation — Kickoff v0.6

**Version:** 0.6
**Status:** Draft — dispatch-ready spec (Tier-1 / EBS-07 signing surface, code change); round-3 P1+P2 + round-4 section-order patches applied — re-confirm before dispatch
**Baseline:** `4d03cf6` (TIP-67B committed + pushed to origin/master). Planning brief: `tip_68_planning_brief_v0_1.md` v0.8. Decision basis: `docs/p0_prod_crypto_decomposition_and_decision_brief_v0_1.md` (D-P0-1 HSM/PKCS#11; D-P0-2 TSA/CA deferred).

## 1. Goal

Production-grade signing **custody** (key protection + integrity/origin — NOT a legal-sufficiency/non-repudiation/certification claim, those deferred per D-P0-2): the eKYC **neutral proof** is signed by an **HSM-held ES256 key via PKCS#11** (private key never leaves the token), selectable by an **environment-gated, fail-closed** DI switch, with **key rotation** that never breaks historical-package verification — and ZERO change to the evidence hash chain or the neutral-proof claim shape. (See §3.0 — Slice A swaps the key backend only; it adds NO new signature/field to the runtime.)

## 2. The seam (no architecture change)

`IEvidenceSigner` (`src/TagEkyc.Application/Ports/EvidenceSignerPorts.cs`) is the only signing seam; crypto concretes already live in `Infrastructure/Signing/`. This TIP adds ONE new adapter + a DI selector. Do NOT introduce an algorithm-specific port; do NOT leak PKCS#11/X509 into Application/Api.

## 3. Pinned design

### 3.0 Runtime signing surface — UNCHANGED (single proof signature)
- The adapter implements BOTH `IEvidenceSigner` methods (`SignAsync` + `SignProofAsync`) for interface completeness — any caller of either gets HSM custody.
- BUT Slice A changes NO runtime signing behaviour. The completion path today calls **only `SignProofAsync`** (`VerificationCompletionApplicationService.cs:219`), producing ONE neutral-proof signature whose claim BINDS `manifestHash` (the `signedManifestHash` field). The manifest is **not separately signed**. Slice A MUST NOT add a second signature, a second envelope, or a new field — only the key BACKEND changes.
- Read "sign proof + manifest" anywhere in this TIP as "the proof signature that BINDS the manifest hash," NOT two signatures. Adding a second signing operation = scope creep → STOP/RRI.

### 3.1 New adapter `Pkcs11Es256JwsEvidenceSigner` (Infrastructure/Signing/)
- Implements `IEvidenceSigner` (`SignAsync` + `SignProofAsync`) producing the SAME envelope as today: `signatureFormat=JWS`, `signatureScheme=jws-es256-v1`, `signatureAlgorithm=ES256`, `kid`, `signedAt`, `signatureValue`, sign-time `publicKeyJwk` + `publicKeyFingerprint`. The JWS payload (canonical claim object), header (`{alg:ES256, kid}`), signature encoding (IEEE P1363 fixed-field), and fingerprint recipe (sha256 over JCS `{kty,crv,x,y}`) are IDENTICAL to `LocalDevEs256JwsEvidenceSigner` — only the key custody differs.
- Uses **Pkcs11Interop** (managed PKCS#11 wrapper). The sign operation runs ON the token per the pinned digest rule below (§ PKCS#11 mechanism); the **private key handle is never exported** to managed memory. The produced signature MUST be byte-equivalent to LocalDev's `ECDsa.SignData(signingInput, SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)` over the same signing input — proven by the existing reference verifier accepting the JWS.
- The public key is read from the token's public-key object (or the matching cert object) and exported to `publicKeyJwk` `{kty:EC,crv:P-256,x,y}` (public-only, no `d`).
- **Shared construction (no copy-paste drift, P2):** the claim/header/payload JSON building, JWK export, and fingerprint recipe MUST be SHARED between LocalDev and PKCS#11 adapters — extract an internal helper; do NOT copy-paste the private methods from `LocalDevEs256JwsEvidenceSigner`. **Conformance test:** for the SAME request + a pinned `signedAt`, both adapters produce BYTE-IDENTICAL protected-header + payload; only the signature segment differs by key.
- Options `Pkcs11Es256JwsEvidenceSignerOptions` (config section `TagEkyc:EvidenceSigning:Pkcs11`): `LibraryPath` (PKCS#11 module .so/.dll), `TokenLabel` (or slot), `Pin`, **token-object identifiers** `KeyLabel` (PKCS#11 `CKA_LABEL`) / `KeyObjectId` (PKCS#11 `CKA_ID`), and the **JWS** `Kid`. No secrets hard-coded; PIN from config/secret store.
- **Disambiguate `KeyLabel`/`KeyObjectId` vs `Kid` (PINNED):** `KeyLabel`/`KeyObjectId` locate the PRIVATE KEY OBJECT on the token (PKCS#11 attributes); `Kid` is the PUBLIC envelope/JWS-header key identifier (`tagekyc-es256-<year>-v<n>`) recorded in the signature + used by consumers. They are SEPARATE; do NOT reuse the PKCS#11 object id as the JWS `kid` (rename the option from a bare `KeyId` to `KeyObjectId` to avoid the conflation).

Required adapter guardrails:
- **PIN/secret handling (process-readable output):** `Pin` MUST be loaded only from configuration/secret provider and MUST NOT appear in logs, exception messages, DTOs, test output, diagnostics, dumps, or **any process-readable report/output** (same phrasing as the private-key no-leak rule, so both are grep-checkable). Options `ToString()`/logging must redact PIN. Tests must assert startup/config failure messages do not echo the PIN.
- **PKCS#11 session lifecycle:** the adapter MUST open authenticated signing sessions against the configured token and close/logout/dispose sessions deterministically. It MUST be safe under concurrent `SignAsync`/`SignProofAsync` calls, either by per-call session acquisition or an explicitly synchronized session pool. Tests must include concurrent signing smoke test with SoftHSM.
- **Public-key/private-key match:** adapter MUST prove the exported public key corresponds to the configured private signing key. At startup or first sign, perform a self-test: sign a fixed non-secret test payload with the token key and verify with the exported public key. Startup/signing fails closed if the public key/cert object does not match the signing key.
- **PKCS#11 mechanism / digest rule (PINNED — prevent double-hash / wrong signing input):** the JWS signing input is `ASCII(base64url(protectedHeader) + "." + base64url(payload))`. ES256 = ECDSA-P256 over **exactly one** SHA-256 of that input. Pin ONE mechanism and forbid the other:
  - **CHOSEN:** compute `digest = SHA-256(signingInput)` IN CODE, then sign the 32-byte digest with **`CKM_ECDSA`** (raw ECDSA — the token does NOT hash). Pass the digest, never the raw input, to `CKM_ECDSA`.
  - **FORBIDDEN:** calling `CKM_ECDSA_SHA256` (token hashes) on an already-SHA-256'd digest → **double-hash** = wrong signature; equally forbidden: passing the raw input to `CKM_ECDSA` (signs un-hashed). Either mistake yields a signature the verifier rejects.
  - Output: PKCS#11 ECDSA returns raw `r || s` already (fixed 64 bytes for P-256) — validate length and emit as JOSE `r || s` base64url; if a vendor returns DER, convert. The startup self-test + the existing reference verifier MUST pass on the produced JWS (this is the guard that catches any digest/mechanism mismatch).

### 3.2 Environment-gated DI selector (composition root, `Program.cs`)
- Config contract: `TagEkyc:EvidenceSigning:Backend` valid values are exactly `LocalDev` and `Pkcs11`. Unknown values fail at startup. Production or `RequireHardwareSigner=true` requires `Pkcs11`. Non-production may default to `LocalDev` only when Backend is absent.
- Config `TagEkyc:EvidenceSigning:Backend` ∈ { `LocalDev`, `Pkcs11` }. The composition root resolves the matching `IEvidenceSigner`.
- **FAIL-CLOSED gate:** if the runtime is a production environment (`IHostEnvironment.IsProduction()` OR explicit `TagEkyc:EvidenceSigning:RequireHardwareSigner=true`) then Backend MUST be `Pkcs11` AND its config MUST be present/valid — otherwise **throw at startup** (do NOT fall back to LocalDev / in-process key). Non-production may default to `LocalDev` only when Backend is absent.
- `LocalDevEs256JwsEvidenceSigner` is RETAINED (non-prod fallback); not removed.
- **Eager validation — NOT lazy (PINNED):** `AddSingleton<IEvidenceSigner>(factory)` runs the factory LAZILY on first resolve, so a production misconfig would otherwise only throw on the FIRST REQUEST, not at startup. The fail-closed gate MUST be enforced EAGERLY at startup — via `IValidateOptions<…>` + `.ValidateOnStart()`, OR an `IHostedService`/startup hook that constructs + self-tests the signer before the app accepts traffic. The fail-closed test MUST exercise the **host build/startup path** (host build throws on prod+missing-Pkcs11), NOT merely the first request path.

### 3.3 Key rotation
- The CURRENT signing key/`kid` come from the active backend config. Signing always uses the current key.
- Historical packages already persist their sign-time `publicKeyJwk`/`publicKeyFingerprint`/`kid` in the envelope (TIP-67B) → they verify against THAT key, independent of the current key. **No JWKS needed** (deferred to A2).
- Rotation procedure (doc + test): provision a new key/`kid` in the HSM, point config at it; new packages sign under the new `kid`; packages signed under the old `kid` still verify via their persisted sign-time key.
- **`kid` scheme (pinned):** human-readable + self-describing, `tagekyc-es256-<year>-v<n>` (e.g. `tagekyc-es256-2026-v1`).
- Rotation verification invariant: historical verification uses persisted sign-time public JWK/fingerprint from the package envelope, not the current HSM key. The rotation test should verify P1 after current config points to `kid2`. It must not require signing with `kid1` again.
- **Trust-anchor history (consumer-side — DOC requirement, P1):** "historical packages verify via the persisted sign-time key" is sound at the DATA level but NOT at the TRUST level. Per `signflow_integration_contract`, the consumer MUST NOT trust an embedded JWK alone — it must match a PINNED (`kid`, `publicKeyFingerprint`) in its out-of-band anchor set. Therefore rotation REQUIRES: (1) the consumer retains the OLD (`kid`, `fingerprint`) in its pinned anchor SET until the old packages' retention expires; (2) a rotation NOTICE is issued when a new `kid` becomes current. Without this, "old package verifies after rotation" silently degrades into "trust whatever fingerprint sits inside the package" (defeats the trust anchor). Slice A MUST document this trust-anchor-history + rotation-notice obligation in `signflow_integration_contract` (JWKS deferred ≠ trust-history deferred). No code beyond the doc + the `kid`-set verification already in the reference verifier.

### 3.4 SoftHSM for dev/CI (same code path)
- Dev/CI exercises `Pkcs11Es256JwsEvidenceSigner` against **SoftHSM2** (same PKCS#11 API) so the production path is tested, not bypassed. The PKCS#11 library path/token are test-configured. If the CI image lacks SoftHSM, the PKCS#11 integration tests are gated behind an explicit trait and the gate is **logged loudly** (no silent skip) — but the preferred outcome is SoftHSM installed in CI. Confirm Pkcs11Interop + SoftHSM2 portability at build time and report.

- SoftHSM validation: SoftHSM end-to-end test is required before acceptance. **If CI cannot run SoftHSM, raise it as a STOP/RRI for explicit reviewer acceptance** (report local OS/version, SoftHSM version, Pkcs11Interop version, exact test command, and the reason CI could not run it). Local-only SoftHSM evidence may be accepted ONLY with explicit Contractor/GPT/Homeowner sign-off — never auto-accepted. Silent skip is a blocker.
- **Provisioning recipe (reproducible, NOT machine-dependent):** the test fixture MUST script SoftHSM setup end-to-end so it runs on a clean box: token init (`softhsm2-util --init-token --slot … --label …`), slot/token label, ES256 (P-256) keypair generation/import, `CKA_LABEL`/`CKA_ID` convention matching the `Pkcs11` options, deterministic teardown/cleanup, and Windows/Linux library-path resolution (`SOFTHSM2_MODULE` / configured `LibraryPath`). NO dependency on a pre-existing personal-machine token.

### 3.5 Legal decision-basis trace (no new legal opinion)
- Legal basis evidence: the cited provisions (Luật GDĐT 20/2023/QH15 Điều 10/11; NĐ 23/2025/NĐ-CP replacing the dead NĐ 130/2018) and the CA/TSA/qualified deferral conclusion are recorded in `docs/p0_prod_crypto_decomposition_and_decision_brief_v0_1.md` §4a (Contractor deep-research, verified).
- TIP-68 treats D-P0-2 as an accepted decision basis from traceable legal research / Homeowner governance, not as a fresh legal opinion from Codex.

### 3.6 Forward-compat scheme dispatch — scope PINNED (P2)
- The forward-compat guard (a future `jws-es256-x5c-v1` must be additive) splits by layer: the **VERIFIER / view path FAILS CLOSED on an unknown `signatureScheme`** (that is where trust decisions are made — already proven by the 67B reference verifier test). The **persistence MAPPER stays TOLERANT on read** — it MUST NOT reject a row with a future/unknown scheme on read (same tolerance posture as 67A `ParseProfile`), else a later CA scheme would break reads of new rows.
- Pin: **fail-closed = verifier/view; tolerant-read = mapper.** Slice A adds no new scheme; this only fixes the scope wording so the builder knows WHICH layer rejects unknown.

## 4. STOP / NOT in scope

- NO change to the evidence hash chain or the neutral-proof claim shape — `manifestBodyHash`/`packageHash`/`manifestHash` golden vectors MUST be byte-identical; proof claim fields frozen.
- NO JWKS endpoint (Slice A2). NO CA cert / X.509 chain / RFC-3161 TSA / qualified-sig / non-repudiation claim (deferred, D-P0-2). NO webhook/payload signing. NO document/transaction binding (neutrality). NO runtime verifier engine.
- NO removal of `LocalDevEs256JwsEvidenceSigner`. NO new algorithm-specific port. NO PKCS#11/X509 leak into Application/Api.
- Touching the hash graph / 67A profile-challenge / global enum converter / lld_02/04 = defect → STOP.

## 5. Definition of Done

- [ ] `Pkcs11Es256JwsEvidenceSigner` signs the **neutral proof** (the runtime's only signing call — `SignProofAsync`) via **SoftHSM** end-to-end; envelope shape identical to TIP-67B; reference verifier + round-trip pass. **No second signature / envelope / field is added — only the key backend changes (§3.0).**
- [ ] **Shared construction conformance:** claim/header/payload build + JWK export + fingerprint are SHARED (not copy-pasted) between adapters; for the same request + pinned `signedAt`, LocalDev and PKCS#11 produce BYTE-IDENTICAL protected-header + payload (only the signature segment differs).
- [ ] **PIN/secret handling:** `Pin` loaded only from configuration/secret provider; never in logs, exceptions, DTOs, test output, diagnostics, dumps, or **any process-readable output** (same phrase as the private-key rule); options logging/`ToString()` redacts PIN; startup/config failure tests prove the PIN is not echoed.
- [ ] **`KeyLabel`/`KeyObjectId` vs `Kid` disambiguated:** PKCS#11 token-object identifiers (`CKA_LABEL`/`CKA_ID`) locate the private key; the JWS `Kid` is the separate public envelope key id — not reused from the PKCS#11 object id.
- [ ] **PKCS#11 session lifecycle/concurrency:** authenticated sessions close/logout/dispose deterministically and concurrent `SignAsync`/`SignProofAsync` calls are safe (per-call session or synchronized pool); SoftHSM concurrent signing smoke test passes.
- [ ] **Public-key/private-key match:** startup or first-sign self-test signs a fixed non-secret payload with the token key and verifies with the exported public key; mismatch fails closed.
- [ ] **PKCS#11 mechanism / digest (no double-hash):** SHA-256 computed in code → 32-byte digest signed via `CKM_ECDSA` (token does NOT re-hash); NOT `CKM_ECDSA_SHA256` on a pre-hashed digest, NOT raw input to `CKM_ECDSA`; output validated as JOSE raw `r || s`; existing reference verifier validates the produced JWS (catches any digest/mechanism mismatch).
- [ ] **Private key never exported** — sign delegated to the token; test/assert no private-key material (`d`, PKCS#8, raw scalar) appears in the envelope, DTO, logs, or process-readable output.
- [ ] **Env-gate fail-closed test (EAGER):** production environment + missing/invalid Pkcs11 config → the **host build/startup path throws** (eager `ValidateOnStart`/startup self-test, NOT lazy on first resolve); test exercises the startup path; never resolves the in-process LocalDev key in production.
- [ ] **Config contract:** `Backend` accepts exactly `LocalDev`/`Pkcs11`; unknown values fail at startup; production or `RequireHardwareSigner=true` requires `Pkcs11`; non-production may default to `LocalDev` only when Backend is absent.
- [ ] **Rotation test:** sign P1 under `kid1`; switch current config to `kid2`; new package signs under `kid2`; **P1 still verifies via its persisted sign-time key** after current config points to `kid2`, without requiring signing with `kid1` again.
- [ ] **Trust-anchor history documented:** `signflow_integration_contract` states the consumer must retain old (`kid`,`fingerprint`) in its pinned anchor SET until retention + that a rotation notice is issued — so "old package verifies after rotation" cannot degrade to "trust the embedded JWK alone."
- [ ] **SoftHSM validation:** end-to-end SoftHSM test required before acceptance; if CI cannot run SoftHSM, raise a **STOP/RRI for explicit reviewer acceptance** (report local OS/version, SoftHSM version, Pkcs11Interop version, exact test command, reason) — local-only evidence accepted ONLY with Contractor/GPT/Homeowner sign-off. Silent skip is a blocker.
- [ ] **Forward-compat guard (scope pinned, §3.6):** the **verifier/view fails closed** on an unknown `signatureScheme` (bogus-scheme test); the **persistence mapper stays tolerant on read** (does not reject an unknown/future-scheme row) — assert BOTH layers.
- [ ] **SoftHSM provisioning recipe:** the test fixture scripts token init / keypair gen / `CKA_LABEL`-`CKA_ID` convention / cleanup / Windows-Linux library-path resolution — reproducible on a clean box, no personal-machine token dependency.
- [ ] Golden `manifestBodyHash`/`packageHash`/`manifestHash` **byte-identical**; full `dotnet test` green.
- [ ] Docs: `signflow_integration_contract_v0_1.md` rotation + `kid`-resolution note (historical packages verify via embedded sign-time key); `lld_01` envelope/backend note. (README TIP-68 row + TIP index = **Contractor housekeeping, NOT in the builder's scope** — do not edit README.)

## 6. Tier-1 adversarial checks (build self-attack + Contractor spot-check)

(a) rotation breaks historical verify? must NOT. (b) PKCS#11 adapter leaks private key (process/logs/DTO)? must NOT. (c) prod-gate silently uses in-process key? must NOT (startup throw). (d) golden hash drift? must NOT. (e) unknown `signatureScheme` accepted? must NOT (fail-closed — forward-compat guard).

## 7. Build instructions

Build on baseline `4d03cf6`. Implement §3 exactly. **Validation:** `dotnet build` + full `dotnet test` (report counts) + confirm golden vectors byte-identical + report the SoftHSM/Pkcs11Interop CI-portability result. **DO NOT COMMIT** (await Contractor adversarial spot-check on CODE). Report per: (a) adapter + DI gate diff; (b) test results incl. the 5 Tier-1 checks; (c) golden unchanged; (d) SoftHSM CI status; (e) any STOP/RRI hit.

## Changelog

### v0.6 - doc-consistency round-4 (2026-06-24)
- Reordered subsections to monotonic 3.4 → 3.5 (legal) → 3.6 (forward-compat); no ref changes.

### v0.5 - deep-code-review round-3 (2026-06-24) — 4 P1 + 3 P2 patched
- P1 §3.0: runtime signs ONLY the neutral proof (`SignProofAsync`); no second signature/field; "proof + manifest" = proof binds `manifestHash`.
- P1 §3.3: rotation trust-anchor history + rotation notice (consumer keeps old kid/fingerprint pinned until retention) → `signflow_integration_contract`.
- P1 §3.1: signature byte-equivalent to LocalDev `SignData(SHA256, IeeeP1363)`; line-19 reconciled with the pinned digest rule.
- P1 §3.2: eager startup validation (`ValidateOnStart`/startup self-test), test the host build path — not lazy-on-first-resolve.
- P2 §3.1: shared construction (no copy-paste) + byte-conformance test. §3.6: forward-compat scope (verifier/view fail-closed; mapper tolerant-read). §3.4: SoftHSM provisioning recipe. DoD updated for all.

### v0.4 - GPT round-2 P2 patches (2026-06-24) — no P1 blockers
- P2-01: PIN no-leak uses "process-readable output" phrasing (grep-consistent with the private-key rule) — §3.1 + DoD.
- P2-02: SoftHSM-unavailable-in-CI = STOP/RRI for explicit reviewer sign-off — §3.4 + DoD.
- P2-03: §3.1 options disambiguate PKCS#11 `KeyLabel`/`KeyObjectId` (token object) vs JWS `Kid` (public envelope id); added DoD line.

### v0.3 - review round-2 patch (2026-06-24)
- §3.1: pinned the PKCS#11 digest rule (SHA-256 in code → `CKM_ECDSA` on the digest; forbid double-hash via `CKM_ECDSA_SHA256`-on-digest or raw-input signing) + DoD line updated.
- §1: narrowed "production-grade" to signing custody + integrity/origin (not legal-sufficiency/non-repudiation/certification).
- DoD: README/TIP-index removed from builder scope (Contractor housekeeping).

### v0.2 - technical review patch (2026-06-24)
- Added required guardrails for PIN/secret handling, PKCS#11 session lifecycle/concurrency, public/private key match self-test, PKCS#11 ES256/JOSE signature format, exact backend config contract, rotation verification invariant, mandatory SoftHSM validation/waiver reporting, and D-P0-2 legal evidence trace.
