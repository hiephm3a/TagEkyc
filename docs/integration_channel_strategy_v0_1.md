# TagEkyc Integration Channel Strategy

**File:** `docs/integration_channel_strategy_v0_1.md`
**Version:** 0.1
**Status:** Draft
**Date:** 2026-06-24
**Baseline:** Product Brief v0.1.2, HLD v0.1, LLD API Contracts v0.5, SignFlow Integration Contract v0.1
**Purpose:** Defines the recommended integration posture for business applications, web/mobile clients, local agents, SDKs, and trusted adapters without over-claiming S1 production trust.

## Changelog

### v0.1 - Initial draft

- Separated integration channels from evidence trust boundaries.
- Recommended TagEkyc-owned hosted capture, SDKs, and controlled agents as the default capture path.
- Defined REST/spec-only integration as a contract surface, not a sufficient trust anchor for evidence collection.
- Added roadmap gates for production-grade capture trust, device/session binding, artifact handling, adapter verification, callbacks, and evidence proof.

## 1. Executive Position

TagEkyc SHOULD support multiple integration channels, but it MUST NOT treat every channel as equally trusted.

The preferred production posture is:

```text
Business application backend
  -> creates TagEkyc session and passes business context
  -> launches TagEkyc hosted capture, SDK, or controlled agent
  -> receives or polls sanitized result
  -> verifies evidence package/proof when required

TagEkyc capture surface
  -> owns or controls document, selfie, liveness, NFC, fingerprint, and device capture flow
  -> binds capture to a server-issued session challenge
  -> uploads artifacts or secure references to TagEkyc

TagEkyc server / trusted adapters
  -> perform OCR, NFC validation, face match, liveness, fingerprint, risk, and quality decisions
  -> record evidence results
  -> assemble and sign evidence package/proof
```

Specification-only integration is necessary, but not sufficient. A client that follows a JSON schema is compatible; it is not automatically trustworthy.

## 2. Problem Statement

TagEkyc receives evidence about document images, selfie images, liveness media, NFC reads, OCR outputs, face matches, fingerprint captures, and risk signals. If an ordinary client application can independently collect those inputs, process them, and submit final `Passed` results, then TagEkyc is effectively trusting the client-side capture and decision process.

That posture is not acceptable for production identity assurance.

The design must distinguish:

- integration compatibility;
- capture orchestration;
- artifact authenticity and integrity;
- evidence decision authority;
- legal/audit reliance.

## 3. Channel Taxonomy

| Channel | Primary user | TagEkyc role | Trust posture |
| --- | --- | --- | --- |
| REST API only | Business backend, trusted partner backend | Create sessions, read results, complete sessions, verify proof | Good for orchestration and result consumption; not sufficient for untrusted media/evidence collection by default. |
| Hosted capture web flow | Web onboarding, mobile browser, desktop handoff | TagEkyc owns capture UI and upload flow | Recommended low-friction default when native device integration is not required. |
| Web SDK | Web applications needing embedded UX | TagEkyc controls capture component; client hosts or embeds it | Recommended when client needs UI control but TagEkyc must own capture protocol. |
| Mobile SDK | iOS/Android apps | TagEkyc controls native capture, NFC, camera/liveness flow | Recommended for stronger capture control and device capabilities. |
| Local PC agent / kiosk agent | Branch, hospital, kiosk, desktop signing station | Controlled capture agent submits artifacts under capture scope | Recommended for controlled environments with device registration and operational policy. |
| Trusted adapter API | OCR/NFC/face/liveness/fingerprint engines | Adapter submits processed evidence results | High trust only when adapter is registered, scoped, monitored, versioned, and audited. |
| Partner direct media API | Certified partner or provider | Partner uploads media/artifact references | Allowed only by reviewed provider authorization packet and explicit policy. |

## 4. Role Boundaries

### 4.1 BusinessConsumer

Business consumers create sessions and consume sanitized results. They SHOULD NOT submit capture artifacts or evidence results unless explicitly authorized under a separate role.

Allowed by default:

- create verification session;
- pass business context such as `subjectRef`, `purpose`, `profile`, `externalSessionId`, `clientReference`, and `challenge`;
- read session/package result;
- complete a session when policy allows;
- verify a signed proof view.

Not allowed by default:

- submit raw or derived evidence as final truth;
- mark OCR, NFC, liveness, face match, or fingerprint checks as passed;
- receive raw biometric/document/NFC/fingerprint payloads;
- bypass TagEkyc capture flow and still claim production-grade evidence.

### 4.2 CaptureAgent

Capture agents, SDKs, hosted flows, and device gateways collect or upload artifacts. They are not final decision authorities.

Allowed by policy:

- submit session-bound document, selfie, liveness, NFC, fingerprint, or device-capture artifacts;
- include artifact hashes, metadata hashes, capture-agent id, device id, request id, and correlation id;
- request retry guidance through the capture flow.

Not sufficient by itself:

- a capture agent assertion that the subject is verified;
- a client-side OCR or liveness result from an untrusted app;
- a media hash without artifact availability, session binding, replay resistance, and audit.

### 4.3 TrustedAdapter

Trusted adapters process artifacts and submit evidence results. This role is for server-side, provider-side, or controlled internal engines, not ordinary business clients.

Required production controls:

- adapter registration and key lifecycle;
- strict scopes;
- engine name/version capture;
- input artifact linkage;
- payload integrity and signing;
- deterministic result mapping;
- audit and monitoring;
- fail-closed behavior for malformed, stale, or replayed input.

## 5. Why Spec-Only Integration Is Not Enough

A spec can prove that a request is well-formed. It cannot prove that:

- the user was physically present;
- the camera feed was genuine;
- the media was captured during the current session;
- the NFC read was fresh and not replayed;
- OCR was performed by an approved engine;
- liveness was actually executed;
- the device was not tampered with;
- the client did not skip or alter the capture flow.

Therefore, TagEkyc SHOULD publish specs, but MUST NOT rely on spec compliance alone for production evidence trust.

The spec is the contract. The SDK/hosted flow/agent is the execution rail. The server-side trusted adapter decision is the evidence authority.

## 6. Recommended Integration Model

### 6.1 Default Web Flow

Use when the relying application is a web app, portal, HIS, or SignFlow-like business system.

```text
1. Business backend creates TagEkyc session.
2. TagEkyc returns capture URL or short-lived SDK token.
3. User completes hosted capture flow.
4. TagEkyc processes artifacts through trusted adapters.
5. Business backend receives webhook or polls session/package.
6. Business backend verifies signed proof when needed.
```

### 6.2 Default Mobile Flow

Use when the relying application is a native mobile app.

```text
1. Mobile app asks its backend to start eKYC.
2. Backend creates TagEkyc session.
3. Backend returns short-lived SDK token to mobile app.
4. Mobile app launches TagEkyc Mobile SDK.
5. SDK performs document/selfie/liveness/NFC flow and uploads artifacts.
6. TagEkyc/trusted adapters decide evidence results.
7. Backend receives result and unlocks business workflow.
```

The mobile app MUST NOT receive a long-lived TagEkyc API key.

### 6.3 Local Agent / Kiosk Flow

Use when capture occurs on a PC, kiosk, hospital desk, branch device, or signing station.

```text
1. Business backend creates session.
2. Operator or user opens a device-bound capture agent.
3. Agent authenticates as CaptureAgent with registered device/capture-agent id.
4. Agent captures or uploads artifacts and metadata.
5. Server-side trusted adapters produce evidence results.
6. Business backend reads or receives final result.
```

Production local agents SHOULD use device registration, key rotation, operator audit, version policy, and remote disablement.

### 6.4 Trusted Partner / Certified Provider Flow

Use only when an external provider is explicitly approved as a trusted adapter or certified capture provider.

The partner may submit evidence results or artifact references only under a reviewed provider authorization packet that defines:

- provider identity;
- allowed artifact/evidence types;
- raw payload policy;
- retention and legal hold obligations;
- callback and reconciliation behavior;
- evidence signature and verification requirements;
- audit, incident, and revocation procedure.

## 7. Commercial Pattern Reference

Market eKYC systems commonly expose several channels at once:

- hosted web flow for fast integration and lower maintenance;
- web SDK for embedded UX;
- mobile SDK for camera/NFC/liveness control;
- REST API for backend orchestration and result retrieval;
- webhooks/callbacks for asynchronous decisions;
- dashboard or workflow builder for verification templates.

Representative public references:

- Entrust/Onfido documents API-managed workflows, SDK tokens, and recommends SDKs for capture/upload of document photos and live selfies; direct media upload is possible but not recommended because it loses SDK quality-control and fraud-protection mechanisms.
  Source: `https://documentation.identity.entrust.com/api/latest/`
- Jumio describes Web Client, Web SDK, Mobile SDKs, and REST APIs as separate integration channels, with Mobile SDKs providing stronger access to camera/NFC capabilities and camera-injection detection.
  Source: `https://www.jumio.com/identity-verification-integration-channels-which-one-is-right-for-your-business/`
- Veriff positions SDKs as the option when the customer wants Veriff's front-end solution and built-in verification flow, and decision results are delivered through webhooks.
  Sources: `https://devdocs.veriff.com/docs/sdk-guide`, `https://devdocs.veriff.com/docs/decision-webhook`
