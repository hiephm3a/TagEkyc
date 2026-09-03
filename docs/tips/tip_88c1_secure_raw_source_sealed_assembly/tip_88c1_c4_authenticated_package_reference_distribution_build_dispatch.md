# TIP-88C1-C4 — Authenticated Package Reference Distribution Build Dispatch

Version: `0.2`
Status: `ROUND_3_EXACT_BYTE_CLOSURE_CANDIDATE`
Date: `2026-08-19`
Baseline commit: `8d76689a746960bd24ca944eaaf4858f39026511`
Ratified Scope v0.2 SHA-256: `F59B9DB19585AD4C30D8AD0EA98DEEF8069A5AAB28E505D58203684AA9CEBA0E`
Round-2 authority SHA-256: `B7A4C6AC4C9A48EAAC0547AD0457093CB581C0FD4AD134983765DAB5FF93C250`
Authoring mode: `DOCS_ONLY`
Implementation authority: `NONE`

## 0. Changelog

### v0.2 — Round-3 narrow executable closure

- Inserts an exact dependency-free request classifier and topology gate before
  cursor-key or database work; Disabled/Invalid returns unavailable with zero
  C4 dependency access.
- Pins the complete indexed `AcceptedCursorKeys` configuration tree, effective-
  configuration override semantics, lowercase key identity and rotation shape.
- Carries the concrete authenticator/validator/store PrincipalId chain and adds
  the one missing LocalDev C4 credential path, changing the allowlist 34 -> 35.
- Classifies duplicate continuation `pageSize` as cursor-invalid and makes the
  first-page/continuation split explicit in precedence and mutations.
- Confirms the real C3 proof is constructible from the C4 integration path
  without changing the landed C3 test file, and restores two omitted inherited
  debt sentinels.

### v0.1 — Round-2 executable candidate

- Converts the ratified C4 scope into one buildable HTTP, cursor, database,
  readiness and proof contract.
- Pins one read-only SQL function, one partial supporting index, zero new
  tables/columns/states/events, and the fifth database capability/LOGIN pair.
- Pins the HTTP distinction between an omitted cursor and `?cursor=` before
  model binding or cursor decoding.
- Pins a versioned binary cursor, HMAC-SHA-256 construction, key rotation rules
  and a byte-exact absolute vector.
- Carries the application-owned PackageId/principal-bound cursor distinction
  through DTOs, SQL, precedence and mutations.
- Budgets disposable-database, LF-normalization, pre-Up LOGIN and all three
  raw-worktree ModelSnapshot tripwires in the initial allowlist.
- Defines exactly 24 proof owners (`C401`–`C424`) and 34 discriminating
  mutations (`C4M01`–`C4M34`).

## 1. Authority, precedence and non-authorization

Precedence is:

1. current explicit Homeowner instruction;
2. ratified C4 Scope v0.2 at
   `F59B9DB19585AD4C30D8AD0EA98DEEF8069A5AAB28E505D58203684AA9CEBA0E`;
3. landed C1, C2 and C3 contracts at baseline
   `8d76689a746960bd24ca944eaaf4858f39026511`;
4. this dispatch only after independent Round-2 review and Homeowner controlled-
   build ratification.

This candidate grants no implementation, migration execution, provider
operation, test execution, stage, commit, push, merge, PR, deployment,
production activation, real Raw BIO, PackageId discovery beyond the bounded
route, recipient management, package download or delivery authority.

Round-1 decisions are closed. C4 is one authenticated list of opaque,
application-owned PackageIds. It is not download authority, an inbox,
notification, delegated delivery, per-principal ownership or durable listing
audit.

`C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` remains
`ASSIGNED_TO_TIP_88C1_C4 / ASSIGNED_OPEN` until implementation closeout.

## 2. Task-0 gates and exact landed facts

### 2.1 Repository and review-chain gate

Before any future build:

```text
HEAD   = 8d76689a746960bd24ca944eaaf4858f39026511
branch = tip-88a-raw-export-policy-catalog-build
staged paths = 0
```

Required reviewed anchors:

| Source | SHA-256 |
| --- | --- |
| C4 Scope v0.2 | `F59B9DB19585AD4C30D8AD0EA98DEEF8069A5AAB28E505D58203684AA9CEBA0E` |
| C4 ledger before this append | `6EF7BF988D564339AC02507E2A47E10F04B34FD210662E89082FD7AD75DB9921` |
| Round-1 closure bundle | `D6E0E21AC59670FBB105DE6A32704D53CC111F0B5B21B00CD2B305C6833C5420` |
| Round-2 Homeowner authority | `B7A4C6AC4C9A48EAAC0547AD0457093CB581C0FD4AD134983765DAB5FF93C250` |

If an anchor cannot be reproduced, report `REVIEW_CHAIN_INCOMPLETE`; do not
infer authority or a predecessor verdict.

### 2.2 Landed ownership and ordering facts

The landed C2 package row SHA is
`AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E`.
It persists non-empty `PackageId`, `RecipientClientApplicationId`, `State` and
nullable `FinalizedAtUtc`. The C2 sparse CHECK admits non-null
`FinalizedAtUtc` exactly for `State='Finalized'`, and the alternate key
`uq_raw_export_recipient_package_package` makes PackageId unique. Therefore
`(FinalizedAtUtc, PackageId)` is a total order over the eligible set.

The authenticated context SHA is
`4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401`.
The PostgreSQL API-key store SHA is
`96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91`.
The concrete API chain is independently pinned as:

