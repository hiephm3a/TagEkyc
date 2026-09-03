# TIP-88C1-B2 DURABLE-OBJECT — O03 Mutation Correction

Version: 0.1
Status: DRAFT_NARROW_CORRECTION
Date: 2026-08-06
IndependentReview: PENDING
HomeownerRatification: NOT_RECORDED
CommitAuthority: NONE

## 1. Authority binding

This draft supersedes only the O03 mutation wording in §11.1 of:

```text
Path: docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_object_custody_build_dispatch.md
Version: 0.5
SHA-256: EFCA33CB6A17EE2FD4E07D22AB173162919A58B60F07BFC0222AB567E0126918
Proof ID: O03
Test method: O03_one_attempt_can_create_only_one_object_custody_row
```

No other dispatch row, production constraint, schema object, function signature,
state, transition, outcome token, readiness code, or evidence codec is changed.

## 2. Superseded wording

The one-layer mutation description is superseded:

```text
drop attempt unique constraint; controlled owner/GUC insert bypasses begin's
friendly replay check → second row for same attempt succeeds
```

That description does not account for the neighboring composite foreign key and
therefore cannot be satisfied by dropping the unique constraint alone.

## 3. Corrected O03 proof

### O03 canonical positive control

With the canonical schema, a second row for the same `AttemptId` must fail with:

```text
SQLSTATE: 23505
Constraint: uq_raw_export_provisional_objects_attempt
Row count for AttemptId: 1
```

### O03-A — neighboring-defense discrimination

Drop only:

```text
uq_raw_export_provisional_objects_attempt
```

Attempt the same controlled owner/GUC insert used by O03. The mutation must fail
with:

```text
SQLSTATE: 23503
Constraint: fk_raw_export_provisional_objects_attempt_binding
```

O03 must turn RED because it expected the canonical `23505` and exact unique
constraint name but observed the neighboring composite foreign-key defense.
Restore the unique constraint exactly before continuing.

### O03-B — load-bearing uniqueness proof

Drop exactly:

```text
uq_raw_export_provisional_objects_attempt
fk_raw_export_provisional_objects_attempt_binding
```

Use the same `AttemptId` with a fresh valid `ProvisionalObjectIdentity` and a
fresh valid `ObjectKey`. The second insert must succeed and the row count for the
`AttemptId` must be exactly `2`. Restore both constraints exactly afterward.

## 4. Unaffected production contract

The canonical production schema continues to require both constraints:

```text
uq_raw_export_provisional_objects_attempt
fk_raw_export_provisional_objects_attempt_binding
```

Their names, definitions, ordering, enforcement, migration bytes, and runtime
behavior are not changed by this draft. This document corrects only the required
mutation evidence so the unique constraint is tested without misrepresenting the
neighboring defense.

## 5. Lifecycle boundary

This draft is not ratified and grants no implementation, staging, commit, push,
merge, PR, deployment, production activation, Raw BIO, R2, or delivery authority.
It must not be edited in place into a ratified record. A separate append-only
successor is required after independent review and Homeowner ratification.
