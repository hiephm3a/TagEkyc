# TIP-69 Verification Engine Slice 1 — Chip-Evidence Decomposition + Capture-Binding + Verify-Engine Port — Planning Brief v0.1

**Version:** 0.7
**Status:** **ACCEPTED / READY_TO_DISPATCH** (GPT + Codex, after 5 review rounds — round-5 = PII-wording consistency only) — first CODE slice of the verification "brain", framed as the **Verification-Extension seam**; Tier-1 (EBS evidence surface). GPT + Codex review rounds 1–4 APPLIED (r1 binding defect; r2 server-authority + PII-classification; r3 server-owned `PayloadHash` + proof-bound≠consumer-visible; r4 server-normalized `serverDecisionResult` bound / positive `CAPTURE_BOUND_TO_SESSION` required / PII-linkability wording / pinned `HashCanonical` label). Binding = canonical `NfcEvidenceDecisionBasis` (server-normalized) → server-owned `PayloadHash` via pinned helper → manifest → proof.
**Baseline:** `140ea29` (verification-engine strategy v1.0 committed)
**Decision basis:** `docs/verification_engine_liveness_strategy_decision_brief_v0_1.md` v1.0 (two-layer LOCKED; VE-01 build-own-engine + integrate-readers; VE-06 CSCA; **VE-07 chip-evidence decomposition PROPOSED**; capture-binding; honest assurance; server-side-not-a-trust-boundary).

## 0. Why this slice + what grounds it

The strategy is codified; this is the FIRST code slice. It builds the **shared seam** that BOTH the HN212 client-agent path and the future portable-ONNX-engine path plug into — so we don't fork. Grounded in the as-built map (2026-06-25):
- Evidence ingest exists: `POST /evidence-results` → `EvidenceResultSubmissionRequestDto` → `EvidenceResult` (`EvidenceResultType` incl. `NfcValidation`/`FaceMatch`/`Liveness`/`FingerprintMatch`; `Confidence`; `ReasonCodes`; **`SanitizedSummaryRef`**; `PayloadHash`; `EngineName/Version`).
- Completion → proof exists: `CalculateAssuranceLevel` (`VerificationCompletionApplicationService.cs:556`), `BuildEvidenceEngines` (`:620`), the neutral proof claim (TIP-67B).
- **THE BINDING SURFACE = `PayloadHash` (corrected after round-1 review).** The manifest evidence ref (`ManifestEvidenceRefDto`, `ManifestContracts.cs:7`; built at `VerificationCompletionApplicationService.cs:603`) binds only `Type/Id/VaultRef/ArtifactHash/PayloadHash`. The proof signs `BuildEvidenceEngines(...)` + `manifestHash`; `EvidenceProofEngineRef` has no reason/sub-state fields. Therefore **`ReasonCodes` and `SanitizedSummaryRef` are NOT proof-bound** — they are query/debug/label surfaces only. **Anything that must be cryptographically attested MUST be inside the canonical payload whose digest is `PayloadHash`.** (Today `PayloadHash` is adapter-supplied, `VerificationEvidenceApplicationService.cs:221` — see §2.)
- **NO engine ports exist** (lld_04 conceptual).
- Live-probe (2026-06-25) confirmed the real chip sub-results (PACE/SOD-internal/AA-CA) we now decompose.

## 1. Intent

Realize **VE-07 (chip-evidence decomposition)** + **capture-binding** + a **Layer-2 verify-engine port** + **honest assurance recording** by making the VE-07 sub-states + capture-binding state part of a **canonical, internal-confidential decision-basis payload (no raw subject PII/biometrics)** whose digest is the evidence result's **`PayloadHash`** — so they bind into the proof along the existing path: `canonical payload → PayloadHash → ManifestEvidenceRef.PayloadHash → manifestBodyHash → manifestHash → signed proof`. **No manifest/proof-claim SHAPE change**; the binding is achieved through the field that is already hashed. This slice builds the **Verification-Extension seam** (§1.5) and ships the **chip/NFC extension as the reference** (the end-to-end proof carrier); FaceMatch/Liveness/C06/VNeID/OTP + the real engines are follow-on slices against the same contract.

## 1.5 Verification-Extension model (organizing principle)

Variable eKYC assurance (some transactions need Liveness + C06 + OTP, some need only one) is modeled as **composable extensions**, NOT a hardcoded ladder. Each check is a self-contained **Verification Extension**; the session's enabled set (the "flags" at create time) = `RequiredChecks` + per-extension config; the orchestrator composes whatever is enabled; the neutral completion-service aggregates the union into ONE proof + a derived assurance.

