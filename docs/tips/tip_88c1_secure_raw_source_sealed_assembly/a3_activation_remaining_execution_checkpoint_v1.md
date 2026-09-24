# A3 activation remainder — bounded execution checkpoint

Status: technical checkpoint only. This document does not ratify a RowId, change the seven-row activation partition, re-mint the seal, or qualify a hospital deployment. A3 remains HOLD.

## Results on current restored bytes

- Server joined focused gate: `tests/TagEkyc.IntegrationTests/TestResults/a3-activation-remaining/a3-remaining-final-restored-joined.trx`, SHA-256 `92212AA540109692E0DCA361F5BA171A7256D8CC38D938CF1115479A1413993D`, 15/15 PASS, 0 skipped. The gate contains six BP10 exits, existing public O08, two new P05 provider-loss cases, and the joined reachable admission finals (including P04/P05).
- Agent closed result catalogue/transport focused gate (sibling CaptureAgent repo): `tests/TagEkyc.CaptureAgent.Tests/TestResults/a3-activation-remaining/a3-o-shape-agent-restored-v2.trx`, SHA-256 `AB9871339ABD239358A86CF55C1C4570AA535F46C73C25799FB79436CFD622B8`, 32/32 PASS, 0 skipped.
- BP10: `long.MaxValue` claimed body is bounded to the configured six-byte retained window, leaving exactly two of eight bytes and consuming one of two stream slots while admission is in progress. Both counters return on Final, malformed Final, broker fault, Handoff, pipeline fault, and cancellation. Removing lease disposal made all six cases RED; restored source `CaptureRuntimeRawIngressAdmissionService.cs` SHA-256 `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C`.
- P05: an unavailable active commitment key returns exact O05 before B with no custody changes, and restoration permits the same metadata to enter B. An explicit payload-provider failure inside B returns O05 only after rollback; restoration permits exact retry. Two separate O05-to-O08 guard mutations made only their respective named test RED. These are incremental proofs, not exact P05 closure: the existing Evaluating shell/token and original-budget preservation still need a joined scenario.
- O/E/S wire shape: the Server now omits optional null members from `CaptureAgentFinalResult` while retaining fixed `Content-Length`; the Agent requires the exact O/E/S field sets. The previous Server emitted five fields even for O, while the contract requires `{outcomeCode}` only. Restoring explicit-null emission makes the public O08 test RED; restored joined Server and Agent gates above PASS.
- P04: the fresh invalid-header O04 branch remains green, but the endpoint returns before broker/admission when Content-Encoding or Trailer is present. There is still no authorized existing-R1 pre-start terminalization/fenced-cleanup join. Do not count this as closed.

## Failed-run accounting for this bounded slice

The new slice directories contain 11 Server TRX and 2 Agent TRX. Six runs have failures and all six are classified below; other seven runs are green. Historical TRX outside these directories are not represented by this count.

| Run | Cases | Classification | Failure and disposition |
| --- | ---: | --- | --- |
| `a3-bp10-lease-release-mutant.trx` | 0/6 | EVIDENCE_RED | Removing `using` leaked the live capacity lease; all six exits failed the full-window reacquire. Mutant restored. |
| `a3-bp10-restored-joined.trx` | 6/7 | PRODUCT_DEFECT_PREDECESSOR | Public O08 exposed explicit-null fields in an O response. Server writer and Agent parser corrected; final joined gate green. |
| `a3-o-shape-explicit-null-mutant.trx` | 0/1 | EVIDENCE_RED | Re-introducing explicit null optional fields made public O08 field-set assertion RED. Mutant restored. |
| `a3-p05-preb-code-mutant.trx` | 1/2 | EVIDENCE_RED | Pre-B provider-unavailable projection changed O05 to O08; only active-key test RED. Mutant restored. |
| `a3-p05-inb-code-mutant.trx` | 1/2 | EVIDENCE_RED | In-B provider-unavailable projection changed O05 to O08; only rollback/retry test RED. Mutant restored. |
| Agent `a3-o-shape-agent-restored.trx` | 9/31 | SUPERSEDED_RED | The old test helper injected explicit null fields into catalogue JSON after the parser correction; helper removed, explicit-null negative added, v2 32/32 green. |

## Authority disposition

`BP10`: bounded mechanism proof is a candidate for independent row review, not Homeowner-ratified here. `P05`: OPEN with exact existing-Evaluating-shell/budget joined gap. `P04`: OPEN with exact existing-R1 terminalization gap. Four `TRANSPORT-EXPECT-FENCE` rows remain OPEN pending their separate deployment-topology qualification. Activation-open count remains **7**; the existing seal is not re-minted. Source drift is correctly rejected by the current build seal (`ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT`) until an authorized governance transaction.
