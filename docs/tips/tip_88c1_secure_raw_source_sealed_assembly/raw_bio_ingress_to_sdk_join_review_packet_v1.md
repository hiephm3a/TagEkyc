# RAW BIO ingress-to-SDK joined delivery correction — review packet v1

**Status:** `READY_FOR_INDEPENDENT_REVIEW / TECHNICAL_PASS_CANDIDATE`

**Date:** 2026-09-25

**Baseline:** `dd4f027a5a0e875e7f70c132acd2d94836c3aba3`

**Companion manifest SHA-256:** `06D7620CFA0F2A00E183A5303C426F9D1DB4CFDCC5FAD2063EC98EA4518ACCBB`

All manifest hashes are SHA-256 over canonical Git object content, after clean filters and line-ending normalization, rather than checkout-specific working-tree bytes.

## 1. Repository inventory before implementation

```text
SEARCHED
  raw-ingress endpoint, admission, broker, body/custody pipeline
  R3 Available publication
  authorization/job control plane
  DurableRawExportAssemblyWorkSource and DurableWorker
  assembly/C2 package production services
  C3/C4 delivery endpoints
  standalone TagEkyc.RawExport.Client SDK

FOUND
  every stage above already existed and had separate evidence

MISSING
  one joined proof from raw ingress to SDK decode
  retained-mode jobs were not selectable by the production durable work-source

DO NOT REIMPLEMENT
  API, authorization engine, job engine, raw ingress, custody pipeline,
  work-source, worker, assembler, package service, SDK, or SignFlow
```

The predecessor evidence proved two adjacent paths but not their composition:

```text
raw ingress -> two Available publications
fixture R3 sources -> job -> DurableWorker -> package -> SDK decode
```

The joined test exposed a production defect rather than another missing subsystem.

## 2. Product defect and bounded correction

Raw-ingress retention authority legitimately uses export mode:

```text
EncryptedRawVaultRetained
```

The production function `tagekyc.raw_export_next_assembly_candidate()` selected only:

```text
EncryptedExportPacket
```

Consequently a real retained-source job could be authorized and created but was never acquired by `DurableRawExportAssemblyWorkSource`.

The only product change is migration:

```text
20260925090000_RawExportAssemblyRetainedModeWorkSource
```

It changes the existing candidate predicate to accept exactly:

```text
EncryptedExportPacket
EncryptedRawVaultRetained
```

It adds no API, engine, worker, queue, schema field, SDK surface, or SignFlow dependency. `Down` restores the packet-only predicate.

## 3. Joined production-path proof

`Raw_ingress_publications_feed_the_same_job_durable_delivery_and_sdk_decode` exercises one continuous logical flow:

```text
challenge-bound verification session
  -> production raw-ingress HTTP endpoint
  -> production admission/broker/body/custody pipeline
  -> DG2 + selfie Available publications
  -> session completion + existing authorization/job APIs
  -> one SDK-created JobId
  -> production DurableRawExportAssemblyWorkSource acquires that exact JobId
  -> production assembly orchestration and C2 package preparation
  -> production durable result recording
  -> C4 listing + C3 authenticated package download
  -> standalone TagEkyc.RawExport.Client decrypt
  -> exact original DG2 and selfie bytes
```

The raw-ingress side uses the current test-only site qualification provider; this is a lab product-flow proof, not a real-site qualification claim. The control-plane TestServer uses production application/repository types with a test-owned DbContext lifetime because a second TestServer data-source lifetime would otherwise dispose the shared fixture source. The public HTTP and SDK behavior, production durable work-source, assembly provider, package store and PostgreSQL residues remain real.

Restored joined evidence:

```text
raw-bio-ingress-to-sdk-restored.trx
1/1 PASS, 0 skip
```

## 4. Discriminating mutation

The retained-mode arm was removed while leaving the packet-mode arm intact:

```text
i."ExportMode" IN ('EncryptedExportPacket','EncryptedRawVaultRetained')
  ->
i."ExportMode"='EncryptedExportPacket'
```

The same joined test failed:

```text
raw-bio-retained-mode-predicate-mutant.trx
0/1 PASS
System.TimeoutException:
The durable work source did not acquire the SDK-created job.
```

This failure occurs after the public SDK creates the retained-mode job and before assembly. It directly distinguishes the corrected production predicate from the predecessor behavior.

## 5. Migration and adjacent regression proof

`Retained_mode_work_source_down_and_reapply_preserve_exact_function_and_security_metadata` proves:

```text
current 25090000
  retained + packet predicates present
  owner = tagekyc_raw_export_deployer
  SECURITY DEFINER = true
  search_path = pg_catalog
  ACL present

Down to 24130000
  packet predicate remains
  retained predicate absent
  owner / SECURITY DEFINER / configuration / ACL unchanged

Reapply 25090000
  complete function definition and security metadata exactly restored
  EF history ends at 25090000
  HasPendingModelChanges = false
```

Focused retained results:

```text
raw-bio-retained-mode-migration-final.trx        1/1 PASS
raw-bio-existing-delivery-regression-final.trx  1/1 PASS
raw-bio-durable-work-source-regression.trx       1/1 PASS
raw-bio-current-migration-discovery-final.trx    1/1 PASS
```

The migration discovery and consent-retention latest-migration tripwires now name `20260925090000_RawExportAssemblyRetainedModeWorkSource`.

## 6. Boundaries and governance posture

```text
Production delta        one additive EF migration
SDK delta               0
SignFlow delta          0
New API/engine/class    0 product services; one migration class only
Site qualification     not claimed
Deployment/push         none
```

The current activation manifest covers the changed product/test bytes, so the revision-17 seal is expected to fail closed with current-byte drift. This technical packet does not re-open A3, change count/topology/site policy, or re-mint the seal. If the correction is independently accepted, governance may mint one bounded successor retaining:

```text
authority_open_row_count = 0
approved topology = DurableWorker
site qualification = required / v1
```

## 7. Requested independent disposition

```text
RAW ingress -> SDK joined delivery             PASS candidate
Retained-mode durable work-source correction   PASS candidate
Migration Down/Reapply security hygiene        PASS candidate
Existing packet-mode delivery regression       PASS candidate
No duplicate subsystem introduced              CONFIRM candidate
```
