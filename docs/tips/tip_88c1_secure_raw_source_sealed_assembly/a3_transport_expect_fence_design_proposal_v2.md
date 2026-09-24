# A3 strict Expect transport — design proposal v2

**Status:** PROPOSED FOR INDEPENDENT DESIGN REVIEW — NOT IMPLEMENTED; NOT AN IMPLEMENTATION DISPATCH  
**Date:** 2026-09-22  
**Baseline:** `a3_transport_expect_fence_design_proposal_v1.md`; current TagEkyc and CaptureAgent sources  
**Scope:** one raw-ingress POST over direct TLS to Kestrel; no change to the control-plane client, payload domain, broker/R1 semantics, or external operation count  
**Governance:** activation-open census 7; A3 HOLD; all four TRANSPORT-EXPECT-FENCE rows OPEN; F1_1_TO_1024 remains REAL_PRODUCT_BLOCKER

## Changelog

### v2 — design correction only

- Constrained the request writer and response parser to closed HTTP/1.1 grammars and chose fixed `Content-Length` for every recognized raw-ingress final JSON response; no chunked decoder.
- Split strict body-release transport from a **separate, missing Agent-to-Kestrel TLS identity prerequisite**. Specified the proposed identity policy without claiming it exists.
- Pinned ALPN, post-`100` send states, final/timeout behavior, affected ratified sentinels, and distinct mechanism-versus-four-row acceptance gates.
- Recorded risks and a test oracle for the already-existing broker-before-body ordering. No source code, tests, census, partition, backlog, manifest, or seal were changed by this document.

## TIP Analytical Summary / Intent Ledger

### Intent and expected outcome

Preserve the one-byte minimum and one external HTTP operation while preventing the production Agent from handing **any** raw body byte to TLS before a qualified `100 Continue`. This is a design for the measured F1 defect: at a final pre-body error, .NET 8 `SocketsHttpHandler` copies a known-length body of at most 1024 bytes despite the infinite Expect timeout. The observed 1024-byte copy was into the handler-provided transport stream; it was **not** a packet capture or proof of TLS-socket arrival. The 1025-byte control did not copy. The current raw route rejects `Transfer-Encoding` and requires positive `Content-Length`, so neither raising the minimum to 1025 nor switching to chunked is a permitted repair.

### Accepted design choices and non-claims

| Choice | Scope | Non-claim |
| --- | --- | --- |
| One fresh direct TLS connection and one fixed HTTP/1.1 POST per raw operation | Raw-ingress transport only | Not a general HTTP client or a second admission request |
| Platform `SslStream`, platform chain/hostname checks, exact ALPN `http/1.1` | TLS mechanism | Not proof of the missing Kestrel-specific peer identity |
| Only exact qualified `100 Continue` releases body | Strict send state | `100` alone is not durable-R1 proof without the server ordering and peer-identity prerequisites |
| Fixed-length final JSON framing | Raw-ingress application response paths | Does not change status, outcome code, JSON business shape, broker/R1 state, or permit chunked requests |
| Close on deadline, never send on deadline | All pre-`100` paths | No timer fallback or automatic resend |

### Deferred gate

Peer identity and deployment topology are a **named separate prerequisite**, `A3-KESTREL-TLS-IDENTITY`. Transport implementation may be evaluated as a mechanism before this gate closes, but no TRANSPORT-EXPECT-FENCE RowId may be ratified from transport proof alone. This document does not provision certificates, pins, deployment endpoints, or production activation.

## 1. Current facts and the exact defect

`CaptureRuntimeHttpClient.SubmitRawExportSourceAsync` constructs a single POST to `/api/ekyc/raw-export/source-ingress`, sets `ExpectContinue = true`, prohibits chunked, attaches the retained raw content, and calls `rawHttp.SendAsync`. Its default raw handler is `SocketsHttpHandler` with `Expect100ContinueTimeout = InfiniteTimeSpan`, `UseProxy = false`, and no redirects/cookies/decompression. The Agent constructor requires an HTTPS origin. These facts do **not** establish that a particular trusted-cert peer is the hospital Kestrel process, nor do they exclude an external TLS-terminating intermediary.

