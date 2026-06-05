# TagEkyc High-Level Design v0.1

## Product Purpose

TagEkyc is an independent eKYC and identity assurance platform. It verifies identity evidence from documents, CCCD/NFC, face match, liveness, fingerprint, and risk signals, then produces auditable evidence packages for external systems.

TagEkyc MUST answer who the person is. It MUST NOT answer what document the person agreed to sign.

## Non-Goals

- TagEkyc MUST NOT perform digital signing.
- TagEkyc MUST NOT render signing documents for consent.
- TagEkyc MUST NOT depend on SignFlow internals.
- TagEkyc MUST NOT expose raw sensitive evidence to consumers by default.
- S1 MUST NOT claim production legal certification, regulatory approval, or production biometric assurance.

## Target Consumers

- SignFlow
- Hospital Information Systems
- Patient Portal applications
- Registration and onboarding portals
- Internal operator tools
- Other client applications requiring identity assurance

TagEkyc also distinguishes caller roles. Business clients create sessions and consume sanitized results. Capture agents, SDKs, and device gateways submit captured artifacts under separate scopes. Internal adapters process artifacts into evidence results inside secure boundaries. Operator/admin tools are privileged operational callers.

## High-Level Architecture

```mermaid
flowchart LR
    Client["Business Client"] --> Api["TagEkyc API"]
    Agent["Capture Agent / SDK / Device Gateway"] --> Api
    Api --> Sessions["Verification Sessions"]
    Api --> Auth["Client Applications / API Keys"]
    Sessions --> Artifacts["Capture Artifacts"]
    Artifacts --> Vault["Evidence Vault"]
    Artifacts --> Adapters["Internal Engine Adapters"]
    Adapters --> Results["Evidence Results"]
    Results --> Sessions
    Sessions --> Doc["Document Verification"]
    Sessions --> Bio["Biometric Verification"]
    Doc --> Engines["Engine Adapters"]
    Bio --> Engines
    Sessions --> Evidence["Evidence Package Builder"]
    Evidence --> Vault
    Sessions --> Audit["Append-only Audit"]
    Sessions --> Webhooks["Webhook Dispatcher"]
    Webhooks --> Client
```

## Suggested Bounded Contexts / Modules

### Tenancy / Client Applications

Manages external client applications, API keys, permissions, webhook subscriptions, allowed purposes, and callback configuration. It SHOULD isolate sessions by client application.

### Verification Sessions

Owns lifecycle, state transitions, RequiredChecks policy, profile, external correlation fields, captured artifacts, verification checks, evidence results, evidence package, callbacks/webhooks, expiry, and final decision assembly. `VerificationSession` is the root business correlation object, not merely a technical session id. It MUST use explicit state and result enums.

`STANDARD_EKYC_PROFILE` is the generic platform profile for ordinary identity assurance. `TRANSACTION_BOUND_EKYC_PROFILE` is used when the result must be tied to an external transaction and requires `bindingNonceHash`.

### Capture Artifacts

Represents captured, uploaded, or received inputs such as document images, selfie images, liveness media, NFC read artifacts, fingerprint captures, and device/capture metadata. Raw artifacts MUST remain inside vault or secure adapter boundaries.

### Evidence Results

Represents processed outputs derived from one or more capture artifacts, such as OCR, NFC validation, face match, liveness, fingerprint match, fraud/risk, and capture quality results. Business clients receive sanitized result summaries, refs, hashes, and correlation fields.

### Document Verification

Owns OCR document evidence, CCCD/NFC result shapes, document consistency checks, and document-level confidence signals.

### Biometric Verification

Owns face match, liveness, fingerprint, and biometric confidence/risk signals.

### Evidence

Builds `EkycEvidencePackage` from immutable evidence result records, artifact refs, VaultRefs, hashes, timestamps, adapter versions, and audit references.

### Vault

