# TIP-88C1-C3 — Authenticated Package Delivery Build Dispatch

Version: `0.2`  
Status: `ROUND_3_EXACT_BYTE_CANDIDATE`  
Baseline commit: `7dd15cca6b2159d08220167fe5f0b4cf95fca195`  
Ratified scope: `TIP-88C1-C3 Authenticated Package Delivery Scope Brief v0.2.1`  
Scope SHA-256: `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80`  
Scope review bundle SHA-256: `205CC41823995F9F882305CF3FE4F139DC2CCD125C7F21770C03A71E27B1E0A1`  
Authority: Homeowner Round-1 ratification and Round-2 dispatch-authoring packet, 2026-08-18  
Implementation authority: `NONE`  
Commit / push / deployment authority: `NONE`

## 0. Changelog

### v0.2 — Round-3 exact-byte reconciliation

- Accepts all independently reported Round-2 findings; no counter-claim was
  required.
- Makes C317 prove real landed C2 Prepare-to-Finalize output through real C3,
  count-neutrally, so C3 cannot pass against self-seeded package metadata.
- Pins exact lifecycle-event actor, correlation and evidence sources, including
  reconciler-generated OutcomeUnknown/Expired events.
- Inserts the Disabled/Invalid topology gate before any C3 DB/provider access
  and narrows the capacity STOP wording to permit authentication plus the
  explicitly nonlocking recipient-scoped pre-probe.
- Defines 30 minutes as the provider/copy cancellation boundary and 35 minutes
  as the completion-CAS grace/lease boundary; late-after-lease completion is
  the existing OutcomeUnknown transition.
- Restores the ratified public Range code
  `RAW_EXPORT_PACKAGE_RANGE_NOT_SUPPORTED`, clarifies public versus provider
  receipt wording, and requires concrete authenticator/validator sources in the
  review bundle.
- Preserves 38 paths, nine SQL functions, seven states, ten transitions, 26
  proof methods and 34 mutations.

### v0.1 — Round-2 executable candidate

- Converts the ratified C3 reference-presented delivery scope into an exact
  schema, SQL, API, provider, readiness, allowlist and proof contract.
- Records Task-0 PrincipalId census as `PASS_ON_LANDED_AUTHORITY`: the
  authenticated API-key path returns persisted `ApiKeyRow.PrincipalId`
  unchanged; C3 rejects `Guid.Empty` and does not derive identity from
  `ClientApplicationId`.
- Pins two C3 tables, nine C3 SQL functions, one capability/LOGIN pair, one
  additive fifth package-reader credential, 38 permanent paths, 26 proof
  methods and 34 scratch mutations.
- Pins absolute idempotency, deterministic delivery-id and delivery-receipt
  vectors.
- Carries `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` as
  `DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY` and anticipates the inherited
  shared-database and line-ending harness debts without weakening closeout.

## 1. Authority and precedence

The authority order is:

1. the Homeowner Round-1 ratification and Round-2 authoring packet;
2. ratified Scope Brief v0.2.1 at the exact SHA above;
3. landed C2 package, C1 assembly and B2 custody contracts at the baseline;
4. this dispatch after a later Homeowner controlled-build ratification;
5. implementation and as-built evidence.

This candidate does not authorize implementation. Review corrections may edit
only this file and append the C3 review ledger. Round 3 is the final exact-byte
documentation correction cycle unless executable work later exposes a genuine
contract contradiction or a required thirty-ninth path.

The following gate remains active:

```text
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1
= DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY
```

C3 is reference-presented only. No collection/inbox/notification route exists.
Manual or separately authorized out-of-band `PackageId` presentation is the
only bounded input. This slice cannot support or claim a real recipient or
hospital pilot, package discovery, or end-to-end delivery UX.

## 2. Task-0 gates and landed feasibility

All gates run before schema or production edits.

### 2.1 Baseline and working-tree gate

Require:

```text
HEAD   = 7dd15cca6b2159d08220167fe5f0b4cf95fca195
branch = tip-88a-raw-export-policy-catalog-build
staged paths = 0
scope SHA-256 = A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80
```

Preserve unrelated dirt. Never use `git add -A` or `git add .`.

### 2.2 Concrete BusinessConsumer principal gate — passed on landed code

The concrete production authentication chain is:

```text
presented API key
-> LocalDevApiKeyAuthenticator
-> IApiKeyStore
-> PostgresHashedApiKeyStore.FindByPresentedKeyAsync
-> ApiKeyRow.PrincipalId
-> ResolvedApiKey.PrincipalId
-> AuthenticatedClientContext.PrincipalId
```

`PostgresHashedApiKeyStore.ToResolved` transfers the persisted UUID unchanged.
Local-development entries already seed explicit non-empty UUIDs. C3 may add the
download scope to its BusinessConsumer fixture, but shall not change
`AuthenticatedClientContext`, `ResolvedApiKey`, `ApiKeyRow`, provisioning
defaults, or authenticator semantics.

Round-3 review evidence must carry exact unchanged baseline bytes and hashes
for both concrete handoff implementations; neither is a permanent-path
allowlist member unless an implementation edit is later required:

```text
src/TagEkyc.Api/LocalDev/LocalDevApiKeyAuthenticator.cs
SHA-256 1BBB32988AB92BFB32C9027E49EE5E4C4EA7B6382395DA073D756574BB630287

src/TagEkyc.Application/LocalDev/LocalDevApiKeyValidator.cs
SHA-256 C9E03F9383CA2991F589ECBA178E2E877231D96DBE7D5D58DE23BC56AC26E9F8
```

C3 runtime rules are exact:

- `PrincipalId == Guid.Empty` returns
  `RAW_EXPORT_PACKAGE_DELIVERY_PRINCIPAL_REQUIRED` / 403 before recipient,
  package or delivery lookup;
- no code substitutes `ClientApplicationId`, `ApiKeyId`, key prefix, subject id
  or a generated UUID;
- C3 integration provisioning supplies an explicit non-empty persisted
  principal;
- a deployed BusinessConsumer key with an empty persisted principal is simply
  ineligible for C3.

Mutation `C3M01` replaces the explicit fixture principal with `Guid.Empty` or
adds a runtime fallback to `ClientApplicationId`; C301 must RED.

### 2.3 Predecessor compatibility gate

Verify exact landed facts before coding:

- C2 package state `Finalized` has non-null `EnvelopeDigest`,
  `EncryptedPackageLength`, `PackageCiphertextDigest`,
  `ConditionalCreateEvidenceDigest`, `ProviderReceiptDigest` and immutable
  object/key snapshot members;
- the package alternate key is exact `PackageId`;
- the key registration composite identity is
  `(RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion)`;
- the object key matches `^raw-export/c2-package/v1/[0-9a-f]{32}$`;
- C2 keeps its four credentials and remains valid when C3 is disabled;
- `RecipientPackageEncryptedSpool` bounds, seals and zeroes pooled ciphertext
  segments and may be reused as an unchanged internal dependency;
- `RecipientPackageCodec.TryReadEnvelopeDigest` and the immutable C2 envelope
  digest make a second provider read or C2 parser change unnecessary.

Any missing fact is STOP/RRI. C3 shall not repair C2, unwrap a CEK, change C2
state, or add a C2 package-global integrity tombstone.

