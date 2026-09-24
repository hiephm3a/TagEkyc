# TIP-88C1-C2 — Recipient Encryption + Encrypted Package Custody Scope Brief

Status: `ROUND_1_CLOSED — READY_FOR_ROUND_2_EXECUTABLE_DISPATCH`
Version: `0.2`
Date: `2026-08-17`
Repository: `TagEkyc`
Branch: `tip-88a-raw-export-policy-catalog-build`
Baseline: `d9735f84b3946887d8301dda9686a7303aa71683`
Change authority: `DOCS_ONLY_SCOPE_BRIEF_AND_REVIEW_EVIDENCE`
Implementation authority: `NONE`

## 0. Changelog

### v0.2 — Round 1 closure

- Reconciles two independent reviews bound to v0.1 SHA
  `E9C94C5AA989907CDA3727BC1FAC34E95794BAFC993FE0E75522239B2636A57E`.
- Corrects the temporal model: C2 prepares a provisional encrypted package
  before C1 seal and recognizes final custody only after exact seal/finalize.
- Selects a trusted C1-to-C2 handoff; C2 does not independently verify the C1
  manifest/authentication value.
- Declares the only approved landed-C1 contract delta:
  `C2AssemblyPreparationRequest.RecipientClientApplicationId`.
- Freezes the recipient-key snapshot at successful Prepare and makes later
  revocation a delivery gate, not an in-place re-key operation.
- Selects recipient-controlled decryption with no usable TagEkyc post-Prepare
  CEK recovery capability.
- Separates C1 preparation identity from C2 package identity, and plaintext
  assembly length from encrypted package length.
- Pins ProviderReceiptDigest semantics for Round 2, reserves an envelope
  extension slot, prohibits reuse of the source attempt-KEK pattern, and keeps
  delivery/C3 outside scope.
- Registers all Round-1 findings and counter-claims in the sibling ledger.

### v0.1 — Round 1 candidate

- Re-anchors C2 to the landed C1 authenticated-assembly implementation.
- Defines C2 as recipient encryption, final package identity and encrypted
  package custody; authenticated delivery remains C3.
- Reuses the landed deterministic `C2PreparationId` and exact
  `Prepare/GetPreparation/Finalize/Abort` protocol instead of inventing a
  second handoff.
- Separates recipient-key selection from source encryption, source historic
  commitment and C1 assembly-authentication capabilities.
- Prohibits durable plaintext assembly and plaintext package custody.
- Introduces the Homeowner's three-round documentation-convergence rule and a
  sibling append-only review ledger from the first review round.
- Authorizes no code, migration, provider operation, staging or commit.

## 1. Process authority and convergence boundary

C2 follows the bounded process selected by the Homeowner:

1. **Round 1 — Scope / product / trust.** Review this brief for the C2 product
   boundary, recipient-key authority, package custody, disclosure posture and
   explicit deferrals.
2. **Round 2 — Executable dispatch.** Reconcile accepted findings into one
   on-code dispatch with an exact path allowlist, schemas, roles, provider
   protocol, outcomes, codecs and discriminating proofs.
3. **Round 3 — Exact-byte closure.** Review one exact candidate SHA. Fix only
   contradictions that make implementation unsafe, impossible or outside the
   ratified authority.

After Round 3, remaining executable uncertainty moves to code and
discriminating tests. A later document round is allowed only for a genuine
contract, product-authority or allowlist contradiction and must be a narrow
append-only correction. Editorial improvement and speculative hardening are
recorded as debt, not used to reopen the full brief.

Every review, finding, claim, disposition and successor SHA must be recorded in
`tip_88c1_c2_recipient_package_review_ledger.md`. A review that is not present
in that ledger cannot silently be treated as closed.

Validation policy for a later authorized build:

- changed/affected tests during implementation;
- expensive predecessor suites only when their owned surface changes or a
  failure implicates them;
- exactly one complete unfiltered Release suite on final closeout bytes;
- no failed, stale or differently configured run substitutes for closeout.

## 2. Authoritative inputs

