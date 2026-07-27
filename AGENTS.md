# TagEkyc Repository Instructions

These instructions apply to the entire repository.

## Required governance reads

Before drafting, reviewing, dispatching, implementing, or closing a TIP, read
the applicable sections of:

- `docs/00_REVIEW_AND_TIP_PLAYBOOK.md`

For the first eligible complex TIP whose Planning Brief begins after
2026-07-26, also read and apply:

- `docs/process_improvements/PI-TAG-001_semantic_trace_and_review_convergence_pilot.md`

PI-TAG-001 is a one-TIP pilot, not a generally ratified rule. Follow its
`Pilot status`:

- `NOT_STARTED`: the first eligible complex TIP must activate it before review
  round 1;
- `ACTIVE`: apply it only to the named pilot TIP;
- `COMPLETED`, `RATIFIED`, `REVISE_AND_RETRIAL`, `REJECTED`, or `SUPERSEDED`:
  do not apply it to another TIP unless a later Homeowner instruction
  explicitly reactivates it.

An eligible complex TIP is defined by PI-TAG-001. A wording-only,
archive-only, or trivial mechanical TIP does not consume the pilot.
TIP-88B4 is the empirical source of PI-TAG-001 and does not consume the pilot.

## Precedence and scope

- Current user and system instructions take precedence.
- The playbook remains the general TagEkyc workflow.
- PI-TAG-001 may alter review-loop handling only for its explicitly named
  pilot TIP and only within the Homeowner-authorized exception recorded there.
- Neither this file nor PI-TAG-001 grants implementation, schema, migration,
  raw-data, provider, production, deployment, commit, push, or merge authority.
- Preserve unrelated dirty files and use an explicit allowlist when staging.
