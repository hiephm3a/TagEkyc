# Production Raw Export Claim Providers — Intent Ledger

Status: IMPLEMENTATION AUTHORIZED / CANDIDATE ONLY  
Baseline: seal revision 24, HEAD `62655d1`  
Pilot: PI-TAG-001 / TIP-88C1 / High-risk  
Selected modules: cryptography/key management; raw/restricted data; governance/docs  
Independent review: required before commit  
Round-5 checkpoint: enabled; round-10 hard stop: enabled

## Intent

Replace the fixture-only content-commitment and subject-reference-token claim providers in the Production graph with two distinct OpenBao Transit HMAC providers while preserving the existing public contracts and fail-closed broker behavior.

## Expected Outcome

- `IContentCommitmentService` and `ISubjectRefTokenService` use dedicated OpenBao `hmac` keys and distinct AppRole policies in Production.
- `KeyId + KeyVersion` selects one immutable configured Transit key name and exact key version within an activation window.
- HMAC material never enters the TagEkyc process; only a 32-byte HMAC result does.
- The Production broker validates both provider capabilities before serving traffic; the API readiness surface reports the same gate.
- Fixture catalogs and in-process HMAC services cannot enter the Production graph.
- No schema, migration, protocol, A3, KEK, Layer-2, or SignFlow change occurs.

## Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Reuse the existing OpenBao TLS/AppRole transport | Avoid a second secret-provider subsystem | Refactor connection options only; preserve KEK behavior | Does not merge KEK and HMAC permissions |
| Dedicated Content and Subject Transit `hmac` keys | Purpose and policy separation | Two catalogs, clients, services and policies | No shared key or credential |
| Configuration-backed immutable selector catalogs | Existing contracts already carry ID/version; no persistence is required | Exact selector/version/window mapping | No dynamic catalog administration |
| Transit `sha2-256` HMAC | Matches the existing 32-byte HMAC-SHA-256 contract | Parse and validate exact Transit output shape | No algorithm negotiation |
| One debt update with the product successor | Avoid a documentation-only seal churn | Audit debt rows 238–247 against current source | Does not close unproven debt |

## Rejected / Deferred Branches

| Branch | Disposition | Why | Follow-up |
| --- | --- | --- | --- |
| New database catalog | Rejected for this slice | No inventory evidence requires persistence | STOP/RRI if later proven necessary |
| Reuse KEK Transit key/AppRole | Rejected | Violates purpose/policy separation | None |
| In-process HMAC fallback | Rejected | Secret key material would enter process memory | Fixture remains test-only |
| PostgreSQL backup/restore qualification | Deferred | Independent go-live operations gate | Remains OPEN |

## Invariant Trace Matrix

| ID | Requirement | Owner | Preconditions/order | Contract | Outcome/error | Evidence/residue | Proof | Negative/mutation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CP1 | Content HMAC is produced in Transit | `OpenBaoContentCommitmentService` | exact active selector before call | `IContentCommitmentService` | 32-byte success or ProviderFailure | no secret/key bytes retained | real OpenBao exact-output test | fixture/wrong-key/malformed response RED |
| CP2 | Subject token is produced in distinct Transit key | `OpenBaoSubjectRefTokenService` | exact active selector before call | `ISubjectRefTokenService` | 32-byte success or ProviderFailure | no secret/key bytes retained | real OpenBao exact-output test | cross-policy access RED |
| CP3 | Production never falls back to fixtures | Production DI guard | production registration before broker construction | service descriptors | startup/readiness failure | no request admitted | production graph test | fixture registration RED |
| CP4 | Key metadata is production-qualified | claim readiness validator | TLS/AppRole then metadata read | configured catalog | ready or deterministic failure | read-only provider observation | real key metadata test | wrong type/exportable/plaintext-backup/version RED |

## Conditional Matrices

Outcome/precedence: provider selection and caller cancellation take precedence over generic provider failure; all provider/network/shape failures collapse to the existing typed `ProviderFailure`.  
Shape/nullability: Transit response must be exactly `data.hmac = vault:v<exact-version>:<base64-32-bytes>`; all other shapes fail closed.  
Ordering: resolve selector/window → authenticate dedicated AppRole → call exact HMAC endpoint/version → validate response → return copied 32 bytes.  
Test-bite: positive real-provider output plus wrong selector/window/version, policy isolation, response-shape, network and fixture mutations are required.

## Omitted Modules

- SQL/schema/transaction: not applicable; inventory found no durable state requirement.
- API: no endpoint or DTO changes; only existing readiness integration is used.
- Worker/queue: not applicable; providers are synchronous request dependencies.

## Debt / Gap Impact

Audit rows 238–247. The slice may close the four KEY/KEY2 adapter/catalog gates and factually update KEK/profile rows only where current source proves closure. Backup/restore, real-site qualification and unrelated fixture/P0 gates remain open.

## Non-Claims

This slice does not qualify a site, prove PostgreSQL lossless restore, authorize deployment, change Raw BIO/A3/Layer-2 semantics, or close unrelated P0 gates.

## Dispatch Readiness

Implementation is authorized by the Homeowner within this ledger. Any need for a table, migration, persistence-model change, new endpoint/protocol, A3/KEK/SignFlow change, or broader authority triggers STOP/RRI.
