# ADR — eKYC Neutrality (opaque challenge, client-binds)

**File:** `docs/adr/ekyc_neutrality_decision_v0_1.md`
**Version:** 0.1
**Status:** Accepted (Homeowner, 2026-06-22)
**Implemented by:** TIP-67A (neutralize profile) → TIP-67B (neutral verifiable proof).
**Source:** a draft vision written during SignFlow work, not reviewed when TagEkyc was built — `D:\Task\Remote Signing\Codex_SignFlow\docs\Idie\vision_neutral_ekyc_service_v0_1.md` (E-D01..E-D04, E-Q01..E-Q05). That doc is a SignFlow-repo draft and is NOT TagEkyc governance; this ADR is TagEkyc's own record.

## Context
TagEkyc drifted toward SignFlow-coupling: it has a `TRANSACTION_BOUND_EKYC_PROFILE` + `externalTransactionId` + mandatory binding validation — i.e. it absorbed SignFlow's "transaction" concept into the eKYC core. It also does not echo/sign the client's challenge. The neutral-eKYC vision (above) was never reviewed during the TagEkyc build. TIP-67 v0.3 (transaction-bound verifiable evidence) would have DEEPENED the drift by making eKYC cryptographically commit SignFlow's transaction binding.

## Decision
eKYC is an **independent, neutral identity-assurance service** shared by multiple internal products (SignFlow is just ONE client). It keeps exactly one job: **prove its own authenticity** — "this subject is real, matches identity X, at time T, at assurance Y" — and **echo the client's opaque challenge** verbatim.

- **E-D02 — opaque challenge:** the client passes a `challenge` (an opaque value — e.g. SignFlow's binding nonce, a shift code, a case id). eKYC does NOT interpret it; it echoes it back and (TIP-67B) signs it into the proof.
- **E-D03 — binding is the client's job:** eKYC does NOT bind its result to a document/transaction/context. The client (SignFlow) builds its own `AuthorizationBindingDigest = H(challenge ‖ document_hash ‖ resultHash)` (canonical proof field `resultHash`; `result_hash` is formula notation only). Splitting eKYC out moves binding to the right place; it does not remove it.
- **E-D01 / E-D04:** neutral + reusable; self-built internal ⇒ consent is one-place (architectural, not in code).

## Consequences
- **Neutralize the profile (TIP-67A):** `TRANSACTION_BOUND_EKYC_PROFILE` → a generic challenge-bound profile; `externalTransactionId`/`externalSessionId`/`bindingNonceHash` → opaque `challenge` / client-correlation refs, **echoed, not interpreted**. No "transaction" concept in the eKYC core.
- **Neutral verifiable proof (TIP-67B):** eKYC signs a proof `{sessionId, identityRef, result, assuranceLevel, requiredChecks, completedChecks, evidenceEngines[], achievedAssurance, timestamp, challenge, signedManifestHash, resultHash}` and exposes a verifiable view + public key; the client verifies (against an out-of-band trust anchor) and does its own binding to the signed `resultHash`.
- **Provider model (E-Q05):** SignFlow's `IEkycEngine` is a per-provider abstraction; a TagEkyc-specific adapter is **feasible** (the contract can be adapted to the neutral challenge-response flow). **TagEkyc is its own provider following its own flow** — TagEkyc defines the neutral contract; SignFlow writes a TagEkyc adapter; do NOT couple TagEkyc to a rigid SignFlow interface. (This ADR makes no claim about SignFlow's internal implementation state — SignFlow HLD/LLD specify the interface; confirm the exact adapter delta with a SignFlow-side audit.)

## Open items (carried)
- **E-Q01** assurance level in request/response — partly built (`AssuranceLevel` + required checks); confirm `required_assurance`/`achieved_assurance` shape.
- **E-Q02** proof / result_hash components — the signed proof MUST include identity/method/session/challenge, not just current manifest fields (TIP-67B).
- **E-Q03** vault TTL/retention for a shared service — must satisfy the strictest client (signing). [NEEDS-LEGAL]
- **E-Q04** data-controller model — only if eKYC is ever outsourced to a third party. N/A while self-built.

## Status / supersession
This ADR supersedes the TIP-67 v0.3 "transaction-bound verifiable evidence" direction. The drift (transaction-bound coupling) is registered as P1 debt and is **targeted for resolution by TIP-67A** — it is NOT resolved until TIP-67A is built, reviewed, and accepted (decision accepted ≠ implementation done).
