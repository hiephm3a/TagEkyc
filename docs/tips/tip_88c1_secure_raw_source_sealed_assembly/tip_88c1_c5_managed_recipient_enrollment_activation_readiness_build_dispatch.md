# TIP-88C1-C5 — Managed Recipient Enrollment & Activation Readiness Build Dispatch

Version: `0.6`
Status: `PHASE_B_REVIEW_CANDIDATE`
Date: `2026-08-24`
Baseline commit: `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`
Ratified Scope v0.3 SHA-256: `7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5`
Round-1 ledger SHA-256: `87A68407A4E0AAE5D9EDA99D9B8B2C3C9C6332DE98D3A69CED918D0FC9694CC2`
Round-1 closure bundle SHA-256: `EC98C388AFE96A076FE558B2B59714771828632A89588AD3525B294E9E238DD2`
Round-2 Homeowner authority SHA-256: `0BF276DDDDD756467525748A9E9C91952E8265E085FC205646E75B8FC0EBC0A5`
Authoring mode: `DOCS_ONLY`
Implementation authority: `NONE`

## 0. Changelog

### v0.6 — Phase-B executable register reconciliation

- Phase-B correction R4 completes the exact landed `api_keys` insert projection,
  pins the sole-candidate-conflict index identity, targets the idempotency
  conflict explicitly, and binds every C5-created lifecycle timestamp to the
  one post-lock `AdmissionAtUtc`.
- The provisional operation CHECK now admits the post-admission incomplete
  shape for all seven operation kinds. No finding changes the Phase-A
  denominator, product/trust decision, proof owner, permanent path or mutation
  allocation.
- Reconciles the exact §§2-9 contracts, not only the proof registers: server-
  owned `requiredScope` routes managed authentication; a recipient-scoped
  transaction advisory lock linearizes an absent identity; an uncommitted
  provisional operation claim makes replay-before-generation executable; and
  replay returns only the committed immutable result snapshot.
- Pins auth-before-body endpoint construction, five-conflict credential
  exhaustion, framed colon-safe audit targets, exact revision authority,
  before/after scope digests and the complete C502/C508/C526 fixtures.
- Incorporates by exact path and SHA the ratified Phase-A denominator:
  279 requirements, 156 named observations, 77 discriminators, 79 controls,
  78 allocated mutation dimensions, 30 proof owners and 47 permanent paths.
- Replaces the v0.5 proof-taxonomy and mutation tables with the exact finite
  R8 register artifacts named in §19. The earlier tables remain historical
  predecessor text and are non-normative wherever §19 differs.
- Preserves C5M65 and C5M78 as `CONTROL / NOT_ALLOCATED` and binds their sole
  durable enforcement to exact constraint
  `ck_raw_export_recipient_key_registration_shape` under C529.
- Adds a fresh Dispatch-v0.6 Task-0 gate. The verifier reads these v0.6 bytes,
  resolves every incorporated artifact by its pinned SHA and recomputes the
  complete denominator instead of inheriting the v0.5/Phase-A result.
- Changes no product/trust decision, permanent path, proof owner, source/test/
  migration or executable byte. The durable-model and codec text changes are
  the count-neutral exact-contract realization of already-ratified Phase-B
  requirements; they do not authorize implementation.

### v0.5 — Round-2 proof-taxonomy and final residual successor

- Routes an OperatorAdmin credential through persisted C5 authority when its
  scope set contains the exact management scope; unrelated admin scopes are
  permitted and do not cause LocalDev fallback. Recipient credentials retain
  exact two-scope equality.
- Splits C511 into canonical real-function identity-lock serialization and an
  independent disposable-catalog partial-index discriminator for C5M17.
- Classifies every named §12 observation as `DISCRIMINATOR` or `CONTROL`, maps
  every discriminator to §13 and records the reason for every control. A later
  successor cannot add an unclassified or mutation-orphaned named bite.
- Reassigns C5M08 count-neutrally to the generic activation-writer companion
  gate so that property has a direct discriminator.
- Keeps the authoritative census at 47 paths / 30 proofs / 48 mutations and
  changes no durable model, product/trust decision, codec or vector.

### v0.4 — Round-2 credential-routing and residual mutation successor

- Replaces the client-id-only authentication policy adapter with an exact
  resolved-credential router inside the allowlisted authenticator composition.
  C5 recipient/admin credentials use persisted C5 authority with zero LocalDev
  calls; an unrelated credential under the same client uses the exact landed
  global policy path and retains both allow and deny behavior.
- Strengthens C504/C505 with same-client Active/Disabled policy controls and
  explicit credential classification before policy selection.
- Corrects C5M04/M16/M23/M31 so SQL uniqueness or another enforcement layer
  cannot mask the intended named discriminator.
- Keeps the authoritative census at 47 paths / 30 proofs / 48 mutations and
  changes no Round-1 product/trust decision, durable model, codec or vector.

### v0.3 — Round-2 proof and provenance successor

- Corrects the append-only ledger mechanically so the complete v0.1 ledger
  SHA `0FD72FF4...6DDD8` is a true exact byte prefix before §22.
- Scopes the C5 persisted-policy adapter only to the `IApiKeyAuthenticator`
  construction; unrelated `ILocalDevClientPolicyProvider` consumers retain
  landed behavior. Activation-scope keys without a C5 companion fail closed.
- Strengthens C504/C505 with same-client behavioral preservation and generic-
  provisioner alternate-writer rejection controls.
- Corrects C5M11/M12/M14/M24/M28/M32/M35/M40 so every mutation has an actual
  input/surface, isolates neighboring enforcement and can RED at its named bite.
- Keeps the authoritative census at 47 paths / 30 proofs / 48 mutations and
  changes no product/trust decision, durable model, codec or absolute vector.

### v0.2 — Round-2 review reconciliation successor

- Closes `C5-R2-CC-F01` by identifying all eight landed C2/C3/C4 test writes
  that must supply `RevocationReason`; the new sparse CHECK remains strict and
  C529 owns the predecessor regression discriminator.
- Closes `C5-R2-GPT-F01` without path 48: a C5 managed-policy adapter in the
  already-allowlisted service-registration path resolves persisted C5 policy
  first, never calls LocalDev for a C5 identity, and preserves LocalDev fallback
  only for non-C5 identities.
- Closes `C5-R2-GPT-F02` by pinning a durable emergency-revoke result/count
  summary in operation and audit rows; exact replay returns the committed
  counts rather than re-reading mutable C3 delivery state.
- Closes `C5-R2-GPT-F03/F04/F06/F07` by removing unratified global PrincipalId
  uniqueness, pinning the required `api_keys` composite alternate key, defining
  a feasible replacement write order, defining the dedicated manager
  connection/secret contract, and enumerating key/audit vocabularies.
- Closes `C5-R2-GPT-F05` count-neutrally: every C501-C530 owner now contains the
  negative state needed by its comparator and every C5M01-C5M48 row names one
  edit, one fixture dimension and one RED bite.
- Keeps the census at 47 paths / 5 tables / 6 indexes / 9 functions / 30 proofs
  / 48 mutations. C1-C4 production semantics remain closed.

### v0.1 — Round-2 executable candidate

- Converts ratified C5 Scope v0.3 into exact management routes, DTOs,
  persistence, SQL, roles, lock order, readiness, audit and replay contracts.
- Keeps C1-C4 semantics byte-closed while making managed PostgreSQL identity,
  credential and public-key lifecycle executable.
- Pins five new C5 tables, one additive column on the landed recipient-key
  registry, nine owned SQL functions, one capability/LOGIN pair and
  six supporting indexes.
- Pins a 47-path permanent allowlist, 30 proof owners (`C501-C530`) and 48
  one-dimensional mutations (`C5M01-C5M48`).
- Carries disposable-database, pre-Up LOGIN, LF and all three raw-worktree
  ModelSnapshot tripwires in the initial allowlist.
- Makes every proof declare its fixture, positive control, mutation, named RED
  discriminator and byte/catalog restore. Wrong-reason or anonymous RED is a
  HARD STOP.

## 1. Authority, precedence and non-authorization

Precedence is:

1. current explicit Homeowner instruction;
2. ratified C5 Scope v0.3 at
   `7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5`;
3. landed C1-C4 contracts at baseline
   `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`;
4. this Dispatch only after two independent reviews and Homeowner controlled-
   build ratification.

This candidate grants no implementation, migration execution, API/provider
operation, test execution, stage, commit, push, merge, PR, deployment,
production activation or real Raw BIO authority.

Round-1 product/trust decisions are closed. In particular, C5 uses one stable
managed PrincipalId, exact C3+C4 activation scopes, Active/Revoked hard
cutover, planned-rotation drain, emergency-revoke stranding, historical
fingerprint non-reuse, one-time credential secret semantics and unchanged
C2/C3/C4 interpretation.

The following remain open through Dispatch:

```text
C4-AUTH-SCOPE-PROVISIONING-DEBT-01          ASSIGNED_OPEN
C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01   ASSIGNED_OPEN
C5-C2-PRELOCK-CLOCK-NONCLAIM-01            OPEN / PREDECESSOR_OWNED
```

## 2. Task-0 gates and landed feasibility

### 2.1 Repository and review chain

Before a future controlled build:

```text
HEAD   = 4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f
branch = tip-88a-raw-export-policy-catalog-build
staged paths = 0
```

Verify the four hashes in this header and that Scope v0.3 is byte-identical.
The v0.1 Round-2 reviews supplied by the Homeowner are stored verbatim as:

```text
reviews/CC_C5_DISPATCH_V0_1_ROUND2_REVIEW.md
  B3FEA541BAC2C2599405C8FF8B17BA179778371692C9D56F3259E8E7C295A455
reviews/GPT_C5_DISPATCH_V0_1_ROUND2_REVIEW.md
  0685D14704260BF60B145B6BB3C13A56CB2DAA20BAFD1915E3893909CD4958D9
```

They are predecessor reviews for the v0.2 successor, not PASS artifacts for
v0.2. The exact v0.2 successor reviews supplied by the Homeowner are stored
verbatim as:

```text
reviews/CC_C5_DISPATCH_V0_2_ROUND2_SUCCESSOR_REVIEW.md
  94E68660C95FBB7A21BDADA4F3FFE07750521BD71181A73E166EE3B7A367F859
reviews/GPT_C5_DISPATCH_V0_2_ROUND2_SUCCESSOR_REVIEW.md
  ADBF15C76AE9E116DFA40FC6CB189DC7BCC25E8DB76E85D9BEBDBDC8997FDBF1
```

The CC review is `PASS`; the GPT review is `ROUND_2_CHANGES_REQUIRED` and owns
the three corrections reconciled in v0.3. They are predecessor reviews for
the v0.3 successor, not PASS artifacts for v0.3. The exact v0.3 successor
reviews supplied by the Homeowner are stored verbatim as:

```text
reviews/CC_C5_DISPATCH_V0_3_ROUND2_SUCCESSOR_REVIEW.md
  A3CA670DD35BF8CC911390D4C8231CC9ECA12B0008D5EE2B67F9FEA116D890D4
reviews/GPT_C5_DISPATCH_V0_3_ROUND2_SUCCESSOR_REVIEW.md
  78DA63FA4C1197E2B53F60DF8D529E202E9F650270A2B974BEB15280EEB9A2FA
```

The CC review is `PASS`; the GPT review is `ROUND_2_CHANGES_REQUIRED` and owns
the two residual corrections reconciled in v0.4. They are predecessor reviews
for the v0.4 successor, not PASS artifacts for v0.4. The exact v0.4 reviews
supplied by the Homeowner are stored verbatim as:

```text
reviews/CC_C5_DISPATCH_V0_4_ROUND2_REVIEW.md
  84E77FEB491F2CCC86FEE147CF071C0700D31BD98A1AFACDE259AB7C62607F8E
reviews/GPT_C5_DISPATCH_V0_4_ROUND2_REVIEW.md
  0728CC865FE3D0E40DFAF1BC4E45ECCA292870339A76710BDB53BD4256680BF8
```

Both reviews require Round-2 changes. They own the proof taxonomy,
admin-superset routing and C5M17 corrections reconciled in v0.5. They are not
PASS artifacts for v0.5. Both exact v0.5 successor reviews must be supplied
and carried before Round 3; missing either is `REVIEW_CHAIN_INCOMPLETE`.

### 2.2 Concrete managed-auth chain

The build must preserve this production chain:

```text
api_keys row
-> PostgresHashedApiKeyStore exact-prefix lookup + fixed-time digest compare
-> ResolvedApiKey persisted ClientApplicationId / PrincipalId / category / scopes
-> store-agnostic credential-aware C5 authenticator
-> AuthenticatedClientContext
```

The canonical recipient path must not call the landed generic provisioner's
`PrincipalId ?? ClientApplicationId` fallback and must not consult
`LocalDevRuntimePolicySource`. C5 writes the managed row only through its own
guarded repository/function path and authenticates it through the chain above.

The PostgreSQL store gains one narrow activation-scope gate. If an `api_keys`
row contains either exact C3/C4 activation scope, authentication requires:

```text
CallerCategory = BusinessConsumer
scope set = exact two-scope C3C4RecipientV1 set
matching Active C5 identity + policy + credential companion
(ClientApplicationId, PrincipalId, ApiKeyId) equality
```

Thus a generic provisioner cannot create a usable C5 activation credential
without the companion. A same-client credential containing no C5 activation
scope retains landed behavior; unrelated scopes and policy capabilities are
not shadowed. Prefix-lookalikes do not enter this branch. No client/principal
fallback is added.

The existing `LocalDevApiKeyValidator` remains byte-unchanged and
store-agnostic. `Program.cs` keeps the landed global registration
`ILocalDevClientPolicyProvider -> LocalDevRuntimePolicySource` for
`VerificationSessionApplicationService`, `VerificationEvidenceApplicationService`,
`ApiKeyProvisioningService` and every other unrelated consumer. It does not
replace that registration.

`Program.cs` replaces only the `IApiKeyAuthenticator` registration with
`C5CredentialAwareApiKeyAuthenticator`, declared in that same allowlisted API
composition file and consuming the C5 services registered by the allowlisted
`RecipientManagementServiceCollectionExtensions.cs`. Infrastructure does not
reference the API assembly. The authenticator owns no new durable authority.
It performs the landed header extraction, resolves the presented
credential exactly once through `IApiKeyStore`, applies the landed null,
status, expiry, required-scope and context-mapping rules, and selects policy
authority from the resolved credential rather than ClientApplicationId alone.

Credential classification is exact and precedes policy lookup. It uses the
server-owned `requiredScope` passed by the endpoint/application call; that
value is never read from route, query, header or body data:

1. `BusinessConsumer` plus the exact two-scope `C3C4RecipientV1` credential set
   is a C5 recipient activation credential only when `requiredScope` is exactly
   `business.raw-export.package.download` or
   `business.raw-export.package.references.read`. The PostgreSQL store has
   already required the matching Active C5 identity/policy/credential companion
   and exact `(ClientApplicationId, PrincipalId, ApiKeyId)` binding. The
   authenticator reads that persisted C5 authority and makes zero LocalDev
   policy calls.
2. `OperatorAdmin` whose scope set contains the exact string
   `operator.raw-export.recipient.manage` is the C5 management credential only
   when `requiredScope` equals that exact management scope. Unrelated
   OperatorAdmin scopes are permitted and preserved. The authenticator
   validates its persisted active, unexpired `api_keys` authority and makes
   zero LocalDev policy calls.
3. Every other resolved credential, including an unrelated credential under a
   ClientApplicationId that also owns a C5 identity, calls the unchanged global
   `ILocalDevClientPolicyProvider` exactly as the landed validator does. C5
   identity/policy state neither enables nor disables that credential.

