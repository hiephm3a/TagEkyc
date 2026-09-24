# A3 BP10 — row-closure review packet v1

Status: **technical closure requested for BP10 only; no ratification recorded**. Activation-open census remains **7** and A3 remains **HOLD**. P04, P05 and all four `TRANSPORT-EXPECT-FENCE` RowIds remain OPEN.

## 1. Exact row and production mechanism

`BP10 WindowCapacityIsBoundedAcrossAllExitPaths` requires the raw plaintext window to be bounded and both the aggregate-byte and concurrent-stream capacity to return on every success, denial and fault exit. The current admission service computes `min(claimed length, maximumPlaintextWindowBytesPerStream)` before `RawExportIngressCapacity.TryAcquire`, and its `using` lease releases both counters when admission exits.

Current source and test SHA-256:

| File | SHA-256 |
| --- | --- |
| `src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs` | `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `545DC9460ED308E26252547D053F3C8690B773B1CA185DC01C3ED2D45376F2BE` |

## 2. Load-bearing test and mutation

`BP10_BoundedWindowAndBothCountersReturnOnEveryAdmissionExit` sets aggregate budget 8 bytes, two stream slots, per-stream window 6 bytes and claimed length `long.MaxValue`. While the first admission is suspended in B, exactly two bytes remain; three bytes and a third stream are denied. It then exercises six exits: Final, malformed Final, broker fault, Handoff, pipeline fault and cancellation. Every exit re-acquires the full eight-byte window, which requires both counters to have returned. The body is never read in these cases.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `a3-bp10-bounded-exits-baseline.trx` | 6/6 PASS | `925147B1DA28C607F20613F8761C3B5B0BC017CCDA973E7C8331B50ED939B74C` |
| `a3-bp10-lease-release-mutant.trx` | 0/6; all six fail full-window reacquire | `AE76B3C7B0017DAF77202F78B36DCFAD9829DB2EB81A8B97C6DB19371C9A7D59` |
| `a3-remaining-final-restored-joined.trx` | 15/15 PASS, zero skip | `92212AA540109692E0DCA361F5BA171A7256D8CC38D938CF1115479A1413993D` |

The mutation changed only `using var lease` to `var lease` in the admission service. It was restored to the source SHA above. The 15-case restored gate joins BP10's six exits with current public O08, both incremental P05 provider-loss cases, and the six reachable Kestrel/Agent admission finals. The P05 cases are not used to close P05.

## 3. Server response-framing ratified sentinel (set 2)

The same bounded work discovered a separate contract defect: the A3 Server writer had serialized explicit null optional fields into O responses, while the ratified O/E/S wire contract requires exact field sets. The Server writer and Agent parser were corrected; `DefaultIgnoreCondition.WhenWritingNull` is scoped to `CaptureAgentFinalResult`, not generic `RuntimeError` JSON. This source change required its own Server-side ratified dependency set, independent of the Agent-client sentinel set.

The affected dependency scan selected the direct A1 raw-ingress HTTP outcome tests and the A3 21-result catalogue, All14 no-egress, exact CRT1 boundary, admission, broker-claim and nine R2–R6 publication/replay scenarios. One focused filter ran all 107 selected cases; it did **not** rerun unrelated process-kill or provider-only proofs. The catalogue test asserts the exact raw JSON member set for every O/E/S result, rather than merely looking for an expected code.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `a3-server-framing-ratified-sentinel-set2.trx` | 85/107 PASS; 22 old A1 assertions RED | `5B8EB54501B267680234828C65564C0605ABDA450F1A5E84D91EA78DAB58166F` |
| `a3-server-framing-ratified-sentinel-set2-v2.trx` | 107/107 PASS, zero skip | `C0D18AF71B54E103FBC761C7616B49369B92D1ED4851B734A03679158C8A5C42` |

All 22 predecessor failures were in `RawIngressA3Outcome_MapsWithoutReadingBody` (21 cases) and `RawIngressEvaluation_RetryHorizonIsAllowedWithoutOtherFields` (one case). Those older tests expected explicit null optional members. Their assertions were corrected to demand exactly O=`outcomeCode`, E=`outcomeCode`+`retryNotBeforeUtc`, and S=four success members; no product source was changed between the two 107-case runs. Current `Tip88C1C6BA1RawIngressBoundaryTests.cs` SHA-256 is `DDF867499DC1A429D36A64123A5B4FCD928187A629CD85D9D252F2FEFE299554`.

The Agent closed-result/parser sentinel from the predecessor checkpoint is 32/32 PASS (`a3-o-shape-agent-restored-v2.trx`, SHA-256 `AB9871339ABD239358A86CF55C1C4570AA535F46C73C25799FB79436CFD622B8`). An explicit-null Server mutation made public O08 RED; restored Server writer SHA-256 is `D172F1D094CB9064A80489DB12D84343C30B604BA737F3178C6FB0A0B4C56325`.

## 4. Failed-run accounting and limits

The predecessor [bounded execution checkpoint](a3_activation_remaining_execution_checkpoint_v1.md) accounts for its 11 Server and 2 Agent TRX, including six failed runs. This successor adds exactly two Server TRX: the 85/107 predecessor above is `SUPERSEDED_ASSERTION_RED`; the 107/107 successor is green. Thus the bounded slice now contains **15 TRX, 7 failed runs, 7/7 classified**, and no failed run is silently excluded. The checkpoint's six classifications remain historical; this packet does not rewrite its previously reviewed bytes.

The 107-case sentinel proves the Server response-shape correction on the selected current-byte HTTP dependency surface. It does not prove the P04 existing-R1 terminalization path, the P05 existing-Evaluating-shell/budget path, or direct-Kestrel hospital deployment topology. It does not change Prepared's separate legacy route. The seven-row activation partition SHA-256 remains `B7AC9BB68204FA69F37FB4F16BBFFD305C3437CC6E5BAA136ADFDCB6777C91E3`; no authority or seal transaction is claimed.

## 5. Requested decision

**Request independent technical acceptance and Homeowner ratification of BP10 only.** If ratified, a subsequent single governance transaction may remove BP10 from the retained partition, record the exact Homeowner decision, update the ledger and re-mint the activation seal from **7 to 6**. Until that transaction, BP10 is a technical candidate and the official count remains **7**. P04, P05 and four Expect rows remain open; Activated remains blocked.
