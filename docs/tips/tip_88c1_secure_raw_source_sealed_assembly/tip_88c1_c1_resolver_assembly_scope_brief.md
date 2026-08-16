# TIP-88C1-C1 — Resolver + Authenticated Assembly Scope Brief

Status: `ROUND_1_RECONCILED — READY_FOR_ROUND_2_DISPATCH`  
Version: `0.2`  
Date: `2026-08-15`  
Repository: `TagEkyc`  
Branch: `tip-88a-raw-export-policy-catalog-build`  
Baseline: `c2956b168f8475e6285dc51d4037bab55234e32c`  
Change authority: `DOCS_ONLY_SCOPE_BRIEF_AND_REVIEW_EVIDENCE`  
Implementation authority: `NONE`

## 0. Changelog

### v0.2 — Round 1 reconciled

- Corrects C2 ordering to durable registration, `Prepare`, atomic committed
  seal, then `Finalize`; unknown seal never authorizes abort.
- Pins fresh authority before first plaintext, immediately before C2 Prepare,
  and inside the new-seal transaction after locks and provider/key/assembly
  I/O; exact committed replay remains non-mutating and does not re-authorize.
- Authenticates the exact attempt-scoped `ManifestDigest`, not the bare
  `AssemblyDigest`.
- Adds landed C1-A acceptance/session-selection and landed AUTH
  reuse/extension disposition evidence to the on-code census.
- Makes the B4 state-machine amendment explicit: the head state is reserved,
  but the transition shape/guards and sealed-state lease behavior are not.
- Separates durable C2 recovery semantics from prohibited durable plaintext.
- Narrows the locator/key prohibition to public and general-runtime surfaces;
  restricted custody locator and wrapped-key records remain permitted.
- Moves lock-order, B4 sealed-state compatibility, typed-verification
  dependency and streaming anti-buffer proofs into Round 2.
- Records both independent reviews and every finding disposition in the
  append-only ledger; Round 1 is closed without a fourth scope pass.

### v0.1 — Round 1 candidate

- Re-anchors Resolver + Assembly C1 to the landed R1–R6, Durable Key,
  Durable Object, B3 authorization and B4 job foundations.
- Separates the C1 product slice into two executable milestones without
  splitting its end-to-end security contract:
  `Resolve/Freeze` and `Assemble/Seal`.
- Introduces the Homeowner's three-round documentation-convergence rule.
- Records landed surfaces, missing surfaces, trust boundaries, explicit
  deferrals and the questions that Round 1 must answer.
- Does not authorize implementation, migration, staging, commit or provider
  operation.

## 1. Process authority and three-round convergence rule

For this C1 Resolver + Assembly phase, the Homeowner has replaced the prior
open-ended document loop with the following bounded process:

1. **Round 1 — Scope / product / trust:** review this brief for product intent,
   authority boundaries, capability separation, data exposure and deferrals.
2. **Round 2 — Executable dispatch:** reconcile accepted Round-1 findings into
   one on-code dispatch with an exact allowlist, migrations/functions,
   transition ownership, outcomes and proof obligations.
3. **Round 3 — Closure:** review exact candidate bytes and exact SHA-256. Fix
   only contradictions that block safe implementation or make the proof
   contract impossible.

After Round 3:

- executable uncertainty moves to real code and discriminating tests;
- a code-discovered implementation defect is corrected under the dispatch's
  bounded implementation/remediation authority;
- a new document round is permitted only for a genuine contract,
  product-authority or allowlist contradiction, and must be a narrow
  append-only amendment rather than a full redraft;
- editorial improvement and speculative hardening are debt, not reasons to
  reopen the planning loop.

This is a Homeowner phase-specific override of the higher review-round limits
in PI-TAG-001. It does not silently edit the global process-improvement file.
Every round and every received review must instead be recorded in the sibling
append-only review ledger.

Validation policy:

- during implementation, run changed/affected tests first;
- run expensive shared suites only when their owned surface changes or a
  failure implicates them;
- run one complete unfiltered Release suite on final candidate bytes at
  closeout;
