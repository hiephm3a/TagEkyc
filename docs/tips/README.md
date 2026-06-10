# TagEkyc TIP Index

**File:** `docs/tips/README.md`
**Version:** 0.4
**Status:** Active
**Date:** 2026-06-10
**Baseline:** Product Brief v0.1.1
**Purpose:** Indexes TIP folders and records the TIP document naming convention.

## Changelog

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
| TIP-03 | `tip_03_core_domain_contracts/` | [`tip_03_kickoff_v0_2.md`](tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md), [`tip_03_execution_report_v0_1.md`](tip_03_core_domain_contracts/tip_03_execution_report_v0_1.md) |
