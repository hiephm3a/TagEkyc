# Logical Data Model

**File:** `docs/lld_01_data_model_v0_1.md`
**Version:** 1.9
**Status:** Active - S1 data-model, evidence-integrity, neutral proof, and TIP-88C1 v0.17 planning amendment
**Date:** 2026-07-29
**Baseline:** `a98f278`
**Purpose:** Authoritative S1 logical data model and as-built evidence-integrity contract for TagEkyc. This document is not a SQL migration and does not prescribe database technology.

## Changelog

### v1.9 - TIP-88C1 v0.17 final projection-clock correction

- Split configuration readiness, CaptureAgent pre-wait and custody post-wait
  projections; elapsed waits never enter the server decision formula.
- Re-derived expired-owner token/backoff waits sequentially after the shared
  lease/minimum-age owner-CAS anchor.
- Removed the duplicate per-claim attempt limit and transferred retry-count
  ownership/recovery semantics to a mandatory Build-Brief gate.

### v1.8 - TIP-88C1 v0.16 self-auditing planning closure

- Synchronized the 15-path continuation/readiness derivation and split pending
  prior-R2 settlement from ready same-owner/expired-owner progression.
- Synchronized the 92-symbol register audit, finite attempt/key/object/retry
  limits, common busy backoff and persisted non-resettable disposition clock.
- Made exact-fence `Terminated` and P4 `TerminatedBeforeStart` the closed
  settling set. Schema and implementation remain Build-Brief blocked.

### v1.7 - TIP-88C1 v0.15 symbol and retry correction

- Deferred every C1 phase/residue/retry/body/admission statement to Planning
  section 10.0 and registered exact symbol ownership in Planning section 15.1.
- Added the expired-owner reclaim term to the exact three-case continuation
  projection.
- Made temporary unavailability retryable but non-terminal/non-replay-stable
  and replaced client-observation language with server-observable P4/P5 facts.

### v1.6 - TIP-88C1 v0.14 phase-table and termination-budget correction

- Referenced Planning section 10.0 as the single P0–P7 transport source and
  separated metadata-only outcomes from body-required progression.
- Added P4 post-R1/pre-admission attempt terminalization without deleting Bound
  alias/idempotency evidence.
- Added the four-component conditional prior-R2 termination budget contract.

### v1.5 - TIP-88C1 v0.13 admission and re-entry correction

- Added metadata/R1/admission/body phase boundaries and a no-prebuffer/no-disk
  transport posture.
- Split internal claim results from the only CaptureAgent-visible final union.
- Added exact same-owner re-entry with durable prior-R2 termination.
- Removed dynamic agent-capacity attestation and made server capacity
  custody-only.

### v1.4 - TIP-88C1 v0.12 physical-capacity correction

- Replaced the conflated capacity admission with an eight-key manifest split
  between CaptureAgent retained buffers and custody active-stream plaintext
  windows; each slot and aggregate branch is independently reachable.
- Split transport abort/cancellation/incomplete-body retry from terminal
  clean-EOF content mismatch and pinned their residue/retry behavior.
- Synchronized the exact two positive commitment vectors, unsupported
  `LivenessMedia` negative, and pre-begin versus post-R2 declaration correction.

### v1.3 - TIP-88C1 v0.11 transport and bounded-capacity correction

- Pinned one external no-resume custody-ingress operation shape per invocation
  while keeping
  begin/broker/complete/R1–R6 internal.
- Added exact size/memory/concurrency configuration and runtime admission/
  release semantics.
- Added one claim-token TTL to the lost-response retention relation and split
  live evaluation wait from lookup-lock contention with `RetryNotBeforeUtc`.
- Ratified D2 for DG2 portrait and live selfie; deferred LivenessMedia.

### v1.2 - TIP-88C1 v0.10 lifecycle and privacy correction

- Replaced producer-envelope v1 with content-free v2 and kept the claimed
  digest exclusively behind the keyed commitment/R3 verification path.
- Modeled a stable alias plus CAS-replaceable current evaluation issuance,
  monotonic latest-token horizon and exact reissue/concurrency semantics.
- Added effective-deadline R1 admission, bounded token/continuation
  configuration, the closed ingress result union and length-prefixed
  C1-only `C1HashCanonical`; the landed Evidence-Integrity `HashCanonical` JCS
  codec is unchanged.
- No schema or implementation is authorized.

### v1.1 - TIP-88C1 v0.9 external-review correction

- Added the planned server-canonical producer-claim envelope binding, complete
  normalized broker result and broker-owned admission recomputation.
- Made evaluation and token expiry identical; closed retry lineage,
  capability/lifetime residue, variant-based key-failure classification and
  the historic-key SQL outcome.
- Added bounded reconciliation plaintext handling and custody-host readiness;
  no schema or implementation is authorized.

### v1.0 - TIP-88C1 v0.8 capture-time custody model

- Added capture-time ingress claim/idempotency, versioned keyed content
  commitment and subject token, immutable authority snapshots, fenced
  encryption attempts, and complete recoverable R1 cryptographic context.
- Added Available-only accepted-session source selection, exact preparation
  disposition arbitration, monotonic renewal, plaintext-lifetime/host posture,
  historic-key/DR lifecycle, and structural unreachable-case proofs.
- Advanced source reservation and assembly fingerprint planning codecs to v2;
  retained no bare Raw BIO digest or unrestricted locator surface.
- Closed V1 model gaps with server-authored capture acceptance/selection,
  two-edge begin/complete ingress, separate source/attempt/staged fingerprints,
  idempotent R1 key reference, complete-provisional recovery, deterministic C2
  `Preparing` pre-registration, in-Seal disposition CAS, and exact capability
  ownership.
- Made the NewCandidate `complete` branch the atomic R1 source/context commit;
  a pre-R1 `ClaimEvaluating` shell has no source identity or recovery state.
- Added distinct New/Existing evaluation-token semantics, fresh authority
  revalidation inside `complete`, and immutable monotonic shell-reclaim fields.
- Applied the v0.8-V5 checkpoint corrections: a durable alternate-key alias/
  tombstone family, stateful opaque token broker, typed dependency inputs
  re-entering `complete`, and exact token failure states.
- Applied the final v0.8-V6 planning corrections: broker-owned `complete`,
  mandatory 1–300-second DB-issued token TTL, exact alias/canonical residue and
  the closed token-variant/result transition matrix. External review remains
  required; no schema or implementation is authorized.

### v0.9 - TIP-88C1 retained-custody candidate synchronization

- Replaced the circular source fingerprint with separate pre-encryption
  reservation and post-encryption staged-ciphertext identities.
- Added the planned C1-owned immutable per-job source-binding family, exact
  retained-source reuse semantics, committed-seal replay/finalize-wins rules,
  and required B4 sealed-state compatibility.
- Normalized persisted C1 timestamps to truncated UTC microseconds and
  synchronized canonical authentication names, assembly stream/digest, and C2
  preparation identity.
- Recorded the ratified D1 retained-mode direction, pending durable-custody
  decisions, isolated credential topology, prohibited observability, and
  unchanged non-authorization boundaries.

### v0.8 - TIP-88C1 GUID canonicalization synchronization

- Pinned lowercase GUID `N` strings for every C1 hash/id preimage, matching the
  authoritative landed evidence-hash convention and call sites.
- Required an independent cross-language vector for all stable and
  attempt-scoped C1 derivations.
- Recorded this as the round-5 patch-regression correction; no runtime or
  authorization scope changed.

### v0.7 - TIP-88C1 secure raw-source planning amendment

- Added the planned metadata-only source descriptor and sealed-assembly
  boundaries for C1 without defining or authorizing a migration.
- Recorded provider-neutral/S3-reference posture, application encryption,
  immutable class/source binding, and B4/C2 handoff responsibilities.
- Carried `GOV-001` and `ART-001` through `ART-009` into exact fixture and
  real-artifact gates; no implementation or raw persistence is authorized.

### v0.6 - TIP-67B neutral verifiable proof

- Replaced the completion signing path with a neutral self-contained proof claim (`proofVersion = neutral-proof-v1`) that signs result, assurance, sorted checks, ordered evidence engines, challenge, hashed identity reference, and `signedManifestHash`.
- Added sign-time public-key material (`publicKeyJwk`, `publicKeyFingerprint`) to the manifest-row signature envelope so verification uses the key that actually signed the package.
- Confirmed the proof is downstream of `manifestHash`; `manifestBodyHash`, `packageHash`, and `manifestHash` golden vectors remain byte-identical.

### v0.5 - TIP-67A eKYC neutrality opaque challenge

