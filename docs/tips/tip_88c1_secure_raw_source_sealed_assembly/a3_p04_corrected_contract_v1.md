# A3 P04 corrected contract v1

Date: 2026-09-23

Status: **SUCCESSOR CONTRACT — TECHNICAL CLOSURE CANDIDATE — NOT HOMEOWNER-RATIFIED**

This successor narrows P04 to the application outcome that the shipping endpoint can own and observe. It supersedes the original P04 clause that coupled application header rejection to an existing-R1/early-body terminalization path. P04 may close only under this **CORRECTED contract**; this document does not claim implementation of that original clause.

## 1. P04 application outcome

For an authenticated raw-ingress request carrying either `Content-Encoding` or `Trailer`, the application returns HTTP 400 with the exact sole public outcome `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`. The rejection occurs before broker admission, therefore no B/R1 is created and no application body byte is read.

Both header arms are independently load-bearing. Removing only the `Content-Encoding` arm must fail only the named Content-Encoding case; removing only the `Trailer` arm must fail only the named Trailer case.

## 2. Malformed HTTP framing is not P04

`Transfer-Encoding`, invalid or missing `Content-Length`, and other bad framing are rejected before authentication by the endpoint parser or by Kestrel. They use the generic request/protocol-invalid surface and are not converted into the A3 P04 business outcome.

The pre-authentication ordering is intentional. `AuthenticateAsync` returns only after its nonce transaction has committed. Moving an application-visible framing rejection after authentication would spend signature verification and consume a nonce for a request that was never framable. Some malformed framing is rejected inside HTTP/Kestrel before the application can authenticate or classify it at all; such failures must not be relabeled as an A3 business outcome.

The ordering is anchored by an independent PostgreSQL observation: the named Transfer-Encoding case returns the generic code while the committed authentication-nonce count remains zero. Moving that check after authentication must fail at the nonce-count assertion.

## 3. Early body before `100 Continue` is not P04

The application/Kestrel boundary has no trustworthy observer that can prove when a client or TLS-terminating intermediary first placed body bytes on the transport. Early body before `100 Continue` is therefore governed by `SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION`, not by P04.

The executable qualification must prove, for the exact site/deployment revision, that B/R1 remains ahead of the first application body read, that Kestrel's `100 Continue` is relayed only after durable commit, that exactly one raw POST occurs, and that no intermediary-generated early `100`, prebuffer, or hidden retry is observed. The F3 auto-`100` topology is a mandatory negative control and must fail qualification.

## State boundary

This contract correction and its evidence do not move P04 out of the five-row activation partition, do not ratify P04, do not move the four Expect rows, and do not re-mint the activation seal. Census remains **5** and A3 remains **HOLD** pending independent review and Homeowner ratification.
