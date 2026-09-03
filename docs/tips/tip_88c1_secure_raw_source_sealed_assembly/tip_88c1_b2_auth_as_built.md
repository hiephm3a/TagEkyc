# TIP-88C1-B2-AUTH — As-Built: Controller Authority-Snapshot Lifecycle

This document maps the B2-AUTH implementation to the controller-authority
surface required by the TIP-88C1 planning contract. It records the
fixture-only authority profile and the exact boundary inherited by B2-CORE.

- Baseline: `c776f026532a7a93a6986a55c5c63258ab261ab2`.
- Migration:
  `20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs`.
- Profile configuration:
  `TagEkyc:RawExport:AuthoritySnapshot:Profile`.

## Landed lifecycle

`tagekyc.raw_export_authority_snapshots` is an append-only event ledger keyed
by `AuthoritySnapshotEventId`. Its scope is:

```text
ClientApplicationId
+ VerificationSessionId
+ CaptureAcceptanceId
+ RawClass
```

`Revision` is unique and monotonic within that scope. A `Granted` event stores
the immutable AuthoritySnapshot v1 fields. `Withdrawn` and `Revoked` are
sparse events bound to the effective grant by `TargetRevision`; they never
mutate the grant row.

The database binds each event to both the landed verification session and the
landed C1-A capture acceptance. The insert guard additionally proves that the
acceptance has the same client application, verification session, and raw
class as the authority event.

## Ratified fixture-only values

The closed fixture set is persisted and CHECK-constrained:

```text
ApprovedPurpose       = SubjectRawBiometricExport
ReuseDisposition      = FreshAuthorityRequired
ExtensionDisposition  = Forbidden
```

This is not a production legal-authority model. The
`C1-BB-D1-AUTHORITY-DISPOSITION-GATE` is ratified only for the fixture profile;
real controller-authority and D1 disposition values remain Gate-B work.

## Function and precedence contract

All functions are `SECURITY DEFINER`, owned by
`tagekyc_raw_export_deployer`, and use `search_path = pg_catalog`.

- `raw_export_append_authority_snapshot(...)` appends `Granted`, allocates a
  new `AuthoritySnapshotId`, and assigns the next scope revision.
- `raw_export_withdraw_authority_snapshot(...)` appends `Withdrawn` only when
  `TargetRevision` is the current effective grant.
- `raw_export_revoke_authority_snapshot(...)` applies the same rule for
  `Revoked`.
- `raw_export_resolve_current_authority_for_source(uuid,uuid,uuid,text,timestamptz)`
  returns the complete frozen grant or no row.

Resolution uses latest-event-overall precedence. It never falls back to an
older grant: a latest `Withdrawn`, `Revoked`, not-yet-valid grant, or expired
grant resolves to no row. A later valid `Granted` event becomes the new
authority and receives a new snapshot identity. B2-CORE must call this exact
resolver at its authority barrier.

## ACL and mutation boundary

- The table grants no privilege to `PUBLIC` or `tagekyc_runtime`.
- Append, withdraw, and revoke are deployer/bootstrap-only and are not
  executable by `tagekyc_runtime`.
- Runtime receives `EXECUTE` only on the read-only resolver.
- Every append path requires the fail-closed actor GUC.
- Direct INSERT is denied by a context guard.
- UPDATE and DELETE are denied even to the owner by the common append-only
  trigger.

No broker, completion function, source/attempt row, key operation, raw bytes,
network call, or production controller integration is added by this slice.

## Readiness boundary

The only accepted profile is `Fixture`.

```text
missing/empty
  -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING
unknown value
  -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID
Production + Fixture
  -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE
Development/Test + Fixture
  -> ready
```

Therefore table existence cannot make Production ready. A future Gate-B slice
must add the ratified production profile and controller-authority source.

## Model and verification

The ModelSnapshot change is additive for the authority event entity and its
session/acceptance relationships. The resulting snapshot SHA-256 is:

```text
33213141C81993180700AFF94F8B7198689B9D0D3855E385D9C123ECA4FC0211
```

The E3 snapshot tripwire is refreshed to that exact hash without changing its
round-trip or ACL logic.

The following scratch mutations were observed red and then restored:

- an 80-byte intended function name failed
  `C1B2_authority_identifiers_round_trip_and_acl_is_exact`;
- selecting the oldest revision failed
  `C1B2_latest_grant_after_withdraw_wins_without_reusing_snapshot_identity`
  with expected revision 3 and observed revision 1;
- filtering terminal events out before latest-event selection failed
  `C1B2_withdraw_and_revoke_make_latest_authority_resolve_none` by returning
  the prior grant;
- removing the append-only trigger failed
  `C1B2_append_only_direct_insert_and_actor_guards_fail_closed` because UPDATE
  no longer raised;
- removing acceptance-to-scope equality checks failed the same guard test
  because the foreign-client grant no longer raised.
- replacing both grant-path actor lookups with a fixed UUID failed the same
  guard test because an unset actor context no longer raised.