- Persona models each end-user verification as an inquiry, provides hosted/customizable UI, and supports data retrieval through API and notifications through webhooks.
  Sources: `https://docs.withpersona.com/inquiries`, `https://docs.withpersona.com/webhooks`
- Sumsub uses WebSDK/MobileSDK access tokens for user-side verification flows, while Sumsub performs authenticity, image-integrity, and data checks.
  Sources: `https://docs.sumsub.com/docs/how-id-verification-works`, `https://docs.sumsub.com/docs/get-started-with-web-sdk`
- Stripe Identity creates server-side verification sessions and uses hosted/modal client flows plus asynchronous webhook/API result handling.
  Sources: `https://docs.stripe.com/identity/verification-sessions`, `https://docs.stripe.com/identity/handle-verification-outcomes`

These examples support the same architectural direction for TagEkyc: API for orchestration, controlled capture surfaces for evidence collection, and server-side/provider-side decision authority.

## 8. S1 Current-State Alignment

Current S1 already has useful role separation:

- `BusinessConsumer` creates sessions, reads summaries/packages, completes sessions, and reads verification views.
- `CaptureAgent` appends capture artifacts.
- `TrustedAdapter` appends evidence results.
- Business responses are sanitized by default.
- The verification view exposes a signed proof surface for consumer verification.

However, current S1 is still a runtime-contract foundation. It does not yet provide production-grade:

- hosted capture flow;
- web or mobile SDK;
- local agent;
- raw artifact vault and resolver;
- production device/app attestation;
- replay-resistant capture challenge protocol;
- production webhook delivery/retry/outbox;
- production payload/webhook signature model;
- provider-specific evidence authorization packet;
- certified engine/provider integration;
- legal/regulatory production readiness.

## 9. Product Decision Recommendation

TagEkyc SHOULD adopt the following product decision:

1. The canonical integration model is API-orchestrated, TagEkyc-controlled capture.
2. Business applications integrate through REST APIs, SDK tokens/capture URLs, result APIs, webhooks, and signed proof verification.
3. End-user media and device evidence should be collected by TagEkyc hosted flow, TagEkyc SDK, controlled local agent, or explicitly approved trusted provider.
4. Ordinary clients MUST NOT be allowed to submit final `Passed` evidence results.
5. REST-only artifact or evidence submission is reserved for registered CaptureAgents, TrustedAdapters, or reviewed provider integrations.
6. Production trust requires session-bound capture challenge, artifact integrity, replay protection, server-side or certified adapter decisioning, audit, and evidence proof.

## 10. Minimum Future Runtime Capabilities

Before TagEkyc can claim production-grade capture trust, it needs at least:

- short-lived SDK/capture token issuance;
- capture session lifecycle separate from business session lifecycle where needed;
- hosted capture web flow or SDK shell;
- mobile SDK protocol for document/selfie/liveness/NFC capture;
- local agent registration and device identity model;
- challenge-bound capture payloads;
- artifact upload or secure artifact reference policy;
- artifact availability and resolver rules;
- payload signing or authenticated upload;
- trusted adapter registry and engine-version policy;
- evidence result verification and audit;
- webhook/callback delivery with signature and replay protection;
- evidence proof verification guidance for relying applications.

## 11. Non-Goals For This Draft

This document does not:

- define final SDK APIs;
- choose a camera, NFC, liveness, OCR, face-match, fingerprint, storage, HSM, or webhook provider;
- authorize raw artifact persistence;
- approve direct media upload from arbitrary clients;
- claim legal/regulatory production readiness;
- replace `lld_03_api_contracts_v0_1.md` as the public route contract;
- change current S1 runtime behavior.

## 12. Open Decisions

| ID | Decision | Recommended owner |
| --- | --- | --- |
| IC-01 | First controlled capture surface: hosted web, mobile SDK, local PC agent, or provider integration | Product + engineering |
| IC-02 | SDK token model: issuer, TTL, audience, scope, refresh, revocation | Security + engineering |
| IC-03 | Capture challenge protocol and replay window | Security + architecture |
| IC-04 | Raw artifact policy for each check type and environment | Legal + security + product |
| IC-05 | Device/app attestation posture by channel | Security |
| IC-06 | Trusted adapter onboarding and revocation model | Engineering + operations |
| IC-07 | Callback/webhook delivery and signature model | Engineering + security |
| IC-08 | Evidence package/proof verification requirements for relying apps | Architecture + legal |

## 13. Suggested Next Slice

Open a reviewed planning TIP for the first controlled capture surface. The narrowest useful slice is:

```text
Backend creates verification session
  -> returns short-lived capture token
  -> hosted capture stub accepts session-bound artifact metadata/hash
  -> TrustedAdapter remains server-side
  -> BusinessConsumer reads sanitized result
```

The slice should prove role separation and session-bound capture without claiming final production security.
