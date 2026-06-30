# FaceMatch Calibration — Phase-1 Consent & Retention Policy v0.1

**Version:** 0.4
**Status:** **ACCEPTED (GPT + Codex) — HARD-STOP READY FOR LEGAL/DPO REVIEW.** No raw-biometric collection until legal/DPO sign-off + the §7 checklist is completed. The §3 consent wording is aligned with the §5 withdrawal-window/final-freeze semantics. Rounds 1–3 of GPT + Codex patches APPLIED (r3: consent text now explains the withdrawal token [keep-it / lost-token consequence] + the windowed-withdrawal/aggregate-recompute wording; consent records kept out of the eval folder + minimized; checklist withdrawal-token issuance/lost-token item). Both reviewers: HARD-STOP-ready after these, legal/DPO sign-off still mandatory before any real-biometric collection. Rounds 1–2 prior: (r2: withdrawal-before-freeze aggregate regeneration semantics, withdrawal-token min-properties [high-entropy/non-citizen-derived/store-hash-only] + lost-token fallback, stale encryption open-item fixed). Both reviewers: clean HARD-STOP gate for legal/DPO review after r2. Round-1 prior: — withdrawal token (no retained identity-mapping), capture/SDK leak control + dry-run gate, derived-artifact (scores/pairs/temp) deletion, encryption-at-rest mandatory, crypto-shred deletion (SSD/backup-realistic), consent-record-keeping promoted to a §7 gate. No Phase-1 biometric is collected until this policy is completed + **legal/DPO sign-off** is obtained.
**Gates:** `docs/facematch_calibration_engine_adequacy_study_plan_v0_1.md` §3 (Phase-1 HARD-STOP) + §5.
**⚠️ Not legal advice.** This is an OPERATIONAL governance policy drafted by the Contractor. The data controller (Homeowner/org) MUST obtain legal/DPO review of legal sufficiency before any real-biometric collection — VN personal-data obligations are the controller's responsibility.

## 0. Scope & purpose

A SMALL, one-time, consented collection of **real CCCD chip-DG2 face images + live selfies** from volunteers, used SOLELY to measure the FaceMatch engine's genuine/impostor score distributions (ROC/EER/FAR-FRR) on the real DG2-vs-selfie operating condition (Phase 1 of the calibration study). It is a **transient research/eval collection** — NOT the production eKYC system (which is no-raw / hash-only), NOT a retained dataset, NOT model-training data.

## 1. Legal basis (controller to confirm with legal/DPO)

- **Biometric data + government-ID data = SENSITIVE personal data** under the VN personal-data-protection regime — at minimum **Nghị định 13/2023/NĐ-CP (PDP Decree, eff. 2023-07-01)** which requires **explicit, informed, specific consent**, purpose limitation, retention limitation, and data-subject rights. ⚠️ **Verify the CURRENT instrument** — a VN Personal Data Protection LAW may now supersede/supplement the 2023 Decree; legal review must confirm the obligations in force at collection time.
- **Controller:** the Homeowner/org running the collection is the data controller and bears the legal obligations. The Contractor (remote) never receives raw biometric (only aggregate stats).
- **Lawful basis = consent** (explicit, written, withdrawable) for a defined research/eval purpose; no secondary use.

## 2. Data minimization — collect ONLY what the ROC needs

