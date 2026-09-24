# A3 continuous R2_R6 — BP18 stage/terminal-intent race review packet v1

Status: **builder candidate for bounded independent review**, not ratified A3 PASS. The route matrix now has 20 R2_R6 rows (BP19 was moved here from BROKER_CLAIM). This packet proposes only `BP18 StageCannotOvertakeTerminalIntent` for closure; all other R2_R6 rows stay open. Candidate census is 64 normative + 1 seam = **65 open**, subject to independent ratification. No full suite, stage, commit, push, landing or production activation is claimed.

## Exact source and evidence

- Test: `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs`, SHA-256 `6E77FFD520687C4B3AE037BEA9FAF9D9C167CB935133BEF5D11D13EF5F61D6E3`.
- A3 migration restored: `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs`, SHA-256 `91DD833A50E834145D34501F0C793D987A5A34C31CC011402D7DEAF7A485552A`.
- Product pipeline unchanged: `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs`, SHA-256 `5D9A2B7E161A700F61793A9E1E2F96562DB8FBF04EB2803B0A9989CE1F2F80AD`.
- BP18 baseline `a3-r2r6-ti-r3-final-baseline.trx`: SHA-256 `D777B76D1C7B35A81C3B126392F14DF5841068CC2F2713A021DBA10BE186AFE3`, 2/2.
- Isolated R3 intent-check mutant source: SHA-256 `EE8F4C0405A517EA678E8C21A9EE9ADDC98A9F7F75D724E22F87346CB1050F88`; it changes only `IF intent-present` to `IF FALSE AND intent-present` at migration line 2538. Rebuilt RED `a3-r2r6-ti-r3-final-guard-red.trx`: SHA-256 `A33808B13806F78A4E622B4EAA9CD818B88BA648C164701C66E4590A29DB953A`, 1 PASS / 1 FAIL. Only `intentFirst:true` fails at the real R3 call with `P0001: A3_R2_TERMINAL_INTENT_STAGE_FORBIDDEN`; the independent write guard prevents the wrong stage from committing. This RED demonstrates loss of the R3 typed `StateConflict` projection, not a committed unsafe stage.
- After byte-exact restoration and rebuild, joined BP18/TI01 sentinel `a3-r2r6-ti-r3-final-restored-joined-v2.trx`: SHA-256 `E5BECA0E20A12FA4EA3BCD57471D0A17E61FD798105325D3CBD6749806DCDCE4`, 5/5, zero skip. It contains both new two-order race cases and all three existing TI01 tuple/cause variants.
- Excluded diagnostic: first restored joined `a3-r2r6-ti-r3-final-restored-joined.trx`, SHA-256 `EECA94235DF240FC7693F259B694E9466687F28D1295CE92E2467BC3E82C5914`, 0/5 due to PostgreSQL connection bootstrap `NpgsqlException`/`EndOfStreamException` before test bodies. It is neither mutation RED nor restoration GREEN.

The test uses real R2 ciphertext creation, MinIO object write, production verification, TI01 SQL and R3 stage service. A real session-row lock queues two independent role connections; `pg_locks` confirms both waits before release. A separate observer reads committed attempt, head and publication state after each order. Synthetic source/key material and the lock owner establish safe preconditions and deterministic schedule; neither returns the decision under test. There is no public HTTP/Agent or full R2–R6 claim in this packet.

## Read-only reachability findings left open

- BP17: unresolved/incomplete PUT outcome can be projected as O21 by the request pipeline although the companion says O18. The complete-object O21 versus immediately recovered O22 timing also needs exact interpretation before a broad claim. No canned Final is substituted.
- BP19: an unknown verifier enum can fabricate O20, while the finalization repository accepts undefined numeric enum text. The latter file's cumulative A3 authorization is restricted to cleanup/reconcile-context corrections, so this packet does not edit its parser or request an implicit allowlist expansion.

Both are open product/authority items, not hidden under the BP18 result. Other R2_R6 rows and the independent Kestrel seam remain open. The reconciliation ledger contains the four-part reachability declaration, row-specific evidence, and excluded diagnostic. Candidate manifest and mutant sweep must pass on these exact bytes before ratification.
