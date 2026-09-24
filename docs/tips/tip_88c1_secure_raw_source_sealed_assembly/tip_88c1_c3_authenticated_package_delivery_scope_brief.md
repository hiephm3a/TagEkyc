# TIP-88C1-C3 — Authenticated Package Delivery Scope Brief

**Version:** 0.2.1  
**Status:** ROUND-1 NARROW CLOSURE CANDIDATE — DOCS ONLY — NOT RATIFIED — NOT DISPATCHED  
**Date:** 2026-08-18  
**Owner:** Homeowner + Codex Contractor/Drafter  
**Baseline commit:** `7dd15cca6b2159d08220167fe5f0b4cf95fca195`  
**Baseline branch:** `tip-88a-raw-export-policy-catalog-build`  
**Change authority:** `DOCS_ONLY_SCOPE_BRIEF_AND_APPEND_ONLY_REVIEW_LEDGER`  
**Implementation authority:** `NONE`

## 0. Changelog

### v0.2.1 — Narrow `IntegrityUnavailable` ownership correction

- Accepts `C3-R1-v02-F10`: v0.2 correctly made
  `IntegrityUnavailable` terminal for one `DeliveryId`, but one paragraph
  accidentally claimed a package-global denial without any package-level
  durable authority or create-admission predicate.
- Selects delivery-local semantics. The failed `DeliveryId` cannot retry, reset
  or reopen. A new idempotency key may authorize a new `DeliveryId` for the same
  immutable package and must perform a fresh full provider read and integrity
  verification with no inherited success or failure authority.
- Adds no package tombstone, C2 mutation, new route, new state or package-level
  gate. Repeated deterministic failure is an accepted bounded trade-off for the
  narrow reference slice and remains fail-closed before response start.

### v0.2 — Round-1 reconciliation candidate

- Accepts both independent v0.1 reviews after source verification; no reviewer
  finding is rejected. Stable ledger ids preserve both reviewers' native ids
  without collision.
- Splits create-replay, status and content precedence so historical replay and
  status do not silently reauthorize content or disappear after later key
  revocation, completion or expiry.
- Adds terminal `IntegrityUnavailable` for known pre-response absence or
  integrity failure and reserves `OutcomeUnknown` for genuinely ambiguous
  stream completion/process-loss outcomes.
- Pins fail-fast process-spool admission before any durable stream transition,
  with no delivery mutation when capacity is unavailable.
- Binds completion receipts to the exact authenticated content-stream attempt
  and requires a stable non-empty principal for all C3 delivery operations.
- Makes delivery-reader configuration/readiness C3-owned and additive, with C2
  remaining independently valid when C3 is disabled or unconfigured; records
  exact S3, DB-capability and SQL-role census deltas.
- Defers PackageId discovery/issuance to
  `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` with an exact closure condition and a
  product/pilot activation gate; C3 remains reference-presented only.
- Corrects predecessor bookkeeping by treating the included C2 as-built status
  as a historical snapshot and binding the landed C2 commit separately in the
  review bundle.

### v0.1 — Round-1 candidate

- Re-anchors C3 to the landed C1 authenticated assembly and C2 recipient-
  encrypted package implementation.
- Defines C3 as authenticated, direct-recipient delivery of one exact C2
  `Finalized` encrypted package.
- Selects a two-step durable delivery contract: idempotent authorization,
  followed by exact delivery-content streaming and a sanitized status/receipt
  projection.
- Pins direct recipient identity, current exact key-eligibility recheck,
  non-enumeration, package-state gating, lock order, post-lock admission time,
  restart behavior and server-stream-completion semantics.
- Prohibits public/presigned URLs, package listing/search, range/HEAD delivery,
  provider locators, TagEkyc CEK recovery and recipient-management APIs.
- Requires package integrity verification into a bounded encrypted-memory spool
  before the first HTTP response byte is committed.
- Activates the Homeowner three-round documentation convergence boundary for
  C3 and creates the sibling append-only review ledger before review round 1.

## 1. Authority and source hierarchy

This artifact is a docs-only scope candidate. It does not authorize source,
test, project, package, API, schema, migration, provider, runtime, environment,
deployment, stage, commit, push, merge or production work.

Precedence is:

1. current explicit Homeowner instruction;
2. accepted TIP-88C1 planning and ratified/landed C1 and C2 contracts;
3. this C3 scope candidate after independent review and Homeowner ratification;
4. later C3 executable dispatch, only after separate authority.

Source anchors at drafting time:

