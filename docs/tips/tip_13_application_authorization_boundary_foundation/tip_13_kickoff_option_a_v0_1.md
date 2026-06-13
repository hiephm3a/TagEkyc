# TIP-13 Application Authorization Boundary Foundation Kickoff Option A v0.1

**File:** `docs/tips/tip_13_application_authorization_boundary_foundation/tip_13_kickoff_option_a_v0_1.md`
**Version:** 0.2
**Status:** Accepted - kickoff only; implementation requires separate dispatch
**Date:** 2026-06-13
**Baseline:** `cc42076299ed7a1d7fac4f64ced554740633f2b4`
**Purpose:** Drafts a narrow kickoff for application authorization boundary hardening using current LocalDev authentication only, based on accepted TIP-12 actor/scope/ownership planning.

## Changelog

### v0.2 - Kickoff accepted

- Recorded GPT Gate acceptance of TIP-13 Option A kickoff.
- Reconfirmed public API/DTO behavior changes are out of scope for Option A and require a separate reviewed TIP/kickoff.
- Preserved that implementation still requires a separate dispatch command.

### v0.1 - Initial kickoff draft

- Opened TIP-13 as kickoff/planning only.
- Selected Option A: application authorization boundary hardening using current LocalDev auth only.
- Preserved BusinessConsumer, CaptureAgent, and TrustedAdapter separation.
- Kept Operator, Admin, and System/InternalService production behavior reserved or STOP/RRI.
- Prohibited public API/DTO behavior changes, production auth provider, credential store, database, EF, migrations, durable persistence, webhook/outbox/retry, crypto/signing/replay, provider/vendor selection, pilot/production readiness, and SignFlow runtime dependency work.

## 1. Kickoff Summary

TIP-13 is the first post-TIP-12 candidate for actor/scope/ownership enforcement hardening.

Selected candidate:

```text
Option A - Application authorization boundary hardening using current LocalDev auth only.
```

This kickoff is accepted as kickoff-only. It does not authorize implementation until a separate dispatch command is provided.

TIP-13 must not change TagEkyc from LocalDev evidence-ready/non-production/non-certified status.

## 2. Baseline Inputs

- `docs/tips/tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md`
- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- Current LocalDev auth/application service shape in `src/TagEkyc.Application/AuthenticatedClientContext.cs`, `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs`, and application service authorization checks.

## 3. Option A Goal

The goal is to harden the application authorization boundary without changing the authentication substrate.

If separately dispatched for implementation, Option A should make authorization decisions more explicit, auditable, and testable around:

- actor category;
- caller scope;
- client/session ownership;
- allowed client bindings for CaptureAgent and TrustedAdapter;
- current LocalDev policy;
- forbidden cross-client access;
- forbidden role mixing.

Option A must use the current LocalDev auth model only.

## 4. Required Preserved Boundaries

TIP-13 Option A must preserve:

- current LocalDev auth only;
- BusinessConsumer / CaptureAgent / TrustedAdapter separation;
- Operator/Admin/System as reserved or STOP/RRI;
- no public API/DTO behavior changes;
- no production auth provider;
- no credential store;
- no DB, EF, migrations, schema, or durable persistence;
- no webhook, outbox, retry, delivery ledger, subscription model, or dispatcher;
- no crypto, signing, replay protection, HMAC, JWS, KMS, HSM, or key rotation;
- no provider/vendor selection;
- no pilot readiness claim;
- no production readiness claim;
- no SignFlow runtime, source, database, network, package, deployment, or internal-model dependency.

Durable persistence remains deferred until after actor/scope/ownership enforcement is clarified, unless homeowner explicitly accepts a narrower exception.

## 5. Candidate Implementation Shape If Separately Dispatched

This section describes the allowed shape if a separate implementation dispatch command is later provided. The accepted kickoff is not implementation authorization by itself.

Allowed implementation pattern:

- Keep current LocalDev authentication and `AuthenticatedClientContext`.
- Introduce or centralize application-layer authorization decisions if it reduces duplicated checks and preserves current behavior.
- Keep authorization explicit at the application service boundary.
- Preserve current endpoint and DTO behavior.
- Add focused tests proving current allowed/forbidden actor/scope/ownership behavior.
- Add architecture or contract tests only if needed to prevent BusinessConsumer exposure of internal/auth metadata.

Option A must not change public API routes, request/response DTOs, JSON shape, status codes, error envelopes, completion response, package summary, notification projection payload, or public contract behavior.

Any authorization centralization must preserve:

- same allowed actors;
- same forbidden actors;
- same ownership checks;
- same status/error behavior;
- same public DTO/API JSON;
- same completion authority;
- same package/hash/notification semantics.

Likely checks:

| Operation | Expected actor boundary |
| --- | --- |
| Create verification session | BusinessConsumer only, own client, allowed policy/scope. |
| Get verification session | BusinessConsumer only, own client, sanitized output. |
| Append capture artifact metadata | CaptureAgent only, allowed client/session binding. |
| Append trusted evidence result | TrustedAdapter only, allowed client/session binding. |
| Complete/finalize session | Current LocalDev BusinessConsumer behavior only; production authority remains STOP/RRI. |
| Get evidence package summary | BusinessConsumer only, own client, sanitized package summary. |
| Get completion notification projection | S1 app-service projection only for owned completed-session output; no public route, webhook delivery, subscription, outbox, or retry. |

