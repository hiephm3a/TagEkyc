# TIP-88C1-C6B-A1 — Server Identity and Control Foundation Dispatch

> R27 successor with bounded A1 continuation, 2026-09-12. Original R27 authority is Homeowner attachment 033da395-183e-45b6-b716-7d5816d14594; current R26 authority is attachment 6d84e902-daa1-4688-9689-0aa98d387b0f and its canonical-correction addendum. Preserved predecessor `tip_88c1_c6b_a1_server_identity_control_dispatch.md` SHA `BB3B2AEED01753F563B5DC56720172DBD9EB1EA27EE88EEBA11F65038CCB2881`. Current canonical text incorporates the already-authorized typed-gateway, startup/global-pepper and cancellation corrections, not only R27. Bounded as-built correction f53454e8-7c46-48fc-b592-d733f0c545ea names HISTORICAL_LANDED_CANCEL without changing product behavior. The separate report is tip_88c1_c6b_a1_as_built.md; execution results do not belong in this normative catalogue. This does not claim full A1 completion or grant production/stage/commit/push.

## R27 amendment catalogue / exact changed bytes

- Umbrella grammar successor: `tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md`, SHA `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062`.
- Operation Master successor: `tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md`, SHA `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`.
- CRT1 micro-RRI successor: `tip_88c1_c6b_a1_crt1_exact_signed_bytes_contract_rri_r27_metadata_v1.md`, SHA `6131ABABCAE0415AA899D6997E9A4375398CCAF0AD743F64E16517625D97A61A`.
- Transition reference-only successor: `tip_88c1_c6b_a1_transition_sql_01_root_enrollment_lifecycle_r27_metadata_v1.md`, SHA `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102`; SQL fenced blocks unchanged.
- Transition reference-only successor: `tip_88c1_c6b_a1_transition_sql_02_rotation_catalog_r27_metadata_v1.md`, SHA `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115`; R11/R13 typed bigint literals corrected to match the declared result; transition semantics unchanged.
- Transition reference-only successor: `tip_88c1_c6b_a1_transition_sql_03_capability_binding_readiness_r27_metadata_v1.md`, SHA `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F`; R24/R25, R28 and R26 corrections reconciled with tested Foundation under bounded authority.
- Transition reference-only successor: `tip_88c1_c6b_a1_transition_sql_04_auth_nonce_credential_revoke_r27_metadata_v1.md`, SHA `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C`; SQL fenced blocks unchanged.

| Product/proof path | Checkpoint SHA | Current SHA |
| --- | --- | --- |
| `src/TagEkyc.Api/CaptureRuntimeEndpoints.cs` | `3E985A93C768848DCB96B045B337A88CB329FB97F0752BD51E9C1CFD6BF4B1C5` | `B6DF0DB351DD3428E6474E0C0DE05F832F6C840B90C79BA5660F6690E95557EB` |
| `tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs` | `C3E36CAC98572189B96747FD69505532B3C53A14C4F03F9DB4D5756C98F705AE` | `D9E13128E303D4D7E8D9B93B45DA015FEE71657D3C11124DA238FA9DB552F482` |

The ten metadata members and literal vector are in the Operation Master
successor. R27 uses only `ingress=IngressMetadataSha256=<64 lowercase hex>`.
The predecessor checkpoint STOP about a missing grammar slot is closed by this
Homeowner amendment; the checkpoint report is historical evidence and stays
unchanged. R27 HTTP/N/A3 success and 400/403/503 proofs remain Stage-2 implementation
obligations now measured through the full IntegrationTests run below, not inferred from parser/crypto alone.

The previously ratified typed-gateway v0.9 and migration-principal v0.3 remain
operative within their scopes; their predecessor parent/OM references are
historical authority lineage, not a requirement to execute an old R27 grammar.
Current bounded continuation does change the authorized SQL/function identities
and implementation guards stated in the canonical master and companion; the
former R27-only no-SQL-change statement is historical, not operative. It does
not grant production activation; the as-built reconciliation below records measured candidate evidence.


**Version:** v0.13 — R27 successor / bounded A1 as-built reconciliation 2026-09-12  
**Authority:** A1 IMPLEMENTATION AUTHORITY RESTORED on this exact-byte catalogue; stage/commit/push/A2/A3 NOT GRANTED  
**Parent:** C6B-A Planning v1.8 SHA `DACA8141CC68E03AAB9D858C547DBC4FAB7A542E6CAD0F66AB43E37116B0F0D6`
**Split checkpoint authority:** non-executable umbrella v0.5 SHA `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062`

## Normative catalogue binding

This parent is an index and scope fence. The sole executable A1 dispatch
catalogue is the exact union below; duplicated route, schema, function, ACL,
lock, result and proof prose later in this file is a derived review aid and
MUST NOT override, extend or repair that union:

| Catalogue member | Exact SHA-256 |
| --- | --- |
| Planning authority | `DACA8141CC68E03AAB9D858C547DBC4FAB7A542E6CAD0F66AB43E37116B0F0D6` |
| Versioned verifier-pepper RRI v2 | `FFECB648A0FA5F654C0765CC0927E38C95BD9489920F49A88E491E15B8E470F0` |
| Exact verifier-pepper SecretRef resolution amendment A2 | `6CB725C1217DDDA5E59CD65393FCC62953F36FA7C003B71460B1DAD09377F09A` |
| R01/R02 root request fingerprint RRI | `509D3951F08830539150B35A88E2BAF709CB1E297831A45D841E8927CBE5A42B` |
| CRT1 exact signed-bytes micro-RRI | `6131ABABCAE0415AA899D6997E9A4375398CCAF0AD743F64E16517625D97A61A` |
| CRT1 proof-owner project-boundary correction | `A79601F3944DAD71F12FCEABEE6B6BE6E1DACAC88C4E134BECF44B3D5519FFB5` |
| Literal DDL master | `8FB6C4A608F99641915C44E291C142DFDB2282E93116BC7CD73DCF1FEF866CB1` |
| Operation/function master | `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018` |
| Transition SQL 01 | `13097C3650C8874501EDB9378CFDA009D72ABB422A093ABCA42EBA3E46D79102` |
| Transition SQL 02 | `BF6892BC320C2F7DE8FCD68EC6F1A9D48BFE0FBCA005978AA2604093492C0115` |
| Transition SQL 03 | `771BA6AE1C63147B70C4DF3E22B267E5E87A1ED85F6ABF027EE3869121D20B7F` |
| Transition SQL 04 | `8B5C9336255E623862699650F637F5D56D353A8C7FE030E4A03E88CAF622EE3C` |
| PostgreSQL 16 core mutation evidence | `756E2420ED9E8C9480D4F469CC971BDFB847506B8733B602BA84A81663330E5B` |
| PostgreSQL 16 runtime evidence | `04A86A6C4F1C2002BAFDCA73CAE1CDF3ADE4BB546D23C14759D387015220BACE` |
| PostgreSQL 16 identity/audit evidence | `678078E228A299EBA5EA44B0012A891A0A7B45D2FB9F7A95B07574019C08625E` |
| PostgreSQL 16 expiry-on-denied evidence | `5E324200F58B9D0710C5C201942612D2D8B262E1CAD156807FBCD90485591F8F` |

The identity/audit proof is executable bound evidence for the sixteen named
lineage, event-equality and capability-graph mutations. R24/R25 reuse the landed neutral
capture/evidence append boundaries named by the operation master; the obsolete
placeholder SQL names `capture_runtime_apply_capture_artifact` and
`capture_runtime_apply_evidence_result` below are non-operative and MUST NOT be
implemented. Any set mismatch, stale member hash or attempt to infer authority
from the derived body is a dispatch failure.

## Scope

Server-only, platform-neutral foundation: literal schema/migrations for platform
credentials, runtime registrations/installations/generations, bootstrap,
request nonce, role/trust/config catalogs, capability/binding, rotation and
append-only operation/event ledgers; exact API records; CRT1/ENROLL1/ROTATE1;
two-transaction nonce; neutral capture/evidence wrappers. Synthetic data only.

Excluded: Agent CNG/loopback, broker R1-R6, deployment LOGIN creation/secrets,
operational cutover, SDK, production. A1 does own sentinel schema/readiness,
compiled conditional route mapping/unmapping, runtime-specific online/operator
connection selection, public Raw runtime auth/nonce and pre-broker DTO, plus
offline tool code for bootstrap issue/revoke and cutover SQL invocation.
Database tests use ephemeral test LOGINs;
production callability is A4.

## Verified reuse-before-invention census

| Surface | Classification | Exact evidence |
| --- | --- | --- |
| Client context/auth | `REUSE_AS_IS` for Client routes only | `AuthenticatedClientContext.cs`; never add runtime/operator fields |
| session/cancel | `EXTEND_EXISTING` | `VerificationSessionEndpoints.cs`; `EfVerificationFinalizationBoundary.cs` |
| capture/evidence validation | `REUSE_CORE_AFTER_EXTRACTION` | `VerificationEvidenceApplicationService.cs`; current Client/allowed-Agent wrapper is not reusable |
| atomic append/idempotency | `EXTEND_EXISTING` | `EfAppendIdempotencyBoundary.cs` |
| API-key cryptography | `REUSE_PATTERN_ONLY` | `ApiKeyProvisioningService.cs`, `ApiKeyHasher.cs`, `PostgresHashedApiKeyStore.cs` |
| platform operator credential/context | `ACTUAL_MISSING` | no `teo_` partition/context/authenticator exists |
| runtime registry/install/generation/nonce | `ACTUAL_MISSING` | nullable artifact Agent/Device observations are not a registry |
| trust/role/config catalogs | `ACTUAL_MISSING` | current C6B provider is appsettings/allowed-set candidate only |
| bootstrap/rotation/capability/binding | `ACTUAL_MISSING` | no reusable durable aggregate/service exists |
| sentinel/readiness | `ACTUAL_MISSING / EXTEND` | add A1 readiness; do not overload C6B authority |

