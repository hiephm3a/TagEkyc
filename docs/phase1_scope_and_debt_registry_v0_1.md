# Phase 1 Scope and Debt Registry v0.14

**Version:** 0.14
**Status:** Active — S1 debt registry with TIP-88C1 v0.17 planning amendment
**Date:** 2026-07-29

## Changelog

### v0.14 — TIP-88C1 v0.17 final projection-clock correction

- Registered distinct readiness, CaptureAgent pre-wait and custody post-wait
  clocks and the corrected sequential late-token/backoff expired-owner sums.
- Removed the duplicate per-claim attempt limit.
- Added `C1-BB-RETRY-COUNT-ACCOUNTING-GATE` with all nine mandatory
  Build-Brief questions; no durable retry-count enforcement is claimed.

### v0.13 — TIP-88C1 v0.16 self-auditing planning closure

- Registered the 15-path readiness derivation and combined evaluation-wait plus
  expired-owner-reclaim negative.
- Registered the 92-symbol population/register diff, finite resource/retry
  limits, common busy backoff and persisted non-resettable disposition clock.
- Registered pending/ready re-entry/reclaim separation and the exact two-value
  prior-R2 settling set. Independent review remains required.

### v0.12 — TIP-88C1 v0.15 symbol and retry correction

- Deferred every C1 phase/residue/retry/body/admission summary to Planning
  section 10.0 and registered exact symbol ownership in section 15.1.
- Registered the exact three-case continuation projection, including
  expired-owner `ReclaimExecutionBudget`.
- Registered temporary unavailability as retryable but
  non-terminal/non-replay-stable and P4/P5 as server-observable.

### v0.11 — TIP-88C1 v0.14 phase-table and termination-budget correction

- Registered Planning section 10.0 as the single P0–P7 outcome source,
  metadata-only finalization and body-required progression.
- Registered exact P4 terminalization with retained Bound alias/idempotency
  evidence.
- Registered four bounded no-default prior-R2 termination budgets and
  conditional runtime projections.

### v0.10 — TIP-88C1 v0.13 admission and re-entry correction

- Registered metadata-first admission signaling, bounded memory-only transport
  residual and no proxy/disk prebuffering.
- Split internal claim progression from the final CaptureAgent response.
- Registered same-owner fenced re-entry only after durable prior-R2
  termination.
- Removed agent-capacity attestation; local exhaustion is local and server
  capacity outcomes are custody-only.

### v0.9 — TIP-88C1 v0.12 physical-capacity correction

- Split producer-buffer and custody-window capacity by physical host and made
  every slot/aggregate admission branch independently reachable.
- Registered the retryable incomplete-transport versus terminal clean-content-
  mismatch classification and phase-specific declaration correction.
- Replaced stale three-class commitment evidence with two supported positives
  and the `LivenessMedia` unsupported negative; added the standing
  mutation-reachability sweep.

### v0.8 — TIP-88C1 v0.11 transport and bounded-capacity correction

- Registered one external no-resume ingress operation shape per invocation with
  internal claim/R1–R6
  ceremony.
- Registered exact artifact-size, plaintext-memory and concurrency bounds plus
  fail-closed readiness/runtime outcomes.
- Registered the lost-response TTL retention term and distinct live-evaluation
  retry boundary.
- Ratified D2 for DG2 portrait and live selfie; deferred LivenessMedia to a
  separately ratified resumable transport slice.

### v0.7 — TIP-88C1 v0.10 lifecycle and privacy correction

- Registered Homeowner option (b): content-free producer-envelope v2, with the
  claimed digest protected only by keyed commitment and verified at R3.
- Registered executable token reissue/current-slot CAS semantics, finite
  latest-issued cleanup horizon, server-cap-aware R1 admission and exact
  configuration bounds.
- Registered the closed response union and length-prefixed
  `C1HashCanonical`, distinct from landed JCS `HashCanonical`; retained every
  implementation/provider/raw/production gate.

### v0.6 — TIP-88C1 v0.9 external-review correction

- Registered producer-claim envelope binding, broker-owned normalized
  admission recomputation, exact evaluation/token expiry and full retry
  lineage.
- Registered phase-conditioned capability/lifetime residue, variant-based
  active/historic key failure and reconciliation plaintext hygiene/readiness.
- Kept every implementation, provider, legal, raw-artifact and production gate
  open pending independent review and Homeowner ratification.

### v0.5 — TIP-88C1 v0.8 review-required planning patch

- Registered executable capture-time ingress idempotency, private keyed content
  commitment, complete R1 recovery context, fenced producer/reconciler
  ownership and honest graceful/abrupt plaintext-lifetime semantics.
- Registered authority snapshots/checkpoints, accepted-session source
  selection, C2 disposition CAS, monotonic renewal, historic-key/DR and
  CaptureAgent host-posture gates.
- Preserved every legal/controller/provider/real-artifact/implementation/
  production gate and the distinct GOV/ART phase boundaries.
- Registered the V1 corrections: additive server-authored capture acceptance,
  rotation-safe begin/complete ingress, source/attempt/staged fingerprint
  separation, idempotent key-reference recovery, exact provisional recovery,
  deterministic C2 pre-registration/in-Seal disposition CAS, and fail-closed
  capability-graph equality.
- Registered the V3 correction that makes NewCandidate `complete` and R1 one
  atomic source/context commit; a bounded evaluation shell is not a source.
