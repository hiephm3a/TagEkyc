# TIP-88C1-B2-DEKKEK — Attempt DEK/KEK provider as built

Status: IMPLEMENTED IN WORKING TREE — NOT COMMITTED

Baseline: `10594dc82758e7aa76f6e9ea5a0d0c2d0cc63f87`

## Envelope model

`IAttemptKeyProvider` accepts the R1-frozen
`AttemptKeyReference` and returns an `AttemptKeyResult`. The KEK remains
confined to the fixture provider and is used only for AES-256-GCM wrap/unwrap.
No contract member exposes KEK bytes. The only key material crossing the
boundary is a 32-byte DEK through `IAttemptDekLease`, because the future R2
AEAD operation must use that DEK in-process. Disposing the lease zeroizes its
owned buffer and revokes subsequent material access.

`WrappedDekMetadata` is opaque nonce + authentication tag + ciphertext. The
reservation reference is authenticated as associated data.

## Fixture custody and idempotency

The fixture catalog is bound to the PROFILES reference:

- provider `fixture-kek-provider-v1`;
- KEK id `fixture-kek-v1`, version 1;
- fingerprint
  `f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2`.

The fingerprint remains the public SHA-256 reference defined by PROFILES; it
is not a disclosure or hash of secret KEK material. Fixture KEK bytes use the
distinct purpose `tagekyc-tip88c1-fixture-attempt-kek-material-v1` and are
separate from the KEY1 commitment and KEY2 subject-token fixtures.

On first use, the singleton process-local store has no entry. The provider
generates a random 32-byte DEK, wraps it and stores only the wrapped form under
`AttemptKeyReservationId`. Repeated calls, including after recreating the
provider over the same store, unwrap the same DEK. Reusing the reservation id
with different frozen context returns typed `ReservationConflict`. An
unknown reference returns typed `ReferenceMismatch`.

## Production gate

Configuration `TagEkyc:RawExport:AttemptKey:Profile` accepts only
`Fixture`. Missing, unknown and Production+Fixture fail closed with:

- `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_MISSING`;
- `PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_INVALID`;
- `PROD_RAW_EXPORT_ATTEMPT_KEY_FIXTURE_ACTIVE`.

The API readiness pipeline includes this validator. This slice adds no network,
vault/HSM, database migration, source broker or raw-biometric operation.

## Deferred boundary

`C1-B2-KEK-DEK-PROVIDER-GATE` is narrowed: the fixture envelope operation,
zeroizable DEK lease and process-local wrapped-DEK store are landed. Production
still requires a real vault/HSM unwrap operation in which the KEK never enters
the application process, plus a real durable wrapped-DEK store surviving
process loss.
