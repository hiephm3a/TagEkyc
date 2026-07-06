# TIP-80S — Server Pre-Complete Evidence Ledger + Append Idempotency — Planning Brief

**Status:** **PATCHED after GPT + Codex review + contractor cross-repo verify.** SPLIT into two slices with different readiness:
- **TIP-80S-L (Ledger)** — read-only reconciliation endpoint. **BUILT + COMMITTED `7df4826`** (verified-on-code).
- **TIP-80S-I (Idempotency)** — append dedup. **READY_FOR_BUILD** — the companion agent slice (TIP-80A `4a1084f`) is DONE and now transmits the key on both paths, so the server can require + dedup. Retry-ON co-deploys with this slice.

**Repo:** `D:\Task\Remote Signing\TagEkyc` (server). **Baseline:** current server tip incl. TIP-80S-L `7df4826`.
**Companion agent:** CaptureAgent TIP-80A `4a1084f` (DONE) — sends `Idempotency-Key: <runId>|<submissionSlot>` on both append paths.
**Tier:** Tier-1 security-critical (evidence integrity / anti-double-submit / trust separation).

## 0. Server preflight — CONFIRMED on code (`6b7dade`)
- Append has NO idempotency: `AppendCaptureArtifactAsync` mints `Guid.NewGuid()` (`VerificationEvidenceApplicationService.cs:110`), `AppendEvidenceResultAsync` mints `Guid.NewGuid()` (`:330`). Only Create reads `Idempotency-Key` (`VerificationSessionEndpoints.cs:40`).
- No pre-Complete accepted-id ledger: `GET /verification-sessions/{id}` = summary only.
- Ledger data already exists: `ICaptureArtifactRepository.ListBySessionAsync` (`:198`), `IEvidenceResultRepository.ListBySessionAsync` (`:260`).
- Write guards to preserve: appends rejected once `ReadyToComplete` (409, `LoadWritableSessionAsync:1174`) or terminal; expiry + client-app auth enforced.
- **Complete uses LATEST-per-check selection** (`VerificationCompletionApplicationService.cs:93/514`); `IsReadyToComplete` uses latest (`VerificationEvidenceApplicationService.cs:~1353`). Ledger MUST mirror this.

## 0.1 CROSS-REPO GAP found by contractor (both reviews missed) — RESOLVED by TIP-80A `4a1084f`
Originally the frozen agent `TagEkycHttpClient.SendAsync` (`:130-152`) set ONLY the API-key header; the append methods transmitted NO `Idempotency-Key`, so any server dedup keyed on `Idempotency-Key` would have been inert end-to-end — TIP-80S-I could NOT be server-only. **This historical gap is now RESOLVED:** the companion agent slice TIP-80A (`4a1084f`, committed + verified-on-code) sends `Idempotency-Key: <runId>|<submissionSlot>` on both append paths, so this server slice's dedup is live end-to-end.

Also: TIP-80's failure model is "timeout ⇒ `submission_state_unknown`, stop; caller relaunches ⇒ **RE-CAPTURE** (new payload)". A re-capture is NOT an identical HTTP retry, which breaks naive dedup (content-hash ⇒ mints duplicate; `runId:slot`+payload-409 ⇒ rejects the legit re-run). Resolved by the retry-model in §4.0.

---

# TIP-80S-L — Pre-Complete Evidence Ledger (READY_FOR_BUILD)

## L.1 Intent
An authorized read endpoint returning the session's accepted artifact + evidence ids and per-check status, so the caller reconciles server-side BEFORE `Complete` (never trusting the untrusted agent receipt).

**Honest-claim boundary (do NOT overstate after L ships):** TIP-80S-L only enables server-side pre-Complete reconciliation. It does **NOT** provide append idempotency, lost-response retry safety, or anti-double-submit guarantees. The only claim earned after L is: *the caller can inspect the server ledger before Complete instead of trusting the agent receipt.* Production-safe submit-only **retry** remains BLOCKED until TIP-80S-I + the companion agent slice ship.