This already fits the as-built neutral/additive evidence pipeline (every extension just emits `EvidenceResult`(s) bound into the same manifest). Adding C06/OTP later = add an extension, **no re-architect**.

**Extension contract (the abstraction this seam introduces):**
```
VerificationExtension {
  id / requiredCheckType        // existing enum + planned VNeID, OtpFactor
  category                      // IdentityEvidence | PossessionFactor | QualityGate
  capturePort / enginePort      // its Layer-1 capture-adapter + Layer-2 engine-port
  emitsEvidenceType             // EvidenceResultType
  needsInputs[]                 // dependency graph (FaceMatch needs NFC DG2 ref-face)
  requirement: Required | Optional
  onFail: Block | DegradeAssurance
  bindingRequired: true         // must capture-bind to the session challenge
}
```

**Load-bearing rules (these keep "independent flows" from becoming independent trust):**
1. **Independent in EXECUTION, centralized in TRUST** — an extension runs its own flow + emits evidence, but it MUST NOT self-grade assurance or bind directly into the hash chain; the neutral completion-service derives assurance + signs. ⚠️ **As-built gap (round-1):** today completion trusts the `TrustedAdapter`-submitted `Result=Passed` (`VerificationEvidenceApplicationService.cs:122`) — so "centralized trust" is only partial. TIP-69 adds **server-side fail-closed validators** (NFC decomposition + capture-binding) so a submitted `Passed` is not blindly trusted for production-grade.
2. **Category is a CONTRACT FIELD, reserved — NOT yet enforced** — `category` is recorded on the extension/evidence, but as-built `CalculateAssuranceLevel` (`:556`) ignores it (only checks required-check membership). So this slice **records** category; it does NOT make category drive assurance. **Invariant pinned now:** no `PossessionFactor` (OTP) may promote *identity* assurance until the assurance policy catalog replaces the hardcoded mapping (follow-on; §6/§7).
3. **Dependency-aware orchestration** — parallel where independent (OTP ⟂ FaceMatch), ordered where dependent (NFC DG2 → FaceMatch ref-face; Liveness binds the same capture session). Declared via `needsInputs` (seeded by `EvidenceResult.InputCaptureArtifactIds`).
4. **Per-extension required/optional + on-fail + binding** — each extension carries its own fail policy (block vs degrade-assurance) and MUST record its capture-binding state.

**Anti-overbuild discipline:** adopt the extension MODEL (one common contract + a STATIC registry + an orchestrator that composes the enabled set). Do **NOT** build a dynamic plugin runtime (runtime DLL load / DI-scan / external plugin discovery) — that is overbuild for now. Statically-wired modules behind one interface is the target.

**This slice (TIP-69)** introduces the contract + the chip/NFC reference extension only. The FaceMatch/Liveness/C06/OTP extensions, the policy catalog, and policy-driven assurance mapping are scoped as follow-ons (§6, §7).

## 2. Scope — ALLOWED (floor: a real vertical slice, not 1 class)