| Input | C2 use |
|---|---|
| Landed C1 Resolver + Authenticated Assembly | Immutable assembly identity/items, exact authenticated manifest, deterministic C2 preparation and monotonic disposition |
| `RawExportAssemblyContracts.cs` | Exact current `Prepare/GetPreparation/Finalize/Abort` handoff and closed fixture outcomes |
| B3/B4 authorization and job lineage | Recipient client, permit, policy, purpose, attempt/revision/fence and expiry provenance |
| B2 AUTH snapshot | Fresh-authority-required and extension-forbidden posture |
| Landed R1–R6 / Durable Key / Durable Object | Source custody only; not automatically package custody authority |
| GOV/ART chain | Lifecycle, retention, purge, hold, audit and package-completeness gates |
| TIP-88C1 planning brief D8 / ART-003 | C2 ownership and final-package-completeness boundary |

Exact current hashes and review-chain references belong in the sibling ledger.
The baseline commit is authoritative; stale planning claims do not override
landed code.

## 3. On-code boundary

### 3.1 Landed and reusable

- C1 persists one immutable authenticated assembly and ordered item set.
- C1 registers a deterministic `C2PreparationId` as `Preparing` before provider
  I/O and moves the disposition monotonically through
  `Pending`, `SealCommitted`, `AbortAuthorized`, `Finalized` or `Aborted`.
- C1 calls C2 `Finalize` only after the exact committed seal references that
  preparation; an unknown seal cannot authorize abort.
- The landed C2 port has exact operations:
  `PrepareAsync`, `GetPreparationAsync`, `FinalizeAsync`, `AbortAsync`.
- The fixture C2 provider proves response-loss and replay shape but is not a
  production package provider and retains no production-recipient package.
- C1 already prevents ordinary runtime from selecting raw sources, reading
  object locators, unwrapping source keys or sealing assemblies.

### 3.2 Not landed

- no recipient-key enrollment or immutable recipient-key snapshot;
- no production/reference recipient-encryption provider;
- no final encrypted-package format or package identity codec;
- no package-specific durable object reservation, metadata state machine or
  exact package read/delete capability;
- no package completeness proof closing ART-003;
- no package retention/hold/purge policy binding;
- no package provider readiness topology or separated credentials;
- no public delivery handle, download route, receipt, delivery ledger or C3
  authentication.

These absences define C2. They must not be hidden behind the fixture sink or a
process-memory dictionary.

## 4. Product objective

C2 converts exactly one trusted C1 candidate assembly into one provisional
recipient-encrypted package before seal, then recognizes that package as final
custody only after the exact C1 seal is committed and the same preparation is
finalized. It never persists the plaintext assembly or plaintext package.

The intended successful flow is:

```text
C1 registers exact C2PreparationId as Preparing
-> trusted Prepare request carries RecipientClientApplicationId
   plus the exact candidate fingerprints/digests and bounded canonical writer
-> C2 atomically selects and freezes one eligible recipient-key snapshot
-> C2 creates one non-deliverable provisional encrypted package
-> C2 Prepare returns an exact receipt; C1 records Pending
-> C1 commits SealCommitted plus immutable assembly identity/items
-> C2 Finalize re-binds the provisional package to the exact committed seal
-> package becomes custody-complete but remains non-deliverable
```

C2 does not temporally encrypt an already sealed assembly. It encrypts the
exact candidate stream before seal, and its provisional result cannot become
final custody unless the exact `C2PreparationId` is later named by the C1
committed seal. Success means C2 can prove the package bytes, frozen recipient-
key lineage, candidate/committed-assembly lineage and durable custody state
after restart. Success does not authorize delivery or expose a locator.

## 5. Exact slice boundary

### 5.1 C2 owns

- immutable recipient-key enrollment/snapshot identity sufficient to encrypt
  and later prove which recipient key/version was used;
- fail-closed key status, algorithm/profile compatibility and historic
  verification metadata;
- streaming recipient encryption of the exact C1 assembly stream;
- a byte-pinned encrypted-package envelope and final package identity;
- explicit separation of identities:
  - C1 owns `C2PreparationId`, derived exactly by the landed C1 codec from
    `AssemblyId` and `AssemblyFingerprint`; C2 does not redefine it and it has
    no independent recipient-key dimension;
  - C2 owns an immutable package identity/equality fingerprint that binds the
    supplied `C2PreparationId`, assembly candidate binding,
    `RecipientClientApplicationId`, the frozen recipient-key snapshot and the
    package profile;
  - same C1 preparation plus the same package equality set is exact replay;
    the same preparation with any different C2-owned member is conflict;