- do not use a failed or stale run as a substitute for that final census.

## 2. Authoritative inputs

This brief consumes, but does not modify, the following landed or planning
contracts.

| Input | Role in C1 |
|---|---|
| `tip_88c1_planning_brief.md` | Product intent, source binding, assembly envelope and C2 boundary |
| Landed C1-A capture acceptance | Append-only acceptance events and exactly one server-authored session selection per class |
| Landed B3 authorization | One-time authorization/session/recipient/policy provenance |
| Landed B4 job foundation | Job identity, attempt/revision/fence, ownership lease and lifecycle vocabulary |
| Landed B2 AUTH snapshot | `ReuseDisposition=FreshAuthorityRequired`, `ExtensionDisposition=Forbidden`, retention/revocation/hold provenance |
| Landed R1–R3 | Reserved source, durable encryption, exact verification and `Staged` |
| Landed R4–R6 | Committed source publication, `Available`, stable winning object/key and cleanup |
| Landed Durable Key | Durable wrapped-key reservation and typed key recovery |
| Landed Durable Object | Conditional single-part object custody and exact-object read/delete boundaries |

The exact file hashes and predecessor closeout references are registered in
`tip_88c1_c1_resolver_assembly_review_ledger.md`. A reviewer must report any
live-byte mismatch before reusing predecessor evidence.

## 3. On-code boundary: what is landed and what is absent

### 3.1 Landed and reusable

- B4 has a metadata-only raw-export job with actor-scoped lease, attempt,
  revision and fence ownership.
- C1-A has landed append-only `raw_export_capture_acceptance_events` and
  `raw_export_session_capture_selections`; the latter is the server-authored
  source-selection authority. C1 must consume it and may not derive selection
  from `capture_artifacts`, `QualityState`, latest timestamp or provider order.
- B2 AUTH has landed and constrained the current disposition set to
  `ReuseDisposition=FreshAuthorityRequired` and
  `ExtensionDisposition=Forbidden`. The planning marker
  `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` is therefore stale for this baseline;
  C1 consumes the landed values and may not invent another value.
- B4 head-state vocabulary reserves `Assembling` and `AssemblySealed`, and the
  head-shape constraint admits a sealed head.
- B4 transition-event shape and mutation guards do **not** admit an
  `Assembling -> AssemblySealed` event. The attempt phase remains correctly
  `Assembling`; it does not need a new phase value merely to seal.
- R4–R6 persists a source publication that binds the winning attempt, durable
  object, durable key, staged fingerprint, authority/consent provenance,
  opaque locator identity, evidence and `AvailableAtUtc`.
- Durable Object owns exact-object read/delete and provider posture.
- Durable Key owns wrapped-key lifecycle and bounded DEK leasing.
- R2 owns framed ciphertext decoding and typed AEAD verification.
- R4–R6 preserves the winning object and winning key while obsolete resources
  are cleaned independently.

### 3.2 Not landed

- no immutable per-job source-binding table;
- no assembly-worker-only resolver from an `Assembling` job to exact
  `Available` sources;
- no public contract that exposes an authorized bounded plaintext stream while
  hiding provider locator, credentials and key material;
- no assembly root/item/preparation persistence;
- no canonical assembly codec or assembly authentication service;
- no B4 phase function that performs the guarded
  `Assembling -> AssemblySealed` transition;
- no admitted B4 transition-event row for `AssemblySealed`; a new C1 migration
  must replace `CK_b4_job_transition_event_shape` and re-emit the landed B4
  transition/head guard functions while preserving every existing branch and
  restoring their exact landed bodies in `Down()`;
- no test-only C2 prepare/finalize/abort sink;
- no production raw-source adapter, production C2 recipient-key provider,
  package builder or delivery surface.

These absences define C1. They must not be disguised as adapters over process
memory or as generic repository reads.

## 4. Product objective

C1 converts the exact authorized set of already-`Available` encrypted raw
sources for one B4 job into one durable, canonical, authenticated assembly
handoff without durably materializing plaintext.

The intended successful flow is:

