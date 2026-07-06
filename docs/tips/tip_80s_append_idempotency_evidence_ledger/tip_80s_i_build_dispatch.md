# TIP-80S-I — Server Append Idempotency Dedup — BUILD DISPATCH (for Codex)

**Brief (build-locked, approved):** `docs/tips/tip_80s_append_idempotency_evidence_ledger/tip_80s_planning_brief.md` — build the **TIP-80S-I** section (§4.0–4.3). The Ledger (TIP-80S-L) is already **BUILT + COMMITTED `7df4826`** — do NOT re-touch it.
**Repo:** `D:\Task\Remote Signing\TagEkyc` (server). **Baseline:** current tip incl. TIP-80S-L `7df4826`. Agent repo OUT of scope.
**Companion (DONE):** agent TIP-80A `4a1084f` already sends `Idempotency-Key: <runId>|<submissionSlot>` on both append paths. So the wire is live — this slice makes the server dedup on it.
**Tier:** Tier-1 security-critical (anti-double-submit / evidence integrity).

## WHAT TO BUILD (§4.1)
Server-side append idempotency on **both** `POST .../capture-artifacts` and `POST .../evidence-results`:
- **Key = opaque string.** Read `Idempotency-Key` header. **Taxonomy (pinned):** header absent OR whitespace-only ⇒ **400 `IDEMPOTENCY_KEY_REQUIRED`**; header present but malformed/oversize/control-chars ⇒ **400 `IDEMPOTENCY_KEY_INVALID_FORMAT`**. **Do NOT parse the key** (the agent's `<runId>|<slot>` internal shape is not the server's business). Derive `endpointKind` from the route (`captureArtifact` vs `evidenceResult`) and `submissionSlot` from `artifactType`/`resultType` in the request body.
- **Require the key** on both endpoints. Safe now — the only append client (agent `4a1084f`) sends a key on both submit-only AND demo. The endpoints don't read it for append today (`VerificationSessionEndpoints.cs:95`) ⇒ update the append handlers + **all TIP-04/05/06/08 unit tests AND API/integration test helpers** to pass an `Idempotency-Key`.
- **Canonical fingerprint = hash the FULL normalized accepted-row** (server-computed effective values, NOT a hand-picked subset, NOT raw JSON) via `EvidenceCanonicalization.HashCanonical` (order-independent). **Invariant: if two submissions would persist a materially different row, their fingerprints MUST differ** (else a same-key second submission with different data is silently deduped/dropped).
  - capture artifact: `endpointKind:"captureArtifact"`, `sessionId`, `artifactType`, `artifactHash`, `metadataHash`, `captureSource`, `deviceId`, `captureAgentId`.
  - evidence result: `endpointKind:"evidenceResult"`, `sessionId`, `resultType`, `effectiveResult`, `effectiveReasonCodes`, `confidence`, `retryReasonCode`, `sanitizedSummaryRef`, `effectivePayloadHash` (server-derived for NFC/Face/Liveness), `payloadSignatureStatus`, `engineName`, `engineVersion`, canonicalized `inputCaptureArtifactIds`, and normalized decision-basis inputs feeding the effective result/hash. Use the **effective** (post-`Prepare*`) values, not the raw adapter-requested ones.
  - Never hash raw request text; never omit `endpointKind`/type.
- **Persist** per record: `sessionId`, `idempotencyKey`, `endpointKind`, `submissionSlot`, `mintedId`, `fingerprint`, `createdAt`. **Unique constraint on `(VerificationSessionId, IdempotencyKey)`.**
- **Replay resolution (deterministic):**
  - stored `endpointKind`/`submissionSlot` differs ⇒ **409 `IDEMPOTENCY_KEY_SLOT_MISMATCH`**.
  - fingerprint matches ⇒ return existing `mintedId`, `Accepted:true`, **`Deduplicated:true`**; NO new row, NO state re-transition, only a `*_DEDUPLICATED` audit event.
  - fingerprint differs (same key+slot) ⇒ **409 `IDEMPOTENCY_KEY_PAYLOAD_MISMATCH`**.
- **`Deduplicated: bool = false`** added to `CaptureArtifactSubmissionResponseDto` + `EvidenceResultSubmissionResponseDto` (additive; existing agent ignores it).

