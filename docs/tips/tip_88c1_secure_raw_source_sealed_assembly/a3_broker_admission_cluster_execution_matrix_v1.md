# A3 BROKER_ADMISSION cluster execution matrix v1

Scope: exactly P02–P07. Incoming official census: **56 normative + 1 seam = 57**. No full suite, landing or production activation.

| Row | Production reachability | Shared scenario | Body | Residue / Agent | Guard | Candidate disposition |
|---|---|---|---|---|---|---|
| P02/O02 | Real zero-row bound read in `RawIngressBrokerTransactionFacade` | BA-A Kestrel/TLS + Agent + broker/PostgreSQL | 0 reads | all durable/provider families unchanged; terminal lease zeroized | BA-G1 projection + BA-G3 terminal retry set | PASS pending review |
| P03/O03 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P04/O04 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P05/O05 | No production emitter located | source audit only | — | synthetic Final prohibited | — | SEMANTIC_BLOCKER |
| P06/O06 | Actual-excess R2 emitter exists; declared-excess pre-body emitter cannot receive ratified per-class maxima | prior frozen R2 component + current source audit | declared path absent | actual component does not close declared path | — | SEMANTIC_PRODUCT_BLOCKER |
| P07/O07 | Real retained-ingress effective-expiry gate | BA-A Kestrel/TLS + Agent + broker/PostgreSQL | 0 reads | alias/claim anti-reuse shell, no reservation/provider; terminal lease zeroized | BA-G2 SQL outcome + BA-G3 terminal retry set | PASS pending review |

## Shared scenario BA-A

`server pre-state → HTTPS host → auth prerequisite → real client → body expectation → response → rows`

Fresh isolated PostgreSQL + ratified raw-ingress seed → loopback Kestrel/TLS → already-authenticated context only → production `CaptureRuntimeHttpClient` → zero request-body/body-pipeline reads → exact outcome-only HTTP result → P02/P07. Authentication itself is outside this row claim.

## Horizontal phases

1. Complete six-row production reachability audit.
2. Add one shared Server theory and one shared Agent-custody theory.
3. Build and run positive controls: Server 2/2; Agent 2/2.
4. Run BA-G1, BA-G2, BA-G3 isolated current-byte mutations.
5. Restore byte-exact and rebuild.
6. Run restored Server 2/2 and Agent 2/2.
7. Freeze failed-run census, mutant registry, manifest/drift and one review packet.

No scenario remains unprocessed. Four are explicit blockers; two are candidate-PASS.
