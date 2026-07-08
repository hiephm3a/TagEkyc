# TIP-82S Generic Server-Side Session Cancel / Void - Planning Brief

Status: v0.5 - **READY_FOR_BUILD** (both re-reviews cleared: GPT ACCEPT-WITH-MINOR-PATCH -> READY, Codex NEEDS-PATCHES-nhe -> near-READY; v0.5 closed all: pinned the append-boundary interface `AppendIdempotencyApplyStatus.SessionTerminal`, swept stale "hashed" -> strict-token EventPayloadRef, reason length locked `<=64`, removed "MAY accept Idempotency-Key" -> header ignored completely, OQ-2/OQ-3 RESOLVED). v0.3/v0.4 fixed: P0 append-revive (BOTH reviewers independently confirmed), atomic state+audit CAS, retracted v0.2 "append harmless", false ledger claim. Closes the SERVER half of gap G-005.
Date: 2026-07-08
Repo: D:\Task\Remote Signing\TagEkyc (server). Baseline: 804e16b (post G-005).
Decision basis: OQ-5 verify-on-code - `Cancelled` state exists but is unreachable; a caller cannot void a session or stop it completing after a withdrawal-after-partial. Consumer-agnostic per the eKYC-abstraction rule: GENERIC session cancel a BusinessConsumer invokes with an OPAQUE reason - the words "withdrawal"/"consent" MUST NOT appear in the server API/domain/scope.

## 1. Intent
Give a BusinessConsumer caller an authoritative, idempotent, tenant-scoped way to CANCEL (void) its own verification-session such that: (i) a cancelled session can NEVER complete into a signed proof, (ii) a cancelled session can NEVER be revived by a late in-flight append, and (iii) the cancellation is recorded atomically in the append-only audit store with an opaque, non-PII strict-token reason. This closes G-005's server half. The reason is opaque; the server does not model "why".

## 2. Scope - ALLOWED

### 2.1 New endpoint POST /api/ekyc/verification-sessions/{id}/cancel (generic)
- Add route in `VerificationSessionEndpoints.cs:15-26`; handler mirrors `CompleteAsync` (:159-180): authenticate -> command -> Results.Ok / ToError.
- Auth: `authenticator.Authenticate(httpContext)` with NO endpoint-scope arg; category+scope check IN the app service (like `RequireBusinessCompletion`).
- **No Idempotency-Key header (OQ-2 RESOLVED):** cancel is idempotent by TERMINAL STATE (re-cancel of an already-`Cancelled` session => 200, no side-effect). Do NOT read/require the header (there is no cancel fingerprint store; a header would be fake idempotency and could imply a migration).
- Request DTO (NEW): `CancelVerificationSessionRequestDto(string? Reason = null, string? RequestId = null, string? CorrelationId = null)`. `Reason` is an OPAQUE, caller-defined **strict token/code, NOT free-text and NOT withdrawal-specific**: bounded `<= 64` chars, restricted token charset (e.g. `[A-Za-z0-9_.:-]`, no whitespace-sentences, no C0/C1 control chars; mirror the challenge validation `VerificationSessionApplicationService.cs:71-104`), else `INVALID_CANCEL_REASON`. The strict charset makes free-text/PII impractical; the server stores the validated token in `EventPayloadRef` (2.6), NEVER interprets/parses it.
- Response echoes the terminal state (`VerificationSessionStateDto.Cancelled`, ContractEnums.cs:60) + session id.

### 2.2 Auth: new scope session.cancel (BusinessConsumer), swept into ALL allowlists
- `ApplicationAuthorization.cs`: `SessionCancelScope = "session.cancel"` (:5-9) + `RequireBusinessCancellation<T>` mirroring `RequireBusinessCompletion` (:25-30), category = `BusinessConsumer`.
- **Consistency-sweep (count-sweep discipline):** register the scope in EVERY policy allowlist or `ValidatePolicy` (`VerificationSessionApplicationService.cs:357`) silently 403s:
  - `LocalDevRuntimePolicySource.cs:26` `BusinessScopes` set (`ldev_biz` :55 inherits it).
  - Any narrow seeded key a cancel test targets.
- Tenant scoping: `session.ClientApplicationId != caller.ClientApplicationId` => `FORBIDDEN_CLIENT_APPLICATION` + `SESSION_ACCESS_DENIED` audit (mirror `VerificationCompletionApplicationService.cs:51-55`).

