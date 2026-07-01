# TIP-73 — Agent-side Silent-Face Liveness Reference Engine + Earned `silent-face` Method — Planning Brief

**Version:** 0.3
**Status:** **READY_TO_DISPATCH / READY_TO_IMPLEMENT — GPT ACCEPT-WITH-PATCHES + Codex ACCEPT-WITH-TINY-PATCH, round-2 doc-hygiene applied (all CODE-verified).** Round-2: (i) §7 restructured into RESOLVED (OQ-1/2/3 LOCKED) vs STILL-OPEN (OQ-4/5 impl-choice only) so the locked scope cannot be reopened at build; (ii) stale `SelfieImage` first-build wording removed from §2.3 (now pure deferral); (iii) DoD adds an explicit `LivenessMedia`-only happy-path + `SelfieImage`-not-accepted test; (iv) **CODE-verified + corrected the `evidenceEngines` binding wording** — engine identity binds via the **signed proof claim `evidenceEngines` → `resultHash` (ES256-JWS)**, NOT the manifest hash (manifest refs carry no engine fields: `BuildManifestEvidenceRefs:603` vs `BuildEvidenceEngines:620` → `resultHashPreimage:45`). Fourth engine slice, the **agent-side liveness counterpart of TIP-71** feeding the **TIP-72 server-side Liveness evidence path**. Tier-1. ⚠️ Unlike TIP-71 (no server change), TIP-73 **requires ONE small reviewed server-delta**: TIP-72 deliberately **rejects** `silent-face` (`LIVENESS_METHOD_UNEARNED`); TIP-73 builds the engine, so it must **earn** `silent-face` *while preserving the anti-marker property* (§2.0). **Round-1 patches (all CODE-verified):** (1) OQ-1 LOCKED → explicit **versioned server-side allowlist** consistency check (NOT free-form `StartsWith`; capability/principal-binding deferred); (2) license wording downgraded from assertion → **candidate pending the committed packet**; (3) **first build uses dedicated `LivenessMedia` ONLY** — the `SelfieImage` fallback is BLOCKED by the server policy gate `IsCompatibleInput`/`AllowedSupportingArtifactTypes` (`VerificationEvidenceApplicationService.cs:1303` + `LocalDevRuntimePolicySource.cs:174` — does not include `SelfieImage`), so it becomes a separate flagged policy-change+test, OUT of first build; (4) added DoD **proof-binding test for `evidenceEngines`** (engineName is OUTSIDE the Liveness basis `PayloadHash` but bound via the signed proof claim `evidenceEngines` → `resultHash`/JWS, not the manifest hash — see round-2 (iv)); (5) engine fields are **fixed short values** (DB caps `EngineName`128/`EngineVersion`64 — `TagEkycDbContext.cs:93`), provenance lives in the packet not the engine string; (6) `0.80` labelled **carried-not-calibrated**; (7) sanity vector labelled **orientation-only, not PAD adequacy**.
**Baseline:** `2cd9c2d` (TIP-72 committed; 137 unit + 37 arch + 13 contract + 35 integration green). Agent repo baseline `4f8fa6a` (TIP-71, github.com/3asoftvn/TagEkyc.CaptureAgent).
**Decision basis:** verification-engine strategy v1.0 (§4 OSS pick = Silent-Face/MiniFASNet, §1a passive-RGB tier, §6 TIP-72/73 reconciliation); TIP-72 server Liveness contract; TIP-71 (agent FaceMatch — structural mirror); KEEP_NO_RAW_DEFAULT; model-license research; Homeowner locked decisions (engine OURS+portable ONNX; agent-side; no-raw-server; raw-protection relocates to the agent).

## 0. Why this slice + what grounds it

