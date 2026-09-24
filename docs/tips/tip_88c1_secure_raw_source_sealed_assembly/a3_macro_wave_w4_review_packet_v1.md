# A3 macro-wave W4 internal/no-egress — final cluster review packet v1

Status: **submitted for independent review; no row ratification requested**. This
is one packet for P24–P28, not five row handoffs. Incoming and outgoing official
authority-open census: **46 = 45 normative + 1 seam**. A3 remains **HOLD**.

## 1. Scope and disposition

The five rows and their fixed exit conditions were declared before execution in
`a3_macro_wave_w4_execution_matrix_v1.tsv`. The final disposition is recorded
once in `a3_macro_wave_w4_final_matrix_v1.tsv`: P24 remains an exact joined
Activated/residue proof gap; P25 remains an owning-side/product-and-proof gap;
P26 is a named product gap; P27 and P28 are named product/authority gaps. No
W4 row is marked IMPLEMENTED. The already-ratified All14 no-egress aggregate
remains wire-only coverage and is not substituted for an O24–O28 producer proof.

The P27/P28 unsupported OS-control residual-risk decision belongs to
Homeowner/security, not to a test fixture. W4 does not invent a positive host
posture state or an independent reconciler owner.

## 2. P25 bounded product correction

The Agent managed RawVault gate accepted a portrait class maximum larger than
aggregate host plaintext capacity. The predecessor product test failed **0/1**
at `Assert.Throws Failure: No exception was thrown`. Production now rejects
both portrait and live-selfie class maxima above aggregate with
`RAW_EXPORT_SOURCE_CAPACITY_CONFIGURATION_INVALID`; a polling-interval error
retains its separate generic configuration code. The same test passed **1/1**
after the correction. Removing only the portrait/aggregate guard failed
**0/1** at the same invariant. The corrected Agent production SHA-256 is
`051750515D4C6BC2B6E8F38E83886643576236637EF7658D42FFE0FADD5856CE`;
the narrow mutant SHA-256 is
`F28D66B9AE6FFA5921B3E467375DFBD6C180B07DF5271836A624DA3295E9324D`.
The corrected source was restored byte-exact. This is a real product correction,
not P25 row closure.

The remaining P25 gap includes the polled Agent configuration path and the
authority's runtime framing relation: capacity must accommodate
`min(active ChunkSize, declared body)`. The current admission path has no
active-profile ChunkSize at its decision point. W4 does not claim that a
transport chunk or a synthetic Final closes this relation.

## 3. Restored horizontal gates and diagnostic

- Server current-byte readiness plus All14 wire no-egress: **17/17 PASS**, zero
  fail/skip, TRX SHA-256
  `86EC85232D27EE5EFC734ED02850CE785974686204C7DD818264645EA2F70DBC`.
- Agent current-byte configuration and retained ownership: **94/94 PASS**,
  zero fail/skip, TRX SHA-256
  `7BCD43C8F2A9B06C7743028D95B08B92F3393F5690866B38168C04AB08E97B2A`.
- Server and Agent builds were initially started concurrently and contended for
  the shared Agent.Core intermediate DLL (`CS2012 file in use`) before test
  execution; no TRX was created. W4-006 is an excluded build diagnostic. The
  sequential Agent successor passed 94/94 on the same source bytes. No
  business assertion failed in that invocation.

Earlier within-wave controls: Server **3/3**, Agent correction **1/1**, Agent
restored configuration **41/41**. These are current-byte component controls,
not a full Server↔Agent owning-side row proof.

## 4. Machine evidence and accounting

`a3_macro_wave_w4_trx_inventory_v1.tsv` inventories all **7/7** W4 TRX in
both repos; `a3_macro_wave_w4_failed_run_census_v1.tsv` classifies both failed
TRX as **EVIDENCE_RED** (one actual predecessor product defect, one deliberate
guard mutation). The build collision had no TRX and is retained in scratch
ledger W4-006. `a3_macro_wave_scratch_evidence_v1.tsv` contains W4-001 through
W4-008 with run/source SHA and restore status. The whole-repo manifest verifier
checks every W4 TRX against the manifest and rejects unclassified failed W4
runs, duplicate mutant hashes, live mutant source matches, and changed current
bytes. The W4 candidate manifest SHA-256 is
`560E73D4027633F922DD01AA5A5E76EC39036C69DB624EFAAC28271AC74709A1`
for **2,472 files**. The W3-ratification predecessor manifest SHA-256 is
`2F96BA6A77B0538748F6CC3A4F9D2C377B04C0C26A31A7276C806D64A439AA3F`.
The exact predecessor delta is **18 added / 4 modified / 0 deleted**, matched
by all **22/22** entries in `a3_macro_wave_w4_allowed_delta_v1.tsv`; **0** lie
outside the allowlist. The added W3 manifest/sidecar, ratification record and
seal descriptor are ordinary predecessor artifacts excluded from W3's own
self-referential manifest, not newly minted W4 decisions.

Machine counters from the final verifier: `w4_trx_in_manifest=7/7`,
`w4_trx_inventory=7/7`, `outside_name_family_failed=0`, `w4_failed_runs=2`,
`classified_failed_runs=2`, `unclassified_failed_runs=0`,
`mutants=102 distinct`, `live_mutant_matches=0`,
`staged_server=0`, `conflicted_server=0`, `staged_agent=0`, and
`conflicted_agent=0`. The current packet and manifest/sidecar are intentionally
excluded from that manifest to avoid self-reference; the packet SHA is pinned
in the handoff rather than claiming to be inside its own manifest.

Verbatim final verifier output (`-Mode Verify`):

```text
manifest_files=2472
manifest_sha=560E73D4027633F922DD01AA5A5E76EC39036C69DB624EFAAC28271AC74709A1
w4_trx_in_manifest=7/7
outside_name_family_failed=0
w4_trx_inventory=7/7
observed_delta=22 allowed_delta=22 outside_allowlist=0
w4_failed_runs=2
classified_failed_runs=2
unclassified_failed_runs=0
mutants=102
live_mutant_matches=0
staged_server=0 conflicted_server=0
staged_agent=0 conflicted_agent=0
```

Verbatim partition-verifier output:

```text
official_open_rows=46
partition_rows=46
assigned_once=46
duplicate=0
unassigned=0
extra=0
macro_waves=6
wave_W1_R2_R6_EXECUTION=8
wave_W2_AUTHORIZED_IMPLEMENTATION=11
wave_W3_ADMISSION_OUTCOMES=5
wave_W4_INTERNAL_NO_EGRESS=5
wave_W5_RETENTION_CUSTODY=12
wave_W6_TRANSPORT_SEAMS=5
scratch_runs=39
```

The partition remains **46 assigned once, 0 duplicate, 0 unassigned, 0 extra**.
No full suite, stage, commit, push, A4 or production activation was performed.
Both repos must have staged=0 and conflicted=0 at handoff; unstaged/untracked
worktree content is preserved and is not called clean.

## 5. Review request

Accept or reject the bounded P25 correction and the five explicit W4
dispositions. Do **not** ratify P24–P28 from this packet. The next work on P24
and P25 is joined proof/product work; P26–P28 require named production and, for
P27/P28, authority owners. A3 remains HOLD with seal count 46.