Classification is by exact category, the relevant exact-set/containment rule
above and exact server-owned `requiredScope`, never prefix, client-id existence
or caller-supplied route metadata. An activation-scope row without the matching
C5 companion is rejected by
`PostgresHashedApiKeyStore` before authenticator policy routing. Prefix-
lookalikes remain unrelated; any row containing an activation scope but not the
exact pair fails the store gate and cannot gain either C5 or fallback authority.

The authenticator's non-C5 branch must be behaviorally equivalent to the landed
validator path: the same global policy object controls it, the same public
error/status precedence applies, and the resolved credential maps the same
persisted fields into `AuthenticatedClientContext`. C504 establishes two
same-client before/after controls: a broader global policy Disabled remains
denied after C5 enrollment, and a broader global policy Active retains the
same successful outcome/context shape. C505 uses a counting LocalDev trap and
requires zero calls for canonical C5 recipient and OperatorAdmin credentials,
including an OperatorAdmin carrying one unrelated sibling scope, while an
unrelated same-client credential makes exactly one global-policy call.

### 2.2.1 Dedicated manager connection and secret authority

The exact configuration section is
`TagEkyc:RawExport:RecipientManagement`:

```text
Topology                    Disabled | PostgresDurable
DatabaseConnectionString    development/test only
DatabaseConnectionStringSecretRef production required
```

The login role is not configurable: it is exactly
`tagekyc_raw_export_recipient_manager_login`. In production, plaintext
`DatabaseConnectionString` is forbidden and the secret reference is resolved
through landed `SecretRefResolver` (`env:`/`file:`); missing or invalid secret
maps to `PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CONFIG_INVALID`. In development
or test, a secret ref takes precedence; otherwise the explicit connection
string is permitted.

`RecipientManagementConnectionFactory` opens only this dedicated connection,
runs `SELECT current_user`, and requires the exact login role before any
management SQL. A mismatch maps to
`PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID`. It never falls
back to the ordinary `tagekyc_runtime` DbContext connection. `Disabled` and
syntactically invalid topology short-circuit before secret resolution, DB
connection or service construction. C524 proves the same C5 function is denied
to `tagekyc_runtime` and succeeds through the manager LOGIN.

### 2.3 Landed key/C2/C3 lock feasibility

Landed C2 reserve captures its clock before locking the exact recipient key.
Landed C3 create locks key then package; begin-stream locks key, package, then
delivery. C5 therefore uses this compatible order:

```text
initial completed-operation/idempotency probe
-> if no terminal replay/conflict, recipient-scoped transaction advisory lock
-> idempotency re-probe under that authority
-> management operation claim row
-> managed recipient identity row when present
-> managed credential row OR exact current recipient-key row
-> C3 drain SELECT without FOR UPDATE and without waiting
-> one fresh clock_timestamp() persisted as operation AdmissionAtUtc
-> guarded writes + audit + operation completion
```

Every mutating function first performs one read-only completed-operation probe.
A terminal exact replay or conflict returns without taking lifecycle authority.
When that probe has no terminal result, the function acquires exactly one
transaction-scoped PostgreSQL advisory lock before its mandatory idempotency
re-probe, before inserting the operation claim and before any lifecycle write:

```sql
SELECT pg_catalog.pg_advisory_xact_lock(
  pg_catalog.hashtextextended(
    'tagekyc:raw-export:c5:recipient:' || p_recipient::text,
    0));
```

The domain string, lowercase PostgreSQL UUID text and seed `0` are exact. Hash
collisions may serialize unrelated recipients but cannot weaken correctness.
The lock is acquired for both absent and existing identity paths, before the
fresh clock, claim and row lock, and is released only by transaction commit or
rollback. The initial read-only probe neither creates a claim nor captures the
admission clock. The re-probe under advisory authority closes the race between
that initial probe and lifecycle serialization. First `EnrollRecipient`
therefore owns the same recipient lifecycle key-space even though no identity
row exists. After acquisition, an existing identity is additionally locked
`FOR UPDATE`; an absent identity is inserted only by the advisory-lock holder.
No session advisory lock is used.

C2/C3 do not acquire the C5 advisory, operation or identity locks, so there is
no reverse edge. Every competing C5 operation acquires the same recipient
advisory lock and then locks identity before a credential/key row.
The drain query is a plain MVCC `SELECT count(*) FILTER (...)`; it does not use
`FOR UPDATE`, advisory locks, polling, delay, provider I/O or another service.

The key lock is the linearization bridge:

- C3 admission first: it commits the delivery before rotation acquires K1;
  the post-lock drain query sees it.
- rotation first: it revokes K1 before C3 can revalidate; C3 becomes
  `Ineligible`.
- C2 reserve first: package freezes K1.
- rotation first: fresh C2 selection chooses K2.
- stale C2 selection: exact reserve returns landed `Unavailable`.

If implementation adds any waiting operation after acquiring the key lock,
STOP and promote `C5-C2-PRELOCK-CLOCK-NONCLAIM-01` to a defect requiring
separate C2 repair authority. A test timeout is not a waiver.

### 2.4 Harness pre-controls

The build begins with these controls, already budgeted in the allowlist:

- `PostgresPersistenceFixture` provisions
  `tagekyc_raw_export_recipient_manager_login` before migration `Up` and
  verifies canonical LOGIN attributes; cluster roles are not created inside a
  disposable database.
- migration Down/reapply and catalog negatives use a disposable database cloned
  at the current migration, never the shared template; the exact database is
  dropped in `finally`.
- any temporary cluster role has a name outside every owned namespace, is
  revoked/dropped in `finally`, and absence is asserted afterwards.
- catalog text comparisons normalize CRLF and LF on both sides only; visible
  whitespace, predicates, identifiers and tokens cannot be deleted/collapsed.
- `TagEkycDbContextModelSnapshot.cs` remains `eol=lf`; E3, R316 and C201 pins
  move together to the final raw-worktree SHA after the additive-model proof.
- PostgreSQL harness cleanup remains `docker compose down -v --remove-orphans`;
  direct postgres fixtures retain `--tmpfs /var/lib/postgresql/data --rm`.

## 3. Exact management HTTP surface

All routes are under:

```text
/api/ekyc/raw-export/recipient-management
```

Exact routes:

| Method and route | Operation kind | Success |
| --- | --- | --- |
| `POST /recipients` | `EnrollRecipient` | `201 Created` or `200 ExistingMatch` |
| `POST /credentials/issue` | `IssueCredential` | `201 Created` or `200 ExistingMatchSecretUnavailable` |
| `POST /credentials/replace` | `ReplaceCredential` | `201 Replaced` or `200 ExistingMatchSecretUnavailable` |
| `POST /credentials/revoke` | `RevokeCredential` | `200 Revoked/ExistingMatch` |
| `POST /keys/enroll` | `EnrollKey` | `201 Created` or `200 ExistingMatch` |
| `POST /keys/rotate` | `RotateKey` | `200 Rotated/ExistingMatch` or `409 DeliveryDrainRequired` |
| `POST /keys/revoke` | `RevokeKey` | `200 Revoked/ExistingMatch` |
| `GET /recipients/{recipientClientApplicationId:D}/readiness` | read projection | `200` ready/not-ready projection |

Each POST requires exactly one `Idempotency-Key` header after authentication.
Its value is ASCII `[A-Za-z0-9._:-]{1,128}` with no trimming or alternate
normalization. Missing, empty, duplicate or malformed is request-invalid.

All routes first authenticate and authorize:

```text
CallerCategory = OperatorAdmin
PrincipalId != Guid.Empty
ApiKeyId != Guid.Empty
scope contains exactly required authority:
operator.raw-export.recipient.manage
```

The actor may have unrelated OperatorAdmin scopes; only the required scope is
checked. BusinessConsumer and every other category are denied before parsing a
target body, resolving a repository or probing target existence. Authorized
target-not-found and forbidden are distinct; unauthorized callers receive the
same `403 RAW_EXPORT_RECIPIENT_MANAGEMENT_FORBIDDEN` for every target.

Every POST endpoint is mapped with `HttpContext` and server services only; a
typed request DTO is not a minimal-API handler parameter and automatic body
model binding is prohibited. The endpoint passes the exact server-owned
`operator.raw-export.recipient.manage` `requiredScope` to authentication,
performs the category/scope/identity authorization above, and only then calls
`HttpRequest.ReadFromJsonAsync<TRequest>` exactly once with C5-local
`RecipientManagementJsonOptions`. Those options use the Web naming policy,
`PropertyNameCaseInsensitive=false` and
`UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow`; they are passed
explicitly and do not change global API JSON behavior. Any unknown/unmapped
member, including an alternate-case spelling, is request-invalid after
successful authorization and before repository resolution. An unauthorized
request with malformed or unmapped-member JSON returns the same management-
forbidden response as valid JSON, with body-parser and repository counters both
zero. Moving parsing before authorization is exactly C5M51 and must RED
`C502-AUTHORIZATION-BEFORE-BODY`.

### 3.1 Exact request DTOs

```csharp
public sealed record EnrollManagedRecipientRequest(
    Guid RecipientClientApplicationId,
    Guid PrincipalId);

public sealed record IssueManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    DateTimeOffset? ExpiresAtUtc);

public sealed record ReplaceManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    Guid CurrentApiKeyId,
    long CurrentCredentialRevision,
    DateTimeOffset? ExpiresAtUtc,
    string Reason);

public sealed record RevokeManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    Guid ApiKeyId,
    long ExpectedRevision,
    string Reason);

public sealed record EnrollRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    string PublicKeyAlgorithm,
    string PublicKeySpkiBase64,
    string PublicKeyFingerprintHex,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc);

public sealed record RotateRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int CurrentKeyVersion,
    long CurrentKeyRevision,
    int NewKeyVersion,
    string PublicKeyAlgorithm,
    string PublicKeySpkiBase64,
    string PublicKeyFingerprintHex,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc,
    string Reason);

public sealed record RevokeRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    long ExpectedRevision,
    string Reason);
```

No request accepts scopes, CallerCategory, state, server revision/timestamps,
audit actor, private key, PackageId, delivery action or plaintext credential.
For each forbidden family, an otherwise valid authenticated request carrying a
representative exact member (`Scopes`, `CallerCategory`, `State`, `Revision`,
`OccurredAtUtc`, `ManagerPrincipalId`, `PrivateKey`, `PackageId`,
`DeliveryAction` or `PresentedKey`) must return request-invalid with repository
calls zero. Absence from the DTO is insufficient unless the C5-local unmapped-
member rejection above is active.
Reason is canonical printable UTF-8 after JSON decoding, length `1..128`, no
leading/trailing whitespace and no control character.

### 3.2 Exact response DTOs and public codes

```csharp
public sealed record ManagedRecipientIdentityDto(
    Guid RecipientClientApplicationId, Guid PrincipalId,
    string State, long Revision);

public sealed record ManagedCredentialDto(
    Guid ApiKeyId, Guid RecipientClientApplicationId, Guid PrincipalId,
    int CredentialVersion, string State, long Revision,
    DateTimeOffset IssuedAtUtc, DateTimeOffset? ExpiresAtUtc,
    string? PresentedKey);

public sealed record RecipientPublicKeyDto(
    Guid RecipientClientApplicationId, string RecipientKeyId,
    int RecipientKeyVersion, string PublicKeyAlgorithm,
    string PublicKeyFingerprintHex, string State, long Revision,
    DateTimeOffset ValidFromUtc, DateTimeOffset ValidUntilUtc,
    DateTimeOffset RegisteredAtUtc, DateTimeOffset? RevokedAtUtc);

public sealed record RecipientPublicKeyOperationResultDto(
    RecipientPublicKeyDto Key,
    string? Warning,
    int? AuthorizedDeliveryCount,
    int? StreamingDeliveryCount,
    int? InterruptedDeliveryCount);

public sealed record ManagedRecipientReadinessDto(
    Guid RecipientClientApplicationId, bool Ready,
    IReadOnlyList<string> Codes,
    int AuthorizedDeliveryCount, int StreamingDeliveryCount,
    int InterruptedDeliveryCount);
```

`PresentedKey` is non-null only on the first `Created/Replaced` response of the
transaction that persisted its digest. Every replay returns null with
`ExistingMatchSecretUnavailable`. Hash, SPKI bytes, private material, package
metadata and foreign identity data are never returned.

Key enroll/rotate return a `RecipientPublicKeyOperationResultDto` with null
warning/counts. Emergency revoke returns exact warning
`EXISTING_DELIVERIES_MAY_BE_STRANDED` and three nonnegative counts captured in
the committing transaction. Exact replay returns those original durable
counts; it never re-queries mutable C3 delivery rows.

Public error codes, in precedence order after authorization:

```text
RAW_EXPORT_RECIPIENT_MANAGEMENT_REQUEST_INVALID        400
RAW_EXPORT_RECIPIENT_MANAGEMENT_IDEMPOTENCY_CONFLICT   409
RAW_EXPORT_RECIPIENT_MANAGEMENT_TARGET_NOT_FOUND       404
RAW_EXPORT_RECIPIENT_MANAGEMENT_PRINCIPAL_CONFLICT     409
RAW_EXPORT_RECIPIENT_MANAGEMENT_CREDENTIAL_CONFLICT    409
RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_PROFILE_INVALID    400
RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_CONFLICT            409
RAW_EXPORT_RECIPIENT_MANAGEMENT_DELIVERY_DRAIN_REQUIRED 409
RAW_EXPORT_RECIPIENT_MANAGEMENT_UNAVAILABLE             503
```

`CandidateConflict` is internal. Credential issue/replacement attempts exactly
five independently generated candidates at most. If candidates 1 through 5
all conflict on exact landed unique index `IX_api_keys_KeyPrefix`, the public
result is exactly
`409 RAW_EXPORT_RECIPIENT_MANAGEMENT_CREDENTIAL_CONFLICT`, with no operation,
credential, event or plaintext response committed. Four conflicts followed by
a unique fifth candidate is a success control. No sixth candidate is generated.

Provider/database/cancellation uncertainty is never mapped to not-found,
conflict, an empty readiness success or a second credential issue.

## 4. Canonical hashes, idempotency and audit codecs

All SHA-256 inputs use bytes, not platform text. `LP(x)` is a four-byte signed
big-endian nonnegative length followed by `x`. UUID text is lowercase `N`
format (32 ASCII hex). Integer text is invariant minimal ASCII. Timestamp text
is UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'` (28 ASCII bytes). Hex is lowercase.

```text
IdempotencyKeyDigest = SHA256(
  LP(UTF8("tip-88c1-c5-idempotency-v1")) ||
  LP(ASCII(Idempotency-Key)))
```

Each endpoint defines `CanonicalPayload` as LP concatenation in DTO field
order, excluding server fields and plaintext credential. Nullable expiry is
LP(empty) when null. SPKI is its decoded DER bytes; claimed fingerprint is its
decoded 32 bytes. Then:

```text
PayloadDigest = SHA256(
  LP(UTF8("tip-88c1-c5-payload-v1")) ||
  LP(UTF8(OperationKind)) ||
  LP(CanonicalPayload))

EqualityFingerprint = SHA256(
  LP(UTF8("tip-88c1-c5-equality-v1")) ||
  LP(UUID_N(ManagerPrincipalId)) ||
  LP(UTF8(OperationKind)) ||
  LP(UUID_N(RecipientClientApplicationId)) ||
  LP(HEX(PayloadDigest)))
```

The activation scope-set codec is independent of JSON representation:

```text
ScopeSetDigest(scopes) = SHA256(
  LP(UTF8("tip-88c1-c5-scope-set-v1")) ||
  LP(INVARIANT(scopes.Count)) ||
  CONCAT(LP(UTF8(scope)) for each exact scope in ordinal order))
```

The empty set is count `0` with no following scope field. The C5 activation set
is count `2` followed by exact `business.raw-export.package.download` then
`business.raw-export.package.references.read`. This digest is recomputed by
application and SQL; no JSON text or caller digest is authoritative.

For key events, `TargetIdentityPayload` is:

```text
LP(UTF8("tip-88c1-c5-target-v1")) || LP(UTF8(OperationKind)) ||
LP(UUID_N(RecipientClientApplicationId)) || LP(UTF8(RecipientKeyId)) ||
LP(INVARIANT(CurrentOrOnlyVersion)) || LP(INVARIANT_OR_DASH(NewVersion))
```

`TargetIdentity` is exact ASCII `v1.` plus RFC 4648 base64url of that payload
without `=` padding. `INVARIANT_OR_DASH` is invariant nonnegative ASCII or
exact ASCII `-`. This length framing is authoritative even when RecipientKeyId
contains `:`. Non-key identity/credential events retain the UUID `N` targets
listed in §5.5.

`COUNT_OR_DASH(x)` is invariant nonnegative ASCII when a durable emergency
revoke count is present and exact ASCII `-` otherwise. `DIGEST_OR_DASH(x)` is
lowercase 64-character hex for a present 32-byte digest and exact ASCII `-`
otherwise. Audit evidence is:

```text
AuditEvidenceDigest = SHA256(
  LP(UTF8("tip-88c1-c5-audit-v2")) ||
  LP(UUID_N(OperationId)) || LP(UTF8(OperationKind)) ||
  LP(UUID_N(ManagerApiKeyId)) || LP(UUID_N(ManagerPrincipalId)) ||
  LP(UUID_N(RecipientClientApplicationId)) ||
  LP(UTF8(EventType)) || LP(UTF8(TargetIdentity)) || LP(UTF8(Reason)) ||
  LP(INVARIANT(PriorRevision)) || LP(INVARIANT(NewRevision)) ||
  LP(HEX(PayloadDigest)) ||
  LP(DIGEST_OR_DASH(PriorScopesDigest)) ||
  LP(DIGEST_OR_DASH(NewScopesDigest)) || LP(UTC(OccurredAtUtc)) ||
  LP(COUNT_OR_DASH(AuthorizedDeliveryCount)) ||
  LP(COUNT_OR_DASH(StreamingDeliveryCount)) ||
  LP(COUNT_OR_DASH(InterruptedDeliveryCount)))
```

The server recomputes every digest. Request-provided hashes are comparison
inputs only. SQL recomputes the equality and audit digest independently from
typed arguments; application and SQL values must match or return conflict.
Absolute vectors are pinned in §4.1 and must be independently recomputed in
review and tests.

### 4.1 Absolute vector V1

```text
Idempotency-Key              c5-op-0001
ManagerPrincipalId           30000000-0000-0000-0000-000000000099
ManagerApiKeyId              20000000-0000-0000-0000-000000000099
OperationId                  50000000-0000-0000-0000-000000000001
OperationKind                EnrollRecipient
RecipientClientApplicationId 10000000-0000-0000-0000-000000000011
PrincipalId payload          30000000-0000-0000-0000-000000000011
PriorRevision                0
NewRevision                  1
OccurredAtUtc                2026-08-23T00:00:00.0000000Z
EventType                    ManagedRecipientEnrolled
TargetIdentity               10000000000000000000000000000011
Reason                       MANAGED_RECIPIENT_ENROLLED
PriorScopesDigest            9DB0AFC8FF0AF27FF5CE08AE785783750DC03DB8679D3746DC4049815FF4459F
NewScopesDigest              406372E4455DE83F44404B972F7F9411894D74B84BB5C2AF73C8DEB25A35FE91
IdempotencyKeyDigest         A630818EC496111E4D559929E5DBFC819F559BC3107734AAF988FE7EC94561BC
CanonicalPayload length      72
PayloadDigest                F38F0B3BCCF9C39261B54126C332759F263CE75CE456FA45FB59A0CBB966B623
EqualityFingerprint          13AF13F77DF6717DC287103922E37B95FD0C8F0DF5C1B18B5632D0FE4CF848EA
AuditEvidenceDigest          2928BC24B8CE5B033E15D228D6FD50E7470CF1BB287175C93FE730E16540242A
```

For `EnrollRecipient`, CanonicalPayload is
`LP(UUID_N(RecipientClientApplicationId)) || LP(UUID_N(PrincipalId))`.

The colon-bearing TargetIdentity vector is:

```text
OperationKind                RotateKey
RecipientClientApplicationId 10000000-0000-0000-0000-000000000011
RecipientKeyId               hospital:key:01
CurrentKeyVersion            1
NewKeyVersion                2
TargetIdentity length        141
TargetIdentity               v1.AAAAFXRpcC04OGMxLWM1LXRhcmdldC12MQAAAAlSb3RhdGVLZXkAAAAgMTAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMTEAAAAPaG9zcGl0YWw6a2V5OjAxAAAAATEAAAABMg
```

## 5. Exact durable model

C5 adds five tables and one column; it adds no durable readiness state.

### 5.1 `raw_export_managed_recipient_identities`

```text
RecipientClientApplicationId uuid PK, non-empty
PrincipalId                   uuid NOT NULL, non-empty, != client id
State                         varchar(16) Active|Disabled
Revision                      bigint > 0
CreatedAtUtc                  timestamptz NOT NULL
UpdatedAtUtc                  timestamptz NOT NULL
```

An alternate key on `(RecipientClientApplicationId, PrincipalId)` is the
composite FK target. PrincipalId never changes for that managed recipient.
The same PrincipalId under another ClientApplicationId is not prohibited by
C5; global cross-client PrincipalId uniqueness was not ratified in Scope.

### 5.2 `raw_export_managed_recipient_policies`

```text
RecipientClientApplicationId uuid PK/FK identity RESTRICT
ActivationProfile            varchar(64) = C3C4RecipientV1
ActivationScopesDigest       bytea(32)
State                         varchar(16) Active|Disabled
Revision                      bigint > 0
CreatedAtUtc                  timestamptz NOT NULL
UpdatedAtUtc                  timestamptz NOT NULL
```

The digest is exactly `ScopeSetDigest` from §4 over the ordinal-sorted two-scope
set. The table is a C5 overlay and does not replace or delete broader client-
policy rows.

### 5.3 `raw_export_managed_recipient_credentials`

```text
ApiKeyId                      uuid PK
RecipientClientApplicationId uuid NOT NULL
PrincipalId                   uuid NOT NULL
CredentialVersion             integer > 0
State                         varchar(16) Active|Revoked
Revision                      bigint > 0
IssuedAtUtc                   timestamptz NOT NULL
RevokedAtUtc                  timestamptz NULL iff Active
RevocationReason              varchar(128) NULL iff Active
ReplacedByApiKeyId            uuid NULL, self-FK RESTRICT
```

It has a composite FK to the managed identity pair and a composite FK to a new
alternate key on `api_keys(ApiKeyId, ClientApplicationId, PrincipalId)`. It has
unique `(RecipientClientApplicationId, CredentialVersion)` and a partial unique
index on RecipientClientApplicationId where State=`Active`. Key material,
principal, scopes, category and expiry never mutate; only lifecycle columns
change through guarded functions.

### 5.4 `raw_export_recipient_management_operations`

```text
OperationId                   uuid PK, non-empty
ManagerApiKeyId               uuid non-empty
ManagerPrincipalId            uuid non-empty
IdempotencyKeyDigest          bytea(32)
EqualityFingerprint           bytea(32)
PayloadDigest                 bytea(32)
AdmissionAtUtc                timestamptz NULL until first post-lock capture
OperationKind                 varchar(32), exact seven mutating kinds
RecipientClientApplicationId uuid non-empty
Outcome                       varchar(48) NULL only in uncommitted claim
ResultIdentityRevision        bigint NULL
ResultApiKeyId                uuid NULL
ResultCredentialVersion       integer NULL
ResultKeyId                   varchar(128) NULL
ResultKeyVersion              integer NULL
ResultRevision                bigint NULL
AuthorizedDeliveryCount       integer NULL
StreamingDeliveryCount        integer NULL
InterruptedDeliveryCount      integer NULL
ResultSnapshot                jsonb NULL only in uncommitted claim
CompletedAtUtc                timestamptz NULL only in uncommitted claim
```

Unique `(ManagerPrincipalId, IdempotencyKeyDigest)` is the replay authority.
The exact row CHECK has two shapes:

```text
provisional claim:
  Outcome / ResultSnapshot / CompletedAtUtc and every result/count field NULL
  AdmissionAtUtc NULL before admission; for every OperationKind it may become
  non-null after the owning operation reaches post-lock admission while every
  result/outcome field remains NULL
  for IssueCredential/ReplaceCredential it remains unchanged across
  intermediate CandidateConflict retries

completed:
  Outcome / ResultSnapshot / CompletedAtUtc NOT NULL
  AdmissionAtUtc NOT NULL
  result/count fields satisfy the operation-specific sparse rules
```

The completed-row CHECK implements this exact seven-operation sparse result
matrix. `-` means SQL NULL; `Y` means non-null. A value shown as `1` is the
initial revision of the newly inserted result row, while `r+1` is the committed
post-update revision of the target row.

| OperationKind | Stored Outcome | IdentityRevision | ApiKeyId | CredentialVersion | KeyId | KeyVersion | ResultRevision | A/S/I counts | ResultSnapshot |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `EnrollRecipient` | `Created` | `1` | - | - | - | - | - | -/-/- | Y |
| `IssueCredential` | `Created` | - | Y | Y | - | - | `1` | -/-/- | Y |
| `ReplaceCredential` | `Replaced` | - | new | new | - | - | `1` | -/-/- | Y |
| `RevokeCredential` | `Revoked` | - | target | target | - | - | `r+1` | -/-/- | Y |
| `EnrollKey` | `Created` | - | - | - | Y | `1` | `1` | -/-/- | Y |
| `RotateKey` | `Rotated` | - | - | - | K2 | K2 | `1` | -/-/- | Y |
| `RevokeKey` | `Revoked` | - | - | - | target | target | `r+1` | Y/Y/Y | Y |

For replacement and rotation, `ResultRevision=1` belongs to the newly inserted
credential companion or K2 result respectively; the event's revision pair
continues to bind the predecessor row as specified in §5.5. Every UUID/id and
version cell marked non-null must equal the typed committed response field.
All three count cells are nonnegative together or NULL together. No completed
row can carry a result field outside its row above, and no provisional row can
carry any result field. `ResultSnapshot` must agree field-for-field with this
typed matrix and is not an alternate authority. `ExistingMatch` and
`ExistingMatchSecretUnavailable` are replay mappings over the original stored
outcome; neither is a second stored `Outcome`.

The provisional row exists only inside the owning transaction and can never be
committed. `ResultSnapshot` is server-created canonical JSON for the exact
successful response DTO with `PresentedKey` omitted, including original state,
revision, timestamps, warning and counts. It accepts no caller JSON. Only
completed operations persist; transaction rollback leaves no row. Replay is
deserialized solely from this immutable operation snapshot and never rebuilt
from current identity, policy, credential, key or C3 delivery rows.

`AdmissionAtUtc` is internal operation authority, not a response field. It is
written exactly once from the sole post-lock `clock_timestamp()` call. The own
uncommitted claim makes it reusable by later SQL invocations on the same
connection/transaction after intermediate candidate conflicts. It is never
accepted from the application, never changed once non-null and never added to
`ResultSnapshot`.

The CHECK therefore admits exactly two provisional sub-shapes for every one of
the seven `OperationKind` values: pre-admission with `AdmissionAtUtc` NULL, and
post-admission with `AdmissionAtUtc` non-null. Both retain NULL outcome,
snapshot, completion, result and count fields. It does not limit the
post-admission sub-shape to credential operations.

### 5.5 `raw_export_recipient_management_events`

```text
ManagementEventId             uuid PK, non-empty
OperationId                   uuid UNIQUE/FK operation RESTRICT
ManagerApiKeyId               uuid non-empty
ManagerPrincipalId            uuid non-empty
RecipientClientApplicationId uuid non-empty
EventType                     varchar(48), exact operation event set
TargetIdentity                varchar(512)
PriorRevision                 bigint >= 0
NewRevision                   bigint > 0
Reason                        varchar(128)
AuthorizedDeliveryCount       integer NULL
StreamingDeliveryCount        integer NULL
InterruptedDeliveryCount      integer NULL
PayloadDigest                 bytea(32)
PriorScopesDigest             bytea(32) NULL
NewScopesDigest               bytea(32) NULL
EvidenceDigest                bytea(32)
OccurredAtUtc                 timestamptz NOT NULL
```

There is exactly one immutable audit event for each completed mutating
operation. UPDATE/DELETE are rejected by
`raw_export_guard_recipient_management_event()` through trigger
`trg_raw_export_recipient_management_event_append`; the manager capability has
no direct table DML.

The three count columns are all non-null and nonnegative only for
`RecipientKeyRevoked`; otherwise all three are null. Operation rows use the
same sparse rule for `OperationKind=RevokeKey`. Scope digests bind the exact
activation-capability transition, independently of any broader client policy:

| EventType | PriorScopesDigest | NewScopesDigest |
| --- | --- | --- |
| `ManagedRecipientEnrolled` | §4 empty-set digest | exact C3/C4 pair digest |
| `ManagedCredentialIssued` | §4 empty-set digest | exact C3/C4 pair digest |
| `ManagedCredentialReplaced` | exact C3/C4 pair digest | exact C3/C4 pair digest |
| `ManagedCredentialRevoked` | exact C3/C4 pair digest | §4 empty-set digest |
| `RecipientPublicKeyEnrolled` | NULL | NULL |
| `RecipientPublicKeyRotated` | NULL | NULL |
| `RecipientPublicKeyRevoked` | NULL | NULL |

The management event is the sole durable authority for these audit-only scope
digests and binds the same `OperationId` as its completed operation. The
operation `ResultSnapshot` remains exactly the public successful response DTO
with `PresentedKey` omitted and contains no audit-only scope fields. Credential
revoke records loss of usability of that credential's activation capability;
it does not claim that the identity-level policy overlay was deleted. The exact
event vocabulary, target encoding and reason authority are:

| OperationKind | EventType | TargetIdentity | Reason |
| --- | --- | --- | --- |
| `EnrollRecipient` | `ManagedRecipientEnrolled` | recipient UUID lowercase `N` | server constant `MANAGED_RECIPIENT_ENROLLED` |
| `IssueCredential` | `ManagedCredentialIssued` | ApiKeyId UUID lowercase `N` | server constant `MANAGED_CREDENTIAL_ISSUED` |
| `ReplaceCredential` | `ManagedCredentialReplaced` | new ApiKeyId UUID lowercase `N` | canonical caller Reason |
| `RevokeCredential` | `ManagedCredentialRevoked` | ApiKeyId UUID lowercase `N` | canonical caller Reason |
| `EnrollKey` | `RecipientPublicKeyEnrolled` | §4 framed key target; version + new=`-` | server constant `RECIPIENT_PUBLIC_KEY_ENROLLED` |
| `RotateKey` | `RecipientPublicKeyRotated` | §4 framed key target; old + new versions | canonical caller Reason |
| `RevokeKey` | `RecipientPublicKeyRevoked` | §4 framed key target; version + new=`-` | canonical caller Reason |

