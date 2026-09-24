# TIP-88C1 Secure Raw Source + Sealed Assembly — Feasibility Spike

**File:** `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_feasibility_spike.md`

**Version:** 0.3

**Status:** CHARACTERIZED — STORAGE DIRECTION RECORDED — IMPLEMENTATION BLOCKED — PLANNING INPUT ONLY

**Date:** 2026-07-28

**TagEkyc baseline:** `33b478c326fa3a69c95aebddddc0990fdcd36b21`

**CaptureAgent baseline inspected read-only:**
`b193f6316e13a82fb2f9f985f68500feae194cfa`

**Authority:** Homeowner instruction to run the C1 feasibility/source-discovery
step after TIP-88B4 and governance closeout

**Purpose:** Characterize whether a real Raw BIO source, producer handoff, and
class-to-artifact path exist for TIP-88C1 before a Planning Brief or build
dispatch assumes them.

## Changelog

### v0.3 — Planning-status history clarified

- Clarified that the v0.1 planning status below was the required status at
  planning opening, not the current status of the subsequently reviewed brief.
- Preserved all feasibility findings, provider direction, blockers, and
  non-authorization.

### v0.2 — Provider-neutral storage direction recorded

- Recorded the Homeowner-selected provider-neutral raw-artifact storage
  boundary.
- Selected an S3-compatible adapter as the first reference implementation,
  exercised against an exact pinned MinIO version/digest rather than
  `minio/minio:latest`.
- Kept MinIO as a qualified candidate rather than a domain dependency or an
  automatic production approval.
- Kept an encrypted-filesystem adapter as a separately qualified single-node
  alternative, not a second mandatory C1 implementation.
- Required TagEkyc application-layer per-artifact encryption before either
  provider receives bytes.
- Preserved the remaining mode, class-set, key-provider, retention-authority,
  and cross-repository decisions as implementation blockers.

### v0.1 — Initial feasibility characterization

- Inspected the TagEkyc server and sibling `TagEkyc.CaptureAgent` source
  read-only.
- Confirmed that TagEkyc persists capture metadata and hashes but has no raw
  source/provider/resolver capability.
- Confirmed that CaptureAgent currently owns bounded in-memory buffers and
  destroys them; `RawVault` is a rejected configuration value, not a provider.
- Mapped each `RawExportRawClass` to its currently observable producer state.
- Identified the post-completion B2 consent ordering as a load-bearing source
  establishment constraint.
- Activated PI-TAG-001 for TIP-88C1 before Planning Brief review round 1.
- Recorded the decisions and evidence required before implementation dispatch.

## 1. Authority and non-authorization

This spike is read-only characterization plus documentation. It records the
Homeowner's provider-neutral direction, but it does not:

- authorize or perform Raw BIO collection, copying, upload, persistence,
  resolution, assembly, encryption, or delivery;
- approve MinIO, an encrypted filesystem, or any other provider for production;
- select a filesystem layout, cloud provider, key provider, package format, or
  production topology;
- authorize changes in `TagEkyc.CaptureAgent`;
- authorize TagEkyc source, tests, schema, migration, API, worker, or deployment
  changes;
- treat fixture bytes, LocalDev metadata, a `VaultRef` type, or a database column
  as proof that a raw source exists; or
- authorize commit, push, merge, PR, release, or production activation.

No raw files, patient data, device output, credentials, or restricted artifacts
were opened during this spike.

## 2. PI-TAG-001 pilot activation

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

No risk module is omitted. The exact runtime shape is not yet selected, but each
module can become load-bearing in C1: producer handoff, isolated worker,
source-at-rest encryption, metadata persistence, authority/precedence, and
restricted-data lifecycle must be resolved together.

## 3. Questions investigated

1. Does TagEkyc currently receive or retain raw artifact bytes?
2. Does a server-side raw provider, vault, resolver, or read port exist?
3. Which raw classes exist transiently in CaptureAgent?
4. Which raw classes have a stable class-specific identity, digest, and
   retrievable source?
5. Can the current CaptureAgent flow satisfy either the no-retain or durable
   source mode without new behavior?
