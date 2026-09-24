# A3 macro-wave W5 — retention/custody cluster review packet v1

**Status:** CANDIDATE — independent review required. **Scope:** all 12 W5 rows, one handoff. **Authority-open census:** 46 = 45 normative + 1 seam, unchanged. **A3:** HOLD. No full suite, stage, commit, push, landing or production activation.

## 1. Disposition of all twelve rows

The exact 12/12 dispositions and named exit gaps are in `a3_macro_wave_w5_final_matrix_v1.tsv`, SHA-256 `DD5953773745F25554C461E7AF17822702342D0C8658052C143FBBB3D22BB04C`. The only **new** candidate is `A3_RetentionCheckpoint_LatestReferenceNotOldEffective`. `E01_Race_IssueWithdrawal` remains its pre-existing `CANDIDATE_PENDING_RATIFICATION`, awaiting an explicit Homeowner decision; W5 does not represent that as a new proof or a ratification. The other ten W5 rows stay OPEN at their named gaps. In particular, the permit-revision and C3 reader-zero additions below are bounded component evidence, not closures.

The 12-row prespecified mechanism matrix is `a3_macro_wave_w5_execution_matrix_v1.tsv`, SHA-256 `6EB0B2B966E9A8FDD5C5C5EE1E199F4075E90865B42E7D35E5F6757BF35B3D2C`. Execution was grouped by retention-lineage mechanism, not by one build/test cycle per RowId. No production source has a lasting W5 change.

## 2. Latest-reference candidate — current-byte RED and restoration

`E01_RevisionInvalidation_UpdatedReferenceRejectsActualR1R3R4R5` now asserts that revision 1's event remains time-effective after the reference head updates. Its four cases then traverse production R1, R3, R4 and R5 respectively; each denies fresh use on the *head* mismatch. The shared W5 baseline (permit-revision control, four checkpoints, two export controls) was **7/7 PASS**; TRX SHA-256 `EAADA4BD6A5BE67475ADBBE1696CD1880B1687A710B761ABBCF2D7975ED55544`.

`W5-G-LATEST-REFERENCE-HEAD` removed only the production current-reference-versus-binding revision equality in `20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs`. Mutant source SHA-256 `EE8D4DBE3EA4838D685C0F9E315519870DA0BCAEBF2B8599BD158FC20340E0C6`; `a3-w5-lineage-latest-head-mutation.trx`, SHA-256 `BC05C4BDB02DC587DB5D4D219DE1EA0336F581274319354D04C1F116019B4006`, was **3 PASS / 4 FAIL**. Only the four named R1/R3/R4/R5 cases failed: expected `SOURCE_RETENTION_NOT_AUTHORIZED` but R1 returned `NewReservation`, and the stage checkpoints advanced to `Staged`, `Committed` or `Available`. The three adjacent controls stayed green. The migration was restored byte-exact to SHA-256 `F9CE348596EBCC4C79F64675A427AFB179BE5C31D9567C44580AB709ECB313F9` before rebuild. Historical ratified C1/C3 stale-reference coverage is not silently substituted for these four new assertions.

This requests **independent technical review of one candidate**, not Homeowner ratification. If accepted, the row stays authority-open until the explicit grouped decision; no census or seal decrement occurs now.

## 3. Bounded C3 and permit-revision evidence — rows remain OPEN

The C3 test uses a real retained-source export package and first starts a positive delivery. After the underlying reference is withdrawn, it invokes production `RecipientPackageDeliveryCoordinator.PrepareContentAsync` with a counting provider reader. The result is exact `Ineligible`, `OpenCount=0`, and only the pre-existing `Authorized` delivery event remains—no `StreamingStarted` append.

