# Sequence Flows v0.1

## Create Generic Verification Session

```mermaid
sequenceDiagram
    participant Client as Business Client
    participant Api as TagEkyc API
    participant Auth as API Key Auth
    participant Sessions as Verification Sessions
    participant Audit as Audit Log

    Client->>Api: POST /api/ekyc/verification-sessions
    Api->>Auth: Validate API key and scopes
    Auth-->>Api: Client application context
    Api->>Sessions: Create STANDARD_EKYC_PROFILE session with purpose, subjectRef, requiredChecks
    Sessions->>Audit: Append SESSION_CREATED
    Sessions-->>Api: verificationSessionId, requestId, correlationId, state=CREATED
    Api-->>Client: 201 Created
```

Generic session creation MUST derive `clientApplicationId` from authentication. Generic platform requests do not default to `externalSystem = SignFlow`, `purpose = SIGNING_AUTH`, or mandatory `bindingNonceHash`.

## Capture Artifact Submission

```mermaid
sequenceDiagram
    participant Agent as Capture Agent / SDK / Device Gateway
    participant Api as TagEkyc API
    participant Vault as Evidence Vault
    participant Sessions as Verification Sessions
    participant Audit as Audit Log

    Agent->>Api: POST /verification-sessions/{id}/capture-artifacts
    Api->>Sessions: Validate session, capture scope, artifact type, expiry
    Api->>Vault: Store raw artifact or secure object reference
    Vault-->>Api: vaultRef, artifactHash
    Api->>Sessions: Register CaptureArtifact
    Sessions->>Audit: Append CAPTURE_ARTIFACT_ACCEPTED
    Api-->>Agent: 202 Accepted with captureArtifactId
```

Capture artifacts are inputs such as document images, selfie images, liveness media, NFC read artifacts, fingerprint captures, and device metadata. Raw artifacts remain inside vault or secure adapter boundaries.

## Internal Adapter Processing Artifact Into Evidence Result

```mermaid
sequenceDiagram
    participant Sessions as Verification Sessions
    participant Adapter as Internal Engine Adapter
    participant Vault as Evidence Vault
    participant Results as Evidence Results
    participant Audit as Audit Log

    Sessions->>Adapter: Execute check with captureArtifactRefs
    Adapter->>Vault: Read artifact through secure boundary
    Vault-->>Adapter: Secure artifact handle
    Adapter-->>Results: Sanitized EvidenceResult, reasonCodes, hashes
    Results->>Audit: Append EVIDENCE_RESULT_RECORDED
    Results-->>Sessions: Check status PASSED / REVIEW_REQUIRED / FAILED_* / TECHNICAL_ERROR
```

Adapters convert artifacts into evidence results such as OCR, NFC validation, face match, liveness, fingerprint match, fraud/risk, and capture quality. Business clients do not submit arbitrary `PASSED` evidence.

## Capture Quality Retry Flow

```mermaid
sequenceDiagram
    participant Agent as Capture Agent / User Flow
    participant Sessions as Verification Sessions
    participant Quality as Capture Quality Adapter
    participant Results as Evidence Results
    participant Audit as Audit Log

    Sessions->>Quality: Evaluate CAPTURE_QUALITY
    Quality-->>Results: RETRY_REQUIRED with reasonCode BLURRY_IMAGE
    Results->>Audit: Append CAPTURE_QUALITY_RETRY_REQUIRED
    Sessions-->>Agent: Retry requested with sanitized reason
    Agent->>Sessions: Submit new capture artifact
    Sessions->>Audit: Append CAPTURE_ARTIFACT_ACCEPTED
```

`RETRY_REQUIRED` means the artifact quality is insufficient and the user/operator should capture again. `FAILED_CAPTURE_QUALITY` means the artifact cannot be used. These are distinct from `FAILED_IDENTITY`, `REVIEW_REQUIRED`, and `TECHNICAL_ERROR`.

## Complete Verification Session