- Registered V4 token-lineage, fresh-authority and test-bite corrections:
  distinct New/Existing tokens, in-place monotonic shell reclaim, and
  in-`complete` authority revalidation before disclosure or R1.
- Registered the v0.8-V5 checkpoint corrections: durable alternate-key aliases/
  tombstones, stateful opaque token brokerage, typed key results routed through
  `complete`, exact token outcomes and cycle-qualified review evidence.
- Registered the final v0.8-V6 corrections: broker-owned validator/derivation/
  `complete`, no provisional-result or selector crossing to ingress, mandatory
  bounded DB-issued token TTL, exact alias/canonical residue, and the complete
  token-variant/result matrix. These are patched but not internally re-reviewed;
  implementation remains blocked pending external review and ratification.

### v0.4 — TIP-88C1 v0.7 retained-custody candidate

- Recorded the ratified `EncryptedRawVaultRetained` mode direction and kept its
  controller/legal/retention/purge/hold/access sub-decisions open.
- Registered the split source fingerprints, immutable per-job source mapping,
  B4 sealed-state amendment, unknown-seal recovery, microsecond replay, process
  isolation, and prohibited-observability gates.
- Preserved every real-artifact, provider, legal, security, readiness,
  implementation, and production non-claim.

### v0.3 — TIP-88C1 round-5 canonicalization correction

- Synchronized the registered C1 planning version to v0.6 after the GUID
  canonicalization patch regression was corrected.
- No debt disposition, gate, scope, or authorization changed.

### v0.2 — TIP-88C1 secure raw-source planning amendment

- Registered C1's planned custody/assembly ownership and the unresolved
  `GOV-001`/`ART-001` through `ART-009` gates.
- Distinguished generated pinned-provider fixture evidence from real-artifact
  and production approval.
- Kept raw biometric protection and capture-artifact retention open until
  reviewed lifecycle, authority, provider, and operational evidence closes.

### v0.1 — Initial Phase 1 scope and debt registry

- Established S1 scope, deferred debt, exit criteria, and risk registry.

## S1 In-Scope

- Verification Session API
- Client Application and API Key authentication model
- RequiredChecks policy model
- `VerificationSession` as root business correlation object
- `STANDARD_EKYC_PROFILE` and `CHALLENGE_BOUND_EKYC_PROFILE` policy naming
- CaptureArtifact and EvidenceResult logical model
- Generic `CAPTURE_QUALITY` result category
- CCCD/NFC result shape
- Face match result shape
- Liveness result shape
- Fingerprint result shape
- Evidence VaultRef/hash model
- `EkycEvidencePackage` manifest model
- Append-only audit event model
- Webhook/callback result delivery model, with actual delivery/retry/outbox deferred after TIP-07 Option A
- SignFlow integration contract
- Mock or PoC adapters behind interfaces

## S1 Out-of-Scope

- Production-certified legal eKYC
- Digital signing
- SignFlow database or code dependency
- Certified NFC document validation
- Certified biometric/liveness assurance
- Production fingerprint hardware fleet management
- Production capture artifact retention policy enforcement
- Production capture quality retry policy enforcement
- Production payload signature implementation
- Production webhook signature and replay protection
- Production evidence package signature implementation
- Production capture agent/device trust program
- Full operator review console
- Formal regulatory reporting
- Database migrations and implementation code in the documentation baseline

## Deferred Debts

TIP-88C1 v0.17 preserves the broker boundary and content-free envelope,
ratifies only the two D2 still-image classes, and bounds artifact size,
plaintext memory and concurrency separately on the CaptureAgent and custody
hosts. Planning section 10.0 is the single phase/residue/retry/body/admission
source for every C1 summary in this registry; section 15.1 is the single
normative-symbol source. Each invocation uses one external admission-gated
no-resume operation shape, and same-UUID retry invokes that same shape again.
The exact 15-path readiness enumeration crosses five pending/ready state cases
with no wait, evaluation wait or busy backoff; both expired-owner cases add
reclaim cost. Lease and reconciler minimum age share the owner-CAS anchor;
later token/backoff waits are sequential. CaptureAgent evaluates future wait
before sleeping, while the custody server evaluates only future operation cost
from post-wait DB time. Temporary unavailability is retryable but
non-terminal/non-replay-stable; the next invocation chooses busy, ready
same-owner progression or recapture from durable settlement, budget and buffer
evidence. The 91-symbol audit owns three finite source/key/object limits, one
common busy backoff and one persisted non-resettable disposition clock.
Retry-count accounting is unpinned and blocks Build-Brief dispatch at
`C1-BB-RETRY-COUNT-ACCOUNTING-GATE`.
TIP-88C1 v0.10 made the Homeowner-ratified
producer-envelope v2 content-free; keyed commitment/R3 owns digest binding and
no database-plus-candidate unkeyed membership verifier is allowed. It also
closes CAS current-evaluation reissue/response-loss/concurrency and finite
cleanup, effective server-cap R1 admission, exact configuration bounds, closed
response shape and length-prefixed canonical hashing.
These patches are pending external CC/GPT verification and authorize no
implementation.

