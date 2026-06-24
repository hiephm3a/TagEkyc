# TagEkyc Documentation Governance

**File:** `docs/00_DOCS_GOVERNANCE.md`
**Version:** 0.2
**Status:** Active
**Date:** 2026-06-08
**Baseline:** Product Brief v0.1.2
**Purpose:** Defines documentation versioning, changelog, and reading-order rules for TagEkyc docs.

## Changelog

### v0.2 - TIP directory and naming convention

- Standardized TIP documents under `docs/tips/tip_XX_slug/`.
- Added canonical TIP artifact filename format.
- Defined stable artifact names such as `brief`, `kickoff`, `review`, `execution_report`, `closeout`, and `roadmap`.

### v0.1 - Documentation governance introduced

- Added required document metadata convention.
- Added required changelog convention.
- Defined source-of-truth and reading-order rules.
- Defined expectations for future agents and TIP documents.

## 1. Source of Truth

`docs/00_product_brief_v0_1.md` is the product source-of-truth for the v0.1.2 baseline.

Other baseline documents must align to the Product Brief unless a later accepted baseline explicitly supersedes it.

Agents MUST NOT silently change the Product Brief. If a contradiction or typo is found, report it or patch it only when explicitly authorized.

## 2. Required Reading Order

Future agents SHOULD read documents in this order before changing planning or baseline docs:

1. `docs/00_README.md`
2. `docs/00_DOCS_GOVERNANCE.md`
3. `docs/00_product_brief_v0_1.md`
4. `docs/tag_ekyc_docs_baseline_closeout_v0_1_1.md`
5. The active TIP or target document being changed.
6. Supporting HLD/LLD/integration docs relevant to the task.

## 3. Required Document Metadata

Every new planning, baseline, or closeout document SHOULD include metadata near the top:

```md
**File:** `docs/...`
**Version:** 0.1
**Status:** Draft / Planning / Accepted / Active / Closed
**Date:** YYYY-MM-DD
**Baseline:** Product Brief v0.1.2
**Purpose:** Short purpose statement.
```

Existing docs SHOULD be upgraded to this format when they are materially patched. Small typo-only patches do not require metadata backfill unless requested.

## 4. Required Changelog

Every new planning, baseline, or closeout document SHOULD include a `## Changelog` section near the top.

Changelog entries SHOULD be concise and newest-first:

```md
## Changelog

### v0.1.1 - Minor alignment patch

- Clarified boundary or decision.
- Preserved S1 scope.

### v0.1 - Initial draft

- Created initial document.
```

If a document is materially changed, update its changelog in the same patch. A material change includes scope, architecture, API, data model, security boundary, profile, or acceptance criteria changes.

## 5. Versioning Rules

- Use `v0.x` while TagEkyc is in planning/S1 baseline work.
- Increment patch version for minor wording, alignment, or boundary clarification.
- Increment minor version for new sections, new planning decisions, or changed acceptance criteria.
- Do not claim an accepted baseline unless the user explicitly accepts it.
- Closeout documents should record the accepted baseline state and next recommended step.

## 6. TIP Document Rules

TIP documents MUST state:

- Purpose.
- Baseline inputs.
- Scope.
- Non-goals.
- Proposed skeleton or implementation boundary.
- Stack or decision status when applicable.
- Acceptance criteria.
- Open questions.
- Recommended next action.

TIP documents MUST include metadata and changelog once opened.

TIP planning changes SHOULD be visible inside the TIP document, not only in Git history.

## 7. TIP Directory and Naming Rules

Every TIP MUST live in its own folder:

```text
docs/tips/tip_XX_short_slug/
```

TIP file names MUST use:

```text
tip_XX_<artifact>_vA_B.md
```

Where:

- `XX` is a two-digit TIP number, for example `01`, `02`, `03`.
- `short_slug` is a stable lowercase purpose slug, for example `project_skeleton` or `s1_execution`.
- `<artifact>` is one of the stable artifact names below unless the user explicitly approves a new artifact type.
- `vA_B` is the document version with dots replaced by underscores, for example `v0_1` for document version `0.1`.

Canonical artifact names:

- `brief`: scope, decisions, non-goals, acceptance criteria, and execution report when the TIP is small.
- `kickoff`: dispatch packet for implementation work.
- `review`: independent review findings.
- `execution_report`: implementation result and validation details when it should be separate from the brief.
- `closeout`: accepted result, residual risk, and next recommended step.
- `roadmap`: multi-TIP execution plan or phase plan.

Examples:

- `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md`
- `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`
- `docs/tips/tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md`

The `**File:**` metadata line inside each TIP document MUST match the actual path.

## 8. Agent Patch Rules

Future agents SHOULD:

- Patch docs surgically.
- Preserve Product Brief as source-of-truth.
- Update document changelog for material changes.
- Avoid rewriting documents wholesale unless explicitly requested.
- Keep SignFlow as a consumer profile, not the base platform.
- Keep S1 bounded and avoid production-certified eKYC claims.
- Avoid source code, migrations, or project skeleton unless the active TIP explicitly dispatches that work.

## 9. Git History Is Not Enough

Git history is useful but not sufficient for future agents. The current document should explain its own version history and decision evolution so a future reader can understand why the document is in its current state without reconstructing all commits.