| Source | SHA-256 | Use |
| --- | --- | --- |
| `tip_88c1_planning_brief.md` | `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` | C1/C2/C3 ownership and raw-reader prohibition |
| `tip_88c1_c2_recipient_package_scope_brief.md` | `B48AECEEEB2CEA2B71A6BA2951BEC7339195DA10AB72713AD0D834051FC56851` | C2/C3 boundary and delivery recheck intent |
| `tip_88c1_c2_recipient_package_build_dispatch.md` | `0790D1A6828C3365178011F0E10A1B58757B4FE7E2214828C55A6D720B0BB64D` | executable C2 state, custody and proof contract |
| `tip_88c1_c2_recipient_package_as_built.md` | `C36855C44C221A1744570E9943B1659F462DE50169BD61A3BF89DDEA997B95AA` | historical C2 closeout snapshot; its pending-review label is superseded by landed-commit provenance |
| `tip_88c1_c2_recipient_package_review_ledger.md` | `C4721DD2EA5D2B1910E0B46914EB6DBD7FEA5E9C1CE39DFC2DD9080802423511` | C2 finding provenance and open debt |
| `AuthenticatedClientContext.cs` | `4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401` | landed caller identity/category/scope model |
| `IApiKeyAuthenticator.cs` | `72A7E14658FFC4F3990D87CA590CD2A19084FC012C3D0813A2351A12750F9CF4` | landed API authentication boundary |
| `VerificationSessionEndpoints.cs` | `3D5EB692A759087651DBB973C1607B826994580A94FFDD392311A933A98A2B64` | existing API pipeline/error-envelope convention |
| `RecipientPackageContracts.cs` | `F3882BFCCA3309610F0D46A151131569BF06DC642F30C2A5342D142CDD50B7F5` | landed package locator/read interfaces and state mapping |
| `RecipientPackageOptions.cs` | `F0BE59B691CD2E8F0C04F4AB1B6AFF7EE82BE9449870A033940B4045CD11DCE3` | landed package topology and four-credential boundary |
| `S3CompatibleRecipientPackageProvider.cs` | `0F5D1CA658CA62D6B2B261FDC3FEEABE438ABEF8839F9FAA6634E5407321502D` | exact-object read feasibility and provider behavior |
| `RawExportRecipientKeyRegistrationRow.cs` | `8D73CA3C4ED2F2C43C62A5D754918D60EE554DA551EEB6014D7588C6B207E903` | exact key status/revision/validity source |
| `RawExportRecipientPackagePreparationRow.cs` | `AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E` | finalized package and frozen key/object binding source |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | landed schema/state/ACL feasibility |

The C2 scope/dispatch files remain exact reviewed authority evidence even when
their local Git tracking status differs from the landed implementation commit.
The baseline commit `7dd15cca6b2159d08220167fe5f0b4cf95fca195` and a bundle-
local landed-provenance record are authoritative for the fact that C2 was
reviewed, committed and pushed. The included C2 as-built bytes are retained as
historical evidence; their stale pending-review header is not projected as the
current C2 lifecycle state.

## 2. Current landed facts

At baseline, C2 already provides:

- one immutable `PackageId` and package equality fingerprint per exact C2
  preparation;
- exact `RecipientClientApplicationId` and frozen recipient key id/version,
  fingerprint, public key, revision and validity window;
- exact package profile, provider configuration identity, endpoint fingerprint,
  bucket, object key and object-binding digest;
- exact envelope digest, encrypted-package length, package ciphertext digest,
  provider receipt and conditional-create evidence after successful prepare;
- a `Finalized` state and `FinalizedAtUtc` after the exact C1 seal is committed;
- bounded single-part S3-compatible exact-object reads through internal
  provider ports;
- no TagEkyc-side package CEK unwrap/recovery capability.

The landed C2 provider has writer, reconciler, lifecycle and posture-probe
credentials. It has no delivery-reader credential, no public delivery port and
no API route. C3 adds exactly:

```text
S3 provider credentials: 4 -> 5
DB capabilities:         3 -> 4
SQL capability roles:    3 -> 4
SQL LOGIN roles:         3 -> 4
```

The added member is delivery-only in every census. It is not a rename or reuse
of a C2 reconciler/lifecycle capability.

## 3. Product objective

C3 lets the exact recipient client authenticate to TagEkyc, authorize one
bounded delivery session for one exact C2-finalized encrypted package, stream
that package through TagEkyc without exposing its provider locator, and query a
durable sanitized server-delivery result.

Successful flow:

```text
BusinessConsumer API key with business.raw-export.package.download
-> direct caller ClientApplicationId
-> POST exact PackageId + Idempotency-Key
-> exact recipient-scoped Finalized package lookup
-> exact frozen recipient-key registration lock and current eligibility check
-> one post-lock admission clock
-> durable Authorized delivery identity
-> GET exact DeliveryId/content under the same direct recipient identity
-> fail-fast acquire one of two process spool permits
-> repeat package/key/session lock and eligibility checks
-> durable Streaming attempt/fence committed before provider I/O
-> exact object read through delivery-only credential
-> bounded encrypted-memory spool
-> length + package digest + envelope digest verification before response start
-> exact no-store application/octet-stream response
-> durable ServerStreamCompleted event and receipt digest after server copy ends
```

The successful result proves TagEkyc completed writing the verified encrypted
package to the HTTP response stream. It does **not** prove that the recipient
received every network byte, persisted the package, unwrapped the CEK,
decrypted the package or accepted its contents.

## 4. Exact C3 boundary

### 4.1 C3 owns

- direct recipient authentication and the dedicated
  `business.raw-export.package.download` scope;
- exact recipient-scoped package authorization without cross-recipient
  existence disclosure;
- current eligibility recheck of the exact frozen recipient key before
  delivery authorization and before each stream attempt;
- durable delivery identity, idempotency, lifecycle, attempt fence and
  append-only events;
- exact finalized-package object resolution behind an internal contract;
- a distinct delivery-reader provider credential;
- bounded pre-response integrity verification of encrypted package bytes;
- full-object HTTP streaming, interruption classification and restart-safe
  retry from byte zero;
- sanitized status and server-stream-completion receipt projections;
- API pipeline, database, object-provider, restart, ACL and mutation proofs.

### 4.2 C3 does not own

- raw source or C1 plaintext access;
- C1 assembly creation, C2 encryption, re-key, package replacement or package
  cleanup;
- recipient-key enrollment, rotation, revocation commands or management UI;
- recipient private keys, package CEK unwrap, escrow or decryption;
- package list/search/discovery, bucket/object locators or presigned URLs;
- range, resumable, multipart, `HEAD`, copy, rename or cacheable downloads;
- client acknowledgements, recipient signatures, proof of possession,
  proof of decryption or legal non-repudiation;
- callbacks, webhooks, outbox delivery, email/SMS notifications or client SDKs;
- real Raw BIO adapter activation, production provider qualification,
  deployment or compliance approval.

