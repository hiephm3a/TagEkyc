# TIP-11 Production Data Boundary and Durable State Foundation v0.1

**File:** `docs/tips/tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md`
**Version:** 0.2
**Status:** Accepted - planning only
**Date:** 2026-06-12
**Baseline:** `3825e49ef`
**Purpose:** Opens S2 planning by defining the first post-S1 foundation slice for production data boundaries, durable metadata posture, policy snapshot identity, and vault/retention/legal-hold constraints without dispatching implementation.

## Changelog

### v0.2 - Planning accepted

- Recorded GPT Gate acceptance with no blocker findings.
- Reconfirmed TIP-11 is accepted only as S2 production foundation / non-production hardening planning.
- Reconfirmed no implementation, kickoff, DB/provider/migration, local durable adapter, vault lifecycle, retention enforcement, webhook/outbox/retry, production crypto, vendor/provider selection, raw artifact storage, pilot readiness, production readiness, or SignFlow runtime dependency is authorized.
- Recorded Option B as the next kickoff candidate, requiring a separate reviewed kickoff before implementation.

### v0.1 - Initial S2/TIP-11 planning draft

- Opened S2 planning under the accepted TIP-10 ordering.
- Defined TIP-11 as the first S2 foundation planning slice, not implementation dispatch.
- Scoped the durable-state target around metadata, policy snapshots, audit, and evidence/package records.
- Preserved raw artifact storage, production database selection, migrations, webhook/outbox/retry, production crypto, vendor selection, pilot readiness, production readiness, and SignFlow runtime dependencies as non-goals unless later explicitly accepted.

## 1. S2 Goal

S2 target:

```text
S2 - Production Foundation, Non-Production Hardening
```

S2 should move TagEkyc from S1 LocalDev evidence readiness toward a production-foundation architecture, while still avoiding pilot-readiness and production-readiness claims.

S2 is successful when TagEkyc has planned and, in later accepted runtime TIPs, implemented the foundational boundaries needed before real-user pilot decisions:

- durable session/audit/evidence metadata posture;
- explicit raw artifact and VaultRef/hash boundary;
- retention, deletion eligibility, and legal-hold classification model;
- policy versioning and decision snapshot identity;
- production actor trust separation for BusinessConsumer, CaptureAgent, TrustedAdapter, Operator, and Admin;
- a safe ordering path for later vault, provider, webhook, signing, and operations work.

S2 is not production. S2 is not pilot readiness. S2 is the controlled engineering foundation before those gates can be evaluated.

## 2. TIP-11 Summary

TIP-11 should prepare the first S2 runtime-capable foundation slice. This brief is planning only and does not authorize code changes.

Recommended implementation direction for a future TIP-11 kickoff:

- Define durable-state boundaries before choosing full production infrastructure.
- Keep application tables/records to sanitized metadata, identifiers, hashes, policy snapshot ids, status, timestamps, and audit facts.
- Keep raw artifacts and biometrics out of application persistence.
- Treat VaultRef as an external secure storage reference, not as permission to implement raw storage now.
- Introduce policy snapshot identity with every session and final decision boundary before multi-client or real-user use.
- Preserve current S1 LocalDev behavior until a later accepted kickoff explicitly changes runtime persistence.

## 3. Baseline Inputs

