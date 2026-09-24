# TIP-88C1-C6B-A3 — C6B Broker Composition Dispatch

**Version:** v0.1 — PI-TAG-001 child candidate  
**Authority:** NOT GRANTED  
**Split checkpoint basis:** umbrella v0.5 pre-manifest SHA `CC0EB43DD3CBF56A488ACEC7CD7619BA55EE7D6215EE3B4A27ACF33541B60899`  
**Design dependency:** final A1 runtime/pre-broker contract SHA; implementation dependency: A1 as-built SHA and C6B dispatch v1.6 SHA `248A9D3AA966FF643245CB67C46455D82B13BE59577E621355324FE67F7292F6`

## Scope

Create the already-authorized isolated OS-independent ASP.NET Core broker host
and HTTP adapter, plus the exact typed identity/binding validation composed with
landed C6B R1. A1 owns the public route/auth/nonce/pre-broker DTO; A3 owns only
the private broker host route, typed adapter and broker transaction. The pre-body transaction validates runtime lineage, binding and
acceptance, invokes only `raw_export_begin_retained_source_ingress_with_authority`
and commits R1. Only then may API consume Request.Body. Existing body pipeline
later invokes complete/handoff and R2-R6; it performs no new runtime reauth.

## Required ledgers

- copy literal landed begin and complete/handoff signatures and map each field
  to Binding, Acceptance, Request metadata or Server config; derived identities
  are removed from caller input;
- cross-domain lock graph includes runtime/session/capability, acceptance and
  landed alias/exact ingress locks with no reverse edge;
- exact broker project/route/DTO/status/connection/ACL/DI paths from upstream
  allowlist; no JSON bag and no outcome 37;
- all 36 outcomes plus committed replay/custody convergence and first-body-read
  proofs, with one method and RED mutation per boundary.

## Completion

Synthetic end-to-end R1→R6 and broker/API suites PASS; A3 cannot alter the
frozen C6B outcome table or claim deployment/cutover.
