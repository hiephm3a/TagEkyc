# C125 V0.3 — corrected migration boundaries and complete proof

## Disposition

`TECHNICAL PASS — C125 THREE-ENTRY DOWN-GUARD CORRECTION IS READY FOR TWO-REVIEW CLOSURE.`

This packet stops before commit, push, deployment, governance change, or seal re-freeze. C125 is not declared closed by this packet alone. Post-seal recovery remains closed, Layer 2 measurement ownership remains paused, and process kill/OS restart remains not proven.

Baseline HEAD: `4ab6098500b6765eb59a4a9ce64cf4ce24a2b7e9`.

## Exact write-set

Product: one migration file and exactly three expected SHA-256 literals (`3 additions / 3 deletions`):

| Guard entry | Stale expected | Corrected expected |
|---|---|---|
| `complete_raw_export_source_ingress_claim(...)` | `4eefedc274d1d2c6152dd4100ed75f4ab4cdccdc997b04290da82bcc83a87b65` | `a4682fe2dd19a942e9bdde7a91b7a7a4347634881ea78c11425633691db59772` |
| `raw_export_begin_retained_source_ingress_with_authority(...)` | `f761acfe2123d34bb1bde39aee31eb302ac720b3117ff8dc2f845bc11ae089ba` | `dfdf8a282b89ab6614bdfe62593df6049c4567c72ba282e0430fd4d43db575aa` |
| `capture_runtime_read_bound_raw_ingress(...)` | `39bc73673df8801b8c09461636694140dc01d3108edf1c44530151cb053a7f10` | `7bf645cc66e8f69824e353310570fb34f76ab5aec44281db4acf8a5c213b78a2` |

Corrected migration working-tree SHA-256: `2BE79B830A9718C26EC7C29FFE1401A49BFD9952A7E7CBD4651D19C01E05ED0C`.

No SQL operation body, guard logic, signature, ordering, exception code, migration transaction behavior, designer, snapshot, runtime, SDK, project, or solution file changed.

Test-only: `Tip88C1C6BA3MigrationTests.cs` (`162 additions / 2 deletions`) corrects the exact migration checkpoint and adds read-only history/catalog diagnostics. C125 and the shared `Prepare()` helper are unchanged.

## Negative cases and conclusion boundary

The V0.2 equality failures are reclassified as `SUPERSEDED_CHECKPOINT_DEFECT`, not as a product rollback-atomicity defect. Those tests captured a latest-state baseline and then asked EF to migrate across five newer Down migrations before A3 rejected. V0.3 instead starts the body-drift and population-Down proofs at exact A3.

At exact A3:

- SPACE and bare-CR mutations each change only R20;
- the 30-entry catalog reports exactly the R20 mismatch before the attempt;
- Down rejects with `A3_CAPTURE_CURRENT_BODY_MISMATCH`;
- all compared function bodies, applied history, guard catalog and relevant metadata remain unchanged after rejection;
- the injected drift remains present;
- population Down waits for the independent writer, preserves its committed rows, rejects with `A3_RETENTION_DOWN_POPULATED`, and leaves A3 bodies/history/catalog unchanged.

This proves atomic rejection at the exact A3 boundary. It does **not** claim that a latest-to-predecessor sequence is globally atomic.

## Latest-to-predecessor partial progress

An independent control database is migrated through the legitimate chain to exact A3 and receives the same R20 drift. The latest database receives the same drift and is asked to migrate to the A3 predecessor.

Observed result:

- five migrations newer than A3 complete their Down operations;
- A3 remains the last applied migration after its guard rejects;
- all 24 body observations and all 30 guard-catalog records equal the independent exact-A3 control;
- the R20 drift remains present;
- exactly three function bodies differ between latest and exact A3;
- all three are owned by `20260924130000_RawExportLegacyConsentClassFence.Down()` via `RewriteConsentLookup(false)`;
- owner, `SECURITY DEFINER`, configuration and ACL remain unchanged for those three functions;
- no unexplained body, history, or catalog delta was observed.

The exact five migration transitions and three body deltas are retained in `latest_to_a3_partial_progress_v3.tsv` and in the final TRX output.

## Direct 30/30 guard proof

`A3_CaptureCurrentGuard_ExactBoundaryAllEntriesMatch` migrated a disposable database to exact A3 and emitted all 30 signature-specific rows from the production `CaptureCurrentGuard`. Each row contains the exact signature, expected hash and live normalized `pg_proc.prosrc` hash.

Normalization is exactly `SHA256(UTF8(prosrc.Replace(CRLF, LF)))`: no trim, no bare-CR rewrite, and no overload collapse.

Result: `30 / 30 MATCH`. The direct table in `guard_comparison_direct_v3.tsv` is generated from `c125-restored-final-v3.trx`, not copied or patched from V0.1.

Final run identity: `restored-final-v3`, 2026-09-27 08:32:16–08:33:05 +07:00, disposable PostgreSQL databases, no secrets in captured output.

## Mutations and restored gate

| Run | Result | Discrimination |
|---|---:|---|
| M1 stale completion hash | `0/2`, zero skip | exact completion entry mismatch + positive Down `A3_CAPTURE_CURRENT_BODY_MISMATCH` |
| M2 stale retained-begin hash | `0/2`, zero skip | exact retained-begin entry mismatch + positive Down `A3_CAPTURE_CURRENT_BODY_MISMATCH` |
| M3 stale bound-reader hash | `0/2`, zero skip | exact bound-reader entry mismatch + positive Down `A3_CAPTURE_CURRENT_BODY_MISMATCH` |
| restored final v3 | `9/9`, zero skip | C125, positive round-trip, SPACE, CR, population Up/Down, migration discovery, 30/30 guard proof, latest-to-A3 boundary proof |

Each mutant changed one expected hash only, was built from its own source, and was restored before the next run. The restored migration hash is the corrected hash stated above.

`restored-final` and `restored-final-v2` are retained superseded green runs. `restored-final-v3` is the authoritative final run because the test source was subsequently strengthened to assert and emit the exact five migration removals and three catalog deltas.

## Failed-run accounting

Seven failed runs are retained and classified: two V0.1 runs, one V0.2 run, the V0.3 helper-defect run, and three discriminating mutants. They contain 12 failed test executions in total. There are zero unclassified failed runs and zero skipped/not-executed tests in all cited V0.3 gates.

No historical PASS is reinterpreted as a false positive without source/run provenance.

## Database and repository state

All boundary and mutation cases use fixture-owned disposable PostgreSQL databases. The focused final gate includes C125 and migration discovery, whose fixture cleanup/restore assertions complete successfully. No operational database or hospital information is used.

The manifest hashes working-tree bytes because this review packet intentionally precedes commit. Its `hash_basis` column states `WORKING_TREE_BYTES@HEAD_4ab6098500b6765eb59a4a9ce64cf4ce24a2b7e9`. The manifest and its SHA sidecar exclude themselves; packet and packet sidecar handling is stated in the manifest note.

`docs/00_GDRIVE_FILE_INDEX.md` remains the user's pre-existing dirty file and was not touched. No commit, push, deploy, governance update, or seal re-freeze was performed.

## Review decision requested

Review the same working-tree bytes for closure of C125. Only after two reviews PASS may the three-literal correction and its test/evidence package be committed. Evidence-only re-freeze remains a separate later transaction.