```mermaid
sequenceDiagram
    participant Client as Business Client
    participant Api as TagEkyc API
    participant Sessions as Verification Sessions
    participant Risk as Risk Evaluator
    participant Evidence as Evidence Package Builder
    participant Audit as Audit Log

    Client->>Api: POST /verification-sessions/{id}/complete
    Api->>Sessions: Validate session state
    Sessions->>Risk: Evaluate required checks and evidence results
    Risk-->>Sessions: Result, riskScore, assuranceLevel
    Sessions->>Evidence: Build EkycEvidencePackage
    Evidence-->>Sessions: evidencePackageId and packageHash
    Sessions->>Audit: Append SESSION_COMPLETED
    Sessions-->>Api: Final result
    Api-->>Client: 200 OK
```

## Webhook Callback To Business Client

```mermaid
sequenceDiagram
    participant TagEkyc as TagEkyc Webhooks
    participant Client as Client Callback API
    participant Audit as Audit Log

    TagEkyc->>Client: VerificationCompleted payload with webhookSignature placeholder
    Client-->>TagEkyc: 2xx accepted
    TagEkyc->>Audit: Append WEBHOOK_DELIVERED
```

Future production `webhookSignature` SHOULD include delivery id, timestamp, and replay protection.

## Transaction-Bound SignFlow Binding Validation

```mermaid
sequenceDiagram
    participant SignFlow as SignFlow
    participant TagEkyc as TagEkyc
    participant Signing as Signing Session

    SignFlow->>TagEkyc: Create TRANSACTION_BOUND_EKYC_PROFILE session with externalSessionId, externalTransactionId, bindingNonceHash
    TagEkyc-->>SignFlow: verificationSessionId
    TagEkyc-->>SignFlow: Completion result webhook
    SignFlow->>Signing: Load expected externalSessionId, externalTransactionId, bindingNonceHash, evidencePackageHash policy
    SignFlow->>SignFlow: Compare expected values with TagEkyc payload
    alt Values match
        SignFlow->>Signing: Bind evidencePackageId to signing session
    else Any value mismatches
        SignFlow->>Signing: Reject eKYC result
    end
```

SignFlow is the first named transaction-bound consumer profile. `purpose = SIGNING_AUTH` and `bindingNonceHash` are SignFlow/transaction-bound requirements, not generic platform defaults.

## Failure Flow

```mermaid
sequenceDiagram
    participant Adapter as Engine Adapter
    participant Api as TagEkyc API
    participant Sessions as Verification Sessions
    participant Audit as Audit Log
    participant Webhooks as Webhook Dispatcher
    participant Client as Client Application

    Adapter-->>Api: EvidenceResult RETRY_REQUIRED, FAILED_CAPTURE_QUALITY, FAILED_IDENTITY, REVIEW_REQUIRED, or TECHNICAL_ERROR
    Api->>Sessions: Record failed check
    Sessions->>Audit: Append CHECK_FAILED
    alt RETRY_REQUIRED
        Sessions-->>Client: Sanitized retry reason / continue capture
    else Terminal or review outcome
        Sessions->>Sessions: Determine result FAILED, REVIEW_REQUIRED, or ERROR
        Sessions->>Webhooks: Dispatch failure result if terminal
        Webhooks->>Client: VerificationCompleted result=FAILED / REVIEW_REQUIRED / ERROR
    end
```

## Timeout / Expired Flow

```mermaid
sequenceDiagram
    participant Scheduler as Expiry Worker
    participant Sessions as Verification Sessions
    participant Evidence as Evidence Package Builder
    participant Audit as Audit Log
    participant Webhooks as Webhook Dispatcher
    participant Client as Client Application

    Scheduler->>Sessions: Find sessions where expiresAt < now and not terminal
    Sessions->>Audit: Append SESSION_EXPIRED transition event
    Sessions->>Sessions: Update current-state projection to EXPIRED if projection is used
    Sessions->>Evidence: Build partial evidence package if evidence exists
    Evidence-->>Sessions: Optional evidencePackageId
    Sessions->>Webhooks: Dispatch expired result
    Webhooks->>Client: VerificationCompleted result=EXPIRED
```
