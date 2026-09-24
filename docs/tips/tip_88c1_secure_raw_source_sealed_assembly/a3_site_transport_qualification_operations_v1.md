# A3 site transport qualification operations

This is an operational control, not a per-hospital product build.

## Development

Run `tools/Invoke-A3SiteRawIngressTransportQualification.ps1` in its default
`DevelopmentHarness` mode. It exercises the isolated TLS/Kestrel/PostgreSQL
harness and its F3 early-`100 Continue` negative control. The emitted record is
always identified as `development-loopback`; it cannot be relabelled as a real
site and does not qualify production deployment.

## Prepare a future site

When a hospital endpoint exists, an administrator prepares the binding without
changing or rebuilding TagEkyc:

```powershell
pwsh -File tools/Invoke-A3SiteRawIngressTransportQualification.ps1 `
  -Mode PrepareSite `
  -SiteId hospital-001 `
  -EndpointOrigin https://ekyc.hospital.example:8443 `
  -DeploymentRevision network-2026-01 `
  -ConfigurationPath C:\ProgramData\TagEkyc\site-transport.json `
  -RecordPath C:\ProgramData\TagEkyc\site-transport-qualification.json
```

This creates configuration only and prints
`PREPARED_NOT_QUALIFIED`. It never manufactures a PASS record.

Start the Activated API with this configuration. A missing record no longer
stops the host: health and qualification traffic remain available, while the
raw-ingress request gate continues to return a fixed-length 503 without reading
the request body.

From an Agent-side machine on the same network path that production capture
will use, probe the exact endpoint. The probe uses platform CA/name validation,
requires ALPN `http/1.1`, sends one bodyless `Expect: 100-continue` request to
the real raw-ingress path and fails if any intermediary emits `100 Continue`.
It never sends biometric bytes or credentials:

```powershell
pwsh -File tools/Test-A3SiteRawIngressTransport.ps1 `
  -SiteId hospital-001 `
  -EndpointOrigin https://ekyc.hospital.example:8443 `
  -DeploymentRevision network-2026-01 `
  -OutputRecordPath C:\DeploymentEvidence\site-pass.json
```

Then install the resulting PASS record atomically:

```powershell
pwsh -File tools/Install-A3SiteRawIngressTransportQualification.ps1 `
  -ConfigurationPath C:\ProgramData\TagEkyc\site-transport.json `
  -CandidateRecordPath C:\DeploymentEvidence\site-pass.json
```

The installer rejects records for a different `siteId`, HTTPS origin or
deployment revision. A valid replacement is read without restarting the API.

The same workflow is also available through the single administration entry
point. Omit `-InstallOnPass` when an operator wants to inspect/archive the
candidate record first:

```powershell
pwsh -File tools/Invoke-A3SiteRawIngressTransportQualification.ps1 `
  -Mode ProbeSite `
  -ConfigurationPath C:\ProgramData\TagEkyc\site-transport.json `
  -RecordPath C:\DeploymentEvidence\site-pass.json `
  -InstallOnPass
```

The probe is deliberately a negative topology test. The shipped product proof
already establishes that Kestrel releases `100 Continue` only after the durable
B/R1 boundary. The site probe establishes the missing deployment fact: the
actual path does not insert an earlier `100 Continue`. A TLS-terminating proxy
can therefore be used only if it passes this probe; a transparent TCP forwarder
is naturally compatible.

## Runtime behaviour

- `/health/site-transport-qualification` reports `Qualified`, `Expiring`,
  `Missing`, `Invalid` or `Expired` without exposing raw biometric data.
- The host logs `CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_EXPIRING`
  during the configured warning window (default 24 hours).
- Missing, mismatched or expired evidence fails closed before authentication or
  request-body access.
- Renewal is an atomic record replacement; restart is not required.

The product activation seal binds that site qualification is required and the
policy version. It does not bind one hospital's record SHA, so the same product
build can be deployed to multiple hospitals.
