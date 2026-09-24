# A3 retained-row execution checkpoint: P04, P05, Gate B

Status: P05 technical candidate; P04 open product gap; Gate B topology rule clarified. **No ratification, census change, seal re-mint, or production activation.** Current activation-open census is **6** (P04, P05, four Expect rows); A3 remains HOLD.

## P05 — joined stateful proof

`P05_ExistingEvaluatingShellSurvivesCapabilityLossAndSameLeaseRetry` seeds an Evaluating alias/claim and token through the production B SQL function. It then uses the public Agent → HTTPS/Kestrel → production admission/qualified-broker path with the *same* metadata and retained lease. A missing active commitment key yields exact HTTP 503/O05, no Server body read or body-pipeline call, and byte-identical alias/claim rows. After restoring the provider, the same pair yields O11 (the original Evaluating shell), not a new R1. The whole durable snapshot (including reservation/attempt/key/object rows), the lease's original retention budget, and its expiry remain unchanged after both responses. Separate predecessor component tests still cover active-key absence and provider loss inside B with rollback; this test supplies the previously missing stateful public join.

`P05_ExistingEvaluatingShellStrictTlsKeepsOriginalLease` repeats that stateful exchange through the **public production raw-transport constructor**, platform-trusted TLS and an isolated Linux CA/CRL runner; it does not inject a raw handler or alter trust policy. The runner removed its PostgreSQL network/container and ephemeral CA environment. The direct-path TRX has 1 executed / 1 passed / 0 skipped.

The current-source guard mutation changes only `QualifiedRawIngressBroker`'s O05 mapping from `CapabilityUnavailable` to `CapacityUnavailable`. The focused test fails 0/1 with expected `RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE`, actual `RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE`; the source is then restored byte-exact to SHA-256 `20663BB747D3A8F72E5BF77E1F42744625ABFE276A2E7B654F29CDD4EAA33E38`. The final restored joined gate is 10/10, zero skipped, including the P04 fresh-header control and both predecessor P05 provider cases. This is a request for technical P05 row review, **not** a Homeowner ratification event.

| Evidence | SHA-256 | Result |
| --- | --- | --- |
| Final test source `Tip88C1C6BA3BrokerAdmissionClusterTests.cs` | `E7280565E8011C3D423F9C15C6B936BD1C0288AE501A7D9A438ABF732BDAE3A7` | Current bytes |
| `a3-p05-final-guard-mutant.trx` | `1DDD464A73705A6808BBC043FA1BBA66CD6AE121C666FCD44497BC972F296A8F` | 0/1, `EVIDENCE_RED` |
| `a3-p04-p05-closure-restored-joined.trx` | `EB457AE11452E4144A4B793EB417C599E33D5B5F2E5D010DB65262AC5CED80EB` | 10/10, zero skipped |
| `a3-p05-closure-strict-tls-20260922151654-1.trx` | `ADF9E5D5E5DCD70EAED96F73B5B95C85C54EFB8C57DABDF2B1DFD16E3ADA3E04` | 1/1, zero skipped |

The earlier `a3-p05-existing-shell-guard-mutant.trx` and `a3-p05-current-guard-mutant.trx` are also intentional `EVIDENCE_RED` for the same mapping on predecessor test bytes. The bounded slice has **3 failed TRX, all classified**; only the final one is cited as current-test-byte mutation evidence. One initial zero-test filter, one missing test `using` compile error, and one cross-OS `project.assets.json` package-path error produced no TRX and are excluded diagnostics; the final Windows build/test and isolated-container test succeeded. A predecessor joined gate that included a deliberately skipped isolated-TLS test is superseded by the explicit zero-skip 10/10 Windows gate plus the separate 1/1 container gate.

## P04 — not closed by the fresh-header proof

The current endpoint rejects `Content-Encoding`/`Trailer` after authentication but **before** broker admission. That correctly proves the fresh P1–P3 O04/zero-body branch; it cannot prove the row's P4 branch, where R1 already exists and early body/proxy prebuffering must lead to exact `AdmissionProtocolRejected` / `TerminatedBeforeStart`, fenced cleanup and retained anti-reuse identity. There is no current application-level owner that observes a pre-admission body arrival and joins it to the exact committed reservation/attempt. The existing R2 terminal-intent SQL allowlist contains only size-limit, commitment-mismatch and recapture codes; the no-start cleanup function is granted to the reconciler role, not the broker. Simply moving the header check after broker or reusing the reconciler function would create an R1 for a fresh invalid header or act without an authorized exact fence. **P04 remains an implementation/authority gap, not a test-only gap.** No synthetic Final or unsafe terminalization was added.

The canonical ownership table still labels P04 `TEST_REQUIRED`, which predates this source-level product-gap finding. Before any next seal re-mint, the same governance transaction must reconcile that row to `IMPLEMENTATION_REQUIRED` with the exact missing P4 mechanism; do not silently leave the registry at `TEST_REQUIRED`. This checkpoint does not change the ownership table, its approved hash, or the current activation census.

## Gate B topology clarification

`a3_expect_gate_b_topology_successor_v1.md`, SHA-256 `CFF46284B6A65D4C0624760EAF3D31B4682AD07A858229757534497E4A74BC38`, supersedes the shorthand “no proxy” for future qualification. Direct TLS to Kestrel and genuinely transparent L4 passthrough are candidate topologies. An L7 TLS-terminating proxy is a separate candidate only if the **exact** proxy/configuration proves it relays Kestrel's `100` only after durable B/R1, never originates an early `100`, and prevents early application body buffering/retry or a second raw POST. The F3 early-`100` variant is a known disqualifying counterexample. This clarification neither attests a hospital route nor closes any of the four Expect RowIds.

The format-2 revision-4 activation seal remains at count 6. New evidence bytes correctly cause a current-manifest-drift build warning; no seal is re-minted absent a Homeowner row/scope governance transaction.
