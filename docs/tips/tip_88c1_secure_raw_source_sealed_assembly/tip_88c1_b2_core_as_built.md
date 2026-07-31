# TIP-88C1-B2-CORE — NewCandidate complete/R1 as-built

Version: 0.1  
Status: IMPLEMENTED IN WORKING TREE — VALIDATION IN PROGRESS — NOT COMMITTED  
Baseline: `7c958887d4878afbb01a470652246325b2984531`  
Authority: CORE dispatch + rev.2 + ratified consent-policy addendum

## 1. Landed boundary

This slice turns only a B1 `ClaimEvaluating` NewCandidate shell into a
recoverable R1 reservation. It does not implement ExistingMatch,
FingerprintConflict, conflict tombstones, historic-key recovery, ciphertext,
real key creation, object-store I/O, R2–R6, delivery, or public API activation.

The transaction owner is
`RawExportSourceClaimComparisonBroker`. Its order is:

1. validate the evaluation token and read the frozen shell;
2. recompute the producer-envelope fingerprint;
3. resolve the authority-declared consent policy and effective consent;
4. call KEY1 and KEY2 outside a database transaction;
5. resolve the fixture custody/KEK reference and deterministic nonce/framing
   metadata;
6. open one short R1 transaction;
7. call `complete_raw_export_source_ingress_claim`;
8. return `NewReservation` only after commit.

No public API registration was added.

## 2. Persistence shape

`raw_export_source_reservations` is immutable and stores source-stable recovery
context, including the content commitment, subject token, frozen authority and
consent-policy selectors, producer retention fields, custody profile selectors,
effective/reservation expiry and admission/source fingerprints.

`raw_export_source_encryption_attempts` is immutable and stores R1 attempt
identity, fence, provisional object identity, idempotent key-reservation
identity, KEK reference, framing/nonce metadata, ownership lease and attempt
fingerprint. `R2TerminationDisposition` remains null.

`raw_export_source_head` is the fenced mutable CAS surface and selects the
current R1 attempt with
`CustodyState = Reserved`, reservation revision 1 and fence 1.

`uq_raw_export_source_ingress_source` binds one ingress claim to one source.
The B1 ingress write guard is replaced in place so its existing claim trigger
also rejects a `Reserved` transition unless the reservation already exists.
This preserves the exact B1 trigger manifest while retaining the backstop.
All three tables are deployer-owned, have no direct runtime or broker table
privileges, and are writable only through the SECURITY DEFINER R1 context.

## 3. Consent-policy selector

The CORE migration adds:

```text
raw_export_authority_snapshots.ConsentPolicyId
raw_export_authority_snapshots.ConsentPolicyVersion
```

Granted events require both; Withdrawn/Revoked events require both null. The
composite FK targets
`raw_export_policy_versions(PolicyId, PolicyVersion)`. The append and resolver
functions carry the exact selector. Both broker preflight and the in-transaction
second barrier call
`raw_export_resolve_subject_consent_for_authorization(session, policyId,
policyVersion)` with the authority-declared pair.

Production has no seeded/default/latest policy. Tests create a fresh version 1
through `EfRawExportPolicyRepository` using the landed
`RAW_EXPORT_REQUIREMENTS` v1 rule set. Production policy provisioning remains a
deployment gate.

## 4. Fingerprints and fixture strategy

The landed domains are:

```text
tip-88c1-ingress-admission-v1
tip-88c1-source-reservation-v2
tip-88c1-encryption-attempt-v1
tip-88c1-nonce-seed-commitment-v1
tip-88c1-framing-parameters-v1
```

All use C1 length-prefixed UTF-8/NFC canonicalization. UUIDs are lowercase
RFC-4122 text without hyphens; byte values are lowercase hexadecimal; timestamps
are UTC with six fractional digits.

Fixture Random96 uses:

```text
NonceDerivationSeedReferenceOrWrappedSeed = "none"
NonceDerivationSeedCommitment =
  C1HashCanonical(nonce-seed-domain, NonceStrategyId, "none")
FramingParametersDigest =
  C1HashCanonical(framing-domain, EncryptionSuiteId,
                  EncryptionFramingVersion, ChunkSize, NonceStrategyId)
```

Golden vectors:

```text
AdmissionFingerprint =
54BFF92D062CCA6F9D6702D09075B473C9747A6669772D54F55345B78D6BEA62

SourceReservationFingerprint =
1D740F22E174288D24FC17C884F750F1975D88997529717AD9D8BCDEF56CF500

EncryptionAttemptFingerprint =
C53985AA6523F79BD92FD2B10EC67D13DF5BA5402E35D19EB8A7050C9EC1A431

NonceDerivationSeedCommitment =
DFA71064BA60F7E073A333F71031969FF2DAA847043976D7FF8F58C40F6005EA

FramingParametersDigest =
87AB6A5C24A28C357F097A1623B787E3F34B4EE283B11DE47FCE53F19A4258D8
```