### 2.3 Cancel = AUTHORITATIVE CAS that persists STATE + AUDIT atomically
- **LOCKED:** cancel goes through a finalization-style CAS boundary, NOT bare `SetStateAsync` (last-writer-wins would blindly overwrite an already-`Completed` session => a signed proof with a Cancelled session).
- Add `TryCancelAsync(expectedSession, cancelledSession, auditEvent)` mirroring `IVerificationFinalizationBoundary.TryFinalizeAsync` (`RepositoryPorts.cs:150-155`), SAME result enum (`NotFound/AlreadyCompleted/StateMismatch/Applied`, :138-148), in BOTH impls:
  - EF: single DB transaction (mirror `EfVerificationFinalizationBoundary.cs:16-56`) that writes `State=Cancelled` AND appends the `SESSION_CANCELLED` audit row together; terminal-state check inside the txn; `DbUpdateConcurrencyException` -> StateMismatch.
  - In-memory: CAS by whole-record equality (mirror `LocalDevInMemoryRepositories.cs:429-437`) + enqueue the audit event in the same critical section.
- **[P1] State + audit ATOMIC:** never `Cancelled`-without-audit nor audit-without-`Cancelled` (fault-injection test). Do NOT call `auditEvents.AppendAsync` as a separate step.
- **[self-check] CAS-mismatch re-classify:** on StateMismatch, RE-LOAD + re-classify: now `Completed` => 409 SESSION_TERMINAL; now `Cancelled` => idempotent 200; still non-terminal => retry the CAS ONCE (bounded).
- Transition target `Cancelled` via `WithCancellation(...)` (or `WithState(Cancelled)`, :145). **No new domain field, no `CancelledAt` column, no migration** - cancel timestamp = the audit event `OccurredAt`.

### 2.4 [P0] Append boundary must NOT revive a terminal session (the revive fix)
- **Retraction of v0.2:** an in-flight append is NOT harmless. `EfAppendIdempotencyBoundary.cs:73` currently sets `row.State = finalState` (computed from an OLD snapshot at `VerificationEvidenceApplicationService.cs:151`/`:415`) WITHOUT re-checking the current state in-transaction. So after a cancel commits, a late append can set `Cancelled -> InProgress/ReadyToComplete` and re-open the completion path. This defeats the void.
- **FIX (this slice):** the append boundary (BOTH `EfAppendIdempotencyBoundary` and the in-memory equivalent) MUST, inside the transaction/critical-section, re-read the CURRENT session state and REFUSE the append (no evidence write, no state flip) if the session is terminal (`Completed/Cancelled/Expired/TechnicalTerminal`). **An existing-key idempotent REPLAY stays a pure read (unchanged); only a NEW append re-checks + can be refused.**
- **[interface PIN - do NOT let the builder invent]:** `IAppendIdempotencyBoundary` currently returns `AppendIdempotencyApplyResult` with `AppendIdempotencyApplyStatus` (idempotency states only; no terminal, `RepositoryPorts.cs:78`). Extend `AppendIdempotencyApplyStatus` with a new value `SessionTerminal` (do NOT overload an existing idempotency status); the boundary returns `AppendIdempotencyApplyResult` with `Status = SessionTerminal` when the in-transaction terminal re-check fails; the append app service (`VerificationEvidenceApplicationService`) maps `SessionTerminal` -> `Failure("SESSION_TERMINAL", ..., 409)` (reuse the existing terminal message). This keeps the terminal signal a first-class boundary result, not an exception or an ambiguous idempotency code.
- This makes `Cancelled` (and `Expired`, `Completed`, `TechnicalTerminal`) genuinely un-revivable, closing the append-race the app-service load-time guard (`ValidateWritableAppendSession :1464`) cannot (its check is before the race window).

