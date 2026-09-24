# P29-P36 Homeowner ratification record

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 0
A3-Reviewed-Manifest-SHA256: D2D26E1B06FFD3D902F1DE1EC2740B335B70C7B38573967779E94F2E81D563E4

Decision source: the Homeowner's explicit instruction on 2026-09-24: “Tôi ratify P29–P36 tại a6d8405, gộp luôn hai điều nhỏ kia”.

The reviewed scope decision is `a3_p29_p36_ratification_scope_decision_v1.md`. This record ratifies exactly P29-P36 on the reviewed DurableWorker joined proof and includes the two narrow verifiability corrections requested by the Homeowner.

The successor activation-open count is zero and the approved assembly topology remains `DurableWorker`. Zero-open does not qualify a hospital site: site transport qualification policy version 1 remains mandatory and raw ingress remains fail-closed without a current matching PASS record.

This record does not ratify any deferred backlog row and does not authorize push, deployment, site qualification, or product/SDK source changes.

The PowerShell 5.1 technical-review verifier reads the immutable reviewed evidence snapshot at commit `c7b7eb9b1738b4134edcc4e34538aab025e43f0a`; it does not reinterpret the successor zero-row activation partition as the predecessor eight-row review partition.

The retained site-gate TRX exercises generated seal revision 14. Adding that TRX and the current PowerShell 5.1 transcript changes only evidence bytes and therefore produces the revision-15 evidence-only successor; it does not change runtime, product, SDK, partition, topology, count, or site-policy semantics.
