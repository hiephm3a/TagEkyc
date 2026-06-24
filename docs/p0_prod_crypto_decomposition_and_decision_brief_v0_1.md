# P0 Production-Crypto — Decomposition & Decision Brief v0.1

**Status:** D-P0-1 DECIDED (on-prem HSM/PKCS#11) · D-P0-2 RESOLVED by legal research (2026-06-23) · Slice A DRAFTED = TIP-68 (planning v0.8 / kickoff v0.6) — re-confirm before dispatch
**Date:** 2026-06-23
**Baseline:** `4d03cf6` (TIP-67A + TIP-67B committed and **pushed to origin/master** 2026-06-24 — intentional, to give GPT independent GitHub-context review)
**Author:** Contractor
**Purpose:** Decompose the "production signing / P0" debt into discrete sub-items, surface the keystone decision(s) the Homeowner must make (like D-01 was for persistence), and recommend a sequencing — so we spec a vertical slice, not a blind mega-TIP.

## 0. The one good finding (de-risks everything)

`IEvidenceSigner` (Application/Ports/EvidenceSignerPorts.cs) is a **clean single seam**: no algorithm-specific port, and grep confirms zero `LocalDevEs256`/`X509`/`P12`/`RSA`/`ECDsa` leaks into Application or Api — all crypto concretes live in `Infrastructure/Signing/`. The envelope already carries self-describing crypto-agility metadata (`signatureFormat`/`signatureScheme`/`signatureAlgorithm`/`keyId`) + sign-time `publicKeyJwk`/`publicKeyFingerprint` (TIP-67B).

**Consequence:** a production backend = (1) a new adapter behind the SAME `IEvidenceSigner`, plus (2) an environment-gated DI selector (today `Program.cs:28-32` hardcodes the LocalDev signer, no gate). **No architecture change.** The hard parts are key-custody, rotation + consumer trust-anchor history, trusted time, and the LEGAL bar — not the code seam. (Key PUBLICATION / JWKS is its own concern, deferred to Slice A2 — see §2.)

## 1. The 7 P0 sub-items (as-built state)

| # | Sub-item | As-built today | Scaffolding | Gating type |
| --- | --- | --- | --- | --- |
| 1 | **Production signing backend** (key never in app memory) | LocalDev in-process ES256 or configured P12 only; DI hardcoded, no env-gate | `IEvidenceSigner` seam + envelope metadata | **Code** (keystone) |
| 2 | **Key publication / JWKS** | Per-package sign-time JWK in verification-view only; no `.well-known/jwks.json`, no registry | `publicKeyJwk`/`publicKeyFingerprint` fields + fingerprint recipe in integration contract | Code |
| 3 | **Key rotation** | None; sign-time key persisted per package; no version history/registry | Verifier already "uses persisted sign-time key, not current signer" (TIP-67B) | Code + policy |
| 4 | **Trust anchor / CA** | Out-of-band pinned `kid`+fingerprint (no CA, no X.509 chain) | Trust-anchor contract documented (integration contract §) | **Legal-gated** |
| 5 | **RFC-3161 trusted timestamp (TSA)** | `signedAt` bound into claim = tamper-evident within our OWN signature, NOT third-party attested | none | **Legal-gated** |
| 6 | **Webhook signing** | `WebhookSignatureStatus=PlaceholderUnverified` marker only; completion is internal projection (TIP-07 Option A); no delivery/outbox | none | Code (separate TRANSPORT surface) |
| 7 | **Non-repudiation / legal sufficiency** | dev-grade only; no certification; decision-basis (confidence/reasonCodes) unsigned (= P1, separate) | neutrality ADR + verifier contract = framework only | **Legal — not code** |

## 2. Dependency clustering → proposed slices

- **Slice A — Production signing backend + rotation (items 1+3).** Choosing the custody model (item 1) determines the `kid` scheme + the rotation mechanism (item 3). **NOTE (TIP-68, 2026-06-24): JWKS (item 2) was REMOVED from Slice A → deferred to Slice A2** — TIP-67B already persists + embeds the sign-time key per package, so verification + post-rotation historical verification work WITHOUT a JWKS; adding it now = YAGNI. Slice A is the first vertical slice; built dev-level via SoftHSM (D-P0-1). **This slice = TIP-68.**
- **Slice A2 — JWKS / public-key discovery (item 2).** Deferred; only needed for a 2nd consumer, to stop per-package key embedding, or for public discovery.
- **Slice B — Trusted timestamp (item 5).** Independent; can follow A. Whether it's P0-now depends on the legal answer (§4 → RESOLVED in §4a: deferred).
- **Slice C — Webhook signing (item 6).** A *transport* surface, not the evidence surface. Logically separate from the proof signature; can be sequenced independently (also depends on whether webhook delivery itself is in scope yet — currently it's an internal projection).
- **Legal gate — items 4 (CA) + 7 (non-repudiation).** These are NOT code-first. They depend on a jurisdiction/standards answer (§4). Sequencing them before the legal answer = wasted work.

## 3. Keystone decision: the key-custody model (D-P0-1)

Like D-01 (persistence), one choice cascades. Options:

| Option | What | Pros | Cons |
| --- | --- | --- | --- |
| **A. Provider-neutral managed-KMS port (recommended)** | Add a thin `ICryptographicKeyCustody`/KMS adapter behind `IEvidenceSigner`; concrete KMS (Azure Key Vault / AWS KMS / GCP) decided later; dev uses a local/emulated backend | Key never leaves HSM-backed KMS; ES256/P-256 supported by all majors; defers the cloud-provider lock-in; matches the persistence pattern (port now, concrete later); buildable now | Need a clean custody port; emulator for dev/CI |
| B. Direct single-cloud KMS | Wire one concrete KMS (e.g. Azure Key Vault) now | Fastest to a real prod key | Couples to one cloud before infra decided; harder to test offline |
| C. On-prem HSM (PKCS#11) | SoftHSM dev / HSM prod | Full custody control; on-prem fit | Heaviest ops; PKCS#11 interop pain; likely premature |
| D. CA-issued qualified cert + HSM key | eIDAS/NĐ-130 qualified-grade | Highest legal weight | Slow, costly, legal-gated; only justified if §4 says required |

**Contractor recommended Option A** (neutral port). **Homeowner DECIDED Option C — on-prem HSM (PKCS#11)** (2026-06-23). Implication: Slice A targets a PKCS#11 signing adapter behind `IEvidenceSigner`; **dev/CI uses SoftHSM** (same PKCS#11 API, free, offline) so the build is not blocked; prod uses a real HSM. Ops weight (HSM operation/backup, PKCS#11 interop) is higher than KMS — accepted; the seam still allows swapping to a KMS later if ops proves heavy.

## 4. The legal gate (D-P0-2) — genuinely the Homeowner's call

The neutrality ADR says the eKYC proof is **NOT the document signature** — it attests "identity verified at assurance Y, time T, challenge echoed." SignFlow does the qualified document signing. So the legal bar for the *eKYC proof* signature may be LOWER than for a qualified e-signature.

**Question:** For the eKYC PROOF to be relied on (audit/pilot/production), is an **HSM-held ES256 key + out-of-band pinned-fingerprint trust anchor** sufficient — deferring CA-issued qualified certs (item 4) and a qualified RFC-3161 TSA (item 5) until a regulator/standard explicitly requires them? Or must the eKYC proof itself carry qualified-cert + qualified-timestamp from the start?

### 4a. RESOLVED (legal research, 2026-06-23) — TSA / qualified-sig are NOT required for evidentiary value

**Framework correction:** the governing texts are **Luật Giao dịch điện tử 20/2023/QH15** (effective 2024-07-01, replaced Luật 51/2005) and **Nghị định 23/2025/NĐ-CP** (effective 2025-04-10, **replaced NĐ 130/2018** + NĐ 48/2024 per Điều 51). NĐ 130/2018 is dead — do not cite it.

**Finding (verified 3-0, cross-checked chinhphu.vn / NEAC / VCCI):** Vietnamese law does NOT require a trusted timestamp (RFC-3161 / dịch vụ cấp dấu thời gian) or a qualified digital signature **on the eKYC verification record itself** for it to have evidentiary value. The evidentiary value of a data message (Điều 11 Luật GDĐT 2023) turns on: (i) reliability of how it was created/sent/received/stored; (ii) integrity assurance; (iii) identifiability of the originator. Minimum "original/evidence" standard (Điều 10): **integrity from first creation + accessible/usable in complete form** — no digital signature required.

- **Timestamp = an OPTIONAL "trust service"** (one of three: Điều 28, 31), a service you MAY use, not a mandatory attachment. Mandatory only in the narrow case Điều 19.1.c (when law requires showing time bound to a *chứng thư điện tử* / e-certificate) and special acts (e.g. e-notarization).
- Banking eKYC rules (TT 17/2024-NHNN account-opening; TT 06/2023-NHNN = an amendment to TT 39/2016 on *lending*) impose **safe/complete/integral STORAGE** duties on credit institutions — NOT timestamp/qualified-sig on the record. They bind TCTDs, not automatically a neutral third-party eKYC service; the higher-level Luật GDĐT principle governs either way.
- NĐ 69/2024 (e-ID, replaced NĐ 59/2022) imposes no RFC-3161 / qualified-cert requirement on the verification record.

**What TagEkyc already satisfies:** integrity (hash chain + append-only DB + JWS), originator identification (kid + ES256 signature + audit), complete-form retrieval (verification-view). HSM custody (D-P0-1) strengthens exactly the integrity/origin-authenticity the law cares about.

**Decision:** TSA (item 5), CA-issued cert (item 4), and qualified non-repudiation (item 7) are **NOT P0 for evidentiary admissibility → deferred-with-doc behind a legal milestone.** They are *probative-weight enhancements*, relevant only if (a) a specific sector regulator demands them, or (b) we proactively want maximum probative weight. They slot in later behind the same `IEvidenceSigner` envelope/port (cheap, additive). Caveat: "admissible" (low bar — integrity + traceability, which we meet) ≠ "maximally hard to challenge" (where a TSA/qualified-cert helps). If a specific TagEkyc client is a bank/regulated entity with its own evidentiary demands, re-check that client's sector rule.

So items 4/5/7 are **deferred**; **Slice A (HSM signing + rotation; JWKS deferred to A2) is the P0-now scope.**

## 5. Recommended path

1. ✅ **D-P0-1 DECIDED** — on-prem HSM (PKCS#11); dev/CI = SoftHSM.
2. ✅ **D-P0-2 RESOLVED** (§4a) — TSA/CA/qualified NOT required for admissibility → items 4/5/7 deferred behind a legal milestone.
3. ✅ **Slice A drafted = TIP-68** (`docs/tips/tip_68_prod_hsm_signing/`, brief v0.8 / kickoff v0.6) — PKCS#11 HSM signing adapter behind `IEvidenceSigner` + key rotation (+ consumer trust-anchor history) + **eager** env-gated fail-closed DI (**JWKS deferred to A2**); DoD = working dev-level flow (SoftHSM) + tests; golden manifestHash/proof **byte-identical** (signing downstream/additive). Deep-code-review round-3 P1+P2 patched; re-confirm before dispatch.
4. Review loop (GPT/Codex) → build → adversarial spot-check on CODE → closeout → commit.
5. Slices B (RFC-3161 TSA) and C (webhook signing) deferred; revisit only on a sector-regulator demand or a proactive probative-weight decision.

## 6. What Slice A would NOT do (scope floor + ceiling)

- NOT change the hash chain or the neutral-proof claim shape (golden byte-identical).
- NOT bind documents/transactions (neutrality held).
- NOT implement CA chains, qualified certs, qualified TSA, or webhook delivery (separate slices / legal-gated).
- NOT remove the LocalDev adapter (it stays as the dev/CI backend behind the gate).

## 7. Open items / debt this brief does not resolve

- Concrete KMS/cloud provider choice (follow-on infra decision after D-P0-1).
- Sector/client-specific legal re-checks may still need external legal/standards counsel; D-P0-2 itself is the accepted decision basis for this slice.
- Decision-basis binding (confidence/reasonCodes) remains the separate **P1** legal-lens TIP.
