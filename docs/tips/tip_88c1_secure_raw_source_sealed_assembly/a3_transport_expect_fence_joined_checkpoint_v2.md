# A3 Expect fence — F3 and retry successor checkpoint v2

Status: TECHNICAL CHECKPOINT / GATE B OPEN. Date: 2026-09-22. This succeeds `a3_transport_expect_fence_joined_checkpoint_v1.md`; it does not ratify any RowId, change the seven-row activation partition, or re-mint the seal. A3 remains HOLD.

## F3: a TLS intermediary can release body before Kestrel B/R1

The new test `ExpectFence_IntermediaryCannotQualifyBySendingItsOwnContinue` uses the public Agent client constructor and production `StrictRawIngressTransport`. A test-only TLS intermediary presents a platform-trusted certificate, forwards the real request headers over TLS to Kestrel, and permits Kestrel's production broker to reach a PostgreSQL B/R1 transaction held before commit. An independent database connection confirms no durable B/R1. The intermediary then either relays the real origin's responses or, in the negative topology mutation, sends its own syntactically valid `HTTP/1.1 100 Continue` before Kestrel can authorize release. Its first read of the Agent's raw body is checked against the independent committed-state query.

| Invocation | TRX SHA-256 | Result |
| --- | --- | --- |
| Transparent forwarding (no synthetic `100`) — `a3-expect-f3-transparent-baseline-20260922113318-1.trx` | `00A9891427D09A3609767410164424D4E12B69EDDD25E7D9043BD7617FE5B59F` | 1/1 PASS: no body while B/R1 is held; at intermediary's first body read B/R1 is committed. |
| Intermediary-generated early `100` — `a3-expect-f3-auto-continue-red-20260922113451-1.trx` | `6690155076959C4FC3B9F413A8FBB76568696566FD2C690F1D953533784036F2` | 0/1 intended RED: `An intermediary-generated 100 released raw body before durable B/R1.` The origin B/R1 transaction had reached the held pre-commit point. |
| Restored joined direct/rollback/F3 baseline — `a3-expect-f3-restored-joined-20260922113658-1.trx` | `203FF0E17546AB3AB2E4679A0A497E5133904E7F91B2CD61A058EE6607653563` | 6/6 PASS, zero skip. Covers 2 direct joined scenarios, 3 rollback sizes (1/1024/1025), and F3 transparent forwarding. |

The RED is an **invalid-topology diagnostic**, not a product-source mutation and not a claim that the Agent can identify a trusted TLS terminator. It demonstrates the limit of CA/hostname validation: a TLS peer with the approved endpoint certificate can emit `100` without Kestrel's durable B/R1. The two other named BP13 negative dimensions remain the predecessor finite-fallback RED and buffering-intermediary RED in `a3_transport_expect_fence_execution_matrix_v1.md`; F3 does not substitute for them. All three are now represented, but BP13 remains OPEN because the actual hospital direct-Kestrel topology is not independently qualified.

## Ambiguous send through orchestrator

The current Agent focused test `A3_AmbiguousRawSendStaysUnknownOnExplicitRetainedRetry` passed 2/2 (`a3-expect-orchestrator-ambiguous-baseline.trx`, SHA-256 `4B08A977A283FEBDE716293509C137F81017E0B579E12D7F029400CB58A68110`): `BodyPartial` and `BodyCompleteNoFinal` both produce `SUBMISSION_STATE_UNKNOWN`; `CaptureAgentOrchestrator.RetryRetainedSubmissionAsync()` leaves the raw client call count at one. The v1 joined lost-final case separately counted one raw POST at real Kestrel and its targeted owner-guard mutation made only that case RED with POST count two. These are complementary current-source checks, **not** a claim that one test has run the full orchestrator through Kestrel after lost final. No orchestrator or owner production bytes changed in this successor.

## Current bytes and execution limits

- Joined test source SHA-256: `2D55C82D884BFC02ACE7B6EC5B7934E2D35D86ECF67D73AC330A6775B279149B`.
- Server admission restored/current SHA-256: `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C`.
- Agent strict transport remains `1FA123539BE8F1EE02D44F24BAA0F484E4B47766A4172A7341B77CDCC7FEA8B8`; retained owner remains `B2AD6803334F32B9C498204F051D2C4B720E77403377D6BE41765961A5663AC1`; orchestrator remains `579881AB060538AF08387597FCD0EDD1BE6B93099EFA679A00FF0356AF432D11`.
- Linux runner (`Run-A3IsolatedTlsLinux.ps1` and `.sh`) used a container-local CA/CRL with `SslStream` revocation still `Online`; no Windows trust-store import or user click. Parent cleanup removed the dedicated PostgreSQL container and network after both normal and intended-RED runs.
- This is not a full immutable whole-repo TRX census. The v1 disclosed overwritten early Linux diagnostic remains a provenance limitation. The new F3 RED is classified here as `EVIDENCE_RED_TEST_TOPOLOGY`; no new failed run is left unclassified within this successor.
- Current API build deliberately reports `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` and yields an invalid seal while source bytes differ from the prior approved manifest. That is fail-closed. No production activation or seal re-mint is authorized here.

## Gate B: evidence still unavailable locally

The Homeowner stated the intended hospital topology is direct Agent → TLS Kestrel. The repository and local Kestrel test can verify product behavior under that topology; neither can attest the **deployed** hospital route, certificate/key-use boundary, or absence of a TLS-terminating reverse proxy, load balancer or sidecar. Gate B requires an operationally approved record tied to the actual endpoint and deployment revision, with an independent check of the live route and the party responsible for private-key access. Do not fill that record from this test fixture, a diagram alone, the Agent's CA-valid certificate, or this packet.

The site record must identify the exact Agent destination origin (DNS/IP and port), Kestrel serving workload and deployment revision, endpoint certificate identity and who can exercise its private key, network route and any intermediary inventory, the independent live-route/configuration check and its evidence location, validity window, responsible verifier and approver. It must explicitly state whether any proxy/LB/sidecar terminates TLS or can emit `100 Continue`/buffer the body. Secrets, patient raw bytes and private-key material do not belong in the record. A changed route, endpoint certificate-use boundary or TLS termination location requires renewed qualification; an unsigned or expired record is not positive evidence.

Disposition by RowId remains unchanged: `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, `BP03 BrokerCommitObservedBeforeFirstRawRead`, `BP13 ThreeExpectTransportMutationsFailQualification`, and `Agent raw HTTP Expect → durable server B/R1 before first body byte` are all **OPEN**; activation census **7**, A3 **HOLD**. Before ratification, resolve Gate B and perform row-specific final review on current bytes; do not infer four closures from one shared RED.
