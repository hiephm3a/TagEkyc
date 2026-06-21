# TIP-64 S1 Evidence-Integrity Consolidation — Kickoff v0.1

**File:** `docs/tips/tip_64_s1_evidence_integrity_consolidation/tip_64_kickoff_v0_1.md`
**Version:** 0.3.1
**Status:** Draft — dispatch-ready spec (Tier-2 / evidence-bearing); GPT/Codex review round 3 patches applied; pending convergence confirmation before build
**Date:** 2026-06-21
**Baseline:** `dc9d0ee` (master). Planning brief: `tip_64_planning_brief_v0_1.md` v0.3.1.
**Purpose:** Exact execution contract for Codex to consolidate the as-built S1 evidence-integrity spec into `lld_01` and flag the Tier-2 legal/crypto open items. Docs-only. No code/test/behavior change.

---

## Changelog

### v0.3.1 — GPT/Codex review round 3 (2026-06-21)
- P1: added `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` to the §2 source list (`VerificationCompletedEventDto` is where `WebhookSignatureStatus`/`EvidencePackageSignatureStatus` actually live).
- P2: added TIP-07 to §2(2) as context/cross-check (code still wins).
- (brief) P1: brief §8 STOP/RRI brought in line with kickoff §5 (code-wins resolves discrepancies incl. timestamp/audit; no STOP-trap).

### v0.3 — GPT/Codex review round 2 (2026-06-21)
- P1: STOP/RRI no longer contradicts code-wins. §5 rewritten — code-wins RESOLVES discrepancies (do NOT STOP); §8 narrowed to STOP only on genuinely-unresolvable ambiguity. Listed the known code-wins discrepancies (signature naming, timestamp, audit model) so the builder won't STOP when it reads the stale TIP-06 statements.
- P1: §3.1 audit hashing rewritten to the as-built single `VERIFICATION_COMPLETED` event + manifest auditRefs = existing+completion sorted by EventId, with a `> note:` that TIP-06 §16's three-event/pre-post/exclude-prior model is stale.
- (brief) P2: softened "legal weight / legally-weightiest" → "highest legal/crypto review sensitivity."

### v0.2 — GPT/Codex review patches (2026-06-21)
- P1 timestamp: removed the false "whole-second/UTC" instruction; as-built uses full-precision `DateTimeOffset.UtcNow` via Web JSON with no truncation (compounds T2-1). Property order reworded as implementation-dependent, not portable canonical.
- P1-2 webhook: `webhookSignatureStatus` documented only on the completion-notification projection DTO; added a guard not to promote the deferred `webhook_deliveries` entity to as-built; §3.2 reconciles only the as-built signature fields.
- P2-1: added a stale-name (`payloadSignature|evidencePackageSignature|webhookSignature`) validation grep + DoD.
- P2-2: lld_01 has no metadata block — instruct to add one (matching lld_02/03) with Version 0.2 + Changelog, instead of bumping a non-existent `**Version:**`.

### v0.1 — Initial kickoff
- Authored dispatch-ready Tier-2 consolidation spec: source-of-truth precedence, exact scope (lld_01 evidence-integrity section + signature-field reconciliation + Tier-2 Open Items), DoD, scope floor, STOP/RRI, validation, report format.

## 1. Objective / precondition
Refresh `lld_01_data_model_v0_1.md` (edit in place, do not rename) with an authoritative as-built **Evidence-Integrity** section + reconcile stale signature fields + a **Tier-2 Open Items** block. **Metadata (P2-2):** lld_01 currently has only a `# Logical Data Model v0.1` title and NO metadata block — add a metadata block (File / Version / Status / Date / Baseline / Purpose) matching `lld_02`/`lld_03`, set Version `0.2`, and add a `## Changelog`. **Precondition: GPT + Codex review converged and Homeowner dispatch.** Until then, do not build.

## 2. Source of truth + precedence (mandatory)