- Reframed the shipped transaction-bound session data as neutral challenge-bound eKYC: `CHALLENGE_BOUND_EKYC_PROFILE`, `Challenge`, and optional `ClientReference`.
- Recorded that existing EF columns named `BindingNonceHash` and `ExternalTransactionId` are retained as compatibility storage columns only; logical/domain semantics are `Challenge` and `ClientReference`.
- Confirmed `Challenge`/`ClientReference` are echoed in public session/completion DTOs but do not enter the evidence hash chain.

### v0.4 - TIP-66 evidence-package signing

- Added S1 package-level real JWS signing over the stable TIP-65 `manifestHash` through the `IEvidenceSigner` abstraction and local non-production ES256 dev adapter with configured P12 support.
- Recorded the internal signature envelope (`signatureFormat`, `signatureScheme`, `signatureAlgorithm`, `keyId`, `signedAt`, `signatureValue`) as manifest-row/internal-manifest metadata only; public BusinessConsumer DTOs expose only the existing signature status field.
- Marked T2-2 resolved at dev-foundation level only; production HSM/KMS/CA signing, legal sufficiency, non-repudiation, replay protection, payload signing, webhook signing, and decision-basis binding remain unresolved debt.

### v0.2 - TIP-64 S1 evidence-integrity consolidation

- Added the as-built Evidence-Integrity contract for `HashCanonical`, deterministic ids, manifest/package hash chaining, completion audit hashing, and placeholder signature statuses.
- Reconciled stale signature-field names on `evidence_results` and `evidence_packages` to as-built `...SignatureStatus` fields.
- Recorded Tier-2 open items for non-JCS canonicalization and placeholder-only signatures.

### v0.3 - TIP-65 RFC 8785 JCS canonicalization

- Replaced implementation-deterministic Web JSON hashing with RFC 8785 JCS canonicalization for evidence hash/id inputs.
- Pinned timestamp/guid value formatting and recorded durable hash metadata (`packageVersion`, `canonicalizationScheme`, `hashAlgorithm`) in the package/manifest integrity surface.
- Added post-spot-check enforcement notes: hash metadata fail-closed is enforced on package/manifest read, hashed evidence graphs forbid raw JSON numbers, and S1 retains no legacy canonicalizer/corpus.
- Marked T2-1 resolved by TIP-65 and kept T2-2 placeholder signatures open.

### v0.1 - Initial logical data model

- Defined the initial provider-neutral logical entities and artifact evidence lifecycle requirements.

## Classification

- Public: non-sensitive operational metadata.
- Internal: business metadata that should not be public.
- Confidential: identifiers, correlation values, or operational secrets.
- Restricted: raw identity document data, biometrics, biometric templates, or high-risk evidence artifacts.

## Provider-Neutral Artifact Evidence Lifecycle Design Requirements

This section carries provider-neutral artifact evidence lifecycle design requirements only. It is not a schema, migration, DTO, API contract, resolver design, storage design, access-control design, audit schema, security mechanism, legal-hold implementation, or package-builder implementation.

Durable metadata fields may hold classified metadata-safe references, hashes, identifiers, and sanitized summaries only. A metadata reference is not evidence availability proof, and a package completeness candidate is not a complete package. Artifact/raw evidence persistence remains denied unless a later reviewed storage authorization packet explicitly permits a narrow classified scope.

Packet/checklist references in this LLD are requirements to carry into later review work, not approved packets:

- Storage authorization packet for `ART-001`.
- Reference resolution packet for `ART-002`.
- Package completeness packet for `ART-003`.
- Retention/expiry packet for `ART-004`.
- Purge/disposal packet for `ART-005`.
- Legal-hold sync packet for `ART-006`.
- Access/audit/security packet for `ART-007`.
- Orphan handling packet for `ART-008`.
- Provider evidence authorization packet for `ART-009`.

The design must carry these state families as planning/design requirements only:

| State family | Required states to carry |
| --- | --- |
| Reference resolution | `NotPresent`, `PresentButUnresolved`, `ResolvedAvailable`, `Missing`, `Expired`, `Deleted`, `Inaccessible`, `Unauthorized`, `Quarantined`, `OrphanSuspected` |
| Orphan handling | `NotChecked`, `NoReference`, `ReferencePresentUnresolved`, `ArtifactAvailable`, `ArtifactMissing`, `ArtifactExpired`, `ArtifactDeleted`, `ArtifactInaccessible`, `ArtifactUnauthorized`, `ArtifactQuarantined`, `OrphanSuspected`, `OrphanConfirmed`, `Reconciled` |
| Package completeness | `NotProfiled`, `ProfiledNotChecked`, `MissingRequiredClass`, `ReferenceUnresolved`, `OrphanRiskUnresolved`, `LifecycleBlocked`, `AccessBlocked`, `Quarantined`, `ReviewPending`, `CompleteCandidate`, `CompleteForReviewedUse`, `Invalidated` |
| Retention/expiry | `RetentionUnclassified`, `RetentionClassifiedNotReviewed`, `RetainedWithinWindow`, `ReviewWindowOpen`, `ReviewWindowClosed`, `Expired`, `ExpiryUnknown`, `DisputeReviewHoldPending`, `DisputeReviewHoldAccepted`, `EnvironmentMismatch`, `ExpiredReferenceNonSuccess` |
| Purge/disposal | `DisposalUnclassified`, `DisposalNotAuthorized`, `DisposalAuthorizedNotExecuted`, `DisposalBlockedByHold`, `DisposalQuarantined`, `DisposalFailed`, `DisposalPartial`, `DisposalRetried`, `DisposedTombstoned`, `ReferenceInvalidated` |
| Legal-hold sync | `HoldUnclassified`, `HoldUnknown`, `HoldCandidate`, `HoldAccepted`, `HoldConflicted`, `HoldReleased`, `HoldRejected`, `HoldStale` |
| Access/audit/security | `AccessUnclassified`, `AccessDeniedDefault`, `AccessRestricted`, `AccessApprovedPlanning`, `AccessRevokedOrExpired`, `AccessConflicted`, `AuditExpected`, `AuditMissing`, `SecurityUnproven`, `DependencyBlocked` |

Non-success states include missing, unresolved, expired, deleted, inaccessible, unauthorized, quarantined, orphan-suspected, orphan-confirmed, inconsistent, unreviewed, hold-conflicted, access-denied, audit-missing, security-unproven, dependency-blocked, and raw-payload-denied states. These states must not support evidence availability or package completeness claims without a later reviewed packet.

`ResolvedAvailable`, `ArtifactAvailable`, `CompleteForReviewedUse`, `RetainedWithinWindow`, `DisputeReviewHoldAccepted`, `DisposedTombstoned`, `HoldAccepted`, `HoldReleased`, `HoldRejected`, and `AccessApprovedPlanning` are narrow packet-scoped states only. They are not general readiness, capability, implementation, evidence availability, package completeness, legal, audit, security, production, pilot, certification, or support claims.

Raw payload collection and persistence are denied by default. Provider-specific evidence collection requires a later reviewed provider evidence authorization packet and must STOP/RRI before any exception. Restricted artifact access requires a later reviewed access/audit/security packet and must STOP/RRI before access is treated as authorized.

STOP/RRI is required before runtime implementation, provider-specific evidence collection, raw payload handling, artifact/raw evidence persistence, restricted artifact access, or any claim that this LLD section provides readiness, legal, audit, security, production, pilot, certification, support, evidence availability, package completeness, or capability proof.

Existing sequence, API, and adapter LLD wording that mentions vault, storage, package, or artifact handling is governed by this lifecycle section. Those existing mentions do not authorize artifact/raw evidence persistence, raw payload handling, resolver capability, package completeness, restricted artifact access, evidence availability, or runtime implementation.

`GOV-001` branch/deferred-scope traceability and `ART-001` through `ART-009` must be carried until later reviewed TIPs resolve them beyond planning/design requirements.

### TIP-88C1 planned logical source and assembly model

TIP-88C1 plans capture-time retained-source metadata independently of later
B3/B4 export. Exact schema remains Build-Brief blocked.

Every phase/residue/retry/body/admission statement in this C1 subsection is a
non-normative projection of Planning section 10.0; if wording differs, section
10.0 wins. Each CaptureAgent invocation uses one external authenticated
operation shape per artifact; a retry invokes the same shape again with the
same UUID, with no external begin/upload-part/complete sub-protocol.
Metadata-only outcomes finalize without `AdmissionAccepted`, body or a new
R1/attempt/key/object. Only committed New/same-owner/reclaim progression emits
admission and receives one bounded body. The claim token,
begin/broker/complete/R1–R6 ceremony, `InternalClaimResult` and all intermediate
rows are internal; there is no upload-session, part, receipt, resume,
public-complete or agent-held provider capability. Pre-admission body
transmission/prebuffer is protocol-invalid; every proxy disables request/disk
buffering. Kernel/TLS residual bytes are bounded, memory-only and never claimed
as zero.

