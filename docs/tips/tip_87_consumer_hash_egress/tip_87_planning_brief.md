# TIP-87 Suppress raw-derived hashes from BusinessConsumer egress (B1 mitigation for the SHA256(DG2) RRI) - Planning Brief

Status: READY_FOR_BUILD v1.0 (Codex + GPT both ACCEPT/BUILD-LOCKABLE @v0.3; Homeowner flipped 2026-07-10). v0.3 - Codex + GPT reviews patched (convergent). Applied: OQ-4 RESOLVED = Case A (verified on-code: no BusinessConsumer signed-manifest/evidence-ref download endpoint; only summary + JWS-claim views) -> B1 closes known BusinessConsumer DTO egress, not partial; OQ-1/2/3 LOCKED (remove-outright / remove PayloadHash uniformly / concrete build-recon grep scope); boundary test guards CLR AND JSON property names; serialization tests on all 3 surfaces + a JWS-decode defensive test; `docs/lld_03_api_contracts_v0_1.md` update added to scope (public schema break) + breaking-contract note; RRI status = OPEN / PARTIALLY-MITIGATED-B1 (not closed); fixed file:line (ToPublicEvidenceRef :739, not :633 which is the internal BuildManifestEvidenceRefs - do not touch). BUILD-LOCKABLE after this review; not dispatched until Homeowner marks READY_FOR_BUILD. Repo: D:\Task\Remote Signing\TagEkyc (server), baseline origin/master 1a8768e. This is the **B1 (API-egress) mitigation** for the git-tracked `SHA256(DG2)` RRI (`fa98531`): stop disclosing raw-derived hashes (`ArtifactHash`, `PayloadHash`) through BusinessConsumer read DTOs. **Egress-only: does NOT touch ingress/persist/manifest/normalized-basis/signed-proof.**
Date: 2026-07-10

## 0. Grounding (recon 09-10/07, file:line)
- **The leak:** `NfcArtifactHash = SHA256(DG2)` (`ReflectionHn212SdkBridge` sets `NfcArtifactBytes = dg2.ToArray()`) is a STABLE per-card, per-person identifier, proof-bound into append-only + backup, AND returned to BusinessConsumers. (Owned by the debt register `fa98531`, not this TIP.)
- **Two consumer-facing egress fields to suppress:**
  - `ArtifactHash` on `EvidenceLedgerCaptureArtifactDto.ArtifactHash` and `EvidenceRefSummaryDto.ArtifactHash` (BusinessConsumerContracts).
  - `PayloadHash` on `EvidenceLedgerRequiredCheckDto.PayloadHash` (`BusinessConsumerContracts.cs:63`) and `EvidenceLedgerEvidenceResultDto.PayloadHash` (`:77`). **PayloadHash is ALSO a leak channel:** for `CaptureQuality`/`DocumentOcr`/`FingerprintMatch` the server persists an ADAPTER-SUPPLIED `request.PayloadHash` as-is (`VerificationEvidenceApplicationService.cs:380`/`:389`; validated only for `sha256:` shape, `HashRef.cs`), so an adapter could put `SHA256(stable value)` there. (Server-owned NFC/Face/Liveness PayloadHash is session-varying, but removing all uniformly is simpler + closes the adapter channel.)
