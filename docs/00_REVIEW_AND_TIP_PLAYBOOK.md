# TagEkyc Review and TIP Playbook

**File:** `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`
**Version:** 0.1-draft
**Status:** Draft for review
**Date:** 2026-06-08
**Baseline:** Product Brief v0.1.1
**Purpose:** Captures reusable review, planning, and dispatch practices for TagEkyc TIP work.

## Changelog

### v0.1-draft - Initial cross-project playbook draft

- Generalized useful governance and review practices learned from prior SignFlow work.
- Defined TIP lifecycle expectations for TagEkyc.
- Defined subagent review patterns before dispatch.
- Added skeleton-only, security-boundary, and documentation-history guardrails.

## 1. Scope

This playbook captures reusable process lessons for TagEkyc documentation, planning, review, and TIP dispatch.

Lessons from prior SignFlow work may be reused only as generalized engineering and governance practices. They MUST NOT import SignFlow business semantics into TagEkyc.

TagEkyc remains an independent eKYC / identity assurance platform. SignFlow remains a transaction-bound consumer profile, not the base TagEkyc platform model.

## 2. Non-Import Rule

The following SignFlow-specific concerns MUST NOT be imported into TagEkyc platform behavior unless a future accepted TagEkyc baseline explicitly requires them:

- Digital signing.
- WYSIWYS rendering.
- Signing consent capture or proof.
- TSP/TSA/certificate workflows.
- SignFlow database, runtime, packages, internal models, or source code.
- Signing document content handling.
- Treating `purpose = SIGNING_AUTH` as the generic platform default.

Allowed imports are process-level lessons only:

- Documentation governance.
- Version history discipline.
- TIP planning before implementation.
- Scope and boundary review.
- Security/data-boundary review.
- Independent reviewer/subagent patterns.
- Completion and closeout reports.

## 3. Documentation Governance Rules

Every new planning, baseline, closeout, or playbook document SHOULD include:

- Metadata block.
- `## Changelog`.
- Clear status.
- Baseline reference.
- Purpose statement.

Material patches SHOULD update the changelog in the same patch.

Git history is not enough. A future agent should understand the current document state and decision evolution by reading the docs directly.

## 4. TIP Lifecycle

A TIP SHOULD move through these stages:

1. Planning opened.
2. Stack/scope decisions resolved.
3. Non-goals and acceptance criteria tightened.
4. Independent review performed when risk justifies it.
5. Findings returned before patching when review-only is requested.
6. Surgical planning patch applied if accepted.
7. Dispatch authorized.
8. Implementation completed.
9. Completion report written.
10. Commit or closeout performed.

Planning and implementation SHOULD NOT be mixed unless the user explicitly dispatches execution.

## 5. TIP Planning Template

TIP planning docs SHOULD include:

- Purpose.
- Baseline inputs.
- Scope.
- Non-goals.
- Proposed project/module shape.
- Stack decision.
- Acceptance criteria.
- Test expectations.
- Open questions.
- Recommended next action.
- Completion report expectations when applicable.

TIP planning MUST state what is deferred.

## 6. Skeleton-Only Guardrails

When a TIP is skeleton-only, it MUST NOT implement business behavior.

Skeleton-only means:

- Project/folder structure may be created.
- Build/test placeholders may be created.
- Namespace placeholders may be created.
- Minimal enum/type placeholders may exist only if required for compilation.
- Optional health/build smoke endpoint may exist.

Skeleton-only MUST NOT include:

- State transitions.
- Lifecycle validation.
- RequiredChecks enforcement.
- Evidence aggregation.
- Risk evaluation.
- eKYC engine behavior.
- Fake `PASSED`/`FAILED` responses.
- Persistence implementation.
- Migrations.
- Cryptography/signature implementation.
- Raw artifact storage.
- LLD03 business API routes/controllers.
- Full LLD03 DTO surface.

## 7. Security and Data Boundary Guardrails

TagEkyc docs and implementation plans SHOULD preserve these boundaries:

- Business clients do not receive raw artifacts by default.
- Business clients do not receive internal VaultRefs by default.
- Consumer payloads use sanitized summaries, evidence refs, package refs, hashes, and correlation fields.
- Raw artifact access requires explicit policy, scoped authorization, and audit.
- Capture agent/device gateway scopes are distinct from business client scopes.
- Internal adapter scopes are distinct from business client scopes.
- Operator/admin scopes are privileged and audited.
- Signature concepts remain separated as `payloadSignature`, `webhookSignature`, and `evidencePackageSignature`.

Any plan that weakens these boundaries SHOULD STOP+REPORT before implementation.

## 8. Integration Boundary Rules

Consumer integrations must not become the base platform model.

For SignFlow:

- SignFlow is a `TRANSACTION_BOUND_EKYC_PROFILE` consumer.
- `bindingNonceHash` is required for transaction-bound profile, not generic sessions.
- `purpose = SIGNING_AUTH` is SignFlow-specific, not generic default.
- TagEkyc.SignFlow placeholders must be TagEkyc-owned contracts only.
- No SignFlow source code, database, runtime packages, or internal models may be referenced.

## 9. Subagent Review Pattern

Subagents are useful when review questions are independent and can be separated by role.

Recommended reviewer roles:

- Product Baseline Alignment Reviewer.
- Security / Data Boundary Reviewer.
- Skeleton Scope Control Reviewer.
- Documentation Governance / Version History Reviewer.
- Architecture / Dependency Direction Reviewer.

Reviewers SHOULD return findings only unless patching is explicitly requested.

The main agent SHOULD:

- Give each reviewer a narrow role.
- Avoid asking reviewers to confirm a desired answer.
- Prefer limited context per reviewer when independence matters.
- Ask for severity and file/section references.
- Return findings before patching when requested.
- Patch only confirmed issues.

## 10. Dispatch Review Checklist

Before dispatching a TIP, check:

- Product Brief alignment.
- Generic platform vs consumer profile boundary.
- S1 scope control.
- Security/data boundary preservation.
- Documentation metadata and changelog.
- Acceptance criteria completeness.
- Test expectations.
- Deferred items.
- Git status and intended files.

For skeleton-only dispatch, also check:

- Clean build is required.
- Placeholder tests are required.
- No migrations.
- No persistence implementation.
- No business controllers/routes.
- No eKYC engines.
- No cryptography/signature implementation.
- No raw artifact storage.
- No fake adapter behavior.
- Architecture tests verify dependency direction if feasible.

## 11. Completion Report Expectations

A TIP completion report SHOULD include:

- Files changed.
- Summary of work.
- Scope boundaries respected.
- Tests/build run.
- Deviations or STOP+REPORT items.
- Deferred items.
- Git status.
- Self-check against non-goals.

If an expected check is not feasible, the completion report MUST explain why.

## 12. Current TIP-01 Application

For TIP-01, this playbook implies:

- Use .NET 8 Web API skeleton.
- Use xUnit placeholder tests.
- Include UnitTests, ContractTests, and ArchTests placeholders.
- Keep Infrastructure empty/placeholder only.
- Keep Adapters empty/shell only.
- Keep SignFlow as TagEkyc-owned transaction-bound profile placeholders only.
- Allow only optional health/build smoke endpoint in Api.
- Do not implement LLD03 business APIs.
- Do not implement persistence, migrations, Docker runtime services, cryptography, or eKYC business logic.