| Family | Planned identity/state | Forbidden durable content |
| --- | --- | --- |
| Capture acceptance | Server-authored append-only acceptance event plus immutable session/class selection with exact acceptance id, artifact id and positive revision | CaptureAgent/public caller selection; landed `QualityState` treated as final authority |
| CaptureAgent buffer admission | Exact per-class declared size; one local retained-buffer slot; exact declared-byte reservation against the host aggregate; local readiness only | Dynamic server attestation/assertion; server claim to enforce agent memory; unbounded process-local buffer; implicit default; leaked retained buffer after exit |
| Custody stream admission | One producer/deployment stream slot plus exact configured plaintext-window reservation against the deployment aggregate; every server-owned admission has bounded release/expiry | Artifact-size inference from the window; unchecked/unbounded allocation; worst-case product relation that subsumes aggregate admission; leaked admission after terminal exit |
| Ingress transport admission | Planning section 10.0 P0–P7 branch; metadata-only final or committed R1/re-entry CAS then transport admission; bounded memory-only pre-admission network residual | Body/admission/new R1 on metadata-only final; body transmitted/read/prebuffered before admission; proxy/request buffering; disk spill; second application call |
| Ingress claim alias | One durable row per authenticated client/producer/instance/UUID key; state Evaluating/Bound/ConflictTombstone; attempted exact-artifact identity; nullable canonical-claim FK; immutable content-free v2 `ProducerClaimEnvelopeFingerprint`; one CAS-replaceable current evaluation id/owner/disposition/issue/expiry/token schema/variant/audience/digest; monotonic non-reused revision/fence and latest-issued expiry | Raw token, SourceArtifactId, plaintext, bare digest/content-derived envelope value, locator, credentials; replacement of a live slot; counter/horizon reset; deletion/rebinding while prior token may live |
| Canonical ingress claim | One instance-independent exact-artifact row and one source; NewCandidate complete atomically performs R1; alternate exact replay binds alias to this row; conflict burns alias while canonical row remains unchanged | Second canonical source for exact artifact; alias key stored as the sole canonical identity; conflict disclosure |
| Source reservation/attempt | Source-stable reservation fingerprint; per-attempt fingerprint/row; subject token; keyed commitment; authority snapshot; owner/revision/fence; monotonic `R2TerminationDisposition ∈ {TerminatedBeforeStart, Terminated}` and termination time; first disposition-required CAS timestamp plus derived non-resettable expiry; complete suite/framing/nonce/provisional-object/idempotent-key-reference context; effective lifetime | Plaintext, unwrapped DEK, KEK/provider credential, bare digest; retry/reclaim/rotation reset of disposition clock |
| P4 rejected reservation generation | Bound alias + canonical idempotency identity; `AdmissionProtocolRejected`; `R2TerminationDisposition = TerminatedBeforeStart`; monotonic reservation/attempt/fence lineage; exact non-arming, cleanup/capacity and disposition-clock evidence | Alias deletion/rebinding; zero-R1 claim; retained provisional object/key; body acceptance; reuse by different owner; failure to count P4 as a settled prior R2 |
| Internal claim result | NewReservation, ExistingMatch, ReservationReclaimed or exact internal claim outcome | Serialization/public DTO; CaptureAgent egress; final-result reuse |
| CaptureAgent final result | Available, AlreadyAvailable, ClaimEvaluationInProgress with exact RetryNotBeforeUtc, or closed OutcomeOnly code | New/Existing/Reclaimed, owner/lease/revision/fence/token, inferred fields |
| Staged/Available source | Exact staged ciphertext fingerprint, restricted locator/envelope/key metadata and append-only lifecycle evidence | Raw/plaintext/ciphertext object bytes in PostgreSQL; locator outside restricted repository |
| C1 job-source binding | Immutable `(JobId, Ordinal, RawClass, CaptureAcceptanceId, CaptureArtifactId, CaptureRevision, SourceArtifactId)` from exact session selection | Mutable/first/latest replacement, inherited authority |
| Sealed assembly | Job-stable v2 identity/items; attempt manifest/authentication; exact C2 preparation | Plaintext package, bare item digest, locator/delivery handle |
| Preparation disposition | Deterministic id registered `Preparing` before C2 I/O; exact `(AssemblyId,C2PreparationId)` monotonic pending/seal/finalize or abort states | Unregistered orphan, scan-only/direct delete, cross-preparation finalize |

The current claim-evaluation disposition is the exact monotonic set
`Active | Completed | Expired | Reclaimed | Conflict`, with only `Active`
non-terminal. Canonical source `CurrentDisposition` is exactly
`ClaimEvaluating | Reserved | Encrypting | Staged | Available |
RecaptureRequired | ContentCommitmentMismatch | SourceEncryptionFailed |
Quarantined | Deleted`. C2 preparation is exactly
`Preparing | Pending | SealCommitted | AbortAuthorized | Finalized | Aborted`.
Unknown/default values fail closed. The B4 operational head retains its landed
13-value closed set and C1 may publish only `AssemblySealed`.

Ingress identity is server-known and distinct from producer claims. CaptureAgent
claims digest/length/media/capture time and buffer lifetime; custody persists
only a dedicated versioned HMAC content commitment plus schema/key version.
Before `begin`, a trusted server canonicalizer binds normalized identity,
length, media, capture and retention claims to content-free envelope v2 and
persists only its session-scoped fingerprint on the alias. The digest is
excluded; keyed commitment at `complete` and actual R3 verification own content
binding. A v1 record remains v1-only with no silent reinterpretation.
No persisted ingress/descriptor artifact may be recomputable from database
values plus candidate plaintext into a membership answer without a protected
key; putting digest/content back into v2 is a required RED mutation.
`begin` freezes the active/historic key selector in durable internal state and
first claims the unique alias key. The selector is not returned to ingress. A
NewCandidate receives an opaque single-use token bound to alias/
shell identity; an ExistingCandidate receives a bounded idempotently replayable
token bound to alias/canonical identity. Token bytes are 32-byte CSPRNG material;
only SHA-256 digest plus schema, variant, audience, DB-issued UTC-microsecond
issue/expiry, alias identity, producer-envelope fingerprint and one current
evaluation slot persist. A live slot is never replaced: concurrent/repeated
begin after an internally lost result returns
`RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` with exact
`RetryNotBeforeUtc = CurrentTokenExpiresAtUtc`, while lookup timeout alone is
`RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY`. Expired/terminal/reclaim-eligible or
expired Bound-alias reissue CASes a fresh non-reused evaluation/token/owner/
times/digest, strictly increases revision/fence and advances the monotonic
latest-issued expiry. A separate claim-
comparison broker validates that stateful token, internally derives exactly one
typed `DerivedAdmission` containing the recomputed envelope fingerprint,
commitment tuple and normalized length/media/capture/retention fields, or an
active-key-unavailable/historic-key-unavailable/token-invalid result and invokes
`complete` itself. It supplies no trusted final `AdmissionFingerprint`; broker-
owned `complete` checks envelope equality and recomputes the canonical admission
from persisted identity and normalized claims. The result never crosses to
ingress; ingress cannot call the validator/`complete`, choose a selector or
access the commitment-key registry directly.
The presented capability includes opaque token bytes plus non-secret evaluation
id/revision/fence/variant/expiry metadata; stale lineage is distinguishable as
restart without persisting an old raw token or digest history.

Broker-owned `complete` checks the exact token/alias/row/revision/fence and freshly
revalidates current actor/client/session/acceptance/retention authority before
mapping the typed broker result, comparison, disclosure or allocation. For
NewCandidate, that same `complete` transaction CAS-consumes the shell token,
binds the alias and performs R1. Existing exact match atomically binds the alias
to the unchanged canonical claim; mismatch burns `ConflictTombstone`. Therefore
A(K1) New → A(K2) Existing → B(K2) Conflict creates no B shell/source. New
post-begin residue is one Evaluating alias plus one non-source canonical shell;
Existing-alternate post-begin residue is one Evaluating alias plus the unchanged
canonical claim; pre-begin residue is zero alias/shell. Reclaim
CAS-replaces the current slot on the same alias/shell with a fresh evaluation,
strictly increases revision/fence/latest-expiry and never deletes/reinserts it
while a token could remain valid. A committed
`ClaimEvaluating` shell has no
`SourceArtifactId`, reservation, attempt, object, or key reservation. Timeout
preserves the applicable alias/canonical residue. R2 independently verifies
actual content.
Rotation, authority change, stale-token reclaim and instance restart cannot
create or disclose a second source; a missing historic key creates no new
source. Tampered/wrong-bound tokens fail
`RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID`; expired/stale/reclaimed tokens fail
`RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED`. For valid tokens, fresh authority
precedes active/historic key-unavailable outcomes. Result type is selected by
token variant, not current registry status: Existing produces
`RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE`, including when its
stored version equals the active version. New content-free v2 envelope/lineage mismatch is
token invalid; only a correctly bound Existing comparison can produce business
fingerprint conflict. The mandatory
`RawExportSourceClaimEvaluationTokenTtlSeconds` is an integer in 1–300 with no
default. DB `statement_timestamp()` truncated to UTC microseconds issues the
token; evaluation issue/expiry equals token issue/expiry exactly, the complete
timeout budget must be strictly smaller than TTL, and unbound cleanup waits
beyond monotonic latest-issued expiry plus a mandatory 1–300-second margin.
Claim lock/comparison/complete/safety/tombstone configs are mandatory,
no-default, bounded and checked; latest-issuance cleanup is at most 600 seconds
after that issuance. Same-token retry additionally requires unchanged alias/
evaluation/owner/revision/fence lineage.
One lost-result path is readiness-valid only when one full token TTL plus
idempotency-lock, comparison, complete, encryption-attempt and safety budgets
fit strictly inside maximum producer plaintext retention. CaptureAgent waits
to the retry boundary only while the same remaining-budget projection holds;
otherwise it zeroizes and requires recapture.

