# TIP-12 Actor Trust, Caller Scopes, and Access Boundary Planning v0.1

**File:** `docs/tips/tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md`
**Version:** 0.2
**Status:** Accepted - planning only
**Date:** 2026-06-12
**Baseline:** `1baaf6be2ee3a71fcc990ae501f21f7bd62bdbc4`
**Purpose:** Opens TIP-12 as a planning-only actor trust, caller scope, ownership, and access-boundary brief before durable persistence, vault lifecycle, production auth, webhook/outbox, provider trust, or production readiness work proceeds.

## Changelog

### v0.2 - Planning accepted

- Recorded GPT Gate acceptance of TIP-12 planning.
- Preserved TIP-12 as planning-only with no kickoff and no implementation.
- Recorded TIP-13 `Application Authorization Boundary Foundation` as the next safe kickoff/planning direction only.
- Reconfirmed durable persistence remains deferred until after actor/scope/ownership enforcement is clarified unless homeowner explicitly accepts a narrower exception.

### v0.1 - Initial planning draft

- Opened TIP-12 as planning-only.
- Defined actor inventory, caller scope model, ownership/access matrix, audit actor identity model, and cross-client boundary rules.
- Recorded LocalDev-only trust versus future production trust boundaries.
- Mapped dependencies that must wait for TIP-12 findings.
- Preserved no implementation, no kickoff, no `src/**` or `tests/**` changes, no production auth, no credential lifecycle, no durable persistence, no webhook/outbox/retry, no crypto/signing/replay, no vendor selection, no pilot/production readiness claim, and no SignFlow runtime dependency.

## 1. Purpose

TIP-12 defines the actor trust, caller scopes, ownership, and access-boundary model that must exist before TagEkyc hardens durable persistence, vault lifecycle, production auth/client trust, webhook/outbox/retry, provider trust, or production readiness work.

TIP-12 is planning only. It does not authorize runtime implementation, source changes, test changes, production credential handling, database work, provider selection, or production readiness claims.

Durable Persistence Foundation Planning is blocked on TIP-12 actor/ownership findings unless homeowner explicitly accepts a narrower exception.

## 2. Baseline Inputs

- `docs/00_README.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/lld_04_engine_adapter_contracts_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/signflow_integration_contract_v0_1.md`
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/tips/tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md`
- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md`
- `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md`
- Current LocalDev runtime trust shape in `src/TagEkyc.Application/AuthenticatedClientContext.cs`, `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs`, and `src/TagEkyc.Domain/AuditEvent.cs`.

## 3. Current S1/S2 State

S1 is closed as LocalDev evidence-ready, non-production, and non-certified. Current runtime behavior uses LocalDev API-key records, in-memory policy, caller category, scopes, and client application ids to prove narrow S1 boundaries.

TIP-11 Option B added domain/application metadata boundary primitives, including internal actor/data-boundary metadata, without exposing metadata to BusinessConsumer outputs and without adding durable persistence.

Current state is not production trust:

- API keys are LocalDev/test material, not production secrets.
- Caller categories exist, but production onboarding, credential lifecycle, device trust, adapter trust, admin separation, and operator review semantics are not implemented.
- Cross-client denial exists for current session access paths, but full production authorization policy is not complete.
- Audit events can record actor type/id fields, but production actor identity semantics are not final.
- Durable persistence, vault lifecycle, webhook/outbox/retry, production auth, credential stores, provider trust, and production crypto remain deferred.

## 4. Actor Inventory