- conditional single-part durable package write and exact response-loss
  recovery;
- encrypted-package metadata, evidence and custody lifecycle;
- exact `Prepare/GetPreparation/Finalize/Abort` behavior compatible with landed
  C1;
- zeroization of bounded plaintext/key scratch on handled paths;
- package-specific readiness, capability separation and fixture/reference
  provider evidence;
- ART-003 reference-package evidence for this bounded synthetic slice, without
  closing the production / real-data ART-003 gate.

### 5.2 C2 does not own

- raw capture, source selection or source custody;
- modification of the canonical C1 assembly bytes or manifest semantics;
- recipient authentication at download time;
- public package discovery, URL generation, download, delivery, receipt,
  callback, retry or outbox behavior;
- legal/compliance approval for real patient data;
- production activation of S3, HSM, KMS, PKI or recipient directories;
- multipart, versioning, Object Lock or heavy-media transport;
- client-controlled provider keys, object locators, encryption profiles or
  package identifiers.

C3 owns authenticated delivery. A production raw-source adapter remains a
separate pre-production activation slice and is not smuggled into C2.

## 6. Recipient-key authority

- C1 to C2 is a trusted internal handoff. C2 consumes the exact candidate
  fingerprints/digests and does not independently reconstruct or verify
  `ManifestDigest` or `AssemblyAuthenticationValue` before encryption. It must
  not add a second C1 verification projection, repository read port or
  authentication path.
- The recipient client comes only from the server-derived
  `C2AssemblyPreparationRequest.RecipientClientApplicationId`, whose value is
  copied from the already authorized C1 job lineage. Runtime callers cannot
  submit or replace it.
- The landed seven-field request versus the planning thirteen-field assumption
  is recorded contract drift. Round 2 corrects only the approved one-field
  recipient handoff; the other absent fields are not implementation defects.
- A caller cannot submit a recipient key, key id, certificate, algorithm or
  provider locator as part of `Prepare`.
- Key selection returns an immutable snapshot containing, at minimum,
  recipient client, key identity/version, public-key fingerprint, algorithm
  profile, validity window, revocation/status evidence and selector schema.
- The reference enrollment is a deployer-seeded public-key/descriptor registry
  behind an explicit production gate. Recipient or delegated-administrator
  lifecycle authority is modeled, but C2 adds no recipient-management API.
  TagEkyc never enrolls or possesses the recipient private key, KEK or any
  equivalent decryption capability.
- Snapshot admission and key revocation serialize on the same exact registry
  key identity/revision. After required locks, C2 captures one current clock,
  validates recipient/status/window/profile, and atomically inserts the
  immutable snapshot plus the package-operation identity before provider I/O.
  That successful insert is the recipient-snapshot linearization point:
  - if revocation commits first, Prepare fails closed before streaming;
  - if snapshot admission commits first, that package keeps the snapshot and
    later revocation cannot mutate or re-key it;
  - no database lock is retained across provider or encryption I/O.
- Replay loads the persisted snapshot and never re-resolves current/latest key.
- A missing, ambiguous, expired, revoked, indeterminate or incompatible key is
  fail-closed and creates no final package.
- Rotation/revocation after successful Prepare blocks later delivery while the
  key is ineligible, but cannot reverse an exact `SealCommitted` preparation or
  create a replacement package under the same `C2PreparationId`.
- A replacement-key package requires a new authorized RawBio export job against
  the same source transaction, and only while authorization and retention still
  permit access.
- Recipient encryption is cryptographically and operationally separate from
  source DEK/KEK, historic commitment, subject-token and C1 assembly-
  authentication keys.
- C2 requires encryption-capable public material only. Private recipient
  decryption keys are outside TagEkyc custody and must never enter the process,
  database, provider metadata, logs or evidence.

Round 2 must select the exact reference recipient-key mechanism and algorithm
profile from on-code/package evidence. Round 1 does not silently choose a
production PKI, KMS or vendor.

