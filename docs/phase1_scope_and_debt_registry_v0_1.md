# Phase 1 Scope and Debt Registry v0.1

## S1 In-Scope

- Verification Session API
- Client Application and API Key authentication model
- RequiredChecks policy model
- `VerificationSession` as root business correlation object
- `STANDARD_EKYC_PROFILE` and `CHALLENGE_BOUND_EKYC_PROFILE` policy naming
- CaptureArtifact and EvidenceResult logical model
- Generic `CAPTURE_QUALITY` result category
- CCCD/NFC result shape
- Face match result shape
- Liveness result shape
- Fingerprint result shape
- Evidence VaultRef/hash model
- `EkycEvidencePackage` manifest model
- Append-only audit event model
- Webhook/callback result delivery model, with actual delivery/retry/outbox deferred after TIP-07 Option A
- SignFlow integration contract
- Mock or PoC adapters behind interfaces

## S1 Out-of-Scope

- Production-certified legal eKYC
- Digital signing
- SignFlow database or code dependency
- Certified NFC document validation
- Certified biometric/liveness assurance
- Production fingerprint hardware fleet management
- Production capture artifact retention policy enforcement
- Production capture quality retry policy enforcement
- Production payload signature implementation
- Production webhook signature and replay protection
- Production evidence package signature implementation
- Production capture agent/device trust program
- Full operator review console
- Formal regulatory reporting
- Database migrations and implementation code in the documentation baseline

## Deferred Debts

