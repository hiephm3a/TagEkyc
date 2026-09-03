# TIP-88C1-B2-BETA — Existing-candidate outcomes as built

Status: IMPLEMENTED IN WORKING TREE — NOT COMMITTED
Baseline: `77cfd53ad10726b76f7f3521231698454a681d22`

## Corrected begin semantics

B1 compared the full `IngressIdentityFingerprint`, which contains
`IngressIdempotencyKey`; alternate aliases therefore failed before the
Existing branch. BETA re-emits begin with unchanged signature and compares the
canonical claim explicitly on client application, authenticated principal,
producer, capture agent, verification session, acceptance, artifact, revision,
raw class, challenge hash and authority snapshot. Only the idempotency key may
differ. The canonical claim/fingerprint is not rewritten; the new alias stores
its attempted identity/envelope fingerprints and its own fenced token.

## Existing completion

The broker uses the canonical claim's frozen historic commitment-key selector,
never a caller-selected/current selector. A KEY1 failure crosses into complete
as a typed provisional result (Existing token plus null commitment). Complete
validates lineage and runs the fresh authority/consent barrier first.

- `ExistingMatch`: bind to the existing source, allocate no second graph.
- Digest mismatch: `RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT`; a fresh alias
  becomes terminal `ConflictTombstone`.
- Historic-key unavailable: preserve evaluating alias and canonical graph.
- Authority/consent loss wins over simultaneous historic-key unavailability
  with `SOURCE_RETENTION_NOT_AUTHORIZED` and no source disclosure.

## Function lifecycle

BETA `CREATE OR REPLACE`s begin and complete with unchanged signatures, so
function identity, owner and ACL are preserved and no overload is introduced.
`Down()` restores the exact landed B1 begin body and exact CORE complete body.
Landed migration bytes are unchanged.

## Validation

Named tests cover exact immutable match plus every non-key mismatch, digest-only
match/conflict, tombstoning, historic-key unavailability, barrier precedence
and a single canonical source graph. Mutation and full-suite census are in the
builder closeout report.
