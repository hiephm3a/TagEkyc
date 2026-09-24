# A3 strict Expect transport and TagEkyc transport identity — bounded design v3

**Status:** DESIGN CORRECTION FOR INDEPENDENT REVIEW — IMPLEMENTATION HOLD  
**Date:** 2026-09-22  
**Predecessor:** `a3_transport_expect_fence_design_proposal_v2.md`; all unmodified HTTP grammar, ordering, F0–F8, and mutation requirements in v2 remain in force  
**Scope:** TagEkyc raw-ingress transport and its narrowly reusable TagEkyc TLS peer-identity prerequisite; no SignFlow code dependency  
**Governance:** activation-open census 7; four TRANSPORT-EXPECT-FENCE RowIds OPEN; F1_1_TO_1024 REAL_PRODUCT_BLOCKER; A3 HOLD; no activation authority

## Changelog and intent

This successor corrects the *identity prerequisite and proof boundary* of v2, and adds the four bounded review clarifications below. It does not approve an actual hospital endpoint, certificate, key custodian, pin, or topology. No product/test code, census, partition, backlog, manifest, activation seal, or production configuration is changed by this design.

The business-size contract remains **minimum 1 byte**. One fixed-`Content-Length` HTTP/1.1 POST remains the external operation. The Server rejects request `Transfer-Encoding`; neither chunking nor a 1025-byte minimum is an escape hatch. The specialized strict HTTP transport owns Expect/100, body release, bounded response parsing, and no automatic resend. A separate foundation, `TAG-TRANSPORT-IDENTITY-V1`, owns peer identity and deployment qualification. A3 is its first consumer, not its certificate-lifecycle owner.

## 1. Two different claims that must not be conflated

| Claim | Evidence/owner | Limit |
| --- | --- | --- |
| The TLS peer presents a chain- and hostname-valid certificate whose public key matches an approved SPKI SHA-256 | Agent runtime, using platform TLS checks plus an additive pin from a protected, approved profile | Proves possession of the matching private key; **does not prove** that the peer is the Kestrel process |
| Only the designated TagEkyc Kestrel serving workload may use that private key for this endpoint, and Agent reaches Kestrel directly without TLS termination in between | Approved deployment record, named custody/network owners, deployment qualification and change control | A signed/hash-bound assertion plus operational checks; not a fact discoverable by Agent from TLS alone |
| A parsed `100 Continue` follows durable Server B/R1 commit and precedes the first raw byte | Qualified peer/deployment **and** real Kestrel/PostgreSQL/Agent ordering proof | `100` by itself is not a commit receipt |

A reverse proxy or sidecar given the same certificate/private key can pass chain, hostname, and SPKI checks and still generate `100` before Kestrel commits B/R1. `UseProxy=false` and a direct socket prevent the Agent from *selecting* an HTTP proxy; they do not prove that the network contains no TLS terminator. The hospital's chosen route is direct Agent → Kestrel. The deployment half below is therefore mandatory for four-row closure, not optional documentation.

## 2. `TAG-TRANSPORT-IDENTITY-V1`: bounded machine-verifiable profile

The proposed profile is TagEkyc-owned and names a role, `CaptureRuntimeServer`. Its reviewed, immutable revision contains at least:

```text
ProfileId / ProfileRevision
Role = CaptureRuntimeServer
Exact HTTPS origin, canonical DNS hostname and port
Platform certificate-chain validation = REQUIRED
Platform hostname validation = REQUIRED
Allowed negotiated ALPN = exactly http/1.1
Current SPKI SHA-256 (exact 32-byte value)
Optional next SPKI SHA-256 (separately approved rotation value)
Approved deployment-record identity/revision/SHA-256
Profile provenance, validity window and authorized custodian
```

The Agent rejects missing, malformed, stale or mismatched profile/revision, unsupported ALPN, platform TLS failure, hostname failure, or a peer SPKI outside the explicit current/next set **before any HTTP header or raw byte is written**. A pin check adds a rejection *after* normal platform chain and hostname validation; it never replaces them, never uses an accept-all callback, and never learns a pin from the certificate on the very connection it is qualifying. No silent fallback to another hostname, HTTP version, pin, or system proxy is allowed. Rotation is a reviewed current/next overlap followed by a separately reviewed removal of the old pin; server-supplied pin data and first-use trust are forbidden.

The server certificate must be configured for the approved Kestrel endpoint and ALPN HTTP/1.1 must be observed on the real deployment. `UseHttpsRedirection` and a test certificate are not production identity configuration. An inability to negotiate exact HTTP/1.1 is a STOP, not permission to change Expect semantics.

### Existing enrollment channel: inspected, not assumed reusable

The existing `CandidateKeyId`, `PublicVerifierSpki`, and `PublicKeyThumbprint` are **Agent signing-key** fields. `CaptureRuntimeEnrollmentResponse` contains Agent identity/credential and revision fields, but no approved Server TLS SPKI or transport-profile revision. `CaptureRuntimeHttpClient.RedeemAsync` parses that closed response. The Agent's configured Server origin is not a pin. Consequently the existing enrollment channel cannot carry a Server pin *as it stands*, and fetching a pin across the unqualified TLS connection would be circular.