```text
B4 job Assembling under exact attempt/revision/fence/lease
-> revalidate B3/B1/B2/session/authority/consent/retention/reuse and job expiry
-> freeze one exact Available source per required ordered RawClass
-> resolve each frozen source through restricted custody capabilities
-> stream decrypt + typed AEAD verify + historic commitment verify
-> stream canonical assembly bytes and compute assembly evidence
-> fresh authority/deadline check immediately before C2 Prepare
-> persist deterministic Preparing identity, then C2 Prepare outside DB
-> persist Pending
-> inside one new-seal transaction: exact replay first; otherwise lock,
   capture post-lock current time, freshly revalidate authority/deadlines,
   then persist SealCommitted + immutable assembly identity/items/evidence
   and guarded B4 Assembling -> AssemblySealed
-> C2 Finalize only for the exact preparation referenced by committed seal
```

Success means the durable database contains metadata and evidence sufficient
to replay, audit and hand off the same assembly, while PostgreSQL, logs,
exceptions, metrics and application outputs contain no raw plaintext,
unwrapped DEK/KEK or provider credential. Restricted custody tables may retain
the landed opaque object locator and approved wrapped-key metadata; these are
never assembly/public outputs.

## 5. Scope decomposition

C1 is one end-to-end security contract with two executable milestones. The
Round-2 dispatch may order their implementation separately, but neither
milestone may invent a weaker trust model.

### 5.1 Milestone C1-R — Resolve and Freeze

C1-R owns:

- accepting only an actor-scoped B4 `Assembling` job under the current
  attempt/revision/fence/lease;
- fresh revalidation of the job's B3 authorization, verification session,
  subject/client/recipient binding, policy version, purpose, source-retention
  deadline, revocation and hold posture, and job expiry;
- requiring the exact landed AUTH dispositions
  `ReuseDisposition=FreshAuthorityRequired` and
  `ExtensionDisposition=Forbidden`; a retained source never carries authority
  from an earlier job and C1 never extends its source lifetime;
- deriving the ordered required RawClass set from the B4 job and deriving each
  acceptance only from landed `raw_export_session_capture_selections` joined to
  its append-only `raw_export_capture_acceptance_events`, never from
  caller-selected source identities;
- selecting exactly one eligible `Available` source for each required class;
- freezing immutable per-job bindings before the first raw-source read;
- binding at minimum:
  `JobId`, `Ordinal`, `RawClass`, `VerificationSessionId`,
  `CaptureAcceptanceId`, `CaptureArtifactId`, `CaptureRevision`,
  `SourceArtifactId`, source publication identity/revision and the exact
  winning attempt/object/key identities required for recovery;
- exact replay for the same frozen set and a terminal conflict for any
  different set;
- rejecting missing, ambiguous, stale, non-Available, authority-mismatched or
  binding-invalid sources without disclosing source existence;
- exposing a narrow assembly-worker resolver that returns only the metadata
  and scoped operations required to read and verify the frozen source.

C1-R does not expose:

- an arbitrary source lookup;
- a caller-selected artifact, object, key or locator;
- provider credentials, object keys, wrapped/plain DEKs or internal authority
  rows;
- plaintext bytes in a DTO, repository result or persisted cache.

### 5.2 Milestone C1-A — Assemble and Seal

C1-A owns:

- reading only the exact bindings frozen by C1-R;
- bounded streaming from durable object custody through typed AEAD
  verification and historic commitment verification;
- enforcing ordered class, ordinal, declared length, media type and exact
  source identity;
- producing a byte-pinned canonical assembly stream with a fixed header,
  canonical manifest and ordered length-delimited item bodies;
- computing an unkeyed assembly fingerprint for equality/audit and a separate
  `AssemblyDigest`; building the attempt-scoped `ManifestDigest` that binds the
  assembly digest, attempt, fence, normalized creation time, authentication
  key id/version and all manifest fields; and authenticating the exact
  `ManifestDigest` under a dedicated assembly-authentication capability;
- registering a deterministic C2 preparation as `Preparing` before external
  I/O, calling test-only C2 `Prepare`, then persisting `Pending`;
