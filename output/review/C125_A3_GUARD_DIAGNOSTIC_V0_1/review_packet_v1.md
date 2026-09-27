# C125 A3 guard diagnostic signature — review packet v1

## Disposition

`TECHNICAL PASS CANDIDATE — OPERATIONAL DIAGNOSTIC FINDING IMPLEMENTED, REVIEW REQUIRED.`

Baseline: `ae5370d23c10fde47bcfa8b13bcfa41ac741a490`, seal revision 20.

This is a diagnostic-only successor to the already closed C125 correction. It does not reopen C125, alter its three corrected hashes, or change Layer 2, post-seal recovery, process restart durability, raw ingress, SDK, or activation governance.

## Repository search and reuse

Search covered the production A3 `CaptureCurrentGuard`, migration tests, retained C125 evidence, and existing `PostgresException.Detail` usage. The existing guard, exact-A3 fixture, `AppendBodyDrift`, `GuardCatalog`, `Bodies`, `History`, and disposable PostgreSQL infrastructure are reused. No new endpoint, engine, schema, migration ID, normalization path, or runtime service was created.

## Product correction

The guard still rejects under the identical condition and retains the exact stable message code:

```sql
RAISE EXCEPTION USING
  MESSAGE = 'A3_CAPTURE_CURRENT_BODY_MISMATCH',
  DETAIL = expected.signature;
```

`MessageText` is unchanged. The exact overloaded PostgreSQL signature is exposed only through `PostgresException.Detail`. Normalization, expected hashes, iteration order, transaction behavior and SQL operation bodies are unchanged.

Product diff: one migration file, `3 additions / 1 deletion`. Working-tree SHA-256: `2982DCF38A76507B1525EFE7E18D7A85EADBDF2DD71FF46773ACEFAC726C1DAE`.

Test diff: one existing migration-test file, `29 additions / 0 deletions`. Working-tree SHA-256: `BC5A57401E4AAB05155F02943A1CBC9C92EEAB803D71E36CE2861C95A7276569`.

No SDK, API, runtime service, project, solution, schema, designer, snapshot, governance, or seal file changed.

## Direct proof

`A3_CaptureCurrentGuard_MismatchReportsExactSignature` has three separately reported theory cases:

1. completion;
2. retained-begin;
3. bound-reader.

Each case migrates a fixture-owned disposable database to exact A3, proves the baseline guard catalog is fully matched, changes exactly the selected function body, proves exactly one catalog mismatch, invokes the real Down path, and asserts:

- `MessageText == A3_CAPTURE_CURRENT_BODY_MISMATCH`;
- `Detail == exact overloaded signature`;
- bodies, history and guard catalog are unchanged by rejection.

The exact observed triplets are retained in `diagnostic_observations_v1.tsv` and the restored-final TRX.

## Mutation and restored gates

| Run | Result | Meaning |
|---|---:|---|
| baseline | `4/4 PASS`, zero skip | three diagnostic cases plus unchanged C125 |
| no-detail mutant | `0/3 PASS`, zero skip | deleting only the new `DETAIL` makes all three exact-signature assertions fail with actual `null` |
| restored final | `12/12 PASS`, zero skip | prior C125 V0.3 nine-case gate plus all three diagnostic cases |

The mutant preserves the rejection code and removes only the new diagnostic payload. It is therefore discriminating for this correction rather than receiving credit for a build, fixture, timeout, or unrelated failure.

## Boundaries and state

- C125 remains closed; this packet changes only failure diagnostics.
- The current activation manifest intentionally drifts while these unreviewed source/test/evidence bytes exist. No re-freeze is performed before review.
- All tests use fixture-owned disposable PostgreSQL databases; no hospital or operational database is involved.
- `docs/00_GDRIVE_FILE_INDEX.md` remains the pre-existing dirty file and is not edited or staged.
- No commit, push, deployment, governance update, or seal re-freeze is part of this packet.

After two reviews PASS on the same bytes, the correction/test/evidence may be committed. Evidence-only re-freeze remains a separate transaction.
