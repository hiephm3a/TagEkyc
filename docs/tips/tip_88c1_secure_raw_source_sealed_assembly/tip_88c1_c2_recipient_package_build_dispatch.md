# TIP-88C1-C2 — Recipient Package Controlled Build Dispatch

Status: `ROUND_3_RECONCILED — BUILDER_ENTRY_RATIFICATION_REQUIRED`
Version: `0.2.1-R3C1`
Date: `2026-08-17`
Repository: `TagEkyc`
Branch: `tip-88a-raw-export-policy-catalog-build`
Baseline: `d9735f84b3946887d8301dda9686a7303aa71683`
Predecessor scope v0.2 SHA-256:
`B48AECEEEB2CEA2B71A6BA2951BEC7339195DA10AB72713AD0D834051FC56851`
Change authority: `DOCS_ONLY_ROUND_3_NARROW_RECONCILIATION; NO_ROUND_4_PLANNING`

## 1. Purpose and authority boundary

This dispatch turns the ratified C2 scope into one executable proof-build
contract. It owns the recipient-encrypted package between the trusted C1
`PrepareAsync` handoff and exact C1 `FinalizeAsync`/`AbortAsync` convergence.

It does not authorize implementation, migration execution, provider operation,
stage, commit, push, deployment, real Raw BIO or delivery. Homeowner build
ratification is required after independent review of the exact document SHA.

The ratified decisions D1–D7 are fixed:

- C1→C2 is a trusted internal handoff; C2 does not independently verify C1;
- the recipient-key snapshot freezes at the successful reservation
  linearization point;
- the package is immutable and self-contained;
- TagEkyc retains no usable package-CEK recovery capability after Prepare;
- recipient/delegated administration owns recipient-key lifecycle;
- no recipient-independent package signature is enabled in this slice; and
- `RawExportAssemblyTopology` is unchanged; the provider is injected.

## 2. Landed compatibility contract

### 2.1 Exact C1 delta

The baseline still has the seven-field request. The only permitted C1 code
change during the controlled build is the following one-field successor:

```csharp
public sealed record C2AssemblyPreparationRequest(
    Guid C2PreparationId,
    Guid AssemblyId,
    byte[] AssemblyFingerprint,
    byte[] ManifestDigest,
    byte[] AssemblyDigest,
    byte[] AssemblyAuthenticationValue,
    long CompleteAssemblyLength,
    Guid RecipientClientApplicationId);
```

`RawExportAssemblyOrchestrator` passes the already authorized
`job.RecipientClientApplicationId`. It does not add a DB query or caller input.

No other parameter, return column, C1 function, C1 role, grant, readiness
census, assembly byte, manifest codec, authentication codec or topology value
changes. The existing one unit, four architecture and twenty-six integration
C1 test methods remain active and count-stable.

### 2.2 Temporal handoff

```text
C1 Preparing + exact trusted request + bounded writer
→ C2 freezes recipient-key snapshot and package identity
→ C2 writes one non-deliverable provisional encrypted object
→ C2 returns exact ProviderReceiptDigest
→ C1 records Pending and commits SealCommitted
→ C2 Finalize binds the same provisional package to the exact seal
→ C2 Finalized, custody-complete, still non-deliverable
```

`Finalize` does not rewrite package bytes. `Abort` never deletes an object that
fails exact package identity/evidence inspection.

## 3. Fixed controlled-build profile

The current profile is fixed, not deployment-variable:

| Field | Exact value |
|---|---|
| Package format | `TIP-88C1-C2-PACKAGE-V1` |
| Package profile | `tip-88c1-c2-package-profile-v1` |
| Maximum complete assembly length | `33,554,432` bytes |
| Plaintext frame size | `1,048,576` bytes |
| Maximum data-frame count | `32` |
| Maximum JCS header length | `1,848` bytes |
| Maximum encrypted package length | `33,557,106` bytes |
| Content encryption | `A256GCM-FRAME-V1` |
| CEK | 32 CSPRNG bytes per package write attempt |
| Nonce prefix | 8 CSPRNG bytes per package write attempt |
| Provider operation token | 32 CSPRNG bytes generated once for a missing package row |
| Provider operation token digest | `SHA256(ProviderOperationToken)`; 32 bytes |
| Frame nonce | prefix + `UInt32BE(frameOrdinal)` |
| Data tag | 16 bytes |
| Completion nonce suffix | `0xffffffff` |
| Recipient key format | DER SubjectPublicKeyInfo |
| Recipient key algorithm | RSA, 3072–4096 bits |
| CEK wrap algorithm | `RSA-OAEP-256` |
| Package signature algorithm | `none` |
| Single-part operation timeout | 5 minutes |
| Positive-absence observations | 2 successful exact-key observations, cleanup only |
| Minimum absence-observation separation | 1 second |
| Maximum absence-observation window | 30 seconds |
| Object versioning | prohibited |
| Object Lock | prohibited |
| Multipart/copy/list/presign/batch delete | prohibited |

The maximum encrypted length is exactly:

```text
33,554,432 plaintext
+ 22 magic
+ 4 header-length field
+ 1,848 maximum header
+ 32 * (4 ordinal + 4 ciphertext-length + 16 tag)
+ 32 completion record
= 33,557,106 bytes
```

The header bound assumes RSA-4096 (`512` wrapped bytes, `683` base64url
characters), `RecipientKeyVersion <= 2147483647`, and an ASCII-safe
`RecipientKeyId` matching `[A-Za-z0-9._:-]{1,128}`. Every encoder rejects a
header or package over its bound before provider I/O.

Configuration exposes an immutable-by-id provider map. Each entry describes
one normalized endpoint, bucket and four distinct credentials. Existing
ids cannot be rebound to different endpoint/bucket semantics; historical ids
needed by nonterminal rows remain resolvable. Missing/drifted entries fail
closed. Configuration cannot change the cryptographic/framing values above. A
configurable crypto profile, alternate public-key algorithm, package signature
or larger package is a future gate, not an implicit option.

The current profile has no independently mutable provider prefix. `ObjectKey`
is the final normalized S3 key, including the fixed namespace
`raw-export/c2-package/v1/`; an endpoint/configuration/provider adapter MUST NOT
prepend, strip or rewrite another prefix. Bucket policy and the three object
credentials are scoped to that exact fixed namespace. A deployment-variable
prefix requires a future versioned locator contract and is not authorized here.

## 4. Recipient-key registry and snapshot

### 4.1 Registry ownership

`raw_export_recipient_key_registrations` is deployer-owned and table-owner-only.
There is no application enrollment API. Deployment authority may insert public
key descriptors and may transition exactly `Active → Revoked` with an exact
revision check. Private keys, private-key handles, KEKs and decryption
credentials are forbidden.

