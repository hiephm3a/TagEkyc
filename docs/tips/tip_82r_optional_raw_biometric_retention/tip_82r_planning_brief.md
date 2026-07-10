# TIP-82R - Optional Raw Biometric Export/Retention and Disposal Policy - Planning Brief

## Status

PROPOSED / DECISION TIP ONLY. **v1.1 (2026-07-10) - consistency fixes (DoD ratifies D-1..D-10 not D-9; D-2 rewritten as mode x controller pair, `SignFlow evidence store` removed from the option list; the DG2-RRI mitigation menu renamed (i)/(ii)/(iii) by disposition so "Option B" no longer collides with Track B). v1.0 - CC framing patch (Homeowner-approved scope):** (1) "supports" -> **governed PATH TO BUILD**; capability does NOT exist at runtime and is code-enforced fail-closed; (2) governing principle added - *TagEkyc avoids ungoverned raw BIO, not raw BIO*, with the irrevocability rationale for why the gate must cost a decision, not a config flag; (3) absolute `NEVER retain liveness` -> "not approved absent a specific Legal/DPO-approved flow" (gated default, not prohibition); (4) "defer Track B entirely" -> **defer UNTIL a retention purpose is established and ratified** (evidence-driven, not ideological); (5) **THREE modes** named separately (`external_export_only_no_retain` / `encrypted_export_packet` / `encrypted_raw_vault_retained`) - collapsing the packet mode into export-only is a named STOP; (6) **controller is an ORTHOGONAL AXIS** (TagEkyc / SignFlow / external TSP), `signflow_evidence_store` removed from the mode list - ratify the PAIR (mode x controller); (7) **new D-10 cross-border transfer gate** (jurisdiction of recipient AND processing infrastructure; fail-closed if outside VN or unknown) - previously absent entirely; (8) `SHA256(DG2)` RRI cross-referenced as a **governance lesson**, still owned by the debt register (`fa98531`), NOT by this TIP; (9) decision-only preserved - no runtime build, no B1/B2, no proof-contract change; (10) **no self-citation of legal instrument numbers** - Legal confirms the instrument/articles in force at ratification. Prior: **v0.9** - v0.2 patched after Contractor review: added a recommendation per decision (so this can be RATIFIED, not just read), a Track A/Track B split, the crypto-shred vs append-only analysis, the consent-withdrawal-after-retention transition, and the controller-liability flag. v0.3 folded GPT review: explicit mode matrix, recommended initial posture, consent purpose/recipient binding, per-flow minimum-needed table, backup/object-version blocker, stricter access/export audit defaults, BusinessConsumer alternatives, and concrete follow-on split. v0.4 folded CC + GPT deep review: corrected trigger/crypto-shred overclaims, pinned Track A terminal/retry policy, added D-8 export topology and D-9 immutable policy versioning, expanded key hierarchy/audit/source-trace/recipient-erasure requirements, and recorded verify-on-code facts. v0.5 folded CC re-review: removed remaining crypto-shred "ONLY" wording, reconciled D-5 with existing proof-bound raw-byte `ArtifactHash`, defaulted D-8 away from server raw packet handoff, split `disposed` vs `crypto_shredded`, and clarified Track A disposal attestation vs crypto-shred proof. v0.6 verifies `NfcArtifactBytes` exactly: current HN212 production path hashes DG2 bytes, so `NfcArtifactHash` is a stable per-card biometric-derived identifier. v0.7 extracts that RRI to the hospital-trial DPO gate/debt register, records that the value is consumer-visible via current business read APIs, and keeps TIP-82R as a cross-reference rather than the owning approval gate. v0.8 corrects the framing: TIP-82R is the general raw-BIO retention/export policy for eKYC capture material; EIDCA is only a vendor/documentary example showing that a downstream integration may demand raw BIO exchange. v0.9 adds the cheaper mitigation path: stop disclosing `ArtifactHash` in consumer-facing ledger/package-summary DTOs before accepting risk or changing the proof contract.

This TIP does not authorize a runtime raw vault implementation. It records the policy, lifecycle, consent, audit, disposal, and integration decisions required before any raw biometric retention/export mode can be built.

Default remains `verify_and_discard`: raw DG2/selfie/liveness material exists only transiently in CaptureAgent memory, while the TagEkyc server stores hashes, evidence, proof material, and audit-safe metadata only.

**TIP-82R itself is NOT on the hospital-trial critical path unless the trial policy asks to retain/export raw BIO.** The BHXH discharge trial currently runs on `verify_and_discard` with no raw-BIO retention/export. TIP-82R may remain parked for that trial as long as the trial does not require raw storage/export. **Exception discovered by this TIP:** current `NfcArtifactHash = SHA256(DG2)` is a separate hospital-trial RRI because it exists in today's verify-and-discard proof chain. That RRI is owned by the git-tracked Phase 1 debt/RRI register (`docs/phase1_scope_and_debt_registry_v0_1.md`); any hospital-trial DPIA/consent draft is a downstream mirror, not the ownership record. TIP-82R only cross-references it.

## Context

TagEkyc currently uses a no-raw-server posture. CaptureAgent processes raw eKYC capture material in memory: CCCD chip data (`SOD`, DG groups including DG1/DG2/DG13/DG15 when read), chip portrait/DG2, live selfie, liveness frame/video or PAD artifacts, optional hand-signature image, and other flow-specific raw capture data. It submits proof-bound evidence/hashes and disposes raw buffers. Business-facing surfaces remain proof/hash-only and must not expose raw biometric artifacts.

TIP-82 intentionally made `TAGEKYC_BIOMETRIC_RETENTION_MODE=raw_vault` fail-closed with `RETENTION_POLICY_UNSUPPORTED`. TIP-82 also deferred any raw biometric evidence vault to TIP-82R, gated by explicit consent, Legal/DPO retention policy, encrypted storage, access audit, controlled export, deletion/crypto-shred, and legal-hold workflow.

This TIP is not scoped to TSP/EIDCA. EIDCA/TSP is one possible downstream consumer/use-case that demonstrates why raw-BIO export might be requested. The policy must remain neutral and reusable for any approved eKYC flow that asks to retain or export raw capture material.

## Vendor Evidence: EIDCA 1.0.8

EIDCA 1.0.8 is a concrete vendor/documentary example, not the scope boundary of this TIP. It shows that at least one downstream integration may ask TagEkyc/SignFlow to exchange raw capture material:

- CTS registration/renewal schemas require `raw_data` fields carrying Base64 chip material: `sod`, `dg1`, `dg2`, `dg13`, and `dg15`.
- The same flows require `info.image` as a Base64 selfie image.
- Some registration flows describe `face_matching` with `chip_image`, `selfie_image`, and `face_matching_score`.
- The signing flow clearly requires selfie Base64 via `info.image`.
- The signing schema is inconsistent about whether DG2/raw chip data is required: some text says send DG2 + selfie or `chip_raw_data.sod/dg2`, while other schema areas emphasize selfie. This is a vendor-confirmation gap, not a TagEkyc assumption.

Why now: `verify_and_discard` is still the privacy baseline, but TagEkyc now has credible evidence that some approved eKYC consumers may request raw BIO retention/export. EIDCA is the current example; future examples could be dispute review, regulated external acceptance, customer retention requirements, or special capture modalities. TagEkyc therefore needs a policy-first decision TIP before raw Base64/files/packets can appear in any implementation path. Raw biometric must not be smuggled through ordinary DTOs, logs, receipts, normal DB tables, or business APIs.

### Example Source Trace: EIDCA (v0.4)

