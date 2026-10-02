# PRODUCTION_VERIFICATION_PERSISTENCE_PRINCIPAL_CORRECTION — Intent Ledger

Baseline: `b02898d02c9204fb8afb7d381d32a31bfa3d322e` / seal revision 26.

## Ratified intent

Adopt persistence-boundary option C. The ordinary production API login remains a member of
`tagekyc_runtime` for the existing function-gated runtime surface and additionally becomes a
member of a new NOLOGIN capability role, `tagekyc_application_persistence`, for the bounded
direct-EF verification persistence surface.

This correction does not grant table privileges to `tagekyc_runtime` and does not change the
raw-export privilege model.

## Accepted design

| Concern | Decision |
| --- | --- |
| Capability role | `tagekyc_application_persistence`, NOLOGIN, INHERIT, no elevated role attributes |
| Login membership | Deployment provisioning grants both `tagekyc_runtime` and the new capability to the ordinary API login |
| Core tables | `verification_sessions`, `capture_artifacts`, `evidence_results`, `verification_decisions`, `evidence_packages`, `evidence_manifests`, `audit_events`, `append_idempotency_records` |
| Read privileges | `SELECT` on the eight core tables |
| Append privileges | `INSERT` on the eight core tables |
| Mutation privilege | Column-scoped `UPDATE` on `verification_sessions` only |
| Forbidden privileges | `DELETE`, `TRUNCATE`, `REFERENCES`, `TRIGGER`, table-level `UPDATE`, schema/database `CREATE`, database or non-system object ownership, `WITH GRANT OPTION`, PUBLIC relation access, raw-export table DML, and new privileged-function execution |
| Readiness | Measure role attributes, exact membership, database owner/effective `CREATE`, exact table/column ACL across `r/p/v/m/f` relations, PUBLIC relation ACL, non-system ownership, grant options, absence of direct runtime access to the ratified core tables, and absence of capability leakage to capture/raw-export roles |
| E3 compatibility | Exact recursive ordinary role set is the two ratified capabilities; runtime stays zero-table-access for the ratified verification-core surface and the application capability receives no raw-export table DML |

The allowed `verification_sessions` update columns are:

`State`, `Result`, `AssuranceLevel`, `FinalDecisionId`, `EvidencePackageId`,
`EvidencePackageHash`, `ManifestHash`, `RequestId`, `CorrelationId`, `CompletedAt`.

## Rejected alternatives

- A: granting core-table DML to `tagekyc_runtime`, because it would collapse the raw-export
  least-privilege boundary.
- B: rewriting the core EF repositories as SECURITY DEFINER functions, because it is a larger
  persistence redesign than required to restore the existing product graph.
- A second DbContext or persistence subsystem.

## Non-claims

- This correction does not close PostgreSQL backup/restore qualification.
- It does not change authority, retention, consent, A3, Layer 2, KEK, Claim Providers,
  SignFlow, Raw BIO protocol, or raw-export semantics.
- It does not provision a site-specific login in a migration; login membership remains a
  deployment action.

## STOP gates

Stop and request new ratification if implementation requires a new data table/column,
application-port semantics change, new endpoint/protocol, transaction-boundary change,
raw-export ACL relaxation, or any product correction outside this capability boundary.

## Trace matrix

| Ratified invariant | Production mechanism | Focused proof |
| --- | --- | --- |
| Runtime keeps zero core-table access | Readiness measures `tagekyc_runtime` across all eight tables and update columns | Direct runtime SELECT/INSERT/UPDATE remains denied; mutation must turn readiness red |
| API gets only bounded EF access | ACL-only migration grants exact table and column privileges to the new role | Create/append/finalize flow under restricted API login |
| Views cannot create an undeclared read surface | Readiness inventories `r/p/v/m/f`, requires expected EF objects to remain `r/p`, and rejects both capability and PUBLIC privileges on every other relation | Restricted LOGIN reads a deliberately PUBLIC-granted definer-owned raw-export view; readiness returns `PROD_APPLICATION_PERSISTENCE_PRIVILEGE_INVALID` |
| Ownership cannot bypass ACLs | Readiness rejects the capability/current LOGIN as owner of any non-system schema, relation, sequence or function | Independent schema, relation and function ownership mutations each turn readiness RED and restore GREEN |
| Database authority cannot bypass the object census | Readiness rejects effective database `CREATE` for the LOGIN/runtime/application roles and rejects any of them as current-database owner | Direct `GRANT CREATE ON DATABASE` and `ALTER DATABASE ... OWNER TO` mutations each turn readiness RED, restore, then GREEN |
| Allowed grants cannot be delegated | Every allowlisted schema/relation/column/function ACL requires `is_grantable=false` | Schema USAGE, table SELECT, column UPDATE and function EXECUTE grant-option mutations each turn readiness RED and restore GREEN |
| No destructive privilege | Readiness rejects DELETE/TRUNCATE/REFERENCES/TRIGGER and table-level UPDATE | Per-arm privilege mutation |
| No role leakage | Readiness rejects membership held by runtime, capture identities, and raw-export roles | Membership mutation for capture/broker role |
| Missing capability fails closed | Startup membership check and readiness require both ordinary roles | Missing-membership negative proof |
| Existing raw-export function gate remains intact | No changes to raw-export ACLs or functions | Existing focused startup/catalog proof plus ACL census |
| E3 remains exact rather than permissive | E3 measures exactly two reachable roles, safe direct options on both edges, no outgoing capability edges, the unchanged runtime matrix, and a principal-specific application matrix | Two-capability GREEN, third-role RED, one-role mutant RED, overbroad current-user matrix mutant RED |

## Review and release posture

Implementation and focused evidence may be produced in the working tree. Product commit,
evidence successor/re-freeze, and push remain forbidden until independent review PASS.

## Homeowner amendment incorporated

The final ratified surface additionally includes `SELECT` only on
`tagekyc.api_keys` and `public."__EFMigrationsHistory"`, explicit `USAGE` on
both `tagekyc` and `public`, and exactly one audited SECURITY DEFINER boolean
companion function for managed-recipient scope matching. The application
capability retains zero table/column privilege on the three recipient tables.
Readiness performs a whole-database non-system ACL-entry census for schemas,
relations `r/p/v/m/f`, columns, sequences and functions, and distinguishes an
existing-but-unreadable relation with `PROD_DB_PRIVILEGE_INVALID`. The former
unlanded role name is superseded by `tagekyc_application_persistence`.

The managed-recipient SECURITY DEFINER function is a hardening boundary for the
application capability. It does not remove historical C5 grants that give
`tagekyc_runtime` SELECT on the three companion tables; the ordinary LOGIN still
inherits that read surface. Whether C5 can revoke it is a separate inventory and
correction, now recorded in the debt registry.
