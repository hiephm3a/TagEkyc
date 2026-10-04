# Independent review prompt — SMID50 Reader Provider V0.4

Review the exact V0.4 candidate and evidence. V0.3 received CC PASS and GPTW HOLD for one product defect and one evidence weakness; this is the bounded successor.

Check these points independently:

1. Caller `ReasonCodes` cannot persist or influence assurance through reserved durable namespaces (`PACE_SUCCESS`, `BAC_SUCCESS`, `SOD_DECLARED_DG_*`, `SOD_SUPPLIED_DG_*`).
2. Durable access/DG tokens are re-derived only from validated decision-basis flags; ordinary non-reserved reason codes are not broadly broken.
3. The target mutation genuinely makes both spoof-persistence cases and the BAC-to-High case RED, and inverse restore is byte-exact.
4. Recursive physical audit enumeration no longer uses silent traversal failure; every traversal error is counted and makes qualification fail.
5. The physical result binds the full 34-file candidate manifest and reports `EnumerationErrorCount=0` without retaining subject/card payload.
6. Focused Agent and Server gates, build, census, manifest, and ZIP are internally consistent.

Boundaries: no commit, re-freeze, push, PACE claim, schema/migration, or hospital evidence. Return PASS or specific actionable findings.