Key identity is:

```text
(RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion)
```

One partial unique index permits at most one `Active` row per recipient. Public
key fingerprint is exactly `SHA256(PublicKeySpki)`. SPKI length is 384–1024
bytes; the parsed RSA modulus is 3072–4096 bits and exponent is valid.

### 4.2 Candidate selection and snapshot linearization

For new work, `raw_export_select_active_recipient_key` returns one candidate
snapshot without claiming that it is frozen. The coordinator computes the
canonical equality, package identity and locator binding from that candidate.
The reserve function then:

1. locks that exact `(RecipientClientApplicationId, RecipientKeyId,
   RecipientKeyVersion)` row `FOR UPDATE`;
2. captures one `clock_timestamp()` after the lock;
3. checks `Active`, recipient equality, exact key revision/fingerprint, profile
   compatibility and `ValidFromUtc <= now`;
4. checks `ValidUntilUtc >= now + 5 minutes`;
5. re-derives and exact-compares the package equality fingerprint, `PackageId`,
   deterministic object key and `ObjectBindingDigest` from the locked row and
   trusted arguments;
6. inserts the package row with a complete immutable copy of the public
   snapshot, restricted historical locator and one append-only event; and
7. commits before any encryption/provider I/O.

That insert is the snapshot linearization point. Deployment revocation locks
the same key row. If revoke commits first, reserve fails. If reserve commits
first, the package continues with its copied snapshot even if the registry row
is later revoked. Replay reads the package snapshot and never selects latest.
Later revocation blocks C3 delivery but does not re-key, mutate or reverse C2.
If selection and reserve race with rotation/revocation, reserve returns a
retryable key outcome and writes nothing. A non-locking package probe may find
an already-frozen snapshot; the caller then locks that frozen key before the
package row and revalidates exact replay. A race that inserts the package after
the probe returns a retry outcome, never a false conflict or latest-key replay.

## 5. Identities and canonical evidence

Length-prefixed canonical preimages use the landed `C1HashCanonical`
UTF-8/NFC scalar encoding, lowercase hex for byte fields, lowercase `N` GUIDs
and invariant decimal integers. The RFC8785-JCS envelope is a separate surface:
its GUID string values use lowercase RFC 4122 `D` form with hyphens. No encoder
may apply one surface's GUID representation to the other.

### 5.1 Package equality

```text
PackageEqualityFingerprint = SHA256 canonical domain:
tip-88c1-c2-package-equality-v1

C2PreparationId
AssemblyId
AssemblyFingerprint
ManifestDigest
AssemblyDigest
AssemblyAuthenticationValue
RecipientClientApplicationId
RecipientKeyId
RecipientKeyVersion
RecipientKeyFingerprint
PackageProfile
CompleteAssemblyLength
```

```text
PackageId = DeterministicGuid(
  "tip-88c1-c2-package-id-v1",
  { c2PreparationId, packageEqualityFingerprint })
```

The deterministic object key is:

```text
raw-export/c2-package/v1/{PackageId:N}
```

This is the complete final S3 key. It is persisted and passed byte-for-byte to
Put/Head/Get/Delete. No configured prefix is concatenated before or after it.

The raw locator never appears in public/general-runtime DTOs, logs or audit.
Restricted durable metadata stores:

```text
ProviderKind
ProviderConfigurationId
ProviderEndpointFingerprint
BucketName
ObjectKey
```

`ProviderEndpointFingerprint` is exactly:

```text
C1HashCanonical(
  "tip-88c1-c2-provider-endpoint-v1",
  ProviderKind,
  NormalizedAbsoluteServiceUri,
  ForcePathStyle,              // lowercase true|false
  RegionIdentifier)
```

The normalized URI has lowercase scheme/host, an explicit effective port, one
trailing slash and no dot segments; userinfo, query and fragment are forbidden.
Credentials are excluded. On
restart, configuration is resolved by `ProviderConfigurationId` and must match
the persisted endpoint fingerprint and bucket before any operation. Missing or
drifted configuration fails closed; recovery never redirects to a different
provider instance. The locator fields plus their digest are owner-only:

```text
ObjectBindingDigest = C1HashCanonical(
  "tip-88c1-c2-object-binding-v1",
  ProviderKind,
  ProviderConfigurationId,
  ProviderEndpointFingerprint,
  BucketName,
  ObjectKey)
```

### 5.2 Provider operation token

For a missing package row, the coordinator fills exactly 32 bytes with the
platform CSPRNG immediately before reserve and computes:

```text
ProviderOperationTokenDigest = SHA256(ProviderOperationToken)
```

The raw token is a transient protected value. It is never persisted, placed in
the envelope/object metadata, logged, returned to C1 or accepted from a caller.
It is zeroized immediately after its digest has been passed to reserve. The
successful package-row insert is the digest linearization point and the
database enforces `UNIQUE (ProviderOperationTokenDigest)`; a collision fails
closed and never authorizes provider I/O.

Replay first probes the durable package row. An existing row always reuses the
persisted digest and never generates or reconstructs a token. If two missing-row
callers race, each may have a transient candidate token, but only the insert
winner freezes its digest. The loser zeroizes its token, re-reads the winner and
continues only on exact package-equality replay. The token digest is deliberately
not a member of `PackageEqualityFingerprint`: it identifies the one frozen
provider operation, not the recipient package's semantic equality.

The independent token-derivation vector uses raw bytes `20..3f` and yields:

```text
ProviderOperationTokenDigest
72dbb7336c76780023f83da4c355f2eeea85733b13d3477697917790c1229084
```

The full package codec vector in §6.3 continues to inject the already-derived
digest `cc` repeated 32 times so the downstream codecs remain isolated and
byte-stable. C207 must additionally prove the production generation→digest→
reserve→header/evidence wiring and turn RED when either the transient token or
the persisted digest is substituted.

### 5.3 Normalized conditional-create evidence

Created response and exact recovery inspection produce the same evidence:

```text
ConditionalCreateEvidenceDigest = C1HashCanonical(
  "tip-88c1-c2-conditional-create-evidence-v1",
  ObjectBindingDigest,
  EncryptedPackageLength,
  PackageCiphertextDigest,
  ProviderEntityTagDigest,
  "Unversioned",
  "PresentExact")
```

No response-path label (`Created` versus `Recovered`) enters the digest.

### 5.4 Provider receipt

```text
ProviderReceiptDigest = C1HashCanonical(
  "tip-88c1-c2-provider-receipt-v1",
  C2PreparationId,
  PackageId,
  PackageEqualityFingerprint,
  AssemblyId,
  AssemblyFingerprint,
  ManifestDigest,
  AssemblyDigest,
  AssemblyAuthenticationValue,
  RecipientClientApplicationId,
  RecipientKeyId,
  RecipientKeyVersion,
  RecipientKeyFingerprint,
  PackageProfile,
  ObjectBindingDigest,
  EncryptedPackageLength,
  PackageCiphertextDigest,
  EnvelopeDigest,
  ProviderOperationTokenDigest,
  ConditionalCreateEvidenceDigest)
```

