# TIP-88C1-C5 — Managed Recipient Enrollment & Activation Readiness Scope Brief

Version: `0.3`
Status: `ROUND_1_CLOSURE_SUCCESSOR_READY_FOR_REVIEW`
Date: `2026-08-23`
Baseline commit: `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`
Authority: `HOMEOWNER_C5_ROUND1_AUTHORITY_AND_AMENDMENT`
Authority record SHA-256: `0709C9DAC7845A9010E293E6A0EF9DD53F99F6C515A75C669CD96B3757565DAE`
Authoring mode: `DOCS_ONLY_WITH_AUTHORIZED_P0_CORRECTION`
Implementation authority: `NONE`

## 0. Changelog and C5-P0 pre-scope correction

### v0.3 — Round-1 closure successor

- Qualifies the post-lock-clock rule as a C5-owned operation rule rather than
  incorrectly describing landed C2/C3 as compliant with it.
- Records `C5-C2-PRELOCK-CLOCK-NONCLAIM-01`: landed C2 captures its admission
  clock before the recipient-key `FOR UPDATE` and later uses it for the key
  validity-window check; C5 does not retrofit that predecessor behavior.
- Requires the Round-2 Dispatch to prove planned rotation never waits while
  holding the recipient-key row lock and to promote the non-claim to a defect
  if C5 introduces a longer hold that invalidates the bounded-exposure claim.
- Preserves the v0.2 product/trust decisions and closes the exact
  three-artifact review-chain gate in the v0.3 successor bundle.

### v0.2 — Round-1 review reconciliation

- Makes the stable PrincipalId authoritative on the managed recipient identity,
  not independently selectable on each replacement credential.
- Separates the exact two-scope recipient activation profile from the broader
  client policy so C5 does not remove unrelated business/session capabilities.
- Rejects historical SPKI/fingerprint reuse because the landed registry has a
  recipient-wide historical fingerprint uniqueness constraint.
- Adds the third C2 rotation interleaving: select K1, rotate, then reserve K1
  returns `Unavailable`; retry/reselection is explicit rather than automatic.
- Pins one-time credential response-loss replay without pretending the secret
  can be reconstructed from its digest.
- Requires planned rotation to drain C3 `Authorized`, `Streaming` and
  `Interrupted` deliveries, while emergency revoke remains fail-closed and may
  strand work that must be exported again.
- Defines recovery after standalone key revocation and corrects the P0 path
  census to three producers, three tripwires and one attributes control path.
- Reconciles all supplied Round-1 findings; exact review-chain closure remains
  pending the missing verbatim GPT artifact.

### v0.1 — Round-1 scope candidate

- Binds C5 to managed recipient authentication identity, exact C3/C4 scope
  provisioning, recipient public-key lifecycle and technical activation
  readiness.
- Keeps C1-C4 package, delivery and reference semantics closed.
- Selects explicit persisted principals, replacement-versioned credentials and
  a no-overlap hard-cutover public-key rotation model compatible with landed
  C2/C3 behavior.
- Selects authenticated OperatorAdmin management routes; BusinessConsumer is
  never management authority.
- Requires durable append-only audit and exact finite readiness ownership.
- Carries operational proof-construction rules into Round 2.

### C5-P0 — platform-independent raw-worktree snapshot tripwires

The baseline committed `TagEkycDbContextModelSnapshot.cs` blob is LF with
SHA-256:

```text
07339BB8E749512EB2F4F17681315B53C6C9C33E6EF3B59C7757F66F8E09232F
```

The pre-correction Windows worktree had CRLF bytes with SHA-256
`5F8653C3D679DBA8EC3D933192BCB4B61E180BB3243953DCED60AAD4E87E8E3C`.
Three absolute tripwires pinned that machine-dependent value.

The authorized correction:

- declares `eol=lf` in `.gitattributes` for the C4 migration, its Designer and
  `TagEkycDbContextModelSnapshot.cs`;
- checks those three paths out as LF without changing semantic text;
- pins all three absolute snapshot tripwires to the LF snapshot hash above;
- leaves raw `File.ReadAllBytes` hashing intact.

Positive control on the corrected bytes:

```text
C201  PASS
R316  PASS
E3 F6 PASS
total 3 passed / 0 failed / 0 skipped
```

This closes `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` only for the three
producer files and three tripwire files named by P0; `.gitattributes` is the
seventh changed control path. It does not claim repository-wide line-ending
hardening. P0 is a separately authorized pre-scope correction; the remainder
of C5 remains docs-only.

## 1. Authority, precedence and source hierarchy

This artifact does not authorize a C5 implementation, migration, API call,
provider action, test edit, stage, commit, push, merge, PR, deployment or
production activation.

Precedence is:

1. current explicit Homeowner authority and amendment;
2. landed C1-C4 contracts at baseline
   `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`;
3. this Scope only after two independent Round-1 reviews and Homeowner
   ratification;
4. a future executable Dispatch under separate Round-2 authority.

Landed anchors inspected during authoring:

| Source | SHA-256 | Scope use |
| --- | --- | --- |
| Homeowner C5 authority record | `0709C9DAC7845A9010E293E6A0EF9DD53F99F6C515A75C669CD96B3757565DAE` | controlling Round-1 and P0 record; original thread message remains highest authority |
| `ApiKeyProvisioningService.cs` | `540A810A10A9099BCD333243BC056228B5AE6AFA95773D87C07FBDFD3552A7D1` | current managed credential writer and gaps |
| `PostgresHashedApiKeyStore.cs` | `96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91` | persisted authentication readback |
| `ApiKeyRow.cs` | `5AED6CEDE08E305A4437E7CF6302A67606B6696463A2F272753E638A470FBC15` | persisted identity/scope fields |
| `LocalDevRuntimePolicySource.cs` | `8C19D7529343B8A27D6DAB73A0E7A0E19C27E97E33019CBB27B4D844B7608475` | only landed allowed-scope policy provider; not canonical production proof |
| `LocalDevApiKeyValidator.cs` | `C9E03F9383CA2991F589ECBA178E2E877231D96DBE7D5D58DE23BC56AC26E9F8` | store-agnostic validator and principal mapping |
| `AuthenticatedClientContext.cs` | `4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401` | authenticated authority object |
| recipient-key row | `8D73CA3C4ED2F2C43C62A5D754918D60EE554DA551EEB6014D7588C6B207E903` | landed key identity/lifecycle |
| recipient-key configuration | `B414030C73FCA2C7EC87B0CA71E1C472F2616A85B65D20020C05A2E00A5A823B` | exact state/validity/one-active constraints |
| C2 package row | `AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E` | frozen key snapshot |
| C3 delivery row | `FAFF3AC460B16C415F238B2E2E7FEFE352484E5F6CCAF704414A4C595A0CB126` | current-key delivery authority |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | reserve/snapshot/Restrict behavior |
| C3 migration | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` | delivery/key Restrict and eligibility |
| C4 migration, LF worktree | `0CDD0B352403A04814579EB57BA0224A23E094A2E30D4F85A646F58C8D020844` | historical reference behavior |
| C4 real-chain proof | `1A92ED6E4A5CCB062F325F6DAB7CD29DEE1E8F8A607739E1370EA5E389BC799B` | C411 C1->C2->C4->C3 evidence |

Unrelated dirty or untracked working-tree files are not C5 authority and are
not silently admitted into a future allowlist.

## 2. Predecessor debt disposition

### 2.1 C4 activation debt assigned, not closed

```text
C4-AUTH-SCOPE-PROVISIONING-DEBT-01
= ASSIGNED_TO_TIP_88C1_C5
= ASSIGNED_OPEN
```

It remains open through Scope and Dispatch. It closes only at controlled C5
closeout when exact managed/non-LocalDev provisioning, authentication and
C3/C4 authorization proofs pass on final bytes.

### 2.2 C3 reachability gap closure comparison

The C4 ledger defined four closure conditions. They compare to landed C4 as
follows:

| Required condition | C4 evidence | Result |
| --- | --- | --- |
| exact implementation | committed C4 route, service, SQL projection, cursor and readiness at `4d5dfe3e...` | satisfied |
| real-C2 ownership proof | C411 uses real C1 output and real C2 Prepare/Finalize; no direct-seeded canonical package | satisfied |
| independent C3 handoff | C411 submits the listed opaque PackageId to real C3, which independently allows an authorized actor and rejects a missing-scope actor | satisfied |
| controlled closeout | C401-C424 `24/24`, C4M01-C4M34 `34/34`, final Release `1158/0/1`, 36-path controlled commit | satisfied |

Only after this comparison, C5 records:

```text
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1
= CLOSED BY TIP-88C1-C4
= commit 4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f
```

C5 does not reopen PackageId discovery, application-level ownership, cursor or
reference-list semantics.

### 2.3 C4 non-claims

```text
C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01
= ASSIGNED_TO_TIP_88C1_C5
= ASSIGNED_OPEN
```

C5 closes it only through the PostgreSQL managed credential path. The following
remain unclosed and are not C5 objectives:

```text
C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01 = CARRIED_OPEN
C4-ROLE-SIBLING-SURFACE-NONCLAIM-01      = CARRIED_OPEN
```

## 3. Landed feasibility findings

### 3.1 Managed authentication path

The existing PostgreSQL path persists and reads:

```text
ApiKeyId
ClientApplicationId
PrincipalId
CredentialRef / CredentialType / CredentialStatus
KeyPrefix / KeyHash
ScopesJson
CallerCategory
ExpiresAtUtc
CreatedAtUtc
```

`PostgresHashedApiKeyStore` reads one row by exact key prefix, compares the
stored hash in fixed time and returns the persisted client, principal, category
and scopes. `LocalDevApiKeyValidator` is store-agnostic despite its name: it
maps the resolved record's `PrincipalId` into `AuthenticatedClientContext`.

Two gaps are real:

1. `ApiKeyProvisioningService` currently writes
   `PrincipalId = command.PrincipalId ?? command.ClientApplicationId`, so an
   omitted principal becomes derived authority.
2. the only `ILocalDevClientPolicyProvider` is an in-memory LocalDev source and
   its allowed-scope set omits both C3 and C4 scopes. Therefore the current
   managed provisioner cannot legitimately issue the exact recipient
   capability pair.

C5 owns the narrow managed-policy/principal correction. LocalDev cannot serve
as the canonical production proof.

The exact current source of each authenticated field is:

| Field | Current provisioning source | Current durable source | Authentication mapping |
| --- | --- | --- | --- |
| `ClientApplicationId` | `ApiKeyProvisioningCommand.ClientApplicationId` | `api_keys.ClientApplicationId` | `PostgresHashedApiKeyStore` -> context |
| `PrincipalId` | optional command value, currently defaulted to client id | `api_keys.PrincipalId` | store value copied unchanged by validator |
| `CallerCategory` | command enum | `api_keys.CallerCategory` text | exact enum parse, unknown fails closed |
| scopes | command set checked against `ILocalDevClientPolicyProvider` | canonical JSON in `api_keys.ScopesJson` | parsed set used for exact required-scope checks |
| credential status/expiry | provisioner defaults/status and command expiry | `api_keys.CredentialStatus` / `ExpiresAt` | resolved for each request |

The C5 correction must preserve this readback chain while replacing the two
non-production authorities: principal fallback and LocalDev-only allowed-scope
policy.

### 3.2 Exact landed recipient scopes

```text
C3: business.raw-export.package.download
C4: business.raw-export.package.references.read
```

Both landed application services require `BusinessConsumer`, exact scope and a
stable non-empty principal. C4 package ownership stays
`RecipientClientApplicationId`; C3/C4 actor/cursor/receipt identity stays
`PrincipalId`.

### 3.3 Recipient-key registry and cross-slice satisfiability

The landed registry key is:

```text
(RecipientClientApplicationId, RecipientKeyId, RecipientKeyVersion)
```

It stores RSA-OAEP-256 SPKI, SHA-256 fingerprint, validity window, state,
revision, registration time and revocation time. Schema admits only `Active`
and `Revoked` and has a partial unique index allowing at most one Active row per
recipient.

The executable C2 profile imports the full DER SubjectPublicKeyInfo, requires
complete consumption, requires an RSA key size in `3072..4096` bits and wraps
the CEK with OAEP-SHA256. C5 enrollment and readiness use that exact profile;
accepting a smaller RSA key because it merely fits the schema byte-length range
is forbidden.

Landed C2 and C3 carry `Restrict` foreign keys to the exact registry identity.
C2 locks and revalidates the selected Active key, recomputes the SPKI digest,
then freezes key identity/version/fingerprint/SPKI/revision/validity into the
package. C3 locks and revalidates that same frozen key identity both at new
delivery admission and when an `Authorized`/`Interrupted` delivery begins or
retries streaming. C4 C410 deliberately does not join live key state.

The selected hard-cutover model below is therefore satisfiable without changing
C2/C3 interpretation:

- rotation atomically revokes K1 and activates K2;
- C2 may freeze K1, freshly select K2, or return stale-selection `Unavailable`
  when selection and exact reserve straddle cutover;
- historical package rows remain immutably bound to K1;
- C3 denies new K1 delivery and K1 stream start/retry after revocation;
- C4 continues listing the historical finalized package.

No `STOP/RRI — CROSS-SLICE SEMANTICS REQUIRED` is raised for this selected
model. Overlap, a `Retired` state or delivery-after-revocation would require
cross-slice changes and are rejected for C5.

## 4. Product objective and exact boundary

C5 provides managed technical eligibility for a direct recipient:

```text
authenticated management actor
-> managed recipient policy
-> persisted managed BusinessConsumer credential
-> stable explicit PrincipalId
-> exact C3+C4 scopes
-> recipient public-key enrollment/lifecycle
-> C5 owning readiness GREEN
-> predecessor readiness consumed GREEN
```

C5 owns:

- managed recipient policy and exact allowed scope set;
- managed credential issue/replacement/revocation for a recipient;
- recipient public-key enrollment, hard-cutover rotation and revocation;
- authenticated management authorization and durable audit;
- C5-owned readiness plus aggregate predecessor readiness composition;
- canonical managed-auth end-to-end proof through C1-C4.

C5 does not own:

- production activation, deployment, hospital go-live or pilot approval;
- C1 resolver/assembly behavior;
- C2 CEK, codec, package object, snapshot or re-key behavior;
- C3 stream, integrity, receipt or delivery outcomes;
- C4 reference-list, cursor or ownership behavior;
- private recipient keys or client-side decryption;
- inbox, notification, delegation or per-principal package ownership;
- production Raw BIO adapter or real Raw BIO evidence.

## 5. Managed identity authority

The authoritative chain is:

```text
managed recipient identity (one exact client application -> stable principal)
-> managed recipient policy
-> managed credential record in PostgreSQL
-> persisted ClientApplicationId
-> persisted explicit PrincipalId
-> persisted BusinessConsumer category
-> persisted exact scopes
-> PostgresHashedApiKeyStore
-> store-agnostic validator/authenticator
-> AuthenticatedClientContext
```

Required rules:

- the managed recipient identity row is the canonical authority for the exact
  `(ClientApplicationId, PrincipalId)` pair and carries its own revision/state;
- `PrincipalId` is supplied explicitly when that identity is enrolled, is
  globally non-empty/stable and is not mutable by credential replacement;
- it is non-empty and distinct from `ClientApplicationId` in canonical proof;
- omission never defaults to client identity and never generates a random id;
- every credential version has a guarded/composite binding to that exact
  identity pair; supplying another principal is conflict, not a new actor;
- `ClientApplicationId` remains the C2-C4 package owner;
- `PrincipalId` remains actor, cursor and receipt authority;
- credential material is generated once, returned only in the first successful
  response and only its digest is persisted;
- plaintext credentials are never audited;
- LocalDev records are fixtures only.

If the first successful credential response is lost after commit, an exact
idempotency replay returns durable credential metadata and
`ExistingMatchSecretUnavailable`; it cannot return the plaintext. Recovery is
an explicitly authorized replacement operation with a new idempotency key that
issues a new secret and revokes the orphaned credential. The server never
silently generates a second secret on replay.

This is the intended closure of
`C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01` at build closeout, not at Scope.

## 6. Managed recipient policy and scope provisioning

C5 distinguishes two sets:

1. the broader managed client policy, which may retain independently owned
   business/session scopes required by existing product behavior;
2. the C5 recipient-activation credential profile, whose scope set is exactly:

```text
business.raw-export.package.download
business.raw-export.package.references.read
```

Rules:

- arbitrary scope strings and prefix-based admission are rejected;
- C5 adds exactly the two recipient capabilities to the managed policy without
  deleting or replacing unrelated pre-existing allowed scopes;
- the recipient credential command does not accept an arbitrary scope list; it
  selects the named C5 profile and the server materializes the exact pair;
- a canonical recipient credential carries the exact pair, not a superset;
- existing credential rows are immutable; scope change issues a replacement
  credential version and revokes the predecessor in one management operation;
- revocation is effective for the next authentication because the PostgreSQL
  store resolves current durable state on every request;
- an already-authorized C3 response stream is not retroactively terminated by
  credential revocation; no new C3/C4 request may authenticate with the revoked
  credential;
- scope grant/revoke creates durable audit evidence.

The policy provider used by production authentication/provisioning must be
managed and persisted. Extending only `LocalDevRuntimePolicySource` is not a C5
closure. C5 does not claim ownership of or rewrite the semantics of unrelated
session/business scopes; its exact owned set is the recipient-activation
overlay/profile plus the exact management scope.

## 7. Management API and caller authority

C5 includes a narrow authenticated management HTTP surface rather than an
unauthenticated internal command port. Exact routes and DTOs belong in Round 2,
but all operations share this authority:

```text
CallerCategory = OperatorAdmin
PrincipalId    = stable, non-empty, persisted
Required scope = operator.raw-export.recipient.manage
```

BusinessConsumer, CaptureAgent and TrustedAdapter are denied before repository
mutation. Target recipient identity comes from the command body only after
management authorization; it is never inferred from the management actor.

The first production OperatorAdmin credential is a deployment/bootstrap input,
not recipient evidence and not proof of production activation. Canonical C5
tests authenticate a managed PostgreSQL-backed OperatorAdmin record; they do
not substitute a LocalDev key.

Management responses must not disclose credential hashes, plaintext after the
one-time issue response, private keys, foreign recipient key material or
package metadata. A caller without management authority receives the same
forbidden outcome regardless of target existence.

## 8. Recipient public-key authority

The recipient client application is the authoritative owner. Each record binds:

```text
RecipientClientApplicationId
RecipientKeyId (stable key-family id)
RecipientKeyVersion (initial 1, then strictly increasing within the family)
PublicKeyAlgorithm = RSA-OAEP-256
PublicKeySpki = complete DER SPKI for RSA 3072..4096 bits
PublicKeyFingerprint = SHA256(PublicKeySpki)
State = Active | Revoked
ValidFromUtc < ValidUntilUtc
RegisteredAtUtc
RevokedAtUtc / RevocationReason when Revoked
Revision > 0
```

TagEkyc never receives or stores the private key. Key enrollment is an
administrative attestation that the supplied public key belongs to the target
recipient; it is not proof the recipient can later decrypt a package.

## 9. Enrollment admission

The first key becomes eligible only after the durable enrollment transaction
commits. Admission fails closed for:

- malformed or non-canonical SPKI;
- algorithm other than RSA-OAEP-256, RSA size outside `3072..4096`, trailing
  SPKI bytes or any other profile mismatch;
- fingerprint mismatch against SHA-256 recomputed from SPKI;
- empty owner/key id, nonpositive version or duplicate identity/version;
- duplicate fingerprint for the recipient;
- recipient/client mismatch;
- invalid or already-expired validity window;
- another Active key for the recipient;
- caller-supplied state, revision or timestamps;
- missing authenticated management authority.

Caller-supplied fingerprint is comparison input only. It can never override the
server-recomputed value.

Initial enrollment uses version `1`. Eligibility for new C2 preparation
requires `ValidFromUtc <= admission time` and `ValidUntilUtc >= admission time
+ 5 minutes`, matching the landed C2 reservation gate. A registry row may exist
before its validity starts, but C5 readiness remains not-ready and it is not
Active-selection eligible until this condition holds.

## 10. Rotation decision

C5 selects no-overlap hard cutover because the landed model admits one Active
key and no Retired state.

```text
lock recipient lifecycle authority
-> lock current K1
-> fresh clock
-> revalidate K1 and proposed K2
-> K1 Active -> Revoked
-> insert K2 Active
-> append audit
-> commit = rotation linearization point
```

K2 keeps the stable `RecipientKeyId`, uses a strictly greater version and a new
fingerprint/SPKI. K1 does not remain Active during overlap. A failed transaction
leaves K1 Active and K2 absent; there is no half-rotated state. Rollback means
transactional rollback, not a state transition after commit.

Historical SPKI/fingerprint reuse is prohibited permanently for the recipient.
The landed alternate key is unique on
`(RecipientClientApplicationId, PublicKeyFingerprint)` across all historical
versions, so a later version cannot reuse K1 material. Reuse would require a
cross-slice/schema redesign and is outside C5.

Planned rotation has a mandatory drain gate. After locking K1 and before the
fresh clock/CAS, it must prove that no C3 delivery frozen to K1 is in
`Authorized`, `Streaming` or `Interrupted`. If any exists, rotation returns
`DeliveryDrainRequired` with no key mutation. The operator must allow an active
stream to finish, start/finish an Authorized delivery, retry/finish an
Interrupted delivery, or let the owning C3 lifecycle reach its existing
terminal outcome. C5 does not cancel or rewrite C3 rows.

The key-row lock is shared ordering authority with C3 admission: a C3
CreateDelivery/begin-stream that wins first becomes visible to the drain check;
rotation that wins first prevents a later K1 admission. An already Streaming
response admitted before cutover is not retroactively killed by C5.

Concurrency with C2 package preparation has three observable outcomes:

1. C2 reserves K1 before cutover: the package remains frozen to K1;
2. rotation commits before C2 selection: C2 selects and reserves K2;
3. C2 selects K1, rotation commits, then C2 reserves K1: the landed reserve
   comparator returns `Unavailable` and creates no package row.

For outcome 3, the caller may explicitly retry the same C2 preparation request
after confirming no package row was reserved; the landed provider then performs
a fresh selection and may choose K2. C5 does not make one reserve call silently
substitute K2 and does not alter C2 idempotency/equality semantics.

## 11. Revocation and historical behavior

Revocation linearizes at the transaction commit that changes the exact key from
Active to Revoked and appends audit evidence using one post-lock fresh clock.

- before C2 snapshot admission, revocation makes the key ineligible;
- after C2 snapshot admission, revocation does not rewrite or delete the
  package or its frozen key snapshot;
- C3 rejects a new delivery for revoked K1;
- C3 also returns `Ineligible` when an already `Authorized` delivery has not
  begun streaming, or an `Interrupted` delivery attempts to resume, because the
  stream-admission function revalidates key State, Revision and fingerprint;
- a stream already admitted and actively emitting bytes is not retroactively
  canceled by C5, but a later restart/retry must pass current-key admission;
- C4 historical reference visibility stays unchanged per C410;
- revocation never means package deletion or re-key.

Emergency standalone revoke is allowed without the planned-rotation drain gate
because security revocation must fail closed. The management response must warn
and the audit event must record the count/state summary of affected
`Authorized`, `Streaming` and `Interrupted` deliveries. Work stranded before
completion has no re-key path in C5; recovery is a new export/package flow.

After standalone revoke, recipient activation readiness remains RED until an
explicit `EnrollReplacementAfterRevocation` operation creates the same stable
`RecipientKeyId` at the next greater version with entirely new SPKI/fingerprint
and makes it Active. This recovery is separately idempotent/audited and cannot
reuse any historical fingerprint. It never rewrites packages frozen to K1.

Revocation records reason, management actor, request id, prior revision and
timestamp. Repeating the same exact request returns the original durable result;
a conflicting request returns conflict.

## 12. Credential and encryption-key lifecycles are separate

| Authority | Identity | Consumer | Effect of revoke |
| --- | --- | --- | --- |
| managed API credential | `ApiKeyId` plus version/replacement lineage | authenticator, then C3/C4 authorization | blocks subsequent authentication; does not alter key/package rows |
| recipient encryption key | `(RecipientClientApplicationId, KeyId, Version)` | C2 selection/freeze and C3 current eligibility | blocks new C2 use and may block new C3 delivery; does not hide C4 history |

The operations have separate idempotency keys, revisions, audit events and
timelines. One is never inferred from or used as the identifier of the other.

## 13. Activation readiness ownership

`Activation Readiness` means technical eligibility only. It is not go-live.

C5 selects several owning validators aggregated by a thin C5 readiness surface:

1. managed-recipient policy/credential validator owned by C5;
2. recipient-key lifecycle validator owned by C5;
3. existing C2, C3 and C4 readiness results consumed as predecessor signals;
4. provider/config readiness consumed from existing owners.

C5 does not copy predecessor SQL/catalog logic. It reports component identity
and exact failing component without turning a predecessor failure into a C5
state mutation.

Readiness itself is a live projection, not a durable lifecycle state. C5 does
not persist `Ready`/`NotReady` transitions and does not audit ordinary probe
changes. Durable audit is required for the management operations whose
committed state causes the projection to change.

The management/readiness surface additionally exposes a non-durable
`RotationReadiness` projection with the exact count of K1 deliveries in
`Authorized`, `Streaming` and `Interrupted`. A nonzero count does not make the
recipient generally activation-not-ready; it specifically blocks planned
rotation so an operator cannot rotate blind.

For one recipient, GREEN requires:

- active managed recipient identity and policy with one canonical stable
  PrincipalId;
- exactly one active managed BusinessConsumer credential selected for
  activation, bound to that PrincipalId and carrying the exact activation
  two-scope profile;
- credential not expired or revoked;
- exactly one eligible Active key;
- exact owner, algorithm, SPKI fingerprint and validity invariants;
- audit lineage consistent with current policy/credential/key revisions;
- C2/C3/C4 predecessor readiness GREEN.

`READINESS-OWNERSHIP-RULE-01` applies at production and proof level. Every
C5-owned table, function, role, LOGIN, grant, policy name and readiness item is
enumerated by an exact finite set. `LIKE`, `ILIKE`, regex, prefix/suffix and
open-ended catalog ownership are forbidden.

## 14. Durable audit claims

C5 records append-only events for:

- recipient policy enrollment/change;
- credential issue/replacement/revoke;
- scope grant/revoke as the before/after exact scope-set digest;
- key enrollment/rotation/revoke;
- readiness state changes only if a durable activation decision is introduced.

Each event binds event id, operation id/idempotency digest, authenticated
management `ApiKeyId` and `PrincipalId`, target recipient client id, operation
kind, target identity/version, prior/new revision, one post-lock timestamp,
reason and evidence digest.

Audit never stores plaintext API credentials, private keys, CEKs, package
plaintext or claims that a package was downloaded/decrypted. C3/client evidence
owns those claims.

## 15. Idempotency, concurrency and linearization

Every mutating command uses a caller-provided idempotency key converted to a
durable digest and equality fingerprint.

| Race/replay | Product rule | Linearization authority |
| --- | --- | --- |
| duplicate exact non-secret request | exact durable `ExistingMatch` response | idempotency row/event commit |
| credential issue response lost | `ExistingMatchSecretUnavailable`; metadata only, never regenerated plaintext | credential/idempotency commit |
| recover lost credential response | explicit replacement request issues new secret and revokes orphan | replacement transaction commit |
| same idempotency, different input | conflict, no mutation | durable equality comparator |
| concurrent same-version key enroll | one winner, loser exact replay or conflict | unique identity + guarded transaction |
| rotate vs C2 reserve | K1, K2, or stale-selection `Unavailable`; explicit retry may reselect only when no package row exists | locked key rows + commit |
| revoke vs C2 reserve | reserve-first may freeze K1; revoke-first makes K1 unavailable | locked K1 + commit |
| credential revoke vs C3/C4 auth | completed auth may finish current request; next resolution sees revoke | managed credential row commit |
| scope replacement vs auth | auth resolves either old or new committed state, never a mixture | replacement/revoke transaction commit |

For C5-owned mutating operations, all C5-acquired blocking locks precede one
fresh `clock_timestamp()` admission time. This rule does not retrofit landed
C2/C3 operations; their existing lock/clock semantics remain predecessor-owned
unless separately authorized. C5 authority, validity and deadlines are
revalidated using the same post-lock time before CAS/audit writes.

Round 2 must pin one global lock order compatible with landed C2/C3. A proposed
order that creates an inversion is STOP/RRI.

Round-2 Dispatch must also pin and prove that planned rotation never holds the
recipient-key row lock across any waiting operation. It checks the delivery
drain condition, returns `DeliveryDrainRequired` and releases; it never waits
under that lock. If Round 2 lengthens the lock hold for any reason, the landed
C2 five-minute admission margin ceases to bound the pre-lock-clock exposure and
`C5-C2-PRELOCK-CLOCK-NONCLAIM-01` is promoted to a defect requiring separate
repair authority.

## 16. Canonical end-to-end proof

Future executable evidence must perform:

```text
managed PostgreSQL-backed OperatorAdmin authentication
-> managed recipient policy enrollment
-> persisted managed recipient identity with explicit PrincipalId
-> first BusinessConsumer credential bound to that identity
-> exact C3+C4 scopes
-> authenticate through PostgresHashedApiKeyStore and validator
-> replace credential and prove PrincipalId remains identical
-> simulate lost first response and prove replay exposes no plaintext
-> recipient public-key enrollment
-> C5 readiness GREEN
-> real C1 assembly
-> real C2 package encrypted to the enrolled public key
-> real C4 PackageId listing
-> real C3 authenticated delivery
```

Prohibited substitutes:

- LocalDev recipient or administrator credential as canonical auth evidence;
- direct insertion of a finalized C2 package;
- test-computed substitute package metadata/object;
- direct-seeded key row that bypasses C5 enrollment;
- a fake C3/C4 gateway as the canonical handoff;
- recipient private key handling by TagEkyc.

## 17. Required semantic traces and proof construction

### 17.1 Invariant trace matrix

| Invariant | Input authority | Enforcement boundary | Durable evidence | Required negative control |
| --- | --- | --- | --- | --- |
| principal persisted, non-empty and not derived | managed credential command | C5 admission + DB constraint + auth readback | credential row + audit | omit principal or substitute ClientApplicationId |
| exact recipient scopes only | managed policy exact set | provisioning and next authentication | scope-set digest + credential | add sibling/prefix-lookalike scope |
| one Active key | recipient lifecycle | key admission/rotation transaction | key rows + audit | second Active key |
| SPKI fingerprint truthful | supplied SPKI | server recompute before write | frozen fingerprint | change one SPKI byte, keep claimed fingerprint |
| historical snapshot immutable | committed C2 package | Restrict FK + no update path | package frozen key fields | rotate/revoke and assert package unchanged |
| C3 current eligibility preserved | current key row | landed C3 admission | delivery outcome/event | revoke K1 after package freeze |
| C4 visibility unchanged | package owner/state | landed C4 query | listed PackageId | revoke K1 and require same reference |
| audit actor truthful | authenticated admin context | C5 write transaction | append-only audit | supply body actor different from context |

### 17.2 Ordering graph

```text
authenticate management actor
-> idempotency probe
-> recipient lifecycle authority
-> credential/key rows in deterministic order
-> one fresh clock
-> authority/validity/revision revalidation
-> guarded writes
-> append audit
-> commit
```

Common locks with C2/C3 must retain their relative landed order. The fresh-clock
step shown above governs C5-owned mutations only. Round 2 must enumerate every
lock-bearing operation and every pairwise interleaving.

### 17.3 Mandatory proof-construction rules

Each Round-2 obligation must state:

```text
fixture construction
canonical positive control
one-dimensional mutation
exact named RED discriminator
byte/catalog restore method and canonical restore hash
```

Operational rules:

- `ONE-DIMENSION`: negative and positive controls differ in exactly the target
  dimension; RED names the isolating probe.
- `TWO-STATE`: before comparing states, assert that they genuinely differ in
  the intended dimension.
- `NAMED GATING`: every gating assertion has a bite name; a RED without its
  named bite is a HARD STOP, identical to a false-green.

Tests may not prove behavior by scanning an unrelated declaration, wildcard
census, direct-seeded substitute or generic error alone.

### 17.4 Required future bite families

Round 2 must include at least:

- explicit identity principal vs derived/empty/replacement-drift principal;
- exact two-scope activation profile vs missing/extra/lookalike scope, while an
  unrelated pre-existing policy scope remains unchanged;
- managed PostgreSQL path vs LocalDev substitute;
- BusinessConsumer recipient vs non-BusinessConsumer;
- OperatorAdmin management authority vs BusinessConsumer denial;
- malformed SPKI, fingerprint mismatch and unsupported profile;
- historical fingerprint/SPKI reuse rejection;
- duplicate version/fingerprint and second-active ambiguity;
- all three rotation/C2 outcomes, including stale-selection `Unavailable` and
  explicit retry/reselection;
- planned-rotation drain with genuinely different outstanding-delivery states;
- planned rotation returns `DeliveryDrainRequired` and releases without any
  wait while holding the recipient-key row lock;
- revocation-before/after C2 reserve;
- Authorized/Interrupted C3 ineligibility after revoke and active-stream
  non-retroactivity;
- standalone-revoke replacement recovery to readiness GREEN;
- credential lost-response replay with zero plaintext and explicit replacement;
- historical package unchanged, C3 denied, C4 visible after revocation;
- exact audit actor/action/revision/time/evidence;
- exact readiness ownership with real sibling catalog objects;
- real C1->C2->C4->C3 chain.

## 18. Debt and non-claim register

| Identifier | C5 disposition | Closure condition/state |
| --- | --- | --- |
| `C4-AUTH-SCOPE-PROVISIONING-DEBT-01` | assigned to C5 | `ASSIGNED_OPEN`; managed credential closeout only |
| `C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01` | assigned to C5 | `ASSIGNED_OPEN`; PostgreSQL chain proof only |
| `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` | predecessor closure | `CLOSED BY TIP-88C1-C4` after four-row comparison |
| `C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01` | not C5 objective | `CARRIED_OPEN` |
| `C4-ROLE-SIBLING-SURFACE-NONCLAIM-01` | not C5 objective | `CARRIED_OPEN` |
| `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` | P0 narrow closure | three producers + three tripwires; `.gitattributes` is the seventh control path; broader debt carried |
| `C5-C2-PRELOCK-CLOCK-NONCLAIM-01` | predecessor behavior exposed by C5 contention | landed C2 captures `clock_timestamp()` before its blocking recipient-key `FOR UPDATE` and then uses that timestamp for key validity-window admission; C5 does not retrofit or repair it. Rotation makes the wait reachable, currently bounded by a short C5 lock hold and the existing five-minute margin. Repair is predecessor-owned and requires separate authority; any longer C5 hold promotes this to a defect |
| `SHARED-DB-MIGRATION-STATE-DEBT-01` | harness pre-control | disposable DB; no shared-template Down |
| `READINESS-OWNERSHIP-DEBT-01` | exact-set rule | carried; C5 bound by RULE-01 |
| `R2-OBS-DEBT-01` | predecessor owner | carried open |
| `R4R6-DURABLE-O-T15-QUARANTINE-EVIDENCE` | predecessor owner | carried open |
| `TIP68-HOST-STARTUP-ROOT-CAUSE-01` | separate owner | mitigated, root cause unproven |

## 19. Product readiness remaining after C5

C5 closeout alone is not Product Ready. At least these remain:

1. production Raw BIO source adapter plus real-source end-to-end evidence;
2. deployment/hospital-pilot activation packet, including bootstrap operator
   credential, provider secrets, operational monitoring and rollback.

Real recipient/hospital activation is prohibited until those gates close.

## 20. TIP analytical summary / intent ledger

### Intent

Close the managed recipient identity, exact-scope, public-key lifecycle and
technical readiness gap without changing C1-C4 product semantics.

### Accepted decisions

- PostgreSQL managed recipient identity owns one explicit stable principal,
  distinct from client identity and immutable across credential replacement.
- recipient activation credential scope set is exactly C3+C4 and
  replacement-versioned; broader client-policy scopes remain independently
  owned and unchanged.
- management is authenticated OperatorAdmin with one exact management scope.
- public-key lifecycle is Active/Revoked with no-overlap hard cutover,
  permanent historical fingerprint non-reuse and a planned-rotation drain gate.
- emergency revoke may strand pre-stream/retry delivery and requires a new
  export; replacement enrollment with new material restores readiness.
- C2 rotation concurrency admits K1, K2 or stale-selection `Unavailable`;
  reselection is an explicit retry, never silent substitution.
- lost credential issue responses cannot replay plaintext; recovery replaces
  and revokes the orphan credential.
- rotation/revocation preserve C2 snapshots, C3 current eligibility and C4
  historical visibility exactly as landed.
- C5 readiness aggregates owning validators instead of duplicating predecessors.
- audit is durable, append-only and actor-bound.

### Rejected/deferred branches

- LocalDev as canonical proof;
- arbitrary/prefix scope policy;
- mutable-in-place credentials;
- overlapping Active keys or new Retired state;
- package re-key/replacement/deletion on revocation;
- BusinessConsumer management authority;
- private-key/decryption handling;
- per-principal package ownership, inbox, notification and delegation.

### Risks

- the current provisioner defaults missing principal to client identity;
- the current production registration uses an in-memory LocalDev policy source;
- no landed key-management/audit operation exists;
- C2/C3 lock compatibility must be proven executable, not inferred;
- landed C2 uses a pre-lock admission clock for a post-wait key-validity check;
  C5 must keep its recipient-key lock hold short and non-waiting unless separate
  predecessor repair authority is granted;
- bootstrap OperatorAdmin remains deployment-owned.

## 21. Three-round documentation and review-artifact protocol

```text
Round 1  Scope
Round 2  executable Dispatch
Round 3  exact-byte reconciliation only
```

After Round 3 PASS, uncertainty moves to code and discriminating tests. No
Round 4 exists without explicit Homeowner reopening.

Every C5 review successor bundle must contain exact verbatim artifacts for both
reviewers under `reviews/` and bind their SHA-256 in the ledger. This v0.3
closure successor carries the exact GPT v0.1 Round-1 review and the exact GPT
and CC v0.2 closure reviews, in addition to the already-carried exact CC v0.1
review. The prior summary placeholder is retired. Exact-artifact provenance is
complete, but the successor still does not self-grant Round-1 ratification.

## 22. Round-1 reviewer questions

Each reviewer must answer all of the following, not merely confirm the words
exist:

1. Is C5 bounded to managed identity/key lifecycle/readiness?
2. Is the exact persisted managed PrincipalId authority identified?
3. Can the proposed managed policy grant the exact C3/C4 scopes without
   arbitrary scope admission?
4. Is OperatorAdmin with the exact management scope the correct grant/revoke
   authority?
5. Is the recipient-key registry state authoritative and sufficient?
6. Is exactly one Active key, with no overlap, correct?
7. Is rotation linearized at the atomic K1 revoke/K2 activate commit?
8. Is revocation linearized at the guarded state/audit commit?
9. Does revocation preserve historical C2 package binding?
10. Does landed C3 current-key eligibility remain consistent?
11. Does C4 historical visibility remain unchanged?
12. Are auth credential and encryption-key timelines separated?
13. Are audit claims exact and non-overclaiming?
14. Is readiness correctly split into owners plus an aggregate?
15. Does the selected model require any C1-C4 production semantic change?
16. Is the canonical proof genuinely managed/PostgreSQL rather than LocalDev?
17. Does C5 accidentally include deployment or pilot activation?
18. Are the two remaining Product Ready gates explicit?
19. Does the P0 correction preserve raw-worktree hashing while making bytes
    platform-independent?
20. Are ONE-DIMENSION, TWO-STATE and NAMED GATING enforceable in Round 2?
21. Is permanent historical fingerprint/SPKI non-reuse the correct consequence
    of the landed unique constraint?
22. Are all three C2/rotation outcomes and explicit retry boundaries correct?
23. Are credential lost-response and replacement semantics safe and usable?
24. Does managed identity, rather than each credential row, own stable
    PrincipalId?
25. Is planned-rotation drain plus emergency-revoke stranding/re-export an
    acceptable product/operational rule?
26. Is recovery from standalone key revoke sufficient to restore readiness?

## 23. STOP/RRI conditions

STOP before expanding if C5 requires:

- C1-C4 semantic redesign;
- overlap/Retired key state or delivery-after-revocation changes;
- per-principal package ownership;
- delegated delivery, inbox or notifications;
- production Raw BIO adapter;
- hospital deployment/pilot activation;
- TagEkyc private-key/CEK/decryption handling;
- package re-key/replacement;
- an implementation path before Round-2 authority;
- a review successor without both exact reviewer artifacts.

Report `STOP/RRI — CROSS-SLICE SEMANTICS REQUIRED` if a proposed key rule cannot
be satisfied by landed C2/C3. Report `REVIEW_CHAIN_INCOMPLETE` if either review
artifact is unavailable.

## 24. Current authorization state

```text
C5-P0:                         CORRECTED / TARGETED 3 OF 3 PASS / UNCOMMITTED
C5 Scope v0.3:                ROUND_1_CLOSURE_SUCCESSOR_READY_FOR_REVIEW
Technical Gate 1:             CLOSED — C5 RULE QUALIFIED / C2 NON-CLAIM PINNED
Review Gate 2:                CLOSED — 3 REQUIRED EXACT ARTIFACTS CARRIED
Independent Round-1 findings: F01, F03-F08, CC-F01 PRESERVED CLOSED; F02 CLOSED IN V0.3
Homeowner scope ratification: NOT_RECORDED
Round-2 dispatch authority:   NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```