| Source | Version / reviewed date | Observed fields | Confidence | Reviewer |
| --- | --- | --- | --- | --- |
| Controlled copy required before EIDCA-flow ratification; current working source: `C:/Users/Admin/Downloads/EIDCA -Tai Lieu - 1.0.8.pdf` (local source filename contains Vietnamese accents); SHA-256 `087f2939d54d4872425be17ea876de26da03957d148a7cd4f058073fb13a71c6` | EIDCA 1.0.8 / reviewed 2026-07-10 | `raw_data.sod`, `raw_data.dg1`, `raw_data.dg2`, `raw_data.dg13`, `raw_data.dg15`; `info.image`; `face_matching.chip_image`, `face_matching.selfie_image`, `face_matching_score`; `chip_raw_data.sod/dg2` appears in signing-related text | Enrollment/renewal raw-data need = document-observed against the local PDF; signing selfie = document-observed; signing DG2/chip raw = inconsistent/vendor-gap | Codex + reviewer-supplied EIDCA findings |

Before Homeowner/Legal/DPO ratifies any EIDCA-backed flow, store a controlled copy or controlled doc-store record for this PDF outside a personal Downloads path and record its file id/path plus SHA-256. Vendor confirmation must later add: confirmation date, vendor contact/channel, exact API field mapping, per-flow required raw classes, post-acceptance retention obligations, recipient erasure/withdrawal behavior, and accepted export topology.

## Intent

Define the optional raw biometric retention/export policy for eKYC capture material that can be activated only by explicit Legal/DPO-approved policy and consent scope.

### Governing principle (v1.0)

> **TagEkyc does not avoid raw BIO at all costs; it avoids UNGOVERNED raw BIO.**
> The raw retention/export capability **does not exist at runtime today** and is **code-enforced fail-closed** (`raw_vault` -> `RETENTION_POLICY_UNSUPPORTED`; unknown modes -> `RETENTION_POLICY_INVALID`). This TIP does not create the capability.
> When a lawful business/regulatory workflow genuinely requires raw BIO, there is a **governed PATH TO BUILD it** - via explicit purpose + legal basis/consent, a minimum-needed raw class set, DPO/Homeowner ratification, encryption, access/export audit, retention trigger + duration, disposal/crypto-shred, legal-hold handling, and no raw in ordinary DB tables, logs, receipts, or BusinessConsumer DTOs.
> "Governed" means **the gate costs a decision, not a config flag.**

**Why the gate must be expensive, not merely present:** biometric material is **irrevocable**. A leaked password is rotated; a leaked face/fingerprint/chip-portrait is not. The decision cost must therefore be proportional to the irreversibility of the harm - which is why every mode below requires a ratified purpose, not an operator toggle.

**Governance lesson (cross-reference, NOT owned here).** The separate `SHA256(DG2)` RRI - owned by the git-tracked Phase 1 debt register (commit `fa98531`), not by TIP-82R - is **not** a reason to treat raw BIO as taboo. It IS admissible evidence about the **control environment**: a stable per-person identifier entered the append-only + signed-proof + consumer-API surface **without a DPO decision, without a DPIA entry, and unnoticed until adversarial verify-on-code surfaced it**. If ungoverned *hashes* could slip through, ungoverned *raw images* are the same failure mode at larger scale. That is exactly why this TIP asks for **pre-authorization** of the raw path rather than post-hoc discovery - and why the DPO should weigh it when calibrating how much process to require before enabling Track B. **Separate the ownership; do not separate the lesson.**

Preserve the default TagEkyc posture:

- Default mode: `verify_and_discard`.
- Raw retention/export: optional, policy-gated, and fail-closed until approved. **Not prohibited - unbuilt and ungranted.**
- TSP/EIDCA: example consumer/use-case only, not the policy scope. A regulated downstream (TSP acceptance, dispute handling, statutory obligation) may legitimately require raw exchange.
- Business surfaces remain proof/hash-only unless a separate controlled export workflow is approved.

## Track split - decide A and B INDEPENDENTLY

TIP-82R covers two different capabilities with different legal bases, consent scopes, and lifecycles. Split them so a transient export can be ratified without accidentally approving long-term raw retention:

- **Track A - EXPORT, transient.** Raw material is assembled, submitted to an approved recipient/adapter, and disposed. Modes: `external_export_only_no_retain`, `encrypted_export_packet`.
- **Track B - RETENTION at rest (vault).** Raw biometric is kept beyond the export transaction, e.g. for a dispute window. Mode: `encrypted_raw_vault_retained`. Legal basis, consent purpose, DPIA, controller assignment, and retention schedule are SEPARATE Legal/DPO decisions.

Legal/DPO must confirm the legal basis under the **applicable VN personal-data regime**; Legal identifies the exact instrument and articles in force **at ratification time** (this TIP deliberately does not self-cite instrument numbers).

**Track A can be approved WITHOUT approving Track B.** A consumer/integration requirement to EXCHANGE raw material is not proof that TagEkyc should KEEP it. Approving B "because A needs raw material" is the failure mode this split prevents.

### THREE modes, not two (v1.0)

Do NOT collapse the export modes into one. `encrypted_export_packet` is **transient-but-persisted** (survives a crash, has a TTL, needs crypto-shred + backup semantics). It is the mode most likely to be needed in practice and the one most likely to be smuggled in under the label "export only" if it is not named separately.

1. **`external_export_only_no_retain`** - raw exists only in memory for one submission attempt. No durable retry.
2. **`encrypted_export_packet`** - short-lived ENCRYPTED packet persisted until submission/acceptance; TTL + crypto-shred + disposal proof + backup/object-version semantics required before build.
3. **`encrypted_raw_vault_retained`** - raw kept at rest for a ratified retention purpose; per-artifact keys, legal hold, access workflow, dual-control, crypto-shred proof.

### Controller is an ORTHOGONAL AXIS, not a mode (v1.0)

`signflow_evidence_store` is **NOT a storage mode** and must not appear in a list of modes - listing it there is exactly how a **legal liability decision** gets chosen as if it were a disk location. Model it as a second, independent dimension:

| Axis | Values |
| --- | --- |
| **Mode** (what/how long) | `verify_and_discard` / `external_export_only_no_retain` / `encrypted_export_packet` / `encrypted_raw_vault_retained` |
| **Controller** (who is legally responsible for the raw copy) | TagEkyc / SignFlow / external TSP-recipient |

Every combination must be ratified as a PAIR (mode x controller). Assigning the controller to SignFlow or an external TSP transfers DPIA, consent text, subject-rights handling, breach-notification duty, retention, and disposal proof to that party. That is a Legal/DPO decision about **liability**, not a technical one about disks.

## Recommended Initial Posture (v0.3)

- Keep TagEkyc default `verify_and_discard`.
- Do NOT build `encrypted_raw_vault_retained` for the hospital trial or as a side effect of any external integration.
- If an approved consumer requires raw material, prefer `external_export_only_no_retain`.
- Permit `encrypted_export_packet` only when retry/acceptance handling genuinely requires a short-lived encrypted packet with TTL and disposal proof.
- **Defer `encrypted_raw_vault_retained` UNTIL a retention PURPOSE is established and ratified** (not "defer entirely" - the deferral is evidence-driven, not a prohibition). Enabling it requires an explicit Legal/DPO retention requirement, consent text, controller assignment, key-management design, legal-hold workflow, and backup/object-version disposal semantics.

## Mode Decision Matrix

Mode answers *what/how long*. **Controller** (who is legally responsible for the raw copy) is a SEPARATE axis - see the controller table above. Ratify the PAIR (mode x controller) per flow.