6. Does the landed B2 consent order permit raw retention before authorization?
7. What must a C1 Planning Brief decide before a Builder can implement without
   inventing the source boundary?

## 4. Evidence inspected

### 4.1 TagEkyc server

| Evidence | Observation | Consequence |
| --- | --- | --- |
| `src/TagEkyc.Contracts/CaptureAgent/CaptureAgentContracts.cs:24` | `CaptureArtifactSubmissionRequestDto` contains type/source/producer ids and hashes only; there is no byte body, stream, source handle, or trusted `VaultRef` input | The landed CaptureAgent HTTP route is metadata-only |
| `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs:129` | Every accepted capture artifact is created with `VaultRef: null` | Capture submission cannot establish a retrievable source |
| `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs:644` | Completion manifest projection also records `VaultRef: null` | Completion does not discover or backfill raw custody |
| `src/TagEkyc.Application/Ports/RepositoryPorts.cs:23` | `ICaptureArtifactRepository` can append/list metadata records only | No raw read/write port exists |
| `src/TagEkyc.Application/Ports/MetadataReferencePorts.cs:36,43` | The metadata registry explicitly denies artifact-access proof | Metadata presence is not raw-source availability |
| `src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs` | DI contains repositories and B4 orchestration, but no raw source/vault/provider/resolver | There is no server composition path to resolve bytes |
| repository-wide provider scan | No blob/S3/object-store/raw-source reader or writer implementation was found in `src/` | A provider cannot be inferred from naming or dormant columns |

The `CaptureArtifactRow.VaultRef` column and domain `VaultRef` value type are
schema/model placeholders. Because the only live application writes are null
and no resolver consumes them, they are not a source capability.

### 4.2 CaptureAgent

| Evidence | Observation | Consequence |
| --- | --- | --- |
| `SensitiveByteBuffer.cs:3-27` | Raw arrays are wrapped, zeroed, and invalidated on `Dispose` | The existing custody contract is bounded memory |
| `CaptureAgentOrchestrator.cs:55-56,227-228` | Chip and face results are `using`-scoped | DG2, selfie, and liveness buffers die when the run exits |
| `CaptureAgentOrchestrator.cs:493-500` | Every mode except `VerifyAndDiscard` throws `RETENTION_POLICY_UNSUPPORTED` | Raw retention is intentionally disabled |
| `CaptureAgentProductionConfigGate.cs:209-216` | The production configuration gate independently rejects non-discard retention | Production cannot enable a raw source through configuration |
| `CaptureAgentPorts.cs:98-102` | `RawVault` exists only as an enum value | It is a future vocabulary token, not an implemented provider |
| `Hn212CccdReader.cs:59-71` | NFC aggregate and SOD buffers are cleared after verification/hash use | They are not available to a later job |
| `Hn212CccdReader.cs:121-127` | DG15, AA response, and challenge buffers are cleared | AA material has no durable or post-run source |
| `TagEkycHttpClient.cs` and request factories | Only capture/evidence metadata and hashes are submitted to TagEkyc | No hidden raw upload path exists |

The sibling CaptureAgent worktree had unrelated pre-existing changes. This
spike read it without modifying, staging, cleaning, or relying on those changes
as accepted TagEkyc capability.

## 5. Current data flow

```text
device/card/camera
→ CaptureAgent SensitiveByteBuffer
→ local verification and digest calculation
→ TagEkyc HTTP metadata/hash submission
→ TagEkyc capture/evidence metadata rows (VaultRef = NULL)
→ CaptureAgent zero/dispose
```

There is no edge from the B4 job to a raw source:

```text
B4 job ──X──> raw resolver/provider
```

Therefore a B4 permit/job cannot currently be fulfilled with raw bytes, even
when the authorization metadata is valid.

## 6. Raw-class availability matrix

`RawExportRawClass` is a policy vocabulary, not evidence that every class has a
producer. Current status is:

