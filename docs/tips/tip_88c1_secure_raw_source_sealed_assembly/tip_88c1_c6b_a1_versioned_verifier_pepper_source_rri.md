TIP-88C1-C6B-A1 — SELF-CONTAINED BOUNDED RRI v2
VERSIONED VERIFIER PEPPER SOURCE, CANONICAL VERIFIER SECRETS,
AND PEPPER LIFECYCLE AUTHORITY

RRI ID:
C6B-A1-VERSIONED-VERIFIER-PEPPER-SOURCE-01

VERSION:
v2 — CONSOLIDATED SELF-CONTAINED CANDIDATE

BOUND AMENDMENT:
C6B-A1-VERSIONED-VERIFIER-PEPPER-SOURCE-01-A2
SHA-256 `6CB725C1217DDDA5E59CD65393FCC62953F36FA7C003B71460B1DAD09377F09A`

STATUS:
RRI CANDIDATE — NOT IMPLEMENTATION AUTHORITY UNTIL HOMEOWNER RATIFICATION
AND BOUNDED INDEPENDENT PASS

THIS DOCUMENT SUPERSEDES ALL EARLIER CHAT-ONLY DRAFTS, ADDENDA AND DELTAS
WITH THIS RRI ID.

NO READER OR BUILDER IS REQUIRED OR PERMITTED TO RECONSTRUCT AUTHORITY FROM
EARLIER CHAT MESSAGES.

============================================================
0. EXACT BASELINE AND STOP STATE
================================

HISTORICAL PRE-AMENDMENT STOP BASELINE — NON-OPERATIVE:

The hashes in this §0 block preserve the exact bytes at which the original
pepper-source STOP was raised. They are not the current executable catalogue.
The authoritative successor hashes are the exact catalogue in the bound Parent
v0.8; no Builder may consume a §0 predecessor hash as current authority.

Parent v0.7:
F3D6B76CA8A157AA5DC47BB0D6C642618D2ACEE41CC19A6A64CB70FC6F1CA4DE
824 lines

Operation Master:
B1094A807D68BE98D51D9A427AFC92FDD899A878DEF6487D2D79C6A3DE594141
1082 lines

Planning v1.8:
DACA8141CC68E03AAB9D858C547DBC4FAB7A542E6CAD0F66AB43E37116B0F0D6

Umbrella technical dispatch:
F41C24BD0BD58F6B070EFE17BB69E7F89823F37B94F48A339870CD19D67825FD

Current companion/evidence baseline:

DDL:
86B4B9960F943A86B6178FA548945A99D90057D61D4ED631151D447D844113D7

T1:
A4B80DE9BE3BDDF227D5B2BEFF067304E99423B0A9317F98CEC5896C77341169

T2:
FD6BEA8156A6E9F6285CF30678CB3DDB96C71526711D78FCE07C473669CE9410

T3:
689A80006F030C6241D9CCA0A1D4A95E28891BB4255B5EE019695C1BF320EEBA

T4:
9B11EE079ADEF2DA2B9DED1366BC81ED0A0492610C2F1CD2477F0918C640FEEA

Core proof:
CC14649510AF5702448CCF8142292B0B481ED7E69010BD7E08525D1012DF96B8

Runtime proof:
FE12F9342FDB79BD39C4ECC3A7F222A165BFC1BF3884F4A0E6D8EFB5F0D34B3F

Identity/audit proof:
9F857DBA979C658B4D2F1514E4C0100D5B467C9526D2AA9831440FAE0495832F

Expiry proof:
DC284FAFF9C3C0E7016D38E10A57D0CDA4D13291646208BE8455CD189DE231BD

Implementation HARD STOP:

C6B-A1-VERSIONED-VERIFIER-PEPPER-SOURCE-01
= OPEN

Evidence before STOP:

* Contracts/Application A1 work had started.
* Application build passed.
* Main A1 migration contained the current DDL/T1-T4 composition.
* Infrastructure build passed at the STOP checkpoint.
* Migration Designer had not been regenerated.
* `CaptureRuntimeEntities.cs` and
  `CaptureRuntimeEntityConfigurations.cs` were incomplete/non-candidate.
* API/Auth implementation had not proceeded.
* no `.csproj` mutation;
* staged = 0;
* no commit;
* no push.

While this RRI is OPEN:

NO additional product code/schema/test/config/migration/Designer mutation.
NO stage.
NO commit.
NO push.

Only authority/document reconciliation is permitted.

============================================================

1. SCOPE AND PRECEDENCE
   ============================================================

This RRI closes only:

A. exact versioned verifier-pepper source;
B. exact verifier-pepper cryptographic material;
C. canonical representation of all 32-byte A1 verifier secrets;
D. exact HKDF domain derivation;
E. API-host CurrentVersion ownership;
F. offline R01 version/SecretRef ownership;
G. historical-version resolution;
H. readiness/reference-safe retirement;
I. supersession of the older product-owned pepper lifecycle model;
J. exact artifact/proof authority required to implement the above.

THIS RRI DOES NOT REOPEN:

* Planning D1-D13 except the exact clauses expressly superseded below;
* runtime identity model;
* platform-operator business identity;
* credential rotation semantics;
* R10 ordering/concurrency;
* capability ownership/audience/TTL/replay/bind semantics;
* expiry-on-denied;
* A1/A3 ownership;
* A2;
* A3;
* A4 secret VALUES.

