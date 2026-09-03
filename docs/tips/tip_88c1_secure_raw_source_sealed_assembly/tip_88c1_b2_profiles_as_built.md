# TIP-88C1-B2-PROFILES — As-Built: Fixture Custody Profiles

This document maps the B2-PROFILES implementation to the TIP-88C1 custody
profile prerequisites for B2-CORE. The slice supplies fixture-only frozen
references and mandatory custody time bounds. It does not add a database
surface, key operation, provider I/O, broker, completion path, or source row.

- Baseline: `4d01ffdda440f0dc8dfbce60c35a4bdebca4e386`.
- Profile key: `TagEkyc:RawExport:CustodyProfile:Profile`.
- Accepted profile: `Fixture` only.

## CORE facade

`ICustodyProfileProvider` is an internal Infrastructure contract with exactly
three getter-only surfaces:

```text
ActiveSourceEncryptionProfile
ActiveKekReference
TimeBounds
```

`AddTagEkycCustodyProfiles(IConfiguration)` registers the fixture catalogs,
the mandatory parsed time-bound state, and the facade. The contract contains
only opaque strings, integer versions/sizes, and `TimeSpan` values. It exposes
no enum, provider implementation, key material, key handle, or operation
method.

## Source-encryption fixture

```text
StorageProfileId                 fixture-storage-local-v1
SourceEncryptionProfileId        fixture-source-encryption-v1
SourceEncryptionProfileVersion   1
EncryptionSuiteId                fixture-aead-aes256gcm-v1
EncryptionFramingVersion         1
NonceStrategyId                  fixture-nonce-random96-v1
ChunkSize                        1048576
```

These values are opaque routing/audit references. Business, authority,
policy, retention, and error behavior must not branch on their contents.

## KEK-reference fixture

```text
KeyProviderId    fixture-kek-provider-v1
KekId            fixture-kek-v1
KekVersion       1
KekFingerprint  f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2
```

The fingerprint is the public, non-secret SHA-256 value of
`tagekyc-tip88c1-fixture-kek-reference-v1`. The entire bundle is a frozen
reference only. It is not key material and does not perform key resolution,
wrapping, encryption, network access, or provider I/O.

## Mandatory custody time bounds

| Configuration key | Unit | Accepted range | Fixture value |
| --- | --- | ---: | ---: |
| `RawExportSourceClaimSafetyMarginMilliseconds` | milliseconds | `[1,30000]` | `5000` |
| `RawExportSourceMaximumRemainingContinuationWindowSeconds` | seconds | `[1,3600]` | `3600` |
| `RawExportSourceEncryptionAttemptDeadlineSeconds` | seconds | `[1,3600]` | `900` |
| `RawExportSourceOwnershipLeaseDurationSeconds` | seconds | `[1,3600]` | `300` |

All four keys are mandatory and have no implicit default. Missing,
non-integer, below-minimum, or above-maximum input fails readiness with
`RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID`. B2-CORE consumes the parsed
`CustodyTimeBounds`; it cannot obtain a value from an invalid state.

## Readiness and Gate-B boundary

Readiness precedence is:

```text
missing/empty profile
  -> PROD_RAW_EXPORT_CUSTODY_PROFILE_PROFILE_MISSING
unknown profile
  -> PROD_RAW_EXPORT_CUSTODY_PROFILE_PROFILE_INVALID
Production + Fixture
  -> PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE
Development/Test + Fixture + invalid time bounds
  -> RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID
Development/Test + Fixture + valid time bounds
  -> ready
```

The fixture catalog can never make Production ready. A future Gate-B/R2 slice
must bind a ratified production custody profile to a real KEK provider and key
operation. That future work remains outside this slice.

## Mutation evidence

The following scratch mutations were observed red and restored:

- widening the safety-margin maximum from `30000` to `30001` caused
  `C1B2_profiles_each_invalid_time_bound_fails_closed_without_default` to
  report that no readiness exception was thrown;
- changing fixture `ChunkSize` from `1048576` to `1048577` caused
  `C1B2_profiles_provider_returns_exact_nonproduction_fixture_bundles` to
  report the exact value mismatch;
- bypassing the Production fixture check caused
  `C1B2_profiles_readiness_precedence_and_fixture_gate_are_exact` to report
  that no readiness exception was thrown;
- adding a scratch `ResolveKey()` method to the facade caused
  `C1B2_profiles_contract_is_opaque_and_has_no_key_operation_surface` to fail
  its getter-only manifest.

## Non-claims

This slice provides no real storage profile, encryption implementation, KEK
provider, DEK/KEK operation, network capability, source reservation,
encryption attempt, broker, completion path, production readiness, or raw-byte
handling. It changes no migration, DbContext model, or ModelSnapshot.
