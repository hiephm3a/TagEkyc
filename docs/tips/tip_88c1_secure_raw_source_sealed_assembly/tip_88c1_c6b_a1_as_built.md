# TIP-88C1-C6B-A1 — As-built evidence

Moved verbatim from the reviewed parent Completion section under Homeowner
correction f53454e8-7c46-48fc-b592-d733f0c545ea. Historical measured runs below
describe the preceding candidate; final correction measurements are recorded separately.

## Bounded correction f53454e8 — current measurement boundary

- SPECIFIED: Preserve product implementation, R26 wire/fingerprint/identity/ACL,
  R13/R24/R25/R27/R28 and typed gateway. No RRI, no project changes, no stage/commit/push.
- DONE: Moved the whole former Completion report into this separate file. The
  normative parent has no report tables and ends at its preceding section.
  Parent: `9BC62E297098A2E06E20DF6B14786F7BDF0DC3008B7F652178119DC01FE2E661`,
  988 lines. Operation Master:
  `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018`,
  1605 lines. Their current dependency bindings are rehashed together.
- DONE: Named matching-operation A1 durable replay separately from
  HISTORICAL_LANDED_CANCEL. No-capability history writes nothing; elapsed live
  capability history materializes one Expire operation/Expired event before
  returning the persisted Cancelled response. Neither fabricates Cancel/audit
  history. No Idempotency-Key is consumed.
- DONE: Added the exact named two-shape real HTTP/PG theory and strengthened
  first-A1 cancellation/exact-replay positive control. Only
  `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1CancellationHttpTests.cs` changes
  among the 625 frozen source files. The session schema has no Reason column:
  the proof preserves the complete existing session/audit bytes and proves no
  synthetic reason-bearing Cancel operation/audit is created.
- MEASURED: Integration project build: 0 errors, 50 warnings (including synthetic
  raw-SQL test warnings); not a zero-warning claim.
- MEASURED: `arch-asbuilt-correction.trx`: 153 passed, 0 failed, after removal
  of the report and final normative parent mutation.
- MEASURED: `r26-normative-asbuilt-correction.trx`: 20 passed, 0 failed.
  This is 17 R26 cases including the two new historical cases plus 3 complete
  normative SQL cases, executed after the final test/catalogue mutation.
- MEASURED: Full integration completed: 1000 passed, 0 failed, 1 explicit manual
  generator skip, total 1001. Run 2026-09-12 18:15:48 to 19:12:05 (+07:00).
  This current run, not the historical 998/999 run below, covers the new test bytes.
  Current source freeze: `source-freeze-asbuilt-correction.json`, 625 files,
  `B3517A6F2E632301FCF9CE28A3764E656A77228AC4C927CCF479257785CE181B`.
- MEASURED: Focused PostgreSQL execution acquired
  `Global\TagEkycPostgresFixture` exclusively; before/after census contained
  only untouched `signflow-postgres` on 15432. No TagEkyc fixture residue.
- MEASURED: Full execution used the same exclusive mutex and its final census
  also contained only untouched `signflow-postgres` on 15432. Exit code 0;
  no TagEkyc fixture residue. The sole skip remains the explicit manual
  `Manual_generate_tip67g_golden_vectors` generator, not a failing regression.
- CORRECTED EVIDENCE CLAIM: Historical `arch-final-rebind.trx` 153/153 was
  measured before the Completion table was appended to the previous parent.
  It did not validate that final parent. The independent reviewer reproduced
  duplicate R26/R27/R28, and this correction accepts that finding. Current
  ArchTests are measured after separating the report, not before writing it.

Exact focused command:
`dotnet test tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Tip88C1C6BA1CancellationHttpTests|FullyQualifiedName~Tip88C1C6BA1NormativeSqlProofTests" --logger "trx;LogFileName=r26-normative-asbuilt-correction.trx" --results-directory TestResults/a1-r26-correction -v minimal`

Exact full command:
`dotnet test tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj --no-build --logger "trx;LogFileName=integration-full-asbuilt-correction.trx" --results-directory TestResults/a1-r26-correction -v minimal`


### MEASURED — correction evidence hashes

| TRX in TestResults/a1-r26-correction | Result | SHA-256 |
| --- | --- | --- |
| `arch-asbuilt-correction.trx` | 153 passed / 0 failed / 0 skipped | `F554827CD91FAA70E5A3324760C2639BBFD1B01352A45BADD9F898DB62BA72F7` |
| `r26-normative-asbuilt-correction.trx` | 20 passed / 0 failed / 0 skipped | `84C7C52A256CB9A7732DB5ED7B8B4090FD5E55064F7BB41D4FE84CA63781AC46` |
| `integration-full-asbuilt-correction.trx` | 1000 passed / 0 failed / 1 skipped | `0B704051D2CB24FADC11C6A74ADC899F66DA88D5138AD9E3605897793DC0447C` |
| `arch-asbuilt-correction-final.trx` | 153 passed / 0 failed / 0 skipped | `637EC6556FE71FDD4CA760A7D8700313EE0910FF7013416F468F204B870CD382` |

All correction measurements follow the final mutation of their target source or
normative artifact. The report is separate and not parsed as catalogue rows.
Parent/source bindings are verified as full 64-character SHA-256 values, not
prefixes. Independent bounded as-built acceptance remains the next review;
this report grants no new authority.


## Completion

**State: A1 IMPLEMENTATION CANDIDATE — READY FOR INDEPENDENT AS-BUILT REVIEW.**
This records measured implementation evidence, not a fresh Homeowner grant or
an independent CC/GPT acceptance verdict.