## 7. Streaming and cryptographic boundary

- C2 consumes only the exact canonical assembly writer tied to the registered
  `C2PreparationId` and immutable C1 assembly fingerprint.
- The complete plaintext assembly must never be durably stored or buffered as
  one process-memory array.
- Encryption is bounded streaming with explicit maximum plaintext, ciphertext
  and operation duration.
- `CompleteAssemblyLength` remains the landed plaintext canonical-assembly
  length. `EncryptedPackageLength` is a distinct C2 durable field including
  envelope/framing/AEAD/wrapped-key overhead. No content-length check or
  conditional write may conflate them.
- The immutable, self-contained package carries or cryptographically binds:
  package identity, C1 assembly/provenance binding, recipient identity,
  recipient key identity/version/fingerprint, encryption metadata, encrypted
  payload, encrypted-package length and package integrity/authentication
  binding. Server metadata may support lifecycle/index/audit but is not needed
  to reconstruct package identity or provenance.
- Self-contained does not mean recipient-verifiable TagEkyc publisher
  authentication. Envelope v1 reserves explicit algorithm and extension slots
  for a later signature decision without claiming a package-signature
  capability now.
- The C2 encryption component generates a per-package CEK and every nonce from
  a CSPRNG inside the cryptographic boundary; provider/object adapters receive
  only the resulting envelope/ciphertext and protected metadata. Caller input,
  configuration and provider metadata cannot supply CEK or nonce bytes.
  Idempotency uses durable operation identity and exact recovery, never nonce
  reuse or deterministic AEAD under the same key.
- The CEK is wrapped solely for the frozen recipient capability. After
  successful Prepare, TagEkyc retains no usable CEK unwrap/recovery path: no
  `WrappedCEKForTagEkyc`, package-recovery KEK or escrow. Restart reconciliation
  verifies exact stored ciphertext/envelope evidence without decrypting it.
- The landed source-side attempt-KEK pattern must not be cloned for packages.
  Round 2 must include a capability proof whose mutation introduces a TagEkyc
  package-CEK unwrap path and turns RED even if encryption/replay still pass.
- Plaintext chunks, content-encryption keys and cryptographic scratch are
  zeroized on normal and handled exceptional exits. Abrupt process loss is not
  falsely described as active zeroization.
- Logs, metrics, exceptions, audit and public/general-runtime DTOs contain no
  plaintext, package ciphertext, recipient public-key bytes, private key,
  content-encryption key, provider credential or object locator.

## 8. Package custody and crash protocol

The provider and metadata stores do not commit atomically. Round 2 must pin a
reservation/provisional protocol that covers crashes:

1. before recipient-snapshot linearization;
2. concurrent revoke versus snapshot linearization;
3. after snapshot linearization but before provider write, including a later
   revocation that must not re-key this operation;
4. during provider write;
5. after provider success before metadata acknowledgement;
6. after metadata acknowledgement before C1 seal observation;
7. after C1 `SealCommitted` before C2 finalize response;
8. during abort/cleanup and after lost cleanup response.

Required properties:

- deterministic package operation identity is persisted before provider I/O;
- conditional create prevents replacement of a different package;
- response loss is resolved by exact identity/evidence lookup, not list/scan;
- same identity + same immutable fingerprint returns exact replay;
- same identity + different fingerprint is conflict;
- `OutcomeUnknown` never authorizes a second encryption attempt that could
  reuse or fork cryptographic state;
- a persisted snapshot is immutable; current-key lookup, rotation or revocation
  cannot replace it on replay;
- abort is allowed only after landed C1 `AbortAuthorized` for the exact
  preparation;
- finalized package custody cannot be replaced or aborted;
- incomplete/obsolete resources are invisible to C3 and reconciled through
  actor-specific capabilities;
- package deletion is exact-key, single-object and evidence-backed; batch,
  list, copy, presign, multipart, versioning and Object Lock remain prohibited
  unless a later slice explicitly opens them.