- **COLLECT:** (a) the **DG2 FACE IMAGE** read from the chip; (b) one or more **live selfies**; (c) an **opaque participant id** (random, salted, not derived from the citizen-ID number); (d) OPTIONAL coarse demographic bin (age-band / sex) ONLY if consented, for same-demographic hard-negative grouping.
- **DO NOT collect/store for this study:** the citizen-ID NUMBER, name, address, DOB, or any other chip data-group fields; the full SOD/chip dump; any document image beyond the DG2 face. (The ROC needs faces, not identity fields.) If the NFC read incidentally exposes those, they are NOT written to the eval input — only the DG2 face image + selfie are saved.
- **No linkage (with a withdrawal path — round-1 P1):** the opaque id must NOT be reversible to the citizen-ID number. Withdrawal is supported via a **withdrawal token** the participant keeps — a **high-entropy random token, NOT derived from any citizen value**; the system stores only the token's **hash/HMAC → opaque-id** (so the token store itself is not a re-identification weakness) — so NO opaque-id↔real-identity mapping table is retained. **Lost-token fallback:** withdrawal then proceeds via the access-restricted consent record (§3, which is itself minimized + deleted per §5/§7); if no such record is kept, a lost token means the participant's data is removable only via the window-end global deletion. If a mapping is genuinely unavoidable, it is a SEPARATE encrypted, access-restricted store deleted with the data.
- **Capture-flow leak control (round-1 P1):** the chip-read SDK/device (e.g. HN212) can incidentally expose CCCD fields / access codes / SOD+DG dumps / reader debug logs / camera temp files / EXIF / thumbnails / crash dumps. ONLY the **DG2 face image + selfie** are written to the eval input; everything else is NOT persisted, and device/SDK logging/temp/sync is disabled or redacted — proven by the §7 dry-run.

## 3. Consent (informed, voluntary, specific, withdrawable)

Each volunteer signs/records a consent that states, in plain Vietnamese:
- WHAT is collected (a photo of their face from the CCCD chip + a selfie), and explicitly that ID number/name/address are NOT collected/retained for this study;
- WHY (to test how well a face-matching algorithm tells people apart — a one-time technical evaluation);
- WHERE it is stored (a single local machine, encrypted, not uploaded, not shared);
- HOW LONG (until the eval run + verification completes, then securely deleted — see §5);
- THAT they may WITHDRAW during the retention/eval window → their raw images + local derived per-pair rows are deleted, and the aggregate is recomputed without them if not yet finalized (after the final aggregate-freeze + deletion, only non-identifying aggregate remains — §5);
- THAT they RECEIVE a **withdrawal token** they must KEEP until the window ends — it identifies their local study data WITHOUT the study retaining their name/CCCD; **if the token is lost**, deletion may require the controller's documented fallback (the access-restricted consent record) or may be impossible without re-identifying data the study intentionally does not retain;
- THAT only aggregate statistics (no images, no identity) leave the machine;
- WHO to contact to withdraw / ask questions.
Consent is recorded (signed form or recorded acknowledgement) + retained with the same protection + deleted with the data. **Identifiable consent records are NOT stored in the eval dataset folder; any minimized consent/deletion attestation that survives MUST NOT contain CCCD number / name / address / face image unless legal/DPO explicitly requires it.**

## 4. Storage & access

- **Local-only:** images stored ONLY in the gitignored local input dir on a SINGLE controlled machine (e.g. `local_datasets/phase1-dg2/<opaque-id>/`). NEVER committed (gitignore enforced), NEVER uploaded, NEVER synced (no Drive/GitHub), NEVER sent to the Contractor.
- **Encryption at rest is MANDATORY (round-1 P2):** the input dir lives inside an encrypted container / OS disk encryption — or a NAMED, signed exception is recorded before any collection. No real CCCD biometric on unencrypted storage.
- **Access list:** a NAMED, minimal set of people who may access the folder (record who); no shared/again-used credentials.
- **No raw biometric in logs / outputs / crash dumps** — reuse the no-raw discipline; the harness writes only opaque-keyed scores (local, gitignored) + aggregate stats.

## 5. Retention & deletion (delete-after-eval)

