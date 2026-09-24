# A3 strict Expect bounded slice — STOP checkpoint

**Status:** PARTIAL IMPLEMENTATION / NOT FROZEN / NO ROW CLOSURE  
**Date:** 2026-09-22  
**Authority-open census:** 7; four TRANSPORT-EXPECT-FENCE rows OPEN; A3 HOLD  
**Dispatch:** strict HTTP + TagEkyc transport identity (default NOT READY), fixed-length Server finals, stage-aware ambiguous send. This is one bounded slice, not a new scope decision.

## Current source and bounded results

The Agent raw-ingress default now uses a fresh direct TLS/HTTP/1.1 operation, writes fixed request headers with `Expect: 100-continue`, and withholds `HttpContent.CopyToAsync` until parsing exact `HTTP/1.1 100 Continue` from a chain/hostname/SPKI/ALPN-qualified peer. A final before `100` does not copy body bytes. The handler exposes observed-`100`, content-copy count and stage to the caller. No profile source is configured in production: missing profile refuses before connecting. This is a fail-closed implementation state, **not** a claim of an approved signed installation profile or approved hospital deployment.

The Activated Server raw route now serializes the **existing five-field** `CaptureAgentFinalResult` and existing `{code, correlationId}` error shape to a bounded JSON byte array and sets exact `Content-Length`; it does not change status, outcome code or nullable JSON fields. Focused TestServer coverage is 21/21 PASS on current bytes. This is not yet a real Kestrel wire-framing proof.

The retained owner distinguishes zero-copy typed transport failure from any copied byte. Partial body and complete body without a final both map to `SUBMISSION_STATE_UNKNOWN`; the retained continuation refuses an explicit second submission when marked ambiguous. An untyped exception or cancellation while inside submission is also conservatively unknown; caller cancellation is not proof of zero-copy. Missing TLS identity maps to `NOT_READY`. The final-byte focused Agent transport/client/retained-ownership gate is 101/101 PASS; legacy client compatibility is 6/6 PASS. These are not a Server-side no-second-POST assertion.

Current exception-to-custody map (source inspection plus focused tests; not joined Server proof):

| At retained-owner catch | Reported result | Explicit same-lease retry |
| --- | --- | --- |
| Typed TLS identity refusal, zero copied | `NOT_READY` | May be attempted only after readiness changes and lease remains valid |
| Other typed failure, zero copied | First result conservatively `SUBMISSION_STATE_UNKNOWN` | Allowed by zero-copy fact; no automatic repost |
| Typed failure, positive copied count (partial or complete without final) | `SUBMISSION_STATE_UNKNOWN` | Forbidden until separately proven durable reconciliation |
| Untyped `HttpRequestException` or `OperationCanceledException` during submission | `SUBMISSION_STATE_UNKNOWN` | Forbidden; copied count is not known |
| Parsed Server final outcome | Exact existing O-code | Existing per-code lease policy, not a transport retry |

Mechanism mutations were restored byte-exact on `StrictRawIngressTransport.cs` (SHA-256 `E301788FEB7DF6473DA8A5E3C448C9CC79BBD351CAE4C9E5442A3B3DA1C43E7E`):

| Mutation | Target result | Adjacent control |
| --- | --- | --- |
| Early copy when declared length `<=1024` | 1 and 1024 byte final-before-100 cases RED | 1025 and 8192 byte cases PASS |
| Bypass SPKI equality | wrong-pin refusal RED (`Assert.Throws`: no exception) | no row closure claimed |
| Bypass deployment-presence check | missing-deployment refusal RED | no signed-record verifier claim |

The failed runs are retained, not treated as evidence: strict transport attempt a2 (10 test-cert fixture failures), retained owner attempt a1 (3 predecessor expectations assumed an untyped loss was retryable), raw-client compatibility attempt a1 (22 synthetic abbreviated-JSON fixture failures), Server framing attempt a1 (21 TestServer stream `Length` failures), restored Agent attempts a1/a3 (one TLS-refusal/listener-disposal fixture failure each). The three mutation TRX above are intended RED. This is a bounded run accounting note, **not** the required whole-repo TRX/manifest freeze.

## STOP: finish lines not reached

1. There is no production signed-profile loader/verifier, independent issuer trust anchor, or independently protected revision floor. The injected peer source used by synthetic tests is not a signing authority. The actual approved issuer key and separately controlled anchor location cannot be inferred from current source. The deployment record is only represented by a hash-shaped field in the mechanism object; no approved hospital record is loaded or verified. This explicitly leaves Gate A production qualification and Gate B open. Do not substitute the synthetic test identity for either.
2. No joined real Kestrel/TLS/PostgreSQL/production-Agent matrix independently observes durable B/R1 before first content-to-TLS copy at lengths 1, 1024, 1025 and large. No Server-side count/durable-row assertion yet proves no second raw POST after an ambiguous send.
3. The complete F0–F8 negative matrix, stage-collapse mutation, ALPN/interim/fallback mutations, two affected-source ratified sentinel sets, whole-repo failed-run census, and candidate manifest are not complete. The targeted tests above are component/mechanism evidence only.
4. Source changes make the existing activation seal stale (`ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` at build). This is expected fail-closed behavior. No seal re-mint is authorized by this slice.

**Disposition:** bounded correction stays in the working tree for review; do not call the slice DONE, do not ratify any of the four rows, do not change census 7 or A3 HOLD, and do not activate, stage, commit or push. Resume implementation only with an exact signer/anchor/revision-floor authority and the joined proof plan; do not choose a local-admin threat assumption in code.