- **Egress surfaces + mappings:** `GET /api/ekyc/verification-sessions/{id}/evidence-ledger` -> `VerificationSessionApplicationService.GetEvidenceLedgerAsync` maps `artifact.ArtifactHash?.ToString()` + `latest.PayloadHash` (`:273`) + `item.PayloadHash` (`:301`); `GET /api/ekyc/evidence-packages/{id}` -> `VerificationCompletionApplicationService.ToPackageSummary` (`:358`) -> `ToPublicEvidenceRef` (`:739`) preserving `evidenceRef.ArtifactHash`. (`:633` is `BuildManifestEvidenceRefs` - the INTERNAL manifest builder, NOT the public projection; do not touch it.)
- **[OQ-4 RESOLVED = Case A, verified on-code by review]** there is NO BusinessConsumer endpoint that returns the full signed manifest / raw evidence refs. Only two public package surfaces exist (`VerificationSessionEndpoints.cs:206`): `/evidence-packages/{id}` -> `EvidencePackageSummaryDto` (summary via `ToPublicEvidenceRef` only), and `/verification-view` -> `EvidencePackageVerificationViewDto` built from the DECODED JWS proof claim (`:415`/`:702`), which does NOT list manifest evidence refs. So B1 (summary-DTO suppression) **DOES close the known BusinessConsumer DTO egress** - it is NOT merely partial. Defensive check retained (§2.4): decode the verification-view JWS and assert the signed claim carries no `artifactHash`/`payloadHash` (the `resultHash` preimage is 16 fields without `ArtifactHash`).
- **No real consumer reads them** (so this is a schema break, not a behavior break for a known reader): SignFlow's ledger DTO has only `EvidenceCompleteEligible` + `AllRequiredChecksPassed` (`SignFlow.Ekyc.Application/TagEkycContracts.cs:58`); the orchestrator reads only `ledger.EvidenceCompleteEligible` (`TagEkycVerificationOrchestrator.cs:174`).
- **Proof verification does NOT need them:** TIP-67G `resultHash` preimage = 16 fields, NO `ArtifactHash`; TIP-67B binds to `resultHash` from the decoded signed claim. So suppressing consumer egress is an API-contract change, NOT a proof-contract change.
- **Tests that must FLIP (not be "fixed"):** `Tip80SLedgerTests.cs:52`/`:73` assert `sha256:passed-payload`/`sha256:retry-payload` are VISIBLE -> must assert ABSENT. `ProjectionBoundaryTests.cs:58` currently PERMITS `PayloadHash` on ledger DTOs -> must FORBID `ArtifactHash`+`PayloadHash` on every BusinessConsumer DTO (turn the old permit into the guard rail).
- **Must stay byte-identical (proof this TIP is scoped right):** golden vector `docs/contracts/golden_neutral_proof_vectors.json`, TIP-67G tests, and the proof-bound hash tests (`Tip05...:1365`/`:1419`, `Tip06...`, `Tip08...:502`/`:557`).

## 1. Intent
Remove the two raw-derived hash fields (`ArtifactHash`, `PayloadHash`) from the BusinessConsumer read DTOs (evidence-ledger + evidence-package-summary) so a stable per-person identifier (`SHA256(DG2)`) and any adapter-supplied stable hash are no longer disclosed to third-party consumers - killing cross-consumer correlation via those fields. The internal proof-bound hashes are UNCHANGED (still computed, persisted, proof-bound). This is the recommended-first, cheapest mitigation; it is NOT a substitute for the DPO disposition of the RRI. **Completeness (OQ-4, resolved Case A on-code):** there is no BusinessConsumer endpoint returning the raw signed manifest/evidence refs, so B1 closes the known BusinessConsumer DTO egress (not merely partial). It still does NOT erase the internal append-only/backup/proof copy - so the RRI is **PARTIALLY-MITIGATED-B1, not CLOSED.**

## 2. Scope - ALLOWED (egress only)

### 2.1 Remove the fields from BusinessConsumer DTOs
- Remove `ArtifactHash` from `EvidenceLedgerCaptureArtifactDto` + `EvidenceRefSummaryDto`.
- Remove `PayloadHash` from `EvidenceLedgerRequiredCheckDto` + `EvidenceLedgerEvidenceResultDto`.
- **[OQ-1 LOCKED] Remove OUTRIGHT, do NOT replace** with a `ConsumerScopedArtifactRef`: no consumer reads these; a replacement would MINT a new stable-per-consumer identifier for an unproven need (anti data-minimization). A consumer-scoped ref = a separate future TIP only if a consumer states a concrete need.

### 2.1b Update the API-contract docs (this is a public schema break)
- Update `docs/lld_03_api_contracts_v0_1.md`: remove `ArtifactHash` from the documented `EvidenceRefSummaryDto` (`:178`) and the package-evidence-ref optional-artifact-hash note (`:272`); remove `PayloadHash` from the documented ledger DTOs. Do NOT change the proof/internal-manifest data-model docs (those describe the UNCHANGED internal chain).
- Record a **breaking-contract note** (changelog/runbook): these fields are removed from BusinessConsumer API responses for privacy; no replacement identifier; external consumers relying on them must migrate.