`C1B2CORE_three_fingerprint_golden_vectors_are_pinned` freezes the exact
preimages. The independent PowerShell recomputation writes each NFC UTF-8 scalar
behind a four-byte unsigned big-endian length and hashes the resulting stream
with `SHA256.Create().ComputeHash`; it does not load a TagEkyc assembly.

The plaintext digest exists only in the broker command and the KEY1 LP preimage.
It is not passed to SQL, persisted, logged, audited, or returned.

## 5. Outcomes

Reachable CORE outcomes are exactly:

```text
NewReservation
SOURCE_RETENTION_NOT_AUTHORIZED
CLAIM_TOKEN_INVALID
RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID
```

The effective-cap outcome CAS-transitions only
`CurrentClaimEvaluationDisposition: Active -> Completed`; alias state stays
`Evaluating`, claim state stays `ClaimEvaluating`, and no reservation, attempt or
head row is created.

## 6. ACL and deployment gates

`complete_raw_export_source_ingress_claim` is executable by
`tagekyc_raw_export_claim_broker`, not `tagekyc_runtime`. The broker role is
NOLOGIN. The isolated claim-comparison worker login inherits both
`tagekyc_runtime` for the landed resolver functions and the broker role for
`complete_*`; the broker role itself receives no extra execute grant on the
landed E3 function manifest.

Open gates carried forward:

- `C1-B2-CORE-BROKER-ROLE-DEPLOYMENT-GATE`;
- `C1-B2-BETA-DEFERRED-OUTCOMES-GATE`;
- `C1-R2-R6-DEK-KEK-OBJECT-GATE`;
- `C1-KEY2-CATALOG-GATE`;
- `C1-NONCE-FRAMING-STRATEGY-GATE` for a real seed-derived nonce strategy;
- production consent-policy provisioning gate.

## 7. Verification map

- `C1B2CORE_new_candidate_commits_full_recovery_context_atomically`
  proves happy-path R1 and exact recovery readback.
- `C1B2CORE_envelope_mismatch_is_token_invalid_with_zero_residue`
  proves token-bound producer envelope handling.
- `C1B2CORE_effective_cap_completes_evaluation_without_r1_rows`
  proves the ratified terminal disposition.
- `C1B2_grant_rejects_unknown_consent_policy_version_binding`
  proves the authority selector FK.
- `C1B2CORE_complete_is_broker_only_and_tables_reject_direct_dml`
  proves broker-only execute and table denial.
- `C1B2CORE_three_fingerprint_golden_vectors_are_pinned` freezes the
  Admission, SourceReservation and EncryptionAttempt fingerprint preimages and
  expected SHA-256 outputs.
- `C1B2CORE_nonce_and_framing_codecs_are_pinned_and_plaintext_digest_is_absent`
  pins fixture vectors and the prohibited column surface.
- `C1B2CORE_source_reservation_is_append_only_even_as_owner` proves immutable
  R1 evidence.
- `C1B2CORE_reserved_claim_requires_a_source_reservation` proves the
  claim/reservation transition backstop.
- `F6_migration_apply_rollback_reapply_restores_snapshot_functions_and_acls_exactly`
  proves rollback/reapply and snapshot/ACL restoration.

Observed mutation-red evidence:

- adding a 64-byte intended identifier reddened
  `C1B2CORE_all_intended_identifiers_are_at_most_63_utf8_bytes` with actual 64;
- neutralizing the immutable-row `TG_OP` branch reddened
  `C1B2CORE_source_reservation_is_append_only_even_as_owner` because no
  exception was thrown;
- removing the `Reserved` reservation-existence branch from the B1 ingress
  write guard reddened
  `C1B2CORE_reserved_claim_requires_a_source_reservation` because the invalid
  transition succeeded;
- removing `fk_raw_export_authority_snapshot_consent_policy` reddened
  `C1B2_grant_rejects_unknown_consent_policy_version_binding` because the
  unknown selector was accepted.

ModelSnapshot SHA-256:

```text
7BD13A5CC701D7E50A81AF4FD522BDA42E0749F491D81279F1A876E4ED6EAEE9
```

Final validation:

```text
Release build:       0 warnings, 0 errors
B1 targeted:         7 passed, 0 failed
E3 targeted:        28 passed, 0 failed
CORE targeted:       9 passed, 0 failed
AUTH targeted:       8 passed, 0 failed
ArchTests:          93 passed, 0 failed
Pending model:       clean
Full solution:     846 passed, 0 failed, 1 intentional manual-generator skip
```
