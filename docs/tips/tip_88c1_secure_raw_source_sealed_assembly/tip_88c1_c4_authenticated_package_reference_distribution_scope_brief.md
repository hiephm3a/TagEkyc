# TIP-88C1-C4 — Authenticated Package Reference Distribution Scope Brief

Version: `0.2`
Status: `ROUND_1_CLOSURE_CANDIDATE`
Date: `2026-08-19`
Baseline commit: `8d76689a746960bd24ca944eaaf4858f39026511`
Authority SHA-256: `C9469AC246901CD97DF57D0DC7AA81D55831273DE3F8956991BBCD9C39761027`
Authoring mode: `DOCS_ONLY`
Implementation authority: `NONE`

## 0. Changelog

### v0.2 — Round-1 closure candidate

- Distinguishes an absent cursor, which is the valid first-page request, from a
  present-but-empty or otherwise invalid cursor, which returns the uniform
  cursor-invalid result.
- Freezes effective page size in the cursor: a continuation without `pageSize`
  uses the cursor value, while a supplied continuation value must match it.
- Clarifies the landed trust model: package references are owned by the client
  application; principals under that application may independently list the
  same references but cannot reuse one another's cursor.
- Registers per-principal recipient ownership as an unlanded,
  requirement-dependent product gap rather than implying that C4 provides it.
- Reconciles the two independent v0.1 reviews without reopening the remaining
  scope decisions.

### v0.1 — Round-1 scope candidate

- Assigns `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` to C4 without marking it
  closed.
- Selects a minimal authenticated, direct-recipient, reference-listing API.
- Retains the existing opaque `PackageId`; it is a locator, never bearer
  authority.
- Defines package ownership as the landed C2
  `RecipientClientApplicationId`; a stable non-empty `PrincipalId` is the
  authenticated actor and cursor-binding identity, not a new package owner.
- Selects projection-only eligibility over landed C2 data and rejects a new
  issuance table/event subsystem.
- Pins total-order keyset pagination, recipient-bound tamper-protected cursors,
  non-disclosure precedence and bounded page sizes.
- Keeps recipient-key revocation as a C3 delivery gate rather than hiding a
  historically finalized reference.
- Pins all four capability census families independently.
- Requires executable compatibility proof using a package produced by the real
  C2 pipeline.
- Carries shared-database, line-ending, LOGIN-role and exact-catalog pre-controls
  into the future Round-2 dispatch.

## 1. Authority and source hierarchy

This artifact is a docs-only scope candidate. It does not authorize source,
test, project, package, API, schema, migration, provider, environment,
deployment, stage, commit, push or production work.

Precedence is:

1. current explicit Homeowner instruction;
2. ratified and landed TIP-88C1 C1, C2 and C3 contracts;
3. this C4 scope only after independent Round-1 review and Homeowner
   ratification;
4. a future C4 executable dispatch under separate authority.

Source anchors at drafting time:

| Source | SHA-256 | Use |
| --- | --- | --- |
| Homeowner C4 Round-1 authority | `C9469AC246901CD97DF57D0DC7AA81D55831273DE3F8956991BBCD9C39761027` | exact product/trust boundary and three-round discipline |
| C2 scope | `B48AECEEEB2CEA2B71A6BA2951BEC7339195DA10AB72713AD0D834051FC56851` | recipient package ownership and key-snapshot intent |
| C2 dispatch | `0790D1A6828C3365178011F0E10A1B58757B4FE7E2214828C55A6D720B0BB64D` | package state/custody and real-C2 proof surface |
| C2 as-built | `C36855C44C221A1744570E9943B1659F462DE50169BD61A3BF89DDEA997B95AA` | landed C2 evidence |
| C3 scope | `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` | direct-recipient delivery and D2 key-revocation decision |
| C3 dispatch | `AB415A0526D65307E0036A450374928BA81BEF53C4E8BACAA7D837997BB86988` | C3 authority, non-disclosure and proof contract |
| C3 as-built | `C204A40F5C9AD03C852CAD714606403730F3FCC5873DCD7C9D3087B330BF2D33` | landed delivery behavior and open reachability debt |
| `AuthenticatedClientContext.cs` | `4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401` | API key, client application, category, scopes and principal identity |
| `IApiKeyAuthenticator.cs` | `72A7E14658FFC4F3990D87CA590CD2A19084FC012C3D0813A2351A12750F9CF4` | landed authentication boundary |
| `ApiKeyProvisioningService.cs` | `540A810A10A9099BCD333243BC056228B5AE6AFA95773D87C07FBDFD3552A7D1` | persisted non-empty principal provisioning source |
| `PostgresHashedApiKeyStore.cs` | `96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91` | persisted principal readback into authenticated context |
| C3 application service | `1A5EF013B848565C784443005A4C7601F345A9D58E4FE0F7545C6F30DCE82E15` | BusinessConsumer/principal/scope enforcement pattern |
| C2 package row | `AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E` | package owner, state and ordering fields |
| C2 key row | `8D73CA3C4ED2F2C43C62A5D754918D60EE554DA551EEB6014D7588C6B207E903` | key lifecycle source; not a C4 visibility predicate |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | exact package state, ACL and sparse invariants |
| C3 migration | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` | landed fourth package capability and C3 boundary |
| C2 integration proof file | `F59C95631E365C851622B3D5585FA1CD4ECBDB914288F5764AF9C87B7E9AA6A3` | real `Prepare -> Finalize` fixture machinery |
| C3 integration proof file | `672CE05ED049355DF6E9BE73FB19A5058B4B29026E322CBA90C21E9EDF768071` | predecessor compatibility proof pattern |

The baseline commit is the controlling landed provenance. Untracked historical
documents in the working tree are read-only context and are not silently
admitted into a future C4 allowlist.

## 2. Landed feasibility facts

The landed C2 package row already carries all data needed for a minimal C4
projection:

```text
PackageId
RecipientClientApplicationId
RecipientKeyId / RecipientKeyVersion / RecipientKeyFingerprint
RecipientKeyRevision / RecipientKeyValidFromUtc / RecipientKeyValidUntilUtc
State
FinalizedAtUtc
```

The C2 sparse constraint requires a `Finalized` package to carry non-null
`FinalizedAtUtc`, package length/digests and provider evidence. `PackageId` is
unique and non-empty. `FinalizedAtUtc` is nullable at the CLR/schema surface and
is not unique, so it is not a total ordering key by itself.

The C3 delivery path already:

- authenticates through `IApiKeyAuthenticator`;
- requires `BusinessConsumer`, non-empty `PrincipalId` and its own download
  scope;
- applies exact recipient-scoped database predicates;
- rechecks the frozen key registration and current key eligibility;
- treats missing, foreign and non-finalized packages uniformly;
- verifies the full encrypted package before the first response byte.

Therefore C4 does not need package bytes, provider locators, S3 access, C3
delivery state, current recipient-key state or a new durable reference-issuance
row to make an eligible `PackageId` reachable.

## 3. Product objective

C4 gives an authenticated direct recipient the smallest server surface needed
to discover opaque references to its own finalized C2 packages and then present
one selected `PackageId` to C3.

```text
BusinessConsumer authentication
-> C4 reference-read scope
-> exact client-application owner predicate
-> bounded keyset page of opaque PackageId references
-> selected PackageId
-> C3 CreateDelivery / Content
-> C3 authenticates and authorizes again
```

C4 success proves only that TagEkyc presented a reference to a package owned by
the authenticated recipient client application at query time. It does not
prove download, receipt, decryption, viewing, acceptance or clinical use.

## 4. Exact C4 boundary

### 4.1 C4 owns

- one authenticated direct-recipient package-reference listing route;
- exact owner and eligible-state projection over landed C2 rows;
- opaque `PackageId` presentation;
- stable total ordering and keyset continuation;
- recipient- and query-bound cursor integrity;
- bounded page size and non-disclosure precedence;
- one C4 database read capability with exact role/readiness ownership;
- proof that real C2 output is listed for only its rightful recipient;
- handoff proof that the selected reference remains subject to independent C3
  authorization.

### 4.2 C4 does not own

- package creation, encryption, custody, replacement, re-key or deletion;
- package provider access, streaming, delivery state or receipts;
- Raw BIO, CEK, private key or plaintext access;
- recipient-key enrollment, rotation or revocation management;
- notification, push, SMS, email, inbox UI, read/unread or message lifecycle;
- delegated, guardian, family, representative or proxy authorization;
- a public package lookup/search endpoint;
- production raw-source integration, deployment or pilot activation.

`C3-DELEGATED-DELIVERY-V1` remains deferred and outside C4.

## 5. Identity and ownership model

The caller is exactly:

```text
CallerCategory       = BusinessConsumer
ClientApplicationId  = authenticated, non-empty
PrincipalId          = authenticated, stable, non-empty
Required scope       = business.raw-export.package.references.read
```

Package ownership is the exact landed equality:

```text
package.RecipientClientApplicationId
    = actor.ClientApplicationId
