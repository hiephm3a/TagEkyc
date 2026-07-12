# TIP-88B2 Raw Export Authorization Decision - Boundary / Placeholder

Status: BOUNDARY ONLY (not a build brief). Opens **after TIP-88B1 (control plane) lands** AND the verification-session recon (below) is done. Split from TIP-88B per GPT + Codex round-2. Still INERT of raw bytes.

## Scope (after 88B1 lands)
The runtime authorization HOT PATH over the 88B1 control plane:
- **Verification-session binding** - reconcile the existing verification-session model FIRST (S recon below); use existing `VerificationSessionId` as the FK/name unless a deliberate reason not to.
- **Authorization evaluation** (per export attempt), in ONE atomic boundary (the B1 resolver is called IN this SAME transaction; eligibility read + decision/evidence write are atomic - a revoke/withdraw/publish between them must yield NO `Authorized` row): call B1's LOCK-CAPABLE resolver `ResolveExportEligibilityForAuthorization(principalId, policyId, policyVersion)` (authoritative DB time; holds the canonical-ordered aggregate locks) and BIND its typed `GrantRef`/`LifecycleRef`/`FulfillmentRefs[]` into the decision -> requires `State == Active` (which already covers grant + lifecycle + current-rule-set + POLICY-scoped fulfillment, derived-at-read; stale => Inactive) -> **PLUS the SUBJECT/ATTEMPT-scoped gate B1 does NOT do: `ConsentArtifact`/subject consent bound to the exact `EkycSessionId`/subject** (a policy-level consent NEVER substitutes; per-attempt subject consent required, TIP-88B1 S0/OQ-B1.6) -> write evidence + emit decision. Any failed gate => deterministic DENY. The decision binds the exact evaluated typed refs returned by the resolver (`GrantRef`/`LifecycleRef`/`FulfillmentRefs[]`, PolicyVersion, RuleSetVersion) PLUS the subject-consent artifact id.
- **Idempotency** - identity binds authenticated principal + request id + `EkycSessionId` + `PolicyId` + `PolicyVersion`; same key+semantics => same `ExportDecisionId`; different semantics => conflict; concurrent duplicates => exactly one decision. Reuse TIP-80S `AppendIdempotency` primitives.
- **[Locked - decision/evidence cardinality] a SINGLE immutable append-only row per authorization attempt** in a dedicated typed `raw_export_authorization_decisions` table, with `Outcome` in `{Authorized, Denied}`. **Only `Authorized` rows yield a usable permit/decision id; `Denied` rows are evidence only.** NO separate REQUESTED/AUTHORIZED/DENIED event rows unless an explicit attempt-header/event-child model is deliberately chosen. Typed columns: `ExportDecisionId`, idempotency identity/hash, `PrincipalId`, `ClientApplicationId`, `ApiKeyId`, `EkycSessionId`, `PolicyId`, `PolicyVersion`, bound `RuleSetVersion`, evaluated grant/lifecycle/fulfillment record IDs, subject-consent artifact id, exact `AllowedRawClasses`, `Outcome`, **`PrimaryCause` + full `Causes[]` (typed child rows or canonical deterministic representation, per B1's deterministic-cause contract - NOT a single unspecified `Cause`)**, `DecisionExpiresAtUtc`, `DecidedAtUtc` (DB-stamped). Written in the SAME transaction as the decision (write-fail => not authorized).
- **Metadata-only permit** `RawExportAuthorizationDecision` (a.k.a. `RawExportPermit`): freezes the exact `AllowedRawClasses` + version so the later raw-assembly slice acts on the FROZEN decision, not current policy.
- **Concurrency** - authorize racing revoke / a new rule-set publish -> never a stale AUTHORIZED; real two-transaction tests.

## Round-3 boundary patches (GPT)
1. **B2 MUST call the authoritative 88B1 LOCK-CAPABLE resolver** (`ResolveExportEligibilityForAuthorization(principalId, policyId, policyVersion)`, TIP-88B1 S2.5 - evaluation time is authoritative DB time, no caller `nowUtc`; it holds the canonical-ordered aggregate locks so a revoke/withdraw/publish cannot commit between eval and the Authorized write). B2 does NOT duplicate grant / lifecycle / fulfillment-validity / current-rule-set logic NOR invent its own state-locking protocol - it consumes B1's `State` + returned record IDs inside its atomic boundary. B2 ADDS only the subject/attempt-scoped gates B1 does not do (subject `ConsentArtifact`, session binding, idempotency, evidence, permit).
2. **The exact allowed-class snapshot in a decision uses TYPED child rows (or a canonical deterministic representation), NOT an unspecified JSON collection** - so the frozen `AllowedRawClasses` is queryable + deterministically comparable and the assembly slice cannot re-resolve.
3. B2 remains blocked on the verification-session recon AND 88B1 landing.

## Locked principles carried into 88B2 (do not weaken)
- **[GPT-3] a permit is NOT an irrevocable bearer capability.** A non-consumed permit becomes UNUSABLE if, before assembly: the grant is revoked; the policy is Suspended/Revoked; the bound rule set is no longer current; a required fulfillment is withdrawn/expired; or the permit expires. **The raw-assembly slice MUST revalidate these current gates + expiry + one-time-consumption** - the permit is a decision record, not a token that survives revocation.
- Carry all round-1 decisions: canonical principal = Guid; exact-version grant; dedicated typed decision table; current-rule-set + current-fulfillment re-check; revocable lifecycle; idempotency; permit freezes exact allowed classes; no raw bytes/hash/assembly; trust 88A CatalogApproved (no re-derivation).

## Verification-session recon (REQUIRED before 88B2 design) - lock:
- canonical `EkycSessionId` type/source (map to existing `VerificationSessionId`?);
- tenant/client ownership of a session;
- acceptable session state for export;
- subject/session binding;
- principal/client access boundary (cross-client session request must DENY).

Do NOT build 88B2 idempotency or permit schema before this recon is resolved.

## Deferred beyond 88B2 (raw-assembly slice)
Actual raw read/assemble, real package/manifest hash, exported-classes, delivery, transient dispose/attestation, and the one-time-CONSUMPTION ENFORCEMENT of a permit.
