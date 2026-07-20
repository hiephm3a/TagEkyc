# TIP-88B (SUPERSEDED - split by GPT + Codex round-2 review, 2026-07-12)

The combined "enforcement spine" brief (v0.2) was split into two independently reviewable/landable slices, because one slice carrying identity + grant + fulfillment-validity + revocable lifecycle + authorization hot path + idempotency + evidence + permit + concurrency was too large to build-lock or adversarially verify safely (both reviewers required the split; CC had flagged it as OQ-1).

- **TIP-88B1 - Raw Export Control Plane** (identity surface + exact-version grant + fulfillment + revocable runtime lifecycle/activation; append-only, DB-enforced): `docs/tips/tip_88b1_raw_export_control_plane/tip_88b1_planning_brief.md`. **Review + land FIRST.**
- **TIP-88B2 - Raw Export Authorization Decision** (session binding + authorization hot path + idempotency + typed evidence + metadata permit + concurrency) was itself SPLIT (review round-1) and RENAMED 2026-07-19 into a clean sequential order:
  - **TIP-88B2 - Subject Export Consent** (greenfield append-only subject consent + server-derived purpose/recipient + deployment-only recording/withdraw authority + latest-event/no-fallback + TargetRevision + full DB/SECURITY-DEFINER manifest + resolver that returns the consented raw-class set without evaluating coverage; **READY_FOR_BUILD v1.0**): `docs/tips/tip_88b2_subject_export_consent/tip_88b2_planning_brief.md`. Build + land NEXT (88B1 is landed).
  - **TIP-88B3 - Raw Export Authorization Decision + Permit** (per-attempt authz hot path; consumes 88B1 + TIP-88B2; BLOCKED_ON_TIP_88B2_LANDED): `docs/tips/tip_88b3_raw_export_authorization_decision/tip_88b3_planning_brief.md` (+ `tip_88b3_boundary.md` = historical verification-session recon). Opens after TIP-88B2 lands + 88B1 landed.

Do not build from this file. The v0.2 content + full round-1/round-2 review history is preserved in git log.