Before implementation dispatch, the transport-identity owner must approve an **out-of-band protected provisioning path** for the exact profile and pin, independent of the connection being qualified. Reusing an existing installation/package channel is preferred if its authority, integrity, confidentiality as needed, and rotation semantics can be demonstrated; this design does not silently add fields to the closed enrollment response or invent a second network enrollment call. If no approved provisioning path exists, implementation remains HOLD.

## 3. Approved deployment record: a machine-checked assertion, not a TLS discovery

`ApprovedTransportDeployment` is a distinct, Homeowner/deployment-security-approved record with exact, parseable fields:

```text
Decision = APPROVED
DeploymentRevision / validity window
ProfileId / ProfileRevision / profile SHA-256
Exact Agent-facing HTTPS origin, DNS hostname and port
Kestrel host/service identity and certificate identity/SPKI set
Certificate/private-key custody owner and exclusive authorization boundary:
  only the designated TagEkyc Kestrel serving workload may use the key
TLS termination location = TagEkyc Kestrel
Intermediary TLS termination = FORBIDDEN
Agent-to-Kestrel route = DIRECT; named network/operations owner
Current/next certificate rotation authority and change procedure
Approval identity, date and record SHA-256
```

The key may live in a local certificate store, protected secret mount, HSM/KMS, or equivalent reviewed custody mechanism; physical key-file co-location on the Kestrel host is not required. No reverse proxy, sidecar, load balancer, or other TLS terminator may possess **or exercise** the approved endpoint key under this deployment record. The record must state how named operators checked key-use authorization and the route at the hospital (for example, controlled key-access inventory and network/configuration inspection), with timestamp and evidence locators. A text assertion or SHA alone cannot prove the physical route or exclusive key custody. A missing, expired, contradictory, or unverified attestation is **not** positive topology evidence.

Expiry is an operational event, not a silent surprise at the first failed capture. The approved record must name the advance-warning interval, responsible on-call owner, alert destination and renewal procedure. The proposed default is alerts 30 days and 7 days before expiry, then daily until a successor record is approved; operations must see a distinct `TRANSPORT_DEPLOYMENT_ATTESTATION_EXPIRING` warning without patient data. At expiry, readiness fails closed with a distinct `TRANSPORT_DEPLOYMENT_ATTESTATION_EXPIRED` reason; renewal requires fresh human verification and successor approval, never an automatic date extension. The exact warning schedule is frozen with the first hospital deployment record, not inferred by the Agent.

The transport-identity readiness verifier must parse exact field/value pairs, reject duplicates/missing fields and unknown decision values, and compare the approved record's profile/revision/SHA, endpoint, pin set and topology against the actual deployment inputs. A reviewed manifest binds the exact record and profile bytes. Any certificate rotation, endpoint move, key replication, new terminator, route change, or change to bound provenance invalidates readiness until a successor approval. A new authority file does not automatically update this record or activation scope. The verifier must fail closed rather than self-approve or recompute an approval.

The Agent can machine-check its profile, certificate/ALPN and matching approved deployment-record identity/revision. Deployment control must independently verify that the live host/route still matches the approved record. Only the conjunction is a qualification. The existing A3 activation seal is a precedent for exact hash/revision binding, **not** evidence that this new transport record is already integrated with it. The implementation design must state exactly how the profile/record identity is delivered to Agent, how readiness is compiled or provisioned, and how drift blocks A3 before route/worker activation; that binding must be reviewed and tested before it is claimed. No runtime read of arbitrary Markdown is proposed.

## 4. Strict transport still owns HTTP, not certificate lifecycle

After `TAG-TRANSPORT-IDENTITY-V1` qualifies the connection, the A3 handler applies v2's closed HTTP/1.1 grammar: one reviewed POST, positive fixed `Content-Length`, `Expect: 100-continue`, bounded headers, no request or response chunking, no redirects/proxy/pooling, and exact fixed-length JSON finals. It does not implement a general HTTP stack. A final response before qualified `100`, including O02/O07, is consumed without touching retained raw content. An unknown or malformed 1xx, duplicate `100`, timeout, EOF, or TLS-identity failure never releases the body. When a complete final response is already buffered with `100`, the final takes precedence over body release. Deadline closes; it never sends.

The independent observer in the joined proof must see durable B/R1 **before** first raw copy into TLS, not infer order from timestamps or from a test flag. The broker, Expect transport, Agent and end-to-end RowIds each need their own named assertion; one RED for one assertion cannot be credited to all four. Tests using a synthetic TLS peer or topology are mechanism tests and cannot substitute for the approved hospital deployment record.

## 5. Ambiguous send states and operational classification