The Server API currently calls `app.UseHttpsRedirection()`; no production Kestrel HTTPS certificate/endpoint identity configuration is established by that line. A test Kestrel certificate is not a production peer-identity policy. A direct Agent-to-Kestrel deployment was specified by the Homeowner, but operational intent is not yet a machine-enforced identity proof.

The 1..1024-byte defect is a real product blocker, not an invalid-size test. The minimum is **1 byte** and the size-validity of 1..1024 bytes must not change. The new transport must be independent of .NET's 1024-byte Expect threshold. No `length <= 1024` rejection, request chunking, permissive certificate validation, or two-operation preflight is allowed in this design.

## 2. Existing B/R1-before-body architecture — preserve and confirm

`CaptureRuntimeRawIngressAdmissionService.AdmitAsync` awaits `broker.AdmitAsync(context, ...)` before it can call `bodyPipeline.ProcessAsync(context, handoff.Value, body, ...)`. A broker `Final` returns without entering that body pipeline. In the current ASP.NET path, the first application body read belongs to `ProcessAsync`; Kestrel's automatic `100 Continue` is produced on a body read when `Expect: 100-continue` is present and the final response has not started. Kestrel also completes the final response before automatic body drain when the application did not read the body. Thus the intended ordering is already architectural, not a new B/R1 check to add:

```text
broker.AdmitAsync -> durable B/R1 commit -> Handoff -> first ProcessAsync body read
                                                 -> Kestrel 100 -> Agent body release
```

The implementation proof must **confirm** this order on the actual deployed .NET/Kestrel runtime, not re-derive it solely from source or timestamps. At a pause point after the strict client parses `100` but before it copies content, a separate PostgreSQL observer must see the committed B/R1. Rollback must produce no committed R1, no Server application body read, and zero Agent body-copy calls. The test must also assert that no middleware, authenticator, alternate route, or drain path reads the body before the broker. The test-only authentication premise does not prove production authentication.

Source anchors: `CaptureRuntimeRawIngressAdmissionService.cs` (broker call before `bodyPipeline.ProcessAsync`), [Kestrel v8.0.0 `HttpProtocol.ProduceContinueAsync`](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Servers/Kestrel/Core/src/Internal/Http/HttpProtocol.cs), and the current real Kestrel/TLS/PostgreSQL F0/F1 test matrix. The source explains the mechanism; the runtime observer is the acceptance oracle.

## 3. Narrow raw request grammar — reject before any socket write

The specialized `HttpMessageHandler` is an API adapter, **not** a serializer for arbitrary `HttpRequestMessage`. It validates a fixed request, then writes only the following reviewed fields in a deterministic order. It must not forward `request.Headers` wholesale or accept another path/method/content owner.

```text
POST /api/ekyc/raw-export/source-ingress HTTP/1.1\r\n
Host: <the exact approved HTTPS origin authority>\r\n
Content-Type: image/jpeg\r\n
Content-Length: <one decimal integer, 1..permitted class maximum>\r\n
Expect: 100-continue\r\n
Connection: close\r\n
<the ten canonical RawIngressMetadataHeaders fields>\r\n
X-TagEkyc-Plaintext-Sha256: <canonical digest>\r\n
<the five X-TagEkyc-Capture-Runtime-{Credential-Id,
  Credential-Generation,Timestamp,Nonce,Signature} CRT1 fields>\r\n
\r\n
<NO BODY YET>
```

The ten metadata names are those returned by production `CaptureRuntimeWireCodec.RawIngressMetadataHeaders`: Agent-Configuration-Revision, Verification-Session-Id, Capture-Artifact-Id, Capture-Revision, Raw-Class, Idempotency-Key, Captured-At-Utc, Retention-Started-At-Utc, Retention-Expires-At-Utc, and Retention-Budget-Seconds, with their existing exact prefixes. The signed CRT1 preimage and content object remain produced by the current owner; the new handler neither re-signs nor buffers a copy of raw plaintext.

Pre-write validation rejects: non-HTTPS or non-approved origin; userinfo; query/fragment or different path; method/version other than POST/HTTP/1.1; missing/duplicate/noncanonical Host, Content-Length, Expect or Content-Type; any `Transfer-Encoding`, `Upgrade`, `Proxy-*`, `Content-Encoding`, `Trailer`, authentication-challenge or unknown raw-ingress header; CR/LF/control bytes in values; and any metadata/content-length mismatch. `Host` is generated from the approved origin, not taken from untrusted caller text. `Connection: close` is generated exactly once. A rejected request opens no socket and cannot touch the retained body.