## L.2 Scope — ALLOWED
- New endpoint **`GET /api/ekyc/verification-sessions/{id}/evidence-ledger`**, **business read scope** (`business.session.read` — the caller reconciles, NOT the agent/adapter key).
- Sanitized DTO:
  - `verificationSessionId`, `state`, **two distinct booleans (do NOT collapse):**
    - `evidenceCompleteEligible` — mirror ONLY `CompleteAsync`'s **evidence** precondition (`VerificationCompletionApplicationService.cs:79-112`): every required check has a latest evidence, each with non-null `payloadHash` and result ≠ `NotSupported`. **It is TRUE even when the verdict is fail/review** (e.g. latest `FailedIdentity`), because such a session is completable into a fail/review proof. **Caveat (name it accurately):** this does NOT account for the state/expiry/client-auth checks `CompleteAsync` runs BEFORE the evidence gate (`:57-77`) — the caller must still check `state`/expiry itself and handle Complete errors. It is the evidence-readiness signal only, not a "Complete will succeed" guarantee.
    - `allRequiredChecksPassed` — mirror `IsReadyToComplete` (`VerificationEvidenceApplicationService.cs:1366`): every required check's latest == `Passed` (this drives the server's auto-transition to `ReadyToComplete`).
    - Rationale: `IsReadyToComplete` (all-Passed) is NARROWER than "can be Completed". Exposing only a single `readyToComplete` would strand fully-evidenced fail/review sessions (caller thinks it can't Complete).
  - `requiredChecks[]`: `checkType`, `submissionStatus` (`missing` | `submitted`), `result` = **`VerificationResultDto?`** — the REAL enum `{NotAvailable, Passed, RetryRequired, FailedCaptureQuality, FailedIdentity, ReviewRequired, TechnicalError, NotSupported}` (`ContractEnums.cs:64`), NULL when `submissionStatus=missing`. **Do NOT collapse to a fabricated `Failed`/`NotApplicable`** — the caller needs the exact result for policy. `currentEvidenceResultId`, `payloadHash`, `createdAt` — all **computed with the SAME latest-per-check selection as Complete** (P1-05/Codex-P1). Never "any Passed evidence exists".
  - `acceptedCaptureArtifacts[]`: `captureArtifactId`, `artifactType`, `artifactHash`, `createdAt`.
  - `acceptedEvidenceResults[]`: `evidenceResultId`, `resultType`, `result`, `payloadHash`, `createdAt`.
  - **Duplicate visibility (P1-06):** because pre-TIP80S rows may already contain duplicates per check, the ledger lists ALL accepted submissions grouped by check/type **plus a server-selected `currentEvidenceResultId` per check using Complete's latest-selection rule**, so the caller can see "FaceMatch A vs B" and know which one Complete would use — or reject/flag for manual review.

## L.3 STOP / NOT
- Pure read (no business writes): an authorized successful ledger read MUST NOT mutate state, run completion, or append anything (test asserts state unchanged before/after). **Allowed exception:** a DENIED unauthorized/cross-tenant read MAY reuse the EXISTING sanitized `SESSION_ACCESS_DENIED` security audit (`VerificationSessionApplicationService.cs:195`) — correct security behavior, not a new side effect. No NEW writes beyond that.
- Sanitized only: no PII/biometric/vault-ref/raw-path/plaintext-identity (same rules as existing DTOs).
- Business-read scope only: agent/trusted-adapter/capture-agent credentials MUST NOT read it (cross-tenant + agent-key rejected via existing access path).
- No proof/manifest/completion/decision-basis change.

## L.4 Definition of Done (TIP-80S-L)
- [ ] `GET .../evidence-ledger` returns per-check status via Complete's latest-selection logic (not "any Passed"); `allRequiredChecksPassed` matches `IsReadyToComplete`; `evidenceCompleteEligible` matches `CompleteAsync`'s evidence-gate (missing-evidence / null-payloadHash / `NotSupported`). Unit-tested incl.: (a) a fully-evidenced fail case (all required checks present, latest `FailedIdentity`) ⇒ `evidenceCompleteEligible=true` + `allRequiredChecksPassed=false`; (b) stale-`Passed`-then-newer-`RetryRequired`/`ReviewRequired`/`FailedIdentity` ⇒ the check shows the newer result, never green.
- [ ] Duplicates surfaced: grouped submissions + `currentEvidenceResultId` per check (test with a pre-existing duplicate).
- [ ] Business-read scope enforced; agent/adapter key + cross-tenant ⇒ existing access-denied; sanitized (grep/test — no PII).
- [ ] Read-only: state/completion unchanged by a ledger call (test).
- [ ] Existing TIP-04/05/06/08 tests green; no proof/completion/decision-basis change.

---

# TIP-80S-I — Append Idempotency (READY_FOR_BUILD — companion agent slice TIP-80A `4a1084f` DONE)

## 4.0 Retry-model — LOCKED (companion agent slice DONE)
**Companion agent slice = TIP-80A, COMMITTED `4a1084f` (verified-on-code).** The agent now transmits `Idempotency-Key: <runId>|<submissionSlot>` on both append paths (submit-only ⇒ `<TAGEKYC_RUN_ID>|<slot>`; demo ⇒ `demo-<Guid>|<slot>` per-RunAsync-session) via validating `Headers.Add`, plus a default-OFF gated identical-retry (enabled only with `TAGEKYC_SERVER_IDEMPOTENCY=on`, retry-ON co-deploys with THIS server slice). **⇒ TIP-80S-I is NO LONGER gated — it can build now.** Since the agent sends a key on BOTH paths, the server can safely **require** the key universally.
Idempotency protects the **lost-response** case:
1. Agent transmits **`Idempotency-Key: <runId>|<submissionSlot>`** header on both appends — **pipe delimiter** (the slot value itself contains a colon, e.g. `artifact:NfcReadArtifact`, so `:` would be ambiguous). `runId` = caller-owned `TAGEKYC_RUN_ID`, stable by construction. **The server treats the whole key as an OPAQUE string** (no parsing) for the unique constraint; it derives `endpointKind` from the route (capture-artifacts vs evidence-results) and `submissionSlot` from `artifactType`/`resultType` in the request body, storing those alongside the opaque key.
2. Agent does a **bounded in-process identical retry**: on timeout/unknown, resend the **same semantic payload (same immutable DTO) + same key** up to N times, THEN report `submission_state_unknown`. Byte-identical JSON is NOT required — the **server canonical fingerprint is the dedup truth** (matches TIP-80A `4a1084f`). (This safely supersedes TIP-80's "no auto-retry" now that the server dedups.)
3. **Re-run** (operator relaunch, re-capture) = a NEW `runId` = a new attempt; reconciled via the **ledger** (caller sees duplicates + `currentEvidenceResultId`, decides). NOT deduped by the append 409.

So: same key + same fingerprint ⇒ dedup; same key + different fingerprint ⇒ genuine client bug ⇒ 409. Re-capture does not hit the 409 path because it carries a new `runId`.

## 4.1 Scope — ALLOWED (server)
- Both append endpoints honor `Idempotency-Key` as an **opaque string** (no colon-parsing; slot values contain `:`). The agent uses `<runId>|<submissionSlot>` (pipe) but the server does NOT depend on that internal shape; it derives `endpointKind`/`submissionSlot` from route + request body, NOT from the key. **Key taxonomy (pinned):** header absent OR whitespace-only ⇒ **400 `IDEMPOTENCY_KEY_REQUIRED`**; header present but malformed / oversize / control-chars ⇒ **400 `IDEMPOTENCY_KEY_INVALID_FORMAT`**.
- **Dedup ordering (P1-01/Codex-P1) — bypass ONLY existing-record reads, NEVER expiry/terminal for new writes:** after `auth + session-exists + client-app access`, look up `(sessionId, key)`. **If a record EXISTS**, resolve it as a pure READ (return dedup id / 409 mismatch — no write, no state transition) — this returns even when the session is now `ReadyToComplete` OR terminal, because it creates/transitions nothing (a lost-response retry after the final evidence transitioned state at `:359`, or even after Complete, must still recover its id). **If NO record exists** (new/first key), the request goes through the **full writable-state guard** (`LoadWritableSessionAsync`: expiry, `ReadyToComplete`, terminal, policy) before appending — so a new/missing/mismatched key on an expired/terminal/ready session is STILL rejected. Dedup-read bypasses write-state guards ONLY because it writes nothing; nothing that would append bypasses them.
- **Canonical idempotency fingerprint (P1-02/Codex-P1) — hash the FULL normalized accepted-row, NOT a hand-picked subset and NOT raw JSON.** The rule: the fingerprint MUST cover **every field that materially contributes to the accepted/persisted row** (post-normalization), so any material difference under the same key reliably ⇒ 409 (never a silent dedup that drops the second submission's differing data). Concretely:
  - capture artifact: `endpointKind:"captureArtifact"`, `sessionId`, `artifactType`, `artifactHash`, `metadataHash`, `captureSource`, `deviceId`, `captureAgentId` (all persisted acceptance-material fields).
  - evidence result: `endpointKind:"evidenceResult"`, `sessionId`, `resultType`, **effectiveResult**, **effectiveReasonCodes**, `confidence`, `retryReasonCode`, `sanitizedSummaryRef`, **effectivePayloadHash** (server-derived for NFC/Face/Liveness, `:264-326`), `payloadSignatureStatus`, `engineName`, `engineVersion`, canonicalized `inputCaptureArtifactIds`, and the normalized decision-basis inputs that feed the effective result/hash. Use the SERVER-computed effective values (after the `Prepare*` normalization), not the raw adapter-requested ones.
  - Hash via the existing `EvidenceCanonicalization.HashCanonical` (order-independent). Never hash raw request text; never omit `endpointKind`/type. **Guiding invariant:** if two submissions would persist a materially different row, their fingerprints MUST differ.
- **Persist per idempotency record (P1-03):** `sessionId`, `idempotencyKey`, `endpointKind`, `submissionSlot`, `mintedId`, `fingerprint`, `createdAt`. Unique constraint on `(VerificationSessionId, IdempotencyKey)`.
- **Replay resolution (deterministic):**
  - `endpointKind`/`submissionSlot` differs from stored ⇒ 409 `IDEMPOTENCY_KEY_SLOT_MISMATCH`.
  - fingerprint matches ⇒ return existing `mintedId`, `Accepted:true`, `Deduplicated:true`; no new row, no re-transition, only a `*_DEDUPLICATED` audit event.
  - fingerprint differs (same key+slot) ⇒ 409 `IDEMPOTENCY_KEY_PAYLOAD_MISMATCH`.
- **Atomicity (P1-04/Codex-P1) — hard redline, not "documented":** idempotency-record insert + artifact/evidence insert + state transition + audit MUST commit in ONE transaction/boundary. **The append idempotency path MUST NOT call the self-saving append repos (`EfCaptureArtifactRepository.cs:9`, `EfEvidenceResultRepository.cs:9`, `EfAuditEventRepository.cs:9`) in a way that commits BEFORE the idempotency record** — introduce an explicit boundary/unit-of-work following the existing `EfVerificationFinalizationBoundary` (`:16`) pattern (or equivalent single-`SaveChanges`/transaction seam), so a crash can never accept the append without recording its key. Conflict handling: insert-wins first; on unique-constraint conflict (concurrent same-key), load existing + compare fingerprint (same ⇒ dedup response, diff ⇒ 409) — the unique constraint is the concurrency arbiter.
- **Require key (OQ-1 LOCKED — now safe):** REQUIRE `Idempotency-Key` on both append endpoints; missing/blank ⇒ 400 `IDEMPOTENCY_KEY_REQUIRED`. **Safe because the only append client (agent TIP-80A `4a1084f`) now sends a key on BOTH submit-only AND demo paths.** Update every remaining keyless caller to supply the header: the endpoints don't read it for append today (`VerificationSessionEndpoints.cs:95`), so the append handlers + **all TIP-04/05/06/08 unit tests AND API/integration test helpers** must pass an `Idempotency-Key`.
- **Dedup audit event (P2-01):** `sessionId`, redacted/hashed `idempotencyKey`, `endpointKind`, `slot`, `mintedId`, `deduplicated:true` — no raw payload/PII/biometric.

## 4.2 STOP / NOT
- No proof/manifest/completion/decision-basis change; no guard weakening; no cross-session dedup; no key binding two different fingerprints (must 409); no PII in idempotency store/audit.

## 4.3 Definition of Done (TIP-80S-I)
- [x] Companion agent slice (TIP-80A `4a1084f`): transmits `Idempotency-Key=<runId>|<submissionSlot>` (pipe; server treats it as opaque) on both appends + default-off gated identical-retry. **DONE — verified-on-code.**
- [ ] Server dedup: first mints; same-key+same-fingerprint ⇒ same id + `Deduplicated:true`, no dup row, no re-transition; different fingerprint ⇒ 409 `IDEMPOTENCY_KEY_PAYLOAD_MISMATCH`; different slot ⇒ 409 `IDEMPOTENCY_KEY_SLOT_MISMATCH`; missing ⇒ 400 `IDEMPOTENCY_KEY_REQUIRED`. Unit-tested each, incl. same-semantic-payload-different-JSON-order ⇒ dedup.
- [ ] Dedup runs before the writable-state guard: replay after the session reached `ReadyToComplete` still returns the dedup id (test the lost-response-on-final-evidence case).
- [ ] Atomicity: single transaction boundary + unique constraint; documented/tested crash-safety.
- [ ] Existing tests updated for the required key; TIP-04/05/06/08 green; no proof/completion change.

## 5. Review tier & attacks
Tier-1. (a) different-fingerprint key reuse ⇒ 409? (b) dedup too coarse collapsing distinct submissions? (c) atomicity crash window? (d) ledger PII leak? (e) ledger cross-tenant / agent-key read? (f) ledger side effects / state mutation? (g) proof/completion/decision-basis unchanged? (h) dedup regressing a state transition or blocked by the ReadyToComplete guard? (i) does the agent actually send the key (else inert)? (j) does the ledger check-status match Complete's latest-selection (no false green)?

## 6. Open questions — RESOLVED
- OQ-1: require key on the trusted append path, ships WITH the agent slice (else keyless demo breaks). 400 `IDEMPOTENCY_KEY_REQUIRED`.
- OQ-2: `Deduplicated: bool = false` added to both response DTOs (additive).
- OQ-3: single EF transaction/unit-of-work boundary (not multiple repo `SaveChangesAsync`) + unique constraint.
- OQ-4: ledger under `business.session.read`; check-status via Complete's latest-selection; expose BOTH `evidenceCompleteEligible` (Complete evidence-gate) and `allRequiredChecksPassed` (`IsReadyToComplete`), never a single conflated `readyToComplete`; result-per-check (`VerificationResultDto?`) returned for caller policy.

## STOP / RRI (Homeowner — restate)
STOP + ask if the draft/build: changes proof/manifest/completion or decision-basis; weakens a guard; lets a key bind two fingerprints; dedups across sessions; leaks PII in ledger/idempotency store; gives the ledger side effects; ships TIP-80S-I server dedup WITHOUT the companion agent slice (would be inert); or the ledger check-status diverges from Complete's latest-selection.