It is the one durable cross-slice commitment returned to C1. Same preparation
and same exact object reproduce it. Any changed member is conflict.

### 5.5 Operational observation and cleanup evidence

All operational evidence uses the same length-prefixed rules from §5 and binds
the persisted provider instance/object identity. Optional observations are
encoded as two consecutive scalar fields: presence marker `0|1`, then empty
string when absent or the canonical value when present. Raw exceptions and
credentials never enter a preimage.

```text
ProviderObservationEvidenceDigest = C1HashCanonical(
  "tip-88c1-c2-provider-observation-v1",
  PackageId,
  ObjectBindingDigest,
  ProviderOperationTokenDigest,
  ObservationKind,
  ObservedEncryptedPackageLengthPresent, ObservedEncryptedPackageLength,
  ObservedPackageCiphertextDigestPresent, ObservedPackageCiphertextDigest,
  ObservedEnvelopeDigestPresent, ObservedEnvelopeDigest)

PositiveAbsenceEvidenceDigest = C1HashCanonical(
  "tip-88c1-c2-positive-absence-v1",
  PackageId,
  ObjectBindingDigest,
  ProviderOperationTokenDigest,
  FirstObservedAtUtc,
  SecondObservedAtUtc,
  "PositivelyAbsent")

CleanupProgressEvidenceDigest = C1HashCanonical(
  "tip-88c1-c2-cleanup-progress-v1",
  PackageId,
  ObjectBindingDigest,
  ProviderOperationTokenDigest,
  CleanupResultKind,
  ProviderObservationEvidenceDigest)

QuarantineEvidenceDigest = C1HashCanonical(
  "tip-88c1-c2-quarantine-v1",
  PackageId,
  PackageEqualityFingerprint,
  ObjectBindingDigest,
  ProviderOperationTokenDigest,
  QuarantineReason,
  ProviderObservationEvidenceDigest)
```

`ObservationKind` is closed to `PutOutcomeUnknown | PresentExact |
PositivelyAbsent | PresentMismatch | ProviderUnavailable`.
`CleanupResultKind` is closed to `PositiveAbsenceConfirmed |
DeleteOutcomeUnknown | ProviderUnavailable`; only the first may produce
`Aborted`, while the latter two produce/remain `CleanupPending`.
`QuarantineReason` is closed to `ExistingObjectMismatch | MetadataConflict |
ProviderIdentityConflict`. UTC timestamps use fixed RFC3339 microsecond `Z`
form. Each codec has an all-member sensitivity test; source/provider labels not
listed above cannot silently enter the digest.

## 6. Self-contained package format

### 6.1 Envelope

The byte stream is:

```text
ASCII("TIP-88C1-C2-PACKAGE-V1")
UInt32BE(HeaderLength)
UTF8(RFC8785-JCS(Header))

for each plaintext frame:
    UInt32BE(FrameOrdinal)
    UInt32BE(CiphertextLength)
    Ciphertext
    Tag[16]

completion:
    UInt32BE(0xffffffff)
    UInt64BE(CompleteAssemblyLength)
    UInt32BE(DataFrameCount)
    CompletionTag[16]
```

Header fields are exactly:

```text
assemblyDigest
assemblyFingerprint
assemblyId
c2PreparationId
completeAssemblyLength
contentEncryptionAlgorithm
extensionAlgorithms          // empty array in v1
framePlaintextBytes
keyWrapAlgorithm
noncePrefix                  // base64url, no padding
packageEqualityFingerprint
packageId
packageProfile
providerOperationTokenDigest
recipientClientApplicationId
recipientKeyFingerprint
recipientKeyId
recipientKeyVersion
signatureAlgorithm           // exact "none"
wrappedCek                   // base64url, no padding
```

The four GUID-valued header members (`assemblyId`, `c2PreparationId`,
`packageId`, `recipientClientApplicationId`) are lowercase `D` strings. The
same values use lowercase `N` strings when they enter any length-prefixed
canonical preimage.

`EnvelopeDigest = SHA256(Magic || UInt32BE(HeaderLength) || HeaderBytes)`.

Data-frame AAD is the length-prefixed payload:

```text
domain: tip-88c1-c2-frame-aad-v1
EnvelopeDigest, FrameOrdinal, PlaintextLength, "data"
```

Completion AAD is:

```text
domain: tip-88c1-c2-completion-aad-v1
EnvelopeDigest, DataFrameCount, CompleteAssemblyLength
```

The completion tag authenticates stream termination and total length. A
missing, duplicate, reordered, oversized or post-completion frame is invalid.

### 6.2 Encryption capability boundary

Production code imports only the frozen public SPKI, validates RSA parameters,
generates CEK/nonce bytes with `RandomNumberGenerator.Fill`, wraps the CEK with
RSA OAEP SHA-256, streams framed AES-256-GCM and zeroizes CEK/plaintext scratch.

No production interface or method accepts recipient private material or
returns/unpacks the CEK. The source-side attempt-KEK classes and
`UnwrapDekAsync` are forbidden dependencies. Restart recovery parses and hashes
the envelope/object without decrypting it.

Tests may hold a test-only recipient private key solely to prove that an
external recipient can unwrap/decrypt/verify the package. That key cannot enter
production DI, repository rows, logs or evidence.

### 6.3 Absolute vector

The canonical vector uses five-byte plaintext `hello`, CEK `00..1f`, nonce
prefix `0102030405060708`, fixed 384-byte wrapped-CEK fixture bytes, and the
identifiers/digests documented by the bundle recomputation script. Its provider
binding is:

```text
ProviderKind                 s3-compatible-single-part-v1
ProviderConfigurationId      c2-vector-minio-v1
NormalizedServiceUri         http://127.0.0.1:9000/
ForcePathStyle               true
RegionIdentifier             us-east-1
ProviderEndpointFingerprint  b4f942afacda0bb25a8ff90be8acbd004fbff3b3433a526024ab6bfb5e32a890
BucketName                   tagekyc-c2-vector
```