```

`PrincipalId` is the authenticated human/service actor, persisted with the
landed API-key record and read back by `PostgresHashedApiKeyStore`. It is bound
into the cursor. C4 does not derive it from `ClientApplicationId`, accept
`Guid.Empty`, or accept it in route/query/body fields. It is not package
ownership because C2 does not persist a principal id on the package.

Consequently, principals authenticated under the same recipient client
application see the same application-owned package references. This initial
slice models the direct recipient as the client application (for example, one
hospital application), not as an independently addressable patient/user within
that application. If the product requires per-principal package ownership,
that is a genuine predecessor identity-model expansion and is STOP/RRI rather
than an application-layer guess.

`AllowedClientApplicationIds`, API-key prefix, OperatorAdmin, CaptureAgent,
TrustedAdapter, package creator identity and caller-supplied recipient ids do
not grant reference access.

## 6. Minimal API contract

The selected Round-1 surface is one route:

```http
GET /api/ekyc/raw-export/package-references?pageSize={n}&cursor={opaque}
Authorization: existing API-key authentication
```

Rules:

- on a first-page request, `cursor` is absent and an absent `pageSize` defaults
  to `25`; a supplied `pageSize` must be an integer in `1..50`;
- on a continuation request, the cursor freezes the effective page size; an
  absent `pageSize` uses the value authenticated by the cursor, while a supplied
  value must be an integer in `1..50` and exactly equal that cursor value;
- an absent cursor is valid only as the first-page form; a cursor query
  parameter that is present but empty is invalid, and the cursor is opaque
  thereafter;
- no `recipientId`, `ClientApplicationId`, `PrincipalId`, `PackageId`, state or
  sort override is accepted;
- response is `200` with an array of zero to fifty references and an optional
  `nextCursor`;
- no total count, foreign count, provider locator, object key, key metadata,
  package digest, subject identity or delivery state is returned.

Reference DTO:

```text
PackageId       non-empty UUID, D-format JSON string
FinalizedAtUtc  UTC timestamp, used only as presentation/order metadata
```

Page DTO:

```text
Items           ordered reference array
NextCursor      opaque string or null
```

There is no C4 exact-PackageId lookup route. The existing C3 CreateDelivery
route remains the only next hop for a selected reference.

## 7. Reference identity decision

The existing `PackageId` is the C4 presented reference.

```text
PackageId possession != package-delivery authority
```

This is safe in the bounded model because:

1. `PackageId` is opaque, unique and non-empty;
2. C4 never accepts it as a lookup parameter;
3. C3 re-authenticates the caller and repeats an exact recipient predicate;
4. C3 repeats package/key eligibility and full provider verification;
5. foreign, missing and ineligible packages remain publicly indistinguishable
   at the C3 boundary.

A second `DeliveryReference` token would add identifier lifecycle and
revocation semantics without adding authority. It is rejected for this slice.
If later review demonstrates that a separate token is necessary for security
rather than UX, C4 must STOP/RRI with the exact trust reason.

## 8. Eligibility and reference lifecycle

A package is reference-presentable only when all predicates hold in the same
database query:

```text
RecipientClientApplicationId = authenticated ClientApplicationId
State                         = Finalized
FinalizedAtUtc                IS NOT NULL
PackageId                     <> Guid.Empty
```

The query does not infer ownership or eligibility from object key, provider
locator, global latest package, key id, package existence or caller input.

The landed C2 row has no independent package-retention deadline, expiry flag or
separate lifecycle projection. Its current `State` is therefore the only
authoritative package lifecycle gate available to C4, and exact `Finalized` is
the closed eligible set. C4 must not invent a retention window from
`FinalizedAtUtc`, key validity or C3 delivery expiry. If a later owning slice
lands an authoritative package-retention/tombstone field, C4 visibility must be
re-adjudicated rather than silently ignoring it.

Current recipient-key state is deliberately not a C4 visibility predicate.
The frozen key identity remains historical package metadata. Per C3 decision
D2, later revocation/expiry blocks new C3 delivery admission but does not
retroactively mutate or hide a finalized package reference. C4 therefore does
not join the live key-registration table and does not duplicate C3 policy.

There is no new durable issuance state. Visibility is a projection of current
landed C2 state:

- a newly finalized package appears on a fresh first-page query;
- a row not exactly `Finalized` is absent;
- a package becoming ineligible between pages is skipped on continuation;
- a package finalized ahead of an already-issued boundary is visible only when
  traversal restarts at page one;
- a replayed cursor may observe current eligibility, not a frozen snapshot.

This is accepted read-committed keyset pagination, not snapshot export.

## 9. Total ordering and cursor security

References are ordered descending by the composite key:

```text
(FinalizedAtUtc DESC, PackageId DESC)
```

`FinalizedAtUtc` alone is prohibited because it is nullable and non-unique.
Eligible rows require it non-null; `PackageId` supplies the deterministic
tie-breaker. The continuation predicate for a descending page is semantically:

```text
FinalizedAtUtc < boundary.FinalizedAtUtc
OR (FinalizedAtUtc = boundary.FinalizedAtUtc
    AND PackageId < boundary.PackageId)