- `docs/tips/tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md`
- `docs/tips/tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md`
- `docs/phase1_scope_and_debt_registry_v0_1.md`
- `docs/lld_01_data_model_v0_1.md`
- `docs/lld_03_api_contracts_v0_1.md`
- `docs/lld_04_engine_adapter_contracts_v0_1.md`
- `docs/tagekyc_hld_v0_1.md`
- `docs/00_AGENT_COORDINATION_BUS.md`
- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`

## 4. In Scope for TIP-11 Planning

TIP-11 planning may define:

- S2 phase objective and boundaries.
- Durable metadata model candidates for:
  - client applications and actor identity references;
  - verification sessions;
  - required checks and policy snapshot ids;
  - capture artifact metadata without raw payloads;
  - evidence result metadata without raw provider payloads;
  - final decisions;
  - evidence package metadata;
  - append-only audit events.
- Raw artifact boundary rules:
  - application persistence may store VaultRef/hash/classification metadata only;
  - raw CCCD images, NFC payloads, face media, liveness media, fingerprint images/templates, and sensitive device payloads remain outside application DB.
- Retention/legal-hold planning primitives:
  - retention class;
  - deletion eligibility state;
  - legal-hold marker;
  - purge-block reason;
  - access audit requirement.
- Policy versioning primitive:
  - policy snapshot id;
  - required-check set version;
  - threshold/provider/version references when available;
  - decision reproducibility boundary.
- Candidate implementation options for later kickoff:
  - docs-only architecture slice;
  - code-level interfaces and in-memory-compatible durable boundary;
  - local development persistence adapter after explicit approval;
  - migration-backed provider after explicit approval.
- Review and test expectations for any future implementation.

## 5. Out of Scope and Non-Goals

TIP-11 planning and any future TIP-11 kickoff must not silently include:

- production-ready claim;
- pilot-ready claim;
- legal/compliance/certification acceptance;
- production database vendor selection;
- EF/DbContext/migrations or any durable persistence implementation unless a later kickoff explicitly dispatches it;
- raw artifact or biometric storage;
- vault provider selection;
- retention/deletion/legal-hold enforcement without accepted legal policy;
- webhook delivery, retry, outbox, subscription model, dispatcher, delivery ledger, or dead-letter behavior;
- production cryptography, evidence package signing, webhook signing, replay protection, HMAC, JWS, KMS, HSM, key rotation, or secret lifecycle;
- NFC/face/liveness/fingerprint vendor or certified engine selection;
- specialized evidence endpoints;
- production auth/client trust implementation beyond planning;
- SignFlow runtime, source, database, network, deployment, package, or internal-model dependency.

SignFlow remains an external consumer profile only.

## 6. Candidate Runtime Scope Options

These options are for planning review. None is dispatched by this document.

| Option | Scope | Recommendation | Reason |
| --- | --- | --- | --- |
| A - Planning only | Keep TIP-11 as a planning/design artifact and defer all code. | Safe default for first review. | Lets homeowner resolve persistence, legal-hold, and vault boundaries before implementation. |
| B - Domain/application metadata boundary | Add domain/application abstractions for durable metadata without DB provider or migrations. | Candidate first runtime slice after review. | Can prepare repository contracts and policy snapshot fields while preserving LocalDev behavior. |
| C - Local development persistence adapter | Add a local durable adapter for development only. | Requires STOP/RRI. | Introduces persistence behavior and recovery semantics; must avoid being mistaken for production DB readiness. |
| D - Migration-backed provider | Add EF/DbContext/migrations or selected durable provider. | Deferred by default. | Requires database posture, migration strategy, data classification, and legal retention decisions. |
| E - Vault lifecycle implementation | Implement vault object lifecycle and retention enforcement. | Deferred by default. | Requires legal/compliance answers and storage/security design. |

Recommended next step after this planning draft:

```text
Review Option A/B and decide whether TIP-11 should remain planning-only or proceed to a narrow kickoff for Option B.
```

## 7. Proposed Durable Metadata Boundary

Future TIP-11 implementation should distinguish application metadata from restricted raw evidence.

Application metadata candidates:

- `VerificationSessionId`
- `ClientApplicationId`
- `SubjectRef`
- `Purpose`
- `Profile`
- `ExternalSessionId`
- `ExternalTransactionId`
- `BindingNonceHash`
- `State`
- `RequiredChecks`
- `PolicySnapshotId`
- `CaptureArtifactId`
- `EvidenceResultId`
- `EvidencePackageId`
- `PackageHash`
- `ManifestHash`
- `VaultRef`
- `ArtifactHash`
- `PayloadHash`
- `RetentionClass`
- `DeletionEligibility`
- `LegalHoldStatus`
- `AuditEventId`
- `RequestId`
- `CorrelationId`
- timestamps and actor references.

Restricted raw data that must not enter application persistence:

- raw document images;
- raw CCCD/NFC payloads;
- raw face images;
- liveness videos or challenge media;
- fingerprint images or templates;
- raw provider payloads that contain biometric, identity, or device secrets;
- plaintext identity fields when hash/reference form is sufficient;
- secrets, API keys, webhook signing keys, and provider credentials.

## 8. Policy Snapshot Boundary

Any future durable session/final decision record should be able to answer:

- which RequiredChecks were required;
- which policy version or snapshot produced them;
- which checks were optional, disabled, or deferred;
- which provider/adapter version contributed evidence when available;
- which thresholds or decision rules were used when available;
- whether fingerprint was enabled or omitted by policy;
- whether the session was transaction-bound and which binding fields were required.

Policy snapshot identity should be introduced before:

- multi-client policy variation;
- real-user pilot decisions;
- production provider reliance;
- evidence package signing;
- production audit reliance.

## 9. Retention, Deletion, and Legal-Hold Boundary

TIP-11 should not decide final legal policy. It should prepare fields and gates that prevent accidental storage without governance.

Planning primitives:

| Primitive | Purpose | Default |
| --- | --- | --- |
| `RetentionClass` | Classifies how long metadata or vault objects may be retained. | Required before real-user data. |
| `DeletionEligibility` | Records whether an object/session can be purged or is blocked. | Unknown until legal policy accepted. |
| `LegalHoldStatus` | Blocks deletion while legal hold applies. | Not active unless explicit legal hold is set. |
| `PurgeBlockReason` | Explains why deletion cannot proceed. | Required if deletion is blocked. |
| `AccessAuditRequired` | Marks access as requiring audit trail. | Required for restricted artifact references. |

No enforcement behavior should be implemented until legal/compliance acceptance defines the rules.

## 10. STOP/RRI Questions

Stop before dispatching implementation if any answer is missing:

| Gate | Question | Default recommendation |
| --- | --- | --- |
| Runtime scope | Is TIP-11 allowed to implement code, or should it remain planning-only? | Planning-only until accepted kickoff. |
| Persistence provider | Is a DB/provider allowed in this TIP? | No provider selection in planning. |
| Migration posture | Are migrations allowed? | No migrations unless explicitly dispatched. |
| Raw artifact storage | Can raw artifacts or biometrics be stored? | No. Metadata only. |
| Vault implementation | Is a vault provider selected or authorized? | No. Model VaultRef boundary only. |
| Legal retention | Are retention/deletion/legal-hold rules legally accepted? | No enforcement without legal acceptance. |
| Policy versioning | What is the minimum policy snapshot identity for S2? | Require `PolicySnapshotId` planning before runtime changes. |
| Pilot target | Is S2 targeting real-user pilot? | No pilot-readiness claim from TIP-11. |
| Webhook | Should webhook/outbox/retry be included? | No. Defer until durable boundary and signing/replay planning. |
| SignFlow | Should SignFlow become runtime dependency? | No. Keep external consumer profile only. |

## 11. Review Checklist

Reviewers should check:

- Does the brief preserve S1 closed state?
- Does it avoid production and pilot readiness claims?
- Does it keep raw artifacts out of application persistence?
- Does it separate metadata durability from raw vault storage?
- Does it avoid selecting database, vault, vendor, provider, crypto, or legal policy?
- Does it keep webhook/outbox/retry deferred?
- Does it require policy snapshot identity before audit reliance?
- Does it keep SignFlow external-consumer-only?
- Can a future builder tell which runtime option, if any, is approved?

## 12. Acceptance Criteria

TIP-11 planning is acceptable when:

- S2 goal is recorded.
- TIP-11 scope is limited to production data boundary and durable state foundation.
- Non-goals explicitly prohibit raw artifact storage, DB/migrations, webhook/outbox/retry, production crypto, vendor selection, pilot readiness, production readiness, and SignFlow runtime dependency unless separately accepted.
- Candidate runtime options are separated from actual dispatch.
- Durable metadata boundary is defined.
- Policy snapshot boundary is defined.
- Retention/deletion/legal-hold primitives are defined as planning primitives, not legal acceptance.
- STOP/RRI questions are listed before any implementation.
- Governance docs identify TIP-11 as active planning only.

## 13. Recommended Next Action

TIP-11 planning is accepted.

Prepare a separate TIP-11 kickoff for Option B and submit it for review before any implementation.

Accepted kickoff candidate:

```text
Option B - Domain/application metadata boundary without DB provider, migrations, raw artifact storage, webhook/outbox/retry, production crypto, or SignFlow runtime dependency.
```

No implementation is authorized by this planning brief.