Key ids in TargetIdentity use their already-validated exact case-sensitive
text inside the §4 length-prefixed payload; no delimiter parsing is permitted.
Server-owned reason constants cannot be supplied or overridden by a request.

The event `PriorRevision/NewRevision` authority is exact:

| OperationKind | Revision authority |
| --- | --- |
| `EnrollRecipient` | managed identity `0 -> 1` |
| `IssueCredential` | newly inserted managed credential companion `0 -> 1` |
| `ReplaceCredential` | predecessor managed credential revision `r -> r+1` as it becomes Revoked |
| `RevokeCredential` | target managed credential revision `r -> r+1` |
| `EnrollKey` | newly inserted recipient-key row `0 -> 1` |
| `RotateKey` | K1 recipient-key revision `r -> r+1`; K2 is separately inserted at revision 1 |
| `RevokeKey` | target recipient-key revision `r -> r+1` |

C519 reads the named committed row(s), event and operation snapshot back and
asserts every pair against this table; substituting identity, K2, policy or a
request revision is not equivalent evidence.

### 5.5.1 Finite server-time authority

Every server-owned lifecycle timestamp caused by one C5 mutation is assigned
from that operation's single persisted `AdmissionAtUtc`. No C5 mutation uses a
second `clock_timestamp()`, `statement_timestamp()`, `transaction_timestamp()`,
`now()`, application clock or caller value for any field in this table.
`CompletedAtUtc` records the operation's linearized admission/commit decision
time and therefore equals `AdmissionAtUtc`; it is not a later wall-clock sample.

| OperationKind | Exact timestamp assignments |
| --- | --- |
| `EnrollRecipient` | identity `CreatedAtUtc=UpdatedAtUtc=AdmissionAtUtc`; policy `CreatedAtUtc=UpdatedAtUtc=AdmissionAtUtc` |
| `IssueCredential` | `api_keys.CreatedAt=AdmissionAtUtc`; companion `IssuedAtUtc=AdmissionAtUtc` |
| `ReplaceCredential` | new `api_keys.CreatedAt=AdmissionAtUtc`; new companion `IssuedAtUtc=AdmissionAtUtc`; predecessor companion `RevokedAtUtc=AdmissionAtUtc` |
| `RevokeCredential` | target companion `RevokedAtUtc=AdmissionAtUtc` |
| `EnrollKey` | new key `RegisteredAtUtc=AdmissionAtUtc` |
| `RotateKey` | K1 `RevokedAtUtc=AdmissionAtUtc`; K2 `RegisteredAtUtc=AdmissionAtUtc` |
| `RevokeKey` | target key `RevokedAtUtc=AdmissionAtUtc` |

For every row above, the same operation row has
`CompletedAtUtc=AdmissionAtUtc` and its sole management event has
`OccurredAtUtc=AdmissionAtUtc`. A lifecycle column not named for that operation
is not written. C509/C519/C521 read these exact committed assignments back; any
second clock source or unequal value is a named gating failure.

### 5.6 Existing-row additive changes

`raw_export_recipient_key_registrations` gains nullable
`RevocationReason varchar(128)`. Migration maps any pre-C5 Revoked row with no
reason to exact sentinel `PRE_C5_UNSPECIFIED`, then replaces the sparse CHECK:
Active requires both revocation fields null; Revoked requires both non-null.
C5 never changes key identity, SPKI, fingerprint or validity after insert.

The stricter CHECK intentionally affects predecessor test SQL. Exactly eight
landed statements must be corrected, only by adding a nonblank explicit
`RevocationReason` while preserving their existing state/revision/time intent:

```text
Tip88C1C2RecipientPackageTests.cs          lines 364, 1009, 1105
Tip88C1C3RecipientPackageDeliveryTests.cs lines 405, 1067, 1244, 1288
Tip88C1C4RecipientPackageReferenceTests.cs line 1860
```

The line numbers are baseline anchors and may move mechanically. The owning
statement census is by exact SQL occurrence. The CHECK must not be weakened to
make predecessor tests green; C529 proves all eight writes supply reason and a
scratch removal/nulled reason is rejected by the named sparse-shape bite.

`api_keys` gains only the composite alternate key
`(ApiKeyId, ClientApplicationId, PrincipalId)`; no column or existing read
contract changes. `ApiKeyId` is already the primary key, so duplicate tuples
cannot exist; migration nevertheless performs an explicit duplicate precheck
before adding the exact composite UNIQUE constraint required as the PostgreSQL
FK target.

### 5.7 Exact index delta

Six C5 supporting/uniqueness indexes exist:

```text
uq_raw_export_managed_recipient_identity_pair
uq_api_keys_managed_identity
uq_raw_export_managed_credential_version
uq_raw_export_managed_credential_active
uq_raw_export_recipient_management_idempotency
uq_raw_export_recipient_management_event_operation
```

The landed active-key and historical-fingerprint indexes remain byte-semantic
predecessor constraints and are not recreated or weakened.

## 6. Exact SQL surface, transitions and lock order

The eight application-callable functions are `LANGUAGE plpgsql SECURITY
DEFINER SET search_path=pg_catalog`, owned by
`tagekyc_raw_export_deployer`. One additional owned trigger function enforces
event append-only semantics. Identifiers are at most 63 UTF-8 bytes. Exact
signatures are:

```text
raw_export_enroll_managed_recipient(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid)

raw_export_issue_recipient_credential(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz)

raw_export_replace_recipient_credential(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text)

raw_export_revoke_recipient_credential(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text)

raw_export_enroll_recipient_key(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,text,bytea,bytea,timestamptz,timestamptz)

raw_export_rotate_recipient_key(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,integer,text,bytea,bytea,timestamptz,timestamptz,text)

raw_export_revoke_recipient_key(
  uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,text)

raw_export_read_recipient_activation_readiness(uuid)

raw_export_guard_recipient_management_event()
  RETURNS trigger
```

For the seven mutations, the leading arguments are respectively operation id,
manager API key, manager PrincipalId, recipient client id, idempotency digest,
equality fingerprint and payload digest. Subsequent values are typed target
inputs. UUID result identities are supplied by the application but checked for
non-empty and persisted only by SQL. The functions never accept actor values
from the HTTP body.

Each mutating function returns one row containing exact `Outcome`, operation
id, target ids/versions, prior/new revision, occurred time and relevant drain
counts. Credential functions additionally return whether the candidate digest
was committed; only application memory owns its matching plaintext.

### 6.1 Common replay and transaction prefix

Every mutating function:

1. validates non-null/non-empty/length/profile operation arguments; credential
   candidate fields alone may be null only for the claim/replay phase below;
2. performs the initial read-only completed-operation/idempotency probe for the
   exact `(ManagerPrincipalId, IdempotencyKeyDigest)` tuple;
3. if that probe finds a completed row, returns its immutable `ResultSnapshot`
   as
   `ExistingMatch` only when operation kind, target and
   equality fingerprint all match; credential issue/replace maps this to
   `ExistingMatchSecretUnavailable`; it never reads mutable result rows;
   otherwise it returns `Conflict`;
4. if there is no terminal replay/conflict, acquires the exact recipient-scoped
   transaction advisory lock from §2.3;
5. re-probes the same idempotency tuple under that authority and applies the
   exact terminal replay/conflict behavior from step 3;
6. if still absent, inserts the provisional claim shape from §5.4 with exact
   `ON CONFLICT ("ManagerPrincipalId","IdempotencyKeyDigest") DO NOTHING`, then
   re-reads and re-applies the equality comparison; any uncommitted competing
   row on that exact replay authority is resolved by PostgreSQL before this
   step returns. A collision on generated `OperationId` / exact constraint
   `PK_raw_export_recipient_management_operations` is not suppressed, is not an
   idempotency race, rolls back with no operation/event/state change and maps
   fail-closed to `Unavailable` without a retry;
7. locks the exact recipient identity when it exists; first enrollment relies
   on the already-held recipient advisory authority before inserting it;
8. locks credential rows in ascending CredentialVersion, or key rows in
   ascending `(RecipientKeyId, RecipientKeyVersion)`;
9. performs any C3 drain query without a lock or wait;
10. if the own provisional claim has NULL `AdmissionAtUtc`, captures exactly one
    fresh `clock_timestamp()` and writes it to that claim; otherwise reuses the
    already-present value and must not invoke the clock again;
11. revalidates manager/recipient authority, revisions and validity using that
    exact `AdmissionAtUtc`, performs writes, appends one event whose
    `OccurredAtUtc` equals it, writes the immutable result snapshot and
    completes the operation in the same transaction.

For issue/replace only, candidate prefix/digest arguments may be SQL NULL on
the first invocation within the repository transaction. Steps 1-6 then either
return a completed replay/conflict or insert and return `CandidateRequired`
with an uncommitted provisional claim. Only the new-operation owner generates
candidate material and re-invokes the same function on the same connection and
transaction with non-null candidate arguments. The second invocation sees its
own claim, continues steps 7-11 and may repeat only the candidate portion after
an intermediate internal `CandidateConflict`. That intermediate signal keeps
the transaction, recipient advisory authority, provisional claim and its
already-captured `AdmissionAtUtc` alive; it does not roll them back and is never
a public response. Candidate generation then retries inside that same authority
and the next SQL invocation reuses the stored timestamp without another clock
call. `CandidateRequired` and intermediate `CandidateConflict` are never
committed operation outcomes.

The repository owns an explicit transaction around each call. It commits only
`Created`, `Replaced`, `Rotated`, `Revoked` or a newly completed exact durable
success. It rolls back on `DeliveryDrainRequired`, terminal candidate-conflict
exhaustion after candidate five, public `Conflict`, `NotFound`, validation
failure or uncertainty, so those outcomes leave no operation/event row. An
intermediate internal `CandidateConflict` is explicitly excluded from this
rollback list. A later call may re-evaluate drain with the same idempotency key
because no mutation committed. Exact replay is available only for a committed
operation.

The sole advisory-lock use is the exact transaction-scoped recipient authority
in §2.3, acquired before row locks and released by transaction completion. No
C5 function uses a session advisory lock, `pg_sleep`, polling, provider/network
I/O, dynamic SQL, unbounded catalog lookup or any wait after acquiring the
recipient-key row lock. Credential generation is local application work and
occurs while the recipient advisory lock and provisional claim are held, but it
must perform no provider/network/database wait.

### 6.2 Identity and policy enrollment

`raw_export_enroll_managed_recipient` inserts one Active identity and one
Active `C3C4RecipientV1` overlay atomically. Exact replay returns the original
revision; same client with another principal is `Conflict`. The same principal
under another client is an independent managed identity and is not a conflict.
SQL computes the exact scope digest from:

```text
business.raw-export.package.download
business.raw-export.package.references.read
```

in ordinal order. Neither function nor repository accepts a caller scope list.
The enrollment event persists the §4 empty-set digest as `PriorScopesDigest`
and the exact two-scope digest as `NewScopesDigest`; SQL recomputes both and
includes them in audit-v2 evidence. The completed operation snapshot remains
the exact public enrollment response and is joined to that event by
`OperationId`.

### 6.3 Credential issue, replacement and revoke

Issue requires an Active identity/policy and zero Active managed credential.
It inserts the landed `api_keys` row by this exact finite projection; omitted or
implicitly defaulted columns are prohibited because the SQL function, unlike
the landed application provisioner, does not inherit application defaults:

| `api_keys` column | Exact C5 value/authority |
| --- | --- |
| `ApiKeyId` | generated non-empty candidate UUID |
| `ClientApplicationId` | exact managed `RecipientClientApplicationId` |
| `PrincipalId` | canonical persisted managed-identity `PrincipalId` |
| `CredentialRef` | server-owned `managed-api-key:` plus `ApiKeyId` lowercase `N`; never request supplied |
| `CredentialType` | exact landed literal `ManagedApiKey` |
| `CredentialStatus` | exact literal `Active` |
| `KeyPrefix` | exact 16-character candidate prefix |
| `KeyHash` | exact 32-byte candidate digest from the existing `ApiKeyStorePepper` path |
| `ScopesJson` | exact no-whitespace JSON array `["business.raw-export.package.download","business.raw-export.package.references.read"]` in ordinal order |
| `ExpiresAt` | validated caller-supplied expiry |
| `CallerCategory` | exact literal `BusinessConsumer` |
| `AllowedClientApplicationIdsJson` | SQL NULL |
| `AllowedCaptureAgentIdsJson` | SQL NULL |
| `OAuthClientId` | SQL NULL |
| `MtlsSubjectDn` | SQL NULL |
| `CreatedAt` | exact operation `AdmissionAtUtc` |

The row is followed by companion version 1. Its audit transition is empty scope
set to the exact C3/C4 activation pair. Intermediate `CandidateConflict` occurs
if and only if PostgreSQL reports SQLSTATE `23505` for exact landed unique index
`IX_api_keys_KeyPrefix` on `tagekyc.api_keys`; the transaction, advisory
authority and provisional claim remain active while the application generates
the next candidate. `PK_api_keys`, `uq_api_keys_managed_identity`, managed
credential version/one-active constraints and any other `23505` never map to
`CandidateConflict`, never consume the five-candidate budget and roll back
fail-closed: a deterministically revalidated lifecycle conflict maps to the
already-ratified `Conflict`; an unexpected or ambiguous uniqueness failure maps
to `Unavailable`. The first candidate attempt persists the single post-lock
`AdmissionAtUtc`; attempts two through five reuse it and do not call the clock.
The application may generate at most five candidates only while no
operation/credential commit exists. Five conflicts cause terminal exhaustion,
roll back the whole transaction and provisional claim, and map exactly to
`409 RAW_EXPORT_RECIPIENT_MANAGEMENT_CREDENTIAL_CONFLICT`; candidate five may
still commit normally when unique, and candidate six is prohibited.

Replacement locks identity then the exact Active credential. It requires the
expected revision and same canonical principal. Its new `api_keys` row uses the
same complete projection above, with a new candidate `ApiKeyId`, prefix, hash,
credential reference and `CreatedAt=AdmissionAtUtc`; no nullable authority
field is inherited from the predecessor. The exact feasible write order inside
one transaction is:

1. insert the new `api_keys` material row with CredentialStatus Active; it is
   still unusable because the required C5 Active companion does not yet exist;
2. mark the old companion Revoked and increment its revision;
3. mark the old `api_keys.CredentialStatus` Revoked;
4. insert the new Active companion at the next credential version;
5. set old `ReplacedByApiKeyId` to the now-existing new ApiKeyId;
6. append event, complete operation and commit.

Replacement audits the exact activation pair before and after because the
recipient capability set is preserved while material/version changes. A
standalone credential revoke audits the exact pair to the empty set because the
target credential becomes unusable, while leaving the identity-level overlay
unchanged. Only the event carries those audit-only digests; the operation
snapshot remains the exact public response, and the event's unique
`OperationId` FK proves their common committed operation.

No half-state is externally observable. The new `api_keys` row cannot
authenticate until its Active companion exists; the partial one-Active index is
released before the new Active companion insert; the self-FK target exists
before it is assigned. C507 injects a deterministic exception between steps 3
and 4 and proves rollback restores old companion/API-key Active, leaves no new
companion or usable key and leaves no operation/event.

Revoke changes only Active to Revoked, increments companion revision, updates
the exact `api_keys` status and appends audit. Exact repeat is ExistingMatch;
wrong identity/revision/reason under another idempotency key is Conflict or
NotFound according to authorized target visibility.