| Mode | Track | Raw export | Raw retention at rest | Approval posture | Disposal/audit |
| --- | --- | --- | --- | --- | --- |
| `verify_and_discard` | Baseline | No | No | Default; the only mode that exists today | Agent disposes raw buffers after verification; proof/hash-only server records. |
| `external_export_only_no_retain` | Track A | Yes, only to approved recipient | No | Recommended first posture when an approved flow requires raw exchange | Dispose immediately after accepted submission or terminal failure; immutable export audit; disposal is an ATTESTATION (no ciphertext/key existed). |
| `encrypted_export_packet` | Track A | Yes, via encrypted packet | Short-lived pending-export packet only | Allowed only if retry/acceptance genuinely requires durable retry | TTL, crypto-shred, disposal proof, and backup/object-version decision required before build. |
| `encrypted_raw_vault_retained` | Track B | Controlled export possible | Yes | Deferred UNTIL a retention purpose is established and ratified | Per-artifact keys, legal hold, access workflow, dual-control for human access, crypto-shred proof. |

Homeowner/Legal/DPO must ratify a specific **mode x controller** pair per flow. Approval of Track A does not approve Track B. **No mode above except `verify_and_discard` exists at runtime today.**

Mode-gate invariant (v0.4, verified against CaptureAgent):

- Today, unset/`verify_and_discard` passes.
- Today, `raw_vault` is recognized but fails closed before capture with `RETENTION_POLICY_UNSUPPORTED`.
- Today, any unknown mode fails closed before capture with `RETENTION_POLICY_INVALID`.
- Future Track A must use a distinct mode such as `external_export_only_no_retain`; enabling Track A must NOT enable `raw_vault` / Track B.
- `raw_vault` remains unsupported unless and until a separate Track B build exists.

Track A terminal/retry policy (v0.4):

- `external_export_only_no_retain` means no durable retry.
- Raw may exist only in memory during one submission attempt.
- If submission cannot complete, raw is disposed unless an approved `encrypted_export_packet` policy exists.
- Retry that requires raw after process crash is NOT allowed in pure export-only mode.
- Accepted submission, terminal external reject, user cancellation, timeout/terminal failure, or consent withdrawal during pending export all cause immediate disposal and immutable audit.
- Durable retry means switching to `encrypted_export_packet`, with TTL, crypto-shred, disposal proof, and backup/object-version semantics approved before build.

## Scope Allowed

Decision/planning only:

- Define allowed raw BIO classes.
- Define lifecycle states and transition rules.
- Define consent and policy gates.
- Define allowed storage/export patterns.
- Define audit and access requirements.
- Define deletion/crypto-shred expectations.
- Define vendor/recipient-confirmation questions for any external raw-BIO exchange flow; EIDCA is the current example.
- Define what a future build TIP must implement and test.

Allowed future modes, subject to decision:

- `verify_and_discard`: remains default.
- `external_export_only_no_retain`: raw packet generated for approved external submission, then discarded.
- `encrypted_export_packet`: short-lived encrypted packet retained only until submission/acceptance.
- `encrypted_raw_vault_retained`: encrypted vault at rest with TTL, legal hold, audit, and crypto-shred.

Controller (orthogonal axis, NOT a mode): TagEkyc / SignFlow / external TSP-recipient. `signflow_evidence_store` is deliberately NOT listed as a mode - it is the (mode x controller=SignFlow) pair and carries a controller-liability transfer.

## Out of Scope

- No runtime raw vault implementation in this TIP.
- No DB schema, EF migration, storage adapter, object store, or key-management build.
- No change to current proof/hash-only business APIs.
- No BusinessConsumer raw download endpoint.
- No TSP integration claim inside TagEkyc core.
- No weakening of TIP-82 consent gate or `verify_and_discard`.
- No raw biometric in normal logs, receipts, proof payloads, or generic evidence records.

## Decisions Needed - each with a Contractor RECOMMENDATION to ratify or override

Homeowner / Legal / DPO decide. Every item below carries a proposed answer + rationale so this TIP can be RATIFIED in one pass rather than re-designed. The bias throughout is **data-minimization + fail-closed toward less raw material**.

**D-1. Raw classes allowed.** Options: CCCD chip DG/SOD data (`sod`, DG1/DG2/DG13/DG15 or other groups actually read) / DG2 portrait or extracted chip portrait / selfie image / liveness frame-video or PAD artifacts / hand-signature image / face-matching material (`chip_image`,`selfie_image`,`face_matching_score`) / other flow-specific raw capture data.
> **RECOMMEND (minimal):** approve raw classes per flow, purpose, and recipient; default to **none retained/exported** unless the approved flow proves need. When export is required, allow only the minimum raw class set needed by that flow and prefer Track A transient export. Liveness frame/video: **not approved absent a specific Legal/DPO-approved flow** (highest sensitivity, no demonstrated general need today - a gated default, NOT a prohibition). Hand-signature image and other special capture data require a specific approved flow and consent purpose.
> *Rationale:* every extra class widens the DPIA scope and the breach blast-radius. Adding a class later is cheap; un-collecting it is not.

Minimum-needed rule (v0.3): for each flow, export only the raw class set required by the approved vendor contract and consent policy. Do not export every raw class that the Agent happens to have captured.

| Flow / use-case | Recommended allowed raw classes | Confirmation status |
| --- | --- | --- |
| Generic eKYC verification | None retained/exported; process in memory only under `verify_and_discard` | Default posture |
| External raw-BIO exchange | Minimum raw class set required by approved recipient contract and consent purpose, Track A preferred | Needs per-recipient confirmation before build dispatch |
| EIDCA enrollment | NOT APPROVED - enumerated only if/when that flow is pursued: CTS-schema-required `sod`, `dg1`, `dg2`, `dg13`, `dg15` plus selfie, Track A only | Needs vendor confirmation before build dispatch |
| EIDCA renewal | NOT APPROVED - enumerated only if/when that flow is pursued: CTS-schema-required `sod`, `dg1`, `dg2`, `dg13`, `dg15` plus selfie, Track A only | Needs vendor confirmation before build dispatch |
| EIDCA signing | Selfie via `info.image`; DG2/chip raw blocked by default, including `face_matching.chip_image` | Selfie confirmed from EIDCA 1.0.8; DG2/chip raw is vendor gap |
| Dispute retention | TBD raw class set | Legal/DPO decision; Track B only |
| Liveness media | None by default | Do not retain/export unless Legal/DPO approves a specific flow |
| Hand signature image | Separate policy class if used | Not approved by this TIP unless a specific approved flow requires it |

`face_matching.chip_image` is DG2/chip-raw for policy purposes and is blocked by the same default as DG2. Only `selfie_image` and `face_matching_score` are outside that DG2/chip-raw block. Derived scores/proof hashes are not raw BIO, but may still be sensitive metadata and must remain audit-safe/proof-bound. `face_matching_score` is not a liveness frame/video retention approval.