## 5. API contract selected for Round 1

Initial C3 exposes exactly three BusinessConsumer routes:

```text
POST /api/ekyc/raw-export/packages/{packageId:D}/deliveries
GET  /api/ekyc/raw-export/deliveries/{deliveryId:D}
GET  /api/ekyc/raw-export/deliveries/{deliveryId:D}/content
```

No collection route is mapped. No route returns a provider URL, bucket, object
key, access key, internal credential, public key bytes, private material or C2
provider receipt.

This v0.2.1 candidate selects **reference-presented delivery**: the authenticated
recipient must already possess the opaque `PackageId` through an authorized
business exchange. `PackageId` possession is not authority; direct recipient
authentication and every C3 admission check still apply. The current baseline
has no public TagEkyc surface that issues that reference, so this bounded slice
must not claim end-to-end package discovery or notification. The missing
reference-distribution/inbox surface is registered as
`C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` and is
`DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY`. It closes only when a separately
authorized authenticated surface permits the exact
`RecipientClientApplicationId` to list or receive opaque `PackageId` references
owned by that recipient without disclosing another recipient's package. Until
then, only manual or authorized out-of-band reference presentation is possible;
the debt blocks a real recipient/hospital pilot and any end-to-end delivery UX
claim, but does not block this authorization-correctness reference slice.

### 5.1 Create delivery

The POST requires `Idempotency-Key`:

- 1–128 visible ASCII characters after exact validation;
- raw value is never persisted or logged;
- durable state stores only SHA-256 of the exact UTF-8 bytes;
- same recipient + same idempotency digest + same PackageId is exact replay;
- same recipient + same idempotency digest + different PackageId is conflict.

Identity derivation selected for the executable dispatch:

```text
DeliveryId = DeterministicGuid(
  "tip-88c1-c3-delivery-id-v1",
  RecipientClientApplicationId,
  IdempotencyKeyDigest)
```

The package remains a mandatory equality member even though it is not repeated
inside `DeliveryId`; reuse against another package is durable conflict, never a
second delivery row.

New creation returns `201 Created`. Exact replay returns `200 OK`. Both return
the same sanitized delivery projection. No response contains a content URL
with a provider signature; the only content route is the fixed TagEkyc route.

After authentication, required scope/category/principal validation and exact
idempotency shape validation, create precedence is:

```text
recipient + idempotency digest already exists
  + same PackageId
    -> 200 exact persisted projection
    -> no package/key reauthorization and no durable mutation
  + different PackageId
    -> 409 idempotency conflict; existing row unchanged

no delivery identity exists
  -> require current exact recipient-scoped Finalized package
  -> require current exact frozen-key eligibility
  -> create one Authorized row or return the exact denial
```

Later key revocation/expiry, delivery expiry, `IntegrityUnavailable` or
`ServerStreamCompleted` does not erase historical exact replay. Replay is not
permission to stream content and does not refresh an authorization deadline.

### 5.2 Delivery status

The status projection contains only:

```text
DeliveryId
PackageId
State
AuthorizedAtUtc
AuthorizationExpiresAtUtc
StreamAttemptCount
ServerStreamCompletedAtUtc? 
EncryptedPackageLength
PackageCiphertextDigest (base64url)
DeliveryReceiptDigest? (base64url)
```

It does not contain recipient key identifiers/fingerprints, C1/C2 internal
ids, object binding, provider receipt, endpoint, bucket or object key.

After authentication, required scope/category/principal validation and the
recipient-scoped delivery lookup, status returns the persisted projection for
every durable state, including `Expired`, `IntegrityUnavailable`,
`OutcomeUnknown` and `ServerStreamCompleted`. Status does not recheck current
key eligibility, perform provider I/O or authorize content.

### 5.3 Content response

The content route returns only a full package:

```text
200 OK
Content-Type: application/octet-stream
Content-Length: exact EncryptedPackageLength
Content-Disposition: attachment; filename="tagekyc-package-{PackageId:D}.t88pkg"
Cache-Control: no-store
Pragma: no-cache
X-Content-Type-Options: nosniff
```

HTTP compression is disabled. `Range`, `If-Range` and partial-content behavior
are unsupported. An authenticated owner request carrying a Range header is
rejected before provider I/O. `HEAD` is not mapped. A retry starts from byte
zero and re-verifies the entire object.

## 6. Caller identity and non-disclosure

- The caller category is exactly `BusinessConsumer`.
- `PrincipalId` is required to be a stable non-empty UUID for every C3 route.
  Missing/empty principal identity returns
  `RAW_EXPORT_PACKAGE_DELIVERY_PRINCIPAL_REQUIRED` / 403 before package or
  delivery state is created, changed or disclosed.
- The authenticated caller's `ClientApplicationId` must equal the package's
  `RecipientClientApplicationId` and the delivery row's recipient id.
- `AllowedClientApplicationIds`, `OperatorAdmin`, `CaptureAgent`,
  `TrustedAdapter`, package creator/requester identity and API-key prefix do not
  grant delivery in this initial slice.
- The client cannot submit a recipient id in headers, query, body or route.
- Package and delivery queries include the authenticated recipient id in the
  database predicate. They do not load a cross-recipient row and reject it in
  application memory.
- Missing package, wrong recipient, non-finalized package, aborted/quarantined
  package and missing delivery all produce the same sanitized not-found result
  after authentication. No state-specific detail crosses that boundary.
- Exact-recipient key ineligibility may return a stable delivery-ineligible
  conflict without exposing another recipient's package existence.

## 7. Recipient-key eligibility and linearization

C3 rechecks the **exact frozen key registration**, not the current/latest key:

```text
RecipientClientApplicationId
RecipientKeyId
RecipientKeyVersion
PublicKeyFingerprint
PublicKeyAlgorithm
Revision
State
ValidFromUtc
ValidUntilUtc
```

Delivery is eligible only when the registration still matches the immutable C2
snapshot, `State = Active`, algorithm remains `RSA-OAEP-256`, and the one
post-lock admission clock is inside `[ValidFromUtc, ValidUntilUtc)`.

Rotation to a newer key does not invalidate an older package while its exact
frozen key remains active and in-window. Revocation, expiry, missing row,
fingerprint/algorithm drift or ambiguous catalog state blocks new delivery
authorization and new stream admission. It does not mutate or re-key C2.

Concurrency rule:

- exact recipient key row locks before exact package row;
- package locks before delivery row;
- after all required blocking locks, capture `clock_timestamp()` once;
- no blocking lock follows that capture;
- revalidate recipient, key, package state/revision and delivery state using
  that same time;
- if revocation commits first, admission fails;
- if stream admission commits first, that admitted bounded stream may finish;
  later revocation blocks the next admission.

No database transaction spans provider read, spool verification or HTTP I/O.

## 8. Delivery lifecycle and timing

Required states:

```text
Authorized
Streaming
Interrupted
IntegrityUnavailable
OutcomeUnknown
ServerStreamCompleted
Expired
```

Semantic rules:

- `Authorized`: durable authorization exists, no provider read has begun.
- `Streaming`: one exact attempt/fence won admission before provider I/O.
- `Interrupted`: a known pre-completion provider/read/client-cancellation path
  ended without server-stream completion; retry may start from byte zero.
- `IntegrityUnavailable`: positive object absence or a deterministic
  length/digest/envelope/package/object-binding failure was proven before the
  response started; no receipt exists and the same `DeliveryId` cannot retry.
  This state is delivery-local, not a package-global tombstone. C3 does not
  repair/quarantine/mutate the C2 package; operational repair or replacement
  remains predecessor-owned.
- `OutcomeUnknown`: process/host loss or ambiguous response completion prevents
  a claim that the server stream completed; no receipt is created.
- `ServerStreamCompleted`: the verified spool was copied to the server response
  stream and exact byte count/digest checks completed; immutable terminal state.
- `Expired`: no new stream may begin after the authorization deadline.

Fixed initial profile to be reproduced exactly by the dispatch:

```text
DeliveryAuthorizationLifetime = 30 minutes
DeliveryStreamOperationLimit   = 30 minutes
DeliveryStreamLeaseDuration    = 35 minutes
MaximumConcurrentSpools        = 2 per process
MaximumEncryptedPackageLength  = 33,557,106 bytes (landed C2 bound)
```

These are current-profile constants, not hot-reloadable behavior. If future
configuration is required it needs an immutable per-delivery profile snapshot
and a separately ratified gate.

At most one attempt for one `DeliveryId` may be `Streaming`. Retry after
`Interrupted` increments attempt count and fence. An expired/stale `Streaming`
attempt is never silently called completed; restart reconciliation classifies
it `OutcomeUnknown`. A new delivery authorization with a new idempotency key is
required after terminal completion, expiry, `IntegrityUnavailable` or
irreducible uncertainty. For `IntegrityUnavailable`, the new identity does not
reset or reopen the terminal delivery and receives no authority from it. It may
target the same immutable `PackageId`, but must perform a fresh full provider
read and verification and may deterministically fail again before response
start. C3 persists no package-global integrity denial in this initial slice.

Process-spool admission is fail-fast and carries no authority:

```text
authenticate/scope/category/principal + validate request shape
-> try-acquire one of exactly two process spool permits
-> if unavailable: RAW_EXPORT_PACKAGE_DELIVERY_CAPACITY_UNAVAILABLE / 503
   with no lock, attempt-count, fence, state, lease or event mutation
-> if acquired: begin the key -> package -> delivery locked admission
-> release the permit on every normal or exceptional exit
```

The permit is acquired before `Authorized|Interrupted -> Streaming`; a row is
never made `Streaming` merely to wait for process memory capacity. POST create
and GET status do not acquire a spool permit.

C3 does not claim exactly-once network delivery. Multiple separately authorized
deliveries of the same immutable package to the same recipient are legal and
independently audited.

## 9. Integrity-before-response boundary

Before committing response headers or bytes, C3 must:

1. load only an exact finalized recipient-scoped package projection;
2. build the exact landed C2 locator internally;
3. use a distinct delivery-reader credential;
4. read no more than the landed encrypted-package bound into a segmented,
   pooled, encrypted-ciphertext memory spool;
5. compute and compare exact encrypted length, package ciphertext digest and
   envelope digest against C2 durable metadata;
6. reject extra bytes, early EOF, malformed envelope, wrong package identity,
   wrong object binding or any provider/configuration drift;
7. seal the spool before response start;
8. stream the sealed spool once while counting/hashing the server copy;
9. zero all pooled segments and transient digest scratch on normal and handled
   exceptional exits.

The spool contains only the already recipient-encrypted package. It is bounded
process memory, never a durable plaintext or ciphertext cache, file, database
blob, distributed cache or restart authority. Process loss drops it.

No error discovered after response start is converted into a false JSON success
or a false completed receipt. A known copy failure/cancellation is
`Interrupted`; only a lost process/host or genuinely ambiguous response-
completion boundary is `OutcomeUnknown`.

## 10. Delivery receipt and audit meaning

`DeliveryReceiptDigest` uses a C3-specific canonical domain and binds at least:

```text
DeliveryId
PackageId
RecipientClientApplicationId
DeliveryAttemptNumber
DeliveryFence
PackageCiphertextDigest
EncryptedPackageLength
AuthorizedAtUtc
StreamStartedAtUtc
ServerStreamCompletedAtUtc
AuthenticatedApiKeyId
AuthenticatedPrincipalId
```