| Actor | Planning definition | Current S1 LocalDev posture | Future production posture |
| --- | --- | --- | --- |
| BusinessConsumer | External client application that creates sessions and consumes sanitized summaries/packages for its own subjects and sessions. | LocalDev API key can create/read/complete scoped sessions for one client application. | Production client principal with tenant ownership, scopes, policy, credential lifecycle, webhook subscription ownership, and audited access. |
| CaptureAgent | SDK, browser/mobile capture component, device gateway, or agent allowed to append capture artifact metadata for an allowed client/session. | LocalDev scoped key can append capture artifact metadata for an allowed client id. | Device/agent identity with enrollment, allowed client bindings, capture source/device policy, and audit identity. |
| TrustedAdapter | Internal or separately trusted evidence processor allowed to append sanitized evidence results derived from capture artifacts. | LocalDev scoped key can append trusted evidence results for an allowed client id. | Trusted adapter identity with provider/device binding, allowed evidence result types, input artifact constraints, and versioned trust policy. |
| Operator | Privileged operational reviewer or support actor. | Reserved only; no production operator console or operator-specific access model. | Human or service-desk actor with tightly scoped read/review/support permissions, reason codes, and high-audit requirements. |
| Admin | Privileged tenant/platform administrator. | Folded into `OperatorAdmin` category as a reserved LocalDev concept; no production admin APIs. | Separate administrative actor for client policy, actor credentials, scopes, and lifecycle actions; every action audited. |
| System/InternalService | TagEkyc-owned internal service, scheduler, package builder, or projection generator acting under system authority. | Application services perform internal state transitions/projections without production service identity. | Service principal with least-privilege internal scopes, deterministic audit actor identity, and no business-tenant impersonation by default. |
| SignFlow external consumer profile | A BusinessConsumer-style external caller using TagEkyc contracts for transaction-bound verification. | Documented profile and contracts only. | May be onboarded as a BusinessConsumer client profile, but remains external and must not become a TagEkyc runtime/source/database/network/package/internal-model dependency. |

## 5. Caller Scope Model

TIP-12 planning separates actor category, tenant ownership, operation scope, and data classification.

Proposed scope families for future review:

| Scope family | Example scope names | Actor categories | Notes |
| --- | --- | --- | --- |
| Business session | `business.session.create`, `business.session.read` | BusinessConsumer, SignFlow external consumer profile | Bound to owned `clientApplicationId`; no cross-client reads. |
| Business completion | `session.complete` | BusinessConsumer by current S1 behavior; future may split to BusinessConsumer/System/InternalService | Requires STOP/RRI before deciding whether external consumers or only internal services finalize sessions in production. |
| Capture write | `capture.artifact.append` | CaptureAgent | Must bind capture actor to allowed client/session/device/capture source. |
| Trusted evidence write | `trusted.evidence.append` | TrustedAdapter | Must not be granted to ordinary BusinessConsumer credentials. |
| Evidence package read | `evidence.package.read` | BusinessConsumer, Operator, System/InternalService | BusinessConsumer gets sanitized package only for owned sessions; internal manifest access is separate. |
| Completion projection read | `completion.projection.read` | BusinessConsumer, System/InternalService | For polling/projection outputs; webhook delivery remains deferred. |
| Internal manifest read | `internal.manifest.read` | Operator, Admin, System/InternalService | High-risk; requires explicit policy, audit, and no raw artifact exposure by default. |
| Audit read | `audit.read` | Operator, Admin, System/InternalService | Requires reason/audit and tenant boundary. |
| Client policy admin | `client.policy.admin` | Admin | Future production only. |
| Actor credential admin | `actor.credentials.manage` | Admin | Future production only; must not be added as LocalDev credential store in TIP-12. |
| Cross-client support | `support.cross_client.lookup` | Operator/Admin only after STOP/RRI | Forbidden by default; requires explicit support policy, reason codes, and audit. |

Scope rules:

- Scope is necessary but not sufficient; actor category, tenant binding, session ownership, data classification, and operation state must also pass.
- BusinessConsumer credentials must not append trusted evidence, manage credentials, administer policy, or read cross-client data.
- CaptureAgent credentials must not retrieve business summaries or evidence packages unless a future user-facing capture flow explicitly designs that access.
- TrustedAdapter credentials must not read business outputs or complete sessions unless a future internal service design explicitly separates adapter write from finalization authority.
- Operator/Admin access must be privileged, audited, and blocked from raw sensitive payload exposure unless a later accepted evidence-access policy allows it.

