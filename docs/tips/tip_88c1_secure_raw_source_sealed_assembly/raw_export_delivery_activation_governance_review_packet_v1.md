# Raw-export delivery ratification and DurableWorker governance re-freeze

Date: 2026-09-24

## Authority and bounded meaning

The Homeowner ratified the raw-export delivery slice at TagEkyc `97b35fa` and SignFlow `44a16b0`, authorized the assembly topology transition to `DurableWorker`, and authorized a separate governance re-freeze.

This transaction does not reinterpret the delivery technical PASS as P29-P36 row ratification. The accepted delivery packets expressly excluded that claim. Instead, enabling assembly removes `ASSEMBLY_NOT_ACTIVATION_PREREQUISITE`: the eight assembly rows return from the deferred backlog to the active partition.

## Successor state

```text
approved assembly topology       DurableWorker
previous activation-open rows    0
reactivated assembly rows        8 (P29-P36)
deferred candidate-only rows     30
site qualification policy        required / version 1
A3                                HOLD
Activated                         blocked: CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE
```

The ownership registry now points P29-P36 to the production `DurableRawExportAssemblyWorkSource`, marks that producer reachable under the approved topology, and keeps each exact row `TEST_REQUIRED / ROW_PROOF_AND_HOMEOWNER_RATIFICATION`.

## Verification

- Partition/backlog verification proves `46 = 1 reconciled + 3 ratified successors + 4 site-qualified + 30 deferred + 8 active`, with zero duplicate, missing, or extra rows.
- Generator self-test passes 7/7, including the `YES ⇔ DurableWorker` consistency rule.
- `raw-export-delivery-activation-generated-seal-and-worker.trx` is 2/2 PASS, SHA-256 `F748AFC49A469E70B2CD9914A866EC23CC5A388B6A4E40DD98755096E40EF06B`:
  - generated current seal uses the approved `DurableWorker` topology and nonzero count, then fails Activated before routes with `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE` rather than `INVALID`;
  - the production DurableWorker registration still exposes a real `IRawExportAssemblyWorkSource` caller.
- Product and SDK bytes are unchanged from the ratified delivery commits. The only test-source correction updates the prior generated-seal host assertion from zero-open startup to the new nonzero assembly-gate behavior.

The exact current manifest, ratification record, scope decision, partition, ledger, ownership SHA values and final evidence revision are machine-bound in `a3_activation_evidence_seal_v1.tsv`.

No push or production activation is part of this transaction.