```text
IApiKeyAuthenticator.cs        72A7E14658FFC4F3990D87CA590CD2A19084FC012C3D0813A2351A12750F9CF4
LocalDevApiKeyAuthenticator.cs 1BBB32988AB92BFB32C9027E49EE5E4C4EA7B6382395DA073D756574BB630287
LocalDevApiKeyValidator.cs     C9E03F9383CA2991F589ECBA178E2E877231D96DBE7D5D58DE23BC56AC26E9F8
LocalDevApiKeyStore.cs         A40CCD01D692BCEA871DA602B424526EBB63B2A9B63282B838D4A90921E4B3EF
LocalDevRuntimePolicySource.cs 8C19D7529343B8A27D6DAB73A0E7A0E19C27E97E33019CBB27B4D844B7608475
ApiKeyProvisioningService.cs   540A810A10A9099BCD333243BC056228B5AE6AFA95773D87C07FBDFD3552A7D1
PostgresHashedApiKeyStore.cs   96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91
AuthenticatedClientContext.cs  4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401
```

Both stores populate `ResolvedApiKey.PrincipalId`; the validator copies it
unchanged into `AuthenticatedClientContext.PrincipalId`, and the authenticator
returns that context. C401 must drive the concrete chain and reject a persisted
or LocalDev empty PrincipalId rather than relying on the record default.

The future build adds one isolated LocalDev reference credential only in
`LocalDevApiKeyStore.cs`:

```text
ApiKeyId             20000000-0000-0000-0000-000000000011
ClientApplicationId  LocalDevRuntimePolicySource.BusinessClientId
PrincipalId          LocalDevRuntimePolicySource.BusinessClientId
Presented key        localdev-recipient-package-reference-key
Prefix               ldev_reference
Scopes               exactly business.raw-export.package.references.read
Category             BusinessConsumer
Status               Active
```

It does not add the C4 scope to another LocalDev key. Managed recipient/API-key
enrollment remains outside C4 and production activation; no change to
`LocalDevRuntimePolicySource.cs` or `ApiKeyProvisioningService.cs` is required
by this bounded build. No C4 request/query field may supply or replace either
authenticated identity.

### 2.3 HTTP-boundary feasibility gate

No landed endpoint currently distinguishes an omitted optional query key from
an empty value. C4 must read `HttpContext.Request.Query.ContainsKey("cursor")`
and the exact `StringValues` cardinality before constructing an application
request. It must not rely on nullable model binding.

These two requests are distinct:

```text
GET /api/ekyc/raw-export/package-references?pageSize=25
-> 200 first page

GET /api/ekyc/raw-export/package-references?pageSize=25&cursor=
-> 400 RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_INVALID
```

### 2.4 Harness pre-control

Before migration generation or mutation:

- provision and verify `tagekyc_raw_export_package_reference_login` at cluster
  scope before migration Up;
- run apply/Down/reapply only in a disposable database created from the current
  canonical database and drop only that exact database in `finally`;
- never Down or mutate the shared canonical/template database;
- record raw-worktree ModelSnapshot SHA and move all three absolute tripwires
  together after the additive-model proof;
- compare catalog/model SQL after applying `.ReplaceLineEndings("\n")` to both
  sides only; visible whitespace, tokens, predicates and identifiers remain
  byte-identical after common line-ending normalization;
- record anonymous Docker volume count before/after affected tests and require
  zero growth.

`SHARED-DB-MIGRATION-STATE-DEBT-01` and
`LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` remain open harness debts, not C4
capability claims.

## 3. Fixed topology, configuration and census

### 3.1 Topology

Configuration section:

```text
TagEkyc:RawExport:PackageReference
```

Exact topology values:

```text
Disabled
PostgresDurable
Invalid (internal fail-closed classification)
```

Absent or exact `Disabled` with no sibling members is valid Disabled. Disabled
keeps the route mapped but returns the public unavailable result and readiness
is not-applicable/green. Unknown topology, sibling configuration under Disabled,
or incomplete PostgresDurable configuration is Invalid and readiness RED.

Topology resolution is a dependency-free parse of the effective configuration.
After raw query classification and dependency-free cursor syntax checks, both
Disabled and Invalid return `RAW_EXPORT_PACKAGE_REFERENCE_UNAVAILABLE / 503`
with zero protected-value resolution, C4 connection opening, SQL query or cursor
minting. Only valid PostgresDurable may enter key-material and database work.

PostgresDurable requires:

```text
DatabaseConnectionString          non-empty
ActiveCursorKeyId                 grammar [a-z0-9][a-z0-9._-]{0,63}
ActiveCursorKeyVersion            integer 1..2147483647
AcceptedCursorKeys                1..4 distinct (KeyId, KeyVersion) pairs
CursorKeys:<KeyId>:<Version>       protected configuration value
```

The exact effective `IConfiguration` representation is:

```text
TagEkyc:RawExport:PackageReference:Topology
TagEkyc:RawExport:PackageReference:DatabaseConnectionString
TagEkyc:RawExport:PackageReference:ActiveCursorKeyId
TagEkyc:RawExport:PackageReference:ActiveCursorKeyVersion
TagEkyc:RawExport:PackageReference:AcceptedCursorKeys:0:KeyId
TagEkyc:RawExport:PackageReference:AcceptedCursorKeys:0:KeyVersion
... contiguous indices 1..3 only when present ...
TagEkyc:RawExport:PackageReference:CursorKeys:<KeyId>:<Version>
```

Environment-variable equivalents replace every colon with `__`, for example:

```text
TagEkyc__RawExport__PackageReference__AcceptedCursorKeys__0__KeyId
TagEkyc__RawExport__PackageReference__AcceptedCursorKeys__0__KeyVersion
TagEkyc__RawExport__PackageReference__CursorKeys__c4-cursor-01__1
```

