# A3 site qualification Layer 2 — corrected measurement ownership ledger v2

Status: implementation review candidate, uncommitted  
Baseline: Server `54d864450e699064bdd7ebf1d36ff8e94ee87482`; Agent `52b9fb9b7f550702c3c4f1dc925914561e2bcee3`; activation seal revision 21  
Decision: synthetic enrolled Capture Agent for the real body path plus a separately authenticated telemetry plane.

## Reuse inventory

| Need | Existing production owner reused | Explicitly not rebuilt |
|---|---|---|
| Raw send and TLS identity | `CaptureRuntimeHttpClient` → `StrictRawIngressTransport` → `ContentToTlsStream` | no second sender, no TLS override |
| Raw authorization | existing CRT1 authenticator and closed ten-header metadata digest | no qualification header or second signature format |
| Admission ordering | existing metadata broker and `CaptureRuntimeRawIngressAdmissionService` | no parallel admission protocol |
| Durable B/R1 phase | existing broker transaction and `RawIngressBrokerTransactionFacade` | no test-only commit flag |
| Site blocking | existing per-request site qualification gate | no second site policy |
| Control authentication | existing API-key authenticator with dedicated `capture.raw-export.site-qualification` scope | no API key on raw CRT1 request |
| Site identity | current `ICaptureRuntimeSiteTransportQualificationSettingsProvider` | caller cannot choose another site/origin/revision |

## Seven measurements

| Measurement | Owner | Exact observation point | Real production path | Correlation key | Hostile condition / bad value | Retained evidence |
|---|---|---|---|---|---|---|
| `AgentBodySendsWhileBOrR1Held` | Agent | `ContentToTlsStream` byte callback feeding `RawIngressTransportMeasurement.OnContentBytesCopied` | existing raw POST serialization | signed credential generation + signed ingress idempotency key + one-time run | intermediary releases body before commit / non-zero | agent observation plus joined TLS run |
| `ServerApplicationBodyReadsWhileBOrR1Held` | API | request-body measuring stream calls `SiteRawIngressQualificationRequestMeasurement.ObserveBodyRead` before body pipeline | existing raw endpoint and body pipeline | consumed run attached to the authenticated request | application reads before broker commit / non-zero | durable server observation; hostile component test changes the value |
| `RawPostCount` | API/store | earliest exact-binding observation in `RuntimeIngressAsync` and PostgreSQL run row | existing raw endpoint before admission | exact signed binding plus site/origin/deployment | second matching raw POST / not one | durable run report and lost-final joined run |
| `KestrelContinueRelayedAfterCommit` | joint | client records the phase at exact `100`; server records durable commit | strict raw transport + broker transaction | one-time run and qualification suite | `100` observed before client knows commit / false | ordered client/server report and F3 pair |
| `EarlyOrIntermediaryContinueObserved` | header-only probe | existing `Assert-QualifyingFinal` path, which never writes its declared byte | same site endpoint through the network route | probe execution | any `100` before final / true | header-only probe result; F3 hostile topology RED |
| `ApplicationPrebufferObserved` | joint | `RetainedRawHttpContent.SerializeToStreamAsync` begins observation; server observes application body reads | existing content object and raw endpoint | exact signed binding | serialization/read starts before commit / true | one-shot observations; early-continue real-serialization test |
| `HiddenRetryObserved` | joint | client transport-entry count compared with durable server raw-POST count | same two full-body exchanges | qualification suite with exactly one run per required mode | either count differs from one / true | full-body and lost-final joined reports |

The client cannot cryptographically identify which TLS terminator emitted `100 Continue`. The measurable safety property is narrower and sufficient: no `100` is acted upon before the server's durable B/R1 commit has been observed. The F3 topology mutation demonstrates why this distinction matters.

## Exact run authority

The one-time run is bound to:

- API-key owner, current site id, endpoint origin, and deployment revision;
- qualification suite and required mode;
- credential id and generation;
- signed ingress idempotency key;
- closed ingress metadata SHA-256;
- exact `image/jpeg` media type and content length;
- exact plaintext SHA-256.

The raw request contains no API key. CRT1 verifies the ordinary signed request first. Only then may the exact unexpired single-use binding cross an unqualified site's gate. Control operations re-check owner and current site/origin/revision. Agent and server observations are one-shot and cannot overwrite an earlier bad value with a later safe value.

## Scenario split

1. Header-only probe: no body byte is written; any intermediary `100` makes the route fail qualification.
2. `FullBodyHeldCommit`: body traverses the real sender, TLS, Kestrel, CRT1, broker, PostgreSQL, and body pipeline; final response must be observed.
3. `LostFinalNoRetry`: the same path commits and copies the declared body, the response is deliberately lost, and no second client entry or server POST is permitted.

The PowerShell reducer requires exactly two fresh reports, one of each body mode, from the same suite/site/origin/deployment. It also requires both observers complete, exact content-byte counts, final seen only for the full-body run, and final absent for the lost-final run. Missing or contradictory evidence yields `MEASUREMENT_INCOMPLETE`, never `PASS`.

## Locked boundaries

- Qualification record remains exactly 15 fields, format version 1.
- Existing seven consumer guards and runtime site policy are unchanged.
- Existing raw URL and CRT1 wire contract are unchanged.
- A3, P29–P36, assembly topology, activation count, and all ratified semantics are unchanged.
- Telemetry contains correlation/authority identifiers only; it excludes verification session, capture artifact, subject, raw class, and biometric bytes.
- A lab run is not a hospital qualification and does not install a PASS record without explicit `InstallOnPass`.
- No hospital information was used.