| Priority | Debt | Description | Exit Trigger |
| --- | --- | --- | --- |
| P0 | Raw biometric protection | Define encryption, access, retention, and deletion controls before real biometric data is stored. | Before pilot with real users |
| P0 | TIP-88C1 raw-source custody and sealed-assembly governance | **Status: OPEN / PLANNING v0.17 PATCHED — INDEPENDENT REVIEW REQUIRED — IMPLEMENTATION BLOCKED.** `EncryptedRawVaultRetained` remains a mode direction only; controller/legal basis/retention/purge/hold/access/B4/delivery and D3–D9 remain open. D2 is ratified only for `ChipDg2Portrait` and `LiveSelfieImage`; `LivenessMedia` requires a separate resumable-transport slice. Planning section 10.0 solely owns phase, residue, retry, body and admission behavior; section 15.1 solely owns the 91-symbol population/register, 15-path readiness derivation, distinct readiness/client-pre-wait/server-post-wait clocks, three finite source/key/object limits, exact common busy backoff and persisted non-resettable disposition clock. Pending and ready same-owner/expired-owner cases are separate; exact-fence `Terminated` and P4 `TerminatedBeforeStart` both settle prior R2. Each invocation has one external operation shape; a retry invokes the same shape with the same UUID. Retry-count accounting remains Build-Brief blocked. All other C1 invariants, GOV/ART gates, fixture-only MinIO posture and non-authorization boundaries remain unchanged. No Build Brief, implementation, provider operation, raw persistence/reuse, production, legal, audit, security, readiness, or capability claim exists. | Independent review re-runs P1–P4 population/path/mutation checks; `C1-BB-RETRY-COUNT-ACCOUNTING-GATE`, all pending D1/D3–D9, acceptance/schema, symbol/alias/token/broker/transport/capacity contracts and cross-repo/B4 allowlists require ratification before dispatch; real artifacts require D5, both host/key/DR readiness surfaces and exact ART resolutions |
| P0 | C1-A-CLASS-PROVENANCE-GATE | **OPEN.** C1-A's as-built authority boundary is deliberately split. PostgreSQL enforces the verification-session FK, capture-artifact FK, positive monotonic `CaptureRevision`, append-only tables, one immutable selection per `(VerificationSessionId, RawClass)`, the existing-`CaptureAcceptanceId` reference, and the fail-closed actor GUC. The caller supplies, and SQL does **not** prove, `SessionChallengeHash` computed in C# with `HashCanonical("tip-69-capture-session-challenge", ...)`; for NFC-derived classes the caller also asserts `RawClass` because landed `capture_artifacts.ArtifactType = NfcReadArtifact` does not distinguish DG1/DG2/DG13/DG15/SOD/AA material. Brief §6.1's component-level ownership/challenge and class-specific identity validation therefore remains application-layer responsibility for those fields. | Close only when the capture layer records independently verifiable class-specific artifact identity for NFC-derived classes and the C1 acceptance producer verifies it. Brief §7/D2 additionally requires a class-specific digest for `ChipDg2Portrait`. `LiveSelfieImage` is not blocked by this provenance gap. |
| P0 | C1-B2-INGRESS-COMPLETE-GATE | **OPEN — intentionally deferred by TIP-88C1-B1.** B1 lands only the key-free claim shell, four-field alias, bounded lookup-edge locking, content-free v2 producer-envelope fingerprint, digest-only evaluation token, live-slot protection and monotonic reclaim CAS. It allocates no `SourceArtifactId` and therefore does not add `uq_raw_export_source_ingress_source`. The claim-comparison broker, keyed `ContentCommitment`/HMAC, `complete_raw_export_source_ingress_claim`, `AdmissionFingerprint` recomputation, `SourceArtifactId` allocation, `uq_raw_export_source_ingress_source`, R1 crypto/DEK/KEK/object context, `Bound`/`ConflictTombstone` resolution, provider/key-provider integration and fixture-key resolution remain absent. | Close only through a separately authorized C1-B2 build that lands every listed piece, preserves B1 token/alias revision-and-fence semantics, adds the deferred uniqueness only when a source can exist, and proves broker-only completion plus zero unauthorized source/object/key residue. |
| P0 | TIP-88C1 retry-count accounting Build-Brief gate | **`C1-BB-RETRY-COUNT-ACCOUNTING-GATE` is OPEN and blocks Build-Brief dispatch/readiness.** The Build Brief must answer exactly: (1) whether the first invocation counts; (2) whether `ClaimEvaluationInProgress` and each busy outcome increment; (3) whether transport disconnect increments; (4) whether metadata-only finals such as `AlreadyAvailable` increment; (5) whether scope is UUID, alias, canonical claim or source; (6) which durable row owns the count when pre-R1 retries precede any source row and the alias has no counter; (7) what resets on terminal/cleanup; (8) how CaptureAgent restart recovers monotonic count; and (9) how agent/server disagreement resolves. `MaximumRetryCountPerIngress` is only an unpinned proposed bound; it is not durable cross-process enforcement. | Ratified Build Brief pins all nine answers, exact storage/ownership/recovery/failure semantics and a discriminating gate test before implementation dispatch |
| P0 | C1-KEY-VAULT-ADAPTER-GATE | **OPEN — intentionally deferred by TIP-88C1-KEY1.** KEY1 lands only the in-process content-commitment path: `IContentCommitmentService.ComputeAsync(selector, lpPayload)` computes HMAC-SHA-256 with a key resolved through the copied (TagEkyc-owned, effective-`internal`) ProtectedValues resolver and a NON-SECRET fixture key, so the key material is confined to `InProcessContentCommitmentService` and never crosses to the broker/consumer. No vault-side (key-never-in-process) computation exists. A future Model-A `VaultCommitmentService` (OpenBao Transit or PKCS#11 HSM) must implement the SAME `IContentCommitmentService` so the broker is unchanged, computing the MAC inside the vault without returning key bytes to the process. | Close only through a separately authorized slice that lands a Transit/HSM adapter behind `IContentCommitmentService`, proves the key never enters process memory on that path, and pins its own golden/interop vector. |
| P0 | C1-KEY-CATALOG-GATE | **OPEN — intentionally deferred by TIP-88C1-KEY1.** KEY1 resolves `CommitmentKeySelector(KeyId, KeyVersion)` through `FixtureContentCommitmentCatalog`, which maps only fixed fixture selectors to a `config:` reference; an unknown selector returns a typed `ProviderFailure`. No real selector→reference/key-version binding, rotation, activation/validity window, or catalog governance exists. | Close only through a separately authorized slice that lands the real selector→reference binding (versioned, rotation/validity-aware) with its provider wiring, replacing the fixture catalog, before any real content-commitment key is used. |
| P0 | C1-KEY2-VAULT-ADAPTER-GATE | **OPEN — intentionally deferred by TIP-88C1-KEY2.** KEY2 lands only the in-process subject-ref-token path: `ISubjectRefTokenService.ComputeAsync(selector, lpPayload)` computes HMAC-SHA-256 with a subject-token key resolved through the shared (TagEkyc-owned, effective-`internal`) ProtectedValues resolver and a NON-SECRET fixture key distinct from the commitment key, so the key material is confined to `InProcessSubjectRefTokenService` and never crosses to the caller. No vault-side (key-never-in-process) computation exists. A future Model-A `VaultSubjectRefTokenService` (OpenBao Transit or PKCS#11 HSM) must implement the SAME `ISubjectRefTokenService` so the caller is unchanged, computing the token inside the vault without returning key bytes. | Close only through a separately authorized slice that lands a Transit/HSM adapter behind `ISubjectRefTokenService`, proves the subject-token key never enters process memory on that path, and pins its own golden/interop vector. |
| P0 | C1-KEY2-CATALOG-GATE | **OPEN — intentionally deferred by TIP-88C1-KEY2.** KEY2 resolves `SubjectTokenKeySelector(KeyId, KeyVersion)` through `FixtureSubjectTokenCatalog`, which maps only fixed fixture selectors to a `config:` reference distinct from the commitment key's; an unknown selector returns a typed `ProviderFailure`. No real subject-token selector→reference/key-version binding, rotation, activation/validity window, or catalog governance exists. | Close only through a separately authorized slice that lands the real subject-token selector→reference binding (versioned, rotation/validity-aware) with its provider wiring, replacing the fixture catalog, before any real subject-ref token is minted. |
| P0 | C1-BB-D1-AUTHORITY-DISPOSITION-GATE | **RATIFIED FIXTURE-ONLY (TIP-88C1-B2-AUTH).** Planning (brief lines 3936-3937) left `AuthoritySnapshot.ReuseDisposition` and `ExtensionDisposition` with "no value is admitted" pending this Homeowner D1 gate. Ratified fixture-only closed sets, CHECK-constrained in `raw_export_authority_snapshots`: `ReuseDisposition ∈ {'FreshAuthorityRequired'}`, `ExtensionDisposition ∈ {'Forbidden'}` (safest posture: reuse always requires fresh authority re-eval; extension never inferred). `ApprovedPurpose` fixed `'SubjectRawBiometricExport'` (byte-exact with the landed subject-consent PurposeCode). These are NON-LEGAL fixture values; production is blocked by the fixture-authority gate below. | Close only when the real D1 controller/legal/retention authority ratifies the production closed sets (Gate B), replacing the fixture CHECK values under a new AuthoritySnapshot schema version. |
| P0 | C1-B2-AUTH-FIXTURE-PRODUCTION-GATE | **OPEN — intentionally fixture (TIP-88C1-B2-AUTH).** The controller authority-snapshot surface `raw_export_authority_snapshots` is deployer/bootstrap-seeded fixture data; `raw_export_resolve_current_authority_for_source` is the only runtime-EXECUTE function (append/withdraw/revoke are deployer-only). Production readiness fails closed via config `TagEkyc:RawExport:AuthoritySnapshot:Profile`: Production+`Fixture` → `PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE`; missing → `..._PROFILE_MISSING`; unknown → `..._PROFILE_INVALID`. No production non-fixture path exists in this slice. | Close only through a Gate-B slice that adds a `Ratified` profile bound to real controller authorities (external evidence contract, not a self-asserted flag), so Production readiness can pass. |
| P0 | C1-B2-PROFILES-FIXTURE-PRODUCTION-GATE | **OPEN — intentionally fixture (TIP-88C1-B2-PROFILES).** The source-encryption profile bundle (`fixture-storage-local-v1` / `fixture-source-encryption-v1` / `fixture-aead-aes256gcm-v1` / `fixture-nonce-random96-v1` / ChunkSize 1048576) and the KEK reference bundle (`fixture-kek-provider-v1` / `fixture-kek-v1` / v1 / fingerprint `f6e43157…` = SHA-256 of a documented non-secret string) are dev/test fixtures behind `ICustodyProfileProvider`. Production readiness fails closed via config `TagEkyc:RawExport:CustodyProfile:Profile`: Production+`Fixture` → `PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE`; missing → `..._PROFILE_MISSING`; unknown → `..._PROFILE_INVALID`; out-of-range time-bounds → `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`. Identifiers are opaque; nothing branches business/authority/policy on them. | Close only through a Gate-B slice that adds a `Ratified` profile bound to real storage/encryption profiles and a real KEK provider (external evidence, not a self-asserted flag). |
| P0 | C1-B2-KEK-DEK-PROVIDER-GATE | **NARROWED by TIP-88C1-B2-DEKKEK.** The FIXTURE envelope provider is LANDED: `IAttemptKeyProvider.CreateOrGetAttemptKeyAsync` (operation-scoped, idempotent by `AttemptKeyReservationId`) with a fixture KEK (SHA-256 of a dedicated material string, distinct from the commitment/subject-token keys, confined + zeroized), random 32-byte DEK wrapped AES-256-GCM under the KEK (AAD-bound to the reference), a process-local wrapped-DEK store, and a zeroizable `IAttemptDekLease` (the ONLY key material crossing the boundary; KEK never exposed). Production blocked via `TagEkyc:RawExport:AttemptKey:Profile` (fixture-active/missing/invalid codes). **STILL deferred:** the real KEK-unwrap behind a vault/HSM (key never in process — analogous to `C1-KEY-VAULT-ADAPTER-GATE`/`C1-KEY2-VAULT-ADAPTER-GATE`) AND a durable wrapped-DEK store that survives process loss (the fixture store is in-memory/process-local). | Close through a separately authorized slice that lands the real vault/HSM KEK-unwrap and a durable wrapped-DEK store behind the same `IAttemptKeyProvider`, before any real Raw BIO is encrypted in production. |
| P0 | C1-B2-CORE-BROKER-ROLE-GATE | **OPEN — deployment (TIP-88C1-B2-CORE).** `complete_raw_export_source_ingress_claim` is granted EXECUTE ONLY to the NOLOGIN capability role `tagekyc_raw_export_claim_broker` and is REVOKED from `tagekyc_runtime` (verified on-code). The C# claim-comparison broker is the only caller and opens/commits the R1 transaction (the SQL function never self-commits). **Deployment prerequisite:** the app path that runs the broker must connect as a LOGIN principal holding exactly the `tagekyc_raw_export_claim_broker` capability (not runtime, not deployer); the ingress/general-runtime path must NOT hold it. Provisioning the role membership/connection identity is operational, not an EF-migration artifact. | Before hospital-trial production deploy: provision the broker capability principal, confirm ingress/runtime cannot EXECUTE complete, and confirm readiness. |
| P0 | C1-B2-CORE-DEFERRED-OUTCOMES-GATE | **OPEN — intentionally deferred by TIP-88C1-B2-CORE.** CORE lands ONLY the NewCandidate path (shell → valid Reserved source) with outcomes NewReservation, SOURCE_RETENTION_NOT_AUTHORIZED, CLAIM_TOKEN_INVALID, RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID (cap-reject = Active→Completed, zero rows, no retry). **NARROWED by TIP-88C1-B2-BETA:** β closed ExistingMatch, FingerprintConflict→ConflictTombstone, and RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE (and re-emitted `begin` to fix the alternate-idempotency-key reachability bug — only `IngressIdempotencyKey` may differ for an alternate alias). STILL deferred: RAW_EXPORT_SOURCE_ALREADY_AVAILABLE (needs an Available descriptor from R5) and the P4/P5 early-body/re-entry residue. The fixture nonce/framing codec (`tip-88c1-nonce-seed-commitment-v1`, `tip-88c1-framing-parameters-v1`, Random96 seed sentinel `"none"`) is CORE-defined for the fixture strategy; a real seed-derived strategy is a future gate. R2–R6 (actual DEK/KEK unwrap + AEAD + object write + Staged/Available) remain deferred (see C1-B2-KEK-DEK-PROVIDER-GATE). | Close through the R2–R6 slices (which unlock AlreadyAvailable + P4/P5 residue), each preserving CORE's atomic-R1 + broker-only + recovery-context invariants. |
| P0 | Stable DG2 artifact-hash RRI | Status: OPEN / PARTIALLY-MITIGATED-B1. Current HN212 production capture computes `NfcArtifactHash = SHA256(DG2)` over raw DG2 bytes. This is a stable per-card biometric-derived identifier, proof-bound into append-only evidence history, present in ordinary evidence-table backups, and historically returned to authorized BusinessConsumers through evidence-ledger / evidence-package summary APIs. TIP-87 closed the BusinessConsumer DTO egress path by removing `ArtifactHash`/`PayloadHash`, but the internal append-only, backup, and signed-proof copy remains, so this RRI is not closed. It is not raw image retention, but it is not erasable by crypto-shred or ordinary row deletion. Access control prevents one consumer from reading another consumer's sessions, but does not prevent cross-consumer correlation if the same globally stable value is shared/compared. DPO/Homeowner must explicitly choose a disposition before real-patient hospital-trial reliance: acknowledge/ratify, keep the B1 egress mitigation, or open a proof-contract mitigation TIP (keyed/domain-separated/session-varying binding, with blast-radius to TIP-67G/golden vectors, SignFlow/consumer verification, and Agent hash computation/keying). | Before hospital trial with real patients |
| P0 | Capture artifact retention policy | Define retention, deletion, legal hold, vault lifecycle, and recapture handling for raw capture artifacts. | Before real user artifacts are stored |
| P0 | Capture agent trust/scoping model | Define which agents, SDKs, gateways, and adapters may submit artifacts or evidence results. Business clients must not submit arbitrary `PASSED` evidence. | Before pilot with real capture devices |
| P0 | Legal certification gap | Confirm certification requirements per jurisdiction and use case. | Before production claim |
| P0 | Evidence package signature | Replace placeholder `evidencePackageSignature` with managed signing keys and verification process. | Before external audit reliance |
| P1 | NFC production readiness | Validate supported CCCD/NFC documents, devices, and authentication model. | Before NFC is used for production decisions |
| P1 | Fingerprint hardware dependency | Define device trust, SDK integration, and capture quality controls. | Before fingerprint check becomes mandatory |
| P1 | Capture quality retry policy | Define retry counts, reason codes, terminal `FAILED_CAPTURE_QUALITY`, UX messaging, and operator override rules. | Before production capture flows |
| P1 | Face liveness quality | Select and test liveness/PAD engine for expected threat model. | Before production biometric assurance |
| P1 | Policy versioning | Make RequiredChecks and risk policies versioned and reproducible. | Before multiple client policies |
| P1 | Payload signature model | Define when `payloadSignature` is needed, canonical payload format, algorithm, key management, and verification rules. | Before signed API payload reliance |
| P1 | Webhook signature/replay protection | Define `webhookSignature`, delivery id, timestamp tolerance, replay cache, rotation, and retry behavior. | Before production webhook reliance |
| P1 | Request/correlation id conventions | Define `requestId`, `correlationId`, idempotency, log propagation, and support lookup conventions. | Before multi-client pilot |
| P1 | Profile naming and policy mapping | **Resolved by TIP-67A:** `TRANSACTION_BOUND_EKYC_PROFILE` was neutralized to `CHALLENGE_BOUND_EKYC_PROFILE` with opaque `Challenge` and optional `ClientReference`. Old profile/field names remain input-only compatibility aliases. | Resolved |
| P1 | Decision basis not bound to evidence hash (EBS) | Per-evidence `Confidence`, `decisionReasonCodes`/`retryReasonCodes` (and `RiskScore` when it becomes live) are persisted and append-only-protected but are NOT covered by the manifest/package hash chain — only the final `Result` and `AssuranceLevel` are bound (see `VerificationCompletionApplicationService` decisionSeed/manifestBody). The quantitative/qualitative basis most likely to be litigated is therefore not tamper-evident within TagEkyc's own chain (`PayloadHash` is adapter-asserted and the raw payload is not retained, so `Confidence` is not transitively verifiable). Surfaced by the TIP-65 adversarial spot-check (2026-06-22). Proposed approach: encode `Confidence` as a decimal-string (`F6` invariant, per the TIP-65 numeric convention) + `reasonCodes` as ordered string arrays into the manifest body (= a new package version under the TIP-65 versioning rule); needs the legal lens first (does NĐ 130 / the assurance framework require attesting the quantitative basis, or only `AssuranceLevel` + `Result`?). | Bind via a future evidence-model TIP (number assigned when activated, after signing **TIP-66**) before external/legal reliance on the decision rationale |
| P1 | eKYC neutrality drift (SignFlow coupling) | **Resolved by TIP-67A for shipped S1 behavior:** the eKYC core no longer interprets SignFlow transaction concepts; it echoes opaque challenge/client correlation only. Neutral verifiable proof remains separate in TIP-67B. | Resolved for 67A; 67B remains future proof surface |
| P1 | Raw-export rule-table runtime-role separation (TIP-88A deployment gate) | The TIP-88A raw-export requirement-rule tables `tagekyc.raw_export_requirement_rule_sets` and `tagekyc.raw_export_requirement_rules` are migration-seeded and immutable at runtime. In-DB enforcement (always on, role-independent) is the `reject_raw_export_rule_runtime_mutation()` `BEFORE INSERT/UPDATE/DELETE` trigger. Defense-in-depth (must be provisioned operationally): the production runtime DB principal MUST have `SELECT` only on both tables — no `INSERT`/`UPDATE`/`DELETE`. Production `/readiness` fails closed with `PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE` (HTTP 503) if the runtime principal holds any mutation privilege, so a mis-provisioned or single-role deployment blocks readiness rather than silently weakening the derivation-rule gate. Concrete role names/grants are operational, not an EF-migration artifact (see the Raw-Export Rule-Table Privilege Gate section of `docs/deployment/hospital_trial/postgres_migration_runbook.md`). Landed as-built in commit `e63cdf9` (TIP-88A). | Before hospital-trial production deploy: provision the SELECT-only runtime role on both rule tables and confirm `/readiness` passes |
| P1 | Raw-export control-plane role provisioning + root-authority bootstrap (TIP-88B1 deployment gate) | TIP-88B1 (raw-export control plane, landed `2c684cf`) adds 4 append-only event tables (`raw_export_grants`, `raw_export_control_authorities`, `raw_export_fulfillments`, `raw_export_policy_lifecycle`) written ONLY via `SECURITY DEFINER` functions. In-DB enforcement (always on): direct raw-SQL INSERT is rejected (append-guard trigger) and the actor principal comes from a transaction-local GUC (`SET LOCAL tagekyc.actor_principal_id`), fail-closed. **Deployment gate (must be provisioned operationally, NOT an EF-migration artifact):** three NOLOGIN CAPABILITY roles the migration creates — `tagekyc_raw_export_deployer` (owns the SD functions + INSERT on the 4 event tables), `tagekyc_runtime` (least-privilege runtime capability: `USAGE` + `EXECUTE` on the 4 append functions; the migration does NOT grant it table `SELECT`), `tagekyc_raw_export_bootstrapper` (deploy-only: `EXECUTE` on `raw_export_bootstrap_global_authority`). **Because `tagekyc_runtime` is NOLOGIN, the app connects as a LOGIN principal provisioned to hold exactly the runtime capability (role membership/inheritance or `SET ROLE`), whose `current_user` is NOT the deployer/bootstrapper. The EFFECTIVE runtime role used during resolver/readiness queries ALSO needs an operational `GRANT SELECT` on the read set the resolver/readiness read directly — 88A `raw_export_policy_versions`/`_requirements`/`_closures`/`_requirement_rule_sets`/`_requirement_rules` + the 4 88B1 event tables — with NO INSERT/UPDATE/DELETE. Grant it DIRECTLY to the `tagekyc_runtime` capability role (so it is effective under BOTH role-inheritance AND `SET ROLE tagekyc_runtime`; a grant to only the login role is suspended under `SET ROLE`). This SELECT is NOT migration-provided; it is a deployment step (a missing SELECT surfaces as a generic resolver/readiness failure, not one of the five listed codes).** Root `GrantAdmin`/`RecorderAuthorityAdmin`/`ActivationAuthority` authorities MUST be bootstrap-seeded (real operator principals, not a dev/default) before use. Production `/readiness` fails closed with `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING`, `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT`, `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE`, `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`, or `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID` if mis-provisioned. NOTE: the actor principal is application-asserted (not DB-authenticated); non-forgeability depends on the `tagekyc_runtime`/deployer role separation being provisioned. See the runbook section "Raw-Export Control-Plane Role & Bootstrap Gate". | Before hospital-trial production deploy: provision the 3 roles, connect the app as a LOGIN principal that inherits/holds the `tagekyc_runtime` capability (NOT deployer/bootstrapper), grant SELECT on the read set to `tagekyc_runtime`, bootstrap real root authorities, and confirm `/readiness` passes |
| P1 | Raw-export subject-consent role provisioning + authority bootstrap (TIP-88B2 deployment gate) | TIP-88B2 (subject export consent, landed `8cd52a3`) adds 3 append-only tables (`raw_export_subject_consent_events`, `_classes`, `_authorities`) written ONLY via `SECURITY DEFINER` functions owned by the non-login `tagekyc_raw_export_deployer` (`search_path=pg_catalog`). In-DB enforcement is always on: UPDATE/DELETE denied by trigger on all three tables; direct INSERT rejected outside the intended append path; actor from `SET LOCAL tagekyc.actor_principal_id`, fail-closed; class children only in the same transaction as their `Granted` parent (xmin guard). **Deployment gate (operational, NOT an EF-migration artifact):** the app connects as a LOGIN principal holding the `tagekyc_runtime` capability (not deployer/bootstrapper); `tagekyc_runtime` gets `EXECUTE` on **exactly three** functions (`raw_export_resolve_subject_consent_for_authorization`, `raw_export_append_subject_consent_granted`, `raw_export_append_subject_consent_withdrawn`) and on NOTHING else — not the authority-management function, not the bare hash/lock-key/session-lock helpers; runtime holds NO `INSERT`/`UPDATE`/`DELETE`/`TRUNCATE` on the three consent tables and MUST NOT be granted `UPDATE` on `tagekyc.verification_sessions` (the resolver's row lock runs inside the SECURITY DEFINER function as the deployer). **Consent recorder/withdrawer authorities MUST be bootstrap-seeded by the DEPLOYMENT role before any consent write** (`SubjectConsentRecorder` governs `Granted`, `SubjectConsentWithdrawer` governs `Withdrawn`); runtime cannot self-grant. Authority state is latest-event-overall with NO fallback, so an EXPIRING authority silently stops consent capture — monitor `ValidUntilUtc` on seeded authorities. Production `/readiness` fails closed with `PROD_RAW_EXPORT_SUBJECT_CONSENT_FUNCTION_ACL_INVALID` / `..._TABLE_MUTATION_PRIVILEGE` on manifest or privilege drift. **Operational caution: consent `Withdrawn` deliberately does NOT require the session to be `Completed` — do not add any deployment-level guard blocking withdrawal on session state, or consent becomes non-revocable.** See the runbook section "Raw-Export Subject-Consent Role & Bootstrap Gate (TIP-88B2)". | Before hospital-trial production deploy: provision the runtime capability with exactly the 3 EXECUTEs and no table DML, bootstrap the recorder/withdrawer authorities with real operator principals, and confirm `/readiness` passes |
| P1 | Raw-export aggregate function manifest tripwire | Namespace-wide unknown-function tripwire removed when the TIP-88B1 readiness validator moved to an exact whitelist (TIP-88B2 round 3). This prevents later TIP-88B2/TIP-88B3 functions from breaking the B1 readiness gate, but it also stops B1 from detecting unrelated privileged `raw_export_*` functions such as a future backdoor. | Restore via an aggregate cross-slice function manifest before hospital deployment |
| P2 | Raw-export subject-consent trigger defence asymmetries | TIP-88B2 verification surfaced two defence-in-depth asymmetries. First, `raw_export_subject_consent_classes` has no direct `current_user`/append-context guard on INSERT; it is protected transitively by parent event creation plus the same-transaction/xmin child guard. Second, `RAW_EXPORT_SUBJECT_CONSENT_DIRECT_INSERT_UNSUPPORTED` has separate current-user and append-context branches; tests exercise the context branch, while the current-user branch is normally preempted by `42501` ACL denial before the trigger fires. Neither is a production defect under the current least-privilege + SECURITY DEFINER design, but the unproven branch/shape should be ratified or explicitly strengthened before relying on leaked-privilege defence claims. | Close with a future trigger-hardening/review slice: add a direct class append-context guard if desired, and create a safe harness that exercises the current-user trigger branch without weakening production ACLs |
| P3 | Raw-export consent resolver lock mode untested | No test distinguishes the TIP-88B2 authorization resolver's consent-scope advisory lock being SHARED vs EXCLUSIVE. Changing `raw_export_resolve_subject_consent_for_authorization` from `pg_advisory_xact_lock_shared` to the exclusive form keeps the entire suite green, because no test runs two resolvers concurrently against one consent scope. Effect would be a silent THROUGHPUT regression (concurrent authorization reads on the same scope serialise instead of proceeding in parallel), not a correctness or safety regression — the brief pins SHARED, so this is an unguarded compliance drift. Closing it needs one test: resolver A holds its transaction open while resolver B on the same scope completes without waiting. | Close opportunistically in TIP-88B3 (which consumes this resolver) or a later hardening slice |
| P2 | Operator review | Add review queue for ambiguous or failed checks. | Before manual review operations |
| P2 | Webhook observability | Add replay, dead-letter, dashboards, and alerting. | Before high-volume integrations |

## Exit Criteria for S1

- A client application can create a verification session using API key authentication.
- RequiredChecks are persisted and enforced by session lifecycle.
- Mock/PoC capture artifacts and document, NFC, face, liveness, and fingerprint evidence results can be recorded using stable result shapes.
- Capture quality can produce `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `REVIEW_REQUIRED`, or `TECHNICAL_ERROR` without collapsing those outcomes into identity failure.
- Final verification result can be calculated from evidence summaries.
- Evidence package can be built using VaultRefs/hashes and a deterministic manifest hash.
- Audit events are written for key lifecycle actions.
- Completion notification result can be prepared through the LocalDev application projection. Actual webhook delivery, retry, and outbox behavior are deferred after TIP-07 Option A and must not be claimed as implemented in S1 closeout.
- SignFlow contract is documented as a challenge-bound profile with client-side binding validation rules.
- Documentation clearly states S1 is not production-certified eKYC.

TIP-09 reconciliation note: current implemented S1 uses generic TrustedAdapter `/evidence-results` for evidence recording, not specialized evidence result routes. Fingerprint remains optional/demo/deferred and is not part of the default SignFlow S1 required checks unless explicitly enabled by policy in a later accepted slice.

## Risk Registry

### CCCD NFC Production Readiness

Priority: P1

Risk: S1 may model CCCD/NFC result shape but not prove production compatibility with official documents, mobile devices, readers, or legal validation requirements.

Mitigation: Keep NFC behind `INfcDocumentReader`, record adapter version, and validate device/document support before production use.

### Fingerprint Hardware Dependency

Priority: P1

Risk: Fingerprint matching depends on capture device quality, SDK support, driver deployment, and secure template handling.

Mitigation: Keep fingerprint behind `IFingerprintMatcher`, classify fingerprint data as Restricted, and require device trust design before pilot.

### Face Liveness Quality

Priority: P1

Risk: Mock or low-quality liveness detection may be bypassed and MUST NOT be treated as production assurance.

Mitigation: Use S1 only for evidence shape and integration readiness. Select a tested PAD/liveness provider for production.

### Raw Biometric Data Protection

Priority: P0

Risk: Raw face, liveness, and fingerprint data create severe privacy and security exposure if stored or logged incorrectly.

Mitigation: Use VaultRef/hash outside the vault boundary, audit access, and define retention before real data collection.

### Stable DG2 Artifact Hash

Priority: P0

Status: OPEN / PARTIALLY-MITIGATED-B1

Risk: The current NFC proof chain stores `NfcArtifactHash = SHA256(DG2)`. Because DG2 is stable for the CCCD, this value can link sessions for the same card/person. It is proof-bound, append-only, and present in backups. Before TIP-87, it was consumer-visible to authorized BusinessConsumers for the owning client application. Client-application access control does not prevent cross-consumer correlation if recipients compare or leak the same globally stable value.

Mitigation: Treat this git-tracked debt entry as the owning hospital-trial DPO/Homeowner disposition record, not as a raw-export TIP-82R detail. TIP-87 closed the BusinessConsumer DTO egress path by removing `ArtifactHash`/`PayloadHash` from BusinessConsumer read DTOs while leaving the internal proof chain unchanged. This partially mitigates third-party disclosure/correlation risk, but the internal append-only, backup, and signed-proof copy remains; the RRI is not closed. If accepted, disclose the retained proof/evidence metadata in the trial DPIA/consent material. If DPO rejects even internal linkability, open a proof-contract mitigation TIP before real-patient reliance.

### Legal Certification Gap

Priority: P0

Risk: S1 evidence readiness does not equal legal eKYC certification.

Mitigation: Avoid production readiness claims and run jurisdiction-specific legal/compliance review before launch.

## Classification Guide

- P0: Blocks real-user pilot or production claim.
- P1: Blocks production reliability, assurance, or scale.
- P2: Improves operations, supportability, or ergonomics after core proof is stable.