**D-2. Mode x controller pair (per flow).** Two independent axes, ratified as a PAIR:
> - **MODE** (what/how long): `external_export_only_no_retain` / `encrypted_export_packet` / `encrypted_raw_vault_retained`.
> - **CONTROLLER** (who is legally responsible for the raw copy): TagEkyc / SignFlow / external TSP-recipient.
>
> `SignFlow evidence store` is deliberately NOT a mode option - it is the (mode x controller=SignFlow) pair and carries a controller-liability transfer, not a disk choice.
> **RECOMMEND:** approve **Track A = `external_export_only_no_retain`** as the default approved mode, with **`encrypted_export_packet`** (short, bounded TTL) permitted ONLY where retry/acceptance genuinely requires holding the packet. **DEFER Track B (`encrypted_raw_vault_retained`) UNTIL a retention purpose is established and ratified** - the deferral is evidence-driven (no retention purpose has been demonstrated), NOT a prohibition. Controller assignment is decided separately on its own axis.
> *Rationale:* a requirement to submit/exchange raw material is not the same as a requirement to keep it. Track B costs a controller assignment, a DPIA, a retention schedule, and a disposal worker - none of which is justified without an explicit retention purpose.
> **WARNING - CONTROLLER IS A SEPARATE AXIS, NOT A MODE.** Assigning the raw-copy controller to SignFlow or an external TSP-recipient REASSIGNS THE DATA CONTROLLER/PROCESSOR BOUNDARY. Legal/DPO must confirm responsibilities under the **applicable VN personal-data regime** (Legal identifies the instrument and articles in force at ratification; this TIP does not self-cite): DPIA, consent text, subject-rights handling, breach-notification duty, retention, and disposal proof. Choosing it is a LEGAL decision about liability, not a technical one about disks. Ratify the PAIR (mode x controller); never pick a controller as "just where the bytes live".

**D-3. Retention trigger and duration.** Options: until external acceptance / through workflow completion / dispute window / only under legal hold / immediate disposal after success.
> **RECOMMEND:** retain **only until the approved external/workflow acceptance point**, with a short bounded TTL for retry (hours, not days), then **immediate crypto-shred** if anything was retained encrypted. After workflow success (for example CTS issuance/signing success in a TSP flow) -> dispose IMMEDIATELY unless a Legal/DPO-approved hold applies. **No dispute-window retention** unless Legal specifically requires it - and if so it becomes Track B with its own consent + DPIA.

**D-4. Consent scope.** Options: separate `export_raw_biometric_to_recipient` / `retain_raw_biometric` / `retain_for_dispute` / whether reuse for a new purpose needs fresh consent.
> **RECOMMEND:** SEPARATE, explicit scopes - `export_raw_biometric_to_recipient` (Track A, recipient-bound) and `retain_raw_biometric` (Track B, deferred). **Do NOT bundle either into the existing capture consent.** Reuse for a new purpose (for example signing-time selfie reuse) -> **requires fresh consent** (purpose-limitation under the applicable VN personal-data regime; Legal confirms the instrument/articles at ratification).
> **Coupling to the SHIPPED consent contract (do not re-invent):** these scopes must flow through the existing chain - the caller emits a bound consent assertion, the Agent gate verifies `scope >= required` (`TAGEKYC_CONSENT_*`, TIP-82), and the withdrawal sentinel remains live. A new scope is a new value in that contract, not a new mechanism.

Purpose/recipient binding (v0.3): the consent assertion and policy record must bind more than the scope string. They must bind:

- purpose: verification, enrollment, renewal, signing, dispute, or the approved customer-specific purpose;
- recipient: approved external recipient / adapter identifier;
- raw classes allowed;
- retention/export mode;
- TTL and disposal trigger;
- consent reference and policy id.

A generic raw-BIO export scope is not enough by itself; it must not be reusable across recipients, purposes, or broader raw class sets.

**D-5. Access/export audit.** Options: who may request/approve export, what is logged, dual-control, immutable audit per packet.
> **RECOMMEND:** export requestable ONLY by an approved service identity - **never a BusinessConsumer**. Every export emits an **immutable audit event** carrying: opaque session id, policy id, export id + packet HASH, recipient, actor/service identity, timestamp, result. **NO raw Base64, no file path, in audit.** Dual-control is required for any Track-B vault ACCESS (deferred with Track B); Track A export does not need dual-control if it is fully automated and audited.
> *Fit note:* the server's existing `audit_events` table is DB-trigger append-only - a good home for these records while the trigger is installed/enabled and the runtime role remains least-privileged.

Access/export audit defaults (v0.3):

- all raw export/access events are immutable-audited;
- export requires service identity, policy id, consent id/ref, recipient id, and packet hash/id;
- automated recipient/adapter submission may use a pre-approved service policy, but still logs recipient and packet hash/id;
- human-initiated export, manual re-export, or any raw-vault access requires dual-control approval;
- no audit event may contain raw Base64 or a filesystem path to raw material.
- no new raw-export audit event may store plaintext-biometric hashes unless Legal/DPO explicitly approves them as biometric-derived metadata.
- preferred audit identifiers: opaque artifact id, policy id + version, consent id/ref, recipient id, export packet hash, and ciphertext hash.
- avoid `SHA256(raw selfie)`, `SHA256(raw DG2 portrait)`, or `SHA256(raw chip image)` by default; those are stable biometric-derived identifiers.

Existing proof-chain RRI (v0.7, verified on code): current CaptureAgent computes `ArtifactHash` as `sha256` over plaintext artifact bytes. For selfie and liveness this identifies a capture instance and changes per capture. For NFC, the current HN212 production path sets `NfcArtifactBytes = dg2.ToArray()` in `ReflectionHn212SdkBridge`, then `Hn212CccdReader` computes `CaptureAgentHash.Sha256(read.NfcArtifactBytes)`. Therefore `NfcArtifactHash` is `SHA256(DG2)` today: a stable per-card biometric-derived identifier that can link sessions for the same CCCD.

This is an existing, proof-bound, previously-unratified RRI, not a comfort exemption. It is not a raw-export audit event and TIP-82R does not retroactively invalidate existing signed proof/hash surfaces, but the risk is **not owned by TIP-82R** because it applies to today's verify-and-discard hospital trial. The owning record is the git-tracked Phase 1 debt/RRI register; hospital-trial DPIA/consent text may mirror it, but must not be the only ownership record. The hospital-trial DPO/Homeowner must explicitly acknowledge/ratify or mitigate it before real-patient trial reliance.

Erasure severity: `SHA256(DG2)` cannot be crypto-shredded because it is a plaintext hash, not ciphertext under a destructible key. It cannot be row-deleted without bypassing the append-only evidence posture, and it is already part of signed/proof-bound history. It also persists in ordinary backups of the evidence tables; crypto-shred does not neutralize those backup copies because there is no retained key to destroy.

Consumer exposure (v0.9, verified on code): `ArtifactHash` leaves the server to authorized BusinessConsumer read endpoints today. `GET /api/ekyc/verification-sessions/{id}/evidence-ledger` returns `EvidenceLedgerCaptureArtifactDto.ArtifactHash`, and `GET /api/ekyc/evidence-packages/{id}` returns `EvidencePackageSummaryDto.EvidenceRefs[*].ArtifactHash` via `ToPublicEvidenceRef`. The verification-view DTO does not directly list evidence refs. Client-application access controls prevent one consumer from reading another consumer's sessions, but they do **not** prevent cross-consumer correlation if two consumers receive or compare the same globally stable `SHA256(DG2)` value for the same CCCD/person.