```

The cursor is a stateless, versioned, tamper-protected token. Round 2 must pin
its exact codec and absolute vectors. Its authenticated payload must bind:

```text
cursor schema/profile version
authenticated ClientApplicationId
authenticated PrincipalId
caller category = BusinessConsumer
query semantic/profile version
page size
last FinalizedAtUtc
last PackageId
issued-at UTC
expires-at UTC
```

Initial cursor lifetime is `15 minutes`. Validation uses one server clock.
The MAC key comes through the existing protected-value boundary under a
C4-owned purpose/configuration; it is never caller supplied or persisted in a
cursor. Round 2 must define key id/version, readiness and rotation behavior
without exposing key material.

Cursor rules:

- cursor absence selects the first page and is not an error;
- a cursor parameter that is present but empty is invalid;
- a continuation with no `pageSize` uses the cursor's authenticated page size;
- a continuation with a supplied `pageSize` that differs from the cursor's
  authenticated value returns the same uniform cursor-invalid result;
- recipient A's cursor replayed by recipient B is the same public invalid-
  cursor result as a tampered token;
- another principal under the same client application cannot replay it because
  `PrincipalId` is bound;
- same-actor replay before expiry is allowed and carries no delivery authority;
- expired, malformed, wrong-version, wrong-query or MAC-invalid cursors are
  indistinguishable;
- offset pagination is prohibited;
- no cursor contains provider locator, package digest, recipient key or total
  count.

## 10. Non-disclosure and public precedence

Precedence is stable:

| Priority | Condition | Public result | State effect |
| --- | --- | --- | --- |
| 1 | missing/invalid authentication | existing authentication failure | none |
| 2 | wrong category, missing scope or empty principal | `RAW_EXPORT_PACKAGE_REFERENCE_FORBIDDEN` / 403 | none |
| 3 | invalid first-page page size or other non-cursor request shape | `RAW_EXPORT_PACKAGE_REFERENCE_REQUEST_INVALID` / 400 | none |
| 4 | cursor is present but empty, malformed, tampered, foreign, expired or wrong-profile; or supplied continuation page size differs from its authenticated cursor value | `RAW_EXPORT_PACKAGE_REFERENCE_CURSOR_INVALID` / 400 | none |
| 5 | cursor-key or database dependency unavailable/indeterminate | `RAW_EXPORT_PACKAGE_REFERENCE_UNAVAILABLE` / 503 | none |
| 6 | valid query | `200` with own eligible references, possibly empty | none |

The SQL function takes authenticated owner identity as an argument and includes
it in the leading predicate. It never loads a foreign package and filters it in
application memory. C4 accepts no PackageId probe, returns no total count and
executes a bounded `pageSize + 1` owner-scoped query. It does not promise
constant-time database execution, but it must not branch on, count or return
foreign rows.

Required non-disclosure proof compares:

```text
database contains a foreign eligible package
database contains no foreign eligible package
```

For an authenticated recipient with the same own rows, public status, shape,
items and cursor semantics must be identical. Timing instrumentation may flag
gross cross-recipient scans but must not claim cryptographic constant time.

## 11. Audit decision

C4 adds no durable listing/issuance event in the initial slice.

Reasoning:

- listing is read-only projection, not authorization or delivery;
- a durable event would create an issuance lifecycle that is not needed for
  safety;
- C3 already owns durable delivery attempts and receipts;
- access logs may support operations but are not authoritative C4 evidence.

C4 therefore makes no claim that a recipient saw, selected, downloaded,
received, decrypted or viewed a package. If a later compliance requirement
demands durable listing audit, it must state the exact claim and open a separate
bounded amendment.

## 12. Capability, role and readiness census

The landed package graph after C3 is four database capabilities/roles/logins
and five S3 credentials. C4 adds database read authority only:

```text
S3 provider credentials:  5 -> 5
DB capabilities:          4 -> 5
SQL capability roles:     4 -> 5
SQL LOGIN roles:          4 -> 5
```

Proposed fifth database member:

```text
capability/role: tagekyc_raw_export_package_reference
LOGIN role:      tagekyc_raw_export_package_reference_login
```

The C4 capability may execute only exact C4 SECURITY DEFINER read functions
and use the `tagekyc` schema. It receives no table SELECT/DML, C2 prepare/
reconcile/lifecycle, C3 delivery mutation or other runtime capability.

C4 introduces no S3/object-store configuration or credential. C4 readiness is
database/configuration-owned and must fail closed for missing/aliased LOGIN,
wrong membership, role attributes, function owner, `SECURITY DEFINER`, exact
`search_path=pg_catalog`, overload/signature drift, public/grantable ACL or
unexpected grants.

`READINESS-OWNERSHIP-RULE-01` applies at production and proof level. No C4
readiness or test evidence may use open-ended `LIKE`, prefix or suffix catalog
census over `pg_class`, `pg_proc`, `pg_roles` or `pg_auth_members`. Every owned
set is enumerated exactly.

## 13. Persistence, SQL and indexing boundary

The working assumption is confirmed: C4 needs no new durable issuance table,
event table, state machine or package column.

Round 2 may define:

- one exact recipient-scoped SECURITY DEFINER list function;
- the fifth capability/LOGIN role, ownership, grants and readiness surface;
- a partial/supporting index whose leading key is
  `RecipientClientApplicationId` and whose total ordering suffix is
  `FinalizedAtUtc, PackageId`, limited to finalized/non-null rows;
- C4 cursor-key configuration/readiness and application DTO/service/route.

The supporting index is non-authoritative and changes no C2 state semantics.
C4 must not grant direct table access. Any proposal to add a mutable package
field, C2/C3 command, reference row or event is STOP/RRI.

## 14. Mandatory real-predecessor proof

At least one named Round-2 integration proof must be impossible to satisfy with
a test-authored substitute:

```text
real C1 assembly fixture
-> real RecipientPackagePreparationProvider
-> landed C2 reserve/encrypt/conditional Put/Finalize
-> persisted Finalized PackageId owned by recipient A
-> C4 production list path as authenticated recipient A
-> exact PackageId present
-> same opaque PackageId passed to real C3 CreateDelivery
-> C3 independently authenticates and authorizes
```

The canonical proof must not directly insert a Finalized C2 row, write package
metadata/object bytes or pass expected owner metadata into C4.

Independent mutations:

1. change the recipient on the genuinely produced package while retaining all
   other fields; the reference must disappear from A's page;
2. remove only the ownership predicate; a foreign real package appears and the
   proof must RED at the cross-recipient assertion;
3. bypass C3 re-authorization after C4 handoff; the handoff proof must RED.

Each mutation must fail at its own discriminator while neighboring predicates
remain active. Static census and seeded rows may support other partitions but
cannot replace this proof.

## 15. Required semantic traces

### 15.1 Invariant trace matrix

| ID | Requirement | Owner | Preconditions/order | Public surface | Outcome/state | Required bite |
| --- | --- | --- | --- | --- | --- | --- |
| C4-I01 | direct BusinessConsumer only | API/application | auth -> scope -> category -> principal | one GET route | 403, no DB disclosure | wrong category/scope/empty principal |
| C4-I02 | exact client-app ownership | DB function | owner predicate before ordering/limit | reference items | own rows only | real C2 recipient change + predicate removal |
| C4-I03 | finalized package only | DB function | state + non-null time predicate | reference items | no non-final state | widen each state independently |
| C4-I04 | PackageId is reference, not authority | C4 + C3 | C4 output -> independent C3 auth | opaque PackageId | C3 decides | bypass/reuse mutation |
| C4-I05 | total stable order | DB + codec | `(time,id)` descending | page sequence | no dup/skip for stable set | remove tie-breaker with equal timestamps |
| C4-I06 | recipient-bound cursor | cursor codec | verify MAC/context/expiry before query | cursor | uniform 400 | A-to-B, principal, tamper, expiry mutations |
| C4-I07 | bounded non-disclosure | DB/API | exact owner leading predicate, limit+1 | page only | no total/foreign oracle | foreign-present vs absent comparison |
| C4-I08 | current key does not hide history | DB function | no live-key join | reference item | reference remains; C3 may deny | revoked-key visibility + C3 denial |
| C4-I09 | no issuance state/audit overclaim | architecture | read-only projection | no commands/events | no durable mutation | forbidden write/event dependency |
| C4-I10 | exact capability ownership | migration/readiness | pre-Up LOGIN -> exact grants | readiness | fail closed | role/ACL/owner/config/overload mutations |
| C4-I11 | no provider authority | architecture/readiness | S3 census stays 5 | none | zero object calls | credential/config/client dependency mutation |
| C4-I12 | real predecessor compatibility | integration | real C2 Finalize before C4 | real reference | listed then C3 handoff | direct-seed substitute forbidden |

### 15.2 Ordering graph

```text
authenticate
-> validate category/scope/non-empty identities
-> distinguish absent first-page cursor from present continuation cursor
-> if first page: validate supplied page size or apply default 25
-> if continuation: verify non-empty format/version/MAC/context/expiry, recover
   frozen page size, and reject a supplied mismatching page size