**(1) AS-BUILT code wins absolutely.** Read before writing any statement:
- `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` — `HashCanonical`, `DeterministicGuid`, `CanonicalJsonOptions`, the `manifestBody`/`packageHash`/`manifestHash` construction, completion audit payload hashes.
- `src/TagEkyc.Domain/EvidencePackage.cs`, `HashRef.cs`, `SignaturePlaceholderStatus.cs`, `AuditEvent.cs`, `EvidenceResult.cs`.
- `src/TagEkyc.Contracts/InternalAudit/Manifest/ManifestContracts.cs`.
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` — `VerificationCompletedEventDto` (where `WebhookSignatureStatus` + `EvidencePackageSignatureStatus` actually live; the projection DTO, not a persisted webhook entity).

**(2) Then decided TIP kickoffs:** TIP-06 §12/§14/§15/§16; **TIP-07 (completion-notification projection + `WebhookSignatureStatus`/`EvidencePackageSignatureStatus` + no-webhook-dispatch) as context/cross-check only — code still wins**.

**(3) On conflict, code wins** — write the as-built value + a one-line `> note:` recording the superseded statement.

**(4) Preserve existing content:** read `lld_01` IN FULL first; preserve still-correct entity sections + the TIP-49 Provider-Neutral Artifact Evidence Lifecycle section; only ADD the Evidence-Integrity section and reconcile the specific stale signature fields. Do not blind-rewrite the data model.

## 3. Exact scope (lld_01 only)

### 3.1 Add an "Evidence Integrity" section
Document, exactly as-built and traceable to code:
- **Canonicalization contract:** `HashCanonical(label, value)` = SHA-256 over UTF-8 of `"{label}\n{compactJson}"`, output `sha256:<lowercase-hex>`; `compactJson` from `System.Text.Json` with `JsonSerializerDefaults.Web`; document the exact label strings used. **Property order is implementation-dependent** — it is the current anonymous-object declaration order in code, NOT a fixed/portable canonical ordering. **Timestamps (P1):** describe exactly as-built — `DateTimeOffset.UtcNow` is captured once (`operationNow`) and reused as `completedAt`/`createdAt`, then serialized through System.Text.Json Web defaults; there is **NO whole-second truncation and no canonical timestamp formatter**, so full `DateTimeOffset` precision (sub-second + offset rendering) is part of the hash input. Do NOT state any whole-second/UTC-canonical behavior. (This compounds T2-1.)
- **Hash chain:** `manifestBodyHash` (over the manifest body fields) → `packageHash` (over the package envelope incl. `manifestBodyHash`) → `manifestHash` (over `{bodyHash, packageHash}`). Give the exact field set + order of each hashed object as in code.
- **Deterministic ids:** `DeterministicGuid` derivation for `decisionId`, `evidencePackageId`, completion `auditEventId` (content-hash-derived, with the version/variant bit handling as coded).
- **Audit hashing & manifest audit-refs (as-built — differs from TIP-06; code wins):** the code creates exactly ONE completion audit event `VERIFICATION_COMPLETED` (payload-hash label `tip-06-completion-audit-payload`); the manifest `auditEventRefs` = ALL existing session audit events PLUS that completion event, sorted by `EventId` (ordinal). Document this exactly. Add a `> note:` that TIP-06 §16's three-event model (`FINAL_DECISION_CALCULATED`/`EVIDENCE_PACKAGE_CREATED`/`SESSION_COMPLETED`), its pre/post audit-ref split, and its exclude-prior-events rule are NOT as-built (stale).
- **Signature-status model:** `SignaturePlaceholderStatus` has one value `PlaceholderUnverified`; the layers are status markers only — NO cryptographic signing exists in S1. As-built locations (P1-2): `evidencePackageSignatureStatus` (evidence package), `payloadSignatureStatus` (evidence result), and `webhookSignatureStatus` **only on the completion-notification projection DTO `VerificationCompletedEventDto`** — `webhookSignatureStatus` is NOT a persisted webhook entity field. The `webhook_deliveries` entity in lld_01 is a future/deferred concept (webhooks are deferred in S1), not as-built.

### 3.2 Reconcile stale signature fields (code-wins) — only the AS-BUILT ones
- `evidence_packages`: `evidencePackageSignature` → `evidencePackageSignatureStatus` (status enum per as-built `EvidencePackage.EvidencePackageSignatureStatus`); add a `> note:`.
- `evidence_results`: `payloadSignature` → `payloadSignatureStatus` (per as-built DTO/domain); add a `> note:`.
- **Webhook guard (P1-2): do NOT rename or promote `webhook_deliveries.webhookSignature` to an as-built status field.** The `webhook_deliveries` entity and its `webhookSignature` are deferred/conceptual in S1 — leave them, and add a `> note:` marking them deferred/projection-only (the only as-built webhook signature status is on `VerificationCompletedEventDto`). Reconcile signature fields ONLY where an as-built equivalent exists; do not invent reconciliations.

### 3.3 Tier-2 Open Items block (FLAG, do NOT resolve)
Add a clearly-marked block stating these are open legal/crypto decisions, NOT resolved by this TIP:
- **T2-1 Canonicalization is implementation-deterministic, not a portable standard.** The hash reproducibility depends on the .NET `System.Text.Json` Web serializer and anonymous-object property order; it is NOT RFC 8785 (JCS). Consequences to state: a future field-reorder/serializer change silently breaks historical hashes; the hash is not independently reproducible by a non-.NET verifier. **Requires legal/crypto sign-off** before any production/legal reliance (candidate: adopt RFC 8785 JCS or COSE/JWS). Links: EBS-01.
- **T2-2 Signatures are placeholders only.** All signature statuses are `PlaceholderUnverified`; no key, no signing, no verification exists. The evidence is NOT cryptographically signed and provides no non-repudiation or legal-audit reliance. **Requires production signing (HSM/KMS) decision** — P0 debt. Links: EBS-07.

The section MUST NOT claim the as-built hashing/signatures are legally sufficient, production-grade, non-repudiable, or audit-reliable.

## 4. Out of scope (do not touch)
`lld_02_*`, `lld_03_*`, `lld_04_*`, `tagekyc_hld_*`, `docs/00_*`, `src/**`, `tests/**`, any other doc. No new design; no adopting JCS or implementing signatures; no behavior/API/DTO change; no resolving the Tier-2 items.

## 5. Contradiction handling — code-wins RESOLVES discrepancies (do NOT STOP)
"Code wins absolutely" (§2.1) already resolves every code↔TIP-06 discrepancy: write the as-built value + a `> note:` that the TIP-06 statement is stale, and list each in the closeout. **Known discrepancies to expect (apply code-wins, do NOT STOP):**
- **Signature field naming** → as-built `...SignatureStatus` (§3.2).
- **Timestamp** → TIP-06 §14 "whole-second UTC" vs code full-precision `DateTimeOffset.UtcNow`, no truncation (§3.1).
- **Audit model** → TIP-06 §16 three events + pre/post split + exclude-prior vs code's single `VERIFICATION_COMPLETED` event with manifest auditRefs = existing + completion sorted by `EventId` (§3.1 audit hashing).
Only a discrepancy that genuinely CANNOT be resolved by reading the code (ambiguous which is correct) goes to STOP (§8).

## 6. Definition of Done (verifiable)
- [ ] lld_01 has an Evidence-Integrity section: canonicalization (label+format+property-order+timestamp behavior), hash chain (exact field sets + order), deterministic ids, audit hashing, signature-status model — all matching the code.
- [ ] `evidence_packages`.`evidencePackageSignature` and `evidence_results`.`payloadSignature` reconciled to as-built `...SignatureStatus`, each with a note. `webhook_deliveries.webhookSignature` left as deferred/conceptual (NOT promoted to as-built), with a note; `webhookSignatureStatus` documented only on the completion-notification projection DTO.
- [ ] No stale signature literal (`payloadSignature`/`evidencePackageSignature`/`webhookSignature`) remains except inside an explicit stale/deferred note.
- [ ] Tier-2 Open Items block present: T2-1 (canonicalization-not-JCS) and T2-2 (placeholder signatures), each marked "requires legal/crypto sign-off", linked to EBS-01/EBS-07.
- [ ] No statement claims legal sufficiency / production-grade / non-repudiation / audit reliance.
- [ ] Every consolidated statement traceable to a cited code file or TIP-06 section; no statement contradicts code.
- [ ] Persistence-agnostic: no DB/FK/migration/durability facts added.
- [ ] lld_01 metadata block added (matching lld_02/03) with Version 0.2 + a `## Changelog` entry. Existing entity sections + TIP-49 lifecycle section preserved.
- [ ] `git diff --stat` shows ONLY `docs/lld_01_data_model_v0_1.md` changed. Docs-only.
- [ ] OPEN list for any TIP-06 rule lacking code.

## 7. Scope floor (anti-creep, both directions)
- Docs-only; only `lld_01`. Touching anything else = defect → STOP.
- Every sentence traces to code or TIP-06. No invented rules, no new design, no resolving Tier-2 items. Under-consolidation (omitting a built integrity rule) is also a defect.

## 8. STOP/RRI
- A TIP-06 rule with no corresponding code → flag OPEN, do not write as authoritative.
- A code↔TIP-06 discrepancy NOT covered by §5 **and** not resolvable by reading the code (genuinely ambiguous) → STOP and report. The §5 known discrepancies (signature naming, timestamp, audit model) are resolved by code-wins — do NOT STOP on those.
- Any attempt to resolve a Tier-2 item (adopt JCS / implement signing) → STOP (Homeowner/legal decision).
- Any required change outside `lld_01` → STOP.

## 9. Validation + report
- Docs-only → no test run. Run static validation:
  - `git diff --stat` (confirm only lld_01 changed) and `git diff -- docs/lld_01_data_model_v0_1.md`.
  - `rg "JCS|RFC 8785|legally sufficient|non-repudiation|production-grade|whole-second|PlaceholderUnverified|SignatureStatus" docs/lld_01_data_model_v0_1.md` — confirm Tier-2 limits stated, no over-claim, and no false whole-second timestamp claim.
  - `rg "payloadSignature|evidencePackageSignature|webhookSignature" docs/lld_01_data_model_v0_1.md` — any remaining stale signature literal MUST sit inside an explicit stale/deferred note (P2-1).
- Do NOT commit (await Contractor review).
- Report: 5-line summary + (a) the Evidence-Integrity subsections written; (b) signature-field reconciliations; (c) Tier-2 Open Items as written; (d) any OPEN rule + any code↔TIP discrepancy found.

## 10. Review after build (Contractor)
Contractor adversarial spot-check (this is EBS-01/02/07 — highest stakes): verify the canonicalization + hash-chain description matches the code EXACTLY (label strings, field order, timestamp handling — a wrong description here would mislead a future legal/crypto verifier); confirm signature-field reconciliations match as-built; confirm the Tier-2 limits are flagged honestly with no legal-sufficiency overclaim; confirm no new design / no resolution of Tier-2 items. Then closeout, and register T2-1/T2-2 as Homeowner+legal debt items.
