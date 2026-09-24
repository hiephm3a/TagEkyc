# A3 activation-scope topology guard — bounded review packet v1

## Disposition requested

Review the narrow mechanism that binds an approved assembly exclusion (`Disabled`) to the effective runtime topology. This packet does **not** request row closure, a 46→7 census change, an Activated route, or re-minting the current seal. A3 remains HOLD. The Homeowner-approved scope re-baseline and its exact governance transaction are still to be frozen together.

## Mechanism

- The production host resolves `RawExportAssemblyOptions` from its effective configuration and registers `ICaptureRuntimeAssemblyTopology` from that same options singleton.
- Before pepper access, A3 readiness, or Activated route selection, startup requires a format-2 seal with approved/build topology `Disabled` and equality to the effective topology. Format-1 seals can never authorize zero authority-open rows.
- Format-2 seal generation requires the exact `APPROVED` scope decision, `NO` assembly inclusion paired with `Disabled`, the exact census and ownership/partition/ledger SHAs, plus current-byte checks for authority sources cited by candidate/assurance rows. Missing or mismatched provenance fails generation. Runtime does not read governance documents.
- The existing format-1 seal descriptor remains at revision 2/count 46. Neither canonical partition nor reconciliation ledger was edited for this bounded guard proof.

## Current-byte source identities

| File | SHA-256 |
| --- | --- |
| `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeStartup.cs` | `2156B282A57B8F5CCA296F32E79C51140DF5F4727AA909D128971385261CD65D` |
| `src/TagEkyc.Api/Program.cs` | `2DB6EE5AD07BA02C4387972C8839B27D6E76C167491B7CD96ADE87D4CDF7D444` |
| `tests/TagEkyc.UnitTests/Tip88C1C6BA1StartupTests.cs` | `8DB345787FCE756111DD0C4356D9E7045D5200793033C72A2557D4649682322A` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `BB7ED03EBCAC1DFFC7B02D1F1F2448C80DA12D954C1E3579519CA07AA788D7C6` |
| `tools/Generate-CaptureRuntimeActivationEvidenceSeal.ps1` | `AA6C0C41C57094FCBBBCA1D02395FFA306706BB50C281CEC9F5DD8E8D639866C` |

The mutation changed only `effectiveAssemblyTopology == seal.ApprovedAssemblyTopology` to `true` in `CaptureRuntimeStartup.cs`. Mutant source SHA: `B72625009C3BD5B6351F493EDBBCA8CE7EFB015668BEE08DA7657B7ECF7F8002`. The source was restored byte-exact to `2156B282...61CD65D` before the final gates.

## Raw test inventory

Counters are `total/passed/failed/skipped` from each TRX.

| TRX | SHA-256 | Counters | Role |
| --- | --- | --- | --- |
| `tests/TagEkyc.UnitTests/TestResults/a3-scope-topology-baseline-unit.trx` | `67757C423633D2F7EBE668D4D869C38E1FF8C2CDA21DC4E0A7AF60B910B96F02` | 17/17/0/0 | Baseline GREEN |
| `tests/TagEkyc.IntegrationTests/TestResults/a3-scope-topology-baseline-host.trx` | `5779CB5CCF3C9A5C6FA54DE27F7E0A4298674D5ACBBF27F1047D2DA1926A607D` | 11/11/0/0 | Baseline GREEN |
| `tests/TagEkyc.UnitTests/TestResults/a3-scope-topology-guard-mutant-unit.trx` | `D760173D7411FFB05A697568EF3162C7CADF37AF2ED9AC567C574DABE3997311` | 17/14/3/0 | EVIDENCE_RED: DurableWorker, FixtureProof, Invalid; all other cases PASS |
| `tests/TagEkyc.IntegrationTests/TestResults/a3-scope-topology-guard-mutant-host.trx` | `F0DFC3F53D92343B79E9204A081F9B0C4612CB135AB1045FFD6A9B593F745ABC` | 11/10/1/0 | EVIDENCE_RED: effective invalid topology; all other cases PASS |
| `tests/TagEkyc.UnitTests/TestResults/a3-scope-topology-restored-unit.trx` | `D87DF756CBD07F84ACB6ED5100325004E1FC4455466A182708273783C028B6DE` | 17/17/0/0 | Restored GREEN |
| `tests/TagEkyc.IntegrationTests/TestResults/a3-scope-topology-restored-host.trx` | `01318992B1831F240220399C129C8796728215E2403AB4A709ECF1CB3B30E95A` | 11/11/0/0 | Restored GREEN |

The unit REDs fail at `Assert.Throws(): No exception was thrown`; the host RED fails at `Assert.ThrowsAny(): No exception was thrown`. The failed-run census records both failed TRX. An earlier host attempt tried `DurableWorker` and `FixtureProof` in the host fixture; both were rejected during DI validation before the target startup assertion, so they were removed from the host case set and retained as direct startup controls. That pre-target diagnostic did not create a TRX and is not counted as evidence.

Generator self-tests: `6/6 PASS`, including valid scope, contradictory NO/YES topology and wrong census. The existing API builds on the current format-1 descriptor. The format-2 path has not yet been run against a signed real scope decision/manifest; that is part of the later single governance transaction, not claimed here.

## Boundary and next transaction

This proof establishes a load-bearing runtime topology comparison and a guarded generator design. It does not by itself establish the seven-row partition, bind an actual Homeowner scope record, classify deferred backlog, or prove all four retained mechanisms. After independent review of this bounded guard, perform one successor governance transaction for the signed scope decision, E01 reconciliation, deferred-row inventory, exact seven-row partition/verifier, reviewed manifest and ratification record, and format-2 seal. The nonzero seal must continue to block Activated.