DPO disposition menu for the `SHA256(DG2)` RRI (v0.9; named by disposition to avoid collision with Track A/B):
- **(i) ACKNOWLEDGE/RATIFY residual risk.** DPO explicitly accepts that a stable, pseudonymous identifier derived from the CCCD chip portrait is retained in append-only proof history/backups and is currently disclosed to authorized BusinessConsumers.
- **(ii) STOP DISCLOSING it to BusinessConsumers = the B1 API-egress mitigation (recommended first; distinct from Track B raw-vault).** Remove or replace `ArtifactHash` in the two consumer-facing DTO surfaces (`EvidenceLedgerCaptureArtifactDto.ArtifactHash` and `EvidenceRefSummaryDto.ArtifactHash`) with an opaque/non-linkable reference or a consumer-scoped value. Keep the internal proof-bound hash unchanged. This is an API-contract change, NOT a proof-contract change. It reduces third-party exposure + cross-consumer correlation but does NOT erase the internal append-only/backup/proof copy - the RRI stays open.
- **(iii) CHANGE THE PROOF HASH SCHEME.** If DPO rejects even internal retention/linkability, open a separate proof-contract TIP, for example replacing global raw-byte hashes with keyed/domain-separated/session-varying identifiers or another reviewed binding scheme. This affects the proof contract, golden vectors such as TIP-67G fixtures, SignFlow/consumer verifier behavior, and Agent hash computation/keying. Do not change it inside TIP-82R.

B1 (consumer API-egress mitigation - distinct from Track B raw-vault) feasibility (v0.9, verified on code/docs): consumer proof verification does **not** require exposed `ArtifactHash`. TIP-67G `resultHash` preimage has exactly 16 fields and no `ArtifactHash`; `Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson` builds `resultHash` from proof fields ending in `signedManifestHash`; TIP-67B verifier rules say clients verify the JWS, read facts only from the decoded signed claim, recompute `resultHash`, and bind to `resultHash`. Therefore suppressing `ArtifactHash` from ledger/package-summary DTOs can reduce cross-consumer correlation without rewriting signed proof history.

**D-6. Deletion / crypto-shred.** Options: TTL evaluator / disposal proof / key-destruction semantics / backup+object-version handling / legal-hold override.
> **RECOMMEND: crypto-shred is the PRIMARY disposal mechanism for retained encrypted raw artifacts** (per-artifact data key; destroy the key -> ciphertext is unrecoverable). See the dedicated section below. Crypto-shred is recommended because it preserves append-only evidence posture and neutralizes backups/object versions you cannot chase; it is not the only theoretical append-only-compatible disposal mechanism.
> **Disposal proof for retained encrypted artifacts** = an audit event containing the opaque artifact id, the CIPHERTEXT hash, the destroyed key id, destruction timestamp, and actor - signed. It proves destruction WITHOUT ever referencing the plaintext. **Track A memory-only disposal is different:** it produces a disposal attestation, not cryptographic proof, because no ciphertext/key existed.
> **Legal hold** suspends TTL disposal. **Consent withdrawal makes the artifact immediately disposal-eligible.** Where the two collide, see the conflict rule in the lifecycle section - it is escalated to DPO, not silently resolved by code.

Build blocker (v0.3): no raw vault or encrypted export-packet build may start until backup/object-version disposal semantics are decided:

- are raw packets included in backups;
- are object versions enabled;
- can crypto-shred render backup/object-version copies unrecoverable;
- how legal hold interacts with backup retention;
- whether the key store or key-encryption keys are backed up, and how those backups are shredded.

Key hierarchy decision (v0.4):

- each raw artifact must have a unique DEK (data-encryption key);
- the DEK must not be stored in plaintext;
- if the DEK is wrapped by a KEK/KMS key, wrapped-DEK backup/version semantics must be defined;
- crypto-shred must destroy or render unrecoverable all material needed to unwrap the DEK;
- KEK/KMS backup policy must not defeat artifact-level crypto-shred.

**D-7. Recipient/vendor confirmation.** For every approved raw-BIO exchange flow, confirm exact raw classes, exact API fields, mandatory vs optional material, post-acceptance retention obligations, withdrawal/erasure behavior, and whether any documentation is inconsistent. EIDCA signing is the current example: confirm whether it requires raw DG2/chip or selfie Base64 alone, whether `chip_raw_data.sod/dg2` is mandatory / optional / doc drift, and post-acceptance retention obligations on the recipient/TSP side.
> **RECOMMEND: name an OWNER + a fail-closed default per recipient/flow.** Owner = Homeowner (or a named integration lead). **Default until WRITTEN confirmation: send/export the least raw set already confirmed; for EIDCA signing, selfie Base64 only and no DG2/chip raw.** If the recipient later mandates DG2 or another raw class, that reopens D-1 - it does not silently expand scope.
> *Rationale:* "confirmed with vendor" with no owner and no default is how a decision TIP stalls forever. Fail-closed toward LESS raw data costs a rejected submission; fail-open costs a biometric disclosure.

**D-8. Track A export topology.** Options per flow: Agent-to-approved-recipient direct / Agent-to-approved adapter / Agent-to-TagEkyc-server encrypted packet handoff.
> **RECOMMEND:** choose exactly one topology per flow before any build dispatch. Default recommendation is **Agent-to-approved adapter** or **Agent-to-approved-recipient direct** if the path can be made auditable, because it preserves TagEkyc server's current no-raw-server invariant. **Agent-to-TagEkyc-server encrypted packet handoff is DISFAVORED**; choosing it requires a separate explicit decision to open a new server-side raw-packet surface, even if the server never sees plaintext.
> No build TIP may start until D-8 is selected for that flow, including where an `encrypted_export_packet` physically lives: Agent local encrypted packet, TagEkyc server object store/queue, or SignFlow/TSP adapter queue.

Recipient withdrawal/erasure obligation (v0.4): for any Track A export, the recipient contract/API must define withdrawal/erasure notification behavior. If a subject withdraws after export, the system records withdrawal and, if policy requires, sends an erasure/stop-processing notice to the recipient and audits notice id/result. If the recipient cannot honor erasure, the consent text must disclose this limitation before export.

**D-9. Raw policy record versioning.** A raw export/retention policy id must point to an immutable versioned record, not a mutable config name.
> **RECOMMEND:** every raw export/access/disposal audit references `policyId + policyVersion`. A policy record includes: mode, **controller**, allowed raw classes, recipient, purpose, TTL, disposal trigger, consent text version, Legal/DPO approval reference, export topology, and **recipient/infrastructure jurisdiction**. Changing any field creates a new version; no mutation-in-place.

**D-10. Cross-border transfer gate (v1.0 - previously missing entirely).** Options: recipient + processing infrastructure entirely inside Vietnam / recipient or infrastructure (incl. cloud region, sub-processors, support access) outside Vietnam.
> **RECOMMEND: confirm jurisdiction BEFORE any export, and fail-closed if unknown.** Exporting raw BIO to a recipient (or to infrastructure) outside Vietnam is a **cross-border transfer of personal data** - a SEPARATE regulatory obligation (transfer-impact assessment / filing), NOT covered by a domestic DPIA. Pin:
> - the policy record (D-9) must carry the recipient's and the processing infrastructure's **jurisdiction**, including cloud region and any sub-processor/support access path;
> - if either is outside Vietnam, **Legal/DPO must ratify a cross-border personal-data transfer assessment before the first export**; the export path stays fail-closed until then;
> - "the vendor is a Vietnamese company" is NOT sufficient - confirm where the data is actually processed and who can access it;
> - Legal identifies the exact instrument, filing, and articles in force at ratification (this TIP does not self-cite).
> *Rationale:* raw biometric leaving the jurisdiction is the single obligation most likely to be missed, because it hides behind a domestic-looking vendor relationship.

## Disposal mechanism: crypto-shred is the recommended answer (v0.4)

This connects TIP-82R to an architecture constraint the first draft missed.