## Frozen ownership and A3 seam

A1 owns public platform/runtime routes, authentication/nonce, capability/bind,
neutral capture/evidence cores, foundation schema, Prepared sentinel and compiled
conditional route mapping. A4 owns production LOGINs/secrets, quiescence,
activation and deployment.

For `POST /api/ekyc/raw-export/source-ingress`, A1 owns only canonical headers,
CRT1 transaction N and an immutable authenticated runtime context passed to
`ICaptureRuntimeRawIngressAdmission`. That context is authentication evidence,
not authorization/admission evidence. A1 performs no BindingId/session/
acceptance lookup. Request.Body remains unread. A3 alone implements that port,
transaction-B fresh binding/role/acceptance checks, private broker transport,
R1 and R2-R6.
Without A3 registration the route is unmapped/NOT_READY; no production stub.

```csharp
public interface ICaptureRuntimeRawIngressAdmission
{
    ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context,
        Stream body,
        CancellationToken cancellationToken);
}
```

The API passes `HttpRequest.Body` as an opaque stream with ReadCount zero. No
Application contract may reference `HttpRequest`, `HttpContext`, `IResult` or
another ASP.NET type. The context contains only CaptureAgentId,
DeviceInstallationId, stable CredentialId, CredentialGeneration,
RolePolicyId/Revision, canonical route role, SignedAtUtc, 32-byte Nonce,
RequestFingerprint and canonical signed raw-ingress metadata committed by N. It
contains no ClientApplicationId, ApiKeyId, Binding/session/acceptance/capacity
result, broker token or source-authority decision.

## Authentication and transaction contract

Presented platform key is exactly 47 ASCII octets: literal `teo_` followed by
exactly 43 canonical unpadded base64url characters decoding to exactly 32 CSPRNG
bytes. Reject non-ASCII, whitespace, `=`, noncanonical encoding, case folding,
extra separators and every other length. `KeyLookupPrefix` is the deterministic
first 12 characters of the canonical 43-character payload; it is a nonsecret
index projection, not another wire segment and never authenticates. Digest is
`HMAC-SHA256(K_domain[VerifierPepperVersion, PlatformCredential],
ASCII("platform-credential-v1") || 0x00 || decoded-secret-32)` and all 32 bytes
are compared fixed-time. `K_pepper_version` in older shorthand means this
derived domain key, never raw `K_master`. Mutable decoded and
presented buffers are zeroed.

### Capture Runtime versioned verifier-pepper authority

The bound self-contained RRI v2 is the sole normative authority for the codec,
master material, HKDF, three digest domains, CurrentVersion, R01 adapter and
pepper lifecycle. Earlier Planning/umbrella requirements for offline
`capture-runtime-verifier-key provision|rotate|retire|readiness`, deployment
CurrentVersion CAS and callable pepper lifecycle races are **SUPERSEDED /
NON-OPERATIVE FOR A1**. They must not be implemented. Configuration plus A4
deployment ownership, immutable process CurrentVersion, SQL reference scan and
fail-closed readiness are the only operative lifecycle model.

All four textual material classes use exactly one canonical 43-character
unpadded base64url representation that decodes to exactly 32 bytes and passes
canonical re-encode plus ordinal equality. No trim, normalization, repair or
decode-length-only acceptance is allowed. BootstrapSecret and
CaptureCapabilitySecret are exactly 32 CSPRNG bytes; earlier `at least 256 bits`
or equivalent variable-length language is **SUPERSEDED / NON-OPERATIVE FOR
A1**. The 256-bit security floor is preserved while larger wire values are not
members of A1.

`CaptureRuntimeVerifierCryptography.cs` is the one pure Application-owned codec
and KDF implementation. From canonical `K_master[V]`, it uses HKDF-SHA256 with
32 zero salt bytes, 32-byte output and exactly one expand block. Exact ASCII
info strings are `TAG-EKYC-C6BA1-VERIFIER-PEPPER/platform-credential-v1`,
`TAG-EKYC-C6BA1-VERIFIER-PEPPER/bootstrap-digest-v1`, and
`TAG-EKYC-C6BA1-VERIFIER-PEPPER/capability-digest-v1`. The final HMAC messages
are respectively exact ASCII labels `platform-credential-v1`,
`bootstrap-digest-v1`, `capability-digest-v1`, then `0x00`, then the exact
decoded 32-byte presented secret. Raw master material is never the final digest
key. Full golden vectors and tail-bit aliases are frozen by the bound RRI.

The API-hosted authority is the Application-owned
`ICaptureRuntimeVerifierPepperSource` in `CaptureRuntimePorts.cs`. Its
`CurrentVersion` is used for every new API-hosted R03, R20a and legal R20b
digest. `TryResolveAsync(version, domain, cancellationToken)` resolves exactly
that version and `CaptureRuntimeVerifierPepperDomain` and returns an
`ICaptureRuntimeVerifierPepperLease` whose matching `Version`, `Domain` and
`ReadOnlyMemory<byte> Key` expose only the derived 32-byte domain key. Its
mutable backing buffer is process-local, never persisted, logged or returned,
and zeroed on `Dispose`; Application never receives `K_master` or PRK. Durable platform-operator,
bootstrap and capability rows are always verified using their own
`VerifierPepperVersion`; current-version, try-current-then-old, try-all and
Client API-key-pepper fallbacks are forbidden. An unavailable exact version is
a dependency failure (`NOT_READY`/existing 503 mapping), not a credential
mismatch.

The API-hosted closed configuration section is exactly
`TagEkyc:CaptureRuntimeVerifierPeppers`, containing only positive
`CurrentVersion` and a finite `Versions` collection whose entries contain only
positive unique `Version` and nonblank `SecretRef`. At least one entry is
required and exactly one matches CurrentVersion. No plaintext-secret or open
extension property is permitted. Infrastructure caches only immutable mapping
metadata and CurrentVersion. Public helper
`CaptureRuntimeVerifierPepperSecretRefResolver`, inside
`CaptureRuntimeVerifierPepperProvider.cs`, is the sole material resolver for
both API provider and R01. It consumes `env:` values exactly and validates
`file:` actual raw bytes as exactly 43 ASCII bytes before conversion; it never
uses the generic newline-trimming material path. The generic resolver remains
unchanged for unrelated domains. Missing, malformed or unavailable material
fails closed. `CaptureRuntimeDatabaseOptions` remains unchanged.
Its exact public static `Resolve(string)` returns the closed status
`Success|ReferenceInvalid|MaterialUnavailable|MaterialInvalid`; Success alone
carries a disposable material lease exposing the exact owned 43 ASCII bytes as
`ReadOnlyMemory<byte>`, every failure carries no material, and disposal zeroes
the backing array. It returns no managed secret string or raw path/file error.

R01 is the sole offline adapter exception. The deployment operator supplies
both `--capture-runtime-verifier-pepper-version <positive Int32>` and
`--capture-runtime-verifier-pepper-secret-ref <SecretRef>`. The provisioner
resolves that ref using the same public exact-preserving helper as the API
provider, hashes and persists the same version, and never uses API appsettings/DI
or Client API-key pepper. No HTTP
caller inherits this exception. A4 owns consistent production values across
R01 and the API host; mismatch is a deployment error, never a fallback trigger.
Context contains nonempty CredentialId/PrincipalId, `OperatorAdmin`, exact scope
`operator.capture-runtime.manage`, prefix, revision and authentication time—no
ClientApplicationId or ApiKeyId.

Runtime CRT1, ENROLL1 and ROTATE1 copy the canonical P-256/SPKI/P1363 protocols
from umbrella checkpoint bytes. CRT1 transaction N verifies exact active current
runtime/install/generation/trust/route role without protected target, inserts
32-byte nonce and commits. Transaction B independently re-locks/rechecks and
mutates target/audit. Revoked key writes no nonce; B rollback never releases N.
The sole route-role exception is active-successor R13 exact committed-operation
replay: N still verifies cryptography/lifecycle/current generation and commits
the nonce, then permits only the exact read-only operation-row lookup described
below. Every state-changing route, including predecessor R13 completion, requires
its frozen route role.

Every runtime transaction uses a new context from
`ICaptureRuntimeDbContextFactory` and the named runtime-online connection
profile. It must not resolve the ordinary scoped `TagEkycDbContext`. Production
credentials remain absent; A1 integration tests inject ephemeral operator and
online logins. A4 later supplies production identities/secret references.

Create exact interfaces `ICaptureRuntimeDbContextFactory` and
`ICaptureRuntimeOperatorDbContextFactory`; each exposes
`ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken =
default)`. `CaptureRuntimeDatabaseOptions.SectionName` is exactly
`TagEkyc:CaptureRuntimeDatabase`, with only `OnlineConnectionString`,
`OnlineConnectionStringSecretRef`, `OperatorConnectionString` and
`OperatorConnectionStringSecretRef`. Tests may use direct values; production
requires both secret references and forbids direct values. Each factory holds
separately built immutable `DbContextOptions<TagEkycDbContext>` and returns a new
await-disposed context. It may not resolve the ordinary scoped context, ordinary
options, `IDbContextFactory<TagEkycDbContext>` or `IServiceScopeFactory`.
Online, operator and ordinary connection strings must be nonempty and pairwise
distinct after semantic normalization with `NpgsqlConnectionStringBuilder`, not
raw string comparison. Readiness opens each and verifies the expected distinct
`session_user`/`current_user`; equivalent reformatted connections, a shared login
identity or fallback fail startup/readiness.

## Closed API rules and operation ledger

