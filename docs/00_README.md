# TagEkyc

TagEkyc is an independent eKYC and identity assurance platform that verifies documents, biometrics, liveness, and fingerprints, then produces auditable evidence packages for consuming systems. TagEkyc is designed to answer who the person is, expose verification results through APIs and webhooks, and preserve evidence using VaultRef/hash references rather than leaking raw sensitive data.

`00_product_brief_v0_1.md` is the accepted Product Brief v0.1.1 baseline and SHOULD be read first. Other baseline documents align to it.

## What TagEkyc Does

- TagEkyc MUST create and manage verification sessions for external client applications.
- TagEkyc MUST apply explicit RequiredChecks policy per session.
- TagEkyc MUST record document, CCCD/NFC, face match, liveness, fingerprint, and risk result shapes.
- TagEkyc MUST produce an `EkycEvidencePackage` with evidence manifest, hashes, VaultRefs, timestamps, and audit references.
- TagEkyc MUST keep evidence and audit records append-only by design.
- TagEkyc SHOULD deliver completion results through webhook/callback contracts.
- TagEkyc MAY use mock or PoC verification engines in S1 behind adapter interfaces.

## What TagEkyc Does Not Do

- TagEkyc MUST NOT perform digital signing.
- TagEkyc MUST NOT determine what a signer saw or agreed to sign.
- TagEkyc MUST NOT depend on SignFlow code, database, deployment, or runtime.
- TagEkyc MUST NOT return raw CCCD images, raw face images, raw liveness captures, or raw fingerprint data to consumer systems.
- TagEkyc MUST NOT claim production-certified legal eKYC readiness in S1.

## Relationship With SignFlow

SignFlow is one consuming system of TagEkyc. SignFlow MUST call TagEkyc as an external identity assurance provider and bind the returned evidence to a signing session using `externalSessionId`, `externalTransactionId`, `bindingNonceHash`, policy, and evidence manifest.

Responsibility split:

- TagEkyc proves who the person is.
- SignFlow proves what the person saw and agreed to sign.
- `bindingNonce` proves that identity verification and signing consent belong to the same transaction context.

## S1 Target

TagEkyc S1 is an evidence-ready eKYC platform with mock/PoC verification engines. S1 MUST include stable result shapes, session state, client application/API key authentication, RequiredChecks policy, Evidence VaultRef model, append-only audit log, webhook delivery, and SignFlow integration contract. S1 is NOT production-certified eKYC.

## Document Index

1. [Product Brief](00_product_brief_v0_1.md)
2. [High-Level Design](tagekyc_hld_v0_1.md)
3. [Logical Data Model](lld_01_data_model_v0_1.md)
4. [Sequence Flows](lld_02_sequence_flows_v0_1.md)
5. [API Contracts](lld_03_api_contracts_v0_1.md)
6. [Engine Adapter Contracts](lld_04_engine_adapter_contracts_v0_1.md)
7. [Phase 1 Scope and Debt Registry](phase1_scope_and_debt_registry_v0_1.md)
8. [SignFlow Integration Contract](signflow_integration_contract_v0_1.md)
