# TagEkyc Docs Baseline Closeout v0.1.1

**File:** `docs/tag_ekyc_docs_baseline_closeout_v0_1_1.md`
**Version:** 0.1.1
**Status:** Closed - baseline accepted
**Date:** 2026-06-08
**Baseline:** Product Brief v0.1.1
**Purpose:** Records accepted documentation baseline and next planning step.

## Changelog

### v0.1.1 - Baseline closeout recorded

- Recorded Product Brief v0.1.1 acceptance as product baseline.
- Confirmed baseline docs aligned to Product Brief.
- Captured key decisions, open debts, and TIP-01 as next recommended step.

## 1. Baseline Verdict

- Product Brief v0.1.1 accepted as product baseline.
- Baseline docs aligned to Product Brief.
- Ready for TIP-01 Project Skeleton planning.

## 2. Files in Baseline

- `docs/00_product_brief_v0_1.md`
- `docs/00_README.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_02_sequence_flows_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/lld_04_engine_adapter_contracts_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/signflow_integration_contract_v0_1.md`

## 3. Key Decisions Captured

- TagEkyc is an independent eKYC / identity assurance platform.
- SignFlow is a consumer profile, not the base platform.
- `STANDARD_EKYC_PROFILE` and `TRANSACTION_BOUND_EKYC_PROFILE` are defined.
- `bindingNonceHash` is required only for transaction-bound profile.
- `VerificationSession` is root business correlation object.
- `CaptureArtifact` and `EvidenceResult` are separate concepts.
- `CAPTURE_QUALITY` and retry semantics are part of S1 model.
- Business clients do not receive raw artifacts or internal VaultRefs by default.
- Signature layers are separated conceptually.
- S1 is not production-certified eKYC.

## 4. Known Open Decisions / Debts

- Raw biometric protection, including encryption, access, retention, and deletion controls.
- Capture artifact retention policy, including deletion, legal hold, vault lifecycle, and recapture handling.
- Capture agent / SDK / device gateway trust and scoped submission model.
- Legal certification requirements by jurisdiction and use case.
- Evidence package signature implementation and managed signing key process.
- NFC production readiness for supported documents, devices, and validation model.
- Fingerprint hardware dependency, device trust, SDK integration, and capture quality controls.
- Capture quality retry policy, including retry counts, reason codes, terminal failure, UX messaging, and operator override.
- Face liveness / PAD production engine selection and testing.
- RequiredChecks and risk policy versioning.
- Payload signature model.
- Webhook signature and replay protection model.
- Request id, correlation id, idempotency, and log propagation conventions.
- Profile naming and policy mapping for future consumer profiles.
- Operator review queue and manual review operations.
- Webhook observability, replay, dead-letter handling, dashboards, and alerting.

## 5. Next Recommended Step

- Open TIP-01 Project Skeleton planning.
- TIP-01 should create repository/project skeleton only.
- TIP-01 should not implement eKYC business logic yet.

## 6. Git Status

```bash
?? Note.txt
?? docs/
```
