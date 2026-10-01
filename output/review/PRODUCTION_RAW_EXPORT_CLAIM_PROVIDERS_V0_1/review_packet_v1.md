# Production Raw Export claim providers — review packet v1

## Disposition

`TECHNICAL PASS CANDIDATE — GPTW MEDIUM-01/MEDIUM-02/LOW CORRECTED; RE-REVIEW REQUIRED.`

Baseline is `62655d1a636fe9db06cea41e37bf1b811067b5ca` (seal revision 24), branch `tip-88a-raw-export-policy-catalog-build`, upstream posture `+4/-0`. This candidate is not committed, re-frozen, pushed, or deployed.

The Homeowner-authorized boundary is unchanged: two OpenBao Transit HMAC claim providers, two production selector catalogs, separate credentials/policies/keys, production DI/readiness, real OpenBao proof, and factual debt-registry reconciliation. No schema, migration, endpoint, protocol, A3, Layer 2, KEK state-machine, SignFlow, or backup/restore change exists in this slice.

The pre-existing Homeowner file `docs/00_GDRIVE_FILE_INDEX.md` and the two pre-existing review ZIPs at the review root were not touched or included.

## Review correction

The first external review returned CC PASS and GPTW HOLD with two production-composition findings plus one evidence-packaging gap. All three are corrected without changing the authorized boundary.

### MEDIUM-01 — exact production interface ownership

Production no longer proves only that OpenBao concrete services happen to exist. Each runtime interface has exactly one singleton descriptor whose `ImplementationType` is the corresponding OpenBao implementation:

```
IContentCommitmentService -> OpenBaoContentCommitmentService
ISubjectRefTokenService   -> OpenBaoSubjectRefTokenService
```

The production guard rejects zero, multiple, factory/instance, fixture, or any other implementation mapping. Two adversarial tests pre-register an unknown content provider and an unknown subject provider; both production compositions fail closed with `PROD_RAW_EXPORT_CLAIM_PROVIDERS_MISSING`.

The public production-registration extension enforces the same boundary before `TryAdd` can preserve an unknown pre-registration. Existing fixture graphs are identified by their already-existing fixture catalog markers so their specific fixture codes remain stable; unknown factory/instance/type mappings receive `PROD_RAW_EXPORT_CLAIM_PROVIDERS_MISSING`.

### MEDIUM-02 — CA loading stays inside readiness/operation boundary

Claim-provider service construction no longer creates `OpenBaoHttpTransport` or reads the configured CA file. A thread-safe lazy client is evaluated only inside `ComputeAsync` or `ValidateAsync`; therefore missing/unreadable/malformed CA failures are caught by the existing provider/readiness boundaries.

The production `Program` and `/readiness` endpoint are exercised with the exact production `RawExportClaimProviderReadinessCheck`. Missing file, malformed PEM, and a directory used as an unreadable file each return HTTP 503 containing `PROD_RAW_EXPORT_CLAIM_PROVIDER_INVALID`; no raw `FileNotFoundException`, `CryptographicException`, or `UnauthorizedAccessException` reaches the response.

An eager-construction mutation forces the Content client's `Lazy` value inside the service constructor. All three CA arms become RED with the raw constructor exception escaping before the readiness check runs. The inverse patch restores the provider source byte-exact and all three arms return to the stable 503 contract.

The first HTTP proof attempted to resolve every readiness check and was stopped by unrelated incomplete PKCS#11 test configuration before the claim-provider check ran. It is retained and classified `SUPERSEDED_TEST_SETUP`; the successor keeps the real Program/endpoint while selecting the exact production claim-provider readiness check so this boundary is measured without unrelated readiness construction.

### LOW — build transcripts retained

The final Infrastructure, IntegrationTests, and RawIngressBroker build transcripts are retained in this evidence directory and end with `EXIT_CODE=0`.

### Fresh review MEDIUM — deployed AppRole policy separation

Production readiness now proves policy isolation against the deployed OpenBao instance rather than inferring it from distinct secret-reference names. After validating both providers' own key metadata, the Content credential performs a fixed non-sensitive HMAC probe against the active Subject key and the Subject credential probes the active Content key. Only an OpenBao HTTP 403 after the existing re-authentication attempt counts as denial. Any successful cross-call returns `PROD_RAW_EXPORT_CLAIM_PROVIDER_SEPARATION_INVALID`; authentication, transport, TLS, or unexpected-status failures remain provider-invalid failures. Successful probe bytes are zeroed before the readiness failure is raised.

