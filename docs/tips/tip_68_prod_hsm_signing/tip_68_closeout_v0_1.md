# TIP-68 Production HSM Signing Backend + Key Rotation — Closeout v0.1

**Status:** ACCEPT — Contractor adversarial spot-check (2 rounds) CLEAN after the SoftHSM e2e remediation; ready for commit by allowlist (push per standing delegation)
**Baseline:** `12f5302` (doc-sync + TIP-68 spec; code unchanged from `4d03cf6`)
**Spec:** `tip_68_kickoff_v0_1.md` v0.6 (§5 = authoritative DoD) · Decision basis: `docs/p0_prod_crypto_decomposition_and_decision_brief_v0_1.md` (D-P0-1 HSM/PKCS#11; D-P0-2 TSA/CA/qualified deferred, legal-researched)
**Date:** 2026-06-24

## Summary

P0 prod-crypto **Slice A**: production signing CUSTODY for the eKYC neutral proof via an **HSM-held ES256 key over PKCS#11** (`Pkcs11Es256JwsEvidenceSigner`), behind the single `IEvidenceSigner` seam — the private key never leaves the token. Selectable by an **environment-gated, fail-closed** DI switch; **key rotation** via the TIP-67B persisted sign-time key (no JWKS). Dev/CI exercises the SAME PKCS#11 code path against **SoftHSM2**. ZERO change to the evidence hash chain or the neutral-proof claim shape. This is signing CUSTODY + integrity/origin — NOT a legal-sufficiency / non-repudiation / certification claim (those deferred).

## Outcome vs Intent

| Intent (kickoff v0.6) | Outcome | Status |
| --- | --- | --- |
| PKCS#11 HSM adapter behind `IEvidenceSigner`, same envelope; key never leaves token | `Pkcs11Es256JwsEvidenceSigner` (Pkcs11Interop 5.3.0); handle-only `C_Sign`; no `CKA_VALUE` export | ✅ |
| Digest rule: SHA-256 in code → `CKM_ECDSA` (no double-hash) | Implemented + e2e-verified; JOSE P1363 r‖s normalize; byte-equivalent to LocalDev `SignData(SHA256,IeeeP1363)` | ✅ |
| Shared construction (no copy-paste) + byte-conformance | `Es256JwsEvidenceSignatureBuilder` shared by both adapters; identical header+payload test | ✅ |
| Eager, fail-closed DI gate (NOT lazy) | `ValidateOnStart` + `EvidenceSignerStartupValidationHostedService.StartAsync` self-test → host build/startup throws on prod+missing-Pkcs11; startup-path tests | ✅ |
| Rotation: old packages verify via persisted sign-time key; trust-anchor history | Sign-time-key verification; consumer trust-anchor-history + rotation-notice documented in `signflow_integration_contract` | ✅ |
| SoftHSM end-to-end on the prod code path (not bypassed) | 5 e2e tests OPEN a real SoftHSM2 token, sign on-token, reference-verify the JWS, fail-closed on key mismatch, concurrent-sign | ✅ |
| Forward-compat scope | verifier/view fail-closed on unknown scheme; mapper tolerant-read | ✅ |
| No JWKS / CA / TSA / qualified / webhook / document binding; golden byte-identical | Held; golden vectors byte-identical | ✅ |

## Hash Chain Posture

Signing remains downstream of `manifestHash`; golden vectors **byte-identical** (`Tip65EvidenceCanonicalizationTests` 7/7):

- `manifestBodyHash = sha256:e8aa856e1cc6fd31f085f3ace99ef0ddafc6f8538dd0443def7d4bf91ccc96d4`
- `packageHash      = sha256:0a3cbc9b031c2131822b676a5f40c11b478636e74704732fd558655833c122c0`
- `manifestHash     = sha256:124c36f819308aa86cb5813f63ec11d7bdbc410cbd97b2d5e7f23d6bf60dc2af`

## Validation

- `dotnet build TagEkyc.sln` — passed, 0 warnings.
- `dotnet test TagEkyc.sln` — **190/190**, skipped 0.
- TIP-68 filter — **14/14**; SoftHSM e2e (`Tip68SoftHsmE2ETests`) — **5/5, skipped 0, token genuinely opened**.
- Golden — 7/7 byte-identical.
- SoftHSM2 **2.5.0** (Chocolatey `softhsm.portable`, module `softhsm2-x64.dll`); canonical override = `SOFTHSM2_MODULE`.
- New dependency: `Pkcs11Interop 5.3.0`.

## Review Ladder

- **V1 (Tier-0 automated):** build clean; 190/190; golden 7/7. PASS.
- **V2 (Tier-1 adversarial, on CODE — 2 rounds):**
  - **Round 1** found a CRITICAL/BLOCKER: the original 9 tests were all green but NONE opened a token — the PKCS#11 sign path had ZERO runtime coverage (the green count actively masked the gap; the provisioning `.ps1` was orphaned). NOT accepted; forced the SoftHSM e2e.
  - **Round 2** (post-remediation) verified on CODE that the 5 new e2e tests GENUINELY exercise the HSM path: real module DLL + on-token keygen (`CKA_TOKEN`/`SENSITIVE`/non-`EXTRACTABLE`), hard-fail (no skip/swallow), on-token `CKM_ECDSA`, JWS reference-verified under the token-exported public JWK + pinned fingerprint, and the fail-closed self-test reached via a REAL key mismatch (throw is a string unique to that path). PASS.
- **V3 (Tier-2 crypto):** real ECDSA P-256 on the token, fingerprint-pinned trust anchor, fail-closed self-test, PIN never in process-readable output, no key export — verified at **dev/SoftHSM** level. Production hardware HSM provisioning + CA/TSA/qualified remain deferred (D-P0-2). PASS at dev-foundation.

**Methodology note (the strongest vindication so far of "test-green ≠ correct"):** a 9/9-green build hid that the central deliverable (HSM signing) never executed; only adversarial CODE reading aimed at the right surface caught it, and the fix was forced to actually open a token before acceptance.

## Decision / Branch Disposition

- Built on `12f5302`; **NOT committed** at build time (awaited spot-check). Now ACCEPT → commit by **allowlist** (Codex), push per standing delegation.
- Allowlist EXCLUDES known-dirty: `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, all `bin/obj`.
- Pre-commit cleanup applied: `SOFTHSM2_MODULE` made the canonical fixture override (CI-portable, not tied to one machine's `%TEMP%`); fixture reverts the process `PATH` prepend on dispose.

## Debt

| Item | Severity | Disposition |
| --- | --- | --- |
| Production hardware HSM provisioning (real HSM cluster, backup, vendor PKCS#11 lib, key ceremony) | P0-infra | Ops task before prod cutover; the code seam is ready |
| SoftHSM in CI pipeline (run the e2e every build) | P1 | `SOFTHSM2_MODULE` canonical entry in place; CI pipeline wiring deferred |
| CA-issued cert / X.509 chain / RFC-3161 TSA / qualified non-repudiation | Deferred | Behind legal milestone (D-P0-2); additive via the same envelope/scheme |
| JWKS / public-key discovery | Deferred | Slice A2 |
| Decision-basis binding (confidence/reasonCodes unsigned) | P1 | Separate legal-lens TIP |
| `LoadPkcs11Library` per-sign (perf) | Low | Acceptable for dev; revisit if prod throughput needs a pooled library/session |

## GDrive Sync / Doc Posture

- Docs changed: `lld_01_data_model_v0_1.md` (signing backend note), `signflow_integration_contract_v0_1.md` (rotation + trust-anchor-history note), this closeout, `tips/README.md` (TIP-68 row → built/ACCEPT). README index = Contractor housekeeping (not builder scope).
- **GDriveSync NOT run** (known `invalid_grant`). `docs/00_GDRIVE_FILE_INDEX.md` untouched (known-dirty, excluded). Evidence hash chain unchanged.

## Scope Boundaries (held)

No hash-chain or neutral-proof-claim change (golden byte-identical). No second signature (runtime still signs only the neutral proof via `SignProofAsync`). No JWKS, CA, TSA, qualified, webhook, payload, or document/transaction binding. `LocalDevEs256JwsEvidenceSigner` retained as the non-PKCS#11 dev fallback. No PKCS#11/X509 leak into Application/Api. No runtime verifier engine.
