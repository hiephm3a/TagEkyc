# TIP-80S — Server Pre-Complete Evidence Ledger + Append Idempotency — Planning Brief

**Status:** **PATCHED after GPT + Codex review + contractor cross-repo verify.** SPLIT into two slices with different readiness:
- **TIP-80S-L (Ledger)** — read-only reconciliation endpoint. **READY_FOR_BUILD after this patch.** No agent dependency.
- **TIP-80S-I (Idempotency)** — append dedup. **NOT READY — needs a companion AGENT slice** (the frozen agent transmits NO idempotency key today) + the retry-model decision below.

**Repo:** `D:\Task\Remote Signing\TagEkyc` (server). **Baseline:** `6b7dade`.
**Companion agent:** CaptureAgent TIP-80 `e8d6f41` (frozen). **TIP-80S-I requires reopening the agent** in a small companion slice (see §4.0).
**Tier:** Tier-1 security-critical (evidence integrity / anti-double-submit / trust separation).

## 0. Server preflight — CONFIRMED on code (`6b7dade`)
- Append has NO idempotency: `AppendCaptureArtifactAsync` mints `Guid.NewGuid()` (`VerificationEvidenceApplicationService.cs:110`), `AppendEvidenceResultAsync` mints `Guid.NewGuid()` (`:330`). Only Create reads `Idempotency-Key` (`VerificationSessionEndpoints.cs:40`).
- No pre-Complete accepted-id ledger: `GET /verification-sessions/{id}` = summary only.
- Ledger data already exists: `ICaptureArtifactRepository.ListBySessionAsync` (`:198`), `IEvidenceResultRepository.ListBySessionAsync` (`:260`).
- Write guards to preserve: appends rejected once `ReadyToComplete` (409, `LoadWritableSessionAsync:1174`) or terminal; expiry + client-app auth enforced.
- **Complete uses LATEST-per-check selection** (`VerificationCompletionApplicationService.cs:93/514`); `IsReadyToComplete` uses latest (`VerificationEvidenceApplicationService.cs:~1353`). Ledger MUST mirror this.

## 0.1 CROSS-REPO GAP found by contractor (both reviews missed) — gates TIP-80S-I
`TagEkycHttpClient.SendAsync` (`:130-152`) sets ONLY the API-key header; `AppendCaptureArtifactAsync`/`AppendEvidenceResultAsync` (`:72`,`:83`) transmit NO `Idempotency-Key` and the request DTOs carry no `runId`/slot. **So the frozen agent sends no key → any server dedup keyed on `Idempotency-Key` is inert end-to-end.** TIP-80S-I therefore CANNOT be server-only; it needs a companion agent slice (§4.0).

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

# TIP-80S-I — Append Idempotency (NOT READY — needs §4.0 companion agent slice + retry-model lock)

