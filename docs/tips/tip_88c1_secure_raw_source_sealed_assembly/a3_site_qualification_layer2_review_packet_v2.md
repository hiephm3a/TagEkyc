# A3 site qualification Layer 2 — corrected implementation review packet v2

Status: **READY FOR INDEPENDENT REVIEW; NOT COMMITTED, NOT RE-FROZEN, NOT PUSHED**  
Baseline: Server `54d864450e699064bdd7ebf1d36ff8e94ee87482`; Agent `52b9fb9b7f550702c3c4f1dc925914561e2bcee3`; seal revision 21  
Hash basis: SHA-256 over current working-tree bytes enumerated by `a3_site_qualification_layer2_review_manifest_v2.tsv`. The current whole-tree activation manifest is intentionally drifted until review acceptance.

## Disposition requested

Review the corrected Layer 2 implementation against L2-R1 through L2-R6 from the predecessor HOLD. This packet does not request A3 reopening, ratification, a real-site PASS, deployment, re-freeze, or push.

Predecessor HOLD packet SHA-256: `AA77188AC046BF08C279C60A1FC7B087D9F8DF1EAAF1C0982D96C649CAC6659E`  
Predecessor HOLD manifest SHA-256: `A1D45FD93DA87CA3856B57AC731243E4AE98BE008C9D404174C3686122857B71`

## What exists now

The production `ProbeSite` path runs the existing header-only probe and consumes two reports produced by a synthetic enrolled Capture Agent using the existing CRT1 raw-ingress path. A 15-field PASS candidate is possible only when the header-only route is safe and both full-body reports are fresh, complete, bound to the same qualification suite/site/origin/deployment, and conservative aggregation of all seven measurements is safe.

```text
synthetic Agent
  → public CaptureRuntimeHttpClient constructor
  → StrictRawIngressTransport / platform TLS / HTTP/1.1
  → existing Kestrel raw endpoint
  → CRT1 verifier
  → existing metadata broker and durable B/R1 transaction
  → PostgreSQL run store and broker observer
  → existing body pipeline
```

No parallel raw endpoint, second raw protocol, custom trust policy, patient session, capture artifact, subject identifier, raw class, or biometric byte was added to telemetry.

## Predecessor HOLD corrections

| Finding | Correction | Discriminating proof |
|---|---|---|
| L2-R1 exact synthetic binding missing | Run binding now includes credential/generation/idempotency, closed metadata digest, media type, length, and plaintext digest. CRT1 remains mandatory before consume. | `Newly_signed_request_with_different_exact_metadata_cannot_use_registered_run` uses a newly valid signature over different metadata and remains blocked before admission/body. PostgreSQL independently rejects every mismatching binding arm. |
| L2-R2 owner/control isolation and overwrite | Read/release/acknowledge/agent-report require the registering API-key owner plus current site/origin/deployment. Agent and server observation writes are one-shot. | API test carries the authenticated actor to every operation; PostgreSQL wrong-owner calls fail; a later hostile observation cannot overwrite the retained safe observation (and vice versa). |
| L2-R3 missed early `100` / prebuffer | Until broker commit is observed, every continue, serialization start, and copied byte is unsafe. The content object itself reports actual serialization; lost event completion prevents evidence completion. | Agent real-serialization test; F3 transparent 1/1 vs intermediary-100 0/1; joined full-body path confirms commit-before-copy. |
| L2-R4 report binding/completion/freshness | Reports carry suite/site/origin/deployment, content bytes, final-response state, and separate agent/server completion markers. Reducer requires exactly one run of each mode and all binding/freshness/completion predicates. | Tool positive control plus 7/7 negative values; incomplete tool proof; joined two-mode 2/2. |
| L2-R5 stale caller clock at row lock | Consume is PL/pgSQL: select row `FOR UPDATE`, then read `clock_timestamp()`, then decide and consume. | `Consume_rechecks_database_clock_after_waiting_past_expiry` holds the row lock past TTL and expects rejection; two independent consumers prove exactly one winner. |
| L2-R6 renewal and normal submissions | Run observation is independent of current site-qualified state. Exact renewal traffic is measured while the active record remains installed. Telemetry lookup failure is ignored for an ordinary already-qualified submission. | Named renewal and normal-submission tests both pass without replacing the installed qualification. |

## Joined production proof

`SiteQualification_joined_agent_tls_kestrel_broker_postgres_report_is_complete` is a two-case theory and uses:

- the public production `CaptureRuntimeHttpClient` constructor;
- platform trust inside the disposable Linux CA/CRL runner;
- TLS/Kestrel and the existing raw route;
- ordinary CRT1 request signing/verification;
- production PostgreSQL qualification store and broker observer;
- production metadata broker/admission/body pipeline.

The deterministic test scope object implements only the per-request measurement interface needed to inspect body-read phase; it does not authorize the request, synthesize a result, create a run, write a report, or replace the production broker/store.

Final restored result:

```text
FullBodyHeldCommit  PASS: final observed, declared content bytes copied, 1 transport entry, 1 POST
LostFinalNoRetry    PASS: connection lost after complete body, no final, outcome unknown, 1 transport entry, 1 POST
Both                PASS: B/R1 committed before copy/read, observers complete, no hidden retry
TRX                 a3-site-layer2-joined-r5-20260927090507-1.trx — 2/2, zero skip
```

## Hostile topology calibration

The existing F3 route was rerun on current bytes:

```text
transparent intermediary                     1/1 PASS
intermediary-generated early 100             0/1 RED
failure: "An intermediary-generated 100 released raw body before durable B/R1."
```

This is a topology mutation, not a production-source mutation. It demonstrates an environment that changes the measured safety property.

## Focused restored evidence

```text
Server component + admin endpoint                  17/17 PASS, zero skip
PostgreSQL exact binding/TTL/race/ACL                3/3 PASS, zero skip
Migration discovery tripwires                        2/2 PASS, zero skip
Agent coordinator                                    4/4 PASS, zero skip
Joined Agent/TLS/Kestrel/CRT1/Broker/Postgres         2/2 PASS, zero skip
F3 transparent topology                              1/1 PASS, zero skip
PowerShell 5.1 measured candidate                    PASS; 15 fields; 7/7 guards
PowerShell 5.1 incomplete candidate                  PASS; install blocked; target unchanged
```

PowerShell version retained: `5.1.19041.6456`.

## Failed-run accounting

| Run | Result | Classification | Successor |
|---|---:|---|---|
| joined r2 | 0/1 | `PRODUCT_DEFECT_PREDECESSOR`: admin endpoint serialized enum mode numerically while production Agent required the named wire value | explicit string serialization; r3 and r5 green |
| joined r4 | 1/2 | `SUPERSEDED_TEST_DEFECT`: lost-final expected the owner-layer exception although direct public submit correctly exposes `RawIngressTransportException` with complete-body/no-final stage | exact transport reason/stage/byte assertions; r5 2/2 |
| server-in-container combined | 17/20 | `INVALID_RUNNER_TOPOLOGY`: three tests intentionally create isolated Docker PostgreSQL clusters and cannot do so from the nested TLS container | same three tests on Windows Docker host 3/3 |
| F3 early-continue mutation | 0/1 | `EVIDENCE_RED_TEST_TOPOLOGY` | transparent counterpart 1/1 |

No failed run is re-labelled green or deleted.

## Product defects discovered during joined proof

1. PostgreSQL consume previously compared expiry with caller time captured before waiting for the row lock. It now reads the database clock after lock acquisition; the held-lock-past-TTL test would fail under the old function.
2. The admin report endpoint previously emitted numeric enum mode while the real Agent reads the named mode. The joined production path caught it; the endpoint now explicitly emits the string name.

## Safety and scope

- The site bypass exists only for an exact active one-time run after ordinary CRT1 authentication; every denial remains before admission/body.
- A raw request cannot select the bypass with an API-key header; the CRT1 parser rejects that header.
- Current PASS qualification remains installed during renewal measurement; installation remains explicit.
- Qualification record schema stays at exactly 15 fields, format version 1.
- Runtime qualification consumer, seven guards, raw URL, A3, P29–P36, count `0`, `DurableWorker`, and site policy `YES/v1` are unchanged.
- No SDK source changed.
- No real hospital information, deployment, qualification installation, commit, seal mint, or push occurred.
- `docs/00_GDRIVE_FILE_INDEX.md` is Homeowner-owned pre-existing work and was not touched.

## Next transaction, only after two-review PASS

Commit the Server and Agent bytes, then create one evidence-only successor. The seal may change only `evidence_revision`, `ratification_manifest_sha256`, and `ratification_record_sha256`; count, topology, site policy, partition, ledger, scope decision, and ownership hashes must remain unchanged. No site is qualified by that transaction.
