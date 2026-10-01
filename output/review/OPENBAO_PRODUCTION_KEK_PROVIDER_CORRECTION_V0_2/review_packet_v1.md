# OpenBao production KEK provider — correction review packet v1

## Disposition

`TECHNICAL PASS CANDIDATE — FIVE REVIEW FINDINGS CORRECTED; REVIEW REQUIRED.`

Baseline is `580035f1eceab4763cb6d389fd97c925d6a794ee`, branch `tip-88a-raw-export-policy-catalog-build`, upstream posture `+2/-0`, activation seal revision 23. No commit, re-freeze, push, deployment, A3 reopening, Layer 2 change, SignFlow change, raw BIO protocol change, or backup/restore implementation is part of this candidate.

The pre-existing Homeowner file `docs/00_GDRIVE_FILE_INDEX.md` and pre-existing `output/review/PRODUCTION_COMPOSITION_CORRECTION_V0_1.zip` were not touched by this correction.

## Review correction summary

### H1 — exact Down boundary restored

The review's underlying defect was correct: Up uses `CREATE OR REPLACE` on the existing three-argument `tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint)`, but the candidate Down did not restore its predecessor body before dropping the new representation columns.

One premise in the correction dispatch was not accurate and was not implemented literally. Down does **not** delete all four historical functions. PostgreSQL overload identity keeps the legacy prepare/record/read signatures in the catalog; Up only drops their historical ACLs while adding new overloads. Creating guessed duplicate predecessor functions in Down would therefore be false work.

The actual exact-boundary correction is:

1. restore the same-signature activate body byte-for-byte from `20260802105416_Tip88C1B2DurableKeyProd.cs`;
2. restore the exact historical grants for the retained legacy prepare, record-wrapped, record-recovered, read-envelope and activate functions;
3. preflight the predecessor activate body before Up replaces it;
4. after Down, hash-verify five predecessor bodies with the existing `digest(prosrc with CRLF normalized to LF, sha256)` convention;
5. compare exact owner, `SECURITY DEFINER`, `search_path`, database ACL and the 41-entry predecessor StageRights boundary against a database independently migrated only to the predecessor.

The post-Down test invokes the real legacy prepare function (`InProgress`) and the restored activate function (`Activated`), proves all five signatures exist, and then reapplies the migration.

Exact predecessor activate body digest: `3292839f9446df41453efa2d8b0644bd34200970854014d44390b01f396f1004`.

### H2 — identical replay after Active remains idempotent

`PostgresAttemptKeyReservationProvider` now branches on `PrepareAsync == ExistingMatch` before `RandomNumberGenerator.GetBytes(32)` and before `WrapDekAsync`. It resolves the durable result through `LookupByOperationTokenAsync`, revalidates it through the existing record/activate database boundary, and returns the existing activation. A contradictory `PositivelyAbsent` result is treated as corrupt/unverifiable; unknown lookup state remains outcome-unknown.

`raw_export_openbao_issue_kek_operation` also locks and validates the existing generic operation and journal before deciding whether an issue is new. It may create a fresh journal entry only while the generic operation is `Issued`. When the operation is already `ResultObserved`, it returns the existing matching `Wrapped` journal only after exact token/context/provider/KEK/representation/scheme comparison; it cannot mint a second `Issued` entry.

The real OpenBao test performs a full first `ProvisionAsync` through Transit to `Activated`, snapshots provider operation id/token, journal revision/payload and reservation revision, stops OpenBao, and repeats the identical full `ProvisionAsync` through a lookup-counting/wrap-throwing provider wrapper. The replay remains `Activated`, records exactly one lookup and zero wrap calls, and leaves the snapshot byte-for-byte unchanged. Because candidate construction exists only immediately before the prohibited wrap call, this proves no second candidate DEK, Transit call, provider operation or journal revision is created.

### M1 — persistent authentication/authorization failure is unavailable

After the single existing re-login attempt, any remaining `OpenBaoTransportException` (including 401/403) maps to `KekWrapResult.Unavailable` and invalidates the cached token. It is not absence and not corruption.

The real test changes the AppRole policy to deny Transit, invalidates the current token, observes two denied attempts, and asserts `Unavailable`; it then restores the policy.

### M2 — exception taxonomy is explicit

- `OpenBaoTransportException`, `HttpRequestException`, `IOException`, and `SocketException` map to `Unavailable` because provider access or transport failed.
- `JsonException`, `KeyNotFoundException`, and `FormatException` map to `CorruptOrUnverifiable` because a received provider representation cannot be parsed or verified.
- every other exception maps to `OutcomeUnknown`, preserving fail-closed semantics without falsely asserting corruption.

Wrong response kind is normalized into `JsonException`, so a malformed OpenBao shape follows the representation-error branch rather than the unknown branch.

### L1 — encryption key access now fails closed with a stable boundary

The encryption operation catches non-caller-cancellation unwrap failures and throws `InvalidOperationException("RAW_EXPORT_KEY_ACCESS_INDETERMINATE", inner)`. Caller cancellation is still propagated. Verification retains its existing `KeyAccessIndeterminate` result. No `UnwrapDekAsync` contract changed and no raw OpenBao transport exception is exposed to the encryption caller.

### L2 — migration filename matches the EF migration identity

The SQL partial is now `20260930082453_OpenBaoProductionKekProvider.Sql.cs`, matching the `[Migration("20260930082453_OpenBaoProductionKekProvider")]` identity.

### AAD evidence — exact provider rejection, not catch-all exception evidence

