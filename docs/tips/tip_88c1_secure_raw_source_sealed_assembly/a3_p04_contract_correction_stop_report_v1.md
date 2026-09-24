# A3 P04 contract-correction dispatch — STOP report

Date: 2026-09-23

Status: **STOP — the requested P04 closure would require product/test work or a narrower contract correction.** This report does not amend the P04 contract, move any Expect row, update the ownership registry or ledger, change the activation partition, change the census, or re-mint the activation seal. Current activation-open census remains **5** and A3 remains **HOLD**.

## Triggered stop condition

The dispatch requires the successor P04 contract to cover observable invalid `Content-Encoding`, `Trailer`, **and framing**, all rejected after authentication and before B/R1 as HTTP 400/O04. Current production bytes do not implement that whole corrected contract:

- `RawExportSourceIngressEndpoints.RuntimeIngressAsync` invokes `TryParse` at lines 34–36, before `AuthenticateAsync` at line 52.
- `TryParse` rejects a non-positive/missing `Content-Length` and any `Transfer-Encoding` at lines 201–202. The caller maps that rejection to `CAPTURE_RUNTIME_REQUEST_INVALID`, not O04.
- Only `Content-Encoding` and `Trailer` are checked after authentication and mapped to `TransportProtocolInvalid` at lines 61–64.
- The current joined P04 test mutates only `Content-Encoding: gzip`; it does not establish authenticated O04 handling for `Trailer` plus the framing family.

Therefore a document claiming that P04 closes under the requested corrected contract would overstate current product behavior. This matches the dispatch's explicit stop rule: **stop if closing P04 would require new product code**.

## What is already supportable without product change

Current bytes support this narrower statement only:

> After successful runtime authentication, an observable `Content-Encoding` or `Trailer` header is rejected before broker admission/B/R1 as HTTP 400 with outcome O04, with zero application body read.

Current bytes do **not** support folding every invalid framing case into that statement. Some framing failures are rejected before the application can authenticate them; application-visible `Transfer-Encoding`/length failures currently follow the generic request-invalid path.

The dispatch's separate correction about early raw-body arrival remains technically sound: the application/Kestrel boundary has no trustworthy public signal that distinguishes body bytes arriving before versus after `100 Continue`. `IHttpRequestBodyDetectionFeature.CanHaveBody` only describes whether framing permits a body. Early-arrival qualification therefore belongs to site transport qualification, not to an invented endpoint observation.

## NPS01 remains untouched

No source, migration, grant or precondition for `raw_export_terminate_retained_r2_before_provider_start` was changed. Current migration bytes grant it only to `tagekyc_raw_export_reconciler`, and its ratified absence/lease predicates remain outside P04 endpoint ownership. This report does not propose calling it from the endpoint.

## Required authority choice before a successor package

One of these two corrections is required:

1. **Narrow P04 successor contract:** limit product P04 to authenticated `Content-Encoding`/`Trailer` rejection; retain generic request-invalid handling for framing that fails before authentication. Then P04 may close without product changes, while early-body ordering moves to site qualification.
2. **Keep the dispatch's broader framing requirement:** authorize bounded product/test work that separates framing classification from metadata parsing, preserves authentication precedence where application-observable, maps the approved framing cases to exact O04, and proves zero B/R1/body read. Only after that work may P04 close.

No part of the requested atomic successor package was applied while this choice is unresolved. In particular, the four Expect rows remain in the product activation partition, retain `NOT_IMPLEMENTED`, and have not been relabeled `MOVED_TO_SITE_QUALIFICATION`; the format-2 revision-5/count-5 seal is unchanged.