- atomically persisting `SealCommitted`, immutable assembly root, ordered item
  references and evidence, and performing the exact B4
  `Assembling -> AssemblySealed` transition under the owning
  attempt/revision/fence;
- calling C2 `Finalize` only after the exact committed seal names that exact
  preparation;
- leaving `SealOutcomeUnknown` in reconciliation and never inferring abort;
  C2 `Abort` is legal only after an atomic `AbortAuthorized` disposition proves
  that the exact preparation can never be committed;
- replaying an identical completed seal without re-reading plaintext or
  producing a second assembly;
- preserving a fail-closed recovery state when C2 outcome is unknown.

The C2 fixture has durable identity/disposition/response-loss semantics, not a
durable plaintext-vault claim. Assembly bytes may be memory-only or bounded
encrypted-at-rest, must be non-exportable to production consumers, and must be
purged after `Finalize` or `Abort`. Hash-and-discard and unbounded plaintext
sinks are both insufficient.

The Round-2 dispatch must pin the exact frame/manifest codec, hash domains,
authentication algorithm/provider contract, persistence shapes, transitions
and replay precedence. Round 1 fixes the security boundary, not those SQL and
byte-level implementation details.

## 6. Trust and capability boundaries

The minimum capability graph is:

```text
ordinary runtime / API
  -> may create/read public job metadata through landed B3/B4 surfaces
  -> cannot resolve Available sources, object locators or key material

assembly coordinator
  -> owns job lease/fence and orchestration
  -> may call narrow C1-R/C1-A SECURITY DEFINER or repository surfaces
  -> receives no provider credentials and no plaintext aggregate

source resolver / custody reader
  -> reads exact frozen binding and exact Available source context
  -> may acquire exact-object read and exact-key verification capabilities
  -> cannot select a different source or seal the job

assembly authenticator
  -> authenticates the exact attempt-scoped ManifestDigest under a dedicated
     assembly-authentication key domain
  -> cannot read source objects or unwrap source DEKs

C2 fixture sink
  -> accepts prepared assembly bytes/evidence for proof only
  -> is not production readiness evidence and has no delivery capability
```

No single public/runtime identity may combine arbitrary source selection,
object read, key unwrap and job seal authority. Exact role names, ownership,
memberships, effective ACLs and production-readiness codes are Round-2
dispatch obligations after on-code role census.

## 7. Authority, locking and freshness invariants

- Source choice is frozen only while the B4 job ownership tuple is current.
- Authorization, consent, session, recipient, purpose, policy, retention,
  revocation/hold posture and expiry are revalidated at three distinct
  admission boundaries:
  1. the last safe point before the first plaintext read;
  2. immediately before C2 `Prepare` after assembly I/O completes;
  3. again inside the new-seal transaction, after all required locks and a
     single post-lock current-time capture, and after provider/key/assembly I/O.
- The exact frozen binding is rechecked before every source read and before
  sealing.
- A blocking lock may not leave a pre-lock authorization/deadline timestamp as
  the admission decision. The dispatch must pin one post-lock current-time
  capture and revalidation where needed.
- Lock order must extend, not invert, landed R2/R3/R4–R6 order. It must be
  proven against response-loss replay and concurrent B4 ownership changes.
- The seal CAS changes the B4 job only under the exact current
  attempt/revision/fence and cannot be performed by the source reader or C2
  sink.
- Exact committed `AssemblySealed` replay is evaluated from immutable persisted
  equality surfaces before new-seal admission. It returns without fresh
  reauthorization, plaintext/object/key I/O or mutation. This exception cannot
  be used to admit a new seal.
- One-time B3 consumption continues to be represented by the unique B4 job
  lineage. C1 must not add a second consumption ledger unless executable
  evidence proves the landed invariant insufficient.

## 8. Plaintext and cryptographic handling

- Plaintext exists only in bounded custody-owned chunks between verified
  decryption and the downstream assembly write.
- No complete source and no complete assembly plaintext may be buffered in
  process memory.