The three real-provider assertions now require `OpenBaoTransportException`, HTTP status `400`, and `IsTransient == false` for:

1. wrong attempt-key context;
2. tampered opaque Transit ciphertext;
3. deleted Transit key.

No `ThrowsAnyAsync<Exception>` remains in any `OpenBao*.cs` test file.

The first scenario is the AAD context-binding proof. The second is ciphertext-integrity proof, not an AAD proof: changing/removing AAD cannot legitimately make a bit-flipped AEAD ciphertext authenticate. The third is a provider-key availability failure on the unwrap contract. `ClassifyWrapFailure` returns `KekWrapResult` and applies to the wrap contract; applying it to `UnwrapDekAsync` would require the contract change explicitly forbidden by this round.

The requested mutation that removes `associated_data` only from decrypt was run. It turns the valid positive decrypt into HTTP 400 before the wrong-context assertion, which proves that the provider requires the AAD on decrypt but cannot make the two negative assertions red. A second, semantically discriminating mutation makes the context contribution constant on both wrap and unwrap: the wrong-context decrypt then succeeds and the exact `ThrowsAsync<OpenBaoTransportException>` assertion fails with “No exception was thrown.” This directly proves context binding. The tamper assertion appropriately remains an integrity proof.

`ClassifyWrapFailure` intentionally maps every `OpenBaoTransportException`, including HTTP 400, to `Unavailable`. This is a conscious retry-safe choice for the wrap result contract: an external-provider rejection is not promoted to durable material corruption. The real unwrap path preserves its exact transport exception because `UnwrapDekAsync` has no result-union classification contract in this slice.

### Unrelated A3 assertion weakening removed

The proposed `ThrowsAsync` to `ThrowsAnyAsync` change in `Tip88C1C6BA3BrokerPipelineTests.cs` was reverted. Its exact-type baseline failure is disclosed in the census and is not used as OpenBao green evidence. No production change was made to conceal or accommodate it.

## Why the Up preflight hashes three predecessor functions

`raw_export_read_source_encryption_context` and the legacy prepare function are A3 lineage entry points already protected by the established boundary. This correction adds the same-signature activate function because this migration replaces that body in place and must later reproduce it exactly.

The other new Up functions are new names or new overloads and therefore have no predecessor body to hash. The unchanged legacy record/read overloads are not replaced; their bodies, owner/config and ACL are instead measured from an independent predecessor database and compared after Down. This avoids pretending that a newly introduced overload has a predecessor definition.

## Focused evidence

| Evidence | Result |
|---|---:|
| Restored focused OpenBao/migration/A3 gate | `12/12 PASS`, zero skip |
| Real OpenBao TLS/Raft/AppRole/Transit gate | `1/1 PASS`, zero skip |
| H1a missing required activate restore mutation | `0/1 RED` |
| H1b one-byte predecessor body mutation | `0/1 RED` |
| H1c missing predecessor ACL mutation | `0/1 RED` |
| M1 auth taxonomy regression mutation | `0/1 RED` |
| H2 replay-wrap mutation | `0/1 RED` at `ACTIVE_REPLAY_MUST_NOT_WRAP_OR_CREATE_A_DEK_CANDIDATE` |
| AAD decrypt-omitted mutation | `0/1 RED` at the valid positive decrypt |
| AAD context-unbound mutation | `0/1 RED` at the wrong-context exact exception assertion |
| Ubuntu .NET 8 Infrastructure build | PASS, 0 errors, 2 unrelated existing nullable warnings |

All seven red mutations have separate TRX files. After byte restoration, the 12-case focused gate and the real OpenBao gate both pass in their v2 successors. The failed-run census contains every retained red run and also discloses six diagnostic/interactive failures; none is cited as mutation evidence and none is unclassified.

## Mutation discrimination

- H1a fails at the migration's own exact-signature post-Down body guard.
- H1b's one-byte predecessor-body mutation is rejected by the migration's post-Down exact-body guard before the independent boundary comparison can execute.
- H1c fails at the exact role/signature ACL boundary comparison.
- M1 fails with `expected Unavailable / actual CorruptOrUnverifiable`.
- H2 fails when the active replay reaches the wrap-throwing observer, before it can be credited as an idempotent replay.
- The decrypt-omitted mutation fails at the valid positive decrypt, accurately demonstrating that omitting AAD from only one side cannot be the wrong-context discriminator requested in review.
- The symmetric context-unbound mutation fails at the wrong-context assertion because decrypt succeeds with the wrong context.

These are not build failures, fixture timeouts, or generic “an exception occurred” credits.

## Boundary and deployment facts

- Journal remains PostgreSQL; OpenBao KV was not introduced.
- Existing capability interfaces, fixture journal, A3 semantics, Layer 2, SignFlow and raw BIO protocol are unchanged.
- `POSTGRESQL_LOSSLESS_BACKUP_RESTORE_QUALIFICATION` remains OPEN and blocks production go-live, not this correction review.
- Production activation still requires explicit `TagEkyc:RawExport:AttemptKey:Topology=DurableKey`; the repository fixture default intentionally fails closed in Production.
- The current activation manifest intentionally drifts while these unreviewed product/test/evidence bytes exist. Re-freeze is forbidden before both reviews PASS.

## Requested review disposition

Review only the corrected H1/H2/M1/M2/L1/L2 boundaries plus the retained focused evidence. If both reviewers PASS the same bytes, the next transaction is commit followed by an evidence-only successor/re-freeze; push remains a separate Homeowner decision.