- **Retention window:** the MINIMUM needed — the eval run + result verification, capped at a defined short window (e.g. ≤ 14 days; controller sets the exact value). No indefinite retention; the images are NOT kept as a dataset.
- **Deletion via crypto-shredding (SSD/backup-realistic — round-1 P2):** the input images + any opaque-keyed derived artifacts live inside an **encrypted container**; deletion = **destroy the container + its key/material (crypto-shred)**, NOT just file-overwrite (unreliable on SSD / copy-on-write / backups). **Backup/sync is disabled** for the folder. Deletion MUST also cover the local `*.scores.csv` / `*.pairs.csv` / temp / cache / failed-run partial outputs, EXIF/thumbnails, device/SDK temp, AND any backups.
- **Withdrawal window + aggregate semantics (round-2 P1):** withdrawal is accepted at any time **up to the final-aggregate freeze**. On withdrawal BEFORE freeze, that participant's images + derived rows (`*.scores.csv`/`*.pairs.csv`) are deleted AND any aggregate already generated from them is **invalidated and regenerated without them** — so withdrawal does not just delete raw files while leaving their contribution in the shared metric. After the final freeze + data deletion, only the non-identifying aggregate remains (subject to the legal/DPO-approved consent wording). The participant presents their **withdrawal token** (§2; lost-token fallback there) → deletion is immediate, no questions.
- **Deletion attestation:** document (date / what / by whom) explicitly covering the container, the key, the derived CSVs, temp/cache, and backups.
- **What survives deletion:** ONLY the aggregate statistics report (ROC/EER/FAR-FRR, no images, no identity, no per-pair CSV) + the deletion attestation.

## 6. Use limitation

The collected biometric is used ONLY to compute the calibration ROC. It is NOT: used in/added to the production eKYC system, used to train/fine-tune any model, retained as a reusable dataset, shared with any third party, or used for any purpose beyond this one-time evaluation.

## 7. Preconditions checklist — ALL must be signed off before any Phase-1 collection (the HARD-STOP)

- [ ] Legal/DPO review of this policy + the current VN PDP obligations completed.
- [ ] Consent text finalized (plain Vietnamese) + a recording/signing method ready.
- [ ] Local storage path defined + gitignored + encrypted-at-rest; access list named.
- [ ] Retention window + secure-delete procedure + deletion-attestation template defined.
- [ ] Data-minimization confirmed in the capture flow (only DG2 face + selfie saved; NO id-number/name/address/other DG fields written).
- [ ] Opaque-id scheme confirmed non-reversible to the citizen-ID number.
- [ ] Withdrawal contact + procedure defined.
- [ ] The harness Phase-1 path confirmed to read from the local dir + emit aggregate-only output (no raw leaves the machine).
- [ ] **Withdrawal-token issuance / storage (hash/HMAC only) / lost-token fallback / participant instructions finalized** — participant can withdraw without an opaque-id↔identity table being retained (§2/§5).
- [ ] **No-log / no-temp / no-sync DRY-RUN evidence (round-1 P1):** a capture dry-run proves the flow writes ONLY {DG2 image, selfie, opaque id, optional consented bins} and that device/SDK logs, camera temp, EXIF/thumbnails, crash dumps, and folder sync are disabled/redacted.
- [ ] **Derived-artifact deletion covered:** the deletion procedure explicitly deletes local `*.scores.csv` / `*.pairs.csv` / temp / cache / partial outputs (not just the images).
- [ ] **Encryption-at-rest mandatory** (or a named signed exception); **backup/sync disabled** for the folder (or deletion explicitly covers backups). Deletion = crypto-shred (destroy container + key), not file-overwrite.
- [ ] **Consent-record retention/deletion design finalized (round-1 P2):** the consent records are themselves minimized, protected, and deleted appropriately.

## 8. Open items for review

- The exact retention window (days) + whether any **signed encryption EXCEPTION** is acceptable (encryption-at-rest is otherwise MANDATORY per §4/§7) — controller/legal decision.
- Whether a coarse demographic bin is collected at all (needed for same-demographic hard-negative subgroup FAR; but adds sensitivity) — minimize unless the subgroup analysis is required.
- Minimum N of consented volunteers for a directional Phase-1 signal vs a confident one (ties to the study plan's N/CI caveat) — set expectations; small N = exploratory only.
- *(Consent record-keeping minimization/protection/deletion — PROMOTED to a §7 signed-off precondition, round-1.)*