| Priority | Debt | Description | Exit Trigger |
| --- | --- | --- | --- |
| P0 | Raw biometric protection | Define encryption, access, retention, and deletion controls before real biometric data is stored. | Before pilot with real users |
| P0 | Stable DG2 artifact-hash RRI | Status: OPEN / PARTIALLY-MITIGATED-B1. Current HN212 production capture computes `NfcArtifactHash = SHA256(DG2)` over raw DG2 bytes. This is a stable per-card biometric-derived identifier, proof-bound into append-only evidence history, present in ordinary evidence-table backups, and historically returned to authorized BusinessConsumers through evidence-ledger / evidence-package summary APIs. TIP-87 closed the BusinessConsumer DTO egress path by removing `ArtifactHash`/`PayloadHash`, but the internal append-only, backup, and signed-proof copy remains, so this RRI is not closed. It is not raw image retention, but it is not erasable by crypto-shred or ordinary row deletion. Access control prevents one consumer from reading another consumer's sessions, but does not prevent cross-consumer correlation if the same globally stable value is shared/compared. DPO/Homeowner must explicitly choose a disposition before real-patient hospital-trial reliance: acknowledge/ratify, keep the B1 egress mitigation, or open a proof-contract mitigation TIP (keyed/domain-separated/session-varying binding, with blast-radius to TIP-67G/golden vectors, SignFlow/consumer verification, and Agent hash computation/keying). | Before hospital trial with real patients |
| P0 | Capture artifact retention policy | Define retention, deletion, legal hold, vault lifecycle, and recapture handling for raw capture artifacts. | Before real user artifacts are stored |
| P0 | Capture agent trust/scoping model | Define which agents, SDKs, gateways, and adapters may submit artifacts or evidence results. Business clients must not submit arbitrary `PASSED` evidence. | Before pilot with real capture devices |
| P0 | Legal certification gap | Confirm certification requirements per jurisdiction and use case. | Before production claim |
| P0 | Evidence package signature | Replace placeholder `evidencePackageSignature` with managed signing keys and verification process. | Before external audit reliance |
| P1 | NFC production readiness | Validate supported CCCD/NFC documents, devices, and authentication model. | Before NFC is used for production decisions |
| P1 | Fingerprint hardware dependency | Define device trust, SDK integration, and capture quality controls. | Before fingerprint check becomes mandatory |
| P1 | Capture quality retry policy | Define retry counts, reason codes, terminal `FAILED_CAPTURE_QUALITY`, UX messaging, and operator override rules. | Before production capture flows |
| P1 | Face liveness quality | Select and test liveness/PAD engine for expected threat model. | Before production biometric assurance |
| P1 | Policy versioning | Make RequiredChecks and risk policies versioned and reproducible. | Before multiple client policies |
| P1 | Payload signature model | Define when `payloadSignature` is needed, canonical payload format, algorithm, key management, and verification rules. | Before signed API payload reliance |
| P1 | Webhook signature/replay protection | Define `webhookSignature`, delivery id, timestamp tolerance, replay cache, rotation, and retry behavior. | Before production webhook reliance |
| P1 | Request/correlation id conventions | Define `requestId`, `correlationId`, idempotency, log propagation, and support lookup conventions. | Before multi-client pilot |
| P1 | Profile naming and policy mapping | **Resolved by TIP-67A:** `TRANSACTION_BOUND_EKYC_PROFILE` was neutralized to `CHALLENGE_BOUND_EKYC_PROFILE` with opaque `Challenge` and optional `ClientReference`. Old profile/field names remain input-only compatibility aliases. | Resolved |
| P1 | Decision basis not bound to evidence hash (EBS) | Per-evidence `Confidence`, `decisionReasonCodes`/`retryReasonCodes` (and `RiskScore` when it becomes live) are persisted and append-only-protected but are NOT covered by the manifest/package hash chain — only the final `Result` and `AssuranceLevel` are bound (see `VerificationCompletionApplicationService` decisionSeed/manifestBody). The quantitative/qualitative basis most likely to be litigated is therefore not tamper-evident within TagEkyc's own chain (`PayloadHash` is adapter-asserted and the raw payload is not retained, so `Confidence` is not transitively verifiable). Surfaced by the TIP-65 adversarial spot-check (2026-06-22). Proposed approach: encode `Confidence` as a decimal-string (`F6` invariant, per the TIP-65 numeric convention) + `reasonCodes` as ordered string arrays into the manifest body (= a new package version under the TIP-65 versioning rule); needs the legal lens first (does NĐ 130 / the assurance framework require attesting the quantitative basis, or only `AssuranceLevel` + `Result`?). | Bind via a future evidence-model TIP (number assigned when activated, after signing **TIP-66**) before external/legal reliance on the decision rationale |
| P1 | eKYC neutrality drift (SignFlow coupling) | **Resolved by TIP-67A for shipped S1 behavior:** the eKYC core no longer interprets SignFlow transaction concepts; it echoes opaque challenge/client correlation only. Neutral verifiable proof remains separate in TIP-67B. | Resolved for 67A; 67B remains future proof surface |
| P1 | Raw-export rule-table runtime-role separation (TIP-88A deployment gate) | The TIP-88A raw-export requirement-rule tables `tagekyc.raw_export_requirement_rule_sets` and `tagekyc.raw_export_requirement_rules` are migration-seeded and immutable at runtime. In-DB enforcement (always on, role-independent) is the `reject_raw_export_rule_runtime_mutation()` `BEFORE INSERT/UPDATE/DELETE` trigger. Defense-in-depth (must be provisioned operationally): the production runtime DB principal MUST have `SELECT` only on both tables — no `INSERT`/`UPDATE`/`DELETE`. Production `/readiness` fails closed with `PROD_RAW_EXPORT_RULE_TABLE_MUTATION_PRIVILEGE` (HTTP 503) if the runtime principal holds any mutation privilege, so a mis-provisioned or single-role deployment blocks readiness rather than silently weakening the derivation-rule gate. Concrete role names/grants are operational, not an EF-migration artifact (see the Raw-Export Rule-Table Privilege Gate section of `docs/deployment/hospital_trial/postgres_migration_runbook.md`). Landed as-built in commit `e63cdf9` (TIP-88A). | Before hospital-trial production deploy: provision the SELECT-only runtime role on both rule tables and confirm `/readiness` passes |
| P1 | Raw-export control-plane role provisioning + root-authority bootstrap (TIP-88B1 deployment gate) | TIP-88B1 (raw-export control plane, landed `2c684cf`) adds 4 append-only event tables (`raw_export_grants`, `raw_export_control_authorities`, `raw_export_fulfillments`, `raw_export_policy_lifecycle`) written ONLY via `SECURITY DEFINER` functions. In-DB enforcement (always on): direct raw-SQL INSERT is rejected (append-guard trigger) and the actor principal comes from a transaction-local GUC (`SET LOCAL tagekyc.actor_principal_id`), fail-closed. **Deployment gate (must be provisioned operationally, NOT an EF-migration artifact):** three NOLOGIN CAPABILITY roles the migration creates — `tagekyc_raw_export_deployer` (owns the SD functions + INSERT on the 4 event tables), `tagekyc_runtime` (least-privilege runtime capability: `USAGE` + `EXECUTE` on the 4 append functions; the migration does NOT grant it table `SELECT`), `tagekyc_raw_export_bootstrapper` (deploy-only: `EXECUTE` on `raw_export_bootstrap_global_authority`). **Because `tagekyc_runtime` is NOLOGIN, the app connects as a LOGIN principal provisioned to hold exactly the runtime capability (role membership/inheritance or `SET ROLE`), whose `current_user` is NOT the deployer/bootstrapper. The EFFECTIVE runtime role used during resolver/readiness queries ALSO needs an operational `GRANT SELECT` on the read set the resolver/readiness read directly — 88A `raw_export_policy_versions`/`_requirements`/`_closures`/`_requirement_rule_sets`/`_requirement_rules` + the 4 88B1 event tables — with NO INSERT/UPDATE/DELETE. Grant it DIRECTLY to the `tagekyc_runtime` capability role (so it is effective under BOTH role-inheritance AND `SET ROLE tagekyc_runtime`; a grant to only the login role is suspended under `SET ROLE`). This SELECT is NOT migration-provided; it is a deployment step (a missing SELECT surfaces as a generic resolver/readiness failure, not one of the five listed codes).** Root `GrantAdmin`/`RecorderAuthorityAdmin`/`ActivationAuthority` authorities MUST be bootstrap-seeded (real operator principals, not a dev/default) before use. Production `/readiness` fails closed with `PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING`, `PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT`, `PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE`, `PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID`, or `PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID` if mis-provisioned. NOTE: the actor principal is application-asserted (not DB-authenticated); non-forgeability depends on the `tagekyc_runtime`/deployer role separation being provisioned. See the runbook section "Raw-Export Control-Plane Role & Bootstrap Gate". | Before hospital-trial production deploy: provision the 3 roles, connect the app as a LOGIN principal that inherits/holds the `tagekyc_runtime` capability (NOT deployer/bootstrapper), grant SELECT on the read set to `tagekyc_runtime`, bootstrap real root authorities, and confirm `/readiness` passes |
| P1 | Raw-export aggregate function manifest tripwire | Namespace-wide unknown-function tripwire removed when the TIP-88B1 readiness validator moved to an exact whitelist (TIP-88B2 round 3). This prevents later TIP-88B2/TIP-88B3 functions from breaking the B1 readiness gate, but it also stops B1 from detecting unrelated privileged `raw_export_*` functions such as a future backdoor. | Restore via an aggregate cross-slice function manifest before hospital deployment |
| P2 | Raw-export subject-consent trigger defence asymmetries | TIP-88B2 verification surfaced two defence-in-depth asymmetries. First, `raw_export_subject_consent_classes` has no direct `current_user`/append-context guard on INSERT; it is protected transitively by parent event creation plus the same-transaction/xmin child guard. Second, `RAW_EXPORT_SUBJECT_CONSENT_DIRECT_INSERT_UNSUPPORTED` has separate current-user and append-context branches; tests exercise the context branch, while the current-user branch is normally preempted by `42501` ACL denial before the trigger fires. Neither is a production defect under the current least-privilege + SECURITY DEFINER design, but the unproven branch/shape should be ratified or explicitly strengthened before relying on leaked-privilege defence claims. | Close with a future trigger-hardening/review slice: add a direct class append-context guard if desired, and create a safe harness that exercises the current-user trigger branch without weakening production ACLs |
| P2 | Operator review | Add review queue for ambiguous or failed checks. | Before manual review operations |
| P2 | Webhook observability | Add replay, dead-letter, dashboards, and alerting. | Before high-volume integrations |