### 2.5 Cancel semantics (idempotent, terminal-aware)
- from `Created`/`InProgress`/`ReadyToComplete` => `Cancelled`, 200 + atomic audit.
- already-`Cancelled` => idempotent 200, NO duplicate `SESSION_CANCELLED` audit; a re-cancel with a DIFFERENT reason is STILL idempotent 200 and does NOT mutate the stored original reason (terminal state is the source of truth, not the reason).
- `Completed` => 409 `SESSION_TERMINAL` (complete won the race / already finalized); `Expired`/`TechnicalTerminal` => 409 `SESSION_TERMINAL` (reuse existing guard code/message).
- The 4 existing terminal guards (`VerificationCompletionApplicationService.cs:71`, `VerificationEvidenceApplicationService.cs:1398` + `:1464`, both finalization boundaries) are UNCHANGED except the append-boundary revive-fix in 2.4; if the terminal SET ever changes, all move in lockstep.

### 2.6 Audit record (server-side void evidence) - internal, strict-token, atomic
- Append (atomically with the state change, 2.3) an `AuditEvent` `EventType = "SESSION_CANCELLED"`; mirror the `CreateAuditEvent` factory (`VerificationSessionApplicationService.cs:374-389`); actor = the BusinessConsumer caller.
- **[P2, OQ-1 RESOLVED] Opaque reason stored as the validated strict TOKEN in `AuditEvent.EventPayloadRef`** (bounded, restricted charset, non-PII by construction, 2.1) - the server persists exactly the opaque token and NEVER parses/branches on it; it stays auditable/operator-traceable in the void evidence. Reviewer consensus: both accept a strict-token `EventPayloadRef` (Codex: hash-or-token; GPT: prefers readable strict-token for trace). **DPO-stricter fallback:** if a DPO mandates no readable reason, hash the token into `EventPayloadHash` (pattern `VerificationCompletionApplicationService.cs:157-166`) - a documented follow-up, not this slice's default.
- **[P1 - claim fixed] The audit event is the server-side void evidence, persisted in the append-only audit store (compliance-exportable). It is NOT currently returned by the evidence-ledger read** (`EvidenceLedgerDto`, `BusinessConsumerContracts.cs:84`, carries only checks/artifacts/evidence-results; the ledger builder `VerificationSessionApplicationService.cs:288` does not read audit). **The caller confirms the void via the session-state read (`GET /{id}` => `Cancelled`)** - sufficient for caller confirmation + the audit store serves compliance. Exposing cancel events in the ledger read (sanitized `sessionEvents[]`) is OPTIONAL (OQ-4), not required for G-005's server half.

## 3. STOP / NOT
- NO consumer-specific concept in the server (no "withdrawal"/"consent" in API/domain/scope) - reason is an OPAQUE strict token (stored in `EventPayloadRef`, never parsed); scope `session.cancel` generic.
- NO bare `SetStateAsync` for cancel (must be CAS, 2.3); NO cancel overwriting a `Completed` session.
- **NO reviving a terminal session via a late append (2.4)** - the append boundary re-checks terminal in-transaction.
- NO non-atomic state/audit (2.3) - one transaction.
- NO free-text / PII reason (strict token charset, 2.1); **NO server parsing/branching on reason values** (opaque; docs/examples use generic tokens like `caller_void`/`external_policy_void`, never consent-specific strings in server code). NO claim the reason is readable via evidence-ledger (it is not; caller confirms via session-state, 2.6).
- NO Idempotency-Key header for cancel (terminal-state idempotency, 2.1).
- NO new domain field / `CancelledAt` column / migration.
- NO editing the terminal-guard SET (only the append-boundary revive re-check is added); NO cross-tenant / anonymous cancel; NO signing/proof/assurance logic change.