## 6. Ownership and Access Matrix

Legend:

- `S1`: Allowed in current S1 LocalDev.
- `Future`: Future allowed after production trust design and implementation.
- `Forbidden`: Must remain forbidden.
- `STOP/RRI`: Deferred; requires explicit homeowner/review risk intake before implementation.

| Operation | BusinessConsumer | CaptureAgent | TrustedAdapter | Operator | Admin | System/InternalService | SignFlow external consumer profile |
| --- | --- | --- | --- | --- | --- | --- | --- |
| create verification session | S1, own client only | Forbidden | Forbidden | STOP/RRI | STOP/RRI | Future | S1 as BusinessConsumer profile if onboarded |
| get verification session | S1, own client only | Forbidden | Forbidden | Future with audit | Future with audit | Future | S1 as BusinessConsumer profile if owner |
| append capture artifact metadata | Forbidden | S1, allowed client/session only | STOP/RRI only if adapter also captures | Forbidden | Forbidden | Future internal import only | Forbidden |
| append trusted evidence result | Forbidden | Forbidden | S1, allowed client/session only | Forbidden | Forbidden | Future internal adapter pipeline | Forbidden |
| complete/finalize session | S1 today for own client with scope | Forbidden | Forbidden unless future internal finalizer | STOP/RRI for manual review outcome | STOP/RRI for override/admin finalization | Future recommended finalizer candidate | S1 as BusinessConsumer profile if owner |
| get evidence package summary | S1, own client only after package exists | Forbidden | Forbidden | Future with audit | Future with audit | Future | S1 as BusinessConsumer profile if owner |
| get completion notification projection | S1 app-service projection only for owned completed-session output; no public route, webhook delivery, subscription, outbox, or retry is implemented | Forbidden | Forbidden | Future support read with audit | Future support read with audit | S1 internal projection generation | S1 as BusinessConsumer profile only if owner; no webhook/public delivery implied |
| access internal manifest | Forbidden | Forbidden | Forbidden | STOP/RRI, audited restricted access | STOP/RRI, audited restricted access | Future package builder/audit service | Forbidden |
| read audit events | Forbidden | Forbidden | Forbidden | Future with reason/audit | Future with reason/audit | Future | Forbidden |
| administer client policy | Forbidden | Forbidden | Forbidden | Forbidden | Future | STOP/RRI for automation | Forbidden |
| manage actor credentials | Forbidden | Forbidden | Forbidden | Forbidden | Future | STOP/RRI for automation | Forbidden |
| cross-client lookup | Forbidden | Forbidden | Forbidden | STOP/RRI support-only | STOP/RRI admin-only | STOP/RRI support automation only | Forbidden |

Initial TIP-12 findings:

1. A verification session is owned by exactly one `clientApplicationId`, with optional subject and transaction correlation under that client.
2. A BusinessConsumer may create a session only for its own authenticated client policy.
3. A BusinessConsumer may retrieve only sanitized summaries for sessions owned by its client application.
4. Capture artifact metadata should be appended only by scoped CaptureAgent identities bound to the owning client/session/device policy.
5. Trusted evidence results should be appended only by scoped TrustedAdapter identities or internal services authorized for the result type and input artifacts.
6. Completion authority is not final for production. Current S1 lets a BusinessConsumer complete its own session, but TIP-12 recommends STOP/RRI before deciding whether production completion belongs to BusinessConsumer, System/InternalService, Operator review, or a split model.
7. Evidence package outputs must be sanitized for BusinessConsumer reads and owned by the session's client application.
8. Completion notification/projection outputs are owned by the session's client application; webhook delivery remains blocked until durable state, subscription trust, signing/replay, and outbox planning are accepted.
9. Actor identity in audit must record actor category, actor id/principal id, client application context, credential/key reference where safe, request/correlation ids, and event type without storing raw secrets.
10. BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, and Admin differ by trust source, allowed write/read surfaces, tenant boundary, data classification, and audit intensity.
11. Cross-client access is forbidden by default.
12. LocalDev-only trust is a proof of shape, not production trust.
13. Production auth/client trust implementation blocks real credentials, multi-tenant support, operator/admin tools, production capture/adapter onboarding, and support lookup.
14. Durable persistence assumptions about tenant keys, session owner columns, actor foreign keys, credential references, audit event schema, and admin/support access must wait for TIP-12 findings.