## Exit Criteria for S1

- A client application can create a verification session using API key authentication.
- RequiredChecks are persisted and enforced by session lifecycle.
- Mock/PoC capture artifacts and document, NFC, face, liveness, and fingerprint evidence results can be recorded using stable result shapes.
- Capture quality can produce `RETRY_REQUIRED`, `FAILED_CAPTURE_QUALITY`, `REVIEW_REQUIRED`, or `TECHNICAL_ERROR` without collapsing those outcomes into identity failure.
- Final verification result can be calculated from evidence summaries.
- Evidence package can be built using VaultRefs/hashes and a deterministic manifest hash.
- Audit events are written for key lifecycle actions.
- Completion notification result can be prepared through the LocalDev application projection. Actual webhook delivery, retry, and outbox behavior are deferred after TIP-07 Option A and must not be claimed as implemented in S1 closeout.
- SignFlow contract is documented as a challenge-bound profile with client-side binding validation rules.
- Documentation clearly states S1 is not production-certified eKYC.

TIP-09 reconciliation note: current implemented S1 uses generic TrustedAdapter `/evidence-results` for evidence recording, not specialized evidence result routes. Fingerprint remains optional/demo/deferred and is not part of the default SignFlow S1 required checks unless explicitly enabled by policy in a later accepted slice.