PRECEDENCE RULE:

For A1 only, this RRI supersedes Planning v1.8 and the umbrella technical
dispatch ONLY where this document explicitly identifies:

1. verifier-pepper lifecycle commands/current-version CAS; or
2. variable-length "at least 256 bits" language for BootstrapSecret or
   CaptureCapabilitySecret; or
3. underspecified verifier-pepper material/derivation/canonical encoding.

All unrelated Planning v1.8 authority remains in force.

Successor Parent MUST bind the exact SHA of this RRI as a normative catalogue
member.

No implicit chat context carries authority.

============================================================
2. ONE CANONICAL 32-BYTE BASE64URL CODEC
========================================

The following codec is normative for ALL four material classes:

A. verifier-pepper master material resolved from SecretRef;
B. platform operator credential secret payload after `teo_`;
C. BootstrapSecret;
D. CaptureCapabilitySecret.

Accepted textual input MUST:

* be exactly 43 characters;
* be ASCII only;
* match exactly `[A-Za-z0-9_-]{43}`;
* contain no `=`;
* contain no whitespace;
* contain no CR;
* contain no LF;
* contain no TAB;
* contain no NUL;
* contain no BOM;
* undergo no Unicode normalization;
* undergo no case folding;
* undergo no Trim/TrimEnd;
* undergo no URL decoding;
* undergo no parser repair.

Canonical decode algorithm:

1. Reject unless length is exactly 43.

2. Reject unless every character is in:

   `A-Z a-z 0-9 - _`

3. Translate:

   `-` -> `+`
   `_` -> `/`

   then append EXACTLY one `=`.

4. Base64-decode.

5. Reject unless result length is exactly 32 bytes.

6. Re-encode those exact 32 bytes using standard base64.

7. Convert to canonical unpadded base64url:

   remove trailing `=`;
   `+` -> `-`;
   `/` -> `_`.

8. Compare that canonical result with the ORIGINAL 43-character input using
   ordinal, case-sensitive exact equality.

9. Reject unless equal.

`Convert.FromBase64String(...)` or an equivalent permissive decoder ALONE is
NOT sufficient.

Decode-length equality is NOT canonicality proof.

============================================================
2.1 TAIL-BIT INVARIANT
======================

32 bytes = 256 bits.

43 base64url characters expose 258 encoded bit positions.

Therefore the final symbol contains:

* four data bits;
* two canonical zero tail bits.

The low two bits of the final 6-bit symbol MUST be zero.

Thus the 43rd character of a canonical 32-byte value belongs to exactly:

A E I M Q U Y c g k o s w 0 4 8

This 16-character set is an invariant/proof aid.

The normative enforcement remains:

decode
-> canonical re-encode
-> ordinal exact compare.

For every canonical 32-byte encoding, three noncanonical aliases exist that
share the same high four final-symbol bits.

Those aliases MUST be rejected even if a permissive decoder returns the same
32 bytes.

============================================================
2.2 NO NORMALIZATION OF SECRETREF VALUES
========================================

For Capture Runtime verifier pepper, A1 consumes the exact textual value exposed
by the approved SecretRef resolution boundary.

A1 MUST NOT:

* Trim;
* TrimEnd;
* remove a terminal CR/LF;
* normalize Unicode;
* repair padding/alphabet.

A file/env secret containing:

`<43 canonical chars>\n`

or:

`<43 canonical chars>\r\n`

is INVALID.

Deployment must store the canonical value itself.

The bound A2 amendment closes the landed generic resolver incompatibility.
Capture Runtime verifier-pepper material is resolved only by the public
`CaptureRuntimeVerifierPepperSecretRefResolver` in
`CaptureRuntimeVerifierPepperProvider.cs`. Its `env:` branch preserves the exact
process value; its `file:` branch validates the actual raw bytes before any text
conversion. The generic resolver remains unchanged and is non-operative for
Capture Runtime verifier-pepper material.

This RRI does NOT authorize mutation of unrelated generic SecretRefResolver
behavior.

============================================================
3. EXACT SECRET SIZES AND PLANNING NARROWING
============================================

For A1 successor authority:

VerifierPepper master:
exactly 32 decoded bytes.

Platform operator secret payload:
exactly 32 decoded bytes.

BootstrapSecret:
exactly 32 CSPRNG bytes.

CaptureCapabilitySecret:
exactly 32 CSPRNG bytes.

Each is represented externally, where textual representation exists, by exactly
one canonical 43-character value under §2.

---

## 3.1 EXPLICIT SUPERSESSION OF "AT LEAST 256 BITS"

Planning v1.8 contains language equivalent to:

CaptureCapabilitySecret:
`at-least-256-bit`

Bootstrap:
`at least 256 bits of server entropy`

The umbrella technical material also contains equivalent variable-length
language such as:

`issue at least 256-bit secret`

for these A1 domains.

For A1 successor authority, those variable-length statements are explicitly:

SUPERSEDED FOR A1 BY
C6B-A1-VERSIONED-VERIFIER-PEPPER-SOURCE-01

Sole operative rule:

BootstrapSecret
= exactly 32 bytes.

