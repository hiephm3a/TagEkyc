# TIP-67B Neutral Verifiable Proof — Closeout v0.2

**Status:** ACCEPT — Contractor adversarial spot-check (2 rounds) CLEAN; ready for commit by allowlist (NOT pushed)
**Baseline:** `72a7b41` (master; TIP-67A committed, opaque-challenge/profile contract FROZEN)
**Kickoff:** `tip_67b_kickoff_v0_1.md` v0.4 · **Planning brief:** `tip_67b_planning_brief_v0_1.md` v0.9
**Date:** 2026-06-23

## Summary

TIP-67B turns the eKYC result into a **self-contained, neutral, verifiable signed proof** + a dedicated verification view, WITHOUT changing the evidence hash chain. SignFlow (or any consumer) can verify the proof against an out-of-band trust anchor and then do its OWN transaction/document binding — eKYC never binds the document.

- Completion signs a dedicated `EvidenceProofSignatureRequest` claim (`proofVersion = neutral-proof-v1`): final result, single `assuranceLevel`, deterministically-sorted checks, ordered `evidenceEngines[]`, hashed `identityRef`, `challenge` (opaque echo), `signedManifestHash`, and `resultHash`.
- `GET /api/ekyc/evidence-packages/{id}/verification-view` exposes the signed proof, the attached compact JWS, the **sign-time** public JWK + `publicKeyFingerprint`, and mirrored claim fields. The existing summary route/DTO is unchanged.
- The manifest-row signature envelope persists the **sign-time** `PublicKeyJwk` + `PublicKeyFingerprint` (new migration), so verification uses the key that ACTUALLY signed the package — never the current/rotated signer.
- `docs/signflow_integration_contract_v0_1.md` carries a **self-contained** consumer-verification algorithm (SignFlow can implement from the doc alone, without the test code).

## Outcome vs Intent

| Intent (kickoff v0.4) | Outcome | Status |
| --- | --- | --- |
| Self-contained neutral proof (SUPERSET of TIP-66 claim) signed via a dedicated request | `EvidenceProofSignatureRequest` + `neutral-proof-v1` claim; TIP-66 metadata kept + tested | ✅ |
| `identityRef` ALWAYS-HASH (no raw PII) | `sha256("tip-67b-identity-ref-v1\n"+clientApplicationId+"\n"+subjectRef)`; sanitization test asserts no raw subject/clientAppId/payload/vault/`d` | ✅ |
| `resultHash` stable, preimage excludes self/algos/signature | JCS preimage excludes `resultHash`/algo/scheme/`signatureValue`; shuffled-checks/engines → identical hash (test) | ✅ |
| **Sign-time** public-key binding (not current signer) | Persisted in envelope + migration; view reads persisted key; rotate-key test: old-key verifies / new-key rejected | ✅ |
| Dedicated verification-view route; summary unchanged | New route added; `EvidencePackageSummaryDto` gained no fields | ✅ |
| Verifier reads ONLY from verified claim; mirrored fields cross-checked fail-closed | `MirrorsClaim` requires every view field == decoded claim | ✅ |
| Trust anchor = pinned `kid` + fingerprint (sha256 JCS over `{kty,crv,x,y}`); reject private `d` | Implemented + tested; JWK whitelist | ✅ |
| Golden hash chain byte-identical (proof downstream of `manifestHash`) | manifestBodyHash/packageHash/manifestHash unchanged; no proof field in manifest body | ✅ |
| eKYC stays NEUTRAL (no document/transaction binding) | No `document_hash`/transaction in claim/request; client binds `H(challenge ‖ document_hash ‖ resultHash)` | ✅ |

## Hash Chain Posture

Signing remains downstream of `manifestHash`. Golden vectors **byte-identical** (no hash-chain code changed; TIP-65 golden tests pass):

- `manifestBodyHash = sha256:e8aa856e1cc6fd31f085f3ace99ef0ddafc6f8538dd0443def7d4bf91ccc96d4`
- `packageHash      = sha256:0a3cbc9b031c2131822b676a5f40c11b478636e74704732fd558655833c122c0`
- `manifestHash     = sha256:124c36f819308aa86cb5813f63ec11d7bdbc410cbd97b2d5e7f23d6bf60dc2af`

## Validation

- `dotnet build TagEkyc.sln` — passed (0 warnings/errors).
- `dotnet test TagEkyc.sln` — **passed 176/176** (175 + the post-spot-check signature-tamper test).
- New migration: `20260623120000_Tip67BNeutralProofPublicKeyMaterial` (adds envelope public-key columns; no hash-chain impact).

