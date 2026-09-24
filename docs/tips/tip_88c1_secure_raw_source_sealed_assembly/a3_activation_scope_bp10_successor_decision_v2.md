# A3 activation-scope successor — BP10 Homeowner ratification

Date: 2026-09-22. The Homeowner's “lam đi” follows the explicit request to ratify BP10 and perform a single 7-to-6 governance transaction. This successor records that instruction, not a new choice to defer or ratify any sibling row. The seven-row decision `a3_activation_scope_rebaseline_decision_v1.md` remains historical authority for the 38 deferred rows and the assembly topology.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 6
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: 11047F98A5AEA5FE201BFADBA00F09BA995A617CAE44EC6CD16715C16B5802A5
A3-Partition-SHA256: CEE12B433F38B9F0A4A5BFA21DF3C5135FDE3A240BAAA5D91C68FAC67D2A3EEF
A3-Ledger-SHA256: 4387881048567581EE24FB1556F881958AA8CE407913E870596B7C6FD9DB7B2D

BP10 alone is now `IMPLEMENTED / HOMEOWNER_RATIFIED` on the independently accepted `a3_activation_bp10_row_review_packet_v1.md`, SHA-256 `EC459284F2C5B494C86F21EAECF5A2E8AD93C4EF1728885508E5E2A734D78707`. The active partition has exactly P04, P05, `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, BP03, BP13 and `Agent raw HTTP Expect → durable server B/R1 before first body byte`: six rows in three mechanism families. Those six are still `NOT_IMPLEMENTED`; no ratification or proof is inherited from BP10.

The previous 38-row deferred backlog and its reasons remain byte-identical. `Activated includes assembly = NO` still means assembly topology `Disabled`; a different topology or change of deferred authority requires a new Homeowner scope decision. The canonical ownership registry records BP10's ratification and continues to bind all deferred authority citations. This successor does not waive the continuous-validity, fail-closed or source-drift rules of the seven-row decision.

The format-2 activation seal may be re-minted at count six only with the exact BP10 ratification record and current manifest. A nonzero count continues to block Activated with `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`. A3 remains HOLD. No production activation, full-suite claim, stage, commit, push or landing is authorized.