### 2.2 Update the mappings (stop emitting)
- `VerificationSessionApplicationService.GetEvidenceLedgerAsync` (`:273`/`:301`): stop passing `ArtifactHash`/`PayloadHash` into the DTOs.
- `VerificationCompletionApplicationService.ToPublicEvidenceRef` (`:739`): drop `ArtifactHash` from the PUBLIC evidence ref. (The internal manifest builder `BuildManifestEvidenceRefs` at `:633` MUST NOT change - it feeds the signed proof.)
- Touch ONLY the consumer-projection path. Do NOT change how the hashes are computed, persisted, or proof-bound.

### 2.3 Turn the boundary test into a guard rail
- `ProjectionBoundaryTests.cs:58`: flip from "permits `PayloadHash` on ledger DTOs" to **forbids `ArtifactHash` AND `PayloadHash` on ALL BusinessConsumer DTOs** (reflection over the BusinessConsumer contract namespace) - a permanent invariant, not a one-off edit.
- **[R:GPT-P1-05] forbid BOTH the CLR property names (`ArtifactHash`/`PayloadHash`) AND the serialized JSON names (`artifactHash`/`payloadHash` via any `[JsonPropertyName]`)** - a differently-named CLR property carrying the `[JsonPropertyName("artifactHash")]` attribute would slip a reflection-on-CLR-names guard.

### 2.4 Flip the assertion tests (do NOT fudge expected values)
- `Tip80SLedgerTests` (`:52`/`:73`): change from asserting the payload hashes are PRESENT to asserting they are ABSENT from the ledger response.
- **Serialization tests** on all three surfaces: `/evidence-ledger`, `/evidence-packages/{id}` summary, AND `/verification-view` JSON contain no `artifactHash`/`payloadHash` keys.
- **[OQ-4 defensive] JWS-decode test:** decode the verification-view `SignatureValue` and assert the signed proof CLAIM itself contains no `artifactHash`/`payloadHash` (catches a future proof-schema re-add that would re-open the residual path).

## 3. STOP / NOT
- **EGRESS ONLY.** Do NOT change ingress (`VerificationEvidenceApplicationService.cs:380`/`:389`), the normalized decision-basis builders, the canon labels, `EvidenceResult.PayloadHash` persistence, the manifest/evidence-ref/manifest-hash chain, or the signed proof. The hashes still exist internally.
- **Golden vector + proof-bound hash tests MUST stay GREEN, byte-identical.** If any golden/proof hash changes, the change leaked into the proof path = build is WRONG. (This is the proof B1 is scoped correctly.)
- Do NOT "fix" `Tip80SLedgerTests` by editing expected hash values to keep them - they must ASSERT ABSENCE.
- NO new stable identifier minted to "replace" the removed fields (OQ-1).
- NO migration, NO persistence change, NO change to the append-only tables.
- **The RRI `fa98531` becomes OPEN / PARTIALLY-MITIGATED-B1 (NOT CLOSED)** - B1 reduces third-party egress + cross-consumer correlation; it does NOT erase the internal append-only/backup/proof copy. The DPO disposition (acknowledge / this B1 / proof-contract TIP) is still required.
- **ONLY BusinessConsumer read DTOs + their projection mappings + tests + the API-contract docs change.** Internal evidence refs, the manifest builder (`BuildManifestEvidenceRefs`), storage rows, `EvidenceResult.PayloadHash`, and the proof builders are UNCHANGED. No internal model edited to satisfy a compile error; no placeholder null/empty hash field left behind.

