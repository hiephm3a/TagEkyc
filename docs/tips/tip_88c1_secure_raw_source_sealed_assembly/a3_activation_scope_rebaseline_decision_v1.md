# A3 activation-scope re-baseline — Homeowner decision v1

Date: 2026-09-22. Decision source: the Homeowner's explicit “duyệt làm đi” in the task conversation, approving the seven-row/four-mechanism scope with the narrow continuous-validity correction. This record does not ratify the seven retained rows or authorize production activation.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 7
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: 56EC01946103E69FD4FB0F622B9FDD49BA3A4B87A78EF5BC383A0A2B0CCFC07E
A3-Partition-SHA256: B7AC9BB68204FA69F37FB4F16BBFFD305C3437CC6E5BAA136ADFDCB6777C91E3
A3-Ledger-SHA256: 5CA3C1ABAFDC4CD8F1AA3BF66A8D091EDEDA1940B5A93DA2EB22A93D8EA04E2C

## Exact disposition

- Reconcile `E01_Race_IssueWithdrawal` to its prior E01 ten-row Homeowner ratification, anchored in the reconciliation ledger. This is not a new ratification.
- Retain P04 and P05 as operative activation gates with the existing TRANSPORT-HEADER-GUARD and ADMISSION-CAPABILITY mechanisms.
- `DEFER_ASSEMBLY_GATE` for P29–P36: W2 authority to build remains, but assembly execution is not an Activated prerequisite for this delivery. The worker must remain `Disabled` under this decision.
- Of 31 `CANDIDATE_ONLY` rows, retain only `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`; defer the other 30 from this delivery's activation gate.
- Promote all four `DERIVED_ASSURANCE` rows into this delivery's activation gate: BP03, BP10, BP13, and `Agent raw HTTP Expect → durable server B/R1 before first body byte`.
- The seven retained rows and four shared mechanisms are the exact identities in `a3_activation_scope_partition_v1.tsv`. The 38 deferred rows are enumerated in `a3_activation_scope_deferred_backlog_v1.tsv`, not removed from history, marked `IMPLEMENTED`, or treated as disproven. That backlog preserves each row's prior proof status, authority class, mechanism, producer count/reachability, and proof strength.

`Activated includes assembly = NO` is equivalent to `DEFER_ASSEMBLY_GATE`; `Activated includes assembly = YES` would instead require `ASSEMBLY_IS_ACTIVATION_GATE`. A contradictory pair is invalid. The format-2 seal binds this exact decision, the ownership registry, partition and ledger. Startup compares the approved topology in that seal with effective production topology before Activated routes are selected. This decision permits only the `NO`/`Disabled` pair. A future `YES`/`DurableWorker` decision requires a successor Homeowner re-baseline, not a config-only change.

## Continuous validity and authority registry

`a3_outcome_ownership_table_v1.tsv` is the canonical authority-classification registry for this activation scope. Any A3 authority-status transition must update the registry in the same governance transaction. An authority decision absent from the registry does **not** affect A3 activation scope and must not be accepted as input to seal generation. This rule closes the case of a new Homeowner authority file appearing while the old cited source bytes remain unchanged.

The governance verifier/generator must fail closed before any later re-mint if a deferred `CANDIDATE_ONLY` requirement receives operative authority, a deferred assurance requirement becomes an activation requirement, the approved assembly topology changes, or any bound byte/provenance changes without a successor Homeowner decision. There is no automatic add/remove of RowIds, census recomputation or conversion to `IMPLEMENTED`. The old scope decision becomes insufficient and returns to the Homeowner. Runtime does not read governance documents; it uses exact reviewed identities compiled into the seal and compares the effective topology to the approved value.

## Limit

Current activation-open count is seven, not zero. A format-2 seal minted from this decision must therefore return `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE` for an otherwise valid Activated startup. The four retained mechanisms still need row-level proof and ratification; ordinary readiness and the final full suite also remain pending. A3 stays HOLD; no stage, commit, push, landing or production activation is authorized.
