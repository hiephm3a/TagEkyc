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
- Added TIP kickoff subagent convergence rules after TIP-04 review showed boundary review alone does not catch all dispatch-affecting decisions.
- Added TIP-05/TIP-06 confirmation lessons for API pipeline tests, review coverage ledgers, patch carry-through gates, blocker-only final gates, report updates, and allowlist commits.

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
- Dispatch Readiness Reviewer.
- Contract / API Semantics Reviewer.
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

For TIP kickoffs that may authorize implementation, subagent review SHOULD run as separate lanes rather than one generic self-check:

- Boundary / Security Reviewer: checks scope creep, raw data exposure, production security claims, persistence creep, dependency direction, and consumer-profile leakage.
- Dispatch Readiness Reviewer: checks whether implementation can start without judgment calls. It MUST list every unresolved `MAY`, `SHOULD`, `if feasible`, `chosen behavior`, open question, precedence conflict, default value gap, or test nondeterminism that affects implementation.
- Contract / API Semantics Reviewer: checks endpoint status codes, DTO defaults, caller ownership, request/response field authority, and deterministic test expectations.

Kickoff review SHOULD converge before external review or implementation dispatch:

- Round 1: run independent reviewer lanes and patch findings.
- Round 2: run at least a Dispatch Readiness re-review on the patched draft.
- Continue until the Dispatch Readiness Reviewer can answer: `Implementation can start without human judgment calls inside the documented boundaries.`
- If two consecutive review rounds produce only non-blocking wording improvements and no implementation-affecting findings, treat the draft as converged for review handoff.
- Do not count an explorer/context-gathering agent as a self-check review round. Explorers inform the main draft; reviewer lanes judge the draft.

The reviewer prompt for dispatch readiness SHOULD explicitly ask:

- Can implementation start without human judgment calls?
- Which open questions affect implementation?
- Which `MAY`/`SHOULD`/`if feasible` statements need pinned decisions?
- Are precedence rules specified when two rules conflict?
- Are error/status codes deterministic?
- Are response defaults deterministic?
- Can tests assert the chosen behavior without guessing?
- Are LocalDev/NonProduction shortcuts clearly named and gated?

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
- No implementation-affecting open questions remain.
- Ambiguous `MAY`/`SHOULD` choices are either pinned or explicitly deferred behind STOP+ASK.
- Error/status conventions and response defaults are deterministic.
- Rule precedence is specified where idempotency, uniqueness, policy, lifecycle, or authorization rules can overlap.
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

## 12. Runtime TIP Review Rules

TIP-05 showed that accepted service behavior is not enough when a TIP adds runtime HTTP endpoints.

For any TIP that adds or changes runtime endpoints:

- API pipeline tests are required. Route inventory tests are useful but do not prove header auth, JSON binding, endpoint delegates, status codes, or error envelopes through the actual HTTP path.
- Prefer `TestServer`, `WebApplicationFactory`, or equivalent `HttpClient` tests for caller category, scope, request body, response envelope, and lifecycle behavior.
- A route inventory test may prove specialized endpoints are absent, but it does not replace HTTP happy-path and rejection-path coverage.
- If confirmation finds only service-level tests for a runtime endpoint, STOP+REPORT before acceptance unless the user explicitly waives API-level coverage.

Subagent review convergence must be measured on the same decision surface:

- Do not split two reviewers across unrelated aspects and call the result converged.
- Run reviewers against the same kickoff/report using the same acceptance and risk rubric when convergence matters.
- Treat matching findings as strong signal, divergent findings as synthesis input, and single-reviewer findings as items to verify rather than discard.
- The main agent should synthesize findings around implementation-affecting ambiguity: caller identity, ownership, state transitions, endpoint boundary, data integrity, error precedence, and testability.

### L-TAG-Review-01 - Full Coverage First, Then Invalidation Review

A TIP that touches runtime contracts, lifecycle, DTOs, audit/hash behavior, finalization, or security boundaries MUST receive at least one full-system review with coverage attestation before review narrows to patch deltas.

The full-system review MUST cover:

- Lifecycle/state behavior.
- Security and public/private data boundaries.
- Hash, audit, evidence chain, idempotency, and determinism.
- API/error precedence and DTO defaults.
- Test/proof level.
- Scope boundaries and STOP gates.
- Builder ambiguity.
- Repo-real feasibility.

After full coverage is established, later patch reviews MUST NOT reset to full-document exhaustive review by default. The patcher MUST provide an affected-surface map that lists:

- Rule changed.
- Sections touched.
- DTO impact.
- API impact.
- Error/status impact.
- Hash/audit impact.
- Test impact.
- STOP gates.
- Tail, matrix, and self-check impact.

Reviewers SHOULD re-review only the invalidated coverage areas plus a lightweight blocker-only sentinel sweep for stale `NEEDS PATCHES`, `pending`, old draft labels, superseded gates, deprecated field names, and direct contradictions.

A full review MAY be rerun only when a patch changes core invariants, changes scope, introduces new runtime/API surface, or contradicts a previously clean coverage area.

### L-TAG-Review-02 - Finding Classification and Stop Rule

Review findings MUST be classified as:

- `BLOCKER`.
- `PATCH_REGRESSION`.
- `LATENT_SPEC_GAP`.
- `TEST_HARDENING_ONLY`.
- `BOOKKEEPING_ONLY`.
- `DEFERRED`.

Only `BLOCKER`, `PATCH_REGRESSION`, and implementation-blocking `LATENT_SPEC_GAP` findings require immediate patch and re-review.

After two consecutive rounds with no blocker-category findings, the final gate MUST stop planning review and move hardening-only items to Deferred Notes unless a new patch changes core invariants, scope, runtime/API surface, or a previously clean coverage area.

Final external or architect gates SHOULD be blocker-only unless explicitly asked for editorial polish. Blockers are limited to:

- Builder can implement wrong runtime behavior.
- Public or BusinessConsumer API can leak internal, raw, or sensitive data.
- Completed/package/hash/audit/idempotency/determinism invariant can be broken.
- A critical invariant has no representative proof or test.

Wording polish, extra overlap tests, matrix formatting, minor bookkeeping, and out-of-scope production concerns SHOULD be recorded as `DEFERRED` notes instead of restarting planning review.

Confirmation review SHOULD include explicit STOP gates:

- Runtime endpoint tests are service-only and do not cover API/route behavior.
- A deferred or specialized endpoint is mapped.
- BusinessConsumer can reach capture/evidence write paths.
- A readiness state such as `READY_TO_COMPLETE` leaks final decision semantics.
- Boundary scans show persistence, raw upload, production auth/secret lifecycle, webhook runtime, or other deferred infrastructure.

Execution reports must be updated after confirmation patches:

- If tests, packages, validation totals, boundary interpretation, or accepted status change during confirmation, update the TIP execution report before commit.
- The report should describe confirmation-only patches separately from implementation behavior.

When committing with a dirty worktree:

- Stage by explicit allowlist.
- Check `git diff --cached --name-only` before commit.
- Verify excluded dirty files remain unstaged.
- Do not include unrelated playbook, coordination, note, or prior-TIP files in a feature commit unless explicitly requested.

## 13. Current TIP-01 Application

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