The overall response parser is also a **closed list**, not an HTTP stack: write this one fixed request; read one bounded status line; read a bounded header section through the blank line; optionally read exact `100 Continue`; then read one small fixed-length final JSON body. There is no request chunking, response chunked decoding, redirect, cookie, keep-alive, pooling, decompression, proxy support, HTTP/2, or HTTP/3. Anything outside this list fails closed.

Proposed resource limits to freeze in implementation review: 2 KiB status line, 8 KiB total headers, 32 header fields, and the existing 65,536-byte maximum response body. Lines and total header bytes are counted before allocation growth; deadline applies through connect, TLS, header wait, body send, and final read. These are implementation bounds, not raw-payload size limits.

## 4. TLS and Kestrel peer identity — separate prerequisite

The raw transport uses the platform `SslStream` and **retains** platform certificate-chain and exact hostname validation. It must never accept any certificate via a permissive `RemoteCertificateValidationCallback`; a future pin check may only add a rejection on top of successful platform validation. TLS must advertise only ALPN `http/1.1` and reject a connection whose negotiated protocol is absent or different. Merely writing an HTTP/1.1 request line is not enough. If the real Kestrel deployment does not negotiate exact `http/1.1`, that is a STOP for this design, not permission to fall back to HTTP/2, HTTP/3, or unnegotiated HTTP/1.1.

`A3-KESTREL-TLS-IDENTITY` must separately establish the production endpoint and certificate lifecycle, exact hostname, normal system validation, and an **approved Kestrel SPKI SHA-256 identity set** (current plus explicitly approved next identity for rotation). A direct socket with no proxy plus `UseProxy=false` is insufficient by itself: a TLS-terminating network device with another host-trusted certificate can otherwise generate `100` before the Server's B/R1. The pin source, rotation authority, protected configuration, failure semantics and actual no-terminator topology need their own review and production evidence. This proposal neither builds that mechanism nor claims it already exists.

The identity and ALPN controls are *additional prerequisites* for attributing the received `100` to qualified Kestrel. Until they pass, positive transport tests are component/mechanism evidence only, never four-row closure.

## 5. Response grammar and fixed-length framing choice

Choose **option A**: every recognized raw-ingress application final response, including pre-`100` denials and post-body outcomes, is bounded JSON with one exact `Content-Length`; the Agent rejects `Transfer-Encoding` and never implements a chunked decoder. A later implementation may need a narrow Server response-framing change because current `Results.Json` paths do not, by themselves, establish a fixed-length wire response. That change must cover the raw route's outcome mapper and early authenticated/authorization/runtime-error paths. It may change framing only—not status, outcome code, JSON field shape, or broker/R1 semantics. A framework-generated error outside the recognized contract is an untyped fail-closed transport error, not a fabricated O-code or permission to resend. If the necessary recognized responses cannot be made fixed-length, stop for design review rather than silently add response chunking.

Interim grammar is **exactly** `HTTP/1.1 100 Continue` followed by the blank line, within the header bounds. No body or other interim semantics are accepted. `101`, `102`, `103`, `199`, `HTTP/1.0 100`, malformed 1xx, duplicate/extra observed `100`, or any interim content fails closed. A valid final response uses `HTTP/1.1 <allowed status>`; exactly one canonical numeric `Content-Length` from 1 through 65,536; exactly one `Content-Type` whose value is `application/json` or `application/json; charset=utf-8`; no `Transfer-Encoding`, content compression or conflicting length; bounded JSON read for precisely that many bytes; and an allowed status/outcome pair validated by existing `ParseRawIngressResult`. Optional bounded non-framing headers such as `Date`, `Server` and `Connection` cannot change this interpretation. The parsed exact-length body is the complete response for this fresh `Connection: close` exchange; the client then closes the connection. It does not wait for unbounded EOF or silently reinterpret extra bytes. An extra byte already prefetched beyond that body makes the response invalid.

