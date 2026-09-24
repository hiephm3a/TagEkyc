# Raw-export delivery correction — migration hygiene successor v4

**Status:** `READY_FOR_INDEPENDENT_REVIEW / TECHNICAL_PASS_CANDIDATE`

**Date:** 2026-09-24

**Predecessor packet:** `raw_export_delivery_combined_review_packet_v3.md`

**Manifest SHA-256:** `A72D4527532214D2E650432F59923046DF6B040329A2D066F0756B2DAF79EB90`

This bounded successor closes the two migration-proof findings from the independent v3 review. It changes no product source, SDK source, public contract, SignFlow source or A3 governance state.

## 1. F1 closed: current migration tripwire

`Tip88C1C6BA3MigrationTests.CurrentMigrationId` now names the actual last migration:

```text
20260924130000_RawExportLegacyConsentClassFence
```

The focused current-byte test resets a real PostgreSQL database, applies the complete EF history, and asserts that discovered and applied migrations both end at `130000` with no pending model changes.

## 2. F2 closed: consent-fence Down/Reapply proof

The new focused test starts from current migration `130000` and reads all three production functions from the PostgreSQL catalogue:

```text
raw_export_stage_verified_source_ciphertext
raw_export_commit_staged_source
raw_export_publish_available_source
```

It then proves this exact sequence:

```text
current 130000
  → all three definitions contain the exact RawClass fence
  → owner is tagekyc_raw_export_deployer
  → SECURITY DEFINER is true
  → search_path=pg_catalog is retained
  → ACL is present

Down to 120000
  → all three definitions lose only the RawClass fence
  → owner / SECURITY DEFINER / configuration / ACL remain byte-for-byte equal

Reapply 130000
  → all three complete function definitions and security metadata equal the initial current state
  → EF history ends at 130000
  → no pending model changes
```

Focused restored evidence:

```text
raw-export-delivery-migration-hygiene-final-v2.trx
2/2 PASS, 0 skip
SHA-256 9D99C0B672093D1ACFA84BF306C63E69AD0007BB92E76FF0EBF1378C976836A4
```

## 3. Failed-run accounting

Two predecessor runs are retained rather than deleted:

```text
raw-export-delivery-migration-hygiene.trx
1/2 PASS — SUPERSEDED_RED
The first test compared an unsorted expected function list with a sorted catalogue result.

raw-export-delivery-migration-hygiene-final.trx
1/2 PASS — SUPERSEDED_RED
The command used --no-build and reran the predecessor binary.
```

Neither run reached a product/migration failure. Both point to the final 2/2 successor and are classified by the whole-repo census.

Current whole-repo accounting:

```text
TRX total              1,324
Server TRX             1,153
Agent TRX                171
failed TRX              525
current-slice failed      31
unclassified               0
count mismatch             0

inventory SHA-256
860830D1C2810E4A19074BBF838D60EF6B419B489CDE2E98D4D763C8B731E67F

failed-run census SHA-256
3B237DC6817576CF641CF6639970AB461FD6FE872DA670C17F51D950E7170139
```

## 4. Bounded rerun decision

No product or SDK source changed from v3. Therefore the same-Job E2E, consent mutation, SDK gate and C5 profile gate remain the exact predecessor evidence and were not rerun. This successor runs only the two migration tests requested by review.

The build continues to emit the expected `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` warning. This test-only successor does not re-mint the A3 activation seal.

## 5. Requested disposition

```text
F1 current migration tripwire             CLOSE candidate
F2 130000 Down/Reapply + owner/ACL proof   CLOSE candidate
v3 product/SDK conclusions                 unchanged
raw-export delivery slice                  READY_FOR_RATIFICATION candidate
```