The first narrow inner `raw_export_retained_snapshot_is_current` mutation was **7/7 GREEN**, TRX SHA-256 `71C4A28B9D815B5F625F9F278D7712C3985AD1E849C4FB7616BD1327FC83D75F`; it is an **excluded non-discriminating diagnostic**, not RED evidence. The broader production C3 begin-stream current-authority guard mutation, source SHA-256 `926C72905F8309CBEFE9761AAF63FDAE941F97278FBC05DA0F4C76D07CD71D0F`, produced **6 PASS / 1 FAIL** in TRX SHA-256 `3AD8716061DC541FDEC95F0BC33DADE050EA80237D23AEB59E6B438BEAF98884`: only the withdrawn C3 case changed from expected `RAW_EXPORT_PACKAGE_DELIVERY_INELIGIBLE` to `RAW_EXPORT_PACKAGE_DELIVERY_UNAVAILABLE`. That migration was restored byte-exact to SHA-256 `3D4D0106E5CBD568CF3053CA1BFBB529B55070CE975D9561E9F466F9D863F7CF`. Because the *narrow retained branch* has not been shown load-bearing, `A3_ExportWithdrawal_BlocksBeforeProviderRead` remains OPEN.

The new `A3_RetentionSnapshot_ExactPermitRevision` control rejects revision 2 and `NULL` through the real PostgreSQL append, accepts exact revision 1, checks zero failed-append residue and verifies the installed `bigint` revision column. A source-wide producer search found only `raw_source_issue_retention_authority`, which always inserts permit revision 1. There is no production revision-2 permit to use as the otherwise-valid counterexample for a narrow exact-revision mutation. This is a **reachability/authority gap**, not merely an unwritten test: either versioned permit issuance must be authorized and built, or the row's exact-revision proof must be scoped to the current single-revision product. W5 makes neither decision. Both test additions are in current source SHA-256 `7CC506599A2A4DD1EFEB3EFCC5F3845A259D96B7D066BC2334569808E99411A8`.

## 4. Restored joined gate and cost boundary

After both production migrations returned to predecessor bytes, one rebuilt W5 affected-family gate `a3-w5-lineage-restored-joined.trx`, SHA-256 `955A7E1D8DAB94B267404FF9C6A61C39732DF9B285C1131A0DCBC69E53D90CDA`, passed **15/15, zero skip**. It includes the revision-1 permit control, four updated-head checkpoints, two export branches, both actual C3/B2 session-lock orders and six retained-stage cross-border-clock controls. This is an affected-family joined gate, **not** a claim that the absent continuation, migration-checkout, public-precedence or completion scenarios were executed. W5 did not rerun the expensive process-kill matrix or a full suite; no product source affected by those frozen proofs changed.

## 5. Raw machine accounting and freeze

```text
official_open_rows=46
partition_rows=46 assigned_once=46 duplicate=0 unassigned=0 extra=0
wave_W5_RETENTION_CUSTODY=12
scratch_runs=45 (W5-000 through W5-005 are W5 entries)
manifest_files=2486
manifest_sha=8738A557DCDF420FBDC2C5E0AC54FA6246C832286C34A10CF33214CBC3CC65AF
w5_trx_in_manifest=5/5
outside_name_family_failed=0
w5_trx_inventory=5/5
observed_delta=19 allowed_delta=19 outside_allowlist=0
w5_failed_runs=2 classified_failed_runs=2 unclassified_failed_runs=0
mutants=105 live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

The whole-repo W5 verifier (`a3_macro_wave_w5_manifest_verify.ps1`) hashes git-visible files plus every retained TRX under both repositories, not merely `tests/`. The exact five-run inventory includes the 7/7 green-under-mutation diagnostic; the two failed TRX have distinct `EVIDENCE_RED` rows, source SHA, first assertion and guard in `a3_macro_wave_w5_failed_run_census_v1.tsv`. Registry includes all three W5 mutant source hashes, including the non-discriminating diagnostic. The sole pre-test build error was missing namespace import in the new test, recorded as `W5-000` in scratch, fixed before any TRX and not represented as a business RED. Candidate manifest and sidecar are self-excluded; the predecessor W4 manifest/sidecar/packet are ordinary W5 inventory entries. Packet v1 is self-excluded to avoid a self-hash cycle.

**Requested disposition:** accept the bounded W5 evidence and review the one latest-reference candidate; keep all other W5 row dispositions as recorded, with no A3 PASS or production enablement claim.