Credential revocation is prospective at authentication resolution. A request
that already completed authentication/authorization retains its immutable
`AuthenticatedClientContext` and may finish the currently authorized operation;
C5 does not re-resolve the credential mid-request. After the revoke transaction
commits, the next authentication using that credential is denied. This rule
does not confer authority on a second request and does not alter C3 key-current
delivery checks.

### 6.4 Key enrollment, rotation and revoke

Application profile validation accepts the exact case-sensitive literal
`RSA-OAEP-256` only, successfully imports the complete DER SPKI, consumes all
bytes, identifies RSA, enforces `3072..4096` bits and recomputes SHA-256. Any
other literal, including case variants, maps to
`RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_PROFILE_INVALID` before SQL. SQL
independently requires the same literal plus non-empty identity/key, version,
32-byte digest, validity and historical uniqueness.

Initial enroll requires version 1 and no Active key. Rotation requires:

```text
same RecipientKeyId
NewKeyVersion > CurrentKeyVersion
new historical fingerprint
exact CurrentKeyRevision
K1 State = Active
K2 validity satisfies now and now + 5 minutes
```

After locking K1, planned rotation executes exactly one non-locking aggregate:

```sql
SELECT
  count(*) FILTER (WHERE d."State"='Authorized'),
  count(*) FILTER (WHERE d."State"='Streaming'),
  count(*) FILTER (WHERE d."State"='Interrupted')
FROM tagekyc.raw_export_recipient_package_deliveries d
WHERE d."RecipientClientApplicationId"=p_recipient
  AND d."RecipientKeyId"=p_key_id
  AND d."RecipientKeyVersion"=p_current_version
  AND d."State" IN ('Authorized','Streaming','Interrupted');
```

Any nonzero count returns `DeliveryDrainRequired`; the transaction releases K1
without mutation. Zero counts permit the single fresh clock, K1 revoke/revision
increment/reason and K2 Active insert in one commit.

Emergency revoke uses no drain gate, but captures the same three counts after
the K1 lock and before the fresh clock, then persists the exact counts in the
operation and event rows before revoking K1. Its result carries warning
`EXISTING_DELIVERIES_MAY_BE_STRANDED`. Exact replay returns the committed
warning/counts and never re-reads later C3 states. Replacement-after-revocation
uses the enroll function with the same key id, next greater version and
entirely new material; it is admitted only when no Active key exists.

### 6.5 Exact state transitions

```text
Identity:  absent -> Active; Active -> Disabled is reserved, not exposed by C5 v1
Policy:    absent -> Active; Active -> Disabled is reserved, not exposed by C5 v1
Credential absent -> Active -> Revoked
Key        absent -> Active -> Revoked
Operation  absent -> Completed (atomic only)
Event      append-only; no update/delete
```

No `Pending`, `Retired`, `Ready`, `NotReady`, package deletion or re-key state
is authorized.

## 7. Application behavior and precedence

The application service owns authorization, exact DTO validation, SPKI
profile, candidate credential material and mapping. The repository owns typed
SQL calls only. It does not reimplement SQL equality, infer actor identity or
repair an ambiguous outcome.

For credential issue/replace:

1. authorize actor;
2. validate request/idempotency;
3. open the repository transaction and invoke the exact SQL function with null
   candidate arguments so it performs the initial idempotency probe and, only
   when no terminal result exists, acquires recipient authority, re-probes and
   creates the provisional claim;
4. if SQL returns a committed replay, deserialize only its immutable operation
   `ResultSnapshot`, return null `PresentedKey` and invoke the credential secret
   generator zero times; conflict likewise generates nothing;
5. only on `CandidateRequired`, generate one managed API key candidate and hash
   it using the existing `ApiKeyStorePepper` path, then re-invoke the same SQL
   function in the same transaction;
6. return plaintext only for `Created/Replaced` whose committed prefix/digest
   equals the candidate;
7. only on intermediate internal `CandidateConflict` for exact
   `IX_api_keys_KeyPrefix`, keep the same transaction,
   advisory authority, provisional claim and `AdmissionAtUtc`, then generate a
   new candidate, maximum five, only after proving no operation or credential
   committed; every retry reuses that timestamp and performs zero additional
   `clock_timestamp()` calls;
8. after the fifth conflict, roll back and return exact
   `RAW_EXPORT_RECIPIENT_MANAGEMENT_CREDENTIAL_CONFLICT`; on replay, conflict,
   uncertainty or cancellation return zero plaintext.

The service never logs the presented key, passes it to SQL, stores it in an
event or reconstructs it. A response-loss test consumes/discards the first
response, repeats exact idempotency and must observe durable metadata plus null
plaintext with generator calls zero. The replay projection comes only from the
committed operation snapshot, even if a later credential/key lifecycle change
has altered the live row. Recovery is the explicit replace route with a new
idempotency key.

Route-specific outcome precedence after authorization is:

```text
malformed request/idempotency
-> durable idempotency conflict/exact replay
-> target/identity conflict
-> profile or revision conflict
-> DeliveryDrainRequired where applicable
-> committed outcome
-> Unavailable for uncertainty
```

No failure changes another row or appends an audit event. `ExistingMatch` is a
durable success, not a new write.

## 8. Role, ACL and catalog census

C5 adds exactly:

```text
capability NOLOGIN: tagekyc_raw_export_recipient_manager
canonical LOGIN:   tagekyc_raw_export_recipient_manager_login
membership:        LOGIN -> capability, ADMIN false, INHERIT true, SET false
```

Both roles are NOSUPERUSER, NOCREATEDB, NOCREATEROLE, NOREPLICATION,
NOBYPASSRLS; capability cannot login; LOGIN has password null in the controlled
test cluster. Migration requires the LOGIN to pre-exist and creates/verifies
only the capability. The integration fixture expands its exact bootstrap list
from 10 to 11 raw-export LOGIN roles.

The capability receives schema USAGE and EXECUTE on exactly the seven mutating
functions plus readiness read. It receives no EXECUTE grant on the trigger
guard and no direct table SELECT/INSERT/
UPDATE/DELETE, no sequence privilege, no C1-C4 function and no role-admin edge.
PUBLIC, the LOGIN directly and every other runtime role receive no C5 EXECUTE.
Every function ACL is exact two-way equality: one non-owner EXECUTE entry,
granted by the owner, non-grantable, to the C5 capability.

Because the existing PostgreSQL authenticator resolves `api_keys` through the
ordinary application runtime, `tagekyc_runtime` receives SELECT on exactly the
three C5 authentication-projection tables: managed identities, policies and
credentials. It receives no SELECT on operations/events/key SPKI, no C5
function EXECUTE and no C5 DML. The C5 function owner receives exact
SELECT/INSERT/UPDATE (not DELETE) on existing `api_keys`; this is required for
guarded credential creation/revocation and is asserted as an owner grant, not a
runtime capability. All five new tables and the guard function are owned by
`tagekyc_raw_export_deployer`.

Census summary:

```text
new C5 tables                   5
existing tables altered/read    api_keys + recipient key registry + C3 delivery
new SQL functions               9 (8 callable + 1 trigger guard)
new supporting indexes          6
new capability roles            1
new LOGIN roles                 1
recipient C2-C5 capabilities    5 -> 6
cluster raw-export LOGIN list   10 -> 11
S3 provider credentials         5 -> 5
new S3/provider operation       0
```

Catalog ownership is an exact set comparison over the names/signatures above.
`LIKE`, `ILIKE`, regex and prefix/suffix ownership are forbidden. A wildcard
may discover plausible siblings only when the result is then compared against
the finite authoritative set.

## 9. Readiness and composition

C5 has one production validator with ordered codes:

```text
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CONFIG_INVALID
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CATALOG_INVALID
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID
PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_PREDECESSOR_INVALID
```

Topology values are exactly `Disabled`, `PostgresDurable`, `Invalid`.
Disabled-clean is dependency-free and green. Invalid is dependency-free and
returns config-invalid. PostgresDurable validates exact connection, role,
catalog and live managed state. Its first opened management connection must
report `current_user=tagekyc_raw_export_recipient_manager_login`; ordinary
runtime identity or any other login is role-topology-invalid. Managed-auth
readiness also resolves a canonical C5 recipient through the credential-aware
authenticator and a PostgreSQL OperatorAdmin through the persisted
`api_keys` management-policy branch, with a zero-call LocalDev trap for both.

For one recipient, managed-auth readiness requires one Active identity/policy,
one Active companion/API-key pair with exact stable principal, BusinessConsumer
category, exact canonical two scopes, active status and unexpired time. Key
readiness requires exactly one Active key, complete RSA profile, recomputed
fingerprint, current validity through `now + 5 minutes`, consistent audit and
no ambiguous rows.

The recipient projection calls existing C2, C3 and C4 validators as owners and
preserves their exact codes under the predecessor component. It never copies
their catalog SQL or converts failure to durable C5 state. Global `/readiness`
registers one C5 check and remains bound by the existing two-second quiescing
contract.

RotationReadiness reports exact three delivery-state counts but does not change
general activation readiness. No readiness probe mutates data or emits audit.

## 10. Exact 47-path permanent allowlist

Only these repository paths may change in a future controlled build:

```text
01 .gitattributes
02 docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_scope_brief.md
03 docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md
04 docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_review_ledger.md
05 docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_as_built.md
06 src/TagEkyc.Contracts/RawExport/RecipientManagementContracts.cs
07 src/TagEkyc.Application/Ports/RecipientManagementPorts.cs
08 src/TagEkyc.Application/RawExport/RecipientManagementApplicationService.cs
09 src/TagEkyc.Api/RecipientManagementEndpoints.cs
10 src/TagEkyc.Api/Program.cs
11 src/TagEkyc.Api/ReadinessEndpoint.cs
12 src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs
13 src/TagEkyc.Infrastructure/RawExport/RecipientManagementContracts.cs
14 src/TagEkyc.Infrastructure/RawExport/RecipientManagementCodec.cs
15 src/TagEkyc.Infrastructure/RawExport/RecipientManagementConnectionFactory.cs
16 src/TagEkyc.Infrastructure/RawExport/RecipientManagementRepository.cs
17 src/TagEkyc.Infrastructure/RawExport/RecipientManagementReadinessValidator.cs
18 src/TagEkyc.Infrastructure/RawExport/RecipientManagementServiceCollectionExtensions.cs
19 src/TagEkyc.Infrastructure/RawExport/RecipientPublicKeyProfileValidator.cs
20 src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
21 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportManagedRecipientIdentityRow.cs
22 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportManagedRecipientPolicyRow.cs
23 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportManagedRecipientCredentialRow.cs
24 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientManagementOperationRow.cs
25 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientManagementEventRow.cs
26 src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientKeyRegistrationRow.cs
27 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportManagedRecipientIdentityConfig.cs
28 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportManagedRecipientPolicyConfig.cs
29 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportManagedRecipientCredentialConfig.cs
30 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientManagementOperationConfig.cs
31 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientManagementEventConfig.cs
32 src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs
33 src/TagEkyc.Infrastructure/Persistence/Migrations/20260823120000_Tip88C1C5ManagedRecipientEnrollment.cs
34 src/TagEkyc.Infrastructure/Persistence/Migrations/20260823120000_Tip88C1C5ManagedRecipientEnrollment.Designer.cs
35 src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
36 tests/TagEkyc.ContractTests/Tip88C1C5RecipientManagementContractTests.cs
37 tests/TagEkyc.UnitTests/Tip88C1C5RecipientManagementApplicationTests.cs
38 tests/TagEkyc.UnitTests/Tip88C1C5RecipientManagementCodecTests.cs
39 tests/TagEkyc.ArchTests/Tip88C1C5RecipientManagementArchTests.cs
40 tests/TagEkyc.IntegrationTests/Tip88C1C5RecipientManagementTests.cs
41 tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
42 tests/TagEkyc.IntegrationTests/Tip84BHashedApiKeyStoreTests.cs
43 tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs
44 tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs
45 tests/TagEkyc.IntegrationTests/Tip88C1C4RecipientPackageReferenceTests.cs
46 tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
47 tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs
```

Any 48th permanent path, tenth SQL function, sixth C5 table, seventh index,
31st proof or 49th mutation is STOP/RRI. Unchanged dependency files may be
included in a review bundle as evidence but are not changed payload.

## 11. Proof construction protocol

Every mutation runs alone. Before a source mutation, record the canonical
SHA-256; apply one edit; run only its owner; require RED at the exact named bite;
reverse the edit; require the exact original SHA and canonical owner PASS. A
neighboring comparator, build error, fixture failure, anonymous assertion or
different bite does not close it.

Catalog mutations use a disposable current database. Database-local functions,
indexes and grants are restored by transaction rollback or database drop.
Cluster roles/memberships use names outside all `tagekyc_raw_export_*` owned
names, are revoked/dropped in `finally`, and are proven absent. No mutation
touches the shared template or leaves a role/container/volume.

`TWO-STATE` means the proof first asserts the positive and negative fixtures
differ only in the named dimension. `NAMED GATING` means exception-presence,
type/code and value checks are one explicit named assertion; `Throws` cannot
hide an anonymous gate. `ONE-DIMENSION` is recorded in each evidence manifest.

## 12. Historical v0.5 C501-C530 proof manifest — superseded by §19

This section is historical predecessor text only. It is not an implementation
or proof-execution source; §19, the SHA-bound R8 registers and §19.1 control
every current fixture, observation, mutation and restore obligation.

The table is executable, not descriptive. Every named observation is explicitly
classified: `D` is a mutation-owned `DISCRIMINATOR`; `C` is a canonical,
preservation, layered-defense or failure-injection `CONTROL`. `Restore` means
the exact protocol in §11 plus the canonical owner rerun.