Indices start at zero, are contiguous and contain exactly `KeyId` and
`KeyVersion`. Collection order has no semantic meaning. Pair identity and all
comparisons are ordinal; a duplicate pair is Invalid, while the same KeyId at
different versions is valid. KeyId is canonical lowercase ASCII so .NET's
case-insensitive configuration path lookup cannot alias two logical ids.

The active pair must occur exactly once in `AcceptedCursorKeys`. The effective
post-provider-precedence tree is authoritative: a later provider overriding the
same exact path replaces it and is not a second element. Unknown/empty children,
non-contiguous/non-numeric indices, duplicate pairs, malformed scalars, more
than four pairs, a material leaf without an accepted pair, or an accepted pair
without exactly one material leaf make topology Invalid. No comma-delimited or
map shorthand is accepted. `ToString`, exceptions, DTOs, logs and readiness
redact all connection/key material.

Each configured key value is canonical unpadded base64url text of exactly 32
bytes. The C4 protected-value catalog maps only an accepted logical pair to:

```text
Purpose  = raw-export.package-reference-cursor.hmac
ValueId  = <KeyId>:<KeyVersion>
Reference= config:TagEkyc:RawExport:PackageReference:CursorKeys:<KeyId>:<Version>
```

The active pair signs new cursors. Any accepted pair verifies its own cursor.
During rotation, add the new pair, retain the old pair/material, then change
active; remove the old pair only after every cursor it signed has passed the
15-minute expiry. An unaccepted pair is cursor-invalid. An accepted pair whose
material cannot be resolved/decoded is dependency-unavailable. There is no
unsigned fallback.

### 3.2 Exact census

Every production and proof census keeps four families separate:

```text
S3 provider credentials:  5 -> 5 unchanged
DB capabilities:          4 -> 5
SQL capability roles:     4 -> 5
SQL LOGIN roles:          4 -> 5
```

Exact database role families after C4:

```text
tagekyc_raw_export_package_preparer
tagekyc_raw_export_package_reconciler
tagekyc_raw_export_package_lifecycle
tagekyc_raw_export_package_delivery
tagekyc_raw_export_package_reference
```

Exact LOGIN family is the same five names with `_login`. Exact membership is
one LOGIN-to-like-named-capability edge per family, with `admin_option=false`,
`inherit_option=true`, `set_option=false`. C4 must not alter the first four.

The C4 capability and LOGIN are `INHERIT`, non-superuser, non-createdb,
non-createrole, non-replication, non-bypassrls; capability is NOLOGIN and LOGIN
has no password under the test prerequisite. C4 adds no S3 credential or client.

## 4. Public route, DTO and error contract

### 4.1 Exact route and query shape

Exactly one route is added:

```http
GET /api/ekyc/raw-export/package-references?pageSize={n}&cursor={opaque}
Authorization: existing API-key authentication
Required scope: business.raw-export.package.references.read
Caller category: BusinessConsumer
```

Only ordinal, case-sensitive `pageSize` and `cursor` query-key spellings are
accepted; the endpoint enumerates the raw key names before using
`ContainsKey("cursor")`. No body, route value,
recipient/client/principal/package/state/sort/filter field is accepted.

First page:

```text
cursor key absent
pageSize absent -> 25
pageSize present exactly once -> ASCII decimal grammar
                                 `^(?:[1-9]|[1-4][0-9]|50)$`
```

Continuation:

```text
cursor key present exactly once and non-empty
pageSize absent -> authenticated cursor page size
pageSize present exactly once -> the same ASCII grammar and equal cursor page size
```

Unknown query key or duplicate first-page pageSize is request-invalid.
Present-empty/duplicate cursor, or any supplied continuation pageSize whose
cardinality is not exactly one or whose value is empty, non-canonical,
out of range or unequal to the authenticated value, is cursor-invalid. Thus
`?cursor=X&pageSize=25&pageSize=25` is cursor-invalid even though both supplied
scalars are equal.

### 4.2 DTO shapes

Public contracts are exactly:

```csharp
public sealed record RecipientPackageReferenceDto(
    Guid PackageId,
    DateTimeOffset FinalizedAtUtc);

public sealed record RecipientPackageReferencePageDto(
    IReadOnlyList<RecipientPackageReferenceDto> Items,
    string? NextCursor);
```

`Items` is non-null and ordered; empty is `[]`. `NextCursor` is null unless an
additional own eligible row exists. PackageId is a lowercase D-format JSON UUID
under the existing serializer behavior; `FinalizedAtUtc` is UTC. No total,
foreign count, provider locator, object/key metadata, package digest, subject,
delivery state, principal or client id is returned.

### 4.3 Public errors

```text
RAW_EXPORT_PACKAGE_REFERENCE_FORBIDDEN
  "Package reference access is forbidden." / 403

RAW_EXPORT_PACKAGE_REFERENCE_REQUEST_INVALID
  "Package reference request is invalid." / 400

RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_INVALID
  "Package reference cursor is invalid." / 400

RAW_EXPORT_PACKAGE_REFERENCE_UNAVAILABLE
  "Package reference listing is temporarily unavailable." / 503
```

Errors use the landed `{error:{code,message,correlationId}}` envelope. No error
contains the cursor, key id/version, owner, boundary, PackageId, row count,
connection string or provider metadata.

### 4.4 Route-specific precedence

1. existing API-key authentication;
2. exact BusinessConsumer category, required scope and non-empty authenticated
   client/principal;
3. exact-case query-key allowlist; an unknown key is request-invalid;
4. classify by raw cursor key presence: absence selects first page; exactly one
   non-empty value selects continuation; present-empty or duplicate cursor is
   cursor-invalid;
5. classify pageSize within that branch: first-page absence defaults to 25 and
   any invalid/cardinality failure is request-invalid; continuation absence is
   deferred to its authenticated cursor value, while empty, duplicate or
   non-canonical/out-of-range supplied values are cursor-invalid;