## 4.0 Retry-model — LOCKED DECISION (Homeowner to confirm)
Idempotency protects the **lost-response** case, via a **companion AGENT slice** (small, reopens `e8d6f41`):
1. Agent transmits **`Idempotency-Key: <runId>|<submissionSlot>`** header on both appends — **pipe delimiter** (the slot value itself contains a colon, e.g. `artifact:NfcReadArtifact`, so `:` would be ambiguous). `runId` = caller-owned `TAGEKYC_RUN_ID`, stable by construction. **The server treats the whole key as an OPAQUE string** (no parsing) for the unique constraint; it derives `endpointKind` from the route (capture-artifacts vs evidence-results) and `submissionSlot` from `artifactType`/`resultType` in the request body, storing those alongside the opaque key.
2. Agent does a **bounded in-process identical retry**: on timeout/unknown, resend the EXACT same request bytes + same key up to N times, THEN report `submission_state_unknown`. (This safely supersedes TIP-80's "no auto-retry" now that the server dedups.)
3. **Re-run** (operator relaunch, re-capture) = a NEW `runId` = a new attempt; reconciled via the **ledger** (caller sees duplicates + `currentEvidenceResultId`, decides). NOT deduped by the append 409.

So: same key + same fingerprint ⇒ dedup; same key + different fingerprint ⇒ genuine client bug ⇒ 409. Re-capture does not hit the 409 path because it carries a new `runId`.

## 4.1 Scope — ALLOWED (server)
- Both append endpoints honor `Idempotency-Key` as an **opaque string** (no colon-parsing; slot values contain `:`). The agent uses `<runId>|<submissionSlot>` (pipe) but the server does NOT depend on that internal shape — it validates only non-empty + length bound (⇒ `IDEMPOTENCY_KEY_INVALID_FORMAT` on empty/oversize) and derives `endpointKind`/`submissionSlot` from route + request body, NOT from the key.
- **Dedup runs as a read, AFTER auth + client-app-session access check but BEFORE the writable-state guard (P1-01/Codex-P1):** an existing same-key replay returns the dedup response even when the session is now `ReadyToComplete` (the last evidence append transitions state at `:359`, so a lost-response retry MUST still dedup, not hit `SESSION_READY_TO_COMPLETE`). A missing/new key still passes through the normal writable guard.
- **Canonical idempotency fingerprint (P1-02) — server-normalized, NOT raw JSON:**
  - capture artifact: `{endpointKind:"captureArtifact", sessionId, artifactType, artifactHash, captureSource, deviceId, captureAgentId}` (+ any field that materially affects acceptance).
  - evidence result: `{endpointKind:"evidenceResult", sessionId, resultType, effectivePayloadHash (server-derived for NFC/Face/Liveness, `:264-326`), effectiveResult, inputCaptureArtifactIds, session-challenge/binding}`.
  - Hash via the existing `EvidenceCanonicalization.HashCanonical` pattern (order-independent). Never hash raw request text; never omit `endpointKind`/type.
- **Persist per idempotency record (P1-03):** `sessionId`, `idempotencyKey`, `endpointKind`, `submissionSlot`, `mintedId`, `fingerprint`, `createdAt`. Unique constraint on `(VerificationSessionId, IdempotencyKey)`.
- **Replay resolution (deterministic):**
  - `endpointKind`/`submissionSlot` differs from stored ⇒ 409 `IDEMPOTENCY_KEY_SLOT_MISMATCH`.
  - fingerprint matches ⇒ return existing `mintedId`, `Accepted:true`, `Deduplicated:true`; no new row, no re-transition, only a `*_DEDUPLICATED` audit event.
  - fingerprint differs (same key+slot) ⇒ 409 `IDEMPOTENCY_KEY_PAYLOAD_MISMATCH`.
- **Atomicity (P1-04/Codex-P1) — hard design, not OQ:** idempotency insert + artifact/evidence insert + state transition + audit in ONE EF transaction/boundary. Note current repos self-`SaveChangesAsync` (`EfCaptureArtifactRepository.cs:9`, `EfEvidenceResultRepository.cs:9`) + separate state/audit saves — this slice MUST introduce a single transaction boundary (unit-of-work) so a crash cannot accept the append without recording the key. Conflict handling: insert-wins first; on unique conflict, load existing + compare fingerprint (same ⇒ dedup, diff ⇒ 409).
- **Require key (OQ-1 LOCKED):** REQUIRE `Idempotency-Key` on the append endpoints for the trusted-adapter/capture-agent submit path; missing ⇒ 400 `IDEMPOTENCY_KEY_REQUIRED`. (Only viable AFTER the companion agent slice sends it — so TIP-80S-I ships with the agent slice, else demo/legacy keyless appends break.) Update existing append tests to supply a key.
- **Dedup audit event (P2-01):** `sessionId`, redacted/hashed `idempotencyKey`, `endpointKind`, `slot`, `mintedId`, `deduplicated:true` — no raw payload/PII/biometric.

## 4.2 STOP / NOT
- No proof/manifest/completion/decision-basis change; no guard weakening; no cross-session dedup; no key binding two different fingerprints (must 409); no PII in idempotency store/audit.

## 4.3 Definition of Done (TIP-80S-I) — gated on §4.0 agent slice
- [ ] Companion agent slice: transmits `Idempotency-Key=<runId>|<submissionSlot>` (pipe delimiter; server treats it as opaque) on both appends + bounded in-process identical retry before `submission_state_unknown`; goes through its own brief→review→build in the CaptureAgent repo.
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