| ID | Fixture and positive control | One-dimensional mutation | Named observations (`D` / `C`) | Restore |
| --- | --- | --- | --- | --- |
| C501 | Real minimal API maps exactly eight routes; reflection pins all DTO properties and no forbidden field | C5M01 removes one route mapping | D `C501-EXACT-HTTP-SURFACE` | source SHA + canonical PASS |
| C502 | First proves three actors differ only by category/scope: valid PostgreSQL OperatorAdmin, BusinessConsumer with same management scope, and OperatorAdmin missing only that scope; each denied fixture returns FORBIDDEN with repository calls=0 | C5M02 removes only category comparator; C5M03 removes only required-scope containment comparator | D `C502-OPERATOR-CATEGORY`; D `C502-MANAGEMENT-SCOPE` | source SHA + repository-call zero control |
| C503 | First proves explicit distinct nonempty principal and Guid.Empty requests differ only in PrincipalId; empty principal returns REQUEST_INVALID with repository calls=0, while an independent direct-SQL control rejects empty; valid enroll asserts identity/api-key/context triple equality | C5M04 removes only the application Guid.Empty guard so repository calls become nonzero; C5M05 emits ClientApplicationId instead of persisted PrincipalId | D `C503-NONEMPTY-PRINCIPAL-BEFORE-REPOSITORY`; C `C503-SQL-NONEMPTY-PRINCIPAL-INDEPENDENT`; D `C503-PERSISTED-PRINCIPAL-NOT-DERIVED` | source SHA + rows rolled back |
| C504 | Same client has two unrelated credentials: under broader global policy Disabled the first is denied both before and after C5 enrollment; under broader global policy Active the second preserves the exact success/context shape; exact activation credential succeeds while generic-provisioner activation scopes without companion reject | C5M06 drops one activation scope; C5M07 adds one sibling scope; C5M08 removes only the activation-companion requirement so the generic writer becomes usable | D `C504-EXACT-ACTIVATION-SCOPE-SET`; C `C504-SAME-CLIENT-DISABLED-STAYS-DENIED`; C `C504-SAME-CLIENT-ACTIVE-STAYS-EQUIVALENT`; D `C504-NO-GENERIC-ACTIVATION-WRITER` | source SHA + disposable DB |
| C505 | Credential-aware authenticator resolves canonical recipient/admin from PostgreSQL with counting LocalDev fallback=0, including OperatorAdmin with management scope plus one unrelated scope; an unrelated credential under the same C5 client calls the unchanged global provider exactly once; other global consumers retain the original provider instance | C5M09 replaces only OperatorAdmin required-scope containment with singleton-set equality, sending the valid admin-superset credential to fallback | D `C505-ADMIN-SUPERSET-ROUTING`; C `C505-MANAGED-POLICY-ZERO-LOCALDEV-CALL`; C `C505-RESOLVED-CREDENTIAL-ROUTING`; C `C505-AUTH-ONLY-COMPOSITION-SCOPE` | DI bytes restore + PASS |
| C506 | Issue succeeds once; first response is discarded; exact replay reads durable metadata with null secret; explicit replacement returns a new secret | C5M10 regenerates/returns secret on replay | D `C506-REPLAY-PLAINTEXT-ABSENT` | source SHA + zero log scan |
| C507 | Replacement snapshots old/new credential states and proves versions/digests differ while identity pair matches; deterministic failure between old revoke/new companion proves rollback | C5M11 changes only the new api_keys/companion PrincipalId assignment from persisted PrincipalId to RecipientClientApplicationId | D `C507-REPLACEMENT-STABLE-PRINCIPAL`; C `C507-REPLACEMENT-ATOMIC-ROLLBACK` | source SHA + old Active/no-new/no-op/no-event controls |
| C508 | Canonical real revoke rejects next auth; separate TWO-STATE rows hold api_keys Active in both and differ only companion Active/Revoked, with Revoked rejected | C5M12 removes only companion-State predicate from store activation-scope join | D `C508-MANAGED-COMPANION-STATE-ENFORCED`; C `C508-NEXT-AUTH-REVOKED` | store SHA + canonical PASS |
| C509 | Real RSA-3072 SPKI + exact literal + digest succeeds; malformed/trailing and wrong-fingerprint fixtures isolate their dimensions; wrong literal returns KEY_PROFILE_INVALID with repository calls=0, while a direct-SQL control independently rejects wrong literal | C5M13 removes only complete DER rejection; C5M14 removes only application literal comparator; C5M15 removes only computed-vs-claimed fingerprint equality | D `C509-SPKI-COMPLETE`; D `C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY`; D `C509-FINGERPRINT-RECOMPUTE`; C `C509-SQL-ALGORITHM-INDEPENDENT` | source SHA + zero persisted rows |
| C510 | K1 v1 enrolls, revokes, then a higher-version same-recipient request with identical historical SPKI/fingerprint is first proven otherwise valid and rejected; a second recipient with that fingerprint is allowed and first proven distinct only by RecipientClientApplicationId | C5M16 removes only RecipientClientApplicationId from the historical lookup, incorrectly making the fingerprint globally unique | C `C510-HISTORICAL-FINGERPRINT-NONREUSE`; D `C510-HISTORICAL-FINGERPRINT-RECIPIENT-SCOPED` | source/catalog SHA + PASS |
| C511 | Canonical real-function concurrency proves the recipient-identity lock serializes two same-recipient enrollments to one winner; an independent scratch control directly attempts two CHECK-valid Active rows, where the partial index alone rejects the second insert; separate history v1/current v3 then fresh-material candidate v2 isolates strict monotonicity | C5M17 drops only the partial one-Active index in the disposable scratch catalog so the second direct insert succeeds; C5M18 removes only `candidateVersion > historicalMaxVersion` comparator | C `C511-IDENTITY-LOCK-SERIALIZATION`; D `C511-ONE-ACTIVE-KEY`; D `C511-STRICT-VERSION` | source SHA + disposable DB |
| C512 | Four fixtures first assert delivery state is None/Authorized/Streaming/Interrupted; only None rotates; a blocker owns a matching delivery row while canonical aggregate remains bounded | C5M19 omits only Authorized; C5M20 omits only Streaming; C5M21 omits only Interrupted; C5M22 wraps only the aggregate source in a `SELECT ... FOR UPDATE` CTE | D `C512-AUTHORIZED-DRAIN`; D `C512-STREAMING-DRAIN`; D `C512-INTERRUPTED-DRAIN`; D `C512-NONWAITING-DRAIN` | function SHA/catalog restore + bounded completion |
| C513 | Concurrent rotations share K1; exactly one K1 Revoked/K2 Active/event result; canonical deterministic exception after state writes/before event proves full rollback | C5M23 appends one valid-tuple K2 update after insert (`Revoked`, nonnull time/reason, revision increment), producing a committable zero-Active state after K1 revoke; C5M24 removes only event append from rotate function | D `C513-HARD-CUTOVER`; D `C513-REQUIRED-ROTATION-EVENT`; C `C513-ATOMIC-ROLLBACK-CONTROL` | source/catalog SHA + no-half-state PASS |
| C514 | Barrier controls force (a) reserve K1 first, (b) rotate before selection, (c) select K1 then rotate then reserve; identities are asserted distinct | C5M25 removes stale revision/state reject; C5M26 silently reselects K2 inside reserve | D `C514-STALE-K1-UNAVAILABLE`; D `C514-NO-SILENT-RESELECTION` | predecessor/catalog restore + exact C2 SHA |
| C515 | Barrier forces reserve-first and revoke-first in separate disposable DBs | C5M27 permits revoked K1 reserve | D `C515-REVOKE-VS-RESERVE` | C2/C5 catalog restore + PASS |
| C516 | Real emergency revoke proves Authorized/Interrupted strand, admitted Streaming survives and 1/1/1 summary replays; separate scratch rows first prove State differs while revision/fingerprint/validity/package remain identical | C5M28 removes only C3 current-key State comparator in scratch catalog; C5M29 adds only planned drain rejection to emergency revoke | D `C516-C3-STATE-COMPARATOR`; D `C516-EMERGENCY-REVOKE-NO-DRAIN`; C `C516-DURABLE-COUNT-REPLAY` | C3/C5 catalog restore + PASS |
| C517 | Real C2 package snapshot is frozen, K1 revoked, exact package fields are byte-equal and real C4 still lists PackageId | C5M30 adds live-key predicate to C4 catalog function | C `C517-HISTORICAL-SNAPSHOT-UNCHANGED`; D `C517-C4-VISIBILITY-UNCHANGED` | C4 catalog restore + PASS |
| C518 | Standalone revoke makes readiness RED; recovery first proves stable RecipientKeyId, greater version and fresh SPKI/fingerprint, then makes readiness GREEN while old package remains K1 | C5M31 assigns a different RecipientKeyId to the recovery row | D `C518-RECOVERY-SAME-KEY-FAMILY`; C `C518-RECOVERY-FRESH-MATERIAL` | source/catalog restore + PASS |
| C519 | Every event binds authenticated admin A, exact vocabulary/target/reason/revisions/payload/counts/post-lock time; owner/deployer direct UPDATE and DELETE both reach trigger and reject, while manager DML denial stays in C524 | C5M32 assigns target RecipientClientApplicationId to ManagerPrincipalId field; C5M33 moves only audit clock pre-lock; C5M35 removes only UPDATE rejection from trigger | D `C519-AUDIT-AUTHENTICATED-ACTOR`; D `C519-AUDIT-POSTLOCK-CLOCK`; D `C519-AUDIT-APPEND-ONLY-UPDATE` | source/catalog SHA + PASS |
| C520 | Exact replay and same-key/different-payload controls first prove fingerprints equal/different | C5M34 drops equality comparator | D `C520-IDEMPOTENCY-EQUALITY` | function SHA + PASS |
| C521 | Instrumented barriers prove global lock order; after key acquisition only drain SELECT/clock/writes occur and function completes under bounded blocker controls | C5M47 inverts identity/key order; C5M48 moves clock before locks | D `C521-GLOBAL-LOCK-ORDER`; D `C521-POSTLOCK-FRESH-CLOCK` | migration SHA + canonical concurrency PASS |
| C522 | Real ready recipient is GREEN; separately revoke credential and key after proving intended state difference | C5M36 omits credential predicate; C5M37 omits key predicate | D `C522-MANAGED-AUTH-READINESS`; D `C522-KEY-READINESS` | validator SHA + rows restored |
| C523 | Exact table/role/function/index sets GREEN; real sibling of every database-local kind and synthetic role control are first proven outside each owned set | C5M38 replaces only table exact-name set with wildcard; C5M39 replaces only function exact-signature set with wildcard | D `C523-EXACT-TABLE-OWNERSHIP`; D `C523-EXACT-FUNCTION-OWNERSHIP` | disposable DB; siblings absent |
| C524 | Canonical exact roles/ACL GREEN; scratch REVOKE of only manager EXECUTE makes validator RED; runtime function/DML deny, manager function success/current_user and all other posture controls remain independent | C5M40 removes only mandatory manager-capability EXECUTE comparator while scratch grant remains revoked | D `C524-MANDATORY-MANAGER-EXECUTE`; C `C524-DEDICATED-MANAGER-ROLE`; C `C524-MANAGER-NO-DIRECT-DML` | role/catalog cleanup + validator SHA |
| C525 | Disabled/Invalid and missing-secret paths resolve zero manager DB/predecessor services with CONFIG_INVALID; durable path requires exact current_user manager role and invokes C2/C3/C4 owners once | C5M41 bypasses exactly the C3 owning validator | D `C525-OWNER-READINESS-COMPOSITION`; C `C525-CONFIG-BEFORE-DB`; C `C525-EXACT-CONNECTED-ROLE` | DI source SHA + PASS |
| C526 | PostgreSQL admin -> C5 identity/credential/key -> real C1 -> real C2 -> real C4 -> real C3; only opaque PackageId crosses C4/C3 and no canonical key/package row is test-seeded | C5M42 direct-inserts only the canonical C2 package row | D `C526-REAL-MANAGED-C1-C4-CHAIN` | source SHA + provider/DB cleanup |
| C527 | EF additive-model diff is only C5 model/additive key column/constraint; all three raw-byte pins equal final LF snapshot | C5M43 pins one tripwire to predecessor hash | D `C527-THREE-TRIPWIRE-LOCKSTEP` | tripwire SHAs + canonical C201/R316/E3 PASS |
| C528 | Migration apply/Down/reapply only in disposable DB and normalized catalog compare; anonymous Docker volumes counted before/after targeted harness and `down -v` guard asserted independently | C5M44 changes only migration target from disposable DB to shared template | D `C528-DISPOSABLE-MIGRATION-ONLY` | DB dropped; volume count equal |
| C529 | Affected auth and C2/C3/C4 proofs run on final bytes; exact census proves all eight predecessor revoke SQL sites set nonblank reason and strict sparse CHECK rejects a null-reason Revoked row | C5M45 removes `RevocationReason` from exactly one baseline predecessor update | D `C529-STRICT-REVOCATION-REASON-PREDECESSOR` | predecessor SHA/catalog restore |
| C530 | As-built/ledger list exact allowlist, proofs, mutations, debts/nonclaims and both Product-Ready gates; no premature closure | C5M46 removes only `C5-C2-PRELOCK-CLOCK-NONCLAIM-01` from carried register | D `C530-DEBT-AND-NONCLAIM-ACCOUNTING` | docs SHA + review diff |

### 12.1 Observation taxonomy and no-orphan rule

The classification is normative:

- every `D` name appears as a required named bite in §13;
- every §13 mutation targets exactly one `D` name;
- a `C` observation is required for canonical PASS but is not represented as
  mutation-closed and cannot be cited as mutation evidence;
- a successor may not add a named observation unless the same successor either
  maps it to a concrete §13 mutation as `D` or records it here as `C` with a
  bounded reason;
- an unclassified name, a `D` without a mutation, or a mutation targeting `C`
  is a documentation HARD STOP before implementation.

The complete control register is:

| Control | Bounded reason |
| --- | --- |
| `C503-SQL-NONEMPTY-PRINCIPAL-INDEPENDENT` | independent database defense control; C5M04 isolates the application layer |
| `C504-SAME-CLIENT-ACTIVE-STAYS-EQUIVALENT` | before/after preservation control for unrelated behavior |
| `C504-SAME-CLIENT-DISABLED-STAYS-DENIED` | before/after preservation control for unrelated behavior |
| `C505-AUTH-ONLY-COMPOSITION-SCOPE` | DI/topology preservation control; it owns no independent comparator |
| `C505-MANAGED-POLICY-ZERO-LOCALDEV-CALL` | canonical call-count control across recipient/admin paths; C5M09 owns the admin-superset routing defect |
| `C505-RESOLVED-CREDENTIAL-ROUTING` | canonical three-branch composition control; individual routing defects use their named discriminators |
| `C507-REPLACEMENT-ATOMIC-ROLLBACK` | deterministic failure-injection rollback control |
| `C508-NEXT-AUTH-REVOKED` | real-operation end-to-end control; C5M12 owns the isolated companion-State comparator |
| `C509-SQL-ALGORITHM-INDEPENDENT` | independent database-layer defense control; C5M14 isolates the application layer |
| `C510-HISTORICAL-FINGERPRINT-NONREUSE` | landed same-recipient UNIQUE-constraint control; C5M16 owns recipient scoping |
| `C511-IDENTITY-LOCK-SERIALIZATION` | canonical real-function concurrency control; C5M17 owns only the physical partial index |
| `C513-ATOMIC-ROLLBACK-CONTROL` | deterministic failure-injection rollback control |
| `C516-DURABLE-COUNT-REPLAY` | canonical committed-response replay control; no standalone comparator is claimed |
| `C517-HISTORICAL-SNAPSHOT-UNCHANGED` | predecessor byte-preservation control; C5M30 targets only C4 visibility |
| `C518-RECOVERY-FRESH-MATERIAL` | landed fingerprint-uniqueness plus canonical recovery control; C5M31 owns stable key-family identity |
| `C524-DEDICATED-MANAGER-ROLE` | canonical positive topology control; ACL discriminators are separately mutation-owned |
| `C524-MANAGER-NO-DIRECT-DML` | canonical least-privilege ACL control; no mutation closure is claimed |
| `C525-CONFIG-BEFORE-DB` | fail-fast dependency-zero control; no mutation closure is claimed |
| `C525-EXACT-CONNECTED-ROLE` | canonical connected-identity control; no mutation closure is claimed |

The build must mechanically derive both sets from §§12-13 and require:

```text
named observations = discriminators UNION controls
discriminators INTERSECT controls = empty
mutation target names = discriminators
blank/unclassified names = 0
```

## 13. Historical v0.5 C5M01-C5M48 mutation manifest — superseded by §19