6. for continuation only, dependency-free token framing/base64url/payload-
   length/schema/key-id grammar/category/profile/scalar-bound checks; failures
   are cursor-invalid and no configuration key lookup occurs;
7. dependency-free topology gate: Disabled or Invalid returns unavailable with
   zero protected-value resolution, C4 DB access or cursor minting;
8. valid PostgresDurable configuration and accepted logical key-id/version;
   an unaccepted token pair is cursor-invalid without arbitrary config lookup;
9. required accepted token-key and active mint-key material resolution;
   unavailable/indeterminate/invalid material is unavailable;
10. fixed-time MAC, authenticated client/principal/category/profile/time/
    boundary and continuation pageSize equality; failures are cursor-invalid;
11. database dependency unavailable/indeterminate;
12. valid query -> 200 with zero or more own references.

For a structurally valid cursor naming an accepted key whose material is
unavailable, result is unavailable even if the token would later prove to have
an invalid MAC or expiry. The service uses only the dependency-free canonical
payload shape and configured accepted-pair membership before material
resolution; it does not trust unauthenticated payload claims as authorization.

Authentication and authorization happen before query disclosure. One request
clock is captured after public shape validation and before key resolution; it
is reused for cursor validation and any next-cursor issuance. Cancellation maps
to the host's normal cancellation behavior and creates no durable residue.

## 5. Canonical cursor codec and absolute vector

### 5.1 Token grammar

```text
token = BASE64URL(payload) "." BASE64URL(mac)
mac   = HMAC-SHA-256(key32, ASCII("tip-88c1-c4-cursor-mac-v1") || 0x00 || payload)
```

Base64url is RFC 4648 URL alphabet, no padding, no whitespace. There is exactly
one dot. Decoder re-encodes both components and requires exact ordinal equality
to reject non-canonical encodings. MAC comparison uses fixed-time equality.
Maximum token length is 244 ASCII characters.

Payload binary layout, in order:

| Field | Encoding |
| --- | --- |
| magic | ASCII `C4R1`, 4 bytes |
| schema version | `0x01` |
| key-id length | unsigned byte, `1..64` |
| key id | strict canonical lowercase ASCII, `[a-z0-9][a-z0-9._-]{0,63}` |
| key version | uint32 big-endian, `1..2147483647` |
| caller category | `0x00` = landed BusinessConsumer enum value |
| query profile | `0x01` = exact C4 list semantics |
| ClientApplicationId | RFC-4122/network-order 16 bytes |
| PrincipalId | RFC-4122/network-order 16 bytes |
| page size | uint16 big-endian, `1..50` |
| last FinalizedAtUtc | UTC ticks, signed int64 big-endian |
| last PackageId | RFC-4122/network-order 16 bytes, non-empty |
| issued-at | Unix seconds, signed int64 big-endian |
| expires-at | Unix seconds, signed int64 big-endian |

Payload length is exactly `86 + key-id-length`. Decoder rejects trailing bytes,
invalid UTC ticks, empty identities, wrong category/profile, expiry not exactly
issued+900 seconds, issued in the future, or `now >= expires`. No clock skew or
grace period is accepted.

Issuance converts the one request clock with `ToUnixTimeSeconds()` and sets
expiry to that integer plus 900. Validation compares the same request clock in
Unix seconds; it does not capture a second time or use a local timezone.

### 5.2 Absolute vector

```text
key bytes:
000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f

key id/version: c4-cursor-01 / 1
client:         11111111-1111-1111-1111-111111111111
principal:      22222222-2222-2222-2222-222222222222
page size:      50
boundary time:  2026-08-19T12:34:56.7890120Z
UTC ticks:      639227396967890120
boundary id:    33333333-3333-3333-3333-333333333333
issued:         2026-08-19T12:35:00Z / 1787142900
expires:        2026-08-19T12:50:00Z / 1787143800

payload length: 98
payload hex:
43345231010c63342d637572736f722d30310000000100011111111111111111111111111111111122222222222222222222222222222222003208defdee46f7fcc833333333333333333333333333333333000000006a85a2f4000000006a85a678

payload base64url:
QzRSMQEMYzQtY3Vyc29yLTAxAAAAAQABERERERERERERERERERERESIiIiIiIiIiIiIiIiIiIiIAMgje_e5G9_zIMzMzMzMzMzMzMzMzMzMzMwAAAABqhaL0AAAAAGqFpng

MAC preimage length: 124
MAC preimage SHA-256:
ffbb3a933b712974dff4959ad02e856fbe4417a3eb2ff795cdb34b657ff11eaa

MAC hex:
003fddb482d18cfb3e7eec47ce4666a661496210a9ff55fd8000f376c4d4bfc3

MAC base64url:
AD_dtILRjPs-fuxHzkZmpmFJYhCp_1X9gADzdsTUv8M

token length: 175
token:
QzRSMQEMYzQtY3Vyc29yLTAxAAAAAQABERERERERERERERERERERESIiIiIiIiIiIiIiIiIiIiIAMgje_e5G9_zIMzMzMzMzMzMzMzMzMzMzMwAAAABqhaL0AAAAAGqFpng.AD_dtILRjPs-fuxHzkZmpmFJYhCp_1X9gADzdsTUv8M
```

The vector must be recomputed independently in review. Tests pin every value,
not merely successful round-trip.

### 5.3 Key-material lifecycle

Protected material is resolved before any owner query. Continuations resolve
the token key and active mint key before the query when they differ. First-page
requests resolve the active key before the query. All leases and decoded key
buffers are disposed/zeroed in `finally`; no `string` is created from decoded
key bytes. A missing accepted key or invalid base64url/length is unavailable.
An unaccepted token key is cursor-invalid without attempting arbitrary config
lookup.

