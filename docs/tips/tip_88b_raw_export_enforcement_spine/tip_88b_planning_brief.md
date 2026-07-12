# TIP-88B (SUPERSEDED - split by GPT + Codex round-2 review, 2026-07-12)

The combined "enforcement spine" brief (v0.2) was split into two independently reviewable/landable slices, because one slice carrying identity + grant + fulfillment-validity + revocable lifecycle + authorization hot path + idempotency + evidence + permit + concurrency was too large to build-lock or adversarially verify safely (both reviewers required the split; CC had flagged it as OQ-1).

- **TIP-88B1 - Raw Export Control Plane** (identity surface + exact-version grant + fulfillment + revocable runtime lifecycle/activation; append-only, DB-enforced): `docs/tips/tip_88b1_raw_export_control_plane/tip_88b1_planning_brief.md`. **Review + land FIRST.**
- **TIP-88B2 - Raw Export Authorization Decision** (session binding + authorization hot path + idempotency + typed evidence + metadata permit + concurrency): `docs/tips/tip_88b2_raw_export_authorization_decision/tip_88b2_boundary.md`. Boundary only; opens after 88B1 lands + the verification-session recon.

Do not build from this file. The v0.2 content + full round-1/round-2 review history is preserved in git log.
