# A3 site qualification Layer 2 — intent ledger

Status: implementation in progress  
Baseline: `54d8644` / activation seal revision 21  
Decision: use a synthetic enrolled Capture Agent for the real body path and a separately authenticated telemetry plane for reporting.

## Product intent

Replace the six safe constants emitted by `ProbeSite` with observations from the existing CRT1 raw-ingress path. Keep the existing 15-field qualification record, consumer policy, per-request site gate, raw endpoint, and fail-closed `MEASUREMENT_INCOMPLETE` behavior until all measurements are present.

The telemetry plane may report transport phase, counters, booleans, and an opaque qualification-run identifier. It must not report a verification-session id, capture-artifact id, subject reference, or biometric bytes.

## Reuse inventory

| Need | Existing owner reused | No duplicate implementation |
|---|---|---|
| Signed run correlation | CRT1 `CredentialId`, `CredentialGeneration`, and signed `Idempotency-Key` metadata | No qualification header and no second signature format |
| Raw send state | `StrictRawIngressTransport` and `ContentToTlsStream` | No second HTTP sender |
| Durable admission boundary | `IRawIngressMetadataBroker` followed by `CaptureRuntimeRawIngressAdmissionService` | No parallel admission pipeline |
| Site blocking | `ISiteRawIngressTransportQualificationRuntimeGate` | No second site policy |
| Administrative authentication | `IApiKeyAuthenticator` with a dedicated capture qualification scope | No API key on CRT1 raw requests |
| Site identity | `ICaptureRuntimeSiteTransportQualificationSettingsProvider` | Caller cannot choose another site/origin/revision |

## Seven-measurement trace

| Measurement | Owner | Production observation point | Bad condition | Retained evidence |
|---|---|---|---|---|
| `AgentBodySendsWhileBOrR1Held` | Capture Agent | `StrictRawIngressTransport.SendAsync` byte counter while the server run reports broker-held | intermediary releases `100` before B/R1 commit | byte count while held |
| `ServerApplicationBodyReadsWhileBOrR1Held` | API host | request-body measuring stream before `ICaptureRuntimeRawIngressBodyPipeline.ProcessAsync` | application reads while broker-held | read count while held |
| `RawPostCount` | API host | earliest exact-binding observation in `RuntimeIngressAsync` | a second matching raw POST | matching POST count |
| `KestrelContinueRelayedAfterCommit` | joint | exact client `100` observation plus server broker-commit marker | client observes `100` before committed marker | ordered phase observations |
| `EarlyOrIntermediaryContinueObserved` | probe | existing header-only `Assert-QualifyingFinal` run | any `100` before final when no body is written | header-only outcome |
| `ApplicationPrebufferObserved` | joint | Agent body serialization and API body-read phase | serialization/read begins before commit | phase boolean |
| `HiddenRetryObserved` | joint | Agent transport-entry count compared with API `RawPostCount` | either side records more than one attempt | both counters |

## Qualification-run authority boundary

An administrator holding only `capture.raw-export.site-qualification` may create a short-lived run. The server binds it to the configured site id, endpoint origin, deployment revision, and the supplied enrolled credential id, credential generation, and ingress idempotency key. The raw CRT1 request never carries the API key.

Only the exact active binding may pass the site gate before the site is qualified. The run is consumed atomically after CRT1 authentication and is single-use. Missing, expired, used, wrong-credential, wrong-generation, or wrong-idempotency bindings remain blocked before admission and body reads. An ordinary raw request remains blocked even if its operator separately owns the qualification scope.

## Scenario split

1. Header-only run: detect intermediary-generated early `100`; never sends a body byte.
2. Full-body held-commit run: exercise the real raw body path and measure commit/body ordering.
3. Lost-final run: compare client/server attempt counts without treating an unknown result as retry authority.

No single run is credited for contradictory header-only and full-body conditions.

## Invariants and non-claims

- The qualification record remains exactly 15 fields and format version 1.
- No patient identity, subject identity, session id, artifact id, or biometric content enters telemetry.
- A successful lab run is not a hospital qualification and does not install a PASS record by itself.
- No hospital hostname, certificate, IP address, or site id is used in implementation tests.
- Layer 2 does not reopen A3, P29–P36, or any ratified outcome semantics.
- The current `MEASUREMENT_INCOMPLETE` result remains until all three scenarios complete and the existing installer accepts the assembled record.

## Required negative proof

The implementation is incomplete until focused tests prove: exact active run passes; ordinary, wrong-scope, wrong-binding, missing, expired, and already-used runs remain site-blocked; every denial has zero admission calls and zero body reads; and the denial is not caused by activation-evidence incomplete/invalid or startup-not-ready. TTL, single-use, credential, generation, and idempotency binding each require an independently discriminating negative case.

## Change boundaries

Expected product surfaces: one authenticated telemetry endpoint family; one shared PostgreSQL qualification-run store callable by the API and broker; narrow instrumentation at the existing broker/admission/body boundaries; and a Capture Agent qualification command that delegates to the existing raw sender. The qualification record schema, raw CRT1 protocol, site policy, raw-ingress URL, and normal submission semantics are locked.