The exact canonical codec and an absolute vector are Round-2 dispatch
obligations. The receipt is derived from persisted/runtime output state, not
from echoed request input.

`AuthenticatedApiKeyId` and `AuthenticatedPrincipalId` are the exact identity
that authenticated the successful **content-stream attempt**, not necessarily
the identity that created the delivery authorization. The authorization row
separately preserves its creator API-key/principal ids. Each attempt durably
binds its own non-empty stream API-key/principal ids before provider I/O; the
receipt uses the ids from the exact completed attempt/fence.

The receipt means only:

```text
SERVER_STREAM_COMPLETED_FOR_EXACT_VERIFIED_ENCRYPTED_PACKAGE
```

It is not named `RecipientReceipt`, does not contain a recipient signature and
does not prove network receipt, storage, decryption, viewing, acceptance or
legal acknowledgement. A future acknowledgement protocol requires a separate
contract and trust decision.

Audit events bind the exact API key id, principal id, recipient client,
delivery/package ids, outcome, timestamps and correlation identity. Raw API
keys, idempotency keys, package bytes, provider locators and recipient public
key material are never logged or returned.

## 11. Route-specific outcome and error precedence

Authentication, required scope/category/non-empty-principal checks and request-
shape validation precede all three tables. Recipient-scoped lookup precedes any
state-specific disclosure. Higher rows within one route table win; a row from
one route must never be projected onto another route.

### 11.1 CreateDelivery

| Order | Condition | HTTP/result | Durable effect |
| --- | --- | --- | --- |
| 1 | API key missing/invalid | existing authentication error / 401 | none |
| 2 | scope/category/principal invalid | stable authorization error / 403 | none |
| 3 | malformed PackageId or Idempotency-Key | stable validation error / 400 | none |
| 4 | existing recipient/idempotency row + different PackageId | `RAW_EXPORT_PACKAGE_DELIVERY_IDEMPOTENCY_CONFLICT` / 409 | existing row unchanged |
| 5 | existing recipient/idempotency row + same PackageId | exact persisted projection / 200 | no reauthorization or mutation |
| 6 | missing/wrong-recipient/non-finalized package | uniform not found / 404 | none |
| 7 | exact frozen key revoked/expired/mismatched | `RAW_EXPORT_PACKAGE_DELIVERY_INELIGIBLE` / 409 | denial audit only |
| 8 | key/catalog/readiness indeterminate | `RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` / 503 | no authorization mutation |
| 9 | exact new authorization | sanitized projection / 201 | one `Authorized` row/event |

### 11.2 GetDeliveryStatus

| Order | Condition | HTTP/result | Durable effect |
| --- | --- | --- | --- |
| 1 | API key missing/invalid | existing authentication error / 401 | none |
| 2 | scope/category/principal invalid | stable authorization error / 403 | none |
| 3 | malformed DeliveryId | stable validation error / 400 | none |
| 4 | missing/wrong-recipient delivery | uniform not found / 404 | none |
| 5 | exact recipient-owned delivery in any durable state | sanitized persisted projection / 200 | none |

### 11.3 GetDeliveryContent

| Order | Condition | HTTP/result | Durable effect |
| --- | --- | --- | --- |
| 1 | API key missing/invalid | existing authentication error / 401 | none |
| 2 | scope/category/principal invalid | stable authorization error / 403 | none |
| 3 | malformed DeliveryId | stable validation error / 400 | none |
| 4 | missing/wrong-recipient delivery/package | uniform not found / 404 | none |
| 5 | unsupported Range/If-Range on proven owned delivery | `RAW_EXPORT_PACKAGE_RANGE_NOT_SUPPORTED` / 416 | none |
| 6 | persisted `Expired`, `IntegrityUnavailable`, `OutcomeUnknown` or `ServerStreamCompleted` | stable terminal result / 410 | no permit or provider I/O |
| 7 | another stream attempt owns live lease | `RAW_EXPORT_PACKAGE_DELIVERY_IN_PROGRESS` / 409 | no permit; row unchanged |
| 8 | process spool permit unavailable for an apparently admissible row | `RAW_EXPORT_PACKAGE_DELIVERY_CAPACITY_UNAVAILABLE` / 503 | no delivery mutation |
| 9 | exact frozen key revoked/expired/mismatched after locked revalidation | `RAW_EXPORT_PACKAGE_DELIVERY_INELIGIBLE` / 409 | denial audit only; permit released |
| 10 | key/catalog/readiness state indeterminate | `RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` / 503 | no stream mutation; permit released |
| 11 | provider transient/unavailable before response start | unavailable / 503 | attempt `Interrupted`; byte-zero retry remains legal |
| 12 | positive absence or deterministic length/digest/envelope/binding failure before response | integrity unavailable / 503 | terminal `IntegrityUnavailable`; no response bytes/receipt |
| 13 | known client cancellation/copy failure after response start | transport terminates | `Interrupted`; no receipt |
| 14 | process/host loss or genuinely ambiguous completion | transport/status only | stale `Streaming` reconciles to `OutcomeUnknown`; no receipt |
| 15 | exact verified server copy completes | 200 full object | `ServerStreamCompleted` + receipt |

The dispatch must reuse the existing API error-envelope shape and pin exact
public codes/messages. Raw SQLSTATE, provider status, object existence,
recipient key id/version and internal state do not cross the API boundary.

## 12. Shape and nullability matrix