TIP-72 built the SERVER-side proof-bound Liveness evidence path (validates capture-binding + media-lock + **server-derives `isLive`** from the score + owns `PayloadHash`), but with **NO engine** — it accepts only the test-only `fixture-liveness` method and **rejects** the reserved real method `silent-face`. TIP-73 builds the **first real CaptureAgent liveness engine** that produces a genuine passive-2D liveness score with a commercial-clean OSS **candidate** (Silent-Face/MiniFASNet, Apache-2.0 — **pending the license/provenance packet, §2.8**) and submits a `LivenessEvidenceDecisionBasis` through the existing TIP-72 contract — keeping the TagEkyc server **biometric-free** and **inference-free**. Grounds:
- **TIP-72 server contract is ready**: `LivenessEvidenceDecisionBasis` (score + threshold + live-media ref + capture-binding + method/grade) → server-derived `isLive` (`score >= 0.80`) → server-owned `PayloadHash` → manifest/proof. The agent POPULATES + submits it — PLUS the §2.0 earned-method server-delta.
- **Engine candidate = Silent-Face-Anti-Spoofing / MiniFASNet-V2** (strategy §4 OSS pick): Apache-2.0 repo, passive/silent, single-frame 2D, ONNX via `Microsoft.ML.OnnxRuntime` in the agent. **Commercial-clean is a CANDIDATE claim subject to the §2.8 packet** (the repo license does not by itself prove every converted ONNX artifact/weight is commercial-clean). dlib has no liveness; there is no in-scope commercial-clean fallback engine (certified commercial PAD = the documented high-tier follow-on).
- **No-raw-server is foundational + as-built** (TIP-39/55; `VaultRef` always null). The P0 raw-protection control **relocates to the agent** — and liveness raw media is the *most* raw input, so the no-raw-agent controls bite harder than TIP-71.
- **TIP-71 already built the mirror**: `IFaceMatchScoreSource` (fixture + real), `SensitiveByteBuffer`, `FaceMatchScoreNormalizer`, fail-closed exceptions, the independent `challengeHash` canonicalizer, the two-credential role, the license packet, the earned-engineName invariant. TIP-73 REUSES these.

## 0.5 What makes liveness DIFFERENT from FaceMatch (do not blind-copy TIP-71)

1. **No reference image.** Liveness is computed on the **live media ALONE** — NO DG2 reference, NO same-session-`Passed`-NFC reference, NO reference-artifact-hash in the basis. The basis references only the **live-media artifact** + capture-binding. (Do NOT copy TIP-71's `referenceFaceSource`/`referenceArtifactHash` mapping.)
2. **Live media = `LivenessMedia` (first build).** The server policy gate `IsCompatibleInput` (`:1303`) accepts `LivenessMedia` for Liveness unconditionally, but `SelfieImage` ONLY if the client policy's `AllowedSupportingArtifactTypes` includes it — and the local default does NOT (`LocalDevRuntimePolicySource.cs:174`). So **the first build captures + submits a dedicated `LivenessMedia` artifact.** The `SelfieImage`-reuse fallback is a SEPARATE flagged change (policy + media-lock already allow it at `:914`, but the input-compatibility gate must be opened in policy + tested) — OUT of first build (§2.3, OQ-3).
3. **Earned method needs a server-delta.** FaceMatch (TIP-70) accepted any `engineName`; Liveness (TIP-72) **rejects** `silent-face`. TIP-73 changes the server guard to earn it (§2.0) — flagged + reviewed.
4. **Sharper no-overclaim.** Passive single-frame 2D liveness deters basic 2D print/screen replay ONLY. It is **NOT** certified PAD, anti-deepfake, anti-injection, anti-3D-mask, or iBeta/ISO-30107-3. `livenessGrade` stays `passive-2d-only`; the proof/docs MUST keep this caveat.
5. **Preprocessing is a silent-failure crux.** MiniFASNet expects an exact crop (face bbox expanded by a model-specific scale, padded, resized e.g. 80×80, channel order per the export). A mismatch **silently degrades** the score without erroring — the TIP-71 pts68-misalign failure mode. The agent MUST pin + verify preprocessing against the model card / a sanity vector.
6. **Possible 2-model ensemble.** MiniVision Silent-Face uses **two** MiniFASNet models (different scale/crop) summed. Pin EXACTLY which artifact(s) ship + how outputs combine; the packet hashes each.

## 1. Intent

Add a **passive-2D liveness score source** to the existing `TagEkyc.CaptureAgent` (.NET 8, the TIP-71 sibling repo) that: takes the session's **dedicated `LivenessMedia`** capture, runs **Silent-Face/MiniFASNet** locally to produce a `livenessScore ∈ [0,1]` (= P(live)), and submits a `LivenessEvidenceDecisionBasis` through the **existing TIP-72 server contract** — with **no raw media persisted/logged anywhere**, deterministic non-PII tests, an honest passive-not-certified posture, and a recorded license-evidence packet. **Plus** the minimal reviewed server-delta (§2.0) so the server accepts `silent-face` as an earned method. **No server inference, no ONNX/Silent-Face dependency in any server project.** The server threshold `0.80` is **carried from TIP-72, NOT a calibrated Silent-Face production threshold** — TIP-73 must not claim threshold adequacy (calibration = follow-on, §6).

## 1.5 Trust + raw-biometric posture (LOCKED — same as TIP-71 §1.5, restated for liveness)

> Server-side non-retention of raw liveness media is an intentional privacy/liability posture, not a cryptographic proof gap. The durable signed proof binds the server-normalized decision-basis — method, engine/version (via `evidenceEngines`), threshold, score, liveness grade, capture provenance, session/challenge binding, server-derived isLive, PayloadHash, manifest hash. Because TIP-72 computes PayloadHash server-side over the basis, the declared decision is tamper-evident within the proof chain. What this deliberately does not provide is later independent raw-media replay; that is deferred to the optional encrypted short-TTL EvidenceVault mode. **Agent-side inference is execution placement, not a trust boundary.** Raw-media protection relocates to the agent: in-memory only, no persistence/logging, secure disposal.

Trust remains server-side: the server validates the authorized agent + session/challenge/capture binding + the live-media artifact + score/threshold/method/engine semantics, **server-derives `isLive`** (never trusts an adapter verdict), normalizes, computes the server-owned `PayloadHash`, binds the basis into the manifest/proof. The agent's score is **declared** (same trust tier as TIP-71's FaceMatch score).

