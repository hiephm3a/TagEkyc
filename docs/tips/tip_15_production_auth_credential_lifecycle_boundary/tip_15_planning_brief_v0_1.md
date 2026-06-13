# TIP-15 Production Auth / Credential Lifecycle Boundary Planning Brief v0.1

**File:** `docs/tips/tip_15_production_auth_credential_lifecycle_boundary/tip_15_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - planning only
**Date:** 2026-06-14
**Baseline:** `7eed6e12f0996f3e8b71beaf124660ac5488565b`
**Purpose:** Defines the production authentication and credential lifecycle boundary for TagEkyc without implementing production authentication, credential storage, or identity-provider integration.

## Changelog

### v0.1 - Initial planning draft

- Opened TIP-15 as a docs-only production auth / credential lifecycle boundary brief.
- Separated current LocalDev API-key trust from future production authentication.
- Defined production actor categories and credential-bearing principal posture for BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System/InternalService.
- Proposed a planning-level credential reference model with no raw secret storage assumptions.
- Defined lifecycle requirements for onboarding, activation, rotation, revocation, suspension, expiry, audit trail, and break-glass policy.
- Defined tenant/client ownership and scope grant model requirements for later persistence and authorization work.
- Recorded audit identity requirements for future durable audit and persistence.
- Identified future schema concepts durable persistence must preserve without freezing raw secret or provider-specific identity assumptions.
- Added STOP/RRI topics for identity provider posture, credential backend, hashed secret rules, lifecycle authority, admin/operator authority, break-glass access, cross-client support lookup, System/InternalService authority, production completion authority impact, audit identity, migration/persistence, and legal/compliance exposure.
- Preserved no implementation, no `src/**`, no tests, no public API/DTO/JSON/status/error behavior changes, no DB/EF/migrations, no credential store, no secret backend, no identity-provider integration, no OAuth/OIDC/mTLS/certificate implementation, no durable persistence, no webhook/outbox/retry, no vault/raw artifact lifecycle, no crypto/signing/replay, no provider/vendor integration, no pilot/production/certification readiness claim, and no SignFlow runtime/source/database/network/package/internal-model dependency.

## 1. Purpose

TIP-15 is planning only. It does not authorize runtime implementation, source changes, tests, public contract changes, persistence work, credential storage, production identity-provider work, secret backend work, OAuth/OIDC/mTLS/certificate work, crypto work, provider work, or production-readiness claims.

The purpose is to define the production authentication and credential lifecycle boundary before any durable persistence implementation freezes identity, credential-reference, tenant ownership, or audit assumptions.

TIP-15 follows accepted planning and implementation state:

- S1 is closed as LocalDev evidence-ready only, non-production, and non-certified.
- TIP-10 production readiness planning is accepted.
- TIP-11 metadata/data-boundary foundation is closed for LocalDev/non-production primitives only.
- TIP-12 actor trust, caller scopes, and access-boundary planning is accepted.
- TIP-13 Option A application authorization boundary is implemented and closed using current LocalDev auth only.
- TIP-14 recommends production auth / credential lifecycle planning before durable persistence implementation.

## 2. Baseline Inputs

- `docs/tips/tip_14_post_tip_13_s2_debt_registry_convergence/tip_14_planning_brief_v0_1.md`
- `docs/tips/tip_12_actor_trust_caller_scopes_access_boundary/tip_12_planning_brief_v0_1.md`
- `docs/tips/tip_13_application_authorization_boundary_foundation/tip_13_closeout_v0_1.md`
- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/tips/README.md`

Baseline check before drafting:

```text
HEAD: 7eed6e12f0996f3e8b71beaf124660ac5488565b
Worktree: clean
Latest validation supplied by user: dotnet test TagEkyc.sln --no-restore = 81 passed, 0 failed, 0 skipped
```

## 3. Current LocalDev Auth Boundary

Current LocalDev auth is a proof of actor/scope/ownership shape only.

LocalDev posture:

- Static LocalDev API-key records can identify caller category, scopes, and allowed client application ids for the current S1/S2 non-production boundary.
- TIP-13 centralized LocalDev actor/category/scope/ownership enforcement through application authorization.
- LocalDev API-key values are not production secrets.
- LocalDev actor ids, key names, and policy records are not a production identity-provider, credential-store, tenant registry, or lifecycle system.
- Operator/Admin/System production behavior remains reserved or STOP/RRI.

Production posture must be different:

- Production credentials require onboarding, activation, rotation, revocation, suspension, expiry, audit trail, owner assignment, and administrative authority.
- Production identity must support stable actor/principal references that survive secret rotation.
- Production audit must reference actors and credential refs without persisting raw secrets.
- Production credential posture must be selected through STOP/RRI before implementation.

## 4. Production Actor Categories

TIP-15 retains the TIP-12 actor inventory and separates actor category, principal identity, credential reference, tenant/client ownership, and scopes.

| Actor | Production planning definition | Credential-bearing posture | Default production status |
| --- | --- | --- | --- |
| BusinessConsumer | External client application that creates sessions and reads sanitized outputs for owned sessions. | May have production credentials after onboarding, tenant/client binding, scope grant, lifecycle owner, and audit identity are accepted. | Allowed candidate, not implemented. |
| CaptureAgent | SDK, mobile/browser capture component, device gateway, or capture service allowed to append capture artifact metadata for allowed clients/sessions. | May have production credentials only after capture/device enrollment, allowed client binding, source policy, and revocation model are accepted. | STOP/RRI for device/capture trust details. |
| TrustedAdapter | Trusted evidence processor or provider adapter allowed to append sanitized evidence results. | May have production credentials only after adapter/provider registration, allowed result types, input constraints, and revocation authority are accepted. | STOP/RRI for provider/adapter trust details. |
| Operator | Privileged human or support actor for operational review or support access. | Reserved until privileged support workflow, reason codes, approval gates, and cross-client policy are accepted. | STOP/RRI; no default credential. |
| Admin | Privileged tenant/platform administrator for policy, credential, scope, and lifecycle administration. | Reserved until admin authority, separation of duties, lifecycle authority, and audit requirements are accepted. | STOP/RRI; no default credential. |
| System/InternalService | TagEkyc-owned service principal, scheduler, policy evaluator, package builder, projection generator, or automation. | May require production service credentials, but authority and impersonation rules must be accepted first. | STOP/RRI for service-principal authority. |

SignFlow remains an external BusinessConsumer-style profile only if onboarded as a TagEkyc client. TIP-15 does not introduce any SignFlow runtime, source, database, network, package, or internal-model dependency.

## 5. Credential Eligibility and Reserved Actors

| Actor | May hold production credentials? | Required before credentials exist | Forbidden assumption |
| --- | --- | --- | --- |
| BusinessConsumer | Yes, after accepted production auth kickoff. | Tenant/client registration, credential reference model, allowed scopes, lifecycle owner, rotation/revocation authority, audit identity. | Do not treat LocalDev API keys as production secrets or durable credential ids. |
| CaptureAgent | Yes, but only after device/capture trust RRI. | Capture enrollment, allowed client/session binding, device/source posture, revocation model, audit identity. | Do not let capture credentials read business summaries or submit trusted evidence by default. |
| TrustedAdapter | Yes, but only after adapter/provider trust RRI. | Adapter registration, provider trust posture, allowed result types, input artifact constraints, revocation model, audit identity. | Do not let BusinessConsumer credentials submit trusted `PASSED` evidence. |
| Operator | Reserved. | Privileged support workflow, reason codes, access minimization, approval/dual-control policy if needed, audit review. | Do not grant broad cross-client lookup or raw artifact/biometric access by default. |
| Admin | Reserved. | Administrative authority model, separation from Operator, credential lifecycle authority, tenant/platform scope, audit trail. | Do not allow unreviewed admin evidence reads or unaudited credential changes. |
| System/InternalService | Reserved candidate. | Service-principal registry, least-privilege internal scopes, tenant-context rules, no default impersonation, production completion authority decision. | Do not let internal services bypass tenant boundaries without explicit audited authority. |

Planning recommendation:

```text
Production credential-bearing principals should be modelled as stable principal records plus separate credential references.
Credential values are replaceable lifecycle artifacts and must not be the actor identity.
```

## 6. Credential Reference Model

TIP-15 defines credential references only at planning level. It does not create a credential store, secret backend, schema, code, API, or migration.

Future production credential references should preserve these concepts:

| Concept | Purpose | Boundary |
| --- | --- | --- |
| `principalId` | Stable actor/service identity independent of credential rotation. | Must not be a raw API key, token, certificate private key, or mutable display name. |
| `actorCategory` | BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, System/InternalService. | Required for authorization and audit. |
| `credentialRef` | Safe reference to a credential artifact, such as key id, OAuth client id, certificate id, service principal id, or secret version id. | Raw secrets forbidden. Provider-specific shape must remain behind an abstraction until selected. |
| `credentialType` | Planning classification: managed API key, OAuth/OIDC client, mTLS/certificate, service principal, or other approved type. | STOP/RRI before selection or implementation. |
| `credentialStatus` | Planned lifecycle state: pending, active, suspended, revoked, expired, rotated/replaced. | Required for future authorization decisions. |
| `tenantId` / `clientApplicationId` | Owner context and allowed client binding. | Must be derived from trusted registration, not caller body input alone. |
| `scopeGrantSetId` | Versioned or reviewable scope grants attached to the principal/credential. | Scope is necessary but not sufficient; ownership and actor category still apply. |
| `issuedAt` / `activatedAt` / `expiresAt` / `revokedAt` / `rotatedAt` | Lifecycle timestamps. | Should be durable audit facts, not inferred from raw secret material. |
| `issuedBy` / `revokedBy` / `rotatedBy` | Actor/admin/system authority for lifecycle changes. | Requires admin/operator authority RRI. |
| `credentialFingerprint` | Non-secret identifier for diagnostics, such as prefix, hash id, certificate fingerprint, or provider id. | Must not enable credential reconstruction or leakage. |
| `lastUsedAt` / `lastUsedFrom` | Optional operational telemetry for risk review and cleanup. | Must avoid logging raw secrets, biometric payloads, or sensitive evidence. |

Credential references must support rotation without changing `principalId` and must support revocation without deleting audit history.

## 7. Identity Provider Posture Options

TIP-15 does not select a production identity provider. The following options remain STOP/RRI:

| Option | Candidate use | Advantages | Risks / decisions |
| --- | --- | --- | --- |
| Managed API keys | BusinessConsumer, CaptureAgent, TrustedAdapter, possibly service principals. | Simple integration and LocalDev continuity. | Requires secret backend, hashed storage rules, rotation/revocation tooling, leak response, admin lifecycle. |
| OAuth/OIDC clients | BusinessConsumer, Admin/Operator, service-to-service clients. | Standards-based delegation, external IdP compatibility, short-lived tokens. | Requires IdP selection, client registration, token validation, claims mapping, tenant binding, key rollover policy. |
| mTLS/certificates | TrustedAdapter, device gateways, high-trust service-to-service paths. | Strong channel/client binding. | Requires PKI/certificate authority, issuance, renewal, revocation, device binding, operational complexity. |
| Service principals | System/InternalService and possibly trusted backend adapters. | Clear internal identity and least-privilege internal scopes. | Requires service registry, deployment identity, no default tenant impersonation, audit semantics. |
| Hybrid posture | Different credential types by actor class. | Allows fit-for-purpose identity per actor. | Increases migration, audit, lifecycle, and support complexity. |

Default planning recommendation:

```text
Keep production auth provider selection open. Use provider-neutral principalId, credentialRef, credentialType, credentialStatus, tenant binding, scope grants, and audit identity concepts in future schema planning.
```

## 8. Credential Lifecycle Requirements

Future production auth implementation must include explicit lifecycle behavior before real credentials are issued.

| Lifecycle stage | Requirement | STOP/RRI owner decision |
| --- | --- | --- |
| Onboarding | Register tenant/client, actor category, principal, allowed client bindings, initial scope grants, lifecycle owner, and audit identity. | Who approves BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System principals? |
| Activation | Credentials start inactive/pending until approved and activated. Activation must be audited. | May activation be self-service, admin-only, or dual-control for privileged actors? |
| Rotation | Rotation must create a new credential ref or version without changing stable principal identity. Old credential retirement window must be explicit. | Who may rotate, what overlap is allowed, and how consumers are notified? |
| Revocation | Revocation must disable future use and preserve audit history. Emergency revocation must exist for leaked credentials. | Which Admin/System authority can revoke and what evidence/reason is required? |
| Suspension | Temporary disablement should be separate from irreversible revocation. | Who may suspend and reactivate? What reason codes are required? |
| Expiry | Credentials should support explicit expiry and renewal policy. | Which actor categories require expiry? Are non-expiring credentials forbidden? |
| Audit trail | Every lifecycle action must record actor, authority, credentialRef, principalId, tenant/client context, reason, request/correlation id, and timestamp. | Which lifecycle events are externally reportable or legally retained? |
| Break-glass | Disabled by default until accepted. If allowed, must be time-bound, reason-coded, approval-gated, tenant-minimized, and heavily audited. | Is break-glass permitted at all? Who approves and reviews it? |

Lifecycle non-negotiables:

- Raw secrets must not be written to audit logs.
- Credential values must not be used as stable actor identity.
- Revoked credentials must not erase historical audit identity.
- Privileged lifecycle actions require reason codes and audit review.
- LocalDev credentials must not be promoted or migrated as production secrets.

## 9. Tenant / Client Ownership and Scope Grants

Production authorization must derive tenant/client ownership from trusted identity registration and credential binding, not from request body claims alone.

Future model concepts:

| Concept | Requirement |
| --- | --- |
| Tenant | Legal/organizational owner boundary for one or more client applications. Exact tenant model remains STOP/RRI until commercial/legal posture is accepted. |
| Client application | Business application boundary that owns sessions, policies, package summaries, completion projections, and webhook subscriptions if added later. |
| Principal | Stable identity for an actor or service. A principal may be bound to one tenant, one client application, or an explicit allowed client set. |
| Credential ref | Authentication artifact reference linked to a principal and lifecycle state. |
| Scope grant | Least-privilege permission set attached to principal/credential context and constrained by actor category and tenant/client bindings. |
| Grant version | Optional future versioned record for auditability when scopes or bindings change. |
| Support exception | Explicit privileged access exception with reason, approval, minimization, and audit. Forbidden by default. |

Scope grant rules:

- Scope is necessary but not sufficient.
- Actor category must match the operation.
- Tenant/client ownership must match the target session or resource.
- BusinessConsumer cannot append capture artifacts, append trusted evidence, manage credentials, read audit, or perform cross-client support lookup.
- CaptureAgent cannot read business outputs or append trusted evidence by default.
- TrustedAdapter cannot read business outputs, complete sessions, or manage credentials by default.
- Operator/Admin privileges must be separated and audited.
- System/InternalService cannot impersonate a tenant actor by default.

## 10. Audit Identity Requirements

Future durable audit must preserve production identity without leaking secrets.

Minimum audit identity concepts:

| Field | Purpose | Boundary |
| --- | --- | --- |
| `actorCategory` | Identifies actor class for authorization and review. | Required for every authenticated action. |
| `principalId` | Stable actor/service identity. | Required in production. |
| `credentialRef` | Safe credential reference used for the request. | Raw secret, bearer token, private key, and full API key forbidden. |
| `credentialType` | Authentication posture used for the request. | Provider-neutral value until IdP is selected. |
| `tenantId` / `clientApplicationId` | Owner context and allowed client binding. | Required for tenant-scoped operations. |
| `scopeGrantSetId` | Scope grant version/reference used for the decision. | Should be captured for reproducible decisions. |
| `sessionId` / resource id | Target resource. | Required when session/resource-scoped. |
| `operation` / `eventType` | Action attempted or completed. | Required. |
| `authorizationDecision` | Allowed/denied plus reason code for sensitive decisions. | Required for denied sensitive and privileged operations. |
| `reasonCode` | Privileged support, admin, lifecycle, or break-glass reason. | Required for privileged actions. |
| `requestId` / `correlationId` | Traceability across systems. | Required where available. |
| `sourceIp` / client telemetry | Optional risk/support metadata. | Must avoid sensitive payload logging and comply with legal policy. |

Audit identity recommendations:

- Denied authenticated cross-client attempts should be auditable.
- Credential lifecycle changes must be auditable as first-class security events.
- Operator/Admin support reads must carry reason codes and minimized output.
- System/InternalService actions must identify the service principal and should not default to BusinessConsumer impersonation.
- Production completion authority changes must be visible in audit identity and decision records.

## 11. Durable Persistence Concepts to Preserve Later

TIP-15 does not implement durable persistence, DB, EF, migrations, repositories, or schema. It records concepts that a future persistence slice must preserve.

Future durable persistence should be able to represent:

- stable `principalId` independent of credential value;
- actor category per principal;
- credential reference separate from principal identity;
- credential type and status;
- tenant/client ownership and allowed client bindings;
- scope grant set or grant version;
- lifecycle timestamps and lifecycle actor refs;
- revocation/suspension/expiry without deleting audit history;
- reason codes for privileged lifecycle and support operations;
- audit actor identity on every security-sensitive event;
- service-principal identity for System/InternalService actions;
- production completion authority actor if completion moves away from BusinessConsumer;
- provider-neutral identity fields that can support API keys, OAuth/OIDC, certificates/mTLS, and service principals;
- migration path from LocalDev-only records without promoting LocalDev key strings to production secrets.

Persistence must not assume:

- raw API keys are stored in the application database;
- LocalDev API keys are production credential records;
- credential value equals actor id;
- every actor uses the same credential type;
- BusinessConsumer completion authority remains production authority;
- Operator/Admin cross-client access exists by default;
- System/InternalService can bypass tenant ownership without audited authorization.

## 12. LocalDev Compatibility

TIP-15 preserves LocalDev compatibility while keeping LocalDev separate from production auth.

Allowed LocalDev compatibility posture:

- LocalDev may continue to use static development API-key records for current non-production tests and flows.
- LocalDev actor/category/scope/client-binding shape may continue to exercise authorization boundaries.
- Future production-neutral interfaces may map LocalDev credentials to safe `principalId` and `credentialRef` placeholders only in development/test context.

Forbidden LocalDev assumptions:

- LocalDev API-key values are not production secrets.
- LocalDev key names are not durable production credential ids.
- LocalDev policy source is not a production credential store.
- LocalDev key material must not be migrated into production credential storage.
- LocalDev validation does not prove pilot readiness, production readiness, certification, legal reliance, or secure secret lifecycle.

## 13. Production Completion Authority Impact

TIP-15 does not decide production completion authority, but records how auth choices affect it.

Current S1 LocalDev behavior allows BusinessConsumer completion for owned sessions with the current LocalDev scope. That remains LocalDev-only.

Production options remain STOP/RRI:

| Option | Impact |
| --- | --- |
| BusinessConsumer finalizes | Keeps S1 shape, but requires strong evidence gating, client trust, and audit proof that the client cannot self-certify arbitrary evidence. |
| System/InternalService finalizes | Recommended candidate for production if policy evaluation and package creation become internal authority. Requires service-principal identity and audit. |
| Operator review finalizes | Requires privileged workflow, reason codes, manual review policy, tenant minimization, and audit review. |
| Split model | BusinessConsumer requests completion; System/InternalService finalizes after policy/evidence checks. Requires request/decision audit separation. |

No implementation may assume production completion authority until a later accepted RRI or kickoff decides it.

## 14. STOP/RRI Table

| STOP/RRI item | Decision needed | Why it shapes implementation | Candidate options | Forbidden assumptions |
| --- | --- | --- | --- | --- |
| Identity provider posture | Decide managed API keys, OAuth/OIDC, mTLS/certificates, service principals, or hybrid per actor. | Determines credential refs, token validation, lifecycle, audit identity, and tenant binding. | Managed API keys; OAuth/OIDC; mTLS/certificates; service principals; hybrid. | Do not assume LocalDev API keys are production credentials. |
| Credential store or secret backend | Decide where credential secrets, hashes, client secrets, certificates, or provider refs live. | Determines storage, rotation, revocation, operational access, and incident response. | Secret manager; IdP-managed clients; certificate authority; application-managed hashed API key store; no app-managed secrets. | Do not add a credential store or secret backend in TIP-15. |
| Hashed secret storage rules | If app-managed API keys exist, decide hashing, salt/pepper, prefix display, lookup strategy, and rotation windows. | Prevents raw secret persistence and controls leak response. | Hash-only lookup; keyed hash; external secret store; IdP-only credentials. | Do not store raw secrets in DB, config, audit, logs, or docs. |
| Rotation/revocation authority | Decide who may rotate, revoke, suspend, reactivate, and expire credentials. | Shapes Admin/System APIs, audit events, support workflow, and emergency response. | Tenant Admin; platform Admin; security operator; automated expiry; dual control. | Do not allow unaudited lifecycle changes. |
| Admin/operator lifecycle authority | Decide separation between Admin credential management and Operator support/review access. | Prevents support roles from becoming broad security administrators. | Admin-only credential lifecycle; Operator read-only support; dual-control privileged actions. | Do not merge Operator and Admin authority by default. |
| Break-glass access | Decide whether emergency privileged access is allowed. | Affects cross-client lookup, sensitive reads, lifecycle overrides, and legal exposure. | No break-glass; time-boxed dual-control; security-only; tenant-approved. | Do not implement implicit emergency access. |
| Cross-client support lookup | Decide if any privileged actor can search across clients and by which identifiers. | Affects tenant isolation, audit, privacy, and support tooling. | Forbidden; reason-coded limited lookup; approval-gated lookup; tenant-scoped only. | Do not grant broad cross-client search by default. |
| System/InternalService authority | Decide service principal model, internal scopes, tenant context, and impersonation rules. | Shapes policy evaluation, package building, completion, projections, and audit. | Named service principals; workload identity; service account per function; no impersonation by default. | Do not let internal services bypass authorization silently. |
| Production completion authority impact | Decide whether BusinessConsumer, System/InternalService, Operator, or split model finalizes production sessions. | Completion drives final decision, evidence package creation, notifications, and downstream reliance. | BusinessConsumer; System/InternalService; Operator review; split request/finalize model. | Do not treat S1 BusinessConsumer completion as production authority. |
| Audit identity model | Decide required durable audit fields and retention/legal treatment. | Audit schema must survive credential rotation and support incident/legal review. | Provider-neutral principal/credential refs; IdP claims snapshot; scope grant version; reason codes. | Do not log raw secrets or rely on mutable display names. |
| Migration/persistence impact | Decide what identity/credential concepts durable persistence must preserve before DB work. | Prevents schema from freezing LocalDev key assumptions or single-credential-type assumptions. | Provider-neutral schema concepts; deferred provider columns; migration-safe abstractions. | Do not add DB/EF/migrations or production credential tables in TIP-15. |
| Legal/compliance impact if real production credentials are used | Decide legal/compliance posture for real clients, real users, jurisdiction, consent, support access, and incident response. | Real credentials can enable real data processing and regulated reliance. | Engineering-only environment; limited no-reliance pilot; jurisdiction-specific pilot; production track. | Do not issue real production credentials or claim readiness from TIP-15. |

## 15. Hard Boundary / Non-Goal Table

| Boundary | Invariant |
| --- | --- |
| TIP-15 scope | Docs-only planning. It opens no implementation. |
| Runtime/source | No runtime implementation and no `src/**` changes. |
| Tests | No tests are added or changed. |
| Public contracts | No public API, DTO, JSON, status, or error behavior changes. |
| Persistence | No DB, EF, migrations, durable repositories, durable schema, or recovery implementation. |
| Credential store | No credential store, API-key store, secret backend, vault, or production credential records. |
| Identity provider | No production IdP, OAuth/OIDC, mTLS, certificate, PKI, service principal, or token validation implementation. |
| Secret handling | No raw secret, hashed secret, key generation, rotation, revocation, or expiry implementation. |
| Webhook | No webhook route, subscription, outbox, retry, delivery ledger, signing, replay, or dispatcher. |
| Vault/raw artifacts | No vault lifecycle, raw artifact storage, biometric storage, retention enforcement, deletion, or legal hold workflow. |
| Crypto/signing | No crypto/signing/replay implementation, HMAC, JWS, KMS, HSM, certificate, or key-management implementation. |
| Providers/vendors | No provider, vendor, engine, cloud service, certificate authority, legal provider, or paid service selection/integration. |
| Readiness claims | No pilot readiness, production readiness, certified eKYC, external audit reliance, or legal reliance claim. |
| SignFlow | No SignFlow runtime/source/database/network/package/internal-model dependency. SignFlow remains an external consumer profile only. |

## 16. Candidate Next Slice After TIP-15

| Candidate | Verdict | Why | Required guardrails |
| --- | --- | --- | --- |
| Production auth implementation kickoff | Candidate only after TIP-15 review accepts enough STOP/RRI answers. | Directly implements credential-bearing principal boundary, lifecycle, and auth enforcement. | Must be narrow, explicit on provider/store posture, forbid readiness claims, and include tests. |
| Durable persistence foundation planning/kickoff | Recommended next if production auth remains planning-only but identity concepts are accepted. | Persistence can now preserve principal/credential/audit concepts without freezing LocalDev secrets. | Must remain provider-neutral or explicitly approved; no raw secret assumptions; no raw artifact storage. |
| Privileged support workflow planning | Strong candidate if Operator/Admin/cross-client questions remain unresolved. | Operator/Admin lifecycle and support lookup are high-risk before production credentials exist. | Planning-only unless authority, reason codes, approval gates, and audit are accepted. |
| Production completion authority decision | Strong candidate if completion semantics block auth or persistence. | Completion authority affects System/InternalService principal design and audit schema. | Decision-only unless a later kickoff authorizes implementation. |

TIP-15 recommendation:

```text
Next governed slice: durable persistence foundation planning/kickoff only if it preserves TIP-15 provider-neutral principal, credential reference, tenant binding, scope grant, lifecycle, and audit identity concepts without implementing production auth or storing raw secrets.

Fallback if STOP/RRI remains unresolved: privileged support workflow planning or production completion authority decision before production auth implementation kickoff.

Do not start production auth implementation until identity provider posture, credential store/secret backend, hashed secret rules, lifecycle authority, and legal/compliance impact are accepted.
```

## 17. Acceptance Criteria

TIP-15 planning is acceptable when:

- It separates current LocalDev auth from future production auth.
- It defines production actor categories and credential-bearing principals for BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System/InternalService.
- It identifies which actors may have production credentials and which remain reserved or STOP/RRI.
- It defines a planning-level credential reference model without raw secret storage assumptions.
- It defines lifecycle requirements for onboarding, activation, rotation, revocation, suspension, expiry, audit trail, and break-glass policy.
- It defines tenant/client ownership and scope grant model requirements.
- It defines audit identity requirements for future durable persistence.
- It identifies schema concepts durable persistence must preserve without freezing raw secret assumptions.
- It preserves LocalDev compatibility without treating LocalDev API keys as production secrets.
- It records STOP/RRI items for identity provider posture, credential store or secret backend, hashed secret storage rules, rotation/revocation authority, admin/operator lifecycle authority, break-glass access, cross-client support lookup, System/InternalService authority, production completion authority impact, audit identity model, migration/persistence impact, and legal/compliance impact if real production credentials are used.
- It recommends a next governed slice after TIP-15.
- It does not modify `src/**`, tests, public contracts, runtime behavior, DB/EF/migrations, credential store, secret backend, production IdP integration, OAuth/OIDC/mTLS/certificate implementation, durable persistence, webhook/outbox/retry, vault/raw artifact lifecycle, crypto/signing/replay, provider/vendor integration, readiness claims, or SignFlow dependency posture.

## 18. Recommended Review Questions

Homeowner/GPT review should focus on:

1. Are BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, Admin, and System/InternalService credential postures separated clearly enough?
2. Are the credential reference concepts sufficient for a future durable persistence slice without choosing an IdP or secret backend?
3. Should any actor move from STOP/RRI to allowed implementation candidate, especially BusinessConsumer and System/InternalService?
4. Is break-glass forbidden by default acceptable, or should a time-boxed emergency model be planned now?
5. Should the next slice be durable persistence foundation planning/kickoff, privileged support workflow planning, production completion authority decision, or production auth implementation kickoff?

Do not dispatch implementation from TIP-15.