Atomic NewCandidate `complete`/R1 persists every essential recovery reference
before provider/key/object I/O:
attempt revision/fence, provisional identity, suite/framing/nonce context and
an idempotent `AttemptKeyReservationId`; no key-provider I/O occurs under the
DB transaction. The attempt row round-trips
`SourceEncryptionProfileId/Version`, suite/framing,
`NonceStrategyId`, recoverable seed reference/wrapped seed, seed commitment,
`ChunkSize`, `FramingParametersDigest`, provisional identity and key reference;
a fingerprint digest never substitutes for those recovery inputs. Exact
provisional inspect/read plus an authenticated completion
record lets a fenced reconciler recover complete ciphertext, bounded-decrypt to
recompute length/commitment and execute R3 without restarting R2. R3 persists
only actual post-encryption results and the staged ciphertext fingerprint.
Provider visibility remains provisional → staged → committed → metadata `Available`.
Only `Available` descriptors are resolver-readable after `ART-002`; PostgreSQL
stores metadata, never raw/plaintext/ciphertext object bytes.

The current conceptual `vault_objects` entity is not promoted, reused, or
treated as the C1 source-of-truth by this planning amendment. A future reviewed
Build Brief must either create the v0.8 restricted C1 families or explicitly
supersede the v0.8 restricted families under Homeowner authority; generic `vaultRef` or
`storageUri` fields are not C1 capability.

Source identity, encryption attempts and staged ciphertext are distinct:
`SourceReservationFingerprint` is source-stable,
`EncryptionAttemptFingerprint` rotates with attempt/fence/key/object context,
and `StagedCiphertextFingerprint` binds that exact attempt plus actual output.
Assembly identity/content and attempt metadata are distinct. `AssemblyId`,
`AssemblyFingerprint`, `AssemblyDigest`, and the exact ordered item content are
job-stable for unchanged immutable sources. `AttemptId`, fence, attempt
timestamp, `ManifestDigest`, authentication value, C2 idempotency fingerprint,
and deterministic `C2PreparationId` are attempt-scoped. The id is registered
`Preparing` before external I/O; same C2 fingerprint returns exact
created/existing match and exact-id lookup resolves lost responses. A distinct
fingerprint yields a distinct preparation id. The exact source-reservation/
encryption-attempt/staged/assembly-fingerprint preimages,
historic codec registry, keyed commitment/subject-token vectors, and
golden vectors are owned by the TIP-88C1 Planning/Build Brief and must use the
landed RFC-8785 JCS/SHA-256 evidence-canonicalization conventions. Only the C2
preparation referenced by the committed seal can finalize.
The C1 keyed-commitment runtime manifest has exactly two positive vectors,
`ChipDg2Portrait` and `LiveSelfieImage`; `LivenessMedia` is an unsupported-class
negative, not a third positive vector.
Attempt timestamp and `AssemblyAuthenticationValue` are created once and reused exactly
for retry of that attempt; an ambiguous authentication-provider result fails
the attempt closed instead of being recomputed under the same attempt identity.
Every GUID entering a C1 hash/id preimage is a lowercase `N` string. Timestamps
are converted to UTC, truncated to whole microseconds before hashing and
persistence, and formatted with exactly six fractional digits plus `Z`.
Every free-text scalar is valid Unicode normalized exactly once to NFC before
UTF-8; invalid Unicode is rejected. Integers are invariant decimal strings,
enums use exact names, hashes use
lowercase `sha256:<hex>`, arrays preserve declared order, and canonical
authentication names are `AssemblyAuthenticationKeyId`,
`AssemblyAuthenticationKeyVersion`, and `AssemblyAuthenticationValue`. An
independent implementation must reproduce ingress/admission identity, keyed
content commitment and subject token with public fixture-only HMAC keys, all
three source/attempt/staged fingerprints, the
deterministic assembly id, stable fingerprint, non-self-referential canonical
assembly stream/digest, attempt manifest, and C2-idempotency vector through
database readback.

The planned worker uses single-flight landed B4 renewal during external I/O,
stops/joins periodic renewal, rejects stale/lower responses, and may issue at
most one synchronous final renewal before Seal. No database lock spans external
I/O. Pre-Prepare and in-Seal authority checks are distinct. Exact
`Pending → SealCommitted` preparation-disposition CAS occurs inside the same
transaction as assembly identity/items, B4 head and transition; never a
scan/deadline alone selects finalization or abort.

Only `Available` descriptors can freeze into a B4 mapping. Landed
`capture_artifacts`/`QualityState` is not authoritative. A required additive
server-authored acceptance-event family and immutable session/class selection
provides exact `CaptureAcceptanceId + CaptureArtifactId + CaptureRevision`;
same-source concurrent freeze existing-matches and different-source freeze
conflicts. That surface/producer must be ratified before Build Brief.
Pre-publication ingress outcomes therefore have a structurally unreachable
frozen-B4 case. Later loss of an Available frozen source is
`RAW_EXPORT_SOURCE_UNAVAILABLE`.

Every source/evidence row references a versioned immutable authority snapshot.
Loss before `Available` blocks publication; later loss blocks read/reuse/
delivery. Physical purge versus legal hold remains policy-gated.

CaptureAgent owns its monotonic plaintext deadline; server effective expiry is
the minimum of the producer projection and configured continuation cap.
After token/envelope/lineage and fresh-authority checks, `complete` requires the
effective producer/server-capped expiry to remain strictly later than its DB
statement timestamp by more than
`EncryptionAttemptDeadline + SafetyMargin`; equality fails closed
without consuming the token or creating provider/key/object residue. Token TTL
and its already-contained comparison/complete budgets are not added again to
that remaining-lifetime formula.
Healthy producer ownership excludes reconciliation. Graceful terminal paths
execute zeroization; abrupt loss makes no erasure claim. A reconciler may
bounded-decrypt complete ciphertext but never reconstructs the producer buffer:
it holds at most one configured plaintext chunk and one unwrapped attempt key,
zeroizes/disposes both on every graceful exit, and is governed by separate
custody-reconciliation host-posture readiness with exact code
`RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID`. Historic
content-commitment/subject-token keys require HA, escrow/backup, DR, capacity,
retirement and referenced-version readiness. Production host posture is a
fail-closed readiness gate.

A dead external call retains its owner marker. Same-owner re-entry requires the
exact authenticated ingress owner/identity, a CAS match on the current
reservation revision/fence, fresh authority/time predicates and durable
`PreviousFencedR2Terminated`: request reader exited, provider writer quiesced,
exact provider attempt terminally acknowledged/inspected and the old fence CAS
recorded `Terminated`, or P4 proves neither reader nor writer armed and records
`TerminatedBeforeStart`. `SameOwnerRetryPendingTermination` cannot create a new
attempt; `SameOwnerReentryReady` can. Expired-owner reclaim has the same
pending/ready split. Different owner, stale CAS or unproven settlement remains
reservation busy or recapture when the full continuation path cannot fit.
Four mandatory Int32-second no-default `[1,3600]` settings budget reader
quiescence, provider-writer quiescence, provider abort-or-inspection and
termination CAS. Their checked sum is `PreviousR2TerminationBudget`. Runtime
uses the Planning section 15.1 five-state × three-preceding-wait path
enumeration. Pending cases include prior termination and cannot replace the
attempt; ready cases omit it. Both expired-owner cases include
`ReclaimExecutionBudget`. P4 `TerminatedBeforeStart` and armed-R2 `Terminated`
make `PreviousFencedR2Terminated` durable. Lease and reconciler minimum age
share the exact owner-CAS reservation/attempt timestamp; a token or busy
backoff can start later and is sequential in readiness. CaptureAgent includes
the future wait before sleeping; custody uses DB time after waiting and its
post-wait projection contains no wait term.

