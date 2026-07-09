# TIP-84 Production-Auth Posture Decision (resolves TIP-15 STOP/RRI items that gate TIP-84B)

Status: DECIDED 2026-07-09 (Homeowner ratified). Resolves the MINIMUM open TIP-15 STOP/RRI items needed to unblock TIP-84B (hashed persistent API-key store). Does NOT re-open TIP-15's boundary or its locked invariants. Records the ratified posture; a decision-register `D-NN` entry is minted at the TIP-84B governance commit.
Repo: D:\Task\Remote Signing\TagEkyc (server). Boundary: TIP-15 (accepted, planning-only). Implements: TIP-84A (store seam, DONE) -> TIP-84B (this decision unblocks) -> TIP-84C (lifecycle, deferred).

## Decisions ratified (Homeowner, 2026-07-09)

### D1 - Credential hashing scheme (resolves TIP-15 §14 Row 3) = KEYED HASH
- **KEYED HASH: `HMAC-SHA256(pepper, presentedApiKey)`.** Rationale: production API keys are HIGH-ENTROPY random tokens (unlike low-entropy passwords), so a FAST keyed hash is sufficient - slow hashing (Argon2/bcrypt) is unnecessary. The keyed hash means a DB dump alone cannot verify/brute-force keys without the pepper (defense-in-depth).
- **Lookup:** by non-secret `KeyPrefix` (indexed, stored plaintext - it is not a secret) to select the candidate, then **constant-time** compare the HMAC. No plaintext-key scan.
- **Salt:** not required per-key with a keyed hash over a high-entropy token; the server-side pepper provides the keyed protection. (A per-key salt MAY be added later without breaking the scheme; not needed now.)

### D2 - Credential store backend (resolves TIP-15 §14 Row 2) = APPLICATION-MANAGED HASHED STORE (Postgres) + pepper via SecretRefResolver
- **Key RECORDS** (the keyed hash + `KeyPrefix` + clientApplicationId + scopes + status + expiresAt + callerCategory + allowed*Ids - **NO plaintext key**) live in **Postgres (D-01, EF)**, a new provider-neutral `ApiKeyRow`/credential table following the established persistence pattern.
- **The pepper** (the HMAC key) is loaded via **`SecretRefResolver`** (`env:`/`file:`, TIP-83B) - it is NEVER stored in the DB, config plaintext, logs, or audit. No external vault is provisioned for the trial (an external secret manager can be adopted behind the same resolver later - hardening TIP).
- Satisfies TIP-15's no-raw-secret invariant: only the keyed HASH is in the DB; the pepper is external.

### D3 - IdP posture (resolves TIP-15 §14 Row 1, NARROW only) = MANAGED API KEYS in-scope; full IdP DEFERRED
- Confirmed: **managed (hashed) API keys are in-scope** for the trial actor classes (BusinessConsumer / CaptureAgent / TrustedAdapter). The FULL provider posture (OAuth/OIDC, mTLS/certificates, service principals, hybrid) stays OPEN per TIP-15 §7 default ("keep production auth provider selection open"). This decision confirms only the managed-API-key sub-path; it does not choose an IdP.

## Constraints carried (TIP-15 invariants + soft rows - the build MUST honor)
- **No raw secret** in DB/config/audit/logs/docs; credential value != actor identity (TIP-15 §6/§8/§11). Only the keyed hash persists; pepper external.
- **Principal/credential separation** (TIP-15 §5/§6): keep `apiKeyId` (rotating credential id) and `clientApplicationId`/principal DISTINCT; do not collapse. `credentialFingerprint` = the non-secret `KeyPrefix` (must not enable reconstruction).
- **[Row 11] Provider-neutral schema:** the `ApiKeyRow` uses the provider-neutral credential vocabulary (Domain already scaffolds `PrincipalId`/`CredentialRef`/`CredentialType`/`CredentialStatus`) with DEFERRED provider-specific columns, so OAuth/mTLS can be added later without a breaking migration. Do NOT bake an API-key-only schema.
- **[Row 10] Audit identity:** lookups/authn surface `principalId`/`credentialRef`/non-secret `KeyPrefix` for audit - never the plaintext key or the hash-with-pepper preimage.
- LocalDev seeded keys are NEVER promoted/migrated to the production store (TIP-15 §8).

## Deferred (NOT this decision, NOT TIP-84B)
- **TIP-84C (lifecycle):** rotation/revocation/suspend/reactivate/expire authority (Row 4), Admin/Operator separation (Row 5, overlaps D-12), break-glass (Row 6 - already default-OFF per TIP-15 §8).
- Full IdP choice (rest of Row 1), service-principal authority (Row 8), cross-client support lookup (Row 7, D-12), production completion authority (Row 9 = D-08), legal/compliance posture (Row 12).

## What this unblocks
- **TIP-84B** now has its posture: a Postgres-backed `IApiKeyStore` production impl that stores keyed-hash + prefix (no plaintext), verifies by prefix-select + constant-time HMAC, loads the pepper via `SecretRefResolver`, behind the TIP-84A seam, with a production `no-plaintext-key` startup guard. The TIP-84B brief cites this decision as its basis.