## 1.6 No-overclaim — passive-2D liveness ≠ certified PAD ≠ complete eKYC (SHARPER than TIP-71)

TIP-73 produces **passive-2D liveness evidence ONLY** (the live capture is not an obvious 2D print/screen replay per an uncertified OSS model). It MUST NOT claim — in engine output, proof wording, or docs: certified PAD, iBeta/ISO-30107-3, anti-deepfake, anti-injection, anti-3D-mask, active-challenge liveness, or complete high-assurance eKYC by itself. `livenessGrade` stays `passive-2d-only`. A profile requiring **certified** liveness MUST NOT be satisfied by a TIP-73 result. (The server's `CalculateAssuranceLevel` does not yet distinguish certified vs passive grade — the **grade→assurance policy-catalog** that would gate certified-only profiles is the TIP-72-noted follow-on, NOT this slice; TIP-73 must not pretend that gate exists, and must not bump assurance by shipping a real engine.)

## 2. Scope — ALLOWED

### 2.0 Earned-`silent-face` server-delta (THE central item — LOCKED to allowlist, minimal, reviewed)

TIP-72 rejects `silent-face` at `VerificationEvidenceApplicationService.cs:887` (`IsReservedRealLivenessMethod → LIVENESS_METHOD_UNEARNED`) and allows only `method == "fixture-liveness"` + `grade == "passive-2d-only"` (`:895`). TIP-73 makes `silent-face` an **earned** real method **without** re-opening the anti-marker hole.

**LOCKED decision (was OQ-1) — explicit versioned server-side allowlist consistency check:**
- Maintain an **explicit, versioned, server-side allowlist** of earned real liveness engine identities (e.g. `{ "silent-face-minifasnet": "<accepted version pattern>" }`). Accept `method == "silent-face"` + `grade == "passive-2d-only"` **iff** the top-level `EngineName` (+ `EngineVersion`) matches an allowlist entry. Keep `fixture-liveness` accepted **iff** `EngineName` is the fixture label. Reject the cross cases (method=`silent-face` + non-allowlisted/missing engine → `LIVENESS_METHOD_UNEARNED`; method=`fixture-liveness` + real-engine engine → `LIVENESS_DECISION_BASIS_MISMATCH`). **NOT** a free-form `StartsWith` on user text — a typo'd or unknown engine fails closed.
- **HONEST scope of what the allowlist earns (do not overclaim):** the allowlist + consistency check prevents the TIP-72 server-path / fixtures / typos / accidental split-brain from claiming `silent-face`. It does **NOT** prove inference actually ran — an authorized TrustedAdapter could still assert an allowlisted engine string without running it. That residual is the **declared-trust tier** (§1.5), accepted by design (same as the declared score). A stronger **capability/principal binding** (the allowlisted engine bound to a provisioned trusted-adapter/capture-agent profile) is a **DEFERRED follow-on**, folded into the existing cross-principal-binding debt (§6) — NOT required for TIP-73.
- **Everything else in `PrepareLivenessEvidenceResult` stays UNCHANGED**: score-range, threshold-conflict, media-lock (`:914`), **server-derived `isLive`** (`score >= 0.80`), capture-binding (`CAPTURE_BOUND_TO_SESSION` vs `DIRECT_CLIENT_UPLOAD_UNTRUSTED` + coerce ReviewRequired), below-threshold→ReviewRequired, **verdict-conflict→coerce ReviewRequired** (not reject), normalize-before-hash, server-owned `PayloadHash`. The `tip-72-liveness-decision-basis` canonicalization label + normalized-basis SHAPE are **UNCHANGED** (legacy golden byte-identical). `engineName/version` stay OUT of the basis (`:1024`) — bound via `evidenceEngines` (§2.2a).
- **STOP boundary:** the delta is ONLY the earned-method allowlist guard + its tests/docs. NO new fields, NO SHAPE change, NO threshold change, NO assurance change, NO new error codes beyond the existing `LIVENESS_*` set. More than that → STOP + flag.

### 2.1 Liveness score source in the existing CaptureAgent (LOCATION LOCKED, mirror TIP-71)
Add `ILivenessScoreSource` (fixture + real) to `TagEkyc.CaptureAgent.Core` (the existing sibling repo, OUTSIDE `TagEkyc.sln` / server CI). It MAY reference the TagEkyc **Contracts DTOs** (incl. `LivenessEvidenceDecisionBasisDto`) via the narrow contract reference, but MUST NOT add ONNX/Silent-Face/native deps to any server project or `TagEkyc.sln`. Pluggable so it is testable WITHOUT a model (`FixtureLivenessScoreSource` analogous to `FixtureFaceMatchScoreSource`).
- `fixture-liveness` source submits top-level `engineName` = the fixture label (server consistency check classifies it as fixture). The real source submits an allowlisted engine identity (§2.2a).

### 2.2 Silent-Face / MiniFASNet — the reference agent's liveness source
Local passive-2D liveness producing `livenessScore ∈ [0,1]` (= P(live)) from the live media. ONNX via `Microsoft.ML.OnnxRuntime`, referenced ONLY by the agent.
- **Pin the artifact(s) + preprocessing EXACTLY** (§0.5.5–6): exact ONNX file(s) + hash, input size, crop-scale/padding, channel order, normalization, softmax→P(live) mapping (incl. ensemble combine). A sanity vector (a known synthetic live + a known 2D-replay fixture) proves the pipeline **orientation** (live > spoof) — the liveness analogue of TIP-71's pts5-vs-pts68 check, guarding silent-degradation. **The sanity vector validates preprocessing/score direction ONLY — it is NOT liveness accuracy, PAD certification, or threshold adequacy evidence.**

### 2.2a Engine identity = TOP-LEVEL request field, FIXED SHORT values, bound via `evidenceEngines` (round-1 patch)
- `EngineName` / `EngineVersion` are the **top-level** request fields (TIP-71 §2.5a pattern). **Use fixed SHORT values** to respect the DB caps (`EngineName` ≤128, `EngineVersion` ≤64 — `TagEkycDbContext.cs:93`): e.g. `EngineName = "silent-face-minifasnet"`, `EngineVersion = "<short model+runtime version>"`. **Do NOT pack hashes/commits/long provenance into the engine fields** — that lives in the license packet (§2.8) and the agent's diagnostics.
- Engine identity is **excluded from the Liveness basis `PayloadHash`** (`:1024`) but IS bound into the **signed proof claim** via `evidenceEngines` → `resultHash` (ES256-JWS): `BuildEvidenceEngines` (`VerificationCompletionApplicationService.cs:620`) carries `EngineName/EngineVersion`, which enters `resultHashPreimage.evidenceEngines` → `resultHash = HashCanonical(...)` (`Es256JwsEvidenceSignatureBuilder.cs:27,45,50`). It is **NOT bound via the liveness basis `PayloadHash` NOR via the manifest hash** — the manifest evidence refs (`BuildManifestEvidenceRefs`, `:603`) carry ResultType/Id/ArtifactHash/PayloadHash only, no engine fields. So the earned-method gate's engine binding is proof-protected through the proof claim/`resultHash`/JWS, and TIP-73 MUST add a DoD test for that (§4).

### 2.3 Submit via the EXISTING TIP-72 contract (server unchanged except §2.0)
The agent posts (a) the dedicated **`LivenessMedia` capture-artifact** (trusted `CaptureSource` = `PcAgent`; `ExternalPreStaged` NEVER for the happy path), then (b) the `Liveness` evidence with a `LivenessEvidenceDecisionBasis`:
- `livenessScore` = normalized P(live) ∈ [0,1]; `method = silent-face`; `livenessGrade = passive-2d-only`; `liveMediaArtifactId`/`liveMediaArtifactHash` = the `LivenessMedia` artifact; `liveCaptureBinding` populated so the server stamps `CAPTURE_BOUND_TO_SESSION`.
- `thresholdApplied`/`serverDerivedIsLive`/`adapterRequestedVerdict` **omitted or server-consistent** — the server owns the threshold + derives `isLive`; a sent verdict MUST agree with `score>=0.80` else the server coerces ReviewRequired (`LIVENESS_VERDICT_MISMATCH`).
- **`challengeHash`** computed with the agent's existing independent canonicalizer (TIP-71 pinned `tip-69-capture-session-challenge` algorithm + golden vector — REUSE). The agent does NOT reference `TagEkyc.Application`.
- **`SelfieImage`-reuse fallback = NOT a first-build path.** The first-build happy path submits a dedicated `LivenessMedia` artifact ONLY. (`SelfieImage` reuse is a deferred follow-on — it needs a server policy change opening `AllowedSupportingArtifactTypes` to include `SelfieImage` + its own `IsCompatibleInput` test; tracked in §6 debt. It is NOT in TIP-73's first build.)

### 2.4 Score normalization (mirror TIP-71 §2.5)
`livenessScore` is decimal `[0.0,1.0]` = P(live), higher = more live. The agent normalizes the model output (softmax real-class prob, ensemble-combined) into that scale+direction BEFORE submission. **Fail-closed** on NaN/Infinity/<0/>1/missing/unknown-scale. A `LivenessScoreNormalizer` mirrors `FaceMatchScoreNormalizer`; tests cover orientation (live>spoof), range rejects, NaN/Infinity.

### 2.5 Engine-failure fail-closed (mirror TIP-71 §2.6, core eKYC)
On ANY failure — no face / multiple faces / unusable-or-too-small crop / media decode failure / engine exception or timeout / unsupported media format / ensemble-disagreement-if-defined — the agent fails closed: do NOT submit Liveness, OR submit `ReviewRequired`/`TechnicalError`-equivalent with sanitized reason codes. NEVER `Passed` when the pipeline is incomplete/ambiguous (no default score, no skip-and-pass). Tests: no-face, multi-face, decode-fail, engine-error/timeout, unsupported-media.

### 2.6 No-raw-agent controls (P0 — the security crux, bites harder for liveness)
Raw live frames/media in-memory only (reuse `SensitiveByteBuffer`); no disk persistence; no logging; excluded from crash/dump; secure disposal after inference; **native ONNX-runtime cache/temp behavior** documented + disabled/redirected-into-sandbox/proven-no-raw. No real faces/media in repo/tests. Best-effort-in-managed-.NET caveat (no claim of guaranteed memory erasure).

### 2.7 Deterministic non-PII tests
The score→basis mapping + disposal + fail-closed + the §2.0 earned-method allowlist tested with committed synthetic fixtures / the fixture score source (no real biometric, no model at CI time). A separate OPT-IN inference test may run the real ONNX on **synthetic** faces (SFHQ, MIT) + a synthetic 2D-replay sample with tolerance + the orientation assertion (live>spoof, NOT accuracy/PAD evidence).

### 2.8 License-evidence packet (apply-ready, mirror TIP-71 §2.8 — Silent-Face specifics; CANDIDATE gate)
**Silent-Face/MiniFASNet is the SELECTED commercial-clean CANDIDATE, subject to this committed license/provenance packet — NOT a settled fact.** No model/ONNX artifact may ship unless the packet proves: exact Silent-Face source repo + commit; the exact MiniFASNet model artifact filename(s) (PyTorch source + the ONNX export shipped) + their **hashes**; the **ONNX conversion recipe/commit** (export reproducibility); the ONNX-runtime package version; the upstream **Apache-2.0 LICENSE** text (code + weights) or immutable link + retrieval date; the README commercial-use grant excerpt; the model/weight **provenance** (training-data licensing not restricted/non-commercial); the SFHQ test-dataset license/version/hash; an explicit **exclusion list** (NO non-commercial PAD weights / no CelebA-Spoof-or-other-restricted-licensed weights unless cleared); the **passive-2d / not-certified-PAD caveat**; the production legal/procurement caveat. **STOP: any model/ONNX artifact not traceable to the packet must not ship.**

## 3. STOP / NOT in this slice (ceiling) — defect if violated
- **NO server-side raw media ingestion / NO `IEvidenceVault` / NO raw media on the server.**
- **NO server inference / NO ONNX / NO Silent-Face dependency in ANY server project.** Engine lib is agent-only.
- **NO change to manifest/proof SHAPE; NO change to the `tip-72-liveness-decision-basis` canonicalization or normalized-basis shape** (legacy golden byte-identical). The ONLY server change is the §2.0 earned-method allowlist guard — more → STOP + flag.
- **NO new error codes** beyond the existing `LIVENESS_*` set; **NO threshold change** (stays `0.80`, carried-not-calibrated); **NO assurance/`CalculateAssuranceLevel` change.**
- **NO certified-PAD / anti-deepfake / anti-injection / anti-3D / active-challenge claim** — keep `passive-2d-only` honesty.
- **NO active-challenge liveness, NO multi-frame motion/blink/depth** this slice (passive single-frame only).
- **NO `SelfieImage`-reuse path** without the separate policy change + test (OUT of first build, §2.3).
- **NO non-commercial PAD weights** without a separate license + evidence packet; **NO shipping the candidate engine before the §2.8 packet is committed.**
- **NO real face media committed** to repo/tests.
- **NO treating agent inference as a trust boundary** — execution placement only.
- **NO weakening the anti-marker property** while un-reserving `silent-face` — the allowlist consistency check must keep the server-path / split-brain / fixture-claims-real cases rejected; **NO free-form `StartsWith`.**

## 4. Definition of Done

**Server-delta (§2.0):**
- [ ] `silent-face` + `passive-2d-only` ACCEPTED **iff** top-level `EngineName`(+version) matches the **explicit versioned allowlist**; `fixture-liveness` accepted iff fixture engine; both cross cases rejected (`LIVENESS_METHOD_UNEARNED` / `LIVENESS_DECISION_BASIS_MISMATCH`). Tests prove all four quadrants + a non-allowlisted/typo'd real-looking engine fails closed.
- [ ] **Proof-binding test (`evidenceEngines`):** a Liveness proof/verification view contains the earned engine id (`silent-face-minifasnet`), and mutating the engine ref changes the `resultHash` / ES256-JWS proof verification (**NOT** the manifest hash, which carries no engine fields) — proving the earned-method gate's engine binding is regression-protected (engine identity is outside the basis `PayloadHash`, bound via the proof claim `evidenceEngines` → `resultHash`).
- [ ] `tip-72-liveness-decision-basis` golden vectors byte-identical (no SHAPE/canonicalization drift); server-derived isLive / capture-binding / verdict-conflict / PayloadHash logic unchanged. `dotnet test TagEkyc.sln` green.
- [ ] No ONNX/Silent-Face reference in any server `.csproj` (grep-verified).

**Agent engine:**
- [ ] The agent captures a dedicated `LivenessMedia`, runs Silent-Face/MiniFASNet, and submits `Liveness` evidence such that the (delta'd) server validates it to a proof-bound result — proven by an integration test against the real server contract — reaching `CAPTURE_BOUND_TO_SESSION` on the happy path; `ExternalPreStaged` appears only in the negative test (→ ReviewRequired).
- [ ] **First-build happy path uses `CaptureArtifactType.LivenessMedia` ONLY.** A test asserts the `SelfieImage` input is NOT accepted for Liveness under the default client policy (`IsCompatibleInput`/`AllowedSupportingArtifactTypes` rejects it) — enabling it requires a separate policy-change TIP with its own test.
- [ ] Top-level `EngineName = silent-face-minifasnet` + short `EngineVersion` (within DB caps) + `livenessScore` submitted; basis engine fields null/audit-only; a test asserts no method↔engineName split-brain.
- [ ] **Preprocessing-orientation guard:** a sanity vector proves live>spoof on the real engine (opt-in) — orientation-only, not PAD adequacy.
- [ ] **Score normalization:** orientation (live>spoof), range/NaN/Infinity rejects.
- [ ] **Engine-failure fail-closed:** no-face / multi-face / decode-fail / engine-error-or-timeout / unsupported-media → NEVER `Passed`.
- [ ] **Credential/role:** wrong-category fails, two-credential flow passes; no hardcoded secrets.
- [ ] **No-overclaim:** wording does not imply certified PAD / anti-deepfake / active / complete-high-assurance; `passive-2d-only` honest; `0.80` not claimed calibrated.

**No-raw-agent controls (P0, measurable, best-effort in managed .NET):** log-sink scan (no raw bytes/base64), temp/disk sandbox (incl. ONNX-runtime cache/temp documented + no-raw), raw-buffer disposal policy + test, crash/dump policy, repo/tests contain no real media. Best-effort caveat stated.

**Compliance:** license-evidence packet committed (§2.8) incl. ONNX conversion recipe + artifact hashes + Apache-2.0 texts + provenance + exclusion list + passive-not-certified caveat. Candidate engine not shipped until the packet is committed; untraceable artifact → not shipped.

**Contract pin:** the agent pins the TagEkyc contract baseline (`2cd9c2d` + the §2.0 delta) / a recorded compatible snapshot; a Contracts DTO change makes the agent build/test fail visibly.

## 5. Review tier
**Tier-1 adversarial.** Attacks: (a) does the agent leak raw live media to disk/logs/dumps (P0)? (b) **does the §2.0 earned-method allowlist re-open the anti-marker hole** — can the server-path / a fixture / a split-brain / a typo'd engine claim `silent-face`? (c) does liveness reach `Passed`/isLive ONLY via a bound, in-range, real-engine capture (no bypass)? (d) is the passive-2d-only honesty intact (no certified-PAD / calibrated-threshold overclaim)? (e) is the preprocessing pinned/verified (no silent-degradation)? (f) any ONNX/Silent-Face dep leaking into a server project? (g) is the Apache-2.0 weights provenance genuinely commercial-clean per the PACKET (not just the repo license)? (h) any SHAPE/canonicalization/golden drift? (i) is the engine identity proof-bound via `evidenceEngines` + regression-tested? (j) any real media in repo/tests? (k) is agent inference framed as placement-not-trust-boundary?

## 6. Debt (carried / created / updated)

| Item | Disposition |
| --- | --- |
| **Liveness / anti-spoof (Silent-Face ONNX)** | CLOSED for the passive-2d agent path by THIS slice (TIP-73 = agent engine; TIP-72 = server path). |
| **Earned-method server-delta (un-reserve `silent-face`)** | NEW — the one reviewed server change; anti-marker preserved via the explicit versioned allowlist consistency check (§2.0). |
| **Capability/principal binding** (allowlisted engine ↔ provisioned trusted-adapter/capture-agent profile) | NEW — DEFERRED; folds into the existing cross-principal-binding debt (the allowlist is declared-trust, not proof-of-inference). |
| **Cross-principal binding** (TrustedAdapter ↔ CaptureAgent ↔ device) | Carried from TIP-71 (unchanged) — honest, not pretended safe. |
| **`SelfieImage`-reuse Liveness path** | NEW — needs a client-policy change (`AllowedSupportingArtifactTypes` += `SelfieImage`) + test; OUT of first build (§2.3). |
| **Grade→assurance policy catalog** (certified vs passive gating) | Follow-on (TIP-72-noted) — TIP-73 does NOT bump assurance; certified-only profiles not satisfiable by passive silent-face until this lands. |
| **Certified / commercial PAD escalation tier** (iBeta/ISO-30107-3, active-challenge, anti-injection/deepfake/3D) | Follow-on / high-risk tier (strategy §4) — vendor + evidence packet; NOT this slice. |
| **Active-challenge liveness** (blink/turn/motion) | Future upgrade tier; passive single-frame only here. |
| **Liveness threshold calibration + governance** (spoof FAR/FRR, per-profile) | Follow-on (ships the carried `0.80`; the agent declares the engine) — ties to the FaceMatch calibration study methodology. |

## 7. Open questions for review

**RESOLVED / LOCKED (round-1 — do NOT reopen at build time):**
- **OQ-1 earned-method delta:** LOCKED to an explicit **versioned server-side allowlist** consistency check; capability/principal binding **deferred** (folds into the cross-principal debt); fixture-as-real **rejected** (§2.0).
- **OQ-2 engineName matching:** LOCKED to the explicit versioned allowlist — **NOT** a free-form prefix/`StartsWith`; engine fields are fixed short values within the DB caps (§2.2a).
- **OQ-3 media source:** first build LOCKED to a **dedicated `LivenessMedia` ONLY**; the `SelfieImage`-reuse fallback is a **separate policy-change follow-on** (needs `AllowedSupportingArtifactTypes` += `SelfieImage` + test), OUT of TIP-73 first build (§2.3, §6).

**STILL OPEN — implementation choice only (decided during build, not scope-affecting):**
- **OQ-4 ensemble:** ship the full MiniVision 2-model ensemble (closer to reference, heavier) or a single MiniFASNet-V2 (lighter, slightly weaker)? Decide by what the license/provenance packet can fully trace + what the sanity vector validates; document the choice + its tier implication.
- **OQ-5 preprocessing source-of-truth:** model-card preprocessing sufficient, or a reference-output vector from the original repo for byte/score parity? Lean: a committed sanity vector (synthetic live + replay) asserting orientation; full numeric parity opt-in.

## STOP / RRI (from Homeowner — restate)
STOP + ask for review if the draft/build tries to: add server-side raw media ingestion or `IEvidenceVault`; store raw media on server; change manifest/proof SHAPE or the `tip-72-liveness-decision-basis` canonicalization; change the threshold or `CalculateAssuranceLevel`; **widen the server change beyond the §2.0 earned-method allowlist guard, weaken the anti-marker property, or use a free-form `StartsWith` instead of the allowlist**; enable the `SelfieImage`-reuse path without the separate policy change + test; ship the candidate engine before the §2.8 packet is committed; claim certified PAD / anti-deepfake / anti-injection / active liveness / calibrated threshold; treat agent inference as a trust boundary; use non-commercial PAD weights without license evidence; or commit real face media.

## Changelog
### v0.3 — GPT + Codex round-2 doc-hygiene patches (CODE-verified)
- **§7 restructured** into RESOLVED (OQ-1/2/3 LOCKED) vs STILL-OPEN (OQ-4/5, impl-choice only) — removes the governance risk of a build reopening locked scope (GPT P1-01).
- **§2.3 stale `SelfieImage` wording removed** — the first-build path is now `LivenessMedia`-only with the fallback as a pure deferral pointer (GPT P1-02).
- **DoD** adds an explicit `LivenessMedia`-only happy path + a test asserting `SelfieImage` is rejected for Liveness under the default policy (GPT P2-01).
- **`evidenceEngines` binding wording corrected** (§2.2a, §4): CODE-verified that engine identity binds via the signed proof claim `evidenceEngines` → `resultHash` (ES256-JWS), NOT the manifest hash (`BuildManifestEvidenceRefs:603` carries no engine fields; `BuildEvidenceEngines:620` → `resultHashPreimage.evidenceEngines:45` → `resultHash:50`) (Codex P2).
### v0.2 — GPT + Codex round-1 review patches (all CODE-verified)
- **OQ-1/OQ-2 LOCKED** → explicit versioned server-side **allowlist** consistency check (not free-form `StartsWith`); capability/principal binding deferred to the cross-principal debt; fixture-as-real rejected. Added the honest scope note (allowlist ≠ proof inference ran; declared-trust tier).
- **License wording downgraded** (Status/§0/§2.8): Silent-Face = commercial-clean **CANDIDATE pending the committed packet**, not settled fact; added model/weight provenance to the packet gate.
- **First build = dedicated `LivenessMedia` ONLY** (§0.5.2, §1, §2.3): CODE-verified the `SelfieImage` fallback is blocked by `IsCompatibleInput`/`AllowedSupportingArtifactTypes` (`:1303` + `:174`); demoted to a flagged separate policy-change+test (OQ-3, §6 debt).
- **Added DoD proof-binding test for `evidenceEngines`** (§2.2a, §4): engine identity is outside the Liveness basis `PayloadHash` (`:1024`) but bound via `evidenceEngines` (`:620`/`Es256...:27`) — must be regression-tested.
- **Engine fields fixed short values** (§2.2a): CODE-verified DB caps `EngineName`128/`EngineVersion`64 (`TagEkycDbContext.cs:93`); provenance → packet, not engine string.
- **`0.80` labelled carried-not-calibrated** (§1, §6); **sanity vector labelled orientation-only, not PAD adequacy** (§2.2, §2.7).
### v0.1 — initial draft (agent-side Silent-Face liveness, earned-method server-delta, no-overclaim, no-raw-agent, license packet).