CaptureCapabilitySecret
= exactly 32 bytes.

This is an intentional protocol NARROWING, not a security-floor reduction:

previous floor:
>= 256 bits

successor exact value:
256 bits

Therefore the 256-bit floor is preserved.

Larger secret sizes simply cease to be valid members of the A1 wire protocol.

This narrowing changes no other capability semantic property.

============================================================
3.2 PLATFORM OPERATOR KEY
=========================

Wire form remains:

literal `teo_`
+
exactly one canonical 43-character §2 payload.

Only the payload is decoded.

The complete presented key is therefore exactly 47 ASCII characters.

A noncanonical alias that decodes to the same 32 bytes MUST be rejected.

No normalization occurs before successful authentication.

============================================================
3.3 BOOTSTRAP SECRET
====================

BootstrapSecret:

* exactly 32 CSPRNG bytes;
* exactly one canonical 43-character representation;
* no noncanonical textual alias is accepted.

Digest input uses only the exact decoded 32 bytes.

============================================================
3.4 CAPTURE CAPABILITY SECRET
=============================

CaptureCapabilitySecret:

* exactly 32 CSPRNG bytes;
* exactly one canonical 43-character representation;
* no noncanonical textual alias is accepted.

Digest input uses only the exact decoded 32 bytes.

============================================================
3.5 GENERATED SECRET ENCODING
=============================

Generated platform/bootstrap/capability secrets use:

Base64(32 bytes)
-> remove trailing `=`
-> `+` to `-`
-> `/` to `_`

Result MUST be exactly 43 characters and MUST pass §2 when decoded again.

One 32-byte secret has exactly ONE accepted textual representation.

============================================================
4. EXACT VERIFIER-PEPPER MASTER MATERIAL
========================================

For configured verifier-pepper version V:

`CaptureRuntimeVerifierPepperSecretRefResolver` resolves one exact textual
`env:` value or one exact 43-byte `file:` value under the bound A2 amendment.

That exact value MUST pass §2.

The decoded exact 32 bytes are:

K_master[V]

K_master[V] is the sole master pepper material for version V.

The textual SecretRef value is never used directly as HMAC/HKDF key material.

There is NO additional subjective "weak material" test.

VALID:
exactly one canonical §2 value -> exactly 32 bytes.

INVALID:
anything else.

Do NOT:

* estimate entropy from visible characters;
* apply password-strength heuristics;
* reject based on repeated-byte patterns;
* invent other min/max lengths.

The 32 bytes are assumed to originate from an approved CSPRNG-controlled
deployment secret process.

============================================================
5. EXACT HKDF-SHA256 DERIVATION
===============================

One K_master[V] derives exactly three domain-specific 32-byte keys.

Algorithm:

HKDF-SHA256.

Definitions:

Hash = SHA-256
HashLen = 32
L = 32

IKM:

K_master[V]

Salt:

exactly 32 zero bytes.

Extract:

PRK[V] =
HMAC-SHA256(
key  = 32 zero bytes,
data = K_master[V]
)

Exact HKDF-Expand info ASCII bytes:

Platform:

TAG-EKYC-C6BA1-VERIFIER-PEPPER/platform-credential-v1

Bootstrap:

TAG-EKYC-C6BA1-VERIFIER-PEPPER/bootstrap-digest-v1

Capability:

TAG-EKYC-C6BA1-VERIFIER-PEPPER/capability-digest-v1

Rules:

* ASCII exactly;
* no terminating NUL;
* no CR;
* no LF;
* no surrounding whitespace;
* no Unicode encoding ambiguity.

For domain D:

T1 =
HMAC-SHA256(
key  = PRK[V],
data = ASCII(info[D]) || 0x01
)

Because:

L = HashLen = 32

there is exactly one HKDF-Expand block.

K_domain[V,D] = T1.

No version number is inserted into:

* salt;
* info;
* IKM decoration.

VerifierPepperVersion selects K_master[V].

Do NOT insert:

* environment name;
* machine ID;
* deployment ID;
* SecretRef text;
* version decimal;
* credential ID;
* ClientApplicationId;
* prefix

into HKDF.

============================================================
6. EXACT STORED DIGEST FORMULAS
===============================

Platform operator credential:

Digest =
HMAC-SHA256(
key  = K_domain[V, Platform],
data = ASCII("platform-credential-v1")
|| 0x00
|| decoded-platform-secret-32
)

Bootstrap secret:

Digest =
HMAC-SHA256(
key  = K_domain[V, Bootstrap],
data = ASCII("bootstrap-digest-v1")
|| 0x00
|| decoded-bootstrap-secret-32
)

Capture capability secret:

Digest =
HMAC-SHA256(
key  = K_domain[V, Capability],
data = ASCII("capability-digest-v1")
|| 0x00
|| decoded-capability-secret-32
)

Every durable digest is exactly 32 bytes.

Comparisons are fixed-time over exactly 32 bytes.

Any older shorthand:

K_pepper_version

means the appropriate:

K_domain[V,D]

NOT raw K_master[V].

Raw K_master[V] MUST NOT be used as a final digest HMAC key.

============================================================
6.1 TEXTUAL ALIAS RULE
======================

For a canonical text S and noncanonical alias S':