Three mandatory no-default limits bind active attempts/source, key-material
records/source and provisional objects/source independently. One mandatory
common busy backoff governs capacity-unavailable, idempotency-busy and
reservation-busy. `MaximumRetryCountPerIngress` remains an unpinned proposed
bound behind `C1-BB-RETRY-COUNT-ACCOUNTING-GATE`; no schema/counter or durable
cross-process semantics are implied until the Build Brief answers all nine
accounting questions. The first disposition-required CAS
persists `DispositionStartedAtUtc` and
`DispositionExpiresAtUtc = DispositionStartedAtUtc + DispositionEnvelope`;
retry, reclaim, restart and rotation cannot reset them. Exact schema remains a
future Build-Brief responsibility.

The continuation cap comes only from mandatory integer
`RawExportSourceMaximumRemainingContinuationWindowSeconds` in `[1,3600]`, with
no default. It must strictly contain claim lock/comparison/complete,
conditional `PreviousR2TerminationBudget`, attempt deadline and safety, and be
no greater than maximum plaintext-retention budget. Every
`C1HashCanonical` value uses LP domain/scalars and LP-counted ordered arrays.
This is a new planning compatibility profile and does not reinterpret the
as-built Evidence-Integrity `HashCanonical` JCS contract below.
Planning D4.3 has separate closed unions. `InternalClaimResult` never crosses
custody ingress. `CaptureAgentFinalResult` contains only
Available/AlreadyAvailable, ClaimEvaluationInProgress with exact
`RetryNotBeforeUtc`, or one enumerated OutcomeOnly code. NewReservation,
ExistingMatch, ReservationReclaimed, owner/revision/fence and internal outcomes
cannot egress.
Planning section 10.0, not this LLD, owns their exact phase, body/admission,
residue and retry mapping.

The ratified D2 class set is exactly `ChipDg2Portrait` and `LiveSelfieImage`.
`LivenessMedia` is deferred to a separately ratified resumable multipart
transport and AEAD `ChunkSize` is never a transport part. Eight mandatory,
no-default integer settings split capacity by physical host: two class ceilings,
CaptureAgent retained-buffer slots and aggregate declared bytes, and custody
plaintext window, producer/deployment stream slots and aggregate window bytes.
Each class ceiling fits the agent aggregate; the custody window fits the
custody aggregate; producer streams do not exceed deployment streams. There is
no product-cover readiness relation, so an exact aggregate reservation can bind
while an adjacent slot remains. CaptureAgent local exhaustion never starts the
external operation. Server code directly enforces custody capacity and only
refuses oversized declarations; it receives no agent capacity assertion/
attestation and does not enforce CaptureAgent process memory. Declared oversize or
unavailable capacity fails before begin/provider/key I/O. R2 independently
counts bytes and aborts at the first excess byte; no oversize source can become
Available. Invalid configuration is
`RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID`; runtime outcomes are
`RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` and
`RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`.

P4 exists only before successful `AdmissionAccepted` write/flush with reader
and writer unarmed; successful write/flush or reader arming is P5, without any
client-observation predicate. P4/P5 transport abort/cancellation/incomplete body
is non-terminal, non-replay-stable
`RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` and cleans the exact fenced R2
residue. The next same-UUID invocation returns reservation-busy while
termination is not durable, re-enters when durable and the exact case budget
fits, or requires recapture when budget is insufficient or the buffer is lost.
P4 uses `TerminatedBeforeStart`; P5 proves
all four durable termination conditions. Clean EOF with a shorter length or other authenticated
claim mismatch is terminal `CONTENT_COMMITMENT_MISMATCH` and refuses same-UUID
retry. A declaration can be corrected on the same UUID only after pre-begin
zero-residue rejection. After R2 begins it is frozen: class-valid
actual-over-declared bytes require a new accepted capture/revision and UUID;
actual-over-class bytes have no correction path.

Mandatory `RawExportIngressMaximumPreAdmissionBufferedBytes` and qualified
proxy/middleware posture bound the honest kernel/TLS memory-only residual.
Missing phase preservation, enabled prebuffer/disk spill or an invalid bound
fails readiness as `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID`; runtime early
body returns `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`.
Planning section 10.0 distinguishes P0–P3 zero committed-R1 residue from P4
Bound alias/canonical preservation and exact attempt/object/key/capacity
cleanup; no blanket zero-residue rule spans both.

Landed B4 predates `AssemblySealed` lease finality and needs a separately
reviewed additive early exclusion. Implementation ownership remains pending.

The Homeowner-ratified mode direction is `EncryptedRawVaultRetained`; it permits
multiple independently authorized jobs to bind the same retained source but
does not authorize capture, persistence, reuse, build, or production. Its exact
controller/legal/retention/purge/hold/access/B4/delivery sub-decisions remain
pending, and D3–D9 remain unratified. D2 is ratified only for
`ChipDg2Portrait` and `LiveSelfieImage`; `LivenessMedia` remains deferred. The
reference adapter is S3-compatible and may use only an
exact pinned MinIO version/digest for reviewed generated, non-patient fixtures.
It is not a production provider approval. Encrypted filesystem remains a
separately qualified optional topology.

Before fixture evidence, a reviewed composite authorization must carry
`GOV-001` and every `ART-001` through `ART-009` row and the applicable
TIP-38-through-TIP-46 storage, resolution, orphan, retention/expiry,
purge/cleanup, hold-conflict, access/audit/security, and raw-payload fields.
Fixture-only dispositions do not close real-artifact gates. Before real capture
persistence, `ART-001`, `ART-004`, `ART-005`, `ART-006`, `ART-007`,
`ART-008`, and `ART-009` require reviewed resolution; `ART-002` gates source
availability reliance and `ART-003` remains C2 package-completeness scope.

No provider credential or source KEK authority may exist in the public/general
API process. Ingress owns begin/provisional write but no validator, `complete`
or commitment-key-registry edge. The claim-comparison broker owns stateful token
validation, exact token-bound active/historic derive and broker-only `complete`;
it accepts no caller-selected selector and cannot enumerate/export keys.
Reconciliation owns
exact-provisional inspect/read plus historic verify/unwrap; assembly owns
committed-`Available` read plus historic verify/unwrap; lifecycle owns
delete/hold without read/HMAC/unwrap. Missing or extra capability edges fail
readiness. Alias/function ACLs, token CSPRNG/digest/audience/TTL and the broker
validator/derive/complete edges are part of exact readiness. Locator/key/credential/raw/subject content is default-deny on
manifests, results, errors, logs, traces, metrics, audit payloads, diagnostics,
crash dumps, dead letters, and public surfaces, subject only to the narrow
restricted repository/adapter/key-record exceptions in the Planning Brief.
Fixture evidence cannot be represented as real-artifact, production,
legal/compliance, audit, security, readiness, performance, retention-policy,
or production-provider qualification.

Status is `PLANNING PATCHED — INDEPENDENT REVIEW REQUIRED — IMPLEMENTATION
BLOCKED`. This subsection is planning synchronization only. It is not a schema,
migration, runtime port, provider selection, packet approval, raw-payload
authorization, evidence-availability proof, package-completeness proof, or
production/readiness claim.

## Evidence-Integrity

This section describes the as-built S1 evidence-integrity behavior in `VerificationCompletionApplicationService`, the domain records, and the internal manifest/BusinessConsumer contracts. Code wins over older TIP wording. This section is persistence-agnostic except for naming the integrity metadata that TIP-65 persists on package/manifest rows; it does not define append-only triggers, durability behavior, provider-specific storage behavior, or raw artifact lifecycle behavior.

### Canonicalization

`HashCanonical(label, value)` serializes `value` through the application evidence canonicalizer, pins non-JSON-native values, canonicalizes the resulting JSON with RFC 8785 JCS, then computes SHA-256 over the UTF-8 bytes of:

```text
{label}
{jcsCanonicalJson}
```

The returned hash format is `sha256:<lowercase-hex>`, enforced by `HashRef`.

JCS canonicalization sorts object member names by ordinal UTF-16 code unit order, emits minimal JSON whitespace, and uses RFC 8785 string/number rules. Property declaration order is no longer part of the canonical hash contract; tests assert field names and canonical output.

Existing as-built S1 evidence timestamps are converted to UTC before
canonicalization and formatted as `yyyy-MM-ddTHH:mm:ss.fffffffZ` using invariant
culture and exactly seven fractional digits. The planned C1 persisted/replayed
identity contract is an explicit compatibility profile: convert to UTC,
truncate ticks to a whole microsecond before hashing/persistence, and format
`yyyy-MM-ddTHH:mm:ss.ffffffZ`. It does not alter existing evidence hashes.
Guids are formatted consistently with the `N` format where they enter hash/id
inputs as strings.