Static census is not executable compatibility evidence. Canonical C317 must
create at least one package through the landed real C2 path:

```text
real C1 assembly fixture
-> RecipientPackagePreparationProvider.PrepareAsync
-> landed C2 reserve/encrypt/conditional Put/Finalize
-> exact persisted Finalized PackageId and immutable metadata
-> C3 create/content through production coordinator and DeliveryReader
-> full C3 pre-response verification
-> ServerStreamCompleted
```

The proof may use fixture-owned recipient enrollment and provider credentials,
but it must not directly insert a Finalized C2 package row, calculate substitute
C2 metadata in the test, or write an object that bypasses C2. C3 receives only
the resulting opaque PackageId. It must obtain length, ciphertext digest,
envelope digest and object binding from the locked C2 projection.

The same count-neutral method owns two independent discriminator subcases:

- C3M25 changes one real C2-produced ciphertext payload byte outside the
  envelope encoding after Finalize while all durable C2 metadata remains
  unchanged. The embedded envelope digest therefore still matches, and only
  removal of the ciphertext-digest comparator can make the scenario escape
  delivery-local `IntegrityUnavailable` before headers/bytes.
- C3M26 starts from a second real C2-produced package, keeps its exact object,
  length and ciphertext digest unchanged, and uses the isolated test-admin DB
  surface to replace only the persisted EnvelopeDigest with a different
  32-byte value. This scratch-only inconsistent input is never a canonical C2
  success substitute. Only removal of the envelope comparator can make the
  scenario escape `IntegrityUnavailable` before headers/bytes.

Each mutation must RED at its own assertion while the neighboring comparator
remains active. Other state-partition tests may use bounded fixtures, but they
cannot substitute for the real C2-to-C3 compatibility success proof.

### 2.4 Harness-debt pre-control

Two inherited debts are active:

```text
SHARED-DB-MIGRATION-STATE-DEBT-01
LINE-ENDING-FRAGILE-COMPARISON-DEBT-01
```

They do not permit a waiver. The initial allowlist includes the disposable
PostgreSQL fixture, migration/Designer/snapshot, E3 tripwire and affected
catalog proofs. Integration tests must clone or create a disposable database
at the canonical current migration, run C3 apply/Down/reapply only in that
database, and drop only that exact database in `finally`. They must never Down
or mutate a shared canonical template.

All catalog/model SQL text comparison normalizes both actual and expected
values to LF using `.ReplaceLineEndings("\n")` before comparison. The test must
also compare the normalized content exactly; normalization cannot delete or
collapse visible whitespace, tokens, predicates or identifiers. Git index LF
normalization is reported separately at commit time and never bypassed.

## 3. Fixed topology, profile and capability census

### 3.1 C3 topology

Add C3-owned `RecipientPackageDeliveryOptions` at:

```text
TagEkyc:RawExport:RecipientPackageDelivery
```

Topology is exactly:

```text
Disabled | S3CompatibleDurable | Invalid
```

Absent topology means `Disabled` only when no other C3 setting is present.
Residue under a Disabled section is invalid. Enabled C3 requires landed C2
`RecipientPackage` topology `S3CompatibleDurable`, the same exact
`ProviderConfigurationId`, endpoint fingerprint, bucket, region and path-style
posture, plus one C3-owned `DeliveryReader` credential. C3 cannot override the
C2 endpoint, bucket, object prefix or provider kind.

The C3 credential access-key id must differ ordinally from all C2 access-key
ids: Writer, Reconciler, Lifecycle and PostureProbe. Secrets are required,
redacted from `ToString`, exceptions, readiness and logs, and never persisted.

When C3 is Disabled:

- C2 resolution/readiness is byte-behavior compatible and needs no C3 value;
- host composition succeeds without a delivery DB login or S3 reader;
- the three routes remain mapped and return
  `RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` / 503 after authentication and
  request-shape checks, with no C3 delivery database/provider access;
- C3 readiness reports not-applicable/green rather than making C2 RED.

Invalid or enabled-but-incomplete C3 is readiness RED and routes fail closed.

### 3.2 Fixed current profile

These are compile-time current-profile constants, not configuration inputs:

```text
DeliveryAuthorizationLifetime = 30 minutes
DeliveryStreamOperationLimit   = 30 minutes
DeliveryStreamLeaseDuration    = 35 minutes
MaximumConcurrentSpools        = 2 per process
MaximumEncryptedPackageLength  = 33,557,106 bytes
```

No `IOptionsMonitor`, hot reload, environment override, SQL parameter or GUC
may change these values. A future variable profile needs an immutable version,
digest and per-delivery snapshot under separate Homeowner authority.

### 3.3 Exact capability delta

```text
C2 S3 credentials:       4 -> 4 unchanged
C3 additive credentials: 0 -> 1 DeliveryReader
Combined package creds:  4 -> 5
C2 DB capabilities:      3 -> 3 unchanged
C3 DB capabilities:      0 -> 1 Delivery
C2 SQL capability roles: 3 -> 3 unchanged
C3 SQL capability roles: 0 -> 1 tagekyc_raw_export_package_delivery
C2 SQL LOGIN roles:      3 -> 3 unchanged
C3 SQL LOGIN roles:      0 -> 1 tagekyc_raw_export_package_delivery_login
```

The capability role is `NOLOGIN`, `INHERIT`, non-superuser, non-createdb,
non-createrole, non-replication and non-bypassrls. The LOGIN role is deployment
owned and must pre-exist; migration does not create it. Membership is exactly:

```text
tagekyc_raw_export_package_delivery_login
-> tagekyc_raw_export_package_delivery
admin_option=false, inherit_option=true, set_option=false
```

No PUBLIC, ordinary runtime, C2 role, DK role, capability role or unrelated
login receives delivery function EXECUTE.

## 4. Public API and exact error contract

Map exactly:

```text
POST /api/ekyc/raw-export/packages/{packageId:D}/deliveries
GET  /api/ekyc/raw-export/deliveries/{deliveryId:D}
GET  /api/ekyc/raw-export/deliveries/{deliveryId:D}/content
```

All use the landed API-key authentication envelope, caller category exactly
`BusinessConsumer`, scope exactly
`business.raw-export.package.download`, stable non-empty principal and direct
`ClientApplicationId` recipient ownership. `AllowedClientApplicationIds` never
grants C3 access.

No `HEAD`, collection, list, search, inbox, notification, presign, redirect,
webhook, Range or acknowledgement route is mapped. Route input never accepts a
recipient id. No DTO contains provider endpoint, bucket, key, binding digest,
provider receipt, credential, recipient public key, key
id/version/fingerprint, C1/C2 internal identity or raw idempotency/API key. The
only receipt field exposed is the public C3 `DeliveryReceiptDigest` in §5.3.

### 4.1 DTOs

POST has no JSON body. It requires one `Idempotency-Key` header of 1–128 bytes,
all bytes in ASCII `0x21..0x7e`; leading/trailing whitespace is data and is not
trimmed. Multiple header values, commas introduced by header coalescing,
non-ASCII, empty or oversized values are invalid. Raw value is not logged.

Create/status projection is exactly:

