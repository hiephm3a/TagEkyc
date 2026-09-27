# C125 revised three-entry correction — STOP packet v2

## Disposition

`HOLD — the three bounded stale hashes are corrected, but the corrected baseline exposed a separate rollback-atomicity failure.`

Per the dispatch stop-condition, no mutant, test correction, commit, push, deploy, governance change, or seal re-freeze was performed.

Baseline: `4ab6098500b6765eb59a4a9ce64cf4ce24a2b7e9`.

## Exact product diff

Only `CaptureCurrentGuard` in `20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` changed: three expected SHA-256 literals, `3 additions / 3 deletions`.

| Exact entry | Old | New |
|---|---|---|
| `complete_raw_export_source_ingress_claim(...)` | `4eefedc274d1d2c6152dd4100ed75f4ab4cdccdc997b04290da82bcc83a87b65` | `a4682fe2dd19a942e9bdde7a91b7a7a4347634881ea78c11425633691db59772` |
| `raw_export_begin_retained_source_ingress_with_authority(...)` | `f761acfe2123d34bb1bde39aee31eb302ac720b3117ff8dc2f845bc11ae089ba` | `dfdf8a282b89ab6614bdfe62593df6049c4567c72ba282e0430fd4d43db575aa` |
| `capture_runtime_read_bound_raw_ingress(...)` | `39bc73673df8801b8c09461636694140dc01d3108edf1c44530151cb053a7f10` | `7bf645cc66e8f69824e353310570fb34f76ab5aec44281db4acf8a5c213b78a2` |

Current migration working-tree SHA-256: `2BE79B830A9718C26EC7C29FFE1401A49BFD9952A7E7CBD4651D19C01E05ED0C`.

No SQL operation body changed. Before and after the patch, normalized full-literal hashes remain:

- `RetainedCompletionOperation`: 24427 bytes, `ab5de138ca4855b6b0bfee5b64bf3af8b23bcca508e4d5c61b828285c0581c90`.
- `RetainedBeginOperation`: 34838 bytes, `0a47bbcf0a2b44fd58546bc6fd7fad4146fd077bdfc46c15db8d3820a67e5b79`.
- `BoundRawIngressReader`: 10734 bytes, `fba121df45e1c142fab90ee0bf9ad7833a5772a73c4a6be3432720b6dc79a21e`.

## Source and database provenance

For each entry, three independently identified values agree: normalized A3 source body, corrected guard expected value, and the exact-boundary PostgreSQL body retained by v0.1.

| Source body | Bytes | SHA-256 |
|---|---:|---|
| `RetainedCompletionOperation` | 22602 | `a4682fe2dd19a942e9bdde7a91b7a7a4347634881ea78c11425633691db59772` |
| `RetainedBeginOperation` | 32255 | `dfdf8a282b89ab6614bdfe62593df6049c4567c72ba282e0430fd4d43db575aa` |
| `BoundRawIngressReader` | 9235 | `7bf645cc66e8f69824e353310570fb34f76ab5aec44281db4acf8a5c213b78a2` |

Normalization is exactly CRLF to LF, UTF-8, SHA-256; no trim and no bare-CR normalization.

The v0.1 diagnostic retained all 30 exact-boundary rows: 27 already matched and these three actual values now equal their corrected expected values. Therefore the retained observation plus the exact three-literal diff yields 30/30. A second instrumented 30-row diagnostic was not run after the corrected gate exposed the independent rollback failure; this limitation is explicit.

## Corrected baseline run

Artifact: `runs/corrected-baseline/c125-corrected-baseline.trx`.

SHA-256: `FE656397DB78AB6EBDBA0661DBEC6C19F564792C0A6562785365C5ACEDF7ABF5`.

Result: 7 executed, 4 passed, 3 failed, 0 skipped.

Passed:

- `C125_migration_model_is_clean_and_b4_down_contract_is_present`.
- `A3_CaptureLineageMigration_EmptyRoundTripRestoresPredecessor`.
- `A3_MigrationPopulationGuard_WaitsForWriterThenRejectsWithoutLoss(down: False)`.
- `A3_MigrationDiscovery_FromEmptyMatchesCurrentModelAndHistory`.

Failed:

- both `A3_CaptureLineageMigration_BodyDriftRejectsDownAtomically` cases (`" "` and `"\r"`);
- `A3_MigrationPopulationGuard_WaitsForWriterThenRejectsWithoutLoss(down: True)`.

All three failures occur after the intended guard rejection. The expected exception is observed, but the following database-body equality assertion fails: line 189 for both drift cases and line 108 for population Down. This is not classified as fixture drift. It is retained as `NEW_ROLLBACK_ATOMICITY_FINDING` pending review of migration-history and catalog transitions across the multi-migration Down request.

The two passed shared-database tests independently restore/assert the latest migration. Failed cases use disposable isolated databases which are destroyed by fixture disposal. No operational database was used.

## Mutations and final green

M1, M2, M3 and restored final were not run. The stop-condition fired before mutation credit was permitted. No mutation or closure claim is made.

## State and boundaries

- Product write-set: exactly one migration file, exactly three hash literals.
- Test/SDK/runtime/designer/snapshot/project/solution delta: zero.
- `docs/00_GDRIVE_FILE_INDEX.md` remains the user's pre-existing dirty file and was not touched.
- STOP packet v0.1 remains intact.
- No commit, push, deploy, governance update, or seal re-freeze.
- Post-seal recovery remains closed.
- C125 remains open.
- Layer 2 remains paused.
- Process kill/OS restart remains not proven.

## Next decision

Determine whether a multi-migration downgrade is required to be atomic back to the caller-visible pre-attempt catalog, or whether the existing tests incorrectly compare the latest catalog to the valid A3 boundary after newer Down migrations have committed. That decision must precede mutants and final closure; this packet does not choose or implement either interpretation.