```text
PackageEqualityFingerprint
556411f661e7d2f0a32334a6666f0a9873cdcdb5c9e5cc9f8be5267ba09df1a8

PackageId
ad8b18c9-0119-5c71-bf0b-208e69cd6cba

ObjectBindingDigest
19eeb2893ca64a56d69c8b39f98f27caaae826248b3be07aaad45215b85c30b2

HeaderLength                 1554
EnvelopeLength               1580
EncryptedPackageLength       1641

EnvelopeDigest
413e5ec59723a40b1325c775f31a48095fc24575f5350e8054bccdf9a36de609

DataCiphertext               e27a42dce2
DataTag                      501e8236a7bf630de24dbb71ad220be5
CompletionTag                0fb61c1bbcd0dff0cccf60b366ea4cf7

PackageCiphertextDigest
d9cfc7a064ea0108b6db3bfe2069f8afda6d8dda1b6c444d9036f8fe217f75ad

ConditionalCreateEvidenceDigest
2a0dfa94bc2fbe31671d54b743e7ea0bf3af20e36f6c1aa7dcad766af6cd6d59

ProviderReceiptDigest
bf9ad5f91bc2033f2e3378f64c37d0ca889f186ad078b7e7efd8aea2c36b8dcd
```

The fixed wrapped-CEK bytes are a codec fixture, not a claim that RSA-OAEP is
deterministic. A separate proof performs real RSA wrap/recipient unwrap.

## 7. Durable schema

### 7.1 `raw_export_recipient_key_registrations`

```text
RecipientClientApplicationId uuid
RecipientKeyId                text matching [A-Za-z0-9._:-]{1,128}
RecipientKeyVersion           integer > 0
PublicKeyAlgorithm            text = 'RSA-OAEP-256'
PublicKeySpki                 bytea (384..1024)
PublicKeyFingerprint          bytea(32)
ValidFromUtc                  timestamptz
ValidUntilUtc                 timestamptz
State                         text IN ('Active','Revoked')
Revision                      bigint > 0
RegisteredAtUtc               timestamptz
RevokedAtUtc                  timestamptz NULL

PK (RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion)
UNIQUE (RecipientClientApplicationId, PublicKeyFingerprint)
partial UNIQUE RecipientClientApplicationId WHERE State='Active'
```

Shape rules make `RevokedAtUtc` null only for `Active`, non-null only for
`Revoked`, and require `ValidFromUtc < ValidUntilUtc`.

### 7.2 `raw_export_recipient_package_preparations`

One row per `C2PreparationId`:

```text
C2PreparationId, PackageId, PackageEqualityFingerprint
AssemblyId, JobId, AttemptId, FencingToken
AssemblyFingerprint, ManifestDigest, AssemblyDigest
AssemblyAuthenticationValue, CompleteAssemblyLength
RecipientClientApplicationId
RecipientKeyId, RecipientKeyVersion, RecipientKeyFingerprint
RecipientPublicKeySpki, RecipientKeyRevision
RecipientKeyValidFromUtc, RecipientKeyValidUntilUtc
PackageProfile, ProviderOperationTokenDigest, ObjectBindingDigest
ProviderKind, ProviderConfigurationId, ProviderEndpointFingerprint
BucketName, ObjectKey
EnvelopeDigest?, EncryptedPackageLength?, PackageCiphertextDigest?
ConditionalCreateEvidenceDigest?, ProviderReceiptDigest?
AbortAuthorizationDigest?, PositiveAbsenceEvidenceDigest?
CleanupProgressEvidenceDigest?, QuarantineEvidenceDigest?
State, Revision
SnapshotFrozenAtUtc, PutStartedAtUtc?, PreparedAtUtc?, FinalizedAtUtc?
AbortAuthorizedAtUtc?, CleanupPendingAtUtc?, AbortedAtUtc?, QuarantinedAtUtc?
```

Unique constraints cover `PackageId`, `PackageEqualityFingerprint`,
`AssemblyId`, and `ProviderOperationTokenDigest`. Exact CHECK constraints define sparse state shape. Digest fields
are exactly 32 bytes when present. The restricted locator is stored so recovery
does not depend on process memory or a reversible guess. It is never returned
to C1/public/general runtime, and credentials are never stored on this row.

### 7.3 `raw_export_recipient_package_events`

Append-only event identity is `(C2PreparationId, EventRevision)`. Event kinds:

```text
SnapshotFrozen
PutStarted
PutOutcomeUnknown
Prepared
Finalized
AbortAuthorized
CleanupPending
Aborted
Quarantined
```

Every package-row mutation and its event insert are one transaction. Event
evidence digest is required for observation/terminal events. No UPDATE or
DELETE is granted on the journal.

## 8. State machine and outcome precedence

Persistent states:

```text
Reserved
PutInFlight
PutOutcomeUnknown
Prepared
Finalized
AbortAuthorized
CleanupPending
Aborted
Quarantined
```

Allowed transitions:

```text
new → Reserved
Reserved → PutInFlight | AbortAuthorized
PutInFlight → Prepared | PutOutcomeUnknown
PutOutcomeUnknown → Prepared | Quarantined
Prepared → Finalized | AbortAuthorized
AbortAuthorized → Aborted | CleanupPending | Quarantined
CleanupPending → Aborted | Quarantined
any nonterminal object-backed state → Quarantined on exact evidence conflict
```

`Finalized`, `Aborted` and `Quarantined` are terminal. `Finalized` is not
deliverable authorization. Once `PutInFlight` commits, neither positive absence
nor a caller/process flag may return the preparation to `Reserved`, authorize a
fresh CEK/write, or declare it `Aborted`. `PutOutcomeUnknown` remains durable
until the exact object appears or exact conflicting evidence justifies
quarantine. This is the no-late-write fence: an armed request can never
resurrect an object behind a reset or terminal absence decision.

Provider outcomes retain the landed C1 enums. Exact precedence is:

```text
invalid argument/shape                       → Conflict
same C2PreparationId, different equality     → Conflict
terminal Quarantined                         → Conflict
terminal Aborted on Prepare/Finalize          → Conflict
exact Prepared/Finalized Prepare replay       → ExistingMatch + receipt
exact Finalized Finalize replay               → ExistingMatch
provider/capability unavailable               → Unavailable
write/read/delete result uncertain            → OutcomeUnknown
delete unavailable/uncertain after authority  → CleanupPending
new exact prepared object                     → Prepared
new exact finalization                        → Finalized
exact abort completion                        → Aborted
```

No generic state or revision rejection may precede exact durable replay.

## 9. SQL functions, roles and ACL

### 9.1 Roles

Migration creates only capability `NOLOGIN` roles:

```text
tagekyc_raw_export_package_preparer
tagekyc_raw_export_package_reconciler
tagekyc_raw_export_package_lifecycle
```

Deployment-owned LOGIN roles already exist in the target environment:

```text
tagekyc_raw_export_package_preparer_login
tagekyc_raw_export_package_reconciler_login
tagekyc_raw_export_package_lifecycle_login
```

Exact attributes: `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION
NOBYPASSRLS INHERIT`. Each LOGIN receives exactly one corresponding capability
membership with `ADMIN=false, INHERIT=true, SET=false`. Migration Down revokes
membership, drops only capability roles and preserves LOGIN roles.

