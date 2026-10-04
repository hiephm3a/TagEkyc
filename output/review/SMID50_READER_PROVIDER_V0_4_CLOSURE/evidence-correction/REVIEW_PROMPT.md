# Review prompt — V0.4 evidence correction

Review only the remaining GPTW HOLD and the two non-blocking CC LOW items.

Verify:

1. Every per-file hash/read failure calls `Add-EnumerationError` with phase `before-file-hash` or `after-file-hash`.
2. Any nonzero `EnumerationErrorCount`, `UnreadableLogCountBefore`, or `UnreadableLogCountAfter` makes the qualification fail.
3. Empty harness output also fails explicitly.
4. The new physical result has all four relevant error/unreadable counts at zero and binds candidate manifest `750CEBDA...E529DFB`.
5. No Agent/Server product bytes changed; prior V0.4 product evidence remains bound by outer ZIP SHA `058580EE...A0F8`.

Return PASS or one precise actionable finding. COMMIT/RE-FREEZE/PUSH remain NO.
