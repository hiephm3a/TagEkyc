# Hospital BHXH eKYC Trial — Consent + DPIA Draft (for Legal/DPO)

> **STATUS: DRAFT STARTING POINT — NOT LEGAL ADVICE.** Written by the engineering side to give your Legal/DPO team a concrete base. It must be reviewed and adapted by qualified counsel against **NĐ 13/2023/NĐ-CP** (bảo vệ dữ liệu cá nhân), **Luật Giao dịch điện tử 20/2023 + NĐ 23/2025**, and hospital/BHXH regulations before any real use. The DPO sign-off — not this document, not engineering — is the gate.

## 0. Deployment shape (frames everything below)
The eKYC does **not** introduce a new autonomous identity decision. It **replaces the now-unlawful "photocopy + hold CCCD" step** with a **consented verify-and-discard** identity check, and **strengthens** the existing naked-eye check that already gates BHXH discharge. Human review remains: staff visual confirmation + cross-system (liên thông) for high-risk cases. During the trial, eKYC output is **assistive evidence for the existing human confirmation**, not a replacement for human judgment.

**Core privacy property (verify-and-discard):** the system verifies (NFC chip + face match + liveness) and **retains only a signed identity-assertion proof + evidence hashes — NOT raw facial images, NOT CCCD chip images/photocopies.** Raw biometric is transient (in-memory, disposed after verification). This is the central data-minimization argument and MUST be enforced + evidenced in the retention policy (§DPIA-7).

**DPO gate surfaced by TIP-82R v0.9:** the current proof chain keeps `NfcArtifactHash = SHA256(DG2)`. DG2 is the CCCD chip portrait data and is stable for the card, so this hash is a stable per-card biometric-derived identifier. It is proof-bound into append-only evidence history, present in ordinary evidence-table backups, and returned to authorized BusinessConsumers through evidence-ledger / evidence-package summary APIs. This is not raw image retention, but it is not erasable by crypto-shred or ordinary row deletion. DPO/Homeowner must explicitly choose a disposition before real-patient trial reliance: acknowledge/ratify, stop disclosing `ArtifactHash` in BusinessConsumer DTOs, or require a separate proof-contract mitigation TIP.

---

## PART A — Patient consent content (to be turned into the signed/on-screen form)

### A.1 What we ask consent for
- Reading the **CCCD chip via NFC** (identity data: name, DoB, ID number, portrait DG2) for this visit's identity confirmation.
- Capturing a **live facial image + liveness check** to confirm the person present matches the CCCD.

### A.2 Purpose (limited)
Solely to **confirm your identity for the BHXH discharge/settlement confirmation** of THIS treatment episode (bảng kê khám chữa bệnh / cam kết sử dụng dịch vụ). Not used for any other purpose, not shared beyond the BHXH claim workflow.

### A.3 Legal basis
Your **explicit consent** under NĐ 13/2023 — biometric and health-linked data are **sensitive personal data**. You may **refuse** and **withdraw** at any time.

### A.4 Voluntary + alternative (must be genuine)
Consent is **freely given**. If you decline, you are **NOT denied service** — the hospital falls back to the existing manual identity check (naked-eye / liên thông), exactly as today. (A "consent" that gates care would not be valid under PDPD — the alternative path must be real.)

### A.5 What is kept, and for how long
- **Kept:** a signed identity-verification result (assertion + evidence hashes + timestamp) attached to your BHXH confirmation, retained per the hospital record-retention schedule.
- **Important RRI for DPO approval:** one retained evidence hash is currently `SHA256(DG2)`, a stable hash of the CCCD chip portrait data. It can link verification sessions for the same CCCD within systems that receive/keep the hash, including cross-consumer correlation if recipients compare or leak the value. It is included in append-only proof/evidence history and backups, and cannot be deleted without changing the proof contract.
- **NOT kept:** raw facial image, liveness video/frames, CCCD chip images — these are **discarded immediately after verification** (verify-and-discard).
- Retention period + deletion: [Legal to set — e.g., aligned to BHXH claim audit window].

### A.6 Your rights
Access, correction, deletion, withdrawal of consent, and complaint — [hospital DPO contact + process]. Withdrawal does not affect processing already lawfully done.

### A.7 Who processes it
The hospital (data controller) using its **on-premise** eKYC system. No data leaves the hospital infrastructure; **no cross-border transfer**; no third-party sub-processor beyond the on-prem system.