## 6. Explicit Non-Goals

TIP-13 Option A does not:

- implement production authentication;
- add OAuth/OIDC, mTLS, certificates, external identity provider, secret manager, or production API key lifecycle;
- add credential store or credential lifecycle implementation;
- add database, EF, DbContext, migrations, schema, local durable adapter, or durable persistence;
- add webhook/outbox/retry/subscriptions/delivery ledger;
- add crypto/signing/replay protection;
- add provider/vendor/device selection;
- add operator/admin console or production support workflow;
- decide production completion authority;
- change public API routes, request/response DTOs, JSON shape, status codes, error envelopes, completion response, package summary, notification projection payload, or public contract behavior;
- claim pilot readiness;
- claim production readiness;
- introduce SignFlow runtime/source/database/network/package/internal-model dependency.

## 7. Dispatch Allowlist

If a separate implementation dispatch command is later provided, the dispatch allowlist is:

- `src/TagEkyc.Application/**`
- `src/TagEkyc.Domain/**` only if needed for value objects/enums
- `tests/TagEkyc.UnitTests/**`
- `tests/TagEkyc.ContractTests/**`
- `tests/TagEkyc.ArchTests/**`
- `docs/tips/tip_13_application_authorization_boundary_foundation/**`
- `docs/tips/README.md`
- `docs/00_AGENT_COORDINATION_BUS.md`

Forbidden unless a separate reviewed TIP/kickoff explicitly authorizes it:

- `src/TagEkyc.Api/**`
- `src/TagEkyc.Infrastructure/**`
- `src/TagEkyc.Adapters/**`
- `src/TagEkyc.SignFlow/**`
- migrations
- project/package references
- deployment/config/secrets

## 8. Test Expectations

If a separate implementation dispatch command is later provided, tests must prove:

- BusinessConsumer cannot append capture artifact.
- BusinessConsumer cannot append trusted evidence.
- CaptureAgent cannot create/read/complete/package-read.
- TrustedAdapter cannot create/read/complete/package-read.
- Cross-client access remains denied.
- Operator/Admin/System do not gain runtime behavior.
- Completion authority remains current LocalDev behavior.
- No public DTO/API JSON/status/error behavior changes.
- No SignFlow runtime dependency.

## 9. STOP/RRI Questions

Stop before implementation if any answer is unclear:

| Gate | Question | Default recommendation |
| --- | --- | --- |
| Implementation dispatch | Has a separate implementation dispatch command been provided? | No implementation without a separate dispatch command. |
| Public contract | Does any change alter public API routes, request/response DTOs, JSON shape, status codes, error envelopes, completion response, package summary, notification projection payload, or public contract behavior? | Out of scope for Option A; requires a separate reviewed TIP/kickoff. |
| Production auth | Does the implementation add or imply production auth provider/credential lifecycle? | Forbidden in Option A. |
| Persistence | Does the implementation add DB/EF/migrations/durable persistence? | Forbidden in Option A. |
| Operator/Admin/System | Does the implementation grant new runtime Operator/Admin/System behavior? | STOP/RRI. |
| Completion authority | Does the implementation change who can complete sessions? | Keep current LocalDev behavior; production authority remains STOP/RRI. |
| SignFlow | Does the implementation reference SignFlow runtime/source/database/network/package/internal model? | Forbidden. |

## 10. Review Checklist

Reviewers should verify:

- Current LocalDev auth is preserved.
- BusinessConsumer, CaptureAgent, and TrustedAdapter remain separated.
- Operator/Admin/System behavior is reserved or STOP/RRI.
- BusinessConsumer cannot submit capture artifacts or trusted evidence results.
- CaptureAgent cannot read business summaries/packages or complete sessions.
- TrustedAdapter cannot read business summaries/packages or complete sessions.
- Cross-client access remains forbidden.
- Completion notification remains app-service projection only and does not imply public webhook delivery.
- No public API routes, request/response DTOs, JSON shape, status codes, error envelopes, completion response, package summary, notification projection payload, or public contract behavior changes are introduced.
- No runtime work is authorized by the accepted kickoff alone.
- No production auth, credential store, durable persistence, webhook/outbox/retry, crypto/signing/replay, provider/vendor, production/pilot readiness, or SignFlow runtime dependency is introduced.

## 11. Acceptance Criteria

TIP-13 Option A kickoff is acceptable when:

- Scope is limited to application authorization boundary hardening using current LocalDev auth only.
- Allowed actor/scope/ownership checks are listed.
- Forbidden work is explicit.
- Public API routes, request/response DTOs, JSON shape, status codes, error envelopes, completion response, package summary, notification projection payload, and public contract behavior are explicitly out of scope for Option A.
- Dispatch allowlist and forbidden paths are explicit.
- Test expectations cover actor separation, cross-client denial, reserved Operator/Admin/System behavior, completion authority, public contract stability, and SignFlow runtime dependency absence.
- STOP/RRI gates cover public contract changes, production auth, persistence, Operator/Admin/System behavior, completion authority, and SignFlow boundary.
- Durable persistence remains deferred until actor/scope/ownership enforcement is clarified unless homeowner explicitly accepts a narrower exception.
- Reviewers can tell exactly what implementation would be allowed after acceptance.

## 12. Recommended Next Action

Kickoff is accepted. Await a separate implementation dispatch command before any code changes.

Do not implement TIP-13 until a separate dispatch command is provided.
