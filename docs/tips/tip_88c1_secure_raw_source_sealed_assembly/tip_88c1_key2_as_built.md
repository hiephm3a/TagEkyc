# TIP-88C1-KEY2 — As-Built: Operation-Scoped Subject-Ref-Token Service

Maps the KEY2 working-tree implementation to the TIP-88C1 planning contract and
the KEY2 build dispatch. The service is the second key prerequisite for C1-B2:
it computes a keyed subject-reference token over caller-supplied bytes while
keeping key material behind an operation-scoped interface.

- **Baseline:** `6aeb8c9` on
  `tip-88a-raw-export-policy-catalog-build`.
- **Working-tree status:** implemented and validated locally; not committed,
  pushed, merged, deployed, or production-activated.
- **Scope:** cryptographic operation seam only. No `SubjectRef` computation,
  source reservation, database migration, model change, real key provider, or
  network call.

## Brief-to-landed map

| Required surface | Landed implementation |
| --- | --- |
| Separate broker capability | `ISubjectRefTokenService` in `TagEkyc.Contracts.RawExport` |
| Separate selector | `SubjectTokenKeySelector(KeyId, KeyVersion)`; it is not assignable to or from `CommitmentKeySelector` |
| Typed result | `SubjectRefTokenResult`: defensive-copy 32-byte token or `SubjectRefTokenFailure.ProviderFailure`; redacted `ToString()` |
| Domain-agnostic operation | `ComputeAsync` performs HMAC-SHA-256 over the exact caller-supplied `lpPayload`; it does not construct or interpret the subject-token domain or LP fields |
| Sole key bridge | Internal `InProcessSubjectRefTokenService`: resolve lease → HMAC → dispose/zero lease → return token |
| Distinct fixture binding | Internal `FixtureSubjectTokenCatalog`, purpose `raw-export.subject-ref-token.hmac`, and a subject-token-only `config:` path |
| DI entry point | `AddTagEkycSubjectRefToken(IConfiguration)` |
| Seam and behavior proof | `Tip88C1Key2SubjectRefTokenTests` |

## Capability separation

KEY2 does not add a second global `IProtectedValueCatalog` registration. KEY1's
catalog/resolver registration is already singleton-scoped; sharing that slot
would make resolution depend on registration order. Instead, the KEY2 DI
factory constructs a capability-scoped internal resolver with
`FixtureSubjectTokenCatalog`, while reusing the existing provider registry,
resolver implementation, options, and time provider.

This gives both registration orders the same observable result:

```text
AddTagEkycContentCommitment → AddTagEkycSubjectRefToken
AddTagEkycSubjectRefToken → AddTagEkycContentCommitment
```

The services also use different public selector types, protected-value
purposes, catalog types, configuration paths, fixture keys, and golden MACs.
No operation service references the other capability's catalog.

This is in-process capability separation, not per-identity process isolation.
The planning §D4 broker/isolated-crypto identity boundary remains a deployment
gate and is not claimed by KEY2.

## Golden vector

- Subject-token fixture key (NON-SECRET):
  `6665646362613938373635343332313066656463626139383736353433323130`
  (`fedcba9876543210fedcba9876543210`).
- Caller LP payload: `0000000464656D6F`.
- Subject-ref token:
  `B9643CE6382558106D53D5463EEB6C90033EF90620D5C3E3AEC240198BA0D02A`.
- KEY1 fixture key is different:
  `3031323334353637383961626364656630313233343536373839616263646566`.
- Independent recompute:

```text
node -e "const c=require('crypto');const k=Buffer.from('6665646362613938373635343332313066656463626139383736353433323130','hex');const p=Buffer.from('0000000464656d6f','hex');console.log(c.createHmac('sha256',k).update(p).digest('hex'))"
```

The operation intentionally does not embed
`TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN:C1:V1` or the
`{domain, StableDataScopeId, ControllerIdentity, NFC(SubjectRef)}` layout.
C1-B2 owns construction of those caller bytes.

## Gate boundaries

- **`C1-KEY2-VAULT-ADAPTER-GATE`** — provide the real
  OpenBao-Transit/HSM subject-token operation behind
  `ISubjectRefTokenService`, with production identity and readiness evidence.
- **`C1-KEY2-CATALOG-GATE`** — replace the fixture selector-to-reference
  binding with a real, versioned subject-token catalog and rotation/validity
  policy.

KEY2 does not satisfy either gate. It uses a documented non-secret
configuration fixture, no network, and no production key.

## Acceptance evidence

Named tests cover:

- public/Application seam isolation and absence of a key-returning member;
- byte-exact independent golden vector;
- capability separation in both DI registration orders;
- lease disposal and zeroing;
- unknown selector and missing fixture configuration as typed
  `ProviderFailure`.

Mutation runs deliberately break each load-bearing mechanism and restore the
source byte-identically afterward. The final report records the exact RED
assertion and full per-project test census.