New CaptureRuntime JSON rejects unknown/duplicate members and trailing bytes. Mutation idempotency,
except the explicit R26 compatibility route, exists only in one lower-case UUIDv4-N
`Idempotency-Key` header, never duplicated in body. R26 preserves the landed Client DTO,
accepts/parses/consumes no such header, and uses only its SQL-derived session identity
and exact internal raw 32-byte fingerprint in the Operation Master R26 section.
Default new CaptureRuntime JSON ceiling is 16 KiB; configuration publication is 32 KiB;
GET has no body. `Content-Type` is exactly `application/json`. Secret responses
are no-store/no-cache/no-redirect. Outcomes: 400 `REQUEST_INVALID`; 403
`ACCESS_DENIED`; 404 `RESOURCE_NOT_AVAILABLE`; 409 `CONFLICT` or
`EXISTING_MATCH_SECRET_UNAVAILABLE`; 503 `NOT_READY`; error body contains only
code+correlationId.

| Route/operation | Closed semantic contract and owner |
| --- | --- |
| offline platform provision/revoke | PrincipalId, expiry, idempotency; secret once; dedicated service, no Client FK |
| POST operator bootstrap issue/revoke | Managed + exact trust/role/config revisions, attestation digest, capped 10m; digest/event atomic |
| POST runtime enrollment redeem | issuance+secret, CandidateKeyId, SPKI/thumbprint, time/nonce/ENROLL1; atomic runtime/install/gen1 activation; exact replay lineage |
| POST operator suspend/reactivate/revoke/retire | runtime ID, expected revision, closed reason; guarded lifecycle CAS |
| POST operator credentials/revoke | exact runtime/install/credential/generation/revision |
| POST operator rotations + revoke | exact current lineage; capped 10m authorization |
| POST runtime rotation complete | one presented CRT1, two pure fingerprint candidates, one atomic classifier/N; selected predecessor+ROTATE1 cutover or exact successor read-only replay; no authentication fallback/reconcile route |
| POST trust/role/config publish | immutable revision + head CAS; role enum exactly Bind,CaptureObservation,TrustedEvidence,RawIngress,Configuration,CredentialRotation |
| POST role/config assign | exact published revision/runtime CAS; role next-generation only; config override reducing only |
| GET operator readiness | exact lifecycle/current generation/catalog/pepper/nonce/sentinel facts; no local-host facts |
| POST session capture-capabilities | Action Issue or Replace; Replace requires exact ActiveUnbound ID+revision; fixed 5m, 32-byte secret once |
| POST runtime executions/bind | capability ID+secret+BindOperationId; singular binding; durable ExecutionExpiresAtUtc is exactly min(CapabilityExpiresAtUtc, SessionExpiresAtUtc); credential/runtime/install/trust/config are fresh predicates, not binding-expiry terms |
| POST runtime executions/reconcile | exact lineage read only; no secret/state/event |
| GET runtime self/configuration | effective closed config + business revision + opaque ETag |
| POST runtime capture-artifacts | BindingId; caller Agent/Device absent; neutral core gets server-derived identities |
| existing evidence-results after cutover | CRT1 TrustedEvidence; compatibility Agent/Device absent; accepted-evidence semantics unchanged |
| existing Client cancel | same finalization B terminalizes ActiveUnbound or previously Bound capability, applies expiry first and preserves immutable binding history; replay retains first-winner metadata |
| sentinel | A1 creates/reads Prepared/Activated model; only A4 activates/deploys |

## Ledger 2 — literal durable schema catalogue

All tables are `tagekyc`, owner existing deployer, all FKs RESTRICT, revisions
bigint >0, times timestamptz, digests bytea length 32, state text under exact
CHECK, no cascade/JSON/open bag/wall-clock partial index.

1. `platform_operator_credentials`: `CredentialId uuid PK`,
   `KeyLookupPrefix varchar(12) COLLATE C UNIQUE` base64url CHECK,
   `SecretDigest bytea(32)`, `VerifierPepperVersion int>0`, nonzero
   `PrincipalId uuid`, `Scopes text[]` exactly sorted singleton
   `operator.capture-runtime.manage`, `State` Active/Revoked,
   `Revision bigint`, `IssuedAtUtc`,`ExpiresAtUtc`,`RevokedAtUtc?`; exact sparse
   lifecycle, expiry>issue, immutable authority fields.
2. `capture_runtime_registrations`: `CaptureAgentId uuid PK`, `RuntimeType`
   Managed, `TrustProfileId/Revision`, `ConfigurationId/Revision`, nullable paired
   `NextRolePolicyId/Revision`, nullable `ConfigurationOverrideId`, lifecycle
   Active/Suspended/Revoked/Retired, `Revision`,`CreatedAtUtc` and sparse
   suspension/revoke/retire timestamps; Agent PK, exact trust/config refs,
   nullable next-role pair, reducing override ref, Active/Suspended/Revoked/
   Retired sparse lifecycle.
3. `capture_runtime_installations`: `DeviceInstallationId uuid PK`, Agent FK,
   nullable paired `CurrentCredentialId uuid/CurrentCredentialGeneration bigint`,
   lifecycle Pending/Active/Suspended/Revoked/Retired, `Revision`, enrollment and
   sparse terminal timestamps; installation PK, Agent FK, current credential
   pair both-null iff Pending, Active/Suspended/Revoked/Retired; partial one
   Active Managed installation per Agent.
4. `capture_runtime_credential_generations`: `CredentialId uuid`, `Generation
   bigint`, `DeviceInstallationId uuid`, `PublicVerifierSpki bytea(91)`,
   `Algorithm` exact CRT1, `PublicKeyThumbprint bytea(32)`, `CandidateKeyId uuid`,
   `RolePolicyId/Revision`, `ValidFromUtc/ValidUntilUtc`, state
    Pending/Active/Rotated/Revoked/Retired, `Revision`, nullable RotatedAtUtc and
    other sparse terminal times;
   credential+generation PK, exact
   installation alternate key, 91-byte P-256 SPKI, thumbprint unique,
    CandidateKeyId globally UNIQUE, role revision, validity/state. Rotated iff
    RotatedAtUtc is non-null and revoke/retire timestamps are null; Revoked and
    Retired have their own mutually exclusive timestamp. Deferred
   constraint trigger enforces Active installation points to its Active selected
   generation and vice versa.
5. `capture_runtime_bootstrap_issuances`: `BootstrapIssuanceId uuid PK`, unique
   `KeyLookupPrefix varchar(12)`, digest/pepper, Managed, trust/role/config IDs+
   revisions, attestation/request digests, issue operation/operator, issue/expiry,
   state Active/Redeemed/Revoked/Expired, revision, sparse redeemed lineage and
   revoke/expire fields; issuance PK, lookup/digest/pepper,
   immutable trust/role/config/attestation authority, operator/idempotency,
   Active/Redeemed/Revoked/Expired exact sparse result tuple.
6. `capture_runtime_request_nonces`: credential UUID, generation bigint,
   `Nonce bytea(32)`, signed/admitted/purge timestamptz; PK credential+generation+32-byte nonce,
   signed/admitted/purge times with purge exactly signed+150s; immutable; index
   includes purge, credential, generation, nonce.
7. `capture_capabilities`: capability UUID PK, session UUID FK, unique lookup
   prefix, digest/pepper, audience ManagedCaptureRuntime, exact landed challenge
   stored as `varchar(128) COLLATE C`, issued/expiry, state/revision,
   predecessor/successor UUIDs and sparse bound/revoke/expire times; capability PK/session FK, digest/pepper/audience,
   challenge, expiry, ActiveUnbound/Bound/Revoked/Expired, unique predecessor and
   successor. Deferred guard prevents cycles/asymmetry/multiple successors and
   any new ActiveUnbound after session ever bound.
8. `capture_execution_bindings`: binding UUID PK; unique session and capability;
   ClientApplicationId, AgentId, installation, credential/generation, thumbprint,
   runtime/install/credential/trust/role revisions, exact challenge
   `varchar(128) COLLATE C`, bound/expiry times and BindOperationId; frozen client/
   Agent/install/generation/thumbprint/revisions/challenge/horizon/operation;
   immutable.
9. Exact tables are `capture_runtime_role_policy_revisions/heads`,
   `capture_runtime_trust_profile_revisions/heads`, and
   `capture_runtime_configuration_revisions/heads`. Revision tables use
   `(CatalogId,Revision)` PK; heads use CatalogId PK+composite current FK.
   Role rows contain sorted unique closed role array and effective/publisher
   fields. Trust rows contain Managed, RetainedRawEnabled,
   AllowTrustedEvidence, RequireHandoffAttestation and effective/publisher
   fields. Configuration rows contain the fields below. Exact revision+head tables for role policy, trust profile and configuration;
   immutable revisions and head expected-revision CAS. Configuration fields are
   exactly `EffectiveAtUtc`, `ExpiresAtUtc`, `RawExportEnabled`,
   `PlaintextBudgetSeconds`, `RawExportSourceClaimSafetyMarginMilliseconds`,
   `CaptureAgentConfigurationPollingIntervalSeconds`,
   `RawExportSourceMaximumChipDg2PortraitBytes` `[1,67108864]`,
   `RawExportSourceMaximumLiveSelfieImageBytes` `[1,67108864]`,
   `RawExportCaptureMaximumAggregatePlaintextBytesPerHost` `[1,2147483647]`,
   `RawExportCustodyMaximumPlaintextWindowBytesPerStream` `[1,16777216]`,
   `RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment`
   `[1,2147483647]`, and `RawExportIngressMaximumPreAdmissionBufferedBytes`
   `[1,65536]`. Freshness requires now+polling+safety<expiry.
