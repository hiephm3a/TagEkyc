# TIP-80S-L — Pre-Complete Evidence Ledger — BUILD DISPATCH (for Codex)

**Brief (build-locked, approved):** `docs/tips/tip_80s_append_idempotency_evidence_ledger/tip_80s_planning_brief.md` — build EXACTLY the **TIP-80S-L** section. **Do NOT build TIP-80S-I** (idempotency) — it is NOT ready (needs a companion agent slice; server-only dedup would be inert).
**Repo:** `D:\Task\Remote Signing\TagEkyc` (server). **Baseline:** `6b7dade`. Agent repo is OUT of scope.
**Tier:** Tier-1 (evidence integrity / trust separation). Small, read-only slice — build tight.

## WHAT TO BUILD (TIP-80S-L only)
New endpoint **`GET /api/ekyc/verification-sessions/{id}/evidence-ledger`**, **`business.session.read`** scope, returning a sanitized ledger DTO:
- `verificationSessionId`, `state`, and **TWO distinct booleans (do NOT collapse to one)**:
  - `evidenceCompleteEligible` — mirror ONLY `CompleteAsync`'s **evidence** precondition (`VerificationCompletionApplicationService.cs:79-112`): every required check has a latest evidence with non-null `payloadHash` and result ≠ `NotSupported`. TRUE even for a fail/review verdict (that session IS completable). **Name it accurately — it does NOT cover the state/expiry/client-auth checks Complete runs first (`:57-77`); it is the evidence-readiness signal, NOT a "Complete will succeed" guarantee.** Caller still checks `state`/expiry and handles Complete errors.
  - `allRequiredChecksPassed` — mirror `IsReadyToComplete` (`VerificationEvidenceApplicationService.cs:1366`): every required check's latest == `Passed`.
  - Exposing only one `readyToComplete` (all-Passed) would strand fully-evidenced fail/review sessions — forbidden.
- `requiredChecks[]`: `checkType`, `submissionStatus` (`missing`|`submitted`), `result` = **`VerificationResultDto?`** (the REAL enum in `ContractEnums.cs:64` — `NotAvailable, Passed, RetryRequired, FailedCaptureQuality, FailedIdentity, ReviewRequired, TechnicalError, NotSupported`; NULL when missing — do NOT invent `Failed`/`NotApplicable`), `currentEvidenceResultId`, `payloadHash`, `createdAt` — **computed with the SAME latest-per-check selection Complete uses** (`VerificationCompletionApplicationService.cs:93/514`), NOT "any Passed evidence exists".
- `acceptedCaptureArtifacts[]`: `captureArtifactId`, `artifactType`, `artifactHash`, `createdAt` (from `ICaptureArtifactRepository.ListBySessionAsync`).
- `acceptedEvidenceResults[]`: `evidenceResultId`, `resultType`, `result`, `payloadHash`, `createdAt` (from `IEvidenceResultRepository.ListBySessionAsync`).
- Duplicates: list ALL accepted submissions grouped by check/type, PLUS a server-selected `currentEvidenceResultId` per check using Complete's latest-selection rule, so the caller can see "FaceMatch A vs B" and know which one Complete would use.

## HARD REDLINES (defect if violated)
1. **Pure read (no business writes)** — an authorized successful ledger read MUST NOT mutate session state, run completion, append artifacts/evidence, or change the evidence package (test asserts session state/completion byte-identical before==after). **Exception, allowed & desired:** a DENIED cross-tenant/unauthorized read MAY emit the EXISTING sanitized security audit (`SESSION_ACCESS_DENIED`, `VerificationSessionApplicationService.cs:195`) — that is correct security behavior, not a forbidden side effect. Do NOT add any NEW write beyond reusing that existing denied-access audit.
2. **Latest-selection parity** — check status MUST match Complete's latest-per-check logic (`LatestEvidenceByRequiredCheck`, `VerificationCompletionApplicationService.cs:514`, plus the `IsReadyToComplete` logic at `VerificationEvidenceApplicationService.cs:~1353`). A stale `Passed` followed by a newer `RetryRequired`/`ReviewRequired`/`FailedIdentity` MUST show that newer result, never green. The helper is currently **private and duplicated across two services** — **extract ONE pure shared helper with NO behavior change** (preferred, avoids drift), or, only if extraction is infeasible, copy it and add parity tests asserting the ledger and Complete pick the identical evidence per check. Do NOT re-implement a looser variant.
3. **Business-read scope only** — `business.session.read`. Capture-agent / trusted-adapter / cross-tenant callers MUST be rejected via the existing access path. The agent must NOT be able to read this.
4. **Sanitized only** — ids + hashes + sanitized enums. NO PII / biometric / vault-ref / raw-path / plaintext-identity (same rules as existing DTOs).
5. **No proof / manifest / completion / decision-basis change** — this is a read projection over existing data. No changes to NFC/FaceMatch/Liveness `Prepare*` or completion/signing.
6. **No idempotency work** — do not touch the append endpoints or add dedup here. That is TIP-80S-I, explicitly deferred.

## HONEST-CLAIM BOUNDARY (put in code comments / PR text, do not overstate)
TIP-80S-L only enables server-side pre-Complete reconciliation. It does NOT provide append idempotency, lost-response retry safety, or anti-double-submit. Production-safe submit-only retry stays blocked until TIP-80S-I + the companion agent slice ship.

## DEFINITION OF DONE (brief §L.4 — all boxes)
- [ ] Endpoint returns per-check status via Complete's latest-selection (test: stale-`Passed`-then-newer-`RetryRequired`/`ReviewRequired`/`FailedIdentity` ⇒ shows the newer result, NOT green); `allRequiredChecksPassed` matches `IsReadyToComplete`; `evidenceCompleteEligible` matches `CompleteAsync`'s evidence-gate (test: fully-evidenced fail case — all checks present, latest `FailedIdentity` ⇒ `evidenceCompleteEligible=true`, `allRequiredChecksPassed=false`).
- [ ] Duplicates surfaced with `currentEvidenceResultId` per check (test with a pre-existing duplicate row).
- [ ] Business-read scope enforced; agent/adapter key + cross-tenant ⇒ existing access-denied (test); sanitized (grep/test — no PII).
- [ ] Read-only: state/completion unchanged by a ledger call (test).
- [ ] TIP-04/05/06/08 tests green; no proof/completion/decision-basis change.
- [ ] `dotnet test` green (if the full-solution run shows unrelated shared-Postgres schema-contention reds, run the affected project(s) in isolation and report).

## COMMIT / PUSH DISCIPLINE
- **Do NOT commit until I authorize.** Then commit **by explicit file allowlist** (report the list first).
- EXCLUDE: `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, `bin`, `obj`, `local_models`, `local_datasets`, `*.onnx`; `docs/reviews/**` stays local. Leave the pre-existing server dirty files alone unless they are part of this slice.
- **NEVER push.** No real PII/biometric in code/tests/fixtures/logs.

## REPORT BACK (for adversarial verify on CODE)
Files changed; exact test names per DoD box (esp. latest-selection-not-green, read-only-state-unchanged, scope/cross-tenant reject, sanitization); `dotnet test` result; any deviation + why. I verify on the code, not test-green.
