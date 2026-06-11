# TagEkyc Agent Coordination Bus

**File:** `docs/00_AGENT_COORDINATION_BUS.md`
**Version:** 0.9
**Status:** Active
**Date:** 2026-06-11
**Baseline:** Product Brief v0.1.1
**Purpose:** Defines how Codex, GPT web, reviewers, and future automations coordinate TagEkyc work with minimal user message-bus involvement.

## Changelog

### v0.9 - TIP-07 Option A implementation recorded

- Recorded that TIP-07 Planning Brief v0.3 was accepted for Option A only.
- Recorded TIP-07 Option A code/test commit `916dd2918c2ab47ab0658ebf271fae45e22fb3ca`.
- Recorded post-commit validation: 63 passed, 0 failed across contract, architecture, and unit tests.
- Recorded that TIP-07 Option A introduced no public route, webhook, outbox, retry, EF, or SignFlow drift.

### v0.8 - TIP-06 docs cleanup closed

- Recorded that `DOC-CLEANUP-TIP06` is complete because the coordination bus, TIP-06 kickoff packet, and TIP-02 roadmap already reflect accepted implementation and docs closeout state.
- Cleared the stale TIP-06 docs-cleanup active-work recommendation.
- Recorded the then-current dirty worktree as an untracked TIP-07 planning brief that was not yet authorized for implementation.

### v0.7 - TIP-06 runtime closeout synchronized