### Outcome vs Intent

| Intended outcome | Actual result | Evidence / boundary |
| --- | --- | --- |
| A1 server identity/control foundation | Implemented and exercised on synthetic PostgreSQL 16 | Frozen source inventory: `TestResults/a1-r26-correction/source-freeze-v10.json`, 625 files, inventory SHA-256 `611E7649E2ADC071AD544C59131DA9107CB4628D59BF7644F8F5F0E82F2E9BE4`; no source drift after the full run |
| R26 exact internal cancellation identity | Eight-input function; no Idempotency-Key; raw 32-byte server-derived fingerprint; first committed metadata replay; expiry-on-denied in B | Canonical §R26, 47-function manifest, real HTTP/PG cancellation tests; narrow existing-owner audit INSERT grant, no online role expansion |
| R28 startup/global pepper requirement | Prepared/Activated selection, global census lifted without membership change | Same-state/same-time sets and counts, six branch-removal RED controls, restricted-role startup proofs |
| R27 N/HTTP/A3 boundary | Committed nonce before one unread stream handoff; replay cannot call A3 again; closed result shapes | Real HTTP/PG tests included in full run; this does not implement A3 production behavior |
| Canonical schema/transition execution | DDL and complete SQL proof fences execute; R11/R13 return literals match bigint contract | Normative runner, DDL projection and lifecycle/race tests; no new transition design |

### Measured execution

Evidence directory: `TestResults/a1-r26-correction/`. Full source/build and
earlier failures remain preserved, not overwritten or relabelled green.

| Run | Result | Exact TRX SHA-256 |
| --- | --- | --- |
| `unit-v10.trx` | 322/322 PASS | `13D9F5D169E8AEE0B27FEA3F2AE077787B177F227BF3D0168E0CAF47786DAC55` |
| `arch-v10.trx` | 153/153 PASS | `322D950AFAC95327A608735EBBDE1A3048E4409E49FF15713E435088AEC26362` |
| `contract-v10.trx` | 15/15 PASS | `ECCBB0F81A5978CA912EFD1AAB8E43D99DD2FFA841575731883446C20634F323` |
| `integration-full-v10.trx` | 998 passed / 0 failed / 1 explicit manual skip | `20FE28AAEF195B1D925F29DAC85EFFABA3EB468A59A99390490FFD464786B293` |
| `normative-v10.trx` | 3/3 PASS | `35938DEF6B6537B4FD5F2E642D32FCA8FA279336F53A461F51E3AAC94648FB5C` |

Post-rebind `arch-final-rebind.trx`: 153/153 PASS; SHA-256 `BA28D5BEA3A10ACCCA97987F241599A52B70E6EAD383839E0A50E7EB01417C3F`.
Post-rebind `normative-final-rebind.trx`: 3/3 PASS; SHA-256 `BB35990A5ED1F5260384C0467CE4AFA9953789E91B7CA038C3356DE8014F1C3A`.

Full integration ran from 2026-09-12 09:04:20 to 09:58:18 (+07:00).
The only non-executed test is
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`:
its source explicitly marks it a manual one-shot fixture generator, not a normal
test. No failed test is hidden by this skip. The whole solution build passed
with 0 errors and 47 warnings; warnings are not claimed absent or all historical.
Full integration includes the new A1 proofs; the earlier scoped 137/139 result
is superseded by the current full run, with its failure evidence retained.

### Decision / Branch Disposition

- R26, R28, typed-gateway, CRT1 and rotation decisions remain as ratified.
  No additional authentication fallback, endpoint, package lifetime or authority
  engine was invented during final reconciliation.
- A2 Agent/SDK and A3 production ingress remain excluded. Broker/mTLS and
  purge/legal-hold dispositions are unchanged. Production and real retained
  biometric activation remain prohibited.
- Four historical Down tests now isolate cluster-global roles in disposable
  PostgreSQL clusters. The product Down dependency guard was not weakened.
  C527/R2A4 historical architecture guards are reconciled; current full
  ArchTests are green.
- Existing reuse census above is the pre-implementation baseline, not a claim
  that the implemented A1 objects are still missing.
- No project-reference/.csproj change in this continuation. No stage, commit
  or push. Dirty/untracked worktree bytes are the candidate, not landed commits.

### Proof scope and debt / gap final state

Actual operator/enrollment HTTP matrices exercise real handlers, parsers and
response mappers with explicitly fake typed services/authentication; they are
not end-to-end cryptographic/SQL proofs. Separate production crypto/PG and
execution HTTP tests provide those evidence sources. Catalogue-join tests
check finite declarations, identities and ownership sets, not every runtime
acceptance rule by themselves. The 34-entry proof map is navigation, not
coverage inferred from matching method names.

PI-TAG-001 rounds 1–10 and cumulative root causes are recorded in
`TestResults/a1-r26-correction/evidence.json`. Round 5 required the scope-wide
checkpoint; rounds 6–9 repaired incomplete earlier coverage and fixture defects.
Round 10 has two same-surface bounded subagent verdicts of zero actionable
findings. This is not represented as independent CC/GPT review of the complete
as-built candidate, nor as ratification of PI-TAG-001 for other TIPs. No round 11
was opened. Earlier failed/aborted runs remain classified in the evidence log.

Final handoff is for independent as-built review and Homeowner ratification.
Operational deployment, production activation, A2/A3 completion, and
stage/commit/push remain ungranted.