## 7. Audit Actor Identity Model

Audit actor identity should be stable enough for future durable audit and narrow enough to avoid secret leakage.

Candidate audit identity fields:

| Field | Purpose | Boundary |
| --- | --- | --- |
| `actorCategory` | BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, System/InternalService. | Required for every audited operation. |
| `actorId` | Stable actor/principal/service id. | Required in production; LocalDev may use key/category-derived ids. |
| `clientApplicationId` | Tenant/owner context of the operation. | Required; must not be body-supplied without credential validation. |
| `credentialRef` | Safe reference such as API key id/key prefix, certificate id, OAuth client id, service principal id. | Raw secrets forbidden. |
| `allowedClientApplicationIds` | Support for agents/adapters bound to one or more client applications. | Must be explicit; no wildcard except STOP/RRI. |
| `sessionId` | Session affected by the operation. | Required when session-scoped. |
| `operation` / `eventType` | What happened. | Required. |
| `authorizationDecision` | Allowed/denied and reason code. | Required for sensitive denied attempts and privileged reads. |
| `requestId` / `correlationId` | Support and traceability. | Required where available. |
| `reasonCode` | Operator/admin/support reason. | Required for privileged/cross-client/read-audit operations. |
| `payloadHash` / `payloadRef` | Integrity reference without raw payload. | No raw secrets, raw artifacts, or biometric data. |

Audit rules:

- Denied cross-client access should be audited when an authenticated caller is known.
- Operator/Admin reads of audit, internal manifest, or restricted refs require reason codes.
- System/InternalService events must identify the service, not impersonate a BusinessConsumer by default.
- Production audit identity cannot rely on LocalDev API key strings or mutable display names.

## 8. Cross-Client Boundary Rules

Default rule:

```text
An actor may access only sessions, evidence package summaries, completion projections, and audit views owned by its allowed client application set.
```

Forbidden by default:

- BusinessConsumer reading another client's session, package, notification projection, audit events, internal manifest, or policy.
- CaptureAgent or TrustedAdapter using an allowed client binding to discover other client sessions.
- SignFlow-specific callers accessing generic TagEkyc internals outside their BusinessConsumer-owned sessions.
- Operator/Admin broad search without explicit production support policy, reason code, audit event, and data minimization.
- Any actor reading raw artifacts, biometrics, VaultRefs, or internal manifests through BusinessConsumer endpoints.

Future support exceptions require STOP/RRI and must define:

- who may search;
- which identifiers may be searched;
- whether subject refs are hashed or plaintext;
- what reason code is required;
- what audit event is emitted;
- what output fields are minimized;
- how tenant boundaries are preserved.

## 9. LocalDev vs Production Trust Boundary