- Recorded that TIP-06 planning was accepted externally and runtime implementation was committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`.
- Recorded post-commit validation: 60 passed, 0 failed across contract, architecture, and unit tests.
- Replaced stale TIP-06 pending kickoff/implementation gate wording with docs-cleanup state.

### v0.6 - TIP-06 planning state synchronized

- Recorded that TIP-04 implementation exists and TIP-05 implementation was accepted after confirmation review.
- Recorded TIP-06 kickoff v0.1 internal draft v0.31 as the current active planning packet in the worktree.
- Replaced the stale TIP-04 kickoff recommendation with the current TIP-06 review and user-gate next step.

### v0.5 - TIP-03 closed

- Recorded TIP-03 as accepted and closed.
- Cleared TIP-03 review active work.
- Set TIP-04 kickoff preparation as the next recommended action.

### v0.4 - TIP-03 review state synchronized

- Recorded TIP-03 coordinator review as completed with no findings.
- Replaced stale TIP-02A active-work packet with TIP-03 review status and next-step guidance.
- Preserved the user gate on phase acceptance and on opening runtime work beyond the accepted TIP-03 boundary.

### v0.3 - TIP-02A hygiene pass recorded

- Recorded root `.gitignore` addition for .NET generated outputs and common local files.
- Confirmed restore/build/test still pass after the hygiene patch.
- Confirmed `bin/` and `obj/` no longer appear in `git status`.
- Kept TIP-02A active because `Note.txt` remains as a non-generated untracked local file outside the intended skeleton source set.

### v0.2 - S1 execution roadmap opened

- Added TIP-02 S1 execution roadmap as the implementation plan after TIP-01.
- Recorded TIP-02A repository hygiene and TIP-03 core runtime foundation as next recommended work.
- Preserved user gates for phase acceptance, LLD changes, and legal/compliance decisions.
- Recorded TIP-01 as accepted only for TIP-02A confirmation pass after external review requested repo evidence and generated-output hygiene.

### v0.1.2 - TIP-01 validation rerun recorded

- Revalidated TIP-01 skeleton workspace on 2026-06-10.
- Confirmed restore/build/test still pass with the current working tree.
- Kept the next step at user review or a new TIP, with no additional safe execution opened.

### v0.1.1 - TIP-01 skeleton dispatch executed

- Recorded TIP-01 skeleton implementation as completed pending user review.
- Captured restore/build/test validation results.
- Published next coordination step after successful skeleton dispatch.

### v0.1 - Coordination bus introduced

- Added file-based coordination model for agent handoffs.
- Defined coordinator automation responsibilities.
- Defined user decision gates and STOP+ASK triggers.
- Added message packet and queue format for future agents.

## 1. Goal

The user should not be the message bus between agents.

TagEkyc work SHOULD be coordinated through this file and related TIP/closeout documents. Agents should leave durable state, next actions, blockers, and decision requests in the repository so another agent or automation can continue without asking the user to restate context.

## 2. Coordination Model

Use a file-based bus with one coordinator automation.

- `docs/00_AGENT_COORDINATION_BUS.md` defines the protocol and current queues.
- Active TIP documents define implementation scope.
- Completion reports or closeout sections record finished work and validation.
- Git status records changed files, but it is not enough by itself.

The coordinator automation SHOULD periodically:

- Read the required baseline docs in governance order.
- Read this coordination bus.
- Read the active TIP and latest git status.
- Identify the next safe action.
- Execute safe in-scope work directly when possible.
- Request user input only for explicit gates or STOP+ASK triggers.
- Update this file or the active TIP/closeout when coordination state changes.

## 3. Roles

### Coordinator Automation

Owns cross-agent continuity.

Responsibilities:

- Maintain the current work state.
- Convert user goals into bounded TIP tasks.
- Dispatch implementation only when the TIP is ready.
- Run review/checklists before and after execution.
- Prepare concise packets for GPT web or another reviewer when external review is useful.
- Integrate reviewer findings into the active TIP or implementation plan.
- Stop and ask the user only when required by Section 5.

### Builder Agent

Owns implementation for the active TIP.

Responsibilities:

- Stay inside the active TIP scope.
- Preserve product baseline and LLD boundaries.
- Run required build/test checks.
- Write a completion report.
- STOP+REPORT if implementation would exceed scope.

### Reviewer Agent or GPT Web

Owns independent review.

Responsibilities:

- Review only the packet or files assigned.
- Return findings with severity and document/file references.
- Avoid changing scope unless explicitly asked.
- Identify baseline, legal, security, and LLD boundary risks.

### User

Owns product, legal, and gate decisions only.

The user should not be asked to relay routine agent messages, summarize completed work, or decide implementation details already covered by an accepted TIP.

## 4. Autonomy Rules

Agents MAY proceed without user input when all are true:

- The action is inside an accepted baseline or active TIP.
- The action does not change Product Brief, HLD, LLD, legal posture, or S1 scope.
- The action does not introduce production-certified eKYC claims.
- The action preserves SignFlow as a consumer profile only.
- The action does not store or expose raw sensitive evidence beyond documented boundaries.
- The action can be verified by local build/test/docs checks.

Agents SHOULD prefer direct execution over asking the user for routine choices when the TIP already resolves the decision.

## 5. STOP+ASK User Gates

Agents MUST stop and ask the user before:

- Accepting or closing a phase gate.
- Changing Product Brief source-of-truth decisions.
- Changing HLD or LLD behavior outside the active TIP.
- Expanding S1 scope or weakening a non-goal.
- Making legal, compliance, certification, retention, legal hold, or production-readiness decisions.
- Selecting a vendor or certified engine in a way that creates commercial, legal, or regulatory commitment.
- Introducing real biometric/raw artifact storage, retention enforcement, deletion policy, or legal hold behavior.
- Exposing raw artifacts, biometric data, internal VaultRefs, or sensitive evidence to business consumers.
- Changing SignFlow from a consumer profile into a platform dependency.
- Creating external accounts, paid services, credentials, or deployment infrastructure.

When asking the user, provide:

- The exact gate or decision.
- The default recommendation.
- Consequences of accepting or rejecting.
- Files or sections affected.

## 6. Message Packet Format

Use this format for durable messages between agents:

```md
### MSG-YYYYMMDD-HHMM-short-slug