| Raw class | Current producer observation | Stable class-specific digest/source? | C1 status |
| --- | --- | --- | --- |
| `ChipDg1` | No live extraction/output contract found | No | UNAVAILABLE |
| `ChipDg2Portrait` | DG2 face bytes are present in a `SensitiveByteBuffer` | Digest exists only indirectly through the current NFC artifact path; no retrievable source | TRANSIENT_ONLY |
| `ChipDg13` | No live extraction/output contract found | No | UNAVAILABLE |
| `ChipDg15` | Present temporarily during active-authentication verification | Cleared; no class identity/source | TRANSIENT_ONLY |
| `ChipSod` | Present temporarily during NFC verification | Cleared; no class identity/source | TRANSIENT_ONLY |
| `AaChallenge` | Derived temporarily for active authentication | Cleared; no class identity/source | TRANSIENT_ONLY |
| `AaResponse` | Present temporarily when active authentication succeeds | Cleared; no class identity/source | TRANSIENT_ONLY |
| `LiveSelfieImage` | Selfie bytes are present in a `SensitiveByteBuffer` | Hash metadata exists; no retrievable source | TRANSIENT_ONLY |
| `LivenessMedia` | Liveness bytes are present in a `SensitiveByteBuffer` | Hash metadata exists; no retrievable source | TRANSIENT_ONLY |
| `HandSignatureImage` | No producer or capture surface found | No | UNAVAILABLE |

No class is `SOURCE_READY`. C1 must use an explicit supported subset and fail
closed for every other class; policy taxonomy alone must never trigger a
fallback or a mislabeled aggregate.

## 7. Consent and source-establishment ordering

The landed B2 contract permits a subject export consent `Granted` event only
after the verification session is `Completed`. It is export authorization, not
retroactive permission to retain buffers captured earlier.

Current order:

```text
capture raw
→ verify
→ submit metadata/evidence
→ complete session
→ CaptureAgent run exits and buffers are zeroed
→ B2 export consent may be granted
→ B3 authorization and permit
→ B4 job
```

This creates two distinct, unresolved source paths:

1. **Durable source modes:** a separately approved pre-capture
   retention/legal-basis gate must authorize encrypted source establishment.
   Later B2 export consent authorizes export use; it cannot legalize earlier
   retention retroactively.
2. **`ExternalExportOnlyNoRetain`:** one bounded same-process coordinator would
   have to retain the still-live buffers through completion, B2 consent,
   authorization, B4 bind/lease, C1 assembly, and one submission attempt. No
   such protocol, callback, worker bridge, or authority revalidation path exists
   today.

Neither path may be invented by the Builder. Planning must choose and specify
one before dispatch.

## 8. Feasibility verdict

| Question | Verdict |
| --- | --- |
| Is a real Raw BIO producer present? | PARTIAL — transient buffers exist for some classes |
| Is a retrievable approved source present? | NO |
| Is a raw handoff contract present? | NO |
| Is durable raw retention enabled? | NO — explicitly fail-closed |
| Can B4 jobs resolve bytes today? | NO |
| Can C1 planning proceed? | YES |
| Can C1 implementation be dispatched from current facts? | NO |
| Can raw/provider production enablement be claimed? | NO |

The C1 concept is feasible only as a coordinated source-establishment plus
sealed-assembly slice. It is not feasible as a server-only resolver added to the
current TagEkyc repository because there is nothing for that resolver to read.

## 9. Mandatory Planning Brief decisions

Before a C1 build dispatch, the Planning Brief must close all of the following:

1. **Initial mode:** choose the first supported `RawExportMode`; do not claim all
   three modes.
2. **Supported class subset:** name only classes with an implemented producer
   and reject every unsupported authorized class before raw read.
3. **Source-establishment topology:** define where encrypted bytes first become
   durable, or define the complete no-retain same-process sequence.
4. **Producer contract:** bind producer identity, client application, session,
   session challenge, subject, artifact id, raw class, digest, byte length,
   media type, capture time, and source expiry.
5. **Authority ordering:** distinguish pre-capture retention authority from
   post-completion B2 export consent and revalidate B3/B4 authority at every
   required checkpoint.