The focused four-arm matrix proves only `deny/deny` is accepted. The real OpenBao proof then deliberately broadens the Content policy to permit the Subject HMAC endpoint; production readiness becomes RED with the stable separation code. Restoring the narrow policy returns the real gate to green. A product mutation that skips the two cross-policy observations makes exactly this real overbroad-policy assertion RED.

## Production implementation

### Content commitment

`OpenBaoContentCommitmentService` implements the existing `IContentCommitmentService` contract. It resolves the exact `(KeyId, KeyVersion)` through `OpenBaoContentCommitmentCatalog`, submits the existing length-prefixed payload to `/v1/{mount}/hmac/{key}/sha2-256`, requests the exact `key_version`, and accepts only `vault:v{requestedVersion}:<base64>` containing exactly 32 bytes.

### Subject-reference token

`OpenBaoSubjectRefTokenService` implements the existing `ISubjectRefTokenService` contract with a separate selector catalog, Transit key, AppRole credential references and ACL policy. It uses the same existing 32-byte result contract without exposing HMAC key bytes to TagEkyc.

### Catalog and readiness

Configuration root: `TagEkyc:RawExport:ClaimProviders`.

Each provider has an HTTPS address, optional namespace/CA path, AppRole secret references, Transit mount, bounded request timeout, and one or more bindings:

```
KeyId + KeyVersion
    -> TransitKeyName
    -> NotBeforeUtc / NotAfterUtc
```

The active selectors are the already-existing ingress-broker commitment and subject selector id/version fields. Readiness rejects:

- missing or malformed configuration;
- an active selector absent from, not yet active in, or expired from its catalog;
- shared content/subject key names;
- shared content/subject AppRole credential references;
- reuse of either configured KEK credential reference;
- wrong Transit key name or type;
- missing or non-false `derived`, `exportable`, or `allow_plaintext_backup` metadata;
- absent, retired, unavailable, or below-minimum exact key versions;
- provider authentication, authorization, network, TLS, or response-shape failure;
- either deployed AppRole credential being authorized to use the other provider's active HMAC key.

Readiness exposes only stable TagEkyc codes. The underlying exception is retained for local diagnostics but is not returned by the readiness endpoint.

### Production composition

Production API and RawIngressBroker graphs register only the exact OpenBao interface implementations and readiness validator. Fixture/in-process/unknown claim providers remain unavailable to production graphs. The API readiness surface evaluates the claim-provider validator, and the separate RawIngressBroker validates it before listening.

The existing OpenBao HTTP transport and AppRole token session are reused through a shared connection-options contract. No second OpenBao transport, secret resolver, HMAC contract, or broker protocol was created.

The narrow custody composition edits in this write-set close a production graph defect exposed by the focused proof: `IConfiguration` is registered for the existing `OpenBaoProductionCustodyProfileProvider`, and that provider is registered only for exact Production profile `OpenBao`. Production+`Fixture` continues to start health/readiness surfaces while the existing readiness authority rejects it fail-closed.

## Real OpenBao proof

The retained real integration test runs the pinned OpenBao image with TLS, integrated Raft storage, AppRole authentication, Transit, two HMAC keys and two independent policies.

It proves:

1. both keys are created as `hmac`, `key_size=32`, non-derived, non-exportable, and with plaintext backup disabled;
2. readiness validates the actual pinned HMAC metadata shape (`latest_version` / `min_available_version`), not an assumed AES key map;
3. content and subject payloads each produce exactly 32 bytes;
4. TagEkyc output equals a direct root-authorized OpenBao HMAC for the same exact input and key version;
5. OpenBao verifies both HMAC values;
6. the content AppRole cannot call the subject key and the subject AppRole cannot call the content key;
7. production readiness detects an intentionally overbroad Content policy that can call the Subject key and returns the stable separation-invalid code;
8. a catalog version not present in OpenBao fails readiness at the version arm;
9. denied key access returns the existing provider-failure result and never falls back to a fixture;
10. stopped/unavailable OpenBao returns provider failure;
11. caller cancellation is propagated by the provider contract.