if:

DecodePermissive(S) = DecodePermissive(S')

only S may authenticate/verify.

S' MUST fail canonical parsing.

This applies to:

* platform key payload;
* BootstrapSecret;
* CaptureCapabilitySecret;
* verifier-pepper SecretRef material.

No textual alias may create an alternate successful authentication,
fingerprint, replay or idempotency path.

============================================================
7. SHARED PURE CRYPTOGRAPHIC AUTHORITY
======================================

Create exactly:

src/TagEkyc.Application/CaptureRuntime/
CaptureRuntimeVerifierCryptography.cs

Purpose:

one pure dependency-free A1 implementation authority for:

* canonical §2 codec;
* canonical encoding;
* HKDF §5;
* domain-key derivation;
* the three §6 digest formulas;
* A1-owned buffer zeroization helpers where appropriate.

It MUST NOT:

* read configuration;
* resolve SecretRef;
* open DB connections;
* reference Api;
* reference Infrastructure;
* contain Client API-key pepper logic.

Existing project graph permits:

Infrastructure -> Application

and the offline ApiKeyProvisioner already references both Application and
Infrastructure.

Therefore this shared implementation requires NO `.csproj` mutation.

The API-host provider and offline R01 adapter MUST use this exact shared
cryptographic authority rather than independently implementing competing codecs
or KDFs.

============================================================
8. APPLICATION PORT
===================

Add to existing:

src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs

exact semantic types equivalent to:

CaptureRuntimeVerifierPepperDomain:
PlatformCredential
BootstrapDigest
CapabilityDigest

ICaptureRuntimeVerifierPepperSource:

```
int CurrentVersion { get; }

ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(
    int version,
    CaptureRuntimeVerifierPepperDomain domain,
    CancellationToken cancellationToken = default)
```

ICaptureRuntimeVerifierPepperLease:

```
int Version { get; }

CaptureRuntimeVerifierPepperDomain Domain { get; }

ReadOnlyMemory<byte> Key { get; }

Dispose()
```

Required semantics:

* Key is exactly one derived 32-byte K_domain[V,D].
* Application never receives K_master or PRK.
* `TryResolveAsync(V,D)` resolves EXACTLY V and D.
* no fallback to CurrentVersion;
* no fallback to another configured version;
* no fallback to Client API-key pepper.
* nullable/unavailable result maps to dependency failure, not digest mismatch.

Lease owns one mutable underlying key buffer.

Dispose MUST zero it.

============================================================
9. API-HOST VERSIONED CONFIG CONTRACT
=====================================

Create exactly:

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperOptions.cs

Canonical section:

TagEkyc:CaptureRuntimeVerifierPeppers

Closed fields:

CurrentVersion: positive Int32

Versions:
finite collection

Each Versions entry contains exactly:

Version: positive Int32
SecretRef: nonblank string

No plaintext-secret field is authorized.

Forbidden configuration properties include:

Pepper
Secret
SecretValue
Value
RawSecret
InlineValue
ApiKeyPepper

Validation:

1. CurrentVersion > 0.
2. At least one version exists.
3. every Version > 0.
4. Version values unique.
5. exactly one entry matches CurrentVersion.
6. every SecretRef nonblank.
7. SecretRef uses exactly `env:<NAME>` or `file:<ABSOLUTE_PATH>` and is accepted
   by `CaptureRuntimeVerifierPepperSecretRefResolver` under the bound A2
   amendment.
8. resolution/material validity ultimately obeys §2/§4.
9. malformed structure fails startup.
10. missing/unavailable material fails readiness closed.

Do NOT modify CaptureRuntimeDatabaseOptions.

Its connection-only contract remains unchanged.

Production SecretRef VALUES remain A4 authority.

No production secret value is placed into A1 docs/config.

============================================================
10. INFRASTRUCTURE PROVIDER
===========================

Create exactly:

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperProvider.cs

It implements:

ICaptureRuntimeVerifierPepperSource.

Dependencies:

CaptureRuntimeVerifierPepperOptions
+
public `CaptureRuntimeVerifierPepperSecretRefResolver` in this provider file
+
CaptureRuntimeVerifierCryptography

Exact cross-assembly helper contract is the A2 contract:
public static `Resolve(string secretRef)` returns a
`CaptureRuntimeVerifierPepperSecretRefResolution` with closed status
`Success|ReferenceInvalid|MaterialUnavailable|MaterialInvalid`. Success alone
carries a disposable `CaptureRuntimeVerifierPepperMaterialLease` whose
`CanonicalAscii` is the exact owned 43-byte `ReadOnlyMemory<byte>`; failure
carries no material. Disposal zeroes the mutable backing array. The helper
returns no secret string, raw file exception, path or environment-variable
detail. R01 and API provider use this same method, status mapping and lease.

It MUST NOT depend on:

ApiKeyStoreOptions
ApiKeyStorePepperResolver
Client API-key pepper material
TagEkyc.Api.

Resolution for `(V,D)`:

1. locate exactly Version=V.
2. resolve that entry's SecretRef through the exact-preserving public helper;
   never through `SecretRefResolver.Resolve(...).Value`.
3. pass the lease's exact 43 ASCII bytes through §2 canonical parsing and
   dispose the lease on every path.
4. obtain K_master[V].
5. derive PRK[V].
6. derive only K_domain[V,D].
7. zero A1-owned K_master and PRK buffers.
8. return a lease owning only the requested 32-byte K_domain buffer.
9. lease Dispose zeroes it.

Permitted long-lived cache:

* immutable version -> SecretRef metadata;
* immutable CurrentVersion.

Forbidden long-lived cache:

* resolved pepper text;
* K_master;
* PRK;
* K_domain.

Unknown/missing/invalid pepper version is a dependency failure.

It is NOT credential mismatch.

============================================================
11. CURRENT VERSION AND ISSUANCE OWNERSHIP
==========================================

There are exactly TWO execution adapters for one semantic concept.

---

## 11.1 API-HOSTED CURRENTVERSION

For one API process:

CurrentVersion =
CaptureRuntimeVerifierPepperOptions.CurrentVersion.

It is immutable for the entire process lifetime.

API-hosted new verifier rows MUST stamp exactly that version.

Includes:

R03 bootstrap issuance.

R20a capability Issue.

R20b legal successor capability creation.

No external HTTP/API caller may:

* supply;
* choose;
* override

VerifierPepperVersion.

Where SQL takes `p_verifier_pepper_version`, server-owned Application code
provides CurrentVersion.

---

## 11.2 OFFLINE R01

R01 executes through:

tools/TagEkyc.ApiKeyProvisioner/Program.cs

The tool does NOT use the API host appsettings/DI model for this authority.

Add exact canonical R01 arguments:

--capture-runtime-verifier-pepper-version <positive Int32>

--capture-runtime-verifier-pepper-secret-ref <SecretRef>

For this offline deployment invocation:

CurrentVersion =
the exact positive CLI version supplied by the authorized deployment operator.

The tool:

1. parses exact positive version.
2. resolves the exact Capture Runtime SecretRef through the same public
   `CaptureRuntimeVerifierPepperSecretRefResolver` used by the API provider.
3. validates canonical pepper material through
   CaptureRuntimeVerifierCryptography.
4. derives Platform domain key.
5. generates/validates exact canonical platform secret.
6. computes §6 platform digest.
7. supplies the SAME version to SQL.
8. persists that exact VerifierPepperVersion.
9. zeroes A1-owned secret/key buffers.

R01 MUST NOT use:

--pepper
--pepper-value
plaintext pepper arguments
ApiKeyStorePepperResolver
TagEkyc:ApiKeyStore:PepperSecretRef

for Capture Runtime platform credentials.

The deployment operator invoking the offline CLI is server/deployment authority,
not an external HTTP caller.

---

## 11.3 DEPLOYMENT CONSISTENCY

A4/deployment is responsible for using the same intended active version and
SecretRef in:

* API-host Capture Runtime verifier-pepper configuration; and
* offline R01 invocation.

A1 does NOT implement online synchronization between the two processes.

Mismatch is deployment/config error.

Neither side may silently substitute another version.

============================================================
11.4 HISTORICAL ROW VERIFICATION
================================

Existing durable rows are verified ONLY with their own persisted
VerifierPepperVersion.

Applies to:

platform_operator_credentials
capture_runtime_bootstrap_issuances
capture_capabilities

Forbidden:

try CurrentVersion first;
try every configured version;
fall back to old/new pepper;
fall back to API-key pepper.

Persisted V selects exactly K_master[V] and then the required domain D.

============================================================
12. READINESS, ROTATION AND RETIREMENT
======================================

The authoritative SQL readiness contract remains source of:

required_pepper_versions.

API-host required set is exactly:

{ configured CurrentVersion }
UNION
{ every distinct SQL required_pepper_versions value }

Every member MUST:

* have exactly one configured version entry;
* resolve SecretRef;
* pass §2 canonical material validation.

Otherwise:

NOT_READY.

---

## 12.1 IMMUTABLE PROCESS CURRENTVERSION

CurrentVersion MUST NOT hot-reload/change within one process lifetime.

A deployment change:

N -> N+1

requires process restart/redeployment.

New process freezes N+1 before listener/readiness acceptance.

There is no in-process CurrentVersion writer or issuance race.

---

## 12.2 SUCCESSOR ROTATION MODEL

Pepper rotation N -> N+1:

1. deployment config contains both resolvable N and N+1 while N remains
   referenced;

2. deployment changes CurrentVersion to N+1;

3. process restarts;

4. startup validates N+1 and every SQL-required historical version;

5. only then readiness may become green.

Afterward:

* new R03/R20 rows use N+1;
* existing V=N rows still verify only with N;
* no durable digest is rewritten solely due to rotation.

---

## 12.3 RETIREMENT

Version N may be removed from deployment configuration only when BOTH:

N != CurrentVersion

AND

N is absent from authoritative SQL required_pepper_versions.

A1 does not delete secret material.

A4/deployment removes the external secret/config entry.

If N is removed prematurely:

startup/readiness MUST become NOT_READY.

No fallback version may restore green.

The invariant:

retirement denied while referenced

is therefore enforced through:

configuration presence
+
SQL reference scan
+
fail-closed startup/readiness.

============================================================
12.4 SUPERSEDED OLD LIFECYCLE MODEL
===================================

For A1, explicitly SUPERSEDE:

`capture-runtime-verifier-key provision`

`capture-runtime-verifier-key rotate`

`capture-runtime-verifier-key retire`

`capture-runtime-verifier-key readiness`

and:

`deployment current-version CAS`

and any Planning/umbrella race row requiring those exact product-owned pepper
lifecycle callables.

They are historical/non-operative for successor A1 authority.

DO NOT IMPLEMENT THEM.

There is:

* no A1 pepper lifecycle DB table;
* no A1 pepper lifecycle HTTP route;
* no new lifecycle CLI family;
* no in-process pepper CAS;
* no runtime hot pepper rotation.

This supersession changes ONLY verifier-pepper lifecycle ownership.

============================================================
13. DI, FILE AUTHORITY AND PROCESS BOUNDARIES
=============================================

---

## 13.1 EXACT NEW PRODUCT FILES AUTHORIZED AFTER RRI PASS

Create exactly:

src/TagEkyc.Application/CaptureRuntime/
CaptureRuntimeVerifierCryptography.cs

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperOptions.cs

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperProvider.cs

No other new product file is authorized by this RRI.

---

## 13.2 EXISTING A1 FILES THAT MAY BE MODIFIED AS DIRECT CONSEQUENCE

src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs

src/TagEkyc.Application/CaptureRuntime/
CaptureRuntimeEnrollmentApplicationService.cs

src/TagEkyc.Application/CaptureRuntime/
CaptureRuntimeManagementApplicationService.cs

src/TagEkyc.Application/CaptureRuntime/
CaptureRuntimeExecutionApplicationService.cs

src/TagEkyc.Infrastructure/Auth/
PlatformOperatorCredentialAuthenticator.cs

src/TagEkyc.Infrastructure/Persistence/
TagEkycPersistenceServiceCollectionExtensions.cs

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimePersistenceBoundary.cs

tools/TagEkyc.ApiKeyProvisioner/Program.cs

tests/TagEkyc.UnitTests/
Tip88C1C6BA1ContractTests.cs

tests/TagEkyc.IntegrationTests/
Tip88C1C6BA1PersistenceTests.cs

tests/TagEkyc.ArchTests/
Tip88C1C6BA1ArchitectureTests.cs

Only changes directly required by this RRI are authorized.

Existing baseline A1 authority remains otherwise unchanged.

---

## 13.3 READ-ONLY EVIDENCE REMAINS READ-ONLY

DO NOT add mutation authority to:

src/TagEkyc.Infrastructure/Persistence/
RawExportControlPlaneReadinessValidator.cs

It remains read-only evidence.

---

## 13.4 FORBIDDEN AUTHORITY EXPANSION

This RRI does NOT authorize mutation of:

.csproj
.sln
config/appsettings*.json
deployment/
IaC/
generic SecretRefResolver implementation
ApiKeyStore pepper implementation
A2 files
A3 files

No new project reference.

No Infrastructure -> Api dependency.

---

## 13.5 DI

Modify existing:

TagEkycPersistenceServiceCollectionExtensions.cs

only as required to:

* bind CaptureRuntimeVerifierPepperOptions;
* validate closed structure;
* validate on startup;
* register CaptureRuntimeVerifierPepperProvider as
  ICaptureRuntimeVerifierPepperSource.

Provider may be singleton only if no plaintext pepper/derived key is retained
between Resolve calls.

R01 CLI does NOT instantiate API DI/appsettings solely for this feature.

============================================================
14. REQUIRED EXECUTABLE PROOFS
==============================

Add/extend exact proofs below.

---

## 14.1 PURE CODEC/KDF GOLDEN VECTOR

Exact method:

VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors

Exact file:

tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs

The proof MUST use fixed externally materialized synthetic expected values.

Canonical synthetic master:

K_master bytes:

00 01 02 03 04 05 06 07
08 09 0A 0B 0C 0D 0E 0F
10 11 12 13 14 15 16 17
18 19 1A 1B 1C 1D 1E 1F

Canonical base64url:

AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8

Expected PRK hex:

46bd320605c5a6b6163ab70bc6345b92a5f908e79fe58979c23ebb47d1a5e307

Expected Platform domain key hex:

4db8994a75704c012def93ba380e2e6e63191f4901bedac22e8ef9876d6f00bb

Expected Bootstrap domain key hex:

f091805ec2529b08b4661530f80befd7462824ca19d901324188de655f1a58d3

Expected Capability domain key hex:

eea3404eea1c83d037a2e459bed78d13ba21862f97220f86bb229ed2ed1bc95f

Synthetic platform secret bytes:

20..3F

Canonical payload:

ICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8

Expected platform digest hex:

c5a2f56f1325e67b4deb97fa5d5d5ed21a238d0e3c122a5fc30f16f91a72b473

Synthetic bootstrap secret bytes:

40..5F

Canonical:

QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl8

Expected bootstrap digest hex:

ac1d78c9278e787d899465772567e26ee48967fad8b4c05350937a1c0805dec9

Synthetic capability secret bytes:

60..7F

Canonical:

YGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn8

Expected capability digest hex:

e249239c0a2e4532c78c7d006d576f9819293264107dc91c4f1a8b04493315a3

The test MUST NOT derive both expected and actual values using only the same
implementation under test.

---

## 14.2 TAIL-BIT ALIASES

For the canonical master ending:

`...Hh8`

the following same-byte aliases MUST be RED:

`...Hh9`
`...Hh-`
`...Hh_`

Proof MUST demonstrate that a permissive decoder can yield the same 32 bytes,
then prove the A1 canonical codec rejects all three via re-encode mismatch.

Equivalent alias proof MUST exist for the presented:

* platform payload;
* BootstrapSecret;
* CaptureCapabilitySecret.

For canonical values ending `8`, aliases ending:

`9`
`-`
`_`

MUST be rejected.

---

## 14.3 PROVIDER / CURRENT / HISTORICAL / RETIREMENT PROOF

Exact method:

VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed

Exact file:

tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs

Must prove:

1. CurrentVersion resolves.
2. R03 new row stamps CurrentVersion.
3. R20 new row stamps CurrentVersion.
4. persisted V=N verifies using N after CurrentVersion=N+1.
5. missing N never falls back.
6. missing N maps dependency failure/NOT_READY.
7. missing CurrentVersion makes readiness RED.
8. SQL required `{1,2}` requires both.
9. deleting configured 1 while SQL requires 1 keeps RED.
10. deleting 1 after it is neither Current nor SQL-required does not cause RED
    solely because 1 is absent.
11. duplicate Version config rejected.
12. CurrentVersion without matching entry rejected.
13. blank/malformed SecretRef rejected.
14. unresolved SecretRef fails closed.
15. noncanonical resolved pepper including CR/LF fails.
16. valid Client API-key pepper cannot act as fallback.
17. no plaintext pepper/full SecretRef leaks.

---

## 14.4 R01 OFFLINE PROOF

Exact method:

R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef

Exact file:

tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs

Preferred proof drives the actual offline provisioning process using:

--capture-runtime-verifier-pepper-version N

--capture-runtime-verifier-pepper-secret-ref <test SecretRef>

Assert:

platform_operator_credentials.VerifierPepperVersion = N

and stored digest matches exactly the Platform domain key derived from that
SecretRef.

RED:

* missing version;
* noninteger version;
* zero version;
* negative version;
* missing SecretRef;
* blank SecretRef;
* invalid SecretRef;
* unresolved SecretRef;
* noncanonical pepper;
* API-key pepper configured but Capture Runtime pepper absent;
* stored version differs from digest version.

No credential row on failed provisioning.

If executable-process invocation is impossible without build-topology mutation,
the test MAY drive the exact extracted R01 command boundary only if a separate
test proves Program.cs maps both canonical CLI arguments exactly to that
boundary.

Direct SQL-only proof is insufficient.

---

## 14.5 PRESENTED SECRET ALIAS PROOF

Exact method:

PresentedVerifierSecrets_RejectNonCanonicalTailAliases

Exact file:

tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs

Must prove through owning verification paths:

* canonical platform key succeeds in valid fixture;

* its 3 same-byte aliases fail;

* canonical bootstrap secret succeeds in valid fixture;

* its 3 same-byte aliases fail;

* canonical capability secret succeeds in valid fixture;

* its 3 same-byte aliases fail.

Alias attempts MUST NOT create a successful alternative operation/replay path.

---

## 14.6 ARCHITECTURE PROOF

Exact method:

CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency

Exact file:

tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs

RED if:

* Capture Runtime provider uses ApiKeyStore pepper;
* Infrastructure depends on Api;
* new project reference appears;
* plaintext pepper option exists;
* generic SecretRefResolver is mutated by this RRI;
* RawExportControlPlaneReadinessValidator becomes writable;
* more than one competing A1 codec/KDF implementation is introduced;
* R01 is forced through API host DI/appsettings.

---

## 14.7 EXISTING PROOFS REMAIN REQUIRED

Reconcile, do not remove:

SecretOnce_DigestsAndPepperRetirementAreClosed

Readiness_ValidatesSchemaAclPepperNonceAndConnections

DispatchCatalogue_IsClosedAndBidirectionallyJoined

============================================================
15. AUTHORITY RECONCILIATION
============================

Materialize this RRI as exactly one repository document:

docs/tips/tip_88c1_secure_raw_source_sealed_assembly/
tip_88c1_c6b_a1_versioned_verifier_pepper_source_rri.md

Successor Parent MUST bind its exact SHA.

Successor OP MUST reference the bound RRI.

Mechanically reconcile Parent/OP/artifact/proof/secret/readiness views.

---

## 15.1 OLD PEPPER LIFECYCLE OCCURRENCES

Search exact Planning/umbrella/operative A1 authorities for:

capture-runtime-verifier-key provision
capture-runtime-verifier-key rotate
capture-runtime-verifier-key retire
capture-runtime-verifier-key readiness
deployment current-version CAS

These historical occurrences MAY remain in immutable Planning/umbrella bytes
only because the successor Parent binds THIS RRI and explicitly marks them:

SUPERSEDED / NON-OPERATIVE FOR A1.

There MUST be zero competing operative lifecycle interpretation.

---

## 15.2 VARIABLE-LENGTH SECRET OCCURRENCES

Search for:

at-least-256-bit
at least 256 bits
at least 256-bit

and equivalent variable-length wording attached to:

BootstrapSecret
CaptureCapabilitySecret
bootstrap secret
capture capability secret

Every such historical statement MUST be explicitly classified by successor
authority as superseded/non-operative for A1.

Sole operative size rules:

BootstrapSecret = exactly 32 bytes.

CaptureCapabilitySecret = exactly 32 bytes.

Operative variable-length occurrence count after reconciliation:

0.

---

## 15.3 UNCHANGED PLANNING AUTHORITY

Do NOT treat this RRI as generic supersession of Planning v1.8.

Every unrelated Planning decision remains authoritative.

============================================================
16. MECHANICAL RED CONDITIONS
=============================

Bounded review MUST HOLD if ANY is true:

1. No exact Application verifier-pepper port exists.

2. No shared exact codec/KDF authority exists.

3. API-host and R01 may independently invent different codec/KDF behavior.

4. Provider is Api-owned.

5. Infrastructure depends on Api.

6. `.csproj` or `.sln` mutation is required.

7. Client API-key pepper is reused/aliased/fallback.

8. resolved pepper remains free-form text.

9. pepper may decode to anything other than exactly 32 bytes.

10. any platform/bootstrap/capability 32-byte secret accepts more than one
    textual representation.

11. decode-length-only canonicality is accepted.

12. canonical re-encode + ordinal compare is absent.

13. tail-bit aliases are accepted.

14. Trim/TrimEnd/newline removal can make an invalid secret valid.

15. generic SecretRefResolver normalization is silently relied upon when it
    destroys canonicality evidence.

16. HKDF salt unspecified.

17. HKDF info unspecified.

18. HKDF output length unspecified.

19. raw K_master may be final digest HMAC key.

20. golden expected outputs are generated only by code under test.

21. API external caller may supply VerifierPepperVersion.

22. API new R03/R20 issuance does not use frozen CurrentVersion.

23. R01 receives version without Capture Runtime SecretRef.

24. R01 receives SecretRef without positive version.

25. R01 uses API-key pepper.

26. R01 requires API host DI/appsettings.

27. R01 writes a different version from the one whose key produced the digest.

28. historical row may fall back to CurrentVersion/another version.

29. missing required pepper maps to credential mismatch instead of dependency
    failure.

30. readiness may be green while CurrentVersion unresolved.

31. readiness may be green while SQL-required version unresolved.

32. referenced old version may be removed while SQL still requires it.

33. old product lifecycle commands remain simultaneously operative.

34. deployment CurrentVersion CAS remains an A1 requirement.

35. CurrentVersion may mutate during process lifetime.

36. A1 invents pepper lifecycle DB/HTTP/tool state.

37. operative Planning/umbrella text still allows BootstrapSecret larger than
    32 bytes.

38. operative Planning/umbrella text still allows CaptureCapabilitySecret
    larger than 32 bytes.

39. exact-size narrowing is misrepresented as lowering the previous 256-bit
    floor.

40. RawExportControlPlaneReadinessValidator gains mutation authority.

41. production secret VALUE is introduced.

42. A2 or A3 scope is opened.

43. any unrelated A1 semantic decision changes.

============================================================
17. BOUNDED REVIEW, REBIND AND TERMINAL OUTPUT
==============================================

While reviewing this RRI:

DO NOT mutate product files.

Review only:

A. exact canonical codec;
B. tail-bit enforcement;
C. exact 32-byte secret narrowing;
D. verifier-pepper material;
E. exact HKDF;
F. exact three digest formulas;
G. shared crypto authority layering;
H. Application port;
I. API-host options/provider/DI;
J. offline R01 adapter;
K. CurrentVersion;
L. historical-version resolution;
M. readiness/retirement;
N. lifecycle supersession;
O. Planning variable-length supersession;
P. direct artifact/proof consequences.

Do NOT reopen unrelated A1 architecture.

Required correction work:

1. materialize this exact RRI document;
2. update Parent to bind it;
3. update OP and affected catalogue views;
4. rebind every changed SHA;
5. perform bounded independent review;
6. verify staged=0;
7. verify conflicted=0;
8. perform NO product mutation during the RRI round.

Terminal response MUST return:

* successor Parent SHA + line count;
* successor OP SHA + line count;
* exact RRI SHA + line count;
* changed companion SHA list;
* exact new product-file authority list;
* confirmation no `.csproj`/project-graph change;
* canonical codec result;
* tail-bit alias result;
* exact golden-vector values/proof identity;
* exact final BootstrapSecret size;
* exact final CaptureCapabilitySecret size;
* operative variable-length Planning occurrence count;
* old pepper-lifecycle operative occurrence count;
* API CurrentVersion rule;
* R01 version/SecretRef rule;
* historical version rule;
* readiness/retirement rule;
* no ApiKey pepper reuse result;
* staged/conflicted counts;
* complete bounded findings.

Terminal verdict MUST be exactly:

PASS — 0 ACTIONABLE FINDINGS

or

HOLD — <exact actionable findings>

A bounded PASS restores the previously granted A1 implementation authority on
the successor exact-byte Parent.

A bounded PASS DOES NOT grant:

* stage;
* commit;
* push;
* A2;
* A3;
* production/A4 secret values;
* any scope outside A1.
