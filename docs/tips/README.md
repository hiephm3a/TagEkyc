# TagEkyc TIP Index

**File:** `docs/tips/README.md`
**Version:** 0.15
**Status:** Active
**Date:** 2026-06-12
**Baseline:** Product Brief v0.1.1
**Purpose:** Indexes TIP folders and records the TIP document naming convention.

## Changelog

### v0.15 - TIP-11 Option B implementation closeout indexed

- Added TIP-11 Option B execution report and closeout to the TIP index.
- Recorded TIP-11 Option B implementation as complete pending homeowner/GPT closeout review.
- Preserved that no future TIPs or new runtime work are opened by the closeout.

### v0.14 - TIP-11 Option B kickoff accepted

- Recorded TIP-11 Option B kickoff as accepted.
- Preserved implementation as a separate next step under the accepted Option B dispatch allowlist.

### v0.13 - TIP-11 Option B kickoff draft indexed

- Added TIP-11 Option B kickoff draft to the TIP index.
- Preserved the kickoff as pending review and not authorized for implementation.

### v0.12 - TIP-11 planning accepted

- Recorded TIP-11 planning brief as accepted.
- Preserved Option B as the next kickoff candidate only, with no implementation dispatch.

### v0.11 - TIP-11 S2 planning opened

- Added TIP-11 production data boundary and durable state foundation planning brief.
- Recorded TIP-11 as S2 planning-only with no implementation dispatch.

### v0.10 - TIP-10 planning accepted

- Recorded TIP-10 planning brief as accepted.
- Preserved TIP-11 as the accepted next runtime-TIP recommendation, not an implementation dispatch.

### v0.9 - TIP-10 planning indexed

- Added TIP-10 production readiness planning compass to the TIP index.
- Preserved TIP-10 as planning-only with no runtime dispatch.

### v0.8 - TIP-09 closeout indexed

- Added TIP-09 S1 hardening closeout to the TIP index.
- Added previously omitted TIP-04, TIP-05, and TIP-06 implementation artifacts to the index table for continuity.

### v0.7 - TIP-08 indexed

- Added TIP-08 planning brief and closed kickoff document to the TIP index.

### v0.6 - TIP-07 indexed

- Added TIP-07 completion notification planning brief to the TIP index.

### v0.5 - TIP-03 closed

- Added TIP-03 closeout to the TIP index.

### v0.4 - TIP-03 execution report indexed

- Added TIP-03 execution report to the TIP index.

### v0.3 - TIP-03 kickoff v0.2 accepted

- Updated TIP-03 kickoff index to the accepted v0.2 review-patched document.

### v0.2 - TIP-03 kickoff indexed

- Added TIP-02A confirmation report to the TIP-02 artifact list.
- Added TIP-03 core domain/contracts kickoff document.

### v0.1 - TIP index introduced

- Added a central index for TIP folders.
- Recorded the canonical TIP folder and artifact filename convention.

## Naming Convention

Each TIP has its own folder:

```text
docs/tips/tip_XX_short_slug/
```

Each TIP artifact uses:

```text
tip_XX_<artifact>_vA_B.md
```

Canonical artifact names:

- `brief`
- `kickoff`
- `review`
- `execution_report`
- `closeout`
- `roadmap`

## TIP Folders

| TIP | Folder | Current artifact |
| --- | --- | --- |
| TIP-01 | `tip_01_project_skeleton/` | [`tip_01_brief_v0_1.md`](tip_01_project_skeleton/tip_01_brief_v0_1.md), [`tip_01_execution_report_v0_1.md`](tip_01_project_skeleton/tip_01_execution_report_v0_1.md), [`tip_01_review_v0_1.md`](tip_01_project_skeleton/tip_01_review_v0_1.md) |
| TIP-02 | `tip_02_s1_execution/` | [`tip_02_roadmap_v0_2.md`](tip_02_s1_execution/tip_02_roadmap_v0_2.md), [`tip_02_review_v0_3.md`](tip_02_s1_execution/tip_02_review_v0_3.md), [`tip_02a_confirmation_report_v0_1.md`](tip_02_s1_execution/tip_02a_confirmation_report_v0_1.md) |
| TIP-03 | `tip_03_core_domain_contracts/` | [`tip_03_kickoff_v0_2.md`](tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md), [`tip_03_execution_report_v0_1.md`](tip_03_core_domain_contracts/tip_03_execution_report_v0_1.md), [`tip_03_review_v0_1.md`](tip_03_core_domain_contracts/tip_03_review_v0_1.md), [`tip_03_closeout_v0_1.md`](tip_03_core_domain_contracts/tip_03_closeout_v0_1.md) |
| TIP-04 | `tip_04_api_key_policy_session_lifecycle/` | [`tip_04_kickoff_v0_3.md`](tip_04_api_key_policy_session_lifecycle/tip_04_kickoff_v0_3.md), [`tip_04_execution_report_v0_1.md`](tip_04_api_key_policy_session_lifecycle/tip_04_execution_report_v0_1.md) |
| TIP-05 | `tip_05_capture_artifact_evidence_recording/` | [`tip_05_kickoff_v0_3.md`](tip_05_capture_artifact_evidence_recording/tip_05_kickoff_v0_3.md), [`tip_05_execution_report_v0_1.md`](tip_05_capture_artifact_evidence_recording/tip_05_execution_report_v0_1.md) |
| TIP-06 | `tip_06_final_decision_evidence_package/` | [`tip_06_kickoff_v0_1.md`](tip_06_final_decision_evidence_package/tip_06_kickoff_v0_1.md) |
| TIP-07 | `tip_07_completion_notification/` | [`tip_07_planning_brief_v0_3.md`](tip_07_completion_notification/tip_07_planning_brief_v0_3.md) |
| TIP-08 | `tip_08_signflow_transaction_bound_profile/` | [`tip_08_planning_brief_v0_3.md`](tip_08_signflow_transaction_bound_profile/tip_08_planning_brief_v0_3.md), [`tip_08_kickoff_v0_4.md`](tip_08_signflow_transaction_bound_profile/tip_08_kickoff_v0_4.md) |
| TIP-09 | `tip_09_s1_hardening_closeout/` | [`tip_09_closeout_v0_1.md`](tip_09_s1_hardening_closeout/tip_09_closeout_v0_1.md) |
| TIP-10 | `tip_10_production_readiness_planning_compass/` | [`tip_10_planning_brief_v0_1.md`](tip_10_production_readiness_planning_compass/tip_10_planning_brief_v0_1.md) - accepted planning-only |
| TIP-11 | `tip_11_production_data_boundary_durable_state_foundation/` | [`tip_11_planning_brief_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_planning_brief_v0_1.md) - accepted planning-only, [`tip_11_kickoff_option_b_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_kickoff_option_b_v0_1.md) - accepted Option B kickoff, [`tip_11_option_b_execution_report_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_execution_report_v0_1.md) - implemented, [`tip_11_option_b_closeout_v0_1.md`](tip_11_production_data_boundary_durable_state_foundation/tip_11_option_b_closeout_v0_1.md) - pending closeout review |
