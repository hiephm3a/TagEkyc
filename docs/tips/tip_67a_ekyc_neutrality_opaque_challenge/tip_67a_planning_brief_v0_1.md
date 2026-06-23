# TIP-67A eKYC Neutrality — Opaque-Challenge Profile — Planning Brief v0.6

**File:** `docs/tips/tip_67a_ekyc_neutrality_opaque_challenge/tip_67a_planning_brief_v0_1.md`
**Version:** 0.6
**Status:** Draft — implementation planning (architecture course-correction; refactor of shipped behavior); GPT/Codex review round 5 patches applied; pending convergence
**Date:** 2026-06-22
**Baseline:** `e8cf836` (master).
**Decision basis:** `docs/adr/ekyc_neutrality_decision_v0_1.md` (Accepted 2026-06-22). This is the FIRST of a 2-TIP split: **67A** neutralizes the profile (this TIP); **67B** adds the neutral verifiable proof + view (depends on 67A).
**Purpose:** Remove the SignFlow-coupling drift from the eKYC core: reframe `TRANSACTION_BOUND_EKYC_PROFILE` into a generic **opaque-challenge** model, treat `externalTransactionId`/`externalSessionId`/`bindingNonceHash` as an opaque challenge / client-correlation **echoed, not interpreted**, and echo the challenge verbatim in the result. No signing change, no public verification surface (those are 67B).

