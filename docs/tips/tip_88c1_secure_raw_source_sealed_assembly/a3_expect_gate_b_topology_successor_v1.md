# A3 Gate B topology qualification — successor clarification

Status: DEVELOPMENT RULE ONLY. This document does not attest a hospital deployment, ratify an Expect row, change the six-row activation partition, or re-mint the activation seal. A3 remains HOLD.

## What Gate B qualifies

The invariant is not “no proxy by name.” It is: the peer that sends `HTTP/1.1 100 Continue` to the Agent must not release the raw body before the Server's B/R1 transaction has durably committed. The Agent's normal CA/name TLS validation proves the certificate identity of its TLS peer, not that the peer is the Kestrel workload or that it waited for Kestrel's B/R1. The F3 controlled intermediary test demonstrates that a trusted TLS terminator which synthesizes `100` can violate this ordering.

The deployment record must identify the Agent's exact HTTPS origin, the serving Kestrel deployment revision, where TLS terminates, every intermediary's role, and which workload can exercise the endpoint certificate key. The record is evidence about the actual installation, not a fixture-generated assertion. Local tests can qualify a *candidate configuration* but cannot certify an uninspected hospital route.

## Supported topology candidates

| Candidate | Development qualification | Deployment evidence still required |
| --- | --- | --- |
| Direct Agent → Kestrel TLS | Existing strict Agent/Kestrel/PostgreSQL F0 proof and negative transport mutations. | Verify that the installed origin reaches the stated Kestrel workload and that no device on that route terminates TLS or emits HTTP responses. |
| Layer-4 TCP/TLS passthrough | Run the same F0 ordering proof through the actual passthrough configuration. The device must not terminate TLS, parse HTTP, perform application-level request prebuffering/replay, or generate `100`. Ordinary TCP/network buffering is not itself a violation. | Verify the installed forwarding configuration and endpoint key-use boundary. “Load balancer” is not by itself a rejection. |
| Layer-7 TLS-terminating proxy | **Not qualified by the current F0 proof.** Test the exact proxy product/configuration with an independent PostgreSQL observer and probes at Agent, proxy ingress/egress and Kestrel. Hold B/R1 before commit; prove the proxy does not synthesize/originate an early `100` or relay `100` before downstream Kestrel has authorized it after durable B/R1. Only then may the proxy relay Kestrel's `100` to the Agent and allow raw body into application buffers or forward it; no hidden retry or second raw POST. Verify final/error paths, fixed response framing and configuration drift controls. F3 early-`100` behavior must fail qualification. | Identify its certificate/key-use authority and bind the exact validated proxy configuration to the deployment record. CA/name validation alone does not qualify it. If it cannot preserve the one-operation fence, select direct/L4 topology or seek a separately approved protocol change. |

This permits a conforming proxy design; it does not declare an arbitrary TLS terminator safe. A transparent test proxy is a control, not evidence about a different hospital proxy. No production configuration may silently move between these categories while reusing a prior qualification.

## Development versus activation

P04 and P05 technical work and the current direct-Kestrel mechanism tests may proceed without a hospital installation signature. The four `TRANSPORT-EXPECT-FENCE` RowIds remain individually open under the present activation decision until their exact row evidence and the chosen deployment topology are reviewed. Gate B cannot be satisfied by another local mutation or by the Homeowner's topology intention alone. It requires qualification of the **actual** deployment configuration before Activated is authorized there.

Current activation-open census: **6** (P04, P05, four Expect rows). This successor does not edit the frozen predecessor packets, partition, reconciliation ledger, ratification record, or format-2 revision-4 seal.