There is no same-preparation package generation or re-key path. If a revoked
snapshot makes the package unusable for delivery, remediation is a new
authorized export operation/job that re-derives the canonical assembly from
still-authorized encrypted source custody. Durable plaintext recovery and
decrypting the old recipient package are both forbidden.

## 9. State and outcome classes

Round 2 must derive exact names and precedence, but may not omit these classes:

- preparation registered / replay match / preparation conflict;
- recipient key unavailable / indeterminate / invalid / incompatible;
- provider unavailable / outcome unknown;
- conditional-create existing match / object conflict;
- package metadata conflict / stale revision or fence;
- prepared custody pending C1 seal;
- seal committed, finalization pending;
- finalized / exact finalized replay;
- abort authorized / aborted / cleanup pending;
- authority or expiry loss before external mutation;
- authority loss after committed seal, which cannot fork the exact preparation
  and must converge to custody finalization while delivery remains denied.

No raw provider exception, locator or recipient-key existence detail may cross
the restricted boundary.

## 10. Capability and trust graph

At minimum:

```text
ordinary runtime / API
  -> cannot resolve recipient keys, read C1 assembly streams, mutate package
     custody, access object locators or finalize/abort preparations

C2 coordinator
  -> reads exact preparation and invokes narrow key/encryption/custody ports
  -> receives no provider credentials or private recipient key

recipient-key resolver
  -> resolves exact recipient snapshot
  -> cannot read assembly/package bytes or finalize custody

package writer / reconciler
  -> conditionally writes or inspects exact package identity
  -> cannot choose recipient/assembly or deliver package

package lifecycle
  -> exact cleanup only after durable authority
  -> cannot create, finalize or deliver package

C3 (future)
  -> may consume only a finalized package through a separate authenticated
     delivery contract
```

Exact roles, ownership, ACLs, credential separation and effective-membership
proof are Round-2 obligations. Reusing raw-source writer/reconciler/lifecycle
credentials for package custody is prohibited unless an exact equivalence is
explicitly ratified; naming similarity is not equivalence.

## 11. Freshness, locking and replay

- Exact finalized replay is checked from immutable persisted equality surfaces
  before any provider/key/plaintext work.
- New work locks and validates the exact C1 preparation/assembly lineage,
  recipient-key snapshot and package reservation without inverting landed C1
  lock order.
- A blocking wait cannot leave a pre-wait authority, key-validity or deadline
  timestamp as the admission decision.
- Provider/key I/O occurs outside database transactions.
- After provider I/O, mutations use exact revision/fence/fingerprint CAS.
- `Finalize` and `Abort` races serialize on the exact preparation/package
  lineage; one cannot infer victory from a process-local flag.
- Recovery after restart uses only durable context and exact provider evidence.

## 12. Required durable metadata

The exact schema belongs to Round 2. Recovery must be able to re-establish:

- `C2PreparationId`, `AssemblyId`, job/attempt/fence lineage;
- immutable assembly/manifest fingerprints and the trusted request's
  authentication value, without a second C1 verification path;
- recipient client and immutable recipient-key snapshot identity;
- package profile/version, deterministic package identity and operation token;
- recipient-only wrapped CEK metadata, with no TagEkyc unwrap selector, escrow
  or package-recovery KEK;
- encrypted package object identity, distinct plaintext/encrypted lengths,
  ciphertext digest, envelope digest and package-integrity evidence;
- state, revision, timestamps, provider receipt/inspection evidence;
- finalization or abort authority and cleanup evidence.

No durable field may be introduced solely because it is convenient for a
fixture. No value required after restart may exist only in process memory or
inside a non-reversible digest.

The landed `ProviderReceiptDigest` is not an arbitrary provider blob. It is the
32-byte SHA-256 digest of a Round-2 byte-pinned canonical provisional-package
receipt. Its preimage must commit, at minimum, to:

```text
C2PreparationId
C2 PackageId / package equality fingerprint
AssemblyId + AssemblyFingerprint + ManifestDigest
RecipientClientApplicationId
recipient-key snapshot identity/version/fingerprint
package format/profile and reserved extension set
exact encrypted object identity/binding
EncryptedPackageLength + CiphertextDigest + EnvelopeDigest
provider operation identity and successful conditional-create evidence
```