### A.8 Consent record
Consent (grant/refuse/withdraw) is **captured and stored as an auditable record BEFORE any biometric capture**, with timestamp + operator + scope. (Enforced technically by the consent-gate — see Part C.)

---

## PART B — DPIA input checklist (for the DPO's impact assessment)

- **B.1 Data inventory:** CCCD chip DG data (incl. DG2 portrait), live facial image, liveness media, derived match/liveness scores, signed proof. Classify each as sensitive (biometric/health-linked).
- **B.2 Necessity + proportionality:** identity confirmation is an EXISTING mandatory BHXH step; eKYC is a less-intrusive, consented replacement for photocopy-and-hold. Document why NFC+face is proportionate vs alternatives.
- **B.3 Legal basis + consent mechanics:** explicit consent; how obtained, recorded, and withdrawn; the genuine non-biometric alternative.
- **B.4 Data flows:** capture (agent) → verify (on-prem server) → signed proof to BHXH confirmation. Diagram it. Confirm NO raw biometric persisted, NO external transfer. Also confirm whether any BusinessConsumer/BHXH-facing integration receives evidence ledger or evidence package summary fields containing `ArtifactHash`.
- **B.5 Data minimization:** verify-and-discard; only proof + hashes retained. This is the primary mitigation — must be technically enforced + audited. DPO exception/acknowledgement required: current NFC proof hash is `SHA256(DG2)`, which is stable per CCCD and consumer-visible to authorized BusinessConsumer read APIs.
- **B.6 Security controls:** HSM-backed signing key, encryption at rest (Postgres) + in transit (HTTPS/TLS), role-based access, tamper-evident audit log, on-prem network isolation. (Delivered by the production-infra work — TIP-83.)
- **B.7 Retention + deletion schedule:** raw biometric = immediate delete; proof/evidence = [Legal-set window]; documented deletion + audit. `SHA256(DG2)` follows the proof/evidence retention schedule and persists in backups; it is not covered by raw-buffer disposal or crypto-shred.
- **B.8 Risk register + mitigations:**
  - *False accept* (wrong person) → mitigated by human backstop (visual + liên thông) AND calibrated thresholds (§5.2 study); eKYC assistive during trial.
  - *False reject* (patient blocked) → manual fallback always available.
  - *Biometric breach* → verify-and-discard + encryption + on-prem minimize exposure.
  - *Stable DG2 hash linkability* → DPO/Homeowner explicit disposition before real-patient trial; recommended first mitigation is to stop disclosing `ArtifactHash` in BusinessConsumer DTOs. If DPO rejects even internal proof-chain linkability, open a proof-contract mitigation TIP before relying on NFC/FaceMatch proof hashes.
  - *Function creep* → purpose limitation + access controls + audit.
  - *Document forgery (no CSCA yet)* → residual, accepted for trial given human backstop; documented in the CSCA risk-acceptance ADR; re-visit before scale-up.
- **B.9 Subject rights process:** how access/deletion/withdrawal requests are handled.
- **B.10 DPO + notification:** DPO appointed; assess whether A05 notification/registration applies for this sensitive-data processing.
- **B.11 Trial scope + review:** limited cohort, defined duration, supervised, with a go/no-go review before any expansion to open production or to bệnh án (higher-stakes) use.

---

## PART C — Technical consent-gate (engineering commitment tied to A.8/B.5)
Before ANY biometric capture, the app **hard-blocks** unless a consent record (grant, with scope + timestamp + operator) exists; refusal routes to the manual fallback; withdrawal is honored. Raw biometric buffers are disposed after verification; only the signed proof + evidence hashes persist. This is built as a fail-closed gate (separate engineering slice) so the privacy promises above are enforced by code, not policy alone.

---

## Open items for Legal/DPO to decide
1. Retention windows (raw = immediate delete is proposed; proof/evidence = ?).
2. Whether A05 notification/registration is required for this processing.
3. Exact consent wording + language(s) + literacy-accessible format.
4. Trial cohort definition, duration, and the go/no-go criteria to exit "trial → production".
5. Whether internal staff calibration-sample collection (§5.2) needs its own (simpler) staff consent — recommended yes.
6. DPO/Homeowner disposition of `NfcArtifactHash = SHA256(DG2)` as retained, backup-persistent, consumer-visible proof/evidence metadata: acknowledge/ratify, stop disclosing to BusinessConsumers, or require a separate proof-contract mitigation TIP before real-patient reliance.