| Area | Current LocalDev | Future production requirement |
| --- | --- | --- |
| Credential material | Static LocalDev API key values in runtime policy source. | Secret store or identity provider; hashed/rotated/revoked credentials; no raw secret storage. |
| Actor categories | `BusinessConsumer`, `CaptureAgent`, `TrustedAdapter`, `OperatorAdmin`. | Separate BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System/InternalService identities. |
| Scopes | String scopes checked in LocalDev auth/application services. | Versioned scope catalog, least privilege, policy-backed grants, and reviewed admin lifecycle. |
| Client ownership | `clientApplicationId` from LocalDev key; cross-client denial for current paths. | Durable tenant ownership, indexed policy, support exceptions, audit, and migration-safe schema. |
| Capture/device trust | Allowed capture agent ids and client ids are LocalDev lists. | Device/SDK/gateway enrollment, attestation where needed, capture source policy, and revocation. |
| Adapter trust | LocalDev TrustedAdapter key can append evidence. | Provider/adapter registration, allowed result types, adapter version trust, and evidence input constraints. |
| Operator/Admin | Reserved, not implemented. | Privileged roles, support workflows, reason codes, approval gates, and stronger audit. |
| Audit identity | Actor type/id fields exist; identity semantics not final. | Stable actor/service/credential references and denied-attempt audit policy. |
| Production readiness | Not claimed. | Requires separate accepted implementation, validation, security/legal review, and no certification claim unless approved. |

## 10. Dependency Map

| Dependency | TIP-12 relationship |
| --- | --- |
| Durable persistence | Blocked until session owner, actor identity references, credential refs, tenant boundary, support exceptions, and audit schema assumptions are accepted. Durable Persistence Foundation Planning is blocked on TIP-12 actor/ownership findings unless homeowner explicitly accepts a narrower exception. |
| Vault lifecycle | Blocked until actors allowed to create/read/revoke VaultRef metadata and restricted access audit rules are defined. |
| Webhook/outbox/retry | Blocked until subscription owner, completion projection reader, delivery actor, client trust, signing/replay, and outbox tenant boundary are defined. |
| Production auth/client trust | Direct dependent implementation; TIP-12 should become the source for scope/actor acceptance criteria. |
| Adapter/device trust | Blocked until CaptureAgent and TrustedAdapter identity, allowed client bindings, device policy, provider trust, and evidence result authority are accepted. |
| Provider/vendor readiness | Blocked until TrustedAdapter authority, evidence result provenance, actor audit identity, and provider trust boundaries are defined. |
| Production crypto/signing | Blocked until signing actor/service principal, key ownership, evidence package authority, webhook subscription ownership, replay actor identity, and audit semantics are defined. |

## 11. Candidate Next TIPs After TIP-12

| Candidate | Scope | Notes |
| --- | --- | --- |
| TIP-13 - Application Authorization Boundary Foundation | Draft and review application authorization boundary hardening using current LocalDev auth only. | Selected next kickoff/planning direction. No production auth provider, credential store, or implementation is opened by TIP-12. |
| TIP-14 - Durable Persistence Foundation Re-plan | Revisit durable metadata schema after TIP-12 owner/actor findings. | Must not proceed unless homeowner accepts TIP-12 findings or a narrower exception. |
| TIP-15 - Adapter and Device Trust Planning | Define CaptureAgent/TrustedAdapter onboarding, allowed result types, device identity, and provider trust. | Should follow TIP-12 actor model. |
| TIP-16 - Operator/Admin Access and Support Workflow Planning | Define privileged read, audit-read, support lookup, reason code, and admin policy management model. | Required before operator console/admin APIs. |
| TIP-17 - Webhook Subscription Trust, Outbox, and Signing Planning | Define subscription owner, delivery actor, retry/outbox tenant boundary, signing and replay prerequisites. | Depends on durable persistence and production auth. |

## 12. STOP/RRI Questions

