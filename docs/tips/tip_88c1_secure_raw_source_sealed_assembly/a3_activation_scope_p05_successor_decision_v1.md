# A3 activation-scope successor — P05 Homeowner ratification

Date: 2026-09-22. The Homeowner's explicit “Tôi Ratify” follows the independent GPT and CC recommendation to ratify P05. This successor records P05 ratification only; it does not ratify P04 or the four TRANSPORT-EXPECT-FENCE rows. The seven-row re-baseline and BP10 successor remain historical authority for the 38 deferred rows and assembly topology.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 5
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: A6FF31517C3E927E94F678FC4233FAD8C81AACB6522BF85F7506D25817430475
A3-Partition-SHA256: 54D04EC2AE304BE3097712FDEE94C7D891B78F220CD651CF3CFAC686E04EEEAE
A3-Ledger-SHA256: 0206EE4C5B8013B872477638AD62B62977CD3ABC7B22EA017F75CAABB2A88EA0

P05 alone is now `IMPLEMENTED / HOMEOWNER_RATIFIED` on the independently accepted `a3_activation_p04_p05_gate_b_checkpoint_v1.md`, SHA-256 `B108F27AC37F7DED9D71C62F669AC438BDAAF1442B5448BFDBD2F79DBDEA125A`. The active partition retains exactly P04 and the four Expect-fence rows. All five remain `NOT_IMPLEMENTED`; none inherits P05 evidence. P04 is reclassified in the canonical ownership registry as `IMPLEMENTATION_REQUIRED` because the existing-R1 early-body path lacks a production owner for pre-start terminalization and attempt-fenced cleanup. That classification does not authorize a product change or close P04.

The 38-row deferred backlog remains byte-identical. Activated assembly inclusion remains NO with approved topology `Disabled`. Authority promotion, topology change or scope drift still requires a successor Homeowner decision under the original continuous-validity rules.

The format-2 activation seal may be re-minted at count five only with an exact P05 ratification record and current manifest. Nonzero count continues to block Activated with `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`. A3 remains HOLD. This decision does not authorize production activation, stage, commit, push or landing.