10. `capture_runtime_configuration_overrides`: override UUID PK, Agent unique FK,
    base config ID/revision FK, nullable false-only RawExportEnabled and nullable
    copies of every reducible numeric field, revision/update time; one/Agent; nullable false-only
    RawExportEnabled and numeric reductions proven <= referenced base.
11. `capture_runtime_rotation_authorizations`: rotation UUID PK, exact runtime/
    install/current credential+generation, operation/fingerprint/operator,
    authorize/expiry, state/revision, nullable successor CandidateKeyId/SPKI/
    thumbprint/new lineage and sparse terminal time/reason; exact current lineage,
    operator/fingerprint/expiry, Active/Completed/Revoked/Expired sparse
    successor tuple and one Active/install.
12. `capture_runtime_management_operations` PK
    `(ActorCredentialId,OperationKind,IdempotencyKey)` with fingerprint,
    target discriminator/ID, result code/revision/nullable ID and timestamps;
    `capture_runtime_management_events` event UUID PK + operation FK, closed
    event type, actor/target/before-after revision/reason/recorded time. Exact management operation+event tables; immutable fingerprints/results,
    closed operation kinds, append-only events. This ledger excludes bootstrap
    redemption, rotation completion and platform-root operations, which have
    their own exact identities below.
13. `capture_capability_operations` PK
    `(ClientApplicationId,VerificationSessionId,OperationKind,IdempotencyKey)`;
    fingerprint, sparse runtime actor lineage, result capability/binding/revision
    and timestamps. `capture_capability_events` event UUID PK + operation and
    capability FKs, session, closed event, sparse Client/runtime actor,
    before-after revision/recorded time.
14. `capture_runtime_bootstrap_redemption_operations`: PK
    `(BootstrapIssuanceId,RedeemOperationId)`, immutable fingerprint,
    CandidateKeyId, stable result CredentialId/Generation/Agent/installation and
    result code/timestamps. It owns exact R05 response-loss replay; no secret is
    persisted or re-emitted.
15. `capture_runtime_rotation_completion_operations`: PK
    `(RotationAuthorizationId,BusinessIdempotencyKey)`, immutable fingerprint,
    stable CredentialId, predecessor/successor Generation, CandidateKeyId,
    result revisions/state/timestamps. It owns exact R13 successor replay.
16. `platform_operator_root_operations` and `platform_operator_root_events`:
    operation UUID PK, closed Provision/Revoke kind, immutable fingerprint,
    nonempty operator PrincipalId, nullable target credential for Provision and
    required target for Revoke, result code/revision/timestamps; event UUID PK +
    operation FK, closed event and no secret/digest. Only offline root functions
    write them; the first root requires no actor credential row.
17. No durable `capture_runtime_security_denial_events` table is authorized.
    Ordinary denials remain bounded non-authoritative telemetry and create no
    authoritative operation/event row. Replay and conflict are return-only.
    Only an authorized expiry-on-denied transition persists its exact `Expired`
    operation/event pair. This avoids a new indefinite security-event corpus.
18. `capture_runtime_cutover_state`: `Profile varchar` PK CHECK Managed,
    State Prepared/Activated, Revision, PreparedAtUtc, nullable ActivatedAtUtc/
    ActivatedByCredentialId with exact sparse CHECK; singleton Managed Prepared revision 1;
    Activated timestamp/actor only when A4 CASes it.

Named SECURITY DEFINER transition families cover platform verification,
bootstrap issue/revoke/redeem, runtime lifecycle, credential revoke, catalog
publish/assign, rotation authorize/revoke/complete/replay, nonce claim/cleanup,
capability issue/replace/bind/reconcile/expiry, config resolution and readiness.
No state mutation occurs through table DML.

The finite callable-name and full-signature sets are canonical projections of
the exact bound operation master and T1--T4 companions in the normative
catalogue. This derived body intentionally does not duplicate either set. Any
generated callable list must equal that projection byte-for-byte; in particular
it must not contain the obsolete `capture_runtime_apply_capture_artifact` or
`capture_runtime_apply_evidence_result` placeholders.

Only `capture_runtime_issue_or_replace_capability` and
`capture_runtime_cancel_session_with_capability` additionally grant EXECUTE to
landed `tagekyc_runtime`. Rotation authorize/revoke grant only
`tagekyc_capture_runtime_operator`; complete/replay grant only
`tagekyc_capture_runtime_application`. Replay is an internal R13 branch reached
only after valid successor CRT1 transaction N and writes no state/event.

## Lock, expiry, audit and cleanup

Global advisory domains: nonce-capacity 1, bootstrap 5, runtime 10, installation 20, generation
30, catalog 40, rotation 60, session 70, capability 80; same-domain signed keys
ascending, then row FOR UPDATE and expected-revision/fingerprint CAS. Redeem is
5→10→20→30; lifecycle 10→20→30; assignment 10→40; rotation
10→20→30→60; capability/cancel 70→80; bind and runtime business
10→20→30→70→80. Collision is serialization only; rows are always rechecked.

Pre-identity and post-identity/pre-business denial are bounded telemetry-only
without target IDs and create no authoritative operation/event row. Replay and
conflict after a stable business operation identity return the frozen result or
typed conflict without inserting another operation or event. Accordingly,
authoritative event CHECK domains do not admit `Rejected` or `Replayed`.
Expired bootstrap/rotation/capability is the explicit exception: it is CASed and
committed with one terminal operation/event pair before typed denial mapping—never
thrown away.

Nonce cleanup deletes at most 500 rows strictly `PurgeAfterUtc < now`, ordered
by purge/credential/generation/nonce with SKIP LOCKED. Maximum rows 1,000,000 and
maximum expired age 900s are readiness inputs owned by the A1 options validator;
A4 must provide production values and has no fallback. Nonce claim first runs
one bounded cleanup batch, acquires global nonce-capacity advisory domain 1,
recounts under that lock, and fails `NOT_READY` without insertion when the
count is already at the ceiling. All insertions take domain 1 before the unique
nonce insert, so ceiling-1 admits one winner only. Cleanup failure also
fails closed. It never removes a live or equality-boundary nonce.

Platform credential and runtime generation expiry are time-derived at every
authentication check; no request writes an Expired lifecycle row merely to
authenticate. Revoked/Retired remain explicit durable terminal states. The
`Expired` text state is therefore removed from those two tables to avoid a
decorative, ownerless transition; bootstrap/rotation/capability expiry remains
materialized because their owning operations require terminal replay evidence.

## Race-pair ledger — normative transcription of Planning §8.6

| Pair | Shared order/CAS | Winner, loser and replay |
| --- | --- | --- |
| bootstrap issue/issue | issuance identity 5 + operation uniqueness | first commit; exact replay nonsecret; fingerprint mismatch conflict |
| bootstrap issue/revoke | issuance 5, state recheck | first visible; revoke never false-NotFound beside committed issue |
| redeem/revoke-or-expire | 5→10→20→30 | first terminal wins; loser creates no identity; exact redeem returns IDs |
| redeem/redeem | same issuance and identity locks | one activation; loser conflict/replay; no orphan |
| Managed activation/activation | 10→20→30 | one Active install/current generation; loser terminal conflict |
| rotation authorize/authorize-or-revoke-or-expire | 10→20→30→60 | one Active authorization; terminal wins once; exact replay nonsecret |
| rotation authorize/runtime terminal | 10→20→30→60 | terminal lifecycle first wins or later terminalizes authorization; no completion |
| rotation complete/complete-or-auth terminal | 10→20→30→60 + expected current CAS | one successor; exact replay same successor; expired/revoked loses |
| rotation complete/credential revoke-or-runtime terminal | same identity locks | first terminal wins; completion only if all Active; never split generation |
| role assign/rotation-or-evidence-or-R1 | 10→40 then owning resource locks | assignment is next-generation; current binding unchanged; committed R1 converges |
| catalog publication/rollback | catalog 40 + expected head | one next revision; loser conflict; rollback is newer audited revision |
| config assign/config read | 10→40 | reader wholly old/new; widening rejected; replay stable |
| pepper configuration change/verifier use | immutable API-host metadata snapshot + exact-version resolution + DB required-version scan | issuance freezes CurrentVersion; verification uses persisted version; removal is operationally valid only when version is neither current nor SQL-required; missing required version is NOT_READY—A1 does not claim it can block deletion in the external secret store |
| nonce cleanup/replay | timestamp then nonce unique; purge strictly `< now` | valid envelope retains nonce; cleanup bounded, no lifecycle join |
| suspend/reactivate/revoke/retire | 10→20→30 expected revision | revoke/retire terminal; otherwise first commit; loser current/conflict |
| activation/runtime terminal | 10→20→30 | terminal runtime prevents new Active installation |
| capability issue-or-replace/bind | identity if applicable then 70→80 | Bound wins; one successor/predecessor; exact replay nonsecret |
| issue/issue or replace/replace | 70→80 + active unique/expected revision | one secret winner; repeat nonsecret; mismatch conflict |
| replace/cancel | 70→80 | terminal session wins; no Active successor |
| bind/cancel | bind 10→20→30→70→80; cancel 70→80 | terminal prevents Bound; committed Bound may revoke, never transfer; no deadlock |
| request nonce/same nonce | nonce PK in committed N | one reaches target; loser generic replay/no disclosure |
| distinct nonce admissions at ceiling-1 | global nonce-capacity domain 1, cleanup+recount+insert | one insertion reaches ceiling; loser NOT_READY with no nonce/target lookup |
| nonce admission/rotate-or-lifecycle | N commits no authority; B rechecks identity locks | lifecycle denial prevents target; nonce remains |
| config/capture/evidence/pre-broker vs lifecycle | N then B identity locks before resource | lifecycle first denies; business first valid at checkpoint; post-R1 unaffected |

