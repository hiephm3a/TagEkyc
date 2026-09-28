# Site qualification Layer 2 — corrected implementation review packet v3

Status: **READY FOR TWO INDEPENDENT REVIEWS; NOT COMMITTED, NOT RE-FROZEN, NOT PUSHED**  
Baseline: Server `54d864450e699064bdd7ebf1d36ff8e94ee87482`; Agent `52b9fb9b7f550702c3c4f1dc925914561e2bcee3`; seal revision 21, count 0, `DurableWorker`, site policy YES/v1.  
Hash basis: SHA-256 over current working-tree bytes enumerated by the companion v3 manifest. `docs/00_GDRIVE_FILE_INDEX.md` is excluded as pre-existing Homeowner work.

## Review request

Review the current Layer 2 measurement plane. This packet does not ask to reopen A3, qualify a real site, deploy, commit, re-freeze, or push.

## Production path and boundaries

The two body-bearing runs use the existing raw ingress path:

```text
synthetic enrolled Capture Agent
→ public CaptureRuntimeHttpClient
→ StrictRawIngressTransport / platform TLS / HTTP/1.1
→ existing Kestrel raw endpoint and CRT1 authenticator
→ production admission and durable B/R1 broker
→ production body pipeline
→ PostgreSQL qualification store / broker observer
→ administrative report readback
→ existing 15-field qualification candidate reducer
```

The administrative API key is never placed on the raw request. The raw request is authorized by ordinary CRT1 plus an exact, short-lived, one-use run binding. The binding covers credential id/generation, signed idempotency key, closed metadata digest, media type, length and plaintext digest. Synthetic enrollment is site/origin/deployment bound and requires explicit operator authority.

Telemetry contains correlation and authority identifiers only. It contains no verification-session id, capture-artifact id, subject reference, raw class, or biometric bytes.

## Corrections since v2 HOLD

| v2 finding | current correction | direct proof |
|---|---|---|
| Joined proof bypassed production auth/body owner | Both modes now use the production CRT1 authenticator, retained-submission owner, Kestrel/TLS, production admission, durable broker, production body pipeline, PostgreSQL and MinIO. The fixture only supplies deterministic infrastructure. | `a3-site-layer2-v3-joined-final8-20260927131501-1.trx`: 2/2 PASS, zero skip. |
| SQL consume allowed NULL inequality bypass | All seven binding comparisons use `IS DISTINCT FROM`. | Seven-arm theory is green; mutation of only the credential arm back to `<>` gives 6/7 PASS and the NULL credential arm RED. |
| Synthetic provenance was not server-owned | A production enrollment table/function/store and operator-scoped enrollment API bind the credential/generation to the configured site/origin/deployment before registration. | Focused gate includes rejection before enrollment plus operator authority and exact binding tests. |
| Broker observer could affect ordinary traffic | Broker hold/commit observation is invoked only when the authenticated admission context carries a qualification run id. | `Ordinary_broker_request_never_invokes_failing_qualification_telemetry`: a throwing observer receives zero calls. |
| F3 was not connected to the measurement plane | F3 now sends both administrative and raw connections through the same TLS intermediary. The intermediary accepts concurrent connections, identifies method/path after TLS, forwards admin traffic normally, and applies early-100 plus first-body observation only to the exact CRT1 raw POST. The retained owner, production authentication/broker observation and actual report remain in use. | Connection-aware diagnostic identifies the first accepted request as admin registration and proves the predecessor single-accept proxy never accepted raw. Corrected transparent 1/1 PASS; raw-only early-100 topology 0/1 RED with the exact safety message; hostile-observation 1/1 PASS; the emitted hostile report reduces to candidate `FAIL`. |
| Proxy cleanup could swallow unexpected I/O/TLS faults | Each forwarding direction now records an unexpected fault at the instant it occurs, before `Task.WhenAll` waits for the peer direction. Outer cleanup prioritizes that recorded fault over a later owned cancellation, observes both tasks, and rethrows the original exception. | `a3-site-layer2-v3-proxy-fault-ownership-final2-20260928004918-1.trx`: pre-forward fault, owned cancellation, Agent→Kestrel fault-before-shutdown and Kestrel→Agent fault-before-shutdown all pass, 4/4. |
| Broad focused run contained four stale test failures | Three historical Program graph tests now remove the unrelated raw-export control-plane service they do not exercise. The missing-key fixture now truly omits the content key, while its assertion preserves the established O05 contract: HTTP 200 with exact `CapabilityUnavailable`, not a transport 503. No product behavior changed for this correction. | `a3-site-layer2-v3-program-graph-drift-restored2-20260927174807-1.trx`: 3/3 PASS. `a3-site-layer2-v3-broker-qualification-restored2-20260927173231-1.trx`: 25/25 PASS. Predecessor REDs are retained and classified. |
| Wrong-binding theory regenerated unrelated metadata | One frozen signed metadata vector is used; each theory case mutates exactly one of credential, generation, or idempotency binding. | Three separately named theory rows remain blocked before admission/body. |
| Evidence overstated numeric enum and PowerShell network coverage | Endpoint explicitly emits `Mode.ToString()` and the host JSON converter also emits enum names. PowerShell proofs are described only as reducer/generator/installer proofs. | Joined production client parsed both named modes; PowerShell transcript records its actual scope. |

## Two product defects found and corrected