The shared integration bootstrap provisions/verifies those three LOGIN roles
before any path migrates to the C2 migration. C2 test-method ordering is not a
prerequisite mechanism; the migration never creates or drops LOGIN roles.

### 9.2 Ten-function surface

All functions are `SECURITY DEFINER SET search_path = pg_catalog`, owned by
`tagekyc_raw_export_deployer`, use schema-qualified relations/functions and
have PUBLIC EXECUTE revoked.

| Function | Exact argument types | EXECUTE capability |
|---|---|---|
| `raw_export_select_active_recipient_key` | `uuid` | preparer |
| `raw_export_reserve_recipient_package` | `uuid, uuid, uuid, uuid, uuid, bigint, bytea, bytea, bytea, bytea, bigint, uuid, text, integer, bytea, bigint, bytea, bytea, text, text, bytea, text, text, bytea, text` | preparer |
| `raw_export_begin_recipient_package_put` | `uuid, bigint, bytea, bigint, bytea` | preparer |
| `raw_export_record_recipient_package_put_unknown` | `uuid, bigint, bytea` | preparer |
| `raw_export_record_recipient_package_prepared` | `uuid, bigint, bytea, bigint, bytea, bytea, bytea` | preparer, reconciler |
| `raw_export_read_recipient_package_recovery_context` | `uuid` | preparer, reconciler, lifecycle |
| `raw_export_finalize_recipient_package` | `uuid, bigint, bytea` | preparer |
| `raw_export_authorize_recipient_package_abort` | `uuid, bigint, bytea` | lifecycle |
| `raw_export_record_recipient_package_abort_result` | `uuid, bigint, text, bytea` | lifecycle |
| `raw_export_record_recipient_package_quarantined` | `uuid, bigint, bytea, text` | reconciler, lifecycle |

Argument order is fixed:

```text
select-active-key:
  RecipientClientApplicationId

reserve:
  C2PreparationId, PackageId, AssemblyId, JobId, AttemptId, FencingToken,
  AssemblyFingerprint, ManifestDigest, AssemblyDigest,
  AssemblyAuthenticationValue, CompleteAssemblyLength,
  RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion,
  RecipientKeyFingerprint, RecipientKeyRevision,
  PackageEqualityFingerprint, ProviderOperationTokenDigest,
  ProviderKind, ProviderConfigurationId, ProviderEndpointFingerprint,
  BucketName, ObjectKey, ObjectBindingDigest, PackageProfile

begin-put:
  C2PreparationId, ExpectedRevision, EnvelopeDigest,
  EncryptedPackageLength, PackageCiphertextDigest

put-unknown:
  C2PreparationId, ExpectedRevision, ProviderObservationEvidenceDigest

prepared:
  C2PreparationId, ExpectedRevision, ProviderEntityTagDigest,
  EncryptedPackageLength, PackageCiphertextDigest,
  ConditionalCreateEvidenceDigest, ProviderReceiptDigest

read:
  C2PreparationId

finalize:
  C2PreparationId, ExpectedRevision, AssemblyFingerprint

authorize-abort:
  C2PreparationId, ExpectedRevision, AbortAuthorizationDigest

abort-result:
  C2PreparationId, ExpectedRevision, CleanupResultKind,
  CleanupEvidenceDigest

quarantined:
  C2PreparationId, ExpectedRevision, QuarantineEvidenceDigest,
  QuarantineReason
```

The exact generic mutation projection is:

```text
outcome text
row_revision bigint
state text
```

`raw_export_record_recipient_package_prepared` appends:

```text
provider_receipt_digest bytea
```

The exact reserve projection is:

```text
outcome text
row_revision bigint
state text
package_id uuid
package_equality_fingerprint bytea
recipient_key_id text
recipient_key_version integer
recipient_key_fingerprint bytea
recipient_public_key_spki bytea
recipient_key_revision bigint
recipient_key_valid_from_utc timestamptz
recipient_key_valid_until_utc timestamptz
provider_operation_token_digest bytea
object_binding_digest bytea
provider_receipt_digest bytea
```

The exact active-key selection projection is:

```text
outcome text
recipient_client_application_id uuid
recipient_key_id text
recipient_key_version integer
recipient_key_fingerprint bytea
recipient_public_key_spki bytea
recipient_key_revision bigint
recipient_key_valid_from_utc timestamptz
recipient_key_valid_until_utc timestamptz
```

The exact recovery projection is:

```text
outcome text
row_revision bigint
state text
c2_preparation_id uuid
package_id uuid
package_equality_fingerprint bytea
assembly_id uuid
job_id uuid
attempt_id uuid
fencing_token bigint
assembly_fingerprint bytea
manifest_digest bytea
assembly_digest bytea
assembly_authentication_value bytea
complete_assembly_length bigint
recipient_client_application_id uuid
recipient_key_id text
recipient_key_version integer
recipient_key_fingerprint bytea
recipient_public_key_spki bytea
recipient_key_revision bigint
recipient_key_valid_from_utc timestamptz
recipient_key_valid_until_utc timestamptz
package_profile text
provider_operation_token_digest bytea
provider_kind text
provider_configuration_id text
provider_endpoint_fingerprint bytea
bucket_name text
object_key text
object_binding_digest bytea
envelope_digest bytea
encrypted_package_length bigint
package_ciphertext_digest bytea
conditional_create_evidence_digest bytea
provider_receipt_digest bytea
abort_authorization_digest bytea
positive_absence_evidence_digest bytea
cleanup_progress_evidence_digest bytea
quarantine_evidence_digest bytea
```

Optional sparse fields return SQL NULL. Changing a function name, argument,
return order/type or ACL matrix is STOP/RRI.

Tables remain owner-only. Capabilities receive schema USAGE and only the exact
function EXECUTE grants above. No runtime/general C1 role receives package
table/function access.

## 10. Provider protocol

### 10.1 Prepare

1. Validate the trusted request. Through the already-landed assembly-sealer
   capability call `raw_export_read_assembly_recovery_context(C2PreparationId)`
   and require exact `AssemblyId`/`AssemblyFingerprint` equality. That existing
   projection supplies trusted `JobId`, `AttemptId` and `FencingToken`; no C1
   function, grant or request field is added.
2. Probe the package row without locking. For an existing row, use only its
   frozen key/locator identity; for a missing row, call
   `raw_export_select_active_recipient_key(RecipientClientApplicationId)`.
3. For a missing package row, generate one transient operation token/digest.
   Compute equality, `PackageId`, final deterministic object key, endpoint fingerprint
   and object binding from that exact candidate. Reserve SQL locks/revalidates
   the exact candidate key, re-derives those identities and freezes the complete
   snapshot/locator/token digest. Selection alone never freezes or authorizes
   provider I/O. Existing-row replay uses only the frozen digest.