Every row must join to at least one transition function and one independently
RED-capable concurrency test. An unlisted race is STOP/RRI, not Builder policy.

## Outcome precedence and disclosure ledger — normative Planning §9

| Use case | Evaluation order | Public result and forbidden disclosure/residue |
| --- | --- | --- |
| invalid Client capability | capability authority → session → expiry/replay | access/ineligible; no binding/artifact/acceptance/raw facts |
| invalid runtime credential | credential → runtime → installation → route role | generic access denied; disclose no session/runtime target state; no nonce if lifecycle invalid |
| exact bound mutation | credential/runtime/install → binding → horizon → operation replay | exact result; nothing past horizon |
| expired exact replay | current auth → binding identity → durable replay → horizon | existing durable response only, otherwise expired; never re-execute |
| expiry after pre-R1 evidence | current identity → binding → horizon | deny new mutation/R1; immutable committed evidence remains |
| rotated pre-R1 credential | old auth fails; successor mismatches frozen binding | deny/new session; no takeover/mixed generation |
| trust/profile incompatible | authenticated authority → capability requirements → trust | ineligible plus bounded durable semantic audit; no target detail before authority |
| first bind | authority → session unbound → guarded CAS | Bound and one binding/event |
| exact bind retry | authority → existing binding → exact lineage/operation | same BindingId/horizon; no second event |
| bind response lost | current lineage → capability ID+operation → committed binding | same nonsecret binding or generic unavailable; no secret/state creation |
| competing runtime | authority → existing binding mismatch | conflict/ineligible; original unchanged; no original runtime identity disclosed |
| revoked before R1 | credential/runtime/install before capability/session detail | deny; no bind/R1/body |
| revoked after committed R1 | committed custody/replay fence before new external authority | R2-R6 converges; new capture/read/reuse denied |
| operator auth/scope failure | platform auth → scope before DTO target | generic access denied; no target/audit authority event |
| bootstrap issue/revoke | actor/scope → closed DTO → trust refs → idempotency/state | created/nonsecret replay/conflict/terminal; secret once |
| bootstrap redeem | issuance validity → secret → candidate PoP → idempotency → activation CAS | identities/exact replay/generic invalid/conflict; no partial Active rows |
| rotation authorize/complete | actor/current auth → authorization state/expiry → dual PoP → CAS | one successor or terminal/conflict; no split generation |
| catalog publish/assign | actor/scope → closed schema/references → idempotency/head | published/assigned/replay/conflict; append-only revision/event |
| readiness | actor/scope before runtime lookup | snapshot or generic unavailable; no local-host claims |
| verifier-pepper dependency unavailable | authenticate authority envelope/prefix without target disclosure → resolve exact persisted version | existing dependency failure/NOT_READY (503); never ACCESS_DENIED, target detail, CurrentVersion substitution, try-all or Client API-key fallback |

Pre-identity failures are telemetry-only. After canonical authority, public codes
remain at the coarsest row above unless an exact reviewed route ledger narrows
them without increasing disclosure. No lower-priority predicate may leak when a
higher-priority authentication/authority predicate already fails.

## Exact ACL boundary

Migration creates only NOLOGIN operator/authenticator/application roles. Every
function is SECURITY DEFINER, owner deployer, search_path pg_catalog. PUBLIC,
ordinary runtime, broker and all three roles have zero table/sequence DML.
Operator executes management/catalog/readiness; authenticator verifier+nonce;
application capability/bind/reconcile/config/capture/evidence. A1 test fixture
may provide synthetic versioned pepper metadata/material only inside tests. The
API process resolves the closed Capture Runtime pepper source; the offline R01
process resolves only its explicit CLI SecretRef. The provider itself gains no
database/table authority.
creates/drops ephemeral operator and online LOGINs. A4 alone owns production
LOGINs/memberships/secrets. Down first preflights and STOPs on any A4 LOGIN
membership, revokes every full function signature, drops API functions, drops
current-generation/capability-graph/immutable triggers, drops helpers, drops
event tables before operation tables, drops binding/capability/rotation/nonce/
bootstrap tables, removes the installation-current composite FK, then drops
generations/installations/overrides/registrations, catalog heads before
revisions, platform credentials and sentinel, and the three NOLOGIN capability
roles last. It never drops an A4 LOGIN.

## A1 mutation allowlist

`TagEkycPersistenceServiceCollectionExtensions.cs` is explicitly allowed only
to register both Capture Runtime factories, the closed database options and its
validator; default DbContext registration and Client route behavior remain
unchanged.

## Ledger 1 — literal route and operation catalogue

New runtime request records live in `TagEkyc.Contracts.CaptureRuntime`; R26 retains
the existing `TagEkyc.Contracts.BusinessConsumer.CancelVerificationSessionRequestDto`.
New runtime UUID wire form is lower-case `N`, revisions are positive Int64, UTC is canonical `O` ending Z.
`Idempotency-Key` is header-only where required; R26 deliberately does not accept,
parse or consume it. “B” below means one transaction containing
the stated locks, fresh recheck, resource+audit write and replay result.

| ID and exact callable | Auth / request → response | Transaction ownership |
| --- | --- | --- |
| R01 `ApiKeyProvisioner platform-operator provision` | deployment invocation; `PrincipalId,ExpiresAtUtc` + operation UUID; exact root fingerprint domain `TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1` binds only operation/principal/expiry and excludes generated verifier material + `p_now_utc`; exact positive pepper version + SecretRef remain verifier inputs, not request identity → credential ID, lookup prefix, secret once, expiry, revision | provisioner resolves supplied Capture Runtime SecretRef without API-host DI, calls the sole shared fingerprint helper, hashes and persists the same pepper version; platform row+root operation/event; regenerated-secret lost-response replay is `ExistingMatchSecretUnavailable`, never conflict or second secret |
| R02 non-route CLI `TagEkyc.ApiKeyProvisioner platform-operator revoke` | credential ID/revision/operation + fixed `DeploymentRevocation`; exact root fingerprint domain `TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1` binds those four values and excludes `p_now_utc`/result state → state+revision | operator-root B; credential lock, lifecycle CAS+event, commit/replay; exact replay does not duplicate root history |
| R03 `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances` | PlatformOperator; `BootstrapIssueRequest` exact authority revisions+attestation → secret-once response | B lock issuance 5; verify refs; operation+issuance+event; commit/replay |
| R04 `POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke` | PlatformOperator; issuance ID+expected revision+reason → state/revision | B lock 5; expiry/state CAS+operation/event; commit |
| R05 `POST /api/ekyc/capture-runtime/enrollments/redeem` | bootstrap+ENROLL1; issuance/secret/CandidateKeyId/SPKI/thumbprint/time/nonce → lineage/revisions | B 5→10→20→30; secret/authority/PoP/idempotency; registration+Pending install+gen1+current pointer+Active+event; commit/replay |
| R06–R09 `POST /api/ekyc/operator/capture-runtimes/{suspend|reactivate|revoke|retire}` | PlatformOperator; runtime ID/revision/reason → state/revision | B 10→20→30 as touched; lifecycle CAS+operation/event; terminal precedence; commit/replay |
| R10 `POST /api/ekyc/operator/capture-runtimes/credentials/revoke` | PlatformOperator; exact lineage/credential revision/reason → state/revision | B 10→20→30; current pointer/state CAS+operation/event; commit/replay |
| R11 `POST /api/ekyc/operator/capture-runtimes/credential-rotations` | PlatformOperator; current lineage+expiry → rotation metadata | B 10→20→30→60; active checks+operation/authorization/event; commit/replay |
| R12 `POST /api/ekyc/operator/capture-runtimes/credential-rotations/revoke` | PlatformOperator; rotation ID/revision/reason → state/revision | B 10→20→30→60; state CAS+event; commit/replay |
| R13 `POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete` | one presented CRT1; same closed body and Idempotency-Key; pure predecessor/successor fingerprint candidates before lookup; exact atomic classifier chooses predecessor CredentialRotation or current successor exact replay, no authentication fallback | one N commits selected fingerprint/branch and nonce; R13a B 10→20→30→60 performs dual-PoP cutover or expiry-on-denied; R13b rechecks exact committed tuple read-only; generic 403 mismatch, 503 dependency failure; no config probe/polling/reconcile route |
| R14 `POST /api/ekyc/operator/capture-runtimes/role-policies/assign` | PlatformOperator `operator.capture-runtime.manage`; `CaptureRuntimeRolePolicyAssignmentRequest` → assignment/revision | `capture_runtime_assign_role_policy`; B 10→40; `RoleAssignment_IsNextGenerationOnly` |
| R15 `POST /api/ekyc/operator/capture-runtimes/configurations/assign` | PlatformOperator `operator.capture-runtime.manage`; `CaptureRuntimeConfigurationAssignmentRequest` → assignment/revision | `capture_runtime_assign_configuration`; B 10→40; `ConfigurationAssignment_ReducesAndEtagIsOpaque` |
| R16 `POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish` | PlatformOperator `operator.capture-runtime.manage`; `CaptureRuntimeTrustProfilePublicationRequest` → revision/head | `capture_runtime_publish_trust_profile`; B 40; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| R17 `POST /api/ekyc/operator/capture-runtime-control/role-policies/publish` | PlatformOperator `operator.capture-runtime.manage`; `CaptureRuntimeRolePolicyPublicationRequest` → revision/head | `capture_runtime_publish_role_policy`; B 40; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| R18 `POST /api/ekyc/operator/capture-runtime-control/configurations/publish` | PlatformOperator `operator.capture-runtime.manage`; `CaptureRuntimeConfigurationPublicationRequest` → revision/head | `capture_runtime_publish_configuration`; B 40; `CatalogPublication_ExpectedHeadAndRollbackAsRevision` |
| R19 `GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness` | PlatformOperator → closed readiness snapshot | consistent operator connection read after auth/scope; no state mutation/target leak |
| R20 `POST /api/ekyc/verification-sessions/{sessionId}/capture-capabilities` | existing Client session owner; `Action,CurrentCapabilityId?,ExpectedRevision?` → ID, secret once, expiry/state | existing session B 70→80; owner/session/horizon+Issue/Replace CAS+operation/event; commit/replay |
| R21 `POST /api/ekyc/capture-runtime/executions/bind` | CRT1 Bind + capability ID/secret/BindOperationId → BindingId/horizon | N commit then B 10→20→30→70→80; lineage/role/session/secret/horizon+binding/event; commit/replay |
| R22 `POST /api/ekyc/capture-runtime/executions/reconcile` | CRT1 + capability ID/BindOperationId → exact BindingId/horizon or unavailable | N commit then consistent B read; current lineage and exact operation; no mutation/secret |
| R23 `GET /api/ekyc/capture-runtime/self/configuration` | CRT1 Configuration + If-None-Match → 200 config+ETag or 304 | N commit then consistent B read of exact assignment/revisions; no business mutation |
| R24 `POST /api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts` | CRT1 CaptureObservation + existing business fields, no Agent/Device → existing result | N commit then runtime B 10→20→30→70→80+artifact; derived identity+idempotency+artifact+audit; commit/replay |
| R25 existing `POST /api/ekyc/verification-sessions/{id}/evidence-results` | CRT1 TrustedEvidence+BindingId + existing verdict fields, no Agent/Device → existing result | N commit then runtime B identity→session/evidence; exact acceptance/idempotency+audit; commit/replay |
| R26 existing `POST /api/ekyc/verification-sessions/{id}/cancel` | existing Client session owner and landed DTO, deliberately no Idempotency-Key → existing terminal result | existing finalization B 70→80; sole eight-input SQL derives operation ID from session UUID and raw SHA256 from exact R26 domain+client UUID+session UUID; first A1 cancellation writes one Cancel operation, Cancelled event and SESSION_CANCELLED audit; matching-operation A1 durable replay writes none; HISTORICAL_LANDED_CANCEL without an A1 Cancel operation preserves the persisted response and creates no synthetic Cancel history; expiry-first still materializes one standalone Expire operation/event for an elapsed live capability, otherwise no writes |
| R27 public `POST /api/ekyc/raw-export/source-ingress` | CRT1 RawIngress + signed metadata/body commitment → A3 domain result | A1 owns N, unread-body envelope and fixed HTTP status/body serialization of the closed result; A3 owns B binding/acceptance/R1 and domain-outcome selection |
| R28 A1 startup route selection | deployment process + schema/ACL/sentinel/global pepper/A3-port facts → one immutable route set or startup failure | read-only before listener accepts traffic; Prepared starts control and legacy/Client routes including R26 without A3; malformed/unknown state, dependency mismatch or Activated with missing A3 fails startup; Activated maps runtime producer handlers and ordinary Client routes only |