```text
DeliveryId: Guid
PackageId: Guid
State: string
AuthorizedAtUtc: DateTimeOffset
AuthorizationExpiresAtUtc: DateTimeOffset
StreamAttemptCount: int
ServerStreamCompletedAtUtc: DateTimeOffset?
EncryptedPackageLength: long
PackageCiphertextDigest: base64url(32 bytes)
DeliveryReceiptDigest: base64url(32 bytes)?
```

POST new returns 201; exact replay returns 200. Status returns 200 for every
owned durable state without key/provider revalidation.

Content success headers are exact:

```text
Content-Type: application/octet-stream
Content-Length: persisted EncryptedPackageLength
Content-Disposition: attachment; filename="tagekyc-package-{PackageId:D}.t88pkg"
Cache-Control: no-store
Pragma: no-cache
X-Content-Type-Options: nosniff
Content-Encoding: absent
Accept-Ranges: none
```

### 4.2 Public error codes/messages

Errors use the landed error-envelope shape. Exact C3 codes and safe messages:

| Code | HTTP | Message |
| --- | ---: | --- |
| `RAW_EXPORT_PACKAGE_DELIVERY_PRINCIPAL_REQUIRED` | 403 | `Authenticated principal is not eligible for package delivery.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_FORBIDDEN` | 403 | `Package delivery is not authorized.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_REQUEST_INVALID` | 400 | `Package delivery request is invalid.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_IDEMPOTENCY_CONFLICT` | 409 | `The idempotency key is already bound to another package.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_NOT_FOUND` | 404 | `Package delivery was not found.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_INELIGIBLE` | 409 | `The package is not eligible for delivery.` |
| `RAW_EXPORT_PACKAGE_RANGE_NOT_SUPPORTED` | 416 | `Partial package delivery is not supported.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_IN_PROGRESS` | 409 | `Package delivery is already in progress.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_CAPACITY_UNAVAILABLE` | 503 | `Package delivery capacity is temporarily unavailable.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` | 503 | `Package delivery is temporarily unavailable.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_INTEGRITY_UNAVAILABLE` | 503 | `Verified package content is unavailable.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_EXPIRED` | 410 | `Package delivery authorization has expired.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_OUTCOME_UNKNOWN` | 410 | `Package delivery outcome is unknown.` |
| `RAW_EXPORT_PACKAGE_DELIVERY_ALREADY_COMPLETED` | 410 | `Package delivery is already complete.` |

Missing package, wrong recipient, non-Finalized package, missing delivery and
cross-recipient delivery all use the same 404. Raw SQLSTATE/provider status and
internal state never cross the boundary.

### 4.3 Route-specific precedence

After authentication, category/scope/principal and request-shape validation,
all three routes evaluate the C3 topology gate before any C3 delivery DB or
provider access. `Disabled` or `Invalid` returns
`RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` / 503 with zero C3 delivery DB,
provider, permit or lifecycle mutation. API-key authentication may use the
landed API-key store; that is not C3 delivery DB access.

Enabled Create order:

1. authentication;
2. category/scope/principal;
3. route/idempotency shape;
4. C3 topology availability gate;
5. existing recipient+digest same package -> exact historical replay;
6. existing recipient+digest different package -> conflict;
7. exact recipient-owned Finalized package;
8. exact frozen-key eligibility;
9. readiness/catalog indeterminate;
10. create Authorized.

Enabled Status order: authentication -> category/scope/principal -> route shape
-> C3 topology gate -> recipient-scoped lookup -> exact persisted projection.
It performs no provider I/O, key recheck or reconciliation mutation.

Content order:

1. authentication;
2. category/scope/principal;
3. route shape;
4. C3 topology availability gate;
5. recipient-scoped delivery/package existence;
6. owned Range/If-Range rejection;
7. persisted terminal state;
8. live stream lease;
9. process permit;
10. locked key/package/delivery eligibility;
11. provider transient/unknown;
12. deterministic integrity failure;
13. response copy cancellation/failure;
14. ambiguous host loss;
15. exact completed copy.

The pre-permit probe is non-authoritative. Every admission fact is revalidated
after locks and at the fresh clock.

## 5. Canonical identity and evidence codecs

All text is UTF-8. Hex is lowercase. UUID scalar strings are lowercase `N`
format. Timestamps use
`yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`. Integer scalars are invariant decimal.

### 5.1 Idempotency digest and DeliveryId

```text
IdempotencyKeyDigest = SHA-256(exact validated Idempotency-Key UTF-8 bytes)

DeliveryId = EvidenceCanonicalization.DeterministicGuid(
  "tip-88c1-c3-delivery-id-v1",
  new {
    recipientClientApplicationId = RecipientClientApplicationId.ToString("N"),
    idempotencyKeyDigest = lowerhex(IdempotencyKeyDigest)
  })
```

JCS orders object properties lexicographically. Implemented source property
order cannot change the canonical bytes.

Absolute vector I:

```text
Idempotency-Key UTF-8 = C3-Key-01
IdempotencyKeyDigest  = 146769e1bf7a74ed89c039e8ed331e7ddeec0314b009ee73ae7f3d137605bb4c
RecipientClientApplicationId = 33333333-3333-3333-3333-333333333333
JCS = {"idempotencyKeyDigest":"146769e1bf7a74ed89c039e8ed331e7ddeec0314b009ee73ae7f3d137605bb4c","recipientClientApplicationId":"33333333333333333333333333333333"}
label-plus-JCS SHA-256 = 8d07a60a5783b6337c61373dc75c022336e640cdc3ccaa1d6c8693251ab5e12f
DeliveryId = 0aa6078d-8357-53b6-bc61-373dc75c0223
```

PackageId is a mandatory equality member persisted separately. It is not
folded into DeliveryId. Unique identity is
`(RecipientClientApplicationId, IdempotencyKeyDigest)`.

### 5.2 Delivery equality fingerprint

Use `C1HashCanonical.Compute` with domain
`tip-88c1-c3-delivery-equality-v1` and ordered scalar fields:

```text
DeliveryId, PackageId, RecipientClientApplicationId,
IdempotencyKeyDigest, PackageEqualityFingerprint,
C2PreparationId, RecipientKeyId, RecipientKeyVersion,
RecipientKeyFingerprint, RecipientKeyRevision,
EncryptedPackageLength, PackageCiphertextDigest, EnvelopeDigest
```

It is stored as 32 bytes and exact replay compares it plus PackageId. Request
echo never substitutes for the persisted value.

### 5.3 Delivery receipt

`DeliveryReceiptDigest` is
`C1HashCanonical.Compute("tip-88c1-c3-delivery-receipt-v1", scalars...)` in this
exact order:

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

The SQL completion function derives it from locked persisted delivery/package
state and its single completion clock. C# independently recomputes the returned
projection and fixed-time compares it. The actor fields are the exact content
attempt actor, not POST creator input.

Absolute vector R:

```text
DeliveryId = 11111111-1111-1111-1111-111111111111
PackageId  = 22222222-2222-2222-2222-222222222222
RecipientClientApplicationId = 33333333-3333-3333-3333-333333333333
DeliveryAttemptNumber = 2
DeliveryFence = 7
PackageCiphertextDigest = 000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f
EncryptedPackageLength = 123456
AuthorizedAtUtc = 2026-08-18T01:02:03.0040000Z
StreamStartedAtUtc = 2026-08-18T01:03:04.0050000Z
ServerStreamCompletedAtUtc = 2026-08-18T01:04:05.0060000Z
AuthenticatedApiKeyId = 44444444-4444-4444-4444-444444444444
AuthenticatedPrincipalId = 55555555-5555-5555-5555-555555555555
canonical preimage length = 399
DeliveryReceiptDigest = 9e1d054f0d722918e51c1512b1f17bcca24c709355c2f48ae01bd9f450f32881
```

The test must also assert the complete 399-byte preimage hex from bundle
`VECTOR_RECOMPUTE.ps1`, not only the final digest.

### 5.4 Correlation and lifecycle evidence

For each authenticated POST/content request, the application derives, before
calling C3 SQL, exactly:

```text
RequestCorrelationDigest = C1HashCanonical.Compute(
  "tip-88c1-c3-request-correlation-v1",
  NormalizeFormC(HttpContext.TraceIdentifier))
```

The normalized trace identifier must be non-empty. Empty input returns
`RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE` before C3 mutation. No caller header,
generated fallback, `ClientApplicationId` or reconciler-local value may replace
it. Only the 32-byte digest is persisted. A reconciler event reuses the causal
actor and correlation already persisted by the request whose state it
terminalizes; the reconciler never invents an authenticated actor or
correlation.

Lifecycle evidence uses these separate exact domains:

```text
tip-88c1-c3-delivery-authorization-v1
tip-88c1-c3-delivery-stream-admission-v1
tip-88c1-c3-delivery-interruption-v1
tip-88c1-c3-delivery-integrity-v1
tip-88c1-c3-delivery-outcome-unknown-v1
tip-88c1-c3-delivery-expiry-v1
```

All use `C1HashCanonical.Compute` and these ordered scalars:

| Evidence | Ordered fields |
| --- | --- |
| Authorization | DeliveryId, PackageId, RecipientClientApplicationId, DeliveryEqualityFingerprint, CreatorApiKeyId, CreatorPrincipalId, AuthorizationCorrelationDigest, AuthorizedAtUtc, AuthorizationExpiresAtUtc |
| Stream admission | DeliveryId, PackageId, RecipientClientApplicationId, DeliveryAttemptNumber, DeliveryFence, StreamApiKeyId, StreamPrincipalId, StreamCorrelationDigest, StreamStartedAtUtc, StreamLeaseExpiresAtUtc |
| Interruption | DeliveryId, PackageId, RecipientClientApplicationId, DeliveryAttemptNumber, DeliveryFence, InterruptionKind, observed-length presence/value, observed-package-digest presence/value, observed-envelope-digest presence/value, ObjectBindingDigest, InterruptedAtUtc |
| Integrity | DeliveryId, PackageId, RecipientClientApplicationId, DeliveryAttemptNumber, DeliveryFence, IntegrityFailureKind, observed-length presence/value, observed-package-digest presence/value, observed-envelope-digest presence/value, ObjectBindingDigest, IntegrityUnavailableAtUtc |
| Outcome unknown | DeliveryId, PackageId, RecipientClientApplicationId, DeliveryAttemptNumber, DeliveryFence, StreamApiKeyId, StreamPrincipalId, StreamCorrelationDigest, StreamStartedAtUtc, StreamLeaseExpiresAtUtc, OutcomeUnknownAtUtc, `LeaseExpiredAfterRestartOrLateCompletion` |
| Expiry | DeliveryId, PackageId, RecipientClientApplicationId, predecessor State (`Authorized` or `Interrupted`), causal ApiKeyId, causal PrincipalId, causal CorrelationDigest, AuthorizationExpiresAtUtc, ExpiredAtUtc |

Presence is a separate canonical scalar from value. No fallback digest,
conflated source, caller-supplied evidence or test-computed substitute is
allowed.

## 6. Exact persistence model

Migration timestamp/name is fixed:

```text
20260819120000_Tip88C1C3AuthenticatedPackageDelivery
```

### 6.1 `raw_export_recipient_package_deliveries`

Exact columns:

```text
DeliveryId uuid PK
PackageId uuid NOT NULL FK -> C2 PackageId
RecipientClientApplicationId uuid NOT NULL
IdempotencyKeyDigest bytea NOT NULL (32)
DeliveryEqualityFingerprint bytea NOT NULL (32)
C2PreparationId uuid NOT NULL
PackageEqualityFingerprint bytea NOT NULL (32)
RecipientKeyId varchar(128) NOT NULL
RecipientKeyVersion integer NOT NULL (>0)
RecipientKeyFingerprint bytea NOT NULL (32)
RecipientKeyRevision bigint NOT NULL (>0)
PackageRevisionAtAuthorization bigint NOT NULL (>0)
EncryptedPackageLength bigint NOT NULL (1..33,557,106)
PackageCiphertextDigest bytea NOT NULL (32)
EnvelopeDigest bytea NOT NULL (32)
ObjectBindingDigest bytea NOT NULL (32)
CreatorApiKeyId uuid NOT NULL
CreatorPrincipalId uuid NOT NULL
AuthorizationCorrelationDigest bytea NOT NULL (32)
State varchar(32) NOT NULL
Revision bigint NOT NULL (>0)
AuthorizedAtUtc timestamptz NOT NULL
AuthorizationExpiresAtUtc timestamptz NOT NULL
StreamAttemptCount integer NOT NULL DEFAULT 0
DeliveryFence bigint NOT NULL DEFAULT 0
StreamStartedAtUtc timestamptz NULL
StreamLeaseExpiresAtUtc timestamptz NULL
StreamApiKeyId uuid NULL
StreamPrincipalId uuid NULL
StreamCorrelationDigest bytea NULL
InterruptionKind varchar(64) NULL
InterruptionEvidenceDigest bytea NULL
InterruptedAtUtc timestamptz NULL
IntegrityFailureKind varchar(64) NULL
IntegrityEvidenceDigest bytea NULL
IntegrityUnavailableAtUtc timestamptz NULL
OutcomeUnknownAtUtc timestamptz NULL
ServerStreamCompletedAtUtc timestamptz NULL
VerifiedByteCount bigint NULL
VerifiedPackageCiphertextDigest bytea NULL
DeliveryReceiptDigest bytea NULL
ExpiredAtUtc timestamptz NULL
```

Exact keys/indexes:

```text
pk_raw_export_recipient_package_delivery (DeliveryId)
uq_raw_export_recipient_package_delivery_idempotency
  (RecipientClientApplicationId, IdempotencyKeyDigest)
uq_raw_export_recipient_package_delivery_equality
  (DeliveryEqualityFingerprint)
ix_raw_export_recipient_package_delivery_reconcile
  (State, StreamLeaseExpiresAtUtc, AuthorizationExpiresAtUtc, DeliveryId)
fk_raw_export_recipient_package_delivery_package (PackageId)
fk_raw_export_recipient_package_delivery_key
  (RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion)
```

All UUIDs are non-empty. Authorization expiry is exactly AuthorizedAt + 30m.
Allowed states are exactly the seven scope states. The sparse CHECK encodes:

- Authorized: no attempt/terminal fields, count/fence zero;
- Streaming: complete current attempt actor/start/lease, no terminal fields;
- Interrupted: current attempt plus kind/evidence/time, no receipt;
- IntegrityUnavailable: current attempt plus integrity kind/evidence/time, no
  receipt and never reopens;
- OutcomeUnknown: current attempt plus unknown time, no receipt;
- ServerStreamCompleted: current attempt, exact verified length/digest,
  completion time and receipt; immutable;
- Expired: expiry time; attempt fields are absent when never started and prior
  attempt history may remain after Interrupted expiry; no receipt.

Every optional digest is either NULL or exactly 32 bytes. UNKNOWN never
satisfies a state shape; required members use `IS NOT NULL` before comparisons.

### 6.2 `raw_export_recipient_package_delivery_events`

Exact columns:

```text
DeliveryEventId uuid PK
DeliveryId uuid NOT NULL FK -> delivery
RecipientClientApplicationId uuid NOT NULL
Revision bigint NOT NULL
EventType varchar(32) NOT NULL
DeliveryAttemptNumber integer NULL
DeliveryFence bigint NULL
AuthenticatedApiKeyId uuid NOT NULL
AuthenticatedPrincipalId uuid NOT NULL
CorrelationDigest bytea NOT NULL (32)
EvidenceDigest bytea NOT NULL (32)
OccurredAtUtc timestamptz NOT NULL
```

Unique `(DeliveryId, Revision)`. Event types exactly:

```text
Authorized | StreamingStarted | Interrupted | IntegrityUnavailable |
OutcomeUnknown | ServerStreamCompleted | Expired
```

Event identity is deterministic from DeliveryId and revision using domain
`tip-88c1-c3-delivery-event-id-v1`. The event CHECK pins state-specific
attempt/fence presence. Events are append-only: an owner-only trigger rejects
UPDATE/DELETE; runtime receives no table DML.

The exact non-null actor, correlation and evidence sources are:

| EventType | Actor and correlation source | EvidenceDigest source |
| --- | --- | --- |
| Authorized | creator API key/principal and AuthorizationCorrelationDigest | authorization codec §5.4 |
| StreamingStarted | exact admitted stream API key/principal and StreamCorrelationDigest | stream-admission codec §5.4 |
| Interrupted | persisted current stream actor/correlation | InterruptionEvidenceDigest |
| IntegrityUnavailable | persisted current stream actor/correlation | IntegrityEvidenceDigest |
| OutcomeUnknown | persisted current stream actor/correlation; never reconciler identity | outcome-unknown codec §5.4 |
| ServerStreamCompleted | persisted current stream actor/correlation | DeliveryReceiptDigest |
| Expired from Authorized | creator actor/correlation | expiry codec §5.4 |
| Expired from Interrupted | persisted last-stream actor/correlation | expiry codec §5.4 |

An event/source mismatch is a state-shape failure, not a logging preference.

The scope's “denial audit only” does not create a delivery row or lifecycle
event. It is a sanitized structured security-audit record through the landed
logging boundary, binding recipient, PackageId/DeliveryId when known, API key,
non-empty principal, correlation digest and stable denial code. It contains no
raw API/idempotency key, provider locator or key metadata. It is not replay or
lifecycle authority, and denial-log failure cannot turn a denied request into
success.

## 7. State transitions and outcome precedence

Active transitions:

| ID | From | Condition | To | Result |
| --- | --- | --- | --- | --- |
| T01 | absent | exact new eligible authorization | Authorized | Created |
| T02 | any persisted | exact recipient+idempotency+PackageId replay | unchanged | ExistingMatch |
| T03 | Authorized | fresh eligible begin + permit | Streaming | Started |
| T04 | Interrupted | fresh eligible retry + permit | Streaming | Started |
| T05 | Authorized | admission clock >= authorization expiry | Expired | Expired |
| T06 | Interrupted | admission/reconcile clock >= expiry | Expired | Expired |
| T07 | Streaming | known provider/copy interruption | Interrupted | Interrupted |
| T08 | Streaming | deterministic pre-response failure | IntegrityUnavailable | IntegrityUnavailable |
| T09 | Streaming | lease expired before durable completion, by reconciler or late completion | OutcomeUnknown | OutcomeUnknown |
| T10 | Streaming | exact verified server copy + CAS | ServerStreamCompleted | Completed |

Forbidden transitions include any terminal reopen, IntegrityUnavailable retry,
second live stream, completion without exact fence/actor/copy, and package/C2
mutation. A new idempotency key creates a distinct delivery and may target the
same package; it inherits no state, bytes or evidence and performs its own full
provider read and verification.

SQL outcome tokens are closed:

```text
Created | ExistingMatch | IdempotencyConflict | NotFound | Ineligible |
Unavailable | Authorized | Started | InProgress | Interrupted |
IntegrityUnavailable | OutcomeUnknown | Completed | Expired |
Terminal | StateConflict | None
```

Function-specific result shapes reject unknown tokens in C#.

The exact ownership of those tokens is:

| Function | Allowed outcomes |
| --- | --- |
| create | `Created`, `ExistingMatch`, `IdempotencyConflict`, `NotFound`, `Ineligible`, `Unavailable` |
| read | `ExistingMatch`, `NotFound` |
| probe content | `Authorized`, `InProgress`, `Expired`, `IntegrityUnavailable`, `OutcomeUnknown`, `Completed`, `NotFound` |
| begin stream | `Started`, `InProgress`, `Expired`, `Terminal`, `Ineligible`, `Unavailable`, `NotFound`, `StateConflict` |
| record interrupted | `Interrupted`, `ExistingMatch`, `StateConflict` |
| record integrity unavailable | `IntegrityUnavailable`, `ExistingMatch`, `StateConflict` |
| complete | `Completed`, `ExistingMatch`, `OutcomeUnknown`, `StateConflict` |
| reconcile next | `Expired`, `OutcomeUnknown`, `None` |

## 8. SQL surface, locking and restart

All functions are in schema `tagekyc`, owned by
`tagekyc_raw_export_deployer`, `SECURITY DEFINER`, and have exactly
`SET search_path TO pg_catalog`. Identifiers are schema-qualified. PUBLIC
EXECUTE is revoked.

### 8.1 Exact nine-function census

Eight callable functions, all EXECUTE only to
`tagekyc_raw_export_package_delivery`:

```text
raw_export_create_recipient_package_delivery
  (uuid,uuid,uuid,bytea,uuid,uuid,bytea)
raw_export_read_recipient_package_delivery
  (uuid,uuid)
raw_export_probe_recipient_package_delivery_content
  (uuid,uuid)
raw_export_begin_recipient_package_delivery_stream
  (uuid,uuid,uuid,uuid,bytea)
raw_export_record_recipient_package_delivery_interrupted
  (uuid,bigint,bigint,text,bytea)
raw_export_record_recipient_package_delivery_integrity_unavailable
  (uuid,bigint,bigint,text,bytea)
raw_export_complete_recipient_package_delivery
  (uuid,bigint,bigint,bigint,bytea)
raw_export_reconcile_next_recipient_package_delivery
  ()
```

One trigger function, owner-only and not callable by runtime:

```text
raw_export_guard_recipient_package_delivery_event() RETURNS trigger
```