## Risk Registry

### CCCD NFC Production Readiness

Priority: P1

Risk: S1 may model CCCD/NFC result shape but not prove production compatibility with official documents, mobile devices, readers, or legal validation requirements.

Mitigation: Keep NFC behind `INfcDocumentReader`, record adapter version, and validate device/document support before production use.

### Fingerprint Hardware Dependency

Priority: P1

Risk: Fingerprint matching depends on capture device quality, SDK support, driver deployment, and secure template handling.

Mitigation: Keep fingerprint behind `IFingerprintMatcher`, classify fingerprint data as Restricted, and require device trust design before pilot.

### Face Liveness Quality

Priority: P1

Risk: Mock or low-quality liveness detection may be bypassed and MUST NOT be treated as production assurance.

Mitigation: Use S1 only for evidence shape and integration readiness. Select a tested PAD/liveness provider for production.

### Raw Biometric Data Protection

Priority: P0

Risk: Raw face, liveness, and fingerprint data create severe privacy and security exposure if stored or logged incorrectly.

Mitigation: Use VaultRef/hash outside the vault boundary, audit access, and define retention before real data collection.

### Stable DG2 Artifact Hash

Priority: P0

Status: OPEN / PARTIALLY-MITIGATED-B1

Risk: The current NFC proof chain stores `NfcArtifactHash = SHA256(DG2)`. Because DG2 is stable for the CCCD, this value can link sessions for the same card/person. It is proof-bound, append-only, and present in backups. Before TIP-87, it was consumer-visible to authorized BusinessConsumers for the owning client application. Client-application access control does not prevent cross-consumer correlation if recipients compare or leak the same globally stable value.

Mitigation: Treat this git-tracked debt entry as the owning hospital-trial DPO/Homeowner disposition record, not as a raw-export TIP-82R detail. TIP-87 closed the BusinessConsumer DTO egress path by removing `ArtifactHash`/`PayloadHash` from BusinessConsumer read DTOs while leaving the internal proof chain unchanged. This partially mitigates third-party disclosure/correlation risk, but the internal append-only, backup, and signed-proof copy remains; the RRI is not closed. If accepted, disclose the retained proof/evidence metadata in the trial DPIA/consent material. If DPO rejects even internal linkability, open a proof-contract mitigation TIP before real-patient reliance.

### Legal Certification Gap

Priority: P0

Risk: S1 evidence readiness does not equal legal eKYC certification.

Mitigation: Avoid production readiness claims and run jurisdiction-specific legal/compliance review before launch.

## Classification Guide

- P0: Blocks real-user pilot or production claim.
- P1: Blocks production reliability, assurance, or scale.
- P2: Improves operations, supportability, or ergonomics after core proof is stable.
