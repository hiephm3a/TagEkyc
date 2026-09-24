# TIP-88C1-C6B-A4 — Deployment Identity and Direct Cutover Dispatch

**Version:** v0.1 — PI-TAG-001 child candidate  
**Authority:** NOT GRANTED / DBA-DEPLOYMENT AUTHORITY REQUIRED  
**Depends on:** exact reviewed as-built A1+A2+A3 SHAs
**Split checkpoint basis:** umbrella v0.5 pre-manifest SHA `CC0EB43DD3CBF56A488ACEC7CD7619BA55EE7D6215EE3B4A27ACF33541B60899`

## Scope

Provision two deployment-managed LOGINs and secret references:
`tagekyc_capture_runtime_online_login` inherits only authenticator+application;
`tagekyc_capture_runtime_operator_login` inherits only operator. Configure exact
online/operator connection profiles, broker isolation and readiness evidence.
Perform quiesced direct cutover; no product migration creates LOGIN/passwords.
A4 owns no product-code mutation. A1/A3 produce conditional route/connection
code; A4 supplies LOGIN membership, secret references/values and deployment
binding. A4 also owns bootstrap-enroll orchestration: invoke A1 tool contract,
launch A2 child, pass inherited stdin, verify ACK, and revoke/reissue on loss.

## Cutover ceremony

1. maintenance gate disables and drains legacy producer writes;
2. foundation migration installs Prepared sentinel/functions;
3. DBA creates LOGINs, exact memberships and secret references;
4. offline operator command rechecks literal zero-row predicates and CASes
   Prepared→Activated;
5. new binary starts only on exact schema/ACL/sentinel and maps only canonical
   runtime routes; legacy routes remain absent;
6. post-start zero-row recheck catches any TOCTOU residue before traffic opens.

Rollback restores a compatible old binary and only its deployment/service
connection credentials before database Down—never legacy CaptureAgent/
TrustedAdapter producer keys or dual-read authority. Production activation remains separately blocked by purge/legal-hold and
other C6B activation gates.

## Completion

Requires explicit Homeowner/DBA deployment authority, exact IaC/secret/config
allowlist, role catalog evidence, quiescence/drain proof, apply/rollback rehearsal
and as-built SHAs. Product-code authority alone cannot close A4.