**The constraint.** The server's six evidence/audit tables (`capture_artifacts`, `evidence_results`, `verification_decisions`, `evidence_packages`, `evidence_manifests`, `audit_events`) are protected by a DB trigger (`tr_*_append_only` / `tagekyc.deny_append_only_mutation()`, installed in the initial migration). While installed/enabled, the trigger blocks `UPDATE`/`DELETE` attempts by any role including owner DML; but an owner/superuser can still `ALTER`/`DROP`/disable it - hence least-privilege runtime role remains required. Append-only is a FEATURE: it protects proof immutability. A row-level `DELETE`-based purge would need to drop/disable/bypass the trigger - i.e. it would destroy the evidentiary-integrity guarantee that made those rows trustworthy.

**Why this matters here.** Any raw-BIO disposal designed as "DELETE the row when the TTL expires" collides head-on with that posture. It is the same open architecture question raised by the retention slice (TIP-83E-3, OQ-1 append-only-vs-purge).

**The recommended resolution for raw BIO: crypto-shred.**
- Each raw artifact is encrypted under its OWN data key.
- Disposal = **destroy the key**, not the row. The ciphertext may remain (in the table, in a backup, in an object-store version) and is permanently unrecoverable.
- This is **append-only-compatible** (no UPDATE/DELETE needed), **backup-compatible** (you cannot chase every backup copy; destroying the key neutralizes all of them at once), and **audit-compatible** (the disposal event is itself an append-only audit row).
- Disposal proof (D-6) = an audit event with opaque artifact id + ciphertext hash + destroyed key id + timestamp + actor. It proves destruction without referencing plaintext.

Crypto-shred is not the only mechanism that can be compatible with append-only table design. For example, partition-drop/detach is DDL and row-level `BEFORE UPDATE OR DELETE` triggers do not fire on it. But partition-drop does not neutralize backups/object versions you cannot reach. Crypto-shred is therefore the **recommended** mechanism, and the only one in this brief that addresses unreachable backups/object versions without weakening evidence-table append-only guarantees.

**Consequences to pin:**
- The raw vault (if Track B is ever approved) MUST live **outside** the six trigger-protected evidence tables - it is not "evidence", it is regulated raw material with a TTL. Do not smuggle it into an evidence table.
- Key management (per-artifact keys, key destruction semantics, backup of the key-store) becomes the load-bearing security surface. A key that is backed up somewhere unshredded defeats the whole mechanism - **the key-store must NOT be included in ordinary backups**, or its backups must be shreddable too.
- A row-DELETE purge remains OUT of scope for TagEkyc evidence tables regardless of what TIP-82R decides.
- STOP if crypto-shred destroys only an application row but leaves recoverable wrapped keys in ordinary backups.

## Proposed Raw BIO Lifecycle

- `captured`: raw BIO exists transiently after NFC/camera/signature capture. In default `verify_and_discard`, this state transitions directly to disposal after verification/export is not authorized.
- `retained_pending_external_acceptance`: raw BIO is encrypted and retained only because an approved policy requires submission to an external party, retry handling, or acceptance confirmation.
- `export_submitted_to_external_party`: raw BIO or an encrypted export packet has been submitted to the approved external party/adapter. Audit records must include opaque session id, policy id, export id/hash, recipient, timestamp, actor/service identity, and result. No raw Base64 in audit.
- `eligible_for_disposal`: external acceptance, CTS issuance/signing completion, timeout, cancellation, or policy TTL makes the raw BIO disposable unless legal hold applies.
- `disposed`: plaintext/raw buffers were cleared/disposed without retained ciphertext. This is the Track A memory-only terminal state; evidence is a signed/sanitized disposal attestation, not cryptographic proof.
- `crypto_shredded`: retained ciphertext may remain, but the DEK/wrapped-key material needed to recover plaintext has been destroyed or rendered unrecoverable. Evidence is cryptographic disposal proof using ciphertext hash + destroyed key id.
- `legal_hold`: disposal is suspended by Legal/DPO-approved hold. Access remains restricted and audited. Release from hold moves the artifact back to `eligible_for_disposal`.
- `withdrawn`: consent withdrawal has been received after raw material existed in a Track A/B policy path. This is a lifecycle state/transition introduced by TIP-82R, not something the current Agent implements.
- `disposal_blocked_by_legal_hold`: withdrawal or TTL made the artifact disposal-eligible, but Legal/DPO hold prevents disposal. The system records both legal bases and escalates.

Withdrawal transitions (v0.4):

- `captured` + withdrawal before export -> `eligible_for_disposal` -> `disposed`; no export.
- `retained_pending_external_acceptance` + withdrawal -> `eligible_for_disposal` immediately; no TTL wait.
- `export_submitted_to_external_party` + withdrawal -> local retained material goes to `eligible_for_disposal`, but the export cannot be un-sent; audit records export-before-withdrawal and recipient notice behavior from D-8.
- `legal_hold` + withdrawal -> `disposal_blocked_by_legal_hold`, DPO escalation.
- `disposal_blocked_by_legal_hold` + hold release -> `eligible_for_disposal`.
- `disposed` or `crypto_shredded` + withdrawal -> audit the withdrawal only; no raw material remains locally.

The shipped consent chain currently handles withdrawal BEFORE and DURING capture (pre-capture check + between-append checks + `CONSENT_WITHDRAWN_AFTER_PARTIAL_SUBMIT`). It has NO rule for withdrawal AFTER raw material has been retained or exported; that is a TIP-82R decision/build requirement.

Required rule: after the approved workflow reaches its success/acceptance point, if the customer policy does not require long-term retention, raw BIO must move immediately to `disposed` for memory-only Track A or `crypto_shredded` for retained encrypted packets, with audit/attestation appropriate to the mode. For a TSP flow, this means after signing/CTS issuance succeeds.

**Conflict rule (v0.4): legal hold vs right-to-erasure.** A subject's consent withdrawal and a Legal/DPO legal hold can point in opposite directions on the same artifact. Legal/DPO must confirm the rule under the applicable VN personal-data regime (Legal identifies instrument/articles at ratification). Pin: (a) the system does NOT auto-resolve it - the artifact enters `disposal_blocked_by_legal_hold`; (b) the conflict is escalated to the DPO with both legal bases recorded; (c) the artifact remains encrypted, access-restricted, and audited while blocked; (d) the subject is informed that disposal is suspended and why, per the consent text. The applicable raw-retention/export consent text must therefore SAY that a legal hold can suspend erasure - or the consent is misleading.

## Integration Impact

CaptureAgent:

- Default stays `verify_and_discard`.
- `raw_vault` or any raw-retention mode remains fail-closed until TIP-82R decisions are approved and a later build TIP exists.
- CaptureAgent is the first raw-BIO boundary: DG2, selfie, liveness media, SOD/DG groups, and optional hand-signature image originate here. Any approved raw-retention/export mode must therefore change Agent behavior deliberately, not only server behavior.
- Future build may need an approved export packet path, but not generic local raw persistence.
- Agent must not silently extend raw buffer lifetime. If a policy requires export, the Agent may hold raw BIO only for the minimum in-memory window needed to build an approved encrypted packet or submit through an approved controlled channel.
- No Agent-local raw cache by default. Any local pending-export cache is a separate explicit decision requiring encryption, TTL, crash-cleanup, audit, and preflight visibility.
- Agent config/preflight must stay fail-closed: unsupported raw-retention modes reject before capture, as TIP-82 does today.
- Host/WPF UI may display policy state and operator-safe status only; it must not render, save, preview, log, or expose raw Base64 export material outside the already necessary camera/NFC capture UX.
- Agent run logs and submit-only receipts remain sanitized: policy id, export status, packet hash/id, and disposal status only; no raw BIO, no Base64, no file path to raw material.
- If policy chooses `external_export_only_no_retain`, D-8 must specify whether the export is Agent-to-recipient, Agent-to-approved-adapter, or Agent-to-TagEkyc-server packet handoff. Each option has different trust, retry, and audit consequences and must be build-scoped separately.
- After export acceptance or workflow success, Agent-owned raw buffers must be disposed immediately unless an approved local pending-export cache exists and remains under a valid retention/legal-hold state.