4. For exact `Prepared|Finalized`, return persisted receipt without key
   resolution, encryption or object I/O.
5. For `Reserved`, generate CEK/nonce, wrap CEK and stream the C1 writer into a
   bounded, zeroizing, encrypted-only pooled spool. This computes exact
   envelope/package digests and length before provider I/O. Complete plaintext
   is never buffered and no temp file is used.
6. CAS `PutInFlight` with the exact operation-token, envelope digest, package
   digest and encrypted length, then issue one single-part conditional Put from
   the encrypted spool with `If-None-Match: *`.
7. On successful Put, compare provider evidence to the precommitted digest and
   CAS `Prepared`.
8. On ambiguous response, CAS `PutOutcomeUnknown`, then inspect exact key.
9. Exact present object must match the precommitted envelope digest, encrypted
   length and complete package digest before CAS `Prepared`; no decryption
   occurs.
10. After `PutInFlight` has committed, positive absence is observational only:
    state remains `PutOutcomeUnknown`, and no fresh CEK, token or Put is allowed.
    Only work that failed before the `PutInFlight` CAS remains `Reserved` and
    may start an attempt.
11. Identity/evidence mismatch becomes `Quarantined`; it is never overwritten
    or auto-deleted.

`C2-DURABLE-PUT-TERMINAL-EVIDENCE-V1` is the future gate for any bounded
terminal-absence/reset policy after an armed Put. Until a provider supplies
durable evidence that the exact operation can no longer linearize, an absent
armed operation remains non-deliverable `PutOutcomeUnknown`. This is an
intentional fail-safe pending state, not a claim of successful cleanup.

### 10.2 Finalize

`Finalize` admits only exact `Prepared` plus matching assembly fingerprint and
expected revision. It updates no package byte or recipient snapshot. Exact
`Finalized` replay returns `ExistingMatch`; other terminal or mismatched states
return `Conflict`.

### 10.3 Abort

C1 first proves its landed durable `AbortAuthorized` digest. C2 then persists
the same authorization only from `Reserved` (no Put armed) or `Prepared`.
`PutInFlight` and `PutOutcomeUnknown` cannot enter `AbortAuthorized`: they stay
pending until exact presence becomes `Prepared` or exact conflict becomes
`Quarantined`. The lifecycle capability GETs and hashes the exact package before
deleting it, then uses one exact-key single delete. A failed/unknown delete
records `CleanupPending`; retry again performs exact inspection. Two exact
positive-absence observations are required before `Aborted`. A mismatch goes
`Quarantined`; `Finalized` cannot be aborted. HEAD-only metadata, one failed
delete, a caller boolean and process-local state are insufficient evidence.

## 11. S3-compatible reference provider

The controlled reference uses the already pinned MinIO server/client digests
and a separate bucket plus fixed final-key namespace from source custody. Four distinct credentials are
required:

| Capability | Required operations |
|---|---|
| writer | exact-prefix single-part `PutObject` only |
| reconciler | exact-prefix `GetObject` + `HeadObject` only |
| lifecycle | exact-prefix `GetObject` + `HeadObject` + single-key `DeleteObject` only |
| posture probe | bucket-only `GetBucketVersioning`, `GetBucketObjectLockConfiguration`, `GetLifecycleConfiguration`, `GetBucketPolicy` |

The lifecycle GET is required to hash and compare the complete package before
delete; HEAD metadata alone is not byte-identity evidence. The posture probe
has no object GET/HEAD/PUT/DELETE authority, and the other three credentials
have none of its bucket-posture operations.

Cross-capability, cross-bucket and cross-namespace operations are denied. PUBLIC
access, lifecycle rules, versioning and Object Lock are absent. Forbidden SDK
members are matched by exact symbol, not substring:

```text
CreateMultipartUpload, UploadPart, CompleteMultipartUpload, AbortMultipartUpload
ListObjects, ListObjectsV2, CopyObject, GetPreSignedURL
DeleteObjects, DeleteObjectsAsync, DeleteObjectsRequest
```

`DeleteObjectAsync(DeleteObjectRequest, CancellationToken)` is the only delete
member allowed and only in the lifecycle provider.

## 12. Readiness and fail-closed codes

Package readiness is explicit and separate from `RawExportAssemblyTopology`.
The service extension registers C2 components only when called by an authorized
composition root; it does not edit `Program.cs` or appsettings in this slice.

Exact first-code precedence:

```text
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CONFIG_INVALID
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_ROLE_TOPOLOGY_INVALID
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CATALOG_INVALID
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_PROVIDER_UNAVAILABLE
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_BUCKET_UNAVAILABLE
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_OBJECT_LOCK_PROHIBITED
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_VERSIONING_PROHIBITED
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_LIFECYCLE_PROHIBITED
PROD_RAW_EXPORT_RECIPIENT_PACKAGE_PUBLIC_ACCESS_PROHIBITED
```

Catalog readiness proves three tables, ten exact function signatures, owner,
SECURITY DEFINER/search path, ACL matrix, role attributes/memberships and no
extra overload. It does not require a recipient-specific active key to exist.
Prepare returns `Unavailable` for no eligible key without exposing whether a
recipient is enrolled.

Live provider readiness uses only the posture-probe credential. It verifies the
configured provider instance/bucket and the four bucket-level postures above;
writer, reconciler and lifecycle credentials are separately denied those calls.

Object-lock precedence is before versioning because real S3/MinIO Object Lock
requires versioning. Null/unknown/403/5xx posture is fail-closed, never treated
as absent.

## 13. Concurrency and lock order

The C2 relative order is:

```text
recipient-key row
→ package preparation row
→ package event row
→ provider I/O outside transaction
→ post-I/O package CAS
```

Replay may perform a non-locking package probe to discover the frozen key
identity, but then acquires key→package and revalidates. No code acquires
package→key. Deployment revoke takes only the exact key row. Object operations
never occur while SQL locks are held.

The landed C1 recovery-context read used for lineage is non-locking. It is
exact-compared before reserve and re-read after any wait that could stale the
lineage; it introduces no package→C1 lock edge.

Every state mutation uses exact expected revision plus immutable package
identity/fingerprint. Response-loss recovery prefers exact replay before stale
revision conflict. PostgreSQL `40P01` is not a business outcome.

## 14. Exact permanent-path allowlist (34)

