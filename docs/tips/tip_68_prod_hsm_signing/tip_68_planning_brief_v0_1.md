# TIP-68 Production HSM Signing Backend + Key Rotation — Planning Brief v0.8

**Version:** 0.8
**Status:** Draft — implementation planning (Tier-1 evidence-bearing, EBS-07 signing surface); Slice A of the P0 prod-crypto track; round-5 governance-metadata cleanup applied — **READY_TO_DISPATCH** (kickoff v0.6 = authoritative DoD)
**Baseline:** `4d03cf6` (TIP-67A + TIP-67B committed and **pushed to origin/master** 2026-06-24 — intentional, for GPT GitHub-context review)
**Decision basis:** `docs/p0_prod_crypto_decomposition_and_decision_brief_v0_1.md` (D-P0-1 = on-prem HSM/PKCS#11; D-P0-2 = TSA/CA/qualified deferred, legal-researched)

## 0. Decision basis (why this scope, why now)

- **D-P0-1 (Homeowner, 2026-06-23):** production signing custody = **on-prem HSM via PKCS#11**; dev/CI = **SoftHSM** (same PKCS#11 API, free, offline). The single seam `IEvidenceSigner` already isolates all crypto concretes to Infrastructure — prod = a new adapter + an environment-gated DI selector, **no architecture change**.
- **D-P0-2 (legal research, 2026-06-23, verified):** under Luật GDĐT 20/2023/QH15 (Điều 10/11) + NĐ 23/2025/NĐ-CP (replaced the dead NĐ 130/2018), a trusted timestamp (RFC-3161) and a qualified signature are **NOT required on the eKYC verification record** for evidentiary value — integrity + originator-identification + complete-form retrieval suffice. So **CA cert (item 4), RFC-3161 TSA (item 5), qualified non-repudiation (item 7) are DEFERRED** behind a legal milestone. They slot in later behind the same envelope/port. This TIP delivers **production-grade signing CUSTODY (HSM key protection) + the integrity/origin properties** the law actually requires — it is explicitly NOT a claim of legal sufficiency, non-repudiation, or certification (those remain deferred debt). "Production grade" here = the custody/integrity/origin layer, not legal admissibility status.

**Legal basis evidence trace:** the cited provisions (Luật GDĐT 20/2023/QH15 Điều 10/11; NĐ 23/2025/NĐ-CP replacing the dead NĐ 130/2018) and the deferral conclusion for CA cert / RFC-3161 TSA / qualified non-repudiation are recorded in `docs/p0_prod_crypto_decomposition_and_decision_brief_v0_1.md` §4a (Contractor deep-research, 2026-06-23, verified). TIP-68 treats D-P0-2 as a traceable decision basis from that accepted research / Homeowner governance record, NOT as a fresh legal opinion from Codex.

## 1. Intent

Make the eKYC neutral proof signable by an **HSM-held private key** (never in process memory / not a loose P12) and support **key rotation** — all WITHOUT changing the neutral-proof claim shape or the evidence hash chain. The LocalDev/in-process adapter stays as the dev fallback behind an explicit gate. (JWKS publication is deferred to Slice A2 — see §3; per-package embedded sign-time key from TIP-67B already covers verification + historical-package verification after rotation.)

## 2. Scope — ALLOWED (the floor: a working vertical slice, not 1 class)

1. **PKCS#11 HSM signing adapter** implementing `IEvidenceSigner` (both `SignAsync` + `SignProofAsync`), producing the SAME ES256/JWS envelope as today (`signatureFormat=JWS`/`signatureScheme=jws-es256-v1`/`signatureAlgorithm=ES256`/`kid`/`signedAt`/`signatureValue` + sign-time `publicKeyJwk`/`publicKeyFingerprint`). Key handle resolved via PKCS#11; **private key never leaves the HSM** (sign operation delegated to the token).
2. **Dev/CI backend = SoftHSM** through the SAME PKCS#11 adapter (so the prod code path is exercised in tests, not bypassed). The existing `LocalDevEs256JwsEvidenceSigner` is retained as a separate non-PKCS#11 dev fallback.
3. **Environment-gated DI selector** (composition root): explicit config chooses the signer backend; **fail-closed** — a production environment MUST NOT silently fall back to the in-process/generated key (misconfig → startup error, not a weak key).
4. **Key rotation support** — multiple `kid`s coexist; sign with the CURRENT key; historical packages still verify against their **persisted sign-time key** (TIP-67B already persists `publicKeyJwk`/`fingerprint` in the envelope, so no JWKS is needed for historical verification). Rotation = introduce a new `kid` as current; prior keys remain valid for verifying the packages they signed.
5. Config, **pinned `kid` scheme** (human-readable + self-describing, e.g. `tagekyc-es256-2026-v1`), docs (`signflow_integration_contract` rotation/kid note + `lld_01` envelope/backend note), tests.

### 2.1 Technical guardrails required for dispatch

**PIN/secret handling (process-readable output):**
- `Pin` MUST be loaded only from configuration/secret provider and MUST NOT appear in logs, exception messages, DTOs, test output, diagnostics, dumps, or **any process-readable report/output** (same phrasing as the private-key no-leak rule → both grep-checkable).
- Options `ToString()`/logging must redact PIN.
- Tests must assert startup/config failure messages do not echo the PIN.

**PKCS#11 session lifecycle:**
- The adapter MUST open authenticated signing sessions against the configured token and close/logout/dispose sessions deterministically.
- It MUST be safe under concurrent `SignAsync`/`SignProofAsync` calls, either by per-call session acquisition or an explicitly synchronized session pool.
- Tests must include concurrent signing smoke test with SoftHSM.

**Public-key/private-key match:**
- Adapter MUST prove the exported public key corresponds to the configured private signing key.
- At startup or first sign, perform a self-test: sign a fixed non-secret test payload with the token key and verify with the exported public key.
- Startup/signing fails closed if the public key/cert object does not match the signing key.

**PKCS#11 mechanism / digest (PINNED — no double-hash):**
- ES256 = ECDSA-P256 over EXACTLY ONE SHA-256 of the JWS signing input. PINNED: compute `SHA-256(signingInput)` in code, then sign the 32-byte digest with **`CKM_ECDSA`** (token does NOT re-hash).
- FORBIDDEN: `CKM_ECDSA_SHA256` on an already-hashed digest (double-hash → wrong signature), or raw input to `CKM_ECDSA` (un-hashed).
- Output validated as JOSE raw `r || s` (P-256 = 64 bytes; convert if a vendor returns DER). The startup self-test + existing reference verifier MUST pass — the guard that catches any digest/mechanism mismatch.

**Config contract:**
- `TagEkyc:EvidenceSigning:Backend` valid values are exactly `LocalDev` and `Pkcs11`.
- Unknown values fail at startup.
- Production or `RequireHardwareSigner=true` requires `Pkcs11`.
- Non-production may default to `LocalDev` only when Backend is absent.

**Rotation verification invariant:**
- Historical verification uses persisted sign-time public JWK/fingerprint from the package envelope, not the current HSM key.
- The rotation test should verify P1 after current config points to `kid2`. It must not require signing with `kid1` again.

**SoftHSM validation:**
- SoftHSM end-to-end test is required before acceptance. **If CI cannot run SoftHSM, raise a STOP/RRI for explicit reviewer acceptance** (report local OS/version, SoftHSM version, Pkcs11Interop version, exact test command, reason). Local-only SoftHSM evidence accepted ONLY with explicit Contractor/GPT/Homeowner sign-off — never auto-accepted.
- Silent skip is a blocker.

**`KeyLabel`/`KeyObjectId` vs `Kid` (disambiguation):**
- PKCS#11 token-object identifiers (`CKA_LABEL`/`CKA_ID`) locate the private key on the token; the JWS `Kid` is the SEPARATE public envelope key id (`tagekyc-es256-<year>-v<n>`). Do not reuse the PKCS#11 object id as the JWS `kid` (option named `KeyObjectId`, not a bare `KeyId`).

**Runtime signing surface — UNCHANGED (single proof signature):**
- The completion path signs ONLY the neutral proof (`SignProofAsync`, `VerificationCompletionApplicationService.cs:219`); `manifestHash` is bound INSIDE the proof claim. The adapter implements both `IEvidenceSigner` methods, but Slice A adds NO second signature/envelope/field — only the key backend changes. "Sign proof + manifest" = the proof signature that binds the manifest hash, not two signatures.

**Eager startup validation (NOT lazy):**
- The DI singleton factory runs on first resolve → enforce the fail-closed gate EAGERLY (`IValidateOptions` + `ValidateOnStart()` or a startup hook), and test the host build/startup path — not just the first request.

**Trust-anchor history (rotation, consumer-side):**
- "Verify via persisted sign-time key" is sound at the data level but the consumer MUST NOT trust an embedded JWK alone (it must match a pinned `kid`+`fingerprint`). Rotation requires the consumer to retain old (`kid`,`fingerprint`) in its pinned anchor set until retention + a rotation notice — documented in `signflow_integration_contract`. (JWKS deferred ≠ trust-history deferred.)

**Shared construction + forward-compat scope:**
- Claim/header/payload/JWK/fingerprint logic SHARED (not copy-pasted) between adapters; byte-conformance test (same request + pinned `signedAt` → identical header+payload).
- Forward-compat scheme dispatch: **verifier/view fails closed** on unknown `signatureScheme`; **mapper stays tolerant on read**. SoftHSM fixture provides a reproducible provisioning recipe (token init / keypair / label-id / cleanup / OS lib-path).

## 3. Scope — STOP / NOT in this slice (the ceiling)

- **NO change to the evidence hash chain or the neutral-proof claim shape** — `manifestBodyHash`/`packageHash`/`manifestHash` golden vectors MUST be byte-identical; the proof claim fields are frozen (TIP-67B). Signing is downstream/additive. This is the regression check.
- **NO CA-issued certificate / X.509 chain validation, NO RFC-3161 TSA, NO qualified-signature, NO non-repudiation legal claim** — deferred per D-P0-2 (legal milestone).
- **NO webhook signing** (separate transport surface), **NO payload signing**, **NO document/transaction binding** (neutrality held).
- **NO JWKS endpoint** — deferred to **Slice A2** (only needed for a 2nd consumer / to stop embedding the key per package / public discovery). Per-package embedded sign-time key already covers verification + post-rotation historical verification. Adding it now = YAGNI.
- **NO removal of the LocalDev adapter** (it stays as the non-PKCS#11 dev fallback behind the gate).
- **NO runtime verifier engine** (verifier stays the reference test + the self-contained consumer contract; no dead-code engine — TIP-65 lesson).
- Touching the hash graph / 67A profile-challenge / global enum converter / lld_02/04 = defect → STOP.

## 4. Definition of Done

> **Source of truth: the Kickoff (`tip_68_kickoff_v0_1.md` v0.6) §5 is the AUTHORITATIVE + COMPLETE DoD — build acceptance uses it.** The list below is the planning summary; it additionally requires (per Kickoff §5): `KeyLabel`/`KeyObjectId` vs `Kid` disambiguation, **eager** startup fail-closed (not lazy DI resolve), shared-construction byte-conformance, trust-anchor-history doc, forward-compat scope (verifier fail-closed / mapper tolerant-read), and the SoftHSM provisioning recipe.

- [ ] PKCS#11 adapter signs the **neutral proof** via SoftHSM end-to-end; the proof claim binds `manifestHash` through `signedManifestHash`. **No second manifest signature/envelope/field is added.** Envelope identical in shape to TIP-67B; round-trip verify passes.
- [ ] PIN/secret handling: `Pin` loaded only from configuration/secret provider; redacted from option logging/`ToString()`, exception messages, DTOs, diagnostic dumps, and test output; startup/config failure tests prove the PIN is not echoed.
- [ ] PKCS#11 session lifecycle: authenticated sessions close/logout/dispose deterministically and concurrent `SignAsync`/`SignProofAsync` calls are safe (per-call session or synchronized pool); SoftHSM concurrent signing smoke test passes.
- [ ] Public-key/private-key match self-test: token key signs a fixed non-secret payload and the exported public key verifies it; mismatch fails closed at startup or first sign.
- [ ] PKCS#11 mechanism/JWS format: exact JWS signing input bytes are signed with ES256 semantics equivalent to SHA-256 + ECDSA P-256; signature is converted/validated as JOSE ES256 raw `r || s`; existing reference verifier validates the produced JWS.
- [ ] Private key never extracted — sign delegated to the token (assert no private-key bytes in process/logs/DTO).
- [ ] Env-gated DI: explicit backend selection; **prod-gate fail-closed** test (production profile + missing HSM config → startup throws, never uses in-process key).
- [ ] Config contract: `TagEkyc:EvidenceSigning:Backend` accepts exactly `LocalDev`/`Pkcs11`; unknown values fail at startup; production or `RequireHardwareSigner=true` requires `Pkcs11`; non-production may default to `LocalDev` only when Backend is absent.
- [ ] Rotation test: sign package P1 under `kid1`; rotate current config to `kid2`; new packages sign under `kid2`; **P1 still verifies via its persisted sign-time key** after current config points to `kid2` and without signing with `kid1` again.
- [ ] SoftHSM validation: end-to-end SoftHSM test is required before acceptance; **if CI cannot run SoftHSM, raise a STOP/RRI for explicit Contractor/GPT/Homeowner sign-off** (report local OS/version, SoftHSM version, Pkcs11Interop version, exact test command, reason) — local-only evidence accepted ONLY with that sign-off, never auto-accepted. Silent skip is a blocker. (Full DoD: Kickoff v0.6 §5.)
- [ ] Golden `manifestBodyHash`/`packageHash`/`manifestHash` **byte-identical**; full `dotnet test` green.
- [ ] Docs: `signflow_integration_contract` rotation + `kid`-resolution note (historical packages verify via embedded sign-time key; no JWKS in Slice A); `lld_01` envelope/backend note.

## 5. Review tier

**Tier-1 adversarial (EBS-07 signing surface)** — attacks the review/build spot-check must attempt: (a) does rotation break historical-package verification? (must not — old packages verify via their persisted sign-time key); (b) does the PKCS#11 adapter leak the private key anywhere (process/logs/DTO)? (must not — sign delegated to the token); (c) prod-gate fail-closed (production profile + missing HSM config MUST throw at startup, never use the in-process key); (d) golden byte-identical (no hash drift); (e) does the envelope/verifier still fail-closed on an unknown `signatureScheme`? (forward-compat guard).

## 6. Debt (carried / created)

| Item | Severity | Disposition |
| --- | --- | --- |
| Real HSM provisioning (prod hardware/cluster, backup, PKCS#11 vendor lib) | P0-infra | Out of code scope; ops task before prod cutover |
| CA-issued cert / X.509 chain / RFC-3161 TSA / qualified non-repudiation | Deferred | Behind legal milestone (D-P0-2); add via same envelope/port |
| JWKS publication / public key discovery | Deferred | **Slice A2** — when a 2nd consumer appears or per-package embedding is dropped |
| Webhook signing | Separate | Distinct transport surface |
| Ops weight of HSM vs KMS | Accepted | Seam allows later swap to KMS if ops proves heavy |
| Decision-basis binding (confidence/reasonCodes unsigned) | P1 | Separate legal-lens TIP |

## 7. Open questions for review

- **JWKS — RESOLVED: deferred to Slice A2** (per-package embedded sign-time key already covers verification + post-rotation; YAGNI now).
- **kid scheme — DECIDED: human-readable + self-describing** (`tagekyc-es256-2026-v1`), crypto-agility per TIP-65/66.
- PKCS#11 .NET binding choice (lean: **Pkcs11Interop**, the standard managed wrapper) — confirm CI-portable with SoftHSM at build time.

## 8. Forward-compat: CA migration (preserve readiness, build nothing now)

Moving to a CA-issued certificate later must be **additive, non-disruptive**. CA does NOT change the crypto — it wraps the SAME ES256 public key with a CA signature; the HSM private key + signing op are unchanged. To keep that migration free of operational disruption, Slice A MUST preserve three invariants (all already the established pattern — this section just forbids breaking them):

1. **`IEvidenceSigner` stays the only seam** — a future CA-backed adapter swaps via DI; no crypto concrete leaks into Application/Api.
2. **Envelope stays self-describing + scheme-versioned + additively extensible** — a future CA scheme adds nullable fields (e.g. `x5c` cert chain) under a NEW `signatureScheme` (e.g. `jws-es256-x5c-v1`); the verifier dispatches by the recorded scheme and **fails closed on unknown** (the forward-compat DoD item, Kickoff §5, guards this).
3. **Historical immutability** — never re-sign/mutate prior packages; old packages verify under their recorded scheme (pinned fingerprint), new ones under CA. Coexistence, not in-place migration → zero disruption to existing evidence.

Known non-zero cost when CA actually lands (a future slice, NOT now): the consumer (SignFlow) verifier must be extended to validate the `x5c` chain against trusted CA roots — a coordinated versioned-contract rollout. Old verification never breaks; the hash chain is never touched (signing is downstream of `manifestHash`).

## Changelog

### v0.8 - governance-metadata cleanup round-5 (2026-06-24)
- Baseline line corrected: TIP-67A+67B are **pushed to origin/master** (not "parked-not-pushed; ahead 2") — matches the kickoff + reality.
- Decision brief status refreshed ("ready to draft" → "Slice A drafted = TIP-68").
- Planning DoD SoftHSM bullet hardened to the STOP/RRI sign-off requirement (matched to kickoff §5, so a builder reading the summary can't under-read it).

### v0.7 - doc-consistency round-4 (2026-06-24)
- Planning §4 DoD fixed "signs the proof + manifest" → "signs the neutral proof; claim binds `manifestHash`; no second signature"; added a **Source-of-truth pointer to Kickoff v0.6 §5** (the authoritative complete DoD) listing the items planning only summarizes (eager startup, trust-anchor history, shared-construction, mapper scope, SoftHSM recipe, `KeyObjectId`≠`Kid`).
- Fixed stale cross-ref "DoD §5(e)" → "the forward-compat DoD item, Kickoff §5".
- (Decision brief) refreshed the stale "brief v0.4 / kickoff v0.3, Pending review" governance line → v0.7/v0.6, round-3 patched.

### v0.6 - deep-code-review round-3 (2026-06-24) — 4 P1 + 3 P2 patched
- P1: runtime signs ONLY the neutral proof (`SignProofAsync`); "proof + manifest" clarified as the proof claim that BINDS `manifestHash`, not two signatures; adapter implements both methods but adds no second signature.
- P1: rotation trust-anchor HISTORY — consumer retains old (`kid`,`fingerprint`) in its pinned set until retention + rotation notice (documented in `signflow_integration_contract`); "verify via embedded key alone" forbidden.
- P1: PKCS#11 mechanism reconciled + byte-equivalence to LocalDev `SignData(SHA256, IeeeP1363)` pinned (the §3.1 digest rule already chose SHA-256-in-code → `CKM_ECDSA`).
- P1: fail-closed gate enforced EAGERLY at startup (`ValidateOnStart`/startup self-test), test the host build path — not lazy on first resolve.
- P2: shared crypto-agnostic construction (no copy-paste) + byte-conformance test; forward-compat scope pinned (verifier/view fail-closed, mapper tolerant-read); SoftHSM provisioning recipe.
- (Confirmed already-fixed from round-2, reviewer read a pre-patch copy): decision-brief JWKS-drift + "production grade" overclaim — both already corrected; decision brief §0 line tightened.

### v0.5 - GPT round-2 P2 patches (2026-06-24) — no P1 blockers
- P2-01: PIN no-leak uses the same "process-readable output" phrasing as the private-key rule (grep-consistent).
- P2-02: SoftHSM unavailable in CI = a STOP/RRI for explicit Contractor/GPT/Homeowner sign-off (not auto-accepted "local-only").
- P2-03: disambiguated PKCS#11 token-object identifiers (`KeyLabel`/`KeyObjectId` = `CKA_LABEL`/`CKA_ID`) vs the JWS `Kid` (public envelope key id) — do not conflate.

### v0.4 - review round-2 patch (2026-06-24)
- Pinned the PKCS#11 digest rule (§2.1) to prevent double-hash / wrong signing input: SHA-256 in code → sign digest with `CKM_ECDSA`; forbid `CKM_ECDSA_SHA256`-on-digest and raw-input signing.
- Narrowed "production grade" wording (§0) to **signing custody + integrity/origin**, NOT a legal-sufficiency / non-repudiation / certification claim (avoid overclaim).
- (Cross-doc) the P0 decision brief was corrected: JWKS moved out of Slice A → Slice A2 (it had stale "Slice A = items 1+2+3" text).
- (Builder scope) README/TIP-index updates are Contractor housekeeping, not in the builder DoD (kickoff aligned).

### v0.3 - technical review patch (2026-06-24)
- Added required guardrails for PIN/secret handling, PKCS#11 session lifecycle/concurrency, public/private key match self-test, PKCS#11 ES256/JOSE signature format, exact backend config contract, rotation verification invariant, and mandatory SoftHSM validation/waiver reporting.
- Added legal evidence trace wording for D-P0-2 and clarified that TIP-68 uses the accepted legal research as decision basis, not a fresh Codex legal opinion.
- Corrected the DoD docs line so JWKS stays deferred to Slice A2 and does not re-enter Slice A through documentation scope.

### v0.2 — pre-review revision (2026-06-24)
- Removed JWKS from Slice A → deferred to Slice A2 (per-package embedded sign-time key from TIP-67B already covers verification + post-rotation historical verification; JWKS now = YAGNI). Title/intent/scope/DoD/review/debt updated.
- Pinned the `kid` scheme (human-readable + self-describing).
- Added §8 Forward-compat: CA migration (3 invariants to preserve so a later CA move is additive + non-disruptive; verifier fail-closed-on-unknown-scheme added to DoD §5).

### v0.1 — initial draft
- Initial draft before JWKS was deferred: PKCS#11 HSM signing + env-gated DI + JWKS + rotation.
