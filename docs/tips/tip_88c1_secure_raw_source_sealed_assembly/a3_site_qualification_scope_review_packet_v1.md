# A3 site qualification scope — implementation packet

## Outcome

The same TagEkyc build can be prepared for any future hospital without requiring a hospital during development. Product census becomes zero; the four Expect rows move to `SITE_QUALIFICATION_REQUIRED`. An actual deployment remains fail-closed until its own exact site/origin/revision record passes.

## Product changes

- Activation seal binds `site qualification required = true` and policy version 1, not a per-hospital SHA.
- File provider reloads each evaluation; atomic renewal needs no restart.
- Startup and every raw-ingress request validate exact site ID, HTTPS origin, deployment revision, validity and all transport counters before authentication or body access.
- Expiring records emit a stable warning; expired records return a distinct stable failure.
- `/health/site-transport-qualification` exposes state and expiry without biometric material.

## Administration

- `Invoke-A3SiteRawIngressTransportQualification.ps1 -Mode DevelopmentHarness` is permanently labelled `development-loopback` and rejects a hospital label.
- `-Mode PrepareSite` creates the future site binding but prints `PREPARED_NOT_QUALIFIED`; it cannot mint PASS.
- `Install-A3SiteRawIngressTransportQualification.ps1` rejects cross-site/origin/revision evidence and installs a matching record atomically.
- Operational instructions are in `a3_site_transport_qualification_operations_v1.md`.

## Focused evidence

- Startup/policy/file-provider unit gate: 29/29 PASS.
- Host-graph regression excluding the intentionally stale generated seal: 11/11 PASS.
- Continuous expiry gate restored: 1/1 PASS.
- Mutation deleting only the per-request gate: 0/1, expected 503 became 400.
- Restored `RawExportSourceIngressEndpoints.cs` SHA-256: `E31D6BD6EAF724E99CE4D82464B93BE95D91BBA07DA379415F970877E1562338`.
- Generator self-tests: 7/7 PASS.
- Tool smoke: future-site configuration says `PREPARED_NOT_QUALIFIED`; development harness rejects a fake hospital label; installer rejects a development record for a different site.

The current generated seal is intentionally invalid after source/governance drift until the exact zero-row transaction is frozen. Prepared remains unaffected; Activated cannot slip through on stale bytes.
