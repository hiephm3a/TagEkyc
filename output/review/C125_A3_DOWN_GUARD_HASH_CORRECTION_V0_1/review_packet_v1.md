# C125 A3 Down-guard hash correction — STOP packet v1

## Disposition

`HOLD — bounded one-literal correction was not applied.`

The required whole-guard comparison found three mismatches at the exact A3 Down boundary, not one. The dispatch explicitly requires a stop and separate report when any additional mismatch appears. No product, test, SDK, governance, or seal byte remains changed by this run.

Baseline commit: `4ab6098500b6765eb59a4a9ce64cf4ce24a2b7e9`.

## Repository gate

- Migration before and after diagnostics: SHA-256 `F9CE348596EBCC4C79F64675A427AFB179BE5C31D9567C44580AB709ECB313F9` over working-tree bytes.
- The temporary diagnostic was completely removed. `git diff --exit-code` for the migration passes.
- Pre-existing `docs/00_GDRIVE_FILE_INDEX.md` modification was not touched.
- No commit, push, deploy, or seal re-freeze was performed.

## Baseline defect

`C125_migration_model_is_clean_and_b4_down_contract_is_present` executed once and failed once, with zero skips, at `A3_CAPTURE_CURRENT_BODY_MISMATCH`.

Retained artifact:

- `runs/baseline/c125-baseline.trx`
- SHA-256 `D99F76FC21AE778AA246FB1468F775C37C98B51D26055F6B03870D8FBD6E8F86`

## Exact-boundary guard comparison

The diagnostic ran the unmodified C125 route down to the A3 migration and read each live `pg_proc.prosrc` at boundary migration `20260913120000_Tip88C1C6BA3RetainedIngressComposition`. It used the production normalization rule exactly: replace CRLF with LF, encode UTF-8, SHA-256. It did not trim whitespace or normalize bare CR.

Result: 30 entries observed; 27 matched; 3 mismatched.

| Signature | Expected | Actual |
|---|---|---|
| `complete_raw_export_source_ingress_claim(...)` | `4eefedc274d1d2c6152dd4100ed75f4ab4cdccdc997b04290da82bcc83a87b65` | `a4682fe2dd19a942e9bdde7a91b7a7a4347634881ea78c11425633691db59772` |
| `raw_export_begin_retained_source_ingress_with_authority(...)` | `f761acfe2123d34bb1bde39aee31eb302ac720b3117ff8dc2f845bc11ae089ba` | `dfdf8a282b89ab6614bdfe62593df6049c4567c72ba282e0430fd4d43db575aa` |
| `capture_runtime_read_bound_raw_ingress(...)` | `39bc73673df8801b8c09461636694140dc01d3108edf1c44530151cb053a7f10` | `7bf645cc66e8f69824e353310570fb34f76ab5aec44281db4acf8a5c213b78a2` |

The complete 30-row comparison is in `guard_comparison_v1.tsv`. The diagnostic TRX is `runs/guard-comparison/c125-guard-comparison.trx`, SHA-256 `20BE838D9F4377F38C6DF168218FF6560A326989B208722166B6576981D8F9E4`.

## Why this packet stops

Changing only `4eef…` to `a468…` would merely expose the next stale expected hash and C125 would remain red. Expanding the product patch to three hashes without independently correlating the two additional live bodies to their authoritative source literals would violate the bounded dispatch. Therefore:

- no one-literal correction was applied;
- no mutation/restored-green sequence was run;
- no positive migration gate was claimed;
- C125 remains `PRODUCT_DEFECT_CONFIRMED / OPEN`;
- post-seal recovery remains closed;
- Layer 2 remains paused;
- process kill/restart remains not proven.

## Required next decision

Diagnose the two additional mismatches by correlating each exact A3-boundary body with its authoritative migration literal and lineage. If they are the same bounded stale-hash class, issue a revised three-entry correction dispatch with per-entry mutation discrimination. If either reflects a broader migration-order or body-ownership problem, keep C125 open and correct that contract instead.