1. **Canonical decision-basis payload + server-computed `PayloadHash` (NFC)** — define a structured `NfcEvidenceDecisionBasis` { VE-07 sub-state flags; capture-binding state; **`serverDecisionResult`** (the persisted, server-normalized result — NOT the raw adapter result; see normalization rule below); optional `adapterRequestedResult` (audit-only); engineName/version; input artifact ids + content hashes; sanitized summary label }. The VE-07 flags are status flags (`NFC_READ_OK`, `PACE_SUCCESS`, `SOD_INTERNAL_VALID`, `DG_HASHES_MATCH_SOD`, `CSCA_VERIFIED`|`CSCA_NOT_VERIFIED`, `CHIP_AUTH_RESPONSE_VALID`|`CHIP_AUTH_WEAK_CONTEXT`|`CHIP_AUTH_NOT_PERFORMED`).
   - **Data classification:** the object holds **no raw subject PII/biometric/plain CCCD/MRZ/DG fields, face, or certificate material** (PII chip data stays off-server). It MAY contain **pseudonymous, subject-derived/linkable artifact & content hashes** plus **internal operational identifiers** (`sessionId`, `captureAgentId`, `deviceId`, artifact ids, `challengeHash`). Classification = **internal-confidential only, NOT consumer-visible / not public-safe** (it is not "PII-free" in a linkability sense — handle as confidential).
   - **Normalization rule (the persisted result IS what's bound):** the canonical payload carries the **server-normalized `serverDecisionResult`** (after the validator in item 2). If the server coerces `ReviewRequired`, the payload binds `ReviewRequired` — never the adapter's downgraded-away `Passed`. The proof-bound payload and the persisted `EvidenceResult.Result` MUST agree.
   - **Binding (computed post-normalization, pinned helper):** the server hashes the **final, normalized** object via the existing domain-separated helper — `EvidenceCanonicalization.HashCanonical("tip-69-nfc-evidence-decision-basis", basis)` (`EvidenceCanonicalization.cs:19`), **NOT** a raw `SHA256(JCS(...))` (which would break the label convention) — to produce `PayloadHash`, so the flags bind: `payload → PayloadHash → manifest → proof`. (Legacy evidence types keep adapter-supplied `PayloadHash` unchanged — see §3 golden rule.)
   - **`PayloadHash` is server-owned for NFC (anti-spoof):** for `NfcValidation`, the **persisted `PayloadHash` is ALWAYS the server-computed value** (as-built persists `request.PayloadHash` at `VerificationEvidenceApplicationService.cs:221/:249` — this slice overrides that for NFC). Any adapter-supplied `PayloadHash` is **ignored, or if present MUST match** the server-computed value else **reject `NFC_PAYLOAD_HASH_MISMATCH`** — an adapter cannot bind a decision-basis it didn't actually submit.
   - **What server-compute does / does NOT mean (honesty):** server recomputation binds the **declared + validated** decision-basis and prevents post-submission tampering; it does **NOT** by itself prove the raw chip operations (SOD/AA/CSCA) — those rest on the adapter/engine evidence + later CSCA verification (VE-06). "Server-computed `PayloadHash`" ≠ "server cryptographically verified the chip."
2. **Fail-closed NFC validator (server-authoritative)** — for `NfcValidation` with `Result=Passed`, **reject** (not "flag") unless the decision-basis carries the required sub-state set: `NFC_READ_OK`, `PACE_SUCCESS`, `SOD_INTERNAL_VALID`, `DG_HASHES_MATCH_SOD`, one CSCA state, one chip-auth state, **and the POSITIVE binding state `CAPTURE_BOUND_TO_SESSION`**. Negative binding states (`DIRECT_CLIENT_UPLOAD_UNTRUSTED`, `CAPTURE_BINDING_MISSING`, `CAPTURE_BINDING_UNVERIFIED`) can only lead to reject / `ReviewRequired` — they **never** satisfy `Passed`. A bare "NFC passed" fails with `NFC_EVIDENCE_DECOMPOSITION_REQUIRED`. **The server-computed validator result is AUTHORITATIVE OVER the adapter-submitted `Result`** — the adapter cannot self-grade `Passed` past missing decomposition/binding; the server rejects or coerces `ReviewRequired` (never trusted-Passed).
3. **Capture-binding contract + validator** — a concrete binding object `{ challengeHash, sessionId, captureAgentId, deviceId, capturedAt, artifactHash }` (where `challengeHash` is a **domain-separated hash** of the session challenge, never the raw challenge) included in the canonical payload (so binding is hashed, not inferred from loose fields). Validator: `CaptureSource.ExternalPreStaged` OR missing binding → forces `DIRECT_CLIENT_UPLOAD_UNTRUSTED` and **cannot** satisfy production-grade NFC/liveness/face trust (accepted only as non-production / `ReviewRequired`). `CAPTURE_BOUND_TO_SESSION` requires a verified challenge-bound capture, not just a present `CaptureAgentId`. This validation is **server-authoritative** — an adapter-submitted `Passed` over an `ExternalPreStaged`/un-bound capture is overridden to non-production / `ReviewRequired`.
4. **Layer-2 verify-engine port (seam only)** — define `IFaceMatchEngine` (ref-face + live-face → result + score/confidence + engineName/version) + a deterministic fixture adapter, to prove the Layer-2 seam shape. The **end-to-end proof carrier for THIS slice is the NFC extension** (item 1–3); FaceMatch reaching proof is exercised via the fixture but the real ONNX engine is TIP-70+. (Other ports — liveness/OCR/finger — declared same shape, deferred.)
5. **Honest assurance recording** — method/grade/sub-states are bound via the canonical payload → `PayloadHash` (item 1), NOT via reason codes and NOT via new proof-claim fields. `verified: passed` must never imply certified-grade or CSCA-verified. Assurance mapping (`:556`) **unchanged** this slice (category recorded, not yet enforced — §1.5 rule 2).
6. **`ReasonCodes` = operational mirror only** — coarse machine flags MAY be duplicated into `ReasonCodes` for query/debug/final-decision reason aggregation, but the **source of truth for VE-07 sufficiency is the payload-hashed canonical object**, never `ReasonCodes` alone.
7. Tests (fixtures, deterministic, **no raw subject PII/biometric**) + docs (lld_01 evidence decision-basis note; **lld_03 — the additive `NfcEvidenceDecisionBasis` submission field IS a contract change → document it**; README/index = housekeeping).

## 3. STOP / NOT in this slice (ceiling)

- **NO manifest/proof-claim SHAPE change** — NO new fields on `ManifestEvidenceRefDto`, `EvidenceProofEngineRef`, the decision seed, or the package/manifest body. The VE-07 binding is achieved via the EXISTING `PayloadHash` field (now carrying the canonical decision-basis for NFC), not by adding fields.
- **Golden rule (narrowed, round-1):** existing TIP-65/TIP-66 golden vectors MUST stay **byte-identical for unchanged legacy fixtures** (their `PayloadHash` is adapter-supplied and untouched). TIP-69 MAY introduce **new** NFC fixtures whose `PayloadHash` is server-computed over the new canonical payload (new expected hashes are legitimate). **Forbidden drift = manifest/proof SHAPE drift OR accidental changes to existing fixtures** — not the legitimate new hash of new test data.
- **NO `AssuranceLevel` enum change** and **no change to `CalculateAssuranceLevel` (`:556`)** this slice (category recorded, not enforced; policy-driven mapping = follow-on).
- **NO real engine yet** — `IFaceMatchEngine` gets a fixture adapter; the portable ONNX face-match (ArcFace/InsightFace) + liveness (Silent-Face) = TIP-70+; HN212 client agent = its own slice; CSCA validation = VE-06 slice.
- **NO raw subject PII/biometric/plain CCCD/MRZ/DG/face/cert** in code/tests/logs (use synthetic fixtures; never the real CCCD/face; pseudonymous hashes + operational ids are internal-confidential, not consumer-visible).
- **NO capture-surface/JWKS/CA/TSA/webhook/document-binding**; no global-enum-converter change; no `lld_02` flow rewrite.
- **NO consumer-visible substate surface** — the decision-basis is proof-bound for **internal audit / manifest verification only**. The BusinessConsumer view does NOT expose evidence refs / `PayloadHash` (`BusinessConsumerContracts.cs:106`; public evidence-ref summary `VerificationCompletionApplicationService.cs:709` returns no `PayloadHash`), and this slice does NOT add one. "Proof-bound" ≠ "consumer-visible": RP-side inspection of VE-07 substates = a follow-up payload-resolver / public verification extension (§6), not this slice.
- Touching the hash graph / 67A profile-challenge / proof claim shape = defect → STOP.

## 4. Definition of Done

- [ ] **Binding (the core fix):** VE-07 sub-states live in the canonical `NfcEvidenceDecisionBasis`; `PayloadHash` is computed/verified over it server-side. **Tamper test:** mutating a flag and recomputing `PayloadHash` changes the manifest/proof binding; a separate test confirms mutating `ReasonCodes` only is NOT treated as the proof-bound source of truth.
- [ ] **Hash-override test (malicious adapter):** adapter submits decision-basis A but `PayloadHash` of B → server persists the server-computed hash of A (or rejects with `NFC_PAYLOAD_HASH_MISMATCH`); the mismatched adapter hash never reaches the manifest.
- [ ] **Fail-closed NFC:** a bare `NfcValidation=Passed` (no decomposition) is **rejected** with `NFC_EVIDENCE_DECOMPOSITION_REQUIRED` and cannot move the session to ready/production-grade.
- [ ] **Capture trust:** an `ExternalPreStaged` / un-bound `NfcReadArtifact` + `NfcValidation=Passed` is NOT accepted as trusted production-grade (forced `DIRECT_CLIENT_UPLOAD_UNTRUSTED` / `ReviewRequired`); test proves it.
- [ ] **Vertical, not hollow:** at least one extension-produced `EvidenceResult` (the NFC extension) reaches completion/proof and its proof-bound `PayloadHash` is asserted end-to-end. A port/interface-only implementation is NOT accepted. `IFaceMatchEngine` port + fixture adapter defined (seam shape).
- [ ] **Result agreement:** a test where the server coerces `ReviewRequired` proves the canonical payload binds `serverDecisionResult=ReviewRequired` (NOT `Passed`) and equals the persisted `EvidenceResult.Result`.
- [ ] **Hash convention pinned:** `PayloadHash` is produced by `EvidenceCanonicalization.HashCanonical("tip-69-nfc-evidence-decision-basis", basis)`; a golden test fixes the expected hash for a known decision-basis (guards against label/canonicalization drift).
- [ ] **Golden:** existing TIP-65/TIP-66 vectors **byte-identical** (unchanged legacy fixtures); new NFC fixtures have new server-computed hashes; no manifest/proof SHAPE drift. Full `dotnet test` green. **No raw subject PII/biometric/plain CCCD/MRZ/DG/face/cert** in the decision-basis or any fixture; pseudonymous hashes + operational ids are internal-confidential, not consumer-visible.
- [ ] Docs: `lld_01` decision-basis/binding note; **`lld_03` documents the additive `NfcEvidenceDecisionBasis` submission field**; debt items recorded (§6).

## 5. Review tier

**Tier-1 adversarial (EBS evidence surface).** Attacks: (a) does the decomposition bind via `canonical payload → PayloadHash → manifest → proof` (tamper test), or is it cosmetic? (b) can a bare "NFC passed" slip through without sub-states (fail-closed)? (c) can an un-bound/`ExternalPreStaged` capture be recorded as trusted production-grade? (d) legacy golden byte-identical + no SHAPE drift? (e) any PII in the new decision-basis payload or fixtures? (f) is `category` correctly recorded-but-not-enforced (no accidental assurance inflation)?

## 6. Debt (carried / created)

| Item | Disposition |
| --- | --- |
| Portable ONNX face-match + liveness engine (real Layer-2) | TIP-70+ (fixture adapter now) |
| HN212 client capture-agent (Windows, posts decomposed evidence) | Separate slice (proven via live-probe) |
| CSCA validation (DS→CSCA chain) | VE-06 slice |
| Per-capture nonce (today session-level challenge only) | Optional later; this slice records bound-vs-unbound from provenance |
| Runtime assurance-mapping alignment to the §3 target matrix | Later (golden-safe) |
| Liveness/anti-spoof live-probe (v2) | Separate follow-up |
| FaceMatch / Liveness extensions (against the §1.5 contract) | Follow-on extension slices |
| **C06/VNeID extension** — new `RequiredCheckType` member (`VNeIDVerify`) + engine-port (identity-evidence category) | Follow-on (VE-04 lifts from "pending" to "enum-reserved") |
| **OTP extension** — new `RequiredCheckType` member (`OtpFactor`, PossessionFactor category) IF the proof must attest it; else stays in RP auth | Follow-on; decision in §7 |
| **Assurance policy catalog** (transaction-type → enabled-extension-set) + policy-driven `CalculateAssuranceLevel` (replace hardcoded `:556` combo) | Follow-on decision (golden-safe) |
| **Consumer-visible substate resolver** — public verification extension so a RP can inspect VE-07 substates (today proof-bound for internal audit only; `BusinessConsumerContracts.cs:106` / public summary `:709` expose no `PayloadHash`) | Follow-on (additive, privacy-reviewed) |

## 7. Decisions resolved (round-1 review converged)

- **Sub-state binding surface:** the canonical `NfcEvidenceDecisionBasis` → `PayloadHash` is the single source of truth (§2.1). `ReasonCodes` = operational mirror only; `SanitizedSummaryRef` = sanitized label only. (Closes the self-contradiction in earlier drafts.)
- **Golden:** legacy vectors byte-identical; new NFC fixtures get new server-computed hashes (§3 golden rule).
- **Variable-LoA / extension model:** `RequiredChecks` IS the per-session policy knob for the enabled set. The §1.5 contract shape (category / needsInputs / requirement / onFail) is recorded by this slice; category is NOT yet enforced by assurance (rule 2).
- **OTP:** **default = relying-party auth/MFA, NOT in the proof.** Add an `OtpFactor` `PossessionFactor` extension only when a medical/legal flow explicitly needs TagEkyc to attest the 2nd factor — and even then it must NEVER promote identity assurance (requires the policy catalog first).
- **C06/VNeID:** **do NOT add `RequiredCheckType.VNeIDVerify` to the runtime enum in TIP-69.** Reserve it in the decision register/docs only; the VE-04 / C06 extension slice adds the enum member + DTO/mapper/policy/contract tests (append-only, disabled-by-default). Keeps this slice focused.
- **Assurance policy catalog:** out of scope here; recorded as a **blocking prerequisite** before any extension `category` (OTP/C06/VNeID) is allowed to influence assurance — replaces the hardcoded `CalculateAssuranceLevel` (`:556`) in a later golden-safe slice (§6).