## Review Ladder

- **V1 (Tier-0 automated):** build clean; 176/176; golden vectors re-asserted byte-identical. PASS.
- **V2 (Tier-1 adversarial, on CODE):** Contractor spot-check round 1 (subagent breadth + crux) = 8/8 load-bearing CLEAN (sign-time key, negative matrix, claim-only reads + mirror cross-check, identityRef no-PII, resultHash determinism, golden, provider-neutral, neutrality). One LOW coverage gap raised (no isolated `VerifyData()==false`). Round 2 (post-patch) verified the fix on CODE: `ReplaceSignatureSegment` flips one byte of `parts[2]` only, intact header/payload/fingerprint/kid/challenge → reaches the ECDSA `VerifyData==false` branch (not a pre-check short-circuit). PASS.
- **V3 (Tier-2 legal-crypto):** ES256/JWS, fingerprint-pinned trust anchor, fail-closed matrix, JWK whitelist (no private `d`), self-contained consumer contract — verified at **dev-foundation** level. Deferred: full canonical-preimage exposure on demand; production HSM/KMS/JWKS/CA/RFC-3161; legal sufficiency / non-repudiation. (P0 — see Debt.)

## Post-Spot-Check Patches (round 2)

- **Fix A** — added `Reference_verifier_rejects_signature_segment_tamper_under_matching_trust_anchor`: isolates the ECDSA verify-fails branch (same class as the TIP-66 signature-tamper lesson). Forged-key cases short-circuit at the fingerprint pin; this test keeps the pinned fingerprint/kid/JWK intact and corrupts only the signature segment → `VerifyData==false`.
- **Fix B** — expanded `signflow_integration_contract_v0_1.md` into a self-contained verifier algorithm (JWS structure + header, trust-anchor + fingerprint recipe, ordered verify steps, signed-claim fields + ordering rules, `resultHash` preimage/label/exclusions, full fail-closed matrix, client-owned binding). SignFlow can implement WITHOUT reading the test code.

## Decision / Branch Disposition

- Built on baseline `72a7b41`; **NOT committed** at build time (awaited spot-check). Now ACCEPT → to be committed by **allowlist** (Codex), **NOT pushed** (push only on Homeowner authorization).
- Allowlist EXCLUDES known-dirty: `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, all `bin/obj`.
- Decisions reaffirmed: sign-time key persisted in envelope (not a current-signer port); dedicated `EvidenceProofSignatureRequest` (no TIP-66 overload); `identityRef` always-hash; dedicated route (summary frozen); verifier trusts only the decoded claim.

## Debt

| Item | Severity | Disposition |
| --- | --- | --- |
| Decision-basis binding — `confidence`/`reasonCodes` remain UNSIGNED (proof carries MVP facts, not legally-complete rationale) | P1 | Open; needs legal lens (separate TIP) |
| Production signing — HSM/KMS/JWKS/CA/RFC-3161 timestamping/webhook signing | P0 | Open; dev ES256/local-P12 only |
| Reference verifier is test-only (`internal` to the integration test project) | INFO | Mitigated — canonical algorithm now self-contained in `signflow_integration_contract_v0_1.md`; SignFlow re-implements |
| Compatibility columns `BindingNonceHash`/`ExternalTransactionId` not renamed; TIP-67B test-file/column hygiene | P3 | Deferred (no behaviour impact; retained as storage-only per lld_01 v0.5) |
| Full canonical-preimage exposure on demand (Tier-2) | P2 | Deferred |

## GDrive Sync / Doc Posture

- Docs changed: `tip_67b_kickoff_v0_1.md` (v0.4), `tip_67b_planning_brief_v0_1.md` (v0.9), this closeout (v0.2), `docs/tips/README.md` (v1.24), `lld_01_data_model_v0_1.md` (v0.6), `lld_03`, `signflow_integration_contract_v0_1.md`.
- **GDriveSync NOT run** (known `invalid_grant` / expired OAuth — do not run). `docs/00_GDRIVE_FILE_INDEX.md` left untouched (known-dirty, excluded from allowlist). Doc hashes update on the next authorized sync.

## Scope Boundaries (held)

No document/transaction binding added to TagEkyc (clients bind their own context). No production crypto/JWKS/CA/RFC-3161/webhook. No runtime verifier endpoint. No hash-chain, 67A profile-challenge, or global-enum-converter change.