There is no overload. Readiness proves exact identity arguments, owner,
prosecdef/proconfig, body hash, effective ACL and absence of same-name extras.

### 8.2 Create/replay locking

Create first probes `(recipient,idempotency digest)` without a blocking lock.
If found, it returns exact persisted replay/conflict without package/key
reauthorization or mutation.

For absence, probe exact recipient+PackageId to obtain immutable identifiers,
then lock in global order:

```text
exact recipient-key row FOR UPDATE
-> exact package row FOR UPDATE
-> no delivery row yet
-> one clock_timestamp()
-> revalidate key State=Active, exact frozen key fields and
   ValidFromUtc <= clock < ValidUntilUtc
-> revalidate package State=Finalized, recipient and complete metadata
-> INSERT ... ON CONFLICT DO NOTHING
-> durable re-read by recipient+idempotency
-> Created / ExistingMatch / IdempotencyConflict
```

No caller timestamp or transaction-start time is admission authority.

### 8.3 Stream admission

After a nonlocking owned-delivery probe and successful fail-fast process permit:

```text
lock exact frozen recipient key
-> lock exact package
-> lock exact delivery
-> one clock_timestamp()
-> no further blocking lock
-> revalidate bindings, C2 Finalized, key Active/exact/window,
   authorization expiry, state and live lease
-> CAS Authorized|Interrupted -> Streaming
-> StreamAttemptCount + 1, DeliveryFence + 1
-> StreamStartedAtUtc = clock
-> StreamLeaseExpiresAtUtc = clock + 35 minutes
-> persist exact stream ApiKeyId/PrincipalId/correlation digest
-> append event
-> commit
```

Provider read, spool verification and HTTP copy occur only after commit and
with no database/advisory lock held. The operation cancellation deadline is
`StreamStartedAtUtc + 30 minutes`; it never extends the 35-minute lease.

### 8.4 Completion and failure CAS

All terminal/interrupt functions lock the delivery and require exact
`State=Streaming`, revision and fence. The 30-minute operation boundary governs
provider read, verification and response-copy cancellation: success is eligible
only when the exact copy task completed before operation cancellation was
observed. It is not a SQL completion deadline. Completion then captures one
database clock and requires `clock < StreamLeaseExpiresAtUtc`; the remaining
five minutes are bounded persistence grace. It compares passed copy
length/digest to persisted package values, computes receipt from persisted
fields, writes only completion fields/state/revision and appends one event. If
the copy completed but the SQL clock is at or after the 35-minute lease, the same
call performs T09 and returns `OutcomeUnknown` without a receipt. Cancellation,
copy failure or an incomplete copy can never use the persistence grace to
become Completed.

Known provider/read/caller/response copy failure writes Interrupted. Positive
GET absence or deterministic length/digest/envelope/object-binding mismatch
before response start writes delivery-local IntegrityUnavailable. Process loss
does not run a catch block and cannot claim interruption/completion.

### 8.5 Restart reconciler

One hosted C3 reconciler calls only
`raw_export_reconcile_next_recipient_package_delivery()`. The function probes
one due candidate ordered by due time then DeliveryId, locks key -> package ->
delivery, captures one clock, revalidates, and performs at most one mutation:

- stale Streaming (`clock >= StreamLeaseExpiresAtUtc`) -> OutcomeUnknown;
- Authorized/Interrupted past authorization expiry -> Expired;
- otherwise None.

It uses `FOR UPDATE SKIP LOCKED` only at the final delivery lock after the
nonlocking candidate probe and ordered predecessor locks; no reverse delivery
-> package/key path exists. Retry delay is bounded process scheduling only and
is not durable authority. Restart correctness comes from database state, never
an in-process cache or spool.

## 9. Provider, spool and HTTP boundary

### 9.1 Exact provider member surface

The C3 provider exposes one operation:

```text
OpenExactAsync(RecipientPackageLocator, CancellationToken) -> bounded Stream
```

Implementation uses exactly `GetObjectAsync(GetObjectRequest, ct)` with the
locked C2 bucket/key. No second executable image/config path exists. Forbidden
members include `GetPreSignedURL`, `ListObjects*`, `GetObjectMetadata*`, Range,
Select, Copy, Put, Delete, DeleteObjects, multipart and bucket administration.
Do not match `GetObject` as a substring of `GetObjectAsync`; architecture proof
uses exact symbols.

GET 404 maps positive absence. 403, 429, 5xx, timeout and transport failure map
Unavailable/Interrupted before response; a truncated successful body is
deterministic integrity failure. No provider exception escapes publicly.

### 9.2 Fifth-credential IAM

The DeliveryReader policy permits only:

```text
s3:GetObject on arn:aws:s3:::<exact-bucket>/raw-export/c2-package/v1/*
```

It permits no bucket action and no other object prefix. Positive tests use the
exact credential; cross-bucket/prefix and all forbidden actions deny. C2
credentials cannot call the C3 content adapter, and ordinary runtime cannot
assume the C3 role.

Runtime readiness performs a fixed missing-object GET probe under the package
prefix: expected 404 proves endpoint/bucket/credential reachability without
listing or mutation; 403/5xx/ambiguous is RED. Over-privilege is proved by
effective policy inspection in deployment/test evidence, not by adding
forbidden SDK calls to production.

### 9.3 Bounded encrypted-memory spool

A singleton pool owns exactly two `SemaphoreSlim` permits. Acquisition is
`WaitAsync(TimeSpan.Zero)` and occurs before the Streaming transition. Capacity
failure releases/retains no authority and performs zero DB/provider mutation.

The C3 coordinator creates a fresh landed `RecipientPackageEncryptedSpool` per
attempt. It reads at most 33,557,106 bytes, rejects an extra byte and early EOF,
seals, compares exact length/digest, opens the sealed spool to recompute the C2
envelope digest, and fixed-time compares all digests before headers start. It
does not store a file, DB/blob/cache entry or plaintext. Every rented segment
and transient digest is zeroed in `finally`; the permit is released once on
every path.

After verification, response headers are set and the sealed spool is copied
once while an independent counter/hash verifies the server copy. Completion is
recorded only after `CopyToAsync` returns and the copy counter/digest match.

## 10. Readiness and composition

C3 readiness codes in first-error order:

```text
PROD_RAW_EXPORT_PACKAGE_DELIVERY_CONFIG_INVALID
PROD_RAW_EXPORT_PACKAGE_DELIVERY_ROLE_TOPOLOGY_INVALID
PROD_RAW_EXPORT_PACKAGE_DELIVERY_CATALOG_INVALID
PROD_RAW_EXPORT_PACKAGE_DELIVERY_PROVIDER_UNAVAILABLE
PROD_RAW_EXPORT_PACKAGE_DELIVERY_BUCKET_UNAVAILABLE
PROD_RAW_EXPORT_PACKAGE_DELIVERY_OBJECT_LOCK_PROHIBITED
PROD_RAW_EXPORT_PACKAGE_DELIVERY_VERSIONING_PROHIBITED
PROD_RAW_EXPORT_PACKAGE_DELIVERY_LIFECYCLE_PROHIBITED
PROD_RAW_EXPORT_PACKAGE_DELIVERY_PUBLIC_ACCESS_PROHIBITED
PROD_RAW_EXPORT_PACKAGE_DELIVERY_IAM_INVALID
```