For `rfc8785-jcs-v1`, hashed evidence graphs do not use raw JSON numbers. Numeric values that enter any hash/id evidence seed MUST be encoded as strings before canonicalization: integer values use `ToString(CultureInfo.InvariantCulture)`, and fractional/score values use `decimal` formatted as `F6` with invariant culture. Scale 6 is part of `rfc8785-jcs-v1`. The as-built S1 hashed evidence objects are string/bool/null/object/array only; tests parse every actual hash/id seed and fail if a `JsonValueKind.Number` appears. The canonicalizer's number formatting remains defensive general-purpose code and rejects NaN/Infinity, but S1 evidence is not allowed to rely on it.

The active TIP-65 metadata constants are:

| Field | Value | Surface |
| --- | --- | --- |
| `packageVersion` | `evidence-package-v2` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |
| `canonicalizationScheme` | `rfc8785-jcs-v1` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |
| `hashAlgorithm` | `sha256` | Manifest body, package hash input, `EvidencePackage`, internal `EvidenceManifestDto`, package/manifest persistence rows |

The public BusinessConsumer `EvidencePackageSummaryDto` remains unchanged by TIP-65: it still exposes `PackageVersion`, but it does not expose `canonicalizationScheme` or `hashAlgorithm`. The internal manifest DTO is the S1 verifier metadata surface.

### Deterministic Ids

`DeterministicGuid(label, value)` uses the same `"{label}\n{jcsCanonicalJson}"` input style as `HashCanonical`, SHA-256 hashes it, copies the first 16 bytes, sets the GUID version nibble to 5, sets the RFC 4122 variant bits, and returns a `Guid`.

The as-built labels and input fields are:

| Derived id | Label | Input field set |
| --- | --- | --- |
| `decisionId` | `tip-06-decision` | `SessionId`, `EvidenceIds`, `Result`, `AssuranceLevel`, `ForceReview`, `RequestId`, `CorrelationId`, `CompletedAt` |
| `evidencePackageId` | `tip-06-evidence-package` | `SessionId`, `DecisionId` |
| completion `auditEventId` | `tip-06-completion-audit` | `SessionId`, `PackageId` |

TIP-65 changes deterministic-id canonicalization to `rfc8785-jcs-v1`; legacy ids produced under `web-json-deterministic-v1` remain historical/localdev values and are not re-derived or mutated.

### Hash Chain

The S1 completion path builds a three-step hash chain:

| Hash | Label | Input field set |
| --- | --- | --- |
| `manifestBodyHash` | `tip-06-manifest-body` | `EvidencePackageId`, `VerificationSessionId`, `PackageVersion`, `CanonicalizationScheme`, `HashAlgorithm`, `EvidenceRefs`, `AuditEventRefs`, `ResultRef`, `Result`, `AssuranceLevel`, `RequestId`, `CorrelationId`, `CreatedAt` |
| `packageHash` | `tip-06-evidence-package` | `EvidencePackageId`, `VerificationSessionId`, `PackageVersion`, `CanonicalizationScheme`, `HashAlgorithm`, `ManifestBodyHash`, `ResultRef`, `EvidenceRefs`, `Result`, `AssuranceLevel`, `CreatedAt` |
| `manifestHash` | `tip-06-evidence-manifest` | `BodyHash`, `PackageHash` |

`PackageVersion` is `evidence-package-v2`; `CanonicalizationScheme` is `rfc8785-jcs-v1`; `HashAlgorithm` is `sha256`. `ResultRef` is the deterministic final decision id. In `manifestBodyHash`, `EvidenceRefs` are `ManifestEvidenceRefDto` values ordered by `ResultType` then evidence result id, with fields `Type`, `Id`, `VaultRef`, `ArtifactHash`, and `PayloadHash`. `VaultRef` is `null` in the as-built completion service. `AuditEventRefs` are `ManifestAuditRefDto` values with `EventId`, `EventType`, and `EventPayloadHash`.

`packageHash` uses only the selected evidence result ids for `EvidenceRefs`, in the selected evidence order used by the completion service, not the full manifest evidence-ref objects.

### Audit Hashing And Manifest Audit Refs

The as-built completion path creates exactly one completion audit event:

| Audit event | Value |
| --- | --- |
| `EventType` | `VERIFICATION_COMPLETED` |
| Event id label | `tip-06-completion-audit` |
| Payload-hash label | `tip-06-completion-audit-payload` |
| Payload-hash fields | `SessionId`, `DecisionId`, `EvidencePackageId`, `Result`, `RequestId`, `CorrelationId`, `CompletedAt` |

The manifest `AuditEventRefs` are all existing audit events for the verification session plus the new completion audit event, converted to `ManifestAuditRefDto(EventId, EventType, EventPayloadHash)`, then sorted by `EventId` using ordinal string ordering. The `EvidencePackage.AuditEventRefs` list stores the sorted audit event ids.

> note: TIP-06 section 16 described a three-event audit model (`FINAL_DECISION_CALCULATED`, `EVIDENCE_PACKAGE_CREATED`, `SESSION_COMPLETED`), a pre/post audit-ref split, and exclusion of prior events from the manifest/package refs. That model is not as-built in S1; code wins.

### Signature-Status And Package Signing Model

`SignaturePlaceholderStatus` has two values after TIP-66: `PlaceholderUnverified` and `Signed`. New TIP-66 evidence packages receive `EvidencePackageSignatureStatus = Signed`; legacy/pre-TIP-66 packages remain `PlaceholderUnverified` and must not be synthesized as signed.

As-built signature-status locations:

| Location | Field | Meaning |
| --- | --- | --- |
| `EvidenceResult` | `PayloadSignatureStatus` | Per-evidence payload signature status marker only. |
| `EvidencePackage` and package DTOs | `EvidencePackageSignatureStatus` | Package-level status. `Signed` means the package has a TIP-66 internal JWS envelope over the package manifest hash. |
| `VerificationCompletedEventDto` | `WebhookSignatureStatus` | Completion-notification projection marker only; not a persisted webhook delivery field. |

TIP-67B signs a stable attached compact JWS neutral proof claim after `manifestHash` is computed. The claim is a TIP-66-compatible superset carrying `proofVersion`, `purpose`, `sessionId`, hashed `identityRef`, `packageId`, `packageVersion`, `canonicalizationScheme`, `hashAlgorithm`, `result`, single `assuranceLevel`, sorted `requiredChecks` and `completedChecks`, ordered `evidenceEngines`, `signedAt`, `challenge`, `signedManifestHash`, `resultHash`, `resultHashAlgorithm`, and `resultHashCanonicalizationScheme`. The local dev ES256 adapter loads a configured P12 key from `TagEkyc:EvidenceSigning` when provided and otherwise uses an in-process non-production generated key for local/dev execution. The signer generates `signedAt` once and writes the same pinned UTC value into both the JWS payload and the manifest-row envelope. Signing is downstream/additive and does not feed back into `manifestBodyHash`, `packageHash`, or `manifestHash`; TIP-65 golden hash vectors remain byte-identical.

`identityRef` is always `sha256:<hex>` over `tip-67b-identity-ref-v1\n{clientApplicationId:N}\n{subjectRef}`. Raw `SubjectRef` is not exposed in the verification view or signed JWS. `resultHash` is `sha256:<hex>` over `{label}\n{JCS(preimage)}` with label `tip-67b-neutral-proof-result`; the preimage excludes `resultHash`, `resultHashAlgorithm`, `resultHashCanonicalizationScheme`, and `signatureValue`.

The signature envelope is internal verifier metadata with:

| Field | Meaning |
| --- | --- |
| `signatureFormat` | `JWS` format marker, not the algorithm. |
| `signatureScheme` | `jws-es256-v1` profile/version id. |
| `signatureAlgorithm` | `ES256` algorithm selector. |
| `keyId` | Signing key id from the JWS protected header `kid`. |
| `signedAt` | UTC signing timestamp, normalized to microsecond precision (see note below). |
| `signatureValue` | Attached compact JWS value. |
| `publicKeyJwk` | Sign-time public JWK with public params only: `{kty,crv,x,y}`. |
| `publicKeyFingerprint` | `sha256` over RFC 8785 JCS of the public JWK. |

> **TIP-66/TIP-67A note:** `signedAt` in the JWS claim and signature envelope is normalized to PostgreSQL-compatible microsecond precision before signing/persistence so persisted-readback verification is stable. This does not alter the evidence hash chain.

The envelope is persisted on the authoritative internal manifest row and exposed through the internal `EvidenceManifestDto` only. It is not duplicated onto the package row and is not exposed through the public BusinessConsumer package summary. The package row and manifest row both retain the existing `EvidencePackageSignatureStatus` status field and must be written consistently in the same finalization transaction.

