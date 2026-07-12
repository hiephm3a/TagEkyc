# TIP-88 (SUPERSEDED - split 2026-07-10, 88B further split 2026-07-12)

This combined "policy-gate foundation" brief was split for adversarial-verifiability. Current shape (governance index):

- **TIP-88A - Raw Export Policy Catalog Foundation** (INERT data spine; no runtime export capability): `docs/tips/tip_88a_raw_export_policy_catalog/`. **LANDED + PUSHED** (commit `e63cdf9`, closeout `62fed65`) - the 6-table append-only catalog.
- **TIP-88B - Raw Export Enforcement Spine** was itself SPLIT into two slices (too large as one): see `docs/tips/tip_88b_raw_export_enforcement_spine/tip_88b_planning_brief.md` (superseded pointer). The current pieces are:
  - **TIP-88B1 - Raw Export Control Plane** (PROPOSED): identity surface + exact-version grant(+revoke) + fulfillment(+scoped recorder authority) + revocable runtime lifecycle + fail-closed activation gate + effective-state resolver. Append-only, DB-enforced. **NOT authorization/evidence/permit/package** - those are 88B2. `docs/tips/tip_88b1_raw_export_control_plane/`. Review + land FIRST.
  - **TIP-88B2 - Raw Export Authorization Decision** (BOUNDARY only): the per-attempt authorization hot path - verification-session binding + subject-consent + idempotency + typed decision/evidence + metadata permit + concurrency. Opens after 88B1 lands + a verification-session recon. `docs/tips/tip_88b2_raw_export_authorization_decision/`.
- **Raw-assembly / RawBioPackage slice** (LATER, after 88B2): the actual raw read/assemble + real package/manifest hash + delivery + one-time consumption. NOT any of the above.

Do not build from this file.