On a **final response before `100`**, read the entire declared final body, close the connection, and do not call or copy raw content. The Agent must still receive exact O02/O07/etc. to apply its existing recapture/retry policy. Failure to parse the final body is fail-closed and cannot turn into an invented outcome. If a read returns `100` and a complete or partial final response in the same buffer, preserve the extra bytes and **prioritize parsing the final response before any body release**; do not discard them or send the body. A duplicate `100` observed before release fails closed. A later contradictory message after release cannot retroactively unsend bytes and is treated as ambiguous, never as an automatic-retry signal.

## 6. Explicit send states and ownership

| State / event | Body-copy count | Required behavior |
| --- | ---: | --- |
| Before headers/TLS failure | 0 | Fail closed; no external request body or auto retry |
| `HEADERS_SENT_NO_100` | 0 | Wait for exact `100` or fixed-length final; deadline/EOF/malformed reply closes without sending |
| `BODY_RELEASED` after exact qualified `100`, before first copy | 0 | Pause-point observer can inspect committed R1; a buffered final takes precedence over copying |
| `BODY_PARTIAL` (`0 < copied < declared`) | Partial | Close on failure; surface explicit ambiguous-send state to existing custody policy; never automatic resend |
| `BODY_COMPLETE_NO_FINAL` | Exactly declared | Close on lost/truncated final or deadline; preserve explicit unknown-result state and original lease policy; never automatic resend |
| `FINAL_RECEIVED` | 0 or exactly declared | Deliver only a fully parsed, shape-valid existing Agent result; close after the exact final body |

The handler must retain whether `100` was observed and the exact number of content bytes copied. A generic `IOException`, cancellation or deadline must not cause an upper layer to infer “request definitely not sent” after release. The design does not invent new replay authority: `BODY_PARTIAL` and `BODY_COMPLETE_NO_FINAL` flow into the existing typed ambiguous-response/retained-lease policy, with no second automatic POST. If existing types cannot carry this distinction, implementation must stop and bring that contract gap for review before coding around it.

**Deadline CLOSES; it never SENDS.** The prior `Expect100ContinueTimeout` behavior could send on timeout; the strict handler's timeout cancels and closes with zero content copies while in `HEADERS_SENT_NO_100`. The planned named test anchor is `F4_NoResponseUntilDeadline_NoBodyWrite`, paired with a timeout-fallback mutation that must make only that ordering assertion RED. This must not be reintroduced as a liveness “fix”.

## 7. Proof matrix and two distinct finish lines

One shared Kestrel/TLS/PostgreSQL/production-Agent matrix uses non-patient synthetic payload lengths **1, 1024, 1025 and one representative permitted large length**. It records TLS/ALPN identity result, first interim/final response, independent B/R1 state, first Agent content-to-TLS write, first Server application body read, state/lease ownership and durable residue. A test-only intermediary or authenticator is declared as such; neither can prove production authentication or deployment identity. No timestamp-only ordering claim is sufficient.

| Scenario | Discriminating oracle |
| --- | --- |
| F0 success, all four lengths | Hold B/R1; zero body copy; commit visible to separate PG observer; client receives exact `100` and **pauses before copy**; observer confirms committed R1; release sends exact body once; Server first-read occurs afterward |
| F1 rollback/final error, all four lengths | Broker B/R1 attempt rolls back; final fixed-length response is parsed; committed R1 absent; Agent body-copy count and Server application body-read count both zero, especially 1024 versus 1025 |
| F2 buffering intermediary | Topology/identity qualification fails, not merely an outcome-code assertion |
| F3 early intermediary `100` | Identity qualification fails before body release; no content copy |
| F4 silent origin through overall deadline | Connection closes; zero body copy, no timer-triggered send |
| F5 `103 Early Hints` / other unsupported 1xx | Fail closed with zero body copy |
| F6 malformed or duplicate observed `100` | Fail closed before release; if a duplicate arrives after release, report ambiguity, do not claim unsent bytes |
| F7 exact `100`, then connection loss during body | `BODY_PARTIAL`, no automatic resend; retained lease/custody policy preserved |
| F8 exact `100`, full body, lost final | `BODY_COMPLETE_NO_FINAL`, no automatic resend; no invented O-code |
| Final before `100`, including O02/O07 | Read complete fixed-length JSON, parse exact status/outcome, close with zero body copy |

