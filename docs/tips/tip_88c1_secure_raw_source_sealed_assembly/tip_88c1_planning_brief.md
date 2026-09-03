# TIP-88C1 Secure Raw Source + Sealed Assembly — Planning Brief

**Version:** 0.17

**Status:** PLANNING PATCHED — INDEPENDENT REVIEW REQUIRED — IMPLEMENTATION BLOCKED

**Date:** 2026-07-29

```text
D1 MODE DIRECTION RATIFIED
D1 DURABLE-CUSTODY SUB-DECISIONS PENDING WHERE IDENTIFIED
D2 INITIAL CLASS SUBSET RATIFIED
D3–D9 UNRATIFIED
PLANNING PATCHED — INDEPENDENT REVIEW REQUIRED — IMPLEMENTATION BLOCKED
NOT AUTHORIZED FOR BUILD BRIEF
NOT AUTHORIZED FOR IMPLEMENTATION
NOT AUTHORIZED FOR REAL RAW PERSISTENCE
NOT AUTHORIZED FOR DEPLOYMENT
```

**Grounded TagEkyc baseline:** `33b478c326fa3a69c95aebddddc0990fdcd36b21`

**CaptureAgent baseline inspected read-only:**
`b193f6316e13a82fb2f9f985f68500feae194cfa`

**Parent contract:**
`docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md`

**Feasibility input:**
`docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_feasibility_spike.md`

**Review protocol:**
`docs/process_improvements/PI-TAG-001_semantic_trace_and_review_convergence_pilot.md`

## 0. Changelog

### v0.17 — separated continuation clocks and final semantic closure

- Separated configuration-time readiness, CaptureAgent pre-wait and custody
  post-wait projections; the server projection contains only future operation
  cost and never re-adds an elapsed wait.
- Re-derived expired-owner readiness from timer anchors: lease and reconciler
  age share one owner-CAS anchor, while a later token/backoff is sequential.
- Removed the non-discriminating per-claim attempt limit while preserving the
  ratified one-claim/one-source cardinality.
- Transferred retry-count accounting to the mandatory
  `C1-BB-RETRY-COUNT-ACCOUNTING-GATE`; planning closes after this round without
  granting Build-Brief or implementation authority.

### v0.16 — self-auditing readiness, symbol and re-entry closure

- Replaced the hand-written producer-continuation readiness groups with the
  exhaustive reachable-path enumeration and its mechanically derived relation
  set, including evaluation-wait plus expired-owner reclaim.
- Published the full normative-symbol population and register-diff protocol;
  closed the five readiness-pinned limits, common bounded backoff,
  `AdmissionProtocolRejected`, total evaluation-wait and disposition-envelope
  clock.
- Split pending prior-R2 termination from ready same-owner/expired-owner
  progression, and made both `Terminated` and `TerminatedBeforeStart` exact
  durable settling values.
- Published the P1–P4 stopping checks, sibling sweep and mutation-reachability
  result. Independent external review remains required; no Build Brief or
  implementation authority results.

### v0.15 — normative-symbol, reclaim-projection and replay correction

- Added one authoritative normative-symbol register covering configuration,
  derived quantities, predicates and dispositions, including exact observers,
  fail-closed codes and boundary proofs.
- Added the expired-owner reclaim cost to the exact three-case runtime
  continuation projection and a discriminating reclaim-term mutation.
- Made temporary unavailability retryable but non-terminal/non-replay-stable,
  and defined one external operation shape per invocation and retry.
- Replaced client-observation language with server-observable P4/P5 facts and
  synchronized narrative phase/residue references to section 10.0.
- Re-ran the sibling-anchor and mutation-reachability sweeps. Independent
  external review remains required; no Build Brief or implementation authority
  results.

### v0.14 — exhaustive one-call phase and termination-budget correction

- Added the normative P0–P7 phase × outcome table as the single source of
  truth for one-call transport termination, body admission, residue, retry and
  result-union behavior.
- Split metadata-only finalization from body-required progression and pinned
  `AlreadyAvailable`, live evaluation, conflict, busy, authority and other
  pre-body outcomes to the metadata-only branch.
- Split early-body rejection before R1 from the reachable post-R1/pre-admission
  P4 window, preserving Bound alias/idempotency evidence while deterministically
  terminalizing and cleaning the exact attempt.
- Added four mandatory bounded no-default previous-R2 termination budgets,
  conditional runtime projections and mutation-proven readiness relations.
- Re-ran the sibling-anchor and mutation-reachability sweeps and retained the
  v0.13 duplication report without restructuring.

### v0.13 — admission handshake, result-union and re-entry correction

- Pinned a metadata-first admission handshake inside the one external
  operation, including proxy/no-disk-buffering posture, bounded honest
  kernel/TLS residual and exact early-body failure.
- Split internal claim progression from the only CaptureAgent-visible final
  result union; New/Existing/Reclaimed claim states can no longer egress.
- Joined transport retry to fenced ownership through exact same-owner CAS
  re-entry after durable proof that the prior R2 writer is terminated.
- Removed the invented agent-side admission attestation. CaptureAgent capacity
  is purely local; server capacity failure now means custody capacity only.
- Synchronized HLD v0.9, LLD v1.5, Debt Registry v0.10 and TIP Index v1.78.
  Independent external review remains required; no Build Brief or
  implementation authority results.

### v0.12 — physical-host capacity and body-termination correction

- Replaced the conflated per-operation capacity model with Model A: exact,
  independently reachable CaptureAgent-host buffer reservations and
  custody-server plaintext-window reservations.
- Classified aborted/cancelled/incomplete transport separately from a clean
  content-claim mismatch, preserving same-UUID retry only for the former.
- Replaced stale three-class commitment vectors with the exact two supported
  positives plus the `LivenessMedia` negative, and pinned declaration-correction
  behavior before `begin` versus after R2 admission.
- Added a standing mutation-reachability rule and swept the Invariant Trace and
  Test-Bite matrices for adjacent constraints that subsume a claimed mutation.
- Synchronized HLD v0.8, LLD v1.4, Debt Registry v0.9 and TIP Index v1.77.
  Independent external review remains required; no Build Brief or
  implementation authority results.

### v0.11 — one-call transport, capacity, wait-budget, and D2 correction

- Pinned one external CaptureAgent custody-ingress operation shape per
  invocation and made
  `begin` through R6 an internal TagEkyc ceremony with no upload session,
  multipart, resume, or public completion surface.
- Added mandatory, bounded, no-default per-class size, per-operation memory,
  aggregate memory, and per-producer/deployment concurrency configuration,
  with pre-I/O declared-size/capacity admission and stream-time actual-size
  enforcement.
- Added the token TTL to the one-lost-response retention relation and split a
  live evaluation from lock contention as
  `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` with an exact
  `RetryNotBeforeUtc`.
- Ratified D2 for `ChipDg2Portrait` and `LiveSelfieImage` only; deferred
  `LivenessMedia` until a separately ratified resumable multipart transport
  slice.
- Synchronized HLD v0.7, LLD v1.3, Debt Registry v0.8 and TIP Index v1.76.
  Independent external review remains required; no Build Brief or
  implementation authority results.

### v0.10 — targeted external-review lifecycle and privacy correction

- Replaced the content-derived producer-envelope v1 with Homeowner-ratified v2:
  the envelope freezes identity, length, media, capture and retention claims but
  contains no digest; keyed `ContentCommitment` remains the only persisted
  content verifier.
- Made the alias identity stable while its current evaluation slot is
  CAS-replaceable by a fresh, non-reused issuance after expiry/terminal
  disposition; pinned response loss, concurrent begin, Bound-alias reissue,
  old-token invalidation and maximum-ever cleanup horizon.
- Corrected complete-time admission to use the effective producer/server-capped
  deadline and pinned every token/continuation budget configuration and bound.
- Closed the CaptureAgent response union and defined length-prefixed
  C1-only `C1HashCanonical`; added non-confirmability and mutation requirements
  without changing landed Evidence-Integrity JCS hashing.
- Synchronized HLD v0.6, LLD v1.2, Debt Registry v0.7 and TIP Index v1.75.
  Independent external review remains required; no Build Brief or
  implementation authority results.

### v0.9 — External V6 review correction

- Bound every token to a server-canonical, session-scoped
  `ProducerClaimEnvelopeFingerprint`; expanded the broker-internal admission
  result with every normalized producer field required by R1; and required
  `complete` to compare the envelope binding and recompute
  `AdmissionFingerprint`.
- Defined evaluation issue/expiry as exactly token issue/expiry, qualified every
  same-token retry by unchanged alias/evaluation/owner/revision/fence, and made
  token validity explicitly distinct from remaining plaintext authority.
- Corrected the token-result matrix: key-unavailable type is selected by token
  variant, Existing alone has a persisted business comparator, and New lineage
  mismatch is token-invalid.
- Split pre-begin capability failure from post-begin active-key failure, closed
  all alias/shell residue and lifetime-result branches, and synchronized the
  historic-key SQL outcome into the LLD.
- Corrected the reconciliation plaintext model: it has no producer buffer but
  does bounded-decrypt plaintext chunks, so custody reconciliation now has
  explicit lifetime, zeroization, dump/swap/debugger and host-readiness duties.
- Recorded the two external reviews and the one partially accepted retention-
  relation proposal without opening an internal V7. Independent review remains
  required; no Build Brief or implementation authority results.

### v0.8 — Capture-time ingress, recoverable encryption, and disposition arbitration

- Made R1–R6 an explicit capture-time custody-ingress saga independent of B4,
  with a UUID idempotency claim, state-shaped replay, bounded contention, and
  fenced producer/reconciler ownership.
- Separated authoritative ingress identity, producer-claimed integrity, keyed
  content commitment, and server-verified content evidence; historic key
  versions are used for replay and missing historic keys fail closed.
- Moved complete recoverable cryptographic context into R1, added monotonic
  encryption attempts, and distinguished CaptureAgent loss from custody-worker
  loss so no reconciler is instructed to recreate unavailable plaintext.
- Added the immutable authority snapshot, fresh authority checkpoints,
  exact retained-source selection from server-authored acceptance evidence, and
  Available-only structural proofs for pre-publication ingress outcomes.
- Added exact C2 preparation disposition arbitration, serialized final renewal,
  CaptureAgent-owned plaintext lifetime with a server-side cap, graceful versus
  abrupt-loss semantics, and production host-posture readiness.
- Replaced `SubjectRefHash` with a versioned keyed `SubjectRefToken`, advanced
  enclosing C1 preimages to v2, closed locator wording, corrected the landed B4
  incompatibility record, and synchronized every matrix and review artifact.
- Closed V1 findings with a two-edge begin/complete claim safe across rotation
  and CaptureAgent restart, actual HMAC-key resolution, server-authored capture
  acceptance/selection, source/attempt/staged fingerprint separation,
  idempotent R1 key-reference recovery, exact complete-provisional verification,
  deterministic C2 pre-registration and in-Seal disposition CAS, precise
  outcome unions, and an exact least-privilege capability graph.
- Closed the V3 atomicity finding by making the `complete` NewCandidate branch
  the R1 transaction itself: it allocates and persists the source plus complete
  recovery context atomically, and returns `NewReservation` only after commit.
- Closed V4 identity/temporal regressions with distinct new-versus-existing
  evaluation-token semantics, in-`complete` authority revalidation, non-reused
  shell identity/fencing, and named tests that bite each atomic/crash boundary.
- Applied the v0.8-V5 checkpoint corrections: durable alternate-key aliases,
  stateful opaque token validation, typed dependency results routed back through
  `complete`, closed token outcomes, and cycle-qualified review labels.
- Applied the final v0.8-V6 corrections: broker-owned validation/derivation/
  `complete`, no selector or provisional-result egress to ingress, mandatory
  bounded DB-issued token TTL, exact alias/canonical residue, and the complete
  token-variant/result matrix. Per Homeowner direction these patches are not
  internally re-reviewed; external CC/GPT review is required.

### v0.7 — Consolidated D1 direction and distributed-custody corrections

- Recorded the Homeowner-ratified mode direction
  `EncryptedRawVaultRetained` while keeping every durable-custody
  controller/legal/retention/purge/hold/access sub-decision explicitly pending.
- Split pre-encryption `SourceReservationFingerprint` from the
  post-encryption `StagedCiphertextFingerprint`, froze C1-owned per-job source
  selection before raw read, and made the R1–R6 crash protocol deterministic.
- Separated committed-seal replay equality from fresh lease/revision admission,
  added `SealOutcomeUnknown`, and required committed-seal authority to win over
  every pending or concurrent preparation abort.
- Recorded the required TIP-88C1-owned B4 amendment that makes
  `AssemblySealed` lease-ineligible, non-claimable, and non-reclaimable.
- Normalized persisted C1 timestamp preimages to truncated UTC microseconds,
  fixed `JobExpiresAtUtc`, C2 preparation identity, renewal/seal ordering,
  canonical assembly bytes, authentication names, and substitution proofs.
- Restored process/identity/credential isolation for custody ingress and worker
  authority, defined the default-deny ingress response, and added one canonical
  prohibited-content/non-claims contract covering observability and fixtures.
- Preserved the three distinct `ART` phase gates and synchronized the changed
  active contracts into the HLD, LLD, debt registry, and TIP index.
- This docs-only authoring change authorizes no build brief, implementation,
  provider execution/evidence, raw persistence/reuse, commit, push, merge, or
  deployment.

### v0.6 — Round-5 root-cause and GUID canonicalization correction

- Recorded the mandatory round-5 root-cause checkpoint after one
  `PATCH_REGRESSION`.
- Replaced the conflicting C1 GUID `D` input format with lowercase `N`, matching
  the authoritative LLD and landed evidence-hash call sites.
- Required a cross-language golden vector covering every C1 scalar encoding,
  deterministic assembly id, stable fingerprint, attempt manifest digest, and
  C2 idempotency fingerprint.
- Added a drafting/reviewer rule that any future canonical preimage change must
  reconcile scalar encodings against both authoritative docs and landed call
  sites before claiming the convention is inherited.

### v0.5 — V4 governance, retry-identity, and lease-liveness corrections

- Synchronized the C1 planning amendment into the HLD, LLD, and debt registry
  required by D9.
- Required a composite fixture-evidence authorization covering every relevant
  `ART-001` through `ART-009` lifecycle field without treating generated
  fixtures as closure for real artifacts.
- Split job-stable assembly identity/content values from attempt-scoped
  manifest, authentication, and C2 preparation values, with exact preimages
  and reclaim proofs.
- Added the B4 lease-renewal protocol, latest-revision propagation, failure
  cleanup, and a longer-than-one-lease mutation proof.
- Added the round-4 affected-surface map and synchronized document versions and
  status wording.

### v0.4 — V3 class-set, post-seal, governance, and key-capability corrections

- Required exact ordinal/class equality between seal items and immutable B4 job
  classes inside the seal transaction.
- Pinned the committed C1 seal as the authority point for finalizing its exact
  C2 preparation; post-seal withdrawal blocks later delivery but does not
  create an abort-versus-finalize race.
- Added D9 carrying `GOV-001` and `ART-001` through `ART-009`, with explicit
  docs, fixture-evidence, real-artifact, and production gates.
- Split source-encryption key readiness from assembly-authenticator signer and
  C2 verifier compatibility.
- Added class-set, post-seal withdrawal, and independent key-capability proofs
  and synchronized the index.

### v0.3 — V2 seal replay and outcome synchronization

- Completed the seal command with every assembly identity and ordered-item
  field that its transaction persists.
- Added an exact-existing-identity replay branch before new-seal
  state/lease/CAS admission, so response-loss recovery can return
  `ExistingMatch` from `AssemblySealed`.
- Pinned new-seal precedence to landed B4 semantics:
  actor/ownership → existing replay → revision/fence/lease → fresh authority →
  atomic publish.
- Synchronized all source binding and integrity outcome tokens across matrices,
  invariants, and tests.
- Updated the round-2 affected-surface map and TIP index; no scope or
  authorization changed.

### v0.2 — Round-1 distributed-boundary corrections

- Added the missing C1-owned B4
  `Assembling → AssemblySealed` command, immutable assembly tables, exact CAS
  predicates, state/evidence shape, and same-transaction seal publication.
- Replaced the impossible provider-write crash claim with a provider-neutral
  reserve → provisional object → commit → availability protocol plus a complete
  orphan-reconciliation matrix.
- Added fresh authority/deadline revalidation inside the seal transaction,
  immediately before CAS publication.
- Added session-challenge binding and exact authenticated-principal/API-key to
  producer identity mapping at custody ingress.
- Added the missing C2 prepare/finalize/abort sink protocol and ordered it
  around the C1 seal transaction without allowing a stale worker to publish or
  deliver bytes.
- Required a new class-specific `ChipDg2Portrait` capture-artifact identity and
  digest instead of reusing the aggregate NFC artifact.
- Split custody-ingress outcomes from assembly-worker outcomes and added the
  required consumer mapping to every shape row.
- Recorded the affected-surface map for this round; no implementation authority
  was added.

### v0.1 — Initial planning draft

- Grounded C1 in the landed B3 authorization permit, B4 job/fence contract, and
  the read-only CaptureAgent source characterization.
- Recorded the Homeowner-selected provider-neutral raw-artifact storage
  boundary.
- Selected an S3-compatible adapter, tested against a pinned MinIO
  version/digest, as the first reference implementation direction.
- Kept encrypted filesystem as an optional, separately qualified single-node
  provider rather than mandatory parallel scope.
- Distinguished reference-provider implementation from production-provider
  approval.
- Proposed `EncryptedExportPacket` and the three currently observable biometric
  classes as the smallest coherent first implementation, pending Homeowner
  ratification.
- Added every PI-TAG-001 matrix required for this High-risk pilot.
- Preserved no implementation, raw-data, provider-production, commit, push,
  merge, deployment, or real-patient authorization.

## 1. Purpose and boundary

TIP-88C1 plans the first real Raw BIO custody path. It must accept raw material
only from the trusted CaptureAgent boundary, encrypt each artifact before
provider persistence, retain it only under separately ratified durable-custody
authority, resolve only the exact source artifacts frozen for a valid B4 job,
and produce one canonical authenticated assembly result without leaving a
durable plaintext package.

C1 does not deliver bytes to a recipient. C2 owns recipient-key enrollment,
final recipient encryption, final package identity, and encrypted package
custody. C3 owns authenticated delivery.

This Planning Brief is not an implementation dispatch. Its unresolved decisions
in section 4 are STOP/RRI gates.

## 2. PI-TAG-001 activation

```text
PI-TAG-001 PILOT
Pilot TIP: TIP-88C1
Risk tier: High-risk
Selected modules: SQL/schema/transaction; API; worker/queue;
                  cryptography/key management; raw/restricted data;
                  governance/docs
Required matrices: Invariant Trace; Outcome and Precedence;
                   Shape and Nullability; Ordering Graph; Test-Bite
Independent reviewer: required — C1 establishes the first real Raw BIO custody path
Round-5 root-cause checkpoint: enabled
Round-10 hard stop: enabled
Pilot metrics report: required before closeout
```

No risk module is omitted.

## 3. TIP Analytical Summary / Intent Ledger

### Intent

Create a provider-neutral, fail-closed custody and assembly boundary that:

- receives an authenticated, class-specific producer stream before CaptureAgent
  disposes it;
- encrypts raw material before storage;
- stores only opaque locator, integrity, encryption-envelope, ownership, and
  lifecycle metadata in PostgreSQL;
- permits the isolated C1 worker to resolve only the exact frozen job classes;
- verifies session, subject, client, producer, artifact, class, length, digest,
  expiry, authority, attempt, and fencing bindings;
- produces a deterministic assembly manifest and authenticated assembly seal;
- executes verified zeroization on every graceful terminal path and separately
  records the residual-memory risk of abrupt process or host loss.

### Expected Outcome

For the selected mode and supported class subset:

```text
trusted CaptureAgent
→ capture-time authenticated custody ingress
→ R1–R6 bounded encrypt-before-store retained-source establishment
→ Available provider-neutral encrypted source
→ later B3 authorization
→ later B4 job/attempt/fence
→ C1 freezes the exact accepted-session capture source
→ isolated bounded resolve
→ exact class/digest/size verification
→ canonical authenticated assembly seal
→ C2 handoff contract
```

R1–R6 is independent of any B4 export job: ingress identity contains no
`JobId`, and a retained source can predate every export request. A later job may
bind that source only under fresh authority. A recapture never silently replaces
an already frozen job source. Every ingress outcome records the no-job
disposition; where the outcome is reachable only before `Available`, the
B4-frozen case is a structural impossibility because the resolver exposes only
`Available` descriptors.

No public reader, recipient delivery, durable plaintext package, or production
provider claim is created.

### Accepted Decisions

| Decision | Accepted direction | Consequence |
| --- | --- | --- |
| Storage dependency | Domain/application code depends on a provider-neutral raw-artifact storage port | MinIO, Ceph, S3, and filesystem types cannot enter the domain contract |
| First reference adapter | S3-compatible adapter | C1 integration evidence uses a real S3 protocol implementation |
| Reference test provider | Exact pinned MinIO version and image digest | `minio/minio:latest` is forbidden as acceptance evidence |
| MinIO posture | Mature candidate, not automatic production approval | Archive/support/licensing/patch posture must be accepted separately |
| Filesystem posture | Allowed future single-node provider | It is not mandatory parallel C1 scope |
| Encryption placement | TagEkyc encrypts each artifact before the provider receives it | Provider-side encryption may be additional only |
| Provider substitution | Conformance is defined by the port, not vendor identity | A qualified Ceph RGW or other S3 backend can replace MinIO without domain changes |
| D1 mode direction | `EncryptedRawVaultRetained` | Separately authorized later jobs may bind the same retained source; this does not authorize persistence or reuse |
| D2 initial class subset | `ChipDg2Portrait`, `LiveSelfieImage` only | Still-image one-call transport; every other class fails closed before source read |
| Source selection owner | C1 immutable per-job binding keyed by `(JobId, Ordinal, RawClass)` | Retry/reclaim for the same job reuses the exact selected `SourceArtifactId`; separate jobs may bind the same retained source under fresh authority |
| Persisted time precision | UTC truncated to whole microseconds before canonicalization, hashing, and persistence | PostgreSQL readback preserves exact replay equality |

### Rejected / Deferred Branches

| Branch | Disposition | Why | Follow-up |
| --- | --- | --- | --- |
| MinIO-specific domain types or SDK contracts | Rejected | Creates vendor lock-in | S3 adapter owns SDK details |
| Implement MinIO and filesystem providers in parallel | Deferred | Duplicates proof burden before topology requires it | Add filesystem only for a qualified single-node deployment |
| Treat SignFlow's dev MinIO container as production capability | Rejected | It is a development container, not a hardened shared service | Separate production qualification |
| CaptureAgent local raw vault as system of record | Rejected for initial direction | Conflicts with TagEkyc-owned custody and centralized operations | Reopen only by Homeowner amendment |
| Public raw-reader or presigned download URL | Rejected | Violates the C1/C3 boundary | C3 may deliver only final encrypted packages |
| Provider server-side encryption as the only protection | Rejected | Couples confidentiality to provider configuration | Per-artifact application encryption is mandatory |
| `ExternalExportOnlyNoRetain` as the first implementation | Deferred | Requires a same-process post-completion sequence that does not exist | Separate mode-specific slice or amendment |
| `EncryptedExportPacket` as the selected D1 direction | Rejected by Homeowner for C1 | A bounded single-export source does not satisfy later independently authorized TSP/CTS reuse | Remains a policy mode for other reviewed slices |
| Treat retained source existence as reuse authority | Rejected | Custody and authorization are independent | Every later job creates its own binding and passes fresh authority |
| `LivenessMedia` in C1 | Deferred | Heavy media requires resumable transport; AEAD chunks are not upload parts | Separate ratified upload-session/part/receipt/resume/completion-manifest slice |

### Debt / Gap Impact

| Gap | State in this draft | Dispatch effect |
| --- | --- | --- |
| Pre-capture retention authority | `HOMEOWNER RATIFICATION REQUIRED`; external legal/DPO evidence remains open | Blocks real capture persistence and production/raw enablement |
| Initial mode | D1 direction ratified as `EncryptedRawVaultRetained`; controller/legal/retention/purge/hold/access sub-decisions remain open | Blocks Planning ratification and build-brief drafting until the section 4 decision aid is resolved |
| Initial class subset | Ratified: `ChipDg2Portrait`, `LiveSelfieImage`; heavy media deferred | No implementation authority; exact two-class manifest required |
| Cryptographic suite and key provider | Open | Blocks build brief |
| Production storage provider | Qualification deferred | This does not invalidate the conceptual reference-adapter design; no Build Brief drafting, implementation, provider execution, or production enablement is authorized |
| CaptureAgent cross-repository allowlist | Open | Blocks build brief |
| C2 sink contract | Shape required, implementation deferred | C1 must use a fixture sink until C2 lands |
| B4 assembly-seal and retained-mode compatibility | TIP-88C1 implementation-owned additive amendment required but not authorized | Blocks build brief until exact migration/function/port allowlist is frozen and every D1 dependency is ratified |
| Provider/metadata crash consistency | Protocol frozen in section 5.6 | Build brief must map every crash cell to provider and metadata operations |
| `GOV-001` and `ART-001` through `ART-009` | Carried by D9; unresolved until the exact row-specific evidence gate closes | Blocks provider-fixture evidence, build dispatch, or real-artifact persistence at the boundaries stated in D9 |

### Non-Claims

This draft and every generated fixture/reference-provider result do not claim:

- that MinIO is production-approved, supported, patched, or legally accepted;
- that an encrypted filesystem implementation exists;
- that TagEkyc or CaptureAgent currently retains raw artifacts;
- that B2 export consent retroactively authorizes capture-time retention;
- that every `RawExportRawClass` has a producer;
- that a local container proves HA, backup, recovery, KMS, or operational
  readiness;
- that assembly is a recipient-encrypted deliverable;
- real-artifact, production, legal/compliance, audit, security, readiness,
  performance, retention-policy, or production-provider qualification;
- that C1 is dispatched, implemented, committed, pushed, deployed,
  production-enabled, or authorized to persist or reuse real raw material.

### Dispatch Readiness

**Not ready.** D1 mode direction and the narrow D2 subset are ratified. D1
durable-custody sub-decisions and D3–D9 remain unratified. D2 is limited to the two
still-image classes stated below. All required matrices must receive
an independent clean review before the Homeowner may separately authorize
Build Brief drafting.

## 4. Decisions required before build-brief drafting

### D1 — Initial mode

**Homeowner-ratified mode direction:** `EncryptedRawVaultRetained`.

Encrypted raw sources are intended to remain in durable encrypted custody so
separately authorized later jobs may reuse them, including TSP signing and CTS
registration. `EncryptedExportPacket` does not satisfy that product objective.

Selecting `EncryptedRawVaultRetained` does not authorize capture, persistence,
deployment, production activation, or raw reuse. D1 is not fully closed until
the following decision aid is separately ratified.

#### D1 Homeowner Decision Aid

The v0.8 instruction ratifies the technical lifecycle rows explicitly marked
`RATIFIED PLANNING DIRECTION`. Legal/controller/provider/key/production choices
remain `HOMEOWNER RATIFICATION REQUIRED`. Neither status is implementation,
real-persistence, provider-execution, or legal approval.

| Sub-decision | Options and consequences | Recommended default | Status |
| --- | --- | --- | --- |
| Mode × controller pair | **A.** Hospital is controller; its TagEkyc deployment/operator is processor/custodian; later TSP/CTS consumers act only under separately documented authority. **B.** Hospital and downstream TSP/CTS have an approved joint/independent-controller arrangement, increasing reuse, notice, audit, purge, and dispute coordination. **C.** A separate TagEkyc service operator acts as controller, creating additional direct legal/subject-rights obligations. | A for a hospital-owned on-prem deployment, subject to DPO/counsel confirmation | `HOMEOWNER RATIFICATION REQUIRED` |
| Legal basis and retention authority | Purpose-specific consent where legally sufficient; statutory/regulatory authority; or another DPO/counsel-approved basis. No technical document may invent or substitute the basis. | No default legal basis; block real persistence until one is approved and recorded | `HOMEOWNER RATIFICATION REQUIRED` |
| Retention start/end and classes | Start at accepted custody establishment or another approved event; end at fixed absolute deadline, purpose completion, revocation instruction, or the earliest applicable rule. Longer classes increase breach, subject-rights, backup, and operational burden. | Shortest fixed class that supports the approved TSP/CTS purpose; no in-place extension | `HOMEOWNER RATIFICATION REQUIRED` |
| Extension and reuse authority | Never extend; extend only under a new reviewed authority event; or use a pre-authorized bounded renewal policy. Reuse may be per-job or controller-policy based, but every job still passes fresh technical authority. | New reviewed authority event; never infer extension/reuse from object existence | `HOMEOWNER RATIFICATION REQUIRED` |
| Purge authority | Controller instruction, absolute expiry, subject/session instruction, authorization revocation, corruption/orphan reconciliation, or approved operational cleanup may compete. | Hospital/controller owns policy decision; isolated custody service executes idempotently with evidence | `HOMEOWNER RATIFICATION REQUIRED` |
| Legal hold and precedence | Hold may suspend physical deletion while blocking read/reuse, or may permit tightly scoped evidence access under separate authority. Incorrect precedence can either destroy held evidence or turn a hold into reuse authority. | Hold blocks purge and all ordinary read/reuse; release resumes the previously due purge, subject to approved legal policy | `HOMEOWNER RATIFICATION REQUIRED` |
| Access authority | Per-job B1/B2/B3/B4 authorization; separately privileged audited operator access; or another ratified controller path. | Per-job authority for normal reuse; exceptional access isolated, non-enumerating, and audited | `HOMEOWNER RATIFICATION REQUIRED` |
| Capture-time ingress identity | A stable CaptureAgent UUID plus authoritative producer/client/session/accepted-capture revision/class/authority-snapshot identity claims one server-generated source. | Exact v0.8 claim and conflict state machine | `RATIFIED PLANNING DIRECTION` |
| Authority loss before `Available` | Publication is forbidden; an explicit ratified purge/hold predicate selects physical disposition. | Never expose a source that lost authority before publication | `RATIFIED PLANNING DIRECTION`; purge/hold branch remains pending |
| Authority loss after `Available` | Read, reuse, new assembly use, and delivery stop immediately; physical purge versus hold follows its ratified policy. | Immediate logical deny, separately governed physical disposition | `RATIFIED PLANNING DIRECTION`; purge/hold branch remains pending |
| C2 authority cutoff | Revalidate before Prepare and again inside Seal. Invalid authority blocks Prepare/Seal; after a committed seal, only its exact preparation finalizes for custody convergence while later use remains blocked. | Default-deny before new custody; exact finalize after committed seal | `RATIFIED PLANNING DIRECTION` |
| Accepted-source discriminator | C1 freezes the exact acceptance event, `CaptureArtifactId` and `CaptureRevision` referenced by the server-authored session-completion selection; landed `capture_artifacts` is not sufficient authority; public first/latest/arbitrary selection is forbidden. | Add the exact section 6.1 acceptance-event/selection dependency and fail closed on missing/conflicting/ambiguous evidence | `RATIFIED PLANNING DIRECTION`; additive acceptance surface/producer allowlist requires separate ratification before Build Brief |
| B4 amendment ownership | Amend in TIP-88C1 implementation or open a separately ratified predecessor TIP. Separating it adds ordering/dependency cost. | TIP-88C1 implementation owns the additive B4 compatibility amendment after a reviewed Build Brief and dispatch | `HOMEOWNER RATIFICATION REQUIRED` |
| Delivery boundary | C1 stops at authenticated sealed assembly/C2 preparation; a later slice owns recipient encryption/custody/delivery. Moving delivery into C1 expands trust and API scope. | Keep delivery outside C1 | `HOMEOWNER RATIFICATION REQUIRED` |

The retention contract must eventually freeze the legal/business purpose,
retention class, start event, end event, maximum duration, extension authority,
reuse authority, revocation behavior, and handling of data-subject or hospital
instructions. The lifecycle contract must freeze ordinary-expiry,
authorization-revocation, subject/session, orphan, and corruption purge;
hold creation/release; hold-versus-purge precedence; retry/idempotency; audit;
and provider/metadata crash reconciliation.

A retained source is never self-authorizing. Each later job must independently
bind the exact source artifact, subject, client, session/workflow, purpose,
class, consumer, authorization expiry, and current retention/hold/revocation
state. Different jobs may bind the same `SourceArtifactId`; no job inherits
another job's authority.

### D2 — Initial supported class subset

**Homeowner-ratified initial subset:**

```text
ChipDg2Portrait
LiveSelfieImage
```

These are the only two classes admitted by this slice. They are still-image
artifacts demonstrated to have both a
class-specific digest and an identifiable capture-artifact identity path at the
surveyed producer handoff boundary. Other classes may be transiently observable
at lower layers without a stable C1 producer handoff contract. C1 must add a
class-specific digest for `ChipDg2Portrait`; the current aggregate NFC hash
cannot stand in for it.

`LivenessMedia` is explicitly deferred. The one-call, no-resume transport
ratified below is acceptable for the bounded still-image subset and is not
acceptable for heavy media. Enabling `LivenessMedia` requires a separately
ratified transport slice that owns upload-session identity, bounded parts, part
receipts, resume/reclaim, expiry, backpressure and an authenticated completion
manifest. Encryption `ChunkSize` is an AEAD framing parameter and must never be
interpreted as a resumable transport part.

Every other class must be rejected before source read with a closed unsupported
capability outcome. Enumeration in policy is not producer evidence.

This D2 subset is ratified for planning. It does not authorize implementation,
provider selection, Raw BIO handling or production activation.

### D3 — Source encryption and key boundary

The build brief must pin:

- streaming AEAD framing and version;
- nonce derivation/generation and uniqueness;
- immutable authenticated associated data;
- per-artifact data-encryption key behavior;
- key-encryption key provider interface;
- wrapped-key metadata and key-version binding;
- zeroization and disposal rules;
- rotation, revocation, loss, and readiness precedence.

The evidence-signing key must not be reused as a raw-source encryption key.
Fixture/local keys may prove mechanics only. A production KMS/Vault/HSM-backed
key provider is a production-enablement gate.

### D4 — CaptureAgent and custody-service scope

The build brief must name exact files/projects in both repositories; this
Planning Brief does not authorize either change. The topology is:

```text
CaptureAgent bounded in-memory buffer
→ restricted authenticated custody-ingress process
→ TagEkyc-owned isolated custody component
→ application encryption
→ provider-neutral storage adapter
```

The public BusinessConsumer API does not expose ingress. It receives no provider
credential, DEK generation/unwrap capability, source KEK authority, locator,
custody read/delete authority, or in-process registration that obtains one.
Ingress, assembly/read, and lifecycle/delete use distinct processes, runtime
identities, and credential scopes. A separate container satisfies the minimum
process boundary; a separate VM/host is optional unless later ratified.

| Authority | Owning process/identity |
| --- | --- |
| Ingress claim begin, provisional create/write and attempt-key preparation | Restricted custody-ingress orchestrator only; no `complete`, token-validator or commitment-key registry access |
| Claim-evaluation token validation, token-bound active/historic commitment-HMAC derivation and claim completion | Isolated claim-comparison broker only; `complete` is broker-only and accepts no arbitrary selector input, key enumeration or exported key bytes |
| Exact-attempt provisional inspect/read, historic commitment-HMAC verification, source-key recovery/unwrap and R3–R6 promotion | Isolated custody-reconciliation identity only |
| Committed-source read, historic commitment-HMAC verification and source-key recovery/unwrap | Isolated C1 assembly worker only |
| Delete/purge/hold execution | Isolated lifecycle identity only; no HMAC or unwrap capability |
| Subject-token HMAC and assembly authentication | Isolated C1 cryptographic identity only |
| Credential/key rotation and revocation | Deployment key administrator, never public API |

The HMAC and key capabilities above are operation-scoped services, not exported
key bytes. The ingress orchestrator cannot call the commitment-key registry,
token validator or `complete` directly. It submits only the opaque new-
candidate or existing-candidate token plus authenticated producer claims to
the isolated claim-comparison broker. The broker revalidates the token against
its exact durable alias row, obtains the frozen selector from durable internal
state, derives under that exact active/historic version, and invokes `complete`
itself within the same broker operation. The typed provisional result never
crosses the broker-to-ingress boundary. The broker accepts no caller-selected
selector and cannot enumerate or export key bytes. Reconciliation
and assembly can verify only under the exact stored historic version. Reconciliation
can inspect/read only the exact fenced provisional attempt; assembly can read only
an exact committed `Available` descriptor. Lifecycle can delete/hold only an exact
opaque artifact identity and cannot decrypt, derive a commitment, list, or read.
Readiness compares this complete capability graph, alias/function ACLs, CSPRNG
availability, token schema/audience/TTL, broker-to-validator,
broker-to-complete and broker key-registry scope. It fails for any missing,
extra, cross-process, direct ingress-to-validator/complete/key-registry, or
public/general-API grant. Negative composition tests must remove each required
edge and add each forbidden edge independently.

#### D4.1 Exact ingress claim

CaptureAgent generates one RFC-4122 variant, version-4 UUID per exact capture
artifact before upload. Canonical text is 32 lowercase hexadecimal characters
(`Guid` `N` form); all-zero, non-v4, non-RFC-4122, uppercase, braced, or hyphenated
input is rejected before claim. The same logical operation reuses that key.

The authenticated lookup key is:

```text
ClientApplicationId
ProducerId
CaptureAgentInstanceId
IngressIdempotencyKey
```

The full authoritative `IngressIdentityFingerprint` additionally binds:

```text
AuthenticatedPrincipalId
VerificationSessionId
CaptureAcceptanceId
CaptureArtifactId
CaptureRevision
RawClass
SessionChallengeHash
AuthoritySnapshotId
```

The server verifies every field against the credential and the exact
section 6.1 server-authored acceptance event, then atomically maps the claim key
and full identity to one server-generated `SourceArtifactId`. A second use of the claim key cannot
create another source. `CaptureAgentInstanceId` remains authenticated evidence,
but process restart cannot create a second source: a second unique exact-artifact
edge excludes instance id, claim UUID and authority-snapshot version and binds
`ClientApplicationId + ProducerId + VerificationSessionId +
CaptureArtifactId + CaptureRevision + RawClass`. Reuse of that exact
artifact with a different instance or UUID must resolve the existing claim and
then either exact-match or conflict; it can never insert another source.

The planned metadata surface is:

```text
tagekyc.raw_export_source_ingress_claims
tagekyc.raw_export_source_ingress_claim_aliases
tagekyc.begin_raw_export_source_ingress_claim(...)
tagekyc.validate_raw_export_claim_evaluation_token(...)
tagekyc.complete_raw_export_source_ingress_claim(...)

pk_raw_export_source_ingress_claims
uq_raw_export_source_ingress_exact_artifact
uq_raw_export_source_ingress_source
fk_raw_export_source_ingress_claims_session
fk_raw_export_source_ingress_claims_acceptance
fk_raw_export_source_ingress_claims_capture_artifact
ck_raw_export_source_ingress_state

pk_raw_export_source_ingress_claim_aliases
uq_raw_export_source_ingress_alias_key
fk_raw_export_source_ingress_alias_claim
ck_raw_export_source_ingress_alias_state
```

All identifiers must pass static 63-byte and exact catalog round-trip gates.
`uq_raw_export_source_ingress_alias_key` covers every observed four-field lookup
key in the alias/consumption ledger;
`uq_raw_export_source_ingress_exact_artifact` covers the instance-independent
exact-artifact tuple; `uq_raw_export_source_ingress_source` covers
`SourceArtifactId`. The canonical claim row stores one exact-artifact identity,
the full identity fingerprint and later `AdmissionFingerprint`, not plaintext
or a bare Raw BIO digest. An alias row stores the four-field lookup tuple,
attempted exact-artifact/identity fingerprint, state
`Evaluating | Bound | ConflictTombstone`, nullable canonical claim reference,
immutable alias and envelope identity, plus one CAS-replaceable current
evaluation slot:

```text
CurrentClaimEvaluationId
CurrentClaimEvaluationOwnerId
CurrentClaimEvaluationDisposition
CurrentTokenIssuedAtUtc
CurrentTokenExpiresAtUtc
CurrentTokenSchemaVersion
CurrentTokenVariant
CurrentTokenAudience
CurrentTokenDigest
CurrentClaimEvaluationRevision
CurrentClaimEvaluationFence
LatestIssuedTokenExpiresAtUtc
```

`CurrentClaimEvaluationDisposition` is the exact closed set
`Active | Completed | Expired | Reclaimed | Conflict`; only `Active` is
non-terminal. The whole slot starts Active, and its later disposition is
monotonic. An unknown/default value is rejected, never treated as terminal or
live.

Within the token formulas below, unprefixed `ClaimEvaluation*` and `Token*`
names refer to the corresponding `CurrentClaimEvaluation*` and `CurrentToken*`
members of this one slot; they are not a second row or duplicate clock.

Each issuance uses a fresh non-reused evaluation id and token, replaces the
whole current slot atomically, strictly increases revision/fence and updates
`LatestIssuedTokenExpiresAtUtc = max(previous, new expiry)`. No current-slot
field is described as individually immutable across issuances. The
`ProducerClaimEnvelopeFingerprint` itself remains immutable for that alias/
accepted-artifact lineage. It is domain-separated, contains no content-derived
value and is never indexed as a content lookup, logged, returned or treated as
verified content. The alias stores no raw token, SourceArtifactId, locator,
bare digest or provider/key secret. The session FK
maps `VerificationSessionId` to `verification_sessions(Id)` and the
acceptance FK maps `CaptureAcceptanceId` to
`raw_export_capture_acceptance_events(CaptureAcceptanceId)`; the
capture-artifact FK maps `CaptureArtifactId` to `capture_artifacts(Id)`;
the functions also prove that the accepted artifact belongs to that exact
session before inserting.

ACL/readiness scope is exact: the restricted ingress identity may execute only
`begin`; the isolated claim-comparison broker may execute only
`validate_raw_export_claim_evaluation_token` and
`complete_raw_export_source_ingress_claim`; neither identity has direct table
DML, and only the broker has the token-bound commitment-HMAC capability. The
validator and `complete` are not granted to ingress, the public/general API or
lifecycle/assembly identities. Alias/canonical tables have zero direct runtime
DML and no column ACLs; owner/grantor, no-extra-grantee, function owner/search-
path and missing/extra capability equality are catalog/readiness gates. Exact
role names and the full function/table manifest remain Build-Brief decisions,
not authority here.

Historic-key replay cannot hold a database lock while computing an HMAC. The
atomic repository protocol is therefore:

```text
begin_raw_export_source_ingress_claim
  → bounded lock on alias-key and exact-artifact edges
  → verify actor, producer, client, session, accepted artifact/revision,
    class, challenge and authority identity; normalize the authenticated
    producer-claim envelope and compute its server-canonical fingerprint
  → atomically insert/observe the one alias row for this four-field key;
    a Bound alias resolves only its canonical claim and a ConflictTombstone
    always conflicts, so no accepted or burned alternate key can be rebound
  → bind each current token/evaluation issuance to that exact
    ProducerClaimEnvelopeFingerprint; changing any v2 envelope field under the
    current issuance is token-invalid before dependency interpretation
  → if a current evaluation is unexpired and non-terminal, never replace it:
    return `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` plus the exact
    UTC-microsecond `RetryNotBeforeUtc = CurrentTokenExpiresAtUtc`; response loss
    therefore waits until expiry rather than invalidating a token another caller
    may hold, and is never conflated with lookup-lock contention
  → after expiry, explicit terminal disposition or eligible crashed-owner
    reclaim, atomically CAS a fresh non-reused evaluation id/token/current-slot,
    increase revision/fence and preserve the maximum-ever token-expiry horizon
  → NewCandidate: alias targets a new canonical ClaimEvaluating shell that has no
    SourceArtifactId, reservation, attempt, object or key reservation; freeze
    active key selector in durable internal state; return opaque
    NewClaimEvaluationToken
  → ExistingCandidate: alias is Evaluating for the exact existing canonical
    Reserved-or-later row; freeze its historic key selector in durable internal
    state; return opaque
    ExistingClaimComparisonToken
  → return only the opaque token, variant and public expiry/restart metadata;
    never return schema/key selector, SourceArtifactId or locator

claim-comparison broker validates the opaque token against the alias row
→ internally produces exactly one typed provisional result:
    DerivedAdmission(
      ProducerClaimEnvelopeFingerprint,
      ContentCommitmentSchemaVersion,
      ContentCommitmentKeyId,
      ContentCommitmentKeyVersion,
      ContentCommitment,
      ClaimedPlaintextLength,
      MediaType,
      CapturedAtUtc,
      PlaintextRetentionStartedAtUtc,
      PlaintextRetentionExpiresAtUtc,
      PlaintextRetentionBudgetSeconds)
    ActiveCommitmentKeyUnavailable
    HistoricCommitmentKeyUnavailable
    ClaimTokenInvalid
→ invokes complete_raw_export_source_ingress_claim itself, in the same broker
  operation, passing that result without exposing it to ingress
→ returns only complete's `InternalClaimResult` to the custody ingress
  orchestrator.
No provisional or internal claim result is a CaptureAgent final/SQL outcome or
crosses the external boundary.

complete_raw_export_source_ingress_claim
  → bounded lock + verify token variant/authentication, expiry, both lookup
    edges, alias state, immutable current-evaluation/canonical identity, exact revision
    and fence
  → compare the broker-recomputed ProducerClaimEnvelopeFingerprint with the
    immutable begin-bound value; mismatch is CLAIM_TOKEN_INVALID, never a
    business fingerprint conflict
  → freshly revalidate current actor/client/session/acceptance/authority before
    any source disclosure or NewCandidate allocation; revalidate actual
    remaining producer plaintext lifetime against the complete statement time
  → rebind the broker-produced result to this exact validated token/alias/
    evaluation/canonical lineage; only now interpret it under the precedence
    matrix
  → recompute AdmissionFingerprint from persisted ingress identity, the exact
    commitment tuple and normalized producer fields; never trust a caller- or
    broker-supplied AdmissionFingerprint scalar
  → ExistingCandidate + DerivedAdmission: compare the persisted complete
    reservation, atomically CAS Evaluating alias to Bound on exact match, and
    return internal ExistingMatch; on mismatch CAS an unbound Evaluating alias to
    ConflictTombstone, while an already Bound alias remains Bound to its
    original canonical claim; return exact conflict; canonical reservation
    remains read-only
  → NewCandidate, in this same short R1 transaction:
      consume the single-use token by CASing its exact in-place shell identity,
      owner, revision and fence
      → CAS AdmissionFingerprint
      → allocate SourceArtifactId, reservation revision, attempt revision/fence,
        provisional object identity, AttemptKeyReservationId and nonce domain
      → persist the complete pre-encryption context + authority snapshot
      → CAS alias to Bound with the canonical claim reference
      → CAS ClaimEvaluating to Reserved
      → commit
  → only after that commit return internal NewReservation; otherwise return the
    exact internal existing/conflict/busy outcome
```

Every begun claim, including broker key failure, re-enters `complete` through
the broker-only edge. External dependency code may produce only the typed
provisional union inside that broker invocation; neither ingress nor another
caller can construct or submit it. `complete` independently re-verifies the
token, alias and canonical lineage, so the broker cannot cross-wire cached
results between tokens, aliases, evaluations, revisions or fences. An invalid/
expired/stale token is adjudicated by `complete` at its initial token/row
validation step and never reaches authority or dependency precedence. For a
valid token, simultaneous authority loss plus key unavailability returns the
higher-priority authority outcome. Only if authority remains valid does
`complete` map New-token key failure to
`RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE`, Existing-token key failure to
`RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE`, or
`DerivedAdmission` to comparison/allocation. The failure type is selected
solely by token variant, never by whether the selected version currently equals
the registry's active version: Existing remains Historic even when
`storedVersion == activeVersion`.

The two token variants share one stateful unforgeability contract:

```text
ClaimEvaluationTokenSchemaVersion = 1
TokenBytes = 32 CSPRNG bytes encoded unpadded base64url
Audience = "tagekyc.raw-export-source-ingress-claim-comparison"
PersistedTokenDigest = SHA-256(TokenBytes)
Configuration = RawExportSourceClaimEvaluationTokenTtlSeconds
AllowedTtlSeconds = [1, 300], integer, mandatory, no implicit default
TokenIssuedAtUtc = truncate_to_utc_microseconds(statement_timestamp())
TokenExpiresAtUtc = TokenIssuedAtUtc + configured TTL
ClaimEvaluationIssuedAtUtc = TokenIssuedAtUtc
ClaimEvaluationExpiresAtUtc = TokenExpiresAtUtc
MaxTokenValidityHorizon = TokenIssuedAtUtc + 300 seconds
```

The bearer presentation is the opaque `TokenBytes` plus its non-secret
`ClaimEvaluationId`, `ClaimEvaluationRevision`, `ClaimEvaluationFence`,
`TokenVariant` and `TokenExpiresAtUtc` restart metadata returned by `begin`.
The database stores no `TokenBytes`. Validator precedence is exact: malformed
schema/variant/audience or a digest mismatch against the same current lineage is
`...TOKEN_INVALID`; a presented evaluation id/revision/fence that no longer
equals the current slot, or elapsed expiry, is `...RESTART_REQUIRED`. Thus an
old legitimately issued token remains distinguishable from a tampered bearer
after current-slot replacement without retaining the old digest/token.

`begin` is the sole issuer. It stores only the digest plus schema, audience,
variant, issue/expiry times, alias identity, immutable
`ProducerClaimEnvelopeFingerprint` and the current evaluation slot. The broker calls
`validate_raw_export_claim_evaluation_token` and `complete`; both
constant-time-compare the digest and verify exact audience, variant, expiry,
alias/evaluation identity, revision and fence. The validator returns only an
internal broker-scoped frozen selector capability, never token bytes,
SourceArtifactId, locator or general key access. There is no stateless token-
authentication key or hidden rotation requirement: unforgeability comes from
the random bearer plus the durable digest.

Re-minting is an explicit current-slot CAS, never an overwrite of a live
issuance:

```text
Live, unexpired, non-terminal current slot
  → concurrent/repeated begin returns
    RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS
    with RetryNotBeforeUtc = CurrentTokenExpiresAtUtc
  → current token remains valid and byte-unrecoverable after response loss

Expired/terminal/reclaim-eligible current slot
  → fresh CSPRNG token + fresh ClaimEvaluationId + fresh owner/times/digest
  → ClaimEvaluationRevision and ClaimEvaluationFence strictly increase
  → prior token becomes RESTART_REQUIRED by evaluation/revision/fence mismatch

Bound alias after current-slot expiry
  → may issue a fresh ExistingClaimComparisonToken under the same CAS rules
  → canonical claim and Bound identity remain byte-unchanged
```

Only the returned raw token can use the current issuance; because the database
stores only its digest, lost-response recovery cannot reproduce it. A caller
that lost the response waits until the returned `RetryNotBeforeUtc` and invokes
`begin` again only if its remaining plaintext-retention budget can still cover
that wait plus idempotency lock, comparison, complete, R2 and safety budgets.
Otherwise CaptureAgent abandons the operation, zeroizes the buffer on the
graceful path and returns `RECAPTURE_REQUIRED`.
Two concurrent `begin` calls never invalidate each other: one creates the
current slot and the loser returns the exact evaluation-in-progress variant.
`ConflictTombstone` never reissues.
Bound aliases and conflict tombstones remain durable for their canonical
retention; only an unbound non-source shell may be cleaned under the exact
horizon below.

Readiness fails closed if CSPRNG,
digest/constant-time verification, audience/TTL configuration, function ACL or
broker capability differs from this contract. Missing, non-integer or out-of-
range TTL fails readiness; configuration is never replaced by a default.

Evaluation and token lifetimes are one clock and one horizon; the builder must
not introduce a second lease/default:

```text
ClaimEvaluationIssuedAtUtc == TokenIssuedAtUtc
ClaimEvaluationExpiresAtUtc == TokenExpiresAtUtc
```

Every budget component is mandatory, integer, has no implicit default, uses
checked arithmetic and maps to the named runtime enforcement:

| Configuration key | Unit/range | Runtime owner |
| --- | --- | --- |
| `RawExportSourceClaimIdempotencyLockTimeoutMilliseconds` | milliseconds, `[1, 5000]` | DB alias/exact-artifact lock timeout |
| `RawExportSourceClaimComparisonBudgetMilliseconds` | milliseconds, `[1, 120000]` | broker derive cancellation deadline |
| `RawExportSourceClaimCompleteTransactionBudgetMilliseconds` | milliseconds, `[1, 30000]` | broker-owned `complete` DB statement/transaction timeout |
| `RawExportSourceClaimSafetyMarginMilliseconds` | milliseconds, `[1, 30000]` | non-work execution margin |
| `RawExportSourceClaimTombstoneSafetyMarginSeconds` | seconds, `[1, 300]` | post-token cleanup delay |

The symbolic names below are the checked duration conversions of those exact
keys. The configured execution budget must satisfy:

```text
IdempotencyLockTimeout
+ ClaimComparisonBudget
+ CompleteTransactionBudget
+ SafetyMargin
< ClaimEvaluationTokenTtl

ClaimEvaluationTokenTtl
+ IdempotencyLockTimeout
+ ClaimComparisonBudget
+ CompleteTransactionBudget
+ PreviousR2TerminationBudget
+ EncryptionAttemptDeadline
+ SafetyMargin
< ConfiguredMaximumPlaintextRetentionBudget
```

Partial/missing configuration, overflow, unit-conversion error or equality
fails readiness. `ClaimEvaluationTokenTtl` is the checked duration conversion
of `RawExportSourceClaimEvaluationTokenTtlSeconds`; the second relation budgets
one complete wait after an internally lost begin result plus the worst-case
four-component prior-R2 termination budget defined in section 5.6.4. Runtime
omits that term only after durable termination proof. Every issuance updates
the monotonic maximum-ever horizon:

```text
LatestIssuedTokenExpiresAtUtc =
  max(previous LatestIssuedTokenExpiresAtUtc, TokenExpiresAtUtc)

EarliestUnboundShellCleanupUtc =
  LatestIssuedTokenExpiresAtUtc + TombstoneSafetyMargin

EarliestUnboundShellCleanupUtc
  <= CurrentTokenIssuedAtUtc + 600 seconds
```

The 600-second bound is the 300-second maximum token TTL plus the 300-second
maximum tombstone margin, relative to the latest successful issuance. Cleanup
also requires no live current evaluation and a durable non-reuse/tombstone
predicate. Repeated authenticated issuance legitimately advances the horizon;
missing/unbounded values never do. The Build Brief must pin every component,
UTC-microsecond equality/±1 boundary, response-loss/concurrent-begin/Bound-alias
reissue and cleanup-one-microsecond-early tests.

- `NewClaimEvaluationToken` is short-lived, single-use and backed by the
  in-place alias plus canonical shell. It binds both lookup edges, immutable
  alias/envelope identity and the current `ClaimEvaluationId`,
  `ClaimEvaluationOwnerId`, `ClaimEvaluationExpiresAtUtc`, monotonic non-reused
  `ClaimEvaluationRevision`, monotonic non-reused `ClaimEvaluationFence`, and
  the frozen active schema/key selector.
- `ExistingClaimComparisonToken` is short-lived, authenticated, bounded and
  idempotently replayable. It binds the alias plus both lookup edges, immutable
  canonical-row identity, current reservation revision/fence, expiry and the
  stored historic selector. First exact completion binds the alias to the
  canonical claim; replay observes that same binding. The canonical reservation
  is never mutated.

The token-variant × provisional-result matrix is exact:

| Token variant | Provisional result | Token action | Alias/canonical transition | Retry | Residue/cleanup | Required proof |
| --- | --- | --- | --- | --- | --- | --- |
| New | Exact `DerivedAdmission` | CAS-consume token | `Evaluating → Bound`; canonical `ClaimEvaluating → Reserved` in atomic R1 | Later retry restarts at `begin` and observes Bound/existing | One Bound alias + one complete canonical source | Atomic R1 plus consumed-token replay |
| Existing | Exact `DerivedAdmission` | Token remains idempotently replayable while the full lineage is current | Evaluating alias becomes Bound, or an already Bound alias remains Bound; canonical claim remains byte-unchanged | Same token only while unexpired and alias/evaluation/owner/revision/fence are unchanged; otherwise `begin` | Bound alias + unchanged canonical claim | Historic-key rotation, replay and stale-fence negative |
| Existing | Correctly envelope-bound `DerivedAdmission` differs from persisted canonical `AdmissionFingerprint` | Presented evaluation becomes terminal | Unbound Evaluating alias becomes `ConflictTombstone`; already Bound alias remains Bound; canonical claim unchanged | Same presented token is not retryable; later same key conflicts | Durable tombstone or existing Bound alias; no new source | Existing business-fingerprint conflict |
| New | `ActiveCommitmentKeyUnavailable` | Token is not consumed | Evaluating alias and canonical shell unchanged | Same token only while unexpired and alias/evaluation/owner/revision/fence are unchanged; after expiry/reclaim use `begin` | Evaluating alias + non-source canonical shell, retained through the safe cleanup horizon | Key restore, expiry, stale-fence and cleanup boundaries |
| Existing | `HistoricCommitmentKeyUnavailable`, including stored version equal to active version | Token is not consumed | Evaluating alternate or already Bound alias unchanged; canonical claim unchanged | After exact key restoration, same token only while unexpired and full lineage unchanged; otherwise `begin` | Existing alias/canonical state only; no second source | Historic==active positive control, restore, expiry and reclaimed-fence negative |
| Wrong variant/result pairing; content-free v2 producer-envelope mismatch; cross-token/alias/evaluation result; or `ClaimTokenInvalid` | Reject as `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID` | Presented token terminal | No transition | No retry of that token; restart at `begin` | Preserve whatever alias/canonical state existed before the call | Variant/result/v2-envelope/lineage cross-product mutation; digest-only change is excluded and follows keyed Existing comparison or New R3 |
| Expired, stale or reclaimed token | Reject as `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` | No transition | No transition | `begin` only | Preserve existing alias/canonical state until safe cleanup | UTC-microsecond expiry and reclaim race |

Every typed-result branch flows only through broker → `complete`, and fresh
authority precedes result interpretation for every valid token. The named
`C1_claim_token_variant_result_matrix_is_exact_and_authority_precedes_result`
test must mutate every pairing, retry rule, transition and residue cell.

Both variants are internal and contain no source id or locator. A concurrent
revision/fence change causes bounded restart from `begin`, never comparison
under the active key by default. No second alias row exists for one alias key
and no second canonical claim exists for one exact artifact.

A crashed alias/canonical `ClaimEvaluating` owner is reclaimed only by replacing
the expired/reclaim-eligible current slot through CAS on the same alias: a fresh
evaluation id/token/owner/times/digest is created, revision and fence strictly
increase, `LatestIssuedTokenExpiresAtUtc` never decreases, and no value is
reused. The shell is never deleted/reinserted while any token could remain
valid. Terminal unbound-shell cleanup is allowed only after
`EarliestUnboundShellCleanupUtc` and a durable non-reuse/tombstone predicate make
every prior token invalid. `complete` checks the exact evaluation identity/
owner/revision/fence/expiry, so a stale token cannot complete a reclaimed
shell. The shell has no
`SourceArtifactId`, is not a source reservation and cannot become
provider-visible. Winner R1 commit makes the loser restart and observe
`ExistingMatch`; winner R1 rollback leaves no advertised reservation and allows
the loser to reclaim the same shell candidate. Rotation between begin and
complete cannot alter the frozen selector.

Fresh current authority is a second mandatory barrier inside `complete`, after
token/row validation and before ExistingMatch disclosure, fingerprint conflict,
or NewCandidate allocation. It revalidates actor/client/session ownership,
server-authored acceptance/revision, authority snapshot source and current
retention authority. Withdrawal, expiry or accepted-capture change during the
external HMAC window returns `SOURCE_RETENTION_NOT_AUTHORIZED` (or the earlier
non-enumerating ownership outcome) with no source disclosure or R1 mutation.
NewCandidate retains exactly its Evaluating alias plus bounded non-source
canonical shell; ExistingCandidate retains its Evaluating alternate alias plus
unchanged canonical claim. This in-transaction barrier has precedence over
comparison/allocation.

An accepted alternate key is durable before internal progression:
exact-artifact replay CASes its alias to `Bound(canonical claim)` in the same
`complete` transaction that returns internal `ExistingMatch`. A different
attempted artifact/identity CASes the
alias to `ConflictTombstone`; the tombstone never points to or discloses the
canonical source but permanently prevents that key from binding another
artifact. Abandoned Evaluating aliases follow the in-place monotonic reclaim
protocol and cannot be deleted/rebound while any prior token is valid. The
required three-call proof is:

```text
A(K1) → internal NewReservation
A(K2) → internal ExistingMatch and durable K2→canonical-A alias
B(K2) → FingerprintConflict; zero B shell/source
```

When no live evaluation blocks issuance, only broker-owned `complete` returns
one `InternalClaimResult` reservation/state-shaped variant:

```text
NewReservation(SourceArtifactId, ReservationState)
ExistingMatch(SourceArtifactId, CurrentSourceState, CurrentDisposition)
ReservationReclaimed(SourceArtifactId, EncryptionAttemptRevision, Fence)
```

`CurrentDisposition` is the exact canonical-source set
`ClaimEvaluating | Reserved | Encrypting | Staged | Available |
RecaptureRequired | ContentCommitmentMismatch | SourceEncryptionFailed |
Quarantined | Deleted`. The internal result reads one persisted value; no
caller-supplied or unknown value is accepted.

Different identity/admission content raises:

```text
SQLSTATE P0001
MessageText RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT
```

A bounded wait in either function raises:

```text
SQLSTATE 55P03
MessageText RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY
```

Observing a live current evaluation after acquiring the lookup edge is not a
lock timeout. `begin` returns the typed result:

```text
RawExportSourceIngressResult.ClaimEvaluationInProgress
OutcomeCode = RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS
RetryNotBeforeUtc = CurrentTokenExpiresAtUtc
```

`RetryNotBeforeUtc` is normalized to UTC microseconds and is the only retry
metadata in `CaptureAgentFinalResult`. It is returned only to the same
authenticated producer/client/instance and exact idempotency key. It exposes no
token, owner, revision, fence, source, state or locator. Lock contention keeps
the distinct `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY` code and millisecond-scale
bounded-backoff semantics.

Token failures are not busy outcomes:

```text
tampered | wrong schema/variant/audience/binding | consumed New token replay:
  SQLSTATE P0001
  MessageText RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID

expired token | stale revision/fence | reclaimed owner:
  SQLSTATE P0001
  MessageText RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED
```

Both omit source/state/locator details and append no per-attempt evidence row.
`...TOKEN_INVALID` forbids retry of that token; the authenticated caller may
restart from `begin` with the original claim key. `...RESTART_REQUIRED` is
retryable only from `begin`. Neither creates or rebinds an alias, canonical
claim, source, attempt, object or key. A pre-existing Evaluating/Bound/
ConflictTombstone alias remains unchanged.

Timeout before `begin` commits leaves zero alias and zero shell. Timeout after a
successful New `begin` leaves one Evaluating alias plus one already committed
non-source `ClaimEvaluating` shell. Timeout after a successful Existing `begin`
leaves one Evaluating alternate alias plus the unchanged canonical claim. None
leaves a new `SourceArtifactId`, reservation, attempt, object or key
reservation; the bounded alias/shell expiry/reclaim path must converge only
after the safe cleanup horizon. Neither case discloses
existing state or creates a second source. Direct-function negative tests must prove
actor/producer/client/session/challenge/artifact/revision/class/authority
mismatch, instance-restart convergence, exact SQLSTATE/message, token expiry/
replay/tamper, existing-token idempotent replay, stale-token versus in-place
shell reclaim, authority withdrawal during external HMAC, rotation between
begin/broker-complete, pre-begin and both post-begin busy residue shapes, and zero unauthorized
source/attempt/object/key residue. Two-connection tests and mutations cover
winner commit, winner rollback, crash immediately before/after the atomic R1
commit, revision/fence restart, historic-key loss and both timeout phases.

#### D4.2 Identity, integrity, and replay

Server-known identity fields are the full ingress tuple above. Producer-claimed
operational/integrity fields are:

```text
ClaimedPlaintextDigest
ClaimedPlaintextLength
MediaType
CapturedAtUtc
PlaintextRetentionStartedAtUtc
PlaintextRetentionExpiresAtUtc
PlaintextRetentionBudgetSeconds
```

They are authenticated claims, not server-proven content evidence. CaptureAgent
does not compute the keyed commitment. The custody boundary derives the
commitment without buffering the complete plaintext merely to construct R1;
R2 independently verifies actual digest and length while streaming once.
All D4.2 fields are transmitted in the metadata phase of the one external
operation. The body is not transmitted until the server's transport-level
`AdmissionAccepted` signal after committed R1. D4.2 contains no agent-capacity
attestation, reservation proof, server-visible local slot id or local memory
claim.

Before `begin`, the trusted server canonicalizer normalizes the non-content
producer-claim envelope—identity, length, media, capture time and retention
fields, explicitly excluding `ClaimedPlaintextDigest`—and computes the v2
session-scoped
`ProducerClaimEnvelopeFingerprint` defined in section 5.6.1. `begin` persists
only that fingerprint, never `ClaimedPlaintextDigest`. The broker recomputes the
fingerprint from the same v2 fields, and `complete` requires exact equality with
the begin-bound alias value before interpreting any result. Thus one current
token/evaluation cannot change a v2 envelope field; such a change is
`RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID`. A digest-only change is deliberately
outside v2 and follows the keyed-comparison/R3 behavior in section 5.6.1. For
an alternate alias that legitimately targets an existing canonical claim, a
correctly bound but different
`AdmissionFingerprint` remains the business `FingerprintConflict` comparator.

`ExistingMatch` means the same claim identity and `AdmissionFingerprint`, not
that replay bytes were re-read. `Available` proves the original execution
verified and published; in-progress/staged/terminal states return their exact
current disposition and are never revived. `FingerprintConflict` returns no
source id, state, or details.

#### D4.3 Internal claim result and restricted final response

Two different closed unions exist and must never share a public type.
The normative P0–P7 phase × outcome table in section 10.0 is the single source
of truth for which union carries each outcome, whether body transmission is
forbidden or required, whether `AdmissionAccepted` exists, and the exact
phase-specific residue/retry rule. This section defines union shapes only and
must not be read as an independent transport sequence.

`InternalClaimResult` is process-internal to custody ingress:

```text
NewReservation:
  SourceArtifactId + ReservationState

ExistingMatch:
  SourceArtifactId + CurrentSourceState + CurrentDisposition

ReservationReclaimed:
  SourceArtifactId + new EncryptionAttemptRevision + new Fence

InternalClaimOutcome:
  exactly one of:
    RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY
    RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS
    RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID
    RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED
    SOURCE_RETENTION_NOT_AUTHORIZED
    RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE
    RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE
    RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT
    RAW_EXPORT_SOURCE_RESERVATION_BUSY
    RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID
    CONTENT_COMMITMENT_MISMATCH
    RECAPTURE_REQUIRED
    RAW_EXPORT_SOURCE_RESUME_PENDING
  plus RetryNotBeforeUtc only for ClaimEvaluationInProgress
```

`InternalClaimResult` is consumed by the ingress orchestrator to continue
R2–R6 or select a final mapping. It is never serialized, returned, logged as a
CaptureAgent result or admitted to an external DTO. In particular,
`NewReservation`, `ExistingMatch`, `ReservationReclaimed`, reservation
revision, fence, owner and lease never cross the process boundary.

`CaptureAgentFinalResult` is the only union the agent can observe, after the
single external operation either reaches its final outcome or fails:

```text
Available | AlreadyAvailable:
  OutcomeCode + SourceArtifactId + CurrentSourceState + CurrentDisposition

ClaimEvaluationInProgress:
  OutcomeCode = RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS
  RetryNotBeforeUtc = exact CurrentTokenExpiresAtUtc

OutcomeOnly:
  OutcomeCode is exactly one of:
    ACCESS_DENIED
    RAW_EXPORT_SOURCE_BINDING_INVALID
    NOT_FOUND_OR_NOT_ALLOWED
    RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID
    RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY
    RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID
    RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED
    SOURCE_RETENTION_NOT_AUTHORIZED
    RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE
    RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE
    RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT
    RAW_EXPORT_SOURCE_RESERVATION_BUSY
    RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID
    RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED
    RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE
    CONTENT_COMMITMENT_MISMATCH
    RECAPTURE_REQUIRED
    RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE
    RAW_EXPORT_SOURCE_RESUME_PENDING
  OutcomeCode only; SourceArtifactId/state/disposition are omitted
```

Exact mapping is mandatory:

- a newly reserved source, including successful same-owner re-entry, selects the
  section 10.0 body-required branch, continues internally through R2–R6 and
  returns `Available`;
- an existing source already published selects the section 10.0 metadata-only
  branch and returns `AlreadyAvailable` without a new R1, attempt, key, object,
  `AdmissionAccepted` or body;
- a live evaluation returns `ClaimEvaluationInProgress`; lock contention, a
  conflict, authority failure or a live incompatible owner returns its exact
  section 10.0 metadata-only final outcome;
- `ReservationReclaimed` is internal progression and never an external result;
- a terminal source returns its exact terminal `OutcomeOnly` code and is never
  revived.

The transport-level `AdmissionAccepted` signal in section 7.1 is neither union
and is not an application result. It contains no source, claim, owner, lease,
revision, fence, token or retry field.

State/disposition are closed enums, never provider data.
`ClaimEvaluationInProgress.RetryNotBeforeUtc` is the sole retry field in
`CaptureAgentFinalResult`; it is required on that variant and forbidden
everywhere else. No final variant exposes token, lease, owner, revision, fence
or other retry metadata; retry behavior is determined solely by the exact
section 10.1 contract. Assembly-command outcomes such as
`RAW_EXPORT_SOURCE_INTEGRITY_INVALID` belong only to section 10.2 and cannot be
inferred or returned through this union. Shape/nullability tests enumerate every
listed final code, enumerate all internal variants separately, reject an
unlisted code or extra/missing field, and prove no
`InternalClaimResult` variant can egress as `CaptureAgentFinalResult`.

No response contains provider/storage profile, any storage locator, endpoint,
bucket/key/path, ciphertext/envelope/wrapped-key metadata, key reference,
credential, descriptor, bare digest, or internal reconciliation fields. Any
future additive field requires a separately reviewed and ratified amendment.

### D5 — Pre-capture authority and source lifetime

The approved policy/consent/legal artifact must authorize encrypted source
establishment before capture bytes are persisted. Later B2 consent authorizes
export use and remains mandatory before raw read/assembly; it cannot cure an
unauthorized earlier retention.

Every reservation, descriptor, and append-only custody event binds an immutable
versioned authority snapshot:

```text
AuthoritySnapshotSchemaVersion
AuthoritySnapshotId
AuthorityArtifactId
AuthorityArtifactVersion
ControllerIdentity
ApprovedPurpose
RetentionPolicyId
RetentionPolicyVersion
RetentionClass
RetentionStartEvent
AbsoluteSourceExpiresAtUtc
ReuseDisposition
ExtensionDisposition
RevocationPolicyId
PurgePolicyId
LegalHoldPolicyId
EvaluatedAtUtc
```

The exact canonical field set is frozen per schema version. A new authority
dimension requires a new version; old rows remain verifiable under their
original version. Source existence or expiry alone is never authority evidence.
No retry extends expiry in place.

This technical snapshot does not invent or approve a legal basis, controller,
retention class, purge policy, or hold precedence. Real persistence remains
blocked until those exact values are separately ratified. Current authority is
freshly revalidated at reservation, encryption admission, R3 staging, R5
publication, read, reuse, C2 Prepare, Seal, purge, and legal-hold checkpoints.

Authority loss before `Available` prevents publication. The exact physical
disposition is selected only by a ratified predicate:

```text
legal hold active                           → quarantine under hold; no read/reuse
no legal hold + deletion/purge authorized  → delete object and destroy/revoke key
recovery still authorized and independent of the producer-held buffer
                                            → recover only to a non-readable state,
                                              then revalidate before publication
otherwise                                  → terminal unavailable; no publication
```

After `Available`, authority loss immediately blocks read, reuse, new assembly,
and delivery; purge versus hold follows the separately ratified policy. Before
C2 Prepare it blocks Prepare. After Prepare but before Seal it blocks Seal and
routes the exact preparation through section 8.2 disposition arbitration. After
Seal commit, only the referenced preparation finalizes for custody convergence;
all later read/reuse/delivery remains blocked.

### D6 — C1-owned B4 seal amendment

C1 must add one actor-scoped repository operation equivalent to:

```text
SealAssemblyAsync
```

It owns the only transition:

```text
Assembling → AssemblySealed
```

The command must bind:

```text
PrincipalId
ClientApplicationId
JobId
AttemptId
LeaseOwnerId
ExpectedRevision
ExpectedFencingToken
AssemblyId
AssemblyFingerprint
JobExpiresAtUtc
ManifestVersion
CreatedAtUtc
ManifestDigest
AssemblyDigest
AssemblyAuthenticationKeyId
AssemblyAuthenticationKeyVersion
AssemblyAuthenticationValue
C2PreparationId
OrderedItems[
  Ordinal
  RawClass
  SourceArtifactId
  ContentCommitmentSchemaVersion
  ContentCommitmentKeyVersion
  ContentCommitment
  PlaintextLength
  MediaType
]
```

Inside one fresh database transaction, after all provider/key/assembly I/O has
finished, the operation must:

1. derive the actor, verify exact job ownership, and lock/read the head, the
   exact `(AssemblyId,C2PreparationId)` disposition, plus any existing assembly
   identity/items under one deterministic lock order;
2. if an assembly identity already exists, require the head to be
   `AssemblySealed` and compare only the persisted committed replay-equality
   set: `JobId`, `AssemblyId`, `AssemblyFingerprint`, authoritative
   `JobExpiresAtUtc`, `ManifestVersion`, normalized `CreatedAtUtc`,
   `ManifestDigest`, `AssemblyDigest`,
   `AssemblyAuthenticationKeyId`, `AssemblyAuthenticationKeyVersion`,
   `AssemblyAuthenticationValue`, `C2PreparationId`, committed
   `AttemptId`/`FencingToken`, and every persisted ordered item field. Return
   `ExistingMatch` without re-authorizing or mutating state. Different
   persisted content returns `AssemblyConflict`; invalid graph shape or a
   referenced disposition not in `SealCommitted/Finalized` returns
   `AssemblyGraphInvalid`;
3. when no assembly identity exists, prove `Assembling`, exact current
   attempt/lease owner/revision/fence, and a live lease;
4. compare command `OrderedItems` to immutable `raw_export_job_classes`; count,
   ordinals, order, and every `RawClass` must be exactly equal, with no missing,
   extra, substituted, duplicated, or reordered item;
5. revalidate B1/B2/B3/B4 authority and every applicable deadline using fresh
   database state;
6. require the exact target disposition to be `Pending` and CAS it to
   `SealCommitted`; `Preparing`, `AbortAuthorized`, `Aborted`, a different
   preparation id, or a stale attempt/fence is rejected under the same lock;
7. insert one immutable assembly identity per `JobId` plus the exact command
   item rows;
8. CAS the operational head to `AssemblySealed`, increment revision, retain the
   exact current attempt/fence, and clear lease owner/expiry;
9. append one `AssemblySealed` transition with the same attempt/fence and no
   failure code.

Steps 1–9 execute inside that one transaction; steps 4–9 are the atomic
new-seal publication branch. No provider, key-provider, or sink I/O occurs
inside it. The migration must add the exact event/shape, mutation context,
function, ACL/readiness manifest, and rollback/reapply behavior; the landed B4
migration is not edited.

`ExpectedRevision`, `LeaseOwnerId`, lease expiry, and other current-admission
values are not committed assembly identity. They apply only to step 3 when no
assembly exists. The exact-existing branch in step 2 occurs first and therefore
survives response loss after revision advanced and lease ownership/expiry were
cleared.

`JobExpiresAtUtc` is owned by the immutable B4 job identity. The seal command
must carry that exact value; the repository compares it with B4 under the seal
transaction and persists it into the assembly identity and manifest. It is
never derived from attempt lease expiry and does not create or extend a
retention deadline.

For a new seal, stale revision/fence returns `FenceStale` and expired/mismatched
lease returns `LeaseNotHeld` before class-set and fresh-authority checks. A
class-set mismatch returns `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH`. Invalid,
withdrawn, or expired authority after admission returns the exact authority
outcome with no assembly identity or `AssemblySealed` transition. This
precedence matches section 10.2 and the required tests.

`ExpectedRevision` on the seal command is always the latest revision returned
by acquire or renewal. A worker must not retain the original acquire revision
after any successful `RenewLeaseAsync`.

The landed B4 baseline predates sealed-assembly custody. Its acquire function
rejects only `TerminalFailed`, `Cancelled`, and `Expired` as terminal states;
`AssemblySealed` is not an early closed branch and can fall into attempt/lease
logic before later transition/CHECK protection rejects it. That is a real
compatibility gap, not historical wording. The future reviewed additive B4
amendment must add:

```text
AssemblySealed = lease-ineligible
AssemblySealed = non-claimable
AssemblySealed = non-reclaimable
```

It covers fresh claim, reclaim, renewal, attempt creation, transition
validation, and stale-worker paths. Lease acquire/reclaim/renew attempts against
that state return the landed closed `ConcurrencyConflict` outcome with no new
attempt, lease, revision, or transition. Exact seal replay remains the separate
`ExistingMatch` path. Removing the sealed-state exclusion must
make a named future mutation test fail by observing an attempted
`AssemblySealed → Assembling` reversal.

For an unsealed `EncryptedRawVaultRetained` job, safe reclaim requires an
expired lease, fresh authority/retention/hold/revocation checks, and reuse of
the exact frozen C1 source mapping; it must never select a recapture or extend
any source/job deadline. A failed predicate has one closed outcome and cannot
mint an attempt. No new durable-custody lifecycle state is selected by v0.8; a
future state would require a ratified amendment.

The recommended owner is the future TIP-88C1 implementation slice so seal and
retained-mode compatibility are reviewed together, but this ownership remains
`HOMEOWNER RATIFICATION REQUIRED` in D1. No new TIP identifier is minted and
this document does not authorize code, migration, function, or test changes.

### D7 — Provider/metadata crash protocol

The storage and metadata owners must implement the section 5.6 reservation and
provisional-visibility protocol. A provider object may exist transiently without
an available descriptor, but it remains outside the resolver-readable
namespace/state and is deterministically reconciled. C1 does not claim that two
independent systems commit atomically.

### D8 — C2 sink boundary

Before C2 lands, C1 uses a test-only sink that implements the exact
prepare/finalize/abort protocol in section 8.2. Production readiness remains
false. C1 may not replace that protocol with a discard-only success stub.

### D9 — Existing GOV/ART lifecycle governance

C1 carries, and does not silently resolve, the existing sequence:

```text
GOV-001
→ ART-009 provider raw-payload default deny
→ ART-001 storage boundary
→ ART-002 reference resolution
→ ART-008 orphan handling
→ ART-004 retention/expiry
→ ART-005 purge/disposal
→ ART-006 legal-hold synchronization
→ ART-007 access/audit/security
→ ART-003 final package completeness (C2)
```

Before a C1 implementation build brief may authorize provider-specific fixture
evidence, a reviewed authorization packet must:

- carry `GOV-001` and every `ART-*` row with owner and disposition;
- authorize only generated, non-patient fixture bytes against the exact pinned
  reference provider;
- satisfy the composite provider-evidence fields inherited from TIP-38 through
  TIP-46 for that bounded evidence: exact storage boundary and environment,
  resolver semantics, orphan detection/reconciliation, bounded
  retention/expiry, purge/disposal and cleanup, legal-hold conflict
  disposition, least-privilege access, audit/security evidence, and raw-payload
  default deny outside the authorized fixture;
- explicitly disposition `ART-003` as not applicable to C1 fixture-provider
  evidence unless a reviewed package-completeness use is in scope; no package
  completeness claim is allowed; and
- record that every fixture-only disposition is non-patient,
  reference-environment-only evidence and does not close or weaken the
  corresponding real-artifact, production, legal/compliance, audit, security,
  readiness, performance, retention-policy, production-provider-
  qualification, evidence-availability, or package-completeness gate.

Before any real capture artifact is persisted, `ART-001`, `ART-004`,
`ART-005`, `ART-006`, `ART-007`, `ART-008`, and `ART-009` must be resolved by
reviewed evidence rather than merely accepted as deferred. The lifecycle owner
must define deletion versus an active B4 attempt, sealed C1 assembly, C2
preparation/package, legal hold, corruption quarantine, and post-withdrawal
purge.

`ART-002` is a separate resolver/readability gate. It must close before a
descriptor becomes resolver-readable or enters `Available`, before any raw
source read, and before source availability is relied upon. It is not merged
into the persistence list above. `ART-003` remains the separate C2
package-completeness gate.

Before planning ratification or build dispatch, the exact C1 amendment must be
synchronized into:

```text
docs/tagekyc_hld_v0_1.md
docs/lld_01_data_model_v0_1.md
docs/phase1_scope_and_debt_registry_v0_1.md
```

That amendment records what C1 owns, which ART rows remain open, and why
reference-provider evidence is not real-artifact or production approval.

## 5. Provider-neutral storage contract

The conceptual port name is `IRawArtifactStore`; the final namespace and
signature belong to the reviewed build brief.

The port must expose bounded operations equivalent to:

```text
CreateProvisionalCiphertextIfAbsent
InspectProvisionalCiphertext
OpenProvisionalCiphertextRead
CommitProvisionalCiphertext
AbortProvisionalCiphertext
OpenCommittedCiphertextRead
DeleteCiphertext
ProbeCapability
```

It must not expose:

```text
ListAll
Overwrite
RenameAcrossArtifactIdentity
PublicUrl
PresignedDownload
ArbitraryBucketOrPath
ProviderCredential
```

`InspectProvisionalCiphertext` accepts only the exact
`EncryptionAttemptFingerprint` and returns authenticated completeness,
provider-version, ciphertext-length/digest and framing-result metadata without
a locator. `OpenProvisionalCiphertextRead` accepts the same exact fingerprint
and returns only a bounded ciphertext stream to the custody-reconciliation
identity. Neither operation lists, guesses, opens a different attempt, returns
plaintext, or appears in the public/general API. They exist so a reconciler can
validate a complete R2 object and execute R3–R6 after worker loss without
restarting R2 or relying on process-memory results.

### 5.1 Required create semantics

- Before encryption, the caller supplies the immutable source identity and exact
  `EncryptionAttemptFingerprint`; it cannot supply or predict ciphertext
  digest/length or runtime-produced envelope/wrapped-key metadata.
- A new identity creates exactly one complete provisional object that is not
  resolver-readable.
- Same exact attempt plus the same `EncryptionAttemptFingerprint` is idempotent.
  Same attempt identity plus a different attempt fingerprint is a conflict;
  a higher fenced attempt uses a distinct provisional identity and never
  overwrites the prior attempt.
- After one-pass encryption and provider staging, R3 records one immutable
  `StagedCiphertextFingerprint` from the actual ciphertext/envelope result.
- Commit promotes only the object matching that exact staged fingerprint. It
  is idempotent for the same staged fingerprint and conflicts or quarantines
  different content according to section 5.6.
- A cancelled or failed write leaves no available descriptor. A crash may leave
  a reserved row, provisional object, or committed-but-unavailable object only
  in the exact states covered by section 5.6.
- Existing ciphertext is never overwritten.

### 5.2 Required read semantics

- Read requires an `Available` metadata state and opaque committed locator
  issued by the storage boundary.
- Locator is not a credential and cannot select an arbitrary bucket/path.
- Returned content is ciphertext only.
- Length and ciphertext digest are checked before decryption.
- Plaintext digest, class, binding, and AEAD authentication are checked during
  bounded decryption.
- A stale B4 fencing token cannot publish an assembly result.

### 5.3 Required delete semantics

- Delete is idempotent.
- Deletion operates only on the exact immutable artifact identity.
- Provider deletion success does not erase append-only custody evidence.
- Purge retry cannot resurrect, copy, or extend the artifact.

### 5.4 S3-compatible reference adapter

The first adapter must:

- use S3-compatible behavior rather than MinIO domain contracts;
- run conformance tests against an exact pinned MinIO release/image digest;
- avoid `latest`, root credentials, public buckets, and shared SignFlow
  credentials;
- use a dedicated bucket/prefix and least-privilege service identities;
- disable public access and public/presigned download surfaces;
- prove conditional create, incomplete-upload cleanup, bounded streaming,
  checksum verification, cancellation, retry, delete, and credential
  isolation;
- keep provider-side encryption additive to TagEkyc application encryption.

MinIO production use requires separate evidence for support/patch ownership,
license review, TLS, credential rotation, backup/restore, monitoring, capacity,
and migration/exit strategy.

### 5.5 Encrypted-filesystem alternative

An encrypted-filesystem adapter may be selected for a qualified single-node
deployment only if it proves the same port conformance plus:

- canonical root confinement with no traversal or symlink escape;
- restrictive OS identity and directory ACLs;
- same-volume temporary write, flush-to-disk, atomic rename, and directory
  durability;
- no partial final file after crash;
- exclusive create/no overwrite under concurrency;
- quota, disk-pressure readiness, backup/restore, purge, and orphan cleanup;
- no multi-replica claim without a separately qualified shared-filesystem
  topology.

It is not part of the first implementation unless the Homeowner changes the
deployment choice before dispatch.

### 5.6 Capture-time reservation, encryption, and crash recovery

R1–R6 establishes a retained encrypted source at capture time; it contains no
`JobId` and does not depend on a later B3/B4 export.

#### 5.6.1 Identity, commitment, and fingerprint codecs

The custody boundary persists no bare Raw BIO digest. CaptureAgent submits a
claimed digest/length. The trusted server canonicalizer first derives a
session- and ingress-identity-scoped envelope binding, then the isolated
custody process derives the keyed content commitment:

```text
ProducerClaimEnvelopeFingerprint =
  C1HashCanonical("tip-88c1-producer-claim-envelope-v2", {
    IngressIdentityFingerprint,
    ClaimedPlaintextLength,
    MediaType,
    CapturedAtUtc,
    PlaintextRetentionStartedAtUtc,
    PlaintextRetentionExpiresAtUtc,
    PlaintextRetentionBudgetSeconds
  })

ContentCommitmentSchemaVersion = 1
domain = "TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1"
ContentCommitmentKeyBytes =
  ResolveContentCommitmentKeyBytes(
    ContentCommitmentKeyId,
    ContentCommitmentKeyVersion)

ContentCommitment =
  HMAC-SHA-256(ContentCommitmentKeyBytes, LP({
    domain,
    StableDataScopeId,
    ControllerIdentity,
    VerificationSessionId,
    CaptureArtifactId,
    CaptureRevision,
    RawClass,
    ClaimedPlaintextDigest,
    ClaimedPlaintextLength,
    MediaType
  }))
```

`ProducerClaimEnvelopeFingerprint` v2 contains no digest, commitment or other
content-derived value. Its identity, length, media, capture-time and retention
fields freeze the non-content producer envelope for one authenticated
session/artifact/claim lineage. It is restricted to the alias/token comparison
surface, never a content verifier/index, source descriptor, response, audit,
metric or log field. A v1 record, if ever implemented under a separately
ratified build, remains verifiable only under its literal v1 codec; no reader
may reinterpret v1 bytes as v2 or silently rewrite their version.

The claimed plaintext digest is frozen only through the versioned keyed
`ContentCommitment` produced inside the broker/`complete` boundary and is
authoritatively checked against streamed actual bytes at R3. The accepted cost
of Homeowner option (b) is explicit: an agent changing only its claimed digest
between `begin` and `complete` is detected at Existing comparison through the
new keyed commitment, or at R3 for New after one reservation, attempt key and
provisional object may have been created. Section 5.6.5's exact fenced cleanup
removes that burned attempt. This delayed detection neither publishes an
unverified source nor weakens content confirmation resistance.

`LP` encodes every scalar as canonical UTF-8 preceded by one unsigned 32-bit
network-order byte length.
`C1HashCanonical(domain, { values in written order })` is exactly:

```text
SHA-256(
  LP(domain)
  || LP(canonical(value_1))
  || ...
  || LP(canonical(value_n)))
```

The literal domain and every scalar are independently length-prefixed; no
concatenation, delimiter inference, JSON serialization or omitted/null field is
allowed. Ordered arrays encode an LP element count followed by each element's
fixed-schema fields, each independently LP-encoded, in declared order. Every
`C1HashCanonical` construction in this brief—including ingress, envelope,
admission, source reservation, attempt, assembly, manifest and C2
fingerprints—uses this one codec.
`C1HashCanonical` is a new C1-only compatibility profile; it does not
reinterpret or modify the landed S1 Evidence-Integrity
`HashCanonical(label, value)` JCS codec documented in the LLD.

Every free-text/string scalar is valid Unicode and is normalized exactly once
to NFC before UTF-8; invalid Unicode is rejected.
GUIDs are lowercase `N`; enums are exact case-sensitive contract spellings
(normalization never changes their case); integers are invariant unsigned decimal without padding;
timestamps use UTC, tick truncation to microseconds, exactly six fractional
digits and `Z`; hashes/HMACs are lowercase hexadecimal. No null is allowed in
this preimage. The HMAC key is dedicated and is not a DEK, KEK, signing key, or
subject-token key. Only commitment bytes plus schema/key id/version persist.

The three identities are:

```text
IngressIdentityFingerprint =
  C1HashCanonical("tip-88c1-ingress-identity-v1", {
    ClientApplicationId, ProducerId, CaptureAgentInstanceId,
    IngressIdempotencyKey, AuthenticatedPrincipalId,
    VerificationSessionId, CaptureAcceptanceId, CaptureArtifactId, CaptureRevision,
    RawClass, SessionChallengeHash, AuthoritySnapshotId
  })

AdmissionFingerprint =
  C1HashCanonical("tip-88c1-ingress-admission-v1", {
    IngressIdentityFingerprint,
    ContentCommitmentSchemaVersion, ContentCommitmentKeyId,
    ContentCommitmentKeyVersion, ContentCommitment,
    MediaType, CapturedAtUtc,
    PlaintextRetentionStartedAtUtc,
    PlaintextRetentionExpiresAtUtc,
    PlaintextRetentionBudgetSeconds
  })

SourceReservationFingerprint =
  C1HashCanonical("tip-88c1-source-reservation-v2", {
    SourceArtifactId, AdmissionFingerprint, SubjectRefTokenSchemaVersion,
    SubjectRefTokenKeyId, SubjectRefTokenKeyVersion, SubjectRefToken,
    AuthoritySnapshotSchemaVersion, AuthoritySnapshotId,
    AbsoluteSourceExpiresAtUtc, StorageProfileId,
    SourceEncryptionProfileId, SourceEncryptionProfileVersion
  })

EncryptionAttemptFingerprint =
  C1HashCanonical("tip-88c1-encryption-attempt-v1", {
    SourceReservationFingerprint,
    EncryptionAttemptRevision, Fence, ProvisionalObjectIdentity,
    AttemptKeyReservationId,
    EncryptionSuiteId, EncryptionFramingVersion,
    KeyProviderId, KekId, KekVersion, KekFingerprint,
    NonceStrategyId, NonceDerivationSeedCommitment,
    ChunkSize, FramingParametersDigest
  })
```

`complete` receives the normalized non-digest fields and the keyed commitment
inside the broker-only result. It recomputes `AdmissionFingerprint` itself from
the persisted `IngressIdentityFingerprint` and the exact formula above. The
broker does not supply a trusted final fingerprint scalar, and
`ClaimedPlaintextDigest` is neither returned by the broker result nor persisted
by `complete`.

No persisted ingress, alias, descriptor, evidence or observability artifact may
be recomputable from database-resident values plus candidate plaintext into a
yes/no content-membership verifier without a protected key. The keyed
`ContentCommitment` is the sole persisted content verifier. A mutation that
adds the claimed digest or any content-derived value back to envelope v2, or
introduces another unkeyed persisted candidate verifier, must turn
`C1_producer_claim_envelope_is_token_bound_and_complete_recomputes_admission`
red.

`SourceReservationFingerprint` is immutable for the life of one source and
does not contain attempt revision, fence, object identity, actual key version,
nonce, or framing result. `EncryptionAttemptFingerprint` is immutable for one
attempt and changes on every replacement attempt. Provider provisional
create/inspect/open/commit operations key exact-attempt idempotency on the latter;
they never compare a replacement attempt against the source-stable fingerprint
alone.

For a new claim, the active commitment schema/key is frozen. For an existing
claim, the incoming commitment is recomputed using the row's historic schema/key,
never the current active key. Missing historic key yields
`RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE`, creates no second row,
and exposes no source details. Rotation never reinterprets an old row.

During R2 the custody process computes actual digest/length and derives the same
commitment under the frozen historic version. R3 requires actual digest/length
equal the authenticated claims and actual commitment equal the stored
commitment. Any mismatch yields `CONTENT_COMMITMENT_MISMATCH`; no `Staged` or
`Available` state is possible. Bare digest is discarded after comparison and
never persisted, logged, indexed, returned, or audited.

`StagedCiphertextFingerprint` remains post-encryption:

```text
C1HashCanonical("tip-88c1-staged-ciphertext-v1", {
  EncryptionAttemptFingerprint,
  ProviderObjectVersion,
  CiphertextDigest,
  CiphertextLength,
  EncryptionSuiteId,
  EncryptionFramingVersion,
  EncryptionEnvelopeMetadataDigest,
  WrappedKeyMetadataDigest,
  AuthenticationChunkCommitment,
  ChunkCount
})
```

#### 5.6.2 Recoverable R1–R6 protocol

```text
R1 short DB transaction (the NewCandidate branch of
complete_raw_export_source_ingress_claim):
   lock and validate the exact ClaimEvaluating shell/token variant,
     ClaimEvaluationId/owner/revision/fence/expiry and both lookup edges
   → freshly revalidate current session/acceptance/retention authority
   → CAS-consume the exact single-use NewClaimEvaluationToken
   → CAS AdmissionFingerprint
   → allocate SourceArtifactId and reservation revision
   → allocate monotonic EncryptionAttemptRevision and fence
   → allocate opaque provisional object identity
   → allocate AttemptKeyReservationId and nonce domain
   → persist complete pre-encryption context + authority snapshot
   → CAS ClaimEvaluating to Reserved
   → commit Reserved

R2 outside DB transaction:
   load/fence exact attempt
   → idempotently create-or-get the exact attempt DEK by
     AttemptKeyReservationId and recover/unwrap it under the frozen KEK
   → one bounded AEAD stream
   → independently compute digest/length/commitment
   → write exact provisional object
   → collect provider/encryption results

R3 short DB transaction:
   CAS exact reservation/attempt revision/fence/object
   → verify claims, commitment, provider version, ciphertext and authority
   → persist only actual post-R1 results
   → exactly one attempt becomes Staged

R4 commit/promote only the exact StagedCiphertextFingerprint
R5 fresh authority check + persist opaque committed locator + Available evidence
R6 remove obsolete provisional residue and destroy superseded attempt keys
```

v0.8 selects the authority's `recoverable key-reservation reference` branch,
not key-provider I/O inside the database transaction. `begin` is a bounded
pre-R1 evaluation transaction and may persist only the non-source
`ClaimEvaluating` shell. For `NewCandidate`, `complete` is R1; there is no
second post-complete source-creation transaction. It returns internal `NewReservation`
only after the following context commits atomically. R1 persists
`AttemptKeyReservationId`, KEK id/version/fingerprint, suite/framing, nonce
strategy/seed, chunk parameters, provisional object identity, authority
snapshot, content commitment, ownership/fence, attempt deadline, and source
expiry. The isolated key capability must implement idempotent
`CreateOrGetAttemptKey(AttemptKeyReservationId, frozen key context)`: the same
reference always recovers the same DEK/wrapped-key result, a different context
conflicts, and no durable external key exists before that call. R2 is the first
caller and may retry the same reference after process loss. Thus R1 remains one
short DB transaction, no key-provider I/O or lock spans it, and after commit no
essential recovery secret exists only in process memory. R1 never persists
plaintext, an unwrapped DEK, KEK/provider credentials, or a bare digest.

If a future key provider cannot satisfy this exact idempotent recoverable-
reference contract, it is incompatible with v0.8; the design must return to
Homeowner review rather than moving key-provider I/O into R1 or adding an
unreviewed `CryptoPreparing` state.

R2 completion does not rely on process-memory result fields. The exact
provisional object is a self-authenticating framed ciphertext stream with a
final authenticated completion record binding
`EncryptionAttemptFingerprint`, chunk count, plaintext length, claimed-content
commitment, ciphertext length/digest and framing result. The record contains no
bare plaintext digest and cannot be accepted without AEAD verification.
`InspectProvisionalCiphertext` supplies provider completeness/version and
ciphertext metadata; the fenced reconciliation identity then opens that exact
provisional ciphertext, recovers the same attempt key, bounded-decrypts it,
recomputes plaintext length and keyed commitment, and supplies those verified
actuals to R3. A missing/incomplete/invalid final record is not resumable and
converges to cleanup/recapture. This is a new bounded verification of already
complete ciphertext, not a second R2, provider overwrite, or cross-process
plaintext continuation.

Each replacement encryption attempt uses a higher attempt revision, new fence,
new `AttemptKeyReservationId`, new DEK, new nonce domain, and new provisional
object identity. It commits all corresponding R1 fields before replacement R2.
DEK+nonce is never reused across attempts or plaintext. A second encryption is
not represented as replay of the first; exactly one attempt may CAS to `Staged`.

#### 5.6.3 Producer and custody plaintext authority, lifetime, and hygiene

Every phase/residue/retry/body/admission statement in this subsection inherits
section 10.0 and does not restate or override its transport contract.

CaptureAgent is authoritative for its buffer lifetime and transmits:

```text
PlaintextRetentionStartedAtUtc
PlaintextRetentionExpiresAtUtc
PlaintextRetentionBudgetSeconds
```

The countdown starts when plaintext enters the bounded custody operation.
CaptureAgent uses a local monotonic timer; the UTC values are its authenticated
wall-clock projection. The server validates:

```text
PlaintextRetentionExpiresAtUtc > PlaintextRetentionStartedAtUtc
PlaintextRetentionExpiresAtUtc - PlaintextRetentionStartedAtUtc
  == PlaintextRetentionBudgetSeconds
PlaintextRetentionBudgetSeconds
  <= ConfiguredMaximumPlaintextRetentionBudget
abs(PlaintextRetentionStartedAtUtc - trusted server observation time)
  <= AllowedProducerClockSkew
```

All times use the v0.8 microsecond codec. The server continuation cap is owned
by mandatory integer configuration
`RawExportSourceMaximumRemainingContinuationWindowSeconds`, unit seconds,
range `[1, 3600]`, with no implicit default. Checked conversion yields
`ConfiguredMaximumRemainingContinuationWindow`; overflow, missing, non-integer
or out-of-range values fail readiness as
`RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`. The server calculates:

```text
ServerMaximumContinuationExpiresAtUtc =
  R1TransactionTimestamp + ConfiguredMaximumRemainingContinuationWindow

EffectivePlaintextRetentionExpiresAtUtc =
  min(PlaintextRetentionExpiresAtUtc,
      ServerMaximumContinuationExpiresAtUtc)

ContinuationLatestStartUtc =
  EffectivePlaintextRetentionExpiresAtUtc
  - ServerPostWaitContinuation
```

`ServerPostWaitContinuation` and every symbol above are defined once in the
section 15.1 normative-symbol register. Section 15 separately pins
configuration-time readiness, CaptureAgent pre-wait and custody post-wait
projections over five mutually exclusive state cases. The post-wait server
projection contains no elapsed wait. The prior-R2 termination term is counted
once only in pending cases; reclaim is required in both expired-owner cases.

The server never extends the producer deadline; CaptureAgent independently
enforces the monotonic deadline. Invalid interval, excessive budget, skew,
arrival-after-expiry, or insufficient remaining time yields the fail-closed
`RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID`.

Token validity is not plaintext authority. After token/envelope/lineage
validation and fresh authority, but before interpreting a valid broker result
or mutating R1, `complete` uses its DB statement/transaction timestamps,
calculates the effective deadline above and requires:

```text
CompleteStatementTimestamp < EffectivePlaintextRetentionExpiresAtUtc

EffectivePlaintextRetentionExpiresAtUtc - CompleteStatementTimestamp
  > EncryptionAttemptDeadline + SafetyMargin
```

The R1 transaction persists that exact effective deadline only after the gate
passes. Equality or one microsecond less—including producer-valid but server-
cap-insufficient input—fails
`RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID`, leaves the exact alias/shell or
alias/canonical identity residue unchanged, marks the evaluation/token terminal
for that producer buffer without CAS-consuming it as a successful claim, and
permits no provider/key/object I/O. This runtime gate—not an invalid double-count of TokenTTL plus
the comparison/complete budgets already contained within it—closes slow begin/
broker/complete requests. Readiness still proves the token execution budget is
strictly below TokenTTL, the server continuation window can contain one
complete encryption attempt, and post-R1/reclaim budgets are strictly below
the configured maximum plaintext-retention budget.

On graceful success, typed terminal failure, handled cancellation, retry
exhaustion, normal shutdown, or controlled exit, CaptureAgent executes and tests
its zeroization/disposal routine before return/exit. On crash, forced kill,
power/kernel loss, or machine reset, application zeroization cannot run;
OS page reclamation is not cryptographic erasure. Abrupt loss yields
`RECAPTURE_REQUIRED` unless complete ciphertext exists and every remaining step
is independent of the producer-held buffer.

Production readiness requires a separately ratified CaptureAgent host posture
covering locked/non-pageable memory where supported, swap/pagefile, hibernation,
crash/core dumps, debugger/dump access, least privilege, endpoint controls,
hardening, restart, and incident evidence. Missing/unverifiable mandatory
controls yield `RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID`; unsupported controls
require explicit residual-risk acceptance.

The reconciliation worker does not possess or reconstruct the lost
CaptureAgent buffer, but it does handle plaintext while bounded-decrypting
complete ciphertext. It must stream at most one configured chunk, never
materialize the complete artifact, keep the unwrapped attempt key and plaintext
chunk only for the verification operation, and execute verified zeroization/
disposal for each chunk and key on success, typed failure, cancellation and
controlled exit. Abrupt worker/host loss makes no active-erasure claim.

Production custody-reconciliation readiness separately validates locked/non-
pageable memory where supported, swap/pagefile, hibernation, crash/core dumps,
debugger/dump access, least privilege, endpoint controls, chunk/lifetime bounds,
hardening, restart and incident evidence. Missing or unverifiable mandatory
controls yield `RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID`;
unsupported controls require explicit residual-risk acceptance. Mutations that
retain a decrypted chunk/key past its bounded operation, omit graceful
zeroization, enable dumps/swap without accepted residual risk, or reuse the
CaptureAgent-only readiness result must turn named reconciliation-hygiene tests
red.

#### 5.6.4 Fenced ownership and deterministic reconciliation

Every phase/residue/retry/body/admission statement in this subsection inherits
section 10.0 and does not restate or override its transport contract.

A matching live producer has priority while plaintext may still complete R2.
Reservation ownership stores `OwnerKind`, `OwnerId`, `OwnershipLeaseExpiresAtUtc`,
`ReservationRevision`, `EncryptionAttemptRevision`, and `Fence`. Producer
continuation and reconciler are asymmetric: the reconciler has no producer
buffer and cannot restart R2, but bounded R3 verification does transiently
handle decrypted plaintext chunks under section 5.6.3.
It may CAS-claim only after ownership is absent/expired, minimum age is reached,
no healthy producer holds the fence, and the recovery/disposition threshold is
met. Before reclaim eligibility, bounded wait yields
`RAW_EXPORT_SOURCE_RESERVATION_BUSY`.

The reconciler may recover complete ciphertext through R3–R6 or clean an
unrecoverable attempt. It never starts or restarts R2. Only the same live
CaptureAgent operation may continue an active R2. A returning authenticated
owner first enters exactly one of two non-overlapping states:

- `SameOwnerRetryPendingTermination`: the owner/identity/current-row/freshness
  predicates match, but the prior exact R2 is not durably settled. No new
  attempt or writer may start. The pending-termination runtime projection
  decides bounded `RAW_EXPORT_SOURCE_RESERVATION_BUSY` versus
  `RECAPTURE_REQUIRED`.
- `SameOwnerReentryReady`: the same predicates match and the prior exact R2 is
  durably settled. The current-row CAS may create the new monotonic
  attempt/fence.

At/after `ContinuationLatestStartUtc`, no replacement R2 starts;
incomplete/missing ciphertext becomes `RECAPTURE_REQUIRED`.

`SameOwnerReentryReady` is exact and entirely server-proven:

```text
Authenticated client + producer + CaptureAgent instance + ingress UUID
  + session/challenge + accepted artifact/revision/class
  == the current reservation owner and ingress identity
AND the transaction reads and CAS-matches the exact current
  ReservationRevision + Fence
AND PreviousFencedR2Terminated
AND fresh authority/lifetime/latest-start predicates pass
```

The caller presents the authenticated ingress identity; raw
`ReservationRevision` and `Fence` never egress. “Holds the current
revision/fence” means the server atomically matches those persisted values under
the same owner tuple and uses them as the CAS predecessor. A successful CAS
keeps the ownership marker, advances `ReservationRevision`, creates a new
monotonic `EncryptionAttemptRevision` and new non-reused fence, and continues
R2 internally. A different caller, identity mismatch, stale CAS or caller that
cannot match the current owner remains
`RAW_EXPORT_SOURCE_RESERVATION_BUSY`.

`PreviousFencedR2Terminated` is true only after all of the following are durable
for the exact previous attempt revision/fence:

1. the request-body reader has exited and cannot deliver another plaintext
   byte;
2. the provider write pump/task has quiesced;
3. exact-attempt provider abort/close has returned a terminal acknowledgement,
   or exact inspection proves that attempt can accept no further write;
4. a CAS under the old revision/fence records `R2TerminatedAtUtc` and the
   monotonic `R2TerminationDisposition` as exactly one settling value:
   `Terminated` when R2 was armed, or `TerminatedBeforeStart` when P4 proves the
   reader/provider writer were never armed.

The section 15.1 normative-symbol register owns the four exact wall-clock
configuration keys, their checked conversions, and the
`PreviousR2TerminationBudget` derivation. No second termination budget or
implicit default is permitted.

Missing, zero, negative, non-integer, out-of-range, overflowed, partial or
relationally invalid values fail readiness as
`RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`. `ProviderAbortOrInspectionBudget`
belongs only to proving the prior exact R2 unable to write; it does not replace
the separately owned `ProviderAbortOrFinalizeBudget` in the reconciliation
disposition envelope.

Connection loss alone, cancellation request alone, object cleanup request or
process-local task state is insufficient. While
`SameOwnerRetryPendingTermination` holds, no second writer or new attempt may
start. The exact projection in section 15.1 returns bounded
`RAW_EXPORT_SOURCE_RESERVATION_BUSY` while completion still fits, or
`RECAPTURE_REQUIRED` when it cannot fit. P4 `TerminatedBeforeStart` satisfies
the durable predicate only after its exact old-fence CAS and cleanup evidence
commit; it is not inferred from the fact that the body was not admitted.

| Observed state | Exact predicate | Single action | Visibility |
| --- | --- | --- | --- |
| No claim/row | Valid new request | Execute R1 | None |
| `Reserved`, healthy owner | Lease live and producer can finish before effective deadline | Owner continues; others bounded-wait | None |
| `Reserved`, same owner returns; prior exact R2 is not durably settled | Exact `SameOwnerRetryPendingTermination` | No replacement CAS; apply the pending-termination projection and return bounded busy or recapture | None |
| `Reserved`, same owner returns; prior exact R2 is durably settled | Exact `SameOwnerReentryReady` | CAS current revision/fence; retain owner marker; create new monotonic attempt/fence; continue R2 internally | None |
| `Reserved`, owner expired; prior exact R2 is not durably settled | Exact `ExpiredOwnerReclaimPendingTermination` | No replacement CAS; apply termination + reclaim projection and return bounded busy or recapture | None |
| `Reserved`, owner expired; prior exact R2 is durably settled | Exact `ExpiredOwnerProducerReclaimReady` and before latest-start | Producer CAS-reclaims with new attempt/fence | None |
| `Reserved`/`Encrypting`, incomplete object | Producer unavailable or latest-start reached | Abort exact object, destroy/revoke attempt key, terminal `RECAPTURE_REQUIRED` | None |
| Complete provisional object, R3 absent | Exact attempt inspection and authenticated completion record valid; R1 key reference recovers; custody host posture valid | Reconciler CAS-claims, bounded-decrypts and promptly zeroizes exact plaintext chunks while recomputing length/commitment, and executes R3; no R2/producer-buffer restart | None |
| Complete object, claim/commitment mismatch | Any exact content predicate fails | Terminal `CONTENT_COMMITMENT_MISMATCH`; delete after hold predicate permits; destroy/revoke key | None |
| Object corrupt | Legal hold active | Quarantine under hold, block every read/reuse, append evidence | None |
| Object corrupt | No hold and purge/delete authorized | Delete exact object, destroy/revoke key, append terminal evidence | None |
| `Staged`, authority current | Exact provisional object matches staged fingerprint | R4 exact commit | None |
| `Staged`, authority invalid | Hold active | Keep quarantined non-readable object/key under hold; never R4/R5 | None |
| `Staged`, authority invalid | No hold and deletion authorized | Delete object, destroy/revoke key, terminalize | None |
| R4 committed, R5 absent, authority current | Exact object validates | R5 publishes `Available` | None until R5 |
| R4 committed, authority invalid | Hold active | Quarantine non-readable committed object; never `Available` | None |
| R4 committed, authority invalid | No hold and deletion authorized | Delete object, destroy/revoke key, terminalize | None |
| `Available`, R6 pending | Exact descriptor/object agree | Remove obsolete provisional/key residue | Exact `Available` only |

Only `Available` descriptors are resolver-visible. `Reserved`, `Encrypting`,
`Staged`, terminal, quarantined, and provider-only states cannot be frozen by a
B4 job. Provider-only orphans carry only non-secret reservation correlation and
deadline; object names never supply session, subject, class, or authority.

Named re-entry proofs are:

- `C1_same_owner_retry_after_disconnect_reenters_without_reservation_busy`;
- `C1_different_producer_remains_reservation_busy`;
- `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated`.

#### 5.6.5 Cleanup bounds

| State/residue | Owner | Deadline/limit | Required disposition |
| --- | --- | --- | --- |
| Evaluating claim alias/token | Ingress then reconciler | Token expiry + safe non-reuse horizon | In-place reclaim with higher revision/fence; Bound on exact completion; ConflictTombstone on unbound conflict; never rebind/delete while prior token can live |
| Bound/ConflictTombstone alias | Ingress metadata owner | At least canonical claim/source or anti-reuse horizon | Preserve anti-reuse binding; lifecycle purge only under a separately pinned proof that no old key can re-enter |
| Claim without `Staged` attempt | Producer then reconciler | Effective plaintext deadline and attempt deadline | Continue only with live producer; otherwise recapture/cleanup |
| Incomplete provisional object | Reconciler after fenced claim | Reconciliation budget | Abort object; destroy/revoke attempt key |
| Superseded attempt | Reconciler | Strictly before durable `DispositionExpiresAtUtc` | Delete object; destroy/revoke exact key |
| Complete recoverable object | Reconciler | R3–R6 budget | Verify and advance or exact terminal disposition |
| Hold-governed corrupt/invalid object | Lifecycle identity | Ratified hold release | Quarantine, no read/reuse; release resumes due purge |
| Provider-only orphan | Lifecycle identity | Minimum age + reconciliation budget | Correlate safely or delete |

Section 15.1 owns mandatory no-default limits for active attempts/source,
key-material records/source, provisional objects/source and plaintext
in-memory lifetime. One claim and one exact artifact map to one
`SourceArtifactId`, so no duplicate per-claim attempt limit exists. It also owns
the common bounded CaptureAgent retry backoff for capacity, idempotency-lock
and reservation busy. Atomic counters and exact-attempt rows enforce the three
source/key/object limits; every terminal/expiry/abort path releases or
reconciles its owned count. Retry-count accounting remains fail-closed behind
`C1-BB-RETRY-COUNT-ACCOUNTING-GATE`; it is not described as durable
cross-process enforcement. Missing, partial, malformed or relationally invalid
values fail readiness; no prose-only “bounded” claim is accepted.

The first monotonic CAS that makes an exact attempt require disposition writes
both `DispositionStartedAtUtc` from DB time and
`DispositionExpiresAtUtc = DispositionStartedAtUtc + DispositionEnvelope` on
that attempt/disposition record. The custody reconciler compares DB time with
the persisted expiry before its worker-claim/disposition CAS. Retry, lease
renewal, owner reclaim, process restart, provider/key rotation and repeated
cleanup do not reset or extend either timestamp.

## 6. Source descriptor and custody metadata

PostgreSQL may store only metadata. The immutable source descriptor must bind:

```text
SourceArtifactId
IngressIdempotencyKey
IngressIdentityFingerprint
AdmissionFingerprint
VerificationSessionId
SubjectRefTokenSchemaVersion
SubjectRefTokenKeyId
SubjectRefTokenKeyVersion
SubjectRefToken
ClientApplicationId
AuthenticatedPrincipalId
AuthenticatedApiKeyId
CaptureAcceptanceId
CaptureArtifactId
CaptureRevision
RawClass
ProducerId
CaptureAgentInstanceId
SessionChallengeHash
ContentCommitmentSchemaVersion
ContentCommitmentKeyId
ContentCommitmentKeyVersion
ContentCommitment
ClaimedPlaintextLength
MediaType
CapturedAtUtc
PlaintextRetentionStartedAtUtc
PlaintextRetentionExpiresAtUtc
PlaintextRetentionBudgetSeconds
EffectivePlaintextRetentionExpiresAtUtc
AuthoritySnapshotSchemaVersion
AuthoritySnapshotId
AbsoluteSourceExpiresAtUtc
ReservationExpiresAtUtc
SourceReservationFingerprint
EncryptionAttemptFingerprint
StagedCiphertextFingerprint
CustodyState
ReservationRevision
EncryptionAttemptRevision
Fence
StorageProfileId
SourceEncryptionProfileId
SourceEncryptionProfileVersion
OpaqueStorageLocator
CiphertextDigest
CiphertextLength
EncryptionSuiteId
EncryptionFramingVersion
NonceStrategyId
NonceDerivationSeedReferenceOrWrappedSeed
NonceDerivationSeedCommitment
ChunkSize
FramingParametersDigest
EncryptionEnvelopeMetadataDigest
WrappedKeyMetadataDigest
KeyProviderId
KekId
KekVersion
KekFingerprint
AttemptKeyReservationId
WrappedDekMetadataIfReturned
AuthenticatedCompletionRecordDigest
CreatedAtUtc
SchemaVersion
```

The bare claimed/actual plaintext digest is deliberately absent. Authority
snapshot fields are normalized into the immutable snapshot row and referenced
by id/version; append-only custody evidence carries the same identity.
The logical descriptor is normalized: source-stable fields live in one
immutable source row; each replacement attempt has one immutable attempt row
containing `EncryptionAttemptFingerprint`, key/object/framing context and actual
completion results; one fenced mutable head selects the current attempt/state.
No replacement rewrites a prior attempt or the source-stable fingerprint.
The fingerprint digest never substitutes for the listed source-encryption
profile, nonce strategy, recoverable seed/reference, chunk and framing inputs;
all are independently persisted and exact-readback tested.

`StorageProfileId` is an opaque infrastructure routing/audit discriminator.
Domain and application authorization/business semantics must not branch on a
vendor. Infrastructure routing, reconciliation, and audit may select an adapter
by that opaque identity. Provider selection must not change authority, policy,
retention, legal mode, error semantics, source binding, or seal semantics.

Custody claim, attempt creation/reclaim, staging, availability, authority loss,
hold, deletion request/completion, corruption, recapture requirement, and
terminal cleanup evidence is append-only. Mutable ownership/state heads use
revision/fence/CAS and never replace history.

Content-commitment and subject-token keys are durability-critical. Rows retain
exact schema/key versions. Keys bind to stable protected-data/controller scope,
not host/pod/deployment instance. Restore/relocation is incomplete without every
referenced historic key and `StableDataScopeId`. A key retires only after its
last dependent row expires/purges or a separately ratified evidence-preserving
migration completes. Readiness inventories every referenced version, projected
historic-version count (`maximum retention horizon / rotation interval`),
provider quota/cost, escrow/backup, DR transfer, and retirement evidence.
Fixture HMAC vectors use explicit public non-production keys rejected by
production readiness.

### 6.1 Immutable per-job source bindings

C1 owns one immutable mapping created before the first raw read:

```text
JobId
Ordinal
RawClass
CaptureAcceptanceId
CaptureArtifactId
CaptureRevision
SourceArtifactId
```

`UNIQUE(JobId, Ordinal)` and `UNIQUE(JobId, RawClass)` prevent ambiguous
selection. The mapping is per job, not exclusive ownership of the retained
source. Separate jobs may bind the same `SourceArtifactId` only after each
passes fresh independent authority for the exact subject, client,
session/workflow, purpose, consumer, class, expiry, and current
retention/hold/revocation state.

The landed `capture_artifacts` row is not this authority: it has no
`CaptureRevision`, and its initial `QualityState` does not prove final
acceptance. C1 therefore requires an additive, server-authored acceptance
surface before ingress or job binding can be implemented:

```text
tagekyc.raw_export_capture_acceptance_events
tagekyc.raw_export_session_capture_selections

pk_raw_export_capture_acceptance_events
uq_raw_export_capture_acceptance_revision
uq_raw_export_capture_acceptance_artifact
pk_raw_export_session_capture_selections
uq_raw_export_session_capture_selection_class
fk_raw_export_capture_acceptance_session
fk_raw_export_capture_acceptance_artifact
fk_raw_export_session_selection_acceptance

CaptureAcceptanceId
VerificationSessionId
ClientApplicationId
RawClass
CaptureArtifactId
CaptureRevision
SessionChallengeHash
AcceptedEvidenceRef
AcceptedAtUtc
AcceptancePolicyId
AcceptancePolicyVersion
```

The restricted server capture-acceptance component, never CaptureAgent or a
public caller, appends one event only after validating authenticated capture
metadata, exact session ownership/challenge, class-specific artifact identity,
positive monotonic `CaptureRevision`, and the acceptance policy. The authoritative
session-completion transaction writes one immutable selection per
`VerificationSessionId + RawClass`, referencing exactly one
`CaptureAcceptanceId`; it cannot select an artifact/revision absent from the
acceptance events. `UNIQUE(VerificationSessionId, RawClass)` on selections
prevents first/latest reinterpretation after completion. Ingress uses an exact
acceptance event; later B4 binding uses only the exact completion selection.
The two must reference the same artifact/revision or binding fails.

The acceptance-event PK is `CaptureAcceptanceId`.
`uq_raw_export_capture_acceptance_revision` covers
`VerificationSessionId + RawClass + CaptureRevision`;
`uq_raw_export_capture_acceptance_artifact` covers
`VerificationSessionId + RawClass + CaptureArtifactId + CaptureRevision`.
The selection PK is its server-generated selection id and
`uq_raw_export_session_capture_selection_class` covers
`VerificationSessionId + RawClass`. The three named FKs bind event→session,
event→capture artifact and selection→acceptance event. All identifiers must pass
the same static 63-byte and catalog round-trip gates.

This is a required C1/predecessor metadata dependency, not a claim about landed
schema. Its exact migration/producer integration and cross-repository allowlist
must be separately ratified before a Build Brief; without it, implementation
remains STOP/RRI.

Selection uses the exact `CaptureAcceptanceId`, `CaptureArtifactId`, and
`CaptureRevision` referenced by that authoritative session selection for
`VerificationSessionId + RawClass`. The public caller cannot choose a source.
First/latest/provider order/nearest timestamp are forbidden. Missing evidence
returns `RAW_EXPORT_SOURCE_SELECTION_NONE`; conflicting or multiple accepted
evidence for the discriminator returns
`RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS`.

Freeze uses one atomic insert/CAS. Same job/ordinal/class and same selected
source returns `ExistingMatch`; a concurrent loser proposing a different source
returns `RAW_EXPORT_SOURCE_SELECTION_CONFLICT` with no overwrite. Every retry,
reclaim, and successor attempt for the same `JobId` reuses the mapping; recapture
cannot replace it. A future authenticated non-public administrative selector is
not foreclosed but requires a separate ratified amendment.

Every resolution/read verifies the frozen producer, actor, session, challenge,
subject, client, class, source artifact, and applicable capture artifact.
Unreadable, revoked, or expired frozen sources return
`RAW_EXPORT_SOURCE_UNAVAILABLE`; any binding substitution returns
`RAW_EXPORT_SOURCE_BINDING_INVALID`.

Future negative tests and separate bite-proving mutations must cover:

```text
cross-session substitution
cross-subject substitution
cross-client substitution
cross-class substitution
source-artifact substitution after binding freeze
```

The positive retained-vault proof uses two different `JobId` values, two
independent authority decisions, two immutable per-job mappings, and the same
retained `SourceArtifactId`. It succeeds only while retention, hold,
revocation, purpose, consumer, and expiry state permits; it cannot weaken any
substitution rejection.

### 6.2 Canonical prohibited-content contract

The following content is default-forbidden from descriptors exposed outside
the restricted repository, provider manifests, assembly metadata, operation
results, errors, logs, exception messages/data, trace tags/baggage, metric
labels, audit payloads, diagnostics, application-controlled crash dumps, and
dead-letter metadata:

```text
raw plaintext bytes
raw ciphertext bytes
unwrapped DEKs
KEKs
provider credentials
SecretIDs or tokens
any storage locator
bucket name
object key
filesystem root
arbitrary path
provider endpoint
public URL
vault reference
provider handle
signed URL
retrieval token
wrapped-key material
bare claimed or actual Raw BIO digest
content commitment or subject token outside its restricted record
SubjectRef on logging/telemetry surfaces
descriptor fields not explicitly allowlisted for that consumer
raw provider exception text
```

The only narrow exceptions are ciphertext bytes inside the provider payload or
bounded ciphertext stream; an opaque locator inside the restricted custody
repository/adapter record; and approved wrapped-key metadata inside the
restricted key/custody record. The session-scoped
`ProducerClaimEnvelopeFingerprint` is permitted only in the ingress alias/token
comparison surface defined by D4 and section 5.6.1. Its canonical v2 preimage
contains no digest, commitment or other content-derived value; it therefore
freezes only non-content claims and cannot confirm candidate plaintext. It is
never a content index, source descriptor, response, evidence, audit, metric or
log field and does not authorize persistence of a bare digest. Those values may never be copied into the
manifest, caller response, observability, dead-letter, or public/general API
surface. All local forbidden lists reference this section rather than creating
weaker variants.

## 7. Producer handoff

### 7.1 One-call external transport

Each CaptureAgent invocation uses one external authenticated custody-ingress
operation shape for one Raw BIO artifact. A retry invokes that same operation
shape again with the same UUID; there is no external
begin/upload-part/complete sub-protocol. Section 10.0 is the normative P0–P7
phase × outcome contract. The operation first transmits metadata only and then
selects exactly one non-overlapping branch:

```text
CaptureAgent transmits metadata only
→ ingress advances only as far through P0–P3 as the phase table permits:
    authenticate/validate metadata
    → custody capacity admission
    → internal begin/broker/complete when earlier phases did not terminate
→ exactly one branch is selected at the first fixed outcome

Branch A — metadata-only termination:
  a P0–P3 final condition, or an InternalClaimResult mapped final, occurs
  → the phase table maps it to CaptureAgentFinalResult
  → AdmissionAccepted is not emitted
  → no body is requested, accepted or transmitted
  → no new R1, attempt, key or object is created for this call

Branch B — body required:
  a new durable R1 or eligible same-owner re-entry CAS commits
  → server emits transport-level AdmissionAccepted
  → CaptureAgent transmits the one bounded body stream
  → internal R2 → R3 → R4 → R5 → R6
  → server emits exactly one CaptureAgentFinalResult
```

`begin`, broker comparison, broker-only `complete`, R1–R6 and
`InternalClaimResult` are internal TagEkyc steps inside that one external
operation. They are not independent calls available to CaptureAgent.
`AlreadyAvailable`, `ClaimEvaluationInProgress`, both busy outcomes,
fingerprint conflict, authority/retention/capability/key/token failures and
every other P0–P3 final outcome listed in section 10.0 select Branch A.
Only a committed new R1 or eligible same-owner/reclaim continuation selects
Branch B.

This slice has no upload session, upload id, transport part, part receipt,
resume endpoint, public completion call or agent-held storage/object
capability. Encryption `ChunkSize` frames AEAD ciphertext only; it is not a
transport part and creates no resumability.

Before `AdmissionAccepted`, CaptureAgent **must not transmit any body byte**.
The ingress edge, including every reverse proxy and middleware hop, must not
request-buffer, pre-read, pre-buffer, spool or spill the body before committed
R1. Disk buffering and spill-to-disk are forbidden at every hop for the entire
operation. Section 10.0 separates two reachable early-body dispositions:

- P0–P2: zero alias/shell/R1/source/attempt/key/object. P3 before R1: zero
  committed R1/source/attempt/key/object; an already committed Evaluating alias
  and non-source shell retain their safe-horizon disposition. The exact final is
  `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`, and only the bounded
  memory-only network residual is discarded;
- P4, after R1 committed but before `AdmissionAccepted` was successfully written
  and flushed, while the body reader and provider writer are not armed: the
  same protocol-invalid final code, but never a zero-R1 claim.
  CAS the exact current reservation revision/fence to
  `AdmissionProtocolRejected`, record
  `R2TerminationDisposition = TerminatedBeforeStart`, terminalize the exact
  attempt, deterministically abort/forget the provisional object identity and
  destroy/revoke its attempt-key reservation, and release custody capacity.
  The Bound alias and canonical idempotency identity remain durable so
  anti-reuse is preserved.

After a P4 rejection the exact same authenticated owner may restart the same
UUID only from metadata, only after correcting transport behavior, while it
still owns the unchanged buffer and the full section 15 runtime projection
fits. `begin` must observe the Bound canonical
`AdmissionProtocolRejected` disposition and CAS a new monotonic reservation
revision/attempt/fence; another owner, changed identity or stale CAS cannot
restart it. No final result includes revision, fence or cleanup detail.

Mandatory integer no-default configuration
`RawExportIngressMaximumPreAdmissionBufferedBytes`, Int32 bytes in
`[1, 65536]`, pins the maximum unavoidable pre-admission network buffering
across the qualified ingress topology. Readiness is false as
`RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID` when the proxy/middleware
configuration cannot prove: metadata-first admission signaling, buffering
disabled, disk spill disabled, the configured bound, and pass-through of early
body rejection.

The honest residual is not “zero bytes physically reached the server.” Kernel
socket and TLS-record buffers may contain up to the pinned bounded number of
early bytes. They must remain memory-only, never be copied into application
buffers before R1, never reach proxy/middleware disk, and be discarded on
protocol rejection. Acceptable realizations include HTTP
`Expect: 100-continue` with a pinned non-buffering proxy topology, or a duplex
RPC whose first client frame is metadata and whose first server frame is
`AdmissionAccepted`. This brief mandates properties, not a framework.

Transport failure in P4 while writing/flushing `AdmissionAccepted`, or
abort/cancellation/incomplete body in P5, fails the external operation as
`RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` and invokes exact fenced cleanup
under section 7.2. P4 records `TerminatedBeforeStart`; P5 must prove all four
`PreviousFencedR2Terminated` conditions. This outcome is not a replay-stable
terminal disposition. On the next same-UUID invocation, termination not yet
durable returns `RAW_EXPORT_SOURCE_RESERVATION_BUSY`; durable termination plus
sufficient registered continuation budget permits same-owner re-entry; an
insufficient budget or lost buffer returns `RECAPTURE_REQUIRED`. No retry can
revive an expired producer buffer, and a cleanly completed content-claim
mismatch is not this retryable transport case.
The retained ownership marker does not itself block the retry: the returning
owner is `SameOwnerRetryPendingTermination` until
`PreviousFencedR2Terminated` proves the old reader and provider writer can no
longer produce a byte, then becomes `SameOwnerReentryReady`. Only the ready case
may start replacement R2.

Real transport tests must cross a reverse proxy or an equivalent buffering
harness. `C1_admission_handshake_rejects_early_body_and_proxy_prebuffering`
must prove P0–P3 early-client-body/proxy-prebuffer cases return the exact
protocol-invalid outcome with zero disk spill and zero new R1/provider/key
residue, and prove the P4 case preserves its committed alias/idempotency
identity while terminalizing/cleaning only the exact attempt.
`C1_early_body_after_R1_before_admission_terminalizes_exact_attempt` is the
dedicated P4 proof: pause after R1 commit, inject early body before the signal
is successfully written and flushed, and assert `AdmissionProtocolRejected`,
`TerminatedBeforeStart`, exact object/key/capacity cleanup, retained Bound alias
and successful eligible same-owner metadata restart.
`C1_admission_signal_follows_committed_R1_before_agent_body_send` must prove the
agent sends no body before the signal and that the signal follows committed R1.
`C1_already_available_returns_without_admission_body_or_new_R1` must prove an
already-Available artifact returns `AlreadyAvailable` with zero body requested
or transmitted and no new R1/attempt/key/object.
`C1_metadata_terminal_outcomes_never_request_or_accept_body` must independently
cover a live evaluation, fingerprint conflict, each busy class and authority
failure with no `AdmissionAccepted` and no body.

For each artifact, CaptureAgent must provide:

- one exact `RawExportRawClass`;
- the corresponding `CaptureArtifactId`;
- the accepted `CaptureRevision`;
- session, subject, and client binding obtained from the trusted session;
- producer and CaptureAgent instance identity;
- claimed byte length/media type and a class-specific SHA-256 digest computed
  from the exact submitted bytes;
- the exact session-challenge hash already bound into the landed capture
  evidence;
- capture timestamp and bounded stream;
- one stable v0.8 UUID ingress idempotency key;
- `PlaintextRetentionStartedAtUtc`, `PlaintextRetentionExpiresAtUtc`, and
  `PlaintextRetentionBudgetSeconds`, derived from its local monotonic lifetime.

### 7.2 Artifact-size, memory, and concurrency admission

The ratified capacity topology is **Model A: exact runtime reservations split
at the physical host boundary**. CaptureAgent owns its retained plaintext
buffer; the custody server owns only its bounded active-stream plaintext
window. A single number must never claim to measure memory on both machines.

Every key below is mandatory, integer, has no implicit default, uses checked
conversion/arithmetic and is validated by both readiness and runtime admission:

| Physical owner | Configuration key | Type, unit and inclusive range | Enforcement |
| --- | --- | --- | --- |
| Shared class contract | `RawExportSourceMaximumChipDg2PortraitBytes` | Int64 bytes, `[1, 67108864]` | Exact per-artifact ceiling for `ChipDg2Portrait`; configured on both sides and refused by the server before `begin` |
| Shared class contract | `RawExportSourceMaximumLiveSelfieImageBytes` | Int64 bytes, `[1, 67108864]` | Exact per-artifact ceiling for `LiveSelfieImage`; configured on both sides and refused by the server before `begin` |
| CaptureAgent host | `RawExportCaptureMaximumConcurrentRetainedBuffersPerHost` | Int32 buffers, `[1, 32]` | Maximum locally retained producer buffers on one authenticated CaptureAgent host |
| CaptureAgent host | `RawExportCaptureMaximumAggregatePlaintextBytesPerHost` | Int64 bytes, `[1, 2147483647]` | Sum of exact declared-byte reservations for locally retained producer buffers |
| Custody/server host | `RawExportCustodyMaximumPlaintextWindowBytesPerStream` | Int64 bytes, `[1, 16777216]` | Maximum active plaintext working window/chunk for one custody stream; never an artifact-size limit |
| Custody/server host | `RawExportCustodyMaximumConcurrentStreamsPerProducer` | Int32 streams, `[1, 32]` | Concurrent custody streams for one authenticated producer |
| Custody/server host | `RawExportCustodyMaximumConcurrentStreamsPerDeployment` | Int32 streams, `[1, 256]` | Concurrent custody streams across the deployment |
| Custody/server host | `RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment` | Int64 bytes, `[1, 2147483647]` | Sum of exact active-stream plaintext-window reservations on the custody deployment |

The tuple is valid only when:

```text
MaximumChipDg2PortraitBytes
  <= RawExportCaptureMaximumAggregatePlaintextBytesPerHost

MaximumLiveSelfieImageBytes
  <= RawExportCaptureMaximumAggregatePlaintextBytesPerHost

RawExportCustodyMaximumPlaintextWindowBytesPerStream
  <= RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment

RawExportCustodyMaximumConcurrentStreamsPerProducer
  <= RawExportCustodyMaximumConcurrentStreamsPerDeployment
```

There is deliberately **no** readiness relation requiring either aggregate
ceiling to cover `per-reservation maximum × concurrency maximum`. Such a
worst-case-cover relation would make aggregate exhaustion unreachable after the
slot gate passes. Checked multiplication is still exercised in boundary tests
to prove overflow-safe fixture construction, but it is not a production
readiness predicate.

CaptureAgent atomically reserves one local buffer slot plus the exact declared
artifact bytes before it retains/submits the buffer. Its local aggregate can
therefore bind while buffer slots remain. Local capacity failure is resolved by
the CaptureAgent SDK and the external operation is never started. No local
reservation id, capacity assertion, attestation or dynamic proof is sent to the
server. CaptureAgent readiness and local admission exclusively own that
guarantee; process/host termination releases the in-memory reservation.

Independently, before `begin` the custody server atomically reserves one
per-producer/deployment stream slot plus the exact configured plaintext-window
cost for the selected stream profile. Its aggregate window reservation can bind
while stream slots remain. Server code directly enforces this reservation,
allocates no larger plaintext window and releases it on every supported exit.

Before `begin`, the trusted ingress boundary rejects a non-positive declared
length or a declared length above its exact class ceiling as
`RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED`. After validating the producer
lifetime, it atomically admits only the custody stream-slot/window reservation;
failure of a custody producer/deployment slot or custody aggregate-window
reservation is `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`. The server neither
enforces nor claims knowledge of CaptureAgent process memory. These server
outcomes occur before alias/shell creation and before provider or key I/O.
Server admission is released on every success, failure, cancellation and
graceful exit. Its durable crash horizon is exact:

```text
CapacityAdmissionExpiresAtUtc =
  min(PlaintextRetentionExpiresAtUtc,
      CapacityAdmissionStatementTimestamp
      + ConfiguredMaximumPlaintextRetentionBudget)
```

A crashed holder expires no later than that timestamp; the capacity reconciler
uses CAS and cannot extend it. Capacity is never an unbounded process-local
counter. CaptureAgent-local reservations are not represented as server-owned
memory; agent process/host failure destroys the retained plaintext and the same
UUID can continue only if the original buffer still exists under section 7.1.

The bounded stream uses an independent checked Int64 byte counter during R2/R3.
The first byte that would exceed the declared length or exact class limit
terminates the read as
`RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED`; no additional plaintext is
accepted and no descriptor can become `Available`. Because the canonical
R1-before-body-read order is intentional, an understated stream can be
discovered only while R2 consumes it; the exact fenced provisional object/key
is then aborted and cleaned.

Section 10.0 owns the two disjoint body-termination classifications:

- transport abort, caller cancellation, or an otherwise incomplete body is
  non-terminal/non-replay-stable
  `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`; fenced R2 cleanup runs, and the
  next same-UUID invocation follows the section 10.0 busy/re-entry/recapture
  rule from durable termination, section 15.1 budget and buffer evidence;
- a normally completed body with clean EOF whose actual length, digest or keyed
  commitment disagrees with the authenticated claim is
  `CONTENT_COMMITMENT_MISMATCH`; it is terminal for that claim and same-UUID
  retry is refused.

The document must not claim that an actual byte count can be known before the
body is read.

Missing, non-integer, out-of-range, partial, overflowed or relationally invalid
capacity configuration fails readiness as
`RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID`. Equality, one unit below,
one unit above, Int32/Int64 overflow, missing-one-key, class substitution,
understated actual length, custody producer/deployment
stream-slot/aggregate-window races and all server release paths are mandatory
server tests. Fixtures must independently make the custody aggregate bind while
its adjacent slot remains available, and make each custody slot bind while its
aggregate remains available. Temporarily removing each reachable server limit,
reservation or release path must turn
`C1_artifact_size_memory_and_concurrency_limits_fail_closed` red.

CaptureAgent buffer-slot/aggregate-byte equality, overflow, exhaustion and
release fixtures belong exclusively to CaptureAgent readiness and the named
cross-repository test
`CaptureAgent_raw_export_local_capacity_limits_fail_closed`. They are not
server-side `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` cases. A later dynamic
agent attestation would require a separately ratified schema/binding contract;
the Build Brief must not invent one.

These are authenticated producer claims, not verified server content evidence.
The custody boundary derives the keyed commitment, then independently recomputes
digest/length while encrypting. It rejects mismatched class, session, subject,
client, artifact/revision, length, media type, commitment, producer, lifetime,
authority, or duplicate-conflict input with no `Available` source.

`ChipDg2Portrait` must not reuse the aggregate `NfcReadArtifact` identity. The
initial class subset requires a new class-specific capture-artifact type and
`CaptureArtifactId` for the exact DG2 portrait bytes, submitted before raw
custody. Its digest is computed from those exact bytes. The aggregate NFC
artifact may remain as evidence input for existing checks, but it is neither the
DG2 source identity nor the DG2 digest.

CaptureAgent retains plaintext only inside the active bounded operation, within
the effective deadline and retry budget. It executes verified zeroization before
every graceful terminal return/exit. Abrupt process/host loss cannot execute
that routine and requires recapture unless complete ciphertext makes every
remaining step independent of the producer-held buffer. Reconciliation may
still transiently decrypt bounded plaintext chunks under section 5.6.3. A provider reference alone is not
success; exact metadata and complete provider content must agree.

## 8. C1 assembly contract

The C1 worker receives only:

- the actor-scoped B4 job identity;
- the current attempt, latest revision, fencing token, lease owner, and lease
  expiry;
- the exact ordered job classes;
- the immutable C1 per-job source mapping and only its exact `Available`
  descriptors;
- scoped provider and key capabilities.

The canonical assembly manifest must bind:

```text
AssemblyId
AssemblyFingerprint
JobId
PermitId
AuthorizationDecisionId
AttemptId
FencingToken
VerificationSessionId
SubjectRefTokenSchemaVersion
SubjectRefTokenKeyId
SubjectRefTokenKeyVersion
SubjectRefToken
ClientApplicationId
RecipientClientApplicationId
PolicyId
PolicyVersion
PurposeCode
ExportMode
JobExpiresAtUtc
ordered (Ordinal, RawClass, SourceArtifactId,
         ContentCommitmentSchemaVersion, ContentCommitmentKeyVersion,
         ContentCommitment, PlaintextLength, MediaType)
ManifestVersion
CreatedAtUtc
AssemblyDigest
AssemblyAuthenticationKeyId
AssemblyAuthenticationKeyVersion
AssemblyAuthenticationValue
```

The canonical identity/hash codec is the landed
`EvidenceCanonicalization` RFC-8785 JCS + SHA-256 convention. GUID values use
lowercase `N` strings. Every persisted/replayed timestamp is first converted to
UTC, truncated to whole microseconds with
`normalizedTicks = ticks - (ticks % 10)`, and formatted with exactly six
fractional digits plus `Z`. Rounding, hashing 100-nanosecond values before
database truncation, and seven fractional digits in a persisted replay identity
are forbidden. Enums use their exact names, hashes use lowercase
`sha256:<hex>`, integer values use invariant decimal strings rather than JSON
numbers, arrays preserve declared order, and every domain label below is
literal. D3 must freeze golden vectors and the authentication algorithm/key
provider; it may not change these preimages:

`SubjectRefToken` is:

```text
SubjectRefTokenSchemaVersion = 1
domain = "TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN:C1:V1"
SubjectTokenKeyBytes =
  ResolveSubjectTokenKeyBytes(
    SubjectRefTokenKeyId,
    SubjectRefTokenKeyVersion)
SubjectRefToken =
  HMAC-SHA-256(SubjectTokenKeyBytes, LP({
    domain,
    StableDataScopeId,
    ControllerIdentity,
    NFC(SubjectRef)
  }))
```

`SubjectRef` must be valid Unicode, normalized exactly once to NFC, encoded
UTF-8 and length-prefixed with the same codec as section 5.6. Invalid Unicode is
rejected. The dedicated subject-token key is not the commitment key, DEK, KEK,
or signing key. Token/key data is restricted metadata and never public/logged.

| Value | Scope | Exact derivation/preimage |
| --- | --- | --- |
| `AssemblyId` | Job-stable | `DeterministicGuid("tip-88c1-assembly-id-v1", { JobId })` |
| `AssemblyFingerprint` | Job-stable | `C1HashCanonical("tip-88c1-assembly-fingerprint-v2", { AssemblyId, JobId, PermitId, AuthorizationDecisionId, VerificationSessionId, SubjectRefTokenSchemaVersion, SubjectRefTokenKeyId, SubjectRefTokenKeyVersion, SubjectRefToken, ClientApplicationId, RecipientClientApplicationId, PolicyId, PolicyVersion, PurposeCode, ExportMode, JobExpiresAtUtc, OrderedItems[Ordinal, RawClass, SourceArtifactId, ContentCommitmentSchemaVersion, ContentCommitmentKeyVersion, ContentCommitment, PlaintextLength, MediaType] })` |
| `AssemblyDigest` | Job-stable when inputs and canonical assembly are valid | SHA-256 over the non-self-referential canonical bounded assembly byte stream below |
| `ManifestDigest` | Attempt-scoped | `C1HashCanonical("tip-88c1-assembly-manifest-v1", manifest body)` where the body contains every listed manifest field except `AssemblyAuthenticationValue`, and includes the current attempt/fence, normalized attempt `CreatedAtUtc`, authentication key id/version, stable fingerprint, and stable assembly digest |
| `AssemblyAuthenticationValue` | Attempt-scoped | D3-selected authenticator over the exact `ManifestDigest` under the listed assembly-authentication key id/version; it cannot authenticate a different manifest or key version |
| `C2PreparationFingerprint` | Attempt-scoped | `C1HashCanonical("tip-88c1-c2-preparation-v1", { AssemblyId, AssemblyFingerprint, AttemptId, FencingToken, ManifestDigest, AssemblyDigest, AssemblyAuthenticationKeyId, AssemblyAuthenticationKeyVersion, AssemblyAuthenticationValue, RecipientClientApplicationId })` |
| `C2PreparationId` | Attempt-scoped deterministic identity | `DeterministicGuid("tip-88c1-c2-preparation-id-v1", { C2PreparationFingerprint })`; C1 supplies this id to Prepare, same fingerprint/id exact-matches, a distinct fingerprint yields a distinct id, and only the id referenced by the committed C1 seal can finalize |

The `tip-88c1-assembly-stream-v1` byte stream is:

```text
ASCII "TIP-88C1-ASSEMBLY-V1"
|| UInt32BE(headerByteLength)
|| UTF8(RFC8785-JCS({
     ManifestVersion,
     AssemblyId,
     JobId,
     PermitId,
     AuthorizationDecisionId,
     VerificationSessionId,
     SubjectRefTokenSchemaVersion,
     SubjectRefTokenKeyId,
     SubjectRefTokenKeyVersion,
     SubjectRefToken,
     ClientApplicationId,
     RecipientClientApplicationId,
     PolicyId,
     PolicyVersion,
     PurposeCode,
     ExportMode,
     JobExpiresAtUtc,
     OrderedItems[Ordinal, RawClass, SourceArtifactId,
                  ContentCommitmentSchemaVersion,
                  ContentCommitmentKeyVersion,
                  ContentCommitment, PlaintextLength, MediaType]
   }))
|| for each OrderedItem in ascending Ordinal:
     UInt32BE(ordinal)
     || UInt64BE(plaintextLength)
     || exact plaintext bytes
```

All listed values are required; absence/null is invalid. GUIDs use lowercase
`N`, the timestamp uses normalized six-fraction UTC, enums use exact spelling,
digests use lowercase `sha256:<hex>`, lengths are invariant non-negative
integers in JCS strings and unsigned big-endian values in binary framing, and
array order is semantic. `AssemblyDigest`, `ManifestDigest`, authentication,
attempt/fence/time, C2 preparation identity, provider locator, and transport
chunking are excluded, so the digest cannot contain itself. Dependency is:

The v1 source-reservation and assembly-fingerprint codecs remain registry
entries for historic/fixture verification only. v0.8 creates only
`tip-88c1-source-reservation-v2` and
`tip-88c1-assembly-fingerprint-v2`; no existing row is silently reinterpreted.
HMAC golden vectors ship explicit public fixture-only keys which production
readiness rejects.

```text
canonical assembly stream
→ AssemblyDigest
→ manifest body
→ ManifestDigest
→ AssemblyAuthenticationValue
→ C2 idempotency fingerprint
```

`CreatedAtUtc` is captured once per attempt before C2 preparation and is reused
byte-for-byte for retry of that preparation. `AssemblyAuthenticationValue` is
likewise created once for that attempt and reused exactly; an ambiguous
key-provider
result cannot be recomputed and presented as the same attempt, so the attempt
fails closed and a reclaim creates a new attempt-scoped manifest. Reclaim
changes `AttemptId`, `FencingToken`, `CreatedAtUtc`, `ManifestDigest`,
`AssemblyAuthenticationValue`, C2 idempotency fingerprint, and therefore
`C2PreparationId`. It must not change `AssemblyId`, `AssemblyFingerprint`,
`AssemblyDigest`, or the ordered item content when the immutable source set is
unchanged. A different stable fingerprint for the same `AssemblyId` is a
logical assembly conflict; a different attempt-scoped manifest for the same
stable fingerprint is not.

The sealed manifest envelope contains the manifest body plus
`AssemblyAuthenticationValue`; the value is not recursively included in its own
`ManifestDigest`. The manifest must not contain raw bytes, provider
credentials, unwrapped keys, or any storage locator; section 6.2 applies.

The assembly byte stream is bounded and ephemeral. C1 may hand it only to the
internal C2 sink contract or a test-only fixture sink. C1 must not persist a
durable plaintext assembly. Until C2 exists, production readiness remains false.

### 8.1 Immutable assembly persistence

C1 adds exactly three metadata-only aggregates:

```text
raw_export_assembly_identities
raw_export_assembly_items
raw_export_assembly_preparation_dispositions
```

`raw_export_assembly_identities` has one row per job enforced by
`UNIQUE(JobId)` and binds authoritative `JobExpiresAtUtc`, the complete
manifest digest, assembly fingerprint, authentication metadata, committed
attempt/fence, and `C2PreparationId`.
`raw_export_assembly_items` contains the exact non-empty ordered manifest item
set. `raw_export_assembly_preparation_dispositions` is keyed by exact
`(AssemblyId, C2PreparationId)` and carries the monotonic section 8.2 barrier.
None stores raw, plaintext assembly, ciphertext package, provider credential,
bare Raw BIO digest, or any storage locator; section 6.2 applies.

The exact disposition `Pending → SealCommitted`, assembly identity, complete
item set, B4 head CAS, and `AssemblySealed` transition are the five surfaces
written in the one transaction defined by D6. A deferred integrity guard must
prevent an assembly identity from committing without its complete ordered item
set. Runtime receives no direct table mutation privilege.

### 8.2 C2 prepare/finalize/abort protocol

The conceptual internal sink is:

```text
PrepareAsync(C2PreparationId, AssemblyPreparationRequest, boundedAssemblyStream)
GetPreparationAsync(C2PreparationId, AssemblyId)
FinalizeAsync(C2PreparationId, AssemblyId)
AbortAsync(C2PreparationId, AssemblyId)
```

`PrepareAsync` binds:

```text
AssemblyId
AssemblyFingerprint
JobId
AttemptId
FencingToken
CreatedAtUtc
ManifestDigest
AssemblyDigest
AssemblyAuthenticationKeyId
AssemblyAuthenticationKeyVersion
AssemblyAuthenticationValue
RecipientClientApplicationId
idempotency fingerprint
```

It verifies digest/authentication with a capability separate from source
encryption, consumes the stream once, and returns `Created` or `ExistingMatch`
for the caller-supplied deterministic `C2PreparationId`. Preparation is
non-deliverable and cannot advance B4. Same C2 fingerprint/id returns exact
match; the same id with different content conflicts; a distinct attempt
fingerprint creates a distinct quarantined id. `GetPreparationAsync` is exact-id
only and returns a closed state/digest match, never lists or discloses custody
location. Only the exact id referenced by the committed seal can finalize.

Canonical order:

```text
finish local authenticated assembly
→ verify current attempt/fence locally
→ fresh authority/deadline check
→ derive C2PreparationFingerprint and deterministic C2PreparationId
→ persist exact (AssemblyId, C2PreparationId) Preparing disposition candidate
→ C2 PrepareAsync with that id (outside DB transaction)
→ verify Created/ExistingMatch via exact id and CAS Preparing → Pending
→ stop and join periodic renewal; snapshot latest revision/fence
→ if needed, execute at most one synchronous final renewal from that snapshot
→ carry only the monotonic latest committed revision/fence
→ SealAssemblyAsync:
     fresh authority/deadline revalidation
     exact preparation-disposition CAS to SealCommitted
     immutable assembly identity/items
     B4 Assembling → AssemblySealed CAS
     transition append
→ if seal returns success/ExistingMatch: C2 FinalizeAsync idempotently
→ if seal returns SealOutcomeUnknown:
     do not abort; exact replay/reconciliation determines commit
→ otherwise authorize Abort only through the exact disposition CAS
→ C2 AbortAsync only after AbortAuthorized
```

No `Preparing` row or Prepare call occurs if the first authority check fails.
Authority loss during or after Prepare is caught before `Pending` CAS or in
Seal; a created preparation may remain quarantined but then follows exact
disposition arbitration. A stale worker that passed the first check may finish
quarantined output but can neither seal nor finalize it.

Transport timeout, connection loss, or another indeterminate seal response is
`SealOutcomeUnknown`. It never authorizes abort. Exact replay/reconciliation
reads the durable disposition.

#### 8.2.1 Durable preparation-disposition barrier

Arbitration is keyed by `(AssemblyId, C2PreparationId)` and one lock/CAS
authority shared by preparation registration, new-seal and abort-authorization
decisions:

```text
Preparing → Pending → SealCommitted → Finalized
     ↘         ↘ AbortAuthorized → Aborted
```

`SealCommitted` and `AbortAuthorized` are mutually exclusive and monotonic.
`SealAssemblyAsync` refuses `AbortAuthorized`; abort never runs from a scan
result alone.

| Durable observation | Exact action |
| --- | --- |
| `Preparing`, exact C2 id absent | Exact retry Prepare or CAS `AbortAuthorized` only after the same attempt can never continue |
| `Preparing`, exact C2 id Created/ExistingMatch and fingerprint equal | CAS to `Pending` |
| `Preparing`, exact C2 id conflicts | CAS `AbortAuthorized`; fail closed |
| Seal references target `C2PreparationId` | CAS/observe `SealCommitted`; never abort; finalize target |
| Seal references another preparation | CAS target to `AbortAuthorized`; abort target |
| No seal, and higher attempt/fence, terminal/expired job, `JobExpiresAtUtc`, or atomic failure transition proves target can never seal | CAS target to `AbortAuthorized`; abort target |
| No seal and current attempt can still seal | Remain `Pending`; do not abort |
| Seal result unknown | Remain/reconcile; never infer abort |

A seal for preparation B can never finalize A. The predicate is:

```text
CommittedAssembly.C2PreparationId == TargetC2PreparationId
```

The preparation deadline only makes a record eligible for reconciliation. It
does not authorize deletion and cannot be shorter than a valid seal window.
`JobExpiresAtUtc` is the final new-seal cutoff; earlier safe abort requires a
monotonic higher fence/attempt or terminal transition. There is no unregistered
C2 orphan: the deterministic id and `Preparing` row exist before external I/O.
A lost Prepare response is resolved only by exact-id replay/lookup, then the
same monotonic barrier authorizes `Pending` or `AbortAuthorized`.

Authority withdrawal after a committed seal does not abort its exact
preparation. It finalizes for custody convergence while every later
read/reuse/delivery is blocked and ratified purge/hold policy applies.
Finalization is not delivery authority.

#### 8.2.2 Renewal serialization

Periodic renewal is single-flight. Before seal the worker stops and joins it,
rejects any stale/lower revision or fence response, snapshots the latest state,
and may perform at most one synchronous final renewal. A delayed/lower response
never overwrites newer state. No renewal remains in flight during Seal; exact
committed replay skips Prepare and renewal.

Until C2 implements this contract, the fixture sink must model all three
operations, idempotency, failure, crash windows, stale-fence behavior, and
`SealOutcomeUnknown`. It must be memory-only or bounded encrypted-at-rest,
non-exportable, unavailable to production consumers, purged on abort and after
finalization, and excluded from real-artifact, production, legal/compliance,
audit, security, readiness, performance, retention-policy, and
production-provider-qualification evidence. An unbounded plaintext or
hash-and-discard sink cannot satisfy C1 closeout or production readiness.

## 9. Canonical ordering graph

### 9.1 Source establishment

The sequence below shows the maximum body-required path. It is not
unconditional: after every P0–P3 check, section 10.0 may terminate Branch A
immediately, skip every later arrow, emit no admission and request no body.

```text
CaptureAgent invokes the one external operation shape and transmits metadata only
→ restricted ingress authenticates and validates metadata; proxy/middleware
  buffering and disk spill are disabled
→ derive producer identity from authenticated principal/API key
→ validate idempotency UUID and exact server-authored capture acceptance event,
  revision, session/challenge, subject-token, client, producer and class binding
→ validate selected mode/class/provider/key capability
→ validate declared size against the exact class ceiling
→ validate CaptureAgent lifetime claim and immutable authority snapshot
→ reserve custody per-producer/deployment stream concurrency and exact
  aggregate plaintext-window capacity with the bounded crash horizon before
  begin/provider/key I/O
→ begin claim; claim/observe the four-field alias row, then persist/observe only
  a non-source canonical ClaimEvaluating shell and freeze active selector, or
  target the exact complete canonical row and freeze its historic selector;
  mint stateful opaque token/digest under the matching variant
→ claim-comparison broker validates token and internally derives exactly one
  DerivedAdmission / ActiveKeyUnavailable / HistoricKeyUnavailable /
  TokenInvalid result
→ the broker calls complete for that same token/alias invocation:
    validate exact token digest/variant/audience/alias/identity/revision/fence
    → freshly revalidate current session/acceptance/authority
    → interpret typed broker result under canonical precedence
    ExistingCandidate exact → bind alias and return idempotent ExistingMatch
    Existing/New conflict → burn alias as ConflictTombstone
    NewCandidate exact → CAS-consume token + bind alias + one atomic R1
      allocation and complete recovery-context commit
→ broker returns one InternalClaimResult to ingress
→ select exactly the section 10.0 phase-table branch:
    metadata-only final:
      map ExistingMatch(Available), live evaluation, conflict, busy, authority,
      retention, capability, key or token outcome directly to
      CaptureAgentFinalResult; emit no AdmissionAccepted; request/accept no body;
      create no new R1/attempt/key/object for this call
    body-required progression:
      only NewReservation or eligible same-owner/reclaim progression continues;
      R1 or exact re-entry CAS is already committed before application/proxy
      body consumption; bounded memory-only kernel/TLS buffering may exist
      within the pinned transport ceiling
      → server emits transport-level AdmissionAccepted
      → only then CaptureAgent transmits and ingress consumes one bounded stream
      → if a prior call died, same-owner re-entry CASes current owner/revision/
        fence and conditionally proves/budgets PreviousFencedR2Terminated
      → R2 fences, enforces actual byte limits, and streams plaintext once
        through verification + AEAD into the exact provisional object
      → R3 CAS actual results + verified commitment + StagedCiphertextFingerprint
      → R4 fresh-authority exact staged commit
      → R5 fresh-authority committed locator + Available evidence
      → R6 clean provisional residue
      → map the exact final outcome to CaptureAgentFinalResult
→ on graceful terminal path CaptureAgent zeroizes/disposes before return
```

This section is explanatory. Section 10.0 alone owns phase membership, body and
admission disposition, durable residue, retry and result-union mapping.
Any failure before availability must not return a usable locator. Crash residue
is reconciled by section 5.6. Abrupt CaptureAgent loss does not claim active
zeroization and requires recapture unless ciphertext is already complete. No
step above creates an external upload session, part receipt, resume or public
completion operation.

### 9.2 Fresh C1 attempt

```text
read actor-scoped B4 job
→ acquire B4 attempt/lease/fence
→ start bounded B4 lease-renewal supervision carrying the latest revision
→ revalidate B1/B2/B3/B4 authority
→ verify selected mode and supported exact class set
→ resolve authoritative accepted-session CaptureArtifactId + CaptureRevision
  per ordered class and atomically freeze immutable C1 mappings
→ resolve only those exact frozen Available descriptors
→ verify provider/key readiness
→ for each ordered class:
     open ciphertext
     verify ciphertext length/digest
     bounded decrypt/authenticate
     recompute/verify plaintext length + keyed content commitment and bindings
     stream into canonical assembler
→ finalize canonical manifest and assembly digest
→ authenticate assembly seal
→ fresh authority/deadline check
→ derive deterministic C2PreparationId and register Preparing disposition
→ C2 PrepareAsync with that exact id outside the database transaction
→ exact-id verify and CAS Preparing → Pending
→ stop and join periodic renewal; snapshot monotonic latest state
→ perform at most one synchronous final renewal if needed
→ SealAssemblyAsync:
     actor/ownership check
     exact existing-identity replay branch, if present
     otherwise revision/fence/lease admission
     exact ordered-item equality with immutable B4 job classes
     fresh authority/deadline revalidation
     CAS exact preparation to SealCommitted
     immutable assembly identity/items
     CAS publish AssemblySealed + append transition in the same transaction
→ finalize, remain pending on unknown, or CAS AbortAuthorized then abort
→ execute graceful plaintext/partial-buffer disposal
```

No database transaction or B4 lock may span provider, key-provider, or assembly
I/O.

### 9.3 Retry/reclaim

```text
acquire a new AttemptId and higher FencingToken
→ repeat fresh authority and source checks
→ reuse the exact immutable per-job source mapping; never select a recapture
→ rebuild deterministically from those immutable encrypted sources
→ stale worker may finish local computation or prepare quarantined C2 output
  but cannot seal or finalize
→ exact same AssemblyId, AssemblyFingerprint, AssemblyDigest, and ordered
  content must result
→ new AttemptId/fence/time produce a new ManifestDigest,
  AssemblyAuthenticationValue, C2 idempotency fingerprint, and distinct
  C2PreparationId
```

Retry does not extend source, permit, job, key, or retention deadlines.

### 9.4 Lease-renewal supervision

C1 must use landed B4 `RenewLeaseAsync` whenever provider, key, assembly, or C2
work can outlive the current lease. The build brief must freeze a renewal
interval and final-seal margin that are strictly below the configured lease
duration and are bounded by `JobExpiresAt`; renewal never extends the job.

The supervisor carries the exact actor, job, `AttemptId`, `LeaseOwnerId`,
current `FencingToken`, and latest `Revision`. Every successful renewal must:

1. keep the same attempt, owner, and fence;
2. replace the in-memory revision and expiry atomically only when the returned
   revision/fence is not stale or lower; and
3. make that latest revision the only revision eligible for the next renewal,
   failure record, or seal.

Renewal is single-flight and runs in its own short B4 transaction. It never
holds a database lock over provider, key, assembly, or C2 I/O. Before seal, the
worker stops and joins the supervisor, snapshots the latest revision, and only
then may issue one synchronous final renewal. A delayed response cannot
overwrite newer state; no periodic/final renewals overlap.

`ConcurrencyConflict`, `FenceStale`, `LeaseNotHeld`, expiry, cancellation, or
an ambiguous renewal result cancels the linked external-I/O token and forbids
seal. The worker disposes plaintext, aborts any known unsealed C2 preparation,
only after its disposition becomes `AbortAuthorized`, and leaves unknown or
still-sealable preparation `Pending`. Calls
that ignore cancellation still cannot publish because B4 admission rejects the
stale revision/fence/lease.

Once the head is `AssemblySealed`, acquire/reclaim/renewal cannot mint an
attempt, lease, or transition. Exact seal replay uses the committed identity
path and never re-enters the lease state machine.

The proof matrix must include work lasting longer than one initial lease:
renewal makes it seal successfully with the latest revision; removing renewal
must fail safely without a seal, and using the original acquire revision after
a successful renewal must turn the named test red.

## 10. Outcome and precedence matrix

Higher rows win over lower rows within the same command. Custody ingress and C1
assembly are separate commands; their matrices must not be merged.

### 10.0 Normative one-call phase × outcome contract

This table is the single source of truth for transport behavior. Sections
D4.3, 7.1, 9.1, 10.1 and 10.1.1 and the synchronized HLD/LLD may define shapes
or explain mechanisms, but may not introduce a different phase, body,
admission, residue, retry or union rule.

```text
P0  transport arrival, before authentication
P1  authenticated metadata validation: identity, binding, ownership, class
    capability, class ceiling and producer lifetime
P2  custody capacity admission
P3  claim: begin, broker comparison, broker-only complete, through committed R1
P4  committed R1/re-entry CAS exists; AdmissionAccepted has not been
    successfully written and flushed; body reader and provider writer are not
    armed
P5  AdmissionAccepted was successfully written and flushed, or the body reader
    is armed; body transmission and R2 may follow
P6  R3 through R6
P7  final result emission
```

The server is the P4/P5 observer. After successful signal emission it classifies
P5 conservatively unless it can prove that no body reader was created, the old
connection is closed, and no byte can reach the provider writer. No client
acknowledgement is assumed. “Forbidden” means the server does not request or
accept an application body and
CaptureAgent must not transmit one. Every successful final result is emitted at
P7; earliest/latest below identify where its outcome becomes fixed. A transport
loss at P7 cannot change a durable outcome: exact replay returns
`AlreadyAvailable` for an Available source or the same terminal disposition.
`RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` is neither terminal nor
replay-stable. Every invocation uses one external operation shape; a retry
invokes that same shape again with the same UUID. No row authorizes an external
begin/upload-part/complete sub-protocol.

| Outcome code | Earliest/latest terminating phase | Body required/requested/forbidden | `AdmissionAccepted` | Exact durable residue at that phase | Retry rule | Carrying union |
| --- | --- | --- | --- | --- | --- | --- |
| `ACCESS_DENIED` | P0–P0 | Forbidden; reject without body | No | Zero new alias/shell/source/attempt/key/object/capacity; discard bounded memory-only network residue | New authenticated operation only | `CaptureAgentFinalResult.OutcomeOnly` when an authenticated response channel exists; otherwise transport closes without an application result |
| `RAW_EXPORT_SOURCE_BINDING_INVALID` | P1–P1 | Forbidden | No | Zero new alias/shell/source/attempt/key/object; capacity not admitted | Correct identity/binding and start one new operation; same invalid request is not retried automatically | `CaptureAgentFinalResult.OutcomeOnly` |
| `NOT_FOUND_OR_NOT_ALLOWED` | P1–P1 | Forbidden | No | Zero new alias/shell/source/attempt/key/object; no existence disclosure | Only after caller obtains valid ownership/authority; no automatic retry | `CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID` | P0–P4 | Forbidden; any received body is rejected, never consumed as artifact content | No. P4 is before successful write/flush and before reader/writer arming | P0–P2: zero alias/shell/R1/source/attempt/key/object. P3 before R1: zero committed R1/source/attempt/key/object; a committed Evaluating alias and non-source shell, if any, retain their safe-horizon disposition. P4: keep Bound alias/canonical idempotency identity; CAS exact reservation/attempt to `AdmissionProtocolRejected` + `TerminatedBeforeStart`; abort provisional identity, destroy/revoke attempt key and release capacity; zero disk spill in all phases | P0–P3: same UUID only after transport correction while unchanged buffer/lifetime remain valid. P4: same authenticated owner may restart same UUID from metadata by CAS to a new monotonic reservation revision/attempt/fence; changed owner/identity/stale CAS is refused | P0 detection is latched until authentication precedence resolves; final code is `CaptureAgentFinalResult.OutcomeOnly`; never `InternalClaimResult` egress |
| `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE` | P1–P3 | Forbidden | No | P1: zero alias/shell/source/attempt/key/object. P3 active-key failure: Evaluating alias + non-source shell + unconsumed token; zero R1/source/attempt/key/object | Restore capability; P3 may reuse the exact unexpired current-lineage token, otherwise restart at `begin` | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` | P1 or P5 | P1 forbidden; P5 body was required and is stopped at first excess byte | P1 no; P5 yes | P1: zero alias/shell/source/attempt/key/object/capacity. P5: terminal exact attempt, fenced object/key cleanup, capacity release, no Available descriptor | P1 corrected declaration may reuse same UUID only for unchanged class-valid bytes; after R2 starts a new accepted capture/revision + UUID is required; actual-over-class has no correction | `CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | P1 or P3 | Forbidden | No | P1: zero alias/shell/source/attempt/key/object. P3: evaluation/token terminal for this producer buffer; preserve exact New alias+shell or Existing alternate alias+canonical identity; zero R1/source/attempt/key/object | No same-buffer retry; recapture/new accepted artifact | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` | P2–P2 | Forbidden | No | Zero alias/shell/source/attempt/key/object; release any partially acquired custody capacity atomically or at exact crash expiry | Exact `BusyRetryBackoff` only while the applicable section 15.1 path fits; otherwise graceful zeroization + `RECAPTURE_REQUIRED` | `CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY` | P3–P3 | Forbidden | No | Pre-begin: zero alias/shell. New post-begin: one Evaluating alias + non-source shell. Existing post-begin: one Evaluating alternate alias + unchanged canonical claim. All: zero new source/attempt/key/object | Same key after exact `BusyRetryBackoff` while the applicable section 15.1 path fits | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` | P3–P3 | Forbidden | No | Existing alias/current evaluation unchanged; zero new token/source/attempt/key/object | At/after exact `RetryNotBeforeUtc` only when evaluation wait + full continuation projection fits; else zeroize + `RECAPTURE_REQUIRED` | `InternalClaimOutcome → CaptureAgentFinalResult.ClaimEvaluationInProgress` |
| `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID` | P3–P3 | Forbidden | No | Existing alias/canonical state unchanged; zero new alias/source/attempt/key/object | Same token never; authenticated caller may restart at `begin` | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` | P3–P3 | Forbidden | No | Existing alias/canonical state unchanged through safe cleanup horizon; zero new source/attempt/key/object | Restart only at `begin` with original claim key | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `SOURCE_RETENTION_NOT_AUTHORIZED` | P3–P3 | Forbidden | No | Authorized sanitized rejection evidence only; New keeps Evaluating alias+shell, Existing keeps alternate alias+canonical claim; zero new R1/source/attempt/key/object | New operation only after fresh authority exists and producer lifetime remains valid | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` | P3–P3 | Forbidden | No | Original canonical source/alias unchanged; zero second source/attempt/object/key | Restore exact key and replay current unexpired lineage, else restart at `begin` | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT` | P3–P3 | Forbidden | No | Unbound Evaluating alias becomes `ConflictTombstone`; Bound alias stays bound; canonical source unchanged; zero second source/attempt/object/key | Same attempted conflicting key is terminal; use only a genuinely different authorized artifact identity | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_RESERVATION_BUSY` | P3–P3 | Forbidden | No | Existing owner/state unchanged; zero second attempt/object/key | Exact `BusyRetryBackoff`; exact same owner can continue only after current-row CAS and durable prior-R2 termination and only while the applicable section 15.1 path fits | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE` | P3–P3 | Forbidden; no body requested, accepted or transmitted | No | Original Available descriptor + Bound alias unchanged; no new R1/reservation revision/attempt/key/object and custody capacity released | Idempotent same-identity replay returns the same result | `ExistingMatch(Available) → CaptureAgentFinalResult.AlreadyAvailable` |
| `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` | P4–P5 | P4 body is forbidden and reader/writer are not armed; P5 body may be partial | P4 signal not successfully written/flushed; P5 signal written/flushed or body reader armed | P4: Bound alias/canonical identity retained; exact attempt `TerminatedBeforeStart`; provisional identity/key cleanup; capacity release. P5: owner retained; exact reader/writer quiescence, provider abort/inspection, termination CAS, object/key cleanup and capacity release | Not replay-stable or terminal. Next same-UUID invocation: termination not durable → `RAW_EXPORT_SOURCE_RESERVATION_BUSY`; durable termination and sufficient continuation budget → same-owner re-entry; insufficient budget or lost buffer → `RECAPTURE_REQUIRED` | `CaptureAgentFinalResult.OutcomeOnly`; transport loss does not convert this retryable result into a replay-stable disposition |
| `CONTENT_COMMITMENT_MISMATCH` | P6–P6 | Required and cleanly completed | Yes | Terminal exact attempt/source evidence; deterministic object/key cleanup; no Available descriptor | No same-UUID retry; new accepted capture and UUID | `CaptureAgentFinalResult.OutcomeOnly` |
| `RECAPTURE_REQUIRED` | P4–P6 | P4 no body accepted; P5 may be incomplete; P6 body may be complete but unrecoverable | P4 no; P5/P6 yes | Terminal current reservation/attempt; exact object/key/capacity cleanup; no Available descriptor; alias anti-reuse identity retained | No same-operation retry; new accepted capture required | `CaptureAgentFinalResult.OutcomeOnly`; transport loss may defer observation to replay |
| `RAW_EXPORT_SOURCE_RESUME_PENDING` | P6–P6 | Body was required and complete ciphertext now exists; producer body is no longer requested | Yes | Same reservation/staged or complete-provisional state; exact recoverable object/key context retained; no Available descriptor yet | No R2 restart; deterministic custody reconciliation continues R3–R6 | `InternalClaimOutcome → CaptureAgentFinalResult.OutcomeOnly` |
| `RAW_EXPORT_SOURCE_AVAILABLE` | P6–P7 | Required and completed | Yes | One immutable Available descriptor + evidence + Bound alias + committed ciphertext; R6 removes provisional residue | Exact replay returns `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE`; never re-encrypt the same source | `CaptureAgentFinalResult.Available` |
| `SOURCE_ENCRYPTION_FAILED` | `STRUCTURALLY UNREACHABLE` as a final P0–P7 code; it is an internal P5 cause class | `STRUCTURALLY UNREACHABLE` as an external result | `STRUCTURALLY UNREACHABLE` as an external result | Internal P5 failure follows exact fenced cleanup and is mapped by recoverability to temporary-unavailable or recapture-required | Follow the mapped public outcome only | No union; internal cause must not egress |
| `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `STRUCTURALLY UNREACHABLE` in a ready one-call operation; startup/readiness or pre-activation runtime projection blocks entry | Forbidden because ingress activation/continuation is denied | No | Zero new ingress residue; existing rows unchanged | Correct configuration/projection and re-probe readiness | No one-call union; readiness code only |
| `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` | `STRUCTURALLY UNREACHABLE` in a ready one-call operation; owning-side readiness blocks entry | Forbidden | No | Zero new ingress residue | Correct complete eight-key configuration and re-probe | No one-call union; readiness code only |
| `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID` | `STRUCTURALLY UNREACHABLE` at runtime; ingress readiness blocks activation before P0 | Forbidden | No | Zero new ingress residue | Correct qualified proxy/transport posture and re-probe | No one-call union; readiness code only |
| `RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID` | `STRUCTURALLY UNREACHABLE` in server P0–P7; CaptureAgent readiness blocks capture submission | Forbidden | No | Zero server ingress residue; local buffer follows CaptureAgent policy | Correct/ratify host posture before capture | No one-call union; CaptureAgent readiness code only |
| `RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID` | `STRUCTURALLY UNREACHABLE` in producer P0–P7; custody reconciliation readiness blocks that worker | Forbidden for reconciliation start | No new producer admission signal | Existing exact fenced custody residue remains unavailable/quarantined; no new mutation | Correct/ratify custody host posture and reconcile | No one-call union; readiness code only |
| `RAW_EXPORT_AUTHORITY_INVALID` | `STRUCTURALLY UNREACHABLE` in custody-ingress P0–P7; this code belongs to assembly/B4 checkpoints, while ingress uses `SOURCE_RETENTION_NOT_AUTHORIZED` | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | Assembly/B4 residue is owned by sections 10.2–10.3, not this transport | Follow assembly/B4 contract | No one-call union |
| `RAW_EXPORT_SOURCE_SELECTION_NONE` | `STRUCTURALLY UNREACHABLE`; assembly command only after source availability | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | No source binding/provider read under section 10.2 | Follow assembly selection contract | No one-call union |
| `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS` | `STRUCTURALLY UNREACHABLE`; assembly command only | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | No source binding/provider read under section 10.2 | Follow assembly selection contract | No one-call union |
| `RAW_EXPORT_SOURCE_SELECTION_CONFLICT` | `STRUCTURALLY UNREACHABLE`; assembly binding CAS only | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | Existing frozen source binding unchanged | Follow assembly binding contract | No one-call union |
| `RAW_EXPORT_SOURCE_UNAVAILABLE` | `STRUCTURALLY UNREACHABLE`; later resolver/assembly command only because ingress exposes only Available descriptors | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | Frozen mapping unchanged; no assembly/provider read | Follow retained-mode resolver contract | No one-call union |
| `RAW_EXPORT_SOURCE_INTEGRITY_INVALID` | `STRUCTURALLY UNREACHABLE`; assembly committed-source verification only | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | Quarantine/corruption evidence under section 10.2 | Follow assembly integrity contract | No one-call union |
| `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED` | `STRUCTURALLY UNREACHABLE`; C2 preparation command only | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | No assembly identity; exact preparation abort disposition | Follow C2 preparation contract | No one-call union |
| `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` | `STRUCTURALLY UNREACHABLE`; seal command only | `STRUCTURALLY UNREACHABLE` | `STRUCTURALLY UNREACHABLE` | No assembly identity/seal transition; exact candidate abort disposition | Correct immutable class-set mismatch only through separately authorized job/capture flow | No one-call union |

Typed assembly/B4 variants such as `ExistingMatch`, `ASSEMBLY_SEALED`,
`AssemblyConflict`, `AssemblyGraphInvalid`, `FenceStale`, `LeaseNotHeld`,
`SealOutcomeUnknown`, `ASSEMBLY_SEALED_FINALIZATION_PENDING`, `NoOpFinal`,
`ConcurrencyConflict`, `LeaseHeld`, `Expired` and `Reclaimed` are command result
variants, not custody-ingress outcome codes. They are structurally outside
P0–P7 and remain owned solely by sections 10.2–10.3.

The branch membership is therefore exhaustive:

```text
Metadata-only Branch A:
  every P0–P3 terminating ingress row above, including AlreadyAvailable

Body-required Branch B:
  committed NewReservation or eligible same-owner/reclaim progression
  → P4
  → AdmissionAccepted
  → P5–P6 outcome
  → P7 final result
```

`C1_already_available_returns_without_admission_body_or_new_R1` covers the
idempotent Available replay. `C1_metadata_terminal_outcomes_never_request_or_accept_body`
covers a live evaluation, fingerprint conflict, idempotency busy, reservation
busy and retention-authority failure. Every cell asserts no
`AdmissionAccepted`, no body requested/accepted/transmitted, and exact phase
residue. `C1_phase_outcome_table_is_exhaustive_and_non_overlapping`
enumerates every stable code in this table, rejects a blank/unknown/duplicate
phase cell and proves every section 10.2/10.3-only code is structurally
unreachable through `CaptureAgentFinalResult`.

### 10.1 Custody-ingress command

Priority determines the winning code only; section 10.0 determines its phase,
body/admission disposition, residue, retry and union.

| Priority | Condition | Outcome | Durable result | Residue rule |
| --- | --- | --- | --- | --- |
| 1 | Missing/invalid authenticated credential | `ACCESS_DENIED` | None | No reservation/object |
| 2 | Credential-derived producer/client/instance differs from request | `RAW_EXPORT_SOURCE_BINDING_INVALID` | None | No reservation/object |
| 3 | Body transmission or proxy-prebuffered body is detected before the section 10.0 P5 boundary | `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID` | P0–P3: no committed R1. P4: exact committed R1 identity plus `AdmissionProtocolRejected` / `TerminatedBeforeStart` | Section 10.0 phase-specific residue: P0–P3 has zero committed R1/source/attempt/key/object; P4 preserves Bound alias/idempotency identity, terminalizes exact attempt, cleans provisional object/key and releases capacity; zero disk spill in both |
| 4 | Session/challenge/subject/client ownership mismatch | non-enumerating `NOT_FOUND_OR_NOT_ALLOWED` | None | No reservation/object |
| 5 | Mode/class/provider/key/host capability unsupported, including every class outside ratified D2 | `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE` | None | Before source read/begin; zero alias/shell/source/attempt/object/key |
| 6 | Declared length is non-positive or exceeds the exact class ceiling | `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` | None | Before begin/provider/key I/O; zero alias/shell/source/attempt/object/key |
| 7 | Producer lifetime interval/skew/budget invalid before `begin` commits | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | None | Zero alias/shell/source/attempt/object/key; no provider write |
| 8 | Custody producer/deployment stream slot or custody aggregate-window reservation is unavailable | `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` | None | Before begin/provider/key I/O; zero alias/shell/source/attempt/object/key |
| 9 | Idempotency claim lock exceeds bounded timeout | `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY` | Pre-begin: zero alias/shell. New post-begin: one Evaluating alias + one non-source canonical shell. Existing-alternate post-begin: one Evaluating alias + unchanged canonical claim | Zero new source/attempt/object/key; preserve the applicable alias/canonical residue |
| 10 | Lookup succeeds and observes a live, unexpired, non-terminal current evaluation | `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` + exact `RetryNotBeforeUtc` | Existing alias/current slot unchanged | No invalidation, new token/source/attempt/object/key; retry only at/after current expiry while retention remains sufficient |
| 11 | Token tampered/wrong schema, variant, audience or binding; consumed New token replay | `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID` | Alias/canonical row unchanged | No disclosure/new residue; same token not retryable |
| 12 | Token expired, stale revision/fence, or prior owner reclaimed | `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` | Alias/canonical row unchanged | No disclosure/new residue; restart at `begin` |
| 13 | Fresh pre-capture retention authority absent/expired inside broker-invoked `complete` | `SOURCE_RETENTION_NOT_AUTHORIZED` | Sanitized rejection evidence only when authorized | New leaves Evaluating alias + shell; Existing leaves Evaluating alternate alias + unchanged canonical claim; no new source/object |
| 14 | Valid New token produces `ActiveCommitmentKeyUnavailable` after fresh-authority check | `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE` | Evaluating alias + non-source canonical shell + unconsumed token | Preserve alias/shell through safe horizon; zero source/attempt/object/key |
| 15 | Valid Existing token plus typed unavailable historic commitment key after fresh authority check | `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` | Original canonical row/alias unchanged; readiness false | No second source/object |
| 16 | Same alias key, different identity/admission fingerprint | `FingerprintConflict` / `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT` | Unbound Evaluating alias becomes `ConflictTombstone`; already Bound alias stays bound to its original canonical claim; canonical reservation unchanged | No B shell/source or source disclosure |
| 17 | Matching reservation has a different healthy owner, same-owner identity/CAS fails, or `PreviousFencedR2Terminated` is false | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` | Original ownership unchanged | No second attempt/object; same owner may re-enter only after exact termination proof |
| 18 | Valid token reaches `complete` without strict effective producer/server-capped remaining `EncryptionAttemptDeadline + SafetyMargin` | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | Evaluation/token terminal for that producer buffer without successful-claim consumption; New preserves alias + non-source shell identity, Existing preserves alternate alias + canonical identity | No R1/provider write; cleanup only after safe token horizon; recapture required |
| 19 | Actual byte counter would exceed declared or exact class limit during R2 | `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` | Terminal pre-publication evidence | Stop accepting bytes; exact fenced object/key cleanup |
| 20 | Body completes normally but actual length, digest or commitment differs from authenticated claim | `CONTENT_COMMITMENT_MISMATCH` | Terminal pre-publication evidence | Exact object/key cleanup; same UUID refused |
| 21 | Plaintext unavailable before complete recoverable ciphertext | `RECAPTURE_REQUIRED` | Terminal current custody operation | Exact object/key cleanup |
| 22 | Section 10.0 P4/P5 transport abort/cancellation/incomplete body, or provider/key transient failure | `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` | Exact owner marker plus fenced termination state | Non-terminal/non-replay-stable: next same-UUID invocation is busy until termination is durable, then same-owner re-entry if section 15.1 budget and buffer predicates pass, else recapture |
| 23 | Complete ciphertext awaits producer-buffer-independent R3–R6 recovery | `RAW_EXPORT_SOURCE_RESUME_PENDING` | Same reservation/staged state | Deterministic bounded-decrypt reconciliation with custody plaintext hygiene |
| 24 | Exact replay of same `Available` identity | `RAW_EXPORT_SOURCE_ALREADY_AVAILABLE` | Original descriptor + Bound alias unchanged | No new object |
| 25 | Success | `RAW_EXPORT_SOURCE_AVAILABLE` | One immutable descriptor + availability evidence + Bound alias | One committed ciphertext object |

`RAW_EXPORT_SOURCE_RESUME_PENDING` never means “restart encryption”; it means
complete ciphertext is recoverable without plaintext.

#### 10.1.1 Complete new-outcome contracts

These rows add typed mapping, exposure and proof details. They inherit phase,
body/admission, residue and retry semantics from section 10.0 and cannot
override that table.

| Outcome | Exact trigger/precedence | Typed mapping and SQL surface | CaptureAgent result fields | No-job disposition | B4-frozen case | Retry | Residue | Append evidence and exposure | Required proof |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID` | Authenticated ingress detects early body transmission or intermediary prebuffer before the section 10.0 P5 boundary; P0 detection remains subordinate to credential precedence | `RawExportSourceIngressResult.TransportProtocolInvalid`; transport/application boundary outcome | `OutcomeCode` only | P0–P3 reject without committed R1; P4 CASes the exact attempt to `AdmissionProtocolRejected` / `TerminatedBeforeStart` while preserving Bound alias/idempotency identity | `STRUCTURALLY UNREACHABLE`: no Available descriptor exists | No same-invocation continuation. Same-UUID retry invokes the same operation shape and follows the exact phase-specific section 10.0 rule | P0–P3: zero committed R1/source/attempt/key/object. P4: exact attempt/object/key cleanup + capacity release; alias/canonical anti-reuse evidence retained. All: zero disk spill | Stable code only; no buffered-byte count, proxy, revision/fence or transport detail | Real reverse-proxy/equivalent P0–P3 and P4 cases; admission signal follows committed R1; P4 terminalization/anti-reuse proof; disk-spill mutation RED |
| `CONTENT_COMMITMENT_MISMATCH` | Body completes normally; at clean EOF R3 actual digest/length/commitment differs; wins over transient-provider mapping | `RawExportSourceIngressResult.ContentCommitmentMismatch`; SQL N/A, application verification branch | `OutcomeCode` only | Terminalize exact attempt/source; deterministic object/key cleanup | `STRUCTURALLY UNREACHABLE`: resolver freezes only `Available`; mismatch cannot reach `Staged/Available` | No same-operation or same-UUID retry; new accepted capture and UUID | No available descriptor/object/key | Append internal `ContentCommitmentMismatch` custody event with source/attempt ids and stable code only; CaptureAgent receives code only; no digest/commitment | Clean-shorter-body and digest/length/commitment mutations; retry refusal |
| `RECAPTURE_REQUIRED` | Plaintext unavailable and no complete authenticated recoverable ciphertext, after authority/integrity classification | `RawExportSourceIngressResult.RecaptureRequired`; SQL N/A, fenced custody branch | `OutcomeCode` only | Terminalize current operation; exact object/key cleanup; new accepted capture required | `STRUCTURALLY UNREACHABLE`: resolver freezes only `Available`; this is pre-publication | No same-operation retry | No available descriptor; exact attempt cleanup | Append internal `RecaptureRequired` event with cause class only; CaptureAgent receives code only; no locator/content | Abrupt-loss/source audit; no behavioral mutation for impossible B4 case |
| `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY` | `begin`/broker-invoked `complete` lookup lock reaches bounded timeout before ownership decision | `RawExportSourceIngressResult.IdempotencyBusy`; direct DB SQLSTATE `55P03`, exact MessageText `RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY` | `OutcomeCode` only | No source/reservation decision or disclosure; pre-begin leaves zero alias/shell; New post-begin preserves one Evaluating alias + non-source shell; Existing-alternate post-begin preserves one Evaluating alias + unchanged canonical claim | `STRUCTURALLY UNREACHABLE`: no source identity has been returned/frozen | Same key after exact `BusyRetryBackoff`, only while the applicable section 15.1 path fits | Zero new source/reservation/attempt/object/key; preserve the applicable alias/canonical residue through its safe cleanup horizon | No append evidence row; bounded counter tagged only by stable code; no audit flood or identifiers | Two-connection pre-begin/New-post-begin/Existing-post-begin timeout, cleanup-horizon convergence, bounded-backoff and Available-only architecture proof |
| `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` | `begin` acquired the lookup edge and found an unexpired non-terminal current evaluation; distinct from lock timeout and before any replacement | `RawExportSourceIngressResult.ClaimEvaluationInProgress`; exact code plus server-authored UTC-microsecond retry boundary | `OutcomeCode` + required `RetryNotBeforeUtc = CurrentTokenExpiresAtUtc` only | Preserve exact alias/current slot; never invalidate another caller's live token | `STRUCTURALLY UNREACHABLE`: no source descriptor is disclosed | Same key at/after `RetryNotBeforeUtc` only if the exact applicable section 15.1 `RP-*` path covers evaluation wait plus its selected pending/ready/reclaim case; otherwise zeroize and `RECAPTURE_REQUIRED` | Zero new token/source/attempt/object/key; preserve current issuance through expiry and safe cleanup horizon | No append row or token/owner/revision/fence exposure; one bounded stable-code metric | Lost internal begin result, concurrent repeat, exact retry timestamp/shape, no-hot-loop, combined wait+expired-reclaim and retention-insufficient mutations |
| `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` | Pre-begin declared length is invalid/over its class ceiling, or the R2 checked actual counter would cross declared/class limit; outranks commitment mismatch on overflow | `RawExportSourceIngressResult.ArtifactSizeLimitExceeded`; application boundary outcome | `OutcomeCode` only | Pre-begin: zero residue. R2: terminalize exact attempt and clean fenced provisional object/key | `STRUCTURALLY UNREACHABLE`: no `Available` descriptor is produced | Pre-begin zero-residue rejection may retry the same UUID with a corrected declaration for the same unchanged buffer only when actual bytes fit the class ceiling and authority/lifetime remain valid. After R2 starts, declared length is frozen: actual-over-declared but class-valid bytes require a new accepted capture/revision and new UUID; actual-over-class bytes have no declaration-correction path | Pre-begin zero alias/shell/source/attempt/object/key; R2 exact attempt cleanup and capacity release | Stable code/class only; no declared/actual length, digest, provider or locator exposure | Per-class equality ±1, pre-begin correction, understated class-valid stream, actual class overflow, Int64 overflow, no-provider-I/O pre-begin and exact R2 cleanup |
| `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` | Valid declared size/lifetime but atomic custody producer/deployment stream slot or aggregate-window reservation cannot be acquired before `begin`; CaptureAgent local exhaustion never calls the server | `RawExportSourceIngressResult.CapacityUnavailable`; application admission outcome | `OutcomeCode` only | Zero alias/shell/source/attempt/object/key | `STRUCTURALLY UNREACHABLE`: no descriptor exists | Exact `BusyRetryBackoff` only while the applicable section 15.1 `RP-*` path fits; otherwise zeroize and `RECAPTURE_REQUIRED` | Release no unowned capacity; every owned server slot/reservation releases on all exits or by CAS at exact `CapacityAdmissionExpiresAtUtc` | Bounded stable-code metric without sizes/counts/producer ids | Custody producer/deployment slot and aggregate-window fixtures in which each target binds while adjacent gates remain available; release/cancellation/exact crash-expiry and limit-removal mutations |
| `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` | Section 10.0 P4 signal write/flush failure before reader/writer arming, or P5 transport abort/cancellation/incomplete body or provider/key transient failure; clean EOF mismatch has higher content-mismatch classification | `RawExportSourceIngressResult.SourceTemporarilyUnavailable`; application transport/custody outcome | `OutcomeCode` only | P4 records exact `TerminatedBeforeStart`; P5 quiesces exact body reader/provider writer, aborts/inspects exact fenced R2 object/key and durably records `PreviousFencedR2Terminated`; both keep owner marker | `STRUCTURALLY UNREACHABLE`: no `Available` descriptor is produced by an incomplete attempt | Non-terminal/non-replay-stable. Next same-UUID invocation: not-durable termination is busy; durable termination plus section 15.1 budget and buffer predicates permits same-owner re-entry; insufficient budget or lost buffer is `RECAPTURE_REQUIRED` | No available descriptor/object/key; exact phase-specific cleanup, capacity release and monotonic termination evidence | Stable code/cause class only; no byte count, digest, locator, owner/fence or transport detail | Disconnect in P4, before first P5 byte and mid-R2; same-owner immediate retry, different-producer busy, not-yet-terminated busy, budget/buffer recapture, clean-shorter mismatch and same-UUID refusal |
| `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE` | Either unsupported mode/class/provider/key/host before `begin`, or valid New token with `ActiveCommitmentKeyUnavailable` after fresh authority | `RawExportSourceIngressResult.CapabilityUnavailable`; exact SQL surface remains a Build-Brief decision | `OutcomeCode` only | Pre-begin: zero alias/shell. Post-begin active-key failure: preserve Evaluating alias + non-source shell and do not consume token | `STRUCTURALLY UNREACHABLE`: no descriptor is returned by this path | Post-begin same token only while unexpired and full alias/evaluation/owner/revision/fence lineage is unchanged; otherwise `begin` | Zero source/attempt/object/key; post-begin alias/shell survives to safe cleanup horizon | No key/version/source exposure; bounded stable-code metric | Distinct pre/post-begin residue, restored-key retry, stale-fence restart and phase-collapse mutations |
| `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | Invalid producer interval/skew/budget before `begin`, or insufficient strict effective producer/server-capped remaining lifetime inside `complete` after token/authority | `RawExportSourceIngressResult.PlaintextRetentionInvalid`; exact SQL surface remains a Build-Brief decision | `OutcomeCode` only | Pre-begin: zero alias/shell. Post-begin: evaluation/token terminal for this producer buffer without successful-claim consumption; preserve New alias+shell identity or Existing alias+canonical identity | `STRUCTURALLY UNREACHABLE`: no `Available` descriptor is produced | No same-buffer retry; recapture/new accepted artifact required | Zero R1/source/attempt/object/key; post-begin identity residue survives only to safe cleanup horizon | No raw configured values or producer times exposed; stable code only | Effective min-side, server-cap equality/±1, slow-token window, phase residue and no-provider-I/O proofs |
| `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID` | Valid outer credential but token digest/schema/variant/audience/binding fails, consumed New token is replayed, or token/result variants mismatch; after lock-busy, before authority/dependency | `RawExportSourceIngressResult.ClaimTokenInvalid`; direct DB SQLSTATE `P0001`, exact MessageText `RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID` | `OutcomeCode` only | Existing alias/canonical state unchanged; no disclosure | `STRUCTURALLY UNREACHABLE`: no descriptor is returned by this path | Same token never; authenticated caller may restart at `begin` | Zero new alias/canonical/source/attempt/object/key; preserve pre-existing Evaluating, Bound or tombstone state | No append row; bounded stable-code security metric without token/ids | Tamper/wrong schema/variant/audience/binding/consumed-token and cross-token/cross-alias/result-variant mutations with exact SQL oracle |
| `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` | Valid token shape/digest but expiry elapsed or alias/evaluation owner/revision/fence changed; after lock-busy, before authority/dependency | `RawExportSourceIngressResult.ClaimRestartRequired`; direct DB SQLSTATE `P0001`, exact MessageText `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` | `OutcomeCode` only | Existing alias/canonical state unchanged; no disclosure | `STRUCTURALLY UNREACHABLE`: no descriptor is returned by this path | Retry only from `begin` with original claim key | Zero new alias/canonical/source/attempt/object/key; preserve pre-existing state until safe cleanup horizon | No append row; bounded stable-code metric only | Exact UTC-microsecond expiry, max horizon, cleanup-one-microsecond-early and stale-token-versus-reclaim races |
| `RAW_EXPORT_SOURCE_RESERVATION_BUSY` | Exact matching non-terminal reservation has a different healthy owner, same-owner identity/current revision/fence CAS fails, `SameOwnerRetryPendingTermination` or `ExpiredOwnerReclaimPendingTermination` holds, or reclaim predicate is false | `RawExportSourceIngressResult.ReservationBusy`; DB admission SQLSTATE `55P03`, exact MessageText `RAW_EXPORT_SOURCE_RESERVATION_BUSY` | `OutcomeCode` only | Preserve exact owner/state | `STRUCTURALLY UNREACHABLE`: matching source is not yet `Available` | Same key after exact `BusyRetryBackoff` while the applicable pending/ready section 15.1 path fits; same owner may re-enter only after durable settlement and CAS | No second attempt/object/key; owner marker retained | No append row per poll; bounded stable-code metric only; no source/lease/revision/fence/locator fields | Same-owner ready positive, different-producer busy, both pending-termination cases, stale-CAS, bounded-backoff and reclaim-boundary races |
| `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` | Valid Existing token produces typed broker failure, even when stored version equals active version; broker-invoked `complete` has passed fresh authority and then maps unavailable selected key before fingerprint compare | `RawExportSourceIngressResult.HistoricCommitmentKeyUnavailable`; direct DB SQLSTATE `P0001`, exact MessageText `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE` | `OutcomeCode` only | Preserve original row/alias; create no second source/attempt | Reachable on ingress replay after `Available`; no B4 mutation; independent later resolver/readiness failure maps use to `RAW_EXPORT_SOURCE_UNAVAILABLE` | Restore the exact key and replay the same token only while unexpired and alias/evaluation/owner/revision/fence remain unchanged; otherwise `begin` | Original source/alias unchanged; zero new object/key | Append internal sanitized historic-key-unavailable event; external code omits key id/version/source/state | Historic==active positive control, rotation/DR, same-token recovery, reclaimed-fence restart, expiry and simultaneous authority-withdrawal/key-unavailable precedence mutations |

The structural proof is the repository architecture rule:

```text
Resolver result state == Available
```

No repository method returns `Reserved`, `Encrypting`, `Staged`, quarantined, or
terminal descriptors to source-binding freeze. Structural/source-audit tests
prove this rule; no behavioral mutation is claimed for unreachable B4 cases.
Later loss of a previously `Available` frozen source is exclusively
`RAW_EXPORT_SOURCE_UNAVAILABLE`.

### 10.2 Assembly/prepare/seal command

| Priority | Condition | Outcome | Durable result | Residue rule |
| --- | --- | --- | --- | --- |
| 1 | Invalid/missing actor | `ACCESS_DENIED` | None | No provider read/preparation |
| 2 | Actor/client/job ownership mismatch | non-enumerating `NOT_FOUND_OR_NOT_ALLOWED` | None | No provider read/preparation |
| 3 | B1/B2/B3/B4 authority invalid at pre-read checkpoint | `RAW_EXPORT_AUTHORITY_INVALID` | Exact B4 terminal/cancel outcome | Provider read count zero |
| 4 | Mode/class/provider/key capability unsupported | `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE` | No source read | No plaintext |
| 5 | Attempt/revision/fence/lease stale before work | exact B4 concurrency/lease outcome | No mutation | Local buffers disposed |
| 6 | Zero eligible source candidates for any class before binding freeze | `RAW_EXPORT_SOURCE_SELECTION_NONE` | No source binding | No provider read |
| 7 | More than one eligible candidate for any class before binding freeze | `RAW_EXPORT_SOURCE_SELECTION_AMBIGUOUS` | No source binding | No provider read |
| 8 | Frozen source missing/expired/deleted/revoked/unreadable | `RAW_EXPORT_SOURCE_UNAVAILABLE` | Mapping unchanged; exact retry/terminal state per ratified retained-mode rules | No assembly |
| 9 | Frozen source binding mismatch or substitution | `RAW_EXPORT_SOURCE_BINDING_INVALID` | Corruption/security evidence | No assembly |
| 10 | Ciphertext/AEAD/plaintext integrity mismatch | `RAW_EXPORT_SOURCE_INTEGRITY_INVALID` | Quarantine/corruption evidence | No assembly |
| 11 | Provider/key/assembly transient failure | closed retryable outcome | B4 retryable only after retained-mode rules are ratified | Partial buffers/preparation removed |
| 12 | Authority/deadline invalid immediately before Prepare | `RAW_EXPORT_AUTHORITY_INVALID` | No new preparation/seal | No C2 custody |
| 13 | C2 prepare failure/conflict | `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED` | No assembly identity | Register/observe exact abort disposition |
| 14 | Exact committed assembly exists and persisted replay-equality set matches | `ExistingMatch` / `ASSEMBLY_SEALED` | Existing identity/head/transition unchanged | Exact referenced preparation finalized |
| 15 | Committed identity exists but content differs or graph is invalid | `AssemblyConflict` / `AssemblyGraphInvalid` | Existing commit unchanged | New preparation CAS `AbortAuthorized` |
| 16 | New-seal CAS stale/lease not held | exact `FenceStale`/`LeaseNotHeld`/conflict | No assembly identity/seal transition | Abort only after disposition CAS |
| 17 | Ordered assembly items differ from immutable B4 job classes | `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` | No assembly identity/seal transition | Abort only after disposition CAS |
| 18 | Authority/deadline invalid inside Seal | `RAW_EXPORT_AUTHORITY_INVALID` | No assembly identity/seal transition | Abort only after disposition CAS |
| 19 | Seal transport/commit result is indeterminate | `SealOutcomeUnknown` | Unknown until exact replay/reconciliation | Remain `Pending`; never infer abort |
| 20 | Seal committed; finalize pending/fails | `ASSEMBLY_SEALED_FINALIZATION_PENDING` | Exact assembly identity + B4 seal + `SealCommitted` | Recovery finalizes exact preparation |
| 21 | Fresh seal success | `ASSEMBLY_SEALED` | One immutable assembly identity/items + B4 seal | Exact preparation finalizes |

### 10.3 B4 compatibility amendment

| Priority | Condition | Outcome | Durable result |
| --- | --- | --- | --- |
| 1 | Exact committed assembly replay through `SealAssemblyAsync` | `ExistingMatch` | Existing seal unchanged; exact preparation finalizes |
| 2 | Different replay content or invalid committed graph | `AssemblyConflict` / `AssemblyGraphInvalid` | Existing seal unchanged |
| 3 | Lease acquire/reclaim/renew called when head is `AssemblySealed` | `ConcurrencyConflict` | No attempt, lease, revision, or transition |
| 4 | Unsealed retained-mode job; lease live | `LeaseHeld` | No mutation |
| 5 | Unsealed retained-mode job; lease expired; B4 job deadline reached | `Expired` | Existing B4 expiry evidence; no new attempt |
| 6 | Unsealed retained-mode job; lease expired; retention/hold/revocation authority invalid | `RAW_EXPORT_AUTHORITY_INVALID` | No new attempt; append closed authority evidence; physical purge/hold remains gated |
| 7 | Unsealed retained-mode job; lease expired; exact frozen mapping and all fresh checks valid | `Reclaimed` | New attempt/fence/revision; mapping unchanged |

The exact public visibility and stable error-token manifest must be frozen in
the build brief. Raw provider messages, object keys, bucket names, paths, and
existence details must not escape.

## 11. Shape and nullability matrix

| Variant | Required | Nullable/absent | Forbidden | Consumer mapping |
| --- | --- | --- | --- | --- |
| Source create authoritative identity | Credential-derived producer/client/instance; session/challenge; accepted capture id/revision; class; UUID claim key; authority snapshot | None | Caller-selected actor/source/provider | Restricted custody ingress |
| Source create producer claims | Metadata-phase claimed digest/length/media/captured time and plaintext-retention start/expiry/budget; body only after admission | None | Agent capacity assertion/attestation/reservation id; claim represented as verified evidence; server claim to enforce CaptureAgent memory; server extension of deadline; upload session/part/receipt/resume/public completion | Restricted one-call custody ingress |
| Ingress claim alias | Four-field key; attempted exact-artifact/identity fingerprint; immutable content-free v2 `ProducerClaimEnvelopeFingerprint`; Evaluating/Bound/ConflictTombstone; one CAS-replaceable current evaluation id/owner/disposition/issue/expiry/revision/fence/token schema/variant/audience/digest; monotonic latest-issued expiry | Canonical claim FK absent until Bound; no source id ever | Raw token, claimed digest, content-derived envelope value, locator, provider/key secret; deletion/rebinding while prior token may live; replacement of a live current slot | `begin`; broker-only validator/`complete` |
| Claim-evaluation bearer | Opaque 32-byte token + non-secret evaluation id/revision/fence/variant/expiry restart metadata | No source id, selector, envelope, owner or canonical state | Persistence/logging of raw token; caller-selected audience/key; provider/locator/content field; CaptureAgent exposure | Returned once by internal `begin`; consumed only by isolated broker |
| Claim-comparison broker internal result | Exactly one fully normalized `DerivedAdmission` carrying envelope fingerprint, commitment tuple, length/media/captured/retention fields; or variant-selected ActiveKeyUnavailable/HistoricKeyUnavailable; or ClaimTokenInvalid | Commitment and normalized claims absent on non-derived variants | Bare digest; trusted final AdmissionFingerprint scalar; crossing the broker boundary; ingress construction; final public/SQL outcome; caller-selected selector; source id/locator/key bytes | Produced and consumed inside one broker invocation; `complete` checks envelope equality and recomputes AdmissionFingerprint |
| Transport admission signal | Transport-level `AdmissionAccepted` only after committed R1 | No application payload | Source/claim/owner/lease/revision/fence/token/retry/result field; signal before R1; second application call | Same external transport operation only |
| P4 rejected reservation generation | Bound alias + canonical source; exact current reservation revision/fence predecessor; `AdmissionProtocolRejected`; `R2TerminationDisposition = TerminatedBeforeStart`; terminal attempt/cleanup/capacity evidence | No body/content result; no Available descriptor | Alias deletion/rebinding; zero-R1 claim; retained object/key/capacity; restart by different identity; revision/fence egress | Custody ingress/reconciler only |
| Previous-R2 termination configuration | Four exact mandatory Int32-second `[1,3600]` no-default keys and checked `PreviousR2TerminationBudget` sum | None | Missing/partial/default/zero/negative/out-of-range/overflowed component; unbounded wait; silent reuse of ProviderAbortOrFinalizeBudget | Readiness + producer/reconciler continuation projection |
| Internal claim result | Exact D4.3 `NewReservation`, `ExistingMatch`, `ReservationReclaimed` or closed internal claim outcome | Retry boundary only on internal ClaimEvaluationInProgress | Serialization, CaptureAgent DTO/log/response, public egress | Custody ingress orchestrator only |
| CaptureAgent final result | Exact D4.3 `Available`, `AlreadyAvailable`, `ClaimEvaluationInProgress` or closed `OutcomeOnly` code | Source id/state/disposition only on Available variants; `RetryNotBeforeUtc` required only for ClaimEvaluationInProgress | Any internal claim variant; extra/missing variant field; provider/storage/key/ciphertext/digest/descriptor/internal fields; section 6.2 | Authenticated CaptureAgent receives exactly one final result |
| Source reserved descriptor | Claim/identity/admission fingerprints; keyed commitment; subject token; authority snapshot; recoverable crypto context; owner/revision/attempt/fence/deadlines; monotonic R2 termination disposition/time | Staged/committed fields absent | Plaintext, bare digest, unwrapped key, credentials, any storage locator outside restricted record | Custody producer/reconciler |
| Source staged descriptor | Reserved fields + `StagedCiphertextFingerprint` and actual ciphertext/envelope metadata | Committed locator absent | Section 6.2 content | Custody reconciler |
| Source available descriptor | All immutable metadata and restricted provider/key fields | Deletion fields absent | Section 6.2 content; locator never leaves repository/adapter | Actor-scoped C1 resolver |
| C1 job-source binding | `JobId`, `Ordinal`, `RawClass`, accepted `CaptureArtifactId`, `CaptureRevision`, selected `SourceArtifactId` | None | Mutable replacement, first/latest choice, inherited authority | Isolated C1 worker/repository |
| Unsupported class rejection | Exact class and closed capability code in internal evidence | Source descriptor absent | Fabricated fallback source | Ingress or C1 orchestrator sanitized outcome |
| Source deleted | Immutable descriptor + append-only delete evidence | Read capability absent | Recreated locator or extended expiry | Lifecycle/reconciliation worker |
| Assembly input item | Ordinal, exact class/source id, versioned keyed content commitment, length, media type | None | Bare digest, credential, any storage locator | Isolated C1 assembler |
| C2 prepared result | Preparation id + exact assembly/job/digest/authentication fingerprint | Package/delivery fields absent | Deliverable/public handle | `SealAssemblyAsync` command only |
| C2 preparation disposition | Exact `AssemblyId + deterministic C2PreparationId`, fingerprint, attempt/fence and monotonic `Preparing/Pending/SealCommitted/AbortAuthorized/Finalized/Aborted` | Final external evidence absent until executed | Unregistered orphan; cross-preparation finalize; scan-only abort; locator | Prepare/seal/abort arbiter only |
| Assembly sealed result | Assembly identity, job/attempt/fence, manifest digest, authentication metadata, C2 preparation id | Final package id absent | Recipient ciphertext claim, delivery claim | C2 finalizer/recovery and actor-scoped job read |
| Seal outcome unknown | Exact logical seal command identity + closed outcome | Commit state unknown until replay | Abort authority, provider details, speculative failure | C1 reconciliation only |
| Failure result | Closed code and sanitized allowlisted context | Success/assembly fields absent | Every section 6.2 prohibited field | Calling restricted component only; public mapping remains non-enumerating |

## 12. Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract surface | Outcome/error | State/evidence/residue | Named proof required | Negative/mutation proof |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| C1-I01 | No provider coupling in domain | Application port | Before adapter selection | `IRawArtifactStore` | Build/architecture failure | No provider types outside adapter | Architecture dependency test | Inject MinIO/S3 type into domain and observe RED |
| C1-I02 | Encrypt before provider write | Custody component | Authority and binding valid | Encrypting source pipeline | `SOURCE_ENCRYPTION_FAILED` | No final object/descriptor | Provider observes ciphertext unequal to plaintext | Bypass encryption and observe RED |
| C1-I03 | Exact credential/producer/session/challenge/subject/client binding | Custody ingress | Authentication first | Source create command | `RAW_EXPORT_SOURCE_BINDING_INVALID` | Zero reservation/object | Cross-owner and replay matrix | Remove one derived-identity/challenge predicate and observe RED |
| C1-I04 | Exact class-specific bytes and keyed commitment | Producer + custody | Class supported | Source descriptor | `CONTENT_COMMITMENT_MISMATCH` | No bare digest; zero success residue | Two positive keyed-commitment vectors: `ChipDg2Portrait`, `LiveSelfieImage`; one unsupported-capability negative: `LivenessMedia` | Reuse NFC aggregate input for DG2, change either positive input, or admit the unsupported class and observe RED |
| C1-I05 | Crash-safe immutable provider/metadata saga | Custody + adapter + reconciler | Reservation before I/O | Storage port + custody metadata | deterministic idempotent/conflict/reconcile | Only section 5.6 states reachable; no available partial object | Every predicate-complete crash-window test | Remove one reservation/visibility/reconcile predicate and observe RED |
| C1-I06 | No PostgreSQL raw bytes | Persistence | Descriptor creation | Schema/catalog | readiness failure | Metadata only | Catalog type/name/size gate | Add bytea/payload column and observe RED |
| C1-I07 | Authority precedes raw read | Worker | Attempt acquired | C1 orchestrator | authority code | Provider read count zero | Withdraw/expiry precedence tests | Move read earlier and observe RED |
| C1-I07B | Fresh authority/deadline checks precede both new Prepare custody and seal publication | Orchestrator + seal repository | Before `Preparing`/C2 call and inside Seal | Prepare gate + `SealAssemblyAsync` | exact authority/expiry outcome | Pre-Prepare loss creates no row/call; loss during/after Prepare may leave quarantined custody but never seals/finalizes and reaches exact abort disposition | Withdraw/expire before, during and after Prepare and inside Seal | Remove either revalidation or permit finalization after loss and observe RED |
| C1-I08 | No DB lock over provider/key I/O | Repository/orchestrator | Snapshot then release | Transaction boundary | invariant failure | Connection/transaction clean | Blocking provider probe | Hold transaction across probe and observe RED |
| C1-I09 | Stale fence cannot publish | B4/C1 CAS | Assembly locally complete | Seal publication | `JOB_FENCE_STALE` | No sealed identity from stale worker | Paused-worker reclaim race | Remove fence predicate and observe RED |
| C1-I10 | Bounded plaintext lifetime with honest abrupt-loss posture | CaptureAgent + custody producer + reconciliation worker | Operation admission through graceful terminal or abrupt loss | Producer monotonic deadline + CaptureAgent host posture + bounded reconciliation chunks/key + custody host posture | retention-invalid/recapture/capture-host-invalid/custody-reconciliation-host-invalid | Every graceful plaintext/key path zeroizes; abrupt paths make no erasure claim; reconciler never reconstructs producer buffer | CaptureAgent and reconciliation graceful-path instrumentation + abrupt-loss/dump/swap inspection | Retain a reconciliation chunk/key, skip either graceful zeroize, reuse CaptureAgent-only readiness, or equate crash with erasure and observe RED |
| C1-I11 | Deterministic ordered assembly | Assembler | Exact ordered classes | Canonical manifest | `ASSEMBLY_SEALED` | One logical assembly digest | Golden manifest/vector | Reorder classes and observe RED |
| C1-I12 | Provider substitution preserves semantics | Adapter conformance suite | Same port vectors | S3 adapter; future filesystem adapter | same closed outcomes | No domain change | Provider contract suite | Adapter-specific semantic deviation turns suite RED |
| C1-I13 | Production stays disabled without qualified provider/key/C2 | Readiness | Startup/probe | Readiness manifest | stable unavailable code | No worker activation | Missing/invalid capability matrix | Substitute fixture readiness and observe RED |
| C1-I14 | Deletion does not erase custody evidence | Lifecycle owner | Expiry/purge due | Delete + append evidence | deleted/idempotent | Object absent, history retained | Purge/retry test | Delete evidence row or recreate object and observe RED |
| C1-I15 | B4 seal publication is one atomic metadata transaction | Seal repository | C2 `Pending`, authority fresh | disposition + assembly identity + item set + B4 head + transition | sealed/existing/conflict | All five surfaces commit or none | Migration/function transaction test | Split each of the five surfaces outside the transaction independently and observe RED |
| C1-I16 | C2 preparation cannot become orphaned or deliverable without exact C1 seal | C2 sink boundary | Deterministic id + `Preparing` before Prepare; finalize after seal CAS | prepare/exact lookup/finalize/abort | preparing/pending/sealed/aborted | Crash before/during/after Prepare converges by immutable preparation id | Prepare/seal/finalize crash matrix | Allocate id only after Prepare or finalize before seal and observe RED |
| C1-I17 | Seal items equal the immutable B4 job-class set exactly | Seal repository | Before fresh authority and publication | `OrderedItems` versus `raw_export_job_classes` | `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` | No assembly identity/head/transition | Missing/extra/substituted/reordered item matrix | Remove one count/ordinal/class predicate and observe RED |
| C1-I18 | A committed seal finalizes only its exact C2 preparation | C1 disposition barrier + C2 | Registered candidate exists | `(AssemblyId,C2PreparationId)` CAS | `SealCommitted` or `AbortAuthorized`, never both | No cross-preparation finalize/abort race | Lost response, other-preparation seal and concurrent abort | Remove target-id equality or barrier CAS and observe RED |
| C1-I19 | Retry separates job-stable assembly identity/content from attempt-scoped manifest/preparation | Assembler + C2 sink | Reclaim creates new attempt/fence | Exact v2 derivation table in section 8 | stable fingerprint conflict or attempt-idempotent preparation | Same stable identity/content; distinct unsealed attempt candidates; one sealed preparation finalizes | Cross-attempt golden vector | Put attempt/fence in stable fingerprint or omit them from manifest/C2 fingerprint and observe RED |
| C1-I20 | Long-running work renews the B4 lease and propagates the latest revision | C1 renewal supervisor | Lease acquired before external I/O | landed `RenewLeaseAsync` + seal `ExpectedRevision` | exact B4 renewal/admission outcome | No seal from expired/stale/original-revision worker; buffers/preparations reconciled | Longer-than-one-lease success/failure matrix | Remove renewal or seal with pre-renew revision and observe RED |
| C1-I21 | Source identity is stable while attempt/staged identities bind their exact time-scoped inputs | Custody repository + encryption pipeline | R1 before R2; R3 after R2 | SourceReservation + EncryptionAttempt + StagedCiphertext fingerprints | conflict/integrity outcome | Immutable source plus immutable attempt/staged rows | Source/attempt/post-encryption golden and crash vectors | Put attempt fields in source fingerprint or omit exact attempt from staged fingerprint and observe RED |
| C1-I22 | Server-authored accepted selection is immutable per job but retained sources may serve independently authorized jobs | Acceptance producer + C1 source-binding repository | Exact acceptance event and session selection before first raw read | `(JobId,Ordinal,RawClass,CaptureAcceptanceId,CaptureArtifactId,CaptureRevision,SourceArtifactId)` | none/ambiguous/conflict/unavailable/binding-invalid | One mapping/job/class; no implicit recapture | Missing/conflicting/concurrent/recapture/substitution and two-job reuse matrix | Treat landed QualityState as authority, select latest, or overwrite a binding and observe RED |
| C1-I23 | `AssemblySealed` cannot re-enter the B4 lease/attempt state machine | Future reviewed B4 compatibility amendment | Seal committed | claim/reclaim/renew/transition surfaces | `ConcurrencyConflict` | No new attempt/lease/revision/transition | Sealed-state matrix | Remove sealed-state exclusion and observe `AssemblySealed → Assembling` RED |
| C1-I24 | Committed replay equality excludes fresh-admission-only values | Seal repository | Existing identity lookup precedes fresh admission | persisted replay-equality set | `ExistingMatch`/conflict/graph-invalid | No duplicate or mutation | Response-loss replay after revision/lease change | Compare `ExpectedRevision` or `LeaseOwnerId` on replay and observe RED |
| C1-I25 | Persisted canonical timestamps round-trip exactly | Canonical codec + persistence | Normalize before hash and persist | six-fraction UTC microsecond codec | deterministic equality/conflict | Same bytes before/after DB readback | Non-zero seventh-digit cross-language vector | Hash seven digits or round instead of truncate and observe RED |
| C1-I26 | Prohibited content cannot enter metadata or observability surfaces | Custody/assembly/error/observability boundary | Before emit/persist/report | Canonical section 6.2 allowlist | redaction/rejection/readiness failure | No prohibited field in captured surfaces | Full sink/log/trace/metric/audit scan | Add one logged locator/SubjectRef/credential field and observe RED |
| C1-I27 | Public/general API process owns no provider or KEK authority | Composition/readiness | Process construction/startup | process identity + registrations + credential manifest | readiness failure | No provider/key secret loaded | Process topology inventory test | Register ingress/provider/key client in public API process and observe RED |
| C1-I28 | One ingress claim/exact artifact creates one source across concurrency, restart and alternate-key reuse | Ingress claim repository | Authentication and accepted evidence first | unique durable alias key + instance-independent canonical exact-artifact edge + in-place identity/fence + token variants + atomic complete/R1 | New/Existing/Conflict/IdempotencyBusy | One complete Reserved canonical source; every accepted/burned alternate key is durably Bound/ConflictTombstone and cannot rebind | Two-connection winner/rollback/timeouts, stale reclaim, changed-instance replay and A(K1)→A(K2)→B(K2) | Omit alias binding/tombstone, split complete/R1, delete/reinsert, reuse revision/fence, remove unique edge/timeout, or permit duplication and observe RED |
| C1-I29 | Bare Raw BIO digest never persists or escapes | Commitment codec + persistence/observability | Before R1 persist/emit | keyed commitment v1 | commitment mismatch/readiness failure | Only commitment + schema/key version | Catalog/result/log/audit scan | Persist/log/index claimed digest and observe RED |
| C1-I30 | Historic commitment comparison uses the row's key selector atomically across rotation and canonical precedence | Claim-comparison broker + key registry + broker-only complete | `begin` freezes selector and producer-envelope fingerprint internally; broker validates token, recomputes envelope binding, derives the normalized provisional result and calls `complete` in one invocation; `complete` re-verifies token/alias/envelope/revision/fence, fresh authority and remaining plaintext lifetime before mapping result | stored schema/key id/version + opaque token/digest + envelope fingerprint; fully normalized provisional union remains broker-internal | token invalid/restart, authority/retention invalid, New active-key unavailable, Existing historic-key unavailable, exact match/conflict | No arbitrary selector, cross-token/result/claim mix, ingress complete, false conflict/second row, pre-arbiter final error or stale-authority disclosure | Rotate/withdraw/key-fail between begin/broker-complete, historic==active, replay, changed claim, reclaim, retry and restore key | Give ingress direct complete/key access, accept caller selector, let typed result cross boundary, trust broker fingerprint, classify by current key status, or omit fresh authority/lifetime/token/alias/envelope/revision/fence and observe RED |
| C1-I31 | Atomic complete/R1 durably owns the source, recoverable key reference and every non-secret context needed after process loss | Custody repository + key provider | NewCandidate complete, before provider/key/object I/O | token/revision + source/reservation/attempt/fence + idempotent AttemptKeyReservationId + framing/nonce/object identity | new reservation only after commit, recapture or recoverable progression | No advertised/usable source without complete R1; no essential recovery secret only in memory; no key I/O in R1 transaction | Crash before/after complete/R1 commit, key create-or-get and complete R2 | Split complete from R1, return SourceArtifactId before context commit, make key reference non-idempotent, move context to R3, or call key provider under DB lock and observe RED |
| C1-I32 | Healthy producer, returning same owner and reconciler cannot create concurrent writers | Fenced reservation head | Ownership/age/time and same-owner re-entry predicates | owner identity + lease/revision/fence CAS + `PreviousFencedR2Terminated` | reservation busy/reclaimed/recapture | One current owner and bounded monotonic attempts; owner marker retained | Same-owner terminated positive, different-producer busy, previous-writer-not-terminated busy and producer/reconciler races | Remove owner identity/current CAS/termination/minimum-age/fence predicate and observe RED |
| C1-I33 | Authority snapshot is immutable and revalidated at every custody boundary | Authority repository | Before admission/staging/publication/read/prepare/seal/purge | versioned snapshot + fresh state | authority invalid | No self-authorizing source | Withdraw at each R1–R6/C2 checkpoint | Omit snapshot field or R3/R5/pre-Prepare check and observe RED |
| C1-I34 | C2 preparation disposition is monotonic and exact | Disposition repository | Deterministic id registered `Preparing` before Prepare | Preparing/Pending/SealCommitted/AbortAuthorized head | prepare/pending/finalize/abort | Exactly one durable disposition; no unregistered orphan | Crash before/during/after Prepare, seal A vs B, unknown response, late abort | Remove pre-registration, permit direct abort, or finalize a non-referenced id and observe RED |
| C1-I35 | Renewal state is single-flight and monotonic | Renewal supervisor | Prepare completed, before Seal | stop/join + optional final renewal | exact B4 outcome | No stale/lower response overwrites state | Delayed-response and concurrent-renewal race | Allow periodic/final overlap or lower revision write and observe RED |
| C1-I36 | Only `Available` source descriptors can freeze into B4 mappings | Resolver repository | Before source-binding insert | actor-scoped Available-only query | selection/unavailable | Pre-publication ingress outcomes structurally cannot have frozen-job disposition | Architecture/source audit | Expose Reserved/Encrypting/Staged and observe structural test RED |
| C1-I37 | Every claim-token negative and reissue state has one closed oracle | `begin` + broker-owned complete | Lock-busy classified, then current evaluation id/digest/schema/variant/audience/v2-envelope binding/expiry/owner/revision/fence | stable alias + CAS-replaceable current slot + lock-busy code + live-evaluation code/retry boundary + two exact token SQL outcomes | idempotency-busy, evaluation-in-progress, claim-token-invalid or restart-required | No source disclosure/rebinding/new residue; live token never invalidated by concurrent begin | Response loss, concurrent begin, exact RetryNotBefore, expired/terminal/Bound reissue and one case per invalid/stale/envelope category | Collapse both busy causes, omit retry boundary, overwrite live slot, reuse evaluation id/token/revision/fence, omit latest-expiry horizon/category, or vary SQLSTATE/message and observe RED |
| C1-I38 | Alternate dependency failure returns through the authoritative arbiter | Claim-comparison broker + broker-only complete | Valid token and broker-internal typed provisional dependency result | DerivedAdmission/ActiveKeyUnavailable/HistoricKeyUnavailable union | fresh authority wins before capability/historic-key outcome | No final dependency error outside complete | New/Existing simultaneous authority-withdrawal + key-unavailable cases | Return directly from broker or move authority below typed-result mapping and observe RED |
| C1-I39 | Non-content producer claims are immutable per current token and persisted content membership remains keyed | Trusted server canonicalizer + alias + broker-owned complete + R3 | Before `begin`, inside complete before R1, and R3 actual verification | content-free v2 ProducerClaimEnvelopeFingerprint + keyed commitment + normalized broker result + recomputed AdmissionFingerprint | token-invalid for v2 envelope/lineage mismatch; Existing commitment conflict; New digest-only change fails at R3 | No bare digest, unkeyed persisted candidate verifier, trusted broker fingerprint scalar or New business-mismatch cell | Change each v2 field after begin; change only digest; cross-wire result; candidate-plaintext confirmation attempt | Put digest/content-derived value back into envelope, add any unkeyed persisted verifier, trust supplied AdmissionFingerprint, or map New lineage mismatch to business conflict and observe RED |
| C1-I40 | Reconciliation plaintext is bounded, zeroized and host-gated | Reconciliation worker + readiness | Before provisional open through each graceful/abrupt terminal | exact chunk/key lifetime + custody host manifest | custody-host-invalid/recapture/integrity outcome | No full-artifact plaintext buffer, dump/swap exposure or active-erasure claim after crash | Chunk/key lifetime instrumentation, cancellation/failure zeroize, dump/swap/debugger mutations | Say reconciler has no plaintext, retain one chunk/key, or remove custody-host gate and observe RED |
| C1-I41 | Token issuance, evaluation/busy waits, prior-R2 termination, reclaim and cleanup are finite and non-destructive | `begin` + alias current-slot CAS + readiness | Before issue/reissue/reclaim/cleanup and before CaptureAgent waits | fresh evaluation/token, strict revision/fence, monotonic latest expiry, exact RetryNotBefore, bounded backoff/TTL/budgets/margins, five state cases and 15 path relations | evaluation-in-progress/busy/restart/readiness-invalid/recapture | No live-token overwrite, hot polling, raw-token recovery, counter reuse, termination/reclaim double-count/omission, retention overrun or early cleanup | Lost response, each busy final, concurrent begin, Bound reissue, expiry/terminal/reclaim, all `RP-*` paths, max/±1/overflow | Delete an `RP-*` row; omit TTL/backoff/PreviousR2TerminationBudget/ReclaimExecutionBudget; merge pending/ready states; count termination after settlement; reset horizon/counter; or clean one microsecond early and observe RED |
| C1-I42 | R1 admits only an executable effective plaintext window | broker-owned complete + readiness | After fresh authority and effective-deadline calculation, before R1 | producer expiry + exact server cap + attempt deadline/margin | retention-invalid/time-bounds-invalid | No R1/source/attempt/key/object when producer-valid server cap is insufficient | Cap equality/±1, min selects each side, missing/range/overflow and producer-valid-cap-invalid | Check producer expiry alone, calculate effective after R1, omit cap relation/default it and observe RED |
| C1-I43 | Internal claim progression and CaptureAgent final response are separate exhaustive closed unions | ingress internal/final mapping boundary | After claim result and before CaptureAgent return | exact D4.3 internal and final variant/code/field manifests | mapping/readiness failure | No internal New/Existing/Reclaimed egress; no inferred shape, assembly outcome or retry metadata except final ClaimEvaluationInProgress.RetryNotBeforeUtc | Enumerate both unions, exact mapping and every final code/nullability cell | Serialize an InternalClaimResult, add unknown code/field, omit retry boundary or expose internal revision/fence and observe RED |
| C1-I44 | C1 canonical hashes have unambiguous field boundaries without changing landed JCS hashes | all `C1HashCanonical` codecs | Before hash/persist/compare | LP domain + independently LP-encoded canonical values/ordered-array count | codec/readiness failure | No delimiter/JSON/ambiguous concatenation or collision with landed Evidence-Integrity `HashCanonical` | Cross-language golden values with boundary-collision pairs and landed-JCS unchanged control | Remove one LP, swap array fields/count, use delimiter/JSON, alias the JCS name, or change a landed JCS vector and observe RED |
| C1-I45 | External ingress is exactly one metadata-first, admission-gated, bounded no-resume operation with exhaustive phase outcomes | CaptureAgent transport + ingress edge + custody | P0 through one P7 final result | section 10.0 Branch A metadata-only final, or Branch B committed R1/re-entry CAS → AdmissionAccepted → one body → R2–R6 | exact phase-table outcome | No body/admission on Branch A; no early application/proxy body acceptance, disk spill, upload session, part/receipt, resume, public complete or agent-held storage capability | Metadata-only matrix; real proxy/equivalent P0–P4 early-body, prebuffer, spill, signal-order and P4/P5 failure cases | Request body for Branch A, collapse P0–P3/P4 residue, emit signal before R1, enable buffering, expose internal call or infer multipart and observe RED |
| C1-I46 | Artifact size and host-local plaintext capacity are bounded without cross-host attestation | CaptureAgent local readiness + custody server admission + R2/R3 | Before local external call, before server begin/provider/key I/O and during actual byte count | eight-key physical-host configuration; pure local agent slots/bytes; custody-only server slots/windows | local SDK capacity result; server size-limit/capacity-unavailable/capacity-config-invalid | No agent reservation assertion, cross-host enforcement claim, unbounded buffer/window, overflow, oversubscription or Available oversize | Agent fixtures in CaptureAgent readiness; independently reachable custody producer/deployment/aggregate fixtures in server tests | Add an agent attestation field, remove a host-local limit/release or accept one excess byte and observe the appropriate named gate RED |
| C1-I47 | Initial class support is exactly the ratified still-image subset | readiness + ingress + resolver | Before source read | `ChipDg2Portrait`, `LiveSelfieImage` | unsupported capability | No `LivenessMedia` or other class, multipart inference or aggregate DG2 substitution | Every enum class, exact two positives, LivenessMedia negative | Admit LivenessMedia/another class or treat ChunkSize as a part and observe RED |
| C1-I48 | Transport incompleteness is retryable but a clean content mismatch is terminal | custody ingress + CaptureAgent | During R2 through clean R3 completion | transport termination signal + clean-EOF length/digest/commitment result | temporarily-unavailable/content-commitment-mismatch | No clean mismatch retried under the same UUID and no dropped connection misclassified as a content claim | Disconnect before first byte, disconnect mid-R2, clean shorter body, same-UUID retry after disconnect, same-UUID refusal after clean mismatch | Collapse both branches to either outcome and observe the named classification test RED |
| C1-I49 | Same-owner retry cannot overlap the previous fenced R2 writer | custody ownership head + provider adapter | Re-entry after a dead external call | exact owner identity + current reservation revision/fence + pending/ready split + durable `PreviousFencedR2Terminated` CAS accepting exact `Terminated` or `TerminatedBeforeStart` | ready internal progression, pending reservation busy, or recapture | No release-on-abort, concurrent writer, raw fence egress, dead conditional or lease-expiry-only retry | Immediate same-owner positive after armed termination and P4-before-start termination; different producer; both pending-termination cases | Merge pending/ready, reject P4 settling value, skip one termination predicate, accept different owner, reuse attempt/fence or expose fence and observe RED |
| C1-I50 | Every stable code has one non-overlapping P0–P7 disposition or an explicit structural exclusion | section 10.0 phase table | Before any transport/result implementation | exact phase/body/admission/residue/retry/union cell | table-mapped final or readiness/assembly structural exclusion | No blank, duplicate, inferred or independently restated transport branch | Enumerate every stable code and every section 10.2/10.3-only exclusion | Delete a code/cell, duplicate phase ownership, make a structural code egress or diverge D4.3/7.1/9.1/HLD/LLD and observe RED |

## 13. Test-Bite Matrix

| Claim | Positive control | Negative case | Broken mechanism | Named test expected RED | Restoration proof |
| --- | --- | --- | --- | --- | --- |
| S3 create is immutable | Same id/fingerprint returns same locator | Same id/different fingerprint conflicts | Remove conditional create | `C1_s3_create_is_idempotent_but_never_overwrites` | Provider empty + source hash restored |
| Encrypt-before-store | Decrypt round-trip matches source | Stored bytes must not contain plaintext | Write plaintext directly | `C1_provider_never_receives_plaintext` | Adapter/cipher pipeline hash restored |
| Binding is exact | Correct credential-derived producer/session/challenge/accepted-artifact/revision/class succeeds | Change each field independently or replay across challenge | Remove one authoritative equality check | `C1_source_binding_matrix_rejects_every_mismatch_and_replay` | Source and DB empty |
| Crash protocol converges | Every R1–R6 state resumes or cleans deterministically | Crash after each boundary | Remove visibility/reconciliation step | `C1_source_create_crash_matrix_never_exposes_unavailable_object` | One available object or zero residue |
| Fingerprints follow time order | Source-stable vector survives replacement; attempt vector rotates; staged vector uses exact attempt + actual R2 output | Put attempt fields in source fingerprint, require ciphertext at R1, or omit exact attempt at R3 | Merge any two fingerprint codecs | `C1_source_attempt_and_staged_fingerprints_are_non_circular_and_correctly_scoped` | Exact three golden vectors restored |
| Authority precedes read | Valid authority reads once | Withdrawn/expired authority reads zero | Move provider open before revalidation | `C1_authority_failure_precedes_provider_read` | Provider read count returns zero |
| Authority loss before Prepare creates no custody | Valid authority creates `Preparing` and calls Prepare once | Withdraw/expire before pre-registration | Move authority check after `Preparing`/Prepare | `C1_preprepare_authority_failure_creates_no_preparation_or_sink_call` | Preparation row/call count zero |
| Authority loss after Prepare cannot publish | Valid preparation reaches exact seal/finalize | Withdraw during/after Prepare and inside Seal | Skip Pending/Seal revalidation or allow finalize without committed seal | `C1_postprepare_authority_loss_quarantines_then_aborts_without_seal` | Preparation may exist; no seal/finalize; exact `AbortAuthorized → Aborted` |
| Fence controls publish | Current attempt seals | Reclaimed stale attempt fails publish | Remove fence from CAS | `C1_stale_worker_cannot_publish_after_reclaim` | One current seal only |
| Seal replay survives response loss | First call seals; identical replay returns `ExistingMatch` | Different item/digest/preparation conflicts | Require `Assembling`/live lease before existing branch | `C1_exact_seal_replay_from_AssemblySealed_is_idempotent` | One identity/item set/transition |
| Unknown seal never aborts committed preparation | Seal commits, response is lost, exact replay finds it | Timeout before caller receives result | Map indeterminate exception to abort | `C1_lost_seal_response_never_aborts_and_eventually_finalizes` | Exact preparation finalized once |
| Preparation arbitration is exact | Referenced target finalizes; another/unreferenced target aborts after CAS | Concurrent seal/abort and seal B vs candidate A | Remove disposition barrier or target-id equality | `C1_preparation_disposition_is_exact_monotonic_and_race_safe` | Referenced finalized; other candidate aborted |
| New-seal precedence matches B4 | Current attempt + valid authority seals | Make fence stale and authority withdrawn simultaneously | Move authority before admission | `C1_new_seal_stale_admission_precedes_authority_failure` | No identity/head/transition; preparation aborted |
| Seal class set is exact | Exact ordered B4 classes seal once | Missing, extra, substituted, duplicated, or reordered item | Remove one exact-set predicate | `C1_seal_requires_exact_B4_ordered_class_set` | No identity/head/transition |
| Cleanup and zeroization claims are honest | Graceful terminals execute zeroization; abrupt loss returns recapture | Skip graceful cleanup or simulate abrupt host loss | Disable graceful routine or claim crash erasure | `C1_captureagent_zeroizes_graceful_paths_and_never_claims_abrupt_erasure` | Buffer instrumentation/host evidence restored |
| Source binding freezes server-authored accepted evidence | Exact acceptance event/artifact/revision freezes once and retry existing-matches | Landed QualityState only, missing/conflicting selection, concurrent different source, recapture, substitutions | Trust caller/latest or overwrite loser | `C1_job_source_binding_requires_exact_authoritative_acceptance_selection` | Original acceptance/mapping/source inventory restored |
| Retained source reuse is independently authorized | Two distinct jobs with fresh authority bind the same retained source | Reuse without fresh authority or across mismatched purpose/consumer fails | Inherit authority from first job or enforce global source exclusivity | `C1_retained_source_can_bind_two_independently_authorized_jobs` | Both per-job mappings exact; no inherited authority |
| Sealed state is final for leasing | Unsealed job can acquire; sealed job returns closed outcome | Try claim/reclaim/renew after seal | Remove `AssemblySealed` exclusion | `C1_AssemblySealed_job_cannot_reenter_Assembling` | B4 amendment bytes restored |
| Class commitment is class-specific/private | Two independent keyed commitment vectors | DG2 aggregate digest alias and bare digest persistence fail | Reuse aggregate digest or persist claim | `C1_dg2_commitment_cannot_alias_nfc_or_expose_digest` | Exact fixture commitment restored |
| Readiness is fail-closed | Qualified S3/key/C2 fixture ready | Missing/unknown/extra capability fails | Default missing capability to ready | `C1_readiness_rejects_incomplete_or_unknown_manifest` | Pinned unavailable code restored |
| Key roles stay independent | Source encryption, assembly signing and C2 verification use compatible but separate manifests | Missing/revoked/mismatched signer or verifier fails readiness | Reuse source KEK/DEK as assembly-authentication key or collapse manifests | `C1_readiness_separates_source_encryption_from_assembly_authentication` | Independent active/historical key manifests restored |
| C2 preparation follows seal authority | Pre-register deterministic id, Prepare, exact seal, then finalize | Crash before/during/after Prepare plus stale/conflict at every seal boundary | Allocate opaque id after Prepare, finalize before seal, or omit abort/recovery | `C1_c2_prepare_seal_finalize_crash_matrix_is_fenced_and_idempotent` | No unregistered orphan and no deliverable without exact seal |
| Post-seal withdrawal does not fork custody | Exact committed seal finalizes its referenced preparation | Withdraw after seal before finalization | Abort or replace the referenced preparation after seal | `C1_post_seal_withdrawal_finalizes_custody_but_never_authorizes_delivery` | Exact preparation finalized; later delivery remains denied |
| Reclaim identity scopes are exact | Same immutable sources across attempts preserve stable identity/content | Attempt/fence/time change only attempt-scoped values | Put attempt fields in stable fingerprint or omit them from manifest/preparation fingerprint | `C1_reclaim_preserves_stable_assembly_but_rotates_attempt_manifest` | Golden stable/changed field set restored |
| Scalar canonicalization is interoperable | Independent implementation reproduces the complete fixed vector including decomposed/composed Unicode and a non-zero seventh timestamp digit | Omit NFC, change GUID `N` to `D`, truncate to six digits incorrectly, round instead of truncate, numeric string, enum case, hash case, or array order | Diverge one scalar encoder | `C1_cross_language_canonicalization_reproduces_all_identity_and_attempt_vectors` | NFC/normalize → canonicalize → hash → persist/readback → exact replay vector restored |
| Lease renewal enables progress without weakening fencing | Work longer than initial lease renews and seals with returned revision | Renewal conflict/expiry cancels and cannot seal | Remove renewals, ignore returned revision, or let renewal race seal | `C1_long_assembly_renews_lease_and_seals_only_with_latest_revision` | Repository/source hashes restored; no partial seal/preparation |
| Observability is default-deny | Allowed stable codes/ids are emitted | Locator, SubjectRef, provider/key/credential/raw fields are injected one at a time | Add prohibited field to a structured log/trace/metric/audit/dead-letter sink | `C1_prohibited_content_never_reaches_observability_or_metadata_surfaces` | Captured surfaces empty of canonical forbidden list |
| Public API has no custody authority | Public API starts with no provider/key credentials or registrations | Add any provider read/write/list/delete/admin/bootstrap or KEK registration | Co-host custody service in public process | `C1_public_api_process_has_no_provider_or_source_key_authority` | Composition manifest restored |
| Ingress idempotency is executable across rotation, restart and future alternate-key reuse | First call New; same/different instance/key replay Existing with Bound alias; concurrent timeout Busy | Same key/different fingerprint, same artifact/new UUID, winner rollback, then reuse alternate key against another artifact | Remove alias binding/tombstone, either unique edge, begin/broker-complete CAS, or hide-loser rule | `C1_ingress_claim_alias_is_durable_across_rotation_restart_and_future_reuse` | A(K1) New; A(K2) Existing+Bound; B(K2) Conflict with zero B shell/source |
| Atomic complete/R1 never advertises a context-free internal source | Successful NewCandidate InternalClaimResult observes source, reservation, attempt, fence, key reference, provisional identity and recovery context together | Inject fault immediately before commit; one Evaluating alias + bounded shell may remain and no result/source is visible | Split `complete` from R1 or return internal `NewReservation` before transaction commit | `C1_complete_returns_source_only_after_atomic_R1_context_commit` | Alias/shell residue only before commit; successful internal readback shows complete atomic context and no final egress |
| Producer envelope is content-free and frozen before token issuance | Same normalized v2 non-content envelope recomputes exact begin binding; keyed commitment/R3 owns digest | Change each v2 field after begin; change digest only; put digest back into v2; add a third unkeyed persisted candidate verifier | Omit persisted envelope binding, trust broker AdmissionFingerprint, or permit database-plus-candidate plaintext confirmation without a key | `C1_producer_claim_envelope_is_token_bound_and_complete_recomputes_admission` | Exact TOKEN_INVALID for v2/lineage mismatch; Existing keyed conflict and New R3 mismatch for digest-only change; no bare digest/unkeyed verifier |
| Claim token/reclaim identity cannot cross lineages | Existing Bound-alias token replay remains idempotent; New token completes only exact alias/shell | Tamper/wrong variant/audience/binding/expiry; reclaim then race stale token | Bypass persisted digest/constant-time verification, delete/reinsert, reset revision/fence, or omit alias/owner check | `C1_claim_token_contract_is_closed_and_stale_token_cannot_cross_reclaim` | Exact INVALID/RESTART SQL or success; one alias/canonical source; monotonic lineage |
| Fresh authority closes the begin/broker-complete and dependency-failure window | Authority remains valid and broker-internal DerivedAdmission succeeds/existing-matches | Pause after begin, withdraw/expire/change acceptance while active/historic key also becomes unavailable | Return broker failure directly or reuse begin snapshot | `C1_complete_authority_precedes_active_and_historic_key_failure` | New and Existing both return authority outcome; preserve exact alias/canonical residue; no source disclosure/allocation |
| Historic key rotation is replay-safe | Begin freezes stored selector internally; broker validates token; rotate before broker-owned Complete; exact replay succeeds | Historic key missing, stored version equals current active, or owner/revision/fence changes before restoration | Give ingress arbitrary historic access, classify unavailability by registry-active status, compare current key, or omit token/alias/owner/revision/fence restart | `C1_ingress_replay_uses_variant_typed_broker_across_rotation_and_reclaim` | Existing canonical row/Bound alias unchanged; same current-lineage token recovers after key restore; reclaimed token restarts |
| Token broker boundary is exact | Exact token/digest/alias/audience permits only frozen-selector HMAC and broker-owned `complete` | Tamper token, wrong audience/variant, cross-token/cross-alias result, grant ingress direct `complete`/validator/key edge, or remove broker edge | Accept caller selector, let a typed result cross to ingress, or expose general historic derive | `C1_claim_comparison_broker_owns_validation_derivation_and_complete` | Direct ingress completion denied; exact readiness/capability manifest and token/alias rows restored |
| Claim token lifecycle is closed | Mandatory bounded five-config manifest; DB-issued UTC-microsecond time; evaluation equals token lifetime; live slot never replaced; reissue is fresh CAS; latest expiry is monotonic and cleanup finite | Lost response, concurrent begin, exact RetryNotBefore, Bound reissue, missing/range/equality/overflow, diverged time, reused id/token/counter, cleanup one microsecond early | Collapse live evaluation into lock busy, omit retry boundary/TTL retention term, keep slot immutable forever, overwrite live issuance, add default/second lease, reset horizon/counter or clean early | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` | Exact lock-busy/evaluation-in-progress/restart/success, configuration/readiness and alias/shell residue restored |
| Token variant × provisional result is closed | Every matrix cell follows the pinned consume/replay/transition/residue rule and same-token retry requires the full current lineage | Classify Existing by active-version equality, force replay after reclaim, consume Active-unavailable New token, invent New business mismatch, or clear residue early | Collapse variants, ignore owner/revision/fence, or bypass broker→complete/fresh authority/lifetime | `C1_claim_token_variant_result_matrix_is_exact_and_authority_precedes_result` | Exact INVALID/RESTART/outcome and alias/canonical state restored |
| Reconciliation plaintext hygiene is executable | Complete ciphertext is bounded-decrypted one chunk at a time under custody host posture and every graceful path zeroizes chunk/key | Cancellation/failure/success, abrupt loss, swap/dump/debugger and oversize chunk | Treat producer-buffer independence as no plaintext or reuse CaptureAgent-only readiness | `C1_reconciliation_plaintext_chunks_are_bounded_zeroized_and_host_gated` | Buffers/keys disposed, no full plaintext/dump, readiness manifest restored |
| Complete R2 recovery is cryptographically executable | Crash after R1/key create and after complete R2 advances via exact provisional inspect/read without source plaintext | Remove profile/nonce recoverable seed/chunk/framing field, missing completion record, wrong attempt, corrupt frame, non-idempotent key reference | Let fingerprint digest substitute recovery input, make R3 depend on process-memory results, or restart R2 | `C1_complete_provisional_ciphertext_recovers_R3_without_R2_restart` | Exact descriptor readback/object/attempt/staged fingerprint; no second encryption |
| Producer outranks reconciler while continuation is possible | Live producer completes under current fence | Pause at ownership/min-age/latest-start boundaries | Let reconciler claim a healthy operation | `C1_reconciler_never_competes_with_live_plaintext_owner` | One owner/attempt; no premature cleanup |
| Time bounds fail closed | All 15 `RP-*` rows, the two justified non-projection relations and effective deadline pass | equality/±1/overflow/partial/relational invalid cases; token issued one unit before lease expiry makes old max-based `104 < 139` pass but corrected sequential `144 > 139` reject; pending same-owner 80+70 and settled expired-owner 80+50 each exceed 120 | Delete the path manifest row, restore max across unrelated anchors, or omit TTL, PreviousR2TerminationBudget or ReclaimExecutionBudget from its independently reachable path | `C1_plaintext_and_reconciliation_time_bounds_are_complete`; `C1_readiness_rejects_evaluation_wait_then_expired_owner_reclaim_combination`; `C1_readiness_rejects_late_token_after_lease_anchor_for_expired_owner_reclaim` | Exact sanitized readiness result; path↔relation diff and each deletion-only mutation RED |
| Attempt/key/object limits are finite | Three mandatory no-default keys pass ranges and relations; counters start below each exact limit | equality/±1, partial/malformed/overflow; each remaining limit independently binds while adjacent limits remain available | Remove one readiness key, atomic count or terminal release | `C1_attempt_key_object_limits_are_exact` | Exact readiness/runtime outcome; no excess attempt, key or object; cleanup restores capacity |
| Busy retry backoff is exact and retention-aware | Client pre-wait includes one future `BusyRetryBackoff`; readiness includes one worst-case wait; server post-wait includes none | zero/missing/out-of-range backoff; equality at latest start; each busy outcome independently selected; caller has already waited full backoff | Re-add elapsed backoff to server post-wait projection, replace with immediate retry/unbounded delay, or omit it from readiness/client pre-wait | `C1_busy_and_capacity_backoff_is_bounded_and_retention_aware`; `C1_server_post_wait_does_not_recharge_elapsed_busy_backoff` | Exact wait or recapture before waiting; after waiting, server judges only actual remaining lifetime plus future operation cost |
| Disposition envelope has one durable clock | First eligible disposition CAS writes start+expiry once; worker completes before the persisted expiry | retry, lease renewal, reclaim, restart and key/provider rotation attempt to reset; equality/±1 and overflow | Recompute expiry from current time or omit persisted start/expiry | `C1_disposition_envelope_has_one_persisted_non_resettable_clock` | Exact attempt row retains original start/expiry; overdue worker fails closed |
| Server continuation cap gates R1 | Producer and server cap both leave strictly enough effective time | Producer-valid input with cap equal/one microsecond short; missing/out-of-range cap | Check raw producer expiry or calculate cap only after R1 | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` | Retention/time-bounds outcome; zero R1/source/attempt/key/object |
| Internal and final result unions are exact | Every InternalClaimResult maps internally and every CaptureAgentFinalResult round-trips exact fields | Try to serialize NewReservation/ExistingMatch/ReservationReclaimed; unknown final code; missing/extra RetryNotBefore or final fields | Reuse one type, infer final fields from internal state, merge lock/live busy or use open bag | `C1_internal_claim_result_variants_never_cross_captureagent_final_result` | Two type manifests and exact internal-to-final mapping restored |
| C1HashCanonical framing is unambiguous | Independent implementation reproduces LP domain/scalars/array count while landed JCS vectors stay byte-identical | Boundary-collision strings, removed LP, swapped array count/field, JSON/delimiter encoding, name alias to landed codec | Concatenate raw variable fields or reuse landed codec name | `C1_hashcanonical_length_prefixes_every_domain_scalar_and_array_element` | All C1 golden vectors restored; landed Evidence-Integrity vectors unchanged |
| One-call phase table selects metadata-only or body-required branch | Metadata final returns without admission/body; New/re-entry commits R1/CAS, then AdmissionAccepted, one body and one final result | AlreadyAvailable, live evaluation, conflict, both busy classes, authority failure; real proxy/equivalent P0–P4 early body/prebuffer/spill/signal failure | Request/accept body on metadata final, flatten P4 into zero residue, emit signal before R1, expose an internal call or infer multipart | `C1_already_available_returns_without_admission_body_or_new_R1`; `C1_metadata_terminal_outcomes_never_request_or_accept_body`; `C1_phase_outcome_table_is_exhaustive_and_non_overlapping`; `C1_admission_handshake_rejects_early_body_and_proxy_prebuffering`; `C1_early_body_after_R1_before_admission_terminalizes_exact_attempt`; `C1_admission_signal_follows_committed_R1_before_agent_body_send` | Exact phase/body/admission/residue/union mapping; P4 anti-reuse/cleanup; no disk spill/external sub-operation |
| Size, memory and concurrency bounds fail closed | CaptureAgent locally admits its own slot/exact bytes; server independently admits class and custody stream/window capacity | Agent exhaustion handled without server call; server equality ±1, partial config, overflow, understated stream and independently binding custody aggregate/producer/deployment slots | Invent agent assertion, map local exhaustion to server capacity, remove a host-local limit/release or accept one excess byte | `CaptureAgent_raw_export_local_capacity_limits_fail_closed`; `C1_artifact_size_memory_and_concurrency_limits_fail_closed` | Local SDK and custody-only server boundaries restored; no cross-host attestation |
| Incomplete transport differs from clean commitment mismatch | Complete matching body succeeds; same UUID succeeds after retryable disconnect while the buffer/lifetime remain valid | Disconnect before first byte; disconnect mid-R2; clean EOF shorter than declared; same UUID after clean mismatch | Map an incomplete body to content mismatch, map clean mismatch to transient, omit fenced cleanup, or permit same UUID after terminal mismatch | `C1_incomplete_transport_and_clean_content_mismatch_are_distinct` | Exact temporary-unavailable/mismatch outcomes, fenced cleanup and retry/refusal policy restored |
| Same owner re-enters only after previous writer terminates | Exact same ingress owner/current row re-enters immediately with new monotonic attempt/fence after durable termination | Different producer; stale revision/fence; same owner while reader/write pump/provider attempt remains active | Release owner on abort, trust connection loss, skip provider terminal acknowledgement/CAS or reuse fence | `C1_same_owner_retry_after_disconnect_reenters_without_reservation_busy`; `C1_different_producer_remains_reservation_busy`; `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` | Owner marker retained; exactly one writer; exact busy/internal progression restored |
| D2 subset is exact | DG2 portrait and live selfie pass class capability | LivenessMedia and every other class fail before source read | Add LivenessMedia, map unknown class or treat encryption chunk as transport part | `C1_initial_raw_class_subset_excludes_heavy_media_until_resumable_transport` | Exact two-class manifest and unsupported outcome restored |
| Available-only freeze is structural | Repository returns only Available descriptors | Seed every pre-publication state | Expose one non-Available state | `C1_resolver_exposes_only_Available_descriptors` | No behavior mutation claimed for unreachable job cases |

Every mutation must be temporary, separately reported, and restored
byte-identically. A mutation that cannot exercise a supported path is N/A, not
green coverage. Before ratification and again before build closeout, every
mandated mutation must identify a supported fixture in which the mutated
predicate can independently decide the result. If an adjacent stronger
constraint subsumes the target, the mutation is redesigned or marked N/A before
execution and cannot count as proof.

## 14. Provider effort and decision record

| Area | S3-compatible + pinned MinIO reference | Encrypted filesystem |
| --- | --- | --- |
| Adapter implementation | Lower; mature SDK/protocol | Higher; durability primitives are owned by TagEkyc |
| Deployment | Separate service, TLS, IAM, bucket, monitoring | Dedicated volume, OS ACL, backup, quota |
| Atomic create | Conditional object operation | Exclusive create + fsync + atomic rename |
| Multi-replica | Natural | Requires qualified shared storage |
| Exit strategy | Replace with conforming S3 backend | Add S3 adapter and migrate objects |
| Current reuse | SignFlow compose familiarity only | Existing OS/volume operations only |
| Long-term risk | Archived Community maintenance/support posture | TagEkyc owns correctness and filesystem edge cases |

Decision for the first reference implementation:

```text
S3-compatible adapter + exact pinned MinIO integration environment
```

This decision minimizes initial adapter code while keeping production provider
substitution open. It does not approve `minio/minio:latest`, a shared root
account, or MinIO production activation.

## 15. Readiness contract

C1 readiness is false unless the exact selected tuple is complete:

```text
mode
× ratified mode/controller/legal/retention/purge/hold/access decisions
× every frozen raw class
× producer version
× server-authored capture-acceptance event/session-selection producer
× one-call metadata-first/admission-gated/no-resume custody ingress transport
× qualified ingress/proxy no-prebuffer/no-disk-spill posture and bounded
  pre-admission network-buffer ceiling
× exact D2 two-class support
× exact eight-key physical-host capacity manifest
× CaptureAgent-local buffer-slot/aggregate-byte readiness and host posture;
  no dynamic server attestation
× custody producer/deployment stream-slot/aggregate-window admission
  with crash-expiring server capacity
× public-API/custody process and credential topology
× provider adapter
× provisional create/write/inspect/read/commit/abort and committed-read capability/version
× source-encryption suite/version
× source KEK provider/key version + idempotent recoverable key-reference capability
× content-commitment schema plus every referenced historic key version
× subject-token schema plus every referenced historic key version
× StableDataScopeId/controller scope
× assembly-authentication scheme/version
× assembly signer provider/key version
× C2 assembly-verifier scheme/key/version compatibility
× B4 transition/CAS capability
× C2 deterministic-id Prepare/exact-lookup/finalize/abort capability (production only)
× CaptureAgent sensitive-memory/host-posture evidence
× complete timeout/reclaim/disposition relation including all four mandatory
  PreviousR2TerminationBudget components and all three runtime projections
× applicable GOV/ART phase-gate evidence
```

Unknown or extra manifest entries are failures, not ignored extensions.
Fixture/in-memory providers can satisfy unit tests only. Pinned MinIO can
satisfy reference integration evidence only. Production requires the separately
approved provider, key, operations, retention, and C2 manifests.

The process/capability manifest must equal the D4 graph, including operation
direction: ingress calls only `begin`; the broker alone validates the
authenticated begin-issued token against alias/revision state, derives with the
frozen active/historic selector and calls `complete`. Ingress cannot see the
selector or provisional result, call `complete`, enumerate keys or freely derive
historic keys. Reconciliation and assembly
verify only stored historic versions; reconciliation opens only exact fenced
provisional attempts; assembly opens only exact committed `Available` sources;
lifecycle has delete/hold without read/HMAC/unwrap; public/general API has none.
A missing required edge and any extra edge both fail readiness.

Source encryption and assembly authentication are independent capabilities.
The source KEK/DEK must not be reused as the assembly-authentication key.
Readiness fails closed for a missing, unknown, revoked, out-of-window, or
cross-capability-mismatched key/scheme/version. Rotation must preserve historical
verification for every unexpired seal while allowing only the active signer for
a new seal.

Content-commitment and subject-token key registries must restore every version
referenced by retained rows and prove HA, escrow/backup, DR transfer, capacity,
retirement evidence, and fixture-key rejection. Missing historic commitment key
uses `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE`; an incomplete
historic inventory makes readiness false.

### 15.1 Normative symbol register

This register is the sole definition of every duration variable, threshold,
derived quantity, phase predicate and disposition symbol used normatively by
this Planning Brief. A symbol without a row is a specification defect. Every
configuration conversion and derivation uses checked arithmetic. “N/A” below is
an explicit non-applicable contract, never a blank or an implicit default.

| Symbol | Kind | Owner | Source | Unit | Inclusive range | Default rule | OBSERVER | Invalid/indeterminable failure code | Boundary test |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ClaimEvaluationTokenTtl` | configuration | Claim-token readiness | `RawExportSourceClaimEvaluationTokenTtlSeconds` | seconds | `[1,300]` | Mandatory, no implicit default | Readiness parses the startup key; DB `statement_timestamp()` fixes each issue/expiry horizon at issuance | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `IdempotencyLockTimeout` | configuration | Custody claim repository | `RawExportSourceClaimIdempotencyLockTimeoutMilliseconds` | milliseconds | `[1,5000]` | Mandatory, no implicit default | Readiness parses the startup key; repository statement timeout enforces it when the claim lock starts | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ClaimComparisonBudget` | configuration | Claim-comparison broker | `RawExportSourceClaimComparisonBudgetMilliseconds` | milliseconds | `[1,120000]` | Mandatory, no implicit default | Readiness parses the startup key; broker cancellation deadline observes it at derive start | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `CompleteTransactionBudget` | configuration | Broker-owned `complete` repository | `RawExportSourceClaimCompleteTransactionBudgetMilliseconds` | milliseconds | `[1,30000]` | Mandatory, no implicit default | Readiness parses the startup key; repository statement/transaction timeout observes it at `complete` entry | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `SafetyMargin` | configuration | C1 time-bounds readiness | `RawExportSourceClaimSafetyMarginMilliseconds` | milliseconds | `[1,30000]` | Mandatory, no implicit default | Readiness parses the startup key and includes it in every registered continuation projection | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `TombstoneSafetyMargin` | configuration | Claim-alias cleanup | `RawExportSourceClaimTombstoneSafetyMarginSeconds` | seconds | `[1,300]` | Mandatory, no implicit default | Readiness parses the startup key; cleanup worker reads the durable latest-issued horizon before deletion CAS | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `ConfiguredMaximumPlaintextRetentionBudget` | configuration | Custody time-bounds readiness | `RawExportSourceMaximumPlaintextRetentionBudgetSeconds` | seconds | `[1,86400]` | Mandatory, no implicit default | Readiness parses the startup key; ingress compares the authenticated producer interval before `begin` | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` at readiness; `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` at runtime | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `AllowedProducerClockSkew` | configuration | Custody ingress | `RawExportSourceAllowedProducerClockSkewSeconds` | seconds | `[0,300]` | Mandatory, no implicit default | Readiness parses the startup key; ingress compares authenticated producer start with trusted server time before `begin` | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` at readiness; `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` at runtime | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ConfiguredMaximumRemainingContinuationWindow` | configuration | Custody continuation readiness | `RawExportSourceMaximumRemainingContinuationWindowSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; R1 transaction freezes its server-capped deadline | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `EncryptionAttemptDeadline` | configuration | Custody producer/reconciler | `RawExportSourceEncryptionAttemptDeadlineSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; producer/reconciler cancellation deadline observes it when R2 or recovered R3 work starts | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `OwnershipLeaseDuration` | configuration | C1 source-reservation owner | `RawExportSourceOwnershipLeaseDurationSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; the R1/re-entry/reclaim transaction derives `OwnershipLeaseExpiresAtUtc` from DB time | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ReconcilerMinimumAge` | configuration | C1 custody reconciler | `RawExportSourceReconcilerMinimumAgeSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; the exact owner-CAS DB timestamp is persisted as the reservation/attempt timestamp and is the same anchor used to derive that generation's ownership lease; reconciler compares DB time with it before claim | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_reconciler_never_competes_with_live_plaintext_owner` |
| `ReclaimExecutionBudget` | configuration | C1 producer reclaim | `RawExportSourceReclaimExecutionBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; expired-owner reclaim projection includes it before the reclaim CAS and replacement R2 | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ReaderQuiescenceBudget` | configuration | Prior-R2 termination supervisor | `RawExportSourceReaderQuiescenceBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; supervisor deadline observes reader exit for the exact attempt/fence | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `ProviderWriterQuiescenceBudget` | configuration | Prior-R2 termination supervisor | `RawExportSourceProviderWriterQuiescenceBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; supervisor deadline observes provider-writer task quiescence for the exact attempt/fence | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `ProviderAbortOrInspectionBudget` | configuration | Prior-R2 termination supervisor | `RawExportSourceProviderAbortOrInspectionBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; supervisor waits for exact-attempt terminal acknowledgement or non-writability inspection | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `TerminationCasBudget` | configuration | Prior-R2 termination repository | `RawExportSourceTerminationCasBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; repository timeout observes the old-fence termination CAS | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `DispositionEnvelope` | configuration | C1 reconciliation readiness | `RawExportSourceDispositionEnvelopeSeconds` | seconds | `[1,86400]` | Mandatory, no implicit default | Readiness parses the startup key; the first disposition-required CAS uses it once to derive the persisted non-resettable expiry | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_disposition_envelope_has_one_persisted_non_resettable_clock` |
| `ReconciliationBudget` | configuration | C1 custody reconciler | `RawExportSourceReconciliationBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; reconciler deadline observes one exact recovery/disposition execution | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ProviderAbortOrFinalizeBudget` | configuration | C1 reconciliation provider adapter | `RawExportSourceProviderAbortOrFinalizeBudgetSeconds` | seconds | `[1,3600]` | Mandatory, no implicit default | Readiness parses the startup key; adapter deadline observes exact-attempt abort or finalize acknowledgement | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `RawExportIngressMaximumPreAdmissionBufferedBytes` | configuration | Qualified ingress topology | Exact key `RawExportIngressMaximumPreAdmissionBufferedBytes` | bytes | `[1,65536]` | Mandatory, no implicit default | Ingress readiness inspects every proxy/middleware hop before activation; runtime counts detected bounded residual before P5 | `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID` | `C1_admission_handshake_rejects_early_body_and_proxy_prebuffering` |
| `MaximumChipDg2PortraitBytes` | configuration | Shared CaptureAgent/custody class contract | `RawExportSourceMaximumChipDg2PortraitBytes` | bytes | `[1,67108864]` | Mandatory, no implicit default | Both readiness owners parse equal startup values; agent/server byte counters observe declared/actual class size at local admission and P1/P5 | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `MaximumLiveSelfieImageBytes` | configuration | Shared CaptureAgent/custody class contract | `RawExportSourceMaximumLiveSelfieImageBytes` | bytes | `[1,67108864]` | Mandatory, no implicit default | Both readiness owners parse equal startup values; agent/server byte counters observe declared/actual class size at local admission and P1/P5 | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `RawExportCaptureMaximumConcurrentRetainedBuffersPerHost` | configuration | CaptureAgent host | Exact same-named configuration key | buffers | `[1,32]` | Mandatory, no implicit default | CaptureAgent readiness parses it; host-local atomic counter observes it before retaining/submitting a buffer | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` on CaptureAgent | `CaptureAgent_raw_export_local_capacity_limits_fail_closed` |
| `RawExportCaptureMaximumAggregatePlaintextBytesPerHost` | configuration | CaptureAgent host | Exact same-named configuration key | bytes | `[1,2147483647]` | Mandatory, no implicit default | CaptureAgent readiness parses it; host-local atomic exact-byte counter observes it before retaining/submitting a buffer | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` on CaptureAgent | `CaptureAgent_raw_export_local_capacity_limits_fail_closed` |
| `RawExportCustodyMaximumPlaintextWindowBytesPerStream` | configuration | Custody host | Exact same-named configuration key | bytes | `[1,16777216]` | Mandatory, no implicit default | Server readiness parses it; custody allocator observes exact window reservation before `begin` | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `RawExportCustodyMaximumConcurrentStreamsPerProducer` | configuration | Custody host | Exact same-named configuration key | streams | `[1,32]` | Mandatory, no implicit default | Server readiness parses it; atomic per-producer counter observes it before `begin` | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `RawExportCustodyMaximumConcurrentStreamsPerDeployment` | configuration | Custody deployment | Exact same-named configuration key | streams | `[1,256]` | Mandatory, no implicit default | Server readiness parses it; atomic deployment counter observes it before `begin` | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment` | configuration | Custody deployment | Exact same-named configuration key | bytes | `[1,2147483647]` | Mandatory, no implicit default | Server readiness parses it; atomic exact-window counter observes it before `begin` | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `MaximumActiveAttemptsPerSource` | configuration | C1 custody readiness/repository | `RawExportSourceMaximumActiveAttemptsPerSource` | attempts | `[1,64]` | Mandatory, no implicit default | Readiness parses it; repository counts non-terminal exact-attempt rows under the source lock before attempt creation | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_attempt_key_object_limits_are_exact` |
| `MaximumKeyMaterialRecordsPerSource` | configuration | C1 key-custody readiness/repository | `RawExportSourceMaximumKeyMaterialRecordsPerSource` | key-material records | `[1,64]` and `>= MaximumActiveAttemptsPerSource` | Mandatory, no implicit default | Readiness parses it; exact-source transaction counts live/non-destroyed key-material records before key preparation | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_attempt_key_object_limits_are_exact` |
| `MaximumProvisionalObjectsPerSource` | configuration | C1 provider-custody readiness/repository | `RawExportSourceMaximumProvisionalObjectsPerSource` | provisional objects | `[1,64]` and `>= MaximumActiveAttemptsPerSource` | Mandatory, no implicit default | Readiness parses it; exact-source transaction counts non-terminal provisional identities before provider create | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` at readiness; `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` at runtime | `C1_attempt_key_object_limits_are_exact` |
| `MaximumRetryCountPerIngress` | Build-Brief-owned declaration | `C1-BB-RETRY-COUNT-ACCOUNTING-GATE` | Proposed key `RawExportSourceMaximumRetryCountPerIngress`; the Build Brief must pin all nine accounting questions below before dispatch | retries | Planning state admits no enforceable value; post-gate proposed range `[1,1024]` only if ratified | Unpinned bound; no implicit/default value and no durable cross-process enforcement claim; unresolved gate is unconditional STOP/RRI | Planning records the proposed bound only. The Build Brief must identify the exact agent/server counters, durable owner, comparison and recovery semantics before readiness or runtime can enforce it | Unresolved gate blocks Build-Brief dispatch/readiness; post-gate exact codes must be ratified | `C1-BB-RETRY-COUNT-ACCOUNTING-GATE` plus its named Build-Brief test |
| `BusyRetryBackoff` | configuration | CaptureAgent retry projector | `RawExportSourceBusyRetryBackoffMilliseconds` | milliseconds | `[1,30000]` | Mandatory, no implicit default | CaptureAgent readiness parses it; monotonic delay applies exactly after capacity-unavailable, idempotency-busy or reservation-busy and is admitted only while the full continuation projection still fits | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` at readiness; `RECAPTURE_REQUIRED` when delay cannot fit | `C1_busy_and_capacity_backoff_is_bounded_and_retention_aware` |
| `PlaintextRetentionStartedAtUtc` | externally owned | Authenticated CaptureAgent producer envelope | Section 5.6.3 field `PlaintextRetentionStartedAtUtc` | UTC microseconds | `[capture-buffer acquisition, PlaintextRetentionExpiresAtUtc)` | Mandatory producer field, no implicit default | CaptureAgent monotonic clock projects it; ingress verifies signature/binding and compares trusted server time before `begin` | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `PlaintextRetentionExpiresAtUtc` | externally owned | Authenticated CaptureAgent producer envelope | Section 5.6.3 field `PlaintextRetentionExpiresAtUtc` | UTC microseconds | `(PlaintextRetentionStartedAtUtc, Started + ConfiguredMaximumPlaintextRetentionBudget]` | Mandatory producer field, no implicit default | CaptureAgent monotonic clock projects it; ingress verifies binding before `begin` and R1 freezes the effective minimum | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `PlaintextRetentionBudgetSeconds` | externally owned | Authenticated CaptureAgent producer envelope | Section 5.6.3 field `PlaintextRetentionBudgetSeconds = Expires - Started` | seconds | `[1, ConfiguredMaximumPlaintextRetentionBudget]` | Mandatory producer field, no implicit default | CaptureAgent derives it from one monotonic lifetime; ingress proves exact equality before `begin` | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `RetryNotBeforeUtc` | externally owned | Claim-evaluation DB row/final result | `ClaimEvaluationInProgress.RetryNotBeforeUtc = CurrentTokenExpiresAtUtc` | UTC microseconds | `[ProjectionTimestamp, LatestIssuedTokenExpiresAtUtc]` | Exact returned field, no implicit default | DB row owns it durably; CaptureAgent learns it only from the authenticated final result | `RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS` with exact field; malformed/indeterminable response fails closed to recapture | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `TokenIssuedAtUtc` | derived | Claim-token issuance transaction | DB `statement_timestamp()` truncated to UTC microseconds | UTC microseconds | `[transaction statement start, transaction statement end]` | Derived once per issuance, no default | DB issuance statement determines and persists it atomically | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `TokenExpiresAtUtc` | derived | Claim-token issuance transaction | `TokenIssuedAtUtc + ClaimEvaluationTokenTtl` | UTC microseconds | `(TokenIssuedAtUtc, TokenIssuedAtUtc + 300 seconds]` | Derived once per issuance, no default | DB issuance statement computes and persists it atomically | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `ClaimEvaluationIssuedAtUtc` | derived | Claim-evaluation row | `ClaimEvaluationIssuedAtUtc = TokenIssuedAtUtc` | UTC microseconds | Exact equality with `TokenIssuedAtUtc` | Derived once, no default | DB issuance statement persists both equal values in one transaction | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `ClaimEvaluationExpiresAtUtc` | derived | Claim-evaluation row | `ClaimEvaluationExpiresAtUtc = TokenExpiresAtUtc` | UTC microseconds | Exact equality with `TokenExpiresAtUtc` | Derived once, no default | DB issuance statement persists both equal values in one transaction | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `CurrentTokenIssuedAtUtc` | derived | Current claim-evaluation row | The current row's durable `TokenIssuedAtUtc` | UTC microseconds | Equal to the current successful issuance time | No synthetic/default value | DB row determines it at cleanup-bound evaluation | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `CurrentTokenExpiresAtUtc` | derived | Current claim-evaluation row | The current row's durable `TokenExpiresAtUtc` | UTC microseconds | `(CurrentTokenIssuedAtUtc, CurrentTokenIssuedAtUtc + 300 seconds]` | No synthetic/default value | DB row determines it when returning the exact retry boundary | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `R1TransactionTimestamp` | derived | R1 transaction | DB transaction timestamp used by the exact R1 transaction | UTC microseconds | `[R1 transaction begin, R1 commit]` | Derived once, no default | R1 repository obtains it from DB after waits and before persistence | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `CompleteStatementTimestamp` | derived | Broker-owned `complete` transaction | DB statement timestamp at the strict remaining-lifetime gate | UTC microseconds | `[complete statement start, complete transaction end]` | Derived at statement, no default | `complete` repository obtains it from DB immediately before the R1 gate | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `CapacityAdmissionStatementTimestamp` | derived | Custody capacity transaction | DB statement timestamp at atomic capacity admission | UTC microseconds | `[capacity statement start, capacity commit]` | Derived at statement, no default | Capacity repository obtains it from DB immediately before persisting the crash horizon | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `ProjectionTimestamp` | derived | Custody server post-wait projector | Trusted DB current time at the exact admission/re-entry/reclaim decision, after any caller wait | UTC microseconds | `[operation entry, decision commit]` | Derived at decision, never configured/defaulted | Repository reads DB time immediately before projection/CAS; actual remaining lifetime is measured from this timestamp whether or not the caller waited | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_server_post_wait_does_not_recharge_elapsed_busy_backoff` |
| `ReadinessProjectionPrecedingWait` | derived | Configuration-time readiness projector | Closed path cases: none → `0`; live evaluation → `ClaimEvaluationTokenTtl`; one busy final → `BusyRetryBackoff` | checked duration | `[0, max(ClaimEvaluationTokenTtl, BusyRetryBackoff)]` | Total over the three readiness wait cases; exactly one future worst-case wait per `RP-*` row | Readiness expands all five states × three wait cases before activation | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | path↔relation audit plus both expired-owner late-wait negatives |
| `ExpiredOwnerReadinessWait` | derived | Configuration-time readiness projector | None → `max(OwnershipLeaseDuration, ReconcilerMinimumAge)`; evaluation → `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + ClaimEvaluationTokenTtl`; busy → `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + BusyRetryBackoff` | checked duration | `[max(OwnershipLeaseDuration, ReconcilerMinimumAge), max(OwnershipLeaseDuration, ReconcilerMinimumAge) + max(ClaimEvaluationTokenTtl, BusyRetryBackoff)]` | Total over the same three cases. Only lease and minimum age share the exact owner-CAS reservation/attempt anchor and may use `max`; a token/backoff can start later and is sequential | Readiness computes each exact expired-owner row; runtime does not re-add elapsed time | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_readiness_rejects_late_token_after_lease_anchor_for_expired_owner_reclaim` |
| `PreviousR2TerminationBudget` | derived | Prior-R2 termination supervisor | `ReaderQuiescenceBudget + ProviderWriterQuiescenceBudget + ProviderAbortOrInspectionBudget + TerminationCasBudget` | checked duration | `[4,14400]` seconds | Derived exactly, no default | Readiness computes checked sum; runtime includes it only while exact previous termination is not durable | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ReadinessWorstCaseContinuation` | derived | Configuration-time readiness projector | Exact `RP-01`–`RP-15` row formula below: one worst-case preceding wait plus future operation cost | checked duration | Strictly positive and `< ConfiguredMaximumRemainingContinuationWindow` for every row | Total function over exactly 15 path IDs; no path or second/repeated wait is inferred | Readiness evaluates the complete path manifest before activation | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | path↔relation audit and `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ClientPreWaitContinuation` | derived | CaptureAgent pre-wait projector | Future wait from the authenticated final result (`0`, `max(RetryNotBeforeUtc - client trusted now, 0)`, or `BusyRetryBackoff`) plus `max` of the five exact future-operation state costs because no internal state case crosses the final-result boundary | checked duration | `[maximum future operation cost, ConfiguredMaximumPlaintextRetentionBudget)` | Total over the closed final-result union; evaluated before waiting; unknown/overflow/insufficient lifetime is recapture | CaptureAgent compares its monotonic remaining buffer lifetime before sleeping; no durable poll row is created | `RECAPTURE_REQUIRED` | `C1_busy_and_capacity_backoff_is_bounded_and_retention_aware` |
| `ServerPostWaitContinuation` | derived | Custody server post-wait projector | Future operation cost only: lock + comparison + complete + encryption + safety, plus prior-R2 termination iff pending and reclaim iff expired-owner; preceding wait is exactly absent | checked duration | Strictly positive and `< actual remaining lifetime` | Total over the five state cases; no wait term under any outcome | Repository computes from trusted DB `ProjectionTimestamp` immediately before decision/CAS | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` or `RECAPTURE_REQUIRED`; never a second wait charge | `C1_server_post_wait_does_not_recharge_elapsed_busy_backoff` |
| `ServerMaximumContinuationExpiresAtUtc` | derived | R1 transaction | `R1TransactionTimestamp + ConfiguredMaximumRemainingContinuationWindow` | UTC microseconds | `(R1TransactionTimestamp, R1TransactionTimestamp + 3600 seconds]` | Derived exactly, no default | R1 transaction computes and persists it from DB time and registered configuration | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `EffectivePlaintextRetentionExpiresAtUtc` | derived | R1 transaction | `min(PlaintextRetentionExpiresAtUtc, ServerMaximumContinuationExpiresAtUtc)` | UTC microseconds | `(R1TransactionTimestamp, PlaintextRetentionExpiresAtUtc]` | Derived exactly, no default | R1 transaction computes and persists it after producer interval validation | `RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID` | `C1_complete_rejects_insufficient_effective_server_cap_before_R1` |
| `OwnershipLeaseExpiresAtUtc` | derived | C1 source-reservation repository | `R1/re-entry/reclaim DB timestamp + OwnershipLeaseDuration` | UTC microseconds | `(owner-CAS timestamp, owner-CAS timestamp + 3600 seconds]` | Derived at each successful owner CAS, no default | Repository persists it atomically; producer/reconciler compares DB time with it before continue/reclaim | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_reconciler_never_competes_with_live_plaintext_owner` |
| `AbsoluteSourceExpiresAtUtc` | externally owned | Ratified authority snapshot | Section 5.4 `AuthoritySnapshot.AbsoluteSourceExpiresAtUtc` | UTC microseconds | `(authority evaluation time, controller-approved absolute horizon]` | Mandatory authority field, no implicit default or retry extension | Authority evaluator persists it in the immutable snapshot; every checkpoint reads the referenced snapshot | `SOURCE_RETENTION_NOT_AUTHORIZED` | `C1_authority_is_revalidated_at_every_checkpoint` |
| `ReservationExpiresAtUtc` | derived | C1 source-reservation repository | `min(OwnershipLeaseExpiresAtUtc, EffectivePlaintextRetentionExpiresAtUtc, AbsoluteSourceExpiresAtUtc)` | UTC microseconds | `(R1TransactionTimestamp, min of the three owning horizons]` | Derived at R1/re-entry/reclaim, no default | Repository computes/persists it under the owner CAS; no caller supplies or extends it | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` or `SOURCE_RETENTION_NOT_AUTHORIZED` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ContinuationLatestStartUtc` | derived | Custody server post-wait projector | `EffectivePlaintextRetentionExpiresAtUtc - ServerPostWaitContinuation` | UTC microseconds | `[PlaintextRetentionStartedAtUtc, EffectivePlaintextRetentionExpiresAtUtc)` when admissible | Derived exactly, no default | Server computes it at the exact continuation/reclaim decision; start is allowed only while DB time is strictly earlier | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` or `RECAPTURE_REQUIRED` | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `CapacityAdmissionExpiresAtUtc` | derived | Custody capacity repository | `min(PlaintextRetentionExpiresAtUtc, CapacityAdmissionStatementTimestamp + ConfiguredMaximumPlaintextRetentionBudget)` | UTC microseconds | `(CapacityAdmissionStatementTimestamp, PlaintextRetentionExpiresAtUtc]` | Derived exactly, no default | Admission transaction persists it; capacity reconciler reads it with DB time before expiry CAS | `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` | `C1_artifact_size_memory_and_concurrency_limits_fail_closed` |
| `LatestIssuedTokenExpiresAtUtc` | derived | Claim alias row | `max(previous LatestIssuedTokenExpiresAtUtc, TokenExpiresAtUtc)` | UTC microseconds | Monotonic through canonical retention horizon | Initialized by first successful issuance; never reset/defaulted | DB issuance CAS persists it durably; cleanup reads the row after token expiry | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `EarliestUnboundShellCleanupUtc` | derived | Claim cleanup worker | `LatestIssuedTokenExpiresAtUtc + TombstoneSafetyMargin` | UTC microseconds | `(LatestIssuedTokenExpiresAtUtc, CurrentTokenIssuedAtUtc + 600 seconds]` | Derived exactly, no default | Cleanup worker reads durable latest expiry and margin immediately before delete CAS | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_claim_token_ttl_reissue_and_cleanup_horizon_are_exact` |
| `DispositionStartedAtUtc` | derived | Exact source-attempt disposition CAS | DB statement timestamp of the first CAS from a non-disposition state to `TerminatedBeforeStart`, `Terminated`, `AbortAuthorized` or another section 10.0 cleanup-required terminal disposition | UTC microseconds | Absent before disposition is required; then `[first disposition CAS start, commit]` exactly once | Total optional: absent in every non-disposition state; written once on the first enumerated transition and never reset | Exact attempt/disposition row persists it under revision/fence; repository rejects a second start value | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_disposition_envelope_has_one_persisted_non_resettable_clock` |
| `DispositionExpiresAtUtc` | derived | Exact source-attempt disposition CAS + custody reconciler | `DispositionStartedAtUtc + DispositionEnvelope` | UTC microseconds | Absent iff `DispositionStartedAtUtc` is absent; otherwise strictly later by the configured envelope | Total optional paired function; derived/written atomically with the start and never extended | Exact attempt/disposition row persists it; reconciler compares DB time immediately before worker-claim/disposition CAS | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` | `C1_disposition_envelope_has_one_persisted_non_resettable_clock` |
| `PreviousFencedR2Terminated` | predicate | Prior-R2 termination supervisor + repository | True only when exact reader exit/non-arming proof, writer quiescence/non-arming proof, provider terminal acknowledgement/non-writability inspection or no-provider-start proof, and old-revision/fence CAS with `R2TerminationDisposition IN {Terminated, TerminatedBeforeStart}` plus `R2TerminatedAtUtc` are all durable | Boolean | `{false,true}` | False until every applicable durable fact exists; P4 uses the explicit non-arming facts, never an inference | Server determines it from joined task completion or P4 non-arming evidence, provider evidence and persisted exact-fence CAS; connection loss alone is false | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` while false; `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` if evidence is indeterminable | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `FreshFirstAttempt` | predicate | Custody continuation projector | No previous attempt/termination and no expired-owner reclaim is selected | Boolean | `{false,true}` | False unless R1 creates the first attempt | R1 transaction knows no prior attempt exists at the projection/CAS point | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` if indeterminable | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `SameOwnerRetryPendingTermination` | predicate | Custody continuation projector | Exact authenticated owner tuple + current revision/fence match + fresh authority/lifetime, and `PreviousFencedR2Terminated == false` | Boolean | `{false,true}` | False unless all identity/freshness conjuncts pass and only termination remains pending | Repository and termination supervisor read the exact current owner/attempt; no replacement CAS occurs while true | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` if the pending path fits; otherwise `RECAPTURE_REQUIRED` | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `SameOwnerReentryReady` | predicate | Custody continuation projector | Exact authenticated owner tuple + current revision/fence CAS + fresh authority/lifetime + `PreviousFencedR2Terminated == true` | Boolean | `{false,true}` | False unless every conjunct is proven | Repository proves the current authenticated owner/revision/fence and durable settling evidence at the replacement CAS | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` on mismatch; `RECAPTURE_REQUIRED` if the ready path cannot fit | `C1_same_owner_retry_after_disconnect_reenters_without_reservation_busy` |
| `ExpiredOwnerReclaimPendingTermination` | predicate | Custody continuation projector | Ownership lease expired by DB time, producer reclaim identity/freshness passes, and `PreviousFencedR2Terminated == false` | Boolean | `{false,true}` | False unless expiry/reclaim eligibility pass and only termination remains pending | Repository and termination supervisor observe the exact expired owner/attempt; no replacement CAS occurs while true | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` if the pending path fits; otherwise `RECAPTURE_REQUIRED` | `C1_reconciler_never_competes_with_live_plaintext_owner` |
| `ExpiredOwnerProducerReclaimReady` | predicate | Custody continuation projector | Ownership lease expired by DB time, producer reclaim identity/freshness passes, and `PreviousFencedR2Terminated == true` | Boolean | `{false,true}` | False unless expiry, reclaim eligibility and durable settlement are proven | Repository reads DB time/current owner row and wins the expired-owner CAS before replacement R2 | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` on CAS loss; `RECAPTURE_REQUIRED` if the ready path cannot fit | `C1_reconciler_never_competes_with_live_plaintext_owner` |
| `P4Boundary` | predicate | Ingress transport server | R1/re-entry CAS durable; `AdmissionAccepted` not successfully written/flushed; body reader and provider writer not armed | Boolean | `{false,true}` | False unless every stated fact is server-known | Server observes its R1 commit, signal write/flush state and reader/writer construction state before arming either | `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID` at readiness; `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` at runtime | `C1_early_body_after_R1_before_admission_terminalizes_exact_attempt` |
| `P5Boundary` | predicate | Ingress transport server | `AdmissionAccepted` successfully written/flushed OR body reader armed; after successful emission classify P5 unless no reader, closed old connection and no writer-reachable byte are proven | Boolean | `{false,true}` | Conservative true after successful signal emission | Server observes write/flush completion and reader arming; it never depends on client observation/acknowledgement | `RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID` at readiness; `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` at runtime | `C1_admission_signal_follows_committed_R1_before_agent_body_send` |
| `AdmissionAccepted` | disposition | Ingress transport server | Section 10.0 transport signal after durable R1/re-entry CAS | N/A — transport signal | N/A — absent or emitted once | Absent unless body-required Branch B is selected | Server knows successful write/flush; CaptureAgent learns only from the received transport signal | `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` on ambiguous/lost emission | `C1_admission_signal_follows_committed_R1_before_agent_body_send` |
| `AdmissionProtocolRejected` | disposition | C1 source-reservation/attempt repository | Exact P4 CAS target after committed R1/re-entry and before successful `AdmissionAccepted` write/flush, paired with `R2TerminationDisposition = TerminatedBeforeStart` | N/A — enum disposition | N/A — exact single monotonic value for that rejected reservation generation | Never inferred/defaulted; cannot target P0–P3 or P5–P7 | Repository writes it under exact current reservation revision/fence and reads it for same-owner retry/cleanup | Exact section 10.0 P4 final code; indeterminable CAS/cleanup fails closed | `C1_early_body_after_R1_before_admission_terminalizes_exact_attempt` |
| `CurrentClaimEvaluationDisposition` | disposition | Claim-alias row | Closed set `{Active, Completed, Expired, Reclaimed, Conflict}`; `Active` is the only non-terminal member | N/A — enum disposition | Exactly one of the five values | New issuance starts `Active`; every later transition is monotonic and no unknown/default value is accepted | DB alias row is durable evidence; `begin`/broker reads it under the lookup lock | `RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED` when stale/indeterminable | `C1_claim_token_variant_result_matrix_is_exact_and_authority_precedes_result` |
| `CurrentDisposition` | disposition | Canonical source row | Closed set `{ClaimEvaluating, Reserved, Encrypting, Staged, Available, RecaptureRequired, ContentCommitmentMismatch, SourceEncryptionFailed, Quarantined, Deleted}` | N/A — enum disposition | Exactly one of the ten values | Never inferred/defaulted; transitions follow sections 9.1 and 10.0 only | Repository reads the canonical row under exact alias binding; it never trusts caller state | Row-specific section 10.0 code; unknown disposition fails closed | `C1_internal_claim_result_variants_never_cross_captureagent_final_result` |
| `C2PreparationDisposition` | disposition | C1 preparation-disposition barrier | Closed set `{Preparing, Pending, SealCommitted, AbortAuthorized, Finalized, Aborted}` with transitions exactly as sections 8.2 and 10.2 | N/A — enum disposition | Exactly one of the six values | Starts `Preparing`; monotonic transition graph only; unknown/default value forbidden | Preparation repository reads/writes the exact `(AssemblyId, C2PreparationId)` row under its disposition CAS | `RAW_EXPORT_ASSEMBLY_PREPARE_FAILED` or exact seal conflict; indeterminate seal remains `Pending` | `C1_preparation_disposition_is_exact_monotonic_and_race_safe` |
| `B4OperationalHeadState` | externally owned | Landed B4 operational head | TIP-88B4 closed set `{Claimed, Assembling, AssemblySealed, Protecting, PackageSealed, ReadyForDelivery, DeliveryInProgress, DeliveryOutcomeUnknown, Delivered, ReconciliationExpired, TerminalFailed, Cancelled, Expired}` | N/A — enum state | Exactly one of the thirteen landed values | C1 may accept `Assembling` and atomically publish only `AssemblySealed`; no other later state is C1-authorized | B4 repository reads exact state/revision/fence and the C1 seal transaction writes the one authorized target | Exact B4 fence/lease/state outcome; unknown state fails closed | `C1_stale_worker_cannot_publish_after_reclaim` |
| `R2TerminationDisposition` | disposition | C1 source-attempt row | Monotonic exact-attempt value: unset before termination, then `TerminatedBeforeStart` or `Terminated` | N/A — enum disposition | N/A — one-way monotonic transition | Unset before durable proof; never guessed/defaulted to terminated | Repository reads exact revision/fence row; truth requires committed termination CAS evidence | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` while not durable | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `R2TerminatedAtUtc` | derived | C1 source-attempt row | DB timestamp written by the exact old-revision/fence termination CAS | UTC microseconds | `[termination CAS statement start, commit]` | Absent before termination; derived once on monotonic CAS | Repository writes and later reads it with `R2TerminationDisposition` for the exact attempt | `RAW_EXPORT_SOURCE_RESERVATION_BUSY` while absent | `C1_same_owner_reentry_waits_until_previous_fenced_R2_is_terminated` |
| `TerminatedBeforeStart` | disposition | C1 source-attempt repository | `R2TerminationDisposition = TerminatedBeforeStart` after a P4 exact-attempt cleanup CAS | N/A — enum disposition | N/A — exact single monotonic value for the rejected/unavailable pre-reader attempt | Never inferred/defaulted | Repository reads the committed exact reservation revision/fence, non-arming facts and cleanup evidence after CAS | Exact P4 section 10.0 final code (`RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID` or `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`); indeterminable cleanup fails closed | `C1_early_body_after_R1_before_admission_terminalizes_exact_attempt` |
| `Available` | disposition | C1 source repository | R5-published immutable Available descriptor under sections 9.1/10.0 | N/A — durable state | N/A — one immutable descriptor per canonical source | Never inferred/defaulted | Repository and resolver know it only from committed R5 descriptor/evidence | `RAW_EXPORT_SOURCE_UNAVAILABLE` when later read cannot prove it | `C1_resolver_exposes_only_Available_descriptors` |
| `AlreadyAvailable` | disposition | C1 custody ingress | Section 10.0 replay of the same immutable Available source identity | N/A — final result variant | N/A — exact identity replay only | Never inferred/defaulted | Repository proves the Bound alias and same canonical Available descriptor at internal ExistingMatch | `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT` on different identity/admission | `C1_already_available_returns_without_admission_body_or_new_R1` |
| `ReuseDisposition` | Build-Brief-owned declaration | Homeowner D1 authority decision | `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` must enumerate the closed `AuthoritySnapshot.ReuseDisposition` set before Build-Brief dispatch | N/A — policy enum | Planning state: no value is admitted; post-gate exact closed set only | No implicit/default value; unresolved gate is unconditional STOP/RRI | Before the gate, readiness cannot activate and every attempted value is rejected; after ratification the authority evaluator persists the selected closed value | `SOURCE_RETENTION_NOT_AUTHORIZED`; unresolved/unknown value blocks dispatch/readiness | `C1_authority_is_revalidated_at_every_checkpoint` plus Build-Brief gate test |
| `ExtensionDisposition` | Build-Brief-owned declaration | Homeowner D1 authority decision | `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` must enumerate the closed `AuthoritySnapshot.ExtensionDisposition` set before Build-Brief dispatch | N/A — policy enum | Planning state: no value is admitted; post-gate exact closed set only | No implicit/default value; unresolved gate is unconditional STOP/RRI and extension is never inferred | Before the gate, readiness cannot activate and every attempted value is rejected; after ratification the lifecycle checkpoint reads the persisted closed value | `SOURCE_RETENTION_NOT_AUTHORIZED`; unresolved/unknown value blocks dispatch/readiness | `C1_authority_is_revalidated_at_every_checkpoint` plus Build-Brief gate test |
| `JobExpiresAtUtc` | externally owned | Landed B4 immutable job identity | TIP-88B4 Planning Brief field `RawExportJobIdentity.JobExpiresAtUtc` | UTC microseconds | `(job bind time, permit expiry]` | Mandatory landed field, no implicit default or extension | B4 repository persists it at bind; C1 reads the immutable job identity before prepare/seal | B4 exact expiry outcome; C1 fails `RAW_EXPORT_AUTHORITY_INVALID` | `C1_job_expiry_prevents_new_seal_without_rekeying_existing_attempt` |
| `TerminalDisposition` | predicate | C1 source repository | Any section 10.0 outcome explicitly marked terminal; excludes `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` | Boolean | `{false,true}` | False unless the exact durable terminal row exists | Repository determines it from the committed canonical disposition/evidence at replay | Fail closed to the row-specific stable code; unknown token is rejected | `C1_phase_outcome_table_is_exhaustive_and_non_overlapping` |
| `RetryableTemporaryUnavailable` | disposition | Custody ingress/source repository | Exact `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE` P4/P5 row in section 10.0 | N/A — non-terminal disposition | N/A — durable attempt result but not replay-stable terminal | Never treated as terminal/defaulted | Repository reads exact attempt cleanup/termination state; next invocation re-evaluates the registered three-way retry rule | Busy, same-owner re-entry, or `RECAPTURE_REQUIRED` exactly as section 10.0 | `C1_incomplete_transport_and_clean_content_mismatch_are_distinct` |
| `ContinuationBudgetSufficient` | predicate | Custody server post-wait projector | `ProjectionTimestamp + ServerPostWaitContinuation` is strictly before both effective producer expiry and server continuation cap | Boolean | `{false,true}` | False on equality, overflow or indeterminacy | Server computes from registered values after any wait at first attempt/re-entry/reclaim; CaptureAgent separately evaluates `ClientPreWaitContinuation` before waiting | `RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID` server-side; `RECAPTURE_REQUIRED` client-side | `C1_plaintext_and_reconciliation_time_bounds_are_complete` |
| `ProducerBufferAvailable` | predicate | CaptureAgent | Original unchanged accepted-artifact buffer exists and its monotonic retention deadline has not elapsed | Boolean | `{false,true}` | False after disposal/loss/expiry or if indeterminable | CaptureAgent alone knows from buffer ownership plus monotonic lifetime immediately before retry send | `RECAPTURE_REQUIRED` | `C1_incomplete_transport_and_clean_content_mismatch_are_distinct` |

`C1-BB-RETRY-COUNT-ACCOUNTING-GATE` is a mandatory Build-Brief-owned open
item. Before dispatch, one ratified answer must pin all nine questions:

1. whether the first invocation is counted;
2. whether `ClaimEvaluationInProgress` and each busy outcome increment;
3. whether a transport disconnect increments;
4. whether a metadata-only final such as `AlreadyAvailable` increments;
5. whether the count is scoped to UUID, alias, canonical claim or source;
6. which durable row owns it, given that pre-R1 retries precede every source
   row and the current alias shape has no counter;
7. what resets it on terminal disposition or cleanup;
8. how a CaptureAgent restart recovers a monotonic count; and
9. what happens when agent-side and server-side counts disagree.

Until all nine answers and their discriminating test are ratified, the proposed
`MaximumRetryCountPerIngress` is an unpinned bound. It does not provide durable
cross-process enforcement, readiness cannot activate it, and unresolved status
fails the Build-Brief gate rather than silently choosing semantics.

#### 15.1.1 Full normative-symbol population and register diff

The population rule includes every named configuration, derived quantity,
predicate or disposition that this brief calls normative, readiness-pinned,
bounded or mandatory; every operand/case in a readiness relation or runtime
projection; and every phase CAS target/disposition not already represented by
an explicitly closed parent row. Stable final outcome codes remain exhaustively
owned by section 10.0 and are represented here by the predicates/disposition
rows that consume them; ordinary DTO/schema fields remain owned by their exact
shape manifests. A parent disposition row counts only when its `Source` or
`Inclusive range` cell enumerates the complete set; an open cross-reference
does not count.

The following marker-delimited, ordinally sorted block is the complete
population input. The mechanical audit compares it with the first column of the
register above.

```text
NORMATIVE_SYMBOL_POPULATION_BEGIN
AbsoluteSourceExpiresAtUtc
AdmissionAccepted
AdmissionProtocolRejected
AllowedProducerClockSkew
AlreadyAvailable
Available
B4OperationalHeadState
BusyRetryBackoff
C2PreparationDisposition
CapacityAdmissionExpiresAtUtc
CapacityAdmissionStatementTimestamp
ClaimComparisonBudget
ClaimEvaluationExpiresAtUtc
ClaimEvaluationIssuedAtUtc
ClaimEvaluationTokenTtl
ClientPreWaitContinuation
CompleteStatementTimestamp
CompleteTransactionBudget
ConfiguredMaximumPlaintextRetentionBudget
ConfiguredMaximumRemainingContinuationWindow
ContinuationBudgetSufficient
ContinuationLatestStartUtc
CurrentClaimEvaluationDisposition
CurrentDisposition
CurrentTokenExpiresAtUtc
CurrentTokenIssuedAtUtc
DispositionEnvelope
DispositionExpiresAtUtc
DispositionStartedAtUtc
EarliestUnboundShellCleanupUtc
EffectivePlaintextRetentionExpiresAtUtc
EncryptionAttemptDeadline
ExpiredOwnerProducerReclaimReady
ExpiredOwnerReadinessWait
ExpiredOwnerReclaimPendingTermination
ExtensionDisposition
FreshFirstAttempt
IdempotencyLockTimeout
JobExpiresAtUtc
LatestIssuedTokenExpiresAtUtc
MaximumActiveAttemptsPerSource
MaximumChipDg2PortraitBytes
MaximumKeyMaterialRecordsPerSource
MaximumLiveSelfieImageBytes
MaximumProvisionalObjectsPerSource
MaximumRetryCountPerIngress
OwnershipLeaseDuration
OwnershipLeaseExpiresAtUtc
P4Boundary
P5Boundary
PlaintextRetentionBudgetSeconds
PlaintextRetentionExpiresAtUtc
PlaintextRetentionStartedAtUtc
PreviousFencedR2Terminated
PreviousR2TerminationBudget
ProducerBufferAvailable
ProjectionTimestamp
ProviderAbortOrFinalizeBudget
ProviderAbortOrInspectionBudget
ProviderWriterQuiescenceBudget
R1TransactionTimestamp
R2TerminatedAtUtc
R2TerminationDisposition
RawExportCaptureMaximumAggregatePlaintextBytesPerHost
RawExportCaptureMaximumConcurrentRetainedBuffersPerHost
RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment
RawExportCustodyMaximumConcurrentStreamsPerDeployment
RawExportCustodyMaximumConcurrentStreamsPerProducer
RawExportCustodyMaximumPlaintextWindowBytesPerStream
RawExportIngressMaximumPreAdmissionBufferedBytes
ReaderQuiescenceBudget
ReadinessProjectionPrecedingWait
ReadinessWorstCaseContinuation
ReclaimExecutionBudget
ReconcilerMinimumAge
ReconciliationBudget
ReservationExpiresAtUtc
RetryNotBeforeUtc
RetryableTemporaryUnavailable
ReuseDisposition
SafetyMargin
SameOwnerReentryReady
SameOwnerRetryPendingTermination
ServerMaximumContinuationExpiresAtUtc
ServerPostWaitContinuation
TerminalDisposition
TerminatedBeforeStart
TerminationCasBudget
TokenExpiresAtUtc
TokenIssuedAtUtc
TombstoneSafetyMargin
NORMATIVE_SYMBOL_POPULATION_END
```

The v0.17 audit result is:

```text
POPULATION_COUNT=91
REGISTER_COUNT=91
MISSING_FROM_REGISTER=0
EXTRA_IN_REGISTER=0
DUPLICATE_POPULATION=0
DUPLICATE_REGISTER=0
REGISTER_ROWS_WITH_NOT_10_COLUMNS=0
REGISTER_ROWS_WITH_BLANK_COLUMNS=0
```

Every row is total under this table-wide rule:

- configuration input maps to one in-range checked value or its exact failure
  code; no missing/malformed/overflow value becomes a default;
- externally owned input maps to the exact required field/value or its exact
  failure; an optional state explicitly enumerates both absent and present;
- a derived row evaluates every source case shown in its `Source` cell, and
  missing input/overflow/indeterminacy maps to its failure code;
- a predicate returns exactly true or false; unprovable evidence follows its
  false/failure rule rather than becoming a third value;
- a disposition accepts only the row's enumerated set (including a closed
  parent set); every unknown value fails closed.
- a `Build-Brief-owned declaration` is total in planning state by admitting no
  value and unconditionally stopping dispatch/readiness until its named
  Homeowner gate replaces that state with a closed enumerated set.

The bundle publishes the population list, independently extracted register list
and both set differences. A reviewer reruns the diff rather than trusting these
counts.

#### 15.1.2 Reachable continuation paths and derived readiness relations

The configuration-time readiness relation set **is** the table below. A row is
one reachable combination of entry state, selected state case and one future
worst-case wait. The closed wait cases are no wait, one live-evaluation wait
(`ClaimEvaluationTokenTtl`), and one configured busy retry
(`BusyRetryBackoff`). Readiness includes that wait because it has not occurred
in the configuration-time projection. The five state cases are mutually
exclusive, and a pending-termination case performs no replacement CAS.

Every row must satisfy the exact bound:

```text
ExactWorstCaseSum
  < ConfiguredMaximumRemainingContinuationWindow
  <= ConfiguredMaximumPlaintextRetentionBudget
```

| Path | Entry state | Selected case | Preceding wait | Exact worst-case sum of registered symbols |
| --- | --- | --- | --- | --- |
| `RP-01` | No prior source attempt | `FreshFirstAttempt` | none | `IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-02` | No prior source attempt after live evaluation | `FreshFirstAttempt` | `ClaimEvaluationTokenTtl` | `ClaimEvaluationTokenTtl + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-03` | No prior source attempt after a busy final | `FreshFirstAttempt` | `BusyRetryBackoff` | `BusyRetryBackoff + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-04` | Same owner, prior R2 not settled | `SameOwnerRetryPendingTermination` | none | `IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-05` | Same owner after live evaluation, prior R2 not settled | `SameOwnerRetryPendingTermination` | `ClaimEvaluationTokenTtl` | `ClaimEvaluationTokenTtl + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-06` | Same owner after a busy final, prior R2 not settled | `SameOwnerRetryPendingTermination` | `BusyRetryBackoff` | `BusyRetryBackoff + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-07` | Same owner, prior R2 settled | `SameOwnerReentryReady` | none | `IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-08` | Same owner after live evaluation, prior R2 settled | `SameOwnerReentryReady` | `ClaimEvaluationTokenTtl` | `ClaimEvaluationTokenTtl + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-09` | Same owner after a busy final, prior R2 settled | `SameOwnerReentryReady` | `BusyRetryBackoff` | `BusyRetryBackoff + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-10` | Expired owner, prior R2 not settled | `ExpiredOwnerReclaimPendingTermination` | none | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-11` | Expired owner after live evaluation, prior R2 not settled | `ExpiredOwnerReclaimPendingTermination` | `ClaimEvaluationTokenTtl` | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + ClaimEvaluationTokenTtl + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-12` | Expired owner after a busy final, prior R2 not settled | `ExpiredOwnerReclaimPendingTermination` | `BusyRetryBackoff` | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + BusyRetryBackoff + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + PreviousR2TerminationBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-13` | Expired owner, prior R2 settled | `ExpiredOwnerProducerReclaimReady` | none | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-14` | Expired owner after live evaluation, prior R2 settled | `ExpiredOwnerProducerReclaimReady` | `ClaimEvaluationTokenTtl` | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + ClaimEvaluationTokenTtl + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |
| `RP-15` | Expired owner after a busy final, prior R2 settled | `ExpiredOwnerProducerReclaimReady` | `BusyRetryBackoff` | `max(OwnershipLeaseDuration, ReconcilerMinimumAge) + BusyRetryBackoff + IdempotencyLockTimeout + ClaimComparisonBudget + CompleteTransactionBudget + ReclaimExecutionBudget + EncryptionAttemptDeadline + SafetyMargin` |

The pre-v0.16 three hand-written producer-continuation groups are removed. The
validator may de-duplicate algebraically equal sums, but its path manifest must
still contain all 15 IDs so a new path cannot appear without a relation. Two
additional readiness relations remain and are justified independently rather
than being represented as continuation paths:

```text
IdempotencyLockTimeout < OwnershipLeaseDuration

DispositionEnvelope
  >= EncryptionAttemptDeadline
     + ReconciliationBudget
     + ProviderAbortOrFinalizeBudget
```

The first prevents a claim-lock wait from consuming the ownership lease. The
second proves that one already-started disposition execution can complete
before its persisted, non-resettable expiry. Neither is a substitute for an
`RP-*` row.

The three projections answer different clock questions and are never merged:

```text
ReadinessWorstCaseContinuation(path) =
  exact RP-01..RP-15 row
  // configuration time: one future worst-case wait is included

ClientPreWaitContinuation(result) =
  future wait from the authenticated result
  + max(the five exact future-operation state costs)
  // before sleeping: evaluation uses
  // max(RetryNotBeforeUtc - client trusted current time, 0);
  // each busy result uses BusyRetryBackoff; no-retry uses zero;
  // internal state does not cross CaptureAgentFinalResult

ServerPostWaitContinuation(state) =
    IdempotencyLockTimeout
  + ClaimComparisonBudget
  + CompleteTransactionBudget
  + EncryptionAttemptDeadline
  + SafetyMargin
  + (pending prior R2 ? PreviousR2TerminationBudget : 0)
  + (expired-owner state ? ReclaimExecutionBudget : 0)
  // after waiting: no preceding-wait term exists
```

The client evaluates `ClientPreWaitContinuation` before deciding to sleep and
returns `RECAPTURE_REQUIRED` if its monotonic remaining buffer lifetime cannot
cover the future wait plus future operation. The server evaluates
`ServerPostWaitContinuation` from trusted DB `ProjectionTimestamp`, which is
read after any wait. Whether the caller actually waited is irrelevant to the
server formula: actual remaining lifetime already reflects all elapsed time.
No evaluation TTL, computed pending wait, `BusyRetryBackoff` or returned
`BusyRetryNotBeforeUtc` is added by the server. If a future Build Brief returns
`BusyRetryNotBeforeUtc` for client symmetry, it is response-only and creates no
durable row, counter or append-per-poll surface.

No conditional arm remains inside a case that presupposes its result.
Termination is counted exactly once while settlement is pending and zero after
`PreviousFencedR2Terminated` becomes durable. `TerminatedBeforeStart` is one of
the predicate's exact settling values, so a committed P4 cleanup selects a ready
case; a P5 disconnect without durable settlement selects a pending case. Reclaim
cost appears in both expired-owner cases and nowhere else.

A replacement attempt is admitted only if
`ProjectionTimestamp + ServerPostWaitContinuation` fits strictly before both
`EffectivePlaintextRetentionExpiresAtUtc` and the server continuation cap.
Equality, one-unit-below/above, overflow, missing partial settings and
individually valid but relationally invalid tuples fail closed as
`RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`. The named
`C1_server_post_wait_does_not_recharge_elapsed_busy_backoff` fixture waits the
full configured backoff, leaves enough actual lifetime for future operation,
and proves that re-adding the elapsed backoff alone turns the test red.

The named negative
`C1_readiness_rejects_evaluation_wait_then_expired_owner_reclaim_combination`
uses seconds: token TTL `40`; lock + comparison + complete `3`; previous-R2
termination `10`; encryption attempt `20`; safety `1`; ownership lease and
reconciler minimum age `40`; reclaim `30`; continuation window and maximum
plaintext retention `139`. Every pre-v0.16 hand-written relation passes
(`74 < 139`, `34 < 139`, `101 < 139`), and the v0.16 max-based `RP-11`
incorrectly passes as `104 < 139`; the corrected row is
`max(40,40) + 40 + 3 + 10 + 30 + 20 + 1 = 144 > 139`, so readiness rejects.
Removing only `ClaimEvaluationTokenTtl`, `PreviousR2TerminationBudget`, or
`ReclaimExecutionBudget` makes the fixture accept and turns the named test red.

The companion
`C1_readiness_rejects_late_token_after_lease_anchor_for_expired_owner_reclaim`
fixes R1/owner-CAS at `t=0`, lease and minimum age at `40`, and token issuance
at `t=39` with TTL `40`. The token may therefore delay the next invocation to
about `t=79`; it is sequential after the shared lease/minimum-age window, not
co-anchored with it. The old max relation still sees `104 < 139`, while the
corrected `144 > 139` rejects.

The only remaining `max` involving these timers is
`max(OwnershipLeaseDuration, ReconcilerMinimumAge)`: both durations begin at
the same exact owner-CAS DB timestamp persisted as that reservation/attempt
generation's timestamp. Token issuance has its own later
`TokenIssuedAtUtc`; busy backoff begins only after its busy final result. Those
waits share no anchor with the owner CAS and are always added sequentially.

Each `RP-*` row bounds exactly one immediately preceding evaluation or busy
wait. Repeated evaluation/busy cycles are not claimed bounded by the path table;
their global accounting depends on
`C1-BB-RETRY-COUNT-ACCOUNTING-GATE`. Until that gate pins the retry semantics,
each client pre-wait and server post-wait decision remains fail-closed against
actual remaining lifetime and degrades to `RECAPTURE_REQUIRED` rather than
overrunning the plaintext horizon.

Separate independently reachable fixtures retain the prior deletion-only
proofs: pending same-owner `80 + 70 > 120`, and settled expired-owner reclaim
`80 + 50 > 120`. A dedicated negative also proves a producer-valid lifetime
with a server continuation cap one microsecond too short is rejected before R1
with zero source/attempt/key/object residue.

The eight size/memory/concurrency keys and all section 7.2 relations are split
between two readiness owners. CaptureAgent readiness proves its local
buffer-slot/aggregate-byte configuration and never sends a dynamic capacity
assertion. Server readiness proves the class and custody stream/window keys
only. Missing, malformed, partial, out-of-range, overflowed or relationally
invalid values return `RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID` on the
owning side. Runtime declared-size failure returns
`RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED`; server
`RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE` means custody admission only. Neither
exposes configured sizes/counts, and the server never claims knowledge of
CaptureAgent host memory.

The separate mandatory
`RawExportIngressMaximumPreAdmissionBufferedBytes` transport key and every
qualified proxy/middleware hop are exact readiness inputs. Missing/out-of-range
ceiling, body prebuffering, request buffering, disk spill, inability to preserve
the metadata/admission/body phase boundary or an unqualified hop returns
`RAW_EXPORT_INGRESS_TRANSPORT_POSTURE_INVALID`. Runtime early body transmission
returns `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`.

Production CaptureAgent readiness validates the ratified host-posture evidence.
Missing or unverifiable mandatory memory/swap/hibernation/dump/debugger/
identity/hardening controls returns
`RAW_EXPORT_CAPTURE_HOST_POSTURE_INVALID`. Where the OS cannot guarantee a
control, explicit Homeowner/security residual-risk acceptance is required.

Production custody-reconciliation readiness independently validates the
reconciliation host, configured maximum plaintext chunk/key lifetimes,
graceful zeroization/disposal instrumentation and the same applicable memory/
swap/hibernation/dump/debugger/identity/hardening classes. It must not inherit
or reuse a CaptureAgent-only verdict. Missing or unverifiable mandatory evidence
returns `RAW_EXPORT_CUSTODY_RECONCILIATION_HOST_POSTURE_INVALID`; unsupported
controls require explicit Homeowner/security residual-risk acceptance.

Readiness responses expose only stable codes and comply with the complete
section 6.2 prohibited-content contract.

### 15.2 v0.17 self-auditing planning stopping criteria

These checks close this authoring round only. They do not ratify the Planning
Brief, authorize a Build Brief or prove runtime/readiness capability.

| Property | Mechanical evidence | Result |
| --- | --- | --- |
| P1 — section 10.0 completeness | The phase table contains `36` stable-outcome rows, every row has exactly `7` cells, and `BLANK_CELL_COUNT=0` | PASS |
| P2 — normative symbols | Population `91`; register `91`; missing `0`; extra `0`; duplicates `0`; wrong-column rows `0`; blank columns `0`; the table-wide totality rule covers every kind; disposition parents and retry-count accounting admit no value behind their named Build-Brief gates | PASS |
| P3 — projection/readiness bijection | Reachable path manifest `RP-01`–`RP-15`; producer-continuation relation rows `15`; missing path relations `0`; extra producer-continuation relations `0`; the two non-projection relations are named and independently justified | PASS |
| P4 — mutation reachability | `52` Invariant Trace rows and `53` Test-Bite rows re-audited. Every mandated behavioral/source mutation has an independently selectable fixture. The Available-only frozen-job behavior remains explicitly N/A because no supported job can freeze a pre-Available source; its source/architecture mutation is still executable and must turn the named test red | PASS |

The projection reachability sweep specifically selects:

- pending same-owner with termination false and no replacement CAS;
- ready same-owner after exact `Terminated`;
- ready same-owner after exact P4 `TerminatedBeforeStart`;
- pending expired-owner with termination false and reclaim not yet executed;
- ready expired-owner with termination true and reclaim cost;
- each of those applicable cases after no wait, evaluation wait and busy
  backoff.

No neighboring earlier guard fixes the target condition in these fixtures. The
corrected `RP-11` and late-token tests isolate sequential evaluation wait +
prior termination + reclaim; the already-waited busy fixture isolates absence
of a server wait charge; the pending 80+70 test isolates termination; the ready
expired-owner 80+50 test isolates reclaim; P4 independently selects the second
settling disposition. Removing the redundant per-claim attempt limit leaves the
per-source/key/object fixtures independently selectable and creates no new
unreachable conditional or mutation.
Removing any one named mechanism changes the expected decision and turns its
named test red. Structural exclusions are reported as N/A and never counted as
green behavioral coverage.

## 16. Acceptance gates before C1 can close

1. D1 durable-custody sub-decisions and D3–D9 are ratified; D2 remains the
   exact two-class still-image subset; the already
   ratified D1 mode direction is not misrepresented as full D1 closure.
2. Independent PI-TAG-001 review converges with zero actionable findings.
3. Exact TagEkyc and CaptureAgent allowlists ratified in a build brief.
4. Source producer proves exact supported class bytes and claims; custody proves
   class-specific keyed commitment without persisting a bare digest.
5. The new DG2 capture-artifact identity is distinct from aggregate NFC
   evidence and round-trips exact bytes/digest.
6. S3 adapter passes the complete provider conformance suite against pinned
   MinIO.
7. Every predicate-complete R1–R6 provider/metadata crash window converges to
   one available exact object, a typed recapture/hold state, or zero residue,
   using executable ingress idempotency, complete R1 cryptographic context,
   fenced ownership, and distinct source-reservation/attempt/staged
   fingerprints.
8. Application-layer encryption and key-envelope vectors are independently
   reproducible.
9. Authority is revalidated at all R1–R6/read/reuse boundaries, immediately
   before C2 Prepare, and freshly inside Seal.
10. Every stale fence fails assembly publication and C2 finalization.
11. Exact preparation `Pending → SealCommitted`, assembly identity, complete
    item set, B4 head CAS, and `AssemblySealed` transition are five surfaces in
    one atomic transaction; splitting any one independently turns its named
    mutation RED.
12. Every prepare/seal/finalize/abort window is idempotent; exact
    deterministic `C2PreparationId` is registered `Preparing` before external
    I/O, and its disposition CAS prevents unregistered orphans,
    cross-preparation finalization and scan-only abort, including
    `SealOutcomeUnknown`.
13. Seal items equal the immutable B4 ordered job-class set exactly; missing,
    extra, substituted, duplicated, or reordered items fail without residue.
14. Post-seal authority loss cannot fork C2 custody: the exact referenced
    preparation converges to finalized custody while later delivery remains
    unauthorized.
15. Every graceful success/fault/cancellation/retry/reclaim path executes
    verified zeroization and cleanup. Abrupt loss claims no active erasure,
    returns recapture when needed, and is covered by host-posture evidence.
16. PostgreSQL contains no raw/ciphertext byte surface.
17. Assembly canonicalization and authentication have independent golden
    vectors.
18. Source encryption, assembly signing, and C2 verification have independent
    readiness manifests and key roles.
19. Missing/invalid/unknown provider, key, producer, class, or C2 capability
    fails readiness closed.
20. The HLD, LLD, and debt registry carry D9's exact `GOV-001`/`ART-001` through
    `ART-009` dispositions before ratification/build dispatch.
21. Provider-specific fixture evidence has a reviewed composite authorization
    carrying the applicable TIP-38 through TIP-46 storage, resolution, orphan,
    retention, purge, hold-conflict, access/audit/security, and raw-payload
    fields; no fixture disposition closes a real-artifact gate.
22. Apply/rollback/reapply, catalog, ACL, role, snapshot, build, and full-suite
    gates pass with real counts.
23. Production provider remains disabled until C2 and production qualification
    gates close.
24. Cross-attempt golden vectors prove the exact stable-versus-attempt-scoped
    derivation table, including C2 retry/conflict behavior.
25. An independent cross-language implementation reproduces lowercase GUID
    `N`, NFC text normalization, truncated six-fraction UTC microsecond timestamp, invariant
    numeric-string, exact enum, lowercase hash, ordered-array,
    deterministic-id, keyed content commitment and subject token with explicit
    public fixture-only keys, source-stable reservation fingerprint,
    attempt fingerprint, staged fingerprint, stable assembly fingerprint,
    canonical assembly stream/digest, attempt manifest, and C2 fingerprint.
    A vector starting with a non-zero seventh timestamp digit proves
    normalize → canonicalize → hash → persist/readback → exact replay.
26. Work longer than one initial lease renews and seals with the latest
    revision; periodic renewal is joined before an optional single final
    renewal; delayed/lower responses and overlap mutations fail safely.
27. No real capture is stored until D9's real-artifact gates close.
28. The additive acceptance-event/session-selection producer and schema are
    separately ratified; C1 freezes the exact server-authored `CaptureAcceptanceId +
    CaptureArtifactId + CaptureRevision` session selection before raw read;
    landed `capture_artifacts` alone is not authority; missing/conflicting
    evidence and concurrent different-source losers fail, retry never selects
    a recapture, and two independently authorized jobs may bind the same
    retained source.
29. The TIP-88C1 B4 amendment proves `AssemblySealed` is lease-ineligible,
    non-claimable, and non-reclaimable across claim/reclaim/renew/attempt/
    transition/stale-worker paths.
30. Exact committed replay compares only persisted identity/content and returns
    `ExistingMatch` after revision/lease state changed.
31. The public/general API process contains no provider credential or source
    KEK authority; ingress, exact-attempt reconciliation, committed read,
    delete/purge, token validation, HMAC and key capabilities match the complete
    least-privilege graph with no extra edge. Ingress has no direct validator,
    `complete` or commitment-key-registry access; only the broker resolves a
    durable token-bound selector and calls `complete`.
32. Every metadata/result/error/log/trace/metric/audit/diagnostic/crash/dead-
    letter surface passes the canonical prohibited-content test and mutation.
33. The authenticated ingress response is state-shaped and contains only
    disclosure-appropriate `SourceArtifactId`, closed outcome/state/disposition;
    conflict/busy reveals no source. Any additive field requires amendment.
34. Fixture sink state is memory-only or bounded encrypted-at-rest, purged on
    abort/finalization, non-exportable, and excluded from the complete canonical
    fixture non-claims list.
35. D9 preserves separate persistence (`ART-001`, `ART-004`–`ART-009`),
    resolver/readability (`ART-002`), and C2 completeness (`ART-003`) gates.
36. Begin/broker-complete claim new/replay/conflict/rollback/timeout races, including
    CaptureAgent instance change, prove that the NewCandidate `complete` branch
    and R1 are one atomic transaction: the result is one complete `Reserved`
    source, or no source with the exact pre-begin/New-post-begin/Existing-post-
    begin alias/canonical residue, with exact SQLSTATE/message and no disclosure. Crash immediately
    before/after commit must never expose a source lacking recovery context.
    A(K1) New, A(K2) Existing and B(K2) Conflict must prove the alternate alias
    is durably Bound/burned and cannot create a B shell/source.
37. Commitment-key rotation between begin/broker-complete replays with the
    durable internal frozen historic selector; token/alias/revision races
    restart boundedly and missing versions return through `complete` without a
    second source.
38. CaptureAgent and custody-worker crash matrices prove cross-process
    source-plaintext restart is impossible and complete ciphertext recovery is
    executable through exact provisional inspect/read, authenticated completion
    record, recoverable key reference and bounded verification.
39. Every content-commitment/subject-token key referenced by retained rows is
    present in readiness, backup/escrow, DR and retirement evidence.
40. Effective plaintext deadline and every duration relation pass
    equality/±1/overflow/partial/relational-invalid tests.
41. Reconciler claim predicates are false for every healthy producer operation.
42. Available-only repository architecture proves the five pre-publication
    ingress outcomes cannot have a frozen B4-source behavior; later source loss
    remains `RAW_EXPORT_SOURCE_UNAVAILABLE`.
43. Production CaptureAgent activation fails closed without ratified,
    verifiable sensitive-memory/host-posture controls or explicit residual-risk
    acceptance.
44. R1 performs no key-provider I/O under its DB transaction; the selected
    idempotent recoverable-key-reference capability reproduces the exact attempt
    key/context after process loss or readiness fails.
45. C2 crashes before, during and after Prepare converge through deterministic
    pre-registration and exact-id lookup; no preparation exists outside the
    disposition barrier.
46. Every token tamper/variant/audience/binding/consumed/expired/stale/reclaimed
    negative maps to exactly one pinned `...TOKEN_INVALID` or
    `...RESTART_REQUIRED` SQLSTATE/message, retry and residue contract.
47. For both New and Existing candidates, simultaneous post-begin authority
    loss plus active/historic key unavailability returns the higher-priority
    authority outcome because the typed broker result re-enters `complete`.
48. Alias table/function ACLs, token CSPRNG/digest/audience/TTL and the isolated
    broker capability graph pass exact missing-and-extra readiness mutations.
49. Content-free v2 `ProducerClaimEnvelopeFingerprint` binds every normalized
    non-content producer claim to the exact ingress identity before token
    issuance; broker-owned `complete` rejects v2-envelope/cross-lineage mismatch
    as token invalid and recomputes `AdmissionFingerprint`; claimed digest
    remains exclusively behind the keyed commitment and R3 verification.
50. Evaluation issue/expiry is byte-exact with token issue/expiry; live current
    slots are never replaced; every same-token retry requires unchanged full
    lineage; response-loss/concurrent begin returns the exact evaluation-in-
    progress variant and retry boundary, never lock Busy; expired/terminal/
    reclaimed and Bound-alias reissue uses a fresh non-reused slot and monotonic
    horizon.
51. Active/historic key-unavailable outcomes are selected solely by token
    variant, including the Existing-stored-version-equals-active positive
    control; a New mismatch never becomes business fingerprint conflict.
52. Reconciliation bounded-decrypt tests prove one configured plaintext chunk
    and one unwrapped attempt key at a time, zeroization/disposal on every
    graceful exit, honest abrupt-loss semantics and independent custody-host
    readiness.
53. No persisted ingress/descriptor artifact is a candidate-plaintext
    membership verifier without a protected key; putting digest/content back in
    envelope v2 or adding a third unkeyed verifier turns the named test red.
54. Every claim-token budget/margin and server-continuation configuration has
    the exact owner/unit/range/no-default/runtime enforcement; overflow,
    partial, equality, lost-response TTL-term and maximum-cleanup-horizon
    mutations fail readiness.
55. Producer-valid but server-cap-insufficient lifetime fails inside `complete`
    before R1 with zero source/attempt/key/object; effective-deadline min-side
    and equality/±1 tests bite.
56. `InternalClaimResult` and `CaptureAgentFinalResult` have distinct exhaustive
    shapes and mappings. NewReservation/ExistingMatch/ReservationReclaimed,
    revision and fence never egress; every final code maps exactly. Every
    `C1HashCanonical` vector remains independently LP-framed and landed JCS
    vectors remain byte-identical.
57. Section 10.0 exhaustively maps every stable code to P0–P7 or an explicit
    structural exclusion. Metadata-only outcomes emit no `AdmissionAccepted`,
    request/accept no body and create no new R1/attempt/key/object for the call.
    Only committed New/re-entry progression emits admission and receives body.
    Real proxy tests prove P0–P3 and P4 early body/prebuffer/spill fail closed
    with distinct exact residue, bounded kernel/TLS residual is memory-only,
    and no upload-session/part/resume/public-complete path exists. P4/P5
    disconnect maps to non-terminal/non-replay-stable temporary-unavailable with
    phase-specific cleanup; the next same-UUID invocation selects busy,
    same-owner re-entry or recapture from durable termination, registered budget
    and buffer evidence. Clean shorter/mismatched completion is terminal.
58. The exact eight-key physical-host capacity manifest passes equality/±1,
    partial, overflow, relational, understated-stream and host-local release
    tests. Agent capacity is purely local and never reaches the server; every
    remaining server fixture is custody-only and independently reachable.
59. D2 admits exactly `ChipDg2Portrait` and `LiveSelfieImage`; LivenessMedia and
    every other class fail before source read. Encryption ChunkSize never
    becomes a transport part.
60. Same-owner retry after a dead call retains the owner marker, proves exact
    prior reader/provider-writer termination, CAS-matches current reservation
    revision/fence, then creates a new monotonic attempt/fence. Different owner
    and not-yet-terminated cases remain reservation busy.
61. Four bounded no-default termination components form
    `PreviousR2TerminationBudget`. Readiness is derived from all 15 reachable
    state × preceding-wait paths. Pending same-owner/expired-owner cases count
    termination exactly once without starting a replacement; ready cases omit
    it. Both expired-owner cases add `ReclaimExecutionBudget`.
    `PreviousFencedR2Terminated` accepts exact-fence `Terminated` or P4
    `TerminatedBeforeStart`. The combined 40+3+10+30+20+1 > 103, pending
    80+70 > 120 and settled-reclaim 80+50 > 120 negatives and their
    deletion-only mutations turn the named time-bounds tests red.
62. `AlreadyAvailable`, live evaluation, fingerprint conflict, both busy
    classes and retention-authority failure return metadata-only finals with
    zero admission/body; no new R1/attempt/key/object is created for those
    calls.
63. The section 15.1 population/register audit reports 91/91 symbols, zero
    missing/extra/duplicate entries, ten populated columns per row and a total
    function or closed value set for every row.
64. Three exact readiness-pinned source/key/object limits and one common
    bounded busy backoff have no implicit defaults and independently bind.
    Retry-count accounting is explicitly unpinned and blocks Build-Brief
    dispatch at `C1-BB-RETRY-COUNT-ACCOUNTING-GATE`.
65. `DispositionStartedAtUtc` and `DispositionExpiresAtUtc` are written once by
    the first exact disposition-required CAS, observed by the reconciler and
    never reset by retry, reclaim, restart or rotation.

## 17. STOP/RRI

STOP/RRI if:

- implementation begins from this draft;
- any D1 durable-custody sub-decision or D3–D9 remains unresolved at
  build-brief drafting/dispatch;
- CaptureAgent changes are needed without explicit cross-repository authority;
- a provider-specific type enters Domain/Application contracts;
- `latest`, root credentials, a public bucket, or shared SignFlow credentials
  are proposed as acceptance evidence;
- any section 6.2 prohibited value reaches a non-allowlisted metadata,
  result, error, log, exception, trace, baggage, metric, audit, diagnostic,
  crash-dump, or dead-letter surface;
- any provider credential or source KEK authority is loaded or registered in
  the public/general API process;
- B2 consent is treated as retroactive capture-time retention authority;
- ingress idempotency lacks the exact UUID grammar, both lookup edges,
  begin/broker-complete token/alias/revision protocol, instance-restart convergence,
  recomputes server-frozen values on replay, waits unboundedly, or discloses an
  existing source on conflict/busy;
- a bare claimed/actual Raw BIO digest is persisted, indexed, logged, returned,
  audited, or placed in any non-ephemeral preimage surface;
- producer-envelope v2 contains a digest/content-derived value, is not server-
  canonicalized and bound before token issue, changes a v2 field under one
  current token, is exposed outside its restricted alias/token surface, or
  `complete` trusts a supplied final `AdmissionFingerprint`;
- any persisted ingress/descriptor artifact permits database-plus-candidate-
  plaintext membership confirmation without a protected key;
- evaluation issue/expiry diverges from token issue/expiry, same-token retry
  ignores alias/evaluation/owner/revision/fence, a live current slot is
  overwritten, reissue reuses an id/token/revision/fence or loses the monotonic
  latest-expiry horizon, or New lineage mismatch is represented as business
  fingerprint conflict;
- any token/budget/tombstone/server-continuation/capacity configuration is missing its
  exact owner/unit/range/no-default/runtime enforcement, has unchecked
  conversion/overflow, or permits an unbounded/early cleanup horizon;
- an existing claim is compared under the active commitment key rather than its
  stored historic version, a result is classified by current registry status
  rather than token variant, or a missing historic key creates a second source;
- authenticated principal/API-key identity does not bind the producer,
  CaptureAgent instance, and session challenge;
- an unsupported class is mapped to an aggregate or neighboring artifact,
  `LivenessMedia` is admitted in C1, or encryption ChunkSize is interpreted as
  a transport part;
- `ChipDg2Portrait` reuses the aggregate NFC artifact identity or digest;
- provider or key I/O occurs inside a DB transaction/lock, or the selected
  recoverable key-reservation reference is not idempotent and sufficient to
  reconstruct the exact attempt key/context;
- a provider object becomes resolver-readable before metadata reaches
  `Available`, or a crash state lacks deterministic reconciliation;
- `ART-002` is not closed before `Available`, resolver readability, raw read,
  or source-availability reliance;
- source-stable reservation identity contains attempt-scoped values, attempt
  identity omits exact key/object/framing context, staged identity omits the
  exact attempt or actual ciphertext integrity, or any immutable fingerprint
  mutates;
- R1 leaves recoverable key-reference/envelope/nonce/framing/object context only in memory,
  or R3 is the first durable location of essential decryption state;
- a reconciler starts/restarts R2, lacks exact provisional inspect/read and
  authenticated completion verification, competes with healthy ownership, or
  starts after the effective latest-start cutoff;
- same-owner re-entry releases the owner marker, trusts connection loss or
  cancellation without durable `PreviousFencedR2Terminated`, omits exact
  owner/identity/current revision/fence CAS, reuses an attempt/fence, or weakens
  different-owner `RAW_EXPORT_SOURCE_RESERVATION_BUSY`;
- CaptureAgent-supplied lifetime extends the configured/server cap, duration
  relations omit the encryption-attempt or one-lost-response token-TTL term,
  `complete` checks producer expiry
  instead of effective producer/server-capped expiry, R1 can commit with an
  insufficient server window, or invalid bounds fail open;
- abrupt process/host loss is described as active zeroization, or production
  host-posture controls/residual risk are absent;
- reconciliation is described as plaintext-free, materializes the full
  plaintext artifact, retains a chunk/unwrapped key past its bounded operation,
  omits graceful zeroization/disposal, or reuses a CaptureAgent-only host
  readiness verdict;
- source selection trusts landed `capture_artifacts`/`QualityState` alone,
  lacks the ratified acceptance-event/session-selection producer, is
  implicit/ambiguous, occurs after raw read, changes on a retry/reclaim, or
  inherits authority from another job;
- a stale worker can publish;
- an `AssemblySealed` job can be claimed, reclaimed, renewed, assigned a new
  attempt, or transitioned back to `Assembling`;
- authority is not refreshed before C2 Prepare and again inside Seal after
  provider/key/assembly I/O;
- seal items do not equal the immutable B4 ordered class set exactly;
- preparation `Pending → SealCommitted`, assembly identity/items, B4 head, and
  transition can partially commit;
- C2 can finalize a preparation without the exact immutable C1 seal;
- C2 id is allocated only after Prepare, a preparation lacks a durable
  `Preparing` row/exact-id lookup, `SealOutcomeUnknown` invokes abort, a
  preparation is aborted without
  `AbortAuthorized`; seal ignores `AbortAuthorized`; or target
  `C2PreparationId` equality is omitted;
- a committed seal's exact C2 preparation is aborted, replaced, or
  re-authorized after post-seal authority loss;
- `GOV-001`/`ART-001` through `ART-009`, or their required HLD/LLD/debt-registry
  amendments, are omitted;
- source encryption, assembly signing, and C2 verification are collapsed into
  one capability or reuse the same key;
- assembly identity/content and attempt manifest/preparation preimages are
  merged, left ambiguous, or contradict the v2 derivation table;
- content-commitment and subject-token keys are reused, tied to a replaceable
  host, missing from restore/DR/readiness, or retired while referenced;
- any C1 preimage uses a GUID format other than lowercase `N` or otherwise
  contradicts the exact scalar canonicalization/golden vector, or any
  `C1HashCanonical` domain/scalar/ordered-array element lacks independent LP
  framing, or the C1 codec aliases/changes landed JCS `HashCanonical`;
- a persisted/replayed timestamp is rounded, retains a seventh fractional
  digit, or is hashed before truncation to UTC microseconds;
- `JobExpiresAtUtc` is inferred from lease expiry or lacks authoritative B4
  identity/command/manifest/persistence binding;
- a reclaim changes job-stable assembly values without changed immutable source
  content, or reuses an attempt-scoped manifest/preparation fingerprint;
- external work can outlive a B4 lease without renewal supervision, renewal
  does not propagate the latest revision, periodic/final renewal overlaps, a
  stale/lower response overwrites state, or renewal can race seal;
- any non-`Available` descriptor can enter source-binding freeze, an impossible
  B4-job disposition is invented for a pre-publication outcome, or later loss
  is mislabeled instead of `RAW_EXPORT_SOURCE_UNAVAILABLE`;
- generated fixture evidence omits any canonical disposition/non-claim or is
  represented as real-artifact, production, legal/compliance, audit, security,
  readiness, performance, retention-policy, or provider-production evidence;
- plaintext is persisted to a temp file or durable assembly;
- custody ingress exposes begin/broker/complete/R1–R6 as multiple external
  calls, creates an upload session/part/receipt/resume/public-complete surface,
  or lets CaptureAgent hold a provider/storage capability;
- a section 10.0 metadata-only outcome emits `AdmissionAccepted`, requests/
  accepts body or creates a new R1/attempt/key/object; any stable code lacks one
  complete non-overlapping phase-table row or an explicit structural exclusion;
- CaptureAgent transmits a body before committed-R1 `AdmissionAccepted`, any
  proxy/middleware pre-reads, pre-buffers, request-buffers or spills the body to
  disk, the bounded memory-only kernel/TLS residual is represented as zero, or
  transport posture/early-body failure lacks its exact code; P0–P3 and P4 use
  one blanket residue, P4 deletes Bound alias/idempotency evidence, or P4 fails
  to terminalize/clean the exact attempt;
- any exact class, CaptureAgent buffer-slot/aggregate-byte, or custody
  stream-slot/window/aggregate-window bound is absent/defaulted/unbounded,
  checked arithmetic or atomic host-local admission/release is missing, an
  agent capacity assertion/attestation is invented, local agent exhaustion
  reaches the server, the server claims to enforce CaptureAgent memory, a
  declared oversize reaches begin/provider/key I/O, or an actual oversize can
  become Available;
- lock contention and a live evaluation share one indistinguishable outcome,
  ClaimEvaluationInProgress omits/misstates RetryNotBeforeUtc, or CaptureAgent
  waits after its remaining retention budget cannot cover wait + claim/
  complete/conditional previous-R2 termination/expired-owner reclaim/R2/safety;
  any of the four
  termination components is missing/defaulted/unbounded, termination is omitted
  while not durable, reclaim cost is omitted on expired-owner producer reclaim,
  or termination is counted again after durable proof;
- either D4.3 union is an open bag, InternalClaimResult and
  CaptureAgentFinalResult share one external type, NewReservation/
  ExistingMatch/ReservationReclaimed/revision/fence can egress, final
  nullability is inferred from code, an unlisted/assembly outcome is accepted,
  or retry metadata appears outside exact final
  ClaimEvaluationInProgress.RetryNotBeforeUtc;
- fixture/local/pinned-MinIO evidence is called production readiness;
- MinIO archive status is treated either as automatic rejection or automatic
  acceptance without qualification;
- the filesystem alternative is implemented without an explicit topology
  decision; or
- C1 claims recipient encryption, package custody, download, delivery, receipt,
  or real-patient readiness.

## 18. Review affected-surface maps

### 18.1 Round 1

| Finding/rule changed | Sections patched | DTO/port impact | API/error impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| Missing B4 seal operation | D6, 8.1, 8.2, 9.2, 10.2, 12, 13, 16, 17 | Adds planned seal command/result and assembly metadata | Closed seal outcomes required; no public route | One assembly/job, exact items and transition | Atomicity/fence/authority mutations |
| Impossible crash guarantee | D7, 5.1–5.6, 6, 9.1, 10.1, 11–13, 16–17 | Adds provisional/commit/abort provider semantics | Adds sanitized reconcile outcomes only | Reservation fingerprint + custody evidence | Full R1–R6 crash matrix |
| Missing pre-seal authority refresh | D6, 9.2, 10.2, 12–13, 16–17 | Seal repository owns fresh check | Exact authority/expiry precedence | No seal evidence on failure | Pause/withdraw/expire mutation |
| Missing challenge/authenticated producer binding | D4, 6–7, 9.1, 10.1, 11–13, 17 | Ingress context and descriptor gain bindings | Non-enumerating binding/replay errors | Challenge and credential identity enter fingerprint/evidence | Per-field/replay mutations |
| Missing C2 sink order | D8, 8.2, 9.2–9.3, 10.2, 11–13, 15–17 | Adds prepare/finalize/abort conceptual port | No delivery/public API | Preparation id bound into assembly identity | Distributed crash/fence matrix |
| DG2 identity ambiguity | D2, 6–7, 11–13, 16–17 | Requires new exact capture-artifact type/id | Restricted capture metadata surface only | Independent DG2 digest | Aggregate-alias mutation |
| Merged matrices/missing consumers | 10–11 | Separates ingress and assembly commands | Precedence now per reachable command | Residue mapped per command | Consumer-specific assertions |

Core scope, provider-neutral direction, public boundary, mode recommendation,
and production non-claims remain unchanged.

### 18.2 Round 2

| Finding/rule changed | Sections patched | DTO/port impact | API/error impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| Seal command missing persisted fields | D6, 8.1, 9.2, 10.2, 12–13 | Command now carries manifest version, assembly digest/authentication and exact items | No new public surface | Every persisted field has an authoritative command source | Exact row round-trip required |
| Existing replay unreachable | D6, 8.2, 9.2, 10.2, 12–13 | Existing branch precedes new-seal lease/CAS checks | Adds exact `ExistingMatch`/conflict/graph outcomes | No duplicate identity/transition | Response-loss replay mutation |
| Seal precedence conflict | D6, 9.2, 10.2, 12–13 | New-seal admission precedes fresh authority | Stable internal order only | No evidence on either failure | Simultaneous stale + withdrawal test |
| Outcome token drift | 10.1–10.2, 12–13 | No shape change | Canonical `RAW_EXPORT_SOURCE_BINDING_INVALID` and `RAW_EXPORT_SOURCE_INTEGRITY_INVALID` | Evidence/tests use same token | Stale-token sweep |

### 18.3 Round 3

| Finding/rule changed | Sections patched | DTO/port impact | API/error impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| Seal did not prove exact B4 job-class equality | D6, 9.2, 10.2, 12–13, 16–17 | `OrderedItems` is compared to immutable `raw_export_job_classes` by count, ordinal, order, and class | Adds closed `RAW_EXPORT_ASSEMBLY_CLASS_SET_MISMATCH` outcome | Prevents missing, extra, substituted, duplicated, or reordered manifest entries | Exact class-set mutation matrix |
| Post-seal authority change forked C2 recovery | D6, D8, 8.2, 9.3, 10.2, 12–13, 16–17 | Exact committed seal authorizes finalization of its referenced preparation only | Later authority loss blocks delivery; it does not undo custody convergence | One immutable seal/preparation lineage; no replacement preparation | Withdraw-after-seal mutation |
| Existing GOV/ART lifecycle governance omitted | Debt map, D9, 16–17, review plan | No runtime DTO yet; build allowlist must include exact governance synchronization | No capability claim until row-specific gates close | HLD/LLD/debt registry retain owner and disposition | Packet, fixture-only, real-artifact, and STOP gates |
| Source encryption and assembly authentication conflated | 7, 9.2, 13, 15–17 | Independent source-key, assembly-signer, and C2-verifier manifests | Readiness fails on missing/revoked/window/mismatch | Historical seal verification survives rotation without KEK/DEK reuse | Independent readiness/key-role mutation |

### 18.4 Round 4

| Finding/rule changed | Sections patched | DTO/port impact | API/error impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| D9 synchronization not executed | D9, 16–17; HLD, LLD, debt registry; TIP index | No runtime DTO; authoritative docs now name C1 ownership and boundaries | No new capability or dispatch authority | GOV/ART status remains explicit and open | Ratification/build STOP now has synchronized evidence |
| Fixture evidence omitted lifecycle fields | D9, 16–17; HLD/LLD amendment | Authorization packet must carry applicable TIP-38 through TIP-46 fields | Fixture-only dispositions cannot become real-artifact readiness | Retention/purge/hold/access/audit/orphan evidence is bounded by environment | Composite-packet and no-overclaim gates |
| Stable and attempt-scoped identities conflated | D6, 8–13, 16–17 | Exact v1 derivation table and two-layer C2 idempotency | Stable fingerprint conflict differs from attempt replay | Stable content hashes versus attempt manifest/authentication are explicit | Cross-attempt golden and mutation vectors |
| Long work omitted B4 renewal | D6, 8–10, 12–13, 16–17 | Renewal supervisor carries latest revision/expiry | Exact B4 renewal/admission failures remain closed | Every renewal appends landed B4 evidence; no C1 seal on failure | Longer-than-one-lease and stale-revision mutations |

### 18.5 Pre-v0.8 authoring cycle — Round-5 mandatory root-cause checkpoint

The pre-v0.8 cycle V5 returned one HIGH `PATCH_REGRESSION`: v0.5 called lowercase GUID `D` strings
the landed convention while the authoritative LLD and completion hash call
sites use lowercase `N`.

| Checkpoint question | Conclusion |
| --- | --- |
| Repeated finding? | No. It was introduced by the v0.5 exact-preimage patch. |
| Root-cause class | Drafter patch-local regression. The reviewer finding is evidence-backed and in scope. |
| Why earlier matrices/prompts missed it | The affected-surface map named LLD/canonicalization generally but did not enumerate scalar encodings; the drafter sweep checked labels/status/version rather than byte-format call sites. |
| Corrective drafting rule | Before adding/changing a canonical preimage, inventory every scalar representation against authoritative docs and landed call sites; name GUID/timestamp/integer/enum/hash/array rules and add an independent golden vector. Never call a convention “landed” from serializer intuition. |
| Corrective reviewer rule | Patch verification for hash/id work must compare every scalar format and domain label with both authoritative docs and real call sites, then mutate at least one representation in the planned vector. |
| Can rounds 6–10 converge in scope? | Yes. The correction is one synchronized GUID format plus vector/rule coverage; it changes no scope, state machine, provider, or authorization boundary. |

The v0.6 correction uses lowercase GUID `N`, adds the cross-language vector,
synchronizes the LLD/index/debt wording, and leaves D1–D9 unresolved.

### 18.6 Consolidated external-review authoring round

The Homeowner authorized one v0.7 patch after GPT, CC, and Codex adjudication
found distributed-saga, exact-replay, B4 compatibility, retained-source,
timestamp, process-isolation, observability, and cross-document gaps. This
round changes core invariants and therefore invalidates the corresponding
full-coverage areas under `L-TAG-Review-01`.

| Rule changed | Sections touched | DTO/port impact | Error/status impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| D1 retained-vault direction and pending sub-decisions | 3–4, 15–17 | No runtime DTO; decision aid only | No capability outcome authorized | Retention/reuse authority remains open | Build/raw/production gates remain closed |
| Reservation versus staged ciphertext identity | 5.1, 5.6, 6, 9–13, 16–17 | Two conceptual fingerprints replace one circular fingerprint | Exact replay/conflict/reconcile outcomes | Two preimages and staged integrity evidence | Temporal-saga and mutation proofs |
| Frozen per-job source selection and retained reuse | 4, 6.1, 8–13, 16–17 | Adds planned C1 job-source-binding surface | Closed none/ambiguous/unavailable/binding outcomes | Source id becomes immutable job input | Recapture/substitution and positive two-job reuse |
| Committed replay and B4 sealed-state compatibility | D6, 8–13, 16–17 | Persisted replay set separated from admission; planned B4 amendment | `ExistingMatch`; sealed non-claimable outcomes | No duplicate seal/attempt | Response-loss and sealed-reversal mutations |
| Unknown seal and finalize-wins abort | D6/D8, 8.2–13, 16–17 | Abort requires atomic committed-seal re-check | `SealOutcomeUnknown` | One committed preparation lineage | Lost-response and concurrent-abort proofs |
| Persisted scalar/assembly/C2 determinism | D6, 8–13, 16–17 | Six-fraction UTC; exact stream; deterministic preparation id | Exact conflict/equality | Non-self-referential digest and cross-language vector | Seventh-digit/rounding/name/order mutations |
| Credential, ingress-response, and prohibited surfaces | D4, 6.2, 10–13, 15–17 | v0.7 introduced a two-field baseline response and isolated process identities; v0.8 supersedes that response with the state-shaped contract in D4 | Closed sanitized outcome vocabulary | Canonical forbidden-content contract | Composition and observability mutations |
| GOV/ART and fixture parity | D9, 8.2, 13, 16–17 | No runtime surface | No phase-gate merge | Fixture evidence cannot over-claim | Complete non-claims/phase-gate sentinel |

### 18.7 v0.8 independent temporal/identity review patch

GPT, CC, and two independent Codex adjudicators confirmed that v0.7 still lacked
an executable ingress claim, recoverable R2→R3 envelope state, producer/
reconciler exclusion, authority evidence/cutoffs, pre-Prepare revalidation,
safe C2 abort arbitration, serialized renewal, exact recapture selection, and
portable review packaging.

| Root cause | Consolidated v0.8 surfaces | Closure mechanism |
| --- | --- | --- |
| Source id required to find its own replay | D4, 5.6, 9.1, 10.1, 11–13 | UUID claim key, full identity/admission fingerprints, unique claim/source edges and bounded race outcomes |
| R2 output could outlive its only decryption context | 5.6, 6, 9.1, 12–13 | Complete wrapped-key/framing/nonce/object context in R1; R3 persists only actual results |
| Reconciler could race healthy ingress or promise impossible plaintext restart | 5.6, 7, 9.1, 10.1, 12–13, 15–17 | Lease/revision/fence, producer priority, effective deadline, complete-object-only recovery and recapture |
| Digest privacy and key rotation were underspecified | D4, 5.6, 6–8, 10–17 | Dedicated versioned HMAC commitment/token, no bare digest, historic-key replay/DR/readiness |
| Authority existed only as expiry and was checked too late | D1/D5, 5.6, 8.2, 9–13, 16–17 | Immutable snapshot plus R1/R2/R3/R5/read/reuse/pre-Prepare/Seal/purge/hold checks |
| Abort/finalize was not atomically scoped to target preparation | 8.1–8.2, 9.2, 10.2, 11–13, 16–17 | Exact disposition barrier and target-id equality; deadline is eligibility only |
| Retained recapture and renewal races were open | 6.1, 8.2, 9.2–9.4, 10–13 | Accepted-session artifact/revision freeze; same/different loser outcomes; single-flight renewal |
| Zeroization and packaging evidence overclaimed | 5.6, 7, 15–17; bundle artifacts | Graceful/abrupt distinction, host posture; exact POSIX 14-entry manifest and corrected closure audit |

The earlier CC example in which abort candidate A observes seal B was overstated
as a normative defect because v0.7 elsewhere required exact referenced
preparation. v0.8 nevertheless removes the local ambiguity and adds the missing
cross-boundary disposition CAS. Multiple-candidate fail-closed behavior was
safe but operationally incomplete; v0.8 adds the trusted accepted-evidence
discriminator without reopening first/latest selection.

### 18.8 v0.8 V1 independent-review correction

Two independent reviewers read the complete authority and five synchronized
documents. The temporal reviewer returned four HIGH and three MEDIUM findings;
the identity/authority reviewer returned three HIGH and two MEDIUM findings.
Packaging absence was a not-yet-executed gate, not a content defect.

| Finding/rule changed | Sections patched | DTO/port impact | Error/status impact | Hash/audit impact | Test/STOP impact |
| --- | --- | --- | --- | --- | --- |
| Source fingerprint mixed replacement-attempt fields | 5.1, 5.6, 6, 9–13, 16–17; LLD/HLD | Adds exact `EncryptionAttemptFingerprint`; provider provisional operations use it | No public outcome change | Source/attempt/staged preimages are separately immutable | Three-vector scope mutations |
| R1 key operation conflicted with transaction boundary | D4, 5.6, 6, 9, 12–17; LLD/HLD | Selects idempotent recoverable `AttemptKeyReservationId`; no key I/O in R1 | Incompatible provider fails readiness | No essential secret only in memory; no orphan key side effect | Key-reference idempotency/crash mutation |
| Complete R2 recovery and capability ownership were not executable | D4, 5, 5.6, 9, 12–17; LLD/HLD | Adds exact-attempt provisional inspect/read and scoped reconcile verify/unwrap | Existing recovery outcomes retained | Authenticated completion record carries no bare digest | Crash-after-complete-R2 and capability-edge mutations |
| Accepted source had no landed authority | D1, D4, 6.1, 9–13, 16–17; LLD/HLD/debt/index | Adds required server-authored acceptance-event/session-selection dependency | Selection none/ambiguous/conflict remain closed | Exact acceptance/artifact/revision enters mapping | Landed-QualityState negative and producer/allowlist STOP |
| CaptureAgent instance in the sole unique edge allowed duplicate source after restart | D4, 5.6, 6, 9–13, 16–17; LLD/HLD | Keeps instance as authenticated evidence and adds an instance-independent exact-artifact edge | Same source exact-matches or conflicts; never duplicates | Exact artifact/revision remains one logical source across process instance change | Changed-instance replay, second-edge removal and race mutations |
| Historic replay could race rotation | D4, 5.6, 9–13, 15–17; LLD/HLD | Splits bounded `begin`/external HMAC/`complete` with opaque token/revision and grants ingress only token-bound historic comparison | Exact busy/conflict/historic-key outcomes | Selector freezes before actual key-byte resolution without general historic enumeration | Rotate between begin/complete plus token/capability mutations |
| HMAC formula named version as key | 5.6, 8; LLD/HLD | Resolves actual key bytes by id/version | Missing version remains fail-closed | Formula no longer conflates selector with secret | Golden/missing-key vectors |
| C2 Prepare could create an unregistered orphan | D6, 8–13, 16–17; LLD/HLD | Deterministic id, `Preparing` before I/O, exact lookup, in-Seal disposition CAS | Lost Prepare response is replay/lookup, never scan | One preparation lineage exists before custody | Crash before/during/after Prepare and atomic-seal mutations |
| Authority test and outcome unions were categorical | 10–13, 16–17 | Exact response variants; split pre/post-Prepare proof | Five outcomes pin types/SQL/fields/retry/residue/evidence | Busy events explicitly append none; sensitive fields omitted | Two non-vacuous authority tests and ten-surface outcome proofs |

### 18.9 v0.8 V3 independent-review correction

The free adversarial V3 review found one HIGH patch regression: `complete`
returned a `NewReservation` before a separately described R1 allocated and
persisted the same source. The correction makes the NewCandidate branch of
`complete_raw_export_source_ingress_claim` the single short R1 transaction.
`NewReservation` is returned only after source/reservation/attempt/fence/key-
reservation/provisional-object/recovery context and authority commit together.
New `begin` may leave one Evaluating alias plus a bounded non-source
`ClaimEvaluating` shell; Existing-alternate `begin` may leave one Evaluating
alias plus the unchanged canonical claim. Busy residue and crash tests
distinguish pre-begin, both post-begin/pre-R1 shapes, and atomic R1 commit
boundaries. HLD, LLD, debt, index, ordering, outcome, invariant and acceptance
surfaces are synchronized.

This patch changed core lifecycle, port, key, identity and C2 invariants, so it
invalidates the corresponding full-coverage areas under `L-TAG-Review-01`.
v0.8-V4 verified the V3 atomicity correction and found the follow-on identity,
authority and test-bite gaps recorded below. v0.8-V5 performed the separate
consolidated-cycle checkpoint recorded in section 18.11. No implementation or
Build Brief authority results.

### 18.10 v0.8 V4 identity/temporal correction

| Finding/rule changed | Sections patched | Closure mechanism | Required proof |
| --- | --- | --- | --- |
| ExistingCandidate could not be both single-use-token and read-only | D4, 5.6, 9, 12–13, 16; HLD/LLD/debt/index | Distinct single-use shell-backed New token and idempotent read-only Existing comparison token; no second row on either unique edge | Existing-token replay and no-mutation catalog/state proof |
| Authority could change during external HMAC | D4/D5, 5.6, 9–13, 16; HLD/LLD | Fresh in-transaction actor/session/acceptance/retention revalidation before disclosure/comparison/allocation | Pause after begin, withdraw/change authority, then require closed result and zero source |
| Stale token could target a replacement shell | D4, 5.6, 12–13, 16; HLD/LLD | The then-current issuance is immutable while live; replacement/reclaim uses a fresh evaluation/token current-slot CAS with monotonic non-reused revision/fence and safe cleanup horizon | Expired-owner/stale-complete race |
| Atomic complete/R1 acceptance was not mutation-proven | 12–13, 16 | Dedicated named pre-commit fault and split/early-return mutation | `C1_complete_returns_source_only_after_atomic_R1_context_commit` RED |
| Busy summary overclaimed zero durable state | 10.1 | Distinguishes pre-begin zero from New alias+shell and Existing alternate-alias+canonical residue | All three timeout phases |
| Review posture was stale | 18.9–19 | Records v0.8-V4 findings and requires a cycle-qualified checkpoint | Wording/version/status sweep |

### 18.11 v0.8-V5 mandatory root-cause checkpoint

v0.8-V5 did not converge. The findings were evidence-backed and in scope; no
reviewer finding was discarded as invented.

| Checkpoint question | Conclusion |
| --- | --- |
| Repeated finding? | No exact repeat. Each arose after a boundary-changing correction exposed an untraced later state/failure. |
| Root-cause class | Drafter lifecycle-coverage gap plus patch regressions: pairwise identity tests omitted future alias reuse; external dependency failure bypassed the new authority arbiter; stateful token negatives lacked closed outcomes; review labels were not cycle-qualified. |
| Why earlier rounds missed it | Reviews traced the motivating success/crash pair but not the full state × token variant × future reuse × simultaneous-failure product. |
| Corrective drafting rule | Every accepted alternate idempotency identity gets a durable alias/tombstone and later-conflict trace; every external dependency result re-enters the authoritative arbiter; every negative test maps one-to-one to outcome/precedence/residue/retry; every stateful token pins issuer/verifier/randomness/digest/audience/expiry/replay/readiness; every review label is cycle-qualified. |
| Corrective reviewer rule | After any boundary-changing patch, review success, failure, stale/replay, capability, simultaneous adjacent-precedence and future-reuse sequences—not only the motivating path. |
| Can one more round converge in scope? | Yes. The v0.8-V5 patch remains docs-only and adds alias/token/arbiter planning surfaces without implementation or provider/raw operation. Per Homeowner direction, v0.8-V6 is the final internal review round; findings after it are reported for external CC/GPT review, not followed by v0.8-V7. |

### 18.12 v0.8-V6 final internal review and external handoff

The final internal V6 review found four evidence-backed contract clusters. They
were patched once under the Homeowner's limit and were not subjected to an
internal V7:

| Final V6 finding | Applied correction | Status |
| --- | --- | --- |
| Broker-derived result was not authenticated across the ingress boundary and selector wording leaked through that boundary | Broker alone validates, derives and invokes broker-only `complete` in one operation; ingress can call only `begin`, sees neither selector nor provisional result, and direct ingress completion is a capability/readiness failure | **PATCHED — NOT INTERNALLY RE-REVIEWED** |
| Token TTL had no exact configuration, clock, horizon or budget contract | Mandatory integer `RawExportSourceClaimEvaluationTokenTtlSeconds` in 1–300; DB UTC-microsecond issuance; expiry/evaluation/budget/cleanup relations and boundary tests pinned | **PATCHED — NOT INTERNALLY RE-REVIEWED** |
| Busy/authority/crash residue omitted the durable alias row | Every surface now distinguishes pre-begin zero, New Evaluating alias + non-source shell, and Existing Evaluating alternate alias + unchanged canonical claim | **PATCHED — NOT INTERNALLY RE-REVIEWED** |
| Token variant × provisional-result retry/consume/transition rules were incomplete | Added the exact seven-row matrix, fresh-authority rule and named cross-product mutation proof | **PATCHED — NOT INTERNALLY RE-REVIEWED** |

Root cause: the prior patch traced components but not the authenticated
producer→consumer edge, exact clock/configuration contract, or the full
token-variant × result × state × retry product. Reusable drafting rule: every
cross-boundary intermediate must either be authenticated to its consumer or
remain inside one authority boundary; every expiring capability must pin
issuer clock, codec, bounds, budget and cleanup horizon; every failure row must
enumerate all durable identities it preserves; every tagged union must have an
exhaustive transition/retry/residue matrix. Reusable reviewer rule: mutate
capability direction, result provenance, exact time boundaries and every union
cross-pair independently.

The Homeowner limited internal review to V6. There is no V7. External CC/GPT
review is required, this document does not claim PASS, and the status remains
**PLANNING PATCHED — INDEPENDENT REVIEW REQUIRED — IMPLEMENTATION BLOCKED**.

### 18.13 v0.9 external CC/GPT correction and handoff

The first external CC/GPT review of the v0.8 bundle verified its SHA-256,
14-entry portable manifest and five-file scope, then returned eight unique
actionable contract gaps after overlap consolidation. The Homeowner authorized
one patch round, not another internal review. v0.9 applies the verified
corrections and returns directly to independent external review:

| External finding | Affected surfaces | Applied v0.9 correction | Status |
| --- | --- | --- | --- |
| Broker admission omitted normalized producer claims and immutable provenance | D4, 5.6.1, 6/6.2, 10, 12–13, 16–17; HLD/LLD/debt/index | Server canonicalizer persists a restricted session-scoped producer-envelope fingerprint; broker returns the full normalized provisional shape; `complete` verifies the envelope and recomputes admission; bare digest and trusted final fingerprint remain forbidden | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Evaluation expiry had no source or upper bound | D4, 5.6, 15–17; HLD/LLD/debt/index | Evaluation issue/expiry is exactly token issue/expiry from the same DB clock and TTL; no second lease/default exists; cleanup uses that one bounded horizon | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Capability outcome erased post-begin alias/shell residue | 10.1–10.1.1, 12–13, 16; debt/index | Split pre-begin zero-residue capability failure from post-begin New active-key failure with Evaluating alias + non-source shell + unconsumed token | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Same-token retry ignored stale owner/revision/fence | D4, 10.1.1, 12–13, 16–17; HLD/LLD/debt/index | Every retry now requires unexpired token plus unchanged alias/evaluation/owner/revision/fence; reclaim requires restart | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| New mismatching admission had no persisted business comparator | D4/D4.2, 10, 12–13, 16–17; HLD/LLD/debt/index | New envelope/lineage mismatch is token invalid; only correctly bound Existing comparison against persisted canonical admission can be business conflict | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Reconciler was called plaintext-free while bounded-decrypting | 5.6.2–5.6.4, 7, 9, 12–13, 15–17; HLD/LLD/debt/index | Corrected to producer-buffer-independent; bounded one-chunk/key lifetime, graceful zeroization/disposal, honest abrupt-loss posture and separate custody-host readiness are explicit | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Active/historic failure classification depended ambiguously on current key status | D4, 10.1.1, 12–13, 15–17; HLD/LLD/debt/index | Result type is selected only by token variant; Existing remains Historic even when stored version equals active; positive/mutation controls are named | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Historic SQL code and retention/capability residue siblings were incomplete | 5.6.3, 10.1–10.1.1, 12–13, 16; LLD/debt/index | LLD pins the exact historic code; capability and retention rows are phase-exact; the prior `None or terminal claim evidence` disjunction is replaced by deterministic residue | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

CC's suggested readiness arithmetic was accepted only in principle. Token
validity and producer plaintext authority are connected at the actual
`complete` statement-time checkpoint; token TTL and the comparison/complete
budgets already contained within it are not double-counted into the post-R1
encryption window. Equality and ±1 tests cover the corrected runtime predicate.

Affected-surface audit: no public DTO/API or implementation surface is
authorized; planned alias and broker-internal shapes change; existing token,
capability, retention and historic-key codes retain their external vocabulary;
one new internal readiness code names custody-reconciliation host posture;
hash impact is the restricted producer-envelope fingerprint plus
`complete`-recomputed admission; all related shape/outcome/invariant/mutation/
STOP surfaces are synchronized. There is no internal V7 and no PASS claim.

### 18.14 v0.10 targeted external-review correction

The v0.9 bundle passed SHA, 14-entry POSIX manifest, 13/13 checksum and five-
file allowlist verification. GPT returned two HIGH and three MEDIUM findings;
CC independently confirmed the content-confirmation defect, withdrew its prior
token-versus-retention arithmetic concern and issued the targeted docs-only
v0.10 authority. Precondition 1.5 passed: no decision between `begin` and
`complete` depends on freezing the digest at begin.

| Finding/rule changed | Sections/siblings | v0.10 correction | Status |
| --- | --- | --- | --- |
| Unkeyed envelope reintroduced candidate-content confirmation | D4/D4.2, 5.6.1, 6.2, 12–13, 16–17; HLD/LLD/debt/index | Homeowner option (b): remove digest/content from envelope, bump literal domain to v2, retain v1-only verification rule, keep digest behind keyed commitment/R3 and require no unkeyed persisted membership verifier | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Token re-mint/reclaim could not coexist with immutable evaluation fields | D4, 10, 12–13, 15–17; HLD/LLD/debt/index | Stable alias plus one CAS-replaceable current slot; live-slot Busy; fresh non-reused expired/terminal/reclaim/Bound issuance; old-token restart; monotonic latest-expiry cleanup horizon | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Complete checked producer expiry but not effective server cap | 5.6.3, 10, 12–13, 15–17; HLD/LLD/debt/index | Effective min deadline is calculated and checked before R1; mandatory bounded continuation config and strict relation; producer-valid/cap-invalid negative | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Token/cleanup budgets lacked exact configuration | D4, 12–13, 15–17; HLD/LLD/debt/index | Five mandatory integer no-default keys with units/ranges/runtime owners, checked arithmetic and finite latest-issuance 600-second maximum horizon | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Closed response union omitted reachable variants | D4.3, 10, 12–13, 16–17; HLD/LLD/debt/index | Exact success/existing/reclaimed/available variants plus enumerated OutcomeOnly; no inferred nullability/retry metadata/assembly outcomes | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| C1 canonical field boundaries were implicit and the generic name collided with landed JCS | 5.6.1, 8, 12–13, 16–17; HLD/LLD/debt/index | New `C1HashCanonical` LP domain/scalar/array-count profile referenced by every C1 construction; landed Evidence-Integrity `HashCanonical` JCS remains byte-identical; collision/golden mutations cover both | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

Homeowner decision record: option (b), content-free envelope v2, is ratified.
Rejected alternative 1 was keying the envelope because it would require a
shared secret across the ingress/server-canonicalizer and isolated broker
identities separated by V6. Rejected alternative 2 was accepting the residual
unkeyed membership risk. The explicit cost of option (b) is later detection of
a digest-only change: Existing detects it at keyed comparison; New may burn one
fenced attempt and detects it at R3. No unverified source becomes Available.

No internal V7/V8 or PASS claim is created. Affected planned data/hash surfaces
are the alias current slot/latest horizon, envelope domain v2 and universal
`C1HashCanonical` framing; public API and implementation remain unauthorized.

### 18.15 v0.11 targeted transport, capacity, wait-budget and D2 correction

The v0.10 bundle passed SHA, exact 14-entry POSIX manifest, 13/13 checksum and
five-file allowlist verification. External review confirmed precondition 1.5,
content-free envelope v2, non-confirmability mutations and the independent C1
codec. One external reviewer found two blocking consequences of the new
non-overwritable live slot; the Homeowner dispatch also pinned the previously
implicit external-call topology, capacity bounds and initial class subset.

| Finding/rule changed | Sections/siblings | v0.11 correction | Status |
| --- | --- | --- | --- |
| Internal claim ceremony was mistaken for a multi-call upload protocol | D4, 7, 9.1, 11–13, 16–17; HLD/LLD/debt/index | Exactly one external metadata+bounded-stream operation; internal begin/broker/complete/R1–R6; R1-before-body; no upload session/part/resume/public complete | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Artifact size, plaintext memory and concurrency were unbounded | 7, 9.1, 10–13, 15–17; HLD/LLD/debt/index | Six mandatory bounded keys, exact relations, atomic capacity admission/release, declared-size pre-I/O rejection and stream-time actual counter/cleanup | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Lost begin result made token TTL a mandatory wait absent from retention relations | D4, 7, 12–13, 15–17; HLD/LLD/debt/index | Added one full token TTL plus claim/complete/R2/safety to the strict producer retention relation and a TTL-removal RED mutation | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Idempotency lock timeout and live evaluation used one unroutable code | D4/D4.3, 10–13, 16–17; HLD/LLD/debt/index | Kept lock-only `...IDEMPOTENCY_BUSY`; added `...CLAIM_EVALUATION_IN_PROGRESS` with exact `RetryNotBeforeUtc` and retention-aware zeroize/recapture rule | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| D2 heavy-media transport was over-broad | D2, 7, 9.1, 12–13, 16–17; HLD/LLD/debt/index | Ratified only DG2 portrait + live selfie; deferred LivenessMedia to a separately ratified resumable multipart transport; ChunkSize remains encryption-only | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

The actual byte count cannot be known before body consumption under the
ratified R1-before-body order. v0.11 therefore makes the distinction explicit:
declared-size and capacity violations fail before begin/provider/key I/O;
understated actual-size overflow fails at the first excess R2 byte, prevents
availability and triggers exact fenced cleanup. No impossible pre-read proof is
claimed.

No PASS claim, Build Brief, implementation, provider/key operation or Raw BIO
authority results from this correction.

### 18.16 v0.12 physical-capacity and body-termination correction

The v0.11 bundle passed SHA, exact 14-entry POSIX manifest, 13/13 checksum and
five-file allowlist verification. External review found that one capacity
number conflated memory on two hosts, its worst-case product relation made
aggregate exhaustion unreachable, incomplete transport was not distinguished
from clean content mismatch, C1-I04 retained a stale class count, and the
declaration-correction clause did not identify its phase.

| Finding/rule changed | Sections/siblings | v0.12 correction | Status |
| --- | --- | --- | --- |
| Producer buffer and custody plaintext window were conflated | 7.2, 9.1, 10–13, 15–17; HLD/LLD/debt/index | Model A splits an eight-key manifest by physical host; agent admission owns buffer slot/exact declared bytes, server admission owns stream slot/exact window cost; server never claims CaptureAgent-memory enforcement | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Worst-case product relation subsumed aggregate admission | 7.2, 12–13, 15–17; HLD/LLD/debt/index | Removed the product-cover predicate and required independently binding slot/aggregate fixtures plus mutation reachability review | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Incomplete body could be mistaken for terminal content mismatch | 7.1–7.2, 10, 12–13, 16–17; HLD/LLD/debt/index | Abort/cancel/incomplete maps to temporary-unavailable with fenced cleanup and eligible same-UUID retry; clean EOF mismatch is terminal and refuses same UUID | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| C1-I04 had a stale three-class vector count | 12–13; HLD/LLD/debt/index | Exact positives are DG2 portrait and live selfie; `LivenessMedia` is the unsupported negative | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Declaration correction did not identify its phase | 7.2, 10.1.1, 12–13, 16–17; HLD/LLD/debt/index | Same-UUID correction exists only after pre-begin zero-residue rejection; R2-frozen understated claims require new acceptance/revision/UUID; actual class overflow has no correction | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

Capacity mutation reachability is explicit:

- agent aggregate: class maxima `64 MiB`, buffer slots `2`, aggregate `96 MiB`;
  after one `64 MiB` reservation the second aggregate admission fails while a
  slot remains;
- agent slot: buffer slots `1`, aggregate `64 MiB`, two `1 MiB` buffers; the
  second slot admission fails while aggregate bytes remain;
- custody aggregate: window `8 MiB`, deployment streams `2`, aggregate
  `12 MiB`; the second window reservation fails while a stream slot remains;
- custody per-producer slot: producer streams `1`, deployment streams `2`,
  window `1 MiB`, aggregate `64 MiB`; the producer's second stream fails while
  deployment and aggregate capacity remain;
- custody deployment slot: deployment streams `1`, two distinct producers,
  window `1 MiB`, aggregate `64 MiB`; the second stream fails while aggregate
  remains.

Each fixture has a positive admission control, exact owner/release proof and a
temporary mutation removing only the deciding limit/reservation that must turn
`C1_artifact_size_memory_and_concurrency_limits_fail_closed` red.

The v0.12 authoring sweep read every C1-I01–C1-I48 mutation target and every
Test-Bite broken-mechanism cell against its adjacent predicates. After replacing
the subsumed capacity product/aggregate target, it found **no additional
subsumed mandated mutation**. Structurally unreachable B4/job cases remain
explicitly N/A and are not counted as green coverage. This is a consistency
sweep result, not an independent-review PASS claim.

Affected-surface audit: the capacity manifest, readiness, ordering, ingress
outcome/precedence, result contract, shape/nullability, invariants, test-bite,
acceptance and STOP surfaces change in planning only. The transport result union
reuses `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`; no new public code is
minted. HLD, LLD, debt and TIP index are synchronized. No implementation, raw
handling, provider/key operation or Build Brief is authorized.

### 18.17 v0.13 admission, union and same-owner correction

The v0.12 bundle passed SHA, exact 14-entry POSIX manifest, 13/13 checksum and
five-file allowlist verification. Independent review accepted Capacity Model A
and found four unpropagated consequences of the one-call transport: no
transport-enforced R1/body phase, internal claim states inside the final union,
same-owner retry colliding with a retained live lease, and an undefined dynamic
agent-capacity assertion.

| Finding/rule changed | Sections/siblings | v0.13 correction | Status |
| --- | --- | --- | --- |
| R1-before-body was application ordering only | D4.2–D4.3, 7, 9–13, 15–17; HLD/LLD/debt/index | One operation now gates metadata → committed R1 → AdmissionAccepted → body; early body is protocol-invalid; proxy/request/disk buffering is forbidden; bounded kernel/TLS memory residual is explicit | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Mid-operation claim states could egress | D4.3, 9–13, 15–17; HLD/LLD/debt/index | Split `InternalClaimResult` from `CaptureAgentFinalResult`; New/Existing/Reclaimed/revision/fence never egress; exact mapping and no-egress test pinned | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Same UUID retry hit its own healthy-owner lease | 5.6.4, 7, 9–13, 16–17; HLD/LLD/debt/index | Retain owner marker; same-owner/current-row CAS plus durable `PreviousFencedR2Terminated` creates a new monotonic attempt/fence; different owner or active writer remains busy | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Agent capacity assertion had no schema or observable server path | D4.2, 7.2, 9–13, 15–17; HLD/LLD/debt/index | Pure local CaptureAgent admission; local exhaustion makes no call; server capacity code and fixtures are custody-only; no attestation surface exists | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

Sibling-anchor sweep results:

- found and corrected the external-union residue in D4.3, ordering, outcome
  mapping, shape/nullability, C1-I43, Test-Bite, acceptance, STOP, HLD, LLD,
  debt and index;
- found and corrected “zero body bytes” overclaim in ordering/acceptance and
  added the bounded transport residual/readiness/code/test anchors;
- found and corrected dynamic agent-manifest/assertion wording in section 7.2,
  precedence, outcome contract, readiness, capacity tests, HLD, LLD and debt;
- found and corrected same-owner retry gaps in ownership rows, busy/transient
  outcomes, invariant/test/acceptance/STOP and sibling documents;
- checked all `NewReservation`, source-ingress `ExistingMatch` and
  `ReservationReclaimed` occurrences. Remaining occurrences are explicitly
  internal or historical. Sealed-assembly `ExistingMatch` is a distinct
  section 10.2 contract and is intentionally unchanged;
- checked D4.2, complete-outcome contracts, readiness, changelog, affected-
  surface map, closure aids and result vocabulary. No other current normative
  anchor still claims external New/Existing/Reclaimed, server-side agent
  attestation, zero physical pre-admission bytes or release-on-abort.

#### Duplication report — observation only

No document is restructured by v0.13. A later Homeowner decision could reduce
duplicated normative text as follows:

| Duplicated normative contract | Current copies | Recommended single normative owner |
| --- | --- | --- |
| C1 authority/status, ratified/open decisions and implementation block | All five | Planning Brief; README/debt link and state only |
| One-call transport, admission phases, outcome precedence and final result vocabulary | Planning, HLD, LLD, debt, index | Planning Brief |
| Claim alias/token/broker, internal/final unions, fencing and same-owner re-entry | Planning, HLD, LLD, debt, index summaries | Planning Brief; LLD keeps only persisted shape |
| Exact data families, nullability and canonical/hash preimages | Planning and LLD, summarized in HLD/debt/index | LLD |
| Process/capability topology and host trust boundaries | Planning and HLD, summarized elsewhere | HLD |
| Capacity keys, relations, readiness codes and mutation fixtures | Planning, HLD, LLD, debt, index | Planning Brief; CaptureAgent-owned settings referenced, not recopied |
| Legal/provider/retention/ART debts and exit triggers | Planning and debt, summarized in HLD/index | Debt Registry |
| TIP version/status/navigation history | Planning/debt/README headers and changelogs | TIP index for navigation; each artifact retains only its own version |

Based on repeated C1 paragraphs and matrices—not legacy unrelated TIP history—a
reference-based rewrite would likely remove approximately **12,000–17,000
duplicated words** across the five documents, roughly **15–22%** of the reported
78,925-word set. This is an estimate, not a restructuring authorization or a
request to close the Planning Brief.

Affected-surface audit: transport posture and one new runtime/readiness code,
the internal/final result type split, reservation termination evidence and
custody-only capacity semantics change in planning only. No multipart/resume
surface, dynamic agent attestation, release-on-abort, Build Brief,
implementation, raw handling or provider/key operation is authorized.

### 18.18 v0.14 exhaustive phase and termination-budget correction

The v0.13 bundle passed SHA, exact 14-entry POSIX manifest, Linux 13/13 checksum,
five-file allowlist and patch parsing. Independent review found one class:
metadata-only outcomes and the committed-R1/pre-admission window were not
enumerated by the unconditional happy-path sequence, and the newly mandatory
prior-R2 termination proof had no time budget.

| Finding/rule changed | Sections/siblings | v0.14 correction | Status |
| --- | --- | --- | --- |
| One-call outcomes lacked an exhaustive phase contract | D4.3, 7.1, 9.1, 10–13, 15–17; HLD/LLD/debt/index | Section 10.0 is the single P0–P7 phase × outcome source; every stable code has complete body/admission/residue/retry/union cells or explicit structural exclusion | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Metadata outcomes were forced through R1/body | D4.3, 7.1, 9.1, 10, 12–13, 16–17; HLD/LLD/debt/index | Branch A returns metadata-only with no admission/body/new R1; Branch B alone commits R1/re-entry CAS and receives body | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| P4 post-R1/pre-admission failure used zero-R1 residue | 5.6.4, 7.1, 10–13, 16–17; HLD/LLD/debt/index | P4 preserves Bound alias/canonical anti-reuse evidence, records `AdmissionProtocolRejected` / `TerminatedBeforeStart`, cleans exact attempt/object/key/capacity and pins same-owner restart | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |
| Prior-R2 termination had no continuation cost | 5.6.3–5.6.4, 7.1, 10, 12–13, 15–17; HLD/LLD/debt/index | Four bounded no-default components form `PreviousR2TerminationBudget`; readiness includes worst-case cost and runtime counts it exactly while termination is not durable | **PATCHED — EXTERNAL RE-REVIEW REQUIRED** |

Sibling-anchor sweep checked section 10.0 against D4.3, 7.1, 9.1, 10.1,
10.1.1, shape/nullability, invariant trace, Test-Bite, readiness, acceptance,
STOP, review plan, HLD, LLD, debt and index. It corrected unconditional
metadata→R1→body wording, blanket zero-R1 residue, post-admission-only transient
wording and wait+claim+complete+R2 sums lacking termination. Remaining
historical v0.11–v0.13 sequences are explicitly changelog/review records; no
current normative sibling owns an independent transport sequence.

The post-table mutation-reachability sweep covered C1-I01–C1-I50 and every
Test-Bite row. New phase fixtures are independently constructible: metadata
AlreadyAvailable/live/conflict/busy/authority cells require no body; P0–P3
early-body runs without committed R1; P4 runs after a proven R1 and before
successful signal write/flush with reader/writer unarmed; the 80-second old sum
plus 70-second termination term
against a 120-second window isolates only the new relation. No additional
subsumed mandated mutation was found. Structural readiness/assembly exclusions
are N/A and are not counted as green behavioral coverage.

The v0.13 duplication table immediately above remains the required report:
authority/status and transport/claim/capacity contracts should be normatively
owned by the Planning Brief; persisted shapes/canonical data by the LLD;
process/trust topology by the HLD; legal/provider/retention debts by the Debt
Registry; and navigation/version history by the TIP index. The
12,000–17,000-word, 15–22% estimate remains observational only. No restructure
is authorized.

### 18.19 v0.15 class-closing symbol and retry audit

The v0.14 bundle passed the independently reported SHA, exact 14-entry POSIX
manifest, Linux checksum and five-file allowlist checks. This patch closes one
root-cause class: a normative symbol or phase boundary without an owned,
observable and testable contract.

One normative-symbol register was added in section 15.1. It defines the six
previously unowned relation terms, every adjacent time/capacity symbol, the
server-observable P4/P5 predicates and the replay/retry dispositions. Existing
section 5.6.4 duplicate configuration/formula prose was removed and now refers
to the register.

The cite-or-delete sweep applied the section 10.0 source reference to Planning
sections 5.6.3–5.6.4, 7.1–7.2, 10.1, 15–16 and the synchronized HLD C1 subsection, LLD C1
subsection, Debt Registry P0 row and TIP Index v0.15 entry. Planning D4.3, 9.1,
10.0, 10.1.1, shape/nullability, outcome mapping, acceptance and STOP already
referenced section 10.0 or were structural non-transport contracts and were
checked without a new restatement. Historical changelog/review records remain
historical; active phase/residue/retry/body/admission semantics defer to section
10.0.

The sibling-anchor sweep covered the five allowed documents for the former
artifact-scoped operation wording, replay-stable temporary-unavailability,
client-observed admission, P0–P3 blanket residue and two-case continuation
wording. Current wording now defines one operation shape per invocation, exact
same-UUID retry, server-observable phase boundaries and all three continuation
cases.

The mutation-reachability sweep rechecked C1-I01–C1-I50 and every Test-Bite row.
The new expired-owner fixture independently selects reclaim with durable prior
termination, so deleting only `ReclaimExecutionBudget` changes 130 seconds to
80 seconds against a 120-second window and turns
`C1_plaintext_and_reconciliation_time_bounds_are_complete` red. The P4 and P5
fixtures independently control signal write/flush and reader arming. The
temporary-unavailable retry fixture independently constructs not-durable,
durable-with-budget and buffer-lost/budget-insufficient outcomes. No newly
subsumed mandated mutation was found; structural exclusions remain N/A and do
not count as green behavioral coverage.

### 18.20 v0.17 final semantic closure

The v0.16 mechanical checks remain the baseline. v0.17 separates the three
projection clocks, corrects late token/backoff anchors, removes the duplicate
per-claim attempt limit and transfers retry accounting to
`C1-BB-RETRY-COUNT-ACCOUNTING-GATE`. Planning closes after v0.17 under the
Homeowner's advance stopping rule. Subsequent semantic residuals are discharged
by Build-Brief gates and implementation tests, not another planning-review
round. No Build-Brief, implementation, provider, raw-data, commit or deployment
authority results.

## 19. Review plan

Planning review follows PI-TAG-001:

1. independent full-coverage review of this artifact and every named source;
2. patch only verified findings, with an affected-surface map;
3. stale wording/version/status/provider/mode sweep after each patch;
4. mutation-reachability sweep across the complete Invariant Trace and
   Test-Bite matrices; every target must have a supported fixture in which no
   adjacent stronger predicate subsumes it, otherwise it is redesigned or
   marked N/A and cannot count as coverage;
5. round-5 root-cause checkpoint if not clean;
6. round-10 hard stop;
7. no Build Brief drafting until the Homeowner ratifies every D1
   durable-custody sub-decision and D3–D9, preserves ratified D2, and
   independent review returns
   `PASS — 0 actionable findings`.

The first reviewer must specifically challenge:

- whether the ratified `EncryptedRawVaultRetained` direction remains
  non-authorizing while every controller/legal/retention sub-decision stays
  visibly open;
- whether the ratified two-class subset maps to exact producer bytes and every
  other class remains fail-closed;
- whether the ingress/worker credential separation is deployable;
- whether S3 and filesystem semantics are actually equivalent under the port;
- whether encryption, manifest authentication, and C2 boundaries are
  non-overlapping;
- whether any source read can precede fresh authority;
- whether R1–R6 and prepare/seal/finalize/abort converge along the actual time
  axis without circular inputs or aborting a committed seal;
- whether every ingress replay is locatable before `SourceArtifactId`, uses the
  historic commitment key, and remains bounded/non-disclosing under concurrency;
- whether R1 alone contains every secret/context needed to validate complete
  ciphertext after worker loss, while no path invents cross-process plaintext;
- whether healthy producer ownership excludes reconciliation and all duration
  relations use `EffectivePlaintextRetentionExpiresAtUtc`;
- whether authority loss at R3/R4/R5/pre-Prepare/in-transaction Seal has one
  exact logical and policy-predicated physical disposition;
- whether preparation A can ever be finalized because of seal B, or aborted
  while its current attempt can still seal;
- whether graceful zeroization evidence is kept distinct from abrupt-loss host
  residual risk;
- whether exact replay excludes admission-only values and survives PostgreSQL
  timestamp round-trip;
- whether CaptureAgent buffer memory and custody-server plaintext-window memory
  remain separately owned/enforced and each slot/aggregate branch can decide a
  supported fixture;
- whether the one-call transport actually prevents client/proxy/application
  body handling before committed-R1 admission, forbids disk spill and states
  the bounded kernel/TLS residual honestly;
- whether section 10.0 contains every stable code exactly once, every cell is
  filled, Branch A can never request body/admission and P4 preserves exact
  anti-reuse identity while cleaning only the rejected attempt;
- whether InternalClaimResult can reach no CaptureAgentFinalResult shape and
  source-ingress New/Existing/Reclaimed remain internal without changing the
  separate sealed-assembly ExistingMatch contract;
- whether same-owner re-entry keeps the owner marker, proves the prior body
  reader and provider writer terminal, and preserves different-owner busy;
- whether all four prior-R2 termination costs are bounded/no-default, appear in
  every applicable `RP-*` relation, are counted exactly while termination is
  pending, and are zero after either exact settling disposition;
- whether the published 91-symbol population and independently extracted
  register list have zero differences/duplicates/blanks, every row is total or
  closed, and all newly owned keys have no duplicate or implicit source;
- whether all 15 reachable state × preceding-wait paths have exactly one
  relation, including evaluation-wait plus expired-owner pending termination and
  reclaim, with only shared-anchor max terms and no extra relation;
- whether readiness, CaptureAgent pre-wait and custody post-wait projections
  use distinct clocks and the server never re-adds an elapsed wait;
- whether pending/ready same-owner and expired-owner cases are disjoint,
  contain no dead conditional, and include `ReclaimExecutionBudget` only in both
  expired-owner cases;
- whether three source/key/object limits and the common busy backoff
  independently bind, retry counting remains blocked behind its nine-question
  Build-Brief gate, and disposition start/expiry are persisted once and never
  reset;
- whether P4/P5 classification uses only signal write/flush and reader/writer
  arming facts known by the server, never client observation;
- whether temporary unavailability remains non-terminal/non-replay-stable and
  the next same-UUID invocation chooses busy, re-entry or recapture from durable
  termination, budget and buffer evidence;
- whether agent capacity exhaustion remains purely local with no invented
  assertion/attestation or server-side fixture;
- whether transport abort/incomplete body and clean EOF commitment mismatch
  remain distinguishable through cleanup and retry;
- whether frozen per-job selection prevents recapture/substitution while
  allowing independently authorized retained-source reuse; and
- whether crash/retry/reclaim can leave plaintext, partial objects, prohibited
  observability, or duplicate logical assemblies.

The review bundle must also be extracted in a clean Linux environment:
`sha256sum -c SHA256SUMS.txt` and a byte-for-byte sorted-entry comparison must
pass for the exact 14-name POSIX manifest.