- From:
- To:
- Status: New / Active / Blocked / Done / Superseded
- Gate: None / User / Review / Build-Test / Legal
- Scope:
- Context:
- Requested action:
- Output expected:
- Links:
```

Keep packets short. Link to docs, TIPs, commits, or reports instead of duplicating long content.

## 7. Queues

### Inbox

No open inbound agent messages.

### Active Work

No active in-scope coordination work.

#### MSG-20260610-0003-tip03-review

- From: Coordinator
- To: User / Next agent
- Status: Done
- Gate: Review
- Scope: TIP-03 implementation review follow-through.
- Context: `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md` now records TIP-02A as accepted and TIP-03 as accepted and closed. The coordinator re-reviewed TIP-03 against `tip_03_kickoff_v0_2.md`, re-ran `dotnet test TagEkyc.sln --no-restore`, and recorded `tip_03_review_v0_1.md` with no findings.
- Requested action: Completed. TIP-03 was accepted and closed.
- Output expected: None.
- Links: `docs/tips/tip_03_core_domain_contracts/tip_03_kickoff_v0_2.md`, `docs/tips/tip_03_core_domain_contracts/tip_03_execution_report_v0_1.md`, `docs/tips/tip_03_core_domain_contracts/tip_03_review_v0_1.md`, `docs/tips/tip_03_core_domain_contracts/tip_03_closeout_v0_1.md`, `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`

#### MSG-20260609-0001-tip01-dispatch

- From: Coordinator
- To: Builder
- Status: Done
- Gate: None
- Scope: TIP-01 skeleton-only dispatch.
- Context: `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md` dispatched and executed within skeleton-only boundaries.
- Requested action: Create .NET 8 Web API skeleton and placeholder xUnit projects within TIP-01 boundaries.
- Output expected: Completed. See TIP-01 execution report and current git status.
- Links: `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md`, `docs/tips/tip_01_project_skeleton/tip_01_execution_report_v0_1.md`

### Pending User Gates

No pending user gates. TIP-06 runtime/docs closeout and TIP-07 Option A code/test implementation are synchronized in governance state. Remaining dirty docs are TIP-07 closeout/governance records only.

### Decisions Recorded

- The coordinator automation may act as the routine message bus.
- The user is needed only for explicit gates, out-of-LLD changes, and legal/compliance decisions.
- TIP-01 skeleton dispatch is complete but only accepted for TIP-02A confirmation pass.
- TIP-01 skeleton dispatch completed locally with passing restore/build/test checks on 2026-06-09.
- TIP-01 skeleton workspace was revalidated on 2026-06-10 with passing restore/build/test checks.
- External review found TIP-01 needs clean repo evidence before full acceptance; generated `bin/` and `obj/` outputs exist as untracked workspace files and must be handled by TIP-02A.
- TIP-02A added root `.gitignore` on 2026-06-10 and removed generated `bin/`/`obj/` noise from `git status`.
- TIP-02A validation after `.gitignore` still passes: `dotnet restore`, `dotnet build -c Release --no-restore`, and `dotnet test -c Release --no-build`.
- TIP-02A confirmation report recorded that `Note.txt` is a scratch reference intentionally left outside the source set and does not block TIP-01 acceptance.
- `tip_02_roadmap_v0_2.md` remains the active-plan source of truth and now records TIP-02A accepted and TIP-03 accepted/closed.
- TIP-03 coordinator review on 2026-06-10 found no boundary or validation issues and revalidated the test suite with `dotnet test TagEkyc.sln --no-restore`.
- TIP-03 was accepted and closed on 2026-06-10 after commit-boundary cleanup. Final commits: `9ab27b1` bootstrap baseline and `19aa700` TIP-03 implementation.
- TIP-04 implementation records exist in `tip_04_execution_report_v0_1.md` with status `Implemented - awaiting review`.
- TIP-05 implementation records exist in `tip_05_execution_report_v0_1.md` with status `Implemented - accepted after confirmation review`.
- TIP-06 kickoff planning exists in `tip_06_kickoff_v0_1.md` as a historical planning record.
- TIP-06 planning was accepted externally; runtime implementation was committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`.
- TIP-06 post-commit validation passed: `TagEkyc.ContractTests` 8 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 36 passed, total 60 passed and 0 failed.
- `DOC-CLEANUP-TIP06` is complete; the coordination bus, TIP-06 kickoff packet, and TIP-02 roadmap now consistently reflect accepted implementation and docs closeout state.
- TIP-07 Planning Brief v0.3 was accepted for Option A only: internal/application service completion notification projection with no public route.
- TIP-07 Option A implementation was committed at `916dd2918c2ab47ab0658ebf271fae45e22fb3ca` (`feat: add TIP-07 completion notification projection`).
- TIP-07 post-commit validation passed: `TagEkyc.ContractTests` 9 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 38 passed, total 63 passed and 0 failed.
- TIP-07 review accepted the early implementation after targeted evidence review; confirmed no public route, webhook dispatcher/subscription, durable outbox, retry scheduler, EF/DbContext/migration/durable persistence, or SignFlow runtime/source/database dependency drift.
- Current dirty worktree state is limited to TIP-07 docs/governance closeout records.