## 6. Exact persistence and index delta

C4 adds:

```text
tables:       0
columns:      0
durable rows: 0
states:       0
events:       0
SQL functions:1
indexes:      1
```

The existing EF configuration gains exactly:

```csharp
entity.HasIndex(row => new
    { row.RecipientClientApplicationId, row.FinalizedAtUtc, row.PackageId })
  .HasDatabaseName("ix_raw_export_recipient_package_reference_list")
  .IsDescending(false, true, true)
  .HasFilter("\"State\" = 'Finalized' AND \"FinalizedAtUtc\" IS NOT NULL");
```

The index is non-unique and non-authoritative. Its exact PostgreSQL shape is:

```sql
CREATE INDEX ix_raw_export_recipient_package_reference_list
ON tagekyc.raw_export_recipient_package_preparations
("RecipientClientApplicationId" ASC, "FinalizedAtUtc" DESC, "PackageId" DESC)
WHERE "State" = 'Finalized' AND "FinalizedAtUtc" IS NOT NULL;
```

No C2 CHECK, alternate key, FK, lifecycle, column or object/provider field
changes. Down drops only the C4 function/index/schema grant; it does not mutate
C2 rows.

## 7. Exact SQL surface and ACL

### 7.1 Function signature and projection

Exactly one new function exists and its identifier is below 63 bytes:

```sql
tagekyc.raw_export_list_recipient_package_references(
    uuid,
    timestamp with time zone,
    uuid,
    integer)
RETURNS TABLE(
    "PackageId" uuid,
    "FinalizedAtUtc" timestamp with time zone)
```

Arguments are, in order:

```text
p_recipient_client_application_id
p_boundary_finalized_at_utc nullable
p_boundary_package_id nullable
p_page_size
```

The function is `LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog`,
owned by `tagekyc_raw_export_deployer`. It rejects with SQLSTATE `P0001` and
message `RAW_EXPORT_PACKAGE_REFERENCE_ARGUMENT_INVALID` when recipient is
null/empty, page size is null/outside 1..50, boundary nullability is unpaired,
or a present boundary PackageId is empty.

The only data query is semantically:

```sql
SELECT p."PackageId", p."FinalizedAtUtc"
FROM tagekyc.raw_export_recipient_package_preparations p
WHERE p."RecipientClientApplicationId" = p_recipient_client_application_id
  AND p."State" = 'Finalized'
  AND p."FinalizedAtUtc" IS NOT NULL
  AND p."PackageId" <> '00000000-0000-0000-0000-000000000000'::uuid
  AND (
       p_boundary_finalized_at_utc IS NULL
       OR p."FinalizedAtUtc" < p_boundary_finalized_at_utc
       OR (p."FinalizedAtUtc" = p_boundary_finalized_at_utc
           AND p."PackageId" < p_boundary_package_id))
ORDER BY p."FinalizedAtUtc" DESC, p."PackageId" DESC
LIMIT p_page_size + 1;
```

The owner predicate is leading and executes in SQL. The repository never loads
foreign rows and filters them in memory. The function accepts no PrincipalId or
PackageId probe, takes no locks, captures no clock, mutates nothing and returns
no total count.

### 7.2 ACL and role

Migration requires the canonical LOGIN to pre-exist, creates or verifies the
NOLOGIN capability, grants one exact membership and schema USAGE, revokes
PUBLIC, and grants EXECUTE only on the exact signature to
`tagekyc_raw_export_package_reference`.

The capability receives no table SELECT/DML and no C1/C2/C3 function. PUBLIC,
the LOGIN directly, unrelated runtime roles and the first four package roles
receive no C4 EXECUTE. Function ACL equality is two-way, not a subset check.

## 8. Application and repository behavior

The API constructs, without interpreting values, this application-boundary
shape from the parsed HTTP query after authentication:

```csharp
public sealed record RecipientPackageReferenceRawQuery(
    IReadOnlyList<string> Keys,
    IReadOnlyList<string> PageSizeValues,
    IReadOnlyList<string> CursorValues);

public interface IRecipientPackageReferenceApplicationService
{
    Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
        AuthenticatedClientContext actor,
        RecipientPackageReferenceRawQuery query,
        CancellationToken cancellationToken);
}
```

`Keys` retains exact case-sensitive decoded key spellings. Omission is an empty
value list; `?cursor=` is a one-element list containing the empty string;
duplicates remain multiple elements. No default model binder may replace this
shape. The application validates actor before inspecting query contents, then
captures the one request clock and delegates to the infrastructure gateway.

The application service validates exact actor category/scope/non-empty ids and
uses the actor's `ClientApplicationId` as the only SQL owner. `PrincipalId` is
used only for cursor binding. Sibling principals under the same client and
scope independently list the same references; one cannot validate the other's
cursor.

Repository results are either:

```text
Success(items pageSize or fewer, hasMore)
Unavailable
```

It maps only the exact SQL argument failure as an internal contract violation;
Npgsql/open/query/cancellation uncertainty never becomes an empty success.
The application removes the `pageSize+1` sentinel, emits at most pageSize and,
when `hasMore`, mints the next cursor from the last emitted item. It never uses
the sentinel row as the boundary. Zero own eligible rows is `200 [] / null`.

Current read-committed semantics are intentional: an ineligible row is skipped;
a newly finalized row ahead of an issued boundary appears only after restart at
page one. No frozen snapshot or issuance claim is made.

## 9. Readiness, composition and predecessor independence

C4 readiness codes in first-error order:

```text
PROD_RAW_EXPORT_PACKAGE_REFERENCE_CONFIG_INVALID
PROD_RAW_EXPORT_PACKAGE_REFERENCE_ROLE_TOPOLOGY_INVALID
PROD_RAW_EXPORT_PACKAGE_REFERENCE_CATALOG_INVALID
PROD_RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_KEY_UNAVAILABLE
```

Disabled-clean returns green. PostgresDurable readiness proves:

- exact active/accepted key-ring syntax and all accepted 32-byte materials;
- exact C4 role/login attributes and one membership edge;
- exact one function name/signature/owner/SECURITY DEFINER/search_path;
- exact function EXECUTE ACL and absence of direct table privileges;
- exact index name, table, ordered keys, directions and normalized predicate;
- exact overload count one and all owned identifiers <=63 bytes;
- C2 and C3 readiness remain green with C4 enabled or disabled.

`READINESS-OWNERSHIP-RULE-01` binds production and proofs. No `LIKE`, prefix,
suffix or open-ended catalog census over `pg_class`, `pg_proc`, `pg_roles` or
`pg_auth_members` is allowed.

Program always registers C4 options/application service and maps the route.
Valid PostgresDurable additionally registers connection/repository/key service;
Disabled/Invalid uses an unavailable gateway after the dependency-free public
classifier and never resolves a cursor key or DB service. Production readiness
registers one C4 check. C4 composition must not make C2 or C3 configuration/
readiness depend on C4.

## 10. Mandatory real-predecessor compatibility proof

One named integration owner must execute exactly:

```text
real C1 assembly fixture
-> real RecipientPackagePreparationProvider
-> landed C2 reserve/encrypt/conditional Put/Finalize
-> persisted Finalized PackageId owned by recipient A
-> C4 production HTTP list as authenticated recipient A
-> exact PackageId present, with no test-supplied owner metadata
-> same opaque PackageId into real C3 CreateDelivery
-> C3 independently authenticates and authorizes
```

This owner is implemented wholly in the new C4 integration-test file. The
integration assembly already has access to the landed internal C3 coordinator,
repository, reader and spool types, while the C3 application service and ports
are public. It may compose those production types and use
`DurableObjectMinioFixture`; it must not call or modify private helpers in
`Tip88C1C3RecipientPackageDeliveryTests.cs`. That predecessor test file remains
an unchanged carried input, not a permanent build path.

The proof may use seeded data for unrelated partitions but this canonical path
must not directly insert a Finalized C2 row, write package metadata/object bytes,
or pass expected owner metadata into C4.

Independent mutations, neighboring predicates active:

1. change only the persisted recipient of the genuinely produced package ->
   reference disappears;
2. remove only the SQL ownership predicate -> foreign real package appears;
3. bypass only C3 re-authorization after handoff -> handoff assertion RED.

## 11. Exact permanent-path allowlist — 35 paths

Only these paths may change in a later ratified build:

```text
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c4_authenticated_package_reference_distribution_build_dispatch.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c4_authenticated_package_reference_distribution_review_ledger.md
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c4_authenticated_package_reference_distribution_as_built.md
src/TagEkyc.Contracts/RawExport/RecipientPackageReferenceContracts.cs
src/TagEkyc.Application/Ports/RecipientPackageReferencePorts.cs
src/TagEkyc.Application/RawExport/RecipientPackageReferenceApplicationService.cs
src/TagEkyc.Application/LocalDev/LocalDevApiKeyStore.cs
src/TagEkyc.Api/RecipientPackageReferenceEndpoints.cs
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/ReadinessEndpoint.cs
src/TagEkyc.Infrastructure/ProtectedValues/ProtectedValueContracts.cs
src/TagEkyc.Infrastructure/ProtectedValues/RecipientPackageReferenceCursorKeyCatalog.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackagePreparationConfig.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.Designer.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceContracts.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceOptions.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceCursorCodec.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceCursorKeyService.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceConnectionFactory.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceRepository.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceReadinessValidator.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceServiceCollectionExtensions.cs
tests/TagEkyc.UnitTests/Tip88C1C4RecipientPackageReferenceCodecTests.cs
tests/TagEkyc.UnitTests/Tip88C1C4RecipientPackageReferenceApplicationTests.cs
tests/TagEkyc.ArchTests/Tip88C1C4RecipientPackageReferenceArchTests.cs
tests/TagEkyc.ArchTests/DockerTestResourceLifecycleTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C4RecipientPackageReferenceTests.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs
tests/TagEkyc.IntegrationTests/Tip83E1ReadinessEndpointTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs
```

The authoritative allowlist census is exactly **35 paths**. Reviewers must count
the literal path lines and require 35/35; no path may be exchanged or silently
dropped.

## 12. Proof manifest — 24 methods, 34 mutations

Exactly one active method owns each proof ID `C401`–`C424`. A Theory is one
method regardless of rows. Scratch mutations add no permanent test method.

