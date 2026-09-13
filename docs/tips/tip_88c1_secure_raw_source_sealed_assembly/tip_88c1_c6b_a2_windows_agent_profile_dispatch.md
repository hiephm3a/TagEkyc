# TIP-88C1-C6B-A2 — Windows Managed CaptureAgent Profile Dispatch

**Version:** v0.1 — PI-TAG-001 child candidate  
**Profile decision:** RATIFIED_BY_HOMEOWNER (2026-09-07) — Windows x64 is the initial Managed CaptureAgent profile; cross-platform and Embedded SDK are future profiles and do not block initial completion  
**Implementation authority:** NOT GRANTED  
**Split checkpoint basis:** umbrella v0.5 pre-manifest SHA `CC0EB43DD3CBF56A488ACEC7CD7619BA55EE7D6215EE3B4A27ACF33541B60899`  
**Design dependency:** final reviewed A1 contract SHA; implementation/integration dependency: A1 as-built SHA  

## Scope

Initial Managed profile is the existing Windows x64 CaptureAgent/WPF package.
Implement CandidateKeyId-named non-exportable P-256 CNG CurrentUser custody,
diagnose/handoff attestation, inherited-stdin bootstrap redemption, CRT1 signing,
config/bind/reconcile orchestration and the shared one-shot IPv4+IPv6 loopback
library for Host/WPF. This is the complete initial Managed profile, without
claiming support for another OS. A2 produces `capture-agent diagnose --handoff` and
`capture-agent enroll --stdin` plus the closed evidence/ACK contracts. A4 owns
launch/orchestration and operator secrets. Cross-platform/SDK remain future,
and do not block this initial profile.

## Required ledgers

- key/material timeline covers creation before server IDs, atomic local mapping,
  response loss, restart, rotation reconciliation and authoritative deletion;
- loopback timeline covers empty slot, first-envelope session binding, bounded
  streaming parser (no secret string/model binding), ACK flush then cleanup;
- exact local store format/path/ACL/atomic replace/integrity and corruption
  behavior; no private-key bytes;
- exact Host/WPF composition and test methods for missing/wrong-account/exportable
  key, port preemption, wildcard/forwarded Host, slow/oversized/abort/shutdown.

## Completion

Both CaptureAgent compositions and full suite PASS on Windows x64 with synthetic
data. No server schema, broker, deployment LOGIN or production activation.