`CaptureAgentOrchestrator` already has `SubmissionStateUnknownException`, `CaptureAgentSubmissionTerminalStatus.SubmissionStateUnknown`, and `SUBMISSION_STATE_UNKNOWN` call sites. This proves an **existing semantic destination**, not that raw-ingress currently maps to it. The strict raw handler must preserve its internal send-stage and copied-byte count, then demonstrate that both `BODY_PARTIAL` and `BODY_COMPLETE_NO_FINAL` map to the existing unknown-result custody policy. Those two diagnostic stages deliberately **collapse into the same external `SubmissionStateUnknown` semantic**: neither allows automatic repost or a fabricated public O-code. If the current raw-ingress call chain cannot safely carry either stage to that policy, stop for a bounded contract decision; do not convert it to “definitely unsent”.

Failures need separate *internal operational* classifications, not new public O-codes:

```text
RAW_TRANSPORT_INTERIM_RESPONSE_INVALID
RAW_TRANSPORT_TLS_IDENTITY_INVALID
RAW_TRANSPORT_CONNECTION_LOST
RAW_TRANSPORT_DEADLINE
RAW_TRANSPORT_RESULT_UNKNOWN
```

The error surface and runbook must distinguish an unexpected `103`/`199` or duplicate `100` from network loss and an ordinary Server denial. Unsupported 1xx can make **all captures at a site fail closed at once**; operations must get a stable, actionable alert and a way to inspect the approved profile/deployment revision and correlation ID. Telemetry may contain only stable reason code, correlation, send stage and non-sensitive profile/deployment revision. It must not log patient raw bytes, plaintext or sensitive digest, signature, nonce, certificate private material, or body snippets. Qualification failure must never be retried as a raw-body resend.

## 6. Change-impact and proof gates

Execution remains **one shared mechanism matrix**, not four row-specific build/review cycles. Use the v2 F0–F8 lengths 1, 1024, 1025 and a representative permitted large payload with real Kestrel/TLS/PostgreSQL/production Agent. Required negative controls include early or auto `100`, buffering intermediary, no reply until deadline, unsupported 1xx, duplicate/malformed `100`, partial send and missing final. For each mutation, record the named affected assertion and adjacent GREEN controls; restore exact bytes and run one joined restored gate. Whole-repo TRX inventory and failed-run census include diagnostics and every resulting TRX, not only referenced filenames.

Affected-source ratified sentinels are selected from **two dependency sets** at freeze:

1. If `CaptureRuntimeHttpClient` or its Agent transport/identity policy changes, run current-byte sentinels for ratified Agent-client paths and custody/unknown-result behavior. The minimum candidate list in v2 §7 is a starting inventory, not an exemption for other dependencies.
2. If Server raw-response framing, authentication/authorization early response, outcome mapping, or middleware changes, independently enumerate and run current-byte sentinels for **all ratified HTTP-response-dependent paths**, including direct Server callers not using the Agent client. The Agent list cannot stand in for this wider Server set; the current ledger/test dependency scan determines it.

No unchanged ratified proof is rerun merely because another RowId exists. A changed production guard/source used by a ratified row does require its affected sentinel. Frozen predecessor TRX remains historical coverage only. Do not run a full suite until the eventual final A3 candidate, and do not stage, commit, push or activate production as part of this design correction.

### Two finish lines

**Strict HTTP mechanism DONE:** current bytes eliminate the 1..1024 early-body copy, independent B/R1-before-copy ordering is observed, F0–F8 and targeted mutations discriminate at the required boundary, partial/lost-final map safely, exact restore and joined gate pass, and both affected-source sentinel sets pass as applicable.

**Four TRANSPORT-EXPECT-FENCE RowIds CLOSED:** all strict HTTP conditions **plus** reviewed `TAG-TRANSPORT-IDENTITY-V1` provisioning and runtime checks, exact approved deployment record and operational qualification, verified drift/readiness behavior, and the separate broker/Expect/Agent/end-to-end assertions pass. Until then, all four remain OPEN, activation-open census remains **7**, and A3 remains **HOLD**. No row is closed by another row's mutation.

The four separate RowIds are `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, `BP03 BrokerCommitObservedBeforeFirstRawRead`, `BP13 ThreeExpectTransportMutationsFailQualification`, and `Agent raw HTTP Expect → durable server B/R1 before first body byte`. The proof must report coverage and disposition for each. A test-only authenticator or a synthetic deployment record does not establish production authentication or hospital topology.

## 7. Decision and implementation STOP gates

Before code dispatch, independent review must approve (a) the protected out-of-band provisioning owner/path for the Server SPKI/profile; (b) who signs and checks exclusive key custody/direct topology and how the exact deployment record is bound to readiness; (c) the bounded Server response-framing impact set; and (d) the raw-ingress mapping to `SubmissionStateUnknown`. These are named missing decisions, not implied by this proposal. If one cannot be made, keep the implementation HOLD and return to the Homeowner rather than weakening payload size, framing, TLS validation, or the one-operation contract.

Non-goals: mTLS, generic PKI, ACME automation, a SignFlow–TagEkyc shared library, proxy/service-mesh support, two-operation admission, request chunking, and production activation. This document is not an approval to provision a certificate or implement the handler.