| ID | Proof obligation |
| --- | --- |
| C401 | concrete LocalDev and PostgreSQL authenticator->validator->store chains preserve non-empty ClientApplicationId/PrincipalId; isolated C4 scope reaches route; fallback/empty rejected |
| C402 | exactly one GET route, query allowlist, no body/lookup/inbox/download/provider surface |
| C403 | omitted cursor -> first-page 200 while present-empty cursor -> exact cursor-invalid 400 at HTTP boundary |
| C404 | first-page default/range/cardinality and continuation omitted/exactly-one-equal/duplicate-or-mismatching pageSize semantics |
| C405 | byte-exact cursor layout, token, MAC, canonical base64url and absolute vector |
| C406 | exact client/principal/profile/category/version/TTL binding, replay and key rotation/unavailability |
| C407 | exact SQL owner, Finalized, non-null time and non-empty PackageId predicates |
| C408 | descending composite total order, equal-time tie-breaker, boundary and pageSize+1 trim |
| C409 | foreign-present versus foreign-absent produces identical public own page/cursor shape |
| C410 | later key revocation does not hide reference; C3 independently may deny delivery |
| C411 | non-substitutable real C1 -> C2 Finalize -> C4 HTTP -> real C3 handoff |
| C412 | listing creates zero DB/provider/audit/issuance mutation and uses no S3 credential |
| C413 | exact one SQL signature, argument guard, two-column return and no overload |
| C414 | exact role/LOGIN attributes, membership, schema/function ACL and zero table privilege |
| C415 | exact indexed config tree/key-ring/readiness positive; topology short-circuit and each typed readiness failure precedence |
| C416 | exact partial index, additive model, all three raw-worktree snapshot pins move together |
| C417 | disposable apply/Down/reapply, exact drop in finally, shared database remains latest |
| C418 | four census families exact; C2/C3 readiness remains green; no wildcard proof census |
| C419 | DTO nullability/JSON/error envelope has no owner/principal/count/provider/key/digest leak |
| C420 | Disabled/Invalid and DB/key dependency failures never access forbidden dependencies, become empty success or leave residue |
| C421 | resolved/decoded cursor key buffers are zeroized/disposed and never logged/stringified |
| C422 | sibling principals independently list same app-owned PackageId but cannot reuse cursor |
| C423 | architecture forbids S3/Raw BIO/CEK/private-key/C2-C3 mutation and extra route dependencies |
| C424 | affected tests, one final unfiltered Release suite, diff/hashes/residue/allowlist closeout |

### 12.1 Required mutations

```text
C4M01  Guid.Empty or principal/client fallback admitted
C4M02  category or required scope widened
C4M03  omitted and present-empty cursor collapsed to one request-invalid branch
C4M04  first-page pageSize default/range/cardinality weakened
C4M05  continuation omission silently defaults to 25 instead of cursor value
C4M06  duplicate/bad/mismatching supplied continuation pageSize accepted or mapped request-invalid
C4M07  MAC verification/fixed-time comparison removed
C4M08  ClientApplicationId cursor binding removed
C4M09  PrincipalId cursor binding removed
C4M10  schema/query/category/TTL/issued-expiry comparator removed
C4M11  non-canonical base64url, trailing bytes or unaccepted key id admitted
C4M12  SQL recipient ownership predicate removed
C4M13  eligible State widened beyond exact Finalized
C4M14  FinalizedAtUtc/non-empty PackageId predicate removed
C4M15  PackageId tie-breaker or one descending boundary comparator removed
C4M16  pageSize+1 bound removed or sentinel used as emitted boundary
C4M17  foreign count/global latest/application-memory filter influences response
C4M18  live recipient-key Active join added to C4 visibility
C4M19  direct-seeded Finalized row substitutes for real C2 path
C4M20  C3 re-authorization bypassed after PackageId handoff
C4M21  SECURITY DEFINER/owner/search_path/function ACL comparator weakened
C4M22  SQL signature/return projection/overload surface changed
C4M23  role or LOGIN attribute/membership/table-privilege comparator weakened
C4M24  exact catalog census replaced by wildcard and sibling object added
C4M25  index owner/key order/direction/filter comparator weakened
C4M26  migration Down touches shared database or disposable drop/finally removed
C4M27  one-sided LF comparison or one-of-three snapshot pin drift admitted
C4M28  topology short-circuit removed; indexed key-ring/case/duplicate validation
       weakened; accepted key failure falls back unsigned; or an unaccepted
       token key resolves arbitrary config
C4M29  key buffer zeroization/disposal/redaction guard removed
C4M30  C4 adds an S3 client/credential/provider operation
C4M31  listing writes issuance/audit state or mutates C2/C3
C4M32  PackageId lookup, inbox/notification/total-count or extra route appears
C4M33  C4 role aliases predecessor role or any 5/5/5/5 census regresses
C4M34  owned identifier >63 bytes or ModelSnapshot/additive tripwire weakened
```

For C4M03, collapsing both HTTP forms to
`RAW_EXPORT_PACKAGE_REFERENCE_REQUEST_INVALID` must make both bites RED: the
omitted form no longer returns 200 and the empty form no longer returns exact
cursor-invalid. Every mutation must RED at its own named assertion while
neighboring predicates remain active, then restore the owner file byte-exactly
and rerun the canonical proof PASS. GREEN is HARD STOP.

## 13. Validation policy

During implementation:

1. build the affected project after each coherent edit;
2. run only changed/affected C4 and predecessor proofs;
3. run the canonical owner before each mutation;
4. run the mutation alone, require the named RED reason, restore exact SHA, and
   rerun the owner;
5. run pending-model, migration apply/Down/reapply and readiness partitions in
   the disposable database;
6. run one complete, unfiltered Release suite exactly once on final bytes.

Do not repeatedly run B4/DK-PROD/full-suite tests when their bytes/surfaces are
unaffected. They become affected only by migration/catalog/role/bootstrap/
shared readiness or their own test-byte changes. The final full suite remains
mandatory and is not replaced by targeted evidence.

Final GREEN permits only exact-hash freeze, as-built/ledger update and a review-
only closeout bundle. Any non-canonical final-suite failure is STOP/RRI; no edit
or retry without new authority.

## 14. Controlled execution order

```text
Task 0  hashes, branch/HEAD/stage, authority chain, tripwire and LOGIN census
Task 1  public contracts, ports, application service and cursor codec vector
Task 2  options, protected-value catalog/key service and DI fail-closed shapes
Task 3  existing EF config index + migration/Designer/snapshot in disposable DB
Task 4  SQL repository, role/ACL/readiness and exact predecessor census
Task 5  HTTP endpoint/Program/readiness composition including absent-vs-empty
Task 6  C401-C424 positive owners and real C1-C2-C4-C3 path
Task 7  C4M01-C4M34 mutation/restore sweep
Task 8  affected validation, pending-model, Release build
Task 9  one final unfiltered Release suite
Task 10 exact hashes, as-built, ledger and review-only closeout bundle
```

