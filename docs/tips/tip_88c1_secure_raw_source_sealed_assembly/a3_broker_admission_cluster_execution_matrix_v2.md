# A3 BROKER_ADMISSION cluster execution matrix v2

Scope: exactly P02–P07. Incoming official census before the Homeowner disposition was **56 normative + 1 seam = 57**. The Homeowner ratifies P02 only, producing **55 normative + 1 seam = 56**. No full suite, landing or production activation.

| Row | Production reachability | Shared scenario | Body | Residue / Agent | Guard | Disposition |
|---|---|---|---|---|---|---|
| P02/O02 | Real zero-row bound read for both a missing artifact and an existing artifact owned by a different client/context | BA-A Kestrel/TLS + Agent + broker/PostgreSQL | 0 reads | exact outcome-only response; no foreign identifiers; all caller and foreign durable/provider families unchanged; terminal lease zeroized | BA-G1 projection + BA-G4 artifact/session/client isolation + BA-G3 terminal retry set | IMPLEMENTED / Homeowner ratified on v2 |
| P03/O03 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P04/O04 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P05/O05 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P06/O06 | Actual-excess R2 emitter exists; declared-excess pre-body emitter cannot receive ratified per-class maxima | prior frozen R2 component + current source audit | declared path absent | actual component does not close declared path | — | SEMANTIC_PRODUCT_BLOCKER |
| P07/O07 | Real retained-ingress effective-expiry gate | BA-A Kestrel/TLS + Agent + broker/PostgreSQL | 0 reads | alias/claim anti-reuse shell, no reservation/provider; terminal lease zeroized | BA-G2 outcome branch + BA-G3 terminal retry set + BA-G5 strict equality comparator | OPEN: comparator corrected and proven; independent `LEAST(...)` source selection remains incomplete |

## Shared scenario BA-A

`server pre-state → HTTPS host → auth prerequisite → real client → body expectation → response → rows`

Fresh isolated PostgreSQL + ratified raw-ingress seed → loopback Kestrel/TLS → already-authenticated context only → production `CaptureRuntimeHttpClient` → zero request-body/body-pipeline reads → exact outcome-only HTTP result. The fixture uses the production-valid capacity tuple `(perProducer=1, perDeployment=2, aggregatePerDeployment=2_097_152, perStream=1_048_576)` and matching `RuntimeOwners.MaximumPlaintextWindowBytesPerStream=1_048_576`. Authentication itself is outside these row claims.

P02 has two table-driven variants. `P02-missing` supplies an artifact absent from the graph. `P02-foreign` first creates a complete, real consent/capability/binding/acceptance graph for client B, then calls the public raw-ingress path as client A with B's exact artifact ID. Both variants must be indistinguishable on the wire: exact 403/O02, outcome-only JSON, no foreign artifact or binding identifier, zero body reads and byte-equal durable/provider state. A direct production SQL-reader control proves the foreign graph resolves for B but not for A.

## Horizontal phases

1. Correct the shared fixture capacity tuple without changing production.
2. Add the cross-client foreign-existing-artifact P02 variant and its production-reader controls.
3. Build once and run the three Server positive controls.
4. Run BA-G1, BA-G2 and BA-G4 as isolated current-source mutations; reuse the unaffected frozen BA-G3 Agent evidence.
5. Restore production byte-exact and rebuild once.
6. Run one restored Server 3/3 gate; retain the frozen predecessor 14/14 sentinel as historical coverage without rerunning it.
7. Freeze the failed-run census, mutant registry, manifest/drift and this single v2 review packet.

All six rows have a disposition. P02 is ratified. P07 keeps valid component evidence but remains open until the multi-source expiry calculation itself has a direct, discriminating proof.