## Changelog
### v0.6 — GPT/Codex review round 5 (2026-06-22)
- Pinned the request field-alias mechanism (custom request converter/legacy-input DTO — STJ can't multi-name) + conflict rule; pinned wire vs persisted canonical forms (snake-upper wire / PascalCase enum-name row).

### v0.5 — GPT/Codex review round 4 (2026-06-22)
- Profile-ONLY wire converter (NOT global — avoids mutating every enum); request field-key input aliases (`bindingNonceHash`/`externalTransactionId`); testable `Challenge` string-safety (≤128, no C0/C1, exact echo); scope floor + the old profile-naming debt row reconciled to the neutral model.

### v0.4 — GPT/Codex review round 3 (2026-06-22)
- Extended neutralization to the CORE (code-grounded review): pinned the profile wire format (default converter = PascalCase → custom converter for snake-upper + legacy-accept + output-canonicalization), renamed the policy flag + error codes, pinned `Challenge` max length ≤ 128, fixed DoD wording.

### v0.3 — GPT/Codex review round 2 (2026-06-22)
- `Challenge` made TRULY opaque (string, no `sha256:` requirement — that imposed eKYC meaning, violating neutrality); legacy sha256 still valid; non-hash challenge test. Pinned exact echo surfaces + roles (Challenge = only anti-substitution value). Title/version drift fixed (v0.3).

### v0.2 — GPT/Codex review round 1 (2026-06-22)
- PINNED the names (no more "converge in review", since 67A freezes 67B's contract): profile `CHALLENGE_BOUND_EKYC_PROFILE`; fields `Challenge`/`ClientReference`/`ExternalSessionId`. Compat strengthened to cover the **persisted row string** (tolerant-parse `TransactionBoundEkycProfile`), not just the wire value (`DomainRowMapper` persists `Profile.ToString()`); EF columns kept (no migration); legacy-row read test required.

### v0.1 — Initial planning brief
- Opened from the eKYC neutrality ADR. 67A = the shipped-code refactor half of the split (67A/67B) so a single TIP does not both refactor shipped architecture and open a new public verification surface.

## 0. Problem
TagEkyc absorbed SignFlow's "transaction" concept: `TRANSACTION_BOUND_EKYC_PROFILE` + `externalTransactionId` + mandatory binding validation (`VerificationSessionApplicationService.cs`, `VerificationSession.cs` domain invariant). Per the ADR, binding is the client's job and the challenge must be opaque to eKYC — so the eKYC core must NOT carry "transaction" semantics. It also does not echo the challenge back (the client cannot do challenge-response anti-tamper without it).

## 1. Scope (this slice)
- **Neutralize the profile (PINNED):** `TRANSACTION_BOUND_EKYC_PROFILE` → **`CHALLENGE_BOUND_EKYC_PROFILE`** (domain `ChallengeBoundEkycProfile`). `STANDARD_EKYC_PROFILE` stays. Compat covers BOTH the wire value AND the persisted row string.
- **Opaque challenge / client refs (PINNED):** `BindingNonceHash` → **`Challenge`** — a **truly opaque string** (no `sha256:` requirement; `HashRef`→`string`; max length ≤ 128); `ExternalTransactionId` → **`ClientReference`**; `ExternalSessionId` stays — opaque, **echoed, never interpreted**. Domain/DTO renamed; EF columns kept (mapper aliases) → no migration. `Challenge` is the ONLY anti-substitution value; the others are correlation echoes only.
- **Neutralize the CORE (not just DTO):** rename the policy flag `AllowsTransactionBoundProfile → AllowsChallengeBoundProfile` + the error codes (`CHALLENGE_BOUND_NOT_ALLOWED`/`MISSING_CHALLENGE`/`INVALID_CHALLENGE`/`CHALLENGE_TOO_LONG`); pin the profile **wire format** (default `JsonStringEnumConverter` emits PascalCase → a custom converter is needed for snake-upper, + legacy-accept on input + canonicalize output).
- **Echo the challenge** verbatim in the completion result / session response (so the client can verify challenge-sent == challenge-received). Echo is in the response DTO ONLY — NOT in the hash graph (manifestHash unchanged; signing the challenge is 67B).
- **Compatibility:** this changes shipped behavior (TIP-08 transaction-bound profile). Provide a back-compat path (accept the old profile name as an alias / version the profile; existing transaction-bound sessions keep working as opaque-challenge sessions). No data loss.
- Update `signflow_integration_contract` + `lld_01`/`lld_03` to the neutral model; register the drift as resolved.

## 2. Non-goals (out of scope → 67B / later)
The neutral signed proof + verification view + public key exposure + verifier contract (TIP-67B); document/transaction binding (the client's job — never eKYC's); any change to the evidence hash graph / `manifestHash` / signing (67B); production crypto. Vault TTL (E-Q03, legal); third-party data-controller (E-Q04).

## 3. Key design decisions (PINNED — names frozen for 67B)
- **Challenge is opaque:** an opaque client string — eKYC validates presence/length/string-safety ONLY, with NO `sha256:`/hash assumption, and assigns NO meaning. Legacy sha256 values stay valid.
- **Profile model:** `STANDARD_EKYC_PROFILE` (no challenge) + `CHALLENGE_BOUND_EKYC_PROFILE` (opaque challenge required). Old `TRANSACTION_BOUND_EKYC_PROFILE` (wire + persisted row string) maps to CHALLENGE_BOUND for compat.
- **Echo, don't bind:** eKYC returns the challenge; the client binds.
- **No hash/golden change in 67A** — echo lives in the response DTO, not the manifest body. STANDARD + (existing) transaction-bound golden vectors byte-identical.

## 4. Acceptance criteria (DoD summary)
Full DoD in kickoff. Summary: the eKYC core has NO "transaction" concept (profile + fields are opaque-challenge/client-ref); the challenge is echoed verbatim in the result; existing transaction-bound sessions still work (compat path) with no data loss; `manifestHash`/golden vectors byte-identical (no hash/signing change); docs (`signflow_integration_contract`/`lld_01`/`lld_03`) reflect the neutral model; full `dotnet test` green. Output = a frozen neutral challenge/profile contract that is the INPUT to TIP-67B.

## 5. STOP/RRI
- Any change to the evidence hash graph / `manifestHash` / signing → STOP (that is 67B).
- Any new public verification surface (JWS/JWK/view) → STOP (67B).
- Breaking existing transaction-bound sessions with no compat path → STOP.
- Re-introducing any client-specific (transaction/document) interpretation into the eKYC core → STOP.

## 6. Recommended next action
Review + converge 67A (freeze the opaque-challenge/profile contract — it is 67B's input). Build 67A. Then 67B (neutral verifiable proof) builds on the frozen contract.