No stage or commit occurs in this sequence.

## 15. STOP/RRI conditions

STOP before expansion if:

- a 36th path, a second SQL function/index, or expansion beyond 24 proofs/34
  mutations becomes necessary;
- omitted cursor and present-empty cursor cannot remain distinct at HTTP entry;
- a cursor can be validated without exact actor/query/TTL/MAC binding;
- key rotation requires unsigned fallback or accepts an unlisted key;
- the SQL function can load a foreign row before filtering, returns count or
  accepts a PackageId probe;
- implementation requires per-principal package ownership or predecessor
  identity/schema changes;
- C2/C3 API/schema/behavior changes beyond the supporting C2-table index;
- a table/column/state/event/issuance/audit subsystem becomes necessary;
- direct table SELECT/DML, S3/Raw BIO/CEK/private-key authority is required;
- a sixth S3 credential, delegated recipient, inbox, notification, exact lookup
  or package lifecycle UX is introduced;
- any wildcard catalog census, identifier truncation, shared-DB Down, snapshot
  pin drift or anonymous-volume growth appears;
- real C1-C2-C4-C3 compatibility can be satisfied by test-authored metadata;
- a required mutation stays GREEN or REDs for the wrong reason;
- final full suite has a non-canonical failure.

## 16. Analytical summary and debt disposition

### Intent and expected outcome

Expose only opaque PackageId references owned by the authenticated client
application, using a principal-bound finite cursor, while C3 remains the sole
delivery authority.

### Accepted decisions

| Decision | Result | Non-claim |
| --- | --- | --- |
| PackageId is the reference | no second identifier | possession is not authority |
| app ownership + principal cursor | sibling principals see same app packages but not each other's cursor | no patient inbox isolation |
| projection-only Finalized listing | no issuance/audit state | no seen/received claim |
| one SQL function/index | bounded owner-leading keyset query | no lookup/search/count |
| protected rotating HMAC ring | 15-minute authenticated cursor | no bearer/delivery token |
| fifth DB role only | zero S3 delta | no provider operation |

### Deferred/carry-forward

```text
PER-PRINCIPAL-RECIPIENT-OWNERSHIP       NOT_LANDED / REQUIREMENT_DEPENDENT
C3-DELEGATED-DELIVERY-V1                DEFERRED
notification/inbox UX                   DEFERRED
recipient-key operations                DEFERRED
C4-AUTH-SCOPE-PROVISIONING-DEBT-01       DEFERRED TO RECIPIENT MANAGEMENT/ACTIVATION
production Raw BIO adapter              DEFERRED
SHARED-DB-MIGRATION-STATE-DEBT-01       OPEN HARNESS DEBT
LINE-ENDING-FRAGILE-COMPARISON-DEBT-01  OPEN HARNESS DEBT
READINESS-OWNERSHIP-DEBT-01              CARRIED / SEPARATE OWNER
R2-OBS-DEBT-01                           CARRIED / SEPARATE OWNER
TIP68-HOST-STARTUP-ROOT-CAUSE-01        MITIGATED / ROOT CAUSE UNPROVEN
```

C4 does not make the product pilot-ready. Pilot still requires production raw
source, recipient-key operations, an authorized managed credential carrying
the C4 scope, deployment/activation and any required per-principal/delegated/
inbox product surface. The isolated LocalDev reference key is not production
enrollment evidence.

## 17. Round-3 exact-byte closure review request

Reviewers must independently verify:

1. exact authority/scope/baseline and 35-path allowlist count;
2. no Round-1 product decision reopened;
3. omitted-versus-empty cursor, duplicate continuation pageSize and branch-
   specific errors are consistent across route, precedence, codec and mutations;
4. binary codec/vector recomputes byte-exactly, including network-order GUIDs,
   UTC ticks, Unix seconds, preimage and MAC;
5. the indexed key-ring representation, lowercase identity, rotation,
   unavailable-vs-invalid classification and zeroization are fail closed;
6. SQL signature/projection/argument guard, owner-leading predicate, total order
   and pageSize+1 behavior are exact;
7. index/model delta is additive and every raw-worktree tripwire is budgeted;
8. exact role/login/function/index ACL/readiness and four-family census contain
   no wildcard or hidden predecessor dependency;
9. real C1-C2-C4-C3 proof cannot be direct-seeded or input-echoed;
10. every proof/mutation has one observable discriminator and wrong-reason
    rejection cannot satisfy it;
11. disposable DB, LF comparison, pre-Up LOGIN and Docker residue controls are
    executable;
12. concrete authenticator/validator/store bytes preserve PrincipalId, the
    isolated LocalDev C4 key does not broaden another key, and managed
    production scope enrollment remains an explicit activation debt;
13. Disabled/Invalid reaches the topology short-circuit after dependency-free
    request checks and before every cursor-key/DB operation;
14. non-claims, inherited debts and STOP/RRI boundaries remain honest.

Round-3 PASS may recommend Homeowner controlled-build ratification. It grants no
implementation authority.

Current state:

```text
C4 Scope v0.2:                         RATIFIED / ROUND 1 CLOSED
C4 Dispatch v0.2:                      ROUND_3_EXACT_BYTE_CLOSURE_CANDIDATE
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1:  ASSIGNED_OPEN
Independent Round-3 closure review:    PENDING
Homeowner build ratification:          NOT_RECORDED
Implementation authority:              NONE
```
