# Verification Method, Engine & Liveness-Tier Strategy — Decision Brief v0.1

**File:** `docs/verification_engine_liveness_strategy_decision_brief_v0_1.md`
**Version:** 1.0
**Status:** VE-01 LOCKED (build portable own engine + integrate readers; two-layer, 2026-06-24). BHYT/BHXH dropped. Multiple GPT/Codex review rounds applied + **HN212 + real-CCCD live-probe runtime confirmation (sanitized, §1b)**. **HN212 chip-read / PACE / SOD-internal / chip-auth / face-match callability CONFIRMED; CSCA source (VE-06) and liveness/anti-spoof live-probe remain separate follow-ups.** READY_TO_COMMIT after sanitized runtime patch.
**Date:** 2026-06-24
**Baseline:** Product Brief v0.1.2; ADR `ekyc_neutrality_decision_v0_1.md`; legal research notes (CC, 2026-06-24)
**Purpose:** Codify the medical-first verification model (how a person is verified) + map each method to engines, assurance levels, and a cost-aware liveness tier — so the engine roadmap is decided once, not re-derived per slice. This is the "verification brain" keystone (analogous to D-01 for persistence).

## 0. Why this brief exists

TagEkyc today has the **evidence/trust/proof spine** (TIP-65 JCS, TIP-66/67B dev signing + neutral proof, TIP-68 HSM custody) but **NO verification engines** — face-match, liveness, OCR, finger-match are conceptual (lld_04 forward); evidence currently arrives via the `TrustedAdapter /evidence-results` API from a stub. So TagEkyc is an "eKYC evidence & trust platform," not yet an identity-verification engine. This brief decides how the verification function gets built/integrated. Target domain: **healthcare first** (not banking), cost-sensitive (already paying CA + C06 per-use fees).

## 1. Verification method matrix (Homeowner-defined, extensible)

```
Person to verify
├─ Method 1: CCCD chip (C06-certified reader + SDK — ALREADY HAVE)
│    → read chip data + GOVERNMENT-SIGNED reference photo / fingerprint (passive-auth / SOD)
│    ├─ 1a. Face matching (live selfie vs CHIP photo) + liveness/PAD (passive OSS baseline; active-challenge when the capture surface supports it)
│    └─ 1b. Finger matching (live fingerprint vs CHIP fingerprint)
└─ Method 2: VNeID (C06 integration) → consume the national eID assertion
(open to more methods later)
```

## 1a. Two-layer architecture (LOCKED, 2026-06-24) — do NOT couple to any one reader