| State | Required | Nullable | Forbidden |
| --- | --- | --- | --- |
| `Authorized` | identity, recipient/package binding, idempotency digest, authorization/expiry, revision | attempt/start/completion/receipt | provider locator in public projection |
| `Streaming` | all Authorized fields, attempt number, fence, start, lease expiry | completion/receipt | second live attempt |
| `Interrupted` | attempt/fence/start, interruption kind/time, revision | receipt/completion | claim of client receipt |
| `IntegrityUnavailable` | attempt/fence/start, exact integrity-failure kind/time, revision | receipt/completion | same-delivery retry or C2 repair mutation |
| `OutcomeUnknown` | attempt/fence/start, classification time, revision | receipt/completion | exact completion claim |
| `ServerStreamCompleted` | exact byte count/digest, completion time, receipt digest | none of terminal evidence | retry/reopen under same DeliveryId |
| `Expired` | authorization/expiry, terminal time/revision | attempt fields when never started | provider I/O after expiry |

Every mandatory field uses `IS NOT NULL` before equality/length predicates;
every forbidden field uses `IS NULL`. SQL `UNKNOWN` must never satisfy a state
shape or transition guard.

## 13. Capability and trust graph

```text
BusinessConsumer recipient
  -> authenticates through the landed API-key boundary
  -> can authorize/query/stream only its own package/delivery
  -> receives encrypted package bytes and sanitized receipt projection

C3 delivery coordinator
  -> sees recipient-scoped package metadata and delivery state
  -> cannot enroll/revoke recipient keys or mutate C2 package custody

C3 delivery DB capability
  -> executes exact SECURITY DEFINER delivery functions
  -> has no table DML and no C2 prepare/reconcile/lifecycle capability

C3 delivery-reader provider credential
  -> GetObject for the exact package bucket/prefix
  -> no Put/Delete/List/Copy/Presign/Multipart/Bucket administration

C2 writer/reconciler/lifecycle/posture credentials
  -> remain distinct and are not usable by C3 delivery

ordinary unrelated runtime / CaptureAgent / TrustedAdapter / OperatorAdmin
  -> no delivery authority
```

The delivery-reader credential is a fifth distinct package-provider credential.
It is configured and validated through **C3-owned additive delivery options and
readiness**, not by making the landed C2 four-credential
`RecipientPackageOptions.Resolve()` require a fifth member. When C3 is disabled
or unconfigured, C2 preparation/reconciliation/lifecycle/posture remains
independently valid and its four-credential readiness result is unchanged.
When C3 is enabled, C3 readiness requires the compatible C2 package topology,
the exact delivery-reader identity and policy, the fourth DB capability role
and fourth LOGIN-to-capability membership. It fails closed if the new provider
identity aliases writer, reconciler, lifecycle or posture-probe identity, or if
effective policy admits a forbidden operation.

## 14. Persistence and evidence boundary

Round 2 may add only the minimum durable delivery tables/events/functions
needed for the lifecycle above. It must not add:

- raw package bytes, public key bytes copied from C2, private key material,
  provider credentials or object locator columns to public/general tables;
- a package list/search index for runtime discovery;
- mutable C2 package or recipient-key fields owned by C3;
- an outbox, callback queue or notification subsystem;
- recipient-management commands.

The C3 package projection must be selected through an exact recipient-scoped,
finalized-state database function. C3 does not grant its login direct SELECT on
C2 tables. The function may return the internal locator only to the delivery
coordinator under the delivery capability; the API DTO never contains it.

Append-only delivery events are the durable source for attempt/outcome audit.
The status projection is derived from persisted delivery state, not request
echo or process memory.

## 15. Required semantic traces

### 15.1 Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract surface | Outcome/error | State/evidence/residue | Named proof class |
| --- | --- | --- | --- | --- | --- | --- | --- |
| C3-I01 | direct recipient only | API + application | auth -> scope -> category -> recipient-scoped lookup | three fixed routes | 403/404 without existence leak | no row on denial | pipeline + cross-recipient mutation |
| C3-I02 | finalized package only | DB projection | key -> package locks, post-lock clock | exact read function | uniform 404 otherwise | C2 unchanged | state-widen mutation |
| C3-I03 | exact frozen key remains eligible | DB admission | key lock before package, one clock | create/begin functions | 409 or 503 | denial audit only | revoke/expiry/concurrency mutations |
| C3-I04 | durable idempotent delivery identity | DB delivery | validated idempotency digest | create result/DTO | Created/ExistingMatch/Conflict | one row | equality-member mutations |
| C3-I05 | one live stream attempt | DB delivery | spool permit -> key -> package -> delivery, CAS fence | begin/retry | CapacityUnavailable/InProgress/Expired/Terminal | append-only attempt event | capacity/concurrency/fence mutations |
| C3-I06 | exact delivery-only object read | provider adapter | committed stream attempt | internal locator | unavailable/integrity | no locator leak | credential/prefix/list/presign mutations |
| C3-I07 | verify before first response byte | coordinator/spool | full bounded read -> verify -> seal | content endpoint | IntegrityUnavailable/503 before start | zeroized spool | early-write/digest/extra-byte mutations |
| C3-I08 | honest completion semantics | coordinator + DB | exact stream actor -> server copy completes -> terminal CAS | status DTO | ServerStreamCompleted | receipt digest binds completed-attempt API key/principal | crash/cancel/actor/receipt-rename mutations |
| C3-I09 | restart has no process authority | DB + provider | stale stream classified from durable state | retry/status | Interrupted/OutcomeUnknown | no false receipt | in-memory cache mutation |
| C3-I10 | no private-key/CEK recovery | architecture | all paths | dependency graph | build/arch RED | none | forbidden type/member mutation |
| C3-I11 | no discovery/presign/range | API + adapter | route/member census | public surface | 404/416 | none | route and SDK-member mutations |
| C3-I12 | readiness/capabilities separated | composition + catalog | exact topology/ACL/policy | readiness | exact fail-closed code | no startup capability when RED | role/ACL/IAM mutations |