## HARD REDLINES (defect if violated)
1. **Dedup ordering — bypass write-state guards ONLY for existing-record reads, NEVER for new writes.** After `auth + session-exists + client-app access`, look up `(sessionId, key)`. **If a record EXISTS** ⇒ resolve as a pure READ (return dedup id, or 409 on fingerprint/slot mismatch) — no write, no transition — and this returns even when the session is now `ReadyToComplete` OR terminal (a lost-response retry after the final evidence transitioned state, or even after Complete, still recovers its id). **If NO record exists** (new/first key) ⇒ go through the FULL writable-state guard (`LoadWritableSessionAsync`: expiry, `ReadyToComplete`, terminal, policy) before appending. So a new/missing/mismatched key on an expired/terminal/ready session is STILL rejected — only the guaranteed-no-write dedup-read skips the write-state guards. **A dedup-read is a RECOVERY READ, not a new acceptance:** it returns the existing id with `Deduplicated:true`, emits ONLY the `*_DEDUPLICATED` audit (never a fresh `*_RECORDED`/state-change), and MUST NOT be read as a terminal/expired session "accepting" a new append.
2. **Atomicity — ONE transaction/boundary; NO self-saving repos before the idempotency record.** idempotency-record insert + artifact/evidence insert + state transition + audit MUST commit together. The append idempotency path MUST NOT call the self-saving append repos (`EfCaptureArtifactRepository.cs:9`, `EfEvidenceResultRepository.cs:9`, `EfAuditEventRepository.cs:9`) in a way that commits BEFORE the idempotency record — use an explicit boundary following the existing `EfVerificationFinalizationBoundary` (`:16`) pattern (or equivalent single-transaction seam) so a crash can't accept an append without recording its key. Conflict handling: insert-wins first; on unique-constraint conflict (concurrent same-key), load existing + compare fingerprint (same⇒dedup, diff⇒409) — the unique constraint is the concurrency arbiter.
3. **A key MUST NEVER bind two different fingerprints** ⇒ 409, never silently overwrite/accept.
4. **NO cross-session dedup** (key scoped per `VerificationSessionId`).
5. **NO change to proof/manifest/completion or the NFC/FaceMatch/Liveness decision-basis validation** (`Prepare*` paths byte-identical); NO existing-guard weakening (expiry/auth/terminal).
6. **NO PII/biometric/vault-ref/raw-payload** in the idempotency store or the `*_DEDUPLICATED` audit (audit = sessionId, redacted/hashed key, endpointKind, slot, mintedId, deduplicated:true).
7. **NO ledger change** (TIP-80S-L `7df4826` frozen); **NO agent change** (TIP-80A `4a1084f` frozen).

## DEFINITION OF DONE (§4.3 — all boxes)
- [ ] Both endpoints: first mints; same-key+same-fingerprint ⇒ same id + `Deduplicated:true` (no dup row, no re-transition); diff fingerprint ⇒ 409 `IDEMPOTENCY_KEY_PAYLOAD_MISMATCH`; diff slot ⇒ 409 `IDEMPOTENCY_KEY_SLOT_MISMATCH`; missing/blank ⇒ 400 `IDEMPOTENCY_KEY_REQUIRED`; malformed/oversize/control ⇒ 400 `IDEMPOTENCY_KEY_INVALID_FORMAT`. Unit-tested each, incl. **same-semantic-payload-different-JSON-order ⇒ dedup** (proves canonical, not raw-text) AND **same-key + a materially-different field (e.g. different `confidence`/`engineName`/`reasonCodes`) ⇒ 409** (proves the fingerprint covers the full row, no silent drop).
- [ ] Dedup ordering: (a) same-key/same-fingerprint replay after `ReadyToComplete` AND after terminal `Completed` ⇒ returns the dedup id (lost-response-on-final-evidence + post-complete cases); (b) a **new/missing/mismatched key** on an **expired** or **terminal** session ⇒ still REJECTED by the writable guard (no append) — tests both.
- [ ] Atomicity: single transaction boundary (following `EfVerificationFinalizationBoundary` or equivalent — NOT the self-saving append repos before the idempotency record) + unique constraint; crash-safety tested (no accepted-append-without-key-record); concurrent same-key ⇒ unique-constraint arbitrates to one mint + dedup for the other.
- [ ] Existing append tests updated for the required key; TIP-04/05/06/08 green; no proof/completion/decision-basis change; ledger (`7df4826`) untouched.
- [ ] `dotnet test` green (if the full-solution run shows unrelated shared-Postgres/SoftHSM env-fixture reds, run the affected project(s) isolated and report).

## COMMIT / PUSH DISCIPLINE
- Do NOT commit until I authorize; then by explicit file allowlist (report list first). EXCLUDE `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, `bin`, `obj`, `local_models`, `local_datasets`, `*.onnx`, `docs/reviews/**`. **NEVER push.** No real PII/biometric in code/tests/fixtures/logs.

## REPORT BACK (for adversarial verify on CODE)
Files changed; exact test names per DoD box (esp. dedup-before-ReadyToComplete-guard, same-payload-different-JSON-order⇒dedup, diff-fingerprint⇒409, atomicity/unique-constraint, missing-key⇒400); `dotnet test` result; any deviation + why. I verify on the code, not test-green.
