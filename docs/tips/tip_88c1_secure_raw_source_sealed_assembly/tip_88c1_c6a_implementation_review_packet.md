# TIP-88C1-C6A implementation review packet

Status: IMPLEMENTATION CANDIDATE — awaiting independent review

Dispatch SHA-256: `90373A24651B4A0C68FA889F84144E1EC538EB3F1F70E76282F017F944968A81`

Repository HEAD: `edcac28ef708b8a8ce31ed10e68c9ef95266de2a`

## Implemented scope

- Exact closed `Production` AuthoritySnapshot readiness profile, with the three
  ratified failure codes preserved.
- Broker-only atomic source-ingress composition function. It uses the landed
  alias/exact identities, `LEAST` then `GREATEST` lock order, bounded try-lock
  behavior, existing-claim SnapshotId recovery, and one-transaction append plus
  begin behavior for new claims.
- Current access/source revalidation inside each C3 begin-stream transaction,
  after durable locks and before the `Streaming` CAS.
- Readiness catalog proof for the composition function and C3 barrier helper.

No table, role, login, public API, DTO, PackageExpiresAt, package TTL,
RetentionProfileRef resolver, C6B adapter, provider operation, stage, commit or
push was added or performed.

## Builder decisions

1. The C3 direct recipient's current principal is required to equal the frozen
   decision/job principal. Existing C3 fixtures were changed to carry that
   frozen principal instead of generating an unrelated principal. This follows
   C6A19 and does not change direct-recipient client equality.
2. The C3 barrier is an owner-only SQL helper invoked by the existing
   SECURITY-DEFINER begin-stream function. This keeps the existing repository
   and public function contract unchanged and gives the helper no direct runtime
   EXECUTE grant.
3. The source snapshot uses existing policy metadata and deadlines. No
   operational retention duration is inferred from `RetentionProfileRef`.
4. The migration patches the existing C3 function body with a guarded,
   fail-closed exact-fragment replacement. Up and Down both stop on body drift.

## Independent-review corrections

The first implementation candidate incorrectly applied fulfillment
`ValidFromUtc`/`ValidUntilUtc` filters before `ORDER BY Revision DESC LIMIT 1`.
That could exclude a latest terminal event with null validity fields and select
an older `Accepted` event. The corrected barrier now selects the latest
fulfillment revision overall, then requires that exact revision to be
`Accepted` and currently valid. A latest `Withdrawn` revision therefore blocks
delivery instead of allowing stale accepted authority.

Five runtime negative proofs were added. Each creates a real finalized
package/delivery, mutates one current-authority input, invokes the production
coordinator, and proves `Ineligible`, zero content-reader opens, and no new
delivery event:

- `C6A07A`: latest fulfillment `Withdrawn`;
- `C6A06A`: latest grant `Revoked`;
- `C6A09A`: consent `Withdrawn`;
- `C6A10A`: AuthoritySnapshot terminal;
- `C6A11A`: effective retention horizon elapsed.

## Proof ledger

`C6A01` through `C6A23` plus the five `A` negative cases are represented by
named integration proofs. The
load-bearing database proofs include migration Up/Down/reapply, wrong-principal
rejection with no stream event, existing C3 concurrency/regression coverage,
and direct catalog inspection showing the composition function is owned by
`tagekyc_raw_export_deployer`, is SECURITY DEFINER, has
`search_path=pg_catalog`, and has exactly one non-owner grantee:
`tagekyc_raw_export_claim_broker`.

The existing C1/C2/C3 suites remain the source for committed replay, custody
convergence, provider-I/O ordering, streaming CAS and state-only reconciliation
proofs.

Final local evidence:

- complete C6A filter: 28 passed, 0 failed;
- complete C3 integration class: 50 passed, 0 failed;
- unit suite: 198 passed, 0 failed;
- build: 0 warnings, 0 errors;
- C6A migration Up/Down/reapply and pending-model proof: passed;
- C1 concurrency C116: passed on bounded rerun after one nondeterministic first
  observation;
- C1/C2/B2 combined run: 49 passed, with C116's first observation and the
  pre-existing model-snapshot hash tripwire reported separately;
- architecture suite: 139 passed, 1 pre-existing C5 model-snapshot hash
  tripwire failed for the same dirty snapshot.

Migration SHA-256:
`3D4D0106E5CBD568CF3053CA1BFBB529B55070CE975D9561E9F466F9D863F7CF`.

Migration designer SHA-256:
`8F4A59A11A2E27FAB59A0200405EE4772B705F83D8E89DEE2AA2A2B2565CFB68`.

## Known external worktree facts

The worktree was dirty before C6A. In particular, the C3 and C5 migrations and
the EF model snapshot already had modifications. C6A generated a migration with
no EF model delta. The pre-existing C2 snapshot-hash tripwire therefore remains
separately attributable and is not repaired under C6A authority.

No files are staged.