6. **Source-at-rest protection:** preserve a provider-neutral raw-artifact
   storage port; use an S3-compatible adapter against pinned MinIO as the first
   reference implementation; keep encrypted filesystem as a separately
   qualified single-node alternative; and select the key boundary for durable
   modes without confusing it with C2 recipient encryption. Reference-provider
   implementation is not production-provider approval.
7. **Worker boundary:** define which isolated component owns source credentials,
   bounded read, assembly, cleanup, and stale-fence rejection.
8. **Seal contract:** define canonical manifest encoding, artifact order,
   digest/size/media-type verification, assembly identity, authentication, and
   crash residue.
9. **Lifecycle:** define source TTL, deletion, orphan cleanup, cancellation,
   corruption, partial assembly, and recovery behavior.
10. **Readiness:** require supported mode × class × provider × key capability;
    fixtures or metadata-only references cannot satisfy readiness.
11. **Cross-repository scope:** explicitly authorize the exact CaptureAgent
    files/surfaces needed for producer handoff; a TagEkyc-only allowlist is
    insufficient.
12. **No-public-reader boundary:** preserve no general raw-reader API, no
    runtime/business-client vault credential, no raw bytes in PostgreSQL, and
    no production provider enablement before C2.

Production-provider qualification, legal approval, retention approval, and
deployment credentials remain Homeowner decisions. The provider-neutral port
and S3-compatible first reference adapter are already accepted planning
direction; a drafter must not silently promote that reference adapter to a
production provider.

## 10. Planning STOP/RRI gates

STOP/RRI if any draft:

- treats `VaultRef`, a nullable database column, hash metadata, or an enum as a
  real source;
- assumes B2 export consent retroactively authorizes capture-time retention;
- retains raw bytes before a separately approved source-establishment gate;
- claims `ExternalExportOnlyNoRetain` while staging durable raw or package data;
- enables a provider using fixture-only evidence;
- maps one aggregate NFC hash to several raw classes without class-specific
  bytes and digest proof;
- permits a class not implemented by the selected producer;
- gives the API/runtime process source-provider credentials;
- exposes a general raw read/download interface;
- keeps a database transaction or B4 lease lock across device, source, or
  provider I/O;
- omits stale-fence, consent withdrawal, permit/job expiry, corruption, or
  cancellation checkpoints; or
- attempts implementation before the source topology and cross-repository
  allowlist are ratified.

## 11. Required pilot matrices for the Planning Brief

The Planning Brief must contain:

- an Invariant Trace Matrix for every source, authority, integrity, custody,
  assembly, disposal, and no-leak requirement;
- an Outcome and Precedence Matrix covering authority expiry/withdrawal,
  unsupported capability, source absence, digest mismatch, stale fence,
  cancellation, and crash;
- a Shape and Nullability Matrix for source descriptors, artifact variants, and
  sealed assembly outcomes;
- an Ordering Graph for producer establishment, B2/B3/B4 checkpoints, source
  read, assembly seal, and cleanup across fresh/retry/reclaim paths; and
- a Test-Bite Matrix with positive controls and discriminating mutations for
  class binding, integrity, source ownership, buffer disposal, no-residue, and
  stale-worker rejection.

## 12. Recommended next action

At planning opening, create TIP-88C1 Planning Brief v0.1 under the active
PI-TAG-001 pilot with initial status
`DRAFT — STORAGE DIRECTION RECORDED — MODE/CLASS/KEY DECISIONS REQUIRED — NOT DISPATCHED`.

This is a historical opening instruction. The current Planning Brief status is
authoritative after later review rounds.

The first planning round should compare:

- a durable encrypted source established through a TagEkyc-owned custody
  boundary before CaptureAgent buffer disposal, using the accepted
  provider-neutral port and S3-compatible reference adapter; and
- the stricter same-process `ExternalExportOnlyNoRetain` sequence.

The Planning Brief must return the initial mode, production topology, supported
class subset, key boundary, and cross-repository authority to the Homeowner as
explicit decisions before implementation readiness can be claimed. It must not
reopen the accepted provider-neutral abstraction merely because production
provider qualification remains pending.