Verifier rule: a verifier selects by the recorded `signatureFormat`/`signatureScheme`/`signatureAlgorithm` plus the JWS protected header `alg`/`kid`, verifies the JWS with the sign-time public JWK after matching an out-of-band pinned `kid` and `publicKeyFingerprint`, recomputes `resultHash`, and cross-checks the mirrored verification-view fields against the decoded signed claim. Unknown proof version, scheme, algorithm, header-vs-envelope mismatch, wrong key, forged JWS with attacker JWK, private JWK material, tampered view fields, resultHash mismatch, missing envelope for `Signed`, envelope material on a placeholder row, or claim-vs-row mismatch fails closed. TIP-67B implements this at **dev-level** (real ES256 JWS over the neutral proof using a local non-production key/P12 — NOT production HSM/qualified signing) plus a public verification view and reference consumer tests, not as a runtime verifier endpoint. **Production HSM signing custody is TIP-68 (pending);** consistent with line 223 (TIP-66 is a non-production dev/P12 foundation).

`webhook_deliveries` is a deferred/conceptual entity in S1. The as-built runtime has a completion-notification projection DTO, not webhook dispatch or persisted webhook-delivery signing.

### Verifier And Legacy Rule

A verifier MUST select canonicalization and hashing by the package's own `packageVersion`, `canonicalizationScheme`, and `hashAlgorithm`, never by the latest runtime default. Unknown or inconsistent combinations fail closed. In the as-built EF read path, package and manifest rows are classified while mapping to the domain/internal manifest DTO; an unknown tuple throws a typed hash-metadata error and the package/manifest is not returned.

Legacy packages produced before TIP-65 are `packageVersion = tip-06-localdev-v1`, `canonicalizationScheme = web-json-deterministic-v1`, `hashAlgorithm = sha256`. They are historical/localdev compatibility evidence only, are not JCS-compliant, and are not production/legal-reliance evidence. If re-issued under JCS, a legacy package receives a new package/version/hash; old hashes are not mutated.

Future package field additions require a new package version/scheme mapping and must not change verification of older packages under their recorded metadata.

S1 retains no legacy canonicalizer and has no legacy hash corpus. The current legacy coverage asserts tuple classification and migration/backfill defaults only; it is not legacy hash re-verification.

Minor TIP-65 debt: `FormatNumber` is not proven against the full official RFC 8785 number-vector set. This is accepted for S1 because hashed evidence forbids raw JSON numbers and the tripwire test fails any evidence seed that would invoke number canonicalization.

### Tier-2 Open Items

T2-1 RFC 8785 JCS canonicalization is resolved by TIP-65 for the S1 evidence package hash chain.

T2-2 is resolved by TIP-66 only as an S1 package-level real JWS signing foundation over `manifestHash` using a local non-production ES256 dev/P12 adapter behind `IEvidenceSigner`. This does not resolve production HSM/KMS, CA-issued certificate signing, legal sufficiency, non-repudiation, replay protection, payload signing, webhook signing, runtime consumer verification, or decision-basis binding. Those remain outside S1/TIP-66 reliance. Link: EBS-07.

TIP-68 adds the production-custody signing backend as a swap behind the same `IEvidenceSigner`: `Pkcs11Es256JwsEvidenceSigner` signs the same TIP-66/TIP-67B JWS envelope and neutral-proof claim with an HSM-held ES256 key via PKCS#11. It changes key custody and rotation only; it does not add a second signature, does not change the neutral-proof claim shape, and does not feed back into `manifestBodyHash`, `packageHash`, or `manifestHash`. Production legal sufficiency, CA/TSA/qualified signatures, payload/webhook signing, and runtime verifier services remain separate deferred surfaces.

## Entity: client_applications

Purpose: Represents an external system allowed to create verification sessions.

Key fields: `id`, `name`, `status`, `allowedPurposes`, `allowedChecks`, `webhookBaseUrl`, `createdAt`, `disabledAt`.

Append-only requirement: Not append-only, but status changes MUST be audited.

Sensitive classification: Confidential.

Raw data policy: Raw identity or biometric data MUST NOT be stored here.

## Entity: api_keys

Purpose: Authenticates client applications.

Key fields: `id`, `clientApplicationId`, `keyPrefix`, `keyHash`, `scopes`, `status`, `expiresAt`, `createdAt`, `revokedAt`.

Append-only requirement: Key creation and revocation MUST be audited. Key hash MUST NOT be mutated after creation.

Sensitive classification: Confidential.

Raw data policy: Only hashed API keys are allowed. Raw API keys MUST NOT be stored.

Scope notes: Ordinary business client scopes MUST be distinct from capture agent, device gateway, internal adapter, vault, and operator/admin scopes. Business clients MUST NOT be automatically trusted to submit arbitrary `PASSED` evidence.

## Entity: subjects

Purpose: Holds TagEkyc subject reference metadata when needed across sessions.

Key fields: `id`, `clientApplicationId`, `subjectRef`, `subjectType`, `createdAt`.

Append-only requirement: Not append-only, but merges and corrections MUST be audited.

Sensitive classification: Confidential.

Raw data policy: Raw CCCD, face, fingerprint, and liveness artifacts MUST NOT be stored here.

## Entity: verification_sessions

Purpose: Tracks lifecycle and root business correlation for an eKYC verification session. `VerificationSession` correlates the client application, purpose, `subjectRef`, profile, required checks, optional client correlation refs, capture artifacts, verification checks, evidence results, audit events, evidence package, callbacks/webhooks, and expiry.

Logical key fields: `id`, `clientApplicationId`, `subjectId`, `profile`, `externalSessionId`, `clientReference`, `purpose`, `challenge`, `requestId`, `correlationId`, `state`, `result`, `assuranceLevel`, `expiresAt`, `createdAt`, `completedAt`.

Persistence compatibility note: the current EF/PostgreSQL session row keeps existing column names `ExternalTransactionId` and `BindingNonceHash` to avoid a data migration in TIP-67A. They map to domain/API `ClientReference` and `Challenge`; old persisted profile string `TransactionBoundEkycProfile` is read as `ChallengeBoundEkycProfile`.

Append-only requirement: State transitions SHOULD be represented by append-only audit events. The current row MAY store current state as a projection.

Sensitive classification: Confidential.

Raw data policy: Only correlation identifiers, opaque challenges, policy references, and hashes are allowed. `Challenge` is not required to be a hash and must not be treated as a document, transaction, consent, or evidence payload.

Profile rules: `STANDARD_EKYC_PROFILE` is the generic default for ordinary identity assurance. `CHALLENGE_BOUND_EKYC_PROFILE` requires `Challenge` by policy and allows optional caller-owned `ClientReference`. `Challenge` is an opaque string, 128 .NET characters or fewer, with no C0/C1 control characters and no trim/normalize/hash requirement. `externalSystem` or `clientCode`, if represented, MUST be derived from or validated against `clientApplicationId`, not trusted from the request body by itself.

Evidence-integrity boundary: `Challenge` and `ClientReference` may be echoed in BusinessConsumer create/session/completion DTOs. They do not enter `manifestBodyHash`, `packageHash`, or `manifestHash` in S1/TIP-67A.

Suggested states: `CREATED`, `IN_PROGRESS`, `READY_TO_COMPLETE`, `COMPLETED`, `FAILED`, `EXPIRED`, `CANCELLED`.

Suggested results: `NOT_AVAILABLE`, `PASSED`, `FAILED`, `REVIEW_REQUIRED`, `EXPIRED`, `ERROR`.

## Entity: required_checks

Purpose: Stores the policy required for a verification session.

Key fields: `id`, `verificationSessionId`, `checkType`, `required`, `minimumConfidence`, `policyVersion`, `profile`, `createdAt`.

Append-only requirement: MUST be append-only for a session. Policy changes MUST create a new version or new session.

Sensitive classification: Internal.

Raw data policy: No raw sensitive data allowed.

Suggested check types: `CAPTURE_QUALITY`, `DOCUMENT_OCR`, `DOCUMENT_NFC`, `FACE_MATCH`, `LIVENESS`, `FINGERPRINT`, `RISK_EVALUATION`.

Future specialized capture quality check types MAY include `DOCUMENT_IMAGE_QUALITY`, `SELFIE_IMAGE_QUALITY`, `LIVENESS_MEDIA_QUALITY`, `FINGERPRINT_CAPTURE_QUALITY`, and `NFC_READ_QUALITY`.

## Entity: capture_artifacts

Purpose: Represents captured, uploaded, or received input artifacts for a verification session.

Key fields: `id`, `verificationSessionId`, `artifactType`, `captureSource`, `captureAgentId`, `deviceId`, `vaultRef`, `artifactHash`, `metadataHash`, `qualityState`, `retryReasonCode`, `requestId`, `correlationId`, `createdAt`, `expiresAt`.

Append-only requirement: MUST be append-only. Recapture attempts MUST create new artifacts rather than replacing previous records.