The real proof pins the response format of the OpenBao version actually run. The adapter does not infer the single/batch response form from documentation examples.

## Mutation evidence

Three final surgical mutations run against the same final provider source.

The response-version mutation changes only parsing so `vault:v2` is accepted while the configured selector requires version 1. The exact wrong-version theory row becomes RED (`No exception was thrown`); the other four malformed/shape rows remain green.

The separation mutation replaces the two live cross-policy observations with `false`. The real OpenBao overbroad-policy assertion becomes RED (`expected RawExportClaimProviderReadinessException; no exception was thrown`).

The CA-boundary mutation forces the lazy Content client during service construction. Missing, malformed, and directory CA inputs all become RED with raw file/cryptographic exceptions instead of HTTP 503. Its inverse restore returns the three-arm matrix to green.

Restoring each inverse patch returns its gate to green and restores `OpenBaoClaimProviders.cs` byte-exact to:

`B1A3D4BBB07E5A1293D1E57B7FA52A1EE924A368449F319EBE5AEC2EC876C612`.

Together these mutations prove exact response-version binding, deployed AppRole policy isolation, and the lazy CA readiness boundary are load-bearing.

## Focused evidence

| Evidence | Result |
|---|---:|
| Infrastructure build | PASS, 0 warnings/errors, retained transcript |
| Integration-test project build | PASS, 0 errors; two expected seal-drift/generated-code warnings, retained transcript |
| RawIngressBroker project build | PASS, 0 warnings/errors |
| Final focused provider/composition gate | `53/53 PASS`, zero skip |
| Exact external-review correction gate | `5/5 PASS`, zero skip |
| Final real TLS/Raft/AppRole/Transit gate | `1/1 PASS`, zero skip |
| Eager-CA-construction mutation | `0/3`, all three CA boundary arms RED |
| Restored CA readiness gate | `3/3 PASS`, zero skip |
| Skip-cross-policy-observation mutation | `0/1`, exact overbroad-policy assertion RED |
| Restored cross-policy real gate | `1/1 PASS`, zero skip |
| Wrong-response-version product mutation | `4/5`, exactly target wrong-version row RED |
| Restored response-shape gate | `5/5 PASS`, zero skip |
| Failed-run census | 40 runs, 34 failed tests, 0 unclassified |
| Evidence manifest | `61/61` current working-tree bytes |
| Review archive | `49/49` entries byte-equal to the evidence directory |

No full solution suite was run. This is deliberate: the Homeowner requested the OpenBao/claim-provider boundary only.

The custom-CA transport deliberately uses `X509RevocationMode.NoCheck`. The site-owned OpenBao CA profile has no guaranteed CRL/OCSP distribution point, so revocation network checks would make a private-CA deployment nondeterministically unavailable. Trust remains pinned to the configured CA file with hostname and chain validation; this slice does not weaken that trust anchor.

## Debt-registry reconciliation

The candidate updates only rows whose old prose is contradicted by current source/evidence:

- closes the content-commitment Transit adapter and catalog gates;
- closes the subject-token Transit adapter and catalog gates;
- records the already-landed OpenBao production custody profile provider;
- records the already-landed OpenBao KEK provider and PostgreSQL journal at `915df65` / seal rev24;
- records that AlreadyAvailable and the R2–R6 durable path are landed, without claiming process-kill or OS-restart proof.

`POSTGRESQL_LOSSLESS_BACKUP_RESTORE_QUALIFICATION` remains OPEN and continues to block go-live. It is not implemented or weakened here.

## Explicit boundaries

```
New endpoint / protocol          NO
New schema / migration          NO
Raw BIO protocol change         NO
A3 / Layer 2 reopening          NO
KEK state-machine change        NO
SignFlow change                 NO
Backup/restore implementation   NO
Commit / re-freeze / push       NO
```

## Requested review

Review the current working-tree bytes, manifest, census and retained TRX. If both GPTW and CC return PASS on the same bytes, the next action is a product commit followed by one evidence-only successor/re-freeze. No push is authorized by this packet.
