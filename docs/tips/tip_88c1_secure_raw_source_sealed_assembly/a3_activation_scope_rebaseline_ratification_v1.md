# A3 activation-scope re-baseline — grouped Homeowner ratification v1

Date: 2026-09-22. Decision source: the Homeowner's explicit “duyệt làm đi” following the seven-row/four-mechanism proposal and narrow text correction in the task conversation. This ratifies the **activation-scope decision only**. It does not ratify any of the seven retained rows, prove A3 readiness, or authorize production activation.

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 7
A3-Reviewed-Manifest-SHA256: 0E9328634A5476301F9C9F82AB408255D13D945B25753F9475CFA56CCDDE4521

Exact scope decision: `a3_activation_scope_rebaseline_decision_v1.md`, SHA-256 `7E9ACCC89634E1CACB6A76FF3FAF56C2BAF26EBE656BBB2CF5C7C970C7923766`.

Reviewed grouped packet: `a3_activation_scope_rebaseline_review_packet_v1.md`, SHA-256 `C2ACE2B6E9F9FC853E2F719FDFB97BFBC6D390B758AAE05BEC8177FE88DD7C28`.

The manifest pins ownership registry SHA-256 `56EC01946103E69FD4FB0F622B9FDD49BA3A4B87A78EF5BC383A0A2B0CCFC07E`, retained partition SHA-256 `B7AC9BB68204FA69F37FB4F16BBFFD305C3437CC6E5BAA136ADFDCB6777C91E3`, reconciliation ledger SHA-256 `5CA3C1ABAFDC4CD8F1AA3BF66A8D091EDEDA1940B5A93D8EA04E2C`, and all current-byte source/evidence listed by the manifest verifier. It contains 2,509 files and 1,131 TRX, including all nine bounded topology-guard TRX. It excludes only four derived outputs to break the hash cycle: itself, its sidecar, this ratification record, and the activation seal descriptor.

The approved transaction reconciles the prior E01 ratification, defers eight assembly and thirty candidate-only rows without calling them implemented, and retains exactly seven activation-open rows in four mechanism families. The approved topology is `Disabled` for this delivery. A new authority decision absent from the canonical ownership registry cannot silently change this scope; a registry change, cited authority-source change, topology change, or bound-byte change requires a successor Homeowner re-baseline before seal re-mint. The format-2 activation seal may now be minted at count seven, which must still block Activated as `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`.

A3 remains HOLD. No full-suite PASS, stage, commit, push, landing or production activation follows from this record.
