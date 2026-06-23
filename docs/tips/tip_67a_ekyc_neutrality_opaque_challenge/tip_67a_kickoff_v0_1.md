# TIP-67A eKYC Neutrality — Opaque-Challenge Profile — Kickoff v0.6

**File:** `docs/tips/tip_67a_ekyc_neutrality_opaque_challenge/tip_67a_kickoff_v0_1.md`
**Version:** 0.6
**Status:** Draft — dispatch-ready spec (architecture course-correction; refactor of SHIPPED behavior); GPT/Codex review round 5 patches applied; pending convergence before build
**Date:** 2026-06-22
**Baseline:** `e8cf836` (master). Planning brief: `tip_67a_planning_brief_v0_1.md` v0.6. Decision: `docs/adr/ekyc_neutrality_decision_v0_1.md`.
**Purpose:** Execution contract to neutralize the eKYC↔client boundary: reframe the transaction-bound profile into a generic opaque-challenge model, treat the binding fields as opaque/echoed-not-interpreted, and echo the challenge in the result — with a back-compat path for shipped sessions. NO signing / hash / public-verification change (those are 67B).

## Changelog
### v0.6 — GPT/Codex review round 5 (2026-06-22)
- P1: pinned the request field-alias MECHANISM — `System.Text.Json` can't map two JSON names to one property via `[JsonPropertyName]`, so use a custom request converter / legacy-input DTO; added the conflict rule (`CONFLICTING_CHALLENGE_FIELDS` when new + legacy key differ).
- P2: pinned TWO distinct canonical forms by layer — wire = snake-upper `CHALLENGE_BOUND_EKYC_PROFILE`; DB row = PascalCase enum-name `ChallengeBoundEkycProfile` (don't change the persistence string).

### v0.5 — GPT/Codex review round 4 (2026-06-22)
- P1: wire converter is **profile-ONLY** (`[JsonConverter]` on `VerificationProfileDto`), NOT a global naming change (the global `JsonStringEnumConverter` would mutate every enum's wire) — added to scope floor + STOP.
- P1: **request field-key compat** — `Challenge`/`ClientReference` accept the old JSON keys (`bindingNonceHash`/`externalTransactionId`) via input alias; old-profile+old-field-keys → neutral-out test (profile alias alone didn't cover field renames).
- P2: `Challenge` string-safety made testable (non-empty, ≤128 .NET chars, reject C0/C1, no trim/normalize, exact echo); scope floor updated to the v0.4+ surfaces; fixed the legacy value `Standard` → `StandardEkycProfile`.

### v0.4 — GPT/Codex review round 3 (2026-06-22)
- P1: pinned the profile **wire format** — the API uses the default `JsonStringEnumConverter` (PascalCase), so snake-upper `CHALLENGE_BOUND_EKYC_PROFILE` needs a custom converter; pinned + accept legacy on input + canonicalize output (§3.1).
- P1: "no transaction concept" extended to the CORE — rename the policy flag `AllowsTransactionBoundProfile → AllowsChallengeBoundProfile` and the error codes (`CHALLENGE_BOUND_NOT_ALLOWED`/`MISSING_CHALLENGE`/`INVALID_CHALLENGE`/`CHALLENGE_TOO_LONG`) + lld_03 registry (§3.4).
- P2: `Challenge` max length pinned ≤ 128 (matching the kept column) + fail-before-persist (§3.2); DoD wording fixed (presence/length/string-safety, not "presence/format only").
- P2: compat OUTPUT canonicalization (old value input-only; responses/new rows emit the neutral value).

### v0.3 — GPT/Codex review round 2 (2026-06-22)
- P1: `Challenge` is now a TRULY opaque string — removed the `sha256:` requirement (that imposed eKYC meaning on the challenge, violating neutrality); validate presence/length/string-safety only; legacy sha256 values still accepted; non-hash challenge test. Type `HashRef` → `string` (§3.2).
- P1: pinned the EXACT echo surfaces (Create/Summary/Complete response DTOs MUST echo; EvidencePackageSummaryDto MAY defer to 67B; VerificationCompletedEventDto notification-only) + contract tests (§3.3).
- P3: roles clarified — `Challenge` = the only anti-substitution value; `ClientReference`/`ExternalSessionId` = correlation echoes only.
- P2: title/baseline version drift fixed (v0.3; planning brief ref v0.3).

### v0.2 — GPT/Codex review round 1 (2026-06-22)
- P1: names PINNED (§3.1/§3.2) — profile `CHALLENGE_BOUND_EKYC_PROFILE`/`ChallengeBoundEkycProfile`; fields `Challenge`/`ClientReference`/`ExternalSessionId`; 67A freezes 67B's contract so no "converge in review".
- P1: compat now covers the **persisted row string** (tolerant-parse `TransactionBoundEkycProfile` on load, not raw `Enum.Parse`) + a legacy-row read test, not just the wire value; EF columns kept (no migration).

## 1. Objective
Make the eKYC core neutral: no "transaction" concept; an opaque `challenge` the client supplies and eKYC echoes; binding stays the client's job. Freeze the opaque-challenge/profile contract — it is TIP-67B's input.

## 2. Source of truth + impacted surfaces (read first)
- `src/TagEkyc.Domain/` — `VerificationProfile` (the `TransactionBoundEkycProfile` value), `VerificationSession.cs` (`ExternalSessionId`/`ExternalTransactionId`/`BindingNonceHash` + the domain invariant at ~`:112-122`).
- `src/TagEkyc.Contracts/Common/ContractEnums.cs` — `VerificationProfileDto`.
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs` — creation-time profile + binding validation (~`:63-88`) + the error codes (`TRANSACTION_BOUND_NOT_ALLOWED`/`MISSING_TRANSACTION_BINDING`/`INVALID_BINDING_NONCE_HASH`) to rename.
- `src/TagEkyc.Application/VerificationSessions/LocalDevClientPolicy.cs` + `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs` — `AllowsTransactionBoundProfile` (rename `→ AllowsChallengeBoundProfile`).
- `src/TagEkyc.Api/Program.cs` — the `JsonStringEnumConverter` (pin the profile wire format).
- `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` — `BindingNonceHash` column `HasMaxLength(128)` (the Challenge length bound).
- The completion result / session response DTOs + their projections — where the challenge must be **echoed**.
- `docs/signflow_integration_contract_v0_1.md`, `docs/lld_01_data_model_v0_1.md`, `docs/lld_03_api_contracts_v0_1.md` — update to the neutral model.
- `src/TagEkyc.SignFlow/` (`SigningAuthorizationBindingPlaceholder`) — read; reconcile naming with the opaque-challenge model.

## 3. Scope
### 3.1 Neutralize the profile (names + wire format PINNED)
- Domain enum: `VerificationProfile.TransactionBoundEkycProfile` → **`VerificationProfile.ChallengeBoundEkycProfile`**. DTO enum `VerificationProfileDto` → **`ChallengeBoundEkycProfile`**. `Standard` unchanged.
- **Wire format (PINNED — code-grounded):** the API registers `JsonStringEnumConverter` **GLOBALLY** (`Program.cs`), so the default wire is C# PascalCase (`TransactionBoundEkycProfile`/`StandardEkycProfile`), NOT the snake-upper the docs promise. 67A pins a **profile-ONLY converter** — a `[JsonConverter]` on `VerificationProfileDto` (NOT a global naming-policy change) — that EMITS the canonical neutral `CHALLENGE_BOUND_EKYC_PROFILE`/`STANDARD_EKYC_PROFILE` and PARSES legacy on input (`TRANSACTION_BOUND_EKYC_PROFILE` + the PascalCase `TransactionBoundEkycProfile`/`StandardEkycProfile` forms). **Do NOT change the global enum naming** — that would mutate every other enum's wire (`RequiredCheckTypeDto`/`VerificationResultDto`/status…) = defect → STOP.
- **Compat — input AND output (CRITICAL):**
  - INPUT: accept the old wire value (`TRANSACTION_BOUND_EKYC_PROFILE` and `TransactionBoundEkycProfile`) AND **tolerant-parse the old PERSISTED row string `TransactionBoundEkycProfile`** → `ChallengeBoundEkycProfile` on load (custom alias map, NOT raw `Enum.Parse`).
  - OUTPUT canonicalization — TWO distinct canonical forms by layer (PINNED): the **WIRE** emits snake-upper `CHALLENGE_BOUND_EKYC_PROFILE` (via the profile converter); the **DB ROW** persists the enum NAME `ChallengeBoundEkycProfile` (PascalCase, via `Enum.ToString()` — do NOT change the persistence string format). The old value is accepted as INPUT ONLY (never echoed on the wire / persisted back).
  - Tests required: **legacy-row read** + **old-wire-in → neutral-out** (no data loss).

### 3.2 Opaque challenge / client refs (names + opacity PINNED; echoed, not interpreted)
- Domain/DTO property renames (PINNED): `BindingNonceHash` → **`Challenge`** — an **opaque client-supplied string** (type changes `HashRef` → `string`). Validate ONLY: presence, **max length ≤ 128** (matching the kept `BindingNonceHash` column `HasMaxLength(128)`; fail closed BEFORE persistence with a `CHALLENGE_TOO_LONG` error), UTF-8/string-safety, no control chars. It **MUST NOT require a `sha256:` prefix or assume the value is a hash** (challenge may be a binding nonce, shift code, case id… — eKYC assigns no meaning/format). `ExternalTransactionId` → **`ClientReference`** (opaque string); `ExternalSessionId` stays (opaque client-session correlation).
- **String-safety rule (testable):** `Challenge` MUST be non-empty (exact-string presence), ≤ 128 .NET chars, and MUST reject C0/C1 control characters. Do NOT trim/normalize/upper/lower-case it — echo the EXACT stored value. Non-control Unicode is allowed unless a later security review narrows it.
- **Request field compat (INPUT) — mechanism PINNED:** `[JsonPropertyName]` CANNOT map two JSON names to one property in `System.Text.Json`, so use a **custom request converter / legacy-input DTO mapped in the application layer** that reads EITHER the new key (`challenge`/`clientReference`) OR the legacy key (`bindingNonceHash`/`externalTransactionId`). **Conflict rule:** if both the new and legacy key are present and DIFFER → fail closed (`CONFLICTING_CHALLENGE_FIELDS`); if both present and equal → accept; the new key wins. Responses emit the new neutral keys only. Tests: old request (old profile value + old field keys) → neutral output; conflicting keys → fail closed.
- **Legacy:** existing `sha256:`-prefixed values remain valid challenge strings; the prefix is no longer a semantic requirement for new challenge-bound requests. Add a **non-hash challenge test** (a plain opaque string is accepted + echoed).
- **Roles (P3-01):** `Challenge` is the ONLY anti-substitution challenge-response value (client checks sent==received). `ClientReference`/`ExternalSessionId` are optional correlation echoes only — NOT proof/binding unless TIP-67B explicitly signs them.
- **Persistence (no column migration):** keep the existing EF columns (`BindingNonceHash`/`ExternalTransactionId`/`ExternalSessionId`) as strings; map the renamed domain properties to them in `DomainRowMapper` (`Challenge` ↔ `BindingNonceHash` column, `ClientReference` ↔ `ExternalTransactionId` column). No column migration. (Optional column rename = later hygiene debt.)
- Validation = presence/string-safety ONLY for the challenge-bound profile; never interpret. Preserve the fields through the session lifecycle.

### 3.3 Echo the challenge (exact surfaces PINNED)
- Echo `Challenge` + `ClientReference` (when provided) **verbatim** on:
  - `CreateVerificationSessionResponseDto` — MUST echo.
  - `VerificationSessionSummaryDto` — MUST echo.
  - `CompleteVerificationSessionResponseDto` — MUST echo.
  - `EvidencePackageSummaryDto` — MAY defer the Challenge echo to TIP-67B's verification view (not required here).
  - `VerificationCompletedEventDto` — stays **notification-only** (NOT changed here; if ever changed, documented as non-proof).
- **Contract tests** MUST assert exact field presence AND that the echoed challenge is byte/string-identical to the request value.
- **Echo is in response DTOs ONLY** — it MUST NOT enter the manifest body / hash graph (signing the challenge is TIP-67B). `manifestHash`/`packageHash`/`manifestBodyHash` golden vectors stay byte-identical.

### 3.4 Neutralize policy + error codes (the CORE, not just the DTO)
- Policy flag `ILocalDevClientPolicyProvider.AllowsTransactionBoundProfile` (`LocalDevClientPolicy`, `LocalDevRuntimePolicySource`) → **`AllowsChallengeBoundProfile`** (rename; same semantics). No "transaction" in the policy.
- Error codes (`VerificationSessionApplicationService`): `TRANSACTION_BOUND_NOT_ALLOWED` → **`CHALLENGE_BOUND_NOT_ALLOWED`**; `MISSING_TRANSACTION_BINDING` → **`MISSING_CHALLENGE`**; `INVALID_BINDING_NONCE_HASH` → **`INVALID_CHALLENGE`**; add **`CHALLENGE_TOO_LONG`** and **`CONFLICTING_CHALLENGE_FIELDS`** (new + legacy key differ). Update the `lld_03` error registry. No external consumer (SignFlow adapter) exists yet → this is a pre-integration rename: document the old→new mapping; no runtime deprecated-alias needed unless a live consumer is found.

### 3.5 Docs
- Update `signflow_integration_contract` (neutral challenge-response: client sends opaque challenge → eKYC echoes; client binds via `H(challenge ‖ document_hash ‖ result_hash)`), `lld_01`/`lld_03` (rename + opaque semantics + echo field). Mark the SignFlow-coupling drift resolved.

## 4. Out of scope (do not touch)
The signed proof / verification view / public key exposure / verifier contract (TIP-67B); the evidence hash graph / `manifestHash` / signing; document/transaction binding (client's job); production crypto; vault TTL (E-Q03); lld_02/04, hld.

## 5. Definition of Done (verifiable)
- [ ] eKYC core has NO "transaction" concept: the profile, fields, **policy flag** (`AllowsChallengeBoundProfile`), AND **error codes** (`CHALLENGE_BOUND_NOT_ALLOWED`/`MISSING_CHALLENGE`/`INVALID_CHALLENGE`/`CHALLENGE_TOO_LONG`) are neutral; validation is presence / max-length(≤128) / UTF-8-string-safety / no-control-character only — **no hash/prefix/semantic format requirement**, never interpretation.
- [ ] **Wire + field compat (profile-ONLY converter):** the profile wire value is the canonical neutral form via a **profile-scoped** converter (NO global enum-naming change); old wire/persisted values AND old request field keys (`bindingNonceHash`/`externalTransactionId`) are accepted on INPUT; all responses + new rows emit the neutral value/keys (**old-profile + old-field-keys → neutral-out test**). `Challenge` string-safety (≤128, no C0/C1, no trim/normalize, exact echo) tested.
- [ ] `Challenge` is an **opaque string** (no `sha256:` requirement) — a **non-hash challenge** is accepted + echoed (test). The challenge is echoed verbatim on `CreateVerificationSessionResponseDto` / `VerificationSessionSummaryDto` / `CompleteVerificationSessionResponseDto` (contract tests assert exact field presence + byte/string-identical echo); NOT in the hash graph.
- [ ] **Back-compat (wire AND persisted):** the old `TRANSACTION_BOUND_EKYC_PROFILE` wire value still works; a persisted row stored as `TransactionBoundEkycProfile` loads as `ChallengeBoundEkycProfile` (**legacy-row read test**); renamed domain properties map to the EXISTING columns (no column migration); no data loss.
- [ ] **`manifestHash`/`packageHash`/`manifestBodyHash` golden vectors byte-identical** (67A changes no hash/signing).
- [ ] `signflow_integration_contract` + `lld_01` + `lld_03` reflect the neutral model; drift marked resolved; the opaque-challenge/profile contract is documented as **frozen** (67B's input).
- [ ] Full `dotnet test` green (report counts).

## 6. Scope floor (anti-creep)
- Allowed ONLY: the profile rename + a **profile-ONLY** wire converter (NOT global enum naming) + alias; the opaque-challenge/client-ref reframe (semantics + naming) + challenge length/string-safety validation; the response-DTO echo; the compat path for old wire/persisted values + old request field keys (input aliases) + output canonicalization; the policy-flag rename; the error-code rename + `lld_03` error registry; the 3 docs. Anything touching the hash graph / signing / a new verification surface / the GLOBAL enum converter / client-specific interpretation = defect → STOP.

## 7. STOP/RRI
- Hash graph / manifestHash / signing change → STOP (67B).
- New public verification surface (JWS/JWK/view) → STOP (67B).
- No compat path for shipped transaction-bound sessions → STOP.
- Any transaction/document interpretation re-introduced into eKYC core → STOP.

## 8. Validation + report
- Run `dotnet build`; `dotnet test TagEkyc.sln`; confirm golden vectors unchanged; `git diff --stat`.
- Do NOT commit (await Contractor spot-check).
- Report: (a) the rename + alias (compat proof); (b) the opaque-challenge/client-ref reframe; (c) the echo (where, + test); (d) golden-unchanged proof; (e) docs updated; (f) the frozen opaque-challenge/profile contract for 67B; (g) full test counts.

## 9. Review after build (Contractor)
Contractor adversarial spot-check: on CODE — confirm no "transaction" interpretation remains in the eKYC core; the challenge is opaque + echoed (not in the hash); the compat alias works (old sessions unbroken); golden vectors byte-identical (no hash/signing change); docs neutral + drift resolved; the opaque-challenge contract is clearly frozen for 67B. Then closeout (playbook structure). Then dispatch 67B.
