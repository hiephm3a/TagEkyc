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

TagEkyc S1 is an evidence-ready LocalDev eKYC platform with mock/PoC verification engines. Current S1 includes stable result shapes, session state, client application/API key authentication, RequiredChecks policy, Evidence VaultRef/hash model, append-only audit log, an internal completion notification projection, and a SignFlow transaction-bound integration contract. Public webhook delivery, retry, and outbox behavior remain deferred as recorded in the TIP-09 closeout. S1 is NOT production-certified eKYC.

## Document Index

1. [Product Brief](00_product_brief_v0_1.md)
2. [Documentation Governance](00_DOCS_GOVERNANCE.md)
3. [High-Level Design](tagekyc_hld_v0_1.md)
4. [Logical Data Model](lld_01_data_model_v0_1.md)
5. [Sequence Flows](lld_02_sequence_flows_v0_1.md)
6. [API Contracts](lld_03_api_contracts_v0_1.md)
7. [Engine Adapter Contracts](lld_04_engine_adapter_contracts_v0_1.md)
8. [Phase 1 Scope and Debt Registry](phase1_scope_and_debt_registry_v0_1.md)
9. [SignFlow Integration Contract](signflow_integration_contract_v0_1.md)
10. [Docs Baseline Closeout v0.1.1](tag_ekyc_docs_baseline_closeout_v0_1_1.md)
11. [TIP-01 Project Skeleton Brief](tips/tip_01_project_skeleton/tip_01_brief_v0_1.md)
12. [TIP-01 Execution Report](tips/tip_01_project_skeleton/tip_01_execution_report_v0_1.md)
13. [Agent Coordination Bus](00_AGENT_COORDINATION_BUS.md)
14. [TIP-02 S1 Execution Roadmap](tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md)
15. [TIP-09 S1 Hardening Closeout](tips/tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md)

## Documentation Version History

All new planning, baseline, and closeout docs SHOULD include metadata and a changelog. Follow [Documentation Governance](00_DOCS_GOVERNANCE.md) before patching docs so future agents can understand document history without reconstructing Git commits.