Mutations are grouped by mechanism, not RowId: eager copy before `100`; timeout-to-send fallback; bypass peer/ALPN qualification; accept early/intermediary `100`; accept unsupported 1xx/duplicate; and collapse partial/lost-final into definitely-unsent retry. Each mutation must RED at its named assertion with adjacent controls GREEN. A transport mutation that only makes one of four row tests RED proves only that covered dimension. Restore exact source bytes, rebuild once, run the joined restored gate plus affected-source ratified sentinels, then one whole-repo TRX/failed-run census, manifest/drift/mutant sweep and one mechanism packet. No row is closed because a sibling passes.

**Transport mechanism DONE** only when the <=1024 body-on-final-error write is gone on current bytes, a current-byte mutation is discriminating at the same 1024/1025 boundary, restoration is byte-exact, the joined restored gate passes, and ratified rows depending on `CaptureRuntimeHttpClient` do not regress.

**Four RowIds CLOSED** only when that transport mechanism **and** `A3-KESTREL-TLS-IDENTITY` both pass, and each RowId's separate broker, Expect-transport, Agent-side, or end-to-end assertion is satisfied. The retained four are `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, `BP03 BrokerCommitObservedBeforeFirstRawRead`, `BP13 ThreeExpectTransportMutationsFailQualification`, and `Agent raw HTTP Expect → durable server B/R1 before first body byte`. Until then all four remain OPEN and the activation census stays seven.

### Affected-source ratified sentinels if `CaptureRuntimeHttpClient` changes

Recalculate the exact dependency set from current ledger/test references at implementation freeze. The existing **ratified rows with an explicit production-Agent-client HTTP leg** are, at minimum:

- `BP07 ExistingMatchReadsNoBodyAndNoNewKey`; `BP11 AvailableOnlyAfterPublicationCommit`.
- `P02/O02`, `P03/O03`, `P08/O08`, `P09/O09`, `P10/O10`, `P12/O12`, `P13/O13`, `P14/O14`, `P15/O15`.
- `P17/O17`, `P18/O18`, `P19/O19`, `P20/O20`, `P21/O21`, `P22/O22`.

These map chiefly to `Tip88C1C6BA3BrokerAdmissionClusterTests`, `Tip88C1C6BA3BrokerClaimClusterTests`, and `Tip88C1C6BA3R2R6ClusterHttpTests`; `AvailableHttpResponseWaitsForR5PublicationCommit` is the named BP11 control. The accepted joined Agent result-shape/no-egress controls in `Tip88C1C6BA3ClientServerAcceptanceTests` are adjacent sentinels too, but are not relabeled as four-row Kestrel/Expect proof. `BP16` durable verifier and `BP17` ciphertext-failure tests are not claimed to depend on the Agent HTTP implementation merely because they were in an earlier joined gate. **Standing rule:** a changed hash in any ratified row's actual proof dependency set requires that row's affected-source sentinel to rerun; a frozen predecessor TRX is not current-source regression proof.

## 8. Residual risk and STOP gates

This is a hand-written HTTP/1.1 client on a patient-biometric plaintext path, not a trivial handler swap. Accepted *bounded* residuals: fresh-connection overhead; strict rejection of responses a generic client might accept; platform TLS certificate/ALPN/library defects outside this parser; and the fact that a post-`100` connection loss may be ambiguous. Bounds are the exact grammar, platform TLS, additive identity pin, small fixed response, explicit send states, no reuse/retry, and current-byte negative proof.

Not accepted: raw body copied before qualified `100`; a 1024-byte size exception; unbounded header/body allocation; unknown request headers; request or response chunking by inference; accepting a merely host-trusted TLS terminator as Kestrel; permissive certificate callback; HTTP/2/3 or ALPN fallback; a timeout that sends; automatic resend after any partial or unknown result; disclosure in logs; or closure of four rows from one red assertion.

**STOP and return for Homeowner/independent design decision** if exact ALPN or identity cannot be deployed, recognized final JSON cannot be made fixed-length, typed partial/lost-final handling cannot preserve custody, or the single-operation contract cannot meet the fence. A second external HTTP operation, a new 1025-byte minimum, accepting request `Transfer-Encoding`, and any product code change are outside this design-only round. No test, mutation, full suite, staging, commit, push, seal re-mint or production activation is authorized here.