-> exact recipient-scoped DB list
-> exact Finalized/non-null eligibility
-> composite descending boundary
-> fetch at most pageSize + 1
-> emit at most pageSize items
-> mint recipient/query-bound next cursor only when another own row exists
```

No lock, transaction-spanning I/O, provider operation or C2/C3 mutation is
required for this read-only flow.

### 15.3 Test-bite matrix

| Risk | Positive proof | Mutation that must RED |
| --- | --- | --- |
| PackageId becomes bearer authority | C4 output still denied by C3 under wrong recipient | skip C3 auth/owner check in handoff fixture |
| owner predicate self-fulfilling | real C2 package listed for A only | change persisted owner; remove SQL owner predicate |
| timestamp is not total | equal timestamps yield deterministic no-duplicate pages | remove PackageId tie-breaker |
| cursor replay crosses actor | A cursor works only for exact A principal/client | replay under B or sibling principal |
| cursor is editable | canonical cursor round-trips | flip boundary/page-size/context/MAC byte |
| absent/continuation page size is ambiguous | first page defaults to 25; continuation omission preserves cursor value | treat absent first-page cursor as invalid; silently default continuation to 25; accept a mismatching supplied continuation size |
| key revocation hides history | revoked frozen key reference remains listed | add live-key `State='Active'` join |
| foreign rows influence response | A response equal with/without B rows | count/global latest/offset mutation |
| hidden durable issuance | listing leaves DB unchanged | add/reference event or mutable row dependency |
| wildcard readiness returns | exact set census passes | replace exact names with wildcard and add sibling object |
| test-generated predecessor | real C2 -> C4 -> C3 path passes | replace with direct-seeded Finalized row |

## 16. Harness and technical-debt pre-controls

The following debts are hazards, not C4 objectives:

```text
SHARED-DB-MIGRATION-STATE-DEBT-01
LINE-ENDING-FRAGILE-COMPARISON-DEBT-01
TIP68-HOST-STARTUP-ROOT-CAUSE-01
R2-OBS-DEBT-01
READINESS-OWNERSHIP-DEBT-01
```

The Round-2 dispatch must budget from its first allowlist:

- migration, Designer, ModelSnapshot and every absolute snapshot tripwire;
- a disposable database cloned/created at the canonical current migration;
- apply/Down/reapply only inside that database and exact drop in `finally`;
- no mutation or Down against a shared canonical template;
- provision/verify every new LOGIN role before migration Up;
- normalize both sides of catalog/model SQL comparisons with
  `.ReplaceLineEndings("\n")` without deleting/collapsing visible whitespace,
  tokens, predicates or identifiers;
- Docker teardown that creates no anonymous-volume growth;
- exact named catalog census under `READINESS-OWNERSHIP-RULE-01`.

## 17. Risks and STOP/RRI conditions

| Risk | Disposition |
| --- | --- |
| PackageId treated as bearer token | prohibited; C3 independently authenticates/authorizes |
| application recipient confused with individual principal | explicitly separated; per-principal ownership is STOP/RRI |
| live key policy duplicated in listing | prohibited; historical reference remains visible |
| timestamp-only cursor | prohibited; composite total key required |
| stateless cursor key unavailable | fail closed 503; no unsigned fallback |
| foreign existence/count oracle | exact owner predicate, no lookup/total count |
| new issuance/event subsystem | rejected for v1 |
| C2/C3 mutation from C4 | prohibited |
| delegated/inbox/notification scope | deferred |
| wildcard catalog evidence | prohibited in production and tests |

STOP before expansion if implementation requires C1/C2/C3 semantic redesign,
PackageId bearer authority, CEK/private key/Raw BIO access, S3 access, delegated
recipient, notification/inbox subsystem, recipient-key management, production
raw-source adapter, mutable package fields, a reference table/event, or an
unbounded/non-owner-leading listing query.

## 18. TIP analytical summary / intent ledger

### Intent

Make landed C2 package references reachable to the exact authenticated direct
recipient while keeping C3 as the sole download authority.

### Accepted decisions

1. Existing `PackageId` is the opaque reference.
2. Package owner is `RecipientClientApplicationId`; `PrincipalId` is actor and
   cursor binding.
3. One GET listing route; no exact lookup, total count or inbox.
4. Eligibility is exact `Finalized` plus non-null finalization time.
5. Live key revocation does not hide a historical reference.
6. Projection-only; no durable issuance/audit state.
7. Descending `(FinalizedAtUtc, PackageId)` keyset order.
8. Fifteen-minute stateless authenticated cursor bound to client, principal and
   query profile.
9. One fifth DB/SQL capability and LOGIN; S3 credential count unchanged.
10. Real-C2-produced package proof is mandatory.
11. Package ownership is client-application scoped. Sibling principals with the
    required scope may independently list the same application-owned PackageId,
    but their principal-bound cursors are not interchangeable.

### Rejected/deferred branches

- separate DeliveryReference identifier;
- offset pagination;
- per-principal ownership without predecessor data;
- current-key join for visibility;
- durable list/read/unread events;
- package lookup/search, notification and inbox UX;
- delegated recipient;
- recipient-key management and production raw-source adapter.

### Debt disposition

`C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` is
`ASSIGNED_TO_TIP_88C1_C4`. It remains open through scope, dispatch and build.

It closes only when exact-byte independent closeout evidence proves:

1. authenticated rightful recipient obtains the real-C2-produced PackageId;
2. a foreign `ClientApplicationId` cannot obtain the reference, and a sibling
   principal cannot reuse another principal's cursor;
3. sibling principals under the same authenticated `ClientApplicationId`, each
   with the required scope, may independently list the same application-owned
   PackageId without cursor sharing being accepted;
4. selected PackageId reaches real C3, which independently re-authenticates and
   authorizes;
5. real-C2 ownership mutations discriminate correctly;
6. no new issuance state, download authority, inbox/delegation or S3 authority
   entered C4;
7. controlled commit review passes on the exact implementation bytes.

### Remaining product/pilot gaps after C4

- production Raw BIO source adapter and end-to-end real-source evidence;
- recipient-key operational enrollment/rotation/revocation management;
- delegated delivery, notification/inbox UI and package lifecycle UX;
- `PER-PRINCIPAL-RECIPIENT-OWNERSHIP` =
  `NOT_LANDED / REQUIREMENT_DEPENDENT`: C2 persists client-application
  ownership only. If a hospital pilot requires patient/user-level package
  isolation inside one
  client application, C4 must STOP/RRI for a predecessor identity-model
  expansion rather than infer ownership from the authenticated principal;
- deployment/activation and hospital-pilot operational authorization.

C4 closes reference reachability only. It does not by itself make the product
pilot-ready.

## 19. Three-round convergence boundary

```text
Round 1: scope/trust/product boundary
Round 2: executable dispatch
Round 3: exact-byte reconciliation only
```

Unresolved Round-1 product decisions may not be deferred silently into Round 2.
After Round 3, executable uncertainty moves to code and discriminating tests
unless a genuine contract contradiction requires Homeowner adjudication.

## 20. Round-1 reviewer questions

Reviewers must answer explicitly:

1. Is C4 bounded to reference reachability rather than download authority?
2. Can existing `PackageId` safely serve as the opaque reference?
3. Is package ownership correctly defined as client-application ownership, and
   is the principal/application distinction acceptable for the direct slice?
4. Are exact `Finalized`/non-null predicates sufficient?
5. Is historical visibility under later key revocation consistent with C3 D2?
6. Can foreign package presence, absence or count influence public output?
7. Is `(FinalizedAtUtc, PackageId)` a total order and is null handling closed?
8. Does the cursor bind every required actor/query field and fail closed?
9. Is projection-only operation safe without durable issuance state?
10. Is no durable listing audit the correct bounded decision?
11. Is any C2/C3 schema/API amendment beyond a supporting index necessary?
12. Does any language accidentally introduce inbox/notification UX?
13. Is delegated delivery fully excluded?
14. Are all four census families pinned independently and exact?
15. Is the real-C2-data proof unavoidable and non-vacuous?
16. Are the closure conditions for the C3 debt sufficient?
17. Which production/pilot gaps remain after C4?
18. Are absent-cursor and continuation-page-size semantics unambiguous across
    the route contract, cursor codec and public precedence?
19. Does every claim distinguish client-application package ownership from
    principal-bound cursor authority, including sibling-principal behavior?

## 21. Explicit boundaries and authorization state

This scope candidate authorizes no implementation, migration, test execution,
provider operation, stage, commit, push, merge, PR, deployment, production
activation, real Raw BIO, package download or delivery.

Current state:

```text
C4 Scope v0.2:                         ROUND_1_CLOSURE_CANDIDATE
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1:  ASSIGNED_OPEN
Independent review of v0.1:            RECONCILED — THREE FINDINGS ACCEPTED
Independent closure review of v0.2:    PENDING
Homeowner scope ratification:          NOT_RECORDED
Round-2 dispatch authority:            NONE
Implementation authority:              NONE
```