Critical wire contracts are not inferred from the summary rows:

- R05 carries BootstrapIssuanceId exactly once, bootstrap secret as 43 canonical
  unpadded base64url characters decoding to 32 bytes, CandidateKeyId UUIDv4-N,
  successor SPKI as 91-byte canonical DER encoded unpadded base64url, thumbprint
  as 64 lower-hex characters, timestamp canonical O/Z, nonce as 43 canonical
  base64url characters decoding to 32 bytes, and ENROLL1 P1363 signature as 86
  canonical base64url characters decoding to 64 bytes. It accepts no CRT1,
  Client API-key or platform credential. RedeemOperationId is the sole
  Idempotency-Key. First success is 201; exact replay is 200 nonsecret lineage;
  invalid secret/PoP is generic 403; fingerprint/state conflict is 409.
- R13 path carries RotationId only. The body is exactly CandidateKeyId UUIDv4-N,
  SuccessorPublicVerifierSpki (91-byte DER as canonical unpadded base64url),
  SuccessorPublicKeyThumbprint (64 lower-hex) and SuccessorProof (64-byte P1363
  as canonical unpadded base64url). Idempotency-Key is header-only. Fingerprint
  covers path RotationId, stable CredentialId plus predecessor/successor Generation pair, all four body
  values and idempotency semantics. Success 200 returns exactly CredentialId,
  Generation, CandidateKeyId, PublicKeyThumbprint, CredentialRevision,
  InstallationRevision and RotationRevision. Generic auth/terminal is 403;
  authorized absent target is 404; mismatch/conflict is 409; dependency is 503.
- R20 path supplies VerificationSessionId. Body is the closed union
  `Issue { Action:"Issue" }` or `Replace { Action:"Replace",
  CurrentCapabilityId, ExpectedRevision }`; opposite-arm fields are forbidden.
  Idempotency-Key is header-only. Secret appears only on first success. Exact
  replay returns 409 `EXISTING_MATCH_SECRET_UNAVAILABLE` in the canonical
  `code`/`correlationId` error envelope; persisted nonsecret capability metadata
  remains frozen, not added to that HTTP error. ClientApplicationId derives from session auth.
- R21 body is exactly CaptureCapabilityId, CaptureCapabilitySecret and
  BindOperationId. BindOperationId is the single header Idempotency-Key value and
  is not duplicated independently. Capability secret is 43 canonical unpadded
  base64url characters decoding to 32 bytes; the generic fingerprint's ordinal-1
  SHA256 commitment binds the decoded secret, while its separate keyed digest
  verifies the persisted capability. Success returns only BindingId,
  ExecutionExpiresAtUtc and frozen revisions; exact replay is nonsecret.

There is no rotation reconcile route and no authentication by an inactive
predecessor. `CredentialId` is the installation-stable lineage identity;
`Generation` advances by one; `CandidateKeyId` is the globally unique
per-rotation key/replay identity and never equals or replaces CredentialId.
Ambiguous R13 recovery uses the same route and identical durable business
fingerprint. It excludes the CRT1 envelope, nonce and signature, which differ per
HTTP attempt; it includes RotationId, stable CredentialId, predecessor/successor
Generation, CandidateKeyId, successor SPKI/thumbprint and Idempotency-Key. Each
request computes the two representable pure fingerprint candidates before any
target lookup, verifies its one presented CRT1 once, and invokes the one atomic
rotation-completion classifier/nonce function. The locked branch selects one
fingerprint and one Predecessor or Successor actor. No authentication fallback,
second classifier call, polling, config probe or new reconcile route is allowed.
Only the active predecessor with CredentialRotation plus ROTATE1 can perform
cutover; the exact current successor can only read committed completion. A
Rotated predecessor has no authentication exception. Null alternate candidates
at generation boundaries do not invalidate a representable selected candidate.

For R13 only, after the single-pass classifier selects the exact successor and
commits nonce transaction N, exact committed-
operation replay is allowed without requiring the successor's frozen role policy
to contain `CredentialRotation`. The replay must match stable CredentialId,
successor Generation, RotationId, CandidateKeyId, SPKI, thumbprint,
Idempotency-Key and fingerprint; mismatch denies generically before any other
lookup. This exception is read-only and grants no first completion or other
route. The predecessor-authenticated state-changing branch still requires
`CredentialRotation` and both proofs.

## Ledger 3 — process, connection, role and function authority

| Process/boundary | Connection/login source | Membership / callable functions | Explicit denial |
| --- | --- | --- | --- |
| ordinary TagEkyc Client routes | existing default `TagEkycDbContext`/landed deployment identity | existing landed functions plus only combined Client capability/cancel functions granted to existing `tagekyc_runtime` | no operator/authenticator role; no runtime verifier/nonce functions; no A1 table DML |
| runtime authentication and non-append business boundaries | `ICaptureRuntimeDbContextFactory.CreateAsync`, mandatory named `CaptureRuntimeOnline` secret reference; A1 tests use `tagekyc_c6ba_test_online_login` | authenticator+application roles; verifier/nonce then permitted capability/bind/config functions | no operator functions or direct A1 table DML; this connection does not own neutral append B |
| R24/R25 neutral append B after committed N | existing ordinary scoped Client `TagEkycDbContext`, separate from N; explicitly authorized typed-gateway v0.9 boundary | sole seven-input append-authority helper EXECUTE via `tagekyc_runtime`, then existing neutral planner/writer in the same B | no direct A1 authority-table SELECT/DML, caller-selected helper session, runtime-factory append context or second B |
| operator HTTP routes | separate `ICaptureRuntimeOperatorDbContextFactory`, mandatory named `CaptureRuntimeOperator` secret reference; test login `_operator_login` | operator role and exact R03/R04/R06-R19 functions | no authenticator/application functions or DML |
| offline provisioner product code | CLI resolves its explicit deployment connection SecretRef and opens its own connection/transaction; no API DI/factory | R01/R02 root functions under the reviewed offline deployment principal; commit before any output; no embedded secret | no online/runtime connection, Client API key or API-host pepper inference |
| A3 broker | absent in A1; typed `ICaptureRuntimeRawIngressAdmission` consumer contract only | no A1 production callability until A3 exact contract lands | A1 must not grant broker function/table access |
| migration owner | existing NOLOGIN `tagekyc_raw_export_deployer` | owns DDL/SECURITY DEFINER functions; R26 additionally has only the exact eleven audit_events INSERT columns pinned in OM R26 and T03, revoked by Down | not an application LOGIN; no whole-table audit INSERT grant or transfer of owner privilege to an online A1 role |

All functions use fully qualified objects, owner deployer,
`SECURITY DEFINER SET search_path=pg_catalog`; revoke PUBLIC and all unrelated
roles before the sole grants. Test fixture creates two LOGINs without shared
membership, validates `current_user`/`session_user`, and drops them on success
or failure. Production LOGIN creation/membership/secret values are A4-only.