### Next Recommended Action

Complete TIP-07 docs/governance closeout only. Do not modify `src/` or `tests/` without a separate accepted implementation gate.

### Outbox

### MSG-20260611-0001-tip06-kickoff-state

- From: Coordinator
- To: User / Next agent
- Status: Done
- Gate: None
- Scope: TIP-06 docs closeout only.
- Context: TIP-06 planning was accepted externally, and TIP-06 runtime implementation was committed at `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`. Post-commit validation passed: `TagEkyc.ContractTests` 8 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 36 passed, total 60 passed and 0 failed. The coordination bus, TIP-06 kickoff packet, and TIP-02 roadmap now reflect accepted implementation and docs closeout state.
- Requested action: Completed. Leave source, tests, `Note.txt`, and the untracked TIP-07 planning brief unchanged unless a later accepted TIP brings them into scope.
- Output expected: None.
- Links: `docs/tips/tip_06_final_decision_evidence_package/tip_06_kickoff_v0_1.md`, `docs/tips/tip_02_s1_execution/tip_02_roadmap_v0_2.md`, commit `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef`

### MSG-20260610-0004-tip03-review-complete

- From: Coordinator
- To: User / Next agent
- Status: Done
- Gate: Review
- Scope: TIP-03 review result.
- Context: The accepted TIP-03 kickoff v0.2 was reviewed against the current implementation and tests. No findings were identified.
- Requested action: Completed. TIP-03 was accepted and closed.
- Output expected: None.
- Links: `docs/tips/tip_03_core_domain_contracts/tip_03_review_v0_1.md`, `docs/tips/tip_03_core_domain_contracts/tip_03_execution_report_v0_1.md`, `docs/tips/tip_03_core_domain_contracts/tip_03_closeout_v0_1.md`

### MSG-20260609-1200-tip01-complete

- From: Coordinator
- To: User / Next agent
- Status: Superseded
- Gate: Review
- Scope: TIP-01 skeleton-only dispatch result review.
- Context: Superseded by TIP-01 review findings and `tip_01_execution_report_v0_1.md`. TIP-01 should not be fully accepted until TIP-02A records clean post-hygiene evidence.
- Requested action: Proceed with TIP-02A confirmation pass.
- Output expected: Clean source change set and restore/build/test evidence.
- Links: `docs/tips/tip_01_project_skeleton/tip_01_brief_v0_1.md`, `docs/tips/tip_01_project_skeleton/tip_01_execution_report_v0_1.md`, `README.md`

## 8. GPT Web Coordination

GPT web SHOULD be used as a reviewer or planning partner, not as the state owner.

If GPT web has no direct repository access, the coordinator should prepare a compact packet in `Outbox` containing:

- Goal.
- Relevant doc links or excerpts.
- Specific review questions.
- Expected finding format.

When GPT web returns findings, paste or import only the findings into `Inbox` or the active TIP. The coordinator should then resolve or reject findings against the baseline docs.

## 9. Automation Schedule Recommendation

During active development, run the coordinator every 4 hours during working days or on demand before/after a manual Codex session.

The coordinator should stay quiet when there is no safe next action. It should produce a concise status only when it changed files, completed checks, found a blocker, or needs a user gate.
