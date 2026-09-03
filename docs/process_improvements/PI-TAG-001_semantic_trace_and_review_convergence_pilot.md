# PI-TAG-001 — Semantic Trace and Review Convergence Pilot

**Status:** CANDIDATE — APPROVED FOR ONE-TIP PILOT
**Pilot status:** NOT_STARTED
**Version:** 0.1
**Date:** 2026-07-26
**Owner:** Homeowner + Contractor/Drafter
**Pilot target:** first eligible complex TIP whose Planning Brief begins after
2026-07-26; record the exact TIP identifier before review round 1
**Binding scope:** the named pilot TIP only
**Proposed ratified code:** `L-TAG-Review-03` after a current code census
**Not yet:** a generally binding playbook rule

## 0. Changelog

### v0.1 — Initial candidate

- Adapted the TIP-88B4 semantic-trace and convergence lessons for a bounded
  TagEkyc pilot.
- Added the one-TIP round-5 checkpoint / round-10 hard-stop exception.
- Added activation, matrix, module, Drafter, reviewer, metrics, and promotion
  requirements.
- Preserved the active playbook outside the explicitly named pilot.

## 1. Purpose

PI-TAG-001 adds a semantic-trace discipline to the existing TagEkyc Review
Ladder. It is intended to prevent locally correct prose, code, or tests from
remaining disconnected from the interfaces, ordering, outcomes, durable state,
and discriminating proofs needed to make the requirement real.

This candidate is self-contained so a TagEkyc task does not depend on access to
a sibling repository. Its empirical origin is:

- TagEkyc TIP-88B4, version 0.9;
- eight recorded review rounds;
- Round 5 root-cause analysis;
- Round 8 `PASS — 0 HIGH / 0 MEDIUM / 0 LOW`;
- version 0.9 closeout-metadata update after the recorded Round 8 PASS.

The sibling SignFlow repository candidate, located from the TagEkyc repository
root at
`../Codex_SignFlow/docs/process_improvements/PI-011_semantic_trace_and_review_convergence_protocol.md`,
contains the cross-program form of the same lesson. It is background, not a
runtime dependency or source required to execute this pilot.

## 2. Relationship to the active TagEkyc playbook

This pilot extends:

- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md` §6, Autonomous Slice Review Ladder;
- §10, Subagent Review Pattern;
- §11, Dispatch Review Checklist;
- §13 `L-TAG-Review-01`, full coverage before invalidation review;
- §13 `L-TAG-Review-02`, finding classification and stop rule;
- §13 `L-TAG-Proof-01`, proof claim evidence-source rule.

The playbook remains authoritative except for the bounded loop-control
exception in §3 below. Nothing in this candidate weakens STOP/RRI, scope,
authorization, or evidence-source requirements.

## 3. One-TIP loop-control exception

The Homeowner authorized this pilot to test a different non-convergence rule
for one named TIP:

- rounds 1–4 follow the normal Review Ladder;
- if round 5 is not clean, stop patching temporarily and perform the mandatory
  root-cause checkpoint in §9;
- rounds 6–10 are allowed only when that checkpoint concludes that the work can
  converge inside the authorized scope without bypassing STOP/RRI;
- round 10 is a hard stop;
- no round beyond 10 is permitted without a new Homeowner instruction.

For the pilot only, this replaces the playbook §6 automatic five-round stop.
All other active playbook rules remain in force.

`L-TAG-Review-02` still governs finding classification. A clean verdict means:

```text
PASS — 0 actionable findings
```

`TEST_HARDENING_ONLY`, `BOOKKEEPING_ONLY`, or `DEFERRED` observations may remain
only when they are explicitly recorded and do not affect implementation,
dispatch safety, a load-bearing invariant, or the authorized acceptance
contract.

## 4. Eligibility and activation

A TIP is eligible when it introduces or changes at least one of:

- lifecycle or state transitions;
- transaction, concurrency, lock, lease, retry, or idempotency behavior;
- API authorization, error precedence, or externally observable contract;
- worker or queue processing;
- cryptographic or key behavior;
- restricted/raw-data handling;
- composition-root or cross-module wiring;
- a dispatch/governance transition spanning multiple dependent surfaces.

A wording-only, archive-only, metadata-only, or trivial mechanical TIP does not
consume the pilot unless the Homeowner explicitly designates it.

TIP-88B4 is the empirical source of this protocol and does not consume the
pilot. The pilot begins with the next eligible complex TIP.

Before review round 1, the pilot Planning Brief or review plan must contain:

```text
PI-TAG-001 PILOT
Pilot TIP: <exact identifier>
Risk tier: <Light | Full | High-risk>
Selected modules: <list>
Required matrices: Invariant Trace; plus conditional matrices
Independent reviewer: <required/not required + rationale>
Round-5 root-cause checkpoint: enabled
Round-10 hard stop: enabled
Pilot metrics report: required before closeout
```

After activation, update this file:

```text
Pilot status: ACTIVE
Pilot TIP: <exact identifier>
Activation date: <YYYY-MM-DD>
```

Do not edit the artifact while a reviewer is reviewing that version.

## 5. Core semantic trace

Before accepting a load-bearing requirement, trace:

```text
Requirement or claim
→ owning component or layer
→ ordering and prerequisites
→ interface, signature, DTO, or schema
→ normal outcomes and error precedence
→ state, evidence, and rollback residue
→ observable acceptance proof
→ discriminating negative or mutation proof
```

A requirement is not closed merely because it appears in prose or has a green
test. Every required row must be executable and observable through the surfaces
frozen by the same artifact.

## 6. Pilot matrices

### 6.1 Invariant Trace Matrix — always required

| ID | Requirement | Owner | Preconditions/order | Contract surface | Outcome/error | State/evidence/residue | Named proof | Negative/mutation proof |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |

No load-bearing cell may contain an unexplained `TBD`, “implementation detail,”
or Builder choice.

### 6.2 Outcome and Precedence Matrix — conditionally required

Trigger: two or more failures, retries, terminal states, validation layers, or
authority checks can compete.

| Command/use case | Higher-to-lower precedence | External result | Durable result | No-residue rule |
| --- | --- | --- | --- | --- |

Every named outcome must appear on the relevant interface, application mapping,
state/evidence rule, and named proof. Unreachable or unmapped outcomes are
findings.

### 6.3 Shape and Nullability Matrix — conditionally required

Trigger: union/sentinel results, optional records, polymorphic DTOs,
state-shaped rows, or partial-success responses.

| Variant | Required fields | Nullable/absent fields | Forbidden fields | Consumer mapping |
| --- | --- | --- | --- | --- |

### 6.4 Ordering Graph — conditionally required

Trigger: correctness depends on lock, event, middleware, validation,
retry/checkpoint, key-selection, or governance-transition order.

List all reachable entry paths, including direct calls, retries, workers,
administrative paths, and test harness paths. A canonical order is not closed
until every path uses it or is excluded with evidence.

### 6.5 Test-Bite Matrix — conditionally required

Trigger: a new load-bearing guard could pass through an earlier generic guard,
an unconditional rejection, a mock-only path, or echoed test input.

| Claim | Positive control | Negative case | Broken mechanism | Named test expected RED | Restoration proof |
| --- | --- | --- | --- | --- | --- |

Mutation proof is required when a green test would otherwise be ambiguous. For
low-risk docs/governance claims, an exact negative search or state-transition
audit may replace source mutation.

Each omitted conditional matrix requires a one-sentence `Not applicable`
rationale in the activation block or Planning Brief.

## 7. Risk modules

Select only applicable modules:

- **SQL/schema/transaction:** migration chain, catalog state, constraints,
  triggers, lock order, fresh-time semantics, SQLSTATE/message, rollback
  residue, apply/rollback/reapply, ACL equivalence.
- **API:** authentication/authorization order, existence leakage, DTO/version
  compatibility, status/error precedence, cancellation, timeout, stream
  interruption, idempotency.
- **Worker/queue:** receive, claim/lease, checkpoint, side effect, ack, duplicate
  delivery, reclaim, stale worker, poison message, lost receipt.
- **Cryptography/key management:** trusted key selection, version/fingerprint
  binding, operation and verification, rotation/revocation precedence, tamper,
  wrong key, algorithm mismatch, provider readiness.
- **Raw/restricted data:** authority, source resolution, bounded read,
  integrity, transformation, custody, disposal, buffer lifetime, logging/DB
  prohibition, cleanup, digest/package binding.
- **Governance/docs:** source hierarchy, terminology, version, status, mirror
  synchronization, stale wording, authorization boundaries, and no accidental
  implementation or readiness claim.

Every omitted module requires a one-sentence `Not applicable` rationale.

## 8. Drafter and reviewer protocol

For every accepted finding, the Drafter must:

1. classify and source-verify the finding;
2. identify affected matrix rows and downstream sections;
3. patch all affected active sections;
4. search for stale names, counts, status, rationale, and assumptions;
5. recheck version, status, placeholders, mirrors, and change history;
6. record whether scope, architecture, acceptance, source facts, or dispatch
   safety changed;
7. submit one stable artifact version for the next review.

For Full or High-risk review, the independent reviewer must:

- read the complete active artifact and explicitly required source documents;
- cite current evidence and the conflicting invariant;
- distinguish defect, recommendation, hypothesis, hardening, bookkeeping, and
  future-slice scope;
- review the full artifact again only when the patch invalidates full coverage
  under `L-TAG-Review-01`;
- not repeat a closed finding without new evidence;
- not stop after reaching a finding quota;
- report exact severity/actionability counts, including an honest zero.

## 9. Mandatory round-5 root-cause checkpoint

If round 5 is not clean:

1. freeze patching;
2. group all findings by semantic surface and originating patch;
3. classify the cause as:
   - Drafter patch-local regression;
   - incomplete earlier review;
   - unsupported, repeated, loose, or out-of-scope reviewer claim;
   - source-of-truth conflict;
   - unresolved architecture or scope;
4. identify why the current matrices or prompts failed to expose it earlier;
5. write corrective drafting and reviewer rules for the next version;
6. decide whether rounds 6–10 can converge inside authorized scope.

If the answer to step 6 is no, STOP immediately. Do not use the remaining round
budget merely because it exists.

## 10. Pilot metrics and decision

The pilot Completion/Review Report must record:

| Metric | Required evidence |
| --- | --- |
| TIP and risk tier | exact identifier, modules, and N/A rationales |
| Rounds to first clean verdict | integer |
| Findings by round | exact actionability/severity counts |
| Patch-local regressions | count and cause |
| Unsupported/repeated reviewer findings | count and evidence |
| Matrix gaps found before review | count |
| Test-bite/mutation failures | count and named proof |
| Round-5 checkpoint | used/not used and result |
| Builder false STOP or false pass | count |
| Post-dispatch contract correction | count |
| Wording/version/status drift | count |

The pilot is eligible for ratification only when:

1. all required matrix rows close with a clean verdict;
2. no unresolved wording/version/status/placeholder drift remains;
3. no false pass or false STOP is caused by the pilot contract;
4. no post-dispatch correction is caused by an omitted invariant;
5. reviewer findings remain evidence-backed and in scope;
6. review cost is proportionate to risk;
7. the Homeowner explicitly accepts the pilot outcome.

After the pilot, update `Pilot status` to `COMPLETED` and record the evidence.
An independent reviewer verifies the pilot report. The Homeowner then chooses:

- `RATIFY`;
- `REVISE_AND_RETRIAL`;
- `REJECT`.

Record the selected decision in this header and in
`docs/process_improvements/README.md`. `RATIFY` must name the canonical
playbook version that supersedes this candidate. `REVISE_AND_RETRIAL` requires
a new explicit Homeowner activation before another TIP may consume it.

Only `RATIFY` may patch the canonical playbook, allocate an official
`L-TAG-*` code, update templates, or make the protocol generally binding.

## 11. Non-authorization

PI-TAG-001 governs drafting and review only. It does not authorize:

- source, tests, schema, migration, package, API, runtime, or deployment work;
- raw/restricted-data collection, access, persistence, export, or delivery;
- provider, adapter, storage, resolver, key, algorithm, or tool selection;
- commit, push, merge, release, production, legal, audit, security,
  certification, readiness, or capability claims.
