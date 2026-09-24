# A3 macro-wave W3 — grouped Homeowner ratification v1

**Date:** 2026-09-21  
**Decision source:** Homeowner's explicit instruction in the task conversation: “RATIFY P08 + P14 TOGETHER.”  
**Reviewed successor packet:** `a3_macro_wave_w3_review_packet_v2.md`  
**Reviewed successor packet SHA-256:** `C6FDDCD8023FEA53B2398E1B67BA0271C228383CF8E8CFFC06185978D1D519B5`  
**Candidate manifest:** `a3_macro_wave_w3_ratification_candidate_manifest_v1.tsv`

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 46
A3-Reviewed-Manifest-SHA256: 2F96BA6A77B0538748F6CC3A4F9D2C377B04C0C26A31A7276C806D64A439AA3F

The Homeowner ratifies these two rows **together**, and no others:

- P08/O08 `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`;
- P14/O14 `RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE`.

Independent review accepted the exact P08 same-denied-lease retry and the retained P14 locked-selector/recovery proof. The candidate manifest pins the updated reconciliation ledger SHA-256 `AB609D04B5871662BB9D642A582F2E26CE30312EC02110A4F723B4C3CDF7D78B` and open partition SHA-256 `586C8746A95AC978C1BD050C24D4ACBAD28CE63A083B8488045CC7E65699E513`. The partition verifier reports 46 authority-open rows, each assigned once, with zero duplicate, unassigned or extra rows.

This record authorizes exactly one Activation Evidence Seal re-mint to revision 2 against the above manifest, partition, ledger and this record's own SHA-256. The manifest covers every Git-visible file and every retained TRX in both repositories, including all 14 W3 TRX. To break the hash cycle, it excludes exactly four derived governance outputs: itself, the manifest file and sidecar, and the seal descriptor. `a3_macro_wave_w3_ratification_manifest_verify.ps1` enforces that explicit set and reports zero unmatched live files. Build must check the exact record fields and the manifest's partition/ledger identities. A3 remains HOLD and no production activation, full suite, Wave 4, stage, commit or push is authorized.