Sensitive classification: Restricted.

Raw data policy: Raw document images, selfie images, liveness media, NFC read artifacts, fingerprint captures, and sensitive device metadata MUST remain inside vault or secure adapter boundaries. Business clients receive only sanitized refs, hashes, summaries, and correlation fields.

Suggested artifact types: `DOCUMENT_FRONT_IMAGE`, `DOCUMENT_BACK_IMAGE`, `SELFIE_IMAGE`, `LIVENESS_MEDIA`, `NFC_READ_ARTIFACT`, `FINGERPRINT_CAPTURE`, `DEVICE_CAPTURE_METADATA`.

Suggested quality states: `PENDING`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `ACCEPTED_FOR_VERIFICATION`, `TECHNICAL_ERROR`.

## Entity: verification_checks

Purpose: Tracks execution attempts for required or policy-driven checks against one or more capture artifacts.

Key fields: `id`, `verificationSessionId`, `requiredCheckId`, `checkType`, `status`, `attemptNumber`, `inputArtifactRefs`, `adapterName`, `adapterVersion`, `resultRef`, `reasonCodes`, `requestId`, `correlationId`, `startedAt`, `completedAt`.

Append-only requirement: SHOULD be append-only per attempt. Current check status MAY be stored as a projection.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Stores artifact refs and hashes only. Raw artifact bytes MUST NOT be stored here.

Suggested statuses: `PENDING`, `RUNNING`, `PASSED`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, `TECHNICAL_ERROR`.

## Entity: evidence_results

Purpose: Stores sanitized processed verification outputs derived from one or more capture artifacts.

Key fields: `id`, `verificationSessionId`, `verificationCheckId`, `resultType`, `inputArtifactRefs`, `result`, `confidence`, `reasonCodes`, `retryReasonCode`, `sanitizedSummaryRef`, `payloadHash`, `PayloadSignatureStatus`, `engineName`, `engineVersion`, `requestId`, `correlationId`, `createdAt`.

Append-only requirement: MUST be append-only. Corrections MUST create a new evidence result version.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Evidence results MUST expose sanitized outputs only. Raw sensitive data remains in vault or secure adapter boundary.

Signature notes: `payloadSignature` was the stale v0.1 name. The as-built field is `PayloadSignatureStatus = PlaceholderUnverified`; no cryptographic payload signature exists in S1.

Suggested result types: `CAPTURE_QUALITY`, `DOCUMENT_OCR`, `NFC_VALIDATION`, `FACE_MATCH`, `LIVENESS`, `FINGERPRINT_MATCH`, `FRAUD_RISK`.

Suggested result values: `PASSED`, `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `FAILED_IDENTITY`, `REVIEW_REQUIRED`, `TECHNICAL_ERROR`, `NOT_SUPPORTED`.

## Entity: document_evidence

Purpose: Stores specialized document OCR/visual inspection evidence result metadata derived from capture artifacts.

Key fields: `id`, `verificationSessionId`, `documentType`, `issuingCountry`, `documentNumberHash`, `fullNameHash`, `dateOfBirthHash`, `ocrConfidence`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw document images and extracted plaintext SHOULD NOT be stored in this entity. Use VaultRef/hash.

## Entity: nfc_evidence

Purpose: Stores specialized NFC/e-chip evidence result metadata for supported identity documents.

Key fields: `id`, `verificationSessionId`, `documentType`, `chipReadStatus`, `passiveAuthResult`, `activeAuthResult`, `dataGroupHashes`, `result`, `vaultRef`, `artifactHash`, `readerName`, `readerVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw NFC data groups MUST be stored only through VaultRef or omitted in S1.

## Entity: face_evidence

Purpose: Stores specialized face match evidence result metadata.

Key fields: `id`, `verificationSessionId`, `referenceSource`, `probeVaultRef`, `referenceVaultRef`, `matchScore`, `threshold`, `result`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw face images MUST NOT be returned to consumers. Storage MUST use VaultRef/hash.

## Entity: liveness_evidence

Purpose: Stores specialized liveness detection evidence result metadata.

Key fields: `id`, `verificationSessionId`, `challengeType`, `livenessScore`, `threshold`, `presentationAttackSignals`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw liveness video/images MUST use VaultRef/hash and MUST NOT be returned to consumers.

## Entity: fingerprint_evidence

Purpose: Stores specialized fingerprint match evidence result metadata.

Key fields: `id`, `verificationSessionId`, `fingerPosition`, `captureDeviceId`, `templateHash`, `matchScore`, `threshold`, `result`, `vaultRef`, `artifactHash`, `engineName`, `engineVersion`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Restricted.

Raw data policy: Raw fingerprint images/templates MUST use VaultRef/hash. Consumer payloads MUST NOT include raw fingerprint data.

## Entity: verification_results

Purpose: Stores final aggregated verification decision.

Key fields: `id`, `verificationSessionId`, `result`, `assuranceLevel`, `riskScore`, `failedChecks`, `completedChecks`, `decisionReasonCodes`, `retryReasonCodes`, `createdAt`.

Append-only requirement: MUST be append-only. Corrections MUST create a new result version.

Sensitive classification: Confidential.

Raw data policy: No raw sensitive artifacts allowed.

Suggested assurance levels: `NONE`, `LOW`, `MEDIUM`, `HIGH`, `UNKNOWN`.

## Entity: evidence_packages

Purpose: Stores the manifest and integrity metadata for evidence delivered to consumers.

Key fields: `id`, `verificationSessionId`, `packageVersion`, `canonicalizationScheme`, `hashAlgorithm`, `manifestHash`, `evidenceRefs`, `auditEventRefs`, `resultRef`, `packageHash`, `EvidencePackageSignatureStatus`, `createdAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Confidential with Restricted references.

Raw data policy: Package MUST contain VaultRefs/hashes, not raw CCCD, face, liveness, or fingerprint data.

Signature notes: `evidencePackageSignature` was the stale v0.1 placeholder-material name. The package row keeps `EvidencePackageSignatureStatus`; TIP-66 signature material is stored on the internal manifest row only, not duplicated onto the package row. This package-level signature is distinct from per-evidence `PayloadSignatureStatus` and the completion-notification projection `WebhookSignatureStatus`.

## Entity: vault_objects

Purpose: Represents stored evidence artifacts or references to secure external object storage.

Key fields: `id`, `vaultRef`, `objectType`, `contentHash`, `encryptionKeyRef`, `retentionClass`, `storageUri`, `createdAt`, `expiresAt`.

Append-only requirement: MUST be append-only for object metadata. Retention changes MUST be audited.

Sensitive classification: Restricted.

Raw data policy: Raw data MAY exist only inside the vault storage boundary. Application tables MUST use VaultRef/hash.

## Entity: audit_events

Purpose: Records immutable operational and evidence lifecycle events.

Key fields: `id`, `clientApplicationId`, `verificationSessionId`, `actorType`, `actorId`, `eventType`, `eventPayloadHash`, `eventPayloadRef`, `requestId`, `correlationId`, `occurredAt`.

Append-only requirement: MUST be append-only.

Sensitive classification: Confidential.

Raw data policy: Event payloads SHOULD avoid raw sensitive data. Large/sensitive payloads MUST use VaultRef/hash.

## Entity: webhook_subscriptions

Purpose: Defines callback endpoints for client applications.

Key fields: `id`, `clientApplicationId`, `eventType`, `targetUrl`, `secretRef`, `status`, `createdAt`, `disabledAt`.

Append-only requirement: Not append-only, but creation, update, and disable actions MUST be audited.

Sensitive classification: Confidential.

Raw data policy: No raw sensitive data allowed.

## Entity: webhook_deliveries

Purpose: Tracks webhook delivery attempts and retry state.

Key fields (deferred/conceptual; not as-built S1): `id`, `subscriptionId`, `verificationSessionId`, `eventType`, `deliveryId`, `payloadHash`, `webhookSignature`, `signatureTimestamp`, `deliveryStatus`, `attemptCount`, `lastAttemptAt`, `nextRetryAt`, `responseStatusCode`, `responseBodyHash`, `createdAt`.

Append-only requirement: Delivery attempts SHOULD be append-only or stored as immutable attempt records. Current status MAY be a projection.

Sensitive classification: Confidential.

Raw data policy: Payload bodies SHOULD be referenced by hash/ref. Raw response bodies SHOULD NOT be stored.

Signature notes: Future production `webhookSignature` SHOULD include delivery id, timestamp, and replay protection. S1 MAY use placeholders.

> note: `webhook_deliveries` and `webhookSignature` are deferred/conceptual in S1 and are not promoted to as-built fields by TIP-64. The only as-built webhook signature status is `WebhookSignatureStatus = PlaceholderUnverified` on the `VerificationCompletedEventDto` completion-notification projection.
