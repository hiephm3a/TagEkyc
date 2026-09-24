# A3 site-probe operational successor review

## Outcome

The same product build can now be prepared, measured and qualified later at a real site without requiring a hospital during development and without restarting the API after record installation.

Product activation census remains **0**. The four Expect items remain `PRODUCT COMPLETE / SITE QUALIFICATION REQUIRED`; no row is ratified, reopened or deferred by this successor.

## Runtime correction

The predecessor startup gate made hot installation impossible: an Activated host with no record terminated before its health endpoint existed. `CaptureRuntimeStartup` now validates the product seal, count and topology at startup, while the authoritative site decision stays in the live raw-ingress gate. The host can expose health and receive the header-only qualification probe; raw ingress remains fixed-length 503 and bodyless until a current matching PASS record is installed.

The focused mutation reintroduced the old startup check. `a3-site-probe-startup-mutant.trx` is **0/1 RED**, failing before host creation with `CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID`. After byte restoration, focused host tests are **2/2 PASS** and startup/policy/provider unit tests are **29/29 PASS**.

## Probe and administration

`tools/Test-A3SiteRawIngressTransport.ps1` performs the exact-site negative topology probe. It uses the platform trust path with online revocation, requires HTTP/1.1 ALPN, sends one `POST /api/ekyc/raw-export/source-ingress` header block with `Expect: 100-continue`, deliberately sends zero body bytes, rejects all interim responses and requires a recognized fixed-length Capture Runtime 403/503 final. Only then does it emit the existing 15-field candidate record.

`Invoke-A3SiteRawIngressTransportQualification.ps1` now supports `ProbeSite`, preserves absolute operational paths and optionally invokes the existing atomic installer through `-InstallOnPass`. The explicit switch is required because installation changes live ingress eligibility. The parser self-test proves a valid final is accepted and an early `100` is rejected.

## Evidence accounting

`a3_site_probe_failed_run_census_v1.tsv` accounts for both failed runs: one intended mutation RED and one superseded generated-seal failure under intentionally stale predecessor governance. The final generated-seal case must pass after revision-8 freeze. No product, test or operational claim uses the superseded run as green evidence.

The retained post-freeze host graph is `a3-site-probe-hostgraph-revision8.trx`, SHA-256 `8A8E16813043777E8EFB9474A9BD9C718A0D672BC3F0ED15740FA106A4F89E66`: **14/14 PASS, zero skip** against the real generated revision-8/count-0/policy-1 provider. This closes the predecessor packet's evidence-retention gap; the earlier 13/14 stale-seal run remains classified rather than overwritten.

The exact-site probe is a deployment-path test, not a substitute for the already frozen joined B/R1 mechanism proof. Their conjunction is the qualification: product bytes establish Kestrel's durable-boundary behavior; the site probe establishes that the deployed route does not insert an earlier `100 Continue`.