1. Consume previously took caller time before waiting for the row lock. It now locks first and obtains PostgreSQL `clock_timestamp()` after the lock, closing the expiry TOCTOU.
2. The admin report endpoint previously serialized `Mode` numerically while the Agent requires `FullBodyHeldCommit` / `LostFinalNoRetry`. The wire contract now emits the names explicitly.

## Current exact evidence

```text
Focused server/PostgreSQL/admin/bypass gate
  a3-site-layer2-v3-focused-restored-final.trx      29/29 PASS, 0 skip

Joined production two-mode gate
  a3-site-layer2-v3-joined-final8-20260927131501-1.trx  2/2 PASS, 0 skip

Agent coordinator/strict transport core
  a3-site-layer2-v3-agent-core-final.trx             6/6 PASS, 0 skip

Migration discovery/current-history tripwires
  a3-site-layer2-v3-migration-tripwires-final.trx    2/2 PASS, 0 skip

F3 predecessor diagnostic
  a3-site-layer2-v3-f3-proxy-diagnostic-20260927144345-1.trx  0/1 diagnostic RED
  c1=POST /api/ekyc/site-transport-qualification/runs; rawPosts=0;
  send/proxy waiting; no raw connection accepted

Proxy fault ownership and cleanup
  a3-site-layer2-v3-proxy-fault-ownership-final2-20260928004918-1.trx  4/4 PASS
  unexpected pre-forward IOException propagates;
  owned cancellation cleans up without a false fault;
  Agent→Kestrel fault recorded before shutdown survives later cancellation;
  Kestrel→Agent fault recorded before shutdown survives later cancellation

Historical Program graph drift correction
  a3-site-layer2-v3-program-graph-drift-restored2-20260927174807-1.trx  3/3 PASS

Broker qualification disposition correction
  a3-site-layer2-v3-broker-qualification-restored2-20260927173231-1.trx  25/25 PASS
  missing content key is exact O05 HTTP 200 / CapabilityUnavailable;
  every other provider defect remains fail-closed as specified

F3 corrected multi-connection transparent topology
  a3-site-layer2-v3-f3-proxy-transparent-final5-20260928005446-1.trx  1/1 PASS
  asserts both admin registration and exact raw POST were observed;
  asserts zero early-100 emission

F3 early-Continue topology mutation
  a3-site-layer2-v3-f3-proxy-topology-red-final5-20260928005816-1.trx  0/1 RED
  "An intermediary-generated 100 released raw body before durable B/R1."

F3 hostile observation/report
  a3-site-layer2-v3-f3-proxy-hostile-final5-20260928010159-1.trx  1/1 PASS
  asserts both admin registration and exact raw POST were observed;
  asserts exactly one early-100 emission on the raw path
  actual report: ContinueObservedBeforeBrokerCommit=true,
                 KestrelContinueRelayedAfterCommit=false,
                 AgentBodyBytesSentWhileBrokerHeld=24,
                 ApplicationPrebufferObserved=true
  exact input SHA-256: 6FA3EAB19083A504F69AED3CE16F9F7749BA75287B928CF9F7C5AEB7C9CD50CA
  reducer time: 2026-09-28T01:05:31.8988915+00:00
  header-only early-Continue input: false
  reducer: ACTUAL_F3_FULL_BODY_REPORT_TO_CANDIDATE=FAIL

PowerShell 5.1 reducer/generator/installer
  positive candidate PASS; exact 15 fields; 7/7 negative guards;
  bad/incomplete candidates not installed; PrepareSite PASS
```

The F3 reducer calibration feeds the fresh hostile production-coordinator report from the immediately preceding current-source run into the exact candidate reducer. The header-only input is explicitly `false`, so the candidate `FAIL` is discriminated by the full-body hostile measurements rather than by the already-established header-only check. A second report is a reducer fixture only, used to satisfy the fixed two-mode input shape; the hostile values themselves are not synthesized. The separate joined proof establishes both real modes end to end. The corrected proxy keeps platform TLS validation, serves all owned connections concurrently, preserves non-cancellation connection faults, and records only connection ordinal, TLS stage, method/path and task state; it records no credential, signature, API key or payload.

## Failed-run accounting

`a3_site_qualification_layer2_failed_run_census_v3.tsv` retains and classifies 37 failed TRX runs containing 55 failed tests. Arithmetic is explicit: 37/37 failed runs and 55/55 failed tests classified. It includes the initial implementation baseline, predecessor product defects, intentional mutations/topology REDs, the connection-aware proxy diagnostic, the exact Program graph reproduction, the two superseded missing-key contract runs, and every superseded joined/F3 harness failure. No RED is renamed green or deleted.

## Scope and current state

- Qualification record schema remains exactly 15 fields, format version 1.
- Runtime consumer policy and seven fail-closed guards are unchanged.
- Existing raw URL/protocol remains the only raw ingress path.
- A3/P29–P36 semantics, partition, count 0, topology and site policy are unchanged.
- No real hospital identity, site record installation or deployment occurred.
- Current seal drift is expected because product/test/tool bytes are under review.
- The three GPT HOLD closures in this follow-up changed only test harness, test contract and evidence bytes; they added no further product or SDK correction.
- Server and Agent changes are uncommitted; no push occurred.

## Requested disposition

If both reviewers accept these exact bytes, the next transaction may commit them and create one evidence-only successor. Until then: no commit, no seal mint, no push.