| Gate | Question | Default recommendation |
| --- | --- | --- |
| Completion authority | Should BusinessConsumer continue to finalize sessions in production, or should finalization move to System/InternalService after evidence readiness? | STOP/RRI before production. Keep current S1 behavior only as LocalDev. |
| Operator access | May operators read session summaries, packages, internal manifests, or audit events? | Defer until operator/support policy exists. |
| Admin access | May admins read evidence content, or only administer policy/credentials? | Prefer policy/credential administration only; evidence reads require separate reason/audit policy. |
| Cross-client lookup | Should support/admin actors ever search across clients? | Forbidden by default; require explicit STOP/RRI. |
| CaptureAgent ownership | Are CaptureAgents bound to one client, many clients, a device, a user, or a session handoff token? | Defer until device/capture trust planning. |
| TrustedAdapter authority | Which adapters may append which result types, and who attests provider output? | Defer until adapter/provider trust planning. |
| Audit denied attempts | Which denied operations must append audit events? | Require for authenticated sensitive/privileged/cross-client attempts. |
| Credential lifecycle | What identity provider, key store, rotation, revocation, and break-glass model is allowed? | Not in TIP-12; plan only. |
| Durable schema exception | May durable persistence planning proceed before TIP-12 is accepted? | No unless homeowner explicitly accepts a narrower exception. |
| SignFlow boundary | Should SignFlow become a runtime/source/database/network/package/internal-model dependency? | No. Keep external consumer profile only. |

## 13. Explicit Non-Goals

TIP-12 does not:

- implement runtime behavior;
- create a kickoff;
- edit `src/**`;
- edit `tests/**`;
- implement production auth;
- implement credential lifecycle, credential store, API key store, OAuth/OIDC, certificates, mTLS, or external identity provider;
- add database, EF, DbContext, migrations, schema, or durable persistence;
- add webhook, outbox, retry, delivery ledger, subscription management, or dispatcher;
- add crypto, signing, replay protection, HMAC, JWS, KMS, HSM, or key rotation;
- select a vendor, provider, engine, database, vault, cloud service, or legal/compliance provider;
- claim legal/compliance/certification readiness;
- claim pilot readiness;
- claim production readiness;
- expose raw artifacts, biometrics, internal VaultRefs, or internal manifests to BusinessConsumer outputs;
- introduce SignFlow runtime, source, database, network, package, deployment, or internal-model dependency.

SignFlow remains an external consumer profile. TIP-12 may describe SignFlow as a BusinessConsumer-style external caller using TagEkyc contracts, but it must not introduce SignFlow code, database, runtime package, deployment, network dependency, or internal model dependency.

## 14. Acceptance Criteria

TIP-12 planning is acceptable when:

- Actor inventory covers BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, System/InternalService, and SignFlow external consumer profile.
- Caller scope model separates actor category, scopes, ownership, and data classification.
- Ownership/access matrix includes create session, get session, append capture artifact metadata, append trusted evidence result, complete/finalize session, get evidence package summary, get completion notification projection, access internal manifest, read audit events, administer client policy, manage actor credentials, and cross-client lookup.
- Each matrix operation is classified as current S1 LocalDev allowed, future production allowed, forbidden, or deferred/STOP-RRI for each actor.
- Audit actor identity model identifies minimum actor, credential, tenant, request/correlation, event, and reason-code fields without raw secret leakage.
- Cross-client access is forbidden by default with explicit STOP/RRI for support exceptions.
- LocalDev-only trust is clearly separated from future production trust.
- Production auth/client trust blockers are listed.
- Durable persistence assumptions are blocked until TIP-12 actor/ownership findings are accepted, unless homeowner explicitly accepts a narrower exception.
- Non-goals preserve planning-only scope and prohibit runtime/source/test/auth/credential/database/webhook/crypto/vendor/readiness/SignFlow dependency creep.
- Governance docs identify TIP-12 as accepted planning only.
- Validation is run with `dotnet test TagEkyc.sln --no-restore` and reported by the agent final response.

## 15. Recommended Next Action

TIP-12 planning is accepted as planning-only.

Prepare a separate reviewed kickoff for:

```text
TIP-13 - Application Authorization Boundary Foundation
```

Selected candidate:

```text
Option A - Application authorization boundary hardening using current LocalDev auth only.
```

Do not dispatch durable persistence, vault lifecycle, webhook/outbox/retry, production auth, credential lifecycle, crypto/signing, provider/vendor readiness, pilot readiness, production readiness, or SignFlow runtime dependency work from TIP-12.
