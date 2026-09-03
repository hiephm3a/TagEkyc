# TIP-88B1-E3 — Post-Build Remediation — BUILD DISPATCH

Status: **APPROVED FOR REMEDIATION IMPLEMENTATION**

Binding plan:

`docs/tips/tip_88b1e3_resolver_runtime_read_access/tip_88b1e3_remediation_plan_v3.md`

Repository:

`D:\Task\Remote Signing\TagEkyc`

HEAD anchor:

`774a94e30909d7257477e230f82a95b660793b1f`

This is a new remediation dispatch. The original E3 build dispatch remains
superseded and must not be reused. This document does not authorize commit,
push, merge, or deployment.

## 0. Re-anchor to the post-build working tree

Do not require `src/` or `tests/` to be clean. E3 is already implemented but
uncommitted.

Before editing:

1. Assert HEAD is the anchor above.
2. Recompute the complete source/test SHA-256 manifest in remediation-plan
   section 0.
3. Assert the migration source SHA-256 is:
   `885627346DB000D511A21F1710EAD6B55B4E539FE5620B609BA89D24CA1115E0`.
4. Assert ModelSnapshot SHA-256 is:
   `523E2823CACFB22091DDD01C3CC5D45241FC047121390A7A0647F2F645E708F6`.
5. On a fresh PostgreSQL 16 cluster, verify:
   - `160000 <= server_version_num < 170000`; patch releases inside major 16
     are allowed and the observed Task-0 value `160013` is not an immutable
     patch-version pin;
   - current migration is E3;
   - the three pinned function body hashes and ACLs;
   - B2 hardened Granted MD5;
   - required role attributes;
   - deployer pre/post capability manifests.
6. Classify dirty files using remediation-plan section 0.
7. Assert the planning brief lists exactly six actor-negative controls:
   missing, blank, malformed, actor mismatch, other connection, and NULL
   principal.
8. Stop on any mismatch other than the allowed PostgreSQL 16 patch release. Do
   not regenerate or silently bless a new baseline.

Task 0 is Baseline A and remains immutable audit evidence. After all permanent
implementation edits are complete, but before the first mutation proof, capture
Baseline B from every allowlisted source/test file and every catalog surface
listed in remediation-plan section 0. Every scratch mutation restores the exact
Baseline-B candidate hash/catalog snapshot. A green test alone is not
restoration evidence. Final audit proves both that B-minus-A contains only the
authorized remediation delta and that post-mutation B equals pre-mutation B.

## 1. Production posture

- PostgreSQL minimum version: 16.
- Dedicated application `LOGIN`, `INHERIT`, `NOSUPERUSER`, `NOCREATEDB`,
  `NOCREATEROLE`, `NOREPLICATION`, `NOBYPASSRLS`.
- `session_user == current_user`; production readiness does not support
  `SET ROLE`.
- Exactly one login-to-runtime `pg_auth_members` row across all grantors:
  `admin_option=false`, `inherit_option=true`, `set_option=false`.
- Login transitive reachable-role set exactly `{tagekyc_runtime}`.
- Runtime has no outgoing role membership.
- Runtime remains zero-of-seven across all fourteen protected tables.
- Deployer DML/ownership equals the pinned landed baseline; E3 adds only the
  allowed-classes SELECT delta.

## 2. Required implementation

### R1 — caller and role manifest

In `RawExportControlPlaneReadinessValidator.cs`:

- collect identity, server version, required role attributes, and OID-based role
  graph without named-role privilege helpers;
- implement the exact readiness order from remediation-plan section 4;
- reject PostgreSQL older than 16;
- reject active `SET ROLE`;
- validate all three capability-role attribute manifests;
- validate recursive closure, exactly-one edge, and every edge option.

### R2 — per-function backing reads

Pin the nine-table union and per-function mapping from remediation-plan section
3. Require effective deployer SELECT on every dependency. Preserve the complete
pre-E3 deployer DML/ownership baseline; do not impose a zero-write rule on the
deployer.

### R3 — migration-time exact function ACL

In `20260724015546_Tip88B1E3ResolverReadBoundary.cs`, validate exact overload,
owner, owner attributes, and exactly one non-owner ACL row:
`(grantor=tagekyc_raw_export_deployer, grantee=tagekyc_runtime,
privilege=EXECUTE, is_grantable=false)` before the migration transaction can
commit. Owner authority remains a separate check.

### R4 — fulfillment materialization

In `EfRawExportAuthorizationProjectionReader.cs`, implement the exact Accepted
and Withdrawn row-shape contract. Map violations to the existing invariant code
without null-to-empty conversion.

### R5 — B2 named-constraint mode

In the explicit E3 B2 Granted body:

1. set the named internal constraint DEFERRED before parent insert;
2. insert parent and class children;
3. force IMMEDIATE validation;
4. restore DEFERRED on successful return.

Update the hardened body hashes and authorized G4 assertion only. Down restores
the exact pre-E3 body.

## 3. Required non-vacuous gates

- elevated `session_user` plus `SET ROLE`;
- server version lower than 16 through an isolated catalog/control seam;
- missing/invalid required role attributes for all three capability roles;
- direct and transitive live membership mutations, plus ADMIN, INHERIT, SET,
  and parallel-grantor membership-row mutations;
- cycle-protection proof: PostgreSQL must reject a circular role grant with
  SQLSTATE `0LP01`; readiness is tested separately against valid direct and
  transitive graphs because a live cyclic catalog graph cannot be created;
- expected runtime EXECUTE retained plus an alternate-grantor runtime EXECUTE
  row must make readiness return the exact function-ACL code;
- each per-function backing read effectively absent before readiness;
- migration creator default-ACL pre-probes for both an unrelated grantee and an
  alternate grantor to runtime, each followed by failed E3 apply;
- all fulfillment null/empty/invalid negative cells;
- caller input constraint mode IMMEDIATE;
- two distinct Granted calls in one transaction;
- entry and successful-return constraint-mode mutation proofs.

Role, membership, rename, and default-ACL mutations run in a dedicated disposable
PostgreSQL 16 container with a unique name and dynamic port, never the shared
port-55432 fixture. Each captures pre-state, proves mutation effectiveness,
observes red, restores in `finally`, and proves post-state equivalence.

## 4. Permanent allowlist

Production:

- `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs`
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportAuthorizationProjectionReader.cs`
- `src/TagEkyc.Infrastructure/Persistence/Migrations/20260724015546_Tip88B1E3ResolverReadBoundary.cs`

Tests:

- `tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs`
- `tests/TagEkyc.IntegrationTests/Tip88B31RawExportAuthorizationSchemaTests.cs`
  — method
  `G4_migration_apply_rollback_reapply_preserves_acl_equivalence` only

Documentation:

- `tip_88b1e3_remediation_plan_v3.md`
- `tip_88b1e3_remediation_build_dispatch.md`
- `tip_88b1e3_planning_brief.md`
- `tip_88b1e3_build_dispatch.md` (supersession banner only)
- `postgres_migration_runbook.md`

No other permanent file may change without a stop/review.

Forbidden:

- landed B1/B2/B3 migrations;
- ModelSnapshot;
- public API/contracts;
- SignFlow;
- tenant model;
- new runtime functions;
- runtime direct SELECT;
- unrelated tests or dirty files.

## 5. Validation order

1. New named non-mutation gates individually.
2. Capture Baseline B after permanent implementation and before scratch
   mutations.
3. Every required observed-red mutation.
4. E3 plus B1 readiness targeted suites.
5. B1/B2/B3-1/B3-4 regressions.
6. Pending-model gate.
7. Full solution build.
8. Full suite sequentially.
9. Fresh PostgreSQL 16 catalog audit from remediation-plan section 11.
10. Final Baseline-A/B delta, candidate restoration, catalog-equivalence,
    scratch-role, scratch-default-ACL, and working-tree audit.

## 6. Stop conditions

Stop if:

- any Task-0 hash/catalog value differs, except the allowed PostgreSQL patch
  release inside major 16;
- PostgreSQL 16 edge options cannot be queried or enforced;
- a parallel-edge mutation is not distinguishable;
- a live cyclic graph is expected to reach readiness instead of being rejected
  by PostgreSQL with SQLSTATE `0LP01`;
- the dedicated mutation cluster is not isolated;
- Baseline B is not captured before mutations or exact candidate restoration
  cannot be proved;
- historical readiness precedence changes;
- a backing-read mutation leaves effective SELECT true;
- the default-ACL pre-probe is not active for the actual creator;
- a fulfillment test claims the wrong enforcement point;
- either B2 constraint-mode mutation stays green;
- a public error, ModelSnapshot change, landed migration edit, or new runtime
  function appears necessary.

## 7. Report

Report:

1. Baseline-A Task-0 hashes/classification, Baseline-B candidate hashes/catalog,
   the authorized B-minus-A delta, and exact post-mutation B restoration;
2. PostgreSQL version and exact role/edge manifests;
3. readiness precedence mapping;
4. deployer pre/post capability equivalence;
5. function ACL/default-ACL evidence including exact grantor OIDs/role names and
   the alternate-grantor observed-red proofs;
6. fulfillment matrix and exact residue counts;
7. both constraint-mode observed-red proofs;
8. targeted and full build/test totals;
9. fresh-catalog result and exact cleanup evidence;
10. deviations or incomplete work.

Do not commit. Do not push. Do not merge. Do not deploy.