## 4. Definition of Done
- [ ] `ArtifactHash` removed from `EvidenceLedgerCaptureArtifactDto` + `EvidenceRefSummaryDto`; `PayloadHash` removed from `EvidenceLedgerRequiredCheckDto` + `EvidenceLedgerEvidenceResultDto`.
- [ ] Ledger + package-summary mappings no longer emit them; ingress/persist/proof path UNCHANGED (diff scoped to consumer projection + DTO + tests).
- [ ] **`ProjectionBoundaryTests` now FORBIDS `ArtifactHash`+`PayloadHash` on all BusinessConsumer DTOs** (was: permitted), guarding BOTH CLR names AND `[JsonPropertyName]` JSON names (`artifactHash`/`payloadHash`).
- [ ] `Tip80SLedgerTests` asserts the payload hashes are ABSENT; serialization tests confirm `/evidence-ledger` + `/evidence-packages/{id}` + `/verification-view` emit no `artifactHash`/`payloadHash`; JWS-decode test confirms the signed claim carries neither.
- [ ] **Golden vector + TIP-67G + Tip05/Tip06/Tip08 proof-bound hash tests stay GREEN, UNEDITED** (proves egress-only, not proof-contract).
- [ ] `docs/lld_03_api_contracts_v0_1.md` updated (remove the documented `ArtifactHash`/`PayloadHash` from the BusinessConsumer DTOs, `:178`/`:272`); breaking-contract note recorded. No proof/internal-manifest doc changed.
- [ ] No known real consumer breaks (SignFlow reads only the 2 booleans). All construction sites of the DTOs updated (compiler-enforced); no internal model edited to satisfy compile; diff = consumer DTOs + projection mappings + tests + lld_03 only.
- [ ] `dotnet build` + `dotnet test` green; no migration; **RRI `fa98531` recorded as OPEN / PARTIALLY-MITIGATED-B1 (not closed).**

## 5. Review tier & attacks
Tier-1 (BusinessConsumer PUBLIC contract change + privacy-sensitive; low blast - no known reader). Attacks: (a) did any change leak into ingress/persist/normalized-basis/manifest/signed-proof (golden vector must be byte-identical)? (b) did `Tip80SLedgerTests` get "fixed" by editing expected hashes instead of asserting absence? (c) is the boundary test now a real FORBID guard (reflect the whole BusinessConsumer namespace, not just the two named DTOs - catch a future re-add)? (d) is the field removed from EVERY consumer surface (ledger + package summary + any other), or only some? (e) does removing the field break a real consumer (SignFlow) vs only a schema/test? (f) was a new stable identifier minted to replace it (must not)? (g) is the RRI wrongly marked closed?

## 6. Open questions - ALL LOCKED (both reviewers required these closed before build)
- **OQ-1 [LOCKED]:** remove the fields OUTRIGHT; no `ConsumerScopedArtifactRef` in TIP-87 (a replacement mints a new stable-per-consumer identifier for an unproven need; separate future TIP if a real consumer need appears).
- **OQ-2 [LOCKED]:** remove `PayloadHash` from ALL BusinessConsumer DTOs UNIFORMLY (not just adapter-supplied kinds) - simpler + uniform boundary + closes the adapter-supplied stable-hash channel; consumers verify via signed `resultHash`, not `PayloadHash`. Lost utility (consumer self-audit of its adapter's payload hash) is acceptable + re-addable later as a consumer-scoped ref.
- **OQ-3 [LOCKED - build recon checklist]:** grep ALL public/BusinessConsumer DTOs + endpoints + serialization for `ArtifactHash`/`PayloadHash`/`artifactHash`/`payloadHash`/`EvidenceRef`/`Manifest`/`Package`/`HashRef`; report every egress surface and classify each as (removed here / not-BusinessConsumer / proof-contract-residual). Not just DTO-type grep - include the JSON/endpoint responses.
- **OQ-4 [RESOLVED = Case A, verified on-code]:** NO BusinessConsumer endpoint returns the raw signed manifest/evidence refs (only summary + JWS-claim views; no manifest-download route). So B1 closes the known BusinessConsumer DTO egress. Retain the JWS-decode defensive test (§2.4). (Had it been Case B - a signed-manifest download - B1 would have been partial and the residual would need a proof-contract mitigation; that is not the case here.)

## 7. Companion / boundary
- **The RRI `SHA256(DG2)`** (debt register `fa98531`) stays open: B1 is egress-only; the internal append-only/backup/proof copy is untouched and un-erasable. DPO still chooses (i) acknowledge / (ii) this B1 / (iii) a proof-contract mitigation TIP.
- **B2 (ingress domain-separation)** = a SEPARATE, more expensive slice: it would domain-separate/session-bind the adapter-supplied `PayloadHash` before persist, which CHANGES the manifest/signed-proof for `CaptureQuality` (in SignFlow S1, has a golden vector) = a proof-contract + golden-vector change. Deferred; only pursued if DPO rejects internal linkability.
- **TIP-82R** (raw-BIO retention/export policy) is a separate decision TIP; B1 does not depend on it.