### 15.2 Ordering graph

All create, replay, stream, retry and reconciler paths use:

```text
authenticate/scope/category
-> validate route/idempotency shape
-> recipient-scoped probe without blocking lock where needed
-> fail-fast spool permit for content only
-> exact recipient-key row lock
-> exact finalized package row lock
-> exact delivery row lock when present
-> one clock_timestamp()
-> revalidate key/package/delivery/deadline
-> durable CAS/event
-> commit
-> provider read / spool / HTTP outside DB transaction
-> terminal CAS/event
```

No path may lock delivery before package or package before recipient key. No
blocking lock may follow the admission clock. Provider/network I/O never occurs
while a database or advisory lock is held.

### 15.3 Test-Bite Matrix

| Claim | Positive control | Broken mechanism | Required RED discriminator |
| --- | --- | --- | --- |
| direct recipient | owner downloads | remove recipient predicate / accept delegated id | cross-recipient request becomes 200 instead of uniform 404 |
| finalized only | exact Finalized package | admit Prepared/Quarantined | forbidden state authorizes delivery |
| key recheck | active exact frozen key | skip state/window/fingerprint comparator | revoked/expired/drifted key authorizes |
| lock/time freshness | revoke and delivery interleave | old timestamp or lock reversal | post-wait expiry/revoke incorrectly admits or deadlocks |
| idempotency | exact replay | drop PackageId equality | key reuse against another package exact-matches |
| spool capacity | one permit available | transition Streaming before/without permit | capacity denial mutates attempt/fence/state/event |
| preverify | exact object | write response before digest/envelope verification | response starts for tampered/extra/truncated object |
| integrity classification | deterministic mismatch | map mismatch to Interrupted/OutcomeUnknown or retry same DeliveryId | expected delivery-local terminal IntegrityUnavailable disappears |
| integrity ownership | Delivery A becomes IntegrityUnavailable, then new idempotency creates Delivery B for same package | add package-global denial or reuse A's outcome/read bytes | B is blocked without authority or avoids its own fresh provider read/verification |
| exact read capability | delivery reader only | reuse reconciler or add List/Presign | architecture/IAM proof observes forbidden capability |
| completion honesty | exact copy and durable terminal state | mark complete before copy/after ambiguous crash or bind POST actor | receipt exists without server copy completion or exact stream actor |
| restart | recover from durable row/object | process cache supplies package/state | restart proof passes without provider/DB read |
| no CEK recovery | recipient private unwrap only in test fixture | add production decrypt/unwrap dependency | architecture proof RED |

## 16. Risk modules

| Module | Applicability | Required review focus |
| --- | --- | --- |
| SQL/schema/transaction | Required | sparse states, lock order, fresh time, CAS, ACL, apply/Down/reapply |
| API | Required | auth/scope/category, non-disclosure, pipeline behavior, response-start boundary, cancellation |
| Worker/queue | Partially applicable | restart reconciliation only; no queue/outbox/notification worker is authorized |
| Cryptography/key management | Required | exact frozen-key eligibility and receipt digest; no CEK/private-key path |
| Raw/restricted data | Required | only encrypted package bytes, bounded buffers, locator/log/DTO prohibition |
| Governance/docs | Required | C2/C3 boundary, no production/readiness/receipt overclaim, three-round ledger |

## 17. Validation and proof expectations

Round-2 dispatch must pin an exact allowlist and named proof manifest covering at
least:

- API pipeline tests for all three routes, not service-only tests;
- create replay after completion/expiry/revocation and status over every durable
  state, proving neither path reauthorizes content;
- direct-recipient and cross-recipient non-disclosure;
- stable non-empty principal enforcement and completed-attempt actor binding;
- exact finalized-state and frozen-key eligibility;
- revoke/expiry/concurrency interleavings and lock-order deadlock probes;
- idempotency replay/conflict and delivery state sparse constraints;
- fail-fast spool-capacity denial with zero durable mutation;
- provider exact-read, no List/Presign/Range and credential isolation;
- tamper/truncate/extra-byte/envelope mismatch before response start;
- `IntegrityUnavailable` versus `Interrupted`/`OutcomeUnknown` discriminators;
- cancellation before/after response start and process-loss classification;
- restart recovery without process cache;
- exact receipt vector and persisted evidence source;
- architecture prohibition of package unwrap/private keys/CEK recovery;
- readiness, owner/ACL/effective membership and exact IAM policy;
- migration apply/Down/reapply, pending-model and snapshot tripwire;
- affected tests while building and one unfiltered Release suite on final
  closeout bytes only.

Tests must be count-neutral where strengthening an existing predecessor proof
is sufficient. New C3 behaviors require named C3 tests. A green test that can
pass through an earlier generic denial, request echo, mock-only read or
unconditional failure requires a discriminating mutation.

## 18. TIP Analytical Summary / Intent Ledger

### Intent

Deliver one exact recipient-encrypted C2 package through an authenticated,
non-enumerable TagEkyc boundary with honest durable server-stream evidence.

### Expected Outcome