## 4. Definition of Done
- [ ] `POST /{id}/cancel` (mirror CompleteAsync); `CancelVerificationSessionRequestDto`; reason bounded token `<=64` no-control-chars else `INVALID_CANCEL_REASON`; no Idempotency-Key header; response echoes `Cancelled` + id.
- [ ] `session.cancel` scope + `RequireBusinessCancellation` (BusinessConsumer); swept into `BusinessScopes` (LocalDevRuntimePolicySource.cs:26) + any narrow test key. Tests (ALL category-rejection cases): CaptureAgent credential => 403; TrustedAdapter credential => 403; BusinessConsumer without `session.cancel` => 403; BusinessConsumer with scope but different `clientApplicationId` => FORBIDDEN_CLIENT_APPLICATION + SESSION_ACCESS_DENIED audit.
- [ ] **[P1] Atomic CAS cancel:** `TryCancelAsync(expected, cancelled, auditEvent)` in BOTH repos writes `State=Cancelled` + `SESSION_CANCELLED` audit in ONE transaction; fault-injection test => never Cancelled-without-audit nor audit-without-Cancelled.
- [ ] **[evidence-critical] cancel-vs-complete race:** exactly one wins; a cancel NEVER overwrites a `Completed` session (test both orders); CAS-mismatch re-classify (Completed=>409, Cancelled=>200, non-terminal=>retry-once).
- [ ] **[P0] append cannot revive a terminal session:** `AppendIdempotencyApplyStatus` extended with `SessionTerminal`; append boundary re-checks current state in-transaction and returns `SessionTerminal`; app service maps => 409 SESSION_TERMINAL, no evidence write, no state flip; a NEW append onto `Cancelled`/`Completed`/`Expired`/`TechnicalTerminal` rejected. Tests: cancel-then-append in both orders, incl. append `Created->InProgress` and `InProgress->ReadyToComplete` after cancel => rejected. Existing-key idempotent replay unchanged (pure read). Existing append happy-path (non-terminal) still works.
- [ ] Semantics: non-terminal => 200 Cancelled; already-Cancelled => idempotent 200; Completed/Expired/TechnicalTerminal => 409 SESSION_TERMINAL. Tests each.
- [ ] **[P2] Reason = validated strict token in `EventPayloadRef`** (no free-text/PII; server never parses/branches - grep: no `if (reason ==`); caller confirms void via `GET /{id}` => `Cancelled`. Test: malformed/over-long reason => `INVALID_CANCEL_REASON`.
- [ ] Post-cancel: `complete` => 409; capture-artifacts/evidence-results append => 409 (2.4). No migration / no new column / no domain field (grep). Existing terminal guards untouched except 2.4 revive re-check.
- [ ] dotnet build + dotnet test green (env-fixture reds isolated); no signing/proof logic change.

## 5. Review tier & attacks
Tier-1 (evidence integrity + authz). Attacks: (a) cancel overwrite a Completed session? (b) cancel-vs-complete deterministic, no signed-proof-with-cancelled-session? (c) **late append revive a Cancelled session (the P0)?** (d) state/audit non-atomic (Cancelled-without-audit)? (e) new scope missing from an allowlist? (f) cross-tenant/anonymous cancel? (g) consumer term ("withdrawal"/"consent") leak? (h) caller plaintext reason / PII persisted? (i) migration or terminal-guard drift? (j) is a cancelled session truly un-completable AND un-appendable afterwards?

## 6. Open questions
- OQ-1: RESOLVED = reason = validated strict TOKEN in `EventPayloadRef` (reviewer consensus; non-PII by strict charset; server never parses). DPO-stricter fallback = `EventPayloadHash` (documented follow-up if a DPO forbids a readable reason).
- OQ-2: RESOLVED = idempotency by terminal-state (already-Cancelled => 200, no duplicate audit, reason-mismatch stays 200 not 409). The endpoint IGNORES any `Idempotency-Key` header COMPLETELY - not read, logged, validated, stored, or used for correlation in this slice; correlation is via `RequestId`/`CorrelationId`.
- OQ-3: RESOLVED = cancel on `Created` (zero evidence) allowed => `Cancelled` (generic void from any non-terminal state).
- OQ-4: DEFER (non-blocking) = do NOT expose cancel events in the evidence-ledger read (sanitized `sessionEvents[]`) in this slice; session-state read (`GET /{id}` => `Cancelled`) + the audit store are sufficient for G-005. Revisit if real-customer evidence later needs caller-readable void events.

## 7. Companion / boundary
- Agent half = **TIP-82W (DONE, pushed)** - agent aborts in-capture on withdrawal; TIP-82S is the SERVER half that voids the session so partial evidence cannot be completed OR revived.
- **This slice provides the GENERIC capability only** - the consumer-side wiring (SignFlow calling `/cancel` after a withdrawal-after-partial, from its consent projection) is the consumer's job, out of scope here.
- **G-005 closure precision:** server-half = TIP-82S (un-completable + un-revivable + recorded); full end-to-end = TIP-82W + TIP-82S + the consumer invoking `/cancel`. G-005 closes once the server capability + consumer wiring are committed (or a documented DPO risk-acceptance if a piece slips).