TagEkyc server:

- Proof/hash-only evidence surfaces remain unchanged.
- Any future raw vault must be separate from normal evidence DB tables and normal receipts.
- Normal business APIs must expose only hashes, vault refs, policy state, and export status, never raw BIO.
- For future raw-export/retention features, BusinessConsumer may receive only export status, packet hash/id, recipient id, policy state, and disposal status. It must never receive raw Base64, raw packet bytes, vault plaintext, or raw file paths.
- Existing business read APIs already expose proof-chain `ArtifactHash` values in the evidence ledger and evidence package summary. That is not raw BIO or a raw-export endpoint, but `NfcArtifactHash = SHA256(DG2)` is a consumer-visible stable per-card identifier that can enable cross-consumer correlation if shared/compared. The owning RRI is tracked in the git-tracked Phase 1 debt register outside TIP-82R.

External recipients / adapters:

- SignFlow/EIDCA is an approved consumer only if policy allows; it is an example, not the policy scope.
- Any recipient/adapter that receives raw BIO must be bound to a policy id/version, purpose, raw class set, consent text, retention/erasure obligation, and export topology.
- TSP enrollment/renewal may require raw chip data plus selfie export.
- Signing definitely needs selfie Base64 per EIDCA 1.0.8, while DG2/raw chip requirement is unresolved pending vendor confirmation.

## Verify-on-code Notes (v0.4)

These are code-checked facts used by this decision TIP; they are not new build authorization.

- Append-only trigger: `20260621075836_InitialPostgresPersistence.cs` creates `tagekyc.deny_append_only_mutation()` and `BEFORE UPDATE OR DELETE` triggers on exactly six tables: `capture_artifacts`, `evidence_results`, `verification_decisions`, `evidence_packages`, `evidence_manifests`, and `audit_events`. The hospital Postgres runbook separately requires a non-owner runtime role with no `UPDATE`, `DELETE`, `ALTER`, `DROP`, or trigger-disable capability. Therefore the correct premise is "installed/enabled trigger blocks row DML; owner/superuser can still alter/drop/disable, so least privilege is required."
- Current Agent retention gate: `TAGEKYC_BIOMETRIC_RETENTION_MODE` unset or `verify_and_discard` maps to `VerifyAndDiscard`; `raw_vault` maps to `RawVault` but fails before capture with `RETENTION_POLICY_UNSUPPORTED`; unknown values fail with `RETENTION_POLICY_INVALID`.
- Current Agent withdrawal chain: TIP-82/TIP-82W covers pre-capture, between-append, and after-partial-submit withdrawal (`CONSENT_WITHDRAWN_AFTER_PARTIAL_SUBMIT`). There is no current raw-retained/post-export lifecycle, so withdrawal-after-retention is a new TIP-82R policy/build requirement.
- Current `ArtifactHash` reality: CaptureAgent computes `ArtifactHash` with `CaptureAgentHash.Sha256(bytes)` over plaintext NFC artifact bytes, selfie bytes, and liveness bytes (`Hn212CccdReader`, `Hn212FaceCaptureFrameStore`, `NoTempFileFaceCamera`). `ReflectionHn212SdkBridge` currently sets `NfcArtifactBytes = dg2.ToArray()`, so the NFC artifact hash is `SHA256(DG2)` today, not a session-varying envelope hash. The server validates the hash string shape and proof-binds it in normalized FaceMatch/Liveness/NFC decision basis. Therefore existing `NfcArtifactHash` is a stable per-card biometric-derived identifier and a DPO/Homeowner RRI, while selfie/liveness artifact hashes are capture-instance identifiers with lower cross-session linkability.
- Current BusinessConsumer exposure: `ApplicationAuthorization.RequireBusinessSessionRead/RequireBusinessReadEndpoint` permits `BusinessConsumer` callers with `business.session.read`; `VerificationSessionApplicationService.GetEvidenceLedgerAsync` maps every accepted capture artifact to `EvidenceLedgerCaptureArtifactDto(..., artifact.ArtifactHash?.ToString(), ...)`; `VerificationCompletionApplicationService.ToPackageSummary` maps manifest refs to `EvidenceRefSummaryDto` through `ToPublicEvidenceRef`, preserving `evidenceRef.ArtifactHash`. Therefore `ArtifactHash`, including `SHA256(DG2)` for NFC artifacts, is returned to the authorized BusinessConsumer that owns the session/package. `EvidencePackageVerificationViewDto` itself does not include evidence refs, but this does not remove ledger/package-summary exposure.
- Current consumer-verification dependency: no verified consumer proof path requires public `ArtifactHash`. TIP-67G's frozen `resultHash` preimage excludes `ArtifactHash`; TIP-67B's verifier contract uses only the verification view + decoded signed claim + pinned trust anchor, and `Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson` computes `resultHash` from the neutral proof fields ending at `signedManifestHash`. This supports the **B1 consumer API-egress mitigation** (distinct from Track B raw-vault): suppress `ArtifactHash` from consumer-facing ledger/package-summary DTOs without changing signed proof semantics.

## Definition of Done for Decision TIP