| Mutation | Owner | One edit only | Required named bite |
| --- | --- | --- | --- |
| C5M01 | C501 | remove one route mapping | `C501-EXACT-HTTP-SURFACE` |
| C5M02 | C502 | remove OperatorAdmin comparator | `C502-OPERATOR-CATEGORY` |
| C5M03 | C502 | remove required management-scope containment comparator | `C502-MANAGEMENT-SCOPE` |
| C5M04 | C503 | remove only application Guid.Empty guard so the empty request reaches repository | `C503-NONEMPTY-PRINCIPAL-BEFORE-REPOSITORY` |
| C5M05 | C503 | emit ClientApplicationId as PrincipalId | `C503-PERSISTED-PRINCIPAL-NOT-DERIVED` |
| C5M06 | C504 | omit C3 activation scope | `C504-EXACT-ACTIVATION-SCOPE-SET` |
| C5M07 | C504 | add unrelated/sibling scope to credential | `C504-EXACT-ACTIVATION-SCOPE-SET` |
| C5M08 | C504 | remove activation-companion requirement from the store gate | `C504-NO-GENERIC-ACTIVATION-WRITER` |
| C5M09 | C505 | replace OperatorAdmin required-scope containment with singleton-set equality | `C505-ADMIN-SUPERSET-ROUTING` |
| C5M10 | C506 | return regenerated secret on replay | `C506-REPLAY-PLAINTEXT-ABSENT` |
| C5M11 | C507 | assign RecipientClientApplicationId instead of persisted PrincipalId to new replacement rows | `C507-REPLACEMENT-STABLE-PRINCIPAL` |
| C5M12 | C508 | remove only companion-State predicate from activation-scope store join | `C508-MANAGED-COMPANION-STATE-ENFORCED` |
| C5M13 | C509 | remove complete-DER-consumption rejection | `C509-SPKI-COMPLETE` |
| C5M14 | C509 | remove only application `RSA-OAEP-256` literal comparator | `C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY` |
| C5M15 | C509 | remove computed-vs-claimed fingerprint equality | `C509-FINGERPRINT-RECOMPUTE` |
| C5M16 | C510 | remove RecipientClientApplicationId predicate from historical fingerprint lookup | `C510-HISTORICAL-FINGERPRINT-RECIPIENT-SCOPED` |
| C5M17 | C511 | drop only the partial one-Active key index in scratch catalog | `C511-ONE-ACTIVE-KEY` |
| C5M18 | C511 | remove candidate-version-greater-than-historical-max comparator | `C511-STRICT-VERSION` |
| C5M19 | C512 | omit Authorized from drain predicate | `C512-AUTHORIZED-DRAIN` |
| C5M20 | C512 | omit Streaming from drain predicate | `C512-STREAMING-DRAIN` |
| C5M21 | C512 | omit Interrupted from drain predicate | `C512-INTERRUPTED-DRAIN` |
| C5M22 | C512 | wrap aggregate source in one `SELECT ... FOR UPDATE` CTE | `C512-NONWAITING-DRAIN` |
| C5M23 | C513 | append one post-insert UPDATE that makes K2 a CHECK-valid Revoked tuple after canonical K1 revoke | `C513-HARD-CUTOVER` |
| C5M24 | C513 | remove only rotation event append from SQL function | `C513-REQUIRED-ROTATION-EVENT` |
| C5M25 | C514 | remove stale K1 revision/state rejection | `C514-STALE-K1-UNAVAILABLE` |
| C5M26 | C514 | silently reselect K2 in same reserve call | `C514-NO-SILENT-RESELECTION` |
| C5M27 | C515 | allow reserve of revoked exact K1 | `C515-REVOKE-VS-RESERVE` |
| C5M28 | C516 | remove only C3 current-key State comparator in scratch catalog | `C516-C3-STATE-COMPARATOR` |
| C5M29 | C516 | apply planned drain to emergency revoke | `C516-EMERGENCY-REVOKE-NO-DRAIN` |
| C5M30 | C517 | add live Active-key join to C4 listing in scratch catalog | `C517-C4-VISIBILITY-UNCHANGED` |
| C5M31 | C518 | assign a different RecipientKeyId to the recovery row | `C518-RECOVERY-SAME-KEY-FAMILY` |
| C5M32 | C519 | assign target RecipientClientApplicationId to event ManagerPrincipalId | `C519-AUDIT-AUTHENTICATED-ACTOR` |
| C5M33 | C519 | capture audit/admission clock before locks | `C519-AUDIT-POSTLOCK-CLOCK` |
| C5M34 | C520 | remove replay equality comparator | `C520-IDEMPOTENCY-EQUALITY` |
| C5M35 | C519 | remove only UPDATE rejection from event trigger guard | `C519-AUDIT-APPEND-ONLY-UPDATE` |
| C5M36 | C522 | omit active managed credential readiness predicate | `C522-MANAGED-AUTH-READINESS` |
| C5M37 | C522 | omit active key/profile readiness predicate | `C522-KEY-READINESS` |
| C5M38 | C523 | replace only exact table-name set with wildcard | `C523-EXACT-TABLE-OWNERSHIP` |
| C5M39 | C523 | replace only exact function-name/signature set with wildcard | `C523-EXACT-FUNCTION-OWNERSHIP` |
| C5M40 | C524 | remove mandatory manager-EXECUTE comparator while scratch grant is revoked | `C524-MANDATORY-MANAGER-EXECUTE` |
| C5M41 | C525 | bypass exactly the C3 owning validator | `C525-OWNER-READINESS-COMPOSITION` |
| C5M42 | C526 | direct-insert the canonical C2 package row | `C526-REAL-MANAGED-C1-C4-CHAIN` |
| C5M43 | C527 | retain predecessor hash in exactly one snapshot tripwire | `C527-THREE-TRIPWIRE-LOCKSTEP` |
| C5M44 | C528 | change migration Down target from disposable database to shared template | `C528-DISPOSABLE-MIGRATION-ONLY` |
| C5M45 | C529 | remove `RevocationReason` assignment from one baseline predecessor revoke SQL statement | `C529-STRICT-REVOCATION-REASON-PREDECESSOR` |
| C5M46 | C530 | erase `C5-C2-PRELOCK-CLOCK-NONCLAIM-01` from carried register | `C530-DEBT-AND-NONCLAIM-ACCOUNTING` |
| C5M47 | C521 | acquire key before managed identity | `C521-GLOBAL-LOCK-ORDER` |
| C5M48 | C521 | move fresh clock before blocking locks | `C521-POSTLOCK-FRESH-CLOCK` |

The historical v0.5 sweep was C5M01 through C5M48. The v0.6 ordered sweep is
the exact allocation in §19. Stop immediately on GREEN,
wrong-bite RED, anonymous RED, non-byte-exact restore or residue. Later
mutations do not run until the current owner is restored and canonical PASS.

## 14. Validation policy and execution order

### Task 0

1. verify baseline/branch/staged zero and preserve unrelated dirt;
2. verify Scope, authority and bundle hashes;
3. census allowlist exactly 47 unique normalized paths;
4. verify identifiers <=63 bytes and exact 5/8/6/1+1 census;
5. verify C2/C3 lock statements and Restrict FKs remain landed;
6. provision the 11th LOGIN before migration Up;
7. verify `.gitattributes` and three predecessor tripwire pins;
8. census exactly eight predecessor Revoked SQL writes requiring explicit
   `RevocationReason` and fail if another equivalent write exists;
9. prove the resolved-credential authenticator, persisted C5 policy services
   and dedicated manager secret/connection contract fit paths 10/15/18 without
   changing `LocalDevApiKeyValidator.cs` or creating an Infrastructure -> API
   dependency;
10. derive the §12 discriminator/control sets and §13 mutation-target set,
    require exact equality of discriminators and targets, zero overlap, zero
    unclassified observations and the complete control register.

### Build and affected validation

```text
contracts/DTOs -> application authorization -> codec/profile
-> model/config/migration -> SQL/ACL -> repository/readiness/composition
-> targeted unit/contract/architecture
-> targeted C5 integration positive controls
-> affected auth + C2/C3/C4 tests
-> ordered mutation sweep over the exact §19 allocation
-> Release build 0 warnings / 0 errors
-> complete unfiltered Release suite exactly once on frozen final bytes
-> exact-hash freeze and review-only closeout bundle
```

Do not run B4/DK-PROD on each apply unless a changed shared guard, migration,
role, model or auth surface affects them. They remain part of the one final
unfiltered suite. No retry-on-failure or filtered substitute closes that suite.

The final suite is GREEN only with zero failures in Contract, Architecture,
Unit and Integration and only the canonical intentional golden-vector skip.
Docker anonymous-volume count must not increase across the targeted PostgreSQL
harness validation; do not prune or delete pre-existing volumes.

## 15. STOP/RRI conditions

STOP before or during a future build if:

- a 48th permanent path, tenth function, sixth table, seventh index, 31st proof
  or 49th mutation is required;
- any Round-1 product/trust decision must reopen;
- C1-C4 semantic code must change rather than be exercised as a predecessor;
- planned rotation waits, polls or performs provider I/O while holding K1;
- C2 pre-lock-clock exposure is no longer bounded by a short C5 hold;
- a mutation stays GREEN, REDs at a neighboring/anonymous bite or cannot restore
  byte/catalog state exactly;
- a named §12 observation is unclassified, a discriminator lacks an exact §13
  mutation target, a mutation targets a control, or a control lacks a bounded
  reason in §12.1;
- a catalog negative needs a cluster role inside the owned namespace or leaves
  a cluster object behind;
- exact managed PrincipalId cannot be proven without derivation/default;
- managed policy resolution requires editing `LocalDevApiKeyValidator.cs`, or
  manager configuration/secret resolution requires an appsettings/secret path
  outside the existing 47-path design;
- canonical evidence requires LocalDev, direct-seeded C2, fake C3/C4 or private
  recipient key handling;
- per-principal package ownership, inbox/notification, re-key/replacement,
  production Raw BIO, deployment or pilot activation becomes necessary;
- either independent review artifact is missing.

Required dispositions are respectively `STOP/RRI`,
`STOP/RRI — CROSS-SLICE SEMANTICS REQUIRED`,
`STOP/RRI — C5-C2-PRELOCK-CLOCK DEFECT PROMOTION REQUIRED`, or
`REVIEW_CHAIN_INCOMPLETE`.

## 16. Closeout and debt disposition

At controlled closeout only, after exact-byte full-suite GREEN:

```text
C4-AUTH-SCOPE-PROVISIONING-DEBT-01
  may close only if C505/C508/C526 pass on final managed PostgreSQL bytes.

C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01
  may close only if C503/C507/C508 prove persisted identity -> credential ->
  Postgres store -> validator -> AuthenticatedClientContext.

C5-C2-PRELOCK-CLOCK-NONCLAIM-01
  remains OPEN / PREDECESSOR_OWNED unless separately repaired; C512/C521 prove
  only that C5 does not lengthen the exposure with a post-key-lock wait.
```

Carry all other Scope §18 debts/nonclaims. C5 closeout is technical activation
readiness, not Product Ready. Production Raw BIO adapter/evidence and the
deployment/hospital-pilot activation packet remain required.

## 17. Round-2 reviewer questions

Each independent reviewer must answer:

1. Are five tables, one key column, nine functions and one role pair sufficient
   and no broader authority hidden?
2. Does resolved-credential routing preserve both same-client unrelated allow
   and deny behavior while enforcing C5 identity/policy/credential state?
3. Is the explicit PrincipalId chain non-derived and replacement-stable?
4. Is the exact two-scope overlay distinct from broader client policy?
5. Are one-time secret/replay/recovery semantics executable without storing
   plaintext?
6. Are SPKI, fingerprint, historical non-reuse and version rules exact?
7. Does the global lock order remain compatible with landed C2/C3?
8. Does planned rotation perform no waiting operation after K1 lock?
9. Do C512/C514-C517 prove every landed hard-cutover consequence without
   changing C2/C3/C4 semantics?
10. Are emergency revoke and replacement recovery correctly separate?
11. Are audit and idempotency codecs unambiguous and independently reproducible?
12. Are role/ACL/catalog sets finite and two-way exact?
13. Does readiness aggregate predecessor owners without duplicating them?
14. Is the 47-path allowlist complete, including pre-Up LOGIN, LF and all three
   raw-byte tripwires?
15. Does every C501-C530 row include a real fixture, positive control,
   one-dimensional mutation, named bite and restore?
16. Are all 78 allocated §19 mutations individually discriminating at their owner?
17. Is the real managed C1->C2->C4->C3 proof free of LocalDev/direct seeds/fakes?
18. Are all Product Ready and predecessor nonclaims preserved?

## 18. Current authorization state

```text
C5 Scope v0.3:              RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.6:           PHASE_B_REVIEW_CANDIDATE
Allowlist:                  47 exact permanent paths
Durable delta:              5 tables / 1 existing column / 6 indexes
SQL/roles:                  9 functions / 1 capability / 1 LOGIN
Requirements/observations: 279 / 156
Discriminator/control:     77 / 79
Proofs/mutations:           30 / 78
v0.1 review artifacts:      COMPLETE 2 OF 2 / CHANGES_REQUIRED
v0.2 successor reviews:     COMPLETE 2 OF 2 / SPLIT VERDICT
v0.3 successor reviews:     COMPLETE 2 OF 2 / SPLIT VERDICT
v0.4 successor reviews:     COMPLETE 2 OF 2 / CHANGES_REQUIRED
v0.5 successor reviews:     COMPLETE 2 OF 2 / PHASE-A REQUIRED
Phase-A R8 technical:       COMPLETE 2 OF 2 / PASS
Phase-B v0.6 reviews:       PENDING 0 OF 2
Round 3 authority:          NONE
Implementation authority:  NONE
Stage / commit / push:      NONE
```

## 19. Phase-B normative register incorporation

Sections 19-22 are the authoritative v0.6 successor. The exact auth, HTTP,
codec, durable schema, SQL transaction, application, audit and proof contracts
reconciled directly in §§2-9 and §19.1 are current normative text. Where an
earlier proof, taxonomy, mutation, fixture or Task-0 statement differs, this
successor controls. The following artifacts are normative parts of this
executable specification and
must be carried beside the Dispatch in every v0.6 review bundle. A missing file
or SHA mismatch is `STOP/RRI — REGISTER DENOMINATOR UNSOUND`.

