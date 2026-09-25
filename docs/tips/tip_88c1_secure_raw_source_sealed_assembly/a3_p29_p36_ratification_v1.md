# P29-P36 Homeowner ratification record

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 0
A3-Reviewed-Manifest-SHA256: 7E5138787ED07D97343F47BE2A58ABF23C0CD7E233EAEA02C6E91211D29C0DAE

Decision source: the Homeowner's explicit instruction on 2026-09-24: “Tôi ratify P29–P36 tại a6d8405, gộp luôn hai điều nhỏ kia”.

The reviewed scope decision is `a3_p29_p36_ratification_scope_decision_v1.md`. This record ratifies exactly P29-P36 on the reviewed DurableWorker joined proof and includes the two narrow verifiability corrections requested by the Homeowner.

The successor activation-open count is zero and the approved assembly topology remains `DurableWorker`. Zero-open does not qualify a hospital site: site transport qualification policy version 1 remains mandatory and raw ingress remains fail-closed without a current matching PASS record.

This record does not ratify any deferred backlog row and does not authorize push, deployment, site qualification, or product/SDK source changes.

The PowerShell 5.1 technical-review verifier reads the immutable reviewed evidence snapshot at commit `c7b7eb9b1738b4134edcc4e34538aab025e43f0a`; it does not reinterpret the successor zero-row activation partition as the predecessor eight-row review partition.

The retained site-gate TRX exercises generated seal revision 14. Adding that TRX and the current PowerShell 5.1 transcript changes only evidence bytes and therefore produces the revision-15 evidence-only successor; it does not change runtime, product, SDK, partition, topology, count, or site-policy semantics.

The revision-16 evidence-only successor retains the focused eight-case site-qualification run: one positive qualification control and seven single-field negative variants for the existing transport-measurement guards. Each negative variant proves the live raw-ingress request gate returns the site-qualification invalid code before admission or body consumption. This successor changes no product, SDK, partition, topology, count, site-policy, or previously ratified semantics.