- No plaintext, raw ciphertext, unwrapped DEK/KEK, provider credential or nonce
  seed may be persisted in PostgreSQL or emitted to logs/errors/metrics.
- The landed restricted custody record may persist its opaque exact object
  locator and approved wrapped-key metadata. Those values remain accessible
  only through custody/key capability surfaces and are prohibited from C1
  assembly tables, public/general-runtime DTOs, logs, errors, metrics, audit
  payloads and outputs.
- Custody-owned plaintext chunks, cryptographic scratch and DEK leases are
  zeroized on normal and handled exceptional paths. The contract does not
  claim active zeroization after abrupt process loss.
- Historic commitment selection uses only the selector frozen with the source;
  no current/latest/caller-selected key is admitted.
- Assembly authentication uses a domain and capability distinct from source
  encryption, historic commitment and subject-token domains.

## 9. Replay, failure and disclosure posture

The dispatch must define exact typed outcomes and precedence. Round 1 fixes
these classes:

- exact frozen-binding replay;
- source selection missing/ambiguous/conflicting;
- source no longer `Available` or exact binding invalid;
- authority/consent/session/job-expiry loss;
- source object/key read indeterminate;
- deterministic AEAD or historic-commitment verification failure;
- C2 prepare/finalize outcome unknown;
- assembly equality match versus assembly conflict;
- stale job attempt/revision/fence;
- successful `AssemblySealed` replay.

Errors returned outside the restricted worker boundary must not reveal which
source, class, object, key, authority or cryptographic check failed. Durable
internal evidence may be more specific when its read ACL is correspondingly
restricted.

## 10. Required durable recovery context

No value required after process/provider restart may live only in process
memory or only inside a one-way fingerprint. At minimum, recovery must be able
to re-establish:

- exact job/attempt/revision/fence lineage;
- ordered frozen source bindings and their source publication revisions;
- exact winning object and key identities/selectors;
- source declared length/media/capture metadata and stored commitment;
- assembly codec/profile version;
- assembly root identity and ordered item metadata;
- C2 preparation identity, operation token/receipt and disposition;
- assembly fingerprint/authentication/evidence metadata;
- the transition/event identity needed for exact replay.

Plaintext digest values that the landed source contract prohibits remain
transient and prohibited from persistence.

## 11. Explicit deferrals and non-claims

This C1 proof slice does not include or claim:

- a production CaptureAgent/raw-source adapter or real Raw BIO input;
- production C2 recipient-key/provider integration;
- recipient package construction, recipient encryption, R4/R5 package phases
  or C3 delivery;
- production activation of OpenBao/HSM/S3 identities;
- multipart, versioning, Object Lock or heavy-media transport;
- caller/admin source selection;
- cross-region disaster recovery;
- opening the deferred `R4R6-DURABLE-O-T15-QUARANTINE-EVIDENCE` gate;
- proof that fixture providers are production durable providers.

The successful C1 proof-build claim is limited to synthetic plaintext and
fixture/development providers crossing the same durable contracts used by the
production composition root.

## 12. Preliminary implementation surface categories

Round 1 does not authorize paths. Round 2 must derive an exact allowlist from
on-code evidence. Expected categories are:

- C1 contracts and domain entities;
- EF mappings, DbContext, one additive migration, Designer and ModelSnapshot;
- restricted resolver repository/service;
- canonical assembly codec/orchestrator/authentication boundary;
- test-only C2 fixture sink;
- B4 phase-specific seal surface and event vocabulary, including a new C1
  migration that replaces `CK_b4_job_transition_event_shape`, re-emits
  `enforce_raw_export_job_transition_insert()` and
  `enforce_raw_export_job_head_mutation()`, preserves the existing head-shape
  and `Phase='Assembling'`, and restores the exact landed B4 bodies in `Down()`;
- focused integration, architecture and contract tests;
- C1 as-built and the append-only review ledger.

The B4 amendment above is explicitly part of C1 and does not edit landed B4
migration bytes. Any other required change to landed R1–R6, Durable Key,
Durable Object or B3/B4 contract must STOP/RRI rather than silently expand the
scope. Additive replacement functions in the new C1 migration require exact
signature lifecycle, ACL restoration and `Down()` bodies.

