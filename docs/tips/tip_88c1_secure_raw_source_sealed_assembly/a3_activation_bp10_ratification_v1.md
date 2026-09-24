# A3 BP10 — exact Homeowner ratification

Date: 2026-09-22. Decision source: the Homeowner's explicit “lam đi” immediately after the request to ratify BP10 and perform the single 7-to-6 activation-governance transaction. This record ratifies BP10 only. It does not ratify P04, P05 or any of the four TRANSPORT-EXPECT-FENCE rows and does not authorize production activation.

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 6
A3-Reviewed-Manifest-SHA256: FEFBC46610BDD190A367F92606CBA502ABC04BB2F384E72692D6A4D51E311E85

Reviewed BP10 packet: `a3_activation_bp10_row_review_packet_v1.md`, SHA-256 `EC459284F2C5B494C86F21EAECF5A2E8AD93C4EF1728885508E5E2A734D78707`. Its six-path bounded-window proof, all-six RED lease-disposal mutation, 15/15 restored joined gate and 107/107 current-byte Server-framing ratified sentinel were independently accepted by CC and GPT. No test or product source is modified by this governance transaction.

Scope successor: `a3_activation_scope_bp10_successor_decision_v2.md`, SHA-256 `D654967348A89B965414DFD9B2E68BF632CB7E3CBA9944571A79DD0D58E5A0EB`. The reviewed manifest pins current partition SHA-256 `CEE12B433F38B9F0A4A5BFA21DF3C5135FDE3A240BAAA5D91C68FAC67D2A3EEF`, reconciliation ledger SHA-256 `4387881048567581EE24FB1556F881958AA8CE407913E870596B7C6FD9DB7B2D`, and canonical ownership registry SHA-256 `11047F98A5AEA5FE201BFADBA00F09BA995A617CAE44EC6CD16715C16B5802A5`. It inventories 2,625 files and 1,221 TRX, reports 106 distinct recorded mutants with zero live matches, and excludes only itself, its sidecar, this record and the seal descriptor from the hash cycle.

The predecessor 46-row wave partition, seven-row scope decision and 38-row deferred backlog remain historical and unchanged. Current activation-open census is six: P04, P05 and four Expect-fence rows. The approved assembly topology remains `Disabled`. A format-2 revision-4 seal at count six must still block Activated as `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`. A3 remains HOLD; this record makes no full-suite PASS, stage, commit, push, landing or production-activation claim.