After a later controlled build, an exact BusinessConsumer recipient can create
an idempotent delivery authorization, stream only its exact finalized package,
and query a sanitized durable result. No provider locator, recipient private
material or raw/plaintext source becomes reachable.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| direct recipient only | smallest auditable authority | no delegated/operator delivery | no recipient-management claim |
| two-step delivery session | durable idempotency/restart/receipt boundary | three exact routes | no package listing |
| exact frozen key recheck | C2 explicitly makes revocation a delivery gate | read-only key/package admission | no re-key or C2 mutation |
| verify encrypted object before response | prevents knowingly emitting corrupt package bytes | bounded memory spool | no plaintext buffering |
| full-object retry only | keeps initial integrity/state model bounded | no Range/HEAD/resume | no heavy-media readiness |
| server-stream-completion receipt | matches observable server evidence | durable digest/event | no client possession/decryption proof |
| C3-owned fifth delivery credential | preserves least privilege without reverse dependency | S3 4->5, DB capability 3->4, SQL role pairs 3+3->4+4 under C3 options/readiness | C2 remains valid with C3 disabled/unconfigured |
| route-specific precedence | preserves idempotent history while gating bytes | replay/status survive later key/state changes; content does not | replay is not reauthorization |
| IntegrityUnavailable terminal | separates known corruption/absence from ambiguous completion | deterministic pre-response failure cannot same-delivery retry | C3 does not repair C2 |
| delivery-local integrity ownership | avoids inventing an unratified package tombstone | a new DeliveryId may target the same package but must read/verify from scratch | repeated deterministic fail-closed work is accepted |
| fail-fast spool admission | process capacity is not durable failure | permit precedes locked Streaming CAS | no queue/attempt burn on capacity denial |
| completed-attempt actor receipt | binds the operation that emitted bytes | non-empty principal and per-attempt actor fields | no claim that POST creator streamed |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| presigned/public provider URL | Rejected | leaks provider surface and bypasses C3 admission/audit | none for initial C3 |
| package list/search | Rejected | creates discovery/existence surface | separate product decision |
| delegated/operator download | Deferred | authority semantics not ratified | `C3-DELEGATED-DELIVERY-V1` |
| Range/resumable delivery | Deferred | complicates integrity, receipt and concurrency | heavy-media transport slice |
| recipient-signed acknowledgement | Deferred | requires recipient signing identity/protocol | `C3-RECIPIENT-ACK-V1` |
| server/package publisher signature | Deferred | C2 envelope v1 only reserves slots | separate cryptographic decision |
| recipient enrollment API | Deferred | C2 uses deployment-seeded registry | recipient-management slice |
| recipient inbox/reference issuance | `DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY` | no public PackageId channel exists at baseline; reference possession is not authority | `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1`; blocks real pilot/end-to-end UX until authenticated recipient-owned reference surface exists |
| package-global integrity tombstone | Rejected for initial C3 | would require separate durable package-level authority and create/content admission semantics | future bounded abuse/operational policy decision if needed |
| production provider/raw adapter activation | Deferred | separate operational/legal evidence | production activation gates |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| C2 delivery absent | owned by C3 | scope opened | C3 dispatch/build/review |
| C2 key revocation delivery gate | made executable at C3 admission | exact frozen-key recheck | concurrency proof |
| C2 no delivery credential | add distinct read-only credential | least privilege preserved | readiness/IAM proof |
| client receipt ambiguity | terminology narrowed | no overclaim | optional `C3-RECIPIENT-ACK-V1` |
| PackageId issuance absent | keep reference-presented C3 only | authorization correctness remains testable; product discovery is not | `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` closure condition in §5 |
| C2 as-built header stale | preserve bytes as historical snapshot and bind landed commit separately | predecessor status is not inferred from stale header | bundle-local C2 landed-provenance record |
| inherited production/GOV/ART debt | unchanged | remains open | existing activation gates |

### Non-Claims

This scope is not implementation, production readiness, a real Raw BIO
adapter, provider qualification, delivery availability, PackageId discovery,
recipient notification/inbox, recipient possession, decryption proof, legal
receipt, compliance approval or deployment authority.

### Dispatch Readiness

Round 1 is review-only. Round 2 may author an executable dispatch only after:

- independent reviewers bind the exact v0.1/v0.2 bytes and review this v0.2.1
  reconciliation successor;
- every blocker/latent implementation gap is registered and adjudicated;
- exact routes, state semantics, precedence, lock/time model, receipt meaning
  and evidence sources remain internally consistent;
- Homeowner explicitly authorizes Round-2 dispatch authoring.

## 19. Three-round convergence boundary

The sibling ledger is append-only from round 1.

- Round 1: full-system scope review with coverage attestation.
- Round 2: executable dispatch review against the reconciled scope.
- Round 3: exact-byte closure review.

After round 3, do not continue whole-document patch loops. If no contract
blocker remains, freeze the dispatch and move executable uncertainty to code
and discriminating tests under separate build ratification. If a genuine
contract contradiction, unsafe boundary or required out-of-scope path remains,
STOP/RRI for a Homeowner decision; code must not be used to bypass it.

Every round records reviewer identity, exact artifact SHA, finding ids,
classification, disposition, successor anchor and stale-sentinel sweep.

## 20. Round-1 review request

Reviewers must read this complete artifact and the source anchors, then report:

- lifecycle/state correctness;
- API authentication, direct-recipient authorization and non-disclosure;
- hash/audit/receipt meaning and idempotency;
- lock order, revocation timing, retry, restart and response-start behavior;
- provider credential and raw/restricted-data boundaries;
- repo-real feasibility against landed C2 and API authentication code;
- builder ambiguity and any unresolved implementation-affecting choice;
- exact finding classification:
  `BLOCKER`, `PATCH_REGRESSION`, `LATENT_SPEC_GAP`,
  `TEST_HARDENING_ONLY`, `BOOKKEEPING_ONLY` or `DEFERRED`.

A zero-finding verdict must identify inspected files/surfaces, at least three
plausible risks considered and remaining uncertainty. A PASS does not ratify
this scope or authorize implementation.

## 21. Explicit boundaries

No source, tests, API route, schema, migration, provider operation, package,
environment, stage, commit, push, merge, PR, deployment, production activation,
real Raw BIO, recipient management, decryption, client acknowledgement or
delivery execution is authorized by this document.
