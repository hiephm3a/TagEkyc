# A3 site-probe operational successor

Date: 2026-09-23. Decision source: the Homeowner's explicit instruction `Làm đi`, following the accepted direction that development must not require a real hospital and that a reusable site tool must be ready before the first hospital exists.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 0
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: 2DA0A4C5586C54FBED422B7474EE72DC0B224D3A150B6E54C14382D4C03E962F
A3-Partition-SHA256: BE0C0E1A2B82BF4B9E8B10387F58D4589A71233DCD3483573B6C344D6798B136
A3-Ledger-SHA256: 6250CC537ED67F6FA3C0BC7795065455BEA591C3FC0271B17B6759B6EC54A2C4

This successor changes no activation row, authority classification, assembly scope or deferred backlog. Product census remains zero. Site-transport qualification policy version remains 1 and continues to bind exact `siteId`, HTTPS origin and deployment revision.

The operational correction has two inseparable parts:

1. An Activated host with a valid zero-open product seal may start without a site record so that health and the exact deployed endpoint are reachable for qualification. Missing, invalid or expired site evidence still blocks every raw-ingress request before authentication or body access.
2. The site probe uses platform CA/name validation, online revocation checking and ALPN `http/1.1`, sends exactly one header-only `Expect: 100-continue` request to the production raw-ingress path, sends zero body bytes and rejects any interim `100`. A recognized fixed-length Capture Runtime final response is required before it emits a candidate PASS record.

The probe neither uploads biometric data nor manufactures a hospital identity from the development harness. Installation remains an explicit atomic operation. A TLS-terminating intermediary is permitted only when the exact deployed route passes; a failed probe does not modify the live record. No stage, commit, push or production deployment is authorized by this decision.
