# Raw-export delivery correction — combined review packet v3

**Status:** `READY_FOR_INDEPENDENT_REVIEW / TECHNICAL_PASS_CANDIDATE`

**Date:** 2026-09-24

**Manifest SHA-256:** `1A185AF6A69A49B456FCD061298AB1EAC133DB9EA998FBC45964FEABD4C67B46`

This successor answers the remaining review findings without adding a second delivery engine or changing SignFlow architecture.

## 1. Boundary correction: client belongs to the TagEkyc SDK

The public client and its tests have moved out of `Codex_SignFlow` into:

```text
sdk/TagEkyc.RawExport.Client/
tests/TagEkyc.RawExport.Client.Tests/
```

`TagEkyc.sln` now builds both projects. The client remains standalone: it depends only on the .NET runtime, owns no Server DI registration, and does not reference SignFlow. SignFlow's only change in this successor is deletion of the misplaced copy. A future SignFlow SDK integration may consume the package; this packet does not implement that integration.

## 2. M1 closed: no automatic privilege elevation

The product now has two exact managed-recipient profiles:

```text
C3C4RecipientV1
  business.raw-export.package.download
  business.raw-export.package.references.read

RawExportDeliveryRecipientV2
  business.raw-export.authorize
  business.raw-export.job.manage
  business.raw-export.package.download
  business.raw-export.package.references.read
```

Migration `20260924120000_RawExportDeliveryRecipientCredential` does not update existing policies or API keys. Existing download-only credentials remain download-only after upgrade. Full-flow authority is issued only after an explicit enrollment using `RawExportDeliveryRecipientV2`.

The exact profile is enforced by the application service, authentication policy, API-key store, SQL functions, readiness validator, EF constraint and contract tests.

Current evidence:

```text
raw-export-delivery-c5-profile-final.trx   3/3 PASS
```

It includes the predecessor-to-current migration proof, explicit full-profile issuance, C504 least-privilege preservation and the C526 production C1–C4 chain.

## 3. Joined same-JobId proof

The new test uses one public SDK acquisition and one exact durable identity chain:

```text
TagEkycRawExportClient
  → POST authorization
  → POST job J
  → durable work source acquires J
  → production C1 resolver/assembly
  → production C2 recipient package
  → C4 lists package P for J
  → production C3 downloads P
  → SDK decrypts and decodes P
  → exact DG2 and selfie bytes returned
```

The assertions bind `VerificationSessionId`, `JobId`, `PackageId`, recipient identity, both raw classes and the final plaintext bytes. It does not join two independent fixtures and call them one flow.

```text
raw-export-delivery-same-job-e2e-final.trx   1/1 PASS
SHA-256 C8B702A7C18C0BF82A13884FE9602AAA3EC65B51E4EFA04FA88ED79754B94D87

raw-export-delivery-restored-final.trx       1/1 PASS
SHA-256 4A5EF812E5AAFBDC4F931045D0C97125E1BD227B13E20DD54AE2183F4533DF06
```

The SDK's local unit/codec gate is also current:

```text
raw-export-delivery-sdk-unit-final.trx       4/4 PASS
SHA-256 90800B5D7CCBAACABEB99E7989DCC73CC29748F005102EE7673E2703E6434DF1
```

## 4. Two product defects found by the joined proof

### 4.1 Multi-class consent selected an arbitrary row

The R3/R4/R6 SQL functions called `raw_export_resolve_subject_consent_for_authorization` and assigned the first returned row to a scalar record. For a session containing both `ChipDg2Portrait` and `LiveSelfieImage`, one source could be rejected as `SourceRetentionNotAuthorized` depending on row order.

Migration `20260924130000_RawExportLegacyConsentClassFence` adds the missing exact `RawClass` predicate to stage, commit and publish. It is reversible and preserves the existing functions, owners and ACLs.

The mutation changes only equality to inequality:

```text
raw-export-delivery-consent-class-mutant.trx   0/1 PASS
failure: expected Staged, actual SourceRetentionNotAuthorized
SHA-256 F8E163A35591FF5226ED89F36EE7674D294DDF94FA6FD84730E2EAC73C204FF5

restored migration source SHA-256
31A7B2148934E0B4F863DC92600E12F6580DD09750B68DA6CD1ED7C4B5F5606F
```

### 4.2 SDK interpreted the wire digest with the wrong encoding

The Server C3 contract emits the package ciphertext digest as canonical base64url. The first joined run reached the real delivery response and exposed that the SDK attempted hexadecimal decoding. The SDK now requires canonical unpadded base64url and exactly 32 decoded bytes before fixed-time comparison.

The RED run is retained as `PRODUCT_DEFECT_PREDECESSOR`; both the final joined proof and the 4/4 SDK gate pass on current bytes.

## 5. L1 closed: C528 is named debt, not a historical exclusion

`raw-export-delivery-correction-c5-full-restored.trx` remains `24/25`, but its classification is now:

```text
KNOWN_TEST_DEBT
```

The named debt is C528's assumption that a rollback may cross a later A3 current-body guarded migration. It is not silently excluded and is not used as green evidence. The bounded credential successor's own Down/Reapply behavior remains covered by the separate migration proof.

## 6. Failed-run accounting

The whole-repo census covers Server and Agent trees, including root-level result directories.

```text
TRX total              1,321
Server TRX             1,150
Agent TRX                171
failed TRX              523
current-slice failed      29
unclassified               0
count mismatch             0
```

New joined-flow REDs are retained and classified as product-defect predecessors, superseded correction/fixture attempts, or the deliberate consent-class mutation. No RED is renamed as PASS.

```text
whole-repo inventory SHA-256
C315970886983CC27691E891E708679DBD605B033323336D267EC4601151CE02

failed-run census SHA-256
CB4EF84A0375A816CD3F9BEF31B3C225D0A9B78FAE3633B39EC07FAFB14CA26B
```

## 7. Build and boundaries

```text
TagEkyc.sln build                         PASS
C5 contract tests                        2/2 PASS
C5 profile/migration/current chain       3/3 PASS
SDK unit/codec                           4/4 PASS
same-JobId joined flow                   1/1 PASS
consent-class mutation                   0/1 RED, intended
restored joined flow                     1/1 PASS

SignFlow runtime/TSP architecture        unchanged
assembly implementation                  reused, not rebuilt
package encryption/decryption codecs     reused, not rebuilt
A3 partition/census                      unchanged at product-complete state
A3 seal                                  not re-minted
push                                     not performed
```

The build emits the expected `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` warning because TagEkyc product bytes changed after the last A3 freeze. This packet does not launder that warning by re-minting activation governance as part of a delivery correction.

## 8. Requested review disposition

```text
SDK ownership boundary                   ACCEPT candidate
M1 least-privilege migration             CLOSE candidate
same-JobId public delivery chain         ACCEPT candidate
multi-class consent defect               CLOSE candidate
SDK base64url digest defect              CLOSE candidate
L1 C528 debt classification              CLOSE candidate
SignFlow architecture impact             NONE
```
