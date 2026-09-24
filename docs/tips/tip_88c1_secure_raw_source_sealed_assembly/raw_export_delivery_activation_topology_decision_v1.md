# Raw-export delivery ratification and assembly-topology successor

Decision source: the Homeowner's explicit instruction on 2026-09-24: “Tôi ratify raw-export delivery slice tại TagEkyc `97b35fa` và SignFlow `44a16b0`; cho phép chuyển assembly topology sang `DurableWorker` và thực hiện governance re-freeze riêng”.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 8
A3-Activated-Includes-Assembly: YES
A3-Assembly-Topology: DurableWorker
A3-Ownership-Table-SHA256: 54746345950D27A1BB716DDFDC4648963AE6559444339AF4F2F17D24972BB932
A3-Partition-SHA256: A3BEAD4A0CABC47DF24B5227EE8C1DAE1999E4EC287299959DF6F8A3C537DC2E
A3-Ledger-SHA256: 4269341453674733478D90019B55F11403A01F2CE30825FF9DA5D44B3250ABE1

## Exact decision

1. The raw-export delivery slice is ratified on TagEkyc commit `97b35fa161b615547c6b34071fac741175b80666` and SignFlow commit `44a16b0`.
2. Activated includes assembly and the approved topology is `DurableWorker`.
3. P29-P36 are not row-ratified by this decision. The prior delivery review packets explicitly excluded that inference. The topology transition removes their deferral basis, so all eight return to the activation partition as `NOT_IMPLEMENTED` under `ASSEMBLY-WORK-SOURCE`.
4. The thirty `CANDIDATE_ONLY` rows remain deferred. The four Expect rows remain product-complete and subject to site qualification policy version 1. Previously ratified rows remain closed.
5. The successor seal must bind count eight and `DurableWorker`. Therefore Activated remains blocked by `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE` until P29-P36 receive exact row proof and Homeowner ratification.

This decision does not authorize production activation, deployment to a hospital, staging, pushing, or silent closure of an assembly outcome row.