The receipt is created before C1 seal, so it does not claim `SealCommitted` or
delivery. C1 may continue treating the value as opaque 32-byte evidence; C2
owns its semantics, absolute vector, equality/replay and conflict mutations.
Round 2 must prove that changing any member changes the receipt and that a
same-id/different-receipt replay conflicts.

## 13. Preliminary implementation surface

Round 1 authorizes no paths. Round 2 derives an exact allowlist from live code.
Expected categories:

- C2 contracts/options/readiness;
- recipient-key snapshot entities, mappings and repository;
- encrypted-package reservation/custody entities and mappings;
- one additive migration, Designer and ModelSnapshot;
- streaming recipient-encryption and package codec;
- S3-compatible reference package provider with separate credentials;
- C2 coordinator/reconciler/lifecycle services;
- focused unit, architecture and integration proofs;
- C2 as-built and append-only review ledger.

Changes to landed C1 interfaces, schemas, transitions or proofs require an
explicit on-code compatibility claim. A change outside the Round-2 allowlist
is STOP/RRI rather than silent expansion.

The only approved landed-C1 contract delta is:

```text
C2AssemblyPreparationRequest += Guid RecipientClientApplicationId

RawExportAssemblyOrchestrator request construction passes:
job.RecipientClientApplicationId
```

This is a trusted server-derived handoff field. It adds no migration, SQL
function, role, grant, topology member or readiness-census entry. It does not
add `AssemblyAuthenticationKeyId/Version` and does not authorize any other C1
change. Every constructor/proof affected by the record shape belongs in the
Round-2 exact allowlist and must preserve the landed C1 proof census unless the
dispatch explicitly accounts for a count-neutral replacement.

No `RawExportAssemblyTopology` value is added. The C2 provider remains an
injected `IC2AssemblyPreparationProvider`; if Round 2 proves that reference-
provider production composition requires a new topology selector, that is
STOP/RRI rather than a silent enum edit.

Authorized recipient-resolution trace, recorded verbatim:

```text
hop 1  C2PreparationId -> JobId/AttemptId/FencingToken
       EXISTS: raw_export_read_assembly_recovery_context(uuid),
       already GRANTed to tagekyc_raw_export_assembly_sealer, no
       disposition filter, therefore readable while 'Preparing'.
hop 2  JobId -> RecipientClientApplicationId
       NO authorized path for C2's capability. Only two functions in the
       whole repository expose it:
         raw_export_read_job(uuid,uuid,uuid)
           GRANTed to tagekyc_runtime ONLY; requires principal_id and
           client_application_id (C2 has neither); enforces
           raw_export_current_actor() <> principal_id ->
           RAW_EXPORT_JOB_ACTOR_MISMATCH.
         raw_export_read_job_source_verification_context(...)
           GRANTed to the resolver role, actor-guarded.
       Tables are owner-only; no role reads them directly.
```

The rejected alternative (new narrow read function GRANTed to sealer) was
declined on proportionality: it forces the landed ten-function census from
10 to 11 across all six pinned sites (migration create/owner/revoke/grant/
Down, readiness function_check + function_surface_check, C105 grant census,
C124 signature/ACL census, dispatch section 7 manifest, as-built statement)
and grants C2 a new DB read capability, drifting toward the rejected
F02-B model.
This delta RESTORES the ratified planning §8.2 contract, which already
required PrepareAsync to bind RecipientClientApplicationId.

## 14. Round-1 adjudication and Round-2 obligations

Two independent reviews bound exact v0.1 SHA
`E9C94C5AA989907CDA3727BC1FAC34E95794BAFC993FE0E75522239B2636A57E`.
The Homeowner adjudicated their combined findings in authority packet SHA
`0ED9A9A596DAD916F4D8E85BDDB92037ADE1593BF81F7D41EF3B16A21D4B208E`.
Full claims, reviewer attribution and counter-claims are preserved in the
sibling ledger.