Stores sensitive artifacts or references to external secure storage. Business consumers receive sanitized result summaries, evidence refs, package refs, hashes, and correlation fields instead of raw sensitive data. Internal VaultRefs MAY be exposed only through explicit evidence-access policy, authorization, and audit. Default business-consumer payloads MUST NOT expose raw artifacts.

### Audit

Records append-only events for session creation, evidence ingestion, adapter execution, result calculation, webhook delivery, and administrative actions.

### Webhooks

Dispatches verification completion callbacks to subscribed consumers with retry tracking and signature metadata. Production design SHOULD distinguish `payloadSignature`, `webhookSignature`, and `evidencePackageSignature`; S1 MAY use placeholders.

### Device/Agent Gateway

Coordinates browser/mobile/agent capture flows when device-side evidence is required. S1 MAY use simplified PoC ingestion endpoints.

### Engine Adapters

Defines interfaces for OCR, NFC, face match, liveness, fingerprint match, risk evaluation, evidence vault, and webhook delivery. S1 MAY use mock adapters behind these interfaces.

## S1 Boundary

S1 MUST include:

- Verification Session API
- Client Application/API Key authentication model
- RequiredChecks policy
- `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE` policy naming
- CaptureArtifact and EvidenceResult logical model
- Generic `CAPTURE_QUALITY` result category
- CCCD/NFC result shape
- Face match result shape
- Liveness result shape
- Fingerprint result shape
- Evidence VaultRef model
- `EkycEvidencePackage`
- Append-only audit log
- Webhook/callback result delivery
- SignFlow integration contract

S1 MAY include optional PoC adapters for OCR CCCD, NFC document reading, face matching, liveness detection, fingerprint matching, and risk evaluation. These MUST remain behind interfaces.

## Future Production Boundary

Production readiness SHOULD add:

- Certified document/NFC readers where legally required
- Certified biometric/liveness engines where legally required
- Hardware-backed key management for evidence signing
- Stronger vault encryption and retention controls
- Regulatory audit evidence, operator review, and compliance workflows
- Formal threat model and privacy impact assessment
- Operational monitoring, alerting, and incident response

## Security / Privacy Principles

- Raw CCCD, face, liveness, and fingerprint artifacts MUST be treated as highly sensitive.
- Consumer result payloads MUST use sanitized result summaries, evidence refs, package refs, hashes, and correlation fields.
- API keys MUST be hashed at rest and scoped by client application.
- Webhook payloads SHOULD be signed when the signature model is implemented.
- Business clients MUST NOT be treated as automatically trusted to submit arbitrary `PASSED` evidence.
- Capture/adapter submission scopes MUST be distinct from ordinary business client scopes.
- Evidence access MUST be audited.
- Data retention MUST be explicit per client and purpose.
- S1 SHOULD minimize raw artifact persistence unless needed for evidence replay.

## Evidence Principles

- Evidence records MUST be append-only once accepted.
- Evidence packages MUST include deterministic manifests.
- Hashes MUST be computed over canonical evidence metadata and referenced artifacts.
- Evidence packages SHOULD include adapter name/version and confidence values.
- Result decisions MUST be reproducible from the evidence manifest where possible.

## SignFlow Integration Overview

SignFlow is the first named `TRANSACTION_BOUND_EKYC_PROFILE` consumer, not the generic TagEkyc platform model. Generic sessions do not default to `externalSystem = SignFlow` or `purpose = SIGNING_AUTH`.

For signing authorization, SignFlow creates a TagEkyc verification session with `externalSessionId`, `externalTransactionId`, `subjectRef`, `purpose = SIGNING_AUTH`, `bindingNonceHash`, and `requiredChecks`. Any `externalSystem` or `clientCode` value MUST be derived from or validated against the authenticated `clientApplicationId`.

TagEkyc returns a verification result and evidence package. SignFlow MUST validate that `externalSessionId`, `externalTransactionId`, and `bindingNonceHash` match the signing transaction before binding the evidence to a signing session.
