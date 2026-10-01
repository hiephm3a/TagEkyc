# Production Raw Export Authority Snapshot — Intent Ledger

Status: IMPLEMENTATION AUTHORIZED / CANDIDATE NOT REVIEWED

Baseline: `a7a448f09c53cdea1f943b335ce70acf9c4adda3` (seal revision 25).

## Intent

Close `C1-B2-AUTH-FIXTURE-PRODUCTION-GATE` without creating a new authority state machine or treating a configuration flag as ratification.

## Expected Outcome

Production readiness accepts an exact production authority-snapshot profile only when the live PostgreSQL authority ledger is structurally bound to the already-landed authority evidence lineages:

- `LegacyExport` grants bind to an authorized export decision, its permit, policy and consent evidence;
- `SourceRetention` grants bind to the retained-source permit and consent-reference lineage;
- terminal events bind to an existing grant in the same authority scope;
- runtime cannot append, withdraw or revoke authority snapshots directly.

Fixture, missing, unknown, stale or unbound authority data remains fail-closed.

## Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Reuse `raw_export_authority_snapshots` and existing evidence tables | The schema already carries artifact, version, controller, policy, consent and principal provenance | Readiness/composition/tests only | Does not ratify new legal values |
| Keep the exact `Production` profile name but make it evidence-backed | Avoids a cosmetic rename while removing the self-asserted bypass | Existing configuration remains syntactically compatible; behavior becomes stricter | The string alone grants nothing |
| Permit an empty authority ledger | A host must start before the first session; readiness proves capability and rejects any invalid rows | No bootstrap fixture row is needed | Does not qualify a site or create an authority |
| Validate both `LegacyExport` and `SourceRetention` lineages | Both are existing production paths | No parallel authority protocol | Neither lineage may substitute for the other |

## Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| New authority table/schema version | Rejected in this slice | Existing columns and evidence relations are sufficient | STOP/RRI if implementation disproves this |
| `Ratified=true` or equivalent configuration assertion | Rejected | It is not evidence | None |
| Change D1 reuse/extension/legal semantics | Deferred and forbidden here | Requires separate legal/controller ratification | `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` remains unchanged |
| Backup/restore or site qualification | Deferred | Separate operations/deployment gates | Existing go-live gates remain open |

## Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| `C1-B2-AUTH-FIXTURE-PRODUCTION-GATE` | Add evidence-backed production readiness | Candidate closure | Requires focused proof and independent review |
| `C1-BB-D1-AUTHORITY-DISPOSITION-GATE` | No change | Still governed by existing ratified closed values and non-claims | Any semantic change requires new ratification |
| `POSTGRESQL_LOSSLESS_BACKUP_RESTORE_QUALIFICATION` | No change | OPEN | Blocks hospital go-live |

## Non-Claims

This slice does not change retention/legal semantics, collect authority evidence, authorize a hospital deployment, qualify a site, prove backup/restore, change Raw BIO protocol, reopen A3/Layer 2, or modify KEK, Claim Providers or SignFlow.

## Dispatch Readiness

- Implementation is authorized by the Homeowner.
- Allowed surfaces: authority-snapshot readiness/composition, focused integration tests, factual debt-registry update and review evidence.
- STOP/RRI: schema/migration, authority state-machine, retention/legal semantics, Raw BIO protocol, A3/Layer 2, KEK/Claim Providers or SignFlow change.
