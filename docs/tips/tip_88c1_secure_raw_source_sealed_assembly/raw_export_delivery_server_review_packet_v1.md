# TagEkyc raw-export control plane and durable assembly work source — review packet v1

Status: `READY_FOR_INDEPENDENT_REVIEW`

Manifest SHA-256: `2B11C3FD4B315AB7E654471392BE74EAAB722F02988999C2CB370C922D5C37CC`

This is the separate TagEkyc packet requested after the client review. It contains no claim about SignFlow runtime wiring.

## Product delta

The implementation adds bounded adapters around existing TagEkyc mechanisms:

- raw-export authorization/job/package control-plane contracts and endpoints;
- a projection reader from durable job state to final package identity;
- a production `DurableRawExportAssemblyWorkSource` registered for the explicit `DurableWorker` topology;
- secret-reference backed assembly authentication material;
- a PostgreSQL candidate-selector function and least-privilege resolver capability.

Assembly remains `Disabled` under the approved activation-scope decision. This packet does not enable it and does not change the activation census.

## F03 work-source/PostgreSQL proof

One consolidated PostgreSQL run proves all requested behavior without repeated database resets:

```text
Claimed packet job is selected and acquired
two workers cannot acquire the same live lease
live Assembling lease is not returned as a candidate
expired lease is reclaimed with a distinct attempt
retryable outcome records attempt failure and becomes reacquirable
terminal outcome terminalizes with exact authority reason
Sealed outcome does not terminalize or release a live lease
expired JobExpiresAt/PermitExpiresAt is excluded
function owner = tagekyc_raw_export_deployer
SECURITY DEFINER = true
search_path = pg_catalog
EXECUTE = resolver only; PUBLIC/runtime/sealer denied
migration up -> down -> up recreates/removes/recreates the function
```

Retained final result: `1/1 PASS`, zero skip.

## Control-plane evidence

Current-byte focused unit result: `2/2 PASS`, zero skip. It verifies exact identity/scope/idempotency/class preconditions and reuse of the existing authorization/job repositories.

## Failed-run accounting

Six run files are retained: five predecessor RED files are classified and one final file is PASS:

| File | Class | Reason |
|---|---|---|
| `a3-raw-export-work-source-current.trx` | `EXCLUDED_DIAGNOSTIC` | Docker engine was not running; test body never executed. |
| `...-v2.trx` | `SUPERSEDED_RED` | read assertion reused a tracked EF context. |
| `...-v3.trx` | `SUPERSEDED_RED` | fixture expected retryable failure to rename state `Claimed`; production correctly keeps `Assembling` while making it reacquirable. |
| `...-v4.trx` | `SUPERSEDED_RED` | expired-job fixture violated the equality deadline constraint. |
| `...-v5.trx` | `SUPERSEDED_RED` | fixture updated only `JobExpiresAt`; schema requires `JobExpiresAt = PermitExpiresAt`. |
| `...-v6.trx` | `PASS` | final current-byte proof. |

There are zero unclassified failed runs in this bounded slice.

## Registry/backlog correction

The eight P29–P36 ownership rows no longer claim that a production work source is absent:

```text
ProducerCount                 1
Production work source       present and registered
ProducerReachableInActivated NO (current topology is Disabled)
Proof boundary               PRODUCTION_COMPONENT
Disposition                  still deferred by Homeowner scope
Reason                        ASSEMBLY_NOT_ACTIVATION_PREREQUISITE
```

The generated deferred backlog was rebuilt and verified: `38` rows total, including exactly `8` assembly rows. No deferred row became `IMPLEMENTED`; no P29–P36 row is closed by this packet.

The proof-line citation drift exposed during verification was mechanically corrected. The legacy ownership verifier now reaches a pre-existing unsupported proof-strength value on `A3_AgentRawExpectDoesNotSendBeforeCommittedR1` (`UNKNOWN_PROOF_STRENGTH`), outside this delivery slice. That unrelated verifier/schema defect is disclosed rather than misreported as a clean whole-table PASS. The backlog verifier itself passes Build and Verify.

## Activation state and non-claims

```text
assembly topology             Disabled
P29-P36                       deferred / not implemented
activation census             unchanged
activation seal               not re-minted
production activation         not authorized by this packet
```

Builds continue to emit the expected fail-closed warning `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT`; the implementation does not mint its own successor authority.

Requested disposition:

```text
ACCEPT bounded TagEkyc control-plane adapters
ACCEPT F03 durable work-source/ACL/migration proof
ACCEPT registry/backlog correction without row closure
or HOLD with an exact remaining technical finding
```