| Finding | Round-1 disposition | v0.2 / Round-2 anchor |
|---|---|---|
| C2-R1-F01 | `ACCEPTED_CONTRACT_CHANGE` | §4 two-phase Prepare-before-seal model |
| C2-R1-F02 | `ACCEPTED_TRUST_MODEL_AND_ONE_FIELD_DELTA` | §§6, 13; trusted handoff, recipient only |
| C2-R1-F03 | `HOMEOWNER_DECISION_RESOLVED` | §§6, 8; snapshot/revoke linearization and delivery recheck |
| C2-R1-F04 | `HOMEOWNER_DECISION_RESOLVED` | §15; reference ART-003 evidence only |
| C2-R1-F05 | `HOMEOWNER_DECISION_RESOLVED` | §§7, 12; no TagEkyc CEK recovery |
| C2-R1-F06 | `MANDATORY_ROUND_2_EXECUTABLE_PROOF` | §12 receipt meaning/vector/mutations |
| C2-R1-F07 | `PARTIALLY_REFUTED_WITH_RESIDUAL_DOC_DEBT` | ledger provenance; future C1 status touch |
| C2-R1-F08 | `RESOLVED_NO_CHANGE` | §13; injected provider, no topology enum |
| C2-R1-F09 | `ACCEPTED_EXECUTABLE_DETAIL` | §7; plaintext vs encrypted length |
| C2-R1-F10 | `ACCEPTED_CONTRACT_CLARIFICATION` | §5.1; C1 preparation vs C2 package identity |
| C2-R1-F11 | `RESOLVED_BY_ONE_FIELD_DELTA` | §§6, 13 |
| C2-R1-F12 | `HOMEOWNER_DECISION_RESOLVED` | §§6, 8; new authorized export job only |
| C2-R1-F13 | `ACCEPTED_EDITORIAL_CORRECTION` | §7; cryptographic boundary owns CEK/nonce |
| C2-R1-F14 | `MANDATORY_ROUND_2_CAPABILITY_PROOF` | §7; source attempt-KEK pattern prohibited |
| C2-R1-F15 | `ACCEPTED_FUTURE_PROOFING` | §§7, 12; extension/signature algorithm slots |

Round 2 must now pin exact schema, roles/ACLs, key-registry lifecycle,
encryption/framing profile, package/receipt codecs, provider protocol, state
precedence, lock order, readiness manifest, path allowlist and discriminating
proof census. It must not reopen D1–D7 or add a second C1 verification path.

## 15. Explicit deferrals and non-claims

This scope does not authorize or claim:

- implementation, migration or provider operation;
- real Raw BIO or real patient data;
- production recipient keys, certificates, HSM/KMS or provider activation;
- public package locator, download, delivery receipt, callback or outbox;
- C3 authenticated delivery;
- recipient-management/enrollment APIs;
- recipient-independent TagEkyc publisher signature capability; envelope v1
  only reserves the future extension/algorithm slots;
- any TagEkyc CEK unwrap, recovery, escrow or package-recovery KEK;
- production raw-source adapter;
- multipart, versioning, Object Lock or heavy-media package transport;
- legal/compliance certification, cross-region DR or performance readiness;
- production closure of any inherited GOV/ART debt. The bounded claim is
  exactly:
  `ART-003_REFERENCE_PACKAGE_EVIDENCE_SATISFIED`; the
  `PRODUCTION / REAL-DATA ART-003 GATE REMAINS OPEN`, and production
  ART-004/005/006/007 remain open.

## 16. Round-1 exit criteria

Round 1 closes only when:

- at least two independent reviews bind the exact brief SHA;
- every review and finding is registered in the sibling ledger;
- each finding has a stable id, severity, claim/disposition and successor
  location;
- product/trust questions are answered or explicitly deferred by Homeowner;
- Round 2 can write one executable dispatch without reopening C2/C3 boundary;
- no review artifact or predecessor claim is silently inferred.

Current status:

```text
Independent reviews registered: 2 — GPT and CC, exact v0.1 SHA
Findings adjudicated:           15 / 15
Round-2 dispatch authoring:     AUTHORIZED
Round-2 build authority:        NO
Implementation authorized:     NO
```

The Homeowner packet carries the two reviewer identities, exact target SHA,
combined finding register and adjudication. Individual raw report SHA values
were not supplied in this packet; the ledger records that evidence fact
explicitly rather than inventing or silently inferring report bytes.

## 17. Boundaries

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