Object Lock precedes Versioning because real S3/MinIO Object Lock requires
versioning. C3 invokes the landed C2 posture interpretation but owns its own
enabled/disabled result, role/catalog census and DeliveryReader reachability.

Catalog readiness proves exactly:

- two C3 tables, owned by deployer, no runtime/public DML;
- nine C3 functions, exact signatures/body hashes/security settings;
- one NOLOGIN capability and one LOGIN with exact attributes/membership;
- eight exact EXECUTE grants to capability and zero other effective grants;
- no C3 login membership in C2, DK, assembly, job or ordinary runtime roles;
- C2 exact 3+3 role and four-provider-credential readiness stays GREEN when C3
  is disabled/unconfigured.

Program resolves C3 options first. Disabled composition uses no C3 DB/provider
factory. Enabled composition registers the one delivery connection factory,
reader, pool, coordinator, reconciler and readiness validator. Invalid config
cannot be masked by a missing DI service or a hot-reload fallback.

## 11. Exact permanent-path allowlist — 38 paths

Only these paths may change during a later controlled build:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c3_authenticated_package_delivery_build_dispatch.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c3_authenticated_package_delivery_review_ledger.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c3_authenticated_package_delivery_as_built.md
src/TagEkyc.Contracts/RawExport/RecipientPackageDeliveryContracts.cs
src/TagEkyc.Application/Ports/RecipientPackageDeliveryPorts.cs
src/TagEkyc.Application/RawExport/RecipientPackageDeliveryApplicationService.cs
src/TagEkyc.Api/RecipientPackageDeliveryEndpoints.cs
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/ReadinessEndpoint.cs
src/TagEkyc.Application/LocalDev/LocalDevApiKeyStore.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackageDeliveryRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackageDeliveryEventRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackageDeliveryConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackageDeliveryEventConfig.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260819120000_Tip88C1C3AuthenticatedPackageDelivery.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryContracts.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryOptions.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryCodec.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryConnectionFactory.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryRepository.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryObjectClientFactory.cs
src/TagEkyc.Infrastructure/RawExport/S3CompatibleRecipientPackageDeliveryReader.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliverySpoolPool.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryCoordinator.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryReconciler.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryReadinessValidator.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageDeliveryServiceCollectionExtensions.cs
tests/TagEkyc.UnitTests/Tip88C1C3RecipientPackageDeliveryCodecTests.cs
tests/TagEkyc.UnitTests/Tip88C1C3RecipientPackageDeliveryApplicationTests.cs
tests/TagEkyc.ArchTests/Tip88C1C3RecipientPackageDeliveryArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs
tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
tests/TagEkyc.IntegrationTests/Tip83E1ReadinessEndpointTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
```

`RecipientPackageCodec.cs`, `RecipientPackageOptions.cs`,
`RecipientPackageEncryptedSpool.cs`, C2 migration/tests and auth model/store are
reviewed unchanged dependencies. If any needs editing, STOP/RRI for path 39;
do not exchange an allowlisted path silently.

The migration/Designer/snapshot and E3 paths are pre-authorized because the
model is additive. The disposable DB fixture is pre-authorized for the inherited
shared-migration-state debt. The readiness endpoint/test and local fixture
store are explicit composition impacts, not late allowlist discoveries.

## 12. Proof manifest — 26 methods, 34 mutations

Exactly one active test method owns each proof ID `C301`–`C326`. A Theory is
one method regardless of data rows. No new proof ID may be introduced without
dispatch correction. Scratch mutations never add permanent test methods.

| ID | Proof obligation |
| --- | --- |
| C301 | concrete persisted non-empty PrincipalId reaches all routes; empty/fallback rejected |
| C302 | exact three-route/auth/category/scope surface; Disabled/Invalid topology gate before C3 DB/provider/permit/mutation |
| C303 | recipient-scoped package/delivery non-disclosure and no delegated-id authority |
| C304 | exact idempotency validation/digest/DeliveryId/equality absolute vectors |
| C305 | Created/ExistingMatch/conflict precedence and zero-mutation historical replay |
| C306 | exact Finalized C2 package and immutable snapshot/FK binding; no direct-seeded canonical substitute |
| C307 | exact frozen key state/fingerprint/revision/window under lock and fresh clock |
| C308 | key->package->delivery lock order, post-lock clock, no deadlock/stale admission |
| C309 | seven-state sparse matrix, ten transitions, append-only event shapes and exact actor/correlation/evidence source matrix |
| C310 | two-permit fail-fast capacity before Streaming with zero durable/provider mutation |
| C311 | concurrent begin/retry yields one attempt/fence; stale CAS cannot write |
| C312 | known interruption permits byte-zero retry with incremented attempt/fence |
| C313 | authorization expiry and stale Streaming restart -> Expired/OutcomeUnknown |
| C314 | exact GET-only provider member, exact key, no second read/cache/HEAD/Range |
| C315 | fifth-credential IAM, identity separation, cross-prefix/bucket/action denial |
| C316 | bounded sealed pooled encrypted spool, extra/early EOF and complete zeroization |
| C317 | real C2 Prepare-to-Finalize package through real C3; length/ciphertext/envelope/object-binding verification before first response byte |
| C318 | delivery-local IntegrityUnavailable and new DeliveryId fresh read/reverify |
| C319 | exact success headers; Range/If-Range/HEAD/compression/presign/list absent |
| C320 | completed copy, 30-minute cancellation and 35-minute persistence-grace/late-completion discrimination |
| C321 | receipt absolute vector, SQL/C# equivalence and exact content-attempt actor |
| C322 | status/all-state and create replay stay historical after revoke/expiry/terminal state |
| C323 | production has no unwrap/private key/CEK recovery/plaintext/cache/log/DTO leak |
| C324 | C3 config/readiness, role/ACL/effective-membership and C2 independence |
| C325 | apply/Down/reapply, pending model, E3 tripwire, normalized SQL and disposable DB |
| C326 | affected/release validation census, hygiene, residue and exact allowlist/hash freeze |

### 12.1 Required scratch mutations

```text
C3M01 principal fallback/Guid.Empty admission
C3M02 category/scope widened or Disabled topology gate moved after C3 access
C3M03 recipient predicate removed
C3M04 idempotency raw value trimmed/persisted or digest changed
C3M05 PackageId equality removed from replay
C3M06 Prepared or Quarantined admitted as Finalized
C3M07 key State comparator removed
C3M08 key fingerprint/revision comparator removed
C3M09 key validity endpoint widened
C3M10 transaction_timestamp/pre-lock clock substituted
C3M11 delivery-before-package or package-before-key lock inversion
C3M12 NULL state-shape or event actor/correlation/evidence-source comparator weakened
C3M13 append-only event guard removed
C3M14 Streaming transition moved before permit
C3M15 permit count changed or wait made blocking
C3M16 revision or fence comparator removed
C3M17 stale Streaming or post-lease late completion classified Interrupted/Completed
C3M18 GetObject key replaced by caller/config prefix
C3M19 exact GetObjectAsync replaced by HEAD/Range/list/presign
C3M20 DeliveryReader replaced by C2 reconciler/lifecycle credential
C3M21 DeliveryReader IAM admits cross-prefix/bucket GetObject
C3M22 DeliveryReader IAM admits List/Put/Delete/Copy/Multipart
C3M23 maximum length check removed or extra byte accepted
C3M24 early EOF accepted
C3M25 real C2-produced non-envelope ciphertext byte tampered with only ciphertext comparator removed
C3M26 real C2-produced exact object plus scratch-only persisted EnvelopeDigest mismatch with only envelope comparator removed
C3M27 response start moved before seal/verification
C3M28 IntegrityUnavailable mapped to retry/global package denial
C3M29 new DeliveryId reuses prior delivery spool/evidence
C3M30 completion recorded before copy, after copy cancellation, without copy hash, or rejected solely because SQL clock passed the 30-minute operation boundary while lease remains live
C3M31 receipt/event binds POST creator or reconciler identity instead of exact causal stream actor/correlation
C3M32 receipt field/order/domain changed
C3M33 restart uses process cache instead of DB+provider
C3M34 role/function/table/ACL/body hash or ModelSnapshot tripwire widened
```

Each mutation must RED at its named discriminator, not a compilation error,
missing substring or unrelated failure. Restore every mutated file to its
recorded SHA, rerun the canonical owner proof and require PASS. Mutation report
records before/mutated/restored hashes, command, FQN, failure message and why
the failure discriminates the claim.

## 13. Validation policy

During implementation run only changed/affected tests:

1. compile the touched project;
2. C301–C326 owners affected by the edit;
3. exact predecessor proofs affected by a shared surface;
4. the current scratch mutation and restore control.

Do not repeatedly run the full repository suite. Before the final suite require:

- 26/26 canonical C3 proofs PASS;
- 34/34 mutations RED for assigned reasons and restore controls PASS;
- C2 package, B4 readiness, DK readiness and API-auth affected proofs PASS;
- release build 0 warnings / 0 errors;
- pending-model clean;
- migration apply/Down/reapply and normalized catalog/model evidence clean;
- staged paths zero, `git diff --check` PASS, task residue zero.

Then run exactly one complete, unfiltered Release suite on frozen final bytes.
Only the already-ratified manual golden-vector generator skip is accepted. Any
other failure is STOP/RRI; do not edit/retry/rerun without authority.

Closeout records exact test census rather than predicting a number here. It
also produces `SOURCE_MANIFEST.tsv`, mutation matrix, raw logs, vector
recompute script/output, catalog/ACL/IAM evidence, environment identity,
repository hashes and an external review-only ZIP with `SHA256SUMS` and
sidecar. No bundle in a temporary-cleaner directory is acceptable; use the
durable sibling closeout-bundles directory outside the repository.

## 14. Controlled execution order

```text
Task 0  gates in §2
Task 1  contracts/options/codecs + absolute vectors
Task 2  additive model/config/migration + normalized E3/pending-model proof
Task 3  SQL functions, roles, ACL, state and concurrency tests
Task 4  provider reader, IAM, spool pool and readiness
Task 5  application/API/reconciler composition and route tests
Task 6  C301-C326 positive controls
Task 7  C3M01-C3M34 mutation/restore sweep
Task 8  affected predecessor proofs + build/model/catalog hygiene
Task 9  one frozen-byte unfiltered Release suite
Task 10 as-built, ledger, manifests and review-only bundle
STOP     independent closeout review pending
```

No implementation begins before a later Homeowner ratification of this exact
dispatch SHA.

## 15. STOP/RRI conditions

STOP immediately if:

- stable non-empty PrincipalId requires runtime derivation, `Guid.Empty`, new
  semantics or an unauthorized auth path;
- a thirty-ninth permanent path or a C2/auth-model edit is required;
- the schema needs package-global integrity authority, recipient discovery,
  notification, acknowledgement, private-key/CEK recovery or plaintext;
- C2 becomes invalid when C3 is disabled/unconfigured;
- any route can enumerate, accept caller recipient identity, presign, range,
  HEAD or disclose a provider locator/key;
- provider I/O, a blocking C3 lock, or durable attempt/fence/state/event mutation
  occurs before capacity admission; landed API-key authentication and the exact
  recipient-scoped nonlocking pre-probe are allowed; no provider or HTTP copy
  may occur while a DB lock is held;
- lock order, post-lock clock, sparse state, CAS, receipt actor or restart
  semantics cannot be implemented exactly;
- provider verification needs a second object read, disk/blob cache or more
  than 33,557,106 bytes;
- a required mutation remains GREEN or REDs for an unrelated reason;
- migration Down touches predecessor schema, shared DB must be reset, snapshot
  manual editing is needed, or normalized SQL differs visibly;
- readiness cannot prove the exact role/function/ACL topology or enabled reader
  identity;
- final suite has a non-canonical failure or repository bytes drift during it.

## 16. Non-claims and forbidden work

This dispatch does not authorize or claim:

- PackageId discovery, inbox, notification or real recipient/hospital pilot;
- client receipt, decryption, storage, viewing, acceptance or non-repudiation;
- recipient enrollment/revocation/management;
- package CEK recovery, server plaintext or C2 repair;
- real Raw BIO, production activation, R4/R5 assembly/delivery, deployment;
- multipart, versioning, Object Lock, presigned URL, Range or list;
- stage, commit, push, merge, PR or provider operation during documentation.

`IntegrityUnavailable` is terminal only for its exact DeliveryId. Repeating a
fresh full verification under a new idempotency key may fail identically; this
bounded repeated work is accepted rather than inventing package authority.

## 17. Round-3 exact-byte closure review request

Reviewers must verify the exact bytes and independently answer:

1. Does Task-0 use persisted PrincipalId without runtime derivation?
2. Are all states, SQL outcomes, NULL shapes and transitions reachable and
   mutually satisfiable?
3. Can the lock order and restart scan avoid delivery/package/key inversion?
4. Is exact replay historical while content remains current-key gated?
5. Does new DeliveryId after IntegrityUnavailable force a fresh provider read?
6. Can any response byte precede full bounded verification?
7. Is receipt derived from exact completed stream actor and durable output?
8. Does Disabled C3 leave landed C2 topology/readiness valid?
9. Do IAM/ACL and exact function/role censuses prevent capability aliasing?
10. Are 38 paths sufficient, with harness debts anticipated rather than waived?
11. Does canonical C317 consume a package produced by landed C2 rather than a
    self-consistent test seed, and do tampered real-C2 bytes fail before output?
12. Are all seven event actor/correlation/evidence sources executable, including
    reconciler terminalization without a fabricated actor?
13. Is the 30-minute operation boundary separated from the 35-minute lease so
    completed copy has a bounded persistence path and late completion is T09?
14. Does the public Range code remain exactly the ratified Scope code, and do
    Disabled/Invalid topology checks run before C3 storage/provider access?

Required verdict vocabulary:

```text
PASS — READY FOR HOMEOWNER C3 CONTROLLED-BUILD RATIFICATION
FAIL — NOT READY FOR HOMEOWNER C3 CONTROLLED-BUILD RATIFICATION
STOP/RRI — HOMEOWNER ADJUDICATION REQUIRED
```
