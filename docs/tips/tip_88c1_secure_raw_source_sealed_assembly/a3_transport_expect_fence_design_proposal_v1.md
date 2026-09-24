# A3 strict Expect transport — design proposal v1

Status: PROPOSED FOR REVIEW, NOT IMPLEMENTED. Date: 2026-09-22. No row closure, no activation authority. A3 remains HOLD; activation-open census remains seven.

## Decision and defect boundary

The Homeowner retains a raw-payload minimum of **one byte**. Size-valid 1–1024-byte payloads may be rejected for other documented reasons, but may not be excluded solely to evade .NET's `Expect100ErrorSendThreshold`. The current one-operation Agent path uses `SocketsHttpHandler` with infinite `Expect100ContinueTimeout`, known `Content-Length`, and direct TLS to Kestrel. On a final error before a `100 Continue`, .NET 8 deliberately sends a known payload of at most 1024 bytes. The F1 real PostgreSQL rollback test measured a completed Agent-side content copy at 1024 bytes and no copy at 1025 bytes; committed R1 and Server application body reads were both zero. Thus `F1_1_TO_1024 = REAL_PRODUCT_BLOCKER`. A completed copy into the handler stream is not itself a packet capture.

Primary runtime source: https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/HttpConnection.cs (`Expect100ErrorSendThreshold`, final-response branch). The current Server rejects chunked requests and requires `Content-Length > 0` in `RawExportSourceIngressEndpoints.TryParse`; changing framing or imposing minimum 1025 is outside this proposal.

## Narrow implementation surface

Replace **only the production raw-ingress HTTP transport**, leaving the control-plane `HttpClient`, CRT1 preimage/signature, retained-buffer owner, endpoint, broker and response outcome parser unchanged. A specialized `HttpMessageHandler` can preserve `CaptureRuntimeHttpClient.SubmitRawExportSourceAsync`'s request/response API while owning one HTTP/1.1 exchange directly over a fresh TCP/TLS connection. It must accept only the configured HTTPS origin and the single raw-ingress POST; it is not a general-purpose HTTP client.

State machine for the one external operation:

1. Validate the fixed origin, TLS server identity, method/path, signed headers, exact positive `Content-Length`, one owned content object, and overall lease/deadline. Connect directly; no OS proxy, redirect, cookie, authentication challenge handling, retry, decompression or connection reuse.
2. Send **headers only**, including `Expect: 100-continue` and the exact `Content-Length`. Do not call `HttpContent.CopyToAsync`, access raw lease bytes, or queue body bytes in any user-space/transport stream at this stage.
3. Read a bounded HTTP/1.1 response header section. An authenticated origin `100 Continue` is the **only** body-release event. A final response, malformed/unexpected 1xx, EOF, cancellation or overall deadline before `100` closes the connection with zero body copy; there is no time-based send fallback. A final response is still parsed by the existing Agent outcome parser where its form is valid.
4. After `100`, copy exactly the declared content length once from the retained lease to TLS, then read one bounded final response. No automatic resend after partial write or lost response. Preserve existing Agent custody/error ownership; any ambiguous partial-write result needs its existing typed handling, not a new silent retry.
5. Dispose TLS/socket and temporary header/response buffers. Never log plaintext, signatures, nonces or body bytes. Preserve lease zeroization/retirement in the owning layer.

The parser must fail closed on ambiguous HTTP framing (duplicate/conflicting lengths, transfer encoding, oversized headers/body, unexpected status or extra interim responses) and bound every allocation by the existing response limit or a reviewed smaller header limit. Use system hostname/certificate validation, with an explicit deployment-pinned Kestrel identity or equivalently reviewed direct-topology attestation; `UseProxy=false` alone does not prove that an external TLS-terminating intermediary cannot emit an early `100`. Certificate rotation/overlap must be provisioned without silently changing the approved topology.

## Why the server still matters

`100 Continue` is a release signal, **not by itself proof of R1**. The direct authenticated Kestrel endpoint is qualified only if the production `CaptureRuntimeRawIngressAdmissionService` awaits broker B/R1 before invoking the first body read. A separate PostgreSQL observer must see committed B/R1 before the raw handler observes `100` and before the first Agent transport write. A failed/rolled-back B/R1 must produce a final response with no `100` and no Agent body copy. This is one request, with the existing signed metadata and fixed `Content-Length`; no metadata preflight request is introduced.

## Horizontal proof before row disposition

Use one shared real Kestrel/TLS/PostgreSQL/production-Agent matrix over non-patient synthetic payloads of 1, 1024, 1025 and a representative larger permitted length. Cases: held B/R1 commit then release; pre-commit rollback; final rejection; silent origin until overall deadline; malformed/extra interim response; connection loss before and after `100`; direct-origin positive; invalid-topology intermediary auto-Continue and prebuffering. Record first received interim/final status, independent committed-R1 observation, first Agent content/transport write, first Server application body read, and durable/key/object residue. A test-only authenticator or downstream R2 double must be declared and may not prove production authentication or R2.

Mutate by mechanism, not by RowId: (a) copy body before `100`, (b) allow timer fallback, (c) accept unqualified early `100`/intermediary, and (d) allow eager buffering. Each negative must turn the named ordering/qualification assertion RED while sibling controls stay GREEN. Restore bytes exactly, rebuild once, run the joined restored gate and affected ratified sentinels, then freeze one packet with whole-repo TRX/failed-run census, manifest/drift and mutant sweep. Disposition the four RowIds separately; one RED does not close all four.

## Go/no-go before code

This design preserves the three pinned constraints: one external HTTP operation, declared `Content-Length`, and no Agent body copy before the qualified post-R1 signal. Implementation should not start until the narrow parser/TLS identity and ambiguous-response handling are reviewed. If a safe, bounded one-operation transport cannot be demonstrated, stop and ask the Homeowner whether a two-operation protocol is allowed; do not weaken the payload minimum, framing contract or activation gate by inference.