| Artifact | Rows / purpose | SHA-256 |
| --- | --- | --- |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R8.tsv` | 256 Scope requirements | `47D39359E372410971B99641758F80823603D8E7AF75D99733E979D83862F6DB` |
| `requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R8.tsv` | 23 Phase-B/R7 authority requirements | `8DBB62180859C91A3A6A28ADDA47DD8322DE394AB5FC9506414DB40054294F61` |
| `requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv` | 156-observation inverse map | `C376570FE798EB1971FBBFF8964FE05654AA2EC53976EB830E681DAEA000EDD7` |
| `requirements/ALL_OBSERVATION_NATURE_AUDIT_R8.tsv` | 77 D / 79 C nature audit | `6AC12C59B26E207F06B099A89CCA628298E5FA6F6710648FD850CD298FEEAC14` |
| `requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv` | 78 allocated semantic dimensions | `350EAD7DB8382EBCF6551CDD49D688A64D2FF32F7753FAF06251B151444B9490` |
| `requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R8.tsv` | corrected C5M01-C5M48 dimensions | `E5BF10EC6D79B86697E0CFD456F9F857BFC96A0E35677CFD07E2A9ABA64D2DC4` |
| `requirements/MUTATION_DEMOTIONS_R8.tsv` | C5M65/C5M78 non-allocation | `4478F7F846C4FB50FD6A6B2BF909527E795E11E63616F25FB0A852F69D0D714C` |
| `requirements/AUTHORITY_CLAUSE_CLASSIFICATION_R8.tsv` | 19/19 successor clauses classified | `AFAC07C21C707BBD90F297D359DC4C2D59F76079C18FF61833D3F4E4A8595031` |
| `requirements/SEMANTIC_RECONCILIATION_R8.md` | isolation and finding dispositions | `01D0C4232E8078F0CE9F9976F519C638FDFBFFEAE2706376B507D982A88C9BD7` |
| `requirements/DEMOTION_LOAD_BEARING_CONSEQUENCE_R8.md` | C529 predecessor preservation | `F184EA1988089BA9E85F3F96FAE0A3802D4144871F8915A24B5A919DE857A331` |

The exact authority universe is the union of the two requirement registers.
Every requirement maps to at least one named observation. Every named
observation maps back to at least one requirement and exactly one proof owner.
The observation inverse map and nature audit together are the complete proof
manifest. The mutation allocation is the complete mutation manifest.

For every `D`, the owning proof must construct a canonical positive fixture,
assert the intended negative state differs in exactly the registered dimension,
apply only `ExactMutationEdit`, require the registered `NamedBite` to RED with
neighboring predicates active, restore byte/catalog state exactly and rerun the
canonical owner. For every `C`, `NatureDecisionBasis` is the bounded positive,
preservation, topology, failure-injection, finite-set or durable-defense
construction; a control is never mutation evidence.

The ordered allocation is exactly:

```text
C5M01-C5M64
C5M66-C5M77
C5M79-C5M80
```

`C5M65` and `C5M78` remain `CONTROL / NOT_ALLOCATED`. There is no C5M81.
Discovery of requirement 280, observation 157, proof 31, path 48 or mutation
dimension 79 is `STOP/RRI — REGISTER DENOMINATOR UNSOUND`.

The final isolation decisions are mandatory:

- C5M10 returns only `C5M10_MUTATION_ONLY_CANARY` and invokes the credential
  secret generator zero times; C5M54 invokes generation before durable replay
  resolution while `PresentedKey` remains null.
- C5M40 removes only mandatory manager `EXECUTE` presence with no unexpected
  grant; C5M80 admits one unexpected-extra grant while mandatory `EXECUTE`
  remains present.
- C5M49 removes only the RSA-size comparator and must RED
  `C509-RSA-SIZE-RANGE-BEFORE-REPOSITORY` with repository calls zero.
- C5M59 omits only predecessor `api_keys.CredentialStatus Active -> Revoked`.
- C5M11 changes only the persisted PrincipalId argument source and REDs before
  SQL/composite-FK execution.
- C5M79 removes only post-lock SQL `ValidUntilUtc > fresh_now_utc`.

### 19.1 Exact Phase-B proof constructions

These constructions are part of the current proof manifest and supersede the
short historical rows in §12 wherever they add Phase-B detail:

- `C501-EXACT-HTTP-SURFACE` reflects the seven POST DTOs and eight routes and
  pins the endpoint's explicit C5-local JSON options: Web naming,
  case-sensitive property matching and unmapped-member disallow. The global API
  options remain unchanged.
- `C502-AUTHORIZATION-BEFORE-BODY`: map a real management POST without a typed
  body parameter. Send malformed JSON under (a) BusinessConsumer with the same
  management scope and (b) OperatorAdmin missing only that scope. First assert
  each actor differs from the positive only in the named category/scope
  dimension; both receive the exact forbidden response with body-parser and
  repository counters zero. C5M51 moves only the parser call before auth and
  must RED this bite.
- `C505-REQUIRED-SCOPE-ROUTING-PRESERVATION`: one PostgreSQL OperatorAdmin
  credential contains management plus a sibling scope. With server-owned
  management `requiredScope`, it uses C5 persisted authority and LocalDev calls
  remain zero. With a sibling endpoint's different server-owned requiredScope,
  the same credential uses the landed global policy path. C5M53 ignores only
  `requiredScope` and must RED the named routing bite.
- `C506-REPLAY-ZERO-SECRET-GENERATION` and
  `C506-REPLAY-PLAINTEXT-ABSENT`: discard a first committed credential response,
  replay the exact idempotency request, assert the immutable metadata snapshot,
  null plaintext and generator calls zero. C5M10 returns only
  `C5M10_MUTATION_ONLY_CANARY` without invoking the generator; C5M54 invokes the
  generator before durable replay resolution while keeping `PresentedKey` null.
  `C506-CANDIDATE-CONFLICT-EXHAUSTION` drives five exact candidate conflicts to
  the named 409 and zero durable rows, where every conflict is independently
  proven to be SQLSTATE `23505` for `IX_api_keys_KeyPrefix`. A separate four-
  conflict/unique-fifth control succeeds and asserts exactly five generator
  calls. Separate `PK_api_keys`, `uq_api_keys_managed_identity`, companion
  version and one-active violations assert zero candidate retries and their
  exact fail-closed `Conflict`/`Unavailable` mapping.
- `C508-CREDENTIAL-REVOKE-NONRETROACTIVE`: authenticate and authorize operation
  A through the real C3 authenticated delivery/response-stream path using the
  managed credential. Pause A only after successful credential authentication
  and authorization and after C3 has acquired its authorized execution context,
  but before response completion. Revoke that credential through the real C5
  manager, then resume the same C3 response stream to completion without
  credential re-resolution. A new C3 or C4 request B using the same credential
  is denied. Authentication/store call counters plus repository/event state
  prove A made no second credential lookup; `C508-NEXT-AUTH-REVOKED` remains the
  next-resolution control and C5M12 remains isolated to companion State.
- `C509-SERVER-OWNED-FIELDS`: after valid OperatorAdmin authentication, send an
  otherwise canonical request separately augmented with `Scopes`,
  `CallerCategory`, `State`, `Revision`, `OccurredAtUtc`, `ManagerPrincipalId`,
  `PrivateKey`, `PackageId`, `DeliveryAction` and `PresentedKey`. First assert
  each member is absent from the owning DTO, then require exact request-invalid
  and repository calls zero for every case. A positive canonical request uses
  the same local options and reaches the repository. Unknown-member rejection,
  not DTO omission alone, is the gating observation.
- `C509-REGISTERED-TIME-AUTHORITY` reads the complete §6.3 landed `api_keys`
  projection and the §5.5.1 lifecycle timestamp table after each applicable
  operation. It asserts the exact server-owned credential reference/type/status,
  canonical scopes, four NULL authority fields and every timestamp equality to
  the persisted `AdmissionAtUtc`; no application/default or second-clock value
  is accepted.
- `C519-TARGET-IDENTITY-FRAMING` independently recomputes the §4 colon-bearing
  absolute vector. `C519-REVISION-AUTHORITY` executes all seven mutating kinds
  and reads the exact named revision row(s) against the §5.5 table.
  `C519-SCOPE-DIGEST-BINDING` executes recipient enrollment plus credential
  issue, replacement and revoke, independently recomputes each empty/active,
  active/active and active/empty transition from the exact scope set, recomputes
  audit-v2 evidence, and compares the committed event bytes. It separately
  proves the operation snapshot equals the exact original public response with
  no audit-only scope fields and that event and operation bind the same
  `OperationId`. The three key events prove both event digest columns remain
  NULL.
- `C520-EXECUTABLE-CLAIM-ROW` pauses after provisional insertion inside a real
  transaction, proves the nullable incomplete CHECK shape is valid but
  invisible externally, then for a credential conflict proves the same claim
  may carry only `AdmissionAtUtc` while every result/outcome field remains NULL,
  and finally completes and commits. A separate non-credential `EnrollKey`
  control pauses after post-lock admission and proves the identical incomplete
  shape is CHECK-valid. The claim insert targets only the exact idempotency
  tuple; a forced unrelated `OperationId` PK collision returns `Unavailable`
  and leaves no state. Injected failure rolls back to absence.
  `C520-HISTORICAL-COMMIT-SNAPSHOT-REPLAY` commits an operation,
  mutates the live lifecycle later, and proves exact replay still equals the
  original snapshot rather than current rows.
- `C521-ABSENT-IDENTITY-LINEARIZATION` races two first enrollments for one absent
  recipient and instruments the initial read-only idempotency probe, exact
  advisory acquisition, mandatory under-authority re-probe, provisional claim
  and fresh clock in that order. C5M50 removes only the absent-identity advisory
  acquisition; the re-probe, tuple and one-active defenses remain and the named
  bite must RED. `C521-POSTLOCK-FRESH-CLOCK` additionally drives four exact
  candidate conflicts followed by a unique fifth candidate, asserts exactly one
  `clock_timestamp()` capture, asserts attempts two through five read the same
  persisted `AdmissionAtUtc`, and proves final validity plus event
  `OccurredAtUtc`, operation `CompletedAtUtc` and every applicable lifecycle
  timestamp in §5.5.1 use that exact value. C5M48 moves the sole capture before
  the blocking locks and must RED the existing named bite.
- `C526-RATIFIED-CANONICAL-SEQUENCE` runs, in order: PostgreSQL manager
  authentication with exact management `requiredScope`; managed identity
  enrollment; first credential issue; recipient authentication to both exact
  C3/C4 required scopes; credential replacement with the same persisted
  PrincipalId; simulated loss of the first replacement response; exact replay
  with null plaintext and zero generator calls; authentication using the
  captured replacement secret with the same PrincipalId; recipient key
  enrollment; aggregate readiness GREEN; real C1; real C2 package preparation;
  real C4 PackageId listing; and real C3 authenticated delivery. The proof may
  retain the replacement secret only in test memory to exercise authentication;
  it does not claim lost-response recovery. No managed identity, credential,
  key, C2 package or predecessor metadata is direct-seeded. Only the opaque
  PackageId crosses from C4 to C3. C5M42 direct-inserts only the canonical C2
  package row and must RED `C526-REAL-MANAGED-C1-C4-CHAIN`.

## 20. Proof ownership and load-bearing predecessor constraint

Proof owners remain exactly C501-C530. The owner of each observation is the
`Owner` column in the inverse map and must equal the observation prefix. No
proof 31 or new `[Fact]` is authorized by this Dispatch.

`C529-DEMOTION-CONSTRAINT-PRESERVATION` is a required control. It must assert
that exact landed constraint
`ck_raw_export_recipient_key_registration_shape` exists and equivalently
enforces both `Revision > 0` and `ValidFromUtc < ValidUntilUtc`. Absence,
rename without reconciliation, weakening or non-equivalent replacement is a
named gating failure. Because this is predecessor byte/catalog preservation,
it remains a control and does not allocate mutation dimension 79.

The 47-path allowlist in §10 remains exact and unchanged. Review/register/
bundle artifacts outside the repository are evidence, not permanent product
paths. A required permanent path 48 is a denominator STOP.

## 21. Fresh Dispatch-v0.6 Task-0 contract

The Phase-B bundle must carry `tools/verify_dispatch_v06_task0.py` and its
fresh JSON output. The verifier must read the actual Dispatch bytes, parse the
machine contract below, hash every incorporated register, and recompute all
sets and gates. Copying the Phase-A JSON is prohibited.

<!-- C5_PHASE_B_MACHINE_CONTRACT_BEGIN -->
```text
CONTRACT_VERSION=0.6
BASELINE=4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f
SCOPE_PATH=repo/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_scope_brief.md
SCOPE_SHA256=7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5
ALLOWLIST_PATH=metadata/ALLOWLIST_47.txt
ALLOWLIST_SHA256=CD6D1BFB385BC1B6213E650080E23AB41CC935A124DE10192C95E87DEC7F47E7
SCOPE_REGISTER_PATH=requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R8.tsv
SCOPE_REGISTER_SHA256=47D39359E372410971B99641758F80823603D8E7AF75D99733E979D83862F6DB
AUTHORITY_REGISTER_PATH=requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R8.tsv
AUTHORITY_REGISTER_SHA256=8DBB62180859C91A3A6A28ADDA47DD8322DE394AB5FC9506414DB40054294F61
OBSERVATION_REGISTER_PATH=requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv
OBSERVATION_REGISTER_SHA256=C376570FE798EB1971FBBFF8964FE05654AA2EC53976EB830E681DAEA000EDD7
NATURE_AUDIT_PATH=requirements/ALL_OBSERVATION_NATURE_AUDIT_R8.tsv
NATURE_AUDIT_SHA256=6AC12C59B26E207F06B099A89CCA628298E5FA6F6710648FD850CD298FEEAC14
MUTATION_REGISTER_PATH=requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv
MUTATION_REGISTER_SHA256=350EAD7DB8382EBCF6551CDD49D688A64D2FF32F7753FAF06251B151444B9490
CORRECTED_ALLOCATION_PATH=requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R8.tsv
CORRECTED_ALLOCATION_SHA256=E5BF10EC6D79B86697E0CFD456F9F857BFC96A0E35677CFD07E2A9ABA64D2DC4
DEMOTION_REGISTER_PATH=requirements/MUTATION_DEMOTIONS_R8.tsv
DEMOTION_REGISTER_SHA256=4478F7F846C4FB50FD6A6B2BF909527E795E11E63616F25FB0A852F69D0D714C
AUTHORITY_CLAUSE_REGISTER_PATH=requirements/AUTHORITY_CLAUSE_CLASSIFICATION_R8.tsv
AUTHORITY_CLAUSE_REGISTER_SHA256=AFAC07C21C707BBD90F297D359DC4C2D59F76079C18FF61833D3F4E4A8595031
SEMANTIC_RECONCILIATION_PATH=requirements/SEMANTIC_RECONCILIATION_R8.md
SEMANTIC_RECONCILIATION_SHA256=01D0C4232E8078F0CE9F9976F519C638FDFBFFEAE2706376B507D982A88C9BD7
LOAD_BEARING_EVIDENCE_PATH=requirements/DEMOTION_LOAD_BEARING_CONSEQUENCE_R8.md
LOAD_BEARING_EVIDENCE_SHA256=F184EA1988089BA9E85F3F96FAE0A3802D4144871F8915A24B5A919DE857A331
EXPECTED_REQUIREMENTS=279
EXPECTED_SCOPE_REQUIREMENTS=256
EXPECTED_AUTHORITY_REQUIREMENTS=23
EXPECTED_OBSERVATIONS=156
EXPECTED_DISCRIMINATORS=77
EXPECTED_CONTROLS=79
EXPECTED_MUTATIONS=78
EXPECTED_PROOF_OWNERS=30
EXPECTED_PERMANENT_PATHS=47
EXPECTED_AUTHORITY_CLAUSES=19
EXPECTED_DEMOTIONS=C5M65,C5M78
EXPECTED_MUTATION_IDS=C5M01-C5M64,C5M66-C5M77,C5M79-C5M80
LOAD_BEARING_CONSTRAINT=ck_raw_export_recipient_key_registration_shape
PHASE_B=REVIEW_CANDIDATE_ONLY
ROUND_3_AUTHORITY=NONE
IMPLEMENTATION_AUTHORITY=NONE
```
<!-- C5_PHASE_B_MACHINE_CONTRACT_END -->

The fresh output must set all of these gates to true:

```text
every_union_requirement_has_observation
every_observation_has_union_requirement
semantic_assignment_observations_subset_named
nature_audit_denominator_complete
every_discriminator_has_mutation
every_mutation_has_requirement
authority_clause_denominator_complete
artifact_hash_bindings_match_dispatch
mutation_dimensions_unique
proof_owners_exact
permanent_paths_exact
demotions_exact_and_not_allocated
exact_contract_reconciliation_present
historical_sections_explicitly_non_executable
```

Any false gate is STOP/RRI. Task-0 does not authorize implementation or tests.

## 22. Phase-B completion boundary

This document and its bundle are review candidates only. Builder completion is
limited to freezing exact bytes, calculating hashes, proving mechanical
integrity and sending the same ZIP/SHA to both reviewers.

```text
Phase B closure:            NOT SELF-CLAIMED / DUAL REVIEW PENDING
Round 3 authority:          NONE
Implementation authority:  NONE
Tests:                      NOT RUN / NOT AUTHORIZED
Stage / commit / push:      NONE
Merge / PR / deployment:    NONE
Production activation:      NONE
```
