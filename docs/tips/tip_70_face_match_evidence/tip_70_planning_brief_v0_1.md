# TIP-70 Face-Match Evidence Path — Decision-Basis + Capture-Binding + Server-Authoritative Score-Threshold — Planning Brief

**Version:** 0.4
**Status:** **READY_TO_DISPATCH** — second engine slice (FaceMatch), the **proof-bound FaceMatch evidence path**; mirrors TIP-69 NFC. Tier-1 (EBS evidence surface). GPT + Codex rounds 1–3 APPLIED — r1: reference-face source LOCKED to trusted chip-DG2 + cross-evidence NFC dependency; r2: referenced NFC must be **`Result == Passed`** (TIP-69 append-time fail-closed makes that the sufficient + only-buildable guarantee — flags aren't persisted queryably); r3: DG2-hash wording unified to "referenced NFC artifact hash" (no DG2-sub-hash), reference-use honesty caveat added, `Passed` wording tightened.
**Baseline:** `448a05e` (post-TIP-69 + completion-level test).
**Decision basis:** `docs/verification_engine_liveness_strategy_decision_brief_v0_1.md` v1.0 (two-layer; VE-01 build-own-engine; honest assurance; **server-side-not-a-trust-boundary** — trust = capture provenance + challenge binding). TIP-69 brief (Verification-Extension model; the NFC decision-basis pattern to mirror).

## 0. Why this slice + what grounds it

TIP-69 built the seam + the NFC reference extension (proof-bound). FaceMatch is the next extension. Grounded in the as-built map (2026-06-26, post-`448a05e`):
- **`IFaceMatchEngine` port + `DeterministicFixtureFaceMatchEngine` exist but are NOT wired** (`VerificationEnginePorts.cs:3-48`). Score source seam ready.
- **FaceMatch evidence today = legacy adapter-supplied `PayloadHash`, trusted as-is** (`VerificationEvidenceApplicationService.cs:278-284`) — NOT proof-bound, NO threshold, NO capture-binding. (Contrast the NFC special-case `:260-277`.)
- **No `FaceMatchEvidenceDecisionBasis`** exists (only `NfcEvidenceDecisionBasis`).
- **`Confidence` is stored but never read** — `CalculateAssuranceLevel` (`:556-573`) ignores it; NO threshold logic anywhere. The adapter self-declares the `Result` today.
- **The server is hash-only** — `CaptureArtifact` holds `VaultRef?`/`ArtifactHash?` only; no byte-retrieval port; no ONNX/ML dependency. → a **real ONNX engine cannot run server-side without new byte-ingestion infra** (deferred; §6).
- Pattern to mirror: NFC's `canonical decision-basis → server-OWNED PayloadHash (HashCanonical label) → manifest → proof`, server-authoritative fail-closed, capture-binding.

## 1. Intent

Make **FaceMatch evidence proof-bound + capture-trusted + server-authoritative threshold-enforced**, mirroring TIP-69 NFC — by putting the match score + binding into a canonical **`FaceMatchEvidenceDecisionBasis`** whose digest is the server-owned `PayloadHash`. **Placement-agnostic:** the basis carries the score regardless of who computed it (capture-agent now; server-side later), so this slice does NOT lock the engine-placement fork. **No real ONNX, no model, no byte-ingestion** here (TIP-71); the score is the declared engine output (fixture in tests), and the server enforces the threshold + binding over it. **No liveness** (separate concern; TIP-72). No manifest/proof SHAPE change; legacy golden byte-identical.

## 1.5 Honest trust framing (carry from strategy)

- **Server-authoritative over the RESULT, not over the raw score.** The server enforces the threshold + binding and may coerce `ReviewRequired`, but unless the engine runs server-side on retrieved bytes (TIP-71+), the **score itself is the (authorized) agent's declared output** — exactly the NFC trust tier (server binds declared+validated facts; does not re-run the engine). "Threshold passed" ≠ "server independently verified the faces match." Recorded honestly.
- **FaceMatch ≠ liveness.** A passing match never implies anti-spoof/PAD. Liveness is a separate extension (TIP-72); `verified` must not imply it.
- **Reference-use honesty (carry the same honesty as score-source).** The server validates the referenced NFC evidence's eligibility (`Result == Passed`, same-session) + binds the declared reference into the proof, but it does NOT independently verify that the adapter actually used the DG2 bytes for the comparison — that arrives only with the server-side engine + byte-ingestion (TIP-71). Same trusted-(authorized-)agent tier as the declared `matchScore`.
- **Capture provenance is the trust anchor** (server-side-not-a-trust-boundary rule): an `ExternalPreStaged`/un-bound face capture cannot yield production-grade FaceMatch.

## 1.6 Reference-face source is LOAD-BEARING (LOCKED — the round-1 core fix)

FaceMatch has TWO faces with DIFFERENT roles — a **live** face and a **reference** face — and identity value lives entirely in the reference. A match against an untrusted reference (a user-uploaded selfie, a photographed card, a document image) proves only "two images look alike", NOT "the live person IS the CCCD chip-holder". So:

- **Production-grade FaceMatch requires `referenceFaceSource = ChipDg2FromTrustedNfc` ONLY.** For CCCD medical eKYC a document/selfie reference is forgeable (anyone can present a fake card photo) → it is NEVER production-grade. `DocumentImage`/`SelfieImage` references are allowed only as **assist / non-production / `ReviewRequired`**.
- **The reference must bind to a same-session `NfcValidation` evidence with `Result == Passed`** (cross-evidence dependency — the chip read that cryptographically established the DG2 trust). The DG2 reference is trusted via the NFC chain, NOT via live-capture binding.
- **Two roles, two trust mechanisms:** the **live** face must be TagEkyc session-capture-bound (`CAPTURE_BOUND_TO_SESSION`); the **reference** (chip DG2) is trusted via the NFC-evidence dependency. Do NOT reuse NFC's single-artifact `Any()` binding semantics (`VerificationEvidenceApplicationService.cs:442`) — it is unsafe for two roles.

This extends TIP-69's "facts must bind through PayloadHash" to a **cross-evidence dependency**: the FaceMatch basis binds the referenced NFC evidence id + the referenced **NFC artifact hash** (not a separate DG2-sub-hash — see §2b RRI), and the server resolves+validates that NFC evidence.

## 2. Scope — ALLOWED (floor: a real vertical slice)

1. **`FaceMatchEvidenceDecisionBasis` (mirror NFC + cross-evidence reference)** — additive field on the FaceMatch evidence submission:
   `{ liveFaceArtifactId + liveFaceArtifactHash; matchScore (decimal); thresholdApplied (server); isMatch (server-normalized); referenceFaceSource (enum: ChipDg2FromTrustedNfc | DocumentImage | SelfieImage); referenceEvidenceResultId; referenceEvidenceType (= NfcValidation for production-grade); referenceArtifactId; referenceArtifactHash (the referenced NFC artifact hash); optional referencePayloadHash (of the selected NFC evidence); live capture-binding obj (challengeHash domain-separated, sessionId, captureAgentId, deviceId, capturedAt, artifactHash — for the LIVE face); serverDecisionResult; adapterRequestedResult (audit); engineName/version; sanitizedSummaryLabel }`.
   - **Data classification (round-1 wording):** **no raw subject PII/biometric/plain face bytes** — only hashes + ids. But face artifact **content hashes are biometric-derived / linkable** references → classification = **internal-confidential biometric-derived references**, NOT "subject-PII-free" and NOT public-safe.
2. **Server-OWNED `PayloadHash`** — server canonicalizes the FINAL normalized basis + computes `PayloadHash` via `EvidenceCanonicalization.HashCanonical("tip-70-face-match-decision-basis", basis)` (NOT raw `SHA256(JCS)`). Adapter-supplied `PayloadHash` ignored / if present must match else reject `FACE_MATCH_PAYLOAD_HASH_MISMATCH`.
2b. **Reference-source fail-closed validator (the round-1 fix)** — for `FaceMatch` with `Result=Passed` to be **production-grade**, ALL must hold, else coerce `ReviewRequired` (never trusted-Passed):
   - `referenceFaceSource == ChipDg2FromTrustedNfc` — any other source → `FACE_MATCH_REFERENCE_NOT_PRODUCTION_GRADE`, max `ReviewRequired`.
   - `referenceEvidenceResultId` resolves to a **same-session `NfcValidation` evidence with `Result == Passed`**. **Why `Passed` is sufficient (and the only buildable check):** TIP-69's append-time fail-closed validator already guarantees that an `NfcValidation` can ONLY reach `Passed` if it carried the full chip-trust decomposition (`NFC_READ_OK`/`PACE_SUCCESS`/`SOD_INTERNAL_VALID`/`DG_HASHES_MATCH_SOD`/a CSCA state/a chip-auth state) + a trusted `CAPTURE_BOUND_TO_SESSION`. Those flags live inside the hashed canonical payload and are **NOT persisted queryably** — so TIP-70 does NOT (and cannot) re-read them; **`Result == Passed` IS the guarantee**. Missing / cross-session / **any non-`Passed` result** (`ReviewRequired`, `FailedIdentity`, `RetryRequired`, `TechnicalError`, `FailedCaptureQuality`, …) → `FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED`.
   - The referenced NFC evidence must have a `PayloadHash`, and `referenceArtifactHash` must match one of that NFC evidence's input `NfcReadArtifact` hashes. **RRI (do not invent a DG2 hash):** `NfcReadArtifact` carries a single artifact hash, NOT a DG2-specific sub-hash — TIP-70 binds the **NFC artifact hash + referenced NFC evidence id + NFC `PayloadHash`**; DG2-sub-artifact hashing is a later NFC/HN212-adapter slice.
3. **Server-authoritative threshold + `isMatch` + fail-closed** — for `FaceMatch=Passed`: require `matchScore >= configuredThreshold` AND positive `CAPTURE_BOUND_TO_SESSION` on the **live** face AND the §2b reference checks. **Server OWNS `thresholdApplied`, `isMatch` (= recomputed `score >= threshold`), `serverDecisionResult`, `PayloadHash`** — an adapter-supplied `isMatch` that conflicts with server recomputation is ignored or rejected `FACE_MATCH_DECISION_BASIS_MISMATCH`. Below-threshold → coerce `ReviewRequired` + `FACE_MATCH_BELOW_THRESHOLD`; bare Passed without basis/score → `FACE_MATCH_DECISION_BASIS_REQUIRED`. `serverDecisionResult` normalized BEFORE hashing (payload result ≡ persisted `EvidenceResult.Result`). Threshold = server config, NOT adapter-chosen.
4. **Score-source = adapter-declared; do NOT wire `IFaceMatchEngine` this slice** — the `matchScore` is the authorized capture-agent's declared engine output (§1.5 trust tier). The slice does NOT inject/invoke `IFaceMatchEngine` and adds NO ML deps; the `DeterministicFixtureFaceMatchEngine` is used ONLY as a **test helper** to generate deterministic scores (and remains available for future server-side invocation). Real engine + server-side invocation = TIP-71. (Resolves the round-1 "fixture vs declared score" contradiction.)
5. **Honest recording** — method (`face-match`), `engineName/version`, `thresholdApplied`, and a coarse score-band live in the proof-bound basis; `verified: passed` must never imply liveness/PAD or certified grade. `CalculateAssuranceLevel` (`:556`) **unchanged**; `category=IdentityEvidence` recorded, not enforced.
6. Tests (deterministic, **no raw subject PII/biometric** — synthetic hashes + numeric scores, never real faces) + docs (lld_03 additive FaceMatch basis field + error codes).

## 3. STOP / NOT in this slice (ceiling)

- **NO real ONNX / model / image-decode / byte-ingestion / `IEvidenceVault`** — declared/synthetic score only (NO runtime engine invocation; the fixture only generates test submissions); real engine = TIP-71.
- **NO liveness / anti-spoof** — TIP-72.
- **NO manifest/proof-claim SHAPE change** — bind via the existing `PayloadHash`; no new fields on `ManifestEvidenceRefDto` / `EvidenceProofEngineRef` / decision seed / manifest body.
- **NO `CalculateAssuranceLevel` (`:556`) change**, no `AssuranceLevel` enum change, no `Confidence`-into-assurance wiring (threshold drives the FaceMatch evidence Result only, not the global assurance mapping).
- **NO consumer-visible substate surface** (no `PayloadHash`/score to BusinessConsumer).
- **NO enum changes** (`EvidenceResultType`/`RequiredCheckType`/`CaptureSource` untouched; no VNeID).
- **NO raw subject PII/biometric** anywhere (face artifact hashes are biometric-derived references — internal-confidential, not public).
- **NO production-grade document/selfie-reference FaceMatch** — only `ChipDg2FromTrustedNfc` is production-grade; document/selfie reference flows + their assurance grading = follow-on (§6).
- **Legacy TIP-65/66 golden byte-identical**; legacy non-FaceMatch + non-NFC paths untouched.
- Touching the hash graph / proof claim shape / TIP-69 NFC path = defect → STOP.

## 4. Definition of Done

- [ ] **Binding:** FaceMatch sub-facts live in canonical `FaceMatchEvidenceDecisionBasis`; `PayloadHash` server-computed via the pinned `HashCanonical("tip-70-face-match-decision-basis", basis)`. **Tamper test:** mutate `matchScore`/a flag + recompute → manifest/proof binding changes; `ReasonCodes`-only mutate → NOT the proof-bound source.
- [ ] **Hash-override (malicious adapter):** basis A but `PayloadHash` B → server uses computed-A or rejects `FACE_MATCH_PAYLOAD_HASH_MISMATCH`; mismatched hash never reaches the manifest.
- [ ] **Threshold + server-owned `isMatch`:** `FaceMatch=Passed` with `matchScore < threshold` → coerced `ReviewRequired` + `FACE_MATCH_BELOW_THRESHOLD`; bare Passed without basis/score → `FACE_MATCH_DECISION_BASIS_REQUIRED`; adapter `isMatch` conflicting with server recompute → ignored/`FACE_MATCH_DECISION_BASIS_MISMATCH`. Threshold = server config.
- [ ] **Reference fail-closed (round-1 core):** FaceMatch `Passed` with `referenceFaceSource != ChipDg2FromTrustedNfc` → coerced `ReviewRequired` (`FACE_MATCH_REFERENCE_NOT_PRODUCTION_GRADE`), EVEN IF score ≥ threshold and live capture bound. A FaceMatch referencing a missing / cross-session / **any non-`Passed`** NFC evidence (`ReviewRequired`, `FailedIdentity`, `RetryRequired`, `TechnicalError`, `FailedCaptureQuality`) → not Passed (`FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED`). A FaceMatch correctly referencing a same-session `Passed` NFC evidence → Passed (the happy path).
- [ ] **Live-face capture trust (role-aware):** the `CAPTURE_BOUND_TO_SESSION` requirement applies to the **live** face artifact; an `ExternalPreStaged`/un-bound LIVE capture + Passed → never trusted Passed (`DIRECT_CLIENT_UPLOAD_UNTRUSTED`); the chip-DG2 reference is trusted via NFC dependency, not live-binding. `CAPTURE_BOUND_TO_SESSION` server-stamped iff the live artifact `IsCaptureBindingTrusted`.
- [ ] **Result agreement:** a coerced `ReviewRequired` is what the canonical payload binds (`serverDecisionResult`) AND equals the persisted `EvidenceResult.Result`.
- [ ] **Vertical, not hollow:** a FaceMatch `EvidenceResult` reaches completion/proof and its proof-bound `PayloadHash` value is asserted e2e (mirror TIP-69 Tip06/Tip08).
- [ ] **Golden:** legacy TIP-65/66 byte-identical; new FaceMatch fixtures have new server-computed hashes; no SHAPE drift. Full `dotnet test` green. **No raw subject PII/biometric** in any fixture (synthetic hashes + numeric scores only).
- [ ] Docs: lld_03 FaceMatch basis + error codes.

## 5. Review tier

**Tier-1 adversarial (EBS evidence surface).** Attacks: (a) does the FaceMatch basis bind via `PayloadHash → manifest → proof` (tamper test)? (b) can a below-threshold or bare Passed slip through? (c) **REFERENCE: can a FaceMatch reach Passed with a non-chip-DG2 reference, or referencing a missing/cross-session/non-Passed NFC evidence (i.e. image-similarity masquerading as identity)?** (d) is the LIVE-face binding role-aware (an un-bound live capture rejected even if reference is trusted)? (e) is `isMatch`/threshold server-owned (adapter can't self-grade)? (f) is the score-source honesty framing correct (declared score, engine not re-run)? (g) legacy golden byte-identical + no SHAPE drift? (h) any raw PII/biometric? (i) any accidental "FaceMatch ⇒ liveness" implication?

## 6. Debt (carried / created)

| Item | Disposition |
| --- | --- |
| **Real ONNX face-match engine** (ArcFace/InsightFace-class) — replace fixture | **TIP-71.** Needs: commercial-OK model + license check; deterministic inference; PII-free test strategy (synthetic faces / pinned embedding vectors). |
| **Engine placement decision** (capture-agent-side vs server-side-transient) + the byte-ingestion / `IEvidenceVault` port if server-side | **TIP-71 fork** — this slice is placement-agnostic; the basis already carries the score either way. Lean: agent-side near-term (HN212/PcAgent), server-side for hosted-web later. |
| **Liveness / anti-spoof extension** (Silent-Face/MiniFASNet) | TIP-72 (separate threat model; FaceMatch ≠ liveness). |
| **Threshold calibration + governance** (config value, per-profile override, FAR/FRR justification) | Follow-on; this slice ships a single server config threshold. |
| Assurance policy catalog (category-aware) | Carried from TIP-69 (blocking before category drives assurance). |
| **Consumer verification-view exposure** of FaceMatch method / score-band / threshold / reference source | Deferred — this slice binds them internally via `PayloadHash` only; BusinessConsumer cannot inspect without a later verification-view/API TIP (privacy-reviewed). Carried with TIP-69's consumer-substate-resolver debt. |
| **Document/selfie reference (non-CCCD flows)** as a graded fallback | Follow-on — only `ChipDg2FromTrustedNfc` is production-grade here; document/selfie-reference flows (no-chip) need their own assurance grading. |

## 7. Decisions resolved (round-1) + remaining open

**Resolved (round-1):**
- **referenceFaceSource:** LOCKED — production-grade = `ChipDg2FromTrustedNfc` ONLY (live-face vs trusted chip-DG2); document/selfie reference = non-production / `ReviewRequired` assist only (§1.6, §2b).
- **Threshold source:** single global server config this slice; per-`VerificationProfile` override = debt (§6).
- **Score-source honesty:** accepted — server enforces threshold + owns `isMatch` over the authorized-agent-declared `matchScore`; does NOT re-run the engine (TIP-71 server-side). Same trust tier as NFC; documented in §1.5.
- **Engine wiring:** `IFaceMatchEngine` NOT wired this slice (test-helper only) — resolves the round-1 contradiction (§2.4).

**Remaining open for review:**
- **DG2 reference representation:** reference the NFC **evidence result id + NFC artifact hash** (preferred — no new artifact type, no DG2-sub-hash), OR explicitly allow `NfcReadArtifact` as a FaceMatch reference input (a small compat change at `VerificationEvidenceApplicationService.cs:728`). Confirm the cleaner path during build. (Lean: evidence-result reference; avoid touching the artifact-compat matrix.)
- Confirm no golden drift + no SHAPE change (mirror TIP-69 verification).
