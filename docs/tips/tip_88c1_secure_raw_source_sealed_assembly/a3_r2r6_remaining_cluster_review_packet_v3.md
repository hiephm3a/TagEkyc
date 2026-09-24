# A3 R2–R6 remaining-cluster review packet v3

Status: **SUCCESSOR CORRECTION / INDEPENDENT REVIEW REQUIRED**. This packet supersedes the HOLD disposition of v2 only for the three findings below. All accepted v2 evidence remains frozen. No full suite, stage, commit, push or production activation occurred; A3 remains HOLD.

## 1. Provenance and census correction

The v2 phrase “previously ratified” was correct for four rows, but it incorrectly included BP17 ciphertext. The reconciliation chronology establishes:

- BP18 was independently accepted before the BP21 successor;
- BP21 was independently accepted before the BP16/BP17 successor;
- BP16 and BP11 were jointly ratified after the complete failed-run audit on manifest `4DBCB74F…9223`.

Those four rows were already excluded from the incoming **official 56** and must not be subtracted again. BP17 ciphertext has strong builder evidence but no recorded Homeowner ratification event; its table prose is restored to candidate-pending-ratification rather than upgraded by documentation. Independently, ratifying the five new P rows would move the official census from **56 to 51 open = 50 normative + 1 seam** while BP17 remains open. No historical packet is rewritten.

## 2. P19 exact first public response

Independent review correctly found that the v2 assertion permitted either O18 or O19 for the first clean-mismatch response. The test now requires:

```text
CONTENT_COMMITMENT_MISMATCH / HTTP 422 on the first production Agent response
```

while retaining the exact terminal cause, positive-absence object evidence, key revocation, no Available publication and bodyless same-UUID terminal replay assertions. Current test source is `Tip88C1C6BA3R2R6ClusterHttpTests.cs`, SHA-256 `3506F74EFF64843426CD11BAF46348B99488C9F67E571D85447454168BA024C6`.

- Tightened current-source family: `a3-r2r6-p19-exact-first-response-baseline.trx`, SHA-256 `1E32A05DADE8223F83AB5FF917594197BBAA27DDB0D1E45A4F363A58C1308E48`, **9/9 PASS**.
- G-O19 changed only production terminal O19 projection to O18, pipeline mutant SHA-256 `4B98AEC57EDFC41AF61FA871258527FAE9AC3F6311CD6EBAF6938EB34919FE1D`.
- `a3-r2r6-p19-first-response-projection-red.trx`, SHA-256 `725690003AB82847CB46D651CE705A02C3C03AAF6B14542469D282767201F535`, is **8 pass / 1 fail**. Only `contentMismatch:true` is RED at expected `CONTENT_COMMITMENT_MISMATCH`, actual `RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE`.

The pre-mutation 9/9 proves current production already had the required behavior; this correction tightens and discriminates the proof rather than changing product semantics.

## 3. P20 current-byte ciphertext mutation

Independent review also correctly requested a current-byte repin for the durable-ciphertext claim. G-O20 changed only the production verifier-failure intent cause from `RECAPTURE_REQUIRED` to `CONTENT_COMMITMENT_MISMATCH`, pipeline mutant SHA-256 `641FB57F91CC9858E3785B4FB1BA45D4E6BA46FC9E90DEEF058BBD49487B8833`.

`a3-r2r6-p20-ciphertext-current-source-red.trx`, SHA-256 `91C54B591388216FCB5F2A3E4D12405F7F5B609EFC83F2476F9E8178E94F71FC`, is **1 pass / 1 fail**. Only `tamper:true` is RED at the first production `AdvanceAsync` with `RAW_INGRESS_CONTINUATION_NOT_READY`; the untampered durable-ciphertext control remains GREEN. This repins the semantic distinction on current source rather than relying only on the older BP17 run.

## 4. Restoration

Both mutations were isolated and restored. Production pipeline returned byte-exact to SHA-256 `399F7D61E2E5C4789D58842F4F7E7CF84E789FE5E84399CCF951D02F881036F2`. After rebuild, `a3-r2r6-review-correction-restored-joined-final.trx`, SHA-256 `E3BBD889A6DA26F01945AB0E25E9A09D7F60FAC71F48E323A29DAC58B3307E59`, passed **27/27**, zero fail/skip, including the tightened P19 family, P20 ciphertext pair and all v2 cluster sentinels.

The successor failed-run delta is now **8 EVIDENCE_RED + 1 SUPERSEDED_RED = 9**, unclassified zero. G-O19 and G-O20 are present in the cumulative mutant registry; no source mutation survives.

## 5. Requested disposition

The two reviewer holds are both addressed rather than choosing between them:

| Row | v3 disposition request |
| --- | --- |
| P17 / O17 | Ratify the accepted v2 evidence |
| P18 / O18 | Ratify the accepted v2 evidence |
| P19 / O19 | Ratify after exact-first-response tightening and G-O19 RED |
| P20 / O20 | Ratify after current-byte G-O20 RED |
| P22 / O22 | Ratify the accepted v2 evidence |

BP17 `CiphertextFailureIsNotProducerMismatch` requires a separate Homeowner ratification decision; this packet does not silently include it. All ten OPEN/blocker rows listed in v2 otherwise remain OPEN. Official census stays **56** until the requested five-row disposition is authorized; after those five rows alone, it becomes **51**. This is not A3 PASS.

## 6. Mechanical freeze

The successor manifest is generated after this packet, the matrix, failed-run census, registry and reconciliation ledger are frozen. It contains **1,870 files** (1,717 Server + 153 Agent) and resolves **582/582** distinct referenced TRX, with zero missing, ambiguous or omitted reference. Against candidate v2, drift is **7 added / 7 modified / 0 deleted**: the v2 manifest+sidecar as predecessor artifacts, this v3 packet, four new TRX, one tightened test source and six audit/document artifacts.

The cumulative registry contains **88 distinct mutant hashes**, duplicate zero and live match zero. The successor failed-run census contains **9/9** classified failures: 8 `EVIDENCE_RED`, 1 `SUPERSEDED_RED`, 0 excluded and 0 unclassified; every failed TRX is in the manifest and `outside_name_family_failed=0`. Both repositories report `staged=0, conflicted=0`; the worktrees remain intentionally dirty and no cleanliness claim is made.