## 13. Round-1 adjudication

Two independent reviews were received against v0.1 SHA
`AECB9B1585EC52B15CFDD8F701A037539F5A3C66CD5FAB638A94286618CFE71A`.
Their exact reports and individual dispositions are registered in the sibling
ledger. The product-boundary answers are:

1. One security contract with `Resolve/Freeze` and `Assemble/Seal` remains the
   accepted boundary.
2. Caller-selected/generic lookup remains forbidden; landed C1-A session
   selection is now an explicit input.
3. The conceptual capability split is accepted; exact roles and effective ACL
   proof belong to Round 2.
4. Three fresh-authority boundaries, post-lock time and committed-replay
   precedence are now explicit.
5. A test-only C2 sink is accepted only with the corrected seal/finalize order
   and plaintext-storage restrictions.
6. The B4 state-machine amendment is accepted as C1 scope. The claimed
   unresolved acceptance and AUTH disposition dependencies were stale bundle/
   planning evidence: both are already landed at this baseline.
7. Exact lock order, B4 sealed-state lease closure, typed verification
   dependency, streaming anti-buffering and all SQL/codec/ACL/golden-vector
   details are Round-2 executable proof obligations.

Round-1 finding disposition summary:

| Finding | Disposition |
|---|---|
| C2 Finalize before committed seal | `ACCEPTED_CONTRACT_CHANGE` |
| Missing authority checks before Prepare and inside Seal | `ACCEPTED_CONTRACT_CHANGE` |
| Authenticator targeted bare AssemblyDigest | `ACCEPTED_CONTRACT_CHANGE` |
| Capture acceptance/session selection allegedly absent | `REJECTED_WITH_ON_CODE_CLAIM`; landed C1-A evidence added |
| C2 fixture could imply durable plaintext | `ACCEPTED_CONTRACT_CHANGE` |
| Retention/reuse/hold omitted | `ACCEPTED_CONTRACT_CHANGE`; alleged unresolved disposition gate rejected because landed AUTH pins exact values |
| Blanket locator/key-material prohibition | `ACCEPTED_CONTRACT_CHANGE` |
| B4 transition contract does not admit AssemblySealed | `ACCEPTED_CONTRACT_CHANGE` |
| AssemblySealed lease/reclaim compatibility | `ACCEPTED_EXECUTABLE_PROOF` |
| Global lock-order placement | `ACCEPTED_EXECUTABLE_PROOF` |
| R2 typed-failure dependency | `ACCEPTED_EXECUTABLE_PROOF`; C1 must use typed verifier boundary, not the encryption-orchestrator catch-all |
| No-full-buffer guarantee | `ACCEPTED_EXECUTABLE_PROOF` |
| Predecessor review artifact not carried in Round-1 ZIP | `ACCEPTED_GOVERNANCE_EVIDENCE_GAP`; R4–R6 exact final bundle is carried forward, while R2/R3 are treated as committed implementation anchors rather than re-adjudicated review verdicts |

## 14. Round-1 exit criteria

Round 1 closes only when:

- every received review is registered in the append-only ledger;
- each finding has an identifier, severity, disposition and owner;
- product/trust decisions are accepted or explicitly deferred;
- no missing review artifact is silently inferred;
- the successor brief SHA is recorded if bytes change;
- Round 2 can produce one exact executable dispatch without reopening the
  product boundary.

Round-1 exit status:

```text
Received reviews registered:      YES — 2
Contract findings adjudicated:    YES
Successor scope issued:           YES — v0.2
Missing review silently inferred: NO
Round-2 product boundary stable:  YES
```

Current dispatch readiness: `YES — AUTHOR ROUND_2_EXECUTABLE_DISPATCH`.

## 15. Boundaries

This brief authorizes no implementation and no external side effect.

```text
NO PRODUCTION CHANGE
NO MIGRATION
NO PROVIDER OPERATION
NO STAGE
NO COMMIT
NO PUSH
NO MERGE
NO PR
NO DEPLOY
```