Only these paths may change during a ratified build:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c2_recipient_package_review_ledger.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c2_recipient_package_as_built.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_as_built.md
src/TagEkyc.Contracts/RawExport/RawExportAssemblyContracts.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientKeyRegistrationRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackagePreparationRow.cs
src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackageEventRow.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackagePreparationConfig.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackageEventConfig.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageContracts.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageOptions.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageCodec.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageCryptoService.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageEncryptedSpool.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageRepository.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageObjectClientFactory.cs
src/TagEkyc.Infrastructure/RawExport/S3CompatibleRecipientPackageProvider.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReadinessValidator.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs
tests/TagEkyc.UnitTests/Tip88C1C1AssemblyCodecTests.cs
tests/TagEkyc.ArchTests/Tip88C1C1ResolverAssemblyArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs
tests/TagEkyc.UnitTests/Tip88C1C2RecipientPackageCodecTests.cs
tests/TagEkyc.ArchTests/Tip88C1C2RecipientPackageArchTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs
tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
```

The list above actually contains 34 entries. `PostgresPersistenceFixture`
adds/verifies only the three deployment-owned C2 LOGIN prerequisites before
Migration Up. `Tip88B1E3ResolverReadBoundaryTests` may change only its existing
`ExpectedModelSnapshotSha256` constant after C201 proves that the C2 model delta
is additive; no other E3 assertion or test census may change.
No `.csproj`, `Program.cs`, appsettings, C1 dispatch, C1 migration or predecessor
production file outside the two named C1 code paths may change. A 33rd path is
the pre-Up bootstrap amendment, the 34th is the E3 snapshot tripwire amendment,
and a 35th path is STOP/RRI.

## 15. Proof manifest (C201–C226)

Each proof has a positive control and the named mutation must turn RED for the
stated discriminator. Scratch mutations are restored byte-identically.

| ID | Required proof and discriminating mutation |
|---|---|
| C201 | pre-Up bootstrap proves three exact C2 LOGIN prerequisites; apply/Down/reapply, pending-model clean, exact additive three-table model and count-neutral E3 snapshot-SHA tripwire update; remove one bootstrap role/mapping/constraint or restore the predecessor snapshot SHA → RED |
| C202 | exact three capability-role/three LOGIN-role attributes and memberships, owner and ACL; grant PUBLIC or wrong membership → RED |
| C203 | ten exact function signatures/ACL matrix, active-key selector present, unsafe reset absent and zero overload; wildcard/extra overload/reset function → RED |
| C204 | C1 passes server-derived recipient exactly; substitute client id or empty recipient → RED |
| C205 | candidate-select→exact-key-lock/revalidate, package equality/PackageId/provider-instance object-binding golden vector; rotate between select/reserve or reorder/omit one member → RED |
| C206 | envelope/frame/completion absolute vector; change endianness/AAD/frame order or render one JCS GUID as `N` instead of lowercase `D` → RED |
| C207 | operation-token derivation plus receipt/conditional-create/observation/cleanup/quarantine vectors and all-member sensitivity; substitute token/digest or omit recipient/key/provider-instance/object member → RED |
| C208 | real RSA public wrap and test-recipient private unwrap/decrypt; introduce any production unwrap method/dependency → architecture RED |
| C209 | bounded 33,554,432-byte plaintext, 32-frame/1,848-header/33,557,106-package arithmetic and zeroizing encrypted-only spool; exceed one bound, buffer complete plaintext, use temp or leave unzeroized ciphertext `MemoryStream` → RED |
| C210 | single-part conditional create at the persisted final `ObjectKey`, exact response-loss recovery, no overwrite and restart config-id/endpoint-fingerprint equality; remove `If-None-Match:*`, prepend/rewrite a configured prefix, or redirect same bucket/key to another endpoint → RED |
| C211 | exact durable Prepare/Get replay before revision conflict; same id/different equality → Conflict; weaken comparator → RED |
| C212 | revoke-before-reserve fails; reserve-before-revoke completes frozen package; selection/rotation race writes nothing or retries; replay never latest-selects; re-resolve current key → RED |
| C213 | exact Prepared→Finalized and Finalized replay; wrong assembly fingerprint/revision changes nothing; weaken comparator → RED |
| C214 | authorized exact abort only from unarmed `Reserved` or exact `Prepared`; lifecycle GET/hash, single delete, `CleanupPending`, and two-observation absence; admit `PutInFlight|PutOutcomeUnknown`, use HEAD-only, one observation or caller boolean → RED |
| C215 | mismatched existing object quarantines and is not deleted/overwritten; change quarantine to delete/put → RED |
| C216 | process/provider restart recovers exact object without decrypting; paused armed Put cannot be reset/aborted and late completion converges only to Prepared/conflict quarantine; add cache/CEK dependency or positive-absence reset → RED |
| C217 | writer/reconciler/lifecycle/posture credentials are distinct with exact allow/deny matrix; swap/share one credential or remove lifecycle GET → RED |
| C218 | posture-only bucket calls and exact forbidden SDK member set; grant object CRUD to posture or allow multipart/list/copy/presign/batch delete → RED |
| C219 | CEK/plaintext scratch zeroization and redaction; log/exception mutation exposing protected value → RED |
| C220 | all 31 landed C1 tests remain active; pass-2 source read count and assembly bytes unchanged; third-read workaround → RED |
| C221 | every provider state/outcome and precedence row is reachable; swap replay/conflict/unavailable precedence → RED |
| C222 | revoke/select/reserve/reconcile/abort and paused-Put forced interleavings follow key→package, terminate without `40P01`, and cannot create late-write resurrection; reverse lock or admit reset/Aborted while Put is armed → RED |
| C223 | readiness first-code matrix including ObjectLock-before-Versioning and unknown/403/5xx fail-closed; adjacent swap/predicate invert → RED |
| C224 | recipient parser rejects truncation, duplication, reorder, bad completion, tag and assembly digest; skip completion/tag verification → RED |
| C225 | fixture evidence proves `ART-003_REFERENCE_PACKAGE_EVIDENCE_SATISFIED` only; mutation claiming production/real-data closure → RED |
| C226 | exact 34-path census with 32→33 bootstrap and 33→34 E3-tripwire amendment provenance, no public locator/delivery/API/topology/Program surface; add forbidden path/member → RED |

Mutation closeout is one complete matrix pass after canonical positive tests.
Repeated ad-hoc full-suite runs are forbidden.

## 16. Validation policy

During implementation:

1. build only affected projects;
2. run changed C2 unit/architecture tests;
3. run focused C2 integration tests, including pre-Up LOGIN bootstrap;
4. run the 31 affected landed C1 tests only when C1 request/orchestrator/test
   bytes change;
5. run C201 migration/pending-model gates after schema changes;
6. run each named mutation with immediate byte-identical restoration; and
7. run the complete unfiltered Release suite exactly once on final closeout
   bytes.

The final suite is not replaced by a diagnostic subset. An environment-only
failure may use standing environment self-heal authority, but repository bytes
must remain unchanged and the replacement run must be explicitly classified.

## 17. Task sequence

### Task 0 — freeze on-code prerequisites

- verify branch/HEAD and staged zero;
- verify scope/dispatch hashes and 34 paths;
- verify the exact ten-function argument/return projections, including the
  selector and absence of a reset-after-unknown surface, against the
  dispatch before migration authoring;
- prove AWS SDK supports exact single-part `If-None-Match:*`, streaming content
  length and exact HEAD/GET semantics on the pinned MinIO image;
- confirm RSA OAEP SHA-256 with DER SPKI works on net8.0 without a package
  change; and
- recompute all absolute vectors independently.

Failure of any Task-0 feasibility item is STOP/RRI before schema work.

### Task 1 — contracts/codecs and C1 delta

Implement the one recipient field, fixed codecs, vector tests and capability
architecture test. Preserve all C1 bytes/semantics except the request shape and
constructor call.

### Task 2 — schema/ACL/readiness

Implement three tables, ten functions, three capability roles, exact login
memberships, model snapshot and C201–C203/C223.

### Task 3 — reference provider

Implement bounded crypto stream, S3 capability clients, durable repository,
prepare/recovery/finalize/abort and C204–C218.

### Task 4 — convergence and closeout

Run C219–C226, the full mutation matrix, final affected tests, one unfiltered
Release suite, freeze hashes, update as-built/ledger and create a review-only
bundle. Stop at independent review; do not stage or commit.

## 18. STOP/RRI conditions

Stop before proceeding if implementation requires:

- a 35th permanent path;
- any C1 change beyond the recipient field and constructor propagation;
- `RawExportAssemblyTopology`, `Program.cs`, appsettings or `.csproj` change;
- a new C1 SQL function/read projection or grant;
- recipient private key, TagEkyc CEK unwrap/recovery, escrow or source KEK reuse;
- package bytes that require server DB state to reconstruct identity/provenance;
- multipart, versioning, Object Lock, plaintext over `33,554,432`, header over
  `1,848`, more than 32 data frames, or package over `33,557,106` bytes;
- a changed state/outcome/domain/vector not corrected by reviewed authority;
- a SQL function outside the exact ten-function surface;
- any reset/new CEK/terminal-absence path after `PutInFlight` commits;
- inability to recover response loss without plaintext or CEK persistence;
- a mutation that remains GREEN or REDs for an unrelated reason;
- production/real-data ART closure, delivery or locator exposure; or
- any non-canonical final-suite failure.

## 19. Round-3 exact-byte review questions

An independent reviewer must answer in one pass:

1. Is the recipient-key snapshot/revoke linearization executable and deadlock
   free?
2. Does every crash window either converge without TagEkyc CEK recovery or
   remain durably non-deliverable/non-forking behind the named terminal-evidence
   gate?
3. Is exact-object structural recovery sufficient and non-vacuous?
4. Does the self-contained envelope authenticate order, termination, totals and
   C1/recipient binding for the recipient?
5. Are package identity, receipt and conditional-create codecs non-circular and
   fully reproducible?
6. Are the state sparse shapes, transition precedence and SQL signatures
   complete?
7. Are capability roles, S3 credentials and ACLs least-privilege without hidden
   shared authority?
8. Does abort avoid deleting unknown/mismatched objects?
9. Does the allowlist include every necessary predecessor tripwire and no
   unrelated path?
10. Do C201–C226 discriminate the contract rather than source substrings?

Expected clean verdict:

```text
PASS — READY FOR HOMEOWNER C2 CONTROLLED-BUILD RATIFICATION
```

## 20. Explicit non-claims

```text
NO IMPLEMENTATION AUTHORITY
NO PRODUCTION ACTIVATION
NO REAL RAW BIO
NO RECIPIENT MANAGEMENT API
NO DELIVERY / DOWNLOAD / LOCATOR / CALLBACK / OUTBOX
NO RECIPIENT-INDEPENDENT PACKAGE SIGNATURE
NO SERVER-SIDE PACKAGE DECRYPTION OR CEK RECOVERY
NO MULTIPART / VERSIONING / OBJECT LOCK / HEAVY MEDIA
NO ART-003 PRODUCTION OR REAL-DATA CLOSURE
NO STAGE / COMMIT / PUSH / MERGE / PR / DEPLOY
```

## 21. Round-2 reconciliation carried into this successor

Dispatch v0.1 SHA-256
`64AFF414A90B56F9CED302BA6E1B8DED858376BF79FFE85B9049075F2CEF2AA7`
received two independent reports on the same bundle. One report returned FAIL
with seven executable findings; the other returned PASS with two bounded
findings. A PASS did not erase the open FAIL findings. On-code and scope-bound
adjudication accepted all nine:

```text
GPT-F01  missing landed C1 lineage-read hop                    ACCEPTED
GPT-F02  key-snapshot / PackageId circularity                  ACCEPTED
GPT-F03  late-Put resurrection after reset/abort               ACCEPTED
GPT-F04  non-durable locator / provider-instance binding       ACCEPTED
GPT-F05  missing posture and abort-inspection capabilities     ACCEPTED
GPT-F06  plaintext/package bound conflation                    ACCEPTED
GPT-F07  pre-Up LOGIN bootstrap absent from allowlist          ACCEPTED
CC-F01   CRLF review manifests break POSIX sha256sum            ACCEPTED
CC-F02   GUID representation differs by canonical surface       ACCEPTED
```

The reconciliation sweep also closed two sibling inconsistencies rather than
deferring them to another document round:

```text
CON-F01  scope-required CleanupPending missing from states      CLOSED
CON-F02  C202 said six LOGIN roles although C2 defines three    CLOSED
```

The scope v0.2 bytes and decisions D1–D7 remain unchanged. This is the third
and final document round under the convergence policy. The two exact-byte
reviews disagreed: one returned PASS and independently verified the 14
downstream vectors, while the other found three bounded executable
contract/allowlist contradictions. On-code and intra-document adjudication
accepted all three without reopening scope or planning:

```text
R3-F01  ProviderOperationTokenDigest derivation absent         ACCEPTED
R3-F02  E3 exact ModelSnapshot SHA tripwire outside allowlist  ACCEPTED
R3-F03  configured prefix absent from historical binding       ACCEPTED
```

This R3C1 correction pins the transient-token contract, admits the exact E3
tripwire as path 34 and removes mutable-prefix semantics by making `ObjectKey`
the final key. The prior 14 package/evidence vector values remain unchanged;
the token-derivation vector is additional and independently recomputable.
There is no Round 4 planning pass. After Homeowner ratifies the corrected exact
SHA, remaining uncertainty moves to implementation and discriminating tests.