## Ledger 4 — secret and cryptographic material lifecycle

| Material | Generator and holders | Durable form / replay | Terminal handling and negative proof |
| --- | --- | --- | --- |
| Platform operator key | offline provisioner mutable 32-byte buffer; presented once on stdout to invoking deployment process as exact `teo_` + 43 canonical unpadded base64url characters | public first-12-character payload projection; HMAC-SHA256 over `ASCII("platform-credential-v1") || 0x00 || decoded-secret-32` under versioned pepper | reject noncanonical grammar; compare digest fixed-time; zero presented/decoded buffers; revoke/expiry deny; logs/argv/env/file scan RED |
| Capture Runtime verifier pepper | A4 supplies production SecretRef values; API host uses closed options + Infrastructure provider; R01 receives exact version+SecretRef from deployment operator; A1 holds resolved material only while hashing | exact version on every platform/bootstrap/capability digest row; API issuance uses CurrentVersion; verification uses persisted version; no API-key or alternate-version fallback | readiness resolves `{CurrentVersion} UNION SQL required_pepper_versions`; missing required/current version is NOT_READY; removal legal only when not current and absent from SQL required set; lease and A1-owned mutable buffers zero on disposal |
| bootstrap secret | R03 CSPRNG mutable buffer → A4 orchestration → A2 stdin (outside A1) | lookup prefix + HMAC digest/version; exact replay omits secret | redeemed/revoked/expired terminal; transport loss requires revoke/reissue; no HTTP/log cache |
| candidate/server verifier | A2 owns private key; A1 receives canonical 91-byte P-256 SPKI and 32-byte thumbprint only | immutable generation row + CandidateKeyId replay identity | mismatch/reuse denies; private material never server-side |
| capability secret | R20 CSPRNG mutable buffer → authenticated Client response | lookup prefix + HMAC digest/version; replay returns metadata+secret unavailable | Bound/replace/cancel/expiry terminal; plaintext absent from event/audit/log |
| CRT1 nonce | Agent CSPRNG; server parser mutable 32-byte buffer | immutable nonce PK, signed/admitted/purge; N commit survives B | bounded cleanup only after expiry; duplicate generic deny; equality never deleted |
| successor verifier | A2 private; A1 receives SPKI/thumbprint/CandidateKeyId+dual proof | stable CredentialId + Generation+1; Pending→Active generation/current pointer atomically; same R13 route classifies one presented CRT1 and one selected fingerprint, with read-only exact successor replay | never instruct deletion on ambiguous transport result; no server private key or authentication fallback; no A2 recovery implementation is authorized by A1 |

## Ledger 5 — exact mutation and evidence paths

Existing paths authorized to modify are exactly:

```text
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/VerificationSessionEndpoints.cs
src/TagEkyc.Api/ReadinessEndpoint.cs
src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs
src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs
src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs
src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs
src/TagEkyc.Application/Ports/ApplicationServicePorts.cs
src/TagEkyc.Application/Ports/RepositoryPorts.cs
src/TagEkyc.Contracts/CaptureAgent/CaptureAgentContracts.cs
src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs
src/TagEkyc.Infrastructure/Persistence/DomainRowMapper.cs
src/TagEkyc.Infrastructure/Persistence/EfAppendIdempotencyBoundary.cs
src/TagEkyc.Infrastructure/Persistence/EfVerificationFinalizationBoundary.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs
tools/TagEkyc.ApiKeyProvisioner/Program.cs
tests/TagEkyc.IntegrationTests/Tip88C1AAcceptanceSurfaceTests.cs
src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs
src/TagEkyc.Application/VerificationSessions/SessionOperationResult.cs
src/TagEkyc.Application/LocalDev/LocalDevInMemoryRepositories.cs
tests/TagEkyc.UnitTests/Tip82SSessionCancelTests.cs
tests/TagEkyc.UnitTests/Tip06CompletionApplicationTests.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceSliceTests.cs
tests/TagEkyc.IntegrationTests/Tip68ProdHsmSigningTests.cs
tests/TagEkyc.IntegrationTests/Tip83ASigningKeyJwksTests.cs
tests/TagEkyc.IntegrationTests/Tip83BPostgresProductionInfraTests.cs
tests/TagEkyc.IntegrationTests/Tip83E1ReadinessEndpointTests.cs
tests/TagEkyc.IntegrationTests/Tip83E3RetentionConfigTests.cs
tests/TagEkyc.IntegrationTests/Tip84BHashedApiKeyStoreTests.cs
tests/TagEkyc.IntegrationTests/Tip86DecisionThresholdConfigTests.cs
tests/TagEkyc.IntegrationTests/Tip88AE2PolicyPermitTtlConfigTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs
tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C4RecipientPackageReferenceTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C5RecipientManagementTests.cs
tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs
tests/TagEkyc.ArchTests/DockerTestResourceLifecycleTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs
tests/TagEkyc.ArchTests/Tip88C1B2R2DurableCustodyEncryptionArchTests.cs
```

New paths are limited to these exact names inside existing projects:

```text
src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeAuthenticationContracts.cs
src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeEnrollmentContracts.cs
src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeManagementContracts.cs
src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeControlContracts.cs
src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs
src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeVerifierCryptography.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeEnrollmentApplicationService.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeManagementApplicationService.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeControlApplicationService.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs
src/TagEkyc.Api/CaptureRuntimeEndpoints.cs
src/TagEkyc.Infrastructure/Auth/PlatformOperatorCredentialAuthenticator.cs
src/TagEkyc.Infrastructure/Auth/CaptureRuntimeRequestAuthenticator.cs
src/TagEkyc.Infrastructure/CaptureRuntime/ICaptureRuntimeDbContextFactory.cs
src/TagEkyc.Infrastructure/CaptureRuntime/ICaptureRuntimeOperatorDbContextFactory.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDatabaseOptions.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDbContextFactory.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeOperatorDbContextFactory.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeDatabaseOptionsValidator.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperOptions.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeVerifierPepperProvider.cs
src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntimeEntities.cs
src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntimeEntityConfigurations.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260908120000_Tip88C1C6BA1Foundation.Designer.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ContractTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AclTests.cs
tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs
tests/TagEkyc.ArchTests/Tip88C1C6BA1DispatchCatalogueTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1StartupTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1RuntimeAppendTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1RouteRegistrationTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1RotationTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ResponseWireTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ExecutionTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1EnrollmentTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ConfigurationSerializationTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ClosedJsonTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1EnrollmentHttpTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1ManagementHttpContractTests.cs
tests/TagEkyc.UnitTests/Tip88C1C6BA1AppendHttpLimitTests.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeStartupDependencyReader.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeStartupCatalogue.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeRotationPersistenceBoundary.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeManagementPersistenceBoundary.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeExecutionPersistenceBoundary.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeEnrollmentPersistenceBoundary.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeControlPersistenceBoundary.cs
src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeAppendAuthority.cs
tests/TagEkyc.IntegrationTests/HistoricalPreparedWebApplicationFactory.cs
tests/TagEkyc.IntegrationTests/IsolatedMigrationPostgres.cs
tests/TagEkyc.IntegrationTests/A1SyntheticDbFailureInterceptor.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1EnrollmentTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1CanonicalDdlProjectionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1CancellationHttpTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AppendAuthorityTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ManagementTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ExecutionHttpTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1NormativeSqlProofTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RotationHttpTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RawIngressBoundaryTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RootCliTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PlatformAuthenticationTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1StartupCompositionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RuntimeAppendTransactionTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RotationTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1StartupReadinessDiagnosticTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1TransitionCoverageTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C6BA1LifecycleRaceTests.cs
src/TagEkyc.Infrastructure/Persistence/EfAppendBusinessTransaction.cs
src/TagEkyc.Api/CaptureRuntimeRotationEndpoints.cs
src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs
src/TagEkyc.Api/CaptureRuntimeRouteRegistration.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeStartup.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeRotationApplicationService.cs
src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeAppendApplicationService.cs
src/TagEkyc.Application/VerificationSessions/AuthorityNeutralVerificationEvidence.cs
```

The exact paths above reflect the typed-gateway v0.9 split, not additional
feature authority. Historical factory consumers are limited to test-host isolation
of pre-A1 fixtures; snapshot and architecture tripwires reconcile authorized A1
model/C6B handoff changes, and R214 retains its zeroization check in both build
modes. No deployment or production activation follows from these test changes.

`CaptureRuntimePorts.cs` owns both platform-operator and capture-runtime request
authenticator interfaces. Api consumes those Application ports and the two
Infrastructure authenticator classes implement them through Infrastructure's
existing Application reference. No Api-owned compatibility interface, project
file mutation or reverse Infrastructure-to-Api reference is authorized.

No solution/project, CaptureAgent, broker-host, deployment/IaC or SDK path is
authorized. Before mutation, every consumed dirty/untracked file receives
repository-relative path, Git state, raw SHA-256, purpose and proof ID; drift is
STOP/rebind. Generated migration filenames are frozen immediately after
generation before any further product mutation.

The following paths are read-only evidence and explicitly excluded from A1
mutation. The readiness validator is landed C6B evidence; the three raw-source
paths are A3-owned evidence. Any required edit is STOP/rebind to the owning
slice:

```text
src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs
src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs
src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs
src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs
```

Current dirty/untracked consumed-byte basis:

| Git | Exact path | SHA-256 | Purpose |
| --- | --- | --- | --- |
| M | `src/TagEkyc.Api/Program.cs` | `97A41D2C2EE3025C497C203E44ED8B2BD5758CD9EA962D600F65BA412C80C499` | DI/route selection |
| M | `src/TagEkyc.Contracts/TrustedAdapter/TrustedAdapterContracts.cs` | `923CDF0E0204EFFC1A44FC9CC906DDB5D12A8BBD9290A76FD760906B93EC9012` | neutral evidence DTO |
| M | `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs` | `945FD9C6028CE3B310891F21FF3F20E033928CA8C88CDD3DEBAB09F551C7B92E` | EF basis |
| M read-only | `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | `AE4408D7B2BF46A3EAFA9F1801D59C37BF3B5950662EBCA73ED1109D147762C7` | landed C6B readiness evidence only |
| M | `tests/TagEkyc.IntegrationTests/Tip88C1AAcceptanceSurfaceTests.cs` | `B4655A1562C6B3414066D638BD4682F0D736E94877415D967674477B5FA2D2BF` | legacy assertion |
| ?? | `src/TagEkyc.Api/CaptureAgentConfigurationEndpoints.cs` | `532F4196BE430DCDF877F263D6DB37890E399353DE835D579A63F893616B47A8` | old route removal |
| ?? | `src/TagEkyc.Api/CaptureAgentConfigurationProvider.cs` | `831D6032AA4C25105924501D808F5CD3E67498D7F97CAC32157BC6747EBF8A88` | C6B config fields |
| ?? | `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | `C37599AB9D4EE15F666FDBFBF1CCC082342F8D139D14FB37B9BCEA027D548570` | unread-body seam |
| ?? read-only | `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | `F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA` | A3 boundary evidence only |
| ?? read-only | `src/TagEkyc.Application/RawExport/RawExportSourceIngressApplicationService.cs` | `C996F31D4ADBBE7F33225879807741622CB67DDDD8305E5498C8C6CCC8357195` | A3 behavior evidence only |
| ?? read-only | `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | `4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66` | A3 signed-metadata evidence only |

Repository basis is TagEkyc HEAD
`c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9`; staged/conflicted were zero at
materialization. Final v0.11 release must rehash this table and bind the current
umbrella exact SHA; abbreviated SHA text is prohibited in the released ledger.

## Proof ledger

Each row identifies a logical acceptance obligation. The descriptive names are
not a claim that an identically named test method exists. As-built coverage must
map every ID to actual source methods and executed evidence, distinguishing
runtime/persistence proofs from architecture and pure-code proofs. An unexecuted
or incomplete mapping does not satisfy the obligation.

| ID / logical proof obligation | Required RED mutation |
| --- | --- |
| A1-01 `PlatformCredentialAuthentication_HasNoClientPartition` | fabricate ClientApplicationId |
| A1-32 `RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` | include generated verifier material or `p_now_utc`, omit an intent field, emit noncanonical text, conflict/duplicate/re-emit secret on regenerated-verifier exact replay |
| A1-33 `RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` | omit target/revision, accept arbitrary reason, include `p_now_utc`, duplicate history or replay changed intent |
| A1-34 `RootRequestFingerprint_R01R02_DomainsAreSeparatedAndImplementationIsUnique` | share/unlabel domains, add a competing implementation or broaden either helper signature |
| A1-02 `AuthenticationEnvelope_MixedSchemesDenyWithoutFallback` | enable fallback |
| A1-03 `Crt1_GoldenVectorsMatchClientAndServer` | reconstruct CRT1 bytes in authenticator; route R05 through CRT1; mutate or omit method, path, timestamp, nonce, media type, content length, body commitment or operation binding; accept noncanonical timestamp/nonce; accept a missing, extra or CRLF final terminator instead of exactly one LF; retain a mutable request/decoded-field alias; duplicate the proof owner or competing builder; or read RawIngress body before R1 |
| A1-04 `NonceCommit_SurvivesBusinessRollbackAndRejectsDuplicate` | share N/B transaction |
| A1-05 `RuntimeConnection_CannotResolveDefaultClientDbContext` | inject default context |
| A1-06 `Enrollment_AtomicActivationAndLostResponseReplay` | omit current-generation trigger |
| A1-07 `Rotation_DualProofAtomicCutoverAndSameRouteReplay` | accept one proof/delete successor on timeout |
| A1-08 `SecretOnce_DigestsAndPepperRetirementAreClosed` | re-emit secret/retire referenced pepper |
| A1-09 `CatalogPublication_ExpectedHeadAndRollbackAsRevision` | mutate old revision |
| A1-10 `ConfigurationAssignment_ReducesAndEtagIsOpaque` | widening override/equal ETag-revision assumption |
| A1-11 `CapabilityIssue_SecretOnceAndExactReplay` | second secret-bearing replay |
| A1-12 `CapabilityReplace_ExpectedCurrentAndBoundWins` | replace Bound/wrong revision |
| A1-13 `Bind_OneWinnerSameLineageReplayNoTakeover` | permit second runtime |
| A1-14 `BindingReconcile_IsReadOnlyAndNonEnumerating` | emit secret/create binding |
| A1-15 `SessionCancel_TerminalizesCapabilityAtomically` | second connection/event |
| A1-16 `CaptureWrapper_DerivesRuntimeIdentityInOneBTransaction` | caller Agent/Device authority |
| A1-17 `EvidenceWrapper_PreservesAcceptanceAndDerivedIdentity` | legacy API-key producer wrapper |
| A1-18 `RawPublicSeam_CommitsNonceAndLeavesBodyUnreadForA3` | read body/resolve binding in A1 |
| A1-19 `FoundationMigration_ApplyRollbackReapplyEnforcesAllShapes` | drop one CHECK/FK/index |
| A1-20 `DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown` | extra EXECUTE/DML edge |
| A1-21 `Sentinel_SelectsOneRouteSetBeforeTraffic` | map legacy+runtime together |
| A1-22 `DirectCutoverBinary_RemovesLegacyProducerAuthentication` | restore old config/capture auth |
| A1-23 `AuditFailure_RollsBackBButRetainsNonceAndNoSecretsLeak` | commit target without audit/log secret |
| A1-24 `Readiness_ValidatesSchemaAclPepperNonceAndConnections` | omit one failing dependency |
| A1-25 `Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation` | add an Api-owned authenticator interface, reverse Infrastructure-to-Api reference, project-file mutation or other forbidden reference/path |
| A1-26 `DispatchCatalogue_IsClosedAndBidirectionallyJoined` | orphan one identifier or break one catalogue join |
| A1-27 `VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed` | permit HTTP version selection, fallback, unresolved required-version readiness GREEN, invalid config, Client API-key reuse or direct-SQL-only R01 proof |
| A1-28 `CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency` | reference ApiKey pepper, add reverse/project dependency, authorize plaintext config, make readiness evidence writable or route R01 through API DI/appsettings |
| A1-29 `VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors` | accept a tail-bit alias, variable length, normalization or implementation-generated expected vector |
| A1-30 `R01PlatformProvision_UsesExplicitCaptureRuntimePepperVersionAndSecretRef` | omit either CLI authority input, use Client pepper/API DI, or persist a version different from the digest key version |
| A1-31 `PresentedVerifierSecrets_RejectNonCanonicalTailAliases` | accept any same-byte textual alias for platform, bootstrap or capability material |

## Seven mandatory executable ledgers before authority

1. Literal DDL: every table/column/type/nullability/default/CHECK/FK/index,
   transition function and Down order. No aggregate shorthand or JSON state.
2. Route ledger: exact record/property/type/limit/auth/fingerprint/status/error,
   service/repository/function/test method for every A1 route/tool.
3. Lock graph: every operation from bootstrap through rotation/capability/bind,
   including terminal expiry and denial audit.
4. Function ACL: owner deployer; sole A1 operator/authenticator/application test
   roles; zero table DML; no production LOGIN assumption.
5. Material ledger: creation, mutable buffer holders, durable digest/verifier,
   ambiguous response recovery and deletion for platform/bootstrap/capability/
   rotation secrets.
6. Race-pair ledger: every operation pair from Planning v1.7 §8.6, their shared
   advisory/row lock and CAS, legal winner, loser result, durable residue and
   exact-replay result. A lock order without loser semantics is incomplete.
7. Outcome-precedence/disclosure ledger: every competing failure predicate from
   Planning v1.7 §9, evaluation order, public code/status, durable effect and
   facts the caller must not learn. Authentication and authority predicates
   always precede protected-target/state disclosure.

The seven ledgers are a relational catalogue, not seven independent prose
sections. They must join bidirectionally:

```text
every route -> DTO -> auth/role -> transaction/function -> tables -> outcome -> test
every SQL function -> sole caller/process/role -> state transition -> Down/test
every secret -> generator/holder/digest/replay/terminalizer -> leak proof
every race pair -> common lock/CAS -> winner/loser/replay -> proof
every public outcome -> ordered predicate -> allowed disclosure -> proof
every mutated file -> requirement/owner -> generated surface -> build/test
```

An orphan route, function, table/column, role, secret, result code, race outcome,
test or mutated file is a blocking catalogue error; Builder may not fill it in.

## PI-TAG-001 Round-3 convergence diagnosis

Rounds 1–2 did not converge because prose inventories were patched locally while
their identities were not joined across schema, route, authentication, replay,
ACL and file ownership. That allowed three cumulative failures: CandidateKeyId
was conflated with stable CredentialId; replay promised durable results without
operation rows; and A1's mutation family reached into A3.

Round 3 changes the method, not merely the wording: the rotation identity is
ratified once in the parent and copied literally; every response-loss path must
name its operation row and exact read authority; callable SQL names and grantees
form a finite catalogue; A1/A3 has one typed port; wildcard mutation paths are
deleted. Reviewers must read the entire v0.11 and report all catalogue orphans in
one result, including route→DTO→function→table→outcome→test joins. A review that
stops after its first finding is incomplete.