- [ ] Homeowner chooses whether this stays `TIP-82R` and approves the neutral title.
- [ ] **Homeowner/Legal/DPO RATIFY or OVERRIDE each of D-1 .. D-10** (each carries a Contractor recommendation - the deliverable is a decision, not a re-design).
- [ ] **Track A (export, transient) approved or rejected INDEPENDENTLY of Track B (retention at rest).** Approving A does not approve B.
- [ ] Legal/DPO approve or reject raw BIO retention/export as an optional capability.
- [ ] Allowed raw BIO classes enumerated (D-1); **mode x controller PAIR ratified per flow** (D-2; controller is an orthogonal axis - assigning it to SignFlow/external TSP REASSIGNS the data controller); retention triggers/durations defined (D-3).
- [ ] **D-10 cross-border gate closed:** recipient AND processing-infrastructure jurisdiction confirmed (incl. cloud region + sub-processor/support access). If either is outside Vietnam, a cross-border personal-data transfer assessment is ratified by Legal/DPO BEFORE the first export; export stays fail-closed until then.
- [ ] **Framing invariants hold:** default remains `verify_and_discard`; `raw_vault` remains code-enforced fail-closed today; raw retention/export remains optional + policy-gated + Legal/DPO/Homeowner-approved (**not prohibited - unbuilt and ungranted**); this TIP authorizes NO runtime build, NO B1/B2 implementation, NO proof-contract change.
- [ ] **B1 (consumer API-egress mitigation) remains a SEPARATE slice** and is NOT a substitute for any decision here.
- [ ] **RRI `fa98531` (`SHA256(DG2)`) remains OPEN after this patch** - cross-referenced as a governance lesson, owned by the debt register, disposed of at the hospital-trial DPO gate.
- [ ] Legal confirms the **exact applicable instrument and articles in force at ratification**; this TIP self-cites none.
- [ ] Consent scopes named AND mapped to user-facing consent text, AND wired into the existing `TAGEKYC_CONSENT_*` scope contract (D-4) - including purpose, recipient, raw classes, retention mode, TTL/disposal trigger, and that **withdrawal after retention forces immediate crypto-shred**.
- [ ] Access/export audit requirements defined (D-5), including cross-reference to the **hospital-trial DPO/Homeowner disposition of the existing proof-bound, backup-persistent, consumer-visible `SHA256(DG2)` RRI**: acknowledge/ratify, stop disclosing to BusinessConsumers, or open a proof-contract mitigation TIP. No new plaintext-biometric hashes in raw-export audit unless approved; Track A memory-disposal attestation vs Track B crypto-shred proof distinguished; DEK/KEK/wrapped-key destruction + key-store-backup/object-version semantics, and legal-hold rules defined (D-6).
- [ ] Track A terminal/retry policy accepted: pure export-only has no durable retry; durable retry requires `encrypted_export_packet`.
- [ ] D-8 export topology selected per flow, defaulting to Agent-to-approved-adapter/direct if feasible; any Agent-to-TagEkyc-server handoff explicitly accepts a new server-side raw-packet surface.
- [ ] D-9 immutable policy record shape accepted; every export/audit references `policyId + policyVersion`.
- [ ] Backup/object-version disposal semantics are accepted before any raw vault or encrypted export-packet build dispatch.
- [ ] **The legal-hold vs right-to-erasure conflict rule is accepted, and the applicable raw-retention/export consent text says a legal hold can suspend erasure** (otherwise the consent is misleading).
- [ ] Recipient/vendor confirmation has a NAMED OWNER and a fail-closed default per raw-BIO exchange flow; for EIDCA signing, selfie-only/no-DG2 stands until written confirmation (D-7).
- [ ] For any vendor-document-backed flow, structured source trace is recorded: controlled copy/doc-store location, SHA-256, source file/version/date, sections/schema names, observed fields, confidence, reviewer, plus vendor confirmation result/date when obtained. EIDCA 1.0.8 is the current example.
- [ ] Recipient withdrawal/erasure behavior accepted: recipient contract/API supports erasure/stop-processing notice with audit, OR consent text discloses that exported data cannot be recalled/erased by TagEkyc.
- [ ] If the hospital-trial DPO rejects current `SHA256(DG2)` consumer exposure but accepts internal proof retention, open an API-contract mitigation to stop disclosing `ArtifactHash` through BusinessConsumer ledger/package-summary DTOs. If DPO rejects even internal proof-chain linkability, open a separate proof-contract mitigation TIP before further real-patient reliance on NFC/FaceMatch proof hashes; expected blast-radius includes TIP-67G/golden vectors, SignFlow/consumer verification, and Agent hash computation/keying.
- [ ] Follow-on build TIPs are split explicitly and each names whether it is Track A or Track B. Working labels only - real build TIPs should use the then-current TIP numbers:
  - Vendor/recipient confirmation + contract mapping packet: confirmed per-flow raw classes, exact API fields, DG2/chip signing answer where relevant, post-acceptance retention obligations, recipient erasure behavior, accepted export path.
  - Policy config + consent-scope/purpose/recipient enforcement only.
  - External export packet builder, no retain.
  - Encrypted pending-export cache + retry + disposal.
  - Encrypted raw vault + legal hold + access workflow.
  - External recipient / adapter integration, including TSP/EIDCA only if that flow is approved.
  - Disposal worker + crypto-shred proof.
- [ ] `verify_and_discard` remains the default and current production gate behavior remains fail-closed.

## STOP/RRI

STOP and ask for review if any draft/build:

- Enables raw retention by default.
- Allows BusinessConsumer to download raw BIO freely.
- Stores raw BIO in ordinary DB tables, logs, receipts, run logs, proof payloads, or generic evidence records.
- Claims TSP/EIDCA integration is TagEkyc core or the scope boundary of this raw-BIO policy.
- Breaks proof/hash-only business surfaces.
- Treats EIDCA as proof that all flows require raw DG2 for signing before vendor confirmation.
- Treats EIDCA as the reason/scope for all raw-BIO retention instead of merely one example that raw-BIO exchange may be requested.
- Implements runtime vault/storage before Legal/DPO decisions are closed.
- Keeps raw BIO after CTS/signing success when the approved customer policy says dispose immediately.
- **(v0.2/v0.5)** Designs retained-raw disposal as a row-level `DELETE`/`UPDATE` against the six trigger-protected evidence tables, or treats row purge as equivalent to backup-neutral disposal. Crypto-shred is the recommended retained-ciphertext mechanism; partition/drop-style alternatives need separate approval and still must address backups/object versions.
- **(v0.2)** Places a raw vault INSIDE the evidence tables, or backs up the per-artifact key-store in an unshreddable backup (defeats crypto-shred entirely).
- **(v0.2/v1.0)** Treats the controller assignment (SignFlow / external TSP) as a storage-location choice rather than a data-controller reassignment, or ships consent text that promises erasure without disclosing that a legal hold can suspend it.
- **(v1.0)** Reads "policy-gated" as "available on request", or presents the capability as existing at runtime. It does not exist; `raw_vault` is code-enforced fail-closed. The gate costs a decision, not a config flag.
- **(v1.0)** Collapses `encrypted_export_packet` into `external_export_only_no_retain` (a persisted, TTL'd, crypto-shreddable packet is NOT memory-only export).
- **(v1.0)** Exports raw BIO to a recipient or infrastructure outside Vietnam without a ratified cross-border transfer assessment (D-10), or accepts "the vendor is a Vietnamese company" as proof of domestic processing.
- **(v1.0)** Cites a specific legal instrument/article inside this TIP as if it were legal advice; Legal confirms the instrument in force at ratification.
- **(v0.2)** Approves Track B (retention at rest) as a side-effect of approving Track A (transient export), or sends DG2/chip raw before written vendor confirmation.
- **(v0.4)** Implements durable retry under `external_export_only_no_retain`; durable retry is `encrypted_export_packet`.
- **(v0.4)** Starts a build before D-8 export topology or D-9 policy-versioning is selected.
- **(v0.4)** Claims crypto-shred while leaving recoverable DEKs/wrapped-DEKs/KEK material in ordinary backups or object versions.
- **(v0.4/v0.5)** Puts new plaintext-biometric hashes such as `SHA256(raw selfie)` or `SHA256(raw DG2)` in raw-export/access audit without explicit Legal/DPO approval, or misreads the existing proof-bound `ArtifactHash` exception as permission to create new raw-BIO audit identifiers.
- **(v0.4/v1.0)** Treats this TIP's technical notes about the personal-data regime as legal advice instead of Legal/DPO-confirmed requirements.
- **(v0.5)** Chooses Agent-to-TagEkyc-server packet handoff without an explicit decision to open a new server-side raw-packet surface.
- **(v0.5)** Claims Track A memory-only disposal has cryptographic proof equivalent to crypto-shred; it has disposal attestation only.
- **(v0.6)** Treats current `NfcArtifactHash = SHA256(DG2)` as harmless or already-approved without explicit DPO/Homeowner acknowledgement, or changes the proof-hash contract inside TIP-82R.
- **(v0.6)** Sends or exports `face_matching.chip_image` while claiming DG2/chip raw remains blocked.
- **(v0.7)** Buries the current `NfcArtifactHash = SHA256(DG2)` RRI inside this parkable raw-BIO policy TIP instead of routing it to the hospital-trial DPO/debt gate.
- **(v0.7)** Claims `ArtifactHash` is internal-only; today it is returned to authorized BusinessConsumers through evidence-ledger and evidence-package summary APIs.
- **(v0.9)** Frames DPO choice as only accept-risk vs proof-contract rewrite while skipping the cheaper API mitigation: stop disclosing `ArtifactHash` to BusinessConsumers.