Lesson from the Hanel HN212 SDK: it BUNDLES reader + face-match + liveness + OCR into one Windows-desktop box. We must SPLIT these so the product does not depend on any single reader/vendor (customers may use a different reader; future Mobile/Tablet/Ubuntu clients can't run a Windows desktop SDK).

```
LAYER 1 — READER / CAPTURE ADAPTER (pluggable)
  yields: passive-auth'd chip data + DG2 reference face + a live face image (+ fingerprint)
  ├─ HN212 (Windows)        — chip + cam + built-in face/liveness   ← PRIORITY deployment
  ├─ chip-only reader + cam — reads chip only → external webcam for the live face
  ├─ Mobile/Tablet (NFC+cam)— later phase
  └─ VNeID                   — consume national eID assertion

LAYER 2 — VERIFICATION ENGINE (OURS — build + own, portable)
  given (chip ref-face, live face) → face-match score + liveness verdict
  → ONNX: liveness (Silent-Face/MiniFASNet) + face-match (ArcFace/InsightFace)
  → OCR (PaddleOCR) = OPTIONAL capture-assist, NOT core engine — only for chip-less/damaged-card fallback or MRZ/CAN derive; when the chip is present, DG13 is the authoritative data (no OCR needed)
  → ONNX Runtime is cross-platform (Win/Linux/Android/iOS) → runs where Hanel's SDK cannot
```

**Decisions locked:**
- **The verification engine is OURS, portable (ONNX), and the universal default.** A reader's built-in face/liveness (e.g. HN212's anti-spoof) is an OPTIONAL local input — recorded + tier-assessed in the proof, but NEVER the foundation. **API-dump finding (2026-06-25): HN212's anti-spoof + face-match are SOFTWARE (ViewFaceCore/SeetaFace on RGB frames), NOT an HN212-proven hardware accelerator → treat HN212 liveness as software passive RGB (comparable TIER to our OSS, not stronger) unless a vendor cert/provenance packet proves otherwise.** Do NOT assume any OTHER reader provides verified hardware-liveness without a separate device-specific evidence packet. (Homeowner-reported market state — unverified, not an API-dump conclusion: no VN CCCD reader with verified hardware-liveness; one vendor self-claims.) Any chip-only reader + a webcam is fully served by our engine.
- **Engine placement (tiered):** server-side ONNX is the DEFAULT (any client on any OS just captures + uploads; one engine, consistent, easy to tune — maximally portable). HN212 present → **consume its software anti-spoof verdict as one input + tier-assess** (it is ViewFaceCore-grade, NOT a stronger hardware tier — §1a findings). **Capture-binding is required from the FIRST slice for ANY non-HN212-hardware-liveness path** (server-side ONNX on chip-only+webcam too, not just remote): the live-face capture MUST be bound to the session challenge/timestamp — server-side liveness trusts the uploaded frame, so capture-binding is the minimum mitigation against injection/replay. Remote/mobile ADDITIONALLY add client-side active-challenge later.
- **Server-side ONNX is NOT a trust boundary by itself (LOCKED).** Its face-match/liveness verdict is valid ONLY when the live-face / NFC / capture artifact was produced by a TagEkyc-controlled capture surface — hosted flow, SDK, local agent, HN212 agent, or an explicitly authorized CaptureAgent — WITH session-challenge binding. An ordinary BusinessConsumer **direct upload MUST NOT be accepted as production-grade liveness evidence** (running the engine server-side on a client-supplied image = trusting the client's capture, which the `integration_channel_strategy` role boundaries forbid). The engine's location (server) gives portability, not trust; trust comes from capture provenance + challenge binding.
- **Hanel HN212 = the PRIORITY capture adapter (Layer 1), not the platform.** Its `CompareFace`/anti-spoof (= ViewFaceCore/SeetaFace — **OSS-grade passive RGB software**, a comparable tier to our engine but a DIFFERENT implementation; ours is Silent-Face/MiniFASNet + ArcFace/InsightFace) + `VerifySOD`/`DSCert` (SOD-INTERNAL verify only — the CSCA chain is a SEPARATE online step, §1a) are ingested as evidence; our engine guarantees coverage when the reader lacks them.
- **Hanel SDK analysis — API-dump RESOLVED (2026-06-25, via a reflection probe; live-probe now confirmatory only):**
  - **Anti-spoof = SOFTWARE RGB passive** (`Hn212FaceProcessing.AddFrame(Mat)`, `DetectFaceInfo.lastSpoofingStatus:AntiSpoofingStatus`) — no hardware NIR/depth API, no certification reference → ViewFaceCore-grade (≈ our OSS), and Mat-frame-based so not strictly HN212-locked.
  - **CSCA trust-anchor is NOT local.** The local SDK only does SOD-INTERNAL verify (`SodFile.VerifySignedData()` + `VerifyDataGroupHashes()` against the embedded `DocumentSignerCertificate`). DS→VN-CSCA chain validation = an **ONLINE LICENSED Hanel service** (`VerifyCscaRequest{DSCert,ClientId,LicenseId,Token}` → `VerifyCscaResponse{Result,Signature}`). → **DECISION VE-06: source the CSCA trust anchor — use Hanel's online VerifyCsca as one adapter, OR build our own CSCA-master-list validation.** "Chip = trust anchor" is NOT free/offline.
  - **Face-match standalone = YES** (`VnHn212Reader.CompareFace(byte[],byte[])→int`; `FaceMatchingUtil.ViewFaceRecognizer(stream,stream,threshold)→float` = ViewFaceCore; `EKycEngine.FaceVerify`).
  - **PACE access auto-derivable** (`GetAcessCodeFromMRZLine`, `DetectCardAcessCode`, `DetectCardQRCode`, `OcrImageInfos.AccessCode`) OR manual (`StartReadCard(accessCode)`); full PACE/BAC crypto present.
  - **License = online-service-gated** (`LicenseId`/`Token`/`ClientId` for CSCA-verify + the Verify API); local read/capture/compare may be offline.
  - **Local SDK outputs are UNSIGNED + UNBOUND** — `CompareFace→int`, `FaceVerifyResult`, `CardFullData.VerifySOD:CardStepStatus` are plain values; session-challenge/timestamp/signature exist ONLY in Hanel's online Verify API (`VerifyRequest{RequestId,Timestamp,Salt,Token}`). → **CONFIRMS the LOCKED rule above: TagEkyc MUST do its own capture-binding + provenance signing around the SDK outputs (the SDK does not).**
  - Windows-desktop (PC/SC + DirectShow + USB) = a client-side CaptureAgent/local-agent submitting evidence to `/evidence-results`.
  - **Live-probe (Step 2) — chip path DONE (see §1b);** anti-spoof/liveness camera path still pending.

## 1b. Runtime live-probe confirmation — HN212 + real CCCD (sanitized, 2026-06-25)

A reflection live-probe drove a real HN212 (serial redacted: `HANEL-READER-REDACTED`) against a real CCCD. **Sanitized observations only** (no PII; see the privacy redline below):

- **Manual access-code read bypasses OCR/camera/message-pump** — supplying the access code (last 6 digits of the citizen-ID number) made the SDK read the chip directly, skipping the OCR-scan-to-derive-key. Confirms the **headless / server-side / chip-only-reader path is viable** (observed in one live probe).
- The chip pipeline **succeeded once** end-to-end: `CONNECT_CARD → PACE → READ_DGS → VERIFY_SOD → AACA_AUTHEN → FINISH`, all on the **local SDK** (no online call).
- **`VerifySOD` SUCCESS = SOD-INTERNAL validity only** (signature valid under the embedded DS cert + DG hashes match). It does **NOT** prove the DS cert chains to the VN CSCA — that is **VE-06**, still open. Do NOT record `CSCA_CHAIN_VERIFIED` for this probe.
- **AA/CA (chip-authentication) SUCCESS** → model **chip-genuine / chip-possession as a DISTINCT evidence check**, separate from SOD-internal verify and separate from CSCA validation. ⚠️ The probe's AA/CA used the SDK's internal challenge → it confirms **chip capability, NOT session-bound anti-replay** (`CHIP_AUTH_WEAK_CONTEXT`). The API `StartAA(byte[] challenge)` lets us feed a **server/session-generated challenge** → the chip signs OUR challenge → THAT is genuine session-bound anti-replay. (Exact AA/CA evidence fields — server challenge / sessionId / nonce / chip response / DG15 / algorithm / result — pinned in the engine-slice TIP, not here.)
- **Face-match callable standalone**: HN212 `CompareFace(chipDG2, selfie)` returned a score in one sample (`score=79`, threshold `60`) = ViewFaceCore standalone path viable. This **confirms callability only — it does NOT benchmark model quality and does NOT certify PAD/liveness.**
- **DSCert material was available** → **VE-06 is now actionable** (we have a real DS cert to validate against a CSCA list / provider API).
- **Local SDK outputs are unsigned + unbound** (plain values, no session/timestamp/signature) → reconfirms the LOCKED rule: TagEkyc must perform its own **session-challenge binding + capture-provenance recording + proof signing**.

**Legal-value wording (do NOT overclaim):** these are **cryptographic evidence candidates**, posterior-verification-ready *if* a DS/CSCA trust path or a provider API is available. Legal value requires policy/legal review + provider confirmation — NOT asserted here.

### Privacy redline (LOCKED)
Raw HN212/CCCD live logs contain **PII + biometric data** — name, date of birth, address, MRZ, citizen-ID number, face image/base64, raw DS-cert/certificate material. They **MUST NOT** be committed, attached to review packets, or synced to Drive/GitHub **without redaction**. Only **sanitized summaries, hashes, VaultRefs, and non-sensitive status fields** may enter docs or evidence packets. (The live-probe log from 2026-06-25 is treated as sensitive: not committed; secure/delete.)

## 2. Core principle — the chip is the TRUST ANCHOR (this reframes the threat model)

The reference biometric (photo/fingerprint) comes from the **CCCD chip, government-signed (passive-auth)** — NOT a user-uploaded document image. Consequences:
- **Document/OCR-forgery threat is eliminated** — we match against signed government data, not a photographed card.
- **The only residual threat is PRESENTATION**: is the live person actually the chip-holder? → that is exactly what **liveness (1a) or finger-match (1b)** defends. Liveness is not a checkbox; it bites the one remaining hole after the chip anchors identity.
- **BHYT/insurance is a DOWNSTREAM consumer, NOT a verification target (Homeowner, 2026-06-24):** BHXH is already linked to CCCD, so the right-person check is achieved simply by verifying the live person == the CCCD chip-holder (chip + face [+ liveness]); the BHYT entitlement is then looked up by the verified CCCD number. eKYC verifies identity on CCCD and emits the proof; it does NOT implement a separate BHYT anti-fraud mode, and there is no BHXH legal residual to prove.

## 3. Method → assurance → proof recording

Each method emits an evidence result carrying method + checks + assurance. **No proof TOP-LEVEL schema change:** the existing fields are the engine refs (`evidenceEngines[]` = `EvidenceResultType`/`EvidenceResultId`/`EngineName`/`EngineVersion`/`CheckType`), `requiredChecks`/`completedChecks`, and `assuranceLevel`. Method class + liveness type/grade are NOT new top-level proof fields — they are recorded in the underlying **EvidenceResult payload/metadata** and bound into the signed proof indirectly via `evidenceEngines[]` + `signedManifestHash`/the manifest hash chain. If a consumer later needs method/liveness-grade as FIRST-CLASS verification-view fields, that is a SEPARATE proof/API schema TIP (so the honest claim is "no top-level change + grade bound via the hash chain", not "no change at all").

| Method | Engine(s) | Indicative assurance | Notes |
| --- | --- | --- | --- |
| VNeID assertion (M2) | C06/VNeID adapter (consume) | Highest; lowest eKYC burden | assurance = whatever VNeID returns; gated by C06 grant |
| Chip + finger-match (1b) | finger matcher + fingerprint reader HW + chip-fingerprint access (C06) | High | finger PAD more mature; needs hardware; confirm chip-finger read is permitted by the C06 SDK |
| Chip + face + liveness (1a) | face-match (1:1) + liveness/PAD — **passive OSS baseline** (first-slice default); active-challenge = upgrade tier when the capture surface supports it | Moderate-high | **the market differentiator** (most VN medical stops short of liveness) |
| Chip + face only | face-match (1:1) | Moderate | the common current-market baseline |

**NOTE — this is the TARGET/indicative mapping, not the as-built runtime.** As-built maps any passed session with `DocumentNfc + FaceMatch + Liveness` → `High`, else `Medium` (`VerificationCompletionApplicationService.cs:568`). Aligning the runtime assurance mapping to this matrix (distinct VNeID / finger / face+liveness tiers) is a LATER implementation change, not assumed by this brief.

**HONEST assurance recording (mandatory):** the proof MUST NOT imply a higher liveness/PAD grade than actually performed. Method + liveness type/grade (e.g. `oss-passive+active-challenge, NOT certified PAD`; `HN212-hardware` vs `our-engine`) are recorded in the EvidenceResult metadata and bound via `evidenceEngines[]` + the hash chain (per §3 above) — not as new top-level fields. `verified: passed` must never imply certified-grade. Ties to the decision-basis-binding P1 debt + assurance-level mapping.

## 4. Liveness tier + OSS pick (cost-aware)

Legal finding (research, 2026-06-24, verified 25/25): **VN healthcare does NOT mandate certified liveness/PAD.** The certified-liveness expectation is a **BANKING-sector** requirement (NHNN online-banking / payment-account rules), NOT healthcare. ⚠️ Do NOT anchor to a specific instrument as "current banking law" — QĐ 2345/QĐ-NHNN (2023) was reportedly superseded across 2024-2025 (e.g. by QĐ 2872 / migrated into NHNN Thông tư); the load-bearing point is only "banking-sector, not healthcare" (verify the exact current banking instrument separately if ever needed — it is out of scope here). NĐ 69/2024 (official) tiers identity assurance by risk (biometric only required at levels 3-4); the service org picks the level per activity. So **liveness is a THREAT-MODEL choice for medical, not a legal blocker.**

OSS reality: **no OSS liveness is independently certified** (iBeta/NIST/ISO 30107-3); all OSS stops only basic 2D (print, screen replay) — not deepfake/injection/3D.

**Tier strategy (the cost answer):**
- **Low-risk bulk (onboarding, telemedicine):** OSS liveness — **not legally blocked** for the low-risk medical baseline (per current research, subject to service risk acceptance) + **sufficient ONLY for the defined low-risk threat model** (basic-2D deterrence, not deepfake/injection) + zero per-check license cost.
- **High-risk (controlled drugs, legal-weight e-consent):** escalate to a stronger / certified commercial liveness ONLY here, so per-check license cost is NOT stacked on the low-risk bulk. (BHYT is NOT a high-risk tier — it's a downstream CCCD-keyed lookup, §2.)
- The provider-neutral engine seam lets us **start OSS and escalate later — no re-architecture.** This is the structural answer to "don't pile CA + C06 + liveness fees on every patient."

**OSS pick:** **Silent-Face-Anti-Spoofing (MiniVision, MiniFASNet-V2)** — Apache-2.0 (code + weights, commercial-OK), passive/silent, ONNX → .NET 8 via `Microsoft.ML.OnnxRuntime` (the ONNX export is typically a convert/community artifact → the build TIP MUST pin the exact artifact/commit + conversion recipe; not pinned here). Optional second layer: **MediaPipe** active challenge-response (blink/turn, free). **AVOID `bob.pad.face`** (GPLv3 copyleft → forces source release on a closed product).

## 5. Legal basis + residuals to verify (do NOT over-rely)

Verified (research): no medical mandate for certified liveness; NĐ 69/2024 tiered; TT 26/2025/TT-BYT e-prescription requires only recording the ID number "(if available)" + Đề án 06 data-linkage. **Residuals (NOT yet read clause-by-clause — verify before betting):**
- ~~BHXH rule for BHYT anti-card-sharing~~ — **DROPPED (Homeowner): BHYT is a downstream CCCD-keyed lookup, not an eKYC verification target → no BHXH residual.**
- **Luật KCB 2023 (15/2023/QH15) + NĐ 96/2023** telemedicine identity clauses.
- **Controlled drugs** (gây nghiện/hướng thần) — may demand higher identity assurance than outpatient TT 26/2025.
- **Chip-fingerprint access** — confirm the C06 SDK permits reading the chip fingerprint template (some access is restricted even at C06 level).

## 6. Engine roadmap priority (two-layer, §1a)

1. **Define the two ports** (LOCKED): Layer-1 capture-adapter port (yields passive-auth'd chip data + ref-face + live-face) ⟂ Layer-2 verify-engine port (ref-face + live-face → match score + liveness verdict). Both feed the existing `/evidence-results` → neutral-proof pipeline.
2. **Build OUR portable ONNX engine (Layer 2)** — core = face-match (ArcFace/InsightFace) + liveness (Silent-Face/MiniFASNet), server-side default; the universal coverage for any reader/OS. (OCR/PaddleOCR = OPTIONAL capture-assist module, not core — chip-less/damaged-card fallback or MRZ/CAN, per §1a.)
3. **HN212 capture adapter (Layer 1, PRIORITY deploy)** — Windows agent reads chip + captures face; ingests HN212's built-in match/liveness/SOD as evidence; falls back to our engine where absent.
4. Chip-only-reader + webcam adapter — our engine does face+liveness.
5. Finger-match (1b) — reader HW + chip-finger access; extend.
6. VNeID adapter (M2) + Mobile/Tablet/Ubuntu capture clients — later (our ONNX engine already covers them server-side).

## 7. Open decisions (Homeowner)

| ID | Decision | Recommended lean |
| --- | --- | --- |
| VE-01 | Build vs integrate per engine | **LOCKED: BUILD our portable ONNX engine (Layer 2, core = face-match + liveness; OCR = optional capture-assist, not core) as the universal default; INTEGRATE readers (HN212/chip-only/VNeID) as Layer-1 adapters.** Reader built-in face/liveness = optional accelerator, not foundation. High-tier/certified liveness = escalation later |
| VE-02 | First engine slice | **First SUBSTANTIAL vertical slice (NOT a 1-class minimal):** define the two ports + ONE Layer-2 ONNX path (face-match + liveness, deterministic fixture images) + the HN212 priority Layer-1 adapter + capture-binding for non-HN212 paths. Ingest BOTH verdicts into the proof AFTER the EvidenceResult method/grade encoding is settled (§3). Real e2e (real images, not stub) |
| VE-03 | Capture surface first | Hosted-web (lowest friction) — per `integration_channel_strategy_v0_1.md` IC-01 |
| VE-04 | VNeID integration scope + assurance mapping | Pending C06 grant terms |
| VE-05 | Verify residuals: §1a Hanel items (API-dump RESOLVED; chip live-probe DONE §1b) + telemedicine (Luật KCB 2023/NĐ 96) + controlled-drugs identity bar + **liveness/anti-spoof live-probe (pending)** | Before the relevant slice; BHXH/BHYT DROPPED |
| VE-06 | **CSCA trust-anchor sourcing**: live SOD-internal verify SUCCEEDED but that is NOT DS→CSCA chain. DSCert material now available (§1b) → concrete options: (a) Hanel online `VerifyCsca` adapter, (b) build own CSCA-master-list validation, (c) hybrid | Lean: support both (online adapter now; own-list later for offline/independence). Do NOT mark CSCA verified from SOD-internal success |
| VE-07 | **Chip-evidence-log sufficiency contract** — `PROPOSED` (lock before accepting real-chip evidence): TagEkyc must NOT treat "NFC passed" as sufficient. The proof/evidence must DECOMPOSE chip evidence into distinct sub-states (NFC read / PACE / SOD-internal valid / DG-hashes-match-SOD / CSCA verified-or-not / chip-auth valid / chip-auth-weak-context-if-challenge-not-session-bound / face-match / liveness-performed / liveness-certified-or-not / HN212-software-verdict-ingested / capture-bound-to-session). The capture-bound/authorized-agent/direct-upload-untrusted states are already governed by the LOCKED §1a server-not-a-trust-boundary rule. **The exact flag set is INDICATIVE — finalized in the engine-slice TIP / proof schema, not locked here** (avoid premature schema-locking). | Principle LOCKED; flag set indicative |

## 8. Non-goals (this brief does not)

- Choose a specific face-match model or commercial liveness vendor (only the OSS baseline pick).
- Build any engine or capture surface (this is the decision layer).
- Claim production/legal readiness or certification.
- Change the proof TOP-LEVEL schema (method/liveness-grade are bound via EvidenceResult metadata + the hash chain per §3; first-class verification-view exposure of method/grade = a separate TIP).
- Align the runtime assurance-level mapping to the §3 target matrix (that is a later implementation change).
- Resolve the SignFlow consumer-integration track (separate: Consumer Guide + thin verify-library + adapter).

## 9. Recommended next step

VE-01 is LOCKED (build portable engine + integrate readers; two-layer). Remaining inputs: the §1a Hanel HN212 verify-items (when a physical device is available for hands-on check) + the telemedicine/controlled-drugs legal points (BHXH/BHYT dropped). Then open a TIP for the **first SUBSTANTIAL vertical slice** (phased — not a 1-class minimal):
1. Define the Layer-1 capture-adapter port ⟂ Layer-2 verify-engine port + the EvidenceResult method/grade encoding (so honest assurance recording per §3 is real before verdicts are ingested).
2. Build ONE Layer-2 server-side ONNX path (face-match + liveness) against deterministic fixture images + the HN212 priority Layer-1 adapter; **capture-binding to the session challenge for the non-HN212 path.**
3. THEN ingest BOTH verdicts (HN212-builtin AND our-engine) into the proof pipeline, method/grade recorded honestly via EvidenceResult metadata + hash chain.
4. Real e2e — actual ONNX face-match + liveness on real images (not a stub), SoftHSM-style forcing function.

This deploys HN212 immediately while staying reader/OS-agnostic (chip-only-reader, Mobile/Tablet/Ubuntu later need no re-architecture).

## Changelog

### v1.0 — HN212 + real-CCCD live-probe runtime confirmation, sanitized (2026-06-25)
- Added **§1b** "Runtime live-probe confirmation (sanitized)" — chip pipeline CONNECT_CARD→PACE→READ_DGS→SOD-internal→AA/CA→FINISH succeeded once on the LOCAL SDK; manual access-code read bypasses OCR/camera (headless path viable); CompareFace standalone callable (`score=79`/thr 60, callability only — NOT a quality/PAD claim); DSCert available; local outputs unsigned/unbound. Careful wording (observed-once / callability / not-certified / not-CSCA / not-legal-grade).
- Added a **Privacy redline (LOCKED)**: raw HN212/CCCD logs hold PII+biometrics → never commit/sync without redaction; sanitized summaries/hashes/VaultRefs/status only. The 2026-06-25 probe log is sensitive (not committed).
- **VE-06** strengthened: SOD-internal SUCCESS ≠ CSCA chain; DSCert now available → concrete options (online VerifyCsca / build-own CSCA list / hybrid); do NOT mark CSCA verified.
- Added **VE-07** (`PROPOSED`) — chip-evidence-log sufficiency contract: never "NFC passed" alone; decompose into distinct sub-states; flag set INDICATIVE (final set in the engine-slice TIP/proof schema, not locked here); capture-binding states already covered by the §1a LOCKED rule.
- Noted the `StartAA(challenge)` capability → feed a server/session challenge for genuine session-bound chip-auth anti-replay (probe's AA/CA used the SDK's internal challenge = chip-capability only, `CHIP_AUTH_WEAK_CONTEXT`).
- Reverse-review pushback vs the patch spec: version corrected to **v1.0** (brief was already v0.9, not v0.8); VE-07 kept PROPOSED + flags indicative (no premature schema-lock); chip-auth field list deferred to the TIP; reader serial redacted.
- Status: chip-read/PACE/SOD-internal/chip-auth/face-match callability CONFIRMED; CSCA (VE-06) + liveness/anti-spoof live-probe remain separate follow-ups.

### v0.9 — review: scope the API-dump overclaims (2026-06-25)
- §1a line 49: HN212 face/anti-spoof is "OSS-grade passive RGB software, comparable tier but DIFFERENT implementation" (not "the same OSS our engine uses" — ours is Silent-Face/ArcFace, HN212 is ViewFaceCore).
- §1a line 46 + changelog: the "no VN reader has hardware-liveness" claim is scoped to HN212 (API-dump proves only HN212); market-wide absence is Homeowner-reported + unverified; don't assume other readers without a device-specific evidence packet.
- VE-06 wording: "SOD-internal verification is not root-trusted without DS→CSCA validation".

### v0.8 — Hanel SDK API-dump findings (2026-06-25)
- Ran a reflection probe over the Hanel SDK assemblies (`hn212_probe`). Corrected §1a:
  - **Anti-spoof is SOFTWARE (ViewFaceCore RGB-frame), NOT an HN212-proven hardware accelerator** → comparable tier to our OSS engine (different implementation), not stronger; the "HN212 hardware anti-spoof accelerator" framing was wrong. (Market-wide hardware-liveness absence = Homeowner-reported + unverified, NOT an API-dump conclusion; don't assume other readers without a device-specific packet.)
  - **CSCA trust-anchor is NOT in the local SDK** — local does SOD-internal verify only; DS→CSCA chain = Hanel's ONLINE LICENSED `VerifyCsca` service. Added **VE-06** (source CSCA: online adapter vs build-own).
  - Face-match standalone = yes (ViewFaceCore); PACE access auto-derivable; license = online-service-gated.
  - **Local SDK outputs are unsigned + unbound** → confirms the LOCKED rule that TagEkyc must do its own capture-binding + provenance signing.
- Live-probe (Step 2) downgraded to confirmatory.

### v0.7 — Homeowner: server-side engine is not a trust boundary (2026-06-25)
- §1a placement: added LOCKED rule — server-side ONNX verdict is valid ONLY for artifacts from a TagEkyc-controlled capture surface (hosted/SDK/agent/HN212/authorized CaptureAgent) with session-challenge binding; ordinary BusinessConsumer direct upload MUST NOT count as production-grade liveness evidence. Engine location = portability, not trust; trust = capture provenance + challenge binding.

### v0.6 — re-confirm: passive-vs-active liveness honesty + polish (2026-06-25)
- Method 1a (§1 matrix + §3 table): "active-challenge liveness" → **liveness/PAD = passive OSS baseline (first-slice default); active-challenge = upgrade tier when the capture surface supports it** (the core engine Silent-Face/MiniFASNet is PASSIVE — no overclaim of active/anti-injection by default).
- §1a placement: HN212 hardware anti-spoof "trust" → "consume + tier-assess; rely only after §1a verify-items pass".
- §4 OSS pick: dropped the over-specific "opset-11"; the build TIP pins the exact ONNX artifact/commit + conversion recipe.

### v0.5 — re-confirm: OCR fully out of core engine (2026-06-25)
- §6 (roadmap step 2) + §7 (VE-01) corrected: portable engine core = face-match + liveness; OCR = optional capture-assist (not core/default) — closes the last §1a-vs-§6/§7 contradiction flagged at re-confirm.

### v0.4 — GPT/Codex review round-1 patches (2026-06-24)
- P1 proof-honesty (§3): dropped the "no proof-schema change" overclaim — method/liveness-grade live in EvidenceResult metadata + bound via `evidenceEngines[]`+hash chain; top-level proof unchanged; first-class exposure = separate TIP. (Verified against `BusinessConsumerContracts.cs`/`EvidenceSignerPorts.cs` engine-ref fields.)
- P1 stale-law (§4/§5): de-anchored QĐ 2345/NHNN (superseded 2024-2025) — "banking-sector, not healthcare" is the only load-bearing point.
- P1 OSS-sufficiency (§4): "legal + sufficient" → "not legally blocked (subject to risk acceptance) + sufficient ONLY for the low-risk threat model".
- P1 slice (§7/§9): relabeled the first slice "first SUBSTANTIAL vertical slice (not minimal)" + phased it (ports + grade-encoding first; both-verdicts-into-proof after).
- P2 OCR (§1a): OCR moved OUT of the core Layer-2 engine → optional capture-assist (chip DG13 is authoritative when present).
- P2 capture-binding (§1a): required from the FIRST slice for ALL non-HN212-hardware-liveness paths (server-side/chip-only+webcam), not "remote later".
- P2 HN212 verify-items (§1a): added session-challenge/timestamp binding + output provenance (hardware vs software-wrapper verdict).
- P2 assurance-mapping (§3/§8): noted the matrix is target/indicative; as-built runtime (`...CompletionApplicationService.cs:568` DocumentNfc+FaceMatch+Liveness→High else Medium) is a later change.

### v0.3 — BHYT/BHXH dropped; HN212 verify deferred to device (2026-06-24)
- **BHYT/BHXH dropped as a verification target** (Homeowner): BHXH is already linked to CCCD → BHYT is a downstream lookup keyed by the verified CCCD number, not a separate eKYC mode; removed the BHXH legal residual + the "BHYT high-risk tier"/"commercial hook" framing (§2, §4, §5, §7, §9).
- HN212 verify-items deferred to a hands-on check when a physical device is available (§1a, VE-05).
- Pending GPT/Codex review before commit (proper process).

### v0.2 — two-layer architecture LOCKED + Hanel SDK analysis (2026-06-24)
- Added §1a: two-layer architecture (Layer-1 reader/capture adapter ⟂ Layer-2 our portable ONNX engine); engine is OURS + portable + the universal default; reader built-in face/liveness (HN212) = optional accelerator, not foundation; server-side ONNX default with tiered placement (HN212-local + remote-client-active later); portability via ONNX Runtime covers future Mobile/Tablet/Ubuntu where the Hanel Windows SDK can't run.
- Folded the Hanel HN212 SDK analysis + 5 verify-items; Hanel = priority Layer-1 CaptureAgent, not the platform.
- VE-01 LOCKED (build portable engine + integrate readers); VE-02 reframed to a portable-first first slice; §6 roadmap restructured around the two layers.

### v0.1 — initial decision brief
- Verification method matrix, chip-as-trust-anchor, method→assurance→proof, liveness tier + OSS pick, legal basis + residuals, open decisions.
